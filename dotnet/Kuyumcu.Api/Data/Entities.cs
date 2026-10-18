using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuyumcu.Api.Data;

// ─────────────────────────────────────────────────────────────────────────────
// Mevcut MySQL şemasına (Drizzle migration'ları) BİREBİR eşlenen varlıklar.
// Şemanın kaynağı packages/db/src/schema — yeni migration'lar oradan üretilir;
// EF Core burada yalnızca eşleme yapar (database-first davranış).
// Enum kolonları sözleşme sadakati için string taşınır; geçerli değerler
// Sabitler sınıfındadır.
// ─────────────────────────────────────────────────────────────────────────────

public static class Sabitler
{
    public static readonly string[] Planlar = ["BASLANGIC", "PRO", "KURUMSAL"];
    public static readonly string[] Roller = ["SAHIP", "MUDUR", "TEZGAHTAR", "MUHASEBE"];
    public static readonly string[] CariTurleri = ["MUSTERI", "TEDARIKCI", "HER_IKISI", "ATOLYE", "SARRAF"];
    // PLATIN · PALADYUM · CELIK 2026-09-14'te eklendi. Rakip taramasında
    // "metal/ürün tipine göre ayrı stok hattı" standart bulundu.
    /// <summary>
    /// Ürün grubu listesi — TEK KAYNAK <see cref="Kuyumcu.Core.Domain.UrunTipleri"/>
    /// (paket 20, UX-S31). Burada elle yazılı DURURKEN ekrandaki kopya geride
    /// kalmış, PLATIN/PALADYUM/CELIK/DEGERLI_TAS grupları arayüzden hiç
    /// seçilemiyordu.
    /// </summary>
    public static readonly string[] UrunTipleri = Kuyumcu.Core.Domain.UrunTipleri.Hepsi;
    public static readonly string[] HareketTurleri =
        ["ALIS", "SATIS", "TAKAS", "HURDA_ALIS", "EMANET_GIRIS", "EMANET_CIKIS",
         "VERESIYE", "TAHSILAT", "TRANSFER", "SAYIM_DUZELTME", "ISCILIK",
         // Liste GERÇEĞİ yansıtır (ekip oyu Q2, 2026-09-15): aşağıdaki üç tür
         // deftere fiilen yazılıyordu ama listede yoktu — denetçi ya da rapor
         // geliştiricisi bunları "geçersiz tür" sanıp yanlış süzebilirdi.
         // SATIS_IADE/ALIS_IADE: SatisModulu.cs · CARI_DUZELTME: CariModulu.cs.
         // ⚠ Bu dizi kodda ZORLANMAZ (doğrulama yok) — belge değeri taşır.
         "SATIS_IADE", "ALIS_IADE", "CARI_DUZELTME",
         // Paket 27 Faz A — üçüncü paralel defter (döviz). Bu iki türde
         // para_birimi USD|EUR|GBP, doviz_tutar İŞARETLİ efektiftir.
         "DOVIZ_VERESIYE", "DOVIZ_TAHSILAT",
         // Paket T-2 Faz A — AÇILIŞ DEVRİ. Yeni kurulan dükkânın ilk gününde
         // mevcut kasa/banka/cari bakiyesinin deftere girişi. PARA EL
         // DEĞİŞTİRMEZ: OdemeIzi.Yok ile yazılır, tevsik (459 s. VUK) ve MASAK
         // kümülatifleri ETKİLENMEZ (CARI_DUZELTME / SAYIM_DUZELTME gerekçesi).
         // ⚠ Bu satırın eklenmesi bir KAPI DEĞİL, belge değeridir (yukarıdaki
         //   "dizi kodda ZORLANMAZ" uyarısı buna da geçerlidir). Gerçek kapılar:
         //   denetim.sh "Kurulum/ altında dogrudan Add = 0" sayacı ve
         //   DuzeltmePolitikasi TAM-ÖRTÜŞME testi (Data/Kurulum.cs başlığı).
         "ACILIS",
         // Paket 29 Faz A — KIYMETLİ EVRAK (çek/senet portföyü).
         // ⚠ Aynı belge değeri gerekçesi: bu üç tür deftere fiilen yazılır;
         //   listede olmasalardı rapor/denetim tarafı onları "geçersiz tür"
         //   sanıp süzerdi. EVRAK_GIRIS borç KAPATIR (TlTutari −, veresiye payı
         //   VeresiyeKapanisi), EVRAK_KARSILIKSIZ borcu GERİ DOĞURUR (TlTutari +,
         //   Veresiyeden + vade). EVRAK_CIRO Faz B'de yazılacaktır; türü şimdiden
         //   listede durur ki Faz B bu dosyaya ikinci kez dokunmasın.
         // ⚠ Evrağın TAHSİL bacağı burada YOKTUR ve bu bilinçlidir: para
         //   girişi mevcut "TAHSILAT" türüyle yazılır, böylece MASAK kapısı
         //   (MasakKapisi.KapiyaTabiTurler) ve tevsik kümülatifi evrak
         //   tahsilatını nakit/banka tahsilatıyla AYNI ölçer (Mehmet parite şartı).
         "EVRAK_GIRIS", "EVRAK_KARSILIKSIZ", "EVRAK_CIRO",
         // PAKET P-20 (Y-10) — HURDA SATIŞI / RAMAT ÇIKIŞI. Biriken hurdanın
         // rafineriye (ramatçıya) çıkışı: has bacağı EKSİLİR (-), TL bacağı
         // ARTAR (+). HURDA_ALIS ile AYNI defter hattını paylaşır; hurda has
         // bakiyesi bu iki türün has toplamıdır (ayrı stok kolonu YOKTUR —
         // P-20 ölçümü). Fire/ramat farkı bu sürümde YAZILMAZ (K-13 bekliyor).
         "HURDA_SATIS",
         // PAKET P-20b (Y-10 kalan dilim · K-13) — TAHLİL / MUTABAKAT FARKI.
         // Hurda satışında alıcının (rafineri/toptancı) KABUL ETTİĞİ has ile
         // bizim KAYDETTİĞİMİZ has farklı çıkar. HURDA_SATIS satırı yalnız
         // KABUL EDİLEN hası taşır (TL bacağı onunla ödenir); aradaki fark bu
         // TÜRDE ayrı bir bacağa yazılır ve TL bacağı DAİMA 0'dır.
         //   HURDA_SATIS.HasGrami + TAHLIL_FARKI.HasGrami = −(bizim kaydımız)
         // Bu ayrım olmasaydı has-cari denk gelmezdi: ya stok eksik düşerdi ya
         // da cariye kabul edilmeyen has borçlanılırdı.
         // ⚠ Bu tür FİRE DEĞİLDİR ve öyle adlandırılmaz (K-13/1: eritme firesi
         //   imalat kapsamındadır ve 1.0'a GİRMEZ). Tahlil farkı bir ÖLÇÜM
         //   mutabakatsızlığıdır, bir üretim kaybı değil.
         "TAHLIL_FARKI",
         // PAKET P-21 (Y-17 · Y-20) — AVANS ve KONSİNYE ÇIKIŞ hattı.
         //
         // AVANS_ALINDI  : müşteri malı almadan ÖNCE para bırakır. TL bacağı +,
         //                 has bacağı 0, cari bakiye −tutar (biz ona borçlanırız).
         // AVANS_MAHSUP  : bırakılan avansın bir satışa sayılması. TL bacağı −,
         //                 cari bakiyeye DOKUNMAZ — çünkü bakiye AVANS_ALINDI
         //                 anında zaten azalmıştır. İkinci kez azaltılsaydı aynı
         //                 para iki kez alacak yazılırdı (P-21 DoD, M21-02).
         // AVANS_IADE    : satış olmadan avansın geri verilmesi. TL bacağı −,
         //                 cari bakiye +tutar (borcumuz kapanır).
         // KONSINYE_CIKIS: BİZİM malımızın başka bir cariye (kuyumcu/sarraf)
         //                 satılmak üzere emanet gönderilmesi. Has bacağı −,
         //                 TL bacağı 0 — MÜLKİYET DEVRİ DEĞİLDİR, satış değildir.
         // KONSINYE_IADE : o malın geri gelmesi. Has bacağı +, TL bacağı 0.
         // ⚠ Bu beş tür hareketler.tur kapısından (HareketFabrikasi) geçer;
         //   listeye eklenmeselerdi yazma 400 ile REDDEDİLİRDİ.
         "AVANS_ALINDI", "AVANS_MAHSUP", "AVANS_IADE",
         "KONSINYE_CIKIS", "KONSINYE_IADE",
         // PAKET P-21 (Y-21) — RÜCU. Ciro ettiğimiz evrak karşılıksız çıkınca
         // ciro ettiğimiz cariye olan BORCUMUZ geri doğar (TTK md.724).
         // ⚠ EVRAK_KARSILIKSIZ'dan AYRI tür: o satır KAYNAK carinin (keşideci)
         //   borcunu geri doğurur ve TL bacağı +; bu satır BİZİM borcumuzu
         //   doğurur ve TL bacağı −. Tek türle yazılsalardı aynı türde iki zıt
         //   işaret olur, ciro/rücu raporu ikisini birbirinden ayıramazdı.
         "EVRAK_RUCU"];
    /// <summary>
    /// <c>hareketler.personel_id</c> kolonunun TEK anlamı (paket P-08 / Y-13).
    /// Kolon başka bir role (tahsil eden, onaylayan) ÖDÜNÇ VERİLEMEZ; başka rol
    /// gerekirse ayrı kolon açılır. <c>denetim.sh</c> P-08 bu sabiti sayar.
    /// </summary>
    public const string HareketPersonelRolu = "SATAN";

    public static readonly string[] ParaBirimleri = ["TRY", "USD", "EUR", "GBP"];
    public static readonly string[] OdemeTurleri = ["NAKIT", "KART", "HAVALE", "ALTIN", "DIGER"];
    public static readonly string[] KurKaynaklari = ["ALTINKAYNAK", "TRUNCGIL", "HAREM", "KAPALICARSI", "IAB", "MANUAL"];
    public static readonly string[] BankaYonleri = ["GELEN", "GIDEN"];
    public static readonly string[] BankaDurumlari = ["BEKLIYOR", "ESLESTI", "FATURALANDI"];
    public static readonly string[] FaturaTurleri = ["SATIS", "ALIS"];
    public static readonly string[] FaturaDurumlari = ["TASLAK", "KESILDI", "ODENDI", "IPTAL"];
    /// <summary>Faturanın tutarı nereden geldi: BASLIK (yalnız toplam) · KALEM (satır kalemlerinden).</summary>
    public static readonly string[] FaturaKalemKaynaklari = ["BASLIK", "KALEM"];
    /// <summary>faturalar.has_deger otoritesi (paket 1 ↔ 38 tek kolon kararı).</summary>
    public static readonly string[] HasDegerKaynaklari = ["HAREKET", "KALEM", "ELLE"];
    /// <summary>Fatura kaleminin birimi — ürün kartından kopya (A15: gramlı üründe 1 adet = 1 gram).</summary>
    public static readonly string[] KalemBirimleri = ["ADET", "GRAM"];
    /// <summary>Arayüz modu — BASIT (sade tezgâh yüzü) · PROFESYONEL (tam güç).
    /// Bir TERCİHTİR, yetki değildir: sunucudaki hiçbir kapı bu değere bakmaz (K-UX-6).</summary>
    public static readonly string[] ArayuzModlari = ["BASIT", "PROFESYONEL"];
}

