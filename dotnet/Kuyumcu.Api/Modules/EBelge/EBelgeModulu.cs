using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.EBelge;

// ═════════════════════════════════════════════════════════════════════════════
// E-2/E-4/E-5/E-6/E-8 (20.10, kurucu: "satış fişini kaydetsin, arka planda o esnada fatura göndermeye
// başlamamız lazım") — giden kutusu.
//   • Kuyruğa alma: işletmenin bağlantısı açıkken, bağlantı tarihinden SONRA kesilen satış faturası
//     (KESILDI/ODENDI) kendiliğinden kuyruğa girer; geçmiş faturalar gönderilmez.
//   • Gönderim: alıcı e-Fatura'ya kayıtlıysa e-Fatura (TEMEL), değilse e-Arşiv (E-4 otomatik yönlendirme).
//   • Geçici hata (ağ, 5xx, 429) → artan aralıkla yeniden denenir (en çok 12); kalıcı hata (eksik bilgi,
//     doğrulama) → HATA, sebep sade dille; düzeltilince "Yeniden dene".
//   • Durum izleme: GİB sonucu (KABUL / HATA / RET) entegratörden okunur.
//   • İptal (E-6): fatura iptalinde e-Arşiv belge iletimden 8 gün içinde entegratörden iptal edilir; e-Fatura
//     ve 8 günü geçmiş belge iptal EDİLMEZ (iade faturası gerekir) — fatura da iptal olmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EBelgeServisi(KuyumcuDbContext db, IAlanSifreleyici sifreleyici, IHttpClientFactory httpFabrika,
    DenetimServisi denetim, ILogger<EBelgeServisi> gunluk)
{
    public const string HttpAdi = "ebelge";
    public const int EnCokDeneme = 12;
    public static readonly TimeSpan IptalSuresi = TimeSpan.FromDays(8);

    // ── Ayar ─────────────────────────────────────────────────────────────────

    public async Task<EBelgeAyarOzet> AyarGetir(AktifKullanici islem)
    {
        var a = await db.EBelgeAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId);
        return Ozet(a);
    }

    public async Task<EBelgeAyarKayitSonucu> AyarKaydet(EBelgeAyarDto dto, AktifKullanici islem)
    {
        var kod = dto.Saglayici?.Trim();
        if (EBelgeSaglayicilari.Bul(kod) is null) throw new BadRequestException("Entegratör seçin").AlanIle("saglayici");
        var ortam = dto.Ortam is "CANLI" ? "CANLI" : dto.Ortam is "TEST" or null ? "TEST" : throw new BadRequestException("Ortam TEST ya da CANLI olmalıdır").AlanIle("ortam");
        var seri = string.IsNullOrWhiteSpace(dto.SeriOnEki) ? null : dto.SeriOnEki.Trim().ToUpperInvariant();
        if (seri is not null && (seri.Length != Kuyumcu.Core.Domain.Girdi.AlanSinirlari.EBelgeSeriOnEki || !seri.All(c => c is >= 'A' and <= 'Z')))
            throw new BadRequestException("Seri ön eki 3 büyük harf olmalıdır (ör. MHK)").AlanIle("seriOnEki");
        var anahtar = string.IsNullOrWhiteSpace(dto.Anahtar) ? null : dto.Anahtar.Trim();
        if (anahtar is { Length: > Kuyumcu.Core.Domain.Girdi.AlanSinirlari.EBelgeAnahtari }) throw new BadRequestException("API anahtarı en çok 1000 karakter olabilir").AlanIle("anahtar");

        var a = await db.EBelgeAyarlari.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId);
        var simdi = DateTime.UtcNow;
        if (a is null)
        {
            if (anahtar is null) throw new BadRequestException("API anahtarı zorunludur").AlanIle("anahtar");
            a = new EBelgeAyari { Id = Kimlik.Ulid(), TenantId = islem.TenantId, Olusturuldu = simdi, Baslangic = simdi };
            db.EBelgeAyarlari.Add(a);
        }
        else if (anahtar is null && a.Saglayici != kod) throw new BadRequestException("API anahtarı zorunludur").AlanIle("anahtar");
        // Kapalıyken açılırsa başlangıç ŞİMDİ olur: kapalı dönemde kesilen faturalar kendiliğinden gönderilmez.
        if (dto.Etkin && !a.Etkin) a.Baslangic = simdi;
        a.Saglayici = kod!; a.Ortam = ortam; a.SeriOnEki = seri; a.Etkin = dto.Etkin;
        if (anahtar is not null) a.AnahtarSifreli = sifreleyici.Sifrele(anahtar);
        a.Guncellendi = simdi;
        denetim.YazmaKaydet(islem, "EBelgeAyari", a.Id, $"e-belge bağlantısı: {kod} · {ortam} · {(dto.Etkin ? "açık" : "kapalı")}", hassas: true);
        await db.SaveChangesAsync();
        var hata = await Dene(a);
        return new(Ozet(a), hata is null, hata);
    }

    /// <summary>Bağlantı denemesi: işletmenin kendi VKN'si mükellef sorgusuna sorulur (anahtar + erişim doğrulanır).</summary>
    private async Task<string?> Dene(EBelgeAyari a)
    {
        string? hata = null;
        try
        {
            var vkn = await db.Tenants.AsNoTracking().Where(t => t.Id == a.TenantId).Select(t => t.VergiNo).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(vkn)) hata = "İşletmenin vergi numarası eksik; bağlantı denenemedi. İşletme ayarlarına yazın.";
            else await EBelgeSaglayicilari.Bul(a.Saglayici)!.MukellefSorgula(Http(), Baglam(a), vkn.Trim(), default);
        }
        catch (EBelgeHatasi ex) { hata = ex.Message; }
        a.SonDeneme = DateTime.UtcNow; a.SonHata = hata;
        await db.SaveChangesAsync();
        return hata;
    }

    public async Task<EBelgeAyarOzet> AyarKaldir(AktifKullanici islem)
    {
        var a = await db.EBelgeAyarlari.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("e-Fatura bağlantısı yok");
        db.EBelgeAyarlari.Remove(a);   // anahtar geride kalmaz; giden kutusu kayıtları durur
        denetim.YazmaKaydet(islem, "EBelgeAyari", a.Id, "e-belge bağlantısı kaldırıldı", hassas: true);
        await db.SaveChangesAsync();
        return Ozet(null);
    }

    private static EBelgeAyarOzet Ozet(EBelgeAyari? a) => new(
        a?.Saglayici, a?.Ortam, a?.SeriOnEki, a?.Etkin ?? false, a is not null, a?.Baslangic, a?.SonDeneme, a?.SonHata,
        EBelgeSaglayicilari.Tumu.Select(s => new EBelgeSaglayiciOzet(s.Kod, s.Ad)).ToList());

    private EBelgeBaglami Baglam(EBelgeAyari a) => new(sifreleyici.Coz(a.AnahtarSifreli), a.Ortam);
    private HttpClient Http() => httpFabrika.CreateClient(HttpAdi);

    // ── Giden kutusu ─────────────────────────────────────────────────────────

    public async Task<List<EBelgeGonderimOzet>> Liste(AktifKullanici islem, int limit = 200) =>
        (await db.EBelgeGonderimleri.AsNoTracking().Where(g => g.TenantId == islem.TenantId)
            .OrderByDescending(g => g.Olusturuldu).Take(Math.Clamp(limit, 1, 500)).ToListAsync())
        .Select(GOzet).ToList();

    public async Task<EBelgeGonderimOzet?> FaturaDurumu(string faturaId, AktifKullanici islem)
    {
        var g = await db.EBelgeGonderimleri.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.FaturaId == faturaId);
        return g is null ? null : GOzet(g);
    }

    private static EBelgeGonderimOzet GOzet(EBelgeGonderimi g) => new(
        g.FaturaId, g.Durum, g.Senaryo, g.BelgeNo, g.SonHata, g.DenemeSayisi, g.Gonderildi, g.Sonuclandi,
        g.Gonderildi is { } t && g.Senaryo == EBelgeSenaryolari.EArsiv && g.Durum is EBelgeDurumlari.Gonderildi or EBelgeDurumlari.Kabul
            && DateTime.UtcNow - t <= IptalSuresi);

    /// <summary>Kalıcı hatadan sonra (bilgi düzeltildi) ya da elle: kuyruğa geri al ve HEMEN gönder.</summary>
    public async Task<EBelgeGonderimOzet> YenidenDene(string faturaId, AktifKullanici islem)
    {
        var g = await db.EBelgeGonderimleri.FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.FaturaId == faturaId)
            ?? throw new NotFoundException("Bu faturanın e-belge kaydı yok");
        if (g.Durum is not (EBelgeDurumlari.Hata or EBelgeDurumlari.Kuyrukta))
            throw new ConflictException("Bu belge zaten gönderildi; yeniden gönderilmez");
        g.Durum = EBelgeDurumlari.Kuyrukta; g.DenemeSayisi = 0; g.SonrakiDeneme = DateTime.UtcNow; g.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await Isle(g, default);
        return GOzet(g);
    }

    public async Task<(byte[] Pdf, string Ad)> Pdf(string faturaId, AktifKullanici islem)
    {
        var g = await db.EBelgeGonderimleri.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.FaturaId == faturaId)
            ?? throw new NotFoundException("Bu faturanın e-belge kaydı yok");
        if (g.Senaryo is null || g.Gonderildi is null) throw new ConflictException("Belge henüz entegratöre ulaşmadı");
        var a = await db.EBelgeAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == islem.TenantId)
            ?? throw new ConflictException("e-Fatura bağlantısı yok");
        try { return (await EBelgeSaglayicilari.Bul(g.Saglayici)!.Pdf(Http(), Baglam(a), g.Senaryo, g.Ettn, default), $"{g.BelgeNo ?? g.Ettn.ToString()}.pdf"); }
        catch (EBelgeHatasi ex) { throw new ConflictException(ex.Message); }
    }

    /// <summary>Arka plan turu (kiracıya SABİT kapsamda): yeni faturaları kuyruğa al, zamanı gelenleri işle.</summary>
    public async Task Tur(string tenantId, CancellationToken iptal)
    {
        var a = await db.EBelgeAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId, iptal);
        if (a is null) return;
        if (a.Etkin)
        {
            var mevcut = db.EBelgeGonderimleri.Where(g => g.TenantId == tenantId).Select(g => g.FaturaId);
            var yeni = await db.Faturalar.AsNoTracking()
                .Where(f => f.TenantId == tenantId && f.Tur == "SATIS" && (f.Durum == "KESILDI" || f.Durum == "ODENDI")
                         && f.Olusturuldu >= a.Baslangic && !mevcut.Contains(f.Id))
                .OrderBy(f => f.Olusturuldu).Select(f => f.Id).Take(50).ToListAsync(iptal);
            var simdi = DateTime.UtcNow;
            foreach (var id in yeni)
                db.EBelgeGonderimleri.Add(new EBelgeGonderimi
                {
                    Id = Kimlik.Ulid(), TenantId = tenantId, FaturaId = id, Ettn = Guid.NewGuid(), Saglayici = a.Saglayici,
                    Durum = EBelgeDurumlari.Kuyrukta, SonrakiDeneme = simdi, Olusturuldu = simdi, Guncellendi = simdi,
                });
            if (yeni.Count > 0)
            {
                try { await db.SaveChangesAsync(iptal); }
                catch (DbUpdateException) { db.ChangeTracker.Clear(); return; }   // eşzamanlı tur aynı faturayı aldı (benzersiz FaturaId)
            }
        }
        var simdi2 = DateTime.UtcNow;
        var islenecek = await db.EBelgeGonderimleri
            .Where(g => g.TenantId == tenantId && (g.Durum == EBelgeDurumlari.Kuyrukta || g.Durum == EBelgeDurumlari.Gonderildi)
                     && (g.SonrakiDeneme == null || g.SonrakiDeneme <= simdi2))
            .OrderBy(g => g.SonrakiDeneme).Take(20).ToListAsync(iptal);
        foreach (var g in islenecek) await Isle(g, iptal);
    }

    /// <summary>Tek gönderim adımı: KUYRUKTA → gönder; GONDERILDI → GİB sonucunu sor.</summary>
    public async Task Isle(EBelgeGonderimi g, CancellationToken iptal)
    {
        var a = await db.EBelgeAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == g.TenantId, iptal);
        if (a is null) return;
        var saglayici = EBelgeSaglayicilari.Bul(g.Saglayici);
        if (saglayici is null) { Bitir(g, EBelgeDurumlari.Hata, "Bu entegratör artık desteklenmiyor."); await db.SaveChangesAsync(iptal); return; }
        var http = Http();
        var b = Baglam(a);
        var simdi = DateTime.UtcNow;
        try
        {
            if (g.Durum == EBelgeDurumlari.Kuyrukta)
            {
                if (!a.Etkin) return;
                var f = await db.Faturalar.AsNoTracking().FirstAsync(x => x.Id == g.FaturaId && x.TenantId == g.TenantId, iptal);
                if (f.Durum == "IPTAL") { Bitir(g, EBelgeDurumlari.Iptal, null); await db.SaveChangesAsync(iptal); return; }
                if (string.IsNullOrWhiteSpace(f.AliciVergiNo))
                    throw new EBelgeHatasi("Müşterinin TC ya da vergi numarası yok. Müşteri kartına yazın, sonra yeniden deneyin.", false);
                // E-4: alıcı kayıtlı kullanıcıysa e-Fatura, değilse e-Arşiv. Sorgu her gönderim denemesinde yapılır.
                var m = await saglayici.MukellefSorgula(http, b, f.AliciVergiNo.Trim(), iptal);
                var senaryo = m.Kayitli ? EBelgeSenaryolari.EFatura : EBelgeSenaryolari.EArsiv;
                var model = await EBelgeKurucu.Kur(db, f, g.Ettn, senaryo, m.Etiket, a.SeriOnEki, iptal);
                g.Senaryo = senaryo;
                g.BelgeNo = await saglayici.Gonder(http, b, model, iptal) ?? g.BelgeNo;
                g.Durum = EBelgeDurumlari.Gonderildi; g.Gonderildi ??= simdi; g.SonHata = null; g.DenemeSayisi = 0;
                g.SonrakiDeneme = simdi.AddSeconds(30);
            }
            else if (g.Durum == EBelgeDurumlari.Gonderildi && g.Senaryo is not null)
            {
                var d = await saglayici.Durum(http, b, g.Senaryo, g.Ettn, iptal);
                if (d.Durum == EBelgeDurumlari.Gonderildi)
                {
                    g.DenemeSayisi++;
                    // GİB sonucu 3 gün gelmezse izleme durur, kullanıcı görür (sessizce "gönderildi" sayılmaz).
                    if (g.Gonderildi is { } gt && simdi - gt > TimeSpan.FromDays(3))
                        Bitir(g, EBelgeDurumlari.Hata, "3 gündür GİB sonucu gelmedi; entegratör panelinden denetleyin.");
                    else g.SonrakiDeneme = simdi.Add(Aralik(g.DenemeSayisi));
                }
                else Bitir(g, d.Durum, d.Aciklama);
            }
        }
        catch (EBelgeHatasi ex) when (ex.Gecici)
        {
            g.DenemeSayisi++; g.SonHata = Kisalt(ex.Message);
            if (g.Durum == EBelgeDurumlari.Kuyrukta && g.DenemeSayisi >= EnCokDeneme) Bitir(g, EBelgeDurumlari.Hata, ex.Message);
            else g.SonrakiDeneme = simdi.Add(Aralik(g.DenemeSayisi));
        }
        catch (EBelgeHatasi ex)
        {
            if (g.Durum == EBelgeDurumlari.Kuyrukta) Bitir(g, EBelgeDurumlari.Hata, ex.Message);
            else { g.SonHata = Kisalt(ex.Message); g.DenemeSayisi++; g.SonrakiDeneme = simdi.Add(Aralik(g.DenemeSayisi)); }
        }
        // Anahtar ve belge içeriği günlüğe yazılmaz; yalnız durum.
        gunluk.LogInformation("[{TenantId}] e-belge {FaturaId}: {Durum}", g.TenantId, g.FaturaId, g.Durum);
        g.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync(iptal);
    }

    /// <summary>1, 2, 5, 10, 20, 30, 60 dk… (en çok 2 saat).</summary>
    public static TimeSpan Aralik(int deneme) => TimeSpan.FromMinutes(deneme switch
    {
        <= 1 => 1, 2 => 2, 3 => 5, 4 => 10, 5 => 20, 6 => 30, 7 => 60, _ => 120,
    });

    private static void Bitir(EBelgeGonderimi g, string durum, string? aciklama)
    {
        g.Durum = durum; g.SonHata = aciklama is null ? null : Kisalt(aciklama); g.SonrakiDeneme = null;
        if (durum is not EBelgeDurumlari.Hata) g.Sonuclandi = DateTime.UtcNow;
    }

    private static string Kisalt(string s) => s.Length > 500 ? s[..500] : s;

    // ── E-6: fatura iptal kapısı (FaturaServisi.DurumGuncelle çağırır) ──────────

    /// <summary>
    /// Fatura IPTAL'e geçmeden ÖNCE: GİB'e ulaşmamış belge yalnız işaretlenir; e-Arşiv 8 gün içinde
    /// entegratörden iptal edilir; e-Fatura ve süresi geçmiş belge için iptal REDDEDİLİR (fatura iptal olmaz).
    /// </summary>
    public async Task FaturaIptalOncesi(Data.Fatura f)
    {
        var g = await db.EBelgeGonderimleri.FirstOrDefaultAsync(x => x.TenantId == f.TenantId && x.FaturaId == f.Id);
        if (g is null || g.Durum is EBelgeDurumlari.Iptal) return;
        if (g.Durum is EBelgeDurumlari.Kuyrukta or EBelgeDurumlari.Hata or EBelgeDurumlari.Ret)
        {
            Bitir(g, EBelgeDurumlari.Iptal, null); g.Guncellendi = DateTime.UtcNow;
            return;
        }
        if (g.Senaryo == EBelgeSenaryolari.EFatura)
            throw new ConflictException("e-Fatura olarak gönderilmiş fatura buradan iptal edilemez; iade faturası kesin").KodIle("EBELGE_IPTAL_YOK");
        if (g.Gonderildi is { } t && DateTime.UtcNow - t > IptalSuresi)
            throw new ConflictException("e-Arşiv faturanın 8 günlük iptal süresi geçti; iade faturası kesin").KodIle("EBELGE_IPTAL_SURESI");
        var a = await db.EBelgeAyarlari.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == f.TenantId)
            ?? throw new ConflictException("e-Fatura bağlantısı yok; gönderilmiş belge iptal edilemedi");
        try { await EBelgeSaglayicilari.Bul(g.Saglayici)!.Iptal(Http(), Baglam(a), g.Senaryo!, g.Ettn, default); }
        catch (EBelgeHatasi ex) { throw new ConflictException(ex.Message).KodIle("EBELGE_IPTAL_HATA"); }
        Bitir(g, EBelgeDurumlari.Iptal, null); g.Guncellendi = DateTime.UtcNow;
    }
}

