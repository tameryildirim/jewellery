using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK SERVİSİ: girdiyi TOPLAR, saf motoru çağırır, APPEND-ONLY
// profil satırı yazar.
//
// ⚠ PROFİL YAZMANIN TEK YOLU BURASIDIR. İkinci bir yazma motoru açılmaz;
//   append-only kuralı ayrıca KuyumcuDbContext.RiskProfiliniKoru() ile
//   SaveChanges düzeyinde zorlanır (Mehmet K40-5).
//
// ⚠ TÜM TOPLAMLAR SUNUCUDA (TB-19): SumAsync / CountAsync. Hiçbir yerde
//   hareket listesi belleğe çekilmez.
//
// ⚠ RİSK SKORU HİÇBİR KAPIYA BAĞLANMAZ. Bu dosyada BadRequestException yalnız
//   GİRDİ DOĞRULAMASI için atılır (gerekçe kısa, seviye geçersiz); skorun
//   kendisi asla bir işlemi durdurmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Elle seviye ezme gövdesi.</summary>
public sealed class RiskElleDto
{
    [Required(ErrorMessage = "Seviye zorunludur.")]
    public string? Seviye { get; set; }

    [Required(ErrorMessage = "Gerekçe zorunludur.")]
    // PAKET GOC-B (envanter C satır 324): ekran 1000 / kolon 500 — 501–1000 karakter DTO'dan geçip veritabanında
    // patlıyordu (400 yerine 500). Sınır kolonla TEK sabit; servis yolunda da elle.
    [MaxLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.RiskElleGerekce)]
    public string? Gerekce { get; set; }
}