[Table("tenants")]
public class Tenant
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("plan"), MaxLength(20)] public string Plan { get; set; } = "BASLANGIC";
    /// <summary>Kur çekilecek kaynak (ALTINKAYNAK · TRUNCGIL). Boşsa yapılandırmadaki varsayılan.</summary>
    [Column("kur_kaynagi"), MaxLength(20)] public string? KurKaynagi { get; set; }
    /// <summary>
    /// KUR KAYNAKLARI (kurucu 17.10) — döviz kuru kaynağı, altın kaynağından AYRI:
    /// EVDS · TCMB · ALTIN_KAYNAGI · HAREM · ALTINKAYNAK · TRUNCGIL. Boşsa varsayılan
    /// (EVDS anahtarı tanımlıysa EVDS, değilse altın kaynağının kendi döviz kuru).
    /// </summary>
    [Column("doviz_kaynagi"), MaxLength(20)] public string? DovizKaynagi { get; set; }
    /// <summary>
    /// KUR KAYNAKLARI (kurucu 17.10) — has bedeli (KDVK 23/e) kaynağı:
    /// BIST · HAREM · ALTINKAYNAK · ISLEM_KURU · ELLE. YENİ işletme kayıtta BIST ile doğar
    /// (kurucu varsayılanı; AuthServisi). BOŞ = bu paketten önceki kiracı → ISLEM_KURU (bugünkü
    /// davranış; geçiş faturalamayı kesmez — D1 kalıbı, geri doldurma yok).
    /// </summary>
    [Column("ozel_matrah_kaynagi"), MaxLength(20)] public string? OzelMatrahKaynagi { get; set; }
    /// <summary>
    /// Satılan malın maliyeti hangi sırayla düşülür: FIFO · LIFO · ORTALAMA.
    /// Boşsa FIFO — kuyumcuda en yaygın ve VUK'a en uygun olanı.
    /// </summary>
    [Column("maliyet_yontemi"), MaxLength(20)] public string? MaliyetYontemi { get; set; }
    /// <summary>
    /// Kullanıcısı kendi tercihini belirtmemişse (kullanicilar.arayuz_modu NULL)
    /// geçerli olan arayüz modu: BASIT | PROFESYONEL. Varsayılan PROFESYONEL —
    /// mevcut kiracıların gördüğü ekran migration sonrası HİÇ değişmez.
    /// PAKET EKSIK-KUCUK (B2-11 · D1): YENİ işletme (POST /auth/kayit) BASİT doğar —
    /// değer kayıt yolunda açıkça yazılır (AuthServisi), buradaki başlangıç değeri
    /// ve sütunun DB varsayılanı bilinçli olarak PROFESYONEL kalır (eski satırlar korunur).
    /// </summary>
    [Column("varsayilan_arayuz_modu"), MaxLength(12)]
    public string VarsayilanArayuzModu { get; set; } = "PROFESYONEL";
    /// <summary>
    /// İşletme kimlik bilgileri — paket UX-1 Faz B.
    /// <para>
    /// Fatura ve gider pusulasında SATICI (düzenleyen) kimliğidir; tek kaynak
    /// Tenant satırıdır (HurdaModulu düzenleyen bloğu buradan beslenir).
    /// Hepsi NULL olabilir: mevcut kiracılar migration sonrası aynı kalır.
    /// </para>
    /// </summary>
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }
    [Column("vergi_dairesi"), MaxLength(100)] public string? VergiDairesi { get; set; }
    /// <summary>10 hane (kurum) ya da 11 hane (TCKN) — biçim doğrulaması DTO'dadır.</summary>
    [Column("vergi_no"), MaxLength(11)] public string? VergiNo { get; set; }
    [Column("telefon"), MaxLength(20)] public string? Telefon { get; set; }
    [Column("ai_etkin")] public bool AiEtkin { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    /// <summary>
    /// Kiracının yaşam durumu — paket F-2-min Faz A.
    /// <para>
    /// DENEME · AKTIF · SALT_OKUNUR. Yazma kapısının (KiraciDurumKapisi) TEK
    /// kaynağı bu kolon + <see cref="DenemeBitis"/>'tir; modüller kendi içinde
    /// ikinci bir süre kontrolü YAZMAZ.
    /// </para>
    /// <para>
    /// ⚠ Migration varsayılanı 'AKTIF'tir (veritabanı düzeyinde DEFAULT):
    /// mevcut kiracıların yazması migration sonrası KESİLMEZ. 'DENEME'
    /// varsayılanı üretim durdurucu olurdu.
    /// </para>
    /// </summary>
    [Column("durum"), MaxLength(16)] public string Durum { get; set; } = KiraciDurumlari.Aktif;
    /// <summary>Deneme bitiş anı (UTC). NULL = süre kısıtı yok.</summary>
    [Column("deneme_bitis")] public DateTime? DenemeBitis { get; set; }
    /// <summary>Bu kiracıyı doğuran davetin kimliği (kiraci_davetleri.id). Eski kiracılarda NULL.</summary>
    [Column("davet_id"), MaxLength(36)] public string? DavetId { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// Kiracı yaşam durumları (tenants.durum) — paket F-2-min Faz A.
/// <para>
/// ⚠ Bu dizi kodda ZORLANMAZ (VARCHAR(16), enum kısıtı yok): veritabanına elle
/// yazılmış bilinmeyen bir değer de olabilir. Kapı bu yüzden BEYAZ LİSTE ile
/// çalışır — bilinmeyen değer yazmayı KAPATIR (fail-closed).
/// </para>
/// </summary>
public static class KiraciDurumlari
{
    public const string Deneme = "DENEME";
    public const string Aktif = "AKTIF";
    public const string SaltOkunur = "SALT_OKUNUR";

    public static readonly string[] Tumu = [Deneme, Aktif, SaltOkunur];

    public static bool Gecerli(string? d) =>
        d is not null && Array.IndexOf(Tumu, d) >= 0;
}

/// <summary>
/// Tek kullanımlık kiracı daveti — paket F-2-min Faz A.
///
/// <para>
/// ⚠ <b>IKiraciVarlik DEĞİLDİR ve bu bilinçlidir.</b> Davet, kiracı DOĞMADAN
/// ÖNCE var olur (Tenant'ın kendisi gibi): kaydı üreten sistem yöneticisidir,
/// tüketen ise henüz kiracısı olmayan anonim bir istektir. tenant_id kolonu
/// olsaydı ne yazılacağı belirsiz kalırdı.
/// </para>
/// <para>
/// ⚠ <b>SONUCU:</b> global kiracı filtresi ve <c>KiraciYazmaDenetcisi</c> bu
/// tabloya İŞLEMEZ — tek koruma katmanı <c>SistemAnahtariKapisi</c>'dır. Bu bir
/// KABUL EDİLEN RİSKTİR (Mehmet, F-2-min oylaması) ve SESSION-HANDOFF'a
/// yazılmıştır. Belgeleyen test: KiraciFiltreTestleri.
/// </para>
/// <para>
/// ⚠ HAM JETON SAKLANMAZ: yalnız <see cref="JetonHash"/> (SHA-256 hex) tutulur.
/// Ham jeton üretim yanıtında BİR KEZ döner; loga, denetim kaydına ve hata
/// mesajına yazılmaz.
/// </para>
/// </summary>
[Table("kiraci_davetleri")]
public class KiraciDaveti
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("jeton_hash"), MaxLength(100)] public string JetonHash { get; set; } = null!;
    [Column("email"), MaxLength(200)] public string? Email { get; set; }
    [Column("isletme_adi"), MaxLength(200)] public string? IsletmeAdi { get; set; }
    /// <summary>Bu davetle açılacak kiracının deneme süresi (gün).</summary>
    [Column("deneme_gun")] public int DenemeGun { get; set; }
    /// <summary>Jetonun son kullanma anı (UTC).</summary>
    [Column("gecerlilik")] public DateTime Gecerlilik { get; set; }
    [Column("kullanildi_mi")] public bool KullanildiMi { get; set; }
    [Column("kullanan_tenant_id"), MaxLength(36)] public string? KullananTenantId { get; set; }
    [Column("iptal")] public bool Iptal { get; set; }
    /// <summary>Yöneticinin notu — KİŞİSEL VERİ değil, hangi pilot olduğunun izi.</summary>
    [Column("olusturan_not"), MaxLength(200)] public string? OlusturanNot { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>Davetin ekranda/yanıtta görünen durumu — ham kolonlardan TÜRETİLİR (UX-S21).</summary>
public static class DavetDurumlari
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Kullanildi = "KULLANILDI";
    public const string SuresiGecti = "SURESI_GECTI";
    public const string Iptal = "IPTAL";

    public static string Coz(KiraciDaveti d, DateTime now) =>
        d.KullanildiMi ? Kullanildi
        : d.Iptal ? Iptal
        : d.Gecerlilik < now ? SuresiGecti
        : Bekliyor;
}

[Table("subeler")]
public class Sube : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }
    [Column("telefon"), MaxLength(20)] public string? Telefon { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("kullanicilar")]
public class Kullanici : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("soyad"), MaxLength(100)] public string Soyad { get; set; } = null!;
    [Column("email"), MaxLength(254)] public string Email { get; set; } = null!;
    [Column("sifre_hash"), MaxLength(255)] public string SifreHash { get; set; } = null!;
    [Column("rol"), MaxLength(20)] public string Rol { get; set; } = "TEZGAHTAR";
    /// <summary>
    /// D1-14 (18.10, rakip eşitleme: "mali müşavir rolü — yalnız okuma"). Yalnız MUHASEBE rolünde anlamlıdır:
    /// hesap muhasebenin gördüğü her şeyi görür, hiçbir yazma isteği yapamaz (SaltOkunurKapisi, tek kapı).
    /// </summary>
    [Column("salt_okunur")] public bool SaltOkunur { get; set; }
    /// <summary>
    /// Kullanıcının arayüz tercihi: BASIT | PROFESYONEL.
    /// <b>NULL = "bilinmiyor" DEĞİL, "kiracı varsayılanını devral" demektir</b>
    /// (tenants.varsayilan_arayuz_modu). Çözüm tek yerdedir: ArayuzModu.Coz.
    /// Bir TERCİHTİR — hiçbir yasal kapı bu değere bakmaz.
    /// </summary>
    [Column("arayuz_modu"), MaxLength(12)] public string? ArayuzModu { get; set; }

    /// <summary>
    /// PAKET B-1 — kullanıcının aydınlık/karanlık tercihi:
    /// <c>otomatik</c> | <c>aydinlik</c> | <c>karanlik</c>.
    /// <b>NULL = "bilinmiyor" DEĞİL, "dükkân varsayılanını devral" demektir</b>
    /// (kiraci_ayarlari.gorunum_tema_modu). Çözüm TEK yerdedir:
    /// <see cref="Modules.Gorunum.KullaniciGorunumServisi"/>.
    /// Bir TERCİHTİR — hiçbir yasal/para kapısı bu değere bakmaz.
    /// </summary>
    [Column("tema_modu"), MaxLength(12)] public string? TemaModu { get; set; }

    /// <summary>
    /// PAKET B-1 — kullanıcının renk şeması (8 slug'dan biri). NULL = dükkân
    /// varsayılanını devral. Beyaz liste: <see cref="Modules.Gorunum.GorunumSecenekleri.Semalar"/>.
    /// </summary>
    [Column("sema"), MaxLength(24)] public string? Sema { get; set; }

    /// <summary>
    /// PAKET B-1 — kullanıcının gövde yazı boyutu: 16 | 18 | 20 (px).
    /// NULL = dükkân varsayılanını devral.
    /// </summary>
    [Column("yazi_boyutu")] public int? YaziBoyutu { get; set; }

    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("son_giris")] public DateTime? SonGiris { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("refresh_tokenler")]
public class RefreshToken : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("token_hash"), MaxLength(64)] public string TokenHash { get; set; } = null!;
    [Column("ip_adresi"), MaxLength(45)] public string? IpAdresi { get; set; }
    [Column("user_agent"), MaxLength(500)] public string? UserAgent { get; set; }
    [Column("gecerlilik")] public DateTime Gecerlilik { get; set; }
    [Column("iptal_edildi")] public bool IptalEdildi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("kur_snapshots")]
public class KurSnapshot : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("altin_has_fiyati", TypeName = "decimal(15,4)")] public decimal AltinHasFiyati { get; set; }
    [Column("gram_altin_fiyati", TypeName = "decimal(15,4)")] public decimal? GramAltinFiyati { get; set; }
    [Column("usd_try", TypeName = "decimal(15,4)")] public decimal? UsdTry { get; set; }
    [Column("eur_try", TypeName = "decimal(15,4)")] public decimal? EurTry { get; set; }
    [Column("gbp_try", TypeName = "decimal(15,4)")] public decimal? GbpTry { get; set; }
    [Column("kaynak"), MaxLength(20)] public string Kaynak { get; set; } = "MANUAL";

    /// <summary>
    /// KUR KAYNAKLARI (17.10) — döviz alanlarının (usd/eur/gbp) GERÇEK kaynağı; altın kaynağından
    /// ayrı seçilebilir (EVDS · TCMB · HAREM …). NULL: altın kaynağının kendi kuru ya da elle giriş.
    /// </summary>
    [Column("doviz_kaynak"), MaxLength(20)] public string? DovizKaynak { get; set; }

    /// <summary>
    /// PAKET S-3b — yanlış girilen MANUEL kur kaydı GEÇERSİZ kılındı mı?
    /// Satır SİLİNMEZ (defter yerinde değişmez): geçmişte "geçersiz" rozetiyle
    /// kalır, güncel kur seçiminden ve yeni işlemlerden DIŞLANIR.
    /// </summary>
    [Column("iptal_edildi")] public bool IptalEdildi { get; set; }

    /// <summary>İptal gerekçesi — en az 20 karakter, denetim izine düşer.</summary>
    [Column("iptal_gerekce"), MaxLength(1000)] public string? IptalGerekce { get; set; }

    [Column("iptal_tarihi")] public DateTime? IptalTarihi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("cariler")]
