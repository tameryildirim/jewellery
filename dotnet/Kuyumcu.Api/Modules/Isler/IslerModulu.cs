using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler;

// ── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// PAKET P-11 (B-24) — zamanlanmış iş ayarları PATCH gövdesi.
///
/// ÜÇ DURUM (CariGuncelleDto.Aktif/AktifVerildi ile AYNI kalıp; ikinci kalıp yok):
///   alan gövdede YOK  → değişmedi
///   alan null         → ayar SİLİNİR, yapılandırma varsayılanına döner
///   alan dolu         → yeni değer
/// </summary>
public sealed class IsAyarPatchDto
{
    private int? _baslangicSaat, _vadeUfukGun, _masakUyariGun;
    private List<string>? _kapaliIsler;

    [Range(IsAyarAnahtari.SaatEnAz, IsAyarAnahtari.SaatEnCok)]
    public int? BaslangicSaat { get => _baslangicSaat; set { _baslangicSaat = value; BaslangicSaatVerildi = true; } }
    [JsonIgnore] public bool BaslangicSaatVerildi { get; private set; }

    [Range(IsAyarAnahtari.VadeEnAz, IsAyarAnahtari.VadeEnCok)]
    public int? VadeUfukGun { get => _vadeUfukGun; set { _vadeUfukGun = value; VadeUfukGunVerildi = true; } }
    [JsonIgnore] public bool VadeUfukGunVerildi { get; private set; }

    [Range(IsAyarAnahtari.MasakEnAz, IsAyarAnahtari.MasakEnCok)]
    public int? MasakUyariGun { get => _masakUyariGun; set { _masakUyariGun = value; MasakUyariGunVerildi = true; } }
    [JsonIgnore] public bool MasakUyariGunVerildi { get; private set; }

    /// <summary>Kiracının KAPATTIĞI iş kodları. Boş liste / null = hiçbiri kapalı değil.</summary>
    public List<string>? KapaliIsler { get => _kapaliIsler; set { _kapaliIsler = value; KapaliIslerVerildi = true; } }
    [JsonIgnore] public bool KapaliIslerVerildi { get; private set; }
}

