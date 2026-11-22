using DestekTakipAPI.API.Controllers;
using DestekTakipAPI.Application.DTOs.Auth;
using DestekTakipAPI.Application.Settings;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using DestekTakipAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DestekTakipAPI.Tests.Controllers;

public class AuthControllerTests
{
    private static DestekTakipDbContext InMemoryVeritabaniOlustur()
    {
        var opt = new DbContextOptionsBuilder<DestekTakipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DestekTakipDbContext(opt);
    }

    private static IJwtTokenUretici TokenUreticiOlustur()
    {
        var secenekler = Options.Create(new JwtAyarlariSecenekleri
        {
            GizliAnahtar = "BuCokGizliVeEnAzOtuzIkiKarakterUzunlugundaBirAnahtar123!",
            Yayinci = "DestekTakipAPI",
            Izleyici = "DestekTakipClient",
            TokenGecerlilikDakika = 60
        });

        return new JwtTokenUretici(secenekler);
    }

    private static async Task TestKullanicilariEkleAsync(DestekTakipDbContext db)
    {
        db.Roller.AddRange(
            new Rol { RolId = 1, RolAdi = "Kullanici", OlusturmaTarihi = DateTime.UtcNow },
            new Rol { RolId = 2, RolAdi = "Yonetici", OlusturmaTarihi = DateTime.UtcNow }
        );

        string sifreHash = BCrypt.Net.BCrypt.HashPassword("Test123!");

        db.Kullanicilar.AddRange(
            new Kullanici
            {
                KullaniciId = 1,
                Ad = "Ali",
                Soyad = "Yilmaz",
                Eposta = "ali@test.com",
                SifreHash = sifreHash,
                RolId = 1,
                AktifMi = true,
                OlusturmaTarihi = DateTime.UtcNow
            },
            new Kullanici
            {
                KullaniciId = 2,
                Ad = "Veli",
                Soyad = "Kaya",
                Eposta = "veli@test.com",
                SifreHash = sifreHash,
                RolId = 1,
                AktifMi = false, // Pasif hesap
                OlusturmaTarihi = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Giris_GecerliBilgiler_TokenYanitiDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TestKullanicilariEkleAsync(db);

        var tokenUretici = TokenUreticiOlustur();
        var ctrl = new AuthController(db, tokenUretici, NullLogger<AuthController>.Instance);

        var loginDto = new LoginDto
        {
            Eposta = "ali@test.com",
            Sifre = "Test123!"
        };

        var sonuc = await ctrl.Giris(loginDto);

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var token = Assert.IsType<TokenYanitiDto>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.Equal("Ali Yilmaz", token.KullaniciAdSoyad);
        Assert.Equal("Kullanici", token.RolAdi);
    }

    [Fact]
    public async Task Giris_YanlisSifre_UnauthorizedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TestKullanicilariEkleAsync(db);

        var tokenUretici = TokenUreticiOlustur();
        var ctrl = new AuthController(db, tokenUretici, NullLogger<AuthController>.Instance);

        var loginDto = new LoginDto
        {
            Eposta = "ali@test.com",
            Sifre = "YanlisSifre!"
        };

        var sonuc = await ctrl.Giris(loginDto);

        var unauth = Assert.IsType<UnauthorizedObjectResult>(sonuc);
        Assert.Equal("E-posta adresi veya sifre hatali.", unauth.Value);
    }

    [Fact]
    public async Task Giris_BilinmeyenEposta_UnauthorizedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TestKullanicilariEkleAsync(db);

        var tokenUretici = TokenUreticiOlustur();
        var ctrl = new AuthController(db, tokenUretici, NullLogger<AuthController>.Instance);

        var loginDto = new LoginDto
        {
            Eposta = "olmayan@test.com",
            Sifre = "Test123!"
        };

        var sonuc = await ctrl.Giris(loginDto);

        var unauth = Assert.IsType<UnauthorizedObjectResult>(sonuc);
        Assert.Equal("E-posta adresi veya sifre hatali.", unauth.Value);
    }

    [Fact]
    public async Task Giris_PasifHesap_UnauthorizedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        await TestKullanicilariEkleAsync(db);

        var tokenUretici = TokenUreticiOlustur();
        var ctrl = new AuthController(db, tokenUretici, NullLogger<AuthController>.Instance);

        var loginDto = new LoginDto
        {
            Eposta = "veli@test.com",
            Sifre = "Test123!"
        };

        var sonuc = await ctrl.Giris(loginDto);

        var unauth = Assert.IsType<UnauthorizedObjectResult>(sonuc);
        Assert.Equal("Hesabiniz pasif durumda. Yonetici ile iletisime gecin.", unauth.Value);
    }
}