public class Cari : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    [Column("ad"), MaxLength(200)] public string Ad { get; set; } = null!;
    [Column("telefon"), MaxLength(20)] public string? Telefon { get; set; }
    [Column("email"), MaxLength(254)] public string? Email { get; set; }
    [Column("vergi_no"), MaxLength(20)] public string? VergiNo { get; set; }
    [Column("adres"), MaxLength(500)] public string? Adres { get; set; }
    [Column("notlar"), MaxLength(1000)] public string? Notlar { get; set; }
    [Column("kredi_limiti_tl", TypeName = "decimal(15,2)")] public decimal? KrediLimitiTl { get; set; }
    /// <summary>
    /// Bu cariye veresiye satışta varsayılan vade — GÜN. <c>null</c> = vade
    /// tanınmamış; borç doğduğu gün muaccel sayılır (peşin beklenir).
    /// İşlem başına <c>vadeGun</c> ile ezilebilir.
    /// </summary>
    [Column("varsayilan_vade_gun")] public int? VarsayilanVadeGun { get; set; }
    /// <summary>Bu cariye uygulanan fiyat listesi; boşsa kiracının varsayılan listesi.</summary>
    [Column("fiyat_listesi_id"), MaxLength(36)] public string? FiyatListesiId { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;

    // ── PAKET P-16 (Y-27) — CARİ KARTI KİMLİK / KYC ALANLARI ─────────────────
    // Hepsi NULL olabilir: mevcut kiracıların kartları migration sonrası AYNI
    // kalır, hiçbir kayıt geçersizleşmez.

    /// <summary>
    /// Ticari ünvan — <see cref="Ad"/> günlük çağrı adıdır ("Ahmet Usta"),
    /// ünvan faturaya yazılan tüzel addır ("Ahmet Kuyumculuk San. Tic. Ltd.
    /// Şti."). İkisi tek kolona sıkıştırılırsa fatura ya yanlış ünvanla çıkar
    /// ya listede okunmaz uzunlukta ad görünür.
    /// </summary>
    [Column("unvan"), MaxLength(300)] public string? Unvan { get; set; }

    /// <summary>
    /// <see cref="VergiNo"/> alanındaki numaranın TÜRÜ: <c>TCKN</c> (11 hane,
    /// gerçek kişi) ya da <c>VKN</c> (10 hane, tüzel kişi).
    /// <para>
    /// ⚠ NEDEN AYRI KOLON, NEDEN İKİNCİ BİR NUMARA KOLONU DEĞİL: numara tek
    /// kolonda kalır (mevcut benzersizlik kısıtı, MASAK ve fatura yolları o
    /// kolonu okur; ikinci kolon aynı kişiyi iki kez benzersiz yapardı).
    /// Ayrım TÜR ile yapılır; hane doğrulaması bu türe bakar
    /// (<c>Kuyumcu.Core.Domain.KimlikNo</c>).
    /// </para>
    /// <para>
    /// <c>null</c> = tür beyan edilmemiş (göç öncesi kayıtlar). Bu hâlde hane
    /// doğrulaması numaranın uzunluğundan TÜRETİLİR, uydurulmaz.
    /// </para>
    /// </summary>
    [Column("kimlik_turu"), MaxLength(4)] public string? KimlikTuru { get; set; }

    /// <summary>Doğum tarihi — MASAK kimlik tespitinde istenen alan (KYC).</summary>
    [Column("dogum_tarihi")] public DateOnly? DogumTarihi { get; set; }

    /// <summary>Uyruk — ISO 3166-1 ALPHA-3 ("TUR", "DEU"; KESIF-P2 #22, MASAK formuyla tek standart). Boş = beyan yok.
    /// Migration UyrukAlpha3 eski 2 harfli değerleri eşledi; eşlenemeyen değer UYDURULMADI (olduğu gibi kaldı).</summary>
    [Column("uyruk"), MaxLength(3)] public string? Uyruk { get; set; }

    /// <summary>
    /// Bu cariye standart olarak uygulanan iskonto YÜZDESİ (0-100).
    /// <para>
    /// UX-S21: sistem bu oranı BİLİR — satış fişinde kasiyerden tekrar
    /// istenmez, fiş iskontosu boş bırakıldığında SUNUCU bu oranı uygular.
    /// </para>
    /// </summary>
    [Column("iskonto_yuzde", TypeName = "decimal(5,2)")] public decimal? IskontoYuzde { get; set; }

    /// <summary>Müşteri segmenti: <c>BIREYSEL</c> · <c>KURUMSAL</c> · <c>TOPTAN</c> · <c>VIP</c>.</summary>
    [Column("segment"), MaxLength(20)] public string? Segment { get; set; }

    // ── PAKET 40-A — PEP BEYANI (profil DEĞİL, BEYAN) ────────────────────────
    // ⚠ Bu üç kolon carinin O ANKİ beyanıdır ve TEK değerdir; hesaplanan risk
    //   SKORU tarihçelidir ve ayrı tabloya (cari_risk_profilleri) gider.
    // ⚠ PEP BEYANDIR, TESPİT DEĞİLDİR: sistem kimsenin siyasi nüfuz sahibi
    //   olup olmadığını kendi bilmez; müşterinin beyanını ve beyan anını tutar.
    /// <summary>Müşterinin "siyasi nüfuz sahibiyim" beyanı. Varsayılan: beyan yok.</summary>
    [Column("pep_beyani")] public bool PepBeyani { get; set; }
    /// <summary>Beyan edilen görev. <c>PepBeyani</c> true ise ZORUNLU (sunucu 400 döner).</summary>
    [Column("pep_gorev"), MaxLength(200)] public string? PepGorev { get; set; }
    /// <summary>Beyanın alındığı an — SUNUCUDA set edilir, istemciden alınmaz.</summary>
    [Column("pep_beyan_tarihi")] public DateTime? PepBeyanTarihi { get; set; }

    // ── PAKET P-26 (Y-37) — MUHASEBE KÖPRÜSÜ: cari muhasebe hesap kodu ──────
    /// <summary>
    /// Mali müşavirin hesap planındaki karşılık (ör. <c>120.01.0001</c>,
    /// <c>320.01.0001</c>). <b>SERBEST METİNDİR ve DOĞRULANMAZ</b>: tek düzen
    /// hesap planının hangi alt kırılımının kullanıldığı her mükellefte farklıdır;
    /// biçim koda gömülseydi ilk farklı planda kayıt reddedilirdi.
    /// <para>
    /// ⚠ BOŞSA UYDURULMAZ (Anayasa md.1 · P-26 DoD): muhasebe fişi bu carinin
    /// satırında hesap kodu sütununu <b>BOŞ</b> basar ve fişin özetinde kaç
    /// satırın kodsuz olduğunu söyler. "120" gibi bir varsayılan yazmak mali
    /// müşavire yanlış fiş göndermek olurdu.
    /// </para>
    /// </summary>
    [Column("hesap_kodu"), MaxLength(30)] public string? HesapKodu { get; set; }

    // ── PAKET B-3 (karar D2, K1–K5) — "PEŞİN MÜŞTERİ" SİSTEM CARİSİ ─────────
    /// <summary>
    /// Kiracı başına TEK, kimliksiz perakende satış kartı ("Peşin Müşteri").
    /// <para>
    /// ⚠ Silinemez, pasife alınamaz, bayrağı sonradan değiştirilemez ve bakiye
    /// TAŞIYAMAZ — kural <c>KuyumcuDbContext.SistemCarisiniKoru</c> içinde
    /// SaveChanges'ta uygulanır (servisi atlayan yol da takılır). Tekliği
    /// veritabanı kısıtıdır: <c>UQ_cariler_tenant_sistem_carisi</c>
    /// (fonksiyonel anahtar, migration B3PesinMusteriSistemCarisi).
    /// </para>
    /// <para>
    /// ⚠ Yalnız EŞİK ALTI şerittir: MASAK kimlik eşiğine ya da tevsik haddine
    /// ulaşan işlem bu cariyle SUNUCUDA 400 alır (fail-closed, K2).
    /// </para>
    /// </summary>
    [Column("sistem_carisi")] public bool SistemCarisi { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("cari_bakiyeler")]
public class CariBakiye : IKiraciVarlik
{
    [Key, Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tl_bakiye", TypeName = "decimal(15,2)")] public decimal TlBakiye { get; set; }
    [Column("has_gram_bakiye", TypeName = "decimal(15,6)")] public decimal HasGramBakiye { get; set; }

    // ── ÜÇÜNCÜ PARALEL DEFTER: DÖVİZ (paket 27 Faz A) ────────────────────
    // İŞARET TL/has ile AYNI: + = cari BİZE borçlu, - = BİZ cariye borçluyuz.
    // ⚠ Bu üç kolona doğrudan atama YALNIZ Modules/Cari/DovizBakiye.Ekle
    //   içinde yapılır (denetim.sh sayar); TL ve has ile ASLA toplanmaz.
    [Column("usd_bakiye", TypeName = "decimal(15,2)")] public decimal UsdBakiye { get; set; }
    [Column("eur_bakiye", TypeName = "decimal(15,2)")] public decimal EurBakiye { get; set; }
    [Column("gbp_bakiye", TypeName = "decimal(15,2)")] public decimal GbpBakiye { get; set; }
    [Column("son_islem_tarihi")] public DateTime? SonIslemTarihi { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("urunler")]
public class Urun : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("tip"), MaxLength(20)] public string Tip { get; set; } = null!;
    [Column("kod"), MaxLength(100)] public string Kod { get; set; } = null!;
    [Column("ad"), MaxLength(300)] public string Ad { get; set; } = null!;
    [Column("aciklama"), MaxLength(1000)] public string? Aciklama { get; set; }
    [Column("ayar_id"), MaxLength(10)] public string AyarId { get; set; } = null!;
    [Column("milyem", TypeName = "decimal(7,2)")] public decimal Milyem { get; set; }
    [Column("gram_agirlik", TypeName = "decimal(12,4)")] public decimal GramAgirlik { get; set; }
    /// <summary>
    /// Stok/satış birimi: ADET · GRAM (paket 21 — çift birim).
    /// GRAM üründe SÖZLEŞME: "1 adet = 1 gram" — <see cref="GramAgirlik"/> sunucuda 1'e
    /// zorlanır, <see cref="HasGrami"/> gram başına has olur; böylece stok (<c>adet</c>,
    /// decimal 12,3), <c>adet × HasGrami</c> ve <c>adet × birimFiyat</c> hesapları
    /// DEĞİŞMEDEN gramlı stoğu doğru taşır. İki ayrı hesap yolu açılsaydı has bacağı
    /// iki yerde iki farklı yuvarlama üretirdi.
    /// </summary>
    [Column("birim"), MaxLength(10)] public string Birim { get; set; } = "ADET";
    [Column("tas_agirlik", TypeName = "decimal(12,4)")] public decimal? TasAgirlik { get; set; }
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal HasGrami { get; set; }
    [Column("iscilik_tl", TypeName = "decimal(12,2)")] public decimal? IscilikTl { get; set; }
    /// <summary>Opsiyonel sabit satış fiyatı; boşsa satışta has×kur+işçilik hesaplanır.</summary>
    [Column("satis_fiyati", TypeName = "decimal(15,2)")] public decimal? SatisFiyati { get; set; }
    /// <summary>
    /// M1 (müşteri 16.10) — SATIŞ milyemi: doluysa satış = net gram × satış milyemi/1000 × has kuru;
    /// fark (satış − ayar milyemi) işçilik olarak ayrışır, liste marjı UYGULANMAZ. Boş = eski davranış.
    /// </summary>
    [Column("satis_milyemi", TypeName = "decimal(7,2)")] public decimal? SatisMilyemi { get; set; }
    /// <summary>
    /// M1b (müşteri 16.10: "eğer ben 925'den maliyetim varsa 925 yazacağım") — ALIŞ (maliyet) milyemi:
    /// maliyet = net gram × alış milyemi/1000 × has kuru. Satış milyemi boşsa satış da bununla yapılır
    /// ("bileziği kârsız satıyoruz"). Kâr = satış − maliyet; has değeri maliyet sayılmaz.
    /// </summary>
    [Column("alis_milyemi", TypeName = "decimal(7,2)")] public decimal? AlisMilyemi { get; set; }
    /// <summary>M1 — elle fiyat ofseti (± TL, birim başına): hesaplanan liste fiyatına eklenir. Boş = yok.</summary>
    [Column("fiyat_ofset_tl", TypeName = "decimal(12,2)")] public decimal? FiyatOfsetTl { get; set; }
    /// <summary>
    /// M2b (müşteri 16.10) — KDV'ye giren pay, BİNDE. Doluysa kuyum faturasında satırın
    /// toplam × binde/1000 kadarı KDV matrahı olur, KDV = matrah × KDV oranı, kalanı %0'a girer.
    /// Boş = yasal hesap (satış − has bedeli, KDVK 23/e).
    /// </summary>
    [Column("kdv_matrah_binde", TypeName = "decimal(7,3)")] public decimal? KdvMatrahBinde { get; set; }
    /// <summary>
    /// Kanonik parite kodu (CEYREK_YENI, YARIM_YENI, RESAT…). Doluysa ürünün
    /// piyasa fiyatı kur kaynağının o kalemine ait fiyattan okunur; boşsa
    /// has gram × kur + işçilik ile hesaplanır. Bkz. PariteKodlari.
    /// </summary>
    [Column("parite_kodu"), MaxLength(30)] public string? PariteKodu { get; set; }

    /// <summary>
    /// Kıymetli taş kökeni — Kuyum Ticareti Yönetmeliği md.11/(e)
    /// (RG 24.06.2026/33290). Değerler: <c>TAS_YOK</c> · <c>DOGAL</c> ·
    /// <c>SENTETIK</c> · <c>LABORATUVAR_URETIMI</c> · <c>YAPAY_URETIM</c>.
    /// <para>
    /// ⚠ Taş ağırlığı olan bir ürün kökeni belirtilmeden SATIŞA SUNULAMAZ.
    /// Mevcut kayıtlarda <c>null</c> olabilir (göç öncesi veri); satış anında
    /// fail-closed kontrol edilir.
    /// </para>
    /// </summary>
    [Column("tas_kokeni"), MaxLength(30)] public string? TasKokeni { get; set; }

    /// <summary>
    /// Ürün rodyum kaplamalı mı (paket 20, K20-7).
    ///
    /// <para>
    /// Rodyum bir STOK MADENİ DEĞİLDİR: tartılmaz, has hesabına girmez, ayarı
    /// yoktur. Beyaz altın/platin/paladyum yüzeye vurulan kaplamadır ve yalnız
    /// ETİKET/vitrin ibaresi olarak taşınır.
    /// </para>
    /// <para>
    /// ⚠ İbarenin etikette gösterilmesinin ZORUNLU olup olmadığı DOĞRULANMADI
    /// (Darphane ayar evi uygulamaları — VARSAYIM). Bu yüzden ibare
    /// yapılandırmadan okunur ve VARSAYILAN KAPALI gelir (ADR-0005); zorunluluk
    /// iddiası koda gömülmedi.
    /// </para>
    /// </summary>
    [Column("rodyum_kaplama")] public bool RodyumKaplama { get; set; }

    /// <summary>
    /// Ürünün MADENİ — ayarından TÜRETİLİR, ayrıca saklanmaz (tek kaynak
    /// <c>Ayarlar.MadenBul</c>, K20-8). Kolon açılsaydı ayar ile maden iki ayrı
    /// yerde tutulur ve biri güncellenip diğeri unutulabilirdi.
    /// </summary>
    [NotMapped] public string Maden =>
        Kuyumcu.Core.Domain.Ayarlar.MadenBul(AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>Madenin Türkçe görünen adı — ekran ham kod basmaz (UX-S31).</summary>
    [NotMapped] public string MadenAdi => Kuyumcu.Core.Domain.Maden.Ad(Maden);

    /// <summary>
    /// Etiket/fatura/vitrinde kullanılacak ibare. Yönetmelik "sentetik",
    /// "laboratuvar üretimi", "yapay üretim" ibarelerinden <b>en az birini</b>
    /// ister; hangisinin kullanılacağı işletmeye bırakılmıştır. Boşsa kökene
    /// karşılık gelen varsayılan ibare kullanılır.
    /// </summary>
    [Column("tas_ibaresi"), MaxLength(60)] public string? TasIbaresi { get; set; }

    // ── PIRLANTA 4C + sertifika (paket 22, PAKET-22-PIRLANTA-KARAR) ─────────
    /// <summary>Ana taş karatı. Tip PIRLANTA ise ZORUNLU; diğer tiplerde boş olmalı.</summary>
    [Column("pirlanta_karat", TypeName = "decimal(8,3)")] public decimal? PirlantaKarat { get; set; }
    /// <summary>D…Z (GIA renk skalası).</summary>
    [Column("pirlanta_renk"), MaxLength(2)] public string? PirlantaRenk { get; set; }
    /// <summary>FL · IF · VVS1 · VVS2 · VS1 · VS2 · SI1 · SI2 · I1 · I2 · I3</summary>
    [Column("pirlanta_berraklik"), MaxLength(5)] public string? PirlantaBerraklik { get; set; }
    /// <summary>EXCELLENT · VERY_GOOD · GOOD · FAIR · POOR</summary>
    [Column("pirlanta_kesim"), MaxLength(10)] public string? PirlantaKesim { get; set; }
    /// <summary>GIA · HRD · IGI · EGL · DIGER — sertifika no varsa zorunlu.</summary>
    [Column("sertifika_kurumu"), MaxLength(40)] public string? SertifikaKurumu { get; set; }
    [Column("sertifika_no"), MaxLength(60)] public string? SertifikaNo { get; set; }

    /// <summary>
    /// KMTS benzersiz kodu (Adım 8) — OPSİYONEL. Kıymetli Maden Takip Sistemi
    /// perakende kuyumcuyu BAĞLAMIYOR: usul ve esaslarda "kuyumcu"/"perakende"
    /// 0 kez geçiyor, yükümlü taraf rafineriler, işlenmiş takı kapsam dışı.
    /// Bu alan yalnız KAYIT amaçlıdır — gram altın/külçe gibi KMTS etiketli
    /// ürünün kodu saklanabilsin, kapsam genişlerse hazır olalım.
    /// Biçim DOĞRULANMAZ: şema bilinmiyor, tahmin edilmez.
    /// </summary>
    /// <summary>Çok seviyeli ürün ağacındaki yeri. Boşsa sınıflandırılmamış.</summary>
    [Column("kategori_id"), MaxLength(36)] public string? KategoriId { get; set; }

    /// <summary>Ürünün durduğu vitrin/kasa/depo. Bkz. <see cref="Reyon"/> kapsam sınırı.</summary>
    [Column("reyon_id"), MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>
    /// Konsinye mal mı? <b>true</b> ise bu mal bize ait DEĞİLDİR; vitrinde
    /// durur ama satılınca bedeli <see cref="KonsinyeCariId"/>'ye borçlanılır.
    /// Kendi stoğumuzla karıştırılırsa hem stok değeri hem kâr yanlış çıkar.
    /// </summary>
    [Column("konsinye")] public bool Konsinye { get; set; }

    /// <summary>Konsinye malın sahibi (tedarikçi/atölye cari).</summary>
    [Column("konsinye_cari_id"), MaxLength(36)] public string? KonsinyeCariId { get; set; }

    /// <summary>
    /// Konsinye malın satılması hâlinde sahibine ödenecek birim bedel.
    /// Kârımız = satış bedeli − bu tutar. Boşsa satışta borç doğmaz ve
    /// kâr olduğundan yüksek görünür — bu yüzden konsinye üründe ZORUNLUDUR.
    /// </summary>
    [Column("konsinye_birim_bedel", TypeName = "decimal(15,2)")] public decimal? KonsinyeBirimBedel { get; set; }

    // ── PAKET P-16 (Y-26) — ÜRÜN KARTININ EKSİK ALANLARI ────────────────────
    // Hepsi NULL olabilir; mevcut ürünler migration sonrası AYNI kalır.

    /// <summary>
    /// Bu ürünün tedarikçisi (cari). Konsinye sahibinden AYRIDIR:
    /// <see cref="KonsinyeCariId"/> malın SAHİBİDİR (mal bize ait değildir),
    /// tedarikçi malı BİZE SATANDIR (mal bizimdir). İkisi karıştırılırsa ya
    /// kendi malımız borçlanılır ya konsinye mal kâr sayılır.
    /// </summary>
    [Column("tedarikci_id"), MaxLength(36)] public string? TedarikciId { get; set; }

    /// <summary>
    /// Bu ürünün BİZE maliyeti (TL, birim başına).
    /// <para>
    /// ⚠ Kârlılık raporunun tek girdisidir: boşken kâr hesaplanamaz ve
    /// "0 maliyet" VARSAYILMAZ (Anayasa md.1) — rapor o satırı "maliyeti
    /// bilinmiyor" diye ayırır. P-25 bu alana bağlıdır.
    /// </para>
    /// </summary>
    [Column("alis_maliyeti", TypeName = "decimal(15,2)")] public decimal? AlisMaliyeti { get; set; }

    /// <summary>Hedeflenen kâr marjı YÜZDESİ (0-500). Fiyat önerisi için; satışı KISITLAMAZ.</summary>
    [Column("hedef_marj", TypeName = "decimal(6,2)")] public decimal? HedefMarj { get; set; }

    /// <summary>
    /// Bu ürüne uygulanan KDV oranı (yüzde).
    /// <para>
    /// ⚠ Boş = "ürün tipinden türet" (kuyum ürünlerinde özel matrah rejimi).
    /// Oran koda SABİT YAZILMAZ; boş bırakılırsa mevcut KDV motoru karar
    /// verir, bu kolon yalnız ELLE EZME yoludur.
    /// </para>
    /// </summary>
    [Column("kdv_orani", TypeName = "decimal(5,2)")] public decimal? KdvOrani { get; set; }

    /// <summary>Tekil ürünün seri numarası (saat, külçe, sertifikalı taş).</summary>
    [Column("seri_no"), MaxLength(60)] public string? SeriNo { get; set; }

    /// <summary>Yüzük/bilezik ölçüsü ya da zincir boyu — serbest metin ("17", "18 cm").</summary>
    [Column("olcu"), MaxLength(20)] public string? Olcu { get; set; }

    /// <summary>
    /// Kritik stok eşiği — stok bu adedin altına düşünce ürün "azalan stok"
    /// sayılır. <c>null</c> = eşik tanımlanmamış; sistem eşik UYDURMAZ.
    /// </summary>
    [Column("min_stok", TypeName = "decimal(12,3)")] public decimal? MinStok { get; set; }

    [Column("kmts_kodu"), MaxLength(100)] public string? KmtsKodu { get; set; }
    [Column("barkod"), MaxLength(50)] public string? Barkod { get; set; }
    /// <summary>
    /// D1-16 (18.10, rakip eşitleme: "toptancı barkodunu koruma") — ürünün üstünde toptancıdan gelen barkod.
    /// Tezgâh ve sayım okutmasında <see cref="Barkod"/> ile birlikte aranır; ikisi de kiracıda tekildir.
    /// </summary>
    [Column("tedarikci_barkodu"), MaxLength(50)] public string? TedarikciBarkodu { get; set; }
    [Column("rfid"), MaxLength(100)] public string? Rfid { get; set; }
    [Column("gorsel_url"), MaxLength(500)] public string? GorselUrl { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("stoklar")]
public class Stok : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("urun_id"), MaxLength(36)] public string UrunId { get; set; } = null!;
    [Column("adet", TypeName = "decimal(12,3)")] public decimal Adet { get; set; }
    [Column("toplam_has_gram", TypeName = "decimal(15,6)")] public decimal ToplamHasGram { get; set; }

    /// <summary>
    /// PAKET P-21 (Y-23) — REZERVE (bloke) adet: fiziken raftadır ama SÖZ
    /// VERİLMİŞTİR; satılabilir değildir.
    ///
    /// <para>⚠ NEDEN AYRI KOLON, NEDEN <see cref="Adet"/>'TEN DÜŞÜLMEDİ: bloke
    /// mal hâlâ dükkândadır. Adet'ten düşülseydi sayım raporu "eksik" derdi ve
    /// tezgâhtar var olan malı arayıp bulamazdı. Ayrı kolon sayımı bozmadan
    /// satışı engeller: <b>satılabilir = adet − bloke_adet</b>
    /// (<see cref="BlokeKapisi.Satilabilir"/>).</para>
    ///
    /// <para>⚠ DEĞİŞMEZ (BLOKE_TUTARSIZ): <c>0 ≤ bloke_adet ≤ adet</c>. Bu alan
    /// yalnız <see cref="BlokeKapisi"/> üzerinden yazılır; denetim.sh kapı dışı
    /// atamayı sayar.</para>
    /// </summary>
    [Column("bloke_adet", TypeName = "decimal(12,3)")] public decimal BlokeAdet { get; set; }

    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("hareketler")]
public class Hareket : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    /// <summary>TL bacağı — pozitif giriş, negatif çıkış.</summary>
    [Column("tl_tutari", TypeName = "decimal(15,2)")] public decimal TlTutari { get; set; }
    /// <summary>
    /// PAKET YASAL-3 (§ 4.1) — MASAK ÖLÇÜSÜ: bu satırda EL DEĞİŞTİREN değerin TL
    /// karşılığı (işaretsiz). Yalnız <see cref="TlTutari"/>'nın el değiştiren değeri
    /// taşımadığı türlerde dolar: TAKAS = Max(eski, yeni) (defterde fark yazılır),
    /// HURDA_ALIS = gider pusulasının BRÜT bedeli (defterde stopaj sonrası net).
    /// Diğer türlerde NULL — kümülatif <c>ABS(tl_tutari)</c> okur. Tevsik bu kolonu
    /// OKUMAZ (nakit payı <see cref="NakitTutar"/>'dadır). Geçmiş takas satırlarında
    /// brüt değer defterde olmadığı için NULL kalır (uydurulmaz).
    /// </summary>
    [Column("masak_olcu_tl", TypeName = "decimal(15,2)")] public decimal? MasakOlcuTl { get; set; }
    /// <summary>
    /// Bu satırın DÖVİZ bacağının para birimi.
    /// <para>
    /// ⚠ Paket 27'ye kadar bu kolon fiilen HEP "TRY" idi (hiçbir modül
    /// yazmıyordu, tek okuyucu CSV dışa aktarımıydı). Artık
    /// <c>DOVIZ_VERESIYE</c> / <c>DOVIZ_TAHSILAT</c> satırlarında USD·EUR·GBP
    /// değerini taşır ve <see cref="DovizTutar"/> ile BİRLİKTE anlamlıdır.
    /// </para>
    /// </summary>
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";

    /// <summary>
    /// EFEKTİF döviz miktarı — İŞARETLİ, <see cref="TlTutari"/> ile AYNI yön
    /// (+ alacak doğuran, − borç kapatan). <c>null</c> = bu satırın döviz
    /// bacağı YOK (bugünkü tüm TL/has satırları).
    ///
    /// <para>
    /// ⚠ <b>ÇİFTE SAYMA UYARISI (K27-E):</b> döviz satırının
    /// <see cref="TlTutari"/>'si de doludur (efektifin o günkü TL karşılığı) —
    /// çünkü MASAK eşiği ve 459 s. VUK tevsik haddi TL üzerinden işler. Bu
    /// yüzden döviz satırı bir TL BORCU DEĞİLDİR ve öyle sayılamaz:
    /// <see cref="VeresiyeTutar"/> daima 0'dır, <see cref="Tur"/> asla
    /// "TAHSILAT" değildir (<c>HareketFabrikasi</c> bunu kapı olarak zorlar),
    /// böylece <c>VadeKapisi.AcikBorclar</c> hiçbir döviz satırını TL borcu
    /// saymaz. TL toplayan YENİ bir sorgu yazan herkes bu kuralı bilmelidir.
    /// </para>
    /// </summary>
    [Column("doviz_tutar", TypeName = "decimal(15,2)")] public decimal? DovizTutar { get; set; }

    /// <summary>Has bacağı — pozitif artış, negatif azalış.</summary>
    [Column("has_grami", TypeName = "decimal(15,6)")] public decimal HasGrami { get; set; }

    /// <summary>
    /// Bu satırın HAS BACAĞININ madeni (paket 20, K20-1..K20-3).
    ///
    /// <para>
    /// ⚠ <b><see cref="HasGrami"/> BU MADENİN gramıdır; ALTIN dışı gramlar altın
    /// toplamına KATILMAZ.</b> Bu kolon gelmeden önce 100 g gümüş emaneti ile
    /// 100 g altın emaneti defterde AYNI sayıydı ve raporlar ikisini ayırmadan
    /// topluyordu — 1 g gümüş ile 1 g altın aynı şey değildir.
    /// </para>
    /// <para>
    /// TL-only satırlarda (TAHSILAT, ISCILIK, VERESIYE) has bacağı 0'dır ve bu
    /// kolonun değeri bir İDDİA DEĞİLDİR — yalnız doldurulmamış varsayılandır.
    /// Has bacağı sıfırdan farklıysa <c>MadenKapisi.HareketDogrula</c>
    /// (HareketFabrikasi içinde) geçerli maden olmadan yazmayı ENGELLER.
    /// </para>
    /// <para>
    /// GERİ DOLDURMA VARSAYIMI (migration <c>HareketMaden</c>): mevcut satırların
    /// madeni ÜRÜNÜN BUGÜNKÜ AYARINDAN türetildi; ürünü olmayan satırlar 'ALTIN'
    /// bırakıldı. Ürün ayarı satıştan SONRA değiştiyse geçmiş satır yanlış
    /// etiketlenmiş olabilir — bu bir varsayımdır, ölçülmüştür ve sevk raporunda
    /// sayısıyla yazılıdır (Anayasa md.1).
    /// </para>
    /// </summary>
    [Column("maden"), MaxLength(10)] public string Maden { get; set; } = Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>
    /// PAKET P-DIL-BORC-KASA (Y18) — emanetin BIRAKILDIĞI GİBİ (brüt) gramı,
    /// işaretli: giriş +, iade −, storno kaynağın tersi. Yalnız EMANET_GIRIS / EMANET_CIKIS
    /// satırında dolar; ekran emaneti "22 ayar · 12,50 g" diye gösterir, has ikincildir.
    /// Bakiye ve kapılar DEĞİŞMEZ: emanetin hesabı yine <see cref="HasGrami"/> üzerinden yürür.
    /// Eski satırlarda (bu kolondan önce) NULL — ekran o kişide has gösterimine düşer.
    /// </summary>
    [Column("emanet_gram", TypeName = "decimal(15,3)")] public decimal? EmanetGram { get; set; }

    /// <summary>PAKET P-DIL-BORC-KASA (Y18) — emanet girişinde bildirilen taş ağırlığı (işaretli, gram ile aynı yön); yoksa NULL.</summary>
    [Column("emanet_tas", TypeName = "decimal(15,3)")] public decimal? EmanetTas { get; set; }

    /// <summary>PAKET P-DIL-BORC-KASA (Y18) — <see cref="EmanetGram"/>'ın ayarı (Ayarlar kimliği, ör. K22).</summary>
    [Column("emanet_ayar_id"), MaxLength(10)] public string? EmanetAyarId { get; set; }
    /// <summary>
    /// Peşin/veresiye ayrımı: NAKIT · KART · HAVALE · ALTIN · VERESIYE · KARMA.
    /// Bakiyelerin append-only defterden yeniden türetilebilmesi için zorunludur
    /// (Bertan kararı, 2026-09-10); eski kayıtlarda boş olabilir.
    /// </summary>
    [Column("odeme_turu"), MaxLength(10)] public string? OdemeTuru { get; set; }

    /// <summary>
    /// Bu hareketle <b>NAKİT</b> olarak el değiştiren TL — İŞARETSİZ (≥ 0).
    ///
    /// ⚠ NEDEN VAR: <see cref="OdemeTuru"/> tek bir etikettir. "KARMA" (kısmi
    /// ödeme) ya da ileride çok bacaklı fişte nakit payı bu etiketten
    /// çıkarılamaz; 459 s. VUK Genel Tebliği kümülatif nakit toplamı
    /// (<c>TevsikKapisi.GunlukNakitToplam</c>) o payı GÖREMEZDİ. 20.000 nakit
    /// + 20.000 kart şeklinde bölünen ödeme 30.000 TL haddini sessizce
    /// deliyordu — ceza alıcı ve satıcıya BİRDEN kesilir. Bu alan o açığı kapatır.
    ///
    /// <c>null</c> = BİLİNMİYOR ve YALNIZ bu alan eklenmeden önce yazılmış,
    /// payı geri kazanılamayan satırlar içindir. Yeni kod ASLA null yazmaz;
    /// nakit yoksa <c>0</c> yazar (<c>HareketFabrikasi</c> bunu zorlar).
    /// </summary>
    [Column("nakit_tutar", TypeName = "decimal(15,2)")] public decimal? NakitTutar { get; set; }

    /// <summary>
    /// Bu hareketle cariye <b>VERESİYE</b> yazılan TL — İŞARETSİZ (≥ 0).
    /// Nakit için alan ekleyip alacak için eklememek asimetrik olurdu:
    /// <see cref="OdemeTuru"/>'nun var oluş gerekçesi bakiyelerin append-only
    /// defterden yeniden türetilebilmesidir; kısmi ödemede veresiye payı da
    /// etiketten çıkarılamaz. Null anlamı <see cref="NakitTutar"/> ile aynıdır.
    /// </summary>
    [Column("veresiye_tutar", TypeName = "decimal(15,2)")] public decimal? VeresiyeTutar { get; set; }

    /// <summary>
    /// ÇOK BACAKLI ÖDEME FİŞİNİN grup kimliği (ULID).
    ///
    /// <b>DOLU</b> ⇒ bu hareket, tek seferde birden fazla ödeme aracıyla kapatılan
    /// bir fişin kalemidir. <see cref="OdemeTuru"/> o fişte yalnız ÖZET etikettir
    /// ("KARMA"); gerçek kırılım <see cref="NakitTutar"/>, <see cref="VeresiyeTutar"/>
    /// ve fişe bağlı <c>kasa_hareketleri</c> satırlarındadır.
    /// <b>BOŞ</b> ⇒ tek bacaklı eski model; hiçbir davranış değişmez.
    ///
    /// ⚠ <c>fisler</c> diye AYRI BİR TABLO YOKTUR ve bilinçlidir: tevsik kümülatif
    /// toplamının tek kaynağı yeni bir tabloya taşınsaydı, o tabloya satır yazmayan
    /// her nakit yolu (takas, işçilik, tahsilat, tüm eski kayıtlar) toplamdan
    /// düşerdi — açık kapanacakken BÜYÜRDÜ. Fiş, aynı <c>fis_id</c> değerini
    /// taşıyan hareket kümesidir.
    /// </summary>
    [Column("fis_id"), MaxLength(36)] public string? FisId { get; set; }

    /// <summary>
    /// İADE hareketinin TERSİNE ÇEVİRDİĞİ hareket.
    ///
    /// <c>SATIS_IADE</c> satırı hangi satışı, <c>ALIS_IADE</c> satırı hangi
    /// alışı iade ettiğini burada taşır. Bağ olmadan "bu maldan ne kadarı zaten
    /// iade edilmiş" sorusu cevaplanamaz ve aynı satış defalarca iade
    /// edilebilirdi. Maliyet partisi de bu bağ üzerinden geri konur: iade
    /// edilen mal, ÇIKTIĞI maliyetle geri girer — bugünkü maliyetle değil.
    /// </summary>
    [Column("iade_kaynak_hareket_id"), MaxLength(36)] public string? IadeKaynakHareketId { get; set; }

    /// <summary>
    /// STORNO hareketinin HÜKÜMSÜZ KILDIĞI hareket (paket 48 Faz A).
    ///
    /// ⚠ <see cref="IadeKaynakHareketId"/> ile AYRI kolondur ve aynı satırda
    /// ASLA birlikte dolmaz. Fark ticari değil hukukidir:
    ///   • İADE = ticari olgu — müşteri malı geri getirdi, ciro gerçekten oldu
    ///     ve geri döndü; kalan iade hakkı bu bağ üzerinden sayılır.
    ///   • STORNO = HATA DÜZELTMESİ — o işlem hiç olmamalıydı; net etkisi
    ///     sıfırdır ve ciro/kâr raporunda iade gibi sayılmaz (Eray kapısı).
    /// Aynı kolona binseydi, bir kayıt hatası müşterinin iade hakkını tüketir
    /// ve ciro raporunu şişirirdi.
    ///
    /// <c>null</c> = bu satır bir storno değildir. Storno satırının KENDİSİ
    /// stornolanamaz; kaynak satır ise en fazla BİR kez stornolanır
    /// (<c>StornoServisi</c> ikisini de 409 ile reddeder).
    /// </summary>
    [Column("storno_kaynak_id"), MaxLength(36)] public string? StornoKaynakId { get; set; }

    /// <summary>
    /// PAKET P-20b (K-13) — TAHLİL FARKI BACAĞININ KARŞI BACAĞI: farkı doğuran
    /// <c>HURDA_SATIS</c> satırının kimliği.
    ///
    /// <para>
    /// <c>null</c> = bu satır bir tahlil farkı bacağı DEĞİLDİR.
    /// <c>Tur == "TAHLIL_FARKI"</c> olan satırda ise ASLA null olamaz: fark,
    /// hangi hurda satışının mutabakatından doğduğu bilinmeden anlamsızdır ve
    /// öksüz kalan bir fark bacağı has bakiyesini sessizce kaydırır.
    /// </para>
    /// <para>
    /// ⚠ Bu alana YALNIZ <see cref="TahlilFarkiFabrikasi"/> yazar (denetim.sh
    /// <c>TAHLIL_FARKI_ESSIZ_BACAK</c> sayacı fabrika dışı atamayı 0'da tutar);
    /// <see cref="StornoKaynakId"/> / <see cref="IadeKaynakHareketId"/> ile AYNI
    /// satırda birlikte dolmaz — üçü üç ayrı ilişkidir (düzeltme · ticari iade ·
    /// ölçüm mutabakatı).
    /// </para>
    /// </summary>
    [Column("tahlil_kaynak_hareket_id"), MaxLength(36)] public string? TahlilKaynakHareketId { get; set; }

    /// <summary>
    /// Bu hareketle doğan VERESİYE borcunun VADE GÜNÜ — yerel takvim günü.
    ///
    /// ⚠ NEDEN <c>date</c>: vade bir AN değil, bir TAKVİM GÜNÜdür. UTC damgası
    /// olarak tutulsaydı İstanbul'da 20 Eylül'e verilen vade, gün sınırı
    /// kayması yüzünden raporda 19 Eylül görünebilirdi (K5 ile aynı tuzak).
    ///
    /// <c>null</c> = bu hareket borç DOĞURMUYOR (peşin, iade, tahsilat) ya da
    /// bu alan eklenmeden önce yazılmış eski bir satır. İkisi karışmasın diye
    /// vade takibi raporu, vadesi bilinmeyen açık bakiyeyi ayrı kalemde
    /// ("vadesizBakiye") gösterir — sessizce bugüne yazmaz.
    /// </summary>
    [Column("vade_tarihi")] public DateOnly? VadeTarihi { get; set; }

    /// <summary>
    /// Satış anında bu ürüne uygulanan LİSTE fiyatı (birim, indirim ÖNCESİ). FEY
    /// md.11 "indirimli ve indirim öncesi fiyat birlikte" kuralının kaynağı; ayrıca
    /// TRHTUY md.14 "son 30 günün en düşük fiyatı" bu satırlardan türetilir — ayrı bir
    /// fiyat geçmişi tablosu yoktur, defter tek kaynaktır. Eski satırlarda null.
    /// </summary>
    [Column("liste_fiyati", TypeName = "decimal(15,2)")] public decimal? ListeFiyati { get; set; }

    /// <summary>Kalemin TAMAMINA uygulanan indirim (TL, ≥ 0). Hediyede liste fiyatı × adet.</summary>
    [Column("indirim_tutari", TypeName = "decimal(15,2)")] public decimal? IndirimTutari { get; set; }

    /// <summary>İndirimi doğuran kampanya; kasiyer elle indirdiyse boş.</summary>
    [Column("kampanya_id"), MaxLength(36)] public string? KampanyaId { get; set; }

    /// <summary>
    /// Satışın yanında BEDELSİZ verilen mal (promosyon). KDV GUT I/B-10.1: yanında
    /// bedelli mal varsa bedelsiz teslim değildir; tek başına hediye KDVK md.27
    /// emsal bedel doğurur ve sistem onu KABUL ETMEZ (fiş kapısı).
    /// </summary>
    [Column("hediye")] public bool Hediye { get; set; }

    [Column("kur_snapshot_id"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("urun_id"), MaxLength(36)] public string? UrunId { get; set; }
    [Column("karsi_sube_id"), MaxLength(36)] public string? KarsiSubeId { get; set; }
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("fatura_no"), MaxLength(50)] public string? FaturaNo { get; set; }

    /// <summary>İşleme konu KMTS etiketli ürünün kodu — opsiyonel (Adım 8).</summary>
    /// <summary>Çok seviyeli ürün ağacındaki yeri. Boşsa sınıflandırılmamış.</summary>
    [Column("kategori_id"), MaxLength(36)] public string? KategoriId { get; set; }

    /// <summary>Ürünün durduğu vitrin/kasa/depo. Bkz. <see cref="Reyon"/> kapsam sınırı.</summary>
    [Column("reyon_id"), MaxLength(36)] public string? ReyonId { get; set; }

    /// <summary>
    /// Konsinye mal mı? <b>true</b> ise bu mal bize ait DEĞİLDİR; vitrinde
    /// durur ama satılınca bedeli <see cref="KonsinyeCariId"/>'ye borçlanılır.
    /// Kendi stoğumuzla karıştırılırsa hem stok değeri hem kâr yanlış çıkar.
    /// </summary>
    [Column("konsinye")] public bool Konsinye { get; set; }

    /// <summary>Konsinye malın sahibi (tedarikçi/atölye cari).</summary>
    [Column("konsinye_cari_id"), MaxLength(36)] public string? KonsinyeCariId { get; set; }

    /// <summary>
    /// Konsinye malın satılması hâlinde sahibine ödenecek birim bedel.
    /// Kârımız = satış bedeli − bu tutar. Boşsa satışta borç doğmaz ve
    /// kâr olduğundan yüksek görünür — bu yüzden konsinye üründe ZORUNLUDUR.
    /// </summary>
    [Column("konsinye_birim_bedel", TypeName = "decimal(15,2)")] public decimal? KonsinyeBirimBedel { get; set; }

    [Column("kmts_kodu"), MaxLength(100)] public string? KmtsKodu { get; set; }

    /// <summary>
    /// Bu satırın KAÇ BİRİM mala dayandığı (paket P-08 / Y-14).
    ///
    /// <para>
    /// ⚠ <c>null</c> = <b>BİLİNMİYOR</b>, "1" ya da "0" DEĞİL. Bu kolon
    /// açılmadan önce yazılmış her satır null kalır ve geri DOLDURULMAZ:
    /// gramlı üründe "1 adet = 1 gram" sözleşmesi (A15) yüzünden geçmiş bir
    /// SATIS satırının adedi <see cref="HasGrami"/>'den güvenle türetilemez.
    /// Uydurulmuş bir adet, adetsizlikten daha kötüdür (Anayasa md.1) — rapor
    /// bu satırlar için "bilinmiyor" yazar, varsayım ÜRETMEZ.
    /// </para>
    /// <para>
    /// İŞARETSİZ (≥ 0): yön <see cref="TlTutari"/> ve <see cref="HasGrami"/>
    /// kolonlarındadır; adedin de işaretlenmesi aynı yönü iki kez saymak olurdu.
    /// Kapı <c>HareketFabrikasi.Ekle</c> içindedir.
    /// </para>
    /// </summary>
    [Column("adet", TypeName = "decimal(12,3)")] public decimal? Adet { get; set; }

    /// <summary>
    /// İşi YAPAN personel — <b>SATAN</b> (paket P-08 / Y-13).
    ///
    /// <para>
    /// ⚠ <see cref="KullaniciId"/> ile AYRI kolondur ve karıştırılamaz:
    /// <see cref="KullaniciId"/> kaydı <b>GİREN</b> oturumdur (vezne bir kişi,
    /// satan başka kişi olabilir). Personel raporu bugüne dek "KAYDEDEN" diyordu
    /// ve prim kararı bu sayıya dayandırılamıyordu — bu kolon o boşluğu kapatır.
    /// </para>
    /// <para>
    /// ANLAM KİLİDİ: bu kolon YALNIZ "satan"dır. İkinci bir anlam (tahsil eden,
    /// onaylayan, teslim eden) buraya yazılamaz; gerekirse AYRI kolon açılır.
    /// Kilidin kod karşılığı <see cref="Sabitler.HareketPersonelRolu"/> sabiti ve
    /// <c>denetim.sh</c> P-08 sayacıdır.
    /// </para>
    /// <para>
    /// <c>null</c> = BİLİNMİYOR (bu kolondan önceki satırlar ya da satanı
    /// seçilmemiş fiş). Prim <b>hesabı</b> 1.1'dedir (D-2); bu paket yalnız
    /// alanı, indeksi ve anlam kilidini kurar.
    /// </para>
    /// </summary>
    [Column("personel_id"), MaxLength(36)] public string? PersonelId { get; set; }

    /// <summary>
    /// PAKET P-21 (Y-18) — bu defter satırını DOĞURAN sipariş.
    ///
    /// <para>⚠ NEDEN VAR: sipariş modülü bugüne dek deftere hiç yazmıyordu
    /// (Siparis.cs başlığı: "DEFTERE YAZMAZ"). Sipariş → satış dönüşümü o
    /// duvarı bilinçli olarak yıkar; yıkarken bağ kurulmazsa "bu satış hangi
    /// siparişin karşılığıydı" sorusu bir daha cevaplanamaz ve aynı sipariş
    /// ikinci kez satışa çevrilebilirdi. Dönüşüm kapısı
    /// (<c>SiparisSatisKapisi</c>) bu kolona bakarak çift dönüşümü reddeder.</para>
    ///
    /// <para>⚠ Sipariş <b>modülü</b> hâlâ kendi başına deftere yazmaz; yazan
    /// yalnız dönüşüm ucudur ve o da <c>HareketFabrikasi.Ekle</c>'den geçer.</para>
    /// </summary>
    [Column("siparis_id"), MaxLength(36)] public string? SiparisId { get; set; }

    /// <summary>
    /// PAKET CIFT-BACAK (tur 2, hasım KRİTİK) — TAKASTA MÜŞTERİDEN ALINAN eski
    /// altının has'ı, İŞLEM ANINDA donmuş hâliyle. Yalnız <c>TAKAS</c> satırında dolar.
    ///
    /// <para>
    /// ⚠ NEDEN KOLON: <see cref="HasGrami"/> takasta NET has'tır (eski − yeni).
    /// Hurdaya giren eski has önce "net + adet × ürünün has'ı" diye TÜRETİLİYORDU;
    /// ürün kartı sonradan düzenlenince (gram/ayar PATCH) geçmiş hurda bakiyesi
    /// kayıyordu (fiziken olmayan has satılabilirdi). Defter satırı ürün kartına
    /// BAĞLANAMAZ — değer yazıldığı anda sabitlenir ve bir daha değişmez.
    /// </para>
    /// <para>
    /// <c>null</c> = BİLİNMİYOR (bu kolondan önce yazılmış TAKAS satırı). Uydurulmaz
    /// (Anayasa md.1): böyle satır hurda bakiyesine GİRMEZ (bugünkü davranış).
    /// Tek okuyucu <c>HurdaStokKapisi.TakasEskiBacaklari</c>'dır.
    /// </para>
    /// </summary>
    [Column("takas_eski_has_grami", TypeName = "decimal(15,6)")] public decimal? TakasEskiHasGrami { get; set; }

    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("kasalar")]
public class Kasa : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("bakiye", TypeName = "decimal(15,2)")] public decimal Bakiye { get; set; }
    /// <summary>
    /// D1-11 (18.10, rakip eşitleme: "çalışan kasası") — kasanın sorumlu çalışanı. Tezgâhta bu çalışanın
    /// varsayılan kasası budur; boşsa eski davranış (şubenin ilk TL kasası).
    /// </summary>
    [Column("sorumlu_kullanici_id"), MaxLength(36)] public string? SorumluKullaniciId { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("kasa_hareketleri")]
