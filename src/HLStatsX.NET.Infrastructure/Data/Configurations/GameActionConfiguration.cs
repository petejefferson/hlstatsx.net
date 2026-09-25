using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class GameActionConfiguration : IEntityTypeConfiguration<GameAction>
{
    public void Configure(EntityTypeBuilder<GameAction> builder)
    {
        builder.ToTable("hlstats_Actions");
        builder.HasKey(a => a.ActionId);
        builder.Property(a => a.ActionId).HasColumnName("id");
        builder.Property(a => a.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(128);
        builder.Property(a => a.Team).HasColumnName("team").HasMaxLength(32);
        builder.Property(a => a.RewardPlayer).HasColumnName("reward_player");
        builder.Property(a => a.RewardTeam).HasColumnName("reward_team");
        builder.Property(a => a.Count).HasColumnName("count");
        builder.Property(a => a.ForPlayerActions).HasColumnName("for_PlayerActions")
            .HasConversion(v => v ? "1" : "0", v => v == "1");
        builder.Property(a => a.ForPlayerPlayerActions).HasColumnName("for_PlayerPlayerActions")
            .HasConversion(v => v ? "1" : "0", v => v == "1");
        builder.Property(a => a.ForTeamActions).HasColumnName("for_TeamActions")
            .HasConversion(v => v ? "1" : "0", v => v == "1");
        builder.Property(a => a.ForWorldActions).HasColumnName("for_WorldActions")
            .HasConversion(v => v ? "1" : "0", v => v == "1");

        builder.HasOne(a => a.GameNavigation)
            .WithMany(g => g.Actions)
            .HasForeignKey(a => a.Game)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
