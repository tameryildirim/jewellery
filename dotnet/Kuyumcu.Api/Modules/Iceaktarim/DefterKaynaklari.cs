using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Iceaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// DEFTER KAYNAKLARI — paket GOC-47B (veri göçü Faz B)
//
//   acilis-bakiye : cari açılış bakiyesi (TL · has · USD/EUR/GBP)
//   acilis-stok   : açılış stoku → DEVIR partisi (maliyet bilinmiyorsa NULL)
//
// ⚠ BU KLASÖR DEFTERE YAZMAZ (FAIL-CLOSED 3, denetim.sh sayacı): yazma
//   Kurulum.AcilisYazici üzerinden yürür — cari bacağı CariServisi.AcilisYaz →
//   CariBakiyeYazici (TEK YAZICI), stok bacağı T-2'nin STOK açılış şeması.
//
// ⚠ KART ÜRETİLMEZ (Bertan koşulu): cari ya da ürün bulunamazsa satır HATA'dır;
//   "önce kart dosyasını aktarın" denir. Tek dosyada iki varlık üretmek geri
//   almayı karmaşıklaştırır.
//
// ⚠ EMANET BAKİYEYE GİRMEZ (karar 3): adı "emanet" içeren kolon dosya düzeyinde
//   REDDEDİLİR (sessizce yok sayılsaydı kullanıcı "emaneti de girdim" sanırdı).
//
// ⚠ HAS 4 ONDALIK (karar 5): daha fazla ondalık YUVARLANMAZ, satır HATA olur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Defter kaynaklarının satır hata kodları — ekranda sözlükten çevrilir (UX-S31).</summary>
public static class DefterHataKodu
{
    public const string CariYok = "CARI_YOK";
    public const string CariBelirsiz = "CARI_BELIRSIZ";
    public const string UrunBelirsiz = "URUN_BELIRSIZ";
    public const string SistemCarisi = "SISTEM_CARISI";
    public const string UrunYok = "URUN_YOK";
    public const string PasifHedef = "PASIF_HEDEF";
    public const string ZatenAcilis = "ZATEN_ACILIS";
    public const string YinelenenSatir = "YINELENEN_SATIR";
    public const string StokVar = "STOK_VAR";
    public const string SifirSatir = "SIFIR_SATIR";
    public const string HasOndalik = "HAS_ONDALIK";
    public const string TutarOndalik = "TUTAR_ONDALIK";
    public const string AdetGecersiz = "ADET_GECERSIZ";
    public const string MaliyetSifir = "MALIYET_SIFIR";
    public const string MaliyetNegatif = "MALIYET_NEGATIF";
    public const string TutarAsiri = "TUTAR_ASIRI";
    public const string SayiGecersiz = "SAYI_GECERSIZ";
    public const string SayiBelirsiz = CsvOkuyucu.SayiBelirsizKodu;
    public const string AlanZorunlu = "ALAN_ZORUNLU";

    /// <summary>ATLA politikasında "atlandı" sayılabilen kodlar (zaten var olan açılış).</summary>
    public static readonly string[] Atlanabilir = [ZatenAcilis, YinelenenSatir];
}

/// <summary>Kuru prova / uygulama özetinin defter kısmı (UX-S23: etkisi kaydetmeden önce görünür).</summary>
public sealed record DefterOzeti(
    int SatirSayisi,
    decimal TlToplam, decimal HasToplam,
    IReadOnlyDictionary<string, decimal> DovizToplam,
    decimal ToplamAdet, decimal MaliyetiBilinmeyenAdet, decimal ToplamMaliyetTl, decimal ToplamHasGram);

/// <summary>Satır yazımının ihtiyaç duyduğu bağlam (tek kur, tek an, tek gerekçe).</summary>
public sealed record DefterBaglami(
    KuyumcuDbContext Db, Kurulum.AcilisYazici Yazici, KurSnapshot Kur,
    string Gerekce, AktifKullanici Islem, string OturumId, DateTime Simdi);

