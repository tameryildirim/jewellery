namespace Kuyumcu.Web.Servisler;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-37b → S1-j — HAVA DURUMU (karşılama selamı) — SUNUCU ÜZERİNDEN
//
// ÖNCE (P-37b): istemci api.open-meteo.com'a DOĞRUDAN fetch ediyordu. Sunucunun
//   CSP'si `connect-src 'self'` olduğu için tarayıcı isteği ENGELLİYOR, her
//   Panel açılışında konsola CSP ihlali düşüyordu (orkestratör ölçümü 24.09;
//   kutucuk zaten hiç çizilmiyordu). CSP GEVŞETİLMEZ (S1-j koşulu).
// ŞİMDİ: istemci yalnız KENDİ sunucusuna sorar — GET /api/v1/hava (ApiIstemci
//   bandından geçer; APIISTEMCI_DISI_HTTPCLIENT beyaz listesi artık gereksiz).
//   Sunucu (Kuyumcu.Api/Modules/Hava) dış kaynağı çağırır, 30 dk önbellekler;
//   kapalıysa / kaynak yanıt vermezse 204 döner ve kutucuk HİÇ çizilmez.
//
// KURUCUNUN DÜRÜSTLÜK ŞARTLARI (talimat 20) yerinde:
//  (a) DIŞ KAYNAKTIR: veri gelmezse kutucuk SESSİZCE kaybolur; hata bandı yok,
//      sayfa beklemez (aşağıdaki zaman aşımı + sessiz düşme).
//  (b) Şehir SUNUCU yapılandırmasından gelir; kullanıcıdan konum izni istenmez.
//  (c) Varsayılan KAPALI kararı SUNUCUDADIR (Hava:Etkin, varsayılan false —
//      lisans/sözleşme kararı kurucunun). İstemcide ikinci bir anahtar YOKTUR:
//      aynı soruya iki cevap olmasın (UX-S22).
//  (d) Kiracı verisine yazılmaz.
//  (e) UYDURMA YOK: sunucu derece döndürmediyse (204, boş gövde, derecesiz
//      JSON) null döner — "0°" ya da varsayılan sıcaklık ASLA çizilmez.
//
// ⚠ ZAMAN AŞIMI KAPISI (denetim.sh HAVA_BEKLETME_KAPISI): istek
//   `ZamanAsimiSaniye` saniyede bırakılır ve her istisna yutulup null dönülür.
// ═══════════════════════════════════════════════════════════════════════════
public sealed class HavaServisi(ApiIstemci api)
{
    /// <summary>Sunucu bu sürede cevap vermezse beklenmez (saniye). Karşılama ekranı BEKLEMEZ.</summary>
    public const int ZamanAsimiSaniye = 3;

    /// <summary>Sunucudaki uç — CSP `connect-src 'self'` ile uyumlu, göreli yol.</summary>
    public const string Uc = "/hava";

    /// <summary>Bir kez denendi mi (sonuç ister gelsin ister gelmesin).</summary>
    public bool Denendi { get; private set; }

    public async Task<HavaDurumu?> GetirAsync()
    {
        try
        {
            using var iptal = new CancellationTokenSource(TimeSpan.FromSeconds(ZamanAsimiSaniye));
            var y = await api.GetAsync<HavaSunucuYaniti?>(Uc).WaitAsync(iptal.Token);
            Denendi = true;
            // (e) UYDURMA YOK: derecesiz yanıt = veri yok.
            if (y?.Derece is not { } derece) return null;
            return new HavaDurumu(derece, y.Kod, y.Sehir ?? "", y.Kaynak ?? "");
        }
        catch (Exception)
        {
            // SESSİZ DÜŞ: ağ yok, zaman aşımı, 4xx/5xx, bozuk JSON… hepsi aynı
            // sonuca çıkar — kutucuk çizilmez, sayfa etkilenmez, konsola hata yok.
            Denendi = true;
            return null;
        }
    }
}

/// <summary>Ekranda gösterilen hava kaydı. Veritabanına YAZILMAZ.</summary>
/// <remarks>⚠ HAM WMO KODU taşınır, METİN DEĞİL: çeviri ekranda,
/// <see cref="Kuyumcu.Core.I18n.HavaSozlugu"/> üzerinden ve SEÇİLİ DİLDE yapılır (UX-S31).</remarks>
public sealed record HavaDurumu(decimal? Derece, int? Kod, string Sehir, string Saglayici);

/// <summary>GET /api/v1/hava yanıtı (sunucudaki HavaSonucu, camelCase). Derece yoksa veri yok sayılır.</summary>
public sealed record HavaSunucuYaniti(decimal? Derece, int? Kod, string? Sehir, string? Kaynak);
