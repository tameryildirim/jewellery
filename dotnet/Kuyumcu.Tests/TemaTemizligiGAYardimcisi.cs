using AngleSharp.Dom;
using Bunit;
using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ (kurucu kararı 17.10, grup GA): SATIŞ fişi artık HER ZAMAN Yıldız tezgâhıdır
// (Islemler.YildizTezgahMi — Bilesenler/Yildiz/YildizTezgah.razor). Eski PRO/BASİT satış yüzünün
// kimlikleri (#islem-kaydet, #islem-cari, #islem-kalem-pro, #islem-pro-karma …) SATIŞ'ta çizilmez;
// öteki türler (Alış, Hurda, Takas, İade …) özgün formla çizilir ve kimlikleri AYNIDIR.
//
// Bu yardımcı kullanıcının Yıldız'da GERÇEKTEN izlediği yolu sürer — kural/kapı/gövde AYNI kaynaktan
// (sayfanın Gonderilebilir / KaydetKilitSebebi / MasakParcasi / OdemeParcasi'sı) gelir:
//   • Kaydet          → #yildiz-tezgah-kaydet (disabled + title = sayfanın kilidi ve sebebi)
//   • kilit/yasal satır → #yildiz-tezgah-not (tema-yildiz.css: özgün #islem-yasal-kapi / #hata-bandi Yıldız'da display:none)
//   • müşteri         → "Değiştir" katmanı (#yildiz-tezgah-degistir → #yildiz-musteri-sec-{id})
//   • ödeme türü      → ödeme satırına dokun (#yildiz-odeme-0) → #islem-aldim-odeme (BASİT) / #islem-odeme (PRO)
//   • kimlik formu    → "+ Kimlik bilgisi" (#yildiz-tezgah-kimlik) → çekmece (#cekmece-yildiz-kimlik, MasakFormu)
// ⚠ SESSİZ GEÇİŞ YOK: beklenen düğme/alan süre içinde çıkmazsa yardımcı HATA verir.
// ═════════════════════════════════════════════════════════════════════════════
internal static class GA
{
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);

    /// <summary>Sayfa şu an Yıldız satış tezgâhını mı çiziyor (SATIŞ türü)?</summary>
    public static bool Tezgah(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => e.FindAll("#yildiz-tezgah").Count > 0;

    /// <summary>Kaydet düğmesi: SATIŞ'ta tezgâhın birincil düğmesi, öteki türlerde özgün #islem-kaydet.</summary>
    public static IElement Kaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll("#yildiz-tezgah-kaydet") is { Count: > 0 } y ? y[0] : e.Find("#islem-kaydet");

    /// <summary>Kaydet'in yanındaki tek cümle: SATIŞ'ta Fark bloğunun not satırı, öteki türlerde #islem-kaydet-kilit.</summary>
    public static IElement KilitSatiri(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        Tezgah(e) ? e.Find("#yildiz-tezgah-not") : e.Find("#islem-kaydet-kilit");

    /// <summary>Sunucu hata satırı: SATIŞ'ta #yildiz-tezgah-hata (özgün #hata-bandi Yıldız'da display:none), öteki türlerde #hata-bandi.</summary>
    public static string HataSecici(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => Tezgah(e) ? "#yildiz-tezgah-hata" : "#hata-bandi";

    /// <summary>Müşteri seçimi: SATIŞ'ta "Değiştir" katmanından (seçim katmanı kapatır), öteki türlerde #islem-cari seçicisi.</summary>
    public static void MusteriSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id)
    {
        if (!Tezgah(e))
        {
            e.WaitForElement("#islem-cari", Bekle);
            SeciciYardim.Sec(e, "islem-cari", id);
            return;
        }
        if (e.FindAll("#yildiz-musteri-secici").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement($"#yildiz-musteri-sat-{id}", Bekle);
        if (e.Find($"#yildiz-musteri-sat-{id}").GetAttribute("aria-current") == "true")
        {
            e.Find("#yildiz-tezgah-degistir").Click();   // zaten seçili: katman kapanır
        }
        else
        {
            e.Find($"#yildiz-musteri-sec-{id}").Click();
        }
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-musteri-secici")), Bekle);
        e.WaitForAssertion(() => Assert.Equal(id, MusteriId(e)), Bekle);
    }

    /// <summary>Seçili müşterinin kimliği (sayfa durumu — tezgâh üst satırı adı yazar, kimliği değil).</summary>
    public static string? MusteriId(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (!Tezgah(e)) return SeciciYardim.SeciliDeger(e, "islem-cari");
        return (string?)typeof(Kuyumcu.Web.Pages.Islemler)
            .GetField("cariId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(e.Instance);
    }

    /// <summary>Tezgâhta tek ödeme düzenini açar (ödeme satırına dokunma — tuval kalıbı). Karmada düzen zaten açıktır.</summary>
    public static void OdemeDuzenAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (!Tezgah(e) || e.FindAll("#yildiz-tezgah-odeme-duzen").Count > 0) return;
        e.WaitForElement("#yildiz-odeme-0", Bekle).Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen", Bekle);
    }

    /// <summary>Ödeme türü seçicisinin kimliği (düzen açıkken): BASİT tezgâhta #islem-aldim-odeme, PRO'da #islem-odeme.</summary>
    public static string OdemeSeciciId(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        OdemeDuzenAc(e);
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#islem-aldim-odeme, #islem-odeme")), Bekle);
        return e.FindAll("#islem-aldim-odeme").Count > 0 ? "islem-aldim-odeme" : "islem-odeme";
    }

    /// <summary>Tek ödemenin türünü seçer (tezgâhta ödeme düzeni açılarak; öteki türlerde #islem-odeme).</summary>
    public static void OdemeSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string tur)
    {
        if (!Tezgah(e))
        {
            e.WaitForElement("#islem-odeme", Bekle);
            SeciciYardim.Sec(e, "islem-odeme", tur);
            return;
        }
        var id = OdemeSeciciId(e);
        SeciciYardim.Sec(e, id, tur);
        e.WaitForAssertion(() => Assert.Equal(tur, SeciciYardim.SeciliDeger(e, id)), Bekle);
    }

    /// <summary>Satis4Yardim.NakitElleSec'in Yıldız yolu: nakit kuralını sınayan testler nakdi ELLE seçer.</summary>
    public static void NakitSec(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) => OdemeSec(e, "NAKIT");

    /// <summary>Karma ödemeyi açar: tek ödemede "+ Başka ödeme" (sayfanın OdemeyiBol'u — iki satır).</summary>
    public static void KarmaAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-baska-odeme").HasAttribute("disabled")), Bekle);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForElement("#islem-bacak-1-arac", Bekle);
    }

    /// <summary>Karmada yeni ödeme satırı: "+ Başka ödeme" (FisOdemeBacaklari.BacakEkle'nin aynası — özgün #islem-bacak-ekle Yıldız'da çizilmez).</summary>
    public static void BacakEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.Find(e.FindAll("#islem-bacak-ekle").Count > 0 ? "#islem-bacak-ekle" : "#yildiz-tezgah-baska-odeme").Click();

    /// <summary>Tezgâhtaki kalem satırı sayısı (öteki yüzlerde tablo satırları).</summary>
    public static int KalemSayisi(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll(".yz-tezgah-kalem, #islem-kalem-tablo tbody tr.fis-tuval-satir, #islem-kalem-pro tr.fis-pro-satir").Count;

    /// <summary>Tezgâhta kalemi çıkarır: satıra dokun → "Çıkar" (sayfanın KalemSil'i — özgün ✕ ile AYNI yol).</summary>
    public static void KalemCikar(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, int sira)
    {
        if (e.FindAll($"#yildiz-kalem-cikar-{sira}").Count == 0) e.Find($"#yildiz-kalem-{sira}").Click();
        e.WaitForElement($"#yildiz-kalem-cikar-{sira}", Bekle).Click();
    }

    /// <summary>"+ İndirim" alanını açar (fiş iskontosu #islem-fis-iskonto — FisIskontoSatiri, kural AYNI).</summary>
    public static IElement IndirimAlani(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (e.FindAll("#islem-fis-iskonto").Count == 0)
            e.Find(Tezgah(e) ? "#yildiz-tezgah-indirim" : "#islem-indirim").Click();
        return e.WaitForElement("#islem-fis-iskonto", Bekle);
    }

    /// <summary>
    /// Kimlik formu AÇILDI mı (sayfanın KimlikGorunur'u): tezgâhta "+ Kimlik bilgisi" düğmesi çıkar; düğmeye basılır ve
    /// çekmecede MasakFormu (.kart--masak) çizilene kadar beklenir. Öteki türlerde form doğrudan sayfadadır.
    /// </summary>
    public static void KimlikFormuAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (Tezgah(e))
        {
            if (e.FindAll("#cekmece-yildiz-kimlik").Count == 0) e.WaitForElement("#yildiz-tezgah-kimlik", Bekle).Click();
            e.WaitForElement("#cekmece-yildiz-kimlik", Bekle);
        }
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kart--masak")), Bekle);
    }

    /// <summary>Kimlik formu YOK: ne form (MasakFormu/.kart--masak) ne de onu açan tezgâh düğmesi (#yildiz-tezgah-kimlik) var —
    /// kapalı çekmece "form yok" diye yanlış yeşil vermez.</summary>
    public static void KimlikFormuYok(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        Assert.Empty(e.FindAll("#yildiz-tezgah-kimlik"));
        Assert.Empty(e.FindAll("#cekmece-yildiz-kimlik"));
        Assert.Empty(e.FindAll(".kart--masak"));
        Assert.Empty(e.FindComponents<MasakFormu>());
    }

    /// <summary>Kimlik formu gerekli mi (tezgâhta düğme ya da açık çekmece; öteki türlerde .kart--masak).</summary>
    public static bool KimlikFormuVar(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll("#yildiz-tezgah-kimlik, #cekmece-yildiz-kimlik, .kart--masak").Count > 0;

    /// <summary>Satis4Yardim.MasakBelgeNoSil'in Yıldız yolu: önce kimlik çekmecesi açılır, sonra belge no silinir.</summary>
    public static void MasakBelgeNoSil(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        KimlikFormuAc(e);
        Satis4Yardim.MasakBelgeNoSil(e);
    }

    /// <summary>
    /// Yıldız tezgâhında display:none olan özgün parçalar (tema-yildiz.css "TEZGÂH tur 2" ve YILDIZ-4C):
    /// ekran okuyucu da okumaz — görünür metin envanterine ve "tek cümle" sayımına GİRMEZ (görsel-gizli kopyalar girer).
    /// </summary>
    public static readonly string[] TezgahtaCizilmeyen =
    [
        "hata-bandi", "hata-ayrinti", "basari-bandi", "islem-yazdir-sar", "islem-yazdir-hata", "islem-taksit-uyari",
        "islem-taksit-hata", "islem-yerlesim-uyari", "islem-yasal-kapi", "islem-nakit-uyari-kutu", "islem-reyon-hata",
    ];

    /// <summary>Öğe (ya da atası) Yıldız tezgâhının display:none kuralına mı giriyor?</summary>
    public static bool TezgahtaGizli(IElement? el)
    {
        var tezgah = false;
        for (var x = el; x is not null; x = x.ParentElement)
            if (x.ClassList.Contains("yz-islem")) tezgah = true;
        if (!tezgah) return false;
        for (var x = el; x is not null; x = x.ParentElement)
            if (Array.IndexOf(TezgahtaCizilmeyen, x.Id) >= 0) return true;
        return false;
    }

    /// <summary>Bileşenin ekranda (ve ekran okuyucuda) var olan TÜM metni: görsel-gizli kopyalar DAHİL, Yıldız'da display:none parçalar HARİÇ.</summary>
    public static string TumMetin(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        var sb = new System.Text.StringBuilder();
        void Gez(INode n)
        {
            if (n is IElement el && TezgahtaGizli(el)) return;
            if (n.NodeType == NodeType.Text) { sb.Append(n.TextContent); return; }
            foreach (var c in n.ChildNodes) Gez(c);
        }
        foreach (var n in e.Nodes) Gez(n);
        return sb.ToString();
    }
}
