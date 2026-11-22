# 6. Hafta: Destek Takip Sistemi Proje Sunum Rehberi

Bu rehber, mentörünüze (abiye), staj komisyonuna veya jüriye projeyi sunarken adım adım izleyeceğiniz akışı içerir.

---

## 1. Sunum Girişi (1 Dakika)
- "Merhaba, 6 haftalık staj programı kapsamında geliştirdiğim **Yazılım Destek Takip Sistemi (Helpdesk / Issue Tracker)** projesini sunacağım."
- "Proje; kurumsal şirketlerin BT, yazılım veya teknik destek ekiplerinin müşteri ve personel taleplerini kayıt altına almasını, önceliklendirmesini ve çözüme kavuşturmasını sağlayan uçtan uca modern bir platformdur."

## 2. Mimari ve Teknoloji Yığını (2 Dakika)
- **Backend:** C# .NET 9 Web API, Clean Architecture (Domain, Application, Infrastructure, API).
- **Veritabanı:** MS SQL Server (İlişkisel şema, View, Trigger, Stored Procedure, Function).
- **Güvenlik:** JWT Bearer Token, BCrypt şifre hashleme, Rol Bazlı Yetkilendirme (RBAC - Yönetici/Kullanıcı).
- **Frontend:** Next.js (App Router), TypeScript, TailwindCSS v4.
- **Masaüstü:** Electron ile Windows NSIS paketleme.
- **DevOps & Kalite:** xUnit birim testleri (20 adet test), GitHub Actions CI/CD Pipeline.

## 3. Canlı Demo (Live Demonstration) (3 Dakika)
1. **Giriş Ekranı:**
   - `ayse@test.com` (Standart Kullanıcı) ile giriş yapın.
   - Sadece kendi açtığı talepleri gördüğünü gösterin.
   - "+ Yeni Talep" butonuna tıklayıp bir arıza talebi oluşturun.
2. **Yönetici Ekranı:**
   - Çıkış yapıp `ali@test.com` (Yönetici) ile giriş yapın.
   - Sayfa üstündeki istatistik kartlarını gösterin (SQL Function ve View ile beslenen toplam/aktif/çözülen sayılar).
   - Az önce açılan talebe tıklayın, durumunu "Açık"tan "İşleniyor"a getirin.
   - Yorum kısmına *"Sorun inceleniyor, teknik personel yönlendirildi"* yazıp yorum gönderin.
3. **Tarihçe ve Trigger:**
   - Yapılan durum değişikliğinin SQL Trigger'ı tarafından otomatik olarak `TalepDegisiklikGecmisi`ne yazıldığını ve API'den okunduğunu belirtin.

## 4. Testler ve CI/CD Gösterimi (1 Dakika)
- Terminalde `dotnet test` komutunu çalıştırın ve 20 testin de yeşil yandığını gösterin.
- GitHub Actions dosyasını (`.github/workflows/dotnet.yml`) açıp her kod gönderiminde testlerin bulutta otomatik çalıştığını ifade edin.

## 5. Kapanış ve Soru-Cevap
- "Tüm 6 haftalık isterleri eksiksiz tamamlamış bulunmaktayım. Sorularınız varsa yanıtlamaktan memnuniyet duyarım."
