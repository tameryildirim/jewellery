using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// Gider pusulası servisi ve uçları — ADIM-6-7-8 raporu §6/2 ve §6/3'teki
// iki açığı kapatır:
//   • yazdırılabilir belge çıktısı (yalnız veri vardı)
//   • 7 günlük düzenleme süresinin TAKİBİ (tarih hesaplanıp saklanıyordu ama
//     süresi dolanı kimse uyarmıyordu)
//
// GÜVENLİK: pusula satıcı TCKN'si taşır → MASAK verisiyle AYNI rejim.
//   • Liste MASKELİ (son 4 hane), tam değer yalnız tekil GET'te
//   • Her okuma DenetimServisi ile loglanır (KVKK md.12)
//   • TEZGAHTAR erişemez
// Silme ucu YOK — belge append-only.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HurdaServisi(
    KuyumcuDbContext db, DenetimServisi denetim, HurdaAyarlari ayarlar, YasalParametreServisi? yasal = null)
{
    private const string Varlik = nameof(GiderPusulasi);

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET YASAL-4 / Y4-A — 7 GÜN KURALININ ANLAMI (VUK md.234, § 3.1 M-2/M-3).
    //
    // Pusula hurda alışıyla AYNI commit'te düzenlenir (GiderPusulasiKapisi FAZ 2);
    // yani iptal edilmemiş her pusula süresinde düzenlenmiştir. Eski kod
    // `DuzenlemeSonTarihi < şimdi` olan HER pusulayı "süresi DOLMUŞ" sayıyordu:
    // 7 günden eski bütün belgeler Panel'de KRİTİK yanlış alarm veriyordu.
    // Süre takibine YALNIZ şunlar girer: İPTAL EDİLMİŞ ve aynı alış (hareket) için
    // yerine geçerli pusula DÜZENLENMEMİŞ belge. Son gün = ORİJİNAL teslim günü +
    // GIDER_PUSULASI_GUN (teslim gününün değeri; YASAL-1 tablosu, yoksa yapılandırma).
    // ═════════════════════════════════════════════════════════════════════════
    private DateOnly SonGun(DateTime teslimUtc) => GiderPusulasiSuresi.SonGun(ayarlar, yasal, teslimUtc);

    /// <summary>Verilen hareketlerden İPTAL EDİLMEMİŞ (geçerli) pusulası olanlar.</summary>
    private async Task<HashSet<string>> GecerliPusulaliHareketler(string tenantId, IReadOnlyCollection<string> hareketIdler)
    {
        if (hareketIdler.Count == 0) return [];
        var liste = await db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == tenantId && !g.IptalEdildi && hareketIdler.Contains(g.HareketId))
            .Select(g => g.HareketId)
            .Take(hareketIdler.Count * 2)   // hareket başına en çok bir geçerli pusula (+ pay)
            .ToListAsync();
        return liste.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Hareket başına EN SON iptal edilen pusula — yeniden düzenlenecek belge odur
    /// (aynı alışın iki iptal kaydı iki ayrı "bekleyen" sayılmaz).
    /// </summary>
    private static HashSet<string> SonIptaller(IEnumerable<GiderPusulasi> iptaller) =>
        iptaller.GroupBy(g => g.HareketId, StringComparer.Ordinal)
            .Select(gr => gr.OrderByDescending(x => x.Olusturuldu).ThenByDescending(x => x.Id, StringComparer.Ordinal).First().Id)
            .ToHashSet(StringComparer.Ordinal);

    public async Task<object> Listele(
        AktifKullanici islem, DateTime? baslangic, DateTime? bitis, int limit)
    {
        limit = Math.Clamp(limit, 1, 500);
        var sorgu = db.GiderPusulalari.AsNoTracking().Where(g => g.TenantId == islem.TenantId);
        if (baslangic is not null) sorgu = sorgu.Where(g => g.TeslimTarihi >= baslangic);
        // PAKET GOC-A (envanter § A acil hata — "bitiş günündeki pusulalar listede görünmüyor"): ekran bitiş
        // GÜNÜNÜ gece yarısı gönderir; "<" o günün tamamını dışarıda bırakıyordu. Saat bileşeni yoksa bitiş
        // günün SONUNA taşınır (Kur geçmişi UX-4d md.9 ile aynı kural). Ters aralık 400 (K8e).
        GirdiKapisi.Aralik(baslangic is { } b0 ? DateOnly.FromDateTime(b0) : null,
                           bitis is { } b1 ? DateOnly.FromDateTime(b1) : null);
        if (bitis is { } bitisAn)
        {
            var ustSinir = bitisAn.TimeOfDay == TimeSpan.Zero ? bitisAn.Date.AddDays(1) : bitisAn;
            sorgu = sorgu.Where(g => g.TeslimTarihi < ustSinir);
        }

        var liste = await sorgu.OrderByDescending(g => g.TeslimTarihi).Take(limit).ToListAsync();
        await denetim.ListeOkumaKaydet(islem, Varlik, liste.Count, "gider pusulası listesi");

        // Y4-A: gecikti/kalanGun YALNIZ iptal edilmiş + yerine geçerli pusulası
        // olmayan belgede anlamlıdır; diğerlerinde null döner (süre karşılanmıştır).
        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var iptaller = liste.Where(g => g.IptalEdildi).ToList();
        var gecerli = await GecerliPusulaliHareketler(islem.TenantId, iptaller.Select(g => g.HareketId).Distinct().ToList());
        var sonIptaller = SonIptaller(iptaller.Where(g => !gecerli.Contains(g.HareketId)));
        return liste.Select(g =>
        {
            var bekliyor = sonIptaller.Contains(g.Id);
            var sonGun = SonGun(g.TeslimTarihi);
            return new
            {
                g.Id, g.PusulaNo, g.HareketId, g.SaticiTipi, g.SaticiAdSoyad,
                saticiTckn = Masak.MasakServisi.Maskele(g.SaticiTckn),   // liste: MASKELİ
                g.AyarId, g.GramAgirlik, g.HasGrami,
                g.BrutTutar, g.StopajOran, g.StopajTutar, g.NetOdenen,
                g.TeslimTarihi, g.DuzenlemeSonTarihi,
                g.IptalEdildi, g.IptalGerekce, g.IptalTarihi, g.DuzeltilenId,
                gecikti = bekliyor ? bugun > sonGun : (bool?)null,
                kalanGun = bekliyor ? sonGun.DayNumber - bugun.DayNumber : (int?)null,
                sonGun = bekliyor ? sonGun : (DateOnly?)null,
                yenidenDuzenlenebilir = bekliyor && bugun <= sonGun,
            };
        });
    }

    /// <summary>Tekil pusula — tam TCKN. Okuma izi ZORUNLU.</summary>
    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var g = await Bul(id, islem.TenantId);
        await denetim.OkumaKaydet(islem, Varlik, g.Id, "gider pusulası görüntülendi");
        return Yanit(g);
    }

    /// <summary>
    /// Belgenin GERÇEKTEN basıldığını denetim izine yazar (UX-4d madde 20).
    /// Görüntüleme izinden ayrıdır; ekran yazdırma diyaloğunu açtığında çağırır.
    /// </summary>
    public async Task<object> BelgeYazdirildi(string id, AktifKullanici islem)
    {
        var g = await Bul(id, islem.TenantId);
        await denetim.OkumaKaydet(islem, Varlik, g.Id, "gider pusulası belgesi YAZDIRILDI (basım)");
        return new { g.Id, g.PusulaNo, yazdirildi = true, an = DateTime.UtcNow };
    }

    /// <summary>
    /// Yazdırılabilir belge — VUK md.234'ün istediği alanlar, kuyumcunun
    /// (düzenleyen) ve satıcının bilgileriyle birlikte. Okuma izi düşer.
    /// </summary>
    public async Task<object> Belge(string id, AktifKullanici islem)
    {
        var g = await Bul(id, islem.TenantId);
        // ⚠ UX-4d madde 20: GÖRÜNTÜLEME ile YAZDIRMA aynı olay değildir.
        // Denetim izine yalnız belgeyi EKRANDA AÇAN kullanıcı için "yazdırıldı"
        // yazılıyordu; bir VUK incelemesinde "bu pusula kaç kez basıldı"
        // sorusunun cevabı yanlış olurdu (Anayasa md.1: kanıtlanmayan yazılmaz).
        // Görüntüleme burada, yazdırma POST {id}/yazdirildi ucunda kaydedilir.
        await denetim.OkumaKaydet(islem, Varlik, g.Id, "gider pusulası belgesi görüntülendi");

        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == islem.TenantId);
        var sube = await db.Subeler.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == g.SubeId && s.TenantId == islem.TenantId);
        var ayar = Kuyumcu.Core.Domain.Ayarlar.Bul(g.AyarId);

        return new
        {
            belgeTuru = "GİDER PUSULASI",
            dayanak = "213 sayılı VUK md.234",
            g.PusulaNo,
            duzenlemeTarihi = g.Olusturuldu,
            g.TeslimTarihi,
            g.DuzenlemeSonTarihi,
            // Y4-A: belge düzenlendiği gün son günden SONRA mı? (409 kapısı bunu
            // engellediği için yeni belgede hep false; eski kayıtta dürüst ölçü.)
            gecikti = YasalParametreServisi.IslemGunu(g.Olusturuldu) > SonGun(g.TeslimTarihi),
            // VUK md.234 düzeltme yolu: iptal edilen belge SİLİNMEZ, "İPTAL"
            // ibaresiyle saklanır ve yerine yeni pusula düzenlenir.
            g.IptalEdildi, g.IptalGerekce, g.IptalTarihi,

            // UX-1-B devri: işletme kimliği (adres, vergi dairesi/no, telefon)
            // artık tenants satırında tutuluyor. VUK md.234 belgenin DÜZENLEYEN
            // tarafını bu bilgilerle ister; boş olanlar UYDURULMAZ, null döner ve
            // ekran "işletme kartından doldurun" ipucunu gösterir (Anayasa md.1).
            duzenleyen = new
            {
                unvan = tenant?.Ad,
                sube = sube?.Ad,
                adres = tenant?.Adres,
                vergiDairesi = tenant?.VergiDairesi,
                vergiNo = tenant?.VergiNo,
                telefon = tenant?.Telefon,
            },

            satici = new
            {
                g.SaticiTipi,
                saticiTipiAciklama = g.SaticiTipi switch
                {
                    SaticiTipi.NihaiTuketici => "Vergi mükellefi olmayan (zati eşya satışı)",
                    SaticiTipi.EsnafMuaf => "Esnaf muaflığından yararlanan",
                    _ => g.SaticiTipi,
                },
                adSoyad = g.SaticiAdSoyad,
                tckn = g.SaticiTckn,
                adres = g.SaticiAdres,
            },

            mal = new
            {
                // PAKET 20: belgede "hurda altın" yazıyordu — gümüş hurdada bu
                // BELGEYE YANLIŞ CİNS yazmak demekti (VUK md.234 malın cinsini
                // ister). Cins artık ayarın MADENİNDEN gelir.
                // KESIF-P3 (#66): belgede ham ayar KODU (K22) basılıyordu → ayarın ADI
                // ("Hurda altın (22 Ayar — 916/1000)"). Maden adı ve milyem KORUNUR (VUK
                // md.234 malın cinsi). Bilinmeyen kod uydurulmaz: kodun kendisi kalır.
                cins = $"Hurda {Kuyumcu.Core.Domain.Maden.Ad(ayar?.Maden ?? Kuyumcu.Core.Domain.Maden.Altin).ToLowerInvariant()} " +
                       $"({ayar?.Ad ?? g.AyarId}{(ayar is null ? "" : $" — {ayar.Milyem}/1000")})",
                maden = ayar?.Maden ?? Kuyumcu.Core.Domain.Maden.Altin,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(ayar?.Maden ?? Kuyumcu.Core.Domain.Maden.Altin),
                g.GramAgirlik,
                g.HasGrami,
                aciklama = g.MalAciklamasi,
            },

            tutarlar = new
            {
                brutTutar = g.BrutTutar,
                stopajOran = g.StopajOran,
                stopajOranYuzde = $"%{g.StopajOran * 100m:0.##}",
                stopajTutar = g.StopajTutar,
                netOdenen = g.NetOdenen,
                yaziyla = TutarYazi.Yaz(g.NetOdenen),
            },

            // Madde 1: belgenin kapsamadığı şey açıkça yazılır.
            // Y4-B: kesilen stopaj artık dönem borcu olarak izleniyor (Gider pusulaları → Stopaj).
            not = g.StopajTutar > 0m
                ? "Kesilen stopaj vergi dairesine aittir; dönem borcu Gider pusulaları → Stopaj bölümünde izlenir."
                : "Vergi mükellefi olmayan kişiden alımda gelir vergisi tevkifatı yapılmamıştır.",
        };
    }

    /// <summary>
    /// Y4-A — 7 günlük düzenleme süresi takibi. YALNIZ iptal edilmiş ve yerine
    /// geçerli pusula düzenlenmemiş belgeler: son günü gelmemişler "yaklaşan"
    /// (yeniden düzenlenmeli), son günü geçmişler "gecikmiş" (VUK md.234: süresinde
    /// düzenlenmeyen pusula hiç düzenlenmemiş sayılır → BELGESİZ ALIŞ).
    /// Kişisel veri döndürmez — yalnız pusula no, tarih ve tutar.
    /// <para><paramref name="uyariGunu"/> geriye uyum için alınır; geri sayım her
    /// bekleyen belgede gösterilir (bekleyen belge zaten istisnadır).</para>
    /// </summary>
    public async Task<object> SureTakibi(AktifKullanici islem, int uyariGunu = 2)
    {
        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var iptaller = await db.GiderPusulalari.AsNoTracking()
            .Where(g => g.TenantId == islem.TenantId && g.IptalEdildi)
            .OrderByDescending(g => g.TeslimTarihi)
            .Take(SureTakibiSiniri)
            .ToListAsync();
        var gecerli = await GecerliPusulaliHareketler(islem.TenantId, iptaller.Select(g => g.HareketId).Distinct().ToList());
        var sonIptaller = SonIptaller(iptaller.Where(g => !gecerli.Contains(g.HareketId)));

        var bekleyen = iptaller.Where(g => sonIptaller.Contains(g.Id))
            .Select(g =>
            {
                var sonGun = SonGun(g.TeslimTarihi);
                return new SureKalemi(g.Id, g.PusulaNo, g.HareketId, g.TeslimTarihi, sonGun,
                    sonGun.DayNumber - bugun.DayNumber, g.BrutTutar, g.StopajTutar);
            })
            .OrderBy(x => x.SonGun)
            .ToList();

        var iptalSozlugu = iptaller.ToLookup(g => g.HareketId, StringComparer.Ordinal);
        var gecikmis = bekleyen.Where(x => GiderPusulasiSuresi.Durum(iptalSozlugu[x.HareketId], ayarlar, yasal, bugun)
            == PusulaBelgeDurumu.Belgesiz).ToList();
        var yaklasan = bekleyen.Except(gecikmis).ToList();

        // § 4.1 madde 2 — MÜKELLEF satıcıdan alış: pusula DÜZENLENMEZ, belge satıcının
        // faturasıdır. Tedarikçi fatura no'su BOŞ ve pusulası da olmayan (stornolanmamış)
        // hurda alışı = "satıcı faturası bekleniyor". Fatura düzenleme süresi TEYİTSİZ
        // (şartname § 3.2) → yalnız BİLGİ; geri sayım/son gün YOK (tahminle süre yazılmaz).
        var faturaBekleyen = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tur == "HURDA_ALIS"
                     && (h.FaturaNo == null || h.FaturaNo == "")   // TEK yer: tedarikçi faturası yok (P-08 sayacının adlı satırı)
                     && !db.GiderPusulalari.Any(g => g.TenantId == islem.TenantId && g.HareketId == h.Id)
                     && !db.Hareketler.Any(s => s.TenantId == islem.TenantId && s.StornoKaynakId == h.Id))
            .OrderByDescending(h => h.Tarih)
            .Select(h => new FaturaBekleyenKalem(h.Id, h.Tarih, -h.TlTutari))
            .Take(SureTakibiSiniri)
            .ToListAsync();
        var gun = GiderPusulasiSuresi.Gun(ayarlar, yasal, bugun);

        return new
        {
            sure = new { gun, dayanak = "VUK md.234" },
            gecikmisSayisi = gecikmis.Count,
            yaklasanSayisi = yaklasan.Count,
            gecikmis,
            yaklasan,
            // Yaklaşanların EN YAKIN son günü — Panel satırı "son gün 03.10" yazar.
            enYakinSonGun = yaklasan.Count > 0 ? yaklasan[0].SonGun : (DateOnly?)null,
            faturaBekleyenSayisi = faturaBekleyen.Count,
            faturaBekleyen,
            sinir = SureTakibiSiniri,
            sinirdaMi = iptaller.Count >= SureTakibiSiniri,
            uyarilar = new List<string>(),
        };
    }

    /// <summary>UX-S29: süre takibi en çok bu kadar iptal kaydını tarar (sınır yanıtta yazılır).</summary>
    public const int SureTakibiSiniri = 500;

    /// <summary>Satıcı faturası beklenen hurda alışı — geri sayımsız bilgi satırı (kişisel veri yok).</summary>
    public sealed record FaturaBekleyenKalem(string HareketId, DateTime Tarih, decimal TutarTl);

    /// <summary>Süre takibi satırı — kişisel veri taşımaz.</summary>
    public sealed record SureKalemi(
        string Id, string PusulaNo, string HareketId, DateTime TeslimTarihi, DateOnly SonGun,
        int KalanGun, decimal BrutTutar, decimal StopajTutar);

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET S-3b — GİDER PUSULASI İPTALİ (VUK md.234 düzeltme yolu).
    //
    // KURUL TUR 3 / ENGEL 1 + QA turu 3 (KRİTİK): "gider pusulası kaydı hiçbir
    // şekilde düzenlenemiyor; storno/ters kayıt yolu da yok — bu kayıt bugün
    // sistem içinden hiçbir şekilde düzeltilemiyor."
    //
    // VUK md.234 belgesi YERİNDE DÜZELTİLMEZ (belge append-only): yanlış
    // düzenlenen pusula İPTAL edilir ve doğru bilgilerle YENİ pusula düzenlenir.
    // Bu yüzden burada alan güncelleyen bir PATCH AÇILMADI; açılsaydı basılmış
    // bir vergi belgesinin içeriği geriye dönük değişir ve belge zinciri
    // savunulamaz hâle gelirdi.
    //
    // ⚠ KAPSAM — DÜRÜST SINIR (Anayasa md.1): iptal YALNIZ BELGEYİ hükümsüz
    // kılar. Belgeye konu HURDA_ALIS hareketinin stok ve maliyet partisi bacağı
    // DEĞİŞMEZ (StornoServisi.ErtelenenTurler: partisi tüketilmiş malı bugünkü
    // maliyetle geri koymak kâr-zararı bozar). Yani "satıcının adı yanlış
    // yazılmış" düzeltilir; "hiç almadığımız malı almış görünüyoruz"
    // düzeltilmez — ekran bunu DegismezIpucu ile SÖYLER ve mali müşavir notu
    // rapora yazılır.
    // ═════════════════════════════════════════════════════════════════════════
    // ═════════════════════════════════════════════════════════════════════════
    // PAKET S-3d / DP4 — MALİ KİMLİK DIŞI ALANLARDA YERİNDE DÜZELTME
    //
    // QA turu 4 KRİTİK: "gider pusulası kaydının HİÇBİR alanı düzenlenemiyor".
    // Kurucu şartı "her veri girişi düzenlenebilir" ile VUK md.234 arasındaki
    // sınır burada çizilir:
    //   • DÜZENLENEBİLİR (belgenin mali hüviyetini değiştirmez):
    //       satıcı adresi · mal açıklaması
    //   • DEĞİŞMEZ (belge hüviyeti / vergi matrahı / süre):
    //       satıcı tipi · ad soyad · TCKN · ayar · gram · has · brüt · stopaj ·
    //       net · teslim tarihi · düzenleme son tarihi
    //     → düzeltme yolu İPTAL + YENİ PUSULA (Iptal ucu), ekranda DegismezIpucu.
    //
    // ⚠ Teslim tarihi BİLEREK kilitli bırakıldı: VUK'un 7 günlük düzenleme
    //   süresini o tarih belirler, geriye alınması süreyi "uzatır". Mevzuatın
    //   böyle bir düzeltmeye izin verip vermediği DOĞRULANMADI → tutucu davranış
    //   (Anayasa md.1: bilinmeyen tahmin edilmez). Mali müşavir notu rapordadır.
    //
    // PATCH sözleşmesi: gövdede OLMAYAN alan KORUNUR, null olan SİLİNİR.
    // İptal edilmiş belge düzenlenmez (hükümsüz belgenin metni değiştirilemez).
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> Guncelle(string id, JsonElement govde, AktifKullanici islem)
    {
        if (govde.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("Geçersiz istek gövdesi");

        var g = await db.GiderPusulalari
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Gider pusulası bulunamadı");

        if (g.IptalEdildi)
            throw new ConflictException(
                "İptal edilmiş gider pusulası düzenlenemez. Aynı alış için 'Yeniden düzenle' "
                + "ile doğru bilgilerle yeni pusula düzenleyin; yeni hurda alışı GİRMEYİN.");

        var degisenler = new List<string>();

        // QA turu 5 (YÜKSEK): alan BOŞALTILIP kaydedilebiliyordu (PATCH 200).
        // Satıcının adresi VUK md.234'te gider pusulasının zorunlu unsurudur;
        // mal açıklaması da belgenin konusudur. Bu iki alan DÜZELTİLEBİLİR ama
        // SİLİNEMEZ: gönderilirse dolu gelmek zorundadır (PATCH sözleşmesi bozulmaz —
        // gövdede YOKSA hâlâ korunur, yalnız "null/boş = sil" dalı kapatılır).
        if (Alan(govde, "saticiAdres", out var adresOge))
        {
            var yeni = adresOge.ValueKind == JsonValueKind.Null ? null : Kirp(adresOge.GetString(), 500);
            if (string.IsNullOrWhiteSpace(yeni))
                throw new BadRequestException(ZorunluUnsurMesaji("Satıcı adresi"));
            if (yeni != g.SaticiAdres) degisenler.Add("satıcı adresi");
            g.SaticiAdres = yeni;
        }

        if (Alan(govde, "malAciklamasi", out var malOge))
        {
            var yeni = malOge.ValueKind == JsonValueKind.Null ? null : Kirp(malOge.GetString(), 500);
            if (string.IsNullOrWhiteSpace(yeni))
                throw new BadRequestException(ZorunluUnsurMesaji("Mal açıklaması"));
            if (yeni != g.MalAciklamasi) degisenler.Add("mal açıklaması");
            g.MalAciklamasi = yeni;
        }

        if (degisenler.Count == 0)
            throw new BadRequestException(
                "Düzenlenebilir alan gönderilmedi. Bu belgede yalnız satıcı adresi ve "
                + "mal açıklaması düzeltilebilir; diğer alanlar için İPTAL + YENİ PUSULA yolunu kullanın.");

        // ⚠ Detaya KİŞİSEL VERİ (ad, TCKN, adresin kendisi) YAZILMAZ — yalnız
        //   hangi alanın değiştiği. Kayıt hassas sayılır (satıcı verisi taşır).
        denetim.YazmaKaydet(islem, Varlik, g.Id,
            $"gider pusulası {g.PusulaNo} düzenlendi · değişen alan: {string.Join(", ", degisenler)}",
            hassas: true);

        await db.SaveChangesAsync();
        return Yanit(await Bul(g.Id, islem.TenantId));
    }

    /// <summary>
    /// Zorunlu unsur mesajı — belgenin hangi alanının neden boş bırakılamadığını
    /// ve doğru düzeltme yolunu TEK yerden söyler (UX-S30).
    /// </summary>
    private static string ZorunluUnsurMesaji(string alan) =>
        $"{alan} boş bırakılamaz: gider pusulasının zorunlu unsurudur (VUK md.234). "
        + "Doğru değeri yazın; belge tümüyle hatalıysa İPTAL + YENİ PUSULA yolunu kullanın.";

    private static bool Alan(JsonElement govde, string ad, out JsonElement oge)
    {
        foreach (var p in govde.EnumerateObject())
            if (string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase))
            {
                oge = p.Value;
                return true;
            }
        oge = default;
        return false;
    }

    private static string? Kirp(string? metin, int azami)
    {
        if (string.IsNullOrWhiteSpace(metin)) return null;
        var t = metin.Trim();
        return t.Length <= azami ? t : t[..azami];
    }

    public async Task<object> Iptal(string id, string? gerekceHam, AktifKullanici islem)
    {
        var gerekce = Modules.Satis.StornoServisi.GerekceDogrula(gerekceHam);

        var g = await db.GiderPusulalari
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Gider pusulası bulunamadı");

        if (g.IptalEdildi)
            throw new ConflictException("Bu gider pusulası zaten iptal edilmiştir.");

        g.IptalEdildi = true;
        g.IptalGerekce = gerekce;
        g.IptalTarihi = DateTime.UtcNow;

        // ⚠ Detaya KİŞİSEL VERİ (ad, TCKN, adres) yazılmaz — yalnız belge no.
        denetim.YazmaKaydet(islem, Varlik, g.Id,
            $"gider pusulası {g.PusulaNo} İPTAL edildi (VUK md.234) · gerekçe: {gerekce}",
            hassas: true);

        await db.SaveChangesAsync();

        return new
        {
            g.Id, g.PusulaNo, g.IptalEdildi, g.IptalGerekce, g.IptalTarihi,
            // UX-S27: sonuç bandına yazılacak DOĞRU DÜZELTME YOLU sunucudan gelir.
            // Y4-A: eski metin "YENİ bir hurda alışı girin" diyordu → stok ve kasa
            // İKİNCİ KEZ yazılıyordu. Doğru yol aynı alış için yeniden düzenlemedir.
            duzeltmeYolu = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir($"Aynı alış için 'Yeniden düzenle' ile doğru bilgilerle yeni pusula "
                         + $"düzenleyin (son gün {SonGun(g.TeslimTarihi):dd.MM.yyyy}). Yeni hurda alışı "
                         + "GİRMEYİN; stok ve kasa bu iptalden ETKİLENMEZ."),
            sonGun = SonGun(g.TeslimTarihi),
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET YASAL-4 / Y4-A — İPTAL EDİLEN PUSULANIN YERİNE YENİDEN DÜZENLEME
    //
    // Aynı hurda alışı (hareket_id) için doğru SATICI bilgileriyle YENİ pusula.
    // Tutar/gram/has/ayar/stopaj/teslim tarihi ESKİ BELGEDEN gelir (değişmez —
    // tutar hatası hareket stornosuyla düzeltilir). Yeni hurda alışı YAZILMAZ →
    // stok ve kasa ikinci kez yazılmaz (eski yol bunu yapıyordu).
    // Son günden sonra ENGEL (409): VUK md.234 "süresinde düzenlenmeyen gider
    // pusulası hiç düzenlenmemiş sayılır" — geçersiz belge basmak yanlış güven verir.
    // ⚠ İptal + yeniden düzenleme USULÜ birincil kaynakta bulunamadı (şartname
    //   § 3.2 T-4) → mali müşavir teyidi rapordadır; akış stok çift yazımını kapatır.
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<object> YenidenDuzenle(string id, HurdaSaticiDto? dto, AktifKullanici islem)
    {
        var eski = await db.GiderPusulalari.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == islem.TenantId)
            ?? throw new NotFoundException("Gider pusulası bulunamadı");

        if (!eski.IptalEdildi)
            throw new ConflictException(
                "Bu gider pusulası geçerli; yeniden düzenlenemez. Bilgi yanlışsa önce gerekçeyle iptal edin.");

        if (await db.GiderPusulalari.AsNoTracking()
                .AnyAsync(x => x.TenantId == islem.TenantId && x.HareketId == eski.HareketId && !x.IptalEdildi))
            throw new ConflictException(
                "Bu alış için geçerli bir gider pusulası zaten var; ikinci pusula düzenlenmez.");

        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var sonGun = SonGun(eski.TeslimTarihi);
        if (bugun > sonGun)
            throw new ConflictException(
                $"{GiderPusulasiSuresi.Gun(ayarlar, yasal, YasalParametreServisi.IslemGunu(eski.TeslimTarihi))} gün geçti "
                + $"(son gün {sonGun:dd.MM.yyyy}); bu alış için artık gider pusulası düzenlenemez. "
                + "Mali müşavirinize danışın.");

        if (dto is null)
            throw new BadRequestException("Satıcı bilgileri zorunludur (ad soyad, TCKN, adres).");
        // Satıcı TİPİ değişmez: tip stopajı, stopaj ödenen tutarı belirler → tutar
        // değişikliği bu uçla değil, hareket stornosuyla yapılır.
        if (!string.IsNullOrWhiteSpace(dto.SaticiTipi) && dto.SaticiTipi != eski.SaticiTipi)
            throw new BadRequestException(
                "Satıcı tipi yeniden düzenlemede değiştirilemez: stopaj ve ödenen tutar değişirdi. "
                + "Tutar hatası için alış hareketini stornolayın.");
        dto.SaticiTipi = eski.SaticiTipi;
        dto.MalAciklamasi = string.IsNullOrWhiteSpace(dto.MalAciklamasi) ? eski.MalAciklamasi : dto.MalAciklamasi;

        var kapi = new GiderPusulasiKapisi(ayarlar);
        kapi.Dogrula(dto);
        // YASAL-3 § 4.4: yeni satıcı kimliği de yasal listeye karşı taranır.
        await Masak.YaptirimServisi.KimlikTara(db, islem.TenantId, islem, YaptirimNoktasi.HurdaSatici,
            dto.Tckn, dto.AdSoyad, eski.CariId);

        var now = DateTime.UtcNow;
        var pusulaNo = await FaturaNoUretici.SiradakiPusula(db, islem.TenantId, Zaman.UtcdenYerele(now).Year);
        var yeni = kapi.YenidenDuzenle(db, eski, dto, pusulaNo, islem, now);

        // ⚠ Detaya KİŞİSEL VERİ yazılmaz — yalnız belge numaraları.
        denetim.YazmaKaydet(islem, Varlik, yeni.Id,
            $"gider pusulası {eski.PusulaNo} yerine {yeni.PusulaNo} YENİDEN DÜZENLENDİ (aynı alış; yeni hareket yok)",
            hassas: true);

        // YASAL-4 tur 4 (hasım SINIR2): yukarıdaki "geçerli pusula var mı" okuması yarışa açıktır;
        // asıl kilit UQ (tenant_id, gecerli_hareket_id). Çakışan yazma hiçbir satır bırakmaz
        // (pusula + denetim aynı SaveChanges'ta) ve 409'a çevrilir.
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (BenzersizlikIhlali(ex, "gecerli_hareket"))
        {
            throw new ConflictException(
                "Bu alış için aynı anda başka bir gider pusulası düzenlendi; bu istek YAZILMADI. Listeyi yenileyin.");
        }
        catch (DbUpdateException ex) when (BenzersizlikIhlali(ex, "pusula_no"))
        {
            throw new ConflictException(
                "Aynı anda başka bir gider pusulası numara aldı; bu istek YAZILMADI. Listeyi yenileyip tekrar deneyin.");
        }
        return Yanit(await Bul(yeni.Id, islem.TenantId));
    }

    /// <summary>MySQL 1062 + adı verilen benzersiz indeks (başka ihlaller genel yola düşer).</summary>
    private static bool BenzersizlikIhlali(DbUpdateException ex, string indeksParcasi) =>
        ex.InnerException is { } ic
        && (ic.GetType().GetProperty("Number")?.GetValue(ic) is 1062
            || ic.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
        && ic.Message.Contains(indeksParcasi, StringComparison.OrdinalIgnoreCase);

    private async Task<GiderPusulasi> Bul(string id, string tenantId) =>
        await db.GiderPusulalari.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId)
        ?? throw new NotFoundException("Gider pusulası bulunamadı");

    private static object Yanit(GiderPusulasi g) => new
    {
        g.Id, g.PusulaNo, g.HareketId, g.SubeId, g.CariId,
        g.SaticiTipi, g.SaticiAdSoyad, g.SaticiTckn, g.SaticiAdres,
        g.AyarId, g.GramAgirlik, g.HasGrami, g.MalAciklamasi,
        g.BrutTutar, g.StopajOran, g.StopajTutar, g.NetOdenen,
        g.TeslimTarihi, g.DuzenlemeSonTarihi, g.DuzeltilenId, g.Olusturuldu,
        g.IptalEdildi, g.IptalGerekce, g.IptalTarihi,
    };
}

