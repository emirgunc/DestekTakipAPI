using System.ComponentModel.DataAnnotations;

namespace DestekTakipAPI.Application.DTOs.Kategori;

/// <summary>
/// Kategori liste ve detay yanit DTO'su.
/// </summary>
public class KategoriDto
{
    public int KategoriId { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public bool AktifMi { get; set; }
}

/// <summary>
/// Yeni kategori olusturma istek DTO'su.
/// </summary>
public class KategoriOlusturDto
{
    [Required(ErrorMessage = "Kategori adi zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Kategori adi 2-100 karakter olmalidir.")]
    public string KategoriAdi { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Aciklama { get; set; }
}

/// <summary>
/// Kategori guncelleme istek DTO'su (partial update - null gonderilen alanlar degismez).
/// </summary>
public class KategoriGuncelleDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? KategoriAdi { get; set; }
    public string? Aciklama { get; set; }
    public bool? AktifMi { get; set; }
}
