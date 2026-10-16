using Kuyumcu.Core.Domain;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET TCKN-DOGRULAMA sınır testleri.
// Paket para/gram/has/kur hesabına DOKUNMAZ; bu dosya kimlik no kuralının
// BAĞIMSIZ kâhinle (oracle) sınanmasıdır. Kâhin üretim kodunu ÇAĞIRMAZ:
// kontrol haneleri negatif sayı oluşturmayan eşdeğer biçimle hesaplanır
//   10. hane = (7·tek + 9·çift) mod 10   (−1 ≡ 9 mod 10)
//   11. hane = (ilk 10 hane toplamı) mod 10
// ve her önek için 100 (d10,d11) çiftinden YALNIZ BİRİNİN kabul edildiği sınanır.
// Sabit örnekler elle hesaplanmıştır (aşağıda hesap yorumda).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TCKNDOGRULAMASinirTestleri
{
    private static (int d10, int d11) Kahin(ReadOnlySpan<int> ilk9)
    {
        var tek = ilk9[0] + ilk9[2] + ilk9[4] + ilk9[6] + ilk9[8];
        var cift = ilk9[1] + ilk9[3] + ilk9[5] + ilk9[7];
        var d10 = (7 * tek + 9 * cift) % 10;
        var toplam = d10;
        foreach (var h in ilk9) toplam += h;
        return (d10, toplam % 10);
    }

    [Fact]
    public void Kahin_ile_2000_onekte_100_ciftten_yalniz_biri_gecerli()
    {
        var rnd = new Random(20261008);
        Span<int> h = stackalloc int[9];
        for (var n = 0; n < 2000; n++)
        {
            h[0] = rnd.Next(1, 10);
            for (var i = 1; i < 9; i++) h[i] = rnd.Next(0, 10);
            var (d10, d11) = Kahin(h);
            var onek = string.Concat(h.ToArray());
            var kabul = 0;
            for (var a = 0; a < 10; a++)
                for (var b = 0; b < 10; b++)
                {
                    var no = onek + a + b;
                    var gecerli = KimlikNoDogrulayici.TcknGecerli(no);
                    Assert.Equal(a == d10 && b == d11, gecerli);
                    if (gecerli) kabul++;
                }
            Assert.Equal(1, kabul);
        }
    }

    [Fact]
    public void Ilk_hane_sifir_kahin_gecerli_olsa_bile_reddedilir()
    {
        // 0 + 12345678 → tek = 0+2+4+6+8 = 20, çift = 1+3+5+7 = 16 → d10 = (140+144)%10 = 4,
        // toplam = 0+1+...+8 + 4 = 40 → d11 = 0  ⇒ "01234567840" kural olarak tutar ama ilk hane 0.
        Assert.False(KimlikNoDogrulayici.TcknGecerli("01234567840"));
        Assert.Equal(KimlikNoHatasi.TcknGecersiz, KimlikNoDogrulayici.TcknDenetle("01234567840"));
    }

    [Theory]
    // Ayni rakam tekrari: kurucu bulgusu 11111111111 ve tum ayni-rakam numaralari (hicbiri kuraldan gecmez:
    // d: tek=5d, cift=4d → d10=(35d+36d)%10=(71d)%10=d ; toplam=10d → d11=0 ≠ d (d≠0)).
    [InlineData("11111111111")] [InlineData("22222222222")] [InlineData("33333333333")]
    [InlineData("44444444444")] [InlineData("55555555555")] [InlineData("66666666666")]
    [InlineData("77777777777")] [InlineData("88888888888")] [InlineData("99999999999")]
    public void Ayni_rakam_tekrari_reddedilir(string no)
    {
        Assert.False(KimlikNoDogrulayici.TcknGecerli(no));
        Assert.Equal(KimlikNoHatasi.TcknGecersiz, KimlikNoDogrulayici.TcknDenetle(no));
        Assert.Equal(KimlikNoHatasi.TcknGecersiz, KimlikNoDogrulayici.TcknVeyaVknDenetle(no));
        Assert.Equal(KimlikNoDogrulayici.TcknGecersizMesaj, KimlikNo.Dogrula(no, null));
        Assert.Equal(KimlikNoDogrulayici.TcknGecersizMesaj, KimlikNo.Dogrula(no, KimlikNo.Tckn));
        Assert.Equal(VergiKimlikHatasi.TcknGecersiz, VergiKimlik.Denetle(no));
    }

    [Fact]
    public void Negatif_ara_deger_gecerli_numara_kabul_edilir()
    {
        // 1 9 0 9 0 9 0 9 0 → tek = 1, çift = 36 → 7·1−36 = −29 → matematiksel mod 1 ;
        // toplam = 1+9+0+9+0+9+0+9+0+1 = 38 → 8  ⇒ 19090909018 GEÇERLİ.
        Assert.True(KimlikNoDogrulayici.TcknGecerli("19090909018"));
        Assert.Null(KimlikNo.Dogrula("19090909018", null));
        Assert.Equal(VergiKimlikHatasi.Yok, VergiKimlik.Denetle("19090909018"));
        // C# '%' kalıntısı (−29 % 10 = −9) hiçbir haneyle eşleşmemeli; komşu 10. hane reddedilir.
        Assert.False(KimlikNoDogrulayici.TcknGecerli("19090909098"));
    }

    [Theory]
    [InlineData(" 10000000146 ", KimlikNoHatasi.Yok)]          // dış boşluk kırpılır
    [InlineData("100 00000146", KimlikNoHatasi.RakamDisi)]     // iç boşluk = hata (sessiz ayıklama yok)
    [InlineData("1000000014-6", KimlikNoHatasi.RakamDisi)]
    [InlineData("+10000000146", KimlikNoHatasi.RakamDisi)]
    [InlineData("１０００００００１４６", KimlikNoHatasi.RakamDisi)] // tam genişlik rakamlar
    [InlineData("10000000146​", KimlikNoHatasi.RakamDisi)] // sıfır genişlikli boşluk
    [InlineData("1000000014", KimlikNoHatasi.HaneSayisi)]
    [InlineData("100000001460", KimlikNoHatasi.HaneSayisi)]
    public void TcknDenetle_bicim_sinirlari(string no, KimlikNoHatasi beklenen) =>
        Assert.Equal(beklenen, KimlikNoDogrulayici.TcknDenetle(no));

    [Fact]
    public void Ykn_99_ile_baslayan_kural_disi_reddedilir_kural_ici_kabul()
    {
        // 9 9 0 0 0 0 0 0 0 → tek = 9, çift = 9 → (63+81)%10 = 4 ; toplam = 18+4 = 22 → 2 ⇒ 99000000042.
        Assert.True(KimlikNoDogrulayici.YknMi("99000000042"));
        Assert.False(KimlikNoDogrulayici.YknMi("99000000043"));
        Assert.False(KimlikNoDogrulayici.YknMi("10000000146"));
    }

    [Fact]
    public void Vkn_10_hane_bicim_kabul_tur_TCKN_iken_10_hane_red()
    {
        Assert.Null(KimlikNo.Dogrula("1234567890", null));
        Assert.Null(KimlikNo.Dogrula("1234567890", KimlikNo.Vkn));
        Assert.NotNull(KimlikNo.Dogrula("1234567890", KimlikNo.Tckn));
        Assert.NotNull(KimlikNo.Dogrula("10000000146", KimlikNo.Vkn));   // tür VKN iken 11 hane
        Assert.Equal(VergiKimlikHatasi.Yok, VergiKimlik.Denetle("1234567890"));
    }
}
