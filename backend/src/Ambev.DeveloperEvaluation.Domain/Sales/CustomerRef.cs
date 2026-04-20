using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class CustomerRef : ValueObject
{
    public Guid Id { get; }
    public string Name { get; }

    private CustomerRef(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Result<CustomerRef> Create(Guid id, string name)
    {
        if (id == Guid.Empty)
            return Result.Failure<CustomerRef>(Error.Validation("CustomerRef.EmptyId", "Customer ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<CustomerRef>(Error.Validation("CustomerRef.EmptyName", "Customer name cannot be empty."));

        if (name.Length > 100)
            return Result.Failure<CustomerRef>(Error.Validation("CustomerRef.NameTooLong", "Customer name cannot exceed 100 characters."));

        return Result.Success(new CustomerRef(id, name.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }
}
