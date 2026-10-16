using System.Globalization;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// SAKLAMA SÜRESİ ÖNİZLEMESİ — ADR-0008'in açık kalan ucunu kapatır:
/// "saklama_bitis hesaplanıyor ama kimse bakmıyor".
///
/// ⚠ YALNIZ ÖNİZLEME. ADR-0008 kararı "iş ELLE tetiklenir"dir ve bu paket o
///   kararı DEĞİŞTİRMEZ: burada maskeleme YAPILMAZ, yalnız "süresi dolmuş kayıt
///   var" hatırlatması üretilir. Kenarından dolanma denetim.sh sayacıyla
///   ("Isler/ altında SaklamaSuresiIsi maskeleme çağrısı = 0") ve kaynak tarama
///   testiyle engellenir.
///
/// ⚠ AYDA BİR: tekillik anahtarı yerel AY içerir — her gün dırdır etmez.
/// </summary>
public sealed class SaklamaOnizlemeIsi : IZamanlanmisIs
{
    public const string IsKodu = "SAKLAMA_ONIZLEME";
    public const string Rota = "/uyum";

    /// <summary>Kişisel veri ekranıyla aynı rol kümesi.</summary>
    public const string Roller = "SAHIP,MUDUR,MUHASEBE";

    public string Kod => IsKodu;
    public string Ad => "Saklama süresi önizlemesi";

    // PAKET YASAL-3: SAKLAMA_UYGULA maskelemeyi KENDİLİĞİNDEN yapıyorsa (Kuru=false) bu işin
    // "kendiliğinden yapılmaz" cümlesi yanlış olurdu → o durumda bu iş susar.
    public bool Etkin(IConfiguration y) => y.GetValue("Isler:Saklama:Etkin", true)
        && (!y.GetValue("Isler:SaklamaUygula:Etkin", true) || SaklamaUygulaIsi.Kuru(y));

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var servis = baglam.Kapsam.GetRequiredService<Masak.SaklamaSuresiIsi>();
        var islem = new AktifKullanici("", baglam.TenantId, null, "SAHIP");

        // Dönen anonim nesne JSON üzerinden okunur (servisin sözleşmesi budur).
        var oge = JsonSerializer.SerializeToElement(await servis.Onizleme(islem));
        var dolmus = oge.GetProperty("suresiDolmus");
        var toplam = dolmus.GetProperty("kimlikTespiti").GetInt32()
                   + dolmus.GetProperty("beyan").GetInt32();

        if (toplam == 0) return new IsCikti([], 0);

        var ay = baglam.YerelGun.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return new IsCikti(
        [
            new BildirimTaslagi(
                IsKodu, BildirimOnem.Uyari,
                "Saklama süresi dolmuş kişisel veri var",
                FormattableString.Invariant($"{toplam} uyum kaydında saklama süresi doldu. ") +
                    "Maskeleme KENDİLİĞİNDEN yapılmaz; uyum ekranından önizleyip onaylamanız gerekir.",
                Rota, null, null,
                $"SAKLAMA:{ay}",
                Roller),
        ], toplam);
    }
}
