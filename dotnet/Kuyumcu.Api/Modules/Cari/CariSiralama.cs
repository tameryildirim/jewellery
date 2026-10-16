using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Cari;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — CARİ LİSTESİ SUNUCU SIRALAMASI (envanter § G1 "Sıralama")
//
// Liste uç sınırlıdır (limit=100): istemci yalnız GELEN sayfayı sıralarsa
// sınırın dışında kalan kayıtları hiç göstermez ve "en büyük borçlu" diye
// yanlış kişiyi gösterir. Bu yüzden sıralama SUNUCUDA yapılır.
// Parametre: sirala=<ad>&yon=artan|azalan. BEYAZ LİSTE: bilinmeyen ad ya da
// yön 400 (fail-closed; sessizce varsayılana düşmez — Anayasa md.1).
// Sırası kararlı olsun diye her sıralamanın sonunda ad + id gelir (sayfalar
// arasında satır kaybolmaz/yinelenmez).
// Has bakiyesi bilinçli olarak YOK: listedeki has EMANETSİZ hesaplanır
// (CariBakiyeKurali.EmanetsizHas) — ham sütuna göre sıralamak ekrandaki
// sayıyla çelişen bir sıra üretirdi.
// ═════════════════════════════════════════════════════════════════════════════

public static class CariSiralama
{
    public const string Ad = "ad";
    public const string Tur = "tur";
    public const string Telefon = "telefon";
    public const string TlBakiyeSirasi = "tl-bakiye";

    /// <summary>Sunucunun kabul ettiği sıralama adları — istemci kolon tanımındaki SunucuSiralamaAdi ile aynı.</summary>
    public static readonly string[] Tumu = [Ad, Tur, Telefon, TlBakiyeSirasi];

    public static IQueryable<Data.Cari> Uygula(
        IQueryable<Data.Cari> sorgu, KuyumcuDbContext db, string tenantId, string? sirala, string? yon)
    {
        if (string.IsNullOrEmpty(sirala))
        {
            if (!string.IsNullOrEmpty(yon))
                throw new BadRequestException("Sıralama yönü verildi ama sıralama alanı (sirala) yok.");
            return sorgu.OrderBy(c => c.Ad).ThenBy(c => c.Id);
        }
        if (!Tumu.Contains(sirala))
            throw new BadRequestException($"Bilinmeyen sıralama alanı: {sirala}. Geçerli: {string.Join(", ", Tumu)}.");
        var azalan = yon switch
        {
            null or "" or "artan" => false,
            "azalan" => true,
            _ => throw new BadRequestException("Sıralama yönü 'artan' ya da 'azalan' olmalı."),
        };

        return sirala switch
        {
            Tur => (azalan ? sorgu.OrderByDescending(c => c.Tur) : sorgu.OrderBy(c => c.Tur)).ThenBy(c => c.Ad).ThenBy(c => c.Id),
            Telefon => (azalan ? sorgu.OrderByDescending(c => c.Telefon) : sorgu.OrderBy(c => c.Telefon)).ThenBy(c => c.Ad).ThenBy(c => c.Id),
            TlBakiyeSirasi => (azalan
                    ? sorgu.OrderByDescending(c => db.CariBakiyeler.Where(b => b.TenantId == tenantId && b.CariId == c.Id).Select(b => (decimal?)b.TlBakiye).FirstOrDefault() ?? 0m)
                    : sorgu.OrderBy(c => db.CariBakiyeler.Where(b => b.TenantId == tenantId && b.CariId == c.Id).Select(b => (decimal?)b.TlBakiye).FirstOrDefault() ?? 0m))
                .ThenBy(c => c.Ad).ThenBy(c => c.Id),
            _ => (azalan ? sorgu.OrderByDescending(c => c.Ad) : sorgu.OrderBy(c => c.Ad)).ThenBy(c => c.Id),
        };
    }
}
