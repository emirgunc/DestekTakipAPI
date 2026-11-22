using DestekTakipAPI.Infrastructure.Services;
using System.Text;
using System.Security.Claims;
using DestekTakipAPI.Application.DTOs.DestekTalebi;
using DestekTakipAPI.Application.DTOs.Kategori;
using DestekTakipAPI.Application.DTOs.Shared;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DestekTakipAPI.API.Controllers;

/// <summary>
/// Destek taleplerini ve iliskili yorum/tarihce islemlerini yoneten ana API controller'i.
/// GET/POST: Giris yapmis tum kullanicilar.
/// DELETE ve yonetici alanlari: Sadece Yonetici rolu.
/// Route: api/destektalepleri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class DestekTalepleriController : ControllerBase
{
    private readonly DestekTakipDbContext _veritabani;
    private readonly IEpostaServisi _epostaServisi;
    private readonly ILogger<DestekTalepleriController> _logger;

    public DestekTalepleriController(
        DestekTakipDbContext veritabani,
        IEpostaServisi epostaServisi,
        ILogger<DestekTalepleriController> logger)
    {
        _veritabani = veritabani;
        _epostaServisi = epostaServisi;
        _logger = logger;
    }

    // =========================================================================
    // YARDIMCI METOTLAR
    // =========================================================================

    private int? OturumAcanKullaniciId()
    {
        string? deger = User.FindFirstValue("KullaniciId");
        return int.TryParse(deger, out int id) ? id : null;
    }

    private string? OturumAcanKullaniciRolu()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    
    private static (DateTime hedefTarih, int kalanSaat, bool asildi, string durumMetni) HesaplaSla(
        DateTime olusturmaTarihi, string oncelikAdi, string durumAdi)
    {
        int hedefSaat = oncelikAdi switch
        {
            "Kritik" => 4,
            "Yuksek" or "Yüksek" => 8,
            "Orta" => 24,
            _ => 48
        };

        DateTime hedefTarih = olusturmaTarihi.AddHours(hedefSaat);

        if (durumAdi is "Cozuldu" or "Çözüldü" or "Kapatildi" or "Kapatıldı")
        {
            return (hedefTarih, 0, false, "Tamamlandı");
        }

        TimeSpan fark = hedefTarih - DateTime.UtcNow;
        int kalanSaat = (int)Math.Round(fark.TotalHours);
        bool asildi = fark.TotalMinutes < 0;

        string durumMetni = asildi
            ? $"SLA Aşıldı ({Math.Abs(kalanSaat)} sa. gecikti)"
            : (kalanSaat <= 2 ? $"Kritik ({kalanSaat} sa. kaldı)" : $"{kalanSaat} sa. kaldı");

        return (hedefTarih, kalanSaat, asildi, durumMetni);
    }

    private bool IsYonetici()
    {
        return OturumAcanKullaniciRolu() == "Yonetici";
    }

    // =========================================================================
    // GET api/destektalepleri
    // =========================================================================

    /// <summary>
    /// Aktif destek taleplerini listeler.
    /// Yonetici hepsini, Kullanici sadece kendi actigi talepleri gorur.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DestekTalebiListeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TumTalepleriGetir(
        [FromQuery] int? durumId = null,
        [FromQuery] int? kategoriId = null,
        [FromQuery] int? oncelikId = null,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBoyutu = 20)
    {
        if (sayfa < 1) sayfa = 1;
        if (sayfaBoyutu > 100) sayfaBoyutu = 100;

        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        IQueryable<DestekTalebi> sorgu = _veritabani.DestekTalepleri
            .Include(dt => dt.OlusturanKullanici)
            .Include(dt => dt.AtananKullanici)
            .Include(dt => dt.Kategori)
            .Include(dt => dt.Oncelik)
            .Include(dt => dt.Durum)
            .Where(dt => dt.Durum.DurumAdi != "Kapatildi");

        if (!yonetici)
        {
            sorgu = sorgu.Where(dt => dt.OlusturanKullaniciId == aktifKullaniciId);
        }

        if (durumId.HasValue) sorgu = sorgu.Where(dt => dt.DurumId == durumId.Value);
        if (kategoriId.HasValue) sorgu = sorgu.Where(dt => dt.KategoriId == kategoriId.Value);
        if (oncelikId.HasValue) sorgu = sorgu.Where(dt => dt.OncelikId == oncelikId.Value);

        int toplamKayit = await sorgu.CountAsync();

        List<DestekTalebiListeDto> liste = await sorgu
            .OrderByDescending(dt => dt.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBoyutu)
            .Take(sayfaBoyutu)
            .Select(dt => new DestekTalebiListeDto
            {
                TalepId = dt.TalepId,
                Baslik = dt.Baslik,
                KategoriAdi = dt.Kategori.KategoriAdi,
                OlusturanKullanici = new KullaniciOzetDto
                {
                    KullaniciId = dt.OlusturanKullanici.KullaniciId,
                    AdSoyad = dt.OlusturanKullanici.Ad + " " + dt.OlusturanKullanici.Soyad,
                    Eposta = dt.OlusturanKullanici.Eposta
                },
                AtananKullanici = dt.AtananKullanici == null ? null : new KullaniciOzetDto
                {
                    KullaniciId = dt.AtananKullanici.KullaniciId,
                    AdSoyad = dt.AtananKullanici.Ad + " " + dt.AtananKullanici.Soyad,
                    Eposta = dt.AtananKullanici.Eposta
                },
                Oncelik = new TalepOnceligiDto
                {
                    OncelikId = dt.Oncelik.OncelikId,
                    OncelikAdi = dt.Oncelik.OncelikAdi,
                    SiraNo = dt.Oncelik.SiraNo
                },
                Durum = new TalepDurumuDto
                {
                    DurumId = dt.Durum.DurumId,
                    DurumAdi = dt.Durum.DurumAdi
                },
                OlusturmaTarihi = dt.OlusturmaTarihi,
                AcikGunSayisi = (int)(DateTime.UtcNow - dt.OlusturmaTarihi).TotalDays
            })
            .AsNoTracking()
            .ToListAsync();

        foreach (var item in liste)
        {
            var sla = HesaplaSla(item.OlusturmaTarihi, item.Oncelik.OncelikAdi, item.Durum.DurumAdi);
            item.SlaHedefTarihi = sla.hedefTarih;
            item.SlaKalanSaat = sla.kalanSaat;
            item.SlaAsildiMi = sla.asildi;
            item.SlaDurumMetni = sla.durumMetni;
        }

        Response.Headers.Append("X-Toplam-Kayit", toplamKayit.ToString());
        Response.Headers.Append("X-Mevcut-Sayfa", sayfa.ToString());
        Response.Headers.Append("X-Sayfa-Boyutu", sayfaBoyutu.ToString());

        return Ok(liste);
    }

    // =========================================================================
    // GET api/destektalepleri/istatistikler (SQL View & Function Destegi)
    // =========================================================================

    /// <summary>
    /// Destek talepleri hakkinda genel metrikleri ve istatistikleri getirir.
    /// SQL Function (FN_KullanicininAktifTalepSayisi) ve View (VW_AktifTalepDetaylari) islevlerini API'ye sunar.
    /// </summary>
    [HttpGet("istatistikler")]
    [ProducesResponseType(typeof(TalepIstatistikDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> IstatistikleriGetir()
    {
        int? aktifKullaniciId = OturumAcanKullaniciId();

        int toplamTalep = await _veritabani.DestekTalepleri.CountAsync();
        int aktifTalep = await _veritabani.DestekTalepleri.CountAsync(dt => dt.Durum.DurumAdi != "Kapatildi");
        int cozulenTalep = await _veritabani.DestekTalepleri.CountAsync(dt => dt.Durum.DurumAdi == "Cozuldu" || dt.Durum.DurumAdi == "Kapatildi");

        int kullaniciAktifTalep = 0;
        if (aktifKullaniciId.HasValue)
        {
            kullaniciAktifTalep = await _veritabani.DestekTalepleri
                .CountAsync(dt => dt.OlusturanKullaniciId == aktifKullaniciId.Value && dt.Durum.DurumAdi != "Kapatildi");
        }

        var kategoriDagilimi = await _veritabani.DestekTalepleri
            .Where(dt => dt.Durum.DurumAdi != "Kapatildi")
            .GroupBy(dt => dt.Kategori.KategoriAdi)
            .Select(g => new { Kategori = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.Kategori, x => x.Adet);

        var oncelikDagilimi = await _veritabani.DestekTalepleri
            .Where(dt => dt.Durum.DurumAdi != "Kapatildi")
            .GroupBy(dt => dt.Oncelik.OncelikAdi)
            .Select(g => new { Oncelik = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.Oncelik, x => x.Adet);

        var istatistik = new TalepIstatistikDto
        {
            ToplamTalepSayisi = toplamTalep,
            AktifTalepSayisi = aktifTalep,
            CozulenTalepSayisi = cozulenTalep,
            KullanicininAktifTalepSayisi = kullaniciAktifTalep,
            KategoriDagilimi = kategoriDagilimi,
            OncelikDagilimi = oncelikDagilimi
        };

        return Ok(istatistik);
    }

    // =========================================================================
    // GET api/destektalepleri/{id}
    // =========================================================================

    /// <summary>
    /// Tekil talep detayini getirir.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DestekTalebiDetayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TalepDetayiGetir(int id)
    {
        if (id <= 0) return BadRequest("Gecersiz talep ID degeri.");

        var talep = await _veritabani.DestekTalepleri
            .Include(dt => dt.OlusturanKullanici)
            .Include(dt => dt.AtananKullanici)
            .Include(dt => dt.Kategori)
            .Include(dt => dt.Oncelik)
            .Include(dt => dt.Durum)
            .Include(dt => dt.Yorumlar.OrderBy(y => y.OlusturmaTarihi))
                .ThenInclude(y => y.YazanKullanici)
            .AsNoTracking()
            .FirstOrDefaultAsync(dt => dt.TalepId == id);

        if (talep == null) return NotFound($"ID:{id} olan talep bulunamadi.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        if (!yonetici && talep.OlusturanKullaniciId != aktifKullaniciId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Bu talebe erisim yetkiniz bulunmamaktadir.");
        }

        
        var ekler = await _veritabani.TalepEkleri
            .Include(e => e.YukleyenKullanici)
            .Where(e => e.TalepId == id)
            .OrderBy(e => e.YuklenmeTarihi)
            .Select(e => new TalepEkiDto
            {
                EkId = e.EkId,
                TalepId = e.TalepId,
                YorumId = e.YorumId,
                DosyaAdi = e.DosyaAdi,
                DosyaUzantisi = e.DosyaUzantisi,
                DosyaBoyutuBayt = e.DosyaBoyutuBayt,
                IcerikTuru = e.IcerikTuru,
                YuklenmeTarihi = e.YuklenmeTarihi,
                IndirmeUrl = $"/api/destektalepleri/dosyalar/{e.EkId}",
                YukleyenKullanici = new KullaniciOzetDto
                {
                    KullaniciId = e.YukleyenKullanici.KullaniciId,
                    AdSoyad = e.YukleyenKullanici.Ad + " " + e.YukleyenKullanici.Soyad,
                    Eposta = e.YukleyenKullanici.Eposta
                }
            })
            .AsNoTracking()
            .ToListAsync();

        var detayDto = new DestekTalebiDetayDto
        {
            TalepId = talep.TalepId,
            Baslik = talep.Baslik,
            Aciklama = talep.Aciklama,
            OlusturanKullanici = new KullaniciOzetDto
            {
                KullaniciId = talep.OlusturanKullanici.KullaniciId,
                AdSoyad = talep.OlusturanKullanici.Ad + " " + talep.OlusturanKullanici.Soyad,
                Eposta = talep.OlusturanKullanici.Eposta
            },
            AtananKullanici = talep.AtananKullanici == null ? null : new KullaniciOzetDto
            {
                KullaniciId = talep.AtananKullanici.KullaniciId,
                AdSoyad = talep.AtananKullanici.Ad + " " + talep.AtananKullanici.Soyad,
                Eposta = talep.AtananKullanici.Eposta
            },
            Kategori = new KategoriDto
            {
                KategoriId = talep.Kategori.KategoriId,
                KategoriAdi = talep.Kategori.KategoriAdi,
                Aciklama = talep.Kategori.Aciklama,
                AktifMi = talep.Kategori.AktifMi
            },
            Oncelik = new TalepOnceligiDto
            {
                OncelikId = talep.Oncelik.OncelikId,
                OncelikAdi = talep.Oncelik.OncelikAdi,
                SiraNo = talep.Oncelik.SiraNo
            },
            Durum = new TalepDurumuDto
            {
                DurumId = talep.Durum.DurumId,
                DurumAdi = talep.Durum.DurumAdi
            },
            OlusturmaTarihi = talep.OlusturmaTarihi,
            GuncellemeTarihi = talep.GuncellemeTarihi,
            KapanisTarihi = talep.KapanisTarihi,
            Ekler = ekler,
            Yorumlar = talep.Yorumlar
                .Where(y => yonetici || !y.DahiliMi)
                .Select(y => new TalepYorumuDto
                {
                    YorumId = y.YorumId,
                    YorumMetni = y.YorumMetni,
                    OlusturmaTarihi = y.OlusturmaTarihi,
                    DahiliMi = y.DahiliMi,
                    YazanKullanici = new KullaniciOzetDto
                    {
                        KullaniciId = y.YazanKullanici.KullaniciId,
                        AdSoyad = y.YazanKullanici.Ad + " " + y.YazanKullanici.Soyad,
                        Eposta = y.YazanKullanici.Eposta
                    }
                }).ToList()
        };

        var slaDetay = HesaplaSla(talep.OlusturmaTarihi, talep.Oncelik.OncelikAdi, talep.Durum.DurumAdi);
        detayDto.SlaHedefTarihi = slaDetay.hedefTarih;
        detayDto.SlaKalanSaat = slaDetay.kalanSaat;
        detayDto.SlaAsildiMi = slaDetay.asildi;
        detayDto.SlaDurumMetni = slaDetay.durumMetni;

        return Ok(detayDto);
    }

    // =========================================================================
    // POST api/destektalepleri
    // =========================================================================

    /// <summary>
    /// Yeni destek talebi olusturur.
    /// SQL Server uzerinde SP_YeniTalepOlustur Stored Procedure calisir.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DestekTalebiDetayDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TalepOlustur([FromBody] DestekTalebiOlusturDto olusturDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int? kullaniciId = OturumAcanKullaniciId();
        if (!kullaniciId.HasValue) return Unauthorized("Oturum acmis kullanici bulunamadi.");

        int yeniTalepId;

        // SQL Server ortaminda Stored Procedure cagir (Hafta 2 gereksinimi)
        if (_veritabani.Database.IsSqlServer())
        {
            var talepIdCikti = new SqlParameter("@yeniTalepId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            var basariCikti = new SqlParameter("@basariMi", System.Data.SqlDbType.Bit) { Direction = System.Data.ParameterDirection.Output };
            var hataMesajiCikti = new SqlParameter("@hataMesaji", System.Data.SqlDbType.NVarChar, 500) { Direction = System.Data.ParameterDirection.Output };

            await _veritabani.Database.ExecuteSqlRawAsync(
                "EXEC dbo.SP_YeniTalepOlustur @baslik, @aciklama, @olusturanKullaniciId, @kategoriId, @oncelikId, @yeniTalepId OUTPUT, @basariMi OUTPUT, @hataMesaji OUTPUT",
                new SqlParameter("@baslik", olusturDto.Baslik.Trim()),
                new SqlParameter("@aciklama", olusturDto.Aciklama.Trim()),
                new SqlParameter("@olusturanKullaniciId", kullaniciId.Value),
                new SqlParameter("@kategoriId", olusturDto.KategoriId),
                new SqlParameter("@oncelikId", olusturDto.OncelikId),
                talepIdCikti, basariCikti, hataMesajiCikti);

            bool basarili = (bool)(basariCikti.Value ?? false);
            if (!basarili)
            {
                string hata = hataMesajiCikti.Value?.ToString() ?? "Talep olusturulamadi.";
                return BadRequest(hata);
            }

            yeniTalepId = (int)talepIdCikti.Value;
        }
        else
        {
            // Unit Test ve InMemory ortamlari icin EF Core fallback
            var acikDurum = await _veritabani.TalepDurumlari.FirstOrDefaultAsync(d => d.DurumAdi == "Acik")
                            ?? await _veritabani.TalepDurumlari.FirstAsync();

            var yeniTalep = new DestekTalebi
            {
                Baslik = olusturDto.Baslik.Trim(),
                Aciklama = olusturDto.Aciklama.Trim(),
                OlusturanKullaniciId = kullaniciId.Value,
                KategoriId = olusturDto.KategoriId,
                OncelikId = olusturDto.OncelikId,
                DurumId = acikDurum.DurumId,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _veritabani.DestekTalepleri.AddAsync(yeniTalep);
            await _veritabani.SaveChangesAsync();
            yeniTalepId = yeniTalep.TalepId;
        }

        _logger.LogInformation("Yeni destek talebi olusturuldu. TalepId={Id}", yeniTalepId);

        try
        {
            var talepSahibi = await _veritabani.Kullanicilar.FindAsync(kullaniciId.Value);
            if (talepSahibi != null && !string.IsNullOrWhiteSpace(talepSahibi.Eposta))
            {
                await _epostaServisi.TalepOlusturulduBildirimiGonderAsync(
                    talepSahibi.Eposta,
                    $"{talepSahibi.Ad} {talepSahibi.Soyad}".Trim(),
                    yeniTalepId,
                    olusturDto.Baslik.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Talep olusturuldu e-posta bildirimi gonderilemedi. TalepId={Id}", yeniTalepId);
        }

        var detay = await TalepDetayiGetir(yeniTalepId);
        return CreatedAtAction(nameof(TalepDetayiGetir), new { id = yeniTalepId }, ((OkObjectResult)detay).Value);
    }

    // =========================================================================
    // PUT api/destektalepleri/{id}
    // =========================================================================

    /// <summary>
    /// Destek talebini gunceller.
    /// Atama, durum ve oncelik degisiklikleri sadece Yonetici yetkisiyle yapilabilir.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DestekTalebiDetayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TalepGuncelle(int id, [FromBody] DestekTalebiGuncelleDto guncelleDto)
    {
        if (id <= 0) return BadRequest("Gecersiz ID.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var mevcut = await _veritabani.DestekTalepleri
            .Include(dt => dt.Durum)
            .FirstOrDefaultAsync(dt => dt.TalepId == id);

        if (mevcut == null) return NotFound($"ID:{id} olan talep bulunamadi.");
        if (mevcut.Durum.DurumAdi == "Kapatildi") return BadRequest("Kapali talepler guncellenemez.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        bool yoneticiAlaniGonderildi = guncelleDto.AtananKullaniciId.HasValue ||
                                       guncelleDto.DurumId.HasValue ||
                                       guncelleDto.OncelikId.HasValue;

        if (yoneticiAlaniGonderildi && !yonetici)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Atama, durum ve oncelik degisikliklerini sadece Yonetici yapabilir.");
        }

        if (guncelleDto.AtananKullaniciId.HasValue)
        {
            if (guncelleDto.AtananKullaniciId.Value == -1)
            {
                mevcut.AtananKullaniciId = null;
            }
            else
            {
                bool yoneticiMevcut = await _veritabani.Kullanicilar
                    .AnyAsync(k => k.KullaniciId == guncelleDto.AtananKullaniciId.Value && k.AktifMi && k.Rol.RolAdi == "Yonetici");
                if (!yoneticiMevcut)
                {
                    return BadRequest($"ID:{guncelleDto.AtananKullaniciId.Value} aktif yonetici bulunamadi.");
                }
                mevcut.AtananKullaniciId = guncelleDto.AtananKullaniciId.Value;
            }
        }

        bool durumDegisti = false;
        if (guncelleDto.DurumId.HasValue)
        {
            bool durumMevcut = await _veritabani.TalepDurumlari.AnyAsync(d => d.DurumId == guncelleDto.DurumId.Value);
            if (!durumMevcut) return BadRequest($"ID:{guncelleDto.DurumId.Value} durum bulunamadi.");
            if (mevcut.DurumId != guncelleDto.DurumId.Value)
            {
                durumDegisti = true;
                mevcut.DurumId = guncelleDto.DurumId.Value;
            }
        }

        if (guncelleDto.OncelikId.HasValue)
        {
            bool oncelikMevcut = await _veritabani.TalepOncelikleri.AnyAsync(o => o.OncelikId == guncelleDto.OncelikId.Value);
            if (!oncelikMevcut) return BadRequest($"ID:{guncelleDto.OncelikId.Value} oncelik bulunamadi.");
            mevcut.OncelikId = guncelleDto.OncelikId.Value;
        }

        if (!string.IsNullOrWhiteSpace(guncelleDto.Baslik))
        {
            if (mevcut.OlusturanKullaniciId != aktifKullaniciId && !yonetici)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Baslik degisikligini yalnizca talep sahibi veya Yonetici yapabilir.");
            }
            mevcut.Baslik = guncelleDto.Baslik.Trim();
        }

        mevcut.GuncellemeTarihi = DateTime.UtcNow;
        await _veritabani.SaveChangesAsync();

        if (durumDegisti && guncelleDto.DurumId.HasValue)
        {
            try
            {
                var talepSahibi = await _veritabani.Kullanicilar.FindAsync(mevcut.OlusturanKullaniciId);
                var yeniDurum = await _veritabani.TalepDurumlari.FindAsync(guncelleDto.DurumId.Value);
                if (talepSahibi != null && !string.IsNullOrWhiteSpace(talepSahibi.Eposta))
                {
                    await _epostaServisi.TalepDurumuGuncellendiBildirimiGonderAsync(
                        talepSahibi.Eposta,
                        $"{talepSahibi.Ad} {talepSahibi.Soyad}".Trim(),
                        id,
                        mevcut.Baslik,
                        yeniDurum?.DurumAdi ?? "Guncellendi");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Talep durumu guncellendi e-posta bildirimi gonderilemedi. TalepId={Id}", id);
            }
        }

        _logger.LogInformation("Talep guncellendi. ID={Id}", id);
        var guncellenmis = await TalepDetayiGetir(id);
        return Ok(((OkObjectResult)guncellenmis).Value);
    }

    // =========================================================================
    // DELETE api/destektalepleri/{id}
    // =========================================================================

    /// <summary>
    /// Destek talebini kapatir (soft delete - Durum="Kapatildi"). Sadece Yonetici.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Yonetici")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TalepKapat(int id)
    {
        if (id <= 0) return BadRequest("Gecersiz ID.");

        var mevcut = await _veritabani.DestekTalepleri
            .Include(dt => dt.Durum)
            .FirstOrDefaultAsync(dt => dt.TalepId == id);

        if (mevcut == null) return NotFound($"ID:{id} olan talep bulunamadi.");
        if (mevcut.Durum.DurumAdi == "Kapatildi") return NoContent();

        var kapatildi = await _veritabani.TalepDurumlari.FirstOrDefaultAsync(d => d.DurumAdi == "Kapatildi");
        if (kapatildi == null) return StatusCode(StatusCodes.Status500InternalServerError, "Sistem hatasi: 'Kapatildi' durumu bulunamadi.");

        mevcut.DurumId = kapatildi.DurumId;
        mevcut.KapanisTarihi = DateTime.UtcNow;
        await _veritabani.SaveChangesAsync();

        _logger.LogInformation("Talep kapatildi. ID={Id}", id);
        return NoContent();
    }

    // =========================================================================
    // POST api/destektalepleri/{id}/yorumlar (Yeni Yorum Ekleme)
    // =========================================================================

    /// <summary>
    /// Bir destek talebine yeni yorum veya mesaj ekler.
    /// Talep sahibi veya Yoneticiler yorum ekleyebilir.
    /// </summary>
    [HttpPost("{id:int}/yorumlar")]
    [ProducesResponseType(typeof(TalepYorumuDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> YorumEkle(int id, [FromBody] TalepYorumuOlusturDto yorumDto)
    {
        if (id <= 0) return BadRequest("Gecersiz talep ID degeri.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var talep = await _veritabani.DestekTalepleri
            .Include(dt => dt.Durum)
            .FirstOrDefaultAsync(dt => dt.TalepId == id);

        if (talep == null) return NotFound($"ID:{id} olan talep bulunamadi.");
        if (talep.Durum.DurumAdi == "Kapatildi") return BadRequest("Kapatilmis taleplere yorum eklenemez.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        if (!aktifKullaniciId.HasValue) return Unauthorized("Oturum acmis kullanici bulunamadi.");

        bool yonetici = IsYonetici();
        if (!yonetici && talep.OlusturanKullaniciId != aktifKullaniciId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Yalnizca kendi taleplerinize yorum ekleyebilirsiniz.");
        }

        var yeniYorum = new TalepYorumu
        {
            TalepId = id,
            YazanKullaniciId = aktifKullaniciId.Value,
            YorumMetni = yorumDto.YorumMetni.Trim(),
            OlusturmaTarihi = DateTime.UtcNow,
            DahiliMi = yonetici && yorumDto.DahiliMi
        };

        await _veritabani.TalepYorumlari.AddAsync(yeniYorum);
        await _veritabani.SaveChangesAsync();

        var yazanKullanici = await _veritabani.Kullanicilar.FirstAsync(k => k.KullaniciId == aktifKullaniciId.Value);

        var dto = new TalepYorumuDto
        {
            YorumId = yeniYorum.YorumId,
            YorumMetni = yeniYorum.YorumMetni,
            OlusturmaTarihi = yeniYorum.OlusturmaTarihi,
            DahiliMi = yeniYorum.DahiliMi,
            YazanKullanici = new KullaniciOzetDto
            {
                KullaniciId = yazanKullanici.KullaniciId,
                AdSoyad = yazanKullanici.Ad + " " + yazanKullanici.Soyad,
                Eposta = yazanKullanici.Eposta
            }
        };

        _logger.LogInformation("Talebe yorum eklendi. TalepId={TalepId}, YorumId={YorumId}", id, yeniYorum.YorumId);

        if (!yeniYorum.DahiliMi)
        {
            try
            {
                if (talep.OlusturanKullaniciId != aktifKullaniciId.Value)
                {
                    var talepSahibi = await _veritabani.Kullanicilar.FindAsync(talep.OlusturanKullaniciId);
                    if (talepSahibi != null && !string.IsNullOrWhiteSpace(talepSahibi.Eposta))
                    {
                        await _epostaServisi.YeniYorumBildirimiGonderAsync(
                            talepSahibi.Eposta,
                            $"{talepSahibi.Ad} {talepSahibi.Soyad}".Trim(),
                            id,
                            talep.Baslik,
                            $"{yazanKullanici.Ad} {yazanKullanici.Soyad}".Trim(),
                            yeniYorum.YorumMetni);
                    }
                }
                else if (talep.AtananKullaniciId.HasValue)
                {
                    var atanan = await _veritabani.Kullanicilar.FindAsync(talep.AtananKullaniciId.Value);
                    if (atanan != null && !string.IsNullOrWhiteSpace(atanan.Eposta))
                    {
                        await _epostaServisi.YeniYorumBildirimiGonderAsync(
                            atanan.Eposta,
                            $"{atanan.Ad} {atanan.Soyad}".Trim(),
                            id,
                            talep.Baslik,
                            $"{yazanKullanici.Ad} {yazanKullanici.Soyad}".Trim(),
                            yeniYorum.YorumMetni);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yeni yorum e-posta bildirimi gonderilemedi. TalepId={Id}", id);
            }
        }

        return CreatedAtAction(nameof(TalepDetayiGetir), new { id }, dto);
    }

    // =========================================================================
    // GET api/destektalepleri/{id}/yorumlar (Yorumlari Listele)
    // =========================================================================

    /// <summary>
    /// Talebe ait tum yorumlari listeler.
    /// </summary>
    [HttpGet("{id:int}/yorumlar")]
    [ProducesResponseType(typeof(IEnumerable<TalepYorumuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> YorumlariGetir(int id)
    {
        if (id <= 0) return BadRequest("Gecersiz talep ID degeri.");

        var talep = await _veritabani.DestekTalepleri.FirstOrDefaultAsync(dt => dt.TalepId == id);
        if (talep == null) return NotFound($"ID:{id} olan talep bulunamadi.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        if (!yonetici && talep.OlusturanKullaniciId != aktifKullaniciId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Bu talebin yorumlarini gorme yetkiniz bulunmamaktadir.");
        }

        var yorumlar = await _veritabani.TalepYorumlari
            .Include(y => y.YazanKullanici)
            .Where(y => y.TalepId == id && (yonetici || !y.DahiliMi))
            .OrderBy(y => y.OlusturmaTarihi)
            .Select(y => new TalepYorumuDto
            {
                YorumId = y.YorumId,
                YorumMetni = y.YorumMetni,
                OlusturmaTarihi = y.OlusturmaTarihi,
                DahiliMi = y.DahiliMi,
                YazanKullanici = new KullaniciOzetDto
                {
                    KullaniciId = y.YazanKullanici.KullaniciId,
                    AdSoyad = y.YazanKullanici.Ad + " " + y.YazanKullanici.Soyad,
                    Eposta = y.YazanKullanici.Eposta
                }
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(yorumlar);
    }

    // =========================================================================
    // GET api/destektalepleri/{id}/gecmis (Tarihce / Degisiklik Kayitlari)
    // =========================================================================

    /// <summary>
    /// Destek talebinde gerceklesen durum, oncelik ve atama gecmisini dondurur.
    /// SQL Trigger'i tarafindan kaydedilen verileri API uzerinden sunar.
    /// </summary>
    [HttpGet("{id:int}/gecmis")]
    [ProducesResponseType(typeof(IEnumerable<TalepDegisiklikGecmisiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GecmisiGetir(int id)
    {
        if (id <= 0) return BadRequest("Gecersiz talep ID degeri.");

        var talep = await _veritabani.DestekTalepleri.FirstOrDefaultAsync(dt => dt.TalepId == id);
        if (talep == null) return NotFound($"ID:{id} olan talep bulunamadi.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        if (!yonetici && talep.OlusturanKullaniciId != aktifKullaniciId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Bu talebin gecmisini gorme yetkiniz bulunmamaktadir.");
        }

        var gecmis = await _veritabani.TalepDegisiklikGecmisleri
            .Where(g => g.TalepId == id)
            .OrderByDescending(g => g.DegistirmeTarihi)
            .Select(g => new TalepDegisiklikGecmisiDto
            {
                GecmisId = g.GecmisId,
                TalepId = g.TalepId,
                EskiDurumId = g.EskiDurumId,
                YeniDurumId = g.YeniDurumId,
                EskiOncelikId = g.EskiOncelikId,
                YeniOncelikId = g.YeniOncelikId,
                EskiAtananKullaniciId = g.EskiAtananKullaniciId,
                YeniAtananKullaniciId = g.YeniAtananKullaniciId,
                DegistirmeTarihi = g.DegistirmeTarihi,
                DegisiklikAciklamasi = g.DegisiklikAciklamasi
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(gecmis);
    }

    // =========================================================================
    // POST api/destektalepleri/{id}/dosyalar (Dosya / Ek Yukleme)
    // =========================================================================

    /// <summary>
    /// Talebe ekran goruntusu, PDF, log veya belge ekler.
    /// </summary>
    [HttpPost("{id:int}/dosyalar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TalepEkiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DosyaYukle(int id, IFormFile dosya, [FromQuery] int? yorumId = null)
    {
        if (id <= 0) return BadRequest("Gecersiz talep ID.");
        if (dosya == null || dosya.Length == 0) return BadRequest("Lutfen yuklemek icin bir dosya seciniz.");

        if (dosya.Length > 10 * 1024 * 1024) return BadRequest("Dosya boyutu 10MB'dan buyuk olamaz.");

        var talep = await _veritabani.DestekTalepleri.FirstOrDefaultAsync(dt => dt.TalepId == id);
        if (talep == null) return NotFound($"ID:{id} olan talep bulunamadi.");

        int? aktifKullaniciId = OturumAcanKullaniciId();
        if (!aktifKullaniciId.HasValue) return Unauthorized("Oturum acmis kullanici bulunamadi.");

        bool yonetici = IsYonetici();
        if (!yonetici && talep.OlusturanKullaniciId != aktifKullaniciId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Bu talebe dosya yukleme yetkiniz bulunmamaktadir.");
        }

        var izinVerilenUzantilar = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".pdf", ".txt", ".log", ".docx", ".xlsx", ".zip" };
        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!izinVerilenUzantilar.Contains(uzanti))
        {
            return BadRequest($"'{uzanti}' uzantili dosya yuklenemez. Izin verilenler: {string.Join(", ", izinVerilenUzantilar)}");
        }

        var uploadsKlasoru = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsKlasoru))
        {
            Directory.CreateDirectory(uploadsKlasoru);
        }

        var guvenliDosyaAdi = $"{Guid.NewGuid()}{uzanti}";
        var tamYol = Path.Combine(uploadsKlasoru, guvenliDosyaAdi);

        using (var stream = new FileStream(tamYol, FileMode.Create))
        {
            await dosya.CopyToAsync(stream);
        }

        var talepEki = new TalepEki
        {
            TalepId = id,
            YorumId = yorumId,
            YukleyenKullaniciId = aktifKullaniciId.Value,
            DosyaAdi = Path.GetFileName(dosya.FileName),
            SaklananDosyaAdi = guvenliDosyaAdi,
            DosyaUzantisi = uzanti,
            DosyaBoyutuBayt = dosya.Length,
            IcerikTuru = string.IsNullOrWhiteSpace(dosya.ContentType) ? "application/octet-stream" : dosya.ContentType,
            YuklenmeTarihi = DateTime.UtcNow
        };

        await _veritabani.TalepEkleri.AddAsync(talepEki);
        await _veritabani.SaveChangesAsync();

        var yukleyen = await _veritabani.Kullanicilar.FirstAsync(k => k.KullaniciId == aktifKullaniciId.Value);

        var dto = new TalepEkiDto
        {
            EkId = talepEki.EkId,
            TalepId = talepEki.TalepId,
            YorumId = talepEki.YorumId,
            DosyaAdi = talepEki.DosyaAdi,
            DosyaUzantisi = talepEki.DosyaUzantisi,
            DosyaBoyutuBayt = talepEki.DosyaBoyutuBayt,
            IcerikTuru = talepEki.IcerikTuru,
            YuklenmeTarihi = talepEki.YuklenmeTarihi,
            IndirmeUrl = $"/api/destektalepleri/dosyalar/{talepEki.EkId}",
            YukleyenKullanici = new KullaniciOzetDto
            {
                KullaniciId = yukleyen.KullaniciId,
                AdSoyad = yukleyen.Ad + " " + yukleyen.Soyad,
                Eposta = yukleyen.Eposta
            }
        };

        _logger.LogInformation("Dosya yuklendi. TalepId={TalepId}, EkId={EkId}, Dosya={Dosya}", id, talepEki.EkId, talepEki.DosyaAdi);
        return Created(dto.IndirmeUrl, dto);
    }

    // =========================================================================
    // GET api/destektalepleri/dosyalar/{ekId} (Dosya Indirme)
    // =========================================================================

    /// <summary>
    /// Ekli dosyayi guvenli olarak indirir.
    /// </summary>
    [HttpGet("dosyalar/{ekId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> DosyaIndir(int ekId)
    {
        if (ekId <= 0) return BadRequest("Gecersiz dosya ID.");

        var ek = await _veritabani.TalepEkleri.FirstOrDefaultAsync(e => e.EkId == ekId);
        if (ek == null) return NotFound("Dosya kaydi bulunamadi.");

        var uploadsKlasoru = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var tamYol = Path.Combine(uploadsKlasoru, ek.SaklananDosyaAdi);

        if (!System.IO.File.Exists(tamYol)) return NotFound("Fiziksel dosya sunucuda bulunamadi.");

        var stream = new FileStream(tamYol, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, ek.IcerikTuru, ek.DosyaAdi);
    }

    // =========================================================================
    // GET api/destektalepleri/disa-aktar (Excel / CSV Raporu)
    // =========================================================================

    /// <summary>
    /// Destek taleplerini Excel uyumlu UTF-8 CSV formatinda disa aktarir.
    /// Yonetici tum talepleri, Kullanici ise kendi taleplerini indirebilir.
    /// </summary>
    [HttpGet("disa-aktar")]
    [Produces("text/csv")]
    public async Task<IActionResult> TalepleriDisaAktar(
        [FromQuery] int? durumId = null,
        [FromQuery] int? kategoriId = null,
        [FromQuery] int? oncelikId = null)
    {
        int? aktifKullaniciId = OturumAcanKullaniciId();
        bool yonetici = IsYonetici();

        IQueryable<DestekTalebi> sorgu = _veritabani.DestekTalepleri
            .Include(dt => dt.OlusturanKullanici)
            .Include(dt => dt.AtananKullanici)
            .Include(dt => dt.Kategori)
            .Include(dt => dt.Oncelik)
            .Include(dt => dt.Durum);

        if (!yonetici)
        {
            sorgu = sorgu.Where(dt => dt.OlusturanKullaniciId == aktifKullaniciId);
        }

        if (durumId.HasValue) sorgu = sorgu.Where(dt => dt.DurumId == durumId.Value);
        if (kategoriId.HasValue) sorgu = sorgu.Where(dt => dt.KategoriId == kategoriId.Value);
        if (oncelikId.HasValue) sorgu = sorgu.Where(dt => dt.OncelikId == oncelikId.Value);

        var talepler = await sorgu
            .OrderByDescending(dt => dt.OlusturmaTarihi)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Talep ID;Baslik;Kategori;Olusturan;Eposta;Atanan;Oncelik;Durum;Olusturma Tarihi;Kapanis Tarihi;SLA Durumu");

        foreach (var t in talepler)
        {
            var sla = HesaplaSla(t.OlusturmaTarihi, t.Oncelik.OncelikAdi, t.Durum.DurumAdi);
            string baslikTemiz = t.Baslik.Replace(";", " ").Replace("\n", " ").Replace("\r", " ");
            string atanan = t.AtananKullanici != null ? $"{t.AtananKullanici.Ad} {t.AtananKullanici.Soyad}" : "Atama Yapilmadi";
            string kapanis = t.KapanisTarihi.HasValue ? t.KapanisTarihi.Value.ToString("yyyy-MM-dd HH:mm") : "-";

            sb.AppendLine($"{t.TalepId};{baslikTemiz};{t.Kategori.KategoriAdi};{t.OlusturanKullanici.Ad} {t.OlusturanKullanici.Soyad};{t.OlusturanKullanici.Eposta};{atanan};{t.Oncelik.OncelikAdi};{t.Durum.DurumAdi};{t.OlusturmaTarihi:yyyy-MM-dd HH:mm};{kapanis};{sla.durumMetni}");
        }

        byte[] csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        string dosyaAdi = $"DestekTalepleri_Rapor_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

        return File(csvBytes, "text/csv; charset=utf-8", dosyaAdi);
    }


    // =========================================================================
    // POST api/destektalepleri/eposta-test
    // =========================================================================

    /// <summary>
    /// E-posta bildirim altyapisini test etmek icin ornek bir bildirim gonderir. Sadece Yonetici.
    /// </summary>
    [HttpPost("eposta-test")]
    [Authorize(Roles = "Yonetici")]
    public async Task<IActionResult> TestEpostasiGonder([FromQuery] string? aliciEposta = null)
    {
        int? kullaniciId = OturumAcanKullaniciId();
        var kullanici = kullaniciId.HasValue ? await _veritabani.Kullanicilar.FindAsync(kullaniciId.Value) : null;
        string hedef = !string.IsNullOrWhiteSpace(aliciEposta) ? aliciEposta : (kullanici?.Eposta ?? "destek@sirket.com");

        await _epostaServisi.TalepOlusturulduBildirimiGonderAsync(
            hedef,
            kullanici != null ? $"{kullanici.Ad} {kullanici.Soyad}".Trim() : "Sistem Yoneticisi",
            999,
            "DestekTakip Kurumsal E-posta Altyapi Testi");

        return Ok(new
        {
            basarili = true,
            mesaj = $"Test e-posta bildirimi '{hedef}' adresine basariyla gonderildi (veya simulasyon loguna yazildi)."
        });
    }
}
