using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar;
using Kuyumcu.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Banka;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b (19.10, kurucu: "bankaları önceden bağlayalım") — gerçek banka bağlantısı.
//   • Bağlayıcı (IBankaBaglayicisi): bankanın hesap hareketi servisini çağırıp hareketleri
//     SaglayiciHareketi'ne çevirir. Ayrıştırma saf fonksiyondur (birim testli); HTTP burada.
//   • Kimlik: bankanın işletmeye verdiği servis bilgileri, şifreli (BankaBaglantisi). Çekim anında
//     çözülür, hiçbir yanıtta/günlükte düz yazılmaz.
//   • Bankalar sık sorguyu sevmez: gerçek hesap en erken Banka:GercekCekimSaniye (varsayılan 120)
//     saniyede bir sorulur; "Şimdi çek"/"Bağlantıyı dene" bu sınırı beklemez.
//   • Hata sade dille bağlantı kaydına yazılır (kullanıcı adı/şifre, IP izni, bankaya ulaşılamadı).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bağlayıcıya verilen çekim isteği.</summary>
public sealed record BankaCekimIstegi(
    string Iban, string ParaBirimi, IReadOnlyDictionary<string, string?> Kimlik, DateTime Baslangic, DateTime Bitis);

public interface IBankaBaglayicisi
{
    string Kod { get; }
    Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal);
}

/// <summary>Bankanın kendi söylediği hesap bakiyesi (kullanılabilir = bloke düşülmüş; vermeyen bankada null).</summary>
public sealed record BankaBakiyesi(decimal Bakiye, decimal? Kullanilabilir);

/// <summary>
/// Bakiye de veren bağlayıcı (kurucu 31.10: "hesaba ait bakiye"). İsteğe bağlı: bugün yalnız Garanti
/// (Hesap Bilgisi servisi). Hareket çekimi başarılı olduktan sonra çağrılır; hatası hareketleri düşürmez.
/// </summary>
public interface IBankaBakiyeVeren
{
    Task<BankaBakiyesi?> Bakiye(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal);
}

/// <summary>Bankanın döndüğü, kullanıcıya sade dille söylenecek bağlantı hatası.</summary>
public sealed class BankaBaglantiHatasi(string sade, Exception? ic = null) : Exception(sade, ic);

public static class BankaBaglayicilari
{
    private static readonly IReadOnlyList<IBankaBaglayicisi> Hepsi =
    [
        new Baglayicilar.ZiraatBaglayicisi(), new Baglayicilar.IsBankasiBaglayicisi(), new Baglayicilar.GarantiBaglayicisi(),
        new Baglayicilar.AkbankBaglayicisi(), new Baglayicilar.VakifbankBaglayicisi(), new Baglayicilar.QnbBaglayicisi(),
        new Baglayicilar.KuveytTurkBaglayicisi(), new Baglayicilar.AlbarakaBaglayicisi(),
    ];

    public static IBankaBaglayicisi? Bul(string? kod) => Hepsi.FirstOrDefault(b => b.Kod == kod);

    /// <summary>HTTP durumundan sade hata (bağlayıcılar ortak kullanır).</summary>
    public static BankaBaglantiHatasi HttpHatasi(HttpStatusCode durum, string bankaAdi) => durum switch
    {
        HttpStatusCode.Unauthorized => new($"{bankaAdi} servis kullanıcı adını ya da şifresini kabul etmedi."),
        HttpStatusCode.Forbidden => new($"{bankaAdi} bu sunucuya izin vermedi. Sunucu adresinin bankaya bildirildiğinden emin olun."),
        HttpStatusCode.TooManyRequests => new($"{bankaAdi} çok sık sorgulandı; biraz sonra yeniden denenecek."),
        _ when (int)durum >= 500 => new($"{bankaAdi} şu an yanıt vermiyor; biraz sonra yeniden denenecek."),
        _ => new($"{bankaAdi} isteği kabul etmedi (kod {(int)durum})."),
    };
}

