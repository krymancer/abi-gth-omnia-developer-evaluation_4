using Ambev.DeveloperEvaluation.Domain.Sales;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

internal sealed class GetSaleHandler(ISaleRepository saleRepository)
    : IRequestHandler<GetSaleQuery, GetSaleResult?>
{
    public async Task<GetSaleResult?> Handle(GetSaleQuery request, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdAsync(new SaleId(request.SaleId), cancellationToken);
        return sale is null ? null : MapToResult(sale);
    }

    private static GetSaleResult MapToResult(Sale sale) => new(
        sale.Id.Value,
        sale.Number.Value,
        sale.SaleDate,
        sale.Customer.Id,
        sale.Customer.Name,
        sale.Branch.Id,
        sale.Branch.Name,
        sale.TotalAmount.Amount,
        sale.TotalAmount.Currency,
        sale.IsCancelled,
        sale.Items.Select(i => new GetSaleItemResult(
            i.Id.Value,
            i.ProductId,
            i.ProductName,
            i.Quantity.Value,
            i.UnitPrice.Amount,
            i.DiscountRate,
            i.TotalPrice.Amount,
            i.IsCancelled)).ToList());
}
