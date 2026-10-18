using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA MODÜLÜ — KasaModulu desenini izler.
// Kaynak: apps/api/src/modules/fatura/*
//   • Durum makinesi: TASLAK→[KESILDI,IPTAL], KESILDI→[ODENDI,IPTAL],
//     ODENDI→[IPTAL], IPTAL→[]
//   • Fatura no: FTR-{yıl}-{sıra:6 hane} (Banka modülündeki dekont no formatıyla aynı desen)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class FaturaOlusturDto
{
    [Required, StringLength(36, MinimumLength = 26)]
    public string CariId { get; set; } = null!;

    [RegularExpression("SATIS|ALIS")]
    public string? Tur { get; set; }

    /// <summary>KDV dahil toplam tutar</summary>
    // PAKET GOC-A (envanter açık #10): K9 — > 0, en çok 2 ondalık, decimal(15,2) tavanı (POST'ta yoktu → 500 riski).
    [Required, Tutar]
    public decimal Toplam { get; set; }

    [Range(0.0, 1.0)]
    public decimal? KdvOran { get; set; }

    /// <summary>STANDART (varsayılan): KDV tüm tutara. KUYUM: has altın KDV'siz, KDV yalnız işçilik+kâra.</summary>
    [RegularExpression("STANDART|KUYUM")]
    public string? KdvTuru { get; set; }

    /// <summary>KUYUM türünde matrahtan düşülen has altın bedeli (KDV'siz)</summary>
    [Range(0.0, double.MaxValue)]
    public decimal? HasDeger { get; set; }

    [RegularExpression("TRY|USD|EUR|GBP")]
    public string? ParaBirimi { get; set; }

    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaAciklama)]   // GOC-A: iki tarafta da sınır yoktu
    public string? Aciklama { get; set; }

    /// <summary>ISO tarih; verilmezse şimdi</summary>
    // GOC-A (envanter açık #9): K8a işlem tarihi — ileri gün yok, en çok 1 yıl geri (VUK belge tarihi).
    [TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Islem)]
    public DateTime? Tarih { get; set; }

    /// <summary>
    /// Faturalandırılan satış hareketi — OPSİYONEL. Verilirse harekete fatura
    /// numarası damgalanır; gün sonu POS–fatura mutabakatı (Adım 7) bu bağdan
    /// "faturasız satış" listesini çıkarır.
    /// </summary>
    [MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>
    /// OPSİYONEL satır kalemleri (paket 1 Faz A).
    /// <para>
    /// BOŞSA bugünkü davranış BİT BİT AYNIDIR (kalem_kaynagi = BASLIK) — geriye
    /// uyum mutlaktır. DOLUYSA toplam SUNUCUDA kalemlerden hesaplanır,
    /// <see cref="Toplam"/> ile kuruşu kuruşuna eşit değilse 400; KUYUM'da
    /// <see cref="HasDeger"/> OKUNMAZ, kalemlerden türetilir.
    /// </para>
    /// </summary>
    public List<FaturaKalemDto>? Kalemler { get; set; }
}

/// <summary>
/// Fatura satır kalemi girdisi.
/// ⚠ HAS BEDELİ ALANI YOKTUR ve BİLİNÇLİDİR: kuyum KDV'sinde matrahtan düşülen
/// tutar istemciden alınsaydı yanlış beyan kapısı açılırdı. Has bedeli ürün
/// kartındaki has gram ve kur kaydındaki has fiyatından SUNUCUDA türetilir.
/// </summary>
public sealed class FaturaKalemDto
{
    /// <summary>Ürün kartı — ayar/milyem/gram/has/taş ibaresi buradan KOPYALANIR.</summary>
    [MaxLength(36)] public string? UrunId { get; set; }

    /// <summary>Kalemin dayandığı defter satırı (varsa) — kur kaydı da buradan okunur.</summary>
    [MaxLength(36)] public string? HareketId { get; set; }

    [MaxLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaKalemAciklama)] public string? Aciklama { get; set; }

    // PAKET GOC-A: miktar K10 (> 0, 3 ondalık, decimal(12,3) tavanı); tutarlar K9a (≥ 0, 2 ondalık, tavan).
    // POST yolunda tavan yoktu (PATCH'te vardı) → 400 yerine 500 riski; fazla ondalık sessizce yuvarlanıyordu.
    [Gram] public decimal Miktar { get; set; }

    [Tutar(Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir)] public decimal BirimFiyat { get; set; }

    [Tutar(Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir)] public decimal? IscilikTl { get; set; }

    [Tutar(Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir)] public decimal? IndirimTutari { get; set; }
}

/// <summary>
/// Fişten (ya da tek hareketten) fatura kesme girdisi — paket 1 Faz A.
/// <para>
/// Her hareket tek kalemdir; kalemin MİKTARI defterden okunur (ZO-BELGE 30.09,
/// eski "ADET=1 sözleşmesi" kaldırıldı — bkz. <c>FaturaServisi.FistenKalemMiktari</c>),
/// satır toplamı hareketin TL tutarının ta kendisidir (fatura toplamı değişmez).
/// </para>
/// </summary>
public sealed class FaturaFistenDto
{
    /// <summary>Fişin kimliği — fişin TÜM kalemleri faturaya girer (B-4).</summary>
    [MaxLength(36)] public string? FisId { get; set; }

    /// <summary>Fiş kimliği yoksa tek hareket; o hareketin fişi varsa fişin tamamı alınır.</summary>
    [MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>Verilmezse fişin carisi kullanılır; fişte cari yoksa 400.</summary>
    [MaxLength(36)] public string? CariId { get; set; }

    [RegularExpression("SATIS|ALIS")] public string? Tur { get; set; }

    [RegularExpression("STANDART|KUYUM")] public string? KdvTuru { get; set; }

    [Range(0.0, 1.0)] public decimal? KdvOran { get; set; }

    [RegularExpression("TRY|USD|EUR|GBP")] public string? ParaBirimi { get; set; }

    [TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Islem)]   // GOC-A: K8a
    public DateTime? Tarih { get; set; }

    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaAciklama)]   // GOC-A
    public string? Aciklama { get; set; }
}

