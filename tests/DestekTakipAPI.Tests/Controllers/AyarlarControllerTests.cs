using DestekTakipAPI.API.Controllers;
using DestekTakipAPI.Application.DTOs.Ayarlar;
using DestekTakipAPI.Application.Settings;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DestekTakipAPI.Tests.Controllers;

public class AyarlarControllerTests
{
    private static DestekTakipDbContext InMemoryVeritabaniOlustur()
    {
        var opt = new DbContextOptionsBuilder<DestekTakipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DestekTakipDbContext(opt);
    }

    private static AyarlarController ControllerOlustur(DestekTakipDbContext db)
    {
        var ayarlar = Options.Create(new EpostaAyarlariSecenekleri
        {
            EtkinMi = false,
            Sunucu = "smtp.test.com"
        });

        return new AyarlarController(db, ayarlar, NullLogger<AyarlarController>.Instance);
    }

    [Fact]
    public async Task KurumsalAyarlariGetir_VeritabanindaYoksa_VarsayilanAyarlariDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = ControllerOlustur(db);

        var sonuc = await ctrl.KurumsalAyarlariGetir();

        var okSonuc = Assert.IsType<OkObjectResult>(sonuc);
        var dto = Assert.IsType<SirketAyarlariDto>(okSonuc.Value);
        Assert.Equal("DestekTakip Enterprise", dto.SirketAdi);
        Assert.NotEmpty(dto.SistemBasligi);
    }

    [Fact]
    public async Task KurumsalAyarlariGuncelle_GecerliVeriyle_AyarlariGunceller()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = ControllerOlustur(db);

        var guncelleDto = new SirketAyarlariGuncelleDto
        {
            SirketAdi = "Nova Teknoloji Ltd. Sti.",
            SistemBasligi = "Nova Destek Merkezi",
            DestekEposta = "destek@novatek.com",
            DestekTelefon = "+90 (216) 111 22 33",
            WebSitesi = "https://www.novatek.com",
            LogoUrl = "https://www.novatek.com/logo.png",
            AltBilgiMetni = "© 2026 Nova Teknoloji"
        };

        var sonuc = await ctrl.KurumsalAyarlariGuncelle(guncelleDto);

        var okSonuc = Assert.IsType<OkObjectResult>(sonuc);
        var dto = Assert.IsType<SirketAyarlariDto>(okSonuc.Value);
        Assert.Equal("Nova Teknoloji Ltd. Sti.", dto.SirketAdi);
        Assert.Equal("Nova Destek Merkezi", dto.SistemBasligi);
        Assert.Equal("destek@novatek.com", dto.DestekEposta);

        // Veritabanindan da dogrula
        var dbAyar = await db.SirketAyarlari.FirstAsync(s => s.AyarId == 1);
        Assert.Equal("Nova Teknoloji Ltd. Sti.", dbAyar.SirketAdi);
    }

    [Fact]
    public async Task KurumsalAyarlariGuncelle_SirketAdiBos_BadRequestDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = ControllerOlustur(db);

        var guncelleDto = new SirketAyarlariGuncelleDto
        {
            SirketAdi = "   "
        };

        var sonuc = await ctrl.KurumsalAyarlariGuncelle(guncelleDto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(sonuc);
        Assert.Equal("Sirket adi bos birakilamaz.", badRequest.Value);
    }

    [Fact]
    public async Task SistemBilgisiGetir_LisansVeSistemDurumunuDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = ControllerOlustur(db);

        var sonuc = await ctrl.SistemBilgisiGetir();

        var okSonuc = Assert.IsType<OkObjectResult>(sonuc);
        dynamic? val = okSonuc.Value;
        Assert.NotNull(val);
    }
}
