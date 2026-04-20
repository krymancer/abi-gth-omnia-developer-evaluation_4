using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales.Events;

public sealed record SaleModifiedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    SaleId SaleId,
    string SaleNumber) : IDomainEvent;
