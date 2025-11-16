using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence.Configurations;

public class ActionConfiguration : IEntityTypeConfiguration<ActionEntity>
{
    public void Configure(EntityTypeBuilder<ActionEntity> builder)
    {
        builder.ToTable("Actions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Platform)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Level)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TagsJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.ExamplesJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.LinksJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.IsUserCreated)
            .IsRequired();

        // Relationships
        builder.HasOne(e => e.WindowsCommandTemplate)
            .WithMany()
            .HasForeignKey(e => e.WindowsCommandTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.LinuxCommandTemplate)
            .WithMany()
            .HasForeignKey(e => e.LinuxCommandTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for common queries
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Platform);
        builder.HasIndex(e => e.Level);
    }
}
