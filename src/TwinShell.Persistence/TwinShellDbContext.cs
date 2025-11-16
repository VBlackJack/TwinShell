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
    public DbSet<CommandHistoryEntity> CommandHistories => Set<CommandHistoryEntity>();
    public DbSet<UserFavoriteEntity> UserFavorites => Set<UserFavoriteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ActionConfiguration());
        modelBuilder.ApplyConfiguration(new CommandTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new CommandHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new UserFavoriteConfiguration());
    }
}
