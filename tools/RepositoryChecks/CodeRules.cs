using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RepositoryChecks;

public static class CodeRules
{
    internal const int MaximumConditionalSymbols = 8;

    public static IReadOnlyList<CheckDiagnostic> Analyze(string source, string path)
    {
        var text = SourceText.From(source);
        var options = new CSharpParseOptions(LanguageVersion.CSharp9);
        var initialTree = CSharpSyntaxTree.ParseText(text, options, path);
        var symbols = GetConditionalSymbols(initialTree);
        var diagnostics = new List<CheckDiagnostic>();

        if (symbols.Length > MaximumConditionalSymbols)
        {
            diagnostics.Add(new CheckDiagnostic("CODE008",
                $"조건부 컴파일 심볼이 {MaximumConditionalSymbols}개를 초과해 모든 분기를 검사할 수 없습니다.", path));
            return diagnostics;
        }

        List<TextSpan>? alwaysDisabled = null;
        for (var assignment = 0; assignment < 1 << symbols.Length; assignment++)
        {
            var definedSymbols = symbols.Where((_, index) => (assignment & (1 << index)) != 0);
            var tree = assignment == 0
                ? initialTree
                : CSharpSyntaxTree.ParseText(text, options.WithPreprocessorSymbols(definedSymbols), path);
            AnalyzeTree(tree, path, diagnostics);

            var disabled = tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
                .Where(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                .Select(trivia => trivia.Span).ToList();
            alwaysDisabled = alwaysDisabled is null ? disabled : Intersect(alwaysDisabled, disabled);
        }

        foreach (var span in alwaysDisabled ?? [])
        {
            if (ContainsCode(text.ToString(span)))
            {
                Add(diagnostics, "CODE008", "어떤 심볼 조합에서도 활성화되지 않는 조건부 코드가 있어 검사하지 못했습니다.",
                    path, Location.Create(initialTree, span));
            }
        }

        return diagnostics.Distinct().OrderBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Column).ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal).ToArray();
    }

    internal static string[] GetConditionalSymbols(string source) =>
        GetConditionalSymbols(CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp9)));

    private static string[] GetConditionalSymbols(SyntaxTree tree)
    {
        var conditions = tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
            .Select(trivia => trivia.GetStructure())
            .Select(structure => structure switch
            {
                IfDirectiveTriviaSyntax directive => directive.Condition,
                ElifDirectiveTriviaSyntax directive => directive.Condition,
                _ => null
            })
            .OfType<ExpressionSyntax>();
        return conditions.SelectMany(condition => condition.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>().Select(identifier => identifier.Identifier.ValueText))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private static void AnalyzeTree(SyntaxTree tree, string path, List<CheckDiagnostic> diagnostics)
    {
        var syntaxErrors = tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (syntaxErrors.Length > 0)
        {
            foreach (var error in syntaxErrors)
            {
                Add(diagnostics, "CODE001", $"C# 9 구문 오류 {error.Id}: {error.GetMessage()}", path, error.Location);
            }
            return;
        }

        var root = tree.GetRoot();
        foreach (var declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            var name = string.Join(".", declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse().Append(declaration).Select(item => string.Concat(item.Name.DescendantTokens()
                    .Select(token => token.ValueText))));
            if (declaration is not NamespaceDeclarationSyntax ||
                (name != "YouSaidLeft" && !name.StartsWith("YouSaidLeft.", StringComparison.Ordinal)))
            {
                Add(diagnostics, "CODE002", "YouSaidLeft 또는 YouSaidLeft.*의 중괄호 네임스페이스를 사용하세요.",
                    path, declaration.GetLocation());
            }
        }

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case BaseTypeDeclarationSyntax declaration:
                    CheckNamespace(declaration, path, diagnostics);
                    CheckName(declaration.Identifier, declaration is InterfaceDeclarationSyntax ? "인터페이스" : "타입",
                        declaration is InterfaceDeclarationSyntax
                            ? IsInterfaceName(declaration.Identifier.ValueText)
                            : IsPascalCase(declaration.Identifier.ValueText), path, diagnostics);
                    break;
                case DelegateDeclarationSyntax declaration:
                    CheckNamespace(declaration, path, diagnostics);
                    CheckPascalName(declaration.Identifier, "델리게이트", path, diagnostics);
                    break;
                case MethodDeclarationSyntax declaration when declaration.ExplicitInterfaceSpecifier is null &&
                                                              !declaration.Modifiers.Any(SyntaxKind.OverrideKeyword):
                    CheckPascalName(declaration.Identifier, "메서드", path, diagnostics);
                    break;
                case LocalFunctionStatementSyntax declaration:
                    CheckPascalName(declaration.Identifier, "지역 함수", path, diagnostics);
                    break;
                case PropertyDeclarationSyntax declaration when declaration.ExplicitInterfaceSpecifier is null &&
                                                                !declaration.Modifiers.Any(SyntaxKind.OverrideKeyword):
                    CheckPascalName(declaration.Identifier, "프로퍼티", path, diagnostics);
                    break;
                case EventDeclarationSyntax declaration when declaration.ExplicitInterfaceSpecifier is null &&
                                                             !declaration.Modifiers.Any(SyntaxKind.OverrideKeyword):
                    CheckPascalName(declaration.Identifier, "이벤트", path, diagnostics);
                    break;
                case EnumMemberDeclarationSyntax declaration:
                    CheckPascalName(declaration.Identifier, "enum 멤버", path, diagnostics);
                    break;
                case VariableDeclaratorSyntax declaration:
                    CheckVariable(declaration, path, diagnostics);
                    break;
                case ParameterSyntax parameter when parameter.Identifier.ValueText != "_":
                    CheckCamelName(parameter.Identifier, "매개변수", path, diagnostics);
                    break;
                case SingleVariableDesignationSyntax variable:
                    CheckCamelName(variable.Identifier, "지역 변수", path, diagnostics);
                    break;
                case ForEachStatementSyntax statement:
                    CheckCamelName(statement.Identifier, "지역 변수", path, diagnostics);
                    break;
                case CatchDeclarationSyntax declaration when !declaration.Identifier.IsKind(SyntaxKind.None):
                    CheckCamelName(declaration.Identifier, "지역 변수", path, diagnostics);
                    break;
            }

            if (node is MemberDeclarationSyntax member)
            {
                CheckAccessibility(member, path, diagnostics);
            }

            var body = node switch
            {
                IfStatementSyntax statement => statement.Statement,
                ElseClauseSyntax clause when clause.Statement is not IfStatementSyntax => clause.Statement,
                ForStatementSyntax statement => statement.Statement,
                CommonForEachStatementSyntax statement => statement.Statement,
                WhileStatementSyntax statement => statement.Statement,
                DoStatementSyntax statement => statement.Statement,
                _ => null
            };
            if (body is not null and not BlockSyntax)
            {
                Add(diagnostics, "CODE006", "조건문과 반복문의 본문에 중괄호를 사용하세요.", path, body.GetLocation());
            }
        }

        var topLevelTypes = root.DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(declaration => declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax &&
                                  declaration.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax).ToArray();
        if (topLevelTypes.Length == 1 && topLevelTypes[0] is BaseTypeDeclarationSyntax primaryType &&
            !primaryType.Modifiers.Any(SyntaxKind.PartialKeyword) &&
            primaryType.Identifier.ValueText != Path.GetFileNameWithoutExtension(path))
        {
            Add(diagnostics, "CODE007", "최상위 타입이 하나인 파일은 파일명과 타입 이름을 일치시키세요.",
                path, primaryType.Identifier.GetLocation());
        }
    }

    private static void CheckNamespace(SyntaxNode declaration, string path, List<CheckDiagnostic> diagnostics)
    {
        if (!declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any())
        {
            Add(diagnostics, "CODE002", "게임 타입을 YouSaidLeft 네임스페이스 안에 선언하세요.", path, declaration.GetLocation());
        }
    }

    private static void CheckVariable(VariableDeclaratorSyntax declaration, string path, List<CheckDiagnostic> diagnostics)
    {
        var container = declaration.Parent?.Parent;
        var name = declaration.Identifier.ValueText;
        if (container is FieldDeclarationSyntax field)
        {
            if (field.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                CheckConstantName(declaration.Identifier, path, diagnostics);
            }
            else if (field.Modifiers.Any(SyntaxKind.PrivateKeyword) &&
                     (name.Length < 2 || name[0] != '_' || !IsCamelCase(name[1..])))
            {
                Add(diagnostics, "CODE004", "private 필드 이름에 _camelCase를 사용하세요.", path, declaration.Identifier.GetLocation());
            }
        }
        else if (container is EventFieldDeclarationSyntax eventField)
        {
            if (!eventField.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                CheckPascalName(declaration.Identifier, "이벤트", path, diagnostics);
            }
        }
        else if (container is LocalDeclarationStatementSyntax local && local.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            CheckConstantName(declaration.Identifier, path, diagnostics);
        }
        else
        {
            CheckCamelName(declaration.Identifier, "지역 변수", path, diagnostics);
        }
    }

    private static void CheckAccessibility(MemberDeclarationSyntax member, string path, List<CheckDiagnostic> diagnostics)
    {
        if (member.Parent is InterfaceDeclarationSyntax ||
            member is ConstructorDeclarationSyntax constructor && constructor.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            member is MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null } ||
            member is PropertyDeclarationSyntax { ExplicitInterfaceSpecifier: not null } ||
            member is IndexerDeclarationSyntax { ExplicitInterfaceSpecifier: not null } ||
            member is EventDeclarationSyntax { ExplicitInterfaceSpecifier: not null })
        {
            return;
        }

        SyntaxTokenList? modifiers = member switch
        {
            BaseTypeDeclarationSyntax declaration => declaration.Modifiers,
            DelegateDeclarationSyntax declaration => declaration.Modifiers,
            BaseFieldDeclarationSyntax declaration => declaration.Modifiers,
            MethodDeclarationSyntax declaration => declaration.Modifiers,
            PropertyDeclarationSyntax declaration => declaration.Modifiers,
            IndexerDeclarationSyntax declaration => declaration.Modifiers,
            EventDeclarationSyntax declaration => declaration.Modifiers,
            ConstructorDeclarationSyntax declaration => declaration.Modifiers,
            OperatorDeclarationSyntax declaration => declaration.Modifiers,
            ConversionOperatorDeclarationSyntax declaration => declaration.Modifiers,
            _ => null
        };
        if (modifiers is not null && !modifiers.Value.Any(token => token.IsKind(SyntaxKind.PublicKeyword) ||
            token.IsKind(SyntaxKind.PrivateKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword) || token.IsKind(SyntaxKind.InternalKeyword)))
        {
            Add(diagnostics, "CODE005", "선언에 접근 제한자를 명시하세요.", path, member.GetLocation());
        }
    }

    private static void CheckPascalName(SyntaxToken identifier, string kind, string path, List<CheckDiagnostic> diagnostics)
    {
        CheckName(identifier, kind, IsPascalCase(identifier.ValueText), path, diagnostics);
    }

    private static void CheckCamelName(SyntaxToken identifier, string kind, string path, List<CheckDiagnostic> diagnostics)
    {
        if (!IsCamelCase(identifier.ValueText))
        {
            Add(diagnostics, "CODE003", $"{kind} 이름에 영어 camelCase를 사용하세요.", path, identifier.GetLocation());
        }
    }

    private static void CheckName(SyntaxToken identifier, string kind, bool valid, string path, List<CheckDiagnostic> diagnostics)
    {
        if (!valid)
        {
            var expected = kind == "인터페이스" ? "I 접두사와 영어 PascalCase" : "영어 PascalCase";
            Add(diagnostics, "CODE003", $"{kind} 이름에 {expected}를 사용하세요.", path, identifier.GetLocation());
        }
    }

    private static void CheckConstantName(SyntaxToken identifier, string path, List<CheckDiagnostic> diagnostics)
    {
        if (!IsPascalCase(identifier.ValueText))
        {
            Add(diagnostics, "CODE004", "상수 이름에 영어 PascalCase를 사용하세요.", path, identifier.GetLocation());
        }
    }

    private static bool IsPascalCase(string name) => name.Length > 0 && name[0] is >= 'A' and <= 'Z' && HasOnlyAsciiLettersAndDigits(name);

    private static bool IsCamelCase(string name) => name.Length > 0 && name[0] is >= 'a' and <= 'z' && HasOnlyAsciiLettersAndDigits(name);

    private static bool IsInterfaceName(string name) => name.Length > 1 && name[0] == 'I' && IsPascalCase(name[1..]);

    private static bool HasOnlyAsciiLettersAndDigits(string name) => name.All(character =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9');

    private static bool ContainsCode(string source) => CSharpSyntaxTree.ParseText(source)
        .GetRoot().DescendantTokens().Any(token => !token.IsKind(SyntaxKind.EndOfFileToken));

    private static List<TextSpan> Intersect(List<TextSpan> left, List<TextSpan> right)
    {
        var intersections = new List<TextSpan>();
        foreach (var first in left)
        {
            foreach (var second in right)
            {
                var intersection = first.Intersection(second);
                if (intersection is { Length: > 0 })
                {
                    intersections.Add(intersection.Value);
                }
            }
        }
        return intersections;
    }

    private static void Add(List<CheckDiagnostic> diagnostics, string id, string message, string path, Location location)
    {
        var position = location.GetLineSpan().StartLinePosition;
        diagnostics.Add(new CheckDiagnostic(id, message, path, position.Line + 1, position.Character + 1));
    }
}
