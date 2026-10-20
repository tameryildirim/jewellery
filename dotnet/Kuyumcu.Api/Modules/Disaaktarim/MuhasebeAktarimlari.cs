using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Disaaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// 20.10 — MALİ MÜŞAVİR PROGRAMLARINA AKTARIM (M11 / D1-14 kalanı). Biçimler herkese açık resmi
// kaynaklardan: ARASTIRMA-LUCA-ZIRVE-AKTARIM-2026-10-20.md § 1a (Luca "Excel Veri Aktarımı" şablonu,
// resmi ekran görüntüsü) ve § 2a (Zirve "Fatura Excel", resmi örnek dosya).
//   • luca-fis: fatura başına 1 mahsup fişi (120 borç · 600 alacak · 391 alacak); `;` ayraçlı CSV,
//     GG.AA.YYYY, Türkçe tutar (12.000,00). En çok 50 fiş/dosya → dosya bölünür. UTF-8 (BOM) ve
//     Windows-1254 iki kopya (Luca'nın kodlaması belgede yazmıyor; mali müşavir açılanı kullanır).
//   • zirve-fatura: Zirve'nin Sayfa1 başlıklarıyla .xlsx (OpenXML, kütüphanesiz). Zirve kolonları
//     F5 tanım tablosunda sıra numarasıyla eşler; başlık adı serbest. .xlsx kabulü TEYİTSİZ (banka
//     aktarımının resmi örneği .xlsx).
// BİLİNMEYEN UYDURULMAZ: carinin muhasebe hesap kodu yoksa fatura Luca dosyasına GİRMEZ, EKSIK
// listesine yazılır. 600/391 Tek Düzen ana hesaplarıdır; alt hesap yapılandırmadan (Muhasebe:*) verilir.
// ⚠ Son kanıt yalnız mali müşavirin bir kez yüklemesiyle alınır (dış bağımlılık).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>CSV dışı / çok dosyalı kaynak: ZIP'e kendi dosyalarını yazar.</summary>
public interface IDosyaUreten
{
    /// <summary>(dosya adı, içerik, satır sayısı) — içerik belleğe alınır (fatura dökümü küçüktür; ≤ birkaç MB).</summary>
    IAsyncEnumerable<(string DosyaAdi, byte[] Icerik, int SatirSayisi)> Dosyalar(KuyumcuDbContext db, string tenantId);
}

internal static class TrBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    /// <summary>12.000,00 — sıfır "0" yazılır (Luca örneğindeki gibi).</summary>
    public static string Tutar(decimal d) => d == 0m ? "0" : d.ToString("#,##0.00", Tr);
    public static string Gun(DateTime utc) => Zaman.UtcdenYerele(utc).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}

/// <summary>Luca muhasebe fişi aktarım dosyası (§ 1a). Hesap kodları: cari kartı + yapılandırma.</summary>
public sealed class LucaFisKaynagi(string satisHesabi = "600", string kdvHesabi = "391") : IDisaAktarilabilir, IDosyaUreten
{
    public const int FisSiniri = 50;
    public static readonly string[] Kolonlar =
        ["Fiş No", "Fiş Tarihi", "Hesap Kodu", "Evrak No", "Evrak Tarihi", "Açıklama", "Borç", "Alacak", "Miktar", "Döviz", "Kur"];

    static LucaFisKaynagi() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // Windows-1254 kopyası için

    public string Ad => "luca-fis";
    public bool KisiselVeri => false;
    public string[] Basliklar => Kolonlar;

    public async IAsyncEnumerable<string[]> Satirlar(KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var (fisler, _) = await Fisler(db, tenantId);
        foreach (var f in fisler) foreach (var s in f) yield return s;
    }

