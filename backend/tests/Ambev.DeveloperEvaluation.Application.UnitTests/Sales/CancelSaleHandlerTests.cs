using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Sales;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Ambev.DeveloperEvaluation.Application.UnitTests.Sales;

public sealed class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests() =>
        _handler = new CancelSaleHandler(_saleRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ExistingSale_CancelsSaleAndSaves()
    {
        var sale = Sale.Create("S-001", DateTimeOffset.UtcNow, Guid.NewGuid(), "Cust", Guid.NewGuid(), "Branch").Value;
        _saleRepository.GetByIdAsync(new SaleId(sale.Id.Value), Arg.Any<CancellationToken>())
            .Returns(sale);

        var result = await _handler.Handle(new CancelSaleCommand(sale.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sale.IsCancelled.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaleNotFound_ReturnsNotFoundError()
    {
        _saleRepository.GetByIdAsync(Arg.Any<SaleId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await _handler.Handle(new CancelSaleCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound.Sale.NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyCancelledSale_ReturnsConflictError()
    {
        var sale = Sale.Create("S-001", DateTimeOffset.UtcNow, Guid.NewGuid(), "Cust", Guid.NewGuid(), "Branch").Value;
        sale.Cancel();
        _saleRepository.GetByIdAsync(new SaleId(sale.Id.Value), Arg.Any<CancellationToken>())
            .Returns(sale);

        var result = await _handler.Handle(new CancelSaleCommand(sale.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict.Sale.AlreadyCancelled");
    }
}
