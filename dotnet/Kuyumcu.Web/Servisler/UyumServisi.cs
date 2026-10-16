namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// UYUM SERVİSİ — MASAK ve gider pusulası uçlarının tiplenmiş istemcisi.
//
// NEDEN AYRI SERVİS (Bertan, PLAN-2 Adım 1 şartı):
// Mevcut sayfalar API'yi doğrudan `ApiIstemci` ile çağırıyor (`Islemler.razor`
// 733 satır, `Banka.razor` 540). Bu desen kopyala-yapıştır üretiyor ve
// `JsonElement` ile yazılan ekran TEST EDİLEMİYOR. Yeni ekranlar bu deseni
// büyütmeyecek: sözleşme burada tiplenir, sayfa yalnız gösterir.
//
// ⚠ ÖN YÜKLEME (prefetch) YASAK (Mehmet, PLAN-2 Adım 1 koşulu):
// Tekil kayıt uçları OKUMA İZİ düşürür (KVKK md.12). Liste açılırken 50 detay
// çekmek, denetim izini gürültüye boğar ve "kim neye baktı" sorusunu
// cevaplanamaz hâle getirir. Detay YALNIZ kullanıcı tıklayınca çekilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UyumServisi(ApiIstemci api)
{
    // ── MASAK: kimlik tespitleri ─────────────────────────────────────────────

    /// <summary>Liste — belge no MASKELİ gelir, okuma izi tek satır düşer.</summary>
    public Task<List<KimlikTespitSatir>> TespitleriGetir(
        DateTime? baslangic = null, DateTime? bitis = null, int limit = 100)
        => api.GetAsync<List<KimlikTespitSatir>>($"/masak/kimlik-tespitleri{Sorgu(
            ("baslangic", baslangic is { } mbaslangic ? Bicim.Makine(mbaslangic) : null),
            ("bitis", bitis is { } mbitis ? Bicim.Makine(mbitis) : null),
            ("limit", limit.ToString()))}");

    /// <summary>
    /// Tekil kayıt — TAM belge no döner ve <b>okuma izi düşer</b>.
    /// Yalnız kullanıcı açıkça istediğinde çağrılır.
    /// </summary>
    public Task<KimlikTespitDetay> TespitGetir(string id)
        => api.GetAsync<KimlikTespitDetay>($"/masak/kimlik-tespitleri/{id}");

    /// <summary>
    /// UX-4d madde 18: uç bir LİSTE değil, ÖZET + kayıtlar döndürür
    /// ({adet, toplamTutarTl, ilkIslem, sonIslem, kayitlar}). İstemci bunu
    /// List&lt;KimlikTespitSatir&gt; olarak okumaya çalışıyor ve her aramada
    /// JSON hatası alıyordu; ekran da eski listeyi göstermeye devam ediyordu.
    /// </summary>
    public Task<BelgeAramaSonucu> BelgeNoIleAra(string belgeNo)
        => api.GetAsync<BelgeAramaSonucu>(
            $"/masak/kimlik-tespitleri/ara?belgeNo={Uri.EscapeDataString(belgeNo)}");

    // ── MASAK: şüpheli işlem bildirimleri ────────────────────────────────────

    public Task<List<SupheliSatir>> SupheliGetir(string? durum = null)
        => api.GetAsync<List<SupheliSatir>>($"/masak/supheli{Sorgu(("durum", durum))}");

    public Task<SupheliSatir> SupheliOlustur(SupheliOlusturDto dto)
        => api.PostAsync<SupheliSatir>("/masak/supheli", dto);

    /// <summary>
    /// PAKET P-13 (B-12) — GÖNDERİLMİŞ bildirimin düzeltmesi. Asıl kayıt
    /// değişmez; düzeltme ona BAĞLI yeni bir bildirim olarak doğar.
    /// </summary>
    public Task<SupheliSatir> EkBeyan(string id, SupheliOlusturDto dto)
        => api.PostAsync<SupheliSatir>($"/masak/supheli/{id}/ek-beyan", dto);

    /// <summary>Bildirim MASAK Online'a yüklendikten sonra referansı kaydeder.</summary>
    public Task<SupheliSatir> GonderildiIsaretle(string id, string gonderimReferansi)
        => api.PatchAsync<SupheliSatir>($"/masak/supheli/{id}/gonderildi",
            new { gonderimReferansi });

    /// <summary>
    /// DP4-02 (QA turu 4 KRİTİK) — gövde alan adı SÖZLEŞMEYE göre "gerekce"dir.
    /// Eskiden "kapatmaGerekcesi" gönderiliyordu; sunucudaki
    /// <c>MasakModulu.KapatDto.Gerekce</c> bu adı tanımadığı için her kapatma
    /// denemesi 400 "Gerekçe zorunludur" ile dönüyordu ve ekranın kendi tarif
    /// ettiği tek düzeltme yolu kapalıydı.
    /// </summary>
    public Task<SupheliSatir> Kapat(string id, string gerekce)
        => api.PatchAsync<SupheliSatir>($"/masak/supheli/{id}/kapat",
            new { gerekce });

    public Task<MasakOzet> Ozet() => api.GetAsync<MasakOzet>("/masak/ozet");

    /// <summary>
    /// ZO-ISLEM-A 29.09: <paramref name="cariId"/> verilirse sunucu kademeyi aynı gün aynı
    /// müşteriyle yapılmış işlemlerin toplamıyla hesaplar (kaydın geçtiği kapıyla aynı ölçü).
    /// </summary>
    public Task<MasakGereksinim> Gereksinim(decimal tutar, string? cariId = null)
        => api.GetAsync<MasakGereksinim>(
            $"/masak/gereksinim?tutar={tutar.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + (string.IsNullOrWhiteSpace(cariId) ? "" : "&cariId=" + Uri.EscapeDataString(cariId)));

    // ── MASAK: saklama süresi ────────────────────────────────────────────────

    /// <summary>Kaç kayıt etkilenecek — HİÇBİR ŞEY DEĞİŞTİRMEZ.</summary>
    public Task<SaklamaOnizleme> SaklamaOnizleme()
        => api.GetAsync<SaklamaOnizleme>("/masak/saklama-onizleme");

    /// <summary>
    /// Süresi dolmuş kayıtların kimlik alanlarını MASKELER.
    /// ⚠ GERİ ALINAMAZ. Ekranda önce önizleme, sonra açık onay istenir
    /// (ADR-0008: bu iş otomatik zamanlayıcıya bilinçli olarak bağlanmadı).
    /// </summary>
    public Task<SaklamaSonuc> SaklamaUygula()
        => api.PostAsync<SaklamaSonuc>("/masak/saklama-uygula");

    // ── Gider pusulası ───────────────────────────────────────────────────────

    public Task<List<PusulaSatir>> PusulalariGetir(
        DateTime? baslangic = null, DateTime? bitis = null, int limit = 100)
        => api.GetAsync<List<PusulaSatir>>($"/gider-pusulalari{Sorgu(
            ("baslangic", baslangic is { } mbaslangic ? Bicim.Makine(mbaslangic) : null),
            ("bitis", bitis is { } mbitis ? Bicim.Makine(mbitis) : null),
            ("limit", limit.ToString()))}");

    /// <summary>Tekil pusula — TAM TCKN döner, okuma izi düşer.</summary>
    public Task<PusulaDetay> PusulaGetir(string id)
        => api.GetAsync<PusulaDetay>($"/gider-pusulalari/{id}");

    /// <summary>
    /// PAKET S-3d / DP4 — pusulanın MALİ KİMLİK DIŞI alanlarını düzeltir
    /// (satıcı adresi · mal açıklaması). PATCH sözleşmesi: burada iki alan da
    /// HER ZAMAN gönderilir, null = sil demektir. Diğer alanlar için tek yol
    /// İPTAL + YENİ PUSULA'dır (sunucu da böyle kapatır).
    /// </summary>
    public Task<PusulaDetay> PusulaGuncelle(string id, string? saticiAdres, string? malAciklamasi)
        => api.PatchAsync<PusulaDetay>($"/gider-pusulalari/{id}",
            new { saticiAdres, malAciklamasi });

    /// <summary>Yazdırılabilir belge (VUK md.234). Okuma izi düşer.</summary>
    public Task<PusulaBelge> Belge(string id)
        => api.GetAsync<PusulaBelge>($"/gider-pusulalari/{id}/belge");

    public Task<SureTakip> SureTakibi() => api.GetAsync<SureTakip>("/gider-pusulalari/sure-takibi");

    /// <summary>
    /// PAKET S-3b — VUK md.234 düzeltme yolu: belgeyi gerekçeli olarak hükümsüz
    /// kılar (silmez). Yerine doğru bilgilerle yeni pusula düzenlenir.
    /// </summary>
    public Task<PusulaIptalSonuc> PusulaIptal(string id, string gerekce)
        => api.PostAsync<PusulaIptalSonuc>($"/gider-pusulalari/{id}/iptal", new { gerekce });

    /// <summary>
    /// PAKET YASAL-4 / Y4-A — iptal edilmiş pusulanın AYNI alış için yeniden
    /// düzenlenmesi. Yeni hurda alışı GİRİLMEZ; son gün geçtiyse sunucu 409 döner.
    /// </summary>
    public Task<PusulaDetay> PusulaYenidenDuzenle(
        string id, string adSoyad, string tckn, string adres, string? malAciklamasi)
        => api.PostAsync<PusulaDetay>($"/gider-pusulalari/{id}/yeniden-duzenle",
            new { adSoyad, tckn, adres, malAciklamasi });

    // ── PAKET YASAL-4 / Y4-B — stopaj ───────────────────────────────────────
    public Task<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajDonemleriYaniti> StopajDonemleri(int yil)
        => api.GetAsync<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajDonemleriYaniti>($"/stopaj/donemler?yil={yil}");

    public Task<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajOzeti> StopajOzeti()
        => api.GetAsync<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajOzeti>("/stopaj/ozet");

    public Task<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajOdemeSonucu> StopajOde(
        string donem, string kasaId, decimal? tutar, string? tahakkukFisNo)
        => api.PostAsync<Kuyumcu.Contracts.Sayfalar.Stopaj.StopajOdemeSonucu>("/stopaj/odemeler",
            new { donem, kasaId, tutar, tahakkukFisNo });

    public Task<System.Text.Json.JsonElement> StopajGeriAl(string id, string gerekce)
        => api.PostAsync<System.Text.Json.JsonElement>($"/stopaj/odemeler/{id}/geri-al", new { gerekce });

    public Task<(byte[] Veri, string? DosyaAdi)> StopajListesi(string donem)
        => api.GetBaytAsync($"/stopaj/donemler/{donem}/liste.csv");

    // ── MASAK: risk skorlama (paket 40-A) ────────────────────────────────────
    // ⚠ ÖN YÜKLEME YASAK: RiskGetir tekil uçtur ve OKUMA İZİ düşürür
    //   (KVKK md.12, Mehmet K40-3). Yalnız kullanıcı bir cari seçince çağrılır.

    /// <summary>
    /// Cari seçicisinin kaynağı. ⚠ Bu bir LİSTE ucudur (tekil değil); risk
    /// detayı burada ÇEKİLMEZ — yalnız ad ve salt okunur seviye rozeti gelir.
    /// </summary>
    public Task<List<RiskCariSecenek>> RiskCarileri(int limit)
        => api.GetAsync<List<RiskCariSecenek>>($"/cari?aktif=true&limit={limit}");

    /// <summary>Panel/sekme kartı — kişisel veri yok, yalnız sayılar ve ad.</summary>
    public Task<RiskOzet> RiskOzet() => api.GetAsync<RiskOzet>("/masak/risk/ozet");

    /// <summary>Tekil profil + tarihçe. ⚠ OKUMA İZİ DÜŞER — tıklamadan çağrılmaz.</summary>
    public Task<RiskProfilYaniti> RiskGetir(string cariId)
        => api.GetAsync<RiskProfilYaniti>($"/masak/risk/{cariId}");

    /// <summary>Yeniden hesaplar ve YENİ tarihçe satırı yazar (güncelleme değil).</summary>
    public Task<RiskHesapYaniti> RiskHesapla(string cariId)
        => api.PostAsync<RiskHesapYaniti>($"/masak/risk/{cariId}/hesapla", new { });

    /// <summary>Gerekçeli elle seviye ezmesi — sunucu gerekçe uzunluğunu da doğrular.</summary>
    public Task<RiskHesapYaniti> RiskElle(string cariId, string seviye, string gerekce)
        => api.PostAsync<RiskHesapYaniti>($"/masak/risk/{cariId}/elle", new { seviye, gerekce });

    // ── Mutabakat ────────────────────────────────────────────────────────────

    public Task<Mutabakat> Mutabakat(DateTime tarih)
        => api.GetAsync<Mutabakat>($"/raporlar/pos-fatura-mutabakati?tarih={tarih:yyyy-MM-dd}");

    /// <summary>
    /// PAKET S3-4 — BUGÜNÜN gün sonu (Raporlar "Bugün" yüzü). Tarih GÖNDERİLMEZ:
    /// sunucu işletme gününü (Europe/Istanbul, Zaman.YerelBugun) kendisi çözer
    /// (UX-S21 — sistemin bildiği alan istemciden istenmez).
    /// </summary>
    public Task<Mutabakat> BugunMutabakati()
        => api.GetAsync<Mutabakat>("/raporlar/pos-fatura-mutabakati");

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private static string Sorgu(params (string Ad, string? Deger)[] parcalar)
    {
        var dolu = parcalar.Where(p => !string.IsNullOrEmpty(p.Deger))
            .Select(p => $"{p.Ad}={Uri.EscapeDataString(p.Deger!)}").ToList();
        return dolu.Count == 0 ? "" : "?" + string.Join("&", dolu);
    }
}

