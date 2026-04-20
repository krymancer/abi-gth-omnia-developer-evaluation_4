using Ambev.DeveloperEvaluation.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.Infrastructure.Persistence.Configurations;

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new SaleItemId(value))
            .HasColumnName("id");

        builder.Property(i => i.ProductId).HasColumnName("product_id").HasMaxLength(100).IsRequired();
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.DiscountRate).HasColumnName("discount_rate").HasPrecision(5, 4).IsRequired();
        builder.Property(i => i.IsCancelled).HasColumnName("is_cancelled").HasDefaultValue(false);

        builder.OwnsOne(i => i.Quantity, qb =>
            qb.Property(q => q.Value).HasColumnName("quantity").IsRequired());

        builder.OwnsOne(i => i.UnitPrice, ub =>
        {
            ub.Property(m => m.Amount).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            ub.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(i => i.TotalPrice, tb =>
        {
            tb.Property(m => m.Amount).HasColumnName("total_price").HasPrecision(18, 2).IsRequired();
            tb.Property(m => m.Currency).HasColumnName("total_price_currency").HasMaxLength(3).IsRequired();
        });

        builder.Property<Guid>("sale_id").HasColumnName("sale_id");
    }
}