public sealed class DurumGuncelleDto
{
    [Required, RegularExpression("TASLAK|KESILDI|ODENDI|IPTAL")]
    public string Durum { get; set; } = null!;
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class FaturaServisi(KuyumcuDbContext db, KdvAyarlari? kdvAyar = null)
{
    /// <summary>
    /// KDV oranı ve özel matrah kodu — ADR-0005 (rakam koda gömülmez).
    /// <para>
    /// ⚠ OPSİYONEL ve null'da varsayılan (Aşkın'ın koşulu): mevcut
    /// <c>new FaturaServisi(db)</c> çağrıları (RaporFaturaTestleri) KIRILMAZ.
    /// </para>
    /// <para>
    /// ⚠ PAKET P-26 (Y-40): <see cref="KdvAyarlari.OzelMatrahKodu"/> VARSAYILAN
    /// OLARAK BOŞTUR (anahtar K-10 teyidi gelmeden açılmaz). Oran 0.20 DURUYOR.
    /// Kod hiçbir hesaba girmez — yalnız KUYUM faturasının e-belge etiketidir.
    /// </para>
    /// </summary>
    private readonly KdvAyarlari ayar = kdvAyar ?? new KdvAyarlari();

    /// <summary>
    /// PAKET P-26 (Y-40) — yürürlükteki özel matrah kodu + <b>kaynağı</b>.
    /// Beyansız kod ekrana basılamaz; sözleşme <see cref="KdvAyarlari.KaynakBeyani"/>'dir.
    /// </summary>
    public object OzelMatrahAyari(bool? kuyumFaturaVar = null) => new
    {
        // PAKET YASAL-4 / Y4-C: Faturalar başlığındaki tek satır rozet YALNIZ kiracıda
        // KUYUM fatura varken çizilir (sayı ekranda tahmin edilmez, sunucudan gelir).
        kuyumFaturaVar,
        kod = ayar.KodYaDaNull,
        acik = ayar.KodYaDaNull is not null,
        aralikta = ayar.KodAralikta,
        aralik = $"{KdvAyarlari.KodAlt}-{KdvAyarlari.KodUst}",
        varsayilanOran = ayar.VarsayilanOran,
        kaynakBeyani = ayar.KaynakBeyani,
        // Anayasa md.1 — ürün kendi sınırını söyler, gizlemez.
        maliMusavirTeyidi = false,
    };

    /// <summary>PAKET YASAL-4 / Y4-C — kiracıda en az bir KUYUM türlü fatura var mı?</summary>
    public Task<bool> KuyumFaturaVar(AktifKullanici islem) =>
        db.Faturalar.AsNoTracking().AnyAsync(f => f.TenantId == islem.TenantId && f.KdvTuru == "KUYUM");

    /// <summary>İptal edilen fatura başka duruma alınamaz</summary>
    private static readonly Dictionary<string, string[]> GecerliGecisler = new()
    {
        ["TASLAK"]  = ["KESILDI", "IPTAL"],
        ["KESILDI"] = ["ODENDI", "IPTAL"],
        ["ODENDI"]  = ["IPTAL"],
        ["IPTAL"]   = [],
    };

    // ─── Manuel fatura oluştur ────────────────────────────────────────────────

    // ── PAKET GOC-A — servis yolu girdi kapısı (DataAnnotations yalnız controller yolunda) ──────
    // Başlık: K8a işlem tarihi (ileri gün yok — VUK belge tarihi oynatılamaz; en çok 1 yıl geri),
    // açıklama sınırı. Kalem: K10 miktar + K9a tutarlar (SESSİZ YUVARLAMA YOK) + açıklama 300
    // (eskiden 300'ü aşan kalem açıklaması SESSİZCE kırpılıyordu). Metinler sözlükten (ekranla aynı).
    private static void BaslikGirdisiDenetle(DateTime? tarih, string? aciklama)
    {
        if (tarih is { } t)
            GirdiKapisi.IslemTarihi(Zaman.YerelBugun(t.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : t.ToUniversalTime()), "tarih");
        GirdiKapisi.Uzunluk(aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaAciklama, "aciklama");
    }

    private static void KalemGirdisiDenetle(FaturaKalemDto g, string? onek)
    {
        // İleti "<kalem> · <alan>: <sözlük metni>" — alan altı metni (sözlük) AYNI, hangi alan olduğu başta.
        string Ek(string ad) => onek is null ? ad : $"{onek} · {ad}";
        GirdiKapisi.Gram(g.Miktar, "miktar", Ek("Miktar"));
        GirdiKapisi.Tutar(g.BirimFiyat, "birimFiyat", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, Ek("Birim fiyat"));
        GirdiKapisi.Tutar(g.IscilikTl, "iscilikTl", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, Ek("İşçilik"));
        GirdiKapisi.Tutar(g.IndirimTutari, "indirimTutari", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, Ek("İndirim"));
        GirdiKapisi.Uzunluk(g.Aciklama, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaKalemAciklama, "aciklama");
    }

    public async Task<object> Olustur(FaturaOlusturDto dto, AktifKullanici islem)
    {
        BaslikGirdisiDenetle(dto.Tarih, dto.Aciklama);
        GirdiKapisi.Tutar(dto.Toplam, "toplam", Kuyumcu.Core.Domain.Girdi.TutarTuru.Pozitif, "Toplam");
        var cariVar = await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.Id == dto.CariId && c.TenantId == islem.TenantId);
        if (!cariVar) throw new NotFoundException("Cari bulunamadı");

        // ── İKİ AYRI KOD YOLU (Bertan'ın geriye uyum koşulu) ──────────────────
        // Kalem gönderilmeyen istek AŞAĞIDAKİ gövdeden geçer; o gövdenin tek
        // satırı bile değişmemiştir. Kalemli istek ayrı yola sapar.
        if (dto.Kalemler is { Count: > 0 })
            return await KalemYolu(dto, islem);

        // ADR-0005: oran koda gömülü DEĞİL, yapılandırmadan. Varsayılan 0.20 —
        // davranış bugünküyle aynı (paket 38 rakamı taşır, değiştirmez).
        var kdvOran = dto.KdvOran ?? ayar.VarsayilanOran;
        var kdvTuru = dto.KdvTuru ?? "STANDART";
        var toplam  = dto.Toplam;

        var id    = Kimlik.Ulid();
        var now   = DateTime.UtcNow;
        var tarih = dto.Tarih ?? now;

        // Adım 7: faturalandırılan hareket(ler) verilmişse kiracı sınırında doğrula.
        //
        // ⚠ ÇOK KALEMLİ FİŞ: bir hareket bir ödeme fişinin kalemiyse (FisId dolu),
        // fatura O FİŞİN TÜM KALEMLERİNİ damgalar. Yalnız verilen satır
        // damgalansaydı N−1 kalem damgasız kalır, gün sonu KDV mutabakatı
        // (RaporModulu "faturalandırılmamış satış" uyarısı) 3 kalemli bir fişin
        // 2 kalemini faturasız sayardı — KDV beyanının dayanağı o rapordur.
        var damgalanacak = await DamgalanacakBul(dto.HareketId, islem);

        // KDV hesabı türe göre:
        //  STANDART → KDV tüm tutara
        //  KUYUM    → has altın KDV'siz; KDV yalnız (toplam − hasDeger) matrahına
        decimal hasDeger = 0m;
        decimal kdvTutar;
        decimal? hasGram = null, hasBirimFiyat = null;
        var hasKaynagi = "ELLE";
        Kur.OzelMatrahFiyatCozumu? matrahCozum = null;   // KUR KAYNAKLARI (17.10): has bedeli fiyat kaynağı
        if (kdvTuru == "KUYUM")
        {
            if (damgalanacak.Count > 0)
            {
                // ── PAKET 38: HAS BEDELİ DEFTERDEN TÜRETİLİR ─────────────────
                // MATRAHIN KÜMESİ = DAMGANIN KÜMESİ. Ayrışsaydı damgalanmış bir
                // kalem matraha girmemiş olur, fatura ile defter çelişirdi.
                matrahCozum = await Kur.OzelMatrahKaynakCozucu.Coz(db, islem.TenantId, tarih);
                var sonuc = await HasBedeliTuret(damgalanacak, toplam, kdvOran, islem, matrahCozum);

                // TUTARLILIK KAPISI — TOLERANS YOK (OdemeSepeti'nin "bacak toplamı
                // = fiş toplamı" kuralıyla aynı; kasiyer matrahı seçemez, seçseydi
                // KDV beyanı fiilen elle yazılırdı).
                if (dto.HasDeger is { } gonderilen
                    && OzelMatrahMotoru.Yuvarla2(gonderilen) != sonuc.HasDeger)
                {
                    string P(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
                    throw new BadRequestException(
                        $"Has altın bedeli defterden {P(sonuc.HasDeger)} TL olarak türetildi, " +
                        $"gönderilen {P(gonderilen)} TL — fark {P(gonderilen - sonuc.HasDeger)} TL. " +
                        "Hareket bağlı kuyum faturasında tolerans yoktur.");
                }

                hasDeger = sonuc.HasDeger;
                kdvTutar = sonuc.KdvTutar;
                hasGram  = sonuc.HasGram;
                // Bacaklar farklı kur taşıyabilir → belgedeki özet kur AĞIRLIKLI
                // ORTALAMADIR. Hesap bacak bacak yapıldı; bu yalnız kanıt alanıdır.
                hasBirimFiyat = sonuc.HasGram > 0m
                    ? OzelMatrahMotoru.Yuvarla6(sonuc.HasDeger / sonuc.HasGram)
                    : null;
                hasKaynagi = "HAREKET";
            }
            else
            {
                // ── ELLE yol — hareket bağı yok ──────────────────────────────
                // 38-A DEVRİ (UX-1-C2): eski kod `dto.HasDeger ?? 0m` yazıyordu.
                // Sonuç: KdvTuru=KUYUM + hareket bağı YOK + HasDeger gönderilmemiş
                // bir POST'ta has_deger=0 yazılıyor ve KDV TÜM tutar üzerinden
                // hesaplanıyordu — yani kuyum faturası sessizce STANDART faturaya
                // dönüşüyor, vergi yanlış hesaplanıyordu. Ekran bu yolu kapatıyor
                // (Faturalar.razor KuyumEngeli) ama API doğrudan çağrılabilir.
                // FAIL-CLOSED: has bedeli 0 olan kuyum faturası YOKTUR; açıkça
                // gönderilen 0 da reddedilir (sessizce STANDART'a düşürülmez).
                // 17.10 P0-5 — MEVZUAT KİLİDİ: KDVK 23/e özel matrahı ürünün HAS GRAMINI ister (has bedeli =
                // has gram × kur). Gramı bilinmeyen, elle yazılmış has bedeli denetlenemez ve matrahı keyfî
                // düşürür. Bu yol API'de de KAPALI: kuyum faturası yalnız hareket bağından (satış) ya da
                // has gramı bilinen kalemlerden kesilir. (Ekran zaten KuyumEngeli ile kapatıyordu.)
                throw new BadRequestException(
                    "Kuyum faturası yalnız satış kaydından ya da has gramı bilinen kalemlerden kesilir; has bedeli elle yazılamaz.")
                    .AlanIle("hasDeger");
            }
        }
        else
        {
            kdvTutar = toplam - toplam / (1m + kdvOran);
        }
        // ZO-BELGE (29.09): ara toplam ile KDV AYRI AYRI yuvarlanınca ara + kdv
        // toplamdan 1 kuruş sapıyordu (999,99 → 833,33 + 166,67 = 1.000,00).
        // Kalem yolu ile aynı kalıp: önce KDV yuvarlanır, ara toplam FARKTIR →
        // ara_toplam + kdv_tutar == toplam her zaman (her iki türde).
        kdvTutar = OzelMatrahMotoru.Yuvarla2(kdvTutar);
        var araToplam = OzelMatrahMotoru.Yuvarla2(toplam) - kdvTutar;   // KDV hariç toplam

        // P-22 / B-3: TASLAK belge numara TÜKETMEZ. Hareketler yalnız taslak
        // İŞARETİ alır (seriden değildir); gerçek numara kesimde yazılır.
        foreach (var h in damgalanacak) h.FaturaNo = FaturaSeriKapisi.Isaret(id);

        db.Faturalar.Add(new Data.Fatura
        {
            Id             = id,
            TenantId       = islem.TenantId,
            FaturaNo       = null,   // P-22: numara KESİMDE verilir
            CariId         = dto.CariId,
            Tur            = dto.Tur ?? "SATIS",
            KdvTuru        = kdvTuru,
            HasDeger       = Math.Round(hasDeger, 2, MidpointRounding.AwayFromZero),
            AraToplam      = Math.Round(araToplam, 2, MidpointRounding.AwayFromZero),
            KdvOran        = Math.Round(kdvOran, 4, MidpointRounding.AwayFromZero),
            KdvTutar       = Math.Round(kdvTutar, 2, MidpointRounding.AwayFromZero),
            Toplam         = Math.Round(toplam, 2, MidpointRounding.AwayFromZero),
            ParaBirimi     = dto.ParaBirimi ?? "TRY",
            Durum          = "TASLAK",
            BankaHareketId = null,
            Aciklama       = dto.Aciklama,
            Tarih          = tarih,
            Olusturuldu    = now,
            Guncellendi    = now,
            // Paket 38 kanıt alanları. Kod YALNIZ KUYUM satırlarda dolar;
            // STANDART faturaya yazılsaydı yanlış beyan kaynağı olurdu.
            OzelMatrahKodu  = OzelMatrahKoduFor(kdvTuru, hasDeger),
            HasGram         = hasGram,
            HasBirimFiyat   = hasBirimFiyat,
            HasDegerKaynagi = kdvTuru == "KUYUM" ? hasKaynagi : "ELLE",
            // KUR KAYNAKLARI (17.10): has bedelinin KAYNAK KODU + kapanış günü + fiyatı belgeye yazılır.
            OzelMatrahKaynakKodu    = kdvTuru == "KUYUM" ? matrahCozum?.BelgeKodu(false) : null,
            OzelMatrahKapanisGunu   = kdvTuru == "KUYUM" ? matrahCozum?.BelgeGunu : null,
            OzelMatrahKapanisFiyati = kdvTuru == "KUYUM" ? matrahCozum?.BelgeFiyati : null,
        });
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Has bedeli türetimi (paket 38 — TEK MOTOR, ADR-0016) ────────────────

    /// <summary>
    /// YASAL-4 Y4-D (K-10b) — özel matrahın MADEN KAPSAMI. KDVK md.23/e
    /// (mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf): "Altından mamül veya altın
    /// ihtiva eden ziynet eşyaları ile sikke altınların teslim ve ithalinde
    /// matrah, külçe altın bedeli düşüldükten sonra kalan miktardır." Hüküm
    /// YALNIZ ALTIN içindir.
    /// <para>
    /// FAIL-CLOSED (mevzuat kuralı): gümüş için özel matrah 23/g yetkisiyle
    /// Tebliğ'dedir ama gümüş kapanış fiyatı kaynağı sistemde yoktur ve hüküm
    /// mali müşavirce teyit edilmedi → gümüş/platin/paladyum has bedeli
    /// matrahtan DÜŞÜLMEZ (tam bedel matraha girer; fazla KDV, eksik değil).
    /// DOĞRULANAMADI — mali müşavir teyidi (23/g gümüş).
    /// </para>
    /// </summary>
    internal static bool OzelMatrahKapsaminda(string? maden) =>
        maden == Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>
    /// Kalem ALTIN mı — TEK KAYNAK kalemin kendi <c>Maden</c> alanı (oluşurken yazılır).
    /// Maden bilinmiyorsa (NULL) özel matrah UYGULANMAZ: ayardan/ALTIN varsayımından
    /// türetilmez (ZO-BELGE 30.09 — ürünsüz gümüş hurda satırı altın sanılıyordu).
    /// </summary>
    internal static bool KalemAltinMi(Data.FaturaKalemi k) => OzelMatrahKapsaminda(k.Maden);

    /// <summary>
    /// 805 ("altından mamul / altın içeren ziynet") yalnız ALTIN has bedeli
    /// düşülen belgeye basılır; yalnız gümüş kalemli kuyum faturasına basılmaz.
    /// </summary>
    private string? OzelMatrahKoduFor(string kdvTuru, decimal altinHasDeger) =>
        kdvTuru == "KUYUM" && altinHasDeger > 0m ? ayar.KodYaDaNull : null;

    /// <summary>
    /// Verilen hareket kümesinin has bacaklarını kendi kur kayıtlarıyla çarpar ve
    /// <see cref="OzelMatrahMotoru"/>'na verir. İKİNCİ BİR MOTOR DEĞİLDİR: hesabı
    /// yapan sınıf tektir, burada yalnız defterden VERİ TOPLANIR.
    /// <para>
    /// Mehmet K38-3: kur kayıtları TEK sorguda, <c>ids.Contains + TenantId</c>
    /// şartıyla çekilir. FAIL-CLOSED: has'ı olan bir hareketin kur kaydı yoksa ya
    /// da <c>AltinHasFiyati &lt;= 0</c> ise 400 — "has bedeli 0" varsayılmaz
    /// (0 yazmak tüm tutarı matraha sokar, kuyumcuya FAZLA KDV ödetir).
    /// </para>
    /// <para>
    /// has_grami'nin İŞARETİ ÖNEMSİZDİR (satışta negatiftir); <c>Math.Abs</c>
    /// alınır. İşaret korunsaydı satış faturasında matrah toplamdan BÜYÜK çıkardı.
    /// </para>
    /// </summary>
    private async Task<OzelMatrahSonuc> HasBedeliTuret(
        List<Data.Hareket> kume, decimal toplam, decimal kdvOran, AktifKullanici islem, Kur.OzelMatrahFiyatCozumu cozum)
    {
        var bacaklar = await HasBacaklariniTopla(kume, islem, cozum);
        return OzelMatrahMotoru.Hesapla(bacaklar, toplam, kdvOran);
    }

    /// <summary>
    /// KUR KAYNAKLARI (17.10): bacağın has fiyatı <paramref name="cozum"/>'den gelir — ISLEM_KURU'da
    /// hareketin KENDİ kur kaydı (bugünkü davranış AYNEN), diğer kaynaklarda seçili kaynağın SON
    /// İŞGÜNÜ kapanışı (bulunamazsa fail-closed 400).
    /// </summary>
    private async Task<List<HasBacagi>> HasBacaklariniTopla(
        List<Data.Hareket> kume, AktifKullanici islem, Kur.OzelMatrahFiyatCozumu cozum)
    {
        // YASAL-4 Y4-D (K-10b): yalnız ALTIN bacakları özel matraha girer
        // (bkz. OzelMatrahKapsaminda). Gümüş/platin bacağı ALTIN fiyatıyla
        // çarpılıp düşülseydi KDV EKSİK hesaplanırdı.
        var hasliSatirlar = kume
            .Where(h => Math.Abs(h.HasGrami) > 0m && OzelMatrahKapsaminda(h.Maden)).ToList();
        if (hasliSatirlar.Count == 0) return [];

        var kurIdler = hasliSatirlar
            .Where(h => !string.IsNullOrEmpty(h.KurSnapshotId))
            .Select(h => h.KurSnapshotId!).Distinct().ToList();
        var kurlar = kurIdler.Count == 0
            ? new Dictionary<string, Data.KurSnapshot>()
            : await db.KurSnapshots.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && kurIdler.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id);

        var bacaklar = new List<HasBacagi>(hasliSatirlar.Count);
        foreach (var h in hasliSatirlar)
        {
            var kur = string.IsNullOrEmpty(h.KurSnapshotId)
                ? null : kurlar.GetValueOrDefault(h.KurSnapshotId);
            // HER BACAK KENDİ KUR KAYDIYLA (ISLEM_KURU): tek kur VARSAYILMAZ (fişin kalemleri
            // farklı KurSnapshotId taşıyabilir). Kapanış kaynağında hepsi aynı son işgünü kapanışı.
            bacaklar.Add(new HasBacagi(Math.Abs(h.HasGrami), cozum.Fiyat(kur?.AltinHasFiyati,
                "Faturalandırılan işlemin kur kaydı bulunamadı — has bedeli hesaplanamaz")));
        }
        return bacaklar;
    }

    // ─── Özel matrah önizlemesi (paket 38) ──────────────────────────────────

    /// <summary>
    /// SALT OKUNUR. Kaydet'e basılmadan önce: hangi hareketler faturalanacak,
    /// toplam has gram kaç, türetilen has bedeli kaç TL, (toplam verilmişse)
    /// matrah ve KDV ne olur. Kasiyer neyin faturalandığını ÖNCEDEN görür.
    /// <para>
    /// Kapsanan hareket kimlikleri de döner: fişin bir kalemi verilse bile
    /// FİŞİN TAMAMI faturalanır (B-4) ve ekran bunu göstermek zorundadır.
    /// </para>
    /// </summary>
    /// <summary>
    /// M2 (müşteri 16.10: "KDV oranını düzenlediği bir şey olacak"; kurucu 16.10: "ürünün KDV'si
    /// özelde kayıt edilmemişse faturada seçilen KDV uygulansın, KDV'si olan ürünler kendi
    /// KDV'sine göre olacak") — kalemin KDV oranı: ürün kartındaki <c>KdvOrani</c> (yüzde → kesir)
    /// &gt; faturada seçilen oran &gt; yapılandırma varsayılanı. Eskiden ürün oranı faturada HİÇ
    /// okunmuyordu (gizli hata); fatura ekranı oranı her zaman gönderdiği için ürün oranı öncelikli olmalı.
    /// </summary>
    private decimal KalemKdvOrani(decimal? secilenOran, Data.Urun? urun) =>
        urun?.KdvOrani is { } yuzde
            ? Math.Round(yuzde / 100m, 4, MidpointRounding.AwayFromZero)
            : secilenOran ?? ayar.VarsayilanOran;

    /// <summary>M2 — belge başlığı oranı: bütün kalemler aynı orandaysa o, değilse genel oran.</summary>
    private static decimal BaslikOrani(IReadOnlyCollection<decimal> kalemOranlari, decimal genel) =>
        kalemOranlari.Count > 0 && kalemOranlari.Distinct().Count() == 1 ? kalemOranlari.First() : genel;

    public async Task<object> OzelMatrahOnizleme(
        string hareketId, decimal? kdvOran, decimal? toplam, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(hareketId))
            throw new BadRequestException("hareketId verilmelidir");
        var oran = kdvOran ?? ayar.VarsayilanOran;
        if (oran is < 0m or > 1m) throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır");

        // Kiracı sınırı ELLE de yazılır (ADR-0010); yabancı hareket 404.
        var hareket = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Hareket bulunamadı");

        var kume = string.IsNullOrEmpty(hareket.FisId)
            ? [hareket]
            : await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && h.FisId == hareket.FisId)
                .OrderBy(h => h.Olusturuldu).ThenBy(h => h.Id)
                .ToListAsync();

