using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.Infrastructure;

public sealed class SampleDataService(
    IFinanceDataResetService resetService,
    IFinanceStore store,
    DatabaseInitializer initializer) : ISampleDataService
{
    public async Task<Result<SampleDataResetResult>> ResetToSyntheticSampleDataAsync(
        string currency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DomainRules.ValidateCurrency(currency);
            currency = currency.Trim().ToUpperInvariant();

            var reset = await resetService.DeleteAllFinanceDataAsync(cancellationToken).ConfigureAwait(false);
            if (!reset.IsSuccess)
                return Result<SampleDataResetResult>.Failure(reset.Error ?? "Finora could not clear the existing finance data safely.");

            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);

            var bank = new Account
            {
                Name = "Sample bank",
                Type = AccountType.Bank,
                Currency = currency,
                OpeningBalanceMinor = Money.FromMajorUnits(50_000m, currency).MinorUnits,
                Icon = "bank"
            };
            var wallet = new Account
            {
                Name = "Sample wallet",
                Type = AccountType.DigitalWallet,
                Currency = currency,
                OpeningBalanceMinor = Money.FromMajorUnits(5_000m, currency).MinorUnits,
                Icon = "wallet"
            };
            await store.SaveAccountAsync(bank, cancellationToken).ConfigureAwait(false);
            await store.SaveAccountAsync(wallet, cancellationToken).ConfigureAwait(false);

            var categories = await store.GetCategoriesAsync(cancellationToken).ConfigureAwait(false);
            Guid? Category(string name) => categories.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;

            var now = DateTimeOffset.UtcNow;
            var sampleTransactions = new[]
            {
                TransactionFactory.Create(TransactionType.Income, Money.FromMajorUnits(75_000m, currency).MinorUnits, currency, bank.Id, now.AddDays(-18), Category("Salary"), "Sample employer", "Synthetic sample data"),
                TransactionFactory.Create(TransactionType.Expense, Money.FromMajorUnits(18_000m, currency).MinorUnits, currency, bank.Id, now.AddDays(-14), Category("Housing"), "Sample landlord", "Synthetic sample data"),
                TransactionFactory.Create(TransactionType.Expense, Money.FromMajorUnits(1_250m, currency).MinorUnits, currency, wallet.Id, now.AddDays(-5), Category("Food"), "Sample grocery", "Synthetic sample data"),
                TransactionFactory.Create(TransactionType.Expense, Money.FromMajorUnits(650m, currency).MinorUnits, currency, wallet.Id, now.AddDays(-2), Category("Transport"), "Sample transit", "Synthetic sample data")
            };
            foreach (var transaction in sampleTransactions)
                await store.SaveTransactionAsync(transaction, cancellationToken).ConfigureAwait(false);

            await store.RecordTransferAsync(
                bank.Id,
                wallet.Id,
                Money.FromMajorUnits(2_000m, currency).MinorUnits,
                now.AddDays(-7),
                "Synthetic sample transfer",
                cancellationToken).ConfigureAwait(false);

            await store.SaveBudgetAsync(new Budget
            {
                Name = "Sample monthly food budget",
                Kind = BudgetKind.Category,
                Cadence = BudgetCadence.Monthly,
                CategoryId = Category("Food"),
                LimitMinor = Money.FromMajorUnits(10_000m, currency).MinorUnits,
                Currency = currency,
                WarningThresholdPercent = 80,
                RolloverEnabled = false
            }, cancellationToken).ConfigureAwait(false);

            await store.SaveSavingsGoalAsync(new SavingsGoal
            {
                Name = "Sample travel goal",
                TargetMinor = Money.FromMajorUnits(100_000m, currency).MinorUnits,
                StartingMinor = Money.FromMajorUnits(15_000m, currency).MinorUnits,
                Currency = currency,
                TargetDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(10)),
                Notes = "Synthetic sample goal",
                Icon = "target"
            }, cancellationToken).ConfigureAwait(false);

            var today = DateOnly.FromDateTime(DateTime.Today);
            await store.SaveRecurrenceRuleAsync(new RecurrenceRule
            {
                Name = "Sample monthly rent",
                Frequency = RecurrenceFrequency.Monthly,
                Interval = 1,
                DayOfMonth = Math.Min(5, DateTime.DaysInMonth(today.Year, today.Month)),
                StartsOn = today,
                NextDueOn = today,
                TransactionType = TransactionType.Expense,
                AmountMinor = Money.FromMajorUnits(18_000m, currency).MinorUnits,
                Currency = currency,
                AccountId = bank.Id,
                CategoryId = Category("Housing"),
                Merchant = "Sample landlord",
                Note = "Synthetic sample recurring item",
                ReminderMinutesBefore = 24 * 60
            }, cancellationToken).ConfigureAwait(false);

            return Result<SampleDataResetResult>.Success(new SampleDataResetResult(
                AccountsCreated: 2,
                TransactionsCreated: sampleTransactions.Length + 2,
                BudgetsCreated: 1,
                GoalsCreated: 1,
                RecurrenceRulesCreated: 1));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return Result<SampleDataResetResult>.Failure("Finora could not create the synthetic sample dataset safely.");
        }
    }
}
