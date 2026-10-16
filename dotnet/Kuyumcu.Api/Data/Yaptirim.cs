using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-3 (§ 4.4) — YAPTIRIM (MALVARLIĞI DONDURMA) LİSTESİ.
//
// DAYANAK (birincil, şartname § 3.1): 6415 s. K. md.10 (dondurma kararına aykırı
// işlem hükümsüz), md.12/1 (RG'de yayımla sonuç doğurur), md.12/3 (alacak/borç
// 30 gün içinde bildirilir); 7262 s. K. md.3; MASAK Genel Tebliği Sıra No 34
// md.5 (listedeki kişinin istediği işlem ivedilikle bildirilir ve ERTELENİR).
//
// ⚠ yaptirim_kayitlari KİRACI-ÜSTÜDÜR (resmî liste tüm kuyumcular için aynıdır):
//   IKiraciVarlik DEĞİLDİR, HasQueryFilter EKLENMEZ (KuyumcuDbContext kapsam
//   denetiminde gerekçeli beyaz liste maddesi). Yazma YALNIZ sistem anahtarlı
//   uçtandır (ByGatech merkezî günceller; kuyumcu liste yüklemez). SATIR
//   SİLİNMEZ: karar kaldırılırsa `kaldirildi` damgalanır.
// ⚠ Kimlik no ŞİFRELİ iner (AES-GCM, MasakKimlikTespiti.BelgeNo ile aynı
//   altyapı); arama yalnız HMAC parmak izi (`kimlik_no_hash`) üzerinden.
// ⚠ yaptirim_eslesmeleri KİRACIYA AİTTİR (IKiraciVarlik): hangi carinin/hangi
//   denemenin listeyle eşleştiği, tezgâhın kilidi ve MASAK'a bildirim kapatması.
// ═════════════════════════════════════════════════════════════════════════════

[Table("yaptirim_kayitlari")]
public class YaptirimKaydi
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;

    /// <summary>Resmî liste adı (ör. "6415 s. K. md.5 — BMGK kararları").</summary>
    [Column("kaynak"), MaxLength(150)] public string Kaynak { get; set; } = null!;
    [Column("kaynak_url"), MaxLength(500)] public string KaynakUrl { get; set; } = null!;

    /// <summary>Karar (Cumhurbaşkanı/BKK) numarası; bilinmiyorsa NULL.</summary>
    [Column("karar_no"), MaxLength(100)] public string? KararNo { get; set; }

    /// <summary>Kararın Resmî Gazete yayım tarihi — 30 günlük bildirim süresi buradan başlar (6415 md.12/3).</summary>
    [Column("rg_tarihi")] public DateOnly? RgTarihi { get; set; }

    /// <summary>Resmî listenin (kaynak sayfasının) ByGatech'e göre güncel olduğu gün.</summary>
    [Column("liste_tarihi")] public DateOnly ListeTarihi { get; set; }

    /// <summary>Ad-soyad/unvan — NORMALİZE (büyük harf, Türkçe harf katlanmış, tek boşluk).</summary>
    [Column("ad_soyad"), MaxLength(300)] public string AdSoyad { get; set; } = null!;
    [Column("dogum_tarihi")] public DateOnly? DogumTarihi { get; set; }
    [Column("uyruk"), MaxLength(3)] public string? Uyruk { get; set; }

    /// <summary>TCKN/pasaport/VKN — ŞİFRELİ (value converter). Yoksa NULL.</summary>
    [Column("kimlik_no"), MaxLength(200)] public string? KimlikNo { get; set; }
    /// <summary>HMAC-SHA256(normalize(KimlikNo)) — tam eşleşme bu kolondan aranır.</summary>
    [Column("kimlik_no_hash"), MaxLength(64)] public string? KimlikNoHash { get; set; }
    [Column("sifre_surumu")] public byte SifreSurumu { get; set; }

    [Column("eklendi")] public DateTime Eklendi { get; set; }
    /// <summary>Karar kaldırıldıysa damga; NULL = yürürlükte.</summary>
    [Column("kaldirildi")] public DateTime? Kaldirildi { get; set; }
    /// <summary>"sistem:&lt;operatör&gt;".</summary>
    [Column("olusturan"), MaxLength(60)] public string Olusturan { get; set; } = null!;
}