        // M2 — kümenin kalem oranları (ürün oranı > seçilen > varsayılan) ortaksa o (kaydetmeyle AYNI kural).
        decimal? kumeBinde = null;
        {
            var kumeUrunIdler = kume.Where(h => !string.IsNullOrEmpty(h.UrunId)).Select(h => h.UrunId!).Distinct().ToList();
            var kumeUrunleri = kumeUrunIdler.Count == 0 ? new Dictionary<string, Data.Urun>()
                : await db.Urunler.AsNoTracking()
                    .Where(u => u.TenantId == islem.TenantId && kumeUrunIdler.Contains(u.Id))
                    .Take(kumeUrunIdler.Count)   // B-8: kümenin ürün sayısıyla sınırlı
                    .ToDictionaryAsync(u => u.Id);
            oran = BaslikOrani(kume.Select(h => KalemKdvOrani(kdvOran,
                string.IsNullOrEmpty(h.UrunId) ? null : kumeUrunleri.GetValueOrDefault(h.UrunId))).ToList(), oran);
            // M2b — kümenin bütün kalemlerinde aynı binde pay varsa önizleme de o yöntemle.
            var bindeler = kume.Select(h => string.IsNullOrEmpty(h.UrunId) ? null
                : kumeUrunleri.GetValueOrDefault(h.UrunId)?.KdvMatrahBinde).Distinct().ToList();
            kumeBinde = bindeler.Count == 1 ? bindeler[0] : null;
        }

        // KUR KAYNAKLARI (17.10): önizleme kaydetmeyle AYNI kaynağı kullanır (bugünün faturası için).
        var matrahCozum = await Kur.OzelMatrahKaynakCozucu.Coz(db, islem.TenantId, DateTime.UtcNow);
        var bacaklar = kumeBinde is null
            ? await HasBacaklariniTopla(kume, islem, matrahCozum)
            : await HasBacaklariniTopla(kume, islem, Kur.OzelMatrahKaynakCozucu.IslemKuruCozumu(DateTime.UtcNow));
        var (hasGram, hasDeger) = OzelMatrahMotoru.HasBedeli(bacaklar);

        // Zaten damgalı bir küme faturalanamaz; ÖNİZLEME bunu HATA olarak değil
        // BİLGİ olarak döndürür ki ekran sebebi yazabilsin (UX-S30).
        // P-08 / Y-25 — önizleme de aynı TEK YOLU kullanır; ekranın gördüğü
        // sebep ile kaydetmede çıkan hata AYNI kaynaktan gelir (UX-S30).
        var damgali = (await FaturaHareketBagi.IlkBag(db, islem.TenantId, kume))?.FaturaNo;

        decimal? matrah = null, kdvTutar = null, araToplam = null;
        string? uyari = null;
        if (toplam is { } t && t > 0m)
        {
            if (kumeBinde is null && hasDeger >= t)
                uyari = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Has altın bedeli toplam tutardan küçük olmalıdır");
            else
            {
                var sonuc = OzelMatrahMotoru.Hesapla(bacaklar, t, oran, kumeBinde);
                matrah = sonuc.Matrah;
                kdvTutar = sonuc.KdvTutar;
                araToplam = sonuc.AraToplam;
                uyari = sonuc.Uyari;
            }
        }
        else if (bacaklar.Count == 0)
        {
            uyari = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Has bacağı yok — özel matrah uygulanmadı");
        }

