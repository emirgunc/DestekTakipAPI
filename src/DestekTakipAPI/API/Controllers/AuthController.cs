using DestekTakipAPI.Application.DTOs.Auth;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using DestekTakipAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DestekTakipAPI.API.Controllers;

/// <summary>
/// Kullanici kimlik dogrulama islemlerini yoneten controller.
/// Bu controller'daki endpoint'ler herkese aciktir (token gerektirmez).
/// Route: api/auth
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly DestekTakipDbContext _veritabani;
    private readonly IJwtTokenUretici _tokenUretici;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Yapici metot.
    /// </summary>
    public AuthController(
        DestekTakipDbContext veritabani,
        IJwtTokenUretici tokenUretici,
        ILogger<AuthController> logger)
    {
        _veritabani   = veritabani;
        _tokenUretici = tokenUretici;
        _logger       = logger;
    }

    // =========================================================================
    // POST api/auth/giris
    // =========================================================================

    /// <summary>
    /// Kullanicinin sisteme girisini dogrular ve JWT Access Token dondurur.
    /// Guvenlik: Kullanici yok ve sifre yanlis durumlarinda ayni mesaj doner.
    /// </summary>
    [HttpPost("giris")]
    [ProducesResponseType(typeof(TokenYanitiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Giris([FromBody] LoginDto loginDto)
    {
        try
        {
            bool modelGecersiz = !ModelState.IsValid;
            if (modelGecersiz)
            {
                return BadRequest(ModelState);
            }

            Kullanici? kullanici = await _veritabani.Kullanicilar
                .Include(k => k.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Eposta.ToLower() == loginDto.Eposta.ToLower().Trim());

            bool kullaniciBulunamadi = kullanici is null;
            if (kullaniciBulunamadi)
            {
                _logger.LogWarning("Giris basarisiz. Bilinmeyen e-posta: {Eposta}", loginDto.Eposta);
                return Unauthorized("E-posta adresi veya sifre hatali.");
            }

            bool hesapPasif = !kullanici!.AktifMi;
            if (hesapPasif)
            {
                _logger.LogWarning("Pasif hesap giris denemesi. KullaniciId={Id}", kullanici.KullaniciId);
                return Unauthorized("Hesabiniz pasif durumda. Yonetici ile iletisime gecin.");
            }

            bool sifreYanlis = !BCrypt.Net.BCrypt.Verify(loginDto.Sifre, kullanici.SifreHash);
            if (sifreYanlis)
            {
                _logger.LogWarning("Yanlis sifre. KullaniciId={Id}", kullanici.KullaniciId);
                return Unauthorized("E-posta adresi veya sifre hatali.");
            }

            string accessToken           = _tokenUretici.TokenOlustur(kullanici);
            DateTime sonGecerlilikTarihi = _tokenUretici.SonGecerlilikTarihiHesapla();

            _logger.LogInformation("Giris basarili. KullaniciId={Id}, Rol={Rol}",
                kullanici.KullaniciId, kullanici.Rol.RolAdi);

            TokenYanitiDto tokenYaniti = new TokenYanitiDto
            {
                AccessToken         = accessToken,
                SonGecerlilikTarihi = sonGecerlilikTarihi,
                KullaniciAdSoyad    = kullanici.Ad + " " + kullanici.Soyad,
                Eposta              = kullanici.Eposta,
                RolAdi              = kullanici.Rol.RolAdi
            };

            return Ok(tokenYaniti);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata, "Giris: Beklenmeyen hata. E-posta={Eposta}", loginDto.Eposta);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Sunucu hatasi: Giris islemi tamamlanamadi.");
        }
    }
}
