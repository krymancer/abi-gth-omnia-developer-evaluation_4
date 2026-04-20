namespace Ambev.DeveloperEvaluation.Domain.SharedKernel;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    IReadOnlyList<IDomainEvent> PopDomainEvents();
}
