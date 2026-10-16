using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BİLDİRİM / İŞ UÇLARI — GERÇEK HTTP BORU HATTI (paket 6 Faz A)
//
// Servis testi [ModulYetkisi] ve [RolGerekli] niteliklerini GÖRMEZ; bu dosya
// kapıları üretimdeki boru hattında ölçer.
//
// ⚠ HttpOrtami tüm IHostedService'leri kaldırır → IsZamanlayici DÖNGÜSÜ burada
//   koşmaz. Bildirim üretimi elle tetikleme ucundan yapılır; döngünün kendisi
//   canlı doğrulamayla ölçüldü (raporda "birim testle değil canlıyla doğrulandı").
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class BildirimHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Doğrudan bildirim yazar (iş koşturmadan) — uç davranışını ölçmek için.</summary>
    private async Task<string> BildirimYaz(Kiraci k, string tur, string onem, string roller, string anahtar)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        var id = Kimlik.Ulid();
        db.Bildirimler.Add(new Bildirim
        {
            Id = id, TenantId = k.TenantId, Tur = tur, Onem = onem,
            Baslik = $"{tur} başlık", Govde = "gövde", HedefRota = "/uyum",
            GorunurRoller = roller, TekillikAnahtari = anahtar, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>Tezgâhtara "bildirim" modül yetkisini SAHİP eliyle açar (yetki ekranının yolu).</summary>
    private async Task TezgahtaraBildirimYetkisiVer(Kiraci k)
    {
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Tumu],
            ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "rapor", "bildirim"],
            ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka", "fatura", "bildirim"],
        };
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    // ── Kimlik / yetki kapıları ─────────────────────────────────────────────

    [Fact]
    public async Task Jetonsuz_istek_401_doner()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/bildirimler");
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    /// <summary>
    /// TOHUMLAMA REGRESYONU: "bildirim" anahtarı MUDUR/MUHASEBE'ye varsayılan
    /// AÇIK, TEZGAHTAR'a KAPALI gelir. Eski tek-marker mekanizmasında bu anahtar
    /// mevcut kiracıda sessizce 403 verirdi.
    /// </summary>
    [Fact]
    public async Task Varsayilan_yetki_MUDUR_acik_TEZGAHTAR_kapali()
    {
        var k = YeniKiraci("PAKET6-HTTP varsayılan yetki");

        var mudur = await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/bildirimler");
        var tezgah = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/bildirimler");

        Assert.Equal(HttpStatusCode.OK, mudur.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tezgah.StatusCode);
        Assert.Contains("bildirim", await tezgah.Metin(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ESKİ MARKER SENARYOSU: kiracı paket 6'dan ÖNCEKİ hâline getirilir
    /// (yalnız genel marker + 8 eski anahtar). Modül-başı tohumlama "bildirim"i
    /// açar, eski anahtarları YENİDEN tohumlamaz.
    /// </summary>
    [Fact]
    public async Task Eski_marker_li_kiracida_bildirim_anahtari_TOHUMLANIR()
    {
        var k = YeniKiraci("PAKET6-HTTP eski marker");

        // Önce tohumlamayı tetikle, sonra veriyi ESKİ hâle çevir.
        Assert.Equal(HttpStatusCode.OK, (await f.Ortam.Istemci(k, "MUDUR").GetAsync("/api/v1/bildirimler")).StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            // Modül markerleri ve TOHUMLAMA SONRASI eklenen her anahtarın satırı
            // silinir; SAHİBİN kaldırdığı bir yetkiyi taklit etmek için
            // MUHASEBE'nin "rapor" satırı da silinir.
            //
            // ⚠ 2026-09-16 (paket 3): burada yalnız "bildirim" siliniyordu. İkinci
            //   bir tohumlama-sonrası anahtar ("siparis") gelince fikstür artık
            //   "paket 6 öncesi kiracı" OLMUYORDU: markeri silinmiş ama satırları
            //   duran "siparis" yüzünden tohumlama INSERT'i benzersizlik ihlaline
            //   düşüyor ve TÜM parti (bildirim dahil) yutuluyordu. Silme artık
            //   TohumlamaOncesi listesine göre yapılır — fikstür kendi iddiasına
            //   sadık kalır, mekanizmaya DOKUNULMAZ.
            var eskiAnahtarlar = string.Join(",", Moduller.TohumlamaOncesi.Select(m => "'" + m + "'"));
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM rol_yetkileri WHERE tenant_id = {0} AND (modul LIKE '__tohum:%' "
                + "OR (rol <> '__SISTEM__' AND modul NOT IN (" + eskiAnahtarlar + ")))",
                k.TenantId);
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM rol_yetkileri WHERE tenant_id = {0} AND rol = 'MUHASEBE' AND modul = 'rapor'",
                k.TenantId);
        }

        // Önbelleği düşürmek için yeni bir kiracı isteği yerine yetki yazımı
        // kullanılamaz (o tohumlamayı ezerdi); ortam bellek önbelleği süreç
        // geneli olduğundan doğrudan ölçüm için yeni bir süreç gerekmez:
        // Matris() önbelleği 30 dk'dır, bu yüzden ölçüm SERVİS katmanından yapılır.
        using (var db2 = f.Vt.Olustur(k.TenantId))
        {
            var servis = new YetkiServisi(db2, new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

            Assert.True(await servis.ErisebilirMi(k.TenantId, "MUDUR", "bildirim"));
            Assert.True(await servis.ErisebilirMi(k.TenantId, "MUHASEBE", "bildirim"));
            Assert.False(await servis.ErisebilirMi(k.TenantId, "TEZGAHTAR", "bildirim"));
            // Eski anahtar GERİ GELMEDİ: sahibin kaldırdığı yetki yeniden açılmaz.
            Assert.False(await servis.ErisebilirMi(k.TenantId, "MUHASEBE", "rapor"));
        }
    }

    // ── Görünürlük süzmesi ──────────────────────────────────────────────────

    /// <summary>M6-5: gorunur_roller boş bırakılırsa MASAK bildirimi tezgâhtara sızar.</summary>
    [Fact]
    public async Task TEZGAHTAR_MASAK_kaynakli_bildirimi_GORMEZ()
    {
        var k = YeniKiraci("PAKET6-HTTP masak görünürlük");
        await TezgahtaraBildirimYetkisiVer(k);

        var masakId = await BildirimYaz(k, "MASAK_SIB_SURESI", BildirimOnem.Kritik,
            "SAHIP,MUDUR,MUHASEBE", $"SIB:{k.TenantId}");
        var vadeId = await BildirimYaz(k, "VADE_HATIRLATMA", BildirimOnem.Uyari,
            "", $"VADE:{k.TenantId}");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/bildirimler");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var idler = (await cevap.Json()).GetProperty("bildirimler").EnumerateArray()
            .Select(b => b.GetProperty("id").GetString()).ToList();
        Assert.Contains(vadeId, idler);
        Assert.DoesNotContain(masakId, idler);

        // Rozet sayısı da süzülür.
        var sayi = await (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/bildirimler/sayi")).Json();
        Assert.Equal(1, sayi.GetProperty("sayi").GetInt32());

        // Muhasebe İKİSİNİ de görür.
        var muhasebe = await (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/bildirimler")).Json();
        Assert.Equal(2, muhasebe.GetProperty("bildirimler").GetArrayLength());
    }

    /// <summary>
    /// M6-5: MASAK işinin ÜRETTİĞİ bildirim (elle tetikleme ucundan) tezgâhtara
    /// görünmez. Önceki test bildirimi elle yazıyordu; bu test İŞİN kendi
    /// gorunur_roller kararını uçtan uca ölçer.
    /// </summary>
    [Fact]
    public async Task Is_ucundan_uretilen_MASAK_bildirimi_TEZGAHTARA_gorunmez()
    {
        var k = YeniKiraci("PAKET6-HTTP masak iş görünürlük");
        await TezgahtaraBildirimYetkisiVer(k);

        using (var db = f.Vt.Olustur(k.TenantId))
        {
            db.MasakSupheliBildirimler.Add(new MasakSupheliBildirim
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId,
                Gerekce = new string('G', 320), TespitTarihi = DateTime.UtcNow.AddDays(-7),
                SonBildirimTarihi = DateTime.UtcNow.AddDays(1), Durum = SupheliDurum.Acik,
                BildirenKullaniciId = k.KullaniciId, SaklamaBitis = DateTime.UtcNow.AddYears(8),
                Olusturuldu = DateTime.UtcNow.AddDays(-7),
            });
            await db.SaveChangesAsync();
        }

        var kosum = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/isler/{Kuyumcu.Api.Modules.Isler.Isler.MasakSibSuresiIsi.IsKodu}/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, kosum.StatusCode);
        Assert.Equal(1, (await kosum.Json()).GetProperty("uretilenBildirim").GetInt32());

        var tezgah = await (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/bildirimler")).Json();
        Assert.Equal(0, tezgah.GetProperty("bildirimler").GetArrayLength());

        var muhasebe = await (await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/bildirimler")).Json();
        Assert.Equal(1, muhasebe.GetProperty("bildirimler").GetArrayLength());
    }

    /// <summary>Görmediğin bildirimi KAPATAMAZSIN (süzme yazmada da geçerli).</summary>
    [Fact]
    public async Task TEZGAHTAR_gormedigi_MASAK_bildirimini_KAPATAMAZ_404()
    {
        var k = YeniKiraci("PAKET6-HTTP masak kapatma");
        await TezgahtaraBildirimYetkisiVer(k);
        var masakId = await BildirimYaz(k, "MASAK_SIB_SURESI", BildirimOnem.Kritik,
            "SAHIP,MUDUR,MUHASEBE", $"SIB2:{k.TenantId}");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PostJson($"/api/v1/bildirimler/{masakId}/kapat", new { not = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    // ── Liste sınırı ────────────────────────────────────────────────────────

    [Fact]
    public async Task Limit_100000_istense_de_en_cok_100_kayit_doner()
    {
        var k = YeniKiraci("PAKET6-HTTP limit");
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            for (var n = 0; n < 105; n++)
                db.Bildirimler.Add(new Bildirim
                {
                    Id = Kimlik.Ulid(), TenantId = k.TenantId, Tur = "SAHTE", Onem = BildirimOnem.Bilgi,
                    Baslik = "b", Govde = "g", GorunurRoller = "",
                    TekillikAnahtari = $"LIMIT:{k.TenantId}:{n}", Olusturuldu = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/bildirimler?limit=100000");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();

        Assert.Equal(100, g.GetProperty("bildirimler").GetArrayLength());
        Assert.Equal(100, g.GetProperty("limit").GetInt32());
        // UX-S29: sessiz kırpma yok — "daha fazlası var" sunucudan söylenir.
        Assert.True(g.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    // ── Kapatma sözleşmesi ──────────────────────────────────────────────────

    [Fact]
    public async Task Kapatilmis_bildirimi_tekrar_kapatmak_400_doner_ve_satir_SILINMEZ()
    {
        var k = YeniKiraci("PAKET6-HTTP kapatma");
        var id = await BildirimYaz(k, "VADE_HATIRLATMA", BildirimOnem.Uyari, "", $"KAP:{k.TenantId}");

        var ilk = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bildirimler/{id}/kapat", new { not = "tahsil edildi" });
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
        Assert.Equal("Bildirim kapatıldı", (await ilk.Json()).GetProperty("mesaj").GetString());

        var ikinci = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/bildirimler/{id}/kapat", new { not = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);

        using var kontrol = f.Vt.OlusturSistem();
        var b = await kontrol.Bildirimler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.NotNull(b.Kapatildi);                 // damga kondu
        Assert.Equal("tahsil edildi", b.KapatmaNotu);
        Assert.Equal(1, await kontrol.Bildirimler.CountAsync(x => x.TenantId == k.TenantId));   // satır DURUYOR

        // Kapatma denetim izine yazıldı.
        Assert.True(await kontrol.DenetimKayitlari.AnyAsync(
            d => d.TenantId == k.TenantId && d.VarlikTuru == "Bildirim" && d.VarlikId == id));
    }

    [Fact]
    public async Task Baska_kiracinin_bildirimi_KAPATILAMAZ_404()
    {
        var a = YeniKiraci("PAKET6-HTTP kiracı A");
        var b = YeniKiraci("PAKET6-HTTP kiracı B");
        var id = await BildirimYaz(a, "VADE_HATIRLATMA", BildirimOnem.Uyari, "", $"CAPRAZ:{a.TenantId}");

        var cevap = await f.Ortam.Istemci(b, "SAHIP").PostJson($"/api/v1/bildirimler/{id}/kapat", new { not = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Null((await kontrol.Bildirimler.AsNoTracking().SingleAsync(x => x.Id == id)).Kapatildi);
    }

    // ── İş uçları ───────────────────────────────────────────────────────────

    [Fact]
    public async Task TEZGAHTAR_isi_ELLE_calistiramaz_403()
    {
        var k = YeniKiraci("PAKET6-HTTP iş rolü");
        await TezgahtaraBildirimYetkisiVer(k);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PostJson($"/api/v1/isler/{Kuyumcu.Api.Modules.Isler.Isler.VadeHatirlatmaIsi.IsKodu}/calistir", new { });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        var govde = await cevap.Metin();
        // KESIF-P1 (#72): 403 metni rol ADINI taşır, ham kodu değil.
        Assert.Contains("işletme sahibi", System.Text.RegularExpressions.Regex.Unescape(govde), StringComparison.Ordinal);
        Assert.Contains("müdür", System.Text.RegularExpressions.Regex.Unescape(govde), StringComparison.Ordinal);

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, await kontrol.IsCalismalari.CountAsync(x => x.TenantId == k.TenantId));
    }

    /// <summary>
    /// Anayasa md.1: aynı dakika içindeki ikinci elle tetikleme KOŞMAZ ve yanıt
    /// bunu AÇIKÇA söyler. "İş çalıştı: 0 yeni bildirim" demek, koşmamış bir işi
    /// koşmuş gibi raporlamak olurdu (canlı doğrulamada yakalandı).
    /// </summary>
    [Fact]
    public async Task Ayni_dakikada_ikinci_elle_tetikleme_ATLANDI_olarak_raporlanir()
    {
        var k = YeniKiraci("PAKET6-HTTP elle atlandı");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var kod = Kuyumcu.Api.Modules.Isler.Isler.SaklamaOnizlemeIsi.IsKodu;

        var birinci = await (await istemci.PostJson($"/api/v1/isler/{kod}/calistir", new { })).Json();
        var ikinci = await (await istemci.PostJson($"/api/v1/isler/{kod}/calistir", new { })).Json();

        Assert.False(birinci.GetProperty("atlandi").GetBoolean());
        Assert.True(ikinci.GetProperty("atlandi").GetBoolean());
        Assert.Contains("zaten koştu", ikinci.GetProperty("mesaj").GetString()!, StringComparison.Ordinal);
        Assert.DoesNotContain("İş çalıştı", ikinci.GetProperty("mesaj").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bilinmeyen_is_kodu_404_doner()
    {
        var k = YeniKiraci("PAKET6-HTTP bilinmeyen iş");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/isler/BOYLE_BIR_IS_YOK/calistir", new { });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Contains("BOYLE_BIR_IS_YOK", await cevap.Metin(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Is_listesi_tanimli_isleri_ve_kosum_gecmisini_doner()
    {
        var k = YeniKiraci("PAKET6-HTTP iş listesi");

        var calistir = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/isler/{Kuyumcu.Api.Modules.Isler.Isler.SaklamaOnizlemeIsi.IsKodu}/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, calistir.StatusCode);
        var s = await calistir.Json();
        Assert.Equal("BASARILI", s.GetProperty("durum").GetString());
        Assert.False(s.GetProperty("atlandi").GetBoolean());

        var liste = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/isler")).Json();
        var kodlar = liste.GetProperty("isler").EnumerateArray()
            .Select(i => i.GetProperty("kod").GetString()).ToList();

        Assert.Contains("VADE_HATIRLATMA", kodlar);
        Assert.Contains("MASAK_SIB_SURESI", kodlar);
        Assert.Contains("SAKLAMA_ONIZLEME", kodlar);
        Assert.Equal(1, liste.GetProperty("calismalar").GetArrayLength());
        Assert.True(liste.GetProperty("calismalar")[0].GetProperty("elle").GetBoolean());

        // Elle çalıştırma denetim izine yazıldı.
        using var kontrol = f.Vt.OlusturSistem();
        Assert.True(await kontrol.DenetimKayitlari.AnyAsync(
            d => d.TenantId == k.TenantId && d.VarlikTuru == "IS_ELLE_CALISTIRMA"));
    }

    /// <summary>Elle tetikleme oran sınırı: dakikada 3 (kullanıcı başına).</summary>
    [Fact]
    public async Task Elle_calistirma_dakikada_3_ile_sinirlidir()
    {
        var k = YeniKiraci("PAKET6-HTTP oran sınırı");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var kod = Kuyumcu.Api.Modules.Isler.Isler.MasakSibSuresiIsi.IsKodu;

        var kodlar = new List<HttpStatusCode>();
        for (var n = 0; n < 4; n++)
            kodlar.Add((await istemci.PostJson($"/api/v1/isler/{kod}/calistir", new { })).StatusCode);

        Assert.Equal(3, kodlar.Count(c => c == HttpStatusCode.OK));
        Assert.Equal(HttpStatusCode.TooManyRequests, kodlar[3]);
    }
}