/// <summary>Çekici ile ekranın ortak kullandığı gerçek banka çekimi (kimlik çöz → bağlayıcı → durum yaz).</summary>
public sealed class BankaBaglantiCekici(
    KuyumcuDbContext db, IAlanSifreleyici sifreleyici, IHttpClientFactory httpFabrika,
    IConfiguration yapilandirma, ILogger<BankaBaglantiCekici> gunluk)
{
    public const string HttpAdi = "banka";
    private readonly TimeSpan enAz = TimeSpan.FromSeconds(Math.Clamp(yapilandirma.GetValue("Banka:GercekCekimSaniye", 120), 30, 86_400));

    /// <summary>Hareketleri getirir; bağlantı yoksa/aralık dolmadıysa null. Hata bağlantıya yazılır, null döner.</summary>
    public async Task<IReadOnlyList<SaglayiciHareketi>?> Cek(BankaHesap hesap, bool zorla, CancellationToken iptal)
    {
        var (_, hareketler, _) = await Dene(hesap, zorla, iptal);
        return hareketler;
    }

    /// <summary>(başarılı, hareketler, sade hata). Bağlantı kaydının son deneme/başarı/hata alanlarını günceller.</summary>
    public async Task<(bool Basarili, IReadOnlyList<SaglayiciHareketi>? Hareketler, string? Hata)> Dene(
        BankaHesap hesap, bool zorla, CancellationToken iptal)
    {
        var bag = await db.BankaBaglantilari.FirstOrDefaultAsync(b => b.TenantId == hesap.TenantId && b.BankaHesapId == hesap.Id, iptal);
        if (bag is null || bag.BaglayiciKodu != hesap.SaglayiciKodu) return (false, null, "Bu hesabın banka bağlantısı yok.");
        if (!zorla && bag.SonDeneme is { } sd && DateTime.UtcNow - sd < enAz) return (false, null, null);
        var tanim = BankaBaglayiciKatalogu.Bul(bag.BaglayiciKodu);
        var baglayici = BankaBaglayicilari.Bul(bag.BaglayiciKodu);
        if (tanim is null || baglayici is null) return (false, null, "Bu banka için bağlayıcı bulunamadı.");

        bag.SonDeneme = DateTime.UtcNow;
        try
        {
            var kimlik = JsonSerializer.Deserialize<Dictionary<string, string?>>(sifreleyici.Coz(bag.KimlikSifreli)) ?? [];
            // Boş bırakılan isteğe bağlı alan ByGatech'in kendi kaydından gelir (ör. Garanti uygulama kimliği:
            // Banka:GARANTI:clientId — ortam değişkeni Banka__GARANTI__clientId; appsettings'e yazılmaz).
            foreach (var a in tanim.Alanlar.Where(a => !a.Zorunlu && string.IsNullOrWhiteSpace(kimlik.GetValueOrDefault(a.Ad))))
                if (yapilandirma[$"Banka:{tanim.Kod}:{a.Ad}"] is { Length: > 0 } v) kimlik[a.Ad] = v;
            var bitis = DateTime.UtcNow;
            // Son başarılı turdan 1 gün geriye (gecikmeli kayıtlar); ilk kez 7 gün. Tekillik referansla.
            var baslangic = (hesap.SonCekim?.AddDays(-1) ?? bitis.AddDays(-7));
            if (baslangic < bitis.AddDays(-30)) baslangic = bitis.AddDays(-30);
            using var zaman = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            zaman.CancelAfter(TimeSpan.FromSeconds(60));
            var http = httpFabrika.CreateClient(HttpAdi);
            var cekimIstegi = new BankaCekimIstegi(hesap.Iban, hesap.ParaBirimi, kimlik, baslangic, bitis);
            var sonuc = await baglayici.Cek(http, cekimIstegi, zaman.Token);
            bag.SonBasari = DateTime.UtcNow;
            bag.SonHata = null;
            if (baglayici is IBankaBakiyeVeren bakiyeVeren) await BakiyeYaz(bakiyeVeren, http, cekimIstegi, hesap, zaman.Token);
            await db.SaveChangesAsync(iptal);
            return (true, sonuc, null);
        }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            var sade = ex switch
            {
                BankaBaglantiHatasi b => b.Message,
                OperationCanceledException => $"{tanim.BankaAdi} 60 saniyede yanıt vermedi.",
                HttpRequestException => $"{tanim.BankaAdi} servisine ulaşılamadı. İnternet bağlantısını ve sunucu adresini denetleyin.",
                _ => $"{tanim.BankaAdi} yanıtı okunamadı.",
            };
            // Kimlik ve banka yanıtı günlüğe YAZILMAZ; yalnız hata türü.
            gunluk.LogWarning("[{TenantId}] Banka bağlantısı başarısız ({Kod}, {Tur})", hesap.TenantId, bag.BaglayiciKodu, ex.GetType().Name);
            bag.SonHata = sade.Length > 300 ? sade[..300] : sade;
            await db.SaveChangesAsync(iptal);
            return (false, null, sade);
        }
    }

    /// <summary>
    /// Bankanın bakiyesini hesaba yazar. Hata hareketleri DÜŞÜRMEZ ve bağlantıyı hatalı göstermez (hareketler geldi);
    /// eski bakiye zamanıyla yerinde kalır. Kimlik/yanıt günlüğe yazılmaz, yalnız hata türü.
    /// </summary>
    private async Task BakiyeYaz(IBankaBakiyeVeren veren, HttpClient http, BankaCekimIstegi istek, BankaHesap hesap, CancellationToken iptal)
    {
        try
        {
            if (await veren.Bakiye(http, istek, iptal) is not { } b) return;
            var takip = await db.BankaHesaplari.FirstAsync(h => h.Id == hesap.Id && h.TenantId == hesap.TenantId, iptal);
            takip.BankaBakiye = b.Bakiye;
            takip.BankaKullanilabilir = b.Kullanilabilir;
            takip.BankaBakiyeZamani = DateTime.UtcNow;
        }
        // Zaman aşımı dahil her hata yutulur: hareketler zaten alındı, bakiye bir sonraki turda yeniden denenir.
        catch (Exception ex)
        {
            gunluk.LogWarning("[{TenantId}] Banka bakiyesi alınamadı ({Kod}, {Tur})", hesap.TenantId, hesap.SaglayiciKodu, ex.GetType().Name);
        }
    }
}

