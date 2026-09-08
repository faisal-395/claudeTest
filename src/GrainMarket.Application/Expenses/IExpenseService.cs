namespace GrainMarket.Application.Expenses;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
}
