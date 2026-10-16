using Bunit;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-65 — /kur KİLİTLİ DÜĞME GEREKÇESİ [bUnit] (tur 8 · görsel E49 · UX-S30)
//
// QA tur 8: kur geçmişindeki ~100 "İptal et" düğmesi dış kaynaklı satırlarda
// KİLİTLİYDİ ama neden kilitli olduğu düğmenin üstünde yazmıyordu (ipucu
// tablonun ALTINDAYDI, düğmeyle bağı kurulmamıştı). Artık her kilitli düğme
// title + aria-describedby ile sebebini taşır. Kilit KOŞULU değişmedi.
//
// ⚠ Fikstür (sahte API, Ciz) EmanetKurEkranTestleri.cs'teki KurEkranTestleri
//   sınıfınındır; bu dosya o sınıfın partial parçasıdır (ikinci fikstür yok).
//   Oturumsuz varsayılan yüz PROFESYONEL → kur geçmişi çizilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed partial class KurEkranTestleri
{
    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> GecmisliCiz()
    {
        var e = Ciz();
        e.WaitForElement("#kur-iptal-k1");
        return e;
    }

    // ── PAKET S1-i — geçmiş sekmesi 7,4 ekran boyuydu: ilk bakış 10 satır ──
    [Fact]
    public void S1i_Kur_gecmisi_ilk_bakista_10_satir_toplam_yazili_daha_goster()
    {
        var e = GecmisliCiz();
        Assert.Equal(10, e.FindAll("#kur-panel-gecmis tbody tr").Count);
        Assert.Equal("1–10 / 14 kayıt", e.Find("#kur-gecmis-aralik").TextContent.Trim());
        Assert.Equal("4 kayıt daha göster", e.Find("#kur-gecmis-daha").TextContent.Trim());
        e.Find("#kur-gecmis-daha").Click();
        Assert.Equal(14, e.FindAll("#kur-panel-gecmis tbody tr").Count);
        Assert.Empty(e.FindAll("#kur-gecmis-daha"));
    }

    [Fact]
    public void DevreDisiIptalDugmesi_GerekceMetniTasir()
    {
        var e = GecmisliCiz();

        // Dış kaynaklı satır: düğme KİLİTLİ (davranış aynı) ve sebebi düğmenin ÜZERİNDE.
        var kilitli = e.Find("#kur-iptal-k1");
        Assert.True(kilitli.HasAttribute("disabled"));
        Assert.Equal(Sozlukler.Tr.Kur.IptalYok, kilitli.GetAttribute("title"));

        // Görünür yardım metni: aria-describedby gerçek, dolu bir öğeye bağlanır.
        var bagli = kilitli.GetAttribute("aria-describedby");
        Assert.False(string.IsNullOrWhiteSpace(bagli));
        Assert.Contains(Sozlukler.Tr.Kur.IptalYok, e.Find($"#{bagli}").TextContent, StringComparison.Ordinal);

        // Elle girilen satır: düğme AÇIK, kilit gerekçesi YOK (sahte sebep basılmaz).
        var acik = e.Find("#kur-iptal-k2");
        Assert.False(acik.HasAttribute("disabled"));
        Assert.Null(acik.GetAttribute("title"));
    }

    [Fact]
    public void Kur_ekraninda_devre_disi_HER_dugme_bos_olmayan_gerekce_tasir()
    {
        var e = GecmisliCiz();
        var kilitliler = e.FindAll("button[disabled]");
        Assert.NotEmpty(kilitliler);
        Assert.All(kilitliler, d =>
        {
            var title = d.GetAttribute("title");
            var bagli = d.GetAttribute("aria-describedby");
            var aciklama = !string.IsNullOrWhiteSpace(bagli) ? e.FindAll($"#{bagli}").FirstOrDefault()?.TextContent : null;
            Assert.True(!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(aciklama),
                $"gerekçesiz kilitli düğme: #{d.Id} '{d.TextContent.Trim()}'");
        });
    }

    [Fact]
    public void Iptal_onayinda_vazgec_ve_onay_ayni_fiili_tasimaz()
    {
        var e = GecmisliCiz();
        e.Find("#kur-iptal-k2").Click();
        var vazgec = e.Find("#kur-iptal-vazgec").TextContent.Trim();
        var onay = e.Find("#kur-iptal-onayla").TextContent.Trim();
        // Eylem "İptal et" iken vazgeç düğmesi de "İptal" diyordu (E49).
        Assert.Equal(OnayKilitSozlugu.Tr.Vazgec, vazgec);
        Assert.False(Kuyumcu.Web.Bilesenler.OnayKipi.AyniFiil(vazgec, Sozlukler.Tr.Kur.IptalEt));
        Assert.False(Kuyumcu.Web.Bilesenler.OnayKipi.AyniFiil(vazgec, onay));
    }
}
