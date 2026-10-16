using System.Text.Json;
using Kuyumcu.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Kuyumcu.Api.Modules.Hava;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37 — HAVA DURUMU (karşılama ekranı kutucuğu)
//
// ⚠ DIŞ KAYNAKTIR VE VARSAYILAN OLARAK KAPALIDIR. Kurucunun kuralı: sözleşme
//   gerektiren bir kaynak SESSİZCE kullanılmaz. Varsayılan sağlayıcı Open-Meteo
//   (api.open-meteo.com) — anahtarsız, CC BY 4.0; ÜCRETSİZ katmanı TİCARİ OLMAYAN
//   kullanım içindir. Ticari kullanım ücretli plan ister → KURUCU KALEMİ.
//   Bu yüzden "Hava:Etkin" varsayılanı FALSE'tur; açılmadıkça hiçbir dış istek
//   ÇIKMAZ ve uç 204 döner (ekranda kutucuk sessizce kaybolur).
//
// ⚠ KİRACI VERİSİNE YAZILMAZ. Yanıt yalnız bellekte tutulur; hiçbir
//   tabloya düşmez (S1-j: önbellek 30 dk). Kullanıcıdan KONUM İZNİ İSTENMEZ — şehir yapılandırmadan gelir.
//
// ⚠ SAYFAYI BEKLETMEZ: istemci tarafı zaman aşımı kısadır, sunucu tarafında
//   HttpClient.Timeout 2 saniyedir ve HER hata 204'e çevrilir — 500 dönmez,
//   hata bandı ATMAZ (kurucu koşulu).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HavaServisi(IHttpClientFactory fabrika, IConfiguration yapilandirma, IMemoryCache onbellek)
{
    private const string OnbellekAnahtari = "p37-hava";
    // S1-j: 30 dk (koşul: "sunucu open-meteo'yu çağırır, 30 dk önbellek").
    public static readonly TimeSpan OnbellekSuresi = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan ZamanAsimi = TimeSpan.FromSeconds(2);

    /// <summary>Hava servisi açık mı? VARSAYILAN: KAPALI (tutucu).</summary>
    public bool Etkin => yapilandirma.GetValue("Hava:Etkin", false);

    /// <summary>null = veri yok (kapalı, ağ yok ya da kaynak yanıt vermedi). Hata FIRLATMAZ.</summary>
    public async Task<HavaSonucu?> Getir(CancellationToken iptal = default)
    {
        if (!Etkin) return null;
        if (onbellek.TryGetValue<HavaSonucu>(OnbellekAnahtari, out var kayitli)) return kayitli;

        var enlem = yapilandirma["Hava:Enlem"];
        var boylam = yapilandirma["Hava:Boylam"];
        var sehir = yapilandirma["Hava:Sehir"];
        if (string.IsNullOrWhiteSpace(enlem) || string.IsNullOrWhiteSpace(boylam)) return null;

        var taban = yapilandirma["Hava:Adres"] ?? "https://api.open-meteo.com/v1/forecast";
        var adres = $"{taban}?latitude={Uri.EscapeDataString(enlem)}&longitude={Uri.EscapeDataString(boylam)}&current=temperature_2m,weather_code";

        try
        {
            using var istemci = fabrika.CreateClient("hava");
            istemci.Timeout = ZamanAsimi;
            using var cevap = await istemci.GetAsync(adres, iptal);
            if (!cevap.IsSuccessStatusCode) return null;
            await using var akis = await cevap.Content.ReadAsStreamAsync(iptal);
            using var belge = await JsonDocument.ParseAsync(akis, cancellationToken: iptal);
            if (!belge.RootElement.TryGetProperty("current", out var simdi)) return null;
            if (!simdi.TryGetProperty("temperature_2m", out var derece)) return null;

            var sonuc = new HavaSonucu(
                Derece: derece.GetDecimal(),
                Kod: simdi.TryGetProperty("weather_code", out var k) ? k.GetInt32() : null,
                Sehir: sehir,
                Kaynak: "Open-Meteo (CC BY 4.0)");
            onbellek.Set(OnbellekAnahtari, sonuc, OnbellekSuresi);
            return sonuc;
        }
        catch (Exception)
        {
            // ⚠ md.1 gizleme DEĞİLDİR: burada "başarısız işlem" yoktur, İSTEĞE
            //   BAĞLI bir süs verisi yoktur. Uç 204 döner, ekran kutucuğu
            //   kaybolur ve kullanıcıya YALAN söylenmez (uydurma derece yazılmaz).
            return null;
        }
    }
}

public sealed record HavaSonucu(decimal Derece, int? Kod, string? Sehir, string Kaynak);

[ApiController]
[Authorize]
[Route("api/v1/hava")]
public sealed class HavaController(HavaServisi servis) : ControllerBase
{
    /// <summary>GET /api/v1/hava — 200 + derece, ya da 204 (kapalı/ulaşılamadı).</summary>
    [HttpGet]
    [HerkeseAcik("Hava bir süs verisidir; kiracı verisi taşımaz ve hiçbir yetki kapısına bağlı değildir")]
    public async Task<IActionResult> Getir(CancellationToken iptal)
    {
        var sonuc = await servis.Getir(iptal);
        return sonuc is null ? NoContent() : Ok(sonuc);
    }
}
