namespace Ambev.DeveloperEvaluation.Domain.Sales;

public readonly record struct SaleItemId(Guid Value)
{
    public static SaleItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
