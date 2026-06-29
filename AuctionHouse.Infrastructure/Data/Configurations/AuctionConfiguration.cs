using AuctionHouse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionHouse.Infrastructure.Data.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        // SQLite decimal'i native desteklemez; sabit precision için TEXT olarak saklarız.
        builder.Property(a => a.StartPrice).HasConversion<double>();
        builder.Property(a => a.CurrentPrice).HasConversion<double>();
        builder.Property(a => a.MinIncrement).HasConversion<double>();

        // Optimistic concurrency: SQLite RowVersion'ı otomatik üretmediği için
        // bunu concurrency token olarak işaretleyip her SaveChanges'te elle güncelleyeceğiz
        // (BidService içinde). IsRowVersion yerine IsConcurrencyToken kullanıyoruz.
        builder.Property(a => a.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne(a => a.Seller)
            .WithMany(u => u.Auctions)
            .HasForeignKey(a => a.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Auctions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Winner)
            .WithMany()
            .HasForeignKey(a => a.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.EndTime);
    }
}
