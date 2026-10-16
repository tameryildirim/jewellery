using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// BANKA EKSTRESİ İÇE AKTARMA — paket 32 Faz A.
//
// ÜÇ CÜMLELİK SÖZLEŞME:
//   1. YÜKLEME HİÇBİR ŞEY YAZMAZ. /ekstre/yukle banka_hareketleri'ne SIFIR
//      satır yazar; satırlar sahne tablosunda bekler.
//   2. ONAY OLMADAN HAREKET YOK. Yalnız POST /onayla üretir ve YALNIZ gövdede
//      kimliği AÇIKÇA verilen satırlar için. "Tümünü onayla" ucu YOKTUR.
//   3. TEK YAZMA YOLU. Onay, BankaServisi.HareketEkleIc'i çağırır; bu dosyada
//      "BankaHareketleri.Add" GEÇMEZ (denetim.sh sayacı bunu zorlar).
//
// ⚠ MASAK (paket 41): kapı HareketEkleIc'in İÇİNDEDİR ve bu paket onu ATLAMAZ.
//   MASAK kimlik eşiği (07.10.2026'dan itibaren 370.000 TL) üstü tek satırın onayı, kimlik bilgisi verilmediği sürece
//   kapıya takılır ve O SATIR hatalı işaretlenir — diğerleri devam eder.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class EkstreYukleDto
{
    [Required, MaxLength(255)]
    public string DosyaAdi { get; set; } = null!;

    /// <summary>'CSV' — 'MT940' Faz B'dir ve bu pakette 400 döner.</summary>
    [Required, RegularExpression("CSV|MT940")]
    public string Format { get; set; } = EkstreFormati.Csv;

    [Required]
    public string IcerikBase64 { get; set; } = null!;
}

/// <summary>
/// PATCH gövdesi. ⚠ SÖZLEŞME: GÖNDERİLMEYEN ALAN "DEĞİŞMEDİ" DEMEKTİR.
/// Her iki alan da nullable'dır; tam gövdeyle satırı üzerine yazan bir uç
/// YAZILMADI (kurul engel 4 — sipariş kalem imhası deseni).
/// <para>Bağı KALDIRMAK için <c>cariId</c> açıkça <c>""</c> gönderilir.</para>
/// </summary>
public sealed class EkstreSatirGuncelleDto
{
    public string? CariId { get; set; }

    /// <summary>Yalnız ATLANDI ya da YENI (geri alma). ONAYLANDI elle verilemez.</summary>
    [RegularExpression("ATLANDI|YENI")]
    public string? Durum { get; set; }
}

public sealed class EkstreOnaylaDto
{
    /// <summary>⚠ BOŞ GÖNDERİLEMEZ (400) ve tavan aşılamaz (400).</summary>
    public List<string>? SatirIdler { get; set; }

