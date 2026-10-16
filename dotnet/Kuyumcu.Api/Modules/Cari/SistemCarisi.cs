using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 — "PEŞİN MÜŞTERİ" SİSTEM CARİSİ (karar D2, koşullar K1–K5)
//
// Tezgâhın en sık işi adı bilinmeyen müşteriye peşin satıştır. Pusula bunu
// "Hesap Adı — varsayılan PEŞİN MÜŞTERİ" ile çözer (PUSULA-REFERANS § 3);
// taslak (Tezgah.dc.html) satış fişinin ilk alanını "Müşteri [Peşin Müşteri ▾]"
// olarak çizer.
//
// ⚠ MEHMET'İN VETO ALANI — iki kapı birden cari kimliği başına toplar
//   (MasakKapisi.GunlukIslemToplam, TevsikKapisi.GunlukNakitToplam). Paylaşılan
//   tek bir kart bu kapıları iki yönden bozardı:
//     (i)  havuzlanırsa günün bütün peşin müşterileri TEK kimlikte toplanır,
//          eşik YANLIŞ kişide patlar;
//     (ii) null gibi davranılırsa kümülatif koruma sessizce kapanır.
//   Çözüm (K1 + K2, AYNI pakette):
//     K1 — sistem carisinin satırları kümülatif havuzlara GİRMEZ (iki toplam da
//          bu kart için 0 döner);
//     K2 — açılan boşluk FAIL-CLOSED kapanır: bu kartla yapılan TEK işlem ya da
//          FİŞ TOPLAMI MASAK kimlik eşiğine YA DA tevsik haddine ulaşıyorsa sunucu
//          400 döner ("kimlik tespiti için gerçek müşteri seçin"). Kart yalnız
//          EŞİK ALTI şerittir. Eşik sayıları yapılandırmadan (Masak / Tevsik
//          bölümleri) okunur; bu dosyada da istemcide de SAYI YOKTUR.
//   K3 — açık hesap yok: veresiye bacağı reddedilir (OdemeSepeti) ve kartın
//        bakiyesi SaveChanges'ta 0'a kilitlidir (KuyumcuDbContext).
//   K4 — silinemez / pasife alınamaz / bayrak değişmez (servis + SaveChanges).
//
// ⚠ Bu bir MEVZUAT YORUMU DEĞİLDİR: yasal eşiklerin kendisine dokunulmaz;
//   bu kart için kapı, mevzuatın istediğinden DAHA ERKEN kapanır (sıkılaştırma).
//
// TOHUMLAMA (ADR-0027 deseni, "gerekiyorsa tohumla"): kart ilk ihtiyaç anında
// (GET /api/v1/cari/sistem-carisi) oluşturulur. Yarışta iki istek birden
// oluşturmaya kalkarsa veritabanı kısıtı (UQ_cariler_tenant_sistem_carisi)
// ikincisini reddeder, ikincisi mevcut kartı okur.
// ═════════════════════════════════════════════════════════════════════════════

public static class SistemCarisi
{
    /// <summary>
    /// Kartın cari türü — mevcut cari tür listesinden (yeni tür AÇILMADI).
    /// Adı bilerek "Tur" değil: denetim.sh P-08 `Tur = "…"` desenini HAREKET
    /// türü sabiti sayar; bu bir CARİ türüdür.
    /// </summary>
    public const string CariTuru = "MUSTERI";

    /// <summary>Denetim izinde varlık adı.</summary>
    public const string DenetimVarligi = "SistemCarisi";

    /// <summary>K2 reddinin ortak cümlesi — eşik türüne göre tamamlanır.</summary>
    public static string EsikMesaji(string kapi, decimal tutarTl, decimal esikTl) =>
        $"Peşin Müşteri ile bu tutarda işlem yapılamaz: {Math.Abs(tutarTl):N2} TL, "
        + $"{kapi} eşiğine ({esikTl:N2} TL) ulaşıyor. Kimlik tespiti için gerçek müşteri seçin "
        + "(ya da yeni müşteri kartı açın).";

