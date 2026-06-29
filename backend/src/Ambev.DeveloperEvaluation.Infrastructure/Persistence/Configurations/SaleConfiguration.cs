using Ambev.DeveloperEvaluation.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new SaleId(value))
            .HasColumnName("id");

        builder.OwnsOne(s => s.Number, nb =>
        {
            nb.Property(n => n.Value)
                .HasColumnName("sale_number")
                .HasMaxLength(50)
                .IsRequired();
            nb.HasIndex(n => n.Value).IsUnique().HasDatabaseName("ix_sales_sale_number");
        });

        builder.Property(s => s.SaleDate).HasColumnName("sale_date").IsRequired();
        builder.Property(s => s.IsCancelled).HasColumnName("is_cancelled").HasDefaultValue(false);

        builder.OwnsOne(s => s.Customer, cb =>
        {
            cb.Property(c => c.Id).HasColumnName("customer_id").IsRequired();
            cb.Property(c => c.Name).HasColumnName("customer_name").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(s => s.Branch, bb =>
        {
            bb.Property(b => b.Id).HasColumnName("branch_id").IsRequired();
            bb.Property(b => b.Name).HasColumnName("branch_name").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(s => s.TotalAmount, mb =>
        {
            mb.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
            mb.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey("sale_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
