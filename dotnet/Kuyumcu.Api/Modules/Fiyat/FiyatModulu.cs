using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// Kategori alt ağacı TEK kaynaktan gelir (ürün listesi filtresiyle aynı kod).
using KategoriServisi = Kuyumcu.Api.Modules.Urun.KategoriServisi;
using Kuyumcu.Core.Sozlesmeler;

using Kuyumcu.Contracts.Fiyat;

namespace Kuyumcu.Api.Modules.Fiyat;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT — listeler, kampanyalar, teklif. Karar: PAKET-17-FIYAT-LISTESI-KARAR-2026-09-15.md
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FiyatListesiDto
{
    [Required, MaxLength(100)] public string Ad { get; set; } = null!;
    [RegularExpression("PERAKENDE|TOPTAN|OZEL")] public string Tur { get; set; } = "PERAKENDE";
    /// <summary>
    /// Has değerine eklenen yüzde. Negatif olabilir (maliyetine/indirimli liste) ama
    /// <b>sessizce değil</b>: rahat aralık dışı değer <see cref="RiskOnayi"/> ister
    /// (paket S-3c · kurul tur 3 engel 20).
    /// </summary>
    [Range(-100, 500)] public decimal HasMarjYuzde { get; set; }
    /// <summary>İşçiliğin uygulanan yüzdesi; 100 = aynen, 0 = işçilik alınmaz.</summary>
    [Range(0, 500)] public decimal IscilikCarpanYuzde { get; set; } = 100m;
    /// <summary>
    /// Rahat aralık dışı (zarar/aşırı fiyat üreten) değerlerin bilinçli onayı.
    /// Gönderilmezse ve değer riskliyse sunucu 400 döner — fail-closed.
    /// </summary>
    public bool? RiskOnayi { get; set; }
    /// <summary>
    /// PATCH sözleşmesi (UX-S26 / paket UX-4a madde 1): <b>gönderilmeyen alan KORUNUR</b>.
    /// <c>null</c> ya da <c>false</c> gelmesi mevcut varsayılanlığı DÜŞÜRMEZ; yalnız
    /// <c>true</c> bu listeyi varsayılan yapar (diğerlerinin varsayılanlığı düşer).
    /// Varsayılanlık kaldırmanın tek yolu BAŞKA bir listeyi varsayılan yapmaktır —
    /// aksi halde kiracı varsayılansız kalır ve POS hiçbir ürüne otomatik fiyat veremez.
    /// </summary>
    public bool? Varsayilan { get; set; }
    public bool? Aktif { get; set; }
}

public sealed class KampanyaDto
{
    /// <summary>PATCH-SOZLESME tur 3: genel aralık çevirisi üst sınırı 10⁹ üstünde "0,01 veya daha büyük"
    /// der (yanıltıcı) — üst sınır kolon kapasitesi olduğundan ileti elle yazılır.</summary>
    public const string KampanyaDegerAraligi = "İndirim değeri 0,01 ile 9.999.999.999.999,99 arasında olmalıdır";
    [Required, MaxLength(150)] public string Ad { get; set; } = null!;
    // PAKET GOC-C (envanter C km-bas/km-bit: "0001 ya da 2099 yazılabilir"): K8 ortak yıl penceresi (1900–2100);
    // ekran Tarih bileşeniyle AYNI Core sınıfı. 92 gün kuralı servis içinde (değişmedi).
    [Required, Common.Dogrulama.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Serbest)] public DateOnly Baslangic { get; set; }
    [Required, Common.Dogrulama.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Serbest)] public DateOnly Bitis { get; set; }
    [RegularExpression("TUMU|KATEGORI|URUN")] public string HedefTur { get; set; } = "TUMU";
    [MaxLength(36)] public string? HedefId { get; set; }
    [RegularExpression("ISCILIK_YUZDE|TUTAR_YUZDE|TUTAR_TL")] public string IndirimTuru { get; set; } = "ISCILIK_YUZDE";
    [Range(0.01, 10_000_000_000_000, ErrorMessage = KampanyaDegerAraligi)] public decimal Deger { get; set; }
    public bool? Aktif { get; set; }
}

// ── PAKET PATCH-SOZLESME (02.10) — liste/kampanya PATCH gövdeleri ────────────
// Eski hâl: PATCH /fiyat/listeler/{id} ve /fiyat/kampanyalar/{id} OLUŞTURMA
// DTO'suyla bağlanıyordu → yalnız {aktif:false} gönderen istemci 'Ad zorunlu'
// 400'ü alıyor, gönderilmeyen tür/marj/çarpan/tarih/indirim alanları ise
// DTO VARSAYILANLARIYLA (PERAKENDE, %0, %100, 0001-01-01, ISCILIK_YUZDE, 0)
// SESSİZCE EZİLİYORDU. Sözleşme: gövdede YOKSA KORU · null ise SİL (silinemeyen
// alan için alan altı 400) · doluysa doğrula ve yaz. Desen UrunFiyatiDto ile aynı.

/// <summary>PATCH /fiyat/listeler/{id} — kısmi güncelleme gövdesi.</summary>
public sealed class FiyatListesiGuncelleDto
{
    private string? _ad; private string? _tur; private decimal? _marj; private decimal? _carpan;

    [MaxLength(100)]
    public string? Ad { get => _ad; set { _ad = value; AdVerildi = true; } }
    [JsonIgnore] public bool AdVerildi { get; private set; }

    [RegularExpression("PERAKENDE|TOPTAN|OZEL")]
    public string? Tur { get => _tur; set { _tur = value; TurVerildi = true; } }
    [JsonIgnore] public bool TurVerildi { get; private set; }

    // PATCH-SOZLESME tur 2: tırnaklı sayı ("45.000") REDDEDİLİR — Türkçe biçimli dize
    // sessizce invariant okunuyordu (45 bin yerine 45). İstemci sayı gönderir.
    [Range(-100, 500), JsonNumberHandling(JsonNumberHandling.Strict)]
    public decimal? HasMarjYuzde { get => _marj; set { _marj = value; HasMarjYuzdeVerildi = true; } }
    [JsonIgnore] public bool HasMarjYuzdeVerildi { get; private set; }

    [Range(0, 500), JsonNumberHandling(JsonNumberHandling.Strict)]
    public decimal? IscilikCarpanYuzde { get => _carpan; set { _carpan = value; IscilikCarpanYuzdeVerildi = true; } }
    [JsonIgnore] public bool IscilikCarpanYuzdeVerildi { get; private set; }

    public bool? RiskOnayi { get; set; }
    /// <summary>Yalnız <c>true</c> etkilidir (bkz. <see cref="FiyatListesiDto.Varsayilan"/>).</summary>
    public bool? Varsayilan { get; set; }
    public bool? Aktif { get; set; }

    /// <summary>Tam gövdeden (iç çağrılar/testler) kısmi gövde — her alan "gönderildi".</summary>
    public static FiyatListesiGuncelleDto Tam(FiyatListesiDto d) => new()
    {
        Ad = d.Ad, Tur = d.Tur, HasMarjYuzde = d.HasMarjYuzde, IscilikCarpanYuzde = d.IscilikCarpanYuzde,
        RiskOnayi = d.RiskOnayi, Varsayilan = d.Varsayilan, Aktif = d.Aktif,
    };
}

/// <summary>PATCH /fiyat/kampanyalar/{id} — kısmi güncelleme gövdesi.</summary>
public sealed class KampanyaGuncelleDto
{
    private string? _ad; private DateOnly? _bas; private DateOnly? _bit; private string? _hedefTur;
    private string? _hedefId; private string? _indirimTuru; private decimal? _deger;

    [MaxLength(150)]
    public string? Ad { get => _ad; set { _ad = value; AdVerildi = true; } }
    [JsonIgnore] public bool AdVerildi { get; private set; }

    [Common.Dogrulama.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Serbest)]
    public DateOnly? Baslangic { get => _bas; set { _bas = value; BaslangicVerildi = true; } }
    [JsonIgnore] public bool BaslangicVerildi { get; private set; }

    [Common.Dogrulama.TarihPenceresi(Kuyumcu.Core.Domain.Girdi.TarihTuru.Serbest)]
    public DateOnly? Bitis { get => _bit; set { _bit = value; BitisVerildi = true; } }
    [JsonIgnore] public bool BitisVerildi { get; private set; }

    [RegularExpression("TUMU|KATEGORI|URUN")]
    public string? HedefTur { get => _hedefTur; set { _hedefTur = value; HedefTurVerildi = true; } }
    [JsonIgnore] public bool HedefTurVerildi { get; private set; }

    [MaxLength(36)]
    public string? HedefId { get => _hedefId; set { _hedefId = value; HedefIdVerildi = true; } }
    [JsonIgnore] public bool HedefIdVerildi { get; private set; }

    [RegularExpression("ISCILIK_YUZDE|TUTAR_YUZDE|TUTAR_TL")]
    public string? IndirimTuru { get => _indirimTuru; set { _indirimTuru = value; IndirimTuruVerildi = true; } }
    [JsonIgnore] public bool IndirimTuruVerildi { get; private set; }

    [Range(0.01, 10_000_000_000_000, ErrorMessage = KampanyaDto.KampanyaDegerAraligi), JsonNumberHandling(JsonNumberHandling.Strict)]
    public decimal? Deger { get => _deger; set { _deger = value; DegerVerildi = true; } }
    [JsonIgnore] public bool DegerVerildi { get; private set; }

    public bool? Aktif { get; set; }
}

