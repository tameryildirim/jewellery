using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.EntityFrameworkCore;
using GD = Kuyumcu.Api.Common.Dogrulama;   // PAKET GOC-B
using GS = Kuyumcu.Core.Domain.Girdi.AlanSinirlari;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK KAPISI — işlem akışının tek giriş noktası. SIRADAKI-ADIM-KARAR §2/A.
//
// İki fazlı (Bertan, mühürlendi):
//   FAZ 1  Dogrula(tutar, bilgi)  → transaction AÇILMADAN önce, IslemDogrulayici
//          çağrılarının yanında. Eksik varsa BadRequestException(TÜM eksikler);
//          işlem hiç başlamaz. FAIL-CLOSED.
//   FAZ 2  Kaydet(...)            → transaction İÇİNDE, hareket Add'lendikten sonra,
//          SaveChanges'ten önce. Tespit ile hareket AYNI commit'te — denetimde
//          "hareket var, tespit yok" ara durumu doğmaz.
//
// Kapıya tabi akışlar: Satis, Alis, HurdaAlis, Takas, Iscilik, VeresiyeGir,
// TahsilatGir. Transfer değil (karşı taraf yok). Emanet ve elle kasa → K2 sorusu.
//
// TAKAS TUZAĞI: kapıya `farkTl` DEĞİL `Max(eskiDeger, yeniDeger)` verilir.
// 900.000'lik eski altınla 910.000'lik ürün takasında fark 10.000'dir ama el
// değiştiren değer 1,8 M — fark verilirse eşik hiç tetiklenmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// İşlem DTO'larına gömülen tek, OPSİYONEL MASAK bloğu. Eşik altında boş kalır.
/// Zorunlu alan yok — mevcut testlerin derlemesi kırılmaz (Aşkın şartı).
/// </summary>
public sealed class MasakBilgiDto
{
    /// <summary>
    /// Kimlik belgesi türü. UX-4d madde 3: DTO'da düzenli ifade YOKTU, geçersiz
    /// değer ancak kural motorunda yakalanıyordu ve mesaj kabul edilen değerleri
    /// söylemiyordu. Desen TEK KAYNAKtan gelir (Kuyumcu.Core KimlikBelgesi).
    /// </summary>
    [MaxLength(20)]
    [RegularExpression(Kuyumcu.Core.Domain.KimlikBelgesi.Desen,
        ErrorMessage = "Kimlik belgesi türü geçersiz — kabul edilen değerler: "
                     + Kuyumcu.Core.Domain.KimlikBelgesi.Desen)]
    public string? BelgeTuru { get; set; }
    /// <summary>TCKN/pasaport no. Diske ŞİFRELİ iner; bellekte düz metin.</summary>
    // PAKET GOC-B (envanter #3): ekran sınırı = DTO sınırı, TEK sabit (ekran 60/200 idi → 400 ile düşüyordu).
    [MaxLength(GS.MasakBelgeNo)] public string? BelgeNo { get; set; }
    /// <summary>K19 — en az 2 karakter, en az bir harf ("1" MASAK kaydına ad soyad olarak geçiyordu).</summary>
    [MaxLength(GS.MasakAdSoyad), GD.Ad(GS.MasakAdSoyad)] public string? AdSoyad { get; set; }
    /// <summary>K8f — gelecek ya da 120 yıldan eski doğum tarihi 400 (Tedbirler Yön. md.6). Tur 2: 18 yaş altı ENGEL DEĞİL (TMK md.11 yasaklamaz; TEYİTSİZ → paket öncesi davranış).</summary>
    [GD.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.KisiDogum)] public DateOnly? DogumTarihi { get; set; }
    [MaxLength(GS.UyrukKod)] public string? Uyruk { get; set; }
    [MaxLength(GS.Adres)] public string? Adres { get; set; }

    [MaxLength(GS.MasakIslemAmaci)] public string? IslemAmaci { get; set; }
    [MaxLength(GS.MasakAciklama)] public string? Aciklama { get; set; }
    [MaxLength(GS.MasakFonKaynagi)] public string? FonKaynagi { get; set; }
    [MaxLength(GS.MasakFonKaynagi)] public string? EkBelgeReferansi { get; set; }

    /// <summary>
    /// PAKET YASAL-3 (§ 4.2) — Peşin Müşteri ART ARDA fişte tezgâhtarın tek cevabı:
    /// "az önceki müşteriyle aynı kişi mi?". Yalnız sunucu PESIN_ART_ARDA_SORU ile
    /// sorduğunda gönderilir; kimlik bloğu DEĞİLDİR (Peşin kartında kimlik yazılmaz).
    /// </summary>
    public bool? AyniKisiBeyani { get; set; }
}

