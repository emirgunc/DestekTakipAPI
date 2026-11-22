# DestekTakip™ Enterprise B2B - Ticari Ürün, Kurulum ve Satış El Kitabı

**Sürüm:** v2.4.0 Enterprise Commercial Release  
**Hedef Kitle:** KOBİ'ler, Yazılım Şirketleri, Çağrı Merkezleri, Kurumsal Destek Ekipleri  
**Tavsiye Edilen Satış Fiyatı:** 25.000 TL – 50.000 TL (Tek Seferlik Kurumsal Lisans) + Yıllık %20 Bakım/Destek  

---

## 1. Ürün Özeti & Değer Önerisi (Value Proposition)

DestekTakip, şirketlerin iç veya dış müşteri taleplerini uçtan uca, kurumsal standartlarda yönetmesini sağlayan modern, yüksek performanslı ve güvenli bir **Helpdesk & Destek Yönetim Platformu**'dur.

Aylık kullanıcı başı yüksek dolar bazlı abonelik (SaaS) ücreti ödemek istemeyen işletmeler için **On-Premise (Kendi Sunucunda / Yerel Ağda Barındırılabilir)** veya özel bulut ortamlarında çalışan **anahtar teslim (turnkey)** bir çözümdür.

### Temel Ticari Avantajlar
1. **Sıfır Aylık Maliyet:** Tek seferlik lisans ile sınırsız kullanıcı ve sınırsız talep yönetimi.
2. **Veri Gizliliği & Güvenliği:** Tüm veriler şirketin kendi SQL Server veritabanında saklanır; 3. parti bulut sağlayıcılarına veri akışı olmaz (KVKK / GDPR %100 uyumlu).
3. **Tam White-Labeling (Özelleştirilebilir Marka):** Müşteri kendi şirket adını, logosunu, sistem başlığını ve alt bilgisini tek tıkla uygulayabilir.
4. **Hibrit Kullanım:** Hem modern masaüstü uygulaması (Windows Desktop .exe) hem de yerel ağ veya web üzerinden tarayıcıyla erişilebilir.

---

## 2. Modüler Mimari & Özellik Matrisi

| Modül | Özellikler | İşletmeye Faydası |
|---|---|---|
| **Modül 1: Dosya / Ek / Kanıt Yönetimi** | Ekran görüntüleri (PNG, JPG), PDF belgeleri, sistem logları (.txt, .log) yükleme, önizleme ve indirme. | Taleplerin çözüm süresini %40 hızlandırır, iletişim karmaşasını önler. |
| **Modül 2: SLA & Dışa Aktarma Motoru** | Önceliğe göre otomatik SLA süre hesaplama (Kritik: 4s, Yüksek: 8s, Orta: 24s, Düşük: 48s). Süre aşım uyarıları. UTF-8 BOM Excel/CSV rapor indirme. | Hizmet kalitesi taahhütlerini (SLA) garanti altına alır, yönetim raporlamasını saniyelere indirir. |
| **Modül 3: E-posta & SMTP Bildirim Altyapısı** | HTML kurumsal bildirim motoru. Talep oluşturulduğunda, durum güncellendiğinde ve yeni yanıt yazıldığında otomatik e-posta gönderimi. Canlı SMTP test aracı. | Müşteriyi ve personeli anlık bilgilendirir, telefon trafiğini %60 azaltır. |
| **Modül 4: White-Labeling & Kurumsal Ayarlar** | Şirket adı, sistem başlığı, logo URL'si, destek telefon & e-posta ve alt bilgi metnini panelden değiştirme. | Yazılımı satın alan şirketin kendi öz malı gibi kurumsal kimliğine büründürür. |

---

## 3. Teknik Mimari & Standartlar

- **Backend:** .NET 9 Web API (Clean Architecture, Controller-Service Pattern, Strongly-Typed Settings)
- **Veritabanı:** Microsoft SQL Server 2022 (EF Core 9, Stored Procedures, Views, Scalar Functions, Cascade/Restrict Relations)
- **Ön Yüz:** Next.js 16 (Turbopack, Standalone Production Build, Tailwind CSS)
- **Masaüstü:** Electron 36 (Native Windows Frameless/Splash, Embedded Node Runtime)
- **Güvenlik:** JWT Bearer Authentication (HS256 256-bit Key), BCrypt Şifreleme, Role-Based Access Control (Yönetici vs. Kullanıcı)
- **Test Kapsamı:** xUnit & EF Core InMemory Veritabanı ile 30/30 Yeşil Birim Testi (%100 Başarı Oranı)

---

## 4. Kurulum & Canlıya Alma Rehberi (Deployment Guide)

### Adım 1: Veritabanı Hazırlığı
1. Sunucuda veya yerel bilgisayarda Microsoft SQL Server (veya SQL Express) kurulu olmalıdır.
2. `DestekTakipDB` veritabanı oluşturulur.
3. Tablolar, stored procedure ve view script'leri çalıştırılır.

### Adım 2: API Yapılandırması
`appsettings.json` dosyasında veritabanı ve SMTP ayarları girilir:
```json
{
  "ConnectionStrings": {
    "DestekTakipBaglantisi": "Server=.\\SQLEXPRESS;Database=DestekTakipDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "EpostaAyarlari": {
    "Sunucu": "smtp.sirketiniz.com",
    "Port": 587,
    "KullaniciAdi": "destek@sirketiniz.com",
    "Sifre": "GucluSifre123!",
    "GonderenEposta": "destek@sirketiniz.com",
    "GonderenAdi": "Müşteri Destek Sistemi",
    "SslKullan": true,
    "EtkinMi": true
  }
}
```

### Adım 3: Başlatma & Giriş
1. `DestekTakip_Baslat.bat` veya masaüstü kısayolu çift tıklanarak çalıştırılır.
2. İlk giriş için hazır kurumsal yönetici hesabı:
   - **E-posta:** `ali@test.com`
   - **Şifre:** `Test123!`
   - **Rol:** Sistem Yöneticisi
3. Giriş yapıldıktan sonra sol menüden **"Kurumsal Ayarlar"** sayfasına gidilerek şirket adı, logosu ve iletişim bilgileri kaydedilir.

---

## 5. Satış ve Fiyatlandırma Teklifi Şablonu

### Paket A: Standart Paket (25.000 TL)
- DestekTakip v2.4 Tek Sunucu Lisansı
- Sınırsız Kullanıcı ve Sınırsız Talep
- Dosya Yükleme & Excel Dışa Aktarma
- 3 Ay E-posta Teknik Destek

### Paket B: Profesyonel / White-Label Paket (40.000 TL)
- DestekTakip v2.4 Kurumsal Lisans
- Müşteriye Özel Marka, Logo ve Renk Özelleştirmesi
- SMTP Kurumsal E-posta Entegrasyonu
- Yerinde / Uzaktan Sunucu Kurulum Hizmeti
- 1 Yıl Güncelleme ve Öncelikli Destek

---
*DestekTakip™ Enterprise B2B - Kurumsal Yazılım Çözümleri.*
