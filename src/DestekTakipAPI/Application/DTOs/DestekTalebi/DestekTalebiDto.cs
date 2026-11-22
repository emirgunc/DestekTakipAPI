using System.ComponentModel.DataAnnotations;
using DestekTakipAPI.Application.DTOs.Kategori;
using DestekTakipAPI.Application.DTOs.Shared;

namespace DestekTakipAPI.Application.DTOs.DestekTalebi;

/// <summary>
/// Talep altindaki yorum ozet DTO'su.
/// </summary>
public class TalepYorumuDto
{
    public int YorumId { get; set; }
    public string YorumMetni { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
    public bool DahiliMi { get; set; }
    public KullaniciOzetDto YazanKullanici { get; set; } = null!;
}

/// <summary>
/// Talebe yeni yorum ekleme istek DTO'su.
/// </summary>
public class TalepYorumuOlusturDto
{
    [Required(ErrorMessage = "Yorum metni zorunludur.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Yorum 1-2000 karakter olmalidir.")]
    public string YorumMetni { get; set; } = string.Empty;

    public bool DahiliMi { get; set; } = false;
}

/// <summary>
/// Destek talebi tarihce / degisiklik kaydi DTO'su.
/// </summary>
public class TalepDegisiklikGecmisiDto
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
}

/// <summary>
/// Destek talebi liste satiri DTO'su (ozet bilgi).
/// </summary>

/// <summary>
/// Destek talebine veya yoruma yuklenen dosya eki DTO'su.
/// </summary>
public class TalepEkiDto
{
    public int EkId { get; set; }
    public int TalepId { get; set; }
    public int? YorumId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaUzantisi { get; set; } = string.Empty;
    public long DosyaBoyutuBayt { get; set; }
    public string IcerikTuru { get; set; } = string.Empty;
    public DateTime YuklenmeTarihi { get; set; }
    public string IndirmeUrl { get; set; } = string.Empty;
    public KullaniciOzetDto YukleyenKullanici { get; set; } = null!;
}

public class DestekTalebiListeDto
{
    public int TalepId { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string KategoriAdi { get; set; } = string.Empty;
    public KullaniciOzetDto OlusturanKullanici { get; set; } = null!;
    public KullaniciOzetDto? AtananKullanici { get; set; }
    public TalepOnceligiDto Oncelik { get; set; } = null!;
    public TalepDurumuDto Durum { get; set; } = null!;
    public DateTime OlusturmaTarihi { get; set; }
    public int AcikGunSayisi { get; set; }
    public DateTime SlaHedefTarihi { get; set; }
    public int SlaKalanSaat { get; set; }
    public bool SlaAsildiMi { get; set; }
    public string SlaDurumMetni { get; set; } = string.Empty;
}

/// <summary>
/// Destek talebi tam detay DTO'su (yorumlar dahil).
/// </summary>
public class DestekTalebiDetayDto
{
    public int TalepId { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public KullaniciOzetDto OlusturanKullanici { get; set; } = null!;
    public KullaniciOzetDto? AtananKullanici { get; set; }
    public KategoriDto Kategori { get; set; } = null!;
    public TalepOnceligiDto Oncelik { get; set; } = null!;
    public TalepDurumuDto Durum { get; set; } = null!;
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public DateTime? KapanisTarihi { get; set; }
    public List<TalepYorumuDto> Yorumlar { get; set; } = new();
    public DateTime SlaHedefTarihi { get; set; }
    public int SlaKalanSaat { get; set; }
    public bool SlaAsildiMi { get; set; }
    public string SlaDurumMetni { get; set; } = string.Empty;
    public List<TalepEkiDto> Ekler { get; set; } = new();
}

/// <summary>
/// Yeni destek talebi olusturma istek DTO'su.
/// </summary>
public class DestekTalebiOlusturDto
{
    [Required(ErrorMessage = "Baslik zorunludur.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Baslik 5-500 karakter olmalidir.")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aciklama zorunludur.")]
    [MinLength(10, ErrorMessage = "Aciklama en az 10 karakter olmalidir.")]
    public string Aciklama { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori secimi zorunludur.")]
    public int KategoriId { get; set; }

    [Required(ErrorMessage = "Oncelik secimi zorunludur.")]
    public int OncelikId { get; set; }
}

/// <summary>
/// Destek talebi guncelleme istek DTO'su (null gelen alanlar degismez).
/// </summary>
public class DestekTalebiGuncelleDto
{
    [StringLength(500, MinimumLength = 5)]
    public string? Baslik { get; set; }

    /// <summary>
    /// -1 gondermek atama kaldirir, pozitif ID atama yapar. null = degistirme.
    /// </summary>
    public int? AtananKullaniciId { get; set; }
    public int? DurumId { get; set; }
    public int? OncelikId { get; set; }
}

/// <summary>
/// Dashboard istatistik ve metrik DTO'su (SQL View ve Function karsiligi).
/// </summary>
public class TalepIstatistikDto
{
    public int ToplamTalepSayisi { get; set; }
    public int AktifTalepSayisi { get; set; }
    public int CozulenTalepSayisi { get; set; }
    public int KullanicininAktifTalepSayisi { get; set; }
    public Dictionary<string, int> KategoriDagilimi { get; set; } = new();
    public Dictionary<string, int> OncelikDagilimi { get; set; } = new();
}
