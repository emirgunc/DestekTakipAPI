using DestekTakipAPI.Application.DTOs.Kategori;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DestekTakipAPI.API.Controllers;

/// <summary>
/// Destek talebi kategorilerini yoneten API controller'i.
/// GET: Giris yapmis tum kullanicilar. POST/PUT/DELETE: Sadece Yonetici.
/// Route: api/kategoriler
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class KategorilerController : ControllerBase
{
    private readonly DestekTakipDbContext _veritabani;
    private readonly ILogger<KategorilerController> _logger;

    /// <summary>
    /// Yapici metot.
    /// </summary>
    public KategorilerController(DestekTakipDbContext veritabani, ILogger<KategorilerController> logger)
    {
        _veritabani = veritabani;
        _logger     = logger;
    }

    /// <summary>
    /// Tum aktif kategorileri listeler.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<KategoriDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TumKategorileriGetir()
    {
        try
        {
            List<KategoriDto> kategoriler = await _veritabani.Kategoriler
                .Where(k => k.AktifMi)
                .OrderBy(k => k.KategoriAdi)
                .Select(k => new KategoriDto
                {
                    KategoriId  = k.KategoriId,
                    KategoriAdi = k.KategoriAdi,
                    Aciklama    = k.Aciklama,
                    AktifMi     = k.AktifMi
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(kategoriler);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "TumKategorileriGetir: Hata.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Kategoriler getirilemedi.");
        }
    }

    /// <summary>
    /// Tekil kategori getirir.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(KategoriDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KategoriGetir(int id)
    {
        try
        {
            bool gecersizId = id <= 0;
            if (gecersizId)
            {
                return BadRequest("Gecersiz ID degeri.");
            }

            KategoriDto? dto = await _veritabani.Kategoriler
                .Where(k => k.KategoriId == id)
                .Select(k => new KategoriDto
                {
                    KategoriId  = k.KategoriId,
                    KategoriAdi = k.KategoriAdi,
                    Aciklama    = k.Aciklama,
                    AktifMi     = k.AktifMi
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            bool kategoriBulunamadi = dto is null;
            if (kategoriBulunamadi)
            {
                return NotFound($"ID:{id} olan kategori bulunamadi.");
            }

            return Ok(dto);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "KategoriGetir: ID={Id} hata.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Kategori getirilemedi.");
        }
    }

    /// <summary>
    /// Yeni kategori olusturur. Sadece Yonetici.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Yonetici")]
    [ProducesResponseType(typeof(KategoriDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> KategoriOlustur([FromBody] KategoriOlusturDto olusturDto)
    {
        try
        {
            bool modelGecersiz = !ModelState.IsValid;
            if (modelGecersiz)
            {
                return BadRequest(ModelState);
            }

            string temizAd = olusturDto.KategoriAdi.Trim();
            bool ayniIsimVar = await _veritabani.Kategoriler
                .AnyAsync(k => k.KategoriAdi.ToLower() == temizAd.ToLower());

            if (ayniIsimVar)
            {
                return Conflict($"'{temizAd}' adinda bir kategori zaten mevcut.");
            }

            Kategori yeni = new Kategori
            {
                KategoriAdi = temizAd,
                Aciklama    = olusturDto.Aciklama?.Trim(),
                AktifMi     = true
            };

            await _veritabani.Kategoriler.AddAsync(yeni);
            await _veritabani.SaveChangesAsync();

            _logger.LogInformation("Kategori olusturuldu. ID={Id}", yeni.KategoriId);

            KategoriDto yanit = new KategoriDto
            {
                KategoriId  = yeni.KategoriId,
                KategoriAdi = yeni.KategoriAdi,
                Aciklama    = yeni.Aciklama,
                AktifMi     = yeni.AktifMi
            };

            return CreatedAtAction(nameof(KategoriGetir), new { id = yeni.KategoriId }, yanit);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "KategoriOlustur: Hata.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Kategori olusturulamadi.");
        }
    }

    /// <summary>
    /// Kategori gunceller. Sadece Yonetici.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Yonetici")]
    [ProducesResponseType(typeof(KategoriDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KategoriGuncelle(int id, [FromBody] KategoriGuncelleDto guncelleDto)
    {
        try
        {
            bool gecersizId = id <= 0;
            if (gecersizId)
            {
                return BadRequest("Gecersiz ID.");
            }

            bool modelGecersiz = !ModelState.IsValid;
            if (modelGecersiz)
            {
                return BadRequest(ModelState);
            }

            Kategori? mevcut = await _veritabani.Kategoriler.FirstOrDefaultAsync(k => k.KategoriId == id);

            bool bulunamadi = mevcut is null;
            if (bulunamadi)
            {
                return NotFound($"ID:{id} olan kategori bulunamadi.");
            }

            bool yeniAdVar = !string.IsNullOrWhiteSpace(guncelleDto.KategoriAdi);
            if (yeniAdVar)
            {
                string temizAd = guncelleDto.KategoriAdi!.Trim();
                bool cakisiyor = await _veritabani.Kategoriler
                    .AnyAsync(k => k.KategoriAdi.ToLower() == temizAd.ToLower() && k.KategoriId != id);

                if (cakisiyor)
                {
                    return Conflict($"'{temizAd}' adinda baska kategori mevcut.");
                }

                mevcut!.KategoriAdi = temizAd;
            }

            bool aciklamaVar = guncelleDto.Aciklama is not null;
            if (aciklamaVar)
            {
                mevcut!.Aciklama = guncelleDto.Aciklama?.Trim();
            }

            bool aktiflikVar = guncelleDto.AktifMi.HasValue;
            if (aktiflikVar)
            {
                mevcut!.AktifMi = guncelleDto.AktifMi!.Value;
            }

            await _veritabani.SaveChangesAsync();
            _logger.LogInformation("Kategori guncellendi. ID={Id}", id);

            KategoriDto yanit = new KategoriDto
            {
                KategoriId  = mevcut!.KategoriId,
                KategoriAdi = mevcut.KategoriAdi,
                Aciklama    = mevcut.Aciklama,
                AktifMi     = mevcut.AktifMi
            };

            return Ok(yanit);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "KategoriGuncelle: ID={Id} hata.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Kategori guncellenemedi.");
        }
    }

    /// <summary>
    /// Kategoriyi pasiflestirir (soft delete). Sadece Yonetici.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Yonetici")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KategoriSil(int id)
    {
        try
        {
            bool gecersizId = id <= 0;
            if (gecersizId)
            {
                return BadRequest("Gecersiz ID.");
            }

            Kategori? mevcut = await _veritabani.Kategoriler.FirstOrDefaultAsync(k => k.KategoriId == id);

            bool bulunamadi = mevcut is null;
            if (bulunamadi)
            {
                return NotFound($"ID:{id} olan kategori bulunamadi.");
            }

            bool aktifTalepVar = await _veritabani.DestekTalepleri
                .AnyAsync(dt => dt.KategoriId == id && dt.Durum.DurumAdi != "Kapatildi");

            if (aktifTalepVar)
            {
                return BadRequest("Bu kategoriye bagli aktif talepler var. Once talepleri kapatin.");
            }

            mevcut!.AktifMi = false;
            await _veritabani.SaveChangesAsync();

            _logger.LogInformation("Kategori pasiflestirildi. ID={Id}", id);
            return NoContent();
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "KategoriSil: ID={Id} hata.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Kategori silinemedi.");
        }
    }
}
