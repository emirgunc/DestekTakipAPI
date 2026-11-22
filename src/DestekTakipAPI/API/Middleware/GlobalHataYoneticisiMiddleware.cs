using System.Text.Json;
using System.Text.Json.Serialization;

namespace DestekTakipAPI.API.Middleware;

/// <summary>
/// Hata durumlarinda istemciye dondurülen standart JSON yanit modeli.
/// </summary>
public class HataYanitiModeli
{
    public bool BasariMi { get; set; } = false;
    public int HataKodu { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    public string IslemId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Zaman { get; set; } = DateTime.UtcNow;
    public string? Detay { get; set; }
}

/// <summary>
/// Tum pipeline'da yakalanmayan exception'lari merkezi olarak ele alan middleware sinifidir.
/// Program.cs'te diger tum middleware'lerden ONCE kaydedilmelidir.
/// </summary>
public class GlobalHataYoneticisiMiddleware
{
    private readonly RequestDelegate _sonraki;
    private readonly ILogger<GlobalHataYoneticisiMiddleware> _logger;
    private readonly IWebHostEnvironment _ortam;

    /// <summary>
    /// Yapici metot.
    /// </summary>
    public GlobalHataYoneticisiMiddleware(
        RequestDelegate sonraki,
        ILogger<GlobalHataYoneticisiMiddleware> logger,
        IWebHostEnvironment ortam)
    {
        _sonraki = sonraki;
        _logger  = logger;
        _ortam   = ortam;
    }

    /// <summary>
    /// Her HTTP isteginde calistirilan ana metot. ASP.NET Core convention'i: InvokeAsync.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpBaglam)
    {
        try
        {
            await _sonraki(httpBaglam);
        }
        catch (Exception hata)
        {
            _logger.LogError(hata,
                "Yakalanmamis exception. Path={Path} | Method={Method} | Mesaj={Mesaj}",
                httpBaglam.Request.Path,
                httpBaglam.Request.Method,
                hata.Message);

            await HatayiIsleAsync(httpBaglam, hata);
        }
    }

    /// <summary>
    /// Exception tipine gore HTTP kodu belirler ve standart JSON yanit gonderir.
    /// </summary>
    private async Task HatayiIsleAsync(HttpContext httpBaglam, Exception hata)
    {
        bool headerGonderilmis = httpBaglam.Response.HasStarted;
        if (headerGonderilmis)
        {
            return;
        }

        int    httpKodu;
        string mesaj;

        bool yetkisizErisim  = hata is UnauthorizedAccessException;
        bool argümanHatasi   = hata is ArgumentNullException || hata is ArgumentException;
        bool bulunamadi      = hata is KeyNotFoundException;
        bool gecersizIslem   = hata is InvalidOperationException;

        if (yetkisizErisim)
        {
            httpKodu = StatusCodes.Status401Unauthorized;
            mesaj    = "Bu islemi gerceklestirmek icin yetkiniz bulunmamaktadir.";
        }
        else if (argümanHatasi)
        {
            httpKodu = StatusCodes.Status400BadRequest;
            mesaj    = "Gerekli bir parametre eksik veya gecersiz.";
        }
        else if (bulunamadi)
        {
            httpKodu = StatusCodes.Status404NotFound;
            mesaj    = "Aranan kaynak bulunamadi.";
        }
        else if (gecersizIslem)
        {
            httpKodu = StatusCodes.Status422UnprocessableEntity;
            mesaj    = "Islem gerceklestirilemiyor: " + hata.Message;
        }
        else
        {
            httpKodu = StatusCodes.Status500InternalServerError;
            mesaj    = "Beklenmeyen bir sunucu hatasi olustu. Lutfen daha sonra tekrar deneyiniz.";
        }

        bool gelistirmeOrtami = _ortam.IsDevelopment();

        HataYanitiModeli hataYaniti = new HataYanitiModeli
        {
            BasariMi = false,
            HataKodu = httpKodu,
            Mesaj    = mesaj,
            IslemId  = httpBaglam.TraceIdentifier,
            Zaman    = DateTime.UtcNow,
            Detay    = gelistirmeOrtami ? hata.ToString() : null
        };

        httpBaglam.Response.StatusCode  = httpKodu;
        httpBaglam.Response.ContentType = "application/json; charset=utf-8";

        JsonSerializerOptions jsonSecenekler = new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented          = gelistirmeOrtami
        };

        string jsonYanit = JsonSerializer.Serialize(hataYaniti, jsonSecenekler);
        await httpBaglam.Response.WriteAsync(jsonYanit);
    }
}

/// <summary>
/// Program.cs'te kullanimi kolaylastiran uzanti metodu.
/// </summary>
public static class GlobalHataYoneticisiUzantisi
{
    /// <summary>
    /// GlobalHataYoneticisiMiddleware'i pipeline'a ekler.
    /// </summary>
    public static IApplicationBuilder GlobalHataYoneticisiniKullan(
        this IApplicationBuilder uygulama)
    {
        uygulama.UseMiddleware<GlobalHataYoneticisiMiddleware>();
        return uygulama;
    }
}
