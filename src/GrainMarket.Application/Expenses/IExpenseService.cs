namespace GrainMarket.Application.Expenses;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);

    /// <summary>Read-only preview of what the next ExpenseNo will be — never consumes it. See
    /// VoucherService.PeekNextVoucherNoAsync for why this exists as a separate call from Create.</summary>
    Task<NextExpenseNoDto> PeekNextExpenseNoAsync(CancellationToken ct = default);
}
