using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4B — Müşteriler "Yıldız" yüzü (tuval Musteri4/Musteri4a · BİREBİR).
// Yalnız GÖRÜNÜM: liste, arama, süzgeç, kart, oluşturma ve tüm yazma yolları
// Cariler.razor'un MEVCUT kodudur; burada yalnız tuval satırları üretilir.
// Tema temizliği (17.10): tek şema Yıldız — bu yüz her zaman çizilir; özgün gövde "Tüm ayrıntılar" arkasındadır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Cariler
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    /// <summary>Etkin şema Yıldız mı (kabukla AYNI karar noktası)?</summary>

    /// <summary>Vadeye kalan gün (aynı /raporlar/vade-takibi yanıtından; yeni istek YOK).</summary>
    private Dictionary<string, int> yildizKalanlar = [];

    /// <summary>En yakın vadeye bugünden kalan gün (yalnız gelecekteki vadeler; geçmiş = gecikme sözlüğü).</summary>
    internal static Dictionary<string, int> YildizKalanlariHesapla(IEnumerable<Kuyumcu.Contracts.Sayfalar.VadeTakibi.CariSatir>? satirlar)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today).DayNumber;
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var v in satirlar ?? [])
            if (v.EnYakinVade is DateOnly vade && vade.DayNumber - bugun is int k and > 0
                && (!d.TryGetValue(v.CariId, out var eski) || k < eski))
                d[v.CariId] = k;
        return d;
    }

    /// <summary>Tuval sekmeleri (HEPSİ · BORÇLU · TOPTANCI) — kodlar sayfanın TEK segment kaynağından.</summary>
    /// <summary>
    /// Yıldız listesi: iki modda da tuval süzgeci (HEPSİ/BORÇLU/TOPTANCI); tuval Musteri4 listesinde sistem
    /// carisi (Peşin Müşteri) satırı YOK — üst satırdaki "7 müşteri" sayısında VAR (tuval: 7 sayı, 6 satır).
    /// </summary>
    public static IReadOnlyList<CariVeri> YildizGorunen(IReadOnlyList<CariVeri> liste, string seg) =>
        [.. SegmentSuz(liste, seg).Where(c => !c.SistemCarisi)];

    private IReadOnlyList<YildizSekme> YildizSekmeleri =>
    [
        new(SegHepsi, YildizMusteriSozlugu.Hepsi(Dil.Dil), segment == SegHepsi),
        new(SegBorclu, YildizMusteriSozlugu.Borclu(Dil.Dil), segment == SegBorclu),
        new(SegToptanci, YildizMusteriSozlugu.Toptanci(Dil.Dil), segment == SegToptanci),
    ];

    private YildizMusteriBaglami YildizBaglami
    {
        get
        {
            var gorunen = YildizGorunen(CariListesi, segment);
            var (alacak, borc) = YildizMusteriBicim.Sayilar(CariListesi);
            var sinirda = CariListesi.Count >= Sinir;
            return new YildizMusteriBaglami
            {
                Ust = sinirda ? YildizMusteriSozlugu.UstSinirli(Sinir, alacak, borc, Dil.Dil)
                              : YildizMusteriSozlugu.Ust(CariListesi.Count, alacak, borc, Dil.Dil),
                Satirlar = [.. gorunen.Select(c => YildizMusteriBicim.Satir(c, GecikmeGun(c),
                    yildizKalanlar.TryGetValue(c.Id, out var k) ? k : null, TelefonGoster(c.Telefon), Dil.Dil))],
                Sekmeler = YildizSekmeleri,
                Arama = arama,
                Yukleniyor = yukleniyor,
                HataVar = listeHata is not null,
                // YILDIZ-4C (hakem 4B tur 2 DÜŞÜK): arama sunucuda süzülür — sonuç boşsa "henüz müşteri yok" YANLIŞ olur.
                HicYok = !yukleniyor && listeHata is null && CariListesi.Count == 0 && !YildizAramaSuzgeci,
                AramaBos = !yukleniyor && listeHata is null && CariListesi.Count == 0 && YildizAramaSuzgeci,
                SuzgecBos = !yukleniyor && listeHata is null && CariListesi.Count > 0 && gorunen.Count == 0,
                SuzgecEtkin = YildizSuzgecEtkin(turFiltre, pasifleriGoster, yildizAramaPasif),
                SinirNotu = sinirda ? $"{string.Format(Dil.T.Kabuk.SinirUyarisi, Sinir)} {Dil.T.Kabuk.SinirAsildi}" : null,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, YildizAramaDegisti),
                SekmeSec = EventCallback.Factory.Create<string>(this, k => segment = k),
                SatirAc = EventCallback.Factory.Create<string>(this, YildizSatirAc),
                YeniMusteri = EventCallback.Factory.Create(this, YeniModalAc),
                Bantlar = YildizBantlar,
                DigerSuzgecler = YildizDigerSuzgecler,
            };
        }
    }

    /// <summary>Liste bir arama ya da tür/pasif süzgeciyle mi istendi (boş yanıtın anlamı değişir).</summary>
    private bool YildizAramaSuzgeci => !string.IsNullOrWhiteSpace(arama) || !string.IsNullOrEmpty(turFiltre) || pasifleriGoster;

    /// <summary>
    /// YILDIZ-4D: pasif kartlar bu oturumda ARAMA yüzünden mi listeye alındı (tuvalde süzgeç düğmesi yok — pasif
    /// müşteri aramayla bulunur). Bağlantıdan gelen "aktif=false" süzgeci bundan ayrıdır ve arama silinince korunur.
    /// </summary>
    private bool yildizAramaPasif;

    /// <summary>Tür/pasif şeridi yalnız süzgeç BAĞLANTIDAN etkinken görünür (aramanın kendi pasif açılışı sayılmaz).</summary>
    public static bool YildizSuzgecEtkin(string? tur, bool pasifler, bool aramaPasif) =>
        !string.IsNullOrEmpty(tur) || (pasifler && !aramaPasif);

    /// <summary>
    /// YILDIZ-4D: arama yazılınca pasif kartlar da istenir (satırda "kullanımda değil" yazar; kartından aktife alınır);
    /// arama silinince yalnız aramanın açtığı pasif görünümü kapanır. Para/defter verisi değişmez — yalnız liste sorgusu.
    /// </summary>
    public static (bool Pasifler, bool AramaPasif) YildizAramaPasifi(string arama, bool pasifler, bool aramaPasif)
    {
        var dolu = !string.IsNullOrWhiteSpace(arama);
        if (dolu && !pasifler) return (true, true);
        if (!dolu && aramaPasif) return (false, false);
        return (pasifler, aramaPasif);
    }

    private async Task YildizAramaDegisti(string? v)
    {
        arama = v ?? "";
        (pasifleriGoster, yildizAramaPasif) = YildizAramaPasifi(arama, pasifleriGoster, yildizAramaPasif);
        await Yukle();
    }

    /// <summary>Satıra dokununca kart (tuval notu): sayfanın mevcut kartı — BASİT ve PRO aynı yol.</summary>
    private async Task YildizSatirAc(string id)
    {
        if (CariListesi.FirstOrDefault(c => c.Id == id) is { } c) await DuzenleAc(c);
    }

    /// <summary>
    /// Kartın eylem satırı: özgün satır eylemleri (CariEylemleri — TEK kaynak). "Düzenle" kartın
    /// kendisidir; "Pasife al / Kullanımdan kaldır" kartın altındaki özgün düğmededir — ikisi tekrar
    /// edilmez. Diğer eylem yeni bir pencere açar: önce kart kapanır (iki çekmece üst üste binmez).
    /// </summary>
    private IReadOnlyList<SatirEylemleri.Eylem> YildizKartEylemleri(CariVeri c) =>
        [.. CariEylemleri(c)
            .Where(e => !e.Id.StartsWith("c-duzenle-", StringComparison.Ordinal) && !e.Id.StartsWith("c-pasife-al-", StringComparison.Ordinal))
            .Select(e => new SatirEylemleri.Eylem(e.Id, e.Etiket,
                EventCallback.Factory.Create(this, async () => { duzenleCari = null; await e.Tikla.InvokeAsync(); }),
                e.Ikon, e.Tehlike, e.Kilitli, e.KilitNedeni, e.AriaEtiket, e.EkSinif))];
}
