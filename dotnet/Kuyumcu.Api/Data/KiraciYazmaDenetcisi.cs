using Kuyumcu.Api.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// Yazma tarafı kiracı koruması — ADIM1-KARAR §4 (OYBİRLİĞİ, pazarlıksız).
//
// NEDEN ŞART: `HasQueryFilter` yalnız SELECT'e WHERE ekler. Şunlar filtreyle
// TAMAMEN serbesttir:
//     db.Kasalar.Add(new Kasa { TenantId = "baskaKiraci" });   // çapraz yazma
//     izlenenKayit.TenantId = "baskaKiraci";                   // kiracı taşıma
// Faz 0'da KANITLANAN açık (başka kiracının kasasına 99.999 TL, HTTP 200) bir
// YAZMA açığıydı. Okuma filtresi tek başına onu kapatmaz.
//
// ⚠ BİLİNEN SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı ATLAR, bu denetçi
// onları GÖRMEZ. Kod tabanındaki 4 kullanım (Auth ×3, Kur ×1) zaten sistem
// kapsamındadır. Yeni bir ExecuteUpdate/Delete eklenirse bu koruma çalışmaz —
// CI denetimi bu yüzden var.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kiracı sınırının yazma tarafında ihlali. 403 döner ve loglanır — saldırı sinyalidir.</summary>
public sealed class KiraciIhlaliException(string mesaj) : ApiException(403, mesaj, "KiraciIhlali");

/// <summary>
/// ŞUBE sınırının yazma tarafında ihlali (P-04 / Y-1). Okuma süzgeci yalnız
/// SELECT'e WHERE ekler; başka şubenin satırını YAZMAYI durdurmaz. Bu istisna
/// o boşluğu kapatır ve 403 döner.
/// </summary>
public sealed class SubeIhlaliException(string mesaj) : ApiException(403, mesaj, "SubeIhlali");

public sealed class KiraciYazmaDenetcisi(ILogger<KiraciYazmaDenetcisi> gunluk) : SaveChangesInterceptor
{
    /// <summary>
    /// İhlali LOGLAYIP fırlatır. Mehmet'in şartı: bu istisna bir saldırı
    /// sinyalidir, sessizce yutulmaz.
    /// ⚠ Veritabanı denetim izine YAZILAMAZ — bu istisna SaveChanges'ı iptal
    /// ettiği için aynı işlemde eklenecek iz satırı da geri alınırdı. Bu yüzden
    /// kanal uygulama günlüğüdür.
    /// </summary>
    private KiraciIhlaliException Ihlal(string mesaj, string tur, string? kayitKiracisi, string? aktifKiraci)
    {
        gunluk.LogWarning(
            "KİRACI İHLALİ: {Mesaj} · varlık={Tur} · kayıt={KayitKiracisi} · aktif={AktifKiraci}",
            mesaj, tur, kayitKiracisi ?? "(boş)", aktifKiraci ?? "(boş)");
        return new KiraciIhlaliException(mesaj);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData veri, InterceptionResult<int> sonuc)
    {
        Denetle(veri.Context);
        return sonuc;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData veri, InterceptionResult<int> sonuc, CancellationToken iptal = default)
    {
        Denetle(veri.Context);
        return new ValueTask<InterceptionResult<int>>(sonuc);
    }

