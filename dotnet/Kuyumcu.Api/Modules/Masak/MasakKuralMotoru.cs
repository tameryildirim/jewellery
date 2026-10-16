using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK eşik motoru — PLAN Adım 4.
//
// BİLİNÇLİ OLARAK SAF: veritabanı, ağ, saat okuma yok. Girdi verilir, karar
// alınır. Sebebi: bu kuralların yanlış çalışması kuyumcuya CEZA yazdırır;
// her eşik sınırı gerçek kur çekmeden, veritabanı kurmadan test edilebilmeli.
//
// EŞİKLER YAPILANDIRMADAN OKUNUR (appsettings → Masak:*). Rakamlar mevzuatla
// değişir; değiştiğinde KOD DEĞİŞMEMELİ.
//
// FAIL-CLOSED: eksik kimlik/beyan "uyarı verip geçme" değil, işlemi DURDURMA
// sebebidir. Uyarıp geçen bir sistem kuyumcuyu korumaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Mevzuat eşikleri. Varsayılanlar PLAN §1'de doğrulanan 2026 değerleridir.</summary>
public sealed class MasakEsikleri
{
    /// <summary>
    /// Kimlik tespiti zorunluluğu başlangıcı — Tedbirler Yönetmeliği md.5/1-b.
    /// PAKET MASAK-370: 07.10.2026'dan itibaren 370.000 TL (CK 11845, RG 07.10.2026/33393,
    /// "yayımı tarihinde yürürlüğe girer"). İşlem anındaki değer TARİHLİ yasal_parametreler
    /// tablosundan okunur (06.10.2026 ve öncesi 185.000); bu varsayılan yalnız veritabanısız
    /// kurulum ve açılış eşitlik denetimi (appsettings Masak:KimlikEsigi) içindir.
    /// </summary>
    public decimal KimlikEsigi { get; init; } = 370_000m;

    /// <summary>İşlem amacı + açıklama zorunluluğu başlangıcı.</summary>
    public decimal AmacBeyaniEsigi { get; init; } = 200_000m;

    /// <summary>
    /// PAKET MASAK-370 — programın KİMLİK İSTEDİĞİ en düşük tutar. Kimlik tespiti hem
    /// <see cref="KimlikEsigi"/> (md.5/1-b) hem de <see cref="AmacBeyaniEsigi"/> kademesinde
    /// (TEYİTSİZ 200.000 — kurucu kararı md.15 (a): mali müşavir cevabına kadar aynen) zorunludur.
    /// 07.10.2026'dan sonra kimlik eşiği (370.000) amaç beyanı eşiğinin (200.000) ÜSTÜNE çıktığı
    /// için kimliği fiilen 200.000 tetikler. Mesajlar ve Peşin Müşteri kapısı bu tutarı söyler;
    /// aksi hâlde "370.000 TL ve üstü işlemde kimlik zorunludur" diye 250.000 TL'lik işlem reddedilirdi.
    /// Hesaplanan değerdir — yapılandırmadan okunmaz, tabloya yazılmaz.
    /// </summary>
    public decimal KimlikIstenenEnDusukTutar => Math.Min(KimlikEsigi, AmacBeyaniEsigi);

    /// <summary>Nakit işlem beyan formu zorunluluğu başlangıcı.</summary>
    public decimal BeyanFormuEsigi { get; init; } = 2_000_000.01m;

    /// <summary>Ek belge zorunluluğu başlangıcı.</summary>
    public decimal EkBelgeEsigi { get; init; } = 20_000_000.01m;

    /// <summary>Açıklamanın en az uzunluğu (mevzuat: 20 karakter).</summary>
    /// <summary>İşlem amacı BEYANI açıklamasının asgari uzunluğu.</summary>
    public int AsgariAciklamaUzunlugu { get; init; } = 20;