/// <summary>
/// PAKET YASAL-4 / Y4-A — gider pusulası süresinin TEK hesabı (VUK md.234 M-2).
/// Gün sayısı TESLİM GÜNÜNÜN değeridir (YASAL-1 tablosu); satır yoksa yapılandırma
/// (açılış eşitlik denetimi ikisinin aynı olduğunu zaten garanti eder).
/// </summary>
public static class GiderPusulasiSuresi
{
    public static int Gun(HurdaAyarlari ayarlar, YasalParametreServisi? yasal, DateOnly teslimGunu) =>
        yasal?.SatirBul(YasalAnahtar.GiderPusulasiGun, teslimGunu)?.DegerSayi is { } d
            ? (int)d
            : ayarlar.PusulaDuzenlemeGunu;

    /// <summary>Son gün = teslimin YEREL günü + süre (o gün dahil düzenlenebilir).</summary>
    public static DateOnly SonGun(HurdaAyarlari ayarlar, YasalParametreServisi? yasal, DateTime teslimUtc)
    {
        var teslimGunu = YasalParametreServisi.IslemGunu(teslimUtc);
        return teslimGunu.AddDays(Gun(ayarlar, yasal, teslimGunu));
    }

    /// <summary>
    /// Bir ALIŞIN (hareketin) belge durumu — süre takibi, Panel ve gün sonu mutabakatı
    /// AYNI türetmeyi kullanır (hasım bulgusu 01.10: mutabakat 7 günden eski HER
    /// geçerli pusulayı "süresi dolmuş" sayıyordu).
    /// <list type="bullet">
    /// <item>Geçerli (iptal edilmemiş) pusulası var → <see cref="PusulaBelgeDurumu.Gecerli"/> (süre karşılanmış).</item>
    /// <item>Pusulası yok → <see cref="PusulaBelgeDurumu.Belgesiz"/>.</item>
    /// <item>Hepsi iptal: son gün (ORİJİNAL teslim + süre) geçmediyse yeniden düzenlenecek, geçtiyse belgesiz.</item>
    /// </list>
    /// </summary>
    public static PusulaBelgeDurumu Durum(
        IEnumerable<GiderPusulasi> ayniAlisinPusulalari, HurdaAyarlari ayarlar, YasalParametreServisi? yasal, DateOnly bugun)
    {
        var liste = ayniAlisinPusulalari.ToList();
        if (liste.Count == 0) return PusulaBelgeDurumu.Belgesiz;
        if (liste.Any(p => !p.IptalEdildi)) return PusulaBelgeDurumu.Gecerli;
        var sonGun = SonGun(ayarlar, yasal, liste.Min(p => p.TeslimTarihi));
        return bugun > sonGun ? PusulaBelgeDurumu.Belgesiz : PusulaBelgeDurumu.YenidenDuzenlenecek;
    }
}

