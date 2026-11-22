# 1. Hafta: Temiz Kod (Clean Code) Standartları ve Prensipleri Araştırma Raporu

**Hazırlayan:** Stajyer / Yazılım Geliştirici  
**Proje:** Yazılım Destek Takip Sistemi  
**Tarih:** 1. Hafta  

---

## 1. Giriş
Yazılım geliştirmede kodun sadece "çalışması" yeterli değildir. Robert C. Martin'in (Uncle Bob) ifade ettiği gibi: *"Herhangi bir aptal bilgisayarın anlayabileceği kod yazabilir. İyi programcılar insanların anlayabileceği kod yazar."* Bu rapor, 1. hafta kapsamında araştırılan kodlama standartlarını, Clean Code ilkelerini ve bu ilkelerin Yazılım Destek Takip Sistemi projesinde nasıl uygulandığını özetlemektedir.

## 2. Temel Clean Code İlkeleri

### 2.1 Anlamlı İsimlendirme (Meaningful Names)
- **Açıklayıcı ve Amaç Bildiren İsimler:** Değişken, metot ve sınıflar ne yaptıklarını ve ne amaçla kullanıldıklarını açıkça belirtmelidir. `d` yerine `gecenGunSayisi`, `chk()` yerine `KullaniciAktifMi()` tercih edilmelidir.
- **Telaffuz Edilebilir ve Aranabilir İsimler:** Özel kısaltmalardan kaçınılmalı, projenin tamamında tek tip bir terminoloji (ubiquitous language) benimsenmelidir.
- **Macar Notasyonundan Kaçınma:** Değişken türlerini adın önüne eklemek (örn. `strBaslik`, `iTalepId`) modern IDE'ler çağında kod kirliliği yaratır; bundan kaçınılmalıdır.

### 2.2 Fonksiyonlar ve Metotlar (Functions)
- **Küçük Olmalı (Small):** Bir fonksiyon ideal olarak 20 satırı geçmemelidir.
- **Tek Bir İş Yapmalı (Do One Thing - SRP):** Bir metot yalnızca tek bir sorumluluğa sahip olmalı ve onu en iyi şekilde yapmalıdır.
- **Tek Düzey Soyutlama (One Level of Abstraction):** Bir fonksiyon içerisindeki tüm ifadeler aynı soyutlama düzeyinde olmalıdır.
- **Az Sayıda Parametre:** İdeal fonksiyon parametre sayısı 0-2 arasıdır. 3'ten fazla parametre gerekiyorsa DTO / Obje modeline sarılmalıdır.
- **Yan Etkilerden Kaçınma (No Side Effects):** Bir fonksiyon gizlice sınıf durumunu veya global değişkenleri değiştirmemelidir.

### 2.3 SOLID Prensipleri
1. **S - Single Responsibility Principle (Tek Sorumluluk Prensibi):** Bir sınıfın değişmesi için yalnızca tek bir nedeni olmalıdır.
2. **O - Open/Closed Principle (Açık/Kapalı Prensibi):** Yazılım varlıkları geliştirmeye açık, ancak değişime kapalı olmalıdır.
3. **L - Liskov Substitution Principle (Liskov Yerine Geçme):** Alt sınıflar, üst sınıfların yerine kullanılabilmelidir.
4. **I - Interface Segregation Principle (Arayüz Ayrımı):** İstemciler kullanmadıkları arayüzleri uygulamaya zorlanmamalıdır.
5. **D - Dependency Inversion Principle (Bağımlılıkların Tersine Çevrilmesi):** Üst seviye modüller alt seviye modüllere bağımlı olmamalı; her ikisi de soyutlamalara (interfaces) dayanmalıdır.

### 2.4 Hata Yönetimi (Error Handling)
- Hata kodları (`return -1`) yerine **Exceptions** kullanılmalıdır.
- İstisnalar yakalanıp yutulmamalı (`catch (Exception) {}`), loglanmalı veya üst katmana taşınmalıdır.
- ASP.NET Core projelerinde Controller'ları kirletmek yerine **Global Exception Middleware** kullanılmalıdır.

## 3. Destek Takip Sistemi Projesindeki Uygulamalar
- Projede **Clean Architecture** (API, Application, Domain, Infrastructure) mimari ayrımı benimsenmiştir.
- HTTP istek ve yanıtlarında doğrudan Entity değil, **DTO (Data Transfer Object)** modelleri kullanılmıştır.
- Tüm loglama işlemleri `ILogger<T>` abstraction'ı üzerinden yapılandırılmıştır.
