using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales.Events;

public sealed record SaleCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    SaleId SaleId,
    string SaleNumber,
    DateTimeOffset SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    string Currency) : IDomainEvent;
