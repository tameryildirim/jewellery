using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Iceaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// İÇE AKTARIM KAYNAKLARI — paket 47 Faz A
//
// ⚠ FAIL-CLOSED 3 (Bertan + Mehmet ortak koşulu, denetim.sh ile zorlanır):
//   BU DOSYA VE BU KLASÖR KENDİ YAZMA YOLUNU AÇMAZ.
//   `db.Cariler.Add` / `db.Urunler.Add` / `Hareketler.Add` burada YOKTUR;
//   satır uygulaması YALNIZ mevcut servisleri (CariServisi.Olustur /
//   UrunServisi.Olustur) çağırır. Sebep: alan şifreleme, cari bakiye satırı,
//   barkod üretimi, TasKapisi ve MadenKapisi o yolun içindedir; kendi Add'imiz
//   olsaydı hepsi sessizce atlanırdı.
//
// ⚠ DOĞRULAMA İKİ KEZ YAPILIR ve bu bilinçlidir:
//   (1) Kuru provada — DTO DataAnnotations'ı ELLE koşturulur (`Validator`),
//       çünkü DataAnnotations yalnız controller model bağlama yolunda çalışır;
//       servis çağrısında hiç devreye girmez.
//   (2) Uygulamada — servisin kendi kapıları. Prova "geçerli" dediği hâlde
//       servis reddederse bu BEKLENMEDİK bir durumdur: tüm oturum geri alınır
//       (kısmi göç en kötü hâldir).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Şablon kolonu.</summary>
/// <param name="Ad">CSV başlığı (camelCase — dışa aktarım başlıklarının aynası).</param>
/// <param name="Zorunlu">Başlık dosyada yoksa dosya reddedilir.</param>
/// <param name="Ornek">Şablon satırındaki örnek değer (ekranda da gösterilir).</param>
public sealed record IceAktarimKolonu(string Ad, bool Zorunlu, string Ornek);

/// <summary>Bir satırın doğrulama sonucu.</summary>
/// <param name="Dto">Geçerliyse servise gidecek nesne; değilse null.</param>
public sealed record SatirSonucu(object? Dto, string? HataKodu, string? HataMesaji)
{
    public static SatirSonucu Gecerli(object dto) => new(dto, null, null);
    public static SatirSonucu Hata(string kod, string mesaj) => new(null, kod, mesaj);
}

/// <summary>Satır uygulamasının ihtiyaç duyduğu mevcut servisler (DI ile gelir).</summary>
public sealed record IceAktarimServisleri(
    Cari.CariServisi Cari,
    Urun.UrunServisi Urun);

public interface IIceAktarilabilir
{
    /// <summary>URL ve dosya adında kullanılan kısa ad.</summary>
    string Ad { get; }

    /// <summary>Denetim izinde ve satır kaydında görünen hedef varlık türü.</summary>
    string VarlikTuru { get; }

    IceAktarimKolonu[] Kolonlar { get; }

    /// <summary>ham_json'a yazılırken MASKELENECEK kolonlar (ADR-0003 · Mehmet koşulu).</summary>
    string[] KimlikKolonlari { get; }

    /// <summary>Saf doğrulama — veritabanı GÖRMEZ.</summary>
    SatirSonucu Dogrula(IReadOnlyDictionary<string, string> ham);

    /// <summary>Yinelenen kaydın kimliği (yoksa null). Yalnız OKUR.</summary>
    Task<string?> YinelenenBul(KuyumcuDbContext db, string tenantId, object dto);

    /// <summary>Satırı MEVCUT SERVİS üzerinden yazar; oluşan kaydın kimliğini döner.</summary>
    Task<string> Uygula(IceAktarimServisleri servisler, object dto, AktifKullanici islem);

    /// <summary>Geri almada kaydı PASİFE ALIR (soft delete — hard delete yok).</summary>
    Task PasifeAl(IceAktarimServisleri servisler, string varlikId, AktifKullanici islem);
}

public static class Kaynaklar
{
    // GOC-47B: Faz B defter kaynakları (açılış bakiyesi + açılış stoku) — IDefterKaynagi yolu.
    public static readonly IIceAktarilabilir[] Hepsi =
        [new CariKaynagi(), new UrunKaynagi(), new AcilisBakiyeKaynagi(), new AcilisStokKaynagi()];

    public static string[] Adlar => Hepsi.Select(k => k.Ad).ToArray();

