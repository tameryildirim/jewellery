using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// Yazma denetim izi — otomatik, unutulamaz.
//
// Okuma izi açık çağrı ister (EF SELECT'i göremez), ama YAZMA izi burada
// kendiliğinden düşer: hiçbir geliştirici "loglamayı unutamaz".
//
// ⚠ Sınır: ExecuteUpdate/ExecuteDelete SaveChanges'ı atlar, burası onları
// GÖRMEZ — KiraciYazmaDenetcisi ile aynı sınır.
//
// DEĞER YAZILMAZ, yalnız ALAN ADI yazılır. Gerekçe: denetim izinin kendisi
// TCKN/adres gibi kişisel veriyi ikinci bir yerde çoğaltmamalı (KVKK veri
// asgariliği). "Kim neye dokundu" sorusu için alan adı yeter.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DenetimYazmaDinleyicisi(
    IHttpContextAccessor erisim,
    ILogger<DenetimYazmaDinleyicisi>? gunluk = null) : SaveChangesInterceptor
{
    private readonly ILogger<DenetimYazmaDinleyicisi> _gunluk =
        gunluk ?? NullLogger<DenetimYazmaDinleyicisi>.Instance;

    // ── MAKİNE ÜRETİMİ DIŞLAMA LİSTESİ (PAKET P-72, QA turu 8 triyajı) ──────
    // Ölçülen sorun: denetim izinin %78,8'i (8437 satırın 6648'i) kur
    // zamanlayıcısının kullanıcısız KurSnapshot eklemeleriydi; "kim neye
    // dokundu" sorusu bu gürültüde kayboluyordu.
    //
    // KURAL (tek yer, bilinçli olarak TEK TÜR):
    //   • Yalnız listedeki türlerin EKLEME (Added) olayı,
    //   • yalnız KULLANICI BAĞLAMI YOKKEN (kullaniciId null → zamanlayıcı /
    //     arka plan işi) denetim izine YAZILMAZ.
    //   • MANUEL kur girişi (oturumlu kullanıcı) ve kur İPTALİ (Modified) izde
    //     KALIR; güncelleme/silme hiçbir türde dışlanmaz.
    //   • Dışlanan olay KAYBOLMAZ: teknik günlüğe Information düzeyinde düşer
    //     ("gizleme" değil "ayrı defter").
    // Açık typeof listesi — REFLEKSİYONLA "Snapshot adlı her şey" denmez;
    // listeye tür eklemek bir insan kararıdır ve DenetimIziTestleri bunu bekçiler.
    public static readonly IReadOnlyList<Type> MakineUretimiDislananTurler =
        [typeof(KurSnapshot)];

    private static bool MakineUretimiDislanir(EntityState durum, Type tur, string? kullaniciId) =>
        kullaniciId is null
        && durum == EntityState.Added
        && MakineUretimiDislananTurler.Contains(tur);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData veri, InterceptionResult<int> sonuc)
    {
        Kaydet(veri.Context);
        return sonuc;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData veri, InterceptionResult<int> sonuc, CancellationToken iptal = default)
    {
        Kaydet(veri.Context);
        return new ValueTask<InterceptionResult<int>>(sonuc);
    }

    private void Kaydet(DbContext? baglam)
    {
        if (baglam is not KuyumcuDbContext db) return;

        var kullaniciId = erisim.HttpContext?.User.FindFirst("sub")?.Value
            ?? erisim.HttpContext?.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Oturumun şube claim'i — varlıktan şube çıkarılamadığında SON ÇARE.
        // AktifKullanici ile AYNI kaynak ("sube" claim'i); yoksa null kalır.
        var oturumSubeId = erisim.HttpContext?.User.FindFirst("subeId")?.Value;

        var kayitlar = new List<DenetimKaydi>();

        foreach (var giris in db.ChangeTracker.Entries().ToList())
        {
            // Denetim kaydının kendisi denetlenmez — sonsuz döngü olurdu.
            // Ama DOKUNULMASI da engellenir: append-only kuralı burada uygulanır.
            if (giris.Entity is DenetimKaydi)
            {
                if (giris.State is EntityState.Modified or EntityState.Deleted)
                    throw new DenetimIziDegistirilemez(giris.State);
                continue;
            }
            if (giris.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var tur = giris.Metadata.ClrType.Name;

            // Kiracı: varlığın kendi TenantId'si (sistem bağlamında da doğru olur).
            var tenantId = giris.Entity switch
            {
                Tenant t => t.Id,
                IKiraciVarlik k => k.TenantId,
                _ => null,
            };
            if (string.IsNullOrEmpty(tenantId)) continue;

            var eylem = giris.State switch
            {
                EntityState.Added => DenetimEylemi.Ekleme,
                EntityState.Modified => DenetimEylemi.Guncelleme,
                _ => DenetimEylemi.Silme,
            };

            // Yalnız ALAN ADLARI — değerler bilinçli olarak yazılmaz.
            var detay = giris.State == EntityState.Modified
                ? string.Join(", ", giris.Properties
                    .Where(p => p.IsModified)
                    .Select(p => p.Metadata.Name))
                : null;

            var varlikId = giris.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

            // P-72: makine üretimi ekleme → denetim izi yerine teknik günlük.
            if (MakineUretimiDislanir(giris.State, giris.Metadata.ClrType, kullaniciId))
            {
                _gunluk.LogInformation(
                    "Denetim izi dışlaması (P-72): kullanıcı bağlamı olmayan {VarlikTuru} eklemesi " +
                    "denetim izine yazılmadı, teknik günlüğe düştü · kiracı {TenantId} · kayıt {VarlikId}",
                    tur, tenantId, varlikId);
                continue;
            }

            // ── BAĞLAM DAMGASI (paket 48 Faz A) ──────────────────────────────
            // AÇIK, TİP GÜVENLİ switch — REFLEKSİYON KULLANILMAZ. Yeni bir varlık
            // eklendiğinde damgayı derleyici değil İNSAN karar versin; refleksiyonla
            // "SubeId adlı özellik varsa al" denseydi anlamı farklı bir SubeId
            // (ör. Kullanici.SubeId = kullanıcının bağlı olduğu şube) sessizce
            // işlem şubesi gibi damgalanırdı.
            var (varlikSube, varlikKasa) = giris.Entity switch
            {
                Hareket h      => (h.SubeId, (string?)null),
                Kasa k         => (k.SubeId, k.Id),
                KasaHareket kh => ((string?)null, kh.KasaId),
                GunKapanisi g  => (g.SubeId, g.KasaId),
                Sube s         => (s.Id, (string?)null),
                // Paket 25: yerleşim satırının şubesi İŞLEM şubesidir (malın
                // fiilen durduğu yer) — anlamı Hareket.SubeId ile aynıdır.
                StokYerlesimi y => (y.SubeId, (string?)null),
                _              => ((string?)null, (string?)null),
            };
            // Varlıktan şube çıkmadıysa oturumun şubesi; o da yoksa NULL.
            // ⚠ "İlk şube" ya da "varsayılan şube" ATANMAZ (Anayasa md.1).
            var subeId = string.IsNullOrEmpty(varlikSube) ? oturumSubeId : varlikSube;

            kayitlar.Add(DenetimServisi.Olustur(
                tenantId, kullaniciId, eylem, tur, varlikId, detay, erisim,
                subeId: string.IsNullOrEmpty(subeId) ? null : subeId,
                kasaId: string.IsNullOrEmpty(varlikKasa) ? null : varlikKasa));
        }

        if (kayitlar.Count > 0) db.DenetimKayitlari.AddRange(kayitlar);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// APPEND-ONLY İHLALİ — denetim izi değiştirilemez.
//
// `DenetimIzi.cs` başlığı 2026-09-11'den beri "bu tablo güncellenmez, silinmez"
// diye YAZIYORDU, ama bunu uygulayan hiçbir kod YOKTU (ölçüldü: 2026-09-13,
// PLAN-2 §2). Yorumun iddia edip kodun tutmadığı bir kural, kural değildir.
//
// ⚠ SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı atlar, burası onları GÖRMEZ.
// `denetim.sh` bu yüzden ExecuteUpdate/Delete kullanımını beyaz listeyle sayar;
// listeye denetim_kayitlari'na dokunan bir çağrı eklenirse orada yakalanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DenetimIziDegistirilemez(EntityState durum)
    : InvalidOperationException(
        $"Denetim izi append-only'dir: {nameof(DenetimKaydi)} üzerinde " +
        $"{(durum == EntityState.Deleted ? "SİLME" : "GÜNCELLEME")} yapılamaz. " +
        "Değiştirilebilen denetim izi, denetim izi değildir.");
