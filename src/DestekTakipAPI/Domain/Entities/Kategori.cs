namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Destek taleplerini siniflandirmak icin kullanilan kategori bilgisini temsil eder.
/// SQL karsiligi: dbo.Kategoriler tablosu.
/// </summary>
public class Kategori
{
    public int KategoriId { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public bool AktifMi { get; set; }
    public ICollection<DestekTalebi> DestekTalepleri { get; set; } = new List<DestekTalebi>();
}