    public static IIceAktarilabilir? Bul(string? ad) =>
        Hepsi.FirstOrDefault(k => k.Ad.Equals(ad, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// DTO üzerindeki DataAnnotations'ı ELLE koşturur. Servis yolunda model
/// bağlama olmadığı için bu çağrı yapılmazsa doğrulama HİÇ çalışmaz
/// (projenin DEĞİŞMEZ kuralı: "DTO DataAnnotations yalnız controller yolunda").
/// </summary>
public static class DtoDogrulayici
{
    public static string? IlkHata(object dto)
    {
        var sonuclar = new List<ValidationResult>();
        if (Validator.TryValidateObject(dto, new ValidationContext(dto), sonuclar, validateAllProperties: true))
            return null;
        return sonuclar.FirstOrDefault()?.ErrorMessage ?? "Satır geçersiz";
    }
}

// ── Cari kartı ───────────────────────────────────────────────────────────────

public sealed class CariKaynagi : IIceAktarilabilir
{
    public string Ad => "cari";
    public string VarlikTuru => nameof(Data.Cari);

    // Dışa aktarım CariKaynagi.Basliklar'ının AYNASI — ama yalnız KART alanları.
    // Bakiye kolonları (tlBakiye, hasGramBakiye, usd/eur/gbp) BİLİNÇLİ OLARAK
    // YOKTUR: bakiye bir defter sonucudur, kart alanı değildir. Açılış bakiyesi
    // Faz B'de (paket T-2) CariServisi.Duzeltme üzerinden gelir.
    public IceAktarimKolonu[] Kolonlar =>
    [
        new("tur", true, "MUSTERI"),
        new("ad", true, "Ayşe Yılmaz"),
        new("telefon", false, "0532 111 22 33"),
        new("email", false, "ayse@ornek.com"),
        new("vergiNo", false, "1234567890"),
        new("adres", false, "Şişli, İstanbul"),
        new("notlar", false, ""),
        new("krediLimitiTl", false, "50000"),
        new("varsayilanVadeGun", false, "30"),
    ];

    public string[] KimlikKolonlari => ["telefon", "vergiNo", "adres", "email"];

    public SatirSonucu Dogrula(IReadOnlyDictionary<string, string> ham)
    {
        if (BelirsizSayi(ham, "krediLimitiTl", "varsayilanVadeGun") is { } belirsiz) return belirsiz;
        if (!CsvOkuyucu.SayiOku(Al(ham, "krediLimitiTl"), out var limit))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "krediLimitiTl sayı olmalıdır (ondalık ayracı NOKTA)");

        int? vade = null;
        if (!CsvOkuyucu.SayiOku(Al(ham, "varsayilanVadeGun"), out var vadeD))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "varsayilanVadeGun tam sayı olmalıdır");
        if (vadeD is not null)
        {
            if (vadeD.Value != decimal.Truncate(vadeD.Value))
                return SatirSonucu.Hata("SAYI_GECERSIZ", "varsayilanVadeGun tam sayı olmalıdır (gün)");
            vade = (int)vadeD.Value;
        }

        var dto = new Cari.CariOlusturDto
        {
            Tur = (Al(ham, "tur") ?? "").ToUpperInvariant(),
            Ad = Al(ham, "ad") ?? "",
            Telefon = Bos(Al(ham, "telefon")),
            Email = Bos(Al(ham, "email")),
            VergiNo = Bos(Al(ham, "vergiNo")),
            Adres = Bos(Al(ham, "adres")),
            Notlar = Bos(Al(ham, "notlar")),
            KrediLimitiTl = limit,
            VarsayilanVadeGun = vade,
        };

        var hata = DtoDogrulayici.IlkHata(dto);
        return hata is null ? SatirSonucu.Gecerli(dto) : SatirSonucu.Hata("ALAN_GECERSIZ", hata);
    }

    /// <summary>Eşleşme: telefon (rakamlara indirgenmiş) + ad (Türkçe büyük harf).</summary>
    public async Task<string?> YinelenenBul(KuyumcuDbContext db, string tenantId, object dto)
    {
        var d = (Cari.CariOlusturDto)dto;
        var adAnahtar = TurkceMetin.Buyut(d.Ad).Trim();
        var telAnahtar = SadeceRakam(d.Telefon);

        var adaylar = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Ad == d.Ad)
            .Select(c => new { c.Id, c.Ad, c.Telefon })
            .ToListAsync();

        // Ad karşılaştırması Türkçe kültürle BELLEKTE yapılır: MySQL harmanlaması
        // 'İ'/'I' ayrımında tr-TR ile aynı davranmaz, tahmin edilmez.
        var tumAdlar = adaylar.Count > 0
            ? adaylar
            : await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .Select(c => new { c.Id, c.Ad, c.Telefon })
                .ToListAsync();

        return tumAdlar.FirstOrDefault(c =>
            TurkceMetin.Buyut(c.Ad).Trim() == adAnahtar
            && SadeceRakam(c.Telefon) == telAnahtar)?.Id;
    }

