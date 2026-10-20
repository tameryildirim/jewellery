namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR SERVİSİ — paket 34 Faz A'nın tiplenmiş istemcisi.
//
// NEDEN AYRI SERVİS: rapor sayfaları `Api.GetAsync<JsonElement>` ile yazılsaydı
// alan adı değiştiğinde ekran SESSİZCE boşalırdı (UX-S28 yasağı). Sözleşme
// burada tiplenir; sunucu alan adını değiştirirse DERLEME değil, test kırılır
// ve sahte API deseni tek yerden kurulur.
//
// ⚠ Hiçbir metot hatayı yutmaz: ApiHata yukarı çıkar, sayfa hata bandı basar.
//   "catch → listeyi boşalt" Anayasa md.1 ihlalidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RaporServisi(ApiIstemci api)
{
    public Task<KatalogYaniti> Katalog() => api.GetAsync<KatalogYaniti>("/raporlar/katalog");

    public Task<IslemDefteriYaniti> IslemDefteri(
        DateOnly baslangic, DateOnly bitis, string? tur = null, string? subeId = null,
        string? cariId = null, string? kullaniciId = null, string? odemeTuru = null,
        int limit = 100, int ofset = 0)
        => api.GetAsync<IslemDefteriYaniti>("/raporlar/islem-defteri" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis)),
            ("tur", tur), ("subeId", subeId), ("cariId", cariId),
            ("kullaniciId", kullaniciId), ("odemeTuru", odemeTuru),
            ("limit", Bicim.Makine(limit)), ("ofset", Bicim.Makine(ofset))));

    public Task<OdemeAraciYaniti> OdemeAraci(DateOnly baslangic, DateOnly bitis, string? subeId = null)
        => api.GetAsync<OdemeAraciYaniti>("/raporlar/odeme-araci" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis)), ("subeId", subeId)));

    /// <summary>D1-20 — satan bazlı prim ve hedef.</summary>
    public Task<Kuyumcu.Contracts.Sayfalar.Raporlar.PrimRaporYaniti> Prim(DateOnly baslangic, DateOnly bitis, string? subeId = null, decimal? oran = null)
        => api.GetAsync<Kuyumcu.Contracts.Sayfalar.Raporlar.PrimRaporYaniti>("/raporlar/prim" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis)), ("subeId", subeId),
            ("oran", oran is { } o ? o.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)));   // D1-20 dene-gör

    public Task<PersonelYaniti> Personel(DateOnly baslangic, DateOnly bitis, string? subeId = null)
        => api.GetAsync<PersonelYaniti>("/raporlar/personel" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis)), ("subeId", subeId)));

    public Task<SubeKarsilastirmaYaniti> SubeKarsilastirma(DateOnly baslangic, DateOnly bitis)
        => api.GetAsync<SubeKarsilastirmaYaniti>("/raporlar/sube-karsilastirma" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis))));

    /// <summary>
    /// R15 — stok yaşlandırma / devir hızı (P-25 · Y-16).
    /// Dönemden BAĞIMSIZDIR: "bugün itibarıyla rafta ne var" sorusunu sorar
    /// (UX-S33 — ekran bunu kartın altına yazar).
    /// </summary>
    public Task<StokYaslandirmaYaniti> StokYaslandirma(
        string? subeId = null, int? oluEsikGun = null, int? devirPenceresiGun = null,
        int limit = 100, int ofset = 0)
        => api.GetAsync<StokYaslandirmaYaniti>("/raporlar/stok-yaslandirma" + Sorgu(
            ("subeId", subeId),
            ("oluEsikGun", oluEsikGun is { } e ? Bicim.Makine(e) : null),
            ("devirPenceresiGun", devirPenceresiGun is { } d ? Bicim.Makine(d) : null),
            ("limit", Bicim.Makine(limit)), ("ofset", Bicim.Makine(ofset))));

    /// <summary>
    /// ŞUBE-KAPSAM-B (§ 4.3.3 "Gün sonu") — POS/fatura mutabakatı, <paramref name="subeId"/> =
    /// görünüm şubesi (SubeliSorgu.Sube; null = daraltma yok). Tarih null ise sunucu işletme
    /// gününü çözer (UX-S21). Seçim yokken yol UyumServisi.Mutabakat/BugunMutabakati ile AYNI
    /// (UyumServisi bu şeridin dosya alanında değil; tip oradan, tek tanım).
    /// </summary>
    public Task<Mutabakat> PosFaturaMutabakati(DateTime? tarih, string? subeId)
        => api.GetAsync<Mutabakat>("/raporlar/pos-fatura-mutabakati" + Sorgu(
            ("tarih", tarih is { } t ? Bicim.Makine(t) : null), ("subeId", subeId)));

    public Task<KasaDefteriYaniti> KasaDefteri(DateOnly baslangic, DateOnly bitis, string? kasaId = null)
        => api.GetAsync<KasaDefteriYaniti>("/raporlar/kasa-defteri" + Sorgu(
            ("baslangic", Bicim.Makine(baslangic)),
            ("bitis", Bicim.Makine(bitis)), ("kasaId", kasaId)));

    private static string Sorgu(params (string Ad, string? Deger)[] parcalar)
    {
        var dolu = parcalar.Where(p => !string.IsNullOrEmpty(p.Deger))
            .Select(p => $"{p.Ad}={Uri.EscapeDataString(p.Deger!)}").ToList();
        return dolu.Count == 0 ? "" : "?" + string.Join("&", dolu);
    }
}

