using Ambev.DeveloperEvaluation.Application.Abstractions;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public sealed record CancelSaleItemCommand(Guid SaleId, Guid SaleItemId) : ICommand;
