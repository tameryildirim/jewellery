using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23c (FAZ 3) — ÖDEME KALEMİNİN TEK YAZMA YOLU
//
// NE PROBLEMİ ÇÖZÜYOR
// ───────────────────
// Faz 1/2 sonunda ödeme kalemi YALNIZ satış sepeti (SatisModulu.SatisFisi)
// tarafından yazılıyordu. Tahsilat · takas · işçilik · evrak · alış · storno
// kaynaklı fişlerde para el değiştiriyor ama kırılım KAYITSIZ kalıyordu:
// "bu tahsilat neyle yapıldı" sorusu yine kasa_hareketleri'nden ÇIKARIMLA
// cevaplanıyordu. Çıkarım varsayımdır (Anayasa md.1).
//
// ⚠ FAIL-CLOSED KURAL (paket koşulu): sayaç beyaz listesi genişletilebilir AMA
//   her yeni yol için kalem yazımı ZORUNLUDUR; kalem yazmayan yol beyaz listeye
//   GİREMEZ. Bu dosya kuralı âdetten YAPIYA çevirir:
//     • <c>db.OdemeKalemleri.Add/AddRange</c> ÇAĞRISI KOD TABANINDA YALNIZ
//       BURADADIR (denetim.sh: HAREKETLERDE_ODEME_YONTEMI_YENI_YOL = 0 sayacının
//       beyaz listesi artık SatisModulu DEĞİL, bu dosyadır — sayaç GENİŞLEMEDİ,
//       DARALDI).
//     • <see cref="HareketFabrikasi.Ekle"/> ödeme taşıyan HER hareket için
//       <see cref="Otomatik"/>'i çağırır. Yeni bir yazma yolu eklemek için
//       hiçbir şey YAPILMAZ; kalem kendiliğinden doğar. Unutulması imkânsızdır.
//     • Kalemi KENDİ yazan tek istisna satış sepetidir ve bunu
//       <see cref="KalemYazari.Cagiran"/> ile AÇIKÇA beyan eder; beyan etmeyen
//       yol fabrika yolundan geçer.
//
// ⚠ KALEM YAZMAK DEFTERE YAZMAK DEĞİLDİR: hareketler tablosunun tek kapısı
//   HareketFabrikasi olmaya devam eder. Kalem, yazılmış hareketin yanındaki
//   açıklayıcı kayıttır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Bir hareketin ödeme kalemini KİMİN yazdığı.
/// </summary>
public enum KalemYazari
{
    /// <summary>
    /// VARSAYILAN. <see cref="HareketFabrikasi"/> kalemi kendisi türetir ve yazar.
    /// Yeni yazma yolu hiçbir şey yapmadan kapsama girer (fail-closed).
    /// </summary>
    Fabrika,

    /// <summary>
    /// Çağıran kalemi KENDİ yazar ya da BİLİNÇLİ olarak yazmaz. Yalnız iki yerde
    /// kullanılır ve ikisinin de gerekçesi çağrı yerinde yazılıdır:
    ///   • satış sepeti (çok bacaklı fiş — fabrika tek bacak türetemez),
    ///   • storno (ters kayıt YENİ kalem doğurmaz; kaynak kalemleri PASİFLEŞİR).
    /// </summary>
    Cagiran,
}

/// <summary>
/// <c>odeme_kalemleri</c> tablosuna yazmanın TEK KAPISI.
/// </summary>
public static class OdemeKalemFabrikasi
{
    /// <summary>
    /// Kalemleri doğrular ve deftere ekler. <c>Add/AddRange</c> çağrısı
    /// kod tabanında YALNIZ bu metottadır (denetim.sh sayar).
    /// </summary>
    public static void Yaz(KuyumcuDbContext db, IReadOnlyList<OdemeKalemi> kalemler)
    {
        if (kalemler.Count == 0) return;

        foreach (var k in kalemler)
        {
            if (!OdemeAraci.Gecerli(k.Arac))
                throw new InvalidOperationException(
                    $"Ödeme kaleminin aracı kapalı listede değil: '{k.Arac}'. "
                    + $"Geçerli araçlar: {string.Join(", ", OdemeAraci.Tumu)}.");

            if (k.TutarTl < 0m)
                throw new InvalidOperationException(
                    $"Ödeme kalemi tutarı İŞARETSİZDİR (≥ 0); gelen: {k.TutarTl} ({k.Arac}).");

            if (string.IsNullOrWhiteSpace(k.FisId) || string.IsNullOrWhiteSpace(k.HareketId))
                throw new InvalidOperationException(
                    "Ödeme kalemi fiş ve hareket kimliği olmadan yazılamaz "
                    + $"(fis='{k.FisId}', hareket='{k.HareketId}').");
        }

        db.OdemeKalemleri.AddRange(kalemler);
    }