public sealed class RiskServisi(
    KuyumcuDbContext db,
    MasakRiskAyarlari ayarlar,
    MasakEsikleri esikler,
    DenetimServisi denetim,
    YasalParametreServisi? yasal = null)
{
    private readonly RiskMotoru motor = new(ayarlar);

    private static readonly JsonSerializerOptions SebepAyari = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public MasakRiskAyarlari Ayarlar => ayarlar;

    // ── Okuma ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Carinin GÜNCEL profili + son 10 profil satırı.
    /// ⚠ KVKK md.12 (Mehmet K40-3): yanıt cari ADINI taşıdığı için tekil uçta
    ///   OKUMA İZİ düşer. Kimlik belge numarası bu uçta HİÇ GEÇMEZ.
    /// </summary>
    public async Task<object> Getir(string cariId, AktifKullanici islem)
    {
        var cari = await CariBul(cariId, islem.TenantId);

        var tarihce = await db.CariRiskProfilleri.AsNoTracking()
            .Where(p => p.TenantId == islem.TenantId && p.CariId == cariId)
            .OrderByDescending(p => p.Olusturuldu)
            .Take(10)
            .ToListAsync();

        await denetim.OkumaKaydet(islem, "CariRiskProfili", cariId, "risk profili görüntülendi");

        return new
        {
            cariId = cari.Id,
            cariAd = cari.Ad,
            pepBeyani = cari.PepBeyani,
            pepGorev = cari.PepGorev,
            pepBeyanTarihi = cari.PepBeyanTarihi,
            guncel = tarihce.Count == 0 ? null : Sekillendir(tarihce[0]),
            tarihce = tarihce.Select(Sekillendir).ToList(),
            // UX-S30: kilitli/etkisiz olanın SEBEBİ ekranda yazılabilsin.
            kuralEtkin = ayarlar.Etkin,
            gerekceAsgari = ayarlar.ElleGerekceAsgari,
            tazelemeGunu = ayarlar.ProfilTazelemeGunu,
        };
    }

    /// <summary>
    /// Panel/özet kartı: seviye dağılımı, PEP sayısı, tazelenmesi gereken profil
    /// sayısı ve yüksek riskli cari listesi.
    /// ⚠ Liste cari ADI taşır → ListeOkumaKaydet (KVKK md.12). Kimlik verisi yok.
    /// </summary>
    public async Task<object> Ozet(AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;

        // Güncel profil = her cari için EN SON satır. Tümü sunucuda (TB-19).
        // ⚠ GroupBy(...).First() DEĞİL "daha yenisi YOK" deseni kullanılır:
        //   Pomelo'da gruptan varlık döndüren projeksiyon istemciye kaçabilir;
        //   bu desen (tenant_id, cari_id, olusturuldu) indeksinden karşılanır.
        var tid = islem.TenantId;
        var sonlar = db.CariRiskProfilleri.AsNoTracking()
            .Where(p => p.TenantId == tid
                && !db.CariRiskProfilleri.Any(x =>
                    x.TenantId == tid && x.CariId == p.CariId && x.Olusturuldu > p.Olusturuldu));

        var dagilim = await sonlar
            .GroupBy(p => p.Seviye)
            .Select(g => new { seviye = g.Key, adet = g.Count() })
            .ToListAsync();

        var tazelenecek = await sonlar
            .CountAsync(p => p.HesaplanmaTarihi < simdi.AddDays(-ayarlar.ProfilTazelemeGunu));

        var pepSayisi = await db.Cariler.AsNoTracking()
            .CountAsync(c => c.TenantId == tid && c.PepBeyani);

        var profilsiz = await db.Cariler.AsNoTracking()
            .CountAsync(c => c.TenantId == tid && c.Aktif
                && !db.CariRiskProfilleri.Any(p => p.TenantId == tid && p.CariId == c.Id));

        var yuksekler = await sonlar
            .Where(p => p.Seviye == RiskSeviyesi.Yuksek)
            .OrderByDescending(p => p.Puan)
            .Take(ayarlar.OzetListeSiniri)
            .Join(db.Cariler.AsNoTracking(), p => p.CariId, c => c.Id,
                (p, c) => new { p.CariId, c.Ad, p.Puan, p.Seviye, p.Sebepler, p.Kaynak, p.Olusturuldu })
            .ToListAsync();

        if (yuksekler.Count > 0)
            await denetim.ListeOkumaKaydet(islem, "CariRiskProfili", yuksekler.Count, "yüksek riskli cari listesi");

        return new
        {
            dusuk = dagilim.FirstOrDefault(d => d.seviye == RiskSeviyesi.Dusuk)?.adet ?? 0,
            orta = dagilim.FirstOrDefault(d => d.seviye == RiskSeviyesi.Orta)?.adet ?? 0,
            yuksek = dagilim.FirstOrDefault(d => d.seviye == RiskSeviyesi.Yuksek)?.adet ?? 0,
            pepSayisi,
            tazelenecek,
            profilsiz,
            kuralEtkin = ayarlar.Etkin,
            listeSiniri = ayarlar.OzetListeSiniri,
            yuksekRiskliler = yuksekler.Select(y => new
            {
                cariId = y.CariId,
                cariAd = y.Ad,
                puan = y.Puan,
                seviye = y.Seviye,
                kaynak = y.Kaynak,
                sebepler = Coz(y.Sebepler),
                olusturuldu = y.Olusturuldu,
            }).ToList(),
        };
    }

    // ── Hesaplama ────────────────────────────────────────────────────────────

    /// <summary>
    /// Girdileri toplar, motoru çağırır ve YENİ SATIR yazar (güncelleme DEĞİL).
    /// </summary>
    public async Task<object> Hesapla(string cariId, AktifKullanici islem)
    {
        var cari = await CariBul(cariId, islem.TenantId);
        var girdi = await GirdiTopla(cari, islem.TenantId);
        var sonuc = motor.Degerlendir(girdi);

        var satir = YeniSatir(cariId, islem, RiskKaynagi.Hesap, sonuc.Puan, sonuc.Seviye,
            JsonSerializer.Serialize(sonuc.Sebepler, SebepAyari), gerekce: null);

        await YuksegeGecisBildir(cari, islem.TenantId, satir.Seviye);
        db.CariRiskProfilleri.Add(satir);
        await db.SaveChangesAsync();

        return new
        {
            cariId,
            cariAd = cari.Ad,
            profil = Sekillendir(satir),
            girdiOzeti = new
            {
                pepBeyani = girdi.PepBeyani,
                nakitOran = Math.Round(girdi.SonDonemNakitOran, 4),
                toplamTl = girdi.SonDonemToplamTl,
                esigeYakinIslem = girdi.EsigeYakinIslemSayisi,
                kimlikEksik = girdi.KimlikEksikSayisi,
                supheliBildirim = girdi.SupheliBildirimSayisi,
                pencereGun = ayarlar.DegerlendirmePencereGunu,
            },
            kuralEtkin = ayarlar.Etkin,
        };
    }

    /// <summary>
    /// Seviyeyi GEREKÇEYLE elle ezer. Yeni satır (kaynak=ELLE) ve denetim izine
    /// YAZMA kaydı AYNI SaveChanges ile yazılır: iş kaydı yazılıp iz yazılamazsa
    /// ikisi birden geri alınır (DenetimIzi.cs:135-141 sözleşmesi).
    /// </summary>
    public async Task<object> Elle(string cariId, RiskElleDto dto, AktifKullanici islem)
    {
        var cari = await CariBul(cariId, islem.TenantId);

        // DTO DataAnnotations YALNIZ controller yolunda çalışır; servis kendi
        // kontrolünü yapar (K4 sözleşmesi).
        var seviye = dto.Seviye?.Trim().ToUpperInvariant();
        if (!RiskSeviyesi.Gecerli(seviye))
            throw new BadRequestException(
                $"Risk seviyesi geçersiz — kabul edilen değerler: {string.Join(", ", RiskSeviyesi.Hepsi)}");

        var gerekce = dto.Gerekce?.Trim() ?? "";
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Uzunluk(gerekce, Kuyumcu.Core.Domain.Girdi.AlanSinirlari.RiskElleGerekce, "gerekce");   // PAKET GOC-B
        if (gerekce.Length < ayarlar.ElleGerekceAsgari)
            throw new BadRequestException(
                $"Elle seviye değişikliğinde gerekçe zorunludur ve en az {ayarlar.ElleGerekceAsgari} karakter olmalıdır " +
                $"(girilen: {gerekce.Length}). Gerekçesiz ezme skoru anlamsız kılar.");

        // Puan: elle ezmede puan HESAPLANMAZ; son hesabın puanı taşınır ki
        // "seviye elle, puan motordan" ayrımı tarihçede görünsün.
        var sonPuan = await db.CariRiskProfilleri.AsNoTracking()
            .Where(p => p.TenantId == islem.TenantId && p.CariId == cariId)
            .OrderByDescending(p => p.Olusturuldu)
            .Select(p => (int?)p.Puan)
            .FirstOrDefaultAsync() ?? 0;

        var satir = YeniSatir(cariId, islem, RiskKaynagi.Elle, sonPuan, seviye!, "[]", gerekce);
        await YuksegeGecisBildir(cari, islem.TenantId, satir.Seviye);
        db.CariRiskProfilleri.Add(satir);

        // ⚠ MEVCUT İMZA ÇAĞRILIR, YENİDEN YAZILMAZ (Bertan kısmi vetosu).
        //   SaveChanges ÇAĞIRMAZ: aşağıdaki TEK SaveChanges ikisini birlikte yazar.
        denetim.YazmaKaydet(islem, "CariRiskProfili", satir.Id,
            $"risk seviyesi elle değiştirildi: {seviye} · gerekçe: {gerekce}", hassas: true);

        await db.SaveChangesAsync();

        return new { cariId, cariAd = cari.Ad, profil = Sekillendir(satir), kuralEtkin = ayarlar.Etkin };
    }

    // ── PAKET YASAL-3 (§ 4.5) — YÜKSEK'E YENİ GEÇİŞ BİLDİRİMİ ────────────────
    /// <summary>
    /// Cari YÜKSEK seviyeye YENİ geçtiyse (son profil YÜKSEK değilse) SAHİP/MUHASEBE'ye
    /// tek bildirim: "şüpheli işlem bildirimi gerekip gerekmediğini değerlendirin".
    /// ENGEL YOK (risk skoru bağlayıcı değildir — Uyum Programı Yön. md.4 kuyumcuyu
    /// saymaz, şartname § 3.1 M-6). Aynı seviyede kalış yeni bildirim üretmez.
    /// SaveChanges ÇAĞIRMAZ — profil satırıyla aynı commit'te yazılır.
    /// </summary>
    /// <summary>Bildirim türü (bildirimler.tur) — hareket türü DEĞİL.</summary>
    public const string RiskYuksekBildirimi = "RISK_YUKSEK";

    private async Task YuksegeGecisBildir(Data.Cari cari, string tenantId, string yeniSeviye)
    {
        if (yeniSeviye != RiskSeviyesi.Yuksek) return;
        var onceki = await db.CariRiskProfilleri.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.CariId == cari.Id)
            .OrderByDescending(p => p.Olusturuldu)
            .Select(p => p.Seviye).FirstOrDefaultAsync();
        if (onceki == RiskSeviyesi.Yuksek) return;
        var simdi = DateTime.UtcNow;
        db.Bildirimler.Add(new Bildirim
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, Tur = RiskYuksekBildirimi, Onem = BildirimOnem.Uyari,
            Baslik = "Müşterinin risk seviyesi yükseldi",
            Govde = "Bu müşteri için şüpheli işlem bildirimi gerekip gerekmediğini değerlendirin. Gerekirse Uyum ekranından bildirim taslağı açın.",
            HedefRota = "/uyum", GorunurRoller = "SAHIP,MUHASEBE",
            IlgiliVarlikTuru = "Cari", IlgiliVarlikId = cari.Id,
            TekillikAnahtari = $"RISK_YUKSEK:{cari.Id}:{simdi:yyyyMMddHHmmssfff}", Olusturuldu = simdi,
        });
    }

    // ── Girdi toplama ────────────────────────────────────────────────────────

    private async Task<RiskGirdi> GirdiTopla(Data.Cari cari, string tenantId)
    {
        var baslangic = DateTime.UtcNow.AddDays(-ayarlar.DegerlendirmePencereGunu);

        var hareketler = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == cari.Id && h.Olusturuldu >= baslangic);

        // Tümü SUNUCUDA toplanır (TB-19). Nakit payı HareketFabrikasi'nın
        // yazdığı nakit_tutar kolonudur — tevsik kümülatifiyle AYNI kaynak.
        var nakit = await hareketler.SumAsync(h => (decimal?)(h.NakitTutar ?? 0m)) ?? 0m;
        var toplam = await hareketler.SumAsync(h => (decimal?)(h.TlTutari < 0 ? -h.TlTutari : h.TlTutari)) ?? 0m;

        // PAKET YASAL-1 (K-13): her işlem KENDİ GÜNÜNDEKİ kimlik eşiğiyle sınıflanır.
        // Pencere eşik dönemlerine bölünür; her dönem kendi eşiğiyle SUNUCUDA
        // (SQL) sayılır. Eşik sonradan değişse de geçmiş işlemin bandı değişmez.
        var esigeYakin = 0;
        var esikUstu = 0;
        // YASAL-1 tur 2 (hasım DÜŞÜK-4): pencere Olusturuldu'ya göre süzülür ama işlemin
        // GÜNÜ Tarih'tir — geç girilen/içe aktarılan kayıt pencere başından eski olabilir.
        // Dönemler pencerenin EN ESKİ Tarih gününden başlar → o kayıt da kendi gününün eşiğini alır.
        var enEskiTarih = await hareketler.MinAsync(h => (DateTime?)h.Tarih);
        var donemBaslangici = enEskiTarih is { } en && en < baslangic ? en : baslangic;
        foreach (var (donemBas, donemBit, kimlikEsigi) in KimlikEsigiDonemleri(donemBaslangici))
        {
            var (alt, ust) = motor.Band(kimlikEsigi);
            var donem = hareketler.Where(h => h.Tarih >= donemBas && h.Tarih < donemBit);
            esigeYakin += await donem.CountAsync(h =>
                (h.TlTutari < 0 ? -h.TlTutari : h.TlTutari) >= alt
                && (h.TlTutari < 0 ? -h.TlTutari : h.TlTutari) <= ust);

            // Eşik ÜSTÜ işlem sayısı ile o cari için alınmış kimlik tespiti sayısı
            // arasındaki fark. Negatif olamaz (düzeltme kayıtları fazla tespit doğurabilir).
            esikUstu += await donem.CountAsync(h =>
                (h.TlTutari < 0 ? -h.TlTutari : h.TlTutari) >= kimlikEsigi);
        }
        var tespitSayisi = await db.MasakKimlikTespitleri.AsNoTracking()
            .CountAsync(t => t.TenantId == tenantId && t.CariId == cari.Id && t.Olusturuldu >= baslangic);
        var kimlikEksik = Math.Max(0, esikUstu - tespitSayisi);

        var supheli = await db.MasakSupheliBildirimler.AsNoTracking()
            .CountAsync(s => s.TenantId == tenantId && s.CariId == cari.Id);

        // Uyruk carinin KENDİ kolonunda yok; MASAK kimlik tespitinden okunur.
        // Yalnız ÜLKE KODU alınır — ad/belge no/adres okunmaz.
        var uyruk = await db.MasakKimlikTespitleri.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CariId == cari.Id && t.Uyruk != null)
            .OrderByDescending(t => t.Olusturuldu)
            .Select(t => t.Uyruk)
            .FirstOrDefaultAsync();

        return new RiskGirdi(
            CariTuru: cari.Tur,
            PepBeyani: cari.PepBeyani,
            Uyruk: uyruk,
            SonDonemNakitOran: toplam > 0m ? nakit / toplam : 0m,
            SonDonemToplamTl: toplam,
            EsigeYakinIslemSayisi: esigeYakin,
            KimlikEksikSayisi: kimlikEksik,
            SupheliBildirimSayisi: supheli,
            // PAKET YASAL-3 (§ 4.4): yasal liste eşleşmesi (tam ya da ad benzerliği,
            //   "aynı kişi değil" işaretlenmemiş) gerçek girdidir. Puan bağlayıcı DEĞİL.
            YaptirimEslesmesiVar: await db.YaptirimEslesmeleri.AsNoTracking().AnyAsync(e =>
                e.TenantId == tenantId && e.CariId == cari.Id
                && e.Durum != YaptirimEslesmeDurumu.AyniKisiDegil));
    }

    // ── Ortak ────────────────────────────────────────────────────────────────

    /// <summary>
    /// PAKET YASAL-1: değerlendirme penceresini kimlik eşiği dönemlerine böler →
    /// [BasUtc, BitUtc) ve o dönemin eşiği. Tablo verilmemişse (DI'sız kurulum) tek dönem,
    /// yapılandırma eşiği. Pencerede tablonun tanımsız olduğu bir an varsa FAIL-CLOSED (503).
    /// PAKET YASAL-PARAM-2: dönem sınırı GÜN DEĞİL AN'dır (<see cref="YasalParametreServisi.DonemlerAn"/>):
    /// eşik bugün öğlen değiştiyse o sabahki işlem ESKİ eşikle değerlendirilir (onay penceresinin
    /// "bugün daha önce tamamlanmış işlemler yeniden değerlendirilmez" sözü). İşlemin anı <c>Tarih</c>'tir.
    /// </summary>
    private IEnumerable<(DateTime BasUtc, DateTime BitUtc, decimal Esik)> KimlikEsigiDonemleri(DateTime baslangicUtc)
    {
        if (yasal is null)
        {
            yield return (DateTime.MinValue, DateTime.MaxValue, esikler.KimlikEsigi);   // YASAL-1: DI'sız yedek
            yield break;
        }
        // Çağıran baslangicUtc'yi pencerenin EN ESKİ işlem anına (Tarih) çeker; ilk dönemin
        // alttan açık olması yalnız güvenlik payıdır. Şimdiden ileri tarihli kayıt son dönemin
        // eşiğini alır (üstten açık).
        var donemler = yasal.DonemlerAn(YasalAnahtar.MasakKimlikEsigiTl, baslangicUtc, yasal.SimdiUtc);
        for (var i = 0; i < donemler.Count; i++)
        {
            var (bas, bit, esik) = donemler[i];
            yield return (
                i == 0 ? DateTime.MinValue : bas,
                i == donemler.Count - 1 ? DateTime.MaxValue : bit,
                esik);
        }
    }

    private async Task<Data.Cari> CariBul(string cariId, string tenantId) =>
        await db.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cariId && c.TenantId == tenantId)
        ?? throw new NotFoundException("Cari bulunamadı");

    private static CariRiskProfili YeniSatir(
        string cariId, AktifKullanici islem, string kaynak, int puan, string seviye,
        string sebeplerJson, string? gerekce)
    {
        var simdi = DateTime.UtcNow;
        return new CariRiskProfili
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = islem.TenantId,
            CariId = cariId,
            Kaynak = kaynak,
            Puan = puan,
            Seviye = seviye,
            Sebepler = sebeplerJson,
            Gerekce = gerekce,
            KullaniciId = islem.Id,
            HesaplanmaTarihi = simdi,
            Olusturuldu = simdi,
        };
    }

    private static object Sekillendir(CariRiskProfili p) => new
    {
        p.Id,
        cariId = p.CariId,
        kaynak = p.Kaynak,
        puan = p.Puan,
        seviye = p.Seviye,
        sebepler = Coz(p.Sebepler),
        gerekce = p.Gerekce,
        kullaniciId = p.KullaniciId,
        hesaplanmaTarihi = p.HesaplanmaTarihi,
        olusturuldu = p.Olusturuldu,
    };

    private static List<string> Coz(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        // ⚠ Bozuk JSON'da boş liste döner ama SATIR GİZLENMEZ: puan ve seviye
        //   yine görünür. "Hata → kayıt yok" çevirisi yasaktır (UX-S28).
        catch (JsonException) { return []; }
    }
}
