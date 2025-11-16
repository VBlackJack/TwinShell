using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence.Configurations;

public class CommandTemplateConfiguration : IEntityTypeConfiguration<CommandTemplateEntity>
{
    public void Configure(EntityTypeBuilder<CommandTemplateEntity> builder)
    {
        builder.ToTable("CommandTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.CommandPattern)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Platform)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ParametersJson)
            .IsRequired()
            .HasColumnType("TEXT");
    }
}
