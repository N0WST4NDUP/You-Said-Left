[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-NoReparsePoint {
    param([string]$Path)

    $currentPath = [System.IO.Path]::GetFullPath($Path)
    while ($currentPath) {
        $item = Get-Item -LiteralPath $currentPath -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing a symbolic link or reparse point: $currentPath"
        }
        $parent = [System.IO.Directory]::GetParent($currentPath)
        $currentPath = if ($null -eq $parent) { $null } else { $parent.FullName }
    }
}

try {
    $scriptDirectory = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
    if (-not $PSBoundParameters.ContainsKey('RepositoryRoot')) {
        $RepositoryRoot = Join-Path $scriptDirectory '..'
    }
    $resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
    Assert-NoReparsePoint -Path $resolvedRoot
    $gitRoot = & git -C $resolvedRoot rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot find repository root: $gitRoot"
    }
    $gitRoot = [System.IO.Path]::GetFullPath(($gitRoot -join "`n").Trim())
    $hooksPath = & git -C $gitRoot rev-parse --git-path hooks 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot find Git hooks directory: $hooksPath"
    }
    $hooksPath = ($hooksPath -join "`n").Trim()
    if (-not [System.IO.Path]::IsPathRooted($hooksPath)) {
        $hooksPath = Join-Path $gitRoot $hooksPath
    }
    $hooksPath = [System.IO.Path]::GetFullPath($hooksPath)
    Assert-NoReparsePoint -Path $hooksPath
    if ((Test-Path -LiteralPath $hooksPath) -and -not (Test-Path -LiteralPath $hooksPath -PathType Container)) {
        throw "Git hooks path is not a directory: $hooksPath"
    }

    $marker = '# You Said Left RepositoryChecks hook'
    $ownedPrefix = "#!/bin/sh`n$marker`n"
    $hooks = @('pre-commit', 'commit-msg')
    $hookContents = @{}

    # Check both destinations before writing either hook.
    foreach ($hook in $hooks) {
        $source = Join-Path (Join-Path $scriptDirectory 'hooks') $hook
        $destination = Join-Path $hooksPath $hook
        Assert-NoReparsePoint -Path $source
        Assert-NoReparsePoint -Path $destination
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Hook source is missing: $source"
        }
        $content = [System.IO.File]::ReadAllText($source).Replace("`r`n", "`n")
        if (-not $content.StartsWith($ownedPrefix, [System.StringComparison]::Ordinal)) {
            throw "Hook source ownership marker is missing: $source"
        }
        if (Test-Path -LiteralPath $destination) {
            if (-not (Test-Path -LiteralPath $destination -PathType Leaf)) {
                throw "Existing hook is not a file: $destination"
            }
            $existing = [System.IO.File]::ReadAllText($destination).Replace("`r`n", "`n")
            if (-not $existing.StartsWith($ownedPrefix, [System.StringComparison]::Ordinal)) {
                throw "Existing hook is not owned by RepositoryChecks: $destination"
            }
        }
        $hookContents[$hook] = $content
    }

    [void][System.IO.Directory]::CreateDirectory($hooksPath)
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $onWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
    foreach ($hook in $hooks) {
        $destination = Join-Path $hooksPath $hook
        [System.IO.File]::WriteAllText($destination, $hookContents[$hook], $encoding)
        if (-not $onWindows) {
            & chmod +x -- $destination
            if ($LASTEXITCODE -ne 0) {
                throw "Cannot make hook executable: $destination"
            }
        }
    }

    & git -C $gitRoot config --local commit.template .gitmessage
    if ($LASTEXITCODE -ne 0) {
        throw 'Cannot configure the commit message template.'
    }
    Write-Output "Installed pre-commit and commit-msg in $hooksPath"
    exit 0
}
catch {
    [System.Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
