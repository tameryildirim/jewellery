using System.Net;
using System.Net.Http.Json;
using System.Text;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Masak;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Contracts.Sayfalar.Uyum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-18 — BM yaptırım listesi (ayrıştırma + fark), ŞİB gönderim hazırlığı ve gizlilik, uyum raporu, denetim CSV.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1BmListesiTestleri(TestVeritabani vt) : IClassFixture<TestVeritabani>
{
    private static Stream Xml(params (string Ref, string Ad, string? Kimlik)[] kisiler)
    {
        var sb = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><CONSOLIDATED_LIST dateGenerated="2000-01-01T23:00:01.054Z"><INDIVIDUALS>""");
        foreach (var k in kisiler)
        {
            var ad = k.Ad.Split(' ');
            sb.Append($"<INDIVIDUAL><FIRST_NAME>{ad[0]}</FIRST_NAME><SECOND_NAME>{ad[1]}</SECOND_NAME><REFERENCE_NUMBER>{k.Ref}</REFERENCE_NUMBER>");
            sb.Append("<INDIVIDUAL_DATE_OF_BIRTH><TYPE_OF_DATE>EXACT</TYPE_OF_DATE><DATE>1970-05-06</DATE></INDIVIDUAL_DATE_OF_BIRTH>");
            if (k.Kimlik is not null) sb.Append($"<INDIVIDUAL_DOCUMENT><TYPE_OF_DOCUMENT>Passport</TYPE_OF_DOCUMENT><NUMBER>{k.Kimlik}</NUMBER></INDIVIDUAL_DOCUMENT>");
            sb.Append("</INDIVIDUAL>");
        }
        sb.Append("</INDIVIDUALS><ENTITIES><ENTITY><FIRST_NAME>TEST D118 KURULUS</FIRST_NAME><REFERENCE_NUMBER>TEST-D118-E1</REFERENCE_NUMBER></ENTITY></ENTITIES></CONSOLIDATED_LIST>");
        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    [Fact]
    public async Task Bm_xml_ayristirilir_yeni_eklenir_dusen_kaldirilir_bozuk_dosya_hicbir_sey_kaldirmaz()
    {
        var (tarih, satirlar) = BmYaptirimListesi.Coz(Xml(("TEST-D118-1", "ALI VELI", "P123"), ("TEST-D118-2", "AYSE FATMA", null)));
        Assert.Equal(new DateOnly(2000, 1, 1), tarih);
        Assert.Equal(3, satirlar.Count);
        var ali = satirlar.Single(s => s.KararNo == "TEST-D118-1");
        Assert.Equal("ALI VELI", ali.AdSoyad);
        Assert.Equal(new DateOnly(1970, 5, 6), ali.DogumTarihi);
        Assert.Equal("P123", ali.KimlikNo);

        using (var db = vt.OlusturSistem())
            await BmYaptirimListesi.Uygula(db, new DenetimServisi(db, new HttpContextAccessor()), BmYaptirimListesi.VarsayilanUrl, tarih, satirlar, "test", asgari: 1);
        using (var db = vt.OlusturSistem())
            Assert.Equal(3, db.YaptirimKayitlari.AsNoTracking().Count(y => y.Kaynak == BmYaptirimListesi.Kaynak && y.KararNo!.StartsWith("TEST-D118") && y.Kaldirildi == null));

        // İkinci akışta 2 düşer, 3 gelir.
        var (t2, s2) = BmYaptirimListesi.Coz(Xml(("TEST-D118-1", "ALI VELI", "P123"), ("TEST-D118-3", "CAN CEM", null)));
        using (var db = vt.OlusturSistem())
            await BmYaptirimListesi.Uygula(db, new DenetimServisi(db, new HttpContextAccessor()), BmYaptirimListesi.VarsayilanUrl, t2, s2, "test", asgari: 1);
        using (var db = vt.OlusturSistem())
        {
            var aktif = db.YaptirimKayitlari.AsNoTracking().Where(y => y.Kaynak == BmYaptirimListesi.Kaynak && y.KararNo!.StartsWith("TEST-D118") && y.Kaldirildi == null)
                .Select(y => y.KararNo).OrderBy(x => x).ToList();
            Assert.Equal(["TEST-D118-1", "TEST-D118-3", "TEST-D118-E1"], aktif);
        }

        // Taban altı (bozuk/kesik dosya): istisna, hiçbir satır kaldırılmaz.
        var (t3, s3) = BmYaptirimListesi.Coz(Xml(("TEST-D118-9", "XX YY", null)));
        using (var db = vt.OlusturSistem())
            await Assert.ThrowsAsync<InvalidDataException>(() => BmYaptirimListesi.Uygula(db, new DenetimServisi(db, new HttpContextAccessor()), BmYaptirimListesi.VarsayilanUrl, t3, s3, "test"));
        using (var db = vt.OlusturSistem())
            Assert.Equal(3, db.YaptirimKayitlari.AsNoTracking().Count(y => y.Kaynak == BmYaptirimListesi.Kaynak && y.KararNo!.StartsWith("TEST-D118") && y.Kaldirildi == null));
    }
}

[Collection("http")]
public sealed class D1UyumHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Sib_hazirlik_adimlari_gizlilik_rol_ve_salt_okunur_kapisi_uyum_raporu_denetim_csv()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1UYUM");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var gerekce = string.Concat(Enumerable.Repeat("Müşteri kaynağını açıklayamadığı yüksek tutarlı nakit ödeme yaptı. ", 6));
        var r = await s.PostAsJsonAsync("/api/v1/masak/supheli", new { cariId = k.CariId, islemTutariTl = 450000m, gerekce });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.MasakSupheliBildirimler.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;

        var h = await s.GetFromJsonAsync<SibHazirlik>($"/api/v1/masak/supheli/{id}/hazirlik");
        Assert.Equal("https://onlineislemler.masak.gov.tr", h!.Adres);
        Assert.Contains("5549", h.GizlilikNotu);
        Assert.Equal([1, 2, 3, 4, 5, 6], h.Adimlar.Select(a => a.No));
        Assert.Contains(h.Adimlar[2].Alanlar, a => a.Etiket == "Tutar (TL)" && a.Deger == "450.000,00");
        Assert.Equal(gerekce.Trim(), h.Adimlar[4].Alanlar[0].Deger.Trim());
        using (var db = f.Vt.OlusturSistem())
            Assert.True(db.DenetimKayitlari.AsNoTracking().Any(d => d.TenantId == k.TenantId && d.VarlikId == id && d.Hassas && d.Eylem == DenetimEylemi.Okuma));

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync($"/api/v1/masak/supheli/{id}/hazirlik")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/masak/supheli")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/masak/supheli")).StatusCode);
        var salt = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "MUHASEBE", saltOkunur: true);
        Assert.Equal(HttpStatusCode.Forbidden, (await salt.GetAsync("/api/v1/masak/supheli")).StatusCode);   // dışarıdan mali müşavir ŞİB görmez

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        var donem = $"{bugun.AddDays(-30):yyyy-MM-dd}_{bugun:yyyy-MM-dd}";
        var b = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.UyumRaporu}/{donem}");
        Assert.Equal("UYUM RAPORU", b!.BelgeAdi);
        var sib = b.Bolumler.Single(x => x.Baslik == "Şüpheli işlem bildirimi");
        Assert.Equal("1", sib.Satirlar.Single(x => x.Hucreler[0] == "Dönemde tespit edilen").Hucreler[1]);
        Assert.Equal("1", sib.Satirlar.Single(x => x.Hucreler[0] == "Bugün açık (gönderilmemiş)").Hucreler[1]);
        Assert.DoesNotContain(gerekce[..20], System.Text.Json.JsonSerializer.Serialize(b));   // kişisel veri / gerekçe yok
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync($"/api/v1/cikti/{BelgeTurleri.UyumRaporu}/{donem}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/v1/cikti/{BelgeTurleri.UyumRaporu}/2026-01-01_2024-01-01")).StatusCode);

        var csv = await s.GetAsync("/api/v1/denetim/disa-aktar");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        var bayt = await csv.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bayt[..3]);
        Assert.StartsWith("Tarih;Kullanıcı;Eylem;", Encoding.UTF8.GetString(bayt[3..]));
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/denetim/disa-aktar")).StatusCode);
    }
}
