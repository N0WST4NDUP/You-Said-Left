using System.Text;
using System.Text.Json;

namespace RepositoryChecks;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args is ["--help"] or ["-h"])
            {
                Console.WriteLine("RepositoryChecks: lint --all | lint --staged | commit --file <파일> | commit --range <base>..<head> [--root <저장소>]");
                Console.WriteLine("종료 코드: 0 통과, 1 규칙 위반, 2 인자·도구 실행 오류. 소스 파일은 수정하지 않습니다.");
                return 0;
            }
            var options = ParseArguments(args);
            var root = GitInputs.FindRoot(options.GetValueOrDefault("--root"));
            var git = new GitInputs(root);
            if (args[0] == "lint")
            {
                using var snapshot = options.ContainsKey("--staged") ? git.ReadStagedCode() : null;
                var files = snapshot?.Files ?? git.ReadWorkingCode();
                if (files.Count == 0)
                {
                    Console.WriteLine("검사 대상 0개 (Assets/YouSaidLeft/**/*.cs).");
                    return 0;
                }
                var diagnostics = files.SelectMany(file => CodeRules.Analyze(file.Content, file.Path)).ToArray();
                foreach (var diagnostic in diagnostics) { Console.Error.WriteLine(diagnostic); }
                var formatExit = CheckFormatting(snapshot?.Root ?? root, files, root);
                Console.WriteLine($"코드 검사 {files.Count}개, 구문·규칙 위반 {diagnostics.Length}개. Unity 컴파일·플레이 검증은 별도입니다.");
                return diagnostics.Length > 0 || formatExit != 0 ? 1 : 0;
            }
            IReadOnlyList<CheckDiagnostic> commitDiagnostics;
            var count = 1;
            if (options.TryGetValue("--file", out var messageFile))
            {
                var path = Path.GetFullPath(messageFile!, root);
                var message = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
                commitDiagnostics = CommitRules.Analyze(message, path, git.CleanupCommentPrefix());
            }
            else
            {
                var commits = git.ReadCommits(options["--range"]!);
                count = commits.Count;
                commitDiagnostics = commits.SelectMany(commit => CommitRules.Analyze(commit.Content, commit.Sha, commentPrefix: null)).ToArray();
            }
            foreach (var diagnostic in commitDiagnostics) { Console.Error.WriteLine(diagnostic); }
            Console.WriteLine($"커밋 메시지 검사 {count}개, 위반 {commitDiagnostics.Count}개.");
            return commitDiagnostics.Count == 0 ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"CHECK002: 검사 실행 실패: {error.Message}");
            return 2;
        }
    }

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        if (args[0] is not ("lint" or "commit")) { throw new ArgumentException("알 수 없는 검사 명령입니다. --help를 확인하세요."); }
        var options = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length; i++)
        {
            var key = args[i];
            if (key is not ("--root" or "--all" or "--staged" or "--file" or "--range")) { throw new ArgumentException($"알 수 없는 인자: {key}"); }
            string? value = null;
            if (key is "--root" or "--file" or "--range")
            {
                if (++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal)) { throw new ArgumentException($"{key} 값이 필요합니다."); }
                value = args[i];
            }
            if (!options.TryAdd(key, value)) { throw new ArgumentException($"중복 인자: {key}"); }
        }
        var validLint = args[0] == "lint" && options.ContainsKey("--all") != options.ContainsKey("--staged") && !options.ContainsKey("--file") && !options.ContainsKey("--range");
        var validCommit = args[0] == "commit" && options.ContainsKey("--file") != options.ContainsKey("--range") && !options.ContainsKey("--all") && !options.ContainsKey("--staged");
        if (!validLint && !validCommit) { throw new ArgumentException("lint는 --all 또는 --staged, commit은 --file 또는 --range 하나를 지정하세요."); }
        return options;
    }

    private static int CheckFormatting(string workspace, IReadOnlyList<SourceFile> files, string repositoryRoot)
    {
        using var scratch = GitInputs.CreateScratch(workspace, "format");
        var reportRoot = scratch.Root;
        var sourceRoot = Path.Combine(workspace, "Assets", "YouSaidLeft");
        var includes = files.Select(file => file.Path["Assets/YouSaidLeft/".Length..]).ToArray();
        var locations = new Dictionary<string, (string Path, int Offset)>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var file in files) { locations.Add(GitInputs.SafePath(workspace, file.Path), (GitInputs.SafePath(repositoryRoot, file.Path), 0)); }
        if (RunFormatting(workspace, sourceRoot, includes, Path.Combine(reportRoot, "base.json"), locations) != 0) { return 1; }

        // Folder mode has no Unity defines. Activate each conditional branch in disposable copies.
        var branchRoot = Path.Combine(reportRoot, "branches");
        var variantCount = 0;
        for (var fileIndex = 0; fileIndex < files.Count; fileIndex++)
        {
            var file = files[fileIndex];
            var symbols = CodeRules.GetConditionalSymbols(file.Content);
            if (symbols.Length is 0 or > CodeRules.MaximumConditionalSymbols) { continue; }
            var lineEnding = file.Content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            for (var assignment = 1; assignment < 1 << symbols.Length; assignment++)
            {
                var variant = Path.Combine(branchRoot, $"{fileIndex}-{assignment}");
                var target = GitInputs.SafePath(variant, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var defines = symbols.Where((_, index) => (assignment & (1 << index)) != 0)
                    .Select(symbol => "#define " + symbol + lineEnding).ToArray();
                File.WriteAllText(target, string.Concat(defines) + file.Content.TrimStart('\uFEFF'), new UTF8Encoding(file.Content.StartsWith('\uFEFF')));

                for (var directory = Path.GetDirectoryName(file.Path); !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
                {
                    CopyConfiguration(Path.Combine(directory, ".editorconfig"), workspace, variant);
                }
                CopyConfiguration(".editorconfig", workspace, variant);
                locations.Add(target, (GitInputs.SafePath(repositoryRoot, file.Path), defines.Length));
                variantCount++;
            }
        }
        return variantCount == 0 ? 0 : RunFormatting(workspace, branchRoot, null, Path.Combine(reportRoot, "branches.json"), locations);
    }

    private static void CopyConfiguration(string relativePath, string workspace, string variant)
    {
        var source = GitInputs.SafePath(workspace, relativePath);
        if (!File.Exists(source)) { return; }
        GitInputs.RejectLinks(workspace, source);
        var target = GitInputs.SafePath(variant, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
    }

    private static int RunFormatting(string workspace, string folder, IReadOnlyList<string>? includes, string report,
        IReadOnlyDictionary<string, (string Path, int Offset)> locations)
    {
        var arguments = new List<string> { "format", "whitespace", folder, "--folder", "--verify-no-changes", "--report", report };
        if (includes is not null)
        {
            arguments.Add("--include");
            arguments.AddRange(includes);
        }
        var result = ProcessRunner.Run("dotnet", workspace, arguments);
        if (result.ExitCode == 0) { return 0; }
        if (File.Exists(report))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(report));
            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
            {
                var diagnostics = new HashSet<CheckDiagnostic>();
                foreach (var entry in document.RootElement.EnumerateArray())
                {
                    var path = entry.GetProperty("FilePath").GetString()!;
                    var location = locations.TryGetValue(path, out var original) ? original : (Path: path, Offset: 0);
                    foreach (var change in entry.GetProperty("FileChanges").EnumerateArray())
                    {
                        diagnostics.Add(new CheckDiagnostic("FORMAT-" + change.GetProperty("DiagnosticId").GetString(),
                            change.GetProperty("FormatDescription").GetString()!, location.Path,
                            Math.Max(1, change.GetProperty("LineNumber").GetInt32() - location.Offset), change.GetProperty("CharNumber").GetInt32()));
                    }
                }
                foreach (var diagnostic in diagnostics.OrderBy(item => item.Path, StringComparer.Ordinal).ThenBy(item => item.Line).ThenBy(item => item.Column))
                {
                    Console.Error.WriteLine(diagnostic);
                }
                if (diagnostics.Count == 0) { Console.Error.WriteLine("FORMAT001: .editorconfig에 맞지 않는 포맷입니다."); }
                return 1;
            }
        }
        throw new InvalidOperationException($"포맷 도구 실패 ({result.ExitCode}): {result.Error.Trim()} {result.Text.Trim()}");
    }
}
