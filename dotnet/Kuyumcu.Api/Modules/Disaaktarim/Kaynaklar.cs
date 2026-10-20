using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Disaaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// DIŞA AKTARIM KAYNAKLARI — Bertan'ın mimarisi
//
// "Her modüle ayrı export ucu YOK. Tek `Disaaktarim` modülü + `IDisaAktarilabilir`
//  sözleşmesi; modül IQueryable verir, ortak yazıcı CSV üretir. Yoksa 18 modül ×
//  export = 18 kopya."
//
// ⚠ BERTAN'IN UYARISI: "Export tek yerden çalıştığı için kiracı filtresini BİR KEZ
//    yanlış yaparsanız tüm müşterilerin verisi tek dosyada dışarı çıkar."
//    Bu yüzden kaynaklar `db.Set<T>()` üzerinden çalışır — global sorgu filtresi
//    (ADR-0001) devrededir — VE ayrıca `TenantId` şartı elle yazılır (ADR-0010).
//    İki katman: biri kaldırılırsa diğeri tutar. Mutasyon testi ikisini de sınar.
//
// ⚠ 8 YILLIK VERİ BELLEĞE ALINMAZ: `AsAsyncEnumerable()` + `AsNoTracking()`.
//    `ToListAsync()` bu dosyada YASAKTIR.
// ═════════════════════════════════════════════════════════════════════════════

public interface IDisaAktarilabilir
{
    /// <summary>URL ve dosya adında kullanılan kısa ad.</summary>
    string Ad { get; }

    /// <summary>Bu kaynak kişisel veri içeriyor mu? (maskeleme ve denetim izi için)</summary>
    bool KisiselVeri { get; }

    string[] Basliklar { get; }

    IAsyncEnumerable<string[]> Satirlar(KuyumcuDbContext db, string tenantId, bool maskeli);
}

public static class Maske
{
    /// <summary>Son 4 hane görünür — MASAK belge no maskesiyle aynı desen.</summary>
    public static string? Kimlik(string? d, bool maskeli)
    {
        if (!maskeli || string.IsNullOrEmpty(d)) return d;
        return d.Length <= 4 ? new string('*', d.Length) : new string('*', d.Length - 4) + d[^4..];
    }

    /// <summary>E-posta: yerel kısmın ilk harfi + alan adı.</summary>
    public static string? Eposta(string? d, bool maskeli)
    {
        if (!maskeli || string.IsNullOrEmpty(d)) return d;
        var at = d.IndexOf('@');
        return at <= 0 ? "***" : d[0] + "***" + d[at..];
    }

    /// <summary>Adres tamamen gizlenir — kısmi adres de kimliklendiricidir.</summary>
    public static string? Adres(string? d, bool maskeli) =>
        !maskeli || string.IsNullOrEmpty(d) ? d : "[MASKELİ]";
}

// ── Cari ─────────────────────────────────────────────────────────────────────

