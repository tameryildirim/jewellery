using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Kurulum;

// ═════════════════════════════════════════════════════════════════════════════
// AÇILIŞ KAYNAKLARI — paket T-2 Faz A
//
// Bu dosya SAF'tır: veritabanı GÖRMEZ. İçinde satır sözleşmesi, kültürden
// bağımsız doğrulama ve prova jetonunun kanonik metni vardır.
//
// ⚠ NEDEN AYRI DOSYA: kuru provanın "hiçbir şey yazmaz" sözü ancak doğrulamanın
//   yazma yolundan FİZİKSEL OLARAK AYRI durmasıyla güvenilir olur. Buradaki
//   hiçbir fonksiyon DbContext almaz; alamaz.
//
// ⚠ FAZ A / FAZ B SINIRI (dürüstçe yazılı): Faz A'da açılış satırları sihirbaz
//   formundan YAPILANDIRILMIŞ olarak gelir. CSV şablonu ve toplu yükleme
//   (47-A'nın CsvOkuyucu'su ve IceAktarimKapisi'yle) Faz B'dedir; o yol
//   açıldığında bu dosyadaki kolon tanımları ve doğrulama AYNEN kullanılır —
//   ikinci bir doğrulama kopyası yazılmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Satır hata kodları — ekranda sözlükten çevrilir (UX-S31).</summary>
public static class AcilisHataKodu
{
    public const string HedefYok = "HEDEF_YOK";
    public const string HedefBulunamadi = "HEDEF_BULUNAMADI";
    public const string SifirSatir = "SIFIR_SATIR";
    public const string HasBacagiYok = "HAS_BACAGI_YOK";
    public const string YinelenenSatir = "YINELENEN_SATIR";
    public const string ZatenAcilis = "ZATEN_ACILIS";
    public const string DovizHesapFazB = "DOVIZ_HESAP_FAZ_B";
    public const string TutarAsiri = "TUTAR_ASIRI";
    public const string PasifHedef = "PASIF_HEDEF";

    /// <summary>
    /// QA turu 5 (YÜKSEK): kuru prova −250 ₺ kasa açılışını "geçerli" sayıyordu.
    /// Kasa/banka açılış bakiyesi fiziken negatif OLAMAZ (kasada eksi para,
    /// bankada eksi mevduat yoktur; kredili hesap ayrı bir üründür ve Faz B).
    /// CARİ açılışında negatif ANLAMLIDIR (müşteri alacaklı) → tür bazlı ayrım.
    /// </summary>
    public const string NegatifAcilis = "NEGATIF_ACILIS";
}

/// <summary>Satır uyarı kodları — HATA DEĞİLDİR, satır uygulanır.</summary>
public static class AcilisUyariKodu
{
    /// <summary>
    /// Cari açılışı kredi limitinin üstüne çıkıyor. KARAR (T-2 mührü): açılış
    /// REDDEDİLMEZ — mevcut borcun kaydı yeni risk doğurmaz. Bedeli: uygula
    /// gövdesinde SAHİP'in ayrı onayı (limitAsimiOnayi) aranır.
    /// </summary>
    public const string KrediLimitiAsimi = "KREDI_LIMITI_ASIMI";
}

/// <summary>Bir açılış satırı — tür ne olursa olsun aynı şekil.</summary>
public sealed class AcilisSatirDto
{
    /// <summary>Kasa · banka hesabı · cari kimliği.</summary>
    [Required, MaxLength(36)] public string HedefId { get; set; } = "";

