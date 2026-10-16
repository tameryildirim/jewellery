using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Arama;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Arama;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-15 — KABUK ARAMA UCU (kimlikler Y-29 · Y-5 · B-25 · B-29)
//
// NEDEN VAR (Y-5): ekranlar açılır listeleri `?limit=200` ile çekiyordu. 200.
// kayıttan sonrası SESSİZCE yoktu ve kullanıcı bunu göremiyordu (UX-S29 ihlali).
// Bu uç sunucuda ARAR ve SAYFALAR; ekran yalnız gördüğünü çizer.
//
// NEDEN ULID DEĞİL (B-25): "Fişten Fatura Kes" 26 haneli bir ULID istiyordu.
// Burası her kayıt için AD + TÜR + AYIRT EDİCİ ALT BİLGİ döndürür; kimlik
// yalnız gövdeye konur.
//
// ── ŞUBE SÖZLEŞMESİ (PLAN P-15 RİSKİ: "arama ucu şube filtresiyle çakışırsa
//    çapraz şube sonucu döner; negatif test zorunlu") ──────────────────────
//   1. Kapsamın TABANI P-04'ün global sorgu filtresidir. Bu servis kiracı ya
//      da şube süzgecini ELLE KURMAZ; `ISubeVarlik` varlıklarında süzgeç
//      DbContext seviyesinde zaten kuruludur.
//   2. `sube` parametresi yalnız DARALTIR. Şubeye kilitli bir kullanıcı
//      BAŞKA bir şubeyi isterse yanıt 403'tür — sessizce kendi şubesine
//      düşürülmez (fail-blind yasak, Anayasa md.1) ve asla çapraz şube
//      sonucu dönmez.
//   3. Şubesiz varlıklar (Cari, Urun, Fatura, BankaHareket `ISubeVarlik`
//      DEĞİLDİR) kiracı genelindedir; `sube` onlarda anlamsızdır ve
//      UYGULANMAZ. Bu bir kusur değil, veri modelinin gerçeğidir —
//      uydurma bir şube süzgeci "daraltıyormuş" gibi yapardı.
//
// ── YETKİ ───────────────────────────────────────────────────────────────────
//   Tek uç birden çok modülü tarar; bu yüzden sınıf düzeyinde tek
//   `[ModulYetkisi]` YETMEZ. Tür başına `YetkiServisi.ErisebilirMi` çağrılır;
//   yetkisi olmayan tür genel aramada SESSİZCE ATLANMAZ, tek tür aramasında
//   403 döner. Genel aramada atlanan tür yanıtın dışında kalır — kullanıcı
//   zaten o ekranı göremez (menü de göstermez).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class AramaServisi(KuyumcuDbContext db, Yetki.YetkiServisi yetki)
{
    /// <summary>Sunucunun kabul ettiği en büyük sayfa boyu (UX-S29: ekran bunu yazar).</summary>
    public const int Tavan = 50;

    /// <summary>Sorgunun anlamlı sayılması için gereken en az karakter.</summary>
    public const int AsgariSorgu = 2;

    public async Task<AramaYaniti> Ara(
        string? q, string? tur, int? limit, int? ofset, string? sube, AktifKullanici islem)
    {
        var sorgu = (q ?? "").Trim();
        var sayfa = Math.Clamp(limit ?? 10, 1, Tavan);
        var atla = Math.Max(ofset ?? 0, 0);
        var istenenTur = string.IsNullOrWhiteSpace(tur) ? "GENEL" : tur.Trim().ToUpperInvariant();

        if (istenenTur != "GENEL" && Array.IndexOf(AramaTurleri.Tumu, istenenTur) < 0)
            throw new BadRequestException($"Bilinmeyen arama türü: {istenenTur}");

        // ── P-38 · B-25 — GÖZATMA ────────────────────────────────────────────
        // QA turu 7 #159: "Fişten Fatura Kes" fiş araması 200 dönüyor ama
        // HİÇ sonuç vermiyordu. Sebep uydurma değil, ölçüldü: fişin insanın
        // bildiği bir adı yoktur (kimliği ULID'dir) ve arama en az iki karakter
        // istiyordu — yani seçici ancak ULID ya da cari adı EZBERE bilinirse
        // dolabiliyordu. Gözatılabilir türlerde sorgu kısa/boşsa sunucu EN SON
        // kayıtları listeler; boş yanıt "kayıt yok" anlamına GELMEZ (UX-S28).
        var gozatma = sorgu.Length < AsgariSorgu
                      && istenenTur != "GENEL"
                      && AramaTurleri.Gozatilir(istenenTur);

        if (sorgu.Length < AsgariSorgu && !gozatma)
            return new AramaYaniti(sorgu, istenenTur, sayfa, atla, 0, false, Tavan, []);

        var subeSuzgeci = SubeDarallmasiCoz(sube, islem);

        var turler = istenenTur == "GENEL" ? AramaTurleri.Genel : [istenenTur];
        var toplanan = new List<AramaSonucu>();
        var dahaVar = false;

        foreach (var t in turler)
        {
            var modul = AramaTurleri.Modul(t)
                ?? throw new BadRequestException($"Bilinmeyen arama türü: {t}");

            var erisebilir = islem.Rol == "SAHIP"
                || await yetki.ErisebilirMi(islem.TenantId, islem.Rol, modul);
            if (!erisebilir)
            {
                // Tek tür istendiyse sebebi SÖYLENİR; genel aramada tür atlanır.
                if (istenenTur != "GENEL")
                    throw new ForbiddenException($"Bu arama için yetkiniz yok ({modul})");
                continue;
            }

            // Bir fazlasını iste: "daha var mı" sorusunu SAYARAK cevapla,
            // tahmin etme (UX-S29 — sessiz kırpma yok).
            // Gözatmada kısa sorgu SÜZGEÇ DEĞİLDİR (tek harf ULID'de gürültüdür):
            // boş sorguyla en son kayıtlar listelenir.
            var parca = await TurAra(t, gozatma ? string.Empty : sorgu, sayfa + 1, atla, subeSuzgeci);
            if (parca.Count > sayfa)
            {
                dahaVar = true;
                parca.RemoveAt(parca.Count - 1);
            }
            toplanan.AddRange(parca);
        }

        return new AramaYaniti(
            sorgu, istenenTur, sayfa, atla, toplanan.Count, dahaVar, Tavan, toplanan, gozatma);
    }

    /// <summary>
    /// Şube daraltmasını çözer. Dönüş null = daraltma YOK (P-04 filtresi neyse o).
    /// <para>
    /// ⚠ Bu metot GENİŞLETMEZ. Şubeye kilitli kullanıcı başka şube isterse 403;
    /// sessizce kendi şubesine düşürülmez, asla çapraz şube sonucu dönmez.
    /// </para>
    /// </summary>
    private string? SubeDarallmasiCoz(string? sube, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(sube)) return null;
        var istenen = sube.Trim();

        var tumSubeler = SubeYetkisi.TumSubeler(islem.Rol);
        if (!tumSubeler && istenen != islem.SubeId)
            throw new ForbiddenException(
                "Başka bir şubenin kayıtlarında arama yapamazsınız.");

        return istenen;
    }

    private async Task<List<AramaSonucu>> TurAra(
        string tur, string q, int al, int atla, string? sube) => tur switch
    {
        AramaTurleri.Cari => await CariAra(q, al, atla),
        AramaTurleri.Urun => await UrunAra(q, al, atla),
        AramaTurleri.Fis => await FisAra(q, al, atla, sube),
        AramaTurleri.Fatura => await FaturaAra(q, al, atla),
        AramaTurleri.BankaHareket => await BankaHareketAra(q, al, atla),
        AramaTurleri.Hareket => await HareketAra(q, al, atla, sube),
        _ => throw new BadRequestException($"Bilinmeyen arama türü: {tur}"),
    };

    // ── CARİ ────────────────────────────────────────────────────────────────
    // Cari ISubeVarlik DEĞİLDİR (kiracı geneli). Şube daraltması UYGULANMAZ.
    private async Task<List<AramaSonucu>> CariAra(string q, int al, int atla) =>
        (await db.Cariler.AsNoTracking()
            .Where(c => c.Ad.Contains(q)
                     || (c.Telefon != null && c.Telefon.Contains(q))
                     || (c.VergiNo != null && c.VergiNo.Contains(q)))
            .OrderByDescending(c => c.Aktif).ThenBy(c => c.Ad)
            .Skip(atla).Take(al)
            .Select(c => new { c.Id, c.Ad, c.Telefon, c.Tur, c.Aktif })
            .ToListAsync())
        .Select(c => new AramaSonucu(
            c.Id,
            c.Ad,
            // Ayırt edici alt bilgi: aynı adlı iki cari telefonuyla ayrılır.
            // Pasif kayıt GİZLENMEZ, İŞARETLENİR (UX-S25).
            // ⚠ UX-S31: ham kod (MUSTERI/TEDARIKCI) BASILMAZ — türü rozet söyler.
            string.Join(" · ", new[] { EkranTelefon(c.Telefon), c.Aktif ? null : "pasif" }
                .Where(x => !string.IsNullOrWhiteSpace(x))),
            AramaTurleri.Cari,
            $"/cariler?cariId={c.Id}"))
        .ToList();

    // ── ÜRÜN ────────────────────────────────────────────────────────────────
    private async Task<List<AramaSonucu>> UrunAra(string q, int al, int atla) =>
        (await db.Urunler.AsNoTracking()
            .Where(u => u.Ad.Contains(q) || u.Kod.Contains(q))
            .OrderBy(u => u.Ad)
            .Skip(atla).Take(al)
            .Select(u => new { u.Id, u.Ad, u.Kod, u.AyarId, u.GramAgirlik })
            .ToListAsync())
        .Select(u => new AramaSonucu(
            u.Id,
            u.Ad,
            // KATMAN-1b (tasarım hakemi 29.09 KRİTİK): gram tr-TR ("4,1 g"), ham "4.1 g" değil.
            $"{u.Kod} · {u.AyarId} · {EkranGram(u.GramAgirlik)}",
            AramaTurleri.Urun,
            $"/urunler?q={Uri.EscapeDataString(u.Kod)}"))
        .ToList();

    // ── FİŞ (B-25: "Fişten Fatura Kes" kayıt seçicisinin kaynağı) ───────────
    //
    // Fiş bir TABLO DEĞİLDİR: aynı `fis_id` değerini taşıyan hareket satırları
    // kümesidir. Bu yüzden gruplanarak aranır. Hareket ISubeVarlik'tir →
    // P-04 global süzgeci zaten uygulanır; `sube` yalnız DARALTIR.
    private async Task<List<AramaSonucu>> FisAra(string q, int al, int atla, string? sube)
    {
        var temel = db.Hareketler.AsNoTracking().Where(h => h.FisId != null);

        // [P-04 sınıf b] kullanıcı seçimli daraltma — yetki süzgeci DEĞİL
        // (yetki süzgeci global filtrededir; bu yalnız görünümü daraltır).
        if (sube is not null) temel = temel.Where(h => h.SubeId == sube);

        // PAKET OLCEK-YUK (TB-19, ÖLÇÜLDÜ): eşleşme ve sayfalama SUNUCUDA.
        // Eski hâl kiracının TÜM fişlerini gruplayıp belleğe alıyor, eşleşmeyi ve
        // Skip/Take'i bellekte yapıyordu: 180.000 fişli kiracıda tek arama ~500 ms
        // (aranan şey yokken bile). Şimdi:
        //   (1) adı eşleşen cariler SQL alt sorgusudur (diğer aramalarla AYNI kural: Ad.Contains),
        //   (2) eşleşen satırlar gruplanır, en yeni fiş önce, Skip/Take SQL'de,
        //   (3) toplamlar YALNIZ sayfadaki fişler için, fişin TÜM satırlarından hesaplanır
        //       (yanıt sözleşmesi ve kalem sayısı/tutar anlamı aynı),
        //   (4) cari adı yalnız sayfadaki cariler için okunur.
        var adiEslesenCariler = db.Cariler.AsNoTracking()
            .Where(c => c.Ad.Contains(q))
            .Select(c => c.Id);

        var sayfa = await temel
            .Where(h => h.FisId!.Contains(q)
                     || (h.CariId != null && adiEslesenCariler.Contains(h.CariId)))
            .GroupBy(h => h.FisId!)
            .Select(g => new { FisId = g.Key, Tarih = g.Max(x => x.Tarih) })
            .OrderByDescending(g => g.Tarih).ThenBy(g => g.FisId)
            .Skip(atla).Take(al)
            .ToListAsync();
        if (sayfa.Count == 0) return [];

        var sayfaFisleri = sayfa.Select(s => s.FisId).ToList();
        var gruplar = await temel
            .Where(h => sayfaFisleri.Contains(h.FisId!))
            .GroupBy(h => h.FisId!)
            .Select(g => new
            {
                FisId = g.Key,
                Tarih = g.Max(x => x.Tarih),
                KalemSayisi = g.Count(),
                ToplamTl = g.Sum(x => x.TlTutari),
                CariId = g.Max(x => x.CariId),
            })
            .ToDictionaryAsync(g => g.FisId);

        var fisCarileri = gruplar.Values.Where(g => g.CariId != null).Select(g => g.CariId!).Distinct().ToList();
        var cariAdlari = fisCarileri.Count == 0
            ? new Dictionary<string, string>()
            : await db.Cariler.AsNoTracking()
                .Where(c => fisCarileri.Contains(c.Id))
                .Select(c => new { c.Id, c.Ad })
                .Take(fisCarileri.Count)
                .ToDictionaryAsync(c => c.Id, c => c.Ad);

        return sayfa
            .Where(s => gruplar.ContainsKey(s.FisId))
            .Select(s => gruplar[s.FisId])
            .Select(g => new
            {
                g.FisId, g.Tarih, g.KalemSayisi, g.ToplamTl,
                CariAd = g.CariId is not null && cariAdlari.TryGetValue(g.CariId, out var ad)
                    ? ad : null,
            })
            .Select(g => new AramaSonucu(
                g.FisId,
                // AD = insanın tanıdığı şey: cari + tarih. ULID EKRANDA YOK.
                $"{g.CariAd ?? "—"} · {Zaman.UtcdenYerele(g.Tarih):dd.MM.yyyy}",
                // ZO-BELGE 29.09 (tasarım hakemi KRİTİK, K5): tutar ekranın para biçimiyle (tr-TR, ₺) —
                // ham "2100.00 TRY" değil "2.100 ₺". Kural EkranPara ile tek yerde (Bicim.Para ile birebir).
                // Doğrulayıcı 29.09 (KRİTİK): EkranPara mutlak değer basar; alış fişinin toplamı defterde
                // EKSİdir (TlTutari −) ve işaret düşerse alış fişi satış fişiyle aynı görünür. İşaret
                // burada yazılır (defter satırı aramasıyla aynı kalıp: "−").
                $"{g.KalemSayisi} kalem · {(g.ToplamTl < 0 ? "−" : "")}{EkranPara(g.ToplamTl, "TRY")}",
                AramaTurleri.Fis,
                null))
            .ToList();
    }

    // ── DEFTER SATIRI (hareketler) ──────────────────────────────────────────
    //
    // Düzeltme fişi (/uyum) bir defter satırına dayanır ve o satırın kimliği
    // 26 haneli bir ULID'dir; kullanıcıdan ELLE yazması isteniyordu (B-25).
    // Hareket ISubeVarlik'tir → P-04 global süzgeci uygulanır; `sube` DARALTIR.
    private async Task<List<AramaSonucu>> HareketAra(string q, int al, int atla, string? sube)
    {
        var temel = db.Hareketler.AsNoTracking();

        // [P-04 sınıf b] kullanıcı seçimli daraltma — yetki süzgeci DEĞİL.
        if (sube is not null) temel = temel.Where(h => h.SubeId == sube);

        var satirlar = await temel
            .Where(h => (h.Aciklama != null && h.Aciklama.Contains(q)) || h.Id.Contains(q))
            .OrderByDescending(h => h.Tarih)
            .Skip(atla).Take(al)
            .Select(h => new { h.Id, h.Tur, h.Aciklama, h.Tarih, h.TlTutari, h.ParaBirimi })
            .ToListAsync();

        return satirlar.Select(h => new AramaSonucu(
            h.Id,
            // ⚠ UX-S31: ham tür kodu tek başına BASILMAZ; açıklama varsa o gelir.
            string.IsNullOrWhiteSpace(h.Aciklama) ? $"{Zaman.UtcdenYerele(h.Tarih):dd.MM.yyyy}" : h.Aciklama,
            $"{Zaman.UtcdenYerele(h.Tarih):dd.MM.yyyy} · {h.TlTutari.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {h.ParaBirimi}",
            AramaTurleri.Hareket,
            null)).ToList();
    }

    // BELGE-KALAN tur 2 (doğrulayıcı ORTA): arama sonucundaki GÜN işletme günüdür (Europe/Istanbul).
    //   Kayıt anı UTC; 00:00–03:00 TSI belgesi "{Tarih:dd.MM.yyyy}" ile bir gün geri basılıyordu.
    //   Bu dosyadaki beş tarih basımı da Zaman.UtcdenYerele üzerinden (fatura listesi ve çekmeceyle aynı gün).
    // ── FATURA ──────────────────────────────────────────────────────────────
    private async Task<List<AramaSonucu>> FaturaAra(string q, int al, int atla)
    {
        var faturalar = await db.Faturalar.AsNoTracking()
            // P-22: numarası olmayan (TASLAK) belge numaradan ARANAMAZ.
            .Where(f => f.FaturaNo != null && f.FaturaNo.Contains(q))
            .OrderByDescending(f => f.Tarih)
            .Skip(atla).Take(al)
            .Select(f => new { f.Id, f.FaturaNo, f.Tarih, f.Toplam, f.ParaBirimi })
            .ToListAsync();

        return faturalar.Select(f => new AramaSonucu(
            f.Id,
            f.FaturaNo ?? "",
            // UX-S32: tutar para birimiyle birlikte taşınır, "varsayılan TL" sayılmaz.
            // ⚠ UX-S32: tutar para birimiyle birlikte taşınır.
            // ⚠ UX-S31: ham durum kodu BASILMAZ; ayırt edici olan tarih + tutardır.
            $"{Zaman.UtcdenYerele(f.Tarih):dd.MM.yyyy} · {f.Toplam.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {f.ParaBirimi}",
            AramaTurleri.Fatura,
            null)).ToList();
    }

    // ── BANKA HAREKETİ (dekont) ─────────────────────────────────────────────
    // ZO-ISLEM-A 28.09 (tasarım hakemi KRİTİK): POS eşleştirme alanının etiketi
    // "Bankaya yatan tutar" iken tutar hiç ARANMIYORDU ("103" → sonuç yok) ve alt
    // bilgi GELEN parayı "-103439.43 TRY" diye gösteriyordu (yön "GIRIS" ile
    // karşılaştırılıyordu; banka yönü GELEN/GIDEN'dir). Sayısal sorgu artık tutarın
    // BAŞINDAN eşleşir (yazdıkça daralır; "103.439,43" / "103439,43" / "103439.43"
    // aynı sonucu verir) ve metin aramasıyla VEYA'lanır; alt bilgi ekran biçimindedir.
    private async Task<List<AramaSonucu>> BankaHareketAra(string q, int al, int atla)
    {
        // 28.09 (doğrulayıcı DÜŞÜK): "1.250" iki türlü okunabilir (binlik 1250 ya da
        // ondalık 1,25) — belirsiz yazımda İKİ yorum da aranır (VEYA).
        var onekler = TutarOnekleri(q);
        var tutarDeseni = onekler.Count > 0 ? onekler[0] + "%" : null;
        var tutarDeseni2 = onekler.Count > 1 ? onekler[1] + "%" : null;
        var hareketler = await db.BankaHareketleri.AsNoTracking()
            .Where(h => (h.KarsiTarafAd != null && h.KarsiTarafAd.Contains(q))
                     || (h.BankaReferans != null && h.BankaReferans.Contains(q))
                     || (h.Aciklama != null && h.Aciklama.Contains(q))
                     || (tutarDeseni != null && EF.Functions.Like(h.Tutar.ToString(), tutarDeseni))
                     || (tutarDeseni2 != null && EF.Functions.Like(h.Tutar.ToString(), tutarDeseni2)))
            .OrderByDescending(h => h.Tarih)
            .Skip(atla).Take(al)
            .Select(h => new
            {
                h.Id, h.KarsiTarafAd, h.Yon, h.Tutar, h.ParaBirimi, h.Tarih, h.BankaReferans,
            })
            .ToListAsync();

        return hareketler.Select(h => new AramaSonucu(
            h.Id,
            h.KarsiTarafAd ?? h.BankaReferans ?? "—",
            // ⚠ UX-S31/S32: ham yön kodu BASILMAZ; işaret tutarın önünde taşınır
            //   (GELEN "+", GIDEN "−"); tutar ekran biçiminde, para işaretiyle.
            $"{Zaman.UtcdenYerele(h.Tarih):dd.MM.yyyy} · {(h.Yon == "GELEN" ? "+" : "−")}{EkranPara(h.Tutar, h.ParaBirimi)}",
            AramaTurleri.BankaHareket,
            null)).ToList();
    }

    /// <summary>
    /// ZO-ISLEM-A 28.09 — kullanıcının yazdığı tutarı veritabanı biçimine çevirir
    /// ("103.439,43" → "103439.43", "1.250" → "1250", "103" → "103"). Sayı değilse
    /// null döner (o zaman yalnız metin araması çalışır). Başa-eşleşmede kullanılır.
    /// </summary>
    public static string? TutarOnekiCoz(string q)
    {
        var s = q.Replace(" ", "").Replace(" ", "").Replace("₺", "")
                 .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                 .TrimStart('+', '-', '−');
        if (s.Length == 0 || !char.IsAsciiDigit(s[0])
            || !s.All(c => char.IsAsciiDigit(c) || c == '.' || c == ',')) return null;
        if (s.Contains(','))
        {
            if (s.Count(c => c == ',') > 1) return null;
            // Virgüllü yazımda noktalar YALNIZ geçerli binlik gruplaması olabilir ("1.2,3" → null).
            var tamKisim = s[..s.IndexOf(',')];
            if (tamKisim.Contains('.')
                && !System.Text.RegularExpressions.Regex.IsMatch(tamKisim, @"^[1-9]\d{0,2}(\.\d{3})+$")) return null;
            s = s.Replace(".", "").Replace(',', '.');
        }
        // 28.09 (3. doğrulama DÜŞÜK): binlik grubu 0 ile başlayamaz ("0.050" binlik değil,
        // "0050" anlamsız önek üretiyordu) → o yazım ondalık yoruma kalır.
        else if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[1-9]\d{0,2}(\.\d{3})+$"))
        {
            s = s.Replace(".", "");
        }
        else if (s.Count(c => c == '.') > 1)
        {
            return null;
        }
        return s;
    }

    /// <summary>
    /// 28.09 — aranacak tutar önekleri. İlki <see cref="TutarOnekiCoz"/> yorumudur; yazım
    /// TEK noktalı binlik gruplamasıysa ("1.250") ondalık yorumu ("1.250" = 1,25) ikinci
    /// önek olarak eklenir. Birden çok noktalı ("1.250.000") ya da virgüllü yazım belirsiz
    /// değildir.
    /// </summary>
    public static IReadOnlyList<string> TutarOnekleri(string q)
    {
        var birincil = TutarOnekiCoz(q);
        if (birincil is null) return [];
        var s = q.Replace(" ", "").Replace("\u00A0", "").Replace("₺", "")
                 .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                 .TrimStart('+', '-', '−');
        // Tutar kolonu 2 ondalıklıdır (veritabanı metni "1.25"): ondalık yorumun 3. hanesi
        // 0 değilse ("1.255") öyle bir tutar olamaz → ikinci önek eklenmez.
        // "0.050": binlik yorumu yok (grup 0 ile başlamaz) → tek önek, ondalık "0.05".
        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^0\.\d{2}0$")) return [s[..^1]];
        return System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{1,3}\.\d{2}0$")
            ? [birincil, s[..^1]]
            : [birincil];
    }

    /// <summary>
    /// Ekran para biçimi — Web'deki <c>Bicim.Para</c> ile AYNI kural (tr-TR, simge
    /// sonda, bölünmez boşluk, TRY'de kuruş sıfırsa yazılmaz). Sunucu Web projesine
    /// bağlı olmadığı için burada tekrarlanır; ZOISLEMAAramaTestleri iki çıktıyı
    /// birebir karşılaştırır (sapma = kırmızı test).
    /// </summary>
    /// <summary>
    /// KATMAN-1b (tasarım hakemi 29.09 KRİTİK): gram ekran biçimi — tr-TR, gereksiz sıfır yok
    /// (4,1 g · 1,75 g · 1.250 g). Palet alt bilgisi eskiden InvariantCulture ile "4.1 g" basıyordu.
    /// </summary>
    public static string EkranGram(decimal n) =>
        n.ToString("#,##0.###", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " g";

    /// <summary>
    /// KATMAN-1b (tasarım hakemi 29.09 KRİTİK): telefon ekran biçimi. 0 ile başlayan 11 haneli
    /// Türkiye numarası "0532 000 01 04" kalıbında; +90/90 önekli 12 hane aynı kalıba iner;
    /// 10 hane (baştaki 0 yazılmamış) başa 0 alır. Kalıba uymayan değer (yurt dışı, dahili)
    /// DEĞİŞTİRİLMEDEN döner — tahminle biçim uydurulmaz. Arama yine ham değer üzerinden yapılır.
    /// </summary>
    public static string? EkranTelefon(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return tel;
        var rakam = new string(tel.Where(char.IsAsciiDigit).ToArray());
        if (rakam.Length == 12 && rakam.StartsWith("90", StringComparison.Ordinal)) rakam = "0" + rakam[2..];
        else if (rakam.Length == 10 && rakam[0] != '0') rakam = "0" + rakam;
        if (rakam.Length != 11 || rakam[0] != '0') return tel.Trim();
        return $"{rakam[..4]} {rakam[4..7]} {rakam[7..9]} {rakam[9..]}";
    }

    public static string EkranPara(decimal n, string? pb)
    {
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        var y = decimal.Round(Math.Abs(n), 2, MidpointRounding.AwayFromZero);
        var kalip = y != decimal.Truncate(y) ? "N2" : "N0";
        return pb switch
        {
            null or "" or "TRY" => y.ToString(kalip, tr) + " ₺",
            "USD" => y.ToString("N2", tr) + " $",
            "EUR" => y.ToString("N2", tr) + " €",
            "GBP" => y.ToString("N2", tr) + " £",
            _ => y.ToString("N2", tr) + " " + pb,
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/arama")]
public sealed class AramaController(AramaServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>
    /// GET /api/v1/arama — kabuk araması / kayıt seçici kaynağı.
    /// <para>
    /// Sınıf düzeyinde <c>[ModulYetkisi]</c> KULLANILMAZ: uç birden çok modülü
    /// tarar ve yetki TÜR BAŞINA servis içinde sorulur (bkz. AramaServisi).
    /// Tek tür isteyip yetkisi olmayan 403 alır; genel aramada yetkisiz tür
    /// hiç taranmaz.
    /// </para>
    /// </summary>
    [HttpGet]
    [HerkeseAcik("Yetki TÜR BAŞINA AramaServisi içinde YetkiServisi.ErisebilirMi ile sorulur; "
                 + "kiracı/şube kapsamı P-04 global sorgu filtresindedir")]
    public Task<Kuyumcu.Contracts.Arama.AramaYaniti> Ara(
        [FromQuery] string? q,
        [FromQuery] string? tur,
        [FromQuery] int? limit,
        [FromQuery] int? ofset,
        [FromQuery] string? sube)
        => servis.Ara(q, tur, limit, ofset, sube, Islem);
}
