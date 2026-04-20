using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.Sales;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

internal sealed class UpdateSaleHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSaleCommand, Result>
{
    public async Task<Result> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdAsync(new SaleId(request.SaleId), cancellationToken);
        if (sale is null)
            return Result.Failure(Error.NotFound("Sale.NotFound", $"Sale '{request.SaleId}' not found."));

        foreach (var item in request.Items)
        {
            var updateResult = sale.UpdateItem(new SaleItemId(item.ItemId), item.Quantity, item.UnitPrice, item.Currency);
            if (updateResult.IsFailure) return updateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
