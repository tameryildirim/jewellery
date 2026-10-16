namespace Kuyumcu.Api.Data.GeriDoldurma;

/// <summary>
/// <c>belgeler.sahip_turu</c> / <c>sahip_id</c> alanları eklenmeden ÖNCE yazılmış
/// satırların sahibini doldurur (paket 4).
///
/// ⚠ DETERMİNİSTİK, TAHMİN YOK: eski tabloda (<c>urun_belgeleri</c>) her satırın
/// <c>urun_id</c>'si NOT NULL'du; yani belirsiz satır YOKTUR. Sahip tek kaynaktan
/// gelir: <c>sahip_id = urun_id</c>, <c>sahip_turu = 'URUN'</c>.
///
/// ⚠ WHERE KORUMASI ŞARTTIR: yalnız sahibi HENÜZ YAZILMAMIŞ satırlara dokunur.
/// Koruma kalkarsa, sahibi başka bir türe (ör. ISLETME) bağlanmış satırlar da
/// ezilir ve sessiz veri bozulması olur. Testi bu koruma üzerinden yazılmıştır.
///
/// Metin burada sabit tutulur ki migration ile TEST AYNI SQL'i koşsun.
/// </summary>
public static class BelgeSahipGeriDoldurma
{
    public const string UrunSahibi = """
        UPDATE belgeler
        SET sahip_turu = 'URUN', sahip_id = urun_id
        WHERE (sahip_id IS NULL OR sahip_id = '')
          AND urun_id IS NOT NULL AND urun_id <> ''
        """;

    /// <summary>Migration ve testin koştuğu sıra.</summary>
    public static readonly string[] Sirayla = [UrunSahibi];
}
