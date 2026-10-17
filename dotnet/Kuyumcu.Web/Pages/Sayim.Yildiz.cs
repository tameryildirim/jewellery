using Kuyumcu.Contracts.Sayfalar.Sayim;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

/// <summary>GET /sayim/{id} yanıtının başlangıç anı (yanıtta ZATEN var: "acildi").</summary>
public sealed record YildizSayimZaman(string Id, DateTime? Acildi = null);

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5A — Sayım "Yıldız" yüzü (tuval Y_Sayim · BİREBİR · özet kalıbı).
// Yalnız GÖRÜNÜM + OKUMA: rakam, cümle ve fark listesi sayfanın MEVCUT açık sayımıdır
// (GET /sayim/{id}); "Sayımı bitir" MEVCUT Kapat yoludur (hareketli satır kararı gerekiyorsa
// özgün ekranda sorulur), "Barkod okut" ve tüm düzeltmeler özgün ekrandadır ("ayrıntı").
// Öteki 8 şemada bu dosyanın hiçbir üyesi çizime girmez (YildizMi = false).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Sayim
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;
    [Inject] private OturumServisi YildizOturum { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "sayim";

    /// <summary>Sağdaki fark listesinde gösterilen en çok satır (tuval: 4); kalanı "+ N ürün daha" → özgün tam liste.</summary>
    public const int YildizSatirSiniri = 4;

    private bool yildizAyrinti;
    /// <summary>YILDIZ-5A tur 2: "Barkod okut" okutma alanlarını Yıldız yüzünde açar (eski ekrana geçilmez).</summary>
    private bool yildizOkut;
    /// <summary>YILDIZ-5A tur 2: "+ N ürün daha" sağdaki listeyi Yıldız kalıbında TÜM satırlara açar (eski ekrana geçmez);
    /// ardından "Tüm ayrıntılar" (satır düzeltme, iptal) özgün ekrandır.</summary>
    private bool yildizTumSatir;
    private bool yildizOtoAcildi;
    private string? yildizZamanId;
    private DateTime? yildizAcildi;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Tuval açılışı AÇIK sayımı gösterir: Yıldız'da bir kez, açık oturum varsa sayfanın Yukle yolu (salt okuma).
        if (!yildizOtoAcildi && aktif is null && oturumlar.FirstOrDefault(o => o.Durum == "ACIK") is { } acik)
        {
            yildizOtoAcildi = true;
            await Yukle(acik.Id);
            StateHasChanged();
            return;
        }
        if (aktif is { } a && a.Id != yildizZamanId)
        {
            yildizZamanId = a.Id;
            try { yildizAcildi = (await Api.GetAsync<YildizSayimZaman>($"/sayim/{a.Id}")).Acildi; }
            catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { yildizAcildi = null; }
            StateHasChanged();
        }
    }

    /// <summary>Fark listesinin sırası (saf): farkı olan sayılmış satırlar, sonra sayılmayanlar, sonra eşitler.</summary>
    public static IEnumerable<Satir> YildizSirala(IEnumerable<Satir> satirlar) =>
        satirlar.OrderBy(s => s.SayilanAdet is null ? 1 : (s.Fark ?? 0m) != 0m ? 0 : 2).ThenBy(s => s.Ad, StringComparer.CurrentCulture);

    /// <summary>Fark hücresi (saf): sayılmadıysa soluk "?", eksikte kırmızı "−1", fazlada "+1", eşitte "0".</summary>
    public static YildizHucre YildizFarkHucresi(Satir s, string dil) =>
        s.SayilanAdet is null ? new("?", YildizHucreTuru.IcSoluk)
        : (s.Fark ?? 0m) < 0m ? new(YildizListeBicim.Fark(s.Fark!.Value, dil), YildizHucreTuru.IcKotu)
        : new(YildizListeBicim.Fark(s.Fark ?? 0m, dil), YildizHucreTuru.Deger);

    /// <summary>Sayan (tuval "Hakan T."): bu ekranda sayan oturumdaki kullanıcıdır.</summary>
    private string YildizSayan()
    {
        var k = YildizOturum.Kullanici;
        if (k is null) return "—";
        var ilk = (k.Ad ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return (k.Soyad ?? "").Length > 0 ? $"{ilk} {k.Soyad![..1]}." : ilk;
    }

    private string YildizBaslangic()
    {
        if (yildizAcildi is not { } an) return "—";
        var yerel = Bicim.IsletmeZamani(an);
        return DateOnly.FromDateTime(yerel) == DateOnly.FromDateTime(Bicim.IsletmeBugun)
            ? Bicim.Saat(yerel)
            : YildizListeBicim.GunSaat(an, DateOnly.FromDateTime(Bicim.IsletmeBugun), Dil.Dil);
    }

    private YildizSayimBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            var baslik = YildizListeBicim.EkranAdi("/sayim", Dil);
            if (aktif is { } a)
            {
                var acikMi = a.Durum == "ACIK";
                return new YildizSayimBaglami
                {
                    Ust = YildizSayimSozlugu.Ust(dil),
                    Baslik = baslik,
                    Rakam = a.Sayilan,
                    RakamEki = $" / {a.ToplamSatir} {YildizSayimSozlugu.Urun(dil)}",
                    Cumle = YildizSayimSozlugu.Cumle(a.Durum, a.Sayilmayan, a.Farkli, dil),
                    B1Etiket = acikMi ? YildizSayimSozlugu.Bitir(dil) : YildizSayimSozlugu.Yeni(dil),
                    B1Tikla = acikMi ? EventCallback.Factory.Create(this, YildizBitir) : EventCallback.Factory.Create(this, YildizYeni),
                    B1Kilit = acikMi && a.Sayilan == 0 ? YildizSayimSozlugu.BitirKilit(dil) : null,
                    B2Etiket = !acikMi ? YildizSayimSozlugu.OncekiSayimlar(dil) : yildizOkut ? YildizForm5ASozlugu.Say(dil) : YildizSayimSozlugu.BarkodOkut(dil),
                    B2Tikla = !acikMi ? EventCallback.Factory.Create(this, ListeyeDon)
                        : yildizOkut ? EventCallback.Factory.Create(this, Say) : EventCallback.Factory.Create(this, () => yildizOkut = true),
                    // Hareketli satır kararı da burada sorulur (sy-hareketli-onay): "Sayımı bitir" eski ekrana geçmez.
                    OkutAcik = acikMi && (yildizOkut || a.Hareketli > 0),
                    Sutunlar =
                    [
                        new(YildizSayimSozlugu.UrunBaslik(dil)), new(YildizSayimSozlugu.Kayit(dil), Sag: true),
                        new(YildizSayimSozlugu.Sayilan(dil), Sag: true), new(YildizSayimSozlugu.Fark(dil), Sag: true),
                    ],
                    Satirlar = [.. YildizSirala(a.Satirlar).Take(yildizTumSatir ? int.MaxValue : YildizSatirSiniri).Select(s => new YildizListeSatiri(s.UrunId,
                    [
                        new(s.Ad, YildizHucreTuru.Ad, $"yildiz-sayim-ad-{s.UrunId}"),
                        new(YildizListeBicim.Adet(s.SistemAdet, dil), YildizHucreTuru.Deger),
                        new(s.SayilanAdet is { } sa ? YildizListeBicim.Adet(sa, dil) : "—", YildizHucreTuru.Deger),
                        YildizFarkHucresi(s, dil),
                    ]))],
                    Bilgi =
                    [
                        (YildizSayimSozlugu.Baslangic(dil), YildizBaslangic()),
                        (YildizSayimSozlugu.Sayan(dil), YildizSayan()),
                        (YildizSayimSozlugu.HasFarki(dil), YildizListeBicim.HasFark(a.NetFarkHas, dil)),
                    ],
                    Bantlar = YildizBantlar,
                    AyrintiAcik = yildizAyrinti,
                    AyrintiAc = !yildizTumSatir && a.Satirlar.Count > YildizSatirSiniri
                        ? EventCallback.Factory.Create(this, () => yildizTumSatir = true)
                        : EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                    AyrintiEtiketi = !yildizTumSatir && a.Satirlar.Count > YildizSatirSiniri ? YildizSayimSozlugu.Daha(a.Satirlar.Count - YildizSatirSiniri, dil) : null,
                    AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                };
            }
            return new YildizSayimBaglami
            {
                Ust = YildizSayimSozlugu.Ust(dil),
                Baslik = baslik,
                Cumle = YildizSayimSozlugu.AcikYok(dil),
                B1Etiket = YildizSayimSozlugu.Yeni(dil),
                B1Tikla = EventCallback.Factory.Create(this, YildizYeni),
                B2Etiket = YildizBolumSecilebilir ? Dil.T.Sayim.BolumleBaslat : null,
                B2Tikla = EventCallback.Factory.Create(this, YildizBolumleBaslat),
                Gecmis = true,
                Sutunlar = [new(YildizSayimSozlugu.Ad(dil)), new(YildizSayimSozlugu.Durum(dil))],
                Satirlar = [.. oturumlar.Select(o => new YildizListeSatiri(o.Id,
                [
                    new(o.Ad, YildizHucreTuru.Ad, $"yildiz-sayim-oturum-{o.Id}"),
                    new(DurumEtiketi(o.Durum)),
                ],
                null, EventCallback.Factory.Create(this, () => Yukle(o.Id)),
                Soluk: o.Durum == "IPTAL", AcEtiketi: YildizSayimSozlugu.Ac(o.Ad, dil)))],
                BosMetni = hata is null ? YildizSayimSozlugu.GecmisYok(dil) : null,
                Bantlar = YildizBantlar,
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
            };
        }
    }

    /// <summary>"Sayımı bitir": MEVCUT Kapat. YILDIZ-5A tur 2: hareketli satır kararı (sy-hareketli-onay) Yıldız yüzünde,
    /// okutma alanının yanında sorulur (OkutAcik) — eski ekrana geçilmez; gövde özgünle AYNI (hareketliSatirlariUygula).</summary>
    private async Task YildizBitir() => await Kapat();

    /// <summary>"Sayım başlat": MEVCUT yeni oturum formu (YeniAc) — YILDIZ-5A tur 2: Yıldız form kalıbında açılır.
    /// PAKET P-SAYIM (denetim Y10): TEK şubede form sorulmaz — ad otomatik, kapsam "tüm ürünler", MEVCUT Ac (POST /sayim)
    /// doğrudan çağrılır ve okutma alanı açılır. Çok şubede (şube zorunlu seçim) form açılır; hata olursa form + bant kalır.</summary>
    private async Task YildizYeni()
    {
        yildizAyrinti = false;
        if (aktif is not null) aktif = null;
        YeniAc();
        if (subeler.Count == 1) await Ac();
    }

    /// <summary>P-SAYIM (Y10): reyon/kategoriyle sınırlı sayım isteğe bağlıdır — ikincil "Reyon ya da kategori seçerek
    /// başlat" MEVCUT formu (Ad otomatik dolu) açar. Yalnız tek şubede ve reyon/kategori varken görünür.</summary>
    private void YildizBolumleBaslat()
    {
        yildizAyrinti = false;
        if (aktif is not null) aktif = null;
        YeniAc();
    }

    private bool YildizBolumSecilebilir => subeler.Count == 1 && reyonlar.Count + kategoriler.Count > 0;

    /// <summary>Yıldız'da "Sayım başlat" formu açık mı (açık oturum yok, yeni form var, ayrıntı kapalı).</summary>
    private bool YildizBaslatFormu => aktif is null && yeni is not null && !yildizAyrinti;

    /// <summary>"Sayım başlat" formu (tuval form kalıbı Y_Gidervevirman): Ad · Şube (tekse salt okunur) · Kapsam;
    /// Kaydet = MEVCUT Ac (POST /sayim), Vazgeç = formu kapatır. Alanlar Sayim.razor'un Yıldız dalında (aynı kimlikler).</summary>
    private YildizFormBaglami YildizBaslatBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizFormBaglami
            {
                Kok = "yildiz-sayim-form",
                Ust = YildizSayimSozlugu.Ust(dil),
                Baslik = YildizSayimSozlugu.Yeni(dil),
                B1Etiket = YildizForm5ASozlugu.Kaydet(dil), B1Id = "sy-ac",
                B1Tikla = EventCallback.Factory.Create(this, Ac),
                B1Kilitli = kaydediliyor,
                B2Etiket = YildizForm5ASozlugu.Vazgec(dil), B2Id = "yildiz-sayim-vazgec",
                B2Tikla = EventCallback.Factory.Create(this, () => yeni = null),
                NotBaslik = YildizForm5ASozlugu.Not(dil),
                Not = YildizForm5ASozlugu.SayimNot(dil),
                Bantlar = YildizBantlar,
            };
        }
    }

    private RenderFragment YildizBantlar => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata);
        b.AddAttribute(2, nameof(HataBandi.Id), "hata-bandi");
        b.CloseComponent();
        b.OpenComponent<BasariBandi>(3);
        b.AddAttribute(4, nameof(BasariBandi.Mesaj), basariMesaj);
        b.AddAttribute(5, nameof(BasariBandi.Jeton), basariJeton);
        b.AddAttribute(6, nameof(BasariBandi.Id), "basari-bandi");
        b.CloseComponent();
    };
}
