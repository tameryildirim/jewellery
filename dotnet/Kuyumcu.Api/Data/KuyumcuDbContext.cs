using System.Reflection;
using Kuyumcu.Api.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kuyumcu.Api.Data;

/// <summary>Kiracıya ait her varlık bunu uygular. Kapsam denetimi buna bakar.</summary>
public interface IKiraciVarlik
{
    string TenantId { get; }
}

/// <summary>
/// PAKET P-04 (Y-1) — ŞUBEYE ait her varlık bunu uygular.
/// <para>
/// Kapsam denetimi (OnModelCreating sonu) <c>sube_id</c> sütunu olan ama bunu
/// UYGULAMAYAN bir varlık görürse UYGULAMA AÇILMAZ; yeni varlıkta şube süzgecini
/// unutmak böylece derleme/başlangıç zamanına taşınır.
/// </para>
/// </summary>
public interface ISubeVarlik
{
    /// <summary>Kaydın ait olduğu şube. NULL → şubeye bağlı olmayan, kiracı geneli kayıt.</summary>
    string? SubeId { get; }
}

/// <summary>
/// Sistem modeli ile kiracı modeli AYRI önbelleklenir.
/// Sebep: tek modelde `sistem || tenant_id = @p` biçiminde OR'lu bir filtre
/// gerekirdi; MySQL'de OR indeks kullanımını bozar ve `hareketler` büyüdükçe
/// rapor uçları yavaşlar. İki model ile kiracı modelinde saf `tenant_id = @p` çıkar.
/// </summary>
public sealed class KiraciModelOnbellegi : IModelCacheKeyFactory
{
    // ⚠ P-04: şube süzgecinin AÇIK/KAPALI olması da modeli değiştirir. Anahtara
    // girmezse ilk isteğin modeli (ör. SAHIP → süzgeçsiz) önbelleğe yerleşir ve
    // sonraki TEZGAHTAR isteği SÜZGEÇSİZ çalışırdı — tam da kapattığımız açık.
    public object Create(DbContext baglam, bool tasarimZamani) =>
        (baglam.GetType(),
         (baglam as KuyumcuDbContext)?.SistemModu ?? true,
         (baglam as KuyumcuDbContext)?.SubeFiltresiAcik ?? false,
         tasarimZamani);
}

public class KuyumcuDbContext : DbContext
{
    private readonly string? kiraciId;
    private readonly string? subeId;
    private readonly IAlanSifreleyici sifreleyici;

    /// <summary>MASAK hash dinleyicisi ve servisler için — anahtarı DIŞA VERMEZ, yalnız işlem yapar.</summary>
    internal IAlanSifreleyici Sifreleyici => sifreleyici;

    /// <summary>Filtresiz mi çalışıyor? Model önbellek anahtarına girer.</summary>
    internal bool SistemModu { get; }

    /// <summary>Yazma denetçisinin okuduğu aktif kiracı; sistem modunda null.</summary>
    internal string? AktifKiraciId => kiraciId;

    /// <summary>P-04: şube süzgeci kurulu mu? Model önbellek anahtarına girer.</summary>
    internal bool SubeFiltresiAcik { get; }

    /// <summary>P-04: süzgeç açıkken yazma denetçisinin okuduğu aktif şube.</summary>
    internal string? AktifSubeId => subeId;

