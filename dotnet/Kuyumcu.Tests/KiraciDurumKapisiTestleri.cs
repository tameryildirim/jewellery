using System.Data;
using System.Net.Http.Json;
using System.Net;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KİRACI DURUM KAPISI — paket F-2-min Faz A [HTTP + saf]
//
// Deneme bitince ya da durum SALT_OKUNUR olunca YAZMA kapanır; OKUMA ve DIŞA
// AKTARIM her koşulda açık kalır (KVKK md.11-12 · VUK 253/256 — veri rehin
// alınmaz).
//
// ⚠ 403 KAPIDAN GELİR, gövde doğrulamasından değil: yetkilendirme filtresi
//   model bağlamadan ÖNCE koşar. Bu yüzden testler gövdeyi asgari tutar ve
//   "403 mü değil mi" sorusunu sorar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KiraciDurumKapisiTestleri(HttpFikstur f)
{
    private Kiraci KiraciKur(string durum, DateTime? denemeBitis = null)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, $"PAKETF2-TEST {durum}");
        TestKurulum.KiraciDurumuYaz(db, k.TenantId, durum, denemeBitis);
        return k;
    }

    // ── Yazma kapalı ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SaltOkunur_Kiraci_Satis_POST_403()
    {
        var k = KiraciKur(KiraciDurumlari.SaltOkunur);

        var cevap = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/satis", new { });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Contains("SALT OKUNUR", await cevap.Metin(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deneme_Suresi_Gecmis_Kiraci_Urun_PATCH_403()
    {
        var k = KiraciKur(KiraciDurumlari.Deneme, DateTime.UtcNow.AddDays(-1));

        var cevap = await f.Ortam.Istemci(k)
            .PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "Yeni ad" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR()
    {
        // ⚠ BİLİNMEYEN DEĞER NASIL ÜRETİLİR (Aşkın koşulu): durum kolonu
        // VARCHAR(16)'dır, veritabanı düzeyinde enum kısıtı YOKTUR. Elle SQL,
        // eski bir sürümden kalan yazım ya da bozuk bir göç böyle bir değer
        // bırakabilir. Burada DbContext üzerinden doğrudan yazılır (raw SQL'e
        // gerek yok; kısıt zaten yok) — üretimde de aynı yolla oluşabilir.
        var k = KiraciKur("ASKIDA");

        var cevap = await f.Ortam.Istemci(k)
            .PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "Yeni ad" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Contains("tanınmadığı", await cevap.Metin(), StringComparison.Ordinal);
    }

    // ── Okuma ve dışa aktarım AÇIK ───────────────────────────────────────────

    [Fact]
    public async Task Deneme_Suresi_Gecmis_Kiraci_GET_Urunler_200()
    {
        var k = KiraciKur(KiraciDurumlari.Deneme, DateTime.UtcNow.AddDays(-1));

        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/urunler");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    [Fact]
    public async Task SaltOkunur_Kiraci_DisaAktarim_POST_ve_Indir_200()
    {
        var k = KiraciKur(KiraciDurumlari.SaltOkunur);
        var istemci = f.Ortam.Istemci(k);

        var talep = await istemci.PostJson("/api/v1/disa-aktarim", new
        {
            kaynaklar = new[] { "urunler" },
            gerekce = "Salt okunur kiracinin verisini indirme hakki testi",
        });

        // KAPIDAN GEÇTİ: yanıt ne olursa olsun 403 DEĞİL (muafiyet çalışıyor).
        Assert.NotEqual(HttpStatusCode.Forbidden, talep.StatusCode);
        Assert.Equal(HttpStatusCode.OK, talep.StatusCode);

        var jeton = (await talep.Json()).GetProperty("jeton").GetString()!;
        var indir = await istemci.PostJson($"/api/v1/disa-aktarim/{jeton}/indir", new { });
        Assert.NotEqual(HttpStatusCode.Forbidden, indir.StatusCode);
    }

    [Fact]
    public async Task SaltOkunur_Kiraci_Giris_Yapabilir()
    {
        var k = KiraciKur(KiraciDurumlari.SaltOkunur);
        const string sifre = "GucluParola123!";
        string eposta;
        using (var db = f.Vt.OlusturSistem())
        {
            var kullanici = db.Kullanicilar.Single(x => x.Id == k.KullaniciId);
            eposta = $"f2-giris-{k.KullaniciId}@test.local";
            kullanici.Email = eposta;
            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(sifre, 11);
            db.SaveChanges();
        }

        var cevap = await f.Ortam.AnonimIstemci()
            .PostJson("/api/v1/auth/giris", new { email = eposta, sifre });

        Assert.NotEqual(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    [Fact]
    public async Task AKTIF_Kiraci_Tum_Mutasyonlar_Etkilenmez()
    {
        var k = KiraciKur(KiraciDurumlari.Aktif);

        var patch = await f.Ortam.Istemci(k)
            .PatchAsJsonAsync($"/api/v1/urunler/{k.UrunId}", new { ad = "AKTIF kiracı ürünü" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        // Deneme bitişi geçmiş OLSA BİLE, durum AKTIF ise süre kısıtı işlemez.
        var k2 = KiraciKur(KiraciDurumlari.Aktif, DateTime.UtcNow.AddDays(-30));
        var patch2 = await f.Ortam.Istemci(k2)
            .PatchAsJsonAsync($"/api/v1/urunler/{k2.UrunId}", new { ad = "Süresi geçmiş ama AKTIF" });
        Assert.Equal(HttpStatusCode.OK, patch2.StatusCode);
    }

    // ── Muafiyet listesi — SAF testler (segment sınırı) ──────────────────────

    [Fact]
    public void Muafiyet_listesi_GENIS_onek_ICERMEZ()
    {
        // '/api/v1' ya da '/api' muafiyete girseydi kapı HİÇBİR ŞEYİ korumazdı.
        Assert.DoesNotContain("/api/v1", KiraciDurumKapisi.MuafYollar);
        Assert.DoesNotContain("/api", KiraciDurumKapisi.MuafYollar);
        Assert.False(KiraciDurumKapisi.MuafMi("/api/v1/islemler/satis"));
        Assert.False(KiraciDurumKapisi.MuafMi("/api/v1/urunler"));
    }

    [Theory]
    [InlineData("/api/v1/disa-aktarim")]
    [InlineData("/api/v1/disa-aktarim/")]
    [InlineData("/api/v1/disa-aktarim/abc/indir")]
    [InlineData("/API/V1/DISA-AKTARIM")]
    [InlineData("/api/v1/auth/giris")]
    [InlineData("/api/v1/sistem/kiracilar")]
    public void Muaf_yollar_SEGMENT_sinirinda_eslesir(string yol)
        => Assert.True(KiraciDurumKapisi.MuafMi(yol));

    [Theory]
    [InlineData("/api/v1/disa-aktarim-sahte")]
    [InlineData("/api/v1/authx")]
    [InlineData("/api/v1/sistematik")]
    public void Benzer_isimli_yollar_MUAF_DEGILDIR(string yol)
        => Assert.False(KiraciDurumKapisi.MuafMi(yol));

    // ── Önbellek (30 sn) — iki yön ──────────────────────────────────────────

    /// <summary>
    /// Aşkın koşulu: önbellek yalnız "uzatma" yönünde değil, KISITLAMA
    /// yönünde de doğru davranmalı. Süre dolunca kayıt düşer; durum değişimi
    /// ise <c>Bosalt</c> ile ANINDA etkir.
    /// </summary>
    [Fact]
    public void Onbellek_omru_dolunca_yeniden_okutur_ve_Bosalt_aninda_duserir()
    {
        var o = new KiraciDurumOnbellegi();
        var t0 = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

        o.Yaz("t1", new KiraciDurumu(KiraciDurumlari.Aktif, null), t0);
        Assert.NotNull(o.Oku("t1", t0));
        Assert.NotNull(o.Oku("t1", t0.AddSeconds(29)));
        // 30 sn dolunca kayıt geçersizdir → kapı tenants'ı YENİDEN okur,
        // yani AKTIF→SALT_OKUNUR geçişi en geç 30 sn içinde uygulanır.
        Assert.Null(o.Oku("t1", t0.AddSeconds(31)));

        o.Yaz("t1", new KiraciDurumu(KiraciDurumlari.Aktif, null), t0);
        o.Bosalt("t1");
        Assert.Null(o.Oku("t1", t0));
    }

    [Fact]
    public void Durum_beyaz_listesi_bilinmeyeni_KAPATIR()
    {
        var now = DateTime.UtcNow;
        Assert.True(new KiraciDurumu(KiraciDurumlari.Aktif, null).YazabilirMi(now));
        Assert.True(new KiraciDurumu(KiraciDurumlari.Deneme, now.AddDays(1)).YazabilirMi(now));
        Assert.False(new KiraciDurumu(KiraciDurumlari.Deneme, now.AddDays(-1)).YazabilirMi(now));
        Assert.False(new KiraciDurumu(KiraciDurumlari.SaltOkunur, null).YazabilirMi(now));
        Assert.False(new KiraciDurumu("", null).YazabilirMi(now));
        Assert.False(new KiraciDurumu("ASKIDA", null).YazabilirMi(now));
        Assert.False(new KiraciDurumu("aktif", null).YazabilirMi(now));   // büyük/küçük harf duyarlı
    }

    // ── Migration varsayılanı (Aşkın koşulu — üretim durdurucu risk) ─────────

    /// <summary>
    /// ⚠ ÜRETİM DURDURUCU RİSK: migration <c>tenants.durum</c> kolonuna 'AKTIF'
    /// VARSAYILANI koymazsa, migration sonrası MEVCUT kiracılar boş/bilinmeyen
    /// durumla doğar ve fail-closed kapı hepsinin yazmasını kilitler. Bu risk
    /// şimdiye dek yalnız elle SQL ile bakılıyordu — CI'de yakalanmıyordu.
    /// <para>
    /// Test, MIGRATION ile kurulan test şemasını (TestVeritabani → Migrate,
    /// EnsureCreated DEĞİL) sorgular: (1) information_schema'da kolon NOT NULL
    /// ve COLUMN_DEFAULT='AKTIF' mi, (2) durum kolonu VERİLMEDEN ham SQL ile
    /// yazılan bir satır gerçekten 'AKTIF' doğuyor mu. CLR varsayılanı
    /// (TestKurulum.KiraciKur) bilinçli olarak devre dışı bırakılır — sınanan
    /// şemanın kendisidir.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar()
    {
        await using var db = f.Vt.OlusturSistem();
        var baglanti = db.Database.GetDbConnection();
        if (baglanti.State != ConnectionState.Open) await baglanti.OpenAsync();

        // (1) Şema iddiası — kolon NOT NULL ve varsayılanı 'AKTIF'.
        await using (var kolon = baglanti.CreateCommand())
        {
            kolon.CommandText =
                "SELECT COLUMN_DEFAULT, IS_NULLABLE FROM information_schema.COLUMNS "
                + "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tenants' "
                + "AND COLUMN_NAME = 'durum'";
            await using var okuyucu = await kolon.ExecuteReaderAsync();
            Assert.True(await okuyucu.ReadAsync(), "tenants.durum kolonu test şemasında YOK.");
            Assert.Equal(KiraciDurumlari.Aktif, okuyucu.GetValue(0) as string);
            Assert.Equal("NO", okuyucu.GetString(1));
        }

        // (2) Davranış iddiası — durum kolonu VERİLMEDEN yazılan satır AKTIF doğar.
        var id = "PAKETF2-TEST-VARSAYILAN";
        try
        {
            await using (var ekle = baglanti.CreateCommand())
            {
                ekle.CommandText =
                    "INSERT INTO tenants (id, ad, plan, ai_etkin, aktif, olusturuldu, guncellendi) "
                    + "VALUES (@id, 'PAKETF2-TEST varsayilan', 'BASLANGIC', 0, 1, "
                    + "UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))";
                var p = ekle.CreateParameter();
                p.ParameterName = "@id";
                p.Value = id;
                ekle.Parameters.Add(p);
                Assert.Equal(1, await ekle.ExecuteNonQueryAsync());
            }

            await using var oku = baglanti.CreateCommand();
            oku.CommandText = "SELECT durum FROM tenants WHERE id = '" + id + "'";
            Assert.Equal(KiraciDurumlari.Aktif, (string?)await oku.ExecuteScalarAsync());
        }
        finally
        {
            // Fikstür kirletilmez: satır yalnız bu testin kendi kaydıdır.
            await using var sil = baglanti.CreateCommand();
            sil.CommandText = "DELETE FROM tenants WHERE id = '" + id + "'";
            await sil.ExecuteNonQueryAsync();
        }
    }
}