public class KasaHareket : IKiraciVarlik, IBacakliHareket
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("odeme_turu"), MaxLength(10)] public string OdemeTuru { get; set; } = null!;
    [Column("komisyon_oran", TypeName = "decimal(5,4)")] public decimal? KomisyonOran { get; set; }

    /// <summary>
    /// PAKET S-3b — elle girilen kasa hareketinin AÇIKLAMASI.
    /// ⚠ Bu alan yoktu: <c>KasaHareketDto.Aciklama</c> ekrandan gönderiliyor ama
    /// hiçbir yere yazılmıyordu (sessiz veri kaybı — QA turu 3, /kasalar).
    /// Kullanıcı "kira ödemesi" yazıp kaydediyor, geçmişte hiçbir sebep görünmüyordu.
    /// </summary>
    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }

    /// <summary>
    /// PAKET S-3b — bu satır bir BAŞKA kasa hareketini hükümsüz kılan ters
    /// kayıtsa, kaynağın kimliği. Defter yerinde değişmez (ADR-0004): elle
    /// girilmiş kasa hareketinin düzeltmesi de ters satırın VARLIĞINDAN okunur.
    /// </summary>
    [Column("storno_kaynak_id"), MaxLength(36)] public string? StornoKaynakId { get; set; }

    /// <summary>
    /// PAKET P-19 (Y-12) — bu nakit çıkışının GİDER KATEGORİSİ.
    /// <para>
    /// null = bu satır bir gider DEĞİLDİR (satış tahsilatı, virman bacağı,
    /// sayım farkı…). Gider kâr-zararda AYRI satır olur; kategorisiz bir çıkış
    /// "bilinmeyen gider" diye uydurulmaz, gider sayılmaz.
    /// </para>
    /// </summary>
    [Column("gider_kategori_id"), MaxLength(36)] public string? GiderKategoriId { get; set; }

    /// <summary>
    /// PAKET P-19 (Y-11) — VİRMANIN KARŞI BACAĞININ SATIR KİMLİĞİ.
    ///
    /// <para>⚠ <b>`karsi_sube_id` İLE KARIŞTIRILMAZ</b> (anlam ayrımı, plan DoD):</para>
    /// <list type="bullet">
    /// <item><c>hareketler.karsi_sube_id</c> → <b>ŞUBE</b> kimliğidir, <b>MAL</b>
    /// hareketinin (TRANSFER) gittiği şubeyi söyler, <c>hareketler</c> tablosundadır
    /// ve <b>tek satırlıdır</b> (karşı şubede ikinci bir satır doğurmaz).</item>
    /// <item><c>karsi_bacak_id</c> → <b>SATIR</b> kimliğidir, <b>PARA</b> hareketinin
    /// (VİRMAN) diğer yarısını gösterir, <c>kasa_hareketleri</c> /
    /// <c>banka_hareketleri</c> tablolarındadır ve <b>daima çifttir</b>: A.karsi =
    /// B.id ve B.karsi = A.id. Tek başına duran bacak bir VERİ HATASIDIR.</item>
    /// </list>
    /// <para>
    /// ⚠ Bu alana YALNIZ <see cref="CiftBacakFabrikasi"/> yazar (denetim.sh
    /// <c>ESSIZ_BACAK</c> sayacı fabrika dışı atamayı 0'da tutar); çünkü tek
    /// bacağı yazıp diğerini unutan bir yol, kasa bakiyesini sessizce çift ya da
    /// eksik sayardı.
    /// </para>
    /// </summary>
    [Column("karsi_bacak_id"), MaxLength(36)] public string? KarsiBacakId { get; set; }

    [Column("blokaj_bitis")] public DateTime? BlokajBitis { get; set; }
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