// ── API sözleşmesi ───────────────────────────────────────────────────────────
// Sunucu yanıtlarıyla birebir. Alan adı değişirse burada derleme hatası alınır —
// `JsonElement` ile yazılsaydı sessizce null dönerdi.

public sealed record KimlikTespitSatir(
    string Id, string? HareketId, string? CariId, decimal IslemTutariTl,
    string BelgeTuru, string BelgeNo, string AdSoyad,
    DateTime Tarih, DateTime SaklamaBitis, string TespitEdenKullaniciId,
    // ⚠ PAKET 41: HareketId artık iki farklı tabloyu işaret edebilir
    // (hareketler / banka_hareketleri) ve ADR-0009 gereği FK yoktur.
    // Kaynağı OKUYAN taraf bilmek zorundadır; null = eski/bilinmeyen kayıt.
    string? KaynakTur = null);

/// <summary>Belge no ile arama sonucu — özet + kayıtlar (UX-4d madde 18).</summary>
public sealed record BelgeAramaSonucu(
    int Adet, decimal ToplamTutarTl, DateTime? IlkIslem, DateTime? SonIslem,
    List<KimlikTespitSatir> Kayitlar);

public sealed record KimlikTespitDetay(
    string Id, string? HareketId, string? CariId, decimal IslemTutariTl,
    string BelgeTuru, string BelgeNo, string AdSoyad,
    DateOnly? DogumTarihi, string? Uyruk, string? Adres,
    string TespitEdenKullaniciId, DateTime Tarih, DateTime SaklamaBitis,
    string? DuzeltilenId, BeyanBilgi? Beyan,
    /// <summary>PAKET 41 — HareketId hangi tabloyu işaret ediyor.</summary>
    string? KaynakTur = null);

