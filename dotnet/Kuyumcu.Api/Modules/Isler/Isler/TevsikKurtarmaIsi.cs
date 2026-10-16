using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// PAKET YASAL-2 (TB-16) — TEVSIK_KURTARMA: kapının kaçırdığı nakit sınırı aşımını
/// (kural kapatılmış, eski kayıt, eşzamanlı iki fiş, veri göçü) ertesi tur kendisi
/// bulur, <c>tevsik_ihlalleri</c>'ne yazar ve patrona son günüyle bildirir.
///
/// DAYANAK DOĞRULANDI (şartname § 3.1 M-6): VUK mük. md.355 — ödemeyi yapan 5 iş günü
/// içinde kendiliğinden bildirirse ödeme yapan adına ceza kesilmez → varsayılan AÇIK.
///
/// ⚠ SAPMA (çatı sözleşmesinden, gerekçeli): diğer işler veritabanına YAZMAZ, yalnız
///   taslak döndürür. Bu iş ihlal KAYDINI yazar (defter DEĞİL — Isler/ altında
///   defter yazımı = 0 kuralı korunur); tekillik UNIQUE indeksle veritabanında,
///   bildirim yazımı yine IsCalistirici'de (taslak). Yazma TevsikIhlalTarayici'dadır.
/// ⚠ KİŞİSEL VERİ: başlık/gövde ad, TCKN, belge no TAŞIMAZ (K-13).
/// ⚠ FAIL-CLOSED: TEVSIK_BILDIRIM_IS_GUNU tablodan okunamazsa iş HATALI biter.
/// </summary>
public sealed class TevsikKurtarmaIsi : IZamanlanmisIs
{
    public const string IsKodu = "TEVSIK_KURTARMA";
    public const string Rota = "/uyum?sekme=nakit";

    public string Kod => IsKodu;
    public string Ad => "Nakit sınırı aşımı taraması";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:TevsikKurtarma:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var yasal = baglam.Kapsam.GetService<YasalParametreServisi>()
            ?? throw new InvalidOperationException("Yasal parametre servisi yok — bildirim süresi okunamadı (fail-closed).");
        var kural = baglam.Kapsam.GetService<TevsikKurali>()
            ?? new TevsikKurali(new TevsikEsikleri(), yasal);
        var takvim = baglam.Kapsam.GetService<TakvimAyarlari>();

        var sonuc = await TevsikIhlalTarayici.Tara(
            baglam.Db, baglam.TenantId, baglam.AnUtc, kural,
            gun => (int)yasal.Sayi(YasalAnahtar.TevsikBildirimIsGunu, gun), takvim, iptal);

        var taslaklar = sonuc.Yeni.Select(t => new BildirimTaslagi(
            IsKodu,
            BildirimOnem.Kritik,
            TevsikIhlalTarayici.BildirimBasligi(t),
            TevsikIhlalTarayici.BildirimGovdesi(t),
            Rota, "TevsikIhlali", t.Id,
            // İhlal başına TEK bildirim — iş günde 96 kez koşsa da.
            $"TVS:{t.Id}",
            TevsikIhlalTarayici.BildirimRolleri)).ToList();

        return new IsCikti(taslaklar, sonuc.IncelenenGun);
    }
}
