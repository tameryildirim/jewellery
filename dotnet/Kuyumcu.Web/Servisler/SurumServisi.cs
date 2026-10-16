namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// SÜRÜM KÜNYESİ İSTEMCİSİ — paket T-4 Faz A (T-4a).
//
// ⚠ TEK KAYNAK: künyeyi gösteren her yer (Ayarlar kartı, ileride /destek
//   sayfası) BU servisten okur. Sayfa kendi /meta/surum çağrısını açarsa
//   iki kopya künye doğar ve zamanla ayrışır (DuzeltmePolitikaServisi ile
//   aynı gerekçe).
//
// ⚠ HATA YUTULMAZ (UX-S28 · Anayasa md.1): çağrı patlarsa künye "bilinmiyor"
//   olarak DOLDURULMAZ; istisna çağırana çıkar, ekran hata bandı basar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucudaki SurumKunyesi kaydının istemci karşılığı.</summary>
public sealed record SurumKunyesi(
    string Surum,
    string DerlemeZamani,
    string DerlemeZamaniKaynagi,
    string Ortam,
    int MigrationSayisi,
    string? SonMigration,
    int BekleyenMigrationSayisi);

/// <summary>SAHIP ayrıntısı — uygulanmış/bekleyen şema adımı listeleri.</summary>
public sealed record SurumAyrintisi(
    string Surum,
    int MigrationSayisi,
    string? SonMigration,
    List<string> Uygulanmis,
    List<string> Bekleyen,
    string? Uyari);

public sealed class SurumServisi(ApiIstemci api)
{
    private Task<SurumKunyesi>? cekim;

    /// <summary>Son okunan künye (henüz okunmadıysa null).</summary>
    public SurumKunyesi? Kunye { get; private set; }

    /// <summary>Künyeyi oturumda BİR KEZ çeker; sonraki çağrılar bellekten döner.</summary>
    public async Task<SurumKunyesi> Yukle()
    {
        if (Kunye is not null) return Kunye;
        try
        {
            cekim ??= api.GetAsync<SurumKunyesi>("/meta/surum");
            Kunye = await cekim;
            return Kunye;
        }
        catch
        {
            // Başarısız çekim ÖNBELLEKLENMEZ: sonraki deneme yeniden ister.
            cekim = null;
            throw;
        }
    }

    /// <summary>Şema adımı ayrıntısı (sunucu kapısı: SAHIP). Önbelleklenmez.</summary>
    public Task<SurumAyrintisi> Ayrinti() => api.GetAsync<SurumAyrintisi>("/meta/surum/ayrinti");

    /// <summary>Destek çağrısında okunacak TEK satır künye.</summary>
    /// <remarks>
    /// PAKET DIL-SUNUCU: anahtarlar (surum/derleme/ortam…) ve derleme kaynağı sözcüğü (damga/dosya) istek diline
    /// göre <see cref="Kuyumcu.Core.I18n.SunucuIletiSozlugu.KunyeEtiketi"/>'nden gelir; TR çıktı ESKİSİYLE aynıdır.
    /// </remarks>
    public static string TekSatir(SurumKunyesi k, string dil = "tr")
    {
        string E(string a) => Kuyumcu.Core.I18n.SunucuIletiSozlugu.KunyeEtiketi(a, dil);
        return $"{E("surum")}={k.Surum} · {E("derleme")}={k.DerlemeZamani} ({E(k.DerlemeZamaniKaynagi)}) · "
            + $"{E("ortam")}={k.Ortam} · {E("migration")}={k.MigrationSayisi} · {E("son")}={k.SonMigration ?? "-"}"
            + (k.BekleyenMigrationSayisi > 0 ? $" · {E("bekleyen")}={k.BekleyenMigrationSayisi}" : "");
    }
}