/// <summary>Deftere (açılış) yazan içe aktarım kaynağı.</summary>
public interface IDefterKaynagi : IIceAktarilabilir
{
    /// <summary>Başlığında bu parçalardan biri geçen kolon dosyayı REDDETTİRİR.</summary>
    string[] YasakKolonParcalari { get; }

    /// <summary>Saf + veritabanı doğrulaması (yalnız OKUR). Sonuç sırası girdi sırasıdır.</summary>
    Task<List<SatirSonucu>> TopluDogrula(
        KuyumcuDbContext db, AktifKullanici islem, IReadOnlyList<IReadOnlyDictionary<string, string>> hamlar);

    /// <summary>Satırı açılış yazıcısıyla yazar; açılan AcilisKalemi kimliğini döner.</summary>
    Task<string> DefterUygula(DefterBaglami b, object dto);

    /// <summary>Satırın açılışını TERS KAYITLA geri alır (silme yok).</summary>
    Task DefterGeriAl(DefterBaglami b, string kalemId, object dto);

    DefterOzeti Ozetle(IEnumerable<object> dtolar);
}

/// <summary>Defter kaynaklarının ortak yardımcıları.</summary>
internal static class DefterOrtak
{
    public const decimal TutarTavani = Kurulum.AcilisKaynaklari.TutarTavani;
    public const decimal AdetTavani = 1_000_000m;

    /// <summary>
    /// Has gram tavanı: <c>cari_bakiyeler.has_gram_bakiye</c> ve <c>hareketler.has_grami</c>
    /// decimal(15,6) → en çok 999.999.999,999999; has 4 ondalık (karar 5) olduğundan
    /// 999.999.999,9999. Üstü provada TUTAR_ASIRI (uygulamada anlaşılmaz DB taşması değil).
    /// </summary>
    public const decimal HasTavani = 999_999_999.9999m;

    /// <summary>
    /// Defter hücresi için KATI sayı okuma (binlik ayracı yok). Virgül içeren hücre
    /// SAYI_GECERSIZ döner ve mesaj doğru yazımı söyler (ör. '1,5' yerine '1.5').
    /// </summary>
    public static SatirSonucu? SayiOku(IReadOnlyDictionary<string, string> ham, string kolon, out decimal? deger)
    {
        var h = CariKaynagi.Al(ham, kolon);
        // Belirsiz sayı ('45.000') okunmadan önce reddedilir — KatiSayiOku onu 45 okurdu.
        if (CsvOkuyucu.BinlikBelirsizMi(h))
        {
            deger = null;
            return Hata(DefterHataKodu.SayiBelirsiz, CsvOkuyucu.SayiBelirsizMesaji(kolon, h!));
        }
        if (CsvOkuyucu.KatiSayiOku(h, out deger)) return null;
        var t = (h ?? "").Trim();
        return t.Contains(',')
            ? Hata(DefterHataKodu.SayiGecersiz,
                $"{kolon}: '{t}' okunamadı — ondalık ayracı NOKTA, binlik ayracı yok (ör. 1,5 yerine 1.5; 1.500,50 yerine 1500.50 yazın)")
            : Hata(DefterHataKodu.SayiGecersiz, $"{kolon} sayı olmalıdır (ondalık ayracı NOKTA, binlik ayracı yok)");
    }

    /// <summary>
    /// Eşleştirme sorgularının tavanı (B-8: sınırsız liste yok). Aşılırsa SESSİZ KIRPMA
    /// yapılmaz, kuru prova 400 ile durur (fail-closed) — yarım listeyle eşleştirme
    /// "bulunamadı" diye yanlış hata üretirdi.
    /// </summary>
    public const int SorguTavani = 100_000;

    public static List<T> TavanDenetle<T>(List<T> liste, string ne) =>
        liste.Count > SorguTavani
            ? throw new Common.BadRequestException(
                $"{ne} sayısı içe aktarım eşleştirme sınırını ({SorguTavani}) aşıyor; destekle iletişime geçin.")
            : liste;