/// <summary>Eşleşme türü.</summary>
public static class YaptirimEslesmeTuru
{
    /// <summary>Kimlik no birebir aynı — İŞLEM YAPILMAZ.</summary>
    public const string Tam = "TAM";
    /// <summary>Yalnız ad benzerliği — ENGEL DEĞİL, kontrol bildirimi.</summary>
    public const string AdBenzerligi = "AD_BENZERLIGI";
    public static readonly string[] Hepsi = [Tam, AdBenzerligi];
}

/// <summary>Eşleşmenin yakalandığı nokta.</summary>
public static class YaptirimNoktasi
{
    public const string MasakTespit = "MASAK_TESPIT";
    public const string Cari = "CARI";
    public const string HurdaSatici = "HURDA_SATICI";
    public const string ListeGuncelleme = "LISTE_GUNCELLEME";
    public static readonly string[] Hepsi = [MasakTespit, Cari, HurdaSatici, ListeGuncelleme];
}

/// <summary>Eşleşmenin iş durumu (kilidi DEĞİL — kilit yalnız karar kaldırılınca açılır).</summary>
public static class YaptirimEslesmeDurumu
{
    /// <summary>MASAK'a bildirim bekleniyor (görev açık).</summary>
    public const string Acik = "ACIK";
    /// <summary>MASAK'a bildirildi (tarih + evrak no ile kapatıldı).</summary>
    public const string Bildirildi = "BILDIRILDI";
    /// <summary>Ad benzerliği kontrol edildi — aynı kişi değil.</summary>
    public const string AyniKisiDegil = "AYNI_KISI_DEGIL";
    /// <summary>
    /// PAKET YASAL-3 tur 2: liste güncellemesinde eşleşen carinin bildirilecek alacak/borç/
    /// emanet/siparişi YOK → görev açılmaz, cari yine KİLİTLİ. "Bildirildi" DEĞİLDİR (MASAK'a
    /// hiçbir bildirim yapılmadı). Yalnız sistem yazar; kullanıcı PATCH ile bu duruma
    /// geçiremez (Hepsi'nde yok). Kilit yeni para/mal hareketini SaveChanges'ta durdurduğu
    /// için sonradan bakiye doğamaz (KuyumcuDbContext.YaptirimKilidiniKoru).
    /// </summary>
    public const string VarlikYok = "VARLIK_YOK";
    /// <summary>Kullanıcının PATCH ile seçebileceği durumlar.</summary>
    public static readonly string[] Hepsi = [Acik, Bildirildi, AyniKisiDegil];
}

[Table("yaptirim_eslesmeleri")]
public class YaptirimEslesmesi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;

    [Column("yaptirim_kaydi_id"), MaxLength(36)] public string YaptirimKaydiId { get; set; } = null!;

    /// <summary><see cref="YaptirimEslesmeTuru"/>.</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    /// <summary><see cref="YaptirimNoktasi"/>.</summary>
    [Column("nokta"), MaxLength(20)] public string Nokta { get; set; } = null!;

    /// <summary>Eşleşen cari (varsa). TAM + cari → o cariyle her işlem 400.</summary>
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }

    /// <summary><see cref="YaptirimEslesmeDurumu"/>.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = YaptirimEslesmeDurumu.Acik;

    /// <summary>MASAK'a bildirimin son günü (liste güncellemesinde RG + 30 gün; tezgâhta "ivedilikle" = aynı gün).</summary>
    [Column("bildirim_son_gunu")] public DateOnly? BildirimSonGunu { get; set; }
    [Column("masak_bildirim_tarihi")] public DateOnly? MasakBildirimTarihi { get; set; }
    [Column("masak_evrak_no"), MaxLength(100)] public string? MasakEvrakNo { get; set; }
    [Column("not_metni"), MaxLength(500)] public string? Not { get; set; }

    [Column("olusturan_kullanici_id"), MaxLength(36)] public string? OlusturanKullaniciId { get; set; }
    [Column("guncelleyen_kullanici_id"), MaxLength(36)] public string? GuncelleyenKullaniciId { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