public sealed record BeyanBilgi(
    string Id, string Kademe, string? IslemAmaci, string? Aciklama,
    string? FonKaynagi, string? EkBelgeReferansi, DateTime Tarih);

public sealed record SupheliSatir(
    string Id, string? HareketId, string? CariId, string? KimlikTespitiId,
    decimal IslemTutariTl, string Gerekce,
    DateTime TespitTarihi, DateTime SonBildirimTarihi, double KalanSaat,
    string Durum, DateTime? GonderimTarihi, string? GonderimReferansi,
    string? KapatmaGerekcesi, string BildirenKullaniciId,
    DateTime SaklamaBitis, DateTime Olusturuldu, DateTime Guncellendi,
    // PAKET P-13 (B-12): doluysa bu kayıt bir EK BEYANDIR ve hangi gönderilmiş
    // bildirimi düzelttiği buradadır (sunucu alanı: duzeltilenBildirimId).
    string? DuzeltilenBildirimId = null)
{
    /// <summary>Süre doldu mu — sunucudan gelen kalan saate göre.</summary>
    public bool Gecikti => Durum == "ACIK" && KalanSaat < 0;

    /// <summary>Üç günden az kaldıysa uyarı.</summary>
    public bool Yaklasiyor => Durum == "ACIK" && KalanSaat >= 0 && KalanSaat < 72;
}

