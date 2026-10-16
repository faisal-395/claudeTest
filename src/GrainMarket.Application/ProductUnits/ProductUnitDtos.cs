namespace GrainMarket.Application.ProductUnits;

public record ProductUnitDto(int Id, string Name, string? NameUrdu, bool IsActive);

public record CreateProductUnitRequest(string Name, string? NameUrdu);
