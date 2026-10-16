using System.Collections;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// P-03 — JSON ŞEKİL KİLİDİ [HTTP, gerçek MySQL]  (R-10, hasım turu KRİTİK bulgusu)
//
// PROBLEM: sunucu uçlarının çoğu `Task<object>` döndürür (anonim nesne). Anonim
// nesnenin yansımayla eşleştirilebileceği bir DTO TİPİ YOKTUR; bu yüzden
// SozlesmeYansimaTestleri.Ciftler() tablosu bu uçları kapsayamaz. Alan adı ya da
// tipi kayarsa DERLEME YEŞİL KALIR, JSON ayrıştırıcısı alanı sessizce atlar,
// ekran null/0 gösterir (QA turu 2 dersi).
//
// ÇÖZÜM: gerçek HTTP yanıtının şekli, istemci tipinin ALANLARIYLA karşılaştırılır.
// İstemci tipinin her alanı yanıtta camelCase olarak BULUNMALI ve JSON türü
// uyumlu olmalıdır. Sunucu bir alanı yeniden adlandırırsa bu test KIRMIZIYA döner.
//
// KAPSAM (Anayasa md.1): bu dosya finansal/kritik yüzeyi kilitler, TAMAMINI değil.
// Kalan kilitsiz tipler SozlesmeKapsamTestleri.KilitsizBeklenen cırcırında
// ADIYLA yazılıdır ve denetim.sh SOZLESME_KILITSIZ_TIP sayacıyla ölçülür.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class SozlesmeSekilTestleri(HttpFikstur f)
{
    /// <summary>Canlı JSON şekliyle kilitlenen istemci tipleri (kapsam sayacı bunu okur).</summary>
    public static readonly IReadOnlyList<Type> SekilKilitliTipler = Uclar()
        .Select(x => (Type)x[1]).Distinct().ToList();

    /// <summary>(URL kalıbı, istemci tipi) — `{kasa}` / `{cari}` fikstür kimlikleriyle doldurulur.</summary>
    public static TheoryData<string, Type> Uclar() => new()
    {
        { "/api/v1/kasalar/{kasa}/hareketler", typeof(Kuyumcu.Contracts.Sayfalar.Kasalar.HareketYaniti) },
        { "/api/v1/bankalar", typeof(Kuyumcu.Contracts.Sayfalar.Banka.HesapYaniti) },
        { "/api/v1/cari/{cari}/ekstre", typeof(Kuyumcu.Contracts.Sayfalar.Cariler.EkstreYaniti) },
        { "/api/v1/yetkiler", typeof(Kuyumcu.Contracts.Sayfalar.Yetkiler.YetkiMatrisi) },
        { "/api/v1/kur/guncel", typeof(Kuyumcu.Contracts.Sayfalar.Kur.KurVeri) },
        { "/api/v1/kur/guncel", typeof(Kuyumcu.Contracts.Sayfalar.Cariler.KurVeri) },
        { "/api/v1/kur/guncel", typeof(Kuyumcu.Contracts.Sayfalar.Emanet.KurVeri) },
        { "/api/v1/kur/guncel", typeof(Kuyumcu.Contracts.Sayfalar.Fiyatlandirma.KurOzet) },
        { "/api/v1/admin/tenant", typeof(Kuyumcu.Contracts.Sayfalar.Isletme.TenantVeri) },
        { "/api/v1/kurulum/durum", typeof(Kuyumcu.Core.Sozlesmeler.KurulumDurumYaniti) },
        { "/api/v1/raporlar/cari-yaslandirma", typeof(Kuyumcu.Contracts.Sayfalar.Raporlar.YaslandirmaYaniti) },
        { "/api/v1/raporlar/stok-degeri", typeof(Kuyumcu.Contracts.Sayfalar.Raporlar.StokYaniti) },
        // PAKET P-13: yeni iki uç KİLİTSİZ BIRAKILMADI (SozlesmeKapsamTestleri
        // cırcırı yeni kilitsiz tip kabul etmez) — şekil kilidi buraya eklendi.
        { "/api/v1/cari/{cari}/bagli-kayitlar", typeof(Kuyumcu.Contracts.Sayfalar.Cariler.CariBagliKayit) },
        { "/api/v1/urun-kategorileri/{kategori}/bagli-kayitlar", typeof(Kuyumcu.Contracts.Sayfalar.UrunAgaci.KategoriBagliKayit) },
        // PAKET T-P1: Panel "Bugün ne sattım?" kâr satırı — yeni istemci tipi KİLİTSİZ BIRAKILMADI.
        { "/api/v1/raporlar/kar-zarar?baslangic=2026-01-01&bitis=2026-12-31", typeof(Kuyumcu.Contracts.Sayfalar.Panel.PanelKarOzeti) },
        // PAKET EKSIK-B1-SATINALMA (tur 2): Satın alma + İrsaliyeler ekranlarının beş yeni istemci tipi
        // KİLİTSİZ BIRAKILMADI. {satinalma}/{irsaliye} için fikstürde sipariş açılır, onaylanır ve 2 adet
        // mal kabul yapılır → listeler DOLU döner (satır şekli de sınanır). "POST " önekli satır o mal
        // kabulün kendi yanıtını (MalKabulSonucu) sınar.
        { "/api/v1/satinalma", typeof(Kuyumcu.Contracts.Sayfalar.SatinAlma.SatinAlmaListesi) },
        { "/api/v1/satinalma/{satinalma}", typeof(Kuyumcu.Contracts.Sayfalar.SatinAlma.SatinAlmaDetay) },
        { "POST /api/v1/satinalma/{satinalma}/mal-kabul", typeof(Kuyumcu.Contracts.Sayfalar.SatinAlma.MalKabulSonucu) },
        { "/api/v1/irsaliyeler", typeof(Kuyumcu.Contracts.Sayfalar.SatinAlma.IrsaliyeListesi) },
        { "/api/v1/irsaliyeler/{irsaliye}", typeof(Kuyumcu.Contracts.Sayfalar.SatinAlma.IrsaliyeDetay) },
    };

    [Theory, MemberData(nameof(Uclar))]
    public async Task Sunucu_yaniti_istemci_tipinin_tum_alanlarini_tasir(string kalip, Type istemciTipi)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P03 sekil " + Guid.NewGuid().ToString("N")[..6]);

        var istemci = f.Ortam.Istemci(k, "SAHIP");

        // {kategori} fikstürü: kategori KiraciKur'da açılmıyor, uç için üretilir.
        var kategoriId = "";
        if (kalip.Contains("{kategori}"))
        {
            var olustur = await istemci.PostJson("/api/v1/urun-kategorileri",
                new { ad = "P03 sekil kategori", sira = 0 });
            Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
            kategoriId = (await olustur.Json()).GetProperty("id").GetString()!;
        }

        // PAKET EKSIK-B1-SATINALMA: satın alma / irsaliye uçları için dolu fikstür (sipariş → onay → 2 adet kabul).
        var satinalmaId = "";
        var irsaliyeId = "";
        JsonElement? malKabul = null;
        if (kalip.Contains("/satinalma") || kalip.Contains("/irsaliyeler"))
        {
            var sip = await istemci.PostJson("/api/v1/satinalma", new
            {
                tedarikciCariId = k.CariId,
                kalemler = new[] { new { urunId = k.UrunId, adet = 5m, birimFiyatTl = 1_000m } },
            });
            Assert.Equal(HttpStatusCode.OK, sip.StatusCode);
            var sipJ = await sip.Json();
            satinalmaId = sipJ.GetProperty("id").GetString()!;
            Assert.Equal(HttpStatusCode.OK, (await istemci.PostAsync($"/api/v1/satinalma/{satinalmaId}/onayla", null)).StatusCode);
            var kalemId = sipJ.GetProperty("kalemler")[0].GetProperty("id").GetString();
            var kabul = await istemci.PostJson($"/api/v1/satinalma/{satinalmaId}/mal-kabul",
                new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId, adet = 2m } } });
            Assert.Equal(HttpStatusCode.OK, kabul.StatusCode);
            malKabul = await kabul.Json();
            irsaliyeId = malKabul.Value.GetProperty("irsaliyeId").GetString()!;
        }

        var yol = kalip.Replace("{kasa}", k.KasaId).Replace("{cari}", k.CariId)
                       .Replace("{kategori}", kategoriId)
                       .Replace("{satinalma}", satinalmaId).Replace("{irsaliye}", irsaliyeId);
        JsonElement govde;
        if (kalip.StartsWith("POST ", StringComparison.Ordinal))
        {
            govde = malKabul ?? throw new InvalidOperationException($"POST kalıbı için fikstür yok: {kalip}");
        }
        else
        {
            var cevap = await istemci.GetAsync(yol);
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            govde = await cevap.Json();
        }

        var hata = new List<string>();
        Dogrula(istemciTipi, govde, istemciTipi.Name, hata);
        Assert.True(hata.Count == 0,
            $"SÖZLEŞME KAYMASI — {yol} yanıtı {istemciTipi.FullName} ile uyuşmuyor "
            + "(derleme yeşil kalır, ekran null/0 gösterir):\n  " + string.Join("\n  ", hata));
    }

    /// <summary>Şekil kilidi listesi ile uç tablosu birbirini doğrular (liste yalan söyleyemez).</summary>
    [Fact]
    public void Kilitli_tip_listesi_uc_tablosuyla_ayni()
    {
        Assert.NotEmpty(SekilKilitliTipler);
        Assert.Equal(Uclar().Select(x => (Type)x[1]).Distinct().OrderBy(t => t.FullName).ToList(),
                     SekilKilitliTipler.OrderBy(t => t.FullName).ToList());
    }

    // ── şekil doğrulayıcı ────────────────────────────────────────────────────
    private static void Dogrula(Type tip, JsonElement j, string yol, List<string> hata)
    {
        if (j.ValueKind != JsonValueKind.Object) { hata.Add($"{yol}: nesne beklendi, gelen {j.ValueKind}"); return; }
        foreach (var p in tip.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.Name == "EqualityContract") continue;
            // TEL ÜSTÜ ALAN = yazılabilir alan (record konum parametreleri init'tir).
            // Salt okunur hesaplanmış özellik (`EksikListe => Eksikler ?? []`) tel üstünde
            // YOKTUR ve olmaması doğrudur; onu alan sanmak sahte kırmızı üretir.
            if (!p.CanWrite) continue;
            var ad = char.ToLowerInvariant(p.Name[0]) + p.Name[1..];
            if (!j.TryGetProperty(ad, out var deger))
            {
                hata.Add($"{yol}.{ad} ALANI YANITTA YOK ({p.PropertyType.Name}) — istemci null/0 okur");
                continue;
            }
            TurUyar(p.PropertyType, deger, $"{yol}.{ad}", hata);
        }
    }

    private static void TurUyar(Type t, JsonElement j, string yol, List<string> hata)
    {
        var alt = Nullable.GetUnderlyingType(t);
        var nullOlabilir = alt is not null || !t.IsValueType;
        if (alt is not null) t = alt;

        if (j.ValueKind is JsonValueKind.Null)
        {
            if (!nullOlabilir) hata.Add($"{yol}: null geldi ama {t.Name} null kabul etmiyor");
            return;
        }

        if (t == typeof(string) || t == typeof(DateTime) || t == typeof(Guid))
        { Bekle(JsonValueKind.String, j, t, yol, hata); return; }

        if (t == typeof(bool))
        { if (j.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) hata.Add($"{yol}: bool beklendi, {j.ValueKind}"); return; }

        if (t.IsPrimitive || t == typeof(decimal))
        { Bekle(JsonValueKind.Number, j, t, yol, hata); return; }

        if (t.IsEnum) return;

        if (t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t))
        {
            var eleman = t.IsArray ? t.GetElementType()
                       : t.IsGenericType ? t.GetGenericArguments()[^1] : null;
            // Dictionary<string, X> JSON'da nesnedir, dizi değil.
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            { if (j.ValueKind != JsonValueKind.Object) hata.Add($"{yol}: nesne (sözlük) beklendi, {j.ValueKind}"); return; }

            if (j.ValueKind != JsonValueKind.Array) { hata.Add($"{yol}: dizi beklendi, {j.ValueKind}"); return; }
            var ilk = j.EnumerateArray().FirstOrDefault();
            if (eleman is not null && ilk.ValueKind != JsonValueKind.Undefined) TurUyar(eleman, ilk, yol + "[0]", hata);
            return;
        }

        Dogrula(t, j, yol, hata);
    }

    private static void Bekle(JsonValueKind beklenen, JsonElement j, Type t, string yol, List<string> hata)
    {
        if (j.ValueKind != beklenen) hata.Add($"{yol}: {t.Name} için {beklenen} beklendi, gelen {j.ValueKind}");
    }
}
