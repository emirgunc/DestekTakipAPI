using System.Net;
using System.Net.Mail;
using DestekTakipAPI.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DestekTakipAPI.Infrastructure.Services;

/// <summary>
/// E-posta bildirim servisi sozlesmesi.
/// </summary>
public interface IEpostaServisi
{
    Task TalepOlusturulduBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik);
    Task TalepDurumuGuncellendiBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yeniDurum);
    Task YeniYorumBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yazanAd, string yorumMetni);
}

/// <summary>
/// SMTP uzerinden kurumsal HTML e-posta bildirimleri gonderen servis.
/// </summary>
public class EpostaServisi : IEpostaServisi
{
    private readonly EpostaAyarlariSecenekleri _ayarlar;
    private readonly ILogger<EpostaServisi> _logger;

    public EpostaServisi(IOptions<EpostaAyarlariSecenekleri> ayarlar, ILogger<EpostaServisi> logger)
    {
        _ayarlar = ayarlar.Value;
        _logger = logger;
    }

    public async Task TalepOlusturulduBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik)
    {
        string konu = $"[DestekTakip] Talebiniz Alindi (#{talepId}): {baslik}";
        string html = $@"
            <div style='font-family: Arial, sans-serif; background: #0b172a; color: #f1f5f9; padding: 24px; border-radius: 12px;'>
                <h2 style='color: #38bdf8; margin-top: 0;'>Destek Talebiniz Basariyla Alindi</h2>
                <p>Sayin <b>{aliciAd}</b>,</p>
                <p><b>#{talepId}</b> numarali destek talebiniz sisteme kaydedilmis olup incelemeye alinmistir.</p>
                <div style='background: #1e293b; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                    <p style='margin: 0; color: #94a3b8; font-size: 13px;'>Talep Basligi:</p>
                    <p style='margin: 4px 0 0; font-size: 15px; font-weight: bold;'>{baslik}</p>
                </div>
                <p style='color: #94a3b8; font-size: 13px;'>Destek ekibimiz en kisa surede talebinizi yanitlayacaktir.</p>
                <hr style='border: 0; border-top: 1px solid #334155; margin: 20px 0;' />
                <p style='color: #64748b; font-size: 11px;'>© DestekTakip Kurumsal Musteri Destek Sistemi</p>
            </div>";

        await EpostaGonderAsync(aliciEposta, konu, html);
    }

    public async Task TalepDurumuGuncellendiBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yeniDurum)
    {
        string konu = $"[DestekTakip] Durum Guncellendi (#{talepId}): {yeniDurum}";
        string html = $@"
            <div style='font-family: Arial, sans-serif; background: #0b172a; color: #f1f5f9; padding: 24px; border-radius: 12px;'>
                <h2 style='color: #38bdf8; margin-top: 0;'>Destek Talebinizin Durumu Guncellendi</h2>
                <p>Sayin <b>{aliciAd}</b>,</p>
                <p><b>#{talepId}</b> numarali talebinizin durumu guncellendi: <span style='background: #2563eb; color: white; padding: 3px 8px; border-radius: 6px; font-size: 12px;'>{yeniDurum}</span></p>
                <div style='background: #1e293b; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                    <p style='margin: 0; color: #94a3b8; font-size: 13px;'>Talep Basligi:</p>
                    <p style='margin: 4px 0 0; font-size: 15px; font-weight: bold;'>{baslik}</p>
                </div>
                <p style='color: #94a3b8; font-size: 13px;'>Detaylari gormek icin musteri portalina giris yapabilirsiniz.</p>
                <hr style='border: 0; border-top: 1px solid #334155; margin: 20px 0;' />
                <p style='color: #64748b; font-size: 11px;'>© DestekTakip Kurumsal Musteri Destek Sistemi</p>
            </div>";

        await EpostaGonderAsync(aliciEposta, konu, html);
    }

    public async Task YeniYorumBildirimiGonderAsync(string aliciEposta, string aliciAd, int talepId, string baslik, string yazanAd, string yorumMetni)
    {
        string konu = $"[DestekTakip] Yeni Yanit / Mesaj (#{talepId})";
        string html = $@"
            <div style='font-family: Arial, sans-serif; background: #0b172a; color: #f1f5f9; padding: 24px; border-radius: 12px;'>
                <h2 style='color: #38bdf8; margin-top: 0;'>Talebinize Yeni Bir Yanit Yazildi</h2>
                <p>Sayin <b>{aliciAd}</b>,</p>
                <p><b>#{talepId}</b> numarali talebinize <b>{yazanAd}</b> tarafindan yeni bir yanit eklendi:</p>
                <div style='background: #1e293b; padding: 16px; border-radius: 8px; margin: 16px 0; border-left: 4px solid #38bdf8;'>
                    <p style='margin: 0; font-size: 14px;'>{yorumMetni}</p>
                </div>
                <p style='color: #94a3b8; font-size: 13px;'>Talebi yanitlamak veya dosyalari incelemek icin sisteme giris yapabilirsiniz.</p>
                <hr style='border: 0; border-top: 1px solid #334155; margin: 20px 0;' />
                <p style='color: #64748b; font-size: 11px;'>© DestekTakip Kurumsal Musteri Destek Sistemi</p>
            </div>";

        await EpostaGonderAsync(aliciEposta, konu, html);
    }

    private async Task EpostaGonderAsync(string aliciEposta, string konu, string htmlGovde)
    {
        try
        {
            if (!_ayarlar.EtkinMi || string.IsNullOrWhiteSpace(_ayarlar.Sifre))
            {
                _logger.LogInformation("[E-POSTA SIMULASYONU / BILDIRIM] Alici: {Alici} | Konu: {Konu}", aliciEposta, konu);
                return;
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_ayarlar.GonderenEposta, _ayarlar.GonderenAdi),
                Subject = konu,
                Body = htmlGovde,
                IsBodyHtml = true
            };
            mailMessage.To.Add(aliciEposta);

            using var smtpClient = new SmtpClient(_ayarlar.Sunucu, _ayarlar.Port)
            {
                Credentials = new NetworkCredential(_ayarlar.KullaniciAdi, _ayarlar.Sifre),
                EnableSsl = _ayarlar.SslKullan
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("E-posta basariyla iletildi: {Alici} | {Konu}", aliciEposta, konu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gonderilirken hata olustu. Alici: {Alici}", aliciEposta);
        }
    }
}