public sealed class SupheliOlusturDto
{
    public string? HareketId { get; set; }
    public string? CariId { get; set; }
    public string? KimlikTespitiId { get; set; }
    public decimal IslemTutariTl { get; set; }
    public string Gerekce { get; set; } = "";
}

public sealed record MasakOzet(
    int AcikBildirim, int SuresiGecmis, int Yaklasan, int Son30GunTespit);

/// <summary>
/// Sunucunun <c>MasakKuralMotoru.Gereksinim(tutar)</c> çıktısıyla BİREBİR.
/// <para>
/// ⚠ Bu kayıt, kademe mantığının <b>tek doğruluk kaynağının sunucu</b> olmasını
/// sağlar. Eskiden istemci (<c>MasakFormu</c>) hangi alanın hangi eşikte
/// gerektiğini KENDİ hesaplıyordu; sunucu kuralı değişirse istemci sessizce
/// sapardı (teknik borç TB-11).
/// </para>
/// </summary>
public sealed record MasakGereksinim(
    string Kademe, bool KimlikGerekli, bool AmacGerekli,
    bool BeyanFormuGerekli, bool EkBelgeGerekli, int AsgariAciklamaUzunlugu)
{
    /// <summary>PAKET YASAL-PARAM-EKRAN: kararın dayandığı tutar (bağlantılı işlemde bugünkü toplam). Eski sunucuda 0.</summary>
    public decimal OlcuTutarTl { get; init; }

    /// <summary>PAKET YASAL-PARAM-EKRAN: bu işlemin kendi tutarı (mutlak). Eski sunucuda 0.</summary>
    public decimal IslemTutarTl { get; init; }

    /// <summary>Karar bu işlemin tek başına tutarından değil, bugünkü toplamdan mı geldi?</summary>
    public bool ToplamdanMi => OlcuTutarTl > 0m && OlcuTutarTl > IslemTutarTl;
}

