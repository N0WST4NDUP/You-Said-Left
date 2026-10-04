using Xunit;

namespace RepositoryChecks.Tests;

public sealed class CommandTests
{
    [Fact]
    public void LintChecksFormattingInsideConditionalCompilationWithoutChangingSource()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\nindent_size = 4\ncsharp_new_line_before_open_brace = all\ncsharp_preserve_single_line_blocks = false\n");
        const string source = "#if UNITY_EDITOR\nnamespace YouSaidLeft { public class Vehicle { private int _speed; } }\n#endif\n";
        repository.Write("Assets/YouSaidLeft/Vehicle.cs", source);
        var originalError = Console.Error;
        using var error = new StringWriter();
        try
        {
            Console.SetError(error);
            Assert.Equal(1, Program.Main(["lint", "--all", "--root", repository.Root]));
        }
        finally { Console.SetError(originalError); }
        Assert.Contains(Path.Combine(repository.Root, "Assets", "YouSaidLeft", "Vehicle.cs") + "(2,", error.ToString());
        Assert.DoesNotContain(".utmp", error.ToString());
        Assert.Equal(source, repository.Read("Assets/YouSaidLeft/Vehicle.cs"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConditionalFormattingPreservesNestedFileSpecificConfiguration(bool useBom)
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\nindent_size = 4\ncsharp_new_line_before_open_brace = all\ncsharp_preserve_single_line_blocks = false\n");
        repository.Write("Assets/YouSaidLeft/Runtime/.editorconfig", "[Vehicle.cs]\nindent_size = 2\ncharset = " + (useBom ? "utf-8-bom" : "utf-8") + "\n");
        string source = (useBom ? "\uFEFF" : "") + "#if UNITY_EDITOR && UNITY_STANDALONE\nnamespace YouSaidLeft\n{\n  public class Vehicle\n  {\n    private int _speed;\n  }\n}\n#endif\n";
        repository.Write("Assets/YouSaidLeft/Runtime/Vehicle.cs", source);
        var path = Path.Combine(repository.Root, "Assets", "YouSaidLeft", "Runtime", "Vehicle.cs");
        var originalBytes = File.ReadAllBytes(path);
        Assert.Equal(0, Program.Main(["lint", "--all", "--root", repository.Root]));
        Assert.Equal(originalBytes, File.ReadAllBytes(path));
    }

    [Fact]
    public void ConditionalFormattingHandlesManyVariantsWithinSupportedSymbolLimit()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\nindent_size = 4\ncsharp_new_line_before_open_brace = all\ncsharp_preserve_single_line_blocks = false\n");
        for (var index = 0; index < 4; index++)
        {
            repository.Write($"Assets/YouSaidLeft/Vehicle{index}.cs",
                $"#if FLAG1 || FLAG2 || FLAG3 || FLAG4 || FLAG5 || FLAG6 || FLAG7 || FLAG8\nnamespace YouSaidLeft\n{{\n    public class Vehicle{index}\n    {{\n        private int _speed;\n    }}\n}}\n#endif\n");
        }
        Assert.Equal(0, Program.Main(["lint", "--all", "--root", repository.Root]));
    }

    [Fact]
    public void HelpSucceedsWithoutRepository()
    {
        Assert.Equal(0, Program.Main(["--help"]));
    }

    [Fact]
    public void CommitFileUsesMessageRulesAndReportsViolations()
    {
        using var repository = new TestRepository();
        repository.Write("message.txt", "Feat(core): 가속 입력 추가\n");
        Assert.Equal(0, Program.Main(["commit", "--file", Path.Combine(repository.Root, "message.txt"), "--root", repository.Root]));
        repository.Write("message.txt", "feat(core): 가속 입력 추가\n");
        Assert.Equal(1, Program.Main(["commit", "--file", Path.Combine(repository.Root, "message.txt"), "--root", repository.Root]));
    }

    [Fact]
    public void WorkingLintFailsForWhitespaceWithoutChangingSource()
    {
        using var repository = new TestRepository();
        repository.Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\nindent_size = 4\ncsharp_new_line_before_open_brace = all\ncsharp_preserve_single_line_blocks = false\ncsharp_preserve_single_line_statements = false\n");
        const string source = "namespace YouSaidLeft { public class Vehicle { } }\n";
        repository.Write("Assets/YouSaidLeft/Vehicle.cs", source);

        Assert.Equal(1, Program.Main(["lint", "--all", "--root", repository.Root]));
        Assert.Equal(source, repository.Read("Assets/YouSaidLeft/Vehicle.cs"));
    }

    [Fact]
    public void EmptyOwnedCodeScopeSucceeds()
    {
        using var repository = new TestRepository();
        repository.Write("Assets/TutorialInfo/Template.cs", "invalid template source\n");
        Assert.Equal(0, Program.Main(["lint", "--all", "--root", repository.Root]));
    }

    [Theory]
    [InlineData("lint", "--all", "--staged")]
    [InlineData("lint", "--all", "--unknown")]
    [InlineData("commit", "--file")]
    [InlineData("commit", "--range", "HEAD")]
    public void InvalidArgumentsAreExecutionErrors(params string[] arguments)
    {
        Assert.Equal(2, Program.Main(arguments));
    }
}
