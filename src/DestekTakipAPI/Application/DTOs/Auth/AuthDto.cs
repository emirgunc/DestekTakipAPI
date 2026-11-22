using System.ComponentModel.DataAnnotations;

namespace DestekTakipAPI.Application.DTOs.Auth;

/// <summary>
/// Kullanici giris istegi DTO'su.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli e-posta giriniz.")]
    public string Eposta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Sifre en az 6 karakter olmalidir.")]
    public string Sifre { get; set; } = string.Empty;
}

/// <summary>
/// Basarili giris yanit DTO'su - JWT token ve kullanici bilgisi doner.
/// </summary>
public class TokenYanitiDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime SonGecerlilikTarihi { get; set; }
    public string KullaniciAdSoyad { get; set; } = string.Empty;
    public string Eposta { get; set; } = string.Empty;
    public string RolAdi { get; set; } = string.Empty;
}
