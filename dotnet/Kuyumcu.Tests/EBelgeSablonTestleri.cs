using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Xsl;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Cikti;
using Kuyumcu.Api.Modules.EBelge;

namespace Kuyumcu.Tests;

// 20.10 — ByGatech e-belge görüntü şablonu (UBL-TR → HTML). .NET XSLT motoruyla işlenir (entegratör motorları XSLT 1.0;
// şablonda uzantı fonksiyonu yok). Örnek: Veri/ornek-earsiv-ubl.xml (kuyum özel matrahlı e-Arşiv).
public sealed class EBelgeSablonTestleri
{
    private static string Isle(string xslt, string xml)
    {
        var t = new XslCompiledTransform();
        using (var r = XmlReader.Create(new StringReader(xslt))) t.Load(r);
        using var g = XmlReader.Create(new StringReader(xml));
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, t.OutputSettings)) t.Transform(g, w);
        return sb.ToString();
    }

    private static string Ornek => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Veri", "ornek-earsiv-ubl.xml"));

    [Fact]
    public void eArsiv_ornegi_zorunlu_ogeler_kuyum_ayrintisi_ve_Turkce_tutarlarla_islenir()
    {
        var h = Isle(EBelgeSablonu.Metin(), Ornek);
        Assert.Contains("e-ARŞİV FATURA", h);
        Assert.Contains("Gelir İdaresi Başkanlığı", h);
        Assert.Contains("MHK2026000000014", h);
        Assert.Contains("7d1c2f9e-3a4b-4c5d-8e6f-0a1b2c3d4e5f", h);   // ETTN
        Assert.Contains("20.10.2026 14:32", h);                     // tarih + saat:dakika (e-Arşiv zorunlu)
        Assert.Contains("Hatice Demirkol", h);
        Assert.Contains("22 ayar · 10,00 g · has 9,1600 g · işçilik 1.200,00 TL", h);
        Assert.Contains("48.940,00 TL", h);
        Assert.Contains("805", h);
        Assert.Contains("e-Arşiv izni kapsamında elektronik ortamda iletilmiştir.", h);
    }

    [Fact]
    public void eFatura_profili_e_FATURA_yazar_eArsiv_iletim_ibaresi_yok_logo_adresi_verilirse_resim()
    {
        var xml = Ornek.Replace("<cbc:ProfileID>EARSIVFATURA</cbc:ProfileID>", "<cbc:ProfileID>TEMELFATURA</cbc:ProfileID>");
        var h = Isle(EBelgeSablonu.Metin("https://ornek.test/gib.png"), xml);
        Assert.Contains("e-FATURA", h);
        Assert.DoesNotContain("e-ARŞİV FATURA", h);
        Assert.DoesNotContain("elektronik ortamda iletilmiştir", h);
        Assert.Contains("src=\"https://ornek.test/gib.png\"", h);
    }

    [Fact]
    public void Kuyum_ayrintisi_dolu_parcalardan_kurulur()
    {
        var k = new FaturaKalemi { AyarId = "K22", GramAgirlik = 10m, HasGrami = 9.16m, IscilikTl = 1200m, Aciklama = "x" };
        Assert.Equal("22 ayar · 10,00 g · has 9,1600 g · işçilik 1.200,00 TL", EBelgeKurucu.KuyumAyrintisi(k));
        Assert.Equal("Gümüş · 5,50 g", EBelgeKurucu.KuyumAyrintisi(new FaturaKalemi { AyarId = "GUMUS", GramAgirlik = 5.5m, Aciklama = "x" }));
        Assert.Null(EBelgeKurucu.KuyumAyrintisi(new FaturaKalemi { Aciklama = "hizmet" }));
    }

    [Fact]
    public void Gonderim_govdesi_sablonu_base64_ve_kalem_aciklamasini_tasir()
    {
        var f = new Fatura { Id = "f", TenantId = "t", CariId = "c", Tur = "SATIS", KdvTuru = "STANDART", Durum = "KESILDI", Toplam = 1200m, KdvTutar = 200m,
            KdvOran = 0.2m, ParaBirimi = "TRY", Tarih = DateTime.UtcNow, AliciUnvan = "A", AliciVergiNo = "10000000146" };
        var k = new FaturaKalemi { Aciklama = "Bilezik", Miktar = 1m, Birim = "ADET", KdvTuru = "STANDART", KdvOran = 0.2m, SatirToplam = 1200m, KdvTutar = 200m,
            AyarId = "K22", GramAgirlik = 10m };
        var m = EBelgeKurucu.Kur(f, [k], new Tenant { Id = "t", Ad = "İşletme", VergiNo = "1234567890" }, null, null, Guid.Empty,
            EBelgeSenaryolari.EArsiv, null, null, EBelgeSablonu.Metin());
        var j = JsonNode.Parse(NilveraSaglayici.EArsivGovdesi(m))!["ArchiveInvoice"]!;
        var sablon = Encoding.UTF8.GetString(Convert.FromBase64String((string)j["InvoiceInfo"]!["TemplateBase64String"]!));
        Assert.StartsWith("<?xml", sablon);
        Assert.Contains("xsl:stylesheet", sablon);
        Assert.Equal("22 ayar · 10,00 g", (string?)j["InvoiceLines"]![0]!["Description"]);
        // şablon kapalıysa alan gönderilmez (entegratör varsayılanı)
        var m2 = m with { Sablon = null };
        Assert.Null(JsonNode.Parse(NilveraSaglayici.EArsivGovdesi(m2))!["ArchiveInvoice"]!["InvoiceInfo"]!["TemplateBase64String"]);
    }

    [Fact]
    public void Ic_dokum_resmi_belge_numarasini_ve_ETTNyi_yazar()
    {
        var e = Guid.Parse("7d1c2f9e-3a4b-4c5d-8e6f-0a1b2c3d4e5f");
        Assert.Equal("BİLGİ AMAÇLI DÖKÜMDÜR — resmi belge e-Arşiv fatura MHK2026000000014 · ETTN 7d1c2f9e-3a4b-4c5d-8e6f-0a1b2c3d4e5f.",
            CiktiServisi.FaturaEBelgeUyari("EARSIV", "MHK2026000000014", e));
        Assert.StartsWith("BİLGİ AMAÇLI DÖKÜMDÜR — resmi belge e-Fatura · ETTN", CiktiServisi.FaturaEBelgeUyari("EFATURA", null, e));
    }
}
