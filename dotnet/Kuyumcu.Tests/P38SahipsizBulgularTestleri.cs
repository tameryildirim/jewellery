using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Arama;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MKur = Kuyumcu.Api.Modules.Kur;
using MPano = Kuyumcu.Api.Modules.Pano;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-38 — TUR 7'NİN SAHİPSİZ KALAN 6 BULGUSU + B-39 ÇELİŞKİSİ
//
// Kaynak: TRIYAJ-TUR7-2026-09-21.md § 6 ve § 6.2. Her kural için hatayı
// YENİDEN ÜRETEN test:
//   B-18/1  (#112) pano marjı fiyatı sıfıra indiremez — sunucu 400 der
//   B-18/2  (#112) ₺0,00 vitrine BASILMAZ; satır MARJ_GECERSIZ ile atlanır
//   B-70    (#163) emanet girişindeki AÇIKLAMA deftere yazılır
//   B-66/1  (#67)  pasif ürünle satış sunucuda reddedilir
//   B-66/2  (#107) pasif ürün satış listesinde GÖRÜNMEZ (aktif süzgeci)
//   B-25    (#159) fiş seçicisi sorgusuz da SON FİŞLERİ listeler
//   B-18/3  (#229) cari eşleşmesi olmayan ekstre satırı sessizce onaylanmaz
//   B-39    (#48)  pasife alma liste satırından BULUNABİLİR (ekran testi ayrı)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P38PanoSafTestleri
{
    private static MKur.Parite P(string kod, decimal? alis, decimal? satis) =>
        new(kod, MKur.PariteKodlari.Ad(kod), alis, satis, "GRAM");

    private static MPano.PanoKalemAyari A(string kod, decimal alisMarj = 0m, decimal satisMarj = 0m,
        decimal adim = 0m) =>
        new() { PariteKodu = kod, AlisMarjTl = alisMarj, SatisMarjTl = satisMarj, YuvarlamaAdimi = adim };

    // ── B-18/2 — ₺0,00 vitrine çıkmaz ───────────────────────────────────────

    [Fact]
    public void Marj_fiyati_sifira_indirirse_satir_VITRINE_CIKMAZ()
    {
        // QA turu 7 #112'nin birebir senaryosu: −999.999 TL marj kabul edilmiş,
        // panoda "gram altın alış ₺0,00" görünmüştü.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 5_000m, 5_100m)],
            [A(MKur.PariteKodlari.Gram, alisMarj: -999_999m)]);

        Assert.Empty(sonuc.Satirlar);
        var atlanan = Assert.Single(sonuc.Atlananlar);
        Assert.Equal(MPano.AtlamaSebebi.MarjGecersiz, atlanan.Sebep);
    }

    [Fact]
    public void Asagi_yuvarlama_fiyati_sifira_indirirse_satir_ATLANIR()
    {
        // 3 ₺ fiyat, 10 ₺ adım, alış AŞAĞI yuvarlanır → 0. Vitrine basılamaz.
        var sonuc = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gumus, 3m, 4m)],
            [A(MKur.PariteKodlari.Gumus, adim: 10m)]);

        Assert.Empty(sonuc.Satirlar);
        Assert.Equal(MPano.AtlamaSebebi.MarjGecersiz, Assert.Single(sonuc.Atlananlar).Sebep);
    }

    [Fact]
    public void Gecerli_marj_satiri_hala_VITRINE_CIKAR()
    {
        // Kapı ne eksik ne fazla kapansın: normal marj etkilenmez.
        var satir = MPano.PanoFiyatServisi.Hesapla(
            [P(MKur.PariteKodlari.Gram, 5_000m, 5_100m)],
            [A(MKur.PariteKodlari.Gram, alisMarj: -50m, satisMarj: 75m)]).Satirlar.Single();

        Assert.Equal(4_950m, satir.Alis);
        Assert.Equal(5_175m, satir.Satis);
    }

    // ── B-18/1 — kayıt kapısı ───────────────────────────────────────────────

    [Fact]
    public void Dogrula_kotasyon_biliniyorken_fiyati_sifira_indiren_marji_REDDEDER()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(
                A(MKur.PariteKodlari.Gram, alisMarj: -999_999m),
                P(MKur.PariteKodlari.Gram, 5_000m, 5_100m)));

        Assert.Contains("sıfır", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dogrula_ters_makas_ureten_marji_REDDEDER()
    {
        Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(
                A(MKur.PariteKodlari.Gram, alisMarj: 500m, satisMarj: -500m),
                P(MKur.PariteKodlari.Gram, 5_000m, 5_100m)));
    }

    [Fact]
    public void Dogrula_kotasyon_bilinmiyorsa_yalniz_MUTLAK_TAVANI_uygular()
    {
        // Madde 1: kotasyon yoksa uydurma bir sınır konmaz — bu davranış
        // BİLİNÇLİDİR ve raporda yazılıdır.
        MPano.PanoFiyatServisi.Dogrula(A(MKur.PariteKodlari.Gram, alisMarj: -999_999m));

        Assert.Throws<BadRequestException>(() =>
            MPano.PanoFiyatServisi.Dogrula(A(MKur.PariteKodlari.Gram, alisMarj: -2_000_000m)));
    }

    [Fact]
    public void Dogrula_makul_marji_kotasyonla_birlikte_GECIRIR()
    {
        MPano.PanoFiyatServisi.Dogrula(
            A(MKur.PariteKodlari.Gram, alisMarj: -50m, satisMarj: 75m),
            P(MKur.PariteKodlari.Gram, 5_000m, 5_100m));
    }
}

// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P38HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>
    /// Kaynak defterine geçici kotasyon yazar. KÜLÇE seçilir: varsayılan pano
    /// kümesinde YOKTUR (kalem kapalı doğar), yani tohum başka testlerin
    /// gördüğü /pano/guncel çıktısını DEĞİŞTİRMEZ. Defterde "sil" yolu yok —
    /// bu yüzden sızıntı, sızdırmayan bir kalem seçilerek önlenir.
    /// </summary>
    private async Task KotasyonlaCalis(Func<Task> is_)
    {
        var defter = f.Ortam.Services.GetRequiredService<MKur.KurKaynakDefteri>();
        // Testlerde sözleşmesiz dış kaynaklar KAPALI olabilir → adaptör yok,
        // kiracının kaynağı MANUAL'a düşer. İkisi de tohumlanır.
        var kodlar = defter.Kodlar.Append(MKur.KurKaynakDefteri.ManuelKod).Distinct().ToList();
        var onceki = kodlar.ToDictionary(k => k, defter.Pariteler);
        try
        {
            foreach (var kod in kodlar)
                defter.PariteleriKaydet(kod,
                [
                    new MKur.Parite(MKur.PariteKodlari.Kulce,
                        MKur.PariteKodlari.Ad(MKur.PariteKodlari.Kulce), 5_000m, 5_100m, "GRAM"),
                ]);
            // Tohum gerçekten yerleşti mi? (yerleşmezse test sessizce yeşil kalırdı)
            Assert.NotEmpty(defter.Pariteler(MKur.KurKaynakDefteri.ManuelKod));
            await is_();
        }
        finally
        {
            foreach (var (kod, p) in onceki) defter.PariteleriKaydet(kod, p);
        }
    }

    // ── B-18/1 (#112) — sunucu son sözü söyler ──────────────────────────────

    [Fact]
    public async Task Pano_marji_fiyati_sifira_indirirse_400_doner()
    {
        var k = YeniKiraci("P38-TEST pano");
        await KotasyonlaCalis(async () =>
        {
            var c = await f.Ortam.Istemci(k).PatchJson(
                $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Kulce}", new { alisMarjTl = -999_999m });

            Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
            // ⚠ Gövde JSON'dur: Türkçe harfler \u015F gibi kaçırılır, ham metinde
            //   "sıfır" aranamaz. Mesaj AYRIŞTIRILARAK sınanır.
            var mesaj = (await c.Json()).GetProperty("message").GetString();
            Assert.Contains("sıfır", mesaj, StringComparison.OrdinalIgnoreCase);

            // Ve ayar GERÇEKTEN yazılmamış olmalı.
            await using var db = f.Vt.OlusturSistem();
            Assert.False(await db.KiraciAyarlari.AnyAsync(a =>
                a.TenantId == k.TenantId
                && a.Anahtar == MPano.PanoAyarAnahtari.Anahtar(MKur.PariteKodlari.Kulce)));
        });
    }

    [Fact]
    public async Task Pano_makul_marji_kotasyon_varken_KABUL_EDER()
    {
        var k = YeniKiraci("P38-TEST pano ok");
        await KotasyonlaCalis(async () =>
        {
            var c = await f.Ortam.Istemci(k).PatchJson(
                $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Kulce}", new { alisMarjTl = -50m });
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        });
    }

    // ── B-70 (#163) — emanet açıklaması yutulmaz ────────────────────────────

    [Fact]
    public async Task Emanet_girisindeki_ACIKLAMA_deftere_yazilir()
    {
        var k = YeniKiraci("P38-TEST emanet");
        const string metin = "Müşteri bileziği tamir için bıraktı";

        var c = await f.Ortam.Istemci(k).PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m,
            kurSnapshotId = k.KurId, aciklama = metin,
        });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        await using var db = f.Vt.OlusturSistem();
        var h = await db.Hareketler.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS")
            .OrderByDescending(x => x.Olusturuldu).FirstAsync();

        Assert.Equal(metin, h.Aciklama);
    }

    [Fact]
    public async Task Emanet_hareket_listesi_ACIKLAMAYI_geri_verir()
    {
        // "Kayıt oluşuyor ama API'de aciklama: null" bulgusunun okuma yarısı.
        var k = YeniKiraci("P38-TEST emanet oku");
        const string metin = "Tamir emaneti — 3 gün";

        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k).PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 4m,
            kurSnapshotId = k.KurId, aciklama = metin,
        })).StatusCode);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/emanet/{k.CariId}")).Json();
        var ham = j.GetRawText();
        Assert.Contains("3 g", ham, StringComparison.Ordinal);
    }

    // ── B-66 (#67, #107) — pasif ürün ───────────────────────────────────────

    private async Task<string> UrunAc(Kiraci k, string kod)
    {
        var c = await f.Ortam.Istemci(k).PostJson("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod, ad = "P38 test ürünü", ayarId = "K22", gramAgirlik = 5m,
        });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Pasif_urun_AKTIF_suzgecli_listede_GORUNMEZ()
    {
        var k = YeniKiraci("P38-TEST urun liste");
        var id = await UrunAc(k, "P38-U1");
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k).DeleteAsync($"/api/v1/urunler/{id}")).StatusCode);

        var aktifler = await (await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/urunler?aktif=true&limit=200")).Json();
        Assert.DoesNotContain(id, aktifler.GetRawText(), StringComparison.Ordinal);

        // Süzgeçsiz listede DURUYOR (ürün ekranı pasifleri de gösterir) —
        // yani kayıp değil, yalnız SEÇİLEMEZ.
        var hepsi = await (await f.Ortam.Istemci(k).GetAsync("/api/v1/urunler?limit=200")).Json();
        Assert.Contains(id, hepsi.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pasif_urunle_SATIS_sunucuda_REDDEDILIR()
    {
        // "Pasife Al" onayının verdiği söz: "yeni satışta seçilemez".
        var k = YeniKiraci("P38-TEST urun satis");
        var id = await UrunAc(k, "P38-U2");
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k).DeleteAsync($"/api/v1/urunler/{id}")).StatusCode);

        var c = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/satis", new
        {
            urunId = id, adet = 1m, birimFiyatTl = 100m, odemeTuru = "VERESIYE",
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

        Assert.Equal(HttpStatusCode.NotFound, c.StatusCode);
        Assert.Contains("aktif", await c.Metin(), StringComparison.OrdinalIgnoreCase);
    }

    // ── B-25 (#159) — fiş seçicisi boş dönmez ───────────────────────────────

    [Fact]
    public async Task Fis_aramasi_SORGUSUZ_son_fisleri_listeler()
    {
        var k = YeniKiraci("P38-TEST fis");
        var fisId = Kimlik.Ulid();
        await using (var db = f.Vt.OlusturSistem())
        {
            db.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
                Tarih = DateTime.UtcNow, TlTutari = 1_250m, HasGrami = 0m, ParaBirimi = "TRY",
                KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
                FisId = fisId, Aciklama = "P38 fiş satırı", Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var c = await f.Ortam.Istemci(k).GetAsync($"/api/v1/arama?tur={AramaTurleri.Fis}&limit=10");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();

        Assert.True(j.GetProperty("gozatma").GetBoolean());
        Assert.Contains(fisId, j.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sorgusuz_CARI_aramasi_hala_BOS_doner_gozatilabilir_degil()
    {
        // Gözatma yalnız FİŞ için açıldı; diğer türlerin sözleşmesi değişmedi.
        var k = YeniKiraci("P38-TEST arama cari");
        var j = await (await f.Ortam.Istemci(k)
            .GetAsync($"/api/v1/arama?tur={AramaTurleri.Cari}&limit=10")).Json();

        Assert.False(j.GetProperty("gozatma").GetBoolean());
        Assert.Equal(0, j.GetProperty("sayi").GetInt32());
    }

    // ── B-18/3 (#229) — carisiz ekstre satırı ───────────────────────────────

    private static string Iban() =>
        TestIban.Uret();

    private async Task<(string Ithalat, string Satir)> Ekstre(Kiraci k)
    {
        var hesap = (await (await f.Ortam.Istemci(k).PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = Iban(), paraBirimi = "TRY",
        })).Json()).GetProperty("id").GetString()!;

        const string basli = "tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan";
        var icerik = basli + "\n01.09.2026;Havale;;100,00;1;P38R1;;BILINMEYEN UNVAN\n";

        var y = await (await f.Ortam.Istemci(k).PostJson($"/api/v1/bankalar/{hesap}/ekstre/yukle", new
        {
            dosyaAdi = "p38.csv", format = "CSV",
            icerikBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(icerik)),
        })).Json();
        var ithalat = y.GetProperty("ithalatId").GetString()!;

        var satirlar = await (await f.Ortam.Istemci(k)
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=100&offset=0")).Json();
        var satir = satirlar.GetProperty("satirlar").EnumerateArray().First()
            .GetProperty("id").GetString()!;
        return (ithalat, satir);
    }

    [Fact]
    public async Task Cari_eslesmeyen_ekstre_satiri_SESSIZCE_onaylanmaz()
    {
        var k = YeniKiraci("P38-TEST ekstre");
        var (ithalat, satir) = await Ekstre(k);

        var j = await (await f.Ortam.Istemci(k).PostJson(
            $"/api/v1/bankalar/ekstre/{ithalat}/onayla", new { satirIdler = new[] { satir } })).Json();

        Assert.Equal(0, j.GetProperty("basarili").GetInt32());
        Assert.Equal(1, j.GetProperty("carisizAtlanan").GetInt32());
        Assert.Equal(0, j.GetProperty("uretilenHareketSayisi").GetInt32());

        // Satır HATALI değil BEKLİYOR: hâlâ düzeltilebilir (UX-S25).
        var satirlar = await (await f.Ortam.Istemci(k)
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=100&offset=0")).Json();
        Assert.NotEqual("HATALI", satirlar.GetProperty("satirlar").EnumerateArray().First()
            .GetProperty("durum").GetString());

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.BankaHareketleri.CountAsync(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Carisiz_onay_ACIKCA_verilirse_hareket_uretilir_ve_SAYILIR()
    {
        var k = YeniKiraci("P38-TEST ekstre rıza");
        var (ithalat, satir) = await Ekstre(k);

        var j = await (await f.Ortam.Istemci(k).PostJson(
            $"/api/v1/bankalar/ekstre/{ithalat}/onayla",
            new { satirIdler = new[] { satir }, carisizOnay = true })).Json();

        Assert.Equal(1, j.GetProperty("basarili").GetInt32());
        Assert.Equal(1, j.GetProperty("carisizUretilen").GetInt32());
        Assert.Equal(0, j.GetProperty("carisizAtlanan").GetInt32());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// EKRAN TESTLERİ [bUnit] — B-39 (#48) ve B-66 (#107)
//
// DoD EK md.1: düğmenin VARLIĞI yetmez; tıklanınca hangi uca hangi gövdeyle
// gittiği ölçülür. B-39'un dersi de tam budur: P-13 "düğme zaten vardı" diyerek
// kapattı, ama düğme yalnız çekmecenin içindeydi — LİSTEDE yoktu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P38EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Sorgu, string Govde)> istekler = [];

    public P38EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<KabukServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(List<(string, string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var sorgu = istek.RequestUri!.Query;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, sorgu, govde));

            // ⚠ '{}' varsayılanı bırakılmaz: bu pakette dokunulan her uca
            //   GERÇEKÇİ yanıt verilir, yoksa ekran sessizce boş kalır.
            var g = yol switch
            {
                var y when y.EndsWith("/cari/c1/bagli-kayitlar") =>
                    """{"cariId":"c1","hareketSayisi":3,"tlBakiye":-1500.00,"hasGramBakiye":2.5}""",
                var y when y.EndsWith("/cari/c1") =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,"vergiNo":null,"adres":null,"pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"aktif":true}""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","tlBakiye":-1500.00,"hasGramBakiye":2.5,"aktif":true},{"id":"c2","ad":"Kapalı Cari","tur":"MUSTERI","telefon":null,"tlBakiye":0,"hasGramBakiye":0,"aktif":false}]""",
                var y when y.EndsWith("/urunler") =>
                    """[{"id":"u1","tip":"ZIYNET","kod":"Z-1","ad":"Tek taş","ayarId":"K22","ayar":"K22","milyem":916.00,"gramAgirlik":5.0,"tasAgirlik":0.0,"hasGrami":4.58,"iscilikTl":0,"satisFiyati":null,"birim":"ADET","barkod":"B1","aktif":true}]""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/urun-kategorileri") => "[]",
                var y when y.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/fiyat/teklif") =>
                    """{"teklifVar":true,"sebep":null,"listeFiyati":10000.00,"hasDegeri":null,"marjTl":null,"iscilikTl":null,"indirimTutari":null,"satisFiyati":10000.00,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur1","altinHasFiyati":2500.00,"usdTry":40,"eurTry":44,"gbpTry":52}""",
                var y when y.EndsWith("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"Kuyumcu","plan":"PRO","aiEtkin":false,"aktif":true,"varsayilanArayuzModu":"PROFESYONEL"}""",
                var y when y.EndsWith("/admin/vezne") => """{"zorunlu":false}""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // ── B-66 (#107) — pasif ürün satış listesine GELMEZ ─────────────────────

    [Fact]
    public void Islemler_urun_listesini_YALNIZ_aktif_suzgeciyle_ceker()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        e.WaitForElement("#islem-cari");

        // Sorgu dizesinde aktif=true YOKSA pasife alınmış ürün POS'ta seçilebilir
        // olurdu — "Pasife Al" onayının verdiği söz tutulmazdı (QA turu 7 #67).
        e.WaitForAssertion(() => Assert.Contains(
            istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/urunler") && i.Sorgu.Contains("aktif=true")));
    }

    // ── B-39 (#48) — ileri ve geri eylem AYNI yerde ─────────────────────────

    [Fact]
    public void Cari_listesinde_AKTIF_satirda_PASIFE_AL_dugmesi_vardir()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-pasife-al-c1");

        // Simetri: pasif satırda "Aktife al", aktif satırda "Pasife Al".
        Assert.NotNull(e.Find("#c-aktife-al-c2"));
        Assert.Empty(e.FindAll("#c-pasife-al-c2"));
        Assert.Empty(e.FindAll("#c-aktife-al-c1"));
    }

    [Fact]
    public void Listeden_pasife_alma_ONAY_ister_ve_DELETE_atar()
    {
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#c-pasife-al-c1");
        istekler.Clear();

        e.Find("#c-pasife-al-c1").Click();

        // Yıkıcı eylem: önce onay (DoD EK md.4) — tıklamak TEK BAŞINA silmez.
        e.WaitForElement("#c-pasif-onayla");
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");

        // Bağlı kayıt uyarısı korunur (UX-S23).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#c-pasif-bagli")));

        e.Find("#c-pasif-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(
            istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/cari/c1")));
    }
}
