using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Sales;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

internal sealed class ListSalesHandler(ISaleRepository saleRepository)
    : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    public async Task<ListSalesResult> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await saleRepository.ListAsync(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        var results = items.Select(s => new GetSaleResult(
            s.Id.Value,
            s.Number.Value,
            s.SaleDate,
            s.Customer.Id,
            s.Customer.Name,
            s.Branch.Id,
            s.Branch.Name,
            s.TotalAmount.Amount,
            s.TotalAmount.Currency,
            s.IsCancelled,
            [])).ToList();

        return new ListSalesResult(results, request.Page, request.PageSize, totalCount);
    }
}
