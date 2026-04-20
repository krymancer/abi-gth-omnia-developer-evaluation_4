using Ambev.DeveloperEvaluation.Domain.Sales;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Domain.UnitTests.Sales;

public sealed class DiscountPolicyTests
{
    [Theory]
    [InlineData(1, 0.00)]
    [InlineData(3, 0.00)]
    public void GetRate_BelowFour_ReturnsZero(int quantity, decimal expected) =>
        DiscountPolicy.GetRate(quantity).Should().Be(expected);

    [Theory]
    [InlineData(4, 0.10)]
    [InlineData(9, 0.10)]
    public void GetRate_FourToNine_ReturnsTenPercent(int quantity, decimal expected) =>
        DiscountPolicy.GetRate(quantity).Should().Be(expected);

    [Theory]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void GetRate_TenToTwenty_ReturnsTwentyPercent(int quantity, decimal expected) =>
        DiscountPolicy.GetRate(quantity).Should().Be(expected);
}
