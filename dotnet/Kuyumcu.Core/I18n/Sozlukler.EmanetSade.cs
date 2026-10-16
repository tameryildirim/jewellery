namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET EMANET-1 — EMANET EKRANI "ZORUNLU ÖNCE" (dalga T8): TR + EN METİNLER
//
// Kurucu 25.09: "ne işlem yapacaksa en basit haliyle, zorunlu olanlar hariç ilk
// etapta kullanıcı görmesin". Eski metinler BÜYÜK HARFLE vurgu yapıyordu
// ("SİZİN DEĞİLDİR", "GİRMEZ") ve kilit sebebi "Eksik olan:" önekiyle
// basılıyordu. Bu dosya yalnız YENİ metinleri taşır; hub'a (Sozlukler.cs)
// dokunulmaz (orkestratör kuralı).
//
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record; Tr ve En aynı
//   kurucudan geçer.
// ⚠ BÜYÜK HARF VURGU YOK — EmanetSadeTestleri her metni denetler.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Emanet ekranının sade açılış metinleri (bir dilde).</summary>
public sealed record EmanetSadeMetinleri(
    string Aciklama,
    string BilgiEtiketi,
    string Bilgi,
    string EmanetAl,
    string EmanetIade,
    string IadeYok,
    string EmanetiYok,
    string EskiKayitlar,
    string GirisDetayEkle,
    string CikisNotEkle,
    string MadenNotu,
    string IadeCokKucuk,
    string CikisHesapEkle,
    string SatirGeriAl,
    // ── Doğrulayıcı 28.09: geri alınmış kayıt durumunda jargon (storno) ve büyük harf yok ──
    string HareketDegismez,
    string HareketDegismezYolu,
    string GeriAlindi,
    string GeriAlmaAciklama,
    string KurYokGeriAl,
    string GeriAlBaslik,
    string GeriAlOnay,
    string GeriAlGerekce,
    string AramaSinir,
    string IadeEnAz,
    string NedenGeriAlindi,
    string NedenGeriAlmaKaydi,
    string NedenFis,
    string GeriAlYapildi,
    // ── Doğrulayıcı 28.09: ULID banda basılmaz; çekmece tabanı "Vazgeç + Kaydet" ──
    string GeriAlYapildiSade,
    string Vazgec,
    // ── 28.09 canlı ölçüm: yükleme sürerken ve hata hâlinde "Emaneti yok" DENMEZ (UX-S28) ──
    string Yukleniyor,
    string BilgiAlinamadi,
    // ── Tasarım hakemi 28.09 (KRİTİK): Ayrıntı çekmecesinde kilit kutusu yerine TEK kısa satır ──
    string DetayDuzeltme);

