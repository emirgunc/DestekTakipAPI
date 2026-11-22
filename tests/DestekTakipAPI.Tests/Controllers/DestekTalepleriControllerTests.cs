using DestekTakipAPI.Infrastructure.Services;
using System.Security.Claims;
using DestekTakipAPI.API.Controllers;
using DestekTakipAPI.Application.DTOs.DestekTalebi;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DestekTakipAPI.Tests.Controllers;

/// <summary>
/// DestekTalepleriController unit test sinifi.
/// EF Core InMemory veritabani kullanilarak SQL bagimliligi ortadan kaldirilir.
/// Her test izole bir veritabani ornegi kullanir.
/// </summary>
public class DestekTalepleriControllerTests
{
    private static DestekTakipDbContext InMemoryVeritabaniOlustur()
    {
        var opt = new DbContextOptionsBuilder<DestekTakipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DestekTakipDbContext(opt);
    }

    private static async Task TemelVerileriEkleAsync(DestekTakipDbContext db)
    {
        db.Roller.AddRange(
            new Rol { RolId = 1, RolAdi = "Kullanici", OlusturmaTarihi = DateTime.UtcNow },
            new Rol { RolId = 2, RolAdi = "Yonetici", OlusturmaTarihi = DateTime.UtcNow }
        );

        db.Kullanicilar.AddRange(
            new Kullanici
            {
                KullaniciId = 1,
                Ad = "Ali",
                Soyad = "Yilmaz",
                Eposta = "ali@test.com",
                SifreHash = "HASH",
                RolId = 1,
                AktifMi = true,
                OlusturmaTarihi = DateTime.UtcNow
            },
            new Kullanici
            {
                KullaniciId = 2,
                Ad = "Mehmet",
                Soyad = "Demir",
                Eposta = "mehmet@test.com",
                SifreHash = "HASH",
                RolId = 2,
                AktifMi = true,
                OlusturmaTarihi = DateTime.UtcNow
            },
            new Kullanici
            {
                KullaniciId = 3,
                Ad = "Ayse",
                Soyad = "Kaya",
                Eposta = "ayse@test.com",
                SifreHash = "HASH",
                RolId = 1,
                AktifMi = true,
                OlusturmaTarihi = DateTime.UtcNow
            }
        );

        db.Kategoriler.Add(
            new Kategori { KategoriId = 1, KategoriAdi = "Yazilim", AktifMi = true }
        );

        db.TalepOncelikleri.Add(
            new TalepOnceligi { OncelikId = 1, OncelikAdi = "Orta", SiraNo = 2 }
        );

        db.TalepDurumlari.AddRange(
            new TalepDurumu { DurumId = 1, DurumAdi = "Acik", Aciklama = "Acik talep." },
            new TalepDurumu { DurumId = 2, DurumAdi = "Isleniyor", Aciklama = "Isleniyor." },
            new TalepDurumu { DurumId = 5, DurumAdi = "Kapatildi", Aciklama = "Kapali." }
        );

        await db.SaveChangesAsync();
    }

    private static async Task TestTalepleriniEkleAsync(DestekTakipDbContext db)
    {
        db.DestekTalepleri.AddRange(
            new DestekTalebi
            {
                TalepId = 1,
                Baslik = "Yazici calismiyor",
                Aciklama = "Test aciklama 1.",
                OlusturanKullaniciId = 1,
                KategoriId = 1,
                OncelikId = 1,
                DurumId = 1,
                OlusturmaTarihi = DateTime.UtcNow.AddDays(-3)
            },
            new DestekTalebi
            {
                TalepId = 2,
                Baslik = "Yazilim guncelleme",
                Aciklama = "Test aciklama 2.",
                OlusturanKullaniciId = 3,
                AtananKullaniciId = 2,
                KategoriId = 1,
                OncelikId = 1,
                DurumId = 2,
                OlusturmaTarihi = DateTime.UtcNow.AddDays(-1)
            },
            new DestekTalebi
            {
                TalepId = 3,
                Baslik = "Kapali talep",
                Aciklama = "Bu talep kapandi.",
                OlusturanKullaniciId = 1,
                KategoriId = 1,
                OncelikId = 1,
                DurumId = 5,
                OlusturmaTarihi = DateTime.UtcNow.AddDays(-10),
                KapanisTarihi = DateTime.UtcNow.AddDays(-5)
            }
        );

        await db.SaveChangesAsync();
    }

