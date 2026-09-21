namespace CelesteDesktop.Contracts.Install;

public sealed record InstallValidationIssue(
    string Code,
    string Stage,
    string? RelativePath = null);
