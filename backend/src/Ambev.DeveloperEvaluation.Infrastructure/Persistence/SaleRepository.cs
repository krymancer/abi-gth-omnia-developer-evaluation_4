using Ambev.DeveloperEvaluation.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.Infrastructure.Persistence;

internal sealed class SaleRepository(AppDbContext context) : ISaleRepository
{
    public async Task<Sale?> GetByIdAsync(SaleId id, CancellationToken cancellationToken = default) =>
        await context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Sale?> GetByNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        await context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Number.Value == saleNumber, cancellationToken);

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = context.Sales.Include(s => s.Items).AsQueryable();

        query = (sortBy?.ToLowerInvariant(), sortDescending) switch
        {
            ("date", false) => query.OrderBy(s => s.SaleDate),
            ("date", true) => query.OrderByDescending(s => s.SaleDate),
            ("total", false) => query.OrderBy(s => s.TotalAmount.Amount),
            ("total", true) => query.OrderByDescending(s => s.TotalAmount.Amount),
            _ => query.OrderByDescending(s => s.SaleDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Sale sale) => context.Sales.Add(sale);

    public void Remove(Sale sale) => context.Sales.Remove(sale);
}
