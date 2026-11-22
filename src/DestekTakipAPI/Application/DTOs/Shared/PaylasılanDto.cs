namespace DestekTakipAPI.Application.DTOs.Shared;

/// <summary>
/// Talep oncelik ozet DTO'su.
/// </summary>
public class TalepOnceligiDto
{
    public int OncelikId { get; set; }
    public string OncelikAdi { get; set; } = string.Empty;
    public int SiraNo { get; set; }
}

/// <summary>
/// Talep durum ozet DTO'su.
/// </summary>
public class TalepDurumuDto
{
    public int DurumId { get; set; }
    public string DurumAdi { get; set; } = string.Empty;
}

/// <summary>
/// Kullanici ozet bilgisi DTO'su.
/// </summary>
public class KullaniciOzetDto
{
    public int KullaniciId { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string Eposta { get; set; } = string.Empty;
}
