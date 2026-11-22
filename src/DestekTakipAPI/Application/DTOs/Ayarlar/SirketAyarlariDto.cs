namespace DestekTakipAPI.Application.DTOs.Ayarlar;

public class SirketAyarlariDto
{
    public string SirketAdi { get; set; } = string.Empty;
    public string SistemBasligi { get; set; } = string.Empty;
    public string? DestekEposta { get; set; }
    public string? DestekTelefon { get; set; }
    public string? WebSitesi { get; set; }
    public string? LogoUrl { get; set; }
    public string? AltBilgiMetni { get; set; }
    public DateTime GuncellemeTarihi { get; set; }
}

public class SirketAyarlariGuncelleDto
{
    public string SirketAdi { get; set; } = string.Empty;
    public string SistemBasligi { get; set; } = string.Empty;
    public string? DestekEposta { get; set; }
    public string? DestekTelefon { get; set; }
    public string? WebSitesi { get; set; }
    public string? LogoUrl { get; set; }
    public string? AltBilgiMetni { get; set; }
}
