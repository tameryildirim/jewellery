using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// M7 — BANKA SAĞLAYICISI + ÇEKİCİ + CANLI YENİLEME (müşteri 16.10: "düşer düşmez hemen şak ekranda
// görüyoruz… şimdilik simülasyon").
//   • IBankaSaglayici: hesabın gelen hareketlerini veren tek arayüz. Bugün SAHTE (demo bankası);
//     gerçek banka (API / hesap hareketi servisi) aynı arayüze takılır — çekici ve ekran değişmez.
//   • BankaCekimServisi: kiracıya sabit kapsamda; yeni hareketi M7a yolundan (GelenOdemeServisi:
//     müşteriyi bul/aç, IBAN öğren, MASAK) geçirerek yazar. (tenant, hesap, saglayici_referans)
//     benzersiz → aynı hareket iki kez YAZILMAZ.
//   • BankaCekici: arka planda her N saniye (Banka:CekimSaniye, varsayılan 20) tüm kiracılar.
//   • BankaHub (/hubs/banka): yeni hareket yazılınca o kiracının ekranlarına "BankaHareketGeldi".
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sağlayıcının verdiği gelen/giden hareket (banka kaydı).</summary>
public sealed record SaglayiciHareketi(
    string Referans, DateTime Tarih, string Yon, decimal Tutar,
    string? KarsiAd, string? KarsiIban, string? KarsiKimlikNo, string? Aciklama);

public interface IBankaSaglayici
{
    string Kod { get; }
    /// <summary>Hesabın son hareketleri (zaten alınmış olanlar da dönebilir; tekillik referansla).</summary>
    Task<IReadOnlyList<SaglayiciHareketi>> Cek(KuyumcuDbContext db, BankaHesap hesap, CancellationToken iptal);
}

public static class BankaSaglayicilari
{
    public const string Sahte = "SAHTE";
    public static readonly string[] Tumu = [Sahte];

    public static IBankaSaglayici? Bul(string? kod) => kod switch
    {
        Sahte => new SahteBankaSaglayici(),
        _ => null,
    };
}

/// <summary>Demo bankası: "Havale gönder" ekranının yazdığı son 7 günün havaleleri.</summary>
public sealed class SahteBankaSaglayici : IBankaSaglayici
{
    public string Kod => BankaSaglayicilari.Sahte;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(KuyumcuDbContext db, BankaHesap hesap, CancellationToken iptal)
    {
        var sinir = DateTime.UtcNow.AddDays(-7);
        return await db.DemoHavaleleri.AsNoTracking()
            .Where(s => s.TenantId == hesap.TenantId && s.BankaHesapId == hesap.Id && s.Olusturuldu >= sinir)
            .OrderBy(s => s.Olusturuldu)
            .Take(200)
            .Select(s => new SaglayiciHareketi("SAHTE-" + s.Id, s.Olusturuldu, Kuyumcu.Core.Domain.HareketYonu.BankaGelen,
                s.Tutar, s.GonderenAd, s.GonderenIban, s.GonderenKimlikNo, s.Aciklama))
            .ToListAsync(iptal);
    }
}

public sealed record CekimSonucu(int Yeni, int Hatali);

public sealed class BankaCekimServisi(
    KuyumcuDbContext db, GelenOdemeServisi gelen, ILogger<BankaCekimServisi> gunluk,
    IHubContext<BankaHub> hub)
{
    /// <summary>Kiracının sağlayıcılı TÜM hesaplarını çeker; yeni hareket varsa ekrana haber verir.</summary>
    public async Task<CekimSonucu> Cek(string tenantId, CancellationToken iptal = default, string? yalnizHesapId = null)
    {
        var hesaplar = await db.BankaHesaplari
            .Where(h => h.TenantId == tenantId && h.Aktif && h.SaglayiciKodu != null
                     && (yalnizHesapId == null || h.Id == yalnizHesapId))
            .Take(50)
            .ToListAsync(iptal);
        int yeni = 0, hatali = 0;
        var islem = new AktifKullanici("", tenantId, null, "SAHIP");   // arka plan: SaklamaUygulaIsi ile aynı kalıp
        foreach (var hesap in hesaplar)
        {
            var saglayici = BankaSaglayicilari.Bul(hesap.SaglayiciKodu);
            if (saglayici is null) continue;
            var hareketler = await saglayici.Cek(db, hesap, iptal);
            var refs = hareketler.Select(h => h.Referans).ToList();
            var mevcut = (await db.BankaHareketleri.AsNoTracking()
                .Where(b => b.TenantId == tenantId && b.BankaHesapId == hesap.Id && b.SaglayiciReferans != null && refs.Contains(b.SaglayiciReferans))
                .Take(refs.Count)
                .Select(b => b.SaglayiciReferans!)
                .ToListAsync(iptal)).ToHashSet();
            foreach (var h in hareketler.Where(x => !mevcut.Contains(x.Referans)))
            {
                try
                {
                    await gelen.HareketEkle(hesap.Id, new HareketEkleDto
                    {
                        Yon = h.Yon, Tutar = h.Tutar, KarsiTarafAd = h.KarsiAd, KarsiIban = h.KarsiIban,
                        KarsiKimlikNo = h.KarsiKimlikNo, Aciklama = h.Aciklama,
                        Tarih = h.Tarih.ToString("O"),
                    }, islem, bankaReferans: h.Referans.Length > 50 ? h.Referans[..50] : h.Referans, saglayiciReferans: h.Referans);
                    yeni++;
                }
                catch (ConflictException) { db.ChangeTracker.Clear(); }   // eşzamanlı tur aynı hareketi yazdı
                catch (ApiException ex)
                {
                    // Satır bazında hata (ör. MASAK kimlik bloğu): diğerleri sürer; hareket bir sonraki turda yeniden denenir.
                    db.ChangeTracker.Clear();
                    hatali++;
                    gunluk.LogWarning("[{TenantId}] Banka çekimi satırı yazılamadı ({Referans}): {Hata}", tenantId, h.Referans, ex.Message);
                }
            }
            var taze = await db.BankaHesaplari.FirstAsync(x => x.Id == hesap.Id && x.TenantId == tenantId, iptal);
            taze.SonCekim = DateTime.UtcNow;
            await db.SaveChangesAsync(iptal);
        }
        if (yeni > 0) await hub.Clients.Group(tenantId).SendAsync("BankaHareketGeldi", yeni, iptal);
        return new CekimSonucu(yeni, hatali);
    }
}

