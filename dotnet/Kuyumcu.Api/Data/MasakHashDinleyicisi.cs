using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuyumcu.Api.Data;

/// <summary>
/// MasakKimlikTespiti kaydedilirken BelgeNoHash ve SifreSurumu'nu doldurur.
/// Interceptor'da olduğu için UNUTULAMAZ: hash'i elle set etmeyen bir servis
/// yine de aranabilir kayıt üretir. Bu aşamada BelgeNo bellekte düz metindir
/// (converter yazma sırasında uygulanır) — hash düz metinden alınır.
/// </summary>
public sealed class MasakHashDinleyicisi : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData veri, InterceptionResult<int> sonuc)
    { Doldur(veri.Context); return sonuc; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData veri, InterceptionResult<int> sonuc, CancellationToken iptal = default)
    { Doldur(veri.Context); return new ValueTask<InterceptionResult<int>>(sonuc); }

    private static void Doldur(DbContext? baglam)
    {
        if (baglam is not KuyumcuDbContext db) return;
        foreach (var giris in db.ChangeTracker.Entries<MasakKimlikTespiti>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var e = giris.Entity;
            if (string.IsNullOrWhiteSpace(e.BelgeNo))
                throw new InvalidOperationException("MasakKimlikTespiti.BelgeNo boş kaydedilemez.");
            e.BelgeNoHash = db.Sifreleyici.Hashle(e.BelgeNo);
            e.SifreSurumu = db.Sifreleyici.Surum;
        }

        // Gider pusulası satıcı TCKN'si — aynı gerekçe: unutulamaz olmalı.
        foreach (var giris in db.ChangeTracker.Entries<GiderPusulasi>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var e = giris.Entity;
            if (string.IsNullOrWhiteSpace(e.SaticiTckn))
                throw new InvalidOperationException("GiderPusulasi.SaticiTckn boş kaydedilemez.");
            e.SaticiTcknHash = db.Sifreleyici.Hashle(e.SaticiTckn);
            e.SifreSurumu = db.Sifreleyici.Surum;
        }

        // Paket 29 Faz A — kıymetli evrakın BORÇLU kimlik no'su. Aynı gerekçe:
        // hash'i servise bırakmak "her yazma noktası doldursun" âdeti olurdu ve
        // eklenen bir sonraki yol onu unuturdu; o evrak hiçbir aramada çıkmazdı.
        //
        // ⚠ FARK: BorcluKimlikNo OPSİYONELDİR (senette borçlu kimliği çoğu kez
        //   bilinmez). Boş olması hata DEĞİLDİR — hash de boş kalır. Gider
        //   pusulasındaki "boş kaydedilemez" kuralı buraya taşınmaz, çünkü
        //   orada alan zorunludur, burada değil.
        foreach (var giris in db.ChangeTracker.Entries<KiymetliEvrak>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var e = giris.Entity;
            if (string.IsNullOrWhiteSpace(e.BorcluKimlikNo))
            {
                e.BorcluKimlikNoHash = null;
                continue;
            }
            e.BorcluKimlikNoHash = db.Sifreleyici.Hashle(e.BorcluKimlikNo);
            e.SifreSurumu = db.Sifreleyici.Surum;
        }

        // ── PAKET 32 / AS-2 — cari-IBAN bağı ────────────────────────────────
        // ⚠ BURASI BENZERSİZLİĞİN DAYANAĞIDIR: (tenant_id, iban_hash) UNIQUE
        //   kısıtı bu hash üzerinde durur. Şifreli sütuna kısıt konamaz
        //   (AES-GCM her yazmada farklı nonce → aynı IBAN farklı şifreli metin).
        //   Hash'i servise bıraksaydık, eklenen bir sonraki yazma yolu onu
        //   unuttuğu anda "bir IBAN iki cariye bağlanamaz" kuralı SESSİZCE
        //   düşerdi — interceptor bunu unutulamaz kılar.
        foreach (var giris in db.ChangeTracker.Entries<CariIban>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var e = giris.Entity;
            if (string.IsNullOrWhiteSpace(e.Iban))
                throw new InvalidOperationException("CariIban.Iban boş kaydedilemez.");
            e.IbanHash = db.Sifreleyici.Hashle(e.Iban);
        }

        // ── PAKET 32 — ekstre sahne satırının karşı IBAN'ı ──────────────────
        // ⚠ FARK: KarsiIban OPSİYONELDİR (her ekstre satırında IBAN olmaz).
        //   Boş olması hata DEĞİLDİR — hash de null kalır ve o satır otomatik
        //   eşleşmeye hiç girmez.
        foreach (var giris in db.ChangeTracker.Entries<EkstreSatiri>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var e = giris.Entity;
            e.KarsiIbanHash = string.IsNullOrWhiteSpace(e.KarsiIban)
                ? null
                : db.Sifreleyici.Hashle(e.KarsiIban);
        }
    }
}
