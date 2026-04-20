using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Sales;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Ambev.DeveloperEvaluation.Application.UnitTests.Sales;

public sealed class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests() =>
        _handler = new CreateSaleHandler(_saleRepository, _unitOfWork);

    private static CreateSaleCommand ValidCommand(string number = "SALE-001") => new(
        SaleNumber: number,
        SaleDate: DateTimeOffset.UtcNow,
        CustomerId: Guid.NewGuid(),
        CustomerName: "Test Customer",
        BranchId: Guid.NewGuid(),
        BranchName: "Test Branch",
        Currency: "BRL",
        Items: [new CreateSaleItemCommand("PROD-1", "Product One", 3, 100m)]);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSaleId()
    {
        _saleRepository.GetByNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SaleNumber.Should().Be("SALE-001");
        result.Value.TotalAmount.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsToRepository()
    {
        _saleRepository.GetByNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _saleRepository.Received(1).Add(Arg.Any<Sale>());
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        _saleRepository.GetByNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSaleNumber_ReturnsConflictError()
    {
        var existing = Sale.Create("SALE-001", DateTimeOffset.UtcNow, Guid.NewGuid(), "A", Guid.NewGuid(), "B").Value;
        _saleRepository.GetByNumberAsync("SALE-001", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand("SALE-001"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict.Sale.DuplicateNumber");
    }

    [Fact]
    public async Task Handle_ItemWithQuantityAboveMax_ReturnsValidationError()
    {
        _saleRepository.GetByNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = ValidCommand() with
        {
            Items = [new CreateSaleItemCommand("PROD-1", "Product One", 21, 100m)]
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation.");
    }
}
