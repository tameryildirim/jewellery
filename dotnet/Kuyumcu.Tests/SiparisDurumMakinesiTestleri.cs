using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ DURUM MAKİNESİ — SAF testler (paket 3 Faz A).
//
// Veritabanı yok, HTTP yok, saat yok. Mutasyon M143 (Izinli her zaman true) bu
// dosyada ANINDA kırmızıya döner — tek katman ölçülür.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SiparisDurumMakinesiTestleri
{
    /// <summary>Tür bazında izinli geçiş tablosunun BİREBİR kendisi (spesifikasyondan).</summary>
    private static readonly Dictionary<string, Dictionary<string, string[]>> Beklenen = new()
    {
        // ⚠ QA turu 5 (YÜKSEK · paket S-3f): ONAYLANDI tek tıkla ve GERİ DÖNÜŞSÜZ
        //   oluyordu; sipariş aynı anda hem düzenlenemez hem geri alınamaz hâle
        //   geliyordu. ONAYLANDI → TASLAK geri geçişi AÇILDI (gerekçe zorunlu).
        // ⚠ KARAR K-23 (2026-09-21 · PAKET P-22): ATOLYEDE ve HAZIR'dan GERİ
        //   DÖNÜŞ AÇILDI — serbest ama DENETİMLİ (yetki + gerekçe + iz +
        //   görünürlük). Atölye siparişinde HAZIR yalnız ATOLYEDE'ye döner:
        //   malın atölyeden çıktığı iddiası ekrandan doğrulanamaz (md.1).
        [SiparisTuru.MusteriSiparisi] = new()
        {
            ["TASLAK"] = ["ONAYLANDI", "IPTAL"],
            ["ONAYLANDI"] = ["ATOLYEDE", "HAZIR", "TASLAK", "IPTAL"],
            ["ATOLYEDE"] = ["HAZIR", "ONAYLANDI", "IPTAL"],
            ["HAZIR"] = ["TESLIM_EDILDI", "ATOLYEDE", "ONAYLANDI", "IPTAL"],
            ["TESLIM_EDILDI"] = [],
            ["IPTAL"] = [],
        },
        [SiparisTuru.AtolyeSiparisi] = new()
        {
            ["TASLAK"] = ["ONAYLANDI", "IPTAL"],
            ["ONAYLANDI"] = ["ATOLYEDE", "TASLAK", "IPTAL"],
            ["ATOLYEDE"] = ["HAZIR", "ONAYLANDI", "IPTAL"],
            ["HAZIR"] = ["TESLIM_EDILDI", "ATOLYEDE", "IPTAL"],
            ["TESLIM_EDILDI"] = [],
            ["IPTAL"] = [],
        },
        [SiparisTuru.Tamir] = new()
        {
            ["TASLAK"] = ["ONAYLANDI", "IPTAL"],
            ["ONAYLANDI"] = ["ATOLYEDE", "HAZIR", "TASLAK", "IPTAL"],
            ["ATOLYEDE"] = ["HAZIR", "ONAYLANDI", "IPTAL"],
            ["HAZIR"] = ["TESLIM_EDILDI", "ATOLYEDE", "ONAYLANDI", "IPTAL"],
            ["TESLIM_EDILDI"] = [],
            ["IPTAL"] = [],
        },
    };

    [Fact]
    public void Izinli_gecis_listesi_tur_bazinda_birebir_dogrulanir()
    {
        // Üç tür × tüm durum çiftleri — 3 × 6 × 6 = 108 kombinasyon.
        foreach (var tur in SiparisTuru.Tumu)
        foreach (var mevcut in SiparisDurumu.Tumu)
        foreach (var yeni in SiparisDurumu.Tumu)
        {
            var beklenen = Beklenen[tur][mevcut].Contains(yeni);
            Assert.Equal(beklenen, SiparisDurumMakinesi.Izinli(tur, mevcut, yeni));
        }
    }

    [Fact]
    public void TESLIM_EDILDI_terminaldir_hicbir_cikis_yok()
    {
        foreach (var tur in SiparisTuru.Tumu)
        foreach (var yeni in SiparisDurumu.Tumu)
            Assert.False(SiparisDurumMakinesi.Izinli(tur, SiparisDurumu.TeslimEdildi, yeni),
                $"{tur}: TESLIM_EDILDI → {yeni} izinli görünüyor");
        Assert.Empty(SiparisDurumMakinesi.Hedefler(SiparisTuru.MusteriSiparisi, SiparisDurumu.TeslimEdildi));
        Assert.True(SiparisDurumMakinesi.Terminal(SiparisDurumu.TeslimEdildi));
    }

    [Fact]
    public void IPTAL_terminaldir_hicbir_cikis_yok()
    {
        foreach (var tur in SiparisTuru.Tumu)
        foreach (var yeni in SiparisDurumu.Tumu)
            Assert.False(SiparisDurumMakinesi.Izinli(tur, SiparisDurumu.Iptal, yeni),
                $"{tur}: IPTAL → {yeni} izinli görünüyor");
        Assert.True(SiparisDurumMakinesi.Terminal(SiparisDurumu.Iptal));
    }

    [Fact]
    public void Atolye_siparisi_ONAYLANDIdan_dogrudan_HAZIRa_gecemez()
    {
        Assert.False(SiparisDurumMakinesi.Izinli(
            SiparisTuru.AtolyeSiparisi, SiparisDurumu.Onaylandi, SiparisDurumu.Hazir));
        // Ama müşteri siparişi ve tamir geçebilir — tür ayrımı gerçekten var.
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.MusteriSiparisi, SiparisDurumu.Onaylandi, SiparisDurumu.Hazir));
        Assert.True(SiparisDurumMakinesi.Izinli(
            SiparisTuru.Tamir, SiparisDurumu.Onaylandi, SiparisDurumu.Hazir));
    }

    [Fact]
    public void Bilinmeyen_tur_ve_durum_false_doner_fail_closed()
    {
        Assert.False(SiparisDurumMakinesi.Izinli("IMALAT_EMRI", "TASLAK", "ONAYLANDI"));
        Assert.False(SiparisDurumMakinesi.Izinli(SiparisTuru.Tamir, "CILA_BITTI", "HAZIR"));
        Assert.False(SiparisDurumMakinesi.Izinli(SiparisTuru.Tamir, "TASLAK", "KARGOYA_VERILDI"));
        Assert.False(SiparisDurumMakinesi.Izinli(null, null, null));
        Assert.Empty(SiparisDurumMakinesi.Hedefler("IMALAT_EMRI", "TASLAK"));
    }

    [Fact]
    public void Geri_gecis_ve_IPTAL_icin_gerekce_zorunludur()
    {
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Taslak, SiparisDurumu.Iptal));
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Hazir, SiparisDurumu.Iptal));
        // Geri yönlü geçiş (tabloda izinli değil ama kural burada duruyor)
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Hazir, SiparisDurumu.Taslak));
        Assert.True(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Atolyede, SiparisDurumu.Onaylandi));
        // İleri yönlü geçişte gerekçe istenmez
        Assert.False(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Taslak, SiparisDurumu.Onaylandi));
        Assert.False(SiparisDurumMakinesi.GerekceGerekli(SiparisDurumu.Hazir, SiparisDurumu.TeslimEdildi));
    }

    [Fact]
    public void Durum_listesi_KAPALI_alti_durumdur()
    {
        // Durum enflasyonu yasağı: liste büyüdüyse bu test bilinçli güncellenir.
        Assert.Equal(6, SiparisDurumu.Tumu.Length);
        Assert.Equal(3, SiparisTuru.Tumu.Length);
    }

    // ── Gecikme (saat soyutlaması — "bugün" PARAMETRE) ───────────────────────

    [Fact]
    public void Geciken_sozu_gecmis_ve_acik_siparistir()
    {
        var bugun = new DateOnly(2026, 9, 16);
        Assert.True(SiparisGecikme.Gecikti(new DateOnly(2026, 9, 15), SiparisDurumu.Onaylandi, bugun));
        // Bugün söz verilmiş: HENÜZ gecikmedi (gece yarısı tuzağı — K5)
        Assert.False(SiparisGecikme.Gecikti(bugun, SiparisDurumu.Onaylandi, bugun));
        Assert.False(SiparisGecikme.Gecikti(new DateOnly(2026, 9, 17), SiparisDurumu.Onaylandi, bugun));
        // Kapanmış sipariş gecikmiş sayılmaz
        Assert.False(SiparisGecikme.Gecikti(new DateOnly(2026, 9, 15), SiparisDurumu.TeslimEdildi, bugun));
        Assert.False(SiparisGecikme.Gecikti(new DateOnly(2026, 9, 15), SiparisDurumu.Iptal, bugun));
        // Söz verilmemişse gecikme kavramı yok
        Assert.False(SiparisGecikme.Gecikti(null, SiparisDurumu.Onaylandi, bugun));
    }
}
