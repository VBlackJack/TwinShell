using Microsoft.EntityFrameworkCore;
using TwinShell.Persistence.Configurations;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence;

/// <summary>
/// Database context for TwinShell
/// </summary>
public class TwinShellDbContext : DbContext
{
    public TwinShellDbContext(DbContextOptions<TwinShellDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActionEntity> Actions => Set<ActionEntity>();
    public DbSet<CommandTemplateEntity> CommandTemplates => Set<CommandTemplateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ActionConfiguration());
        modelBuilder.ApplyConfiguration(new CommandTemplateConfiguration());
    }
}
