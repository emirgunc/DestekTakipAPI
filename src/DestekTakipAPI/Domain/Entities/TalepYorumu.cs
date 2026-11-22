namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Bir destek talebi altindaki yorum ve mesajlari temsil eder.
/// SQL karsiligi: dbo.TalepYorumlari tablosu.
/// </summary>
public class TalepYorumu
{
    public int YorumId { get; set; }
    public int TalepId { get; set; }
    public int YazanKullaniciId { get; set; }
    public string YorumMetni { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
    public bool DahiliMi { get; set; }

    public DestekTalebi DestekTalebi { get; set; } = null!;
    public Kullanici YazanKullanici { get; set; } = null!;
}
