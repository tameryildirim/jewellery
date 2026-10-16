using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Fatura;

// ═════════════════════════════════════════════════════════════════════════════
// FATURA SERİ KAPISI — PAKET P-22 / B-3.
//
// ⚠ NEDEN VAR (ölçülmüş borç): fatura numarası ÜÇ ayrı yerde, belgenin
//   OLUŞTURULDUĞU anda tüketiliyordu (FaturaModulu.cs:261 · :713 · :944).
//   Belge ise TASLAK doğuyordu. Sonuç: taslağı düzeltmenin tek yolu "iptal et,
//   yenisini kes"ti ve her düzeltme numara serisinde KALICI BİR BOŞLUK
//   bırakıyordu. Seride boşluk açmak bir belge serisinde savunulabilir bir şey
//   değildir; taslak bir belge değil, bir niyettir.
//
// ⚠ SÖZLEŞME: seri YALNIZ BU SINIFTAN tüketilir (denetim.sh sayacı
//   P-22: SERI_TUKETIM_NOKTASI = 1 bunu kilitler). Belge KESİLDİĞİNDE
//   (Durum = KESILDI) numara alır; TASLAK belgenin numarası YOKTUR (NULL).
//
// ⚠ TASLAK DAMGASI: hareketler.fatura_no kolonu "bu hareket zaten faturalandı
//   mı" sorusunun ESKİ (emekli) cevabıdır. Taslak belgenin numarası olmadığı
//   için damga yerine <c>TASLAK:{faturaId}</c> İŞARETİ yazılır. İşaret seriden
//   DEĞİLDİR, numara tüketmez; belge kesilince gerçek numarayla değiştirilir,
//   taslak iptal edilince SİLİNİR (hareket yeniden faturalanabilir olur).
// ═════════════════════════════════════════════════════════════════════════════

public static class FaturaSeriKapisi
{
    /// <summary>Taslak işaretinin ön eki — seri numarası DEĞİLDİR.</summary>
    public const string TaslakOnEki = "TASLAK:";

    /// <summary>Bu damga bir seri numarası mı, yoksa taslak işareti mi?</summary>
    public static bool TaslakIsareti(string? damga) =>
        damga is not null && damga.StartsWith(TaslakOnEki, StringComparison.Ordinal);

    /// <summary>Verilen belge için taslak işareti.</summary>
    public static string Isaret(string faturaId) => TaslakOnEki + faturaId;

    /// <summary>
    /// SERİYİ TÜKETEN TEK NOKTA. Belgeye numara verir ve hareket damgalarını
    /// taslak işaretinden gerçek numaraya çevirir.
    ///
    /// <para>
    /// ⚠ FAIL-CLOSED: numarası olan belgeye ikinci kez numara verilmez ve
    /// TASLAK durumundaki belge numara ALMAZ — ikisi de çağıranın hatasıdır ve
    /// sessizce yutulmaz.
    /// </para>
    /// <para>⚠ <c>SaveChangesAsync</c> ÇAĞIRMAZ: çağıran kendi işleminde yazar.</para>
    /// </summary>
    public static async Task<string> Tuket(
        KuyumcuDbContext db, Data.Fatura fatura, int yil)
    {
        if (fatura.FaturaNo is not null)
            throw new InvalidOperationException(
                $"Fatura {fatura.Id} zaten {fatura.FaturaNo} numarasını taşıyor; seri iki kez tüketilemez.");
        if (fatura.Durum == "TASLAK")
            throw new InvalidOperationException(
                "TASLAK belge numara tüketemez; numara yalnız belge KESİLDİĞİNDE verilir.");

        var no = await FaturaNoUretici.Siradaki(db, fatura.TenantId, yil);
        fatura.FaturaNo = no;

        // Taslak işareti taşıyan hareketler gerçek numarayla damgalanır.
        var isaret = Isaret(fatura.Id);
        // ⚠ Sorgu TEK YOLDAN (FaturaHareketBagi) geçer — ADR-0018.
        var damgalilar = await FaturaHareketBagi.IsaretliHareketler(db, fatura.TenantId, isaret);
        foreach (var h in damgalilar) h.FaturaNo = no;

        return no;
    }

    /// <summary>
    /// Taslak işaretlerini kaldırır — taslak belge İPTAL edilince çağrılır.
    /// Hareket yeniden faturalanabilir hâle gelir; SERİDE BOŞLUK AÇILMAZ çünkü
    /// taslak hiç numara almamıştır.
    /// <para>⚠ <c>SaveChangesAsync</c> ÇAĞIRMAZ.</para>
    /// </summary>
    public static async Task IsaretiKaldir(KuyumcuDbContext db, Data.Fatura fatura)
    {
        var isaret = Isaret(fatura.Id);
        var damgalilar = await FaturaHareketBagi.IsaretliHareketler(db, fatura.TenantId, isaret);
        foreach (var h in damgalilar) h.FaturaNo = null;
    }
}