// ── PAKET 49 — liste × ürün geçersiz kılma ve toplu uygulama ─────────────────

/// <summary>
/// Tekil geçersiz kılma gövdesi (POST + PATCH).
///
/// PATCH SÖZLEŞMESİ (UX-S26 / DoD): <b>gönderilmeyen alan KORUNUR, açık null SİLER</b>.
/// Üç durum KullaniciGuncelleDto'daki desenle çözülür (yeni bir üçlü-durum
/// mekanizması açılmaz): setter "gönderildi" bayrağını kaldırır.
/// </summary>
public sealed class UrunFiyatiDto
{
    private decimal? _sabitFiyat, _hasMarjYuzde, _iscilikCarpanYuzde;
    private string? _not;

    /// <summary>POST'ta zorunlu; PATCH'te yol parametresinden gelir.</summary>
    [MaxLength(36)] public string? UrunId { get; set; }

    [JsonIgnore] public bool SabitFiyatGonderildi { get; private set; }
    /// <summary>Doluysa liste fiyatı budur; marj/işçilik uygulanmaz.</summary>
    [Range(0.01, 100_000_000)]
    public decimal? SabitFiyat { get => _sabitFiyat; set { _sabitFiyat = value; SabitFiyatGonderildi = true; } }

    [JsonIgnore] public bool HasMarjYuzdeGonderildi { get; private set; }
    /// <summary>Listenin marjının YERİNE geçer; negatif olabilir (sert sınır: FiyatAraliklari).</summary>
    [Range(-100, 500)]
    public decimal? HasMarjYuzde { get => _hasMarjYuzde; set { _hasMarjYuzde = value; HasMarjYuzdeGonderildi = true; } }

    [JsonIgnore] public bool IscilikCarpanYuzdeGonderildi { get; private set; }
    /// <summary>Listenin işçilik çarpanının YERİNE geçer; 100 = aynen (sert sınır: FiyatAraliklari).</summary>
    [Range(0, 500)]
    public decimal? IscilikCarpanYuzde { get => _iscilikCarpanYuzde; set { _iscilikCarpanYuzde = value; IscilikCarpanYuzdeGonderildi = true; } }

    [JsonIgnore] public bool NotGonderildi { get; private set; }
    [MaxLength(200)] public string? Not { get => _not; set { _not = value; NotGonderildi = true; } }

    /// <summary>Pasife alma / yeniden aktifleştirme — kayıt SİLİNMEZ (denetim izi).</summary>
    public bool? Aktif { get; set; }

    /// <summary>
    /// Rahat aralık dışı (zarar/aşırı fiyat üreten) değerin bilinçli onayı —
    /// liste gövdesindeki alanla AYNI sözleşme. Gönderilmezse riskli değer 400.
    /// </summary>
    public bool? RiskOnayi { get; set; }
}

/// <summary>
/// Toplu marj/işçilik çarpanı uygulama gövdesi.
///
/// ⚠ ONAYSIZ YAZMA YOK: yazma çağrısı <see cref="ParmakIzi"/> ister; bu damga
/// önizleme yanıtından gelir ve sunucuda YENİDEN hesaplanır. Aradaki veri
/// değişmişse (başkası fiyat girmiş, ürün eklenmiş) damga tutmaz → 409.
/// "Kullanıcı gördüğü özeti onayladı" iddiası böylece sunucuda doğrulanır.
/// </summary>
public sealed class TopluUygulaDto
{
    [RegularExpression("SECILI|KATEGORI")] public string HedefTur { get; set; } = "SECILI";
    public List<string>? UrunIdler { get; set; }
    [MaxLength(36)] public string? KategoriId { get; set; }
    [Range(-100, 500)] public decimal? HasMarjYuzde { get; set; }
    [Range(0, 500)] public decimal? IscilikCarpanYuzde { get; set; }
    /// <summary>Rahat aralık dışı değerin bilinçli onayı (yazma çağrısında zorunlu).</summary>
    public bool? RiskOnayi { get; set; }
    [Required, MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [MaxLength(64)] public string? ParmakIzi { get; set; }
    [MaxLength(200)] public string? Not { get; set; }
}

/// <summary>Toplu uygulamada bir ürünün NEDEN atlandığı — sessiz etkisizlik yasak (UX-4 md.4).</summary>
public static class AtlamaSebebi
{
    /// <summary>Gümüş ürüne marj/işçilik türetmesi uygulanmaz (Q3 mührü).</summary>
    public const string Gumus = "GUMUS";
    /// <summary>Ürün kartında sabit satış fiyatı var; marj değişikliği fiyatı DEĞİŞTİRMEZ.</summary>
    public const string SabitFiyat = "URUN_SABIT_FIYAT";
    /// <summary>Bu ürüne sabit fiyatlı geçersiz kılma tanımlı; marj yine etkisiz kalırdı.</summary>
    public const string GecersizKilmaSabit = "GECERSIZ_KILMA_SABIT_FIYAT";
    /// <summary>Has kuru okunamadı ya da 0 — fiyat üretilemez (fail-closed).</summary>
    public const string FiyatYok = "FIYAT_YOK";

    /// <summary>
    /// ALTIN DIŞI madenin kotasyonu YOK (paket 20, K20-4). Başka bir madenin
    /// kuru ASLA ikame edilmez; ürüne sabit satış fiyatı girilmelidir.
    /// </summary>
    public const string KotasyonYok = "KOTASYON_YOK";
}

public sealed class FiyatServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    // ── Listeler ─────────────────────────────────────────────────────────────

    public Task<List<FiyatListesi>> Listeler(AktifKullanici islem) =>
        db.FiyatListeleri.AsNoTracking()
            .Where(f => f.TenantId == islem.TenantId)
            .OrderByDescending(f => f.Varsayilan).ThenBy(f => f.Ad).ToListAsync();

    // ── Makul aralık kapısı (paket S-3c · kurul tur 3 engel 20) ─────────────
    //
    // QA turu 3: "Has marjı −5 ve işçilik çarpanı 500 uyarısız kabul ediliyor."
    // Negatif marj İŞ OLARAK meşrudur (maliyetine/indirimli liste) — yasaklanmaz;
    // ama SESSİZ olamaz: rahat aralık dışı değer BİLİNÇLİ onay ister (riskOnayi).
    // Sert sınır dışı değer hiç kabul edilmez. Kapı SERVİSTE durur: DataAnnotations
    // yalnız denetleyici yolunda çalışır, servis çağıranı korumaz.
    /// <summary>
    /// Rahat aralık dışıysa Türkçe gerekçe döner, içindeyse <c>null</c>.
    /// Eşikler TEK kaynaktan gelir: <see cref="FiyatAraliklari"/> (ekran da onu kullanır).
    /// </summary>
    public static string? RiskGerekcesi(decimal marj, decimal carpan) =>
        RiskMesaji(FiyatAraliklari.RiskKodu(marj, carpan));

    /// <summary>Risk kodunun Türkçe gerekçesi; kod <c>null</c> ise <c>null</c>.</summary>
    private static string? RiskMesaji(string? kod) =>
        kod switch
        {
            "MARJ_EKSI" => "Has marjı eksi: bu liste satışı maliyetin altına indirir",
            "MARJ_YUKSEK" => "Has marjı %100'ün üzerinde: fiyat has değerinin iki katını aşar",
            "CARPAN_YUKSEK" => "İşçilik çarpanı %200'ün üzerinde: işçilik iki katından fazla yansır",
            _ => null,
        };

    /// <summary>
    /// Sert sınır + risk onayı kapısı. <paramref name="mevcut"/> verilirse yalnız
    /// DEĞİŞEN riskli değer onay ister (var olan kaydın aktif/pasif edilmesi,
    /// eski riskli değeri yeniden onaylatmaz).
    /// </summary>
    private static void AraligiDogrula(FiyatListesiDto dto, FiyatListesi? mevcut) =>
        AraligiDogrula(dto.HasMarjYuzde, dto.IscilikCarpanYuzde, dto.RiskOnayi,
            mevcut?.HasMarjYuzde, mevcut?.IscilikCarpanYuzde);

