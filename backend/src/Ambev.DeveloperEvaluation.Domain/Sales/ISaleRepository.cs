namespace Ambev.DeveloperEvaluation.Domain.Sales;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(SaleId id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);
    void Add(Sale sale);
    void Remove(Sale sale);
}