    /// <summary>Fişler (her biri 3 satır) ve eksik listesi — saf dönüşüm Satir() içinde.</summary>
    public async Task<(List<string[][]> Fisler, List<string> Eksik)> Fisler(KuyumcuDbContext db, string tenantId)
    {
        var faturalar = await (
            from f in db.Faturalar.AsNoTracking().Where(x => x.TenantId == tenantId && x.Tur == "SATIS" && (x.Durum == "KESILDI" || x.Durum == "ODENDI"))
            join c in db.Cariler.AsNoTracking().Where(x => x.TenantId == tenantId) on f.CariId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            orderby f.Tarih, f.Id
            select new { f, hesapKodu = c != null ? c.HesapKodu : null, cariAd = c != null ? c.Ad : null })
            .Take(10_000).ToListAsync();
        var fisler = new List<string[][]>();
        var eksik = new List<string>();
        var no = 0;
        foreach (var r in faturalar)
        {
            if (string.IsNullOrWhiteSpace(r.f.FaturaNo)) { eksik.Add($"{r.f.Id}: fatura numarası yok"); continue; }
            if (string.IsNullOrWhiteSpace(r.hesapKodu)) { eksik.Add($"{r.f.FaturaNo}: müşterinin muhasebe hesap kodu tanımsız ({r.f.AliciUnvan ?? r.cariAd})"); continue; }
            fisler.Add(Fis(++no, r.f, r.hesapKodu!.Trim(), satisHesabi, kdvHesabi));
        }
        return (fisler, eksik);
    }

    /// <summary>Tek fatura → 3 satır (saf). Alacak toplamı = borç: Toplam = (Toplam − KDV) + KDV.</summary>
    public static string[][] Fis(int fisNo, Data.Fatura f, string cariHesabi, string satisHesabi, string kdvHesabi)
    {
        var gun = TrBicim.Gun(f.Tarih);
        var aciklama = $"Satış faturası {f.FaturaNo}" + (string.IsNullOrWhiteSpace(f.AliciUnvan) ? "" : $" · {f.AliciUnvan}");
        var net = f.Toplam - f.KdvTutar;
        var kdvYuzde = Math.Round(f.KdvOran * 100m, 0).ToString(CultureInfo.InvariantCulture);
        string[] S(string hesap, decimal borc, decimal alacak, string acik) =>
            [fisNo.ToString(CultureInfo.InvariantCulture), gun, hesap, f.FaturaNo!, gun, acik, TrBicim.Tutar(borc), TrBicim.Tutar(alacak), "0", "", ""];
        var satirlar = new List<string[]> { S(cariHesabi, f.Toplam, 0m, aciklama), S(satisHesabi, 0m, net, aciklama) };
        if (f.KdvTutar != 0m) satirlar.Add(S(kdvHesabi, 0m, f.KdvTutar, $"Hesaplanan KDV %{kdvYuzde} · {f.FaturaNo}"));
        return [.. satirlar];
    }

    public static string Csv(IEnumerable<string[]> satirlar)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(';', Kolonlar)).Append("\r\n");
        foreach (var s in satirlar) sb.Append(string.Join(';', s.Select(Hucre))).Append("\r\n");
        return sb.ToString();
        static string Hucre(string d) => d.Contains(';') || d.Contains('"') || d.Contains('\n') ? "\"" + d.Replace("\"", "\"\"") + "\"" : d;
    }

    public async IAsyncEnumerable<(string DosyaAdi, byte[] Icerik, int SatirSayisi)> Dosyalar(KuyumcuDbContext db, string tenantId)
    {
        var (fisler, eksik) = await Fisler(db, tenantId);
        var utf8 = new UTF8Encoding(true);
        var tr1254 = Encoding.GetEncoding(1254);
        var parca = 0;
        foreach (var grup in fisler.Chunk(FisSiniri))
        {
            parca++;
            var metin = Csv(grup.SelectMany(g => g));
            var satir = grup.Sum(g => g.Length);
            yield return ($"luca-fis-utf8-{parca}.csv", utf8.GetPreamble().Concat(utf8.GetBytes(metin)).ToArray(), satir);
            yield return ($"luca-fis-1254-{parca}.csv", tr1254.GetBytes(metin), satir);
        }
        var not = new StringBuilder()
            .AppendLine("LUCA — Muhasebe > Fiş İşlemleri > Excel Veri Aktarımı · ayraç \"; (Türkçe)\".")
            .AppendLine($"Fatura başına 1 mahsup fişi: müşteri hesabı borç · {satisHesabi} alacak (KDV hariç) · {kdvHesabi} alacak (KDV). En çok {FisSiniri} fiş/dosya.")
            .AppendLine("Aynı içerik iki kodlamayla verildi (utf8 / 1254): Türkçe harfler bozuk görünürse öbür dosyayı yükleyin.")
            .AppendLine($"Fiş sayısı: {fisler.Count} · dosya: {parca}.");
        if (eksik.Count > 0)
        {
            not.AppendLine().AppendLine("DOSYAYA GİRMEYEN FATURALAR (bilgi uydurulmadı — eksik tamamlanınca yeniden aktarın):");
            foreach (var e in eksik) not.Append("  - ").AppendLine(e);
        }
        yield return ("luca-fis-OKUBENI.txt", utf8.GetBytes(not.ToString()), 0);
    }
}

