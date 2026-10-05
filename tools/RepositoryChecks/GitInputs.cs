using System.Diagnostics;
using System.Text;

namespace RepositoryChecks;

public sealed class GitInputs(string root)
{
    // 검사 도입 이전 이력은 새 메시지 규칙으로 소급 검사하지 않는다.
    public const string LegacyBaseline = "064156e824b71f0e744ebaaa279e6b45e9cfff6c";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public string Root { get; } = Path.GetFullPath(root);

    public static string FindRoot(string? requestedRoot = null)
    {
        var directory = Path.GetFullPath(requestedRoot ?? Environment.CurrentDirectory);
        return ProcessRunner.Run("git", directory, ["rev-parse", "--show-toplevel"]).EnsureSuccess().Text.Trim();
    }

    public IReadOnlyList<SourceFile> ReadWorkingCode()
    {
        var paths = Git("ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "Assets/Scripts")
            .Text.Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(IsOwnedCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var sources = new List<SourceFile>();
        foreach (var path in paths)
        {
            var absolutePath = SafePath(Root, path);
            if (!File.Exists(absolutePath)) { continue; }
            RejectLinks(Root, absolutePath);
            sources.Add(new SourceFile(path, StrictUtf8.GetString(File.ReadAllBytes(absolutePath))));
        }
        return sources;
    }

    public StagedSnapshot ReadStagedCode()
    {
        var entries = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
        foreach (var record in Git("ls-files", "--stage", "-z").Text.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = record.IndexOf('\t');
            var header = record[..tab].Split(' ');
            var path = record[(tab + 1)..];
            if (header[2] != "0") { throw new InvalidOperationException($"병합 충돌을 먼저 해결하세요: {path}"); }
            entries.Add(path, new IndexEntry(header[0], header[1]));
        }
        var changed = Git("diff", "--cached", "--name-only", "-z").Text
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var rulesChanged = changed.Any(path => IsApplicableConfiguration(path) || path == "global.json" || path.StartsWith("tools/RepositoryChecks/", StringComparison.Ordinal));
        var selected = entries.Keys.Where(IsOwnedCode)
            .Where(path => rulesChanged || changed.Contains(path, StringComparer.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        var files = new List<SourceFile>();
        var snapshot = CreateScratch(Root, "snapshot", files);
        var snapshotRoot = snapshot.Root;
        try
        {
            // 단계별 EditorConfig도 인덱스에서 읽어 작업 트리 설정과 섞지 않는다.
            foreach (var path in entries.Keys.Where(IsApplicableConfiguration).Concat(selected))
            {
                var entry = entries[path];
                if (entry.Mode is not ("100644" or "100755")) { throw new InvalidOperationException($"일반 파일만 검사합니다: {path}"); }
                var bytes = Git("cat-file", "blob", entry.Sha).Bytes;
                var target = SafePath(snapshotRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, bytes);
                if (IsOwnedCode(path)) { files.Add(new SourceFile(path, StrictUtf8.GetString(bytes))); }
            }
            return snapshot;
        }
        catch
        {
            snapshot.Dispose();
            throw;
        }
    }

    public IReadOnlyList<CommitMessage> ReadCommits(string range)
    {
        var parts = range.Split("..", StringSplitOptions.None);
        if (parts.Length != 2 || parts.Any(part => string.IsNullOrWhiteSpace(part) || part.StartsWith('-') || part.Contains(' ') || part.StartsWith('.')))
        {
            throw new ArgumentException("커밋 범위는 <기준 커밋>..<대상 커밋> 형식이어야 합니다.");
        }
        var from = Git("rev-parse", "--verify", parts[0] + "^{commit}").Text.Trim();
        var to = Git("rev-parse", "--verify", parts[1] + "^{commit}").Text.Trim();
        var arguments = new List<string> { "rev-list", "--reverse", from + ".." + to };
        if (ProcessRunner.Run("git", Root, ["cat-file", "-e", LegacyBaseline + "^{commit}"]).ExitCode == 0)
        {
            arguments.Add("--not");
            arguments.Add(LegacyBaseline);
        }
        return Git([.. arguments]).Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(sha => new CommitMessage(sha, Git("show", "-s", "--format=%B", sha).Text)).ToArray();
    }

    public string? CleanupCommentPrefix()
    {
        var cleanup = Configuration("commit.cleanup");
        if (cleanup is null or "default") { cleanup = Environment.GetEnvironmentVariable("GIT_EDITOR") == ":" ? "whitespace" : "strip"; }
        if (cleanup != "strip") { return null; }
        var prefix = Configuration("core.commentString") ?? Configuration("core.commentChar") ?? "#";
        if (prefix == "auto") { throw new InvalidOperationException("core.commentChar=auto는 지원하지 않습니다. 명시적 주석 문자를 설정하세요."); }
        return prefix;
    }

    private string? Configuration(string name)
    {
        var result = ProcessRunner.Run("git", Root, ["config", "--get", name]);
        if (result.ExitCode == 1) { return null; }
        return result.EnsureSuccess().Text.Trim();
    }

    private ProcessResult Git(params string[] arguments) => ProcessRunner.Run("git", Root, arguments).EnsureSuccess();
    private static bool IsOwnedCode(string path) => path.StartsWith("Assets/Scripts/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal);
    private static bool IsApplicableConfiguration(string path) => path is ".editorconfig" or "Assets/.editorconfig" or "Assets/Scripts/.editorconfig" ||
        path.StartsWith("Assets/Scripts/", StringComparison.Ordinal) && path.EndsWith("/.editorconfig", StringComparison.Ordinal);

    internal static StagedSnapshot CreateScratch(string workspace, string name, IReadOnlyList<SourceFile>? files = null)
    {
        var owner = Path.Combine(workspace, ".utmp", "RepositoryChecks");
        var existing = owner;
        while (!Directory.Exists(existing)) { existing = Path.GetDirectoryName(existing)!; }
        RejectLinks(workspace, existing);
        Directory.CreateDirectory(owner);
        RejectLinks(workspace, owner);
        var target = Path.Combine(owner, name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(target);
        return new StagedSnapshot(target, owner, files ?? []);
    }

    internal static string SafePath(string root, string path)
    {
        var target = Path.GetFullPath(Path.Combine(root, path));
        var prefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidOperationException("검사 경로가 작업 디렉터리를 벗어났습니다.");
        }
        return target;
    }
    internal static void RejectLinks(string root, string path)
    {
        for (var current = path; current.Length >= root.Length; current = Path.GetDirectoryName(current)!)
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) { throw new InvalidOperationException($"심볼릭 링크·junction은 검사하지 않습니다: {current}"); }
            if (string.Equals(current, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) { break; }
        }
    }
    private sealed record IndexEntry(string Mode, string Sha);
}

public sealed record SourceFile(string Path, string Content);
public sealed record CommitMessage(string Sha, string Content);

public sealed class StagedSnapshot(string root, string owner, IReadOnlyList<SourceFile> files) : IDisposable
{
    public string Root { get; } = root;
    public IReadOnlyList<SourceFile> Files { get; } = files;
    public void Dispose()
    {
        if (!Directory.Exists(Root)) { return; }
        var target = GitInputs.SafePath(owner, Path.GetFileName(Root));
        if (!string.Equals(target, Path.GetFullPath(Root), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) { throw new InvalidOperationException("임시 디렉터리 소유자가 일치하지 않습니다."); }
        GitInputs.RejectLinks(owner, target);
        Directory.Delete(target, true);
    }
}

internal sealed record ProcessResult(int ExitCode, byte[] Bytes, string Error)
{
    public string Text => Encoding.UTF8.GetString(Bytes);
    public ProcessResult EnsureSuccess()
    {
        if (ExitCode != 0) { throw new InvalidOperationException(Error.Trim().Length > 0 ? Error.Trim() : $"명령이 종료 코드 {ExitCode}로 실패했습니다."); }
        return this;
    }
}

internal static class ProcessRunner
{
    internal static ProcessResult Run(string executable, string directory, IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo(executable) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) { start.ArgumentList.Add(argument); }
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"명령을 시작하지 못했습니다: {executable}");
        using var output = new MemoryStream();
        var stdout = process.StandardOutput.BaseStream.CopyToAsync(output);
        var stderr = process.StandardError.ReadToEndAsync();
        Task.WaitAll(stdout, stderr);
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToArray(), stderr.Result);
    }
}
