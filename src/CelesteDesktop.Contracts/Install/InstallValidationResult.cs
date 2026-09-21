using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Install;

public sealed class InstallValidationResult
{
    private InstallValidationResult(
        bool isValid,
        string? canonicalRoot,
        IReadOnlyList<InstallValidationIssue> issues)
    {
        IsValid = isValid;
        CanonicalRoot = canonicalRoot;
        Issues = issues;
    }

    public bool IsValid { get; }

    public string? CanonicalRoot { get; }

    public IReadOnlyList<InstallValidationIssue> Issues { get; }

    public static InstallValidationResult Valid(string canonicalRoot) =>
        new(true, canonicalRoot, Array.Empty<InstallValidationIssue>());

    public static InstallValidationResult Invalid(IEnumerable<InstallValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var copy = issues.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("An invalid result requires at least one issue.", nameof(issues));
        }

        return new InstallValidationResult(
            false,
            null,
            new ReadOnlyCollection<InstallValidationIssue>(copy));
    }
}
