using System.Text;
using System.Text.Json.Nodes;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.EBelge;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// E-belge (20.10) — saf testler: fatura → model (EBelgeKurucu), Nilvera gövdeleri (alan adları OpenAPI'den),
// durum/mükellef/hata ayrıştırma. Eksik bilgi tahminle doldurulmaz: gönderim durur, sebep sade dille.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class EBelgeTestleri
{
    private const string Vkn = "1234567890", Tc = "10000000146";
    private static Tenant Isletme(string? vkn = Vkn) => new() { Id = "t", Ad = "Altın Çarşı Kuyumculuk", VergiNo = vkn, VergiDairesi = "Kapalıçarşı", Adres = "İstanbul" };

    private static Fatura Fatura(string? aliciVkn = Tc, string kdvTuru = "STANDART", string? ozelKod = null) => new()
    {
        Id = "f1", TenantId = "t", CariId = "c", FaturaNo = "FTR-2026-000007", Tur = "SATIS", KdvTuru = kdvTuru, Durum = "KESILDI",
        Toplam = 1200m, KdvTutar = 200m, KdvOran = 0.20m, ParaBirimi = "TRY", Tarih = new DateTime(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc),
        AliciUnvan = "Hatice Demirkol", AliciVergiNo = aliciVkn, OzelMatrahKodu = ozelKod,
    };

    private static FaturaKalemi Kalem(string kdvTuru, decimal toplam, decimal kdv, decimal matrah, decimal has, decimal miktar = 2m) => new()
    {
        Id = "k", TenantId = "t", FaturaId = "f1", Sira = 1, Aciklama = "22 ayar bilezik", Miktar = miktar, Birim = "ADET",
        KdvTuru = kdvTuru, KdvOran = 0.20m, SatirToplam = toplam, KdvTutar = kdv, KdvMatrah = matrah, HasDeger = has,
    };

    [Fact]
    public void Standart_kalem_KDV_haric_fiyat_ve_toplamlar_turkiye_saati_SATIS_tipi()
    {
        var m = EBelgeKurucu.Kur(Fatura(), [Kalem("STANDART", 1200m, 200m, 1000m, 0m)], Isletme(), "h@ornek.com", null,
            Guid.Empty, EBelgeSenaryolari.EArsiv, null, "mhk");
        Assert.Equal("SATIS", m.Tip);
        var s = Assert.Single(m.Satirlar);
        Assert.Equal((1000m, 500m, 20m, 200m, "C62"), (s.NetTutar, s.BirimFiyat, s.KdvYuzde, s.KdvTutar, s.Birim));
        Assert.Null(s.OzelMatrahTutar);
        Assert.Equal((1000m, 200m, 1200m), (m.NetToplam, m.KdvToplam, m.Odenecek));
        Assert.Equal(new DateTime(2026, 10, 20, 12, 30, 0), m.TarihYerel);   // 09:30 UTC = 12:30 TSİ
        Assert.Equal("MHK", m.SeriOnEki);
        Assert.Contains("Mihenk belge no: FTR-2026-000007", m.Notlar);
    }

    [Fact]
    public void Kuyum_kalemi_OZELMATRAH_matrah_satirda_kod_yoksa_gonderim_durur_tahmin_yok()
    {
        var k = Kalem("KUYUM", 10_100m, 100m, 500m, 9_500m, miktar: 1m);
        var ex = Assert.Throws<EBelgeHatasi>(() => EBelgeKurucu.Kur(Fatura(kdvTuru: "KUYUM"), [k], Isletme(), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null));
        Assert.False(ex.Gecici);
        Assert.Contains("özel matrah kodu", ex.Message);

        var m = EBelgeKurucu.Kur(Fatura(kdvTuru: "KUYUM", ozelKod: "805"), [k], Isletme(), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null);
        Assert.Equal(("OZELMATRAH", "805"), (m.Tip, m.OzelMatrahKodu));
        var s = Assert.Single(m.Satirlar);
        Assert.Equal((10_000m, 500m, 100m), (s.NetTutar, s.OzelMatrahTutar!.Value, s.KdvTutar));
    }

    [Fact]
    public void Isletme_VKN_ya_da_alici_kimligi_yoksa_kalici_hata()
    {
        var k = Kalem("STANDART", 1200m, 200m, 1000m, 0m);
        Assert.Contains("İşletmenin vergi numarası", Assert.Throws<EBelgeHatasi>(() =>
            EBelgeKurucu.Kur(Fatura(), [k], Isletme(null), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null)).Message);
        Assert.Contains("Müşterinin TC", Assert.Throws<EBelgeHatasi>(() =>
            EBelgeKurucu.Kur(Fatura(aliciVkn: null), [k], Isletme(), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null)).Message);
    }

    [Fact]
    public void Kalemsiz_fatura_tek_satir_baslik_toplamlariyla()
    {
        var m = EBelgeKurucu.Kur(Fatura(), [], Isletme(), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null);
        var s = Assert.Single(m.Satirlar);
        Assert.Equal((1m, 1000m, 200m), (s.Miktar, s.NetTutar, s.KdvTutar));
    }

    [Fact]
    public void Nilvera_eArsiv_govdesi_sema_alan_adlari_eposta_varsa_ELEKTRONIK_ozel_matrah_satirda()
    {
        var m = EBelgeKurucu.Kur(Fatura(kdvTuru: "KUYUM", ozelKod: "805"), [Kalem("KUYUM", 10_100m, 100m, 500m, 9_500m, 1m)], Isletme(), "h@ornek.com", null,
            Guid.Parse("11111111-2222-3333-4444-555555555555"), EBelgeSenaryolari.EArsiv, null, null);
        var j = JsonNode.Parse(NilveraSaglayici.EArsivGovdesi(m))!["ArchiveInvoice"]!;
        var bilgi = j["InvoiceInfo"]!;
        Assert.Equal("11111111-2222-3333-4444-555555555555", (string?)bilgi["UUID"]);
        Assert.Equal(("OZELMATRAH", "ELEKTRONIK", "NORMAL", "2026-10-20T12:30:00"), ((string?)bilgi["InvoiceType"], (string?)bilgi["SendType"], (string?)bilgi["SalesPlatform"], (string?)bilgi["IssueDate"]));
        Assert.Equal((10_000d, 100d, 10_100d, 100d), ((double)bilgi["LineExtensionAmount"]!, (double)bilgi["KdvTotal"]!, (double)bilgi["PayableAmount"]!, (double)bilgi["GeneralKDV20Total"]!));
        Assert.Equal((Vkn, Tc), ((string?)j["CompanyInfo"]!["TaxNumber"], (string?)j["CustomerInfo"]!["TaxNumber"]));
        var satir = j["InvoiceLines"]![0]!;
        Assert.Equal(("805", 500d, 20d), ((string?)satir["OzelMatrahReason"], (double)satir["OzelMatrahTotal"]!, (double)satir["KDVPercent"]!));
        // e-postasız müşteri: kâğıt
        var kagit = EBelgeKurucu.Kur(Fatura(), [Kalem("STANDART", 1200m, 200m, 1000m, 0m)], Isletme(), null, null, Guid.Empty, EBelgeSenaryolari.EArsiv, null, null);
        Assert.Equal("KAGIT", (string?)JsonNode.Parse(NilveraSaglayici.EArsivGovdesi(kagit))!["ArchiveInvoice"]!["InvoiceInfo"]!["SendType"]);
        Assert.Null(JsonNode.Parse(NilveraSaglayici.EArsivGovdesi(kagit))!["ArchiveInvoice"]!["InvoiceLines"]![0]!["OzelMatrahReason"]);
    }

    [Fact]
    public void Nilvera_eFatura_govdesi_TEMELFATURA_ve_posta_kutusu_etiketi()
    {
        var m = EBelgeKurucu.Kur(Fatura(aliciVkn: Vkn), [Kalem("STANDART", 1200m, 200m, 1000m, 0m)], Isletme(), null, null, Guid.Empty,
            EBelgeSenaryolari.EFatura, "urn:mail:defaultpk@ornek.com", null);
        var j = JsonNode.Parse(NilveraSaglayici.EFaturaGovdesi(m))!;
        Assert.Equal("urn:mail:defaultpk@ornek.com", (string?)j["CustomerAlias"]);
        Assert.Equal("TEMELFATURA", (string?)j["EInvoice"]!["InvoiceInfo"]!["InvoiceProfile"]);
    }

    [Fact]
    public void Durum_esleme_eArsiv_ve_eFatura()
    {
        static EBelgeDurumSonucu A(string j) => NilveraSaglayici.DurumAyristir(EBelgeSenaryolari.EArsiv, Encoding.UTF8.GetBytes(j));
        static EBelgeDurumSonucu F(string j) => NilveraSaglayici.DurumAyristir(EBelgeSenaryolari.EFatura, Encoding.UTF8.GetBytes(j));
        Assert.Equal(EBelgeDurumlari.Kabul, A("""{"StatusCode":"succeed","ReportStatus":"Reported","CancelStatus":false}""").Durum);
        Assert.Equal(EBelgeDurumlari.Gonderildi, A("""{"StatusCode":"waiting","CancelStatus":false}""").Durum);
        Assert.Equal((EBelgeDurumlari.Hata, "Alıcı VKN hatalı"), (A("""{"StatusCode":"error","StatusDetail":"Alıcı VKN hatalı"}""") is var a ? (a.Durum, a.Aciklama) : default));
        Assert.Equal(EBelgeDurumlari.Iptal, A("""{"StatusCode":"succeed","CancelStatus":true}""").Durum);
        Assert.Equal((EBelgeDurumlari.Ret, "fiyat yanlış"), (F("""{"Answer":{"AnswerCode":"rejected","AnswerNote":"fiyat yanlış"},"InvoiceStatus":{"Code":"succeed"}}""") is var r ? (r.Durum, r.Aciklama) : default));
        Assert.Equal(EBelgeDurumlari.Kabul, F("""{"Answer":{"AnswerCode":"unknown"},"InvoiceStatus":{"Code":"succeed"}}""").Durum);
    }

    [Fact]
    public void Mukellef_bos_dizi_kayitsiz_urn_etiketi_secilir_hata_iletisi_okunur()
    {
        Assert.False(NilveraSaglayici.MukellefAyristir("[]"u8.ToArray()).Kayitli);
        var m = NilveraSaglayici.MukellefAyristir(Encoding.UTF8.GetBytes("""[{"TaxNumber":"1234567890","Name":"GIB","Type":"PK"},{"Name":"urn:mail:defaultpk@ornek.com"}]"""));
        Assert.Equal((true, "urn:mail:defaultpk@ornek.com"), (m.Kayitli, m.Etiket));
        Assert.Equal("Alıcı VKN hatalı · Tutar tutmuyor", NilveraSaglayici.Ileti(Encoding.UTF8.GetBytes("""[{"Code":"1","Description":"Alıcı VKN hatalı"},{"Description":"Tutar tutmuyor"}]""")));
        Assert.Equal("Geçersiz model", NilveraSaglayici.Ileti(Encoding.UTF8.GetBytes("""{"Message":"Geçersiz model"}""")));
        Assert.False(NilveraSaglayici.Hata(System.Net.HttpStatusCode.UnprocessableEntity, "[]"u8.ToArray()).Gecici);
        Assert.True(NilveraSaglayici.Hata(System.Net.HttpStatusCode.ServiceUnavailable, []).Gecici);
        Assert.True(NilveraSaglayici.Hata(System.Net.HttpStatusCode.TooManyRequests, []).Gecici);
        Assert.False(NilveraSaglayici.Hata(System.Net.HttpStatusCode.Unauthorized, []).Gecici);
    }

    [Fact]
    public void Yeniden_deneme_araligi_artar_ve_iki_saatte_durur()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), EBelgeServisi.Aralik(1));
        Assert.True(EBelgeServisi.Aralik(4) > EBelgeServisi.Aralik(3));
        Assert.Equal(TimeSpan.FromMinutes(120), EBelgeServisi.Aralik(50));
    }
}
