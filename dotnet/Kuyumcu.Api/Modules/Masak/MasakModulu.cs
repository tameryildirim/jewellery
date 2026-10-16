using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GD = Kuyumcu.Api.Common.Dogrulama;   // PAKET GOC-B
using GS = Kuyumcu.Core.Domain.Girdi.AlanSinirlari;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK servis + uçlar — SIRADAKI-ADIM-KARAR §2/A (E-3 ve okuma izi A'nın içinde).
//
// Mehmet'in şartları (aynen):
//   md.3  MASAK verisine mevcut rol matrisinden AYRI bir izin: TEZGAHTAR asla görmez.
//         → Tüm kişisel-veri uçları [RolGerekli("SAHIP","MUDUR","MUHASEBE")].
//         Yalnız /esikler herkese açık — kişisel veri yok, POS formun ne isteyeceğini bilmeli.
//   md.4  "Kim hangi TCKN'yi GÖRDÜ" → her okuma ucu DenetimServisi.OkumaKaydet çağırır.
//   md.2  Listede belge no MASKELİ (son 4 hane); tam değer yalnız tekil GET'te, izli.
//
// Silme ucu YOK — 8 yıl saklama. Şüpheli bildirim "kapatılır", silinmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SupheliBildirimDto
{
    [MaxLength(36)] public string? HareketId { get; set; }
    [MaxLength(36)] public string? CariId { get; set; }
    [MaxLength(36)] public string? KimlikTespitiId { get; set; }
    // PAKET GOC-B (envanter C satır 161, #10): 0 ve 10^15 üstü kabul ediliyordu → K9 (> 0, 2 ondalık, decimal(15,2) tavanı).
    [GD.Tutar] public decimal? IslemTutariTl { get; set; }
    /// <summary>MASAK ŞİB (G) Açıklama: asgari 300, azami 50.000 karakter (Kuyumcu ŞİB Rehberi 2.0 §4.2.7).</summary>
    [Required,
     MinLength(300, ErrorMessage = "MASAK şüpheli işlem açıklaması en az 300 karakter olmalıdır (Rehber 2.0 §4.2.7)"),
     MaxLength(50_000, ErrorMessage = "Açıklama en fazla 50.000 karakter olabilir")]
    public string Gerekce { get; set; } = null!;
}

public sealed class GonderildiDto
{
    [Required, MaxLength(GS.GonderimReferansi)] public string GonderimReferansi { get; set; } = null!;
}

public sealed class KapatDto
{
    // PAKET GOC-B (envanter C satır 325): ekran 2000 / sunucu 1000 uyuşmuyordu ve en az 20 KIRPILMADAN sayılıyordu
    // (boşluklu gerekçe geçiyordu) → K30 GerekceKurali (kırpılmış 20–1000); servis yolunda GirdiKapisi.Gerekce.
    [Required, GD.Gerekce, MaxLength(GS.SupheliKapatma)]
    public string Gerekce { get; set; } = null!;
}

public sealed class MasakServisi(
    KuyumcuDbContext db, MasakKuralMotoru motor, DenetimServisi denetim,
    TakvimAyarlari? takvim = null)
{
    private const string Tespit = nameof(MasakKimlikTespiti);
    private const string Supheli = nameof(MasakSupheliBildirim);

    /// <summary>Son 4 hane hariç maskeler: 10000000146 → *******0146.</summary>
    public static string Maskele(string belgeNo) =>
        belgeNo.Length <= 4 ? new string('*', belgeNo.Length) : new string('*', belgeNo.Length - 4) + belgeNo[^4..];

    // ── Eşikler — kişisel veri yok, POS'un formu göstermek için bilmesi gerekir ──
    // PAKET YASAL-1: eşikler TARİHLİ tablodan; tarih verilmezse bugün (yerel gün).
    public object Esikler(DateOnly? tarih = null) => EsikYaniti(
        tarih is { } g ? motor.EsiklerGunu(g) : motor.Esikler);

    private static object EsikYaniti(MasakEsikleri e) => new
    {
        kimlikEsigi = e.KimlikEsigi,
        // MASAK-370 tur 3: kimliği FİİLEN isteyen en düşük tutar (md.5/1-b eşiği ile TEYİTSİZ 200.000 amaç beyanı
        // kademesinin küçüğü). Ekranlar formu bu tutarla önceden açar; 07.10.2026 sonrası 370.000 ≠ 200.000.
        kimlikIstenenEnDusukTutar = e.KimlikIstenenEnDusukTutar,
        amacBeyaniEsigi = e.AmacBeyaniEsigi,
        beyanFormuEsigi = e.BeyanFormuEsigi,
        ekBelgeEsigi = e.EkBelgeEsigi,
        asgariAciklamaUzunlugu = e.AsgariAciklamaUzunlugu,
        supheliBildirimIsGunu = e.SupheliBildirimIsGunu,
        saklamaYili = e.SaklamaYili,
        belgeTurleri = KimlikBelgeTuru.Hepsi,
    };

    /// <summary>Tutar için hangi kademe/alanların gerektiği — POS formu buna göre açılır.</summary>
    public MasakGereksinim Gereksinim(decimal tutarTl) => motor.Gereksinim(tutarTl);

    /// <summary>
    /// ZO-ISLEM-A 29.09 (tasarım hakemi KRİTİK — Hurda MASAK çıkmazı): müşteri (cari)
    /// verilirse kademe, kaydın kendisinin geçtiği kapıyla (<see cref="MasakKapisi.DogrulaKumulatif"/>)
    /// AYNI ölçüyle hesaplanır: aynı yerel günde aynı cariyle yapılmış MASAK'a tabi
    /// işlemler + bu işlem (<see cref="MasakKapisi.GunlukIslemToplam"/>, tek kaynak).
    /// Eskiden ekran yalnız bu işlemin tutarını soruyordu (o günkü eşikte 185.000 → YALNIZ_KIMLIK),
    /// kayıt ise kümülatifle (ör. 305.000 → beyan formu) reddediliyordu; amaç/açıklama/
    /// fon kaynağı alanları hiç çizilmediği için kullanıcı döngüde kalıyordu.
    /// Yanıt kademeyi VE (PAKET YASAL-PARAM-EKRAN) kararın ölçüsünü taşır: <c>OlcuTutarTl</c> = bu cariyle
    /// bugünkü bağlantılı toplam (ya da tek işlem tutarı), <c>IslemTutarTl</c> = bu işlemin tutarı — ekran
    /// başlığı gerçek gerekçeyi ("bu müşterinin bugünkü toplamı X") söyler. Toplam yalnız AYNI kiracının
    /// işlemlerinden hesaplanır (tezgâhtar bu satışları zaten görür; başka kiracının carisi için toplam 0).
    /// Peşin Müşteri kartı (sistem carisi) kümülatife girmez — tek işlem kuralı.
    /// </summary>
    public async Task<MasakGereksinim> Gereksinim(decimal tutarTl, string? cariId, string tenantId) =>
        (await GereksinimOlcuyle(tutarTl, cariId, tenantId)).Gerek;

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN (MASAK-370 tasarım hakemi ORTA): kademe + kararın dayandığı
    /// ÖLÇÜ (bağlantılı işlem toplamı = bugünkü önceki işlemler + bu işlem). Ekran başlığı
    /// "bu müşterinin bugünkü toplamı X" diye gerçek gerekçeyi bu değerden yazar.
    /// </summary>
    public async Task<(MasakGereksinim Gerek, decimal OlcuTl)> GereksinimOlcuyle(decimal tutarTl, string? cariId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(cariId)) return (motor.Gereksinim(tutarTl), Math.Abs(tutarTl));
        var onceki = await MasakKapisi.GunlukIslemToplam(db, tenantId, cariId.Trim(), DateTime.UtcNow);
        var olcu = Math.Abs(onceki) + Math.Abs(tutarTl);
        return (motor.Gereksinim(olcu), olcu);
    }

    // ── Kimlik tespitleri ────────────────────────────────────────────────────

    public async Task<object> TespitleriListele(
        AktifKullanici islem, DateTime? baslangic, DateTime? bitis, int limit)
    {
        limit = Math.Clamp(limit, 1, 500);
        var sorgu = db.MasakKimlikTespitleri.AsNoTracking().Where(m => m.TenantId == islem.TenantId);
        if (baslangic is not null) sorgu = sorgu.Where(m => m.Tarih >= baslangic);
        if (bitis is not null) sorgu = sorgu.Where(m => m.Tarih < bitis);

        var liste = await sorgu.OrderByDescending(m => m.Tarih).Take(limit).ToListAsync();
        await denetim.ListeOkumaKaydet(islem, Tespit, liste.Count, "kimlik tespitleri listesi");

        return liste.Select(m => new
        {
            m.Id, m.HareketId, m.KaynakTur, m.CariId, m.IslemTutariTl, m.BelgeTuru,
            belgeNo = Maskele(m.BelgeNo),      // liste: MASKELİ
            m.AdSoyad, m.Tarih, m.SaklamaBitis, m.TespitEdenKullaniciId,
        });
    }

    /// <summary>Tekil kayıt — tam belge no. Okuma izi ZORUNLU (KVKK md.12).</summary>
    public async Task<object> TespitGetir(string id, AktifKullanici islem)
    {
        var m = await db.MasakKimlikTespitleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kimlik tespiti bulunamadı");

        await denetim.OkumaKaydet(islem, Tespit, m.Id, "tam kimlik görüntülendi");

        var beyan = await db.MasakBeyanlari.AsNoTracking()
            .FirstOrDefaultAsync(b => b.KimlikTespitiId == m.Id && b.TenantId == islem.TenantId);

        return new
        {
            m.Id, m.HareketId, m.KaynakTur, m.CariId, m.IslemTutariTl, m.BelgeTuru, m.BelgeNo, m.AdSoyad,
            m.DogumTarihi, m.Uyruk, m.Adres, m.TespitEdenKullaniciId, m.Tarih, m.SaklamaBitis, m.DuzeltilenId,
            beyan = beyan is null ? null : new
            {
                beyan.Id, beyan.Kademe, beyan.IslemAmaci, beyan.Aciklama, beyan.FonKaynagi, beyan.EkBelgeReferansi, beyan.Tarih,
            },
        };
    }

    /// <summary>"Bu kişi daha önce işlem yaptı mı?" — hash ile arama; şifreli sütuna dokunulmaz.</summary>
    public async Task<object> BelgeNoIleAra(string belgeNo, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(belgeNo)) throw new BadRequestException("Belge no boş olamaz");
        var hash = db.Sifreleyici.Hashle(belgeNo);

        var liste = await db.MasakKimlikTespitleri.AsNoTracking()
            .Where(m => m.TenantId == islem.TenantId && m.BelgeNoHash == hash)
            .OrderByDescending(m => m.Tarih).Take(100).ToListAsync();

        await denetim.ListeOkumaKaydet(islem, Tespit, liste.Count, "belge no ile arama");

        return new
        {
            adet = liste.Count,
            toplamTutarTl = liste.Sum(m => m.IslemTutariTl),
            ilkIslem = liste.Count == 0 ? (DateTime?)null : liste.Min(m => m.Tarih),
            sonIslem = liste.Count == 0 ? (DateTime?)null : liste.Max(m => m.Tarih),
            // ⚠ UX-4d madde 18: arama kayıtları LİSTE UCUYLA AYNI alan kümesini
            // taşır. Eskiden 5 alan dönüyordu; istemci bunu liste satırı
            // (KimlikTespitSatir) sanıp ayrıştırmaya çalışıyor, JSON hatası
            // alıyor ve ekranda ESKİ liste kalıyordu — kullanıcı aramanın
            // sonucunu değil, bir öncekini görüyordu (UX-S26/UX-S28).
            kayitlar = liste.Select(m => new
            {
                m.Id, m.HareketId, m.KaynakTur, m.CariId, m.IslemTutariTl, m.BelgeTuru,
                belgeNo = Maskele(m.BelgeNo),
                m.AdSoyad, m.Tarih, m.SaklamaBitis, m.TespitEdenKullaniciId,
            }),
        };
    }

    // ── Şüpheli işlem bildirimi ──────────────────────────────────────────────

    // ⚠ ESKİDEN KODA GÖMÜLÜ SABİTTİ (300 / 50.000) — ADR-0005 ihlali.
    // Mevzuat rakamı yapılandırmadan okunur; rehber sürümü değişince kod değişmez.
    private int AsgariAciklama => motor.Esikler.SupheliGerekceAsgari;
    private int AzamiAciklama => motor.Esikler.SupheliGerekceAzami;

    private async Task<object> SupheliOlusturKayit(SupheliBildirimDto dto, AktifKullanici islem)
    {
        // PAKET GOC-B: işlem tutarı K9 servis yolunda da (DataAnnotations yalnız controller yolunda).
        GD.GirdiKapisi.Tutar(dto.IslemTutariTl, "islemTutariTl");
        var gerekce = dto.Gerekce?.Trim() ?? "";
        if (gerekce.Length < AsgariAciklama)
            throw new BadRequestException(
                $"MASAK açıklaması en az {AsgariAciklama} karakter olmalıdır (şu an {gerekce.Length}). Rehber 2.0 §4.2.7");
        if (gerekce.Length > AzamiAciklama)
            throw new BadRequestException($"MASAK açıklaması en fazla {AzamiAciklama:N0} karakter olabilir");

        if (dto.HareketId is not null)
        {
            var var_ = await db.Hareketler.AsNoTracking()
                .AnyAsync(h => h.Id == dto.HareketId && h.TenantId == islem.TenantId);
            if (!var_) throw new NotFoundException("Hareket bulunamadı");
        }
        if (dto.CariId is not null) await IslemDogrulayici.CariDogrula(db, dto.CariId, islem.TenantId);
        // Kimlik tespiti de DOĞRULANIR. Bu satır olmadan başka kiracının kimlik
        // tespiti, bu kiracının append-only bildirimine bağlanabiliyordu.
        await IslemDogrulayici.KimlikTespitiDogrula(db, dto.KimlikTespitiId, islem.TenantId);

        var now = DateTime.UtcNow;
        var kayit = new MasakSupheliBildirim
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            HareketId = dto.HareketId,
            CariId = dto.CariId,
            KimlikTespitiId = dto.KimlikTespitiId,
            IslemTutariTl = dto.IslemTutariTl,
            Gerekce = gerekce,
            TespitTarihi = now,
            // 10 İŞ GÜNÜ: hafta sonu + RESMÎ TATİLLER hariç. Tatiller atlanmazsa
            // süre olduğundan erken dolardı (ADIM-A §5/2 açığı).
            SonBildirimTarihi = motor.SonBildirimTarihi(
                now, ResmiTatiller.Aralik(Zaman.UtcdenYerele(now).Year, 2, takvim)),
            Durum = SupheliDurum.Acik,
            BildirenKullaniciId = islem.Id,
            SaklamaBitis = motor.SaklamaBitisi(now),
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.MasakSupheliBildirimler.Add(kayit);
        return kayit;
    }

    /// <summary>Tek yazma yolu: hem yeni bildirim hem ek beyan buradan doğar.</summary>
    public async Task<object> SupheliOlustur(SupheliBildirimDto dto, AktifKullanici islem)
    {
        var kayit = (MasakSupheliBildirim)await SupheliOlusturKayit(dto, islem);
        await db.SaveChangesAsync();
        return Sekillendir(kayit);
    }

    public async Task<object> SupheliListele(AktifKullanici islem, string? durum)
    {
        var sorgu = db.MasakSupheliBildirimler.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        if (!string.IsNullOrEmpty(durum))
        {
            if (!SupheliDurum.Hepsi.Contains(durum)) throw new BadRequestException($"Geçersiz durum: {durum}");
            sorgu = sorgu.Where(s => s.Durum == durum);
        }
        var liste = await sorgu.OrderBy(s => s.Durum == SupheliDurum.Acik ? 0 : 1)
            .ThenBy(s => s.SonBildirimTarihi).Take(500).ToListAsync();

        await denetim.ListeOkumaKaydet(islem, Supheli, liste.Count, "şüpheli bildirim listesi");
        return liste.Select(Sekillendir);
    }

    public async Task<object> SupheliGonderildi(string id, GonderildiDto dto, AktifKullanici islem)
    {
        var s = await SupheliBul(id, islem);
        if (s.Durum != SupheliDurum.Acik)
            throw new BadRequestException($"Bildirim {s.Durum} durumunda; yalnız AÇIK bildirim gönderildi işaretlenir");
        s.Durum = SupheliDurum.Gonderildi;
        s.GonderimTarihi = DateTime.UtcNow;
        s.GonderimReferansi = dto.GonderimReferansi.Trim();
        s.Guncellendi = DateTime.UtcNow;
        // UX-4d madde 10 / Mühür 2 DoD: durum geçişi denetim izine GEREKÇELİ düşer.
        denetim.YazmaKaydet(islem, Supheli, s.Id,
            $"şüpheli bildirim GONDERILDI (referans: {s.GonderimReferansi})");
        await db.SaveChangesAsync();
        return Sekillendir(s);
    }

    /// <summary>Şüphe kalktı — kayıt SİLİNMEZ, gerekçesiyle kapatılır (8 yıl saklama).</summary>
    public async Task<object> SupheliKapat(string id, KapatDto dto, AktifKullanici islem)
    {
        var s = await SupheliBul(id, islem);
        if (s.Durum == SupheliDurum.Kapatildi) throw new BadRequestException("Bildirim zaten kapatılmış");
        // PAKET GOC-B: K30 kırpılmış 20–1000 (servis yolu; boşluklu "gerekçe" artık 400).
        var kapatmaGerekcesi = GD.GirdiKapisi.Gerekce(dto.Gerekce, "gerekce", GS.SupheliKapatma);
        s.Durum = SupheliDurum.Kapatildi;
        s.KapatmaGerekcesi = kapatmaGerekcesi;
        s.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, Supheli, s.Id,
            $"şüpheli bildirim KAPATILDI ({s.KapatmaGerekcesi.Length} karakter gerekçe)");
        await db.SaveChangesAsync();
        return Sekillendir(s);
    }

    /// <summary>
    /// PAKET P-13 (B-12) — GÖNDERİLMİŞ bildirimin DÜZELTME YOLU: EK BEYAN.
    /// <para>
    /// Sorun: GONDERILDI durumundaki bir kayıtta ekranda hiçbir eylem yoktu ve
    /// "kapat + yeni bildirim" yazan ipucu yanlıştı — kapatma yalnız AÇIK
    /// bildirimde anlamlıdır, gönderilmiş bildirim kapatılarak geri alınamaz.
    /// </para>
    /// <para>
    /// Çözüm: aslı DEĞİŞMEDEN kalır (append-only), düzeltme yeni ve BAĞLI bir
    /// bildirim olarak doğar. Zincir denetimde okunabilir: hangi bildirimin
    /// hangi ek beyanla düzeltildiği <c>duzeltilen_bildirim_id</c> ile bellidir.
    /// </para>
    /// </summary>
    public async Task<object> SupheliEkBeyan(string id, SupheliBildirimDto dto, AktifKullanici islem)
    {
        var asil = await SupheliBul(id, islem);
        if (asil.Durum != SupheliDurum.Gonderildi)
            throw new BadRequestException(
                $"Ek beyan yalnız MASAK'a GÖNDERİLMİŞ bildirim için düzenlenir; bu bildirim {asil.Durum} durumunda. "
                + "Açık bildirimde metni düzeltmek için bildirimi gerekçesiyle kapatın ve yenisini düzenleyin.");
        if (asil.DuzeltilenBildirimId is not null)
            throw new BadRequestException(
                "Bu kayıt zaten bir ek beyandır; ek beyanın ek beyanı düzenlenmez — "
                + "düzeltme asıl bildirime bağlanır.");

        // Ek beyan, aslın bağlarını DEVRALIR (gövdede gelmediyse): aynı işlem ve
        // aynı müşteri için ikinci bir bildirim, bağları kopuk doğmamalıdır.
        dto.HareketId ??= asil.HareketId;
        dto.CariId ??= asil.CariId;
        dto.KimlikTespitiId ??= asil.KimlikTespitiId;
        dto.IslemTutariTl ??= asil.IslemTutariTl;

        var yeni = (MasakSupheliBildirim)await SupheliOlusturKayit(dto, islem);
        yeni.DuzeltilenBildirimId = asil.Id;
        denetim.YazmaKaydet(islem, Supheli, yeni.Id,
            $"ek beyan düzenlendi (düzeltilen bildirim: {asil.Id}, referans: {asil.GonderimReferansi})");
        await db.SaveChangesAsync();
        return Sekillendir(yeni);
    }

    /// <summary>Panel kartı: açık bildirim, süresi 2 iş günü içinde dolan, süresi geçmiş.</summary>
    public async Task<object> Ozet(AktifKullanici islem)
    {
        var now = DateTime.UtcNow;
        var acik = await db.MasakSupheliBildirimler.AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.Durum == SupheliDurum.Acik).ToListAsync();

        return new
        {
            acikBildirim = acik.Count,
            suresiGecmis = acik.Count(s => s.SonBildirimTarihi < now),
            yaklasan = acik.Count(s => s.SonBildirimTarihi >= now && s.SonBildirimTarihi < now.AddDays(3)),
            son30GunTespit = await db.MasakKimlikTespitleri.AsNoTracking()
                .CountAsync(m => m.TenantId == islem.TenantId && m.Tarih >= now.AddDays(-30)),
        };
    }

    private async Task<MasakSupheliBildirim> SupheliBul(string id, AktifKullanici islem) =>
        await db.MasakSupheliBildirimler.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == islem.TenantId)
        ?? throw new NotFoundException("Şüpheli işlem bildirimi bulunamadı");

    private static object Sekillendir(MasakSupheliBildirim s) => new
    {
        s.Id, s.HareketId, s.CariId, s.KimlikTespitiId, s.IslemTutariTl, s.Gerekce,
        s.TespitTarihi, s.SonBildirimTarihi,
        kalanSaat = Math.Round((s.SonBildirimTarihi - DateTime.UtcNow).TotalHours, 1),
        s.Durum, s.GonderimTarihi, s.GonderimReferansi, s.KapatmaGerekcesi,
        s.DuzeltilenBildirimId,
        s.BildirenKullaniciId, s.SaklamaBitis, s.Olusturuldu, s.Guncellendi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/masak")]
public sealed class MasakController(MasakServisi servis, SaklamaSuresiIsi saklama, RiskServisi risk) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/masak/esikler — kişisel veri yok; POS formu için TÜM rollere açık</summary>
    [HttpGet("esikler")]
    [HerkeseAcik("Yalnız MEVZUAT eşikleri — kişisel veri yok; satış ekranı (TEZGÂHTAR dâhil) MASAK formunu bu yanıta göre açar")]
    public object Esikler([FromQuery] DateOnly? tarih) => servis.Esikler(tarih);

    /// <summary>GET /api/v1/masak/gereksinim?tutar= — bu tutar için hangi alanlar gerekli (POS formu)</summary>
    [HttpGet("gereksinim")]
    [HerkeseAcik("Tutara göre hangi alanın gerektiğini söyler — kişisel veri okumaz, satış ekranında TEZGÂHTAR da çağırır")]
    public async Task<MasakGereksinimYaniti> Gereksinim([FromQuery] decimal tutar, [FromQuery] string? cariId = null)
    {
        // ZO-ISLEM-A 29.09: cari verilirse bağlantılı işlem toplamıyla. YASAL-PARAM-EKRAN: ölçü de döner.
        var (g, olcu) = await servis.GereksinimOlcuyle(tutar, cariId, Islem.TenantId);
        return new(g.Kademe, g.KimlikGerekli, g.AmacGerekli, g.BeyanFormuGerekli, g.EkBelgeGerekli,
            g.AsgariAciklamaUzunlugu, olcu, Math.Abs(tutar));
    }

    /// <summary>
    /// GET /api/v1/masak/saklama-onizleme — süresi dolmuş kişisel veri SAYISI.
    /// Hiçbir şey değiştirmez. Yalnız SAHİP: geri döndürülemez bir işlemin önizlemesi.
    /// </summary>
    [HttpGet("saklama-onizleme")]
    [RolGerekli("SAHIP")]
    public Task<object> SaklamaOnizleme() => saklama.Onizleme(Islem);

    /// <summary>
    /// POST /api/v1/masak/saklama-uygula — 8 yılı dolmuş kayıtların KİMLİK
    /// alanlarını maskeler (tutar/tarih/kademe korunur). GERİ ALINAMAZ.
    /// Bilinçli olarak zamanlayıcıya bağlanmadı; kuyumcu onaylayarak çalıştırır.
    /// </summary>
    [HttpPost("saklama-uygula")]
    [RolGerekli("SAHIP")]
    public Task<object> SaklamaUygula() => saklama.Uygula(Islem);

    /// <summary>GET /api/v1/masak/kimlik-tespitleri — MASKELİ liste — MUHASEBE+ (TEZGAHTAR yok)</summary>
    [HttpGet("kimlik-tespitleri")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Tespitler([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, [FromQuery] int limit = 100) =>
        servis.TespitleriListele(Islem, baslangic, bitis, limit);

    /// <summary>GET /api/v1/masak/kimlik-tespitleri/ara?belgeNo= — hash ile arama — MUHASEBE+</summary>
    [HttpGet("kimlik-tespitleri/ara")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Ara([FromQuery] string belgeNo) => servis.BelgeNoIleAra(belgeNo, Islem);

    /// <summary>GET /api/v1/masak/kimlik-tespitleri/{id} — TAM kimlik, okuma izi düşer — MUHASEBE+</summary>
    [HttpGet("kimlik-tespitleri/{id}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Tespit(string id) => servis.TespitGetir(id, Islem);

    /// <summary>POST /api/v1/masak/supheli — şüpheli işlem işaretle (10 iş günü sayacı başlar) — MUHASEBE+</summary>
    [HttpPost("supheli")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> SupheliOlustur([FromBody] SupheliBildirimDto dto) =>
        StatusCode(201, await servis.SupheliOlustur(dto, Islem));

    /// <summary>
    /// POST /api/v1/masak/supheli/{id}/ek-beyan — GÖNDERİLMİŞ bildirimin
    /// düzeltmesi (PAKET P-13 · B-12) — MUHASEBE+
    /// </summary>
    [HttpPost("supheli/{id}/ek-beyan")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<IActionResult> SupheliEkBeyan(string id, [FromBody] SupheliBildirimDto dto) =>
        StatusCode(201, await servis.SupheliEkBeyan(id, dto, Islem));

    /// <summary>GET /api/v1/masak/supheli?durum= — MUHASEBE+</summary>
    [HttpGet("supheli")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Supheliler([FromQuery] string? durum) => servis.SupheliListele(Islem, durum);

    /// <summary>PATCH /api/v1/masak/supheli/{id}/gonderildi — MASAK'a gönderildi, referans no — MUDUR+</summary>
    [HttpPatch("supheli/{id}/gonderildi")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Gonderildi(string id, [FromBody] GonderildiDto dto) => servis.SupheliGonderildi(id, dto, Islem);

    /// <summary>PATCH /api/v1/masak/supheli/{id}/kapat — şüphe kalktı, gerekçeyle kapat (silinmez) — MUDUR+</summary>
    [HttpPatch("supheli/{id}/kapat")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Kapat(string id, [FromBody] KapatDto dto) => servis.SupheliKapat(id, dto, Islem);

    /// <summary>GET /api/v1/masak/ozet — panel kartı (sayılar, kişisel veri yok) — MUHASEBE+</summary>
    [HttpGet("ozet")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> Ozet() => servis.Ozet(Islem);

    // ── PAKET 40-A — RİSK SKORLAMA ───────────────────────────────────────────
    // ⚠ Bu uçların HİÇBİRİ bir işlemi durdurmaz. Risk skoru satış/MASAK/tevsik
    //   kapısına BAĞLANMAZ: mevzuatta doğrulanmış "yüksek riskliyle işlem
    //   yasaktır" hükmü yoktur (Anayasa md.1 — uydurulmaz).
    // ⚠ Rota sırası: "risk/ozet" SABİT segmenttir ve "risk/{cariId}" ile
    //   çakışmaması için ONDAN ÖNCE yazılır.

    /// <summary>GET /api/v1/masak/risk/ozet — seviye dağılımı + yüksek riskli cari listesi — MUHASEBE+</summary>
    [HttpGet("risk/ozet")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> RiskOzet() => risk.Ozet(Islem);

    /// <summary>GET /api/v1/masak/risk/{cariId} — güncel profil + son 10 satır (okuma izi düşer) — MUHASEBE+</summary>
    [HttpGet("risk/{cariId}")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> RiskGetir(string cariId) => risk.Getir(cariId, Islem);

    /// <summary>POST /api/v1/masak/risk/{cariId}/hesapla — YENİ profil satırı yazar (güncelleme değil) — MUHASEBE+</summary>
    [HttpPost("risk/{cariId}/hesapla")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> RiskHesapla(string cariId) => risk.Hesapla(cariId, Islem);

    /// <summary>POST /api/v1/masak/risk/{cariId}/elle — gerekçeli seviye ezmesi (denetim izine yazma kaydı) — MUDUR+</summary>
    [HttpPost("risk/{cariId}/elle")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> RiskElle(string cariId, [FromBody] RiskElleDto dto) => risk.Elle(cariId, dto, Islem);
}

/// <summary>
/// PAKET YASAL-PARAM-EKRAN — GET /api/v1/masak/gereksinim yanıtı: motor kademesi + kararın
/// dayandığı ölçü. <c>OlcuTutarTl</c> &gt; <c>IslemTutarTl</c> ise karar bağlantılı işlem
/// toplamından (aynı gün aynı müşteri) gelmiştir. Kişisel veri yok; yalnız tutar.
/// </summary>
public sealed record MasakGereksinimYaniti(
    string Kademe, bool KimlikGerekli, bool AmacGerekli, bool BeyanFormuGerekli, bool EkBelgeGerekli,
    int AsgariAciklamaUzunlugu, decimal OlcuTutarTl, decimal IslemTutarTl);
