using System.Globalization;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S2 — SATIŞ PROFESYONEL MOD = tuval Profesyonel.dc.html (Islemler sayfasının parçası)
//
// Yerleşim: tür sekmeleri + "Diğer ▾" · alan satırı (Müşteri+borç rozeti · Tarih ·
// Para · Satıcı · Fiyat listesi) · geniş kalem tablosu + alt toplam şeridi · en
// altta Karma/Tek ödeme hapı + özet + Vazgeç + Kaydet ve yazdır · sağ sütun
// (Yasal kapılar · Müşteri kartı · Kısayollar · not).
//
// ⚠ Kaydet() / POST uçları / gövdeler / yetki / yasal kapılar DEĞİŞMEDİ. Buradaki
//   istekler yalnız OKUMADIR (GET /tevsik/gunluk, /cari/{id}/bakiye, /fiyat/listeler)
//   ve hata YUTULMAZ (UX-S28): kartın kendi satırında sebep yazar.
// ⚠ EŞİK SAYISI YOK (MEVZUAT KURALI · B-3): nakit kuralı ve kimlik eşiği SUNUCUDAN;
//   sunucu yanıt vermezse kart sayısız "Bilinmiyor" yazar (istemci varsayılanı yok).
// ⚠ Islemler.razor ≤ 1.400 satır (S21): yeni mantık ve TevsikOnizle bu dosyada.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Tuvalin profesyonel satış yüzü mü (PRO + SATIS).</summary>
    internal bool ProSatis => Modu.ProfesyonelMi && tur == "SATIS";

    /// <summary>Tuvalin satış fişi (BASİT tezgâh ya da PRO satış) — "Kaydet ve yazdır" ikisinde de aynı kural.</summary>
    private bool SatisTuvali => tur == "SATIS";

    private SatisProMetinleri SP => SatisProSozlugu.Al(Dil.Dil);

    /// <summary>"Diğer ▾" menüsündeki sayfa türleri (İşçilik · Transfer) — tuvalde sekmesi yok.</summary>
    private (string Deger, string Etiket)[] ProEkTurler =>
        Modu.ProfesyonelMi ? TurListesi.Where(x => FisTurleri.ProDigerTurleri.Contains(x.Deger)).ToArray() : [];

    // ── Yasal kapılar kartı ────────────────────────────────────────────────
    private TevsikOnizleme? proTevsik;
    private string? proTevsikHata;

    /// <summary>
    /// Haddi ve bugünkü nakit hakkını sunucudan sorar (salt okunur önizleme).
    /// UX-3 Faz B: BASİT modda önizleme karma OLMADAN da çalışır — ödeme türü seçicisi
    /// gizli, yalnız yasal kapı açılınca çizilir; kapıyı SUNUCU söyler (Mehmet K2).
    /// T-S2: PROFESYONEL modda karma kapalıyken ödeme kapısı bayrakları (tevsikKalan /
    /// tevsikNakitKapali) ESKİSİ GİBİ boş kalır; yalnız Yasal kapılar kartının ayrı
    /// durumu (proTevsik) tazelenir — Kaydet kilidi ve gövde değişmez.
    /// </summary>
    private async Task TevsikOnizle(bool yalnizHad = false)
    {
        tevsikSorulanToplam = ToplamTl;   // YASAL-KAPI-GERI: hangi toplamla soruldu (iskonto/satır düzeltmesi sonrası yeniden sormak için)
        tevsikKalan = null;
        tevsikNakitKapali = false;
        if (tur != "SATIS") return;
        if (!karmaOdeme && Modu.ProfesyonelMi) { await ProYasalTazele(); return; }
        try
        {
            var y = await TevsikGetir();
            if (!yalnizHad) (tevsikKalan, tevsikNakitKapali) = (y.KalanNakitHakki, y.IslemTutariKuraliDevrede);   // T-S1 ilk açılış: YALNIZ had (kapı fişle açılır)
            tevsikHaddi = y.Had > 0m ? y.Had : tevsikHaddi;
            if (Modu.ProfesyonelMi) (proTevsik, proTevsikHata) = (y, null);
        }
        catch (ApiHata ex) { if (Modu.ProfesyonelMi) proTevsikHata = ex.Message; /* kapı yine sunucuda işler */ }
        if (Modu.ProfesyonelMi) await ProKartTazele();
    }

    private string TevsikSorgusu()
    {
        var sorgu = $"/tevsik/gunluk?fisToplamTl={ToplamTl.ToString(CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrEmpty(cariId)) sorgu += $"&cariId={Uri.EscapeDataString(cariId)}";
        return sorgu;
    }

    /// <summary>PRO kartı: aynı önizleme ucu, ayrı durum (ödeme kapısı bayraklarına YAZMAZ).</summary>
    private async Task ProYasalTazele()
    {
        try
        {
            (proTevsik, proTevsikHata) = (await TevsikGetir(), null);
            // t7 (doğrulayıcı DÜŞÜK): sunucunun haddi PRO'da da saklanır — PRO→BASİT canlı geçişte nakit uyarı
            // kutusu "eşik bilinmiyor" sayılıp 0 ₺'lik fişte sayısız metinle açılmasın (kural sunucuda; sayı sunucudan).
            if (proTevsik is { Had: > 0m } ph) tevsikHaddi = ph.Had;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { (proTevsik, proTevsikHata) = (null, ex.Message); }
        await ProKartTazele();
    }

    /// <summary>Kartın nakit satırı: sunucunun haddi (yoksa sayısız); satış dışı türde durum "Bilinmiyor".</summary>
    private decimal? ProNakitHad => proTevsik is { Had: > 0m } p ? p.Had : tevsikHaddi;
    /// <remarks>
    /// "Aşıldı": sunucu bu fişin tutarını kurala takılmış sayıyorsa YA DA sunucu günlük kümülatifi
    /// hesaplamışken fiş, sunucunun verdiği kalan nakit hakkını aşıyorsa (had ve kalan SUNUCUDAN; canlıda
    /// ölçüldü: aynı müşteriye günün ikinci nakit fişi kart "Uygun" derken sunucu 400 veriyordu).
    /// </remarks>
    private bool? ProNakitAsildi => tur == "SATIS" && proTevsik is { } p
        ? (NakitOdemede || PesinSecili) && (p.IslemTutariKuraliDevrede || (p.KumulatifHesaplandi && ToplamTl > p.KalanNakitHakki))   // t15: ödemede nakit yoksa (ve Peşin değilse) sunucu nakit kapısı işlemez → "Aşıldı" değil
        : null;

    // ── Müşteri kartı ──────────────────────────────────────────────────────
    private string? kartCariId, kartHata;
    private DateTime? kartSonIslem;

    private CariVeri? ProCari => string.IsNullOrEmpty(cariId) ? null : cariler.FirstOrDefault(c => c.Id == cariId);

    /// <summary>Seçili gerçek müşterinin son işlem tarihi (GET /cari/{id}/bakiye · sonIslemTarihi) — müşteri değişince bir kez.</summary>
    private async Task ProKartTazele()
    {
        if (!Modu.ProfesyonelMi || cariId == kartCariId) return;
        kartCariId = cariId; kartSonIslem = null; kartHata = null;
        if (string.IsNullOrEmpty(cariId) || cariId == pesinCariId) return;
        try
        {
            var b = await Api.GetAsync<JsonElement>($"/cari/{Uri.EscapeDataString(cariId)}/bakiye");
            if (b.ValueKind == JsonValueKind.Object && b.TryGetProperty("sonIslemTarihi", out var t)
                && t.ValueKind == JsonValueKind.String && t.TryGetDateTime(out var d)) kartSonIslem = d;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException) { kartHata = ex.Message; }
    }

    // ── Fiyat listesi kutusu ───────────────────────────────────────────────
    private List<(string Id, string Ad, bool Varsayilan)>? fiyatListeleri;
    private string? fiyatListesiHata;

    /// <summary>GET /fiyat/listeler — yalnız PRO'da ve bir kez; aktif listeler (ad + varsayılan bayrağı).</summary>
    private async Task FiyatListeleriYukle()
    {
        if (!Modu.ProfesyonelMi || fiyatListeleri is not null) return;
        try
        {
            var liste = await Api.GetAsync<List<JsonElement>>("/fiyat/listeler");
            fiyatListeleri = [.. liste.Where(e => !e.TryGetProperty("aktif", out var a) || a.ValueKind != JsonValueKind.False)
                .Select(e => (Metin(e, "id") ?? "", Metin(e, "ad") ?? "", e.TryGetProperty("varsayilan", out var v) && v.ValueKind == JsonValueKind.True))];
            fiyatListesiHata = null;
        }
        catch (Exception ex) when (ex is ApiHata or JsonException or InvalidOperationException) { fiyatListesiHata = ex.Message; }
    }

    /// <summary>Sunucunun çözümünün AYNASI (FiyatServisi.ListeCoz): müşteriye atanmış aktif liste, yoksa varsayılan.</summary>
    public static (string? Ad, bool Caridan) FiyatListesiCoz(IReadOnlyList<(string Id, string Ad, bool Varsayilan)>? listeler, string? cariListeId)
    {
        if (listeler is null) return (null, false);
        if (!string.IsNullOrEmpty(cariListeId) && listeler.FirstOrDefault(l => l.Id == cariListeId) is { Id: not null and not "" } atanan)
            return (atanan.Ad, true);
        return listeler.FirstOrDefault(l => l.Varsayilan) is { Id: not null and not "" } vars ? (vars.Ad, false) : (null, false);
    }

    /// <summary>Alan satırının PRO ekleri: Satıcı (S2-5b seçicisi buraya taşındı) · Fiyat listesi.</summary>
    private RenderFragment ProUstAlanlar => b =>
    {
        b.OpenComponent<FisSatici>(0);
        b.AddAttribute(1, nameof(FisSatici.Liste), saticilar);
        b.AddAttribute(2, nameof(FisSatici.Hata), saticiHata);
        b.AddAttribute(3, nameof(FisSatici.Basit), false);
        b.AddAttribute(4, nameof(FisSatici.OturumAdi), Oturum.Kullanici?.Ad + " " + Oturum.Kullanici?.Soyad);
        b.AddAttribute(5, nameof(FisSatici.Secili), saticiId);
        b.AddAttribute(6, nameof(FisSatici.SeciliChanged), EventCallback.Factory.Create<string?>(this, v => saticiId = v));
        b.AddAttribute(7, nameof(FisSatici.Etiket), SP.AlanSatici);
        b.CloseComponent();
        var (ad, caridan) = FiyatListesiCoz(fiyatListeleri, ProCari?.FiyatListesiId);
        b.OpenComponent<FisProFiyatListesi>(8);
        b.AddAttribute(9, nameof(FisProFiyatListesi.Ad), ad);
        b.AddAttribute(10, nameof(FisProFiyatListesi.Caridan), caridan);
        b.AddAttribute(11, nameof(FisProFiyatListesi.Hata), fiyatListesiHata);
        b.CloseComponent();
    };

    // ── Ödeme hapı ─────────────────────────────────────────────────────────
    /// <summary>Hap: Karma → mevcut "Ödemeyi böl" yolu; Tek → mevcut "Tek ödemeye dön" yolu.</summary>
    private async Task ProKarmaSec(bool karma)
    {
        if (karma == karmaOdeme) return;
        if (karma) await OdemeyiBol(); else await TekOdemeyeDon();
    }

    // ── Kısayollar (F2 · F4 · F8 · F9) ──────────────────────────────────────
    private int aramaAcJeton;
    /// <summary>Kısayol → odaklanacak alan (F9 = Kaydet, odak değil). Kısayollar kartıyla AYNI eşleme.</summary>
    public static string? KisayolHedefi(string tus, bool karma) => tus switch
    {
        "F2" => "islem-barkod",
        "F4" => "islem-cari",
        "F8" => karma ? "islem-pro-tek" : "islem-odeme",   // SATIS-2: karmada anahtar yok — "Tek ödemeye dön" bağlantısı
        _ => null,
    };

    /// <summary>
    /// PRO satış ekranının F-tuşları. Sayfa kabına bağlıdır (imleç ekrandayken çalışır).
    /// F9 TEK POST yolunu (Kaydet) çağırır; kilitliyken hiçbir şey göndermez — kilit
    /// sebebi Kaydet'in altında zaten yazılıdır (UX-S30).
    /// </summary>
    private async Task ProKisayol(KeyboardEventArgs e)
    {
        if (!Modu.ProfesyonelMi || DigerAcik) return;
        if (e.Key == "F9")
        {
            if (Gonderilebilir && !kaydediliyor) await Kaydet();
            return;
        }
        if (e.Key == "F2") aramaAcJeton++;   // SATIS-3 t13: F2 ürün listesini de açar (odak tek başına açmaz)
        // SATIS-2 (f): tek ödemede ödeme türü alt çubuk katmanındadır — F8 önce katmanı açar.
        if (e.Key == "F8" && ProSatis && !ProOdemeSatirIci && !proOdemeAcik) { proOdemeAcik = true; StateHasChanged(); await Task.Yield(); }
        if (KisayolHedefi(e.Key, karmaOdeme) is { } id)
        {
            try { await JS.InvokeAsync<bool>("kuyumcuOdak.alana", id); }
            catch (JSException) { /* odak taşınamadı: alan ekranda yoksa (ör. transfer) tuş etkisizdir */ }
        }
    }
}
