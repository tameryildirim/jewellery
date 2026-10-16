using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Data;

/// <summary>
/// Bir hareketin ÖDEME İZİ — nakit ve veresiye payları, İŞARETSİZ (≥ 0) TL.
/// Kart/havale/altın payı burada tutulmaz: onlar ne nakittir ne alacak.
/// </summary>
/// <param name="Nakit">Bu hareketle fiilen el değiştiren nakit TL.</param>
/// <param name="Veresiye">Bu hareketle cariye borç/alacak yazılan TL.</param>
/// <param name="Vade">
/// Borç DOĞURAN veresiye payının vade günü; borç azaltan ya da veresiye
/// olmayan izlerde <c>null</c>.
/// </param>
/// <param name="Kalem">
/// PAKET P-23c — ödeme kalemini KİM yazar. Varsayılan
/// <see cref="KalemYazari.Fabrika"/>'dır: yeni bir yazma yolu eklendiğinde
/// kalem KENDİLİĞİNDEN doğar, çağıranın bir şey hatırlaması gerekmez
/// (fail-closed). Kalemi kendi yazan / bilinçli yazmayan iki yol bunu
/// <see cref="KalemYazari.Cagiran"/> ile AÇIKÇA beyan eder.
/// </param>
public readonly record struct OdemeIzi(
    decimal Nakit, decimal Veresiye, DateOnly? Vade, KalemYazari Kalem = KalemYazari.Fabrika)
{
    /// <summary>Para el değiştirmeyen hareketler: transfer, emanet, sayım düzeltme.</summary>
    public static readonly OdemeIzi Yok = new(0m, 0m, null);

    /// <summary>Tamamı nakit — işaret önemsiz, mutlak değer alınır.</summary>
    public static OdemeIzi Nakitten(decimal tutar) => new(Math.Abs(tutar), 0m, null);

    /// <summary>
    /// Tamamı veresiye, BORÇ DOĞURAN yön — işaret önemsiz, mutlak değer alınır.
    ///
    /// ⚠ <paramref name="vade"/> ZORUNLU ve VARSAYILANI YOK: vade takibi
    /// "her veresiye yazma noktası vade koysun" âdetiyle bırakılsaydı, eklenen
    /// bir sonraki veresiye yolu onu unutur ve o borç hiçbir gecikme listesinde
    /// görünmezdi — müşteri borcu sessizce yaşlanırdı. Vadesiz kod DERLENMEZ.
    /// Vade hesabının tek yeri <c>VadeKapisi.VadeHesapla</c>'dır.
    /// </summary>
    public static OdemeIzi Veresiyeden(decimal tutar, DateOnly vade) => new(0m, Math.Abs(tutar), vade);

    /// <summary>
    /// Veresiye bacağı olan ama borç AZALTAN hareketler: iade, tahsilat.
    /// Vade taşımaz — kapanan borcun vadesi kapanmıştır.
    /// Ayrı isimdedir ki iade yazan kişi bilinçli seçsin; <c>Veresiyeden</c>'in
    /// vadesiz bir aşırı yüklemesi OLSAYDI, borç doğuran çağrı da yanlışlıkla
    /// onu seçebilirdi.
    /// </summary>
    public static OdemeIzi VeresiyeKapanisi(decimal tutar) => new(0m, Math.Abs(tutar), null);

    /// <summary>
    /// Tek etiketli ödemeden iz üretir: NAKIT → nakit payı, VERESIYE → veresiye
    /// payı, KART/HAVALE/ALTIN → ikisi de 0 (tevsik edilmiş ya da nakit değil).
    /// </summary>
    /// <param name="vade">
    /// VERESIYE etiketinde borç doğuyorsa vade; borç azaltan yönde (iade,
    /// tahsilat) <c>null</c> geçilir.
    /// </param>
    public static OdemeIzi Etiketten(string? odemeTuru, decimal tutar, DateOnly? vade) => odemeTuru switch
    {
        "NAKIT"    => Nakitten(tutar),
        "VERESIYE" => vade is { } v ? Veresiyeden(tutar, v) : VeresiyeKapanisi(tutar),
        _          => Yok,
    };
}

