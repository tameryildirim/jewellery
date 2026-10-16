using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Kisit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-30 (kimlik R-4) — DEMO KİRACISI: KISIT BEYANI GİRİŞ EKRANINDA
//
// Kapatılan kural (BK-47): "hariç listesi yalnız belgede değil, DEMO
// kiracısının GİRİŞ EKRANINDA okunur". Giriş ekranı oturum açılmadan çizildiği
// için liste anonim bir uçtan (GET /api/v1/genel/kisitlar) iner.
//
// ÖLÇÜLEN ŞEYLER:
//   • Uç anonim erişilebilir ve 8 H satırını döndürür (jetonsuz 200)
//   • Koddaki dizi ile docs/HARIC-LISTESI.md AYRIŞMAZ (ikinci kopya yasağı)
//   • Ekran listeyi ÇİZER; istek patlarsa BOŞ DURUM değil HATA çizer (UX-S28)
//   • Ham durum kodu (HARIC / KAPSAM_DISI) ekrana BASILMAZ (UX-S31)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P30KisitSozlesmeTestleri
{
    /// <summary>Depo kökü — test ikili dosyasından yukarı çıkarak bulunur.</summary>
    private static string DepoKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "docs", "HARIC-LISTESI.md")))
            d = d.Parent;
        Assert.True(d is not null, "docs/HARIC-LISTESI.md bulunamadı — kısıt beyanının tek kaynağı yok.");
        return d!.FullName;
    }

    [Fact]
    public void Kisit_listesi_8_satir_benzersiz_kod_ve_bos_olmayan_baslik_tasir()
    {
        Assert.Equal(8, KisitBeyani.Liste.Count);
        Assert.Equal(8, KisitBeyani.Liste.Select(k => k.Kod).Distinct().Count());
        foreach (var k in KisitBeyani.Liste)
        {
            Assert.Matches(@"^H-[1-8]$", k.Kod);
            Assert.False(string.IsNullOrWhiteSpace(k.Baslik), $"{k.Kod} TR başlığı boş");
            Assert.False(string.IsNullOrWhiteSpace(k.BaslikEn), $"{k.Kod} EN başlığı boş");
            Assert.Contains(k.Durum,
                new[] { KisitBeyani.DurumHaric, KisitBeyani.DurumKapsamDisi, KisitBeyani.DurumSurum11 });
        }
    }

    /// <summary>
    /// İKİNCİ KOPYA YASAĞI: koddaki H kodları ile belgenin H satırları birebir
    /// aynı kümedir. Belgeye satır eklenip koda eklenmezse (ya da tersi) bu test
    /// KIRMIZI olur — demo giriş ekranı belgeden sessizce ayrışamaz.
    /// </summary>
    [Fact]
    public void Kod_ve_HARIC_LISTESI_belgesi_ayni_H_kodlarini_tasir()
    {
        var belge = File.ReadAllLines(Path.Combine(DepoKoku(), "docs", "HARIC-LISTESI.md"));
        var belgeKodlari = belge
            .Where(s => s.StartsWith("| H-", StringComparison.Ordinal))
            .Select(s => s.Split('|')[1].Trim())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var kodKodlari = KisitBeyani.Liste.Select(k => k.Kod).OrderBy(s => s, StringComparer.Ordinal).ToArray();

        Assert.Equal(belgeKodlari, kodKodlari);
        Assert.Equal("docs/HARIC-LISTESI.md", KisitBeyani.Kaynak);
    }
}

[Collection("http")]
public sealed class P30KisitHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Kisitlar_ucu_JETONSUZ_200_doner_ve_8_satir_tasir()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/genel/kisitlar");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var metin = await cevap.Content.ReadAsStringAsync();
        using var belge = JsonDocument.Parse(metin);
        var kok = belge.RootElement;

        Assert.Equal("docs/HARIC-LISTESI.md", kok.GetProperty("kaynak").GetString());
        Assert.Equal(8, kok.GetProperty("toplam").GetInt32());
        var satirlar = kok.GetProperty("kisitlar").EnumerateArray().ToArray();
        Assert.Equal(8, satirlar.Length);
        Assert.Contains(satirlar, s => s.GetProperty("kod").GetString() == "H-1");
        Assert.All(satirlar, s =>
            Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("baslikEn").GetString())));
    }
}

