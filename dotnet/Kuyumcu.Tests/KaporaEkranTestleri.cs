using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAPORA-EKRAN — sipariş detayı kapora kartı: görünüm + dil.
//   · 409 sonrası kart bayat kalmaz (sipariş yeniden okunur, çekmece kapanır)
//   · jargon yok (storno · ters kayıt · sipariş kaydı · Açık hesap) TR + EN
//   · yetkisiz kullanıcıya silik düğme yerine okunur cümle
//   · geçmiş listesi madde işaretsiz, açıklama cümlesi yok
//   · kapora makbuzunda "Has gram" sütunu yok (diğer tahsilat makbuzunda var)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KaporaEkranSozlukTestleri
{
    private static readonly string[] YasakTr = ["storno", "ters kay", "sipariş kaydı", "açık hesap"];
    private static readonly string[] YasakEn = ["reversal", "reversing", "on account", "order record", "storno"];

    private static IEnumerable<string> Metinler(object kayit) =>
        kayit.GetType().GetProperties().Where(p => p.PropertyType == typeof(string))
            .Select(p => (string)p.GetValue(kayit)!);

    [Fact]
    public void KEK01_kapora_metinlerinde_jargon_yok_TR_ve_EN()
    {
        foreach (var m in Metinler(EksikB1Sozlugu.Tr).Concat(Metinler(KaporaGecmisSozlugu.Tr)))
            foreach (var y in YasakTr) Assert.DoesNotContain(y, m, StringComparison.OrdinalIgnoreCase);
        foreach (var m in Metinler(EksikB1Sozlugu.En).Concat(Metinler(KaporaGecmisSozlugu.En)))
            foreach (var y in YasakEn) Assert.DoesNotContain(y, m, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KEK09_geri_ver_onay_etiketi_kisa_tek_satirlik()
    {
        foreach (var m in new[] { KaporaGecmisSozlugu.Tr, KaporaGecmisSozlugu.En })
        {
            Assert.True(m.GeriVerOnay.Length <= 10);
            Assert.DoesNotContain(" ", m.GeriVerOnay.Replace("Geri ver", "").Trim());
        }
    }

    [Fact]
    public void KEK02_sunucu_409_metni_sade_ve_EN_karsiligi_var()
    {
        var tr = Kuyumcu.Api.Modules.Siparis.SiparisServisi.KaporaKilidi("ONAYLANDI",
            new Kuyumcu.Api.Modules.Siparis.SiparisServisi.KaporaBilgisi(100m, DateTime.UtcNow, "ks1", false))!;
        foreach (var y in YasakTr) Assert.DoesNotContain(y, tr, StringComparison.OrdinalIgnoreCase);
        var en = SunucuIletiSozlugu.Cevir(tr, "en");
        Assert.NotEqual(tr, en);
        foreach (var y in YasakEn) Assert.DoesNotContain(y, en, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class KaporaEkranEkranTestleri : BunitContext
{
    private bool kaporaAlindi;          // "başka sekme" kaporayı aldı
    private bool stornolu;
    private string gecmisJson = "[]";
    private bool kaporaPostVar;

    private string DetayJson() =>
        "{\"id\":\"s1\",\"no\":\"S-2026-000001\",\"tur\":\"MUSTERI_SIPARISI\",\"durum\":\"HAZIR\""
        + ",\"cariId\":\"c1\",\"cariAd\":\"Ayşe Yılmaz\",\"subeId\":\"sb1\""
        + ",\"sozVerilenGun\":null,\"aciklama\":\"bilezik\",\"tahminiTutarTl\":null,\"kurSnapshotId\":null,"
        + "\"emanetHareketId\":null,\"geciken\":false,\"duzenlenebilir\":false,\"terminal\":false"
        + ",\"izinliGecisler\":[],\"geriYonluGecisler\":[],\"geriAlmaYetkisi\":true,\"geriAlmaRolleri\":[\"SAHIP\",\"MUDUR\"]"
        + ",\"gerekceIsteyenGecisler\":[\"IPTAL\"],\"gerekceAsgari\":10,\"olusturanKullaniciId\":\"u1\","
        + "\"olusturuldu\":\"2026-09-16T08:00:00Z\",\"guncellendi\":\"2026-09-16T08:00:00Z\","
        + "\"kalemler\":[{\"id\":\"k1\",\"urunId\":null,\"urunAd\":null,\"tanim\":\"yüzük\",\"ayarId\":\"K22\",\"gramAgirlik\":3.5,\"adet\":1.0,\"sira\":0}],"
        + "\"gecisler\":[],\"ustaCariId\":null,\"ustaAd\":null"
        + ",\"kaporaHareketId\":" + (kaporaAlindi ? "\"hk1\"" : "null")
        + ",\"kaporaAlinabilir\":" + (!kaporaAlindi || stornolu ? "true" : "false")
        + ",\"kaporaKilitSebebi\":null"
        + ",\"kaporaTutari\":" + (kaporaAlindi ? "1500.00" : "null")
        + ",\"kaporaTarihi\":null,\"kaporaKasaId\":" + (kaporaAlindi ? "\"ks1\"" : "null")
        + ",\"kaporaStornolu\":" + (stornolu ? "true" : "false")
        + ",\"kaporaGecmisi\":" + gecmisJson + "}";

    private sealed class SahteApi(KaporaEkranEkranTestleri s) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/siparisler/s1/kapora"))
            {
                s.kaporaPostVar = true;
                s.kaporaAlindi = true;   // A sekmesi önce aldı → B'nin isteği 409 alır
                return Task.FromResult(EksikB1Ortak.Yanit(HttpStatusCode.Conflict,
                    """{"message":"Bu siparişte kapora zaten alınmış. Değiştirmek için önce kaporayı geri verin."}"""));
            }
            string? g = yol switch
            {
                var y when istek.Method == HttpMethod.Get && y.EndsWith("/siparisler/s1") => s.DetayJson(),
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u0","kod":"K0","ad":"Ürün 0","ayarId":"K22","gramAgirlik":1.0}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                _ => null,
            };
            return Task.FromResult(g is null
                ? EksikB1Ortak.Yanit(HttpStatusCode.NotFound, """{"message":"sahte API bu ucu tanımıyor"}""")
                : EksikB1Ortak.Yanit(HttpStatusCode.OK, g));
        }
    }

    public KaporaEkranEkranTestleri() => EksikB1Ortak.Kur(this, new SahteApi(this));

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay(string mod, string rol = "SAHIP")
    {
        EksikB1Ortak.Oturum(this, mod, rol);
        var e = Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, "s1"));
        e.WaitForElement("#sipd-kart");
        return e;
    }

    [Fact]
    public void KEK03_409_sonrasi_kart_guncel_kaporayi_gosterir_kapora_al_kalmaz()
    {
        var M = KaporaGecmisSozlugu.Tr;
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            kaporaAlindi = false; stornolu = false; kaporaPostVar = false;
            var e = Detay(mod);
            e.Find("#sipd-kapora-al").Click();
            e.WaitForElement("#sipd-kapora-form");
            e.Find("#sipd-kapora-tutar-g").Input("500");
            e.Find("#sipd-kapora-tutar-g").Blur();
            e.WaitForAssertion(() => Assert.False(e.Find("#sipd-kapora-kaydet").HasAttribute("disabled")));
            e.Find("#sipd-kapora-kaydet").Click();

            e.WaitForAssertion(() =>
            {
                Assert.True(kaporaPostVar);
                Assert.Empty(e.FindAll("#sipd-kapora-form"));                     // çekmece kapandı
                Assert.Empty(e.FindAll("#sipd-kapora-al"));                       // bayat "+ Kapora al" yok
                Assert.Empty(e.FindAll("#sipd-kapora-yok"));                      // "kapora alınmadı" yok
                Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Para(1500m, "TRY"), e.Find("#sipd-kapora-tutar").TextContent);
                Assert.Contains(M.BaskaYerdeDegisti, e.Find("#sipd-hata-bandi").TextContent);
            });
            e.Dispose();
        }
    }

    [Fact]
    public void KEK04_canli_kapora_kartinda_aciklama_cumlesi_ve_jargon_yok_geri_ver_okunur()
    {
        kaporaAlindi = true; stornolu = false;
        foreach (var mod in EksikB1Ortak.Modlar)
        {
            var e = Detay(mod);
            var kart = e.Find("#sipd-kapora").TextContent;
            Assert.Empty(e.FindAll("#sipd-kapora-not"));
            foreach (var y in new[] { "storno", "ters kay", "sipariş kaydı", "Açık hesap" })
                Assert.DoesNotContain(y, kart, StringComparison.OrdinalIgnoreCase);
            var geri = e.Find("#sipd-kapora-geri");
            Assert.False(geri.HasAttribute("disabled"));                          // silik/pasif değil
            geri.Click();
            var modal = e.WaitForElement("#sipd-kapora-geri-aciklama").TextContent;
            foreach (var y in new[] { "storno", "ters kay" })
                Assert.DoesNotContain(y, modal, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(KaporaGecmisSozlugu.Tr.GeriVerOnay, e.Find("#sipd-kapora-geri-onayla").TextContent.Trim());   // 375 px tek satır: kısa etiket
            e.Dispose();
        }
    }

    [Fact]
    public void KEK05_yetkisiz_kullaniciya_silik_dugme_yerine_okunur_cumle()
    {
        kaporaAlindi = true; stornolu = false;
        var e = Detay(Kuyumcu.Web.Servisler.ArayuzModuServisi.Basit, "TEZGAHTAR");
        Assert.Empty(e.FindAll("#sipd-kapora-geri"));
        var kilit = e.Find("#sipd-kapora-geri-kilit");
        Assert.Equal(EksikB1Sozlugu.Tr.GeriVerYetki, kilit.TextContent.Trim());
        Assert.DoesNotContain("metin-soluk", kilit.ClassName ?? "");
    }

    [Fact]
    public void KEK06_gecmis_listesi_madde_isaretsiz_aciklama_cumlesiz()
    {
        kaporaAlindi = true; stornolu = false;
        gecmisJson = """[{"hareketId":"hk2","tarih":"2026-10-09T09:30:00Z","tutar":1500.00,"odemeTuru":"HAVALE","stornolu":false,"stornoTarihi":null,"guncel":true},"""
            + """{"hareketId":"hk0","tarih":"2026-10-09T08:00:00Z","tutar":1000.00,"odemeTuru":"NAKIT","stornolu":true,"stornoTarihi":"2026-10-09T09:00:00Z","guncel":false}]""";
        var e = Detay(Kuyumcu.Web.Servisler.ArayuzModuServisi.Basit);
        e.Find("#sipd-kapora-gecmis-ac").Click();
        Assert.Contains("kapora-gecmis-liste", e.Find("#sipd-kapora-gecmis-liste").ClassName);
        Assert.Empty(e.FindAll("#sipd-kapora-gecmis-aciklama"));
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Contains(".kapora-gecmis-liste { list-style: none;", css);
    }
}

