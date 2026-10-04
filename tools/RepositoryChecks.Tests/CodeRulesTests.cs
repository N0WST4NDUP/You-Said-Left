using Xunit;

namespace RepositoryChecks.Tests;

public sealed class CodeRulesTests
{
    [Fact]
    public void AcceptsOrdinaryGameDeclarations()
    {
        var source = """
            namespace YouSaidLeft.Driving
            {
                public class Vehicle
                {
                    private int _speed;
                    private static int _vehicleCount;
                    private const int MaxSpeed = 100;
                    public int Speed { get; private set; }
                    public event System.Action Changed;

                    public Vehicle(int initialSpeed)
                    {
                        _speed = initialSpeed;
                    }

                    public void Drive(int acceleration)
                    {
                        const int WheelCount = 4;
                        var elapsedSeconds = 0;
                        if (acceleration > 0)
                        {
                            _speed += acceleration;
                        }
                        else if (acceleration == 0)
                        {
                            return;
                        }
                        else
                        {
                            _speed = elapsedSeconds;
                        }
                    }
                }
            }
            """;

        Assert.Empty(Analyze(source));
    }

    [Theory]
    [InlineData("public class Vehicle { }")]
    [InlineData("namespace Other { public class Vehicle { } }")]
    [InlineData("namespace YouSaidLeftExtra { public class Vehicle { } }")]
    public void RejectsMissingOrUnrelatedNamespaces(string source)
    {
        AssertRule(source, "CODE002");
    }

    [Fact]
    public void AcceptsNestedNamespaceUnderProjectRoot()
    {
        Assert.Empty(Analyze("namespace YouSaidLeft { namespace Driving { public class Vehicle { } } }"));
    }

    [Theory]
    [InlineData("public void drive() { }")]
    [InlineData("public int speed { get; set; }")]
    [InlineData("public event System.Action changed;")]
    [InlineData("public event System.Action changed { add { } remove { } }")]
    [InlineData("public class wheel { }")]
    [InlineData("public interface VehicleControls { }")]
    [InlineData("public interface Ivehicle { }")]
    [InlineData("public delegate void changed();")]
    [InlineData("public enum Direction { left, Right }")]
    [InlineData("public void Drive(int Speed) { }")]
    [InlineData("public void Drive() { int Speed = 0; }")]
    [InlineData("public void Drive() { foreach (var Wheel in wheels) { } }")]
    [InlineData("public void Drive() { if (value is int Speed) { } }")]
    [InlineData("public void Drive() { Try(out var Result); }")]
    [InlineData("public void Drive() { var (Speed, acceleration) = (1, 2); }")]
    [InlineData("public void Drive() { try { } catch (System.Exception Error) { } }")]
    [InlineData("public void Drive() { void move() { } }")]
    public void RejectsIncorrectDeclarationNames(string member)
    {
        AssertRule(Wrap(member), "CODE003");
    }

    [Theory]
    [InlineData("private int speed;")]
    [InlineData("private static int Speed;")]
    [InlineData("private const int _maxSpeed = 100;")]
    [InlineData("public const int maxSpeed = 100;")]
    [InlineData("private int _speed, acceleration;")]
    [InlineData("public void Drive() { const int maxSpeed = 100; }")]
    public void RejectsIncorrectFieldOrConstantNames(string member)
    {
        AssertRule(Wrap(member), "CODE004");
    }

    [Theory]
    [InlineData("int _speed;")]
    [InlineData("void Drive() { }")]
    [InlineData("Vehicle() { }")]
    [InlineData("class Wheels { }")]
    [InlineData("event System.Action Changed;")]
    public void RejectsImplicitAccessibilityWhereAChoiceIsRequired(string member)
    {
        AssertRule(Wrap(member), "CODE005");
    }

    [Fact]
    public void AcceptsLanguageAndExternalContractExceptions()
    {
        var source = """
            namespace YouSaidLeft
            {
                public interface IVehicle
                {
                    void Drive(int speed);
                    int Speed { get; }
                    event System.Action Changed;
                }

                public class Vehicle
                {
                    static Vehicle() { }
                    public override void drive() { }
                    public override int speed { get; }
                    void ILegacy.drive() { }
                    int ILegacy.speed { get; }
                    event System.Action ILegacy.changed { add { } remove { } }
                    public void Drive()
                    {
                        System.Action<int> action = _ => { };
                        var (_, speed) = (1, 2);
                    }
                }
            }
            """;

        Assert.Empty(Analyze(source));
    }

    [Theory]
    [InlineData("if (ready) return;")]
    [InlineData("if (ready) { } else return;")]
    [InlineData("for (var i = 0; i < 1; i++) continue;")]
    [InlineData("foreach (var wheel in wheels) continue;")]
    [InlineData("while (ready) break;")]
    [InlineData("do break; while (ready);")]
    public void RequiresBracesOnConditionalsAndLoops(string statement)
    {
        AssertRule(Wrap($"public void Drive() {{ {statement} }}"), "CODE006");
    }

