namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed record CreateSaleResult(Guid SaleId, string SaleNumber, decimal TotalAmount);