/// <summary>Arka plan gönderici — e-belge bağlantısı olan kiracılar (EBelge:IsciEtkin, EBelge:TurSaniye; varsayılan 5 sn).</summary>
public sealed class EBelgeIscisi(SistemKapsami kapsam, ILogger<EBelgeIscisi> gunluk, IConfiguration yapilandirma) : BackgroundService
{
    private readonly bool etkin = yapilandirma.GetValue("EBelge:IsciEtkin", true);
    private readonly TimeSpan aralik = TimeSpan.FromSeconds(Math.Clamp(yapilandirma.GetValue("EBelge:TurSaniye", 5), 2, 600));

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        if (!etkin) { gunluk.LogInformation("e-belge göndericisi KAPALI (EBelge:IsciEtkin=false)"); return; }
        while (!durdur.IsCancellationRequested)
        {
            try
            {
                // SistemKapsami izinli kullanım 16: e-belge bağlantısı olan kiracı listesi.
                List<string> kiracilar;
                await using (var sistem = kapsam.Sistem())
                {
                    var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
                    kiracilar = await db.EBelgeAyarlari.AsNoTracking().Select(a => a.TenantId).Distinct().Take(10_000).ToListAsync(durdur);
                }
                foreach (var k in kiracilar)
                {
                    // SistemKapsami izinli kullanım 17: kiracı başına gönderim — AYRI kapsam, kiracıya SABİT.
                    await using var kk = kapsam.Kiraci(k);
                    try { await kk.ServiceProvider.GetRequiredService<EBelgeServisi>().Tur(k, durdur); }
                    catch (OperationCanceledException) when (durdur.IsCancellationRequested) { throw; }
                    catch (Exception ex) { gunluk.LogError(ex, "Kiracı e-belge turu hatası"); }   // bir kiracı ötekini düşürmez
                }
            }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex) { gunluk.LogError(ex, "e-belge turu hatası"); }
            try { await Task.Delay(aralik, durdur); } catch (OperationCanceledException) { break; }
        }
    }
}

