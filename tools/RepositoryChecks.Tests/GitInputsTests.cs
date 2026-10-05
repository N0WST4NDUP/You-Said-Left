using System.Diagnostics;
using System.Text;
using Xunit;

namespace RepositoryChecks.Tests;

public sealed class GitInputsTests
{
    [Fact]
    public void StagedSnapshotUsesIndexContentAndIndexConfiguration()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        repository.Write("Assets/Scripts/Vehicle.cs", "staged source\n");
        repository.Git("add", ".editorconfig", "Assets/Scripts/Vehicle.cs");
        repository.Write("Assets/Scripts/Vehicle.cs", "unstaged source\n");
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 2\n");

        using var snapshot = new GitInputs(repository.Root).ReadStagedCode();

        var source = Assert.Single(snapshot.Files);
        Assert.Equal("Assets/Scripts/Vehicle.cs", source.Path);
        Assert.Equal("staged source\n", source.Content);
        Assert.Contains("indent_size = 4", File.ReadAllText(Path.Combine(snapshot.Root, ".editorconfig")));
        Assert.Equal("unstaged source\n", repository.Read("Assets/Scripts/Vehicle.cs"));
    }

    [Fact]
    public void WorkingCodeIncludesUntrackedOwnedSourceAndExcludesThirdPartySource()
    {
        using var repository = new TestRepository();
        repository.Write("Assets/Scripts/차량 파일.cs", "owned source\n");
        repository.Write("Assets/TutorialInfo/External.cs", "external source\n");
        repository.Write("Assets/Scripts/Generated.cs", "ignored source\n");
        repository.Write(".gitignore", "Generated.cs\n");

        var sources = new GitInputs(repository.Root).ReadWorkingCode();

        Assert.Equal("Assets/Scripts/차량 파일.cs", Assert.Single(sources).Path);
    }

    [Fact]
    public void ConfigurationOnlyStageChecksAllIndexedOwnedCodeAndExcludesDeletedFiles()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n");
        repository.Write("Assets/Scripts/Vehicle.cs", "source\n");
        repository.Write("Assets/Scripts/Removed.cs", "removed\n");
        repository.Git("add", ".");
        repository.Git("rm", "-f", "Assets/Scripts/Removed.cs");

        using var snapshot = new GitInputs(repository.Root).ReadStagedCode();

        Assert.Equal("Assets/Scripts/Vehicle.cs", Assert.Single(snapshot.Files).Path);
    }

    [Fact]
    public void SnapshotDisposalRemovesOnlyOwnedTemporaryDirectory()
    {
        using var repository = new TestRepository();
        repository.Write("Assets/Scripts/Vehicle.cs", "source\n");
        repository.Git("add", ".");
        var snapshot = new GitInputs(repository.Root).ReadStagedCode();
        var temporaryRoot = snapshot.Root;
        snapshot.Dispose();

        Assert.False(Directory.Exists(temporaryRoot));
        Assert.True(File.Exists(Path.Combine(repository.Root, "Assets/Scripts/Vehicle.cs")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedConfigurationOnlyChangesOrDeletionRecheckIndexedCode(bool deleted)
    {
        using var repository = new TestRepository();
        repository.Write("Assets/Scripts/.editorconfig", "[*.cs]\nindent_size = 4\n");
        repository.Write("Assets/Scripts/Vehicle.cs", "source\n");
        repository.CommitFixture("Chore: fixture baseline");
        if (deleted)
        {
            repository.Git("rm", "Assets/Scripts/.editorconfig");
        }
        else
        {
            repository.Write("Assets/Scripts/.editorconfig", "[*.cs]\nindent_size = 2\n");
            repository.Git("add", "Assets/Scripts/.editorconfig");
        }
        using var snapshot = new GitInputs(repository.Root).ReadStagedCode();
        Assert.Equal("Assets/Scripts/Vehicle.cs", Assert.Single(snapshot.Files).Path);
    }

    [Fact]
    public void SnapshotIncludesIndexedIntermediateAncestorConfiguration()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n");
        repository.Write("Assets/.editorconfig", "[*.cs]\nindent_size = 2\n");
        repository.Write("Assets/Scripts/Vehicle.cs", "source\n");
        repository.Git("add", ".");
        using var snapshot = new GitInputs(repository.Root).ReadStagedCode();
        Assert.Contains("indent_size = 2", File.ReadAllText(Path.Combine(snapshot.Root, "Assets/.editorconfig")));
    }

    [Fact]
    public void CommitRangeReadsActualNewMessagesWithoutStrippingHashBody()
    {
        using var repository = new TestRepository();
        repository.Write("file.txt", "first\n");
        var baseline = repository.CommitFixture("Chore: fixture baseline");
        repository.Write("file.txt", "second\n");
        repository.CommitFixture("Feat(core): 가속 추가\n\n#42는 관련 이슈입니다");
        var commit = Assert.Single(new GitInputs(repository.Root).ReadCommits(baseline + "..HEAD"));
        Assert.StartsWith("Feat(core): 가속 추가\n\n#42", commit.Content);
    }
}

internal sealed class TestRepository : IDisposable
{
    private static readonly string TestBase = Path.Combine(Path.GetTempPath(), "YouSaidLeft-RepositoryChecks-Tests");
    public string Root { get; } = Path.Combine(TestBase, "repository with spaces " + Guid.NewGuid().ToString("N"));

    public TestRepository()
    {
        Directory.CreateDirectory(Root);
        Git("init", "--quiet");
        Git("config", "core.autocrlf", "false");
    }

    public void Write(string path, string content)
    {
        var absolutePath = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.WriteAllText(absolutePath, content, new UTF8Encoding(false));
    }

    public string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    // 이력 검증용 커밋은 이 폐기 가능한 임시 저장소에서만 만들고 Dispose에서 모두 제거한다.
    public string CommitFixture(string message)
    {
        Git("add", ".");
        Git("-c", "user.name=RepositoryChecksTests", "-c", "user.email=tests@localhost", "-c", "commit.gpgsign=false",
            "-c", "core.hooksPath=" + Path.Combine(Root, "unused-hooks"), "commit", "--quiet", "-m", message);
        return Git("rev-parse", "HEAD").Trim();
    }

    public string Git(params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in args) { start.ArgumentList.Add(argument); }
        start.Environment.Remove("GIT_DIR");
        start.Environment.Remove("GIT_WORK_TREE");
        start.Environment.Remove("GIT_INDEX_FILE");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) { throw new InvalidOperationException(error); }
        return output;
    }

    public void Dispose()
    {
        var absoluteRoot = Path.GetFullPath(Root);
        if (!absoluteRoot.StartsWith(Path.GetFullPath(TestBase) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Test directory escaped its owner.");
        }
        if (Directory.Exists(absoluteRoot))
        {
            foreach (var file in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
            Directory.Delete(absoluteRoot, true);
        }
    }
}
