using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class ImportViewModel : ViewModelBase
{
    private readonly ICsvImportService _import;
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private readonly string _noneOption;
    private byte[]? _fileBytes;
    private AccountSummary? _fallbackAccount;
    private bool _createMissingCategories = true;
    private bool _skipDuplicates = true;
    private bool _amountIsMinorUnits;
    private string _fileName;
    private string _summary;
    private string _status = string.Empty;
    private string? _dateColumn;
    private string? _typeColumn;
    private string? _amountColumn;
    private string? _accountColumn;
    private string? _currencyColumn;
    private string? _categoryColumn;
    private string? _merchantColumn;
    private string? _noteColumn;
    private string? _paymentMethodColumn;
    private string? _locationColumn;
    private string? _transferGroupColumn;
    private string? _counterpartyAccountColumn;
    private string? _tagsColumn;

    public ImportViewModel(ICsvImportService import, IFinanceStore store, IAppSettingsService settings)
    {
        _import = import;
        _store = store;
        _settings = settings;
        _noneOption = L("ImportNoneDisplay");
        _fileName = L("ImportNoCsvSelected");
        _summary = L("ImportChooseToValidate");
        OptionalHeaders.Add(_noneOption);
        ImportCommand = new AsyncCommand(ImportAsync);
        RevalidateCommand = new AsyncCommand(RevalidateAsync);
    }

    public ObservableCollection<CsvImportPreviewRow> PreviewRows { get; } = [];
    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public ObservableCollection<string> Headers { get; } = [];
    public ObservableCollection<string> OptionalHeaders { get; } = [];
    public string FileName { get => _fileName; private set => SetProperty(ref _fileName, value); }
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public AccountSummary? FallbackAccount { get => _fallbackAccount; set => SetProperty(ref _fallbackAccount, value); }
    public bool CreateMissingCategories { get => _createMissingCategories; set => SetProperty(ref _createMissingCategories, value); }
    public bool SkipDuplicates { get => _skipDuplicates; set => SetProperty(ref _skipDuplicates, value); }
    public bool AmountIsMinorUnits { get => _amountIsMinorUnits; set => SetProperty(ref _amountIsMinorUnits, value); }
    public string? DateColumn { get => _dateColumn; set => SetProperty(ref _dateColumn, value); }
    public string? TypeColumn { get => _typeColumn; set => SetProperty(ref _typeColumn, value); }
    public string? AmountColumn { get => _amountColumn; set => SetProperty(ref _amountColumn, value); }
    public string? AccountColumn { get => _accountColumn; set => SetProperty(ref _accountColumn, value); }
    public string? CurrencyColumn { get => _currencyColumn; set => SetProperty(ref _currencyColumn, value); }
    public string? CategoryColumn { get => _categoryColumn; set => SetProperty(ref _categoryColumn, value); }
    public string? MerchantColumn { get => _merchantColumn; set => SetProperty(ref _merchantColumn, value); }
    public string? NoteColumn { get => _noteColumn; set => SetProperty(ref _noteColumn, value); }
    public string? PaymentMethodColumn { get => _paymentMethodColumn; set => SetProperty(ref _paymentMethodColumn, value); }
    public string? LocationColumn { get => _locationColumn; set => SetProperty(ref _locationColumn, value); }
    public string? TransferGroupColumn { get => _transferGroupColumn; set => SetProperty(ref _transferGroupColumn, value); }
    public string? CounterpartyAccountColumn { get => _counterpartyAccountColumn; set => SetProperty(ref _counterpartyAccountColumn, value); }
    public string? TagsColumn { get => _tagsColumn; set => SetProperty(ref _tagsColumn, value); }
    public System.Windows.Input.ICommand ImportCommand { get; }
    public System.Windows.Input.ICommand RevalidateCommand { get; }
    public bool HasFile => _fileBytes is { Length: > 0 } && RequiredMappingPresent;
    private bool RequiredMappingPresent => !string.IsNullOrWhiteSpace(DateColumn) && !string.IsNullOrWhiteSpace(TypeColumn) && !string.IsNullOrWhiteSpace(AmountColumn) && !string.IsNullOrWhiteSpace(AccountColumn);

    public async Task LoadAccountsAsync()
    {
        Accounts.Clear();
        foreach (var account in await _store.GetAccountsAsync())
            if (account.State != AccountState.Archived)
                Accounts.Add(account);
        FallbackAccount ??= Accounts.FirstOrDefault(x => x.Id == _settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
    }

    public Task LoadFileAsync(byte[] bytes, string fileName) => RunAsync(async () =>
    {
        if (bytes.Length == 0) throw new InvalidOperationException(L("ImportEmptyFile"));
        _fileBytes = bytes;
        FileName = fileName;
        await using var stream = new MemoryStream(bytes, writable: false);
        var preview = await _import.PreviewAsync(stream, _settings.DefaultCurrency);
        if (!preview.IsSuccess || preview.Value is null)
        {
            PreviewRows.Clear();
            OnPropertyChanged(nameof(HasFile));
            throw new InvalidOperationException(preview.Error);
        }

        Headers.Clear();
        OptionalHeaders.Clear();
        OptionalHeaders.Add(_noneOption);
        foreach (var header in preview.Value.Headers)
        {
            Headers.Add(header);
            OptionalHeaders.Add(header);
        }
        ApplyMapping(preview.Value.SuggestedMapping);
        ApplyPreview(preview.Value);
        Status = L("ImportReviewMapping");
    });

    private Task RevalidateAsync() => RunAsync(async () =>
    {
        if (_fileBytes is null) throw new InvalidOperationException(L("ImportChooseFileFirst"));
        var mapping = BuildMapping();
        await using var stream = new MemoryStream(_fileBytes, writable: false);
        var preview = await _import.PreviewWithMappingAsync(stream, _settings.DefaultCurrency, mapping);
        if (!preview.IsSuccess || preview.Value is null) throw new InvalidOperationException(preview.Error);
        ApplyPreview(preview.Value);
        Status = L("ImportMappingRevalidated");
        OnPropertyChanged(nameof(HasFile));
    });

    private Task ImportAsync() => RunAsync(async () =>
    {
        if (_fileBytes is null) throw new InvalidOperationException(L("ImportChooseValidateFirst"));
        var mapping = BuildMapping();
        await using var stream = new MemoryStream(_fileBytes, writable: false);
        var options = new CsvImportOptions(mapping, FallbackAccount?.Id, CreateMissingCategories, SkipDuplicates, _settings.DefaultCurrency);
        var result = await _import.ImportAsync(stream, options);
        if (!result.IsSuccess || result.Value is null) throw new InvalidOperationException(result.Error);
        var value = result.Value;
        Status = Format("ImportResultFormat", value.ImportedRows, value.SkippedDuplicateRows, value.InvalidRows);
        if (value.Errors.Count > 0)
            Status += L("ImportFirstValidationMessages") + string.Join(" | ", value.Errors.Take(5));
    });

    private CsvColumnMapping BuildMapping()
    {
        if (!RequiredMappingPresent) throw new InvalidOperationException(L("ImportRequiredMappingError"));
        return new CsvColumnMapping(DateColumn!, TypeColumn!, AmountColumn!, AccountColumn!, Optional(CurrencyColumn), Optional(CategoryColumn), Optional(MerchantColumn), Optional(NoteColumn), Optional(PaymentMethodColumn), Optional(LocationColumn), Optional(TransferGroupColumn), Optional(CounterpartyAccountColumn), Optional(TagsColumn), AmountIsMinorUnits);
    }

    private void ApplyMapping(CsvColumnMapping? mapping)
    {
        if (mapping is null) return;
        DateColumn = mapping.DateColumn;
        TypeColumn = mapping.TypeColumn;
        AmountColumn = mapping.AmountColumn;
        AccountColumn = mapping.AccountColumn;
        CurrencyColumn = mapping.CurrencyColumn ?? _noneOption;
        CategoryColumn = mapping.CategoryColumn ?? _noneOption;
        MerchantColumn = mapping.MerchantColumn ?? _noneOption;
        NoteColumn = mapping.NoteColumn ?? _noneOption;
        PaymentMethodColumn = mapping.PaymentMethodColumn ?? _noneOption;
        LocationColumn = mapping.LocationColumn ?? _noneOption;
        TransferGroupColumn = mapping.TransferGroupColumn ?? _noneOption;
        CounterpartyAccountColumn = mapping.CounterpartyAccountColumn ?? _noneOption;
        TagsColumn = mapping.TagsColumn ?? _noneOption;
        AmountIsMinorUnits = mapping.AmountIsMinorUnits;
        OnPropertyChanged(nameof(HasFile));
    }

    private void ApplyPreview(CsvImportPreview preview)
    {
        PreviewRows.Clear();
        foreach (var row in preview.Rows) PreviewRows.Add(row);
        Summary = Format("ImportPreviewSummaryFormat", preview.ValidRows + preview.InvalidRows, preview.ValidRows, preview.InvalidRows);
    }

    private string? Optional(string? value) => string.IsNullOrWhiteSpace(value) || value == _noneOption ? null : value;
    private static string L(string key) => LocalizationResources.Get(key);
    private static string Format(string key, params object[] values) => string.Format(CultureInfo.CurrentCulture, L(key), values);
}