    /// <summary>
    /// P-38 · B-18 (QA turu 7 #229) — CARİSİZ ONAY AÇIK RIZADIR.
    /// Eskiden cari seçilmeden onaylanan satır SESSİZCE carisiz bir banka
    /// hareketi üretiyordu; kullanıcı ne ürettiğini görmüyordu. Artık böyle
    /// bir satır varsayılan olarak ONAYLANMAZ: satır BEKLIYOR'da kalır (yani
    /// hâlâ düzeltilebilir) ve sebebi yanıtta yazar. Kuyumcu gerçekten
    /// carisiz yazmak istiyorsa bu bayrağı açıkça gönderir.
    /// </summary>
    public bool CarisizOnay { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class EkstreServisi(
    KuyumcuDbContext db,
    BankaServisi banka,
    DenetimServisi denetim,
    EkstreAyarlari? ayarlar = null)
{
    private readonly EkstreAyarlari ayar = ayarlar ?? new EkstreAyarlari();

    public const string DenetimVarligi = "EkstreIthalati";

    public EkstreAyarlari Ayarlar => ayar;

    // ── 1) Yükleme — deftere SIFIR satır ─────────────────────────────────────

    public async Task<object> Yukle(
        string hesapId, EkstreYukleDto dto, AktifKullanici islem, bool tekrar = false)
    {
        var hesap = await banka.TenantHesabiBul(hesapId, islem.TenantId);

        if (dto.Format == EkstreFormati.Mt940)
            throw new BadRequestException(
                "MT940 ayrıştırıcısı bu sürümde YOKTUR (Faz B). Elimizde gerçek banka MT940 " +
                "çıktısı olmadığı için doğrulanmamış bir ayrıştırıcı sevk edilmedi. " +
                "Bankanızın CSV/Excel ekstresini kullanın — 'Şablon indir' beklenen sütunları gösterir.");

        byte[] ham;
        try { ham = Convert.FromBase64String(dto.IcerikBase64); }
        catch (FormatException) { throw new BadRequestException("Dosya içeriği okunamadı (base64 bozuk)"); }

        if (ham.Length == 0) throw new BadRequestException("Dosya boş");
        if (ham.Length > ayar.MaksDosyaBayt)
            throw new BadRequestException(
                $"Dosya {ham.Length / 1024} KB; tavan {ayar.MaksDosyaBayt / 1024} KB. " +
                "Kısmi içe aktarım YAPILMAZ — dosyayı dönem dönem bölün.");

        var sha = EkstreAyristirici.DosyaOzeti(ham);

        // Aynı dosya aynı hesaba ikinci kez: 409. Zorlamak isteyen ?tekrar=true
        // ile geçer ve satırlar ZATEN mükerrer işaretlenir (indeks değil, niyet).
        var oncekiVar = await db.EkstreIthalatlari.AsNoTracking().AnyAsync(i =>
            i.TenantId == islem.TenantId && i.BankaHesapId == hesapId && i.Sha256 == sha);
        if (oncekiVar && !tekrar)
            throw new ConflictException(
                "Bu dosya bu hesaba zaten yüklendi. Yine de yüklemek için 'tekrar yükle' " +
                "seçeneğini kullanın; satırlar mükerrer olarak işaretlenecektir.");

        var metin = EkstreAyristirici.MetneCevir(ham);
        var sonuc = EkstreAyristirici.CsvAyristir(metin, ayar);

        var now = DateTime.UtcNow;
        var ithalatId = Kimlik.Ulid();

        // ── Otomatik eşleşme için IBAN hash defteri (TEK sorgu) ─────────────
        var ibanHashlari = sonuc.Satirlar
            .Where(s => s.KarsiIban is not null)
            .Select(s => db.Sifreleyici.Hashle(s.KarsiIban!))
            .Distinct()
            .ToList();

        var ibanSahipleri = ibanHashlari.Count == 0
            ? []
            : await db.CariIbanlari.AsNoTracking()
                .Where(c => c.TenantId == islem.TenantId && ibanHashlari.Contains(c.IbanHash))
                .ToDictionaryAsync(c => c.IbanHash, c => c.CariId);

        // ── Mükerrer tespiti: banka_hareketleri'nde AYNI anahtar var mı ─────
        var anahtarlar = new List<string>();
        var gecici = new List<(AyristirilmisSatir Satir, string? Anahtar)>();
        foreach (var s in sonuc.Satirlar)
        {
            string? anahtar = null;
            if (s.Hata is null && s.Tarih is not null && s.Tutar is not null && s.Yon is not null)
            {
                anahtar = DedupeAnahtari.Uret(
                    hesapId, s.Tarih.Value, s.Tutar.Value, s.Yon, s.BankaReferans, s.KarsiIban, s.Aciklama);
                anahtarlar.Add(anahtar);
            }
            gecici.Add((s, anahtar));
        }

        var defterdekiler = anahtarlar.Count == 0
            ? []
            : await db.BankaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == islem.TenantId && h.BankaHesapId == hesapId
                            && h.DedupeAnahtar != null && anahtarlar.Contains(h.DedupeAnahtar))
                .Select(h => h.DedupeAnahtar!)
                .ToListAsync();
        var defterKumesi = defterdekiler.ToHashSet(StringComparer.Ordinal);

        // Dosyanın KENDİ İÇİNDEKİ tekrarları da yakalanır: ikinci kez görülen
        // anahtar mükerrerdir (aksi hâlde onayda benzersiz indeks patlardı ve
        // kullanıcı sebebini anlamazdı).
        var dosyadaGorulen = new HashSet<string>(StringComparer.Ordinal);

        var satirlar = new List<EkstreSatiri>(gecici.Count);
        foreach (var (s, anahtar) in gecici)
        {
            var durum = EkstreSatirDurumu.Yeni;
            var hata = s.Hata;

            // GELECEK TARİH REDDEDİLİR (paket 41 K41-1 ile aynı gerekçe: ileri
            // tarihli kayıt MASAK kümülatifini bölmek için kullanılabilir).
            if (hata is null && s.Tarih > now.AddDays(1))
                hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("İleri tarihli satır kabul edilmez");

            if (hata is not null) durum = EkstreSatirDurumu.Hatali;
            else if (anahtar is not null &&
                     (defterKumesi.Contains(anahtar) || !dosyadaGorulen.Add(anahtar)))
                durum = EkstreSatirDurumu.Mukerrer;

            string? cariId = null, kaynak = null;
            // OTOMATİK EŞLEŞME YALNIZ IBAN (K4-OK-5). Ünvan benzerliği burada
            // HİÇ hesaplanmaz — hesaplansaydı "az kalsın bağlayacaktı" kodu
            // sonraki pakette bağlardı.
            if (durum != EkstreSatirDurumu.Hatali && s.KarsiIban is not null &&
                ibanSahipleri.TryGetValue(db.Sifreleyici.Hashle(s.KarsiIban), out var sahip))
            {
                cariId = sahip;
                kaynak = Data.EslesmeKaynagi.Iban;
            }

            satirlar.Add(new EkstreSatiri
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                IthalatId = ithalatId,
                SatirNo = s.SatirNo,
                HamSatir = s.HamSatir,
                Tarih = s.Tarih ?? now,
                Yon = s.Yon ?? Core.Domain.HareketYonu.BankaGelen,
                Tutar = s.Tutar ?? 0m,
                // PARA BİRİMİ HESAPTAN GELİR (BankaModulu ile aynı sözleşme).
                ParaBirimi = hesap.ParaBirimi,
                KarsiTarafAd = s.KarsiTarafAd,
                KarsiIban = s.KarsiIban,
                BankaReferans = s.BankaReferans,
                Aciklama = s.Aciklama,
                DedupeAnahtar = anahtar ?? "",
                Durum = durum,
                HataMesaji = hata,
                EslesenCariId = cariId,
                EslesmeKaynagi = kaynak,
                Olusturuldu = now,
                Guncellendi = now,
            });
        }

        var ithalat = new EkstreIthalati
        {
            Id = ithalatId,
            TenantId = islem.TenantId,
            BankaHesapId = hesapId,
            DosyaAdi = dto.DosyaAdi,
            Format = EkstreFormati.Csv,
            Sha256 = sha,
            SatirSayisi = satirlar.Count,
            YeniSayisi = satirlar.Count(s => s.Durum == EkstreSatirDurumu.Yeni),
            MukerrerSayisi = satirlar.Count(s => s.Durum == EkstreSatirDurumu.Mukerrer),
            HataliSayisi = satirlar.Count(s => s.Durum == EkstreSatirDurumu.Hatali),
            OnaylananSayisi = 0,
            Durum = EkstreIthalatDurumu.Prova,
            AcilisBakiye = sonuc.AcilisBakiye,
            KapanisBakiye = sonuc.KapanisBakiye,
            YukleyenKullaniciId = islem.Id,
            Olusturuldu = now,
            Guncellendi = now,
        };

        db.EkstreIthalatlari.Add(ithalat);
        db.EkstreSatirlari.AddRange(satirlar);

        // Denetim izi: YÜKLEME ve ONAY AYRI kayıtlardır (ADR-0004).
        denetim.YazmaKaydet(islem, DenetimVarligi, ithalatId,
            $"ekstre yüklendi · hesap: {hesapId} · dosya: {dto.DosyaAdi} · sha256: {sha[..16]}… · " +
            $"{ithalat.SatirSayisi} satır ({ithalat.YeniSayisi} yeni / {ithalat.MukerrerSayisi} mükerrer / " +
            $"{ithalat.HataliSayisi} hatalı) · üretilen banka hareketi: 0");

        await db.SaveChangesAsync();

        return new
        {
            ithalatId,
            durum = ithalat.Durum,
            dosyaAdi = ithalat.DosyaAdi,
            satirSayisi = ithalat.SatirSayisi,
            yeniSayisi = ithalat.YeniSayisi,
            mukerrerSayisi = ithalat.MukerrerSayisi,
            hataliSayisi = ithalat.HataliSayisi,
            onaylananSayisi = 0,
            // ⚠ SUNUCUDAN GELEN İDDİA: yükleme deftere hiçbir şey yazmadı.
            uretilenHareketSayisi = 0,
            maksOnaySatir = ayar.MaksOnaySatir,
            onizleme = satirlar.Take(50).Select(s => SatirOzeti(s, null)),
        };
    }

