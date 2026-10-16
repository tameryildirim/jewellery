using Kuyumcu.Api.Data;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK eşik motoru — PLAN Adım 4 kabul ölçütü:
// "Her eşiğin TAM SINIRINDA test. Fail-closed: kimlik eksikse işlem
//  tamamlanamaz, uyarı verip geçmez."
// Veritabanı gerekmez; motor saf fonksiyondur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MasakKuralTestleri
{
    // PAKET MASAK-370: bu dosya kademe MERDİVENİNİ (kimlik < amaç beyanı) sınar. Bu merdiven
    // 06.10.2026'ya dek yürürlükteki 185.000 kimlik eşiğiyle kurulur (dönem eşiği açıkça
    // verilir). 07.10.2026 sonrası varsayılan (370.000) ve dönem geçişi Masak370Testleri'nde.
    private static readonly MMasak.MasakKuralMotoru M = new(new MMasak.MasakEsikleri { KimlikEsigi = 185_000m });

    private static MMasak.MasakGirdi Tam(decimal tutar) => new()
    {
        IslemTutariTl = tutar,
        KimlikTespitiVar = true,
        BelgeTuru = KimlikBelgeTuru.TcKimlik,
        BelgeNo = "12345678950",
        AdSoyad = "Ayşe Demir",
        IslemAmaci = "Yatırım",
        Aciklama = "Birikim değerlendirmek için altın alımı yapılmıştır",
        FonKaynagi = "Maaş birikimi ve kira geliri",
        EkBelgeReferansi = "BELGE-2026-001",
    };

    // ── Eşik sınırları — tam kenar değerleri ─────────────────────────────────

    [Theory]
    [InlineData(0, BeyanKademesi.Yok)]
    [InlineData(184_999.99, BeyanKademesi.Yok)]
    [InlineData(185_000, BeyanKademesi.YalnizKimlik)]          // kimlik eşiği
    [InlineData(199_999.99, BeyanKademesi.YalnizKimlik)]
    [InlineData(200_000, BeyanKademesi.AmacBeyani)]            // amaç eşiği
    [InlineData(2_000_000, BeyanKademesi.AmacBeyani)]          // 2 M DAHİL amaç kademesinde
    [InlineData(2_000_000.01, BeyanKademesi.BeyanFormu)]       // form eşiği
    [InlineData(20_000_000, BeyanKademesi.BeyanFormu)]
    [InlineData(20_000_000.01, BeyanKademesi.BeyanFormuVeBelge)] // ek belge eşiği
    [InlineData(500_000_000, BeyanKademesi.BeyanFormuVeBelge)]
    public void KademeSinirlariDogru(decimal tutar, string beklenen)
    {
        Assert.Equal(beklenen, M.Gereksinim(tutar).Kademe);
    }

    [Theory]
    [InlineData(-184_999.99, BeyanKademesi.Yok)]
    [InlineData(-185_000, BeyanKademesi.YalnizKimlik)]
    [InlineData(-200_000, BeyanKademesi.AmacBeyani)]
    [InlineData(-2_000_000.01, BeyanKademesi.BeyanFormu)]
    [InlineData(-20_000_000.01, BeyanKademesi.BeyanFormuVeBelge)]
    public void NegatifTutardaSinirlarAyniYerde(decimal tutar, string beklenen)
    {
        // Aşkın: işaret yalnız ara değerde (−500.000) sınanmıştı, sınırda değil.
        Assert.Equal(beklenen, M.Gereksinim(tutar).Kademe);
    }

    [Fact]
    public void TumGecerliBelgeTurleriKabulEdilir()
    {
        // Aşkın: 5 türden yalnız TcKimlik test ediliyordu; liste bozulsa kimse görmezdi.
        Assert.Equal(5, KimlikBelgeTuru.Hepsi.Count);
        foreach (var tur in KimlikBelgeTuru.Hepsi)
        {
            var sonuc = M.Dogrula(Tam(185_000m) with { BelgeTuru = tur });
            Assert.True(sonuc.Uygun, $"{tur} kabul edilmeliydi: {string.Join(" | ", sonuc.Eksikler)}");
        }
    }

    [Fact]
    public void BelgeNoNullDaReddedilir()
    {
        var sonuc = M.Dogrula(Tam(185_000m) with { BelgeNo = null });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("belge numarası"));
    }

    [Fact]
    public void AlisDaSatisDaEsigeTabidir()
    {
        // Hareket tutarı işaretli olabilir; MASAK yönü sormaz, büyüklüğü sorar.
        Assert.Equal(M.Gereksinim(500_000m).Kademe, M.Gereksinim(-500_000m).Kademe);
    }

    // ── Eşik altı: hiçbir şey istenmez ───────────────────────────────────────

    [Fact]
    public void EsikAltindaHicbirSeyIstenmez()
    {
        var sonuc = M.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 184_999m });
        Assert.True(sonuc.Uygun);
        Assert.Empty(sonuc.Eksikler);
    }

    // ── FAIL-CLOSED: eksik kimlik işlemi DURDURUR ────────────────────────────

    [Fact]
    public void KimlikYoksaIslemKapanmaz()
    {
        var sonuc = M.Dogrula(new MMasak.MasakGirdi { IslemTutariTl = 185_000m });

        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("kimlik tespiti zorunludur"));
    }

    [Fact]
    public void KimlikVarAmaBelgeNoYoksaKapanmaz()
    {
        var sonuc = M.Dogrula(Tam(185_000m) with { BelgeNo = "  " });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("belge numarası"));
    }

    [Fact]
    public void GecersizBelgeTuruReddedilir()
    {
        var sonuc = M.Dogrula(Tam(185_000m) with { BelgeTuru = "EHLIYET_FOTOKOPISI" });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("belgesi türü"));
    }

    [Fact]
    public void AdSoyadZorunludur()
    {
        var sonuc = M.Dogrula(Tam(185_000m) with { AdSoyad = null });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("Ad soyad"));
    }

    // ── Amaç beyanı kademesi ─────────────────────────────────────────────────

    [Fact]
    public void AmacBeyaniKademesindeAmacZorunlu()
    {
        var sonuc = M.Dogrula(Tam(200_000m) with { IslemAmaci = null });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("İşlem amacı"));
    }

    [Theory]
    [InlineData("Kısa", false)]
    [InlineData("On dokuz karakter!", false)]                    // 18 karakter
    [InlineData("On dokuz karakter!!", false)]                   // 19 — TAM SINIR ALTI (Aşkın: off-by-one kör noktası)
    [InlineData("Tam yirmi karakter..", true)]                   // 20 karakter
    [InlineData("Bu açıklama fazlasıyla yeterli uzunluktadır", true)]
    public void AciklamaEnAz20KarakterOlmali(string aciklama, bool beklenenUygun)
    {
        var sonuc = M.Dogrula(Tam(200_000m) with { Aciklama = aciklama });
        Assert.Equal(beklenenUygun, sonuc.Uygun);
    }

    [Fact]
    public void AciklamaBoslukDoldurularakGecilemez()
    {
        // 30 boşluk — trim sonrası 0 karakter
        var sonuc = M.Dogrula(Tam(200_000m) with { Aciklama = new string(' ', 30) });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("en az 20 karakter"));
    }

    [Fact]
    public void KimlikEsigindeAciklamaIstenmez()
    {
        // 185.000–199.999 arası yalnız kimlik; açıklama zorunlu DEĞİL.
        var sonuc = M.Dogrula(Tam(190_000m) with { IslemAmaci = null, Aciklama = null });
        Assert.True(sonuc.Uygun);
    }

    // ── Beyan formu ve ek belge kademeleri ───────────────────────────────────

    [Fact]
    public void BeyanFormuKademesindeFonKaynagiZorunlu()
    {
        var sonuc = M.Dogrula(Tam(2_000_000.01m) with { FonKaynagi = null });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("fon kaynağı"));
    }

    [Fact]
    public void AmacKademesindeFonKaynagiIstenmez()
    {
        var sonuc = M.Dogrula(Tam(2_000_000m) with { FonKaynagi = null });
        Assert.True(sonuc.Uygun);
    }

    [Fact]
    public void EkBelgeKademesindeReferansZorunlu()
    {
        var sonuc = M.Dogrula(Tam(20_000_000.01m) with { EkBelgeReferansi = null });
        Assert.False(sonuc.Uygun);
        Assert.Contains(sonuc.Eksikler, e => e.Contains("ek belge"));
    }

    [Fact]
    public void BeyanFormuKademesindeEkBelgeIstenmez()
    {
        var sonuc = M.Dogrula(Tam(20_000_000m) with { EkBelgeReferansi = null });
        Assert.True(sonuc.Uygun);
    }

    [Fact]
    public void TamDoldurulmusIslemHerKademedeGecer()
    {
        foreach (var tutar in new[] { 185_000m, 200_000m, 2_000_000.01m, 20_000_000.01m })
            Assert.True(M.Dogrula(Tam(tutar)).Uygun, $"{tutar:N2} TL'de geçmeliydi");
    }

    // ── Eksikler TOPLUCA döner (kuyumcu tek tek denemesin) ───────────────────

    [Fact]
    public void TumEksiklerBirdenBildirilir()
    {
        var sonuc = M.Dogrula(new MMasak.MasakGirdi
        {
            IslemTutariTl = 20_000_000.01m,
            KimlikTespitiVar = true,   // ama alanları boş
        });

        Assert.False(sonuc.Uygun);
        // belge türü + belge no + ad soyad + amaç + açıklama + fon kaynağı + ek belge
        Assert.True(sonuc.Eksikler.Count >= 6, $"beklenen ≥6, gelen {sonuc.Eksikler.Count}");
    }

    // ── Şüpheli işlem: 10 İŞ GÜNÜ ────────────────────────────────────────────

    [Fact]
    public void SupheliBildirimSonTarihi10IsGunuSonra()
    {
        // 2026-09-11 Cuma (yerel). 10 iş günü → hafta sonları atlanır → 25 Eylül Cuma.
        var tespit = new DateTime(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc);
        var son = M.SonBildirimTarihi(tespit);

        var yerelSon = Kuyumcu.Api.Common.Zaman.UtcdenYerele(son);
        Assert.Equal(new DateOnly(2026, 9, 25), DateOnly.FromDateTime(yerelSon));
    }

    [Fact]
    public void HaftaSonuIsGunuSayilmaz()
    {
        // Cumadan başlayan 1 iş günü → Pazartesi olmalı, Cumartesi değil.
        var motor = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri { SupheliBildirimIsGunu = 1 });
        var cuma = new DateTime(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc);

        var son = Kuyumcu.Api.Common.Zaman.UtcdenYerele(motor.SonBildirimTarihi(cuma));

        Assert.Equal(DayOfWeek.Monday, son.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 9, 14), DateOnly.FromDateTime(son));
    }

    [Fact]
    public void ResmiTatilVerilirseAtlanir()
    {
        var motor = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri { SupheliBildirimIsGunu = 1 });
        var persembe = new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc);

        // 11 Eylül Cuma tatil ilan edilirse 1 iş günü Pazartesiye kayar.
        var son = Kuyumcu.Api.Common.Zaman.UtcdenYerele(
            motor.SonBildirimTarihi(persembe, [new DateOnly(2026, 9, 11)]));

        Assert.Equal(new DateOnly(2026, 9, 14), DateOnly.FromDateTime(son));
    }

    [Fact]
    public void SonBildirimTarihiGunSonudur()
    {
        // Süre gün sonunda dolar; sabah 09:00'da kaçmış sayılmaz (lehte yorum).
        var son = Kuyumcu.Api.Common.Zaman.UtcdenYerele(
            M.SonBildirimTarihi(new DateTime(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(23, son.Hour);
        Assert.Equal(59, son.Minute);
    }

    // ── Saklama süresi ───────────────────────────────────────────────────────

    [Fact]
    public void SaklamaSuresi8Yil()
    {
        var tarih = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2034, 9, 11, 12, 0, 0, DateTimeKind.Utc), M.SaklamaBitisi(tarih));
    }

    // ── Eşikler yapılandırmadan gelir (mevzuat değişince kod değişmesin) ─────

    [Fact]
    public void EsiklerYapilandirmadanDegistirilebilir()
    {
        var yeni = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri
        {
            KimlikEsigi = 250_000m,
            AmacBeyaniEsigi = 300_000m,
            AsgariAciklamaUzunlugu = 40,
        });

        // Eski eşikte kimlik isterdi, yeni eşikte istemiyor
        Assert.Equal(BeyanKademesi.Yok, yeni.Gereksinim(200_000m).Kademe);
        Assert.Equal(BeyanKademesi.YalnizKimlik, yeni.Gereksinim(250_000m).Kademe);

        // 20 karakterlik açıklama artık yetmiyor
        var sonuc = yeni.Dogrula(Tam(300_000m) with { Aciklama = "Tam yirmi karakter.." });
        Assert.False(sonuc.Uygun);
    }

    [Fact]
    public void VarsayilanEsiklerMevzuatlaAyni()
    {
        // MASAK-370: varsayılan = 07.10.2026'dan itibaren md.5/1-b (RG 07.10.2026/33393).
        var e = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()).Esikler;
        Assert.Equal(370_000m, e.KimlikEsigi);
        Assert.Equal(200_000m, e.AmacBeyaniEsigi);
        Assert.Equal(20, e.AsgariAciklamaUzunlugu);
        Assert.Equal(10, e.SupheliBildirimIsGunu);
        Assert.Equal(8, e.SaklamaYili);
    }
}