/// <summary>Zirve "Fatura Excel" aktarım dosyası (§ 2a) — Sayfa1, resmi başlık düzeni + 2026 KDV oranları.</summary>
public sealed class ZirveFaturaKaynagi : IDisaAktarilabilir, IDosyaUreten
{
    public static readonly string[] Kolonlar =
        ["Tarih", "Türü", "Cari Adı/Açıklama", "Evrak Numarası", "Vergi/T.C Kimlik No",
         "matrah%20", "kdv%20", "matrah%10", "kdv%10", "matrah%1", "kdv%1", "matrah%0",
         "Para Birimi", "Döviz Tutarı", "Stok Kodu", "Miktar"];
    private static readonly int[] Oranlar = [20, 10, 1];

    public string Ad => "zirve-fatura";
    public bool KisiselVeri => true;   // alıcı VKN/TCKN taşır
    public string[] Basliklar => Kolonlar;

    public async IAsyncEnumerable<string[]> Satirlar(KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var (satirlar, _) = await Satirlar(db, tenantId);
        foreach (var s in satirlar) yield return s.Select(h => h is decimal d ? d.ToString(CultureInfo.InvariantCulture) : h?.ToString() ?? "").ToArray();
    }

    /// <summary>Hücreler: string ya da decimal (sayı hücresi).</summary>
    public async Task<(List<object?[]> Satirlar, List<string> Eksik)> Satirlar(KuyumcuDbContext db, string tenantId)
    {
        var faturalar = await (
            from f in db.Faturalar.AsNoTracking().Where(x => x.TenantId == tenantId && x.Tur == "SATIS" && (x.Durum == "KESILDI" || x.Durum == "ODENDI"))
            join c in db.Cariler.AsNoTracking().Where(x => x.TenantId == tenantId) on f.CariId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            orderby f.Tarih, f.Id
            select new { f, cariAd = c != null ? c.Ad : null, cariVkn = c != null ? c.VergiNo : null })
            .Take(10_000).ToListAsync();
        var satirlar = new List<object?[]>();
        var eksik = new List<string>();
        foreach (var r in faturalar)
        {
            var vkn = r.f.AliciVergiNo ?? r.cariVkn;
            if (string.IsNullOrWhiteSpace(vkn)) { eksik.Add($"{r.f.FaturaNo ?? r.f.Id}: alıcının VKN/TCKN'si yok ({r.f.AliciUnvan ?? r.cariAd})"); continue; }
            var s = Satir(r.f, r.f.AliciUnvan ?? r.cariAd ?? "", vkn.Trim());
            if (s is null) { eksik.Add($"{r.f.FaturaNo ?? r.f.Id}: KDV oranı %{Math.Round(r.f.KdvOran * 100m, 0)} için sütun yok"); continue; }
            satirlar.Add(s);
        }
        return (satirlar, eksik);
    }

