using Ambev.DeveloperEvaluation.Application.IntegrationEvents;
using Ambev.DeveloperEvaluation.Domain.Sales.Events;
using Ambev.DeveloperEvaluation.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Ambev.DeveloperEvaluation.Infrastructure.Messaging;

internal sealed class DomainEventHandlers(IMessageBus bus, AppDbContext context)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        object? integrationEvent = notification.DomainEvent switch
        {
            SaleCreatedEvent e => await MapSaleCreatedAsync(e, cancellationToken),
            SaleModifiedEvent e => await MapSaleModifiedAsync(e, cancellationToken),
            SaleCancelledEvent e => new SaleCancelledIntegrationEvent(e.SaleId.Value, e.SaleNumber, e.OccurredOn),
            SaleItemCancelledEvent e => new SaleItemCancelledIntegrationEvent(
                e.SaleId.Value, string.Empty, e.SaleItemId.Value, e.ProductId, e.OccurredOn),
            _ => null
        };

        if (integrationEvent is not null)
            await bus.PublishAsync(integrationEvent);
    }

    private async Task<SaleCreatedIntegrationEvent> MapSaleCreatedAsync(SaleCreatedEvent e, CancellationToken cancellationToken)
    {
        var sale = await context.Sales.FirstOrDefaultAsync(s => s.Id == e.SaleId, cancellationToken);
        return new SaleCreatedIntegrationEvent(
            e.SaleId.Value,
            e.SaleNumber,
            e.SaleDate,
            e.CustomerId,
            e.CustomerName,
            e.BranchId,
            e.BranchName,
            sale?.TotalAmount.Amount ?? 0,
            e.Currency,
            e.OccurredOn);
    }

    private async Task<SaleModifiedIntegrationEvent> MapSaleModifiedAsync(SaleModifiedEvent e, CancellationToken cancellationToken)
    {
        var sale = await context.Sales.FirstOrDefaultAsync(s => s.Id == e.SaleId, cancellationToken);
        return new SaleModifiedIntegrationEvent(
            e.SaleId.Value,
            e.SaleNumber,
            sale?.TotalAmount.Amount ?? 0,
            sale?.TotalAmount.Currency ?? "BRL",
            e.OccurredOn);
    }
}