    public static bool OndalikAsar(decimal d, int basamak) => Math.Round(d, basamak) != d;

    public static string Rakam(string? t) => new((t ?? "").Where(char.IsDigit).ToArray());

    public static SatirSonucu Hata(string kod, string mesaj) => SatirSonucu.Hata(kod, mesaj);

    public static async Task<AcilisKalemi> KalemBul(DefterBaglami b, string kalemId) =>
        await b.Db.AcilisKalemleri
            .FirstOrDefaultAsync(k => k.Id == kalemId && k.TenantId == b.Islem.TenantId)
        ?? throw new Common.ConflictException("Açılış kalemi bulunamadı; geri alma yapılamaz.");
}

// ── Açılış bakiyesi (cari) ───────────────────────────────────────────────────

/// <summary>Bir cari açılış satırı (CariId veritabanı eşleşmesinden sonra dolar).</summary>
public sealed record AcilisBakiyeSatiri(
    string Ad, string? Telefon, string? VergiNo,
    decimal Tl, decimal Has, IReadOnlyList<Kurulum.AcilisDovizBacagi> Dovizler)
{
    public string? CariId { get; init; }
}

public sealed class AcilisBakiyeKaynagi : IDefterKaynagi
{
    public string Ad => "acilis-bakiye";
    public string VarlikTuru => nameof(AcilisKalemi);

    // Dışa aktarım CariKaynagi bakiye kolonlarının AYNASI (tlBakiye, hasGramBakiye, usd/eur/gbp).
    // İşaret: ARTI = müşteri bize borçlu (VERESİYE sözleşmesi), EKSİ = müşteri alacaklı.
    public IceAktarimKolonu[] Kolonlar =>
    [
        new("ad", true, "Ayşe Yılmaz"),
        new("telefon", false, "0532 111 22 33"),
        new("vergiNo", false, ""),
        new("tlBakiye", false, "1500.00"),
        new("hasGramBakiye", false, "12.5000"),
        new("usdBakiye", false, ""),
        new("eurBakiye", false, ""),
        new("gbpBakiye", false, ""),
    ];

    public string[] KimlikKolonlari => ["telefon", "vergiNo"];
    public string[] YasakKolonParcalari => ["emanet"];

    private static readonly (string Kolon, string Pb)[] DovizKolonlari =
        [("usdBakiye", "USD"), ("eurBakiye", "EUR"), ("gbpBakiye", "GBP")];

    public SatirSonucu Dogrula(IReadOnlyDictionary<string, string> ham)
    {
        var ad = CariKaynagi.Bos(CariKaynagi.Al(ham, "ad"));
        if (ad is null) return DefterOrtak.Hata(DefterHataKodu.AlanZorunlu, "ad zorunludur (carinin adı)");

        if (DefterOrtak.SayiOku(ham, "tlBakiye", out var tl) is { } h1) return h1;
        if (DefterOrtak.SayiOku(ham, "hasGramBakiye", out var has) is { } h2) return h2;

        var tlD = tl ?? 0m; var hasD = has ?? 0m;
        if (DefterOrtak.OndalikAsar(tlD, 2))
            return DefterOrtak.Hata(DefterHataKodu.TutarOndalik, "tlBakiye en çok 2 ondalık basamak olabilir");
        if (DefterOrtak.OndalikAsar(hasD, 4))
            return DefterOrtak.Hata(DefterHataKodu.HasOndalik,
                "hasGramBakiye en çok 4 ondalık basamak olabilir (yuvarlanmaz — dosyada düzeltin)");
        if (Math.Abs(tlD) > DefterOrtak.TutarTavani || Math.Abs(hasD) > DefterOrtak.HasTavani)
            return DefterOrtak.Hata(DefterHataKodu.TutarAsiri, "Tutar kabul edilen üst sınırı aşıyor");

        var dovizler = new List<Kurulum.AcilisDovizBacagi>();
        foreach (var (kolon, pb) in DovizKolonlari)
        {
            if (DefterOrtak.SayiOku(ham, kolon, out var d) is { } h3) return h3;
            if (d is not { } dv || dv == 0m) continue;
            if (DefterOrtak.OndalikAsar(dv, 2))
                return DefterOrtak.Hata(DefterHataKodu.TutarOndalik, $"{kolon} en çok 2 ondalık basamak olabilir");
            if (Math.Abs(dv) > DefterOrtak.TutarTavani)
                return DefterOrtak.Hata(DefterHataKodu.TutarAsiri, "Tutar kabul edilen üst sınırı aşıyor");
            dovizler.Add(new Kurulum.AcilisDovizBacagi(pb, dv));
        }

        if (tlD == 0m && hasD == 0m && dovizler.Count == 0)
            return DefterOrtak.Hata(DefterHataKodu.SifirSatir,
                "Satırda en az bir bakiye (TL, has gram ya da döviz) sıfırdan farklı olmalıdır");

        return SatirSonucu.Gecerli(new AcilisBakiyeSatiri(
            ad, CariKaynagi.Bos(CariKaynagi.Al(ham, "telefon")),
            CariKaynagi.Bos(CariKaynagi.Al(ham, "vergiNo")), tlD, hasD, dovizler));
    }

