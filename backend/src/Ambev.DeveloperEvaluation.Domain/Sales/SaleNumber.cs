using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class SaleNumber : ValueObject
{
    public string Value { get; }

    private SaleNumber(string value) => Value = value;

    public static Result<SaleNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<SaleNumber>(Error.Validation("SaleNumber.Empty", "Sale number cannot be empty."));

        if (value.Length > 50)
            return Result.Failure<SaleNumber>(Error.Validation("SaleNumber.TooLong", "Sale number cannot exceed 50 characters."));

        return Result.Success(new SaleNumber(value.Trim().ToUpperInvariant()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
