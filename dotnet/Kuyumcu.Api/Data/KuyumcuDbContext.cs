using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Data;

public class KuyumcuDbContext(DbContextOptions<KuyumcuDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Sube> Subeler => Set<Sube>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<RefreshToken> RefreshTokenler => Set<RefreshToken>();
    public DbSet<KurSnapshot> KurSnapshots => Set<KurSnapshot>();
    public DbSet<Cari> Cariler => Set<Cari>();
    public DbSet<CariBakiye> CariBakiyeler => Set<CariBakiye>();
    public DbSet<Urun> Urunler => Set<Urun>();
    public DbSet<Stok> Stoklar => Set<Stok>();
    public DbSet<Hareket> Hareketler => Set<Hareket>();
    public DbSet<Kasa> Kasalar => Set<Kasa>();
    public DbSet<KasaHareket> KasaHareketleri => Set<KasaHareket>();
    public DbSet<BankaHesap> BankaHesaplari => Set<BankaHesap>();
    public DbSet<BankaHareket> BankaHareketleri => Set<BankaHareket>();
    public DbSet<Fatura> Faturalar => Set<Fatura>();
    public DbSet<RolYetki> RolYetkileri => Set<RolYetki>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Kullanici>().HasIndex(k => new { k.Email, k.TenantId }).IsUnique();
        mb.Entity<Fatura>().HasIndex(f => new { f.FaturaNo, f.TenantId }).IsUnique();
        mb.Entity<RolYetki>().HasIndex(y => new { y.TenantId, y.Rol, y.Modul }).IsUnique();
    }
}