    public async Task<List<SatirSonucu>> TopluDogrula(
        KuyumcuDbContext db, AktifKullanici islem, IReadOnlyList<IReadOnlyDictionary<string, string>> hamlar)
    {
        var saf = hamlar.Select(Dogrula).ToList();

        // Tek sorgu: kiracının TÜM carileri (eşleşme Türkçe kültürle BELLEKTE —
        // MySQL harmanlaması 'İ'/'I' ayrımında tr-TR ile aynı davranmaz).
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId)
            .Select(c => new { c.Id, c.Ad, c.Telefon, c.VergiNo, c.Aktif, c.SistemCarisi })
            .Take(DefterOrtak.SorguTavani + 1)
            .ToListAsync();
        DefterOrtak.TavanDenetle(cariler, "Cari");
        var acilisi = (await db.AcilisKalemleri.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && k.Tur == AcilisTuru.Cari
                            && k.Durum == AcilisKalemDurumu.Uygulandi)
                .Select(k => k.HedefId).Take(DefterOrtak.SorguTavani + 1).ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var vergiyle = cariler.Where(c => DefterOrtak.Rakam(c.VergiNo).Length > 0)
            .ToLookup(c => DefterOrtak.Rakam(c.VergiNo));
        var adTelle = cariler.ToLookup(c =>
            $"{TurkceMetin.Buyut(c.Ad).Trim()}|{DefterOrtak.Rakam(c.Telefon)}");

