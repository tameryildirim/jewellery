using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Disaaktarim;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// 20.10 — Luca fiş CSV ve Zirve fatura xlsx (ARASTIRMA-LUCA-ZIRVE-AKTARIM-2026-10-20.md § 1a/§ 2a).
// Saf: satır üretimi, Türkçe tutar, tarih; dosya: xlsx açılır ve hücreler okunur. Eksik bilgi uydurulmaz.
public sealed class MuhasebeAktarimTestleri
{
    private static Fatura Fatura(string kdvTuru = "STANDART", decimal has = 0m, decimal oran = 0.20m) => new()
    {
        Id = "f1", TenantId = "t", CariId = "c", FaturaNo = "MHK2026000000012", Tur = "SATIS", KdvTuru = kdvTuru, Durum = "KESILDI",
        HasDeger = has, AraToplam = 10_000m, KdvOran = oran, KdvTutar = 2_000m, Toplam = 12_000m + has, ParaBirimi = "TRY",
        Tarih = new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc), AliciUnvan = "Ayşe Kuyumculuk Ltd",
    };

    [Fact]
    public void Luca_fisi_3_satir_borc_alacak_esit_Turkce_tutar_GGAAYYYY()
    {
        var fis = LucaFisKaynagi.Fis(1, Fatura(), "120.001", "600", "391");
        Assert.Equal(3, fis.Length);
        Assert.Equal(["1", "15.10.2026", "120.001", "MHK2026000000012", "15.10.2026", "Satış faturası MHK2026000000012 · Ayşe Kuyumculuk Ltd", "12.000,00", "0", "0", "", ""], fis[0]);
        Assert.Equal(("600", "0", "10.000,00"), (fis[1][2], fis[1][6], fis[1][7]));
        Assert.Equal(("391", "2.000,00", "Hesaplanan KDV %20 · MHK2026000000012"), (fis[2][2], fis[2][7], fis[2][5]));
        var csv = LucaFisKaynagi.Csv(fis);
        Assert.StartsWith("Fiş No;Fiş Tarihi;Hesap Kodu;Evrak No;Evrak Tarihi;Açıklama;Borç;Alacak;Miktar;Döviz;Kur\r\n1;15.10.2026;120.001;", csv);
        // Kuyum: has bedeli KDV dışı ama satış hasılatıdır → 600 alacak = toplam − KDV (has dahil); eşitlik korunur.
        var kuyum = LucaFisKaynagi.Fis(2, Fatura("KUYUM", has: 40_000m), "120.001", "600", "391");
        Assert.Equal(("52.000,00", "50.000,00", "2.000,00"), (kuyum[0][6], kuyum[1][7], kuyum[2][7]));
    }

    [Fact]
    public void Zirve_satiri_oran_sutununa_kuyumda_has_bedeli_KDVsiz_sutuna_bilinmeyen_oran_null()
    {
        var s = ZirveFaturaKaynagi.Satir(Fatura("KUYUM", has: 40_000m), "Ayşe Kuyumculuk Ltd", "1234567890")!;
        Assert.Equal(("15.10.2026", "Satış Faturası", "Ayşe Kuyumculuk Ltd", "MHK2026000000012", "1234567890"), (s[0], s[1], s[2], s[3], s[4]));
        Assert.Equal((10_000m, 2_000m, 40_000m), (s[5], s[6], s[11]));
        Assert.Null(s[7]);
        var on = ZirveFaturaKaynagi.Satir(Fatura(oran: 0.10m), "A", "1234567890")!;
        Assert.Equal((10_000m, 2_000m), (on[7], on[8]));
        Assert.Null(on[5]);
        Assert.Null(ZirveFaturaKaynagi.Satir(Fatura(oran: 0.08m), "A", "1234567890"));
    }

    [Fact]
    public void Xlsx_acilir_Sayfa1_basliklar_ve_sayi_hucreleri_okunur()
    {
        var bayt = XlsxYazici.Yaz("Sayfa1", ["Tarih", "matrah%20"], [["15.10.2026", 10_000.5m]]);
        // Bağımsız okuyucu kanıtı (openpyxl ile elle açılır): bin altına örnek dosya.
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "zirve-ornek.xlsx"),
            XlsxYazici.Yaz("Sayfa1", ZirveFaturaKaynagi.Kolonlar, [ZirveFaturaKaynagi.Satir(Fatura("KUYUM", has: 40_000m), "Ayşe Kuyumculuk Ltd", "1234567890")!]));
        using var zip = new ZipArchive(new MemoryStream(bayt), ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        var wb = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
        Assert.Contains("Sayfa1", wb.ToString());
        var sh = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var hucreler = sh.Descendants(ns + "c").ToList();
        Assert.Equal(4, hucreler.Count);
        Assert.Equal("Tarih", hucreler[0].Descendants(ns + "t").Single().Value);
        Assert.Equal("B2", hucreler[3].Attribute("r")!.Value);
        Assert.Equal("10000.5", hucreler[3].Element(ns + "v")!.Value);
        Assert.Equal("AA", XlsxYazici.SutunAdi(26));
    }
}