public sealed record SaklamaOnizleme(
    DateTime Simdi, SuresiDolmus SuresiDolmus, DateTime? EnYakinSaklamaBitisi, string Aciklama);

public sealed record SuresiDolmus(int KimlikTespiti, int Beyan);

public sealed record SaklamaSonuc(Maskelenen Maskelenen, DateTime Tarih);

public sealed record Maskelenen(int KimlikTespiti, int Beyan);

/// <summary>Liste satırı — TCKN <b>maskelidir</b>.</summary>
public sealed record PusulaSatir(
    string Id, string PusulaNo, string? HareketId, string SaticiTipi, string SaticiAdSoyad,
    string SaticiTckn, string AyarId, decimal GramAgirlik, decimal HasGrami,
    decimal BrutTutar, decimal StopajOran, decimal StopajTutar, decimal NetOdenen,
    DateTime TeslimTarihi, DateTime DuzenlemeSonTarihi,
    // PAKET YASAL-4 / Y4-A: süre YALNIZ iptal edilmiş + yerine yenisi olmayan
    // pusulada anlamlıdır; diğerlerinde sunucu null döner (süre karşılanmış).
    bool? Gecikti, int? KalanGun,
    // PAKET S-3b — sunucu (HurdaServisi.Listele) IptalEdildi bool · IptalGerekce
    // string? · IptalTarihi DateTime? döndürür; adlar ve TİPLER birebir.
    bool IptalEdildi = false, string? IptalGerekce = null, DateTime? IptalTarihi = null,
    string? DuzeltilenId = null, DateOnly? SonGun = null, bool YenidenDuzenlenebilir = false);

/// <summary>Tekil kayıt — TAM TCKN. Okuma izi düşer.</summary>
public sealed record PusulaDetay(
    string Id, string PusulaNo, string? HareketId, string? SubeId, string? CariId,
    string SaticiTipi, string SaticiAdSoyad, string SaticiTckn, string? SaticiAdres,
    string AyarId, decimal GramAgirlik, decimal HasGrami, string? MalAciklamasi,
    decimal BrutTutar, decimal StopajOran, decimal StopajTutar, decimal NetOdenen,
    DateTime TeslimTarihi, DateTime DuzenlemeSonTarihi, string? DuzeltilenId, DateTime Olusturuldu,
    bool IptalEdildi = false, string? IptalGerekce = null, DateTime? IptalTarihi = null);

// ── Yazdırılabilir belge (VUK md.234) ───────────────────────────────────────
// Sunucu VERİYİ döndürür, görsel render istemcide yapılır (ADR-0007):
// sunucuya PDF kütüphanesi eklenmez.