        var dosyada = new HashSet<string>(StringComparer.Ordinal);
        var sonuc = new List<SatirSonucu>(saf.Count);
        foreach (var s in saf)
        {
            if (s.Dto is not AcilisBakiyeSatiri d) { sonuc.Add(s); continue; }

            var adAnahtar = TurkceMetin.Buyut(d.Ad).Trim();
            var vergi = DefterOrtak.Rakam(d.VergiNo);
            var adaylar = vergi.Length > 0
                ? vergiyle[vergi].Where(c => TurkceMetin.Buyut(c.Ad).Trim() == adAnahtar || vergiyle[vergi].Count() == 1).ToList()
                : adTelle[$"{adAnahtar}|{DefterOrtak.Rakam(d.Telefon)}"].ToList();

            if (adaylar.Count == 0)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.CariYok,
                "Bu cari sistemde bulunamadı (ad + telefon ya da vergi no). Önce cari kartı dosyasını aktarın.")); continue; }
            if (adaylar.Count > 1)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.CariBelirsiz,
                "Bu bilgilerle birden fazla cari eşleşiyor; vergi no ya da telefon ekleyin.")); continue; }

            var c = adaylar[0];
            if (c.SistemCarisi)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.SistemCarisi,
                "Peşin müşteri sistem carisine açılış bakiyesi girilemez.")); continue; }
            if (!c.Aktif)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.PasifHedef, "Pasif cariye açılış bakiyesi girilemez.")); continue; }
            if (acilisi.Contains(c.Id))
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.ZatenAcilis,
                "Bu cariye daha önce açılış bakiyesi girilmiş (kurulum sihirbazı ya da önceki içe aktarım).")); continue; }
            if (!dosyada.Add(c.Id))
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.YinelenenSatir, "Aynı cari bu dosyada birden fazla kez geçiyor.")); continue; }

            sonuc.Add(SatirSonucu.Gecerli(d with { CariId = c.Id }));
        }
        return sonuc;
    }

    public async Task<string> DefterUygula(DefterBaglami b, object dto)
    {
        var d = (AcilisBakiyeSatiri)dto;
        var kalem = await b.Yazici.CariYaz(d.CariId!, d.Tl, d.Has, d.Dovizler,
            b.Gerekce, b.Kur.Id, b.Islem, b.OturumId, b.Simdi);
        return kalem.Id;
    }

    public async Task DefterGeriAl(DefterBaglami b, string kalemId, object dto)
    {
        var kalem = await DefterOrtak.KalemBul(b, kalemId);
        await b.Yazici.CariTersYaz(kalem, ((AcilisBakiyeSatiri)dto).Dovizler, b.Gerekce, b.Kur.Id, b.Islem);
    }

    public DefterOzeti Ozetle(IEnumerable<object> dtolar)
    {
        var l = dtolar.OfType<AcilisBakiyeSatiri>().ToList();
        var doviz = l.SelectMany(x => x.Dovizler).GroupBy(x => x.ParaBirimi)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar));
        return new DefterOzeti(l.Count, l.Sum(x => x.Tl), l.Sum(x => x.Has), doviz, 0m, 0m, 0m, 0m);
    }

    // Kart yolu (IIceAktarilabilir) bu kaynakta KULLANILMAZ — servis IDefterKaynagi yolunu seçer.
    public Task<string?> YinelenenBul(KuyumcuDbContext db, string tenantId, object dto) => Task.FromResult<string?>(null);
    public Task<string> Uygula(IceAktarimServisleri servisler, object dto, AktifKullanici islem) =>
        throw new InvalidOperationException("Açılış bakiyesi kart yolundan uygulanmaz.");
    public Task PasifeAl(IceAktarimServisleri servisler, string varlikId, AktifKullanici islem) =>
        throw new InvalidOperationException("Açılış bakiyesi pasife alınmaz; ters kayıtla geri alınır.");
}

// ── Açılış stoku (DEVIR partisi) ─────────────────────────────────────────────

public sealed record AcilisStokSatiri(string Kod, decimal Adet, decimal? BirimMaliyetTl)
{
    public string? UrunId { get; init; }
    public decimal HasGramBirim { get; init; }
}

public sealed class AcilisStokKaynagi : IDefterKaynagi
{
    public string Ad => "acilis-stok";
    public string VarlikTuru => nameof(AcilisKalemi);

    public IceAktarimKolonu[] Kolonlar =>
    [
        new("kod", true, "BLZ-001"),
        new("adet", true, "3"),
        new("birimMaliyetTl", false, ""),
    ];

    public string[] KimlikKolonlari => [];
    public string[] YasakKolonParcalari => ["emanet"];

