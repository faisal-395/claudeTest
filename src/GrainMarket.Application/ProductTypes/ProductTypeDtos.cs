namespace GrainMarket.Application.ProductTypes;

public record ProductTypeDto(int Id, string Name, string? NameUrdu, bool IsActive);

public record CreateProductTypeRequest(string Name, string? NameUrdu);
