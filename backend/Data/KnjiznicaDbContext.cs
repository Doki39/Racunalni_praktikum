using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Data;

public class KnjiznicaDbContext : DbContext
{
    public KnjiznicaDbContext(DbContextOptions<KnjiznicaDbContext> options)
        : base(options) { }

    public DbSet<Knjiznica> Knjiznice { get; set; }
    public DbSet<Knjige> Knjige { get; set; }
    public DbSet<KnjiznicaKnjige> KnjiznicaKnjige { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnjiznicaKnjige>()
            .HasOne(kk => kk.Knjiznica)
            .WithMany(k => k.KnjiznicaKnjige)
            .HasForeignKey(kk => kk.KnjiznicaId);

        modelBuilder.Entity<KnjiznicaKnjige>()
            .HasOne(kk => kk.Knjiga)
            .WithMany(k => k.KnjiznicaKnjige)
            .HasForeignKey(kk => kk.KnjigaId);

        modelBuilder.Entity<KnjiznicaKnjige>()
            .HasIndex(kk => new { kk.KnjiznicaId, kk.KnjigaId })
            .IsUnique();
    }
}
