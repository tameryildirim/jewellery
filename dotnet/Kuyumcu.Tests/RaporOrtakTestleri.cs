using System.Reflection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Rapor;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 34 Faz A — ORTAK RAPOR ALTYAPISI [saf test, veritabanı YOK]
//
// Katalog testi BİLEREK "== 19" DEĞİLDİR (Bertan/Aşkın koşulu): sayı testi
// tautolojidir ve paket 39/42 yeni rapor eklediğinde uygulayıcıyı katalogdan
// satır SİLMEYE iter. Gerçek koruma, her katalog rotasının yansımayla bulunan
// GERÇEK bir [HttpGet] ucuna karşılık gelmesidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RaporOrtakTestleri
{
    // ── RaporAraligi ─────────────────────────────────────────────────────────

    [Fact]
    public void Bitis_baslangictan_onceyse_400()
    {
        var hata = Assert.Throws<BadRequestException>(() =>
            RaporAraligi.Coz(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 9)));
        Assert.Equal(400, hata.StatusCode);
    }

    [Fact]
    public void Yerel_gun_siniri_isletme_saatine_gore()
    {
        // M109 — K5 kuralı: yerel gün 00:00 İstanbul = UTC'de önceki gün 21:00.
        // DateTime.Today ile kurulan aralık bu testi KIRAR.
        var d = RaporAraligi.Coz(new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 15));

        Assert.Equal(DateTimeKind.Utc, d.Bas.Kind);
        Assert.Equal(new DateTime(2026, 9, 14, 21, 0, 0, DateTimeKind.Utc), d.Bas);
        Assert.Equal(new DateTime(2026, 9, 15, 21, 0, 0, DateTimeKind.Utc), d.Bit);
        // Aralık KAPALI yorumlanır: tek gün verilince o gün TAMAMEN kapsanır.
        Assert.Equal(new DateOnly(2026, 9, 15), d.YerelBas);
        Assert.Equal(new DateOnly(2026, 9, 15), d.YerelBit);
    }

    [Fact]
    public void Tarih_verilmezse_bugun_cozulur()
    {
        var d = RaporAraligi.Coz(null, null);
        Assert.Equal(Zaman.YerelBugun(), d.YerelBas);
        Assert.Equal(Zaman.YerelBugun(), d.YerelBit);
    }

    [Fact]
    public void Cok_uzun_donem_400()
    {
        Assert.Throws<BadRequestException>(() =>
            RaporAraligi.Coz(new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1)));
    }

    // ── RaporFiltresi ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void Gecersiz_limit_400(int limit)
    {
        // ⚠ Bu kapı SERVİSTE durur: [Range] yalnız controller bağlamada çalışır.
        Assert.Throws<BadRequestException>(() => new RaporFiltresi(Limit: limit).Dogrula());
    }

    [Fact]
    public void Negatif_ofset_400()
        => Assert.Throws<BadRequestException>(() => new RaporFiltresi(Ofset: -1).Dogrula());

    [Fact]
    public void Gecerli_limit_kabul_edilir()
        => Assert.Equal(500, new RaporFiltresi(Limit: 500).Dogrula().Limit);

    // ── SayfaliSonuc ─────────────────────────────────────────────────────────

    [Fact]
    public void Sunucu_sinirina_degince_daha_fazlasi_var()
    {
        var s = new SayfaliSonuc<int>(ToplamSatir: 1200, Ofset: 0, Limit: 500, Satirlar: [.. Enumerable.Range(1, 500)]);
        Assert.True(s.DahaFazlasiVar);
        Assert.False(new SayfaliSonuc<int>(3, 0, 500, [1, 2, 3]).DahaFazlasiVar);
    }

    // ── RaporGuvenilirligi ───────────────────────────────────────────────────

    [Fact]
    public void Bilinmeyen_varsa_TAM_degildir()
    {
        Assert.Equal(RaporGuvenilirligi.Tam, RaporGuvenilirligi.Hesapla(100m, 0m));
        Assert.NotEqual(RaporGuvenilirligi.Tam, RaporGuvenilirligi.Hesapla(100m, 1m));
        Assert.Equal(RaporGuvenilirligi.Yuksek, RaporGuvenilirligi.Hesapla(95m, 5m));
        Assert.Equal(RaporGuvenilirligi.Orta, RaporGuvenilirligi.Hesapla(60m, 40m));
        Assert.Equal(RaporGuvenilirligi.Dusuk, RaporGuvenilirligi.Hesapla(10m, 90m));
        Assert.Equal(RaporGuvenilirligi.VeriYok, RaporGuvenilirligi.Hesapla(0m, 0m));
    }

    // ── Katalog ──────────────────────────────────────────────────────────────

    [Fact]
    public void Katalog_asgari_sayiyi_tasir_ve_kodlar_tekildir()
    {
        // M116 — katalogdan rapor silinirse KIRMIZI.
        Assert.True(RaporKatalogu.Hepsi.Length >= RaporKatalogu.FazAAsgari,
            $"Katalog {RaporKatalogu.Hepsi.Length} rapor sayıyor, asgari {RaporKatalogu.FazAAsgari}");
        Assert.Equal(RaporKatalogu.Hepsi.Length,
                     RaporKatalogu.Hepsi.Select(r => r.Kod).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Katalogtaki_her_rota_gercek_bir_HttpGet_ucudur()
    {
        var kayitli = KayitliGetUclari();
        foreach (var kart in RaporKatalogu.Hepsi)
            Assert.True(kayitli.Contains(kart.Uc),
                $"{kart.Kod} kataloğa yazılı ama '{kart.Uc}' diye kayıtlı bir [HttpGet] ucu YOK");
    }

    [Fact]
    public void Katalog_adlari_sozlukten_gelir_ham_kod_basilmaz()
    {
        // UX-S31: ham kod (R10) ekrana basılmaz; sözlükte karşılığı olmalı.
        foreach (var kart in RaporKatalogu.Hepsi)
        {
            Assert.NotEqual(kart.Kod, Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.Ad(kart.Kod));
            Assert.NotEqual(kart.Kod, Kuyumcu.Core.I18n.Sozlukler.En.RaporSeti.Ad(kart.Kod));
        }
    }

    /// <summary>Tüm controller'lardan yansımayla toplanan GET rotaları (api/v1 öneksiz).</summary>
    private static HashSet<string> KayitliGetUclari()
    {
        var sonuc = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tip in typeof(RaporKatalogu).Assembly.GetTypes()
                     .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var rota = tip.GetCustomAttribute<RouteAttribute>()?.Template;
            if (rota is null) continue;
            var kok = rota.Replace("api/v1/", "", StringComparison.Ordinal).Trim('/');
            foreach (var m in tip.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var get in m.GetCustomAttributes<HttpGetAttribute>())
                {
                    var sablon = get.Template?.Trim('/');
                    sonuc.Add(string.IsNullOrEmpty(sablon) ? kok : $"{kok}/{sablon}");
                }
        }
        return sonuc;
    }
}