    /// <summary>
    /// TL bacağı. CARI'de <b>pozitif = müşteri bize borçlu</b>
    /// (VERESIYE sözleşmesiyle aynı yön). KASA/BANKA'da mevcut bakiye.
    /// </summary>
    // PAKET GOC-C: K9b işaretli tutar — mutlak tavan decimal(15,2); 2 ondalık servis içinde (OndalikKapisi).
    [Range(typeof(decimal), "-9999999999999.99", "9999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TlTutar { get; set; }

    /// <summary>Has gram bacağı — yalnız CARI'de anlamlıdır (pozitif = altın borcu).</summary>
    // PAKET GOC-C: K11 işaretli has — mutlak tavan decimal(15,6); 6 ondalık servis içinde (OndalikKapisi).
    [Range(typeof(decimal), "-999999999.999999", "999999999.999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal HasGram { get; set; }
}

public sealed record AcilisSatirHatasi(int SatirNo, string HataKodu, string HataMesaji);

public static class AcilisKaynaklari
{
    /// <summary>Tutar tavanı — decimal(15,2) kolonunun taşmaması için.</summary>
    public const decimal TutarTavani = 9_999_999_999_999m;

    /// <summary>Türkçe açıklama — ham kod ekrana basılmaz, ama sunucu da mesajı yazar.</summary>
    public static string Mesaj(string kod) => kod switch
    {
        AcilisHataKodu.HedefYok => "Hedef kayıt seçilmedi",
        AcilisHataKodu.HedefBulunamadi => "Seçilen kayıt bu işletmede bulunamadı",
        AcilisHataKodu.SifirSatir => "Açılış satırında en az bir tutar sıfırdan farklı olmalıdır",
        AcilisHataKodu.HasBacagiYok => "Kasa ve banka açılışında has gram bacağı olamaz",
        AcilisHataKodu.NegatifAcilis =>
            "Kasa ve banka açılış bakiyesi negatif olamaz. Eksi bakiye bir borçtur: "
            + "kredili hesap ya da cari borç olarak girilmelidir.",
        AcilisHataKodu.YinelenenSatir => "Aynı kayıt bu listede birden fazla kez var",
        AcilisHataKodu.ZatenAcilis => "Bu kayda daha önce açılış bakiyesi girilmiş",
        AcilisHataKodu.DovizHesapFazB =>
            "Döviz kasası/hesabı açılışı bu sürümde yoktur; TL hesapla devam edin",
        AcilisHataKodu.TutarAsiri => "Tutar kabul edilen üst sınırı aşıyor",
        AcilisHataKodu.PasifHedef => "Pasif kayda açılış bakiyesi girilemez",
        AcilisUyariKodu.KrediLimitiAsimi =>
            "Açılış bakiyesi bu carinin kredi limitini aşıyor — açılış engellenmez, onayınız istenir",
        _ => kod,
    };

    /// <summary>
    /// SAF doğrulama — veritabanı GÖRMEZ. Satır numarası 1'den başlar (ekranda
    /// kullanıcının gördüğü sıra).
    /// </summary>
    public static List<AcilisSatirHatasi> SafDogrula(string tur, IReadOnlyList<AcilisSatirDto> satirlar)
    {
        var hatalar = new List<AcilisSatirHatasi>();
        var gorulen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < satirlar.Count; i++)
        {
            var s = satirlar[i];
            var no = i + 1;

            if (string.IsNullOrWhiteSpace(s.HedefId))
            {
                hatalar.Add(Hata(no, AcilisHataKodu.HedefYok));
                continue;
            }

            if (!gorulen.Add(s.HedefId))
                hatalar.Add(Hata(no, AcilisHataKodu.YinelenenSatir));

            if (s.TlTutar == 0m && s.HasGram == 0m)
                hatalar.Add(Hata(no, AcilisHataKodu.SifirSatir));

            if (tur != AcilisTuru.Cari && s.HasGram != 0m)
                hatalar.Add(Hata(no, AcilisHataKodu.HasBacagiYok));

            if (Math.Abs(s.TlTutar) > TutarTavani)
                hatalar.Add(Hata(no, AcilisHataKodu.TutarAsiri));
        }

        return hatalar;
    }

    /// <summary>
    /// PAKET GOC-C (envanter ortak kök 3): fazla ondalık SESSİZCE YUVARLANMAZ — TL'de en çok 2,
    /// has gramda en çok 6 ondalık; has tavanı decimal(15,6). İleti GirdiSozlugu'ndan, satır numarasıyla.
    /// Prova ve uygula AYNI kapıdan geçer.
    /// </summary>
    public static void OndalikKapisi(IReadOnlyList<AcilisSatirDto> satirlar)
    {
        for (var i = 0; i < satirlar.Count; i++)
        {
            var s = satirlar[i];
            var onek = $"{i + 1}. satır";
            Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(
                Kuyumcu.Core.Domain.Girdi.SayiKurali.Ondalik(s.TlTutar, Kuyumcu.Core.Domain.Girdi.TutarKurali.OndalikHane), "tlTutar", onek);
            Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.IsaretliHas(s.HasGram, "hasGram", onek);
        }
    }

    public static AcilisSatirHatasi Hata(int satirNo, string kod) =>
        new(satirNo, kod, Mesaj(kod));

    /// <summary>
    /// Prova jetonunun kanonik metni: tür + satırların DEĞİŞMEZ gösterimi.
    /// <para>
    /// ⚠ InvariantCulture ZORUNLU: tr-TR iş parçacığında ondalık ayracı virgül
    /// olsaydı aynı içerik farklı jeton üretir ve uygula 400 verirdi.
    /// </para>
    /// <para>
    /// ⚠ Satır SIRASI korunur (sıralanmaz): kullanıcının ekranda gördüğü liste
    /// neyse o mühürlenir. Sıralama yapılsaydı, satır sırası değişmiş bir gövde
    /// aynı jetonla geçerdi.
    /// </para>
    /// </summary>
    public static string KanonikMetin(string tur, IReadOnlyList<AcilisSatirDto> satirlar)
    {
        var sb = new StringBuilder(tur).Append('\n');
        foreach (var s in satirlar)
            sb.Append(s.HedefId).Append('|')
              .Append(s.TlTutar.ToString("F2", CultureInfo.InvariantCulture)).Append('|')
              .Append(s.HasGram.ToString("F6", CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }

    /// <summary>Kanonik metnin SHA-256'sı (küçük harf onaltılık).</summary>
    public static string Ozet(string kanonik) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kanonik))).ToLowerInvariant();
}
