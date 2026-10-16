using System.Diagnostics;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-D3 — TEMİZ DEMO KİRACISI: SENARYONUN İSTEDİĞİ VERİ
//
// Kaynak: dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md § 3
// (V1–V17). Demo verisi betikler/demo-tohumla.js'teki SENARYO nesnesidir; betik
// onu tohumlamadan ÖNCE senaryoPlaniniDogrula() ile, tohumladıktan SONRA
// sunucudan geri okuyup senaryoOlcumunuDegerlendir() ile sınar.
//
// NEDEN SÜREÇ ÇAĞIRIYORUZ: betik Node'dur, `dotnet test` onu derlemez. Kaynağı
// regex'le okumak davranışı ölçmez; bu yüzden testler GERÇEK node sürecinde
// saf fonksiyonları (ağ çağrısı yok) çağırır. node bulunamazsa test KIRMIZI
// olur — atlanmaz (Anayasa md.1: ölçülemeyen = geçmiş sayılmaz).
//
// ÖLÇÜLMÜŞ ARIZA (P-D3 prova 3, 2026-09-23): 12 ürünün stoğu bugüne tarihli
// ~1,18 milyon TL'lik ALIŞ olarak yazılınca Panel'in "Net kasa" kartı
// (satış − alış − hurda) KIRMIZI ve −1.078.595 TL çizecekti. Demo artık stoğu
// ürün kartının AÇILIŞ STOĞU ucundan açar; PANEL_NET maddesi bunu ölçer.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PD3DemoVeriTestleri
{
    private static string DepoKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return Directory.GetParent(d!.FullName)!.FullName;
    }

    private static string BetikYolu() => Path.Combine(
        DepoKoku(), "dokumanlar", "09-uygulama-kayitlari", "betikler", "demo-tohumla.js");

    private static (int Kod, string Cikti, string Hata) Node(params string[] argumanlar)
    {
        var psi = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // PATH'te node yoksa bilinen Homebrew yolu denenir; o da yoksa Start
        // istisna fırlatır ve test KIRMIZI olur (sessiz atlama YOK).
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node"))
            psi.FileName = "/opt/homebrew/bin/node";
        foreach (var a in argumanlar) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var cikti = p.StandardOutput.ReadToEnd();
        var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, cikti, hata);
    }

    /// <summary>Planı mutasyonla bozup senaryoPlaniniDogrula()'nın eksik listesini döner.</summary>
    private static List<string> PlanEksikleri(string mutasyonJs)
    {
        var js = "const m=require(process.argv[1]);const p=structuredClone(m.SENARYO);"
                 + mutasyonJs + ";process.stdout.write(JSON.stringify(m.senaryoPlaniniDogrula(p)));";
        var (kod, cikti, hata) = Node("-e", js, BetikYolu());
        Assert.True(kod == 0, $"node çıkış {kod}: {hata}");
        return JsonSerializer.Deserialize<List<string>>(cikti)!;
    }

    // Tohum sonrası SAĞLAM ölçüm (P-D3 prova 4'te sunucudan okunan değerlerle aynı biçim).
    private const string SaglamOlcum = """
        const o = {
          kasaBakiyeleri: [217353.27], cariSayisi: 6, musteriSayisi: 5, urunSayisi: 12,
          stoklar: { a:3,b:3,c:3,d:3,e:3,f:3,g:3,h:3,i:3,j:3,k:3,l:3 },
          satisSayisi: 9, satisTutarlari: [11233.21,22466.41,7007.61,3200,15167.98,11233.21,11888.42,7007.61,11233.21],
          nakitSatisTutarlari: [11233.21,7007.61,11233.21,3200,7007.61], acikVeresiye: 2,
          emanetBakiye: 9.16, emanetBosBakiye: 0, kurId: '01M37G7B99Z0QEETNKXYFJXDKZ',
          sema: 'platin', yaziOlcegi: '18', vitrinAktif: 8, yasakKelimeli: [], demoOnekliKod: [],
          tevsikEsigi: 30000, ucuzFiyat: 11233.21, pahaliFiyat: 97568.4, panelNetTl: 96597.66 };
        """;

    private static Dictionary<string, bool> Degerlendir(string mutasyonJs)
    {
        var js = "const m=require(process.argv[1]);" + SaglamOlcum + mutasyonJs
                 + ";const s=m.senaryoOlcumunuDegerlendir(o);const r={};for(const x of s)r[x.v]=x.tamam;"
                 + "process.stdout.write(JSON.stringify(r));";
        var (kod, cikti, hata) = Node("-e", js, BetikYolu());
        Assert.True(kod == 0, $"node çıkış {kod}: {hata}");
        return JsonSerializer.Deserialize<Dictionary<string, bool>>(cikti)!;
    }

    // ── 1) Gerçek plan § 3'ü karşılıyor ─────────────────────────────────────

    [Fact]
    public void Plan_dogrula_bayragi_gercek_demo_planini_gecirir()
    {
        var (kod, cikti, hata) = Node(BetikYolu(), "--plan-dogrula");
        Assert.True(kod == 0, $"--plan-dogrula çıkış {kod}: {cikti} {hata}");
        using var j = JsonDocument.Parse(cikti);
        Assert.Equal(0, j.RootElement.GetProperty("eksik").GetArrayLength());
    }

    [Fact]
    public void Gercek_plan_sayilari_senaryo_listesindeki_asgarilerin_ustundedir()
    {
        var js = "const {SENARYO:p}=require(process.argv[1]);process.stdout.write(JSON.stringify({"
               + "cari:p.cariler.length,musteri:p.cariler.filter(c=>c.tur==='MUSTERI').length,"
               + "urun:p.urunler.length,satis:p.satislar.length,veresiye:p.satislar.filter(s=>s.odeme==='VERESIYE').length,"
               + "kasa:p.kasaAcilisTl,vitrin:p.vitrinKalemleri.length}));";
        var (kod, cikti, hata) = Node("-e", js, BetikYolu());
        Assert.True(kod == 0, hata);
        using var j = JsonDocument.Parse(cikti);
        var r = j.RootElement;
        Assert.True(r.GetProperty("cari").GetInt32() >= 6);
        Assert.True(r.GetProperty("musteri").GetInt32() >= 5);
        Assert.True(r.GetProperty("urun").GetInt32() >= 12);
        Assert.True(r.GetProperty("satis").GetInt32() >= 8);
        Assert.True(r.GetProperty("veresiye").GetInt32() >= 2);
        Assert.True(r.GetProperty("kasa").GetDecimal() >= 50000m);
        Assert.InRange(r.GetProperty("vitrin").GetInt32(), 7, 10);
    }

    // ── 2) Plan doğrulayıcısı her V ihlalini REDDEDER ──────────────────────

    [Theory]
    [InlineData("p.urunler.pop()", "V5")]
    [InlineData("p.urunler[0].kod='DEMO-CYR-22'", "V7")]
    [InlineData("p.urunler[1].kod=p.urunler[0].kod", "V7")]
    [InlineData("p.cariler[0].ad='QA-TEST G2T8 Musteri DUZENLENDI'", "V16")]
    [InlineData("p.urunler[4].acilisAdet=2", "V8")]
    [InlineData("p.kasaAcilisTl=40000", "V3")]
    [InlineData("p.kasaAsgariTl=10000", "V3")]
    [InlineData("p.satislar=p.satislar.slice(0,7)", "V9")]
    [InlineData("p.satislar=p.satislar.filter(s=>s.odeme!=='VERESIYE').concat([p.satislar.find(s=>s.odeme==='VERESIYE')])", "V10")]
    [InlineData("p.tahsilat.oran=1", "V10")]
    [InlineData("p.emanet.cari='EMANET_BOS'", "V11")]
    [InlineData("p.cariler=p.cariler.filter(c=>c.rol!=='EMANET_BOS')", "V12")]
    [InlineData("p.satislar.push({urun:'GRM',cari:'EMANET_BOS',odeme:'NAKIT'})", "V12")]
    [InlineData("p.vitrinKalemleri=p.vitrinKalemleri.concat(['RESAT','HAMIT','BESLI'])", "V14")]
    [InlineData("p.vitrinKalemleri=p.vitrinKalemleri.slice(0,6)", "V14")]
    [InlineData("p.cariler=p.cariler.filter(c=>c.tur!=='TEDARIKCI')", "V4")]
    [InlineData("p.urunler=p.urunler.filter(u=>u.grup!=='gumus')", "V6")]
    [InlineData("p.sema=''", "V15")]
    [InlineData("p.alislar=[{urun:'GBL',adet:2,cari:'M1'}]", "alış")]
    public void Plan_dogrulayici_V_ihlalini_reddeder(string mutasyon, string beklenenOnek)
    {
        var eksik = PlanEksikleri(mutasyon);
        Assert.Contains(eksik, e => e.StartsWith(beklenenOnek, StringComparison.Ordinal));
    }

    // ── 3) Tohum sonrası ölçüm değerlendiricisi FAIL-CLOSED ─────────────────

    [Fact]
    public void Degerlendirici_saglam_olcumde_her_maddeyi_gecirir()
    {
        var r = Degerlendir("");
        Assert.True(r.Count >= 16, $"madde sayısı {r.Count}");
        Assert.All(r, kv => Assert.True(kv.Value, $"{kv.Key} sağlam ölçümde KALDI"));
    }

    [Theory]
    [InlineData("o.kasaBakiyeleri=[-786498.80]", "V3")]              // QA fikstüründeki eksi kasa
    [InlineData("o.kasaBakiyeleri=[217353.27,-5]", "V3")]            // ikinci kasa eksi
    [InlineData("o.kasaBakiyeleri=[40000]", "V3")]                   // pozitif ama asgari altı
    [InlineData("o.kasaBakiyeleri=[]", "V3")]                        // kasa hiç okunamadı
    [InlineData("o.cariSayisi=5", "V4")]
    [InlineData("o.urunSayisi=11", "V5")]
    [InlineData("o.stoklar.a=2", "V8")]
    [InlineData("o.stoklar.a=null", "V8")]                           // bilinmeyen stok = TUTMAZ
    [InlineData("o.satisTutarlari[0]=45000", "V9")]
    [InlineData("o.nakitSatisTutarlari=[31000]", "V9")]
    [InlineData("o.satisSayisi=7", "V9")]
    [InlineData("o.acikVeresiye=1", "V10")]
    [InlineData("o.emanetBakiye=0", "V11")]
    [InlineData("o.emanetBakiye=null", "V11")]
    [InlineData("o.emanetBosBakiye=0.5", "V12")]
    [InlineData("o.emanetBosBakiye=null", "V12")]
    [InlineData("o.kurId=null", "V13")]
    [InlineData("o.vitrinAktif=22", "V14")]
    [InlineData("o.sema='zumrut'", "V15")]
    [InlineData("o.yasakKelimeli=['QA-TEST G1 T8 Kasa DUZ']", "V16")]
    [InlineData("o.yasakKelimeli=undefined", "V16")]
    [InlineData("o.demoOnekliKod=['DEMO-CYR-22']", "V7")]
    [InlineData("o.panelNetTl=-1078595.36", "PANEL_NET")]            // ölçülmüş arıza
    [InlineData("o.panelNetTl=null", "PANEL_NET")]
    [InlineData("o.pahaliFiyat=29000", "ADIM4")]                     // tevsik kapısı tetiklenmez
    [InlineData("o.ucuzFiyat=31000", "ADIM3")]
    [InlineData("o.tevsikEsigi=null", "ADIM4")]                      // eşik okunamadı = TUTMAZ
    public void Degerlendirici_bozuk_olcumu_reddeder(string mutasyon, string madde)
    {
        var r = Degerlendir(mutasyon);
        Assert.True(r.ContainsKey(madde), $"{madde} maddesi değerlendirilmedi");
        Assert.False(r[madde], $"{madde} bozuk ölçümü ({mutasyon}) GEÇİRDİ — fail-open");
    }

    // ── 4) Betik akışı: plan yazımdan ÖNCE sınanır, stok açılıştan gelir ────

    [Fact]
    public void Icerik_doldurma_plani_yazimdan_once_sinar_ve_olcumu_fail_closed_uygular()
    {
        var betik = File.ReadAllText(BetikYolu());
        var bas = betik.IndexOf("async function icerikDoldur(", StringComparison.Ordinal);
        Assert.True(bas > 0, "icerikDoldur bulunamadı");
        var govde = betik[bas..];
        var plan = govde.IndexOf("senaryoPlaniniDogrula(SENARYO)", StringComparison.Ordinal);
        var ilkYazma = govde.IndexOf("istek('POST'", StringComparison.Ordinal);
        Assert.True(plan > 0 && plan < ilkYazma, "plan doğrulaması ilk yazmadan ÖNCE değil");
        Assert.Contains("senaryoOlcumunuDegerlendir(olcum", govde);
        Assert.Contains("SENARYO KARŞILANMADI", govde);
        // Eksik madde varsa betik FIRLATIR — "TOHUMLANDI" yazıp çıkmaz.
        Assert.Contains("if (eksik.length) {", govde);
        Assert.Contains("throw hata;", govde);
        // Stok AÇILIŞ STOĞU ucundan gelir; her ürün için alış YAZILMAZ (PANEL_NET arızası).
        Assert.Contains("acilisStogu: { adet: acilisAdet }", govde);
        Assert.Contains("for (const a of SENARYO.alislar)", govde);
        Assert.DoesNotContain("for (const u of SENARYO.urunler) {\n      const t = teklif[u.k];", govde);
    }

    /// <summary>Sayacın sayacı: denetim.sh'taki DEMO_SENARYO_VERI kapısı sökülürse kırmızı.</summary>
    [Fact]
    public void Denetim_betigi_demo_senaryo_verisi_kapisini_tasir()
    {
        var denetim = File.ReadAllText(Path.Combine(DepoKoku(), "dotnet", "denetim.sh"));
        Assert.Contains("P-02: DEMO_SENARYO_VERI (P-D3)", denetim);
        // Yorumdaki "--plan-dogrula" geçişi SAYILMAZ: node'un GERÇEKTEN çağrıldığı satır aranır
        // (M-D3-12 bu açığı gösterdi — yalnız yorum kalınca test yeşil kalıyordu).
        Assert.Contains("node \"$P02_TOHUM\" --plan-dogrula >/dev/null 2>&1 ||", denetim);
        Assert.Contains("node-YOK(OLCULEMEDI)", denetim);
        foreach (var k in new[] { "senaryoPlaniniDogrula(SENARYO)", "senaryoOlcumunuDegerlendir(olcum", "PANEL_NET" })
            Assert.Contains(k, denetim);
    }
}