    /// <summary>
    /// ORTAK KAPI — liste, ürüne özel fiyat ve TOPLU uygulama yollarının hepsi
    /// buradan geçer (S-3c ikinci dilim).
    ///
    /// ⚠ NEDEN ORTAK: kapı ilk sürümde yalnız ListeOlustur/ListeGuncelle'ye
    /// konmuştu; aynı iki alan üç yazma yolundan daha (ürün fiyatı ekle/güncelle,
    /// toplu uygula) uyarısız yazılabiliyordu — toplu uygulama tek istekte
    /// yüzlerce ürünü etkilediği için sızıntı kapatılan yoldan daha zararlıydı.
    ///
    /// Kısmi gövdede verilmeyen alan NÖTR sayılır (FiyatAraliklari.*Kismi):
    /// "gönderilmedi" = "listeden devral", risk değil.
    /// <paramref name="oncekiMarj"/>/<paramref name="oncekiCarpan"/> verilirse
    /// DEĞİŞMEYEN riskli değer yeniden onay istemez (var olan kayıt kilitlenmez).
    /// </summary>
    private static void AraligiDogrula(decimal? marj, decimal? carpan, bool? riskOnayi,
        decimal? oncekiMarj = null, decimal? oncekiCarpan = null)
    {
        switch (FiyatAraliklari.SertSinirDisiKismi(marj, carpan))
        {
            case "MARJ":
                throw new BadRequestException(
                    $"Has marjı %{FiyatAraliklari.MarjSertAlt:0.##} ile %{FiyatAraliklari.MarjSertUst:0.##} arasında olmalıdır");
            case "CARPAN":
                throw new BadRequestException(
                    $"İşçilik çarpanı %{FiyatAraliklari.CarpanSertAlt:0.##} ile %{FiyatAraliklari.CarpanSertUst:0.##} arasında olmalıdır");
        }

        // ⚠ HER riskli alan ADIYLA söylenir: tek kod döndürmek "çarpanı 480 yaptım,
        // bana marjdan bahsediyor" gibi yanlış alanı işaret ederdi (canlı doğrulamada
        // görüldü). Alanlar TEK TEK sorgulanır; diğeri nötr değerle susturulur.
        var gerekceler = new List<string>();
        if (RiskMesaji(FiyatAraliklari.RiskKoduKismi(marj, null)) is { } g1) gerekceler.Add(g1);
        if (RiskMesaji(FiyatAraliklari.RiskKoduKismi(null, carpan)) is { } g2) gerekceler.Add(g2);
        var gerekce = gerekceler.Count == 0 ? null : string.Join("; ", gerekceler);
        if (gerekce is null) return;
        // Değer hiç değişmediyse (yalnız ad/not/aktiflik güncelleniyorsa) yeniden onay istenmez.
        if (marj == oncekiMarj && carpan == oncekiCarpan) return;
        if (riskOnayi == true) return;
        throw new BadRequestException(
            $"{gerekce}. Devam etmek için riskOnayi alanını işaretleyin");
    }

