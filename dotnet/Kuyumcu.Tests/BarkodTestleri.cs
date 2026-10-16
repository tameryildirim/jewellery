using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BARKOD / ETİKET TESTLERİ — PLAN-2 Adım 5
//
// Code 128 "çalışıyor gibi görünen" ama okunmayan bir barkod üretmek kolaydır:
// sağlama bir yanlışsa okuyucu hiçbir şey göstermez, ekranda ise güzel duran
// çizgiler vardır. Bu yüzden burada ÇİZİMİ değil, KODLAMAYI sınıyoruz:
// sağlama değerleri bilinen örneklerle, modül dizisi bağımsız gösterimle.
//
// ⚠ Desen tablosunun kendisi ayrıca bağımsız doğrulandı: her sembolün
// "bar-boşluk genişlikleri" (6 rakam) gösterimi modül dizisine çevrilip
// tabloyla karşılaştırıldı — 107/107 eşleşti.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Code128Testleri
{
    // ── Sağlama ─────────────────────────────────────────────────────────────

    [Fact]
    public void Saglama_bilinen_ornekle_dogrulanir()
    {
        // "PJJ123C" elle hesaplanır — kodun kendisinden bağımsız:
        //   Başlangıç-B(104) + P(48×1) + J(42×2) + J(42×3) + 1(17×4)
        //   + 2(18×5) + 3(19×6) + C(35×7)
        //   = 104 + 48 + 84 + 126 + 68 + 90 + 114 + 245 = 879
        //   879 mod 103 = 55        (103×8 = 824; 879 − 824 = 55)
        Assert.Equal(55, Code128.Saglama("PJJ123C"));
    }

    [Fact]
    public void Saglama_tek_karakter_degisince_DEGISIR()
    {
        // Sağlamanın işi tam olarak bu: tek haneli okuma hatasını yakalamak.
        Assert.NotEqual(Code128.Saglama("K0123456789"), Code128.Saglama("K0123456788"));
    }

    [Fact]
    public void Saglama_103_un_altindadir()
    {
        // Sembol tablosunda 0–102 arası değer var; taşarsa IndexOutOfRange olurdu.
        foreach (var veri in new[] { "A", "ZZZZZZZZZZZZZZZZZZZZ", "K9YXWVTSRQ", "~~~~~~" })
            Assert.InRange(Code128.Saglama(veri), 0, 102);
    }

    // ── Modül dizisi ────────────────────────────────────────────────────────

    [Fact]
    public void Modul_dizisi_BAR_ile_baslar_ve_BAR_ile_biter()
    {
        // Code 128 her zaman barla başlar; bitiş deseni de barla biter.
        var m = Code128.Moduller("K0123456789");
        Assert.Equal('1', m[0]);
        Assert.Equal('1', m[^1]);
    }

    [Fact]
    public void Modul_uzunlugu_formule_uyar()
    {
        // 11 × (başlangıç + veri + sağlama) + 13 (bitiş deseni)
        const string veri = "K0123456789";
        Assert.Equal(11 * (1 + veri.Length + 1) + 13, Code128.Moduller(veri).Length);
    }

    [Fact]
    public void Bitis_deseni_standarttir()
    {
        Assert.EndsWith("1100011101011", Code128.Moduller("ABC"));
    }

    [Fact]
    public void Baslangic_deseni_Code128B_dir()
    {
        // START-B = 11010010000
        Assert.StartsWith("11010010000", Code128.Moduller("ABC"));
    }

    // ── Kodlanabilirlik ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("K0123456789", true)]
    [InlineData("Ürün-1", false)]      // Ü, ASCII 126'nın üstünde
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("a b~", true)]
    public void Kodlanabilirlik_ASCII_32_126_ile_sinirlidir(string? veri, bool beklenen)
        => Assert.Equal(beklenen, Code128.Kodlanabilir(veri));

    [Fact]
    public void Kodlanamayan_veri_SESSIZCE_gecmez()
    {
        // Türkçe karakterli bir barkod üretilirse okunmaz; hata sessizce
        // yutulursa kuyumcu bunu ancak tezgâhta fark eder.
        var hata = Assert.Throws<ArgumentException>(() => Code128.Moduller("Yüzük1"));
        Assert.Contains("ASCII 32–126", hata.Message);
    }

    // ── SVG ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Svg_gecerli_bir_kok_ogesi_uretir()
    {
        var svg = Code128.Svg("K0123456789");
        Assert.StartsWith("<svg ", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("viewBox=", svg);
    }

    [Fact]
    public void Svg_sessiz_alan_birakir()
    {
        // Sessiz alan olmadan okuyucu barkodun başladığı yeri bulamaz.
        // İlk siyah dikdörtgen 0'da değil, sessiz alan kadar sağda başlamalı.
        var svg = Code128.Svg("ABC", modulGenislik: 2, sessizAlanModul: 10);
        Assert.Contains("x=\"20\"", svg);
        Assert.DoesNotContain("<rect x=\"0\" y=\"0\"", svg);
    }

    [Fact]
    public void Svg_barkod_verisini_erisilebilir_etikete_yazar()
        => Assert.Contains("aria-label=\"barkod K0123456789\"", Code128.Svg("K0123456789"));

    [Fact]
    public void Svg_ardisik_barlari_TEK_dikdortgende_birlestirir()
    {
        // Modül başına bir <rect> üretmek DOM'u gereksiz şişirir (bir etiket
        // sayfasında 40 etiket × ~200 modül = 8.000 düğüm).
        var svg = Code128.Svg("K0123456789");
        var moduller = Code128.Moduller("K0123456789");
        var barSayisi = svg.Split("<rect").Length - 1 - 1;   // -1: arka plan dikdörtgeni
        Assert.True(barSayisi < moduller.Count(c => c == '1'),
            "Ardışık barlar birleştirilmemiş");
    }

    [Fact]
    public void Svg_XSS_tasimaz()
    {
        // Barkod kullanıcı girdisidir; aria-label'a ham geçerse MarkupString
        // ile basılan SVG saldırı yüzeyi olur.
        var svg = Code128.Svg("A<script>B");
        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);
    }
}

