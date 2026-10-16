namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// DÜZELTME POLİTİKASI İSTEMCİSİ — paket UX-1 Faz A.
//
// ⚠ BERTAN KOŞUL 4 (TB-09 borcu büyümesin): sayfalar /meta/duzeltme-politikalari
//   ucunu DOĞRUDAN çağırmaz. Harita OTURUM BAŞINA BİR KEZ buradan çekilir ve
//   önbelleklenir. Sayfa başına politika çağrısı yapılırsa paket reddedilir.
//
// ⚠ MEHMET K8: HedefUc yalnız bir YOL DİZGESİDİR. Bu servis onu ASLA istek
//   kurmak için kullanmaz (Api.PatchAsync(satir.HedefUc) gibi bir çağrı YOKTUR);
//   ekran yalnız gösterir. Sunucu verisiyle yönlendirilen istek deseni açılmaz.
//   DuzeltmePolitikasiTestleri.Ekran_HedefUc_u_DINAMIK_CAGIRMAZ bunu tarar.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucudaki VarlikPolitikasi kaydının istemci karşılığı.</summary>
public sealed record PolitikaSatir(
    string Varlik,
    string Politika,
    string GerekceAnahtari,
    string? HedefUc,
    string? AlternatifYol);

public sealed record PolitikaHaritasi(
    List<PolitikaSatir> Varliklar,
    List<PolitikaSatir> HareketTurleri);

public sealed class DuzeltmePolitikaServisi(ApiIstemci api)
{
    public const string Duzenlenebilir = "DUZENLENEBILIR";
    public const string Storno = "STORNO";
    public const string SaltOkunur = "SALT_OKUNUR";

    private Task<PolitikaHaritasi>? cekim;
    private Dictionary<string, PolitikaSatir>? varliklar;
    private Dictionary<string, PolitikaSatir>? turler;

    /// <summary>Haritayı oturumda BİR KEZ çeker; sonraki çağrılar bellekten döner.</summary>
    public async Task Yukle()
    {
        if (varliklar is not null) return;
        cekim ??= api.GetAsync<PolitikaHaritasi>("/meta/duzeltme-politikalari");
        var harita = await cekim;
        varliklar = harita.Varliklar.ToDictionary(v => v.Varlik, StringComparer.Ordinal);
        turler = harita.HareketTurleri.ToDictionary(v => v.Varlik, StringComparer.Ordinal);
    }

    /// <summary>Varlık politikası — harita yüklü değilse null döner (çağıran önce Yukle çağırır).</summary>
    public PolitikaSatir? Varlik(string ad) =>
        varliklar is not null && varliklar.TryGetValue(ad, out var s) ? s : null;

    /// <summary>Hareket türü politikası (SATIS, ISCILIK, …).</summary>
    public PolitikaSatir? HareketTuru(string tur) =>
        turler is not null && turler.TryGetValue(tur, out var s) ? s : null;
}
