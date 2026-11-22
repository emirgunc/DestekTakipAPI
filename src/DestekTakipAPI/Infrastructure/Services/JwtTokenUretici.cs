using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DestekTakipAPI.Application.Settings;
using DestekTakipAPI.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DestekTakipAPI.Infrastructure.Services;

/// <summary>
/// JWT token uretmekten sorumlu servis sozlesmesi.
/// </summary>
public interface IJwtTokenUretici
{
    string TokenOlustur(Kullanici kullanici);
    DateTime SonGecerlilikTarihiHesapla();
}

/// <summary>
/// IJwtTokenUretici implementasyonu. HS256 imzalama kullanir.
/// </summary>
public class JwtTokenUretici : IJwtTokenUretici
{
    private readonly JwtAyarlariSecenekleri _jwtSecenekler;

    /// <summary>
    /// Yapici metot - IOptions ile konfigurasyon inject edilir.
    /// </summary>
    public JwtTokenUretici(IOptions<JwtAyarlariSecenekleri> jwtSecenekler)
    {
        _jwtSecenekler = jwtSecenekler.Value;
    }

    /// <summary>
    /// Kullanici bilgilerinden imzali JWT token uretir.
    /// </summary>
    public string TokenOlustur(Kullanici kullanici)
    {
        byte[] anahtarBytes = Encoding.UTF8.GetBytes(_jwtSecenekler.GizliAnahtar);
        SymmetricSecurityKey guvenlikAnahtari = new SymmetricSecurityKey(anahtarBytes);
        SigningCredentials imzaKimlik = new SigningCredentials(
            guvenlikAnahtari, SecurityAlgorithms.HmacSha256Signature);

        List<Claim> claimler = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub,   kullanici.KullaniciId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, kullanici.Eposta),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,               kullanici.Rol.RolAdi),
            new Claim("KullaniciId",                 kullanici.KullaniciId.ToString()),
            new Claim("AdSoyad",                     kullanici.Ad + " " + kullanici.Soyad)
        };

        JwtSecurityToken jwtToken = new JwtSecurityToken(
            issuer:            _jwtSecenekler.Yayinci,
            audience:          _jwtSecenekler.Izleyici,
            claims:            claimler,
            notBefore:         DateTime.UtcNow,
            expires:           SonGecerlilikTarihiHesapla(),
            signingCredentials: imzaKimlik);

        JwtSecurityTokenHandler tokenYazici = new JwtSecurityTokenHandler();
        return tokenYazici.WriteToken(jwtToken);
    }

    /// <summary>
    /// Token son gecerlilik tarihini UTC olarak hesaplar.
    /// </summary>
    public DateTime SonGecerlilikTarihiHesapla()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSecenekler.TokenGecerlilikDakika);
    }
}
