using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class Quantity : ValueObject
{
    public const int MaxPerItem = 20;

    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static Result<Quantity> Create(int value)
    {
        if (value <= 0)
            return Result.Failure<Quantity>(Error.Validation("Quantity.NonPositive", "Quantity must be greater than zero."));

        if (value > MaxPerItem)
            return Result.Failure<Quantity>(Error.Validation("Quantity.ExceedsMax", $"Cannot sell more than {MaxPerItem} identical items per sale item."));

        return Result.Success(new Quantity(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
