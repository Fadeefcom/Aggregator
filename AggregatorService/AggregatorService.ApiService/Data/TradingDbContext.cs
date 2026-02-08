using Microsoft.EntityFrameworkCore;

namespace AggregatorService.ApiService.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Tick> Ticks { get; set; }
    public DbSet<Candle> Candles { get; set; }
    public DbSet<Instrument> Instruments { get; set; }
    public DbSet<SourceStatus> SourceStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tick>()
            .HasIndex(t => new { t.Symbol, t.Timestamp });

        modelBuilder.Entity<Candle>()
            .HasIndex(c => new { c.Symbol, c.Period, c.OpenTime });

        modelBuilder.Entity<Instrument>().HasData(
            new Instrument { Symbol = "BTCUSD", BaseCurrency = "BTC", QuoteCurrency = "USD" },
            new Instrument { Symbol = "ETHUSD", BaseCurrency = "ETH", QuoteCurrency = "USD" },
            new Instrument { Symbol = "SOLUSD", BaseCurrency = "SOL", QuoteCurrency = "USD" }
        );
    }
}