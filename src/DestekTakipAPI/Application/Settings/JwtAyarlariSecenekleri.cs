namespace DestekTakipAPI.Application.Settings;

/// <summary>
/// JWT konfigurasyon POCO sinifi. appsettings.json'daki 'JwtAyarlari' bolumune karsilik gelir.
/// </summary>
public class JwtAyarlariSecenekleri
{
    public const string BolumAdi = "JwtAyarlari";
    public string GizliAnahtar { get; set; } = string.Empty;
    public string Yayinci { get; set; } = string.Empty;
    public string Izleyici { get; set; } = string.Empty;
    public int TokenGecerlilikDakika { get; set; } = 60;
}
