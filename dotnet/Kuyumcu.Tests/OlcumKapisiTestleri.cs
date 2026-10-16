using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-02 — YAYIN DAMGASI + ÖLÇÜM KAPISI + TAZE SAYAÇ TABANLARI
//   Kimlikler: B-1 (QA 42/42 rota) · B-2 (yayın damgası + ortam) ·
//              B-21 (BEKLENEN_VARLIK gerçek sayıya) · B-53 (mut.sh sürümlü) ·
//              Y-33 (42 rota ↔ 42 @page çırçırı)
//
// NEDEN BU TESTLER VAR: P-02'nin ürünü KOD DEĞİL, MEKANİZMADIR — denetim.sh
// sayaçları, kapi.sh kilidi, qa-turu.js kapsam kapısı, qa-sunucu.sh damgası.
// Mekanizmanın tek kusuru sessizce SÖKÜLEBİLİR olmasıdır: biri sayacı siler,
// tabanı yükseltir ya da --include süzgecini kaldırır ve kapı temenniye döner.
// Bu testler "sayaç duruyor mu ve HÂLÂ GERÇEĞİ ÖLÇÜYOR mu" sorusunu her
// koşumda sorar (PLAN § 1 md.1: "sayacı olmayan mekanizma DoD'u geçemez" —
// buradaki karşılığı: sayacın da sayacı olur).
//
// ⚠ Bu testler derleme çıktısını değil KAYNAK AĞACI okur; betikler derlenmez.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class OlcumKapisiTestleri
{
    /// <summary>dotnet/ dizini (Kuyumcu.sln'in bulunduğu yer).</summary>
    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    /// <summary>Depo kökü (jewellery/).</summary>
    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;

    private static string Oku(params string[] parcalar) =>
        File.ReadAllText(Path.Combine([DepoKoku(), .. parcalar]));

    private static string Denetim() => Oku("dotnet", "denetim.sh");
    private static string Kayit(params string[] p) =>
        Oku([.. new[] { "dokumanlar", "09-uygulama-kayitlari" }, .. p]);

    private static string WebDizin(string alt) =>
        Path.Combine(DotnetKoku(), "Kuyumcu.Web", alt);

    // ── Ölçüm yardımcıları: denetim.sh'taki komutların C# karşılıkları ───────

    private static int RotaSayisi() =>
        Directory.GetFiles(WebDizin("Pages"), "*.razor")
            .SelectMany(File.ReadAllLines)
            .Count(l => l.StartsWith("@page", StringComparison.Ordinal));

    private static int RecordSayisi() =>
        new[] { "Pages", "Bilesenler", "Layout" }
            .SelectMany(k => Directory.GetFiles(WebDizin(k), "*.razor"))
            .SelectMany(File.ReadAllLines)
            .Count(l => l.Contains("record ", StringComparison.Ordinal));

    /// <summary>
    /// IKiraciVarlik uygulayan varlık sayısı.
    /// <para>⚠ PAKET P-27: kapsam Data/ ile SINIRLIYDI; varlığını Modules/
    /// altına koyan bir paket sayacın GÖRÜŞ ALANI DIŞINDA kalıyordu (P-27
    /// gerçekten orada doğdu). Kapsam Kuyumcu.Api geneline genişletildi;
    /// denetim.sh de aynı genişlemeyi yaptı — ikisi aynı sayıyı ölçer.</para>
    /// </summary>
    private static int KiraciVarlikSayisi() =>
        Directory.GetFiles(Path.Combine(DotnetKoku(), "Kuyumcu.Api"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .SelectMany(File.ReadAllLines)
            .Count(l => l.Contains(": IKiraciVarlik", StringComparison.Ordinal));

    private static int SayacDegeri(string ad)
    {
        var m = Regex.Match(Denetim(), $@"^{Regex.Escape(ad)}=(\d+)\s*$", RegexOptions.Multiline);
        Assert.True(m.Success, $"denetim.sh içinde '{ad}=<sayı>' satırı YOK — sayaç sökülmüş.");
        return int.Parse(m.Groups[1].Value);
    }

    // ═════ Y-33 · ROTA_KAPSAM: 42 rota ↔ 42 @page ═══════════════════════════

    [Fact]
    public void ROTA_KAPSAM_sayaci_eq_ile_gercek_rota_sayisina_sabit()
    {
        var d = Denetim();
        var m = Regex.Match(d, @"^P02_ROTA_BEKLENEN=(\d+)\s*$", RegexOptions.Multiline);
        Assert.True(m.Success, "denetim.sh'ta P02_ROTA_BEKLENEN sayacı YOK (Y-33 çırçırı sökülmüş).");

        // Taban BELGEDEN KOPYALANMAZ, ÖLÇÜLÜR: sayaç gerçek @page sayısına eşit olmalı.
        Assert.Equal(RotaSayisi(), int.Parse(m.Groups[1].Value));

        // Başlık haritası da aynı sayıda olmalı (iki yönlü kilit).
        var harita = Kuyumcu.Web.Layout.RotaBasliklari.Harita.Count;
        Assert.Equal(RotaSayisi(), harita);

        // Karşılaştırma "-eq" olmalı: "-ge" 8 eksik varlığı sessizce geçirmişti.
        Assert.Contains("\"$P02_ROTA\" -eq \"$P02_ROTA_BEKLENEN\"", d, StringComparison.Ordinal);
    }

    // ═════ B-21 · BEKLENEN_VARLIK gerçek sayıya, -eq ile ════════════════════

    [Fact]
    public void BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit()
    {
        var d = Denetim();
        Assert.Equal(KiraciVarlikSayisi(), SayacDegeri("BEKLENEN_VARLIK"));

        // "-ge" gevşekliği KAPANDI: eksik varlık artık sessizce geçemez.
        Assert.Contains("\"$TOPLAM\" -eq \"$BEKLENEN_VARLIK\"", d, StringComparison.Ordinal);
        Assert.DoesNotContain("\"$TOPLAM\" -ge \"$BEKLENEN_VARLIK\"", d, StringComparison.Ordinal);
    }

    // ═════ SAYFA_RECORD: kapsam Pages+Bilesenler+Layout, tavan şişirilmemiş ══

    [Fact]
    public void SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit()
    {
        var d = Denetim();

        // Kapsam genişledi: record'u Bilesenler'e taşıyıp sayaç oyunlamak imkânsız.
        // ⚠ Kontrol SAYFA_RECORD SATIRININ KENDİSİNE yapılır: dosyada aynı üçlü
        //   kapsamı kullanan BAŞKA sayaçlar da var (satır içi style), onların
        //   varlığı bu sayacın daraltıldığını gizlerdi (M5 mutasyonu bunu buldu).
        var recSatiri = d.Split('\n').SingleOrDefault(l => l.StartsWith("SAYFA_RECORD=", StringComparison.Ordinal));
        Assert.NotNull(recSatiri);
        foreach (var k in new[] { "$WEB/Pages", "$WEB/Bilesenler", "$WEB/Layout", "--include='*.razor'" })
            Assert.Contains(k, recSatiri!, StringComparison.Ordinal);

        // Tavan ŞİŞİRİLMEZ: bugün ölçülen değerin ta kendisi olmalı. Tavan ölçümün
        // üstündeyse "yeni record yazma" kuralı fiilen kapalıdır (temenni olurdu).
        Assert.Equal(RecordSayisi(), SayacDegeri("SAYFA_RECORD_TAVAN"));
    }

    // ═════ R18 · .mutorj artıkları sayaçları kirletmesin ════════════════════

    [Fact]
    public void denetim_sh_razor_dizinlerini_include_suzgecsiz_TARAMAZ()
    {
        var ihlaller = Denetim().Split('\n')
            .Select((s, i) => (s, no: i + 1))
            .Where(x => !x.s.TrimStart().StartsWith('#'))
            .Where(x => Regex.IsMatch(x.s, @"grep[^|]*\$WEB/(Pages|Bilesenler|Layout)"""))
            // ⚠ Yalnız DİZİN taramaları: tek bir .cs dosyasını grepleyen satırda
            //   --include süzgeci anlamsızdır (P02-1'in RotaBasliklari.cs ölçümü gibi).
            .Where(x => !x.s.Contains("--include='*.razor'", StringComparison.Ordinal))
            .ToList();

        Assert.True(ihlaller.Count == 0,
            "include süzgeci olmayan grep .mutorj artıklarını da sayar (rota 42 yerine 47 görünür). Satırlar: "
            + string.Join(" · ", ihlaller.Select(x => $"{x.no}: {x.s.Trim()}")));

        // Artık sayacı kurulu ve tavanı gerçek artık sayısının ALTINDA değil, EŞİT olmalı.
        var artik = new[] { "Kuyumcu.Web", "Kuyumcu.Api" }
            .SelectMany(k => Directory.GetFiles(Path.Combine(DotnetKoku(), k), "*.mutorj*", SearchOption.AllDirectories))
            .Count();
        Assert.Equal(artik, SayacDegeri("P02_MUTORJ_TAVAN"));
    }

    // ═════ BK-07 · her sayacın ÖLÇÜM KOMUTU yazılı, taban dosyada sürümlü ═══

    [Fact]
    public void P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu()
    {
        var d = Denetim();
        var bas = d.IndexOf("# >>> P-02", StringComparison.Ordinal);
        var son = d.IndexOf("# <<< P-02", StringComparison.Ordinal);
        Assert.True(bas > 0 && son > bas, "denetim.sh'ta işaretli P-02 bloğu YOK (PLAN § 7 blok kuralı).");

        var blok = d[bas..son];
        // Blok içindeki her sayaç kontrolü için ölçüm komutu yorumu bulunmalı.
        var olcumSayisi = Regex.Matches(blok, @"#\s+ÖLÇÜM:").Count;
        Assert.True(olcumSayisi >= 8, $"P-02 bloğunda yalnız {olcumSayisi} ölçüm komutu yorumu var (en az 8 bekleniyor).");

        // Tabanlar sürümlü dosyada ve CANLI ÖLÇÜMLE uyuşuyor (kopyalanmış taban = 0).
        using var j = JsonDocument.Parse(Kayit("ham", "olcum-2026-09-18.json"));
        var s = j.RootElement.GetProperty("sayaclar");
        Assert.Equal(RotaSayisi(), s.GetProperty("ROTA_KAPSAM").GetProperty("deger").GetInt32());
        Assert.Equal(KiraciVarlikSayisi(), s.GetProperty("BEKLENEN_VARLIK").GetProperty("deger").GetInt32());
        Assert.Equal(RecordSayisi(), s.GetProperty("SAYFA_RECORD_TAVAN").GetProperty("deger").GetInt32());
    }

    // ═════ BK-05 · TEK_EL_IHLAL kapısı kapi.sh'ta ═══════════════════════════

    [Fact]
    public void kapi_sh_TEK_EL_IHLAL_kapisini_tasir_ve_kilit_defteri_var()
    {
        var kapi = Kayit("betikler", "kapi.sh");
        Assert.Contains("TEK_EL_IHLAL", kapi, StringComparison.Ordinal);
        // Fail-closed: ihlalde derlemeye bile geçmeden çıkılır.
        Assert.Contains("exit 3", kapi, StringComparison.Ordinal);
        Assert.Contains("TEK-EL-KILIT", kapi, StringComparison.Ordinal);

        var kilit = Kayit("ham", "TEK-EL-KILIT");
        Assert.Contains("P-0", kilit, StringComparison.Ordinal);
    }

    // ═════ B-53 · mut.sh sürümlendi ═════════════════════════════════════════

    [Fact]
    public void mut_sh_depoda_ve_geri_alma_kanitini_cmp_ile_veriyor()
    {
        var yol = Path.Combine(DotnetKoku(), "mut.sh");
        Assert.True(File.Exists(yol), "dotnet/mut.sh YOK — mutasyon kanıtı yöntemi oturumla birlikte kaybolur (B-53).");
        var mut = File.ReadAllText(yol);

        // Tek eşleşme zorunluluğu: 0 ya da >1 eşleşme kanıtı geçersiz kılar.
        Assert.Contains("ESLESME=", mut, StringComparison.Ordinal);
        // "Geri aldım" iddiası cmp ile KANITLANIR (Anayasa md.1).
        Assert.Contains("cmp -s", mut, StringComparison.Ordinal);
        // Hiçbir testi kırmayan mutasyon sessizce "başarılı" sayılmaz.
        Assert.Contains("TEST AÇIĞI", mut, StringComparison.Ordinal);
    }

    // ═════ B-1 · QA turu 42/42 rota, fail-closed, öykünme kapalı ════════════

    [Fact]
    public void qa_turu_rota_listesini_page_direktiflerinden_URETIR_ve_eksikse_BASLAMAZ()
    {
        var qa = Kayit("betikler", "qa-turu.js");

        // Liste elle yazılmaz, @page'lerden üretilir (tur 6: 39/42, 3 rota sessizce atlanmıştı).
        Assert.Contains("ROTA_KAPSAM_BEKLENEN", qa, StringComparison.Ordinal);
        Assert.Contains("--include='*.razor'", qa, StringComparison.Ordinal);
        // Fail-closed: kapsam tutmuyorsa tur BAŞLAMAZ.
        Assert.Contains("throw new Error", qa, StringComparison.Ordinal);
        Assert.Contains("Tur BAŞLAMADI", qa, StringComparison.Ordinal);

        // Betikteki beklenen kapsam gerçek rota sayısıyla aynı olmalı.
        var m = Regex.Match(qa, @"const ROTA_KAPSAM_BEKLENEN = (\d+)");
        Assert.True(m.Success);
        Assert.Equal(RotaSayisi(), int.Parse(m.Groups[1].Value));

        // ÖYKÜNME KAPALI: giriş ekrandan yapılır, token enjeksiyonu yedektir.
        Assert.Contains("ÖYKÜNME KAPALI", qa, StringComparison.Ordinal);

        // BK-39: tur süresi ölçülür, varsayılmaz.
        Assert.Contains("qa-tur-suresi.json", qa, StringComparison.Ordinal);
    }

    // ═════ B-2 · yayın damgası = ağaç damgası, ortam ÜRETİM ═════════════════

    [Fact]
    public void inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ()
    {
        var sunucu = Kayit("betikler", "qa-sunucu.sh");

        // Ortam: Development değil Production (Swagger kapalı, bekleyen migration kapısı açık).
        Assert.Contains("export ASPNETCORE_ENVIRONMENT=Production", sunucu, StringComparison.Ordinal);
        Assert.DoesNotContain("export ASPNETCORE_ENVIRONMENT=Development", sunucu, StringComparison.Ordinal);

        // Damga uyuşmazlığında 5199 AÇILMAZ (fail-closed).
        Assert.Contains(".agac-damgasi", sunucu, StringComparison.Ordinal);
        Assert.Contains("YAYIN BAYAT", sunucu, StringComparison.Ordinal);
        // Damga İÇERİK karmasıdır (mtime değil — K-20 mtime'ları oynatıyor).
        Assert.Contains("shasum", sunucu, StringComparison.Ordinal);

        // Damgayı yazan taraf: yayın betikleri.
        Assert.Contains(".agac-damgasi", Kayit("betikler", "yayinla.sh"), StringComparison.Ordinal);
        Assert.Contains(".agac-damgasi", Oku("dotnet", "yayin-al.sh"), StringComparison.Ordinal);

        // launch.json SÜRÜMLÜ betiği gösterir: geçici dizindeki kopya oturumla kaybolup
        // 5199'un hiç açılmamasına yol açıyordu (B-2'nin kökü).
        var launch = Oku(".claude", "launch.json");
        Assert.Contains("dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh", launch, StringComparison.Ordinal);
        Assert.DoesNotContain("scratchpad/qa-sunucu.sh", launch, StringComparison.Ordinal);
    }

    // ═════ B-2 · İNCELEME SUNUCUSUNUN TLS'İ GERÇEKTİR, BEYAN DEĞİL ══════════
    // 2026-09-21: 5199 üretim kipinde koşmak zorunda (yukarıdaki test), ama
    // üretim kipi TLS ister ve yerel makinede tarayıcının GÜVENDİĞİ sertifika
    // üretilemiyor (`dev-certs --trust` yönetici parolası ister). Kurucu kararı:
    // açılış kapısı GEVŞETİLMEZ — Kestrel gerçekten TLS konuşur, araya yalnız
    // döngü arayüzünde yaşayan bir köprü konur. Bu test o kararın üç şartını
    // şablon düzeyinde kilitler; biri bozulursa süit kırmızıya döner.
    [Fact]
    public void inceleme_sunucusu_GERCEK_TLS_konusur_ve_kopru_yalniz_dongu_arayuzunde_dinler()
    {
        var sunucu = Kayit("betikler", "qa-sunucu.sh");

        // 1) Kaçamak beyan YASAK: ters vekil yokken "ters vekil arkasındayım" demek
        //    yalandır (Anayasa md.1) ve TLS kapısını beyanla geçmek olurdu.
        //    Ölçüm, kabuğun bu ayarı GERÇEKTEN verebileceği TEK biçimi arar
        //    (ortam değişkeni: çift alt çizgi). Betiğin yorumlarında kuralın
        //    NEDEN konduğu iki nokta üst üsteli biçimle anlatılır; açıklamayı
        //    yasaklamak belgeyi cezalandırmak olurdu, ayarı yasaklamak yeter.
        Assert.DoesNotContain("Uretim__TersVekilArkasinda", sunucu, StringComparison.Ordinal);

        // 2) API gerçekten https dinler; düz http uç noktası ile açılmaz.
        Assert.Contains("--urls \"https://localhost:$TLS_PORTU\"", sunucu, StringComparison.Ordinal);
        Assert.DoesNotContain("--urls http://localhost:5199", sunucu, StringComparison.Ordinal);

        // 3) Tarayıcı yolu köprüden geçer ve köprü SÜRÜMLÜDÜR (oturumla kaybolmaz).
        Assert.Contains("yerel-tls-koprusu.py", sunucu, StringComparison.Ordinal);

        var kopru = Kayit("betikler", "yerel-tls-koprusu.py");
        // Köprü DIŞ arayüze bağlanamaz: dinleme adresi sabit 127.0.0.1'dir.
        Assert.Contains("dinleyici.bind((\"127.0.0.1\", dinleme_portu))", kopru, StringComparison.Ordinal);
        Assert.DoesNotContain("0.0.0.0", kopru, StringComparison.Ordinal);
        // Hedef döngü arayüzünde değilse köprü AÇILMAZ (fail-closed).
        Assert.Contains("if hedef_konak not in DONGU_KONAKLARI:", kopru, StringComparison.Ordinal);
    }

    // ═════ BK-39 · QA turu süresi devri SÜRESİZ OLAMAZ ══════════════════════
    // Hasım doğrulayıcı bulgusu (2026-09-18): ölçüm alınmadı, devir Tamer Bey
    // mührüyle yapıldı — ama sayaç UYARI kaldığı için "paketin kendi kapısı
    // kendi eksiğini geçiriyordu". Devrin bir SONU olmalı: hedef tur koştuğu
    // ya da son tarih geçtiği anda denetim KIRILIR. Bu test o sonu korur.
    [Fact]
    public void QA_TUR_SURESI_devri_son_tarihli_ve_penceresi_kapaninca_KIRAR()
    {
        var sure = JsonDocument.Parse(Kayit("ham", "qa-tur-suresi.json")).RootElement;

        // Devir kaydı eksiksiz: hangi turdan sonra ve hangi tarihe kadar.
        Assert.True(sure.TryGetProperty("devir_turu", out var devirTuru),
            "qa-tur-suresi.json 'devir_turu' taşımıyor — devir penceresi ölçülemez.");
        Assert.True(sure.TryGetProperty("devir_son_tarih", out var sonTarih),
            "qa-tur-suresi.json 'devir_son_tarih' taşımıyor — süresiz erteleme YASAK (Anayasa md.1).");
        Assert.True(devirTuru.GetInt32() >= 1);
        Assert.True(DateOnly.TryParse(sonTarih.GetString(), out _),
            $"devir_son_tarih geçerli bir tarih değil: {sonTarih.GetString()}");

        // Ölçüm hâlâ yoksa bu bir VARSAYIMDIR ve öyle işaretli kalır.
        var olcum = sure.GetProperty("gecerli_ortalama_saat");
        if (olcum.ValueKind == JsonValueKind.Null)
            Assert.Contains("VARSAYIM", sure.GetProperty("varsayim_8_saat_yerine").GetString()!,
                StringComparison.Ordinal);

        // denetim.sh P02-14 artık UYARI-ONLY değil: iki fail-closed dalı var.
        var d = Denetim();
        var blok = d[d.IndexOf("P02-14", StringComparison.Ordinal)..
                     d.IndexOf("P02-15", StringComparison.Ordinal)];
        Assert.Contains("devir_son_tarih", blok, StringComparison.Ordinal);
        Assert.Contains("devir_turu", blok, StringComparison.Ordinal);
        // 2026-09-21: ÜÇÜNCÜ DURUM eklendi — "ölçüm var ama K-20 açık" (üst sınır).
        // Eski kural bunu "hiç ölçüm yok" ile aynı sayıyordu ve tur 7'de 1,46 saat
        // ÖLÇÜLDÜĞÜ hâlde kapıyı kırdı: tüm mühendislik işi kurucunun tek bir iCloud
        // tıkına bağlandı. Üst sınır karamsar bir sayıdır (eşitleme yavaşlatır, temiz
        // süre bundan küçüktür), bu yüzden bütçeyi taşıyabilir. Kırma dalı ARTTI (3→4),
        // AZALMADI: üst sınır da son tarihe bağlıdır ve o tarih geçince kapı kırılır.
        Assert.Equal(4, Regex.Matches(blok, @"p02_kir ").Count);   // kayıt eksik + tur koştu + tarih geçti + üst sınırın son tarihi geçti
        Assert.Contains("p02_uyar", blok, StringComparison.Ordinal); // pencere açıkken görünür kalır

        // Üst sınır dalı GEVŞEK OLAMAZ: hem değeri hem SON TARİHİ okunmalı.
        Assert.Contains("ust_sinir_saat", blok, StringComparison.Ordinal);
        Assert.Contains("ust_sinir_son_tarih", blok, StringComparison.Ordinal);
        // "Hiç ölçüm yok" hâli hâlâ kapıyı KIRAR — bu dal silinemez.
        Assert.Contains("devir kaydı eksik", blok, StringComparison.Ordinal);

        // Dosya tarafı: üst sınır varsa kaynağı ve son tarihi YAZILI olmalı.
        if (sure.TryGetProperty("ust_sinir_saat", out var ust) && ust.ValueKind != JsonValueKind.Null)
        {
            Assert.True(sure.TryGetProperty("ust_sinir_kaynak", out _),
                "ust_sinir_saat var ama kaynağı yazılmamış — ölçümün nereden geldiği bilinmeli (md.1).");
            Assert.True(sure.TryGetProperty("ust_sinir_son_tarih", out var ustSon)
                        && DateOnly.TryParse(ustSon.GetString(), out _),
                "ust_sinir_son_tarih yok ya da geçersiz — süresiz üst sınır YASAK.");
        }
    }

    // ═════ B-2 DoD × K-5 · künye canlı kanıtı kaybolamaz ════════════════════
    // DoD "Ortam: Üretim" canlı kanıtı ister; kanıt bugün ALINAMAZ çünkü üretim
    // açılış kapısı geliştirme parolasını parmak iziyle reddediyor (BK-14: K-5
    // bitmeden Production açılmaz). Tehlike: K-5 kapanınca borç sessizce
    // kaybolur. Sayaç bu yüzden TERS çalışır — testi de onu korur.
    [Fact]
    public void URETIM_KUNYE_KANITI_sayaci_K5_kapaninca_FAIL_verir_ve_parolayi_BASMAZ()
    {
        var d = Denetim();
        var blok = d[d.IndexOf("P02-15", StringComparison.Ordinal)..
                     d.IndexOf("# <<< P-02", StringComparison.Ordinal)];

        // K-5 açıkken uyarı, kapandığında kanıt yoksa KIRAR (tersine çalışan kapı).
        Assert.Contains("p02_uyar", blok, StringComparison.Ordinal);
        Assert.Contains("p02_kir", blok, StringComparison.Ordinal);
        Assert.Contains("uretim-kunye-kaniti.json", blok, StringComparison.Ordinal);

        // Sır sızdırmaz: parola yalnız SHA-256 parmak izine çevrilip karşılaştırılır,
        // hiçbir dalda echo/printf ile basılmaz ve iş bitince unset edilir.
        Assert.Contains("shasum -a 256", blok, StringComparison.Ordinal);
        Assert.Contains("unset P02_PAROLA", blok, StringComparison.Ordinal);
        // Parolayı ANAN her satır ya varlık sınaması, ya parmak izi borusu, ya da unset'tir.
        // Çıktıya giden bir kullanım (boruya girmeyen echo/printf) kabul edilmez.
        // K-5 İKİ sırdır: Jwt:Secret ve MySQL parolası. Canlı ölçüm (5198 Production
        // denemesi) açılışı İLK durduranın Jwt:Secret olduğunu gösterdi; yalnız
        // parolayı ölçen sayaç, parola rotasyonundan sonra alınamayan bir kanıtı ister.
        Assert.Contains("P02_JWT", blok, StringComparison.Ordinal);
        // İKİ sır da parmak iziyle kara listeye karşı sınanır (tek sınama yetmez).
        Assert.Equal(2, Regex.Matches(blok, @"grep -qF ""\$P02_PI""").Count);
        // Üç kapanma yolu: MySQL parmak izi · Jwt DEGISTIR şablonu · Jwt parmak izi.
        Assert.Equal(3, Regex.Matches(blok, @"P02_K5_ACIK=1").Count);

        foreach (var satir in blok.Split('\n')
                     .Where(s => s.Contains("$P02_PAROLA", StringComparison.Ordinal)
                              || s.Contains("$P02_JWT", StringComparison.Ordinal)))
        {
            var izinli = Regex.IsMatch(satir, @"\[ -n ""\$P02_(PAROLA|JWT)"" \]")
                      || satir.Contains("| shasum", StringComparison.Ordinal)
                      || satir.Contains("case \"$P02_JWT\" in DEGISTIR*)", StringComparison.Ordinal)
                      || satir.TrimStart().StartsWith("unset ", StringComparison.Ordinal);
            Assert.True(izinli, $"Sır çıktıya sızabilir — denetlenmemiş kullanım: {satir.Trim()}");
        }
        Assert.DoesNotContain("echo \"$P02_PI", blok, StringComparison.Ordinal);

        // Parolayı DOĞRU kalıpla ayrıştırır. Runbook'taki
        // `grep MYSQL_ROOT_PASSWORD | head -1 | awk '{print $2}'` kalıbı
        // docker-compose.yml'deki AÇIKLAMA satırını yakalar ve 3 karakterlik
        // sahte bir jeton üretir (hasım doğrulayıcı bulgusu, 2026-09-18).
        Assert.Contains("awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/", blok, StringComparison.Ordinal);
        // (Yorumda kalıbın YANLIŞ olduğu anlatılır; yasak olan KOD satırında kullanılmasıdır.)
        var kodSatirlari = blok.Split('\n').Where(s => !s.TrimStart().StartsWith('#'));
        Assert.DoesNotContain(kodSatirlari, s => s.Contains("grep MYSQL_ROOT_PASSWORD", StringComparison.Ordinal));

        // Yanlış kalıbın neden yanlış olduğu ÖLÇÜLEBİLİR olsun: docker-compose.yml
        // içinde bu anahtardan birden fazla satır geçer, ilki yorumdur.
        // Aynı hatalı kalıp QA turu temizlik yönergesinde de vardı (2026-09-18'de
        // düzeltildi): oradan geri sızarsa her tur sonunda yanlış parola denenir.
        var qa = Kayit("betikler", "qa-turu.js");
        Assert.DoesNotContain("grep MYSQL_ROOT_PASSWORD", qa, StringComparison.Ordinal);
        Assert.Contains("MYSQL_ROOT_PASSWORD:/ {print", qa, StringComparison.Ordinal);

        var compose = File.ReadAllLines(Path.Combine(DepoKoku(), "docker-compose.yml"));
        var hepsi = compose.Count(l => l.Contains("MYSQL_ROOT_PASSWORD", StringComparison.Ordinal));
        var anahtar = compose.Count(l => Regex.IsMatch(l, @"^\s*MYSQL_ROOT_PASSWORD:"));
        Assert.True(hepsi > anahtar,
            "docker-compose.yml artık yorum satırı taşımıyorsa bu tuzak kapanmıştır; testi ve runbook notunu güncelleyin.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-30b — ÖLÇÜM KAPILARININ FAIL-OPEN ONARIMI (2026-09-23)
    //   Üç arıza P-30'un hasım doğrulayıcısı tarafından ölçüldü:
    //     ARIZA 1 — BK-45 insan bütçesi kapısı "bayrak yoksa 0" sayıyordu →
    //               kural HİÇ ÖLÇÜLMEDEN yeşil veriyordu (fail-open).
    //     ARIZA 2 — denetim.sh bash betiği olduğu hâlde zsh ile koşuluyor ve
    //               AYNI AĞAÇTA FARKLI KARAR veriyordu (bash rc=0 TEMİZ ·
    //               zsh rc=1 "6 ihlal", altısı da uydurma). Kabuk kapısı kondu.
    //     ARIZA 4 — ExecuteUpdate/Delete beyaz listesi doktrinde 4, betikte 5.
    //   Bu testler o üç onarımı KİLİTLER: kapı sökülürse test kırmızı olur.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Bir kabukla denetim.sh'ı koşar; (çıkış kodu, stdout+stderr) döner.</summary>
    private static (int Kod, string Cikti) KabukIleDenetim(string kabuk)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(kabuk)
        {
            WorkingDirectory = DotnetKoku(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("./denetim.sh");
        using var p = System.Diagnostics.Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, cikti);
    }

    /// <summary>Verilen çalışma dizininden, verilen yolla denetim.sh'ı bash ile koşar.</summary>
    private static (int Kod, string Cikti) DizindenDenetim(string calismaDizini, string betikYolu)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash")
        {
            WorkingDirectory = calismaDizini,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(betikYolu);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, cikti);
    }

    // ═════ ARIZA 5 · çağrı dizinine göre FARKLI KARAR (P-30b doğrulayıcı) ════
    // ÖLÇÜLDÜ (2026-09-23): aynı ağaçta
    //   depo kökünden  "bash dotnet/denetim.sh" → rc=1  (P-29 SLA_OLCULMEMIS_RAKAM 1)
    //   dotnet/ içinden "bash denetim.sh"        → rc=0  "DENETİM TEMİZ"
    // Kök neden: betik "cd $(dirname $0)" yaptıktan SONRA kendini göreli "$0" ile
    // okuyordu; cd sonrası "dotnet/denetim.sh" çözülmüyor → ölçüm BOŞ dönüyor →
    // doğru rakam "ölçülemedi" sayılıp kapı kırılıyordu (başka bir sayaçta aynı
    // boşluk sessizce GEÇİRİRDİ: fail-open). Onarım: yolu cd'den ÖNCE mutlakla.
    [Fact]
    public void denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR()
    {
        var d = Denetim();
        var satirlar = d.Split('\n');

        // 1) Kaynak sözleşmesi: BETIK_YOLU cd'den ÖNCE kurulur.
        var yolSatiri = Array.FindIndex(satirlar, x => x.StartsWith("BETIK_YOLU=", StringComparison.Ordinal));
        var cdSatiri = Array.FindIndex(satirlar, x => x.TrimStart().StartsWith("cd \"$(dirname", StringComparison.Ordinal));
        Assert.True(yolSatiri >= 0, "BETIK_YOLU tanımı yok — betik kendi yolunu mutlaklaştırmıyor.");
        // Ölçüm BOŞ dönerse "eşit değil" değil "ÖLÇÜLEMEDİ" denir ve kapı kırar (fail-closed).
        Assert.Contains("ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı", d, StringComparison.Ordinal);
        Assert.True(cdSatiri > yolSatiri, "cd, BETIK_YOLU'ndan ÖNCE yapılıyor — göreli $0 yine çözülmez.");

        // 2) Betiğin KENDİNİ okuyan hiçbir ölçümü göreli "$0" kullanmaz.
        var kendiniOkuyan = satirlar
            .Where(x => !x.TrimStart().StartsWith('#'))
            .Where(x => Regex.IsMatch(x, @"(grep|sed|awk)[^\n]*""\$0"""))
            .ToList();
        Assert.True(kendiniOkuyan.Count == 0,
            "Betiği göreli $0 ile okuyan ölçüm(ler) var (cd sonrası BOŞ döner): "
            + string.Join(" · ", kendiniOkuyan.Select(x => x.Trim())));

        // 3) CANLI kanıt: üç çağrı biçimi de AYNI çıkış kodunu ve aynı P-29 satırını vermeli.
        var depo = DepoKoku();
        var mutlak = Path.Combine(depo, "dotnet", "denetim.sh");
        static string P29Satiri(string cikti) => cikti.Split('\n')
            .FirstOrDefault(x => x.Contains("SLA_OLCULMEMIS_RAKAM", StringComparison.Ordinal), "(satır yok)")
            .Trim();

        // NOT: bu ağaçta aynı anda başka ajanlar da yazıyor; iki koşum ARASINDA ağaç
        // değişirse karar meşru olarak farklılaşabilir. O yüzden uyuşmazlıkta BİR KEZ
        // tekrarlanır — kalıcı fark (ARIZA 5) hep aynı yönde çıkar, geçici fark kapanır.
        (int Kod, string Cikti) a = (0, ""), b = (0, "");
        for (var deneme = 0; deneme < 2; deneme++)
        {
            a = DizindenDenetim(depo, Path.Combine("dotnet", "denetim.sh"));   // belgelenen biçim
            b = DizindenDenetim(Path.Combine(depo, "dotnet"), "./denetim.sh"); // CI biçimi
            if (a.Kod == b.Kod && P29Satiri(a.Cikti) == P29Satiri(b.Cikti)) break;
        }

        // Asıl sözleşme: KARAR çağrı dizininden bağımsızdır (temiz olmak zorunda değil).
        Assert.Equal(b.Kod, a.Kod);
        Assert.Equal(P29Satiri(b.Cikti), P29Satiri(a.Cikti));
        // Ve hiçbir çağrı biçiminde ölçüm "yapılamamış" olamaz.
        Assert.DoesNotContain("ÖLÇÜLEMEDİ", a.Cikti, StringComparison.Ordinal);
        Assert.DoesNotContain("ÖLÇÜLEMEDİ", b.Cikti, StringComparison.Ordinal);
        Assert.True(mutlak.Length > 0);
    }

    // ═════ ARIZA 5b · kendini OKUYAMAYAN betik "temiz" DİYEMEZ (fail-closed) ═
    // Betik kendi metnini ölçen sayaçlar taşır (P02-5, P-29 S-3). $0 okunabilir
    // ama BAŞKA bir dosyayı gösterirse (ör. "bash -c" ile koşum, bozuk sembolik
    // bağ) bu ölçümler BOŞ döner ve "0 ihlal" sanılırdı. DAVRANIŞ kanıtı: betik
    // metni "bash -c" ile, $0 = boş bir dosya olacak biçimde koşulur → rc=2,
    // "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı" ve HİÇBİR sonuç satırı yok.
    [Fact]
    public void denetim_sh_KENDI_YOLUNDAN_OKUYAMAZSA_OLCULEMEDI_der_ve_KIRAR()
    {
        var gecici = Path.Combine(Path.GetTempPath(), "p30b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(gecici);
        try
        {
            var sahte = Path.Combine(gecici, "denetim.sh");
            File.WriteAllText(sahte, "");   // okunabilir ama betiğin kendisi DEĞİL
            var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash")
            {
                WorkingDirectory = gecici,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(Denetim());
            psi.ArgumentList.Add(sahte);    // bash -c altında $0
            using var p = System.Diagnostics.Process.Start(psi)!;
            var cikti = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();

            Assert.Equal(2, p.ExitCode);
            Assert.Contains("ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("DENETİM TEMİZ", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("✓", cikti, StringComparison.Ordinal);   // yarım karar da karardır
        }
        finally { Directory.Delete(gecici, true); }
    }

    // İkinci savunma hattı: P02-5 GREP_INCLUDE_EKSIK sayacı, betik metnini boş
    // okursa "0 ihlal ✓" DEMEZ, payda 0 → "ÖLÇÜLEMEDİ" ile KIRAR. Blok betikten
    // SÖKÜLÜP iki girdiyle koşulur: boş dosya (✗) ve gerçek betik (✓).
    [Fact]
    public void P02_GREP_INCLUDE_sayaci_BOS_okumada_OLCULEMEDI_der_ve_KIRAR()
    {
        var d = Denetim();
        var bas = d.IndexOf("P02_INC=$(", StringComparison.Ordinal);
        var son = d.IndexOf("# ── P02-6)", StringComparison.Ordinal);
        Assert.True(bas > 0 && son > bas, "denetim.sh'ta P02-5 bloğu bulunamadı — sayaç sökülmüş.");
        var blok = d[bas..son];

        var gecici = Path.Combine(Path.GetTempPath(), "p30b-inc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(gecici);
        try
        {
            var bos = Path.Combine(gecici, "bos.sh");
            File.WriteAllText(bos, "");
            string Kos(string betikYolu)
            {
                var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash")
                { RedirectStandardOutput = true, RedirectStandardError = true };
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(
                    "HATA=0\np02_ok() { printf '  ✓ %s %s\\n' \"$1\" \"$2\"; }\n"
                    + "p02_kir() { printf '  ✗ %s %s\\n' \"$1\" \"$2\"; HATA=1; }\n"
                    + blok + "\necho \"HATA=$HATA\"\n");
                psi.Environment["BETIK_YOLU"] = betikYolu;
                using var p = System.Diagnostics.Process.Start(psi)!;
                var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                return o;
            }

            var bosCikti = Kos(bos);
            Assert.Contains("✗", bosCikti, StringComparison.Ordinal);
            Assert.Contains("ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı", bosCikti, StringComparison.Ordinal);
            Assert.Contains("HATA=1", bosCikti, StringComparison.Ordinal);

            var gercekCikti = Kos(Path.Combine(DotnetKoku(), "denetim.sh"));
            Assert.Contains("✓", gercekCikti, StringComparison.Ordinal);
            Assert.Contains("HATA=0", gercekCikti, StringComparison.Ordinal);
        }
        finally { Directory.Delete(gecici, true); }
    }

    // ═════ ARIZA 6 · müşteri belgesinde bayat satır numarası ════════════════
    // docs/SLA.md müşteriye gider; kanıt sütununda "denetim.sh:65" yazıyordu,
    // P-30b betiğe blok ekleyince gerçek satır 88 oldu. Satır numarası kayar →
    // izlenebilirlik değil, çürüyen gösterge. Atıf DESENLE verilir.
    [Fact]
    public void SLA_belgesi_denetim_sh_atiflarini_SATIR_NUMARASIYLA_vermez()
    {
        var sla = Oku("docs", "SLA.md");
        var bayat = Regex.Matches(sla, @"denetim\.sh:[0-9]+").Select(m => m.Value).ToList();
        Assert.True(bayat.Count == 0,
            "SLA.md'de denetim.sh satır numarası göstergesi: " + string.Join(", ", bayat));

        // Kapının kendisi de duruyor olmalı (belge düzelir, kapı sökülürse cırcır geri gelir).
        Assert.Contains("SLA_BAYAT_SATIR_GOSTERGESI", Denetim(), StringComparison.Ordinal);

        // Ve atıf gerçekten desenle verilmiş olmalı (boşaltıp "0" yapmak sayılmaz).
        Assert.Contains("denetle \"ExecuteUpdate/Delete\"", sla, StringComparison.Ordinal);
    }

    // ═════ ARIZA 2 · yanlış kabuk = yanlış karar ════════════════════════════
    [Fact]
    public void denetim_sh_BASH_DISI_kabukta_kosmayi_REDDEDER()
    {
        var d = Denetim();

        // Kaynak sözleşmesi (her ortamda koşar): shebang bash + BASH_VERSION kapısı + exit 2.
        Assert.StartsWith("#!/usr/bin/env bash", d, StringComparison.Ordinal);
        Assert.Contains("if [ -z \"${BASH_VERSION:-}\" ]; then", d, StringComparison.Ordinal);
        Assert.Contains("BASH içindir", d, StringComparison.Ordinal);
        // Kapı betiğin İLK KOD SATIRI olmalı: aşağı kayarsa araya giren her satır
        // yanlış kabukta koşar ve kapı anlamını yitirir.
        var ilkKod = d.Split('\n').Select(x => x.Trim())
            .First(x => x.Length > 0 && !x.StartsWith('#'));
        Assert.Contains("BASH_VERSION", ilkKod, StringComparison.Ordinal);
        // CI de betiği doğrudan (shebang ile) koşar; 'zsh denetim.sh' biçimine dönerse kapı anlamsızlaşır.
        Assert.Contains("./denetim.sh", Oku(".github", "workflows", "ci.yml"), StringComparison.Ordinal);

        // Canlı kanıt: elde bulunan BASH DIŞI bir kabukla koşulunca çıkış kodu 2 olmalı.
        var kabuk = new[] { "/bin/zsh", "/bin/dash", "/bin/ksh" }.FirstOrDefault(File.Exists);
        Assert.False(kabuk is null,
            "Bu makinede bash dışı kabuk yok — kabuk kapısı CANLI sınanamadı; kapıyı elle koşup kanıtlayın.");
        var (kod, cikti) = KabukIleDenetim(kabuk!);
        Assert.Equal(2, kod);
        Assert.Contains("BASH içindir", cikti, StringComparison.Ordinal);
        // Yanlış kabukta HİÇBİR denetim sonucu üretilmemeli (yarım karar da karardır).
        Assert.DoesNotContain("DENETİM TEMİZ", cikti, StringComparison.Ordinal);
    }

    // ═════ ARIZA 1 · BK-45 insan bütçesi kapısı FAIL-OPEN idi ═══════════════
    // Eski satır: toplam += int(veri.get('insanDkAsildi', 0) or 0)
    // Bayrak yoksa 0 sayıyordu; ham provaların hiçbirinde ölçülmüş süre yoktu.
    // Bu test kapının ÜÇ DURUMUNU (temiz / üst sınır / kırık) GERÇEKTEN koşar:
    // denetim.sh'taki python bloğunu betikten SÖKÜP uydurma provalarla çalıştırır,
    // yani "yazıda öyle yazıyor" değil "kapı öyle davranıyor" sınanır.
    [Fact]
    public void BK45_insan_suresi_kapisi_UC_DURUMLU_ve_OLCUM_YOKKEN_KIRAR()
    {
        var d = Denetim();

        // 1) Fail-open satırı GERİ GELEMEZ. (Yorumda ANILMASI serbesttir — arızanın
        //    ne olduğu yazılı kalsın diye; yasak olan KOD satırında kullanılmasıdır.)
        var denetimKodu = d.Split('\n').Where(x => !x.TrimStart().StartsWith('#')).ToList();
        Assert.DoesNotContain(denetimKodu, x => x.Contains("insanDkAsildi", StringComparison.Ordinal));

        // 2) Kapı ayrı bir sayaç olarak VAR ve üç durumu da taşır.
        var blok = d[d.IndexOf("2b) DEMO_INSAN_SURESI", StringComparison.Ordinal)..
                     d.IndexOf("3) DEMO_KISIT_AYRISMA", StringComparison.Ordinal)];
        Assert.Contains("p30_ok", blok, StringComparison.Ordinal);
        Assert.Contains("p30_uyar", blok, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(blok, @"p30_kir ").Count);   // son tarih geçti + ölçülemedi
        Assert.Contains("P30_INSAN_SON_TARIH", blok, StringComparison.Ordinal);
        Assert.Matches(@"P30_INSAN_SON_TARIH=""\d{4}-\d{2}-\d{2}""", blok);   // süresiz beyan YASAK

        // 3) DAVRANIŞ KANITI — python bloğu sökülür ve uydurma provalarla koşulur.
        var pyBas = blok.IndexOf("<<'PYEOF'", StringComparison.Ordinal) + "<<'PYEOF'\n".Length;
        var pySon = blok.IndexOf("\nPYEOF", pyBas, StringComparison.Ordinal);
        Assert.True(pySon > pyBas, "denetim.sh'ta BK-45 python bloğu bulunamadı — kapı sökülmüş.");
        var py = blok[pyBas..pySon];

        var gecici = Path.Combine(Path.GetTempPath(), "bk45-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(gecici);
        var betik = Path.Combine(gecici, "kapi.py");
        File.WriteAllText(betik, py);

        string Kos(string ad1, string json1, string ad2, string json2)
        {
            var dizin = Path.Combine(gecici, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dizin);
            File.WriteAllText(Path.Combine(dizin, "demo-prova-" + ad1 + ".json"), json1);
            Thread.Sleep(10);   // mtime sıralaması (kapı en yeni İKİ dosyayı okur)
            File.WriteAllText(Path.Combine(dizin, "demo-prova-" + ad2 + ".json"), json2);
            var psi = new System.Diagnostics.ProcessStartInfo("python3")
            { RedirectStandardOutput = true, RedirectStandardError = true };
            psi.ArgumentList.Add(betik);
            psi.ArgumentList.Add(dizin);
            using var p = System.Diagnostics.Process.Start(psi)!;
            var o = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return o;
        }

        const string Olculdu = @"{""insanDkToplam"":15,""insanDkTavan"":20,""insanSuresiOlculdu"":true}";
        const string Beyan   = @"{""insanDkToplam"":15,""insanDkTavan"":20,""insanSuresiOlculdu"":false}";
        const string Bayraksiz = @"{""insanDkToplam"":15,""insanDkTavan"":20}";
        const string HicYok  = @"{""tur"":""GOSTERIM_PROVASI"",""kritik"":0}";
        const string Asan    = @"{""insanDkToplam"":26,""insanDkTavan"":20,""insanSuresiOlculdu"":true}";

        Assert.StartsWith("TEMIZ|", Kos("a", Olculdu, "b", Olculdu));
        Assert.StartsWith("UST_SINIR|", Kos("a", Olculdu, "b", Beyan));
        // ⛔ ASIL ONARIM: ölçüm YOKSA "0 dk" sayılmaz — ayrı bir durumdur ve KIRAR.
        Assert.StartsWith("KIRIK|", Kos("a", Olculdu, "b", HicYok));
        Assert.StartsWith("KIRIK|", Kos("a", Olculdu, "b", Bayraksiz));
        // 20 dakika tavanı gerçekten ölçülüyor (BK-45'in kendi kuralı).
        Assert.StartsWith("KIRIK|", Kos("a", Olculdu, "b", Asan));

        Directory.Delete(gecici, true);
    }

    // ═════ ARIZA 4 · beyaz liste sayısı GERÇEK çağrı sayısına kilitli ═══════
    // Doktrin "4" diyordu, betik 5 sayıyordu. Karar koda bakılarak verildi: 5.
    // Tehlike, sayının bir sonraki ihlalde "yeşile boyamak" için yükseltilmesi;
    // bu test tavanı ÖLÇÜLEN gerçeğe eşitler — yeni çağrı eklendiğinde kırılır.
    [Fact]
    public void ExecuteUpdate_beyaz_listesi_gercek_cagri_sayisina_KILITLI()
    {
        var api = Path.Combine(DotnetKoku(), "Kuyumcu.Api");
        var gercek = Directory.EnumerateFiles(api, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(File.ReadAllLines)
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .Count(l => Regex.IsMatch(l, @"ExecuteUpdateAsync\(|ExecuteDeleteAsync\("));

        var m = Regex.Match(Denetim(), @"denetle ""ExecuteUpdate/Delete \(beyaz liste (\d+)\)"" ""[^""]+"" (\d+)");
        Assert.True(m.Success, "denetim.sh'ta ExecuteUpdate/Delete beyaz liste sayacı YOK — kapı sökülmüş.");
        Assert.Equal(m.Groups[1].Value, m.Groups[2].Value);          // başlık ile tavan aynı sayı
        Assert.Equal(gercek, int.Parse(m.Groups[2].Value));          // tavan = ölçülen gerçek

        // Doktrin belgeleri artık aynı sayıyı söylüyor (çelişki 2026-09-23'te kapandı).
        Assert.DoesNotContain("beyaz listede** (4 kullanım)",
            Oku("dokumanlar", "01-gereksinimler", "SRS.md"), StringComparison.Ordinal);
        Assert.DoesNotContain("(izin verilen: 4)",
            Oku("dokumanlar", "02-mimari", "adr", "0004-denetim-izi-append-only.md"), StringComparison.Ordinal);
    }

    // ═════ ARIZA 3 · betikte zsh'e özgü sözdizimi KALMAZ ════════════════════
    // 22-23.09 arasında bir "${=VAR}" düzeltmesi bash'te "bad substitution"
    // verip ÜÇ KONTROLÜ SESSİZCE ATLATIRKEN betik yine "DENETİM TEMİZ" diyordu.
    // Sessiz atlama = fail-open. Kod satırlarında zsh sözdizimi bir daha olmaz.
    [Fact]
    public void denetim_sh_kod_satirlarinda_ZSH_sozdizimi_TASIMAZ()
    {
        var kodSatirlari = Denetim().Split('\n')
            .Select((s, i) => (Satir: s, No: i + 1))
            .Where(x => !x.Satir.TrimStart().StartsWith('#'))
            .ToList();

        // ${=VAR} zsh'e özgüdür; bash'te "bad substitution" verir ve kontrolü ÖLDÜRÜR.
        var zshler = kodSatirlari
            .Where(x => Regex.IsMatch(SatirinKodKismi(x.Satir),
                        @"\$\{=|\$\{~|setopt |unsetopt |typeset -A|\$\{[A-Za-z_]+:[lu]\}"))
            .Select(x => $"{x.No}: {x.Satir.Trim()}")
            .ToList();
        Assert.True(zshler.Count == 0,
            "denetim.sh kod satırlarında zsh'e özgü sözdizimi var (bash'te sessizce atlanır):\n"
            + string.Join('\n', zshler));
    }

    /// <summary>Satırın satır-sonu yorumundan önceki kısmı (yorumdaki uyarılar ihlal değildir).</summary>
    private static string SatirinKodKismi(string satir)
    {
        var i = satir.IndexOf(" #", StringComparison.Ordinal);
        return i < 0 ? satir : satir[..i];
    }
}
