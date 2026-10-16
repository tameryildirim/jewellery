using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Iceaktarim;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIMI — paket 47 Faz A (SEÇENEK 2: kapılı içe aktarım)
//
// ⚠ ADR-0014'ün "geri alma (import) yoktur, bu bilinçlidir" cümlesi bir GÜVENLİK
//   gerekçesiydi. Bu modül o cümleyi kaldırmaz, KOŞULA BAĞLAR. Koşullar
//   (Mehmet 06 Güvenlik + Bertan 02 Mimari ortak kararı):
//     1. YALNIZ KART: cari ve ürün kartı. Geçmiş hareket/fatura/kasa AKTARILMAZ
//        (seçenek 3 VETO: tevsik günlük nakit kümülatifi ve MASAK bağlantılı
//        işlem kümülatifi geriye dönük anlamsızlaşır, kur_snapshot_id uydurulur,
//        denetim izi YALAN SÖYLER — Anayasa md.1).
//     2. KURU PROVA ZORUNLU: ilk çağrı hiçbir hedef tabloya yazmaz.
//     3. MEVCUT SERVİS YOLU: kendi Add'i yoktur (denetim.sh ile zorlanır).
//     4. TEK TRANSACTION: kısmi göç yoktur; bir satır patlarsa hepsi geri alınır.
//     5. GERİ ALINABİLİR: oturumda oluşan kartlar PASİFE alınır (soft delete).
//     6. SAHİP + gerekçe (≥20) + oran sınırı + denetim izi (hassas).
//     7. SAHNE TABLOSU MASKELİ: ham_json'da kimlik alanı düz metin durmaz.
//
// ⚠ GUNCELLE POLİTİKASI FAZ A'DA YOKTUR: mevcut kartı ezmek geri almayı
//   imkânsızlaştırır. Yinelenen satır ya HATA'dır ya ATLA'nır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class IceAktarimProvaDto
{
    /// <summary>Faz A: "cari" | "urun".</summary>
    [Required(ErrorMessage = "Kaynak zorunludur"), MaxLength(30)]
    public string Kaynak { get; set; } = null!;

    [Required(ErrorMessage = "Dosya adı zorunludur"), MaxLength(255)]
    public string DosyaAdi { get; set; } = null!;

    /// <summary>CSV içeriği base64 olarak (multipart altyapısı yok — paket 4 deseni).</summary>
    [Required(ErrorMessage = "Dosya içeriği zorunludur")]
    public string IcerikBase64 { get; set; } = null!;

    [Required(ErrorMessage = "Gerekçe zorunludur"), MaxLength(500)]
    public string Gerekce { get; set; } = null!;

    /// <summary>HATA (varsayılan, fail-closed) veya ATLA.</summary>
    [MaxLength(10)] public string? YinelenenPolitikasi { get; set; }

    /// <summary>
    /// GOC-47B — SÜTUN EŞLEŞTİRME: { bizimKolon: dosyadakiBaslik }. Kullanıcı kendi
    /// CSV başlıklarını bizim alanlara ekrandan eşler (otomatik tahmin YOK — KVKK,
    /// yapay zekâ/dış servis kullanılmaz). Verilmeyen kolon adıyla aranır.
    /// </summary>
    public Dictionary<string, string>? KolonEslemesi { get; set; }
}

public sealed class IceAktarimUygulaDto
{
    /// <summary>Onay metni BİREBİR yazılmalıdır.</summary>
    [Required(ErrorMessage = "Onay metni zorunludur"), MaxLength(100)]
    public string Onay { get; set; } = null!;
}

/// <summary>
/// PATCH sözleşmesi: gövdede OLMAYAN alan KORUNUR, açıkça null gönderilen
/// opsiyonel alan TEMİZLENİR. Gerekçe zorunlu olduğu için null reddedilir.
/// </summary>
public sealed class IceAktarimGuncelleDto
{
    private string? _gerekce;
    private string? _yinelenenPolitikasi;

    [MaxLength(500)]
    public string? Gerekce { get => _gerekce; set { _gerekce = value; GerekceVerildi = true; } }
    [JsonIgnore] public bool GerekceVerildi { get; private set; }

    [MaxLength(10)]
    public string? YinelenenPolitikasi
    {
        get => _yinelenenPolitikasi;
        set { _yinelenenPolitikasi = value; YinelenenPolitikasiVerildi = true; }
    }
    [JsonIgnore] public bool YinelenenPolitikasiVerildi { get; private set; }
}

