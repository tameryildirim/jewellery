using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// D1-18 (18.10, rakip eşitleme: "yaptırım listesinin kendiliğinden güncellenmesi").
//   • Kaynak: BM Güvenlik Konseyi Birleşik Listesi — resmî XML (scsanctions.un.org; birincil kaynak listesinde).
//     Biçim 18.10.2026'da gerçek dosyayla doğrulandı: CONSOLIDATED_LIST@dateGenerated · INDIVIDUALS/INDIVIDUAL ·
//     ENTITIES/ENTITY · FIRST_NAME…FOURTH_NAME · REFERENCE_NUMBER · INDIVIDUAL_DATE_OF_BIRTH/DATE ·
//     INDIVIDUAL_DOCUMENT/TYPE_OF_DOCUMENT+NUMBER.
//   • Fark: karar numarası (REFERENCE_NUMBER) anahtar — yeni olan eklenir, listeden düşen kaldırılır; ekleme/kaldırma,
//     eşleşme, kilit ve görev mevcut çekirdekten (YaptirimServisi.ListeGuncelle) geçer — ikinci yol yok.
//   • GÜVENLİK: ayrıştırılan satır sayısı tabanın altındaysa (bozuk/kesik dosya) HİÇBİR ŞEY kaldırılmaz, tur atlanır.
//   • Ulusal listeler (6415/7262 kararları — Resmî Gazete) biçim belgesi olmadığı için elle yükleme yolunda kalır.
// ═════════════════════════════════════════════════════════════════════════════

public static class BmYaptirimListesi
{
    public const string Kaynak = "BM Güvenlik Konseyi Birleşik Listesi";
    public const string VarsayilanUrl = "https://scsanctions.un.org/resources/xml/en/consolidated.xml";
    /// <summary>Bu sayının altında satır gelirse dosya bozuk sayılır (gerçek liste ~1.000 satır).</summary>
    public const int AsgariSatir = 500;

