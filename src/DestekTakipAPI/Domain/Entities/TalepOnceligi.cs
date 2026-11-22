namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Destek taleplerinin oncelik seviyelerini temsil eder.
/// SQL karsiligi: dbo.TalepOncelikleri tablosu.
/// </summary>
public class TalepOnceligi
{
    public int OncelikId { get; set; }
    public string OncelikAdi { get; set; } = string.Empty;
    public int SiraNo { get; set; }
    public ICollection<DestekTalebi> DestekTalepleri { get; set; } = new List<DestekTalebi>();
}