[Collection("veritabani")]
public sealed class MuhasebeAktarimVtTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Hesap_kodu_tanimsiz_fatura_Luca_dosyasina_GIRMEZ_eksik_listesine_yazilir_Zirve_VKNsiz_faturayi_atlar_dosyalar_uretilir()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem()) { k = TestKurulum.KiraciKur(kur, "PAKET-MUHASEBE-AKTARIM"); await kur.SaveChangesAsync(); }
        using var db = vt.OlusturSistem();
        var cari = await db.Cariler.FirstAsync(c => c.Id == k.CariId);
        cari.HesapKodu = "120.01.0007"; cari.VergiNo = "1234567890";
        var kodsuz = new Cari { Id = Kimlik.Ulid(), TenantId = k.TenantId, Tur = "MUSTERI", Ad = "Kodsuz Müşteri", Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow };
        db.Cariler.Add(kodsuz);
        Fatura F(string no, string cariId, string durum = "KESILDI") => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = cariId, FaturaNo = no, Tur = "SATIS", KdvTuru = "STANDART", Durum = durum,
            AraToplam = 1_000m, KdvOran = 0.20m, KdvTutar = 200m, Toplam = 1_200m, Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            AliciUnvan = "Alıcı", AliciVergiNo = cariId == k.CariId ? "1234567890" : null,
        };
        db.Faturalar.AddRange(F("F-1", k.CariId), F("F-2", kodsuz.Id), F("F-3", k.CariId, "TASLAK"), F("F-4", k.CariId, "IPTAL"));
        await db.SaveChangesAsync();

        var luca = (LucaFisKaynagi)Kaynaklar.Bul("luca-fis")!;
        var (fisler, eksik) = await luca.Fisler(db, k.TenantId);
        Assert.Single(fisler);                      // yalnız F-1 (F-2 kodsuz, F-3 taslak, F-4 iptal)
        Assert.Single(eksik, e => e.StartsWith("F-2:") && e.Contains("hesap kodu"));
        var dosyalar = new List<(string Ad, byte[] Icerik, int Satir)>();
        await foreach (var d in luca.Dosyalar(db, k.TenantId)) dosyalar.Add(d);
        Assert.Equal(["luca-fis-utf8-1.csv", "luca-fis-1254-1.csv", "luca-fis-OKUBENI.txt"], dosyalar.Select(d => d.Ad));
        Assert.Equal(3, dosyalar[0].Satir);
        Assert.Equal([0xEF, 0xBB, 0xBF], dosyalar[0].Icerik.Take(3));
        Assert.Contains("F-2:", Encoding.UTF8.GetString(dosyalar[2].Icerik));
        Assert.Contains("Fiş No;", Encoding.GetEncoding(1254).GetString(dosyalar[1].Icerik));

        var zirve = (ZirveFaturaKaynagi)Kaynaklar.Bul("zirve-fatura")!;
        var (satirlar, zEksik) = await zirve.Satirlar(db, k.TenantId);
        Assert.Single(satirlar);
        Assert.Single(zEksik, e => e.StartsWith("F-2:") && e.Contains("VKN"));
        var zd = new List<(string Ad, byte[] Icerik, int Satir)>();
        await foreach (var d in zirve.Dosyalar(db, k.TenantId)) zd.Add(d);
        Assert.Equal("zirve-fatura.xlsx", zd[0].Ad);
        Assert.Equal(1, zd[0].Satir);
        Assert.Equal(0x50, zd[0].Icerik[0]);   // "PK" — zip
    }
}
