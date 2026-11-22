namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Bir destek talebine veya yoruma eklenen dosyalari temsil eder (Ekran goruntusu, PDF, Log vb.).
/// SQL karsiligi: dbo.TalepEkleri tablosu.
/// </summary>
public class TalepEki
{
    public int EkId { get; set; }
    public int TalepId { get; set; }
    public int? YorumId { get; set; }
    public int YukleyenKullaniciId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string SaklananDosyaAdi { get; set; } = string.Empty;
    public string DosyaUzantisi { get; set; } = string.Empty;
    public long DosyaBoyutuBayt { get; set; }
    public string IcerikTuru { get; set; } = string.Empty;
    public DateTime YuklenmeTarihi { get; set; } = DateTime.UtcNow;

    public DestekTalebi DestekTalebi { get; set; } = null!;
    public Kullanici YukleyenKullanici { get; set; } = null!;
}
