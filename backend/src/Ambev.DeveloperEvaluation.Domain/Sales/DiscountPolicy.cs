namespace Ambev.DeveloperEvaluation.Domain.Sales;

public static class DiscountPolicy
{
    public static decimal GetRate(int quantity) => quantity switch
    {
        >= 10 => 0.20m,
        >= 4 => 0.10m,
        _ => 0m
    };
}
