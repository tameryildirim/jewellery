using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ SİSTEM SERVİSİ — PAKET P-28. Operatör tarafı.
//
// ⚠ NEDEN AYRI SINIF: /api/v1/sistem/* uçları ANONİMDİR; istek kapsamında
//   kiracı bağlamı YOKTUR ve request-scoped KuyumcuDbContext ÇÖZÜLEMEZ.
//   AbonelikSistemServisi ile AYNI kalıp: yalnız SistemKapsami alınır.
//
// ⚠ DESTEK DAKİKASI DÜZELTİLEBİLİR (DoD sözleşmesi): POST ile doğar, PATCH
//   ile düzelir (gönderilmeyen alan KORUNUR, null olan SİLİNİR), pasife
//   alınır ve yeniden aktifleştirilir. HARD DELETE YOKTUR.
//
// ⚠ HER YAZMA İZ BIRAKIR: DenetimServisi.SistemYazmaKaydet çağrılır
//   (yeniden yazılmaz); damga <see cref="IzDamgasi"/>'dır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kapasite paydası satırı — bir kiracının ölçülmüş toplam yükü.</summary>
public sealed record KapasiteSatiriDto(
    string TenantId, string KiraciAdi, bool Aktif,
    TelemetriOzetDto Kullanim, DepolamaOzetDto Depolama, DestekOzetDto Destek);

/// <summary>
/// F-8 "kiracı başı kaynak tüketimi" satırının ÖLÇÜM ÇIKTISI.
/// <para><c>OlcumVarMi=false</c> ise belgeye rakam YAZILAMAZ (FAIL-CLOSED 1).</para>
/// </summary>
public sealed record KapasiteOzetiDto(
    DateTime OlcumAniUtc, int GunSayisi, int KiraciSayisi, int OlculenKiraciSayisi,
    bool OlcumVarMi,
    long ToplamIstek, long ToplamIslemMs, long ToplamYanitBayt, long ToplamSatir,
    long ToplamDestekDakika,
    double KiraciBasiIstek, double KiraciBasiIslemMs, double KiraciBasiSatir,
    int Sinir, bool SinirinaDegdi,
    IReadOnlyList<KapasiteSatiriDto> Kiracilar,
    string Kisit);

public sealed class TelemetriSistemServisi(SistemKapsami kapsam)
{
    /// <summary>Denetim izindeki varlık türü — SQL süzgeci: varlik_turu='DestekDakikasi'.</summary>
    public const string DenetimVarligi = nameof(DestekDakikasi);

    /// <summary>Denetim izi damgası → detay LIKE 'DESTEK_DAKIKASI%'.</summary>
    public const string IzDamgasi = "DESTEK_DAKIKASI";

    /// <summary>Kapasite özetinde taranan kiracı tavanı (B-8: sınırsız liste yok).</summary>
    public const int KiraciTavani = 500;

    /// <summary>Destek kaydı listesi tavanı (UX-S29: sınır yanıtta YAZILIDIR).</summary>
    public const int DestekTavani = 200;

    /// <summary>Tek kalemde kabul edilen en uzun destek süresi (dakika).</summary>
    public const int DakikaTavani = 600;

    public const string OlcumKisiti =
        "Bu rakamlar YALNIZ bu kurulumda ölçülen pencereyi kapsar; üretim kapasitesi "
        + "taahhüdü değildir. Destek dakikası yalnız KAYDEDİLEN kalemleri içerir — "
        + "kaydedilmemiş destek ölçüme girmez ve bu eksiklik gizlenmez.";

    // ── OKUMA ────────────────────────────────────────────────────────────────

    /// <summary>Tek kiracının ölçülmüş yükü.</summary>
    public async Task<KapasiteSatiriDto> KiraciOzet(string tenantId, int? gun)
    {
        var g = TelemetriServisi.GunNormalle(gun);
        var now = DateTime.UtcNow;
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new NotFoundException("Kiracı bulunamadı");

        return new KapasiteSatiriDto(
            tenant.Id, tenant.Ad, tenant.Aktif,
            await TelemetriServisi.Ozet(db, tenantId, g, now),
            await TelemetriServisi.Depolama(db, tenantId),
            await TelemetriServisi.DestekOzet(db, tenantId, g, now));
    }

