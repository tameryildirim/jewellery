using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Modules.Isler.Isler;
using Kuyumcu.Api.Modules.Kur;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Kabuk;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-EKRAN — "İngilizceye çevirince ekran isimleri Türkçe kalıyor"
// (kurucu bulgusu 08.10)
//
// KÖK NEDEN (gerçek Chrome taramasında ölçüldü): menü/karo/favori adları
// MenuAdlari.Icin(Sozluk) ile seçilirdi ve dil `ReferenceEquals(t, Sozlukler.En)`
// ile anlaşılırdı. BASİT mod sözlüğü PRO sözlüğün KOPYASI olduğu için BASİT + EN
// kullanıcıda bütün menü, "Tüm ekranlar" karoları ve grup adları TÜRKÇE çıkıyordu.
// Bu dosya hem o kök nedeni hem de aynı SINIFI (kopya sözlükte kimlik karşılaştırması,
// sunucudan Türkçe gelen katalog adı, başlıksız sayfa) kilitler.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DilEkranTestleri : BunitContext
{
    private static readonly Regex TurkceHarf = new("[çğıİöşüÇĞÖŞÜ]", RegexOptions.Compiled);

    private static readonly IReadOnlyList<MenuOgesi> TumMenu = [.. MenuTanimi.Ogeler.Concat(MenuTanimi.AltBlok)];

    // KISA ad İKİ dilde de AYNI olabilen tek istisna: "POS" kısaltmasıdır (gerekçe: kurum adı, çevrilmez).
    private static readonly HashSet<string> KisaAdAyniOlabilir = new(StringComparer.Ordinal) { "POS" };

    // ── (1) HER MENÜ ANAHTARI İÇİN EN AD ≠ TR AD ────────────────────────────

    [Fact]
    public void Her_menu_ekraninin_EN_adi_TR_adindan_FARKLI_ve_Turkce_harf_icermez()
    {
        Assert.True(TumMenu.Count >= 40, "menü 40+ ekran taşır; sayı düştüyse tarama körleşir");
        var sorunlar = new List<string>();
        foreach (var o in TumMenu)
        {
            var tr = o.Etiket(Sozlukler.Tr); var en = o.Etiket(Sozlukler.En);
            if (tr == en) sorunlar.Add($"{o.Yol}: EN ad = TR ad ('{tr}')");
            if (TurkceHarf.IsMatch(en)) sorunlar.Add($"{o.Yol}: EN ad Türkçe harf taşıyor ('{en}')");
            var ktr = o.KisaEtiket(Sozlukler.Tr); var ken = o.KisaEtiket(Sozlukler.En);
            if (ktr == ken && !KisaAdAyniOlabilir.Contains(ken)) sorunlar.Add($"{o.Yol}: EN kısa ad = TR kısa ad ('{ktr}')");
            if (TurkceHarf.IsMatch(ken)) sorunlar.Add($"{o.Yol}: EN kısa ad Türkçe harf taşıyor ('{ken}')");
        }
        foreach (var g in MenuTanimi.GrupSirasi)
        {
            var tr = MenuTanimi.GrupEtiketi(g)(Sozlukler.Tr); var en = MenuTanimi.GrupEtiketi(g)(Sozlukler.En);
            if (tr == en || TurkceHarf.IsMatch(en)) sorunlar.Add($"grup {g}: '{tr}' / '{en}'");
        }
        Assert.True(sorunlar.Count == 0, string.Join("\n", sorunlar));
    }

    // ── (2) KÖK NEDEN: BASİT MOD KOPYA SÖZLÜĞÜ DA AYNI DİLDE KONUŞUR ────────

    [Fact]
    public void Basit_mod_sozlugu_EN_iken_menu_adlari_da_EN_dir_kok_neden()
    {
        var basitEn = BasitSozluk.Getir("en");
        Assert.False(ReferenceEquals(basitEn, Sozlukler.En), "BASİT sözlük PRO'nun kopyasıdır (kök neden bu farkın kendisi)");
        Assert.Equal("en", basitEn.DilKodu);
        Assert.Equal("tr", BasitSozluk.Getir("tr").DilKodu);
        Assert.Equal("en", basitEn.Islem.DilKodu);

        foreach (var o in TumMenu)
        {
            Assert.Equal(o.Etiket(Sozlukler.En), o.Etiket(basitEn));
            Assert.Equal(o.KisaEtiket(Sozlukler.En), o.KisaEtiket(basitEn));
            Assert.Equal(o.Etiket(Sozlukler.Tr), o.Etiket(BasitSozluk.Getir("tr")));
        }
        foreach (var g in MenuTanimi.GrupSirasi)
            Assert.Equal(MenuTanimi.GrupEtiketi(g)(Sozlukler.En), MenuTanimi.GrupEtiketi(g)(basitEn));
    }

    [Fact]
    public void Ust_bar_basliklari_EN_modda_hicbir_rotada_Turkce_harf_icermez_PRO_ve_BASIT()
    {
        var sorunlar = new List<string>();
        foreach (var (sablon, secici) in RotaBasliklari.Harita)
            foreach (var (ad, soz) in new[] { ("PRO", Sozlukler.En), ("BASİT", BasitSozluk.Getir("en")) })
            {
                var b = secici(soz);
                if (string.IsNullOrWhiteSpace(b) || TurkceHarf.IsMatch(b)) sorunlar.Add($"{sablon} [{ad}]: '{b}'");
            }
        Assert.True(sorunlar.Count == 0, string.Join("\n", sorunlar));
    }

    [Fact]
    public void Kimlik_no_hatasi_BASIT_EN_sozlukte_de_Ingilizce_ayni_sinif()
    {
        Assert.Equal(KimlikNoSozlugu.En.TcknGecersiz, KimlikNoSozlugu.Icin(BasitSozluk.Getir("en").Islem).TcknGecersiz);
        Assert.Equal(KimlikNoSozlugu.Tr.TcknGecersiz, KimlikNoSozlugu.Icin(BasitSozluk.Getir("tr").Islem).TcknGecersiz);
    }

    // ── (3) KENAR ÇUBUĞU GERÇEK RENDER: BASİT + EN ──────────────────────────

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) => """{"tercihler":{},"mesaj":"ok"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]}""",
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/bildirimler/sayi", StringComparison.Ordinal) => """{"sayi":0,"enYuksekOnem":null}""",
                var y when y.EndsWith("/bildirimler", StringComparison.Ordinal) =>
                    """{"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,"dahaFazlasiVar":false,"bildirimler":[]}""",
                var y when y.EndsWith("/isler", StringComparison.Ordinal) =>
                    """
                    {"azamiCalisma":50,
                     "ayarlar":{"baslangicSaat":0,"vadeUfukGun":7,"masakUyariGun":3,"kapaliIsler":[],
                       "saatEnAz":0,"saatEnCok":23,"vadeEnAz":1,"vadeEnCok":365,"masakEnAz":0,"masakEnCok":60,
                       "varsayilanVadeUfukGun":7,"varsayilanMasakUyariGun":3},
                     "isler":[{"kod":"VADE_HATIRLATMA","ad":"Vade hatırlatma","etkin":true,
                        "yapilandirmaEtkin":true,"kiraciKapatti":false,"sonCalisma":"2026-09-16T08:00:00Z","sonDurum":"BASARILI"}],
                     "calismalar":[]}
                    """,
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    public DilEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<CanliKurServisi>();   // YildizUstMenu (kur şeridi yalnız okunur)
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public async Task Ust_menu_ve_Tum_ekranlar_EN_modda_iki_modda_da_Ingilizce_Turkce_harf_yok(string mod)
    {
        // ESKİ: Kenar_cubugu_EN_modda_iki_modda_da_Ingilizce_Turkce_harf_yok. TEMA TEMİZLİĞİ 17.10:
        // kenar çubuğu kaldırıldı — menü artık üst menü (YildizUstMenu, hesap menüsü dahil) ve
        // "Tüm ekranlar" sayfasıdır (grup başlıkları + ekran adları + açıklamalar). İkisi birlikte sınanır.
        var dil = Services.GetRequiredService<DilServisi>();
        // Program.cs'teki bağ: BASİT mod sözlük katmanı (kopya sözlük) devreye girsin.
        dil.BasitModuBagla(() => mod == ArayuzModuServisi.Basit);
        await dil.DegistirAsync("en");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Dil", "Test", "d@ornek.com", "SAHIP", "s1", null, mod));
        var ust = Render<YildizUstMenu>();
        ust.WaitForAssertion(() => Assert.Equal(5, ust.FindAll("#yildiz-menu a.yildiz-menu-oge").Count));
        var tum = Render<Kuyumcu.Web.Pages.Ekranlar>();
        tum.WaitForAssertion(() => Assert.Equal(MenuTanimi.Ogeler.Count, tum.FindAll("a.yz-kare").Count));

        Assert.Equal(mod == ArayuzModuServisi.Basit, dil.BasitMi);
        // Görünen metin + title + aria-label + placeholder: Türkçe harf 0 ("Merkez" gibi veri harfsizdir).
        // Marka amblemi ("MİHENK") özel addır ve bilerek dilden bağımsızdır (YildizUstMenu.AmblemHarfleri,
        // YildizAmblem2Testleri.YA2_04 iki dilde aynı): yalnız o düğüm metinden düşülür, gerisi sınanır.
        var marka = ust.Find("#yildiz-marka").TextContent;
        Assert.Equal(KabukTuvalSozlugu.MarkaYazimi.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), marka.Trim());
        var gorunen = ust.Find("#yildiz-ust").TextContent.Replace(marka, " ", StringComparison.Ordinal) + " " + tum.Find("#yildiz-tum").TextContent;
        var nitelikler = string.Join(" ", ust.FindAll("[title],[aria-label]").Concat(tum.FindAll("[title],[aria-label],[placeholder]"))
            .SelectMany(x => new[] { x.GetAttribute("title"), x.GetAttribute("aria-label"), x.GetAttribute("placeholder") }));
        Assert.False(TurkceHarf.IsMatch(gorunen), $"[{mod}] menü metni Türkçe harf taşıyor: {gorunen}");
        Assert.False(TurkceHarf.IsMatch(nitelikler), $"[{mod}] menü title/aria-label Türkçe harf taşıyor: {nitelikler}");
        Assert.Contains("Daily work", tum.Find("#ekranlar-grup-baslik-gunluk").TextContent, StringComparison.Ordinal);
        Assert.Contains("Sales", tum.Find("#ekran-karo-islemler").TextContent, StringComparison.Ordinal);
        Assert.Equal("Counter", ust.Find("#yildiz-menu-tezgah").TextContent.Trim());
    }

    // ── (4) SUNUCUDAN TÜRKÇE GELEN KATALOG ADLARI ───────────────────────────

    [Fact]
    public void Kur_kalemi_adlari_sunucu_kodlariyla_BIREBIR_kapsanir_ve_EN_Turkce_degildir()
    {
        var sunucuKodlari = KurModulu_Adlar().Keys.OrderBy(x => x).ToArray();
        Assert.Equal(sunucuKodlari, DilEkranSozlugu.KurKodlari.OrderBy(x => x).ToArray());   // drift: sunucuya kod eklenirse kırmızı
        foreach (var (kod, trAd) in KurModulu_Adlar())
        {
            var en = DilEkranSozlugu.KurAdi("en", kod, trAd);
            Assert.NotEqual(trAd, en);
            // Çeyrek/Reşat gibi özel ad olan kalemlerde "ş" kalabilir; ama "Altın/Külçe/Ayar/Yeni/Eski" Türkçe sözcüğü kalamaz.
            Assert.False(TurkceHarf.IsMatch(en), en);
            Assert.DoesNotMatch(@"\b(Altın|Külçe|Ayar|Yeni|Eski|Gümüş|Hurda|Bilezik|Çeyrek|Yarım)\b", en);
            Assert.Equal(trAd, DilEkranSozlugu.KurAdi("tr", kod, trAd));   // TR'de sunucu adı AYNEN
        }
        Assert.Equal("Bilinmeyen Kalem", DilEkranSozlugu.KurAdi("en", "YOK_BOYLE_KOD", "Bilinmeyen Kalem"));   // ad uydurulmaz
    }

    private static IReadOnlyDictionary<string, string> KurModulu_Adlar() => PariteKodlari.Adlar;

    [Fact]
    public void Zamanlanmis_is_adlari_sunucu_kodlariyla_BIREBIR_kapsanir()
    {
        (string Kod, string Ad)[] sunucu =
        [
            (MasakSibSuresiIsi.IsKodu, Ad<MasakSibSuresiIsi>(x => x.Ad)),
            (SaklamaOnizlemeIsi.IsKodu, Ad<SaklamaOnizlemeIsi>(x => x.Ad)),
            (SaklamaUygulaIsi.IsKodu, Ad<SaklamaUygulaIsi>(x => x.Ad)),
            (StopajHatirlatmaIsi.IsKodu, Ad<StopajHatirlatmaIsi>(x => x.Ad)),
            (StokEsikIsi.IsKodu, Ad<StokEsikIsi>(x => x.Ad)),   // D1-15
            (KasaKapanisHatirlatmaIsi.IsKodu, Ad<KasaKapanisHatirlatmaIsi>(x => x.Ad)),   // D1-11
            (TevsikKurtarmaIsi.IsKodu, Ad<TevsikKurtarmaIsi>(x => x.Ad)),
            (VadeHatirlatmaIsi.IsKodu, Ad<VadeHatirlatmaIsi>(x => x.Ad)),
        ];
        Assert.Equal(sunucu.Select(s => s.Kod).OrderBy(x => x).ToArray(), DilEkranSozlugu.IsKodlari.OrderBy(x => x).ToArray());
        foreach (var (kod, ad) in sunucu)
        {
            var en = DilEkranSozlugu.IsAdi("en", kod, ad);
            Assert.NotEqual(ad, en);
            Assert.False(TurkceHarf.IsMatch(en), en);
            Assert.Equal(ad, DilEkranSozlugu.IsAdi("tr", kod, ad));
        }
    }

    private static string Ad<T>(Func<T, string> oku) where T : class =>
        oku((T)RuntimeHelpers.GetUninitializedObject(typeof(T)));   // Ad bir sabit döner; kurucu/DI gerekmez

    [Fact]
    public void Sunucu_cumleleri_sunucu_kaynagindaki_metinle_AYNI_ve_EN_Turkce_harf_icermez()
    {
        // Drift koruması: anahtar sunucu kaynağında birebir geçmiyorsa çeviri sessizce bayatlamıştır.
        var kaynak = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Api", "Modules", "Masak", "SaklamaSuresiIsi.cs"));
        Assert.Contains("\"Süresi dolmuş kişisel veri yok.\"", kaynak, StringComparison.Ordinal);
        Assert.Equal("No personal data past its retention period.",
            DilEkranSozlugu.SunucuMetni("en", "Süresi dolmuş kişisel veri yok."));

        var abonelik = Kuyumcu.Api.Modules.Abonelik.AbonelikServisi.YukseltmeRedMesaji;
        Assert.Contains(abonelik, DilEkranSozlugu.SunucuCumleleri);
        Assert.DoesNotMatch(TurkceHarf, DilEkranSozlugu.SunucuMetni("en", abonelik));

        foreach (var tr in DilEkranSozlugu.SunucuCumleleri)
        {
            Assert.False(TurkceHarf.IsMatch(DilEkranSozlugu.SunucuMetni("en", tr)), tr);
            Assert.Equal(tr, DilEkranSozlugu.SunucuMetni("tr", tr));   // TR'de sunucu metni AYNEN
        }
        // Eşleşmeyen metin (kullanıcı verisi) çevrilmez, ad uydurulmaz.
        Assert.Equal("Merkez Şube", DilEkranSozlugu.SunucuMetni("en", "Merkez Şube"));
        Assert.Equal("", DilEkranSozlugu.SunucuMetni("en", null));
        Assert.Equal("Identity fields will be masked in 7 records; amount, date and tier are kept.",
            DilEkranSozlugu.SunucuMetni("en", "7 kayıtta kimlik alanları maskelenecek; tutar, tarih ve kademe korunacak."));
    }

    [Fact]
    public void Kilavuz_bolum_basliklari_ve_yaptirim_dayanagi_EN_modda_Turkce_degil()
    {
        foreach (var capa in DilEkranSozlugu.KilavuzCapalari)
            Assert.False(TurkceHarf.IsMatch(DilEkranSozlugu.KilavuzBolum("en", capa, "Türkçe başlık")), capa);
        Assert.Equal("Türkçe başlık", DilEkranSozlugu.KilavuzBolum("en", "yok-capa", "Türkçe başlık"));
        Assert.Equal("Türkçe başlık", DilEkranSozlugu.KilavuzBolum("tr", "kur", "Türkçe başlık"));
        Assert.Equal(Yasal3Sozlugu.YaptirimDayanak, DilEkranSozlugu.YaptirimDayanak("tr"));
        Assert.StartsWith("Basis:", DilEkranSozlugu.YaptirimDayanak("en"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bildirimler_ekraninda_is_adi_EN_modda_Ingilizce_TR_modda_sunucu_adi()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        await dil.DegistirAsync("en");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Dil", "Test", "d@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Bildirimler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Due date reminder", e.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Vade hatırlatma", e.Markup, StringComparison.Ordinal);

        await dil.DegistirAsync("tr");
        e.WaitForAssertion(() => Assert.Contains("Vade hatırlatma", e.Markup, StringComparison.Ordinal));
    }

    // ── (8) KAYNAK KİLİDİ: sunucudan Türkçe gelen adlar ekranda sözlükten geçer ─

    [Theory]
    [InlineData("Pages/Kur.razor", "DilEkranSozlugu.KurAdi(", 2)]
    [InlineData("Pages/Bildirimler.razor", "DilEkranSozlugu.IsAdi(", 4)]
    [InlineData("Pages/Uyum.razor", "DilEkranSozlugu.SunucuMetni(", 1)]
    [InlineData("Pages/Isletme.razor", "DilEkranSozlugu.SunucuMetni(", 3)]
    [InlineData("Pages/Raporlar.razor", "DilEkranSozlugu.SunucuMetni(", 1)]
    [InlineData("Bilesenler/Uyum/YasalListePaneli.razor", "DilEkranSozlugu.YaptirimDayanak(", 1)]
    [InlineData("Pages/Kilavuz.razor", "DilEkranSozlugu.KilavuzBolum(", 1)]
    [InlineData("Pages/Urunler.razor", "DilEkranSozlugu.KayitliAdet(", 1)]
    public void Sunucu_kaynakli_ad_gosteren_ekran_DilEkranSozlugu_cagirir(string dosya, string cagri, int enAz)
    {
        var s = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", dosya));
        var adet = Regex.Matches(s, Regex.Escape(cagri)).Count;
        Assert.True(adet >= enAz, $"{dosya}: {cagri} en az {enAz} yerde çağrılmalı (bulunan {adet}) — sunucudan Türkçe gelen ad ham basılıyor");
    }

    // ── (5) HER @page SAYFASININ BAŞLIĞI VAR (document.title) ───────────────

    [Fact]
    public void Her_sayfa_razor_dosyasi_PageTitle_tasir_document_title_bos_kalmaz()
    {
        var sayfalar = Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Pages");
        var eksik = Directory.GetFiles(sayfalar, "*.razor")
            .Where(f => File.ReadAllLines(f).Take(3).Any(s => s.StartsWith("@page ", StringComparison.Ordinal)))
            .Where(f => !File.ReadAllText(f).Contains("<PageTitle>", StringComparison.Ordinal))
            .Select(Path.GetFileName).ToArray();
        Assert.True(eksik.Length == 0, "PageTitle'sız sayfa: " + string.Join(", ", eksik));
    }

    // ── (6) RAZOR İŞARETLEMEDE ÇIPLAK TÜRKÇE BAŞLIK/ETİKET YOK ──────────────

    [Fact]
    public void Razor_isaretlemesinde_ciplak_Turkce_metin_ve_title_aria_literali_yok()
    {
        var kok = Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web");
        var sorunlar = new List<string>();
        foreach (var f in Directory.GetFiles(kok, "*.razor", SearchOption.AllDirectories))
        {
            if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            var s = File.ReadAllText(f);
            s = Regex.Replace(s, @"@\*.*?\*@", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);
            s = Regex.Replace(s, @"<!--.*?-->", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);
            var i = s.IndexOf("@code", StringComparison.Ordinal);
            var isaret = i < 0 ? s : s[..i];
            var ln = 0;
            foreach (var satir in isaret.Split('\n'))
            {
                ln++;
                foreach (Match m in Regex.Matches(satir, @">([^<>@{}]*[çğıİöşüÇĞÖŞÜ][^<>@{}]*)<"))
                    sorunlar.Add($"{Path.GetFileName(f)}:{ln}: {m.Groups[1].Value.Trim()}");
                foreach (Match m in Regex.Matches(satir, @"\b(title|aria-label|placeholder|alt)=""([^""@]*[çğıİöşüÇĞÖŞÜ][^""@]*)"""))
                    sorunlar.Add($"{Path.GetFileName(f)}:{ln}: {m.Groups[1].Value}={m.Groups[2].Value}");
            }
        }
        Assert.True(sorunlar.Count == 0, string.Join("\n", sorunlar));
    }

    // ── (7) <html lang> SAYFA AÇILIRKEN KAYITLI DİLE UYAR (DAİLY WORK kusuru) ─

    [Fact]
    public void Html_lang_sayfa_acilirken_kayitli_dile_ayarlanir_noktali_I_kusuru()
    {
        var js = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "js", "dil.js"));
        // DilServisi'nin localStorage anahtarıyla AYNI anahtar okunur; kayıtlı dil uygula()'ya verilir.
        Assert.Contains("getItem('kuyumcu-dil')", js, StringComparison.Ordinal);
        Assert.Matches(@"if \(kayitli\) uygula\(kayitli\);", js);
        var servis = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Servisler", "DilServisi.cs"));
        Assert.Contains("private const string Anahtar = \"kuyumcu-dil\";", servis, StringComparison.Ordinal);
    }
}
