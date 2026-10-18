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
    DenetimServisi denetim)
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
        if (!await yetki.ErisebilirMi(islem.TenantId, islem.Rol, modul))
            throw new ForbiddenException($"Bu belgeyi görüntüleme yetkiniz yok ({modul})");

        var belge = tur switch
        {
            BelgeTurleri.CariEkstre => await Ekstre(id, islem, iptal),
            BelgeTurleri.Irsaliye => await IrsaliyeBelgesi(id, islem, iptal),
            BelgeTurleri.Fatura => await FaturaBelgesi(id, islem, iptal),
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
        return new CiktiKimlik(t?.Ad, sube, t?.Adres, t?.VergiDairesi, t?.VergiNo, t?.Telefon);
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
        var not = string.Join(" · ", new[] { adresNotu, i.Aciklama, faturaNotu }.Where(x => !string.IsNullOrWhiteSpace(x)));
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

    // ── D1-01 (18.10) — fatura dökümü ───────────────────────────────────────

    /// <summary>
    /// Entegratör bağlanana dek her fatura kâğıdına basılan uyarı. Sistem e-Fatura/e-Arşiv
    /// göndermiyor; kâğıt döküm müşteriye bilgi verir, yasal belge yerine geçmez (uydurma yok).
    /// </summary>
    public const string FaturaYasalUyari =
        "BİLGİ AMAÇLI DÖKÜMDÜR — e-Fatura / e-Arşiv fatura ya da matbaa basımlı fatura yerine geçmez.";

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

        var uyari = f.Durum switch
        {
            "IPTAL" => $"{FaturaIptalUyari} {FaturaYasalUyari}",
            "TASLAK" => $"{FaturaTaslakUyari} {FaturaYasalUyari}",
            _ => FaturaYasalUyari,
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
        _ => throw new BadRequestException($"Bilinmeyen belge türü: {tur}"),
    };

    private static string[] ImzaBasliklari(string tur) => tur switch
    {
        BelgeTurleri.SatisFisi => ["Satıcı", "Alıcı"],
        BelgeTurleri.TahsilatMakbuzu => ["Tahsil eden", "Ödeyen"],
        BelgeTurleri.EmanetTeslim => ["Teslim eden", "Teslim alan"],
        BelgeTurleri.CariEkstre => ["Düzenleyen"],
        BelgeTurleri.Irsaliye => ["Teslim eden", "Teslim alan"],
        BelgeTurleri.Fatura => ["Düzenleyen", "Teslim alan"],
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
