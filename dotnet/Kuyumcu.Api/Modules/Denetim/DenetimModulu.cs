using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Denetim;

// ═════════════════════════════════════════════════════════════════════════════
// DENETİM İZİ OKUMA — PLAN-2 Adım 0d (Mehmet'in koşulu #2).
//
// Tespit (2026-09-13): denetim izi 2026-09-11'den beri TUTULUYORDU ama onu
// OKUYAN hiçbir uç yoktu. Yani kuyumcu (veri sorumlusu) bir KVKK md.11 talebine
// — "benim verime kim, ne zaman baktı?" — cevap veremiyordu. Kayıt tutmak tek
// başına yükümlülüğü karşılamaz; ibraz edilebilir olması gerekir.
//
// KURALLAR:
//   • YALNIZ SAHİP. Denetim izi, personelin kendi izini görmesi için değil,
//     işletme sahibinin personeli denetlemesi içindir.
//   • SİLME/GÜNCELLEME UCU YOK — ve artık altyapı da engelliyor
//     (DenetimYazmaDinleyicisi → DenetimIziDegistirilemez).
//   • Denetim izini okumak da iz bırakır. Özyineleme yok: okuma bir YAZMA
//     üretir, o yazma yeni bir okuma üretmez.
//   • Kayıtta zaten DEĞER yok, yalnız alan adı var (KVKK veri asgariliği) —
//     bu uç kişisel veriyi ikinci kez çoğaltmaz.
// ═════════════════════════════════════════════════════════════════════════════

// ═════════════════════════════════════════════════════════════════════════════
// MUTABAKAT-SUZGEC — PAKET P-24 (B-7 · B-62).
//
// ÖNCEDEN: Listele() süzgeç zincirini yazıyordu, Ozet() AYNI zinciri İKİNCİ KEZ
// yazıyordu — ama yalnız tarih kısmını. Ekranda üstteki "Toplam kayıt" kartı
// TÜM kayıtları, altındaki liste ise SÜZÜLMÜŞ kayıtları sayıyordu; kullanıcı
// "eylem = SİLME" süzdüğünde kart 312, liste 7 gösteriyordu. İki sayı da
// doğruydu, ama aynı soruya iki cevaptı (UX-S22/UX-S31 ihlali).
//
// ARTIK: süzgeç TEK yerde. Liste de özet de bu nesneden türer; birine eklenen
// süzgeç ötekine de düşer. denetim.sh P-24 bloğu `OZET_AYRI_SORGU = 0` ile
// ikinci bir zincirin açılmasını engeller.
// ═════════════════════════════════════════════════════════════════════════════
internal sealed record DenetimSuzgeci(
    string TenantId,
    DateTime? Baslangic = null, DateTime? Bitis = null,
    string? Eylem = null, string? VarlikTuru = null, string? VarlikId = null,
    bool? Hassas = null, string? KullaniciId = null,
    string? SubeId = null, string? KasaId = null,
    // ZO-BELGE (tasarım hakemi t3): BASİT yüz "kim ne yaptı"yı İNSAN eylemleriyle
    // sınırlar. İsteğe bağlı, varsayılan KAPALI → mevcut çağrılar DEĞİŞMEZ.
    bool SistemHaric = false)
{
    /// <summary>İnsan eylemi olmayan (arka plan/ölçüm/oturum/kişisel görünüm) kayıt türleri.</summary>
    internal static readonly string[] SistemVarliklari =
    [
        "TelemetriOlcumu", "KullaniciTercihi", "KullaniciGorunum", "PanoKalemAyari",
        "RefreshToken", "KurSnapshot", "IsCalismasi",
    ];

    public IQueryable<DenetimKaydi> Uygula(KuyumcuDbContext db)
    {
        var sorgu = db.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == TenantId);

        if (Baslangic is not null) sorgu = sorgu.Where(d => d.Tarih >= Baslangic);
        // B-62 (off-by-one): GÜN olarak gelen bitiş, o günün SONUNU kapsar.
        var ust = Rapor.Mutabakat.UstSinirKapsayici(Bitis);
        if (ust is not null) sorgu = sorgu.Where(d => d.Tarih < ust);

        if (!string.IsNullOrWhiteSpace(Eylem)) sorgu = sorgu.Where(d => d.Eylem == Eylem);
        if (!string.IsNullOrWhiteSpace(VarlikTuru)) sorgu = sorgu.Where(d => d.VarlikTuru == VarlikTuru);
        if (!string.IsNullOrWhiteSpace(VarlikId)) sorgu = sorgu.Where(d => d.VarlikId == VarlikId);
        if (Hassas is not null) sorgu = sorgu.Where(d => d.Hassas == Hassas);
        if (!string.IsNullOrWhiteSpace(KullaniciId)) sorgu = sorgu.Where(d => d.KullaniciId == KullaniciId);
        // Bağlam damgası süzgeçleri (paket 48 Faz A) — damgasız (NULL) satırlar
        // hiçbir şube filtresinde GÖRÜNMEZ; "bilinmiyor" bir şubeye atanmaz.
        // ⚠ Yerel değişken ZORUNLU: satır metni P-04 sınıflandırma tablosunda
        //   (sube-suzgec-siniflandirma.txt:25) BİREBİR mühürlüdür; büyük harfli
        //   özellik adına dönerse GUVENLIK_ELLE_IZOLASYON sayacı kırılır.
        var subeId = SubeId;
        var kasaId = KasaId;
        if (!string.IsNullOrWhiteSpace(subeId)) sorgu = sorgu.Where(d => d.SubeId == subeId);
        if (!string.IsNullOrWhiteSpace(kasaId)) sorgu = sorgu.Where(d => d.KasaId == kasaId);

        // Sistem kaydı = kullanıcısız satır ya da sistem türü (yalnız istenirse).
        if (SistemHaric)
            sorgu = sorgu.Where(d => d.KullaniciId != null && !SistemVarliklari.Contains(d.VarlikTuru));

        return sorgu;
    }
}