    // ── 2) İthalat başlığı + SAYFALI satır listesi (TB-19) ───────────────────

    public async Task<object> Getir(
        string ithalatId, AktifKullanici islem, string? durum, int limit, int offset)
    {
        if (limit < 1 || limit > ayar.SatirListesiTavan)
            throw new BadRequestException(
                $"limit 1 ile {ayar.SatirListesiTavan} arasında olmalıdır (TB-19: sınırsız liste yok).");
        if (offset < 0) throw new BadRequestException("offset negatif olamaz");

        var ithalat = await IthalatBul(ithalatId, islem.TenantId);
        var hesap = await banka.TenantHesabiBul(ithalat.BankaHesapId, islem.TenantId);

        var q = db.EkstreSatirlari.AsNoTracking()
            .Where(s => s.IthalatId == ithalatId && s.TenantId == islem.TenantId);
        if (!string.IsNullOrEmpty(durum))
        {
            if (!EkstreSatirDurumu.Tumu.Contains(durum))
                throw new BadRequestException($"Bilinmeyen durum: {durum}");
            q = q.Where(s => s.Durum == durum);
        }

        // Toplamlar SUNUCUDA sayılır; ekran sayfayı toplayıp tahmin etmez.
        var toplam = await q.CountAsync();
        var satirlar = await q.OrderBy(s => s.SatirNo).Skip(offset).Take(limit).ToListAsync();

        var cariAdlari = await CariAdlari(satirlar, islem.TenantId);

        return new
        {
            ithalatId = ithalat.Id,
            bankaHesapId = ithalat.BankaHesapId,
            hesapAdi = $"{hesap.BankaAdi} — {hesap.HesapAdi}",
            paraBirimi = hesap.ParaBirimi,
            dosyaAdi = ithalat.DosyaAdi,
            format = ithalat.Format,
            durum = ithalat.Durum,
            satirSayisi = ithalat.SatirSayisi,
            yeniSayisi = ithalat.YeniSayisi,
            mukerrerSayisi = ithalat.MukerrerSayisi,
            hataliSayisi = ithalat.HataliSayisi,
            onaylananSayisi = ithalat.OnaylananSayisi,
            acilisBakiye = ithalat.AcilisBakiye,
            kapanisBakiye = ithalat.KapanisBakiye,
            olusturuldu = ithalat.Olusturuldu,
            maksOnaySatir = ayar.MaksOnaySatir,
            limit,
            offset,
            toplam,
            satirlar = satirlar.Select(s => SatirOzeti(s, cariAdlari)),
        };
    }

