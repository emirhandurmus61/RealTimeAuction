using AuctionHouse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionHouse.Infrastructure.Data.Configurations;

public class AuctionImageConfiguration : IEntityTypeConfiguration<AuctionImage>
{
    public void Configure(EntityTypeBuilder<AuctionImage> builder)
    {
        builder.Property(i => i.Url)
            .IsRequired()
            .HasMaxLength(400);

        builder.HasOne(i => i.Auction)
            .WithMany(a => a.Images)
            .HasForeignKey(i => i.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.AuctionId);
    }
}
