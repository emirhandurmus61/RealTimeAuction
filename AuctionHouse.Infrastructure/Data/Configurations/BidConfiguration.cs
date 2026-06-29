using AuctionHouse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionHouse.Infrastructure.Data.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.Property(b => b.Amount).HasConversion<double>();

        builder.HasOne(b => b.Auction)
            .WithMany(a => a.Bids)
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Bidder)
            .WithMany(u => u.Bids)
            .HasForeignKey(b => b.BidderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.AuctionId);
    }
}