    /// <summary>
    /// TEK ARAÇLI yolun kalemini TÜRETİR ve yazar — <see cref="HareketFabrikasi.Ekle"/>
    /// tarafından çağrılır, başka yerden çağrılmaz.
    ///
    /// <para>TÜRETME SIRASI (tahmin YOK, yalnız YAZILI değerler):</para>
    /// <list type="number">
    ///   <item><c>hareketler.odeme_turu</c> kapalı listedeki bir araçsa
    ///   (NAKIT/KART/HAVALE/VERESIYE/DOVIZ) TEK kalem yazılır; tutarı hareketin
    ///   TL bacağının mutlak değeridir. "KARMA" bir araç DEĞİL özet etikettir ve
    ///   bu dalı tetiklemez.</item>
    ///   <item>Etiket yoksa ya da "KARMA"/"ALTIN"/"DIGER" ise kırılım
    ///   <see cref="OdemeIzi"/> paylarından okunur: nakit payı → NAKIT kalemi,
    ///   veresiye payı → VERESIYE kalemi. Kısmi ödeme (ör. alışın yarısı peşin)
    ///   böylece İKİ kalem doğurur ve toplamları fiş toplamına eşitlenir.</item>
    ///   <item>Ne etiket ne pay varsa (transfer, emanet, sayım düzeltme, açılış)
    ///   kalem YAZILMAZ — para el değiştirmemiştir.</item>
    /// </list>
    ///
    /// <para>⚠ KASA BAĞI: tek araçlı yolda kasa satırı hareketten SONRA doğar,
    /// bu yüzden <c>kasa_id</c>/<c>kasa_hareket_id</c> burada NULL bırakılır ve
    /// UYDURULMAZ. Bağ kaybolmaz: <c>kasa_hareketleri.hareket_id</c> aynı
    /// hareketi gösterir, kırılım oradan tamamlanır. Çok bacaklı satış fişinde
    /// bacağın kendi kasası olduğu için o alanlar DOLU yazılır (SatisModulu).</para>
    /// </summary>
    public static void Otomatik(KuyumcuDbContext db, Hareket hareket, OdemeIzi izi)
    {
        var kalemler = Turet(hareket, izi);
        Yaz(db, kalemler);
    }

    /// <summary>
    /// Kalemleri TÜRETİR (yazmaz) — test ve denetim bu saf işlevi doğrudan sınar.
    /// </summary>
    public static List<OdemeKalemi> Turet(Hareket hareket, OdemeIzi izi)
    {
        var sonuc = new List<OdemeKalemi>(2);
        // Fiş, aynı fis_id'yi taşıyan satır kümesidir; tek satırlık yollarda
        // (tahsilat, işçilik, takas, evrak) fiş O HAREKETİN KENDİSİDİR.
        var fisId = string.IsNullOrWhiteSpace(hareket.FisId) ? hareket.Id : hareket.FisId!;

        if (OdemeAraci.Gecerli(hareket.OdemeTuru))
        {
            var tutar = Math.Round(Math.Abs(hareket.TlTutari), 2, MidpointRounding.AwayFromZero);
            if (tutar > 0m)
                sonuc.Add(Kalem(hareket, fisId, 0, hareket.OdemeTuru!, tutar));
            return sonuc;
        }

        var nakit = Math.Round(izi.Nakit, 2, MidpointRounding.AwayFromZero);
        var veresiye = Math.Round(izi.Veresiye, 2, MidpointRounding.AwayFromZero);
        var sira = 0;
        if (nakit > 0m) sonuc.Add(Kalem(hareket, fisId, sira++, OdemeAraci.Nakit, nakit));
        if (veresiye > 0m) sonuc.Add(Kalem(hareket, fisId, sira, OdemeAraci.Veresiye, veresiye));
        return sonuc;
    }

    private static OdemeKalemi Kalem(Hareket h, string fisId, int sira, string arac, decimal tutar) => new()
    {
        Id = Kimlik.Ulid(),
        TenantId = h.TenantId,
        FisId = fisId,
        HareketId = h.Id,
        Sira = sira,
        Arac = arac,
        TutarTl = tutar,
        // UX-S32: tutar para birimiyle BİRLİKTE taşınır. Döviz bacağında
        // hareketin kendi para birimi ve İŞARETSİZ efektif miktarı yazılır.
        ParaBirimi = arac == OdemeAraci.Doviz ? h.ParaBirimi : "TRY",
        DovizTutar = arac == OdemeAraci.Doviz && h.DovizTutar is { } d ? Math.Abs(d) : null,
        // ⚠ Kasa bağı UYDURULMAZ (yukarıdaki gerekçe).
        KasaId = null,
        KasaHareketId = null,
        KurSnapshotId = h.KurSnapshotId,
        Bilinmiyor = false,          // değerler YAZILI kaynaklardan geldi, tahmin yok
        GeriDolduruldu = false,      // canlı yolda doğdu
        Aktif = true,
        Olusturuldu = h.Olusturuldu == default ? DateTime.UtcNow : h.Olusturuldu,
    };
}