    [Fact]
    public void ChecksFilenameForASingleNonPartialType()
    {
        AssertRule("namespace YouSaidLeft { public class Bicycle { } }", "CODE007");
    }

    [Theory]
    [InlineData("namespace YouSaidLeft { public partial class Bicycle { } }")]
    [InlineData("namespace YouSaidLeft { public class Bicycle { } public class Wheel { } }")]
    [InlineData("namespace YouSaidLeft { public class Bicycle { } public delegate void Callback(); }")]
    public void DoesNotGuessPrimaryTypeForPartialOrMultipleTypes(string source)
    {
        Assert.Empty(Analyze(source));
    }

    [Fact]
    public void ReportsMalformedSyntax()
    {
        AssertRule("namespace YouSaidLeft { public class Vehicle {", "CODE001");
    }

    [Fact]
    public void RejectsFileScopedNamespaces()
    {
        AssertRule("namespace YouSaidLeft; public class Vehicle { }", "CODE002");
    }

    [Fact]
    public void ReportsActionableSourceLocation()
    {
        var source = "namespace YouSaidLeft\n{\n    public class Vehicle\n    {\n        private int speed;\n    }\n}\n";
        var diagnostic = Assert.Single(Analyze(source), item => item.Id == "CODE004");

        Assert.Equal("Assets/YouSaidLeft/Scripts/Vehicle.cs", diagnostic.Path);
        Assert.Equal(5, diagnostic.Line);
        Assert.Equal(21, diagnostic.Column);
    }

    [Fact]
    public void AcceptsValidUnityConditionalFieldsAcrossEveryBranch()
    {
        var source = Wrap("""
            #if UNITY_EDITOR
            private int _editorSpeed;
            #elif UNITY_STANDALONE
            private int _desktopSpeed;
            #else
            private int _mobileSpeed;
            #endif
            """);

        Assert.Empty(Analyze(source));
    }

    [Fact]
    public void DoesNotInventMissingTypesWhenAWholeFileIsConditional()
    {
        var source = "#if UNITY_EDITOR\nnamespace YouSaidLeft { public class Vehicle { } }\n#endif\n";

        Assert.Empty(Analyze(source));
    }

    [Fact]
    public void DoesNotTreatDisabledCommentsAsUncheckedCode()
    {
        Assert.Empty(Analyze(Wrap("#if false\n// Explanation only.\n/* No declaration. */\n#endif")));
    }

    [Fact]
    public void FindsViolationsInNestedConditionalBranches()
    {
        var source = Wrap("#if UNITY_EDITOR\n#if DEBUG\nprivate int speed;\n#endif\n#endif");

        AssertRule(source, "CODE004");
        Assert.DoesNotContain(Analyze(source), item => item.Id == "CODE008");
    }

    [Fact]
    public void FindsViolationsInBothConditionalBranches()
    {
        var source = Wrap("""
            #if UNITY_EDITOR
            private int editorSpeed;
            #else
            private int playerSpeed;
            #endif
            """);

        var diagnostics = Analyze(source);
        Assert.Equal(2, diagnostics.Count(item => item.Id == "CODE004"));
        Assert.DoesNotContain(diagnostics, item => item.Id == "CODE008");
    }

    [Fact]
    public void ChecksBooleanConditionalExpressionsAndDeduplicatesSharedViolations()
    {
        var source = Wrap("""
            private int sharedSpeed;
            #if EDITOR && !SERVER || CLIENT
            private int specialSpeed;
            #else
            private int _ordinarySpeed;
            #endif
            """);

        Assert.Equal(2, Analyze(source).Count(item => item.Id == "CODE004"));
    }

    [Theory]
    [InlineData("#if false\nprivate int uncheckedSpeed;\n#endif")]
    [InlineData("#if A || B || C || D || E || F || G || H || I\nprivate int _speed;\n#endif")]
    public void ReportsConditionalCodeItCannotFullyInspect(string member)
    {
        AssertRule(Wrap(member), "CODE008");
    }

    private static IReadOnlyList<CheckDiagnostic> Analyze(string source)
    {
        return CodeRules.Analyze(source, "Assets/YouSaidLeft/Scripts/Vehicle.cs");
    }

    private static void AssertRule(string source, string id)
    {
        Assert.Contains(Analyze(source), item => item.Id == id);
    }

    private static string Wrap(string member)
    {
        return $"namespace YouSaidLeft\n{{\n    public class Vehicle\n    {{\n{member}\n    }}\n}}\n";
    }
}