    public async Task<FiyatListesi> ListeOlustur(FiyatListesiDto dto, AktifKullanici islem)
    {
        AraligiDogrula(dto, null);
        var now = DateTime.UtcNow;
        var liste = new FiyatListesi
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Ad = dto.Ad.Trim(), Tur = dto.Tur,
            HasMarjYuzde = dto.HasMarjYuzde, IscilikCarpanYuzde = dto.IscilikCarpanYuzde,
            Varsayilan = dto.Varsayilan == true, Aktif = dto.Aktif ?? true, Olusturuldu = now, Guncellendi = now,
        };
        // Kiracının HİÇ listesi yoksa ilk liste varsayılandır — aksi halde fiyat
        // teklifi hiçbir cariye üretilemez ve POS otomatik fiyat vermezdi.
        if (!await db.FiyatListeleri.AnyAsync(f => f.TenantId == islem.TenantId)) liste.Varsayilan = true;
        if (liste.Varsayilan) await VarsayilaniDusur(islem.TenantId, now);
        db.FiyatListeleri.Add(liste);
        await db.SaveChangesAsync();
        return liste;
    }

    /// <summary>Tam gövdeli iç çağrı (testler/eski yollar) — kısmi sürüme her alan "gönderildi" olarak iner.</summary>
    public Task<FiyatListesi> ListeGuncelle(string id, FiyatListesiDto dto, AktifKullanici islem) =>
        ListeGuncelle(id, FiyatListesiGuncelleDto.Tam(dto), islem);

    public async Task<FiyatListesi> ListeGuncelle(string id, FiyatListesiGuncelleDto dto, AktifKullanici islem)
    {
        // Kiracının TÜM listeleri izlenerek yüklenir: varsayılanlık bir KİRACI
        // değişmezidir (tam olarak bir aktif varsayılan), tek satıra bakarak
        // doğrulanamaz. Liste sayısı kiracı başına birkaç tanedir.
        var listeler = await db.FiyatListeleri.Where(f => f.TenantId == islem.TenantId).ToListAsync();
        var liste = listeler.FirstOrDefault(f => f.Id == id)
            ?? throw new NotFoundException("Fiyat listesi bulunamadı");

        // PATCH-SOZLESME: gönderilmeyen alan KORUNUR (mevcut değerle birleşir); açık null
        // silinemeyen alanda 400. DataAnnotations yalnız controller yolunda koşar → ELLE de denetlenir.
        var ad = liste.Ad;
        if (dto.AdVerildi)
        {
            ad = (dto.Ad ?? "").Trim();
            if (ad.Length == 0) throw new BadRequestException("Liste adı boş bırakılamaz").AlanIle("ad");
            if (ad.Length > 100) throw new BadRequestException("Liste adı en fazla 100 karakter olabilir").AlanIle("ad");
        }
        var tur = liste.Tur;
        if (dto.TurVerildi)
        {
            if (dto.Tur is not ("PERAKENDE" or "TOPTAN" or "OZEL"))
                throw new BadRequestException("Liste türü PERAKENDE, TOPTAN ya da OZEL olmalıdır").AlanIle("tur");
            tur = dto.Tur;
        }
        if (dto.HasMarjYuzdeVerildi && dto.HasMarjYuzde is null)
            throw new BadRequestException("Has marjı silinemez; 0 yazabilirsiniz").AlanIle("hasMarjYuzde");
        if (dto.IscilikCarpanYuzdeVerildi && dto.IscilikCarpanYuzde is null)
            throw new BadRequestException("İşçilik çarpanı silinemez; 100 yazabilirsiniz").AlanIle("iscilikCarpanYuzde");
        var marj = dto.HasMarjYuzdeVerildi ? dto.HasMarjYuzde!.Value : liste.HasMarjYuzde;
        var carpan = dto.IscilikCarpanYuzdeVerildi ? dto.IscilikCarpanYuzde!.Value : liste.IscilikCarpanYuzde;
        AraligiDogrula(marj, carpan, dto.RiskOnayi, liste.HasMarjYuzde, liste.IscilikCarpanYuzde);
        var now = DateTime.UtcNow;
        var varsayilanIstendi = dto.Varsayilan == true;
        if (varsayilanIstendi && !liste.Varsayilan)
            foreach (var f in listeler.Where(f => f.Varsayilan)) { f.Varsayilan = false; f.Guncellendi = now; }
        // Varsayılan liste pasife alınamaz: cariye liste atanmamış her satış fiyatsız kalırdı.
        if (dto.Aktif == false && (liste.Varsayilan || varsayilanIstendi))
            throw new BadRequestException("Varsayılan fiyat listesi pasife alınamaz; önce başka bir listeyi varsayılan yapın");
        liste.Ad = ad; liste.Tur = tur;
        liste.HasMarjYuzde = marj; liste.IscilikCarpanYuzde = carpan;
        // ⚠ MADDE 1 (paket UX-4a) — KORUMA SATIRI. Eski kod
        //     liste.Varsayilan = dto.Varsayilan || liste.Varsayilan && dto.Varsayilan;
        // yazıyordu: gövdede varsayilan=false gelince varsayılanlık SESSİZCE düşüyor,
        // ListeCoz null dönüyor ve POS'un otomatik fiyatlandırması komple duruyordu.
        // Doğrusu UX-S26: gönderilmeyen/false alan KORUNUR, yalnız true atar.
        if (varsayilanIstendi) liste.Varsayilan = true;
        if (dto.Aktif is { } a) liste.Aktif = a;
        liste.Guncellendi = now;
        // Fail-closed: bu güncellemeden sonra kiracıda AKTİF bir varsayılan liste kalmalı.
        // (Bellekteki liste üzerinden bakılır — değişiklikler henüz kaydedilmedi.)
        if (!listeler.Any(f => f.Varsayilan && f.Aktif))
            throw new BadRequestException(
                "Bu değişiklik kiracıyı varsayılan fiyat listesiz bırakır; önce bir listeyi varsayılan ve aktif yapın");
        await db.SaveChangesAsync();
        return liste;
    }

    private async Task VarsayilaniDusur(string tenantId, DateTime now)
    {
        foreach (var f in await db.FiyatListeleri.Where(f => f.TenantId == tenantId && f.Varsayilan).ToListAsync())
        { f.Varsayilan = false; f.Guncellendi = now; }
    }

    // ── Kampanyalar ──────────────────────────────────────────────────────────

    public Task<List<Kampanya>> Kampanyalar(AktifKullanici islem) =>
        db.Kampanyalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .OrderByDescending(k => k.Baslangic).ToListAsync();

    public async Task<Kampanya> KampanyaOlustur(KampanyaDto dto, AktifKullanici islem)
    {
        await KampanyaDogrula(dto, islem.TenantId);
        var now = DateTime.UtcNow;
        var k = new Kampanya
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, Ad = dto.Ad.Trim(),
            Baslangic = dto.Baslangic, Bitis = dto.Bitis, HedefTur = dto.HedefTur,
            HedefId = dto.HedefTur == "TUMU" ? null : dto.HedefId,
            IndirimTuru = dto.IndirimTuru, Deger = dto.Deger, Aktif = dto.Aktif ?? true,
            Olusturuldu = now, Guncellendi = now,
        };
        db.Kampanyalar.Add(k);
        await db.SaveChangesAsync();
        return k;
    }

    public async Task<Kampanya> KampanyaGuncelle(string id, KampanyaGuncelleDto g, AktifKullanici islem)
    {
        var k = await db.Kampanyalar.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Kampanya bulunamadı");
        // PATCH-SOZLESME: gönderilmeyen alan mevcut kayıttan gelir; birleşik kayıt
        // OLUŞTURMA ile AYNI kurallardan (KampanyaDogrula) geçer. Silinemeyen alana null → 400.
        var ad = k.Ad;
        if (g.AdVerildi)
        {
            ad = (g.Ad ?? "").Trim();
            if (ad.Length == 0) throw new BadRequestException("Kampanya adı boş bırakılamaz").AlanIle("ad");
            if (ad.Length > 150) throw new BadRequestException("Kampanya adı en fazla 150 karakter olabilir").AlanIle("ad");
        }
        var dto = new KampanyaDto
        {
            Ad = ad,
            Baslangic = !g.BaslangicVerildi ? k.Baslangic : g.Baslangic ?? throw Silinemez("baslangic", "Başlangıç tarihi"),
            Bitis = !g.BitisVerildi ? k.Bitis : g.Bitis ?? throw Silinemez("bitis", "Bitiş tarihi"),
            HedefTur = !g.HedefTurVerildi ? k.HedefTur : g.HedefTur ?? throw Silinemez("hedefTur", "Kampanya hedefi"),
            HedefId = g.HedefIdVerildi ? g.HedefId : k.HedefId,
            IndirimTuru = !g.IndirimTuruVerildi ? k.IndirimTuru : g.IndirimTuru ?? throw Silinemez("indirimTuru", "İndirim türü"),
            Deger = !g.DegerVerildi ? k.Deger : g.Deger ?? throw Silinemez("deger", "İndirim değeri"),
            Aktif = g.Aktif,
        };
        // DataAnnotations yalnız controller yolunda → kalıp/aralık burada da elle.
        if (dto.HedefTur is not ("TUMU" or "KATEGORI" or "URUN"))
            throw new BadRequestException("Kampanya hedefi TUMU, KATEGORI ya da URUN olmalıdır").AlanIle("hedefTur");
        if (dto.IndirimTuru is not ("ISCILIK_YUZDE" or "TUTAR_YUZDE" or "TUTAR_TL"))
            throw new BadRequestException("İndirim türü geçersiz").AlanIle("indirimTuru");
        if (dto.Deger < 0.01m)
            throw new BadRequestException("İndirim değeri sıfırdan büyük olmalıdır").AlanIle("deger");
        await KampanyaDogrula(dto, islem.TenantId);
        k.Ad = dto.Ad.Trim(); k.Baslangic = dto.Baslangic; k.Bitis = dto.Bitis;
        k.HedefTur = dto.HedefTur; k.HedefId = dto.HedefTur == "TUMU" ? null : dto.HedefId;
        k.IndirimTuru = dto.IndirimTuru; k.Deger = dto.Deger;
        if (dto.Aktif is { } a) k.Aktif = a;
        k.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return k;
    }

    private static BadRequestException Silinemez(string alan, string ad) =>
        new BadRequestException($"{ad} silinemez").AlanIle(alan);

    private async Task KampanyaDogrula(KampanyaDto dto, string tenantId)
    {
        if (dto.Bitis < dto.Baslangic)
            throw new BadRequestException("Kampanya bitişi başlangıçtan önce olamaz");
        // FEY md.11/(3): indirimli satış süresi en fazla üç ay.
        if (dto.Bitis.DayNumber - dto.Baslangic.DayNumber + 1 > Kampanya.EnFazlaGun)
            throw new BadRequestException(
                $"Kampanya süresi en fazla {Kampanya.EnFazlaGun} gün olabilir (Fiyat Etiketi Yönetmeliği md.11/3 — üç ay)");
        // PATCH-SOZLESME tur 3 (hasım KRİTİK): kolon kampanyalar.deger decimal(15,2) —
        // taşan değer eskiden veritabanı hatası 500 dönüyordu. POST ve PATCH bu kapıdan geçer.
        KolonTavani.Denetle(dto.Deger, 2, KolonTavani.D15_2, "deger", "İndirim değeri");
        if (dto.IndirimTuru is "ISCILIK_YUZDE" or "TUTAR_YUZDE" && dto.Deger > 100m)
            throw new BadRequestException("Yüzde indirim 100'ü aşamaz");
        switch (dto.HedefTur)
        {
            case "URUN":
                if (string.IsNullOrEmpty(dto.HedefId) ||
                    !await db.Urunler.AnyAsync(u => u.Id == dto.HedefId && u.TenantId == tenantId))
                    throw new BadRequestException("Kampanya hedefi ürün bulunamadı");
                break;
            case "KATEGORI":
                if (string.IsNullOrEmpty(dto.HedefId) ||
                    !await db.UrunKategorileri.AnyAsync(c => c.Id == dto.HedefId && c.TenantId == tenantId))
                    throw new BadRequestException("Kampanya hedefi kategori bulunamadı");
                break;
        }
    }

    // ── Teklif ───────────────────────────────────────────────────────────────

    /// <summary>Cariye atanmış liste; yoksa varsayılan; o da yoksa null (liste tanımlanmamış).</summary>
    public async Task<FiyatListesi?> ListeCoz(string tenantId, string? cariId)
    {
        if (!string.IsNullOrEmpty(cariId))
        {
            var listeId = await db.Cariler.AsNoTracking()
                .Where(c => c.Id == cariId && c.TenantId == tenantId)
                .Select(c => c.FiyatListesiId).FirstOrDefaultAsync();
            if (listeId is not null)
            {
                var atanan = await db.FiyatListeleri.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == listeId && f.TenantId == tenantId && f.Aktif);
                if (atanan is not null) return atanan;
            }
        }
        return await db.FiyatListeleri.AsNoTracking()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Varsayilan && f.Aktif);
    }

    /// <summary>Ürünün kategorisinden köke kadar kimlikler (kampanya kategori hedefi alt dalları da kapsar).</summary>
    public async Task<HashSet<string>> KategoriZinciri(Data.Urun urun) =>
        KategoriZinciri(urun, await KategoriUstHaritasi(urun.TenantId));

    /// <summary>Kiracının kategori → üst kategori haritası (önizleme N ürünü tek sorguyla çözer).</summary>
    public async Task<Dictionary<string, string?>> KategoriUstHaritasi(string tenantId) =>
        await db.UrunKategorileri.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToDictionaryAsync(c => c.Id, c => c.UstId);

    /// <summary>Zincir hesabının TEK gövdesi — DB'li sürüm de bunu çağırır (ikizlenmiş kural yok).</summary>
    public static HashSet<string> KategoriZinciri(Data.Urun urun, IReadOnlyDictionary<string, string?> ustler)
    {
        var zincir = new HashSet<string>();
        var id = urun.KategoriId;
        for (var i = 0; id is not null && i < 32; i++)          // döngü koruması
        {
            if (!zincir.Add(id)) break;
            id = ustler.TryGetValue(id, out var ust) ? ust : null;
        }
        return zincir;
    }

    /// <summary>
    /// Ürün için SUNUCU teklifi. <paramref name="hasKuru"/> işlem anındaki kur kaydından
    /// gelir. Liste yoksa null döner — ekran "fiyat listesi tanımlanmamış" der, uydurmaz.
    /// </summary>
    public async Task<FiyatTeklifi?> Teklif(Data.Urun urun, string? cariId, decimal hasKuru, DateOnly bugun)
    {
        var liste = await ListeCoz(urun.TenantId, cariId);
        return liste is null ? null : await TeklifListeyle(urun, liste, hasKuru, bugun);
    }

    /// <summary>Kiracının belirli bir listesi — paket UX-4a madde 3 önizleme paneli için.</summary>
    public Task<FiyatListesi?> ListeGetir(string tenantId, string listeId) =>
        db.FiyatListeleri.AsNoTracking().FirstOrDefaultAsync(f => f.Id == listeId && f.TenantId == tenantId);

    /// <summary>
    /// Teklif — liste ÇAĞIRAN tarafından çözülmüş hâli. POS <see cref="Teklif"/> ile
    /// cariye göre çözer; Fiyatlandırma ekranı seçtiği listeyi doğrudan verir
    /// (aksi halde panel "bu listeyle" derken varsayılan listenin fiyatını basardı).
    /// </summary>
    public async Task<FiyatTeklifi?> TeklifListeyle(Data.Urun urun, FiyatListesi liste, decimal hasKuru, DateOnly bugun)
    {
        var zincir = await KategoriZinciri(urun);
        var adaylar = await db.Kampanyalar.AsNoTracking()
            .Where(k => k.TenantId == urun.TenantId && k.Aktif && k.Baslangic <= bugun && k.Bitis >= bugun)
            .ToListAsync();
        // Paket 49: liste × ürün geçersiz kılması motorun GİRDİSİDİR; POS da bu
        // yoldan geçtiği için ikinci bir hesap yolu açılmaz (tek motor ilkesi).
        var gk = await GecersizKilma(urun.TenantId, liste.Id, urun.Id);
        return TeklifHesapla(urun, liste, hasKuru, bugun, adaylar, zincir, gk);
    }

    /// <summary>Kampanya seçimi + motor çağrısı — önizleme ve teklif AYNI gövdeyi kullanır.</summary>
    public static FiyatTeklifi? TeklifHesapla(
        Data.Urun urun, FiyatListesi liste, decimal hasKuru, DateOnly bugun,
        IReadOnlyList<Kampanya> adaylar, IReadOnlySet<string> zincir, FiyatListesiUrunFiyati? gecersizKilma)
    {
        var uygunlar = adaylar.Where(k => FiyatMotoru.Uygulanir(k, bugun, urun, zincir)).ToList();
        var enIyi = FiyatMotoru.EnIyi(uygunlar, urun, hasKuru, liste, gecersizKilma);
        return FiyatMotoru.Hesapla(urun, hasKuru, liste, enIyi, gecersizKilma);
    }

    /// <summary>Bu liste × ürün kesişimindeki AKTİF geçersiz kılma; yoksa null.</summary>
    public Task<FiyatListesiUrunFiyati?> GecersizKilma(string tenantId, string listeId, string urunId) =>
        db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ListeId == listeId
                                   && x.UrunId == urunId && x.Aktif);

    /// <summary>
    /// TRHTUY md.14 / Kılavuz md.7-3: "indirimden önceki fiyat" = indirimin uygulandığı
    /// tarihten önceki 30 gün içinde uygulanan EN DÜŞÜK fiyat. Kaynağı defterdeki
    /// <see cref="Hareket.ListeFiyati"/> satırlarıdır; 30 günde satış yoksa cari liste
    /// fiyatı esas alınır (Kılavuz md.7-12). Has bileşeni kurla değişir — karşılaştırma
    /// satış anında UYGULANAN liste fiyatı üzerindendir; bu bilinçli ve belgelidir.
    /// </summary>
    public async Task<decimal> IndirimOncesiFiyat(string tenantId, string urunId, decimal cariListeFiyati, DateTime anUtc)
    {
        var bas = anUtc.AddDays(-30);
        var enDusuk = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.UrunId == urunId && h.Tur == "SATIS"
                     && h.ListeFiyati != null && h.Tarih >= bas && h.Tarih < anUtc)
            .MinAsync(h => (decimal?)h.ListeFiyati);
        return enDusuk is { } e ? Math.Min(e, cariListeFiyati) : cariListeFiyati;
    }

    // ── PAKET 49 — liste × ürün görünümü ─────────────────────────────────────

    /// <summary>Liste kiracıya ait mi — yoksa 404 (başka kiracının listesi de "yok"tur).</summary>
    private async Task<FiyatListesi> ListeZorunlu(string tenantId, string listeId) =>
        await db.FiyatListeleri.AsNoTracking().FirstOrDefaultAsync(f => f.Id == listeId && f.TenantId == tenantId)
            ?? throw new NotFoundException("Fiyat listesi bulunamadı");

    private async Task<decimal> HasKuru(string tenantId, string kurSnapshotId) =>
        await db.KurSnapshots.AsNoTracking()
            .Where(k => k.Id == kurSnapshotId && k.TenantId == tenantId)
            .Select(k => (decimal?)k.AltinHasFiyati).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Kur kaydı bulunamadı");

    /// <summary>
    /// ÖNİZLEME — SALT HESAP: hiçbir satır yazmaz. Listenin ürünlere ETKİSİNİ
    /// (UX-S23) sunucuda üretir; ekran formülü değil kırılımı görür (PANO-KARAR §88).
    /// </summary>
    public async Task<OnizlemeSonucu> ListeOnizleme(
        string listeId, string kurSnapshotId, AktifKullanici islem,
        string? kategoriId = null, string? q = null, bool yalnizGecersizKilinan = false,
        int limit = 50, int offset = 0)
    {
        var liste = await ListeZorunlu(islem.TenantId, listeId);
        var hasKuru = await HasKuru(islem.TenantId, kurSnapshotId);
        limit = Math.Clamp(limit, 1, EnFazlaSatir);
        offset = Math.Max(offset, 0);

        var sorgu = db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId && u.Aktif);
        if (!string.IsNullOrWhiteSpace(kategoriId))
        {
            // Kategori hedefi ALT dalları da kapsar — ürün listesindeki kodun AYNISI.
            var dal = await new KategoriServisi(db).AltAgacIdleri(kategoriId, islem.TenantId);
            sorgu = sorgu.Where(u => u.KategoriId != null && dal.Contains(u.KategoriId));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var ara = q.Trim();
            sorgu = sorgu.Where(u => u.Kod.Contains(ara) || u.Ad.Contains(ara));
        }
        if (yalnizGecersizKilinan)
        {
            var kilinanlar = db.FiyatListesiUrunFiyatlari
                .Where(x => x.TenantId == islem.TenantId && x.ListeId == listeId && x.Aktif)
                .Select(x => x.UrunId);
            sorgu = sorgu.Where(u => kilinanlar.Contains(u.Id));
        }

        var toplam = await sorgu.CountAsync();
        var urunler = await sorgu.OrderBy(u => u.Kod).Skip(offset).Take(limit).ToListAsync();
        var satirlar = await SatirlariKur(urunler, liste, hasKuru, islem.TenantId);
        return new OnizlemeSonucu(toplam, limit, offset, satirlar);
    }

    /// <summary>Önizleme satırlarının tek gövdesi — toplu uygulama da bunu kullanır.</summary>
    private async Task<List<OnizlemeSatiri>> SatirlariKur(
        List<Data.Urun> urunler, FiyatListesi liste, decimal hasKuru, string tenantId)
    {
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
        var adaylar = await db.Kampanyalar.AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.Aktif && k.Baslangic <= bugun && k.Bitis >= bugun)
            .ToListAsync();
        var ustler = await KategoriUstHaritasi(tenantId);
        var idler = urunler.Select(u => u.Id).ToList();
        var kilinanlar = await db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ListeId == liste.Id && idler.Contains(x.UrunId))
            .ToListAsync();

        var satirlar = new List<OnizlemeSatiri>(urunler.Count);
        foreach (var u in urunler)
        {
            var gk = kilinanlar.FirstOrDefault(x => x.UrunId == u.Id);
            var aktifGk = gk is { Aktif: true } ? gk : null;
            var t = TeklifHesapla(u, liste, hasKuru, bugun, adaylar, KategoriZinciri(u, ustler), aktifGk);
            var ozet = gk is null ? null
                : new GecersizKilmaOzeti(gk.SabitFiyat, gk.HasMarjYuzde, gk.IscilikCarpanYuzde, gk.Not, gk.Aktif);
            satirlar.Add(t is null
                ? new OnizlemeSatiri(u.Id, u.Kod, u.Ad, u.AyarId, u.GramAgirlik, u.HasGrami, u.IscilikTl,
                    false, u.AyarId.StartsWith('G') ? AtlamaSebebi.Gumus : AtlamaSebebi.FiyatYok,
                    0m, 0m, 0m, 0m, 0m, 0m, null, FiyatKaynagi.Liste, ozet)
                : new OnizlemeSatiri(u.Id, u.Kod, u.Ad, u.AyarId, u.GramAgirlik, u.HasGrami, u.IscilikTl,
                    true, null, t.HasDegeri, t.MarjTl, t.IscilikTl, t.ListeFiyati, t.IndirimTutari,
                    t.SatisFiyati, t.KampanyaId, t.Kaynak, ozet, t.MaliyetTl));
        }
        return satirlar;
    }

    /// <summary>Listedeki geçersiz kılma KAYITLARI (ham) — sayfalı; ekranın süzgeci değil denetimin kaynağı.</summary>
    public async Task<UrunFiyatiSayfasi> UrunFiyatlari(
        string listeId, AktifKullanici islem, bool sadeceAktif = true, int limit = 50, int offset = 0)
    {
        await ListeZorunlu(islem.TenantId, listeId);
        limit = Math.Clamp(limit, 1, EnFazlaSatir);
        offset = Math.Max(offset, 0);
        var sorgu = db.FiyatListesiUrunFiyatlari.AsNoTracking()
            .Where(x => x.TenantId == islem.TenantId && x.ListeId == listeId);
        if (sadeceAktif) sorgu = sorgu.Where(x => x.Aktif);
        var toplam = await sorgu.CountAsync();
        var kayitlar = await sorgu.OrderBy(x => x.UrunId).Skip(offset).Take(limit).ToListAsync();
        return new UrunFiyatiSayfasi(toplam, limit, offset, kayitlar);
    }

    /// <summary>Sayfa başına en fazla satır — UX-S29: sınır ekranda yazar.</summary>
    public const int EnFazlaSatir = 200;

    public async Task<FiyatListesiUrunFiyati> UrunFiyatiEkle(string listeId, UrunFiyatiDto dto, AktifKullanici islem)
    {
        await ListeZorunlu(islem.TenantId, listeId);
        if (string.IsNullOrWhiteSpace(dto.UrunId)) throw new BadRequestException("Ürün seçilmedi");
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.UrunId && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");
        if (await db.FiyatListesiUrunFiyatlari
                .AnyAsync(x => x.TenantId == islem.TenantId && x.ListeId == listeId && x.UrunId == urun.Id))
            throw new ConflictException("Bu listede bu ürünün fiyatı zaten tanımlı; güncelleyin");
        AraligiDogrula(dto.HasMarjYuzde, dto.IscilikCarpanYuzde, dto.RiskOnayi);

        var now = DateTime.UtcNow;
        var kayit = new FiyatListesiUrunFiyati
        {
            Id = Kimlik.Ulid(), TenantId = islem.TenantId, ListeId = listeId, UrunId = urun.Id,
            SabitFiyat = dto.SabitFiyat, HasMarjYuzde = dto.HasMarjYuzde,
            IscilikCarpanYuzde = dto.IscilikCarpanYuzde, Not = dto.Not?.Trim(),
            Aktif = dto.Aktif ?? true, Olusturuldu = now, Guncellendi = now,
        };
        Dogrula(kayit, urun);
        db.FiyatListesiUrunFiyatlari.Add(kayit);
        DenetimeYaz(islem, kayit, urun, "ekleme");
        await db.SaveChangesAsync();
        return kayit;
    }

    /// <summary>
    /// PATCH — gönderilmeyen alan KORUNUR, açık null SİLER (DoD sözleşmesi).
    /// Pasife alma da buradan: <c>aktif=false</c> fiyatı listeye geri döndürür.
    /// </summary>
    public async Task<FiyatListesiUrunFiyati> UrunFiyatiGuncelle(
        string listeId, string urunId, UrunFiyatiDto dto, AktifKullanici islem)
    {
        await ListeZorunlu(islem.TenantId, listeId);
        var kayit = await db.FiyatListesiUrunFiyatlari
            .FirstOrDefaultAsync(x => x.TenantId == islem.TenantId && x.ListeId == listeId && x.UrunId == urunId)
            ?? throw new NotFoundException("Ürüne özel fiyat bulunamadı");
        var urun = await db.Urunler.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == urunId && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");

        var eskiMarj = kayit.HasMarjYuzde; var eskiCarpan = kayit.IscilikCarpanYuzde;
        if (dto.SabitFiyatGonderildi) kayit.SabitFiyat = dto.SabitFiyat;
        if (dto.HasMarjYuzdeGonderildi) kayit.HasMarjYuzde = dto.HasMarjYuzde;
        if (dto.IscilikCarpanYuzdeGonderildi) kayit.IscilikCarpanYuzde = dto.IscilikCarpanYuzde;
        if (dto.NotGonderildi) kayit.Not = dto.Not?.Trim();
        if (dto.Aktif is { } a) kayit.Aktif = a;
        kayit.Guncellendi = DateTime.UtcNow;
        // Uygulanmış SONUÇ denetlenir (PATCH'te gövdede olmayan alan korunur);
        // değişmeyen riskli değer yeniden onay istemez (pasife alma kilitlenmez).
        AraligiDogrula(kayit.HasMarjYuzde, kayit.IscilikCarpanYuzde, dto.RiskOnayi, eskiMarj, eskiCarpan);
        Dogrula(kayit, urun);
        DenetimeYaz(islem, kayit, urun, "güncelleme");
        await db.SaveChangesAsync();
        return kayit;
    }

    /// <summary>
    /// Geçersiz kılma kuralları (fail-closed):
    ///  • AKTİF kayıt en az bir alan taşımalı — boş geçersiz kılma sessiz etkisizliktir.
    ///  • GÜMÜŞ üründe yalnız SABİT fiyat (Q3 mührü): marj/işçilik türetmesi has
    ///    kuruna dayanır, gümüşte fail-closed null döner ve kullanıcı sebebini bilmez.
    /// </summary>
    private static void Dogrula(FiyatListesiUrunFiyati kayit, Data.Urun urun)
    {
        if (kayit.Aktif && !kayit.Doldurulmus)
            throw new BadRequestException("Sabit fiyat, has marjı ya da işçilik çarpanından en az biri girilmeli");
        if (urun.AyarId.StartsWith('G') && kayit.Aktif && kayit.SabitFiyat is null)
            throw new BadRequestException(
                "Gümüş üründe yalnız SABİT fiyatlı geçersiz kılma tanımlanabilir; marj/işçilik türetmesi altın has kuruna dayanır");
    }

    private void DenetimeYaz(AktifKullanici islem, FiyatListesiUrunFiyati kayit, Data.Urun urun, string eylem)
    {
        // Otomatik yazma izi Detay'ı boş bırakır; "fiyatı kim, ne zaman, NEYE
        // çevirdi" sorusu ancak gerekçeli kayıtla cevaplanır (paket 49 iş kuralı).
        denetim?.YazmaKaydet(islem, "FiyatListesiUrunFiyati", kayit.Id, string.Create(CultureInfo.InvariantCulture,
            $"{eylem}: liste={kayit.ListeId} ürün={urun.Kod} sabit={kayit.SabitFiyat} marj={kayit.HasMarjYuzde} çarpan={kayit.IscilikCarpanYuzde} aktif={kayit.Aktif} not={kayit.Not}"),
            hassas: false);
    }

    // ── Toplu uygulama ───────────────────────────────────────────────────────

    /// <summary>
    /// TOPLU MARJ/İŞÇİLİK UYGULAMA. <paramref name="onizleme"/> true iken HİÇBİR
    /// satır yazılmaz (kuru çalıştırma); false iken gövdedeki parmak izi sunucuda
    /// yeniden hesaplanır ve tutmazsa 409 döner — "gördüğüm özeti onayladım"
    /// iddiası ekranın değil SUNUCUNUN doğruladığı bir şeydir.
    /// </summary>
    public async Task<TopluSonuc> TopluUygula(
        string listeId, TopluUygulaDto dto, bool onizleme, AktifKullanici islem)
    {
        // ⚠ İLK SATIR: aralık/risk kapısı her şeyden önce durur — tek istekte
        // yüzlerce ürün etkilendiği için hiçbir yazma hazırlığı yapılmaz.
        // ÖNİZLEMEDE risk ONAYI aranmaz (etkiyi görmeden onay istemek UX-S23'ü
        // kırardı); SERT sınır önizlemede de reddedilir, yazmada onay ZORUNLU.
        AraligiDogrula(dto.HasMarjYuzde, dto.IscilikCarpanYuzde, onizleme ? true : dto.RiskOnayi);
        var liste = await ListeZorunlu(islem.TenantId, listeId);
        var hasKuru = await HasKuru(islem.TenantId, dto.KurSnapshotId);
        if (dto.HasMarjYuzde is null && dto.IscilikCarpanYuzde is null)
            throw new BadRequestException("Has marjı ya da işçilik çarpanından en az biri girilmeli");

        var hedefler = await HedefUrunler(dto, islem.TenantId);
        var mevcutlar = await db.FiyatListesiUrunFiyatlari
            .Where(x => x.TenantId == islem.TenantId && x.ListeId == listeId)
            .ToListAsync();
        var oncekiler = await SatirlariKur(hedefler, liste, hasKuru, islem.TenantId);
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
        var adaylar = await db.Kampanyalar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Aktif && k.Baslangic <= bugun && k.Bitis >= bugun)
            .ToListAsync();
        var ustler = await KategoriUstHaritasi(islem.TenantId);

        var degisenler = new List<TopluSatir>();
        var atlananlar = new List<TopluAtlanan>();
        var uygulanacaklar = new List<(Data.Urun Urun, FiyatListesiUrunFiyati? Mevcut)>();

        foreach (var u in hedefler)
        {
            var mevcut = mevcutlar.FirstOrDefault(x => x.UrunId == u.Id);
            var onceki = oncekiler.First(x => x.UrunId == u.Id);
            // Q3 mührü: gümüşe marj/işçilik türetmesi UYGULANMAZ — atlandı listesinde görünür.
            if (u.AyarId.StartsWith('G')) { atlananlar.Add(new TopluAtlanan(u.Id, u.Kod, AtlamaSebebi.Gumus)); continue; }
            if (mevcut is { Aktif: true, SabitFiyat: > 0m })
            { atlananlar.Add(new TopluAtlanan(u.Id, u.Kod, AtlamaSebebi.GecersizKilmaSabit)); continue; }
            if (u.SatisFiyati is > 0m)
            { atlananlar.Add(new TopluAtlanan(u.Id, u.Kod, AtlamaSebebi.SabitFiyat)); continue; }
            if (!onceki.TeklifVar) { atlananlar.Add(new TopluAtlanan(u.Id, u.Kod, AtlamaSebebi.FiyatYok)); continue; }

            var aday = new FiyatListesiUrunFiyati
            {
                Id = mevcut?.Id ?? "", TenantId = islem.TenantId, ListeId = listeId, UrunId = u.Id,
                SabitFiyat = null,
                HasMarjYuzde = dto.HasMarjYuzde ?? mevcut?.HasMarjYuzde,
                IscilikCarpanYuzde = dto.IscilikCarpanYuzde ?? mevcut?.IscilikCarpanYuzde,
                Aktif = true,
            };
            var yeni = TeklifHesapla(u, liste, hasKuru, bugun, adaylar, KategoriZinciri(u, ustler), aday);
            if (yeni is null) { atlananlar.Add(new TopluAtlanan(u.Id, u.Kod, AtlamaSebebi.FiyatYok)); continue; }
            degisenler.Add(new TopluSatir(u.Id, u.Kod, u.Ad, onceki.ListeFiyati, yeni.ListeFiyati,
                FiyatMotoru.Yuvarla(yeni.ListeFiyati - onceki.ListeFiyati)));
            uygulanacaklar.Add((u, mevcut));
        }

        var parmakIzi = ParmakIziUret(listeId, dto, degisenler);
        var toplamFark = FiyatMotoru.Yuvarla(degisenler.Sum(x => x.Fark));
        var sonuc = new TopluSonuc(false, hedefler.Count, degisenler.Count, toplamFark, parmakIzi,
            degisenler.Take(10).ToList(), atlananlar);
        if (onizleme) return sonuc;

        // ⚠ ONAY KAPISI: önizlemeden dönen damga olmadan (ya da veri değiştiyse) YAZILMAZ.
        if (dto.ParmakIzi != parmakIzi)
            throw new ConflictException(
                "Önizleme özeti onaylanmadı ya da fiyatlar bu arada değişti; önizlemeyi yenileyip tekrar onaylayın");

        var now = DateTime.UtcNow;
        foreach (var (u, mevcut) in uygulanacaklar)
        {
            if (mevcut is null)
            {
                var kayit = new FiyatListesiUrunFiyati
                {
                    Id = Kimlik.Ulid(), TenantId = islem.TenantId, ListeId = listeId, UrunId = u.Id,
                    HasMarjYuzde = dto.HasMarjYuzde, IscilikCarpanYuzde = dto.IscilikCarpanYuzde,
                    Not = dto.Not?.Trim(), Aktif = true, Olusturuldu = now, Guncellendi = now,
                };
                db.FiyatListesiUrunFiyatlari.Add(kayit);
            }
            else
            {
                if (dto.HasMarjYuzde is { } m) mevcut.HasMarjYuzde = m;
                if (dto.IscilikCarpanYuzde is { } c) mevcut.IscilikCarpanYuzde = c;
                if (dto.Not is { Length: > 0 }) mevcut.Not = dto.Not.Trim();
                mevcut.Aktif = true;
                mevcut.Guncellendi = now;
            }
        }
        denetim?.YazmaKaydet(islem, "FiyatListesiTopluUygulama", listeId, string.Create(CultureInfo.InvariantCulture,
            $"toplu uygulama: hedef={hedefler.Count} uygulanan={uygulanacaklar.Count} atlanan={atlananlar.Count} marj={dto.HasMarjYuzde} çarpan={dto.IscilikCarpanYuzde} toplamFark={toplamFark} damga={parmakIzi}"),
            hassas: false);
        await db.SaveChangesAsync();
        return sonuc with { Uygulandi = true };
    }

    private async Task<List<Data.Urun>> HedefUrunler(TopluUygulaDto dto, string tenantId)
    {
        var sorgu = db.Urunler.AsNoTracking().Where(u => u.TenantId == tenantId && u.Aktif);
        if (dto.HedefTur == "KATEGORI")
        {
            if (string.IsNullOrWhiteSpace(dto.KategoriId)) throw new BadRequestException("Kategori seçilmedi");
            var dal = await new KategoriServisi(db).AltAgacIdleri(dto.KategoriId, tenantId);
            sorgu = sorgu.Where(u => u.KategoriId != null && dal.Contains(u.KategoriId));
        }
        else
        {
            var idler = dto.UrunIdler ?? [];
            if (idler.Count == 0) throw new BadRequestException("Ürün seçilmedi");
            if (idler.Count > EnFazlaSatir)
                throw new BadRequestException($"Tek seferde en fazla {EnFazlaSatir} ürüne uygulanabilir");
            sorgu = sorgu.Where(u => idler.Contains(u.Id));
        }
        var urunler = await sorgu.OrderBy(u => u.Kod).ToListAsync();
        if (urunler.Count == 0) throw new BadRequestException("Hedefte ürün yok");
        return urunler;
    }

    /// <summary>
    /// Önizleme damgası: liste + hedef + yüzdeler + her satırın (ürün, eski, yeni)
    /// üçlüsünden türetilir. Aradaki en küçük değişiklik damgayı bozar.
    /// </summary>
    private static string ParmakIziUret(string listeId, TopluUygulaDto dto, List<TopluSatir> satirlar)
    {
        var govde = new System.Text.StringBuilder();
        govde.Append(CultureInfo.InvariantCulture, $"{listeId}|{dto.HedefTur}|{dto.KategoriId}|{dto.HasMarjYuzde}|{dto.IscilikCarpanYuzde}|{dto.KurSnapshotId}");
        foreach (var x in satirlar.OrderBy(x => x.UrunId, StringComparer.Ordinal))
            govde.Append(CultureInfo.InvariantCulture, $";{x.UrunId}:{x.EskiFiyat}:{x.YeniFiyat}");
        var ozet = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(govde.ToString()));
        return Convert.ToHexString(ozet)[..32].ToLowerInvariant();
    }
}

