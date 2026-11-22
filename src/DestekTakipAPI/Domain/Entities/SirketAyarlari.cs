namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Sistem genelindeki kurumsal marka, unvan ve iletisim ayarlarini tutan varlik.
/// White-label ozellestirmeler ve kurumsal satin alma musterileri icin kullanilir.
/// SQL karsiligi: dbo.SirketAyarlari tablosu.
/// </summary>
public class SirketAyarlari
{
    public int AyarId { get; set; } = 1;
    public string SirketAdi { get; set; } = "DestekTakip Enterprise";
    public string SistemBasligi { get; set; } = "Kurumsal Destek Yönetim Platformu";
    public string? DestekEposta { get; set; } = "destek@sirketiniz.com";
    public string? DestekTelefon { get; set; } = "+90 (850) 000 00 00";
    public string? WebSitesi { get; set; } = "https://www.destektakip.com";
    public string? LogoUrl { get; set; }
    public string? AltBilgiMetni { get; set; } = "© 2026 DestekTakip B2B Çözümleri. Tüm hakları saklıdır.";
    public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;
}
