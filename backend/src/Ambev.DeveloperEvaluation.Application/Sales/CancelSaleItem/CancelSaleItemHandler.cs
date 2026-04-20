using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.Sales;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

internal sealed class CancelSaleItemHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelSaleItemCommand, Result>
{
    public async Task<Result> Handle(CancelSaleItemCommand request, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdAsync(new SaleId(request.SaleId), cancellationToken);
        if (sale is null)
            return Result.Failure(Error.NotFound("Sale.NotFound", $"Sale '{request.SaleId}' not found."));

        var cancelResult = sale.CancelItem(new SaleItemId(request.SaleItemId));
        if (cancelResult.IsFailure) return cancelResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
