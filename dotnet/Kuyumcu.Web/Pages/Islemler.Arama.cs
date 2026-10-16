using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — KAYNAK 0 (kurucu 25.09 ~12:05): "ürün adını yazıyorum liste
// olarak gelmiyor … elle ekle gibi bir seçeneğe gerek yok, tek kalsın".
// • Tek kutu (#islem-barkod): yazdıkça (220 ms debounce, FisKalemGirisi) sunucuda
//   ad / ürün kodu / barkod içinde arar — GET /urunler?aktif=true&q=…&limit=21.
//   21. satır yalnız "daha fazlası var" (UX-S29) için istenir; ekranda en çok 20.
// • Kutu boşken odakta: en çok satılan 8 ürün (ilk odakta tek istek, sonra önbellek).
// • Seçim = barkod okutmayla AYNI kapı: fiyat teklifi (/fiyat/teklif) → kalem.
//   Fiyat çıkmazsa ürün seçili kalır, elle satırı açılır ve sebep yazılır.
// ⚠ Kaydet() / POST gövdeleri DEĞİŞMEDİ. Islemler.razor ≤ 1.400 satır (S21).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Ekranda gösterilen en çok öneri (sunucudan bir fazlası istenir).</summary>
    public const int OneriSiniri = 20;

    private IReadOnlyList<UrunVeri>? oneriler;
    private bool oneriAraniyor, oneriSinirda;
    private string? oneriHata;
    private string sonOneriSorgusu = "";

    // Doğrulama bulgusu (27.09): boş odak listesi "en çok satılan 8" olmalı. Liste sunucuda
    // son 90 günün SATIS sayısına göre sıralanır (GET /urunler?aktif=true&siralama=cok-satan) ve kutuya
    // İLK odaklanıldığında BİR KEZ istenir (sayfa açılışına istek eklenmez — ISTEK-1).
    private List<UrunVeri>? enCokSatanlar;
    private bool enCokSatanIstendi;

    /// <summary>Boş odak listesi: sunucunun "en çok satılan" sırası (yüklenmediyse boş).</summary>
    private IReadOnlyList<UrunVeri> BosOdakUrunleri => enCokSatanlar ?? [];

    private async Task EnCokSatanlariYukle()
    {
        if (enCokSatanIstendi) return;
        enCokSatanIstendi = true;
        try
        {
            enCokSatanlar = await Api.GetAsync<List<UrunVeri>>(
                $"/urunler?aktif=true&siralama=cok-satan&limit={Bilesenler.Fis.FisKalemGirisi.BosOdakBoyu}");
            StateHasChanged();
            await SatirBilgisiYukle(enCokSatanlar, null);   // SATIS-4 (c)
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException)
        {
            enCokSatanIstendi = false;   // sonraki odakta yeniden denenir
            oneriHata = ex.Message;      // UX-S28: hata "liste boş"a çevrilmez
        }
    }

    private async Task UrunAra(string q)
    {
        sonOneriSorgusu = q;
        oneriAraniyor = true;
        oneriHata = null;
        StateHasChanged();
        try
        {
            var liste = await Api.GetAsync<List<UrunVeri>>(
                $"/urunler?aktif=true&q={Uri.EscapeDataString(q)}&limit={OneriSiniri + 1}");
            if (q != sonOneriSorgusu) return;   // eski yanıt yeni aramanın üstüne yazılmaz
            oneriSinirda = liste.Count > OneriSiniri;
            oneriler = liste.Take(OneriSiniri).ToList();
            StateHasChanged();                        // liste HEMEN görünür; fiyat/stok arkadan dolar
            await SatirBilgisiYukle(oneriler, q);     // SATIS-4 (c)
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException)
        {
            if (q != sonOneriSorgusu) return;
            oneriler = null;
            oneriSinirda = false;
            oneriHata = ex.Message;
        }
        finally
        {
            if (q == sonOneriSorgusu) oneriAraniyor = false;
        }
    }

    // ═══ SATIS-4 (c) (Kurul 5) — arama satırında FİYAT ve STOK ═══════════════════════════════════════════
    // Liste (GET /urunler) altın üründe satış fiyatı taşımaz ("—" her satırda boşluk gibi görünüyordu) ve stok
    // taşımaz. Liste gelince İKİ toplu istek: stok GET /urunler/stok-adetleri?ids=… (ISTEK-1 ucu, oturum şubesi)
    // ve fiyat GET /fiyat/teklifler?urunIds=… (KESIF-P5 — /fiyat/teklif ile AYNI motor, aynı cari listesi, aynı
    // kur kaydı). Satır başına istek YOK. Fiyat önbelleği kur + müşteri değişince düşer; stok her aramada tazedir.
    // Hata "stok yok"a ÇEVRİLMEZ (UX-S28): okunamayan stok "Stok —", okunamayan fiyat "—" yazar.
    private readonly Dictionary<string, decimal?> satirFiyatlari = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> satirStoklari = new(StringComparer.Ordinal);
    private string satirFiyatAnahtari = "";
    /// <summary>Sunucu sınırı (FiyatTopluTeklifController.EnFazlaUrun) — liste en çok 20 gösterir.</summary>
    public const int TopluTeklifSiniri = 25;

    private async Task SatirBilgisiYukle(IReadOnlyList<UrunVeri>? liste, string? q)
    {
        if (liste is not { Count: > 0 }) return;
        var ids = liste.Select(u => u.Id).Distinct().ToList();
        try
        {
            var sube = Oturum.Kullanici?.SubeId is { Length: > 0 } s ? $"&subeId={Uri.EscapeDataString(s)}" : "";
            var st = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Urunler.StokAdetleriYaniti>(
                $"/urunler/stok-adetleri?ids={Uri.EscapeDataString(string.Join(',', ids))}{sube}");
            foreach (var x in st?.Urunler ?? []) satirStoklari[x.UrunId] = x.ToplamAdet;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException) { /* "Stok —" */ }
        if (q is not null && q != sonOneriSorgusu) return;   // eski arama
        var anahtar = $"{kurId}|{(PesinSecili ? "" : cariId)}";
        if (anahtar != satirFiyatAnahtari) { satirFiyatlari.Clear(); satirFiyatAnahtari = anahtar; }
        var sorulacak = liste.Where(u => u.SatisFiyati is not > 0 && !satirFiyatlari.ContainsKey(u.Id))
            .Select(u => u.Id).Distinct().Take(TopluTeklifSiniri).ToList();
        if (sorulacak.Count > 0 && !string.IsNullOrEmpty(kurId))
        {
            try
            {
                var cari = PesinSecili || string.IsNullOrEmpty(cariId) ? "" : $"&cariId={Uri.EscapeDataString(cariId)}";
                var y = await Api.GetAsync<Kuyumcu.Contracts.Fiyat.TopluTeklifYaniti>(
                    $"/fiyat/teklifler?urunIds={Uri.EscapeDataString(string.Join(',', sorulacak))}&kurSnapshotId={Uri.EscapeDataString(kurId)}{cari}");
                foreach (var x in y?.Satirlar ?? []) satirFiyatlari[x.UrunId] = x.TeklifVar ? x.SatisFiyati : null;
            }
            catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException) { /* "—" */ }
        }
        StateHasChanged();
    }

    /// <summary>
    /// SATIS-3 (canlı ölçümde bulundu): tür İade'ye geçince aday listesi ateşle-unut yükleniyordu ve
    /// yanıt gelince sayfa YENİDEN ÇİZİLMİYORDU — kaynak seçici, başka bir etkileşime kadar boş
    /// ("İşlem seçin" tek satır) görünüyordu. Yükleme bitince çizilir.
    /// </summary>
    private async Task IadeAdaylariYukleCiz()
    {
        await IadeAdaylariYukle();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// t7 (doğrulayıcı ORTA): fiyat hesaplanamayınca SUNUCUNUN SEBEBİ söylenir. Eskiden her durumda gümüşe özgü
    /// metin basılıyordu ("gümüşün gram kotasyonu modelde yok") — altın üründe fiyat listesi yokken yanıltıcıydı.
    /// Sebep kodları: FiyatModulu /fiyat/teklif (LISTE_YOK · KOTASYON_YOK · FIYAT_YOK) + istemcinin HATA/kur yok durumu.
    /// </summary>
    private string FiyatHesaplanamadiMetni(Teklif? t)
    {
        var s3 = Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil);
        if (string.IsNullOrEmpty(kurId)) return s3.FiyatElleKurYok;
        return t?.Sebep switch
        {
            "LISTE_YOK" => s3.FiyatElleListeYok,
            "KOTASYON_YOK" or "GUMUS" => Dil.T.Urun.BarkodFiyatGirilmeli,
            "HATA" => s3.FiyatElleBaglanti,
            _ => s3.FiyatElleHesaplanamadi,
        };
    }

    /// <summary>Listeden seçim — barkod okutmayla aynı kapı (teklif → kalem).</summary>
    private async Task OneriSec(string urunId)
    {
        giris.BarkodHata = null;
        var u = oneriler?.FirstOrDefault(x => x.Id == urunId) ?? urunler.FirstOrDefault(x => x.Id == urunId);
        if (u is null) return;
        if (urunler.All(x => x.Id != u.Id)) urunler = [.. urunler, u];
        giris.UrunId = u.Id;
        giris.Adet = 1m;
        giris.Barkod = "";
        oneriler = null;
        giris.BirimFiyat = await TeklifAl(u.Id);
        if (TakasUrunAta()) return;   // t3: Takas'ta seçim kalem değil, "Yeni ürün"
        if (giris.BirimFiyat is null or <= 0)
        {
            giris.BarkodHata = FiyatHesaplanamadiMetni(giris.SonTeklif);
            return;
        }
        await KalemEkle();
    }

    /// <summary>
    /// SATIS-3 t3 (tasarım hakemi YÜKSEK — KAYNAK 0 "aynı kutu tüm türlerde"): Takas'ın "Yeni ürün" tarafı Satış/Alış'taki
    /// TEK arama kutusunu (#islem-barkod) kullanır. Liste/barkod/elle satırıyla seçilen ürün kalem olarak EKLENMEZ;
    /// takas girdisine (YeniUrunId + teklif fiyatı) yazılır. Takas gövdesi (Kaydet) DEĞİŞMEDİ. Takas dışında false.
    /// </summary>
    private bool TakasUrunAta()
    {
        if (tur != "TAKAS" || string.IsNullOrEmpty(giris.UrunId)) return false;
        takas.YeniUrunId = giris.UrunId;
        // t9 (doğrulayıcı KRİTİK): fiyatı çıkmayan ürün önceki ürünün fiyatını TAŞIMAZ — boş kalır, Kaydet kilitli.
        takas.YeniFiyat = giris.BirimFiyat is > 0m ? giris.BirimFiyat : null;
        if (takas.YeniAdet < 1) takas.YeniAdet = 1;
        giris.Barkod = "";
        giris.BarkodHata = null;
        giris.Sifirla();   // elle satırı ürün seçili diye açık kalmasın (seçim takas girdisine geçti)
        oneriler = null;
        return true;
    }

    /// <summary>Elle satırındaki "Ekle" — Takas'ta takas girdisine, diğer türlerde kaleme.</summary>
    private Task KalemEkleYaDaTakas() => TakasUrunAta() ? Task.CompletedTask : KalemEkle();
}
