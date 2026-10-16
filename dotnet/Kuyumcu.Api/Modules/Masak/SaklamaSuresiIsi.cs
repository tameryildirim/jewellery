using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// SAKLAMA SÜRESİ İŞİ — ADIM-A raporu §5/5'teki açığı kapatır.
// (`saklama_bitis` hesaplanıp saklanıyordu ama onu okuyan iş yoktu.)
//
// GERİLİM: MASAK 8 yıl saklama İSTER; KVKK "amaçla sınırlılık ve ölçülülük"
// gereği süre dolunca kişisel verinin tutulmamasını ister. Mehmet'in md.1
// çözümü: SİLME değil ANONİMLEŞTİRME — kayıt (tutar, tarih, kademe) kalır,
// KİMLİK alanları maskelenir. Böylece hem denetim izi bozulmaz hem kişisel
// veri gereğinden uzun tutulmaz.
//
// APPEND-ONLY İLE ÇELİŞMİYOR: kayıt silinmiyor, satır sayısı değişmiyor;
// yalnız süresi dolmuş kişisel alanlar maskeleniyor. Bu, saklama yükümlülüğünün
// SONA ERMESİNİN gereği — verinin tahrifi değil.
//
// PAKET YASAL-3 (§ 4.3) — KENDİLİĞİNDEN, 30 GÜN ÖNCEDEN HABER + GEREKÇELİ BEKLETME:
//   • Artık SAKLAMA_UYGULA işi (Isler/Isler/SaklamaUygulaIsi.cs) bu motoru günlük
//     çağırır (KVKK Silme Yön. md.7 resen imha; md.11/2 periyot en çok 6 ay).
//     Elle uç (/masak/saklama-uygula) aynı motoru kullanır — TEK MOTOR.
//     Mehmet'in ADR-0008 şartı ("haberi olmadan çalışmamalı") 30 gün önceden
//     SAHİP bildirimiyle karşılanır; Isler:SaklamaUygula:Kuru=true iken iş yalnız
//     bildirir, maskelemez (kurucu kararı K-2).
//   • SÜRE SON İŞLEMDEN (Tedbirler Yön. md.46 "son işlem tarihinden itibaren
//     sekiz yıl"): aday, (a) aynı belge_no_hash'e sahip, süresi DOLMAMIŞ daha yeni
//     bir tespit varsa, (b) kaydın carisiyle son SaklamaYili içinde herhangi bir
//     hareket varsa ATLANIR. saklama_bitis YENİDEN YAZILMAZ (append-only korunur).
//   • BEKLETME (saklama_bekletmeleri): aktif TÜMÜ bekletmesi hiçbir kaydı,
//     TESPİT bekletmesi o kaydı (ve beyanını) maskeletmez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SaklamaSuresiIsi(
    KuyumcuDbContext db, DenetimServisi denetim, MasakKuralMotoru? motor = null)
{
    /// <summary>Maskelenmiş alanın aldığı değer — okunduğunda neden boş olduğu bellidir.</summary>
    public const string Maskelenmis = "[SAKLAMA SÜRESİ DOLDU]";

    /// <summary>Önceden haber verme süresi (gün) — şartname § 4.3 md.1.</summary>
    public const int OncedenHaberGun = 30;

    /// <summary>Bir turda işlenen en çok aday (B-8 sınırlı döküm; kalan ertesi tura kalır).</summary>
    public const int AzamiParti = 5000;

    /// <summary>Aday seçiminin sonucu (kişisel veri yok — yalnız kimlikler/sayılar).</summary>
    public sealed record Adaylar(
        IReadOnlyList<string> TespitIdleri,
        IReadOnlyList<string> BeyanIdleri,
        int SonIslemNedeniyleAtlanan,
        int BekletmeNedeniyleAtlanan,
        string? EnYeniAdayId);

    private int SaklamaYili(DateTime an) =>
        motor?.EsiklerGunu(YasalParametreServisi.IslemGunu(an)).SaklamaYili ?? new MasakEsikleri().SaklamaYili;

    /// <summary>
    /// TEK AYIKLAMA MOTORU. <paramref name="altSinir"/> ≤ saklama_bitis &lt; <paramref name="ustSinir"/>
    /// olan, henüz maskelenmemiş tespitlerden son-işlem ve bekletme kurallarına takılmayanlar.
    /// Kurallar <paramref name="ustSinir"/> anına göre değerlendirilir (yaklaşan kayıtlar için
    /// "o gün maskelenecek mi" sorusu).
    /// </summary>
    public async Task<Adaylar> AdaylariBul(string tenantId, DateTime? altSinir, DateTime ustSinir) =>
        await AdaylariBul(tenantId, altSinir, ustSinir, AzamiParti);

    /// <remarks>
    /// PAKET YASAL-3 tur 2 (hasım bulgusu, AÇLIK): atlama kuralları (bekletme, son işlem)
    /// eskiden Take(AzamiParti) SONRASINDA bellekte uygulanıyordu → en eski AzamiParti aday
    /// kalıcı olarak atlanıyorsa daha yeni adaylar hiçbir turda seçilmezdi (KVKK md.11/2
    /// 6 ay sınırı delinebilirdi). Kurallar artık SORGUDA: Take yalnız GERÇEKTEN
    /// maskelenecek kayıtları sayar. <paramref name="parti"/> yalnız test için küçültülür.
    /// </remarks>
    public async Task<Adaylar> AdaylariBul(string tenantId, DateTime? altSinir, DateTime ustSinir, int parti)
    {
        var q = db.MasakKimlikTespitleri.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.SaklamaBitis < ustSinir && m.AdSoyad != Maskelenmis);
        if (altSinir is { } alt) q = q.Where(m => m.SaklamaBitis >= alt);

        // Bekletme — ustSinir anında AKTİF olanlar (başlamış, bitmemiş, kaldırılmamış).
        var bekletmeler = await db.SaklamaBekletmeleri.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.Kaldirildi == null
                     && b.Baslangic <= ustSinir && b.Bitis > ustSinir)
            .Select(b => new { b.Kapsam, b.KimlikTespitiId }).Take(AzamiParti).ToListAsync();
        var tumuBekliyor = bekletmeler.Any(b => b.Kapsam == SaklamaBekletmeKapsami.Tumu);
        var bekleyenTespit = bekletmeler.Where(b => b.KimlikTespitiId != null)
            .Select(b => b.KimlikTespitiId!).Distinct().ToList();

        // (a) aynı belge hash'iyle süresi dolmamış daha yeni tespit · (b) carisiyle son
        // SaklamaYili içinde hareket (müşteri ilişkisi sürüyor) — ikisi de SORGUDA.
        var yil = SaklamaYili(ustSinir);
        var esik = ustSinir.AddYears(-yil);
        var bekletmeDisi = q.Where(m => !bekleyenTespit.Contains(m.Id));
        var sonIslemVar = bekletmeDisi.Where(m =>
            db.MasakKimlikTespitleri.Any(n => n.TenantId == tenantId && n.BelgeNoHash == m.BelgeNoHash
                && n.SaklamaBitis >= ustSinir && n.AdSoyad != Maskelenmis)
            || (m.CariId != null && db.Hareketler.Any(h => h.TenantId == tenantId && h.CariId == m.CariId && h.Tarih >= esik)));
        var uygun = bekletmeDisi.Where(m =>
            !db.MasakKimlikTespitleri.Any(n => n.TenantId == tenantId && n.BelgeNoHash == m.BelgeNoHash
                && n.SaklamaBitis >= ustSinir && n.AdSoyad != Maskelenmis)
            && !(m.CariId != null && db.Hareketler.Any(h => h.TenantId == tenantId && h.CariId == m.CariId && h.Tarih >= esik)));

        List<(string Id, DateTime Bitis)> secilen;
        int sonIslem, bekletme;
        if (tumuBekliyor)
        {
            secilen = [];
            sonIslem = 0;
            bekletme = await q.CountAsync();
        }
        else
        {
            // Tur başına en çok `parti` UYGUN aday (en eski önce); kalan ertesi tura kalır (günlük iş).
            secilen = (await uygun.OrderBy(m => m.SaklamaBitis).ThenBy(m => m.Id)
                .Select(m => new { m.Id, m.SaklamaBitis }).Take(parti).ToListAsync())
                .Select(m => (m.Id, m.SaklamaBitis)).ToList();
            sonIslem = await sonIslemVar.CountAsync();
            bekletme = bekleyenTespit.Count == 0 ? 0 : await q.CountAsync(m => bekleyenTespit.Contains(m.Id));
        }
        var tespitIdleri = secilen.Select(s => s.Id).ToList();

        // Beyan tespitini İZLER: tespiti maskelenen (ya da zaten maskeli) beyan maskelenir;
        // tespiti atlanan beyan da atlanır.
        var bq = db.MasakBeyanlari.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.SaklamaBitis < ustSinir && b.IslemAmaci != Maskelenmis);
        if (altSinir is { } alt2) bq = bq.Where(b => b.SaklamaBitis >= alt2);
        // Seçim koşulu da SORGUDA (tur 2, açlık): tespiti bu turda seçilen beyan ya da
        // tespiti zaten maskeli / hiç bulunmayan (yetim → KENDİ süresiyle, paket öncesi davranış)
        // ve bekletmede olmayan beyan.
        var beyanlar = tumuBekliyor ? new List<string>() : await bq
            .Where(b => tespitIdleri.Contains(b.KimlikTespitiId)
                     || (!bekleyenTespit.Contains(b.KimlikTespitiId)
                         && (db.MasakKimlikTespitleri.Any(m => m.TenantId == tenantId
                                 && m.Id == b.KimlikTespitiId && m.AdSoyad == Maskelenmis)
                             || !db.MasakKimlikTespitleri.Any(m => m.TenantId == tenantId && m.Id == b.KimlikTespitiId))))
            .OrderBy(b => b.SaklamaBitis).ThenBy(b => b.Id)
            .Select(b => b.Id).Take(parti).ToListAsync();

        var enYeni = secilen.OrderByDescending(s => s.Bitis).ThenByDescending(s => s.Id).Select(s => s.Id).FirstOrDefault();
        return new Adaylar(tespitIdleri, beyanlar, sonIslem, bekletme, enYeni);
    }

    /// <summary>Süresi dolmuş kayıt sayıları — hiçbir şey değiştirmez.</summary>
    public async Task<object> Onizleme(Kuyumcu.Api.Auth.AktifKullanici islem)
    {
        var simdi = DateTime.UtcNow;
        var dolmus = await AdaylariBul(islem.TenantId, null, simdi);
        var yaklasan = await AdaylariBul(islem.TenantId, simdi, simdi.AddDays(OncedenHaberGun));

        var enEski = await db.MasakKimlikTespitleri.AsNoTracking()
            .Where(m => m.TenantId == islem.TenantId && m.AdSoyad != Maskelenmis)
            .OrderBy(m => m.SaklamaBitis)
            .Select(m => (DateTime?)m.SaklamaBitis)
            .FirstOrDefaultAsync();

        var tespit = dolmus.TespitIdleri.Count;
        var beyan = dolmus.BeyanIdleri.Count;
        return new
        {
            simdi,
            suresiDolmus = new { kimlikTespiti = tespit, beyan },
            // PAKET YASAL-3: önümüzdeki 30 gün içinde KENDİLİĞİNDEN gizlenecekler + atlananlar.
            yaklasan = new { gun = OncedenHaberGun, kimlikTespiti = yaklasan.TespitIdleri.Count },
            atlanan = new { sonIslem = dolmus.SonIslemNedeniyleAtlanan, bekletme = dolmus.BekletmeNedeniyleAtlanan },
            enYakinSaklamaBitisi = enEski,
            aciklama = tespit + beyan == 0
                ? "Süresi dolmuş kişisel veri yok."
                : $"{tespit + beyan} kayıtta kimlik alanları maskelenecek; tutar, tarih ve kademe korunacak.",
        };
    }

    /// <summary>
    /// Maskeler. <paramref name="otomatik"/> = SAKLAMA_UYGULA işi (kullanıcısız; denetim
    /// izine "otomatik" etiketiyle, kullanıcı alanı boş). Elle uçta SAHİP onayıyla.
    /// </summary>
    public async Task<object> Uygula(Kuyumcu.Api.Auth.AktifKullanici islem, bool otomatik = false)
    {
        var simdi = DateTime.UtcNow;
        var aday = await AdaylariBul(islem.TenantId, null, simdi);

        var tespitler = aday.TespitIdleri.Count == 0 ? [] : await db.MasakKimlikTespitleri
            .Where(m => m.TenantId == islem.TenantId && aday.TespitIdleri.Contains(m.Id))
            .Take(AzamiParti).ToListAsync();

        foreach (var m in tespitler)
        {
            m.AdSoyad = Maskelenmis;
            m.BelgeNo = Maskelenmis;     // şifreli sütun; maskelenmiş değer yazılır
            m.Adres = null;
            m.DogumTarihi = null;
            m.Uyruk = null;
        }

        var beyanlar = aday.BeyanIdleri.Count == 0 ? [] : await db.MasakBeyanlari
            .Where(b => b.TenantId == islem.TenantId && aday.BeyanIdleri.Contains(b.Id))
            .Take(AzamiParti).ToListAsync();

        foreach (var b in beyanlar)
        {
            b.IslemAmaci = Maskelenmis;
            b.Aciklama = Maskelenmis;
            b.FonKaynagi = null;
            b.EkBelgeReferansi = null;
        }

        var ozet = $"saklama süresi maskelemesi{(otomatik ? " (otomatik)" : "")}: {tespitler.Count} tespit, {beyanlar.Count} beyan"
                   + $" · son işlem nedeniyle atlanan {aday.SonIslemNedeniyleAtlanan} · bekletmede {aday.BekletmeNedeniyleAtlanan}";
        if (otomatik)
        {
            if (tespitler.Count + beyanlar.Count > 0)
            {
                denetim.SistemYazmaKaydet(islem.TenantId, nameof(MasakKimlikTespiti), null, ozet);
                await db.SaveChangesAsync();
            }
        }
        else
        {
            if (tespitler.Count + beyanlar.Count > 0)
                await db.SaveChangesAsync();
            await denetim.OkumaKaydet(islem, nameof(MasakKimlikTespiti), null, ozet);
        }

        return new
        {
            maskelenen = new { kimlikTespiti = tespitler.Count, beyan = beyanlar.Count },
            atlanan = new { sonIslem = aday.SonIslemNedeniyleAtlanan, bekletme = aday.BekletmeNedeniyleAtlanan },
            tarih = simdi,
        };
    }
}
