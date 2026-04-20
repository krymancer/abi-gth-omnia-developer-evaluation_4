using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class SaleItem : Entity<SaleItemId>
{
    public string ProductId { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Quantity Quantity { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;
    public decimal DiscountRate { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public bool IsCancelled { get; private set; }

    private SaleItem() { }

    internal static Result<SaleItem> Create(
        string productId,
        string productName,
        int quantity,
        decimal unitPrice,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return Result.Failure<SaleItem>(Error.Validation("SaleItem.EmptyProductId", "Product ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(productName))
            return Result.Failure<SaleItem>(Error.Validation("SaleItem.EmptyProductName", "Product name cannot be empty."));

        var qtyResult = Quantity.Create(quantity);
        if (qtyResult.IsFailure) return Result.Failure<SaleItem>(qtyResult.Error);

        var priceResult = Money.Create(unitPrice, currency);
        if (priceResult.IsFailure) return Result.Failure<SaleItem>(priceResult.Error);

        var item = new SaleItem
        {
            Id = SaleItemId.New(),
            ProductId = productId.Trim(),
            ProductName = productName.Trim(),
            Quantity = qtyResult.Value,
            UnitPrice = priceResult.Value,
        };

        item.RecalculateTotal();
        return Result.Success(item);
    }

    internal Result Update(int newQuantity, decimal newUnitPrice, string currency)
    {
        if (IsCancelled)
            return Result.Failure(Error.Validation("SaleItem.Cancelled", "Cannot update a cancelled item."));

        var qtyResult = Quantity.Create(newQuantity);
        if (qtyResult.IsFailure) return Result.Failure(qtyResult.Error);

        var priceResult = Money.Create(newUnitPrice, currency);
        if (priceResult.IsFailure) return Result.Failure(priceResult.Error);

        Quantity = qtyResult.Value;
        UnitPrice = priceResult.Value;
        RecalculateTotal();
        return Result.Success();
    }

    internal Result Cancel()
    {
        if (IsCancelled)
            return Result.Failure(Error.Conflict("SaleItem.AlreadyCancelled", "Sale item is already cancelled."));

        IsCancelled = true;
        return Result.Success();
    }

    private void RecalculateTotal()
    {
        DiscountRate = DiscountPolicy.GetRate(Quantity.Value);
        TotalPrice = UnitPrice.Multiply(Quantity.Value).ApplyDiscount(DiscountRate);
    }
}