public sealed record PusulaBelge(
    string BelgeTuru, string Dayanak, string PusulaNo,
    DateTime DuzenlemeTarihi, DateTime TeslimTarihi, DateTime DuzenlemeSonTarihi, bool Gecikti,
    BelgeDuzenleyen Duzenleyen, BelgeSatici Satici, BelgeMal Mal, BelgeTutarlar Tutarlar,
    string Not);

/// <summary>Belgenin DÜZENLEYEN tarafı — işletme kimliği (VUK md.234).
/// Boş alanlar null gelir; ekran uydurmaz, "işletme kartından doldurun" der.</summary>
public sealed record BelgeDuzenleyen(
    string? Unvan, string? Sube,
    string? Adres = null, string? VergiDairesi = null,
    string? VergiNo = null, string? Telefon = null);

public sealed record BelgeSatici(
    string SaticiTipi, string SaticiTipiAciklama, string AdSoyad, string Tckn, string? Adres);

public sealed record BelgeMal(string Cins, decimal GramAgirlik, decimal HasGrami, string? Aciklama);

public sealed record BelgeTutarlar(
    decimal BrutTutar, decimal StopajOran, string StopajOranYuzde,
    decimal StopajTutar, decimal NetOdenen, string Yaziyla);

/// <summary>İptal sonucu — sunucu doğru düzeltme yolunu da döndürür (UX-S27).</summary>
public sealed record PusulaIptalSonuc(
    string Id, string PusulaNo, bool IptalEdildi, string? IptalGerekce,
    DateTime? IptalTarihi, string DuzeltmeYolu);

public sealed record SureBilgi(int Gun, string Dayanak);

/// <summary>PAKET YASAL-4 / Y4-A — süre takibi satırı (iptal edilmiş, yerine yenisi yok).</summary>
public sealed record SureKalemi(
    string Id, string PusulaNo, string? HareketId, DateTime TeslimTarihi, DateOnly SonGun,
    int KalanGun, decimal BrutTutar, decimal StopajTutar);

/// <summary>
/// PAKET YASAL-4 / Y4-A: Gecikmis = son günü geçmiş (pusulası kalmayan alış),
/// Yaklasan = yeniden düzenlenmeli. İptal edilmemiş pusula HİÇ girmez.
/// </summary>
public sealed record SureTakip(
    SureBilgi Sure, int GecikmisSayisi, int YaklasanSayisi,
    List<SureKalemi>? Gecikmis = null, List<SureKalemi>? Yaklasan = null, List<string>? Uyarilar = null,
    DateOnly? EnYakinSonGun = null, int Sinir = 0, bool SinirdaMi = false,
    // § 4.1 madde 2: MÜKELLEF satıcıdan alış, satıcı faturası no'su boş — geri sayımsız bilgi.
    int FaturaBekleyenSayisi = 0);

// ── Gün sonu POS–fatura mutabakatı ──────────────────────────────────────────

public sealed record Mutabakat(
    string Tarih, MutabakatTahsilat Tahsilat, MutabakatSatis Satis,
    MutabakatAlis Alis, MutabakatHurda Hurda, MutabakatFatura Fatura,
    decimal Fark, bool Mutabik, List<string> Uyarilar,
    List<FaturasizIslem> FaturasizIslemler,
    // UX-4d md.11: sunucu fatura↔fiş eşlemesini ve FARK tanımını döndürüyordu,
    // ekran okumuyordu. Eski yanıtlarda null gelebilir → ekran bölümü çizilmez.
    MutabakatFaturaEslesme? FaturaEslesme = null);

public sealed record MutabakatFaturaEslesme(
    int BagliSayisi, decimal BagliTl,
    int BagimsizSayisi, decimal BagimsizTl,
    decimal FaturasizTl, string? FarkTanimi,
    List<BagimsizFatura>? BagimsizFaturalar);

public sealed record BagimsizFatura(
    string Id, string FaturaNo, DateTime Tarih, decimal Toplam,
    string? CariId, string? Durum);

