using System.Globalization;
using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Stopaj;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 / Y4-B — STOPAJ BORCU (çelişki 3b).
//
// MEVZUAT (birincil, şartname § 3.1):
//   • GVK md.94/13 — esnaf muaflığındakilere mal alımı ödemelerinden tevkifat.
//     https://www.mevzuat.gov.tr/mevzuatmetin/1.4.193.pdf
//   • 2009/14592 BKK 13. bent — hurda mal alımı %2 (ilk metin; sonraki değişiklik
//     DOĞRULANAMADI — T-2). https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm
//   • GVK md.98 / md.119 + GİB süreler belgesi — izleyen ayın 26'sı (TEYİTSİZ, T-3).
//
// TÜRETME: dönem borcu = Σ gider_pusulalari.stopaj_tutar (iptal edilmemiş, teslim
// YEREL ayı = dönem). İptal edilip yeniden düzenlenen pusulada stopaj TEK sayılır
// (iptal edilen dışarıda, yenisi içeride — ikisi de aynı tutarı taşır).
// ÖDEME: stopaj_odemeleri + kasa çıkışı MEVCUT gider yolundan (GiderServisi.Kaydet);
// ikinci kasa yazma yolu açılmaz. Düzeltme = kasa satırının stornosu (48-A motoru).
// Oran bu pakette DEĞİŞMEZ; belge üzerindeki stopaj_oran otoritedir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class StopajOdemeDto
{
    public string? Donem { get; set; }
    public string? KasaId { get; set; }
    /// <summary>Boşsa dönemin KALANI ödenir (sistem bildiği değeri sormaz — UX-S21).</summary>
    /// <remarks>PAKET GOC-A (envanter ortak kök 3): K9 — en az 0,01, en çok 2 ondalık (0,004 eskiden
    /// "> 0" kontrolünden geçip 0,00 TL'lik ödeme satırı oluyordu).</remarks>
    [Tutar]
    public decimal? Tutar { get; set; }
    public string? TahakkukFisNo { get; set; }
}

