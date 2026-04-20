using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales.Events;

public sealed record SaleItemCancelledEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    SaleId SaleId,
    SaleItemId SaleItemId,
    string ProductId) : IDomainEvent;