    // ── 3) Tek satır: cari bağla / bağı kaldır / atla / geri al ──────────────

    public async Task<object> SatirGuncelle(
        string satirId, EkstreSatirGuncelleDto dto, AktifKullanici islem)
    {
        var satir = await db.EkstreSatirlari
            .FirstOrDefaultAsync(s => s.Id == satirId && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ekstre satırı bulunamadı");

        // ONAYLANMIŞ SATIR DEĞİŞMEZ: üretilen banka hareketi ona bağlıdır ve
        // sahne geçmişi bozulmaz. Düzeltme yolu, üretilen hareketin storno /
        // düzeltme fişidir (paket 48 / 33).
        if (satir.Durum == EkstreSatirDurumu.Onaylandi)
            throw new ConflictException(
                "Onaylanmış ekstre satırı değiştirilemez. Bu satırdan üretilen banka hareketi " +
                "yanlışsa düzeltme, hareketin storno/düzeltme fişi yoludur.");

        var now = DateTime.UtcNow;
        var degisiklikler = new List<string>();

        // ⚠ PATCH SÖZLEŞMESİ: gövdede YOKSA (null) dokunulmaz; "" ise bağ SİLİNİR.
        if (dto.CariId is not null)
        {
            if (dto.CariId.Length == 0)
            {
                satir.EslesenCariId = null;
                satir.EslesmeKaynagi = null;
                degisiklikler.Add("cari bağı kaldırıldı");
            }
            else
            {
                var cari = await db.Cariler.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CariId && c.TenantId == islem.TenantId)
                    ?? throw new NotFoundException("Cari bulunamadı");

                satir.EslesenCariId = cari.Id;
                satir.EslesmeKaynagi = Data.EslesmeKaynagi.Elle;
                degisiklikler.Add($"cari bağlandı: {cari.Id}");

                // ── ÖĞRENEN EŞLEŞTİRME (K4-OK-6) ───────────────────────────
                if (satir.KarsiIban is not null)
                    degisiklikler.Add(await IbanOgren(satir.KarsiIban, satir.KarsiTarafAd, cari.Id, islem, now));
            }
        }

        if (dto.Durum is not null)
        {
            if (dto.Durum == EkstreSatirDurumu.Atlandi &&
                satir.Durum is EkstreSatirDurumu.Yeni or EkstreSatirDurumu.Mukerrer)
            {
                satir.Durum = EkstreSatirDurumu.Atlandi;
                degisiklikler.Add("atlandı");
            }
            else if (dto.Durum == EkstreSatirDurumu.Yeni && satir.Durum == EkstreSatirDurumu.Atlandi)
            {
                // GERİ DÖNÜŞ YOLU: atlanan satır geri alınabilir.
                satir.Durum = EkstreSatirDurumu.Yeni;
                degisiklikler.Add("atlama geri alındı");
            }
            else if (dto.Durum != satir.Durum)
                throw new BadRequestException(
                    $"'{satir.Durum}' durumundaki satır '{dto.Durum}' yapılamaz.");
        }

        if (degisiklikler.Count == 0)
            throw new BadRequestException("Gövdede değiştirilecek alan yok (cariId ve durum boş).");

        satir.Guncellendi = now;
        denetim.YazmaKaydet(islem, "EkstreSatiri", satir.Id,
            $"satır {satir.SatirNo} · " + string.Join(" · ", degisiklikler));

        await db.SaveChangesAsync();
        await SayaclariTazele(satir.IthalatId, islem.TenantId);

        var cariAdlari = await CariAdlari([satir], islem.TenantId);
        return new { satir = SatirOzeti(satir, cariAdlari) };
    }

