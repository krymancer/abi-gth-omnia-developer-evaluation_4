namespace Ambev.DeveloperEvaluation.Application.IntegrationEvents;

public sealed record SaleCreatedIntegrationEvent(
    Guid SaleId,
    string SaleNumber,
    DateTimeOffset SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredOn);