/// <summary>
/// PAKET P-19 (Y-12) — GİDER/MASRAF KATEGORİSİ (kira, elektrik, personel…).
/// <para>
/// Soyut bir kayıttır: kendi başına para hareketi doğurmaz, nakit çıkışını
/// SINIFLANDIRIR. Silinmez — pasife alınır (UX-S25); geçmiş giderlerin
/// kategorisi yerinde kalsın.
/// </para>
/// </summary>
[Table("gider_kategorileri")]
public class GiderKategorisi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("aciklama"), MaxLength(500)] public string? Aciklama { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("banka_hesaplari")]
public class BankaHesap : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_adi"), MaxLength(100)] public string BankaAdi { get; set; } = null!;
    [Column("hesap_adi"), MaxLength(100)] public string HesapAdi { get; set; } = null!;
    [Column("hesap_sahibi"), MaxLength(150)] public string? HesapSahibi { get; set; }
    [Column("iban"), MaxLength(34)] public string Iban { get; set; } = null!;
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("bakiye", TypeName = "decimal(15,2)")] public decimal Bakiye { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    /// <summary>
    /// M7 (müşteri 16.10: "düşer düşmez hemen şak ekranda görüyoruz") — hareketleri kendiliğinden çeken
    /// sağlayıcı (<see cref="Modules.Banka.BankaSaglayicilari"/>): <c>null</c> = elle/ekstre (bugünkü davranış),
    /// <c>SAHTE</c> = demo bankası. Gerçek banka bağlantısı (API/HBH) aynı arayüze takılır.
    /// </summary>
    [Column("saglayici_kodu"), MaxLength(20)] public string? SaglayiciKodu { get; set; }
    /// <summary>Çekicinin bu hesapta son başarılı turu (UTC).</summary>
    [Column("son_cekim")] public DateTime? SonCekim { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("banka_hareketleri")]
public class BankaHareket : IKiraciVarlik, IBacakliHareket
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("banka_hesap_id"), MaxLength(36)] public string BankaHesapId { get; set; } = null!;
    [Column("yon"), MaxLength(10)] public string Yon { get; set; } = null!;
    [Column("tutar", TypeName = "decimal(15,2)")] public decimal Tutar { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("karsi_taraf_ad"), MaxLength(150)] public string? KarsiTarafAd { get; set; }
    [Column("karsi_iban"), MaxLength(34)] public string? KarsiIban { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }
    [Column("fatura_id"), MaxLength(36)] public string? FaturaId { get; set; }

    /// <summary>
    /// Bu dekontun <c>hareketler</c> defterindeki karşılığı (paket 41).
    /// <para>
    /// ⚠ <b>BUGÜN HER ZAMAN NULL</b> — tüm dekontlar elle girilir, otomatik
    /// dekont üreten bir akış YOKTUR. Kolon "ölü" görünür ama SİLİNMEZ:
    /// MASAK kümülatif toplamı (<c>MasakKapisi.GunlukIslemToplam</c>) banka
    /// kümesini <c>hareket_id IS NULL</c> süzgeciyle okur; süzgeç ilk günden
    /// kodda olmalıdır. Kolon ve süzgeç sonradan eklenirse, arada eklenen bir
    /// otomatik dekont akışı (banka ekstresi içe aktarma — paket 32, havale
    /// bacağının otomatik dekontu) aynı parayı hem hareketler hem
    /// banka_hareketleri tarafından saydırır ve MASAK eşiği SESSİZCE çift
    /// sayılır. Bugün süzgeç etkisizdir (hepsi null) ve davranışı değiştirmez.
    /// </para>
    /// Doldurulması AYRI bir karardır (paket 32) — bu pakette daima null yazılır.
    /// </summary>
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }

    /// <summary>
    /// Bu dekontu doğuran ekstre sahne satırı (paket 32). Elle girilen
    /// dekontlarda null kalır — "bu satır dosyadan mı geldi, elden mi" sorusu
    /// denetimde bu kolonla cevaplanır.
    /// </summary>
    [Column("ekstre_satir_id"), MaxLength(36)] public string? EkstreSatirId { get; set; }

    /// <summary>Bankanın kendi işlem/dekont referansı (ekstrede varsa).</summary>
    [Column("banka_referans"), MaxLength(50)] public string? BankaReferans { get; set; }

    /// <summary>
    /// Mükerrer koruması (paket 32). SHA-256(hesap|tarih|tutar|yön|referans|IBAN|açıklama).
    /// <para>
    /// ⚠ ELLE GİRİLEN DEKONTTA DA DOLDURULUR. Sebep: ekstre yüklendiğinde
    /// "bunu zaten elle girmiştim" ancak elle girilen satırın da anahtarı
    /// varsa görülebilir; yalnız ekstreden gelenler anahtarlansaydı en sık
    /// mükerrer senaryosu (aynı havaleyi hem elle gir hem dosyadan al)
    /// yakalanmazdı.
    /// </para>
    /// <para>
    /// ⚠ SINIR — DÜRÜSTÇE: (tenant_id, banka_hesap_id, dedupe_anahtar)
    /// BENZERSİZ indeksi MySQL'de NULL değerleri kısıtlamaz. Paket 32 öncesi
    /// yazılmış satırların anahtarı NULL'dur ve migration onları GERİYE
    /// DOLDURMAZ. Yani indeks GEÇMİŞE KARŞI KORUMA SAĞLAMAZ; "mükerrer
    /// imkânsız" denemez, "eski kayıtlar kapsam dışı" denir.
    /// </para>
    /// </summary>
    [Column("dedupe_anahtar"), MaxLength(64)] public string? DedupeAnahtar { get; set; }

    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = "BEKLIYOR";

    /// <summary>
    /// M7 — sağlayıcının (banka bağlantısının) bu harekete verdiği tekil kimlik. (tenant, hesap, saglayici_referans)
    /// BENZERSİZDİR: çekici aynı hareketi ikinci kez yazamaz. ⚠ <see cref="BankaReferans"/>'a tekillik KONMADI:
    /// banka ekstresinde bir EFT ile masrafı aynı dekont numarasını meşru olarak taşıyabilir.
    /// </summary>
    [Column("saglayici_referans"), MaxLength(64)] public string? SaglayiciReferans { get; set; }

    /// <summary>
    /// M7a (kurucu 17.10: "borcu vardır ama ekstra bir altın daha almıştır… borçtan düşülsün mü
    /// diye müşteri karar versin") — GELEN ve müşteriye bağlı dekontta kuyumcunun kararı:
    /// <c>null</c> = karar bekliyor · <c>BORC</c> = borcundan düşüldü (TAHSILAT satırı
    /// <see cref="KararHareketId"/>) · <c>SATIS</c> = yeni alışverişin ödemesi (borca dokunulmadı).
    /// </summary>
    [Column("cari_karari"), MaxLength(10)] public string? CariKarari { get; set; }

    /// <summary>
    /// BORC kararında doğan TAHSILAT defter satırı. ⚠ <see cref="HareketId"/> DEĞİL: o kolon
    /// MASAK kümülatifinden dışlama süzgecidir; para MASAK'a dekontun günüyle burada sayılmaya
    /// devam eder, TAHSILAT satırı ise MasakOlcuTl = 0 ile çift sayımdan çıkar.
    /// </summary>
    [Column("karar_hareket_id"), MaxLength(36)] public string? KararHareketId { get; set; }

    /// <summary>
    /// PAKET P-12 (B-42) — bu satır bir BAŞKA banka dekontunu hükümsüz kılan
    /// ters kayıtsa, kaynağın kimliği.
    /// <para>
    /// ⚠ DEFTER YERİNDE DEĞİŞMEZ (ADR-0004): dekontun tutarı/yönü hiçbir yoldan
    /// güncellenmez; hükümsüzlük yalnız ters satırın VARLIĞINDAN okunur. Kolon
    /// olmadan "bu dekont geri alındı mı" sorusunun makine cevabı yoktu ve
    /// ekranda kalıcı bir <c>DegismezIpucu</c> duruyordu (UX-1-D notu).
    /// </para>
    /// </summary>
    [Column("storno_kaynak_id"), MaxLength(36)] public string? StornoKaynakId { get; set; }

    /// <summary>
    /// PAKET P-19 (Y-11) — VİRMANIN KARŞI BACAĞININ SATIR KİMLİĞİ.
    /// Anlam ayrımı ve tek yazma yolu için bkz. <see cref="KasaHareket.KarsiBacakId"/>.
    /// </summary>
    [Column("karsi_bacak_id"), MaxLength(36)] public string? KarsiBacakId { get; set; }

    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("faturalar")]
