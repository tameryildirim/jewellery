using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Telemetri;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-28 — F-4 KİRACI ETİKETLİ TELEMETRİ + DESTEK DAKİKASI (R-2).
//
// Bu dosya üç hattı sınar:
//   (1) TOPLAYICI — saf mantık: pencere yuvarlama, kiracısız istek, geri koyma.
//   (2) YAZICI     — bellekten veritabanına taşıma, upsert, bilinmeyen kiracı.
//   (3) UÇLAR      — kiracı salt okur; operatör destek dakikası yazar/düzeltir.
//
// ⚠ MEHMET KOŞULU ayrı bir yapısal testle kilitlenir: telemetri varlıklarında
//   kişisel veri kolonu DOĞAMAZ (yansımayla ölçülür, gözle değil).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P28TelemetriTestleri(HttpFikstur fikstur)
{
    private Kiraci Kur(string ad)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"P28-{ad}-{Guid.NewGuid():N}"[..24]);
    }

    private TelemetriToplayici Toplayici =>
        fikstur.Ortam.Services.GetRequiredService<TelemetriToplayici>();

    private async Task<int> Bosalt()
    {
        using var kapsam = fikstur.Ortam.Services.CreateScope();
        return await kapsam.ServiceProvider.GetRequiredService<TelemetriYazici>().Bosalt();
    }

    private List<TelemetriOlcumu> Olcumler(string tenantId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return [.. db.Set<TelemetriOlcumu>().AsNoTracking()
            .Where(o => o.TenantId == tenantId).OrderBy(o => o.OlcumSaati)];
    }

    private List<DestekDakikasi> Destekler(string tenantId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return [.. db.Set<DestekDakikasi>().AsNoTracking()
            .Where(d => d.TenantId == tenantId).OrderBy(d => d.Olusturuldu)];
    }

    private int DenetimSay(string tenantId, string parca)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.DenetimKayitlari.AsNoTracking()
            .Count(d => d.TenantId == tenantId && d.Detay != null && d.Detay.Contains(parca));
    }

    private HttpClient Sistem() =>
        fikstur.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "asikin-qa");

    private async Task<string> DestekEkle(string tenantId, int dakika = 30, string konu = DestekKonulari.Egitim)
    {
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{tenantId}/destek-dakikasi",
            new { dakika, kanal = DestekKanallari.Telefon, konu, aciklama = "P28 testi" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (1) TOPLAYICI — SAF MANTIK
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Pencere UTC saatine yuvarlanır: dakika/saniye sıfırlanır.</summary>
    [Fact]
    public void Pencere_saate_yuvarlanir()
    {
        var an = new DateTime(2026, 9, 22, 13, 47, 31, DateTimeKind.Utc);
        var p = TelemetriToplayici.Pencere(an);
        Assert.Equal(new DateTime(2026, 9, 22, 13, 0, 0, DateTimeKind.Utc), p);
        Assert.Equal(DateTimeKind.Utc, p.Kind);
    }

    /// <summary>
    /// KİRACISIZ istek ÖLÇÜLMEZ: ortak bir "bilinmeyen" kovası kiracılar arası
    /// karışma demektir ve kapasite paydasını yanlış hesaplatır.
    /// </summary>
    [Fact]
    public void Kiracisiz_istek_OLCULMEZ()
    {
        var t = new TelemetriToplayici();
        t.Kaydet(null, DateTime.UtcNow, 10, 100, false);
        t.Kaydet("", DateTime.UtcNow, 10, 100, false);
        t.Kaydet("   ", DateTime.UtcNow, 10, 100, false);
        Assert.Equal(0, t.BekleyenPencere);
        Assert.Empty(t.BosaltVeAl());
    }

    /// <summary>Aynı saatteki istekler TEK pencerede toplanır (istek başına satır yok).</summary>
    [Fact]
    public void Ayni_saat_TEK_pencerede_toplanir()
    {
        var t = new TelemetriToplayici();
        var an = new DateTime(2026, 9, 22, 8, 5, 0, DateTimeKind.Utc);
        t.Kaydet("K1", an, 10, 100, false);
        t.Kaydet("K1", an.AddMinutes(50), 20, 200, true);

        var b = Assert.Single(t.BosaltVeAl());
        Assert.Equal(2, b.IstekSayisi);
        Assert.Equal(1, b.HataSayisi);
        Assert.Equal(30, b.IslemMs);
        Assert.Equal(300, b.YanitBayt);
        Assert.Equal(0, t.BekleyenPencere);   // BosaltVeAl kutuyu boşaltır
    }

    /// <summary>İki kiracı AYRI pencereye yazılır — etiketler karışmaz.</summary>
    [Fact]
    public void Iki_kiraci_AYRI_pencereye_yazilir()
    {
        var t = new TelemetriToplayici();
        var an = new DateTime(2026, 9, 22, 8, 5, 0, DateTimeKind.Utc);
        t.Kaydet("K1", an, 10, 100, false);
        t.Kaydet("K2", an, 5, 50, false);

        var hepsi = t.BosaltVeAl();
        Assert.Equal(2, hepsi.Count);
        Assert.Equal(10, hepsi.Single(x => x.TenantId == "K1").IslemMs);
        Assert.Equal(5, hepsi.Single(x => x.TenantId == "K2").IslemMs);
    }

    /// <summary>Yazılamayan birikim geri konurken TOPLANIR, üzerine yazılmaz.</summary>
    [Fact]
    public void GeriKoy_birikimi_TOPLAR()
    {
        var t = new TelemetriToplayici();
        var an = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
        t.Kaydet("K1", an, 10, 100, false);
        var alinan = t.BosaltVeAl();

        t.Kaydet("K1", an, 7, 70, true);       // tur arasında gelen yeni istek
        t.GeriKoy(alinan);                      // yazılamayan birikim iade

        var b = Assert.Single(t.BosaltVeAl());
        Assert.Equal(2, b.IstekSayisi);
        Assert.Equal(17, b.IslemMs);
        Assert.Equal(170, b.YanitBayt);
        Assert.Equal(1, b.HataSayisi);
    }

    /// <summary>Negatif süre/bayt sayaca 0 olarak girer (sayaç geri saymaz).</summary>
    [Fact]
    public void Negatif_olcum_sayaci_geri_SAYDIRMAZ()
    {
        var t = new TelemetriToplayici();
        t.Kaydet("K1", DateTime.UtcNow, -50, -500, false);
        var b = Assert.Single(t.BosaltVeAl());
        Assert.Equal(0, b.IslemMs);
        Assert.Equal(0, b.YanitBayt);
        Assert.Equal(1, b.IstekSayisi);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (2) MEHMET KOŞULU — TELEMETRİ KİŞİSEL VERİ TAŞIMAZ (yapısal)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Otomatik telemetri varlığına kişisel veri kolonu EKLENEMEZ. Gözle değil
    /// YANSIMAYLA ölçülür: yeni bir kolon eklenirse bu test kırmızıya döner.
    /// </summary>
    [Fact]
    public void Telemetri_olcumu_KISISEL_VERI_tasimaz()
    {
        string[] yasak =
        [
            "ad", "soyad", "adsoyad", "eposta", "email", "telefon", "tckn", "vkn",
            "adres", "ip", "ipadresi", "kullanici", "kullaniciid", "kullaniciadi",
            "yol", "path", "sorgu", "query", "govde", "body", "baslik", "header",
            "token", "jeton", "parola", "sifre", "cari", "urun", "musteri",
        ];
        var alanlar = typeof(TelemetriOlcumu).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();

        // Beklenen alan kümesi TAM yazılır: yeni bir alan sessizce eklenemez.
        Assert.Equal(
            ["guncellendi", "hatasayisi", "id", "islemms", "isteksayisi", "olcumsaati",
             "olusturuldu", "tenantid", "yanitbayt"],
            alanlar.OrderBy(x => x, StringComparer.Ordinal).ToList());

        foreach (var y in yasak)
            Assert.DoesNotContain(y, alanlar);
    }

    /// <summary>Birikim kaydı da yalnız sayaç taşır (bellekte bile içerik yok).</summary>
    [Fact]
    public void Birikim_kaydi_yalniz_sayac_tasir()
    {
        var alanlar = typeof(TelemetriBirikimi).GetProperties()
            .Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(
            ["HataSayisi", "IslemMs", "IstekSayisi", "Saat", "TenantId", "YanitBayt"],
            alanlar);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (3) YAZICI — BELLEKTEN VERİTABANINA
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Yazici_birikimi_veritabanina_YAZAR()
    {
        var kiraci = Kur("yaz");
        var an = DateTime.UtcNow;
        Toplayici.Kaydet(kiraci.TenantId, an, 25, 400, false);
        Toplayici.Kaydet(kiraci.TenantId, an, 15, 100, true);

        var yazilan = await Bosalt();

        Assert.True(yazilan >= 1);
        var satir = Assert.Single(Olcumler(kiraci.TenantId));
        Assert.Equal(2, satir.IstekSayisi);
        Assert.Equal(1, satir.HataSayisi);
        Assert.Equal(40, satir.IslemMs);
        Assert.Equal(500, satir.YanitBayt);
        Assert.Equal(TelemetriToplayici.Pencere(an), satir.OlcumSaati);
    }

    /// <summary>
    /// İkinci tur AYNI pencereye gelirse sayaçlar TOPLANIR. Üzerine yazmak,
    /// turlar arası isteklerin sessizce kaybolması demekti.
    /// </summary>
    [Fact]
    public async Task Yazici_ayni_pencereyi_ikinci_turda_TOPLAR()
    {
        var kiraci = Kur("ups");
        var an = DateTime.UtcNow;

        Toplayici.Kaydet(kiraci.TenantId, an, 10, 100, false);
        await Bosalt();
        Toplayici.Kaydet(kiraci.TenantId, an, 30, 300, false);
        await Bosalt();

        var satir = Assert.Single(Olcumler(kiraci.TenantId));
        Assert.Equal(2, satir.IstekSayisi);
        Assert.Equal(40, satir.IslemMs);
        Assert.Equal(400, satir.YanitBayt);
    }

    /// <summary>Kiracı tablosunda karşılığı olmayan etiket YAZILMAZ (artık pencere).</summary>
    [Fact]
    public async Task Yazici_bilinmeyen_kiraciyi_YAZMAZ()
    {
        var hayalet = "P28-YOK-" + Guid.NewGuid().ToString("N");
        Toplayici.Kaydet(hayalet, DateTime.UtcNow, 10, 10, false);

        await Bosalt();

        Assert.Empty(Olcumler(hayalet));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (4) ARA KATMAN — GERÇEK İSTEK ÖLÇÜLÜR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gerçek bir HTTP isteği kiracı etiketiyle sayılır. Ara katmanın boru
    /// hattındaki yeri ancak HTTP testinden görülür (servis testi göremez).
    /// </summary>
    [Fact]
    public async Task Gercek_istek_kiraci_etiketiyle_OLCULUR()
    {
        var kiraci = Kur("arac");
        await Bosalt();                                  // kutuyu temizle
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.GetAsync("/api/v1/telemetri");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        await Bosalt();
        var satir = Assert.Single(Olcumler(kiraci.TenantId));
        Assert.True(satir.IstekSayisi >= 1);
        Assert.True(satir.YanitBayt > 0, "yanıt baytı ölçülmedi");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (5) KİRACI UCU — SALT OKUNUR, KENDİ VERİSİ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Telemetri ucu SAHİP kapısı arkasındadır — MÜDÜR geçemez.</summary>
    [Fact]
    public async Task Telemetri_ucu_MUDURu_reddeder()
    {
        var kiraci = Kur("rol");
        var istemci = fikstur.Ortam.Istemci(kiraci, "MUDUR");

        var cevap = await istemci.GetAsync("/api/v1/telemetri");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    /// <summary>Kiracı BAŞKA kiracının ölçümünü GÖREMEZ (etiket sızıntısı yok).</summary>
    [Fact]
    public async Task Kiraci_baskasinin_olcumunu_GOREMEZ()
    {
        var a = Kur("izoa");
        var b = Kur("izob");
        Toplayici.Kaydet(b.TenantId, DateTime.UtcNow, 999, 9999, false);
        await Bosalt();

        var cevap = await fikstur.Ortam.Istemci(a, "SAHIP").GetAsync("/api/v1/telemetri?gun=30");
        var json = await cevap.Json();

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var kullanim = json.GetProperty("kullanim");
        Assert.Equal(a.TenantId, kullanim.GetProperty("tenantId").GetString());
        Assert.True(kullanim.GetProperty("islemMs").GetInt64() < 999);
    }

    /// <summary>
    /// Ölçüm YOKSA sıfır UYDURULMAZ: olcumVarMi=false ile "veri yok" denir
    /// (FAIL-CLOSED 1 — rakam ölçülmeden üretilmez).
    /// </summary>
    [Fact]
    public async Task Olcum_yoksa_olcumVarMi_FALSE_doner()
    {
        var kiraci = Kur("bos");
        var cevap = await fikstur.Ortam.Istemci(kiraci, "SAHIP").GetAsync("/api/v1/telemetri?gun=1");
        var json = await cevap.Json();

        // ⚠ Mutasyon M28-9 dersi: yalnız "destek" dalını sınamak AÇIKTI —
        //   kullanım dalı `OlcumVarMi: true` sabitine çevrildiğinde test yeşil
        //   kalıyordu. "Hiç ölçülmedi" ile "0 ölçüldü" ayrımı İKİ dalda da kilitli.
        var kullanim = json.GetProperty("kullanim");
        Assert.False(kullanim.GetProperty("olcumVarMi").GetBoolean());
        Assert.Equal(0, kullanim.GetProperty("pencereSayisi").GetInt32());
        Assert.Equal(0, kullanim.GetProperty("istekSayisi").GetInt64());

        Assert.False(json.GetProperty("destek").GetProperty("olcumVarMi").GetBoolean());
        Assert.NotEqual("", json.GetProperty("degismezIpucu").GetString());
    }

    /// <summary>Gün parametresi tavana sıkıştırılır (UX-S29: sınır yanıtta yazılıdır).</summary>
    [Fact]
    public void Gun_tavana_sikistirilir()
    {
        Assert.Equal(TelemetriServisi.VarsayilanGun, TelemetriServisi.GunNormalle(null));
        Assert.Equal(TelemetriServisi.VarsayilanGun, TelemetriServisi.GunNormalle(0));
        Assert.Equal(TelemetriServisi.VarsayilanGun, TelemetriServisi.GunNormalle(-5));
        Assert.Equal(TelemetriServisi.GunTavani, TelemetriServisi.GunNormalle(99_999));
        Assert.Equal(7, TelemetriServisi.GunNormalle(7));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (6) SİSTEM UCU — KİMLİK KAPISI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Sistem telemetri ucu JWT ile AÇILMAZ (sistem anahtarı ister).</summary>
    [Fact]
    public async Task Sistem_telemetri_ucu_JWT_ile_ACILMAZ()
    {
        var kiraci = Kur("jwt");
        var cevap = await fikstur.Ortam.Istemci(kiraci, "SAHIP").GetAsync("/api/v1/sistem/telemetri");
        Assert.NotEqual(HttpStatusCode.OK, cevap.StatusCode);
    }

    /// <summary>Anahtarsız istek de AÇILMAZ.</summary>
    [Fact]
    public async Task Sistem_telemetri_ucu_anahtarsiz_ACILMAZ()
    {
        var cevap = await fikstur.Ortam.AnonimIstemci().GetAsync("/api/v1/sistem/telemetri");
        Assert.NotEqual(HttpStatusCode.OK, cevap.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (7) DESTEK DAKİKASI — DOĞAR, DÜZELİR, PASİFE ALINIR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Destek_dakikasi_eklenir_ve_IZ_birakir()
    {
        var kiraci = Kur("dest");
        var id = await DestekEkle(kiraci.TenantId, 45);

        var satir = Assert.Single(Destekler(kiraci.TenantId));
        Assert.Equal(id, satir.Id);
        Assert.Equal(45, satir.Dakika);
        Assert.True(satir.Aktif);
        Assert.Equal(DestekKaynaklari.Manuel, satir.Kaynak);
        Assert.Equal(1, DenetimSay(kiraci.TenantId, $"{TelemetriSistemServisi.IzDamgasi}: EKLENDI"));
    }

    /// <summary>PATCH sözleşmesi: GÖNDERİLMEYEN alan KORUNUR.</summary>
    [Fact]
    public async Task Patch_gonderilmeyen_alani_KORUR()
    {
        var kiraci = Kur("patc");
        var id = await DestekEkle(kiraci.TenantId, 45);

        var cevap = await Sistem().PatchJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}",
            new { dakika = 60 });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var satir = Assert.Single(Destekler(kiraci.TenantId));
        Assert.Equal(60, satir.Dakika);
        Assert.Equal("P28 testi", satir.Aciklama);          // KORUNDU
        Assert.Equal(DestekKanallari.Telefon, satir.Kanal); // KORUNDU
        Assert.Equal(DestekKonulari.Egitim, satir.Konu);    // KORUNDU
    }

    /// <summary>PATCH sözleşmesi: açıkça null gönderilen alan SİLİNİR.</summary>
    [Fact]
    public async Task Patch_null_gonderilen_alani_SILER()
    {
        var kiraci = Kur("null");
        var id = await DestekEkle(kiraci.TenantId);

        var cevap = await Sistem().PatchJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}",
            new { aciklama = (string?)null });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Null(Assert.Single(Destekler(kiraci.TenantId)).Aciklama);
    }

    /// <summary>Boş PATCH sessizce başarılı olmaz: "değişiklik gönderilmedi" der.</summary>
    [Fact]
    public async Task Bos_patch_sessizce_BASARILI_olmaz()
    {
        var kiraci = Kur("bosp");
        var id = await DestekEkle(kiraci.TenantId);

        var cevap = await Sistem().PatchJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}", new { });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    /// <summary>
    /// DELETE PASİFE ALIR: satır DURUR (hard delete yok) ve geri dönüş yolu
    /// (aktif=true) çalışır.
    /// </summary>
    [Fact]
    public async Task Delete_PASIFE_alir_satir_durur_ve_geri_alinir()
    {
        var kiraci = Kur("pasi");
        var id = await DestekEkle(kiraci.TenantId);

        var sil = await Sistem().DeleteAsync(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        Assert.False(Assert.Single(Destekler(kiraci.TenantId)).Aktif);   // SATIR DURUYOR

        var geri = await Sistem().PatchJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}",
            new { aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);
        Assert.True(Assert.Single(Destekler(kiraci.TenantId)).Aktif);
    }

    /// <summary>Pasif kayıt ÖLÇÜME SAYILMAZ (birim ekonomi pasifi toplamaz).</summary>
    [Fact]
    public async Task Pasif_kayit_olcume_SAYILMAZ()
    {
        var kiraci = Kur("olcp");
        var id = await DestekEkle(kiraci.TenantId, 90);

        var once = await Sistem().GetAsync($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/telemetri");
        Assert.Equal(90, (await once.Json()).GetProperty("destek").GetProperty("toplamDakika").GetInt64());

        await Sistem().DeleteAsync($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}");

        var sonra = await Sistem().GetAsync($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/telemetri");
        var destek = (await sonra.Json()).GetProperty("destek");
        Assert.Equal(0, destek.GetProperty("toplamDakika").GetInt64());
        Assert.False(destek.GetProperty("olcumVarMi").GetBoolean());
    }

    /// <summary>Pasife alınan kayıt LİSTEDE kalır — düzeltme yolu görünür olmalı.</summary>
    [Fact]
    public async Task Pasif_kayit_LISTEDE_kalir()
    {
        var kiraci = Kur("list");
        var id = await DestekEkle(kiraci.TenantId);
        await Sistem().DeleteAsync($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi/{id}");

        var json = await (await Sistem().GetAsync(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi")).Json();

        var kayitlar = json.GetProperty("kayitlar").EnumerateArray().ToList();
        Assert.Single(kayitlar);
        Assert.False(kayitlar[0].GetProperty("aktif").GetBoolean());
        Assert.Equal(TelemetriSistemServisi.DestekTavani, json.GetProperty("sinir").GetInt32());
    }

    // ── Doğrulama kapıları ───────────────────────────────────────────────────

    [Fact]
    public async Task Gecersiz_kanal_400_doner()
    {
        var kiraci = Kur("kanl");
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi",
            new { dakika = 10, kanal = "GUVERCIN", konu = DestekKonulari.Hata });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Empty(Destekler(kiraci.TenantId));
    }

    [Fact]
    public async Task Gecersiz_konu_400_doner()
    {
        var kiraci = Kur("konu");
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi",
            new { dakika = 10, kanal = DestekKanallari.Yazili, konu = "SOHBET" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Empty(Destekler(kiraci.TenantId));
    }

    [Fact]
    public async Task Dakika_tavani_asilirsa_400_doner()
    {
        var kiraci = Kur("tavn");
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi",
            new { dakika = TelemetriSistemServisi.DakikaTavani + 1,
                  kanal = DestekKanallari.Yerinde, konu = DestekKonulari.Kurulum });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Empty(Destekler(kiraci.TenantId));
    }

    [Fact]
    public async Task Sifir_dakika_400_doner()
    {
        var kiraci = Kur("sfr");
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi",
            new { dakika = 0, kanal = DestekKanallari.Uzaktan, konu = DestekKonulari.Veri });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Empty(Destekler(kiraci.TenantId));
    }

    /// <summary>Gelecek tarihli destek kaydı REDDEDİLİR (ölçüm geriye dönüktür).</summary>
    [Fact]
    public async Task Gelecek_tarihli_kayit_400_doner()
    {
        var kiraci = Kur("gel");
        var cevap = await Sistem().PostJson(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/destek-dakikasi",
            new { tarih = DateTime.UtcNow.AddDays(3), dakika = 10,
                  kanal = DestekKanallari.Telefon, konu = DestekKonulari.Hata });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Empty(Destekler(kiraci.TenantId));
    }

    /// <summary>Bilinmeyen kiracıya destek kaydı açılamaz.</summary>
    [Fact]
    public async Task Bilinmeyen_kiraciya_destek_kaydi_404()
    {
        var cevap = await Sistem().PostJson(
            "/api/v1/sistem/kiracilar/P28-YOK-0000/destek-dakikasi",
            new { dakika = 10, kanal = DestekKanallari.Telefon, konu = DestekKonulari.Hata });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    /// <summary>Başka kiracının kaydı, yol kiracısıyla eşleşmezse BULUNAMAZ.</summary>
    [Fact]
    public async Task Baska_kiracinin_kaydi_bu_yoldan_DUZELTILEMEZ()
    {
        var a = Kur("carpa");
        var b = Kur("carpb");
        var id = await DestekEkle(a.TenantId);

        var cevap = await Sistem().PatchJson(
            $"/api/v1/sistem/kiracilar/{b.TenantId}/destek-dakikasi/{id}", new { dakika = 5 });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal(30, Assert.Single(Destekler(a.TenantId)).Dakika);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (8) KAPASİTE ÖZETİ — F-8 PAYDASI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kiracı BAŞINA ortalama yalnız ÖLÇÜMÜ OLAN kiracılara bölünür: hiç istek
    /// görmemiş kiracı paydayı şişirip rakamı iyi gösteremez (Anayasa md.1).
    /// </summary>
    [Fact]
    public async Task Kapasite_ozeti_olcumsuz_kiraciyi_paydaya_KATMAZ()
    {
        var kiraci = Kur("kap");
        Toplayici.Kaydet(kiraci.TenantId, DateTime.UtcNow, 120, 1200, false);
        await Bosalt();

        var json = await (await Sistem().GetAsync("/api/v1/sistem/telemetri?gun=30")).Json();

        var kiraciSayisi = json.GetProperty("kiraciSayisi").GetInt32();
        var olculen = json.GetProperty("olculenKiraciSayisi").GetInt32();
        Assert.True(json.GetProperty("olcumVarMi").GetBoolean());
        Assert.True(olculen >= 1);
        Assert.True(olculen <= kiraciSayisi);
        // Payda ölçülen kiracıdır: toplam / ölçülen = kiracı başı.
        var toplam = json.GetProperty("toplamIslemMs").GetInt64();
        var basi = json.GetProperty("kiraciBasiIslemMs").GetDouble();
        Assert.Equal(Math.Round((double)toplam / olculen, 2), basi, 2);
        Assert.NotEqual("", json.GetProperty("kisit").GetString());
    }

    /// <summary>Kapasite özeti depolama vekilini de ÖLÇER (tablolar yazılıdır).</summary>
    [Fact]
    public async Task Kapasite_ozeti_depolama_satirini_OLCER()
    {
        var kiraci = Kur("dep");
        var json = await (await Sistem().GetAsync(
            $"/api/v1/sistem/kiracilar/{kiraci.TenantId}/telemetri")).Json();

        var depolama = json.GetProperty("depolama");
        Assert.True(depolama.GetProperty("toplamSatir").GetInt64() > 0);   // ürün/stok/cari kuruldu
        Assert.True(depolama.GetProperty("tablolar").GetProperty("urunler").GetInt64() >= 1);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// P-28 DENETİM KAPISI — SAYAÇLAR SÖKÜLEMEZ (mekanizma kuralı, planın 3. ilkesi)
//
// Sayacı `denetim.sh`'a koymak yetmez: sayaç sessizce silinirse ya da tavanı
// yükseltilirse kural kağıt üstünde kalır. Bu sınıf sayacın VARLIĞINI, ÖLÇTÜĞÜ
// DEĞERİ ve TAVANIN YÜKSELMEDİĞİNİ kilitler. Saf testtir, veritabanı istemez.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class P28DenetimKapisiTestleri
{
    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DepoKoku() => Directory.GetParent(DotnetKoku())!.FullName;
    private static string Denetim() => File.ReadAllText(Path.Combine(DotnetKoku(), "denetim.sh"));
    private static string F8Yolu() => Path.Combine(
        DepoKoku(), "dokumanlar", "02-mimari", "F-8-BIRIM-EKONOMI-2026-09-16.md");

    /// <summary>Yorum satırları sökülmüş kaynak (denetim.sh'ın perl ölçümüyle aynı kural).</summary>
    private static string KodSatirlari(string yol)
    {
        var satirlar = File.ReadAllLines(yol)
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
        return string.Join('\n', satirlar);
    }

    private static string TelemetriDizini() =>
        Path.Combine(DotnetKoku(), "Kuyumcu.Api", "Modules", "Telemetri");

    // ── 1) Sayaç blokları YERİNDE mi ────────────────────────────────────────

    [Fact]
    public void P28_sayac_blogu_denetim_sh_icinde_VARDIR()
    {
        var d = Denetim();
        Assert.Contains("# >>> P-28", d, StringComparison.Ordinal);
        Assert.Contains("# <<< P-28", d, StringComparison.Ordinal);
        Assert.Contains("P-28: TELEMETRIDE_KISISEL_VERI", d, StringComparison.Ordinal);
        Assert.Contains("P-28: F8_OLCULMEMIS_SATIR", d, StringComparison.Ordinal);
        Assert.Contains("P-28: TELEMETRI_ELLE_F8_RAKAMI", d, StringComparison.Ordinal);
    }

    // ── 2) TELEMETRIDE_KISISEL_VERI = 0 (BK-12 · Mehmet vetosu) ─────────────

    /// <summary>
    /// OTOMATİK toplama yolunda (ara katman + toplayıcı + yazıcı) istek içeriğine
    /// dokunan TEK bir ifade bile bulunmamalıdır. Ölçüm denetim.sh ile AYNIDIR.
    /// </summary>
    [Theory]
    [InlineData("TelemetriAraci.cs")]
    [InlineData("TelemetriToplayici.cs")]
    [InlineData("TelemetriYazici.cs")]
    public void Otomatik_toplama_yolu_ISTEK_ICERIGINE_bakmaz(string dosya)
    {
        var kod = KodSatirlari(Path.Combine(TelemetriDizini(), dosya));
        foreach (var yasak in new[]
                 { "Request.", "RemoteIpAddress", "ClaimTypes", "FindFirst(\"", ".Identity", "Cookies", "Headers[" })
            Assert.False(kod.Contains(yasak, StringComparison.Ordinal),
                $"{dosya} içinde kişisel veri kapısı açılmış: '{yasak}' (BK-12 · Mehmet vetosu)");
    }

    [Fact]
    public void Telemetri_semasinda_KISISEL_VERI_kolonu_DOGAMAZ()
    {
        var kod = KodSatirlari(Path.Combine(TelemetriDizini(), "TelemetriVarliklari.cs"));
        foreach (var yasak in new[]
                 { "\"ip\"", "\"ip_adresi\"", "\"kullanici\"", "\"kullanici_id\"", "\"email\"",
                   "\"eposta\"", "\"telefon\"", "\"yol\"", "\"path\"", "\"url\"", "\"sorgu\"",
                   "\"basliklar\"", "\"govde\"", "\"cari_id\"", "\"urun_id\"" })
            Assert.False(kod.Contains($"Column({yasak})", StringComparison.Ordinal),
                $"Telemetri şemasına kişisel veri kolonu girmiş: Column({yasak})");
    }

    // ── 3) F8_OLCULMEMIS_SATIR — tavan YALNIZ AZALIR, kilit YAZILI ──────────

    [Fact]
    public void F8_olculmemis_satir_tavani_gercek_olcume_esit_ve_YUKSELMEZ()
    {
        var d = Denetim();
        var m = System.Text.RegularExpressions.Regex.Match(
            d, @"^P28_F8_TAVAN=(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
        Assert.True(m.Success, "denetim.sh'ta P28_F8_TAVAN sayacı YOK — çırçır sökülmüş.");

        var tavan = int.Parse(m.Groups[1].Value);
        // Tavan 2'de mühürlüdür: P-25 (k6) ve P-32 (pilot) rakamı getirince DÜŞER.
        Assert.True(tavan <= 2, $"F8_OLCULMEMIS_SATIR tavanı YÜKSELTİLMİŞ: {tavan} (>2)");

        var gercek = File.ReadAllLines(F8Yolu()).Count(l => l.Contains("| OLCULMEDI |", StringComparison.Ordinal));
        Assert.True(gercek <= tavan,
            $"F-8'de ölçülmemiş satır sayısı tavanı aştı: {gercek} > {tavan}");
    }

    [Fact]
    public void Olculmemis_satir_varken_FIYAT_KILIDI_belgede_YAZILIDIR()
    {
        var metin = File.ReadAllText(F8Yolu());
        var gercek = File.ReadAllLines(F8Yolu()).Count(l => l.Contains("| OLCULMEDI |", StringComparison.Ordinal));
        if (gercek == 0) return; // Faz B kapandıysa kilit gerekmez.

        Assert.Contains("FAIL-CLOSED 1 AKTİF", metin, StringComparison.Ordinal);
    }

    // ── 4) TELEMETRI_ELLE_F8_RAKAMI = 0 (K-16 · fiyat kurucudadır) ──────────

    [Fact]
    public void Telemetri_koduna_PARA_kolonu_girmez()
    {
        foreach (var dosya in Directory.GetFiles(TelemetriDizini(), "*.cs"))
        {
            var kod = KodSatirlari(dosya);
            foreach (var yasak in new[] { "ucret", "fiyat", "tutar", "maliyet" })
                Assert.False(
                    System.Text.RegularExpressions.Regex.IsMatch(kod, $@"Column\(""[^""]*{yasak}[^""]*""\)"),
                    $"{Path.GetFileName(dosya)}: telemetriye para kolonu girmiş ('{yasak}') — F-8 rakamı K-16 ile kurucudadır");
        }
    }

    // ── 5) Düzeltme politikası haritası — iki varlık DAMGALI ────────────────

    [Fact]
    public void Telemetri_varliklari_DUZELTME_POLITIKASINDA_damgalidir()
    {
        var harita = Kuyumcu.Api.Modules.Meta.DuzeltmePolitikasi.Varliklar;

        Assert.True(harita.TryGetValue("TelemetriOlcumu", out var olcum));
        Assert.Equal(Kuyumcu.Api.Modules.Meta.Politikalar.SaltOkunur, olcum!.Politika);
        Assert.Equal(Kuyumcu.Api.Modules.Meta.GerekceAnahtarlari.Turetilmis, olcum.GerekceAnahtari);

        Assert.True(harita.TryGetValue("DestekDakikasi", out var destek));
        Assert.Equal(Kuyumcu.Api.Modules.Meta.Politikalar.Duzenlenebilir, destek!.Politika);
        Assert.Equal("api/v1/sistem/kiracilar/{id}/destek-dakikasi/{kayitId}", destek.HedefUc);
    }
}
