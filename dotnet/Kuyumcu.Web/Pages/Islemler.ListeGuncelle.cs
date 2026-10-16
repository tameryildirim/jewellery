using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-KUCUK (B2-12 · UX-S24) — MÜŞTERİ DEĞİŞİNCE "FİYAT LİSTESİ DEĞİŞTİ —
// GÜNCELLE" SORUSU (kılavuz böyle anlatıyor).
//
// • Fiyatın TEK kaynağı sunucudur (GET /fiyat/teklif): sunucu müşterinin atanmış
//   listesini, yoksa varsayılan listeyi çözer (FiyatServisi.ListeCoz). Ekran liste
//   kimliği karşılaştırmaz; aynı ürün + AYNI kur kaydıyla yeni müşteri için teklif
//   ister ve sepetteki fiyattan FARKLIYSA sorar. Kur kaydı kalemin eklendiği anki
//   kayıttır → fark yalnız müşteriden (listeden) doğar, araya giren kur oynaması
//   "liste değişti" diye sunulmaz.
// • SESSİZ DEĞİL: fiyatlar kendiliğinden değişmez. "Fiyatları güncelle" basılırsa
//   sepet yeni teklife geçer; basılmazsa / "Eski fiyatlar kalsın" denirse eski
//   fiyatlar AYNEN kalır ve sunucuya da o gider (gövde = ekrandaki BirimFiyat).
// • Yalnız TEKLİF FİYATIYLA eklenmiş ve sonradan ELLE DEĞİŞTİRİLMEMİŞ satış
//   kalemleri sorulur; hediye ve elle fiyatlı kalem dokunulmaz.
// • Teklif alınamazsa (ağ/sunucu) sepet değişmez ve sebep bantta yazar (UX-S28).
// ═════════════════════════════════════════════════════════════════════════════

public partial class Islemler
{
    private EksikKucukMetinleri EkK => EksikKucukSozlugu.Al(Dil.Dil);

    /// <summary>Kalem → eklendiği andaki teklif fiyatı ve kur kaydı (referans eşitliği).</summary>
    private readonly Dictionary<FisKalemi, (decimal Fiyat, string KurId)> teklifIzi = [];

    /// <summary>TeklifAl'ın kullandığı kur kaydı (kalem eklenince ize yazılır).</summary>
    private string? sonTeklifKurId;

    /// <summary>Bekleyen öneri: kalem + yeni müşteri için sunucu teklifi.</summary>
    private List<(FisKalemi Kalem, Teklif Teklif)>? listeOnerisi;
    private string? listeHata, listeBasari;
    private int listeBasariJeton, listeSorguNo;

    /// <summary>
    /// Ekrana basılan öneri: kalem hâlâ sepette, tür hâlâ SATIŞ ve fiyatı arada elle
    /// değiştirilmemiş olmalı (fiş temizlendiyse / kalem silindiyse bant kendiliğinden kalkar).
    /// </summary>
    private List<(FisKalemi Kalem, Teklif Teklif)> BekleyenOneri =>
        listeOnerisi is null || tur != "SATIS" ? []
        : [.. listeOnerisi.Where(x => kalemler.Contains(x.Kalem)
                                      && teklifIzi.TryGetValue(x.Kalem, out var iz) && iz.Fiyat == x.Kalem.BirimFiyat)];

    /// <summary>Kalem teklif fiyatıyla eklendiyse izini tutar (elle fiyat → iz yok).</summary>
    private void TeklifIziKaydet(FisKalemi k, Teklif? teklif)
    {
        if (teklif is { TeklifVar: true, SatisFiyati: { } f } && f == k.BirimFiyat
            && !string.IsNullOrEmpty(sonTeklifKurId))
            teklifIzi[k] = (f, sonTeklifKurId);
    }

    /// <summary>
    /// Müşteri değişti: teklif izli satış kalemleri için yeni müşterinin teklifini ister,
    /// farklı olanları öneri olarak bekletir. Fiyat DEĞİŞTİRMEZ.
    /// </summary>
    private async Task ListeFarkiniDenetle()
    {
        var no = ++listeSorguNo;
        listeOnerisi = null; listeHata = null; listeBasari = null;
        if (tur != "SATIS") return;

        var adaylar = kalemler
            .Where(k => !k.Hediye && teklifIzi.TryGetValue(k, out var iz) && iz.Fiyat == k.BirimFiyat)
            .ToList();
        if (adaylar.Count == 0) return;

        var oneri = new List<(FisKalemi, Teklif)>();
        try
        {
            foreach (var k in adaylar)
            {
                var iz = teklifIzi[k];
                var cariParam = string.IsNullOrEmpty(cariId) ? "" : $"&cariId={cariId}";
                var t = await Api.GetAsync<Teklif>($"/fiyat/teklif?urunId={k.UrunId}{cariParam}&kurSnapshotId={iz.KurId}");
                if (t is { TeklifVar: true, SatisFiyati: { } yeni } && yeni > 0m && yeni != k.BirimFiyat)
                    oneri.Add((k, t));
            }
        }
        catch (Exception ex) when (ex is ApiHata or HttpRequestException or System.Text.Json.JsonException)
        {
            if (no == listeSorguNo) listeHata = EkK.ListeHata;
            return;
        }

        // Arada müşteri yeniden değiştiyse eski sorgunun sonucu basılmaz.
        if (no == listeSorguNo && oneri.Count > 0) listeOnerisi = oneri;
    }

    /// <summary>"Fiyatları güncelle": önerilen kalemler yeni teklife geçer (ekran = gövde).</summary>
    private async Task ListeFiyatlariniUygula()
    {
        var bekleyen = BekleyenOneri;
        if (bekleyen.Count == 0) return;
        var sayi = 0;
        foreach (var (k, t) in bekleyen)
        {
            if (t.SatisFiyati is not { } yeni) continue;
            k.BirimFiyat = yeni;
            k.ListeFiyati = t.ListeFiyati;
            k.IndirimOncesi = t.IndirimOncesiFiyat;
            k.IscilikTl = FisProKalemKarti.TeklifIscilik(t, yeni);
            if (teklifIzi.TryGetValue(k, out var iz)) teklifIzi[k] = (yeni, iz.KurId);
            sayi++;
        }
        listeOnerisi = null;
        listeBasari = string.Format(EkK.ListeGuncellendi, sayi);
        listeBasariJeton++;
        await TevsikOnizle();   // fiş toplamı değişti: nakit kilidi yeniden hesaplanır
    }

    /// <summary>"Eski fiyatlar kalsın": öneri kapanır, sepet AYNEN kalır.</summary>
    private void ListeOnerisiniReddet()
    {
        listeOnerisi = null;
        listeSorguNo++;
    }
}