public sealed class MasakKapisi(
    MasakKuralMotoru motor, Modules.Tevsik.TevsikEsikleri? tevsikEsikleri = null)
{
    /// <summary>
    /// PAKET B-3 (D2 · K2): Peşin Müşteri kartının fail-closed sınırı tevsik
    /// haddini de görür. Çağıran yapılandırılmış eşikleri vermezse VARSAYILAN
    /// eşikler kullanılır — "eşik yoksa kontrol yok" YAZILMAZ.
    /// </summary>
    private readonly Modules.Tevsik.TevsikEsikleri tevsik = tevsikEsikleri ?? new Modules.Tevsik.TevsikEsikleri();

    /// <summary>
    /// DI'sız kurulumlar (testlerdeki `new SatisServisi(db, logger)`) için VARSAYILAN
    /// eşiklerle motor. "Motor yoksa kontrol yok" YAZILMAZ — bu bir güvenlik deliği olurdu.
    /// </summary>
    public static MasakKapisi Varsayilan() => new(new MasakKuralMotoru(new MasakEsikleri()));

    /// <summary>
    /// PAKET GOC-B — kimlik tespiti alanlarının girdi kuralı (K8f doğum, K19 ad soyad, K7 pasaport biçimi,
    /// ekran = sunucu uzunluk sınırları). Eksiklik (zorunluluk) MasakKuralMotoru'nundur; burası BİÇİMDİR.
    /// </summary>
    public static void KimlikBilgisiDenetle(MasakBilgiDto b)
    {
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.TarihPenceresi.Denetle(
            b.DogumTarihi, Kuyumcu.Core.Domain.Girdi.TarihTuru.KisiDogum, Zaman.YerelBugun()), "dogumTarihi");
        GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AdKurali.Denetle(b.AdSoyad, GS.MasakAdSoyad), "adSoyad");
        if (b.BelgeTuru == KimlikBelgeTuru.Pasaport)
            GD.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.PasaportKurali.Denetle(b.BelgeNo), "belgeNo");
        GD.GirdiKapisi.Uzunluk(b.BelgeNo, GS.MasakBelgeNo, "belgeNo");
        GD.GirdiKapisi.Uzunluk(b.IslemAmaci, GS.MasakIslemAmaci, "islemAmaci");
        GD.GirdiKapisi.Uzunluk(b.Adres, GS.Adres, "adres");
        GD.GirdiKapisi.Uzunluk(b.Aciklama, GS.MasakAciklama, "aciklama");
        GD.GirdiKapisi.Uzunluk(b.FonKaynagi, GS.MasakFonKaynagi, "fonKaynagi");
        GD.GirdiKapisi.Uzunluk(b.EkBelgeReferansi, GS.MasakFonKaynagi, "ekBelgeReferansi");
    }

    /// <summary>
    /// FAZ 1. Eşik altındaysa sessizce geçer (kademe YOK). Eşik üstünde eksik varsa
    /// TÜM eksikleri tek seferde fırlatır — kuyumcu tek tek denemesin.
    /// </summary>
    public MasakGereksinim Dogrula(
        decimal tutarTl, MasakBilgiDto? bilgi, decimal? kumulatifTutarTl = null, DateOnly? esikGunu = null)
    {
        // Eşik KÜMÜLATİF tutar üzerinden değerlendirilir (verilmişse); kayda
        // geçen ve müşteriye gösterilen tutar yine bu işlemin kendi tutarıdır.
        var olcut = kumulatifTutarTl ?? tutarTl;

        // PAKET YASAL-PARAM-EKRAN (MASAK-370 hasım DÜŞÜK): eşik İŞLEM GÜNÜNÜN değeridir.
        // Çağıran işlem gününü verirse (geç eşleştirilen dekont: dekontun oluşturulduğu
        // gün) o günün eşikleri okunur; verilmezse işlem şimdi yapılıyordur → bugün.
        var e = esikGunu is { } gun ? motor.EsiklerGunu(gun) : motor.Esikler;
        var gerek = esikGunu is { } g1 ? motor.Gereksinim(olcut, g1) : motor.Gereksinim(olcut);
        if (gerek.Kademe == BeyanKademesi.Yok) return gerek;

        // ZO-ISLEM-A 28.09 (doğrulayıcı DÜŞÜK): uyruk kodu denetimi yalnız istemcideydi.
        // Aynı tek kaynak (Kuyumcu.Core UlkeListesi.KodGecerli — ISO 3166-1 alpha-3 +
        // pasaport kodları) sunucuda da uygulanır; boş uyruk bu kapının konusu değildir.
        // Mevzuat hükmü DEĞİL, yazım denetimi (yanlış kod deftere yazılmaz).
        if (!Kuyumcu.Core.I18n.UlkeListesi.KodGecerli(bilgi?.Uyruk))
            throw new BadRequestException(
                $"MASAK: uyruk kodu geçersiz ('{bilgi!.Uyruk!.Trim().ToUpperInvariant()}'); pasaporttaki 3 harfli ülke kodunu yazın (örnek: FRA).");

        // PAKET TCKN-DOGRULAMA (08.10, kurucu emri): belge türü T.C. Kimlik ise belge no TC kimlik no
        // doğrulama kuralından geçmeli (tek kaynak Kuyumcu.Core KimlikNoDogrulayici). Pasaport, sürücü
        // belgesi, mavi kart, vergi levhası bu kurala TABİ DEĞİL (belge türüne göre).
        if (bilgi?.BelgeTuru == KimlikBelgeTuru.TcKimlik && !string.IsNullOrWhiteSpace(bilgi.BelgeNo)
            && !Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecerli(bilgi.BelgeNo.Trim()))
            throw new BadRequestException("MASAK: " + Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecersizMesaj)
                .AlanIle("belgeNo");


        var girdi = new MasakGirdi
        {
            IslemTutariTl = olcut,
            KimlikTespitiVar = bilgi is not null,
            BelgeTuru = bilgi?.BelgeTuru,
            BelgeNo = bilgi?.BelgeNo,
            AdSoyad = bilgi?.AdSoyad,
            IslemAmaci = bilgi?.IslemAmaci,
            Aciklama = bilgi?.Aciklama,
            FonKaynagi = bilgi?.FonKaynagi,
            EkBelgeReferansi = bilgi?.EkBelgeReferansi,
        };
        var sonuc = esikGunu is { } g2 ? motor.Dogrula(girdi, g2) : motor.Dogrula(girdi);

        if (!sonuc.Uygun)
        {
            // UX-4d madde 4: mesaj biçimi TEK. Eskiden kümülatif notu yalnız
            // "toplam > bu işlem" olduğunda basılıyordu; aynı kuyumcu aynı hatayı
            // ilk denemesinde notsuz, ikinci denemesinde notlu görüyordu ve
            // eşiğin neye göre hesaplandığını anlayamıyordu (QA turu 1).
            // Artık kümülatif ölçü verildiğinde HER ZAMAN aynı cümle yazılır.
            // PAKET YASAL-PARAM-EKRAN (MASAK-370 hasım DÜŞÜK): nottaki eşik ana cümleyle AYNI
            // kuralla seçilir — toplam md.5/1-b kimlik eşiğine ulaştıysa O eşik; yalnız amaç
            // beyanı kademesindeyse kimliği isteyen en düşük tutar. İki farklı eşik yan yana yazılmaz.
            var kumulatifNotu = kumulatifTutarTl is { } kt
                ? $" (Bu cariyle bugün kümülatif {kt:N2} TL — eşik "
                  + $"{(Math.Abs(kt) >= e.KimlikEsigi ? e.KimlikEsigi : e.KimlikIstenenEnDusukTutar):N0} TL; "
                  + $"bu işlem {Math.Abs(tutarTl):N2} TL)"
                : "";
            throw new BadRequestException(
                "MASAK: " + string.Join(" · ", sonuc.Eksikler) + kumulatifNotu);
        }

        // PAKET GOC-B (envanter #2/#3, C satır 48–52): eksikler (zorunluluk) yukarıda TEK seferde söylendi;
        // şimdi dolu alanların BİÇİMİ. DataAnnotations yalnız controller yolunda koşar; MASAK kapısı servis
        // yolundan da (satış, takas, hurda, dekont) çağrılır → AYNI Core kuralı burada elle uygulanır.
        // Metin GirdiSozlugu (ekranın alan altı metniyle aynı anahtar).
        if (bilgi is not null) KimlikBilgisiDenetle(bilgi);

        return gerek;
    }

    /// <summary>
    /// FAZ 1 — BAĞLANTILI İŞLEM kümülatifiyle.
    ///
    /// ⚠ NEDEN VAR: Tedbirler Yönetmeliği m.5/1-(b) eşiği "işlem tutarı YA DA
    /// birbiriyle bağlantılı birden fazla işlemin TOPLAM tutarı" üzerinden
    /// kurar. Kapı bugüne kadar çağrı BAŞINA bakıyordu: aynı müşteriye aynı gün
    /// 3 ayrı fişle 150.000'er TL satıldığında hiçbiri o günkü eşiğe (185.000) değmiyor ve
    /// kimlik tespiti HİÇ tetiklenmiyordu — eşik bölme (structuring) tam olarak
    /// budur. (Mevzuat araştırması 2026-09-15, 8.5/10, iki mercek de doğruladı.)
    ///
    /// ÖLÇÜ (PAKET YASAL-3 § 4.1): kümülatif, satırda EL DEĞİŞTİREN değeri okur —
    ///   TAKAS ve HURDA_ALIS satırlarında <c>masak_olcu_tl</c> (Max(eski, yeni) /
    ///   brüt bedel), diğerlerinde defterin TL bacağı. Kolondan ÖNCE yazılmış TAKAS
    ///   satırlarının brüt değeri defterde yoktur → NULL kalır ve fark toplanır
    ///   (geçmişe dönük, uydurulmaz — rapor: PAKET-YASAL-3 § migration).
    /// ⚠ BİLİNEN EKSİK ÖLÇÜM (bilinçli, yalnız bu kalır):
    ///   • <b>EVRAK GİRİŞİ KÜMÜLATİFE GİRMEZ (K4-D, paket 29 Faz A).</b>
    ///     Müşteriden alınan çek/senet (<c>EVRAK_GIRIS</c>) bu listede YOKTUR:
    ///     o satırın altında zaten kümülatife sayılmış bir SATIS/VERESIYE
    ///     vardır ve evrak yalnız o borcu kapatır — para EL DEĞİŞTİRMEZ.
    ///     Sayılsaydı aynı işlem iki kez toplanırdı. ⚠ Bu bir MEVZUAT YORUMU
    ///     DEĞİL, ölçüm doğruluğu kararıdır; gevşek yönde olduğu için burada ve
    ///     uyum belgesinde açıkça yazılıdır.
    ///     ⚠ GEVŞEME YALNIZ GİRİŞE AİTTİR (Mehmet parite şartı): evrakın
    ///     nakit/banka TAHSİL bacağı mevcut <c>TAHSILAT</c> türüyle yazılır ve
    ///     <c>EVRAK_KARSILIKSIZ</c> aşağıdaki listededir — ikisi de
    ///     TAHSILAT/VERESIYE ile AYNI ölçülür.
    /// Hepsi sapmayı GEVŞEK yönde yapar ve burada açıkça yazılıdır.
    ///
    /// ⚠ <b>PEŞİN MÜŞTERİ KÜMÜLATİFE GİRMEZ (PAKET B-3 · karar D2 K1/K5).</b>
    ///   Kiracının sistem carisi ("Peşin Müşteri") bir kişi değil, kimliksiz
    ///   perakende satışların ortak kartıdır; satırları <see cref="GunlukIslemToplam"/>
    ///   havuzuna GİRMEZ. Bu GEVŞEK yönde bir ölçüm kararıdır ve burada açıkça
    ///   yazılıdır. Karşılığı FAIL-CLOSED'dur: bu kartla yapılan işlem tek başına
    ///   MASAK kimlik eşiğine ya da tevsik haddine ulaşırsa işlem REDDEDİLİR
    ///   ("gerçek müşteri seçin") — kimlik bloğuyla bile geçmez. Böylece kart
    ///   yalnız EŞİK ALTI şerittir; eşik bölme bu kart üzerinden kümülatifle
    ///   değil, her işlemin tek başına eşik altında kalma şartıyla sınırlanır.
    ///   ⚠ KALAN RİSK (açıkça): aynı kişi aynı gün bu kartla eşik ALTI birden çok
    ///   işlem yaparsa bağlantılı işlem toplamı ölçülmez — kişi kayıt altında
    ///   değildir (cariId null ile aynı sınır). Rapor: PAKET-B-3 § uyum kaydı.
    /// </summary>
    public async Task<MasakGereksinim> DogrulaKumulatif(
        KuyumcuDbContext db, string tenantId, string? cariId,
        decimal tutarTl, MasakBilgiDto? bilgi, DateTime an,
        AktifKullanici? islem = null, string? subeId = null, DateOnly? esikGunu = null)
    {
        // ── PAKET YASAL-3 (§ 4.4) — YAPTIRIM: kilitli cariyle işlem YOK (6415 md.10) ──
        await YaptirimServisi.CariKilidiDenetle(db, tenantId, cariId);

        // Müşteri bilinmiyorsa "bağlantılı işlem" kurulamaz; tek işlem kuralı işler.
        if (string.IsNullOrEmpty(cariId))
        {
            var tek = Dogrula(tutarTl, bilgi, esikGunu: esikGunu);
            await KimlikTara(db, tenantId, tek, bilgi, cariId, islem);
            return tek;
        }

        // ── PAKET B-3 (D2 · K1 + K2) — PEŞİN MÜŞTERİ: EŞİK ALTI ŞERİT ──────────
        // K1: bu kartın satırları kümülatife GİRMEZ (günün bütün kimliksiz
        //     müşterileri TEK kişi sayılmasın). K2: açılan boşluk fail-closed
        //     kapanır — bu işlem tek başına MASAK kimlik eşiğine YA DA tevsik
        //     haddine ulaşıyorsa işlem REDDEDİLİR; kimlik bloğu verilmiş olsa
        //     bile (kimlik tespiti gerçek bir cari kartına bağlanır).
        if (await Modules.Cari.SistemCarisi.MiAsync(db, tenantId, cariId))
        {
            var tutar = Math.Abs(tutarTl);
            var gerek = esikGunu is { } pg ? motor.Gereksinim(tutar, pg) : motor.Gereksinim(tutar);
            if (gerek.Kademe != BeyanKademesi.Yok)
                throw new BadRequestException(Modules.Cari.SistemCarisi.EsikMesaji(
                    "MASAK kimlik tespiti", tutar, motor.Esikler.KimlikIstenenEnDusukTutar)).AlanIle("cariId");   // MASAK-370: tetikleyen eşik (amaç beyanı kademesi dahil)   // PAKET S2-3: ekran bandı müşteri alanında
            if (tutar > tevsik.Had)
                throw new BadRequestException(Modules.Cari.SistemCarisi.EsikMesaji(
                    "tevsik (459 s. VUK GT)", tutar, tevsik.Had)).AlanIle("cariId");   // PAKET S2-3: ekran bandı müşteri alanında
            // PAKET YASAL-3 (§ 4.2): eşik ALTI tek işlem — art arda Peşin fişlerde tek soru.
            await PesinPencereKapisi.Denetle(db, tenantId, cariId, islem, subeId, tutar,
                bilgi?.AyniKisiBeyani, an, motor.Esikler.KimlikIstenenEnDusukTutar, tevsik.Had, motor.Esikler.PesinPencereDakika);   // MASAK-370: kimlik isteyen en düşük tutar — 07.10.2026 öncesi davranış (185.000 < 200.000) birebir
            return gerek;
        }

        var onceki = await GunlukIslemToplam(db, tenantId, cariId, an);
        var sonuc = Dogrula(tutarTl, bilgi, Math.Abs(onceki) + Math.Abs(tutarTl), esikGunu);
        await KimlikTara(db, tenantId, sonuc, bilgi, cariId, islem);
        return sonuc;
    }

    /// <summary>
    /// PAKET MASAK-ESZAMAN — FAZ 1'in KİLİT ALTINDA yinelenmesi. Çağıran işlemi açmış ve
    /// cari satırını kilitlemiş olmalıdır (<see cref="YasalKilit.CariIcinBaslat"/>); burada
    /// kilit YOKSA fırlatılır (sessiz geçiş yok). Kural ve eşik <see cref="DogrulaKumulatif"/>
    /// ile BİREBİR aynıdır (aynı <see cref="Dogrula"/>, aynı <see cref="GunlukIslemToplam"/>);
    /// değişen yalnız okumanın ZAMANIdır: eşzamanlı ikinci istek kilidi bekler ve ilk isteğin
    /// commit ettiği satırı toplamda görür → eşiği aşan istek beyansız 400.
    /// Dönen gereksinim çağıranın Faz 2 kaydında (masak.Kaydet) kullanılır.
    /// Peşin Müşteri (sistem carisi) ve carisiz işlem kümülatife girmez → Faz 1 sonucu aynen döner;
    /// Peşin kartında art arda fiş pencere sorusu (PesinPencereKapisi) kilit altında yinelenir (MASAK-ESZAMAN-2).
    /// Kimlik taraması: Faz 1'de kademe YOKKEN burada doğduysa ve kimlik bloğu verilmişse tarama
    /// burada koşar; TAM eşleşmede işlem önce GERİ ALINIR (yazılacak hiçbir şey kalmaz), eşleşme
    /// kaydı + bildirim işlem DIŞINDA kalıcı yazılır, sonra 400 (Faz 1 ile aynı davranış).
    /// </summary>
    public async Task<MasakGereksinim> KilitAltindaYinele(
        KuyumcuDbContext db, string tenantId, string? cariId,
        decimal tutarTl, MasakBilgiDto? bilgi, DateTime an, MasakGereksinim faz1,
        AktifKullanici? islem = null, DateOnly? esikGunu = null)
    {
        if (string.IsNullOrEmpty(cariId)) return faz1;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "MASAK-ESZAMAN: kümülatif yineleme yalnız cari kilidi alınmış açık işlemde koşar (YasalKilit.CariIcinBaslat).");
        if (await Modules.Cari.SistemCarisi.MiAsync(db, tenantId, cariId))
        {
            // PAKET MASAK-ESZAMAN-2: Peşin Müşteri kümülatife girmez (B-3 K1) ama art arda fiş PENCERE
            // sorusu (YASAL-3 § 4.2) bir "kontrol et → yaz" kapısıdır: Faz 1'de kilitsiz okunursa eşzamanlı
            // N Peşin fişin hepsi "önceki işlem yok" görür ve soru hiç sorulmaz. Kart satırı bu noktada
            // kilitli (CariIcinBaslat) → pencere aynı argümanlarla, güncel satırlarla YENİDEN sorulur.
            // Tek işlem eşiği (K2) tutara bağlıdır, zamana değil — Faz 1 kararı aynen geçerli.
            await PesinPencereKapisi.Denetle(db, tenantId, cariId, islem, null, tutarTl,
                bilgi?.AyniKisiBeyani, an, motor.Esikler.KimlikIstenenEnDusukTutar, tevsik.Had,
                motor.Esikler.PesinPencereDakika, kilitAltinda: true);
            return faz1;
        }

        var onceki = await GunlukIslemToplam(db, tenantId, cariId, an);
        var sonuc = Dogrula(tutarTl, bilgi, Math.Abs(onceki) + Math.Abs(tutarTl), esikGunu);

        if (faz1.Kademe == BeyanKademesi.Yok)
            await KimlikTara(db, tenantId, sonuc, bilgi, cariId, islem);   // Faz 1'de taranmadıysa
        return sonuc;
    }

    /// <summary>
    /// PAKET YASAL-3 (§ 4.4 nokta 1): kimlik tespiti İSTENEN işlemde (kademe var ve
    /// blok verilmiş) belge no yasal listede TAM eşleşirse işlem ERTELENİR (400) ve
    /// SAHİP'e acil bildirim gider; yalnız ad benzerliği → işlem geçer + bildirim.
    /// Faz 1'de, Dogrula eksiksiz geçtikten SONRA (eksik formda tarama yapılmaz).
    /// </summary>
    private static async Task KimlikTara(
        KuyumcuDbContext db, string tenantId, MasakGereksinim gerek, MasakBilgiDto? bilgi,
        string? cariId, AktifKullanici? islem)
    {
        if (gerek.Kademe == BeyanKademesi.Yok || bilgi is null) return;
        // PAKET MASAK-ESZAMAN: tarama AÇIK İŞLEM içinde koşuyorsa (kilit altı yineleme ya da sipariş
        // kaporası) TAM eşleşmede işlem ÖNCE geri alınır: eşleşme kaydı + KRİTİK bildirim reddedilen
        // işlemle birlikte kaybolmasın (Faz 1 işlem dışı davranışıyla aynı sonuç — kayıt kalıcı, 400).
        if (db.Database.CurrentTransaction is not null
            && await YaptirimServisi.TamEslesme(db, bilgi.BelgeNo) is not null)
        {
            await db.Database.RollbackTransactionAsync();
            db.ChangeTracker.BekleyenleriGeriAl();
        }
        await YaptirimServisi.KimlikTara(db, tenantId, islem, YaptirimNoktasi.MasakTespit,
            bilgi.BelgeNo, bilgi.AdSoyad, string.IsNullOrEmpty(cariId) ? null : cariId);
    }

    /// <summary>
    /// Aynı YEREL GÜNDE (Europe/Istanbul) aynı cariyle yapılmış, MASAK'a tabi
    /// işlemlerin TL toplamı. Gün sınırı tevsik kapısıyla aynı gerekçeyle
    /// yereldir (K5): UTC kullanılsaydı 00:00–03:00 arası işlemler bir önceki
    /// güne düşer ve toplam eksik hesaplanırdı.
    ///
    /// ⚠ PAKET 41 — İKİ AYRIK SORGU, tek bileşik ifade DEĞİL:
    ///   (1) hareketler defteri (bugünkü sorgu, değişmedi),
    ///   (2) banka_hareketleri'nde <c>hareket_id IS NULL</c> olan elle girilmiş
    ///       dekontlar.
    /// İki toplam C# tarafında toplanır. CASE/UNION/COALESCE bileşimi YAZILMAZ
    /// (A5 mühürlü kararı: bileşik ifadenin MySQL çevrimi doğrulanmadı).
    /// Kümeler KESİŞMEZ: bir dekont bir hareketle eşleştiği gün
    /// <c>hareket_id</c> dolar ve banka tarafından otomatik DÜŞER; aynı para
    /// hareketler tarafında sayılır. Çift sayma yapısal olarak imkânsızdır.
    /// </summary>
    public static async Task<decimal> GunlukIslemToplam(
        KuyumcuDbContext db, string tenantId, string cariId, DateTime an)
    {
        // ⚠ PAKET ŞUBE-KAPSAM-A § 2.6 (K-10): "bağlantılı işlemler toplamı"
        // YÜKÜMLÜ (kiracı) genelidir; şube yükümlünün birimidir (MASAK GT 34
        // md.3/1-c). Şube süzgeci açıksa kiracı-geneli SALT OKUMA kapsamı.
        if (db.SubeFiltresiAcik)
            return await SistemKapsami.YasalKumulatifOku(db, tenantId,
                kdb => GunlukIslemToplam(kdb, tenantId, cariId, an));

        // ⚠ PAKET B-3 (D2 · K1): Peşin Müşteri kartının satırları havuza GİRMEZ.
        // Bu kart bir KİŞİ değildir; günün kimliksiz müşterileri toplanırsa eşik
        // yanlış kişide patlar. Boşluk DogrulaKumulatif'te fail-closed kapanır (K2).
        if (await Modules.Cari.SistemCarisi.MiAsync(db, tenantId, cariId)) return 0m;

        var yerelGun = DateOnly.FromDateTime(Zaman.UtcdenYerele(an));
        var (bas, bit) = Zaman.YerelGunAraligiUtc(yerelGun);

        // ⚠ STORNO ÇİFTİ DIŞLANIR (paket 48 Faz A): hükümsüz kılınmış işlem
        // MASAK eşiğine ikinci kez sayılmaz — ÇİFT dışlanır, tek taraf değil.
        var gunun = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId
                     && h.CariId == cariId
                     && h.Tarih >= bas && h.Tarih < bit
                     && KapiyaTabiTurler.Contains(h.Tur))
            .StornoCiftiniDisla(db, tenantId);

        // ⚠ PAKET YASAL-3 (§ 4.1) — MASAK ÖLÇÜSÜ, İKİ AYRIK SORGU (A5 kararı:
        // bileşik COALESCE/CASE YAZILMAZ). (1) masak_olcu_tl dolu satırlar (TAKAS =
        // Max(eski, yeni), HURDA_ALIS = brüt) el değiştiren değerle; (2) boş olanlar
        // defterin TL bacağıyla. Kümeler kesişmez (aynı satır ya dolu ya boştur).
        var olculu = await gunun.Where(h => h.MasakOlcuTl != null)
            .SumAsync(h => h.MasakOlcuTl) ?? 0m;
        var olcusuz = await gunun.Where(h => h.MasakOlcuTl == null)
            .SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m;
        var defter = olculu + olcusuz;

        // ⚠ ZAMAN EKSENİ OLUSTURULDU (Mehmet K41-1): dekontun `tarih` alanı
        // KULLANICI girdisidir ve geçmişe verilebilir. Pencere `b.Tarih` ile
        // okunsaydı 100.000 TL "dün", 100.000 TL "bugün" tarihiyle girilerek
        // o günkü kimlik eşiği (ör. 185.000) hiç tetiklenmeden aşılırdı — eşik bölme (structuring)
        // yolunu kapının kendisi açmış olurdu. `olusturuldu` sunucu saatidir.
        // TenantId şartı global filtreye EK olarak elle de yazılır (çok katmanlı).
        var banka = await db.BankaHareketleri.AsNoTracking()
            .Where(b => b.TenantId == tenantId
                     && b.CariId == cariId
                     && b.Olusturuldu >= bas && b.Olusturuldu < bit
                     && b.HareketId == null)
            // PAKET P-12 (B-42): storno ÇİFTİ eşikten dışlanır — defter
            // tarafındaki kuralın (yukarıda) banka karşılığı. Hükümsüz kılınmış
            // bir dekont müşterinin haddini İKİNCİ KEZ dolduramaz.
            .StornoCiftiniDisla(db, tenantId)
            .SumAsync(b => (decimal?)b.Tutar) ?? 0m;

        return defter + Math.Abs(banka);
    }

    /// <summary>
    /// MASAK kapısına tabi hareket türleri. TRANSFER yoktur (karşı taraf yok),
    /// EMANET ve SAYIM_DUZELTME yoktur (para el değiştirmez).
    /// </summary>
    private static readonly string[] KapiyaTabiTurler =
        ["SATIS", "ALIS", "HURDA_ALIS", "TAKAS", "ISCILIK", "VERESIYE", "TAHSILAT",
         // PAKET P-20 (Y-10): hurda ÇIKIŞI da kıymetli maden işlemidir ve karşı
         // tarafı vardır (ramatçı/rafineri). Listede olmasaydı, eşiğin üstündeki
         // her hurda satışı MASAK kimlik tespitinden KAÇIŞ YOLU olurdu — alışta
         // kapalı olan kapı satışta açık kalamaz (fail-closed parite).
         "HURDA_SATIS",
         // Paket 27: döviz işleminin TL KARŞILIĞI eşiğe ve bağlantılı işlem
         // kümülatifine sayılır — döviz, MASAK kapısından kaçış yolu olamaz.
         "DOVIZ_VERESIYE", "DOVIZ_TAHSILAT",
         // Paket 29 Faz A — PARİTE ŞARTI (Mehmet, kurul mührü): karşılıksız
         // çıkan evrak, VERESIYE ile birebir aynı şeydir (borç yeniden doğar),
         // dolayısıyla VERESIYE neyse odur. ⚠ EVRAK_GIRIS burada YOKTUR ve bu
         // K4-D gevşemesidir (yukarıdaki bilinen-eksik-ölçüm notuna bakınız).
         "EVRAK_KARSILIKSIZ"];

    /// <summary>
    /// FAZ 2. Transaction içinde, hareket eklendikten sonra çağrılır. Kademe YOK ise
    /// hiçbir şey yazmaz. BelgeNo şifreleme ve hash, DbContext/dinleyicide otomatik.
    ///
    /// ⚠ PAKET 41 — <paramref name="kaynakTur"/> ZORUNLUDUR, varsayılanı YOKTUR
    /// (Bertan şartı): <c>hareketId</c> artık iki farklı tabloyu işaret edebilir
    /// ve ADR-0009 gereği yabancı anahtar yoktur. Varsayılan değerli opsiyonel
    /// parametre "unutma" riskini regresyon testine devrederdi; derleyici
    /// zorlaması bedava ve kesindir — yeni bir çağrı noktası eklendiğinde
    /// kaynak yazmayı atlamak DERLEME HATASIDIR.
    /// </summary>
    /// <param name="kaynakTur"><see cref="MasakKaynakTuru"/> — HAREKET ya da BANKA_HAREKET.</param>
    public void Kaydet(
        KuyumcuDbContext db, MasakGereksinim gerek, string hareketId, string? cariId,
        decimal tutarTl, MasakBilgiDto? bilgi, AktifKullanici islem, DateTime now,
        string kaynakTur)
    {
        if (gerek.Kademe == BeyanKademesi.Yok) return;
        if (bilgi is null)
            throw new InvalidOperationException("MASAK Kaydet: Dogrula geçmiş ama bilgi yok — akış hatası.");

        var saklamaBitis = motor.SaklamaBitisi(now);
        var tutar = Math.Abs(tutarTl);

        var tespit = new MasakKimlikTespiti
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            HareketId = hareketId,
            KaynakTur = kaynakTur,
            CariId = cariId,
            IslemTutariTl = tutar,
            BelgeTuru = bilgi.BelgeTuru!,
            BelgeNo = bilgi.BelgeNo!.Trim(),
            AdSoyad = bilgi.AdSoyad!.Trim(),
            DogumTarihi = bilgi.DogumTarihi,
            Uyruk = bilgi.Uyruk?.Trim().ToUpperInvariant(),
            Adres = bilgi.Adres?.Trim(),
            TespitEdenKullaniciId = islem.Id,
            Tarih = now,
            SaklamaBitis = saklamaBitis,
            Olusturuldu = now,
        };
        db.MasakKimlikTespitleri.Add(tespit);

        if (gerek.AmacGerekli)
        {
            db.MasakBeyanlari.Add(new MasakBeyani
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                KimlikTespitiId = tespit.Id,
                HareketId = hareketId,
                KaynakTur = kaynakTur,
                IslemTutariTl = tutar,
                Kademe = gerek.Kademe,
                IslemAmaci = bilgi.IslemAmaci!.Trim(),
                Aciklama = bilgi.Aciklama!.Trim(),
                FonKaynagi = bilgi.FonKaynagi?.Trim(),
                EkBelgeReferansi = bilgi.EkBelgeReferansi?.Trim(),
                AlanKullaniciId = islem.Id,
                Tarih = now,
                SaklamaBitis = saklamaBitis,
                Olusturuldu = now,
            });
        }
    }
}