/// <summary>
/// Hurda alışında TEDARİKÇİ faturası (mükellef satıcının belgesi) yok mu? Süre takibi
/// (SQL) ve gün sonu mutabakatı (bellek) AYNI tanımı kullanır — P-08 sayacının
/// "belge var mı" satırı tek yerde durur (bağ kurmaz; ADR-0018 kapsamı dışı).
/// </summary>
public static class TedarikciFaturasi
{
    public static bool YokMu(string? faturaNo) => string.IsNullOrEmpty(faturaNo);
}

/// <summary>Alışın gider pusulası açısından durumu (tek türetme: <see cref="GiderPusulasiSuresi.Durum"/>).</summary>
public enum PusulaBelgeDurumu { Gecerli, YenidenDuzenlenecek, Belgesiz }

// ── Uçlar ────────────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/gider-pusulalari")]
// Satıcı TCKN'si taşır → MASAK verisiyle aynı rejim: TEZGAHTAR göremez.
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
public sealed class HurdaController(HurdaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/gider-pusulalari — liste (TCKN MASKELİ)</summary>
    [HttpGet]
    public Task<object> Listele(
        [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, [FromQuery] int limit = 100)
        => servis.Listele(Islem, baslangic, bitis, limit);

    /// <summary>GET /api/v1/gider-pusulalari/sure-takibi — 7 günlük süre uyarıları</summary>
    [HttpGet("sure-takibi")]
    public Task<object> SureTakibi([FromQuery] int uyariGunu = 2) => servis.SureTakibi(Islem, uyariGunu);

    /// <summary>GET /api/v1/gider-pusulalari/{id} — tekil (TAM TCKN, okuma izi düşer)</summary>
    [HttpGet("{id}")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>GET /api/v1/gider-pusulalari/{id}/belge — yazdırılabilir belge (GÖRÜNTÜLEME izi)</summary>
    [HttpGet("{id}/belge")]
    public Task<object> Belge(string id) => servis.Belge(id, Islem);

    /// <summary>
    /// PATCH /api/v1/gider-pusulalari/{id} — PAKET S-3d (DP4).
    /// YALNIZ mali kimlik dışı alanlar: satıcı adresi · mal açıklaması.
    /// Gövdede olmayan alan KORUNUR, null olan SİLİNİR.
    /// </summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] JsonElement govde) =>
        servis.Guncelle(id, govde, Islem);

    /// <summary>
    /// POST /api/v1/gider-pusulalari/{id}/iptal — PAKET S-3b (VUK md.234).
    /// Belgeyi hükümsüz kılar (silmez); yerine yeni pusula düzenlenir.
    /// </summary>
    [HttpPost("{id}/iptal")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Iptal(string id, [FromBody] GerekceDto dto) =>
        servis.Iptal(id, dto.Gerekce, Islem);

    /// <summary>
    /// POST /api/v1/gider-pusulalari/{id}/yeniden-duzenle — PAKET YASAL-4 (Y4-A).
    /// İptal edilmiş pusulanın AYNI alışı için yeni pusula; yeni hareket YAZILMAZ.
    /// Son gün geçtiyse 409 (VUK md.234).
    /// </summary>
    [HttpPost("{id}/yeniden-duzenle")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> YenidenDuzenle(string id, [FromBody] HurdaSaticiDto? dto) =>
        servis.YenidenDuzenle(id, dto, Islem);

    /// <summary>
    /// POST /api/v1/gider-pusulalari/{id}/yazdirildi — UX-4d madde 20.
    /// Ekran gerçekten yazdırma başlattığında (window.print) çağırır;
    /// denetim izine AYRI bir "basım" olayı düşer.
    /// </summary>
    [HttpPost("{id}/yazdirildi")]
    public Task<object> Yazdirildi(string id) => servis.BelgeYazdirildi(id, Islem);
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-20 (Y-10) — HURDA STOK / RAMAT ÇIKIŞI OKUMA UÇLARI
//
// Yazma yolu BİLEREK burada DEĞİLDİR: hurda satışı kasa, cari, MASAK, tevsik
// ve kur bacaklarını birlikte yazar; bunların tek yeri SatisServisi'dir
// (HURDA_ALIS ile AYNI hat). Burada yalnız OKUMA vardır:
//   • şubedeki hurda has bakiyesi (defterden TÜRETİLİR — HurdaStokKapisi)
//   • hurda defteri (alış + satış satırları, stornolar dâhil)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HurdaStokServisi(KuyumcuDbContext db, HurdaAyarlari ayarlar)
{
    /// <summary>Liste sınırı — UX-S29: sınır EKRANDA yazılır, sessiz kırpma yok.</summary>
    public const int Sinir = 100;

    public async Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeYaniti> Bakiye(
        AktifKullanici islem, string? istenenSube = null)
    {
        // PAKET ŞUBE-KAPSAM-A § 4.2.2: görünüm şubesi TEK yoldan çözülür.
        //   Kilitli rol → kendi şubesi (başka şube 403). Tüm-şube rolü → seçili
        //   şube ya da null = BİRLEŞİK (bugünkü "şubesiz SAHİP → 400" kalktı;
        //   OturumSubesiCoz yalnız YAZMA yolunda kalır).
        // ⚠ Bakiye türetmesi YİNE TEK YERDEDİR (HurdaStokKapisi.Bakiyeler): birleşik
        //   yanıt şube başına aynı türetmenin maden bazında toplamıdır → kırılım
        //   toplamı = birleşik toplam (K-4) yapısal olarak.
        var sube = await GorunumSubesi.Coz(db, islem, istenenSube);

        List<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeVeri> satirlar;
        List<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaSubeKirilimi>? kirilim = null;
        if (sube is not null)
        {
            satirlar = await HurdaStokKapisi.Bakiyeler(db, islem with { SubeId = sube });
        }
        else
        {
            var subeler = await db.Subeler.AsNoTracking()
                .Where(s => s.TenantId == islem.TenantId)
                .OrderBy(s => s.Ad).ThenBy(s => s.Id)
                .Select(s => new { s.Id, s.Ad })
                .Take(Rapor.RaporFiltresi.EnFazlaLimit)   // şube sayısı onlarcadır (TB-19 sınırı)
                .ToListAsync();
            kirilim = [];
            foreach (var s in subeler)
            {
                var sSatir = await HurdaStokKapisi.Bakiyeler(db, islem with { SubeId = s.Id });
                if (sSatir.Count > 0)
                    kirilim.Add(new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaSubeKirilimi(s.Id, s.Ad, sSatir));
            }
            satirlar = kirilim.SelectMany(k => k.Satirlar)
                .GroupBy(v => v.Maden, StringComparer.Ordinal)
                .Select(g => new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeVeri(
                    g.Key, g.First().MadenAdi,
                    g.Sum(x => x.HasGram), g.Sum(x => x.GirenHas),
                    g.Sum(x => x.CikanHas), g.Sum(x => x.TahlilFarkiHas)))
                .OrderBy(v => v.Maden, StringComparer.Ordinal)
                .ToList();
        }

        // ⚠ Yanıt tipi ANONİM DEĞİL: Kuyumcu.Contracts tipi döner (P-03). Anonim
        //   nesne dönseydi istemci tipi hiçbir derleyici kilidine bağlanmazdı ve
        //   alan adı kayması ekranda sessiz null olurdu.
        return new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeYaniti(
            sube ?? "",
            satirlar,
            // UX-S30 / K-13: ekran kilidinin SEBEBİ sunucudan gelir, ekranda
            // uydurulmaz. Fire kapalıyken metin de sunucunun metnidir.
            ayarlar.FireKaydiAcik,
            ayarlar.FireKaydiAcik ? null : Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(HurdaStokKapisi.FireKapaliMesaji),
            GorunumSubesi.Kapsam(sube),
            kirilim);
    }

    public async Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaDefterYaniti> Defter(
        AktifKullanici islem, int limit = Sinir, string? istenenSube = null)
    {
        limit = Math.Clamp(limit, 1, Sinir);
        // PAKET ŞUBE-KAPSAM-A: seçili şube → kapının şube sorgusu (tek kaynak);
        // null (tüm şubeler) → aynı tür listesiyle şubesiz (birleşik) sorgu.
        var sube = await GorunumSubesi.Coz(db, islem, istenenSube);
        // CIFT-BACAK tur 2 (hasım ORTA): defter, kartın "giren" kalemindeki takas
        // eski has'ını da satır olarak gösterir — aynı kapı (HurdaStokKapisi.DefterSorgu).
        var defterSorgu = HurdaStokKapisi.DefterSorgu(sube is not null
            ? db.Hareketler.AsNoTracking().OturumSubesinde(islem with { SubeId = sube }, HurdaStokKapisi.SubeGerekli)
            : db.Hareketler.AsNoTracking());

        // Bakiye ile AYNI sorgu kaynağı (tek türetme yeri — UX-S22).
        var toplamAdet = await defterSorgu.CountAsync();

        var satirlar = await defterSorgu
            .OrderByDescending(h => h.Tarih)
            .Take(limit)
            .Select(h => new
            {
                h.Id, h.Tur, h.Tarih, h.TlTutari, h.HasGrami, h.Maden,
                h.CariId, h.Aciklama, h.ParaBirimi, h.TahlilKaynakHareketId,
                h.TakasEskiHasGrami,
                stornoSatiri = h.StornoKaynakId != null,
            })
            .ToListAsync();

        // PAKET P-20b — hangi hurda satışına ne kadar tahlil farkı yazılmış?
        // ⚠ Ekran bunu KENDİ toplamaz (UX-S22 / denetim.sh HURDA_STOK_KACAK_YOL
        //   (d) bileşeni): tek türetme yeri sunucudur.
        var farkToplamlari = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.TahlilKaynakHareketId != null)
            .GroupBy(h => h.TahlilKaynakHareketId!)
            .Select(g => new { Kaynak = g.Key, Has = g.Sum(x => x.HasGrami) })
            .ToDictionaryAsync(x => x.Kaynak, x => x.Has);

        var stornolanan = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.StornoKaynakId != null)
            .Select(h => h.StornoKaynakId!)
            .ToListAsync();

        return new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaDefterYaniti(
            satirlar.Select(s => new Kuyumcu.Contracts.Sayfalar.Hurda.HurdaDefterSatiri(
                // TAKAS satırında TL farkı hurdanın bedeli DEĞİLDİR → 0 (ekran "—" yazar);
                // has = müşteriden alınan eski has (net değil).
                s.Id, s.Tur, s.Tarih,
                s.Tur == HurdaStokKapisi.TakasTuru ? 0m : s.TlTutari,
                HurdaStokKapisi.DefterHas(s.Tur, s.HasGrami, s.TakasEskiHasGrami),
                s.Maden ?? Kuyumcu.Core.Domain.Maden.Altin,
                Kuyumcu.Core.Domain.Maden.Ad(s.Maden ?? Kuyumcu.Core.Domain.Maden.Altin),
                s.CariId, s.Aciklama, s.ParaBirimi,
                s.stornoSatiri, stornolanan.Contains(s.Id),
                s.TahlilKaynakHareketId,
                farkToplamlari.TryGetValue(s.Id, out var f)
                    ? Math.Round(f, 6, MidpointRounding.AwayFromZero) : 0m)).ToList(),
            toplamAdet,
            limit,
            toplamAdet > satirlar.Count,
            // UX-S29: sınır ve daraltma yolu METİN olarak döner.
            toplamAdet > satirlar.Count
                ? $"Yalnız son {limit} hurda hareketi gösteriliyor (toplam {toplamAdet})."
                : null);
    }
}

/// <summary>
/// PAKET P-20 — hurda stok okuma uçları. Kişisel veri TAŞIMAZ (satıcı kimliği
/// gider pusulası ucundadır), bu yüzden modül yetkisi "islem" ile açılır ve
/// ayrıca rol kapısı konmaz — hurda defterini gören, işlem defterini de görür.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/hurda")]
[Yetki.ModulYetkisi("islem")]
public sealed class HurdaStokController(HurdaStokServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/hurda/bakiye — şubedeki hurda has bakiyesi (defterden türetilir)</summary>
    [HttpGet("bakiye")]
    public Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaBakiyeYaniti> Bakiye([FromQuery] string? subeId)
        => servis.Bakiye(Islem, subeId);

    /// <summary>GET /api/v1/hurda/defter — hurda alış + satış satırları</summary>
    [HttpGet("defter")]
    public Task<Kuyumcu.Contracts.Sayfalar.Hurda.HurdaDefterYaniti> Defter(
        [FromQuery] int limit = HurdaStokServisi.Sinir, [FromQuery] string? subeId = null)
        => servis.Defter(Islem, limit, subeId);
}
