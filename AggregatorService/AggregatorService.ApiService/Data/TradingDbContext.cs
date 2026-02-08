using Microsoft.EntityFrameworkCore;

namespace AggregatorService.ApiService.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Tick> Ticks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tick>().HasIndex(t => t.Timestamp);
        modelBuilder.Entity<Tick>().HasIndex(t => t.Symbol);
    }
}
