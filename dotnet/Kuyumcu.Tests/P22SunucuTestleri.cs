using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fatura;
using Kuyumcu.Api.Modules.Sayim;
using Microsoft.EntityFrameworkCore;
using MFatura = Kuyumcu.Api.Modules.Fatura;
using MPos = Kuyumcu.Api.Modules.Pos;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-22 — SATIŞ / FATURA / SİPARİŞ / SAYIM / POS EKRAN BORÇLARI
//
// Kimlikler: B-3 (taslak fatura düzenlenebilir + seri tek noktada tüketilir) ·
// B-26/K-23 (sipariş geri dönüşü) · B-33 (sayım DURUM sütunu) · B-44 (sayım
// oturumunun adı) · B-45 (gün sonu kasa sayımı) · B-46/B-55/B-56 (POS bağlı
// kasa düzeltme yolu ve göç akışı).
//
// Her test BİR iş kuralını yeniden üretir; mutasyon kanıtı raporda.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P22SunucuTestleri(TestVeritabani vt)
{
    private static MFatura.FaturaServisi Fatura(KuyumcuDbContext db) => new(db);
    private static SayimServisi Sayim(KuyumcuDbContext db) => new(db, new MUrun.UrunServisi(db));
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperty(ad)!.GetValue(o)!;

    // ═══ B-3 · SERİ YALNIZ KESİMDE TÜKETİLİR ═══════════════════════════════

    [Fact]
    public async Task B3_TASLAK_fatura_numara_TUKETMEZ_kesimde_alir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-seri");
        using var db = vt.Olustur(k);

        var t = await Fatura(db).Olustur(
            new MFatura.FaturaOlusturDto { CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m }, k.Islem);
        var id = Prop<string>(t, "Id");

        using (var oku = vt.OlusturSistem())
        {
            var f = oku.Faturalar.Single(x => x.Id == id);
            Assert.Null(f.FaturaNo);          // ← taslak seri YAKMAZ
            Assert.Equal("TASLAK", f.Durum);
        }

        await Fatura(db).DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);

        using var son = vt.OlusturSistem();
        Assert.StartsWith("FTR-", son.Faturalar.Single(x => x.Id == id).FaturaNo);
    }

    [Fact]
    public async Task B3_IPTAL_edilen_TASLAK_seride_BOSLUK_birakmaz()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-bosluk");
        using var db = vt.Olustur(k);

        // Üç taslak açılır, ikisi iptal edilir, biri kesilir.
        var idler = new List<string>();
        for (var i = 0; i < 3; i++)
            idler.Add(Prop<string>(await Fatura(db).Olustur(
                new MFatura.FaturaOlusturDto { CariId = k.CariId, Toplam = 600m, KdvOran = 0.20m },
                k.Islem), "Id"));

        await Fatura(db).DurumGuncelle(idler[0], new MFatura.DurumGuncelleDto { Durum = "IPTAL" }, k.Islem);
        await Fatura(db).DurumGuncelle(idler[1], new MFatura.DurumGuncelleDto { Durum = "IPTAL" }, k.Islem);
        await Fatura(db).DurumGuncelle(idler[2], new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);

        using var oku = vt.OlusturSistem();
        var yil = DateTime.UtcNow.Year;
        // Eski davranışta üç taslak da numara yakardı ve kesilen belge 000003
        // olurdu. Artık seride boşluk YOKTUR: ilk kesilen belge 000001'dir.
        Assert.Equal($"FTR-{yil}-000001", oku.Faturalar.Single(f => f.Id == idler[2]).FaturaNo);
        Assert.All(new[] { idler[0], idler[1] },
            i => Assert.Null(oku.Faturalar.Single(f => f.Id == i).FaturaNo));
    }

    [Fact]
    public async Task B3_TASLAK_basligi_duzenlenir_KESILMIS_belge_duzenlenmez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-taslak-duzenle");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Fatura(db).Olustur(
            new MFatura.FaturaOlusturDto { CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m },
            k.Islem), "Id");

        await Fatura(db).TaslakGuncelle(id, Govde("""{"aciklama":"düzeltildi","toplam":2400}"""), k.Islem);

        using (var oku = vt.OlusturSistem())
        {
            var f = oku.Faturalar.Single(x => x.Id == id);
            Assert.Equal("düzeltildi", f.Aciklama);
            Assert.Equal(2_400.00m, f.Toplam);
            Assert.Equal(400.00m, f.KdvTutar);     // 2400 − 2400/1,20
            Assert.Equal(2_000.00m, f.AraToplam);
        }

        await Fatura(db).DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Fatura(db).TaslakGuncelle(id, Govde("""{"aciklama":"olmaz"}"""), k.Islem));
        Assert.Contains("TASLAK", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task B3_PATCH_gonderilmeyen_alani_KORUR()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-patch-koru");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Fatura(db).Olustur(
            new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m, Aciklama = "ilk not",
            }, k.Islem), "Id");

        // Yalnız tarih gönderilir; açıklama ve toplam KORUNUR.
        await Fatura(db).TaslakGuncelle(id, Govde("""{"tarih":"2026-03-05"}"""), k.Islem);

        using var oku = vt.OlusturSistem();
        var f = oku.Faturalar.Single(x => x.Id == id);
        Assert.Equal("ilk not", f.Aciklama);
        Assert.Equal(1_200.00m, f.Toplam);
        Assert.Equal(new DateTime(2026, 3, 5), f.Tarih);

        // null → SİL sözleşmesi
        await Fatura(db).TaslakGuncelle(id, Govde("""{"aciklama":null}"""), k.Islem);
        using var oku2 = vt.OlusturSistem();
        Assert.Null(oku2.Faturalar.Single(x => x.Id == id).Aciklama);
    }

    [Fact]
    public async Task B3_KALEMLI_faturanin_toplami_BASLIKTAN_degistirilemez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-kalemli-toplam");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Fatura(db).Olustur(
            new MFatura.FaturaOlusturDto { CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m },
            k.Islem), "Id");

        using (var yaz = vt.OlusturSistem())
        {
            yaz.Faturalar.Single(f => f.Id == id).KalemKaynagi = "KALEM";
            yaz.SaveChanges();
        }

        // Taze bağlam: dışarıdan değiştirilen satır izlenen kopyadan okunmasın.
        using var taze = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Fatura(taze).TaslakGuncelle(id, Govde("""{"toplam":9999}"""), k.Islem));
        Assert.Contains("kalem", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task B3_seri_kapisi_TASLAK_belgeye_numara_VERMEZ()
    {
        using var db = vt.OlusturSistem();
        var fatura = new Fatura { Id = "x", TenantId = "t", Durum = "TASLAK" };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FaturaSeriKapisi.Tuket(db, fatura, 2026));
    }

    // ═══ B-44 · SAYIM OTURUMUNUN ADI DÜZELTİLEBİLİR ════════════════════════

    [Fact]
    public async Task B44_sayim_oturumunun_adi_duzeltilir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-sayim-ad");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Sayim(db).Ac(
            new SayimAcDto { Ad = "Vitirn", SubeId = k.SubeId }, k.Islem), "Id");

        await Sayim(db).Guncelle(id, Govde("""{"ad":"Vitrin sayımı"}"""), k.Islem);

        using var oku = vt.OlusturSistem();
        Assert.Equal("Vitrin sayımı", oku.SayimOturumlari.Single(s => s.Id == id).Ad);
    }

    [Fact]
    public async Task B44_bos_ad_400_doner_ve_KAYIT_DEGISMEZ()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-sayim-bos");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Sayim(db).Ac(
            new SayimAcDto { Ad = "Vitrin", SubeId = k.SubeId }, k.Islem), "Id");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Sayim(db).Guncelle(id, Govde("""{"ad":"  "}"""), k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Sayim(db).Guncelle(id, Govde("""{"ad":null}"""), k.Islem));

        using var oku = vt.OlusturSistem();
        Assert.Equal("Vitrin", oku.SayimOturumlari.Single(s => s.Id == id).Ad);
    }

    [Fact]
    public async Task B44_IPTAL_oturum_duzenlenemez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "P22-sayim-iptal");
        using var db = vt.Olustur(k);

        var id = Prop<string>(await Sayim(db).Ac(
            new SayimAcDto { Ad = "Vitrin", SubeId = k.SubeId }, k.Islem), "Id");
        await Sayim(db).Iptal(id, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Sayim(db).Guncelle(id, Govde("""{"ad":"yeni"}"""), k.Islem));
        Assert.Contains("İptal", ex.Message, StringComparison.Ordinal);
    }

    // ═══ B-55 · POS KASA GÖÇÜ (tahsilat VARKEN) ════════════════════════════

    [Fact]
    public async Task B55_tahsilat_VARKEN_kasa_gocu_TUTANAKLA_yapilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P22-pos-goc");
        var cihazId = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihazId, 1_000m);
        var yeniKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        using var db = vt.Olustur(k);
        var sonuc = await PosYardim.Servis(db).KasaGoc(cihazId, new MPos.PosKasaGocDto
        {
            YeniKasaId = yeniKasa,
            Gerekce = "Kasa yanlış bağlanmıştı, doğru kasaya taşındı.",
        }, k.Islem);

        Assert.Equal(1, Prop<int>(sonuc, "tahsilatSayisi"));

        using var oku = vt.OlusturSistem();
        Assert.Equal(yeniKasa, oku.PosCihazlari.AsNoTracking().Single(c => c.Id == cihazId).KasaId);
        var goc = oku.PosKasaGocleri.AsNoTracking().Single(g => g.PosCihazId == cihazId);
        Assert.Equal(k.KasaId, goc.EskiKasaId);
        Assert.Equal(yeniKasa, goc.YeniKasaId);
        Assert.Equal(1, goc.TahsilatSayisi);
        Assert.Equal(k.Islem.Id, goc.KullaniciId);
        // GEÇMİŞ DEĞİŞMEZ: yazılmış tahsilat satırı olduğu gibi durur.
        Assert.Single(oku.PosTahsilatlari.AsNoTracking().Where(t => t.PosCihazId == cihazId));
    }

    [Fact]
    public async Task B55_gerekcesiz_kasa_gocu_400_doner_ve_kasa_DEGISMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P22-pos-goc-gerekce");
        var cihazId = PosYardim.CihazYaz(vt, k);
        var yeniKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            PosYardim.Servis(db).KasaGoc(cihazId,
                new MPos.PosKasaGocDto { YeniKasaId = yeniKasa, Gerekce = "kısa" }, k.Islem));

        using var oku = vt.OlusturSistem();
        Assert.Equal(k.KasaId, oku.PosCihazlari.AsNoTracking().Single(c => c.Id == cihazId).KasaId);
        Assert.Empty(oku.PosKasaGocleri.AsNoTracking().Where(g => g.PosCihazId == cihazId));
    }

    [Fact]
    public async Task B55_goc_tutanagi_APPEND_ONLY_dir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P22-pos-goc-iz");
        var cihazId = PosYardim.CihazYaz(vt, k);
        var yeniKasa = TestKurulum.KasaEkle(vt.OlusturSistem(), k.TenantId, k.SubeId, "TRY");

        using (var db = vt.Olustur(k))
            await PosYardim.Servis(db).KasaGoc(cihazId, new MPos.PosKasaGocDto
            {
                YeniKasaId = yeniKasa, Gerekce = "Şube değişikliği nedeniyle taşındı.",
            }, k.Islem);

        using var yaz = vt.OlusturSistem();
        var goc = yaz.PosKasaGocleri.Single(g => g.PosCihazId == cihazId);
        goc.Gerekce = "üzeri örtüldü";
        Assert.Throws<SiparisGecisiDegistirilemez>(() => yaz.SaveChanges());
    }

    private static JsonElement Govde(string json) => JsonDocument.Parse(json).RootElement;
}
