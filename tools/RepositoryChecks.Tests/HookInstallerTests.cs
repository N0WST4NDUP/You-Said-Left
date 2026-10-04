using System.Diagnostics;
using Xunit;

namespace RepositoryChecks.Tests;

public sealed class HookInstallerTests
{
    private const string Marker = "# You Said Left RepositoryChecks hook";

    public static IEnumerable<object[]> PowerShellHosts()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return new object[] { "powershell.exe" };
        }
        yield return new object[] { "pwsh" };
    }

    [Theory]
    [MemberData(nameof(PowerShellHosts))]
    public void DefaultRootInstallsFromCopiedScriptAndPreservesLfsHooks(string shell)
    {
        using var repository = new TestRepository();
        var sourceRoot = FindRepositoryRoot();
        repository.Write("tools/setup-hooks.ps1", File.ReadAllText(Path.Combine(sourceRoot, "tools/setup-hooks.ps1")));
        foreach (var hook in new[] { "pre-commit", "commit-msg" })
        {
            repository.Write("tools/hooks/" + hook, File.ReadAllText(Path.Combine(sourceRoot, "tools/hooks/" + hook)));
        }
        var lfsHooks = new[] { "post-checkout", "post-commit", "post-merge", "pre-push" };
        foreach (var hook in lfsHooks)
        {
            repository.Write(".git/hooks/" + hook, "#!/bin/sh\ngit lfs " + hook + " \"$@\"\n");
        }

        var result = Install(repository, Path.Combine(repository.Root, "tools/setup-hooks.ps1"), false, shell);

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains(Marker, repository.Read(".git/hooks/pre-commit"));
        Assert.Contains(Marker, repository.Read(".git/hooks/commit-msg"));
        foreach (var hook in lfsHooks)
        {
            Assert.Equal("#!/bin/sh\ngit lfs " + hook + " \"$@\"\n", repository.Read(".git/hooks/" + hook));
        }
    }

    [Fact]
    public void InstallsOwnedHooksAndPreservesExistingLfsHooks()
    {
        using var repository = new TestRepository();
        var lfsHooks = new[] { "post-checkout", "post-commit", "post-merge", "pre-push" };
        foreach (var hook in lfsHooks)
        {
            repository.Write(".git/hooks/" + hook, "#!/bin/sh\ngit lfs " + hook + " \"$@\"\n");
        }

        var result = Install(repository);

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.True(File.Exists(Path.Combine(repository.Root, ".git/hooks/pre-commit")));
        Assert.True(File.Exists(Path.Combine(repository.Root, ".git/hooks/commit-msg")));
        Assert.Contains(Marker, repository.Read(".git/hooks/pre-commit"));
        Assert.Contains(Marker, repository.Read(".git/hooks/commit-msg"));
        foreach (var hook in lfsHooks)
        {
            Assert.Equal("#!/bin/sh\ngit lfs " + hook + " \"$@\"\n", repository.Read(".git/hooks/" + hook));
        }
        Assert.Equal(".gitmessage", repository.Git("config", "--local", "--get", "commit.template").Trim());
    }

    [Theory]
    [InlineData("pre-commit", "commit-msg")]
    [InlineData("commit-msg", "pre-commit")]
    public void RefusesUnrelatedHookWithoutInstallingTheOtherHook(string conflictingHook, string otherHook)
    {
        using var repository = new TestRepository();
        const string existing = "#!/bin/sh\nprintf 'custom hook\\n'\n";
        repository.Write(".git/hooks/" + conflictingHook, existing);

        var result = Install(repository);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(existing, repository.Read(".git/hooks/" + conflictingHook));
        Assert.False(File.Exists(Path.Combine(repository.Root, ".git/hooks/" + otherHook)));
    }

    [Fact]
    public void ReinstallsOwnedHooksInConfiguredHooksPathWithoutChangingTheSetting()
    {
        using var repository = new TestRepository();
        repository.Git("config", "--local", "core.hooksPath", ".custom hooks");
        repository.Write(".custom hooks/pre-commit", "#!/bin/sh\n" + Marker + "\nexit 8\n");
        repository.Write(".custom hooks/commit-msg", "#!/bin/sh\n" + Marker + "\nexit 8\n");

        var first = Install(repository);

        Assert.True(first.ExitCode == 0, first.Output);
        Assert.DoesNotContain("exit 8", repository.Read(".custom hooks/pre-commit"));
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "tools/hooks");
        Assert.Equal(File.ReadAllText(Path.Combine(sourceDirectory, "pre-commit")), repository.Read(".custom hooks/pre-commit"));
        Assert.Equal(File.ReadAllText(Path.Combine(sourceDirectory, "commit-msg")), repository.Read(".custom hooks/commit-msg"));
        var second = Install(repository);
        Assert.True(second.ExitCode == 0, second.Output);
        Assert.Equal(File.ReadAllText(Path.Combine(sourceDirectory, "pre-commit")), repository.Read(".custom hooks/pre-commit"));
        Assert.Equal(".custom hooks", repository.Git("config", "--local", "--get", "core.hooksPath").Trim());
        Assert.False(File.Exists(Path.Combine(repository.Root, ".git/hooks/pre-commit")));
        Assert.False(File.Exists(Path.Combine(repository.Root, ".git/hooks/commit-msg")));
    }

    private static (int ExitCode, string Output) Install(TestRepository repository, string? scriptPath = null, bool explicitRoot = true, string? shell = null)
    {
        var start = new ProcessStartInfo(shell ?? (OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh"))
        {
            WorkingDirectory = repository.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath ?? Path.Combine(FindRepositoryRoot(), "tools/setup-hooks.ps1") })
        {
            start.ArgumentList.Add(argument);
        }
        if (explicitRoot)
        {
            start.ArgumentList.Add("-RepositoryRoot");
            start.ArgumentList.Add(repository.Root);
        }
        start.Environment.Remove("GIT_DIR");
        start.Environment.Remove("GIT_WORK_TREE");
        start.Environment.Remove("GIT_INDEX_FILE");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill(true);
            throw new TimeoutException("Hook installer exceeded 30 seconds.");
        }
        return (process.ExitCode, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tools/setup-hooks.ps1")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Repository hook installer was not found.");
    }
}
