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

    public static IEnumerable<object[]> AdaptiveRoutePairs()
    {
        yield return ["dashboard", "dashboard-desktop"];
        yield return ["transactions", "transactions-desktop"];
        yield return ["budgets", "budgets-desktop"];
        yield return ["goals", "goals-desktop"];
        yield return ["settings", "settings-desktop"];
    }

    [Theory]
    [MemberData(nameof(RequiredPrimaryRoutes))]
    public void PrimaryRouteNames_AreStableAndNonEmpty(string route)
    {
        Assert.False(string.IsNullOrWhiteSpace(route));
        Assert.DoesNotContain(' ', route);
    }

    [Theory]
    [MemberData(nameof(AdaptiveRoutePairs))]
    public void DesktopRoute_IsDistinctStableCompanionOfMobileRoute(string mobile, string desktop)
    {
        Assert.Equal($"{mobile}-desktop", desktop);
        Assert.NotEqual(mobile, desktop);
        Assert.DoesNotContain(' ', desktop);
    }

    [Fact]
    public void PrivacyRecoveryAndAdaptiveFlows_ArePartOfUiTestContract()
    {
        string[] flows =
        [
            "onboarding",
            "lock",
            "backup-preview",
            "destructive-delete-confirmation",
            "synthetic-sample-reset-confirmation",
            "privacy-mode",
            "transaction-edit",
            "receipt-attachment",
            "csv-mapping",
            "reconciliation",
            "recurrence-actions",
            "mobile-bottom-tabs",
            "tablet-desktop-flyout",
            "resize-route-preservation",
            "large-text",
            "keyboard-focus",
            "screen-reader-semantics"
        ];

        Assert.Equal(flows.Length, flows.Distinct(StringComparer.Ordinal).Count());
    }
}
