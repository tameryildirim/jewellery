using System.Globalization;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Vade;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// VADE HATIRLATMA — geciken ve ufka giren veresiye borçlarını hatırlatır.
///
/// Kaynak VadeKapisi.AcikBorclar'dır (RaporModulu'ndaki vade-takibi raporuyla
/// AYNI hesap). Kod KOPYALANMAZ: iki yerde iki formül olsaydı rapor ile bildirim
/// birbirini tutmazdı ve hangisinin doğru olduğu cevapsız kalırdı.
///
/// ⚠ GÜRÜLTÜ SINIRI: cari başına GÜNDE TEK bildirim (tekillik anahtarında yerel
///   gün var). Cari sayısı Isler:Vade:MaksCari'yi aşarsa tek tek bildirim
///   üretilmez, TEK ÖZET bildirim çıkar (TB-19).
/// ⚠ KİŞİSEL VERİ: gövdede müşteri adı/telefonu GEÇMEZ; kimlik yalnız
///   ilgili_varlik_id'dedir ve hedef rota vade takibi ekranıdır.
/// </summary>
public sealed class VadeHatirlatmaIsi : IZamanlanmisIs
{
    public const string IsKodu = "VADE_HATIRLATMA";
    public const string Rota = "/raporlar/vade-takibi";

    public string Kod => IsKodu;
    public string Ad => "Vade hatırlatma";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:Vade:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var y = baglam.Kapsam.GetRequiredService<IConfiguration>();
        // PAKET P-11 (B-24): eşik artık KİRACI AYARIYLA ezilebilir; ayar yoksa
        // yapılandırma varsayılanı geçerlidir (IsAyarServisi.Varsayilanlar).
        var ayar = await baglam.Kapsam.GetRequiredService<IsAyarServisi>()
            .Oku(baglam.TenantId, iptal);
        var ufukGun = Math.Clamp(ayar.VadeUfukGun, 0, 365);
        var maksCari = Math.Clamp(y.GetValue("Isler:Vade:MaksCari", 500), 1, 5000);

        var db = baglam.Db;
        var yerelGun = baglam.YerelGun;

        // Yalnız BİZE borçlu cariler (pozitif TL bakiye). Tedarikçiye olan
        // borcumuz bu hatırlatmanın konusu değildir.
        var borcluCariler = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == baglam.TenantId && b.TlBakiye > 0m)
            .OrderByDescending(b => b.TlBakiye)
            .Select(b => b.CariId)
            .Take(maksCari + 1)
            .ToListAsync(iptal);

        // Sunucu sınırına DEĞDİ: tek tek bildirim yerine tek özet (TB-19).
        if (borcluCariler.Count > maksCari)
        {
            return new IsCikti(
            [
                new BildirimTaslagi(
                    IsKodu, BildirimOnem.Uyari,
                    "Vadesi takip edilen müşteri sayısı sınırı aştı",
                    FormattableString.Invariant(
                        $"Açık veresiye bakiyesi olan müşteri sayısı {maksCari} sınırını aştı. ") +
                        "Tek tek hatırlatma üretilmedi; listenin tamamı vade takibi ekranındadır.",
                    Rota, null, null,
                    IsAnahtarlari.Tekillik("VADE-OZET", maksCari.ToString(CultureInfo.InvariantCulture), yerelGun),
                    null),
            ], borcluCariler.Count);
        }

        var taslaklar = new List<BildirimTaslagi>();
        foreach (var cariId in borcluCariler)
        {
            iptal.ThrowIfCancellationRequested();
            var (borclar, _) = await VadeKapisi.AcikBorclar(db, baglam.TenantId, cariId, baglam.AnUtc);

            var geciken = borclar.Where(b => b.GecikmeGun > 0).ToList();
            var yaklasan = borclar.Where(b => b.GecikmeGun <= 0 && -b.GecikmeGun <= ufukGun).ToList();
            if (geciken.Count == 0 && yaklasan.Count == 0) continue;

            var kritik = geciken.Count > 0;
            var tutar = (kritik ? geciken : yaklasan).Sum(b => b.AcikTutar);
            var gun = kritik ? geciken.Max(b => b.GecikmeGun) : yaklasan.Min(b => -b.GecikmeGun);

            taslaklar.Add(new BildirimTaslagi(
                IsKodu,
                kritik ? BildirimOnem.Kritik : BildirimOnem.Uyari,
                kritik ? "Vadesi geçmiş veresiye borcu" : "Vadesi yaklaşan veresiye borcu",
                kritik
                    ? FormattableString.Invariant($"Bir müşterinin {tutar:0.00} TL tutarındaki borcu {gun} gündür gecikmiş. Ayrıntı için vade takibi ekranını açın.")
                    : FormattableString.Invariant($"Bir müşterinin {tutar:0.00} TL tutarındaki borcunun vadesine {gun} gün kaldı. Ayrıntı için vade takibi ekranını açın."),
                Rota, "Cari", cariId,
                IsAnahtarlari.Tekillik("VADE", cariId, yerelGun),
                null));
        }

        return new IsCikti(taslaklar, borcluCariler.Count);
    }
}
