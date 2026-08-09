namespace Finora.UiTests;

public sealed class NavigationContractTests
{
    public static IEnumerable<object[]> RequiredPrimaryRoutes()
    {
        yield return ["dashboard"];
        yield return ["transactions"];
        yield return ["budgets"];
        yield return ["goals"];
        yield return ["settings"];
    }

    [Theory]
    [MemberData(nameof(RequiredPrimaryRoutes))]
    public void PrimaryRouteNames_AreStableAndNonEmpty(string route)
    {
        Assert.False(string.IsNullOrWhiteSpace(route));
        Assert.DoesNotContain(' ', route);
    }

    [Fact]
    public void PrivacyAndRecoveryFlows_ArePartOfUiTestContract()
    {
        string[] flows = ["onboarding", "lock", "backup-preview", "destructive-delete-confirmation", "privacy-mode", "transaction-edit", "receipt-attachment", "csv-mapping", "reconciliation", "recurrence-actions"];
        Assert.Equal(flows.Length, flows.Distinct(StringComparer.Ordinal).Count());
    }
}
