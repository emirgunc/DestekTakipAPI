namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Destek taleplerinde gerceklesen degisikliklerin denetim kaydini tutar.
/// SQL karsiligi: dbo.TalepDegisiklikGecmisi tablosu.
/// Bu tablo Trigger tarafindan otomatik doldurulur.
/// </summary>
public class TalepDegisiklikGecmisi
{
    public int GecmisId { get; set; }
    public int TalepId { get; set; }
    public int? EskiDurumId { get; set; }
    public int? YeniDurumId { get; set; }
    public int? EskiOncelikId { get; set; }
    public int? YeniOncelikId { get; set; }
    public int? EskiAtananKullaniciId { get; set; }
    public int? YeniAtananKullaniciId { get; set; }
    public DateTime DegistirmeTarihi { get; set; }
    public string? DegisiklikAciklamasi { get; set; }

    public DestekTalebi DestekTalebi { get; set; } = null!;
}
