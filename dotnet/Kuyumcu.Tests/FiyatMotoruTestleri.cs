using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fiyat;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT MOTORU — saf hesap, veritabanı yok (paket 17)
// Fiyat = has × kur × (1 + marj%) + işçilik × çarpan%; kampanya indirimi sonra.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FiyatMotoruTestleri
{
    private static Urun Altin(decimal has = 10m, decimal? iscilik = 500m, decimal? sabit = null) => new()
    {
        Id = "u1", TenantId = "t", Tip = "ZIYNET", Kod = "K", Ad = "Bilezik", AyarId = "K22",
        Milyem = 916, GramAgirlik = 10.92m, HasGrami = has, IscilikTl = iscilik, SatisFiyati = sabit,
    };
    private static FiyatListesi Liste(decimal marj = 5m, decimal carpan = 100m) => new()
    {
        Id = "l1", TenantId = "t", Ad = "Perakende", HasMarjYuzde = marj, IscilikCarpanYuzde = carpan, Varsayilan = true,
    };
    private static Kampanya K(string tur, decimal deger, string hedefTur = "TUMU", string? hedefId = null) => new()
    {
        Id = "k1", TenantId = "t", Ad = "Kampanya", Baslangic = new(2026, 9, 1), Bitis = new(2026, 9, 30),
        HedefTur = hedefTur, HedefId = hedefId, IndirimTuru = tur, Deger = deger, Aktif = true,
    };

    [Fact]
    public void Liste_fiyati_has_marj_ve_iscilikten_olusur()
    {
        // 10 has × 6.000 = 60.000; %5 marj = 3.000; işçilik 500 → 63.500
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m), null)!;
        Assert.Equal(60_000m, t.HasDegeri);
        Assert.Equal(3_000m, t.MarjTl);
        Assert.Equal(500m, t.IscilikTl);
        Assert.Equal(63_500m, t.ListeFiyati);
        Assert.Equal(63_500m, t.SatisFiyati);
        Assert.Equal(0m, t.IndirimTutari);
        Assert.False(t.SabitFiyat);
    }

    [Fact]
    public void Toptan_listesi_negatif_marj_ve_sifir_iscilik_uygulayabilir()
    {
        // Toptancıya maliyetine yakın: marj −1%, işçilik alınmaz → 59.400
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: -1m, carpan: 0m), null)!;
        Assert.Equal(-600m, t.MarjTl);
        Assert.Equal(0m, t.IscilikTl);
        Assert.Equal(59_400m, t.ListeFiyati);
    }

    /// <summary>Sabit fiyatlı üründe marj ve çarpan UYGULANMAZ — kuyumcu fiyatı koymuştur.</summary>
    [Fact]
    public void Sabit_satis_fiyati_marji_ve_carpani_EZER()
    {
        var t = FiyatMotoru.Hesapla(Altin(sabit: 70_000m), 6_000m, Liste(marj: 50m, carpan: 300m), null)!;
        Assert.True(t.SabitFiyat);
        Assert.Equal(70_000m, t.ListeFiyati);
        Assert.Equal(0m, t.MarjTl);
    }

    /// <summary>Gümüş altın kuruyla fiyatlanmaz — 76.972 TL'lik kolye hatası.</summary>
    [Fact]
    public void Gumus_urune_sabit_fiyat_yoksa_teklif_YOK()
    {
        var g = Altin(); g.AyarId = "G925";
        Assert.Null(FiyatMotoru.Hesapla(g, 6_000m, Liste(), null));
        g.SatisFiyati = 1_200m;
        Assert.Equal(1_200m, FiyatMotoru.Hesapla(g, 6_000m, Liste(), null)!.ListeFiyati);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET 20 — MADEN BAZLI FAIL-CLOSED FİYAT (K20-4)
    // Eski kural yalnız 'G' önekine bakıyordu: PT950 ve PD999 ürünler bu
    // kontrolden GEÇİP altın has kuruyla çarpılıyordu.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 5 g platin alyans ALTIN has kuruyla fiyatlanamaz — eski kod 30.000 TL'lik
    /// bir "has değeri" üretiyordu (mutasyon kanıtı M20-2).
    /// </summary>
    [Fact]
    public void PlatinUrunAltinKuruylaFiyatlanmaz()
    {
        var p = Altin(has: 5m); p.AyarId = "PT950"; p.Tip = "PLATIN";
        Assert.Null(FiyatMotoru.Hesapla(p, 6_000m, Liste(), null));
    }

    /// <summary>Paladyum da aynı kapıdan döner — "G ile başlamıyor" bahanesi yok.</summary>
    [Fact]
    public void PaladyumUrunAltinKuruylaFiyatlanmaz()
    {
        var p = Altin(has: 4m); p.AyarId = "PD999"; p.Tip = "PALADYUM";
        Assert.Null(FiyatMotoru.Hesapla(p, 6_000m, Liste(), null));
    }

    /// <summary>Altın dışı maden SABİT fiyatla satılır — fail-closed, fail-silent değil.</summary>
    [Fact]
    public void PlatinUrunSabitFiyatlaFiyatlanir()
    {
        var p = Altin(has: 5m, sabit: 42_000m); p.AyarId = "PT950"; p.Tip = "PLATIN";
        var t = FiyatMotoru.Hesapla(p, 6_000m, Liste(marj: 50m), null)!;
        Assert.True(t.SabitFiyat);
        Assert.Equal(42_000m, t.ListeFiyati);
        Assert.Equal(0m, t.HasDegeri);
    }

    /// <summary>Çelik kıymetsizdir: has gramı 0'dır, yalnız sabit fiyatla satılır (K20-5).</summary>
    [Fact]
    public void CelikUrunYalnizSabitFiyatlaFiyatlanir()
    {
        var c = Altin(has: 0m); c.AyarId = "CELIK"; c.Tip = "CELIK"; c.Milyem = 0m;
        Assert.Null(FiyatMotoru.Hesapla(c, 6_000m, Liste(), null));
        c.SatisFiyati = 2_500m;
        Assert.Equal(2_500m, FiyatMotoru.Hesapla(c, 6_000m, Liste(), null)!.ListeFiyati);
    }

    /// <summary>REGRESYON: altın senaryosu paket 20'den ETKİLENMEZ.</summary>
    [Fact]
    public void AltinSenaryosuDegismez()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m), null)!;
        Assert.Equal(63_500m, t.ListeFiyati);
        Assert.Equal(FiyatKaynagi.Liste, t.Kaynak);
    }

    [Fact]
    public void Kur_yoksa_teklif_YOK()
    {
        Assert.Null(FiyatMotoru.Hesapla(Altin(), 0m, Liste(), null));
    }

    // ── Kampanya ─────────────────────────────────────────────────────────────

    [Fact]
    public void Iscilik_yuzdesi_indirimi_yalniz_ISCILIGE_uygulanir()
    {
        // liste 63.500; işçilik 500'ün %50'si = 250 → 63.250
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("ISCILIK_YUZDE", 50m))!;
        Assert.Equal(250m, t.IndirimTutari);
        Assert.Equal(63_250m, t.SatisFiyati);
        Assert.Equal("k1", t.KampanyaId);
    }

    /// <summary>Sabit fiyatta işçilik bileşeni bilinmez → işçilik indirimi 0; tutar yüzdesi diye sayılmaz.</summary>
    [Fact]
    public void Sabit_fiyatli_urunde_iscilik_indirimi_SIFIRDIR()
    {
        var t = FiyatMotoru.Hesapla(Altin(sabit: 70_000m), 6_000m, Liste(), K("ISCILIK_YUZDE", 50m))!;
        Assert.Equal(0m, t.IndirimTutari);
        Assert.Null(t.KampanyaId);
    }

    [Fact]
    public void Tutar_yuzdesi_ve_sabit_TL_indirimi()
    {
        Assert.Equal(6_350m, FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("TUTAR_YUZDE", 10m))!.IndirimTutari);
        Assert.Equal(1_000m, FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("TUTAR_TL", 1_000m))!.IndirimTutari);
    }

    /// <summary>İndirim liste fiyatını aşamaz — negatif satış fiyatı yoktur.</summary>
    [Fact]
    public void Indirim_liste_fiyatini_ASAMAZ()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("TUTAR_TL", 1_000_000m))!;
        Assert.Equal(t.ListeFiyati, t.IndirimTutari);
        Assert.Equal(0m, t.SatisFiyati);
    }

    [Fact]
    public void Kampanya_tarih_ve_hedef_kurallari()
    {
        var u = Altin(); u.KategoriId = "kat-alt";
        var zincir = new HashSet<string> { "kat-alt", "kat-ust" };

        Assert.True(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m), new(2026, 9, 15), u, zincir));
        Assert.False(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m), new(2026, 10, 1), u, zincir));       // bitmiş
        Assert.False(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m), new(2026, 8, 31), u, zincir));       // başlamamış
        Assert.True(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m, "KATEGORI", "kat-ust"), new(2026, 9, 15), u, zincir)); // üst kategori kapsar
        Assert.False(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m, "KATEGORI", "baska"), new(2026, 9, 15), u, zincir));
        Assert.True(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m, "URUN", "u1"), new(2026, 9, 15), u, zincir));
        Assert.False(FiyatMotoru.Uygulanir(K("TUTAR_TL", 1m, "URUN", "u2"), new(2026, 9, 15), u, zincir));
        var pasif = K("TUTAR_TL", 1m); pasif.Aktif = false;
        Assert.False(FiyatMotoru.Uygulanir(pasif, new(2026, 9, 15), u, zincir));
    }

    /// <summary>Kampanyalar TOPLANMAZ; müşteri lehine en yüksek TEK indirim seçilir.</summary>
    [Fact]
    public void Birden_fazla_kampanyada_EN_YUKSEK_tek_indirim_secilir()
    {
        var a = K("TUTAR_TL", 1_000m); a.Id = "a";
        var b = K("TUTAR_YUZDE", 10m); b.Id = "b";      // 6.350
        var c = K("ISCILIK_YUZDE", 100m); c.Id = "c";   // 500

        var secilen = FiyatMotoru.EnIyi([a, b, c], Altin(), 6_000m, Liste());
        Assert.Equal("b", secilen!.Id);
        Assert.Equal(6_350m, FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), secilen)!.IndirimTutari);
    }

    [Fact]
    public void Yuvarlama_iki_hane_AwayFromZero()
    {
        // 1,005 has × 1.000 = 1.005,00; %0,125 marj = 1,25625 → 1,26
        var t = FiyatMotoru.Hesapla(Altin(has: 1.005m, iscilik: 0m), 1_000m, Liste(marj: 0.125m), null)!;
        Assert.Equal(1.26m, t.MarjTl);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET 49 — liste × ürün geçersiz kılma (saf motor)
    // Öncelik: (1) geçersiz kılma sabit fiyatı → (2) ürün kartı sabit fiyatı →
    //          (3) geçersiz kılma marj/çarpanı (listeninkinin YERİNE) → (4) liste.
    // ═════════════════════════════════════════════════════════════════════════

    private static FiyatListesiUrunFiyati GK(
        decimal? sabit = null, decimal? marj = null, decimal? carpan = null, bool aktif = true) => new()
    {
        Id = "gk1", TenantId = "t", ListeId = "l1", UrunId = "u1",
        SabitFiyat = sabit, HasMarjYuzde = marj, IscilikCarpanYuzde = carpan, Aktif = aktif,
    };

    /// <summary>M49-1 — kesişim, ürün kartındaki GENEL sabit fiyattan daha özeldir.</summary>
    [Fact]
    public void Gecersiz_kilma_sabit_fiyati_URUN_KARTI_sabit_fiyatini_EZER()
    {
        var t = FiyatMotoru.Hesapla(Altin(sabit: 70_000m), 6_000m, Liste(marj: 5m), null, GK(sabit: 55_000m))!;
        Assert.Equal(55_000m, t.ListeFiyati);
        Assert.True(t.SabitFiyat);
        Assert.Equal(FiyatKaynagi.UrunGecersizKilma, t.Kaynak);
    }

    /// <summary>M49-2 — yalnız marj doluysa işçilik çarpanı LİSTEDEN gelir.</summary>
    [Fact]
    public void Yalniz_marj_gecersiz_kilmasinda_iscilik_carpani_LISTEDEN_gelir()
    {
        // 10 has × 6.000 = 60.000; geçersiz kılma marjı %1 = 600; işçilik listeden %50 → 250
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m, carpan: 50m), null, GK(marj: 1m))!;
        Assert.Equal(600m, t.MarjTl);
        Assert.Equal(250m, t.IscilikTl);
        Assert.Equal(60_850m, t.ListeFiyati);
        Assert.Equal(FiyatKaynagi.UrunGecersizKilma, t.Kaynak);
    }

    /// <summary>M49-2 — yalnız çarpan doluysa marj LİSTEDEN gelir.</summary>
    [Fact]
    public void Yalniz_iscilik_carpani_gecersiz_kilmasinda_marj_LISTEDEN_gelir()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m, carpan: 100m), null, GK(carpan: 20m))!;
        Assert.Equal(3_000m, t.MarjTl);                                  // liste marjı %5
        Assert.Equal(100m, t.IscilikTl);                                 // 500 × %20
        Assert.Equal(63_100m, t.ListeFiyati);
    }

    /// <summary>M49-3 — geçersiz kılma marjı listeninkiyle TOPLANMAZ, YERİNE geçer.</summary>
    [Fact]
    public void Gecersiz_kilma_marji_listeninkiyle_TOPLANMAZ()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m), null, GK(marj: 1m))!;
        Assert.Equal(600m, t.MarjTl);                                    // 60.000 × %1
        Assert.NotEqual(3_600m, t.MarjTl);                               // %5 + %1 DEĞİL
    }

    /// <summary>M49-4 (Q3 mührü) — gümüş + SABİT fiyatlı geçersiz kılma → teklif üretilir, kurdan bağımsız.</summary>
    [Fact]
    public void Gumus_urun_SABIT_fiyatli_gecersiz_kilmayla_FIYATLANIR()
    {
        var gumus = Altin(has: 0m, iscilik: 0m);
        gumus.AyarId = "G925";
        var yuksekKur = FiyatMotoru.Hesapla(gumus, 6_000m, Liste(), null, GK(sabit: 1_200m))!;
        var kursuz = FiyatMotoru.Hesapla(gumus, 0m, Liste(), null, GK(sabit: 1_200m))!;
        Assert.Equal(1_200m, yuksekKur.ListeFiyati);
        Assert.Equal(1_200m, kursuz.ListeFiyati);                        // sabit fiyat has kuruna BAĞLI DEĞİL
        Assert.True(yuksekKur.SabitFiyat);
        Assert.Equal(FiyatKaynagi.UrunGecersizKilma, yuksekKur.Kaynak);
    }

    /// <summary>M49-4 — gümüş + YALNIZ marj geçersiz kılması → hâlâ fail-closed null.</summary>
    [Fact]
    public void Gumus_urun_yalniz_marj_gecersiz_kilmasiyla_HALA_NULL()
    {
        var gumus = Altin(has: 5m);
        gumus.AyarId = "G925";
        Assert.Null(FiyatMotoru.Hesapla(gumus, 6_000m, Liste(), null, GK(marj: 50m)));
    }

    /// <summary>M49-5 — sabit fiyat kaynaklı satırda ISCILIK_YUZDE indirimi 0 kalır (mevcut kural).</summary>
    [Fact]
    public void Gecersiz_kilma_sabit_fiyatinda_ISCILIK_YUZDE_indirimi_SIFIR()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("ISCILIK_YUZDE", 50m), GK(sabit: 40_000m))!;
        Assert.Equal(0m, t.IndirimTutari);
        Assert.Equal(40_000m, t.SatisFiyati);
        // TUTAR_YUZDE aynı satırda çalışır (kampanya kuralı korunuyor).
        var t2 = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), K("TUTAR_YUZDE", 10m), GK(sabit: 40_000m))!;
        Assert.Equal(4_000m, t2.IndirimTutari);
    }

    /// <summary>PASİF geçersiz kılma YOK sayılır — "pasife al" fiyatı listeye döndürür.</summary>
    [Fact]
    public void Pasif_gecersiz_kilma_YOK_sayilir_fiyat_listeye_doner()
    {
        var t = FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(marj: 5m), null, GK(marj: 1m, aktif: false))!;
        Assert.Equal(3_000m, t.MarjTl);
        Assert.Equal(FiyatKaynagi.Liste, t.Kaynak);
    }

    /// <summary>Geçersiz kılma yokken kaynak LISTE, ürün kartı sabit fiyatında URUN_SABIT_FIYAT.</summary>
    [Fact]
    public void Kaynak_rozeti_dogru_isaretlenir()
    {
        Assert.Equal(FiyatKaynagi.Liste, FiyatMotoru.Hesapla(Altin(), 6_000m, Liste(), null)!.Kaynak);
        Assert.Equal(FiyatKaynagi.UrunSabitFiyat,
            FiyatMotoru.Hesapla(Altin(sabit: 70_000m), 6_000m, Liste(), null)!.Kaynak);
    }
}
