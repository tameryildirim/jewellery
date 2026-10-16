using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Api.Modules.Yedek;
using Kuyumcu.Core.I18n;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YEDEK — SAF TESTLER (paket T-3 Faz A)
//
// İki katman:
//   (a) Uygulama sözleşmesi: düzeltme politikası damgaları, RPO/RTO klempi,
//       fail-closed koşu politikası.
//   (b) BETİK SÖZLEŞMESİ: dotnet/yedek-al-ve-dogrula.sh ve yedek-ortak.sh
//       dosyalarının fail-closed kapıları YERİNDE mi. Betik bir kabuk
//       dosyasıdır; derleyici korumaz. Bu testler o boşluğu kapatır ve
//       mutasyon kanıtlarının hedefidir (kapıyı silen = kırmızı).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YedekPolitikaTestleri
{
    private static string BetikKoku
    {
        get
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null && !File.Exists(Path.Combine(d.FullName, "yedek-ortak.sh")))
                d = d.Parent;
            Assert.True(d is not null, "yedek-ortak.sh bulunamadı (betik dotnet/ kökünde durmalı).");
            return d!.FullName;
        }
    }

    private static string Oku(string ad) => File.ReadAllText(Path.Combine(BetikKoku, ad));

    // ── (a) Uygulama sözleşmesi ─────────────────────────────────────────────

    [Fact]
    public void YedekKaydi_SALT_OKUNUR_GerekceAnahtariSozluklerdeVar()
    {
        var p = DuzeltmePolitikasi.Varliklar["YedekKaydi"];
        Assert.Equal(Politikalar.SaltOkunur, p.Politika);
        Assert.Contains(p.GerekceAnahtari, GerekceAnahtarlari.Tumu);
        // TR ve EN karşılığı VAR (çıplak metin taşınmaz).
        Assert.False(string.IsNullOrWhiteSpace(Sozlukler.Tr.Duzeltme.GerekceMetni(p.GerekceAnahtari)));
        Assert.False(string.IsNullOrWhiteSpace(Sozlukler.En.Duzeltme.GerekceMetni(p.GerekceAnahtari)));
    }

    [Fact]
    public void YedekTalebi_DUZENLENEBILIR_HedefUcDolu()
    {
        var p = DuzeltmePolitikasi.Varliklar["YedekTalebi"];
        Assert.Equal(Politikalar.Duzenlenebilir, p.Politika);
        Assert.Equal("api/v1/yedekler/talep/{id}", p.HedefUc);

        var t = DuzeltmePolitikasi.Varliklar["YedekTatbikati"];
        Assert.Equal(Politikalar.SaltOkunur, t.Politika);
    }

    [Fact]
    public void RpoRto_Yapilandirmadan_Okunur_VarsayilanKlemplenir()
    {
        var bos = YedekAyarlari.Oku(new ConfigurationBuilder().Build());
        Assert.Equal(30, bos.SaklamaGun);
        Assert.Equal(360, bos.RpoDakikaTaahhut);
        Assert.Equal(120, bos.RtoDakikaTaahhut);

        var sacma = YedekAyarlari.Oku(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Yedek:SaklamaGun"] = "0",
                ["Yedek:RpoDakika"] = "1",
                ["Yedek:RtoDakika"] = "999999",
            }).Build());
        Assert.Equal(7, sacma.SaklamaGun);              // alt sınıra sıkıştı
        Assert.Equal(15, sacma.RpoDakikaTaahhut);       // alt sınıra sıkıştı
        Assert.Equal(10080, sacma.RtoDakikaTaahhut);    // üst sınıra sıkıştı
    }

    [Fact]
    public void DisKopyaHatasi_KosuBASARISIZ()
    {
        // FAIL-CLOSED 3: tek kopya yedek, yedek değildir.
        Assert.Equal(YedekDurumu.Basarisiz,
            YedekPolitikasi.KosuSonucu(sifreli: true, DisKopyaDurumu.Hata));
        Assert.Equal(YedekDurumu.Basarisiz,
            YedekPolitikasi.KosuSonucu(sifreli: true, DisKopyaDurumu.Yok));
        // FAIL-CLOSED 2: şifresiz yedek de BAŞARISIZ.
        Assert.Equal(YedekDurumu.Basarisiz,
            YedekPolitikasi.KosuSonucu(sifreli: false, DisKopyaDurumu.Kopyalandi));
        Assert.Equal(YedekDurumu.Basarili,
            YedekPolitikasi.KosuSonucu(sifreli: true, DisKopyaDurumu.Kopyalandi));
    }

    [Fact]
    public void Saklama_SonDogrulanmisYedekYoksa_HicbirKayitSilinmez()
    {
        // FAIL-CLOSED 4.
        Assert.False(YedekPolitikasi.SaklamaSilinebilir(0));
        Assert.True(YedekPolitikasi.SaklamaSilinebilir(1));
    }

    // ── (b) Betik sözleşmesi ────────────────────────────────────────────────

    [Fact]
    public void Betik_GomuluParola_Ve_ArgvParolasi_Yok()
    {
        foreach (var ad in new[] { "yedek-al-ve-dogrula.sh", "yedek-ortak.sh" })
        {
            var metin = Oku(ad);
            Assert.DoesNotContain("--password=", metin);
            Assert.DoesNotContain("-p\"$", metin);
            // Kimlik YALNIZ defaults dosyasıyla verilir.
            Assert.Contains("--defaults-extra-file", metin);
        }
        // Eski gömülü geliştirme parolası GERİ GELMESİN.
        Assert.DoesNotContain("KUYUMCU_SIFRE:-", Oku("yedek-al-ve-dogrula.sh"));
    }

    [Fact]
    public void Betik_FailClosed_Kapilari_Yerinde()
    {
        var ortak = Oku("yedek-ortak.sh");
        var giris = Oku("yedek-al-ve-dogrula.sh");

        // FAIL-CLOSED 2 — anahtar yoksa/kısaysa/alan anahtarıyla aynıysa yedek YOK.
        Assert.Contains("anahtar_dogrula", giris);
        Assert.Contains("KUYUMCU_SIFRELEME_ANAHTARI", ortak);
        // FAIL-CLOSED 3 — dış kopya başarısızsa koşu BASARISIZ.
        Assert.Contains("dis_kopya_al", giris);
        Assert.Contains("!= \"KOPYALANDI\"", giris);
        Assert.Contains("BASARISIZ", giris);
        // FAIL-CLOSED 4 — doğrulanmış yedek yoksa silme yok.
        Assert.Contains("dogrulama='DOGRULANDI'", ortak);
        Assert.Contains("HİÇBİR dosyayı silmedi", ortak);
        // Şifreleme boru hattı: dump -> gzip -> gpg (şifresiz ara dosya yok).
        Assert.Contains("dump_akisi | gzip -9 | sifrele", giris);
        // Tatbikat kaynak şemaya yazmaz, şemayı düşürür.
        Assert.Contains("DROP DATABASE IF EXISTS ${YEDEK_TATBIKAT_SEMA}", giris);
        Assert.Contains("sema_dusuruldu", giris);
        // Rapor YALNIZ tablo adı taşır.
        Assert.Contains("uyusmayan_tablolar=", giris);
    }

    [Fact]
    public void Betik_TekDumpYolu_IkinciYedekBetigiYok()
    {
        // MÜHÜR 6 / K3: mysqldump çağrısı YALNIZ yedek-ortak.sh'ta.
        // Kapsam: yedek betikleri. denetim.sh de "mysqldump" kelimesini SAYAÇ
        // olarak içerir; o bir yedek yolu değil, kapının kendisidir.
        var dosyalar = Directory.GetFiles(BetikKoku, "yedek*.sh");
        var dumpTasiyan = dosyalar
            .Where(d => File.ReadLines(d).Any(s =>
                s.Contains("mysqldump", StringComparison.Ordinal) && !s.TrimStart().StartsWith('#')))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal(["yedek-ortak.sh"], dumpTasiyan);
    }
}