    /// <summary>
    /// ŞÜPHELİ İŞLEM BİLDİRİMİ gerekçesinin asgari uzunluğu — MASAK Rehber 2.0 §4.2.7.
    /// <para>
    /// ⚠ Bu, <see cref="AsgariAciklamaUzunlugu"/> ile AYNI ŞEY DEĞİLDİR:
    /// biri işlem amacı beyanı (beyan formu), diğeri şüpheli işlem bildirimi
    /// gerekçesidir. Farklı mevzuat maddelerine dayanır, farklı sınırları vardır.
    /// </para>
    /// <para>
    /// 2026-09-13'te canlı doğrulamada yakalandı: bu değer <c>MasakServisi</c>
    /// içinde <b>koda gömülü sabitti</b> (300) ve ADR-0005'i ("mevzuat eşikleri
    /// koda gömülmez") ihlal ediyordu. Rehber sürümü değişirse kod değişmemeli.
    /// </para>
    /// </summary>
    public int SupheliGerekceAsgari { get; init; } = 300;

    /// <summary>ŞİB gerekçesinin azami uzunluğu — MASAK Rehber 2.0 §4.2.7.</summary>
    public int SupheliGerekceAzami { get; init; } = 50_000;

    /// <summary>Şüpheli işlem bildirim süresi — İŞ GÜNÜ.</summary>
    public int SupheliBildirimIsGunu { get; init; } = 10;

    /// <summary>Belge saklama süresi — yıl.</summary>
    public int SaklamaYili { get; init; } = 8;

    /// <summary>
    /// PAKET YASAL-3 (§ 4.2) — Peşin Müşteri ART ARDA fiş penceresi (dakika).
    /// ⚠ MEVZUAT DEĞİL, POLİTİKA/SİNYAL (şartname § 3.2 T-4): aynı kullanıcı + aynı
    /// şubenin bu süre içindeki Peşin Müşteri işlemleri toplanır; toplam MASAK
    /// kimlik eşiğine ya da tevsik haddine ulaşırsa ekran "aynı müşteri mi?" diye
    /// sorar. Sahada ölçülerek ayarlanır (kurucu K-5). 0 = soru KAPALI.
    /// </summary>
    public int PesinPencereDakika { get; init; } = 60;
}

/// <summary>Bir işlem için MASAK'ın ne istediği.</summary>
public sealed record MasakGereksinim(
    string Kademe,
    bool KimlikGerekli,
    bool AmacGerekli,
    bool BeyanFormuGerekli,
    bool EkBelgeGerekli,
    int AsgariAciklamaUzunlugu);

/// <summary>Doğrulama sonucu. <see cref="Uygun"/> false ise işlem KAPANMAZ.</summary>
public sealed record MasakDogrulama(bool Uygun, IReadOnlyList<string> Eksikler)
{
    public static readonly MasakDogrulama Tamam = new(true, []);
}

/// <summary>Doğrulamaya girecek, o an elde olan bilgiler.</summary>
public sealed record MasakGirdi
{
    public decimal IslemTutariTl { get; init; }
    public bool KimlikTespitiVar { get; init; }
    public string? BelgeTuru { get; init; }
    public string? BelgeNo { get; init; }
    public string? AdSoyad { get; init; }
    public string? IslemAmaci { get; init; }
    public string? Aciklama { get; init; }
    public string? FonKaynagi { get; init; }
    public string? EkBelgeReferansi { get; init; }
}

public sealed class MasakKuralMotoru(MasakEsikleri esikler, YasalParametreServisi? yasal = null)
{
    /// <summary>
    /// PAKET YASAL-1: BUGÜNÜN (yerel gün) eşikleri — tarihli tablodan; tablo
    /// tanımsızsa 503 (fail-closed). Veritabanısız kurulumda yapılandırma nesnesi.
    /// </summary>
    public MasakEsikleri Esikler => yasal is null ? esikler : EsiklerGunu(yasal.Bugun);