/// <summary>
/// <c>hareketler</c> defterine yazmanın TEK KAPISI.
///
/// ⚠ NEDEN VAR: Nakit payı (<see cref="Hareket.NakitTutar"/>) 459 s. VUK
/// tevsik kümülatifinin kaynağıdır. "Her yazma noktası bu alanı doldurmalı"
/// kuralı bir âdet olarak bırakılırsa, eklenen 12'nci yazma noktası onu
/// unutur ve had sessizce delinir — kimse fark etmez. Bu sınıf kuralı
/// âdetten DERLEYİCİ ZORLAMASINA çevirir: <see cref="OdemeIzi"/> zorunlu
/// konumsal parametredir, varsayılanı yoktur; izi vermeyen kod DERLENMEZ.
///
/// <c>denetim.sh</c> ayrıca <c>Hareketler.Add</c> çağrısının kod tabanında
/// YALNIZ burada olduğunu sayar — fabrikayı atlayan yazma CI'da yakalanır.
/// </summary>
public static class HareketFabrikasi
{
    /// <summary>
    /// Taslağı ödeme iziyle damgalar, değişmezleri doğrular ve deftere ekler.
    /// Çağıran taslakta <see cref="Hareket.NakitTutar"/> /
    /// <see cref="Hareket.VeresiyeTutar"/> DOLDURMAZ — bunları yalnız iz belirler.
    /// </summary>
    public static Hareket Ekle(KuyumcuDbContext db, OdemeIzi izi, Hareket taslak)
    {
        // ── P-08 / Y-32 — TÜR KAPISI ────────────────────────────────────────
        // DEĞİŞMEZ: hareketler.tur KAPALI bir listedir.
        //
        // ⚠ NEDEN KAPI, NEDEN SADECE BELGE DEĞİL: Sabitler.HareketTurleri bugüne
        // dek yalnız BELGE değeri taşıyordu ("bu dizi kodda ZORLANMAZ" —
        // Entities.cs). Türü DIŞARIDAN gelen üç yazma noktası vardır
        // (AcilisServisi `Tur = tur`, StornoServisi `Tur = kaynak.Tur`,
        // IsCalistirici `Tur = taslak.Tur`); listede olmayan bir tür bunlardan
        // sessizce deftere düşerse rapor/denetim tarafı onu "geçersiz tür" sanıp
        // SÜZER ve ciro eksik çıkar — kimse fark etmez. Artık 400 döner.
        //
        // ⚠ 500 DEĞİL 400: geçersiz tür bir sunucu çöküşü değil, reddedilen bir
        // istektir; çağıran sebebi okuyabilmelidir (UX-S30).
        if (string.IsNullOrWhiteSpace(taslak.Tur) || !Array.Exists(Sabitler.HareketTurleri, t => t == taslak.Tur))
            throw new BadRequestException(
                $"Tanımsız hareket türü: '{taslak.Tur}'. Geçerli türler: {string.Join(", ", Sabitler.HareketTurleri)}.");

        // ── P-08 / Y-14 — ADET KAPISI ───────────────────────────────────────
        // Adet İŞARETSİZDİR; yön TlTutari ve HasGrami kolonlarındadır. Negatif
        // adet yazılabilseydi "iade" iki kez (hem tutarda hem adette) ters
        // işaretlenir ve adet toplamları sıfırlanırdı.
        // ⚠ null KABUL EDİLİR ve "0" ile DOLDURULMAZ: null = BİLİNMİYOR.
        if (taslak.Adet is { } adet && adet < 0m)
            throw new BadRequestException(
                $"Hareket adedi negatif olamaz: {adet} ({taslak.Tur}). Yön TlTutari/HasGrami kolonlarındadır.");

        // ── P-08 / Y-13 — PERSONEL (SATAN) KAPISI ───────────────────────────
        // Boş dizge null DEĞİLDİR: "" yazılsaydı "satanı seçilmiş ama kimliği
        // boş" diye üçüncü bir hâl doğar ve rapor onu "bilinmiyor" saymazdı.
        if (taslak.PersonelId is not null && string.IsNullOrWhiteSpace(taslak.PersonelId))
            throw new BadRequestException(
                $"PersonelId boş dizge olamaz; satan bilinmiyorsa null bırakılır ({taslak.Tur}).");

        if (izi.Nakit < 0m || izi.Veresiye < 0m)
            throw new InvalidOperationException(
                $"Ödeme izi işaretsiz olmalıdır: nakit={izi.Nakit}, veresiye={izi.Veresiye} ({taslak.Tur}).");

        if (taslak.NakitTutar is not null || taslak.VeresiyeTutar is not null)
            throw new InvalidOperationException(
                $"NakitTutar/VeresiyeTutar taslakta doldurulmaz; yalnız OdemeIzi belirler ({taslak.Tur}).");

        // DEĞİŞMEZ: nakit + veresiye, hareketin TL bacağını aşamaz. Aşıyorsa ya
        // işaret ya yuvarlama ya da çift sayma hatası vardır — sessiz geçilmez.
        // (0,005 payı: TL bacağı ve paylar ayrı ayrı 2 haneye yuvarlanır.)
        var toplamPay = izi.Nakit + izi.Veresiye;
        if (toplamPay > Math.Abs(taslak.TlTutari) + 0.005m)
            throw new InvalidOperationException(
                $"Ödeme izi TL bacağını aşıyor: nakit {izi.Nakit} + veresiye {izi.Veresiye} " +
                $"> |{taslak.TlTutari}| ({taslak.Tur}).");

        // DEĞİŞMEZ: vade yalnız veresiye payı olan harekette anlamlıdır. Payı
        // sıfır olan satıra vade yazılsaydı, vade takibi raporu ödeme doğurmayan
        // bir satırı "geciken borç" sayardı.
        if (izi.Vade is not null && izi.Veresiye <= 0m)
            throw new InvalidOperationException(
                $"Vade yalnız veresiye payı olan harekette olabilir: veresiye={izi.Veresiye}, vade={izi.Vade} ({taslak.Tur}).");

        if (taslak.VadeTarihi is not null)
            throw new InvalidOperationException(
                $"VadeTarihi taslakta doldurulmaz; yalnız OdemeIzi belirler ({taslak.Tur}).");

        // DEĞİŞMEZ (K27-E, paket 27): DÖVİZ satırı TL BORCU DEĞİLDİR.
        //
        // ⚠ NEDEN KAPI, NEDEN SADECE TEST DEĞİL (Bertan'ın birleşik koşulu):
        // döviz satırının TlTutari'si doludur (MASAK/tevsik TL üzerinden işler).
        // Veresiye payı da yazılsaydı ya da türü "TAHSILAT" olsaydı, aynı borç
        // HEM döviz kolonunda HEM VadeKapisi.AcikBorclar'ın TL defterinde
        // sayılırdı. Bir sonraki paketi yazan kişi bu kuralı bilmeyebilir;
        // âdet değil, DERLEME SONRASI KAPI olarak duruyor.
        if (Modules.Cari.DovizBakiye.DovizTuru(taslak.Tur) && izi.Veresiye > 0m)
            throw new InvalidOperationException(
                $"Döviz hareketinde veresiye payı olamaz (K27-E): veresiye={izi.Veresiye} ({taslak.Tur}).");

        // DEĞİŞMEZ (K20-1, paket 20): has bacağı olan satır GEÇERLİ bir maden
        // taşır. Bu çağrı SADECE burada vardır — deftere yazan tek yol burası
        // olduğu için maden işaretinin boş kalması imkânsızdır (denetim.sh sayar).
        Modules.Urun.MadenKapisi.HareketDogrula(taslak);

        taslak.NakitTutar    = Math.Round(izi.Nakit, 2, MidpointRounding.AwayFromZero);
        taslak.VeresiyeTutar = Math.Round(izi.Veresiye, 2, MidpointRounding.AwayFromZero);
        taslak.VadeTarihi    = izi.Vade;

        db.Hareketler.Add(taslak);

        // ── PAKET P-23c (R-17/R-18 "tümü" dalı) — ÖDEME KALEMİ KAPISI ───────
        // DEĞİŞMEZ: ödeme yazan yol kalem de yazar (ODEME_KALEMSIZ_YOL = 0).
        //
        // ⚠ NEDEN BURADA, NEDEN HER MODÜLDE DEĞİL: Faz 1/2'de kalemi yalnız
        // satış sepeti yazıyordu; tahsilat/takas/işçilik/evrak/alış yolları
        // parayı deftere yazıp kırılımı KAYITSIZ bırakıyordu. "Her yazma
        // noktası kalem de yazsın" bir ÂDET olarak bırakılsaydı, eklenen bir
        // sonraki yol onu unutur ve kırılım sessizce eksilirdi — tıpkı
        // NakitTutar'ın bu sınıfı doğuran hikâyesi gibi. Kapı burada olduğunda
        // unutmak İMKÂNSIZDIR: deftere yazan tek yol burasıdır.
        if (izi.Kalem == KalemYazari.Fabrika)
            OdemeKalemFabrikasi.Otomatik(db, taslak, izi);

        return taslak;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-19 (Y-11) — İKİ BACAKLI ATOMİK DESEN
//
// ⚠ NEDEN AYRI BİR FABRİKA: virman (kasa↔kasa, kasa↔banka, banka↔banka) TEK bir
// olaydır ama İKİ satır doğurur. "Önce çıkışı yaz, sonra girişi yaz" âdet olarak
// bırakılsaydı, araya giren bir erken dönüş / istisna / unutulmuş ikinci Add
// paranın YARISINI buharlaştırırdı: kaynak kasa azalır, hedef kasa artmazdı.
// Kimse fark etmezdi — iki kasa ayrı ekranlarda okunuyor.
//
// Bu sınıf kuralı âdetten DERLEYİCİ + SAYAÇ zorlamasına çevirir:
//   • İki taslak TEK çağrıda, TEK parametre listesinde verilir.
//   • `karsi_bacak_id` KARŞILIKLI burada bağlanır (A.karsi=B.id, B.karsi=A.id).
//   • Her iki satırın `Add`'i de burada yapılır — çağıran birini unutamaz.
//   • denetim.sh ESSIZ_BACAK: `KarsiBacakId =` ataması bu dosya DIŞINDA 0.
//   • denetim.sh TEK_SAVECHANGES_DISI_CIFT_BACAK: `BacaklariEsle` çağıran her
//     metotta TAM 1 adet SaveChangesAsync olur (iki bacak TEK transaction).
//
// ⚠ `karsi_sube_id` İLE İLİŞKİSİ (plan DoD — anlam ayrımı yazılı):
//   karsi_sube_id  = MAL hareketinin karşı ŞUBESİ, `hareketler` tablosunda,
//                    TEK satır (TRANSFER ikinci satır doğurmaz).
//   karsi_bacak_id = PARA hareketinin karşı SATIRI, kasa/banka hareketlerinde,
//                    DAİMA ÇİFT. İkisi farklı tablolarda, farklı türde
//                    (şube kimliği ↔ satır kimliği) referanstır; biri diğerinin
//                    yerine kullanılamaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Virman bacağı olabilen para satırı (kasa hareketi · banka dekontu).</summary>
public interface IBacakliHareket
{
    string Id { get; }
    string? KarsiBacakId { get; set; }
}

public static class CiftBacakFabrikasi
{
    /// <summary>
    /// Virmanın İKİ bacağını karşılıklı bağlar ve İKİSİNİ BİRDEN deftere ekler.
    /// Çağıran <c>KarsiBacakId</c> DOLDURMAZ ve <c>Add</c> ÇAĞIRMAZ.
    /// </summary>
    public static void BacaklariEsle(KuyumcuDbContext db, IBacakliHareket cikis, IBacakliHareket giris)
    {
        if (ReferenceEquals(cikis, giris))
            throw new InvalidOperationException("Virmanın iki bacağı aynı satır olamaz.");

        if (string.IsNullOrWhiteSpace(cikis.Id) || string.IsNullOrWhiteSpace(giris.Id))
            throw new InvalidOperationException("Virman bacaklarının kimliği çağırandan ÖNCE atanmış olmalıdır.");

        if (cikis.Id == giris.Id)
            throw new InvalidOperationException($"Virmanın iki bacağı aynı kimliği taşıyamaz: {cikis.Id}.");

        if (cikis.KarsiBacakId is not null || giris.KarsiBacakId is not null)
            throw new InvalidOperationException(
                "Virman bacağının karşı bacağı çağıran tarafından doldurulmaz; eşleştirmeyi yalnız fabrika yapar.");

        cikis.KarsiBacakId = giris.Id;
        giris.KarsiBacakId = cikis.Id;

        Ekle(db, cikis);
        Ekle(db, giris);
    }

    private static void Ekle(KuyumcuDbContext db, IBacakliHareket bacak)
    {
        switch (bacak)
        {
            case KasaHareket k:  db.KasaHareketleri.Add(k); break;
            case BankaHareket b: db.BankaHareketleri.Add(b); break;
            default: throw new InvalidOperationException(
                $"Bilinmeyen virman bacağı türü: {bacak.GetType().Name}. Yeni bacak türü buraya YAZILIR.");
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20b (Y-10 kalan dilim · K-13) — TAHLİL / MUTABAKAT FARKI FABRİKASI
//
// ⚠ NEDEN AYRI BİR FABRİKA (P-19'un CiftBacakFabrikasi deseni):
//   Hurda satışında alıcının (rafineri/toptancı) KABUL ETTİĞİ has ile bizim
//   KAYDETTİĞİMİZ has farklı çıkar. Fark yazılmazsa HAS-CARİ DENK GELMEZ:
//   ya stoktan eksik düşer ya da cariye kabul edilmeyen has borçlanılır.
//   Fark BAĞIMSIZ bir olay DEĞİLDİR — daima bir HURDA_SATIS satırının
//   mutabakatıdır. "Önce satışı yaz, sonra farkı yaz" âdet olarak bırakılsaydı
//   araya giren bir erken dönüş / istisna / unutulmuş ikinci Add, has
//   defterinin YARISINI yazar ve bakiye sessizce kayardı.
//
//   Bu sınıf kuralı âdetten DERLEYİCİ + SAYAÇ zorlamasına çevirir:
//     • Fark bacağı KARŞI BACAĞI (kaynak HURDA_SATIS satırı) verilmeden
//       yazılamaz — parametre zorunludur, varsayılanı yoktur.
//     • `tahlil_kaynak_hareket_id` YALNIZ burada atanır.
//     • Gerekçe ZORUNLUDUR (K-13 şartı): 20 karakterin altı reddedilir.
//     • Deftere ekleme HareketFabrikasi.Ekle ile yapılır — ikinci yazma yolu yok.
//   denetim.sh: TAHLIL_FARKI_ESSIZ_BACAK = 0 (fabrika dışı atama / tür yazımı
//   yok) ve fabrikayı çağıran her metotta TAM 1 SaveChangesAsync.
//
// ⚠ BU FİRE DEĞİLDİR (K-13/1): eritme firesi imalat/atölye kapsamındadır ve
//   1.0'a GİRMEZ. Tahlil farkı bir ÖLÇÜM mutabakatsızlığıdır. İki kavram
//   birbirinin yerine kullanılamaz; adlandırma denetim.sh HURDA_FIRE_SIZINTISI
//   sayacıyla korunur.
// ═════════════════════════════════════════════════════════════════════════════

public static class TahlilFarkiFabrikasi
{
    /// <summary>Fark bacağının hareket türü — dizge TEK yerde durur.</summary>
    public const string Tur = "TAHLIL_FARKI";

    /// <summary>
    /// Farkı doğuran satış satırının türü — kapı bunu doğrular.
    /// ⚠ Ad bilerek <c>KaynakHareketTuru</c>'dur, <c>KaynakTur</c> DEĞİL:
    /// denetim.sh P-20 HURDA_STOK_KACAK_YOL sayacı hurda satışı türünü ATAYAN
    /// kalıbı arar; kısa ad bu SABİT TANIMINI bir YAZMA sanıp sayacı
    /// yanıltırdı (sayaç zayıflatılmadı, isim düzeltildi).
    /// </summary>
    public const string KaynakHareketTuru = "HURDA_SATIS";

    /// <summary>
    /// Gerekçenin asgari uzunluğu. Evin standardı 20'dir (StornoDto,
    /// CariDuzeltmeDto, AcilisDto…); tahlil farkı da aynı ölçüyü taşır ki
    /// "fark" gerekçesi "ok" diye geçiştirilemesin.
    /// </summary>
    public const int GerekceAsgari = 20;

    /// <summary>
    /// TAHLİL FARKI BACAĞINI YAZAR — tek kapı.
    /// </summary>
    /// <param name="kaynak">
    /// Farkı doğuran <c>HURDA_SATIS</c> satırı. Aynı işlemde yeni yazılan satır
    /// da olabilir, düzeltmede veritabanından okunan satır da; her iki hâlde de
    /// fark bacağı ONUN kimliğine bağlanır.
    /// </param>
    /// <param name="farkHasGrami">
    /// İŞARETLİ fark = <c>alıcının kabul ettiği has − bizim kaydımız has</c>.
    /// Negatif = ALEYHTE (alıcı daha az kabul etti, has defterden ek olarak
    /// çıkar), pozitif = LEHTE. 0 kabul EDİLMEZ: farkı olmayan bir fark bacağı
    /// deftere gürültüden başka bir şey yazmaz.
    /// </param>
    public static Hareket Ekle(
        KuyumcuDbContext db, Hareket kaynak, decimal farkHasGrami, string? gerekce,
        string kullaniciId, DateTime an)
    {
        if (kaynak is null)
            throw new InvalidOperationException(
                "Tahlil farkı bacağı karşı bacağı (hurda satışı) olmadan yazılamaz.");

        if (kaynak.Tur != KaynakHareketTuru)
            throw new InvalidOperationException(
                $"Tahlil farkının karşı bacağı yalnız {KaynakHareketTuru} olabilir; gelen: '{kaynak.Tur}'.");

        if (string.IsNullOrWhiteSpace(kaynak.Id))
            throw new InvalidOperationException(
                "Tahlil farkının karşı bacağının kimliği çağırandan ÖNCE atanmış olmalıdır.");

        var fark = Math.Round(farkHasGrami, 6, MidpointRounding.AwayFromZero);
        if (fark == 0m)
            throw new BadRequestException(
                "Tahlil farkı sıfır olamaz: fark yoksa ayrı bacak YAZILMAZ.");

        var g = (gerekce ?? string.Empty).Trim();
        if (g.Length < GerekceAsgari)
            throw new BadRequestException(
                $"Tahlil farkı gerekçesi zorunludur ve en az {GerekceAsgari} karakter olmalıdır "
                + $"(gelen: {g.Length}). Farkın nereden geldiği (rafineri tahlil raporu, "
                + "mutabakat tutanağı) yazılmadan has defteri denetlenemez.");

        return HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id                    = Kimlik.Ulid(),
            TenantId              = kaynak.TenantId,
            SubeId                = kaynak.SubeId,
            Tur                   = Tur,
            Tarih                 = an,
            // ⚠ TL BACAĞI DAİMA 0: fark para değil HAS'tır. TL yazılsaydı aynı
            //   tutar hem satış hem fark satırında sayılır, ciro şişerdi.
            TlTutari              = 0m,
            HasGrami              = fark,
            Maden                 = kaynak.Maden,
            // Para el değiştirmez (OdemeIzi.Yok ile paritedir).
            OdemeTuru             = null,
            KurSnapshotId         = kaynak.KurSnapshotId,
            CariId                = kaynak.CariId,
            TahlilKaynakHareketId = kaynak.Id,
            Aciklama              = g,
            KullaniciId           = kullaniciId,
            Olusturuldu           = an,
        });
    }
}
