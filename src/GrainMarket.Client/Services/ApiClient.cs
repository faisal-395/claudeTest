using System.Net.Http.Json;
using System.Text.Json;
using GrainMarket.Application.Auth;
using GrainMarket.Application.ChartOfAccounts;
using GrainMarket.Application.Dashboard;
using GrainMarket.Application.DeductionRules;
using GrainMarket.Application.DualInvoice;
using GrainMarket.Application.Expenses;
using GrainMarket.Application.Kachis;
using GrainMarket.Application.Ledger;
using GrainMarket.Application.Pakkis;
using GrainMarket.Application.Parties;
using GrainMarket.Application.Products;
using GrainMarket.Application.Purchases;
using GrainMarket.Application.Recovery;
using GrainMarket.Application.Roles;
using GrainMarket.Application.SaleInvoices;
using GrainMarket.Application.Seasons;
using GrainMarket.Application.Trading;
using GrainMarket.Application.UnitConversions;
using GrainMarket.Application.Users;
using GrainMarket.Application.Vouchers;
using GrainMarket.Client.Models;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Client.Services;

/// <summary>Thin typed wrapper over the GrainMarket.Api HTTP surface. Every call goes through
/// TokenAuthHandler (attaches the JWT) and throws ApiException with the server's problem+json
/// message on failure, so pages can show one clear error instead of a raw HttpRequestException.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // --- Auth ------------------------------------------------------------------------------
    public Task<LoginResponse> LoginAsync(LoginRequest request) => PostAsync<LoginRequest, LoginResponse>("api/auth/login", request);

    // --- Dashboard ---------------------------------------------------------------------------
    public Task<DashboardSummaryDto> GetDashboardAsync(DateTime? date = null) =>
        GetAsync<DashboardSummaryDto>($"api/dashboard/summary{(date.HasValue ? $"?date={date:yyyy-MM-dd}" : "")}");

    // --- Roles / Users ------------------------------------------------------------------------
    public Task<List<RoleDto>> GetRolesAsync() => GetAsync<List<RoleDto>>("api/roles");
    public Task<RoleDto> GetRoleAsync(int id) => GetAsync<RoleDto>($"api/roles/{id}");
    public Task<RoleDto> CreateRoleAsync(UpsertRoleRequest request) => PostAsync<UpsertRoleRequest, RoleDto>("api/roles", request);
    public Task<RoleDto> UpdateRoleAsync(int id, UpsertRoleRequest request) => PutAsync<UpsertRoleRequest, RoleDto>($"api/roles/{id}", request);

    public Task<List<UserDto>> GetUsersAsync() => GetAsync<List<UserDto>>("api/users");
    public Task<UserDto> CreateUserAsync(CreateUserRequest request) => PostAsync<CreateUserRequest, UserDto>("api/users", request);
    public Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request) => PutAsync<UpdateUserRequest, UserDto>($"api/users/{id}", request);

    // --- Setup: Parties / Products / Unit Conversions / Chart of Accounts / Deduction Rules / Seasons ---
    public Task<List<PartyDto>> GetPartiesAsync(PartyType? type = null, bool includeInactive = false) =>
        GetAsync<List<PartyDto>>($"api/parties?includeInactive={includeInactive}{(type.HasValue ? $"&type={type}" : "")}");
    public Task<PartyDto> CreatePartyAsync(UpsertPartyRequest request) => PostAsync<UpsertPartyRequest, PartyDto>("api/parties", request);
    public Task<PartyDto> UpdatePartyAsync(int id, UpsertPartyRequest request) => PutAsync<UpsertPartyRequest, PartyDto>($"api/parties/{id}", request);
    public Task DeletePartyAsync(int id) => DeleteAsync($"api/parties/{id}");

    public Task<List<ProductDto>> GetProductsAsync(bool includeInactive = false) => GetAsync<List<ProductDto>>($"api/products?includeInactive={includeInactive}");
    public Task<ProductDto> CreateProductAsync(UpsertProductRequest request) => PostAsync<UpsertProductRequest, ProductDto>("api/products", request);
    public Task<ProductDto> UpdateProductAsync(int id, UpsertProductRequest request) => PutAsync<UpsertProductRequest, ProductDto>($"api/products/{id}", request);
    public Task DeleteProductAsync(int id) => DeleteAsync($"api/products/{id}");

    public Task<List<UnitConversionDto>> GetUnitConversionsAsync() => GetAsync<List<UnitConversionDto>>("api/unit-conversions");
    public Task<UnitConversionDto> CreateUnitConversionAsync(UpsertUnitConversionRequest request) => PostAsync<UpsertUnitConversionRequest, UnitConversionDto>("api/unit-conversions", request);
    public Task<UnitConversionDto> UpdateUnitConversionAsync(int id, UpsertUnitConversionRequest request) => PutAsync<UpsertUnitConversionRequest, UnitConversionDto>($"api/unit-conversions/{id}", request);
    public Task DeleteUnitConversionAsync(int id) => DeleteAsync($"api/unit-conversions/{id}");

    public Task<List<ChartOfAccountDto>> GetChartOfAccountsAsync(bool includeInactive = false) => GetAsync<List<ChartOfAccountDto>>($"api/chart-of-accounts?includeInactive={includeInactive}");
    public Task<ChartOfAccountDto> CreateChartOfAccountAsync(UpsertChartOfAccountRequest request) => PostAsync<UpsertChartOfAccountRequest, ChartOfAccountDto>("api/chart-of-accounts", request);
    public Task<ChartOfAccountDto> UpdateChartOfAccountAsync(int id, UpsertChartOfAccountRequest request) => PutAsync<UpsertChartOfAccountRequest, ChartOfAccountDto>($"api/chart-of-accounts/{id}", request);
    public Task DeleteChartOfAccountAsync(int id) => DeleteAsync($"api/chart-of-accounts/{id}");

    public Task<List<DeductionRuleDto>> GetDeductionRulesAsync(bool includeInactive = false) => GetAsync<List<DeductionRuleDto>>($"api/deduction-rules?includeInactive={includeInactive}");
    public Task<DeductionRuleDto> CreateDeductionRuleAsync(UpsertDeductionRuleRequest request) => PostAsync<UpsertDeductionRuleRequest, DeductionRuleDto>("api/deduction-rules", request);
    public Task<DeductionRuleDto> UpdateDeductionRuleAsync(int id, UpsertDeductionRuleRequest request) => PutAsync<UpsertDeductionRuleRequest, DeductionRuleDto>($"api/deduction-rules/{id}", request);
    public Task DeleteDeductionRuleAsync(int id) => DeleteAsync($"api/deduction-rules/{id}");

    public Task<List<SeasonDto>> GetSeasonsAsync() => GetAsync<List<SeasonDto>>("api/seasons");
    public Task<SeasonDto> CreateSeasonAsync(UpsertSeasonRequest request) => PostAsync<UpsertSeasonRequest, SeasonDto>("api/seasons", request);
    public Task<SeasonDto> UpdateSeasonAsync(int id, UpsertSeasonRequest request) => PutAsync<UpsertSeasonRequest, SeasonDto>($"api/seasons/{id}", request);

    // --- Kachi / Pakki / Dual Invoice -----------------------------------------------------------
    public Task<List<KachiDto>> GetKachisAsync(int? seasonId = null) => GetAsync<List<KachiDto>>($"api/kachis{(seasonId.HasValue ? $"?seasonId={seasonId}" : "")}");
    public Task<KachiDto> GetKachiAsync(int id) => GetAsync<KachiDto>($"api/kachis/{id}");
    public Task<KachiDto> CreateKachiAsync(CreateKachiRequest request) => PostAsync<CreateKachiRequest, KachiDto>("api/kachis", request);
    public Task<KachiDto> UpdateKachiAsync(int id, UpdateKachiRequest request) => PutAsync<UpdateKachiRequest, KachiDto>($"api/kachis/{id}", request);
    public Task CancelKachiAsync(int id) => PostAsync($"api/kachis/{id}/cancel");

    public Task<List<PakkiDto>> GetPakkisAsync(int? seasonId = null) => GetAsync<List<PakkiDto>>($"api/pakkis{(seasonId.HasValue ? $"?seasonId={seasonId}" : "")}");
    public Task<PakkiDto> GetPakkiAsync(int id) => GetAsync<PakkiDto>($"api/pakkis/{id}");
    public Task<PakkiDto> CreatePakkiFromKachiAsync(CreatePakkiFromKachiRequest request) => PostAsync<CreatePakkiFromKachiRequest, PakkiDto>("api/pakkis/from-kachi", request);
    public Task<PakkiDto> CreateStandalonePakkiAsync(CreateStandalonePakkiRequest request) => PostAsync<CreateStandalonePakkiRequest, PakkiDto>("api/pakkis/standalone", request);
    public Task<PakkiDto> UpdatePakkiAsync(int id, UpdatePakkiRequest request) => PutAsync<UpdatePakkiRequest, PakkiDto>($"api/pakkis/{id}", request);
    public Task CancelPakkiAsync(int id) => PostAsync($"api/pakkis/{id}/cancel");

    public Task<DualInvoiceResultDto> CreateDualInvoiceAsync(CreateDualInvoiceRequest request) => PostAsync<CreateDualInvoiceRequest, DualInvoiceResultDto>("api/dual-invoice", request);

    // --- Sale Invoice / Purchase -----------------------------------------------------------------
    public Task<List<SaleInvoiceDto>> GetSaleInvoicesAsync() => GetAsync<List<SaleInvoiceDto>>("api/sale-invoices");
    public Task<SaleInvoiceDto> GetSaleInvoiceAsync(int id) => GetAsync<SaleInvoiceDto>($"api/sale-invoices/{id}");
    public Task<SaleInvoiceDto> CreateSaleInvoiceAsync(CreateSaleInvoiceRequest request) => PostAsync<CreateSaleInvoiceRequest, SaleInvoiceDto>("api/sale-invoices", request);
    public Task CancelSaleInvoiceAsync(int id) => PostAsync($"api/sale-invoices/{id}/cancel");

    public Task<List<PurchaseDto>> GetPurchasesAsync() => GetAsync<List<PurchaseDto>>("api/purchases");
    public Task<PurchaseDto> GetPurchaseAsync(int id) => GetAsync<PurchaseDto>($"api/purchases/{id}");
    public Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request) => PostAsync<CreatePurchaseRequest, PurchaseDto>("api/purchases", request);
    public Task CancelPurchaseAsync(int id) => PostAsync($"api/purchases/{id}/cancel");

    // --- Vouchers: Payment / Receipt / Journal -----------------------------------------------------
    public Task<List<VoucherDto>> GetVouchersAsync(VoucherType? type = null, int? seasonId = null) =>
        GetAsync<List<VoucherDto>>($"api/vouchers?{(type.HasValue ? $"type={type}&" : "")}{(seasonId.HasValue ? $"seasonId={seasonId}" : "")}");
    public Task<VoucherDto> CreatePaymentAsync(CreatePaymentOrReceiptRequest request) => PostAsync<CreatePaymentOrReceiptRequest, VoucherDto>("api/vouchers/payment", request);
    public Task<VoucherDto> CreateReceiptAsync(CreatePaymentOrReceiptRequest request) => PostAsync<CreatePaymentOrReceiptRequest, VoucherDto>("api/vouchers/receipt", request);
    public Task<VoucherDto> CreateJournalAsync(CreateJournalRequest request) => PostAsync<CreateJournalRequest, VoucherDto>("api/vouchers/journal", request);
    public Task CancelVoucherAsync(int id) => PostAsync($"api/vouchers/{id}/cancel");

    // --- Expense -----------------------------------------------------------------------------------
    public Task<List<ExpenseDto>> GetExpensesAsync() => GetAsync<List<ExpenseDto>>("api/expenses");
    public Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request) => PostAsync<CreateExpenseRequest, ExpenseDto>("api/expenses", request);

    // --- Ledger / Recovery / Trading -----------------------------------------------------------------
    public Task<PartyLedgerDto> GetPartyLedgerAsync(int partyId, DateTime? from = null, DateTime? to = null) =>
        GetAsync<PartyLedgerDto>($"api/ledger/party/{partyId}{BuildDateQuery(from, to)}");
    public Task<AccountLedgerDto> GetAccountLedgerAsync(int accountId, DateTime? from = null, DateTime? to = null) =>
        GetAsync<AccountLedgerDto>($"api/ledger/account/{accountId}{BuildDateQuery(from, to)}");

    public Task<List<OutstandingPartyDto>> GetOutstandingAsync() => GetAsync<List<OutstandingPartyDto>>("api/recovery/outstanding");
    public Task<List<RecoveryNoteDto>> GetRecoveryNotesAsync(int partyId) => GetAsync<List<RecoveryNoteDto>>($"api/recovery/notes/{partyId}");
    public Task<RecoveryNoteDto> AddRecoveryNoteAsync(CreateRecoveryNoteRequest request) => PostAsync<CreateRecoveryNoteRequest, RecoveryNoteDto>("api/recovery/notes", request);

    public Task<List<TradingProductPositionDto>> GetTradingStockPositionAsync(DateTime? from = null, DateTime? to = null) =>
        GetAsync<List<TradingProductPositionDto>>($"api/trading/stock-position{BuildDateQuery(from, to)}");

    // --- Backup ----------------------------------------------------------------------------------------
    public async Task<string> CreateBackupAsync()
    {
        var result = await PostAsync<object?, BackupResult>("api/backup", null);
        return result.Path;
    }

    private record BackupResult(string Path);

    // --- Low-level helpers -------------------------------------------------------------------------------
    private static string BuildDateQuery(DateTime? from, DateTime? to)
    {
        var parts = new List<string>();
        if (from.HasValue) parts.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) parts.Add($"to={to:yyyy-MM-dd}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        return await ReadOrThrowAsync<T>(response);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _http.PostAsJsonAsync(url, body, JsonOptions);
        return await ReadOrThrowAsync<TResponse>(response);
    }

    private async Task PostAsync(string url)
    {
        var response = await _http.PostAsync(url, content: null);
        await EnsureSuccessAsync(response);
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _http.PutAsJsonAsync(url, body, JsonOptions);
        return await ReadOrThrowAsync<TResponse>(response);
    }

    private async Task DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        await EnsureSuccessAsync(response);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var status = (int)response.StatusCode;
        string message = response.ReasonPhrase ?? "Request failed.";
        Dictionary<string, string[]>? errors = null;

        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("title", out var titleEl))
                {
                    message = titleEl.GetString() ?? message;
                }
                if (doc.RootElement.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Object)
                {
                    errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsEl.GetRawText(), JsonOptions);
                }
            }
        }
        catch
        {
            // Body wasn't the expected problem+json shape — fall back to the reason phrase.
        }

        throw new ApiException(status, message, errors);
    }
}