    public const string VeresiyeYasakMesaji =
        "Peşin Müşteri'ye borç (veresiye / açık hesap) yazılamaz: fiş farkı sıfır olmalıdır. "
        + "Açık hesap için gerçek müşteri seçin.";

    public const string BakiyeYasakMesaji =
        "Peşin Müşteri kartı borç ya da alacak taşıyamaz. Bu işlem için gerçek müşteri seçin.";

    public const string SilinemezMesaji =
        "Peşin Müşteri programın kendi kartıdır: silinemez ve pasife alınamaz.";

    public const string KimlikAlaniMesaji =
        "Peşin Müşteri kimliksiz satış kartıdır; kimlik/vergi bilgisi ve siyasi nüfuz beyanı yazılamaz. "
        + "Kimliği bilinen müşteri için yeni müşteri kartı açın.";

    /// <summary>
    /// Verilen cari bu kiracının sistem carisi mi? <paramref name="cariId"/> boşsa
    /// <c>false</c>. Kiracı şartı global süzgece EK olarak elle de yazılır.
    /// </summary>
    public static async Task<bool> MiAsync(KuyumcuDbContext db, string tenantId, string? cariId)
    {
        if (string.IsNullOrEmpty(cariId)) return false;
        return await db.Cariler.AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId && c.Id == cariId && c.SistemCarisi);
    }

    /// <summary>
    /// Kiracının sistem carisini döner; yoksa oluşturur (tohumlama). Oluşturmada
    /// denetim izi düşer (<see cref="DenetimServisi.YazmaKaydet"/>, aynı SaveChanges).
    /// </summary>
    public static async Task<Data.Cari> GetirVeyaOlustur(
        KuyumcuDbContext db, DenetimServisi denetim, AktifKullanici islem)
    {
        var mevcut = await Bul(db, islem.TenantId);
        if (mevcut is not null) return mevcut;

        var now = DateTime.UtcNow;
        var kart = new Data.Cari
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            Tur = CariTuru,
            Ad = Kuyumcu.Core.I18n.PesinMusteriSozlugu.Tr.Ad,
            Aktif = true,
            SistemCarisi = true,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Cariler.Add(kart);
        // PAKET CARI-BAKIYE: sıfır bakiye satırı tek yazıcıdan.
        db.CariBakiyeler.Add(Kuyumcu.Api.Common.CariBakiyeYazici.BosSatir(islem.TenantId, kart.Id, now));
        denetim.YazmaKaydet(islem, DenetimVarligi, kart.Id,
            "Peşin Müşteri sistem carisi tohumlandı (ilk ihtiyaç anında)", hassas: false);

        try
        {
            await db.SaveChangesAsync();
            return kart;
        }
        catch (DbUpdateException)
        {
            // Yarış: başka bir istek aynı anda tohumladı ve kısıt ikinciyi reddetti.
            // İzlenen ekleri bırak, kazananı oku. Kazanan YOKSA hata başka bir
            // sebeptendir — yutulmaz, yeniden fırlatılır (Anayasa md.1).
            foreach (var giris in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
                giris.State = EntityState.Detached;
            var kazanan = await Bul(db, islem.TenantId);
            if (kazanan is null) throw;
            return kazanan;
        }
    }

    private static Task<Data.Cari?> Bul(KuyumcuDbContext db, string tenantId) =>
        db.Cariler.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SistemCarisi);
}

/// <summary>
/// Sistem carisi koruması SaveChanges'ta ihlal edildi (K3/K4). 400 döner —
/// istemci düzeltebilir: gerçek müşteri seçer ya da işlemden vazgeçer.
/// </summary>
public sealed class SistemCarisiDegistirilemez(string mesaj) : ApiException(400, mesaj);