    /// <summary>Tek fatura → satır (saf). KUYUM: KDV matrahı oran sütununa, has bedeli matrah%0'a. Oran sütunu yoksa null.</summary>
    public static object?[]? Satir(Data.Fatura f, string cariAd, string vkn)
    {
        var oran = (int)Math.Round(f.KdvOran * 100m, 0);
        var idx = Array.IndexOf(Oranlar, oran);
        if (idx < 0 && f.KdvTutar != 0m) return null;
        var h = new object?[Kolonlar.Length];
        h[0] = TrBicim.Gun(f.Tarih); h[1] = "Satış Faturası"; h[2] = cariAd; h[3] = f.FaturaNo ?? ""; h[4] = vkn;
        var kdvsiz = f.KdvTuru == "KUYUM" ? f.HasDeger : 0m;
        var matrah = f.Toplam - f.KdvTutar - kdvsiz;
        if (idx >= 0) { h[5 + idx * 2] = matrah; h[6 + idx * 2] = f.KdvTutar; }
        else kdvsiz += matrah;   // KDV'siz fatura (oran 0)
        h[11] = kdvsiz == 0m ? null : kdvsiz;
        return h;
    }

    public async IAsyncEnumerable<(string DosyaAdi, byte[] Icerik, int SatirSayisi)> Dosyalar(KuyumcuDbContext db, string tenantId)
    {
        var (satirlar, eksik) = await Satirlar(db, tenantId);
        yield return ("zirve-fatura.xlsx", XlsxYazici.Yaz("Sayfa1", Kolonlar, satirlar), satirlar.Count);
        var not = new StringBuilder()
            .AppendLine("ZİRVE — Genel Muhasebe > Fatura Excel: Alt+S dosyayı seç, F5 tanım tablosunda sütun sıra numaralarını eşle, F3 fişe aktar, F2 kaydet.")
            .AppendLine("Sütunlar: 1 Tarih · 2 Türü · 3 Cari · 4 Evrak no · 5 VKN/TCKN · 6-7 matrah/KDV %20 · 8-9 %10 · 10-11 %1 · 12 KDV'siz tutar (kuyum has bedeli, özel matrah) · 13-16 boş.")
            .AppendLine($"Satır: {satirlar.Count}.");
        if (eksik.Count > 0)
        {
            not.AppendLine().AppendLine("DOSYAYA GİRMEYEN FATURALAR:");
            foreach (var e in eksik) not.Append("  - ").AppendLine(e);
        }
        yield return ("zirve-fatura-OKUBENI.txt", new UTF8Encoding(true).GetBytes(not.ToString()), 0);
    }
}

/// <summary>Kütüphanesiz .xlsx (OpenXML): tek sayfa, satır içi metin ve sayı hücreleri.</summary>
public static class XlsxYazici
{
    public static byte[] Yaz(string sayfaAdi, string[] basliklar, IReadOnlyList<object?[]> satirlar)
    {
        var sheet = new StringBuilder();
        sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
             .Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        Satir(sheet, 1, basliklar);
        for (var i = 0; i < satirlar.Count; i++) Satir(sheet, i + 2, satirlar[i]);
        sheet.Append("</sheetData></worksheet>");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            Ekle(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
                + "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Ekle(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Ekle(zip, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
                + $"<sheets><sheet name=\"{SecurityElement.Escape(sayfaAdi)}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Ekle(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Ekle(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return ms.ToArray();
    }

    private static void Satir(StringBuilder sb, int satirNo, object?[] hucreler)
    {
        sb.Append("<row r=\"").Append(satirNo).Append("\">");
        for (var c = 0; c < hucreler.Length; c++)
        {
            var h = hucreler[c];
            if (h is null) continue;
            var adres = SutunAdi(c) + satirNo;
            if (h is decimal d)
                sb.Append("<c r=\"").Append(adres).Append("\"><v>").Append(d.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
            else
                sb.Append("<c r=\"").Append(adres).Append("\" t=\"inlineStr\"><is><t>").Append(SecurityElement.Escape(h.ToString()!)).Append("</t></is></c>");
        }
        sb.Append("</row>");
    }

    public static string SutunAdi(int indeks)
    {
        var s = "";
        for (var n = indeks; n >= 0; n = n / 26 - 1) s = (char)('A' + n % 26) + s;
        return s;
    }

    private static void Ekle(ZipArchive zip, string ad, string icerik)
    {
        var e = zip.CreateEntry(ad, CompressionLevel.Optimal);
        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
        w.Write(icerik);
    }
}
