using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MPos = Kuyumcu.Api.Modules.Pos;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A 28.09 (4. doğrulama) — sunucu tarafı:
//   • MASAK uyruk kodu denetimi yalnız istemcideydi → MasakKapisi.Dogrula aynı tek
//     kaynağı (UlkeListesi.KodGecerli) uygular; Kosova pasaport kodu RKS kabul edilir.
//   • POS öneri yanıtı hareketin henüz eşleşmemiş tutarını (hareketKalan) taşır —
//     ekranın elle eşleştirme sınırı buna dayanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ZOISLEMA4MasakSafTestleri
{
    private static MMasak.MasakBilgiDto Kimlik(string? uyruk) => new()
    {
        BelgeTuru = KimlikBelgeTuru.Pasaport, BelgeNo = "U1234567", AdSoyad = "John Smith",
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = uyruk,
        IslemAmaci = "Yatırım", Aciklama = "Birikim değerlendirmek için altın alımı yapılıyor",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    [Theory]
    [InlineData("ZZQ")]
    [InlineData("TR")]
    [InlineData("xkx")]
    public void ZO28e_esik_ustunde_ISO_DISI_uyruk_400(string uyruk)
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MMasak.MasakKapisi.Varsayilan().Dogrula(2_000_000m, Kimlik(uyruk)));
        Assert.Contains("uyruk kodu geçersiz", ex.Message);
        Assert.Contains(uyruk.Trim().ToUpperInvariant(), ex.Message);
    }

    [Theory]
    [InlineData("FRA")]
    [InlineData("rks")]
    [InlineData("TUR")]
    [InlineData(null)]
    public void ZO28e_gecerli_ya_da_bos_uyruk_kapidan_gecer(string? uyruk)
    {
        var g = MMasak.MasakKapisi.Varsayilan().Dogrula(2_000_000m, Kimlik(uyruk));
        Assert.NotEqual(BeyanKademesi.Yok, g.Kademe);
    }

    [Fact]
    public void ZO28e_esik_altinda_uyruk_denetlenmez()
    {
        var g = MMasak.MasakKapisi.Varsayilan().Dogrula(1_000m, Kimlik("ZZQ"));
        Assert.Equal(BeyanKademesi.Yok, g.Kademe);
    }
}

