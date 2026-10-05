using System.Text.RegularExpressions;

namespace RepositoryChecks;

public static class CommitRules
{
    private static readonly HashSet<string> Types = ["Feat", "Fix", "Docs", "Refactor", "Test", "Chore"];
    private static readonly HashSet<string> Scopes = ["core", "vfx", "sfx", "uiux", "docs", "adr", "unity", "pkg"];

    private static readonly Regex TitlePattern = new(
        @"\A(?<type>[A-Za-z]+)(?:\((?<scope>[^()\s]+)\))?: (?<summary>.*)\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex IssueSuffixPattern = new(
        @"(?:\A| )\(#[1-9][0-9]*\)\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex MergePattern = new(
        @"\AMerge (?:(?:branch|remote-tracking branch|tag) '[^']+'(?: of \S+)?(?: into \S+)?|commit '[0-9a-fA-F]{40}(?:[0-9a-fA-F]{24})?'(?: into \S+)?|pull request #[1-9][0-9]* from [^\s/]+/\S+)\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex RevertTitlePattern = new(
        "\\ARevert \".+\"\\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex RevertBodyPattern = new(
        @"\AThis reverts commit [0-9a-fA-F]{40}(?:[0-9a-fA-F]{24})?\.\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex MergeRevertBodyPattern = new(
        @"\AThis reverts commit [0-9a-fA-F]{40}(?:[0-9a-fA-F]{24})?, reversing\nchanges made to [0-9a-fA-F]{40}(?:[0-9a-fA-F]{24})?\.\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static IReadOnlyList<CheckDiagnostic> Analyze(
        string message,
        string path = "commit",
        string? commentPrefix = "#")
    {
        ArgumentNullException.ThrowIfNull(message);

        string normalized = message.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n')
            .Select((text, index) => (Text: text, Number: index + 1))
            .Where(line => string.IsNullOrEmpty(commentPrefix) || !line.Text.StartsWith(commentPrefix, StringComparison.Ordinal))
            .SkipWhile(line => string.IsNullOrWhiteSpace(line.Text))
            .ToArray();

        if (lines.Length == 0)
        {
            return [new CheckDiagnostic("COMMIT001", "커밋 제목이 비어 있습니다.", path)];
        }

        var diagnostics = new List<CheckDiagnostic>();
        string title = lines[0].Text;
        int titleLine = lines[0].Number;

        if (lines.Length > 1 && !string.IsNullOrWhiteSpace(lines[1].Text))
        {
            diagnostics.Add(new CheckDiagnostic("COMMIT006", "제목과 본문 사이에 빈 줄을 넣으세요.", path, lines[1].Number));
        }

        bool generatedMerge = MergePattern.IsMatch(title);
        bool generatedRevert = RevertTitlePattern.IsMatch(title)
            && (lines.Skip(1).Any(line => RevertBodyPattern.IsMatch(line.Text))
                || lines.Skip(1).Zip(lines.Skip(2), (first, second) => first.Text + "\n" + second.Text)
                    .Any(MergeRevertBodyPattern.IsMatch));

        if (generatedMerge || generatedRevert)
        {
            return diagnostics;
        }

        Match match = TitlePattern.Match(title);
        if (!match.Success)
        {
            diagnostics.Add(new CheckDiagnostic("COMMIT001", "제목은 Type(scope): 요약 또는 Type: 요약 형식을 사용하세요.", path, titleLine));
            return diagnostics;
        }

        if (!Types.Contains(match.Groups["type"].Value))
        {
            diagnostics.Add(new CheckDiagnostic("COMMIT002", "Type은 Feat, Fix, Docs, Refactor, Test, Chore 중 하나를 사용하세요.", path, titleLine));
        }

        Group scope = match.Groups["scope"];
        if (scope.Success && !Scopes.Contains(scope.Value))
        {
            diagnostics.Add(new CheckDiagnostic("COMMIT003", $"scope는 {string.Join(", ", Scopes)} 중 하나를 사용하거나 생략하세요.", path, titleLine));
        }

        string summary = match.Groups["summary"].Value.TrimEnd();
        int issueStart = summary.IndexOf("(#", StringComparison.Ordinal);
        if (issueStart >= 0)
        {
            Match suffix = IssueSuffixPattern.Match(summary);
            if (!suffix.Success || summary[..suffix.Index].Contains("(#", StringComparison.Ordinal))
            {
                diagnostics.Add(new CheckDiagnostic("COMMIT005", "이슈 참조는 제목 끝에 (#양의정수) 형식으로 쓰거나 생략하세요.", path, titleLine));
            }
            else
            {
                summary = summary[..suffix.Index].TrimEnd();
            }
        }

        if (string.IsNullOrWhiteSpace(summary) || summary.EndsWith(".", StringComparison.Ordinal))
        {
            diagnostics.Add(new CheckDiagnostic("COMMIT004", "요약을 작성하고 끝에 마침표를 붙이지 마세요.", path, titleLine));
        }

        return diagnostics;
    }
}
