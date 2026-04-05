using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class AwardConfiguration : IEntityTypeConfiguration<Award>
{
    public void Configure(EntityTypeBuilder<Award> builder)
    {
        builder.ToTable("hlstats_Awards");
        builder.HasKey(a => a.AwardId);
        builder.Property(a => a.AwardId).HasColumnName("awardId");
        builder.Property(a => a.AwardType).HasColumnName("awardType").HasMaxLength(1);
        builder.Property(a => a.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(128);
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(128);
        builder.Property(a => a.Verb).HasColumnName("verb").HasMaxLength(128);
        builder.Property(a => a.DailyWinnerId).HasColumnName("d_winner_id");
        builder.Property(a => a.DailyWinnerCount).HasColumnName("d_winner_count");
        builder.Property(a => a.GlobalWinnerId).HasColumnName("g_winner_id");
        builder.Property(a => a.GlobalWinnerCount).HasColumnName("g_winner_count");

        builder.HasOne(a => a.DailyWinner)
            .WithMany()
            .HasForeignKey(a => a.DailyWinnerId)
            .IsRequired(false);

        builder.HasOne(a => a.GlobalWinner)
            .WithMany()
            .HasForeignKey(a => a.GlobalWinnerId)
            .IsRequired(false);
    }
}