public sealed class CariKaynagi : IDisaAktarilabilir
{
    public string Ad => "cari";
    public bool KisiselVeri => true;
    public string[] Basliklar =>
        ["id", "tur", "ad", "telefon", "email", "vergiNo", "adres", "krediLimitiTl",
         // Üç paralel defter AYRI kolonlarda taşınır; TOPLANMAZ (K27).
         // PAKET CARI-BAKIYE (tur 2): hasGramBakiye = ekrandaki TEK TANIM (emanetsiz, karar 3);
         // emanet AYRI kolonda (emanetteHas), kayıtlı kolon kayitliHasGramBakiye olarak açıkça adlanır.
         "tlBakiye", "hasGramBakiye", "emanetteHas", "kayitliHasGramBakiye", "usdBakiye", "eurBakiye", "gbpBakiye",
         "aktif", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu =
            from c in db.Cariler.AsNoTracking().Where(x => x.TenantId == tenantId)
            join g in Common.CariBakiyeKurali.Gorunum(db, tenantId)
                on c.Id equals g.Bakiye.CariId into bg
            from g in bg.DefaultIfEmpty()
            orderby c.Olusturuldu
            select new { c, b = g == null ? null : g.Bakiye, emanetsiz = g == null ? 0m : g.EmanetsizHas };

        await foreach (var r in sorgu.AsAsyncEnumerable())
            yield return
            [
                r.c.Id, r.c.Tur, r.c.Ad,
                Maske.Kimlik(r.c.Telefon, maskeli) ?? "",
                Maske.Eposta(r.c.Email, maskeli) ?? "",
                Maske.Kimlik(r.c.VergiNo, maskeli) ?? "",
                Maske.Adres(r.c.Adres, maskeli) ?? "",
                CsvYazici.Sayi(r.c.KrediLimitiTl),
                CsvYazici.Sayi(r.b == null ? 0m : r.b.TlBakiye),
                CsvYazici.Sayi(r.emanetsiz),
                CsvYazici.Sayi(r.b == null ? 0m : r.emanetsiz - r.b.HasGramBakiye),
                CsvYazici.Sayi(r.b == null ? 0m : r.b.HasGramBakiye),
                CsvYazici.Sayi(r.b == null ? 0m : r.b.UsdBakiye),
                CsvYazici.Sayi(r.b == null ? 0m : r.b.EurBakiye),
                CsvYazici.Sayi(r.b == null ? 0m : r.b.GbpBakiye),
                CsvYazici.Evet(r.c.Aktif),
                CsvYazici.Tarih(r.c.Olusturuldu),
            ];
    }
}

// ── Ürün ─────────────────────────────────────────────────────────────────────

public sealed class UrunKaynagi : IDisaAktarilabilir
{
    public string Ad => "urun";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        // maden: ayarın madeni — "hasGrami hangi madenin gramı" sorusu dosyadan
        // da cevaplanabilsin (paket 20, K20-3). rodyumKaplama: etiket ibaresi.
        ["id", "tip", "kod", "ad", "ayarId", "maden", "rodyumKaplama",
         "milyem", "gramAgirlik", "tasAgirlik",
         "tasKokeni", "tasIbaresi", "hasGrami", "iscilikTl", "satisFiyati",
         "barkod", "kmtsKodu", "aktif", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Kod);

        await foreach (var u in sorgu.AsAsyncEnumerable())
            yield return
            [
                u.Id, u.Tip, u.Kod, u.Ad, u.AyarId,
                Kuyumcu.Core.Domain.Ayarlar.MadenBul(u.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                CsvYazici.Evet(u.RodyumKaplama),
                CsvYazici.Sayi(u.Milyem), CsvYazici.Sayi(u.GramAgirlik),
                CsvYazici.Sayi(u.TasAgirlik),
                u.TasKokeni ?? "", u.TasIbaresi ?? "",
                CsvYazici.Sayi(u.HasGrami), CsvYazici.Sayi(u.IscilikTl),
                CsvYazici.Sayi(u.SatisFiyati),
                u.Barkod ?? "", u.KmtsKodu ?? "",
                CsvYazici.Evet(u.Aktif), CsvYazici.Tarih(u.Olusturuldu),
            ];
    }
}

// ── Satış / hareket defteri ──────────────────────────────────────────────────

