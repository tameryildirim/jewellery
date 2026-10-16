using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 41 — MASAK KAPISININ BANKA HAREKETLERİNE YAYILMASI [HTTP, gerçek MySQL]
//
// Kapı bugüne kadar YALNIZ defter hareketlerini görüyordu: aynı müşteriyle aynı
// gün 100.000 TL satış + 100.000 TL havale yapan biri 200.000 TL'ye ulaştığı
// hâlde kimlik tespiti tetiklenmiyordu (BankaModulu.cs'te 'masak' kelimesi
// 0 kez geçiyordu).
//
// ⚠ K41-1 (Mehmet, şartlı veto): kümülatif ölçüm SUNUCU SAATİYLE yapılır ve
//   banka kümesi `olusturuldu` üzerinden okunur. Dekont tarihi KULLANICI
//   girdisidir; kümülatif ona bağlansaydı "dün" + "bugün" tarihli iki dekontla
//   eşik hiç tetiklenmeden aşılırdı — eşik bölme (structuring) yolunu kapının
//   kendisi açardı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class MasakBankaTestleri(HttpFikstur f)
{
    // PAKET MASAK-370: testler GERÇEK saatle (07.10.2026 ve sonrası) koşar. md.5/1-b kimlik eşiği
    // artık 370.000 (RG 07.10.2026/33393); kimliği bugün FİİLEN tetikleyen tutar ise amaç beyanı
    // kademesidir: 200.000 (TEYİTSİZ, kurucu md.15 (a) — mali müşavir cevabına kadar aynen).
    // Kapının "eşik üstü kimliksiz → 400" davranışı bu en düşük tetikleyiciyle sınanır.
    private const decimal Esik = 200_000m;

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static string Iban() => TestIban.Uret();

    private async Task<string> HesapAc(Kiraci k)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari hesap", iban = Iban(), paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> Dekont(
        Kiraci k, string hesapId, decimal tutar, string? cariId = null,
        object? masak = null, string yon = "GELEN", string? tarih = null, string rol = "SAHIP")
        => f.Ortam.Istemci(k, rol).PostJson($"/api/v1/bankalar/{hesapId}/hareket", new
        {
            yon,
            tutar,
            karsiTarafAd = "Ali Veli",
            cariId,
            tarih,
            masak,
        });

    /// <summary>Tam kimlik + beyan bloğu (gövde alan adları MasakBilgiDto ile birebir).</summary>
    private static object TamKimlik(string? fonKaynagi = "Maaş birikimi") => new
    {
        belgeTuru = "TC_KIMLIK",
        belgeNo = "10000000146",
        adSoyad = "Ayşe Demir",
        dogumTarihi = "1985-04-12",
        uyruk = "TUR",
        adres = "İstanbul",
        islemAmaci = "Yatırım",
        aciklama = "Birikim değerlendirmek için havale",
        fonKaynagi,
        ekBelgeReferansi = "BELGE-2026-041",
    };

    private async Task<int> DekontSayisi(string tenantId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.BankaHareketleri.CountAsync(b => b.TenantId == tenantId);
    }

    private async Task<decimal> Bakiye(string hesapId)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.BankaHesaplari.AsNoTracking().SingleAsync(h => h.Id == hesapId)).Bakiye;
    }

    // ── 1. Eşik üstü dekont, kimlik yok → 400 ve HİÇBİR satır yazılmadı ──────

    [Fact]
    public async Task Esik_ustu_dekont_KIMLIKSIZ_400_doner_ve_hicbir_satir_yazilmaz()
    {
        var k = YeniKiraci("P41 esik red");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, Esik, k.CariId);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("MASAK", await cevap.Metin());
        Assert.Equal(0, await DekontSayisi(k.TenantId));   // Faz 1 tx ÖNCESİ
        Assert.Equal(0m, await Bakiye(hesapId));
    }

    [Fact]
    public async Task Esik_ustu_dekont_KIMLIKLI_200_doner_ve_kaynak_tur_BANKA_HAREKET_yazilir()
    {
        var k = YeniKiraci("P41 esik kabul");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, Esik, k.CariId, TamKimlik());

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var hareketId = (await cevap.Json()).GetProperty("hareketId").GetString()!;

        using var db = f.Vt.OlusturSistem();
        var tespit = await db.MasakKimlikTespitleri.AsNoTracking()
            .SingleAsync(m => m.TenantId == k.TenantId);
        Assert.Equal(MasakKaynakTuru.BankaHareket, tespit.KaynakTur);
        Assert.Equal(hareketId, tespit.HareketId);
        Assert.Equal(Esik, tespit.IslemTutariTl);          // İŞLEMİN KENDİ tutarı

        var satir = await db.BankaHareketleri.AsNoTracking().SingleAsync(b => b.Id == hareketId);
        Assert.Null(satir.HareketId);                      // elle dekont → defter bağı YOK
        Assert.Equal(Esik, await Bakiye(hesapId));
    }

    [Fact]
    public async Task GIDEN_yonde_de_esik_uygulanir_yon_ayrimi_yoktur()
    {
        var k = YeniKiraci("P41 giden");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, Esik, k.CariId, yon: "GIDEN");

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    // ── 2. Sınır: >= operatörü ───────────────────────────────────────────────

    [Fact]
    public async Task Sinir_199999_99_gecer_200000_00_reddedilir()
    {
        var k = YeniKiraci("P41 sinir");
        var hesapId = await HesapAc(k);
        var k2 = YeniKiraci("P41 sinir 2");
        var hesapId2 = await HesapAc(k2);

        Assert.Equal(HttpStatusCode.OK, (await Dekont(k, hesapId, 199_999.99m, k.CariId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Dekont(k2, hesapId2, 200_000.00m, k2.CariId)).StatusCode);
    }

    // ── 3. Beyan formu kademesi banka yolunda da işler ───────────────────────

    [Fact]
    public async Task Beyan_formu_kademesinde_fon_kaynagi_yoksa_400()
    {
        var k = YeniKiraci("P41 beyan");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, 2_000_000.01m, k.CariId, TamKimlik(fonKaynagi: null));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // ⚠ Türkçe gövde AYRIŞTIRILARAK sınanır: JSON'da "İ" \u0130 olarak
        //   kaçışlanır, ham metinde alt dizge araması yanıltır.
        Assert.Contains("beyan", (await cevap.Json()).GetProperty("message").GetString()!);
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    // ── 4. BAĞLANTILI İŞLEM: defter + banka aynı kümülatifte ─────────────────

    [Fact]
    public async Task Satis_100000_arti_dekont_100000_kumulatifi_esige_tasir()
    {
        var k = YeniKiraci("P41 caprazlama");
        var hesapId = await HesapAc(k);

        using (var db = f.Vt.Olustur(k))
        {
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).Satis(
                new MSatis.SatisOlusturDto
                {
                    CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 100_000m,
                    OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
                }, k.Islem);
        }

        var cevap = await Dekont(k, hesapId, 100_000m, k.CariId);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var metin = await cevap.Metin();
        Assert.Contains("MASAK", metin);
        Assert.Contains("200.000,00", metin);              // kümülatif mesajda
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    [Fact]
    public async Task Dekont_100000_arti_dekont_100000_ikinci_istek_400()
    {
        var k = YeniKiraci("P41 iki dekont");
        var hesapId = await HesapAc(k);

        Assert.Equal(HttpStatusCode.OK, (await Dekont(k, hesapId, 100_000m, k.CariId)).StatusCode);
        var ikinci = await Dekont(k, hesapId, 100_000m, k.CariId);

        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        Assert.Equal(1, await DekontSayisi(k.TenantId));   // satır sayısı 1'de kaldı
        Assert.Equal(100_000m, await Bakiye(hesapId));
    }

    /// <summary>
    /// ⚠ K41-1 KANITI: dekont tarihi geçmişe verilebilir. Kümülatif SUNUCU
    /// SAATİYLE (olusturuldu) ölçülmezse "dün" + "bugün" tarihli iki dekontla
    /// eşik hiç tetiklenmez — eşik bölme (structuring).
    /// </summary>
    [Fact]
    public async Task DUN_tarihli_100000_arti_BUGUN_tarihli_100000_ikinci_istek_400()
    {
        var k = YeniKiraci("P41 K41-1 gecmis tarih");
        var hesapId = await HesapAc(k);

        var dun = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        Assert.Equal(HttpStatusCode.OK,
            (await Dekont(k, hesapId, 100_000m, k.CariId, tarih: dun)).StatusCode);

        var ikinci = await Dekont(k, hesapId, 100_000m, k.CariId);

        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        Assert.Contains("200.000,00", await ikinci.Metin());
        Assert.Equal(1, await DekontSayisi(k.TenantId));
    }

    [Fact]
    public async Task Ileri_tarihli_dekont_400_doner()
    {
        var k = YeniKiraci("P41 ileri tarih");
        var hesapId = await HesapAc(k);

        var yarin = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var cevap = await Dekont(k, hesapId, 1_000m, k.CariId, tarih: yarin);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // PAKET GOC-A: yarının GÜNÜ artık K8a işlem tarihi kuralıyla (ekranla AYNI sözlük metni) reddedilir;
        // aynı gün içindeki 5 dk'lık ileri saat eski kuralla ("İleri tarihli dekont") reddedilmeye devam eder.
        Assert.Contains("ileri olamaz",
            (await cevap.Json()).GetProperty("message").GetString()!);
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    /// <summary>
    /// Çift sayma YAPISAL olarak imkânsız: defterle eşleşmiş (hareket_id dolu)
    /// bir banka satırı kümülatife GİRMEZ — o para hareketler tarafında sayılır.
    /// </summary>
    [Fact]
    public async Task Hareketle_eslesmis_dekont_kumulatife_IKINCI_KEZ_sayilmaz()
    {
        var k = YeniKiraci("P41 cift sayma");
        var hesapId = await HesapAc(k);

        using (var db = f.Vt.Olustur(k))
        {
            db.BankaHareketleri.Add(new BankaHareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, BankaHesapId = hesapId,
                Yon = "GELEN", Tutar = 100_000m, ParaBirimi = "TRY", CariId = k.CariId,
                HareketId = Kimlik.Ulid(),          // defterle EŞLEŞMİŞ satır
                Durum = "ESLESTI", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // Kendi tutarı 100.000; eşleşmiş satır sayılmazsa kümülatif 100.000 → geçer.
        var cevap = await Dekont(k, hesapId, 100_000m, k.CariId);

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    // ── 5. Cari yoksa kümülatif kurulamaz; tek işlem kuralı işler ────────────

    [Fact]
    public async Task Cari_verilmeden_esik_ustu_dekont_yine_400()
    {
        var k = YeniKiraci("P41 carisiz esik");
        var hesapId = await HesapAc(k);

        Assert.Equal(HttpStatusCode.BadRequest, (await Dekont(k, hesapId, Esik)).StatusCode);
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    [Fact]
    public async Task Cari_verilmeden_esik_alti_dekont_200()
    {
        var k = YeniKiraci("P41 carisiz esik alti");
        var hesapId = await HesapAc(k);

        Assert.Equal(HttpStatusCode.OK, (await Dekont(k, hesapId, 100_000m)).StatusCode);
        Assert.Equal(1, await DekontSayisi(k.TenantId));
    }

    // ── 6. Kiracı izolasyonu ve rol ──────────────────────────────────────────

    /// <summary>REGRESYON: yabancı kiracının carisiyle dekont → 404 (elle TenantId şartı).</summary>
    [Fact]
    public async Task Yabanci_kiracinin_carisiyle_dekont_404()
    {
        var k = YeniKiraci("P41 kiraci A");
        var yabanci = YeniKiraci("P41 kiraci B");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, 1_000m, yabanci.CariId);

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal(0, await DekontSayisi(k.TenantId));
    }

    [Fact]
    public async Task Dekont_ucu_TEZGAHTAR_ile_403()
    {
        var k = YeniKiraci("P41 rol");
        var hesapId = await HesapAc(k);

        var cevap = await Dekont(k, hesapId, 1_000m, k.CariId, rol: "TEZGAHTAR");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── 7. Kapsam dışı olduğu BİLİNÇLİ karar: faturaya dönüştürme ────────────

    /// <summary>
    /// Faturaya dönüştürmede PARA EL DEĞİŞTİRMEZ; kapı orada çalışmaz. Bu test
    /// kararı KİLİTLER: birisi "unutulmuş" sanıp MASAK eklerse kırmızıya döner.
    /// </summary>
    [Fact]
    public async Task Faturaya_donusturme_MASAK_ISTEMEZ_bilincli_kapsam_disi()
    {
        var k = YeniKiraci("P41 fatura");
        var hesapId = await HesapAc(k);

        var dekont = await Dekont(k, hesapId, Esik, k.CariId, TamKimlik());
        Assert.Equal(HttpStatusCode.OK, dekont.StatusCode);
        var hareketId = (await dekont.Json()).GetProperty("hareketId").GetString()!;

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/hareketler/{hareketId}/faturaya-donustur",
            new { cariId = k.CariId, kdvOran = 0.20m });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }
}
