using Ambev.DeveloperEvaluation.Application.Abstractions;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed record CreateSaleCommand(
    string SaleNumber,
    DateTimeOffset SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    string Currency,
    IReadOnlyList<CreateSaleItemCommand> Items) : ICommand<CreateSaleResult>;

public sealed record CreateSaleItemCommand(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