public class Fatura : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    /// <summary>
    /// Belge numarası — FTR-{yıl}-{sıra:6}.
    ///
    /// ⚠ PAKET P-22 / B-3: TASLAK faturada <b>NULL</b>'dur. Numara serisi belge
    /// KESİLDİĞİNDE tüketilir (tek nokta: <c>FaturaSeriKapisi.Tuket</c>). Eski
    /// davranışta taslak da numara yakıyordu; taslağı düzeltmenin tek yolu
    /// "iptal et, yenisini kes" olduğu için her düzeltme seride boşluk
    /// bırakıyordu. Benzersizlik kısıtı çok sayıda NULL'a izin verir.
    /// </summary>
    [Column("fatura_no"), MaxLength(30)] public string? FaturaNo { get; set; }
    [Column("cari_id"), MaxLength(36)] public string CariId { get; set; } = null!;
    [Column("tur"), MaxLength(10)] public string Tur { get; set; } = "SATIS";
    /// <summary>STANDART: KDV tüm tutara. KUYUM: has altın düşülüp KDV yalnız işçilik+kâra.</summary>
    [Column("kdv_turu"), MaxLength(10)] public string KdvTuru { get; set; } = "STANDART";
    /// <summary>Kuyum KDV'sinde matrahtan düşülen has altın bedeli (KDV'siz kısım).</summary>
    [Column("has_deger", TypeName = "decimal(15,2)")] public decimal HasDeger { get; set; }
    [Column("ara_toplam", TypeName = "decimal(15,2)")] public decimal AraToplam { get; set; }
    [Column("kdv_oran", TypeName = "decimal(5,4)")] public decimal KdvOran { get; set; }
    [Column("kdv_tutar", TypeName = "decimal(15,2)")] public decimal KdvTutar { get; set; }
    [Column("toplam", TypeName = "decimal(15,2)")] public decimal Toplam { get; set; }
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("durum"), MaxLength(10)] public string Durum { get; set; } = "TASLAK";
    [Column("banka_hareket_id"), MaxLength(36)] public string? BankaHareketId { get; set; }
    [Column("aciklama", TypeName = "text")] public string? Aciklama { get; set; }
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }

    /// <summary>
    /// BASLIK (eski usul, yalnız toplam) · KALEM (fatura_kalemleri'nden hesaplandı).
    /// GERİ DOLDURMA YOK ve GEREKMEZ: mevcut faturaların hepsi tanım gereği
    /// BASLIK'tır. "Kalemleri boş" ile "kalemsiz üretilmiş" karışırsa ekran ve
    /// rapor bilinmeyeni sıfır sayar (Anayasa md.1).
    /// </summary>
    [Column("kalem_kaynagi"), MaxLength(10)] public string KalemKaynagi { get; set; } = "BASLIK";

    /// <summary>
    /// <see cref="HasDeger"/> alanının OTORİTESİ: HAREKET | KALEM | ELLE.
    /// Bertan'ın koşulu (paket 1 ↔ 38): aynı kolonun anlamı ikiye bölünmesin diye
    /// kaynak ÜÇ DEĞERLİ tek kolonda tutulur. Paket 1 kalemli yolda KALEM yazar;
    /// elle beyan edilen (bugünkü) yol ELLE'dir. HAREKET değerini paket 38 kullanır.
    /// </summary>
    [Column("has_deger_kaynagi"), MaxLength(10)] public string HasDegerKaynagi { get; set; } = "ELLE";

    // ── Paket 38 — özel matrah KANIT alanları (hepsi NULL kabul eder) ────────

    /// <summary>
    /// Özel matrah neden kodu (KDVK md.23/e). YALNIZ <c>KdvTuru = "KUYUM"</c>
    /// satırlarda dolar; HESABA GİRMEZ, etikettir. Yapılandırmadan
    /// (<c>Kdv:OzelMatrahKodu</c>) gelir ve VARSAYILAN OLARAK BOŞTUR —
    /// doğrulanmamış bir beyan kodu faturaya yazılmaz (Mehmet K38-1).
    /// </summary>
    [Column("ozel_matrah_kodu"), MaxLength(5)] public string? OzelMatrahKodu { get; set; }

    /// <summary>
    /// Matrahtan düşülen has GRAM — bedelin DAYANAĞI. Gram olmadan denetimde
    /// "bu 41.000 TL nereden çıktı" sorusu cevapsız kalır.
    /// </summary>
    [Column("has_gram", TypeName = "decimal(15,6)")] public decimal? HasGram { get; set; }

    /// <summary>
    /// Kullanılan has kuru (TL/gram). Bacaklar farklı kur kayıtları taşıyabildiği
    /// için burada AĞIRLIKLI ORTALAMA durur: <c>has_deger / has_gram</c>. Tek kur
    /// varsayılmaz — hesap bacak bacak yapılır (OzelMatrahMotoru), bu kolon
    /// yalnızca belge üzerindeki özet kurdur.
    /// </summary>
    [Column("has_birim_fiyat", TypeName = "decimal(15,4)")] public decimal? HasBirimFiyat { get; set; }

    // ── KUR KAYNAKLARI (kurucu 17.10) — has bedelinin KAYNAK KODU belgeye yazılır ──────
    /// <summary>
    /// Has bedelinin fiyat kaynağı: BIST · HAREM · ALTINKAYNAK · ELLE (gün kapanışı) ·
    /// ISLEM_KURU (satıştaki kur kaydı) · BINDE (müşterinin binde yöntemi, fiyat kullanılmadı).
    /// Yalnız KUYUM faturasında dolar.
    /// </summary>
    [Column("ozel_matrah_kaynak_kodu"), MaxLength(12)] public string? OzelMatrahKaynakKodu { get; set; }
    /// <summary>Kullanılan kapanışın işletme günü (son işgünü) — ISLEM_KURU/BINDE'de NULL.</summary>
    [Column("ozel_matrah_kapanis_gunu")] public DateOnly? OzelMatrahKapanisGunu { get; set; }
    /// <summary>Kullanılan kapanış fiyatı (TL / has gram, 1000/1000) — ISLEM_KURU/BINDE'de NULL.</summary>
    [Column("ozel_matrah_kapanis_fiyati", TypeName = "decimal(15,4)")] public decimal? OzelMatrahKapanisFiyati { get; set; }

    // ── 17.10 P0-8 — ALICI BİLGİSİ BELGEYE DONDURULUR ────────────────────────
    // Kesim anında carinin unvanı/adı, kimlik no'su ve adresi belgeye kopyalanır; cari kartı sonradan
    // düzeltilince kesilmiş faturanın alıcısı GERİYE DÖNÜK değişmez (VUK 230: faturada alıcının adı,
    // adresi, vergi no'su belgenin kendisinde bulunur). Taslakta boştur; KESILDI geçişinde bir kez yazılır.
    [Column("alici_unvan"), MaxLength(300)] public string? AliciUnvan { get; set; }
    [Column("alici_vergi_no"), MaxLength(20)] public string? AliciVergiNo { get; set; }
    [Column("alici_kimlik_turu"), MaxLength(4)] public string? AliciKimlikTuru { get; set; }
    [Column("alici_adres"), MaxLength(500)] public string? AliciAdres { get; set; }
}