public sealed class BildirimKapatDto
{
    /// <summary>Kapatma notu — isteğe bağlı, ama 500 karakteri aşamaz.</summary>
    [MaxLength(500)]
    public string? Not { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// BİLDİRİM SERVİSİ — liste / sayı / kapat.
///
/// ⚠ GÖRÜNÜRLÜK SUNUCUDA SÜZÜLÜR: gorunur_roller dolu bir bildirim yalnız o
///   rollere döner. Ekranda gizlemek kapı sayılmaz (MASAK kaynaklı bildirim
///   TEZGÂHTAR'a HİÇ gönderilmez — BildirimHttpTestleri zorlar).
/// ⚠ BİLDİRİM SİLİNMEZ: kapatma yalnız damgadır.
/// </summary>
public sealed class BildirimServisi(KuyumcuDbContext db, DenetimServisi denetim, IConfiguration yapilandirma)
{
    public const int AzamiListe = 100;

    /// <summary>Kapalı bildirimlerin listede görüneceği gün penceresi (tablo büyümesi kuralı).</summary>
    public int KapaliGosterimGun =>
        Math.Clamp(yapilandirma.GetValue("Isler:KapaliGosterimGun", 30), 1, 3650);

    public async Task<object> Liste(AktifKullanici islem, string? durum, int? limit)
    {
        // ⚠ DataAnnotations yalnız controller yolunda çalışır; sınır SERVİSTE de
        //   sıkıştırılır (UX-S29: sınır ekranda da yazar).
        var sinir = Math.Clamp(limit ?? AzamiListe, 1, AzamiListe);
        var d = (durum ?? "ACIK").ToUpperInvariant();
        if (d is not ("ACIK" or "KAPALI" or "TUMU"))
            throw new BadRequestException("Durum ACIK, KAPALI veya TUMU olmalıdır");

        var eskiSinir = DateTime.UtcNow.AddDays(-KapaliGosterimGun);

        var sorgu = Gorunur(islem);
        sorgu = d switch
        {
            "ACIK" => sorgu.Where(b => b.Kapatildi == null),
            "KAPALI" => sorgu.Where(b => b.Kapatildi != null && b.Kapatildi >= eskiSinir),
            _ => sorgu.Where(b => b.Kapatildi == null || b.Kapatildi >= eskiSinir),
        };

        var satirlar = await sorgu
            .OrderBy(b => b.Onem == BildirimOnem.Kritik ? 0 : b.Onem == BildirimOnem.Uyari ? 1 : 2)
            .ThenByDescending(b => b.Olusturuldu)
            .Take(sinir + 1)
            .Select(b => new
            {
                b.Id, b.Tur, b.Onem, b.Baslik, b.Govde, b.HedefRota,
                b.IlgiliVarlikTuru, b.IlgiliVarlikId,
                b.Olusturuldu, b.Kapatildi, b.KapatanKullaniciId, b.KapatmaNotu,
            })
            .ToListAsync();

        var dahaVar = satirlar.Count > sinir;
        if (dahaVar) satirlar.RemoveAt(satirlar.Count - 1);

        return new
        {
            durum = d,
            limit = sinir,
            azamiLimit = AzamiListe,
            kapaliGosterimGun = KapaliGosterimGun,
            dahaFazlasiVar = dahaVar,
            // D-12 (21.10): bildirim metni kayıtta Türkçe durur; istek diline burada çevrilir (eşleşme yoksa AYNEN).
            bildirimler = satirlar.Select(b => new
            {
                b.Id, b.Tur, b.Onem,
                Baslik = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(b.Baslik),
                Govde = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(b.Govde),
                b.HedefRota, b.IlgiliVarlikTuru, b.IlgiliVarlikId,
                b.Olusturuldu, b.Kapatildi, b.KapatanKullaniciId, b.KapatmaNotu,
            }).ToList(),
        };
    }

    /// <summary>Rozet için: açık bildirim SAYISI + en yüksek önem. Liste çekilmez.</summary>
    public async Task<object> Sayi(AktifKullanici islem)
    {
        var acik = Gorunur(islem).Where(b => b.Kapatildi == null);
        var sayi = await acik.CountAsync();
        var kritik = sayi > 0 && await acik.AnyAsync(b => b.Onem == BildirimOnem.Kritik);
        var uyari = sayi > 0 && await acik.AnyAsync(b => b.Onem == BildirimOnem.Uyari);

        return new
        {
            sayi,
            enYuksekOnem = sayi == 0 ? null : kritik ? BildirimOnem.Kritik : uyari ? BildirimOnem.Uyari : BildirimOnem.Bilgi,
        };
    }

    /// <summary>Bildirimi kapatır — satır SİLİNMEZ, yalnız damgalanır.</summary>
    public async Task<object> Kapat(string id, BildirimKapatDto dto, AktifKullanici islem)
    {
        var not = string.IsNullOrWhiteSpace(dto.Not) ? null : dto.Not.Trim();
        if (not is { Length: > 500 })
            throw new BadRequestException("Kapatma notu en fazla 500 karakter olabilir");

        // ⚠ Görünürlük süzmesi kapatmada DA geçerli: görmediğin bildirimi
        //   kapatamazsın. Kiracı filtresi zaten global.
        var bildirim = await Gorunur(islem).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException("Bildirim bulunamadı");

        if (bildirim.Kapatildi is not null)
            throw new BadRequestException("Bildirim zaten kapatılmış");

        bildirim.Kapatildi = DateTime.UtcNow;
        bildirim.KapatanKullaniciId = islem.Id;
        bildirim.KapatmaNotu = not;

        denetim.YazmaKaydet(islem, "Bildirim", bildirim.Id,
            $"Bildirim kapatıldı ({bildirim.Tur})", hassas: false);
        await db.SaveChangesAsync();

        return new { bildirim.Id, bildirim.Tur, bildirim.Kapatildi, mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bildirim kapatıldı") };
    }

    /// <summary>Rolün GÖRMEYE yetkili olduğu bildirimler — tek süzme noktası.</summary>
    private IQueryable<Bildirim> Gorunur(AktifKullanici islem) =>
        db.Bildirimler.Where(b =>
            b.GorunurRoller == "" ||
            ("," + b.GorunurRoller + ",").Contains("," + islem.Rol + ","));
}

// ── Controller: bildirimler ──────────────────────────────────────────────────

/// <summary>
/// Bildirim uçları. Nitelikler AÇIKÇA yazılır (miras alınmaz) — SayimModulu deseni.
/// </summary>
[ApiController]
[Authorize]
[Yetki.ModulYetkisi("bildirim")]
[Route("api/v1/bildirimler")]
public sealed class BildirimController(BildirimServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/bildirimler?durum=ACIK|KAPALI|TUMU&amp;limit=100 — görünür bildirimler</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Liste([FromQuery] string? durum, [FromQuery] int? limit) =>
        servis.Liste(Islem, durum, limit);

    /// <summary>GET /api/v1/bildirimler/sayi — üst bar rozeti (modül yetkisi olan herkes)</summary>
    [HttpGet("sayi")]
    public Task<object> Sayi() => servis.Sayi(Islem);

    /// <summary>POST /api/v1/bildirimler/{id}/kapat — bildirimi kapat (satır SİLİNMEZ)</summary>
    [HttpPost("{id}/kapat")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR")]
    public Task<object> Kapat(string id, [FromBody] BildirimKapatDto dto) =>
        servis.Kapat(id, dto ?? new BildirimKapatDto(), Islem);
}

// ── Controller: işler ────────────────────────────────────────────────────────

/// <summary>
/// Zamanlanmış iş yönetimi. SAHIP/MUDUR — elle tetikleme kiracının TÜM
/// verisini tarayan bir koşum başlatır, tezgâhtarın işi değildir.
/// </summary>
[ApiController]
[Authorize]
[Yetki.ModulYetkisi("bildirim")]
[RolGerekli("SAHIP", "MUDUR")]
[Route("api/v1/isler")]
public sealed class IslerController(
    IsCalistirici calistirici, IsAyarServisi ayarServisi,
    KuyumcuDbContext db, DenetimServisi denetim) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/isler — tanımlı işler + son 50 çalışma kaydı</summary>
    [HttpGet]
    public async Task<object> Liste()
    {
        var calismalar = await db.IsCalismalari.AsNoTracking()
            .Where(c => c.TenantId == Islem.TenantId)
            .OrderByDescending(c => c.Baslangic)
            .Take(50)
            .Select(c => new
            {
                c.Id, c.IsKodu, c.CalismaAnahtari, c.Baslangic, c.Bitis, c.Durum,
                c.UretilenBildirim, c.IncelenenKayit, c.SureMs, c.HataOzeti,
                elle = c.TetikleyenKullaniciId != null,
            })
            .ToListAsync();

        var sonlar = calismalar
            .GroupBy(c => c.IsKodu)
            .ToDictionary(g => g.Key, g => g.First());

        // PAKET P-11 (B-24): ekran "neden kapalı" sorusunu ayırt edebilmeli —
        // yapılandırma kapısı (kurulum/mevzuat, ekrandan açılamaz) mı, kiracı
        // tercihi (ekrandan açılabilir) mi? İki bayrak AYRI döner (UX-S30).
        var ayarlar = await ayarServisi.Oku(Islem.TenantId, HttpContext.RequestAborted);

        var isler = calistirici.Tumu.Select(i => new
        {
            kod = i.Kod,
            ad = i.Ad,
            yapilandirmaEtkin = calistirici.Etkin(i),
            kiraciKapatti = ayarlar.KapaliIsler.Contains(i.Kod, StringComparer.Ordinal),
            etkin = calistirici.Etkin(i) && !ayarlar.KapaliIsler.Contains(i.Kod, StringComparer.Ordinal),
            sonCalisma = sonlar.TryGetValue(i.Kod, out var s) ? s.Baslangic : (DateTime?)null,
            sonDurum = sonlar.TryGetValue(i.Kod, out var d) ? d.Durum : null,
        }).ToList();

        var v = ayarServisi.Varsayilanlar;

        return new
        {
            isler,
            calismalar,
            azamiCalisma = 50,
            ayarlar = new
            {
                baslangicSaat = ayarlar.BaslangicSaat,
                vadeUfukGun = ayarlar.VadeUfukGun,
                masakUyariGun = ayarlar.MasakUyariGun,
                kapaliIsler = ayarlar.KapaliIsler,
                // UX-S21/S30: sınırlar ve varsayılanlar SUNUCUDAN gelir; ekran
                // kendi kopyasını tutmaz (aynı sorunun iki cevabı olamaz).
                saatEnAz = IsAyarAnahtari.SaatEnAz, saatEnCok = IsAyarAnahtari.SaatEnCok,
                vadeEnAz = IsAyarAnahtari.VadeEnAz, vadeEnCok = IsAyarAnahtari.VadeEnCok,
                masakEnAz = IsAyarAnahtari.MasakEnAz, masakEnCok = IsAyarAnahtari.MasakEnCok,
                varsayilanVadeUfukGun = v.VadeUfukGun,
                varsayilanMasakUyariGun = v.MasakUyariGun,
            },
        };
    }

    /// <summary>
    /// PATCH /api/v1/isler/ayarlar — zamanlanmış iş ayarları (B-24).
    /// Gövdede olmayan alan DEĞİŞMEZ; açıkça null gelen alan SİLİNİR.
    /// </summary>
    [HttpPatch("ayarlar")]
    public async Task<object> AyarGuncelle([FromBody] IsAyarPatchDto dto)
    {
        var a = await ayarServisi.Guncelle(Islem, dto ?? new IsAyarPatchDto(), HttpContext.RequestAborted);
        return new
        {
            baslangicSaat = a.BaslangicSaat,
            vadeUfukGun = a.VadeUfukGun,
            masakUyariGun = a.MasakUyariGun,
            kapaliIsler = a.KapaliIsler,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Zamanlanmış iş ayarları kaydedildi"),
        };
    }

    /// <summary>POST /api/v1/isler/{kod}/calistir — işi ŞİMDİ koştur (3/dk)</summary>
    [HttpPost("{kod}/calistir")]
    [EnableRateLimiting(OranSiniri.IsCalistir)]
    public async Task<object> Calistir(string kod)
    {
        var i = calistirici.Bul(kod) ?? throw new NotFoundException($"Tanımlı iş bulunamadı: {kod}");
        if (!calistirici.Etkin(i))
            throw new BadRequestException($"Bu iş yapılandırmada kapalı: {kod}");
        // B-24: kiracı kendi ayarıyla kapattıysa elle de koşmaz — ekrandaki
        // kilitli düğme ile sunucu AYNI cevabı verir (sessiz etkisizlik yok).
        if (!await calistirici.KiraciEtkinMi(i, Islem.TenantId, HttpContext.RequestAborted))
            throw new BadRequestException($"Bu iş bu işletmenin ayarlarında kapalı: {kod}");

        denetim.YazmaKaydet(Islem, "IS_ELLE_CALISTIRMA", i.Kod,
            $"Zamanlanmış iş elle çalıştırıldı ({i.Kod})", hassas: false);
        await db.SaveChangesAsync();

        var sonuc = await calistirici.ElleCalistir(i, Islem.TenantId, DateTime.UtcNow, Islem.Id, HttpContext.RequestAborted);

        return new
        {
            isKodu = sonuc.IsKodu,
            calismaAnahtari = sonuc.CalismaAnahtari,
            durum = sonuc.Durum,
            atlandi = sonuc.Atlandi,
            uretilenBildirim = sonuc.UretilenBildirim,
            atlananBildirim = sonuc.AtlananBildirim,
            incelenenKayit = sonuc.IncelenenKayit,
            sureMs = sonuc.SureMs,
            hataOzeti = sonuc.HataOzeti,
            // ⚠ Anayasa md.1: ATLANDI durumu "çalıştı" diye raporlanmaz. Elle
            //   koşum anahtarı dakika çözünürlüklüdür (…-Ehhmm); aynı dakika
            //   içindeki ikinci tetikleme koşmaz ve kullanıcı bunu OKUR.
            mesaj = sonuc.Atlandi
                ? "Bu iş aynı çalışma anahtarıyla zaten koştu; yeni koşum yapılmadı (aynı dakika içinde tekrar tetiklendi)."
                : sonuc.Durum == IsDurum.Hatali
                ? "İş hatayla bitti; ayrıntı iş çalışmaları kaydındadır."
                : string.Create(CultureInfo.InvariantCulture,
                    $"İş çalıştı: {sonuc.UretilenBildirim} yeni bildirim, {sonuc.AtlananBildirim} tekrar (atlandı)."),
        };
    }
}
