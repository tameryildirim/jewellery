using System.Reflection;
using System.Text.RegularExpressions;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DÜZELTME POLİTİKASI HARİTASI — paket UX-1 Faz A [saf, DB'siz]
//
// Bu dosya UX-1'in ÇEKİRDEK KAPISIDIR: "her veri girilen yerin bir düzeltme
// yolu vardır" sözü ancak haritanın TAM ve DOĞRU olmasıyla tutulur.
//
// ⚠ AŞKIN K5: "HedefUc gerçekten var olan bir uca işaret eder" testi YANSIMAYLA
//   kurulur — haritanın kendi listesiyle karşılaştıran test kendi kendini
//   doğrular ve hiçbir şeyi yakalamaz. Burada uç kümesi Kuyumcu.Api
//   assembly'sindeki ControllerBase türevlerinden ([Route] + [HttpX]) toplanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DuzeltmePolitikasiTestleri
{
    // ── Yansımayla gerçek uç envanteri ───────────────────────────────────────

    private static readonly Assembly ApiAssembly = typeof(MetaController).Assembly;

    private static HashSet<string> Uclar(params Type[] fiiller)
    {
        var küme = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tip in ApiAssembly.GetTypes()
                     .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var kok = tip.GetCustomAttributes<RouteAttribute>()
                .Select(r => r.Template).FirstOrDefault();
            if (kok is null) continue;

            foreach (var m in tip.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var oz in m.GetCustomAttributes(inherit: false).OfType<Attribute>())
                {
                    if (!fiiller.Contains(oz.GetType())) continue;
                    var sablon = (oz as IRouteTemplateProvider)?.Template;
                    küme.Add(string.IsNullOrEmpty(sablon) ? kok : $"{kok}/{sablon}");
                }
            }
        }
        return küme;
    }

    private static HashSet<string> YazmaUclari() => Uclar(
        typeof(HttpPatchAttribute), typeof(HttpPutAttribute), typeof(HttpDeleteAttribute));

    private static HashSet<string> TumYazmaUclari() => Uclar(
        typeof(HttpPatchAttribute), typeof(HttpPutAttribute),
        typeof(HttpDeleteAttribute), typeof(HttpPostAttribute));

    [Fact]
    public void Yansima_uc_envanteri_BOS_DEGILDIR()
    {
        // Yansıma kırılırsa diğer testler sessizce "hep yeşil" olurdu.
        var yazma = YazmaUclari();
        Assert.Contains("api/v1/subeler/{id}", yazma);
        Assert.Contains("api/v1/kasalar/{id}", yazma);
        Assert.True(yazma.Count >= 15, $"yazma ucu sayısı beklenenden az: {yazma.Count}");
        Assert.Contains("api/v1/islemler/satis-iade", TumYazmaUclari());
    }

    // ── 1. Envanter tam mı? ──────────────────────────────────────────────────

    /// <summary>KuyumcuDbContext'teki HER DbSet varlığı haritada damgalıdır.</summary>
    [Fact]
    public void Her_varlik_haritada_VARDIR()
    {
        var varliklar = typeof(KuyumcuDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(varliklar);
        var eksik = varliklar.Where(v => !DuzeltmePolitikasi.Varliklar.ContainsKey(v)).ToList();
        Assert.True(eksik.Count == 0,
            "Politika haritasında damgası olmayan varlık(lar): " + string.Join(", ", eksik));
    }

    [Fact]
    public void Haritada_UYDURMA_varlik_YOKTUR()
    {
        var gercek = typeof(KuyumcuDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet(StringComparer.Ordinal);

        var fazla = DuzeltmePolitikasi.Varliklar.Keys.Where(v => !gercek.Contains(v)).ToList();
        Assert.True(fazla.Count == 0,
            "Haritada karşılığı olmayan varlık adı: " + string.Join(", ", fazla));
    }

    [Fact]
    public void Politika_degerleri_UC_TANEDIR()
    {
        foreach (var s in DuzeltmePolitikasi.Varliklar.Values
                     .Concat(DuzeltmePolitikasi.HareketTurleri.Values))
        {
            Assert.Contains(s.Politika, Politikalar.Tumu);
            Assert.Contains(s.GerekceAnahtari, GerekceAnahtarlari.Tumu);
        }
    }

    // ── 2. Defter değişmezliği (K-UX-2 / VUK md.219) ─────────────────────────

    [Fact]
    public void Defter_turleri_DUZENLENEBILIR_DEGILDIR()
    {
        foreach (var tur in Sabitler.HareketTurleri)
        {
            Assert.True(DuzeltmePolitikasi.HareketTurleri.TryGetValue(tur, out var s),
                $"Hareket türü haritada yok: {tur}");
            Assert.True(s!.Politika is Politikalar.Storno or Politikalar.SaltOkunur,
                $"{tur} deftere yazar ama '{s.Politika}' damgalı — VUK md.219 ihlali.");
        }
    }

    [Fact]
    public void Defter_varliklari_DUZENLENEBILIR_DEGILDIR()
    {
        string[] defter = ["Hareket", "KasaHareket", "BankaHareket", "DenetimKaydi", "GunKapanisi"];
        foreach (var v in defter)
            Assert.NotEqual(Politikalar.Duzenlenebilir, DuzeltmePolitikasi.Varliklar[v].Politika);
    }

    [Fact]
    public void Denetim_izi_salt_okunurdur()
        => Assert.Equal(Politikalar.SaltOkunur,
            DuzeltmePolitikasi.Varliklar["DenetimKaydi"].Politika);

    [Theory]
    [InlineData("MasakKimlikTespiti")]
    [InlineData("MasakBeyani")]
    [InlineData("MasakSupheliBildirim")]
    [InlineData("DisaAktarimTalebi")]
    public void Masak_ve_disa_aktarim_salt_okunurdur(string varlik)
        => Assert.Equal(Politikalar.SaltOkunur, DuzeltmePolitikasi.Varliklar[varlik].Politika);

    [Fact]
    public void Salt_okunur_varliga_HEDEF_UC_verilmez()
    {
        foreach (var s in DuzeltmePolitikasi.Varliklar.Values
                     .Where(x => x.Politika == Politikalar.SaltOkunur))
            Assert.Null(s.HedefUc);
    }

    // ── 3. HedefUc gerçek uca işaret eder (Aşkın K5) ─────────────────────────

    [Fact]
    public void Duzenlenebilir_varligin_HedefUc_u_GERCEK_bir_yazma_ucudur()
    {
        var uclar = YazmaUclari();
        foreach (var s in DuzeltmePolitikasi.Varliklar.Values
                     .Where(x => x.Politika == Politikalar.Duzenlenebilir))
        {
            Assert.False(string.IsNullOrWhiteSpace(s.HedefUc),
                $"{s.Varlik} DUZENLENEBILIR ama HedefUc boş.");
            Assert.True(uclar.Contains(s.HedefUc!),
                $"{s.Varlik} → '{s.HedefUc}' diye bir PATCH/PUT/DELETE ucu YOK.");
        }
    }

    [Fact]
    public void Storno_HedefUc_lari_GERCEK_uca_isaret_eder()
    {
        var uclar = TumYazmaUclari();
        foreach (var s in DuzeltmePolitikasi.Varliklar.Values
                     .Concat(DuzeltmePolitikasi.HareketTurleri.Values)
                     .Where(x => x.Politika == Politikalar.Storno && x.HedefUc is not null))
            Assert.True(uclar.Contains(s.HedefUc!),
                $"{s.Varlik} → '{s.HedefUc}' diye bir uç YOK.");

        foreach (var s in DuzeltmePolitikasi.Varliklar.Values.Where(x => x.AlternatifYol is not null))
            Assert.True(uclar.Contains(s.AlternatifYol!),
                $"{s.Varlik} alternatif yolu '{s.AlternatifYol}' diye bir uç YOK.");
    }

    [Fact]
    public void Defter_tablolarina_PATCH_veya_DELETE_ucu_ACILMAMISTIR()
    {
        // Mehmet K9: hareketler / kasa_hareketleri / banka_hareketleri /
        // denetim_kayitlari / gun_kapanislari üzerinde yazma ucu OLMAZ.
        string[] yasak =
        [
            "api/v1/hareketler", "api/v1/kasa-hareketleri", "api/v1/banka-hareketleri",
            "api/v1/denetim", "api/v1/gun-kapanislari",
        ];
        foreach (var uc in YazmaUclari())
            Assert.DoesNotContain(yasak, y => uc.StartsWith(y + "/", StringComparison.Ordinal)
                                              || uc == y);
    }

    /// <summary>
    /// MÜHÜR 2 / Mehmet (UX-1 Faz B) — FİNANSAL VARLIKTA HARD DELETE YASAK.
    /// <para>
    /// Deftere yazan ve belge doğuran modüllerde hiçbir [HttpDelete] ucu
    /// açılmaz; düzeltme yolu yalnız PASİFLEŞTİRME ya da STORNO'dur
    /// (VUK md.219 · ADR-0004). Kasa/banka/reyon DELETE'i satırı SİLMEZ,
    /// aktif=0 yazar — bu yüzden listede değildir. Aynı kural dotnet/denetim.sh
    /// içindeki "Finansal varlıkta [HttpDelete]" sayacıyla çiftlenmiştir.
    /// </para>
    /// </summary>
    [Fact]
    public void Finansal_modullerde_HARD_DELETE_ucu_ACILMAMISTIR()
    {
        string[] finansalRotalar =
        [
            "api/v1/faturalar", "api/v1/islemler", "api/v1/emanet", "api/v1/hurda",
            "api/v1/masak", "api/v1/sayim", "api/v1/tevsik", "api/v1/raporlar",
        ];

        var silmeUclari = Uclar(typeof(HttpDeleteAttribute));
        foreach (var uc in silmeUclari)
        {
            Assert.DoesNotContain(finansalRotalar,
                r => uc.Equals(r, StringComparison.Ordinal)
                     || uc.StartsWith(r + "/", StringComparison.Ordinal));
            Assert.DoesNotContain("hareket", uc, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("kapanis", uc, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── 4. Gerekçe anahtarı → TR+EN metin ────────────────────────────────────

    [Fact]
    public void Gerekce_anahtarlarinin_TR_ve_EN_karsiligi_VARDIR()
    {
        foreach (var anahtar in GerekceAnahtarlari.Tumu)
        {
            Assert.False(string.IsNullOrWhiteSpace(Sozlukler.Tr.Duzeltme.GerekceMetni(anahtar)),
                $"TR sözlükte '{anahtar}' karşılığı yok.");
            Assert.False(string.IsNullOrWhiteSpace(Sozlukler.En.Duzeltme.GerekceMetni(anahtar)),
                $"EN sözlükte '{anahtar}' karşılığı yok.");
            Assert.NotEqual(Sozlukler.Tr.Duzeltme.GerekceMetni(anahtar),
                            Sozlukler.En.Duzeltme.GerekceMetni(anahtar));
        }
    }

    [Fact]
    public void Haritadaki_her_gerekce_anahtari_METNE_cozulur()
    {
        foreach (var s in DuzeltmePolitikasi.Varliklar.Values
                     .Concat(DuzeltmePolitikasi.HareketTurleri.Values))
            Assert.False(string.IsNullOrWhiteSpace(
                Sozlukler.Tr.Duzeltme.GerekceMetni(s.GerekceAnahtari)),
                $"{s.Varlik} anahtarı '{s.GerekceAnahtari}' metne çözülmüyor.");
    }

    // ── 5. Mehmet K8 — istemci HedefUc'u DİNAMİK ÇAĞIRMAZ ────────────────────

    [Fact]
    public void Ekran_HedefUc_u_DINAMIK_CAGIRMAZ()
    {
        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web");
        var dosyalar = Directory.EnumerateFiles(kok, "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".razor", StringComparison.Ordinal)
                         || f.EndsWith(".cs", StringComparison.Ordinal))
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        Assert.NotEmpty(dosyalar);
        var desen = new Regex(
            @"\.(Get|Post|Patch|Put|Delete)(Async|BaytAsync)\s*(<[^>(]*>)?\s*\(\s*[A-Za-z0-9_.\[\]]*HedefUc",
            RegexOptions.IgnoreCase);

        foreach (var d in dosyalar)
        {
            var kod = CssKaynak.YorumsuzKod(File.ReadAllText(d));
            Assert.False(desen.IsMatch(kod),
                $"{Path.GetFileName(d)}: sunucudan gelen HedefUc ile dinamik istek kuruluyor (Mehmet K8).");
        }
    }

    // ── PAKET T-2 — AÇILIŞ DEVRİ (ACILIS) ────────────────────────────────────
    //
    // ⚠ Bertan'ın birleşik koşulu: "Sabitler.HareketTurleri'ne ACILIS eklendi"
    //   BİR KAPI DEĞİLDİR (dizi kodda zorlanmaz). GERÇEK kapı bu üç testtir +
    //   denetim.sh'in "Kurulum/ altında dogrudan Add = 0" sayacı.

    [Fact]
    public void ACILIS_turu_DuzeltmePolitikasi_haritasinda_KAYITLI()
    {
        Assert.True(DuzeltmePolitikasi.HareketTurleri.TryGetValue("ACILIS", out var s),
            "ACILIS türü düzeltme politikası haritasında YOK.");
        Assert.Equal(GerekceAnahtarlari.Defter, s!.GerekceAnahtari);
        // Yol dizgesi GERÇEK bir uca işaret eder (Aşkın K5 · aynı dosyadaki
        // Storno_HedefUc_lari_GERCEK_uca_isaret_eder testi de bunu tarar).
        Assert.Equal("api/v1/kurulum/acilis/geri-al", s.HedefUc);
        Assert.Equal("api/v1/cari/{id}/duzeltme", s.AlternatifYol);
    }

    [Fact]
    public void ACILIS_DUZENLENEBILIR_DEGILDIR()
        => Assert.NotEqual(Politikalar.Duzenlenebilir,
            DuzeltmePolitikasi.HareketTurleri["ACILIS"].Politika);

    // ── PAKET F-2-min — KİRACI DAVETİ VE KİRACI DURUMU ───────────────────────

    [Fact]
    public void KiraciDaveti_DuzeltmePolitikasi_Haritasinda_Kayitli()
    {
        Assert.True(DuzeltmePolitikasi.Varliklar.TryGetValue("KiraciDaveti", out var s),
            "KiraciDaveti düzeltme politikası haritasında YOK.");
        Assert.Equal(Politikalar.Duzenlenebilir, s!.Politika);
        // Uç GERÇEKTEN vardır — Duzenlenebilir_varligin_HedefUc_u... testi de tarar.
        Assert.Equal("api/v1/sistem/davetler/{id}", s.HedefUc);
    }

    /// <summary>
    /// tenants.durum SAHİP tarafından değiştirilemez (abonelik kendi kendine
    /// uzatılamaz); düzeltme yolu sistem anahtarıyla korunan uçtur. Tenant'ın
    /// kendisi ise DUZENLENEBILIR kalır (kimlik alanları).
    /// </summary>
    [Fact]
    public void Tenant_Durum_Alani_Politika_Haritasinda()
    {
        var tenant = DuzeltmePolitikasi.Varliklar["Tenant"];
        Assert.Equal(Politikalar.Duzenlenebilir, tenant.Politika);
        Assert.Equal("api/v1/admin/tenant/kimlik", tenant.HedefUc);

        // durum/deneme_bitis'in düzeltme yolu AYRI bir uçtur ve o uç GERÇEKTİR.
        Assert.Contains("api/v1/sistem/kiracilar/{id}", TumYazmaUclari());
    }

    /// <summary>
    /// kiraci_davetleri KASTEN IKiraciVarlik DEĞİLDİR (kiracı-öncesi tablo).
    /// Bu bir kabul edilen risktir: global filtre ve KiraciYazmaDenetcisi ona
    /// işlemez, tek koruma katmanı SistemAnahtariKapisi'dır. Gerekçenin kodda
    /// YAZILI kalmasını bu test zorlar — silinirse kırmızıya döner.
    /// </summary>
    [Fact]
    public void KiraciDavetleri_IKiraciVarlik_Degil_Gerekce_Belgelendi()
    {
        Assert.False(typeof(IKiraciVarlik).IsAssignableFrom(typeof(KiraciDaveti)));

        var kok = Path.Combine(CssKaynak.Kok(), "Kuyumcu.Api");
        var entities = File.ReadAllText(Path.Combine(kok, "Data", "Entities.cs"));
        var i = entities.IndexOf("class KiraciDaveti", StringComparison.Ordinal);
        Assert.True(i > 0, "Entities.cs içinde KiraciDaveti sınıfı bulunamadı.");
        // Sınıfın HEMEN ÜSTÜNDEKİ gerekçe bloğu (xml yorum) taranır.
        var davetBolumu = entities[Math.Max(0, i - 2000)..i];
        Assert.Contains("IKiraciVarlik DEĞİLDİR", davetBolumu, StringComparison.Ordinal);
        Assert.Contains("SistemAnahtariKapisi", davetBolumu, StringComparison.Ordinal);

        // DbContext kapsama denetiminde AÇIK beyaz liste maddesi vardır.
        var ctx = File.ReadAllText(Path.Combine(kok, "Data", "KuyumcuDbContext.cs"));
        Assert.Contains("typeof(KiraciDaveti)", ctx, StringComparison.Ordinal);
    }

    // ── MÜHÜR 4 / K1 — 'SISTEM' ROLÜ AÇILMADI ───────────────────────────────

    /// <summary>
    /// Sistem yöneticisi kimliği YAPILANDIRMA SIRRIDIR. Rol hiyerarşisine
    /// dokunulsaydı tüm [RolGerekli] listeleri etkilenirdi.
    /// </summary>
    [Fact]
    public void SISTEM_rolu_ACILMADI_rol_hiyerarsisi_DEGISMEDI()
    {
        Assert.Equal(0, Kuyumcu.Api.Auth.AktifKullanici.Seviye("SISTEM"));
        Assert.Equal(4, Kuyumcu.Api.Auth.AktifKullanici.Seviye("SAHIP"));
        Assert.Equal(3, Kuyumcu.Api.Auth.AktifKullanici.Seviye("MUDUR"));
        Assert.Equal(2, Kuyumcu.Api.Auth.AktifKullanici.Seviye("MUHASEBE"));
        Assert.Equal(1, Kuyumcu.Api.Auth.AktifKullanici.Seviye("TEZGAHTAR"));
    }

    /// <summary>
    /// Bertan koşulu: BasitMenu bir İZİN LİSTESİDİR; yönetici konsolu oraya
    /// EKLENMEDİ ve profesyonel mod menü kaynağına da eklenmedi.
    /// </summary>
    [Fact]
    public void Sistem_konsolu_MENUYE_eklenmedi()
    {
        Assert.DoesNotContain("/sistem/kiracilar", Kuyumcu.Web.Servisler.BasitMenu.Yollar);
        Assert.False(Kuyumcu.Web.Servisler.BasitMenu.Icerir("/sistem/kiracilar"));

        // TEMA TEMİZLİĞİ 17.10: kenar çubuklu kabuk kaldırıldı; profesyonel menünün kaynağı artık MenuTanimi + YildizKabuk.
        foreach (var dosya in new[] { "MenuTanimi.cs", "YildizKabuk.cs" })
        {
            var menu = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "Layout", dosya));
            Assert.DoesNotContain("/sistem", menu, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(Kuyumcu.Web.Layout.MenuTanimi.Ogeler.Concat(Kuyumcu.Web.Layout.MenuTanimi.AltBlok),
            o => o.Yol.StartsWith("/sistem", StringComparison.Ordinal));
    }

    [Fact]
    public void Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur()
    {
        // ÇİFT YÖNLÜ: eksik olan da FAZLA olan da kırmızıdır. Tek yönlü bir
        // test, haritada kalmış ÖLÜ bir türü (artık yazılmayan) göremezdi.
        var dizi = Sabitler.HareketTurleri.ToHashSet(StringComparer.Ordinal);
        var harita = DuzeltmePolitikasi.HareketTurleri.Keys.ToHashSet(StringComparer.Ordinal);

        var haritadaEksik = dizi.Except(harita).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var dizideEksik = harita.Except(dizi).OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.True(haritadaEksik.Count == 0,
            $"Haritada olmayan hareket türleri: {string.Join(", ", haritadaEksik)}");
        Assert.True(dizideEksik.Count == 0,
            $"Sabitler.HareketTurleri'nde olmayan harita kayıtları: {string.Join(", ", dizideEksik)}");
    }
}
