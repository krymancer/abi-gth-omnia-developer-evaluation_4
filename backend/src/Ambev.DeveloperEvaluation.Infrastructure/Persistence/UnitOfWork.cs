using Ambev.DeveloperEvaluation.Application.Abstractions;

namespace Ambev.DeveloperEvaluation.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