public sealed class IceAktarimServisi(
    KuyumcuDbContext db,
    DenetimServisi denetim,
    Cari.CariServisi cari,
    Urun.UrunServisi urun,
    IceAktarimAyarlari? ayarlar = null,
    Kurulum.KurulumServisi? kurulumServisi = null)
{
    private readonly IceAktarimAyarlari ayar = ayarlar ?? new IceAktarimAyarlari();
    private readonly IceAktarimServisleri servisler = new(cari, urun);

    // GOC-47B: açılış kapıları (mühür + açılış penceresi) T-2 ile ORTAKTIR —
    // CSV yolu mühürlenmiş bir kurulumun arka kapısı olamaz. DI vermezse kapı
    // yine ÇALIŞIR (fail-closed): varsayılan yapılandırmayla kurulur.
    private readonly Kurulum.KurulumServisi kurulum = kurulumServisi
        ?? new Kurulum.KurulumServisi(db, denetim,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Kurulum.KurulumServisi>.Instance);
    private readonly Kurulum.AcilisYazici yazici = new(db, cari);

    public const string DenetimVarligi = "IceAktarim";

    public IceAktarimAyarlari Ayarlar => ayar;

    // ── 1) Şablonlar ─────────────────────────────────────────────────────────

    public object Sablonlar() => new
    {
        onayMetni = ayar.UygulaOnayMetni,
        maksSatir = ayar.MaksSatir,
        maksDosyaBayt = ayar.MaksDosyaBayt,
        gerekceAsgari = ayar.GerekceAsgari,
        politikalar = Data.YinelenenPolitikasi.Tumu,
        kaynaklar = Kaynaklar.Hepsi.Select(k => new
        {
            ad = k.Ad,
            // GOC-47B: deftere yazan (açılış) kaynak mı? Ekran geri alma metnini buna göre seçer.
            defter = k is IDefterKaynagi,
            kolonlar = k.Kolonlar.Select(c => new { ad = c.Ad, zorunlu = c.Zorunlu, ornek = c.Ornek }),
        }),
    };

    /// <summary>Boş şablon: UTF-8 BOM + başlık satırı + tek örnek satır.</summary>
    public string SablonCsv(string kaynakAd)
    {
        var kaynak = Bul(kaynakAd);
        var basliklar = kaynak.Kolonlar.Select(k => k.Ad);
        var ornek = kaynak.Kolonlar.Select(k => (string?)k.Ornek);
        return Disaaktarim.CsvYazici.Satir(basliklar) + Disaaktarim.CsvYazici.Satir(ornek);
    }

    // ── 2) Kuru prova — HİÇBİR HEDEF TABLOYA YAZMAZ ──────────────────────────

    public async Task<object> Prova(IceAktarimProvaDto dto, AktifKullanici islem, string? ip = null)
    {
        var gerekce = (dto.Gerekce ?? "").Trim();
        if (gerekce.Length < ayar.GerekceAsgari)
            throw new BadRequestException(
                $"İçe aktarım gerekçesi en az {ayar.GerekceAsgari} karakter olmalıdır. " +
                "Bu metin denetim izine yazılır ve silinemez.");

        var kaynak = Bul(dto.Kaynak);

        var politika = string.IsNullOrWhiteSpace(dto.YinelenenPolitikasi)
            ? Data.YinelenenPolitikasi.Hata
            : dto.YinelenenPolitikasi.Trim().ToUpperInvariant();
        if (!Data.YinelenenPolitikasi.Tumu.Contains(politika))
            throw new BadRequestException(
                $"Geçersiz yinelenen politikası: '{politika}'. " +
                $"Geçerli değerler: {string.Join(", ", Data.YinelenenPolitikasi.Tumu)} " +
                "(GUNCELLE Faz A'da sunulmaz — geri almayı imkânsızlaştırır).");

        // Dosya kapısı: ikili imza + NUL + katı UTF-8. Geçemezse oturum AÇILMAZ.
        var metin = IceAktarimKapisi.MetneCevir(dto.IcerikBase64, ayar, out var hamBayt);
        var tablo = CsvOkuyucu.Ayristir(metin);

        // GOC-47B: sütun eşleştirme — dosyanın başlıkları bizim kolon adlarına çevrilir.
        var basliklar = KolonlariEsle(tablo.Basliklar, dto.KolonEslemesi, kaynak);

        var (eksik, bilinmeyen) = CsvOkuyucu.BasliklariDenetle(basliklar, kaynak.Kolonlar);
        if (eksik.Length > 0)
            throw new BadRequestException(
                $"Zorunlu kolon eksik: {string.Join(", ", eksik)}. " +
                $"Şablonu indirip kolon adlarını birebir kullanın (/api/v1/ice-aktarim/sablon/{kaynak.Ad}.csv).");

        if (tablo.Basliklar.Length > ayar.MaksAlanSayisi)
            throw new BadRequestException(
                $"Dosyada {tablo.Basliklar.Length} kolon var; en fazla {ayar.MaksAlanSayisi} kolon okunur.");

        if (tablo.Satirlar.Count > ayar.MaksSatir)
            throw new BadRequestException(
                $"Dosyada {tablo.Satirlar.Count} veri satırı var; bir oturumda en fazla " +
                $"{ayar.MaksSatir} satır aktarılabilir. Dosyayı bölerek aktarın.");

        if (tablo.Satirlar.Count == 0)
            throw new BadRequestException("Dosyada veri satırı yok (yalnız başlık satırı bulundu).");

        // ── GOC-47B: defter (açılış) kaynağı — kapılar + toplu doğrulama ────
        var defter = kaynak as IDefterKaynagi;
        List<SatirSonucu>? defterSonuclari = null;
        string? ayniDosyaOturumId = null;
        if (defter is not null)
        {
            // Emanet bakiyeye girmez (karar 3): sessizce yok sayılmaz, dosya reddedilir.
            var yasak = basliklar.Where(b => defter.YasakKolonParcalari
                .Any(p => b.Contains(p, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (yasak.Length > 0)
                throw new BadRequestException(
                    $"Bu dosyada emanet kolonu var ({string.Join(", ", yasak)}). Emanet (müşterinin bizde " +
                    "duran altını) bakiyeye girmez; Emanet ekranından ayrıca girilir. Kolonu dosyadan çıkarın.");

            await DefterKapilari(islem);
            await GuncelKur(islem);   // kur yoksa oturum AÇILMAZ (400)

            var hamlar = tablo.Satirlar
                .Select(h => (IReadOnlyDictionary<string, string>)Esle(basliklar, h)).ToList();
            defterSonuclari = await defter.TopluDogrula(db, islem, hamlar);

            var sha = Ozet(hamBayt);
            ayniDosyaOturumId = await AyniDosyaUygulandi(islem, kaynak.Ad, sha);
        }

        var now = DateTime.UtcNow;
        var oturumId = Kimlik.Ulid();
        var satirlar = new List<IceAktarimSatiri>();

        // Aynı dosya içindeki yinelenenler de yakalanır — yoksa iki özdeş satır
        // veritabanına iki kart yazardı (kuru provada çakışma görünmezdi).
        var dosyaIciAnahtarlar = new HashSet<string>(StringComparer.Ordinal);

        var gecerli = 0; var hatali = 0; var atlanan = 0;

        for (var i = 0; i < tablo.Satirlar.Count; i++)
        {
            var hucreler = tablo.Satirlar[i];
            var satirNo = i + 2;   // başlık 1. satır
            var ham = Esle(basliklar, hucreler);

            var sonuc = defterSonuclari is not null ? defterSonuclari[i] : kaynak.Dogrula(ham);

            string durum; string? kod = sonuc.HataKodu; string? mesaj = sonuc.HataMesaji;

            if (defterSonuclari is not null)
            {
                // Defter kaynağında yinelenen/zaten-açılış kararı TopluDogrula'dadır;
                // ATLA politikası yalnız "zaten var" kodlarını atlar, gerisi HATA'dır.
                if (sonuc.Dto is not null) { durum = IceAktarimSatirSonucu.Gecerli; gecerli++; }
                else if (politika == Data.YinelenenPolitikasi.Atla
                         && DefterHataKodu.Atlanabilir.Contains(sonuc.HataKodu))
                { durum = IceAktarimSatirSonucu.Atlandi; atlanan++; }
                else { durum = IceAktarimSatirSonucu.Hata; hatali++; }
            }
            else if (sonuc.Dto is null)
            {
                durum = IceAktarimSatirSonucu.Hata;
                hatali++;
            }
            else
            {
                var mevcutId = await kaynak.YinelenenBul(db, islem.TenantId, sonuc.Dto);
                var dosyaAnahtari = Anahtar(kaynak, sonuc.Dto);
                var dosyaIciYinelenen = !dosyaIciAnahtarlar.Add(dosyaAnahtari);

                if (mevcutId is not null || dosyaIciYinelenen)
                {
                    if (politika == Data.YinelenenPolitikasi.Hata)
                    {
                        durum = IceAktarimSatirSonucu.Hata;
                        kod = "YINELENEN";
                        mesaj = dosyaIciYinelenen
                            ? "Bu kayıt dosyada birden fazla kez geçiyor."
                            : "Bu kayıt sistemde zaten var (yinelenen politikası: HATA).";
                        hatali++;
                    }
                    else
                    {
                        durum = IceAktarimSatirSonucu.Atlandi;
                        kod = "YINELENEN";
                        mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Zaten kayıtlı — atlandı.");
                        atlanan++;
                    }
                }
                else
                {
                    durum = IceAktarimSatirSonucu.Gecerli;
                    gecerli++;
                }
            }

            satirlar.Add(new IceAktarimSatiri
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                OturumId = oturumId,
                SatirNo = satirNo,
                HamJson = HamJsonUret(ham),
                Sonuc = durum,
                HataKodu = kod,
                HataMesaji = Kirp(mesaj, 500),
                HedefVarlikTuru = kaynak.VarlikTuru,
                Olusturuldu = now,
            });
        }

        var oturum = new IceAktarimOturumu
        {
            Id = oturumId,
            TenantId = islem.TenantId,
            KullaniciId = islem.Id,
            KaynakAd = kaynak.Ad,
            DosyaAdi = dto.DosyaAdi.Trim(),
            DosyaSha256 = Ozet(hamBayt),
            Gerekce = gerekce,
            Durum = IceAktarimDurumu.Prova,
            YinelenenPolitikasi = politika,
            ToplamSatir = tablo.Satirlar.Count,
            GecerliSatir = gecerli,
            HataliSatir = hatali,
            AtlananSatir = atlanan,
            UygulananSatir = 0,
            Olusturuldu = now,
            IpAdresi = ip,
        };

        db.Set<IceAktarimOturumu>().Add(oturum);
        db.Set<IceAktarimSatiri>().AddRange(satirlar);

        // Denetim izi: kuru prova da izdir — "kim ne yüklemeye kalktı".
        denetim.YazmaKaydet(islem, DenetimVarligi, oturumId,
            $"kuru prova · kaynak: {kaynak.Ad} · dosya: {oturum.DosyaAdi} · " +
            $"{oturum.ToplamSatir} satır ({gecerli} geçerli / {hatali} hatalı / {atlanan} atlanan) · " +
            $"politika: {politika} · gerekçe: {gerekce}");

        await db.SaveChangesAsync();

        return new
        {
            oturumId,
            kaynak = kaynak.Ad,
            durum = oturum.Durum,
            yinelenenPolitikasi = politika,
            toplamSatir = oturum.ToplamSatir,
            gecerliSatir = gecerli,
            hataliSatir = hatali,
            atlananSatir = atlanan,
            bilinmeyenKolonlar = bilinmeyen,
            onayMetni = ayar.UygulaOnayMetni,
            // ⚠ HİÇBİR HEDEF TABLOYA YAZILMADI — bu alan ekranda da yazar.
            yazilanKayit = 0,
            // GOC-47B (UX-S23): açılışın ETKİSİ uygulamadan önce görünür.
            ozet = defter?.Ozetle(defterSonuclari!.Where(s => s.Dto is not null).Select(s => s.Dto!)),
            // GOC-47B: aynı dosya (sha256) zaten uygulandıysa uyarı — ikinci uygulama 409 alır.
            ayniDosyaOturumId,
            ilkHatalar = satirlar
                .Where(s => s.Sonuc == IceAktarimSatirSonucu.Hata)
                .Take(100)
                .Select(s => new { satirNo = s.SatirNo, hataKodu = s.HataKodu, hataMesaji = s.HataMesaji }),
        };
    }

    // ── 3) Satır sonuçları (TB-19: limit zorunlu, tavan yapılandırmadan) ─────

    public async Task<object> Satirlar(
        string oturumId, string? sonuc, int limit, int offset, AktifKullanici islem)
    {
        if (limit < 1 || limit > ayar.SatirListesiTavan)
            throw new BadRequestException(
                $"limit 1 ile {ayar.SatirListesiTavan} arasında olmalıdır (TB-19: sınırsız liste yok).");

        var oturum = await OturumBul(oturumId, islem);
        var kaynak = Bul(oturum.KaynakAd);

        var sorgu = db.Set<IceAktarimSatiri>().AsNoTracking()
            .Where(s => s.TenantId == islem.TenantId && s.OturumId == oturum.Id);
        if (!string.IsNullOrWhiteSpace(sonuc))
            sorgu = sorgu.Where(s => s.Sonuc == sonuc);

        var toplam = await sorgu.CountAsync();
        var liste = await sorgu.OrderBy(s => s.SatirNo).Skip(offset).Take(limit)
            .Select(s => new
            {
                id = s.Id,
                satirNo = s.SatirNo,
                sonuc = s.Sonuc,
                hataKodu = s.HataKodu,
                hataMesaji = s.HataMesaji,
                hedefVarlikTuru = s.HedefVarlikTuru,
                hedefVarlikId = s.HedefVarlikId,
                hamJson = s.HamJson,
            })
            .ToListAsync();

        // ⚠ MASKELEME BELLEKTE: kimlik alanları ekrana DÜZ METİN gitmez.
        var maskeli = liste.Select(s => new
        {
            s.id, s.satirNo, s.sonuc, s.hataKodu, s.hataMesaji,
            s.hedefVarlikTuru, s.hedefVarlikId,
            hamJson = MaskeliGoster(kaynak, s.hamJson),
        }).ToList();

        return new
        {
            oturumId = oturum.Id,
            toplamSatir = toplam,
            limit,
            offset,
            sinirdaMi = liste.Count == limit && offset + liste.Count < toplam,
            satirlar = maskeli,
        };
    }

    // ── 4) Uygula — TEK TRANSACTION ──────────────────────────────────────────

    public async Task<object> Uygula(string oturumId, IceAktarimUygulaDto dto, AktifKullanici islem)
    {
        if ((dto.Onay ?? "").Trim() != ayar.UygulaOnayMetni)
            throw new BadRequestException(
                $"İçe aktarımı başlatmak için onay metnini birebir yazın: \"{ayar.UygulaOnayMetni}\"");

        var oturum = await OturumBul(oturumId, islem);

        if (oturum.Durum != IceAktarimDurumu.Prova)
            throw new ConflictException(
                $"Bu oturum '{oturum.Durum}' durumunda; yalnız kuru provası yapılmış (PROVA) oturum uygulanabilir.");

        var kaynak = Bul(oturum.KaynakAd);

        var uygulanacak = await db.Set<IceAktarimSatiri>()
            .Where(s => s.TenantId == islem.TenantId && s.OturumId == oturum.Id
                        && s.Sonuc == IceAktarimSatirSonucu.Gecerli)
            .OrderBy(s => s.SatirNo)
            .ToListAsync();

        // ── GOC-47B: defter kaynağının kapıları transaction'DAN ÖNCE ──────────
        // (burada reddedilen oturum IPTAL'e düşmez; sebep giderilince uygulanabilir)
        var defter = kaynak as IDefterKaynagi;
        KurSnapshot? kur = null;
        if (defter is not null)
        {
            await DefterKapilari(islem);
            if (await AyniDosyaUygulandi(islem, oturum.KaynakAd, oturum.DosyaSha256) is { } onceki)
                throw new ConflictException(
                    $"Bu dosya (içeriği birebir aynı) daha önce uygulandı. İkinci uygulama " +
                    "bakiyeyi/stoğu İKİ KEZ yazardı — reddedildi. Önceki oturumu geri aldıysanız yeniden deneyin.");
            kur = await GuncelKur(islem);
        }

        if (uygulanacak.Count == 0)
            throw new BadRequestException(
                "Uygulanacak geçerli satır yok. Hatalı satırları dosyada düzeltip yeni bir kuru prova çalıştırın.");


        // ⚠ TEK TRANSACTION: "kısmi göç en kötü hâldir". Beklenmedik bir istisna
        //   olursa TÜM satırlar geri alınır ve oturum IPTAL olur.
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // ⚠ DOSYA İKİNCİ KEZ İSTENMEZ: uygulama, provada ŞİFRELİ saklanan
            //   ham satırdan yürür (ADR-0003 dönüştürücüsü burada düz metne
            //   çözer). Böylece "provada gördüğüm satır" ile "uygulanan satır"
            //   AYNI olduğu kanıtlanabilir; ikinci yükleme araya farklı bir
            //   dosya sokulmasına izin verirdi.
            // ⚠ DOĞRULAMA YENİDEN KOŞAR: prova ile uygulama arasında kaynak
            //   kuralları değiştiyse (ör. yeni bir kapı) satır yine reddedilir.
            if (defter is not null)
            {
                // GOC-47B: toplu yeniden doğrulama (veritabanına bakan kurallar dahil:
                // zaten açılış, stok var, cari pasifleşti…). Tek satır bile düşerse
                // TÜM oturum geri alınır — kısmi göç yok.
                var hamlar = uygulanacak
                    .Select(s => (IReadOnlyDictionary<string, string>)JsonSozluk(s.HamJson)).ToList();
                var sonuclar = await defter.TopluDogrula(db, islem, hamlar);
                var simdi = DateTime.UtcNow;
                var baglam = new DefterBaglami(db, yazici, kur!, oturum.Gerekce, islem, oturum.Id, simdi);
                for (var i = 0; i < uygulanacak.Count; i++)
                {
                    var satir = uygulanacak[i];
                    if (sonuclar[i].Dto is not { } d)
                        throw new BadRequestException(
                            $"Satır {satir.SatirNo}: {sonuclar[i].HataMesaji} (kuru provadan sonra veri değişmiş olabilir)");
                    satir.HedefVarlikId = await defter.DefterUygula(baglam, d);
                    satir.Sonuc = IceAktarimSatirSonucu.Uygulandi;
                }
                await kurulum.AdimIsaretle(islem.TenantId, simdi);
            }
            else
            foreach (var satir in uygulanacak)
            {
                var ham = JsonSozluk(satir.HamJson);
                var sonuc = kaynak.Dogrula(ham);
                if (sonuc.Dto is null)
                    throw new BadRequestException(
                        $"Satır {satir.SatirNo}: {sonuc.HataMesaji} (kuru provadan sonra veri değişmiş olabilir)");

                var id = await kaynak.Uygula(servisler, sonuc.Dto, islem);
                satir.HedefVarlikId = id;
                satir.Sonuc = IceAktarimSatirSonucu.Uygulandi;
            }

            oturum.Durum = IceAktarimDurumu.Uygulandi;
            oturum.UygulananSatir = uygulanacak.Count;
            oturum.Uygulandi = DateTime.UtcNow;

            denetim.YazmaKaydet(islem, DenetimVarligi, oturum.Id,
                $"uygulandı · kaynak: {oturum.KaynakAd} · {uygulanacak.Count} kayıt oluşturuldu · " +
                (kur is null ? "" : $"açılış (tek kur: {kur.Id}) · ") +
                $"dosya: {oturum.DosyaAdi} · gerekçe: {oturum.Gerekce}");

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            // GOC-47B: geri alınan transaction'ın izlenen (eklenmiş/değişmiş) varlıkları
            // bağlamda KALIRSA aşağıdaki IPTAL yazımı aynı hatayı (ör. UNIQUE ihlali)
            // yeniden üretir ve oturum PROVA'da asılı kalırdı. Veritabanı zaten geri
            // alındı; bağlam da temizlenir.
            db.ChangeTracker.Clear();
            // Oturumu IPTAL'e çekmek AYRI bir yazmadır: geri alınan transaction'ın
            // içinde yapılsaydı iz de kaybolurdu (ADR-0004 ruhu).
            await IptalYaz(oturum.Id, islem, "uygulama sırasında hata — tüm satırlar geri alındı");
            throw;
        }

        return new
        {
            oturumId = oturum.Id,
            durum = oturum.Durum,
            uygulananSatir = oturum.UygulananSatir,
            atlananSatir = oturum.AtlananSatir,
            hataliSatir = oturum.HataliSatir,
            geriAlinabilir = true,
        };
    }

    // ── 5) Geri al — SOFT DELETE ─────────────────────────────────────────────

    public async Task<object> GeriAl(string oturumId, AktifKullanici islem)
    {
        var oturum = await OturumBul(oturumId, islem);

        if (oturum.Durum != IceAktarimDurumu.Uygulandi)
            throw new ConflictException(
                $"Bu oturum '{oturum.Durum}' durumunda; yalnız UYGULANDI durumundaki oturum geri alınabilir.");

        var kaynak = Bul(oturum.KaynakAd);

        var satirlar = await db.Set<IceAktarimSatiri>()
            .Where(s => s.TenantId == islem.TenantId && s.OturumId == oturum.Id
                        && s.Sonuc == IceAktarimSatirSonucu.Uygulandi && s.HedefVarlikId != null)
            .ToListAsync();

        if (kaynak is IDefterKaynagi defter)
            return await DefterGeriAl(oturum, defter, satirlar, islem);

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var s in satirlar)
            await kaynak.PasifeAl(servisler, s.HedefVarlikId!, islem);

        oturum.Durum = IceAktarimDurumu.GeriAlindi;
        oturum.GeriAlindi = DateTime.UtcNow;

        denetim.YazmaKaydet(islem, DenetimVarligi, oturum.Id,
            $"geri alındı · {satirlar.Count} kayıt PASİFE alındı (silinmedi) · " +
            $"kaynak: {oturum.KaynakAd} · gerekçe: {oturum.Gerekce}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            oturumId = oturum.Id,
            durum = oturum.Durum,
            pasifeAlinan = satirlar.Count,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Bu oturumda oluşan kayıtlar pasife alındı. Kayıtlar SİLİNMEDİ; " +
                    "ilgili ekrandan yeniden aktifleştirilebilir."),
        };
    }

    /// <summary>
    /// GOC-47B — açılış oturumunu TERS KAYITLA geri alır (silme yok, ADR-0004).
    /// Kurulum mühürlendiyse 409 (T-2 kapısı): artık düzeltme fişi/sayım yoludur.
    /// Tek transaction: bir kalem geri alınamazsa (ör. açılış stoğu satılmış)
    /// HİÇBİRİ geri alınmaz ve oturum UYGULANDI kalır.
    /// </summary>
    private async Task<object> DefterGeriAl(
        IceAktarimOturumu oturum, IDefterKaynagi defter, List<IceAktarimSatiri> satirlar, AktifKullanici islem)
    {
        await kurulum.MuhurDogrula(islem.TenantId);
        if (islem.SubeId is null)
            throw new BadRequestException("Geri alma kaydı için şube seçimi gerekli");
        var kur = await GuncelKur(islem);
        var simdi = DateTime.UtcNow;
        var gerekce = Kirp($"Geri alma: {oturum.Gerekce}", 500)!;
        var baglam = new DefterBaglami(db, yazici, kur, gerekce, islem, oturum.Id, simdi);

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var s in satirlar.OrderBy(x => x.SatirNo))
        {
            var dto = defter.Dogrula(JsonSozluk(s.HamJson)).Dto
                ?? throw new ConflictException($"Satır {s.SatirNo} okunamadı; geri alma yapılmadı.");
            await defter.DefterGeriAl(baglam, s.HedefVarlikId!, dto);
        }

        oturum.Durum = IceAktarimDurumu.GeriAlindi;
        oturum.GeriAlindi = simdi;
        denetim.YazmaKaydet(islem, DenetimVarligi, oturum.Id,
            $"geri alındı · {satirlar.Count} açılış kalemi TERS KAYITLA geri alındı (silinmedi) · " +
            $"kaynak: {oturum.KaynakAd} · kur: {kur.Id} · gerekçe: {oturum.Gerekce}");

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            oturumId = oturum.Id,
            durum = oturum.Durum,
            pasifeAlinan = 0,
            geriAlinan = satirlar.Count,
            mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Açılış kayıtları ters kayıtla geri alındı; bakiye ve stok önceki hâline döndü. " +
                    "Kayıtlar SİLİNMEDİ — hesap ekstresinde iki satır olarak görünür."),
        };
    }

    // ── GOC-47B yardımcıları ─────────────────────────────────────────────────

    /// <summary>T-2 açılış kapıları: mühür (409) + açılış penceresi (409) + şube.</summary>
    private async Task DefterKapilari(AktifKullanici islem)
    {
        await kurulum.MuhurDogrula(islem.TenantId);
        await kurulum.AcilisPenceresiDogrula(islem.TenantId);
        if (islem.SubeId is null)
            throw new BadRequestException("Açılış kaydı için şube seçimi gerekli (oturumdan gelir).");
    }

    /// <summary>
    /// Oturumun TEK kur fotoğrafı — kiracının en güncel kaydı. Yoksa açılış yapılmaz
    /// (satır başına kur UYDURULMAZ; yeni kiracıda kur ilk dakikada oluşur).
    /// </summary>
    private async Task<KurSnapshot> GuncelKur(AktifKullanici islem) =>
        await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .OrderByDescending(k => k.Olusturuldu)
            .FirstOrDefaultAsync()
        ?? throw new BadRequestException(
            "Henüz kur kaydı yok; açılış kaydı kur fotoğrafına bağlanır. Birkaç saniye sonra yeniden deneyin.");

    /// <summary>Aynı kaynak + aynı dosya içeriği (SHA-256) UYGULANDI durumda mı?</summary>
    private async Task<string?> AyniDosyaUygulandi(AktifKullanici islem, string kaynakAd, string sha) =>
        await db.Set<IceAktarimOturumu>().AsNoTracking()
            .Where(o => o.TenantId == islem.TenantId && o.KaynakAd == kaynakAd
                        && o.DosyaSha256 == sha && o.Durum == IceAktarimDurumu.Uygulandi)
            .Select(o => o.Id)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Sütun eşleştirme: { bizimKolon: dosyadakiBaslik } → dosya başlığı bizim adla değişir.
    /// Bilinmeyen hedef, dosyada olmayan başlık ve iki hedefe eşlenen tek başlık 400'dür.
    /// </summary>
    private static string[] KolonlariEsle(
        string[] dosyaBasliklari, Dictionary<string, string>? esleme, IIceAktarilabilir kaynak)
    {
        var sonuc = dosyaBasliklari.ToArray();
        if (esleme is null || esleme.Count == 0) return sonuc;

        var kullanilan = new HashSet<int>();
        foreach (var (hedef, kaynakBaslik) in esleme)
        {
            if (string.IsNullOrWhiteSpace(kaynakBaslik)) continue;
            var kolon = kaynak.Kolonlar.FirstOrDefault(k => k.Ad.Equals(hedef, StringComparison.OrdinalIgnoreCase))
                ?? throw new BadRequestException($"Sütun eşleştirmede bilinmeyen alan: '{hedef}'.");
            var i = Array.FindIndex(dosyaBasliklari,
                b => b.Trim().Equals(kaynakBaslik.Trim(), StringComparison.OrdinalIgnoreCase));
            if (i < 0)
                throw new BadRequestException($"Sütun eşleştirme: '{kaynakBaslik}' başlığı dosyada yok.");
            if (!kullanilan.Add(i))
                throw new BadRequestException(
                    $"Sütun eşleştirme: '{kaynakBaslik}' başlığı birden fazla alana eşlenmiş.");
            sonuc[i] = kolon.Ad;
        }
        return sonuc;
    }

    // ── 6) Düzenleme yolu (MÜHÜR 2 / DoD): PATCH + iptal ────────────────────

    /// <summary>
    /// PATCH: gövdede olmayan alan KORUNUR. Yalnız PROVA durumundaki oturum
    /// düzenlenebilir — uygulanmış bir oturumun gerekçesi denetim izinin
    /// parçasıdır ve değiştirilemez (ADR-0004).
    /// </summary>
    public async Task<object> Guncelle(string oturumId, IceAktarimGuncelleDto dto, AktifKullanici islem)
    {
        var oturum = await OturumBul(oturumId, islem);

        if (oturum.Durum != IceAktarimDurumu.Prova)
            throw new ConflictException(
                $"'{oturum.Durum}' durumundaki oturum düzenlenemez; yalnız kuru prova aşamasındaki " +
                "oturumun gerekçesi ve yinelenen politikası değiştirilebilir.");

        var degisen = new List<string>();

        if (dto.GerekceVerildi)
        {
            var g = (dto.Gerekce ?? "").Trim();
            if (g.Length < ayar.GerekceAsgari)
                throw new BadRequestException(
                    $"İçe aktarım gerekçesi en az {ayar.GerekceAsgari} karakter olmalıdır.");
            oturum.Gerekce = g;
            degisen.Add("gerekce");
        }

        if (dto.YinelenenPolitikasiVerildi)
        {
            var p = (dto.YinelenenPolitikasi ?? "").Trim().ToUpperInvariant();
            if (!Data.YinelenenPolitikasi.Tumu.Contains(p))
                throw new BadRequestException(
                    $"Geçersiz yinelenen politikası: '{p}'. " +
                    $"Geçerli değerler: {string.Join(", ", Data.YinelenenPolitikasi.Tumu)}");
            oturum.YinelenenPolitikasi = p;
            degisen.Add("yinelenenPolitikasi");
        }

        if (degisen.Count == 0)
            throw new BadRequestException("Güncellenecek alan gönderilmedi.");

        denetim.YazmaKaydet(islem, DenetimVarligi, oturum.Id,
            $"düzenlendi · alanlar: {string.Join(", ", degisen)} · gerekçe: {oturum.Gerekce}");

        await db.SaveChangesAsync();

        // Politika değiştiyse satır sonuçları ARTIK GÜNCEL DEĞİLDİR: kullanıcı
        // yeniden prova çalıştırmalıdır (sessiz yeniden hesaplama yapılmaz).
        return new
        {
            oturumId = oturum.Id,
            durum = oturum.Durum,
            gerekce = oturum.Gerekce,
            yinelenenPolitikasi = oturum.YinelenenPolitikasi,
            provaYenilenmeli = dto.YinelenenPolitikasiVerildi,
        };
    }

    /// <summary>Pasife alma karşılığı: PROVA oturumunu iptal eder (satırlar kalır, iz kalır).</summary>
    public async Task<object> Iptal(string oturumId, AktifKullanici islem)
    {
        var oturum = await OturumBul(oturumId, islem);

        if (oturum.Durum != IceAktarimDurumu.Prova)
            throw new ConflictException(
                $"'{oturum.Durum}' durumundaki oturum iptal edilemez; uygulanmış oturum için GERİ AL kullanın.");

        await IptalYaz(oturum.Id, islem, "kullanıcı iptal etti");
        return new { oturumId = oturum.Id, durum = IceAktarimDurumu.Iptal };
    }

    // ── 7) Geçmiş ────────────────────────────────────────────────────────────

    public async Task<object> Gecmis(AktifKullanici islem, int limit)
    {
        var kayitlar = await db.Set<IceAktarimOturumu>().AsNoTracking()
            .Where(o => o.TenantId == islem.TenantId)
            .OrderByDescending(o => o.Olusturuldu)
            .Take(limit)
            .Select(o => new
            {
                id = o.Id,
                kullaniciId = o.KullaniciId,
                kaynakAd = o.KaynakAd,
                dosyaAdi = o.DosyaAdi,
                gerekce = o.Gerekce,
                durum = o.Durum,
                yinelenenPolitikasi = o.YinelenenPolitikasi,
                toplamSatir = o.ToplamSatir,
                gecerliSatir = o.GecerliSatir,
                hataliSatir = o.HataliSatir,
                atlananSatir = o.AtlananSatir,
                uygulananSatir = o.UygulananSatir,
                olusturuldu = o.Olusturuldu,
                uygulandi = o.Uygulandi,
                geriAlindi = o.GeriAlindi,
            })
            .ToListAsync();

        return new { toplam = kayitlar.Count, limit, kayitlar };
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task IptalYaz(string oturumId, AktifKullanici islem, string neden)
    {
        var o = await db.Set<IceAktarimOturumu>()
            .FirstOrDefaultAsync(x => x.Id == oturumId && x.TenantId == islem.TenantId);
        if (o is null) return;
        o.Durum = IceAktarimDurumu.Iptal;
        denetim.YazmaKaydet(islem, DenetimVarligi, o.Id, $"iptal · {neden} · gerekçe: {o.Gerekce}");
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// ⚠ İKİ KATMANLI KİRACI KORUMASI (ADR-0010): global sorgu filtresi ZATEN
    /// devrede; buna rağmen TenantId şartı ELLE de yazılır. Biri kaldırılırsa
    /// diğeri tutar.
    /// </summary>
    private async Task<IceAktarimOturumu> OturumBul(string id, AktifKullanici islem) =>
        await db.Set<IceAktarimOturumu>()
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == islem.TenantId)
        ?? throw new NotFoundException("İçe aktarım oturumu bulunamadı");

    private static IIceAktarilabilir Bul(string? ad) =>
        Kaynaklar.Bul(ad)
        ?? throw new BadRequestException(
            $"Bilinmeyen içe aktarım kaynağı: '{ad}'. " +
            $"Geçerli değerler: {string.Join(", ", Kaynaklar.Adlar)}. " +
            "Satış, fatura ve kasa geçmişi aktarılmaz (geçmiş hareket kapsam dışıdır).");

    private static Dictionary<string, string> Esle(string[] basliklar, string[] hucreler)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < basliklar.Length; i++)
            d[basliklar[i]] = i < hucreler.Length ? hucreler[i] : "";
        return d;
    }

    /// <summary>Dosya içi yinelenen tespiti için satır anahtarı.</summary>
    private static string Anahtar(IIceAktarilabilir kaynak, object dto) => dto switch
    {
        Cari.CariOlusturDto c =>
            $"cari|{Kuyumcu.Core.Domain.TurkceMetin.Buyut(c.Ad).Trim()}|" +
            new string((c.Telefon ?? "").Where(char.IsDigit).ToArray()),
        Urun.UrunOlusturDto u => $"urun|{u.Kod.Trim()}",
        _ => $"{kaynak.Ad}|{dto.GetHashCode()}",
    };

    /// <summary>
    /// Satırın ham alanları JSON'a çevrilir. Bu değer DİSKTE ŞİFRELİ durur
    /// (ADR-0003 değer dönüştürücüsü); ekrana giderken
    /// <see cref="MaskeliGoster"/> ile maskelenir.
    /// </summary>
    private static string HamJsonUret(IReadOnlyDictionary<string, string> ham)
    {
        var json = JsonSerializer.Serialize(ham);
        // Şifreli metin ham metinden büyüktür; TEXT sütununa sığması için
        // düz metin tarafı tavanlanır (CSV bomb ikinci savunması).
        return json.Length > 3000 ? json[..3000] : json;
    }

    /// <summary>
    /// EKRANA GİDEN KOPYA MASKELİDİR (Mehmet'in koşulu): satır listesinde
    /// vergi no / telefon / adres / e-posta düz metin dönmez. Dışa aktarımdaki
    /// <see cref="Disaaktarim.Maske"/> ile AYNI maske kullanılır.
    /// </summary>
    private static string MaskeliGoster(IIceAktarilabilir kaynak, string hamJson)
    {
        if (kaynak.KimlikKolonlari.Length == 0) return hamJson;

        Dictionary<string, string> d;
        try { d = JsonSozluk(hamJson); }
        catch (JsonException) { return "{}"; }

        var cikti = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in d)
        {
            if (!kaynak.KimlikKolonlari.Contains(k, StringComparer.OrdinalIgnoreCase)) { cikti[k] = v; continue; }
            cikti[k] = k.ToLowerInvariant() switch
            {
                "email" => Disaaktarim.Maske.Eposta(v, maskeli: true),
                "adres" => Disaaktarim.Maske.Adres(v, maskeli: true),
                _ => Disaaktarim.Maske.Kimlik(v, maskeli: true),
            };
        }
        return JsonSerializer.Serialize(cikti);
    }

    private static Dictionary<string, string> JsonSozluk(string json)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var belge = JsonDocument.Parse(json);
        foreach (var p in belge.RootElement.EnumerateObject())
            d[p.Name] = p.Value.ValueKind == JsonValueKind.Null ? "" : p.Value.ToString();
        return d;
    }

    private static string? Kirp(string? s, int n) => s is { Length: > 0 } && s.Length > n ? s[..n] : s;

    private static string Ozet(byte[] veri) =>
        Convert.ToHexString(SHA256.HashData(veri)).ToLowerInvariant();
}

