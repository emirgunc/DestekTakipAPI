using DestekTakipAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DestekTakipAPI.Infrastructure.Persistence;

/// <summary>
/// Uygulamanin EF Core veritabani baglam sinifidir.
/// Tum DbSet'leri, Fluent API konfigurasyonlarini ve model iliski tanimlamalarini icerir.
/// </summary>
public class DestekTakipDbContext : DbContext
{
    /// <summary>
    /// DestekTakipDbContext bagimlilik enjeksiyonu yapicisidir.
    /// </summary>
    public DestekTakipDbContext(DbContextOptions<DestekTakipDbContext> secenekler)
        : base(secenekler)
    {
    }

    // DbSet'ler — Her biri bir veritabani tablosuna karsilik gelir
    public DbSet<Rol> Roller { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<Kategori> Kategoriler { get; set; }
    public DbSet<TalepOnceligi> TalepOncelikleri { get; set; }
    public DbSet<TalepDurumu> TalepDurumlari { get; set; }
    public DbSet<DestekTalebi> DestekTalepleri { get; set; }
    public DbSet<TalepYorumu> TalepYorumlari { get; set; }
    public DbSet<TalepDegisiklikGecmisi> TalepDegisiklikGecmisleri { get; set; }
    public DbSet<TalepEki> TalepEkleri { get; set; }
    public DbSet<SirketAyarlari> SirketAyarlari { get; set; }

    /// <summary>
    /// Fluent API ile model konfigurasyonlarini tanimlar.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelOlusturucu)
    {
        base.OnModelCreating(modelOlusturucu);

        // -----------------------------------------------------------------------
        // Rol
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<Rol>(varlik =>
        {
            varlik.ToTable("Roller");
            varlik.HasKey(r => r.RolId);
            varlik.Property(r => r.RolAdi).IsRequired().HasMaxLength(50);
            varlik.Property(r => r.OlusturmaTarihi).HasDefaultValueSql("GETDATE()");
        });

        // -----------------------------------------------------------------------
        // Kullanici
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<Kullanici>(varlik =>
        {
            varlik.ToTable("Kullanicilar");
            varlik.HasKey(k => k.KullaniciId);
            varlik.Property(k => k.Ad).IsRequired().HasMaxLength(100);
            varlik.Property(k => k.Soyad).IsRequired().HasMaxLength(100);
            varlik.Property(k => k.Eposta).IsRequired().HasMaxLength(255);
            varlik.HasIndex(k => k.Eposta).IsUnique();
            varlik.Property(k => k.SifreHash).IsRequired().HasMaxLength(255);
            varlik.Property(k => k.OlusturmaTarihi).HasDefaultValueSql("GETDATE()");

            varlik.HasOne(k => k.Rol)
                  .WithMany(r => r.Kullanicilar)
                  .HasForeignKey(k => k.RolId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // -----------------------------------------------------------------------
        // Kategori
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<Kategori>(varlik =>
        {
            varlik.ToTable("Kategoriler");
            varlik.HasKey(k => k.KategoriId);
            varlik.Property(k => k.KategoriAdi).IsRequired().HasMaxLength(100);
            varlik.HasIndex(k => k.KategoriAdi).IsUnique();
        });

        // -----------------------------------------------------------------------
        // TalepOnceligi
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<TalepOnceligi>(varlik =>
        {
            varlik.ToTable("TalepOncelikleri");
            varlik.HasKey(o => o.OncelikId);
            varlik.Property(o => o.OncelikAdi).IsRequired().HasMaxLength(50);
        });

        // -----------------------------------------------------------------------
        // TalepDurumu
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<TalepDurumu>(varlik =>
        {
            varlik.ToTable("TalepDurumlari");
            varlik.HasKey(d => d.DurumId);
            varlik.Property(d => d.DurumAdi).IsRequired().HasMaxLength(50);
        });

        // -----------------------------------------------------------------------
        // DestekTalebi — cift FK nedeniyle ozel konfigürasyon gerektirir
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<DestekTalebi>(varlik =>
        {
            varlik.ToTable("DestekTalepleri");
            varlik.HasKey(dt => dt.TalepId);
            varlik.Property(dt => dt.Baslik).IsRequired().HasMaxLength(500);
            varlik.Property(dt => dt.Aciklama).IsRequired();
            varlik.Property(dt => dt.OlusturmaTarihi).HasDefaultValueSql("GETDATE()");

            // FK 1: OlusturanKullanici — DELETE yasak (Restrict)
            varlik.HasOne(dt => dt.OlusturanKullanici)
                  .WithMany(k => k.OlusturulanTalepler)
                  .HasForeignKey(dt => dt.OlusturanKullaniciId)
                  .OnDelete(DeleteBehavior.Restrict);

            // FK 2: AtananKullanici — DELETE hic etkilemesin (NoAction)
            varlik.HasOne(dt => dt.AtananKullanici)
                  .WithMany(k => k.AtananTalepler)
                  .HasForeignKey(dt => dt.AtananKullaniciId)
                  .OnDelete(DeleteBehavior.NoAction);

            varlik.HasOne(dt => dt.Kategori)
                  .WithMany(k => k.DestekTalepleri)
                  .HasForeignKey(dt => dt.KategoriId)
                  .OnDelete(DeleteBehavior.Restrict);

            varlik.HasOne(dt => dt.Oncelik)
                  .WithMany(o => o.DestekTalepleri)
                  .HasForeignKey(dt => dt.OncelikId)
                  .OnDelete(DeleteBehavior.Restrict);

            varlik.HasOne(dt => dt.Durum)
                  .WithMany(d => d.DestekTalepleri)
                  .HasForeignKey(dt => dt.DurumId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // -----------------------------------------------------------------------
        // TalepYorumu
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<TalepYorumu>(varlik =>
        {
            varlik.ToTable("TalepYorumlari");
            varlik.HasKey(y => y.YorumId);
            varlik.Property(y => y.YorumMetni).IsRequired();
            varlik.Property(y => y.OlusturmaTarihi).HasDefaultValueSql("GETDATE()");

            varlik.HasOne(y => y.DestekTalebi)
                  .WithMany(dt => dt.Yorumlar)
                  .HasForeignKey(y => y.TalepId)
                  .OnDelete(DeleteBehavior.Cascade);

            varlik.HasOne(y => y.YazanKullanici)
                  .WithMany(k => k.Yorumlar)
                  .HasForeignKey(y => y.YazanKullaniciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // -----------------------------------------------------------------------
        // TalepDegisiklikGecmisi — Salt okunur; Trigger tarafindan doldurulur
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<TalepDegisiklikGecmisi>(varlik =>
        {
            varlik.ToTable("TalepDegisiklikGecmisi");
            varlik.HasKey(g => g.GecmisId);
            varlik.Property(g => g.DegistirmeTarihi).HasDefaultValueSql("GETDATE()");

            varlik.HasOne(g => g.DestekTalebi)
                  .WithMany(dt => dt.DegisiklikGecmisi)
                  .HasForeignKey(g => g.TalepId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        // -----------------------------------------------------------------------
        // TalepEki
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<TalepEki>(varlik =>
        {
            varlik.ToTable("TalepEkleri");
            varlik.HasKey(e => e.EkId);
            varlik.Property(e => e.DosyaAdi).IsRequired().HasMaxLength(255);
            varlik.Property(e => e.SaklananDosyaAdi).IsRequired().HasMaxLength(255);
            varlik.Property(e => e.DosyaUzantisi).IsRequired().HasMaxLength(20);
            varlik.Property(e => e.IcerikTuru).IsRequired().HasMaxLength(100);
            varlik.Property(e => e.YuklenmeTarihi).HasDefaultValueSql("GETDATE()");

            varlik.HasOne(e => e.DestekTalebi)
                  .WithMany()
                  .HasForeignKey(e => e.TalepId)
                  .OnDelete(DeleteBehavior.Cascade);

            varlik.HasOne(e => e.YukleyenKullanici)
                  .WithMany()
                  .HasForeignKey(e => e.YukleyenKullaniciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // -----------------------------------------------------------------------
        // SirketAyarlari
        // -----------------------------------------------------------------------
        modelOlusturucu.Entity<SirketAyarlari>(varlik =>
        {
            varlik.ToTable("SirketAyarlari");
            varlik.HasKey(s => s.AyarId);
            varlik.Property(s => s.SirketAdi).IsRequired().HasMaxLength(150);
            varlik.Property(s => s.SistemBasligi).IsRequired().HasMaxLength(150);
            varlik.Property(s => s.DestekEposta).HasMaxLength(150);
            varlik.Property(s => s.DestekTelefon).HasMaxLength(50);
            varlik.Property(s => s.WebSitesi).HasMaxLength(200);
            varlik.Property(s => s.AltBilgiMetni).HasMaxLength(250);
            varlik.Property(s => s.LogoUrl).HasMaxLength(500);
        });
    }
}
