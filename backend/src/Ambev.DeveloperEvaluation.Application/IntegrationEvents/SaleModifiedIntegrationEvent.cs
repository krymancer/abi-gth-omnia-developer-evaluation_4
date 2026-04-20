namespace Ambev.DeveloperEvaluation.Application.IntegrationEvents;

public sealed record SaleModifiedIntegrationEvent(
    Guid SaleId,
    string SaleNumber,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredOn);