    public class SahteEpostaServisi : IEpostaServisi
    {
        public List<(string Alici, string Konu)> GonderilenEpostalar { get; } = new();

        public Task TalepOlusturulduBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik)
        {
            GonderilenEpostalar.Add((aliciEposta, $"Olusturuldu:{talepId}:{baslik}"));
            return Task.CompletedTask;
        }

        public Task TalepDurumuGuncellendiBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yeniDurum)
        {
            GonderilenEpostalar.Add((aliciEposta, $"DurumGuncellendi:{talepId}:{yeniDurum}"));
            return Task.CompletedTask;
        }

        public Task YeniYorumBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yazanAd, string yorumMetni)
        {
            GonderilenEpostalar.Add((aliciEposta, $"YeniYorum:{talepId}:{yorumMetni}"));
            return Task.CompletedTask;
        }
    }

    private static DestekTalepleriController ControllerOlustur(
        DestekTakipDbContext db,
        int kullaniciId,
        string rolAdi,
        IEpostaServisi? epostaServisi = null)
    {
        var claimler = new List<Claim>
        {
            new Claim("KullaniciId", kullaniciId.ToString()),
            new Claim(ClaimTypes.Role, rolAdi)
        };

        var kimlik = new ClaimsIdentity(claimler, "TestAuth");
        var kullanici = new ClaimsPrincipal(kimlik);

        var httpBaglam = new DefaultHttpContext { User = kullanici };

        var ctrl = new DestekTalepleriController(
            db,
            epostaServisi ?? new SahteEpostaServisi(),
            NullLogger<DestekTalepleriController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpBaglam }
        };

        return ctrl;
    }

    // =========================================================================
    // GET /api/destektalepleri
    // =========================================================================

    [Fact]
    public async Task TumTalepleriGetir_YoneticiRolunde_TumAktifTalepleriDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici");
        var sonuc = await ctrl.TumTalepleriGetir();

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var liste = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.Equal(2, liste.Cast<object>().Count());
    }

    [Fact]
    public async Task TumTalepleriGetir_KullaniciRolunde_SadeceKendiTalepleriniDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var sonuc = await ctrl.TumTalepleriGetir();

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var liste = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.Single(liste.Cast<object>());
    }

    // =========================================================================
    // GET /api/destektalepleri/{id}
    // =========================================================================

    [Fact]
    public async Task TalepDetayiGetir_GecerliId_YoneticiRolunde_OkDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici");
        var sonuc = await ctrl.TalepDetayiGetir(id: 1);

        Assert.IsType<OkObjectResult>(sonuc);
    }

    [Fact]
    public async Task TalepDetayiGetir_KullaniciRolunde_BaskasininTalebi_ForbiddenDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        // Ayse (3) Ali'nin (1) talebine bakmaya calisiyor
        var ctrl = ControllerOlustur(db, kullaniciId: 3, rolAdi: "Kullanici");
        var sonuc = await ctrl.TalepDetayiGetir(id: 1);

        var obj = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    // =========================================================================
    // POST /api/destektalepleri (Talep Olusturma)
    // =========================================================================

    [Fact]
    public async Task TalepOlustur_GecerliModel_CreatedAtActionDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var dto = new DestekTalebiOlusturDto
        {
            Baslik = "Yeni Sistem Hatasi",
            Aciklama = "Ekranda 500 hatasi gorunuyor.",
            KategoriId = 1,
            OncelikId = 1
        };

        var sonuc = await ctrl.TalepOlustur(dto);

        Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Equal(1, await db.DestekTalepleri.CountAsync());
    }

    // =========================================================================
    // PUT /api/destektalepleri/{id} (Talep Guncelleme)
    // =========================================================================

    [Fact]
    public async Task TalepGuncelle_YoneticiRolunde_DurumGuncellenir()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici");
        var guncelleDto = new DestekTalebiGuncelleDto
        {
            DurumId = 2
        };

        var sonuc = await ctrl.TalepGuncelle(1, guncelleDto);

        Assert.IsType<OkObjectResult>(sonuc);
        var talep = await db.DestekTalepleri.FindAsync(1);
        Assert.Equal(2, talep?.DurumId);
    }

    [Fact]
    public async Task TalepGuncelle_KullaniciRolunde_DurumDegistirmeyeCalisirsa_ForbiddenDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var guncelleDto = new DestekTalebiGuncelleDto { DurumId = 2 };

        var sonuc = await ctrl.TalepGuncelle(1, guncelleDto);

        var obj = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    // =========================================================================
    // POST & GET /api/destektalepleri/{id}/yorumlar (Yorum Islemleri)
    // =========================================================================

    [Fact]
    public async Task YorumEkle_GecerliTalepVeKullanici_CreatedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var yorumDto = new TalepYorumuOlusturDto
        {
            YorumMetni = "Yazici kablosunu kontrol ettim hala ayni."
        };

        var sonuc = await ctrl.YorumEkle(1, yorumDto);

        Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Equal(1, await db.TalepYorumlari.CountAsync());
    }

    [Fact]
    public async Task YorumEkle_KapaliTalep_BadRequestDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var yorumDto = new TalepYorumuOlusturDto { YorumMetni = "Tekrar dener misiniz?" };

        var sonuc = await ctrl.YorumEkle(3, yorumDto); // Talep 3 kapali

        Assert.IsType<BadRequestObjectResult>(sonuc);
    }

    [Fact]
    public async Task YorumlariGetir_GecerliTalep_YorumListesiDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        db.TalepYorumlari.Add(new TalepYorumu
        {
            TalepId = 1,
            YazanKullaniciId = 1,
            YorumMetni = "Ornek yorum",
            OlusturmaTarihi = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var sonuc = await ctrl.YorumlariGetir(1);

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var liste = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.Single(liste.Cast<object>());
    }

    // =========================================================================
    // GET /api/destektalepleri/istatistikler (SQL View & Function Destegi)
    // =========================================================================

    [Fact]
    public async Task IstatistikleriGetir_MetrikleriHesaplarVeOkDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");
        var sonuc = await ctrl.IstatistikleriGetir();

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var stat = Assert.IsType<TalepIstatistikDto>(ok.Value);

        Assert.Equal(3, stat.ToplamTalepSayisi);
        Assert.Equal(2, stat.AktifTalepSayisi);
        Assert.Equal(1, stat.KullanicininAktifTalepSayisi);
    }

    // =========================================================================
    // DELETE /api/destektalepleri/{id} (Talep Kapatma)
    // =========================================================================

    [Fact]
    public async Task TalepKapat_AktifTalep_YoneticiRolunde_NoContentVeDurumGuncellenir()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici");
        var sonuc = await ctrl.TalepKapat(id: 1);

        Assert.IsType<NoContentResult>(sonuc);
        var talep = await db.DestekTalepleri.FindAsync(1);
        Assert.Equal(5, talep?.DurumId); // 5 = Kapatildi
    }
    // =========================================================================
    // POST /api/destektalepleri/{id}/dosyalar (Dosya Yukleme)
    // =========================================================================

    [Fact]
    public async Task DosyaYukle_GecerliDosya_CreatedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");

        var dosyaIcerigi = "Test log metni";
        var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(dosyaIcerigi));
        var formFile = new FormFile(stream, 0, stream.Length, "dosya", "hata.log")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var sonuc = await ctrl.DosyaYukle(1, formFile);

        var created = Assert.IsType<CreatedResult>(sonuc);
        var dto = Assert.IsType<TalepEkiDto>(created.Value);
        Assert.Equal("hata.log", dto.DosyaAdi);
        Assert.Equal(".log", dto.DosyaUzantisi);
    }

    [Fact]
    public async Task DosyaYukle_GecersizUzanti_BadRequestDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici");

        var stream = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
        var formFile = new FormFile(stream, 0, stream.Length, "dosya", "zararli.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/x-msdownload"
        };

        var sonuc = await ctrl.DosyaYukle(1, formFile);

        Assert.IsType<BadRequestObjectResult>(sonuc);
    }
    // =========================================================================
    // GET /api/destektalepleri/disa-aktar (Excel / CSV Export)
    // =========================================================================

    [Fact]
    public async Task TalepleriDisaAktar_YoneticiRolunde_CsvDosyasiDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici");

        var sonuc = await ctrl.TalepleriDisaAktar();

        var fileResult = Assert.IsType<FileContentResult>(sonuc);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.StartsWith("DestekTalepleri_Rapor_", fileResult.FileDownloadName);
        Assert.True(fileResult.FileContents.Length > 0);
    }

    // =========================================================================
    // E-POSTA BILDIRIM TESTLERI
    // =========================================================================

    [Fact]
    public async Task TalepOlustur_GecerliVeriyle_EpostaBildirimiTetiklenir()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);

        var sahteEposta = new SahteEpostaServisi();
        var ctrl = ControllerOlustur(db, kullaniciId: 1, rolAdi: "Kullanici", epostaServisi: sahteEposta);

        var dto = new DestekTalebiOlusturDto
        {
            Baslik = "E-posta Test Talebi",
            Aciklama = "E-posta bildirimi tetiklenme testi aciklamasi",
            KategoriId = 1,
            OncelikId = 1
        };

        var sonuc = await ctrl.TalepOlustur(dto);

        Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Single(sahteEposta.GonderilenEpostalar);
        Assert.Equal("ali@test.com", sahteEposta.GonderilenEpostalar[0].Alici);
        Assert.Contains("Olusturuldu:", sahteEposta.GonderilenEpostalar[0].Konu);
    }

    [Fact]
    public async Task TalepGuncelle_DurumDegistiginde_EpostaBildirimiTetiklenir()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var sahteEposta = new SahteEpostaServisi();
        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici", epostaServisi: sahteEposta);

        // Talep 1 durumu Acik (1) -> Isleniyor (2) yapilsin
        var guncelleDto = new DestekTalebiGuncelleDto
        {
            DurumId = 2
        };

        var sonuc = await ctrl.TalepGuncelle(1, guncelleDto);

        Assert.IsType<OkObjectResult>(sonuc);
        Assert.Single(sahteEposta.GonderilenEpostalar);
        Assert.Equal("ali@test.com", sahteEposta.GonderilenEpostalar[0].Alici);
        Assert.Contains("DurumGuncellendi:1:Isleniyor", sahteEposta.GonderilenEpostalar[0].Konu);
    }

    [Fact]
    public async Task YorumEkle_YoneticiYorumYazdiginda_TalepSahibineEpostaGider()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TemelVerileriEkleAsync(db);
        await TestTalepleriniEkleAsync(db);

        var sahteEposta = new SahteEpostaServisi();
        // Yonetici (kullaniciId: 2), Ali'nin (kullaniciId: 1) talebi olan 1'e yorum yaziyor
        var ctrl = ControllerOlustur(db, kullaniciId: 2, rolAdi: "Yonetici", epostaServisi: sahteEposta);

        var yorumDto = new TalepYorumuOlusturDto
        {
            YorumMetni = "Talebiniz inceleniyor, lutfen bekleyiniz.",
            DahiliMi = false
        };

        var sonuc = await ctrl.YorumEkle(1, yorumDto);

        Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Single(sahteEposta.GonderilenEpostalar);
        Assert.Equal("ali@test.com", sahteEposta.GonderilenEpostalar[0].Alici);
        Assert.Contains("YeniYorum:1:Talebiniz inceleniyor", sahteEposta.GonderilenEpostalar[0].Konu);
    }
}
