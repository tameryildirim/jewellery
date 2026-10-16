using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET KAPORA-ESZAMAN finansal sınır testleri.
// Kapsam: POST /siparisler/{id}/kapora artık satır kilidi + tek işlem içinde; kapora satırı
// hareketler.siparis_id ile bağlanır; Detay.kaporaGecmisi defterden okunur.
// Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// Yuvarlama: CariServisi.TahsilatGirHareket TL 2 hane, MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KAPORAESZAMANSinirTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static JsonElement J(object yanit) =>
        JsonSerializer.SerializeToElement(yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETKAPORA-ESZAMAN-TEST dg " + ad);
        sistem.SaveChanges();
        return k;
    }

    private static async Task<string> SiparisAc(KuyumcuDbContext db, Kiraci k)
    {
        var y = J(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "sinir",
            Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
        }, k.Islem));
        return y.GetProperty("id").GetString()!;
    }

    private static MSiparis.SiparisKaporaDto Govde(Kiraci k, decimal? tutar, string odeme = "HAVALE") => new()
    {
        TlTutari = tutar, OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private async Task DefterVeBagBos(Kiraci k, string id)
    {
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.SiparisId == id));
        Assert.Equal(0, await kontrol.KasaHareketleri.CountAsync(h => h.TenantId == k.TenantId));
        Assert.Null((await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
    }

    // ── sıfır · eksi · null → 400; defter boş, bağ yok VE kilit bırakılmış (sonraki geçerli kapora 200) ──
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    [InlineData("null")]
    public async Task Sinir_gecersiz_tutar_400_kilit_birakilir_sonraki_kapora_alinir(string ham)
    {
        decimal? tutar = ham == "null" ? null : decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kur("G" + ham);
        string id;
        using (var db = vt.OlusturSistem())
        {
            id = await SiparisAc(db, k);
            await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, Govde(k, tutar), k.Islem));
        }
        await DefterVeBagBos(k, id);

        // Reddedilen istek kilidi işlem sonunda bıraktı mı? Ayrı bağlamdan geçerli kapora 200 olmalı (beklemeden).
        using var db2 = vt.OlusturSistem();
        var gorev = Siparis(db2).Kapora(id, Govde(k, 250m), k.Islem);
        var bitti = await Task.WhenAny(gorev, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(gorev, bitti);
        await gorev;
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-250m, (await kontrol.Hareketler.AsNoTracking()
            .SingleAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT")).TlTutari);
    }

    // ── kuruş: 100,005 → 100,01 · 100,004 → 100,00; kasa, cari, kart VE geçmiş aynı kuruşu gösterir ──
    [Theory]
    [InlineData("100.005", "100.01")]
    [InlineData("100.004", "100.00")]
    public async Task Sinir_kurus_yuvarlamasi_gecmis_defterle_ayni(string girdi, string beklenen)
    {
        var tutar = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        var hedef = decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kur("K" + girdi);
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
        await Siparis(db).Kapora(id, Govde(k, tutar), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce + hedef, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-hedef, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        Assert.Equal(hedef, d.GetProperty("kaporaTutari").GetDecimal());
        var g = Assert.Single(d.GetProperty("kaporaGecmisi").EnumerateArray().ToList());
        Assert.Equal(hedef, g.GetProperty("tutar").GetDecimal());
        Assert.True(g.GetProperty("guncel").GetBoolean());
    }

    // ── 0,004 TL: >0 kapısından geçer, yuvarlanınca 0 → 400; işlem içinde YARIM bağ kalmaz ──
    [Fact]
    public async Task Sinir_yuvarlaninca_sifir_kapora_400_yarim_bag_yok()
    {
        var k = Kur("Y0004");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, Govde(k, 0.004m), k.Islem));
        await DefterVeBagBos(k, id);
    }

    // ── çok büyük: 999.999.999,99 ya kuruşu kuruşuna yazılır ya da 400 — 500 ve YARIM kayıt YOK ──
    [Fact]
    public async Task Sinir_cok_buyuk_kapora_ya_tam_ya_400_yarim_yok()
    {
        var k = Kur("BUYUK");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
        try
        {
            await Siparis(db).Kapora(id, Govde(k, 999_999_999.99m), k.Islem);
        }
        catch (ApiException ex) when (ex is BadRequestException or ConflictException)
        {
            await DefterVeBagBos(k, id);
            return;
        }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce + 999_999_999.99m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        var g = Assert.Single(J(await Siparis(kontrol).Detay(id, k.Islem)).GetProperty("kaporaGecmisi").EnumerateArray().ToList());
        Assert.Equal(999_999_999.99m, g.GetProperty("tutar").GetDecimal());
    }

    // ── boş/yabancı kimlik: kilit kimseyi kilitlemez, 404 döner, defter boş ──
    [Fact]
    public async Task Sinir_yok_ve_yabanci_siparis_404_defter_bos()
    {
        var a = Kur("A");
        var b = Kur("B");
        string bSiparis;
        using (var dbB = vt.OlusturSistem()) bSiparis = await SiparisAc(dbB, b);
        using var db = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(() => Siparis(db).Kapora("01ZZZZZZZZZZZZZZZZZZZZZZZZ", Govde(a, 100m), a.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => Siparis(db).Kapora(bSiparis, Govde(a, 100m), a.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => (h.TenantId == a.TenantId || h.TenantId == b.TenantId) && h.Tur == "TAHSILAT"));
    }

    // ── eşzamanlı FARKLI tutarlı iki kapora: tek satır; kasa ve cari YALNIZ kazanan tutar kadar ──
    [Fact]
    public async Task Sinir_eszamanli_farkli_tutar_tek_kapora_kasa_kazanan_kadar()
    {
        var k = Kur("FT");
        string id; decimal kasaOnce;
        using (var db0 = vt.OlusturSistem()) { id = await SiparisAc(db0, k); kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId); }
        using var db1 = vt.OlusturSistem();
        using var db2 = vt.OlusturSistem();
        var t1 = Task.Run(() => Siparis(db1).Kapora(id, Govde(k, 1_000m), k.Islem));
        var t2 = Task.Run(() => Siparis(db2).Kapora(id, Govde(k, 2_000.50m), k.Islem));
        var hatalar = 0;
        foreach (var t in new[] { t1, t2 })
            try { await t; } catch (ConflictException) { hatalar++; }
        Assert.Equal(1, hatalar);

        using var kontrol = vt.OlusturSistem();
        var tek = await kontrol.Hareketler.AsNoTracking().SingleAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
        Assert.Contains(tek.TlTutari, new[] { -1_000m, -2_000.50m });
        Assert.Equal(kasaOnce - tek.TlTutari, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(tek.TlTutari, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        Assert.Equal(id, tek.SiparisId);
        Assert.Equal(tek.Id, (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
    }

    // ── BULGU (doğrulayıcı, 09.10, ÖNCEDEN VAR — bu paketin ürünü DEĞİL): aynı kasaya FARKLI siparişlerden
    // eşzamanlı kapora → kasalar.bakiye oku-değiştir-yaz ile güncellendiği için KAYIP GÜNCELLEME.
    // Canlı 5198: 10×100 TL kapora → kasa +100 (beklenen +1000); düz /cari/{id}/tahsilat ucunda da aynı
    // (+100 / beklenen +1000) → TahsilatGirHareket ortak gövdesinin kusuru. Beklenen SABİT: kasa = önce + 10×100.
    // Kasa satırı kilidi (ya da "bakiye = bakiye + @d" atomik güncelleme) gelince Skip kaldırılır.
    [Fact]   // KAPORA-ESZAMAN tur 2: BakiyeKilidi ile kapandı — Skip kaldırıldı
    public async Task Sinir_ayni_kasaya_farkli_siparis_eszamanli_kapora_kasa_toplami_dogru()
    {
        var k = Kur("KASA");
        var idler = new List<string>();
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            for (var i = 0; i < 10; i++) idler.Add(await SiparisAc(db0, k));
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
        }
        var baglamlar = idler.Select(_ => vt.OlusturSistem()).ToList();
        try
        {
            await Task.WhenAll(idler.Select((id, i) => Task.Run(() => Siparis(baglamlar[i]).Kapora(id, Govde(k, 100m), k.Islem))));
        }
        finally { foreach (var b in baglamlar) b.Dispose(); }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(10, await kontrol.KasaHareketleri.CountAsync(x => x.TenantId == k.TenantId && x.KasaId == k.KasaId));
        Assert.Equal(kasaOnce + 1_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    // ── Türkçe giriş (kapora formu FormAlani.SayiCoz) ──
    [Theory]
    [InlineData("45.000", 45000.0)]
    [InlineData("1,5", 1.5)]
    [InlineData("0,005", 0.005)]
    [InlineData("999.999.999,99", 999999999.99)]
    public void Sinir_turkce_giris_cozulur(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.True(c.Tamam);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void Sinir_turkce_giris_belirsiz_1500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── GÜVENLİK sınırı: dönüş yolu — kodlu kontrol karakteri, çift kodlama, ters bölü, şema ──
    [Theory]
    [InlineData("/%2509/kotu.example.com")]      // çift kodlu TAB
    [InlineData("/%25252F/kotu.example.com")]    // 3 kat kodlu "/" (fail-closed)
    [InlineData("/%5C%5Ckotu.example.com")]
    [InlineData("/%00/kotu.example.com")]
    [InlineData("/x%0d%0aSet-Cookie:a=b")]
    [InlineData(" //kotu.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/\u007F/kotu.example.com")]
    public void Guvenlik_donus_kodlu_kacis_reddedilir(string girdi)
        => Assert.Null(Kuyumcu.Core.Domain.GuvenliYol.UygulamaIci(girdi));

    [Theory]
    [InlineData("/siparisler/01M4FXQXN2HSW9AAMGY983KCCD")]
    [InlineData("/emanet")]
    [InlineData("/cariler?sekme=ekstre")]
    public void Guvenlik_donus_mesru_yol_korunur(string girdi)
        => Assert.Equal(girdi, Kuyumcu.Core.Domain.GuvenliYol.UygulamaIci(girdi));

    // ═══════════════════════════════════════════════════════════
    // DOĞRULAYICI TUR 2 (09.10) — kendi sınır testlerim. Beklenenler SABİT, elle hesaplandı.
    // ═══════════════════════════════════════════════════════════

    private async Task<List<Exception?>> Paralel(IReadOnlyList<Func<KuyumcuDbContext, Task>> isler)
    {
        var baglamlar = isler.Select(_ => vt.OlusturSistem()).ToList();
        try
        {
            var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gorevler = isler.Select((is_, i) => Task.Run(async () =>
            {
                await kapi.Task;
                try { await is_(baglamlar[i]); return (Exception?)null; }
                catch (Exception e) { return e; }
            })).ToList();
            kapi.SetResult();
            return [.. await Task.WhenAll(gorevler)];
        }
        finally { foreach (var b in baglamlar) b.Dispose(); }
    }

    // ── SINIF (kontrol → ayrı yazma): MASAK günlük kümülatifi eşzamanlı bölünebilir mi? ──
    // Tek 400.000 TL tahsilat beyansız REDDEDİLİR (KE17 aynı tutarı kapora ile kanıtlıyor). Aynı cariye
    // AYNI ANDA 10 × 40.000 TL (toplam 400.000) beyansız tahsilat: sıralı dünyada en az biri MASAK ile
    // reddedilir ve kabul edilenlerin toplamı 400.000'ün ALTINDA kalır. Hepsi geçerse eşik bölünmüş demektir.
    // BULGU (doğrulayıcı tur 2, 09.10): canlı 5198 + bu test KIRMIZI — paralel 10×40.000 TL beyansız 10/10 kabul
    // (sıralı aynı istekler 4 kabul + 6 MASAK reddi). DogrulaKumulatif işlem DIŞINDA, cari kilidi YOK. Düzeltilince Skip kaldırılır.
    [Fact(Skip = "BULGU KAPORA-ESZAMAN dogrulayici tur 2: MASAK kumulatif esigi eszamanli bolunebiliyor (cari satir kilidi yok)")]
    public async Task Tur2_Sinir_eszamanli_masak_kumulatifi_bolunemez()
    {
        var k = Kur("MASAK");
        var isler = Enumerable.Range(0, 10).Select(_ => (Func<KuyumcuDbContext, Task>)(db =>
            Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 40_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem))).ToList();
        var sonuc = await Paralel(isler);
        var beklenmeyen = sonuc.Where(e => e is not null && e is not BadRequestException).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message)));
        using var kontrol = vt.OlusturSistem();
        var kabul = await kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT").SumAsync(h => -h.TlTutari);
        Assert.True(sonuc.Any(e => e is BadRequestException b && b.Message.StartsWith("MASAK")),
            $"Hiçbir istek MASAK ile reddedilmedi; beyansız kabul edilen toplam {kabul} TL (eşik bölündü).");
        Assert.True(kabul < 400_000m, $"Beyansız kabul edilen toplam {kabul} TL ≥ 400.000 TL");
    }

    // ── Kilitlenme (deadlock) sınırı: AYNI kasa + AYNI cari üzerinde eşzamanlı 5 nakit satış + 5 tahsilat.
    // Satış ve tahsilat kasa/cari satırlarını farklı sırada kilitlerse MySQL 1213 → istek 500 olur.
    // Beklenen SABİT: 10'u da başarılı; kasa = önce + 5×1.000 + 5×100 = önce + 5.500; defter toplamı = özet.
    [Fact]
    public async Task Tur2_Sinir_karisik_eszamanli_satis_ve_tahsilat_kilitlenmez_kasa_tutarli()
    {
        Kiraci k;
        using (var sistem = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(sistem, "PAKETKAPORA-ESZAMAN-TEST dg KARISIK", stokAdet: 20m);
            sistem.SaveChanges();
        }
        decimal kasaOnce;
        using (var db0 = vt.OlusturSistem()) kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
        var isler = new List<Func<KuyumcuDbContext, Task>>();
        for (var i = 0; i < 5; i++)
        {
            isler.Add(db => new Kuyumcu.Api.Modules.Satis.SatisServisi(db, NullLogger<Kuyumcu.Api.Modules.Satis.SatisServisi>.Instance)
                .Satis(new Kuyumcu.Api.Modules.Satis.SatisOlusturDto
                {
                    UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m, OdemeTuru = "NAKIT",
                    KasaId = k.KasaId, KurSnapshotId = k.KurId, CariId = k.CariId,
                }, k.Islem));
            isler.Add(db => Cari(db).TahsilatGirHareket(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 100m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        }
        var sonuc = await Paralel(isler);
        Assert.True(sonuc.All(e => e is null),
            "Başarısız istek: " + string.Join(" | ", sonuc.Where(e => e is not null).Select(e => e!.GetType().Name + ": " + e.Message)));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce + 5_500m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(5_500m, await kontrol.KasaHareketleri
            .Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId).SumAsync(h => h.Tutar));
    }

    // ── Kapora geri verme (storno) ile yeni kapora AYNI ANDA: sonunda en çok BİR canlı kapora; kasa = canlı kapora. ──
    [Fact]
    public async Task Tur2_Sinir_storno_ile_eszamanli_yeni_kapora_tek_canli_kapora()
    {
        var k = Kur("STYK");
        string id, ilk; decimal kasaOnce;
        using (var db0 = vt.OlusturSistem())
        {
            id = await SiparisAc(db0, k);
            kasaOnce = TestKurulum.KasaBakiye(db0, k.KasaId);
            ilk = J(await Siparis(db0).Kapora(id, Govde(k, 1_000m), k.Islem)).GetProperty("kaporaHareketId").GetString()!;
        }
        var isler = new List<Func<KuyumcuDbContext, Task>>
        {
            db => new Kuyumcu.Api.Modules.Satis.StornoServisi(db, new DenetimServisi(db, new HttpContextAccessor()),
                    NullLogger<Kuyumcu.Api.Modules.Satis.StornoServisi>.Instance)
                .Storno(ilk, new Kuyumcu.Api.Modules.Satis.StornoDto
                {
                    Gerekce = "Yanlış tutar alındı, kapora müşteriye geri verildi", KurSnapshotId = k.KurId, KasaId = k.KasaId,
                }, k.Islem),
        };
        for (var i = 0; i < 5; i++) isler.Add(db => Siparis(db).Kapora(id, Govde(k, 1_500m), k.Islem));
        var sonuc = await Paralel(isler);
        var beklenmeyen = sonuc.Where(e => e is not null && e is not ConflictException).ToList();
        Assert.True(beklenmeyen.Count == 0,
            "Beklenmeyen istisna: " + string.Join(" | ", beklenmeyen.Select(e => e!.GetType().Name + ": " + e.Message)));

        using var kontrol = vt.OlusturSistem();
        var tahsilatlar = await kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT" && h.StornoKaynakId == null).ToListAsync();
        var stornolar = await kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.StornoKaynakId != null).Select(h => h.StornoKaynakId!).ToListAsync();
        var canli = tahsilatlar.Where(h => !stornolar.Contains(h.Id)).ToList();
        Assert.True(canli.Count <= 1, $"Canlı kapora sayısı {canli.Count} (en çok 1 olmalı)");
        Assert.Equal(kasaOnce + canli.Sum(h => -h.TlTutari), TestKurulum.KasaBakiye(kontrol, k.KasaId));
        var bag = (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId;
        if (canli.Count == 1) Assert.Equal(canli[0].Id, bag);
    }

    // ── Açık yönlendirme: tarayıcının TAB/LF/CR sildiği ve "\" → "/" çevirdiği her kodlama kökene düşer ──
    [Theory]
    [InlineData("/%09%2F/kotu.example.com")]
    [InlineData("/%2F%09/kotu.example.com")]
    [InlineData("/\t\t/kotu.example.com")]
    [InlineData("/%2f%2fkotu.example.com")]
    [InlineData("/%25%32%46/kotu.example.com")]  // %252F'in harf harf kodu → 2 katta "/"
    [InlineData("/x\u2028/kotu")]                 // satır ayırıcı yol içinde: kontrol karakteri değil → korunur mu? (bkz. aşağı)
    public void Tur2_Guvenlik_donus_varyantlari(string girdi)
    {
        var sonuc = Kuyumcu.Core.Domain.GuvenliYol.UygulamaIci(girdi);
        if (girdi.Contains('\u2028'))
        {
            // U+2028 tarayıcı URL ayrıştırıcısında silinmez, yüzde-kodlanır → aynı köken; korunması zararsız.
            Assert.True(sonuc is null || sonuc == girdi);
            return;
        }
        Assert.Null(sonuc);
        Assert.Equal("/", Kuyumcu.Core.Domain.GuvenliYol.Donus(girdi, "//kotu.example.com"));
    }
}
