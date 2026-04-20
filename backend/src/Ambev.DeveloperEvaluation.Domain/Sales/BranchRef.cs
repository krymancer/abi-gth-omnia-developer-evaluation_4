using Ambev.DeveloperEvaluation.Domain.SharedKernel;

namespace Ambev.DeveloperEvaluation.Domain.Sales;

public sealed class BranchRef : ValueObject
{
    public Guid Id { get; }
    public string Name { get; }

    private BranchRef(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Result<BranchRef> Create(Guid id, string name)
    {
        if (id == Guid.Empty)
            return Result.Failure<BranchRef>(Error.Validation("BranchRef.EmptyId", "Branch ID cannot be empty."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<BranchRef>(Error.Validation("BranchRef.EmptyName", "Branch name cannot be empty."));

        if (name.Length > 100)
            return Result.Failure<BranchRef>(Error.Validation("BranchRef.NameTooLong", "Branch name cannot exceed 100 characters."));

        return Result.Success(new BranchRef(id, name.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }
}