[Collection("http")]
public sealed class KaporaEkranMakbuzTestleri(HttpFikstur f)
{
    private string Yaz(Kiraci k, string? siparisId, decimal has)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAHSILAT",
            Tarih = DateTime.UtcNow, TlTutari = 2_000m, HasGrami = has, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId, SiparisId = siparisId,
            Aciklama = "KAPORA-EKRAN test", Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    private async Task<JsonElement> Belge(Kiraci k, string id)
    {
        var c = await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.TahsilatMakbuzu}/{id}");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return await c.Json();
    }

    [Fact]
    public async Task KEK07_kapora_makbuzunda_has_sutunu_yok()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETKAPORA-EKRAN-TEST-M1");
        var j = await Belge(k, Yaz(k, "sip-kapora-1", 0m));
        var b = j.GetProperty("bolumler")[0];
        Assert.Equal(["Tür", "Açıklama", "Tutar"], b.GetProperty("sutunBasliklari").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Equal(["METIN", "METIN", "PARA"], b.GetProperty("sutunBicimleri").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Equal(3, b.GetProperty("satirlar")[0].GetProperty("hucreler").GetArrayLength());
        Assert.DoesNotContain(j.GetProperty("tutarlar").EnumerateArray(), t => t.GetProperty("etiket").GetString()!.Contains("has", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task KEK08_siparissiz_tahsilat_makbuzu_has_sutununu_korur()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETKAPORA-EKRAN-TEST-M2");
        var j = await Belge(k, Yaz(k, null, 0m));
        var b = j.GetProperty("bolumler")[0];
        Assert.Equal(4, b.GetProperty("sutunBasliklari").GetArrayLength());
        Assert.Equal(["METIN", "METIN", "HAS", "PARA"], b.GetProperty("sutunBicimleri").EnumerateArray().Select(x => x.GetString()!).ToArray());
    }
}
