namespace GrainMarket.Application.Products;

public record ProductDto(int Id, string Name, string? NameUrdu, string? Category, string BaseUnit, decimal DefaultRate, bool IsActive);

public record UpsertProductRequest(string Name, string? NameUrdu, string? Category, decimal DefaultRate, bool IsActive);