    /// <summary>
    /// IBAN'ı cariye öğretir. BİR IBAN TEK CARİYE: başka cariye bağlıysa 409 ve
    /// mevcut sahibinin adı döner — sessizce TAŞINMAZ.
    /// </summary>
    private async Task<string> IbanOgren(
        string iban, string? unvan, string cariId, AktifKullanici islem, DateTime now)
    {
        var hash = db.Sifreleyici.Hashle(iban);
        var mevcut = await db.CariIbanlari
            .FirstOrDefaultAsync(c => c.TenantId == islem.TenantId && c.IbanHash == hash);

        if (mevcut is not null)
        {
            if (mevcut.CariId == cariId) return "IBAN zaten bu cariye bağlı";
            var sahip = await db.Cariler.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == mevcut.CariId && c.TenantId == islem.TenantId);
            throw new ConflictException(
                $"Bu IBAN başka bir cariye bağlı: {sahip?.Ad ?? mevcut.CariId}. " +
                "Bir IBAN iki cariye bağlanamaz; önce mevcut bağı kaldırın.");
        }

        db.CariIbanlari.Add(new CariIban
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            CariId = cariId,
            Iban = iban,
            Unvan = unvan,
            Kaynak = IbanBagKaynagi.Ogrenildi,
            Olusturuldu = now,
        });
        return "IBAN öğrenildi";
    }

    // ── 4) Onay — banka hareketini ÜRETEN TEK yol ────────────────────────────

    public async Task<object> Onayla(string ithalatId, EkstreOnaylaDto dto, AktifKullanici islem)
    {
        var idler = (dto.SatirIdler ?? []).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();

        // "TÜMÜNÜ ONAYLA" UCU YOKTUR: ekran seçer, sunucu listeyi ister.
        if (idler.Count == 0)
            throw new BadRequestException(
                "Onaylanacak satır seçilmedi. Bu uç 'tümünü onayla' yapmaz; " +
                "onaylanacak satırların kimliği gövdede açıkça verilir.");
        if (idler.Count > ayar.MaksOnaySatir)
            throw new BadRequestException(
                $"Tek seferde en fazla {ayar.MaksOnaySatir} satır onaylanabilir " +
                $"(gönderilen {idler.Count}). Sayfa sayfa onaylayın.");

        var ithalat = await IthalatBul(ithalatId, islem.TenantId);

        var secililer = await db.EkstreSatirlari
            .Where(s => s.TenantId == islem.TenantId && s.IthalatId == ithalatId && idler.Contains(s.Id))
            .OrderBy(s => s.SatirNo)
            .ToListAsync();

        var bulunamayan = idler.Count - secililer.Count;
        var basarili = 0;
        var carisizAtlanan = 0;   // P-38 · B-18
        var carisizUretilen = 0;  // P-38 · B-18 — bilerek carisiz yazılanlar
        var hatalar = new List<object>();
        var uretilen = new List<string>();

        foreach (var satir in secililer)
        {
            // ⚠ İKİ KATMANLI 409'UN BİRİNCİ KATMANI: durum kontrolü.
            //   İkinci katman (tenant, hesap, dedupe_anahtar) BENZERSİZ
            //   indeksidir ve yarış durumunda DB'den döner.
            if (satir.Durum == EkstreSatirDurumu.Onaylandi)
            {
                hatalar.Add(new { satirNo = satir.SatirNo, hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu satır zaten onaylanmış") });
                continue;
            }
            if (satir.Durum == EkstreSatirDurumu.Hatali)
            {
                hatalar.Add(new { satirNo = satir.SatirNo, hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(satir.HataMesaji ?? "Satır hatalı") });
                continue;
            }

            // ── P-38 · B-18 — CARİSİZ SATIR SESSİZCE GEÇMEZ ────────────────
            // Satır HATALI İŞARETLENMEZ: hatalı satır bir daha onaylanamaz,
            // oysa buradaki eksik düzeltilebilir bir eksiktir. BEKLIYOR'da
            // kalır, sebebi yanıtta yazar, kullanıcı ya cari seçer ya da
            // `carisizOnay: true` ile bilerek carisiz yazar (UX-S27/S30).
            if (string.IsNullOrEmpty(satir.EslesenCariId) && !dto.CarisizOnay)
            {
                carisizAtlanan++;
                hatalar.Add(new
                {
                    satirNo = satir.SatirNo,
                    hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Cari seçilmedi: satır onaylanmadı. Satıra cari eşleştirin ya da "
                         + "bilerek carisiz yazmak için 'carisiz onayla' seçeneğini işaretleyin."),
                });
                continue;
            }

            try
            {
                // ⚠ TEK YAZMA YOLU. Burada db.BankaHareketleri.Add YAZILMAZ:
                //   MASAK kapısı (paket 41) ve ileri tarih kontrolü bu metodun
                //   İÇİNDEDİR ve toplu onayda da çalışmak ZORUNDADIR.
                var sonuc = await banka.HareketEkleIc(
                    ithalat.BankaHesapId,
                    new HareketEkleDto
                    {
                        Yon = satir.Yon,
                        Tutar = satir.Tutar,
                        KarsiTarafAd = satir.KarsiTarafAd,
                        KarsiIban = satir.KarsiIban,
                        Aciklama = satir.Aciklama,
                        CariId = satir.EslesenCariId,
                        Tarih = satir.Tarih.ToString("O"),
                    },
                    islem,
                    ekstreSatirId: satir.Id,
                    bankaReferans: satir.BankaReferans);

                var hareketId = (string)sonuc.GetType().GetProperty("hareketId")!.GetValue(sonuc)!;
                satir.BankaHareketId = hareketId;
                if (string.IsNullOrEmpty(satir.EslesenCariId)) carisizUretilen++;
                satir.Durum = EkstreSatirDurumu.Onaylandi;
                satir.Guncellendi = DateTime.UtcNow;
                uretilen.Add(hareketId);
                basarili++;
                await db.SaveChangesAsync();
            }
            catch (ApiException ex)
            {
                // SATIR BAZINDA HATA: o satır HATALI işaretlenir, diğerleri
                // devam eder. Tümünü geri almak, 400 satırlık bir dosyada tek
                // MASAK eşiği yüzünden hiçbir şeyin geçmemesi demekti.
                // URUN-ACILIS-ATOMIK: reddeden HareketEkleIc, BankaHareket'i Add
                // etmiş ve hesap bakiyesini değiştirmiş olabilir (MASAK kapısı
                // Add'den SONRA koşar). Aşağıdaki SaveChanges bunları SIZDIRMASIN —
                // reddedilen satırın yarım yazımı izleyiciden geri alınır.
                db.ChangeTracker.BekleyenleriGeriAl(satir);
                satir.Durum = EkstreSatirDurumu.Hatali;
                satir.HataMesaji = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;
                satir.Guncellendi = DateTime.UtcNow;
                hatalar.Add(new { satirNo = satir.SatirNo, hata = satir.HataMesaji });
                await db.SaveChangesAsync();
            }
        }

        if (bulunamayan > 0)
            hatalar.Add(new { satirNo = 0, hata = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir($"{bulunamayan} satır bu ithalatta bulunamadı") });

        if (basarili > 0)
        {
            ithalat.Durum = EkstreIthalatDurumu.Onaylandi;
            ithalat.Guncellendi = DateTime.UtcNow;
        }

        // Denetim izi: ONAY, yüklemeden AYRI kayıttır (ADR-0004).
        denetim.YazmaKaydet(islem, DenetimVarligi, ithalatId,
            $"ekstre onaylandı · seçilen: {idler.Count} · başarılı: {basarili} · " +
            $"hatalı: {hatalar.Count} · üretilen banka hareketi: {uretilen.Count} · " +
            $"carisiz üretilen: {carisizUretilen} · cari yok diye atlanan: {carisizAtlanan}");

        await db.SaveChangesAsync();
        await SayaclariTazele(ithalatId, islem.TenantId);

        return new
        {
            ithalatId,
            secilen = idler.Count,
            basarili,
            basarisiz = hatalar.Count,
            uretilenHareketSayisi = uretilen.Count,
            // P-38 · B-18: sonuç GÖRÜNÜR olur (UX-S27) — kaç satır cari yok
            // diye beklemede kaldı, kaç hareket bilerek carisiz yazıldı.
            carisizAtlanan,
            carisizUretilen,
            hatalar,
        };
    }

    // ── 5) Ünvan önerileri — ASLA otomatik uygulanmaz ────────────────────────

    public async Task<object> Oneriler(string satirId, AktifKullanici islem)
    {
        var satir = await db.EkstreSatirlari.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == satirId && s.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ekstre satırı bulunamadı");

        if (string.IsNullOrWhiteSpace(satir.KarsiTarafAd))
            return new { satirId, esik = ayar.OneriEsigi, oneriler = Array.Empty<object>() };

        // TB-19: sınırsız cari taraması yok — ilk 500 aktif cari.
        var cariler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == islem.TenantId && c.Aktif)
            .OrderBy(c => c.Ad)
            .Take(500)
            .Select(c => new { c.Id, c.Ad })
            .ToListAsync();

        var oneriler = cariler
            .Select(c => new { cariId = c.Id, ad = c.Ad, skor = CariEslestirici.Skor(satir.KarsiTarafAd, c.Ad) })
            .Where(o => o.skor > 0)
            .OrderByDescending(o => o.skor)
            .Take(5)
            .ToList();

        return new
        {
            satirId,
            karsiTarafAd = satir.KarsiTarafAd,
            esik = ayar.OneriEsigi,
            tarananCari = cariler.Count,
            // ⚠ SUNUCUDAN GELEN İDDİA: hiçbiri uygulanmadı. Ünvan benzerliği
            //   otomatik bağlamaz (K4-OK-5); kullanıcı seçer, PATCH bağlar.
            otomatikUygulanan = 0,
            oneriler,
        };
    }

    /// <summary>Şablon CSV — başlıklar + tek örnek satır.</summary>
    public static string SablonCsv() =>
        string.Join(";", EkstreAyristirici.SablonBasliklari) + "\r\n" +
        "01.09.2026;ALI VELI HAVALE;;5.000,00;25.000,00;REF123456;TR330006100519786457841326;ALİ VELİ\r\n";

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<EkstreIthalati> IthalatBul(string id, string tenantId) =>
        await db.EkstreIthalatlari.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId)
        ?? throw new NotFoundException("Ekstre ithalatı bulunamadı");

    private async Task<Dictionary<string, string>> CariAdlari(
        IReadOnlyList<EkstreSatiri> satirlar, string tenantId)
    {
        var idler = satirlar.Where(s => s.EslesenCariId is not null)
            .Select(s => s.EslesenCariId!).Distinct().ToList();
        if (idler.Count == 0) return [];
        return await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == tenantId && idler.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Ad);
    }

    /// <summary>
    /// ⚠ SAYAÇLAR İZLENEN VARLIKLA GÜNCELLENİR: ExecuteUpdate beyaz listesi
    /// BÜYÜTÜLMEZ (denetim.sh). ExecuteUpdate SaveChanges'ı atlar, yani ne
    /// yazma denetçisi ne denetim izi görürdü.
    /// </summary>
    private async Task SayaclariTazele(string ithalatId, string tenantId)
    {
        var ithalat = await db.EkstreIthalatlari
            .FirstOrDefaultAsync(i => i.Id == ithalatId && i.TenantId == tenantId);
        if (ithalat is null) return;

        var sayim = await db.EkstreSatirlari.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IthalatId == ithalatId)
            .GroupBy(s => s.Durum)
            .Select(g => new { Durum = g.Key, Adet = g.Count() })
            .ToListAsync();

        int Adet(string d) => sayim.FirstOrDefault(x => x.Durum == d)?.Adet ?? 0;
        ithalat.YeniSayisi = Adet(EkstreSatirDurumu.Yeni);
        ithalat.MukerrerSayisi = Adet(EkstreSatirDurumu.Mukerrer);
        ithalat.HataliSayisi = Adet(EkstreSatirDurumu.Hatali);
        ithalat.OnaylananSayisi = Adet(EkstreSatirDurumu.Onaylandi);
        ithalat.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static object SatirOzeti(EkstreSatiri s, IReadOnlyDictionary<string, string>? cariAdlari = null) => new
    {
        id = s.Id,
        satirNo = s.SatirNo,
        // ⚠ EKRANDA YALNIZ METİN BASILIR (MarkupString YASAK) ve ham satırın
        //   İÇİNDEKİ IBAN'lar da MASKELENİR. Diskte şifreli olması yetmez:
        //   ekrana giden kopya düz metin tam IBAN taşısaydı, KVKK açısından
        //   şifreleme yalnız "yedeği koruyan" bir önlem olurdu (paket 47'nin
        //   "ekrana giden kopya maskelidir" kararıyla aynı).
        hamSatir = HamSatirMaskele(s.HamSatir),
        tarih = s.Tarih,
        yon = s.Yon,
        tutar = s.Tutar,
        paraBirimi = s.ParaBirimi,
        karsiTarafAd = s.KarsiTarafAd,
        karsiIban = IbanMaskele(s.KarsiIban),
        bankaReferans = s.BankaReferans,
        aciklama = s.Aciklama,
        durum = s.Durum,
        hataMesaji = s.HataMesaji,
        eslesenCariId = s.EslesenCariId,
        eslesenCariAd = s.EslesenCariId is not null && cariAdlari is not null
            && cariAdlari.TryGetValue(s.EslesenCariId, out var ad) ? ad : null,
        eslesmeKaynagi = s.EslesmeKaynagi,
        bankaHareketId = s.BankaHareketId,
    };

    /// <summary>Ham satırdaki TR IBAN'larını maskeler ("TR33 **** 1326").</summary>
    public static string HamSatirMaskele(string? ham) =>
        string.IsNullOrEmpty(ham)
            ? ham ?? ""
            : System.Text.RegularExpressions.Regex.Replace(
                ham, @"\bTR\d{24}\b",
                m => IbanMaskele(m.Value)!,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));

    private static object SatirOzeti(AyristirilmisSatir s) => new
    {
        satirNo = s.SatirNo,
        hamSatir = HamSatirMaskele(s.HamSatir),
        tarih = s.Tarih,
        yon = s.Yon,
        tutar = s.Tutar,
        karsiTarafAd = s.KarsiTarafAd,
        karsiIban = IbanMaskele(s.KarsiIban),
        hata = s.Hata,
    };

    /// <summary>KVKK: ekrana giden kopya maskelidir — "TR33 **** 1326".</summary>
    public static string? IbanMaskele(string? iban) =>
        string.IsNullOrWhiteSpace(iban) ? null
        : iban.Length <= 8 ? iban
        : $"{iban[..4]} **** {iban[^4..]}";
}