    /// <summary>
    /// PAKET YASAL-1: VERİLEN YEREL GÜNÜN eşikleri. Mevzuat rakamları tablodan;
    /// açıklama uzunlukları (rehber/politika) yapılandırmada kalır.
    /// </summary>
    public MasakEsikleri EsiklerGunu(DateOnly gun) => yasal is null
        ? esikler
        : new MasakEsikleri
        {
            KimlikEsigi = yasal.Sayi(YasalAnahtar.MasakKimlikEsigiTl, gun),
            AmacBeyaniEsigi = yasal.Sayi(YasalAnahtar.MasakAmacBeyaniEsigiTl, gun),
            BeyanFormuEsigi = yasal.Sayi(YasalAnahtar.MasakBeyanFormuEsigiTl, gun),
            EkBelgeEsigi = yasal.Sayi(YasalAnahtar.MasakEkBelgeEsigiTl, gun),
            SupheliBildirimIsGunu = (int)yasal.Sayi(YasalAnahtar.MasakSibIsGunu, gun),
            SaklamaYili = (int)yasal.Sayi(YasalAnahtar.MasakSaklamaYil, gun),
            AsgariAciklamaUzunlugu = esikler.AsgariAciklamaUzunlugu,
            SupheliGerekceAsgari = esikler.SupheliGerekceAsgari,
            SupheliGerekceAzami = esikler.SupheliGerekceAzami,
            PesinPencereDakika = esikler.PesinPencereDakika,   // YASAL-3: politika, tabloda değil
        };

    /// <summary>Tutara göre hangi kademeye girdiğini söyler (bugünün eşikleriyle).</summary>
    public MasakGereksinim Gereksinim(decimal tutarTl) => Gereksinim(tutarTl, Esikler);

    /// <summary>PAKET YASAL-1: verilen yerel günün eşikleriyle kademe.</summary>
    public MasakGereksinim Gereksinim(decimal tutarTl, DateOnly gun) => Gereksinim(tutarTl, EsiklerGunu(gun));

    private static MasakGereksinim Gereksinim(decimal tutarTl, MasakEsikleri e)
    {
        var t = Math.Abs(tutarTl);   // alış da satış da eşiğe tabidir

        if (t >= e.EkBelgeEsigi)
            return new(BeyanKademesi.BeyanFormuVeBelge, true, true, true, true, e.AsgariAciklamaUzunlugu);

        if (t >= e.BeyanFormuEsigi)
            return new(BeyanKademesi.BeyanFormu, true, true, true, false, e.AsgariAciklamaUzunlugu);

        if (t >= e.AmacBeyaniEsigi)
            return new(BeyanKademesi.AmacBeyani, true, true, false, false, e.AsgariAciklamaUzunlugu);

        if (t >= e.KimlikEsigi)
            return new(BeyanKademesi.YalnizKimlik, true, false, false, false, 0);

        return new(BeyanKademesi.Yok, false, false, false, false, 0);
    }

    /// <summary>
    /// İşlemin MASAK açısından kapanabilir olup olmadığı.
    /// Eksik varsa TÜM eksikler birden döner — kuyumcu tek tek denemesin.
    /// </summary>
    public MasakDogrulama Dogrula(MasakGirdi girdi) => Dogrula(girdi, Esikler);

    /// <summary>PAKET YASAL-1: verilen yerel günün eşikleriyle doğrulama.</summary>
    public MasakDogrulama Dogrula(MasakGirdi girdi, DateOnly gun) => Dogrula(girdi, EsiklerGunu(gun));

