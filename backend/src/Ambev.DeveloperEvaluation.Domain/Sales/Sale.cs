using Ambev.DeveloperEvaluation.Domain.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class Sale : AggregateRoot<SaleId>
{
    private readonly List<SaleItem> _items = [];

    public SaleNumber Number { get; private set; } = null!;
    public DateTimeOffset SaleDate { get; private set; }
    public CustomerRef Customer { get; private set; } = null!;
    public BranchRef Branch { get; private set; } = null!;
    public bool IsCancelled { get; private set; }
    public Money TotalAmount { get; private set; } = null!;

    public IReadOnlyList<SaleItem> Items => _items.AsReadOnly();

    private Sale() { }

    public static Result<Sale> Create(
        string saleNumber,
        DateTimeOffset saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        string currency = "BRL")
    {
        var numberResult = SaleNumber.Create(saleNumber);
        if (numberResult.IsFailure) return Result.Failure<Sale>(numberResult.Error);

        var customerResult = CustomerRef.Create(customerId, customerName);
        if (customerResult.IsFailure) return Result.Failure<Sale>(customerResult.Error);

        var branchResult = BranchRef.Create(branchId, branchName);
        if (branchResult.IsFailure) return Result.Failure<Sale>(branchResult.Error);

        var sale = new Sale
        {
            Id = SaleId.New(),
            Number = numberResult.Value,
            SaleDate = saleDate,
            Customer = customerResult.Value,
            Branch = branchResult.Value,
            TotalAmount = Money.Of(0, currency),
        };

        sale.RaiseDomainEvent(new SaleCreatedEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            sale.Id,
            sale.Number.Value,
            saleDate,
            customerId,
            customerName,
            branchId,
            branchName,
            currency));

        return Result.Success(sale);
    }

    public Result AddItem(
        string productId,
        string productName,
        int quantity,
        decimal unitPrice,
        string currency = "BRL")
    {
        if (IsCancelled)
            return Result.Failure(Error.Conflict("Sale.Cancelled", "Cannot add items to a cancelled sale."));

        var existing = _items.FirstOrDefault(i => i.ProductId == productId && !i.IsCancelled);
        if (existing is not null)
        {
            var newQty = existing.Quantity.Value + quantity;
            var updateResult = existing.Update(newQty, unitPrice, currency);
            if (updateResult.IsFailure) return updateResult;
        }
        else
        {
            var itemResult = SaleItem.Create(productId, productName, quantity, unitPrice, currency);
            if (itemResult.IsFailure) return Result.Failure(itemResult.Error);
            _items.Add(itemResult.Value);
        }

        RecalculateTotal(currency);
        return Result.Success();
    }

    public Result UpdateItem(
        SaleItemId itemId,
        int newQuantity,
        decimal newUnitPrice,
        string currency = "BRL")
    {
        if (IsCancelled)
            return Result.Failure(Error.Conflict("Sale.Cancelled", "Cannot update items on a cancelled sale."));

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure(Error.NotFound("SaleItem.NotFound", $"Item {itemId} not found in sale."));

        var updateResult = item.Update(newQuantity, newUnitPrice, currency);
        if (updateResult.IsFailure) return updateResult;

        RecalculateTotal(currency);

        RaiseDomainEvent(new SaleModifiedEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            Number.Value));

        return Result.Success();
    }

    public Result CancelItem(SaleItemId itemId)
    {
        if (IsCancelled)
            return Result.Failure(Error.Conflict("Sale.Cancelled", "Sale is already cancelled."));

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure(Error.NotFound("SaleItem.NotFound", $"Item {itemId} not found in sale."));

        var cancelResult = item.Cancel();
        if (cancelResult.IsFailure) return cancelResult;

        RecalculateTotal(TotalAmount.Currency);

        RaiseDomainEvent(new SaleItemCancelledEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            itemId,
            item.ProductId));

        return Result.Success();
    }

    public Result Cancel()
    {
        if (IsCancelled)
            return Result.Failure(Error.Conflict("Sale.AlreadyCancelled", "Sale is already cancelled."));

        IsCancelled = true;

        foreach (var item in _items.Where(i => !i.IsCancelled))
            item.Cancel();

        RaiseDomainEvent(new SaleCancelledEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            Number.Value));

        return Result.Success();
    }

    private void RecalculateTotal(string currency)
    {
        var total = _items
            .Where(i => !i.IsCancelled)
            .Aggregate(Money.Of(0, currency), (acc, item) => acc.Add(item.TotalPrice));

        TotalAmount = total;
    }
}
