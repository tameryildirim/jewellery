using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 (§ 4.2) — PEŞİN MÜŞTERİ ART ARDA FİŞ KAPISI.
//
// SORUN: Peşin Müşteri kartı bir KİŞİ değildir; satırları MASAK kümülatifine ve
// tevsik havuzuna GİRMEZ (PAKET B-3 · K1). Karşılığı tek işlem eşiğinin fail-
// closed reddidir (K2). Açık kalan: aynı kişi eşik ALTI birden çok fişle eşiği
// bölebilir ("KALAN RİSK", MasakKapisi.DogrulaKumulatif notu).
//
// KURAL (SİNYAL — mevzuat yorumu DEĞİL, şartname § 3.2 T-4): aynı kullanıcı +
// aynı şube, son PesinPencereDakika içindeki Peşin Müşteri işlemleri + bu işlem
// MASAK kimlik eşiğine ulaşıyor YA DA nakit toplamı tevsik haddini aşıyorsa
// sunucu TEK SORU sorar (masak.ayniKisiBeyani):
//   • yoksa → 400, kod PESIN_ART_ARDA_SORU (ekran soruyu gösterir);
//   • true  → 400 "gerçek müşteri seçin" (mevcut K2 yolu, alan cariId);
//   • false → işlem geçer + denetim izine PESIN_ART_ARDA_FARKLI_KISI (kişisel
//     veri yok: kullanıcı, tutar, pencere toplamı).
// Soru, sorumluluğu beyanla tezgâhtara bağlar ve kayda geçirir (Anayasa md.1:
// tahmin yok — program kişiyi bilemez).
//
// ⚠ TEVSİK TARAFI TUTUCU: kapı bu işlemin NAKİT payını bilmez (yalnız tutarı);
//   pencerenin önceki nakdi + bu işlemin TAMAMI hadle kıyaslanır. Yanlış yönde
//   hata yalnız fazladan soru üretir, hiçbir işlemi gizlice geçirmez.
// ⚠ KULLANICI BİLİNMİYORSA (DogrulaKumulatif'i islem vermeden çağıran yollar)
//   pencere KİRACI GENELİDİR (daha geniş küme → daha çok soru; fail-closed yön).
// ═════════════════════════════════════════════════════════════════════════════

public static class PesinPencereKapisi
{
    /// <summary>Ekranın soruyu göstermesi için hata gövdesindeki kod.</summary>
    public const string SoruKodu = "PESIN_ART_ARDA_SORU";

    /// <summary>Hata gövdesindeki alan anahtarı (camelCase) — soru bu alana aittir.</summary>
    public const string BeyanAlani = "ayniKisiBeyani";

    /// <summary>Denetim izindeki varlık türü (kişisel veri taşımaz).</summary>
    public const string DenetimTuru = "PESIN_ART_ARDA_FARKLI_KISI";

    /// <summary>Tezgâhın gördüğü tek soru cümlesi (sade, kod yok).</summary>
    public const string SoruMesaji =
        "Az önce bu kasada Peşin Müşteri ile işlem yapıldı. Bu, aynı müşteri mi?";

    /// <summary>"Evet, aynı kişi" cevabının red cümlesi — gerçek müşteri seçilir.</summary>
    public const string AyniKisiMesaji =
        "Aynı müşteriyle yapılan işlemlerin toplamı kimlik sınırına ulaşıyor. Müşteriyi seçin ya da yeni müşteri kartı açın.";

    /// <summary>Pencere ölçüsü: işlem adedi, MASAK ölçüsü toplamı, nakit toplamı.</summary>
    public readonly record struct Pencere(int Adet, decimal Olcu, decimal Nakit);

    /// <summary>
    /// Son <paramref name="dakika"/> içindeki Peşin Müşteri işlemleri. Kullanıcı/şube
    /// verilmezse kiracı geneli (fail-closed yön). Storno çifti dışlanır (MASAK
    /// kümülatifiyle aynı kural). Ölçü iki ayrık sorguyla (A5): masak_olcu_tl dolu
    /// satırlar el değiştiren değerle, boşlar TL bacağıyla.
    /// </summary>
    public static async Task<Pencere> PencereOku(
        KuyumcuDbContext db, string tenantId, string pesinCariId,
        string? kullaniciId, string? subeId, DateTime an, int dakika, DateTime? ust = null)
    {
        // ŞUBE-KAPSAM-A ile aynı gerekçe: şubeye kilitli rolde süzgeç kümülatifi
        // bölmesin — kullanıcı + şube koşulu burada AÇIKÇA yazılır.
        if (db.SubeFiltresiAcik)
            return await SistemKapsami.YasalKumulatifOku(db, tenantId,
                kdb => PencereOku(kdb, tenantId, pesinCariId, kullaniciId, subeId, an, dakika, ust));

        var bas = an.AddMinutes(-dakika);
        // PAKET MASAK-ESZAMAN-2: kilit altı yinelemede üst sınır "şimdi"dir — kilidi bırakan eşzamanlı
        // isteğin satırı (tarihi bu isteğin an'ından birkaç ms SONRA olabilir) pencereden düşmesin.
        var son = ust is { } u && u > an ? u : an;
        var q = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.CariId == pesinCariId
                     && h.Tarih >= bas && h.Tarih <= son
                     && PesinTurleri.Contains(h.Tur));
        if (!string.IsNullOrEmpty(kullaniciId)) q = q.Where(h => h.KullaniciId == kullaniciId);
        if (!string.IsNullOrEmpty(subeId)) q = q.Where(h => h.SubeId == subeId);
        q = q.StornoCiftiniDisla(db, tenantId);

