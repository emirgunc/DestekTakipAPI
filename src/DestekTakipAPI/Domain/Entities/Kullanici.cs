namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Sisteme kayitli kullanicilari temsil eder.
/// SQL karsiligi: dbo.Kullanicilar tablosu.
/// </summary>
public class Kullanici
{
    public int KullaniciId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Eposta { get; set; } = string.Empty;
    public string SifreHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool AktifMi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }

    public Rol Rol { get; set; } = null!;
    public ICollection<DestekTalebi> OlusturulanTalepler { get; set; } = new List<DestekTalebi>();
    public ICollection<DestekTalebi> AtananTalepler { get; set; } = new List<DestekTalebi>();
    public ICollection<TalepYorumu> Yorumlar { get; set; } = new List<TalepYorumu>();
}