public sealed class P30GirisEkranTestleri : BunitContext
{
    private bool kisitPatlasin;

    public P30GirisEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => kisitPatlasin))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>'{}' varsayılanı BIRAKILMADI: dokunulan uca gerçek yanıt şekli verilir.</summary>
    private sealed class SahteApi(Func<bool> patlasin) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal))
            {
                if (patlasin())
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    { Content = new StringContent("""{"hata":"sunucu yok"}""", System.Text.Encoding.UTF8, "application/json") });

                var govde = JsonSerializer.Serialize(
                    new KisitBeyaniYaniti(KisitBeyani.Kaynak, KisitBeyani.Liste.Count, KisitBeyani.Liste),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json") });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void Giris_ekrani_KISIT_listesini_cizer_ve_kaynagi_yazar()
    {
        var e = Render<Kuyumcu.Web.Pages.Giris>();

        var liste = e.Find("#giris-kisit-liste");
        Assert.Equal(8, liste.QuerySelectorAll("li").Length);
        Assert.NotNull(e.Find("#giris-kisit-h-1"));
        Assert.Contains("docs/HARIC-LISTESI.md", e.Find("#giris-kisit-kaynak").TextContent);
    }

    /// <summary>UX-S31: ham durum kodu ekrana basılmaz, sözlükten çevrilir.</summary>
    [Fact]
    public void Giris_ekrani_HAM_durum_kodunu_basmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        var metin = e.Find("#giris-kisit").TextContent;

        Assert.DoesNotContain("KAPSAM_DISI", metin);
        Assert.DoesNotContain("SURUM_1_1", metin);
        Assert.Contains("KAPSAM DIŞI", metin);
    }

    /// <summary>UX-S28 · Anayasa md.1: hata "kayıt yok"a çevrilmez.</summary>
    [Fact]
    public void Kisit_ucu_patlarsa_BOS_DURUM_degil_HATA_cizilir()
    {
        kisitPatlasin = true;
        var e = Render<Kuyumcu.Web.Pages.Giris>();

        Assert.NotNull(e.Find("#giris-kisit-hata"));
        Assert.Empty(e.FindAll("#giris-kisit-bos"));
        Assert.Empty(e.FindAll("#giris-kisit-liste"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-30 — PROVA KANIT ZİNCİRİ (BK-45 sertleştirmesi, 2026-09-23)
//
// KUSUR (hasım doğrulayıcı): DEMO_ROTA_KRITIK sayacı AYNI ajanın 11 saniye
// arayla koştuğu iki prova dosyasını YEŞİL veriyordu; BK-45 ise "iki AYRI
// ajan … tek koşum kanıt sayılmaz" diyor. Ayrıca "3 aylık geçmiş" hiçbir
// zaman SQL ile tarih kaydırılarak üretilemez (denetim damga zinciri yalan
// söylemeye başlar — Anayasa md.1).
//
// BU TESTLER ŞUNU KİLİTLER:
//   • Betik --prova modunda koşan ajanın kimliğini FAIL-CLOSED ister
//   • Betikte tarih kaydıran SQL kalıbı YOKTUR (ve olamaz)
//   • denetim.sh iki provanın "kosan" alanının FARKLI olmasını şart koşar
//   • Üretilen prova raporları kosan + adım başlangıç/bitiş damgası taşır
// ═════════════════════════════════════════════════════════════════════════════
public sealed class P30ProvaKanitTestleri
{
    private static string DepoKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "docs", "HARIC-LISTESI.md")))
            d = d.Parent;
        Assert.True(d is not null, "depo kökü bulunamadı (docs/HARIC-LISTESI.md yok).");
        return d!.FullName;
    }

    private static string BetikYolu() => Path.Combine(
        DepoKoku(), "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js");

    private static string DenetimYolu() => Path.Combine(DepoKoku(), "dotnet", "denetim.sh");

    private static string HamDizini() => Path.Combine(
        DepoKoku(), "dokumanlar", "09-uygulama-kayitlari", "ham");

    [Fact]
    public void Betik_prova_modunda_KOSAN_kimligini_FAIL_CLOSED_ister()
    {
        var betik = File.ReadAllText(BetikYolu());
        Assert.Contains("--kosan", betik);
        Assert.Contains("const KOSAN", betik);
        Assert.Matches(@"if\s*\(!KOSAN\)[\s\S]{0,200}throw new Error", betik);
    }

    [Fact]
    public void Betik_SQL_ile_TARIH_KAYDIRMA_kalibi_icermez()
    {
        var betik = File.ReadAllText(BetikYolu());
        foreach (var kalip in new[] { "DATE_SUB", "DATE_ADD", "INSERT INTO", "docker exec" })
            Assert.DoesNotContain(kalip, betik, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"UPDATE\s+[a-z_]+\s+SET", betik);
    }

    [Fact]
    public void Denetim_iki_AYRI_ajan_kosumunu_ve_adim_damgasini_sart_kosar()
    {
        var denetim = File.ReadAllText(DenetimYolu());
        Assert.Contains("TEK-AJAN", denetim);
        Assert.Contains("len(set(kosanlar)) < 2", denetim);
        Assert.Contains("KOSAN-DAMGASIZ", denetim);
        Assert.Contains("not a.get('basladi') or not a.get('bitti')", denetim);
        Assert.Contains("insanDkAsildi", denetim);
        Assert.Contains("betikte-SQL-tarih-kaydirma", denetim);
    }

    /// <summary>
    /// Üretilmiş her prova raporu kanıt olacak biçimde damgalı olmalıdır:
    /// koşan ajan + her adımın başlangıç/bitiş zamanı. Damgasız rapor,
    /// "iki ayrı ajan" ve "süre" iddialarını denetlenemez kılar.
    /// </summary>
    [Fact]
    public void Uretilen_prova_raporlari_KOSAN_ve_adim_damgasi_tasir()
    {
        var ham = HamDizini();
        if (!Directory.Exists(ham)) return;
        var dosyalar = Directory.GetFiles(ham, "demo-prova-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc).Take(2).ToArray();
        if (dosyalar.Length == 0) return;   // henüz prova koşulmadı — iddia da edilmiyor

        var kosanlar = new List<string>();
        foreach (var d in dosyalar)
        {
            using var belge = JsonDocument.Parse(File.ReadAllText(d));
            var kok = belge.RootElement;
            Assert.True(kok.TryGetProperty("kosan", out var k) && !string.IsNullOrWhiteSpace(k.GetString()),
                $"{Path.GetFileName(d)}: 'kosan' damgası yok (BK-45 denetlenemez).");
            kosanlar.Add(k.GetString()!);
            foreach (var adim in kok.GetProperty("adimlar").EnumerateArray())
            {
                Assert.True(adim.TryGetProperty("basladi", out var b) && !string.IsNullOrWhiteSpace(b.GetString()),
                    $"{Path.GetFileName(d)}: adım başlangıç damgası yok.");
                Assert.True(adim.TryGetProperty("bitti", out var s) && !string.IsNullOrWhiteSpace(s.GetString()),
                    $"{Path.GetFileName(d)}: adım bitiş damgası yok.");
            }
        }
        Assert.All(kosanlar, k => Assert.False(string.IsNullOrWhiteSpace(k)));
    }

    // ── DOĞRULAMA TURU 3 (2026-09-23) ───────────────────────────────────────
    // GERÇEK BULGU: RUNBOOK-DEMO.md § 2 gösterim tablosunun 8. adımında yazılı
    // "/vade-takibi" rotası uygulamada YOKTU; canlı tarayıcıda "Sayfa
    // bulunamadı" çiziyordu (doğrusu "/raporlar/vade-takibi"). Prova betiği
    // API ucunu ölçtüğü için bu kusuru göremiyordu. Bu test, gösterim
    // tablosundaki her UI rotasının gerçekten bir @page karşılığı olmasını
    // şart koşar — demo müşteri önünde 404 açmaz (P-30 risk maddesi).
    [Fact]
    public void Runbook_gosterim_tablosundaki_her_rota_gercek_bir_sayfadir()
    {
        var kok = DepoKoku();
        var runbook = File.ReadAllText(Path.Combine(kok, "docs", "RUNBOOK-DEMO.md"));
        var rotalar = Regex.Matches(runbook, @"^\| \d+ \| \d+ \| `(/[a-z0-9/-]*)`", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.True(rotalar.Count >= 10,
            $"Gösterim tablosundan {rotalar.Count} rota okundu; tablo bozulmuş olabilir.");

        var sayfalar = Directory.GetFiles(Path.Combine(kok, "dotnet", "Kuyumcu.Web", "Pages"),
                                          "*.razor", SearchOption.AllDirectories);
        var tanimli = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in sayfalar)
            foreach (Match m in Regex.Matches(File.ReadAllText(d), @"^@page\s+""([^""]+)""", RegexOptions.Multiline))
                tanimli.Add(m.Groups[1].Value);

        var eksik = rotalar.Where(r => !tanimli.Contains(r)).ToList();
        Assert.True(eksik.Count == 0,
            "RUNBOOK-DEMO.md gösterim tablosunda uygulamada KARŞILIĞI OLMAYAN rota var "
            + $"(demo müşteri önünde 'Sayfa bulunamadı' açar): {string.Join(", ", eksik)}");
    }

    // Denetim betiği aynı kuralı fail-closed kapı olarak da taşımalı; testin
    // tek başına kalması kapıyı açar (mekanizma kuralı).
    [Fact]
    public void Denetim_runbook_rotalarinin_varligini_da_olcer()
    {
        var denetim = File.ReadAllText(DenetimYolu());
        Assert.Contains("RUNBOOK-ROTASI-YOK", denetim, StringComparison.Ordinal);
        Assert.Contains("P30_ROTA_YOK", denetim, StringComparison.Ordinal);
    }

    // GERÇEK BULGU (doğrulama turu 3, 2026-09-23): denetim.sh hem `zsh denetim.sh`
    // hem de `./denetim.sh` (BASH shebang — .github/workflows/ci.yml:79) ile
    // koşuluyor. zsh'e özgü ${=VAR} kelime bölme sözdizimi BASH'te
    // "bad substitution" verir ve o kontrol SESSİZCE ATLANIR — fail-closed kapı
    // fail-open olur. Bu test o sözdiziminin geri gelmesini engeller.
    [Fact]
    public void Denetim_betigi_ZSH_e_ozgu_kelime_bolme_sozdizimi_TASIMAZ()
    {
        // Yorum satırları hariç: kuralın KENDİSİ yorumda örnek olarak geçebilir.
        var denetim = string.Join("\n", File.ReadAllLines(DenetimYolu())
            .Where(satir => !satir.TrimStart().StartsWith("#", StringComparison.Ordinal)));
        var kacaklar = Regex.Matches(denetim, @"in\s+\$\{=[A-Za-z_][A-Za-z0-9_]*\}")
            .Select(m => m.Value).Distinct().ToList();
        Assert.True(kacaklar.Count == 0,
            "denetim.sh BASH ile de koşuluyor; ${=VAR} orada 'bad substitution' verip kontrolü "
            + $"sessizce atlatır (fail-open). Taşıyan yazımlar: {string.Join(", ", kacaklar)}. "
            + "Portatif karşılığı: for x in $(echo \"$VAR\").");
    }
}