    public static (DateOnly ListeTarihi, List<YaptirimSatiriDto> Satirlar) Coz(Stream xml)
    {
        var ayar = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var okuyucu = XmlReader.Create(xml, ayar);
        var kok = XDocument.Load(okuyucu).Root ?? throw new InvalidDataException("BM listesi boş");
        var tarih = DateTime.TryParse((string?)kok.Attribute("dateGenerated"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var t)
            ? DateOnly.FromDateTime(t) : DateOnly.FromDateTime(DateTime.UtcNow);
        var satirlar = new List<YaptirimSatiriDto>();
        foreach (var e in kok.Descendants().Where(x => x.Name.LocalName is "INDIVIDUAL" or "ENTITY"))
        {
            var ad = string.Join(' ', new[] { "FIRST_NAME", "SECOND_NAME", "THIRD_NAME", "FOURTH_NAME" }
                .Select(n => ((string?)e.Element(n))?.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            var karar = ((string?)e.Element("REFERENCE_NUMBER"))?.Trim();
            if (ad.Length == 0 || string.IsNullOrEmpty(karar)) continue;
            DateOnly? dogum = null;
            foreach (var d in e.Elements("INDIVIDUAL_DATE_OF_BIRTH").Elements("DATE"))
                if (DateOnly.TryParseExact(d.Value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)) { dogum = g; break; }
            var belgeler = e.Elements("INDIVIDUAL_DOCUMENT")
                .Select(b => (Tur: ((string?)b.Element("TYPE_OF_DOCUMENT"))?.Trim() ?? "", No: ((string?)b.Element("NUMBER"))?.Trim() ?? ""))
                .Where(b => b.No.Length is > 0 and <= 50).ToList();
            var kimlik = belgeler.FirstOrDefault(b => b.Tur.StartsWith("National Identification", StringComparison.OrdinalIgnoreCase)).No
                         ?? belgeler.FirstOrDefault(b => b.Tur.Equals("Passport", StringComparison.OrdinalIgnoreCase)).No;
            satirlar.Add(new YaptirimSatiriDto
            {
                KararNo = karar.Length > 100 ? karar[..100] : karar,
                AdSoyad = ad.Length > 300 ? ad[..300] : ad,
                DogumTarihi = dogum,
                KimlikNo = string.IsNullOrEmpty(kimlik) ? null : kimlik,
            });
        }
        return (tarih, satirlar);
    }

    /// <summary>Fark çıkarır ve mevcut çekirdekle uygular. Dönüş: (eklenen, kaldırılan) ya da atlanma sebebi.</summary>
    public static async Task<object> Uygula(KuyumcuDbContext sistemDb, DenetimServisi denetim, string url, DateOnly listeTarihi,
        List<YaptirimSatiriDto> satirlar, string operatorAdi, int asgari = AsgariSatir)
    {
        var tekil = satirlar.GroupBy(s => s.KararNo!).Select(g => g.First()).ToList();
        if (tekil.Count < asgari)
            throw new InvalidDataException($"BM listesi {tekil.Count} satır (taban {asgari}) — dosya bozuk sayıldı, hiçbir kayıt kaldırılmadı");
        var mevcut = await sistemDb.YaptirimKayitlari.AsNoTracking()
            .Where(y => y.Kaynak == Kaynak && y.Kaldirildi == null)
            .Select(y => new { y.Id, y.KararNo })
            .Take(YaptirimServisi.AzamiListeSatiri).ToListAsync();
        var mevcutKarar = mevcut.Where(m => m.KararNo != null).Select(m => m.KararNo!).ToHashSet(StringComparer.Ordinal);
        var akisKarar = tekil.Select(s => s.KararNo!).ToHashSet(StringComparer.Ordinal);
        var dto = new YaptirimListesiDto
        {
            Kaynak = Kaynak, KaynakUrl = url, ListeTarihi = listeTarihi,
            Ekle = [.. tekil.Where(s => !mevcutKarar.Contains(s.KararNo!))],
            Kaldir = [.. mevcut.Where(m => m.KararNo is null || !akisKarar.Contains(m.KararNo)).Select(m => m.Id)],
        };
        return await YaptirimServisi.ListeGuncelle(sistemDb, denetim, dto, operatorAdi);
    }
}

/// <summary>Günlük BM listesi çekimi (arka plan). Yaptirim:BmOtomatik=false ile kapanır (testler kapatır).</summary>
public sealed class BmYaptirimGuncelleyici(SistemKapsami kapsam, IHttpClientFactory http, ILogger<BmYaptirimGuncelleyici> gunluk, IConfiguration yapilandirma)
    : BackgroundService
{
    private readonly bool etkin = yapilandirma.GetValue("Yaptirim:BmOtomatik", true);
    private readonly string url = yapilandirma.GetValue("Yaptirim:BmUrl", BmYaptirimListesi.VarsayilanUrl)!;

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        if (!etkin) { gunluk.LogInformation("BM yaptırım listesi otomatik çekimi KAPALI (Yaptirim:BmOtomatik=false)"); return; }
        try { await Task.Delay(TimeSpan.FromMinutes(2), durdur); } catch (OperationCanceledException) { return; }
        while (!durdur.IsCancellationRequested)
        {
            try { await Tur(durdur); }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested) { break; }
            catch (Exception ex) { gunluk.LogError(ex, "BM yaptırım listesi çekimi başarısız — liste eski kalır (ekranda eskilik bandı)"); }
            try { await Task.Delay(TimeSpan.FromHours(24), durdur); } catch (OperationCanceledException) { break; }
        }
    }

    public async Task<object> Tur(CancellationToken iptal)
    {
        using var istemci = http.CreateClient();
        istemci.Timeout = TimeSpan.FromMinutes(2);
        await using var akis = await istemci.GetStreamAsync(url, iptal);
        var (tarih, satirlar) = BmYaptirimListesi.Coz(akis);
        // SistemKapsami izinli kullanım 13: kiracı-üstü yaptırım listesi (sistem uç noktasıyla AYNI çekirdek).
        await using var sistem = kapsam.Sistem();
        var db = sistem.ServiceProvider.GetRequiredService<KuyumcuDbContext>();
        var denetim = sistem.ServiceProvider.GetRequiredService<DenetimServisi>();
        var sonuc = await BmYaptirimListesi.Uygula(db, denetim, url, tarih, satirlar, "otomatik:bm");
        gunluk.LogInformation("BM yaptırım listesi güncellendi: {Sonuc}", System.Text.Json.JsonSerializer.Serialize(sonuc));
        return sonuc;
    }
}
