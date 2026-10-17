using Kuyumcu.Contracts.Sayfalar.Siparisler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Sipariş ve tamir "Yıldız" yüzü (tuval Y_Siparisvetamir · BİREBİR).
// Yalnız GÖRÜNÜM: satırlar sayfanın MEVCUT listesidir (GET /siparisler); "Yeni iş" özgün formu,
// satır ve "Teslim et" MEVCUT ayrıntı/düzenleme ekranını (/siparisler/{id}) açar. Yeni istek YOK.
// Tema temizliği (17.10): tek şema Yıldız — bu yüz her zaman çizilir; özgün gövde "Tüm ayrıntılar" arkasındadır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Siparisler
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "siparis";

    private bool yildizAyrinti;
    private string yildizSekme = "acik";
    private string yildizArama = "";

    /// <summary>Tuval sekmeleri (saf): açık = teslim edilmemiş ve iptal olmayan · hazır · teslim edildi.</summary>
    public static bool YildizSekmeyeUyar(string durum, string sekme) => sekme switch
    {
        "hazir" => durum == "HAZIR",
        "teslim" => durum == "TESLIM_EDILDI",
        _ => durum is not ("TESLIM_EDILDI" or "IPTAL"),
    };

    /// <summary>"Teslim et" kilidi (saf): yalnız sunucunun izin verdiği geçişte açık (UX-S30: sebep yazar).</summary>
    public static bool YildizTeslimAcik(Satir s) =>
        s.IzinliGecisler?.Contains("TESLIM_EDILDI") == true;

    /// <summary>Arama (saf): sipariş no, müşteri ve iş açıklaması içinde.</summary>
    public static bool YildizAramayaUyar(Satir s, string? q) =>
        string.IsNullOrWhiteSpace(q)
        || s.No.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)
        || (s.CariAd ?? "").Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)
        || (s.Aciklama ?? "").Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase);

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var tum = liste?.Satirlar ?? [];
            var sekmede = tum.Where(s => YildizSekmeyeUyar(s.Durum, yildizSekme)).ToList();
            var gorunen = sekmede.Where(s => YildizAramayaUyar(s, yildizArama)).ToList();
            var satirlar = gorunen.Select(s => new YildizListeSatiri(s.Id,
            [
                new(string.IsNullOrWhiteSpace(s.Aciklama) ? $"{TurMetni(s.Tur)} · {s.No}" : s.Aciklama!.Trim(), YildizHucreTuru.Ad, $"yildiz-siparis-is-{s.Id}"),
                new(s.CariAd ?? "—"),
                new(s.SozVerilenGun is { } g ? YildizListeBicim.KisaGun(g, bugun, dil) : "—"),
                new(DurumMetni(s.Durum)),
            ],
            YildizTeslimAcik(s)
                ? new YildizEylem(YildizSiparisSozlugu.TeslimEt(dil), $"sip-detay-{s.Id}", Href: $"/siparisler/{s.Id}")
                : new YildizEylem(YildizSiparisSozlugu.TeslimEt(dil), $"sip-detay-{s.Id}",
                                  KilitSebebi: s.Durum is "TESLIM_EDILDI" or "IPTAL" ? DurumMetni(s.Durum) : YildizSiparisSozlugu.TeslimKilit(dil)),
            EventCallback.Factory.Create(this, () => Nav.NavigateTo($"/siparisler/{s.Id}")),
            AcEtiketi: YildizSiparisSozlugu.Ayrinti(s.No, dil))).ToList();
            return new YildizListeBaglami
            {
                Kok = "yildiz-siparis",
                Tur = "siparis",
                Ust = YildizSiparisSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/siparisler", Dil),
                AramaId = "yildiz-siparis-arama",
                AramaEtiket = YildizSiparisSozlugu.AraYerTutucu(dil),
                AramaYerTutucu = YildizSiparisSozlugu.AraYerTutucu(dil),
                Arama = yildizArama,
                AramaDegisti = EventCallback.Factory.Create<string?>(this, v => yildizArama = v ?? ""),
                B1Etiket = YildizSiparisSozlugu.YeniIs(dil),
                B1Id = "sip-yeni-ac",
                B1Tikla = EventCallback.Factory.Create(this, YildizYeniIs),
                SekmelerId = "yildiz-siparis-sekmeler",
                Sekmeler =
                [
                    new("acik", YildizSiparisSozlugu.Acik(dil), yildizSekme == "acik"),
                    new("hazir", YildizSiparisSozlugu.Hazir(dil), yildizSekme == "hazir"),
                    new("teslim", YildizSiparisSozlugu.TeslimEdildi(dil), yildizSekme == "teslim"),
                ],
                SekmeSec = EventCallback.Factory.Create<string>(this, k => yildizSekme = k),
                Sutunlar =
                [
                    new(YildizSiparisSozlugu.Is(dil)), new(YildizSiparisSozlugu.Musteri(dil)),
                    new(YildizSiparisSozlugu.Teslim(dil)), new(YildizSiparisSozlugu.Durum(dil)),
                ],
                EylemSutunu = true,
                Satirlar = satirlar,
                Yukleniyor = yukleniyor,
                BosMetni = hata is not null || liste is null ? null
                    : sekmede.Count == 0 ? YildizSiparisSozlugu.SekmeBos(dil) : YildizSiparisSozlugu.AramaBos(dil),
                SinirNotu = liste is { DahaFazlasiVar: true } l
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, Dil.T.Siparis.SinirUyarisi, l.AzamiLimit) : null,
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiKapat = EventCallback.Factory.Create(this, YildizListeyeDon),
            };
        }
    }

    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata);
        b.AddAttribute(2, nameof(HataBandi.Id), "sip-hata-bandi");
        b.CloseComponent();
        b.OpenComponent<BasariBandi>(3);
        b.AddAttribute(4, nameof(BasariBandi.Mesaj), basari);
        b.AddAttribute(5, nameof(BasariBandi.Jeton), basariJeton);
        b.AddAttribute(6, nameof(BasariBandi.SureliMs), 5000);
        b.AddAttribute(7, nameof(BasariBandi.Id), "sip-basari-bandi");
        b.CloseComponent();
    };

    // ── YILDIZ-5A tur 2 (tasarım hakemi KRİTİK): "Yeni iş" eski ekrana GEÇMEZ ────────────────────────────────
    // Form tuval form kalıbında (YildizForm · Y_Gidervevirman): "Tür" seçimi (tuval "Ne yapıyorsun" satırı) · Müşteri ·
    // (şube) · İş tanımı zorunlu; ürün/ayar/gram/adet, söz günü ve açıklama "+ Ayrıntı ekle" arkasında; birden çok kalem
    // ve teklif "Tüm ayrıntılar"da (özgün form). Alanlar özgün formun bileşenleri (aynı kimlik, aynı formKapisi);
    // Kaydet = MEVCUT Olustur (POST /siparisler, gövde AYNI). Özgün form bu görünümde çizilmez → kimlik tek.
    private bool yildizSipEk;

    /// <summary>Yıldız'da yeni iş formu açık mı.</summary>
    private bool YildizIsFormu => formAcik && !yildizAyrinti;

    /// <summary>"Yeni iş": MEVCUT oluşturma formu (FormuAcKapat) — Yıldız form kalıbında.</summary>
    private void YildizYeniIs()
    {
        yildizAyrinti = false;
        yildizSipEk = false;
        if (!formAcik) FormuAcKapat();
    }

    private YildizFormBaglami YildizIsBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var ek = yildizSipEk || detayAcik;
            return new YildizFormBaglami
            {
                Kok = "yildiz-siparis-form",
                Ust = YildizSiparisSozlugu.Ust(dil),
                Baslik = YildizSiparisSozlugu.YeniIs(dil),
                SecimEtiketi = YildizForm5ASozlugu.Tur(dil),
                Secimler = [.. TurSecenekleri.Select(t => (t.Deger, t.Ad, fTur == t.Deger,
                    EventCallback.Factory.Create(this, () => FormTurSec(t.Deger))))],
                EkleEtiketi = ek ? YildizListeSozlugu.TumAyrintilar(dil) : YildizForm5ASozlugu.AyrintiEkle(dil),
                EkleTikla = ek ? EventCallback.Factory.Create(this, () => yildizAyrinti = true)
                               : EventCallback.Factory.Create(this, () => yildizSipEk = true),
                B1Etiket = YildizForm5ASozlugu.Kaydet(dil), B1Id = "sip-f-kaydet",
                B1Tikla = EventCallback.Factory.Create(this, Olustur),
                B1Kilitli = kaydediliyor || KalemSayiSebebi is not null,
                B1Kilit = KalemSayiSebebi,
                B2Etiket = YildizForm5ASozlugu.Vazgec(dil), B2Id = "sip-f-vazgec",
                B2Tikla = EventCallback.Factory.Create(this, FormuAcKapat),
                NotBaslik = YildizForm5ASozlugu.Not(dil),
                // TAMIR'de emanet uyarısı notun YERİNE (aynı bilgi tek yerde).
                Not = fTur == "TAMIR" ? Dil.T.Siparis.EmanetUyarisi : YildizForm5ASozlugu.SiparisNot(dil),
                Bantlar = YildizBantlar,
            };
        }
    }

    private void YildizListeyeDon()
    {
        yildizAyrinti = false;
        yildizSipEk = false;
        if (formAcik) FormuAcKapat();
    }
}
