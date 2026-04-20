using Ambev.DeveloperEvaluation.Application.Abstractions;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public sealed record UpdateSaleCommand(
    Guid SaleId,
    IReadOnlyList<UpdateSaleItemCommand> Items) : ICommand;

public sealed record UpdateSaleItemCommand(
    Guid ItemId,
    int Quantity,
    decimal UnitPrice,
    string Currency);
