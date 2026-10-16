using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Satis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — KARMA ÖDEMEDE ALTIN BACAĞI (D3) · DEFTER YAZIMI
// Şartname: dokumanlar/02-mimari/sadelestirme/KARAR-HAS-DEFTERI-2026-09-23.md § 4
//
// KARAR (tek cümle): müşterinin ödeme olarak verdiği eski altın HER İKİ DEFTERE
// BİRDEN işlenir — aynı `hareketler` satırında has_grami (+) ve tl_tutari (−) —
// ve bu satır satışın ödeme satırı DEĞİL, satışla AYNI transaction'da ve AYNI
// KurSnapshotId ile yazılan AYRI bir HURDA_ALIS hareketidir (trampa iki ayrı
// teslimdir, KDVK md. 2/5).
//
// ⚠ YENİ BİR DEFTER YOLU İCAT EDİLMEZ: bu dosya mevcut HurdaAlis'in AYNISINI
//   yapar (§ 3.1 ölçümü) — tek fark para el DEĞİŞTİRMEZ (mahsup):
//     • ödeme izi OdemeIzi.Yok (0, 0, null) — nakit de veresiye de yok (B4-18)
//     • kasaya yazmaz, cariye has yazmaz (B4-19 · K3)
//     • gider pusulası AYNI commit'te doğar (B4-20)
//   Deftere yazmanın tek kapısı HareketFabrikasi.Ekle'dir (B4-15).
//
// ⚠ AYRI DOSYA NEDENİ: SatisModulu.cs 2600+ satırdır; altın bacağının yazma
//   kuralları tek yerde okunsun ve mutasyon kanıtları tek eşleşmeli olsun.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Altın bacağının deftere yazım sonucu — fiş yanıtına döner (UX-S27).</summary>
internal sealed record AltinYazimSonucu(string HareketId, AltinBacagiCozumu Cozum, GiderPusulasi? Pusula)
{
    public object Yanit() => new
    {
        hareketId     = HareketId,
        ayarId        = Cozum.Ayar.Id,
        maden         = Cozum.Ayar.Maden,
        gramAgirlik   = Cozum.Bacak.GramAgirlik,
        birimFiyatTlHasGram = Cozum.Bacak.BirimFiyatTlHasGram,
        hasGrami      = Cozum.Deger.HasGrami,
        brutTl        = Cozum.Deger.BrutTl,
        stopajOran    = Cozum.Deger.StopajOran,
        stopajTutarTl = Cozum.Deger.StopajTutar,
        netTl         = Cozum.Deger.NetTl,
        saticiTipi    = Cozum.SaticiTipi,
        // B4-35: fişte altın bacağı varsa İKİ belge doğar (satış fişi + gider
        // pusulası). Mükellef satıcıda pusula düzenlenmez; belgesi onun
        // faturasıdır (HurdaAlis ile aynı kural) — sayı 1 döner, uydurulmaz.
        belgeSayisi   = Pusula is null ? 1 : 2,
        giderPusulasi = Pusula is null ? null : new
        {
            id = Pusula.Id, Pusula.PusulaNo, Pusula.BrutTutar, Pusula.StopajOran,
            Pusula.StopajTutar, Pusula.NetOdenen, Pusula.DuzenlemeSonTarihi,
        },
    };
}

public sealed partial class SatisServisi
{
    /// <summary>
    /// B4-20 — gider pusulası numarası transaction ÖNCESİ alınır (HurdaAlis deseni).
    /// Altın bacağı yoksa ya da satıcı MÜKELLEF ise (pusula değil fatura) null.
    /// </summary>
    private async Task<string?> AltinPusulaNoAl(AltinBacagiCozumu? altin, AktifKullanici islem, DateTime now) =>
        altin is not null && SaticiTipi.PusulaGerekir(altin.SaticiTipi)
            ? await FaturaNoUretici.SiradakiPusula(db, islem.TenantId, Zaman.UtcdenYerele(now).Year)
            : null;