// ═════════════════════════════════════════════════════════════════════════════
// UÇLAR — sınıf düzeyinde [RolGerekli("SAHIP")]: yeni bir uç izinsiz açılamaz.
// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/ice-aktarim")]
[Authorize]
[RolGerekli("SAHIP")]
[Yetki.ModulYetkisi("veri-aktarim")]
public sealed class IceAktarimController(IceAktarimServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/ice-aktarim/sablonlar — desteklenen kaynaklar ve kolonları</summary>
    [HttpGet("sablonlar")]
    public object Sablonlar() => servis.Sablonlar();

    /// <summary>GET /api/v1/ice-aktarim/sablon/{kaynak}.csv — boş şablon (UTF-8 BOM)</summary>
    [HttpGet("sablon/{kaynak}.csv")]
    public IActionResult Sablon(string kaynak)
    {
        var govde = servis.SablonCsv(kaynak);
        var bayt = Disaaktarim.CsvYazici.Bom.Concat(Encoding.UTF8.GetBytes(govde)).ToArray();
        Response.Headers.CacheControl = "no-store";
        return File(bayt, "text/csv; charset=utf-8", $"kuyumcu-{kaynak}-sablon.csv");
    }

    /// <summary>POST /api/v1/ice-aktarim/prova — KURU PROVA: hiçbir kart yazılmaz</summary>
    [EnableRateLimiting(OranSiniri.IceAktarim)]
    [HttpPost("prova")]
    public Task<object> Prova([FromBody] IceAktarimProvaDto dto) =>
        servis.Prova(dto, Islem, HttpContext.Connection.RemoteIpAddress?.ToString());

    /// <summary>GET /api/v1/ice-aktarim/{id}/satirlar — satır sonuçları (limit zorunlu)</summary>
    [HttpGet("{id}/satirlar")]
    public Task<object> Satirlar(
        string id, [FromQuery] string? sonuc = null,
        [FromQuery, Range(1, 200)] int limit = 50, [FromQuery, Range(0, int.MaxValue)] int offset = 0) =>
        servis.Satirlar(id, sonuc, limit, offset, Islem);

    /// <summary>POST /api/v1/ice-aktarim/{id}/uygula — onaylı, tek transaction</summary>
    [EnableRateLimiting(OranSiniri.IceAktarim)]
    [HttpPost("{id}/uygula")]
    public Task<object> Uygula(string id, [FromBody] IceAktarimUygulaDto dto) =>
        servis.Uygula(id, dto, Islem);

    /// <summary>POST /api/v1/ice-aktarim/{id}/geri-al — oluşan kartları PASİFE alır</summary>
    [HttpPost("{id}/geri-al")]
    public Task<object> GeriAl(string id) => servis.GeriAl(id, Islem);

    /// <summary>PATCH /api/v1/ice-aktarim/{id} — gerekçe / politika düzeltme (yalnız PROVA)</summary>
    [HttpPatch("{id}")]
    public Task<object> Guncelle(string id, [FromBody] IceAktarimGuncelleDto dto) =>
        servis.Guncelle(id, dto, Islem);

    /// <summary>POST /api/v1/ice-aktarim/{id}/iptal — PROVA oturumunu iptal eder</summary>
    [HttpPost("{id}/iptal")]
    public Task<object> Iptal(string id) => servis.Iptal(id, Islem);

    /// <summary>GET /api/v1/ice-aktarim/gecmis — kim ne zaman ne aktardı</summary>
    [HttpGet("gecmis")]
    public Task<object> Gecmis([FromQuery, Range(1, 200)] int limit = 50) => servis.Gecmis(Islem, limit);
}
