-- =============================================================================
-- DestekTakipDB - Veritabani Kurulum Scripti
-- Calistirmak icin: sqlcmd -S .\SQLEXPRESS -E -i 00_KurulumTam.sql
-- =============================================================================

USE master;
GO

-- Varsa sil, yeniden olustur
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'DestekTakipDB')
BEGIN
    ALTER DATABASE DestekTakipDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DestekTakipDB;
END
GO

CREATE DATABASE DestekTakipDB;
GO

USE DestekTakipDB;
GO

-- =============================================================================
-- TABLOLAR
-- =============================================================================

CREATE TABLE dbo.Roller (
    RolId           INT IDENTITY(1,1) PRIMARY KEY,
    RolAdi          NVARCHAR(50)  NOT NULL UNIQUE,
    Aciklama        NVARCHAR(255) NULL,
    OlusturmaTarihi DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.Kullanicilar (
    KullaniciId     INT IDENTITY(1,1) PRIMARY KEY,
    Ad              NVARCHAR(100) NOT NULL,
    Soyad           NVARCHAR(100) NOT NULL,
    Eposta          NVARCHAR(255) NOT NULL UNIQUE,
    SifreHash       NVARCHAR(255) NOT NULL,
    RolId           INT           NOT NULL,
    AktifMi         BIT           NOT NULL DEFAULT 1,
    OlusturmaTarihi DATETIME      NOT NULL DEFAULT GETDATE(),
    GuncellemeTarihi DATETIME     NULL,
    CONSTRAINT FK_Kullanicilar_Roller FOREIGN KEY (RolId) REFERENCES dbo.Roller(RolId)
);

CREATE TABLE dbo.Kategoriler (
    KategoriId  INT IDENTITY(1,1) PRIMARY KEY,
    KategoriAdi NVARCHAR(100) NOT NULL UNIQUE,
    Aciklama    NVARCHAR(500) NULL,
    AktifMi     BIT           NOT NULL DEFAULT 1
);

CREATE TABLE dbo.TalepOncelikleri (
    OncelikId  INT IDENTITY(1,1) PRIMARY KEY,
    OncelikAdi NVARCHAR(50) NOT NULL UNIQUE,
    SiraNo     INT          NOT NULL
);

CREATE TABLE dbo.TalepDurumlari (
    DurumId  INT IDENTITY(1,1) PRIMARY KEY,
    DurumAdi NVARCHAR(50)  NOT NULL UNIQUE,
    Aciklama NVARCHAR(255) NULL
);

CREATE TABLE dbo.DestekTalepleri (
    TalepId              INT IDENTITY(1,1) PRIMARY KEY,
    Baslik               NVARCHAR(500) NOT NULL,
    Aciklama             NVARCHAR(MAX) NOT NULL,
    OlusturanKullaniciId INT           NOT NULL,
    AtananKullaniciId    INT           NULL,
    KategoriId           INT           NOT NULL,
    OncelikId            INT           NOT NULL,
    DurumId              INT           NOT NULL,
    OlusturmaTarihi      DATETIME      NOT NULL DEFAULT GETDATE(),
    GuncellemeTarihi     DATETIME      NULL,
    KapanisTarihi        DATETIME      NULL,
    CONSTRAINT FK_DT_OlusturanKullanici FOREIGN KEY (OlusturanKullaniciId)
        REFERENCES dbo.Kullanicilar(KullaniciId),
    CONSTRAINT FK_DT_AtananKullanici FOREIGN KEY (AtananKullaniciId)
        REFERENCES dbo.Kullanicilar(KullaniciId),
    CONSTRAINT FK_DT_Kategori   FOREIGN KEY (KategoriId)  REFERENCES dbo.Kategoriler(KategoriId),
    CONSTRAINT FK_DT_Oncelik    FOREIGN KEY (OncelikId)   REFERENCES dbo.TalepOncelikleri(OncelikId),
    CONSTRAINT FK_DT_Durum      FOREIGN KEY (DurumId)     REFERENCES dbo.TalepDurumlari(DurumId)
);

CREATE TABLE dbo.TalepYorumlari (
    YorumId          INT IDENTITY(1,1) PRIMARY KEY,
    TalepId          INT           NOT NULL,
    YazanKullaniciId INT           NOT NULL,
    YorumMetni       NVARCHAR(MAX) NOT NULL,
    OlusturmaTarihi  DATETIME      NOT NULL DEFAULT GETDATE(),
    DahiliMi         BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_TY_Talep    FOREIGN KEY (TalepId)          REFERENCES dbo.DestekTalepleri(TalepId),
    CONSTRAINT FK_TY_Kullanici FOREIGN KEY (YazanKullaniciId) REFERENCES dbo.Kullanicilar(KullaniciId)
);

CREATE TABLE dbo.TalepDegisiklikGecmisi (
    GecmisId               INT IDENTITY(1,1) PRIMARY KEY,
    TalepId                INT           NOT NULL,
    EskiDurumId            INT           NULL,
    YeniDurumId            INT           NULL,
    EskiOncelikId          INT           NULL,
    YeniOncelikId          INT           NULL,
    EskiAtananKullaniciId  INT           NULL,
    YeniAtananKullaniciId  INT           NULL,
    DegistirmeTarihi       DATETIME      NOT NULL DEFAULT GETDATE(),
    DegisiklikAciklamasi   NVARCHAR(500) NULL,
    CONSTRAINT FK_TDG_Talep FOREIGN KEY (TalepId) REFERENCES dbo.DestekTalepleri(TalepId)
);
GO

-- =============================================================================
-- VIEW
-- =============================================================================
CREATE VIEW dbo.VW_AktifTalepDetaylari AS
SELECT
    dt.TalepId,
    dt.Baslik,
    k.KategoriAdi,
    (ok.Ad + ' ' + ok.Soyad) AS OlusturanAdi,
    (ak.Ad + ' ' + ak.Soyad) AS AtananAdi,
    tp.OncelikAdi,
    td.DurumAdi,
    dt.OlusturmaTarihi,
    DATEDIFF(DAY, dt.OlusturmaTarihi, GETDATE()) AS AcikGunSayisi
FROM dbo.DestekTalepleri dt
INNER JOIN dbo.Kullanicilar ok ON dt.OlusturanKullaniciId = ok.KullaniciId
LEFT  JOIN dbo.Kullanicilar ak ON dt.AtananKullaniciId    = ak.KullaniciId
INNER JOIN dbo.Kategoriler  k  ON dt.KategoriId           = k.KategoriId
INNER JOIN dbo.TalepOncelikleri tp ON dt.OncelikId        = tp.OncelikId
INNER JOIN dbo.TalepDurumlari   td ON dt.DurumId          = td.DurumId
WHERE td.DurumAdi <> 'Kapatildi';
GO

-- =============================================================================
-- FUNCTION
-- =============================================================================
CREATE FUNCTION dbo.FN_KullanicininAktifTalepSayisi (@kullaniciId INT)
RETURNS INT AS
BEGIN
    DECLARE @sayi INT;
    SELECT @sayi = COUNT(*)
    FROM dbo.DestekTalepleri dt
    INNER JOIN dbo.TalepDurumlari td ON dt.DurumId = td.DurumId
    WHERE dt.OlusturanKullaniciId = @kullaniciId
      AND td.DurumAdi <> 'Kapatildi';
    RETURN ISNULL(@sayi, 0);
END;
GO

-- =============================================================================
-- STORED PROCEDURE
-- =============================================================================
CREATE PROCEDURE dbo.SP_YeniTalepOlustur
    @baslik               NVARCHAR(500),
    @aciklama             NVARCHAR(MAX),
    @olusturanKullaniciId INT,
    @kategoriId           INT,
    @oncelikId            INT,
    @yeniTalepId          INT OUTPUT,
    @basariMi             BIT OUTPUT,
    @hataMesaji           NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @basariMi   = 0;
    SET @hataMesaji = '';

    IF NOT EXISTS (SELECT 1 FROM dbo.Kullanicilar WHERE KullaniciId = @olusturanKullaniciId AND AktifMi = 1)
    BEGIN
        SET @hataMesaji = 'Gecersiz kullanici.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Kategoriler WHERE KategoriId = @kategoriId AND AktifMi = 1)
    BEGIN
        SET @hataMesaji = 'Gecersiz kategori.';
        RETURN;
    END

    DECLARE @acikDurumId INT;
    SELECT @acikDurumId = DurumId FROM dbo.TalepDurumlari WHERE DurumAdi = 'Acik';

    INSERT INTO dbo.DestekTalepleri
        (Baslik, Aciklama, OlusturanKullaniciId, KategoriId, OncelikId, DurumId)
    VALUES
        (@baslik, @aciklama, @olusturanKullaniciId, @kategoriId, @oncelikId, @acikDurumId);

    SET @yeniTalepId = SCOPE_IDENTITY();
    SET @basariMi    = 1;
END;
GO

-- =============================================================================
-- TRIGGER
-- =============================================================================
CREATE TRIGGER TR_DestekTalepleri_GuncellemeGecmisi
ON dbo.DestekTalepleri
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(DurumId) OR UPDATE(OncelikId) OR UPDATE(AtananKullaniciId)
    BEGIN
        INSERT INTO dbo.TalepDegisiklikGecmisi
            (TalepId, EskiDurumId, YeniDurumId, EskiOncelikId, YeniOncelikId,
             EskiAtananKullaniciId, YeniAtananKullaniciId, DegistirmeTarihi)
        SELECT
            i.TalepId,
            d.DurumId,             i.DurumId,
            d.OncelikId,           i.OncelikId,
            d.AtananKullaniciId,   i.AtananKullaniciId,
            GETDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.TalepId = d.TalepId;
    END
END;
GO

-- =============================================================================
-- SEED DATA
-- =============================================================================

-- Roller
INSERT INTO dbo.Roller (RolAdi, Aciklama) VALUES
    ('Kullanici', 'Destek talebi olusturabilen standart kullanici.'),
    ('Yonetici',  'Tum taleplere erisip yonetebilen yetkililer.');

-- Oncelikler
INSERT INTO dbo.TalepOncelikleri (OncelikAdi, SiraNo) VALUES
    ('Dusuk',   4),
    ('Orta',    3),
    ('Yuksek',  2),
    ('Kritik',  1);

-- Durumlar
INSERT INTO dbo.TalepDurumlari (DurumAdi, Aciklama) VALUES
    ('Acik',         'Yeni olusturulan, atama bekleyen talep.'),
    ('Isleniyor',    'Uzerinde calisilan talep.'),
    ('Beklemede',    'Ek bilgi bekleniyor.'),
    ('Cozuldu',      'Cozum uygulanmis, dogrulama bekleniyor.'),
    ('Kapatildi',    'Talep tamamlanip kapatilmis.');

-- Kategoriler
INSERT INTO dbo.Kategoriler (KategoriAdi, Aciklama, AktifMi) VALUES
    ('Yazilim',  'Yazilim ve uygulama sorunlari.',    1),
    ('Donanim',  'Bilgisayar ve fiziksel ekipman.',   1),
    ('Ag',       'Internet ve ag baglantisi.',        1),
    ('Guvenlik', 'Sifre ve erisim yetki sorunlari.',  1),
    ('Diger',    'Diger kategorilere girmeyen konular.', 1);

-- Kullanicilar (Sifre: Test123!)
-- Hash: $2a$11$sy4L3SBCsklMAhC2srnAqOxIJYipkqTWc9oepogMUDpTs5mdzxTe2
INSERT INTO dbo.Kullanicilar (Ad, Soyad, Eposta, SifreHash, RolId, AktifMi) VALUES
    ('Ali',    'Yilmaz', 'ali@test.com',    '$2a$11$sy4L3SBCsklMAhC2srnAqOxIJYipkqTWc9oepogMUDpTs5mdzxTe2', 2, 1),
    ('Ayse',   'Kaya',   'ayse@test.com',   '$2a$11$sy4L3SBCsklMAhC2srnAqOxIJYipkqTWc9oepogMUDpTs5mdzxTe2', 1, 1),
    ('Mehmet', 'Demir',  'mehmet@test.com', '$2a$11$sy4L3SBCsklMAhC2srnAqOxIJYipkqTWc9oepogMUDpTs5mdzxTe2', 1, 1);

-- Ornek destek talepleri
DECLARE @yoneticiId INT = (SELECT KullaniciId FROM dbo.Kullanicilar WHERE Eposta = 'ali@test.com');
DECLARE @kullanici1 INT = (SELECT KullaniciId FROM dbo.Kullanicilar WHERE Eposta = 'ayse@test.com');
DECLARE @kullanici2 INT = (SELECT KullaniciId FROM dbo.Kullanicilar WHERE Eposta = 'mehmet@test.com');

DECLARE @yazilimKat  INT = (SELECT KategoriId FROM dbo.Kategoriler WHERE KategoriAdi = 'Yazilim');
DECLARE @donarimKat  INT = (SELECT KategoriId FROM dbo.Kategoriler WHERE KategoriAdi = 'Donanim');
DECLARE @yuksekOnc   INT = (SELECT OncelikId  FROM dbo.TalepOncelikleri WHERE OncelikAdi = 'Yuksek');
DECLARE @ortaOnc     INT = (SELECT OncelikId  FROM dbo.TalepOncelikleri WHERE OncelikAdi = 'Orta');
DECLARE @acikDurum   INT = (SELECT DurumId    FROM dbo.TalepDurumlari WHERE DurumAdi = 'Acik');
DECLARE @islDurum    INT = (SELECT DurumId    FROM dbo.TalepDurumlari WHERE DurumAdi = 'Isleniyor');

INSERT INTO dbo.DestekTalepleri
    (Baslik, Aciklama, OlusturanKullaniciId, AtananKullaniciId, KategoriId, OncelikId, DurumId, OlusturmaTarihi)
VALUES
    ('Yazici calısmiyor',
     'Ofis yazicisi hic tepki vermiyor, surucu guncellemesi denedim.',
     @kullanici1, @yoneticiId, @donarimKat, @yuksekOnc, @islDurum,
     DATEADD(DAY, -3, GETDATE())),

    ('VPN baglantisi kurulamiyor',
     'Evden calisirken VPN baglantisi zaman asimina ugriyor.',
     @kullanici2, NULL, @yazilimKat, @ortaOnc, @acikDurum,
     DATEADD(DAY, -1, GETDATE())),

    ('Outlook e-posta gonderemiyor',
     'Mail gonderdikten sonra Gonderilen Kutusunda gorunmuyor.',
     @kullanici1, NULL, @yazilimKat, @ortaOnc, @acikDurum,
     GETDATE());

PRINT 'DestekTakipDB basariyla kuruldu!';
PRINT 'Test kullanicilari: ali@test.com / ayse@test.com / mehmet@test.com';
PRINT 'Ortak sifre: Test123!';
GO