/// <summary>
/// Rol → modül yetki kaydı. Bir satırın varlığı = o rolün o modüle erişimi var.
/// Tenant bazında; SAHIP her zaman tam yetkilidir (bu tabloda tutulmaz).
/// </summary>
[Table("rol_yetkileri")]
public class RolYetki : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("rol"), MaxLength(20)] public string Rol { get; set; } = null!;
    [Column("modul"), MaxLength(30)] public string Modul { get; set; } = null!;
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 45 Faz A — İki aşamalı giriş (TOTP / MFA).
// Kullanıcı tablosu DEĞİŞMEZ: MFA durumu ayrı 1:1 tabloda durur (Temizle()
// alan listesi bozulmasın, denormalize bayrak iki gerçek üretmesin).
// ═════════════════════════════════════════════════════════════════════════════

[Table("kullanici_mfa")]
public class KullaniciMfa : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    /// <summary>Bugün tek yöntem: TOTP. (SMS/WebAuthn kapsam dışı.)</summary>
    [Column("yontem"), MaxLength(20)] public string Yontem { get; set; } = MfaYontemi.Totp;
    /// <summary>AES-256-GCM ile şifreli giz — "v1:" önekli (ADR-0003). DÜZ METİN YAZILMAZ.</summary>
    [Column("gizli_sifreli"), MaxLength(255)] public string GizliSifreli { get; set; } = null!;
    /// <summary>BEKLIYOR | ETKIN — yalnız ETKIN girişin ikinci adımını tetikler.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = MfaDurumu.Bekliyor;
    [Column("etkinlesti")] public DateTime? Etkinlesti { get; set; }
    /// <summary>Tekrar kapısı: kullanılan TOTP adımı; aynı/daha küçük adım bir daha kabul edilmez.</summary>
    [Column("son_kullanilan_adim")] public long? SonKullanilanAdim { get; set; }
    [Column("yanlis_deneme")] public int YanlisDeneme { get; set; }
    /// <summary>Doluysa ve gelecekteyse DOĞRU kod bile reddedilir (kaba kuvvet kapısı).</summary>
    [Column("kilit_bitis")] public DateTime? KilitBitis { get; set; }
    [Column("kurtarma_uretildi")] public DateTime? KurtarmaUretildi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

[Table("mfa_kurtarma_kodlari")]
public class MfaKurtarmaKodu : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    /// <summary>SHA-256 hex (TokenServisi.Hashle ile aynı biçim). Düz kod ASLA saklanmaz.</summary>
    [Column("kod_hash"), MaxLength(64)] public string KodHash { get; set; } = null!;
    /// <summary>Tek kullanımlık: tüketilen kod işaretlenir, SİLİNMEZ (denetim izi).</summary>
    [Column("kullanildi")] public bool Kullanildi { get; set; }
    [Column("kullanildi_tarih")] public DateTime? KullanildiTarih { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

public static class MfaDurumu
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Etkin = "ETKIN";
}

public static class MfaYontemi
{
    public const string Totp = "TOTP";
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-3 Faz A — Yedek otomasyonu ve doğrulanmış geri yükleme.
//
// ÜÇ TABLO, İKİ KAPSAM:
//   yedek_kayitlari     → SİSTEM düzeyi (kiracısız). Bir yedek dosyası TÜM
//                         kiracıların verisini taşır; "kiracının yedeği" diye
//                         bir şey yoktur. Tenant kalıbıyla aynı muafiyet.
//   yedek_tatbikatlari  → SİSTEM düzeyi. Aylık geri-yükleme provasının kanıtı.
//   yedek_talepleri     → KİRACIYA ait (IKiraciVarlik): "kim, hangi gerekçeyle
//                         elle yedek istedi" izlenir ve kiracılar birbirinin
//                         talebini GÖRMEZ.
//
// ⚠ API bu tablolara yalnız YAZAR/OKUR; süreç başlatmaz (denetim.sh T3-1).
//   Yedeği alan taraf dotnet/yedek-al-ve-dogrula.sh betiğidir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir yedek KOŞUSU — dosyanın kendisi değil, koşunun üstverisi.</summary>
/// <remarks>
/// ⚠ Dosya ADI burada durur ama API'den DÖNMEZ (indirme yüzeyi açılmasın diye
/// yol/ad istemciye verilmez — PAKET-T-3 § Kapsam dışı).
/// </remarks>
[Table("yedek_kayitlari")]
public class YedekKaydi
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    /// <summary>TAM | ELLE (Faz A'da ARTIMLI YOK — MÜHÜR 6 / K1).</summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = YedekTuru.Tam;
    [Column("baslangic")] public DateTime Baslangic { get; set; }
    [Column("bitis")] public DateTime? Bitis { get; set; }
    /// <summary>SURUYOR | BASARILI | BASARISIZ.</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = YedekDurumu.Suruyor;
    [Column("dosya_adi"), MaxLength(200)] public string? DosyaAdi { get; set; }
    [Column("boyut_bayt")] public long? BoyutBayt { get; set; }
    /// <summary>Şifreli dosyanın SHA-256'sı (hex). Tatbikat bunu yeniden ölçer.</summary>
    [Column("sha256"), MaxLength(64)] public string? Sha256 { get; set; }
    /// <summary>FAIL-CLOSED 2: şifresiz yedek üretilmez; bu alan false ise koşu BASARISIZ'dır.</summary>
    [Column("sifreli")] public bool Sifreli { get; set; }
    /// <summary>YOK | KOPYALANDI | HATA — FAIL-CLOSED 3: HATA ⇒ koşu BASARISIZ.</summary>
    [Column("dis_kopya"), MaxLength(20)] public string DisKopya { get; set; } = DisKopyaDurumu.Yok;
    /// <summary>BEKLIYOR | DOGRULANDI | UYUSMADI — doğrulama tatbikattan gelir.</summary>
    [Column("dogrulama"), MaxLength(20)] public string Dogrulama { get; set; } = YedekDogrulama.Bekliyor;
    [Column("dogrulama_tarih")] public DateTime? DogrulamaTarih { get; set; }
    [Column("tablo_sayisi")] public int? TabloSayisi { get; set; }
    /// <summary>⚠ Yalnız teknik hata özeti; sır/parola/veri değeri YAZILMAZ.</summary>
    [Column("hata_ozeti"), MaxLength(500)] public string? HataOzeti { get; set; }
}

/// <summary>Elle yedek TALEBİ — API süreç başlatmaz, kuyruğa satır yazar.</summary>
[Table("yedek_talepleri")]
public class YedekTalebi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    /// <summary>NEDEN — en az 20 karakter (sunucuda da zorlanır).</summary>
    [Column("gerekce"), MaxLength(500)] public string Gerekce { get; set; } = null!;
    /// <summary>BEKLIYOR | ISLENDI | IPTAL | HATA. Satır SİLİNMEZ (DoD: pasife alma).</summary>
    [Column("durum"), MaxLength(20)] public string Durum { get; set; } = YedekTalepDurumu.Bekliyor;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("islendi")] public DateTime? Islendi { get; set; }
    /// <summary>Kuyruk tüketilince üretilen koşunun kimliği.</summary>
    [Column("yedek_kaydi_id"), MaxLength(36)] public string? YedekKaydiId { get; set; }
    [Column("iptal_gerekce"), MaxLength(500)] public string? IptalGerekce { get; set; }
    /// <summary>
    /// PAKET P-77 — talebin TÜRÜ: YEDEK (elle yedek al) ya da DOGRULAMA (son
    /// başarılı yedeği ayrı şemaya geri yükleyip sağlama toplamıyla doğrula).
    /// Mevcut satırlar YEDEK kalır (migration varsayılanı). Kuyruğu aynı betik
    /// tüketir; tür, betiğin hangi işi koşacağını söyler. Doğrulama yedek
    /// DOSYASINI silmez/değiştirmez (yalnız okur).
    /// </summary>
    [Column("tur"), MaxLength(20)] public string Tur { get; set; } = YedekTalepTuru.Yedek;
}

