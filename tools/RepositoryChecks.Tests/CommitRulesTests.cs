using Xunit;

namespace RepositoryChecks.Tests;

public sealed class CommitRulesTests
{
    [Fact]
    public void AllowsRemoteMergeGeneratedByGitPull()
    {
        Assert.Empty(CommitRules.Analyze("Merge branch 'topic' of https://example.com/team/repo\n"));
    }

    [Fact]
    public void AllowsDefaultMergeRevertMessage()
    {
        Assert.Empty(CommitRules.Analyze("Revert \"Merge branch 'topic'\"\n\nThis reverts commit 0123456789abcdef0123456789abcdef01234567, reversing\nchanges made to 89abcdef0123456789abcdef0123456789abcdef.\n"));
    }
    [Theory]
    [InlineData("Feat: 조향 제한 추가")]
    [InlineData("Fix(core): 적색 신호 정지 누락 수정")]
    [InlineData("Docs(docs): 첫 레벨 규칙 작성 (#12)")]
    [InlineData("Refactor(uiux): 표시 상태 분리")]
    [InlineData("Test: 속도 계산 회귀 검사 추가")]
    [InlineData("Chore(pkg): 입력 패키지 버전 고정")]
    [InlineData("Feat(vfx): 제동 효과 추가")]
    [InlineData("Feat(sfx): 충돌 소리 추가")]
    [InlineData("Chore(unity): Editor 설정 변경")]
    [InlineData("Fix: 2인 접속 오류 수정\n\n원인과 확인한 동작을 기록한다\n추가 기록")]
    public void AcceptsConfiguredTitleAndOptionalBody(string message)
    {
        Assert.Empty(CommitRules.Analyze(message));
    }

    [Theory]
    [InlineData("", "COMMIT001")]
    [InlineData("\n# 도움말\n", "COMMIT001")]
    [InlineData("작업 완료", "COMMIT001")]
    [InlineData("Feat:조향 제한 추가", "COMMIT001")]
    [InlineData("Feat : 조향 제한 추가", "COMMIT001")]
    [InlineData("Feat(): 조향 제한 추가", "COMMIT001")]
    [InlineData("feat: 조향 제한 추가", "COMMIT002")]
    [InlineData("Init(pkg): 프로젝트 초기 구성", "COMMIT002")]
    [InlineData("Feat(driving): 조향 제한 추가", "COMMIT003")]
    [InlineData("Feat(Core): 조향 제한 추가", "COMMIT003")]
    [InlineData("Feat: ", "COMMIT004")]
    [InlineData("Feat:     ", "COMMIT004")]
    [InlineData("Feat: 조향 제한 추가.", "COMMIT004")]
    [InlineData("Feat: 조향 제한 추가. (#1)", "COMMIT004")]
    [InlineData("Feat: (#1)", "COMMIT004")]
    [InlineData("Feat: 조향 제한 추가 (#0)", "COMMIT005")]
    [InlineData("Feat: 조향 제한 추가 (#01)", "COMMIT005")]
    [InlineData("Feat: 조향 제한 추가 (#-1)", "COMMIT005")]
    [InlineData("Feat: 조향 제한 추가 (#abc)", "COMMIT005")]
    [InlineData("Feat: 조향 제한 추가 (#1) 뒤에 문장", "COMMIT005")]
    [InlineData("Feat: 조향 제한 추가\n이유", "COMMIT006")]
    [InlineData("Feat: 조향 제한 추가\n  이유", "COMMIT006")]
    [InlineData("fixup! Feat: 조향 제한 추가", "COMMIT001")]
    [InlineData("squash! Feat: 조향 제한 추가", "COMMIT001")]
    public void ReportsMalformedMessages(string message, string diagnosticId)
    {
        Assert.Contains(CommitRules.Analyze(message), diagnostic => diagnostic.Id == diagnosticId);
    }