    private static MasakDogrulama Dogrula(MasakGirdi girdi, MasakEsikleri e)
    {
        var gerek = Gereksinim(girdi.IslemTutariTl, e);
        if (gerek.Kademe == BeyanKademesi.Yok) return MasakDogrulama.Tamam;

        var eksikler = new List<string>();

        if (gerek.KimlikGerekli)
        {
            if (!girdi.KimlikTespitiVar)
                // PAKET MASAK-370: tutar kimlik eşiğinin ALTINDA ama amaç beyanı kademesindeyse
                // (07.10.2026 sonrası 200.000–369.999,99) mesaj gerçek tetikleyici eşiği söyler.
                eksikler.Add(Math.Abs(girdi.IslemTutariTl) >= e.KimlikEsigi
                    ? $"{e.KimlikEsigi:N0} TL ve üstü işlemde kimlik tespiti zorunludur"
                    : $"{e.AmacBeyaniEsigi:N0} TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur");
            else
            {
                if (!KimlikBelgeTuru.Gecerli(girdi.BelgeTuru))
                    // UX-4d madde 3: mesaj KABUL EDİLEN DEĞERLERİ sayar. Eskiden
                    // "Geçerli bir kimlik belgesi türü seçilmelidir" diyordu ve
                    // kuyumcu hangi değerlerin geçerli olduğunu hiçbir yerden
                    // öğrenemiyordu (QA turu 1 fikstür bulgusu).
                    eksikler.Add(Kuyumcu.Core.Domain.KimlikBelgesi.Mesaj);
                if (string.IsNullOrWhiteSpace(girdi.BelgeNo))
                    eksikler.Add("Kimlik belge numarası zorunludur");
                if (string.IsNullOrWhiteSpace(girdi.AdSoyad))
                    eksikler.Add("Ad soyad zorunludur");
            }
        }

        if (gerek.AmacGerekli)
        {
            if (string.IsNullOrWhiteSpace(girdi.IslemAmaci))
                eksikler.Add("İşlem amacı belirtilmelidir");

            var aciklama = girdi.Aciklama?.Trim() ?? "";
            if (aciklama.Length < gerek.AsgariAciklamaUzunlugu)
                // UX-4d madde 2: mesaj HANGİ ALANI kastettiğini söyler. QA turu 1'de
                // gönderilen DTO'nun kendi `aciklama` alanı 47 karakterdi ama kural
                // `masak.aciklama` alanına bakıyordu; "şu an 0" mesajı kuyumcuya
                // dolu bir alanın boş sayıldığını düşündürüyordu.
                eksikler.Add(
                    $"MASAK beyan açıklaması (masak.aciklama) en az {gerek.AsgariAciklamaUzunlugu} "
                    + $"karakter olmalıdır (şu an {aciklama.Length})");
        }

        if (gerek.BeyanFormuGerekli && string.IsNullOrWhiteSpace(girdi.FonKaynagi))
            eksikler.Add("Nakit işlem beyan formu için fon kaynağı zorunludur");

        if (gerek.EkBelgeGerekli && string.IsNullOrWhiteSpace(girdi.EkBelgeReferansi))
            eksikler.Add($"{e.EkBelgeEsigi:N0} TL üstü işlemde ek belge referansı zorunludur");

        return eksikler.Count == 0 ? MasakDogrulama.Tamam : new MasakDogrulama(false, eksikler);
    }

    /// <summary>
    /// Şüpheli işlem bildiriminin son tarihi: tespitten itibaren 10 İŞ GÜNÜ.
    /// Hafta sonu sayılmaz. <paramref name="resmiTatiller"/> verilirse onlar da düşülür.
    /// </summary>
    public DateTime SonBildirimTarihi(DateTime tespitUtc, IReadOnlyCollection<DateOnly>? resmiTatiller = null)
    {
        // İş günü sayımı YEREL takvimde yapılır; UTC'de gün sınırı kayarsa
        // bir günlük hata yasal süreyi kaçırtabilir.
        var yerel = Zaman.UtcdenYerele(tespitUtc);
        var gun = DateOnly.FromDateTime(yerel);
        // PAKET YASAL-1: süre TESPİT GÜNÜNÜN tablosundan.
        var kalan = yasal is null ? esikler.SupheliBildirimIsGunu
            : (int)yasal.Sayi(YasalAnahtar.MasakSibIsGunu, gun);

        while (kalan > 0)
        {
            gun = gun.AddDays(1);
            if (gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (resmiTatiller?.Contains(gun) == true) continue;
            kalan--;
        }

        // Son günün mesai bitimi değil, gün sonu kabul edilir (lehte yorum).
        return Zaman.YerelGunSonuUtc(gun);
    }

    /// <summary>Saklama bitişi: işlem tarihi + saklama yılı (KAYIT GÜNÜNÜN tablosundan — PAKET YASAL-1).</summary>
    public DateTime SaklamaBitisi(DateTime tarihUtc) =>
        tarihUtc.AddYears(yasal is null ? esikler.SaklamaYili
            : (int)yasal.Sayi(YasalAnahtar.MasakSaklamaYil, YasalParametreServisi.IslemGunu(tarihUtc)));
}