// ── API sözleşmesi — sunucu yanıtlarıyla birebir ─────────────────────────────

public sealed record RaporKartiDto(
    string Kod, string Ad, string Rota, string Uc, string[]? Roller, string Faz, string Grup);

public sealed record KatalogYaniti(int Toplam, string? Faz, List<RaporKartiDto>? Raporlar);

public sealed record DefterSatiri(
    string Id, DateTime Tarih, string Tur, string? Yon,
    decimal TlTutari, decimal HasGrami, string? Maden, string? ParaBirimi,
    string? OdemeTuru, decimal? NakitTutar, decimal? VeresiyeTutar,
    string? FisId, string? FaturaNo, string? CariAd, string? UrunAd,
    string? SubeAd, string? KullaniciAd, bool StornoSatiri, bool Stornolandi,
    // P-48: iki bacağın ayrı yönü (GIRIS/CIKIS/null). Yon = KARISIK iken ekran bunları basar.
    string? YonTl = null, string? YonHas = null);

public sealed record DefterOzeti(decimal TlGiris, decimal TlCikis, decimal HasGiris, decimal HasCikis, int SatirSayisi);

public sealed record StornoOzeti(int Adet, decimal TutarTl);

public sealed record IslemDefteriYaniti(
    string? Baslangic, string? Bitis, int ToplamSatir, int Ofset, int Limit,
    bool DahaFazlasiVar, DefterOzeti? Ozet, StornoOzeti? Storno, List<DefterSatiri>? Satirlar);

public sealed record AracParaBirimi(string? ParaBirimi, decimal GirisTutar, decimal CikisTutar, int Adet);

public sealed record OdemeAraciSatiri(
    string Arac, List<AracParaBirimi>? ParaBirimleri, decimal GirisTlToplam, int Adet);

public sealed record VeresiyeOzeti(decimal DoganTl, decimal KapananTl);

public sealed record BilinmeyenOzeti(int NakitPayiBilinmeyenSatir, int NakitPayiBilinenSatir);

public sealed record OdemeAraciYaniti(
    string? Baslangic, string? Bitis, string? Kaynak, List<OdemeAraciSatiri>? Araclar,
    VeresiyeOzeti? Veresiye, BilinmeyenOzeti? Bilinmeyen, StornoOzeti? Storno, string? Guvenilirlik);

public sealed record PersonelSatiri(
    string KullaniciId, string KaydedenAd, int IslemSayisi, decimal CiroTl,
    decimal OrtalamaSepetTl, int IadeAdet, decimal IadeTl, decimal? IadeOrani);

public sealed record PersonelYaniti(
    string? Baslangic, string? Bitis, string? Olcut, string? Uyari,
    int ToplamSatir, List<PersonelSatiri>? Satirlar);

public sealed record SubeSatiri(
    string SubeId, string SubeAd, int SatisAdet, decimal CiroTl, decimal AlisTl,
    decimal HurdaTl, decimal HurdaHasGram, decimal MalFarkiTl, int TransferAdet, decimal TransferTl);