public sealed class HareketKaynagi : IDisaAktarilabilir
{
    public string Ad => "satis";
    public bool KisiselVeri => false;
    // nakitTutar/veresiyeTutar: ödeme kırılımı da taşınabilir olmalı (ADR-0014).
    // Boş hücre = "bilinmiyor" (alan eklenmeden önce yazılmış satır), 0 ile aynı DEĞİLDİR.
    public string[] Basliklar =>
        ["id", "tur", "tarih", "subeId", "cariId", "urunId", "tlTutari", "paraBirimi",
         "hasGrami", "maden", "dovizTutar", "odemeTuru", "nakitTutar", "veresiyeTutar",
         "faturaNo", "kurSnapshotId", "kullaniciId", "aciklama"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .OrderBy(h => h.Tarih).ThenBy(h => h.Id);

        await foreach (var h in sorgu.AsAsyncEnumerable())
            yield return
            [
                h.Id, h.Tur, CsvYazici.Tarih(h.Tarih), h.SubeId,
                h.CariId ?? "", h.UrunId ?? "",
                CsvYazici.Sayi(h.TlTutari), h.ParaBirimi, CsvYazici.Sayi(h.HasGrami),
                h.Maden,
                // Boş hücre = döviz bacağı YOK (TL/has satırı); 0 ile aynı DEĞİLDİR.
                CsvYazici.Sayi(h.DovizTutar),
                h.OdemeTuru ?? "",
                CsvYazici.Sayi(h.NakitTutar), CsvYazici.Sayi(h.VeresiyeTutar),
                h.FaturaNo ?? "", h.KurSnapshotId, h.KullaniciId,
                h.Aciklama ?? "",
            ];
    }
}

// ── Fatura ───────────────────────────────────────────────────────────────────

public sealed class FaturaKaynagi : IDisaAktarilabilir
{
    public string Ad => "fatura";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "faturaNo", "cariId", "tur", "kdvTuru", "hasDeger", "araToplam",
         "kdvOran", "kdvTutar", "toplam", "paraBirimi", "durum", "tarih", "aciklama"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.Faturalar.AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.Tarih).ThenBy(f => f.Id);

        await foreach (var f in sorgu.AsAsyncEnumerable())
            yield return
            [
                f.Id, f.FaturaNo ?? "", f.CariId, f.Tur, f.KdvTuru,
                CsvYazici.Sayi(f.HasDeger), CsvYazici.Sayi(f.AraToplam),
                CsvYazici.Sayi(f.KdvOran), CsvYazici.Sayi(f.KdvTutar),
                CsvYazici.Sayi(f.Toplam), f.ParaBirimi, f.Durum,
                CsvYazici.Tarih(f.Tarih), f.Aciklama ?? "",
            ];
    }
}

// ── Kasa hareketi ────────────────────────────────────────────────────────────

public sealed class KasaHareketKaynagi : IDisaAktarilabilir
{
    public string Ad => "kasa";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "kasaId", "hareketId", "tutar", "paraBirimi", "odemeTuru",
         "komisyonOran", "blokajBitis", "tarih"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderBy(k => k.Tarih).ThenBy(k => k.Id);

        await foreach (var k in sorgu.AsAsyncEnumerable())
            yield return
            [
                k.Id, k.KasaId, k.HareketId ?? "",
                CsvYazici.Sayi(k.Tutar), k.ParaBirimi, k.OdemeTuru,
                CsvYazici.Sayi(k.KomisyonOran), CsvYazici.Tarih(k.BlokajBitis),
                CsvYazici.Tarih(k.Tarih),
            ];
    }
}

// ── POS (paket 31) ───────────────────────────────────────────────────────────
// ⚠ KİŞİSEL VERİ YOK: cihaz tanımı ve tutar/valör bilgisi taşır, maskeleme
// gerekmez. Sayı/tarih biçimlemesi CsvYazici üzerinden InvariantCulture'dır
// (tr-TR'de "1,85" üretip CSV'yi bozma tuzağı, §6).

public sealed class PosCihazKaynagi : IDisaAktarilabilir
{
    public string Ad => "pos-cihazlari";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "subeId", "ad", "bankaAdi", "uyeIsyeriNo", "terminalNo", "kasaId",
         "bankaHesapId", "paraBirimi", "pesinKomisyonOran", "valorGun", "aktif", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.PosCihazlari.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Ad).ThenBy(c => c.Id);

        await foreach (var c in sorgu.AsAsyncEnumerable())
            yield return
            [
                c.Id, c.SubeId, c.Ad, c.BankaAdi, c.UyeIsyeriNo ?? "", c.TerminalNo ?? "",
                c.KasaId, c.BankaHesapId ?? "", c.ParaBirimi,
                CsvYazici.Sayi(c.PesinKomisyonOran),
                c.ValorGun.ToString(System.Globalization.CultureInfo.InvariantCulture),
                c.Aktif ? "1" : "0", CsvYazici.Tarih(c.Olusturuldu),
            ];
    }
}