    public async Task<string> Uygula(IceAktarimServisleri servisler, object dto, AktifKullanici islem)
    {
        // ⚠ KENDİ Add'İMİZ YOK: şifreleme, cari bakiye satırı ve denetim izi
        // bu servisin içindedir.
        var sonuc = await servisler.Cari.Olustur((Cari.CariOlusturDto)dto, islem);
        return Kimlik(sonuc);
    }

    public Task PasifeAl(IceAktarimServisleri servisler, string varlikId, AktifKullanici islem) =>
        servisler.Cari.DeaktifEt(varlikId, islem);

    private static string SadeceRakam(string? t) =>
        new((t ?? "").Where(char.IsDigit).ToArray());

    internal static string? Al(IReadOnlyDictionary<string, string> ham, string kolon) =>
        ham.TryGetValue(kolon, out var v) ? v : null;

    internal static string? Bos(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// GOC-47B tur 3: kart kaynaklarının sayı kolonlarında da binlik ayracına benzeyen
    /// noktalı hücre ('45.000') SAYI_BELIRSIZ ile reddedilir — SayiOku (NumberStyles.Number)
    /// onu 45 okurdu. İlk belirsiz kolonun hatası döner; yoksa null.
    /// </summary>
    internal static SatirSonucu? BelirsizSayi(IReadOnlyDictionary<string, string> ham, params string[] kolonlar)
    {
        foreach (var k in kolonlar)
        {
            var h = Al(ham, k);
            if (CsvOkuyucu.BinlikBelirsizMi(h))
                return SatirSonucu.Hata(CsvOkuyucu.SayiBelirsizKodu, CsvOkuyucu.SayiBelirsizMesaji(k, h!));
        }
        return null;
    }

    /// <summary>
    /// Servis yanıtından kimliği okur.
    /// <para>
    /// ⚠ BÜYÜK/KÜÇÜK HARF DUYARSIZ: CariServisi.Getir anonim nesne döndürür
    /// (<c>id</c>), UrunServisi.Getir ise VARLIĞIN KENDİSİNİ döndürür
    /// (<c>Id</c>). İkisi de desteklenir; birinin şekli değişirse burada
    /// sessizce null'a düşmek yerine açık hata verilir (Anayasa md.1).
    /// </para>
    /// </summary>
    internal static string Kimlik(object servisYaniti)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(servisYaniti);
        using var belge = System.Text.Json.JsonDocument.Parse(json);
        foreach (var p in belge.RootElement.EnumerateObject())
            if (p.Name.Equals("id", StringComparison.OrdinalIgnoreCase)
                && p.Value.GetString() is { Length: > 0 } deger)
                return deger;

        throw new Common.ApiException(500,
            "İçe aktarım: servis yanıtında kayıt kimliği bulunamadı — satır uygulanamadı.");
    }
}

// ── Ürün kartı ───────────────────────────────────────────────────────────────

public sealed class UrunKaynagi : IIceAktarilabilir
{
    public string Ad => "urun";
    public string VarlikTuru => nameof(Data.Urun);

    public IceAktarimKolonu[] Kolonlar =>
    [
        new("tip", true, "ZIYNET"),
        new("kod", true, "BLZ-001"),
        new("ad", true, "22 ayar bilezik"),
        new("ayarId", true, "K22"),
        new("gramAgirlik", true, "12.5"),
        new("aciklama", false, ""),
        new("birim", false, "ADET"),
        new("tasAgirlik", false, "0"),
        new("tasKokeni", false, "TAS_YOK"),
        new("tasIbaresi", false, ""),
        new("iscilikTl", false, "250"),
        new("satisFiyati", false, ""),
        new("barkod", false, ""),
        new("kmtsKodu", false, ""),
    ];

    /// <summary>Ürün kartında kişisel veri yoktur — maskelenecek kolon da yok.</summary>
    public string[] KimlikKolonlari => [];

