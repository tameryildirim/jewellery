using System.Globalization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// MASAK ŞÜPHELİ İŞLEM BİLDİRİMİ SÜRESİ — son bildirim tarihi yaklaşan/geçmiş
/// AÇIK kayıtları hatırlatır.
///
/// ⚠ TARİH YENİDEN HESAPLANMAZ: masak_supheli_bildirimler.son_bildirim_tarihi
///   zaten üretimde hesaplanıp saklanıyor (MasakKuralMotoru.SonBildirimTarihi,
///   "10 iş günü, resmî tatiller hariç"). Bu paket o rakamı yalnız OKUR —
///   yani buraya YENİ bir mevzuat rakamı GİRMEZ. Hesabın mevzuat doğruluğu
///   MASAK paketinin sorumluluğudur.
///
/// ⚠ KİŞİSEL VERİ: başlık/gövde ad-soyad, TCKN ve belge no TAŞIMAZ; yalnız
///   kayıt kısa numarası ve kalan gün yazılır. Görünürlük SAHIP/MUDUR/MUHASEBE
///   ile sınırlıdır — TEZGÂHTAR bu bildirimi GÖRMEZ (süzme SUNUCUDA).
/// </summary>
public sealed class MasakSibSuresiIsi : IZamanlanmisIs
{
    public const string IsKodu = "MASAK_SIB_SURESI";
    public const string Rota = "/uyum";

    /// <summary>Kişisel veri taşıyan ekranlarla AYNI rol kümesi (KenarCubugu ile hizalı).</summary>
    public const string Roller = "SAHIP,MUDUR,MUHASEBE";

    public string Kod => IsKodu;
    public string Ad => "MASAK bildirim süresi";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:Masak:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        // PAKET P-11 (B-24): eşik KİRACI AYARIYLA ezilebilir; ayar yoksa
        // yapılandırma varsayılanı (Isler:Masak:UyariGun) geçerlidir.
        var ayar = await baglam.Kapsam.GetRequiredService<IsAyarServisi>()
            .Oku(baglam.TenantId, iptal);
        var uyariGun = Math.Clamp(ayar.MasakUyariGun, 0, 60);

        var sinir = baglam.AnUtc.AddDays(uyariGun);
        var kayitlar = await baglam.Db.MasakSupheliBildirimler.AsNoTracking()
            .Where(s => s.TenantId == baglam.TenantId
                     && s.Durum == SupheliDurum.Acik
                     && s.SonBildirimTarihi <= sinir)
            .OrderBy(s => s.SonBildirimTarihi)
            .Select(s => new { s.Id, s.SonBildirimTarihi })
            .Take(200)
            .ToListAsync(iptal);

        var taslaklar = new List<BildirimTaslagi>(kayitlar.Count);
        foreach (var k in kayitlar)
        {
            var gecti = k.SonBildirimTarihi < baglam.AnUtc;
            var kalanGun = (int)Math.Ceiling((k.SonBildirimTarihi - baglam.AnUtc).TotalDays);
            var kisaNo = k.Id.Length >= 8 ? k.Id[^8..] : k.Id;

            taslaklar.Add(new BildirimTaslagi(
                IsKodu,
                gecti ? BildirimOnem.Kritik : BildirimOnem.Uyari,
                gecti ? "MASAK bildirim süresi GEÇTİ" : "MASAK bildirim süresi yaklaşıyor",
                gecti
                    ? FormattableString.Invariant($"Açık şüpheli işlem kaydı ({kisaNo}) için son bildirim tarihi geçti. Uyum ekranından durumu güncelleyin.")
                    : FormattableString.Invariant($"Açık şüpheli işlem kaydı ({kisaNo}) için son bildirim tarihine {Math.Max(kalanGun, 0)} gün kaldı."),
                Rota, "MasakSupheliBildirim", k.Id,
                IsAnahtarlari.Tekillik("SIB", k.Id, baglam.YerelGun),
                Roller));
        }

        return new IsCikti(taslaklar, kayitlar.Count);
    }
}
