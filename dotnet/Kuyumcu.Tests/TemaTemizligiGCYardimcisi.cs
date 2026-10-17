using Bunit;
using Kuyumcu.Web.Pages;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TEMA TEMİZLİĞİ 17.10 — GC şeridi (İşlemler ekranının eski tuval yüzleri).
//
// SATIŞ fişi artık HER ZAMAN Yıldız tezgâhıdır (Bilesenler/Yildiz/YildizTezgah.razor):
//   • müşteri "Değiştir" katmanında (#yildiz-tezgah-degistir → #yildiz-musteri-sec-{id});
//   • tek ödeme satırı #yildiz-odeme-0 (dokununca ödeme düzeni: #islem-aldim-odeme / FisOdemeKasa);
//   • "+ Başka ödeme" (#yildiz-tezgah-baska-odeme) karmaya geçirir / karmada yeni satır ekler;
//   • "− Tek ödeme" (#yildiz-tezgah-tek-odeme), Fark (#yildiz-tezgah-fark-tutar), Kaydet (#yildiz-tezgah-kaydet),
//     kilit sebebi / yasal not (#yildiz-tezgah-not), sunucu hatası (#yildiz-tezgah-hata), son fiş + yazdır
//     (#yildiz-tezgah-son-fis, #yildiz-tezgah-yazdir), kimlik çekmecesi (#yildiz-tezgah-kimlik).
// Kalem girişi / ödeme / MASAK / nakit sınırı / Peşin kapıları AYNI sayfa durumundan gelir; bu yardımcılar
// yalnız kullanıcının Yıldız'da GERÇEKTEN izlediği yolu sürer.
//
// ⚠ SESSİZ GEÇİŞ YOK: her adım beklenen öğe çizilene dek bekler, çizilmezse HATA verir.
// ═════════════════════════════════════════════════════════════════════════════
internal static class TemaTemizligiGCYardimcisi
{
    public static readonly TimeSpan Bekle = TimeSpan.FromSeconds(6);

    /// <summary>Yıldız satış tezgâhı çizildi ve açılış yüklemesi (Peşin Müşteri ön seçimi) bitti.</summary>
    public static IRenderedComponent<Islemler> YildizHazir(this IRenderedComponent<Islemler> e, string? pesinAdi = "Peşin Müşteri")
    {
        e.WaitForElement("#yildiz-tezgah-kaydet", TimeSpan.FromSeconds(10));
        // Sahte sunucu sistem carisini vermiyorsa (pesinAdi null) Peşin ön seçimi beklenmez; kur gelene dek beklenir.
        if (pesinAdi is not null)
            e.WaitForAssertion(() => Assert.Contains(pesinAdi, e.Find("#yildiz-tezgah-ust").TextContent), Bekle);
        return e;
    }

    /// <summary>Elle ürün seçip "Ekle" (fiyat sunucu teklifinden dolar); tezgâhta kalem satırı çoğalana dek bekler.</summary>
    public static IRenderedComponent<Islemler> YildizKalemEkle(this IRenderedComponent<Islemler> e, string urunId = "u1", string? beklenenFiyat = null)
    {
        var once = e.FindAll(".yz-tezgah-kalem").Count;
        string Toplam() => e.Find("#yildiz-tezgah-toplam-rakam").TextContent;
        var oncekiToplam = Toplam();
        SeciciYardim.Sec(e, "islem-urun", urunId);
        if (beklenenFiyat is null)
            e.WaitForAssertion(() => Assert.False(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "islem-birimfiyat"))), Bekle);
        else
            e.WaitForAssertion(() => Assert.Equal(beklenenFiyat, SeciciYardim.SeciliDeger(e, "islem-birimfiyat")), Bekle);
        e.Find("#islem-kalem-ekle").Click();
        // Aynı ürün ikinci kez eklenince yeni satır açılmaz, adet artar → satır sayısı YA DA toplam değişmeli.
        e.WaitForAssertion(() => Assert.True(e.FindAll(".yz-tezgah-kalem").Count > once || Toplam() != oncekiToplam), Bekle);
        return e;
    }

    /// <summary>"Değiştir" katmanından müşteri seçer (katman seçimle kapanır).</summary>
    public static IRenderedComponent<Islemler> YildizMusteriSec(this IRenderedComponent<Islemler> e, string cariId)
    {
        if (e.FindAll("#yildiz-tezgah-musteri").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement($"#yildiz-musteri-sec-{cariId}", Bekle).Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#yildiz-tezgah-musteri")), Bekle);
        return e;
    }

    /// <summary>Tek ödeme satırına dokunur → ödeme düzeni (FisOdemeKasa, BASİT'te #islem-aldim-odeme) açılır.</summary>
    public static IRenderedComponent<Islemler> YildizOdemeAc(this IRenderedComponent<Islemler> e)
    {
        if (e.FindAll("#yildiz-tezgah-odeme-duzen").Count == 0) e.WaitForElement("#yildiz-odeme-0", Bekle).Click();
        e.WaitForElement("#yildiz-tezgah-odeme-duzen", Bekle);
        return e;
    }

    /// <summary>"+ Başka ödeme": tek ödeme İLK satır olur, ikinci satır açılır (OdemeyiBol).</summary>
    public static IRenderedComponent<Islemler> YildizBol(this IRenderedComponent<Islemler> e)
    {
        e.WaitForAssertion(() => Assert.False(e.Find("#yildiz-tezgah-baska-odeme").HasAttribute("disabled")), Bekle);
        e.Find("#yildiz-tezgah-baska-odeme").Click();
        e.WaitForElement("#islem-bacak-1-arac", Bekle);
        return e;
    }

    /// <summary>Tür sekmelerinin "Diğer"i: tüm türler / PRO Diğer menüsü açılır.</summary>
    public static IRenderedComponent<Islemler> YildizTurlerAc(this IRenderedComponent<Islemler> e)
    {
        e.Find("#yildiz-tezgah-tur").Click();
        e.WaitForAssertion(() => Assert.Contains("yz-turler-acik", e.Find(".islem-sayfa").ClassList), Bekle);
        return e;
    }

    public static bool YildizKaydetKilitli(this IRenderedComponent<Islemler> e) => e.Find("#yildiz-tezgah-kaydet").HasAttribute("disabled");

    /// <summary>Kaydet açılana dek bekler (açılmazsa kilit sebebini yazar), sonra basar.</summary>
    public static void YildizKaydet(this IRenderedComponent<Islemler> e)
    {
        e.WaitForAssertion(() => Assert.False(e.YildizKaydetKilitli(),
            "Kaydet kilitli: " + e.Find("#yildiz-tezgah-kaydet").GetAttribute("title")), Bekle);
        e.Find("#yildiz-tezgah-kaydet").Click();
        TemaTemizligiGDYardimcisi.FaturaSorusunaHayir(e);   // 17.10: müşterili satışta fatura sorusu → faturasız eski akış
    }

    /// <summary>Fark satırının metni (NBSP düzeltilmiş).</summary>
    public static string YildizFark(this IRenderedComponent<Islemler> e) =>
        e.Find("#yildiz-tezgah-fark-tutar").TextContent.Replace(' ', ' ').Trim();
}
