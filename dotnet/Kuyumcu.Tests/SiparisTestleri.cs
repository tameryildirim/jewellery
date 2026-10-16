using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Siparis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ ÇATISI — SERVİS/VERİTABANI testleri (paket 3 Faz A).
//
// ⚠ DEFTER TEKELİ KANITI: her yazma işleminden önce ve sonra hareketler ve
//   kasa_hareketleri SATIR SAYISI karşılaştırılır. Sipariş modülü deftere
//   yazarsa bu testler kırmızıya döner.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class SiparisTestleri(TestVeritabani vt)
{
    private static SiparisServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<SiparisServisi>.Instance);

    private static SiparisOlusturDto Dto(Kiraci k, string tur = SiparisTuru.MusteriSiparisi) => new()
    {
        Tur = tur,
        CariId = k.CariId,
        SozVerilenGun = Zaman.YerelBugun().AddDays(7),
        Aciklama = "Vitrinde olmayan bilezik",
        Kalemler = [new SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }],
    };

    private static string Kimlik_(object cevap) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cevap))
            .RootElement.GetProperty("id").GetString()!;

    private static string Alan(object cevap, string ad) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cevap))
            .RootElement.GetProperty(ad).GetString()!;

    private static (int Hareket, int Kasa) DefterSayimi(KuyumcuDbContext db, string tenantId) =>
        (db.Hareketler.Count(h => h.TenantId == tenantId),
         db.KasaHareketleri.Count(h => h.TenantId == tenantId));

    // ── Oluşturma ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Siparis_TASLAK_dogar_ve_ilk_gecis_satiri_yazilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-TASLAK");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).Olustur(Dto(k), k.Islem);
        var id = Kimlik_(cevap);

        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisDurumu.Taslak, s.Durum);
        Assert.StartsWith($"S-{Zaman.YerelBugun().Year}-", s.No);

        var gecisler = await db.SiparisGecisleri.AsNoTracking().Where(g => g.SiparisId == id).ToListAsync();
        var tek = Assert.Single(gecisler);
        Assert.Null(tek.OncekiDurum);
        Assert.Equal(SiparisDurumu.Taslak, tek.YeniDurum);
    }

    [Fact]
    public async Task Kalemsiz_siparis_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-KALEMSIZ");
        using var db = vt.Olustur(k);

        var dto = Dto(k);
        dto.Kalemler = [];
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Olustur(dto, k.Islem));
    }

    [Fact]
    public async Task Urun_ve_tanim_ikisi_de_bossa_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-BOSKALEM");
        using var db = vt.Olustur(k);

        var dto = Dto(k);
        dto.Kalemler = [new SiparisKalemDto { Adet = 1m }];
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Olustur(dto, k.Islem));
        Assert.Contains("tanım", hata.Message);
    }

    [Fact]
    public async Task Tahmini_tutar_varken_kur_yoksa_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-KURSUZ");
        using var db = vt.Olustur(k);

        var dto = Dto(k);
        dto.TahminiTutarTl = 25_000m;
        dto.KurSnapshotId = null;
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Olustur(dto, k.Islem));

        // Kur verilince kabul edilir — kural "kur zorunlu", "tutar yasak" değil.
        dto.KurSnapshotId = k.KurId;
        var cevap = await Servis(db).Olustur(dto, k.Islem);
        Assert.NotEmpty(Kimlik_(cevap));
    }

    [Fact]
    public async Task Yabanci_kiracinin_kuru_ve_carisi_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P3-A");
        var b = TestKurulum.KiraciKur(sistem, "P3-B");
        using var db = vt.Olustur(a);

        var dto = Dto(a);
        dto.CariId = b.CariId;
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).Olustur(dto, a.Islem));

        var dto2 = Dto(a);
        dto2.TahminiTutarTl = 100m;
        dto2.KurSnapshotId = b.KurId;
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).Olustur(dto2, a.Islem));
    }

    // ── Geçişler ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Yasak_gecis_TASLAKtan_TESLIM_EDILDIye_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-YASAK");
        using var db = vt.Olustur(k);
        var id = Kimlik_(await Servis(db).Olustur(Dto(k), k.Islem));

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.TeslimEdildi }, k.Islem));
        Assert.Contains("geçilemez", hata.Message);

        // Durum DEĞİŞMEDİ ve log BÜYÜMEDİ (M146/M147 ölçümü: hata enjeksiyonu)
        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisDurumu.Taslak, s.Durum);
        Assert.Equal(1, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));
    }

    [Fact]
    public async Task Iptal_gerekcesiz_reddedilir_gerekceliyse_gecis_satiri_yazilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-IPTAL");
        using var db = vt.Olustur(k);
        var id = Kimlik_(await Servis(db).Olustur(Dto(k), k.Islem));

        // M145/M147 — gerekçesiz iptal: 400 + NE durum NE log değişir.
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Iptal }, k.Islem));
        var once = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisDurumu.Taslak, once.Durum);
        Assert.Equal(1, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));

        // Kısa gerekçe de reddedilir (asgari 10 karakter)
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Iptal, Gerekce = "kısa" }, k.Islem));
        Assert.Equal(1, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));

        await Servis(db).Gecis(id,
            new SiparisGecisDto { YeniDurum = SiparisDurumu.Iptal, Gerekce = "Müşteri vazgeçti, kapora iade edildi" },
            k.Islem);

        var sonra = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisDurumu.Iptal, sonra.Durum);
        var gecisler = await db.SiparisGecisleri.AsNoTracking()
            .Where(g => g.SiparisId == id).OrderBy(g => g.Olusturuldu).ToListAsync();
        Assert.Equal(2, gecisler.Count);
        Assert.Equal(SiparisDurumu.Taslak, gecisler[1].OncekiDurum);
        Assert.Contains("vazgeçti", gecisler[1].Gerekce!);
    }

    [Fact]
    public async Task Gecis_dokumu_her_adimda_bir_satir_buyur()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-ZINCIR");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        Assert.Equal(1, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
        Assert.Equal(2, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
        Assert.Equal(3, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.TeslimEdildi }, k.Islem);

        var gecisler = await db.SiparisGecisleri.AsNoTracking()
            .Where(g => g.SiparisId == id).OrderBy(g => g.Olusturuldu).ToListAsync();
        Assert.Equal(4, gecisler.Count);
        Assert.Equal(
            new[] { "TASLAK", "ONAYLANDI", "HAZIR", "TESLIM_EDILDI" },
            gecisler.Select(g => g.YeniDurum).ToArray());
    }

    [Fact]
    public async Task Gecis_izi_APPEND_ONLYdir_guncelleme_ve_silme_istisna_atar()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-APPENDONLY");
        using var db = vt.Olustur(k);
        var id = Kimlik_(await Servis(db).Olustur(Dto(k), k.Islem));

        var satir = await db.SiparisGecisleri.SingleAsync(g => g.SiparisId == id);
        satir.Gerekce = "sonradan yazıldı";
        await Assert.ThrowsAsync<SiparisGecisiDegistirilemez>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var silinecek = await db.SiparisGecisleri.SingleAsync(g => g.SiparisId == id);
        db.SiparisGecisleri.Remove(silinecek);
        await Assert.ThrowsAsync<SiparisGecisiDegistirilemez>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        Assert.Equal(1, await db.SiparisGecisleri.CountAsync(g => g.SiparisId == id));
    }

    // ── PATCH ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TESLIM_EDILDI_sonrasi_PATCH_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-TERMINAL");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.TeslimEdildi }, k.Islem);

        var govde = System.Text.Json.JsonDocument.Parse("""{"aciklama":"sonradan"}""").RootElement;
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => servis.Guncelle(id, govde, k.Islem));
        Assert.Contains("TASLAK", hata.Message);

        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Vitrinde olmayan bilezik", s.Aciklama);
    }

    [Fact]
    public async Task PATCH_sozlesmesi_gonderilmeyen_alani_korur_null_alani_siler()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-PATCH");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        // Yalnız açıklama gönderilir → söz verilen gün KORUNUR.
        var g1 = System.Text.Json.JsonDocument.Parse("""{"aciklama":"yeni not"}""").RootElement;
        await servis.Guncelle(id, g1, k.Islem);
        var s1 = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("yeni not", s1.Aciklama);
        Assert.NotNull(s1.SozVerilenGun);

        // null gönderilir → SİLİNİR.
        var g2 = System.Text.Json.JsonDocument.Parse("""{"sozVerilenGun":null}""").RootElement;
        await servis.Guncelle(id, g2, k.Islem);
        var s2 = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Null(s2.SozVerilenGun);
        Assert.Equal("yeni not", s2.Aciklama);   // dokunulmayan alan korundu
    }

    [Fact]
    public async Task PATCH_kalemleri_tumuyle_degistirir_ve_bos_dizi_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-KALEMPATCH");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        var bos = System.Text.Json.JsonDocument.Parse("""{"kalemler":[]}""").RootElement;
        await Assert.ThrowsAsync<BadRequestException>(() => servis.Guncelle(id, bos, k.Islem));

        var iki = System.Text.Json.JsonDocument.Parse(
            """{"kalemler":[{"tanim":"tektaş yüzük","adet":1},{"tanim":"künye","adet":2}]}""").RootElement;
        await servis.Guncelle(id, iki, k.Islem);

        var kalemler = await db.SiparisKalemleri.AsNoTracking()
            .Where(x => x.SiparisId == id).OrderBy(x => x.Sira).ToListAsync();
        Assert.Equal(2, kalemler.Count);
        Assert.Equal("tektaş yüzük", kalemler[0].Tanim);
        Assert.Equal(2m, kalemler[1].Adet);
    }

    [Fact]
    public async Task Iyimser_kilit_eskimis_damgayi_409_ile_reddeder()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-KILIT");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        var eski = (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Guncellendi;
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);

        await Assert.ThrowsAsync<ConflictException>(() => servis.Gecis(id,
            new SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir, Guncellendi = eski }, k.Islem));

        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisDurumu.Onaylandi, s.Durum);   // ikinci yazan sessizce kazanmadı
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_siparisi_404_doner()
    {
        using var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P3-IZO-A");
        var b = TestKurulum.KiraciKur(sistem, "P3-IZO-B");

        string id;
        using (var dba = vt.Olustur(a))
            id = Kimlik_(await Servis(dba).Olustur(Dto(a), a.Islem));

        using var dbb = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbb).Detay(id, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbb).Gecisler(id, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(dbb).Gecis(id,
            new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, b.Islem));
    }

    // ── Defter tekeli ────────────────────────────────────────────────────────

    [Fact]
    public async Task Siparis_yazimi_hicbir_hareket_ya_da_kasa_hareketi_uretmez()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-DEFTER");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var once = DefterSayimi(sistem, k.TenantId);

        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.TeslimEdildi }, k.Islem);

        var sonra = DefterSayimi(sistem, k.TenantId);
        Assert.Equal(once, sonra);
        // Stok da değişmez
        Assert.Equal(100m, sistem.Stoklar.Single(s => s.UrunId == k.UrunId).Adet);
    }

    // ── Liste ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Liste_geciken_suzgeci_sunucu_gunune_gore_calisir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-GECIKEN");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var dunku = Dto(k);
        dunku.SozVerilenGun = Zaman.YerelBugun();
        var gecikenId = Kimlik_(await servis.Olustur(dunku, k.Islem));
        // PAKET GOC-C (envanter #9): söz günü KAYIT ANINDA geçmiş olamaz — gecikme ZAMANLA doğar.
        // Kaydı bir gün geriye çekerek "dün söz verilmiş" sipariş canlandırılır (sunucu gün hesabı AYNI).
        var kayit = await db.Siparisler.FirstAsync(x => x.Id == gecikenId);
        kayit.SozVerilenGun = Zaman.YerelBugun().AddDays(-1);
        await db.SaveChangesAsync();

        var ileri = Dto(k);
        ileri.SozVerilenGun = Zaman.YerelBugun().AddDays(5);
        await servis.Olustur(ileri, k.Islem);

        var cevap = await servis.Listele(null, null, null, null, true, null, k.Islem);
        var kok = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;
        var satirlar = kok.GetProperty("siparisler").EnumerateArray().ToList();
        Assert.Single(satirlar);
        Assert.Equal(gecikenId, satirlar[0].GetProperty("id").GetString());
        Assert.True(satirlar[0].GetProperty("geciken").GetBoolean());
        Assert.Equal(1, kok.GetProperty("gecikenSayisi").GetInt32());
    }

    [Fact]
    public async Task Liste_azami_200_satir_doner_ve_toplam_sunucuda_hesaplanir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-LIMIT");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        for (var i = 0; i < 3; i++) await servis.Olustur(Dto(k), k.Islem);

        var cevap = await servis.Listele(null, null, null, null, null, 1000, k.Islem);
        var kok = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;
        Assert.Equal(200, kok.GetProperty("limit").GetInt32());
        Assert.Equal(1000, kok.GetProperty("istenenLimit").GetInt32());
        Assert.Equal(3, kok.GetProperty("toplam").GetInt32());
        Assert.False(kok.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    // ── Sipariş numarası ─────────────────────────────────────────────────────

    [Fact]
    public async Task Ardisik_siparisler_farkli_numara_alir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-NO");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var n1 = Alan(await servis.Olustur(Dto(k), k.Islem), "no");
        var n2 = Alan(await servis.Olustur(Dto(k), k.Islem), "no");
        Assert.NotEqual(n1, n2);
    }

    [Fact]
    public async Task Iki_paralel_olusturma_ayni_numarayi_alamaz()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-PARALEL");

        // İKİ AYRI DbContext = iki ayrı bağlantı; numara çakışması gerçekten yarışır.
        using var db1 = vt.Olustur(k);
        using var db2 = vt.Olustur(k);
        var t1 = Servis(db1).Olustur(Dto(k), k.Islem);
        var t2 = Servis(db2).Olustur(Dto(k), k.Islem);
        await Task.WhenAll(t1, t2);

        var nolar = await sistem.Siparisler.AsNoTracking()
            .Where(s => s.TenantId == k.TenantId).Select(s => s.No).ToListAsync();
        Assert.Equal(2, nolar.Count);
        Assert.Equal(2, nolar.Distinct().Count());
    }

    // ── TAMIR / emanet bağı ──────────────────────────────────────────────────

    [Fact]
    public async Task Tamir_siparisine_emanet_hareketi_opsiyonel_baglanir_yabanci_hareket_reddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-TAMIR");
        var yabanci = TestKurulum.KiraciKur(sistem, "P3-TAMIR-YABANCI");

        // Emanet girişini MEVCUT emanet ucundan yaz (sipariş modülü has bacağına yazmaz).
        using (var dbe = vt.Olustur(k))
        {
            var emanet = new Kuyumcu.Api.Modules.Emanet.EmanetServisi(
                dbe, NullLogger<Kuyumcu.Api.Modules.Emanet.EmanetServisi>.Instance);
            await emanet.Giris(new Kuyumcu.Api.Modules.Emanet.EmanetGirisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 5m, KurSnapshotId = k.KurId,
            }, k.Islem);
        }

        using var db = vt.Olustur(k);
        var hareketId = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS")
            .Select(h => h.Id).FirstAsync();

        // Emanetsiz TAMIR kabul edilir (opsiyonel).
        var opsiyonel = Dto(k, SiparisTuru.Tamir);
        opsiyonel.Kalemler = [new SiparisKalemDto { Tanim = "zincir lehimi" }];
        Assert.NotEmpty(Kimlik_(await Servis(db).Olustur(opsiyonel, k.Islem)));

        // Bağlı TAMIR de kabul edilir.
        var bagli = Dto(k, SiparisTuru.Tamir);
        bagli.Kalemler = [new SiparisKalemDto { Tanim = "bilezik tamiri" }];
        bagli.EmanetHareketId = hareketId;
        var id = Kimlik_(await Servis(db).Olustur(bagli, k.Islem));
        Assert.Equal(hareketId,
            (await db.Siparisler.AsNoTracking().SingleAsync(s => s.Id == id)).EmanetHareketId);

        // Yabancı kiracının hareketi reddedilir.
        using (var dby = vt.Olustur(yabanci))
        {
            var emanet2 = new Kuyumcu.Api.Modules.Emanet.EmanetServisi(
                dby, NullLogger<Kuyumcu.Api.Modules.Emanet.EmanetServisi>.Instance);
            await emanet2.Giris(new Kuyumcu.Api.Modules.Emanet.EmanetGirisDto
            {
                CariId = yabanci.CariId, AyarId = "K22", GramAgirlik = 2m, KurSnapshotId = yabanci.KurId,
            }, yabanci.Islem);
        }
        var yabanciHareket = await sistem.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == yabanci.TenantId && h.Tur == "EMANET_GIRIS")
            .Select(h => h.Id).FirstAsync();

        var kotu = Dto(k, SiparisTuru.Tamir);
        kotu.Kalemler = [new SiparisKalemDto { Tanim = "yüzük" }];
        kotu.EmanetHareketId = yabanciHareket;
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).Olustur(kotu, k.Islem));

        // Emanet bağı YALNIZ TAMIR türünde kurulur.
        var musteri = Dto(k);
        musteri.EmanetHareketId = hareketId;
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Olustur(musteri, k.Islem));
    }

    [Fact]
    public async Task Global_kiraci_filtresi_siparis_tablolarini_kapsar()
    {
        // ⚠ Servisteki açık `TenantId ==` koşulu İKİNCİ katmandır; ASIL kapı
        //   KuyumcuDbContext'in global sorgu filtresidir. Bu test o kapıyı
        //   doğrudan ölçer: predicate'siz sorgu bile yabancı satırı görmemeli.
        using var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P3-FILTRE-A");
        var b = TestKurulum.KiraciKur(sistem, "P3-FILTRE-B");

        string id;
        using (var dba = vt.Olustur(a)) id = Kimlik_(await Servis(dba).Olustur(Dto(a), a.Islem));

        using var dbb = vt.Olustur(b);
        Assert.Empty(await dbb.Siparisler.AsNoTracking().ToListAsync());
        Assert.Empty(await dbb.SiparisKalemleri.AsNoTracking().ToListAsync());
        Assert.Empty(await dbb.SiparisGecisleri.AsNoTracking().ToListAsync());
        Assert.Null(await dbb.Siparisler.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id));
    }

    [Fact]
    public async Task Ayni_kiracida_baska_carinin_emanet_hareketi_tamire_baglanamaz()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-EMANET-CARI");

        // Aynı kiracıda İKİNCİ bir cari ve ONUN emanet girişi.
        var ikinciCariId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        sistem.Cariler.Add(new Cari
        {
            Id = ikinciCariId, TenantId = k.TenantId, Tur = "MUSTERI", Ad = "İkinci müşteri",
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        sistem.CariBakiyeler.Add(new CariBakiye
        {
            CariId = ikinciCariId, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        await sistem.SaveChangesAsync();

        using (var dbe = vt.Olustur(k))
        {
            var emanet = new Kuyumcu.Api.Modules.Emanet.EmanetServisi(
                dbe, NullLogger<Kuyumcu.Api.Modules.Emanet.EmanetServisi>.Instance);
            await emanet.Giris(new Kuyumcu.Api.Modules.Emanet.EmanetGirisDto
            {
                CariId = ikinciCariId, AyarId = "K22", GramAgirlik = 3m, KurSnapshotId = k.KurId,
            }, k.Islem);
        }

        using var db = vt.Olustur(k);
        var baskaninHareketi = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS" && h.CariId == ikinciCariId)
            .Select(h => h.Id).FirstAsync();

        // Sipariş k.CariId'ye ait; başka müşterinin emaneti buraya BAĞLANAMAZ.
        var dto = Dto(k, SiparisTuru.Tamir);
        dto.Kalemler = [new SiparisKalemDto { Tanim = "kolye tamiri" }];
        dto.EmanetHareketId = baskaninHareketi;
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(db).Olustur(dto, k.Islem));
    }

    [Fact]
    public async Task Servisin_kendi_tenant_sarti_global_filtre_OLMASA_da_yabanci_siparisi_reddeder()
    {
        // ⚠ İKİ KATMAN AYRI AYRI ÖLÇÜLÜR: bu test bağlamı FİLTRESİZ (sistem)
        //   kurar, böylece geriye yalnız servisin KENDİ `TenantId ==` koşulu
        //   kalır. Koşul kaldırılırsa (mutasyon M148-1) test kırmızıya döner —
        //   aksi hâlde o satır ölçülmeyen bir savunma olurdu.
        using var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "P3-SERVISKAPI-A");
        var b = TestKurulum.KiraciKur(sistem, "P3-SERVISKAPI-B");

        string id;
        using (var dba = vt.Olustur(a)) id = Kimlik_(await Servis(dba).Olustur(Dto(a), a.Islem));

        var filtresiz = Servis(sistem);
        await Assert.ThrowsAsync<NotFoundException>(() => filtresiz.Detay(id, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => filtresiz.Gecisler(id, b.Islem));
        // Kendi kiracısı için AYNI filtresiz bağlamda erişim çalışır (test doğru şeyi ölçüyor).
        Assert.NotEmpty(Kimlik_(await filtresiz.Detay(id, a.Islem)));
    }

    [Fact]
    public async Task Guncel_damga_KABUL_edilir_Zli_ve_Zsiz_bicimde()
    {
        // ⚠ CANLI DOĞRULAMA BULGUSU (2026-09-16): kilit "eskimiş damgayı
        //   reddediyor mu" diye ölçülmüş ama "GÜNCEL damgayı kabul ediyor mu"
        //   diye ölçülmemişti. Sunucu damgayı Z'siz (Kind=Unspecified) yazıyor;
        //   koşulsuz ToUniversalTime() onu yerel sanıp +3 saat kaydırıyor ve
        //   HER geçiş 409 dönüyordu. Bu test o yolu kapatır.
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P3-DAMGA");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        // Veritabanından okunan hâli (Kind=Unspecified) — ekranın gördüğü değer.
        var damga = (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Guncellendi;
        await servis.Gecis(id,
            new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi, Guncellendi = damga }, k.Islem);
        Assert.Equal(SiparisDurumu.Onaylandi,
            (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Durum);

        // Aynı an, açıkça UTC işaretli (Z'li) gönderilirse de kabul edilir.
        var damga2 = (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Guncellendi;
        await servis.Gecis(id, new SiparisGecisDto
        {
            YeniDurum = SiparisDurumu.Hazir,
            Guncellendi = DateTime.SpecifyKind(damga2, DateTimeKind.Utc),
        }, k.Islem);
        Assert.Equal(SiparisDurumu.Hazir,
            (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Durum);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAKET P-51 — BAŞLIK KİMLİĞİ (cari · tür · şube) TASLAKTA DÜZENLENİR
    //
    // QA turu 8 / KRİTİK: yanlış cariye ya da yanlış türe açılmış TASLAK sipariş
    // yalnız İPTAL edilerek "düzeltilebiliyordu"; geçiş dökümü de bu yüzden yalan
    // söylüyordu. Kapı DEĞİŞMEDİ: TASLAK dışı sipariş hâlâ donmuştur.
    // ═══════════════════════════════════════════════════════════════════════

    private static string IkinciCariEkle(KuyumcuDbContext sistem, string tenantId, string ad)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        sistem.Cariler.Add(new Cari
        {
            Id = id, TenantId = tenantId, Tur = "MUSTERI", Ad = ad,
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        sistem.CariBakiyeler.Add(new CariBakiye
        {
            CariId = id, TenantId = tenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        sistem.SaveChanges();
        return id;
    }

    [Fact]
    public async Task TaslakSiparisinCarisiDegistirilebilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P51-CARI");
        var ikinciCari = IkinciCariEkle(sistem, k.TenantId, "İkinci müşteri");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        var govde = System.Text.Json.JsonDocument
            .Parse($$"""{"cariId":"{{ikinciCari}}"}""").RootElement;
        var cevap = await servis.Guncelle(id, govde, k.Islem);

        Assert.Equal(ikinciCari, Alan(cevap, "cariId"));
        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(ikinciCari, s.CariId);
        // Gönderilmeyen alanlar KORUNDU (PATCH sözleşmesi).
        Assert.Equal("Vitrinde olmayan bilezik", s.Aciklama);
        Assert.Equal(SiparisTuru.MusteriSiparisi, s.Tur);
    }

    [Fact]
    public async Task TaslakDisiSiparisteBaslikPatchReddedilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P51-TASLAKDISI");
        var ikinciCari = IkinciCariEkle(sistem, k.TenantId, "İkinci müşteri");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));
        await servis.Gecis(id, new SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);

        var govde = System.Text.Json.JsonDocument
            .Parse($$"""{"cariId":"{{ikinciCari}}","tur":"TAMIR"}""").RootElement;
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => servis.Guncelle(id, govde, k.Islem));
        Assert.Contains(SiparisDurumu.Onaylandi, hata.Message);   // metin DURUM ADINI içerir

        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(k.CariId, s.CariId);
        Assert.Equal(SiparisTuru.MusteriSiparisi, s.Tur);
    }

    [Fact]
    public async Task TurDegisiminceGecersizBagAcikHataVerir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P51-TURBAG");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        // (a) MUSTERI_SIPARISI → ATOLYE_SIPARISI: usta carisi olmadan OLMAZ.
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));
        var atolye = System.Text.Json.JsonDocument
            .Parse("""{"tur":"ATOLYE_SIPARISI"}""").RootElement;
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => servis.Guncelle(id, atolye, k.Islem));
        Assert.Contains("usta carisi", hata.Message);
        Assert.Equal(SiparisTuru.MusteriSiparisi,
            (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).Tur);

        // (b) Usta carisi AYNI istekte gelirse geçiş yapılır (iki adımlık tuzak yok).
        var birlikte = System.Text.Json.JsonDocument
            .Parse($$"""{"tur":"ATOLYE_SIPARISI","ustaCariId":"{{k.CariId}}"}""").RootElement;
        await servis.Guncelle(id, birlikte, k.Islem);
        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisTuru.AtolyeSiparisi, s.Tur);
        Assert.Equal(k.CariId, s.UstaCariId);

        // (c) Usta carisi bağlıyken MUSTERI_SIPARISI'na dönüş SESSİZCE null'lamaz.
        var geri = System.Text.Json.JsonDocument
            .Parse("""{"tur":"MUSTERI_SIPARISI"}""").RootElement;
        var hata2 = await Assert.ThrowsAsync<BadRequestException>(
            () => servis.Guncelle(id, geri, k.Islem));
        Assert.Contains("usta carisi", hata2.Message);
        var s2 = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SiparisTuru.AtolyeSiparisi, s2.Tur);
        Assert.Equal(k.CariId, s2.UstaCariId);   // bağ DURUYOR
    }

    [Fact]
    public async Task SubeDegisimiKiraciDisiSubeyiReddeder()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P51-SUBE");
        var yabanci = TestKurulum.KiraciKur(sistem, "P51-YABANCI");
        var kendiSube2 = TestKurulum.SubeEkle(sistem, k.TenantId, "Şube-2");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        // Yabancı kiracının ŞUBESİ reddedilir.
        var yabanciSube = System.Text.Json.JsonDocument
            .Parse($$"""{"subeId":"{{yabanci.SubeId}}"}""").RootElement;
        await Assert.ThrowsAsync<NotFoundException>(() => servis.Guncelle(id, yabanciSube, k.Islem));

        // Yabancı kiracının CARİSİ de reddedilir (aynı kapının cari eşdeğeri).
        var yabanciCari = System.Text.Json.JsonDocument
            .Parse($$"""{"cariId":"{{yabanci.CariId}}"}""").RootElement;
        await Assert.ThrowsAsync<NotFoundException>(() => servis.Guncelle(id, yabanciCari, k.Islem));

        var s = await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(k.SubeId, s.SubeId);
        Assert.Equal(k.CariId, s.CariId);

        // KENDİ ikinci şubesi kabul edilir.
        var kendi = System.Text.Json.JsonDocument
            .Parse($$"""{"subeId":"{{kendiSube2}}"}""").RootElement;
        await servis.Guncelle(id, kendi, k.Islem);
        Assert.Equal(kendiSube2,
            (await db.Siparisler.AsNoTracking().SingleAsync(x => x.Id == id)).SubeId);
    }

    [Fact]
    public async Task BaslikDegisimiGecisDokumuneYazilir()
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "P51-IZ");
        var ikinciCari = IkinciCariEkle(sistem, k.TenantId, "İkinci müşteri");
        using var db = vt.Olustur(k);
        var servis = Servis(db);
        var id = Kimlik_(await servis.Olustur(Dto(k), k.Islem));

        // Başlık DEĞİŞMEYEN PATCH iz BIRAKMAZ (gürültü üretilmez).
        var sadeceAciklama = System.Text.Json.JsonDocument
            .Parse("""{"aciklama":"not"}""").RootElement;
        await servis.Guncelle(id, sadeceAciklama, k.Islem);
        Assert.Single(await db.SiparisGecisleri.AsNoTracking()
            .Where(g => g.SiparisId == id).ToListAsync());

        var govde = System.Text.Json.JsonDocument
            .Parse($$"""{"cariId":"{{ikinciCari}}","tur":"TAMIR"}""").RootElement;
        await servis.Guncelle(id, govde, k.Islem);

        var gecisler = await db.SiparisGecisleri.AsNoTracking()
            .Where(g => g.SiparisId == id).OrderBy(g => g.Olusturuldu).ToListAsync();
        Assert.Equal(2, gecisler.Count);
        var iz = gecisler[^1];
        Assert.Equal(SiparisDurumu.Taslak, iz.OncekiDurum);
        Assert.Equal(SiparisDurumu.Taslak, iz.YeniDurum);   // durum listesi ENFLASYONA UĞRAMADI
        Assert.StartsWith(SiparisServisi.BaslikDuzenlendiNotu, iz.Gerekce);
        Assert.Contains(ikinciCari, iz.Gerekce);
        Assert.Contains("TAMIR", iz.Gerekce);

        // Geçiş dökümü ucu da bu satırı gösterir.
        var dokum = System.Text.Json.JsonSerializer.Serialize(await servis.Gecisler(id, k.Islem));
        Assert.Contains(SiparisServisi.BaslikDuzenlendiNotu, dokum);
    }
}