public sealed record MutabakatTahsilat(
    decimal Nakit, decimal Kart, decimal Havale, decimal Diger, decimal Toplam,
    // PARA-DEMO-2 (madde 5): toplama GİRMEYEN kasa girişleri (ayrı satır). Tanım cümlesi API'den
    // GELMEZ (tur 4): istemci sözlüğünden (Sozlukler.ParaDemo2.TahsilatTanimMetni, TR+EN) yazılır.
    decimal VirmanGiris = 0m, decimal AcilisDevri = 0m,
    // PAKET RAPOR-3 tur 2: dövizli kasa girişleri — TL toplamına GİRMEZ, para birimiyle ayrı satır (UX-S32).
    List<MutabakatDovizGiris>? DovizGiris = null);

public sealed record MutabakatDovizGiris(string ParaBirimi, decimal Tutar);

public sealed record MutabakatSatis(
    int Sayisi, decimal ToplamTl, int FaturaliSayisi, decimal FaturaliTl,
    int FaturasizSayisi, decimal FaturasizTl);

public sealed record MutabakatAlis(
    int Sayisi, decimal ToplamTl, int FaturaSayisi, decimal FaturaToplamTl);

public sealed record MutabakatHurda(
    int Sayisi, decimal OdenenTl, decimal HasGrami, int PusulaSayisi,
    decimal PusulaBrutTl, decimal StopajToplamTl, int BelgesizSayisi, int GecikmisPusulaSayisi);

public sealed record MutabakatFatura(
    int Sayisi, decimal ToplamTl, decimal KdvToplam, decimal SatisFaturasiTl);

public sealed record FaturasizIslem(
    string Id, DateTime Tarih, decimal TlTutari, string? OdemeTuru, string? CariId,
    List<string>? OdemeAraclari = null);   // PARA-DEMO-3: gerçek karmada bacak araçları ("Kart + Havale")

// ── PAKET 40-A — risk sözleşmesi ─────────────────────────────────────────────
// ⚠ TİP SÖZLEŞMESİ (QA tur 3 KRİTİK): sunucu `puan` INT döndürür → burada da
//   int. Ondalık bir alanı int okumak DeserializeUnableToConvertValue atar ve
//   Blazor render ağacını öldürür; sonraki kaydetmeler sessizce kaybolur.
// ⚠ Yanıtta kimlik belge numarası alanı YOKTUR ve olmayacaktır (Mehmet K40-3).

/// <summary>Tek profil satırı (append-only tarihçe kaydı).</summary>
public sealed record RiskProfilSatiri(
    string Id, string CariId, string Kaynak, int Puan, string Seviye,
    List<string> Sebepler, string? Gerekce, string KullaniciId,
    DateTime HesaplanmaTarihi, DateTime Olusturuldu);

/// <summary>GET /masak/risk/{cariId} yanıtı.</summary>
public sealed record RiskProfilYaniti(
    string CariId, string CariAd, bool PepBeyani, string? PepGorev, DateTime? PepBeyanTarihi,
    RiskProfilSatiri? Guncel, List<RiskProfilSatiri> Tarihce,
    bool KuralEtkin, int GerekceAsgari, int TazelemeGunu);

/// <summary>Hesaplama sırasında motora giren sayısal özet (ekranda gösterilir).</summary>
public sealed record RiskGirdiOzeti(
    bool PepBeyani, decimal NakitOran, decimal ToplamTl, int EsigeYakinIslem,
    int KimlikEksik, int SupheliBildirim, int PencereGun);

/// <summary>POST hesapla / elle yanıtı — UX-S27: sonuç banda yazılır.</summary>
public sealed record RiskHesapYaniti(
    string CariId, string CariAd, RiskProfilSatiri Profil,
    bool KuralEtkin, RiskGirdiOzeti? GirdiOzeti = null);

/// <summary>Cari seçicisi satırı — sunucunun cari listesinden yalnız 4 alan okunur.</summary>
public sealed record RiskCariSecenek(string Id, string Ad, string? RiskSeviye, bool PepBeyani);

/// <summary>Yüksek riskli cari satırı — kimlik verisi YOK.</summary>
public sealed record RiskliCari(
    string CariId, string CariAd, int Puan, string Seviye, string Kaynak,
    List<string> Sebepler, DateTime Olusturuldu);

/// <summary>GET /masak/risk/ozet yanıtı.</summary>
public sealed record RiskOzet(
    int Dusuk, int Orta, int Yuksek, int PepSayisi, int Tazelenecek, int Profilsiz,
    bool KuralEtkin, int ListeSiniri, List<RiskliCari> YuksekRiskliler);
