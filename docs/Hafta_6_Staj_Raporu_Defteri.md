# 6. Hafta: 6 Haftalık Staj Defteri ve Nihai Çalışma Raporu

**Öğrenci Adı / Stajyer:** Muhammet Emir Güner  
**Kurum / Proje:** Yazılım Destek Takip Sistemi (Full-Stack & Desktop)  
**Kullanılan Teknolojiler:** C# .NET 9 Web API, Entity Framework Core, MS SQL Server, Next.js, TypeScript, TailwindCSS, Electron, xUnit, GitHub Actions CI/CD  

---

## HAFTA HAFTA ÇALIŞMA ÖZETİ

### 1. Hafta: Oryantasyon ve Clean Code Standartları
- Şirket çalışma kültürü, geliştirme ortamları (Visual Studio, Antigravity IDE, Git) ile tanışıldı.
- Clean Code (Temiz Kod) standartları ve SOLID prensipleri derinlemesine incelendi.
- C# isimlendirme standartları, guard clause yapıları ve katmanlı mimari üzerine araştırma raporu hazırlandı.

### 2. Hafta: MS SQL Server Mimarisi ve Veritabanı Tasarımı
- Destek Takip Sistemi için ilişkisel veritabanı şeması modellendi (`DestekTakipDB`).
- Tablolar oluşturuldu: `Roller`, `Kullanicilar`, `Kategoriler`, `TalepOncelikleri`, `TalepDurumlari`, `DestekTalepleri`, `TalepYorumlari`, `TalepDegisiklikGecmisi`.
- İleri SQL nesneleri geliştirildi:
  - **View:** `VW_AktifTalepDetaylari` (Aktif talepler ve açık gün sayısı hesaplaması)
  - **Function:** `FN_KullanicininAktifTalepSayisi` (Skalar fonksiyon)
  - **Stored Procedure:** `SP_YeniTalepOlustur` (İş kurallı talep oluşturma)
  - **Trigger:** `TR_DestekTalepleri_GuncellemeGecmisi` (Durum ve atama değişikliklerini otomatik tarihçe tablosuna kaydetme)
- Seed verileri (BCrypt hashli test kullanıcıları) eklendi.

### 3. Hafta: C# ile RESTful API Mimarisi ve Temel CRUD
- .NET 9 SDK ile ASP.NET Core Web API projesi Clean Architecture (API, Application, Domain, Infrastructure) prensiplerine göre yapılandırıldı.
- Entity Framework Core DbContext ve Fluent API konfigürasyonları yazıldı.
- Destek Talepleri ve Kategoriler için Controller'lar ve DTO modelleri oluşturuldu.
- Swagger UI (OpenAPI) entegrasyonu yapıldı.

### 4. Hafta: Authentication, Authorization (JWT) ve Hata Yönetimi
- JWT (JSON Web Token) altyapısı kuruldu, token üretim ve doğrulama mekanizmaları yazıldı.
- Rol bazlı yetkilendirme (`[Authorize(Roles = "Yonetici")]`) uygulandı (Kullanıcı yalnızca kendi taleplerini görür/yönetir, Yönetici tüm talepleri yönetir).
- Şifreleme için endüstri standardı `BCrypt.Net-Next` kullanıldı.
- Merkezi hata yakalama için `GlobalHataYoneticisiMiddleware` yazıldı.
- Talebe yorum ekleme (`POST /api/destektalepleri/{id}/yorumlar`) ve tarihçe görüntüleme (`GET /api/destektalepleri/{id}/gecmis`) endpointleri tamamlandı.
- SQL View ve Function mantığı `GET /api/destektalepleri/istatistikler` endpoint'ine entegre edildi.
- xUnit ve EF Core InMemory kullanılarak Controller'lar için 20 adet birim test yazıldı ve %100 başarıyla çalıştırıldı.

### 5. Hafta: DevOps, Agile/Scrum ve CI/CD Pipeline
- DevOps kültürü ve Agile/Scrum metodolojileri araştırıldı, kurumsal raporları hazırlandı.
- GitHub Actions üzerinde `.github/workflows/dotnet.yml` dosyası ile sürekli entegrasyon (CI) pipeline'ı kuruldu. Her push ve pull request'te kodun otomatik derlenmesi ve testlerin koşması sağlandı.

### 6. Hafta: UI Entegrasyonu, Masaüstü Paketleme ve Sunum
- Next.js ve TailwindCSS ile modern, karanlık mod (dark theme) destekli web arayüzü kodlandı.
- Canlı istatistik kartları, filtreleme, yeni talep açma ve talep detayında yorum akışı ekranları yapıldı.
- Electron ve electron-builder kullanılarak hem API hem UI tek bir Windows kurulum paketine (`DestekTakip.exe`) dönüştürüldü.
- Staj raporu ve proje sunumu tamamlanarak teslim aşamasına getirildi.
