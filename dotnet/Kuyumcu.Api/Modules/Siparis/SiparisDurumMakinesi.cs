using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Siparis;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ DURUM MAKİNESİ — SAF sınıf (paket 3 Faz A).
//
// Veritabanı yok, HttpContext yok, saat yok: yalnız (tür, mevcut, yeni) üçlüsü.
// Bu yüzden mutasyon kanıtı ucuzdur — M143 tek bir sınıfı bozar ve saf testler
// anında kırmızıya döner.
//
// ⚠ FAIL-CLOSED: bilinmeyen tür ya da bilinmeyen durum → false. "Tanımadığım
//   geçişe izin veririm" davranışı bir durum makinesini durum makinesi olmaktan
//   çıkarır.
// ═════════════════════════════════════════════════════════════════════════════

public static class SiparisDurumMakinesi
{
    /// <summary>
    /// Tür bazında izinli geçişler. TESLIM_EDILDI ve IPTAL TERMİNALDİR —
    /// tabloda anahtarları YOKTUR, dolayısıyla hiçbir çıkışları olamaz.
    /// <para>
    /// ⚠ QA turu 5 (YÜKSEK): ONAYLANDI tek tıkla ve geri dönüşsüz oluyordu; sipariş
    /// aynı anda hem düzenlenemez hem geri alınamaz hâle geliyordu. ONAYLANDI → TASLAK
    /// geri geçişi AÇILDI. Gerekçe zorunluluğu kendiliğinden doğar: GerekceGerekli
    /// geri yönlü geçişte (Sira[yeni] &lt; Sira[mevcut]) zaten true döner.
    /// Daha ileri durumlardan (ATOLYEDE, HAZIR) geri dönüş bir süre AÇILMADI: o
    /// adımlarda mal fiziken atölyeye çıkmış olabilir; geri alma kuralı DOĞRULANMAMIŞTI.
    /// </para>
    /// <para>
    /// ⚠ PAKET P-22 / KARAR K-23 (2026-09-21, KARARLAR-URUN-2026-09-21.md § K-23):
    /// ATOLYEDE ve HAZIR durumlarından GERİ DÖNÜŞ AÇILDI — ama sessiz değil,
    /// DENETİMLİ. Dört şart BİRLİKTE uygulanır ve dördü de test edilir:
    ///   1. Yetki: geri yönlü geçiş <see cref="SiparisServisi.GeriAlmaRolleri"/>
    ///      ister (tezgâhtar ileri götürür, geri alamaz).
    ///   2. Gerekçe: <see cref="GerekceGerekli"/> geri yönde zaten true döner;
    ///      boş/kısa gerekçe 400 ile reddedilir.
    ///   3. Denetim izi: geçiş <c>siparis_gecisleri</c>'ne kim · ne zaman ·
    ///      hangi durumdan hangi duruma · neden olarak düşer (append-only).
    ///   4. Görünürlük: geri alınan geçiş sipariş geçmişinden SİLİNMEZ.
    /// Gerekçe: "düzeltmek için belgeyi iptal et" yolu numara serisini yakar ve
    /// deftere yalan yazdırır (QA turu 7 iki KRİTİK bulgu). UX-1: her veri girişi
    /// düzenlenebilir.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string[]>> Tablo = new(StringComparer.Ordinal)
    {
        [SiparisTuru.MusteriSiparisi] = new(StringComparer.Ordinal)
        {
            [SiparisDurumu.Taslak]    = [SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            [SiparisDurumu.Onaylandi] = [SiparisDurumu.Atolyede, SiparisDurumu.Hazir, SiparisDurumu.Taslak, SiparisDurumu.Iptal],
            // K-23: atölyeden geri çağrılabilir (ONAYLANDI) — gerekçeli, yetkili.
            [SiparisDurumu.Atolyede]  = [SiparisDurumu.Hazir, SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            // K-23: "hazır" yanlış basıldıysa ATOLYEDE ya da ONAYLANDI'ya dönülür.
            [SiparisDurumu.Hazir]     = [SiparisDurumu.TeslimEdildi, SiparisDurumu.Atolyede, SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
        },
        // ⚠ Atölye siparişi ONAYLANDI'dan doğrudan HAZIR'a GEÇMEZ: mal dış
        //   atölyeye verilmeden hazır olamaz (ATOLYEDE adımı atlanamaz).
        [SiparisTuru.AtolyeSiparisi] = new(StringComparer.Ordinal)
        {
            [SiparisDurumu.Taslak]    = [SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            [SiparisDurumu.Onaylandi] = [SiparisDurumu.Atolyede, SiparisDurumu.Taslak, SiparisDurumu.Iptal],
            [SiparisDurumu.Atolyede]  = [SiparisDurumu.Hazir, SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            // ⚠ K-23 + adım atlamama: atölye siparişi HAZIR'dan yalnız ATOLYEDE'ye
            //   döner. ONAYLANDI'ya dönmek malın atölyeden çıktığını iddia ederdi;
            //   o iddia bu ekrandan doğrulanamaz (Anayasa md.1). İki adım geri
            //   gerekiyorsa iki geçiş yazılır ve ikisi de izde görünür.
            [SiparisDurumu.Hazir]     = [SiparisDurumu.TeslimEdildi, SiparisDurumu.Atolyede, SiparisDurumu.Iptal],
        },
        // Tamir küçük olabilir (zincir lehimi) — atölyeye hiç gitmeden hazır olur.
        [SiparisTuru.Tamir] = new(StringComparer.Ordinal)
        {
            [SiparisDurumu.Taslak]    = [SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            [SiparisDurumu.Onaylandi] = [SiparisDurumu.Atolyede, SiparisDurumu.Hazir, SiparisDurumu.Taslak, SiparisDurumu.Iptal],
            [SiparisDurumu.Atolyede]  = [SiparisDurumu.Hazir, SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
            [SiparisDurumu.Hazir]     = [SiparisDurumu.TeslimEdildi, SiparisDurumu.Atolyede, SiparisDurumu.Onaylandi, SiparisDurumu.Iptal],
        },
    };

    /// <summary>Durumun ileri-geri sırası — "geri yönlü geçiş" bunun üzerinden ölçülür.</summary>
    private static readonly Dictionary<string, int> Sira = new(StringComparer.Ordinal)
    {
        [SiparisDurumu.Taslak] = 0,
        [SiparisDurumu.Onaylandi] = 1,
        [SiparisDurumu.Atolyede] = 2,
        [SiparisDurumu.Hazir] = 3,
        [SiparisDurumu.TeslimEdildi] = 4,
        [SiparisDurumu.Iptal] = 5,
    };

    /// <summary>Bu tür için bu geçişe izin var mı? Bilinmeyen tür/durum → false.</summary>
    public static bool Izinli(string? tur, string? mevcut, string? yeni)
    {
        if (tur is null || mevcut is null || yeni is null) return false;
        if (!Tablo.TryGetValue(tur, out var turTablosu)) return false;
        if (!turTablosu.TryGetValue(mevcut, out var hedefler)) return false;   // terminal ya da bilinmeyen
        return Array.IndexOf(hedefler, yeni) >= 0;
    }

    /// <summary>Bu türde bu durumdan gidilebilecek durumlar (ekranın düğme kümesi).</summary>
    public static IReadOnlyList<string> Hedefler(string? tur, string? mevcut)
    {
        if (tur is null || mevcut is null) return [];
        if (!Tablo.TryGetValue(tur, out var turTablosu)) return [];
        return turTablosu.TryGetValue(mevcut, out var hedefler) ? hedefler : [];
    }

    /// <summary>
    /// Geri yönlü geçiş mi? (K-23'ün yetki ve gerekçe şartları buna bağlıdır.)
    /// IPTAL geri yönlü SAYILMAZ: sırası en büyüktür ve kendi rol kapısı vardır.
    /// </summary>
    public static bool GeriYonlu(string? mevcut, string? yeni)
    {
        if (mevcut is null || yeni is null) return false;
        if (yeni == SiparisDurumu.Iptal) return false;
        if (!Sira.TryGetValue(mevcut, out var m) || !Sira.TryGetValue(yeni, out var y)) return false;
        return y < m;
    }

    /// <summary>
    /// Gerekçe zorunlu mu? IPTAL her zaman gerekçe ister; geri yönlü geçiş de
    /// (K-23 şart 2 — ATOLYEDE/HAZIR geri dönüşü dâhil).
    /// </summary>
    public static bool GerekceGerekli(string? mevcut, string? yeni)
    {
        if (yeni == SiparisDurumu.Iptal) return true;
        return GeriYonlu(mevcut, yeni);
    }

    /// <summary>Terminal durum mu? (hiçbir alanı değişmez, salt okunur)</summary>
    public static bool Terminal(string? durum) =>
        durum is not null && Array.IndexOf(SiparisDurumu.Terminal, durum) >= 0;
}

// ═════════════════════════════════════════════════════════════════════════════
// GECİKME ÖLÇÜMÜ — saat soyutlaması (Aşkın'ın koşulu).
//
// "Geciken rozeti" bUnit'te sahte saat ister; projede saat soyutlaması YOK ve
// bu paket için bir tane AÇMAK ikinci bir zaman gerçeği doğururdu. Çözüm:
//   • karar SAF bir işlevde verilir ve "bugün" PARAMETREDİR → saf test ölçer;
//   • sunucu bunu Zaman.YerelBugun() ile çağırır (K5: gece 00:00-03:00 penceresi);
//   • ekran kendi saatine BAKMAZ, sunucunun döndürdüğü `geciken` bayrağını çizer
//     → bUnit testi sahte saat olmadan ölçülebilir (UX-S21: sistemin bildiği
//     değer kullanıcıdan/istemciden istenmez).
// ═════════════════════════════════════════════════════════════════════════════

public static class SiparisGecikme
{
    /// <summary>Söz verilen gün geçmiş VE sipariş hâlâ açıksa gecikmiştir.</summary>
    public static bool Gecikti(DateOnly? sozVerilenGun, string? durum, DateOnly bugun)
    {
        if (sozVerilenGun is not { } gun) return false;
        if (durum is null || Array.IndexOf(SiparisDurumu.Acik, durum) < 0) return false;
        return gun < bugun;
    }
}