        return new
        {
            hareketId = hareket.Id,
            fisId = hareket.FisId,
            kapsananHareketIdler = kume.Select(h => h.Id).ToList(),
            kapsananHareketSayisi = kume.Count,
            hasGram,
            hasDeger,
            // Belgedeki özet kur: ağırlıklı ortalama (bacaklar farklı kur taşıyabilir).
            hasBirimFiyat = hasGram > 0m
                ? OzelMatrahMotoru.Yuvarla6(hasDeger / hasGram)
                : (decimal?)null,
            kullanilanKurSayisi = bacaklar.Select(b => b.HasBirimFiyat).Distinct().Count(),
            defterToplami = OzelMatrahMotoru.Yuvarla2(kume.Sum(h => Math.Abs(h.TlTutari))),
            kdvOran = oran,
            matrah,
            kdvTutar,
            araToplam,
            hasDegerKaynagi = "HAREKET",
            // KUR KAYNAKLARI (17.10): has bedelinin kaynak kodu + kullanılan kapanış (kaydetmeyle aynı).
            ozelMatrahKaynakKodu = kumeBinde is null ? matrahCozum.BelgeKodu(false) : Kur.KurKaynakKodlari.Binde,
            ozelMatrahKapanisGunu = kumeBinde is null ? matrahCozum.BelgeGunu : null,
            ozelMatrahKapanisFiyati = kumeBinde is null ? matrahCozum.BelgeFiyati : null,
            faturaNo = damgali,
            uyari,
            // Y4-D: kümede has bacağı olan altın DIŞI (gümüş/platin/paladyum) satır var
            // mı — ekran "altın dışı madende özel matrah uygulanmaz" notunu YALNIZ bu
            // durumda gösterir (zorunlu-önce). Kaynak: defter satırının madeni.
            altinDisiMadenVar = kume.Any(h => Math.Abs(h.HasGrami) > 0m && !OzelMatrahKapsaminda(h.Maden)),
        };
    }

    /// <summary>
    /// Faturalandırılabilir (henüz damgalanmamış) son işlemler — ekrandaki
    /// "faturalandırılacak işlem" seçicisinin kaynağı.
    /// <para>
    /// TB-19: sınırsız liste yazılmaz; <paramref name="limit"/> yanıtta da döner
    /// ki ekran "daha fazlası var" diyebilsin (UX-S29 — sessiz kırpma yok).
    /// </para>
    /// </summary>
    public async Task<object> FaturalanabilirIslemler(
        AktifKullanici islem, int gun, int limit)
    {
        gun = Math.Clamp(gun, 1, 365);
        limit = Math.Clamp(limit, 1, 200);
        var esik = DateTime.UtcNow.AddDays(-gun);

        // P-08 / Y-25 — "faturalanmamış" tanımı TEK YOLDAN gelir; damga koşulu
        // artık burada değil FaturaHareketBagi içindedir.
        var aday = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId
                     && (h.Tur == "SATIS" || h.Tur == "ALIS")
                     && h.Tarih >= esik);

        var satirlar = await FaturaHareketBagi.Bagsiz(db, islem.TenantId, aday)
            .OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id)
            .Take(limit + 1)
            .ToListAsync();

        var dahaVar = satirlar.Count > limit;
        if (dahaVar) satirlar.RemoveAt(satirlar.Count - 1);

        var urunIdler = satirlar.Where(h => !string.IsNullOrEmpty(h.UrunId))
            .Select(h => h.UrunId!).Distinct().ToList();
        var urunAdlari = urunIdler.Count == 0
            ? new Dictionary<string, string>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Ad);

        return new
        {
            gun,
            limit,
            dahaVar,
            islemler = satirlar.Select(h => new
            {
                id = h.Id,
                h.FisId,
                h.Tur,
                h.Tarih,
                tlTutari = OzelMatrahMotoru.Yuvarla2(Math.Abs(h.TlTutari)),
                hasGrami = OzelMatrahMotoru.Yuvarla6(Math.Abs(h.HasGrami)),
                h.CariId,
                urunAd = h.UrunId is null ? null : urunAdlari.GetValueOrDefault(h.UrunId),
            }).ToList(),
        };
    }

    // ─── Damga kümesi (iki yol da BU metodu kullanır; kopyalanmaz) ────────────
    //
    // ⚠ ÇOK KALEMLİ FİŞ: bir hareket bir ödeme fişinin kalemiyse (FisId dolu),
    // fatura O FİŞİN TÜM KALEMLERİNİ damgalar (B-4). Yalnız verilen satır
    // damgalansaydı N−1 kalem damgasız kalır, gün sonu KDV mutabakatı 3 kalemli
    // bir fişin 2 kalemini faturasız sayardı.
    //
    // Mehmet K1-1: hareket kümesi HER ZAMAN `h.TenantId == islem.TenantId`
    // şartıyla çekilir; yabancı kiracının hareketi 404'tür.
    private async Task<List<Data.Hareket>> DamgalanacakBul(string? hareketId, AktifKullanici islem)
    {
        if (string.IsNullOrEmpty(hareketId)) return [];

        var hareket = await db.Hareketler
            .FirstOrDefaultAsync(h => h.Id == hareketId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Hareket bulunamadı");

        var damgalanacak = string.IsNullOrEmpty(hareket.FisId)
            ? [hareket]
            : await db.Hareketler
                .Where(h => h.TenantId == islem.TenantId && h.FisId == hareket.FisId)
                .ToListAsync();

        return await DamgaKontrol(damgalanacak, islem, tekHareket: string.IsNullOrEmpty(hareket.FisId));
    }

    /// <summary>
    /// Fişin TÜM hareketleri, kiracı sınırında (Mehmet K1-1). Fiş kimliği yoksa
    /// ya da kiracıya ait değilse 404 — yabancı kiracının fişi "boş fiş" değildir.
    /// </summary>
    private async Task<List<Data.Hareket>> FisHareketleriBul(string fisId, AktifKullanici islem)
    {
        var liste = await db.Hareketler
            .Where(h => h.TenantId == islem.TenantId && h.FisId == fisId)
            .OrderBy(h => h.Olusturuldu).ThenBy(h => h.Id)
            .ToListAsync();
        if (liste.Count == 0) throw new NotFoundException("Fiş bulunamadı");

        return await DamgaKontrol(liste, islem, tekHareket: false);
    }

    /// <summary>
    /// BİR HAREKET / BİR FİŞ YALNIZ BİR FATURAYA BAĞLANIR. Kümenin HERHANGİ bir
    /// kalemi damgalıysa fiş zaten faturalanmıştır (mevcut mesaj metni korunur).
    /// </summary>
    /// <remarks>
    /// P-08 / Y-25 — cevap TEK YOLDAN okunur (<see cref="FaturaHareketBagi"/>).
    /// ⚠ Eski hâl yalnız <c>hareketler.fatura_no</c> damgasına bakıyordu; kalemi
    /// bu harekete BAĞLI ama damgası (iptal/elle düzeltme yüzünden) düşmüş bir
    /// belge varsa aynı satış İKİNCİ KEZ faturalanabiliyordu. Otorite artık
    /// <c>fatura_kalemleri.hareket_id</c>'dir; İPTAL edilmiş fatura bağ sayılmaz.
    /// </remarks>
    private async Task<List<Data.Hareket>> DamgaKontrol(
        List<Data.Hareket> kume, AktifKullanici islem, bool tekHareket)
    {
        var bag = await FaturaHareketBagi.IlkBag(db, islem.TenantId, kume);
        // TOPLU-1 tur 2 (doğrulayıcı DÜŞÜK): TASLAK belgenin numarası yoktur — eskiden cümle
        // "zaten  ile faturalandırılmış" (boş numara, çift boşluk) olarak ekrana çıkıyordu.
        if (bag is not null)
            throw new BadRequestException(
                string.IsNullOrWhiteSpace(bag.FaturaNo)
                    ? (tekHareket
                        ? "Bu hareket zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)"
                        : "Bu fişin kalemleri zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)")
                    : tekHareket
                        ? $"Bu hareket zaten {bag.FaturaNo} ile faturalandırılmış"
                        : $"Bu fişin kalemleri zaten {bag.FaturaNo} ile faturalandırılmış");

        return kume;
    }

    // ─── Kalemli fatura yolu (paket 1 Faz A) ─────────────────────────────────

    /// <summary>TB-19: sınırsız liste yazılmaz.</summary>
    public const int KalemUstSiniri = 200;

    /// <summary>
    /// Kalemli fatura. TOPLAM SUNUCUDA HESAPLANIR, TOLERANS YOKTUR; KUYUM'da
    /// has bedeli ELLE GİRİLMEZ, ürün kartındaki has gram × kur kaydındaki has
    /// fiyatından türetilir (OzelMatrahMotoru — tek motor).
    /// <para>
    /// ⚠ DTO'daki DataAnnotations YALNIZ controller yolunda çalışır (mimari
    /// değişmez). Bu metot servis içinden de çağrılabildiği için sınırlar
    /// BURADA ELLE doğrulanır (Mehmet K1-2).
    /// </para>
    /// </summary>
    private async Task<object> KalemYolu(FaturaOlusturDto dto, AktifKullanici islem)
    {
        var girdiler = dto.Kalemler!;
        for (var gi = 0; gi < girdiler.Count; gi++) KalemGirdisiDenetle(girdiler[gi], $"{gi + 1}. kalem");
        if (girdiler.Count > KalemUstSiniri)
            throw new BadRequestException(
                $"Bir faturada en çok {KalemUstSiniri} kalem olabilir; {girdiler.Count} kalem gönderildi");

        var kdvOran = dto.KdvOran ?? ayar.VarsayilanOran;
        var kdvTuru = dto.KdvTuru ?? "STANDART";
        if (kdvOran is < 0m or > 1m) throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır");

        for (var i = 0; i < girdiler.Count; i++)
        {
            var g = girdiler[i];
            // PAKET PATCH-2: kontrol KOLONA YAZILACAK değerle (3 hane, AwayFromZero) — 0,0004 → 0,000 olur.
            if (Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero) <= 0m)
                throw new BadRequestException($"{i + 1}. kalemde miktar sıfırdan büyük olmalıdır").AlanIle("kalemler");
            if (g.BirimFiyat < 0m) throw new BadRequestException($"{i + 1}. kalemde birim fiyat negatif olamaz");
            if (g.IscilikTl < 0m) throw new BadRequestException($"{i + 1}. kalemde işçilik negatif olamaz");
            if (g.IndirimTutari < 0m) throw new BadRequestException($"{i + 1}. kalemde indirim negatif olamaz");
            if (g.Aciklama is { Length: > 300 }) throw new BadRequestException($"{i + 1}. kalemde açıklama 300 karakteri aşamaz");
        }

        // ── Ürünler TEK sorguda (N+1 yok) ─────────────────────────────────────
        var urunIdler = girdiler.Where(g => !string.IsNullOrEmpty(g.UrunId))
            .Select(g => g.UrunId!).Distinct().ToList();
        var urunler = urunIdler.Count == 0
            ? new Dictionary<string, Data.Urun>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        if (urunler.Count != urunIdler.Count)
            throw new NotFoundException("Ürün bulunamadı");

        // ── Kalem hareketleri TEK sorguda, KİRACI SINIRINDA (Mehmet K1-1) ─────
        var hareketIdler = girdiler.Where(g => !string.IsNullOrEmpty(g.HareketId))
            .Select(g => g.HareketId!).Distinct().ToList();
        var hareketler = hareketIdler.Count == 0
            ? new Dictionary<string, Data.Hareket>()
            : await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && hareketIdler.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id);
        if (hareketler.Count != hareketIdler.Count)
            throw new NotFoundException("Hareket bulunamadı");

        // ── Kur kayıtları (yalnız KUYUM'da gerekir) ───────────────────────────
        var kurIdler = hareketler.Values.Where(h => !string.IsNullOrEmpty(h.KurSnapshotId))
            .Select(h => h.KurSnapshotId!).Distinct().ToList();
        var kurlar = kurIdler.Count == 0
            ? new Dictionary<string, Data.KurSnapshot>()
            : await db.KurSnapshots.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && kurIdler.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id);
        // Harekete bağlı olmayan kalemler için kiracının EN SON kur kaydı.
        var sonKur = kdvTuru != "KUYUM" ? null : await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .OrderByDescending(k => k.Olusturuldu).FirstOrDefaultAsync();

        var faturaId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var tarih = dto.Tarih ?? now;
        // KUR KAYNAKLARI (17.10): belgenin TEK has bedeli fiyat kaynağı (kalemler karışmaz).
        var matrahCozum = kdvTuru == "KUYUM" ? await Kur.OzelMatrahKaynakCozucu.Coz(db, islem.TenantId, tarih) : null;
        var bindeKullanildi = false;

        var kalemler = new List<Data.FaturaKalemi>(girdiler.Count);
        for (var i = 0; i < girdiler.Count; i++)
        {
            var g = girdiler[i];
            var urun = string.IsNullOrEmpty(g.UrunId) ? null : urunler[g.UrunId];
            var kalemOran = KalemKdvOrani(dto.KdvOran, urun);   // M2
            var hareket = string.IsNullOrEmpty(g.HareketId) ? null : hareketler[g.HareketId];

            var satirToplam = OzelMatrahMotoru.Yuvarla2(
                g.Miktar * g.BirimFiyat + (g.IscilikTl ?? 0m) - (g.IndirimTutari ?? 0m));
            if (satirToplam <= 0m)
                throw new BadRequestException($"{i + 1}. kalemin satır toplamı sıfırdan büyük olmalıdır");

            // ── Has bacağı: KUYUM'da ZORUNLU, FAIL-CLOSED ─────────────────────
            // Kalemin madeni BİR KEZ belirlenir ve kaleme yazılır (tek kaynak).
            var kalemMaden = urun?.Maden;
            var bacaklar = new List<HasBacagi>();
            decimal? kalemKuru = null;   // has bedeli dayanağı — kur KAYDININ fiyatı
            if (kdvTuru == "KUYUM")
            {
                if (urun is null)
                    throw new BadRequestException(
                        $"{i + 1}. kalemde ürün seçilmedi — kuyum KDV'sinde has bedeli ürün kartından türetilir");

                var kur = hareket is not null && !string.IsNullOrEmpty(hareket.KurSnapshotId)
                    ? kurlar.GetValueOrDefault(hareket.KurSnapshotId)
                    : sonKur;
                // Y4-D: altın dışı ürün kaleminde has bedeli düşülmez (kur da gerekmez).
                // M2b: binde yöntemli üründe has fiyatı KULLANILMAZ — kapanış aranmaz (bugünkü kural).
                if (OzelMatrahKapsaminda(kalemMaden) && urun.KdvMatrahBinde is > 0m)
                {
                    bindeKullanildi = true;
                    if (kur is { AltinHasFiyati: > 0m })
                    {
                        bacaklar.Add(new HasBacagi(
                            KalemToplamHas(Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero), urun.HasGrami)!.Value,
                            kur.AltinHasFiyati));
                        kalemKuru = kur.AltinHasFiyati;
                    }
                    else
                        throw new BadRequestException(
                            $"{i + 1}. kalem için kur kaydı bulunamadı — has bedeli hesaplanamaz");
                }
                else if (OzelMatrahKapsaminda(kalemMaden))
                {
                    // Değişmez: has bedeli = round2(KALEME YAZILAN has gram × dayanak fiyat) — PATCH yoluyla aynı.
                    // KUR KAYNAKLARI: dayanak fiyat = ISLEM_KURU'da kur kaydı, diğerlerinde son işgünü kapanışı.
                    var dayanak = matrahCozum!.Fiyat(kur?.AltinHasFiyati,
                        $"{i + 1}. kalem için kur kaydı bulunamadı — has bedeli hesaplanamaz");
                    bacaklar.Add(new HasBacagi(
                        KalemToplamHas(Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero), urun.HasGrami)!.Value,
                        dayanak));
                    kalemKuru = dayanak;
                }
            }

            var sonuc = OzelMatrahMotoru.Hesapla(bacaklar, satirToplam, kalemOran, kdvTuru == "KUYUM" ? urun?.KdvMatrahBinde : null);

            kalemler.Add(new Data.FaturaKalemi
            {
                Id            = Kimlik.Ulid(),
                TenantId      = islem.TenantId,
                FaturaId      = faturaId,
                Sira          = i + 1,
                HareketId     = g.HareketId,
                UrunId        = g.UrunId,
                Aciklama      = (string.IsNullOrWhiteSpace(g.Aciklama) ? urun?.Ad : g.Aciklama) ?? "Kalem",
                Miktar        = Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero),
                Birim         = urun?.Birim ?? "ADET",
                AyarId        = urun?.AyarId,
                Milyem        = urun?.Milyem,
                GramAgirlik   = urun?.GramAgirlik,
                Maden         = kalemMaden,
                // TEK ANLAM (30.09): kalem.HasGrami = SATIRIN TOPLAM has gramı
                // (fişten yoluyla aynı) — birim has × miktar.
                HasGrami      = KalemToplamHas(Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero), urun?.HasGrami),
                BirimHasGrami = urun?.HasGrami,   // PAKET CARI-BAKIYE: taslak düzeltmesinin tabanı
                // Taş ibaresi METİN olarak KOPYALANIR; ürün kartı sonradan
                // düzeltilse de basılmış fatura değişmez (md.11/(e)).
                TasIbaresi    = urun?.TasIbaresi,
                BirimFiyat    = OzelMatrahMotoru.Yuvarla2(g.BirimFiyat),
                IscilikTl     = g.IscilikTl,
                IndirimTutari = g.IndirimTutari,
                KdvTuru       = kdvTuru,
                KdvOran       = Math.Round(kalemOran, 4, MidpointRounding.AwayFromZero),
                HasDeger      = sonuc.HasDeger,
                HasBirimFiyat = kalemKuru,
                KdvMatrah     = sonuc.Matrah,
                KdvTutar      = sonuc.KdvTutar,
                SatirToplam   = satirToplam,
                Olusturuldu   = now,
            });
        }

        // ── Toplam KALEMLERDEN toplanır; her kalem ayrı yuvarlandı ────────────
        var toplam    = kalemler.Sum(k => k.SatirToplam);
        var kdvTutar  = kalemler.Sum(k => k.KdvTutar);
        var hasDeger  = kalemler.Sum(k => k.HasDeger);
        var araToplam = toplam - kdvTutar;

        // TOLERANS YOK (OdemeSepeti'nin "bacak toplamı = fiş toplamı" kuralının
        // fatura karşılığı). Fark mesajda YAZILIR.
        if (OzelMatrahMotoru.Yuvarla2(dto.Toplam) != toplam)
        {
            string P(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            throw new BadRequestException(
                $"Kalem toplamı {P(toplam)} TL, gönderilen toplam {P(dto.Toplam)} TL — " +
                $"fark {P(dto.Toplam - toplam)} TL. Kalemli faturada tolerans yoktur.");
        }

        var damgalanacak = await DamgalanacakBul(dto.HareketId, islem);
        // P-22 / B-3: TASLAK belge numara TÜKETMEZ. Hareketler yalnız taslak
        // İŞARETİ alır (seriden değildir); gerçek numara kesimde yazılır.
        foreach (var h in damgalanacak) h.FaturaNo = FaturaSeriKapisi.Isaret(faturaId);

        db.Faturalar.Add(new Data.Fatura
        {
            Id              = faturaId,
            TenantId        = islem.TenantId,
            FaturaNo        = null,   // P-22: numara KESİMDE verilir
            CariId          = dto.CariId,
            Tur             = dto.Tur ?? "SATIS",
            KdvTuru         = kdvTuru,
            // KALEM OTORİTEDİR, başlık türetilmiştir (açık sorunun kararı).
            HasDeger        = hasDeger,
            AraToplam       = araToplam,
            KdvOran         = Math.Round(BaslikOrani(kalemler.Select(x => x.KdvOran).ToList(), kdvOran), 4, MidpointRounding.AwayFromZero),
            KdvTutar        = kdvTutar,
            Toplam          = toplam,
            ParaBirimi      = dto.ParaBirimi ?? "TRY",
            Durum           = "TASLAK",
            BankaHareketId  = null,
            Aciklama        = dto.Aciklama,
            Tarih           = tarih,
            Olusturuldu     = now,
            Guncellendi     = now,
            KalemKaynagi    = "KALEM",
            HasDegerKaynagi = "KALEM",
            // Paket 38 kanıt alanları — kalemli yolda kalemlerden toplanır.
            OzelMatrahKodu  = OzelMatrahKoduFor(kdvTuru, hasDeger),
            HasGram         = kdvTuru == "KUYUM" ? KalemHasGrami(kalemler) : null,
            HasBirimFiyat   = kdvTuru == "KUYUM" ? OzetKur(hasDeger, KalemHasGrami(kalemler)) : null,
            // KUR KAYNAKLARI (17.10): has bedelinin kaynak kodu + kapanış günü + fiyatı.
            OzelMatrahKaynakKodu    = matrahCozum?.BelgeKodu(bindeKullanildi),
            OzelMatrahKapanisGunu   = matrahCozum?.BelgeGunu,
            OzelMatrahKapanisFiyati = matrahCozum?.BelgeFiyati,
        });
        db.FaturaKalemleri.AddRange(kalemler);
        await db.SaveChangesAsync();

        return await Getir(faturaId, islem);
    }

    /// <summary>Kalemlerin toplam has gramı (işaretsiz) — bedelin dayanağı.</summary>
    /// <summary>
    /// Kalemin has gramı TEK ANLAM taşır: SATIRIN TOPLAM has gramı (belgedeki has
    /// gram satır toplamıdır; fişten yolu defterdeki hareketin toplamını yazar).
    /// Ürün yollarında birim has × miktar; birim has bilinmiyorsa NULL.
    /// </summary>
    public static decimal? KalemToplamHas(decimal miktar, decimal? birimHas) =>
        birimHas is { } b ? OzelMatrahMotoru.Yuvarla6(miktar * b) : null;

    private static decimal KalemHasGrami(List<Data.FaturaKalemi> kalemler) =>
        OzelMatrahMotoru.Yuvarla6(kalemler.Where(KalemAltinMi).Sum(k => Math.Abs(k.HasGrami ?? 0m)));

    /// <summary>
    /// Belgedeki özet kur = has bedeli / has gram. Bacaklar farklı kur kaydı
    /// taşıyabildiği için bu AĞIRLIKLI ORTALAMADIR; hesabın kendisi bacak bacak
    /// yapılır (tek kur varsayılmaz).
    /// </summary>
    private static decimal? OzetKur(decimal hasDeger, decimal hasGram) =>
        hasGram > 0m ? OzelMatrahMotoru.Yuvarla6(hasDeger / hasGram) : null;

    // ─── Fişten fatura (paket 1 Faz A — bağlayıcı çelişki çözümü) ────────────

    /// <summary>
    /// Fişten faturada kalemin MİKTARI — ZO-BELGE 30.09 (keşif K01, KRİTİK).
    /// </summary>
    /// <remarks>
    /// Eski "ADET=1 SÖZLEŞMESİ" <c>hareketler</c> tablosunda adet kolonu yokken
    /// yazılmıştı; kolon P-08/Y-14 ile AÇILDI (<see cref="Data.Hareket.Adet"/>) ama
    /// buradaki satır güncellenmemişti: 250 adetlik satış faturaya "1 ADET ×
    /// 1.095.125" olarak giriyordu. VUK md.230/4: faturada "Malın veya işin nev'i,
    /// miktarı, fiyatı ve tutarı" bulunur (mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf).
    /// Kaynak sırası, iade ucuyla (SatisServisi.OrijinalAdet / iade-adaylari) AYNI:
    /// (1) defterdeki adet; (2) yoksa |hareket has| ÷ ürün kartının birim has'ı
    /// (gramlı üründe "1 adet = 1 gram" sözleşmesiyle sonuç GRAMDIR); (3) ürünsüz
    /// ya da birim has'ı bilinmeyen satırda 1 (hizmet/işçilik gibi tek kalem).
    /// </remarks>
    public static decimal FistenKalemMiktari(Data.Hareket h, decimal? urunBirimHas, string? urunBirimi = "ADET")
    {
        if (h.Adet is { } adet && adet > 0m) return adet;
        if (urunBirimHas is { } birimHas && birimHas > 0m && h.HasGrami != 0m)
        {
            var turetilen = Math.Round(Math.Abs(h.HasGrami) / birimHas, 3, MidpointRounding.AwayFromZero);
            // ADET üründe türetilen değer TAM SAYI değilse ürün kartı satıştan sonra
            // değişmiştir: "0,109 adet" uydurulmaz — eski tek-kalem davranışına düşülür.
            var gecerli = turetilen > 0m && (urunBirimi == "GRAM" || turetilen == decimal.Truncate(turetilen));
            if (gecerli) return turetilen;
        }
        return 1m;
    }

    /// <summary>
    /// Fişten kalemde BİRİM FİYAT ve kuruş yuvarlama indirimi (ZO-BELGE 30.09).
    /// Kalem kuralı TEK: <c>satır toplamı = Yuvarla2(miktar × birim fiyat) − indirim</c>
    /// ve satır toplamı DEFTERDEKİ TL tutarına eşittir (toplam değişmez). Tutar
    /// miktara kuruşu kuruşuna bölünüyorsa indirim yoktur; bölünmüyorsa birim fiyat
    /// kuruşa YUKARI yuvarlanır ve aradaki fark (miktar × 0,01 TL'den az) kalemde
    /// açıkça "indirim" olarak yazılır — gizli fark yok.
    /// </summary>
    public static (decimal BirimFiyat, decimal? YuvarlamaIndirimi) FistenKalemFiyati(decimal tutar, decimal miktar)
    {
        tutar = OzelMatrahMotoru.Yuvarla2(Math.Abs(tutar));
        if (miktar <= 0m) miktar = 1m;
        var birim = OzelMatrahMotoru.Yuvarla2(tutar / miktar);
        if (OzelMatrahMotoru.Yuvarla2(miktar * birim) == tutar) return (birim, null);
        birim = Math.Ceiling(tutar / miktar * 100m) / 100m;
        var fark = OzelMatrahMotoru.Yuvarla2(miktar * birim) - tutar;
        return (birim, fark > 0m ? fark : null);
    }

    /// <summary>ZO-BELGE 29.09 (doğrulayıcı ORTA): fişin hareket türlerinden fatura türü.
    /// Bütün kalemler alış (ALIS, HURDA_ALIS) → "ALIS"; bütünü satış (SATIS) → "SATIS";
    /// boş, karışık ya da başka türlü fiş → null (çağıran eski varsayılanı uygular).
    /// Ekrandaki <c>Faturalar.FistenTur</c> ile aynı kural (tek anlam iki uçta).</summary>
    public static string? FistenTurTuret(IEnumerable<string> turler)
    {
        var liste = turler.ToList();
        if (liste.Count == 0) return null;
        if (liste.All(t => t is "ALIS" or "HURDA_ALIS")) return "ALIS";
        if (liste.All(t => t == "SATIS")) return "SATIS";
        return null;
    }

    /// <summary>
    /// Fişin (ya da tek hareketin) defter satırlarından fatura üretir.
    /// <para>
    /// FİŞİN TÜM KALEMLERİ GİRER: eksik kalem seçilemez, yoksa 3 kalemli fişin
    /// 2 kalemi faturasız kalır ve gün sonu KDV mutabakatı her gün yanlış alarm
    /// verir (B-4). Has bedeli SUNUCUDA türetilir (OzelMatrahMotoru — tek motor);
    /// istemci gönderemez. Hediye kalemi faturaya 0 BEDELLE + indirim satırıyla
    /// girer (KDV GUT I/B-10.1'in "yanında bedelli mal vardır" savunması belgeye
    /// yansısın diye; defter sözleşmesi SatisModulu.cs'te ÖLÇÜLDÜ: hediyede
    /// tl_tutari = 0, liste_fiyati = liste, indirim_tutari = liste × adet).
    /// </para>
    /// <para>
    /// ⚠ DataAnnotations yalnız controller yolunda çalışır; sınırlar BURADA ELLE
    /// doğrulanır (Mehmet K1-2).
    /// </para>
    /// </summary>
    public async Task<object> FistenOlustur(FaturaFistenDto dto, AktifKullanici islem)
    {
        BaslikGirdisiDenetle(dto.Tarih, dto.Aciklama);   // PAKET GOC-A
        var kdvOran = dto.KdvOran ?? ayar.VarsayilanOran;
        if (kdvOran is < 0m or > 1m) throw new BadRequestException("KDV oranı 0 ile 1 arasında olmalıdır");
        var kdvTuru = dto.KdvTuru ?? "STANDART";
        if (kdvTuru is not ("STANDART" or "KUYUM"))
            throw new BadRequestException("KDV türü STANDART ya da KUYUM olmalıdır");
        // Tür istemciden gelirse önce değer kümesi denetlenir; ASIL tür aşağıda FİŞTEN türetilir.
        if (dto.Tur is not (null or "SATIS" or "ALIS")) throw new BadRequestException("Fatura türü SATIS ya da ALIS olmalıdır");

        // ── Hareket kümesi: KİRACI SINIRINDA (Mehmet K1-1) ───────────────────
        List<Data.Hareket> hareketler;
        if (!string.IsNullOrWhiteSpace(dto.FisId))
            hareketler = await FisHareketleriBul(dto.FisId, islem);
        else if (!string.IsNullOrWhiteSpace(dto.HareketId))
            hareketler = await DamgalanacakBul(dto.HareketId, islem);
        else
            throw new BadRequestException("fisId ya da hareketId verilmelidir");

        // ── Tür FİŞTEN türetilir (ZO-BELGE 29.09, doğrulayıcı ORTA; UX-S21) ─────────
        // Eskiden `dto.Tur ?? "SATIS"`: tür göndermeyen bir istemci alış fişinden SATIŞ
        // faturası kesiyor, satış fişine "ALIS" gönderen de kabul ediliyordu. Şimdi fişin
        // bütün kalemleri alışsa ALIS, bütünü satışsa SATIS türetilir; çelişen tür 400.
        // Karışık/başka türlü fişte eski davranış korunur (dto.Tur ?? SATIS).
        var fistenTur = FistenTurTuret(hareketler.Select(h => h.Tur));
        if (dto.Tur is not null && fistenTur is not null && dto.Tur != fistenTur)
            throw new BadRequestException(fistenTur == "ALIS"
                ? "Bu fiş bir alış fişidir; satış faturası olarak kesilemez"
                : "Bu fiş bir satış fişidir; alış faturası olarak kesilemez");
        var tur = dto.Tur ?? fistenTur ?? "SATIS";

        if (hareketler.Count > KalemUstSiniri)
            throw new BadRequestException(
                $"Bir faturada en çok {KalemUstSiniri} kalem olabilir; fişte {hareketler.Count} kalem var");

        // ── Cari: verilmezse fişten; fişte de yoksa FAIL-CLOSED ──────────────
        var cariId = string.IsNullOrWhiteSpace(dto.CariId)
            ? hareketler.Select(h => h.CariId).FirstOrDefault(c => !string.IsNullOrEmpty(c))
            : dto.CariId;
        if (string.IsNullOrEmpty(cariId))
            throw new BadRequestException("Fişte cari yok — fatura için cari verilmelidir");
        var cariVar = await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.Id == cariId && c.TenantId == islem.TenantId);
        if (!cariVar) throw new NotFoundException("Cari bulunamadı");

        // ── Ürünler ve kur kayıtları TEK sorguda (N+1 yok) ───────────────────
        var urunIdler = hareketler.Where(h => !string.IsNullOrEmpty(h.UrunId))
            .Select(h => h.UrunId!).Distinct().ToList();
        var urunler = urunIdler.Count == 0
            ? new Dictionary<string, Data.Urun>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var kurIdler = hareketler.Where(h => !string.IsNullOrEmpty(h.KurSnapshotId))
            .Select(h => h.KurSnapshotId).Distinct().ToList();
        var kurlar = kurIdler.Count == 0
            ? new Dictionary<string, Data.KurSnapshot>()
            : await db.KurSnapshots.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && kurIdler.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id);

        var faturaId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var tarih = dto.Tarih ?? now;
        // KUR KAYNAKLARI (17.10): belgenin TEK has bedeli fiyat kaynağı.
        var matrahCozum = kdvTuru == "KUYUM" ? await Kur.OzelMatrahKaynakCozucu.Coz(db, islem.TenantId, tarih) : null;
        var bindeKullanildi = false;

        var kalemler = new List<Data.FaturaKalemi>(hareketler.Count);
        var altinHasGram = 0m;   // Y4-D: başlık has gramı — yalnız altın bacakları
        for (var i = 0; i < hareketler.Count; i++)
        {
            var h = hareketler[i];
            var urun = string.IsNullOrEmpty(h.UrunId) ? null : urunler.GetValueOrDefault(h.UrunId);

            // HEDİYE: 0 bedel + indirim satırı. İndirim defterden OKUNUR (liste × adet),
            // varsayılmaz; defterde yoksa liste fiyatı kullanılır.
            //
            // ⚠ FAIL-CLOSED (UX-4c, dalga 1 doğrulayıcısının taşınan bulgusu (a)):
            // liste_fiyati NULL ise eskiden birim fiyat 0, indirim 0 yazılıyor ve
            // satır toplamı 0 çıktığı için AŞAĞIDAKİ hediye kapısı da SESSİZCE
            // geçiyordu. Sonuç: faturada bedeli hiç görünmeyen bir hediye kalemi
            // — GUT I/B-10.1'in "yanında bedelli mal vardır" savunması belgeyle
            // desteklenemez hâle geliyordu. Defter sözleşmesi (SatisModulu)
            // hediyede liste_fiyati'nı ZATEN yazar; yazılmamışsa veri eksiktir,
            // varsayılmaz — durulur ve söylenir (Anayasa md.1).
            // Kalem BELGEDE görünür (liste fiyatıyla), NET BEDELİ 0'dır: indirim
            // satırı liste × adet kadardır. Görünmeseydi GUT I/B-10.1'in
            // "yanında bedelli mal vardır" savunması belgeyle desteklenemezdi.
            var hediye = h.Hediye;
            if (hediye && (h.ListeFiyati is null || h.ListeFiyati <= 0m))
                throw new BadRequestException(
                    $"{i + 1}. kalemde hediye kaleminde liste fiyatı yok — hediye kalemi liste fiyatı olmadan faturalanamaz");
            // K01 (30.09): miktar defterden (bkz. FistenKalemMiktari) — "1 ADET" uydurulmaz.
            var miktar = FistenKalemMiktari(h, urun?.HasGrami, urun?.Birim);
            decimal birimFiyat;
            decimal? indirim;
            if (hediye)
            {
                // Liste fiyatı BİRİM fiyattır; indirim defterde "liste × adet" olarak durur.
                birimFiyat = OzelMatrahMotoru.Yuvarla2(h.ListeFiyati ?? 0m);
                indirim = OzelMatrahMotoru.Yuvarla2(h.IndirimTutari ?? (miktar * birimFiyat));
            }
            else
            {
                (birimFiyat, indirim) = FistenKalemFiyati(h.TlTutari, miktar);
            }
            var satirToplam = OzelMatrahMotoru.Yuvarla2(miktar * birimFiyat - (indirim ?? 0m));
            // İç değişmez: bedelli kalemin satır toplamı defterdeki tutardır (toplam DEĞİŞMEZ).
            if (!hediye && satirToplam != OzelMatrahMotoru.Yuvarla2(Math.Abs(h.TlTutari)))
                throw new InvalidOperationException(
                    $"{i + 1}. kalemde miktar × birim fiyat defterdeki tutarı vermiyor — fatura kesilmedi");

            if (!hediye && satirToplam <= 0m)
                throw new BadRequestException(
                    $"{i + 1}. kalemin satır toplamı sıfırdan büyük olmalıdır (bedelsiz kalem yalnız hediye olabilir)");
            if (hediye && satirToplam != 0m)
                throw new BadRequestException(
                    $"{i + 1}. kalem hediyedir; faturaya 0 bedelle girmelidir (indirim liste tutarına eşit olmalı)");

            decimal hasDeger = 0m, kdvMatrah = 0m, kdvTutar = 0m;
            decimal? kalemKuru = null;   // has bedeli dayanağı — hareketin kur KAYDI
            var kalemOran = KalemKdvOrani(dto.KdvOran, urun);   // M2
            var hasGram = OzelMatrahMotoru.Yuvarla6(Math.Abs(h.HasGrami));
            if (OzelMatrahKapsaminda(h.Maden)) altinHasGram += hasGram;

            if (satirToplam > 0m)
            {
                var bacaklar = new List<HasBacagi>();
                // Y4-D: yalnız ALTIN bacağı (defterdeki hareket madeni) düşülür.
                if (kdvTuru == "KUYUM" && hasGram > 0m && OzelMatrahKapsaminda(h.Maden))
                {
                    // FAIL-CLOSED: kur kaydı yoksa "has bedeli 0" varsayılmaz —
                    // 0 yazmak tüm tutarı matraha sokar, kuyumcuya FAZLA KDV ödetir.
                    var kur = string.IsNullOrEmpty(h.KurSnapshotId) ? null : kurlar.GetValueOrDefault(h.KurSnapshotId);
                    var kurYok = $"{i + 1}. kalem için kur kaydı bulunamadı — has bedeli hesaplanamaz";
                    decimal dayanak;
                    if (urun?.KdvMatrahBinde is > 0m)
                    {   // M2b: binde yöntemi fiyat KULLANMAZ — kapanış aranmaz; kur kapısı bugünkü gibi.
                        bindeKullanildi = true;
                        dayanak = kur is { AltinHasFiyati: > 0m } ? kur.AltinHasFiyati : throw new BadRequestException(kurYok);
                    }
                    else
                        dayanak = matrahCozum!.Fiyat(kur?.AltinHasFiyati, kurYok);   // KUR KAYNAKLARI
                    bacaklar.Add(new HasBacagi(hasGram, dayanak));
                    kalemKuru = dayanak;
                }

                var sonuc = OzelMatrahMotoru.Hesapla(bacaklar, satirToplam, kalemOran, kdvTuru == "KUYUM" ? urun?.KdvMatrahBinde : null);
                hasDeger = sonuc.HasDeger;
                kdvMatrah = sonuc.Matrah;
                kdvTutar = sonuc.KdvTutar;
            }

            kalemler.Add(new Data.FaturaKalemi
            {
                Id            = Kimlik.Ulid(),
                TenantId      = islem.TenantId,
                FaturaId      = faturaId,
                Sira          = i + 1,
                HareketId     = h.Id,
                UrunId        = h.UrunId,
                Aciklama      = urun?.Ad ?? h.Aciklama ?? h.Tur,
                Miktar        = miktar,
                Birim         = urun?.Birim ?? "ADET",
                AyarId        = urun?.AyarId,
                Milyem        = urun?.Milyem,
                GramAgirlik   = urun?.GramAgirlik,
                // Has gram DEFTERDEN gelir (ürün kartı sonradan değişebilir).
                HasGrami      = hasGram,
                // Maden DEFTERDEN gelir (ürünsüz hurda satırında da dolu) — tek kaynak.
                Maden         = h.Maden,
                // Taş ibaresi METİN olarak KOPYALANIR (md.11/(e)).
                TasIbaresi    = urun?.TasIbaresi,
                BirimFiyat    = birimFiyat,
                IscilikTl     = null,
                IndirimTutari = indirim,
                KdvTuru       = kdvTuru,
                KdvOran       = Math.Round(kalemOran, 4, MidpointRounding.AwayFromZero),
                HasDeger      = hasDeger,
                HasBirimFiyat = kalemKuru,
                KdvMatrah     = kdvMatrah,
                KdvTutar      = kdvTutar,
                SatirToplam   = satirToplam,
                Olusturuldu   = now,
            });
        }

        var toplam    = kalemler.Sum(k => k.SatirToplam);
        var kdvToplam = kalemler.Sum(k => k.KdvTutar);
        var hasToplam = kalemler.Sum(k => k.HasDeger);
        if (toplam <= 0m)
            throw new BadRequestException("Fişin toplamı sıfırdan büyük olmalıdır");

        // P-22 / B-3: TASLAK belge numara TÜKETMEZ; fişin TÜM kalemlerine
        // taslak İŞARETİ damgalanır (B-4 kuralı korunur, seri yakılmaz).
        foreach (var h in hareketler) h.FaturaNo = FaturaSeriKapisi.Isaret(faturaId);

        db.Faturalar.Add(new Data.Fatura
        {
            Id              = faturaId,
            TenantId        = islem.TenantId,
            FaturaNo        = null,   // P-22: numara KESİMDE verilir
            CariId          = cariId,
            Tur             = tur,
            KdvTuru         = kdvTuru,
            HasDeger        = hasToplam,
            AraToplam       = OzelMatrahMotoru.Yuvarla2(toplam - kdvToplam),
            KdvOran         = Math.Round(BaslikOrani(kalemler.Select(x => x.KdvOran).ToList(), kdvOran), 4, MidpointRounding.AwayFromZero),
            KdvTutar        = kdvToplam,
            Toplam          = toplam,
            ParaBirimi      = dto.ParaBirimi ?? "TRY",
            Durum           = "TASLAK",
            BankaHareketId  = null,
            Aciklama        = dto.Aciklama,
            Tarih           = tarih,
            Olusturuldu     = now,
            Guncellendi     = now,
            KalemKaynagi    = "KALEM",
            HasDegerKaynagi = "KALEM",
            // Paket 38 kanıt alanları — fişten yolda da has gram/kur belgeye yazılır.
            OzelMatrahKodu  = OzelMatrahKoduFor(kdvTuru, hasToplam),
            // Y4-D: başlık has gramı YALNIZ ALTIN bacaklarındır (deftere göre,
            // ürünsüz gümüş satırı da dışarıda kalır).
            HasGram         = kdvTuru == "KUYUM" ? OzelMatrahMotoru.Yuvarla6(altinHasGram) : null,
            HasBirimFiyat   = kdvTuru == "KUYUM" ? OzetKur(hasToplam, OzelMatrahMotoru.Yuvarla6(altinHasGram)) : null,
            // KUR KAYNAKLARI (17.10): has bedelinin kaynak kodu + kapanış günü + fiyatı.
            OzelMatrahKaynakKodu    = matrahCozum?.BelgeKodu(bindeKullanildi),
            OzelMatrahKapanisGunu   = matrahCozum?.BelgeGunu,
            OzelMatrahKapanisFiyati = matrahCozum?.BelgeFiyati,
        });
        db.FaturaKalemleri.AddRange(kalemler);
        await db.SaveChangesAsync();

        return await Getir(faturaId, islem);
    }

    /// <summary>
    /// Faturanın kalemleri (sıra ile). Kalemsiz faturada BOŞ LİSTE + kalemKaynagi
    /// = "BASLIK" döner; ekran "veri yok" değil "bu fatura başlık düzeyindedir"
    /// diyebilsin (Anayasa md.1 — bilinmeyen sıfır sayılmaz).
    /// </summary>
    public async Task<object> Kalemler(string faturaId, AktifKullanici islem)
    {
        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faturaId && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        var kalemler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.FaturaId == faturaId)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        // TOPLU-1 tur 2: bağlı hareketin fişi var mı — fişsiz satıştan kesilen taslakta ekran
        // "adet satış fişinden gelir" demesin (fiş yok). Tek sorgu, kiracı süzgeçli.
        var bagliIdler = kalemler.Where(k => !string.IsNullOrEmpty(k.HareketId))
            .Select(k => k.HareketId!).Distinct().ToList();
        var fissizIdler = bagliIdler.Count == 0
            ? new HashSet<string>()
            : (await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && bagliIdler.Contains(h.Id) && h.FisId == null)
                .Select(h => h.Id).ToListAsync()).ToHashSet();

        return new
        {
            faturaId    = fatura.Id,
            faturaNo    = fatura.FaturaNo,
            kalemKaynagi = fatura.KalemKaynagi,
            hasDegerKaynagi = fatura.HasDegerKaynagi,
            toplam      = fatura.Toplam,
            kalemler    = kalemler.Select(k => new
            {
                k.Id, k.Sira, k.Aciklama, k.Miktar, k.Birim, k.AyarId, k.Milyem,
                k.GramAgirlik, k.HasGrami, k.TasIbaresi, k.BirimFiyat, k.IscilikTl,
                k.IndirimTutari, k.KdvTuru, k.KdvOran, k.HasDeger, k.KdvMatrah,
                k.KdvTutar, k.SatirToplam, k.UrunId, k.HareketId, k.Maden,
                hareketFissiz = k.HareketId != null && fissizIdler.Contains(k.HareketId),
            }).ToList(),
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // B-3 — TASLAK FATURA VE KALEMLERİ DÜZENLENEBİLİR (PAKET P-22)
    //
    // ⚠ NEDEN: taslağı düzeltmenin tek yolu "iptal + yeniden kes"ti; her
    //   düzeltme numara serisinde boşluk açıyordu. Artık taslak numara
    //   TÜKETMEDİĞİ için (FaturaSeriKapisi) düzeltme bedava ve izlenebilir.
    // ⚠ SINIR: YALNIZ TASLAK. KESILDI/ODENDI/IPTAL belge DEĞİŞMEZ — kesilmiş
    //   belgenin düzeltme yolu iptal + yeni belgedir (mevzuat).
    // ⚠ HARD DELETE YOK: kalem SİLİNMEZ, taslağın tamamı iptal edilir.
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<Data.Fatura> TaslakBul(string id, AktifKullanici islem)
    {
        var fatura = await db.Faturalar
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");
        if (fatura.Durum != "TASLAK")
            throw new BadRequestException(
                $"Yalnız TASLAK fatura düzenlenebilir; bu belge {fatura.Durum} durumunda. "
                + "Kesilmiş belgenin düzeltme yolu iptal edip yeni belge kesmektir.");
        return fatura;
    }

    /// <summary>
    /// PATCH /faturalar/{id} — TASLAK başlığı.
    /// Sözleşme: <b>gövdede yoksa KORU, null ise SİL</b> (yalnız `aciklama` silinebilir).
    /// </summary>
    public async Task<object> TaslakGuncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var fatura = await TaslakBul(id, islem);

        if (govde.TryGetProperty("cariId", out var cariOge))
        {
            if (cariOge.ValueKind != JsonValueKind.String)
                throw new BadRequestException("Cari seçimi silinemez");
            var cariId = (cariOge.GetString() ?? "").Trim();
            if (cariId.Length == 0) throw new BadRequestException("Cari seçimi zorunlu");
            if (!await db.Cariler.AnyAsync(c => c.Id == cariId && c.TenantId == islem.TenantId))
                throw new NotFoundException("Cari bulunamadı");
            fatura.CariId = cariId;
        }

        if (govde.TryGetProperty("tarih", out var tarihOge))
        {
            if (tarihOge.ValueKind != JsonValueKind.String
                || !DateOnly.TryParse(tarihOge.GetString(), CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out var tarih))
                throw new BadRequestException("Tarih formatı geçersiz (yyyy-MM-dd bekleniyor)");
            // BELGE-KALAN (VERİ/YASAL): "tarih" İŞLETME (Europe/Istanbul) GÜNÜDÜR; kayıt UTC anıdır.
            // Gün aynıysa kayıt DEĞİŞMEZ (anın saati korunur — eski yol 00:00–03:00 TSI kaydını
            // UTC gününe, yani bir gün GERİ yazıyordu). Farklı günde yeni değer o günün 00:00 UTC
            // anıdır = 03:00 TSI, yerel günü aynı kalır.
            // PAKET GOC-A (envanter açık #9): taslak fatura tarihi sınırsızdı (gelecek yıl ya da çok eski) —
            // K8a işlem tarihi, sözlük metni (ekran Tarih Tur=Islem ile AYNI kural).
            GirdiKapisi.IslemTarihi(tarih, "tarih");
            if (DateOnly.FromDateTime(Zaman.UtcdenYerele(fatura.Tarih)) != tarih)
                fatura.Tarih = tarih.ToDateTime(TimeOnly.MinValue);
        }

        if (govde.TryGetProperty("aciklama", out var acOge))
        {
            if (acOge.ValueKind == JsonValueKind.Null) fatura.Aciklama = null;
            else if (acOge.ValueKind != JsonValueKind.String)
                throw new BadRequestException("Açıklama metin olmalı");
            else
            {
                var a = GirdiKapisi.Uzunluk(acOge.GetString(), Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaAciklama, "aciklama");   // GOC-A
                fatura.Aciklama = string.IsNullOrEmpty(a) ? null : a;
            }
        }

        if (govde.TryGetProperty("toplam", out var toplamOge))
        {
            if (fatura.KalemKaynagi == "KALEM")
                throw new BadRequestException(
                    "Kalemli faturanın toplamı başlıktan değiştirilemez; kalemleri düzenleyin "
                    + "(toplam kalemlerden TOPLANIR).");
            if (fatura.KdvTuru == "KUYUM")
                throw new BadRequestException(
                    "Kuyum (özel matrah) faturasının toplamı başlıktan değiştirilemez: has bedeli "
                    + "dayanağı yeniden hesaplanamaz. Taslağı iptal edip yeniden kesin "
                    + "(taslak numara tüketmez).");
            if (toplamOge.ValueKind != JsonValueKind.Number)
                throw new BadRequestException("Toplam sayı olmalı").AlanIle("toplam");   // PAKET PATCH-2: alan
            // PATCH-SOZLESME tur 3: decimal aralığı ve faturalar.toplam decimal(15,2) kapasitesi → 400.
            var yeniToplam = KolonTavani.Denetle(
                KolonTavani.JsonSayi(toplamOge, "toplam", "Toplam"), 2, KolonTavani.D15_2, "toplam", "Toplam");
            // PAKET GOC-A (K9): > 0 ve 2'den fazla ondalık SESSİZCE YUVARLANMAZ (Yuvarla2 eskiden 12,345 → 12,35).
            GirdiKapisi.Tutar(yeniToplam, "toplam", Kuyumcu.Core.Domain.Girdi.TutarTuru.Pozitif, "Toplam");

            var kdvTutar = yeniToplam - yeniToplam / (1m + fatura.KdvOran);
            fatura.Toplam = OzelMatrahMotoru.Yuvarla2(yeniToplam);
            fatura.KdvTutar = OzelMatrahMotoru.Yuvarla2(kdvTutar);
            fatura.AraToplam = fatura.Toplam - fatura.KdvTutar;   // 18.10 ZO-BELGE devri: oluşturmayla aynı kalıp (ara + kdv == toplam)
        }

        fatura.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await Getir(id, islem);
    }

    /// <summary>
    /// PATCH /faturalar/{id}/kalemler/{kalemId} — TASLAK kaleminin düzeltilmesi.
    /// Değişen satır yeniden hesaplanır ve belge başlığı KALEMLERDEN toplanır.
    /// </summary>
    public async Task<object> TaslakKalemGuncelle(
        string id, string kalemId, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var fatura = await TaslakBul(id, islem);
        var kalem = await db.FaturaKalemleri
            .FirstOrDefaultAsync(k => k.Id == kalemId && k.FaturaId == id && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura kalemi bulunamadı");

        if (govde.TryGetProperty("aciklama", out var acOge))
        {
            if (acOge.ValueKind != JsonValueKind.String)
                throw new BadRequestException("Kalem açıklaması silinemez");
            var a = (acOge.GetString() ?? "").Trim();
            if (a.Length == 0) throw new BadRequestException("Kalem açıklaması boş olamaz");
            // PAKET GOC-A (envanter C · si-fatura-kalem-aciklama): 300'ü aşan metin SESSİZCE KIRPILIYORDU — 400.
            kalem.Aciklama = GirdiKapisi.Uzunluk(a, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.FaturaKalemAciklama, "aciklama") ?? a;
        }

        var parasalDegisti = false;
        // PATCH-SOZLESME tur 3: kolon kapasitesi kapısı. Taşan değer eskiden veritabanında
        // "Out of range" → 500 dönüyordu. İlk gönderilen parasal alan, türetilmiş tavanın
        // (satır toplamı/has/başlık toplamı) hangi alana yazılacağını belirler.
        string? parasalAlan = null;
        if (Sayi(govde, "miktar", out var miktar))
        {
            parasalAlan ??= "miktar";
            // PAKET PATCH-2 [KRİTİK]: ÖNCE kolon hanesine yuvarla, SONRA sıfır kontrolü. Eskiden 0,0004
            // kontrolden geçip 0,000 yazılıyordu (işçilikli satırda satır toplamı > 0 kalıyordu).
            // PAKET GOC-A (K10): 4+ ondalık SESSİZCE 3 haneye yuvarlanıyordu — Core kuralı, 400 alan:miktar.
            GirdiKapisi.Gram(miktar, "miktar", "Miktar");
            KolonTavani.Denetle(miktar, 3, KolonTavani.D12_3, "miktar", "Miktar");
            // DEFTER BAĞLI satır (fişten kesilen kalem): adet DEFTERDEN gelir; belgede
            // değiştirilirse fatura ile satış kaydı iki ayrı gerçek olur. Adet fişte düzeltilir
            // (fiyat düzeltmesi serbest kalır). Aynı değer yeniden gönderilirse değişiklik yoktur.
            if (!string.IsNullOrEmpty(kalem.HareketId)
                && Math.Round(miktar, 3, MidpointRounding.AwayFromZero) != kalem.Miktar)
                throw new BadRequestException(
                    "Bu satır satış fişinden geldi; adedi fişten değiştirin.");
            var eskiMiktar = kalem.Miktar;
            kalem.Miktar = Math.Round(miktar, 3, MidpointRounding.AwayFromZero);
            // kalem.HasGrami SATIR TOPLAMIDIR. PAKET CARI-BAKIYE: toplam = birim has × miktar
            // (orantılı yeniden hesap 6 hanede birikiyordu: 3 → 1,001 → 3 has'ı kaydırıyordu).
            // Birim bilinmiyorsa (eski kalem) eski toplam ÷ eski miktar BİR KEZ donar ve saklanır;
            // sonraki düzeltmeler hep o birimden hesaplanır. Miktar aynıysa dokunulmaz.
            if (kalem.Miktar != eskiMiktar)
            {
                if (kalem.BirimHasGrami is null && kalem.HasGrami is { } eskiToplam && eskiMiktar > 0m)
                    kalem.BirimHasGrami = OzelMatrahMotoru.Yuvarla6(eskiToplam / eskiMiktar);
                if (kalem.BirimHasGrami is { } birimHas)
                    kalem.HasGrami = KalemToplamHas(kalem.Miktar, birimHas);
            }
            parasalDegisti = true;
        }
        if (Sayi(govde, "birimFiyat", out var birimFiyat))
        {
            parasalAlan ??= "birimFiyat";
            GirdiKapisi.Tutar(birimFiyat, "birimFiyat", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, "Birim fiyat");   // GOC-A K9a
            KolonTavani.Denetle(birimFiyat, 2, KolonTavani.D15_2, "birimFiyat", "Birim fiyat");
            kalem.BirimFiyat = OzelMatrahMotoru.Yuvarla2(birimFiyat);
            parasalDegisti = true;
        }
        // P-54: sayı ya da null dışı değer (ör. "abc") 500 değil 400'dür —
        // ekran bu alanları artık satır içinde düzenletiyor.
        if (govde.TryGetProperty("iscilikTl", out var iscOge))
        {
            parasalAlan ??= "iscilikTl";
            kalem.IscilikTl = SayiYaDaNull(iscOge, "iscilikTl");
            GirdiKapisi.Tutar(kalem.IscilikTl, "iscilikTl", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, "İşçilik");   // GOC-A K9a
            KolonTavani.Denetle(kalem.IscilikTl, 2, KolonTavani.D12_2, "iscilikTl", "İşçilik");
            parasalDegisti = true;
        }
        if (govde.TryGetProperty("indirimTutari", out var indOge))
        {
            parasalAlan ??= "indirimTutari";
            kalem.IndirimTutari = SayiYaDaNull(indOge, "indirimTutari");
            GirdiKapisi.Tutar(kalem.IndirimTutari, "indirimTutari", Kuyumcu.Core.Domain.Girdi.TutarTuru.SifirOlabilir, "İndirim");   // GOC-A K9a
            KolonTavani.Denetle(kalem.IndirimTutari, 2, KolonTavani.D15_2, "indirimTutari", "İndirim");
            parasalDegisti = true;
        }

        if (parasalDegisti)
        {
            var satirToplam = OzelMatrahMotoru.Yuvarla2(
                kalem.Miktar * kalem.BirimFiyat + (kalem.IscilikTl ?? 0m) - (kalem.IndirimTutari ?? 0m));
            if (satirToplam <= 0m)
                throw new BadRequestException("Kalemin satır toplamı sıfırdan büyük olmalıdır");
            // Türetilmiş kolonlar: satır has gramı (15,6) ve başlık toplamı (15,2; satır toplamı
            // başlığın parçası olduğundan onun kapısı satırı da kapsar) — İLK SaveChanges'tan ÖNCE denetlenir ki kalem yazılıp başlık taşmasın.
            var alan = parasalAlan ?? "miktar";
            KolonTavani.Denetle(kalem.HasGrami, 6, KolonTavani.D15_6, alan, "Satırın has gramı");
            var digerToplam = await db.FaturaKalemleri
                .Where(k => k.FaturaId == id && k.TenantId == islem.TenantId && k.Id != kalem.Id)
                .SumAsync(k => k.SatirToplam);
            KolonTavani.Denetle(digerToplam + satirToplam, 2, KolonTavani.D15_2, alan, "Fatura toplamı");

            var bacaklar = new List<HasBacagi>();
            // Y4-D (5. yol): altın dışı (gümüş/platin/paladyum) kuyum kaleminde has
            // bedeli DÜŞÜLMEZ — oluşturma yollarıyla aynı süzgeç (OzelMatrahKapsaminda);
            // bacak yok → KDV tam, düzeltme 400'e takılmaz.
            if (kalem.KdvTuru == "KUYUM" && KalemAltinMi(kalem))
            {
                // ⚠ HAS BEDELİNİN DAYANAĞI KUR KAYDIDIR (orkestratör 30.09 07:50): kalem
                //   oluşurken yazılan kalem.HasBirimFiyat (motorun kullandığı kur kaydındaki
                //   altın has fiyatı) AYNEN kullanılır — güncel kura geçilmez, yuvarlanmış
                //   bedelden geri ÇÖZÜLMEZ. has bedeli = round2(yeni satır toplam has gram ×
                //   kalem.HasBirimFiyat). Dayanak bilinmiyorsa (NULL) ya da has gram yoksa özel
                //   matrah UYGULANMAZ: bacak yok → KDV tam (fail-closed; fazla KDV, eksik değil).
                if (kalem.HasBirimFiyat is { } dayanakKur && dayanakKur > 0m
                    && kalem.HasGrami is { } yeniHasGramToplam && yeniHasGramToplam > 0m)
                    bacaklar.Add(new HasBacagi(yeniHasGramToplam, dayanakKur));
            }

            // M2b — kuyum kaleminde ürünün KDV'ye giren payı (binde) varsa müşterinin yöntemi.
            decimal? kalemBinde = null;
            if (kalem.KdvTuru == "KUYUM" && !string.IsNullOrEmpty(kalem.UrunId))
                kalemBinde = await db.Urunler.AsNoTracking()
                    .Where(u => u.Id == kalem.UrunId && u.TenantId == islem.TenantId)
                    .Select(u => u.KdvMatrahBinde).FirstOrDefaultAsync();
            var sonuc = OzelMatrahMotoru.Hesapla(bacaklar, satirToplam, kalem.KdvOran, kalemBinde);
            kalem.HasDeger = sonuc.HasDeger;
            kalem.KdvMatrah = sonuc.Matrah;
            kalem.KdvTutar = sonuc.KdvTutar;
            kalem.SatirToplam = satirToplam;
        }

        await db.SaveChangesAsync();

        if (parasalDegisti)
        {
            // Başlık KALEMLERDEN toplanır — iki ayrı gerçek olmaz (UX-S21).
            var tumKalemler = await db.FaturaKalemleri
                .Where(k => k.FaturaId == id && k.TenantId == islem.TenantId).ToListAsync();
            fatura.Toplam = tumKalemler.Sum(k => k.SatirToplam);
            fatura.KdvTutar = tumKalemler.Sum(k => k.KdvTutar);
            fatura.HasDeger = tumKalemler.Sum(k => k.HasDeger);
            fatura.AraToplam = fatura.Toplam - fatura.KdvTutar;
            if (fatura.KdvTuru == "KUYUM")
            {
                // Başlık has gramı/özet kur da KALEMLERDEN (TaslakKalemEkle ile aynı kural):
                // miktar düzeltmesi ya da madeni bilinmeyen satır başlığı eski gramla bırakmaz.
                fatura.HasGram       = KalemHasGrami(tumKalemler);
                fatura.HasBirimFiyat = OzetKur(fatura.HasDeger, fatura.HasGram ?? 0m);
            }
            // Y4-D: 805 kodu belgenin GÜNCEL altın has bedeline bağlıdır (oluşturmayla aynı kural).
            fatura.OzelMatrahKodu = OzelMatrahKoduFor(fatura.KdvTuru, fatura.HasDeger);
            fatura.Guncellendi = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return await Kalemler(id, islem);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAKET Q9-2 — TASLAK FATURAYA KALEM EKLEME (QA turu 9, KRİTİK)
    //
    // ⚠ NEDEN: P-54 taslak kalemini düzeltilebilir yaptı ama EKSİK satır
    //   yalnız "iptal + yeniden kes" ile eklenebiliyordu. Taslak numara
    //   tüketmediği (FaturaSeriKapisi) ve belge henüz kesilmediği için satır
    //   eklemek serbesttir; kesilmiş belge DEĞİŞMEZ (TaslakBul kapısı).
    // ⚠ HESAP TEK YERDE: satır, KalemYolu ile AYNI kuralla kurulur (miktar ×
    //   birim fiyat + işçilik − indirim; KUYUM'da has bacağı ürün kartından ×
    //   kiracının EN SON kur kaydından — harekete bağlı olmayan kalemle aynı).
    //   Başlık KALEMLERDEN toplanır (iki ayrı gerçek olmaz — UX-S21).
    // ⚠ SINIRLAR:
    //   • Kalemsiz (BASLIK) taslağa satır EKLENMEZ: toplamı elle girilmiştir;
    //     ilk satır başlık toplamını sessizce ezerdi. Yol: başlık toplamını
    //     düzelt ya da iptal + kalemli yeniden kes.
    //   • Satış kaydına (hareket) bağlı satır sonradan EKLENMEZ: damga ve has
    //     dayanağı fişten fatura yolunda kurulur (B-4 "fişin tamamı").
    //   • Kalem SİLİNMEZ (P-22 kuralı değişmedi) — yalnız EKLEME açıldı.
    // ⚠ DataAnnotations yalnız controller yolunda çalışır → sınırlar ELLE.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>POST /faturalar/{id}/kalemler — TASLAK faturaya satır ekler; yanıt KalemYaniti.</summary>
    public async Task<object> TaslakKalemEkle(string id, FaturaKalemDto? g, AktifKullanici islem)
    {
        if (g is null) throw new BadRequestException("Geçersiz istek gövdesi");

        var fatura = await TaslakBul(id, islem);
        if (fatura.KalemKaynagi != "KALEM")
            throw new BadRequestException(
                "Bu taslak satırsız (yalnız toplam) kaydedildi; satır eklenemez. Toplamı düzenleme "
                + "penceresindeki Toplam alanından düzeltin ya da taslağı iptal edip satırlarla "
                + "yeniden kesin (taslak numara tüketmez).");
        if (!string.IsNullOrWhiteSpace(g.HareketId))
            throw new BadRequestException(
                "Satış kaydına bağlı satır sonradan eklenemez; satıştan fatura için "
                + "'Fişten Fatura Kes' yolunu kullanın.");

        KalemGirdisiDenetle(g, null);   // PAKET GOC-A — POST ile AYNI kapı
        // PAKET PATCH-2: kontrol yuvarlanmış (kolona yazılacak) miktarla — 0,0004 → 0,000 reddedilir.
        if (Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero) <= 0m)
            throw new BadRequestException("Miktar sıfırdan büyük olmalıdır").AlanIle("miktar");
        if (g.BirimFiyat < 0m) throw new BadRequestException("Birim fiyat negatif olamaz");
        if (g.IscilikTl < 0m) throw new BadRequestException("İşçilik negatif olamaz");
        if (g.IndirimTutari < 0m) throw new BadRequestException("İndirim negatif olamaz");
        if (g.Aciklama is { Length: > 300 }) throw new BadRequestException("Açıklama 300 karakteri aşamaz");

        // TB-19/B-8: sınırlı okuma — tavan +1 okunur ki sınıra değildiği anlaşılsın.
        var mevcut = await db.FaturaKalemleri
            .Where(k => k.FaturaId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira).Take(KalemUstSiniri + 1).ToListAsync();
        if (mevcut.Count >= KalemUstSiniri)
            throw new BadRequestException($"Bir faturada en çok {KalemUstSiniri} kalem olabilir");

        Data.Urun? urun = null;
        if (!string.IsNullOrWhiteSpace(g.UrunId))
            urun = await db.Urunler.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == g.UrunId && u.TenantId == islem.TenantId)
                ?? throw new NotFoundException("Ürün bulunamadı");

        var aciklama = (string.IsNullOrWhiteSpace(g.Aciklama) ? urun?.Ad : g.Aciklama.Trim());
        if (string.IsNullOrWhiteSpace(aciklama))
            throw new BadRequestException("Kalem açıklaması boş olamaz: açıklama yazın ya da ürün seçin");

        var miktar = Math.Round(g.Miktar, 3, MidpointRounding.AwayFromZero);
        var satirToplam = OzelMatrahMotoru.Yuvarla2(
            miktar * g.BirimFiyat + (g.IscilikTl ?? 0m) - (g.IndirimTutari ?? 0m));
        if (satirToplam <= 0m)
            throw new BadRequestException("Kalemin satır toplamı sıfırdan büyük olmalıdır");

        var bacaklar = new List<HasBacagi>();
        decimal? kalemKuru = null;   // has bedeli dayanağı — kur KAYDININ fiyatı
        if (fatura.KdvTuru == "KUYUM")
        {
            // FAIL-CLOSED — KalemYolu ile aynı: ürün yoksa ya da kur yoksa DURULUR.
            if (urun is null)
                throw new BadRequestException(
                    "Kuyum faturasına eklenen satırda ürün seçilmeli — has bedeli ürün kartından türetilir");
            // Y4-D: altın dışı ürün kaleminde has bedeli düşülmez (madeni kaleme yazılan değer).
            if (OzelMatrahKapsaminda(urun.Maden))
            {
                var sonKur = await db.KurSnapshots.AsNoTracking()
                    .Where(k => k.TenantId == islem.TenantId)
                    .OrderByDescending(k => k.Olusturuldu).FirstOrDefaultAsync();
                decimal dayanak;
                if (urun.KdvMatrahBinde is > 0m)
                {   // M2b: binde yöntemi fiyat KULLANMAZ — bugünkü kur kapısı aynen.
                    if (sonKur is null || sonKur.AltinHasFiyati <= 0m)
                        throw new BadRequestException("Kur kaydı bulunamadı — has bedeli hesaplanamaz");
                    dayanak = sonKur.AltinHasFiyati;
                    if (fatura.OzelMatrahKaynakKodu is null) fatura.OzelMatrahKaynakKodu = Kur.KurKaynakKodlari.Binde;
                }
                else
                {
                    // KUR KAYNAKLARI (17.10): belgenin İLK fiyat kaynağı ve kapanış günü korunur
                    // (bir belgede iki kapanış olmaz); belge henüz fiyat kullanmadıysa kiracının ayarı.
                    var fiyatliKod = fatura.OzelMatrahKaynakKodu is { } k && k != Kur.KurKaynakKodlari.Binde ? k : null;
                    var cozum = await Kur.OzelMatrahKaynakCozucu.Coz(db, islem.TenantId, fatura.Tarih,
                        fiyatliKod, fiyatliKod is null ? null : fatura.OzelMatrahKapanisGunu);
                    dayanak = cozum.Fiyat(sonKur?.AltinHasFiyati, "Kur kaydı bulunamadı — has bedeli hesaplanamaz");
                    fatura.OzelMatrahKaynakKodu = cozum.BelgeKodu(false);
                    fatura.OzelMatrahKapanisGunu = cozum.BelgeGunu;
                    fatura.OzelMatrahKapanisFiyati = cozum.BelgeFiyati;
                }
                // Değişmez: has bedeli = round2(KALEME YAZILAN has gram × dayanak fiyat) — PATCH yoluyla aynı.
                bacaklar.Add(new HasBacagi(KalemToplamHas(miktar, urun.HasGrami)!.Value, dayanak));
                kalemKuru = dayanak;
            }
        }

        // M2 — taslağa eklenen kalem: ürün kartında oran varsa o, yoksa belgenin oranı.
        var eklenenOran = urun?.KdvOrani is { } urunYuzde
            ? Math.Round(urunYuzde / 100m, 4, MidpointRounding.AwayFromZero) : fatura.KdvOran;
        var sonuc = OzelMatrahMotoru.Hesapla(bacaklar, satirToplam, eklenenOran,
            fatura.KdvTuru == "KUYUM" ? urun?.KdvMatrahBinde : null);   // M2b
        var yeni = new Data.FaturaKalemi
        {
            Id            = Kimlik.Ulid(),
            TenantId      = islem.TenantId,
            FaturaId      = id,
            Sira          = mevcut.Count == 0 ? 1 : mevcut.Max(k => k.Sira) + 1,
            HareketId     = null,
            UrunId        = urun?.Id,
            Aciklama      = aciklama.Length <= 300 ? aciklama : aciklama[..300],
            Miktar        = miktar,
            Birim         = urun?.Birim ?? "ADET",
            AyarId        = urun?.AyarId,
            Milyem        = urun?.Milyem,
            GramAgirlik   = urun?.GramAgirlik,
            Maden         = urun?.Maden,
            HasGrami      = KalemToplamHas(miktar, urun?.HasGrami),   // satırın TOPLAM has gramı
            BirimHasGrami = urun?.HasGrami,   // PAKET CARI-BAKIYE: taslak düzeltmesinin tabanı
            TasIbaresi    = urun?.TasIbaresi,
            BirimFiyat    = OzelMatrahMotoru.Yuvarla2(g.BirimFiyat),
            IscilikTl     = g.IscilikTl,
            IndirimTutari = g.IndirimTutari,
            KdvTuru       = fatura.KdvTuru,
            KdvOran       = eklenenOran,
            HasDeger      = sonuc.HasDeger,
            HasBirimFiyat = kalemKuru,
            KdvMatrah     = sonuc.Matrah,
            KdvTutar      = sonuc.KdvTutar,
            SatirToplam   = satirToplam,
            Olusturuldu   = DateTime.UtcNow,
        };
        db.FaturaKalemleri.Add(yeni);
        mevcut.Add(yeni);

        // Başlık KALEMLERDEN toplanır (TaslakKalemGuncelle ile aynı kural).
        fatura.Toplam    = mevcut.Sum(k => k.SatirToplam);
        fatura.KdvTutar  = mevcut.Sum(k => k.KdvTutar);
        fatura.HasDeger  = mevcut.Sum(k => k.HasDeger);
        fatura.AraToplam = fatura.Toplam - fatura.KdvTutar;
        if (fatura.KdvTuru == "KUYUM")
        {
            fatura.HasGram       = KalemHasGrami(mevcut);
            fatura.HasBirimFiyat = OzetKur(fatura.HasDeger, fatura.HasGram ?? 0m);
        }
        // Y4-D: yalnız gümüş taslağa altın satır eklenince 805 kodu da belgeye girer
        // (kod belgenin GÜNCEL altın has bedeline bağlıdır; oluşturmayla aynı kural).
        fatura.OzelMatrahKodu = OzelMatrahKoduFor(fatura.KdvTuru, fatura.HasDeger);
        fatura.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Kalemler(id, islem);
    }

    /// <summary>P-54: null = alanı sil (PATCH sözleşmesi); sayı değilse 400.</summary>
    private static decimal? SayiYaDaNull(JsonElement oge, string ad) => oge.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Number => KolonTavani.JsonSayi(oge, ad, $"'{ad}'"),
        _ => throw new BadRequestException($"'{ad}' sayı olmalı").AlanIle(ad),   // PAKET PATCH-2: alan
    };

    private static bool Sayi(JsonElement govde, string ad, out decimal deger)
    {
        deger = 0m;
        if (!govde.TryGetProperty(ad, out var oge)) return false;
        if (oge.ValueKind != JsonValueKind.Number)
            throw new BadRequestException($"'{ad}' sayı olmalı").AlanIle(ad);   // PAKET PATCH-2: alan ("1.500" → alanlar.<ad>)
        deger = KolonTavani.JsonSayi(oge, ad, $"'{ad}'");   // decimal aralığı dışı → 400 (500 değil)
        return true;
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    public async Task<object> Listele(
        AktifKullanici islem, string? durum, string? cariId, int limit, int offset)
    {
        var q = db.Faturalar.AsNoTracking().Where(f => f.TenantId == islem.TenantId);
        if (durum is "TASLAK" or "KESILDI" or "ODENDI" or "IPTAL")
            q = q.Where(f => f.Durum == durum);
        if (!string.IsNullOrEmpty(cariId)) q = q.Where(f => f.CariId == cariId);

        var liste = await q.OrderByDescending(f => f.Tarih)
            .Skip(offset).Take(limit).ToListAsync();

        // Cari adlarını tek sorguda topla
        var cariIdler = liste.Select(f => f.CariId).Distinct().ToList();
        var cariAdMap = cariIdler.Count == 0
            ? new Dictionary<string, string>()
            : await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == islem.TenantId && cariIdler.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Ad);

        var islemden = await IslemdenGelenler(liste.Select(f => f.Id).ToList(), islem);
        return new
        {
            faturalar = liste
                .Select(f => CariliYanit(f, cariAdMap.GetValueOrDefault(f.CariId), islemden.Contains(f.Id)))
                .ToList(),
        };
    }

    // ─── Tekil ────────────────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var fatura = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        var cari = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == fatura.CariId && c.TenantId == islem.TenantId);
        var islemden = await IslemdenGelenler([fatura.Id], islem);
        return CariliYanit(fatura, cari?.Ad, islemden.Contains(fatura.Id));
    }

    /// <summary>
    /// TOPLU-1 tur 2 (tasarım hakemi KRİTİK): fişsiz satıştan kesilen taslak listede "Manuel"
    /// görünüyordu. Kaynak, kalemlerin DEFTER bağından (fatura_kalemleri.hareket_id) türetilir —
    /// yeni kolon yok, tek sorgu, kiracı süzgeçli.
    /// </summary>
    private async Task<HashSet<string>> IslemdenGelenler(List<string> faturaIdler, AktifKullanici islem)
    {
        if (faturaIdler.Count == 0) return [];
        var idler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && faturaIdler.Contains(k.FaturaId) && k.HareketId != null)
            .Select(k => k.FaturaId).Distinct().ToListAsync();
        return idler.ToHashSet();
    }

    // ─── Durum güncelle ───────────────────────────────────────────────────────

    public async Task<object> DurumGuncelle(string id, DurumGuncelleDto dto, AktifKullanici islem)
    {
        var fatura = await db.Faturalar
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Fatura bulunamadı");

        var izinli = GecerliGecisler.GetValueOrDefault(fatura.Durum) ?? [];
        if (!izinli.Contains(dto.Durum))
            throw new BadRequestException($"{fatura.Durum} durumundan {dto.Durum} durumuna geçilemez");

        var oncekiDurum = fatura.Durum;
        fatura.Durum = dto.Durum;
        fatura.Guncellendi = DateTime.UtcNow;

        // ── P-22 / B-3: SERİ YALNIZ BURADA TÜKETİLİR ────────────────────────
        // TASLAK → KESILDI: belge numarasını ŞİMDİ alır. Taslak iken düzeltilen
        // ya da iptal edilen belge seride boşluk BIRAKMAZ.
        // 17.10 P0-8: alıcı bilgisi kesim anında belgeye dondurulur (bir kez; sonradan cari düzeltmesi belgeyi değiştirmez).
        if (dto.Durum == "KESILDI" && fatura.AliciUnvan is null)
            await AliciyiDondur(db, fatura);

        if (dto.Durum == "KESILDI" && fatura.FaturaNo is null)
            // BELGE-KALAN: seri yılı belgenin İŞLETME yılıdır (1 Ocak 00:00–03:00 TSI kaydı UTC'de
            // önceki yıldır).
            await FaturaSeriKapisi.Tuket(db, fatura, Zaman.UtcdenYerele(fatura.Tarih).Year);

        // TASLAK → IPTAL: numara hiç alınmadı; hareketlerdeki taslak işareti
        // kaldırılır ki aynı hareket yeniden faturalanabilsin (UX-S25).
        if (dto.Durum == "IPTAL" && oncekiDurum == "TASLAK")
            await FaturaSeriKapisi.IsaretiKaldir(db, fatura);

        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Yardımcı ─────────────────────────────────────────────────────────────

    /// <summary>17.10 P0-8 — carinin o anki unvanı (yoksa adı), kimlik no'su, türü ve adresi belgeye kopyalanır.</summary>
    internal static async Task AliciyiDondur(KuyumcuDbContext db, Data.Fatura fatura)
    {
        var c = await db.Cariler.AsNoTracking()
            .Where(x => x.Id == fatura.CariId && x.TenantId == fatura.TenantId)
            .Select(x => new { x.Ad, x.Unvan, x.VergiNo, x.KimlikTuru, x.Adres })
            .FirstOrDefaultAsync();
        if (c is null) return;
        fatura.AliciUnvan = string.IsNullOrWhiteSpace(c.Unvan) ? c.Ad : c.Unvan;
        fatura.AliciVergiNo = c.VergiNo;
        fatura.AliciKimlikTuru = c.KimlikTuru;
        fatura.AliciAdres = c.Adres;
    }

    // ⚠ P-22 / B-3: BU SINIFTA ARTIK NUMARA ÜRETİCİSİ YOKTUR. Seriyi tüketen
    //   TEK NOKTA FaturaSeriKapisi.Tuket'tir (denetim.sh: SERI_TUKETIM_NOKTASI = 1).

    /// <summary>Nest'teki { ...fatura, cariAd } yanıt şekli.</summary>
    private static object CariliYanit(Data.Fatura f, string? cariAd, bool islemdenGeldi = false) => new
    {
        f.Id,
        f.TenantId,
        f.FaturaNo,
        f.CariId,
        f.Tur,
        f.KdvTuru,
        f.HasDeger,
        f.AraToplam,
        f.KdvOran,
        f.KdvTutar,
        f.Toplam,
        f.ParaBirimi,
        f.Durum,
        f.BankaHareketId,
        f.Aciklama,
        f.Tarih,
        f.Olusturuldu,
        f.Guncellendi,
        f.KalemKaynagi,
        f.HasDegerKaynagi,
        // Paket 38: özel matrahın KANITI belgeyle birlikte okunur.
        f.OzelMatrahKodu,
        // 17.10 P0-8: kesim anındaki alıcı (cari kartı sonradan değişse de belge bunu gösterir)
        f.AliciUnvan, f.AliciVergiNo, f.AliciKimlikTuru, f.AliciAdres,
        f.HasGram,
        f.HasBirimFiyat,
        // KUR KAYNAKLARI (17.10): has bedelinin kaynak kodu + kullanılan kapanış günü ve fiyatı.
        f.OzelMatrahKaynakKodu,
        f.OzelMatrahKapanisGunu,
        f.OzelMatrahKapanisFiyati,
        CariAd = cariAd ?? "—",
        IslemdenGeldi = islemdenGeldi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/faturalar")]
[Yetki.ModulYetkisi("fatura")]
public sealed class FaturaController(FaturaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// GET /api/v1/faturalar/ozel-matrah-ayari — PAKET P-26 (Y-40).
    /// Yürürlükteki özel matrah neden kodunu ve <b>kodun KAYNAĞINI</b> döner.
    /// <para>
    /// ⚠ BEYANSIZ KOD YASAK (UX-S21/UX-S31 · denetim.sh OZEL_MATRAH_BEYANSIZ_EKRAN):
    /// ekran "805" sayısını tek başına basamaz; nereden geldiğini ve mali müşavir
    /// imzası olmadığını aynı yerde söylemek zorundadır. Sözleşme budur.
    /// </para>
    /// </summary>
    [HttpGet("ozel-matrah-ayari")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<object> OzelMatrahAyari() =>
        servis.OzelMatrahAyari(await servis.KuyumFaturaVar(Islem));

    /// <summary>POST /api/v1/faturalar — Manuel fatura oluştur — MUHASEBE+</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Olustur([FromBody] FaturaOlusturDto dto) => servis.Olustur(dto, Islem);

    /// <summary>
    /// POST /api/v1/faturalar/fisten — Fişin defter satırlarından fatura kes — MUHASEBE+.
    /// Fişin TÜM kalemleri girer; has bedeli SUNUCUDA türetilir, elle girilmez.
    /// </summary>
    [HttpPost("fisten")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> FistenOlustur([FromBody] FaturaFistenDto dto) => servis.FistenOlustur(dto, Islem);

    /// <summary>
    /// GET /api/v1/faturalar/ozel-matrah-onizleme?hareketId=…&amp;kdvOran=…&amp;toplam=…
    /// — SALT OKUNUR özel matrah kırılımı (paket 38) — MUHASEBE+.
    /// <para>
    /// Kaydet'e basılmadan önce kasiyer NEYİN faturalandığını ve has bedelinin
    /// nereden geldiğini görür. Mehmet K38-2: yazma ucu değildir ama fatura
    /// tutarlarını ifşa eder → rol kapısı POST ile aynıdır.
    /// </para>
    /// </summary>
    [HttpGet("ozel-matrah-onizleme")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> OzelMatrahOnizleme(
        [FromQuery] string hareketId,
        [FromQuery] decimal? kdvOran,
        [FromQuery] decimal? toplam)
        => servis.OzelMatrahOnizleme(hareketId, kdvOran, toplam, Islem);

    /// <summary>
    /// GET /api/v1/faturalar/faturalanabilir-islemler?gun=30&amp;limit=50 —
    /// Henüz faturalandırılmamış son SATIS/ALIS hareketleri — MUHASEBE+.
    /// Ekrandaki "faturalandırılacak işlem" seçicisinin kaynağıdır; yeni bir
    /// modül açılmaz, fatura modülünün kendi kapısından okunur.
    /// </summary>
    [HttpGet("faturalanabilir-islemler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> FaturalanabilirIslemler(
        [FromQuery] int gun = 30, [FromQuery] int limit = 50)
        => servis.FaturalanabilirIslemler(Islem, gun, limit);

    /// <summary>GET /api/v1/faturalar?durum=KESILDI&amp;cariId=... — Fatura listesi</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] string? durum, [FromQuery] string? cariId,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        => servis.Listele(Islem, durum, cariId, limit, offset);

    /// <summary>GET /api/v1/faturalar/{id} — Fatura detayı</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>
    /// GET /api/v1/faturalar/{id}/kalemler — Faturanın satır kalemleri (sıra ile).
    /// Fatura modülüne erişebilen TÜM roller okuyabilir (sınıf düzeyindeki
    /// [ModulYetkisi("fatura")] kapısı geçerlidir); yazma ucu değildir.
    /// </summary>
    [HttpGet("{id}/kalemler")]
    public Task<object> Kalemler(string id) => servis.Kalemler(id, Islem);

    /// <summary>PATCH /api/v1/faturalar/{id}/durum — Durum geçişi — MUHASEBE+</summary>
    /// <summary>PATCH /api/v1/faturalar/{id} — TASLAK başlığı (P-22 / B-3).
    /// Sözleşme: gövdede yoksa KORU, null ise SİL.</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> TaslakGuncelle(string id, [FromBody] JsonElement govde)
        => servis.TaslakGuncelle(id, govde, Islem);

    /// <summary>PATCH /api/v1/faturalar/{id}/kalemler/{kalemId} — TASLAK kalemi (P-22 / B-3).</summary>
    /// <summary>
    /// PAKET Q9-2: TASLAK faturaya satır EKLER (kesilmiş belgede 400). Yanıt
    /// KalemYaniti — ekran yeni toplamı buradan okur. Kalem SİLME ucu YOKTUR (P-22).
    /// </summary>
    [HttpPost("{id}/kalemler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> TaslakKalemEkle(string id, [FromBody] FaturaKalemDto dto)
        => servis.TaslakKalemEkle(id, dto, Islem);

    [HttpPatch("{id}/kalemler/{kalemId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> TaslakKalemGuncelle(string id, string kalemId, [FromBody] JsonElement govde)
        => servis.TaslakKalemGuncelle(id, kalemId, govde, Islem);

    [HttpPatch("{id}/durum")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> DurumGuncelle(string id, [FromBody] DurumGuncelleDto dto)
        => servis.DurumGuncelle(id, dto, Islem);
}