public sealed record SubeKarsilastirmaYaniti(
    string? Baslangic, string? Bitis, bool TransferCiroyaKatilir,
    int ToplamSatir, decimal ToplamCiroTl, List<SubeSatiri>? Satirlar);

public sealed record KasaGunu(
    string Tarih, decimal Devir, decimal Giris, decimal Cikis, int HareketSayisi,
    decimal Kapanis, string Durum, decimal? SayilanNakit, decimal? KapanisDefterBakiye, decimal? Fark);

public sealed record KasaDefteriSatiri(
    string KasaId, string KasaAd, string? ParaBirimi, decimal AcilisDeviri,
    decimal KapanisBakiye, decimal GuncelBakiye, List<KasaGunu>? Gunler);

public sealed record KasaDefteriYaniti(
    string? Baslangic, string? Bitis, string? SaatDilimi, int UtcOfsetSaat,
    bool YazSaatiRiski, int ToplamSatir, List<KasaDefteriSatiri>? Kasalar);

// ── P-25 / Y-16 — R15 stok yaşlandırma sözleşmesi ───────────────────────────
// ⚠ Alan adı VE TİPİ sunucuyla birebir: `adet` sunucuda decimal(12,3)'tür,
//   istemcide int yazılsaydı 0,5 gramlık mal sessizce yuvarlanırdı.

public sealed record YasKovasiDto(
    string Kod, int AltSinirGun, int? UstSinirGun,
    decimal Adet, decimal HasGram, decimal MaliyetTl, decimal MaliyetiBilinmeyenAdet,
    // KAR-RAPOR-2: HasGram YALNIZ ALTIN hasıdır; diğer madenler kovanın içinde ayrı (gramları toplanmaz).
    List<YaslandirmaMadeniDto>? MadenKirilimi = null);

// MADEN-AYRIMI: FizikselGram = tartı gramı (adet × ürün gramı); ürünsüz partide bilinmez → null.
public sealed record YaslandirmaMadeniDto(
    string Maden, string MadenAdi, decimal Adet, decimal ToplamGram, decimal? FizikselGram = null);

public sealed record DevirDto(
    int PencereGun, decimal TuketilenAdet, decimal KalanAdet,
    decimal? DevirHizi, decimal? YillikTahmini, decimal? OrtalamaRafOmruGun,
    string? Yontem, string? Tanim,
    // RAPOR-GORUNUM (veri c): raf ömrü bir TAHMİNDİR (ileriye) — doğru adı + malın gerçek ortalama yaşı (geriye).
    decimal? TahminiSatisSuresiGun = null, decimal? OrtalamaYasGun = null);

public sealed record OluStokSatiriDto(
    string UrunId, string Kod, string Ad, string? AyarId, string? Maden,
    decimal Adet, decimal HasGram, DateTime EnEskiGiris, int YasGun);

public sealed record OluStokDto(
    int EsikGun, int ToplamSatir, decimal ToplamAdet, int Ofset, int Limit,
    bool DahaFazlasiVar, List<OluStokSatiriDto>? Urunler);

public sealed record StokYaslandirmaYaniti(
    DateTime OlculduAn, string? SubeId, List<YasKovasiDto>? Kovalar,
    decimal ToplamAdet, decimal AltinHasGram,
    List<YaslandirmaMadeniDto>? MadenDagilimi, decimal YasiBilinmeyenAdet,
    DevirDto? Devir, OluStokDto? OluStok, string? Guvenilirlik, List<string>? Uyarilar);

// ── Süzgeç sözleşmeleri ──────────────────────────────────────────────────────
// ⚠ Sayfa içinde record TANIMLANMAZ (denetim.sh kapısı): aynı şekil iki sayfada
//   iki kez yazılırsa biri güncellenir, öteki sessizce eskir.

/// <summary>/kullanicilar listesinin süzgeçte kullanılan alanları.</summary>
public sealed record KullaniciOzeti(string Id, string Ad, string Soyad);

/// <summary>/kasalar listesinin kasa defteri seçicisinde kullanılan alanları.</summary>
public sealed record KasaOzeti(string Id, string Ad, string? ParaBirimi, bool Aktif);
