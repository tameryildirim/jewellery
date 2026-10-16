using System.Globalization;
using Kuyumcu.Api.Modules.Iceaktarim;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// CSV OKUYUCU — paket 47 Faz A [saf]
//
// ⚠ KÜLTÜR TUZAĞI (Aşkın'ın koşulu): test projesinde CultureInfo HİÇ
//   sabitlenmiyor. "Ondalık nokta ile okunur" iddiası ancak iş parçacığı
//   AÇIKÇA tr-TR'ye çekilerek kanıtlanabilir — makinenin varsayılanı
//   invariant olduğu için testin sessizce doğru görünmesi kanıt değildir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CsvOkuyucuTestleri
{
    private static readonly IceAktarimKolonu[] Kolonlar =
    [
        new("tur", true, "MUSTERI"),
        new("ad", true, "Ayşe"),
        new("telefon", false, ""),
    ];

    [Fact]
    public void BOM_atlanir()
    {
        var tablo = CsvOkuyucu.Ayristir("﻿tur,ad\r\nMUSTERI,Ayşe\r\n");
        Assert.Equal("tur", tablo.Basliklar[0]);   // BOM kalsaydı "﻿tur" olurdu
        Assert.Single(tablo.Satirlar);
    }

    [Fact]
    public void Tirnakli_alanda_virgul_ve_yeni_satir_korunur()
    {
        var tablo = CsvOkuyucu.Ayristir("ad,adres\r\nAyşe,\"Şişli, İstanbul\"\r\nAli,\"A sok.\nNo 3\"\r\n");

        Assert.Equal(2, tablo.Satirlar.Count);
        Assert.Equal("Şişli, İstanbul", tablo.Satirlar[0][1]);
        Assert.Equal("A sok.\nNo 3", tablo.Satirlar[1][1]);
    }

    [Fact]
    public void Ikili_tirnak_tek_tirnaga_iner()
    {
        var tablo = CsvOkuyucu.Ayristir("ad\r\n\"Ahmet \"\"Usta\"\"\"\r\n");
        Assert.Equal("Ahmet \"Usta\"", tablo.Satirlar[0][0]);
    }

    [Fact]
    public void Bastaki_CSV_enjeksiyon_tirnagi_SOKULUR()
    {
        // CsvYazici çıkışta "=1+1" hücresini "'=1+1" yapar. Kendi dosyamızı geri
        // okurken o tırnak sökülmezse müşteri adı bozulur.
        Assert.Equal("=1+1", CsvOkuyucu.Temizle("'=1+1"));
        Assert.Equal("@SUM(A1)", CsvOkuyucu.Temizle("'@SUM(A1)"));
        // Meşru kesme işareti KORUNUR: "'Ahmet" bir formül değildir.
        Assert.Equal("'Ahmet", CsvOkuyucu.Temizle("'Ahmet"));
    }

    [Fact]
    public void Ondalik_NOKTA_ile_okunur_tr_TR_kulturunde_de()
    {
        var onceki = Thread.CurrentThread.CurrentCulture;
        try
        {
            // ⚠ AÇIKÇA tr-TR: bu kültürde ondalık ayracı VİRGÜLDÜR.
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            Assert.True(CsvOkuyucu.SayiOku("12.5", out var d));
            Assert.Equal(12.5m, d);

            // Türkçe biçimli "12,5" kabul edilirse 125 olarak okunurdu — REDDEDİLİR (GOC-47B kapanış).
            Assert.False(CsvOkuyucu.SayiOku("12,5", out _));
            Assert.False(CsvOkuyucu.SayiOku("1,500.50", out _));
            Assert.False(CsvOkuyucu.SayiOku("1500,50", out _));
            Assert.True(CsvOkuyucu.SayiOku("-13.9232", out var eksi));
            Assert.Equal(-13.9232m, eksi);
        }
        finally { Thread.CurrentThread.CurrentCulture = onceki; }
    }

    [Fact]
    public void ISO8601_UTC_tarih_okunur()
    {
        Assert.True(CsvOkuyucu.TarihOku("2026-09-14T08:30:00Z", out var t));
        Assert.Equal(new DateTime(2026, 9, 14, 8, 30, 0, DateTimeKind.Utc), t!.Value);
        Assert.Equal(DateTimeKind.Utc, t!.Value.Kind);
    }

    [Fact]
    public void Bos_alan_null_doner_bozuk_alan_FALSE()
    {
        Assert.True(CsvOkuyucu.SayiOku("", out var bos));
        Assert.Null(bos);
        Assert.False(CsvOkuyucu.SayiOku("on iki", out _));
        Assert.False(CsvOkuyucu.EvetOku("belki", out _));
    }

    [Fact]
    public void Bilinmeyen_kolon_HATA_degil_UYARI_zorunlu_kolon_eksikse_HATA()
    {
        // Rakip programın fazladan kolonu göçü DURDURMAZ.
        var (eksik1, bilinmeyen1) = CsvOkuyucu.BasliklariDenetle(["tur", "ad", "eskiProgramId"], Kolonlar);
        Assert.Empty(eksik1);
        Assert.Equal(["eskiProgramId"], bilinmeyen1);

        // Zorunlu kolon eksikse dosya REDDEDİLİR (FAIL-CLOSED 1: yarım kart yok).
        var (eksik2, _) = CsvOkuyucu.BasliklariDenetle(["ad", "telefon"], Kolonlar);
        Assert.Equal(["tur"], eksik2);
    }

    [Fact]
    public void Bos_dosya_REDDEDILIR_bassiz_CSV_tahmin_edilmez()
        => Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => CsvOkuyucu.Ayristir(""));
}
