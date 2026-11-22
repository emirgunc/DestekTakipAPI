namespace DestekTakipAPI.Domain.Entities;

/// <summary>
/// Sistemdeki yetki rollerini temsil eder.
/// SQL karsiligi: dbo.Roller tablosu.
/// Ornek degerler: 'Kullanici', 'Yonetici'
/// </summary>
public class Rol
{
    /// <summary>
    /// Rolun birincil anahtar degeri. Veritabaninda otomatik artar (IDENTITY).
    /// </summary>
    public int RolId { get; set; }

    /// <summary>
    /// Rolun benzersiz adi. Ornek: "Kullanici", "Yonetici".
    /// Maksimum 50 karakter.
    /// </summary>
    public string RolAdi { get; set; } = string.Empty;

    /// <summary>
    /// Rolun ne anlama geldigini aciklayan opsiyonel metin.
    /// NULL olabilir.
    /// </summary>
    public string? Aciklama { get; set; }

    /// <summary>
    /// Rolun sisteme eklenme tarihi. Varsayilan deger: GETDATE().
    /// </summary>
    public DateTime OlusturmaTarihi { get; set; }

    // Navigation Properties
    /// <summary>
    /// Bu role sahip kullanicilarin koleksiyonu.
    /// </summary>
    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}