public sealed class EBelgeAyarDto
{
    public string? Saglayici { get; set; }
    public string? Ortam { get; set; }
    /// <summary>Boş = kayıtlı anahtar korunur (aynı entegratörde).</summary>
    public string? Anahtar { get; set; }
    public string? SeriOnEki { get; set; }
    public bool Etkin { get; set; } = true;
}

[ApiController]
[Authorize]
[Route("api/v1/ebelge")]
[Yetki.ModulYetkisi("fatura")]
public sealed class EBelgeController(EBelgeServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/ebelge/ayar — entegratör bağlantısı (anahtar dönmez).</summary>
    [HttpGet("ayar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EBelgeAyarOzet> AyarGetir() => servis.AyarGetir(Islem);

    /// <summary>PUT /api/v1/ebelge/ayar — bağlantıyı kaydet ve dene (yalnız SAHİP).</summary>
    [HttpPut("ayar")]
    [RolGerekli("SAHIP")]
    public Task<EBelgeAyarKayitSonucu> AyarKaydet([FromBody] EBelgeAyarDto dto) => servis.AyarKaydet(dto, Islem);

    /// <summary>DELETE /api/v1/ebelge/ayar — bağlantıyı kaldır (anahtar silinir).</summary>
    [HttpDelete("ayar")]
    [RolGerekli("SAHIP")]
    public Task<EBelgeAyarOzet> AyarKaldir() => servis.AyarKaldir(Islem);

    /// <summary>GET /api/v1/ebelge/gonderimler — giden kutusu (son 200).</summary>
    [HttpGet("gonderimler")]
    public Task<List<EBelgeGonderimOzet>> Liste([FromQuery] int limit = 200) => servis.Liste(Islem, limit);

    /// <summary>GET /api/v1/ebelge/faturalar/{id} — faturanın e-belge durumu (kayıt yoksa 204).</summary>
    [HttpGet("faturalar/{id}")]
    public Task<EBelgeGonderimOzet?> FaturaDurumu(string id) => servis.FaturaDurumu(id, Islem);

    /// <summary>POST /api/v1/ebelge/faturalar/{id}/yeniden-dene — hatalı belgeyi yeniden gönder.</summary>
    [HttpPost("faturalar/{id}/yeniden-dene")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<EBelgeGonderimOzet> YenidenDene(string id) => servis.YenidenDene(id, Islem);

    /// <summary>GET /api/v1/ebelge/faturalar/{id}/pdf — entegratörün resmi PDF'i.</summary>
    [HttpGet("faturalar/{id}/pdf")]
    public async Task<IActionResult> Pdf(string id)
    {
        var (pdf, ad) = await servis.Pdf(id, Islem);
        return File(pdf, "application/pdf", ad);
    }
}
