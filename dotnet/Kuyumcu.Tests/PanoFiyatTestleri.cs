using Kuyumcu.Api.Common;
using MKur = Kuyumcu.Api.Modules.Kur;
using MPano = Kuyumcu.Api.Modules.Pano;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Vitrin panosu fiyat hesabı — PANO-KARAR-2026-09-11.md §1, §2, §7.
// Veritabanı gerekmez: PanoFiyatServisi saf fonksiyondur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PanoFiyatTestleri
{
    private static MKur.Parite P(string kod, decimal? alis, decimal? satis, string birim = "ADET") =>
        new(kod, MKur.PariteKodlari.Ad(kod), alis, satis, birim);

    private static MPano.PanoKalemAyari A(
        string kod, decimal alisMarj = 0m, decimal satisMarj = 0m,
        decimal adim = 0m, int sira = 0, bool aktif = true, string? etiket = null) =>
        new()
        {
            PariteKodu = kod, AlisMarjTl = alisMarj, SatisMarjTl = satisMarj,
            YuvarlamaAdimi = adim, Sira = sira, Aktif = aktif, Etiket = etiket,
        };

    // ── Marj: sabit TL fark (karar §2) ───────────────────────────────────────

    [Fact]
    public void MarjSabitTlOlarakEklenir()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.CeyrekYeni, 10_660m, 11_070m)],
            [A(MKur.PariteKodlari.CeyrekYeni, alisMarj: -150m, satisMarj: 250m)]).Satirlar.Single();

        Assert.Equal(10_510m, satir.Alis);   // 10.660 − 150
        Assert.Equal(11_320m, satir.Satis);  // 11.070 + 250
    }

    [Fact]
    public void MarjYuzdeDegilSabittir_KurYukselinceKarBuyumez()
    {
        // Karar §2'nin can alıcı noktası: altın %50 yükselse bile kuyumcunun
        // kârı aynı TL kalır. Yüzde olsaydı kâr kendiliğinden büyürdü.
        var ayar = A(MKur.PariteKodlari.CeyrekYeni, satisMarj: 250m);

        var ucuz = MPano.PanoFiyatServisi.Hesapla([P(MKur.PariteKodlari.CeyrekYeni, null, 10_000m)], [ayar]).Satirlar.Single();
        var pahali = MPano.PanoFiyatServisi.Hesapla([P(MKur.PariteKodlari.CeyrekYeni, null, 15_000m)], [ayar]).Satirlar.Single();

        Assert.Equal(250m, ucuz.Satis - 10_000m);
        Assert.Equal(250m, pahali.Satis - 15_000m);
    }

    [Fact]
    public void AlisMarjiNegatifOlabilir()
    {
        // Kuyumcu toptancıdan aşağı alır; eksi marj kasıtlıdır, hata değildir.
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Bilezik22, 6_500m, 6_800m, "GRAM")],
            [A(MKur.PariteKodlari.Bilezik22, alisMarj: -300m)]).Satirlar.Single();

        Assert.Equal(6_200m, satir.Alis);
        Assert.Equal("GRAM", satir.Birim);
    }

    // ── Yuvarlama: satış yukarı, alış aşağı (karar §2) ───────────────────────

    [Fact]
    public void SatisYukariAlisAsagiYuvarlanir()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.CeyrekYeni, 10_663m, 11_071m)],
            [A(MKur.PariteKodlari.CeyrekYeni, adim: 10m)]).Satirlar.Single();

        Assert.Equal(10_660m, satir.Alis);   // 10.663 → aşağı
        Assert.Equal(11_080m, satir.Satis);  // 11.071 → yukarı
    }

    [Theory]
    [InlineData(1, 10_663, 11_071)]
    [InlineData(5, 10_660, 11_075)]
    [InlineData(10, 10_660, 11_080)]
    [InlineData(50, 10_650, 11_100)]
    [InlineData(100, 10_600, 11_100)]
    public void YuvarlamaAdimiSecilebilir(int adim, decimal beklenenAlis, decimal beklenenSatis)
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.CeyrekYeni, 10_663m, 11_071m)],
            [A(MKur.PariteKodlari.CeyrekYeni, adim: adim)]).Satirlar.Single();

        Assert.Equal(beklenenAlis, satir.Alis);
        Assert.Equal(beklenenSatis, satir.Satis);
    }

    [Fact]
    public void YuvarlamaKapaliykenKurusKorunur()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gumus, 42.37m, 44.61m, "GRAM")],
            [A(MKur.PariteKodlari.Gumus, adim: 0m)]).Satirlar.Single();

        Assert.Equal(42.37m, satir.Alis);
        Assert.Equal(44.61m, satir.Satis);
    }

    [Fact]
    public void MarjOnceUygulanirSonraYuvarlanir()
    {
        // 10.663 − 8 = 10.655 → 10 adımıyla aşağı → 10.650
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.CeyrekYeni, 10_663m, 11_071m)],
            [A(MKur.PariteKodlari.CeyrekYeni, alisMarj: -8m, adim: 10m)]).Satirlar.Single();

        Assert.Equal(10_650m, satir.Alis);
    }

    // ── Eksik/bozuk veri ─────────────────────────────────────────────────────

    [Fact]
    public void KotasyonuGelmeyenKalemPanodaGorunmez()
    {
        var satirlar = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 6_900m, 6_950m, "GRAM")],
            [A(MKur.PariteKodlari.Gram), A(MKur.PariteKodlari.Resat)]).Satirlar;

        Assert.Single(satirlar);
        Assert.Equal(MKur.PariteKodlari.Gram, satirlar[0].Kod);
    }

    [Fact]
    public void IkiTarafiDaBosOlanKalemAtlanir()
    {
        var satirlar = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Resat, null, null)],
            [A(MKur.PariteKodlari.Resat)]).Satirlar;

        Assert.Empty(satirlar);
    }

    [Fact]
    public void TekTarafiOlanKalemGosterilir()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Ons, null, 3_640m)],
            [A(MKur.PariteKodlari.Ons)]).Satirlar.Single();

        Assert.Null(satir.Alis);
        Assert.Equal(3_640m, satir.Satis);
    }

    /// <summary>
    /// ⚠ SÖZLEŞME DEĞİŞTİ — PAKET P-38 · B-18 (QA turu 7 bulgu 112).
    /// <para>
    /// Bu test eskiden "fiyat 0'a çekilir" diyordu ve BULGUNUN KENDİSİNİ
    /// mühürlüyordu: müşteriye dönük panoda "gram altın alış ₺0,00" yazması
    /// tam olarak bu davranıştı. Artık ₺0,00 bir FİYAT SAYILMAZ; satır
    /// MARJ_GECERSIZ sebebiyle vitrine hiç çıkmaz ve sebep yönetim ekranında
    /// görünür (Anayasa md.1 — sessizce yutulmaz).
    /// </para>
    /// </summary>
    [Fact]
    public void AsiriNegatifMarjFiyatiSifirinAltinaDusuremez()
    {
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gumus, 42m, 44m, "GRAM")],
            [A(MKur.PariteKodlari.Gumus, alisMarj: -500m)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Equal(MPano.AtlamaSebebi.MarjGecersiz, Assert.Single(sonuc.Atlananlar).Sebep);
    }

    // ── Sıra, kapalı kalem, tavan ────────────────────────────────────────────

    [Fact]
    public void SatirlarSiraAlaninaGoreDizilir()
    {
        var satirlar = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 1m, 2m), P(MKur.PariteKodlari.CeyrekYeni, 1m, 2m)],
            [A(MKur.PariteKodlari.CeyrekYeni, sira: 2), A(MKur.PariteKodlari.Gram, sira: 1)]).Satirlar;

        Assert.Equal(
            new[] { MKur.PariteKodlari.Gram, MKur.PariteKodlari.CeyrekYeni },
            satirlar.Select(s => s.Kod).ToArray());
    }

    [Fact]
    public void KapaliKalemPanodaGorunmez()
    {
        var satirlar = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 1m, 2m)],
            [A(MKur.PariteKodlari.Gram, aktif: false)]).Satirlar;

        Assert.Empty(satirlar);
    }

    [Fact]
    public void TavanSatirdanFazlasiGosterilmez()
    {
        var kodlar = MKur.PariteKodlari.Adlar.Keys.Take(MPano.PanoFiyatServisi.TavanSatir + 3).ToList();
        var satirlar = MPano.PanoFiyatServisi.Hesapla(
            kodlar.Select(k => P(k, 1m, 2m)).ToList(),
            kodlar.Select((k, i) => A(k, sira: i)).ToList()).Satirlar;

        Assert.Equal(MPano.PanoFiyatServisi.TavanSatir, satirlar.Count);
    }

    [Fact]
    public void KuyumcununKendiEtiketiParitAdiniEzer()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.AtaYeni, 1m, 2m)],
            [A(MKur.PariteKodlari.AtaYeni, etiket: "  Cumhuriyet  ")]).Satirlar.Single();

        Assert.Equal("Cumhuriyet", satir.Etiket);
    }

    [Fact]
    public void EtiketBossaParitAdiKullanilir()
    {
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.AtaYeni, 1m, 2m)],
            [A(MKur.PariteKodlari.AtaYeni, etiket: "   ")]).Satirlar.Single();

        Assert.Equal("Yeni Ata Lira", satir.Etiket);
    }

    // ── Varsayılan küme — piyasa ölçümü (karar §7 bulgu #3) ──────────────────

    [Fact]
    public void VarsayilanKume11Kalemdir()
    {
        // 21.10 kurucu: "eski yeni tam liste gelmesi lazım" — dükkân setine sarrafiyenin ESKİ karşılıkları eklendi;
        // okunabilirlik eşiğinin (12) altında kalır.
        Assert.Equal(11, MPano.PanoFiyatServisi.VarsayilanKalemler.Count);
        Assert.True(MPano.PanoFiyatServisi.VarsayilanKalemler.Count < MPano.PanoFiyatServisi.OkunabilirlikUyariEsigi);
    }

    [Fact]
    public void VarsayilanKumeDukkanSetiYeniVeEskiSarrafiyeyiTekKaynakSirasiylaTasir()
    {
        // KuyumAltın'ın "dükkânda gösterilen temel ürün seti" (§7 bulgu #3) + eski çeyrek/yarım/tam/ata (21.10);
        // sıra Core FiyatKalemleri sırası (yeni hemen ardından eski).
        Assert.Equal(
            new[]
            {
                MKur.PariteKodlari.Gram, MKur.PariteKodlari.Bilezik22,
                MKur.PariteKodlari.CeyrekYeni, MKur.PariteKodlari.CeyrekEski,
                MKur.PariteKodlari.YarimYeni, MKur.PariteKodlari.YarimEski,
                MKur.PariteKodlari.TamYeni, MKur.PariteKodlari.TamEski,
                MKur.PariteKodlari.AtaYeni, MKur.PariteKodlari.AtaEski,
                MKur.PariteKodlari.Gumus,
            },
            MPano.PanoFiyatServisi.VarsayilanKalemler.ToArray());
        var siralar = MPano.PanoFiyatServisi.VarsayilanKalemler.Select(Kuyumcu.Core.Domain.FiyatKalemleri.Sira).ToArray();
        Assert.Equal(siralar.OrderBy(x => x), siralar);
    }

    [Fact]
    public void VarsayilanKumedeDovizYoktur()
    {
        // Karar §1 düzeltmesi: USD/EUR varsayılan KAPALI.
        Assert.DoesNotContain(MPano.PanoFiyatServisi.VarsayilanKalemler,
            k => k.Contains("USD") || k.Contains("EUR"));
    }

    [Fact]
    public void VarsayilanKalemlerinHepsiGecerliParitedir()
    {
        foreach (var k in MPano.PanoFiyatServisi.VarsayilanKalemler)
            Assert.True(MKur.PariteKodlari.Gecerli(k), $"geçersiz parite: {k}");
    }

    [Fact]
    public void OkunabilirlikUyariEsigiTavaninAltindadir()
    {
        Assert.True(MPano.PanoFiyatServisi.OkunabilirlikUyariEsigi < MPano.PanoFiyatServisi.TavanSatir);
        Assert.Equal(16, MPano.PanoFiyatServisi.TavanSatir);
        Assert.Equal(12, MPano.PanoFiyatServisi.OkunabilirlikUyariEsigi);
    }

    // ── Doğrulama ────────────────────────────────────────────────────────────

    [Fact]
    public void GecersizPariteKoduReddedilir()
    {
        Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(A("OLMAYAN_KOD")));
    }

    [Fact]
    public void GecersizYuvarlamaAdimiReddedilir()
    {
        Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(A(MKur.PariteKodlari.Gram, adim: 7m)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void GecerliYuvarlamaAdimlariKabulEdilir(int adim)
    {
        MPano.PanoFiyatServisi.Dogrula(A(MKur.PariteKodlari.Gram, adim: adim));
    }

    [Fact]
    public void SacmaBuyuklukteMarjReddedilir()
    {
        Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(A(MKur.PariteKodlari.Gram, satisMarj: 9_999_999m)));
    }

    // ── Gizlilik: marj panoya inmez (Mehmet'in gereksinimi) ──────────────────

    [Fact]
    public void PanoSatirindaMarjVeHamKotasyonAlaniYoktur()
    {
        var alanlar = typeof(MPano.PanoSatir).GetProperties().Select(p => p.Name).ToList();

        Assert.Equal(new[] { "Kod", "Etiket", "Alis", "Satis", "Birim" }, alanlar.ToArray());
        Assert.DoesNotContain(alanlar, a => a.Contains("Marj", StringComparison.OrdinalIgnoreCase));
    }

    // ── Ters makas koruması (gerçek vakadan doğdu) ───────────────────────────

    [Fact]
    public void TersMakasliKalemVitrineCikmaz()
    {
        // GERÇEK VERİ, 11.09.2026 · Altınkaynak "Hamit Altın" (kod PH):
        // alış 44.052,56 · satış 43.250,00 — kaynağın kendi verisi bozuk.
        // Üç ayrı ölçümde de aynı geldi, geçici bir dalgalanma değil.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Hamit, 44_052.56m, 43_250.00m)],
            [A(MKur.PariteKodlari.Hamit)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Equal(MPano.AtlamaSebebi.TersMakas, sonuc.Atlananlar.Single().Sebep);
    }

    [Fact]
    public void TersMakasDigerSatirlariDusurmez()
    {
        // Bozuk bir kalem yüzünden pano komple boşalmamalı.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [
                P(MKur.PariteKodlari.Hamit, 44_052.56m, 43_250.00m),
                P(MKur.PariteKodlari.Gram, 6_673.12m, 6_782.57m, "GRAM"),
            ],
            [A(MKur.PariteKodlari.Hamit, sira: 1), A(MKur.PariteKodlari.Gram, sira: 2)]);

        Assert.Equal(MKur.PariteKodlari.Gram, sonuc.Satirlar.Single().Kod);
        Assert.Single(sonuc.Atlananlar);
    }

    [Fact]
    public void MarjinKendisiMakasiTerseCevirirseSatirDuser()
    {
        // Kaynak sağlam ama kuyumcu satış marjını eksi, alış marjını artı yazmış.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.CeyrekYeni, 10_736m, 11_395m)],
            [A(MKur.PariteKodlari.CeyrekYeni, alisMarj: 900m, satisMarj: -900m)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Equal(MPano.AtlamaSebebi.TersMakas, sonuc.Atlananlar.Single().Sebep);
    }

    [Fact]
    public void AlisSatisaEsitseDeDuser()
    {
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Ons, 4_335.52m, 4_335.52m)],
            [A(MKur.PariteKodlari.Ons)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Equal(MPano.AtlamaSebebi.TersMakas, sonuc.Atlananlar.Single().Sebep);
    }

    [Fact]
    public void TekTarafliKalemTersMakasSayilmaz()
    {
        // Karşılaştıracak ikinci fiyat yoksa kural işlemez.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Ons, null, 3_640m)],
            [A(MKur.PariteKodlari.Ons)]);

        Assert.Single(sonuc.Satirlar);
        Assert.Empty(sonuc.Atlananlar);
    }

    // ── Atlama sebepleri yönetim ekranına bildirilir ─────────────────────────

    [Fact]
    public void KotasyonuGelmeyenKalemSebebiyleBildirilir()
    {
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 6_900m, 6_950m, "GRAM")],
            [A(MKur.PariteKodlari.Gram), A(MKur.PariteKodlari.Resat)]);

        var atlanan = sonuc.Atlananlar.Single();
        Assert.Equal(MKur.PariteKodlari.Resat, atlanan.Kod);
        Assert.Equal(MPano.AtlamaSebebi.KotasyonYok, atlanan.Sebep);
    }

    [Fact]
    public void TavaniAsanKalemlerSebebiyleBildirilir()
    {
        var kodlar = MKur.PariteKodlari.Adlar.Keys.Take(MPano.PanoFiyatServisi.TavanSatir + 3).ToList();
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            kodlar.Select(k => P(k, 1m, 2m)).ToList(),
            kodlar.Select((k, i) => A(k, sira: i)).ToList());

        Assert.Equal(MPano.PanoFiyatServisi.TavanSatir, sonuc.Satirlar.Count);
        Assert.Equal(3, sonuc.Atlananlar.Count);
        Assert.All(sonuc.Atlananlar, x => Assert.Equal(MPano.AtlamaSebebi.TavanAsildi, x.Sebep));
    }

    [Fact]
    public void KapaliKalemAtlananListesineGirmez()
    {
        // Kuyumcu kasten kapatmış — bu bir sorun değil, bildirilmez.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 1m, 2m)],
            [A(MKur.PariteKodlari.Gram, aktif: false)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Empty(sonuc.Atlananlar);
    }
}
