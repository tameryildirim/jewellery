using System.Text.RegularExpressions;
using Kuyumcu.Api.Modules.Meta;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-D — dalga 3 ve 4 ile doğan ekranların düzeltme yolları [saf]
//
// Bu dosyanın tek işi ŞUDUR: "her veri girilen yerin ya bir düzeltme yolu
// vardır ya da neden olmadığı YAZILIDIR" sözünün dalga 3+4 ekranlarında da
// tutulduğunu KANITLAMAK — ve tutulmadığında KIRMIZI vermek.
//
// İki kapı:
//   (1) POLİTİKA KAPSAMI — dalga 3/4'te doğan 12 veri türünün tamamı
//       DuzeltmePolitikasi haritasında ve BEKLENEN damgada.
//       ⚠ Damga listesi burada SABİT yazılıdır: haritanın kendi değerini
//         okuyup ona eşit mi diye sormak kendi kendini doğrular ve hiçbir
//         şeyi yakalamaz (Aşkın K5). Biri "MASAK kaydını da düzenlenebilir
//         yapalım" derse bu test kırmızı verir (VUK/MASAK mevzuat kapısı).
//   (2) YANILTICI DÜĞME YASAĞI — UX-1-D'nin dokunduğu 11 rotanın razor
//       kaynağında KALICI devre dışı düğme (disabled="@true" / disabled="true")
//       kalmaz. "Bir yolu var ama sana kapalı" demek yerine DegismezIpucu
//       gerekçeyi ve doğru yolu yazar (UX-S25, QA turu 3 KRİTİK 4 dersi).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX1DPolitikaTestleri
{
    /// <summary>
    /// Dalga 3 + dalga 4'te doğan veri türleri → BEKLENEN damga.
    /// Sol sütun haritadaki GERÇEK varlık adıdır (Data/Entities.cs).
    /// </summary>
    public static readonly (string Varlik, string Damga)[] DalgaVarliklari =
    [
        // ── Dalga 3 ──
        ("VezneFisi",          Politikalar.Duzenlenebilir),   // 19  · ACIK iken PATCH
        ("StokYerlesimi",      Politikalar.Storno),           // 25  · taşıma/geri-al ters hareketi
        ("IceAktarimOturumu",  Politikalar.Duzenlenebilir),   // 47-A· prova aşamasında PATCH
        ("IceAktarimSatiri",   Politikalar.SaltOkunur),       // 47-A· kanıt kaydı
        ("KurulumDurumu",      Politikalar.Duzenlenebilir),   // T-2 · mühre kadar PATCH
        ("AcilisKalemi",       Politikalar.Storno),           // T-2 · ters ACILIS kaydı
        // ── Dalga 4 ──
        ("KiymetliEvrak",      Politikalar.Duzenlenebilir),   // 29-A· PORTFOYDE iken PATCH
        ("MasakKimlikTespiti", Politikalar.SaltOkunur),       // 41  · MASAK yükümlülük kaydı
        ("EkstreIthalati",     Politikalar.SaltOkunur),       // 32  · parti başlığı
        ("EkstreSatiri",       Politikalar.Duzenlenebilir),   // 32  · onaydan ÖNCE
        ("CariRiskProfili",    Politikalar.SaltOkunur),       // 40-A· append-only tarihçe
        ("PosCihaz",           Politikalar.Duzenlenebilir),   // 31  · PATCH + pasife alma
        ("PosTahsilat",        Politikalar.SaltOkunur),       // 31  · satıştan türetilir
    ];

    [Fact]
    public void Dalga3_ve_4_varliklarinin_TAMAMI_haritada_ve_BEKLENEN_damgada()
    {
        foreach (var (varlik, damga) in DalgaVarliklari)
        {
            Assert.True(
                DuzeltmePolitikasi.Varliklar.TryGetValue(varlik, out var p),
                $"'{varlik}' düzeltme politikası haritasında YOK — dalga 3/4 ekranı "
                + "kullanıcıya düzeltme yolunu söyleyemez (UX-S25).");
            Assert.Equal(damga, p!.Politika);
        }
    }

    [Fact]
    public void Dalga_varliklarinin_gerekce_anahtari_TANIMLI_kumeden_gelir()
    {
        foreach (var (varlik, _) in DalgaVarliklari)
        {
            var p = DuzeltmePolitikasi.Varliklar[varlik];
            Assert.Contains(p.GerekceAnahtari, GerekceAnahtarlari.Tumu);
        }
    }

    [Fact]
    public void Defter_ve_MASAK_turleri_ASLA_DUZENLENEBILIR_damgalanamaz()
    {
        // Mevzuat kapısı (VUK md.219 / MASAK yükümlülük kaydı): "her yer
        // düzenlenebilir olsun" hedefi bu satırlara UYGULANAMAZ.
        string[] defterTurleri =
        [
            "MasakKimlikTespiti", "MasakBeyani", "MasakSupheliBildirim",
            "CariRiskProfili", "DenetimKaydi", "Hareket", "KasaHareket",
            "BankaHareket", "AcilisKalemi", "EkstreIthalati", "PosTahsilat",
        ];
        foreach (var ad in defterTurleri)
        {
            var p = DuzeltmePolitikasi.Varliklar[ad];
            Assert.NotEqual(Politikalar.Duzenlenebilir, p.Politika);
        }
    }

    // ── Kapı 2: yanıltıcı düğme yasağı ───────────────────────────────────────

    /// <summary>UX-1-D'nin kapsamındaki 11 rotanın razor dosyaları.</summary>
    public static readonly string[] DokunulanSayfalar =
    [
        "Vezne.razor",          // /vezne                       (19)
        "UrunAgaci.razor",      // /urunler/agac                (25)
        "Cariler.razor",        // /cariler                     (27 + 40-A)
        "VeriIceAktarim.razor", // /veri-ice-aktarim            (47-A)
        "Kurulum.razor",        // /kurulum                     (T-2)
        "Evraklar.razor",       // /evraklar                    (29-A)
        "EvrakTakvimi.razor",   // /evraklar/takvim             (29-A)
        "Banka.razor",          // /banka                       (41)
        "BankaEkstre.razor",    // /banka/ekstre/{ithalatId}    (32)
        "Uyum.razor",           // /uyum                        (40-A)
        "Pos.razor",            // /pos                         (31)
        "PosTahsilatlar.razor", // /pos/tahsilatlar             (31)
    ];

    [Fact]
    public void Dokunulan_sayfalarda_KALICI_devre_disi_dugme_KALMAZ()
    {
        // disabled="@true" / disabled="true" = koşula bağlı olmayan, HİÇBİR
        // zaman açılmayan düğme. Kullanıcıya "bir yolu var ama sana kapalı"
        // der; doğru cevap DegismezIpucu'dur.
        var kalici = new Regex("""disabled\s*=\s*"@?true"\s*""", RegexOptions.IgnoreCase);
        var suclular = new List<string>();

        foreach (var dosya in DokunulanSayfalar)
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", dosya);
            foreach (Match m in kalici.Matches(kaynak))
                suclular.Add($"{dosya}: {m.Value.Trim()}");
        }

        Assert.True(suclular.Count == 0,
            "Kalıcı devre dışı düğme(ler) bulundu — UX-S25 gereği yerine "
            + "DegismezIpucu çizilmeli:\n  " + string.Join("\n  ", suclular));
    }

    [Fact]
    public void Dokunulan_sayfalarin_TAMAMI_DegismezIpucu_ya_da_duzenleme_yolu_tasir()
    {
        // "Hiçbir şey yok" hâli de bulgudur: her ekran ya bir düzenleme kalıbı
        // (Cekmece / SatirIciDuzenle) ya da bir gerekçe kutusu göstermelidir.
        foreach (var dosya in DokunulanSayfalar)
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", dosya);
            var var1 = kaynak.Contains("<DegismezIpucu", StringComparison.Ordinal);
            var var2 = kaynak.Contains("<Cekmece", StringComparison.Ordinal);
            var var3 = kaynak.Contains("<SatirIciDuzenle", StringComparison.Ordinal);
            Assert.True(var1 || var2 || var3,
                $"{dosya}: ne düzenleme kalıbı ne de gerekçe kutusu var (UX-S25).");
        }
    }
}