[Collection("veritabani")]
public sealed class ZOISLEMA4PosTestleri(TestVeritabani vt)
{
    private static decimal Alan(object o, string ad) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o))
            .RootElement.GetProperty(ad).GetDecimal();

    [Fact]
    public async Task ZO28e_oneri_yaniti_hareketin_ESLESMEMIS_tutarini_tasir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO4 hareket kalan");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m);

        using (var db = vt.Olustur(k))
        {
            var once = await PosYardim.Servis(db)
                .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem);
            Assert.Equal(5_000m, Alan(once, "hareketKalan"));
            await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 1_250.25m }],
            }, k.Islem);
        }
        using (var db = vt.Olustur(k))
        {
            var sonra = await PosYardim.Servis(db)
                .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem);
            Assert.Equal(3_749.75m, Alan(sonra, "hareketKalan"));
        }
    }

    [Fact]
    public async Task ZO28i_ayni_tahsilat_IKI_SATIRLA_gelirse_400_ve_hic_yazmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO4 cift satir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 20_000m);

        using (var db = vt.Olustur(k))
        {
            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
                {
                    BankaHareketId = hareket,
                    Satirlar =
                    [
                        new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 100m },
                        new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = 200m },
                    ],
                }, k.Islem));
            Assert.Contains("yalnız bir kez", ex.Message);
        }
        using (var db = vt.Olustur(k))
            Assert.Equal(0, db.PosYatmaEslesmeleri.Count(e => e.PosTahsilatId == t1));
    }

    // ── ZO-ISLEM-A 28.09 (5. doğrulama, KRİTİK — elle eşleştirme çıkmazı) ──────────
    // Eskiden (tahsilat, hareket) çifti üzerindeki tekil indeks geri alınmış eşleşmeyi de
    // sayıyordu: geri alınan çift yeniden, açık çift ise ikinci kısmi tutarla eşleşemiyordu
    // (409 "Bu kayıt zaten var"). Şimdi her eşleşme ayrı bir olay satırıdır (cift_sira),
    // geri alınan satır İZ olarak kalır.
    [Fact]
    public async Task ZO28j_geri_alinan_cift_yeniden_ve_ikinci_kismi_tutarla_eslesir_iz_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 cift sira");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);   // net 9.815,00
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        MPos.PosEslestirDto Dto(decimal tutar) => new()
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t1, Tutar = tutar }],
        };

        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(1_000m), k.Islem);
        string ilk;
        using (var db = vt.Olustur(k))
            ilk = db.PosYatmaEslesmeleri.Single(e => e.PosTahsilatId == t1).Id;
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).EslesmeGeriAl(ilk,
                new MPos.PosEslesmeGeriAlDto { Gerekce = "Yanlış tutar girildi, yeniden eşleştirilecek" }, k.Islem);
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(1_000m), k.Islem);   // aynı çift yeniden
        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(Dto(470m), k.Islem);     // aynı çift, ikinci kısmi

        using (var db = vt.Olustur(k))
        {
            var satirlar = db.PosYatmaEslesmeleri.Where(e => e.PosTahsilatId == t1)
                .OrderBy(e => e.CiftSira).ToList();
            Assert.Equal(new[] { 1, 2, 3 }, satirlar.Select(e => e.CiftSira).ToArray());
            Assert.True(satirlar[0].GeriAlindi);                        // iz silinmedi
            Assert.Equal("Yanlış tutar girildi, yeniden eşleştirilecek", satirlar[0].GeriAlmaGerekcesi);
            Assert.False(satirlar[1].GeriAlindi);
            Assert.False(satirlar[2].GeriAlindi);
            Assert.Equal(1_470m, satirlar.Where(e => !e.GeriAlindi).Sum(e => e.EslesenTutar));
            var t = db.PosTahsilatlari.Single(x => x.Id == t1);
            Assert.Equal(1_470m, t.YatanTutar);
            Assert.Equal("ESLESTI", db.BankaHareketleri.Single(h => h.Id == hareket).Durum);
        }
    }

    // Yarış koruması veritabanında kaldı: aynı çiftin AYNI sıradaki eşleşmesi ikinci kez
    // yazılamaz (eşzamanlı iki istek aynı sırayı hesaplar → ikincisi reddedilir).
    [Fact]
    public async Task ZO28k_ayni_cift_ayni_sira_iki_kez_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 sira tekil");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t1 = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 5_000m);
        PosYatmaEslesme Satir() => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, PosTahsilatId = t1, BankaHareketId = hareket,
            EslesenTutar = 100m, EslestirenKullaniciId = k.Islem.Id, CiftSira = 1, Olusturuldu = DateTime.UtcNow,
        };
        using (var db = vt.OlusturSistem())
        {
            db.PosYatmaEslesmeleri.Add(Satir());
            await db.SaveChangesAsync();
        }
        using (var db = vt.OlusturSistem())
        {
            db.PosYatmaEslesmeleri.Add(Satir());
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    // Öneri, kısmen eşleşmiş harekette kümeyi hareketin KALANINA eşitler (tam tutarına değil).
    [Fact]
    public async Task ZO28l_oneri_kismi_eslesmis_harekette_KALANA_esitlenen_kumeyi_bulur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 oneri kalan");
        var garanti = PosYardim.CihazYaz(vt, k);                                   // %1,85
        var ziraat = PosYardim.CihazYaz(vt, k, ad: "Ziraat POS", oran: 0m);        // komisyonsuz
        var a = PosYardim.TahsilatYaz(vt, k, garanti, 10_000m);                     // net 9.815,00
        var b = PosYardim.TahsilatYaz(vt, k, ziraat, 2_185m);                       // net 2.185,00
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 12_000m);

        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = a, Tutar = 9_815m }],
            }, k.Islem);
        using (var db = vt.Olustur(k))
        {
            var oneri = await PosYardim.Servis(db)
                .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem);
            var kok = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(oneri)).RootElement;
            Assert.True(kok.GetProperty("bulundu").GetBoolean());
            Assert.Equal(2_185m, kok.GetProperty("hareketKalan").GetDecimal());
            var s = Assert.Single(kok.GetProperty("satirlar").EnumerateArray());
            Assert.Equal(b, s.GetProperty("posTahsilatId").GetString());
        }
    }
}