public sealed class PosTahsilatKaynagi : IDisaAktarilabilir
{
    public string Ad => "pos-tahsilatlari";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "subeId", "posCihazId", "hareketId", "kasaHareketId", "kurSnapshotId", "cariId",
         "brutTutar", "komisyonOran", "komisyonTutar", "netTutar", "yatanTutar",
         "taksitAdedi", "paraBirimi", "islemTarihi", "valorTarihi", "durum", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.PosTahsilatlari.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.IslemTarihi).ThenBy(t => t.Id);

        await foreach (var t in sorgu.AsAsyncEnumerable())
            yield return
            [
                t.Id, t.SubeId, t.PosCihazId, t.HareketId ?? "", t.KasaHareketId ?? "",
                t.KurSnapshotId, t.CariId ?? "",
                CsvYazici.Sayi(t.BrutTutar), CsvYazici.Sayi(t.KomisyonOran),
                CsvYazici.Sayi(t.KomisyonTutar), CsvYazici.Sayi(t.NetTutar),
                CsvYazici.Sayi(t.YatanTutar),
                t.TaksitAdedi.ToString(System.Globalization.CultureInfo.InvariantCulture),
                t.ParaBirimi, CsvYazici.Tarih(t.IslemTarihi), CsvYazici.Tarih(t.ValorTarihi),
                t.Durum, CsvYazici.Tarih(t.Olusturuldu),
            ];
    }
}

public sealed class PosEslesmeKaynagi : IDisaAktarilabilir
{
    public string Ad => "pos-eslesmeleri";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "posTahsilatId", "bankaHareketId", "eslesenTutar", "otomatik",
         "eslestirenKullaniciId", "geriAlindi", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.PosYatmaEslesmeleri.AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderBy(e => e.Olusturuldu).ThenBy(e => e.Id);

        await foreach (var e in sorgu.AsAsyncEnumerable())
            yield return
            [
                e.Id, e.PosTahsilatId, e.BankaHareketId, CsvYazici.Sayi(e.EslesenTutar),
                e.Otomatik ? "1" : "0", e.EslestirenKullaniciId,
                e.GeriAlindi ? "1" : "0", CsvYazici.Tarih(e.Olusturuldu),
            ];
    }
}

// ── Gider pusulası ───────────────────────────────────────────────────────────

public sealed class GiderPusulasiKaynagi : IDisaAktarilabilir
{
    public string Ad => "gider-pusulasi";
    public bool KisiselVeri => true;
    public string[] Basliklar =>
        ["id", "pusulaNo", "hareketId", "subeId", "cariId", "saticiTipi", "saticiAdSoyad",
         "saticiTckn", "saticiAdres", "ayarId", "gramAgirlik", "hasGrami", "malAciklamasi",
         "brutTutar", "stopajOran", "stopajTutar", "netOdenen", "teslimTarihi",
         "duzenlemeSonTarihi", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == tenantId)
            .OrderBy(g => g.PusulaNo);

        // ⚠ SaticiTckn diske AES-256-GCM şifreli iner; EF converter'ı burada
        // ÇÖZER. Maskeleme bu yüzden çözülmüş metnin üzerine uygulanır —
        // maskeli mod, şifreli metni değil gerçek TCKN'yi gizler.
        await foreach (var g in sorgu.AsAsyncEnumerable())
            yield return
            [
                g.Id, g.PusulaNo, g.HareketId, g.SubeId, g.CariId ?? "",
                g.SaticiTipi, g.SaticiAdSoyad,
                Maske.Kimlik(g.SaticiTckn, maskeli) ?? "",
                Maske.Adres(g.SaticiAdres, maskeli) ?? "",
                g.AyarId, CsvYazici.Sayi(g.GramAgirlik), CsvYazici.Sayi(g.HasGrami),
                g.MalAciklamasi ?? "",
                CsvYazici.Sayi(g.BrutTutar), CsvYazici.Sayi(g.StopajOran),
                CsvYazici.Sayi(g.StopajTutar), CsvYazici.Sayi(g.NetOdenen),
                CsvYazici.Tarih(g.TeslimTarihi), CsvYazici.Tarih(g.DuzenlemeSonTarihi),
                CsvYazici.Tarih(g.Olusturuldu),
            ];
    }
}

