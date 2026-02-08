using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
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
        base.OnModelCreating(modelBuilder);

        // --- Tick ---
        modelBuilder.Entity<Tick>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Symbol)
                .HasConversion(s => s.Value, v => Symbol.Create(v))
                .HasMaxLength(20).IsRequired();
            builder.HasIndex(t => new { t.Symbol, t.Timestamp });
        });

        // --- Candle ---
        modelBuilder.Entity<Candle>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Symbol)
                .HasConversion(s => s.Value, v => Symbol.Create(v))
                .HasMaxLength(20).IsRequired();
            builder.HasIndex(c => new { c.Symbol, c.Period, c.OpenTime });
        });

        // --- Instrument ---
        modelBuilder.Entity<Instrument>(builder =>
        {
            builder.HasKey(i => i.Symbol);

            builder.Property(i => i.Symbol)
                .HasConversion(s => s.Value, v => Symbol.Create(v))
                .HasMaxLength(20)
                .IsRequired();
            
            // Seed data
            builder.HasData(
                new { Symbol = Symbol.Create("BTCUSD"), BaseCurrency = "BTC", QuoteCurrency = "USD", Type = "Crypto", IsActive = true },
                new { Symbol = Symbol.Create("ETHUSD"), BaseCurrency = "ETH", QuoteCurrency = "USD", Type = "Crypto", IsActive = true },
                new { Symbol = Symbol.Create("SOLUSD"), BaseCurrency = "SOL", QuoteCurrency = "USD", Type = "Crypto", IsActive = true }
            );
        });

        // --- SourceStatus ---
        modelBuilder.Entity<SourceStatus>(builder =>
        {
            builder.HasKey(s => s.SourceName);
            builder.Property(s => s.SourceName).HasMaxLength(50);
        });
    }
}