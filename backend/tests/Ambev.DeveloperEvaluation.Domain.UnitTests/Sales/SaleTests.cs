using Ambev.DeveloperEvaluation.Domain.Sales;
using Ambev.DeveloperEvaluation.Domain.Sales.Events;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Domain.UnitTests.Sales;

public sealed class SaleTests
{
    private static Sale CreateValidSale(string number = "SALE-001") =>
        Sale.Create(number, DateTimeOffset.UtcNow, Guid.NewGuid(), "Customer A", Guid.NewGuid(), "Branch X").Value;

    [Fact]
    public void Create_ValidParameters_Succeeds()
    {
        var result = Sale.Create("S-001", DateTimeOffset.UtcNow, Guid.NewGuid(), "Cust", Guid.NewGuid(), "Branch");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCancelled.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_RaisesSaleCreatedEvent()
    {
        var result = Sale.Create("S-002", DateTimeOffset.UtcNow, Guid.NewGuid(), "Cust", Guid.NewGuid(), "Branch");
        var sale = result.Value;
        var events = sale.PopDomainEvents();

        events.Should().ContainSingle(e => e is SaleCreatedEvent);
    }

    [Fact]
    public void Create_PopDomainEvents_ClearsEvents()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();

        sale.PopDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ValidItem_Succeeds()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();

        var result = sale.AddItem("P1", "Product One", 3, 100m);

        result.IsSuccess.Should().BeTrue();
        sale.Items.Should().HaveCount(1);
        sale.TotalAmount.Amount.Should().Be(300m); // 3 * 100, no discount below 4
    }

    [Fact]
    public void AddItem_QuantityExceedsMax_Fails()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();

        var result = sale.AddItem("P1", "Product One", 21, 100m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation.");
    }

    [Fact]
    public void AddItem_ToCancelledSale_Fails()
    {
        var sale = CreateValidSale();
        sale.Cancel();

        var result = sale.AddItem("P1", "Product One", 1, 10m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict.Sale.Cancelled");
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesQuantity()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();
        sale.AddItem("P1", "Product One", 3, 100m);

        var result = sale.AddItem("P1", "Product One", 4, 100m);

        result.IsSuccess.Should().BeTrue();
        sale.Items.Should().HaveCount(1);
        sale.Items[0].Quantity.Value.Should().Be(7);
    }

    [Fact]
    public void CancelItem_ExistingItem_Succeeds()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();
        sale.AddItem("P1", "Product One", 1, 50m);
        var itemId = sale.Items[0].Id;

        var result = sale.CancelItem(itemId);

        result.IsSuccess.Should().BeTrue();
        sale.Items[0].IsCancelled.Should().BeTrue();
        sale.TotalAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public void CancelItem_RaisesSaleItemCancelledEvent()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();
        sale.AddItem("P1", "Product One", 1, 50m);
        var itemId = sale.Items[0].Id;

        sale.CancelItem(itemId);
        var events = sale.PopDomainEvents();

        events.Should().ContainSingle(e => e is SaleItemCancelledEvent);
    }

    [Fact]
    public void CancelItem_NotFound_Fails()
    {
        var sale = CreateValidSale();

        var result = sale.CancelItem(SaleItemId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound.SaleItem.NotFound");
    }

    [Fact]
    public void Cancel_ActiveSale_SetsIsCancelled()
    {
        var sale = CreateValidSale();
        sale.AddItem("P1", "Product One", 2, 100m);
        sale.PopDomainEvents();

        var result = sale.Cancel();

        result.IsSuccess.Should().BeTrue();
        sale.IsCancelled.Should().BeTrue();
        sale.Items.Should().AllSatisfy(i => i.IsCancelled.Should().BeTrue());
    }

    [Fact]
    public void Cancel_RaisesSaleCancelledEvent()
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();

        sale.Cancel();
        var events = sale.PopDomainEvents();

        events.Should().ContainSingle(e => e is SaleCancelledEvent);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Fails()
    {
        var sale = CreateValidSale();
        sale.Cancel();
        sale.PopDomainEvents();

        var result = sale.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict.Sale.AlreadyCancelled");
    }

    [Theory]
    [InlineData(1, 100, 100)]    // no discount
    [InlineData(4, 100, 360)]    // 10% discount: 4*100*0.9
    [InlineData(10, 100, 800)]   // 20% discount: 10*100*0.8
    public void AddItem_TotalPrice_AppliesDiscountTier(int qty, decimal price, decimal expectedTotal)
    {
        var sale = CreateValidSale();
        sale.PopDomainEvents();

        sale.AddItem("P1", "Product", qty, price);

        sale.TotalAmount.Amount.Should().Be(expectedTotal);
    }
}
