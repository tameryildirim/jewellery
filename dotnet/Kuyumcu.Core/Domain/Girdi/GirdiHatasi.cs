namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 (08.10, kurucu: "TC kontrolü bir yerde var diğerinde yok
// olmasın") — GİRDİ KURALLARININ TEK KAYNAĞI.
//
// Her veri türünün kuralı Kuyumcu.Core/Domain/Girdi/ altında TEK sınıftadır.
// Üç tüketici AYNI yöntemi çağırır:
//   • ekran bileşeni (Kuyumcu.Web/Bilesenler/Form/*) — alan altı anında hata,
//   • GecersizAlanKapisi — Kaydet kilidi (sebep = alan altı metniyle aynı),
//   • sunucu özniteliği (Kuyumcu.Api/Common/Dogrulama/*) — 400 + aynı metin.
// Metin Kuyumcu.Core/I18n/Sozlukler.Girdi.cs'ten (TR + EN) gelir; kural yalnız
// KOD döndürür, dil bilmez.
//
// Kaynak: dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md
// (B) katalog K1–K34. Her kuralın başında katalog numarası ve kaynak sınıfı
// (BİRİNCİL / STANDART / İŞ KURALI / TEYİTSİZ) yazılıdır.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Girdi kuralının bozulma türü. Metni <c>GirdiSozlugu</c> verir.</summary>
public enum GirdiKodu
{
    IbanBicim, IbanUzunluk, IbanKontrol,
    TelefonKarakter, TelefonHane, TelefonAralik, TelefonYabanci,
    EpostaBicim, EpostaUzun,
    TarihBicim, TarihGecersiz, TarihErken, TarihGec, TarihIleri, TarihCokEski,
    DogumGelecek, DogumResit, DogumCokEski,
    AralikTers, AralikUzun,
    SayiEksi, SayiSifir, SayiOndalik, SayiTavan, SayiEnAz, SayiEnCok, SayiTam,
    TasBrut, MilyemAralik, YuzdeAralik, GunAralik,
    BarkodKarakter, BarkodUzunluk, BarkodBosluk, Ean13Kontrol,
    AdKisa, AdHarf, AdUzun,
    AdresKisa, AdresHarf, AdresUzun,
    PasaportBicim,
    GerekceKisa, GerekceUzun,
    AyarBilinmiyor,
    KimlikRakam, KimlikHane, TcknGecersiz,
    // doğrulayıcı tur 1 (08.10): yabancı IBAN yazılırken eksik/tutmayan · TCKN-veya-VKN hane (10 ya da 11)
    IbanEksik, KimlikHaneIkili,
    // doğrulayıcı tur 3 (08.10): yabancı IBAN biçimi ("TR ile başlar" yabancı numarada yanıltıcıydı)
    IbanYabanciBicim,
    // PAKET KUR-BANT (09.10): elle girilen has/altın/döviz fiyatı makul bant dışı (FiyatBandi)
    FiyatMutlakDisi, FiyatBantDisi,
    // PAKET ISLEM-BANT (09.10): işlem içi has gram fiyatı güncel has fiyatına göre bant dışı (FiyatBandi.IslemDenetle)
    IslemFiyatBantDisi,
}

/// <summary>
/// Kural bozulması: kod + metne girecek değerler (sayı ya da gün). Dil bağımsızdır;
/// biçimleme <c>GirdiSozlugu.Mesaj</c>'da kullanıcının diline göre yapılır.
/// </summary>
public readonly record struct GirdiHatasi(GirdiKodu Kod, decimal? S1 = null, decimal? S2 = null, DateOnly? Gun = null);

/// <summary>
/// Ekran ile sunucunun AYNI uzunluk sınırını okuduğu sabitler (envanter F2:
/// "ekran maxlength = sunucu sınırı"). Yeni ekran/DTO sabiti buraya eklenir.
/// </summary>
public static class AlanSinirlari
{
    /// <summary>IBAN, boşluksuz (ISO 13616 en uzun 34).</summary>
    public const int Iban = 34;
    /// <summary>IBAN kutusu, 4'lü gruplardaki boşluklarla (34 + 8).</summary>
    public const int IbanGorunen = 42;
    public const int Telefon = 20;
    /// <summary>RFC 5321 §4.5.3.1 — yol sınırı 256 → adres en çok 254.</summary>
    public const int Eposta = 254;
    public const int EpostaYerel = 64;
    public const int EpostaAlan = 255;
    public const int Ad = 100;
    public const int Unvan = 200;
    public const int Adres = 500;
    public const int Pasaport = 20;
    public const int Barkod = 50;
    public const int GerekceEnAz = 20;
    public const int GerekceEnCok = 1000;
    /// <summary>MASAK kimlik tespiti — işlem amacı (MasakKapisi DTO ile aynı).</summary>
    public const int MasakIslemAmaci = 100;
    /// <summary>MASAK kimlik tespiti — belge no (MasakKapisi DTO ile aynı).</summary>
    public const int MasakBelgeNo = 50;

    // ── PAKET GOC-A (para ekranları) — ekran EnFazla = DTO StringLength, TEK sabit ──────────
    /// <summary>Banka adı / hesap adı (BankaModulu HesapOlusturDto · HesapGuncelleDto).</summary>
    public const int BankaAdi = 100;
    /// <summary>Hesap sahibi · dekont karşı taraf adı (varchar(150)).</summary>
    public const int HesapSahibi = 150;
    /// <summary>Dekont açıklaması — yeni dekont (HareketEkleDto).</summary>
    public const int DekontAciklama = 500;
    /// <summary>Dekont açıklaması — düzeltme (HareketDuzeltDto; ekstreden gelen uzun açıklama düzeltilebilsin diye).</summary>
    public const int DekontAciklamaDuzelt = 2000;
    /// <summary>Kasa adı (KasaOlusturDto · KasaGuncelleDto).</summary>
    public const int KasaAdi = 100;
    /// <summary>Kasa / gider / emanet / vezne hareket açıklaması.</summary>
    public const int HareketAciklama = 500;
    /// <summary>e-belge entegratörünün API anahtarı (20.10).</summary>
    public const int EBelgeAnahtari = 1000;
    /// <summary>e-belge fatura seri ön eki — GİB numarasının ilk 3 harfi.</summary>
    public const int EBelgeSeriOnEki = 3;
    /// <summary>Kategori adı (gider kategorisi).</summary>
    public const int KategoriAdi = 100;
    /// <summary>Vezne iptal gerekçesi — kolon varchar(500); alt sınır K30 (20).</summary>
    public const int VezneIptalGerekce = 500;
    /// <summary>Fatura kalem açıklaması (fatura_kalemleri.aciklama varchar(300)).</summary>
    public const int FaturaKalemAciklama = 300;
    /// <summary>Fatura açıklaması.</summary>
    public const int FaturaAciklama = 500;
    /// <summary>KUR KAYNAKLARI (17.10) — has bedeli kapanışının dayanak notu (ozel_matrah_kapanislari.dayanak varchar(300)).</summary>
    public const int KapanisDayanakEnCok = 300;
    /// <summary>Gider pusulası satıcı adı soyadı.</summary>
    public const int PusulaSaticiAd = 200;
    /// <summary>Gider pusulası mal açıklaması.</summary>
    public const int PusulaMal = 500;
    /// <summary>Stopaj tahakkuk fişi no.</summary>
    public const int StopajFisNo = 50;
    /// <summary>POS cihaz adı · banka adı.</summary>
    public const int PosCihazAd = 100;
    /// <summary>POS üye işyeri no · terminal no.</summary>
    public const int PosKod = 50;

    // ── PAKET GOC-B (kimlik ve uyum ekranları) — ekran EnFazla = DTO/kolon sınırı, TEK sabit ──
    /// <summary>Cari adı / firma adı (cariler.ad varchar(200); ekran 300 idi → 201–300 karakter 400 alıyordu).</summary>
    public const int CariAd = 200;
    /// <summary>Cari ünvanı (cariler.unvan varchar(300)).</summary>
    public const int CariUnvan = 300;
    /// <summary>Cari notları (cariler.notlar varchar(1000)).</summary>
    public const int CariNot = 1000;
    /// <summary>Muhasebe hesap kodu (cariler.hesap_kodu varchar(30)).</summary>
    public const int HesapKodu = 30;
    /// <summary>Uyruk kodu (ISO 3166-1 alpha-3).</summary>
    public const int UyrukKod = 3;
    /// <summary>MASAK kimlik tespiti — ad soyad (MasakBilgiDto).</summary>
    public const int MasakAdSoyad = 200;
    /// <summary>MASAK — beyan açıklaması (MasakBilgiDto 2000; ekran bilinçli 1000 idi → tek sabit).</summary>
    public const int MasakAciklama = 2000;
    /// <summary>MASAK — fon kaynağı / ek belge referansı (MasakBilgiDto 300).</summary>
    public const int MasakFonKaynagi = 300;
    /// <summary>Kıymetli evrak no (EvrakGirisDto 40).</summary>
    public const int EvrakNo = 40;
    /// <summary>Evrak banka adı / şubesi / keşide yeri (100).</summary>
    public const int EvrakBanka = 100;
    /// <summary>Evrak borçlu adı (200).</summary>
    public const int EvrakBorcluAd = 200;
    /// <summary>Evrak borçlu kimlik no (TCKN 11 / VKN 10; DTO 20).</summary>
    public const int EvrakBorcluKimlik = 20;
    /// <summary>Evrak notları · karşılıksız gerekçesi (500).</summary>
    public const int EvrakNot = 500;
    /// <summary>Şüpheli işlem açıklaması (MASAK ŞİB Rehberi 2.0 §4.2.7 azami 50.000; asgari 300).</summary>
    public const int SupheliAciklama = 50_000;
    /// <summary>Şüpheli bildirim kapatma gerekçesi (KapatDto 1000; ekran 2000 idi).</summary>
    public const int SupheliKapatma = 1000;
    /// <summary>Şüpheli bildirim gönderim referansı (GonderildiDto 100).</summary>
    public const int GonderimReferansi = 100;
    /// <summary>Elle risk seviyesi gerekçesi (masak_risk_profilleri gerekçe kolonu 500; ekran 1000 idi).</summary>
    public const int RiskElleGerekce = 500;
    /// <summary>Yaptırım eşleşmesi — MASAK evrak no (100) · not (500).</summary>
    public const int YaptirimEvrakNo = 100;
    public const int YaptirimNot = 500;
    /// <summary>Nakit sınırı aşımı — evrak no (60) · gerekçe (500).</summary>
    public const int NakitSiniriEvrakNo = 60;
    public const int NakitSiniriGerekce = 500;
    /// <summary>Bildirim kapatma notu (BildirimKapatDto 500).</summary>
    public const int BildirimNotu = 500;

    // ── PAKET GOC-C (ürün ve fiş ekranları) — ekran EnFazla = DTO MaxLength = kolon, TEK sabit ──
    /// <summary>Ürün stok kodu (urunler.kod varchar(100)).</summary>
    public const int UrunKod = 100;
    /// <summary>Ürün adı (urunler.ad varchar(300)).</summary>
    public const int UrunAd = 300;
    /// <summary>M4 — ürün kuralı adı ve "adında geçen" metni (sunucu MaxLength(100)).</summary>
    public const int UrunKuraliMetin = 100;
    /// <summary>M9: alış işçilik belgesinin numarası (alis_iscilik_belgeleri.belge_no).</summary>
    public const int IscilikBelgeNo = 50;
    /// <summary>D1-19 — KVKK aydınlatma metni (sunucu KvkkMetni.EnFazlaKarakter ile AYNI).</summary>
    public const int KvkkMetni = 20_000;
    /// <summary>D1-16 — ürün seti adı (sunucu UrunSetiDto/urun_setleri.ad ile AYNI).</summary>
    public const int UrunSetiAdi = 200;
    /// <summary>D1-04 — kur kilidi notu (kur_kilitleri.not_metni ile AYNI).</summary>
    public const int KurKilidiNotu = 200;
    /// <summary>D1-05 — mesai saati "SS:dd" (sunucu SatisOnayAyarDto deseniyle AYNI).</summary>
    public const int MesaiSaati = 5;
    /// <summary>D1-07 — kara liste sebebi (cariler.veresiye_engel_sebebi ile AYNI).</summary>
    public const int VeresiyeEngelSebebi = 200;
    /// <summary>D2-12 — müşterinin yüzük ölçüsü (cariler.yuzuk_olcusu ile AYNI).</summary>
    public const int CariYuzukOlcusu = 20;
    /// <summary>D2-12 — dilek/puan açıklaması.</summary>
    public const int MusteriNotu = 300;
    /// <summary>D3-11 — API anahtarı adı (api_anahtarlari.ad ile AYNI).</summary>
    public const int ApiAnahtariAdi = 100;
    /// <summary>M7: demo bankası havale açıklaması (sahte_banka_hareketleri.aciklama).</summary>
    public const int SahteHavaleAciklama = 200;
    /// <summary>Ürün açıklaması (urunler.aciklama varchar(1000)).</summary>
    public const int UrunAciklama = 1000;
    /// <summary>Sertifika no · seri no · taş ibaresi (varchar(60)).</summary>
    public const int UrunSertifikaNo = 60;
    public const int UrunSeriNo = 60;
    public const int UrunTasIbaresi = 60;
    /// <summary>Ölçü (varchar(20)).</summary>
    public const int UrunOlcu = 20;
    /// <summary>KMTS kodu · RFID (varchar(100)).</summary>
    public const int UrunKmts = 100;
    public const int UrunRfid = 100;
    /// <summary>Görsel adresi (varchar(500)).</summary>
    public const int UrunGorselUrl = 500;
    /// <summary>Reyon adı (reyonlar.ad varchar(100)).</summary>
    public const int ReyonAdi = 100;
    /// <summary>Ürün ağacı kategori adı (KategoriOlusturDto 150; ekran 100 idi).</summary>
    public const int UrunKategoriAdi = 150;
    /// <summary>Kategori sırası üst sınırı = sunucu KategoriOlusturDto Range(0, int.MaxValue) — ekran (int) taşmasın.</summary>
    public const int KategoriSiraEnCok = int.MaxValue;
    /// <summary>Fiyat listesi adı (FiyatListesiDto 100) · kampanya adı (KampanyaDto 150).</summary>
    public const int FiyatListesiAdi = 100;
    public const int KampanyaAdi = 150;
    /// <summary>Fiyat listesi ürün notu (fiyat_listesi_urun_fiyatlari.not varchar(200); ekran 500 idi).</summary>
    public const int FiyatUrunNotu = 200;
    /// <summary>PAKET EKSIK-B1-SATINALMA: satınalma siparişi / irsaliye açıklaması ve iptal gerekçesi (varchar(500)).</summary>
    public const int TedarikAciklama = 500;
    /// <summary>PAKET EKSIK-B1-SATINALMA: sipariş / irsaliye kalem tanımı (varchar(200)).</summary>
    public const int TedarikKalemTanim = 200;
    /// <summary>PAKET-42: sevk irsaliyesi taşıyan adı (irsaliyeler.tasiyan_ad varchar(100)).</summary>
    public const int IrsaliyeTasiyanAd = 100;
    /// <summary>PAKET-42: araç plakası (irsaliyeler.plaka varchar(20)).</summary>
    public const int IrsaliyePlaka = 20;
    /// <summary>PAKET-42: teslim alan adı (irsaliyeler.teslim_alan_ad varchar(150)).</summary>
    public const int IrsaliyeTeslimAlanAd = 150;
    /// <summary>Panoda ürün etiketi (PanoAyarPatchDto 40).</summary>
    public const int PanoEtiket = 40;
    /// <summary>Satış/fiş/diğer işlem açıklaması (DTO MaxLength 500).</summary>
    public const int IslemAciklama = 500;
    /// <summary>Gider pusulası satıcı bloğu (HurdaSaticiDto): ad 200 · adres 500 · tedarikçi fatura no 50 (ekran 60 idi).</summary>
    public const int SaticiAd = 200;
    public const int SaticiAdres = 500;
    public const int TedarikciFaturaNo = 50;
    /// <summary>Sipariş açıklaması (siparisler.aciklama 500) · kalem tanımı (siparis_kalemleri.tanim 200).</summary>
    public const int SiparisAciklama = 500;
    public const int SiparisKalemTanim = 200;
    /// <summary>Sipariş söz verilen günün en uzak ufku (K8c: sipariş teslim ≤ 2 yıl, İŞ KURALI).</summary>
    public const int SiparisSozEnCokYil = 2;
    /// <summary>Sayım oturumu adı (sayim_oturumlari.ad varchar(150)).</summary>
    public const int SayimAdi = 150;
    /// <summary>İçe/dışa aktarım ve açılış gerekçesi (DTO MaxLength 500; ekran 1000 idi).</summary>
    public const int AktarimGerekce = 500;
    /// <summary>İçe/dışa aktarım onay metni kutusu (DTO MaxLength 100; ekran 200 idi).</summary>
    public const int AktarimOnay = 100;
    /// <summary>Ürün/fiş arama kutusu (sorgu q) — sunucu bu uzunluktan fazlasını aramaz.</summary>
    public const int Arama = 100;
    /// <summary>Etiket kopya adedi en çok (UrunEtiketleri taslak sınırı ile aynı).</summary>
    public const int EtiketKopyaEnCok = 20;
    /// <summary>GOC-C-2 VERI-2: ürün kartı açılış stoğu üst sınırı (ekran &lt;Adet&gt;/&lt;Sayi&gt; EnCok = sunucu [AcilisAdedi] — TEK sabit).</summary>
    public const decimal AcilisStokEnCok = 1_000_000m;
}

/// <summary>Ortak yardımcılar (ondalık hane sayısı, harf var mı).</summary>
public static class GirdiYardim
{
    /// <summary>Değerin anlamlı ondalık hane sayısı (sondaki sıfırlar sayılmaz).</summary>
    public static int OndalikHane(decimal d)
    {
        d = Math.Abs(d);
        var n = 0;
        while (d != decimal.Truncate(d) && n < 28) { d *= 10; n++; }
        return n;
    }

    /// <summary>Metinde en az bir harf (Türkçe dâhil) var mı?</summary>
    public static bool HarfVar(string s)
    {
        foreach (var c in s) if (char.IsLetter(c)) return true;
        return false;
    }

    /// <summary>Kırpar ve ardışık boşlukları teke indirir.</summary>
    public static string BoslukSadelestir(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var b = new System.Text.StringBuilder(s.Length);
        var oncekiBosluk = false;
        foreach (var c in s.Trim())
        {
            var bosluk = char.IsWhiteSpace(c);
            if (bosluk && oncekiBosluk) continue;
            b.Append(bosluk ? ' ' : c);
            oncekiBosluk = bosluk;
        }
        return b.ToString();
    }
}