    /// <summary>
    /// B4-17 — HURDA_ALIS hareketini ve gider pusulasını AYNI transaction içinde
    /// yazar. Çağıran (SatisFisi) transaction'ı açmış olmalıdır; bu metot
    /// SaveChanges ÇAĞIRMAZ — yazımlar fişin tek commit'ine katılır (T-22).
    /// </summary>
    private AltinYazimSonucu AltinBacaginiYaz(
        AltinBacagiCozumu altin, string? pusulaNo, string kurSnapshotId, string? cariId,
        string fisId, string subeId, AktifKullanici islem, DateTime now)
    {
        _ = fisId; // bağ odeme_kalemleri(fis_id, hareket_id) üzerinden kurulur
        var hareketId = Kimlik.Ulid();
        var d = altin.Deger;

        HareketFabrikasi.Ekle(db,
            // B4-18: para el DEĞİŞTİRMEZ (mahsup) — nakit 0, veresiye 0, vade yok.
            // OdemeIzi'ne altın payı alanı EKLENMEZ (HareketFabrikasi.cs gerekçesi).
            OdemeIzi.Yok,
            new Hareket
            {
                Id            = hareketId,
                TenantId      = islem.TenantId,
                SubeId        = subeId,
                Tur           = "HURDA_ALIS",
                Tarih         = now,
                // MÜKELLEF satıcıda belge onun faturasıdır (HurdaAlis ile aynı).
                FaturaNo      = altin.Bacak.Satici!.TedarikciFaturaNo?.Trim(),
                // ⚠ FisId BİLEREK BOŞ: fiş kümesi (fis_id) fatura damgası ve fiş
                // toplamı için SATIŞ satırlarıdır; alış satırı oraya girseydi
                // satış faturası eksi bir alış satırı taşırdı. Bağ, ALTIN ödeme
                // kaleminin (fis_id, hareket_id) çiftindedir.
                Aciklama      = "Satış fişinde eski altınla ödeme (mahsup)",
                TlTutari      = Yuvarla2(-d.NetTl),     // mali bacak: stopaj sonrası NET (B4-17)
                MasakOlcuTl   = Yuvarla2(d.BrutTl),     // YASAL-3 § 4.1: MASAK ölçüsü BRÜT (pusulanın brüt bedeli)
                HasGrami      = Yuvarla6(d.HasGrami),   // fiziki bacak: has GİRİŞİ (B4-17)
                // K20-1: madeni satıcının getirdiği ayardan — gümüş altına karışmaz.
                Maden         = Kuyumcu.Core.Domain.Ayarlar.MadenBul(altin.Ayar.Id) ?? Kuyumcu.Core.Domain.Maden.Altin,
                // Özet etiket YOK: para el değiştirmedi; kırılım ALTIN ödeme kalemindedir.
                OdemeTuru     = null,
                // K-KUR-1: fişin KENDİ kur kaydı — ayrı bir kur yeniden seçilmez.
                KurSnapshotId = kurSnapshotId,
                CariId        = cariId,
                // P-08 / Y-14: hurda GRAMLA alınır; adet UYDURULMAZ.
                Adet          = null,
                KullaniciId   = islem.Id,
                Olusturuldu   = now,
            });

        // B4-12 / B4-20: pusulaya BRÜT verilir; stopaj ve net orada hesaplanır.
        var pusula = pusulaNo is null ? null : hurda.Kaydet(
            db, pusulaNo, altin.Bacak.Satici!, hareketId, subeId, cariId,
            altin.Ayar.Id, altin.Bacak.GramAgirlik!.Value, Yuvarla6(d.HasGrami), d.BrutTl, islem, now);

        return new AltinYazimSonucu(hareketId, altin, pusula);
    }

    /// <summary>
    /// GET /islemler/altin-bacagi/ayarlar — ekranın türetilen net TL'yi sunucuyla
    /// AYNI hesaplayabilmesi için stopaj oranları ve (varsa) alış iskontosu.
    /// Oranlar yapılandırmadan okunur; sunucu bunlarla fiyat TÜRETMEZ.
    /// </summary>
    public object AltinBacagiAyarlari() => new
    {
        stopajOranlari = SaticiTipi.Hepsi
            .Select(t => new { saticiTipi = t, oran = hurda.StopajOran(t), pusulaGerekir = SaticiTipi.PusulaGerekir(t) })
            .ToList(),
        // K-KUR-4: null = ön-dolgu YOK, alan boş açılır ve elle girilir.
        alisIskontoOran     = hurda.Ayarlar.AlisIskontoOran,
        saticiAdresiZorunlu = hurda.Ayarlar.SaticiAdresiZorunlu,
    };
}