public sealed class BarkodUreticiTestleri
{
    [Fact]
    public void Uretilen_barkod_onek_ve_10_hane_tasir()
    {
        var b = BarkodUretici.Uret("01M2EDCQB98ZN7Y9C6PWF31QWD");
        Assert.StartsWith("K", b);
        Assert.Equal(11, b.Length);
        Assert.True(BarkodUretici.BizimBicim(b));
    }

    [Fact]
    public void Uretilen_barkod_Code128_ile_kodlanabilir()
    {
        // Üretilen her barkod basılabilmeli — yoksa etiket boş çıkar.
        var b = BarkodUretici.Uret("01M2EDCQB98ZN7Y9C6PWF31QWD");
        Assert.True(Code128.Kodlanabilir(b));
        Code128.Moduller(b);   // fırlatmamalı
    }

    [Fact]
    public void Farkli_ULIDler_farkli_barkod_verir()
    {
        var a = BarkodUretici.Uret(Kimlik.Ulid());
        var b = BarkodUretici.Uret(Kimlik.Ulid());
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Kisa_girdi_REDDEDILIR()
        => Assert.Throws<ArgumentException>(() => BarkodUretici.Uret("KISA"));

    [Fact]
    public void Elle_girilen_barkod_bizim_bicimimiz_olmak_ZORUNDA_DEGIL()
    {
        // Kuyumcunun tedarikçiden gelen kendi barkodu olabilir; reddetmiyoruz.
        Assert.False(BarkodUretici.BizimBicim("8691234567890"));
        Assert.True(Code128.Kodlanabilir("8691234567890"));
    }

    [Fact]
    public void Karistirilabilir_harfler_uretilen_barkodda_BULUNMAZ()
    {
        // ULID zaten Crockford baz32 — I, L, O, U yok. Bu test o varsayımı
        // kırılırsa haber verir (etikette elle okuma hatası doğururdu).
        for (var i = 0; i < 200; i++)
        {
            var b = BarkodUretici.Uret(Kimlik.Ulid());
            Assert.DoesNotContain(b, c => c is 'I' or 'L' or 'O' or 'U');
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN AKIŞI — otomatik barkod ve çakışma
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class BarkodUrunTestleri(TestVeritabani vt)
{
    private static UrunServisi Urun(KuyumcuDbContext db) => new(db);

    private static UrunOlusturDto Dto(string? barkod = null) => new()
    {
        Tip = "ZIYNET", Kod = $"B-{Kimlik.Ulid()[^10..]}", Ad = "Barkod testi",
        AyarId = "K22", GramAgirlik = 5m, Barkod = barkod,
    };

    [Fact]
    public async Task Barkod_verilmezse_URETILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Barkod üretimi");
        using var db = vt.Olustur(k.TenantId);

        var dto = Dto();
        await Urun(db).Olustur(dto, k.Islem);

        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod);
        Assert.NotNull(urun.Barkod);
        Assert.True(BarkodUretici.BizimBicim(urun.Barkod));
        Assert.True(Code128.Kodlanabilir(urun.Barkod));
    }

    [Fact]
    public async Task Elle_verilen_barkod_KORUNUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Elle barkod");
        using var db = vt.Olustur(k.TenantId);

        var dto = Dto("8691234567890");
        await Urun(db).Olustur(dto, k.Islem);

        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod);
        Assert.Equal("8691234567890", urun.Barkod);
    }