// ── Paket 49 yanıt kayıtları (anonim tip DEĞİL: testler ve ekran aynı sözleşmeyi okur) ──

// P-03: bu tipler Kuyumcu.Contracts.Fiyat içine TAŞINDI (tek tanım, sunucu+ekran ortak).
/// <summary>Önizleme satırı — kırılım + kaynak rozeti. Fiyat üretilemediyse Sebep dolar.</summary>
public sealed record OnizlemeSatiri(
    string UrunId, string Kod, string Ad, string AyarId, decimal Gram, decimal HasGrami, decimal? IscilikTl,
    bool TeklifVar, string? Sebep, decimal HasDegeri, decimal MarjTl, decimal IscilikTlUygulanan,
    decimal ListeFiyati, decimal IndirimTutari, decimal SatisFiyati, string? KampanyaId, string Kaynak,
    GecersizKilmaOzeti? GecersizKilma, decimal? MaliyetTl = null);   // M1b — alış milyemiyle birim maliyet

public sealed record OnizlemeSonucu(int Toplam, int Limit, int Offset, IReadOnlyList<OnizlemeSatiri> Satirlar);

public sealed record UrunFiyatiSayfasi(
    int Toplam, int Limit, int Offset, IReadOnlyList<FiyatListesiUrunFiyati> Kayitlar);

