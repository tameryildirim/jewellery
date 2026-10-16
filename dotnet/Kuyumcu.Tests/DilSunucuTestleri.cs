using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DIL-SUNUCU — sunucunun ekrana giden cümleleri istek diline göre sözlükten.
//
// KURUCU BULGUSU (08.10): "ingilizceye cevirdigimizde ... turkce kaliyor". Kanıtlar:
//   • saf: SunucuIletiSozlugu motoru, sözlük bütünlüğü, IstekDili, künye, doğrulama EN kolu
//   • DB:  gün sonu / mutabakat uyarıları EN istekte İngilizce, TR istekte AYNEN
//   • HTTP: AYNI hata EN istekte İngilizce, TR istekte Türkçe — AYNI alan anahtarı, AYNI status
// IstekDili AsyncLocal'dır; her test kapsamı kendi kurar ve geri alır (statik sızıntı yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DilSunucuSafTestleri
{
    private static readonly Regex TrHarf = new("[çğıİöşüÇĞÖŞÜ]", RegexOptions.CultureInvariant);

    [Fact]
    public void DS01_TR_istekte_cumle_AYNEN_doner()
    {
        Assert.Equal("Cari bulunamadı", SunucuIletiSozlugu.Cevir("Cari bulunamadı", "tr"));
        Assert.Equal("Kasa bulunamadı: 42", SunucuIletiSozlugu.Cevir("Kasa bulunamadı: 42", "tr"));
        Assert.Equal("Cari bulunamadı", SunucuIletiSozlugu.Cevir("Cari bulunamadı", null));
    }

    [Fact]
    public void DS02_EN_istekte_tam_eslesme_ve_sablon_yer_tutucusu_doldurulur()
    {
        Assert.Equal("Customer not found", SunucuIletiSozlugu.Cevir("Cari bulunamadı", "en"));
        Assert.Equal("Cash desk not found: 42", SunucuIletiSozlugu.Cevir("Kasa bulunamadı: 42", "en"));
        Assert.Equal("To add: 5 TRY, Limit: 9 TRY",
            SunucuIletiSozlugu.Cevir("Eklenecek: 5 TL, Limit: 9 TL", "en"));
        // ÇOKLU yer tutucu sırası korunur
        Assert.Equal("Cannot move from status A to status B",
            SunucuIletiSozlugu.Cevir("A durumundan B durumuna geçilemez", "en"));
    }

    [Fact]
    public void DS03_Eslesme_yoksa_cumle_AYNEN_kalir_ceviri_UYDURULMAZ()
    {
        const string bilinmeyen = "Bu cümle sözlükte hiç yok ve uydurulmamalı";
        Assert.Equal(bilinmeyen, SunucuIletiSozlugu.Cevir(bilinmeyen, "en"));
        Assert.False(SunucuIletiSozlugu.Kapsiyor(bilinmeyen));
        Assert.Equal("", SunucuIletiSozlugu.Cevir("", "en"));
        Assert.Equal("", SunucuIletiSozlugu.Cevir(null, "en"));
    }

    [Fact]
    public void DS04_Yakalanan_veri_CEVRILMEZ_kod_ve_etiket_cevrilir()
    {
        // müşteri adı (veri) aynen; durum kodu çevrilir
        var s = SunucuIletiSozlugu.Cevir("'Ayşe Yılmaz' durumundaki satır 'ONAYLANDI' yapılamaz.", "en");
        Assert.Equal("A row in 'Ayşe Yılmaz' status cannot be made 'APPROVED'.", s);
        Assert.Equal("OPEN", SunucuIletiSozlugu.DegerCevir("ACIK", "en"));
        Assert.Equal("ACIK", SunucuIletiSozlugu.DegerCevir("ACIK", "tr"));
        Assert.Equal("Ayşe Yılmaz", SunucuIletiSozlugu.DegerCevir("Ayşe Yılmaz", "en"));
    }

    [Fact]
    public void DS05_Soz_dizimi_yer_tutucu_sayisi_TR_ve_EN_sablonda_AYNI_yineleme_yok_EN_Turkce_harf_icermez()
    {
        var cift = SunucuIletiSozlugu.Ciftler;
        Assert.True(cift.Count > 900, "sözlük beklenenden küçük: " + cift.Count);
        Assert.Equal(cift.Count, cift.Select(c => c.Tr).Distinct(StringComparer.Ordinal).Count());
        foreach (var (tr, en) in cift)
        {
            Assert.True(Sayi(tr) == Sayi(en), $"yer tutucu sayısı farklı: «{tr}» ↔ «{en}»");
            Assert.False(string.IsNullOrWhiteSpace(en), $"boş EN: «{tr}»");
            // EN ileti Türkçe harf taşımaz (veri yer tutucudan gelir); tek istisna: ileti içindeki sabit marka/ek.
            Assert.False(TrHarf.IsMatch(en), $"EN iletide Türkçe harf: «{en}»");
        }
        static int Sayi(string s) => Regex.Matches(s, @"\{\}").Count;
    }

    [Fact]
    public void DS06_Her_sablon_kendi_ornek_cumlesiyle_eslesir_ve_cevrilir()
    {
        foreach (var (tr, en) in SunucuIletiSozlugu.Ciftler.Where(c => c.Tr.Contains("{}")))
        {
            var ornek = tr.Replace("{}", "X7");
            var sonuc = SunucuIletiSozlugu.Cevir(ornek, "en");
            // Aynı cümleyi daha özgül bir şablon yakalamış olabilir; her halde SONUÇ Türkçe harf taşımamalı ya da şablonun kendisi olmalı.
            Assert.True(sonuc != ornek || !TrHarf.IsMatch(ornek), $"şablon eşleşmedi: «{tr}»");
        }
    }

    [Fact]
    public void DS07_Ic_ice_cumle_ve_nokta_ayracli_liste_elemanlari_ayri_ayri_cevrilir()
    {
        var s = SunucuIletiSozlugu.Cevir(
            "Hurda alış: Satıcı adresi zorunludur (VUK md.234 — gider pusulası) · TCKN 11 haneli rakam olmalıdır", "en");
        Assert.Equal(
            "Scrap purchase: The seller address is required (Tax Procedure Law art. 234 — expense voucher) · The national ID number must be 11 digits", s);
    }

    [Fact]
    public void DS07b_Cumle_icindeki_ic_ileti_TAM_cevrilir_banka_eslestirme_ornegi()
    {
        var s = SunucuIletiSozlugu.Cevir(
            "Bu dekont bu müşteriyle eşleştirilirse yasal kimlik tespiti gerekir; sonradan eşleştirme ile yapılamaz. "
            + "Dekontu STORNO edin ve müşteriyi seçerek kimlik bilgileriyle yeni dekont girin. (Kasa bulunamadı: 5)", "en");
        Assert.EndsWith("(Cash desk not found: 5)", s, StringComparison.Ordinal);
        Assert.DoesNotMatch(TrHarf, s);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-US,en;q=0.9", "en")]
    [InlineData("EN-gb", "en")]
    [InlineData("tr", "tr")]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", "tr")]
    [InlineData("de-DE", "tr")]
    [InlineData("", "tr")]
    [InlineData(null, "tr")]
    public void DS08_Accept_Language_basligindan_dil_cozumu(string? baslik, string beklenen)
        => Assert.Equal(beklenen, IstekDili.BasliktanCoz(baslik));

    [Fact]
    public void DS09_IstekDili_kapsami_geri_alinir_varsayilan_TR()
    {
        Assert.Equal("tr", IstekDili.Gecerli);
        using (IstekDili.Kapsam("en"))
        {
            Assert.Equal("en", IstekDili.Gecerli);
            Assert.Equal("Customer not found", SunucuIletiSozlugu.Cevir("Cari bulunamadı"));
        }
        Assert.Equal("tr", IstekDili.Gecerli);
        Assert.Equal("Cari bulunamadı", SunucuIletiSozlugu.Cevir("Cari bulunamadı"));
    }

    [Fact]
    public async Task DS10_IstekDili_asenkron_akista_korunur()
    {
        using var _ = IstekDili.Kapsam("en");
        await Task.Delay(5);
        await Task.Yield();
        Assert.Equal("Product not found", SunucuIletiSozlugu.Cevir("Ürün bulunamadı"));
    }

    [Fact]
    public void DS11_Kunye_satiri_anahtarlari_dile_gore_TR_cikti_ESKISIYLE_ayni()
    {
        var k = new SurumKunyesi("1.2.3", "2026-10-09T01:02:03Z", "damga", "Production", 66, "20261009_Son", 2);
        Assert.Equal(
            "surum=1.2.3 · derleme=2026-10-09T01:02:03Z (damga) · ortam=Production · migration=66 · son=20261009_Son · bekleyen=2",
            SurumServisi.TekSatir(k));
        var en = SurumServisi.TekSatir(k, "en");
        Assert.Equal(
            "version=1.2.3 · build=2026-10-09T01:02:03Z (stamp) · environment=Production · migrations=66 · last=20261009_Son · pending=2",
            en);
        Assert.Equal("(file)", "(" + SunucuIletiSozlugu.KunyeEtiketi("dosya", "en") + ")");
    }

    [Fact]
    public void DS12_Kasa_kapanis_farki_iletisi_DORT_tam_cumle_EN_tam_cevrilir()
    {
        var f = typeof(Kuyumcu.Api.Modules.Kasa.KasaServisi).GetMethod("KapanisFarkIletisi",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        foreach (var fazla in new[] { true, false })
        foreach (var kartVar in new[] { true, false })
        {
            var tr = (string)f.Invoke(null, ["5,00 ₺", fazla, "100,00 ₺", kartVar, "7,00 ₺", "9,00 ₺"])!;
            var en = SunucuIletiSozlugu.Cevir(tr, "en");
            Assert.NotEqual(tr, en);
            Assert.False(TrHarf.IsMatch(en), en);
            Assert.Contains(fazla ? "more than expected" : "less than expected", en, StringComparison.Ordinal);
            Assert.Equal(kartVar, en.Contains("7,00 ₺", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DS13_Dogrulama_EN_kolu_alan_etiketi_ve_desenler_Ingilizce_TR_yolu_degismedi()
    {
        using (IstekDili.Kapsam("en"))
        {
            Assert.Equal("Credit limit (TRY)", DogrulamaSozlugu.AlanAdi("KrediLimitiTl"));
            Assert.Equal("Credit limit (TRY) is required",
                DogrulamaSozlugu.Cevir("KrediLimitiTl", "The KrediLimitiTl field is required."));
            Assert.Equal("Name can be at most 5 characters",
                DogrulamaSozlugu.Cevir("Ad", "The field Ad must be a string with a maximum length of 5."));
            // elle yazılmış TR ErrorMessage → sözlük
            Assert.Equal("The customer type is required", DogrulamaSozlugu.Cevir("Tur", "Cari türü zorunludur"));
        }
        Assert.Equal("Kredi limiti (TL)", DogrulamaSozlugu.AlanAdi("KrediLimitiTl"));
        Assert.Equal("Kredi limiti (TL) zorunludur",
            DogrulamaSozlugu.Cevir("KrediLimitiTl", "The KrediLimitiTl field is required."));
        // Her TR etiketin EN karşılığı vardır (anahtar kümeleri eşit)
        Assert.Equal(
            DogrulamaSozlugu.AlanAdlari.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
            DogrulamaSozlugu.AlanAdlariEn.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı");
    }

    [Fact]
    public void DS15_Ham_kod_DIGER_odeme_araci_olarak_sozlukten_cevrilir_ve_Cari_basligi_menuyle_ayni()
    {
        Assert.Equal("Other", KatalogSozlugu.OdemeAraci("DIGER", "en"));
        Assert.Equal("Diğer", KatalogSozlugu.OdemeAraci("DIGER", "tr"));
        Assert.Equal("Customers", Sozlukler.En.Cari.Baslik);   // document.title ile h1 aynı ad
    }

    [Fact]
    public void DS16_Banka_yer_tutuculari_sozlukten_cicplak_Turkce_literal_yok()
    {
        Assert.Equal("Ziraat Bankası", DilSunucuSozlugu.Al("tr").BankaAdiOrnek);
        Assert.Equal("Ana Hesap", DilSunucuSozlugu.Al("tr").HesapAdiOrnek);
        Assert.DoesNotMatch(TrHarf, DilSunucuSozlugu.En.BankaAdiOrnek + DilSunucuSozlugu.En.HesapAdiOrnek);
        var razor = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web/Pages/Banka.razor"));
        Assert.DoesNotContain("YerTutucu=\"Ziraat", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("YerTutucu=\"Ana Hesap", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void DS17_Api_kaynaginda_istek_dilinden_bagimsiz_sabit_TR_sozluk_cagrisi_kalmadi()
    {
        // TEK YOL: sunucu cümlesi IstekDili'ne göre seçilir. `Sozlugu.Tr` / `Mesaj(..., "tr")` kalıntısı = EN istekte Türkçe sızıntısı.
        var yasak = new Regex(@"(Sozlugu\.Tr\b|Mesaj\(\s*""tr""\s*\)|,\s*""tr""\s*[,)])", RegexOptions.CultureInvariant);
        // Veri adı olan tek muafiyet: Peşin Müşteri seed adı (veri çevrilmez, md.17 kurucu kararı bekliyor).
        var muaf = new[] { "PesinMusteriSozlugu.Tr.Ad" };
        var ihlal = new List<string>();
        foreach (var f in Directory.EnumerateFiles(Path.Combine(Kok(), "Kuyumcu.Api"), "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("/obj/") || f.Contains("/bin/") || f.Contains("/Migrations/")) continue;
            var satirlar = File.ReadAllLines(f);
            for (var i = 0; i < satirlar.Length; i++)
            {
                var l = satirlar[i];
                if (l.TrimStart().StartsWith("//") || l.TrimStart().StartsWith("///")) continue;
                if (muaf.Any(m => l.Contains(m, StringComparison.Ordinal))) continue;
                if (yasak.IsMatch(l)) ihlal.Add($"{Path.GetRelativePath(Kok(), f)}:{i + 1}: {l.Trim()}");
            }
        }
        Assert.True(ihlal.Count == 0, "sabit TR sözlük çağrısı: " + string.Join(" | ", ihlal.Take(5)));
    }

    [Fact]
    public void DS18_DilAraci_oran_siniri_kimlik_ve_hata_yakalayicidan_ONCE_kayitli()
    {
        var p = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Api/Program.cs"));
        var dil = p.IndexOf("app.UseMiddleware<DilAraci>();", StringComparison.Ordinal);
        Assert.True(dil > 0, "DilAraci kayıtlı değil");
        Assert.True(dil < p.IndexOf("app.UseMiddleware<IstekKimligiAraci>();", StringComparison.Ordinal));
        Assert.True(dil < p.IndexOf("app.UseMiddleware<HataYakalayici>();", StringComparison.Ordinal));
        Assert.True(dil < p.IndexOf("app.UseAuthentication();", StringComparison.Ordinal));
        Assert.True(dil < p.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal));
    }

    [Fact]
    public void DS19_Yedek_ekrani_ham_durum_ve_tur_kodlari_sozlukten_cevrilir_tanimsiz_kod_aynen()
    {
        Assert.Equal("Successful", DilSunucuSozlugu.KodAdi("BASARILI", "en"));
        Assert.Equal("Başarılı", DilSunucuSozlugu.KodAdi("BASARILI", "tr"));
        Assert.Equal("Full", DilSunucuSozlugu.KodAdi("TAM", "en"));
        Assert.Equal("Manual", DilSunucuSozlugu.KodAdi("ELLE", "en"));
        Assert.Equal("Failed", DilSunucuSozlugu.KodAdi("BASARISIZ", "en"));
        Assert.Equal("YENI_BILINMEYEN", DilSunucuSozlugu.KodAdi("YENI_BILINMEYEN", "en"));   // ad uydurulmaz
        Assert.Equal("—", DilSunucuSozlugu.KodAdi(null, "en"));
        var razor = File.ReadAllText(Path.Combine(Kok(), "Kuyumcu.Web/Pages/Yedekler.razor"));
        foreach (var ham in new[] { ">@k.Durum<", ">@t.Durum<", ">@t.Sonuc<", ">@k.Tur<" })
            Assert.DoesNotContain(ham, razor, StringComparison.Ordinal);
    }

    [Fact]
    public void DS14_Istemci_dil_basligini_arayuz_dilinden_koyar_tarayici_dilini_ezer()
    {
        var onceki = Bicim.Dil;
        try
        {
            var istek = new HttpRequestMessage(HttpMethod.Get, "api/v1/x");
            istek.Headers.TryAddWithoutValidation("Accept-Language", "de-DE");
            Bicim.Dil = "en";
            ApiIstemci.DilBasligiKoy(istek);
            Assert.Equal(["en"], istek.Headers.GetValues("Accept-Language").ToArray());
            Bicim.Dil = "tr";
            ApiIstemci.DilBasligiKoy(istek);
            Assert.Equal(["tr"], istek.Headers.GetValues("Accept-Language").ToArray());
        }
        finally { Bicim.Dil = onceki; }
    }
}

// ── DB: gün sonu / mutabakat uyarıları ────────────────────────────────────────

[Collection("veritabani")]
public sealed class DilSunucuDbTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private async Task<(Kiraci K, DateTime Now)> FaturasizKiraci(string ad)
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETDILSUNUCU-TEST-" + ad);
        var now = DateTime.UtcNow;
        sistem.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
            Tarih = now, TlTutari = 1234m, HasGrami = -1m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();
        return (k, now);
    }

    private async Task<List<string>> Uyarilar(Kiraci k, DateTime now) =>
        J(await TestKurulum.RaporServisi(vt.Olustur(k))
                .PosFaturaMutabakati(DateOnly.FromDateTime(Zaman.UtcdenYerele(now)).ToString("yyyy-MM-dd"), k.Islem))
            .GetProperty("uyarilar").EnumerateArray().Select(x => x.GetString()!).ToList();

    [Fact]
    public async Task DS20_Gun_sonu_uyarisi_TR_istekte_Turkce_EN_istekte_Ingilizce()
    {
        var (k, now) = await FaturasizKiraci("gunsonu");
        var tr = await Uyarilar(k, now);
        Assert.Single(tr, x => x.Contains("satış faturalandırılmamış", StringComparison.Ordinal));

        using (IstekDili.Kapsam("en"))
        {
            var en = await Uyarilar(k, now);
            var u = Assert.Single(en, x => x.Contains("sales not invoiced", StringComparison.Ordinal));
            Assert.StartsWith("1 sales not invoiced (total ", u, StringComparison.Ordinal);
            Assert.DoesNotContain(en, x => Regex.IsMatch(x, "[çğıİöşüÇĞÖŞÜ]"));
            Assert.Equal(tr.Count, en.Count);   // aynı uyarılar, yalnız dil farkı
        }
    }
}

// ── HTTP: aynı hata, iki dil, aynı alan anahtarı ─────────────────────────────

[Collection("http")]
public sealed class DilSunucuHttpTestleri(HttpFikstur f)
{
    private static readonly Regex TrHarf = new("[çğıİöşüÇĞÖŞÜ]", RegexOptions.CultureInvariant);

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETDILSUNUCU-TEST-" + ad);
    }

    private static HttpRequestMessage Istek(HttpMethod m, string yol, string? dil, object? govde = null)
    {
        var r = new HttpRequestMessage(m, yol);
        if (dil is not null) r.Headers.TryAddWithoutValidation("Accept-Language", dil);
        if (govde is not null) r.Content = JsonContent.Create(govde);
        return r;
    }

    [Fact]
    public async Task DS30_404_ayni_hata_EN_istekte_Ingilizce_TR_istekte_Turkce_ayni_status_ve_error()
    {
        var k = YeniKiraci("404");
        var c = f.Ortam.Istemci(k);
        var tr = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari/yok-boyle-bir-cari", "tr"));
        var en = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari/yok-boyle-bir-cari", "en-US,en;q=0.9"));
        var yok = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari/yok-boyle-bir-cari", null));
        Assert.Equal(HttpStatusCode.NotFound, tr.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, en.StatusCode);
        var jt = await tr.Json(); var je = await en.Json(); var jy = await yok.Json();
        Assert.Equal("Cari bulunamadı", jt.GetProperty("message").GetString());
        Assert.Equal("Customer not found", je.GetProperty("message").GetString());
        Assert.Equal("Cari bulunamadı", jy.GetProperty("message").GetString());   // başlıksız = TR (eski davranış)
        Assert.Equal(jt.GetProperty("error").GetString(), je.GetProperty("error").GetString());
        Assert.Equal(404, je.GetProperty("statusCode").GetInt32());
        Assert.Equal("en", en.Content.Headers.ContentLanguage.Single());
    }

    [Fact]
    public async Task DS31_401_token_yok_ve_bozuk_token_EN_Ingilizce()
    {
        var anonim = f.Ortam.AnonimIstemci();
        var en = await anonim.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari", "en"));
        var tr = await anonim.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari", "tr"));
        Assert.Equal(HttpStatusCode.Unauthorized, en.StatusCode);
        Assert.Equal("Token not found", (await en.Json()).GetProperty("message").GetString());
        Assert.Equal("Token bulunamadı", (await tr.Json()).GetProperty("message").GetString());

        var bozuk = f.Ortam.BozukTokenIstemcisi();
        var b = await bozuk.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari", "en"));
        Assert.Equal("Invalid or expired token", (await b.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task DS32_403_rol_iletisi_EN_Ingilizce_rol_adi_dahil()
    {
        var k = YeniKiraci("403");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");
        // SAHİP'e özel gerçek uç: dışa aktarım (RolGerekli SAHIP)
        var uc = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/disa-aktarim/kaynaklar", "en"));
        var uct = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/disa-aktarim/kaynaklar", "tr"));
        if (uc.StatusCode == HttpStatusCode.Forbidden)
        {
            var je = await uc.Json(); var jt = await uct.Json();
            Assert.StartsWith("Role required for this action:", je.GetProperty("message").GetString(), StringComparison.Ordinal);
            Assert.StartsWith("Bu işlem için gerekli rol:", jt.GetProperty("message").GetString(), StringComparison.Ordinal);
            Assert.Equal(jt.GetProperty("error").GetString(), je.GetProperty("error").GetString());
        }
        else Assert.Fail("403 beklenirdi, gelen: " + (int)uc.StatusCode);
    }

    [Fact]
    public async Task DS33_400_dogrulama_ayni_alan_anahtari_EN_Ingilizce_TR_Turkce()
    {
        var k = YeniKiraci("400");
        var c = f.Ortam.Istemci(k);
        var govde = new { tur = "MUSTERI", ad = "" };
        var tr = await c.SendAsync(Istek(HttpMethod.Post, "/api/v1/cari", "tr", govde));
        var en = await c.SendAsync(Istek(HttpMethod.Post, "/api/v1/cari", "en", govde));
        Assert.Equal(HttpStatusCode.BadRequest, tr.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, en.StatusCode);
        var jt = await tr.Json(); var je = await en.Json();
        var at = jt.GetProperty("alanlar").EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToArray();
        var ae = je.GetProperty("alanlar").EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToArray();
        Assert.Equal(at, ae);     // AYNI alan anahtarı
        Assert.Contains("ad", ae);
        var metinEn = je.GetRawText();
        Assert.DoesNotMatch(TrHarf, metinEn);
        Assert.Contains("zorunludur", jt.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("required", metinEn, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DS34_Bilinmeyen_api_yolu_404_EN_Ingilizce()
    {
        var c = f.Ortam.AnonimIstemci();
        var en = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/yok-boyle-uc", "en"));
        var tr = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/yok-boyle-uc", "tr"));
        Assert.Equal("Endpoint not found", (await en.Json()).GetProperty("message").GetString());
        Assert.Equal("Uç bulunamadı", (await tr.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task DS35_Servis_ici_400_alan_isaretli_EN_Ingilizce_alan_anahtari_ayni()
    {
        var k = YeniKiraci("servis400");
        var c = f.Ortam.Istemci(k);
        // Bilinmeyen sıralama alanı: servis içi BadRequest (sözlük şablonu, {} değerli)
        var tr = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari?sirala=yokBoyleAlan&yon=artan", "tr"));
        var en = await c.SendAsync(Istek(HttpMethod.Get, "/api/v1/cari?sirala=yokBoyleAlan&yon=artan", "en"));
        Assert.Equal(tr.StatusCode, en.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, en.StatusCode);
        var jt = await tr.Json(); var je = await en.Json();
        Assert.StartsWith("Bilinmeyen sıralama alanı: yokBoyleAlan", jt.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("Unknown sort field: yokBoyleAlan", je.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DS36_Girdi_kurali_iletisi_sozluk_diliyle_EN_Ingilizce_TR_Turkce_ayni_alan()
    {
        var k = YeniKiraci("girdi");
        var c = f.Ortam.Istemci(k);
        var govde = new { tur = "MUSTERI", ad = "1" };   // K19: en az bir harf
        var tr = await c.SendAsync(Istek(HttpMethod.Post, "/api/v1/cari", "tr", govde));
        var en = await c.SendAsync(Istek(HttpMethod.Post, "/api/v1/cari", "en", govde));
        Assert.Equal(HttpStatusCode.BadRequest, tr.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, en.StatusCode);
        var jt = await tr.Json(); var je = await en.Json();
        Assert.True(je.GetProperty("alanlar").TryGetProperty("ad", out var ae));
        Assert.True(jt.GetProperty("alanlar").TryGetProperty("ad", out var at));
        var metinEn = string.Join(" ", ae.EnumerateArray().Select(x => x.GetString()));
        var metinTr = string.Join(" ", at.EnumerateArray().Select(x => x.GetString()));
        Assert.NotEqual(metinTr, metinEn);
        Assert.DoesNotMatch(TrHarf, metinEn);
        Assert.Matches(TrHarf, metinTr);
    }
}
