using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.Sales;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

internal sealed class CreateSaleHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSaleCommand, Result<CreateSaleResult>>
{
    public async Task<Result<CreateSaleResult>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var existing = await saleRepository.GetByNumberAsync(request.SaleNumber, cancellationToken);
        if (existing is not null)
            return Result.Failure<CreateSaleResult>(
                Error.Conflict("Sale.DuplicateNumber", $"Sale number '{request.SaleNumber}' already exists."));

        var saleResult = Sale.Create(
            request.SaleNumber,
            request.SaleDate,
            request.CustomerId,
            request.CustomerName,
            request.BranchId,
            request.BranchName,
            request.Currency);

        if (saleResult.IsFailure)
            return Result.Failure<CreateSaleResult>(saleResult.Error);

        var sale = saleResult.Value;

        foreach (var item in request.Items)
        {
            var addResult = sale.AddItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                request.Currency);

            if (addResult.IsFailure)
                return Result.Failure<CreateSaleResult>(addResult.Error);
        }

        saleRepository.Add(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateSaleResult(
            sale.Id.Value,
            sale.Number.Value,
            sale.TotalAmount.Amount));
    }
}
