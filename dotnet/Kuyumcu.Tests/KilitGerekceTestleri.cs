using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-65 — KİLİT GEREKÇESİ YAYGINLAŞTIRMA [bUnit] (tur 8 · UX-1 B48)
//
// Dört ekranda kilitli alanın yanında GEREKÇE ve DÜZELTME YOLU okunur:
//   · /kullanicilar — kendi rol kilidi "Sahipliği Devret" yolunu ADIYLA söyler
//   · /evraklar     — kayıtlı evrakın tür kilidi (DegismezIpucu)
//   · /bildirimler  — kurulumda KAPALI işin açma yolu
//   · /kur          — manuel kur kaydı yalnız gerekçeli iptal edilir
// + /emanet: kilitli "+ Emanet Al" / "İade Et" düğmeleri sebebini yazar.
// Yeni kilit EKLENMEDİ; mevcut davranış aynı — yalnız açıklama.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KilitGerekceTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private static OnayKilitMetinleri O => OnayKilitSozlugu.Tr;

    private const string KullaniciListesi = """
        [{"id":"u1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":false},
         {"id":"u2","ad":"Ayşe","soyad":"Yılmaz","email":"ayse@ornek.com","rol":"TEZGAHTAR","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":true}]
        """;

    private const string EvrakListesi = """
        {"toplamAdet":1,"toplamTutar":30000.00,"limit":200,"azamiLimit":200,"offset":0,
         "dahaFazlasiVar":false,"portfoydeTutar":30000.00,"tahsileVerilenTutar":0.00,
         "buHaftaVadeliTutar":0.00,"karsiliksizTutar":0.00,
         "satirlar":[
           {"id":"e1","evrakTuru":"CEK","yon":"ALINAN","durum":"PORTFOYDE","tutar":30000.00,
            "paraBirimi":"TRY","vadeGunu":"2026-10-10","evrakNo":"CEK-1","bankaAdi":"Ziraat",
            "bankaSube":"Merkez","kesideYeri":"İstanbul","kesideTarihi":"2026-08-01",
            "borcluCariId":null,"borcluAd":null,"kaynakCariId":"c1","kaynakCariAd":"Ayşe Yılmaz",
            "notlar":null,"gecikmeGun":0,"gecikmis":false,"yakindaVadeli":false,"duzenlenebilir":true,
            "izinliGecisler":["TAHSILE_VERILDI"]}]}
        """;

    private const string IsListesi = """
        {"azamiCalisma":50,
         "ayarlar":{"baslangicSaat":0,"vadeUfukGun":7,"masakUyariGun":3,"kapaliIsler":[],
           "saatEnAz":0,"saatEnCok":23,"vadeEnAz":1,"vadeEnCok":365,"masakEnAz":0,"masakEnCok":60,
           "varsayilanVadeUfukGun":7,"varsayilanMasakUyariGun":3},
         "isler":[
           {"kod":"VADE_HATIRLATMA","ad":"Vade hatırlatma","etkin":true,"yapilandirmaEtkin":true,"kiraciKapatti":false,
            "sonCalisma":null,"sonDurum":null},
           {"kod":"TEVSIK_KURTARMA","ad":"Tevsik hatırlatması","etkin":false,"yapilandirmaEtkin":false,"kiraciKapatti":false,
            "sonCalisma":null,"sonDurum":null}],
         "calismalar":[]}
        """;

    public KilitGerekceTestleri()
    {
        P65SahteApi.Kur(this, istekler, (metot, yol) => yol switch
        {
            // Kullanıcılar
            var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","adres":null,"telefon":null,"aktif":true}]""",
            var y when y.EndsWith("/kullanicilar") => KullaniciListesi,
            // Evraklar
            var y when y.Contains("/evraklar/takvim") => """{"bas":"2026-09-18","bit":"2026-10-18","toplamAdet":0,"toplamTutar":0,"gunler":[]}""",
            var y when y.Contains("/evraklar") => EvrakListesi,
            var y when y.Contains("/kasalar") => """[{"id":"k1","ad":"Merkez kasa","paraBirimi":"TRY"}]""",
            var y when y.Contains("/bankalar") => """[{"id":"b1","hesapAdi":"Vadesiz TL","bankaAdi":"Ziraat","paraBirimi":"TRY"}]""",
            // Bildirimler
            var y when y.EndsWith("/isler") => IsListesi,
            var y when y.Contains("/bildirimler") =>
                """{"durum":"ACIK","limit":100,"azamiLimit":100,"kapaliGosterimGun":30,"dahaFazlasiVar":false,"bildirimler":[]}""",
            // Kur
            var y when y.Contains("/kur/kaynaklar") =>
                """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""",
            var y when y.Contains("/kur/gecmis") => """
                [{"id":"k2","altinHasFiyati":6810,"kaynak":"MANUAL","olusturuldu":"2026-09-14T08:00:00Z","ayarFiyatlari":[]}]
                """,
            var y when y.Contains("/kur/pariteler") => """{"kaynak":"ALTINKAYNAK","pariteler":[]}""",
            var y when y.Contains("/kur/guncel") =>
                """{"id":"kur-9","altinHasFiyati":6800,"gramAltinFiyati":7100,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-14T09:00:00Z","ayarFiyatlari":[]}""",
            // Emanet
            var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI"}]""",
            var y when y.Contains("/emanet/") =>
                """{"cariId":"c1","mevcutHasGramBakiye":0,"madenBakiyeleri":[],"hareketler":[]}""",
            _ => null,
        });
    }

    private void OturumAc(string mod = ArayuzModuServisi.Profesyonel) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1", mod, mod));

    [Fact]
    public void KullaniciKendiRolKilidiSahipligiDevretSoyler()
    {
        OturumAc();
        var c = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        c.WaitForAssertion(() => Assert.Contains("ayse@ornek.com", c.Markup, StringComparison.Ordinal));
        // Kendi satırının Düzenle düğmesi (oturum kullanıcısı = u1, ilk satır).
        c.FindAll("button").First(b => b.TextContent.Trim() == Sozlukler.Tr.Kullanici.Duzenle).Click();

        var ipucu = c.WaitForElement("#degismez-ipucu-KendiRolum").TextContent;
        Assert.Empty(c.FindAll("#d-rol"));                                     // kilit AYNI: rol alanı yok
        Assert.Contains(Sozlukler.Tr.Yonetim.KendiRolunDegismez, ipucu, StringComparison.Ordinal);
        // Düzeltme yolu, ekrandaki GERÇEK düğmenin adıyla yazılır.
        Assert.Contains($"“{Sozlukler.Tr.Kullanici.SahipligiDevret}”", ipucu, StringComparison.Ordinal);
        Assert.Contains(string.Format(O.KendiRolYolu, Sozlukler.Tr.Kullanici.SahipligiDevret), ipucu, StringComparison.Ordinal);
    }

    [Fact]
    public void EvrakTuruKilidiGerekceGosterir()
    {
        var c = Render<Kuyumcu.Web.Pages.Evraklar>().AyrintiAc();
        c.WaitForElement("#evrak-duzenle-e1").Click();

        // Kilit AYNI: tür seçici düzenlemede kapalı.
        var ipucu = c.WaitForElement("#degismez-ipucu-EvrakTuru").TextContent;
        Assert.Contains(O.EvrakTuruGerekce, ipucu, StringComparison.Ordinal);
        // Doğru düzeltme yolu (karşılıksız / geri alma → yeniden giriş).
        Assert.Contains(Sozlukler.Tr.Evrak.DegismezYol, ipucu, StringComparison.Ordinal);

        // Yeni evrakta tür SEÇİLEBİLİR → kilit gerekçesi çizilmez (sahte kilit yok).
        c.Find("#cekmece-evrak-iptal").Click();
        c.Find("#evrak-yeni").Click();
        Assert.Empty(c.FindAll("#degismez-ipucu-EvrakTuru"));
    }

    [Fact]
    public void BildirimKapaliIsAcmaYoluYazili()
    {
        // Basit yüz: "kapalı hatırlatmalar" kartı.
        OturumAc(ArayuzModuServisi.Basit);
        var c = Render<Kuyumcu.Web.Pages.Bildirimler>().AyrintiAc();
        var ipucu = c.WaitForElement("#degismez-ipucu-KurulumKapaliIs").TextContent;
        Assert.Contains(O.BildirimKurulumKapaliGerekce, ipucu, StringComparison.Ordinal);
        Assert.Contains(string.Format(O.BildirimKurulumKapaliYol, KasaBildirimSozlugu.Tr.Ac), ipucu, StringComparison.Ordinal);
        // Kilit AYNI: düğme kapalı, istek atamaz.
        Assert.True(c.Find("#is-ac-TEVSIK_KURTARMA").HasAttribute("disabled"));
    }

    [Fact]
    public void BildirimKapaliIsAcmaYolu_profesyonel_yuzde_de_yazili()
    {
        OturumAc(ArayuzModuServisi.Profesyonel);
        var c = Render<Kuyumcu.Web.Pages.Bildirimler>().AyrintiAc();
        var ipucu = c.WaitForElement("#degismez-ipucu-KurulumKapaliIs").TextContent;
        Assert.Contains(O.BildirimKurulumKapaliGerekce, ipucu, StringComparison.Ordinal);
        Assert.Contains(string.Format(O.BildirimKurulumKapaliYol, Sozlukler.Tr.Bildirim.IsAcikKapali), ipucu, StringComparison.Ordinal);
        // Kutu kilitli; sebebi ARTIK etiketin tekrarı değil, gerçek gerekçe.
        Assert.Equal(Sozlukler.Tr.Bildirim.YapilandirmaKapaliIpucu,
            c.Find("#is-acik-TEVSIK_KURTARMA-durum").TextContent.Trim());
    }

    [Fact]
    public void ManuelKurKaydiIptalYoluYazili()
    {
        OturumAc();
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        var ipucu = c.WaitForElement("#degismez-ipucu-ManuelKurKaydi").TextContent;
        Assert.Contains(O.ManuelKurGerekce, ipucu, StringComparison.Ordinal);
        Assert.Contains(string.Format(O.ManuelKurYolu, Sozlukler.Tr.Kur.IptalEt, Sozlukler.Tr.Kur.ManuelGiris),
            ipucu, StringComparison.Ordinal);
        // Davranış aynı: manuel satırın İptal düğmesi AÇIK (gerekçeli iptal yolu).
        Assert.False(c.Find("#kur-iptal-k2").HasAttribute("disabled"));
    }

    [Fact]
    public void Emanet_kilitli_dugmeler_sebebini_yazar()
    {
        var c = Render<Kuyumcu.Web.Pages.Emanet>().AyrintiAc();
        c.WaitForElement("#e-giris-ac");

        // Müşteri seçilmeden: iki düğme de kilitli, sebep title'da VE görünür satırda.
        var giris = c.Find("#e-giris-ac");
        var cikis = c.Find("#e-cikis-ac");
        Assert.True(giris.HasAttribute("disabled"));
        Assert.True(cikis.HasAttribute("disabled"));
        Assert.Equal(O.EmanetMusteriSecilmedi, giris.GetAttribute("title"));
        Assert.Equal(O.EmanetMusteriSecilmedi, cikis.GetAttribute("title"));
        Assert.Equal("e-kilit", giris.GetAttribute("aria-describedby"));
        Assert.Contains(O.EmanetMusteriSecilmedi, c.Find("#e-kilit").TextContent, StringComparison.Ordinal);

        // Müşteri seçildi, elimizde emanet yok: "+ Emanet Al" açılır, "İade Et" sebebini değiştirir.
        SeciciYardim.Sec(c, "e-cari", "c1");
        c.WaitForAssertion(() => Assert.False(c.Find("#e-giris-ac").HasAttribute("disabled")));
        Assert.Null(c.Find("#e-giris-ac").GetAttribute("title"));
        Assert.True(c.Find("#e-cikis-ac").HasAttribute("disabled"));
        // PAKET EMANET-1: sebep TEK kısa cümle (kilit koşulu değişmedi).
        Assert.Equal(EmanetSadeSozlugu.Tr.IadeYok, c.Find("#e-cikis-ac").GetAttribute("title"));
        Assert.Contains(EmanetSadeSozlugu.Tr.IadeYok, c.Find("#e-kilit").TextContent, StringComparison.Ordinal);
    }
}
