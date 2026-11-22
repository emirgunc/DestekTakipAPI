namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Bir destek talebinin icinde bulunabilecegi durumlari temsil eder.
/// SQL karsiligi: dbo.TalepDurumlari tablosu.
/// </summary>
public class TalepDurumu
{
    public int DurumId { get; set; }
    public string DurumAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public ICollection<DestekTalebi> DestekTalepleri { get; set; } = new List<DestekTalebi>();
}