public static class EmanetSadeSozlugu
{
    public static readonly EmanetSadeMetinleri Tr = new(
        Aciklama: "Müşterinin bırakıp sonra geri alacağı altın.",
        BilgiEtiketi: "Emanet nedir?",
        Bilgi: "Emanet altın işletmenin malı değildir; kâr-zarar ve stok değerlemesine girmez.",
        EmanetAl: "Emanet al",
        EmanetIade: "Emanet iade et",
        IadeYok: "Bu müşterinin emaneti yok.",
        EmanetiYok: "Emaneti yok",
        EskiKayitlar: "Eski kayıtlar",
        GirisDetayEkle: "+ Taş ağırlığı ve not ekle",
        CikisNotEkle: "+ Not ekle",
        MadenNotu: "Toplam has yalnız altındır; diğer madenler ayrı satırda yazılır.",
        IadeCokKucuk: "Kalan miktar iade için çok küçük.",
        CikisHesapEkle: "+ Ayar ve gramdan hesapla",
        SatirGeriAl: "Geri al",
        HareketDegismez: "Bu kayıt geri alınamaz: ya daha önce geri alındı, ya kendisi bir geri alma kaydıdır, ya da birden çok satırlı bir fişin parçasıdır.",
        HareketDegismezYolu: "Birden çok satırlı fişte fişin tamamı düzeltme fişiyle düzeltilir. Geri alınmış kayıt ikinci kez geri alınmaz.",
        GeriAlindi: "Bu kayıt geri alındı",
        GeriAlmaAciklama: "Geri alma: {0}",
        KurYokGeriAl: "Güncel kur gelmeden geri alma yapılamaz; geri alma bugünün kuruyla yazılır.",
        GeriAlBaslik: "Kaydı geri al",
        GeriAlOnay: "Bu kaydın yanına ters bir kayıt yazılır; asıl kayıt silinmez, listede kalır. Bu işlem geri alınamaz.",
        GeriAlGerekce: "Geri alma sebebi",
        AramaSinir: "Listede tüm müşteriler görünmüyor; aradığınız yoksa adını yazıp arayın.",
        IadeEnAz: "İade edilecek miktar en az {0} olmalı.",
        NedenGeriAlindi: "Daha önce geri alındı.",
        NedenGeriAlmaKaydi: "Bu, bir geri alma kaydıdır.",
        NedenFis: "Birden çok satırlı bir fişin parçasıdır; fişin tamamı düzeltme fişiyle düzeltilir.",
        GeriAlYapildi: "Kayıt geri alındı ({0}).",
        GeriAlYapildiSade: "Kayıt geri alındı.",
        Vazgec: "Vazgeç",
        Yukleniyor: "Yükleniyor…",
        BilgiAlinamadi: "Emanet bilgisi alınamadı.",
        DetayDuzeltme: "Yanlışsa Geri al ile düzeltin.");

    public static readonly EmanetSadeMetinleri En = new(
        Aciklama: "Gold a customer leaves with you and takes back later.",
        BilgiEtiketi: "What is custody?",
        Bilgi: "Custody gold does not belong to the business; it is excluded from profit and loss and stock valuation.",
        EmanetAl: "Take in custody",
        EmanetIade: "Return custody",
        IadeYok: "This customer has no custody.",
        EmanetiYok: "No custody",
        EskiKayitlar: "Past records",
        GirisDetayEkle: "+ Add stone weight and note",
        CikisNotEkle: "+ Add note",
        MadenNotu: "Total fine weight is gold only; other metals are listed on separate lines.",
        IadeCokKucuk: "The remaining amount is too small to return.",
        CikisHesapEkle: "+ Calculate from karat and weight",
        SatirGeriAl: "Reverse",
        HareketDegismez: "This record cannot be reversed: it was already reversed, it is itself a reversal record, or it is part of a multi-line slip.",
        HareketDegismezYolu: "A multi-line slip is corrected as a whole with a correction slip. A reversed record cannot be reversed twice.",
        GeriAlindi: "This record was reversed",
        GeriAlmaAciklama: "Reversal: {0}",
        KurYokGeriAl: "A reversal needs today's exchange rate; wait until the rate is loaded.",
        GeriAlBaslik: "Reverse the record",
        GeriAlOnay: "A reversing record is written beside this one; the original is not deleted and stays in the list. This cannot be undone.",
        GeriAlGerekce: "Reason for reversal",
        AramaSinir: "Not all customers are shown; if yours is missing, type their name to search.",
        IadeEnAz: "The amount to return must be at least {0}.",
        NedenGeriAlindi: "It was already reversed.",
        NedenGeriAlmaKaydi: "This is a reversal record.",
        NedenFis: "It is part of a multi-line slip; the whole slip is corrected with a correction slip.",
        GeriAlYapildi: "Record reversed ({0}).",
        GeriAlYapildiSade: "Record reversed.",
        Vazgec: "Cancel",
        Yukleniyor: "Loading…",
        BilgiAlinamadi: "Custody details could not be loaded.",
        DetayDuzeltme: "If it is wrong, fix it with Reverse.");

    public static EmanetSadeMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