// ── Controller ───────────────────────────────────────────────────────────────
// Sınıf düzeyinde [ModulYetkisi("banka")] + her uçta TAM rol eşleşmesi.
// TEZGAHTAR bu uçların HİÇBİRİNE giremez (403) — ekstre içe aktarımı muhasebe
// işidir ve deftere toplu yazma üretir.

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class EkstreController(EkstreServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/bankalar/ekstre/sablon.csv — beklenen sütun başlıkları</summary>
    [HttpGet("ekstre/sablon.csv")]
    public IActionResult Sablon() =>
        File(System.Text.Encoding.UTF8.GetBytes("﻿" + EkstreServisi.SablonCsv()),
            "text/csv; charset=utf-8", "banka-ekstre-sablon.csv");

    /// <summary>
    /// POST /api/v1/bankalar/{id}/ekstre/yukle — dosyayı sahneye alır.
    /// ⚠ banka_hareketleri'ne SIFIR satır yazar.
    /// </summary>
    [HttpPost("{id}/ekstre/yukle")]
    [RequestSizeLimit(3_000_000)]
    [EnableRateLimiting(OranSiniri.Ekstre)]
    public Task<object> Yukle(string id, [FromBody] EkstreYukleDto dto, [FromQuery] bool tekrar = false)
        => servis.Yukle(id, dto, Islem, tekrar);

    /// <summary>GET /api/v1/bankalar/ekstre/{id}?durum=&amp;limit=100&amp;offset=0</summary>
    [HttpGet("ekstre/{id}")]
    public Task<object> Getir(
        string id, [FromQuery] string? durum = null,
        [FromQuery, Range(1, 100)] int limit = 100, [FromQuery, Range(0, int.MaxValue)] int offset = 0)
        => servis.Getir(id, Islem, durum, limit, offset);

    /// <summary>PATCH /api/v1/bankalar/ekstre/satir/{id} — gönderilmeyen alan DEĞİŞMEZ</summary>
    [HttpPatch("ekstre/satir/{id}")]
    public Task<object> SatirGuncelle(string id, [FromBody] EkstreSatirGuncelleDto dto)
        => servis.SatirGuncelle(id, dto, Islem);

    /// <summary>GET /api/v1/bankalar/ekstre/satir/{id}/oneriler — ASLA otomatik uygulanmaz</summary>
    [HttpGet("ekstre/satir/{id}/oneriler")]
    public Task<object> Oneriler(string id) => servis.Oneriler(id, Islem);

    /// <summary>
    /// POST /api/v1/bankalar/ekstre/{id}/onayla — SEÇİLİ satırları banka
    /// hareketine çevirir. Boş liste 400; tavan aşımı 400.
    /// </summary>
    [HttpPost("ekstre/{id}/onayla")]
    [EnableRateLimiting(OranSiniri.Ekstre)]
    public Task<object> Onayla(string id, [FromBody] EkstreOnaylaDto dto)
        => servis.Onayla(id, dto, Islem);
}