        // Adet FİŞ değil İŞLEM kümesidir: sepet fişi kalem başına satır yazar; soru
        // için "en az bir önceki işlem var mı" yeterlidir.
        var adet = await q.CountAsync();
        if (adet == 0) return new Pencere(0, 0m, 0m);

        var olculu = await q.Where(h => h.MasakOlcuTl != null).SumAsync(h => h.MasakOlcuTl) ?? 0m;
        var olcusuz = await q.Where(h => h.MasakOlcuTl == null).SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m;
        // Nakit payı: nakit_tutar dolu satırlar + (eski) tek etiketli NAKİT satırlar — ayrık.
        var nakitPay = await q.Where(h => h.NakitTutar != null).SumAsync(h => h.NakitTutar) ?? 0m;
        var nakitEski = await q.Where(h => h.NakitTutar == null && h.OdemeTuru == "NAKIT")
            .SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m;
        return new Pencere(adet, olculu + olcusuz, Math.Abs(nakitPay) + nakitEski);
    }

    /// <summary>
    /// Kapı. Tek işlem kuralları (B-3 · K2) ÇAĞIRANDA önce işler; buraya yalnız eşik
    /// ALTI tek işlem gelir. Soru gerekmiyorsa sessiz döner.
    /// </summary>
    public static async Task Denetle(
        KuyumcuDbContext db, string tenantId, string pesinCariId, AktifKullanici? islem, string? subeId,
        decimal tutarTl, bool? ayniKisiBeyani, DateTime an,
        decimal kimlikEsigi, decimal tevsikHad, int dakika, bool kilitAltinda = false)
    {
        if (dakika <= 0) return;   // politika kapalı (appsettings Masak:PesinPencereDakika = 0)
        var tutar = Math.Abs(tutarTl);
        // PAKET MASAK-ESZAMAN-2 — KİLİT ALTI YİNELEME (MasakKapisi.KilitAltindaYinele çağırır; Peşin
        // kartının satırı CariIcinBaslat ile kilitli). Kural/eşik AYNI; değişen yalnız okumanın zamanı.
        // Faz 1'in bekleyen (henüz yazılmamış) beyan izi atılır ve güncel pencereyle YENİDEN yazılır:
        // aynı işlem için iki iz doğmaz, iz kilit altındaki güncel toplamı taşır.
        if (kilitAltinda)
            foreach (var g in db.ChangeTracker.Entries<DenetimKaydi>()
                         .Where(e => e.State == EntityState.Added
                                  && e.Entity.VarlikTuru == DenetimTuru && e.Entity.VarlikId == pesinCariId)
                         .ToList())
                g.State = EntityState.Detached;
        var p = await PencereOku(db, tenantId, pesinCariId, islem?.Id, subeId ?? islem?.SubeId, an, dakika,
            kilitAltinda ? DateTime.UtcNow : null);
        if (p.Adet == 0) return;

        var masakAsim = p.Olcu + tutar >= kimlikEsigi;
        var tevsikAsim = p.Nakit + tutar > tevsikHad;
        if (!masakAsim && !tevsikAsim) return;

        if (ayniKisiBeyani is null)
            throw new BadRequestException(SoruMesaji).AlanIle(BeyanAlani).KodIle(SoruKodu);

        if (ayniKisiBeyani == true)
            throw new BadRequestException(AyniKisiMesaji).AlanIle("cariId");

        // false — beyan kayda geçer. Kişisel veri YOK: yalnız tutarlar ve pencere.
        var detay = FormattableString.Invariant(
            $"{DenetimTuru}: tezgâhtar 'başka müşteri' beyan etti · bu işlem {tutar:0.00} TL · ")
            + FormattableString.Invariant(
            $"pencere ({dakika} dk) önceki toplam {p.Olcu:0.00} TL, önceki nakit {p.Nakit:0.00} TL, önceki işlem {p.Adet} · ")
            + (masakAsim ? "MASAK kimlik eşiği" : "") + (masakAsim && tevsikAsim ? " + " : "")
            + (tevsikAsim ? "nakit sınırı" : "") + " eşiği bölme sinyali";
        var denetim = new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        if (islem is not null)
            denetim.YazmaKaydet(islem, DenetimTuru, pesinCariId, detay, hassas: true);
        else
            denetim.SistemYazmaKaydet(tenantId, DenetimTuru, pesinCariId, detay);
    }

    /// <summary>
    /// Pencereye giren türler = MASAK kapısına tabi türler (MasakKapisi ile aynı
    /// küme; Peşin kartında veresiye/tahsilat zaten yasaktır).
    /// </summary>
    private static readonly string[] PesinTurleri =
        ["SATIS", "ALIS", "HURDA_ALIS", "TAKAS", "ISCILIK", "HURDA_SATIS"];
}
