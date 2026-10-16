using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// MUTABAKAT SÖZLEŞMESİ — PAKET P-24 (kimlik B-7, B-62).
//
// NEDEN VAR: bu üründe "özet" ile "liste" defalarca AYRI sorgudan türedi ve
// ekranda iki farklı gerçek gösterdi:
//   • vezne kuyruğu ₺0,00 ↔ fiş detayı 2.000 ₺
//   • evrak takvimi "portföyde evrak yok" ↔ portföyde 2 evrak
//   • kâr-zarar 11,45 g ↔ 50,38 has
//   • /denetim-izi üst kartı "Toplam kayıt 312" ↔ altındaki liste "0 / 7"
// Hepsinin kök nedeni AYNI: özet ucu, listenin süzgeç zincirini İKİNCİ KEZ
// yazıyordu. İkinci kopya günü gelince listeden ayrışıyor ve kullanıcı hangi
// sayının doğru olduğunu bilemiyor (Anayasa md.1: bilinmeyen tahmin edilmez).
//
// SÖZLEŞME: özet ve liste TEK süzgeç nesnesinden türer. Süzgeç nesnesi
// `MUTABAKAT-SUZGEC` yorumuyla işaretlenir; denetim.sh (P-24 bloğu) süzgeç
// parametresi alan her özet ucunun bu işareti taşıyan bir builder'dan
// geçtiğini ölçer (OZET_AYRI_SORGU = 0).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Özet ile listenin ayrıştığı an atılır. Sessizce yuvarlanmaz.</summary>
public sealed class MutabakatIhlali(string alan, decimal ozet, decimal liste)
    : InvalidOperationException(
        $"Mutabakat ihlali — '{alan}': özet {ozet}, listenin toplamı {liste}. " +
        "Özet ve liste AYNI sorgudan türemeli (P-24 mutabakat sözleşmesi).")
{
    public string Alan { get; } = alan;
    public decimal Ozet { get; } = ozet;
    public decimal Liste { get; } = liste;
}

public static class Mutabakat
{
    /// <summary>
    /// GÜN SONU KAPSAYICI ÜST SINIR (B-62 off-by-one).
    /// <para>
    /// Ekranlar tarih seçicisinden GÜN gönderir: <c>bitis=2026-09-22</c> →
    /// <c>2026-09-22T00:00:00</c>. Sunucu bunu <c>Tarih &lt; bitis</c> diye
    /// uygularsa BİTİŞ GÜNÜNÜN TAMAMI kapsam dışı kalır: kullanıcı "bugüne
    /// kadar" der, bugünü göremez. /denetim-izi'nde ölçülen kusur budur.
    /// </para>
    /// <para>
    /// KURAL: saat bileşeni YOKSA (00:00:00) tarih bir GÜN olarak okunur ve
    /// üst sınır ertesi günün başlangıcıdır (yarı açık aralık: [bas, bit+1g)).
    /// Saat bileşeni VARSA kullanıcı gerçekten bir ana kadar istemiştir;
    /// değer olduğu gibi kalır. Tahmin yok — iki hâl de sınanır.
    /// </para>
    /// </summary>
    public static DateTime? UstSinirKapsayici(DateTime? bitis) =>
        bitis is not { } b ? null
        : b.TimeOfDay == TimeSpan.Zero ? b.AddDays(1)
        : b;

    /// <summary>
    /// Özet alanı ile listenin toplamının DENK olduğunu doğrular; değilse
    /// <see cref="MutabakatIhlali"/> atar. Sayıyı "düzeltmez", GİZLEMEZ.
    /// </summary>
    public static void Denk(string alan, decimal ozet, decimal listeToplami)
    {
        if (ozet != listeToplami) throw new MutabakatIhlali(alan, ozet, listeToplami);
    }
}
