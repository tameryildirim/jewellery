using System.Net;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3d — QA TURU 4 BORCU (HTTP kapısı).
//
// Kapatılan bulgular:
//   • KRİTİK 3 — "gider pusulası kaydının HİÇBİR alanı düzenlenemiyor".
//     Artık MALİ KİMLİK DIŞI iki alan (satıcı adresi · mal açıklaması)
//     PATCH ile düzeltilir; mali alanlar DEĞİŞMEZ ve gerekçesi ekranda yazar.
//   • KRİTİK 4 — /uyum "Kapat" gövde alan adı (sözleşme testi bu dosyada:
//     sunucunun TANIDIĞI ad "gerekce"dir; "kapatmaGerekcesi" 400 döner).
//
// PATCH sözleşmesi burada da sınanır: gövdede OLMAYAN alan KORUNUR,
// null olan SİLİNİR (kurul engel 4).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S3dHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task<string> PusulaKur(Kiraci k, string? adres = "Ulus Mah. 1", string? mal = "Hurda bilezik")
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        db.GiderPusulalari.Add(new GiderPusulasi
        {
            Id = id, TenantId = k.TenantId, PusulaNo = "GP-2026-000042",
            HareketId = Kuyumcu.Api.Common.Kimlik.Ulid(), SubeId = k.SubeId,
            SaticiTipi = SaticiTipi.NihaiTuketici, SaticiAdSoyad = "Test Satıcı",
            SaticiTckn = "11111111110", SaticiTcknHash = new string('a', 64), SifreSurumu = 0,
            SaticiAdres = adres, MalAciklamasi = mal,
            AyarId = "22K", GramAgirlik = 10m, HasGrami = 9.17m,
            BrutTutar = 10_000m, StopajOran = 0m, StopajTutar = 0m, NetOdenen = 10_000m,
            TeslimTarihi = DateTime.UtcNow, DuzenlemeSonTarihi = DateTime.UtcNow.AddDays(7),
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    // ── DP4 · gider pusulası yerinde düzeltme ────────────────────────────────

    [Fact]
    public async Task Pusulanin_mali_kimlik_disi_alanlari_PATCH_ile_duzeltilir()
    {
        var k = YeniKiraci("S-3d pusula duzenle");
        var id = await PusulaKur(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchJson(
            $"/api/v1/gider-pusulalari/{id}",
            new { saticiAdres = "Çankaya Mah. 7", malAciklamasi = "Hurda 22 ayar bilezik" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.Equal("Çankaya Mah. 7", json.GetProperty("saticiAdres").GetString());

        using var db = f.Vt.OlusturSistem();
        var g = await db.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal("Çankaya Mah. 7", g.SaticiAdres);
        Assert.Equal("Hurda 22 ayar bilezik", g.MalAciklamasi);
        // Mali hüviyet DEĞİŞMEDİ.
        Assert.Equal(10_000m, g.BrutTutar);
        Assert.Equal("Test Satıcı", g.SaticiAdSoyad);
    }

    [Fact]
    public async Task PATCH_sozlesmesi_gonderilmeyen_alan_KORUNUR_null_SILINIR()
    {
        var k = YeniKiraci("S-3d pusula patch");
        var id = await PusulaKur(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        // (1) yalnız adres gönderilir → mal açıklaması KORUNUR
        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PatchJson($"/api/v1/gider-pusulalari/{id}",
                new { saticiAdres = "Yeni adres" })).StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            var g = await db.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
            Assert.Equal("Yeni adres", g.SaticiAdres);
            Assert.Equal("Hurda bilezik", g.MalAciklamasi);   // EZİLMEDİ
        }

        // (2) ⚠ PAKET S-3f (QA turu 5, YÜKSEK) — SÖZLEŞME DEĞİŞTİ.
        //     Bu alanlar VUK md.234'ün ZORUNLU UNSURLARIDIR: düzeltilebilir ama
        //     SİLİNEMEZ. QA turu 5'te adres boşaltılıp 200 alınabiliyordu.
        //     "Gövdede yoksa korunur" dalı DEĞİŞMEDİ (yukarıdaki (1) onu ölçer);
        //     kapanan yalnız "null/boş = sil" dalıdır.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await istemci.PatchJson($"/api/v1/gider-pusulalari/{id}",
                new { malAciklamasi = (string?)null })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await istemci.PatchJson($"/api/v1/gider-pusulalari/{id}",
                new { saticiAdres = "   " })).StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            var g = await db.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
            Assert.Equal("Hurda bilezik", g.MalAciklamasi);   // SİLİNMEDİ
            Assert.Equal("Yeni adres", g.SaticiAdres);        // EZİLMEDİ
        }
    }

    [Fact]
    public async Task Pusula_duzenlemesi_denetim_izine_yazilir_deger_yazilmadan()
    {
        var k = YeniKiraci("S-3d pusula denetim");
        var id = await PusulaKur(k);

        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k, "SAHIP").PatchJson($"/api/v1/gider-pusulalari/{id}",
                new { saticiAdres = "Kızılay Mah. 3" })).StatusCode);

        using var db = f.Vt.OlusturSistem();
        var kayitlar = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == id).ToListAsync();

        var kayit = Assert.Single(kayitlar, d => (d.Detay ?? "").Contains("GP-2026-000042"));
        Assert.Contains("satıcı adresi", kayit.Detay!);
        Assert.True(kayit.Hassas);
        // KİŞİSEL VERİ (adresin kendisi) HİÇBİR denetim satırına YAZILMAZ.
        Assert.DoesNotContain(kayitlar, d => (d.Detay ?? "").Contains("Kızılay"));
    }

    [Fact]
    public async Task Iptal_edilmis_pusula_duzenlenemez_409()
    {
        var k = YeniKiraci("S-3d pusula iptalli");
        var id = await PusulaKur(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PostJson($"/api/v1/gider-pusulalari/{id}/iptal",
                new { gerekce = "Satıcının T.C. kimlik numarası hatalı yazılmış" })).StatusCode);

        var cevap = await istemci.PatchJson($"/api/v1/gider-pusulalari/{id}",
            new { saticiAdres = "Değişmemeli" });
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var g = await db.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal("Ulus Mah. 1", g.SaticiAdres);
    }

    [Fact]
    public async Task Duzenlenebilir_alan_gonderilmezse_400_ve_dogru_yol_yazilir()
    {
        var k = YeniKiraci("S-3d pusula bos");
        var id = await PusulaKur(k);

        // Mali alan gönderilmeye çalışılır: sunucu onu TANIMAZ → 400 + doğru yol.
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchJson(
            $"/api/v1/gider-pusulalari/{id}", new { brutTutar = 1m });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // ⚠ JSON gövdesi AYRIŞTIRILARAK sınanır (ham metinde Türkçe karakter kaçışlıdır).
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("İPTAL + YENİ PUSULA", mesaj);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(10_000m, (await db.GiderPusulalari.AsNoTracking()
            .FirstAsync(x => x.Id == id)).BrutTutar);
    }

    [Fact]
    public async Task MUHASEBE_pusula_duzenleyemez_403()
    {
        var k = YeniKiraci("S-3d pusula rol");
        var id = await PusulaKur(k);

        var cevap = await f.Ortam.Istemci(k, "MUHASEBE").PatchJson(
            $"/api/v1/gider-pusulalari/{id}", new { saticiAdres = "Olmaz" });
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── DP4-02 · /uyum "Kapat" gövde SÖZLEŞMESİ ──────────────────────────────
    // QA turu 4 KRİTİK 4: istemci "kapatmaGerekcesi" gönderiyordu, sunucu
    // "gerekce" bekliyor → her kapatma 400. Bu iki test sözleşmeyi ÇİVİLER.

    private async Task<string> SupheliKur(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        db.MasakSupheliBildirimler.Add(new MasakSupheliBildirim
        {
            Id = id, TenantId = k.TenantId, IslemTutariTl = 312_500m,
            Gerekce = "Olağandışı nakit hareketi", Durum = SupheliDurum.Acik,
            TespitTarihi = DateTime.UtcNow, SonBildirimTarihi = DateTime.UtcNow.AddDays(10),
            SaklamaBitis = DateTime.UtcNow.AddYears(8),
            BildirenKullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
            Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Supheli_kapat_sozlesmedeki_alan_adiyla_200_doner()
    {
        var k = YeniKiraci("S-3d supheli kapat");
        var id = await SupheliKur(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchJson(
            $"/api/v1/masak/supheli/{id}/kapat",
            new { gerekce = "Müşteri belgeleri incelendi, şüphe kalktı ve işlem açıklandı" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(SupheliDurum.Kapatildi, (await db.MasakSupheliBildirimler.AsNoTracking()
            .FirstAsync(x => x.Id == id)).Durum);
    }

    [Fact]
    public async Task Supheli_kapat_YANLIS_alan_adiyla_400_doner()
    {
        var k = YeniKiraci("S-3d supheli yanlis ad");
        var id = await SupheliKur(k);

        // İstemcinin ESKİ (hatalı) gövdesi — bu testin kırmızıya dönmesi,
        // sunucu sözleşmesinin sessizce gevşetildiği anlamına gelir.
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchJson(
            $"/api/v1/masak/supheli/{id}/kapat",
            new { kapatmaGerekcesi = "Müşteri belgeleri incelendi, şüphe kalktı ve işlem açıklandı" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(SupheliDurum.Acik, (await db.MasakSupheliBildirimler.AsNoTracking()
            .FirstAsync(x => x.Id == id)).Durum);
    }
}
