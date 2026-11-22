namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Sistemin ana varligini temsil eder: bir destek talebi kaydini icerir.
/// SQL karsiligi: dbo.DestekTalepleri tablosu.
/// </summary>
public class DestekTalebi
{
    public int TalepId { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public int OlusturanKullaniciId { get; set; }
    public int? AtananKullaniciId { get; set; }
    public int KategoriId { get; set; }
    public int OncelikId { get; set; }
    public int DurumId { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public DateTime? KapanisTarihi { get; set; }

    public Kullanici OlusturanKullanici { get; set; } = null!;
    public Kullanici? AtananKullanici { get; set; }
    public Kategori Kategori { get; set; } = null!;
    public TalepOnceligi Oncelik { get; set; } = null!;
    public TalepDurumu Durum { get; set; } = null!;
    public ICollection<TalepYorumu> Yorumlar { get; set; } = new List<TalepYorumu>();
    public ICollection<TalepDegisiklikGecmisi> DegisiklikGecmisi { get; set; } = new List<TalepDegisiklikGecmisi>();
}
