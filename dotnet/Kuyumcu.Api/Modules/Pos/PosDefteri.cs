using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Pos;

// ═════════════════════════════════════════════════════════════════════════════
// POS DEFTERİ — pos_tahsilatlari tablosuna yazan TEK YOL.
//
// HareketFabrikasi.Ekle deseninin POS karşılığıdır: statik, db'yi parametre
// alır, SaveChanges ÇAĞIRMAZ (çağıranın transaction'ının içindedir).
// `db.PosTahsilatlari.Add(` deseni TÜM AĞAÇTA yalnız bu dosyada geçer ve
// denetim.sh bunu sayar — ikinci bir yazma yolu açan paket orada kırmızıya döner.
//
// ⚠ BU BİR MUHASEBE MOTORU DEĞİLDİR. POS bir YAN DEFTERDİR; kasa bakiyesine,
// cari bakiyeye ve hareketler defterine HİÇ dokunmaz.
// ═════════════════════════════════════════════════════════════════════════════

public static class PosDefteri
{
    /// <summary>Bankacı yuvarlaması KULLANILMAZ (SatisModulu.Yuvarla2 deseni).</summary>
    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Kart bacağı için POS alacak satırı yazar. <paramref name="brut"/> İŞARETLİdir:
    /// satış +, iade −. Math.Abs kullanılmaz — defterin işareti kasa hareketinin
    /// işaretiyle aynı okunur.
    /// </summary>
    /// <returns>Eklenen satır (henüz SaveChanges edilmemiş).</returns>
    public static PosTahsilat Ekle(
        KuyumcuDbContext db, PosAyarlari ayarlar, PosCihaz cihaz,
        decimal brut, int taksit, string? hareketId, string? kasaHareketId,
        string kurSnapshotId, string? cariId, DateTime now,
        decimal? oranGecersizKilma = null, int? valorGunGecersizKilma = null)
    {
        // Oran ve valör günü SATIRA DONDURULUR (snapshot): cihaz ayarı sonradan
        // değişse de yazılmış satırlar DEĞİŞMEZ — kur_snapshot mantığının aynısı.
        var oran = oranGecersizKilma ?? cihaz.PesinKomisyonOran;
        var valorGun = valorGunGecersizKilma ?? cihaz.ValorGun;

        // İade bacağında komisyon: banka kesilen komisyonu genelde İADE ETMEZ
        // (varsayılan). Sözleşmesi farklı olan kuyumcu Pos.IadeKomisyonIadeEdilir
        // ile oransal negatif komisyon yazdırır.
        var komisyon = brut < 0m && !ayarlar.IadeKomisyonIadeEdilir
            ? 0m
            : Yuvarla2(brut * oran);

        var islem = now;
        var satir = new PosTahsilat
        {
            Id = Kimlik.Ulid(),
            TenantId = cihaz.TenantId,
            SubeId = cihaz.SubeId,
            PosCihazId = cihaz.Id,
            HareketId = hareketId,
            KasaHareketId = kasaHareketId,
            KurSnapshotId = kurSnapshotId,
            CariId = cariId,
            BrutTutar = Yuvarla2(brut),
            KomisyonOran = oran,
            KomisyonTutar = komisyon,
            NetTutar = Yuvarla2(brut - komisyon),
            YatanTutar = 0m,
            TaksitAdedi = taksit < 1 ? 1 : taksit,
            ParaBirimi = cihaz.ParaBirimi,
            IslemTarihi = islem,
            ValorTarihi = ValorHesapla(islem, valorGun, ayarlar.ValorIsGunu),
            Durum = PosTahsilatDurumu.Bekliyor,
            Olusturuldu = now,
        };
        db.PosTahsilatlari.Add(satir);
        return satir;
    }

    /// <summary>
    /// Valör tarihi = YEREL işlem gününün üstüne valör günü eklenmiş günün başı (UTC).
    /// <para>
    /// <paramref name="isGunu"/> true ise Cumartesi/Pazar ATLANIR. ⚠ RESMÎ TATİL
    /// TAKVİMİ YOKTUR: bayramda banka yatırmaz ama sistem o günü iş günü sayar.
    /// Bu sınır dürüstçe yazılıdır, uydurulmuş bir tatil tablosu eklenmedi.
    /// </para>
    /// </summary>
    public static DateTime ValorHesapla(DateTime islemUtc, int valorGun, bool isGunu)
    {
        var gun = DateOnly.FromDateTime(Zaman.UtcdenYerele(islemUtc));
        if (!isGunu)
        {
            gun = gun.AddDays(valorGun);
        }
        else
        {
            var kalan = valorGun;
            while (kalan > 0)
            {
                gun = gun.AddDays(1);
                if (gun.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) kalan--;
            }
            // Valör günü 0 olsa bile haftasonuna düşen işlem gününü ileri taşı.
            while (gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) gun = gun.AddDays(1);
        }
        return Zaman.YerelGunBasiUtc(gun);
    }

    /// <summary>
    /// TÜRETİLMİŞ ÖZET (Karar C): yatan_tutar ve durum, geri_alindi=false olan
    /// eşleşmelerin TOPLAMINDAN hesaplanır. İkisi de ELLE ATANMAZ — tek yazan
    /// yer burasıdır, aksi hâlde "çift gerçek" doğar.
    /// <para>IPTAL edilmiş satırın durumu KORUNUR (iptal bir son durumdur).</para>
    /// </summary>
    public static async Task Ozetle(KuyumcuDbContext db, string tenantId, string tahsilatId)
    {
        var t = await db.PosTahsilatlari
            .FirstOrDefaultAsync(x => x.Id == tahsilatId && x.TenantId == tenantId);
        if (t is null) return;

        var yatan = await db.PosYatmaEslesmeleri
            .Where(e => e.TenantId == tenantId && e.PosTahsilatId == tahsilatId && !e.GeriAlindi)
            .SumAsync(e => (decimal?)e.EslesenTutar) ?? 0m;

        t.YatanTutar = Yuvarla2(yatan);
        if (t.Durum == PosTahsilatDurumu.Iptal) return;

        t.Durum = t.YatanTutar <= 0m ? PosTahsilatDurumu.Bekliyor
                : t.YatanTutar >= t.NetTutar ? PosTahsilatDurumu.Yatti
                : PosTahsilatDurumu.Kismi;
    }
}
