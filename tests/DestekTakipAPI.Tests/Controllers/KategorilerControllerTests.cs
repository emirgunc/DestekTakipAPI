using DestekTakipAPI.API.Controllers;
using DestekTakipAPI.Application.DTOs.Kategori;
using DestekTakipAPI.Domain.Entities;
using DestekTakipAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DestekTakipAPI.Tests.Controllers;

public class KategorilerControllerTests
{
    private static DestekTakipDbContext InMemoryVeritabaniOlustur()
    {
        var opt = new DbContextOptionsBuilder<DestekTakipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DestekTakipDbContext(opt);
    }

    [Fact]
    public async Task TumKategorileriGetir_KategorileriDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        db.Kategoriler.AddRange(
            new Kategori { KategoriId = 1, KategoriAdi = "Yazilim", AktifMi = true },
            new Kategori { KategoriId = 2, KategoriAdi = "Donanim", AktifMi = true }
        );
        await db.SaveChangesAsync();

        var ctrl = new KategorilerController(db, NullLogger<KategorilerController>.Instance);
        var sonuc = await ctrl.TumKategorileriGetir();

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var liste = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.Equal(2, liste.Cast<object>().Count());
    }

    [Fact]
    public async Task KategoriGetir_VarolanId_OkDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        db.Kategoriler.Add(new Kategori { KategoriId = 1, KategoriAdi = "Ag Sorunlari", AktifMi = true });
        await db.SaveChangesAsync();

        var ctrl = new KategorilerController(db, NullLogger<KategorilerController>.Instance);
        var sonuc = await ctrl.KategoriGetir(1);

        var ok = Assert.IsType<OkObjectResult>(sonuc);
        var dto = Assert.IsType<KategoriDto>(ok.Value);
        Assert.Equal("Ag Sorunlari", dto.KategoriAdi);
    }

    [Fact]
    public async Task KategoriGetir_OlmayanId_NotFoundDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = new KategorilerController(db, NullLogger<KategorilerController>.Instance);
        var sonuc = await ctrl.KategoriGetir(999);

        Assert.IsType<NotFoundObjectResult>(sonuc);
    }

    [Fact]
    public async Task KategoriOlustur_Gecerli_CreatedDondurur()
    {
        using var db = InMemoryVeritabaniOlustur();
        var ctrl = new KategorilerController(db, NullLogger<KategorilerController>.Instance);
        var dto = new KategoriOlusturDto
        {
            KategoriAdi = "Guvenlik",
            Aciklama = "Guvenlik bildirimleri"
        };

        var sonuc = await ctrl.KategoriOlustur(dto);

        Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Equal(1, await db.Kategoriler.CountAsync());
    }
}