/// <summary>Arka plan çekicisi — tüm kiracıların sağlayıcılı hesapları (Banka:CekimEtkin, Banka:CekimSaniye).</summary>
public sealed class BankaCekici(SistemKapsami kapsam, ILogger<BankaCekici> gunluk, IConfiguration yapilandirma) : BackgroundService
{
    private readonly bool etkin = yapilandirma.GetValue("Banka:CekimEtkin", true);
    private readonly TimeSpan aralik = TimeSpan.FromSeconds(Math.Clamp(yapilandirma.GetValue("Banka:CekimSaniye", 20), 5, 3600));

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        if (!etkin) { gunluk.LogInformation("Banka çekicisi KAPALI (Banka:CekimEtkin=false)"); return; }
        while (!durdur.IsCancellationRequested)
        {
            try { await Tur(durdur); }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex) { gunluk.LogError(ex, "Banka çekici turu hatası"); }
            try { await Task.Delay(aralik, durdur); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task Tur(CancellationToken iptal)
    {
        // SistemKapsami izinli kullanım 11: sağlayıcılı hesabı olan aktif kiracı listesi.
        List<string> kiracilar;
        await using (var sistem = kapsam.Sistem())
        {
            var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
            var aktif = await db.Tenants.AsNoTracking().Where(t => t.Aktif).Select(t => t.Id).Take(10_000).ToListAsync(iptal);
            kiracilar = await db.BankaHesaplari.AsNoTracking()
                .Where(h => h.Aktif && h.SaglayiciKodu != null && aktif.Contains(h.TenantId))
                .Select(h => h.TenantId).Distinct().Take(10_000).ToListAsync(iptal);
        }
        foreach (var kiraciId in kiracilar)
        {
            iptal.ThrowIfCancellationRequested();
            // SistemKapsami izinli kullanım 12: kiracı başına çekim — AYRI kapsam, kiracıya SABİT.
            await using var kiraciKapsami = kapsam.Kiraci(kiraciId);
            try { await kiraciKapsami.ServiceProvider.GetRequiredService<BankaCekimServisi>().Cek(kiraciId, iptal); }
            catch (OperationCanceledException) when (iptal.IsCancellationRequested) { throw; }
            catch (Exception ex) { gunluk.LogError(ex, "Kiracı banka çekimi hatası"); }   // bir kiracı ötekini düşürmez
        }
    }
}

/// <summary>Canlı banka kanalı — bağlanan istemci kendi kiracısının grubuna girer (KurHub ile aynı kalıp).</summary>
[Authorize]
public sealed class BankaHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User is { } u ? AktifKullanici.FromClaims(u).TenantId : null;
        if (!string.IsNullOrEmpty(tenantId)) await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        await base.OnConnectedAsync();
    }
}

// ── Uçlar ────────────────────────────────────────────────────────────────────

public sealed class SaglayiciDto
{
    /// <summary>Boş/null = sağlayıcı yok (elle/ekstre); "SAHTE" = demo bankası.</summary>
    [RegularExpression("SAHTE", ErrorMessage = "Sağlayıcı SAHTE ya da boş olmalıdır")]
    public string? Kod { get; set; }
}

public sealed class SahteHavaleDto
{
    [Range(0.01d, 10_000_000d, ErrorMessage = "Tutar 0,01 ile 10.000.000 arasında olmalıdır")] public decimal Tutar { get; set; }
    [Required(ErrorMessage = "Gönderen adı zorunludur")]
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.HesapSahibi, MinimumLength = 2)]
    [Unvan] public string GonderenAd { get; set; } = null!;
    [Iban] public string? GonderenIban { get; set; }
    [StringLength(11)] public string? GonderenKimlikNo { get; set; }
    [StringLength(Kuyumcu.Core.Domain.Girdi.AlanSinirlari.SahteHavaleAciklama)] public string? Aciklama { get; set; }
}