    public SatirSonucu Dogrula(IReadOnlyDictionary<string, string> ham)
    {
        var kod = CariKaynagi.Bos(CariKaynagi.Al(ham, "kod"));
        if (kod is null) return DefterOrtak.Hata(DefterHataKodu.AlanZorunlu, "kod zorunludur (ürün stok kodu)");

        if (DefterOrtak.SayiOku(ham, "adet", out var adet) is { } h4) return h4;
        if (adet is not { } a || a <= 0m || a > DefterOrtak.AdetTavani || DefterOrtak.OndalikAsar(a, 3))
            return DefterOrtak.Hata(DefterHataKodu.AdetGecersiz,
                "adet sıfırdan büyük olmalı ve en çok 3 ondalık basamak taşımalıdır");

        if (DefterOrtak.SayiOku(ham, "birimMaliyetTl", out var maliyet) is { } h5) return h5;
        if (maliyet is { } m)
        {
            // FAIL-CLOSED 4: SIFIR MALİYET YAZILMAZ — bilinmiyorsa boş bırakılır (NULL).
            if (m == 0m)
                return DefterOrtak.Hata(DefterHataKodu.MaliyetSifir,
                    "birimMaliyetTl sıfır olamaz; maliyet bilinmiyorsa hücreyi BOŞ bırakın");
            if (m < 0m)
                return DefterOrtak.Hata(DefterHataKodu.MaliyetNegatif, "birimMaliyetTl eksi olamaz");
            if (DefterOrtak.OndalikAsar(m, 4) || m > DefterOrtak.TutarTavani)
                return DefterOrtak.Hata(DefterHataKodu.TutarOndalik,
                    "birimMaliyetTl en çok 4 ondalık basamak olabilir");
        }

        return SatirSonucu.Gecerli(new AcilisStokSatiri(kod, a, maliyet));
    }

    public async Task<List<SatirSonucu>> TopluDogrula(
        KuyumcuDbContext db, AktifKullanici islem, IReadOnlyList<IReadOnlyDictionary<string, string>> hamlar)
    {
        var saf = hamlar.Select(Dogrula).ToList();
        var subeId = islem.SubeId
            ?? throw new Common.BadRequestException("Açılış stoku için şube seçimi gerekli (oturumdan gelir).");

        var kodlar = saf.Select(s => s.Dto).OfType<AcilisStokSatiri>().Select(d => d.Kod).Distinct().ToList();

        var urunler = (await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && kodlar.Contains(u.Kod))
                .Select(u => new { u.Id, u.Kod, u.Aktif, u.HasGrami, u.Birim })
                .Take(DefterOrtak.SorguTavani + 1)
                .ToListAsync())
            .ToLookup(u => u.Kod, StringComparer.OrdinalIgnoreCase);
        var idler = urunler.SelectMany(g => g).Select(u => u.Id).ToList();

