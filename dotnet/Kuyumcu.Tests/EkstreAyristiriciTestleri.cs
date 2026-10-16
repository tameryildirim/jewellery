using System.Globalization;
using System.Text;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Banka;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 32 — EKSTRE AYRIŞTIRICI [SAF, veritabanı YOK]
//
// Bu dosyada DbContext hiç geçmez: ayrıştırıcı saf sınıftır ve bu testler onun
// kanıtıdır. Bir gün ayrıştırıcıya DbContext girerse bu dosya DERLENMEZ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EkstreAyristiriciTestleri
{
    private static readonly EkstreAyarlari Ayar = new();

    private const string Baslik = "tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan";

    [Fact]
    public void Tr_ondalikli_noktali_virgullu_dosya_dogru_tutarla_ayrisir()
    {
        var metin = Baslik + "\n01.09.2026;HAVALE;;1.234,56;10.000,00;REF1;TR330006100519786457841326;ALİ VELİ\n";

        var sonuc = EkstreAyristirici.CsvAyristir(metin, Ayar);

        var s = Assert.Single(sonuc.Satirlar);
        Assert.Null(s.Hata);
        Assert.Equal(1234.56m, s.Tutar);
        Assert.Equal("GELEN", s.Yon);
        Assert.Equal("TR330006100519786457841326", s.KarsiIban);
        Assert.Equal("ALİ VELİ", s.KarsiTarafAd);
        Assert.Equal("REF1", s.BankaReferans);
    }

    [Fact]
    public void En_ondalikli_virgullu_dosya_dogru_tutarla_ayrisir()
    {
        var metin = "tarih,aciklama,borc,alacak,bakiye,referans,karsiIban,karsiUnvan\n"
                  + "2026-09-01,TRANSFER,,\"1,234.56\",10000.00,REF2,,JOHN DOE\n";

        var sonuc = EkstreAyristirici.CsvAyristir(metin, Ayar);

        var s = Assert.Single(sonuc.Satirlar);
        Assert.Null(s.Hata);
        Assert.Equal(1234.56m, s.Tutar);
    }

    [Fact]
    public void Borc_dolu_satir_GIDEN_alacak_dolu_satir_GELEN_olur()
    {
        var metin = Baslik
            + "\n01.09.2026;ODEME;500,00;;1;R1;;A"
            + "\n02.09.2026;TAHSILAT;;750,00;2;R2;;B\n";

        var sonuc = EkstreAyristirici.CsvAyristir(metin, Ayar);

        Assert.Equal("GIDEN", sonuc.Satirlar[0].Yon);
        Assert.Equal(500m, sonuc.Satirlar[0].Tutar);
        Assert.Equal("GELEN", sonuc.Satirlar[1].Yon);
        Assert.Equal(750m, sonuc.Satirlar[1].Tutar);
    }

    [Fact]
    public void Bozuk_tarihli_satir_HATALI_isaretlenir_digerleri_AYRISMAYA_DEVAM_EDER()
    {
        // ⚠ EN ÖNEMLİ TEST: atlanan satır = sessizce eksik içe aktarım.
        var metin = Baslik
            + "\n99.99.9999;BOZUK;;100,00;1;R1;;A"
            + "\n02.09.2026;SAGLAM;;200,00;2;R2;;B\n";

        var sonuc = EkstreAyristirici.CsvAyristir(metin, Ayar);

        Assert.Equal(2, sonuc.Satirlar.Count);
        Assert.NotNull(sonuc.Satirlar[0].Hata);
        Assert.Null(sonuc.Satirlar[1].Hata);
        Assert.Equal(200m, sonuc.Satirlar[1].Tutar);
    }

    [Fact]
    public void Tutarsiz_satir_HATALI_isaretlenir_atlanmaz()
    {
        var metin = Baslik + "\n01.09.2026;TUTARSIZ;;;1;R1;;A\n";

        var s = Assert.Single(EkstreAyristirici.CsvAyristir(metin, Ayar).Satirlar);

        Assert.NotNull(s.Hata);
        Assert.Contains("tutar", s.Hata, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Iso_8859_9_kodlu_dosyada_SIRKET_dogru_okunur()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var metin = Baslik + "\n01.09.2026;ODEME;;100,00;1;R1;;ŞİRKET\n";
        var bayt = Encoding.GetEncoding(28599).GetBytes(metin);

        var cozulen = EkstreAyristirici.MetneCevir(bayt);
        var s = Assert.Single(EkstreAyristirici.CsvAyristir(cozulen, Ayar).Satirlar);

        Assert.Equal("ŞİRKET", s.KarsiTarafAd);
    }

    [Fact]
    public void Utf8_dosya_latin5_e_dusmeden_okunur()
    {
        var metin = Baslik + "\n01.09.2026;ODEME;;100,00;1;R1;;ŞİRKET\n";
        var bayt = Encoding.UTF8.GetBytes(metin);

        Assert.Contains("ŞİRKET", EkstreAyristirici.MetneCevir(bayt));
    }

    [Fact]
    public void Ayrac_otomatik_secilir()
    {
        Assert.Equal(';', EkstreAyristirici.AyracBul("a;b;c,d"));
        Assert.Equal(',', EkstreAyristirici.AyracBul("a,b,c"));
    }

    [Fact]
    public void Satir_tavani_asilirsa_400_ve_KISMI_sonuc_DONMEZ()
    {
        var kutu = new StringBuilder(Baslik + "\n");
        for (var i = 0; i < 11; i++) kutu.Append("01.09.2026;X;;1,00;1;R;;A\n");

        var ex = Assert.Throws<BadRequestException>(
            () => EkstreAyristirici.CsvAyristir(kutu.ToString(), new EkstreAyarlari { MaksSatir = 10 }));

        Assert.Contains("tavan", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cok_uzun_satir_reddedilir_CSV_bomb_savunmasi()
    {
        var metin = Baslik + "\n01.09.2026;" + new string('X', 3000) + ";;1,00;1;R;;A\n";

        Assert.Throws<BadRequestException>(
            () => EkstreAyristirici.CsvAyristir(metin, new EkstreAyarlari { MaksSatirUzunluk = 2000 }));
    }

    [Fact]
    public void Alan_sayisi_tavani_asilirsa_o_satir_HATALI_olur()
    {
        var metin = Baslik + "\n" + string.Join(";", Enumerable.Repeat("x", 60)) + "\n";

        var s = Assert.Single(
            EkstreAyristirici.CsvAyristir(metin, new EkstreAyarlari { MaksAlanSayisi = 10 }).Satirlar);

        Assert.NotNull(s.Hata);
    }

    [Fact]
    public void Tarih_sutunu_taninmazsa_dosya_400_ile_reddedilir()
    {
        var metin = "kolon1;kolon2\n1;2\n";

        Assert.Throws<BadRequestException>(() => EkstreAyristirici.CsvAyristir(metin, Ayar));
    }

    [Fact]
    public void CSV_enjeksiyon_tirnagi_giriste_sokulur_mesru_kesme_korunur()
    {
        Assert.Equal("=1+1", EkstreAyristirici.Temizle("'=1+1"));
        Assert.Equal("'Ahmet", EkstreAyristirici.Temizle("'Ahmet"));
    }

    [Fact]
    public void Iban_normallesir_bosluklar_sokulur_buyuk_harfe_cevrilir()
        => Assert.Equal("TR330006100519786457841326",
            EkstreAyristirici.IbanNormalle("tr33 0006 1005 1978 6457 8413 26"));

    [Fact]
    public void Sayi_cozumu_makine_kulturunden_BAGIMSIZDIR()
    {
        // ⚠ TestOrtami/HttpOrtami kültür AYARLAMAZ; kültür bağımlılığı testi
        //   yeşil/kırmızı yapardı. Kültür burada AÇIKÇA değiştirilip sabitlenir.
        var onceki = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.True(EkstreAyristirici.SayiCoz("1.234,56", out var a));
            Assert.Equal(1234.56m, a);

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.True(EkstreAyristirici.SayiCoz("1,234.56", out var b));
            Assert.Equal(1234.56m, b);
        }
        finally { Thread.CurrentThread.CurrentCulture = onceki; }
    }

    [Fact]
    public void Ayni_dosyanin_ozeti_AYNI_farkli_dosyanin_ozeti_FARKLI()
    {
        var a = EkstreAyristirici.DosyaOzeti(Encoding.UTF8.GetBytes("abc"));
        Assert.Equal(a, EkstreAyristirici.DosyaOzeti(Encoding.UTF8.GetBytes("abc")));
        Assert.NotEqual(a, EkstreAyristirici.DosyaOzeti(Encoding.UTF8.GetBytes("abd")));
    }

    [Fact]
    public void Dedupe_anahtari_ayni_girdide_AYNI_referans_degisince_FARKLI()
    {
        var t = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = DedupeAnahtari.Uret("H1", t, 100m, "GELEN", "R1", "TR11", "ACIKLAMA");
        var b = DedupeAnahtari.Uret("H1", t, 100m, "GELEN", "R1", "tr11", "  ACIKLAMA  ");
        var c = DedupeAnahtari.Uret("H1", t, 100m, "GELEN", "R2", "TR11", "ACIKLAMA");

        Assert.Equal(a, b);      // boşluk/büyük-küçük farkı aynı havaledir
        Assert.NotEqual(a, c);   // referans farkı ayırt eder
    }
}
