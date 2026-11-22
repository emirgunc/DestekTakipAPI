using DestekTakipAPI.Application.DTOs.Ayarlar;
using DestekTakipAPI.Application.Settings;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DestekTakipAPI.API.Controllers;

/// <summary>
/// Kurumsal marka (White-label) ve sistem genel ayarlarini yoneten API controller'i.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AyarlarController : ControllerBase
{
    private readonly DestekTakipDbContext _veritabani;
    private readonly EpostaAyarlariSecenekleri _epostaAyarlari;
    private readonly ILogger<AyarlarController> _logger;

    public AyarlarController(
        DestekTakipDbContext veritabani,
        IOptions<EpostaAyarlariSecenekleri> epostaAyarlari,
        ILogger<AyarlarController> logger)
    {
        _veritabani = veritabani;
        _epostaAyarlari = epostaAyarlari.Value;
        _logger = logger;
    }

    // =========================================================================
    // GET api/ayarlar/kurumsal (Anonim erisim - Marka/Logo yansitmasi icin)
    // =========================================================================

    /// <summary>
    /// Kurumsal marka ayarlarini getirir (Sirket adi, logo, unvan, iletisim).
    /// </summary>
    [HttpGet("kurumsal")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SirketAyarlariDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> KurumsalAyarlariGetir()
    {
        var ayar = await _veritabani.SirketAyarlari.FirstOrDefaultAsync(s => s.AyarId == 1);
        if (ayar == null)
        {
            ayar = new SirketAyarlari
            {
                AyarId = 1,
                SirketAdi = "DestekTakip Enterprise",
                SistemBasligi = "Kurumsal Destek Yönetim Platformu",
                DestekEposta = "destek@sirketiniz.com",
                DestekTelefon = "+90 (850) 000 00 00",
                WebSitesi = "https://www.destektakip.com",
                AltBilgiMetni = "© 2026 DestekTakip B2B Çözümleri. Tüm hakları saklıdır.",
                GuncellemeTarihi = DateTime.UtcNow
            };
            await _veritabani.SirketAyarlari.AddAsync(ayar);
            await _veritabani.SaveChangesAsync();
        }

        var dto = new SirketAyarlariDto
        {
            SirketAdi = ayar.SirketAdi,
            SistemBasligi = ayar.SistemBasligi,
            DestekEposta = ayar.DestekEposta,
            DestekTelefon = ayar.DestekTelefon,
            WebSitesi = ayar.WebSitesi,
            LogoUrl = ayar.LogoUrl,
            AltBilgiMetni = ayar.AltBilgiMetni,
            GuncellemeTarihi = ayar.GuncellemeTarihi
        };

        return Ok(dto);
    }

    // =========================================================================
    // PUT api/ayarlar/kurumsal (Sadece Yonetici)
    // =========================================================================

    /// <summary>
    /// Kurumsal marka ayarlarini gunceller (White-label ozellestirme).
    /// </summary>
    [HttpPut("kurumsal")]
    [Authorize(Roles = "Yonetici")]
    [ProducesResponseType(typeof(SirketAyarlariDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KurumsalAyarlariGuncelle([FromBody] SirketAyarlariGuncelleDto guncelleDto)
    {
        if (string.IsNullOrWhiteSpace(guncelleDto.SirketAdi))
        {
            return BadRequest("Sirket adi bos birakilamaz.");
        }

        var ayar = await _veritabani.SirketAyarlari.FirstOrDefaultAsync(s => s.AyarId == 1);
        if (ayar == null)
        {
            ayar = new SirketAyarlari { AyarId = 1 };
            await _veritabani.SirketAyarlari.AddAsync(ayar);
        }

        ayar.SirketAdi = guncelleDto.SirketAdi.Trim();
        ayar.SistemBasligi = string.IsNullOrWhiteSpace(guncelleDto.SistemBasligi) ? "Destek Portali" : guncelleDto.SistemBasligi.Trim();
        ayar.DestekEposta = guncelleDto.DestekEposta?.Trim();
        ayar.DestekTelefon = guncelleDto.DestekTelefon?.Trim();
        ayar.WebSitesi = guncelleDto.WebSitesi?.Trim();
        ayar.LogoUrl = guncelleDto.LogoUrl?.Trim();
        ayar.AltBilgiMetni = guncelleDto.AltBilgiMetni?.Trim();
        ayar.GuncellemeTarihi = DateTime.UtcNow;

        await _veritabani.SaveChangesAsync();
        _logger.LogInformation("Kurumsal ayarlar guncellendi: {SirketAdi}", ayar.SirketAdi);

        var dto = new SirketAyarlariDto
        {
            SirketAdi = ayar.SirketAdi,
            SistemBasligi = ayar.SistemBasligi,
            DestekEposta = ayar.DestekEposta,
            DestekTelefon = ayar.DestekTelefon,
            WebSitesi = ayar.WebSitesi,
            LogoUrl = ayar.LogoUrl,
            AltBilgiMetni = ayar.AltBilgiMetni,
            GuncellemeTarihi = ayar.GuncellemeTarihi
        };

        return Ok(dto);
    }

    // =========================================================================
    // GET api/ayarlar/sistem-bilgisi
    // =========================================================================

    /// <summary>
    /// Sistem saglik, lisans ve yapilandirma durumunu ozetler.
    /// </summary>
    [HttpGet("sistem-bilgisi")]
    [Authorize]
    public async Task<IActionResult> SistemBilgisiGetir()
    {
        int kullaniciSayisi = await _veritabani.Kullanicilar.CountAsync();
        int kategoriSayisi = await _veritabani.Kategoriler.CountAsync();
        int talepSayisi = await _veritabani.DestekTalepleri.CountAsync();

        return Ok(new
        {
            surum = "2.4.0-enterprise",
            lisansTuru = "Enterprise Commercial (Sinirsiz Kullanici & Kategori)",
            lisansDurumu = "Aktif",
            epostaServisiEtkin = _epostaAyarlari.EtkinMi,
            smtpSunucu = _epostaAyarlari.Sunucu,
            veritabani = _veritabani.Database.IsSqlServer() ? "Microsoft SQL Server 2022" : "InMemory Test DB",
            toplamKullanici = kullaniciSayisi,
            toplamKategori = kategoriSayisi,
            toplamTalep = talepSayisi,
            sunucuZamani = DateTime.UtcNow
        });
    }
}