/// <summary>Aylık geri-yükleme provasının KANITI (Aşkın'ın şartı).</summary>
[Table("yedek_tatbikatlari")]
public class YedekTatbikati
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tarih")] public DateTime Tarih { get; set; }
    [Column("yedek_kaydi_id"), MaxLength(36)] public string? YedekKaydiId { get; set; }
    [Column("tablo_sayisi")] public int TabloSayisi { get; set; }
    [Column("uyusan")] public int Uyusan { get; set; }
    [Column("uyusmayan")] public int Uyusmayan { get; set; }
    [Column("sonuc"), MaxLength(20)] public string Sonuc { get; set; } = YedekDurumu.Basarisiz;
    [Column("sure_sn")] public int SureSn { get; set; }
    /// <summary>
    /// ⚠ MEHMET KOŞULU: rapora YALNIZ uyuşmayan TABLO ADLARI yazılır; hiçbir
    /// satır/alan DEĞERİ yazılmaz (yedek kişisel veri taşır, rapor ekranda görünür).
    /// </summary>
    [Column("rapor"), MaxLength(4000)] public string? Rapor { get; set; }
    /// <summary>Tatbikat şeması düşürüldü mü — "kaynağa yazmadık" kanıtının ikinci yarısı.</summary>
    [Column("sema_dusuruldu")] public bool SemaDusuruldu { get; set; }
    /// <summary>ÖLÇÜLEN kurtarma süresi (dk). Taahhüt DEĞİLDİR (MÜHÜR 6 / K2).</summary>
    [Column("rto_dakika_olculen")] public int? RtoDakikaOlculen { get; set; }
}

public static class YedekTuru
{
    public const string Tam = "TAM";
    public const string Elle = "ELLE";
    public static readonly string[] Tumu = [Tam, Elle];
}

public static class YedekDurumu
{
    public const string Suruyor = "SURUYOR";
    public const string Basarili = "BASARILI";
    public const string Basarisiz = "BASARISIZ";
}

public static class YedekDogrulama
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Dogrulandi = "DOGRULANDI";
    public const string Uyusmadi = "UYUSMADI";
}

public static class DisKopyaDurumu
{
    public const string Yok = "YOK";
    public const string Kopyalandi = "KOPYALANDI";
    public const string Hata = "HATA";
}

/// <summary>PAKET P-77 — yedek_talepleri.tur değerleri.</summary>
public static class YedekTalepTuru
{
    public const string Yedek = "YEDEK";
    public const string Dogrulama = "DOGRULAMA";
}

public static class YedekTalepDurumu
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Islendi = "ISLENDI";
    public const string Iptal = "IPTAL";
    public const string Hata = "HATA";
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 31 — POS ALACAK DEFTERİ (yan defter)
//
// Kart tahsilatı bugün kasaya BRÜT giriyor; bankanın kestiği komisyon ve
// 1–30 günlük valör hiçbir yerde tutulmuyordu. Bu dört tablo o boşluğu
// kapatır ve KASA BAKİYESİNE HİÇ DOKUNMAZ (ADR-0016): pos_tahsilatlari bir
// YAN DEFTERDİR, ikinci bir muhasebe motoru değildir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>POS tahsilat satırının kapanma durumu — ELLE ATANMAZ, PosDefteri.Ozetle türetir.</summary>
public static class PosTahsilatDurumu
{
    public const string Bekliyor = "BEKLIYOR";
    public const string Kismi = "KISMI";
    public const string Yatti = "YATTI";
    public const string Iptal = "IPTAL";
}

[Table("pos_cihazlari")]
public class PosCihaz : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("ad"), MaxLength(100)] public string Ad { get; set; } = null!;
    [Column("banka_adi"), MaxLength(100)] public string BankaAdi { get; set; } = null!;
    [Column("uye_isyeri_no"), MaxLength(50)] public string? UyeIsyeriNo { get; set; }
    [Column("terminal_no"), MaxLength(50)] public string? TerminalNo { get; set; }

    /// <summary>Kart bacağının yazıldığı kasa. Bacağın KasaId'si bununla AYNI olmak zorundadır.</summary>
    [Column("kasa_id"), MaxLength(36)] public string KasaId { get; set; } = null!;

    /// <summary>Yatmanın beklendiği banka hesabı (Faz B eşleştirmesi bunu kullanır).</summary>
    [Column("banka_hesap_id"), MaxLength(36)] public string? BankaHesapId { get; set; }

    /// <summary>PAKET-15 kararı: POS cihazı YALNIZ TRY. Döviz POS ucu açılmadı.</summary>
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";

    /// <summary>
    /// Peşin (tek çekim) komisyon oranı, 0..1. ⚠ MEVZUAT DEĞİLDİR: banka üye
    /// işyeri sözleşmesinden gelir, kuyumcudan kuyumcuya değişir. Bu yüzden
    /// koda hiçbir varsayılan sayı gömülmez; kullanıcıdan alınır.
    /// </summary>
    [Column("pesin_komisyon_oran", TypeName = "decimal(5,4)")] public decimal PesinKomisyonOran { get; set; }

    [Column("valor_gun")] public int ValorGun { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}

/// <summary>
/// POS CİHAZININ KASA GÖÇÜ — PAKET P-22 / B-55.
///
/// ⚠ NEDEN VAR: bir cihazda tahsilat yazıldıktan sonra "bağlı kasa" alanı
/// kilitleniyordu; tek çıkış yol "cihazı pasife al, yenisini kur" idi. Bu yol
/// geçmiş raporları iki cihaza bölüyor ve kullanıcıyı belgeyi çöpe atmaya
/// zorluyordu (K-23'ün hastalığının POS'taki kardeşi). Kasa bağı artık
/// TAŞINABİLİR — ama sessiz değil, DENETİMLİ: yetki + gerekçe + iz + görünürlük.
///
/// ⚠ GEÇMİŞ DEĞİŞMEZ: yazılmış <c>pos_tahsilatlari</c> satırları kendi
/// <c>kasa_hareket_id</c>'siyle ESKİ kasaya bağlı kalır. Göç yalnız BUNDAN
/// SONRAKİ tahsilatların hangi kasaya düşeceğini değiştirir. Bu satır o anın
/// tutanağıdır ve SİLİNMEZ (append-only).
/// </summary>
[Table("pos_kasa_gocleri")]
public class PosKasaGocu : IKiraciVarlik
{
    /// <summary>Gerekçenin asgari uzunluğu — boş/üstünkörü gerekçe kabul edilmez.</summary>
    public const int GerekceAsgari = 20;

    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("pos_cihaz_id"), MaxLength(36)] public string PosCihazId { get; set; } = null!;
    [Column("eski_kasa_id"), MaxLength(36)] public string EskiKasaId { get; set; } = null!;
    [Column("yeni_kasa_id"), MaxLength(36)] public string YeniKasaId { get; set; } = null!;

    /// <summary>Göç anında cihazda duran tahsilat sayısı — tutanağın kanıtı.</summary>
    [Column("tahsilat_sayisi")] public int TahsilatSayisi { get; set; }

    [Column("gerekce", TypeName = "text")] public string Gerekce { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("pos_tahsilatlari")]
public class PosTahsilat : IKiraciVarlik, ISubeVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("sube_id"), MaxLength(36)] public string SubeId { get; set; } = null!;
    [Column("pos_cihaz_id"), MaxLength(36)] public string PosCihazId { get; set; } = null!;
    [Column("hareket_id"), MaxLength(36)] public string? HareketId { get; set; }
    [Column("kasa_hareket_id"), MaxLength(36)] public string? KasaHareketId { get; set; }

    /// <summary>
    /// Mimari değişmez (Bertan, çelişki çözümü): para hareketi doğuran her satır
    /// kur fotoğrafı taşır — satır bugün daima TRY olsa bile istisna açılmaz.
    /// </summary>
    [Column("kur_snapshot_id"), MaxLength(36)] public string KurSnapshotId { get; set; } = null!;
    [Column("cari_id"), MaxLength(36)] public string? CariId { get; set; }

    /// <summary>İŞARETLİ brüt: satış + , iade − (Math.Abs KULLANILMAZ).</summary>
    [Column("brut_tutar", TypeName = "decimal(15,2)")] public decimal BrutTutar { get; set; }

    /// <summary>Oran SATIRA DONDURULUR (snapshot) — cihaz ayarı sonradan değişse de geçmiş değişmez.</summary>
    [Column("komisyon_oran", TypeName = "decimal(5,4)")] public decimal KomisyonOran { get; set; }
    [Column("komisyon_tutar", TypeName = "decimal(15,2)")] public decimal KomisyonTutar { get; set; }
    [Column("net_tutar", TypeName = "decimal(15,2)")] public decimal NetTutar { get; set; }

    /// <summary>TÜRETİLMİŞ ÖZET — yalnız PosDefteri.Ozetle yazar (Karar C).</summary>
    [Column("yatan_tutar", TypeName = "decimal(15,2)")] public decimal YatanTutar { get; set; }

    [Column("taksit_adedi")] public int TaksitAdedi { get; set; } = 1;
    [Column("para_birimi"), MaxLength(5)] public string ParaBirimi { get; set; } = "TRY";
    [Column("islem_tarihi")] public DateTime IslemTarihi { get; set; }
    [Column("valor_tarihi")] public DateTime ValorTarihi { get; set; }

    /// <summary>TÜRETİLMİŞ ÖZET — BEKLIYOR|KISMI|YATTI|IPTAL. Elle atanmaz (IPTAL hariç: iptal ucu yazar).</summary>
    [Column("durum"), MaxLength(15)] public string Durum { get; set; } = PosTahsilatDurumu.Bekliyor;
    [Column("iptal_gerekcesi", TypeName = "text")] public string? IptalGerekcesi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("pos_taksit_oranlari")]
public class PosTaksitOran : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("pos_cihaz_id"), MaxLength(36)] public string PosCihazId { get; set; } = null!;
    [Column("taksit_adedi")] public int TaksitAdedi { get; set; }
    [Column("komisyon_oran", TypeName = "decimal(5,4)")] public decimal KomisyonOran { get; set; }

    /// <summary>null = cihazın valör günü kullanılır.</summary>
    [Column("valor_gun")] public int? ValorGun { get; set; }
    [Column("gecerli_baslangic")] public DateTime GecerliBaslangic { get; set; }
    [Column("aktif")] public bool Aktif { get; set; } = true;
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

[Table("pos_yatma_eslesmeleri")]
public class PosYatmaEslesme : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("pos_tahsilat_id"), MaxLength(36)] public string PosTahsilatId { get; set; } = null!;
    [Column("banka_hareket_id"), MaxLength(36)] public string BankaHareketId { get; set; } = null!;
    [Column("eslesen_tutar", TypeName = "decimal(15,2)")] public decimal EslesenTutar { get; set; }
    [Column("otomatik")] public bool Otomatik { get; set; }
    [Column("eslestiren_kullanici_id"), MaxLength(36)] public string EslestirenKullaniciId { get; set; } = null!;

    /// <summary>
    /// ZO-ISLEM-A (28.09): aynı (tahsilat, banka hareketi) çiftinin kaçıncı eşleşme
    /// olayı olduğu (1, 2, 3 …; geri alınmışlar DAHİL sayılır). Tekil indeks bu sırayı
    /// da içerir: geri alınan satır iz olarak kalır, aynı çift yeniden ya da kısmi
    /// ek tutarla eşleştirilebilir; aynı anda gelen iki istek aynı sırayı üretir ve
    /// veritabanı ikincisini reddeder (yarış koruması korunur).
    /// </summary>
    [Column("cift_sira")] public int CiftSira { get; set; } = 1;

    /// <summary>ADR-0004 append-only: eşleşme SİLİNMEZ, geri alınır.</summary>
    [Column("geri_alindi")] public bool GeriAlindi { get; set; }
    [Column("geri_alma_gerekcesi", TypeName = "text")] public string? GeriAlmaGerekcesi { get; set; }
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-37 — KULLANICI TERCİHİ (menü favorileri · menü sırası · panel düzeni)
//
// ⚠ KİRACIYA DEĞİL KULLANICIYA BAĞLIDIR. İki tezgâhtarın favorisi farklıdır;
//   tercihi kiracıya yazmak, bir kullanıcının menüsünü başkasının değiştirmesi
//   demektir (denetim.sh FAVORI_KULLANICI_DISI sayacı bunu ölçer).
// ⚠ IKiraciVarlik'tır: kiracı sızıntısını global süzgeç kapatır. Kullanıcı
//   kırılımı AYRICA her sorguda aranır (tenant tek başına yetmez).
// ⚠ Deger bir JSON metnidir (dizi ya da nesne). İş verisi TAŞIMAZ; yalnız
//   arayüz tercihidir — silinmesi hiçbir defteri bozmaz, bu yüzden hard DELETE
//   serbesttir (finansal varlık değildir).
// ═════════════════════════════════════════════════════════════════════════════
[Table("kullanici_tercihleri")]
public class KullaniciTercihi : IKiraciVarlik
{
    [Key, Column("id"), MaxLength(36)] public string Id { get; set; } = null!;
    [Column("tenant_id"), MaxLength(36)] public string TenantId { get; set; } = null!;
    [Column("kullanici_id"), MaxLength(36)] public string KullaniciId { get; set; } = null!;
    /// <summary>Tercih anahtarı — beyaz liste: menu-favoriler | menu-sirasi | panel-duzeni.</summary>
    [Column("anahtar"), MaxLength(60)] public string Anahtar { get; set; } = null!;
    /// <summary>JSON gövde (dizi ya da nesne). En çok 4000 karakter.</summary>
    [Column("deger", TypeName = "text")] public string Deger { get; set; } = "";
    [Column("olusturuldu")] public DateTime Olusturuldu { get; set; }
    [Column("guncellendi")] public DateTime Guncellendi { get; set; }
}