// ── PAKET P-26 (Y-34) — satınalma siparişi ───────────────────────────────────

public sealed class SatinalmaKaynagi : IDisaAktarilabilir
{
    public string Ad => "satinalma";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "no", "durum", "subeId", "tedarikciCariId", "hesapKodu", "beklenenTarih",
         "aciklama", "iptalGerekce", "olusturuldu"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.SatinalmaSiparisleri.AsNoTracking()
            .Where(s => s.TenantId == tenantId).OrderBy(s => s.No);

        await foreach (var s in sorgu.AsAsyncEnumerable())
            yield return
            [
                s.Id, s.No, s.Durum, s.SubeId, s.TedarikciCariId,
                // ⚠ BOŞSA BOŞ: hesap kodu uydurulmaz (Y-37 · Anayasa md.1).
                s.HesapKodu ?? "",
                s.BeklenenTarih?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "",
                s.Aciklama ?? "", s.IptalGerekce ?? "", CsvYazici.Tarih(s.Olusturuldu),
            ];
    }
}

// ── PAKET P-26 (Y-35) — irsaliye ─────────────────────────────────────────────

public sealed class IrsaliyeKaynagi : IDisaAktarilabilir
{
    public string Ad => "irsaliye";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["id", "no", "yon", "durum", "subeId", "cariId", "tarih", "satinalmaSiparisId",
         "faturaId", "bagsiz", "hesapKodu", "aciklama", "iptalGerekce"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu = db.Irsaliyeler.AsNoTracking()
            .Where(i => i.TenantId == tenantId).OrderBy(i => i.Tarih).ThenBy(i => i.Id);

        await foreach (var i in sorgu.AsAsyncEnumerable())
            yield return
            [
                i.Id, i.No, i.Yon, i.Durum, i.SubeId, i.CariId, CsvYazici.Tarih(i.Tarih),
                i.SatinalmaSiparisId ?? "", i.FaturaId ?? "",
                // Bağsızlık GİZLENMEZ: mali müşavir hangi sevkin faturasız
                // kaldığını dosyanın kendisinden görür.
                i.FaturaId is null && i.Durum == IrsaliyeDurumu.Acik ? "EVET" : "HAYIR",
                i.HesapKodu ?? "", i.Aciklama ?? "", i.IptalGerekce ?? "",
            ];
    }
}

// ── PAKET P-26 (Y-37) — MUHASEBE FİŞİ ────────────────────────────────────────

/// <summary>
/// Mali müşavire gidecek fiş dökümü (fatura bazlı).
///
/// <para>
/// ⚠ <b>BİLİNMEYEN SATIR UYDURULMAZ</b> (P-26 DoD · Anayasa md.1): carinin
/// muhasebe hesap kodu tanımlı değilse <c>hesapKodu</c> sütunu <b>BOŞ</b> çıkar
/// ve <c>eksikAlan</c> sütunu bunu yazar. "120" gibi bir varsayılan koymak mali
/// müşavire yanlış fiş göndermek olurdu; boş hücre görülür, yanlış hücre görülmez.
/// </para>
/// <para>
/// ⚠ Özel matrah sütunları faturanın KENDİ kayıtlı değerlerinden gelir
/// (<c>ozel_matrah_kodu</c>, <c>has_deger</c>) — bu kaynak hiçbir matrah
/// HESAPLAMAZ; tek motor <c>OzelMatrahMotoru</c>'dur.
/// </para>
/// <para>
/// ⚠ TASLAK fatura fişe GİRMEZ: henüz kesilmemiş belge muhasebeleştirilemez.
/// </para>
/// </summary>
public sealed class MuhasebeFisiKaynagi : IDisaAktarilabilir
{
    public string Ad => "muhasebe-fisi";
    public bool KisiselVeri => false;
    public string[] Basliklar =>
        ["faturaId", "faturaNo", "tarih", "tur", "durum", "cariId", "hesapKodu",
         "kdvTuru", "ozelMatrahKodu", "ozelMatrahTutar", "matrah", "kdvOran", "kdvTutar",
         "belgeToplam", "paraBirimi", "eksikAlan"];

