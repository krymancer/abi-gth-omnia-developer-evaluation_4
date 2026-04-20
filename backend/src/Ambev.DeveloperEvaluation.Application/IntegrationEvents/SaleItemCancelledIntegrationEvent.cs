namespace Ambev.DeveloperEvaluation.Application.IntegrationEvents;

public sealed record SaleItemCancelledIntegrationEvent(
    Guid SaleId,
    string SaleNumber,
    Guid SaleItemId,
    string ProductId,
    DateTimeOffset OccurredOn);
