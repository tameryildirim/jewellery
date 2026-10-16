namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// POS SERVİSİ — /api/v1/pos uçlarının TİPLENMİŞ istemcisi (paket 31).
//
// ⚠ TİP TEK YERDE (kurul engel 11 · denetim.sh:274 "Pages içinde istemci record
// tanımı" tavanı): Pos.razor ve PosTahsilatlar.razor KENDİ record'unu
// TANIMLAMAZ; ikisi de aşağıdaki tipleri kullanır. İki ardışık turda iki
// KRİTİK bu ikizlenmeden çıktı.
//
// ⚠ SAYI TİPİ SÖZLEŞMESİ: sunucunun decimal döndürdüğü her alan burada da
// decimal'dır. int YALNIZ taksitAdedi ve valorGun alanlarındadır — başka
// hiçbir yerde int kullanılmaz, aksi hâlde 9815,00 → 9815 sessiz kırpması
// ya da JsonException doğar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>POS cihazı satırı — sunucudaki PosCihazSatir ile BİREBİR aynı alanlar.</summary>
public sealed record PosCihazSatir(
    string Id, string Ad, string BankaAdi, string SubeId, string KasaId,
    string? BankaHesapId, string? UyeIsyeriNo, string? TerminalNo,
    decimal PesinKomisyonOran, int ValorGun, string ParaBirimi, bool Aktif,
    decimal BekleyenAlacak, decimal BugunYatacakNet,
    // QA turu 5 (YÜKSEK): sunucudaki PosCihazSatir.TahsilatVar ile AYNI AD, AYNI TİP (bool).
    bool TahsilatVar = false);

public sealed record PosTaksitOranSatir(
    string Id, string PosCihazId, int TaksitAdedi, decimal KomisyonOran,
    int? ValorGun, DateTime GecerliBaslangic, bool Aktif);

public sealed record PosCihazDetay(PosCihazSatir? Cihaz, List<PosTaksitOranSatir>? TaksitOranlari)
{
    public List<PosTaksitOranSatir> Oranlar => TaksitOranlari ?? [];
}

public sealed record PosTahsilatSatir(
    string Id, string PosCihazId, string CihazAd, string BankaAdi,
    decimal BrutTutar, decimal KomisyonOran, decimal KomisyonTutar, decimal NetTutar,
    decimal YatanTutar, decimal KalanTutar, int TaksitAdedi, string ParaBirimi,
    DateTime IslemTarihi, DateTime ValorTarihi, string Durum, string? IptalGerekcesi);

public sealed record PosTahsilatSayfasi(
    List<PosTahsilatSatir>? Satirlar, int ToplamSatir, int Limit, int Offset,
    int AzamiSatir, bool SinirDegdi)
{
    public List<PosTahsilatSatir> Kayitlar => Satirlar ?? [];
}

public sealed record PosValorSatir(
    DateTime ValorTarihi, string CihazId, string CihazAd, string BankaAdi,
    decimal Brut, decimal Komisyon, decimal Net, decimal Yatan, decimal Kalan);

public sealed record PosOneriSatir(string PosTahsilatId, decimal Tutar, DateTime ValorTarihi, string PosCihazId);

/// <remarks>ZO-ISLEM-A 28.09: HareketKalan = hareketin henüz eşleşmemiş tutarı (elle seçim sınırı);
/// eski yanıtta yoksa null = bilinmiyor (ekran sınır kilidi koymaz, sunucu karar verir).</remarks>
public sealed record PosOneri(bool Bulundu, string BankaHareketId, decimal Toplam,
    List<PosOneriSatir>? Satirlar, string? Mesaj, decimal? HareketKalan = null,
    List<string>? UygunCihazlar = null)
{
    public List<PosOneriSatir> Kayitlar => Satirlar ?? [];
}

public sealed record PosEslesmeSatir(
    string Id, string BankaHareketId, decimal EslesenTutar, bool Otomatik,
    bool GeriAlindi, string? GeriAlmaGerekcesi, DateTime Olusturuldu);

public sealed record PosEslesmeListesi(List<PosEslesmeSatir>? Satirlar)
{
    public List<PosEslesmeSatir> Kayitlar => Satirlar ?? [];
}

public sealed record PosIslemSonucu(string? Mesaj, string? Durum, decimal EslesenToplam, int SatirSayisi);

