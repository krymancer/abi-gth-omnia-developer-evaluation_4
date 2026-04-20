namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public sealed record GetSaleResult(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    string Currency,
    bool IsCancelled,
    IReadOnlyList<GetSaleItemResult> Items);

public sealed record GetSaleItemResult(
    Guid Id,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TotalPrice,
    bool IsCancelled);
