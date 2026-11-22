namespace DestekTakipAPI.Application.Settings;

/// <summary>
/// E-posta (SMTP) bildirim servisi ayarlarini tutan POCO sinifi.
/// appsettings.json'daki 'EpostaAyarlari' bolumune karsilik gelir.
/// </summary>
public class EpostaAyarlariSecenekleri
{
    public const string BolumAdi = "EpostaAyarlari";

    public string Sunucu { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string KullaniciAdi { get; set; } = "destek@sirket.com";
    public string Sifre { get; set; } = string.Empty;
    public string GonderenEposta { get; set; } = "destek@sirket.com";
    public string GonderenAdi { get; set; } = "DestekTakip Bildirim Servisi";
    public bool SslKullan { get; set; } = true;

    /// <summary>
    /// false ise gercek SMTP yerine konsola/loga bildirim basar (Simulasyon/Gelistirme modu).
    /// </summary>
    public bool EtkinMi { get; set; } = false;
}