    [Fact]
    public async Task Ayni_barkod_IKI_urune_verilemez()
    {
        // Okuyucu aynı barkodu iki ürüne çözemez; yanlış ürün satılır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Barkod çakışma");
        using var db = vt.Olustur(k.TenantId);
        var s = Urun(db);

        await s.Olustur(Dto("TEKRAR-1"), k.Islem);

        // P-05 (Y-2): çakışma artık ALAN ALTI 409'dur — cümle aynı, ek olarak
        // hangi alanın hatalı olduğu taşınıyor (ekran mesajı alanın altında gösterir).
        var hata = await Assert.ThrowsAsync<AlanCakismasiException>(
            () => s.Olustur(Dto("TEKRAR-1"), k.Islem));
        Assert.Contains("zaten kullanımda", hata.Message);
        Assert.Equal("barkod", hata.Alan);
        Assert.Equal(409, hata.StatusCode);
    }

    [Fact]
    public async Task Guncellemede_de_barkod_cakismasi_engellenir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Güncelleme çakışma");
        using var db = vt.Olustur(k.TenantId);
        var s = Urun(db);

        var a = Dto("ALFA-1");
        await s.Olustur(a, k.Islem);
        var b = Dto("BETA-1");
        await s.Olustur(b, k.Islem);

        var bId = (await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == b.Kod)).Id;

        // P-05 (Y-2): güncellemede de ALAN ALTI 409.
        var hata = await Assert.ThrowsAsync<AlanCakismasiException>(
            () => s.Guncelle(bId, new UrunGuncelleDto { Barkod = "ALFA-1" }, k.Islem));
        Assert.Equal("barkod", hata.Alan);
    }

    [Fact]
    public async Task Baska_kiracidaki_ayni_barkod_ENGEL_DEGILDIR()
    {
        // Barkod alanı kiracıya özeldir; iki ayrı dükkân aynı tedarikçi
        // barkodunu kullanabilir.
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "Barkod kiracı A");
        var b = TestKurulum.KiraciKur(sistem, "Barkod kiracı B");

        using (var dbA = vt.Olustur(a.TenantId))
            await Urun(dbA).Olustur(Dto("ORTAK-BARKOD"), a.Islem);

        using var dbB = vt.Olustur(b.TenantId);
        await Urun(dbB).Olustur(Dto("ORTAK-BARKOD"), b.Islem);   // fırlatmamalı

        Assert.True(await dbB.Urunler.AnyAsync(u => u.Barkod == "ORTAK-BARKOD"));
    }

    [Fact]
    public async Task Etiket_ucu_barkod_ve_ayar_bilgisini_tasir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Etiket alanları");
        using var db = vt.Olustur(k.TenantId);

        var dto = Dto();
        await Urun(db).Olustur(dto, k.Islem);
        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Kod == dto.Kod);

        var etiket = await Urun(db).Etiket(urun.Id, k.Islem);
        var metin = System.Text.Json.JsonSerializer.Serialize(etiket);

        Assert.Contains(urun.Barkod!, metin);
        Assert.Contains("\"ayarId\":\"K22\"", metin);
        Assert.Contains("hasGrami", metin);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TÜRKÇE HARF KAPANI