public sealed class BankaSaglayiciServisi(KuyumcuDbContext db, BankaCekimServisi cekim, DenetimServisi denetim)
{
    public async Task<object> SaglayiciAyarla(string hesapId, SaglayiciDto dto, AktifKullanici islem)
    {
        var kod = string.IsNullOrWhiteSpace(dto.Kod) ? null : dto.Kod.Trim();
        if (kod is not null && !BankaSaglayicilari.Tumu.Contains(kod))
            throw new BadRequestException("Sağlayıcı SAHTE ya da boş olmalıdır").AlanIle("kod");
        var hesap = await db.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == hesapId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");
        var eski = hesap.SaglayiciKodu;
        hesap.SaglayiciKodu = kod;
        hesap.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, "BankaHesap", hesap.Id, $"banka sağlayıcısı: {eski ?? "yok"} → {kod ?? "yok"}", hassas: false);
        await db.SaveChangesAsync();
        return new { id = hesap.Id, saglayiciKodu = hesap.SaglayiciKodu, sonCekim = hesap.SonCekim };
    }

    /// <summary>Demo bankasına gelen havale yazar ve HEMEN çeker (çekici turunu beklemeden ekrana düşer).</summary>
    public async Task<object> SahteHavale(string hesapId, SahteHavaleDto dto, AktifKullanici islem)
    {
        var hesap = await db.BankaHesaplari.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hesapId && h.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Banka hesabı bulunamadı");
        if (hesap.SaglayiciKodu != BankaSaglayicilari.Sahte)
            throw new ConflictException("Bu hesap demo bankasına bağlı değil; önce hesabın sağlayıcısını 'Demo banka' yapın.");
        if (dto.Tutar <= 0m || Kuyumcu.Core.Domain.Girdi.SayiKurali.Ondalik(dto.Tutar, 2) is not null)
            throw new BadRequestException("Tutar 0,01 ile 10.000.000 arasında olmalıdır").AlanIle("tutar");
        GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.AdKurali.Unvan(dto.GonderenAd), "gonderenAd");
        var iban = string.IsNullOrWhiteSpace(dto.GonderenIban) ? null : Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Normallestir(dto.GonderenIban);
        if (iban is not null && Kuyumcu.Core.Domain.Girdi.IbanDogrulayici.Denetle(iban) is not null)
            throw new BadRequestException("IBAN geçersiz").AlanIle("gonderenIban");
        var kimlik = Kuyumcu.Core.Domain.DogalAnahtar.Normalle(dto.GonderenKimlikNo);
        if (kimlik is not null && Kuyumcu.Core.Domain.KimlikNo.Dogrula(kimlik, Kuyumcu.Core.Domain.KimlikNo.TurTuret(kimlik)) is { } kh)
            throw new BadRequestException(kh).AlanIle("gonderenKimlikNo");

        db.DemoHavaleleri.Add(new SahteBankaHareketi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, BankaHesapId = hesap.Id, Tutar = dto.Tutar,
            GonderenAd = dto.GonderenAd.Trim(), GonderenIban = iban, GonderenKimlikNo = kimlik,
            Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var sonuc = await cekim.Cek(islem.TenantId, default, hesap.Id);
        return new { yeni = sonuc.Yeni, hatali = sonuc.Hatali };
    }

    public async Task<object> SimdiCek(string hesapId, AktifKullanici islem)
    {
        var var_ = await db.BankaHesaplari.AnyAsync(h => h.Id == hesapId && h.TenantId == islem.TenantId);
        if (!var_) throw new NotFoundException("Banka hesabı bulunamadı");
        var sonuc = await cekim.Cek(islem.TenantId, default, hesapId);
        return new { yeni = sonuc.Yeni, hatali = sonuc.Hatali };
    }
}

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
public sealed class BankaSaglayiciController(BankaSaglayiciServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>PUT /api/v1/bankalar/{id}/saglayici — hesabın hareket sağlayıcısı (SAHTE | boş) — SAHIP/MUDUR.</summary>
    [HttpPut("{id}/saglayici")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> SaglayiciAyarla(string id, [FromBody] SaglayiciDto dto) => servis.SaglayiciAyarla(id, dto, Islem);

    /// <summary>POST /api/v1/bankalar/{id}/sahte-havale — demo bankasına gelen havale (yalnız SAHTE hesap).</summary>
    [HttpPost("{id}/sahte-havale")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> SahteHavale(string id, [FromBody] SahteHavaleDto dto) => servis.SahteHavale(id, dto, Islem);

    /// <summary>POST /api/v1/bankalar/{id}/cek — sağlayıcıdan şimdi çek.</summary>
    [HttpPost("{id}/cek")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public Task<object> SimdiCek(string id) => servis.SimdiCek(id, Islem);
}
