# 5. Hafta: DevOps Kültürü ve CI/CD Pipeline Araştırma Raporu

**Hazırlayan:** Stajyer / Yazılım Geliştirici  
**Proje:** Yazılım Destek Takip Sistemi  
**Tarih:** 5. Hafta  

---

## 1. DevOps Nedir?
DevOps, Yazılım Geliştirme (Development) ve BT Operasyonları (Operations) ekipleri arasındaki iletişimi, iş birliğini ve entegrasyonu otomatikleştiren kültürel bir felsefe ve pratikler bütünüdür. Amacı; yazılım teslim döngüsünü kısaltmak, dağıtım sıklığını artırmak ve daha güvenilir sürümler sunmaktır.

## 2. Sürekli Entegrasyon ve Sürekli Dağıtım (CI/CD)

### 2.1 Sürekli Entegrasyon (Continuous Integration - CI)
Geliştiricilerin kod değişikliklerini düzenli aralıklarla merkezi bir depoya (GitHub, GitLab vb.) göndermesi ve her gönderimde otomatik derleme (build) ve otomatik birim testlerin (unit tests) çalıştırılması sürecidir.
- **Faydaları:** Entegrasyon hatalarının anında tespit edilmesi, kod kalitesinin otomatik korunması.

### 2.2 Sürekli Dağıtım / Teslimat (Continuous Delivery / Deployment - CD)
Testlerden başarıyla geçen kod paketlerinin otomatik olarak test (staging) veya canlı (production) ortamlara dağıtılmasıdır.

## 3. Destek Takip Sistemi Projesinde GitHub Actions Uygulaması
Projemizde `.github/workflows/dotnet.yml` dosyası ile CI pipeline'ı kurulmuştur:

```yaml
name: DestekTakipAPI CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: NuGet Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --configuration Release
      - name: Run Tests
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"
```

Bu pipeline sayesinde:
1. Her PR veya commit'te .NET 9 ortamı Ubuntu üzerinde ayağa kalkar.
2. NuGet paketleri restore edilir.
3. Proje Release modunda derlenir.
4. Yazdığımız 20 adet xUnit testi otomatik koşturulur; tek bir test dahi başarısız olursa build kırılır ve hatalı kodun birleşmesi engellenir.