    // ⚠ BAĞLAMSIZ İKİNCİ CTOR EKLEME. Bkz. KiraciBaglami.cs başlığı.
    public KuyumcuDbContext(
        DbContextOptions<KuyumcuDbContext> secenekler, IKiraciBaglami baglam, IAlanSifreleyici sifreleyici)
        : base(secenekler)
    {
        SistemModu = baglam.SistemModu;
        kiraciId = baglam.KiraciId;
        subeId = baglam.SubeId;
        SubeFiltresiAcik = !baglam.SistemModu && !baglam.TumSubeler;
        this.sifreleyici = sifreleyici;

        // FAIL-CLOSED: bağlam çözülemediyse patla. Sessizce "0 satır" dönmektense
        // gürültülü hata tercih edilir — sessiz hâli yanlış veriyi doğru gösterirdi.
        if (!SistemModu && string.IsNullOrEmpty(kiraciId))
            throw new InvalidOperationException(
                "Kiracı bağlamı çözülemedi: KuyumcuDbContext kiracı ya da sistem bağlamı olmadan kullanılamaz.");

        // FAIL-CLOSED (P-04 / Y-1, şube NULL sözleşmesi md.3): tüm-şube ayrıcalığı
        // OLMAYAN bir bağlamda şube boşsa patla. Sessizce "tümünü göster"
        // (fail-open) ya da "0 satır" (fail-blind) YOK.
        // ⚠ TİP: ApiException türevi (403) — sessiz 500 yerine kullanıcıya TÜRKÇE
        // sebep döner. Anlam aynıdır: bağlam yoksa hiçbir veri gösterilmez.
        if (SubeFiltresiAcik && string.IsNullOrEmpty(subeId))
            throw new SubeIhlaliException(
                "Şube bağlamı çözülemedi: bu rol tüm şubeleri göremez ve oturumda şube yok. " +
                "Kullanıcıya bir şube atanmalı ya da tüm şubeleri görebilen bir rol verilmeli.");
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Sube> Subeler => Set<Sube>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<RefreshToken> RefreshTokenler => Set<RefreshToken>();
    /// <summary>PAKET AUTH-SIFIRLA — "Parolamı unuttum" jetonları (yalnız SHA-256 özeti).</summary>
    public DbSet<ParolaSifirlamaJetonu> ParolaSifirlamaJetonlari => Set<ParolaSifirlamaJetonu>();
    public DbSet<KurSnapshot> KurSnapshots => Set<KurSnapshot>();
    public DbSet<Cari> Cariler => Set<Cari>();
    public DbSet<CariBakiye> CariBakiyeler => Set<CariBakiye>();
    public DbSet<Urun> Urunler => Set<Urun>();
    public DbSet<Stok> Stoklar => Set<Stok>();
    public DbSet<Hareket> Hareketler => Set<Hareket>();
    public DbSet<Kasa> Kasalar => Set<Kasa>();
    public DbSet<KasaHareket> KasaHareketleri => Set<KasaHareket>();
    public DbSet<GunKapanisi> GunKapanislari => Set<GunKapanisi>();
    public DbSet<FiyatListesi> FiyatListeleri => Set<FiyatListesi>();
    public DbSet<Kampanya> Kampanyalar => Set<Kampanya>();
    public DbSet<FiyatListesiUrunFiyati> FiyatListesiUrunFiyatlari => Set<FiyatListesiUrunFiyati>();
    public DbSet<SayimOturumu> SayimOturumlari => Set<SayimOturumu>();
    public DbSet<SayimSatiri> SayimSatirlari => Set<SayimSatiri>();
    public DbSet<Belge> Belgeler => Set<Belge>();
    public DbSet<BankaHesap> BankaHesaplari => Set<BankaHesap>();
    public DbSet<BankaHareket> BankaHareketleri => Set<BankaHareket>();
    // ── Paket 32 Faz A — banka ekstresi içe aktarma (SAHNE tabloları) ────────
    // Bu üçü DEFTER DEĞİLDİR: deftere yazan tek yol BankaServisi.HareketEkleIc.
    public DbSet<EkstreIthalati> EkstreIthalatlari => Set<EkstreIthalati>();
    public DbSet<EkstreSatiri> EkstreSatirlari => Set<EkstreSatiri>();
    public DbSet<CariIban> CariIbanlari => Set<CariIban>();
    public DbSet<Fatura> Faturalar => Set<Fatura>();
    public DbSet<FaturaKalemi> FaturaKalemleri => Set<FaturaKalemi>();
    public DbSet<RolYetki> RolYetkileri => Set<RolYetki>();

    // Paket 29 Faz A — çek/senet portföyü. ⚠ Bu tablo BAKİYENİN KAYNAĞI
    // DEĞİLDİR (KiymetliEvrak.cs sınıf yorumu): bakiyeyi yalnız defter
    // (hareketler) değiştirir; burası portföyün aynasıdır.
    public DbSet<KiymetliEvrak> KiymetliEvraklar => Set<KiymetliEvrak>();

    // ── Denetim izi + MASAK uyum (Adım 3) ────────────────────────────────────
    public DbSet<DenetimKaydi> DenetimKayitlari => Set<DenetimKaydi>();
    public DbSet<MasakKimlikTespiti> MasakKimlikTespitleri => Set<MasakKimlikTespiti>();
    public DbSet<MasakBeyani> MasakBeyanlari => Set<MasakBeyani>();
    public DbSet<MasakSupheliBildirim> MasakSupheliBildirimler => Set<MasakSupheliBildirim>();
    /// <summary>Paket 40-A — APPEND-ONLY risk profili tarihçesi (<see cref="RiskProfiliniKoru"/>).</summary>
    public DbSet<CariRiskProfili> CariRiskProfilleri => Set<CariRiskProfili>();
    public DbSet<GiderPusulasi> GiderPusulalari => Set<GiderPusulasi>();
    // ── PAKET P-19 (Y-12) — gider/masraf kategorileri (soyut sınıflandırma) ──
    public DbSet<GiderKategorisi> GiderKategorileri => Set<GiderKategorisi>();
    public DbSet<DisaAktarimTalebi> DisaAktarimTalepleri => Set<DisaAktarimTalebi>();

    // ── Paket 47 Faz A — veri içe aktarımı (kuru provalı, geri alınabilir) ───
    // Global kiracı filtresi satır 98'deki jenerik KiraciFiltresi ile OTOMATİK
    // gelir (ikisi de IKiraciVarlik); indeksler OnModelCreating'de.
    public DbSet<IceAktarimOturumu> IceAktarimOturumlari => Set<IceAktarimOturumu>();
    public DbSet<IceAktarimSatiri> IceAktarimSatirlari => Set<IceAktarimSatiri>();
    public DbSet<StokPartisi> StokPartileri => Set<StokPartisi>();
    public DbSet<StokPartiTuketimi> StokPartiTuketimleri => Set<StokPartiTuketimi>();
    public DbSet<UrunKategorisi> UrunKategorileri => Set<UrunKategorisi>();
    public DbSet<UrunKurali> UrunKurallari => Set<UrunKurali>();   // M4
    public DbSet<Reyon> Reyonlar => Set<Reyon>();

    // Paket 25 Faz A — şube stoğunun reyon kırılımı. `stoklar` tablosu DEĞİŞMEDİ;
    // bu tablo onun alt bölümlemesidir (Σ yerleşim ≤ stok, fark = ATANMAMIŞ).
    public DbSet<StokYerlesimi> StokYerlesimleri => Set<StokYerlesimi>();

    // Paket 45 Faz A — iki aşamalı giriş (TOTP).
    public DbSet<KullaniciMfa> KullaniciMfalari => Set<KullaniciMfa>();
    public DbSet<MfaKurtarmaKodu> MfaKurtarmaKodlari => Set<MfaKurtarmaKodu>();

    // Paket 6 Faz A — bildirim kanalı + zamanlanmış iş çatısı (Data/ZamanlanmisIs.cs).
    public DbSet<IsCalismasi> IsCalismalari => Set<IsCalismasi>();
    public DbSet<Bildirim> Bildirimler => Set<Bildirim>();

    /// <summary>PAKET P-37 — kullanıcı bazlı arayüz tercihleri (menü favorisi/sırası, panel düzeni).</summary>
    public DbSet<KullaniciTercihi> KullaniciTercihleri => Set<KullaniciTercihi>();

    // ── Paket 3 Faz A — sipariş çatısı (durum makinesi) ──────────────────────
    public DbSet<Siparis> Siparisler => Set<Siparis>();
    public DbSet<SiparisKalemi> SiparisKalemleri => Set<SiparisKalemi>();
    public DbSet<SiparisGecisi> SiparisGecisleri => Set<SiparisGecisi>();

    // ── PAKET P-26 (Y-34 · Y-35) — tedarik zinciri: satınalma siparişi + irsaliye ──
    // Global kiracı filtresi jenerik KiraciFiltresi ile OTOMATİK gelir
    // (dördü de IKiraciVarlik); şube süzgeci ISubeVarlik ile (başlık iki tablo).
    public DbSet<SatinalmaSiparisi> SatinalmaSiparisleri => Set<SatinalmaSiparisi>();
    public DbSet<SatinalmaKalemi> SatinalmaKalemleri => Set<SatinalmaKalemi>();
    public DbSet<Irsaliye> Irsaliyeler => Set<Irsaliye>();
    public DbSet<IrsaliyeKalemi> IrsaliyeKalemleri => Set<IrsaliyeKalemi>();

    /// <summary>PAKET P-21 (Y-23) — rezervasyon (bloke) kayıtları.</summary>
    public DbSet<Rezervasyon> Rezervasyonlar => Set<Rezervasyon>();

    // ── Paket 19 Faz A — vezne modeli (Data/VezneFisi.cs) ────────────────────
    public DbSet<VezneFisi> VezneFisleri => Set<VezneFisi>();
    public DbSet<VezneFisKalemi> VezneFisKalemleri => Set<VezneFisKalemi>();
    public DbSet<KiraciAyari> KiraciAyarlari => Set<KiraciAyari>();

    // ── Paket T-2 Faz A — kuruluş sihirbazı + açılış bakiyeleri ──────────────
    // Kiracı süzgeci jenerik KiraciFiltresi ile OTOMATİK gelir (ikisi de
    // IKiraciVarlik). Bu iki tablo DEFTER DEĞİLDİR: deftere HareketFabrikasi
    // ile yazılanın izidir (Data/Kurulum.cs başlığı).
    public DbSet<KurulumDurumu> KurulumDurumlari => Set<KurulumDurumu>();
    public DbSet<AcilisKalemi> AcilisKalemleri => Set<AcilisKalemi>();

    /// <summary>
    /// Kiracı davetleri — paket F-2-min. KİRACI-ÖNCESİ tablo: IKiraciVarlik
    /// DEĞİLDİR, HasQueryFilter EKLENMEZ (gerekçe: <see cref="KiraciDaveti"/>).
    /// </summary>
    public DbSet<KiraciDaveti> Davetler => Set<KiraciDaveti>();

    // ── Paket T-3 Faz A — yedek otomasyonu ───────────────────────────────────
    // ⚠ İKİSİ KİRACISIZDIR (yedek_kayitlari, yedek_tatbikatlari): bir yedek
    //   dosyası TÜM kiracıların verisini taşır, "kiracının yedeği" yoktur.
    //   Muafiyet Tenants/KiraciDaveti kalıbının AYNISIYLA, aşağıdaki beyaz
    //   listeden alınır — HasQueryFilter EKLENMEZ.
    public DbSet<YedekKaydi> YedekKayitlari => Set<YedekKaydi>();
    public DbSet<YedekTatbikati> YedekTatbikatlari => Set<YedekTatbikati>();
    /// <summary>Elle yedek talebi KİRACIYA aittir: jenerik kiracı süzgeci uygulanır.</summary>
    public DbSet<YedekTalebi> YedekTalepleri => Set<YedekTalebi>();

    // ── PAKET 31 — POS ALACAK DEFTERİ (yan defter, ADR-0016) ───────────────
    // Dördü de IKiraciVarlik: global kiracı süzgeci OnModelCreating'de otomatik
    // bağlanır, PosServisi ayrıca HER sorguda elle `TenantId ==` taşır (ADR-0010).
    public DbSet<PosCihaz> PosCihazlari => Set<PosCihaz>();
    public DbSet<PosTahsilat> PosTahsilatlari => Set<PosTahsilat>();

    /// <summary>PAKET P-22 / B-55 — POS cihazının kasa göç tutanakları (append-only).</summary>
    public DbSet<PosKasaGocu> PosKasaGocleri => Set<PosKasaGocu>();
    public DbSet<PosTaksitOran> PosTaksitOranlari => Set<PosTaksitOran>();
    public DbSet<PosYatmaEslesme> PosYatmaEslesmeleri => Set<PosYatmaEslesme>();

    /// <summary>
    /// PAKET P-23 (R-17) — fişin ödeme bacaklarının KALICI kırılımı.
    /// ⚠ DEFTER DEĞİLDİR: bu tabloya yazmak hareket YAZMAZ; deftere yazan tek
    ///   yol <see cref="HareketFabrikasi.Ekle"/> olmaya DEVAM EDER. Kalem,
    ///   yazılmış fişin yanına konan AÇIKLAYICI kayıttır.
    /// ⚠ Faz 1: yalnız İLERİYE dönük yazılır; geçmiş fişlerin kalemi yoktur
    ///   (geri doldurma plan § 11 BK-03'e bağlı).
    /// </summary>
    public DbSet<OdemeKalemi> OdemeKalemleri => Set<OdemeKalemi>();

    /// <summary>
    /// PAKET P-27 (F-1 · R-1) — satılabilir plan kataloğu. KİRACI-ÜSTÜ:
    /// IKiraciVarlik DEĞİLDİR ve HasQueryFilter EKLENMEZ (aşağıdaki kapsam
    /// denetiminde gerekçeli beyaz liste maddesi vardır — KiraciDaveti kalıbı).
    /// </summary>
    public DbSet<Modules.Abonelik.AbonelikPlani> AbonelikPlanlari =>
        Set<Modules.Abonelik.AbonelikPlani>();

    /// <summary>
    /// PAKET P-27 (F-1 · R-1) — kiracının abonelik DÖNEMLERİ (append-only
    /// tarihçe). IKiraciVarlik: global kiracı süzgeci OTOMATİK kurulur.
    /// </summary>
    public DbSet<Modules.Abonelik.KiraciAbonelik> KiraciAbonelikleri =>
        Set<Modules.Abonelik.KiraciAbonelik>();

    /// <summary>
    /// PAKET P-28 (F-4 · R-2) — kiracı etiketli SAATLİK kaynak sayacı.
    /// IKiraciVarlik: global kiracı süzgeci OTOMATİK kurulur.
    /// ⚠ Yalnız sayaç tutar; yol/sorgu/başlık/kullanıcı gibi hiçbir kişisel
    /// veri kolonu YOKTUR (denetim.sh TELEMETRIDE_KISISEL_VERI).
    /// </summary>
    public DbSet<Modules.Telemetri.TelemetriOlcumu> TelemetriOlcumleri =>
        Set<Modules.Telemetri.TelemetriOlcumu>();

    /// <summary>
    /// PAKET P-28 (F-4 · R-2) — kiracıya harcanan destek dakikası.
    /// F-8 "destek" satırının TEK meşru kaynağıdır; hard delete YOKTUR
    /// (pasife alınır), PATCH ile düzeltilir.
    /// </summary>
    public DbSet<Modules.Telemetri.DestekDakikasi> DestekDakikalari =>
        Set<Modules.Telemetri.DestekDakikasi>();

    /// <summary>
    /// PAKET YASAL-1 — tarihli yasal parametreler (ulusal rakamlar). KİRACI-ÜSTÜ:
    /// IKiraciVarlik DEĞİLDİR, HasQueryFilter EKLENMEZ (kapsam denetiminde gerekçeli
    /// beyaz liste maddesi). Okuma yalnız YasalParametreServisi, yazma yalnız
    /// migration + sistem anahtarlı uç.
    /// </summary>
    public DbSet<YasalParametre> YasalParametreler => Set<YasalParametre>();

    /// <summary>
    /// PAKET YASAL-4 / Y4-B — stopaj ödemeleri (append-only). IKiraciVarlik: global
    /// kiracı süzgeci OTOMATİK. Borç TÜRETİLİR (gider_pusulalari), burada yalnız ödeme.
    /// </summary>
    public DbSet<StopajOdemesi> StopajOdemeleri => Set<StopajOdemesi>();

    /// <summary>
    /// PAKET YASAL-3 (§ 4.3) — saklama bekletmeleri (kendiliğinden maskelemeyi
    /// gerekçeyle durdurur). IKiraciVarlik: global kiracı süzgeci OTOMATİK kurulur.
    /// </summary>
    public DbSet<SaklamaBekletme> SaklamaBekletmeleri => Set<SaklamaBekletme>();

    /// <summary>
    /// PAKET YASAL-3 (§ 4.4) — resmî yaptırım (malvarlığı dondurma) listesi. KİRACI-ÜSTÜ:
    /// IKiraciVarlik DEĞİLDİR, HasQueryFilter EKLENMEZ (kapsam denetiminde gerekçeli
    /// beyaz liste maddesi). Yazma yalnız sistem anahtarlı uç; satır silinmez.
    /// </summary>
    public DbSet<YaptirimKaydi> YaptirimKayitlari => Set<YaptirimKaydi>();

    /// <summary>PAKET YASAL-3 (§ 4.4) — kiracının liste eşleşmeleri (IKiraciVarlik).</summary>
    public DbSet<YaptirimEslesmesi> YaptirimEslesmeleri => Set<YaptirimEslesmesi>();

    /// <summary>
    /// PAKET YASAL-2 (TB-16) — geç fark edilen nakit sınırı aşımları. IKiraciVarlik
    /// (global kiracı süzgeci otomatik); ŞUBE TAŞIMAZ — ihlal mükellef genelidir.
    /// </summary>
    public DbSet<TevsikIhlali> TevsikIhlalleri => Set<TevsikIhlali>();

    // ── P-04 (Y-1) — ŞUBE SÜZGECİ DIŞINDA KALAN VARLIKLAR (tam liste, gerekçeli) ──
    //
    //   Kullanici.SubeId, kaydın şubesi DEĞİL, kullanıcının BAĞLI OLDUĞU şubedir
    //   (DenetimYazmaDinleyicisi'ndeki aynı ayrım). Süzgece alınsaydı kullanıcı
    //   yönetimi ekranı kendi şubesi dışındaki personeli göremez, giriş sonrası
    //   "ben" sorgusu ve rol yönetimi bozulurdu. Şube bazlı personel kısıtı bir
    //   YETKİ konusudur ve [RolGerekli] ile çözülür; süzgeçle değil.
    private static readonly Type[] SubeSuzgecDisi = [typeof(Kullanici)];

    private static readonly MethodInfo FiltreMetodu =
        typeof(KuyumcuDbContext).GetMethod(nameof(KiraciFiltresiKur),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    // `this` yakalanır; EF her sorguda güncel context ile yeniden bağlar.
    // Kiracı kimliği SABİT GÖMÜLMEZ — model önbelleği bunu ayırt eder.
    private void KiraciFiltresiKur<T>(ModelBuilder mb) where T : class, IKiraciVarlik =>
        mb.Entity<T>().HasQueryFilter(e => e.TenantId == kiraciId);

    private static readonly MethodInfo SubeFiltreMetodu =
        typeof(KuyumcuDbContext).GetMethod(nameof(KiraciVeSubeFiltresiKur),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    // ── P-04 (Y-1): TEK İFADE. EF bir varlığa YALNIZ BİR HasQueryFilter tutar;
    // ikinci çağrı birincisini SESSİZCE EZER. Bu yüzden kiracı ve şube şartı
    // ayrı ayrı değil, burada AND ile tek ifadede birleşir.
    //
    // `SubeId == null` bacağı: şubesiz (kiracı geneli) kayıtlar — ör. stok dışı
    // açılış kalemi, şubesiz sipariş — GİZLENMEZ. Bunlar hiçbir şubeye ait
    // olmadığı için bir şubeden diğerine sızma üretmezler.
    private void KiraciVeSubeFiltresiKur<T>(ModelBuilder mb) where T : class, IKiraciVarlik, ISubeVarlik =>
        mb.Entity<T>().HasQueryFilter(e => e.TenantId == kiraciId && (e.SubeId == null || e.SubeId == subeId));

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Kullanici>().HasIndex(k => new { k.Email, k.TenantId }).IsUnique();
        mb.Entity<Fatura>().HasIndex(f => new { f.FaturaNo, f.TenantId }).IsUnique();
        mb.Entity<RolYetki>().HasIndex(y => new { y.TenantId, y.Rol, y.Modul }).IsUnique();

        // ── ADIM 1 §2 ölçümü: tenant_id üzerinde öncü indeks 15 tablodan 14'ünde YOKTU.
        // Filtre her sorguya `tenant_id = @p` eklediği için indekssiz sütun tam
        // tarama demekti. Sıcak tablolarda tarih bileşiği de eklendi.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.Tarih });
        // Tevsik kümülatif nakit toplamı (tenant, cari, gün) üzerinden okur ve
        // karma ödemeyle birlikte HER satışta koşacak. Yalnız (tenant, tarih)
        // indeksiyle bu sorgu günün tüm hareketlerini tarardı.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.CariId, h.Tarih });
        // Fişin kalemleri tek sorguda toplanabilsin (fatura damgası, fiş görüntüleme).
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.FisId });
        // "Bu satıştan ne kadarı iade edilmiş" sorgusu her iadede koşar.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.IadeKaynakHareketId });
        // Paket 48 Faz A — "bu hareket zaten stornolandı mı" sorgusu her storno
        // isteğinde çalışır; ayrıca StornoSuzgeci kümülatiflerde bunu kullanır.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.StornoKaynakId });
        // PAKET P-20b (K-13) — "bu hurda satışının tahlil farkı bacakları
        // hangileri" sorgusu hem düzeltme ucunda hem ekranda koşar; indekssiz
        // kalsaydı her hurda satırı için defter taraması yapılırdı.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.TahlilKaynakHareketId });
        // Paket 20 — maden bazlı dönem raporları (gün sonu · aylık · has defteri)
        // "bu dönemde hangi madenden ne kadar" sorusunu bu indeksten karşılar;
        // yalnız (tenant, tarih) indeksiyle her maden kırılımı dönemin TÜM
        // satırlarını tarardı.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.Maden, h.Tarih });
        mb.Entity<KasaHareket>().HasIndex(h => new { h.TenantId, h.Tarih });

        // ── PAKET 34 — RAPOR İNDEKSLERİ ────────────────────────────────────
        // Her satırın yanında HANGİ raporun kullandığı yazılıdır; kullanıcısı
        // olmayan indeks yazma yolunu bedavaya yavaşlatır.
        //   R14 şube karşılaştırma + R10 işlem defteri şube süzgeci
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.SubeId, h.Tarih });
        //   R13 kaydeden kullanıcı raporu + R10 kullanıcı süzgeci
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.KullaniciId, h.Tarih });
        // P-08 / Y-13 — SATAN bazlı okuma (prim hesabı 1.1'de; indeks şimdi
        // konur ki 1.1 tablo taraması yapmasın). KullaniciId indeksinden AYRIDIR:
        // kaydeden ile satan farklı kişi olabilir.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.PersonelId, h.Tarih });
        //   R10 tür süzgeci (SATIS_IADE dökümü) + R19 iade özeti (Faz B)
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.Tur, h.Tarih });
        //   R16 stok devir hızı / ölü stok (Faz B) + ürün bazlı döküm
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.UrunId, h.Tarih });
        //   PAKET RAPOR-4 (KAR-HIZ): ürün bazlı kâr ön-toplamının geçersizleştirme denetimi — son denetimden
        //   bu yana OLUŞTURULAN satırlar (kapanmış aya düşen satış/iade, storno) bu indeksle aranır.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.Olusturuldu });
        // PAKET RAPOR-4 tur 2 (KAR-HIZ ölçümü): ürün bazlı kâr her istekte kiracının kart maliyeti girilmiş ürünlerini
        // okur (KartMaliyetleriToplu: tenant_id + alis_maliyeti > 0, id sırasıyla). İndeks yokken 50.000 ürünlü
        // kiracıda yalnız bu okuma 344 ms (tam satır taraması) sürüyor ve eşzamanlı istekte p95'i 1 sn üstüne itiyordu.
        // (tenant_id, alis_maliyeti) — InnoDB ikincil indeksi birincil anahtarı (id) taşır → kapsayan okuma.
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.AlisMaliyeti });
        //   R11 kasa defteri: kasa bazında gün gün devir/giriş/çıkış
        mb.Entity<KasaHareket>().HasIndex(h => new { h.TenantId, h.KasaId, h.Tarih });
        // PAKET S-3b: "bu kasa hareketi zaten stornolandı mı?" sorgusu indeksten okunur.
        mb.Entity<KasaHareket>().HasIndex(h => new { h.TenantId, h.StornoKaynakId });
        // ── PAKET P-19 (Y-11/Y-12) ──────────────────────────────────────────
        // "Bu bacağın karşı bacağı hangisi?" sorgusu HER virman stornosunda ve
        // her eşsiz-bacak taramasında koşar; tablo taraması yapmasın.
        mb.Entity<KasaHareket>().HasIndex(h => new { h.TenantId, h.KarsiBacakId });
        mb.Entity<BankaHareket>().HasIndex(h => new { h.TenantId, h.KarsiBacakId });
        // Kâr-zarar gider satırı: dönem + kategori bazlı toplam.
        mb.Entity<KasaHareket>().HasIndex(h => new { h.TenantId, h.GiderKategoriId, h.Tarih });
        // Kategori adı kiracı içinde TEKTİR: aynı adla iki "Kira" açılırsa
        // gider raporu aynı masrafı iki satıra böler ve toplam anlamsızlaşır.
        mb.Entity<GiderKategorisi>().HasIndex(g => new { g.TenantId, g.Ad }).IsUnique();
        mb.Entity<GiderKategorisi>().HasIndex(g => new { g.TenantId, g.Aktif });
        // Kapanış kasa+gün başına TEKTİR; benzersizlik veritabanı düzeyinde zorlanır
        // ki iki eşzamanlı kapanış isteği aynı günü iki kez kapatamasın.
        mb.Entity<FiyatListesi>().HasIndex(f => new { f.TenantId, f.Varsayilan });
        mb.Entity<Kampanya>().HasIndex(k => new { k.TenantId, k.Baslangic, k.Bitis });
        // Bir listede bir ürünün TEK geçersiz kılması olur; ikinci POST 409 döner
        // ve yarış hâlinde de veritabanı reddeder (paket 49).
        mb.Entity<FiyatListesiUrunFiyati>().HasIndex(x => new { x.TenantId, x.ListeId, x.UrunId }).IsUnique();
        // Önizleme ve toplu uygulama listenin TÜM geçersiz kılmalarını tek sorguda okur.
        mb.Entity<FiyatListesiUrunFiyati>().HasIndex(x => new { x.TenantId, x.ListeId });
        mb.Entity<SayimOturumu>().HasIndex(s => new { s.TenantId, s.SubeId, s.Durum });
        mb.Entity<SayimSatiri>().HasIndex(s => new { s.TenantId, s.OturumId, s.UrunId }).IsUnique();
        // Liste sorgusunun TAMAMI bu indeksten karşılanır (paket 4).
        mb.Entity<Belge>().HasIndex(b => new { b.TenantId, b.SahipTuru, b.SahipId });
        // Aynı dosyanın ikinci kez yüklendiği görülebilsin — BENZERSİZ DEĞİL
        // (aynı sertifika iki ürüne meşru şekilde bağlanabilir).
        mb.Entity<Belge>().HasIndex(b => new { b.TenantId, b.Sha256 });
        mb.Entity<GunKapanisi>()
            .HasIndex(g => new { g.TenantId, g.KasaId, g.YerelGun }).IsUnique();
        // ── PAKET 40-A — RİSK PROFİLİ İNDEKSLERİ ────────────────────────────
        //   (a) "bu carinin GÜNCEL profili" = en son olusturuldu satırı; tarihçe
        //       ucu da son 10 satırı bu indeksten okur.
        mb.Entity<CariRiskProfili>().HasIndex(p => new { p.TenantId, p.CariId, p.Olusturuldu });
        //   (b) panel/özet kartı: seviye dağılımı ve yüksek riskli cari listesi.
        mb.Entity<CariRiskProfili>().HasIndex(p => new { p.TenantId, p.Seviye });
        //   (c) "kaç cari PEP beyanı verdi" özeti cariler SICAK tablosunu
        //       taramasın diye tek indeks (3 kolon + 1 indeks sınırı, paket 40-A).
        mb.Entity<Cari>().HasIndex(c => new { c.TenantId, c.PepBeyani });

        // ── PAKET 31 — POS ALACAK DEFTERİ İNDEKSLERİ ────────────────────────
        // Her satırın yanında onu KULLANAN sorgu yazılıdır.
        //   cihaz listesi + kart bacağının cihaz çözümü (şube bazlı tek aktif cihaz)
        mb.Entity<PosCihaz>().HasIndex(c => new { c.TenantId, c.SubeId });
        //   aynı adda ikinci cihaz açılamaz (409 yarışta da veritabanından döner)
        mb.Entity<PosCihaz>().HasIndex(c => new { c.TenantId, c.Ad }).IsUnique();
        mb.Entity<PosCihaz>().HasIndex(c => new { c.TenantId, c.Aktif });
        //   valör takvimi: GROUP BY valor_tarihi (sunucuda toplanır, TB-19)
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.ValorTarihi });
        //   "valörü geçmiş ama yatmamış" uyarısı ve bekleyen alacak toplamı
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.Durum, t.ValorTarihi });
        //   cihaz bazlı defter dökümü
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.PosCihazId, t.IslemTarihi });
        //   "bu hareketin / bu kasa hareketinin POS satırı var mı" denetim sorgusu
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.HareketId });
        // PAKET P-22 / B-55: cihazın göç tutanakları zaman sırasıyla okunur.
        mb.Entity<PosKasaGocu>().HasIndex(g => new { g.TenantId, g.PosCihazId, g.Olusturuldu });
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.KasaHareketId });
        //   Faz B: özet yeniden hesabı (tahsilatın eşleşmeleri) ve banka tarafı
        mb.Entity<PosYatmaEslesme>().HasIndex(e => new { e.TenantId, e.PosTahsilatId });

        // ── PAKET P-23 (R-17) — ödeme kalemi erişim yolları ──────────────────
        // (tenant, fis) : "bu fiş neyle ödendi" — FisOku ve storno bu yoldan girer.
        // (tenant, hareket) : hareketten kalemlerine — storno motoru bu yoldan girer.
        // (tenant, arac, olusturuldu) : araç kırılımlı gün sonu/rapor taraması.
        mb.Entity<OdemeKalemi>().HasIndex(k => new { k.TenantId, k.FisId });
        mb.Entity<OdemeKalemi>().HasIndex(k => new { k.TenantId, k.HareketId });
        mb.Entity<OdemeKalemi>().HasIndex(k => new { k.TenantId, k.Arac, k.Olusturuldu });
        mb.Entity<PosYatmaEslesme>().HasIndex(e => new { e.TenantId, e.BankaHareketId });
        //   aynı çiftin aynı SIRADAKİ eşleşmesi iki kez yazılamaz — yarış hâlinde veritabanı
        //   reddeder. ZO-ISLEM-A (28.09): sıra (cift_sira) indekse girdi; geri alınan eşleşme
        //   iz olarak kalır ve aynı çiftin yeniden/kısmi eşleşmesini artık kilitlemez.
        mb.Entity<PosYatmaEslesme>()
            .HasIndex(e => new { e.TenantId, e.PosTahsilatId, e.BankaHareketId, e.CiftSira }).IsUnique();
        //   taksit oranı çözümü: (cihaz, taksit, geçerlilik) ve aktif oran listesi
        mb.Entity<PosTaksitOran>()
            .HasIndex(o => new { o.TenantId, o.PosCihazId, o.TaksitAdedi, o.GecerliBaslangic });
        mb.Entity<PosTaksitOran>().HasIndex(o => new { o.TenantId, o.PosCihazId, o.Aktif });

        mb.Entity<BankaHareket>().HasIndex(h => new { h.TenantId, h.Tarih });
        // PAKET P-12 (B-42): "bu dekont zaten stornolandı mı" sorgusu her storno
        // denemesinde ve her hareket listesinde koşar; kiracı + kaynak ekseni.
        mb.Entity<BankaHareket>().HasIndex(h => new { h.TenantId, h.StornoKaynakId });
        // ⚠ PAKET 41 — ZORUNLU, isteğe bağlı değil: MasakKapisi.GunlukIslemToplam
        // bu katmanın EN SICAK sorgusudur (her satışta ve her dekontta koşar) ve
        // artık banka_hareketleri'ni de cari bazında okur. Tarih ekseni
        // OLUSTURULDU'dur (Mehmet K41-1): dekontun `tarih` alanı KULLANICI
        // girdisidir ve geçmişe verilebilir; kümülatif ona bağlanırsa eşik bölme
        // (structuring) yolu açılır. Sorgu hangi kolonu okuyorsa indeks o kolonda.
        mb.Entity<BankaHareket>().HasIndex(h => new { h.TenantId, h.CariId, h.Olusturuldu });

        // ── PAKET 32 FAZ A — BANKA EKSTRESİ İÇE AKTARMA ─────────────────────
        // (a) MÜKERRER SON SAVUNMASI. İki eşzamanlı onay isteği aynı sahne
        //     satırını iki kez yazamaz: ikinci INSERT veritabanından döner.
        //     ⚠ SINIRI DÜRÜSTÇE: MySQL'de NULL değerler bu kısıtı İHLAL ETMEZ;
        //     paket 32 öncesi satırların dedupe_anahtar'ı NULL'dur ve migration
        //     geriye doldurmaz. Geçmişe karşı koruma YOKTUR (BankaHareket
        //     .DedupeAnahtar açıklaması).
        mb.Entity<BankaHareket>()
            .HasIndex(h => new { h.TenantId, h.BankaHesapId, h.DedupeAnahtar }).IsUnique();
        // (b) İthalat geçmişi listesi (yeni → eski).
        mb.Entity<EkstreIthalati>().HasIndex(i => new { i.TenantId, i.Olusturuldu });
        // (c) "Bu dosyayı bu hesaba zaten yüklemiştim" kapısı (409).
        mb.Entity<EkstreIthalati>().HasIndex(i => new { i.TenantId, i.BankaHesapId, i.Sha256 });
        // (d) Aynı dosyanın aynı satırı iki kez sahneye yazılamaz.
        mb.Entity<EkstreSatiri>()
            .HasIndex(s2 => new { s2.TenantId, s2.IthalatId, s2.SatirNo }).IsUnique();
        // (e) Ekranın SAYFALI listesi ve durum süzgeci bu indeksten okur (TB-19).
        mb.Entity<EkstreSatiri>().HasIndex(s2 => new { s2.TenantId, s2.IthalatId, s2.Durum });
        // (f) BİR IBAN TEK CARİYE. ⚠ Benzersizlik ŞİFRELİ sütun üzerinde
        //     kurulamaz (AES-GCM her yazmada farklı nonce üretir → aynı IBAN
        //     farklı şifreli metne iner ve kısıt hiç tetiklenmezdi). Kısıt HMAC
        //     hash kolonundadır — MasakHashDinleyicisi doldurur.
        mb.Entity<CariIban>().HasIndex(c => new { c.TenantId, c.IbanHash }).IsUnique();
        // (g) Cari kartından "bu müşterinin IBAN'ları".
        mb.Entity<CariIban>().HasIndex(c => new { c.TenantId, c.CariId });
        // (h) Ekstre satırının IBAN'ı da aranır (otomatik eşleşme yolu).
        mb.Entity<EkstreSatiri>().HasIndex(s2 => new { s2.TenantId, s2.KarsiIbanHash });
        mb.Entity<KurSnapshot>().HasIndex(k => new { k.TenantId, k.Olusturuldu });
        mb.Entity<Fatura>().HasIndex(f => new { f.TenantId, f.Tarih });
        // Paket 38: dönem bazlı "özel matrah uygulanan faturalar" dökümü
        // (KDV beyanının eki) bu indeksten okunur — tüm faturaları tarayıp
        // kdv_turu'ne göre elemeye gerek kalmaz.
        mb.Entity<Fatura>().HasIndex(f => new { f.TenantId, f.KdvTuru, f.Tarih });
        // Faturanın kalemleri tek sorguda, sıra ile okunur (belge görüntüleme).
        // Mevcut faturaların HEPSİ tanım gereği başlık düzeyidir; geri doldurma
        // GEREKMEZ çünkü varsayılan VERİTABANI DÜZEYİNDE yazılır. "Kalemleri boş"
        // ile "kalemsiz üretilmiş" karışırsa rapor bilinmeyeni sıfır sayar.
        mb.Entity<Fatura>().Property(f => f.KalemKaynagi).HasDefaultValue("BASLIK");
        mb.Entity<Fatura>().Property(f => f.HasDegerKaynagi).HasDefaultValue("ELLE");
        mb.Entity<FaturaKalemi>().HasIndex(k => new { k.TenantId, k.FaturaId });
        // "Bu defter satırı hangi faturada" sorusu ve POS–fatura mutabakatı.
        mb.Entity<FaturaKalemi>().HasIndex(k => new { k.TenantId, k.HareketId });
        mb.Entity<Sube>().HasIndex(s => s.TenantId);
        mb.Entity<RefreshToken>().HasIndex(r => r.TenantId);
        // PAKET AUTH-SIFIRLA: jeton özeti TEKİL (çakışma = aynı jeton iki satıra yazılamaz);
        // (tenant_id, kullanici_id) — "kullanıcının açık jetonlarını iptal et" sorgusu.
        mb.Entity<ParolaSifirlamaJetonu>().HasIndex(j => j.JetonHash).IsUnique();
        mb.Entity<ParolaSifirlamaJetonu>().HasIndex(j => new { j.TenantId, j.KullaniciId });
        mb.Entity<Cari>().HasIndex(c => c.TenantId);
        mb.Entity<CariBakiye>().HasIndex(c => c.TenantId);
        mb.Entity<Urun>().HasIndex(u => u.TenantId);
        mb.Entity<Stok>().HasIndex(s => s.TenantId);

        // ── PAKET P-04 (Y-1) — ŞUBE SÜZGECİ İNDEKSLERİ ───────────────────────
        // Global süzgeç artık her sorguya `tenant_id = @p AND (sube_id IS NULL
        // OR sube_id = @q)` ekliyor. Öncü (tenant_id, sube_id) indeksi olmayan
        // tabloda bu, kiracının TÜM satırlarını taratır; P-25 yük testi tam
        // burada patlardı. Zaten (tenant, sube, ...) öncüsü olan tablolar
        // (hareketler, sayim_oturumlari, pos_cihazlari, denetim, vezne_fisleri,
        // reyonlar, stok_yerlesimleri, stok_partileri) TEKRAR indekslenmedi.
        mb.Entity<Stok>().HasIndex(s => new { s.TenantId, s.SubeId });
        mb.Entity<Kasa>().HasIndex(k => new { k.TenantId, k.SubeId });
        mb.Entity<PosTahsilat>().HasIndex(t => new { t.TenantId, t.SubeId });
        mb.Entity<GunKapanisi>().HasIndex(g => new { g.TenantId, g.SubeId });
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.SubeId });
        mb.Entity<GiderPusulasi>().HasIndex(g => new { g.TenantId, g.SubeId });
        mb.Entity<Siparis>().HasIndex(s => new { s.TenantId, s.SubeId });
        mb.Entity<StokPartiTuketimi>().HasIndex(t => new { t.TenantId, t.SubeId });
        mb.Entity<AcilisKalemi>().HasIndex(a => new { a.TenantId, a.SubeId });

        // ── PAKET P-05 (Y-2) — DOĞAL ANAHTAR BENZERSİZLİK KISITLARI ──────────
        // NEDEN: bu dört doğal anahtar bugün YALNIZ uygulama katmanında
        // (AnyAsync ön kontrolü) korunuyordu. Ön kontrol ile INSERT arasındaki
        // pencerede aynı kodu yazan ikinci istek sessizce ikinci satırı
        // yaratıyordu (oku-sonra-yaz yarışı). Kısıt veritabanında olmadıkça
        // "benzersiz" bir temenni, sözleşme değildir.
        //
        // NULL TOLERANSI: MySQL'de UNIQUE indeks BİRDEN ÇOK NULL kabul eder
        // (SQL standardı; InnoDB'de doğrulandı — bkz. P-05 raporu § canlı).
        // Bu yüzden barkod/vergi_no için "filtreli indeks" GEREKMEZ; alanın
        // NULL'lanabilir kalması yeterlidir. Boş dizge ('') NULL DEĞİLDİR ve
        // ikinci boş dizge kısıtı ihlal ederdi — bu yüzden yazma yolunda
        // DogalAnahtar.Normalle() boş dizgeyi NULL'a çevirir.
        //
        // İNDEKS ADLARI ELLE VERİLİR: MySQL 1062 hatası anahtar ADINI döndürür
        // (Duplicate entry '…' for key 'urunler.UQ_urunler_tenant_kod').
        // Alan altı Türkçe mesaj bu ada göre seçilir (BenzersizlikSozlugu).
        mb.Entity<Stok>().HasIndex(s => new { s.TenantId, s.SubeId, s.UrunId })
            .HasDatabaseName("UQ_stoklar_tenant_sube_urun").IsUnique();
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.Kod })
            .HasDatabaseName("UQ_urunler_tenant_kod").IsUnique();
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.Barkod })
            .HasDatabaseName("UQ_urunler_tenant_barkod").IsUnique();
        // ⚠ FİLTRELİ KISIT (migration P05CariVergiNoBenzersiz, ham SQL): canlıda
        //   indeks (tenant_id, (CASE WHEN aktif=1 THEN vergi_no END)) olarak durur;
        //   benzersizlik YALNIZ AKTİF cariler arasında geçerlidir (PLAN'ın
        //   "filtreli/NULL toleranslı" maddesi — MySQL'de WHERE'li indeks yoktur,
        //   karşılığı fonksiyonel anahtar parçasıdır). Model burada düz UNIQUE
        //   bildirir; bu BİLEREK yapılan tek sapmadır (Pomelo filtreli indeks
        //   modelleyemez) ve denetim.sh P-05 bloğu CANLI şemadan doğrular.
        //   ⛔ DEĞİŞTİRME: bu bildirimi "düzeltmek" için model üzerinden indeks
        //   üretmeyin (EnsureCreated / diff migration). Düz indeks, PASİF+AKTİF
        //   meşru vergi no çiftini reddeder. Gerçek DDL FİLTRELİDİR; denetim.sh
        //   P-05: INDEKS_IFADESIZ sayacı düz indekse dönüşü yakalar.
        mb.Entity<Cari>().HasIndex(c => new { c.TenantId, c.VergiNo })
            .HasDatabaseName("UQ_cariler_tenant_vergi_no").IsUnique();
        mb.Entity<Kasa>().HasIndex(k => k.TenantId);
        mb.Entity<BankaHesap>().HasIndex(b => b.TenantId);
        // kullanicilar ve rol_yetkileri: mevcut bileşik indekslerin öncü sütunu
        // zaten uygun (Email+TenantId hariç — onun için ayrıca:)
        mb.Entity<Kullanici>().HasIndex(k => k.TenantId);

        // Denetim izi: kiracı+tarih (rapor), hassas kayıt süzmesi (KVKK denetimi)
        mb.Entity<DenetimKaydi>().HasIndex(d => new { d.TenantId, d.Tarih });
        mb.Entity<DenetimKaydi>().HasIndex(d => new { d.TenantId, d.Hassas, d.Tarih });
        mb.Entity<DenetimKaydi>().HasIndex(d => new { d.TenantId, d.VarlikTuru, d.VarlikId });
        // Paket 48 Faz A — "hangi şubede ne oldu" sorgusu (bağlam damgası).
        mb.Entity<DenetimKaydi>().HasIndex(d => new { d.TenantId, d.SubeId, d.Tarih });

        // ── Paket 6 Faz A — bildirim + iş çalışması indeksleri ───────────────
        // Aynı (iş, anahtar) İKİNCİ KEZ EKLENEMEZ: tek koşum kararını veritabanı
        // verir, uygulama "önce SELECT et" ile karar vermez (yarış koşulu kapalı).
        mb.Entity<IsCalismasi>().HasIndex(c => new { c.TenantId, c.IsKodu, c.CalismaAnahtari }).IsUnique();
        mb.Entity<IsCalismasi>().HasIndex(c => new { c.TenantId, c.Baslangic });
        // Aynı tekillik anahtarıyla İKİNCİ bildirim EKLENEMEZ: iş günde 96 kez
        // koşsa da kullanıcı TEK satır görür.
        mb.Entity<Bildirim>().HasIndex(b => new { b.TenantId, b.TekillikAnahtari }).IsUnique();
        mb.Entity<Bildirim>().HasIndex(b => new { b.TenantId, b.Kapatildi, b.Olusturuldu });
        mb.Entity<Bildirim>().HasIndex(b => new { b.TenantId, b.IlgiliVarlikTuru, b.IlgiliVarlikId });

        // ── PAKET P-37 — kullanıcı tercihi: (kiracı, kullanıcı, anahtar) TEKİL.
        //    Tekillik olmadan aynı anahtar iki satıra düşer ve hangisinin geçerli
        //    olduğu veriye bakılarak söylenemez (md.1: bilinmeyen tahmin edilmez).
        mb.Entity<KullaniciTercihi>()
            .HasIndex(t => new { t.TenantId, t.KullaniciId, t.Anahtar }).IsUnique();

        // ── Paket 3 Faz A — sipariş çatısı ───────────────────────────────────
        // Sipariş numarası kiracı içinde TEKİLDİR ve tekilliği VERİTABANI verir:
        // iki eşzamanlı oluşturma aynı numarayı ALAMAZ (uygulama "önce SELECT"
        // ile karar vermez; çakışan INSERT sınırlı döngüyle yeniden denenir).
        mb.Entity<Siparis>().HasIndex(s => new { s.TenantId, s.No }).IsUnique();
        // "Geciken sipariş" sorgusu bu indekse bağlıdır; unutulursa tam tarama olur.
        mb.Entity<Siparis>().HasIndex(s => new { s.TenantId, s.Durum, s.SozVerilenGun });
        mb.Entity<Siparis>().HasIndex(s => new { s.TenantId, s.Tur, s.Durum });
        mb.Entity<Siparis>().HasIndex(s => new { s.TenantId, s.CariId });
        mb.Entity<SiparisKalemi>().HasIndex(k => new { k.TenantId, k.SiparisId });
        mb.Entity<SiparisGecisi>().HasIndex(g => new { g.TenantId, g.SiparisId, g.Olusturuldu });

        // ── PAKET P-21 ──────────────────────────────────────────────────────
        // Rezervasyon: kiracı + şube + ürün + durum üzerinden sorgulanır
        // ("bu üründe kaç adet aktif rezerv var" satış kapısının sorusudur).
        mb.Entity<Rezervasyon>().HasIndex(r => new { r.TenantId, r.SubeId, r.UrunId, r.Durum });
        mb.Entity<Rezervasyon>().HasIndex(r => new { r.TenantId, r.CariId, r.Durum });
        mb.Entity<Rezervasyon>().HasIndex(r => new { r.TenantId, r.SiparisId });
        mb.Entity<Rezervasyon>().HasIndex(r => new { r.TenantId, r.Durum, r.BitisGunu });
        // Y-18: "bu siparişin defter karşılığı" sorgusu.
        mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.SiparisId });

        // ── Paket 19 Faz A — vezne modeli ────────────────────────────────────
        // Vezne kuyruğu TEK indeksten okunur: kiracı + şube + durum + tarih.
        mb.Entity<VezneFisi>().HasIndex(f => new { f.TenantId, f.SubeId, f.Durum, f.Olusturuldu });
        // Aynı vezne fişinin İKİ KEZ deftere yazılması veritabanı düzeyinde
        // imkânsız. ⚠ MySQL'de NULL'lar benzersizliğe girmez — bu yüzden indeks
        // TEK BAŞINA yeterli sayılmaz; asıl koruma tahsil yolundaki iyimser
        // kilittir (VezneFisi.Guncellendi eşzamanlılık jetonu).
        mb.Entity<VezneFisi>().HasIndex(f => new { f.TenantId, f.HareketFisId }).IsUnique();
        // İYİMSER KİLİT: SaveChanges WHERE guncellendi = @eski üretir; ikinci
        // eşzamanlı tahsil DbUpdateConcurrencyException alır → 409.
        mb.Entity<VezneFisi>().Property(f => f.Guncellendi).IsConcurrencyToken();
        mb.Entity<VezneFisKalemi>().HasIndex(k => new { k.TenantId, k.FisId, k.Sira });
        // Bir kiracıda bir ayar anahtarı TEKTİR; yarışta ikinci INSERT düşer.
        mb.Entity<KiraciAyari>().HasIndex(a => new { a.TenantId, a.Anahtar }).IsUnique();

        // ⚠ PAKET 41: kaynak_tur kolonunun VERİTABANI VARSAYILANI 'HAREKET'tir ve
        // migration'da (20260917235727_MasakBankaKaynagi) elle yazılıdır. Kolon 8
        // yıl saklanan append-only tablolara SONRADAN eklendi; varsayılan
        // yazılmasaydı paket öncesi tüm satırlar BOŞ kaynakla kalır ve "bu tespit
        // hangi işleme ait" sorusu denetimde cevapsız kalırdı. Model tarafında
        // HasDefaultValue KULLANILMADI: varlık zaten her zaman açık değer yazar
        // (MasakKapisi.Kaydet, zorunlu kaynakTur) ve model ile anlık görüntü
        // arasında sahte bir fark doğmasın.
        // MASAK: kiracı+tarih; şüpheli bildirimde süre takibi için durum+son tarih
        mb.Entity<MasakKimlikTespiti>().HasIndex(m => new { m.TenantId, m.Tarih });
        mb.Entity<MasakKimlikTespiti>().HasIndex(m => new { m.TenantId, m.CariId });
        mb.Entity<MasakKimlikTespiti>().HasIndex(m => new { m.TenantId, m.SaklamaBitis });
        // Şifreli sütun aranmaz; eşitlik araması hash üzerinden (SIRADAKI-ADIM-KARAR §2)
        mb.Entity<MasakKimlikTespiti>().HasIndex(m => new { m.TenantId, m.BelgeNoHash });

        // ── Alan şifreleme: BelgeNo diske şifreli iner, bellekte düz metin görünür.
        // Converter delegeleri SQL'e çevrilmez; şifreleyici örneğini yakalamaları sorunsuz.
        var s = sifreleyici;
        mb.Entity<MasakKimlikTespiti>().Property(m => m.BelgeNo)
            .HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));

        // ── PAKET 47 — İÇE AKTARIM SAHNE TABLOSU ───────────────────────────
        // (a) Geçmiş ekranı ve durum süzgeci bu iki indeksten okur.
        mb.Entity<IceAktarimOturumu>().HasIndex(o => new { o.TenantId, o.Olusturuldu });
        mb.Entity<IceAktarimOturumu>().HasIndex(o => new { o.TenantId, o.Durum });
        // (b) Satır listesi ve "geçerli satırları uygula" sorgusu.
        mb.Entity<IceAktarimSatiri>().HasIndex(s2 => new { s2.TenantId, s2.OturumId, s2.SatirNo });
        mb.Entity<IceAktarimSatiri>().HasIndex(s2 => new { s2.TenantId, s2.OturumId, s2.Sonuc });
        // (c) ⚠ ADR-0003 / Mehmet koşulu: sahne tablosundaki ham satır DİSKTE
        //     ŞİFRELİDİR. Bu satır kaldırılırsa içe aktarılan telefon/vergi no
        //     düz metin olarak ice_aktarim_satirlari'nda kalır.
        mb.Entity<IceAktarimSatiri>().Property(s2 => s2.HamJson)
            .HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));

        // ── PAKET 32 / AS-2 (Mehmet) — EKSTRENİN KİŞİSEL VERİSİ ─────────────
        // Karşı taraf IBAN'ı ve ham satır KİŞİSEL VERİDİR; ekstre bunu ölçekli
        // hâle getirir (ayda yüzlerce satır). Bu üç satır kaldırılırsa IBAN'lar
        // ekstre_satirlari ve cari_ibanlari tablolarında DÜZ METİN kalır.
        // ⚠ `!` BİLİNÇLİDİR (KiymetliEvrak deseni): alan nullable, EF Core
        //   dönüştürücüyü NULL için ÇAĞIRMAZ.
        mb.Entity<EkstreSatiri>().Property(e => e.KarsiIban)
            .HasConversion(duz => s.Sifrele(duz!), sifreli => s.Coz(sifreli!));
        mb.Entity<EkstreSatiri>().Property(e => e.HamSatir)
            .HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));
        mb.Entity<CariIban>().Property(c => c.Iban)
            .HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));

        // ── PAKET T-2 — KURULUŞ SİHİRBAZI + AÇILIŞ İZİ ─────────────────────
        // (a) Kiracı başına TEK kurulum satırı. Kod yolu da tekilliği korur,
        //     ama eşzamanlı iki istek ikisini birden yazabilirdi.
        mb.Entity<KurulumDurumu>().HasIndex(k => k.TenantId).IsUnique();
        // (b) FAIL-CLOSED 7: aynı hedefe ikinci açılış YOKTUR. Anahtar hedef_id
        //     DEĞİL benzersiz_anahtar'dır — STOK'ta şube boyutunu içerir, yani
        //     aynı ürünün ikinci ŞUBEDEKİ meşru açılışı reddedilmez
        //     (Bertan + Mehmet ortak koşulu; AcilisKalemi.AnahtarUret).
        mb.Entity<AcilisKalemi>()
            .HasIndex(a => new { a.TenantId, a.Tur, a.BenzersizAnahtar }).IsUnique();
        // (c) Oturum geri alma ve "bu oturumda ne yazıldı" dökümü.
        mb.Entity<AcilisKalemi>().HasIndex(a => new { a.TenantId, a.OturumId });

        // ── PAKET T-3 — YEDEK OTOMASYONU ───────────────────────────────────
        // (a) Ekranın ilk sorusu "son yedek ne zaman": baslangic DESC taraması.
        mb.Entity<YedekKaydi>().HasIndex(y => y.Baslangic);
        // (b) "Son BAŞARILI TAM yedek" (saklama fail-closed 4 ve RPO ölçümü).
        mb.Entity<YedekKaydi>().HasIndex(y => new { y.Tur, y.Baslangic });
        // (c) Kuyruk taraması: kiracı + durum + sıra. Betiğin talep modu bunu kullanır.
        mb.Entity<YedekTalebi>().HasIndex(t => new { t.TenantId, t.Durum, t.Olusturuldu });
        // PAKET P-77: mevcut satırlar "YEDEK" türünde kalır (DB varsayılanı);
        // doğrulama talepleri "DOGRULAMA". Betik türe göre iş seçer.
        mb.Entity<YedekTalebi>().Property(t => t.Tur).HasDefaultValue(YedekTalepTuru.Yedek);
        mb.Entity<YedekTatbikati>().HasIndex(t => t.Tarih);

        // Gider pusulası: satıcı TCKN'si de aynı altyapıyla şifreli iner (Adım 6).
        mb.Entity<GiderPusulasi>().Property(g => g.SaticiTckn)
            .HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));
        mb.Entity<GiderPusulasi>().HasIndex(g => new { g.TenantId, g.PusulaNo }).IsUnique();
        // PAKET YASAL-4 tur 4: bir alış için en çok bir GEÇERLİ pusula (eşzamanlı yeniden düzenleme kilidi).
        mb.Entity<GiderPusulasi>().Property(g => g.GecerliHareketId)
            .HasComputedColumnSql("(case when `iptal_edildi` = 0 then `hareket_id` end)", stored: true);
        mb.Entity<GiderPusulasi>().HasIndex(g => new { g.TenantId, g.GecerliHareketId })
            .HasDatabaseName("UQ_gider_pusulalari_tenant_gecerli_hareket").IsUnique();
        mb.Entity<GiderPusulasi>().HasIndex(g => new { g.TenantId, g.TeslimTarihi });
        mb.Entity<GiderPusulasi>().HasIndex(g => new { g.TenantId, g.SaticiTcknHash });
        // PAKET YASAL-4 / Y4-B: dönem toplamı (tenant, donem); kasa satırı başına tek ödeme.
        // Eşzamanlılık kilidi: dönem içinde sıra benzersiz (ön ek (tenant, donem) sorguyu da karşılar).
        mb.Entity<StopajOdemesi>().HasIndex(o => new { o.TenantId, o.Donem, o.SiraNo }).IsUnique();
        mb.Entity<StopajOdemesi>().HasIndex(o => new { o.TenantId, o.HareketId }).IsUnique();

        // ── PAKET YASAL-3 — saklama bekletmesi + yaptırım listesi ──────────
        // Bekletme: iş her turda "aktif bekletme var mı" diye (tenant, bitis) okur.
        mb.Entity<SaklamaBekletme>().HasIndex(b => new { b.TenantId, b.Bitis });
        // Liste: tam eşleşme hash'ten; ad benzerliği normalize ad-soyaddan.
        mb.Entity<YaptirimKaydi>().HasIndex(y => y.KimlikNoHash);
        mb.Entity<YaptirimKaydi>().HasIndex(y => y.AdSoyad);
        mb.Entity<YaptirimKaydi>().Property(y => y.KimlikNo)
            .HasConversion(duz => s.Sifrele(duz!), sifreli => s.Coz(sifreli!));
        // Eşleşme: satış yolları "bu cari kilitli mi" diye (tenant, cari) okur.
        mb.Entity<YaptirimEslesmesi>().HasIndex(e => new { e.TenantId, e.CariId });
        mb.Entity<YaptirimEslesmesi>().HasIndex(e => new { e.TenantId, e.Durum });

        // ── PAKET 29 FAZ A — KIYMETLİ EVRAK (çek/senet portföyü) ───────────
        // Her indeksin yanında HANGİ sorgunun kullandığı yazılıdır.
        //   (a) Portföy listesi + vade takvimi: durum süzgeci + vade sıralaması.
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.Durum, e.VadeGunu });
        //   (b) Cari kartındaki "bu müşterinin borçlu olduğu evraklar".
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.BorcluCariId });
        //   (c) Cari kartındaki "bu müşteriden aldığımız evraklar" (borcu kapanan cari).
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.KaynakCariId });
        //   (d) Takvim TÜM durumlar için de sorulabilir; (a) durumla başladığı
        //       için durumsuz aralık sorgusunu karşılamaz.
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.VadeGunu });
        //   (e) ÇİFT GİRİŞ KAPISI. ⚠ Spesifikasyon bunu (tenant, tur, banka_adi,
        //       evrak_no) üzerinde istemişti; MySQL'de UNIQUE indeks ÇOKLU NULL'a
        //       izin verdiği ve SENET'te banka_adi NULL olduğu için o indeks
        //       senetleri HİÇ yakalamazdı. Bertan'ın birleşik koşulu uyarınca
        //       benzersizlik, asla null olmayan banka_ayirt_edici kolonu
        //       üzerinden kurulur (BankaAdi ?? evrakTuru). Servis içi elle
        //       kontrol TOCTOU'ya karşı İKİNCİ katman olarak KALIR (M29-07).
        mb.Entity<KiymetliEvrak>()
            .HasIndex(e => new { e.TenantId, e.EvrakTuru, e.BankaAyirtEdici, e.EvrakNo }).IsUnique();
        //   (f) Borçlu kimlik no ARAMASI: şifreli sütun aranmaz, hash aranır.
        mb.Entity<KiymetliEvrak>().HasIndex(e => new { e.TenantId, e.BorcluKimlikNoHash });

        // ── Alan şifreleme (ADR-0003): borçlunun TCKN/VKN'si DİSKE ŞİFRELİ İNER.
        // Bu satır kaldırılırsa kimlik numarası kiymetli_evraklar tablosunda
        // DÜZ METİN olarak kalır (M29-11 bunu ölçer).
        // ⚠ `!` BİLİNÇLİDİR: alan NULLABLE (senette borçlu kimliği çoğu kez
        //   bilinmez) ama EF Core değer dönüştürücüyü NULL için ÇAĞIRMAZ —
        //   null her zaman null'a eşlenir. Derleyici bunu bilemediği için
        //   uyarı verirdi; koşullu bir dal yazmak ölü kod olurdu.
        mb.Entity<KiymetliEvrak>().Property(e => e.BorcluKimlikNo)
            .HasConversion(duz => s.Sifrele(duz!), sifreli => s.Coz(sifreli!));
        mb.Entity<GiderPusulasi>().HasIndex(g => g.HareketId);
        // Ürün ağacı: alt kategori araması üst kimlik üzerinden yürür.
        mb.Entity<UrunKategorisi>().HasIndex(k => new { k.TenantId, k.UstId });
        mb.Entity<UrunKurali>().HasIndex(k => new { k.TenantId, k.Aktif, k.Sira });   // M4
        mb.Entity<Reyon>().HasIndex(r => new { r.TenantId, r.SubeId });
        // Paket 25: aynı şubede aynı adla iki reyon — bugüne kadar yalnız KODDA
        // engelleniyordu (KategoriModulu.Olustur), eşzamanlı iki POST ikisini de
        // yazabilirdi. Kural artık veritabanında da duruyor.
        mb.Entity<Reyon>().HasIndex(r => new { r.TenantId, r.SubeId, r.Ad }).IsUnique();

        // Paket 25 — stok yerleşimi: dörtlü anahtar TEKİLDİR (aynı ürün aynı
        // reyonda iki satır tutamaz), reyon içeriği ve düşüm yolu ayrı indeksten.
        mb.Entity<StokYerlesimi>()
            .HasIndex(y => new { y.TenantId, y.SubeId, y.UrunId, y.ReyonId }).IsUnique();
        mb.Entity<StokYerlesimi>().HasIndex(y => new { y.TenantId, y.ReyonId });
        mb.Entity<StokYerlesimi>().HasIndex(y => new { y.TenantId, y.SubeId, y.UrunId });
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.KategoriId });
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.ReyonId });
        mb.Entity<Urun>().HasIndex(u => new { u.TenantId, u.Konsinye });

        // Maliyet partileri: FIFO/LIFO sırası ürün+şube bazında yürür; açık
        // partiler her satışta sorgulanır, bu yüzden indeks zorunlu.
        mb.Entity<StokPartisi>().HasIndex(p => new { p.TenantId, p.SubeId, p.UrunId, p.GirisTarihi });
        mb.Entity<StokPartisi>().HasIndex(p => p.GirisHareketId);
        mb.Entity<StokPartiTuketimi>().HasIndex(t => new { t.TenantId, t.Tarih });
        mb.Entity<StokPartiTuketimi>().HasIndex(t => t.CikisHareketId);
        mb.Entity<StokPartiTuketimi>().HasIndex(t => t.PartiId);

        // Dışa aktarım: jeton araması (indirme yolu) ve kiracı geçmişi.
        mb.Entity<DisaAktarimTalebi>().HasIndex(t => t.JetonHash).IsUnique();
        mb.Entity<DisaAktarimTalebi>().HasIndex(t => new { t.TenantId, t.Olusturuldu });

        mb.Entity<MasakBeyani>().HasIndex(m => new { m.TenantId, m.Tarih });
        mb.Entity<MasakBeyani>().HasIndex(m => new { m.TenantId, m.KimlikTespitiId });
        mb.Entity<MasakSupheliBildirim>().HasIndex(m => new { m.TenantId, m.Durum, m.SonBildirimTarihi });
        mb.Entity<MasakSupheliBildirim>().HasIndex(m => new { m.TenantId, m.TespitTarihi });

        // UX-3: kiracı varsayılanı NOT NULL ve VERİTABANI DÜZEYİNDE .PROFESYONEL..
        // Geri-doldurma gerekmez — migration.dan önceki davranış zaten profesyoneldir;
        // mevcut kiracıların gördüğü ekran bu yüzden HİÇ değişmez.
        mb.Entity<Tenant>().Property(t => t.VarsayilanArayuzModu).HasDefaultValue("PROFESYONEL");

        // ── PAKET F-2-min — kiracı durumu ve davet ──────────────────────────
        // tenants.durum VERİTABANI DÜZEYİNDE 'AKTIF' varsayılanlıdır: migration
        // sonrası MEVCUT kiracıların yazması kesilmez (Riskler § "üretim durdurucu").
        mb.Entity<Tenant>().Property(t => t.Durum).HasDefaultValue(KiraciDurumlari.Aktif);
        // Süresi dolmuş deneme taraması (yönetici listesi) tam tarama yapmasın.
        mb.Entity<Tenant>().HasIndex(t => new { t.Durum, t.DenemeBitis });
        // Aynı jeton iki kez ÜRETİLEMEZ; eşzamanlı tüketim yarışını da bu benzersizlik
        // değil, kullanildi_mi koşullu güncellemesi çözer (SistemServisi/AuthServisi).
        mb.Entity<KiraciDaveti>().HasIndex(d => d.JetonHash).IsUnique();
        mb.Entity<KiraciDaveti>().HasIndex(d => new { d.Gecerlilik, d.KullanildiMi });

        // Paket 45: bir kullanıcıya İKİ MFA kaydı imkânsız (yarış hâlinde de veritabanı reddeder).
        mb.Entity<KullaniciMfa>().HasIndex(m => new { m.TenantId, m.KullaniciId }).IsUnique();
        // "Kalan kurtarma kodu" sayımı ve doğrulama taraması tek indeksten karşılanır.
        mb.Entity<MfaKurtarmaKodu>().HasIndex(k => new { k.TenantId, k.KullaniciId, k.Kullanildi });
        // Aynı hash iki kez üretilirse (kazara) veritabanı yakalar.
        mb.Entity<MfaKurtarmaKodu>().HasIndex(k => new { k.TenantId, k.KodHash }).IsUnique();

        // ── PAKET P-26 (Y-34 · Y-35) — TEDARİK ZİNCİRİ İNDEKSLERİ ───────────
        // Numara kiracı içinde TEKİLDİR: eşzamanlı iki POST aynı numarayı
        // yazamasın diye kural KODDA DEĞİL VERİTABANINDA durur (Reyon dersi).
        mb.Entity<SatinalmaSiparisi>().HasIndex(s => new { s.TenantId, s.No }).IsUnique();
        mb.Entity<SatinalmaSiparisi>().HasIndex(s => new { s.TenantId, s.SubeId, s.Durum });
        mb.Entity<SatinalmaSiparisi>().HasIndex(s => new { s.TenantId, s.TedarikciCariId });
        mb.Entity<SatinalmaKalemi>().HasIndex(k => new { k.TenantId, k.SatinalmaSiparisId });
        mb.Entity<Irsaliye>().HasIndex(i => new { i.TenantId, i.No }).IsUnique();
        // Bağsız irsaliye raporunun taradığı sütunlar (fatura_id NULL + durum).
        mb.Entity<Irsaliye>().HasIndex(i => new { i.TenantId, i.Durum, i.FaturaId });
        mb.Entity<Irsaliye>().HasIndex(i => new { i.TenantId, i.SubeId, i.Tarih });
        mb.Entity<Irsaliye>().HasIndex(i => new { i.TenantId, i.SatinalmaSiparisId });
        mb.Entity<IrsaliyeKalemi>().HasIndex(k => new { k.TenantId, k.IrsaliyeId });
        mb.Entity<IrsaliyeKalemi>().HasIndex(k => k.HareketId);

        // ── PAKET P-27 (F-1 · R-1) — ABONELİK ───────────────────────────────
        // (a) Aktif dönem sorgusu HER mutasyon isteğinin sıcak yolundadır
        //     (KiraciDurumKapisi önbellek ıskaladığında): tenant + durum +
        //     baslangic üçlüsü indekssiz kalsaydı tarihçe tam taranırdı.
        mb.Entity<Modules.Abonelik.KiraciAbonelik>()
            .HasIndex(a => new { a.TenantId, a.Durum, a.Baslangic });
        // (b) Ekrandaki tarihçe listesi (en yeni dönem başta).
        mb.Entity<Modules.Abonelik.KiraciAbonelik>()
            .HasIndex(a => new { a.TenantId, a.Baslangic });
        // (c) "Bu planda kaç kiracı var" — plan pasife alınmadan önce ölçülür.
        mb.Entity<Modules.Abonelik.KiraciAbonelik>()
            .HasIndex(a => new { a.TenantId, a.PlanKodu });
        // (d) Katalog sırası (ekranda seviyeye göre listelenir).
        mb.Entity<Modules.Abonelik.AbonelikPlani>().HasIndex(p => p.Seviye);

        // ── PAKET P-28 (F-4 · R-2) — telemetri indeksleri ────────────────────
        // (a) Pencere upsert'i ve özet sorgusu TEK indeksten geçer; BENZERSİZ
        //     olması kritik: aynı (kiracı, saat) ikinci kez doğarsa toplamlar
        //     iki satıra bölünür ve kapasite paydası sessizce yanlış çıkar.
        mb.Entity<Modules.Telemetri.TelemetriOlcumu>()
            .HasIndex(o => new { o.TenantId, o.OlcumSaati }).IsUnique();
        // (b) Destek özeti tarih aralığıyla sorgulanır.
        mb.Entity<Modules.Telemetri.DestekDakikasi>()
            .HasIndex(d => new { d.TenantId, d.Tarih });
        // (c) Ölçüme yalnız AKTİF kayıtlar girer.
        mb.Entity<Modules.Telemetri.DestekDakikasi>()
            .HasIndex(d => new { d.TenantId, d.Aktif, d.Tarih });

        // ── PAKET YASAL-1 — bir anahtarın bir başlangıç günü TEKTİR. Açık uçlu
        //     iki satır yasağı migration'daki işlevsel UNIQUE indekstedir
        //     (EF modeli işlevsel indeksi ifade edemez; ham SQL ile kurulur).
        //     PAKET YASAL-PARAM-2: aynı gün düzeltme satırı aynı başlangıç gününü paylaşır,
        //     ANI ile ayrışır → tekillik (anahtar, gün, an). Anı BOŞ satırlarda (tohum/ileri
        //     tarihli) gün başına tek satır kuralı migration'daki işlevsel indekste
        //     (ux_yasal_param_gun_tekil) sürer — eski güvence gevşemez.
        mb.Entity<YasalParametre>()
            .HasIndex(p => new { p.Anahtar, p.GecerlilikBaslangic, p.GecerlilikBaslangicAni }).IsUnique();

        // ── PAKET YASAL-2 (TB-16) — aynı (cari, gün, tür) İKİNCİ KEZ yazılamaz:
        //     iş aynı gün iki kez koşsa da tek satır (karar veritabanında).
        mb.Entity<TevsikIhlali>()
            .HasIndex(t => new { t.TenantId, t.CariId, t.IhlalGunu, t.Tur }).IsUnique();
        mb.Entity<TevsikIhlali>().HasIndex(t => new { t.TenantId, t.Durum, t.BildirimSonGunu });

        // ── Kapsama denetimi: kiracı işaretçisi olmayan yeni bir varlık eklenirse
        // UYGULAMA AÇILMAZ. Unutmayı derleme/başlangıç zamanına taşır.
        foreach (var varlik in mb.Model.GetEntityTypes())
        {
            if (varlik.ClrType == typeof(Tenant)) continue;   // kiracının kendisi
            // KASTEN KİRACISIZ (beyaz liste maddesi — paket F-2-min): davet,
            // kiracı DOĞMADAN önce var olur; tenant_id'ye yazılacak değer yoktur.
            // Korumasının tek katmanı SistemAnahtariKapisi'dır (kabul edilen risk).
            if (varlik.ClrType == typeof(KiraciDaveti)) continue;
            // KASTEN KİRACISIZ (beyaz liste — paket T-3): yedek KOŞUSU ve
            // geri-yükleme TATBİKATI tüm örneği kapsar; tenant_id'ye yazılacak
            // değer yoktur. Yazan taraf betiktir, okuyan uç [RolGerekli("SAHIP")]
            // ile korunur ve dosya adı/yolu İSTEMCİYE DÖNMEZ.
            if (varlik.ClrType == typeof(YedekKaydi)) continue;
            if (varlik.ClrType == typeof(YedekTatbikati)) continue;
            // KASTEN KİRACISIZ (beyaz liste — PAKET P-27): abonelik PLAN
            // KATALOĞU kiracı-üstüdür; aynı satır tüm kiracılar için geçerlidir
            // ve kiracı DOĞMADAN vardır (davet kalıbının aynısı). Kiracıya özel
            // olan şey plan değil, ABONELİK DÖNEMİdir (KiraciAbonelik) ve o
            // IKiraciVarlik'tir. Yazma yolunun tek koruması SistemAnahtariKapisi
            // ile korunan uçtur (kabul edilen risk, F-2-min ile aynı).
            if (varlik.ClrType == typeof(Modules.Abonelik.AbonelikPlani)) continue;
            // KASTEN KİRACISIZ (beyaz liste — PAKET YASAL-1): yasal parametreler
            // ULUSAL rakamlardır (tevsik haddi, MASAK eşiği…); aynı satır tüm
            // kiracılar için geçerlidir. Kiracının ekrandan değiştirmesi YOKTUR;
            // yazma yolu yalnız migration + SistemAnahtariKapisi ile korunan uçtur.
            if (varlik.ClrType == typeof(YasalParametre)) continue;
            // KASTEN KİRACISIZ (beyaz liste — PAKET YASAL-3): resmî yaptırım listesi
            // (6415/7262 s. K.) tüm kiracılar için aynıdır; kiracı ekranından yazma
            // YOKTUR. Yazma yolu yalnız SistemAnahtariKapisi ile korunan uçtur.
            if (varlik.ClrType == typeof(YaptirimKaydi)) continue;
            if (!typeof(IKiraciVarlik).IsAssignableFrom(varlik.ClrType))
                throw new InvalidOperationException(
                    $"{varlik.ClrType.Name} IKiraciVarlik uygulamıyor. Kiracı kapsamı dışında varlık olamaz; " +
                    "kasten kiracısızsa buraya açık bir beyaz liste maddesi ekle.");

            // ── P-04 (Y-1) ŞUBE KAPSAM DENETİMİ: sube_id sütunu olup ISubeVarlik
            // uygulamayan varlık = süzgeci unutulmuş varlık. UYGULAMA AÇILMAZ.
            if (varlik.FindProperty(nameof(ISubeVarlik.SubeId)) is not null
                && !typeof(ISubeVarlik).IsAssignableFrom(varlik.ClrType)
                && !SubeSuzgecDisi.Contains(varlik.ClrType))
                throw new InvalidOperationException(
                    $"{varlik.ClrType.Name} SubeId taşıyor ama ISubeVarlik uygulamıyor. Şube süzgeci dışında " +
                    "kalması KASITLI ise SubeSuzgecDisi beyaz listesine gerekçesiyle ekle.");
        }

        if (SistemModu) return;   // ← sistem modelinde filtre YOK (indeks dostu, OR'suz SQL)

        // ⚠ İSTİSNA (gerekçeli — P-04 DoD): Tenant'ın süzgeci AYRIDIR çünkü
        // Tenant IKiraciVarlik DEĞİLDİR; kiracı kimliğini `TenantId` değil `Id`
        // taşır ve jenerik ifade ona uygulanamaz. Tenant'ın şubesi de yoktur.
        mb.Entity<Tenant>().HasQueryFilter(t => t.Id == kiraciId);   // TenantId yok, Id var
        foreach (var varlik in mb.Model.GetEntityTypes()
                     .Where(e => typeof(IKiraciVarlik).IsAssignableFrom(e.ClrType)))
        {
            var metot = SubeFiltresiAcik && typeof(ISubeVarlik).IsAssignableFrom(varlik.ClrType)
                ? SubeFiltreMetodu
                : FiltreMetodu;
            metot.MakeGenericMethod(varlik.ClrType).Invoke(this, [mb]);
        }
    }

    // ── APPEND-ONLY: sipariş geçiş kaydı (paket 3 Faz A, Mehmet'in koşulu) ───
    // "ADR-0004 ruhu" demek zorlama DEĞİLDİR. Denetim izindeki kural nasıl
    // DenetimYazmaDinleyicisi içinde koda döküldüyse, siparis_gecisleri kuralı
    // da burada koda dökülür: UPDATE ya da DELETE denendiği anda SaveChanges
    // patlar — hangi servis, hangi geliştirici denerse denesin.
    //
    // ⚠ SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı ATLAR, burası onları
    //   görmez — KiraciYazmaDenetcisi ve denetim izi ile aynı sınır.
    //   denetim.sh ExecuteUpdate/Delete kullanımını beyaz listeyle sayar.
    private void GecisIziniKoru()
    {
        foreach (var giris in ChangeTracker.Entries<SiparisGecisi>())
            if (giris.State is EntityState.Modified or EntityState.Deleted)
                throw new SiparisGecisiDegistirilemez(giris.State);

        // PAKET P-22 / B-55: POS kasa göç tutanağı da APPEND-ONLY'dir. Göç
        // tutanağı değiştirilebilseydi "hangi tahsilat hangi kasaya düştü"
        // sorusunun cevabı sonradan yazılabilir olurdu.
        foreach (var giris in ChangeTracker.Entries<PosKasaGocu>())
            if (giris.State is EntityState.Modified or EntityState.Deleted)
                throw new SiparisGecisiDegistirilemez(giris.State);
    }

    // ── APPEND-ONLY: risk profili satırı (paket 40-A, Mehmet K40-5) ──────────
    // Korumanın yalnız serviste kalması yetmez: RiskServisi'ni atlayan HERHANGİ
    // bir kod yolu (yeni bir servis, bir betik, bir test) profili sessizce
    // güncelleyebilirdi. Kural SaveChanges'ta uygulanır — kim denerse denesin.
    //
    // ⚠ SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı ATLAR; denetim.sh
    //   onların sayısını beyaz listeyle dondurur (bu pakette 5'te SABİT kaldı).
    private void RiskProfiliniKoru()
    {
        foreach (var giris in ChangeTracker.Entries<CariRiskProfili>())
            if (giris.State is EntityState.Modified or EntityState.Deleted)
                throw new RiskProfiliDegistirilemez(giris.State);
    }

    // ── PAKET B-3 (karar D2 · K3/K4) — PEŞİN MÜŞTERİ SİSTEM CARİSİ KORUMASI ──
    // Kural serviste DE var (CariServisi.DeaktifEt/Guncelle, OdemeSepeti), ama
    // yalnız orada kalsaydı servisi atlayan HERHANGİ bir yol (Vezne fişi, emanet,
    // evrak, bir betik) kartı pasife alabilir ya da ona borç yazabilirdi.
    // Kural SaveChanges'ta uygulanır — kim denerse denesin:
    //   (1) sistem carisi SİLİNEMEZ, PASİFE ALINAMAZ, bayrağı KALDIRILAMAZ;
    //   (2) var olan bir cari sonradan sistem carisine ÇEVRİLEMEZ (tek yol tohumlama);
    //   (3) sistem carisinin bakiyesi (TL, has, döviz) SIFIRDAN BAŞKA olamaz —
    //       kimliksiz karta açık hesap yazılamaz (K3'ün son hattı).
    // ⚠ SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı ATLAR; denetim.sh
    //   onların sayısını beyaz listeyle dondurur (bu paket yeni kullanım EKLEMEDİ).
    private List<string>? SistemCarisiAdaylari()
    {
        foreach (var giris in ChangeTracker.Entries<Cari>())
        {
            if (giris.State is EntityState.Deleted && giris.Property(c => c.SistemCarisi).OriginalValue)
                throw new Modules.Cari.SistemCarisiDegistirilemez(Modules.Cari.SistemCarisi.SilinemezMesaji);
            if (giris.State is EntityState.Modified)
            {
                var onceki = giris.Property(c => c.SistemCarisi).OriginalValue;
                var simdi = giris.Entity.SistemCarisi;
                if (onceki != simdi)
                    throw new Modules.Cari.SistemCarisiDegistirilemez(onceki
                        ? Modules.Cari.SistemCarisi.SilinemezMesaji
                        : "Var olan bir cari Peşin Müşteri kartına çevrilemez.");
                if (simdi && !giris.Entity.Aktif)
                    throw new Modules.Cari.SistemCarisiDegistirilemez(Modules.Cari.SistemCarisi.SilinemezMesaji);
            }
            if (giris.State is EntityState.Added && giris.Entity.SistemCarisi && !giris.Entity.Aktif)
                throw new Modules.Cari.SistemCarisiDegistirilemez(Modules.Cari.SistemCarisi.SilinemezMesaji);
        }

        // Bakiyesi SIFIR OLMAYAN bakiye satırlarının cari kimlikleri — bunlardan
        // biri sistem carisiyse kayıt reddedilir. Sıfır bakiyeli satır (peşin
        // satışta son işlem tarihi güncellemesi gibi) aday bile değildir.
        List<string>? adaylar = null;
        foreach (var giris in ChangeTracker.Entries<CariBakiye>())
        {
            if (giris.State is not (EntityState.Added or EntityState.Modified)) continue;
            var b = giris.Entity;
            // (Karşılaştırma dizi üzerinden yapılır: denetim.sh "döviz kolonu
            //  atama" sayacı `UsdBakiye ==` biçimini atama sanar.)
            if (new[] { b.TlBakiye, b.HasGramBakiye, b.UsdBakiye, b.EurBakiye, b.GbpBakiye }
                .All(x => x == 0m)) continue;
            (adaylar ??= []).Add(b.CariId);
        }
        return adaylar;
    }

    private void SistemCarisiniKoru()
    {
        var adaylar = SistemCarisiAdaylari();
        if (adaylar is null) return;
        if (ChangeTracker.Entries<Cari>().Any(e => e.Entity.SistemCarisi && adaylar.Contains(e.Entity.Id))
            || Cariler.AsNoTracking().Any(c => adaylar.Contains(c.Id) && c.SistemCarisi))
            throw new Modules.Cari.SistemCarisiDegistirilemez(Modules.Cari.SistemCarisi.BakiyeYasakMesaji);
    }

    private async Task SistemCarisiniKoruAsync(CancellationToken iptal)
    {
        var adaylar = SistemCarisiAdaylari();
        if (adaylar is null) return;
        if (ChangeTracker.Entries<Cari>().Any(e => e.Entity.SistemCarisi && adaylar.Contains(e.Entity.Id))
            || await Cariler.AsNoTracking().AnyAsync(c => adaylar.Contains(c.Id) && c.SistemCarisi, iptal))
            throw new Modules.Cari.SistemCarisiDegistirilemez(Modules.Cari.SistemCarisi.BakiyeYasakMesaji);
    }

    // ── PAKET YASAL-3 tur 2 — YAPTIRIM (malvarlığı dondurma) KİLİDİ: TEK MERKEZİ KAPI ──
    // Hasım doğrulama (KRİTİK): kilit yalnız MasakKapisi.DogrulaKumulatif'te vardı;
    // avans, emanet, değişim, rezerv, konsinye, sipariş, vezne, evrak, banka ve cari
    // tahsilat yolları kilitli cariyle PARA/MAL hareketi yazabiliyordu. 6415 s. K.
    // md.10: dondurma kararına aykırı her işlem HÜKÜMSÜZDÜR → kural SaveChanges'ta
    // uygulanır — hangi modül, hangi uç, hangi betik denerse denesin (sistem
    // carisi korumasıyla aynı desen). Kapı, cari taşıyan her YENİ para/mal kaydına
    // bakar: kilitli (TAM eşleşme, karar yürürlükte) cariye yazılamaz → 400
    // YAPTIRIM_ENGEL. MASAK'a bildirim kayıtları (kimlik tespiti, şüpheli işlem
    // bildirimi), tevsik ihlali ve eşleşme kaydının kendisi kapı DIŞIDIR —
    // bildirim yükümlülüğü kilitten bağımsız sürer.
    // ⚠ SINIR: ExecuteUpdate/ExecuteDelete SaveChanges'ı ATLAR (beyaz liste 4).
    private static readonly Type[] YaptirimKapisiTurleri =
    [
        typeof(Hareket), typeof(BankaHareket), typeof(PosTahsilat), typeof(Siparis),
        typeof(Rezervasyon), typeof(VezneFisi), typeof(Irsaliye), typeof(Fatura),
    ];

    private List<string>? YaptirimAdaylari()
    {
        List<string>? adaylar = null;
        foreach (var giris in ChangeTracker.Entries())
        {
            if (giris.State != EntityState.Added) continue;
            if (Array.IndexOf(YaptirimKapisiTurleri, giris.Entity.GetType()) < 0) continue;
            if (giris.Property("CariId").CurrentValue is string cariId && cariId.Length > 0
                && !(adaylar ??= []).Contains(cariId))
                adaylar.Add(cariId);
        }
        return adaylar;
    }

    private IQueryable<string> KilitliKayitlar(List<string> cariler) =>
        YaptirimEslesmeleri.AsNoTracking()
            .Where(e => e.CariId != null && cariler.Contains(e.CariId) && e.Tur == YaptirimEslesmeTuru.Tam)
            .Select(e => e.YaptirimKaydiId);

    private static BadRequestException YaptirimEngeli() =>
        new BadRequestException(Modules.Masak.YaptirimServisi.EngelMesaji)
            .AlanIle("cariId").KodIle(Modules.Masak.YaptirimServisi.EngelKodu);

    private void YaptirimKilidiniKoru()
    {
        if (YaptirimAdaylari() is not { } cariler) return;
        var kayitlar = KilitliKayitlar(cariler).Take(1000).ToList();
        if (kayitlar.Count > 0 && YaptirimKayitlari.AsNoTracking().Any(y => kayitlar.Contains(y.Id) && y.Kaldirildi == null))
            throw YaptirimEngeli();
    }

    private async Task YaptirimKilidiniKoruAsync(CancellationToken iptal)
    {
        if (YaptirimAdaylari() is not { } cariler) return;
        var kayitlar = await KilitliKayitlar(cariler).Take(1000).ToListAsync(iptal);
        if (kayitlar.Count > 0
            && await YaptirimKayitlari.AsNoTracking().AnyAsync(y => kayitlar.Contains(y.Id) && y.Kaldirildi == null, iptal))
            throw YaptirimEngeli();
    }

    public override int SaveChanges(bool kabulEdilenDegisiklikler)
    {
        GecisIziniKoru();
        RiskProfiliniKoru();
        SistemCarisiniKoru();
        YaptirimKilidiniKoru();
        return base.SaveChanges(kabulEdilenDegisiklikler);
    }

    public override async Task<int> SaveChangesAsync(
        bool kabulEdilenDegisiklikler, CancellationToken iptal = default)
    {
        GecisIziniKoru();
        RiskProfiliniKoru();
        await SistemCarisiniKoruAsync(iptal);
        await YaptirimKilidiniKoruAsync(iptal);
        return await base.SaveChangesAsync(kabulEdilenDegisiklikler, iptal);
    }
}