// ── YETKİ KAPISI (paket UX-4a madde 14, Mehmet'in güvenlik koşulu) ───────────
// Fiyat belirleme bir RAPOR işi değil, ÜRÜN/stok işidir. Önceden yalnız
// [Authorize] vardı: rapor yetkisi verilen tezgâhtar uçtan marjı değiştirebilir,
// buna karşılık rapor yetkisi olmayan müdür ekranı göremezdi — kapı tersti.
//   • Okuma  → "urun" modül yetkisi (menüdeki blokla AYNI anahtar).
//   • Yazma  → ek olarak SAHIP/MUDUR rolü (marj ticari karardır).
// Yeni yetki ANAHTARI açılmadı; "urun" zaten Moduller.Tumu'nda ve tüm rollerin
// varsayılanında var → göç/tohumlama gerekmez (mevcut kiracılar etkilenmez).
[ApiController, Authorize]
// P-09: Fiyat listeleri KENDİ anahtarını taşır ("urun" değil).
[Yetki.ModulYetkisi("fiyat-listesi")]
[Route("api/v1/fiyat")]
public sealed class FiyatModulu(FiyatServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    [HttpGet("listeler")] public Task<List<FiyatListesi>> Listeler() => servis.Listeler(Islem);

    [HttpPost("listeler"), RolGerekli("SAHIP", "MUDUR")]
    public Task<FiyatListesi> ListeOlustur([FromBody] FiyatListesiDto dto) => servis.ListeOlustur(dto, Islem);

    [HttpPatch("listeler/{id}"), RolGerekli("SAHIP", "MUDUR")]
    public Task<FiyatListesi> ListeGuncelle(string id, [FromBody] FiyatListesiGuncelleDto dto) => servis.ListeGuncelle(id, dto, Islem);

    [HttpGet("kampanyalar")] public Task<List<Kampanya>> Kampanyalar() => servis.Kampanyalar(Islem);

    [HttpPost("kampanyalar"), RolGerekli("SAHIP", "MUDUR")]
    public Task<Kampanya> KampanyaOlustur([FromBody] KampanyaDto dto) => servis.KampanyaOlustur(dto, Islem);

    [HttpPatch("kampanyalar/{id}"), RolGerekli("SAHIP", "MUDUR")]
    public Task<Kampanya> KampanyaGuncelle(string id, [FromBody] KampanyaGuncelleDto dto) => servis.KampanyaGuncelle(id, dto, Islem);

    /// <summary>
    /// GET /api/v1/fiyat/teklif?urunId=&amp;cariId=&amp;kurSnapshotId= — POS'un tek fiyat kaynağı.
    /// Kırılım döner (has değeri, marj, işçilik, indirim, indirim öncesi 30-gün fiyatı); formül dönmez.
    /// </summary>
    [HttpGet("teklif")]
    public async Task<object> Teklif([FromQuery] string urunId, [FromQuery] string? cariId,
                                     [FromQuery] string kurSnapshotId, [FromQuery] string? listeId = null)
    {
        var islem = Islem;
        var urun = await db.Urunler.AsNoTracking().FirstOrDefaultAsync(u => u.Id == urunId && u.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Ürün bulunamadı");
        var kur = await db.KurSnapshots.AsNoTracking()
            .Where(s => s.Id == kurSnapshotId && s.TenantId == islem.TenantId)
            .Select(s => (decimal?)s.AltinHasFiyati).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Kur kaydı bulunamadı");

        var now = DateTime.UtcNow;
        var bugun = DateOnly.FromDateTime(Zaman.UtcdenYerele(now));
        // listeId verilmişse O liste esas alınır (Fiyatlandırma ekranının "bu listeyle
        // ürün fiyatları" paneli — paket UX-4a madde 3). Verilmemişse POS davranışı
        // AYNEN korunur: cariye atanan liste, yoksa varsayılan.
        var liste = listeId is { Length: > 0 }
            ? await servis.ListeGetir(islem.TenantId, listeId) ?? throw new NotFoundException("Fiyat listesi bulunamadı")
            : await servis.ListeCoz(islem.TenantId, cariId);
        var t = liste is null ? null : await servis.TeklifListeyle(urun, liste, kur, bugun);
        if (t is null)
        {
            // PAKET 20 (K20-4 · UX-S30): "fiyat yok" demek yetmez, SEBEBİ söylenir.
            // Altın dışı madende kotasyon YOKTUR (Faz B'de gelecek); kullanıcı
            // ürüne sabit fiyat girmelidir. "FIYAT_YOK" dense ekran kullanıcıyı
            // fiyat listesi marjıyla oynamaya yollardı — hiç işe yaramazdı.
            var urunMadeni = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId)
                             ?? Kuyumcu.Core.Domain.Maden.Altin;
            var sebep = liste is null ? "LISTE_YOK"
                      : urunMadeni != Kuyumcu.Core.Domain.Maden.Altin ? AtlamaSebebi.KotasyonYok
                      : AtlamaSebebi.FiyatYok;
            return new
            {
                teklifVar = false,
                sebep,
                maden = urunMadeni,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(urunMadeni),
            };
        }

        var oncesi = await servis.IndirimOncesiFiyat(islem.TenantId, urunId, t.ListeFiyati, now);
        return new
        {
            teklifVar = true,
            listeFiyati = t.ListeFiyati, hasDegeri = t.HasDegeri, marjTl = t.MarjTl, iscilikTl = t.IscilikTl,
            indirimTutari = t.IndirimTutari, satisFiyati = t.SatisFiyati, kampanyaId = t.KampanyaId,
            sabitFiyat = t.SabitFiyat,
            // Paket 49 — fiyatın kaynağı (LISTE · URUN_GECERSIZ_KILMA · URUN_SABIT_FIYAT).
            // GERİYE DÖNÜK UYUMLU EK: POS bu alanı okumak zorunda değildir.
            kaynak = t.Kaynak,
            // M1 · M1b — satış milyemi, ofset ve (alış milyemi biliniyorsa) birim maliyet; kâr = satış − maliyet.
            satisMilyemi = t.SatisMilyemi, ofsetTl = t.OfsetTl, maliyetTl = t.MaliyetTl,
            // FEY md.11 — fiş/etiket bunu "indirim öncesi fiyat" olarak basar.
            indirimOncesiFiyat = t.IndirimTutari > 0m ? oncesi : (decimal?)null,
        };
    }

    // ── PAKET 49 — liste × ürün görünümü ─────────────────────────────────────
    // Okuma: "urun" modül yetkisi yeter (sınıf kapısı).
    // Yazma: ek olarak SAHIP/MUDUR — marj ticari karardır (PANO-KARAR §88).

    /// <summary>
    /// GET /listeler/{id}/onizleme — bu listenin ÜRÜNLERE etkisi. SALT HESAP:
    /// hiçbir satır yazmaz (UX-S23 "soyut kaydın etkisi aynı ekranda görünür").
    /// </summary>
    [HttpGet("listeler/{id}/onizleme")]
    public Task<OnizlemeSonucu> ListeOnizleme(
        string id, [FromQuery] string kurSnapshotId, [FromQuery] string? kategoriId = null,
        [FromQuery] string? q = null, [FromQuery] bool yalnizGecersizKilinan = false,
        [FromQuery] int limit = 50, [FromQuery] int offset = 0) =>
        servis.ListeOnizleme(id, kurSnapshotId, Islem, kategoriId, q, yalnizGecersizKilinan, limit, offset);

    /// <summary>GET /listeler/{id}/urun-fiyatlari — tanımlı geçersiz kılma KAYITLARI (sayfalı).</summary>
    [HttpGet("listeler/{id}/urun-fiyatlari")]
    public Task<UrunFiyatiSayfasi> UrunFiyatlari(
        string id, [FromQuery] bool sadeceAktif = true, [FromQuery] int limit = 50, [FromQuery] int offset = 0) =>
        servis.UrunFiyatlari(id, Islem, sadeceAktif, limit, offset);

    [HttpPost("listeler/{id}/urun-fiyatlari"), RolGerekli("SAHIP", "MUDUR")]
    public Task<FiyatListesiUrunFiyati> UrunFiyatiEkle(string id, [FromBody] UrunFiyatiDto dto) =>
        servis.UrunFiyatiEkle(id, dto, Islem);

    /// <summary>PATCH — gönderilmeyen alan korunur, açık null siler; aktif=false pasife alır.</summary>
    [HttpPatch("listeler/{id}/urun-fiyatlari/{urunId}"), RolGerekli("SAHIP", "MUDUR")]
    public Task<FiyatListesiUrunFiyati> UrunFiyatiGuncelle(string id, string urunId, [FromBody] UrunFiyatiDto dto) =>
        servis.UrunFiyatiGuncelle(id, urunId, dto, Islem);

    /// <summary>
    /// POST /listeler/{id}/toplu-uygula[?onizleme=true] — kuru çalıştırma ya da yazma.
    /// Yazma çağrısı önizlemeden dönen <c>parmakIzi</c>yi taşımak ZORUNDADIR (409).
    /// </summary>
    [HttpPost("listeler/{id}/toplu-uygula"), RolGerekli("SAHIP", "MUDUR")]
    public Task<TopluSonuc> TopluUygula(string id, [FromBody] TopluUygulaDto dto, [FromQuery] bool onizleme = false) =>
        servis.TopluUygula(id, dto, onizleme, Islem);
}
