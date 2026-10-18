using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-04 (18.10, rakip eşitleme) — KUR KİLİDİ. Müşteriye söylenen kur süresi boyunca geçerli kalır:
// "Kuru kilitle" (15 dk · 1 saat; sahip/müdür 1 gün · 7 gün · 30 gün) güncel kuru sunucuda kilitler,
// tezgâh o kur kaydıyla fiyatlar ve satışı kilit kimliğiyle gönderir (sunucu tazelik yerine kilidi denetler).
// Müşteri seçiliyse kilit o müşteriye bağlanır; müşteri sonra gelince "Kilitli kur var · Kullan".
// Kilit yardımcı veridir: okunamazsa tezgâh kapanmaz, sebep not satırına yazılır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private List<KurKilidiOzet> kurKilitleri = [];
    private KurKilidiOzet? kurKilidi;
    private string? kilitNot;
    private bool kilitNotKotu, kilitSecenekAcik, kilitleniyor;

    private bool YoneticiMi => Oturum.Kullanici?.Rol is "SAHIP" or "MUDUR";

    /// <summary>Süre seçenekleri (dakika). Tezgâhtar 60 dakikaya kadar.</summary>
    private IReadOnlyList<int> KilitSureleri => YoneticiMi ? [15, 60, 1440, 10080, 43200] : [15, 60];

    /// <summary>Kilit satışta ve sipariş tesliminde geçerlidir.</summary>
    private bool KilitTurunde => tur is "SATIS" or DigerTurleri.Siparis;

    /// <summary>Kilidin bağlanacağı müşteri: satışta seçili müşteri (Peşin = yok), siparişte siparişin müşterisi.</summary>
    private string? KilitCariId => tur == DigerTurleri.Siparis ? diger.Siparis.Secili?.CariId
        : string.IsNullOrEmpty(cariId) || cariId == pesinCariId ? null : cariId;

    private string? KilitliKurId => kurKilidi is { } k && k.Bitis > DateTime.UtcNow ? k.Id : null;

    /// <summary>Satış gövdesine kilit kimliği (yalnız satışta; kilit yoksa alan hiç gitmez).</summary>
    private void KilitGovdeye(Dictionary<string, object?> d)
    {
        if (tur == "SATIS" && KilitliKurId is { } id) d["kurKilidiId"] = id;   // sipariş teslimi kendi gövdesinde gönderir
    }

    private async Task KilitleriYukle()
    {
        try { kurKilitleri = await Api.GetAsync<List<KurKilidiOzet>>("/kur-kilitleri") ?? []; }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { kurKilitleri = []; kilitNot = ex.Message; kilitNotKotu = true; }
    }

    /// <summary>Seçili müşteriye kullanılabilecek kilitler (müşterisiz ya da aynı müşteri; kullanılmakta olan hariç).</summary>
    private IEnumerable<KurKilidiOzet> UygunKilitler => kurKilitleri.Where(k =>
        k.Bitis > DateTime.UtcNow && k.Id != kurKilidi?.Id
        && (k.CariId is null ? KilitCariId is null : k.CariId == KilitCariId));

    private async Task KuruKilitle(int dakika)
    {
        if (kilitleniyor || string.IsNullOrEmpty(kurId)) return;
        kilitleniyor = true;
        try
        {
            var k = await Api.PostAsync<KurKilidiOzet>("/kur-kilitleri", new
            {
                kurSnapshotId = kurId,
                cariId = KilitCariId,
                sureDakika = dakika,
            });
            kurKilidi = k; kilitSecenekAcik = false; kilitNot = null;
            await KilitleriYukle();
        }
        catch (ApiHata ex) { kilitNot = ex.Message; kilitNotKotu = true; }
        finally { kilitleniyor = false; }
    }

    private async Task KilidiKullan(string id)
    {
        if (kurKilitleri.FirstOrDefault(k => k.Id == id) is not { } k) return;
        kurKilidi = k; kilitNot = null;
        KurYansit();
        await ListeFarkiniDenetle();   // sepette güncel kurla fiyatlanmış kalem varsa yeni fiyat SORULUR
    }

    private async Task KilidiKaldir()
    {
        if (kurKilidi is not { } k) return;
        try { await Api.PostAsync<object>($"/kur-kilitleri/{Uri.EscapeDataString(k.Id)}/kaldir"); kilitNot = null; }
        catch (ApiHata ex) { kilitNot = ex.Message; kilitNotKotu = true; }
        kurKilidi = null; KurYansit();
        await KilitleriYukle();
    }

    /// <summary>Fiş bitti/boşaltıldı: kilit tezgâhtan çıkar (sunucuda süresince durur, aynı müşteri dönerse yeniden kullanılır).</summary>
    private void KilitBirak()
    {
        if (kurKilidi is null) return;
        kurKilidi = null; KurYansit();
    }

    /// <summary>Müşteri değişti: başka müşterinin kilidi tezgâhta kalmaz.</summary>
    private void KilitMusteriyeGore()
    {
        if (kurKilidi is { CariId: { } c } && c != KilitCariId) KilitBirak();
    }

    private string KilitSureMetni(int dk) => dk switch
    {
        < 60 => string.Format(D1.DakikaKisa, dk),
        < 1440 => string.Format(D1.SaatKisa, dk / 60),
        _ => string.Format(D1.GunKisa, dk / 1440),
    };

    private string KilitOzetMetni(KurKilidiOzet k) => string.Join(" · ", new[]
    {
        $"{D1.HasKisa} {Bicim.TlKurus(k.AltinHasFiyati)}",
        string.Format(D1.KilitBitis, Bicim.IsletmeZamani(k.Bitis).Date == Bicim.IsletmeZamani(DateTime.UtcNow).Date
            ? Bicim.Saat(Bicim.IsletmeZamani(k.Bitis)) : Bicim.TarihSaat(k.Bitis)),
        k.CariAd,
    }.Where(p => !string.IsNullOrWhiteSpace(p)));

    private YildizKurKilidiBaglami YildizKilit => new()
    {
        Gorunur = KilitTurunde && !string.IsNullOrEmpty(kurId),
        Etkin = KilitliKurId is null ? null : KilitOzetMetni(kurKilidi!),
        SecenekAcik = kilitSecenekAcik,
        Sureler = [.. KilitSureleri.Select(d => (d, KilitSureMetni(d)))],
        Uygunlar = [.. UygunKilitler.Take(3).Select(k => (k.Id, KilitOzetMetni(k)))],
        Not = kilitNot, NotKotu = kilitNotKotu, Calisiyor = kilitleniyor,
        AcKapa = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => kilitSecenekAcik = !kilitSecenekAcik),
        Kilitle = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(this, KuruKilitle),
        Kullan = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, KilidiKullan),
        Kaldir = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, KilidiKaldir),
    };
}