//
// Bu iki test, aynı hatanın iki yönünü de tutar. İkisi de üründe GERÇEKTEN
// oluştu ve ikisi de yasal metni bozuyordu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TurkceMetinTestleri
{
    [Fact]
    public void Buyutmede_i_NOKTALI_I_olur()
    {
        // ToUpperInvariant "sentetik" → "SENTETIK" (noktasız) verirdi;
        // yönetmeliğin ibaresi "SENTETİK"tir.
        Assert.Equal("SENTETİK", TurkceMetin.Buyut("sentetik"));
        Assert.Equal("LABORATUVAR ÜRETİMİ", TurkceMetin.Buyut("laboratuvar üretimi"));
    }

    [Fact]
    public void Kucultmede_I_NOKTASIZ_i_olur()
        => Assert.Equal("ışık", TurkceMetin.Kucult("IŞIK"));

    [Fact]
    public void Icerir_buyuk_I_ile_kucuk_i_yi_ESLESTIRIR()
    {
        // OrdinalIgnoreCase bunu yapamaz — kuyumcu "SENTETİK SAFİR" yazınca
        // ürün kaydı reddediliyordu.
        Assert.True(TurkceMetin.Icerir("SENTETİK SAFİR", "sentetik"));
        Assert.True(TurkceMetin.Icerir("Laboratuvar Üretimi Zümrüt", "laboratuvar üretimi"));
    }

    [Fact]
    public void Icerir_alakasiz_metni_ESLESTIRMEZ()
        => Assert.False(TurkceMetin.Icerir("doğal safir", "sentetik"));
}

// ═════════════════════════════════════════════════════════════════════════════
// SVG KÜLTÜR KAPANI — canlı doğrulamada yakalandı (2026-09-14)
//
// `{1.6:0.##}` Türkçe kültürde "1,6" üretir. SVG `viewBox="0 0 184,8 34"`
// ayrıştırılamaz ve barkod tarayıcıda HİÇ ÇİZİLMEZ — ne hata, ne uyarı,
// sadece boş bir etiket. Testler invariant kültürde koştuğu için YEŞİLDİ.
//
// Aşkın'ın notu bu yüzden duruyor: "yeşil artık bu projede kanıt sayılmıyor,
// yalnız ön koşul." Bu testler artık Türkçe kültürü zorlayarak koşar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BarkodKulturTestleri
{
    /// <summary>Gövdeyi tr-TR kültüründe koşturur, sonra eski kültürü geri verir.</summary>
    private static T TurkceKulturde<T>(Func<T> govde)
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = TurkceMetin.Kultur;
            return govde();
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }

    [Fact]
    public void TURKCE_kulturde_SVG_sayilari_NOKTA_ile_yazilir()
    {
        var svg = TurkceKulturde(() => Code128.Svg("K0123456789", modulGenislik: 1.6));

        Assert.DoesNotContain(",", svg);   // virgül = bozuk SVG
        Assert.Contains("viewBox=\"0 0 ", svg);
    }

    [Fact]
    public void TURKCE_kulturde_viewBox_ayristirilabilir_kalir()
    {
        var svg = TurkceKulturde(() => Code128.Svg("K0N9AERGV3T", yukseklik: 34, modulGenislik: 1.1));

        var bas = svg.IndexOf("viewBox=\"", StringComparison.Ordinal) + 9;
        var son = svg.IndexOf('"', bas);
        var parcalar = svg[bas..son].Split(' ');

        Assert.Equal(4, parcalar.Length);   // "0 0 <genişlik> <yükseklik>"
        Assert.All(parcalar, x => Assert.True(
            double.TryParse(x, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _),
            $"viewBox parçası ayrıştırılamıyor: '{x}'"));
    }

    [Fact]
    public void TURKCE_kulturde_bar_koordinatlari_da_gecerlidir()
    {
        var svg = TurkceKulturde(() => Code128.Svg("ABC", modulGenislik: 1.6));

        // ⚠ `x="` düz aranırsa "viewBo{x="}" de eşleşir — bu test bir kez
        // kendi kusuruyla kırmızı verdi. Bar dikdörtgenleri hedeflenir.
        foreach (var oznitelik in new[] { "<rect x=\"", " width=\"" })
        {
            var i = 0;
            while ((i = svg.IndexOf(oznitelik, i, StringComparison.Ordinal)) >= 0)
            {
                var bas = i + oznitelik.Length;
                var son = svg.IndexOf('"', bas);
                var deger = svg[bas..son];
                Assert.True(double.TryParse(deger, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _),
                    $"Geçersiz SVG sayısı: '{deger}'");
                i = son;
            }
        }
    }
}
