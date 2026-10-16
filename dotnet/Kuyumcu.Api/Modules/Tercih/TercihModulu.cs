using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Tercih;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37 — KULLANICI ARAYÜZ TERCİHLERİ (menü favorisi · menü sırası ·
// karşılama ekranı kutucuk düzeni)
//
// NEDEN SUNUCUDA, NEDEN localStorage DEĞİL: kurucu "kullanıcıya özel" dedi.
// Tarayıcı deposu CİHAZA özeldir — aynı kullanıcı ikinci tezgâhtaki bilgisayara
// geçince favorileri kaybolur, aynı cihazı paylaşan iki tezgâhtar ise birbirinin
// favorisini görür. İkisi de kurucunun istediğinin TERSİDİR.
//
// ⚠ KAPSAM: burada İŞ VERİSİ YOKTUR. Tercih yalnız arayüz çizimini etkiler;
//   hiçbir yetki/kapı bu değere bakmaz (menüde gizlenen bağlantı yetki DEĞİLDİR
//   — son sözü sunucu söyler). Bu yüzden hard DELETE serbesttir: finansal
//   varlık değildir, silinmesi defteri bozmaz (DoD sözleşmesi § finansal).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kabul edilen tercih anahtarları — BEYAZ LİSTE.</summary>
public static class TercihAnahtarlari
{
    public const string MenuFavoriler = "menu-favoriler";
    public const string MenuSirasi    = "menu-sirasi";
    public const string PanelDuzeni   = "panel-duzeni";
    // PAKET S1-b — kenar çubuğunda AÇIK olan grupların slug listesi. Yeni tablo
    // ya da migration GEREKMEZ: kullanici_tercihleri zaten anahtar→JSON metni
    // deposudur; beyaz listeye tek satır eklenir. Değer bir arayüz tercihidir,
    // hiçbir yetki/kapı ona bakmaz (M-UX3-06).
    public const string MenuAcikGruplar = "menu-acik-gruplar";
    // PAKET ŞUBE-KAPSAM-A § 4.2.4 — tüm-şube yetkili kullanıcının BAKTIĞI şube
    // (değer: şube ULID'i ya da "TUMU"). Migration YOK (anahtar→metin deposu).
    // ⚠ YETKİ DEĞİLDİR: yalnız GÖSTERİMİ daraltır; yazma yolu oturum şubesinde,
    // yasal kümülatif (tevsik/MASAK) kiracı genelinde kalır. Yazarken ve okurken
    // GorunumSubesi.Coz ile doğrulanır (sessiz genişleme yok).
    public const string GorunumSubesi = "gorunum-subesi";

    public static readonly string[] Tumu = [MenuFavoriler, MenuSirasi, PanelDuzeni, MenuAcikGruplar, GorunumSubesi];

    /// <summary>
    /// PAKET GRID-1 — kabul edilen anahtar: sabit beyaz liste YA DA ızgara deseni
    /// <c>grid:&lt;ekran&gt;:&lt;grid-id&gt;</c> (ekran bilinen listede; kural
    /// <see cref="Kuyumcu.Core.Domain.IzgaraTercihKurali"/> — istemciyle TEK kaynak).
    /// Migration YOK: kullanici_tercihleri zaten anahtar→metin deposudur.
    /// </summary>
    public static bool Gecerli(string anahtar) =>
        Tumu.Contains(anahtar) || Kuyumcu.Core.Domain.IzgaraTercihKurali.AnahtarGecerli(anahtar);

    /// <summary>Tek bir tercih değerinin en büyük uzunluğu (karakter).</summary>
    public const int EnCokUzunluk = 4000;
}

public sealed class TercihServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>Kullanıcının TÜM tercihleri — anahtar → JSON metni.</summary>
    public async Task<Dictionary<string, string>> Getir(string tenantId, string kullaniciId)
    {
        var satirlar = await db.KullaniciTercihleri
            .Where(t => t.TenantId == tenantId && t.KullaniciId == kullaniciId)
            .ToListAsync();
        return satirlar.ToDictionary(t => t.Anahtar, t => t.Deger);
    }

    /// <summary>
    /// PATCH sözleşmesi: sözlükte OLMAYAN anahtar KORUNUR, değeri <c>null</c>
    /// olan anahtar SİLİNİR, dolu olan yazılır. Tam gövdeyle üzerine yazmak
    /// (gönderilmeyen anahtarı silmek) veri imhasıdır — yapılmaz.
    /// </summary>
    public async Task<Dictionary<string, string>> Yama(
        AktifKullanici islem, Dictionary<string, string?> yama)
    {
        foreach (var (anahtar, _) in yama)
            if (!TercihAnahtarlari.Gecerli(anahtar))
                throw new BadRequestException($"Bilinmeyen tercih anahtarı: {anahtar}");

        // PAKET GRID-1 — ızgara tercihi YAZILIRKEN biçimi denetlenir (fail-closed):
        // sürüm/kolon/sıralama biçim dışıysa ya da 2000 karakteri aşıyorsa 400.
        // Genel 4000 sınırından ÖNCE koşar ki mesaj ızgaranın kendi sınırını söylesin.
        foreach (var (anahtar, deger) in yama)
        {
            if (deger is null || !Kuyumcu.Core.Domain.IzgaraTercihKurali.IzgaraAnahtariMi(anahtar)) continue;
            if (Kuyumcu.Core.Domain.IzgaraTercihKurali.Coz(deger, out var izgaraHata) is null)
                throw new BadRequestException($"Geçersiz ızgara tercihi ({anahtar}): {izgaraHata}");
        }

        foreach (var (anahtar, deger) in yama)
            if (deger is not null && deger.Length > TercihAnahtarlari.EnCokUzunluk)
                throw new BadRequestException(
                    $"Tercih değeri çok uzun ({anahtar}): en çok {TercihAnahtarlari.EnCokUzunluk} karakter");

        // PAKET GRID-1 — kullanıcı başına ızgara anahtarı en çok 200 (depo şişmesin).
        // Sayım: mevcut ızgara anahtarları + bu yamada YENİ yazılanlar − bu yamada silinenler.
        if (yama.Any(p => p.Value is not null && Kuyumcu.Core.Domain.IzgaraTercihKurali.IzgaraAnahtariMi(p.Key)))
        {
            // Sayım SQL'de (COUNT): bu yamada geçen anahtarlar hariç mevcut ızgara anahtarları
            // + bu yamada DOLU yazılan ızgara anahtarları. (Sınırsız ToListAsync yok — B-8.)
            var yamadakiIzgara = yama.Keys
                .Where(Kuyumcu.Core.Domain.IzgaraTercihKurali.IzgaraAnahtariMi).ToList();
            var digerSayi = await db.KullaniciTercihleri
                .CountAsync(t => t.TenantId == islem.TenantId && t.KullaniciId == islem.Id
                    && t.Anahtar.StartsWith(Kuyumcu.Core.Domain.IzgaraTercihKurali.Onek)
                    && !yamadakiIzgara.Contains(t.Anahtar));
            var toplam = digerSayi + yama.Count(p => p.Value is not null
                && Kuyumcu.Core.Domain.IzgaraTercihKurali.IzgaraAnahtariMi(p.Key));
            if (toplam > Kuyumcu.Core.Domain.IzgaraTercihKurali.KullaniciBasinaEnCokAnahtar)
                throw new BadRequestException(
                    $"Kişisel ızgara düzeni sınırı aşıldı: en çok {Kuyumcu.Core.Domain.IzgaraTercihKurali.KullaniciBasinaEnCokAnahtar} ızgara.");
        }

        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi YAZILMADAN ÖNCE doğrulanır —
        // kilitli rol başka şube/TUMU → 403, pasif/yabancı şube → 404. Değer
        // normalize edilir (kırpılır; "tumu" → "TUMU").
        if (yama.TryGetValue(TercihAnahtarlari.GorunumSubesi, out var gorunum) && gorunum is not null)
        {
            var deger = gorunum.Trim();
            if (deger.Length == 0)
                throw new BadRequestException("Görünüm şubesi boş olamaz; tüm şubeler için \"TUMU\" gönderin.");
            if (string.Equals(deger, Common.GorunumSubesi.Tumu, StringComparison.OrdinalIgnoreCase))
            {
                if (!SubeYetkisi.TumSubeler(islem.Rol))
                    throw new ForbiddenException("Bu şubeyi görme yetkiniz yok.");
                deger = Common.GorunumSubesi.Tumu;
            }
            else
            {
                await Common.GorunumSubesi.Coz(db, islem, deger);
            }
            yama = new Dictionary<string, string?>(yama) { [TercihAnahtarlari.GorunumSubesi] = deger };
        }

        // PAKET KATMAN-2 — EŞANLI YAZMA YARIŞI (25.09 tam tur, 5 ekranda kalıcı
        // kırmızı bant "Grubun açık/kapalı durumu kaydedilemedi"). ÖLÇÜLEN kök neden
        // (30.09, canlı 5166): satır YOKKEN aynı anahtara iki eşanlı PATCH →
        // ikisi de "satır yok" görür, ikisi de INSERT eder → benzersiz indeks
        // (tenant_id, kullanici_id, anahtar) 1062 → 409 ya da MySQL 1213 kilitlenme
        // → 500 (10 turun 10'unda). Aynı kullanıcı iki sekmede / hızlı çift
        // tıklamada ilk katlamayı yaparken olur. Çözüm: yarışı KAYBEDEN yazma
        // izleyiciyi temizleyip BAŞTAN okur (artık satırı görür → UPDATE eder).
        // Sınırlı döngü: AzamiDeneme'den sonra hata GİZLENMEZ (Anayasa md.1) —
        // eski 409/500 sözleşmesiyle çağırana döner.
        for (var deneme = 1; ; deneme++)
        {
            try
            {
                await YamayiUygula(islem, yama);
                break;
            }
            catch (Exception ex) when (deneme < AzamiDeneme && EszamanliYazmaCakismasi(ex))
            {
                db.ChangeTracker.Clear();   // yarım kalan ekleme + denetim satırı atılır, sonra yeniden kurulur
            }
        }
        return await Getir(islem.TenantId, islem.Id);
    }

    /// <summary>Eşanlı yazma yarışında en çok kaç kez baştan denenir.</summary>
    public const int AzamiDeneme = 4;

    /// <summary>
    /// Yarışı kaybeden yazmanın imzası: EF <see cref="DbUpdateException"/> (1062
    /// benzersizlik) ya da MySQL kilitlenmesi (1213 — EF bunu "transient failure"
    /// InvalidOperationException'ına sarar, içinde yine DbUpdateException vardır).
    /// Doğrulama/beyaz liste hataları (BadRequest) BURAYA GİRMEZ — onlar yeniden denenmez.
    /// </summary>
    private static bool EszamanliYazmaCakismasi(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is DbUpdateException) return true;
        return false;
    }

    private async Task YamayiUygula(AktifKullanici islem, Dictionary<string, string?> yama)
    {
        var mevcut = await db.KullaniciTercihleri
            .Where(t => t.TenantId == islem.TenantId && t.KullaniciId == islem.Id)
            .ToListAsync();
        var simdi = DateTime.UtcNow;

        foreach (var (anahtar, deger) in yama)
        {
            var satir = mevcut.FirstOrDefault(t => t.Anahtar == anahtar);
            if (deger is null)
            {
                // null = SİL (tercihi varsayılana döndür). Yoksa sessizce geçilir.
                if (satir is not null) db.KullaniciTercihleri.Remove(satir);
                continue;
            }

            if (satir is null)
            {
                db.KullaniciTercihleri.Add(new KullaniciTercihi
                {
                    Id = Kimlik.Ulid(),
                    TenantId = islem.TenantId,
                    KullaniciId = islem.Id,
                    Anahtar = anahtar,
                    Deger = deger,
                    Olusturuldu = simdi,
                    Guncellendi = simdi,
                });
            }
            else
            {
                satir.Deger = deger;
                satir.Guncellendi = simdi;
            }
        }

        // Denetim kaydı: hangi anahtarlar değişti (DEĞER yazılmaz — gereksiz).
        denetim.YazmaKaydet(
            islem, "KullaniciTercihi", islem.Id,
            $"Arayüz tercihi güncellendi: {string.Join(", ", yama.Keys.Order())}",
            hassas: false);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// PAKET ŞUBE-KAPSAM-A § 4.2.4 — kayıtlı görünüm şubesi tercihini OKURKEN çözer.
    /// Geçersiz tercih (pasif/silinmiş şube, yetki dışı şube, kilitli rolde TUMU)
    /// "Tüm şubeler"e DEĞİL, OTURUM ŞUBESİNE düşer ve <c>GecersizTercih = true</c>
    /// döner — sessiz genişleme yok. Tercih yoksa: tüm-şube rolü → tüm şubeler,
    /// kilitli rol → kendi şubesi.
    /// </summary>
    public async Task<GorunumSubesiTercihi> GorunumSubesiCoz(
        AktifKullanici islem, IReadOnlyDictionary<string, string> tercihler)
    {
        var tumSubeler = SubeYetkisi.TumSubeler(islem.Rol);
        tercihler.TryGetValue(TercihAnahtarlari.GorunumSubesi, out var kayitli);
        string? cozulen;
        var gecersiz = false;
        try
        {
            cozulen = await Common.GorunumSubesi.Coz(db, islem, kayitli);
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 403 or 404)
        {
            cozulen = islem.SubeId;
            gecersiz = true;
        }
        return new GorunumSubesiTercihi(kayitli, cozulen, tumSubeler, gecersiz,
            cozulen is null ? Common.GorunumSubesi.Tumu : cozulen);
    }

    /// <summary>Tek anahtarı sıfırla (varsayılana dön).</summary>
    public async Task<Dictionary<string, string>> Sifirla(AktifKullanici islem, string anahtar)
    {
        if (!TercihAnahtarlari.Gecerli(anahtar))
            throw new BadRequestException($"Bilinmeyen tercih anahtarı: {anahtar}");
        return await Yama(islem, new Dictionary<string, string?> { [anahtar] = null });
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// PAKET ŞUBE-KAPSAM-A — görünüm şubesi tercihinin ÇÖZÜLMÜŞ hâli.
/// <c>SubeId</c> null = tüm şubeler (birleşik). <c>TumSubeler</c> seçicinin
/// görünürlüğü için TEK kaynaktır (istemcide rol listesi kopyası tutulmaz).
/// </summary>
public sealed record GorunumSubesiTercihi(
    string? Kayitli, string? SubeId, bool TumSubeler, bool GecersizTercih, string Deger);

public sealed class TercihYamaDto
{
    /// <summary>anahtar → JSON metni. Gönderilmeyen anahtar KORUNUR, null SİLER.</summary>
    public Dictionary<string, string?>? Tercihler { get; set; }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/tercihler")]
public sealed class TercihController(TercihServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/tercihler/benim — kendi arayüz tercihlerin.</summary>
    [HttpGet("benim")]
    [HerkeseAcik("Kendi arayüz tercihin — başka kullanıcının tercihi DÖNMEZ; modül kapısı olsaydı tezgâhtar kendi menüsünü kişiselleştiremezdi")]
    public async Task<object> Benim()
    {
        var t = await servis.Getir(Islem.TenantId, Islem.Id);
        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi okurken doğrulanır (gecersizTercih).
        return new { tercihler = t, gorunumSubesi = await servis.GorunumSubesiCoz(Islem, t) };
    }

    /// <summary>PATCH /api/v1/tercihler/benim — kısmi güncelleme (yoksa koru, null ise sil).</summary>
    [HttpPatch("benim")]
    [HerkeseAcik("Kendi arayüz tercihini yazarsın — başkasınınkine erişim yok (kullanıcı kimliği jetondan gelir, gövdeden DEĞİL)")]
    public async Task<object> Yama([FromBody] TercihYamaDto dto)
    {
        // ⚠ DTO DataAnnotations yalnız controller yolunda çalışır; servis içinde
        //   anahtar beyaz listesi ve uzunluk ELLE denetlenir (kural gereği).
        var t = await servis.Yama(Islem, dto.Tercihler ?? []);
        return new
        {
            tercihler = t,
            gorunumSubesi = await servis.GorunumSubesiCoz(Islem, t),
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Tercihler kaydedildi"),
        };
    }

    /// <summary>DELETE /api/v1/tercihler/benim/{anahtar} — tek tercihi sıfırla.</summary>
    [HttpDelete("benim/{anahtar}")]
    [HerkeseAcik("Kendi tercihini sıfırlarsın — arayüz tercihidir, iş verisi değildir")]
    public async Task<object> Sil(string anahtar)
    {
        var t = await servis.Sifirla(Islem, anahtar);
        return new { tercihler = t, mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Tercih sıfırlandı") };
    }
}