    public async IAsyncEnumerable<string[]> Satirlar(
        KuyumcuDbContext db, string tenantId, bool maskeli)
    {
        var sorgu =
            from f in db.Faturalar.AsNoTracking().Where(x => x.TenantId == tenantId
                                                          && x.Durum != "TASLAK")
            join c in db.Cariler.AsNoTracking().Where(x => x.TenantId == tenantId)
                on f.CariId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            orderby f.Tarih, f.Id
            select new { f, hesapKodu = c != null ? c.HesapKodu : null };

        await foreach (var r in sorgu.AsAsyncEnumerable())
        {
            var eksik = new List<string>();
            if (string.IsNullOrWhiteSpace(r.hesapKodu)) eksik.Add("hesapKodu");
            if (r.f.KdvTuru == "KUYUM" && string.IsNullOrWhiteSpace(r.f.OzelMatrahKodu))
                eksik.Add("ozelMatrahKodu");
            if (string.IsNullOrWhiteSpace(r.f.FaturaNo)) eksik.Add("faturaNo");

            yield return
            [
                r.f.Id, r.f.FaturaNo ?? "", CsvYazici.Tarih(r.f.Tarih), r.f.Tur, r.f.Durum,
                r.f.CariId,
                r.hesapKodu ?? "",                 // ⚠ BOŞ = tanımsız, uydurulmadı
                r.f.KdvTuru,
                r.f.OzelMatrahKodu ?? "",          // ⚠ BOŞ = özel matrah uygulanmadı
                CsvYazici.Sayi(r.f.HasDeger),
                CsvYazici.Sayi(r.f.AraToplam),
                CsvYazici.Sayi(r.f.KdvOran),
                CsvYazici.Sayi(r.f.KdvTutar),
                CsvYazici.Sayi(r.f.Toplam),
                r.f.ParaBirimi,
                string.Join("|", eksik),
            ];
        }
    }
}

// ── Kayıt ────────────────────────────────────────────────────────────────────

public static class Kaynaklar
{
    /// <summary>
    /// İlk turda 6 varlık (Bertan): cari, ürün, satış, fatura, kasa hareketi,
    /// gider pusulası. Yeni kaynak eklemek buraya bir satırdır.
    /// </summary>
    public static readonly IReadOnlyList<IDisaAktarilabilir> Hepsi =
    [
        new CariKaynagi(), new UrunKaynagi(), new HareketKaynagi(),
        new FaturaKaynagi(), new KasaHareketKaynagi(), new GiderPusulasiKaynagi(),
        // Paket 31 — POS yan defteri (ADR-0014 dışa aktarım kapsamı).
        new PosCihazKaynagi(), new PosTahsilatKaynagi(), new PosEslesmeKaynagi(),
        // PAKET P-26 — tedarik zinciri + muhasebe köprüsü (Y-34 · Y-35 · Y-37).
        new SatinalmaKaynagi(), new IrsaliyeKaynagi(), new MuhasebeFisiKaynagi(),
        // 20.10 — mali müşavir programları (M11/D1-14): Luca fiş CSV, Zirve fatura xlsx (MuhasebeAktarimlari.cs).
        new LucaFisKaynagi(), new ZirveFaturaKaynagi(),
    ];

    public static IDisaAktarilabilir? Bul(string ad) =>
        Hepsi.FirstOrDefault(k => k.Ad == ad);

    public static string[] Adlar => [.. Hepsi.Select(k => k.Ad)];
}
