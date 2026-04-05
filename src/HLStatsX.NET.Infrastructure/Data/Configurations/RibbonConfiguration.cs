using HLStatsX.NET.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HLStatsX.NET.Infrastructure.Data.Configurations;

public class RibbonConfiguration : IEntityTypeConfiguration<Ribbon>
{
    public void Configure(EntityTypeBuilder<Ribbon> builder)
    {
        builder.ToTable("hlstats_Ribbons");
        builder.HasKey(r => r.RibbonId);
        builder.Property(r => r.RibbonId).HasColumnName("ribbonId");
        builder.Property(r => r.AwardCode).HasColumnName("awardCode").HasMaxLength(50);
        builder.Property(r => r.AwardCount).HasColumnName("awardCount");
        builder.Property(r => r.Special).HasColumnName("special");
        builder.Property(r => r.Game).HasColumnName("game").HasMaxLength(32).IsRequired();
        builder.Property(r => r.Image).HasColumnName("image").HasMaxLength(50);
        builder.Property(r => r.RibbonName).HasColumnName("ribbonName").HasMaxLength(50).IsRequired();
    }
}

public class RibbonTriggerConfiguration : IEntityTypeConfiguration<RibbonTrigger>
{
    public void Configure(EntityTypeBuilder<RibbonTrigger> builder)
    {
        builder.ToTable("hlstats_Ribbons_Triggers");
        builder.HasKey(t => t.TriggerId);
        builder.Property(t => t.TriggerId).HasColumnName("triggerId");
        builder.Property(t => t.RibbonId).HasColumnName("ribbonId");
        builder.Property(t => t.TriggerType).HasColumnName("type").HasMaxLength(32);
        builder.Property(t => t.TriggerCode).HasColumnName("code").HasMaxLength(64);
        builder.Property(t => t.TriggerValue).HasColumnName("value");

        builder.HasOne(t => t.Ribbon).WithMany().HasForeignKey(t => t.RibbonId);
    }
}