/// <summary>
/// POS cihazı formundaki kasa seçeneği. ⚠ Pages ALTINDA TANIMLANMAZ
/// (denetim.sh "Pages içinde istemci record tanımı" tavanı): istemci sözleşme
/// tipleri tek yerde, bu dosyada durur.
/// </summary>
public sealed record PosKasaSecenegi(string Id, string Ad, string ParaBirimi, string SubeId, bool Aktif);

public sealed class PosServisi(ApiIstemci api)
{
    // ── Cihaz ────────────────────────────────────────────────────────────────

    public Task<List<PosCihazSatir>> CihazlariGetir(bool? aktif = null)
        => api.GetAsync<List<PosCihazSatir>>(
            $"/pos/cihazlar{(aktif is { } a ? $"?aktif={(a ? "true" : "false")}" : "")}");

    public Task<PosCihazDetay> CihazGetir(string id)
        => api.GetAsync<PosCihazDetay>($"/pos/cihazlar/{id}");

    public Task<PosCihazDetay> CihazOlustur(object dto)
        => api.PostAsync<PosCihazDetay>("/pos/cihazlar", dto);

    /// <summary>KISMİ güncelleme: yalnız gönderilen alanlar değişir (PATCH sözleşmesi).</summary>
    public Task<PosCihazDetay> CihazGuncelle(string id, object dto)
        => api.PatchAsync<PosCihazDetay>($"/pos/cihazlar/{id}", dto);

    public Task<PosIslemSonucu> CihazPasifeAl(string id)
        => api.PostAsync<PosIslemSonucu>($"/pos/cihazlar/{id}/pasife-al");

    public Task<PosCihazDetay> TaksitOranEkle(string cihazId, object dto)
        => api.PostAsync<PosCihazDetay>($"/pos/cihazlar/{cihazId}/taksit-oranlari", dto);

    public Task<PosCihazDetay> TaksitOranGuncelle(string cihazId, string oranId, object dto)
        => api.PatchAsync<PosCihazDetay>($"/pos/cihazlar/{cihazId}/taksit-oranlari/{oranId}", dto);

    // ── Defter ───────────────────────────────────────────────────────────────

    public Task<PosTahsilatSayfasi> TahsilatlariGetir(
        string? durum = null, string? bas = null, string? bit = null,
        string? cihazId = null, int limit = 50, int offset = 0, string? subeId = null)
        => api.GetAsync<PosTahsilatSayfasi>($"/pos/tahsilatlar{Sorgu(
            ("durum", durum), ("bas", bas), ("bit", bit), ("cihazId", cihazId),
            ("limit", Bicim.Makine(limit)), ("offset", Bicim.Makine(offset)), ("subeId", subeId))}");

    /// <summary>ŞUBE-KAPSAM-B: <paramref name="subeId"/> = görünüm şubesi (SubeliSorgu.Sube); null = daraltma yok.</summary>
    public Task<List<PosValorSatir>> ValorTakvimi(string? bas = null, string? bit = null, string? subeId = null)
        => api.GetAsync<List<PosValorSatir>>($"/pos/valor-takvimi{Sorgu(("bas", bas), ("bit", bit), ("subeId", subeId))}");

    public Task<PosEslesmeListesi> EslesmeleriGetir(string tahsilatId)
        => api.GetAsync<PosEslesmeListesi>($"/pos/tahsilatlar/{tahsilatId}/eslesmeler");

    public Task<PosIslemSonucu> TahsilatIptal(string id, string gerekce)
        => api.PostAsync<PosIslemSonucu>($"/pos/tahsilatlar/{id}/iptal", new { gerekce });

    // ── Faz B ────────────────────────────────────────────────────────────────

    public Task<PosIslemSonucu> Eslestir(string bankaHareketId, IEnumerable<object> satirlar)
        => api.PostAsync<PosIslemSonucu>("/pos/eslestir", new { bankaHareketId, satirlar });

    public Task<PosOneri> OtoOneri(string bankaHareketId)
        => api.PostAsync<PosOneri>("/pos/oto-eslestir-onerisi", new { bankaHareketId });

    public Task<PosIslemSonucu> EslesmeGeriAl(string id, string gerekce)
        => api.PostAsync<PosIslemSonucu>($"/pos/eslesmeler/{id}/geri-al", new { gerekce });

    private static string Sorgu(params (string Ad, string? Deger)[] ogeler)
    {
        var dolu = ogeler.Where(o => !string.IsNullOrEmpty(o.Deger))
            .Select(o => $"{o.Ad}={Uri.EscapeDataString(o.Deger!)}")
            .ToList();
        return dolu.Count == 0 ? "" : "?" + string.Join("&", dolu);
    }
}