public sealed class BankaBaglantiDto
{
    public string? Kod { get; set; }
    public Dictionary<string, string?>? Alanlar { get; set; }
}

public sealed class BankaBaglantiServisi(
    KuyumcuDbContext db, IAlanSifreleyici sifreleyici, BankaCekimServisi cekim,
    DenetimServisi denetim, IConfiguration yapilandirma)
{
    public BankaBaglayiciKatalogOzet Katalog() => new(
        yapilandirma["Banka:CikisIp"],
        BankaBaglayiciKatalogu.Tumu.Where(t => BankaBaglayicilari.Bul(t.Kod) is not null).Select(t => new BankaBaglayiciOzet(
            t.Kod, t.BankaAdi, t.BasvuruYolu, t.IpKisitli,
            t.Alanlar.Select(a => new BankaAlaniOzet(a.Ad, a.Etiket, a.Gizli, a.Zorunlu, a.Ipucu)).ToList())).ToList());

    public async Task<BankaBaglantiOzet> Getir(string hesapId, AktifKullanici islem)
    {
        await HesapBul(hesapId, islem);
        var bag = await db.BankaBaglantilari.AsNoTracking().FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.BankaHesapId == hesapId);
        return Gorunum(bag);
    }

    public async Task<BankaBaglantiKayitSonucu> Kaydet(string hesapId, BankaBaglantiDto dto, AktifKullanici islem)
    {
        var tanim = BankaBaglayiciKatalogu.Bul(dto.Kod?.Trim());
        if (tanim is null || BankaBaglayicilari.Bul(tanim.Kod) is null)
            throw new BadRequestException("Banka seçin").AlanIle("kod");
        var alanlar = (dto.Alanlar ?? []).Where(k => !string.IsNullOrWhiteSpace(k.Value))
            .ToDictionary(k => k.Key, k => (string?)k.Value!.Trim());
        var hesap = await HesapBul(hesapId, islem);
        var bag = await db.BankaBaglantilari.FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.BankaHesapId == hesapId);
        // Aynı bankada yeniden kayıtta boş bırakılan HER alan eski değerini korur (kurucu 05.11: "şifre, kullanıcı adı
        // dahil her şey sabit kalsın"; yalnız değişen alan yazılır). Başka bankaya geçişte hiçbir alan taşınmaz.
        if (bag is not null && bag.BaglayiciKodu == tanim.Kod)
        {
            var eski = JsonSerializer.Deserialize<Dictionary<string, string?>>(sifreleyici.Coz(bag.KimlikSifreli)) ?? [];
            foreach (var a in tanim.Alanlar.Where(a => !alanlar.ContainsKey(a.Ad) && eski.ContainsKey(a.Ad)))
                alanlar[a.Ad] = eski[a.Ad];
        }
        if (BankaBaglayiciKatalogu.Denetle(tanim, alanlar) is { } h) throw new BadRequestException(h.Hata).AlanIle(h.Alan);

        var simdi = DateTime.UtcNow;
        if (bag is null)
        {
            bag = new BankaBaglantisi { Id = Kimlik.Ulid(), TenantId = islem.TenantId, BankaHesapId = hesapId, Olusturuldu = simdi };
            db.BankaBaglantilari.Add(bag);
        }
        bag.BaglayiciKodu = tanim.Kod;
        bag.KimlikSifreli = sifreleyici.Sifrele(JsonSerializer.Serialize(alanlar));
        bag.Ozet = BankaBaglayiciKatalogu.Ozet(tanim, alanlar) is { Length: > 0 } o ? (o.Length > 300 ? o[..300] : o) : null;
        bag.SonHata = null; bag.SonBasari = null; bag.SonDeneme = null;
        bag.Guncellendi = simdi;
        hesap.SaglayiciKodu = tanim.Kod;
        hesap.Guncellendi = simdi;
        denetim.YazmaKaydet(islem, "BankaBaglantisi", bag.Id, $"banka bağlantısı kaydedildi: {tanim.BankaAdi}", hassas: true);
        await db.SaveChangesAsync();
        // Kaydın hemen ardından dene (çekicinin yolu: başarılıysa hareketler de yazılır); sonuç aynı ekranda.
        var sonuc = await cekim.Cek(islem.TenantId, default, hesapId);
        var taze = await db.BankaBaglantilari.AsNoTracking().FirstAsync(b => b.Id == bag.Id);
        return new(Gorunum(taze), new BankaBaglantiDenemesi(taze.SonHata is null && taze.SonBasari is not null, sonuc.Yeni, taze.SonHata));
    }

    public async Task<BankaBaglantiDeneSonucu> Dene(string hesapId, AktifKullanici islem)
    {
        await HesapBul(hesapId, islem);
        // Başarılıysa hareketler de yazılır (aynı yol: çekici).
        var sonuc = await cekim.Cek(islem.TenantId, default, hesapId);
        var bag = await db.BankaBaglantilari.AsNoTracking().FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.BankaHesapId == hesapId)
            ?? throw new NotFoundException("Bu hesabın banka bağlantısı yok");
        return new(Gorunum(bag), sonuc.Yeni, sonuc.Hatali);
    }

    public async Task<BankaBaglantiOzet> Kaldir(string hesapId, AktifKullanici islem)
    {
        var hesap = await HesapBul(hesapId, islem);
        var bag = await db.BankaBaglantilari.FirstOrDefaultAsync(b => b.TenantId == islem.TenantId && b.BankaHesapId == hesapId)
            ?? throw new NotFoundException("Bu hesabın banka bağlantısı yok");
        // Sır tutulmaz: kayıt tamamen silinir (şifreli kimlik geride kalmaz). Alınmış hareketler durur.
        db.BankaBaglantilari.Remove(bag);
        if (hesap.SaglayiciKodu == bag.BaglayiciKodu) hesap.SaglayiciKodu = null;
        hesap.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, "BankaBaglantisi", bag.Id, "banka bağlantısı kaldırıldı", hassas: true);
        await db.SaveChangesAsync();
        return Gorunum(null);
    }

    private async Task<BankaHesap> HesapBul(string hesapId, AktifKullanici islem) =>
        await db.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == hesapId && h.TenantId == islem.TenantId)
        ?? throw new NotFoundException("Banka hesabı bulunamadı");

    private static BankaBaglantiOzet Gorunum(BankaBaglantisi? b) => b is null
        ? new(null, null, null, null, null, null)
        : new(b.BaglayiciKodu, BankaBaglayiciKatalogu.Bul(b.BaglayiciKodu)?.BankaAdi, b.Ozet, b.SonDeneme, b.SonBasari, b.SonHata);
}

