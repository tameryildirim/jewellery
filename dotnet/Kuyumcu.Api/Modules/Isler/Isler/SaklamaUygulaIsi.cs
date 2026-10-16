using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Isler.Isler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 (§ 4.3) — SAKLAMA_UYGULA: saklama süresi dolan MASAK kimlik
// verisini KENDİLİĞİNDEN maskeler (ADR-0008 güncellemesi).
//
// DAYANAK (birincil, şartname § 3.1): Tedbirler Yön. md.46 (8 yıl, SON İŞLEM
// tarihinden); KVKK Silme/Anonimleştirme Yön. md.7 (şart kalkınca RESEN) ve
// md.11/2 (periyodik imha aralığı en çok 6 ay) — iş GÜNLÜK koştuğu için sınır
// her zaman sağlanır.
//
// Tur başına:
//   1. Önümüzdeki 30 gün içinde gizlenecek kayıt varsa SAHİP'e BİR bildirim
//      ("N kaydın kimlik bilgileri 30 gün sonra kendiliğinden gizlenecek").
//      Tekillik anahtarı pencerenin EN YENİ adayına bağlıdır: aynı küme iki kez
//      bildirilmez; ufka yeni kayıt girince yeni bildirim çıkar.
//   2. Kuru=false ise süresi dolmuş + bekletmesiz + son-işlem kuralını geçen
//      kayıtlar SaklamaSuresiIsi.Uygula ile maskelenir (TEK MOTOR; denetim izine
//      "otomatik" etiketiyle yazılır).
// ⚠ Kişisel veri bildirime YAZILMAZ — yalnız sayılar.
// ⚠ Isler/ ağacında Uygula çağrısı YALNIZ bu dosyada ve TEK kezdir (denetim.sh +
//   IsCatisiSafTestleri M6-6 sayar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SaklamaUygulaIsi : IZamanlanmisIs
{
    public const string IsKodu = "SAKLAMA_UYGULA";
    public const string Rota = "/uyum";
    public const string Roller = "SAHIP";

    public string Kod => IsKodu;
    public string Ad => "Saklama süresi dolan kimlik bilgilerini gizleme";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:SaklamaUygula:Etkin", true);

    /// <summary>Kuru koşum: yalnız bildirir, maskelemez (VARSAYILAN — geri alınamaz işlem).</summary>
    public static bool Kuru(IConfiguration y) => y.GetValue("Isler:SaklamaUygula:Kuru", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var servis = baglam.Kapsam.GetRequiredService<Masak.SaklamaSuresiIsi>();
        var yapilandirma = baglam.Kapsam.GetRequiredService<IConfiguration>();
        var islem = new AktifKullanici("", baglam.TenantId, null, "SAHIP");
        var taslaklar = new List<BildirimTaslagi>();

        // 1) 30 gün önceden haber.
        var an = baglam.AnUtc;
        var yaklasan = await servis.AdaylariBul(baglam.TenantId, an,
            an.AddDays(Masak.SaklamaSuresiIsi.OncedenHaberGun));
        if (yaklasan.TespitIdleri.Count > 0)
            taslaklar.Add(new BildirimTaslagi(
                IsKodu, BildirimOnem.Bilgi,
                "Kimlik bilgileri yakında gizlenecek",
                FormattableString.Invariant($"{yaklasan.TespitIdleri.Count} kaydın kimlik bilgileri 30 gün sonra kendiliğinden gizlenecek. ")
                    + "Yasal bir nedenle saklamanız gerekiyorsa Uyum ekranından gerekçe yazıp bekletin.",
                Rota, null, null,
                $"SAKLAMA_YAKIN:{yaklasan.EnYeniAdayId}",
                Roller));

        // 2) Kendiliğinden maskeleme (kuru değilse).
        var maskelenen = 0;
        if (!Kuru(yapilandirma))
        {
            var sonuc = System.Text.Json.JsonSerializer.SerializeToElement(await servis.Uygula(islem, otomatik: true));
            maskelenen = sonuc.GetProperty("maskelenen").GetProperty("kimlikTespiti").GetInt32()
                       + sonuc.GetProperty("maskelenen").GetProperty("beyan").GetInt32();
            if (maskelenen > 0)
                taslaklar.Add(new BildirimTaslagi(
                    IsKodu, BildirimOnem.Bilgi,
                    "Süresi dolan kimlik bilgileri gizlendi",
                    FormattableString.Invariant($"{maskelenen} kayıtta kimlik bilgileri gizlendi; tutar ve tarih korunuyor."),
                    Rota, null, null,
                    $"SAKLAMA_UYGULANDI:{baglam.YerelGun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    Roller));
        }

        return new IsCikti(taslaklar, yaklasan.TespitIdleri.Count + maskelenen);
    }
}
