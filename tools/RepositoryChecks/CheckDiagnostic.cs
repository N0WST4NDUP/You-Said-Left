namespace RepositoryChecks;

public sealed record CheckDiagnostic(string Id, string Message, string Path, int Line = 1, int Column = 1)
{
    public override string ToString() => $"{Path}({Line},{Column}): error {Id}: {Message}";
}