[ApiController]
[Authorize]
[Route("api/v1/bankalar")]
[Yetki.ModulYetkisi("banka")]
public sealed class BankaBaglantiController(BankaBaglantiServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/bankalar/baglayicilar — hazır banka bağlayıcıları ve bankaya bildirilecek sunucu adresi.</summary>
    [HttpGet("baglayicilar")]
    [RolGerekli("SAHIP", "MUDUR")]
    public BankaBaglayiciKatalogOzet Katalog() => servis.Katalog();

    /// <summary>GET /api/v1/bankalar/{id}/baglanti — bağlantı durumu (sır dönmez).</summary>
    [HttpGet("{id}/baglanti")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<BankaBaglantiOzet> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PUT /api/v1/bankalar/{id}/baglanti — bankanın verdiği servis bilgilerini kaydet ve hemen dene (yalnız SAHİP).</summary>
    [HttpPut("{id}/baglanti")]
    [RolGerekli("SAHIP")]
    public Task<BankaBaglantiKayitSonucu> Kaydet(string id, [FromBody] BankaBaglantiDto dto) => servis.Kaydet(id, dto, Islem);

    /// <summary>POST /api/v1/bankalar/{id}/baglanti/dene — bağlantıyı şimdi dene (başarılıysa hareketler alınır).</summary>
    [HttpPost("{id}/baglanti/dene")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<BankaBaglantiDeneSonucu> Dene(string id) => servis.Dene(id, Islem);

    /// <summary>DELETE /api/v1/bankalar/{id}/baglanti — bağlantıyı kaldır (şifreli kimlik silinir).</summary>
    [HttpDelete("{id}/baglanti")]
    [RolGerekli("SAHIP")]
    public Task<BankaBaglantiOzet> Kaldir(string id) => servis.Kaldir(id, Islem);
}
