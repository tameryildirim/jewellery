using System.Text.Json.Serialization;

namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// KIYMETLİ EVRAK — İSTEMCİ/SUNUCU YANIT SÖZLEŞMESİ (paket 29 Faz A).
//
// ⚠ NEDEN BURADA, NEDEN SAYFADA DEĞİL: denetim.sh "Pages içinde istemci record
//   tanımı" tavanı DOLU (S-2 / MÜHÜR 5 KOŞUL 3). Yeni uçların yanıt tipleri
//   sayfaya elle kopyalanmaz; iki tarafın da gördüğü TEK dizide durur.
//
// ⚠ ONDALIK SÖZLEŞMESİ: sunucunun decimal döndürdüğü her alan burada da
//   decimal'dir. QA turu 3'te /urunler stok modalında sunucu decimal "adet"
//   döndürürken istemci int okumaya çalışmış, tek bir deserialize hatası
//   Blazor render ağacını düşürmüş ve sonraki kaydetmeler sessizce veri
//   kaybetmişti. "adet" burada decimal'dir ve öyle kalır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Portföy listesinin bir satırı.</summary>
public sealed record EvrakSatiri(
    string Id,
    string EvrakTuru,
    string Yon,
    string Durum,
    decimal Tutar,
    string ParaBirimi,
    DateOnly VadeGunu,
    string EvrakNo,
    string? BankaAdi,
    string? BankaSube,
    string? KesideYeri,
    DateOnly? KesideTarihi,
    string? BorcluCariId,
    string? BorcluAd,
    string? KaynakCariId,
    string? KaynakCariAd,
    string? Notlar,
    int GecikmeGun,
    bool Gecikmis,
    bool YakindaVadeli,
    bool Duzenlenebilir,
    string[] IzinliGecisler,
    // PAKET GOC-B tur 2: kimlik no ŞİFRELİ ve listede DÖNMEZ; ekran yalnız "kayıtlı mı" bilir
    // (düzenlemede "kimliği kaldır" eylemini gösterir — dokunulmayan kimlik PATCH'te silinmez).
    bool BorcluKimlikKayitli = false);

/// <summary>Portföy listesi zarfı — toplamlar SUNUCUDA toplanır (TB-19).</summary>
public sealed record EvrakListesi(
    decimal ToplamAdet,
    decimal ToplamTutar,
    int Limit,
    int AzamiLimit,
    int Offset,
    bool DahaFazlasiVar,
    decimal PortfoydeTutar,
    decimal TahsileVerilenTutar,
    decimal BuHaftaVadeliTutar,
    decimal KarsiliksizTutar,
    List<EvrakSatiri>? Satirlar)
{
    /// <summary>Null güvenli okuma kolaylığı — ⚠ JSON'a YAZILMAZ (zarf ikiye katlanırdı).</summary>
    [JsonIgnore] public List<EvrakSatiri> Kalemler => Satirlar ?? [];
}

/// <summary>Vade takviminin bir GÜNÜ — satır değil, GROUP BY toplamı.</summary>
public sealed record EvrakTakvimGunu(DateOnly Gun, decimal Adet, decimal Tutar);

/// <summary>Vade takvimi zarfı.</summary>
public sealed record EvrakTakvimi(
    DateOnly Bas,
    DateOnly Bit,
    decimal ToplamAdet,
    decimal ToplamTutar,
    List<EvrakTakvimGunu>? Gunler,
    // ── QA turu 5 (YÜKSEK): boş durum "Portföyde evrak yok" diyordu, oysa
    //    portföyde evrak VARDI; yalnız seçili aralığın dışındaydı. Aşağıdaki
    //    dört alan DÖNEMDEN BAĞIMSIZDIR (UX-S33) ve ekranın doğru boş durumu
    //    yazmasını + aralığı portföye genişletmesini sağlar.
    decimal PortfoyAdet = 0,
    decimal PortfoyTutar = 0,
    DateOnly? PortfoyIlkVade = null,
    DateOnly? PortfoySonVade = null,
    int AzamiGun = 366)
{
    /// <summary>Null güvenli okuma kolaylığı — ⚠ JSON'a YAZILMAZ (zarf ikiye katlanırdı).</summary>
    [JsonIgnore] public List<EvrakTakvimGunu> Kalemler => Gunler ?? [];
}

/// <summary>Yazma uçlarının ortak yanıtı: evrak + carinin güncel TL bakiyesi.</summary>
public sealed record EvrakSonucu(EvrakSatiri Evrak, decimal? CariTlBakiye, string? Mesaj);