    public SatirSonucu Dogrula(IReadOnlyDictionary<string, string> ham)
    {
        if (CariKaynagi.BelirsizSayi(ham, "gramAgirlik", "tasAgirlik", "iscilikTl", "satisFiyati") is { } belirsiz)
            return belirsiz;
        if (!CsvOkuyucu.SayiOku(CariKaynagi.Al(ham, "gramAgirlik"), out var gram))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "gramAgirlik sayı olmalıdır (ondalık ayracı NOKTA)");
        if (gram is null)
            return SatirSonucu.Hata("ALAN_ZORUNLU", "gramAgirlik zorunludur");
        if (!CsvOkuyucu.SayiOku(CariKaynagi.Al(ham, "tasAgirlik"), out var tas))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "tasAgirlik sayı olmalıdır");
        if (!CsvOkuyucu.SayiOku(CariKaynagi.Al(ham, "iscilikTl"), out var iscilik))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "iscilikTl sayı olmalıdır");
        if (!CsvOkuyucu.SayiOku(CariKaynagi.Al(ham, "satisFiyati"), out var fiyat))
            return SatirSonucu.Hata("SAYI_GECERSIZ", "satisFiyati sayı olmalıdır");

        var koken = CariKaynagi.Bos(CariKaynagi.Al(ham, "tasKokeni"))?.ToUpperInvariant();
        var ibare = CariKaynagi.Bos(CariKaynagi.Al(ham, "tasIbaresi"));

        // ⚠ ADR-0013 — TAŞ KÖKENİ KAPISI SATIR HATASINA ÇEVRİLİR, YUTULMAZ.
        // Kuyum Ticareti Yönetmeliği md.11/(e): taş ağırlığı > 0 olup kökeni
        // boş olan satır GEÇERLİ SAYILAMAZ. Kapının kendisi UrunServisi.Olustur
        // içinde de çalışır; burada erken çağrılmasının sebebi kullanıcının
        // hatayı KURU PROVADA, satır numarasıyla görmesidir.
        try
        {
            Urun.TasKapisi.Dogrula(tas, koken, ibare);
        }
        catch (Common.ApiException ex)
        {
            return SatirSonucu.Hata("TAS_KOKENI", ex.Message);
        }

        var dto = new Urun.UrunOlusturDto
        {
            Tip = (CariKaynagi.Al(ham, "tip") ?? "").ToUpperInvariant(),
            Kod = CariKaynagi.Al(ham, "kod") ?? "",
            Ad = CariKaynagi.Al(ham, "ad") ?? "",
            Aciklama = CariKaynagi.Bos(CariKaynagi.Al(ham, "aciklama")),
            AyarId = (CariKaynagi.Al(ham, "ayarId") ?? "").ToUpperInvariant(),
            GramAgirlik = gram.Value,
            Birim = CariKaynagi.Bos(CariKaynagi.Al(ham, "birim"))?.ToUpperInvariant() ?? "ADET",
            TasAgirlik = tas,
            TasKokeni = koken,
            TasIbaresi = ibare,
            IscilikTl = iscilik,
            SatisFiyati = fiyat,
            Barkod = CariKaynagi.Bos(CariKaynagi.Al(ham, "barkod")),
            KmtsKodu = CariKaynagi.Bos(CariKaynagi.Al(ham, "kmtsKodu")),
        };

        var hata = DtoDogrulayici.IlkHata(dto);
        if (hata is not null) return SatirSonucu.Hata("ALAN_GECERSIZ", hata);

        // PAKET GOC-C: ürün kartı girdi kapısı (taş < brüt #5, eksi tutar K9a, barkod K18)
        // KURU PROVADA da satır hatası olur — uygulamada UrunServisi.Olustur'un 400'üne kalmaz.
        // Kapı tek kaynaktır (UrunServisi.GocCKapisi); TAS_KOKENI ile aynı kalıp.
        try
        {
            Urun.UrunServisi.GocCKapisi(dto.Birim, dto.GramAgirlik, dto.TasAgirlik ?? 0m, dto.SatisFiyati,
                dto.AlisMaliyeti, dto.KonsinyeBirimBedel, dto.IscilikTl, dto.MinStok, dto.Barkod);
        }
        catch (Common.ApiException ex)
        {
            return SatirSonucu.Hata("ALAN_GECERSIZ", ex.Message);
        }
        return SatirSonucu.Gecerli(dto);
    }

    /// <summary>Eşleşme: stok kodu (SKU) — kiracı içinde zaten benzersizdir.</summary>
    public async Task<string?> YinelenenBul(KuyumcuDbContext db, string tenantId, object dto)
    {
        var d = (Urun.UrunOlusturDto)dto;
        return await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Kod == d.Kod)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<string> Uygula(IceAktarimServisleri servisler, object dto, AktifKullanici islem)
    {
        var sonuc = await servisler.Urun.Olustur((Urun.UrunOlusturDto)dto, islem);
        return CariKaynagi.Kimlik(sonuc);
    }

    public Task PasifeAl(IceAktarimServisleri servisler, string varlikId, AktifKullanici islem) =>
        servisler.Urun.DeaktifEt(varlikId, islem);
}