    [Fact]
    public void AcceptsBomCrLfAndTemplateComments()
    {
        const string message = "\uFEFF\r\n# 작성 도움말\r\n\r\nFeat(core): 조향 제한 추가\r\n\r\n이유와 검증\r\n# 주석\r\n";

        Assert.Empty(CommitRules.Analyze(message));
    }

    [Fact]
    public void SupportsCustomCommentPrefix()
    {
        const string message = "// 작성 도움말\nFix: 충돌 판정 수정\n\n// 본문 도움말\n확인한 동작";

        Assert.Empty(CommitRules.Analyze(message, commentPrefix: "//"));
    }

    [Fact]
    public void HistoryDoesNotDiscardLiteralHashBodyWithoutBlankSeparator()
    {
        const string message = "Fix: 충돌 판정 수정\n#12의 재현 절차를 사용했다";

        Assert.Contains(CommitRules.Analyze(message, commentPrefix: null), diagnostic => diagnostic.Id == "COMMIT006");
    }

    [Fact]
    public void HistoryRetainsLiteralHashTitle()
    {
        const string message = "#12 재현 기록\n\nFix: 충돌 판정 수정";

        Assert.Contains(CommitRules.Analyze(message, commentPrefix: null), diagnostic => diagnostic.Id == "COMMIT001");
    }

    [Fact]
    public void DiagnosticUsesOriginalTitleLineAndSuppliedPath()
    {
        const string message = "\n# 작성 도움말\nfeat: 조향 제한 추가";

        CheckDiagnostic diagnostic = Assert.Single(CommitRules.Analyze(message, "커밋 메시지.txt"));

        Assert.Equal("COMMIT002", diagnostic.Id);
        Assert.Equal("커밋 메시지.txt", diagnostic.Path);
        Assert.Equal(3, diagnostic.Line);
    }

    [Theory]
    [InlineData("Merge branch 'feat/steering'")]
    [InlineData("Merge branch 'feat/steering' into main")]
    [InlineData("Merge remote-tracking branch 'origin/main'")]
    [InlineData("Merge tag 'v0.1'")]
    [InlineData("Merge commit '0123456789abcdef0123456789abcdef01234567'")]
    [InlineData("Merge pull request #12 from example/feat-steering\n\nFeat: 조향 제한 추가")]
    public void AcceptsNarrowGitGeneratedMergeTitles(string message)
    {
        Assert.Empty(CommitRules.Analyze(message, commentPrefix: null));
    }

    [Theory]
    [InlineData("Merge 조향 작업")]
    [InlineData("Merge branch ''")]
    [InlineData("Merge commit 'anything'")]
    [InlineData("Merge pull request #0 from example/topic")]
    public void DoesNotExemptArbitraryMergeText(string message)
    {
        Assert.Contains(CommitRules.Analyze(message, commentPrefix: null), diagnostic => diagnostic.Id == "COMMIT001");
    }

    [Theory]
    [InlineData("Revert \"Feat: 조향 제한 추가\"\n\nThis reverts commit 0123456789abcdef0123456789abcdef01234567.")]
    [InlineData("Revert \"Fix: 충돌 판정 수정\"\n\nThis reverts commit 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.")]
    public void AcceptsGitGeneratedRevertWithFullObjectId(string message)
    {
        Assert.Empty(CommitRules.Analyze(message, commentPrefix: null));
    }

    [Theory]
    [InlineData("Revert \"Feat: 조향 제한 추가\"")]
    [InlineData("Revert \"Feat: 조향 제한 추가\"\n\n임의의 본문")]
    [InlineData("Revert \"Feat: 조향 제한 추가\"\n\nThis reverts commit 0123456.")]
    [InlineData("Revert 작업\n\nThis reverts commit 0123456789abcdef0123456789abcdef01234567.")]
    public void DoesNotExemptIncompleteRevertText(string message)
    {
        Assert.Contains(CommitRules.Analyze(message, commentPrefix: null), diagnostic => diagnostic.Id == "COMMIT001");
    }
}