    private void Denetle(DbContext? baglam)
    {
        if (baglam is not KuyumcuDbContext db) return;

        var kiraci = db.AktifKiraciId;

        foreach (var giris in db.ChangeTracker.Entries())
        {
            if (giris.State is EntityState.Unchanged or EntityState.Detached) continue;

            // Kiracı kimliğini hangi alan taşıyor? Tenant'ta Id, diğerlerinde TenantId.
            var alan = giris.Entity switch
            {
                Tenant => nameof(Tenant.Id),
                IKiraciVarlik => nameof(IKiraciVarlik.TenantId),
                _ => null,
            };
            if (alan is null) continue;

            var ozellik = giris.Property(alan);
            var tur = giris.Metadata.ClrType.Name;

            if (giris.State == EntityState.Added)
            {
                if (db.SistemModu)
                {
                    // Sistem bağlamı DAMGALAMAZ: kiracı açıkça verilmelidir.
                    // Aksi hâlde arka plan işleri kiracısız satır üretebilirdi.
                    if (ozellik.CurrentValue is not string { Length: > 0 })
                        throw Ihlal($"Sistem bağlamında {tur}.{alan} açıkça verilmelidir.",
                            tur, null, kiraci);
                    continue;
                }

                if (ozellik.CurrentValue is not string { Length: > 0 } verilen)
                    ozellik.CurrentValue = kiraci;              // boşsa aktif kiracıyla damgala
                else if (verilen != kiraci)
                    throw Ihlal($"{tur} kaydı başka kiracıya yazılamaz (istenen: {verilen}, aktif: {kiraci}).",
                        tur, verilen, kiraci);
                continue;
            }

            // Modified | Deleted
            if (db.SistemModu) continue;

            // (a) Kaydın kiracısını taşıma girişimi
            if (ozellik.IsModified &&
                (string?)ozellik.OriginalValue != (string?)ozellik.CurrentValue)
                throw Ihlal($"{tur}.{alan} değiştirilemez (kiracı taşınamaz).",
                    tur, (string?)ozellik.OriginalValue, kiraci);

            // (b) Yabancı satırın izlenmesi — filtreyi atlatan bir yol kaldıysa son savunma
            if ((string?)ozellik.OriginalValue != kiraci)
                throw Ihlal($"{tur} kaydı bu kiracıya ait değil (kayıt: {ozellik.OriginalValue}, aktif: {kiraci}).",
                    tur, (string?)ozellik.OriginalValue, kiraci);
        }

        SubeDenetle(db);
    }

    // ── P-04 (Y-1) — ŞUBE YAZMA KAPISI ───────────────────────────────────────
    //
    // Yalnız şube süzgeci AÇIKKEN (tüm-şube ayrıcalığı olmayan oturum) çalışır.
    // Ayrıcalıklı roller zaten kiracı genelinde yazabilir; onları burada kesmek
    // şubeler arası transferi ve merkezden düzeltmeyi imkânsız kılardı.
    //
    // NULL kural: SubeId boş bırakılan YENİ kayıt aktif şubeyle DAMGALANIR
    // (kiracı damgasının şube karşılığı). Modified'da şube TAŞINAMAZ.
    private void SubeDenetle(KuyumcuDbContext db)
    {
        if (!db.SubeFiltresiAcik) return;
        var sube = db.AktifSubeId;

        foreach (var giris in db.ChangeTracker.Entries())
        {
            if (giris.State is EntityState.Unchanged or EntityState.Detached) continue;
            if (giris.Entity is not ISubeVarlik) continue;

            var ozellik = giris.Property(nameof(ISubeVarlik.SubeId));
            var tur = giris.Metadata.ClrType.Name;

            if (giris.State == EntityState.Added)
            {
                if (ozellik.CurrentValue is not string { Length: > 0 } verilen)
                {
                    // Şubesi boş bırakılabilen varlıklarda (ör. şubesiz sipariş)
                    // damgalamak anlamı değiştirirdi; yalnız NULL'lanamaz alanda damgala.
                    if (!ozellik.Metadata.IsNullable) ozellik.CurrentValue = sube;
                    continue;
                }
                if (verilen != sube)
                    throw SubeIhlali($"{tur} kaydı başka şubeye yazılamaz (istenen: {verilen}, aktif: {sube}).",
                        tur, verilen, sube);
                continue;
            }

            // Modified | Deleted
            if (ozellik.IsModified &&
                (string?)ozellik.OriginalValue != (string?)ozellik.CurrentValue)
                throw SubeIhlali($"{tur}.SubeId değiştirilemez (şube taşınamaz).",
                    tur, (string?)ozellik.OriginalValue, sube);

            var kayitSube = (string?)ozellik.OriginalValue;
            if (kayitSube is { Length: > 0 } && kayitSube != sube)
                throw SubeIhlali($"{tur} kaydı bu şubeye ait değil (kayıt: {kayitSube}, aktif: {sube}).",
                    tur, kayitSube, sube);
        }
    }

    private SubeIhlaliException SubeIhlali(string mesaj, string tur, string? kayitSubesi, string? aktifSube)
    {
        gunluk.LogWarning(
            "ŞUBE İHLALİ: {Mesaj} · varlık={Tur} · kayıt={KayitSubesi} · aktif={AktifSube}",
            mesaj, tur, kayitSubesi ?? "(boş)", aktifSube ?? "(boş)");
        return new SubeIhlaliException(mesaj);
    }
}
