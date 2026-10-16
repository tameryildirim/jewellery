using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MCari = Kuyumcu.Api.Modules.Cari;
using MVezne = Kuyumcu.Api.Modules.Vezne;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE MODELİ (paket 19 Faz A) — tezgâhtar satar, vezne tahsil eder.
//
// Ölçülen: durum makinesi (R3/R4/R5), dört göz (R2), şube kapsamı (R6), biçim
// (R7), tahsil anı kuru (R8), stok reddinde fişin VEZNEDE kalması (R9), tek
// yazma yolu (R10) ve hareketin sahibinin TAHSİL EDEN olması (R11).
//
// ⚠ KÜLTÜR: tutarlar decimal literal ile yazılır; hiçbir yerde metin ayrıştırma
//   yoktur, dolayısıyla tr-TR/invariant ayrımı bu dosyada karar değiştirmez.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class VezneTestleri(TestVeritabani vt)
{
    private static MVezne.VezneServisi Vezne(KuyumcuDbContext db) =>
        new(db, NullLogger<MVezne.VezneServisi>.Instance,
            new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance),
            new MCari.CariServisi(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db)));

    private static MVezne.VezneFisOlusturDto Fis(Kiraci k, decimal birimFiyat = 5_000m,
        decimal adet = 1m, decimal iscilik = 0m) => new()
    {
        CariId = k.CariId,
        KurSnapshotId = k.KurId,
        Aciklama = "vezne testi",
        Kalemler = [new MVezne.VezneKalemDto
        {
            UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birimFiyat, IscilikTl = iscilik,
        }],
    };

    private static MVezne.VezneTahsilDto Tahsil(Kiraci k, decimal tutar, string arac = "NAKIT") => new()
    {
        KurSnapshotId = k.KurId,
        Bacaklar = [new MSatis.OdemeBacagiDto
        {
            Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId,
        }],
    };

    private static string Kimlik(object cevap)
    {
        var oge = System.Text.Json.JsonSerializer.SerializeToElement(cevap);
        return oge.GetProperty("id").GetString()!;
    }

    private static string Alan(object cevap, string ad)
    {
        var oge = System.Text.Json.JsonSerializer.SerializeToElement(cevap);
        return oge.GetProperty(ad).GetString() ?? "";
    }

    private static decimal Sayi(object cevap, string ad)
    {
        var oge = System.Text.Json.JsonSerializer.SerializeToElement(cevap);
        return oge.GetProperty(ad).GetDecimal();
    }

    /// <summary>Kiracıya ikinci bir kullanıcı ekler (dört göz senaryoları).</summary>
    private string IkinciKullanici(Kiraci k, string rol = "MUDUR")
    {
        using var db = vt.OlusturSistem();
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Kullanicilar.Add(new Kullanici
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Vezne", Soyad = "Görevlisi",
            Email = $"{id}@test.local", SifreHash = "x", Rol = rol, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private void VezneAyariAc(Kiraci k, bool zorunlu = true)
    {
        using var db = vt.OlusturSistem();
        db.KiraciAyarlari.Add(new KiraciAyari
        {
            Id = Kuyumcu.Api.Common.Kimlik.Ulid(),
            TenantId = k.TenantId,
            Anahtar = KiraciAyarAnahtari.VezneZorunlu,
            Deger = zorunlu ? "true" : "false",
            Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private List<Hareket> DefterSatirlari(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return [.. kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId).OrderBy(h => h.Id)];
    }

    private VezneFisi FisOku(string id)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.VezneFisleri.AsNoTracking().Single(f => f.Id == id);
    }

    // ── Mutlu yol ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Fis_ac_veznele_tahsil_et_defter_kasa_ve_fis_bagi_kurulur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-akis");
        using var db = vt.Olustur(k);
        var oncekiKasa = TestKurulum.KasaBakiye(vt.OlusturSistem(), k.KasaId);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 5_000m), k.Islem));
        var veznelendi = await Vezne(db).Veznele(fisId, k.Islem);
        Assert.Equal(VezneDurum.Veznede, Alan(veznelendi, "durum"));
        Assert.Equal(5_000m, Sayi(veznelendi, "toplamTl"));

        var sonuc = await Vezne(db).Tahsil(fisId, Tahsil(k, 5_000m), k.Islem);
        var hareketFisId = Alan(sonuc, "hareketFisId");
        Assert.False(string.IsNullOrEmpty(hareketFisId));

        var fis = FisOku(fisId);
        Assert.Equal(VezneDurum.Tahsil, fis.Durum);
        Assert.Equal(hareketFisId, fis.HareketFisId);
        Assert.Equal(k.KullaniciId, fis.TahsilEdenKullaniciId);

        var defter = DefterSatirlari(k);
        Assert.Single(defter);
        Assert.Equal(hareketFisId, defter[0].FisId);
        Assert.Equal(5_000m, defter[0].TlTutari);
        Assert.Equal(oncekiKasa + 5_000m, TestKurulum.KasaBakiye(vt.OlusturSistem(), k.KasaId));
    }

    [Fact]
    public async Task Toplam_VEZNELEME_aninda_damgalanir_tahsilde_degil()
    {
        // M-V10: kuyrukta görünen tutar ile tahsil edilen tutar ayrışamaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-damga");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 2_500m, adet: 2m, iscilik: 100m), k.Islem));
        Assert.Equal(0m, FisOku(fisId).ToplamTl);          // ACIK fişte damga YOK

        await Vezne(db).Veznele(fisId, k.Islem);
        Assert.Equal(5_100m, FisOku(fisId).ToplamTl);      // 2 × 2.500 + 100
    }

    [Fact]
    public async Task Defterdeki_hareketin_sahibi_TAHSIL_EDENDIR_hazirlayan_degil()
    {
        // R11: kasa farkı tahsil edene, fiyat/indirim hazırlayana sorulur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-sahiplik");
        using var db = vt.Olustur(k);
        var vezneci = IkinciKullanici(k);
        var vezneciIslem = new AktifKullanici(vezneci, k.TenantId, k.SubeId, "MUDUR");

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 3_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Vezne(db).Tahsil(fisId, Tahsil(k, 3_000m), vezneciIslem);

        var defter = DefterSatirlari(k);
        Assert.Single(defter);
        Assert.Equal(vezneci, defter[0].KullaniciId);
        Assert.Equal(k.KullaniciId, FisOku(fisId).HazirlayanKullaniciId);
    }

    [Fact]
    public async Task Deftere_TAHSIL_aninda_gonderilen_kur_yazilir_fisinki_degil()
    {
        // R8 / M-V8: sabah açılan fiş akşam eski kurla kapanmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-kur");
        using var db = vt.Olustur(k);

        var yeniKurId = Kuyumcu.Api.Common.Kimlik.Ulid();
        using (var sistem = vt.OlusturSistem())
        {
            sistem.KurSnapshots.Add(new KurSnapshot
            {
                Id = yeniKurId, TenantId = k.TenantId, AltinHasFiyati = 7_100m,
                GramAltinFiyati = 7_150m, Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow,
            });
            sistem.SaveChanges();
        }

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 4_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var dto = Tahsil(k, 4_000m);
        dto.KurSnapshotId = yeniKurId;                      // TAHSİL anının kuru
        await Vezne(db).Tahsil(fisId, dto, k.Islem);

        var defter = DefterSatirlari(k);
        Assert.Single(defter);
        Assert.Equal(yeniKurId, defter[0].KurSnapshotId);
        Assert.Equal(k.KurId, FisOku(fisId).KurSnapshotId);   // fişinki REFERANS olarak durur
    }

    // ── Durum makinesi ──────────────────────────────────────────────────────

    [Fact]
    public async Task ACIK_fis_tahsil_EDILEMEZ()
    {
        // R3 / M-V3
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-acik-tahsil");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k), k.Islem));

        await Assert.ThrowsAsync<ConflictException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 5_000m), k.Islem));
        Assert.Empty(DefterSatirlari(k));
    }

    [Fact]
    public async Task TAHSIL_edilmis_fis_IKINCI_kez_tahsil_edilemez_ve_defterde_yeni_satir_olmaz()
    {
        // R3 / M-V3: uygulama kapısı ölçülür (indeks katmanı ayrı testte).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-cift-tahsil");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Vezne(db).Tahsil(fisId, Tahsil(k, 1_000m), k.Islem);
        var ilkSayim = DefterSatirlari(k).Count;

        await Assert.ThrowsAsync<ConflictException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 1_000m), k.Islem));

        Assert.Equal(ilkSayim, DefterSatirlari(k).Count);
    }

    [Fact]
    public async Task Ayni_defter_fisi_IKINCI_vezne_fisine_baglanamaz_VERITABANI_reddeder()
    {
        // M-V4 — ÖLÇÜLEN KATMAN: (tenant_id, hareket_fis_id) BENZERSİZ indeksi.
        // Uygulama kapısı bilinçli olarak ATLANIR; aksi hâlde indeks kaldırılsa
        // bile test yeşil kalır ve yanlış güven verirdi (Aşkın koşulu).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-indeks");
        using var db = vt.Olustur(k);

        var birinci = Kimlik(await Vezne(db).Olustur(Fis(k, 1_500m), k.Islem));
        await Vezne(db).Veznele(birinci, k.Islem);
        await Vezne(db).Tahsil(birinci, Tahsil(k, 1_500m), k.Islem);
        var defterFisi = FisOku(birinci).HareketFisId!;

        var ikinci = Kimlik(await Vezne(db).Olustur(Fis(k, 1_500m), k.Islem));

        using var sistem = vt.OlusturSistem();
        var kayit = sistem.VezneFisleri.Single(f => f.Id == ikinci);
        kayit.HareketFisId = defterFisi;                    // AYNI defter fişi
        Assert.Throws<DbUpdateException>(() => sistem.SaveChanges());
    }

    [Fact]
    public async Task VEZNEDE_fisin_kalemleri_DEGISTIRILEMEZ()
    {
        // R4 / M-V5
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-fiyat-kilidi");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 9_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var govde = System.Text.Json.JsonDocument.Parse("""{"aciklama":"fiyat kırdım"}""").RootElement;
        await Assert.ThrowsAsync<ConflictException>(() => Vezne(db).Guncelle(fisId, govde, k.Islem));
    }

    [Fact]
    public async Task ACIK_fiste_kalemler_TOPTAN_degistirilir_ve_aciklama_PATCH_sozlesmesine_uyar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-patch");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m), k.Islem));

        // Gövdede olmayan alan KORUNUR, null gönderilen SİLİNİR.
        var govde1 = System.Text.Json.JsonDocument.Parse(
            $$"""{"kalemler":[{"urunId":"{{k.UrunId}}","adet":3,"birimFiyatTl":2000,"iscilikTl":50}]}""")
            .RootElement;
        var sonuc = await Vezne(db).Guncelle(fisId, govde1, k.Islem);
        Assert.Equal("vezne testi", Alan(sonuc, "aciklama"));      // gövdede yok → KORUNDU
        Assert.Equal(6_050m, Sayi(sonuc, "toplamTl"));             // 3 × 2.000 + 50

        var govde2 = System.Text.Json.JsonDocument.Parse("""{"aciklama":null}""").RootElement;
        var sonuc2 = System.Text.Json.JsonSerializer.SerializeToElement(
            await Vezne(db).Guncelle(fisId, govde2, k.Islem));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, sonuc2.GetProperty("aciklama").ValueKind);
    }

    [Fact]
    public async Task Baska_tezgahtarin_fisi_PATCH_edilemez_ama_MUDUR_edebilir()
    {
        // Mehmet koşulu: sahiplik kararı SUNUCUDA.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-sahiplik-patch");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k), k.Islem));

        var yabanci = IkinciKullanici(k, "TEZGAHTAR");
        var yabanciIslem = new AktifKullanici(yabanci, k.TenantId, k.SubeId, "TEZGAHTAR");
        var govde = System.Text.Json.JsonDocument.Parse("""{"aciklama":"benim değil"}""").RootElement;

        await Assert.ThrowsAsync<ForbiddenException>(() => Vezne(db).Guncelle(fisId, govde, yabanciIslem));

        var mudur = IkinciKullanici(k);
        var mudurIslem = new AktifKullanici(mudur, k.TenantId, k.SubeId, "MUDUR");
        var sonuc = await Vezne(db).Guncelle(fisId, govde, mudurIslem);
        Assert.Equal("benim değil", Alan(sonuc, "aciklama"));
    }

    // ── Dört göz ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Vezne_ayrimi_ACIKKEN_hazirlayan_kendi_fisini_tahsil_EDEMEZ()
    {
        // R2 / M-V2
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-dortgoz");
        VezneAyariAc(k);
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 2_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 2_000m), k.Islem));
        Assert.Contains("tahsil edemezsiniz", ex.Message);
        Assert.Empty(DefterSatirlari(k));

        // Fiş VEZNEDE kalır: başka kullanıcı tahsil edebilmeli.
        Assert.Equal(VezneDurum.Veznede, FisOku(fisId).Durum);
        var vezneci = IkinciKullanici(k);
        await Vezne(db).Tahsil(fisId, Tahsil(k, 2_000m),
            new AktifKullanici(vezneci, k.TenantId, k.SubeId, "MUDUR"));
        Assert.Single(DefterSatirlari(k));
    }

    [Fact]
    public async Task Vezne_ayrimi_KAPALIYKEN_ayni_kullanici_tahsil_edebilir()
    {
        // Tek kişilik dükkân kilitlenmez (varsayılan KAPALI).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-tek-kisi");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 2_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Vezne(db).Tahsil(fisId, Tahsil(k, 2_000m), k.Islem);

        Assert.Single(DefterSatirlari(k));
    }

    // ── Biçim ve iptal ──────────────────────────────────────────────────────

    [Fact]
    public async Task Bos_fis_acilamaz_ve_sifir_fiyatli_kalem_REDDEDILIR()
    {
        // R7: sıfır fiyatlı satış hediye DEĞİL hatadır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-bicim");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() => Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
        {
            KurSnapshotId = k.KurId, Kalemler = [],
        }, k.Islem));

        await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Olustur(Fis(k, birimFiyat: 0m), k.Islem));
    }

    [Fact]
    public async Task Gerekcesiz_iptal_reddedilir_TAHSIL_fis_iptal_edilemez_kayit_silinmez()
    {
        // R5
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-iptal");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_200m), k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Iptal(fisId, new MVezne.VezneIptalDto { Gerekce = "kısa" }, k.Islem));

        await Vezne(db).Iptal(fisId, new MVezne.VezneIptalDto
        { Gerekce = "müşteri vazgeçti, mal vitrine döndü" }, k.Islem);
        var iptalli = FisOku(fisId);
        Assert.Equal(VezneDurum.Iptal, iptalli.Durum);        // SATIR DURUYOR
        Assert.Contains("vazgeçti", iptalli.IptalGerekce!);

        var ikinci = Kimlik(await Vezne(db).Olustur(Fis(k, 1_200m), k.Islem));
        await Vezne(db).Veznele(ikinci, k.Islem);
        await Vezne(db).Tahsil(ikinci, Tahsil(k, 1_200m), k.Islem);
        await Assert.ThrowsAsync<ConflictException>(() => Vezne(db).Iptal(ikinci,
            new MVezne.VezneIptalDto { Gerekce = "yanlış tahsil ettim, geri al" }, k.Islem));
    }

    [Fact]
    public async Task Bos_fis_vezneye_gonderilemez()
    {
        // R7: kalemsiz fiş veznelenemez — kalemler PATCH ile silinemediği için
        // doğrudan veritabanından boşaltılıp kapı ölçülür.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-bos-veznele");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k), k.Islem));

        using (var sistem = vt.OlusturSistem())
        {
            sistem.VezneFisKalemleri.RemoveRange(sistem.VezneFisKalemleri.Where(x => x.FisId == fisId));
            sistem.SaveChanges();
        }

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Vezne(db).Veznele(fisId, k.Islem));
        Assert.Contains("Boş fiş", ex.Message);
    }

    // ── Kapsam (şube / kiracı) ──────────────────────────────────────────────

    [Fact]
    public async Task Baska_subenin_fisi_kuyrukta_GORUNMEZ_ve_tahsil_edilemez()
    {
        // R6 / M-V7
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-sube");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 800m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var ikinciSube = TestKurulum.SubeEkle(vt.OlusturSistem(), k.TenantId);
        var yabanciIslem = new AktifKullanici(k.KullaniciId, k.TenantId, ikinciSube, "SAHIP");

        var kuyruk = System.Text.Json.JsonSerializer.SerializeToElement(
            await Vezne(db).Kuyruk(null, null, yabanciIslem));
        Assert.Equal(0, kuyruk.GetProperty("toplam").GetInt32());

        await Assert.ThrowsAsync<NotFoundException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 800m), yabanciIslem));
    }

    [Fact]
    public async Task Baska_kiracinin_fis_kimligiyle_tahsil_404_verir()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-kiraci-A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-kiraci-B");
        using var dbA = vt.Olustur(a);
        var fisId = Kimlik(await Vezne(dbA).Olustur(Fis(a, 700m), a.Islem));
        await Vezne(dbA).Veznele(fisId, a.Islem);

        using var dbB = vt.Olustur(b);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Vezne(dbB).Tahsil(fisId, Tahsil(b, 700m), b.Islem));
    }

    // ── Kapı paritesi: defter kapıları vezne yolunda da çalışır ─────────────

    [Fact]
    public async Task Kapanmis_kasa_gunune_tahsil_GUN_KILIDINE_takilir_ve_fis_VEZNEDE_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-gun-kilidi");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 900m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        using (var sistem = vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            sistem.GunKapanislari.Add(new GunKapanisi
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId,
                SubeId = k.SubeId, YerelGun = Zaman.YerelBugun(),
                SayilanNakit = TestKurulum.KasaAcilis, DefterBakiye = TestKurulum.KasaAcilis,
                Fark = 0m, KapatanKullaniciId = k.KullaniciId, KapanisZamani = now, Olusturuldu = now,
            });
            sistem.SaveChanges();
        }

        await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 900m), k.Islem));

        Assert.Equal(VezneDurum.Veznede, FisOku(fisId).Durum);   // R9: fiş geri alınır
        Assert.Empty(DefterSatirlari(k));
    }

    [Fact]
    public async Task Tevsik_haddini_asan_NAKIT_tahsil_reddedilir_HAVALE_gecer()
    {
        // 459 s. VUK: 30.000 TL üstü nakit → tevsik. Kapı SatisFisi içindedir;
        // vezne yolu onu ATLAMAZ.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-tevsik", stokAdet: 100m);
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 30_001m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 30_001m), k.Islem));
        Assert.Equal(VezneDurum.Veznede, FisOku(fisId).Durum);

        await Vezne(db).Tahsil(fisId, Tahsil(k, 30_001m, "HAVALE"), k.Islem);
        Assert.Single(DefterSatirlari(k));
    }

    [Fact]
    public async Task Stok_yetmezse_tahsil_reddedilir_ve_fis_VEZNEDE_kalir()
    {
        // R9: sessiz kısmi satış YOKTUR.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-stok", stokAdet: 1m);
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m, adet: 5m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 5_000m), k.Islem));
        Assert.Contains("stok", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(VezneDurum.Veznede, FisOku(fisId).Durum);
        Assert.Empty(DefterSatirlari(k));
    }

    [Fact]
    public async Task Defter_yazildiktan_sonra_fis_bagi_kaybolsa_bile_IKINCI_tahsil_defterde_satir_URETMEZ()
    {
        // ATOMLUK RİSKİ (Bertan/Aşkın koşulu): SatisFisi commit ettikten sonra
        // süreç düşerse fişin hareket_fis_id'si yazılamaz. Tasarım bunu
        // SIRALAMAYLA karşılar: durum defterden ÖNCE TAHSIL'e çevrilir, bu
        // yüzden ikinci deneme 409 alır ve defterde tek fiş kalır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-atomluk");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_100m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Vezne(db).Tahsil(fisId, Tahsil(k, 1_100m), k.Islem);

        using (var sistem = vt.OlusturSistem())          // 3. adımın kaybı taklit edilir
        {
            var kayit = sistem.VezneFisleri.Single(f => f.Id == fisId);
            kayit.HareketFisId = null;
            sistem.SaveChanges();
        }

        await Assert.ThrowsAsync<ConflictException>(
            () => Vezne(db).Tahsil(fisId, Tahsil(k, 1_100m), k.Islem));

        Assert.Single(DefterSatirlari(k));
        Assert.True(FisOku(fisId).HareketFisId is null);   // kuyrukta "mutabakat gerekli"
    }

    [Fact]
    public async Task Eskimis_damga_ile_tahsil_409_verir()
    {
        // İYİMSER KİLİT: ekran gördüğü damgayı gönderir; fiş o damgadan sonra
        // değiştiyse istek reddedilir (iki vezne aynı fişe basamaz).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-kilit");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 600m), k.Islem));
        var eskiDamga = FisOku(fisId).Guncellendi;
        await Vezne(db).Veznele(fisId, k.Islem);          // damga TAZELENDİ

        var dto = Tahsil(k, 600m);
        dto.Guncellendi = eskiDamga;
        await Assert.ThrowsAsync<ConflictException>(() => Vezne(db).Tahsil(fisId, dto, k.Islem));
        Assert.Empty(DefterSatirlari(k));
    }

    // ── Kuyruk sözleşmesi ───────────────────────────────────────────────────

    [Fact]
    public async Task Kuyruk_kisisel_veri_dondurmez_ve_limiti_bildirir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P19-kuyruk");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 400m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var kuyruk = System.Text.Json.JsonSerializer.SerializeToElement(
            await Vezne(db).Kuyruk(null, 5, k.Islem));

        Assert.Equal(VezneDurum.Veznede, kuyruk.GetProperty("durum").GetString());
        Assert.Equal(VezneFisi.AzamiLimit, kuyruk.GetProperty("azamiLimit").GetInt32());
        var satir = kuyruk.GetProperty("fisler")[0];
        Assert.Equal(400m, satir.GetProperty("toplamTl").GetDecimal());
        Assert.False(satir.TryGetProperty("telefon", out _));
        Assert.False(satir.TryGetProperty("vergiNo", out _));
    }
    // ── P-46: kuyrukta gerçek fiş toplamı (kuyruk = detay) ───────────────────
    //
    // QA tur 8 ölçümü: kalemli + ACIK fiş kuyrukta damgasız f.ToplamTl'ye (=0)
    // düşüyordu, detay aynı fişte kalem toplamını hesaplıyordu. Kural: iki uç
    // AYNI yardımcıyı çağırır; damga (M-V10) yalnız vezneleme anında yazılır.

    private static System.Text.Json.JsonElement KuyrukSatiri(object kuyruk, string fisId)
    {
        var oge = System.Text.Json.JsonSerializer.SerializeToElement(kuyruk);
        return oge.GetProperty("fisler").EnumerateArray()
            .Single(f => f.GetProperty("id").GetString() == fisId);
    }

    [Fact]
    public async Task P46_01_Kuyruk_ve_detay_ayni_fis_icin_ayni_toplami_dondurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P46-esit");
        using var db = vt.Olustur(k);
        // 2 × 1.000 + 150 işçilik = 2.150
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m, adet: 2m, iscilik: 150m), k.Islem));

        var satir = KuyrukSatiri(await Vezne(db).Kuyruk(VezneDurum.Acik, null, k.Islem), fisId);
        var detay = await Vezne(db).Detay(fisId, k.Islem);

        Assert.Equal(2_150m, satir.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(Sayi(detay, "toplamTl"), satir.GetProperty("toplamTl").GetDecimal());
        // Damga henüz YOK: ayrım yanıtta korunur, ekran "tahmini" der.
        Assert.Equal(0m, satir.GetProperty("damgaliToplamTl").GetDecimal());
        Assert.True(satir.GetProperty("toplamTahmini").GetBoolean());
        Assert.Equal(0m, FisOku(fisId).ToplamTl);            // M-V10: ACIK fişe damga YAZILMADI
    }

    [Fact]
    public async Task P46_02_Kalem_duzenlenince_kuyruk_toplami_da_degisir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P46-duzen");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m, adet: 2m, iscilik: 150m), k.Islem));
        Assert.Equal(2_150m, KuyrukSatiri(await Vezne(db).Kuyruk(VezneDurum.Acik, null, k.Islem), fisId)
            .GetProperty("toplamTl").GetDecimal());

        // 3 × 1.000 + 200 = 3.200
        var govde = System.Text.Json.JsonDocument.Parse(
            $$"""{"kalemler":[{"urunId":"{{k.UrunId}}","adet":3,"birimFiyatTl":1000,"iscilikTl":200}]}""")
            .RootElement;
        await Vezne(db).Guncelle(fisId, govde, k.Islem);

        var satir = KuyrukSatiri(await Vezne(db).Kuyruk(VezneDurum.Acik, null, k.Islem), fisId);
        Assert.Equal(3_200m, satir.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(Sayi(await Vezne(db).Detay(fisId, k.Islem), "toplamTl"),
            satir.GetProperty("toplamTl").GetDecimal());
    }

    [Fact]
    public async Task P46_03_Veznelenmis_fiste_damgali_tutar_yeniden_hesaplanmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P46-damga");
        using var db = vt.Olustur(k);
        var fisId = Kimlik(await Vezne(db).Olustur(Fis(k, 1_000m, adet: 2m, iscilik: 150m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        Assert.Equal(2_150m, FisOku(fisId).ToplamTl);

        // Damgadan SONRA kalem değişse bile (ekrandan yolu yok; doğrudan
        // veritabanında) gösterilen tutar damgadır — yeniden HESAPLANMAZ.
        using (var sistem = vt.OlusturSistem())
        {
            var kalem = sistem.VezneFisKalemleri.Single(x => x.FisId == fisId);
            kalem.Adet = 5m;
            sistem.SaveChanges();
        }

        using var taze = vt.Olustur(k);
        var satir = KuyrukSatiri(await Vezne(taze).Kuyruk(VezneDurum.Veznede, null, k.Islem), fisId);
        Assert.Equal(2_150m, satir.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(2_150m, satir.GetProperty("damgaliToplamTl").GetDecimal());
        Assert.False(satir.GetProperty("toplamTahmini").GetBoolean());
        Assert.Equal(2_150m, Sayi(await Vezne(taze).Detay(fisId, k.Islem), "toplamTl"));
    }

    [Fact]
    public async Task P46_04_Kuyruk_sorgusu_N_plus_1_acmaz()
    {
        // ÖLÇÜM: EF komut yakalayıcısı kuyruk çağrısında çalışan SQL sayısını
        // sayar. 1 ACIK fişte ve 6 ACIK fişte sayı AYNI olmalı; fiş başına
        // kalem sorgusu açılırsa fark 5 olur ve test KIRMIZI döner.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P46-n1");
        using (var db = vt.Olustur(k))
            await Vezne(db).Olustur(Fis(k, 1_000m, adet: 2m, iscilik: 150m), k.Islem);

        int Say(List<string> komutlar) => komutlar.Count;

        var y1 = new P46SqlSayaci();
        using (var db = PosSqlOrtami.Kur(vt, k.TenantId, y1))
        {
            var kuyruk = System.Text.Json.JsonSerializer.SerializeToElement(
                await Vezne(db).Kuyruk(VezneDurum.Acik, null, k.Islem));
            Assert.Equal(1, kuyruk.GetProperty("fisler").GetArrayLength());
        }

        using (var db = vt.Olustur(k))
            for (var i = 0; i < 5; i++)
                await Vezne(db).Olustur(Fis(k, 1_000m + i, adet: 1m), k.Islem);

        var y6 = new P46SqlSayaci();
        using (var db = PosSqlOrtami.Kur(vt, k.TenantId, y6))
        {
            var kuyruk = System.Text.Json.JsonSerializer.SerializeToElement(
                await Vezne(db).Kuyruk(VezneDurum.Acik, null, k.Islem));
            Assert.Equal(6, kuyruk.GetProperty("fisler").GetArrayLength());
            Assert.All(kuyruk.GetProperty("fisler").EnumerateArray(),
                f => Assert.True(f.GetProperty("toplamTl").GetDecimal() > 0m));
        }

        Assert.Equal(Say(y1.Komutlar), Say(y6.Komutlar));
        // Kalem satırları tek sorguda, IN listesiyle çekilir.
        Assert.Single(y6.Komutlar, s => s.Contains("vezne_fis_kalemleri", StringComparison.OrdinalIgnoreCase)
            && s.Contains(" IN ", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class P46SqlSayaci : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public List<string> Komutlar { get; } = [];

        public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>
            ReaderExecuting(System.Data.Common.DbCommand command,
                Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Komutlar.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>>
            ReaderExecutingAsync(System.Data.Common.DbCommand command,
                Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE KAPISI — SAF testler (veritabanı yok).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class VezneKapisiTestleri
{
    [Fact]
    public void Dort_goz_yalniz_ayar_acikken_uygulanir()
    {
        MVezne.VezneKapisi.DortGozDogrula("u1", "u1", vezneZorunlu: false);   // serbest
        Assert.Throws<BadRequestException>(
            () => MVezne.VezneKapisi.DortGozDogrula("u1", "u1", vezneZorunlu: true));
        MVezne.VezneKapisi.DortGozDogrula("u1", "u2", vezneZorunlu: true);    // farklı kişi
    }

    [Theory]
    [InlineData(VezneDurum.Acik)]
    [InlineData(VezneDurum.Tahsil)]
    [InlineData(VezneDurum.Iptal)]
    public void Tahsil_yalniz_VEZNEDE_durumundan_yapilir(string durum)
        => Assert.Throws<ConflictException>(() => MVezne.VezneKapisi.TahsilDurumuDogrula(durum));

    [Fact]
    public void VEZNEDE_fis_tahsil_kapisindan_gecer()
        => MVezne.VezneKapisi.TahsilDurumuDogrula(VezneDurum.Veznede);

    [Fact]
    public void Duzenleme_yalniz_ACIK_fiste_serbesttir()
    {
        MVezne.VezneKapisi.DuzenlemeDurumuDogrula(VezneDurum.Acik);
        Assert.Throws<ConflictException>(
            () => MVezne.VezneKapisi.DuzenlemeDurumuDogrula(VezneDurum.Veznede));
    }

    [Fact]
    public void Sahiplik_hazirlayana_ve_MUDUR_ustune_acilir()
    {
        MVezne.VezneKapisi.SahiplikDogrula("u1", new AktifKullanici("u1", "t", "s", "TEZGAHTAR"));
        MVezne.VezneKapisi.SahiplikDogrula("u1", new AktifKullanici("u9", "t", "s", "MUDUR"));
        Assert.Throws<ForbiddenException>(() =>
            MVezne.VezneKapisi.SahiplikDogrula("u1", new AktifKullanici("u9", "t", "s", "TEZGAHTAR")));
    }
}