public sealed class DenetimOkumaServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>
    /// UX-4d madde 23: TERS TARİH ARALIĞI SESSİZCE KABUL EDİLİYORDU.
    /// bitiş &lt; başlangıç verildiğinde sorgu hiçbir satır döndürmüyor, ekran
    /// "kayıt yok" ve özet 0 gösteriyordu — kullanıcı denetim izinin gerçekten
    /// boş olduğunu sanıyordu (Anayasa md.1: hata "kayıt yok"a çevrilemez,
    /// UX-S28). Kâr-zarar raporundaki kural (KarZararServisi.Donem) burada da
    /// uygulanır: 400 + Türkçe mesaj.
    /// <para>
    /// PAKET GOC-D (envanter E · "rapor tarih aralıkları 400 + bas ≤ bit", "süzgeç beyaz listeleri"):
    /// kural Core RaporDonemKurali'dadır (ekranın alan altı metniyle AYNI sözlük anahtarı — eskiden
    /// sunucu ve ekran ayrı çıplak cümle yazıyordu). Pencere: 2000..gelecek yıl sonu (3000 yılı
    /// sessiz boş liste DEĞİL, 400); denetim izi sayfalı olduğundan gün sınırı yerine K8e 5 yıl.
    /// Eylem beş sabitten biri, varlık türü tür adı biçiminde olmalı (K32).
    /// </para>
    /// </summary>
    internal static void AralikDogrula(DateTime? baslangic, DateTime? bitis, string? eylem = null, string? varlikTuru = null)
    {
        DateOnly? bas = baslangic is { } b ? DateOnly.FromDateTime(b) : null;
        DateOnly? bit = bitis is { } s ? DateOnly.FromDateTime(s) : null;
        if (Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.Denetle(bas, bit, Zaman.YerelBugun(), Kuyumcu.Core.I18n.IstekDili.Gecerli, enCokGun: 0) is { } h)
            throw new BadRequestException(h.Mesaj).AlanIle(h.Alan);
        if (!Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.ListedeMi(eylem, Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.DenetimEylemleri))
            throw new BadRequestException(Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.Mesaj(Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("eylem");
        if (!string.IsNullOrEmpty(varlikTuru) && !Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.VarlikTuruGecerli(varlikTuru))
            throw new BadRequestException(Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.Mesaj(Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("varlikTuru");
    }

    public async Task<object> Listele(
        AktifKullanici islem,
        DateTime? baslangic, DateTime? bitis,
        string? eylem, string? varlikTuru, string? varlikId,
        bool? hassas, string? kullaniciId,
        int limit, int offset,
        // Bağlam damgası süzgeçleri SONA eklendi ki mevcut konumsal çağrılar
        // (testler dâhil) DEĞİŞMESİN — UX-S4 ruhu: mevcut sözleşme kırılmaz.
        string? subeId = null, string? kasaId = null,
        bool sistemHaric = false)
    {
        limit = Math.Clamp(limit, 1, 500);
        offset = Math.Max(offset, 0);
        AralikDogrula(baslangic, bitis, eylem, varlikTuru);

        // TEK SÜZGEÇ (P-24): özet ucu da bu nesneyi kurar.
        var sorgu = new DenetimSuzgeci(islem.TenantId, baslangic, bitis, eylem, varlikTuru,
            varlikId, hassas, kullaniciId, subeId, kasaId, sistemHaric).Uygula(db);

        var toplam = await sorgu.CountAsync();
        var liste = await sorgu
            .OrderByDescending(d => d.Tarih)
            .Skip(offset).Take(limit)
            .ToListAsync();

        // Kullanıcı adlarını tek sorguda çöz — izde yalnız kimlik tutuluyor.
        // Şube/kasa adları — tek sorguda (kullanıcı adları deseniyle aynı).
        var subeIdler = liste.Where(d => d.SubeId is not null).Select(d => d.SubeId!).Distinct().ToList();
        var subeAdlari = subeIdler.Count == 0
            ? []
            : await db.Subeler.AsNoTracking()
                .Where(x => x.TenantId == islem.TenantId && subeIdler.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad);
        var kasaIdler = liste.Where(d => d.KasaId is not null).Select(d => d.KasaId!).Distinct().ToList();
        var kasaAdlari = kasaIdler.Count == 0
            ? []
            : await db.Kasalar.AsNoTracking()
                .Where(x => x.TenantId == islem.TenantId && kasaIdler.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad);

        var kullaniciIdler = liste.Where(d => d.KullaniciId is not null)
            .Select(d => d.KullaniciId!).Distinct().ToList();
        var adlar = kullaniciIdler.Count == 0
            ? []
            : await db.Kullanicilar.AsNoTracking()
                .Where(k => k.TenantId == islem.TenantId && kullaniciIdler.Contains(k.Id))
                .Select(k => new { k.Id, k.Ad, k.Soyad, k.Rol })
                .ToDictionaryAsync(k => k.Id);

        await denetim.ListeOkumaKaydet(islem, nameof(DenetimKaydi), liste.Count, "denetim izi görüntülendi");

        return new
        {
            toplam,
            limit,
            offset,
            kayitlar = liste.Select(d =>
            {
                adlar.TryGetValue(d.KullaniciId ?? "", out var k);
                // ⚠ Alan adları AÇIKÇA küçük harfle yazılır. Kısayol (`d.Eylem`)
                // kullanılsaydı doğrudan serileştirmede "Eylem", HTTP yolunda
                // "eylem" olurdu — aynı nesnenin iki farklı sözleşmesi. Bu tuzak
                // bu projede daha önce teste patladı (2026-09-12 ve yine bugün).
                return new
                {
                    id = d.Id,
                    tarih = d.Tarih,
                    eylem = d.Eylem,
                    varlikTuru = d.VarlikTuru,
                    varlikId = d.VarlikId,
                    hassas = d.Hassas,
                    detay = d.Detay,
                    ipAdresi = d.IpAdresi,
                    subeId = d.SubeId,
                    subeAd = d.SubeId is not null && subeAdlari.TryGetValue(d.SubeId, out var sa) ? sa : null,
                    kasaId = d.KasaId,
                    kasaAd = d.KasaId is not null && kasaAdlari.TryGetValue(d.KasaId, out var ka) ? ka : null,
                    kullaniciId = d.KullaniciId,
                    kullanici = k is null
                        ? (d.KullaniciId is null ? "sistem" : "(silinmiş kullanıcı)")
                        : $"{k.Ad} {k.Soyad}",
                    rol = k?.Rol,
                };
            }),
        };
    }

    /// <summary>
    /// Ekranın filtre kutularını doldurmak için — sabit liste değil, gerçek veriden.
    /// <para>
    /// ⚠ P-24 (B-7): süzgeç parametreleri <see cref="Listele"/> ile AYNIDIR ve
    /// aynı <c>DenetimSuzgeci</c>'nden geçer. Eksik bırakılırsa üstteki "Toplam
    /// kayıt" kartı ile alttaki listenin toplamı ayrışır; iki sayı da doğru
    /// görünür, hangisinin doğru olduğu bilinmez (Anayasa md.1).
    /// </para>
    /// <para>
    /// Yeni parametreler SONA ve varsayılanlı eklendi ki mevcut konumsal
    /// çağrılar (testler dâhil) kırılmasın — UX-S4 ruhu.
    /// </para>
    /// </summary>
    public async Task<object> Ozet(
        AktifKullanici islem, DateTime? baslangic, DateTime? bitis,
        string? eylem = null, string? varlikTuru = null, string? varlikId = null,
        bool? hassas = null, string? kullaniciId = null,
        string? subeId = null, string? kasaId = null,
        bool sistemHaric = false)
    {
        AralikDogrula(baslangic, bitis, eylem, varlikTuru);

        // TEK SÜZGEÇ (P-24) — Listele ile birebir aynı nesne.
        var sorgu = new DenetimSuzgeci(islem.TenantId, baslangic, bitis, eylem, varlikTuru,
            varlikId, hassas, kullaniciId, subeId, kasaId, sistemHaric).Uygula(db);

        var eylemler = await sorgu.GroupBy(d => d.Eylem)
            .Select(g => new { eylem = g.Key, adet = g.Count() })
            .OrderByDescending(x => x.adet).ToListAsync();

        var turler = await sorgu.GroupBy(d => d.VarlikTuru)
            .Select(g => new { varlikTuru = g.Key, adet = g.Count() })
            .OrderByDescending(x => x.adet).ToListAsync();

        // Şube kırılımı — damgasız satırlar ayrı kalemde ("(damgasız)" değil,
        // subeId = null olarak) döner; uydurma şube atanmaz.
        var subeler = await sorgu.GroupBy(d => d.SubeId)
            .Select(g => new { subeId = g.Key, adet = g.Count() })
            .OrderByDescending(x => x.adet).ToListAsync();
        var subeIdListesi = subeler.Where(x => x.subeId is not null).Select(x => x.subeId!).ToList();
        var subeAdHaritasi = subeIdListesi.Count == 0
            ? []
            : await db.Subeler.AsNoTracking()
                .Where(x => x.TenantId == islem.TenantId && subeIdListesi.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad);

        var hassasAdet = await sorgu.CountAsync(d => d.Hassas);
        var enEski = await sorgu.OrderBy(d => d.Tarih)
            .Select(d => (DateTime?)d.Tarih).FirstOrDefaultAsync();

        var toplam = await sorgu.CountAsync();

        // ⚠ MUTABAKAT KANITI ÇALIŞMA ZAMANINDA DA ARANIR (P-24): kırılımların
        //   toplamı ile üst karttaki "Toplam kayıt" AYNI olmak zorundadır.
        //   Ayrışırsa sessizce yuvarlanmaz — ekrana hata bandı çıkar ve kusur
        //   görünür olur (Anayasa md.1: bilinmeyen "0" sayılmaz).
        Rapor.Mutabakat.Denk("denetim.ozet.eylemler", toplam, eylemler.Sum(x => x.adet));
        Rapor.Mutabakat.Denk("denetim.ozet.turler", toplam, turler.Sum(x => x.adet));
        Rapor.Mutabakat.Denk("denetim.ozet.subeler", toplam, subeler.Sum(x => x.adet));

        return new
        {
            toplam,
            hassasAdet,
            enEskiKayit = enEski,
            eylemler,
            turler,
            subeler = subeler.Select(x => new
            {
                x.subeId,
                subeAd = x.subeId is not null && subeAdHaritasi.TryGetValue(x.subeId, out var ad) ? ad : null,
                x.adet,
            }),
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/denetim")]
[RolGerekli("SAHIP")]   // sınıf düzeyinde: hiçbir uç SAHİP dışına açılmaz
[Yetki.ModulYetkisi("denetim-izi")]
public sealed class DenetimController(DenetimOkumaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/denetim — denetim izi kayıtları (filtreli) — YALNIZ SAHİP</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] string? eylem, [FromQuery] string? varlikTuru, [FromQuery] string? varlikId,
        [FromQuery] bool? hassas, [FromQuery] string? kullaniciId,
        [FromQuery] string? subeId, [FromQuery] string? kasaId,
        [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery] bool sistemHaric = false)
        => servis.Listele(Islem, baslangic, bitis, eylem, varlikTuru, varlikId,
                          hassas, kullaniciId, limit, offset, subeId, kasaId, sistemHaric);

    /// <summary>
    /// GET /api/v1/denetim/ozet — eylem ve varlık kırılımı — YALNIZ SAHİP.
    /// <para>P-24: süzgeç kümesi <see cref="Listele"/> ile AYNIDIR (B-7).</para>
    /// </summary>
    [HttpGet("ozet")]
    public Task<object> Ozet(
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis,
        [FromQuery] string? eylem, [FromQuery] string? varlikTuru, [FromQuery] string? varlikId,
        [FromQuery] bool? hassas, [FromQuery] string? kullaniciId,
        [FromQuery] string? subeId, [FromQuery] string? kasaId,
        [FromQuery] bool sistemHaric = false)
        => servis.Ozet(Islem, baslangic, bitis, eylem, varlikTuru, varlikId,
                       hassas, kullaniciId, subeId, kasaId, sistemHaric);

    // SİLME / GÜNCELLEME UCU BİLİNÇLİ OLARAK YOKTUR. Eklenirse denetim izi
    // anlamını yitirir; altyapı da (DenetimYazmaDinleyicisi) zaten reddeder.
}