        var stoklu = (await db.Stoklar.AsNoTracking()
                .Where(s => s.TenantId == islem.TenantId && s.SubeId == subeId
                            && idler.Contains(s.UrunId) && s.Adet != 0m)
                .Select(s => s.UrunId).Take(DefterOrtak.SorguTavani + 1).ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var anahtarlar = idler.Select(id => AcilisKalemi.AnahtarUret(AcilisTuru.Stok, id, subeId)).ToList();
        var acilisi = (await db.AcilisKalemleri.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && k.Tur == AcilisTuru.Stok
                            && k.Durum == AcilisKalemDurumu.Uygulandi && anahtarlar.Contains(k.BenzersizAnahtar))
                .Select(k => k.HedefId).Take(DefterOrtak.SorguTavani + 1).ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var dosyada = new HashSet<string>(StringComparer.Ordinal);
        var sonuc = new List<SatirSonucu>(saf.Count);
        foreach (var s in saf)
        {
            if (s.Dto is not AcilisStokSatiri d) { sonuc.Add(s); continue; }

            var eslesen = urunler[d.Kod].ToList();
            if (eslesen.Count == 0)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.UrunYok,
                $"'{d.Kod}' kodlu ürün bulunamadı. Önce ürün kartı dosyasını aktarın.")); continue; }
            if (eslesen.Count > 1)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.UrunBelirsiz,
                $"'{d.Kod}' koduyla birden fazla ürün eşleşiyor.")); continue; }

            var u = eslesen[0];
            if (!u.Aktif)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.PasifHedef, "Pasif ürüne açılış stoku girilemez.")); continue; }
            if (acilisi.Contains(u.Id))
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.ZatenAcilis,
                "Bu ürüne bu şubede daha önce açılış stoku girilmiş.")); continue; }
            if (stoklu.Contains(u.Id))
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.StokVar,
                "Bu ürünün bu şubede stoğu zaten var. Açılış yalnız boş stoğa yazılır; fark için sayım kullanın.")); continue; }
            // PAKET GOC-C (#6 · K14): ADET birimli üründe açılış adedi parça sayısıdır — kesir
            // kuru provada SATIR HATASI olur (uygulamada 400'e kalmaz). GRAM birimde kesir serbest.
            if (!string.Equals(u.Birim, Urun.UrunAdetKapisi.GramBirimi, StringComparison.Ordinal)
                && Kuyumcu.Core.Domain.Girdi.AdetKurali.Denetle(d.Adet) is { } adetHata)
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.AdetGecersiz,
                $"'{d.Kod}' adetle satılan ürün: {Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(adetHata, Kuyumcu.Core.I18n.IstekDili.Gecerli)}")); continue; }
            if (!dosyada.Add(u.Id))
            { sonuc.Add(DefterOrtak.Hata(DefterHataKodu.YinelenenSatir, "Aynı ürün kodu bu dosyada birden fazla kez geçiyor.")); continue; }

            sonuc.Add(SatirSonucu.Gecerli(d with { UrunId = u.Id, HasGramBirim = u.HasGrami }));
        }
        return sonuc;
    }

    public async Task<string> DefterUygula(DefterBaglami b, object dto)
    {
        var d = (AcilisStokSatiri)dto;
        var urun = await b.Db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == d.UrunId && u.TenantId == b.Islem.TenantId)
            ?? throw new Common.BadRequestException($"'{d.Kod}' ürünü bulunamadı (kuru provadan sonra silinmiş olabilir).");
        var kalem = await b.Yazici.StokYaz(urun, b.Islem.SubeId!, d.Adet, d.BirimMaliyetTl,
            b.Kur, b.Gerekce, b.Islem, b.OturumId, b.Simdi);
        return kalem.Id;
    }

    public async Task DefterGeriAl(DefterBaglami b, string kalemId, object dto)
    {
        var kalem = await DefterOrtak.KalemBul(b, kalemId);
        await b.Yazici.StokTersYaz(kalem, b.Kur, b.Gerekce, b.Islem, b.Simdi);
    }

    public DefterOzeti Ozetle(IEnumerable<object> dtolar)
    {
        var l = dtolar.OfType<AcilisStokSatiri>().ToList();
        return new DefterOzeti(l.Count, 0m, 0m, new Dictionary<string, decimal>(),
            ToplamAdet: l.Sum(x => x.Adet),
            MaliyetiBilinmeyenAdet: l.Where(x => x.BirimMaliyetTl is null).Sum(x => x.Adet),
            ToplamMaliyetTl: Math.Round(l.Where(x => x.BirimMaliyetTl is not null)
                .Sum(x => x.BirimMaliyetTl!.Value * x.Adet), 2, MidpointRounding.AwayFromZero),
            ToplamHasGram: Math.Round(l.Sum(x => x.Adet * x.HasGramBirim), 4, MidpointRounding.AwayFromZero));
    }

    public Task<string?> YinelenenBul(KuyumcuDbContext db, string tenantId, object dto) => Task.FromResult<string?>(null);
    public Task<string> Uygula(IceAktarimServisleri servisler, object dto, AktifKullanici islem) =>
        throw new InvalidOperationException("Açılış stoku kart yolundan uygulanmaz.");
    public Task PasifeAl(IceAktarimServisleri servisler, string varlikId, AktifKullanici islem) =>
        throw new InvalidOperationException("Açılış stoku pasife alınmaz; ters kayıtla geri alınır.");
}