    /// <summary>
    /// F-8 kapasite paydası. Kiracı BAŞINA ortalamalar YALNIZ ölçümü olan
    /// kiracılara bölünür — hiç istek görmemiş kiracı paydayı şişirip rakamı
    /// iyi göstermez (md.1).
    /// </summary>
    public async Task<KapasiteOzetiDto> KapasiteOzeti(int? gun)
    {
        var g = TelemetriServisi.GunNormalle(gun);
        var now = DateTime.UtcNow;
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        var kiracilar = await db.Tenants.AsNoTracking()
            .OrderBy(t => t.Olusturuldu).Take(KiraciTavani + 1)
            .Select(t => new { t.Id, t.Ad, t.Aktif }).ToListAsync();
        var degdi = kiracilar.Count > KiraciTavani;
        if (degdi) kiracilar = kiracilar.Take(KiraciTavani).ToList();

        var satirlar = new List<KapasiteSatiriDto>(kiracilar.Count);
        foreach (var k in kiracilar)
        {
            satirlar.Add(new KapasiteSatiriDto(
                k.Id, k.Ad, k.Aktif,
                await TelemetriServisi.Ozet(db, k.Id, g, now),
                await TelemetriServisi.Depolama(db, k.Id),
                await TelemetriServisi.DestekOzet(db, k.Id, g, now)));
        }

        var olculen = satirlar.Count(s => s.Kullanim.OlcumVarMi);
        long istek = satirlar.Sum(s => s.Kullanim.IstekSayisi);
        long ms = satirlar.Sum(s => s.Kullanim.IslemMs);
        long bayt = satirlar.Sum(s => s.Kullanim.YanitBayt);
        long satir = satirlar.Sum(s => s.Depolama.ToplamSatir);
        long destek = satirlar.Sum(s => s.Destek.ToplamDakika);

        return new KapasiteOzetiDto(
            now, g, satirlar.Count, olculen,
            OlcumVarMi: olculen > 0,
            istek, ms, bayt, satir, destek,
            KiraciBasiIstek: olculen == 0 ? 0 : Math.Round((double)istek / olculen, 2),
            KiraciBasiIslemMs: olculen == 0 ? 0 : Math.Round((double)ms / olculen, 2),
            KiraciBasiSatir: olculen == 0 ? 0 : Math.Round((double)satir / olculen, 2),
            Sinir: KiraciTavani, SinirinaDegdi: degdi,
            Kiracilar: satirlar,
            Kisit: OlcumKisiti);
    }

