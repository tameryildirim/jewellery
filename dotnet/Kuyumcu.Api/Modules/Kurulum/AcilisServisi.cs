using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kurulum;

// ═════════════════════════════════════════════════════════════════════════════
// AÇILIŞ BAKİYELERİ — kuru prova · uygula · geri al (paket T-2 Faz A)
//
// ⚠ TEK DEFTER YOLU: bu dosyada `db.Hareketler.Add` YOKTUR ve olamaz
//   (denetim.sh "Kurulum/ altında dogrudan Add = 0" sayacı). Defter bacağı
//   YALNIZ HareketFabrikasi.Ekle ile yazılır; cari bacağı ise mevcut
//   CariServisi yolundan geçer (CariModulu.AcilisYaz → DuzeltmeYaz) ki bakiye
//   satırı, döviz bacağı ve denetim izi atlanmasın.
//
// ⚠ ÖDEME İZİ Yok'tur (FAIL-CLOSED 3): açılışta para EL DEĞİŞTİRMEZ. 459 s.
//   VUK tevsik kümülatifi ve MASAK bağlantılı işlem toplamı ETKİLENMEZ.
//   Aksi tasarım nakit haddini SAHTE biçimde doldururdu.
//
// ⚠ FAIL-CLOSED 2 — KURU PROVA ZORUNLUDUR. Uygula, aynı içerik üzerinden
//   sunucunun ürettiği prova jetonu olmadan 400 verir. Jeton bir HMAC'tir
//   (IAlanSifreleyici.Hashle): istemci onu üretemez, yalnız prova döndürür ve
//   prova jetonu YALNIZ hatasız satır kümesinde üretilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AcilisProvaDto
{
    /// <summary>KASA · BANKA · CARI (Faz A). STOK Faz B'dedir.</summary>
    [Required, MaxLength(10)] public string Tur { get; set; } = "";

    public List<AcilisSatirDto> Satirlar { get; set; } = [];
}

public sealed class AcilisUygulaDto
{
    [Required, MaxLength(10)] public string Tur { get; set; } = "";

    public List<AcilisSatirDto> Satirlar { get; set; } = [];

    /// <summary>Kuru provanın döndürdüğü jeton (FAIL-CLOSED 2).</summary>
    [Required] public string ProvaJetonu { get; set; } = "";

    /// <summary>En az 20 karakter — servis içinde ELLE doğrulanır (FAIL-CLOSED 10).</summary>
    [Required, MinLength(20), MaxLength(500)] public string Gerekce { get; set; } = "";

    /// <summary>Kur fotoğrafı — ilk gün MANUAL kayıt sihirbazın 3. adımında üretilir.</summary>
    [Required, MaxLength(36)] public string KurSnapshotId { get; set; } = "";

    /// <summary>
    /// Kredi limiti aşan satırlar için SAHİP'in ayrı onayı (Mehmet koşulu 3).
    /// Uyarı varken bu bayrak gelmezse istek 400'dür — "gördüm ve kabul ediyorum"
    /// kaydı olmadan limit aşımı sessizce geçmez.
    /// </summary>
    public bool LimitAsimiOnayi { get; set; }
}

public sealed class AcilisGeriAlDto
{
    [Required, MaxLength(36)] public string OturumId { get; set; } = "";

    [Required, MinLength(20), MaxLength(500)] public string Gerekce { get; set; } = "";

    [Required, MaxLength(36)] public string KurSnapshotId { get; set; } = "";
}

