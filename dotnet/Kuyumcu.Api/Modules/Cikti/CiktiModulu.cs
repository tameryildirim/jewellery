using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cikti;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 (kimlik Y-6) — ORTAK A4 ÇIKTI SERVİSİ.
//
// DURUM TESPİTİ (2026-09-21, kodda ölçüldü): `window.print` çağrısı üç yerdeydi
// (PusulaBelgesi:213, IslemDefteri:307, UrunEtiketleri:256) ve satış fişi,
// tahsilat makbuzu, emanet teslim belgesi, cari ekstresi için HİÇBİR çıktı
// yoktu — kuyumcu müşterisine verecek kâğıdı üretemiyordu.
//
// BU SERVİS NE YAPAR: dört belge türünü TEK sözleşmeye (`OrtakBelge`)
// indirger. Kâğıt düzeni istemcide TEK bileşendedir (Bilesenler/Yazdirma).
//
// ⚠ SALT OKURDUR: deftere hiçbir şey yazmaz. Tek yazdığı şey DENETİM İZİdir
//   (KVKK md.12 — kişisel veri taşıyan belge görüntülendi).
// ⚠ MODÜL KAPISI BELGE TÜRÜNE GÖREDİR: tek bir [ModulYetkisi] bu ucu ifade
//   edemezdi (fiş "islem", makbuz/ekstre "cari", emanet "emanet"). Bu yüzden
//   kapı controller niteliğinde değil, SERVİSTE tür başına sorulur —
//   AramaServisi'nin (P-15) kurduğu desenin aynısı. Fail-closed: bilinmeyen
//   tür 400, yetkisiz tür 403.
// ⚠ ADR-0007: sunucuda PDF üretimi YOK; sunucu veriyi verir, kâğıdı tarayıcı basar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CiktiServisi(
    KuyumcuDbContext db,
    Yetki.YetkiServisi yetki,
    DenetimServisi denetim,
    Cari.KvkkServisi? kvkk = null)
{
    /// <summary>Cari ekstresine basılacak en fazla satır — UX-S29: sınır ekranda yazar.</summary>
    public const int EkstreSatirTavani = 200;

    /// <summary>Belge türü → gereken yetki modülü. Kapalı liste; geri dal YOK.</summary>
    public static string Modul(string tur) => tur switch
    {
        BelgeTurleri.SatisFisi => "islem",
        BelgeTurleri.TahsilatMakbuzu => "cari",
        BelgeTurleri.EmanetTeslim => "emanet",
        BelgeTurleri.CariEkstre => "cari",
        // PAKET EKSIK-B1-SATINALMA: irsaliye belge ailesindendir ("fatura" modülü) VE ayrıca
        // IrsaliyeController ile AYNI rol kapısından geçer (IrsaliyeRolleri) — tezgâhtar 403.
        BelgeTurleri.Irsaliye => "fatura",
        // D1-01: fatura dökümü "fatura" modülündedir; tezgâhtar da (yetki matrisi izin veriyorsa)
        // müşteriye kesilen faturanın kâğıdını verebilir — satış fişiyle aynı mantık.
        BelgeTurleri.Fatura => "fatura",
        BelgeTurleri.CariMutabakat => "cari",   // D1-10: ekstreyle aynı kapı
        BelgeTurleri.KvkkAydinlatma => "cari",  // D1-19
        BelgeTurleri.KimlikTespit => "uyum",    // D1-13 — + rol kapısı (MASAK kimlik ucuyla AYNI)
        BelgeTurleri.GarantiKarti => "islem",   // D1-13 — satış fişiyle aynı kapı
        BelgeTurleri.OdemePlani => "cari",      // D1-06 — ödeme planı ucuyla aynı kapı
        BelgeTurleri.UyumRaporu => "uyum",      // D1-18 — + rol kapısı (sahip/müdür)
        BelgeTurleri.Teklif => "islem",         // Y-11 — tezgâhın bekletme ucuyla aynı kapı
        _ => throw new BadRequestException($"Bilinmeyen belge türü: {tur}"),
    };

    /// <summary>İrsaliye dökümünü görebilen roller — /api/v1/irsaliyeler [RolGerekli] ile AYNI.</summary>
    public static readonly string[] IrsaliyeRolleri = ["SAHIP", "MUDUR", "MUHASEBE"];

    public async Task<OrtakBelge> Belge(string tur, string id, AktifKullanici islem, CancellationToken iptal = default)
    {
        if (!BelgeTurleri.Gecerli(tur))
            throw new BadRequestException(
                $"Bilinmeyen belge türü: {tur}. Kabul edilenler: {string.Join(", ", BelgeTurleri.Tumu)}");

        var modul = Modul(tur);
        if (tur == BelgeTurleri.Irsaliye && !IrsaliyeRolleri.Contains(islem.Rol))
            throw new ForbiddenException("Bu belgeyi görüntüleme yetkiniz yok (irsaliye)");
        if (tur == BelgeTurleri.UyumRaporu && islem.Rol is not ("SAHIP" or "MUDUR"))
            throw new ForbiddenException("Bu belgeyi görüntüleme yetkiniz yok (uyum raporu)");
        if (tur == BelgeTurleri.KimlikTespit && !IrsaliyeRolleri.Contains(islem.Rol))
            throw new ForbiddenException("Bu belgeyi görüntüleme yetkiniz yok (kimlik tespiti)");
        if (!await yetki.ErisebilirMi(islem.TenantId, islem.Rol, modul))
            throw new ForbiddenException($"Bu belgeyi görüntüleme yetkiniz yok ({modul})");

        var belge = tur switch
        {
            BelgeTurleri.CariEkstre => await Ekstre(id, islem, iptal),
            BelgeTurleri.Irsaliye => await IrsaliyeBelgesi(id, islem, iptal),
            BelgeTurleri.Fatura => await FaturaBelgesi(id, islem, iptal),
            BelgeTurleri.CariMutabakat => await MutabakatBelgesi(id, islem, iptal),
            BelgeTurleri.KvkkAydinlatma => await KvkkBelgesi(id, islem, iptal),
            BelgeTurleri.KimlikTespit => await KimlikTespitBelgesi(id, islem, iptal),
            BelgeTurleri.GarantiKarti => await GarantiKartiBelgesi(id, islem, iptal),
            BelgeTurleri.OdemePlani => await OdemePlaniBelgesi(id, islem, iptal),
            BelgeTurleri.UyumRaporu => await UyumRaporuBelgesi(id, islem, iptal),
            BelgeTurleri.Teklif => await TeklifBelgesi(id, islem, iptal),
            _ => await HareketBelgesi(tur, id, islem, iptal),
        };

        // Denetim izi YENİDEN YAZILMAZ — mevcut servis çağrılır (ADR-0021).
        await denetim.OkumaKaydet(islem, "CiktiBelgesi", id, $"{tur} belgesi görüntülendi");
        return belge;
    }

    // ── Kimlik (düzenleyen) ─────────────────────────────────────────────────

    private async Task<CiktiKimlik> Kimlik(string tenantId, string? subeId, CancellationToken iptal)
    {
        var t = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, iptal);
        var sube = subeId is null
            ? null
            : await db.Subeler.AsNoTracking()
                .Where(s => s.Id == subeId && s.TenantId == tenantId)
                .Select(s => s.Ad).FirstOrDefaultAsync(iptal);

        // ⚠ Boş alanlar UYDURULMAZ (Anayasa md.1): null döner, ekran
        //   "işletme kartından doldurun" ipucunu gösterir.
        // PAKET-04 Faz B: işletme logosu — İşletme ekranından yüklenen en yeni GORSEL (yoksa null, uydurulmaz).
        var logo = await db.Belgeler.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.SahipTuru == BelgeSahipTuru.Isletme && b.Tur == BelgeTuru.Gorsel && b.Aktif)
            .OrderByDescending(b => b.Olusturuldu).Select(b => b.Id).FirstOrDefaultAsync(iptal);
        return new CiktiKimlik(t?.Ad, sube, t?.Adres, t?.VergiDairesi, t?.VergiNo, t?.Telefon, logo);
    }

    private async Task<CiktiTaraf?> Taraf(string? cariId, string tenantId, string baslik, CancellationToken iptal)
    {
        if (cariId is null) return null;
        var c = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariId && x.TenantId == tenantId, iptal);
        return c is null ? null : new CiktiTaraf(baslik, c.Ad, c.VergiNo, c.Adres, c.Telefon);
    }

    // ── Hareket tabanlı üç belge ────────────────────────────────────────────

    private async Task<OrtakBelge> HareketBelgesi(
        string tur, string id, AktifKullanici islem, CancellationToken iptal)
    {
        var h = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, iptal)
            ?? throw new NotFoundException("Hareket bulunamadı");

        var beklenen = BeklenenTurler(tur);
        if (!beklenen.Contains(h.Tur))
            throw new BadRequestException(
                $"Bu hareket '{tur}' belgesi üretmez (hareket türü: {h.Tur}). " +
                $"Beklenen: {string.Join(", ", beklenen)}");

        // Fiş bacakları: çok bacaklı fişte tek satır yarım belge olurdu.
        var bacaklar = h.FisId is null
            ? [h]
            : await db.Hareketler.AsNoTracking()
                .Where(x => x.FisId == h.FisId)
                .OrderBy(x => x.Tarih).ThenBy(x => x.Id)
                .ToListAsync(iptal);

        var urunAdlari = await UrunAdlari(bacaklar.Select(b => b.UrunId), islem.TenantId, iptal);
        // M5 (ses kaydı 07:07: "hem gramını görmem hem de değerini görmem lazım… hepsini has'a çeviremezsin"):
        // ürünün GRAMI (gram × adet) kâğıtta has'tan ÖNCE; ürünsüz satırda "—". Ürün yoksa sütun hiç açılmaz.
        var urunGramlari = await UrunGramlari(bacaklar.Select(b => b.UrunId), islem.TenantId, iptal);
        var gramSutunu = bacaklar.Any(b => b.UrunId is not null);
        string GramHucre(Hareket b) => b.UrunId is not null && urunGramlari.TryGetValue(b.UrunId, out var g) && g > 0m
            ? Sayi(g * Math.Abs(b.Adet ?? 1m), 3) : "—";

        // ⚠ Para birimi TUTARLA BİRLİKTE taşınır (UX-S32) — "varsayılan TL" yok.
        //   Kalem satırları ve toplam AYNI birimi taşır (Q9-4: kalem-toplam tutarlı).
        var paraBirimi = bacaklar.Select(b => b.ParaBirimi).Distinct(StringComparer.Ordinal).Count() == 1
            ? bacaklar[0].ParaBirimi
            : "TRY";

        // PAKET Q9-4 (QA turu 9): müşteriye verilen belgede YÖN "Tür" sütunundadır
        // (Satış / Alış …); defterin iç işareti (satışta has −, alışta TL −) kâğıda
        // BASILMAZ. Sayı Invariant dizilir, biçimi istemci SutunBicimleri ile verir
        // (has: virgül + 4 hane — toplamla aynı; tutar: ₺45.000,00).
        // KAPORA-EKRAN: kapora makbuzu (siparişe bağlı, has etkisiz tahsilat) "Has gram" sütununu taşımaz —
        // her satırı "0,0000 has" olan sütun kâğıtta gürültüdür. Yalnız bu tür; diğer belgeler aynen.
        var hasSutunuYok = tur == BelgeTurleri.TahsilatMakbuzu
            && bacaklar.All(b => b.SiparisId is not null && b.HasGrami == 0m);
        var satirlar = bacaklar.Select(b => new CiktiSatir(hasSutunuYok
            ? [
                Sozluk.HareketTuru(b.Tur),
                b.UrunId is not null && urunAdlari.TryGetValue(b.UrunId, out var ad0) ? ad0 : (b.Aciklama ?? "—"),
                Sayi(Math.Abs(b.TlTutari), 2),
            ]
            : gramSutunu ? [
                Sozluk.HareketTuru(b.Tur),
                b.UrunId is not null && urunAdlari.TryGetValue(b.UrunId, out var adG) ? adG : (b.Aciklama ?? "—"),
                GramHucre(b),
                Sayi(Math.Abs(b.HasGrami), 4),
                Sayi(Math.Abs(b.TlTutari), 2),
            ]
            : [
                Sozluk.HareketTuru(b.Tur),
                b.UrunId is not null && urunAdlari.TryGetValue(b.UrunId, out var ad) ? ad : (b.Aciklama ?? "—"),
                Sayi(Math.Abs(b.HasGrami), 4),
                Sayi(Math.Abs(b.TlTutari), 2),
            ], ParaBirimi: paraBirimi)).ToList();

        var bolum = new CiktiBolum(
            tur == BelgeTurleri.EmanetTeslim ? "Emanet Kalemleri" : "Kalemler",
            hasSutunuYok ? ["Tür", "Açıklama", "Tutar"]
                : gramSutunu ? ["Tür", "Açıklama", "Gram", "Has gram", "Tutar"]
                : ["Tür", "Açıklama", "Has gram", "Tutar"],
            satirlar,
            bacaklar.Count > 1 ? $"Bu belge {bacaklar.Count} bacaklı tek fişin tamamını gösterir." : null,
            SutunBicimleri: hasSutunuYok
                ? [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para]
                : gramSutunu ? [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Gram, SutunBicimi.Has, SutunBicimi.Para]
                : [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Has, SutunBicimi.Para]);

        var tutarlar = new List<CiktiTutar>
        {
            ToplamSatiri("Toplam tutar", "Net tutar", bacaklar.Select(b => b.TlTutari).ToList(),
                paraBirimi, pozitifYon: "müşteriden alınan", negatifYon: "müşteriye ödenen", toplam: true),
        };
        if (bacaklar.Any(b => b.HasGrami != 0m))
            tutarlar.Add(ToplamSatiri("Toplam has gram", "Net has gram", bacaklar.Select(b => b.HasGrami).ToList(),
                "HAS_GRAM", pozitifYon: "müşteriden alınan", negatifYon: "müşteriye verilen", toplam: false));

        var stornolu = await db.Hareketler.AsNoTracking()
            .AnyAsync(x => x.StornoKaynakId == h.Id, iptal);

        return new OrtakBelge(
            Tur: tur,
            BelgeAdi: BelgeAdi(tur),
            BelgeNo: h.FisId ?? h.Id,
            Dayanak: null,
            DuzenlemeTarihi: h.Tarih,
            Duzenleyen: await Kimlik(islem.TenantId, h.SubeId, iptal),
            Karsi: await Taraf(h.CariId, islem.TenantId, "Müşteri", iptal),
            Bolumler: [bolum],
            Tutarlar: tutarlar,
            ImzaBasliklari: ImzaBasliklari(tur),
            Not: h.Aciklama,
            // ⚠ UX-S28/md.1: hükümsüz kılınmış kayıt KÂĞIDA DA yazılır; aksi hâlde
            //   iptal edilmiş bir fiş geçerli belge gibi elden ele dolaşır.
            Uyari: stornolu || h.StornoKaynakId is not null
                ? "BU KAYIT STORNO EDİLMİŞTİR — geçerli bir belge değildir."
                : null,
            // Q9-4: hareket tablosunda okunur fiş sıra numarası YOKTUR (sütun yok);
            // numara uydurulmaz, ekran bunun kayıt numarası olduğunu söyler.
            BelgeNoKayitKimligi: true);
    }

    private static string[] BeklenenTurler(string tur) => tur switch
    {
        BelgeTurleri.SatisFisi => ["SATIS", "SATIS_IADE", "ALIS", "ALIS_IADE"],
        BelgeTurleri.TahsilatMakbuzu => ["TAHSILAT", "VERESIYE"],
        BelgeTurleri.EmanetTeslim => ["EMANET_GIRIS", "EMANET_CIKIS"],
        _ => throw new BadRequestException($"Bilinmeyen belge türü: {tur}"),
    };

    // ── Cari ekstresi ───────────────────────────────────────────────────────

    private async Task<OrtakBelge> Ekstre(string cariId, AktifKullanici islem, CancellationToken iptal)
    {
        var c = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariId, iptal)
            ?? throw new NotFoundException("Cari bulunamadı");

        var hareketler = await db.Hareketler.AsNoTracking()
            .Where(h => h.CariId == cariId)
            .OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id)
            .Take(EkstreSatirTavani + 1)
            .ToListAsync(iptal);

        var sinirDegdi = hareketler.Count > EkstreSatirTavani;
        if (sinirDegdi) hareketler.RemoveAt(hareketler.Count - 1);

        var bakiye = await db.CariBakiyeler.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CariId == cariId, iptal);
        // PAKET CARI-BAKIYE (tek tanım): kâğıttaki has bakiyesi ekrandakiyle AYNI (emanetsiz);
        // emanet ayrı satırda "bakiyeye girmez" diye yazılır.
        var emanetEtkisi = bakiye is null ? 0m
            : await CariBakiyeKurali.EmanetEtkisi(db, bakiye.TenantId, cariId);

        // ═══ PAKET PARA-DEMO (madde 5) — EKSTRE OKUNURLUĞU ═══════════════════════
        // Ölçüldü (01.10, güncel ağaç): satırdaki "Tutar" işlemin HAM TL'siydi; peşin
        // (nakit/kart) satış da tutar olarak listeleniyordu, ödeme türü yazmıyordu →
        // kalemler toplanınca bakiye çıkmıyordu (Kemal: satırlar 28.327,48 · bakiye
        // 10.422,07). Artık: "İşlem" = tür · ödeme (Nakit/Kart/Havale/Has), "Tutar" =
        // işlemin kendisi (bilgi), "Borç etkisi" = satırın cari TL bakiyesine etkisi
        // (CariBakiyeKurali.TlEtkisi — ekranla TEK kural), "Bakiye" = yürüyen bakiye
        // (önceki + satır). Yürüyen bakiye kayıtlı bakiyeden GERİYE doğru kurulur;
        // etkisi bilinmeyen (Faz 0 öncesi) satırdan daha eskisi "—" yazar, uydurulmaz.
        var sirali = hareketler.OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
        var etkiler = sirali.Select(h => CariBakiyeKurali.TlEtkisi(
            h.Tur, h.TlTutari, h.VeresiyeTutar, h.StornoKaynakId is not null)).ToList();
        var yuruyen = new decimal?[sirali.Count];
        decimal? sonra = bakiye?.TlBakiye ?? 0m;
        for (var i = sirali.Count - 1; i >= 0; i--)
        {
            yuruyen[i] = sonra;
            sonra = sonra is { } s2 && etkiler[i] is { } e2 ? s2 - e2 : null;
        }

        // PARA-DEMO-2 (madde 9): has etkisi olmayan satırda has hücresi BOŞ ("0,0000 has" basılmaz).
        // Eksi işareti tek tip U+2212 (ekrandaki Bicim.Tl / HasBicim ile aynı) — bu hücreler sunucuda biçimlenir.
        var hasEtkileri = sirali.Select(h => Modules.Cari.CariServisi.BakiyeHasEtkisi(
            h.Tur, h.HasGrami, h.Maden, h.StornoKaynakId is not null)).ToList();
        // PARA-DEMO-2 (madde 1): gerçek karma satırın bacak araçları ("Kart + Havale").
        var karmaFisler = sirali.Where(h => h.OdemeTuru == "KARMA").Select(h => h.FisId ?? h.Id)
            .Distinct(StringComparer.Ordinal).ToList();
        var karmaAraclari = karmaFisler.Count == 0
            ? new Dictionary<string, List<string>>(StringComparer.Ordinal)
            : (await db.OdemeKalemleri.AsNoTracking()
                    .Where(k => k.TenantId == c.TenantId && karmaFisler.Contains(k.FisId) && !k.Bilinmiyor)
                    .Select(k => new { k.FisId, k.Arac, k.Sira })
                    .ToListAsync(iptal))
                .GroupBy(k => k.FisId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Sira).Select(x => x.Arac).Distinct(StringComparer.Ordinal).ToList(),
                    StringComparer.Ordinal);

        var satirlar = sirali
            .Select((h, i) => new CiktiSatir([
                h.Tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                EkstreIslemAdi(h, karmaAraclari.GetValueOrDefault(h.FisId ?? h.Id)),
                h.Aciklama ?? "—",
                // Q9-4: biçim kalem ile bakiye satırında AYNIDIR (has 4 hane, tutar ₺).
                Sayi(h.TlTutari, 2),
                hasEtkileri[i] == 0m ? "" : EkstreHas(hasEtkileri[i]),
                etkiler[i] is { } e ? Sayi(e, 2) : "—",
                // Bakiye hücresi TL kalır: has bakiyesi bu hücreye eklenince (ölçüldü, 1280 px) İşlem sütunu
                // sıkışıp "Satış · Veresiye" iki satıra kırılıyordu; kâğıtta has bakiyesi Tutarlar bölümünde,
                // ekranda (Cariler detay) has satırının bakiye satırında yazar.
                yuruyen[i] is { } y ? Sayi(y, 2) : "—",
            ], Vurgu: h.StornoKaynakId is not null, ParaBirimi: "TRY"))
            .ToList();

        var bolum = new CiktiBolum(
            "Hareketler",
            ["Tarih", "İşlem", "Açıklama", "Tutar", "Has etkisi", "Borç etkisi", "Bakiye"],
            satirlar,
            // UX-S29: sunucu sınırı EKRANDA (ve kâğıtta) yazar; sessiz kırpma yok.
            sinirDegdi
                ? $"Bu belgede yalnız son {EkstreSatirTavani} hareket var; daha eski hareketler bu belgeye sığmadı."
                : $"Bu belgede en fazla {EkstreSatirTavani} hareket gösterilir; bu hesabın tüm hareketleri belgede.",
            SutunBicimleri: [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para,
                             SutunBicimi.Has, SutunBicimi.Para, SutunBicimi.Para]);

        var tutarlar = new List<CiktiTutar>
        {
            new("TL bakiye", bakiye?.TlBakiye ?? 0m, "TRY", Toplam: true),
            new("Has gram bakiye", CariBakiyeKurali.EmanetsizHas(bakiye?.HasGramBakiye ?? 0m, emanetEtkisi), "HAS_GRAM"),
        };
        if (emanetEtkisi != 0m) tutarlar.Add(new CiktiTutar("Emanette (bakiyeye girmez)", -emanetEtkisi, "HAS_GRAM"));
        if ((bakiye?.UsdBakiye ?? 0m) != 0m) tutarlar.Add(new CiktiTutar("USD bakiye", bakiye!.UsdBakiye, "USD"));
        if ((bakiye?.EurBakiye ?? 0m) != 0m) tutarlar.Add(new CiktiTutar("EUR bakiye", bakiye!.EurBakiye, "EUR"));
        if ((bakiye?.GbpBakiye ?? 0m) != 0m) tutarlar.Add(new CiktiTutar("GBP bakiye", bakiye!.GbpBakiye, "GBP"));

        return new OrtakBelge(
            Tur: BelgeTurleri.CariEkstre,
            BelgeAdi: BelgeAdi(BelgeTurleri.CariEkstre),
            BelgeNo: c.Id,
            Dayanak: null,
            DuzenlemeTarihi: DateTime.UtcNow,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: new CiktiTaraf(CariTarafEtiketi(c.Tur), c.Ad, c.VergiNo, c.Adres, c.Telefon),
            Bolumler: [bolum],
            Tutarlar: tutarlar,
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.CariEkstre),
            Not: null,
            Uyari: c.Aktif ? null : "Bu cari PASİF durumdadır.",
            BelgeNoKayitKimligi: true);
    }

    // ── İrsaliye dökümü (PAKET EKSIK-B1-SATINALMA · B1-2) ──────────────────

    /// <summary>
    /// Kâğıda basılan yasal sınır uyarısı. Birincil kaynak: 213 s. VUK md.230/5 (irsaliyede fiyat
    /// ve bedel hariç fatura bilgileri; malın nereye ve kime gönderildiği) ve md.353/2 (elektronik
    /// düzenlenmesi gereken belgenin kâğıt düzenlenmesi özel usulsüzlük) —
    /// https://www.mevzuat.gov.tr/MevzuatMetin/1.4.213.pdf. Sistem numarası anlaşmalı matbaa
    /// seri-sıra numarası değildir → döküm yasal irsaliye YERİNE GEÇMEZ (fail-closed beyan).
    /// </summary>
    public const string IrsaliyeYasalUyari =
        "BİLGİ AMAÇLI DÖKÜMDÜR — yasal sevk irsaliyesi ya da e-İrsaliye yerine geçmez (VUK md.230/5).";

    private async Task<OrtakBelge> IrsaliyeBelgesi(string id, AktifKullanici islem, CancellationToken iptal)
    {
        var i = await db.Irsaliyeler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("İrsaliye bulunamadı");
        var kalemler = await db.IrsaliyeKalemleri.AsNoTracking()
            .Where(k => k.IrsaliyeId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira).Take(Data.Irsaliye.AzamiKalem).ToListAsync(iptal);
        var urunIdler = kalemler.Where(k => k.UrunId is not null).Select(k => k.UrunId!).Distinct().ToList();
        var urunler = urunIdler.Count == 0
            ? new Dictionary<string, (string Ad, string Kod, string Birim)>()
            : (await db.Urunler.AsNoTracking()
                .Where(u => urunIdler.Contains(u.Id) && u.TenantId == islem.TenantId)
                .Select(u => new { u.Id, u.Ad, u.Kod, u.Birim }).ToListAsync(iptal))
                .ToDictionary(u => u.Id, u => (u.Ad, u.Kod, u.Birim));

        var satirlar = kalemler.Select(k =>
        {
            var u = k.UrunId is not null && urunler.TryGetValue(k.UrunId, out var x) ? x : default;
            var ad = u.Ad ?? k.Tanim ?? "—";
            return new CiktiSatir([
                (k.Sira + 1).ToString(CultureInfo.InvariantCulture),
                ad,
                u.Kod ?? "—",
                IrsaliyeMiktar(k.Adet, u.Birim),
            ]);
        }).ToList();

        var gelen = i.Yon == IrsaliyeYonu.Gelen;
        string? dayanak = null;
        if (i.SatinalmaSiparisId is { } sid)
            dayanak = await db.SatinalmaSiparisleri.AsNoTracking()
                .Where(s => s.Id == sid && s.TenantId == islem.TenantId)
                .Select(s => "Satın alma siparişi " + s.No).FirstOrDefaultAsync(iptal);
        string? faturaNotu = null;
        if (i.FaturaId is { } fid)
            faturaNotu = await db.Faturalar.AsNoTracking()
                .Where(f => f.Id == fid && f.TenantId == islem.TenantId)
                .Select(f => "Bağlı fatura: " + (f.FaturaNo ?? f.Id)).FirstOrDefaultAsync(iptal);
        var cari = await Taraf(i.CariId, islem.TenantId, gelen ? "Gönderen (tedarikçi)" : "Teslim alan", iptal);
        var duzenleyen = await Kimlik(islem.TenantId, i.SubeId, iptal);
        // PAKET SATINALMA-EKRAN: TESLİM ADRESİ. Sevkte malı alan tarafın (cari) adresi; mal kabulde malın
        // geldiği yer, yani işletmenin kendi adresi. Kayıtlı adres yoksa UYDURULMAZ: eksik olduğu yazılır.
        var teslimAdresi = gelen ? duzenleyen.Adres : cari?.Adres;
        // SATINALMA-EKRAN-2: adres yoksa satır HİÇ basılmaz (kâğıda yönerge cümlesi yazılmaz).
        var adresNotu = string.IsNullOrWhiteSpace(teslimAdresi) ? null : "Teslim adresi: " + teslimAdresi.Trim();
        // PAKET-42: VUK sevk bilgileri kâğıda — yalnız dolu olanlar (boş alan için yönerge cümlesi yazılmaz).
        string? sevkNotu = null;
        if (!gelen)
        {
            var parcalar = new List<string>();
            if (!string.IsNullOrWhiteSpace(i.TasiyanAd)) parcalar.Add("Taşıyan: " + i.TasiyanAd);
            if (!string.IsNullOrWhiteSpace(i.Plaka)) parcalar.Add("Plaka: " + i.Plaka);
            if (i.FiiliSevkTarihi is { } fst) parcalar.Add("Fiili sevk: " + Zaman.UtcdenYerele(fst).ToString("dd.MM.yyyy HH:mm"));
            if (!string.IsNullOrWhiteSpace(i.TeslimAlanAd)) parcalar.Add("Teslim alan: " + i.TeslimAlanAd + (i.TeslimTarihi is { } tt ? " (" + Zaman.UtcdenYerele(tt).ToString("dd.MM.yyyy HH:mm") + ")" : ""));
            if (parcalar.Count > 0) sevkNotu = string.Join(" · ", parcalar);
        }
        var not = string.Join(" · ", new[] { adresNotu, sevkNotu, i.Aciklama, faturaNotu }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new OrtakBelge(
            Tur: BelgeTurleri.Irsaliye,
            BelgeAdi: gelen ? "MAL KABUL İRSALİYESİ" : "SEVK İRSALİYESİ",
            BelgeNo: i.No,
            Dayanak: dayanak,
            DuzenlemeTarihi: i.Tarih,
            Duzenleyen: duzenleyen,
            Karsi: cari,
            Bolumler: [new CiktiBolum("Kalemler", ["Sıra", "Ürün / tanım", "Kod", "Miktar"], satirlar,
                $"{satirlar.Count} kalem · fiyat ve bedel irsaliyede yazmaz.",
                SutunBicimleri: [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin])],
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.Irsaliye),
            Not: not,
            Uyari: i.Durum == IrsaliyeDurumu.Iptal
                ? "BU İRSALİYE İPTAL EDİLMİŞTİR — geçerli bir belge değildir. " + IrsaliyeYasalUyari
                : IrsaliyeYasalUyari,
            BelgeNoKayitKimligi: false);
    }

    // ── D1-10 (18.10) — cari hesap mutabakat formu ─────────────────────────

    public const string MutabakatMetni =
        "Kayıtlarımıza göre bu tarih itibarıyla hesabınızın bakiyesi yukarıdadır. Mutabık olup olmadığınızı aşağıda işaretleyip imzalayarak bildirmenizi rica ederiz.";
    public const string MutabakatBizeBorclu = "Bize borçlu";
    public const string MutabakatBizBorcluyuz = "Size borçluyuz";
    public const string MutabakatBorcYok = "Borç yok";
    public const string MutabakatMutabikiz = "☐ Mutabıkız";
    public const string MutabakatMutabikDegiliz = "☐ Mutabık değiliz — bizim kayıtlarımıza göre bakiye:";

    /// <summary>
    /// Bakiyeler EKRANDAKİ ile aynı kaynaktan (CariBakiyeler; has emanetsiz — CariBakiyeKurali). Yön "bize borçlu /
    /// size borçluyuz" yazılır, tutar işaretsiz basılır (artı = müşteri bize borçlu; BorcDili kuralı).
    /// </summary>
    /// <summary>D1-06 — ödeme planı: taksit no · vade · tutar (+ senetli planda senet no); vade farkı yok.</summary>
    private async Task<OrtakBelge> OdemePlaniBelgesi(string planId, AktifKullanici islem, CancellationToken iptal)
    {
        var p = await db.OdemePlanlari.AsNoTracking().FirstOrDefaultAsync(x => x.Id == planId && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Ödeme planı bulunamadı");
        var c = await db.Cariler.AsNoTracking().FirstAsync(x => x.Id == p.CariId && x.TenantId == islem.TenantId, iptal);
        var taksitler = await db.OdemePlaniTaksitleri.AsNoTracking().Where(t => t.PlanId == p.Id && t.TenantId == islem.TenantId)
            .OrderBy(t => t.Sira).Take(36).ToListAsync(iptal);
        var evrakIdler = taksitler.Where(t => t.EvrakId != null).Select(t => t.EvrakId!).ToList();
        var senetNo = await db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == islem.TenantId && evrakIdler.Contains(e.Id))
            .Take(evrakIdler.Count + 1).ToDictionaryAsync(e => e.Id, e => e.EvrakNo, iptal);
        var satirlar = taksitler.Select(t => new CiktiSatir(p.Senetli
            ? [$"{t.Sira}", t.Vade.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), t.EvrakId is null ? "" : senetNo.GetValueOrDefault(t.EvrakId) ?? "", Sayi(t.TutarTl, 2)]
            : [$"{t.Sira}", t.Vade.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), Sayi(t.TutarTl, 2)], ParaBirimi: "TRY")).ToList();
        var bolum = p.Senetli
            ? new CiktiBolum("Taksitler", ["No", "Vade", "Senet no", "Tutar"], satirlar, null, [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para])
            : new CiktiBolum("Taksitler", ["No", "Vade", "Tutar"], satirlar, null, [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para]);
        return new OrtakBelge(
            Tur: BelgeTurleri.OdemePlani,
            BelgeAdi: BelgeAdi(BelgeTurleri.OdemePlani),
            BelgeNo: p.Id,
            Dayanak: null,
            DuzenlemeTarihi: p.Olusturuldu,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: new CiktiTaraf(CariTarafEtiketi(c.Tur), c.Ad, c.VergiNo, c.Adres, c.Telefon),
            Bolumler: [bolum],
            Tutarlar: [new CiktiTutar("Toplam borç", p.ToplamTl, "TRY", Toplam: true)],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.OdemePlani),
            Not: OdemePlaniMetni,
            Uyari: null,
            BelgeNoKayitKimligi: true);
    }

    // ── Y-11 (20.10) — fiyat teklif fişi: bekletilen fişin kur kilidiyle süreli dökümü ──────────────────
    public const string TeklifMetni = "Bu teklif yukarıdaki tarihe kadar, yazılı has fiyatıyla geçerlidir; süre dolunca fiyat güncel kurla yeniden hesaplanır. Teklif satış değildir; stok ayrılmaz.";

    private async Task<OrtakBelge> TeklifBelgesi(string id, AktifKullanici islem, CancellationToken iptal)
    {
        var b = await db.BekleyenFisler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId && x.TeklifNo != null, iptal)
            ?? throw new NotFoundException("Fiyat teklifi bulunamadı");
        var ic = System.Text.Json.JsonSerializer.Deserialize<Kuyumcu.Contracts.Sayfalar.Islemler.BekleyenFisIcerik>(b.Icerik, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            ?? throw new BadRequestException("Teklif içeriği okunamadı");
        var cari = b.CariId is null ? null : await db.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == b.CariId && c.TenantId == islem.TenantId, iptal);
        decimal? hasFiyati = b.KurKilidiId is null ? null : await (
            from k in db.KurKilitleri.AsNoTracking().Where(k => k.Id == b.KurKilidiId && k.TenantId == islem.TenantId)
            join s in db.KurSnapshots.AsNoTracking() on k.KurSnapshotId equals s.Id
            select (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync(iptal);
        var satirlar = ic.Kalemler.Select((k, i) =>
        {
            var brut = Math.Round(k.Adet * k.BirimFiyat, 2, MidpointRounding.AwayFromZero);
            var net = k.Hediye ? 0m : Math.Round(brut * (1m - k.SatirIskontoYuzde / 100m), 2, MidpointRounding.AwayFromZero);
            return new CiktiSatir([$"{i + 1}", k.UrunAdi + (k.Hediye ? " (hediye)" : ""), Sayi(k.Adet, 3), Sayi(k.BirimFiyat, 2),
                k.SatirIskontoYuzde > 0m ? "%" + Sayi(k.SatirIskontoYuzde, 2) : "", Sayi(net, 2)], ParaBirimi: "TRY");
        }).ToList();
        var not = $"Geçerlilik: {Zaman.UtcdenYerele(b.TeklifBitis!.Value):dd.MM.yyyy HH:mm}"
            + (hasFiyati is { } hf ? $" · Has fiyatı (kilitli): {Sayi(hf, 2)} TL/g" : "")
            + (ic.FisIskontoYuzde is > 0m ? $" · Fiş indirimi %{Sayi(ic.FisIskontoYuzde.Value, 2)}" : "")
            + (string.IsNullOrWhiteSpace(ic.Aciklama) ? "" : " · " + ic.Aciklama)
            + " · " + TeklifMetni;
        return new OrtakBelge(
            Tur: BelgeTurleri.Teklif,
            BelgeAdi: BelgeAdi(BelgeTurleri.Teklif),
            BelgeNo: b.TeklifNo!,
            Dayanak: null,
            DuzenlemeTarihi: b.Olusturuldu,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: cari is null ? null : new CiktiTaraf(CariTarafEtiketi(cari.Tur), cari.Ad, cari.VergiNo, cari.Adres, cari.Telefon),
            Bolumler: [new CiktiBolum("Kalemler", ["Sıra", "Ürün", "Adet", "Birim fiyat", "İndirim", "Tutar"], satirlar, null,
                [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para, SutunBicimi.Metin, SutunBicimi.Para])],
            Tutarlar: [new CiktiTutar("Teklif toplamı", b.ToplamTl, "TRY", Toplam: true)],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.Teklif),
            Not: not,
            Uyari: b.TeklifBitis <= DateTime.UtcNow ? "BU TEKLİFİN SÜRESİ DOLMUŞTUR — fiyatlar geçerli değildir." : null,
            BelgeNoKayitKimligi: false);
    }

    /// <summary>D1-18 — dönemlik uyum raporu: kimlik tespiti, ŞİB, yaptırım listesi, hassas kayıt erişimi (yalnız sayılar).</summary>
    private async Task<OrtakBelge> UyumRaporuBelgesi(string id, AktifKullanici islem, CancellationToken iptal)
    {
        var p = id.Split('_');
        if (p.Length != 2 || !DateOnly.TryParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bas)
            || !DateOnly.TryParseExact(p[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bit) || bit < bas || bit.DayNumber - bas.DayNumber > 400)
            throw new BadRequestException("Dönem yyyy-MM-dd_yyyy-MM-dd biçiminde ve en çok 400 gün olmalıdır");
        var (b0, _) = Zaman.YerelGunAraligiUtc(bas);
        var (_, b1) = Zaman.YerelGunAraligiUtc(bit);
        var t = islem.TenantId;
        var simdi = DateTime.UtcNow;
        var tespit = await db.MasakKimlikTespitleri.AsNoTracking().CountAsync(x => x.TenantId == t && x.Tarih >= b0 && x.Tarih < b1, iptal);
        var sibYeni = await db.MasakSupheliBildirimler.AsNoTracking().CountAsync(x => x.TenantId == t && x.TespitTarihi >= b0 && x.TespitTarihi < b1, iptal);
        var sibGonderilen = await db.MasakSupheliBildirimler.AsNoTracking().CountAsync(x => x.TenantId == t && x.GonderimTarihi >= b0 && x.GonderimTarihi < b1, iptal);
        var sibAcik = await db.MasakSupheliBildirimler.AsNoTracking().CountAsync(x => x.TenantId == t && x.Durum == Kuyumcu.Api.Data.SupheliDurum.Acik, iptal);
        var sibGecmis = await db.MasakSupheliBildirimler.AsNoTracking().CountAsync(x => x.TenantId == t && x.Durum == Kuyumcu.Api.Data.SupheliDurum.Acik && x.SonBildirimTarihi < simdi, iptal);
        var eslesme = await db.YaptirimEslesmeleri.AsNoTracking().CountAsync(x => x.TenantId == t && x.Olusturuldu >= b0 && x.Olusturuldu < b1, iptal);
        var listeTarihi = await db.YaptirimKayitlari.AsNoTracking().Where(y => y.Kaldirildi == null)
            .Select(y => (DateOnly?)y.ListeTarihi).MaxAsync(iptal);
        var hassasOkuma = await db.DenetimKayitlari.AsNoTracking()
            .CountAsync(x => x.TenantId == t && x.Hassas && x.Eylem == DenetimEylemi.Okuma && x.Tarih >= b0 && x.Tarih < b1, iptal);
        static CiktiSatir S(string a, string d) => new([a, d]);
        string N(int n) => n.ToString(CultureInfo.InvariantCulture);
        var bolumler = new List<CiktiBolum>
        {
            new("Kimlik tespiti", ["Kalem", "Sayı"], [S("Dönemde yapılan kimlik tespiti", N(tespit))], null, [SutunBicimi.Metin, SutunBicimi.Metin]),
            new("Şüpheli işlem bildirimi", ["Kalem", "Sayı"],
            [
                S("Dönemde tespit edilen", N(sibYeni)), S("Dönemde MASAK'a gönderilen", N(sibGonderilen)),
                S("Bugün açık (gönderilmemiş)", N(sibAcik)), new CiktiSatir(["Süresi geçmiş açık bildirim", N(sibGecmis)], Vurgu: sibGecmis > 0),
            ], null, [SutunBicimi.Metin, SutunBicimi.Metin]),
            new("Yaptırım listesi", ["Kalem", "Değer"],
            [
                S("Son liste tarihi", listeTarihi?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "liste yüklenmemiş"),
                S("Dönemde liste eşleşmesi", N(eslesme)),
            ], null, [SutunBicimi.Metin, SutunBicimi.Metin]),
            new("Hassas kayıt erişimi", ["Kalem", "Sayı"], [S("Dönemde hassas kayıt görüntüleme (denetim izi)", N(hassasOkuma))], null, [SutunBicimi.Metin, SutunBicimi.Metin]),
        };
        return new OrtakBelge(
            Tur: BelgeTurleri.UyumRaporu,
            BelgeAdi: BelgeAdi(BelgeTurleri.UyumRaporu),
            BelgeNo: id,
            Dayanak: $"{bas:dd.MM.yyyy} – {bit:dd.MM.yyyy}",
            DuzenlemeTarihi: simdi,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: null,
            Bolumler: bolumler,
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.UyumRaporu),
            Not: UyumRaporuMetni,
            Uyari: sibGecmis > 0 ? UyumRaporuGecmisUyari : null,
            BelgeNoKayitKimligi: false);
    }

    private const string UyumRaporuMetni = "Bu rapor kişisel veri içermez; şüpheli işlem bildirimlerinin varlığı ve sayısı gizlidir (5549 sayılı Kanun md.4/2).";
    private const string UyumRaporuGecmisUyari = "Süresi geçmiş şüpheli işlem bildirimi var — derhal MASAK'a gönderin.";

    private const string OdemePlaniMetni = "Yukarıdaki taksitleri vadelerinde ödemeyi kabul ederim. Bu plana vade farkı uygulanmamıştır.";

    private async Task<OrtakBelge> MutabakatBelgesi(string cariId, AktifKullanici islem, CancellationToken iptal)
    {
        var c = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariId && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri için mutabakat formu düzenlenmez");
        var bakiye = await db.CariBakiyeler.AsNoTracking().FirstOrDefaultAsync(b => b.CariId == cariId, iptal);
        var emanetEtkisi = bakiye is null ? 0m : await CariBakiyeKurali.EmanetEtkisi(db, bakiye.TenantId, cariId);

        static string Yon(decimal d, int hane) => Math.Round(d, hane) switch
        {
            > 0m => MutabakatBizeBorclu, < 0m => MutabakatBizBorcluyuz, _ => MutabakatBorcYok,
        };
        var satirlar = new List<CiktiSatir>();
        void Ekle(string ad, decimal deger, string birim, int hane, bool zorunlu)
        {
            if (!zorunlu && Math.Round(deger, hane) == 0m) return;
            satirlar.Add(new CiktiSatir([ad, Yon(deger, hane), Sayi(Math.Abs(deger), hane)], ParaBirimi: birim == "HAS" ? null : birim));
        }
        Ekle("Türk lirası", bakiye?.TlBakiye ?? 0m, "TRY", 2, zorunlu: true);
        Ekle("Has altın (gram)", CariBakiyeKurali.EmanetsizHas(bakiye?.HasGramBakiye ?? 0m, emanetEtkisi), "HAS", 4, zorunlu: true);
        Ekle("ABD doları", bakiye?.UsdBakiye ?? 0m, "USD", 2, zorunlu: false);
        Ekle("Euro", bakiye?.EurBakiye ?? 0m, "EUR", 2, zorunlu: false);
        Ekle("İngiliz sterlini", bakiye?.GbpBakiye ?? 0m, "GBP", 2, zorunlu: false);
        // Has satırı HAS biçimiyle, para satırları PARA biçimiyle basılsın diye iki bölüm: biçim sütun başınadır.
        var para = satirlar.Where(x => x.ParaBirimi is not null).ToList();
        var has = satirlar.Where(x => x.ParaBirimi is null).ToList();
        var bolumler = new List<CiktiBolum>
        {
            new("Para bakiyesi", ["Hesap", "Durum", "Tutar"], para, null, [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Para]),
            new("Altın bakiyesi", ["Hesap", "Durum", "Has gram"], has,
                emanetEtkisi != 0m ? "Emanetteki altın bu bakiyeye dâhil değildir." : null,
                [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Has]),
            new("Cevabınız", ["Cevap", "Bakiye"], [
                new CiktiSatir([MutabakatMutabikiz, ""]),
                new CiktiSatir([MutabakatMutabikDegiliz, "………………………………"]),
            ], null, [SutunBicimi.Metin, SutunBicimi.Metin]),
        };

        return new OrtakBelge(
            Tur: BelgeTurleri.CariMutabakat,
            BelgeAdi: BelgeAdi(BelgeTurleri.CariMutabakat),
            BelgeNo: c.Id,
            Dayanak: null,
            DuzenlemeTarihi: DateTime.UtcNow,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: new CiktiTaraf(CariTarafEtiketi(c.Tur), c.Ad, c.VergiNo, c.Adres, c.Telefon),
            Bolumler: bolumler,
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.CariMutabakat),
            Not: MutabakatMetni,
            Uyari: null,
            BelgeNoKayitKimligi: true);
    }

    // ── D1-13 (18.10) — kimlik tespit formu · ürün belgesi (garanti kartı) · ekran imzası ──────────

    public const string GarantiNotu = "Bu belge ürünün satış anındaki özelliklerini gösterir. Garanti koşulları için işletmeye başvurunuz.";

    /// <summary>Kaydın EN SON aktif ekran imzası (PNG base64); yoksa null.</summary>
    private Task<string?> ImzaGorseli(string sahipTuru, string sahipId, string tenantId, CancellationToken iptal) =>
        ImzaGorseli(sahipTuru, [sahipId], tenantId, iptal);

    /// <summary>Sahip kümesinden (ör. fişin bütün satış satırları) en son aktif imza — belge hangi satırdan açılırsa açılsın aynı imza.</summary>
    private async Task<string?> ImzaGorseli(string sahipTuru, IReadOnlyCollection<string> sahipIdler, string tenantId, CancellationToken iptal)
    {
        var icerik = await db.Belgeler.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.SahipTuru == sahipTuru && sahipIdler.Contains(b.SahipId) && b.Tur == BelgeTuru.Imza && b.Aktif)
            .OrderByDescending(b => b.Olusturuldu).Select(b => b.Icerik).FirstOrDefaultAsync(iptal);
        return icerik is null ? null : Convert.ToBase64String(icerik);
    }

    /// <summary>
    /// MASAK kimlik tespit kaydının imzalanacak formu. Tam kimlik basılır (form müşteriye imzalatılır, dosyada saklanır);
    /// okuma izi Belge() içinde düşer (KVKK md.12), rol kapısı MASAK kimlik ucuyla AYNI.
    /// </summary>
    private async Task<OrtakBelge> KimlikTespitBelgesi(string id, AktifKullanici islem, CancellationToken iptal)
    {
        var m = await db.MasakKimlikTespitleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Kimlik tespiti bulunamadı");
        var beyan = await db.MasakBeyanlari.AsNoTracking()
            .FirstOrDefaultAsync(b => b.KimlikTespitiId == m.Id && b.TenantId == islem.TenantId, iptal);
        var tespitEden = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.Id == m.TespitEdenKullaniciId && k.TenantId == islem.TenantId)
            .Select(k => k.Ad).FirstOrDefaultAsync(iptal);
        var satirlar = new List<CiktiSatir>
        {
            new(["Ad soyad", m.AdSoyad]),
            new(["Belge türü", m.BelgeTuru]),
            new(["Belge numarası", m.BelgeNo]),
        };
        if (m.DogumTarihi is { } dt) satirlar.Add(new(["Doğum tarihi", dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)]));
        if (!string.IsNullOrWhiteSpace(m.Uyruk)) satirlar.Add(new(["Uyruk", m.Uyruk]));
        if (!string.IsNullOrWhiteSpace(m.Adres)) satirlar.Add(new(["Adres", m.Adres]));
        var islemSatirlari = new List<CiktiSatir>
        {
            new(["İşlem tutarı", Sayi(m.IslemTutariTl, 2)], ParaBirimi: "TRY"),
            new(["Tespit tarihi", m.Tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)]),
        };
        if (!string.IsNullOrWhiteSpace(tespitEden)) islemSatirlari.Add(new(["Tespit eden", tespitEden]));
        if (beyan is not null)
        {
            if (!string.IsNullOrWhiteSpace(beyan.IslemAmaci)) islemSatirlari.Add(new(["İşlemin amacı", beyan.IslemAmaci]));
            if (!string.IsNullOrWhiteSpace(beyan.FonKaynagi)) islemSatirlari.Add(new(["Fon kaynağı", beyan.FonKaynagi]));
        }
        return new OrtakBelge(
            Tur: BelgeTurleri.KimlikTespit,
            BelgeAdi: BelgeAdi(BelgeTurleri.KimlikTespit),
            BelgeNo: m.Id,
            Dayanak: "MASAK — müşterinin tanınması",
            DuzenlemeTarihi: DateTime.UtcNow,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: null,
            Bolumler:
            [
                new("Kimlik bilgileri", ["Alan", "Değer"], satirlar, null, [SutunBicimi.Metin, SutunBicimi.Metin]),
                new("İşlem", ["Alan", "Değer"], islemSatirlari, null, [SutunBicimi.Metin, SutunBicimi.Metin]),
            ],
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.KimlikTespit),
            Not: null,
            Uyari: null,
            BelgeNoKayitKimligi: true,
            ImzaGorseli: await ImzaGorseli(BelgeSahipTuru.KimlikTespit, m.Id, islem.TenantId, iptal));
    }

    /// <summary>
    /// Ürün belgesi (garanti kartı): satış fişinin ÜRÜNLÜ kalemleri — ad, kod, ayar (milyem), gram, has, taş ibaresi.
    /// Garanti KOŞULU yazılmaz (işletmenin kararıdır, uydurulmaz); not satırı bunu söyler.
    /// </summary>
    private async Task<OrtakBelge> GarantiKartiBelgesi(string hareketId, AktifKullanici islem, CancellationToken iptal)
    {
        var h = await db.Hareketler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hareketId && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Hareket bulunamadı");
        if (h.Tur != "SATIS") throw new BadRequestException("Ürün belgesi yalnız satış için düzenlenir");
        var bacaklar = h.FisId is null ? [h]
            : await db.Hareketler.AsNoTracking()
                .Where(x => x.FisId == h.FisId && x.TenantId == islem.TenantId && x.Tur == "SATIS" && x.UrunId != null)
                .OrderBy(x => x.Tarih).ThenBy(x => x.Id).ToListAsync(iptal);
        var urunIdler = bacaklar.Where(b => b.UrunId != null).Select(b => b.UrunId!).Distinct().ToList();
        var urunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == islem.TenantId && urunIdler.Contains(u.Id))
            .Select(u => new { u.Id, u.Ad, u.Kod, u.AyarId, u.GramAgirlik, u.Birim, u.TasIbaresi })
            .Take(urunIdler.Count + 1).ToDictionaryAsync(u => u.Id, iptal);
        var satirlar = bacaklar.Where(b => b.UrunId != null && urunler.ContainsKey(b.UrunId)).Select(b =>
        {
            var u = urunler[b.UrunId!];
            var adet = Math.Abs(b.Adet ?? 1m);
            var ayar = u.AyarId is null ? null : Kuyumcu.Core.Domain.Ayarlar.Bul(u.AyarId);
            return new CiktiSatir([
                string.IsNullOrWhiteSpace(u.TasIbaresi) ? u.Ad : $"{u.Ad} ({u.TasIbaresi})",
                u.Kod,
                ayar is null ? (u.AyarId ?? "—") : $"{ayar.Ad} · {ayar.Milyem.ToString("0", CultureInfo.InvariantCulture)}",
                u.Birim == "GRAM" ? Sayi(adet, 3) : Sayi(u.GramAgirlik * adet, 3),
                Sayi(Math.Abs(b.HasGrami), 4),
            ]);
        }).ToList();
        if (satirlar.Count == 0) throw new BadRequestException("Bu satışta ürünlü kalem yok");
        return new OrtakBelge(
            Tur: BelgeTurleri.GarantiKarti,
            BelgeAdi: BelgeAdi(BelgeTurleri.GarantiKarti),
            BelgeNo: h.FisId ?? h.Id,
            Dayanak: "Garanti kartı",
            DuzenlemeTarihi: h.Tarih,
            Duzenleyen: await Kimlik(islem.TenantId, h.SubeId, iptal),
            Karsi: await Taraf(h.CariId, islem.TenantId, "Müşteri", iptal),
            Bolumler: [new("Ürünler", ["Ürün", "Kod", "Ayar", "Gram", "Has gram"], satirlar, null,
                [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Gram, SutunBicimi.Has])],
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.GarantiKarti),
            Not: GarantiNotu,
            Uyari: h.StornoKaynakId is not null ? "BU SATIŞ STORNO EDİLMİŞTİR — geçerli bir belge değildir." : null,
            BelgeNoKayitKimligi: true,
            ImzaGorseli: await ImzaGorseli(BelgeSahipTuru.Satis, [h.Id, .. bacaklar.Select(b => b.Id)], islem.TenantId, iptal));
    }

    // ── D1-19 (18.10) — KVKK aydınlatma formu ───────────────────────────────

    public const string KvkkOkudum = "☐ Aydınlatma metnini okudum, bilgilendirildim.";
    public const string KvkkPazarlama = "☐ Kampanya ve bilgilendirme iletileri (SMS / e-posta / telefon) almak için açık rıza veriyorum.";
    public const string KvkkMetinYok = "İşletmenin aydınlatma metni girilmemiş — KVKK ekranından girin";

    /// <summary>
    /// Metin İŞLETMENİNDİR (KvkkMetni'nin geçerli sürümü) — sistem metin yazmaz; yoksa 400. Sürüm numarası kâğıtta
    /// yazar: imzalı form hangi metnin okunduğunu ispatlar.
    /// </summary>
    private async Task<OrtakBelge> KvkkBelgesi(string cariId, AktifKullanici islem, CancellationToken iptal)
    {
        var c = await db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariId && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Cari bulunamadı");
        if (c.SistemCarisi) throw new BadRequestException("Peşin Müşteri için KVKK beyanı alınmaz");
        var m = kvkk is null ? null : await kvkk.GuncelMetin(islem.TenantId);
        if (m is null) throw new BadRequestException(KvkkMetinYok);
        var paragraflar = m.Metin.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => new CiktiSatir([p])).ToList();
        return new OrtakBelge(
            Tur: BelgeTurleri.KvkkAydinlatma,
            BelgeAdi: BelgeAdi(BelgeTurleri.KvkkAydinlatma),
            BelgeNo: c.Id,
            Dayanak: $"Metin sürümü {m.Surum} · {m.Olusturuldu.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}",
            DuzenlemeTarihi: DateTime.UtcNow,
            Duzenleyen: await Kimlik(islem.TenantId, islem.SubeId, iptal),
            Karsi: new CiktiTaraf("İlgili kişi", c.Ad, c.VergiNo, c.Adres, c.Telefon),
            Bolumler:
            [
                new("Aydınlatma metni", [""], paragraflar, null, [SutunBicimi.Metin]),
                new("Beyan", [""], [new CiktiSatir([KvkkOkudum]), new CiktiSatir([KvkkPazarlama])], null, [SutunBicimi.Metin]),
            ],
            Tutarlar: [],
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.KvkkAydinlatma),
            Not: null,
            Uyari: null,
            BelgeNoKayitKimligi: true);
    }

    // ── D1-01 (18.10) — fatura dökümü ───────────────────────────────────────

    /// <summary>
    /// Entegratör bağlanana dek her fatura kâğıdına basılan uyarı. Sistem e-Fatura/e-Arşiv
    /// göndermiyor; kâğıt döküm müşteriye bilgi verir, yasal belge yerine geçmez (uydurma yok).
    /// </summary>
    public const string FaturaYasalUyari =
        "BİLGİ AMAÇLI DÖKÜMDÜR — e-Fatura / e-Arşiv fatura ya da matbaa basımlı fatura yerine geçmez.";

    /// <summary>Entegratöre ulaşmış faturanın dökümü: resmi belgenin türü, numarası ve ETTN'si.</summary>
    public static string FaturaEBelgeUyari(string? senaryo, string? belgeNo, Guid ettn) =>
        $"BİLGİ AMAÇLI DÖKÜMDÜR — resmi belge {(senaryo == EBelge.EBelgeSenaryolari.EFatura ? "e-Fatura" : "e-Arşiv fatura")}"
        + (string.IsNullOrWhiteSpace(belgeNo) ? "" : $" {belgeNo}") + $" · ETTN {ettn}.";

    public const string FaturaIptalUyari = "BU FATURA İPTAL EDİLMİŞTİR — geçerli bir belge değildir.";
    public const string FaturaTaslakUyari = "TASLAKTIR — fatura henüz kesilmedi.";

    private async Task<OrtakBelge> FaturaBelgesi(string id, AktifKullanici islem, CancellationToken iptal)
    {
        var f = await db.Faturalar.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId, iptal)
            ?? throw new NotFoundException("Fatura bulunamadı");
        var kalemler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.FaturaId == id && k.TenantId == islem.TenantId)
            .OrderBy(k => k.Sira).Take(500).ToListAsync(iptal);

        var alis = f.Tur == "ALIS";
        var cari = await db.Cariler.AsNoTracking()
            .Where(c => c.Id == f.CariId && c.TenantId == islem.TenantId)
            .Select(c => new { c.Ad, c.Unvan, c.VergiNo, c.Adres, c.Telefon })
            .FirstOrDefaultAsync(iptal);
        // Kesilmiş faturada alıcı bilgisi faturanın KENDİ kopyasıdır (P0-8); taslakta kart okunur.
        var karsi = new CiktiTaraf(
            alis ? "Satıcı" : "Alıcı",
            f.AliciUnvan ?? (string.IsNullOrWhiteSpace(cari?.Unvan) ? cari?.Ad : cari.Unvan) ?? "—",
            f.AliciVergiNo ?? cari?.VergiNo,
            f.AliciAdres ?? cari?.Adres,
            cari?.Telefon);

        var kuyum = kalemler.Count > 0 ? kalemler.Any(k => k.KdvTuru == "KUYUM") : f.KdvTuru == "KUYUM";
        var satirlar = kalemler.Select(k => new CiktiSatir([
            k.Sira.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(k.TasIbaresi) ? k.Aciklama : $"{k.Aciklama} ({k.TasIbaresi})",
            k.Birim == "GRAM" ? k.Miktar.ToString("N3", TrKultur) + " g" : k.Miktar.ToString("0.###", TrKultur) + " adet",
            KalemGrami(k) is { } g && g > 0m ? Sayi(g, 3) : "",
            k.HasGrami is { } h && h != 0m ? Sayi(Math.Abs(h), 4) : "",
            Sayi(k.BirimFiyat, 2),
            "%" + (k.KdvOran * 100m).ToString("0.##", TrKultur),
            Sayi(k.SatirToplam, 2),
        ], ParaBirimi: f.ParaBirimi)).ToList();

        var bolumler = new List<CiktiBolum>();
        if (satirlar.Count > 0)
            bolumler.Add(new CiktiBolum("Kalemler",
                ["Sıra", "Açıklama", "Miktar", "Gram", "Has gram", "Birim fiyat", "KDV", "Tutar"], satirlar,
                $"{satirlar.Count} kalem",
                SutunBicimleri: [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Gram, SutunBicimi.Has,
                                 SutunBicimi.Para, SutunBicimi.Metin, SutunBicimi.Para]));

        // Toplamlar: kalemli faturada kalemlerin toplamı (fatura ekranındaki "fd-*" satırlarıyla AYNI kaynak);
        // kalemsiz (BASLIK) eski faturada başlık alanları. Yeni formül yok.
        var hasBedeli = kalemler.Count > 0 ? kalemler.Where(k => k.KdvTuru == "KUYUM").Sum(k => k.HasDeger) : f.HasDeger;
        var matrah = kalemler.Count > 0 ? kalemler.Sum(k => k.KdvMatrah) : f.AraToplam - (kuyum ? f.HasDeger : 0m);
        var kdv = kalemler.Count > 0 ? kalemler.Sum(k => k.KdvTutar) : f.KdvTutar;
        var toplam = kalemler.Count > 0 ? kalemler.Sum(k => k.SatirToplam) : f.Toplam;
        var tutarlar = new List<CiktiTutar>();
        if (kuyum && hasBedeli != 0m) tutarlar.Add(new("Has altın bedeli (KDV dışı)", hasBedeli, f.ParaBirimi));
        tutarlar.Add(new("KDV matrahı", matrah, f.ParaBirimi));
        tutarlar.Add(new("KDV", kdv, f.ParaBirimi));
        tutarlar.Add(new("Genel toplam", toplam, f.ParaBirimi, Toplam: true));

        // 20.10: e-belge entegratöre ulaştıysa döküm resmi belgeyi gösterir (numara + ETTN); kâğıt yine bilgi amaçlıdır.
        var eb = await db.EBelgeGonderimleri.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId && g.FaturaId == f.Id
                     && (g.Durum == EBelge.EBelgeDurumlari.Gonderildi || g.Durum == EBelge.EBelgeDurumlari.Kabul))
            .Select(g => new { g.Senaryo, g.BelgeNo, g.Ettn }).FirstOrDefaultAsync(iptal);
        var yasal = eb is null ? FaturaYasalUyari : FaturaEBelgeUyari(eb.Senaryo, eb.BelgeNo, eb.Ettn);
        var uyari = f.Durum switch
        {
            "IPTAL" => $"{FaturaIptalUyari} {yasal}",
            "TASLAK" => $"{FaturaTaslakUyari} {yasal}",
            _ => yasal,
        };
        var dayanak = kuyum && !string.IsNullOrWhiteSpace(f.OzelMatrahKodu)
            ? $"KDV özel matrah (KDVK md.23) · kod {f.OzelMatrahKodu}"
            : null;

        return new OrtakBelge(
            Tur: BelgeTurleri.Fatura,
            BelgeAdi: alis ? "ALIŞ FATURASI" : "FATURA",
            BelgeNo: f.FaturaNo ?? f.Id,
            Dayanak: dayanak,
            DuzenlemeTarihi: f.Tarih,
            Duzenleyen: await Kimlik(islem.TenantId, null, iptal),
            Karsi: karsi,
            Bolumler: bolumler,
            Tutarlar: tutarlar,
            ImzaBasliklari: ImzaBasliklari(BelgeTurleri.Fatura),
            Not: string.IsNullOrWhiteSpace(f.Aciklama) ? null : f.Aciklama.Trim(),
            Uyari: uyari,
            BelgeNoKayitKimligi: f.FaturaNo is null);
    }

    /// <summary>Kalemin tartı gramı: gram birimli kalemde miktarın kendisi, adetlide kart gramı × adet (M5).</summary>
    private static decimal? KalemGrami(Data.FaturaKalemi k) =>
        k.Birim == "GRAM" ? k.Miktar : k.GramAgirlik is { } g ? g * k.Miktar : null;

    /// <summary>Miktar hücresi: tr-TR, ADET tam sayı · GRAM 3 hane (kâğıt; ekstre has hücresiyle aynı yol).</summary>
    internal static string IrsaliyeMiktar(decimal adet, string? birim) =>
        birim == "GRAM"
            ? adet.ToString("N3", TrKultur) + " gram"
            : (adet == decimal.Truncate(adet) ? adet.ToString("N0", TrKultur) : adet.ToString("N3", TrKultur)) + " adet";

    /// <summary>
    /// PARA-DEMO (madde 5) — ekstre satırının adı: tür · ödeme. Has ile yapılan tahsilat
    /// (TL 0) "Altın" yazar; ödeme türü olmayan satır yalnız türünü yazar. Ham kod basılmaz.
    /// </summary>
    internal static string EkstreIslemAdi(Hareket h, IReadOnlyList<string>? karmaAraclari = null)
    {
        var tur = Sozluk.HareketTuru(h.Tur);
        // PARA-DEMO-2 (madde 1): gerçek karmada "Karma ödeme" değil bacak türleri ("Kart + Havale").
        var odeme = h.OdemeTuru == "KARMA" && karmaAraclari is { Count: > 0 }
            ? string.Join(" + ", karmaAraclari.Select(a => EkstreOdemeAdi("SATIS", 1m, 0m, a) ?? "?"))
            : EkstreOdemeAdi(h.Tur, h.TlTutari, h.HasGrami, h.OdemeTuru);
        return odeme is null ? tur : $"{tur} · {odeme}";
    }

    private static readonly CultureInfo TrKultur = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>PARA-DEMO-2 (madde 9) — kâğıt ekstrede has hücresi: 4 hane tr-TR, eksi U+2212.</summary>
    internal static string EkstreHas(decimal n)
    {
        var y = decimal.Round(n, 4, MidpointRounding.AwayFromZero);
        var govde = Math.Abs(y).ToString("N4", TrKultur);
        return y < 0 ? $"−{govde} has" : $"{govde} has";
    }

    /// <summary>PARA-DEMO (madde 5) — ödeme türünün kâğıttaki adı; bilinmeyen kod uydurulmaz (null).</summary>
    internal static string? EkstreOdemeAdi(string tur, decimal tl, decimal has, string? odemeTuru)
    {
        if (tur == "TAHSILAT" && tl == 0m && has != 0m) return "Altın";   // ekranla aynı ad (UX-S31)
        return odemeTuru switch
        {
            "NAKIT" => "Nakit",
            "KART" => "Kart",
            "HAVALE" => "Havale",
            "VERESIYE" => "Veresiye",
            "KARMA" => "Karma ödeme",
            "ALTIN" => "Eski altın",
            "DOVIZ" => "Döviz",
            "DIGER" => "Diğer",
            _ => null,
        };
    }

    // ── ortak ───────────────────────────────────────────────────────────────

    /// <summary>M5: ürünün birim gramı (gram_agirlik) — fiş kaleminde gram × adet.</summary>
    private async Task<Dictionary<string, decimal>> UrunGramlari(
        IEnumerable<string?> idler, string tenantId, CancellationToken iptal)
    {
        var liste = idler.Where(x => x is not null).Select(x => x!).Distinct().ToList();
        if (liste.Count == 0) return [];
        return await db.Urunler.AsNoTracking()
            .Where(u => liste.Contains(u.Id) && u.TenantId == tenantId)
            .Take(liste.Count)
            .ToDictionaryAsync(u => u.Id, u => u.GramAgirlik, iptal);
    }

    private async Task<Dictionary<string, string>> UrunAdlari(
        IEnumerable<string?> idler, string tenantId, CancellationToken iptal)
    {
        var liste = idler.Where(x => x is not null).Select(x => x!).Distinct().ToList();
        if (liste.Count == 0) return [];
        return await db.Urunler.AsNoTracking()
            .Where(u => liste.Contains(u.Id) && u.TenantId == tenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Ad, iptal);
    }

    /// <summary>TOPLU-2 (KESIF-P8 açık kalanı · karar 1): belge başlığı resmî ad, karşı taraf
    /// etiketi ham "Cari" değil hesabın türü (tezgâhtarın tanıdığı ad).</summary>
    public static string CariTarafEtiketi(string? tur) => tur switch
    {
        "MUSTERI" => "Müşteri",
        "TEDARIKCI" => "Tedarikçi",
        _ => "Hesap sahibi",
    };

    public static string BelgeAdi(string tur) => tur switch
    {
        BelgeTurleri.SatisFisi => "SATIŞ FİŞİ",
        BelgeTurleri.TahsilatMakbuzu => "TAHSİLAT MAKBUZU",
        BelgeTurleri.EmanetTeslim => "EMANET TESLİM BELGESİ",
        BelgeTurleri.CariEkstre => "CARİ HESAP EKSTRESİ", // TOPLU-2: KESIF karar 1 — resmî belge adı
        BelgeTurleri.Irsaliye => "İRSALİYE",   // yön belgede ayrıntılanır (SEVK / MAL KABUL)
        BelgeTurleri.Fatura => "FATURA",       // D1-01; alış faturası belgede "ALIŞ FATURASI"
        BelgeTurleri.CariMutabakat => "CARİ HESAP MUTABAKAT FORMU",
        BelgeTurleri.KvkkAydinlatma => "KVKK AYDINLATMA FORMU",   // tek satır (uzun ad kayıt koduna taşıyordu — ölçüldü)
        BelgeTurleri.KimlikTespit => "KİMLİK TESPİT FORMU",
        BelgeTurleri.GarantiKarti => "ÜRÜN BELGESİ",
        BelgeTurleri.OdemePlani => "ÖDEME PLANI",
        BelgeTurleri.UyumRaporu => "UYUM RAPORU",
        BelgeTurleri.Teklif => "FİYAT TEKLİFİ",   // Y-11
        _ => throw new BadRequestException($"Bilinmeyen belge türü: {tur}"),
    };

    private static string[] ImzaBasliklari(string tur) => tur switch
    {
        BelgeTurleri.SatisFisi => ["Satıcı", "Alıcı"],
        BelgeTurleri.TahsilatMakbuzu => ["Tahsil eden", "Ödeyen"],
        BelgeTurleri.EmanetTeslim => ["Teslim eden", "Teslim alan"],
        BelgeTurleri.CariEkstre => ["Düzenleyen"],
        BelgeTurleri.Irsaliye => ["Teslim eden", "Teslim alan"],
        BelgeTurleri.Teklif => ["Teklifi veren", "Müşteri"],   // Y-11
        BelgeTurleri.Fatura => ["Düzenleyen", "Teslim alan"],
        BelgeTurleri.CariMutabakat => ["Düzenleyen (kaşe / imza)", "Müşteri (kaşe / imza)"],
        BelgeTurleri.KvkkAydinlatma => ["Müşteri (ad soyad / imza)"],
        BelgeTurleri.KimlikTespit => ["Müşteri (imza)", "Tespit eden"],
        BelgeTurleri.GarantiKarti => ["Müşteri (imza)", "Satıcı (kaşe / imza)"],
        BelgeTurleri.OdemePlani => ["Müşteri (ad soyad / imza)", "Satıcı (kaşe / imza)"],
        BelgeTurleri.UyumRaporu => ["Düzenleyen (kaşe / imza)"],
        _ => [],
    };

    /// <summary>
    /// PAKET Q9-4 — müşteri belgesinin toplam satırı İŞARETSİZDİR. Bacakların hepsi
    /// aynı yöndeyse etiket "Toplam …"; yönler KARIŞIKSA (ör. satış + bozdurma alışı
    /// tek fişte) net tutarın yönü ETİKETE yazılır ("Net tutar — müşteriye ödenen").
    /// Eksi işareti hiçbir durumda kâğıda çıkmaz; yön bilgisi de kaybolmaz.
    /// </summary>
    public static CiktiTutar ToplamSatiri(
        string toplamEtiketi, string netEtiketi, IReadOnlyList<decimal> degerler, string paraBirimi,
        string pozitifYon, string negatifYon, bool toplam)
    {
        var net = degerler.Sum();
        var karisik = degerler.Any(d => d > 0m) && degerler.Any(d => d < 0m);
        var etiket = karisik
            ? $"{netEtiketi} — {(net < 0m ? negatifYon : pozitifYon)}"
            : toplamEtiketi;
        return new CiktiTutar(etiket, Math.Abs(net), paraBirimi, Toplam: toplam);
    }

    /// <summary>Sayılar InvariantCulture ile dizilir; biçim istemcinin kültür işidir.</summary>
    private static string Sayi(decimal d, int basamak) =>
        d.ToString("F" + basamak.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>
    /// UX-S31: ham veritabanı kodu ("EMANET_GIRIS") kâğıda BASILMAZ.
    /// Geri dal ham kodu döndürmez, okunur bir metne çevirir.
    /// </summary>
    private static class Sozluk
    {
        private const string DovizOnek = "DOVIZ_";

        public static string HareketTuru(string tur) => tur switch
        {
            "SATIS" => "Satış",
            "SATIS_IADE" => "Satış iadesi",
            "ALIS" => "Alış",
            "ALIS_IADE" => "Alış iadesi",
            "TAHSILAT" => "Tahsilat",
            "VERESIYE" => "Veresiye",
            "ISCILIK" => "İşçilik",
            "EMANET_GIRIS" => "Emanet girişi",
            "EMANET_CIKIS" => "Emanet çıkışı",
            // ⚠ Döviz türleri TEK KAYNAKTAN türetilir: taban türün etiketi
            //   alınıp önüne "Döviz" konur. Tür dizgesini burada elle yazmak
            //   ikinci bir döviz tür listesi doğururdu (denetim.sh
            //   "DOVIZ_ turu yazan dosya (sabit 4)" sayacı bunu yasaklar).
            _ when tur.StartsWith(DovizOnek, StringComparison.Ordinal) =>
                "Döviz " + HareketTuru(tur[DovizOnek.Length..]).ToLowerInvariant(),
            _ => tur.Replace('_', ' '),
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Ortak A4 çıktı ucu.
/// <para>
/// ⚠ Sınıf düzeyinde [ModulYetkisi] YOKTUR ve bu bilinçlidir: kapı BELGE
/// TÜRÜNE göre değişir (fiş=islem, makbuz/ekstre=cari, emanet=emanet) ve
/// serviste tür başına sorulur. Tek bir sınıf niteliği ya çok geniş
/// (fail-open) ya çok dar (kullanılamaz) olurdu.
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/cikti")]
public sealed class CiktiController(CiktiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// GET /api/v1/cikti/{tur}/{id} — tek A4 şablonunun verisi.
    /// <para>
    /// ⚠ [RolGerekli] BURADA GENİŞTİR ve bu kasıtlıdır: dört rol de belge
    /// basabilmelidir (tezgâhtar müşteriye fiş verir). Asıl daraltma
    /// CiktiServisi'ndeki TÜR BAŞINA modül kapısıdır. Nitelik yine de
    /// yazılıdır çünkü "özniteliksiz uç" fail-open riskidir (P-09 sayacı):
    /// bir gün servis kapısı sökülürse rol kapısı hâlâ ayakta kalır.
    /// </para>
    /// </summary>
    [HttpGet("{tur}/{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<OrtakBelge> Belge(string tur, string id, CancellationToken iptal) =>
        servis.Belge(tur, id, Islem, iptal);
}
