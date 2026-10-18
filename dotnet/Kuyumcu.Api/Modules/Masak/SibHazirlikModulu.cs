using System.Globalization;
using System.Security.Claims;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Uyum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// D1-18 (18.10, rakip eşitleme: "MASAK gönderim yardımcısı · bildirim gizliliği").
//   • MASAK Online 2.0'a yazılımla bağlanma yolu YOK (XML/XSD/web servis yok; giriş kişinin e-Devlet hesabıyla,
//     şifre paylaşmak yasak — ARASTIRMA-MASAK-EFATURA-BAGLANTI-2026-10-14 A1–A3). Yardımcı bu yüzden HAZIRLAR:
//     formun adım sırasıyla kopyalanacak alanlar; kuyumcu kendisi yükler, referans noyu "Gönderildi"ye yazar.
//   • GİZLİLİK (5549 s. Kanun md.4/2): bildirimin yapıldığı işleme taraf olanlar dahil kimseye açıklanamaz.
//     Hazırlık yalnız sahip/müdür; salt okunur hesap (dışarıdan mali müşavir) ŞİB kayıtlarını hiç görmez.
//     Her hazırlık açılışı hassas okuma olarak denetim izine düşer.
// ═════════════════════════════════════════════════════════════════════════════

public static class SibGizlilik
{
    public const string Adres = "https://onlineislemler.masak.gov.tr";
    public static string Not => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(
        "Gizlilik (5549 sayılı Kanun md.4/2): bildirim yapıldığı, işleme taraf olanlar dahil hiç kimseye açıklanamaz.");
    public static string SaltOkunurRed => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(
        "Şüpheli işlem bildirimleri gizlidir; yalnız okuma hesabı bu kayıtları göremez.");

    /// <summary>Salt okunur (dışarıdan mali müşavir) oturum ŞİB kaydı göremez — 403.</summary>
    public static void SaltOkunurReddet(ClaimsPrincipal kullanici)
    {
        if (kullanici.HasClaim(SaltOkunurKapisi.Claim, "1")) throw new ForbiddenException(SaltOkunurRed);
    }
}

public sealed class SibHazirlikServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<SibHazirlik> Hazirla(string id, AktifKullanici islem)
    {
        var b = await db.MasakSupheliBildirimler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Şüpheli işlem bildirimi bulunamadı");
        await denetim.OkumaKaydet(islem, nameof(MasakSupheliBildirim), b.Id, "ŞİB gönderim hazırlığı görüntülendi");

        var cari = b.CariId is null ? null : await db.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == b.CariId && c.TenantId == islem.TenantId);
        var kt = b.KimlikTespitiId is null ? null : await db.MasakKimlikTespitleri.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == b.KimlikTespitiId && k.TenantId == islem.TenantId);
        var h = b.HareketId is null ? null : await db.Hareketler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == b.HareketId && x.TenantId == islem.TenantId);
        var tuzel = cari?.VergiNo is { Length: 10 };

        static string D(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
        var kisi = tuzel ? [new SibAlan("Gerçek kişi", "— (tüzel kişi; 2. adım)")] : new List<SibAlan>
        {
            new("Ad soyad", D(kt?.AdSoyad ?? cari?.Ad)),
            new(kt is null ? "TC kimlik no" : $"Belge ({kt.BelgeTuru})", D(kt?.BelgeNo ?? cari?.VergiNo)),
            new("Doğum tarihi", (kt?.DogumTarihi ?? cari?.DogumTarihi)?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "—"),
            new("Uyruk", D(kt?.Uyruk ?? cari?.Uyruk)),
            new("Adres", D(kt?.Adres ?? cari?.Adres)),
            new("Telefon", D(cari?.Telefon)),
        };
        var tuzelAlan = tuzel ? new List<SibAlan>
        {
            new("Unvan", D(cari!.Unvan ?? cari.Ad)), new("Vergi no", D(cari.VergiNo)), new("Adres", D(cari.Adres)), new("Telefon", D(cari.Telefon)),
        } : [new SibAlan("Tüzel kişi", "—")];
        var islemAlan = new List<SibAlan>
        {
            new("İşlem tarihi", h is null ? "—" : Zaman.UtcdenYerele(h.Tarih).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)),
            new("İşlem türü", h is null ? "—" : IslemAdi(h.Tur)),
            new("Tutar (TL)", b.IslemTutariTl is { } tt ? tt.ToString("N2", Tr) : "—"),
            new("Ödeme aracı", D(h?.OdemeTuru)),
            new("Şüphenin tespit tarihi", Zaman.UtcdenYerele(b.TespitTarihi).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
            new("Son bildirim günü", Zaman.UtcdenYerele(b.SonBildirimTarihi).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
        };
        return new SibHazirlik(b.Id, SibGizlilik.Adres, SibGizlilik.Not,
        [
            new(1, "Gerçek kişi", kisi, null),
            new(2, "Tüzel kişi", tuzelAlan, null),
            new(3, "İşlem", islemAlan, null),
            new(4, "Şüphe kategorisi", [], "Formda en az 1, en çok 3 kategori seçilir — seçimi bildirimi yapan kişi kendisi yapar."),
            new(5, "Açıklama", [new SibAlan("Açıklama", b.Gerekce)], "En az 300, en çok 50.000 karakter (Rehber 2.0 §4.2.7)."),
            new(6, "Ekler", [], b.KimlikTespitiId is null ? "Varsa belge görüntülerini ekleyin (her biri ≤ 50 MB)." : "Kimlik tespit formunu yazdırıp PDF olarak ekleyebilirsiniz (her biri ≤ 50 MB)."),
        ]);
    }

    private static string IslemAdi(string tur) => tur switch
    {
        "SATIS" => "Satış", "ALIS" => "Alış", "HURDA_ALIS" => "Hurda alış", "TAHSILAT" => "Tahsilat", "TAKAS" => "Takas",
        "EMANET_GIRIS" => "Emanet giriş", "EMANET_CIKIS" => "Emanet çıkış", _ => tur,
    };
}

[ApiController]
[Authorize]
[Route("api/v1/masak/supheli")]
public sealed class SibHazirlikController(SibHazirlikServisi servis) : ControllerBase
{
    /// <summary>GET /api/v1/masak/supheli/{id}/hazirlik — MASAK Online formuna kopyalanacak alanlar (sahip/müdür).</summary>
    [HttpGet("{id}/hazirlik")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<SibHazirlik> Hazirlik(string id)
    {
        SibGizlilik.SaltOkunurReddet(User);
        return servis.Hazirla(id, AktifKullanici.FromClaims(User));
    }
}
