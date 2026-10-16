using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler;

/// <summary>
/// PAKET P-11 (B-24) — ZAMANLANMIŞ İŞ AYARLARI SERVİSİ.
///
/// <para>B-24'ün kökeni: saat / eşik / açık-kapalı yalnız `appsettings.json`
/// içindeydi. Kiracı ne görebiliyor ne değiştirebiliyordu; "ayar var ama
/// ekranda yok" (UX-S25). Ayarlar artık MEVCUT `kiraci_ayarlari` tablosunda.</para>
///
/// <para>⚠ YALNIZ DARALTIR: yapılandırmada KAPALI iş ekrandan AÇILAMAZ
/// (<see cref="IsAyarAnahtari"/> notu). Ekran bunu söyler, servis zorlar.</para>
///
/// <para>⚠ PATCH SÖZLEŞMESİ: gövdede olmayan alan DEĞİŞMEZ, açıkça
/// <c>null</c> gelen alan SİLİNİR (varsayılana döner). Üç durum ayrımı
/// <c>...Verildi</c> bayraklarıyla yapılır; tam gövdeyle üzerine yazılmaz.</para>
/// </summary>
public sealed class IsAyarServisi(KuyumcuDbContext db, IConfiguration yapilandirma, DenetimServisi denetim,
    IEnumerable<IZamanlanmisIs>? isler = null)   // PAKET GOC-B: bilinen iş kodları (DI her zaman verir)
{
    /// <summary>Yapılandırma varsayılanları — kiracı ezmesi yokken geçerli değerler.</summary>
    public IsAyarlari Varsayilanlar => new(
        BaslangicSaat: 0,
        VadeUfukGun: Math.Clamp(yapilandirma.GetValue("Isler:Vade:UfukGun", 7),
            IsAyarAnahtari.VadeEnAz, IsAyarAnahtari.VadeEnCok),
        MasakUyariGun: Math.Clamp(yapilandirma.GetValue("Isler:Masak:UyariGun", 3),
            IsAyarAnahtari.MasakEnAz, IsAyarAnahtari.MasakEnCok),
        KapaliIsler: []);

    /// <summary>Kiracının çözülmüş ayarları (varsayılan + ezme).</summary>
    public async Task<IsAyarlari> Oku(string tenantId, CancellationToken iptal = default)
    {
        var satirlar = await db.KiraciAyarlari.AsNoTracking()
            .Where(a => a.TenantId == tenantId && IsAyarAnahtari.Tumu.Contains(a.Anahtar))
            .ToDictionaryAsync(a => a.Anahtar, a => a.Deger, iptal);

        var v = Varsayilanlar;
        return new IsAyarlari(
            Sayi(satirlar, IsAyarAnahtari.BaslangicSaat, v.BaslangicSaat,
                IsAyarAnahtari.SaatEnAz, IsAyarAnahtari.SaatEnCok),
            Sayi(satirlar, IsAyarAnahtari.VadeUfukGun, v.VadeUfukGun,
                IsAyarAnahtari.VadeEnAz, IsAyarAnahtari.VadeEnCok),
            Sayi(satirlar, IsAyarAnahtari.MasakUyariGun, v.MasakUyariGun,
                IsAyarAnahtari.MasakEnAz, IsAyarAnahtari.MasakEnCok),
            Kodlar(satirlar));
    }

    /// <summary>
    /// PATCH — yalnız VERİLEN alanlar işlenir. Değer <c>null</c> ise ayar
    /// satırı SİLİNİR ve alan yapılandırma varsayılanına döner.
    /// </summary>
    public async Task<IsAyarlari> Guncelle(AktifKullanici islem, IsAyarPatchDto dto, CancellationToken iptal = default)
    {
        var degisen = new List<string>();

        if (dto.BaslangicSaatVerildi)
            await Yaz(islem, IsAyarAnahtari.BaslangicSaat,
                Dogrula(dto.BaslangicSaat, IsAyarAnahtari.SaatEnAz, IsAyarAnahtari.SaatEnCok,
                    "Başlangıç saati 0-23 arasında olmalıdır"), degisen, iptal);

        if (dto.VadeUfukGunVerildi)
            await Yaz(islem, IsAyarAnahtari.VadeUfukGun,
                Dogrula(dto.VadeUfukGun, IsAyarAnahtari.VadeEnAz, IsAyarAnahtari.VadeEnCok,
                    "Vade ufku 1-365 gün arasında olmalıdır"), degisen, iptal);

        if (dto.MasakUyariGunVerildi)
            await Yaz(islem, IsAyarAnahtari.MasakUyariGun,
                Dogrula(dto.MasakUyariGun, IsAyarAnahtari.MasakEnAz, IsAyarAnahtari.MasakEnCok,
                    "MASAK uyarı eşiği 0-60 gün arasında olmalıdır"), degisen, iptal);

        if (dto.KapaliIslerVerildi)
        {
            string? deger = null;
            if (dto.KapaliIsler is { } liste)
            {
                var temiz = liste
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();
                if (temiz.Any(k => k.Length > 40))
                    throw new BadRequestException("İş kodu en fazla 40 karakter olabilir");
                // PAKET GOC-B (envanter C satır 19): API'den uydurma iş kodu listeye yazılabiliyordu → beyaz liste
                // (kayıtlı zamanlanmış işler). DI dışı kurulumda (isler = null) liste bilinmez → eski davranış.
                if (isler is not null)
                {
                    var bilinen = isler.Select(i => i.Kod).ToHashSet(StringComparer.Ordinal);
                    if (temiz.FirstOrDefault(k => !bilinen.Contains(k)) is { } yabanci)
                        throw new BadRequestException($"Bilinmeyen iş: {yabanci}").AlanIle("kapaliIsler");
                }
                var birlesik = string.Join(',', temiz);
                if (birlesik.Length > 200)
                    throw new BadRequestException("Kapatılan iş listesi en fazla 200 karakter olabilir");
                deger = temiz.Count == 0 ? null : birlesik;
            }
            await Yaz(islem, IsAyarAnahtari.KapaliIsler, deger, degisen, iptal);
        }

        if (degisen.Count == 0)
            throw new BadRequestException("Güncellenecek alan gönderilmedi");

        // Denetim izi YENİDEN YAZILMAZ — mevcut servis çağrılır (ADR-0021).
        // ⚠ varlik_id kısa bir kimliktir (kolon sınırı); değişen anahtarlar
        //   AÇIKLAMAYA yazılır — hangi ayarın değiştiği izde okunur kalır.
        denetim.YazmaKaydet(islem, "IsAyari", "is_ayarlari",
            $"Zamanlanmış iş ayarı güncellendi ({string.Join(", ", degisen)})", hassas: false);
        await db.SaveChangesAsync(iptal);

        return await Oku(islem.TenantId, iptal);
    }

    // ── iç ──────────────────────────────────────────────────────────────────

    private static string? Dogrula(int? deger, int enAz, int enCok, string mesaj)
    {
        if (deger is null) return null;                      // PATCH: null = sil
        if (deger < enAz || deger > enCok) throw new BadRequestException(mesaj);
        return deger.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Tek ayar satırını yazar / günceller / siler (satır SİLİNMESİ = varsayılana dönüş).</summary>
    private async Task Yaz(AktifKullanici islem, string anahtar, string? deger, List<string> degisen, CancellationToken iptal)
    {
        var kayit = await db.KiraciAyarlari
            .FirstOrDefaultAsync(a => a.TenantId == islem.TenantId && a.Anahtar == anahtar, iptal);

        if (deger is null)
        {
            // ⚠ Finansal varlık DEĞİLDİR (bir tercih satırıdır): varsayılana
            //   dönüş satırın kaldırılmasıdır. Değişiklik denetim izine düşer.
            if (kayit is null) return;
            db.KiraciAyarlari.Remove(kayit);
            degisen.Add(anahtar);
            return;
        }

        if (kayit is null)
        {
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                Anahtar = anahtar,
                Deger = deger,
                Guncellendi = DateTime.UtcNow,
            });
        }
        else
        {
            if (kayit.Deger == deger) return;
            kayit.Deger = deger;
            kayit.Guncellendi = DateTime.UtcNow;
        }
        degisen.Add(anahtar);
    }

    private static int Sayi(Dictionary<string, string> satirlar, string anahtar, int varsayilan, int enAz, int enCok) =>
        satirlar.TryGetValue(anahtar, out var ham)
        && int.TryParse(ham, System.Globalization.NumberStyles.Integer,
               System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? Math.Clamp(d, enAz, enCok)
            : varsayilan;

    private static IReadOnlyList<string> Kodlar(Dictionary<string, string> satirlar) =>
        satirlar.TryGetValue(IsAyarAnahtari.KapaliIsler, out var ham) && !string.IsNullOrWhiteSpace(ham)
            ? [.. ham.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
            : [];
}
