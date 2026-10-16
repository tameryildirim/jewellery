using Kuyumcu.Core.Domain;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Grid;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — IZGARA TERCİHİNİN İSTEMCİ SERVİSİ
//
// Depo: mevcut /tercihler/benim uçları (P-37 kullanıcı tercihleri). Yeni uç ve
// tablo YOKTUR. Anahtar grid:<ekran>:<grid-id>; kullanıcı kimliği JETONDAN
// gelir (gövdeden değil) → başkasının tercihi okunamaz/yazılamaz.
//
// ⚠ NEDEN DI KAYDI YOK: ızgara ApiIstemci'yi zaten alır; servis onu sarar.
//   Program.cs'e ve bütün bUnit ekran ortamlarına yeni kayıt eklemek
//   gerekmez (bir ekrana ızgara gelince o ekranın testleri kırılmaz).
// ⚠ OKUMA fail-safe: tercih yok/bozuk/eski → null (varsayılan düzen); ağ
//   hatası ise OkunamadiMi=true döner (ekran not düşer, düzeni BOŞALTMAZ — UX-S28).
// ⚠ YAZMA hata gizlemez: istisna çağırana ulaşır, ızgara yerel değişikliği geri alır.
// ═════════════════════════════════════════════════════════════════════════════

// GRID-1 tur 3 (doğrulayıcı DÜŞÜK ×2): açılışta /tercihler/benim 2-4 kez okunuyordu (sayfanın
//   ön okuması + ızgaranın kendi okuması + liste boşalıp dolunca ızgaranın yeniden kurulması).
//   Artık sayfa TEK istemci örneği tutar ve ızgaraya PARAMETRE olarak verir (Izgara.Istemci);
//   örnek başarılı okumayı ÖNBELLEKLER, kendi yazma/silmesiyle önbelleği günceller → sayfa
//   açılışında TEK GET. Okuma HATASI önbelleğe alınmaz (sonraki okuma yeniden dener).

public sealed class IzgaraTercihIstemcisi(ApiIstemci api)
{
    private sealed record TercihCevap(Dictionary<string, string>? Tercihler);

    /// <summary>Okuma sonucu: kayıtlı ham değer var mı, çözülebildi mi, ağ hatası mı.</summary>
    public sealed record OkumaSonucu(IzgaraTercihi? Tercih, bool KayitVar, bool OkunamadiMi);

    /// <summary>Son başarılı okumanın ham tercihleri (bu örneğin yazma/silmesiyle güncel tutulur).</summary>
    private Dictionary<string, string>? onbellek;

    /// <summary>Bu örnek kaç kez sunucudan okudu (test/ölçüm için).</summary>
    public int OkumaSayisi { get; private set; }

    public async Task<OkumaSonucu> OkuAsync(string anahtar)
    {
        if (onbellek is null)
        {
            try
            {
                OkumaSayisi++;
                var cevap = await api.GetAsync<TercihCevap>("/tercihler/benim");
                onbellek = cevap?.Tercihler is null
                    ? new(StringComparer.Ordinal)
                    : new(cevap.Tercihler, StringComparer.Ordinal);
            }
            catch (Exception)
            {
                return new(null, false, true);
            }
        }
        if (!onbellek.TryGetValue(anahtar, out var ham))
            return new(null, false, false);
        // Bozuk değer → null (varsayılan düzen); kayıt yine VAR sayılır ki
        // "Varsayılana dön" onu silebilsin. Tercih SİLİNMEZ (envanter § G1).
        return new(IzgaraTercihKurali.Coz(ham, out _), true, false);
    }

    /// <summary>
    /// GRID-1 tur 2 (doğrulayıcı ORTA): sayfa listeyi İLK KEZ istemeden önce kayıtlı
    /// sıralamayı okur → liste tek istekte, doğru sırayla gelir. Okuma hatası/bozuk/eski
    /// kayıt → null (varsayılan sıra). Okunan tercih önbellekte kalır; aynı örnek ızgaraya
    /// verilince ızgara İKİNCİ GET atmaz.
    /// </summary>
    public async Task<IzgaraSiralama?> KayitliSiralamaAsync<T>(string ekran, string gridId,
        IReadOnlyList<IzgaraKolonu<T>> kolonlar, int surum = 1)
    {
        var sonuc = await OkuAsync(IzgaraTercihKurali.Anahtar(ekran, gridId));
        var duzen = IzgaraDuzeni.Kur(IzgaraKolonu<T>.Tanimlar(kolonlar), sonuc.Tercih, surum);
        return IzgaraKolonu<T>.SiralamaKarsiligi(kolonlar, duzen.Siralama);
    }

    /// <summary>PATCH — yalnız bu anahtar (gönderilmeyen anahtarlar KORUNUR).</summary>
    public async Task YazAsync(string anahtar, IzgaraTercihi tercih)
    {
        var deger = IzgaraTercihKurali.Yaz(tercih);
        await api.PatchAsync<TercihCevap>("/tercihler/benim",
            new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
        if (onbellek is not null) onbellek[anahtar] = deger;
    }

    /// <summary>DELETE — "Varsayılana dön" (mevcut uç; arayüz tercihi, iş verisi değil).</summary>
    public async Task SilAsync(string anahtar)
    {
        await api.DeleteAsync<TercihCevap>($"/tercihler/benim/{Uri.EscapeDataString(anahtar)}");
        onbellek?.Remove(anahtar);
    }
}