public sealed class AcilisServisi(
    KuyumcuDbContext db,
    Cari.CariServisi cariServisi,
    DenetimServisi denetim,
    IAlanSifreleyici sifreleyici,
    KurulumServisi kurulum,
    ILogger<AcilisServisi> logger)
{
    /// <summary>Gerekçe asgarisi — cari düzeltme fişiyle simetrik.</summary>
    public const int GerekceAsgari = 20;

    /// <summary>
    /// PAKET P-68 — açılışın KABUL ETTİĞİ para birimleri. TEK KAYNAK: kuru prova
    /// doğrulaması (DOVIZ_HESAP_FAZ_B) ve /kurulum/durum yanıtındaki
    /// <c>acilisParaBirimleri</c> alanı bu listeden beslenir; ekran sabit liste
    /// YAZMAZ, hedef seçicisini bu alana göre süzer. Döviz kasası/hesabı açılışı
    /// Faz B işidir — o gün yalnız bu dizi genişler.
    /// </summary>
    public static readonly IReadOnlyList<string> KabulEdilenParaBirimleri = ["TRY"];

    // ── KURU PROVA ──────────────────────────────────────────────────────────

    /// <summary>
    /// Satırları doğrular ve satır bazlı sonuç döner. <b>HİÇBİR YAZMA YAPMAZ</b>
    /// (SaveChanges çağrılmaz, hiçbir varlık Add edilmez).
    /// </summary>
    public async Task<object> Prova(AcilisProvaDto dto, AktifKullanici islem)
    {
        var tur = TurDogrula(dto.Tur);
        await kurulum.AcilisPenceresiDogrula(islem.TenantId);
        kurulum.SatirTavaniDogrula(dto.Satirlar.Count);

        AcilisKaynaklari.OndalikKapisi(dto.Satirlar);   // PAKET GOC-C: fazla ondalık 400 (sessiz yuvarlama yok)
        var hatalar = AcilisKaynaklari.SafDogrula(tur, dto.Satirlar);
        var uyarilar = new List<AcilisSatirHatasi>();
        await VeritabaniDogrula(tur, dto.Satirlar, islem.TenantId, hatalar, uyarilar);

        var kanonik = AcilisKaynaklari.KanonikMetin(tur, dto.Satirlar);
        var temiz = hatalar.Count == 0 && dto.Satirlar.Count > 0;

        // ⚠ Jeton YALNIZ temiz provada üretilir: hatalı satır varken jeton
        //   dönseydi "prova zorunlu" kapısı kâğıt üstünde kalırdı.
        var jeton = temiz ? JetonUret(islem.TenantId, tur, kanonik) : null;

        // KVKK: cari açılış listesi toplu kişisel veri gösterir. Bu görüntüleme
        // KAYDA GEÇER (UX-S20 · 6698 md.12) — ekran da kullanıcıya bunu yazar.
        if (tur == AcilisTuru.Cari && dto.Satirlar.Count > 0)
            await denetim.ListeOkumaKaydet(
                islem, "AcilisKalemi", dto.Satirlar.Count, "açılış kuru provası");

        return new
        {
            tur,
            temiz,
            provaJetonu = jeton,
            toplamSatir = dto.Satirlar.Count,
            hataliSatir = hatalar.Select(h => h.SatirNo).Distinct().Count(),
            uygulanacakSatir = temiz ? dto.Satirlar.Count : 0,
            uyariliSatir = uyarilar.Select(u => u.SatirNo).Distinct().Count(),
            limitAsimiOnayiGerekli = uyarilar.Any(u => u.HataKodu == AcilisUyariKodu.KrediLimitiAsimi),
            tlToplam = dto.Satirlar.Sum(s => s.TlTutar),
            hasToplam = dto.Satirlar.Sum(s => s.HasGram),
            kisiselVeriIzi = tur == AcilisTuru.Cari,
            hatalar,
            uyarilar,
        };
    }

    // ── UYGULA ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Temiz prova jetonuyla TEK TRANSACTION'da uygular. Bir satır patlarsa
    /// oturumun tamamı geri alınır (FAIL-CLOSED 9).
    /// </summary>
    public async Task<object> Uygula(AcilisUygulaDto dto, AktifKullanici islem)
    {
        var tur = TurDogrula(dto.Tur);
        await kurulum.MuhurDogrula(islem.TenantId);
        await kurulum.AcilisPenceresiDogrula(islem.TenantId);

        // ⚠ DataAnnotations yalnız denetleyici yolunda çalışır; servis doğrudan
        //   çağrıldığında hiçbir şey doğrulamaz (FAIL-CLOSED 10).
        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < GerekceAsgari)
            throw new BadRequestException(
                $"Açılış gerekçesi en az {GerekceAsgari} karakter olmalıdır");
        if (gerekce.Length > 500)
            throw new BadRequestException("Açılış gerekçesi en çok 500 karakter olabilir");

        if (dto.Satirlar.Count == 0)
            throw new BadRequestException("Açılış listesi boş");
        kurulum.SatirTavaniDogrula(dto.Satirlar.Count);

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        // FAIL-CLOSED 2 — prova jetonu.
        var kanonik = AcilisKaynaklari.KanonikMetin(tur, dto.Satirlar);
        if (JetonUret(islem.TenantId, tur, kanonik) != dto.ProvaJetonu)
            throw new BadRequestException(
                "Bu içerik için geçerli bir kuru prova sonucu yok — önce kuru provayı çalıştırın " +
                "(satırlar prova sonrasında değiştiyse provayı tekrarlayın).");

        // Prova ile uygula ARASINDA veri değişmiş olabilir: kapılar BURADA da
        // koşar. "Prova geçerli dedi" bir GARANTİ DEĞİL, bir ÖN GÖSTERİMDİR.
        AcilisKaynaklari.OndalikKapisi(dto.Satirlar);   // PAKET GOC-C: fazla ondalık 400 (sessiz yuvarlama yok)
        var hatalar = AcilisKaynaklari.SafDogrula(tur, dto.Satirlar);
        var uyarilar = new List<AcilisSatirHatasi>();
        await VeritabaniDogrula(tur, dto.Satirlar, islem.TenantId, hatalar, uyarilar);
        // Kiracı sınırı ihlali 400 DEĞİL 404'tür: "başka kiracının kaydı"
        // ile "hiç yok" istemciye AYNI görünmelidir (K1 · varlık sızdırma yok).
        if (hatalar.Any(h => h.HataKodu == AcilisHataKodu.HedefBulunamadi))
            throw new NotFoundException(
                $"Açılış satırındaki kayıt bu işletmede bulunamadı ({hatalar.First(h => h.HataKodu == AcilisHataKodu.HedefBulunamadi).SatirNo}. satır)");

        // FAIL-CLOSED 7: aynı hedefe ikinci açılış bir DOĞRULAMA hatası değil,
        // bir ÇAKIŞMADIR (409) — istemci gövdeyi düzeltmekle çözemez.
        if (hatalar.FirstOrDefault(h => h.HataKodu == AcilisHataKodu.ZatenAcilis) is { } cakisma)
            throw new ConflictException(
                $"{cakisma.SatirNo}. satır: {cakisma.HataMesaji}. " +
                "Bakiye düzeltmesi için cari düzeltme fişini ya da ters kaydı kullanın.");

        if (hatalar.Count > 0)
            throw new BadRequestException(
                $"Açılış satırlarında hata var ({hatalar[0].SatirNo}. satır: {hatalar[0].HataMesaji}) — " +
                "kuru provayı tekrar çalıştırın.");

        if (uyarilar.Any(u => u.HataKodu == AcilisUyariKodu.KrediLimitiAsimi) && !dto.LimitAsimiOnayi)
            throw new BadRequestException(
                "Bazı satırlar cari kredi limitini aşıyor. Açılış engellenmez, ancak bu satırları " +
                "gördüğünüzü onaylamanız gerekir.");

        var now = DateTime.UtcNow;
        var oturumId = Kimlik.Ulid();

        await using var tx = await db.Database.BeginTransactionAsync();

        var kalemler = new List<AcilisKalemi>();
        foreach (var satir in dto.Satirlar)
        {
            var kalem = tur switch
            {
                AcilisTuru.Cari  => await CariAcilisi(satir, dto, islem, oturumId, now),
                AcilisTuru.Kasa  => await KasaAcilisi(satir, dto, islem, oturumId, now),
                AcilisTuru.Banka => await BankaAcilisi(satir, dto, islem, oturumId, now),
                _ => throw new BadRequestException($"Bu sürümde {tur} açılışı yoktur"),
            };
            db.AcilisKalemleri.Add(kalem);
            kalemler.Add(kalem);

            denetim.YazmaKaydet(islem, "ACILIS", kalem.HareketId,
                FormattableString.Invariant(
                    $"{tur} açılış: hedef={satir.HedefId} tl={satir.TlTutar:+0.##;-0.##;0} has={satir.HasGram:+0.######;-0.######;0}")
                + $" · {gerekce}");
        }

        await kurulum.AdimIsaretle(islem.TenantId, now);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "[{TenantId}] Açılış uygulandı: tur={Tur} satir={Adet} oturum={OturumId}",
            islem.TenantId, tur, kalemler.Count, oturumId);

        // UX-S27: sunucunun döndürdüğü sonuç ekranda banda yazılır.
        return new
        {
            oturumId,
            tur,
            uygulananSatir = kalemler.Count,
            tlToplam = kalemler.Sum(k => k.TlTutar),
            hasToplam = kalemler.Sum(k => k.HasGram),
            geriAlinabilir = true,
        };
    }

    // ── GERİ AL (mühürlenmeden önce) ────────────────────────────────────────

    /// <summary>
    /// Mühürlenmemiş bir açılış oturumunu TERS ACILIS kaydıyla geri alır.
    /// <para>
    /// ⚠ Kalem SİLİNMEZ, defterden satır KALDIRILMAZ (ADR-0004 append-only):
    /// her kalem için ters işaretli ikinci bir ACILIS hareketi yazılır ve kalem
    /// GERI_ALINDI damgası alır. "İkinci bir silme yolu" açılmaz.
    /// </para>
    /// </summary>
    public async Task<object> GeriAl(AcilisGeriAlDto dto, AktifKullanici islem)
    {
        await kurulum.MuhurDogrula(islem.TenantId);

        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < GerekceAsgari)
            throw new BadRequestException(
                $"Geri alma gerekçesi en az {GerekceAsgari} karakter olmalıdır");

        if (islem.SubeId is null)
            throw new BadRequestException("Hareket kaydı için şube seçimi gerekli");

        await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);

        var kalemler = await db.AcilisKalemleri
            .Where(k => k.OturumId == dto.OturumId && k.Durum == AcilisKalemDurumu.Uygulandi)
            .ToListAsync();

        if (kalemler.Count == 0)
            throw new NotFoundException("Geri alınacak açılış oturumu bulunamadı");

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        foreach (var kalem in kalemler)
        {
            var ters = new AcilisSatirDto
            {
                HedefId = kalem.HedefId,
                TlTutar = -kalem.TlTutar,
                HasGram = -kalem.HasGram,
            };
            var tersDto = new AcilisUygulaDto
            {
                Tur = kalem.Tur,
                Gerekce = gerekce,
                KurSnapshotId = dto.KurSnapshotId,
                ProvaJetonu = "",
            };

            _ = kalem.Tur switch
            {
                AcilisTuru.Cari  => await CariAcilisi(ters, tersDto, islem, kalem.OturumId, now, tersKayit: true),
                AcilisTuru.Kasa  => await KasaAcilisi(ters, tersDto, islem, kalem.OturumId, now, tersKayit: true),
                AcilisTuru.Banka => await BankaAcilisi(ters, tersDto, islem, kalem.OturumId, now, tersKayit: true),
                _ => throw new BadRequestException($"Bu sürümde {kalem.Tur} geri alması yoktur"),
            };

            kalem.Durum = AcilisKalemDurumu.GeriAlindi;
            denetim.YazmaKaydet(islem, "ACILIS", kalem.HareketId,
                $"{kalem.Tur} açılış GERİ ALINDI: hedef={kalem.HedefId} · {gerekce}");
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new { oturumId = dto.OturumId, geriAlinan = kalemler.Count };
    }

    // ── Tür bazlı yazıcılar ─────────────────────────────────────────────────

    private async Task<AcilisKalemi> CariAcilisi(
        AcilisSatirDto satir, AcilisUygulaDto dto, AktifKullanici islem,
        string oturumId, DateTime now, bool tersKayit = false)
    {
        // ⚠ İKİNCİ YAZMA YOLU AÇILMAZ: mevcut cari düzeltme yolu 'tur'
        //   parametresiyle ACILIS yazar (Bertan'ın birleşik koşulu).
        var hareketId = await cariServisi.AcilisYaz(satir.HedefId, new Cari.CariDuzeltmeDto
        {
            TlDelta = satir.TlTutar,
            HasDelta = satir.HasGram,
            Gerekce = dto.Gerekce.Trim(),
            KurSnapshotId = dto.KurSnapshotId,
        }, islem);

        return Kalem(AcilisTuru.Cari, satir, islem, oturumId, now, hareketId, tersKayit);
    }

    private async Task<AcilisKalemi> KasaAcilisi(
        AcilisSatirDto satir, AcilisUygulaDto dto, AktifKullanici islem,
        string oturumId, DateTime now, bool tersKayit = false)
    {
        // Gün kapanışı kilidi BURADA uygulanır (FAIL-CLOSED 5): kapanmış bir
        // kasa-gününe açılış yazılamaz.
        var kasa = await IslemDogrulayici.KasaDogrula(db, satir.HedefId, islem.TenantId, "TRY", now);

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId!,
            Tur = "ACILIS",
            Tarih = now,
            Aciklama = $"Açılış devri (kasa): {dto.Gerekce.Trim()}",
            TlTutari = satir.TlTutar,
            HasGrami = 0m,
            KurSnapshotId = dto.KurSnapshotId,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        // PAKET P-44 (spesifikasyon dışı, ÖLÇÜLDÜ): açılış GERİ ALMA yolu
        // (GeriAl → KasaAcilisi, tersKayit: true) negatif TlTutar ile buraya
        // girer ve doğrulama hattındaki "negatif açılış" kontrolünü ATLAR;
        // harcanmış bir açılış geri alınınca kasa eksiye düşebiliyordu.
        Kasa.KasaBakiyeKapisi.Dogrula(kasa, satir.TlTutar);

        // Kasa bacağı AYNI transaction içinde (Bertan koşulu: KasaServisi'ne
        // ikinci bir sözleşme eklenmez, CariModulu'ndaki mevcut kalıp kullanılır).
        await BakiyeKilidi.Kilitle(db, kasa);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        kasa.Bakiye = Math.Round(kasa.Bakiye + satir.TlTutar, 2, MidpointRounding.AwayFromZero);
        kasa.Guncellendi = now;

        var kasaHareketId = Kimlik.Ulid();
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = kasaHareketId,
            TenantId = islem.TenantId,
            KasaId = kasa.Id,
            HareketId = hareketId,
            Tutar = Math.Round(satir.TlTutar, 2, MidpointRounding.AwayFromZero),
            ParaBirimi = kasa.ParaBirimi,
            // Açılışta para el değiştirmez; DIGER etiketi "nakit sayılmaz" demektir.
            OdemeTuru = "DIGER",
            Tarih = now,
            Olusturuldu = now,
        });

        var kalem = Kalem(AcilisTuru.Kasa, satir, islem, oturumId, now, hareketId, tersKayit);
        kalem.KasaHareketId = kasaHareketId;
        return kalem;
    }

    private async Task<AcilisKalemi> BankaAcilisi(
        AcilisSatirDto satir, AcilisUygulaDto dto, AktifKullanici islem,
        string oturumId, DateTime now, bool tersKayit = false)
    {
        var hesap = await db.BankaHesaplari
            .FirstOrDefaultAsync(h => h.Id == satir.HedefId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");

        var hareketId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = hareketId,
            TenantId = islem.TenantId,
            SubeId = islem.SubeId!,
            Tur = "ACILIS",
            Tarih = now,
            Aciklama = $"Açılış devri (banka): {dto.Gerekce.Trim()}",
            TlTutari = satir.TlTutar,
            HasGrami = 0m,
            KurSnapshotId = dto.KurSnapshotId,
            KullaniciId = islem.Id,
            Olusturuldu = now,
        });

        await BakiyeKilidi.Kilitle(db, hesap);   // PAKET KAPORA-ESZAMAN tur 2: kilitli taze okuma (kayıp güncelleme)
        hesap.Bakiye = Math.Round(hesap.Bakiye + satir.TlTutar, 2, MidpointRounding.AwayFromZero);
        hesap.Guncellendi = now;

        db.BankaHareketleri.Add(new BankaHareket
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            BankaHesapId = hesap.Id,
            Yon = satir.TlTutar >= 0m ? "GELEN" : "GIDEN",
            Tutar = Math.Round(Math.Abs(satir.TlTutar), 2, MidpointRounding.AwayFromZero),
            ParaBirimi = hesap.ParaBirimi,
            Aciklama = $"Açılış devri: {dto.Gerekce.Trim()}",
            // Açılış satırı eşleştirilecek bir banka hareketi DEĞİLDİR.
            Durum = "ESLESTI",
            Tarih = now,
            Olusturuldu = now,
        });

        return Kalem(AcilisTuru.Banka, satir, islem, oturumId, now, hareketId, tersKayit);
    }

    private static AcilisKalemi Kalem(
        string tur, AcilisSatirDto satir, AktifKullanici islem,
        string oturumId, DateTime now, string hareketId, bool tersKayit) => new()
    {
        Id = Kimlik.Ulid(),
        TenantId = islem.TenantId,
        OturumId = oturumId,
        Tur = tur,
        HedefId = satir.HedefId,
        SubeId = tur == AcilisTuru.Stok ? islem.SubeId : null,
        // ⚠ Ters kayıt AYNI benzersiz anahtarı kullanamaz (UNIQUE ihlali) ve
        //   zaten YENİ bir kalem değildir: geri almada kalem EKLENMEZ, mevcut
        //   kalem damgalanır. Bu nesne yalnız çağrı simetrisi için üretilir.
        BenzersizAnahtar = tersKayit
            ? $"TERS:{Kimlik.Ulid()}"
            : AcilisKalemi.AnahtarUret(tur, satir.HedefId, islem.SubeId),
        HareketId = hareketId,
        TlTutar = satir.TlTutar,
        HasGram = satir.HasGram,
        Durum = AcilisKalemDurumu.Uygulandi,
        Olusturuldu = now,
    };

    // ── Ortak yardımcılar ───────────────────────────────────────────────────

    private static string TurDogrula(string? tur)
    {
        var t = (tur ?? "").Trim().ToUpperInvariant();
        if (Array.IndexOf(AcilisTuru.FazA, t) < 0)
            throw new BadRequestException(
                $"Geçersiz açılış türü. Kabul edilenler: {string.Join(", ", AcilisTuru.FazA)}");
        return t;
    }

    /// <summary>
    /// Prova jetonu = HMAC(kiracı + tür + kanonik içerik). İstemci üretemez;
    /// içerik bir harf bile değişirse jeton tutmaz.
    /// </summary>
    private string JetonUret(string tenantId, string tur, string kanonik) =>
        sifreleyici.Hashle($"ACILIS|{tenantId}|{tur}|{AcilisKaynaklari.Ozet(kanonik)}");

    /// <summary>Veritabanına BAKAN doğrulama — yalnız OKUR, hiçbir şey yazmaz.</summary>
    private async Task VeritabaniDogrula(
        string tur, IReadOnlyList<AcilisSatirDto> satirlar, string tenantId,
        List<AcilisSatirHatasi> hatalar, List<AcilisSatirHatasi> uyarilar)
    {
        var hedefler = satirlar.Select(s => s.HedefId)
            .Where(h => !string.IsNullOrWhiteSpace(h)).Distinct().ToList();
        if (hedefler.Count == 0) return;

        var acilisi = await db.AcilisKalemleri.AsNoTracking()
            .Where(k => k.Tur == tur && k.Durum == AcilisKalemDurumu.Uygulandi
                        && hedefler.Contains(k.HedefId))
            .Select(k => k.HedefId)
            .ToListAsync();

        var kasalar = tur == AcilisTuru.Kasa
            ? await db.Kasalar.AsNoTracking().Where(k => hedefler.Contains(k.Id))
                .Select(k => new { k.Id, k.ParaBirimi, k.Aktif }).ToListAsync()
            : [];
        var hesaplar = tur == AcilisTuru.Banka
            ? await db.BankaHesaplari.AsNoTracking().Where(h => hedefler.Contains(h.Id))
                .Select(h => new { h.Id, h.ParaBirimi, h.Aktif }).ToListAsync()
            : [];
        var cariler = tur == AcilisTuru.Cari
            ? await db.Cariler.AsNoTracking().Where(c => hedefler.Contains(c.Id))
                .Select(c => new { c.Id, c.Aktif, c.KrediLimitiTl }).ToListAsync()
            : [];

        for (var i = 0; i < satirlar.Count; i++)
        {
            var s = satirlar[i];
            var no = i + 1;
            if (string.IsNullOrWhiteSpace(s.HedefId)) continue;

            if (acilisi.Contains(s.HedefId))
                hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.ZatenAcilis));

            switch (tur)
            {
                case AcilisTuru.Kasa:
                {
                    var k = kasalar.FirstOrDefault(x => x.Id == s.HedefId);
                    if (k is null) hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.HedefBulunamadi));
                    else if (!k.Aktif) hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.PasifHedef));
                    else if (!KabulEdilenParaBirimleri.Contains(k.ParaBirimi))
                        hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.DovizHesapFazB));
                    // QA turu 5 (YÜKSEK): negatif kasa açılışı "geçerli" sayılıyordu.
                    if (s.TlTutar < 0m)
                        hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.NegatifAcilis));
                    break;
                }
                case AcilisTuru.Banka:
                {
                    var h = hesaplar.FirstOrDefault(x => x.Id == s.HedefId);
                    if (h is null) hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.HedefBulunamadi));
                    else if (!h.Aktif) hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.PasifHedef));
                    else if (!KabulEdilenParaBirimleri.Contains(h.ParaBirimi))
                        hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.DovizHesapFazB));
                    // QA turu 5 (YÜKSEK): negatif banka açılışı "geçerli" sayılıyordu.
                    if (s.TlTutar < 0m)
                        hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.NegatifAcilis));
                    break;
                }
                case AcilisTuru.Cari:
                {
                    var c = cariler.FirstOrDefault(x => x.Id == s.HedefId);
                    if (c is null) { hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.HedefBulunamadi)); break; }
                    if (!c.Aktif) { hatalar.Add(AcilisKaynaklari.Hata(no, AcilisHataKodu.PasifHedef)); break; }
                    // KARAR: limit aşımı REDDETMEZ, UYARIR (uçlar ↔ açık_sorular
                    // çelişkisi mühürlendi). Bedeli: SAHİP'in ayrı onayı.
                    if (c.KrediLimitiTl is { } limit && s.TlTutar > limit)
                        uyarilar.Add(new AcilisSatirHatasi(no, AcilisUyariKodu.KrediLimitiAsimi,
                            AcilisKaynaklari.Mesaj(AcilisUyariKodu.KrediLimitiAsimi)));
                    break;
                }
            }
        }
    }
}