    /// <summary>Kiracının destek kayıtları (pasifler DAHİL — düzeltme yolu görünsün).</summary>
    public async Task<object> DestekListe(string tenantId)
    {
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var ham = await db.Set<DestekDakikasi>().AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.Tarih).ThenByDescending(d => d.Olusturuldu)
            .Take(DestekTavani + 1).ToListAsync();
        var degdi = ham.Count > DestekTavani;
        var liste = ham.Take(DestekTavani).Select(Yansit).ToList();
        return new
        {
            tenantId,
            kayitlar = liste,
            sinir = DestekTavani,
            sinirinaDegdi = degdi,
            kanallar = DestekKanallari.Hepsi,
            konular = DestekKonulari.Hepsi,
        };
    }

    // ── YAZMA ────────────────────────────────────────────────────────────────

    public async Task<DestekDakikasiDto> DestekEkle(string tenantId, DestekDakikasiEkleDto dto, string operatorAdi)
    {
        var op = Operator(operatorAdi);
        var now = DateTime.UtcNow;

        // Servis içi ELLE doğrulama (DataAnnotations yalnız controller yolunda).
        var kanal = (dto.Kanal ?? "").Trim();
        var konu = (dto.Konu ?? "").Trim();
        if (!DestekKanallari.Gecerli(kanal))
            throw new BadRequestException($"Geçersiz kanal. Geçerli kanallar: {string.Join(", ", DestekKanallari.Hepsi)}");
        if (!DestekKonulari.Gecerli(konu))
            throw new BadRequestException($"Geçersiz konu. Geçerli konular: {string.Join(", ", DestekKonulari.Hepsi)}");
        var kaynak = string.IsNullOrWhiteSpace(dto.Kaynak) ? DestekKaynaklari.Manuel : dto.Kaynak.Trim();
        if (!DestekKaynaklari.Gecerli(kaynak))
            throw new BadRequestException($"Geçersiz kaynak. Geçerli kaynaklar: {string.Join(", ", DestekKaynaklari.Hepsi)}");
        DakikaDogrula(dto.Dakika);
        var tarih = TarihDogrula(dto.Tarih ?? now, now);

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        _ = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new NotFoundException("Kiracı bulunamadı");

        var satir = new DestekDakikasi
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Tarih = tarih,
            Dakika = dto.Dakika,
            Kanal = kanal,
            Konu = konu,
            Aciklama = Kirp(dto.Aciklama),
            Kaynak = kaynak,
            Aktif = true,
            OperatorAdi = op,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Set<DestekDakikasi>().Add(satir);

        sistem.ServiceProvider.GetRequiredService<DenetimServisi>().SistemYazmaKaydet(
            tenantId, DenetimVarligi, satir.Id,
            $"{IzDamgasi}: EKLENDI · {satir.Dakika} dk · kanal: {satir.Kanal}"
            + $" · konu: {satir.Konu} · kaynak: {satir.Kaynak} · operatör: {op}");

        await db.SaveChangesAsync();
        return Yansit(satir);
    }

    /// <summary>ÜÇ DURUMLU PATCH: gövdede yoksa KORU · null ise SİL.</summary>
    public async Task<DestekDakikasiDto> DestekDuzenle(
        string tenantId, string kayitId, DestekDakikasiGuncelleDto dto, string operatorAdi)
    {
        var op = Operator(operatorAdi);
        var now = DateTime.UtcNow;

        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var satir = await db.Set<DestekDakikasi>()
            .FirstOrDefaultAsync(d => d.Id == kayitId && d.TenantId == tenantId)
            ?? throw new NotFoundException("Destek kaydı bulunamadı");

        var degisim = new List<string>();

        if (dto.DakikaVerildi)
        {
            if (dto.Dakika is null) throw new BadRequestException("Dakika silinemez, yeni bir değer gönderin");
            DakikaDogrula(dto.Dakika.Value);
            if (dto.Dakika.Value != satir.Dakika) degisim.Add($"dakika {satir.Dakika}→{dto.Dakika.Value}");
            satir.Dakika = dto.Dakika.Value;
        }
        if (dto.KanalVerildi)
        {
            var k = (dto.Kanal ?? "").Trim();
            if (!DestekKanallari.Gecerli(k))
                throw new BadRequestException($"Geçersiz kanal. Geçerli kanallar: {string.Join(", ", DestekKanallari.Hepsi)}");
            if (k != satir.Kanal) degisim.Add($"kanal {satir.Kanal}→{k}");
            satir.Kanal = k;
        }
        if (dto.KonuVerildi)
        {
            var k = (dto.Konu ?? "").Trim();
            if (!DestekKonulari.Gecerli(k))
                throw new BadRequestException($"Geçersiz konu. Geçerli konular: {string.Join(", ", DestekKonulari.Hepsi)}");
            if (k != satir.Konu) degisim.Add($"konu {satir.Konu}→{k}");
            satir.Konu = k;
        }
        if (dto.TarihVerildi)
        {
            if (dto.Tarih is null) throw new BadRequestException("Tarih silinemez, yeni bir değer gönderin");
            var t = TarihDogrula(dto.Tarih.Value, now);
            if (t != satir.Tarih) degisim.Add($"tarih {satir.Tarih:yyyy-MM-dd}→{t:yyyy-MM-dd}");
            satir.Tarih = t;
        }
        if (dto.AciklamaVerildi)
        {
            // null → SİL (sözleşme gereği); gövdede hiç yoksa buraya GİRİLMEZ.
            var a = Kirp(dto.Aciklama);
            if (a != satir.Aciklama) degisim.Add(a is null ? "açıklama silindi" : "açıklama güncellendi");
            satir.Aciklama = a;
        }
        if (dto.AktifVerildi)
        {
            if (dto.Aktif is null) throw new BadRequestException("Aktiflik silinemez, true/false gönderin");
            if (dto.Aktif.Value != satir.Aktif)
                degisim.Add(dto.Aktif.Value ? "yeniden AKTİF" : "PASİFE alındı");
            satir.Aktif = dto.Aktif.Value;
        }

        if (degisim.Count == 0)
            throw new BadRequestException("Değişiklik gönderilmedi: en az bir alan güncellenmelidir");

        satir.Guncellendi = now;

        sistem.ServiceProvider.GetRequiredService<DenetimServisi>().SistemYazmaKaydet(
            tenantId, DenetimVarligi, satir.Id,
            $"{IzDamgasi}: GUNCELLENDI · {string.Join(" · ", degisim)} · operatör: {op}");

        await db.SaveChangesAsync();
        return Yansit(satir);
    }

    /// <summary>
    /// PASİFE ALMA — hard delete YOKTUR. Kayıt durur, ölçüme sayılmaz.
    /// <paramref name="aktif"/> true ile aynı uç geri dönüş yoludur.
    /// </summary>
    public Task<DestekDakikasiDto> DestekAktiflik(string tenantId, string kayitId, bool aktif, string operatorAdi) =>
        DestekDuzenle(tenantId, kayitId, new DestekDakikasiGuncelleDto { Aktif = aktif }, operatorAdi);

    // ── YARDIMCILAR ──────────────────────────────────────────────────────────

    private static string Operator(string? op)
    {
        var o = (op ?? "").Trim();
        if (o.Length < 3) throw new BadRequestException("Operatör kimliği zorunludur (en az 3 karakter)");
        return o;
    }

    private static void DakikaDogrula(int dakika)
    {
        if (dakika <= 0) throw new BadRequestException("Dakika sıfırdan büyük olmalıdır");
        if (dakika > DakikaTavani)
            throw new BadRequestException($"Tek kalemde en fazla {DakikaTavani} dakika girilebilir; günü kalemlere bölün");
    }

    /// <summary>Gün başına yuvarlar; GELECEK tarih reddedilir (ölçüm geriye dönüktür).</summary>
    private static DateTime TarihDogrula(DateTime tarih, DateTime now)
    {
        var g = new DateTime(tarih.Year, tarih.Month, tarih.Day, 0, 0, 0, DateTimeKind.Utc);
        if (g > now.Date) throw new BadRequestException("Destek tarihi gelecekte olamaz");
        return g;
    }

    private static string? Kirp(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DestekDakikasiDto Yansit(DestekDakikasi d) => new(
        d.Id, d.TenantId, d.Tarih, d.Dakika, d.Kanal, d.Konu, d.Aciklama,
        d.Kaynak, d.Aktif, d.OperatorAdi, d.Olusturuldu, d.Guncellendi);
}
