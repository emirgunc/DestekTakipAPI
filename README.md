# DestekTakipAPI

Kurumsal destek talebi (Helpdesk / Ticket) süreçlerini yönetmek için **.NET 9** ve **Clean Architecture** prensipleriyle geliştirilmiş RESTful API projesi.

## Mimari ve Katmanlar

Proje sorumlulukların ayrılması (Separation of Concerns) ilkesine göre 4 ana katmandan oluşur:

- **Domain:** Temel varlıklar, enum'lar ve iş kuralları
- **Application:** DTO'lar, servis arayüzleri ve doğrulama mantığı
- **Infrastructure:** Entity Framework Core (`DestekTakipDbContext`), SQL Server bağlantısı, JWT üretimi ve SMTP e-posta servisi
- **API:** Controller'lar (`AuthController`, `DestekTalepleriController`, `KategorilerController`, `AyarlarController`), `GlobalHataYoneticisiMiddleware` ve Swagger/OpenAPI yapılandırması

## Öne Çıkan Özellikler

- **Kimlik Doğrulama & Yetkilendirme:** JWT Bearer Authentication ve rol bazlı erişim kontrolü
- **Merkezi Hata Yönetimi:** Özel `GlobalHataYoneticisiMiddleware` ile standartlaştırılmış API hata yanıtları
- **Veritabanı:** Entity Framework Core + SQL Server (`00_KurulumTam.sql` ile hızlı şema ve örnek veri kurulumu)
- **Test Güvencesi:** `tests/DestekTakipAPI.Tests` altında **30 adet xUnit** birim/entegrasyon testi
- **CI/CD:** `.github/workflows/dotnet.yml` ile her `push` ve `pull_request` üzerinde otomatik derleme, xUnit test koşumu ve kod kapsamı (Code Coverage) raporu

## Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server / SQL Server Express (yerel çalışma için)

### Adımlar

```bash
# 1. Bağımlılıkları yükle
dotnet restore

# 2. Testleri çalıştır (Veritabanı bağlantısı gerektirmez)
dotnet test

# 3. Veritabanını oluştur (SQL Server üzerinde)
# 00_KurulumTam.sql dosyasını SSMS veya sqlcmd ile çalıştırın.

# 4. API'yi başlat
dotnet run --project src/DestekTakipAPI/DestekTakipAPI.csproj
```

> **Güvenlik Notu:** `src/DestekTakipAPI/appsettings.json` içindeki `JwtAyarlari:GizliAnahtar` ve bağlantı dizesi yalnızca yerel geliştirme içindir. Canlı ortamda `dotnet user-secrets` veya ortam değişkenleri (`JwtAyarlari__GizliAnahtar`) kullanılmalıdır.

## Proje Dokümantasyonu

Proje geliştirme sürecine ait teknik raporlar `docs/` klasöründe yer almaktadır:
- Clean Code Standartları Raporu
- Agile & Scrum Metodolojisi Raporu
- DevOps & CI/CD Raporu
