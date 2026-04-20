namespace Ambev.DeveloperEvaluation.Application.IntegrationEvents;

public sealed record SaleCancelledIntegrationEvent(
    Guid SaleId,
    string SaleNumber,
    DateTimeOffset OccurredOn);
