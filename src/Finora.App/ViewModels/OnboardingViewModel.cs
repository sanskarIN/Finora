using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class OnboardingViewModel : ViewModelBase
{
    private readonly IFinanceStore _store; private readonly IAppSettingsService _settings; private string _currency; private string _locale; private int _monthStart; private string _openingBalance = "0"; private bool _sampleData;
    public OnboardingViewModel(IFinanceStore store, IAppSettingsService settings) { _store = store; _settings = settings; _currency = settings.DefaultCurrency; _locale = settings.Locale; _monthStart = settings.FinancialMonthStartDay; FinishCommand = new AsyncCommand(FinishAsync); }
    public string Currency { get => _currency; set => SetProperty(ref _currency, value); } public string Locale { get => _locale; set => SetProperty(ref _locale, value); } public int MonthStart { get => _monthStart; set => SetProperty(ref _monthStart, Math.Clamp(value, 1, 28)); } public string OpeningBalance { get => _openingBalance; set => SetProperty(ref _openingBalance, value); } public bool SampleData { get => _sampleData; set => SetProperty(ref _sampleData, value); }
    public System.Windows.Input.ICommand FinishCommand { get; }
    private Task FinishAsync() => RunAsync(async () =>
    {
        var currency = Currency.Trim().ToUpperInvariant(); if (currency.Length is < 3 or > 8) throw new InvalidOperationException("Enter a valid currency code such as INR.");
        if (!decimal.TryParse(OpeningBalance, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var opening) && !decimal.TryParse(OpeningBalance, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out opening)) throw new InvalidOperationException("Enter a valid opening balance.");
        _settings.DefaultCurrency = currency; _settings.Locale = string.IsNullOrWhiteSpace(Locale) ? CultureInfo.CurrentCulture.Name : Locale.Trim(); _settings.FinancialMonthStartDay = MonthStart;
        var accounts = await _store.GetAccountsAsync();
        if (accounts.Count == 0 && opening != 0) await _store.SaveAccountAsync(new Account { Name = "Opening balance", Type = AccountType.Bank, Currency = currency, OpeningBalanceMinor = Money.FromMajorUnits(opening, currency).MinorUnits });
        if (SampleData && (await _store.GetAccountsAsync()).Count == 0)
        {
            var id = await _store.SaveAccountAsync(new Account { Name = "Sample wallet", Type = AccountType.DigitalWallet, Currency = currency, OpeningBalanceMinor = Money.FromMajorUnits(10000m, currency).MinorUnits }); var category = (await _store.GetCategoriesAsync()).FirstOrDefault(c => c.Name == "Food"); await _store.SaveTransactionAsync(TransactionFactory.Create(TransactionType.Expense, Money.FromMajorUnits(450m, currency).MinorUnits, currency, id, DateTimeOffset.UtcNow, category?.Id, "Sample merchant", "Sample data can be removed from Settings."));
        }
        _settings.OnboardingComplete = true; await Shell.Current.GoToAsync("//dashboard");
    });
}