public sealed class StopajServisi(
    KuyumcuDbContext db, DenetimServisi denetim, Gider.GiderServisi gider,
    Satis.StornoServisi storno, YasalParametreServisi? yasal = null)
{
    /// <summary>Gider kategorisinin adı — kasa raporunda bu adla görünür.</summary>
    public const string KategoriAdi = "Vergi — stopaj";

    /// <summary>Panel/hatırlatma: son güne bu kadar gün (ya da daha az) kala uyarılır.</summary>
    public const int UyariGunu = 5;

    private const string Varlik = nameof(StopajOdemesi);

    /// <summary>
    /// Sorgu tavanı (B-8). UX-S29: SESSİZ KIRPMA YOK — tavan aşılırsa toplam yanlış
    /// çıkacağından yanıt verilmez (fail-closed), sebebi yazılır.
    /// </summary>
    public const int SorguSiniri = 20000;

    private static void SinirDenetle(int adet)
    {
        if (adet > SorguSiniri)
            throw new ConflictException(
                $"Dönemde {SorguSiniri} kaydı aşan gider pusulası/ödeme var; toplam güvenle hesaplanamıyor. Destek ekibine bildirin.");
    }

    // ── Saf yardımcılar ─────────────────────────────────────────────────────

    /// <summary>Teslim anının YEREL ayı — "YYYY-MM".</summary>
    public static string Donem(DateTime utc) =>
        Zaman.UtcdenYerele(utc).ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static bool DonemGecerli(string? donem, out int yil, out int ay)
    {
        yil = ay = 0;
        return donem is { Length: 7 } && donem[4] == '-'
            && int.TryParse(donem[..4], NumberStyles.None, CultureInfo.InvariantCulture, out yil)
            && int.TryParse(donem[5..], NumberStyles.None, CultureInfo.InvariantCulture, out ay)
            && yil is >= 2000 and <= 2100 && ay is >= 1 and <= 12;
    }

    /// <summary>
    /// Son ödeme günü = dönemi İZLEYEN ayın STOPAJ_BEYAN_GUNU'ncü günü (ay kısa ise
    /// ayın son günü). Değer YASAL-1 tablosundan, izleyen ayın ilk gününün satırıyla;
    /// o gün için satır yoksa bugünkü satır. Hiç yoksa null — gün UYDURULMAZ.
    /// </summary>
    public (DateOnly? Gun, string? Teyit) SonGun(int yil, int ay, DateOnly bugun)
    {
        var ilk = new DateOnly(yil, ay, 1).AddMonths(1);
        var satir = yasal?.SatirBul(YasalAnahtar.StopajBeyanGunu, ilk)
                    ?? yasal?.SatirBul(YasalAnahtar.StopajBeyanGunu, bugun);
        if (satir?.DegerSayi is not { } d || d < 1) return (null, satir?.Teyit);
        var gun = Math.Min((int)d, DateTime.DaysInMonth(ilk.Year, ilk.Month));
        return (new DateOnly(ilk.Year, ilk.Month, gun), satir.Teyit);
    }

    // ── Türetme (TEK yer: dönemler, Panel özeti, hatırlatma işi, CSV) ────────

    private async Task<List<StopajDonemi>> Hesapla(string tenantId, DateTime basUtc, DateTime bitUtc, DateOnly bugun)
    {
        var pusulalar = await db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == tenantId && !g.IptalEdildi && g.StopajTutar > 0m
                     && g.TeslimTarihi >= basUtc && g.TeslimTarihi < bitUtc)
            .Select(g => new { g.TeslimTarihi, g.StopajTutar })
            .Take(SorguSiniri + 1)
            .ToListAsync();
        SinirDenetle(pusulalar.Count);

        var donemler = pusulalar.Select(p => Donem(p.TeslimTarihi)).Distinct().ToList();
        var basDonem = Donem(basUtc);
        var bitDonem = Donem(bitUtc.AddTicks(-1));
        var odemeler = await db.StopajOdemeleri.AsNoTracking()
            .Where(o => o.TenantId == tenantId
                     && string.Compare(o.Donem, basDonem) >= 0 && string.Compare(o.Donem, bitDonem) <= 0)
            .OrderBy(o => o.OdemeTarihi)
            .Take(SorguSiniri + 1)
            .ToListAsync();
        SinirDenetle(odemeler.Count);
        var kasaIdleri = odemeler.Select(o => o.HareketId).ToList();
        var stornolu = kasaIdleri.Count == 0 ? [] : (await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.StornoKaynakId != null && kasaIdleri.Contains(h.StornoKaynakId))
            .Select(h => h.StornoKaynakId!)
            .Take(kasaIdleri.Count)
            .ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var kasaAdlari = odemeler.Count == 0 ? new Dictionary<string, string>() : await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .Select(k => new { k.Id, k.Ad })
            .Take(SorguSiniri)
            .ToDictionaryAsync(k => k.Id, k => k.Ad);

        return donemler.Union(odemeler.Select(o => o.Donem)).Distinct()
            .OrderByDescending(d => d, StringComparer.Ordinal)
            .Select(d =>
            {
                DonemGecerli(d, out var yil, out var ay);
                var kesilen = Math.Round(pusulalar.Where(p => Donem(p.TeslimTarihi) == d).Sum(p => p.StopajTutar), 2);
                var satirlar = odemeler.Where(o => o.Donem == d)
                    .Select(o => new StopajOdemeSatiri(o.Id, o.Donem, o.Tutar, o.OdemeTarihi, o.KasaId,
                        kasaAdlari.GetValueOrDefault(o.KasaId), o.HareketId, o.TahakkukFisNo,
                        stornolu.Contains(o.HareketId)))
                    .ToList();
                var odenen = Math.Round(satirlar.Where(o => !o.GeriAlindi).Sum(o => o.TutarTl), 2);
                // Kırpılmaz (hasım bulgusu 01.10): fazla ödeme (ör. ödeme sonrası pusula
                // iptali) EKSİ kalan olarak görünür; Math.Max(0, …) onu gizliyordu.
                var kalan = Math.Round(kesilen - odenen, 2);
                var (sonGun, teyit) = SonGun(yil, ay, bugun);
                return new StopajDonemi(d, kesilen, odenen, kalan, sonGun, teyit,
                    kalan > 0m && sonGun is { } sg && bugun > sg,
                    pusulalar.Count(p => Donem(p.TeslimTarihi) == d), satirlar);
            })
            .ToList();
    }

    public async Task<StopajDonemleriYaniti> Donemler(int yil, AktifKullanici islem)
    {
        if (yil is < 2000 or > 2100) throw new BadRequestException("Geçersiz yıl.");
        var bas = Zaman.YerelAyAraligiUtc(yil, 1).Bas;
        var bit = Zaman.YerelAyAraligiUtc(yil, 12).Bit;
        return new StopajDonemleriYaniti(yil, await Hesapla(islem.TenantId, bas, bit, Zaman.YerelBugun()));
    }

    /// <summary>
    /// Panel ve STOPAJ_HATIRLATMA işi: bu ay kesilen + kalanı olan dönemler (son 13 ay).
    /// Kasa bakiyesi DEĞİŞMEZ — para kasadadır ama vergi dairesine aittir.
    /// </summary>
    public async Task<StopajOzeti> Ozet(string tenantId, DateTime anUtc)
    {
        var bugun = Zaman.YerelBugun(anUtc);
        var buAy = new DateOnly(bugun.Year, bugun.Month, 1);
        var bas = Zaman.YerelAyAraligiUtc(buAy.AddMonths(-12).Year, buAy.AddMonths(-12).Month).Bas;
        var bit = Zaman.YerelAyAraligiUtc(bugun.Year, bugun.Month).Bit;
        var donemler = await Hesapla(tenantId, bas, bit, bugun);
        var buAyKodu = buAy.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var kalanli = donemler.Where(d => d.KalanTl > 0m && d.SonGun is not null).ToList();
        return new StopajOzeti(
            buAyKodu,
            donemler.FirstOrDefault(d => d.Donem == buAyKodu)?.KesilenTl ?? 0m,
            kalanli.Where(d => d.SonGunGecti).OrderBy(d => d.SonGun).ToList(),
            kalanli.Where(d => !d.SonGunGecti && d.SonGun!.Value.DayNumber - bugun.DayNumber <= UyariGunu)
                .OrderBy(d => d.SonGun).ToList(),
            UyariGunu);
    }

    // ── Ödeme ───────────────────────────────────────────────────────────────

    public async Task<StopajOdemeSonucu> OdemeKaydet(StopajOdemeDto? dto, AktifKullanici islem)
    {
        // DataAnnotations yalnız controller yolunda koşar → elle kontrol (servis içi).
        if (dto is null) throw new BadRequestException("İstek gövdesi zorunludur.");
        var alanlar = new Dictionary<string, string>();
        if (!DonemGecerli(dto.Donem, out var yil, out var ay)) alanlar["donem"] = "Dönem YYYY-AA biçiminde olmalıdır.";
        if (string.IsNullOrWhiteSpace(dto.KasaId)) alanlar["kasaId"] = "Ödemenin çıkacağı kasa seçilmelidir.";
        // PAKET GOC-A: K9 YUVARLAMADAN ÖNCE (0,004 → 400; eskiden 0,00 TL'lik satır). Metin sözlükten (ekranla aynı).
        if (dto.Tutar is { } odenen && Kuyumcu.Core.Domain.Girdi.TutarKurali.Denetle(odenen) is { } tutarHatasi)
            alanlar["tutar"] = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(tutarHatasi, Kuyumcu.Core.I18n.IstekDili.Gecerli);
        if (dto.TahakkukFisNo is { Length: > 50 }) alanlar["tahakkukFisNo"] = "Tahakkuk fişi numarası en çok 50 karakter olabilir.";
        if (alanlar.Count > 0)
            throw new BadRequestException($"Stopaj ödemesi: {string.Join(" · ", alanlar.Values)}")
                .AlanIle(alanlar.Keys.First());

        var donem = dto.Donem!;
        // ⚠ EŞZAMANLILIK (hasım bulgusu 01.10, KRİTİK): SIRA kalan hesabından ÖNCE okunur.
        //   Bu okumadan sonra işlenen her ödeme en az (enSonSira + 1) numarasını almıştır;
        //   bizim yazacağımız numara onunla çakışır → UNIQUE (tenant, donem, sira_no) 1062
        //   → 409. Yani kalan okumamızın KAÇIRDIĞI bir ödeme varsa yazma reddedilir;
        //   çift tık / iki sekme / ağ yeniden denemesi kesilenin üstüne ödeme YAZAMAZ.
        //   Sıra = dönemdeki ödeme SAYISI (+1): numaralar 1..n kesintisiz doğar (her yazma
        //   n+1'i dener, çakışan reddedilir), yani sayı = en büyük sıra.
        var enSonSira = await db.StopajOdemeleri.AsNoTracking()
            .CountAsync(o => o.TenantId == islem.TenantId && o.Donem == donem);
        var bas = Zaman.YerelAyAraligiUtc(yil, ay).Bas;
        var bit = Zaman.YerelAyAraligiUtc(yil, ay).Bit;
        var durum = (await Hesapla(islem.TenantId, bas, bit, Zaman.YerelBugun())).FirstOrDefault(d => d.Donem == donem);
        var kalan = durum?.KalanTl ?? 0m;
        if (kalan <= 0m)
            throw new ConflictException($"{donem} döneminde ödenecek stopaj yok (kalan 0,00 TL).");
        var tutar = Math.Round(dto.Tutar ?? kalan, 2, MidpointRounding.AwayFromZero);
        if (tutar > kalan)
            throw new BadRequestException(
                $"Tutar dönemin kalanından büyük: en çok {kalan.ToString("N2", Kultur)} TL ödenebilir.").AlanIle("tutar");

        var kasa = await db.Kasalar.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == dto.KasaId && k.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kasa bulunamadı");
        if (kasa.ParaBirimi != "TRY")
            throw new BadRequestException("Stopaj vergi dairesine TL ödenir; TL kasası seçin.").AlanIle("kasaId");

        var kategori = await gider.SistemKategorisi(KategoriAdi, islem);
        var fisNo = string.IsNullOrWhiteSpace(dto.TahakkukFisNo) ? null : dto.TahakkukFisNo.Trim();
        StopajOdemesi? odeme = null;
        var now = DateTime.UtcNow;
        Gider.GiderKaydiCiktisi sonuc;
        try
        {
        sonuc = await gider.Kaydet(new Gider.GiderKaydetDto
        {
            KasaId = kasa.Id,
            GiderKategoriId = kategori.Id,
            Tutar = tutar,
            Aciklama = $"Stopaj ödemesi · dönem {donem}" + (fisNo is null ? "" : $" · tahakkuk {fisNo}"),
        }, islem, satir =>
        {
            // AYNI SaveChanges — kasa çıkışı ve ödeme belgesi birlikte doğar.
            odeme = new StopajOdemesi
            {
                Id = Kimlik.Ulid(), TenantId = islem.TenantId, Donem = donem, Tutar = tutar,
                OdemeTarihi = now, HareketId = satir.Id, KasaId = kasa.Id, TahakkukFisNo = fisNo,
                KullaniciId = islem.Id, Olusturuldu = now, SiraNo = enSonSira + 1,
            };
            db.StopajOdemeleri.Add(odeme);
            denetim.YazmaKaydet(islem, Varlik, odeme.Id,
                $"stopaj ödemesi · dönem {donem} · {tutar.ToString("0.00", CultureInfo.InvariantCulture)} TL",
                hassas: false, kasaId: kasa.Id);
        });
        }
        catch (DbUpdateException ex) when (SiraCakismasi(ex))
        {
            // Aynı anda başka bir ödeme işlendi: işlem bütünüyle geri alındı (kasa
            // satırı da yazılmadı). Kullanıcı ekranı yenileyip kalanı görür.
            throw new ConflictException(
                $"{donem} dönemi için aynı anda başka bir stopaj ödemesi kaydedildi; bu ödeme YAZILMADI. "
                + "Ekranı yenileyip kalan tutarı kontrol edin.");
        }

        return new StopajOdemeSonucu(odeme!.Id, donem, tutar, sonuc.KasaId, sonuc.KasaAdi,
            sonuc.KasaBakiyesi, Math.Max(0m, kalan - tutar));
    }

    /// <summary>
    /// Ödemeyi GERİ AL — kasa satırının STORNOSU (48-A motoru). Ödeme satırı
    /// silinmez; "geri alındı" olarak görünür ve toplamlara girmez.
    /// </summary>
    public async Task<object> GeriAl(string id, string? gerekce, AktifKullanici islem)
    {
        var o = await db.StopajOdemeleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Stopaj ödemesi bulunamadı");
        var sonuc = await storno.KasaHareketiStorno(o.KasaId, o.HareketId, gerekce, islem);
        denetim.YazmaKaydet(islem, Varlik, o.Id, $"stopaj ödemesi GERİ ALINDI (kasa stornosu) · dönem {o.Donem}",
            hassas: false, kasaId: o.KasaId);
        await db.SaveChangesAsync();
        return new { o.Id, o.Donem, geriAlindi = true, storno = sonuc };
    }

    // ── Mali müşavir listesi (CSV) ──────────────────────────────────────────

    public async Task<(byte[] Veri, string Ad)> ListeCsv(string donem, AktifKullanici islem)
    {
        if (!DonemGecerli(donem, out var yil, out var ay)) throw new BadRequestException("Dönem YYYY-AA biçiminde olmalıdır.");
        var (bas, bit) = Zaman.YerelAyAraligiUtc(yil, ay);
        var liste = await db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId && !g.IptalEdildi && g.StopajTutar > 0m
                     && g.TeslimTarihi >= bas && g.TeslimTarihi < bit)
            .OrderBy(g => g.TeslimTarihi).ThenBy(g => g.PusulaNo)
            .Take(SorguSiniri + 1)
            .ToListAsync();
        SinirDenetle(liste.Count);

        // TAM TCKN taşır → okuma izi ZORUNLU (KVKK md.12), satır sayısıyla.
        await denetim.OkumaKaydet(islem, nameof(GiderPusulasi), null,
            $"stopaj listesi indirildi (mali müşavir) · dönem {donem} · {liste.Count} satır · tam TCKN");

        var sb = new StringBuilder();
        sb.Append("pusula_no;tarih;satici_ad_soyad;tckn;brut_tl;stopaj_oran;stopaj_tl\r\n");
        foreach (var g in liste)
        {
            sb.Append(Hucre(g.PusulaNo)).Append(';')
              .Append(Zaman.UtcdenYerele(g.TeslimTarihi).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
              .Append(Hucre(g.SaticiAdSoyad)).Append(';')
              .Append(Hucre(g.SaticiTckn)).Append(';')
              .Append(g.BrutTutar.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
              .Append(g.StopajOran.ToString("0.0000", CultureInfo.InvariantCulture)).Append(';')
              .Append(g.StopajTutar.ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n");
        }
        var bom = Encoding.UTF8.GetPreamble();
        var govde = Encoding.UTF8.GetBytes(sb.ToString());
        return ([.. bom, .. govde], $"stopaj-{donem}.csv");
    }

    /// <summary>CSV hücresi: ayraç/tırnak/satır sonu varsa tırnakla; formül enjeksiyonu (=,+,-,@) önlenir.</summary>
    private static string Hucre(string? m)
    {
        var s = m ?? "";
        if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s;
        return s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>MySQL 1062 + bizim sıra indeksimiz (başka benzersizlik ihlalleri genel yola düşer).</summary>
    private static bool SiraCakismasi(DbUpdateException ex) =>
        ex.InnerException is { } ic
        && (ic.GetType().GetProperty("Number")?.GetValue(ic) is 1062
            || ic.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
        && ic.Message.Contains("sira_no", StringComparison.OrdinalIgnoreCase);
}

[ApiController]
[Authorize]
[Route("api/v1/stopaj")]
// Satıcı TCKN'si taşıyan liste + vergi borcu: gider pusulası ile AYNI rol kümesi.
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class StopajController(StopajServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/stopaj/donemler?yil= — dönem · kesilen · ödenen · kalan · son gün</summary>
    [HttpGet("donemler")]
    public Task<StopajDonemleriYaniti> Donemler([FromQuery] int? yil) =>
        servis.Donemler(yil ?? Zaman.YerelBugun().Year, Islem);

    /// <summary>GET /api/v1/stopaj/ozet — Panel: bu ay kesilen + geciken/yaklaşan dönemler</summary>
    [HttpGet("ozet")]
    public Task<StopajOzeti> Ozet() => servis.Ozet(Islem.TenantId, DateTime.UtcNow);

    /// <summary>POST /api/v1/stopaj/odemeler — ödeme (kasa çıkışı mevcut gider yolundan)</summary>
    [HttpPost("odemeler")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<StopajOdemeSonucu> OdemeKaydet([FromBody] StopajOdemeDto? dto) => servis.OdemeKaydet(dto, Islem);

    /// <summary>POST /api/v1/stopaj/odemeler/{id}/geri-al — kasa satırının stornosu (48-A)</summary>
    [HttpPost("odemeler/{id}/geri-al")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> GeriAl(string id, [FromBody] GerekceDto dto) => servis.GeriAl(id, dto.Gerekce, Islem);

    /// <summary>GET /api/v1/stopaj/donemler/{donem}/liste.csv — mali müşavir listesi (okuma izi)</summary>
    [HttpGet("donemler/{donem}/liste.csv")]
    public async Task<IActionResult> ListeCsv(string donem)
    {
        var (veri, ad) = await servis.ListeCsv(donem, Islem);
        return File(veri, "text/csv; charset=utf-8", ad);
    }
}
