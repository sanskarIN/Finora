namespace Finora.Application;

public enum IntegritySeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public sealed record IntegrityIssue(
    string Code,
    IntegritySeverity Severity,
    string Message,
    int AffectedRecords = 1);

public sealed record IntegrityReport(
    DateTimeOffset CheckedAtUtc,
    bool DatabaseIntegrityPassed,
    bool ForeignKeysPassed,
    int AccountsChecked,
    int TransactionsChecked,
    int AttachmentsChecked,
    int RecurrenceOccurrencesChecked,
    IReadOnlyList<IntegrityIssue> Issues)
{
    public bool IsHealthy => DatabaseIntegrityPassed && ForeignKeysPassed && Issues.All(x => x.Severity != IntegritySeverity.Error);

    public string ToSanitizedText()
    {
        var lines = new List<string>
        {
            "Finora local data integrity report",
            $"CheckedAtUtc: {CheckedAtUtc:O}",
            $"Healthy: {IsHealthy}",
            $"SQLiteIntegrity: {DatabaseIntegrityPassed}",
            $"ForeignKeys: {ForeignKeysPassed}",
            $"AccountsChecked: {AccountsChecked}",
            $"TransactionsChecked: {TransactionsChecked}",
            $"AttachmentsChecked: {AttachmentsChecked}",
            $"RecurrenceOccurrencesChecked: {RecurrenceOccurrencesChecked}",
            $"IssueCount: {Issues.Count}"
        };

        foreach (var issue in Issues)
            lines.Add($"{issue.Severity}: {issue.Code} ({issue.AffectedRecords}) - {issue.Message}");

        lines.Add("No account names, merchant/payee names, notes, amounts, attachment names, or transaction contents are included in this report.");
        return string.Join(Environment.NewLine, lines);
    }
}

public interface IDataIntegrityService
{
    Task<IntegrityReport> CheckAsync(CancellationToken cancellationToken = default);
}
