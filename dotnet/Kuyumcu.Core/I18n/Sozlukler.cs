// ═══════════════════════════════════════════════════════════════════════════
// BU DOSYA ELLE DÜZENLENİR (üreteç depoda YOK) — K-UX-10 / UX-S12.
//
// Eski başlık "scripts/gen-i18n-cs.mjs tarafından packages/core/src/i18n
// kaynağından üretilmiştir, ELLE DÜZENLEMEYİN" diyordu. Bu uyarı ARTIK
// GEÇERSİZDİR ve yanıltıcıdır: proje kökünde ne `scripts/` ne `packages/`
// klasörü vardır (ls: No such file or directory) ve dosya en son paket 33'te
// elle düzenlenmiştir. Düzeltme paket UX-2 Faz A1'de yapıldı.
//
// KURAL: kullanıcıya görünen her yeni metin buraya TR **ve** EN olarak
// eklenir; Razor içine çıplak metin yazılmaz.
// ═══════════════════════════════════════════════════════════════════════════
namespace Kuyumcu.Core.I18n;

public sealed class SozlukOrtak
{
    public required string Kaydet { get; init; }
    /// <summary>P-36c: durum süzgeçlerinin ortak etiketi.</summary>
    public required string Durum { get; init; }
    /// <summary>P-36c/UX-S30: kayıt sürerken kilitli seçicinin sebebi.</summary>
    public required string Kaydediliyor { get; init; }
    public required string Iptal { get; init; }
    public required string Sil { get; init; }
    public required string Duzenle { get; init; }
    public required string Ekle { get; init; }
    public required string Ara { get; init; }
    public required string Istege { get; init; }
    // UX-4d madde 14 (kılavuz) + madde 15 (oturum) + madde 24 (kur kaynağı)
    public required string YeniSekmedeAc { get; init; }
    public required string KilavuzAciklama { get; init; }
    public required string KilavuzBolumler { get; init; }
    public required string OturumSuresiDoldu { get; init; }
    public required string SozlesmesizKaynakUyari { get; init; }
    public required string DisKaynakKapali { get; init; }
    public required string Geri { get; init; }
    public required string Tumu { get; init; }
    public required string Detay { get; init; }
    public required string Kapat { get; init; }
    public required string Evet { get; init; }
    public required string Hayir { get; init; }
    public required string Aktif { get; init; }
    public required string Pasif { get; init; }
    public required string Yukleniyor { get; init; }
    public required string Baslangic { get; init; }
    public required string Bitis { get; init; }
    public required string Hata { get; init; }
    public required string Basarili { get; init; }
    public required string Bulunamadi { get; init; }
    public required string Aciklama { get; init; }
    public required string Opsiyonel { get; init; }
    public required string Toplam { get; init; }
    public required string Tarih { get; init; }
    public required string Tutar { get; init; }
    public required string Adet { get; init; }
    public required string Degistir { get; init; }
    public required string CikisYap { get; init; }
    public required string Gecmis { get; init; }
    public required string Eylemler { get; init; }
    public required string Yenile { get; init; }
    /// <summary>P-36c/3: salt okunur alanda değer henüz yokken yazılan metin.</summary>
    public required string DegerYok { get; init; }
}

/// <summary>Paket 6 Faz A — bildirim ekranı ve zamanlanmış iş çatısı sözlüğü.</summary>
public sealed class SozlukBildirim
{
    public required string Baslik { get; init; }
    /// <summary>PAKET P-37c — ortak araç şeridi (SayfaBasi) açıklaması.</summary>
    public required string EkranAciklama { get; init; }
    public required string Acik { get; init; }
    public required string Kapali { get; init; }
    public required string Tumu { get; init; }
    public required string Onem { get; init; }
    public required string Kritik { get; init; }
    public required string Uyari { get; init; }
    public required string Bilgi { get; init; }
    public required string Kapat { get; init; }
    public required string KapatBaslik { get; init; }
    public required string KapatOnay { get; init; }
    public required string KapatmaNotu { get; init; }
    public required string KapatildiEtiket { get; init; }
    public required string Git { get; init; }
    public required string Yenile { get; init; }
    public required string BosDurum { get; init; }
    public required string BosDurumKapali { get; init; }
    public required string Kapatildi { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string KapaliPencere { get; init; }
    public required string DegismezGerekce { get; init; }
    public required string DegismezAlternatif { get; init; }
    public required string Isler { get; init; }
    public required string IslerAciklama { get; init; }
    public required string IsAdi { get; init; }
    public required string IsDurumu { get; init; }
    public required string SonCalisma { get; init; }
    public required string SimdiCalistir { get; init; }
    public required string Calisiyor { get; init; }
    public required string KapaliIs { get; init; }
    public required string KapaliIsIpucu { get; init; }
    public required string CalismaGecmisi { get; init; }
    public required string Anahtar { get; init; }
    public required string Sure { get; init; }
    public required string UretilenBildirim { get; init; }
    public required string IncelenenKayit { get; init; }
    public required string HataOzeti { get; init; }
    public required string ElleTetiklendi { get; init; }
    public required string RozetEtiket { get; init; }
    public required string YalnizYoneticiIs { get; init; }

    // ── PAKET P-11 (B-24) — zamanlanmış iş ayarları ──────────────────────────
    public required string Ayarlar { get; init; }
    public required string AyarlarAciklama { get; init; }
    public required string BaslangicSaat { get; init; }
    public required string BaslangicSaatIpucu { get; init; }
    public required string VadeUfukGun { get; init; }
    public required string VadeUfukGunIpucu { get; init; }
    public required string MasakUyariGun { get; init; }
    public required string MasakUyariGunIpucu { get; init; }
    public required string IsAcikKapali { get; init; }
    public required string AyarKaydet { get; init; }
    public required string AyarVarsayilanaDon { get; init; }
    public required string AyarKaydedildi { get; init; }
    public required string AyarAraligi { get; init; }
    public required string VarsayilanDeger { get; init; }
    public required string KiraciKapatti { get; init; }
    public required string KiraciKapattiIpucu { get; init; }
    public required string YapilandirmaKapaliIpucu { get; init; }
    public required string AyarDegismezGerekce { get; init; }
    public required string AyarDegismezYolu { get; init; }
}

/// <summary>Paket 3 Faz A — sipariş çatısı (müşteri siparişi · dış atölye · tamir).</summary>
/// <summary>
/// PAKET 29 FAZ A — çek/senet portföyü ekran metinleri.
/// ⚠ Razor içine ÇIPLAK METİN yazılmaz (dosya başı kuralı); ham veritabanı
///   kodu (PORTFOYDE, CEK) ekrana basılmaz, buradan çevrilir (UX-S31).
/// </summary>
public sealed class SozlukEvrak
{
    public required string Baslik { get; init; }
    public required string TakvimBaslik { get; init; }
    public required string SayfaAciklama { get; init; }
    public required string TakvimAciklama { get; init; }
    public required string Yeni { get; init; }
    public required string EvrakTuru { get; init; }
    /// <summary>P-36c/UX-S30: kayıtlı evrakta tür seçicisi kilitli — sebebi.</summary>
    public required string TurDegismez { get; init; }
    public required string Cek { get; init; }
    public required string Senet { get; init; }
    public required string Durum { get; init; }
    public required string Tutar { get; init; }
    public required string VadeGunu { get; init; }
    public required string EvrakNo { get; init; }
    public required string BankaAdi { get; init; }
    public required string BankaSube { get; init; }
    public required string KesideYeri { get; init; }
    public required string KesideTarihi { get; init; }
    public required string BorcluAd { get; init; }
    public required string BorcluKimlikNo { get; init; }
    public required string BorcluKimlikNoIpucu { get; init; }
    public required string KaynakCari { get; init; }
    public required string Notlar { get; init; }
    public required string Kaydet { get; init; }
    public required string Vazgec { get; init; }
    public required string Duzenle { get; init; }
    public required string TahsileVer { get; init; }
    public required string PortfoyeAl { get; init; }
    public required string TahsilEdildi { get; init; }
    public required string Karsiliksiz { get; init; }
    public required string Ciro { get; init; }
    public required string CiroDevredildi { get; init; }
    public required string Hedef { get; init; }
    public required string HedefKasa { get; init; }
    public required string HedefBanka { get; init; }
    public required string Kasa { get; init; }
    public required string BankaHesabi { get; init; }
    public required string Gerekce { get; init; }
    public required string GerekceIpucu { get; init; }
    public required string OnayBaslik { get; init; }
    public required string OnayMetin { get; init; }
    public required string Onayla { get; init; }
    public required string KayitYok { get; init; }
    public required string KayitYokAciklama { get; init; }
    public required string Gecikmis { get; init; }
    public required string Yakinda { get; init; }
    public required string Gun { get; init; }
    public required string Adet { get; init; }
    public required string Tumu { get; init; }
    public required string PortfoydeToplam { get; init; }
    public required string TahsileVerilenToplam { get; init; }
    public required string BuHaftaVadeli { get; init; }
    public required string KarsiliksizToplam { get; init; }
    public required string DonemdenBagimsiz { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string Kaydedildi { get; init; }
    public required string Guncellendi { get; init; }
    public required string DurumPortfoyde { get; init; }
    public required string DurumTahsileVerildi { get; init; }
    public required string DurumTahsilEdildi { get; init; }
    public required string DurumKarsiliksiz { get; init; }
    public required string DurumCiroEdildi { get; init; }
    public required string DegismezGerekce { get; init; }
    public required string DegismezYol { get; init; }
    public required string TakvimeGit { get; init; }
    public required string ListeyeGit { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): aralık boşken "portföyde evrak yok" deniyordu.</summary>
    public required string AralikBosBaslik { get; init; }
    public required string AralikBosAciklama { get; init; }
    public required string PortfoyToplami { get; init; }
    public required string TumPortfoyuGoster { get; init; }
    public required string SuzgecUygulandi { get; init; }
    public required string GenisletmeSiniri { get; init; }
    public required string SuzgeciTemizle { get; init; }
    public required string KasaSecilmedi { get; init; }
    public required string BankaSecilmedi { get; init; }
    public required string SonDurum { get; init; }
    public required string BakiyeSonrasi { get; init; }
    public required string Eylemler { get; init; }
    public required string ZorunluAlan { get; init; }
    public required string KurYok { get; init; }
}

public sealed class SozlukSiparis
{
    /// <summary>PAKET P-06 (B-4) — detay bağı aynı zamanda DÜZENLEME yoludur.</summary>
    public required string DetayVeDuzenleme { get; init; }
    public required string Baslik { get; init; }
    /// <summary>PAKET P-37c — sipariş listesi ekranının araç şeridi açıklaması.</summary>
    public required string EkranAciklama { get; init; }
    /// <summary>PAKET P-37c — sipariş DETAY ekranının araç şeridi başlığı.</summary>
    public required string DetayEkranBaslik { get; init; }
    /// <summary>PAKET P-37c — sipariş DETAY ekranının araç şeridi açıklaması.</summary>
    public required string DetayEkranAciklama { get; init; }
    public required string Yeni { get; init; }
    public required string No { get; init; }
    public required string Tur { get; init; }
    public required string Durum { get; init; }
    public required string Cari { get; init; }
    public required string Sube { get; init; }
    public required string SozVerilenGun { get; init; }
    public required string Aciklama { get; init; }
    public required string TahminiTutar { get; init; }
    public required string Kalemler { get; init; }
    public required string KalemEkle { get; init; }
    public required string KalemSil { get; init; }
    public required string Urun { get; init; }
    public required string Tanim { get; init; }
    public required string Ayar { get; init; }
    public required string Gram { get; init; }
    public required string Adet { get; init; }
    public required string KalemSayisi { get; init; }

    /// <summary>PAKET S-3a · UX-S21 — ürün bağının kaynağını söyleyen rozet metni.</summary>
    public required string UrundenGeldi { get; init; }
    public required string Olustur { get; init; }
    public required string Duzenle { get; init; }
    public required string DuzenlemeyiBitir { get; init; }
    public required string Kaydet { get; init; }
    public required string Detay { get; init; }
    public required string GecisGecmisi { get; init; }
    public required string AppendOnly { get; init; }
    public required string Gerekce { get; init; }
    public required string GecisYap { get; init; }
    public required string IptalBaslik { get; init; }
    public required string IptalOnay { get; init; }
    public required string BosDurum { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string Geciken { get; init; }
    public required string GecikenSuzgeci { get; init; }
    public required string TumTurler { get; init; }
    public required string TumDurumlar { get; init; }
    public required string DegismezGerekce { get; init; }
    public required string DegismezAlternatif { get; init; }
    public required string EmanetUyarisi { get; init; }
    public required string EmanetHareketId { get; init; }
    public required string TeklifAl { get; init; }
    public required string TeklifKaynak { get; init; }
    public required string KurYok { get; init; }
    public required string CariGerekli { get; init; }
    public required string KalemGerekli { get; init; }
    public required string GerekceKisa { get; init; }
    public required string IzinliGecisYok { get; init; }
    public required string Olusturuldu { get; init; }
    public required string GecisYapildi { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): durum geçişi onaysız ve geri dönüşsüzdü.</summary>
    public required string GecisOnayBaslik { get; init; }
    public required string GecisOnayMetni { get; init; }
    public required string GeriGecisGerekce { get; init; }

    /// <summary>PAKET P-22 / K-23 — geri dönüş düğmesinin etiketi ({0} = hedef durum).</summary>
    public required string GeriAl { get; init; }
    public required string TaslagaDonNotu { get; init; }
    public required string Guncellendi { get; init; }
    public required string SubeOturumdan { get; init; }
    public required string DefterEtkisiYok { get; init; }
    public required string TurMusteri { get; init; }
    public required string TurAtolye { get; init; }
    public required string TurTamir { get; init; }
    public required string DurumTaslak { get; init; }
    public required string DurumOnaylandi { get; init; }
    public required string DurumAtolyede { get; init; }
    public required string DurumHazir { get; init; }
    public required string DurumTeslimEdildi { get; init; }
    public required string DurumIptal { get; init; }
}

/// <summary>Paket 19 Faz A — vezne modeli (tezgâhtar satar, vezne tahsil eder).</summary>
public sealed class SozlukVezne
{
    public required string Baslik { get; init; }
    // ── PAKET P-23c — fiş türü (vezne fişinin genelleştirilmesi) ──────────
    public required string FisTuru { get; init; }
    public required string TurSatis { get; init; }
    public required string TurTahsilat { get; init; }
    public required string TurIscilik { get; init; }
    public required string TurAciklama { get; init; }
    public required string TurDegismez { get; init; }
    public required string TakasEmanetYok { get; init; }
    public required string TutarGerekli { get; init; }
    public required string CariGerekli { get; init; }
    public required string TutarKalemden { get; init; }
    public required string TekBacak { get; init; }
    /// <summary>PAKET P-37c — ortak araç şeridi (SayfaBasi) açıklaması.</summary>
    public required string EkranAciklama { get; init; }
    public required string Kuyruk { get; init; }
    public required string FisiAc { get; init; }
    public required string Yeni { get; init; }
    public required string VezneyeGonder { get; init; }
    public required string TahsilEt { get; init; }
    public required string Iptal { get; init; }
    public required string IptalOnay { get; init; }
    public required string Gerekce { get; init; }
    public required string GerekceKisa { get; init; }
    public required string Hazirlayan { get; init; }
    public required string TahsilEden { get; init; }
    public required string BosDurum { get; init; }
    public required string KendiFisin { get; init; }
    public required string AyrimZorunlu { get; init; }
    public required string AyrimAciklama { get; init; }
    public required string AyrimAcik { get; init; }
    public required string AyrimKapali { get; init; }
    public required string Durum { get; init; }
    public required string DurumAcik { get; init; }
    public required string DurumVeznede { get; init; }
    public required string DurumTahsil { get; init; }
    public required string DurumIptal { get; init; }
    public required string TumDurumlar { get; init; }
    public required string Cari { get; init; }
    public required string Sube { get; init; }
    public required string SubeOturumdan { get; init; }
    public required string Aciklama { get; init; }
    public required string Kalemler { get; init; }
    public required string KalemEkle { get; init; }
    public required string KalemSil { get; init; }
    public required string Urun { get; init; }
    public required string Adet { get; init; }
    public required string BirimFiyat { get; init; }
    public required string Iscilik { get; init; }
    public required string KalemSayisi { get; init; }
    public required string Toplam { get; init; }
    public required string DamgaliToplam { get; init; }
    /// <summary>P-46: ACIK fişin tutarı henüz damgalanmadı (kalemlerden hesaplandı).</summary>
    public required string ToplamTahmini { get; init; }
    /// <summary>P-46: vezneye gönderilmiş fişin tutarı damgalıdır, yeniden hesaplanmaz.</summary>
    public required string ToplamDamgali { get; init; }
    public required string Bacaklar { get; init; }
    public required string BacakEkle { get; init; }
    public required string BacakSil { get; init; }
    public required string Arac { get; init; }
    public required string Tutar { get; init; }
    public required string Kasa { get; init; }
    public required string KasaOtomatik { get; init; }
    public required string Kalan { get; init; }
    public required string KalaniAta { get; init; }
    public required string Duzenle { get; init; }
    public required string Sec { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string KurYok { get; init; }
    public required string KalemGerekli { get; init; }
    public required string BacakGerekli { get; init; }
    public required string ToplamTutmuyor { get; init; }
    public required string Olusturuldu { get; init; }
    public required string Veznelendi { get; init; }
    public required string TahsilEdildi { get; init; }
    public required string IptalEdildi { get; init; }
    public required string DefterFisi { get; init; }
    public required string MutabakatGerekli { get; init; }
    public required string DegismezGerekce { get; init; }
    public required string DegismezAlternatif { get; init; }
    /// <summary>UX-1-D — VEZNEDE fişi: neden düzenlenemez.</summary>
    public required string VezneleDegismez { get; init; }
    /// <summary>UX-1-D — VEZNEDE fişi: doğru düzeltme yolu.</summary>
    public required string VezneleDegismezYolu { get; init; }
    public required string DefterKapilari { get; init; }
    public required string KurTahsilAninda { get; init; }
    public required string Guncellendi { get; init; }
    public required string AracNakit { get; init; }
    public required string AracKart { get; init; }
    public required string AracHavale { get; init; }
    public required string AracVeresiye { get; init; }
}

public sealed class SozlukNav
{
    /// <summary>Paket 31 — POS cihazları ve POS alacak defteri menü girişleri.</summary>
    public required string Pos { get; init; }
    public required string PosTahsilat { get; init; }

    /// <summary>Paket 34 — rapor seti menü girişleri (yalnız üç yeni bağlantı).</summary>
    public required string IslemDefteri { get; init; }
    public required string SatisAnalizi { get; init; }
    public required string KasaRaporu { get; init; }
    public required string Panel { get; init; }
    public required string Cariler { get; init; }
    public required string Urunler { get; init; }
    public required string Islemler { get; init; }
    public required string Kasalar { get; init; }
    public required string Banka { get; init; }
    public required string Faturalar { get; init; }
    public required string Raporlar { get; init; }
    public required string Kullanicilar { get; init; }
    public required string Yetkiler { get; init; }
    public required string DenetimIzi { get; init; }
    public required string VeriDisaAktarim { get; init; }
    /// <summary>Paket 47 Faz A — veri içe aktarımı.</summary>
    public required string VeriIceAktarim { get; init; }
    /// <summary>Paket P-09 (Y-39) — içe+dışa aktarımı kapsayan YETKİ MODÜLÜ adı.</summary>
    public required string VeriAktarimi { get; init; }
    /// <summary>Paket T-2 Faz A — kuruluş sihirbazı.</summary>
    public required string Kurulum { get; init; }
    public required string Etiketler { get; init; }
    public required string UrunAgaci { get; init; }
    public required string Emanet { get; init; }
    public required string Isletme { get; init; }
    public required string Kur { get; init; }
    public required string KarZarar { get; init; }
    public required string VadeTakibi { get; init; }
    public required string Fiyatlandirma { get; init; }
    public required string Sayim { get; init; }
    public required string Uyum { get; init; }
    public required string GiderPusulalari { get; init; }
    /// <summary>PAKET P-19 — gider + virman ekranı.</summary>
    public required string Gider { get; init; }
    /// <summary>PAKET P-20 (Y-10) — hurda stok / ramat çıkışı ekranı.</summary>
    public required string Hurda { get; init; }
    public required string Kilavuz { get; init; }
    public required string Ayarlar { get; init; }
    /// <summary>Paket 6 Faz A — bildirim ekranı.</summary>
    public required string Bildirimler { get; init; }
    /// <summary>Paket 3 Faz A — sipariş ekranı.</summary>
    public required string Siparisler { get; init; }
    /// <summary>Paket 19 Faz A — vezne ekranı.</summary>
    public required string Vezne { get; init; }
    // UX-2 Faz A1 — rota başlığı haritası 27 rotanın TAMAMINI ister.
    public required string PusulaBelgesi { get; init; }
    public required string Giris { get; init; }
    public required string Bulunamadi { get; init; }
    /// <summary>Paket T-3 Faz A — yedekler ekranı.</summary>
    public required string Yedekler { get; init; }
    /// <summary>Paket 29 Faz A — çek/senet portföyü.</summary>
    public required string Evraklar { get; init; }
    /// <summary>Paket 29 Faz A — vade takvimi.</summary>
    public required string EvrakTakvimi { get; init; }

    // ── Paket P-14 (Y-9) — kenar çubuğu grup başlıkları ─────────────────────
    /// <summary>Tezgâhta her gün açılan ekranlar.</summary>
    public required string GrupGunluk { get; init; }
    /// <summary>Müşteri kartı ve stok yönetimi.</summary>
    public required string GrupMusteriStok { get; init; }
    /// <summary>Para giriş/çıkışı, banka, POS, evrak.</summary>
    public required string GrupParaBanka { get; init; }
    /// <summary>Resmî belge ve uyum ekranları.</summary>
    public required string GrupBelgeUyum { get; init; }
    /// <summary>Okuma amaçlı dökümler.</summary>
    public required string GrupRapor { get; init; }
    /// <summary>İşletme kurulumu, kullanıcı, veri.</summary>
    public required string GrupYonetim { get; init; }
    /// <summary>Kenar çubuğu gezinme bölgesinin erişilebilir adı (B-63).</summary>
    public required string MenuEtiketi { get; init; }
}

public sealed class SozlukAuth
{
    public required string UygulamaAdi { get; init; }
    public required string Slogan { get; init; }
    public required string GirisYap { get; init; }
    public required string IsletmeKur { get; init; }
    public required string Eposta { get; init; }
    public required string Sifre { get; init; }
    public required string SifreTekrar { get; init; }
    public required string IsletmeAdi { get; init; }
    public required string Ad { get; init; }
    public required string Soyad { get; init; }
    public required string AdSoyad { get; init; }
    public required string Rol { get; init; }
    public required string GirisBasarisiz { get; init; }
    public required string KayitBasarisiz { get; init; }
    public required string SifrePlaceholder { get; init; }

    // ── paket F-2-min — kayıt davet jetonu ──
    public required string DavetJetonu { get; init; }
    public required string DavetAciklama { get; init; }
    public required string DavetPlaceholder { get; init; }
    public required string Hosgeldin { get; init; }
    public required string PanelBaslik { get; init; }
    public required string PanelAlt { get; init; }
    public required string Ozellik1 { get; init; }
    public required string Ozellik2 { get; init; }
    public required string Ozellik3 { get; init; }

    // ── PAKET P-30 (R-4 · BK-47) — giriş ekranındaki kısıt beyanı ──
    // Metinler TR+EN birlikte doldurulur (required = derleme kapısı); kısıt
    // SATIRLARI burada DEĞİL, Kuyumcu.Contracts.Kisit.KisitBeyani'nde yaşar.
    public required string KisitBaslik { get; init; }
    public required string KisitYukleniyor { get; init; }
    public required string KisitBos { get; init; }
    public required string KisitKaynak { get; init; }
    public required string KisitHatasi { get; init; }
    public required string KisitDurumHaric { get; init; }
    public required string KisitDurumKapsamDisi { get; init; }
    public required string KisitDurumSurum11 { get; init; }

    // ── Paket 45: iki aşamalı giriş (TOTP). Ekran bacağı 45-UI'de bu anahtarları
    // kullanır; TR ve EN blokları BİRLİKTE doldurulur (required = derleme kapısı).
    public required string MfaBaslik { get; init; }
    public required string MfaAciklama { get; init; }
    public required string MfaKur { get; init; }
    public required string MfaKapat { get; init; }
    public required string MfaKod { get; init; }
    public required string MfaKodPlaceholder { get; init; }
    public required string MfaKurtarmaKodu { get; init; }
    public required string MfaKurtarmaUyari { get; init; }
    public required string MfaKurtarmaYenile { get; init; }
    public required string MfaEtkin { get; init; }
    public required string MfaPasif { get; init; }
    public required string MfaGizElle { get; init; }
    public required string MfaKilitli { get; init; }
    public required string MfaKodHatali { get; init; }
    public required string MfaKalanKod { get; init; }
    public required string MfaSaatUyarisi { get; init; }
}

public sealed class SozlukPanel
{
    // ── PAKET P-37 — KARŞILAMA EKRANI (kurucu talimatı 20): selam · hava ·
    //    kutucuk düzeni. Hava DIŞ KAYNAKTIR ve varsayılan KAPALIDIR.
    public required string SelamGunaydin { get; init; }
    public required string SelamIyiGunler { get; init; }
    public required string SelamIyiAksamlar { get; init; }
    public required string SelamIyiGeceler { get; init; }
    public required string HavaBaslik { get; init; }
    public required string HavaAlinamadi { get; init; }
    public required string HavaKapali { get; init; }
    public required string DuzeniDuzenle { get; init; }
    public required string DuzenlemeyiBitir { get; init; }
    public required string DuzeniSifirla { get; init; }
    public required string KutucukGizle { get; init; }
    public required string KutucukGoster { get; init; }
    public required string KutucukYukari { get; init; }
    public required string KutucukAsagi { get; init; }
    public required string GizliKutucuklar { get; init; }
    public required string Tahsilat { get; init; }
    public required string BekleyenYok { get; init; }
    public required string BekleyenRolKapali { get; init; }
    public required string SonIslemYok { get; init; }
    public required string KasaKaydiYok { get; init; }
    public required string HizliEylemler { get; init; }
    public required string BekleyenIsler { get; init; }
    public required string SonIslemler { get; init; }
    public required string DuzenKaydedilemedi { get; init; }
    public required string GunlukSatis { get; init; }
    public required string GunlukAlis { get; init; }
    public required string NetHasGram { get; init; }
    public required string NetKasa { get; init; }
    public required string NetKasaFormul { get; init; }
    public required string ParalelDefter { get; init; }
    public required string MasakSuresiGecti { get; init; }
    public required string MasakSuresiYaklasiyor { get; init; }
    public required string PusulaSuresiGecti { get; init; }
    /// <summary>Paket 40-A — panel uyum kartı satırı.</summary>
    public required string YuksekRiskliCari { get; init; }
    public required string UyumaGit { get; init; }
    public required string PusulalaraGit { get; init; }
    public required string Son7Gun { get; init; }
    public required string BugunOzet { get; init; }
    public required string Satis { get; init; }
    public required string Alis { get; init; }
    public required string Iscilik { get; init; }
    public required string HasGram { get; init; }
    public required string Kur24k { get; init; }
    public required string Canli { get; init; }
    public required string KurTakip { get; init; }
    public required string Altin24k { get; init; }
    public required string Gram { get; init; }
    public required string KurAlis { get; init; }
    public required string KurSatis { get; init; }
    public required string HizliErisim { get; init; }
    public required string SatisYap { get; init; }
    public required string AlisYap { get; init; }
    public required string MusteriBul { get; init; }
    public required string KasaOzeti { get; init; }
    public required string BugunOzeti { get; init; }
    // ── UX-1-C1 (UX-S33 · basit mod) ────────────────────────────────────────
    public required string Aciklama { get; init; }
    public required string BugunItibariyla { get; init; }
    public required string BasitModNotu { get; init; }
}

public sealed class SozlukCari
{
    public required string VergiNo { get; init; }
    public required string CariDuzenle { get; init; }
    public required string PasifeAl { get; init; }
    // UX-4d madde 13
    public required string PasifeAlOnay { get; init; }

    // ── PAKET 40-A — PEP beyanı (tespit DEĞİL, müşterinin beyanı) ────────────
    public required string PepBeyani { get; init; }
    public required string PepGorev { get; init; }
    public required string PepGorevZorunlu { get; init; }
    public required string PepBeyanTarihi { get; init; }
    public required string PepNotu { get; init; }
    public required string PepRozet { get; init; }
    public required string RiskSeviyesi { get; init; }
    public required string Aktiflestir { get; init; }
    public required string Tahsilat { get; init; }
    public required string TahsilatIpucu { get; init; }
    public required string Duzeltme { get; init; }
    public required string DuzeltmeIpucu { get; init; }
    public required string TlDelta { get; init; }
    public required string HasDelta { get; init; }
    public required string Gerekce { get; init; }
    public required string Veresiye { get; init; }
    public required string VeresiyeIpucu { get; init; }
    public required string TlTutar { get; init; }
    public required string OdemeTuru { get; init; }
    public required string Nakit { get; init; }
    public required string Kart { get; init; }
    public required string Havale { get; init; }
    public required string HasGramOdeme { get; init; }
    public required string HasGramIpucu { get; init; }
    public required string HasGramVerilen { get; init; }
    public required string Atolye { get; init; }
    public required string Sarraf { get; init; }

    // ── Paket 27 — döviz defteri (TR+EN BİRLİKTE; "required" bunu zorlar) ──
    public required string DovizBakiye { get; init; }
    public required string DovizVeresiye { get; init; }
    public required string DovizTahsilat { get; init; }
    public required string ParaBirimi { get; init; }
    public required string EfektifMiktar { get; init; }
    public required string DovizBorcuVar { get; init; }
    public required string TlKarsiligiBilgiAmacli { get; init; }
    public required string KurYokDovizYapilamaz { get; init; }
    public required string DovizVadesizUyari { get; init; }
    public required string DovizFisBacagiUyari { get; init; }
    public required string DovizDelta { get; init; }

    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string YeniCari { get; init; }
    public required string AraPlaceholder { get; init; }
    public required string AdFirma { get; init; }
    public required string Tur { get; init; }
    public required string Musteri { get; init; }
    public required string Tedarikci { get; init; }
    public required string HerIkisi { get; init; }
    public required string Telefon { get; init; }
    public required string Adres { get; init; }
    public required string TlBakiye { get; init; }
    public required string HasGram { get; init; }
    public required string Hareketler { get; init; }
    public required string SonHareketler { get; init; }
    public required string HareketYok { get; init; }
    public required string CariBulunamadi { get; init; }
    public required string Olusturulamadi { get; init; }
    public required string CariDetay { get; init; }
    public required string CariSecin { get; init; }
    public required string CariAra { get; init; }
    public required string MusteriAdiYazin { get; init; }
    public required string Alacak { get; init; }
    public required string Borc { get; init; }
    public required string KrediLimiti { get; init; }
    // PAKET P-10 (Y-3): CariOlusturDto.Notlar + CariGuncelleDto.Notlar sunucuda
    // başından beri vardı; hiçbir ekranda girişi YOKTU.
    public required string Notlar { get; init; }
    public required string NotlarIpucu { get; init; }
    public required string PasifeAlBagli { get; init; }
    public required string PasifeAlBagliYukleniyor { get; init; }

    // ── PAKET P-16 (Y-27) — CARİ KARTI KİMLİK / KYC ALANLARI ────────────────
    public required string Unvan { get; init; }
    public required string UnvanIpucu { get; init; }
    public required string KimlikTuru { get; init; }
    public required string KimlikTuruBelirtilmedi { get; init; }
    public required string KimlikTckn { get; init; }
    public required string KimlikVkn { get; init; }
    public required string KimlikNoHaneIpucu { get; init; }
    public required string DogumTarihi { get; init; }
    public required string Uyruk { get; init; }
    public required string UyrukIpucu { get; init; }
    public required string IskontoYuzde { get; init; }
    public required string IskontoYuzdeIpucu { get; init; }
    public required string Segment { get; init; }
    // ── PAKET P-26 (Y-37) — muhasebe hesap kodu
    public required string HesapKodu { get; init; }
    public required string HesapKoduIpucu { get; init; }
    public required string SegmentSecilmedi { get; init; }
    public required string SegmentBireysel { get; init; }
    public required string SegmentKurumsal { get; init; }
    public required string SegmentToptan { get; init; }
    public required string SegmentVip { get; init; }
    public required string KimlikBolumu { get; init; }
}

public sealed class SozlukIsletme
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string IsletmeBilgisi { get; init; }
    public required string IsletmeAdi { get; init; }
    public required string Plan { get; init; }
    public required string PlanBaslangic { get; init; }
    public required string PlanPro { get; init; }
    public required string PlanKurumsal { get; init; }
    public required string PlanDegistir { get; init; }
    public required string AiModulu { get; init; }
    public required string AiAciklama { get; init; }
    public required string AiAcik { get; init; }
    public required string AiKapali { get; init; }
    public required string Subeler { get; init; }
    public required string YeniSube { get; init; }
    public required string SubeAdi { get; init; }
    public required string Adres { get; init; }
    public required string Telefon { get; init; }
    public required string SubeDetay { get; init; }
    public required string SubeYok { get; init; }
    public required string PasifeAl { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string SonSubeUyarisi { get; init; }
    public required string YalnizSahip { get; init; }

    // ── paket F-2-min — deneme / salt-okunur bandı ──
    public required string DurumBasligi { get; init; }
    public required string DurumDeneme { get; init; }
    public required string DurumAktif { get; init; }
    public required string DurumSaltOkunur { get; init; }
    public required string KalanGun { get; init; }
    public required string DenemeBitti { get; init; }
    public required string SaltOkunurUyari { get; init; }
    public required string VerileriDisaAktar { get; init; }
    public required string SureUzatmaNotu { get; init; }
    public required string YazmaKapaliKilidi { get; init; }

    // ── PAKET P-27 (F-1 abonelik + F-3 plan kapısı) ──
    public required string AbonelikBasligi { get; init; }
    public required string AbonelikAciklama { get; init; }
    public required string AbonelikDonemi { get; init; }
    public required string AbonelikBaslangic { get; init; }
    public required string AbonelikBitis { get; init; }
    public required string AbonelikSuresiz { get; init; }
    public required string AbonelikKalanGun { get; init; }
    public required string AbonelikSuresiDoldu { get; init; }
    public required string AbonelikYok { get; init; }
    public required string PlanKaynakRozeti { get; init; }
    public required string PlanYukseltmeKapali { get; init; }
    public required string PlanIndirmeYok { get; init; }
    public required string PlanIndirOnay { get; init; }
    public required string AbonelikGecmisi { get; init; }
    public required string AbonelikGecmisYok { get; init; }
    public required string AbonelikSinirNotu { get; init; }
    public required string AbonelikKaynakSistem { get; init; }
    public required string AbonelikKaynakKiraci { get; init; }
    public required string DonemdenBagimsiz { get; init; }
}

public sealed class SozlukEmanet
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Musteri { get; init; }
    public required string MusteriSecin { get; init; }
    public required string Giris { get; init; }
    public required string Cikis { get; init; }
    public required string Ayar { get; init; }
    public required string GramAgirlik { get; init; }
    public required string TasAgirlik { get; init; }
    public required string HasKarsiligi { get; init; }
    public required string Aciklamasi { get; init; }
    public required string ElimizdekiEmanet { get; init; }
    public required string IadeEdilecekHas { get; init; }
    public required string IadeFazlaUyari { get; init; }
    public required string Hareketler { get; init; }
    public required string HareketYok { get; init; }
    public required string Tur { get; init; }
    public required string Tarih { get; init; }
    public required string GirisTuru { get; init; }
    public required string CikisTuru { get; init; }
    public required string KurYok { get; init; }
    public required string SubeUyarisi { get; init; }
    public required string EmanetMulkiyetUyarisi { get; init; }
    // UX-4c madde 10
    public required string MusteriAra { get; init; }
    public required string AramaDaraltin { get; init; }
    public required string HesaplananHas { get; init; }
    // ── paket UX-1-C2 ──
    public required string TasGramdanKucuk { get; init; }
    public required string GirisKaydedildi { get; init; }
    public required string CikisKaydedildi { get; init; }
    public required string HareketDegismez { get; init; }
    public required string HareketDegismezYolu { get; init; }
    // ── PAKET P-12 (B-43) — emanet hareketi açıklaması düzeltilebilir ──
    public required string AciklamaGuncellendi { get; init; }
    public required string AciklamaKilitliAlanlar { get; init; }
}

/// <summary>
/// Paket 25 — reyon/vitrin bazlı stok bölümlemesi. UX-S31: ham veritabanı kodu
/// (VITRIN, KASA…) ekrana basılmaz, buradan çevrilir; eksik anahtar DERLEME
/// HATASI verir (required).
/// </summary>
public sealed class SozlukYerlesim
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Atanmamis { get; init; }
    public required string AtanmamisIpucu { get; init; }
    public required string YerlesikAdet { get; init; }
    public required string StokAdedi { get; init; }
    public required string Yerlestir { get; init; }
    public required string Tasi { get; init; }
    public required string GeriAl { get; init; }
    public required string KaynakReyon { get; init; }
    public required string HedefReyon { get; init; }
    public required string Reyon { get; init; }
    public required string ReyonIcerigi { get; init; }
    public required string ReyonSecimi { get; init; }
    public required string ReyonSecimiIpucu { get; init; }
    public required string Adet { get; init; }
    public required string YerlesimYok { get; init; }
    public required string ReyonBos { get; init; }
    public required string YetersizYerlesim { get; init; }
    public required string ReyonBosaltilmali { get; init; }
    public required string HedefSecilmeli { get; init; }
    public required string AdetGecersiz { get; init; }
    public required string AdetAtanmamisiAsiyor { get; init; }
    public required string ReyonStokRaporu { get; init; }
    public required string MadenAyriToplanir { get; init; }
    public required string Sinirda { get; init; }
    public required string Sorumlu { get; init; }
    public required string UrunKodu { get; init; }
    public required string UrunAdi { get; init; }
    public required string UrunSecilmeli { get; init; }
}

public sealed class SozlukUrunAgaci
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Kategoriler { get; init; }
    public required string Reyonlar { get; init; }
    /// <summary>P-36c/UX-S30: düzenlenen reyonun şubesi değişmez — sebebi.</summary>
    public required string ReyonSubeDegismez { get; init; }
    public required string YeniKategori { get; init; }
    public required string YeniReyon { get; init; }
    public required string KategoriAdi { get; init; }
    public required string UstKategori { get; init; }
    public required string KokSeviye { get; init; }
    public required string Sira { get; init; }
    public required string ReyonAdi { get; init; }
    public required string SubeDegismez { get; init; }
    public required string ReyonPasifOnay { get; init; }
    public required string ReyonTipi { get; init; }
    public required string TipVitrin { get; init; }
    public required string TipKasa { get; init; }
    public required string TipDepo { get; init; }
    public required string TipCanta { get; init; }
    public required string Sube { get; init; }
    public required string UrunSayisi { get; init; }
    public required string KategoriYok { get; init; }
    public required string ReyonYok { get; init; }
    public required string DonguUyarisi { get; init; }
    public required string KapsamUyarisi { get; init; }
    public required string KategoriPasifeAl { get; init; }
    public required string KategoriPasifOnay { get; init; }
    public required string KategoriBagliKayit { get; init; }
    public required string KategoriBagliYok { get; init; }
    public required string KategoriPasifRozet { get; init; }
}

public sealed class SozlukUrun
{
    // ── UX-1-C1 ────────────────────────────────────────────────────────────
    public required string Aciklama { get; init; }
    /// <summary>UX-S31: aynı kavram tek ad — enum alanı her ekranda "Ürün grubu".</summary>
    public required string UrunGrubu { get; init; }
    public required string StokGirisiYolu { get; init; }
    // UX-4d madde 1
    public required string AcilisStogu { get; init; }
    public required string AcilisStoguIpucu { get; init; }
    /// <summary>URUN-FORM-2: alana bağlı sunucu reddinde pencere altındaki bant için kısa genel cümle.</summary>
    public required string FormAlanHatasiGenel { get; init; }
    // ── PAKET P-10 (Y-3, Y-8) — sunucunun kabul ettiği alanlar + görsel ──
    public required string Rfid { get; init; }
    public required string RfidIpucu { get; init; }
    public required string KmtsKodu { get; init; }
    public required string KmtsKoduIpucu { get; init; }
    public required string GorselUrl { get; init; }
    public required string GorselUrlIpucu { get; init; }
    public required string PariteKodu { get; init; }
    public required string PariteKoduIpucu { get; init; }
    public required string Gorsel { get; init; }
    public required string GorselYok { get; init; }
    public required string Galeri { get; init; }
    public required string GaleriBos { get; init; }
    public required string GaleriIpucu { get; init; }
    public required string GorselUrlGecersiz { get; init; }
    public required string Karat { get; init; }
    public required string Renk { get; init; }
    public required string Berraklik { get; init; }
    public required string Kesim { get; init; }
    public required string SertifikaKurumu { get; init; }
    public required string SertifikaNo { get; init; }
    public required string Sertifikalar { get; init; }
    public required string SertifikaYukle { get; init; }
    public required string PdfOlmali { get; init; }
    public required string DortC { get; init; }
    public required string Birim { get; init; }
    public required string BirimAdet { get; init; }
    public required string BirimGram { get; init; }
    public required string BirimGramIpucu { get; init; }
    public required string Baslik { get; init; }
    public required string YeniUrun { get; init; }
    public required string AraPlaceholder { get; init; }
    public required string UrunAdi { get; init; }
    public required string Barkod { get; init; }
    public required string Kategori { get; init; }
    public required string Ayar { get; init; }
    public required string TumAyarlar { get; init; }
    // ── Paket 20 — maden / saflık ────────────────────────────────────────────
    public required string Maden { get; init; }
    public required string Madenler { get; init; }
    public required string TumMadenler { get; init; }
    public required string Saflik { get; init; }
    public required string SaflikTablosu { get; init; }
    public required string RodyumKaplama { get; init; }
    /// <summary>P-36c/3: rodyum kutusu kilitliyken GÖRÜNÜR sebep (UX-S30).</summary>
    public required string RodyumKaplanmaz { get; init; }
    /// <summary>P-36c/3: ürün kodu alanının etiketi (önce çıplak "Kod" yazılıydı).</summary>
    public required string Kod { get; init; }
    public required string RodyumKaplamaIpucu { get; init; }
    public required string HasHesaplanmaz { get; init; }
    public required string HasHesaplanmazIpucu { get; init; }
    public required string KotasyonYok { get; init; }
    public required string KotasyonYokIpucu { get; init; }
    public required string MadenBakiyeleri { get; init; }
    public required string MadenDagilimi { get; init; }
    public required string MadenUyarisi { get; init; }
    public required string GramAgirlik { get; init; }
    public required string TasAgirlik { get; init; }
    // Kuyum Ticareti Yönetmeliği md.11/(e) — RG 24.06.2026/33290
    public required string TasKokeni { get; init; }
    public required string TasKokeniIpucu { get; init; }
    public required string TasIbaresi { get; init; }
    public required string TasIbaresiIpucu { get; init; }
    public required string TasKokenDogal { get; init; }
    public required string TasKokenSentetik { get; init; }
    public required string TasKokenLaboratuvar { get; init; }
    public required string TasKokenYapay { get; init; }
    public required string TasKokenYok { get; init; }
    public required string GercekMilyem { get; init; }
    public required string GercekMilyemIpucu { get; init; }
    /// <summary>M1 — satış milyemi alanı etiketi ve ipucu.</summary>
    public required string SatisMilyemi { get; init; }
    public required string SatisMilyemiIpucu { get; init; }
    /// <summary>M1 — fiyat ofseti (± TL) alanı etiketi ve ipucu.</summary>
    public required string FiyatOfset { get; init; }
    public required string FiyatOfsetIpucu { get; init; }
    /// <summary>M1b — alış (maliyet) milyemi; M2b — KDV'ye giren pay (binde).</summary>
    public required string AlisMilyemi { get; init; }
    public required string AlisMilyemiIpucu { get; init; }
    public required string KdvMatrahBinde { get; init; }
    public required string KdvMatrahBindeIpucu { get; init; }
    public required string SatisFiyati { get; init; }
    public required string SatisFiyatiIpucu { get; init; }
    public required string Milyem { get; init; }
    public required string Gram { get; init; }
    public required string HasGram { get; init; }
    public required string Durum { get; init; }
    public required string Stok { get; init; }
    public required string SubeStoklari { get; init; }
    public required string Sube { get; init; }
    public required string ToplamHas { get; init; }
    public required string StokYok { get; init; }
    public required string UrunBulunamadi { get; init; }
    public required string Olusturulamadi { get; init; }
    public required string UrunSecin { get; init; }
    public required string UrunAdiYazin { get; init; }
    public required string UrunEkle { get; init; }
    // ── Ürün ekranı boşlukları (2026-09-14): API'de olup ekranda olmayan işlevler
    public required string UrunDuzenle { get; init; }
    public required string PasifeAl { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string BarkodAra { get; init; }
    public required string BarkodAraIpucu { get; init; }
    /// <summary>P-37 — TEK arama yüzeyi: ad/kod/barkod aynı kutuda.</summary>
    public required string AraVeyaBarkod { get; init; }
    public required string BarkodBulunamadi { get; init; }
    public required string BarkodFiyatGirilmeli { get; init; }
    public required string Etiket { get; init; }
    public required string EtiketMetni { get; init; }
    public required string IbareZorunlu { get; init; }
    public required string Dayanak { get; init; }
    public required string SayimDuzelt { get; init; }
    public required string SayilanAdet { get; init; }
    public required string SayimIpucu { get; init; }
    public required string Fark { get; init; }
    public required string Reyon { get; init; }
    public required string TumReyonlar { get; init; }
    public required string TumKategoriler { get; init; }
    public required string Konsinye { get; init; }
    public required string KonsinyeSahibi { get; init; }
    public required string KonsinyeBirimBedel { get; init; }
    public required string KonsinyeIpucu { get; init; }

    // ── PAKET P-16 (Y-26) — ÜRÜN KARTININ EKSİK ALANLARI ───────────────────
    public required string KartBolumu { get; init; }
    public required string Tedarikci { get; init; }
    public required string TedarikciIpucu { get; init; }
    public required string AlisMaliyeti { get; init; }
    public required string AlisMaliyetiIpucu { get; init; }
    public required string AlisMaliyetiYok { get; init; }
    public required string HedefMarj { get; init; }
    public required string HedefMarjIpucu { get; init; }
    public required string KdvOrani { get; init; }
    public required string KdvOraniIpucu { get; init; }
    public required string SeriNo { get; init; }
    public required string Olcu { get; init; }
    public required string OlcuIpucu { get; init; }
    public required string MinStok { get; init; }
    public required string MinStokIpucu { get; init; }
}

public sealed class SozlukIslem
{
    /// <summary>PAKET DIL-EKRAN — alt sözlüğün dili; BASİT katman kopyaladığında kimlik karşılaştırması bozulur.</summary>
    public string DilKodu { get; init; } = "tr";
    // ── UX-1-C1 (POS düzenlenebilirliği · QA turu 1+2 KRİTİK) ──────────────
    public required string Aciklama { get; init; }
    public required string SonSatislar { get; init; }
    public required string SonSatisYok { get; init; }
    // ── P-23 FAZ 2 — ödeme kırılımı (R-17) ──────────────────────────────────
    public required string OdemeKirilimi { get; init; }
    public required string OdemeKirilimiGoster { get; init; }
    public required string OdemeKirilimiGizle { get; init; }
    public required string KirilimKayitli { get; init; }
    public required string KirilimBilinmiyor { get; init; }
    public required string KirilimKismen { get; init; }
    public required string KirilimGeriDolduruldu { get; init; }
    public required string KirilimYok { get; init; }
    public required string IadeEt { get; init; }
    public required string IadeHakkiBitti { get; init; }
    public required string SatisDuzeltmeYolu { get; init; }
    public required string FisNo { get; init; }
    public required string CariSecin { get; init; }
    public required string UrunSecinUyari { get; init; }
    // UX-4d madde 5
    public required string TevsikNakitOnerisi { get; init; }
    public required string Baslik { get; init; }
    public required string Satis { get; init; }
    public required string Alis { get; init; }
    public required string HurdaAlis { get; init; }
    public required string Takas { get; init; }
    public required string Iscilik { get; init; }
    public required string Transfer { get; init; }
    public required string Formu { get; init; }
    public required string KasaId { get; init; }
    public required string KasaIdOpsiyonel { get; init; }
    public required string Kasa { get; init; }
    public required string KasaSecin { get; init; }
    /// <summary>UX-S22 — tek kasa varsa seçici kilitlenir, sebebi yazar.</summary>
    public required string TekKasaOtomatik { get; init; }
    /// <summary>UX-S21 — türetilmiş değerin kaynağı rozette söylenir.</summary>
    public required string KurdanGeldi { get; init; }
    public required string ElleGirildi { get; init; }
    public required string OdemeTuru { get; init; }
    public required string OdemeNakit { get; init; }
    public required string OdemeKart { get; init; }
    public required string OdemeHavale { get; init; }
    public required string OdemeAltin { get; init; }
    public required string OdemeDoviz { get; init; }
    public required string DovizMiktar { get; init; }
    public required string DovizTlKarsiligi { get; init; }
    public required string OdemeVeresiye { get; init; }
    // ── Karma ödeme (çok bacaklı fiş) — 2026-09-15
    public required string KarmaOdeme { get; init; }
    public required string KarmaOdemeIpucu { get; init; }
    public required string Bacaklar { get; init; }
    public required string BacakEkle { get; init; }
    public required string BacakSil { get; init; }
    public required string Arac { get; init; }
    // PAKET P-10 (Y-3): OdemeBacagiDto.PosCihazId + PosTaksitAdedi sunucuda
    // vardı, KART bacağında ekranda hiç sorulmuyordu (UX-S24).
    public required string BacakPosCihazi { get; init; }
    /// <summary>P-36c: ödeme bacağının ARAÇ seçicisinin etiketi (ham select göçü).</summary>
    public required string BacakArac { get; init; }
    /// <summary>P-36c: taksit seçicisinin etiketi.</summary>
    public required string BacakTaksit { get; init; }
    /// <summary>P-36c/UX-S30: kasa tek olduğu için seçici kilitli — sebebi.</summary>
    public required string BacakKasaOtomatik { get; init; }
    /// <summary>P-36c/UX-S30: tek POS cihazı var, seçici kilitli — sebebi.</summary>
    public required string BacakPosTek { get; init; }
    public required string BacakPosTaksit { get; init; }
    public required string BacakPosOtomatik { get; init; }
    public required string BacakPosCihazYok { get; init; }
    public required string FisToplami { get; init; }
    public required string BacakToplami { get; init; }
    public required string Kalan { get; init; }
    public required string KalaniAta { get; init; }
    public required string BacakToplamiUyusmuyor { get; init; }
    // ── İade — 2026-09-15
    public required string SatisIade { get; init; }
    public required string AlisIade { get; init; }
    public required string IadeEdilecekIslem { get; init; }
    public required string IslemSecin { get; init; }
    public required string IadeAdedi { get; init; }
    public required string IadeTutari { get; init; }
    public required string OrantiliTutar { get; init; }
    public required string KalanIadeHakki { get; init; }
    public required string IadeIslemiYok { get; init; }
    public required string NakitKapali { get; init; }
    public required string KalanNakitHakki { get; init; }
    public required string HasGramFiyati { get; init; }

    // ── DP4-03 (QA turu 4 KRİTİK) — hurda alışta satıcı kimliği (VUK md.234) ──
    public required string SaticiKimligi { get; init; }
    public required string SaticiKimligiNot { get; init; }
    public required string SaticiTipi { get; init; }
    public required string SaticiTipiNihai { get; init; }
    public required string SaticiTipiEsnaf { get; init; }
    public required string SaticiTipiMukellef { get; init; }
    public required string SaticiAdSoyad { get; init; }
    public required string SaticiTckn { get; init; }
    public required string SaticiAdres { get; init; }
    public required string TedarikciFaturaNo { get; init; }
    public required string SaticiTipiZorunlu { get; init; }
    public required string SaticiAdZorunlu { get; init; }
    public required string SaticiTcknZorunlu { get; init; }
    public required string SaticiTcknGecersiz { get; init; }
    public required string TedarikciFaturaNoZorunlu { get; init; }
    public required string SaticiKimligiEksik { get; init; }
    public required string CaridenGeldi { get; init; }

    // ── DP4 YÜKSEK #2 — MASAK kilit metni tam cümle olsun (UX-S30) ──────────
    public required string MasakKimlikEksik { get; init; }

    public required string TakasEskiUrun { get; init; }
    public required string TakasYeniUrun { get; init; }
    public required string Fark { get; init; }
    public required string FarkMusteriOder { get; init; }
    public required string FarkIadeEdilir { get; init; }
    public required string KaynakSube { get; init; }
    public required string HedefSube { get; init; }
    public required string SubeSecin { get; init; }
    public required string BirimFiyat { get; init; }
    public required string Birim { get; init; }
    public required string Urun { get; init; }
    public required string Kaydet { get; init; }
    public required string Basarili { get; init; }
    public required string Basarisiz { get; init; }
    public required string BilgiKontrol { get; init; }
    public required string IscilikTutari { get; init; }
    public required string GramAgirlik { get; init; }
    public required string AciklamaOpsiyonel { get; init; }

    // ── MASAK (SIRADAKI-ADIM-KARAR §2/A — Islemler.razor MASAK formu) ──
    public required string MasakBaslik { get; init; }
    public required string MasakAciklama { get; init; }
    public required string MasakBelgeTuru { get; init; }
    public required string MasakBelgeNo { get; init; }
    public required string MasakAdSoyad { get; init; }
    public required string MasakDogumTarihi { get; init; }
    public required string MasakUyruk { get; init; }
    public required string MasakAdres { get; init; }
    public required string MasakIslemAmaci { get; init; }
    public required string MasakBeyanAciklama { get; init; }
    public required string MasakFonKaynagi { get; init; }
    public required string MasakEkBelge { get; init; }
    public required string MasakKademeKimlik { get; init; }
    public required string MasakKademeAmac { get; init; }
    public required string MasakKademeForm { get; init; }
    public required string MasakKademeEkBelge { get; init; }
    public required string MasakEnAzKarakter { get; init; }

    // ── PAKET P-16 (Y-4) — SATIŞ KALEMİ: AYAR/MİLYEM/GRAM/HAS + İSKONTO ─────
    public required string KalemAyar { get; init; }
    public required string KalemMilyem { get; init; }
    public required string KalemGram { get; init; }
    public required string KalemHas { get; init; }
    public required string SatirIskonto { get; init; }
    public required string FisIskonto { get; init; }
    public required string IskontoIpucu { get; init; }
    public required string IskontoOnizleme { get; init; }
    public required string IskontoUygulandi { get; init; }
    public required string IskontoCariden { get; init; }
    public required string ListedenGeldi { get; init; }
    public required string PasifCariGizlendi { get; init; }
    public required string NetTutar { get; init; }
}

/// <summary>
/// PAKET 31 — POS alacak defteri sözlüğü. UX-S31: ham veritabanı kodu
/// (BEKLIYOR/KISMI/YATTI/IPTAL) ekrana BASILMAZ, buradan çevrilir.
/// </summary>
public sealed class SozlukPos
{
    public required string Baslik { get; init; }
    public required string TahsilatBaslik { get; init; }
    public required string YeniCihaz { get; init; }
    public required string CihazAdi { get; init; }
    public required string BankaAdi { get; init; }
    public required string UyeIsyeriNo { get; init; }
    public required string TerminalNo { get; init; }
    public required string BagliKasa { get; init; }
    public required string BankaHesabi { get; init; }
    public required string KomisyonOran { get; init; }
    public required string ValorGun { get; init; }
    public required string BekleyenAlacak { get; init; }
    public required string BugunYatacak { get; init; }
    public required string CihazSayisi { get; init; }
    public required string ValorTakvimi { get; init; }
    public required string ValorTarihi { get; init; }
    public required string Brut { get; init; }
    public required string Komisyon { get; init; }
    public required string Net { get; init; }
    public required string Yatan { get; init; }
    public required string Kalan { get; init; }
    public required string Taksit { get; init; }
    public required string IslemTarihi { get; init; }
    public required string Durum { get; init; }
    public required string DurumBekliyor { get; init; }
    public required string DurumKismi { get; init; }
    public required string DurumYatti { get; init; }
    public required string DurumIptal { get; init; }
    public required string Duzenle { get; init; }
    public required string PasifeAl { get; init; }
    public required string Aktiflestir { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string AcikAlacakUyarisi { get; init; }
    public required string Iptal { get; init; }
    public required string IptalGerekcesi { get; init; }
    public required string IptalOnay { get; init; }
    public required string KayitYok { get; init; }
    public required string CihazYok { get; init; }
    public required string DefterGerekce { get; init; }
    public required string DefterAlternatif { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): kasa kilidi kendi sebebini söyler, POS iptalini değil.</summary>
    public required string KasaKilitGerekce { get; init; }
    public required string KasaKilitYolu { get; init; }

    // PAKET P-22 / B-55 — tahsilat varken "yeni kasaya taşı" göç akışı.
    public required string KasaGocBaslat { get; init; }
    public required string KasaGocBaslik { get; init; }
    public required string KasaGocAciklama { get; init; }
    public required string KasaGocUyari { get; init; }
    public required string KasaGocGerekce { get; init; }
    public required string KasaGocOnayla { get; init; }
    public required string KasaGocSebep { get; init; }
    public required string KasaGocTamam { get; init; }
    public required string YeniKasa { get; init; }
    public required string KomisyonAciklama { get; init; }
    public required string ValorAciklama { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string DonemdenBagimsiz { get; init; }
    public required string TaksitOranlari { get; init; }
    public required string TaksitOranEkle { get; init; }
    public required string Eslestir { get; init; }
    public required string OtoOneri { get; init; }
    public required string OneriYok { get; init; }
    public required string GeriAl { get; init; }
    public required string GeriAlmaGerekcesi { get; init; }
    public required string Kaydedildi { get; init; }
    public required string TatilUyarisi { get; init; }
    public required string Eylemler { get; init; }
    public required string AdCokKisa { get; init; }
    public required string TumDurumlar { get; init; }
    public required string BankaHareketId { get; init; }
}

public sealed class SozlukKasa
{
    public required string Baslik { get; init; }
    public required string YeniKasa { get; init; }
    public required string KasaAdi { get; init; }
    public required string Doviz { get; init; }
    public required string ToplamTlBakiye { get; init; }
    public required string KasaSayisi { get; init; }
    public required string AktifKasa { get; init; }
    public required string Hareket { get; init; }
    public required string HareketGir { get; init; }
    public required string KasaHareketi { get; init; }
    public required string HareketTuru { get; init; }
    public required string KasaGecmisi { get; init; }
    public required string KasaBulunamadi { get; init; }
    public required string Olusturulamadi { get; init; }
    public required string Duzenle { get; init; }
    public required string Duzenlenemedi { get; init; }
    public required string AdDuzenle { get; init; }
    public required string PasifeAl { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string BakiyeliKasaUyarisi { get; init; }
    // ── Gün sonu kapanışı — 2026-09-15
    public required string GunKapanisi { get; init; }
    public required string GunKapat { get; init; }
    public required string SayilanNakit { get; init; }
    public required string DefterBakiye { get; init; }
    public required string Fark { get; init; }
    public required string FarkGerekcesi { get; init; }
    public required string FarkGerekcesiZorunlu { get; init; }
    public required string GunKapali { get; init; }
    public required string GunuYenidenAc { get; init; }
    public required string AcmaGerekcesi { get; init; }
    public required string KapanisGecmisi { get; init; }
    public required string KapanisYok { get; init; }
    public required string HareketGirilemedi { get; init; }
    public required string HareketKaydedildi { get; init; }
    public required string Gelir { get; init; }
    public required string Gider { get; init; }
    public required string Tahsilat { get; init; }
    public required string Odeme { get; init; }
    // ── paket UX-1-C2: düzeltilebilirlik (çekmece · storno · aktife alma) ────
    public required string SayfaAciklama { get; init; }
    public required string Sube { get; init; }
    public required string DovizKilit { get; init; }
    public required string PasifKasaUyari { get; init; }
    public required string HareketStornoYok { get; init; }
    public required string HareketStornoYolu { get; init; }
    public required string HareketDuzenlenemez { get; init; }
    // ── paket S-3b: elle kasa hareketi artık düzeltilebilir ──
    public required string HareketAciklamaYok { get; init; }
    // ── PAKET P-12 (B-41) — kasa hareketi açıklaması düzeltilebilir ──
    public required string HareketAciklamaGuncellendi { get; init; }
    public required string HareketModalBaslik { get; init; }
    public required string HareketSonuc { get; init; }
    public required string HareketStornoOnay { get; init; }
    public required string KasaKaydedildi { get; init; }
    public required string GunKapatildi { get; init; }
}

public sealed class SozlukRapor
{
    public required string Baslik { get; init; }
    public required string AylikSatis { get; init; }
    public required string AylikAlis { get; init; }
    public required string Kar { get; init; }
    public required string NetKar { get; init; }
    public required string NetHasGram { get; init; }
    public required string NetHas { get; init; }
    /// <summary>Paket 20 — maden dağılımı tablosundaki satır sayısı sütunu.</summary>
    public required string HareketSayisi { get; init; }
    public required string GunlukSatisAlis { get; init; }
    public required string StokDagilimi { get; init; }
    public required string CariYaslandirma { get; init; }
    public required string StokDegeri { get; init; }
    public required string Gun_0_30 { get; init; }
    public required string Gun_31_60 { get; init; }
    public required string Gun_61_90 { get; init; }
    public required string Gun_90_plus { get; init; }
    public required string Bugun { get; init; }
    public required string AylikRapor { get; init; }
    public required string KarMarji { get; init; }
    public required string IscilikGeliri { get; init; }
    public required string HurdaAlis { get; init; }
    public required string ToplamAdet { get; init; }
    public required string CariBasligi { get; init; }
    /// <summary>P-47 — cari yaşlandırmada has bakiyesi kolonu (TL'ye ÇEVRİLMEZ, ayrı okunur).</summary>
    public required string YasHasBakiye { get; init; }
    /// <summary>P-47 — TL toplam kolonunun başlığı (has karışmadığını söyler).</summary>
    public required string YasToplamTl { get; init; }
    /// <summary>P-47 — TL bakiyesi sıfır, has bakiyesi dolu satırın rozeti.</summary>
    public required string YasHasBakiyeVar { get; init; }
    /// <summary>P-47 — tablo altı tek cümlelik not.</summary>
    public required string YasHasNotu { get; init; }
    /// <summary>P-47 — alt toplam satırının etiketi.</summary>
    public required string YasListelenenToplam { get; init; }
    /// <summary>P-47 — UX-S29 sınır uyarısı ({0} = listelenen, {1} = toplam cari).</summary>
    public required string YasSinirUyari { get; init; }
}

public sealed class SozlukBanka
{
    public required string Baslik { get; init; }
    public required string YeniHesap { get; init; }
    public required string BankaAdi { get; init; }
    public required string HesapAdi { get; init; }
    public required string HesapSahibi { get; init; }
    public required string Iban { get; init; }
    public required string HesapBulunamadi { get; init; }
    public required string HesapOlusturulamadi { get; init; }
    public required string ToplamBakiye { get; init; }
    public required string HesapSayisi { get; init; }
    public required string Hareketler { get; init; }
    public required string YeniHareket { get; init; }
    public required string DekontGir { get; init; }
    public required string Yon { get; init; }
    public required string Gelen { get; init; }
    public required string Giden { get; init; }
    public required string KarsiTaraf { get; init; }
    public required string KarsiIban { get; init; }
    public required string HareketEklenemedi { get; init; }
    public required string HareketEklendi { get; init; }
    public required string FaturayaDonustur { get; init; }
    public required string Faturalandi { get; init; }
    public required string Bekliyor { get; init; }
    public required string Eslesti { get; init; }
    public required string DurumFiltre { get; init; }
    public required string CariSecInfo { get; init; }
    public required string DonusturBasarili { get; init; }
    public required string DonusturBasarisiz { get; init; }
    public required string HesabiDuzenle { get; init; }
    // ── paket UX-1-C2 ──
    public required string SayfaAciklama { get; init; }
    public required string HesapKaydedildi { get; init; }
    public required string DekontDegismez { get; init; }
    public required string DekontDegismezYolu { get; init; }
    // ── PAKET P-12 (B-42) — dekont düzeltme + storno ──
    public required string DekontGuncellendi { get; init; }
    public required string DekontStornolandi { get; init; }
    public required string DekontStornoYok { get; init; }
    public required string DekontStornoYolu { get; init; }
    public required string DekontDuzenle { get; init; }
    public required string DekontKilitliAlanlar { get; init; }
    public required string PasifHesapUyari { get; init; }
    /// <summary>Kurucu 31.10 — bankanın kendi söylediği bakiye satırı.</summary>
    public required string BankadakiBakiye { get; init; }
    public required string BankaBloke { get; init; }
    /// <summary>{0} = bankadaki bakiye ile Mihenk kaydı arasındaki fark.</summary>
    public required string BankaBakiyeFarki { get; init; }
    public required string HesapGuncellendi { get; init; }
    public required string HesapGuncellenemedi { get; init; }
    // ── paket 41 — MASAK kapısı banka hareketlerine yayıldı ──
    public required string DekontCari { get; init; }
    public required string DekontCariYok { get; init; }
    public required string DekontCariIpucu { get; init; }
    public required string MasakCariUyari { get; init; }
    public required string MasakKilitSebep { get; init; }
    public required string MasakYukleniyorSebep { get; init; }
    public required string MasakTespitDegismez { get; init; }
    // ── paket 32 — banka ekstresi içe aktarma ──
    public required string EkstreYukle { get; init; }
    public required string EkstreBaslik { get; init; }
    public required string EkstreAciklama { get; init; }
    public required string EkstreDosyasi { get; init; }
    public required string EkstreFormat { get; init; }
    public required string Prova { get; init; }
    public required string ToplamSatir { get; init; }
    public required string YeniSatir { get; init; }
    public required string Mukerrer { get; init; }
    public required string HataliSatir { get; init; }
    public required string OnaylananSatir { get; init; }
    public required string Atla { get; init; }
    public required string AtlamayiGeriAl { get; init; }
    public required string SeciliOnayla { get; init; }
    public required string Eslestir { get; init; }
    public required string OneriListesi { get; init; }
    public required string CariBagla { get; init; }
    public required string BagiKaldir { get; init; }
    public required string IbanOgrenildi { get; init; }
    public required string AcilisBakiye { get; init; }
    public required string KapanisBakiye { get; init; }
    public required string DosyaZatenYuklendi { get; init; }
    public required string TekrarYukle { get; init; }
    public required string SatirBulunamadi { get; init; }
    public required string SablonIndir { get; init; }
    public required string TumunuSec { get; init; }
    public required string YuklemeYazmaz { get; init; }
    /// <summary>P-38 · B-18 — seçimdeki carisiz satır uyarısı ({0} = adet).</summary>
    public required string CarisizSatirUyari { get; init; }
    public required string CarisizOnayla { get; init; }
    public required string CarisizOnaylaIpucu { get; init; }
    /// <summary>{0} = bilerek carisiz yazılan hareket sayısı.</summary>
    public required string CarisizUretilen { get; init; }
    /// <summary>{0} = cari yok diye onaylanmayan satır sayısı.</summary>
    public required string CarisizAtlanan { get; init; }
    public required string MukerrerGecmisUyari { get; init; }
    public required string OnaylananSatirDegismez { get; init; }
    public required string OnaylananSatirYolu { get; init; }
    public required string SecimYokSebep { get; init; }
    public required string OnaySiniriSebep { get; init; }
    public required string HamSatir { get; init; }
    public required string EkstreYuklendi { get; init; }
    public required string EkstreOnaylandi { get; init; }
    public required string SinirUyari { get; init; }
    public required string Mt940Yok { get; init; }
}

public sealed class SozlukFatura
{
    public required string Baslik { get; init; }
    public required string YeniFatura { get; init; }
    public required string FaturaNo { get; init; }
    public required string Tur { get; init; }
    public required string Satis { get; init; }
    public required string Alis { get; init; }
    public required string AraToplam { get; init; }
    public required string KdvOran { get; init; }
    public required string KdvTutar { get; init; }
    public required string Toplam { get; init; }
    public required string KdvTuru { get; init; }
    public required string KdvStandart { get; init; }
    public required string KdvKuyum { get; init; }
    public required string HasDeger { get; init; }
    public required string HasDegerIpucu { get; init; }
    public required string Matrah { get; init; }
    public required string Durum { get; init; }
    public required string Taslak { get; init; }
    public required string Kesildi { get; init; }
    public required string Odendi { get; init; }
    public required string Iptal { get; init; }
    public required string FaturaBulunamadi { get; init; }
    public required string Olusturulamadi { get; init; }
    public required string KaynakBanka { get; init; }
    public required string Manuel { get; init; }
    public required string Kaynak { get; init; }
    // ── PAKET P-26 (Y-40) — özel matrah beyanı (UX-S21/UX-S31: kod tek başına basılmaz)
    public required string OzelMatrahBaslik { get; init; }
    public required string OzelMatrahKapali { get; init; }
    public required string OzelMatrahAralikDisi { get; init; }
    public required string OzelMatrahOkunamadi { get; init; }
    public required string OdendiIsaretle { get; init; }
    public required string Kes { get; init; }
    public required string IptalEt { get; init; }
    // ── Paket 1 Faz A — satır kalemleri ──
    public required string Kalemler { get; init; }
    public required string Sira { get; init; }
    public required string Miktar { get; init; }
    public required string BirimFiyat { get; init; }
    public required string KdvMatrah { get; init; }
    public required string SatirToplam { get; init; }
    public required string Iscilik { get; init; }
    public required string Indirim { get; init; }
    public required string TasIbaresi { get; init; }
    public required string KalemsizFatura { get; init; }
    // ── paket UX-1-C2 ──
    public required string SayfaAciklama { get; init; }
    public required string KesOnay { get; init; }
    public required string OdendiOnay { get; init; }
    public required string IptalOnay { get; init; }
    public required string DurumDegisti { get; init; }
    public required string FaturaDegismez { get; init; }
    public required string FaturaDegismezYolu { get; init; }
    public required string KalemDegismez { get; init; }

    // PAKET P-22 / B-3 — TASLAK fatura ve kalemleri düzenlenebilir.
    public required string NoKesimde { get; init; }
    public required string TaslakDuzenle { get; init; }
    public required string TaslakGuncellendi { get; init; }
    public required string KalemGuncellendi { get; init; }
    public required string SayiGecersiz { get; init; }
    public required string ToplamKalemden { get; init; }
    public required string KesilmisDegismez { get; init; }
    public required string KuyumIslemZorunlu { get; init; }
    public required string ToplamUyusmuyor { get; init; }
    public required string KalemEkle { get; init; }
    public required string KalemSil { get; init; }
    public required string KalemToplami { get; init; }
    public required string KalemliKes { get; init; }
    public required string KalemGoster { get; init; }
    public required string KalemBulunamadi { get; init; }
    public required string Urun { get; init; }
    // ── Paket 1 Faz A — fişten fatura ──
    public required string FistenKes { get; init; }
    public required string FisKimligi { get; init; }
    public required string Onizle { get; init; }
    public required string FisKalemSayisi { get; init; }
    public required string FisOnizlemeIpucu { get; init; }
    // ── Paket 38 — özel matrah (kuyum KDV'si) ──
    public required string FaturalanacakIslem { get; init; }
    public required string IslemSecin { get; init; }
    public required string IslemSecilmedi { get; init; }
    public required string FaturalanabilirYok { get; init; }
    public required string FaturalanabilirSinir { get; init; }
    public required string HasDegerKaynagi { get; init; }
    public required string KaynakHareket { get; init; }
    public required string KaynakElle { get; init; }
    public required string HasKuru { get; init; }
    public required string KapsananIslem { get; init; }
    public required string DefterToplami { get; init; }
    public required string OnizlemeyiGuncelle { get; init; }
    public required string OnizlemeYok { get; init; }
    public required string OzelMatrahKodu { get; init; }
}

public sealed class SozlukAyarlar
{
    public required string Baslik { get; init; }
    public required string ProfilBilgileri { get; init; }
    public required string SifreDegistir { get; init; }
    public required string MevcutSifre { get; init; }
    public required string YeniSifre { get; init; }
    public required string SifreGuncellendi { get; init; }
    public required string SifreDegistirilemedi { get; init; }
    public required string SifrelerEslesmiyor { get; init; }
    public required string SifreyiGuncelle { get; init; }
    public required string Dil { get; init; }
    public required string Turkce { get; init; }
    public required string Ingilizce { get; init; }
}

public sealed class SozlukKabuk
{
    public required string MenuAc { get; init; }
    public required string MenuKapat { get; init; }
    public required string Kaydediliyor { get; init; }
    public required string SunucuOnayiBekleniyor { get; init; }
    public required string DuzenlemekIcinTikla { get; init; }
    public required string KaydetKilitli { get; init; }
    public required string BosBirakilamaz { get; init; }
    /// <summary>PAKET P-06 (B-18) — sayıya çevrilemeyen giriş.</summary>
    public required string SayiGecersiz { get; init; }
    // ── PAKET P-15 (Y-29 · Y-5 · B-25 · B-29) — KABUK ARAMA / KAYIT SEÇİCİ ──
    // UX-S31: ham veritabanı kodu (CARI, URUN, FIS…) ekrana BASILMAZ; tür
    // etiketleri buradan çevrilir. Anahtar eksikse DERLEME HATASI verir
    // (required init) — "çıplak metin" yolu kapalıdır.
    public required string AramaAc { get; init; }
    public required string AramaBaslik { get; init; }
    public required string AramaIpucu { get; init; }
    public required string AramaEnAz { get; init; }
    public required string AramaSonucYok { get; init; }
    /// <summary>P-38 · B-25 — liste bir sorgunun değil, "en son kayıtlar" gözatmasının sonucu.</summary>
    public required string AramaGozatma { get; init; }
    public required string AramaHata { get; init; }
    public required string AramaEkranlar { get; init; }
    public required string AramaKayitlar { get; init; }
    public required string AramaKapat { get; init; }
    // ── PAKET P-37 — MENÜ ARAMASI · FAVORİLER · SIRALAMA (kurucu talimatı 20)
    //    Arama artık düğme arkasında değil, menünün EN ÜSTÜNDE duran bir kutudur.
    public required string MenuAramaEtiket { get; init; }
    public required string MenuAramaKisayol { get; init; }
    /// <summary>P-37 — menü arama kutusunun yer tutucusu. KISA OLMAK ZORUNDA:
    /// 240px kenar çubuğunda girdiye ~128px kalır, uzun cümle KESİLİR
    /// (kurucu şikâyeti: "kesik yer tutucu metni").</summary>
    public required string MenuAramaYerTutucu { get; init; }
    public required string AramaKayitHatasi { get; init; }
    public required string Favoriler { get; init; }
    public required string FavoriEkle { get; init; }
    public required string FavoriCikar { get; init; }
    public required string FavoriIpucu { get; init; }
    public required string SiraYukari { get; init; }
    public required string SiraAsagi { get; init; }
    /// <summary>P-37 — sürükleme tutamacının ipucu (görünür ok düğmesi YOK).</summary>
    public required string SiraTutamac { get; init; }
    /// <summary>P-37 — menü aramasındaki temizle (×) düğmesi.</summary>
    public required string AramaTemizle { get; init; }
    public required string SirayiSifirla { get; init; }
    public required string SiraGrupKilidi { get; init; }
    public required string SiraKaydedilemedi { get; init; }
    public required string YetkiYuklenemedi { get; init; }
    public required string YetkiYenidenDene { get; init; }
    public required string SeciciAra { get; init; }
    public required string SeciciTemizle { get; init; }
    public required string SeciciSecili { get; init; }
    public required string SeciciSecimYok { get; init; }
    public required string SeciciListeAc { get; init; }
    public required string SeciciListeKapat { get; init; }
    public required string SeciciEslesmeYok { get; init; }
    public required string SeciciAramaYerTutucu { get; init; }
    public required string SeciciSinir { get; init; }
    public required string SeciciCokSecenek { get; init; }
    public required string SeciciSec { get; init; }
    public required string SeciciKilitli { get; init; }
    public required string SubeKapsami { get; init; }
    public required string SubeTumu { get; init; }
    public required string SubeOturumdan { get; init; }
    public required string SubeTekKaynak { get; init; }
    public required string TurCari { get; init; }
    public required string TurUrun { get; init; }
    public required string TurFis { get; init; }
    public required string TurFatura { get; init; }
    public required string TurBankaHareket { get; init; }
    public required string TurHareket { get; init; }
    // ── PAKET P-18 (B-9) — KartListe sarmalayıcısının erişilebilir adı + ipucu.
    public required string TabloBolgesi { get; init; }
    public required string TabloKaydirIpucu { get; init; }
    // B-29 — derin bağlantı rozeti ve geri dönüş yolu (her sayfada AYNI ad).
    public required string BaglantidanSuzgec { get; init; }
    public required string BaglantiSuzgeciniTemizle { get; init; }
    // ── PAKET S-3a: sayfa kökü hata sınırı (KabukHatasi) ─────────────────────
    public required string SayfaHatasi { get; init; }
    public required string SayfaHatasiYeniden { get; init; }
    // ── UX-1-C0: ortak form altyapısı (alan altı mesaj · sonuç bandı) ────────
    public required string ZorunluAlan { get; init; }
    public required string SifirdanBuyuk { get; init; }
    public required string NegatifOlamaz { get; init; }
    public required string FormEksik { get; init; }
    public required string Kaydedildi { get; init; }
    public required string EpostaGecersiz { get; init; }
    public required string TelefonGecersiz { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string SinirAsildi { get; init; }
    public required string PasifleriGoster { get; init; }
    public required string AktifeAl { get; init; }
    public required string AktifeAlindi { get; init; }
    public required string YetkiYok { get; init; }
    public required string IsKurali { get; init; }
    // ── UX-1-C2: defter düzeltme (storno) · pasifleştirme · onay ─────────────
    public required string Storno { get; init; }
    public required string StornoBaslik { get; init; }
    public required string StornoGerekce { get; init; }
    public required string StornoOnay { get; init; }
    public required string StornoYapildi { get; init; }
    public required string StornoluKayit { get; init; }
    public required string StornoKurYok { get; init; }
    // ── PAKET P-19 (Y-11) — virman bacağı: iki bacaklı kaydın yarısı ──────────
    //   Kasa ve banka ekranı AYNI kavramı AYNI adla söyler (UX-S31: tek kaynak).
    public required string VirmanBacagi { get; init; }
    public required string VirmanBacagiGerekce { get; init; }
    public required string VirmanStornosunaGit { get; init; }
    public required string PasifeAl { get; init; }
    public required string PasifeAlindi { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string Onayla { get; init; }
    public required string TarihAraligi { get; init; }
    public required string DegisiklikYok { get; init; }
    // ── PAKET P-40 — bulunamayan rota: sessiz boş ekran yerine görünür durum ──
    //   UX-S28: içerik de hata da göstermeyen ekran yasaktır. 404 bir HATA durumudur.
    public required string RotaYokBaslik { get; init; }
    public required string RotaYokAciklama { get; init; }
    public required string RotaYokAdres { get; init; }
    public required string RotaYokPanele { get; init; }
    public required string RotaYokBenzer { get; init; }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-36b — GİRDİ AİLESİ ve GÖRÜNÜM metinleri.
// ⚠ Çıplak metin yasağı: Bilesenler/Form/* ve /ayarlar Görünüm bölümü hiçbir
//   dizgeyi koda gömmez; hepsi buradan TR+EN olarak gelir.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SozlukForm
{
    public required string SayiAnlasilmadi { get; init; }
    public required string TelefonYerTutucu { get; init; }
    public required string TelefonIpucu { get; init; }
    public required string EpostaYerTutucu { get; init; }
    public required string AralikBaslangic { get; init; }
    public required string AralikBitis { get; init; }
    public required string AnahtarAcik { get; init; }
    public required string AnahtarKapali { get; init; }
}

public sealed class SozlukGorunum
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string RenkSemasi { get; init; }
    public required string YaziOlcegi { get; init; }
    public required string SemaAltin { get; init; }
    public required string SemaAltinAciklama { get; init; }
    public required string SemaKurumsal { get; init; }
    public required string SemaKurumsalAciklama { get; init; }
    public required string SemaZumrut { get; init; }
    public required string SemaZumrutAciklama { get; init; }
    public required string OlcekYogun { get; init; }
    public required string OlcekYogunAciklama { get; init; }
    public required string OlcekOkunakli { get; init; }
    public required string OlcekOkunakliAciklama { get; init; }
    public required string SahipGerekli { get; init; }
    public required string Yuklenmedi { get; init; }
    public required string Kaydedildi { get; init; }
    public required string KontrastNotu { get; init; }
}

public sealed class SozlukTema
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Sistem { get; init; }
    public required string Aydinlik { get; init; }
    public required string Karanlik { get; init; }
}

public sealed class SozlukArayuz
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Basit { get; init; }
    public required string Profesyonel { get; init; }
    public required string KiraciVarsayilani { get; init; }
    public required string ModAciklamaBasit { get; init; }
    public required string ModAciklamaPro { get; init; }
    public required string VarsayilanBaslik { get; init; }
    public required string VarsayilanAciklama { get; init; }
    public required string AdimUrun { get; init; }
    public required string AdimMusteri { get; init; }
    public required string AdimOdeme { get; init; }
    public required string AdimFis { get; init; }
    public required string YasalKapiAcildi { get; init; }
    // ── UX-3 Faz B — POS basit akışı ──
    public required string BasitAkisEtiket { get; init; }
    public required string BasitAkisIpucu { get; init; }
    public required string BasitOdemeNakit { get; init; }
    public required string SistemBelirledi { get; init; }
    public required string ProAlanIpucu { get; init; }
}

public sealed class SozlukKur
{
    public required string GramAltin { get; init; }
    public required string HasAltin { get; init; }
    public required string Ceyrek { get; init; }
    public required string Yarim { get; init; }
    public required string Tam { get; init; }
    public required string Ata { get; init; }
    public required string Usd { get; init; }
    public required string Eur { get; init; }
    public required string Gbp { get; init; }
    public required string Alis { get; init; }
    public required string Satis { get; init; }
    public required string Kaynak { get; init; }
    public required string Guncelle { get; init; }
    public required string Guncelleniyor { get; init; }
    public required string Guncellenemedi { get; init; }
    public required string Canli { get; init; }
    // ── Kur yönetim ekranı (2026-09-14): manuel giriş · geçmiş · pariteler
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string GuncelKur { get; init; }
    public required string AltinHasFiyati { get; init; }
    public required string GramAltinFiyati { get; init; }
    public required string KaynakDegistir { get; init; }
    public required string SimdiCek { get; init; }
    /// <summary>"Şimdi çek" sürerken düğmenin metni (UX-S30).</summary>
    public required string Cekiliyor { get; init; }
    /// <summary>UX-S27 — yazma işleminin sonucu bandda görünür.</summary>
    public required string KurGuncellendi { get; init; }
    // ── paket UX-1-C2 ──
    public required string ManuelKaydedildi { get; init; }
    public required string KayitDegismez { get; init; }
    public required string KayitDegismezYolu { get; init; }
    // ── paket S-3b: yanlış girilen MANUEL kur geçersiz kılınabilir ──
    public required string IptalEt { get; init; }
    public required string IptalBaslik { get; init; }
    public required string IptalOnay { get; init; }
    public required string GecersizRozet { get; init; }
    public required string IptalYapildi { get; init; }
    public required string IptalYok { get; init; }
    public required string ManuelGiris { get; init; }
    public required string ManuelUyari { get; init; }
    public required string Gecmis { get; init; }
    public required string Pariteler { get; init; }
    public required string ParitelerAciklama { get; init; }
    public required string Kod { get; init; }
    public required string Ad { get; init; }
    public required string Birim { get; init; }
    public required string AyarFiyatlari { get; init; }
    public required string GramFiyati { get; init; }
    public required string GecmisYok { get; init; }
    public required string PariteYok { get; init; }
    public required string SonGuncelleme { get; init; }
    // ── PAKET S-3e — kur kaynağı seçici (kurucu bulgusu 2026-09-18) ──────────
    public required string KaynakSecimi { get; init; }
    public required string KaynakAciklama { get; init; }
    public required string KaynakYok { get; init; }
    public required string KaynakYokSebep { get; init; }
    public required string KaynakTekSebep { get; init; }
    public required string KaynakKaydedildi { get; init; }
    public required string CekmeKapaliSebep { get; init; }
    public required string GuncellenemediRozet { get; init; }
    public required string KaynakAltinkaynak { get; init; }
    public required string KaynakAnkara { get; init; }
    public required string KaynakTruncgil { get; init; }
    public required string KaynakManuel { get; init; }
}

public sealed class SozlukKullanici
{
    public required string Baslik { get; init; }
    public required string YeniKullanici { get; init; }
    public required string AdSoyad { get; init; }
    public required string Eposta { get; init; }
    public required string Rol { get; init; }
    public required string Sifre { get; init; }
    public required string Durum { get; init; }
    public required string SonGiris { get; init; }
    public required string GirisYok { get; init; }
    public required string RolSahip { get; init; }
    public required string RolMudur { get; init; }
    public required string RolMuhasebe { get; init; }
    public required string RolTezgahtar { get; init; }
    public required string Duzenle { get; init; }
    public required string RolDegistir { get; init; }
    public required string SifreSifirla { get; init; }
    // Ayrılan çalışanın erişimi ekrandan kapatılabilmeli (2026-09-14)
    public required string PasifeAl { get; init; }
    public required string PasifeAlOnay { get; init; }
    public required string KendiniPasifeAlamaz { get; init; }
    // ── P-09 (B-36) — sahiplik devri ─────────────────────────────────────────
    public required string SahipligiDevret { get; init; }
    public required string SahipligiDevretAciklama { get; init; }
    public required string SahipligiDevretOnay { get; init; }
    public required string DevirdenSonrakiRolum { get; init; }
    public required string SahiplikDevredildi { get; init; }
    public required string SifrenizDevir { get; init; }
    public required string TekSahipUyari { get; init; }
    public required string SifreGerekli { get; init; }
    public required string SifreDegistir { get; init; }
    public required string MevcutSifre { get; init; }
    public required string YeniSifre { get; init; }
    public required string SifreIpucu { get; init; }
    public required string AktifEt { get; init; }
    public required string PasifEt { get; init; }
    public required string Aktif { get; init; }
    public required string Pasif { get; init; }
    public required string KullaniciBulunamadi { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): 429'da sayfa sessizce boş kalıyordu.</summary>
    public required string ListeYuklenemedi { get; init; }
    public required string YenidenDene { get; init; }
    public required string OtomatikDeneniyor { get; init; }
    public required string Olusturulamadi { get; init; }
    public required string Guncellenemedi { get; init; }
    public required string Olusturuldu { get; init; }
    public required string Guncellendi { get; init; }
    public required string SifreSifirlandi { get; init; }
    public required string RolGuncellendi { get; init; }
    public required string SilOnay { get; init; }
    public required string KendiniSilemez { get; init; }
    public required string YetkiYok { get; init; }
    // UX-4c madde 12
    public required string Sube { get; init; }
    public required string SubeSecin { get; init; }
    public required string SifreKuralUzunluk { get; init; }
    public required string SifreKuralHarf { get; init; }
    public required string SifreKuralRakam { get; init; }
}

public sealed class SozlukYetki
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Rol { get; init; }
    public required string TamYetkili { get; init; }
    public required string Kaydedildi { get; init; }
    public required string Kaydedilemedi { get; init; }
}

/// <summary>Denetim izi ekranı (PLAN-2 Adım 0d) — yalnız SAHİP görür.</summary>
public sealed class SozlukDenetim
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Tarih { get; init; }
    public required string Kullanici { get; init; }
    public required string Eylem { get; init; }
    public required string Varlik { get; init; }
    public required string Detay { get; init; }
    public required string Hassas { get; init; }
    public required string ToplamKayit { get; init; }
    public required string HassasKayit { get; init; }
    public required string EnEski { get; init; }
    public required string TumEylemler { get; init; }
    public required string TumVarliklar { get; init; }
    public required string YalnizHassas { get; init; }
    public required string DahaFazla { get; init; }
    public required string DegistirilemezNot { get; init; }
    public required string Baslangic { get; init; }
    public required string Bitis { get; init; }
    public required string KayitYok { get; init; }
    // ── PAKET P-24 — mutabakat sözleşmesi (B-7 · B-62) ───────────────────────
    /// <summary>Üst kartların hangi kümeyi saydığını söyler (UX-S33 / UX-S29).</summary>
    public required string SuzgeceGore { get; init; }
    /// <summary>Bitiş gününün kapsam İÇİNDE olduğunu söyler (B-62 off-by-one).</summary>
    public required string BitisGunuDahil { get; init; }
    // ── Paket 48 Faz A — bağlam damgası ve storno ────────────────────────────
    public required string Sube { get; init; }
    public required string Kasa { get; init; }
    public required string TumSubeler { get; init; }
    public required string DamgaYok { get; init; }
    public required string Storno { get; init; }
    public required string StornoGerekce { get; init; }
    public required string StornoOnay { get; init; }
    public required string StornoYapildi { get; init; }
    public required string StornoTurDesteklenmiyor { get; init; }
}

/// <summary>Uyum ekranı (PLAN-2 Adım 1) — MASAK ve saklama süresi.</summary>
public sealed class SozlukUyum
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string KimlikTespitleri { get; init; }
    public required string SupheliIslemler { get; init; }
    public required string SaklamaSuresi { get; init; }
    public required string AcikBildirim { get; init; }
    public required string SuresiGecmis { get; init; }
    public required string Yaklasan { get; init; }
    public required string Son30Gun { get; init; }
    public required string Tarih { get; init; }
    public required string AdSoyad { get; init; }
    public required string BelgeTuru { get; init; }
    public required string BelgeNo { get; init; }
    public required string IslemTutari { get; init; }
    public required string SaklamaBitis { get; init; }
    public required string Goruntule { get; init; }
    public required string BelgeNoIleAra { get; init; }
    public required string TamBelgeNo { get; init; }
    public required string AramaNotu { get; init; }
    public required string TespitYok { get; init; }
    public required string MaskeNotu { get; init; }
    public required string KimlikTespitiDetay { get; init; }
    // ── paket 41 — tespitin kaynağı (hareketler / banka_hareketleri) ──
    public required string Kaynak { get; init; }
    public required string KaynakHareket { get; init; }
    public required string KaynakBankaHareket { get; init; }
    public required string KaynakBilinmiyor { get; init; }

    /// <summary>
    /// Ham kaynak kodunu (HAREKET / BANKA_HAREKET) ekrana basmadan çevirir —
    /// UX-S31: ham veritabanı kodu ekrana basılmaz.
    /// </summary>
    public string KaynakAdi(string? kod) => kod switch
    {
        "HAREKET" => KaynakHareket,
        "BANKA_HAREKET" => KaynakBankaHareket,
        _ => KaynakBilinmiyor,
    };
    public required string OkumaIziNotu { get; init; }
    public required string DogumTarihi { get; init; }
    public required string Uyruk { get; init; }
    public required string Adres { get; init; }
    public required string Beyan { get; init; }
    public required string IslemAmaci { get; init; }
    public required string FonKaynagi { get; init; }
    public required string EkBelge { get; init; }
    public required string YeniBildirim { get; init; }
    public required string BildirimNotu { get; init; }
    public required string HareketId { get; init; }
    public required string Gerekce { get; init; }
    public required string EnAzKarakter { get; init; }
    public required string TespitTarihi { get; init; }
    public required string SonBildirim { get; init; }
    public required string Durum { get; init; }
    public required string SuresiDoldu { get; init; }
    public required string GunKaldi { get; init; }
    public required string TumDurumlar { get; init; }
    public required string DurumAcik { get; init; }
    public required string DurumGonderildi { get; init; }
    public required string DurumKapatildi { get; init; }
    public required string BildirimYok { get; init; }
    public required string GonderildiIsaretle { get; init; }
    public required string Kapat { get; init; }
    public required string GonderimEldeNotu { get; init; }
    public required string GonderimReferansiSor { get; init; }
    public required string KapatmaGerekcesiSor { get; init; }
    // UX-4d madde 10 + 18
    public required string AramaSonucu { get; init; }
    public required string AramaSonucYok { get; init; }
    public required string GonderimOnayBaslik { get; init; }
    public required string GonderimOnayNotu { get; init; }
    public required string KapatOnayBaslik { get; init; }
    public required string KapatOnayNotu { get; init; }
    public required string ReferansZorunlu { get; init; }
    public required string GerekceEnAzHata { get; init; }
    public required string BildirimOlusturuldu { get; init; }
    public required string GonderildiIsaretlendi { get; init; }
    public required string BildirimKapatildi { get; init; }
    public required string SuresiDolmusTespit { get; init; }
    public required string SuresiDolmusBeyan { get; init; }
    public required string EnYakinSaklamaBitisi { get; init; }
    public required string MaskelemeUyariBaslik { get; init; }
    public required string MaskelemeUyari { get; init; }
    public required string MaskelemeOnayi { get; init; }
    public required string MaskelemeyiUygula { get; init; }
    public required string MaskelemeSonuc { get; init; }
    public required string SaklamaDayanak { get; init; }

    // ── PAKET 40-A — risk skorlama sekmesi ──────────────────────────────────
    public required string RiskSekmesi { get; init; }
    public required string RiskAciklama { get; init; }
    public required string RiskKapaliNotu { get; init; }
    public required string SeviyeDusuk { get; init; }
    public required string SeviyeOrta { get; init; }
    public required string SeviyeYuksek { get; init; }
    public required string PepSayisi { get; init; }
    public required string Tazelenecek { get; init; }
    public required string Profilsiz { get; init; }
    public required string YuksekRiskliler { get; init; }
    public required string YuksekRiskliYok { get; init; }
    public required string RiskCariSec { get; init; }
    public required string RiskCariSecilmedi { get; init; }
    public required string Puan { get; init; }
    public required string Seviye { get; init; }
    public required string RiskKaynak { get; init; }
    public required string KaynakHesap { get; init; }
    public required string KaynakElle { get; init; }
    public required string Sebepler { get; init; }
    public required string SebepYok { get; init; }
    public required string YenidenHesapla { get; init; }
    public required string SeviyeyiElleDegistir { get; init; }
    public required string ElleBaslik { get; init; }
    public required string ElleGerekce { get; init; }
    public required string ElleGerekceEnAz { get; init; }
    public required string ElleSeviyeSec { get; init; }
    public required string ProfilTarihcesi { get; init; }
    public required string ProfilTarihceYok { get; init; }
    public required string ProfilDegismezBaslik { get; init; }
    public required string ProfilDegismezNot { get; init; }
    public required string RiskHesaplandi { get; init; }
    public required string RiskElleDegistirildi { get; init; }
    public required string RiskListeSiniri { get; init; }
    public required string RiskSkorDurdurmaz { get; init; }
    public required string SebepPepBeyani { get; init; }
    public required string SebepNakitAgirlik { get; init; }
    public required string SebepEsigeYakin { get; init; }
    public required string SebepSupheliGecmis { get; init; }
    public required string SebepKimlikEksik { get; init; }
    public required string SebepYuksekRiskliUlke { get; init; }
    public required string SebepYaptirimEslesmesi { get; init; }

    /// <summary>
    /// Sebep KODUNU (PEP_BEYANI, NAKIT_AGIRLIK…) okunur metne çevirir.
    /// ⚠ UX-S31: ham veritabanı kodu ekrana BASILMAZ. Bilinmeyen kod olduğu
    ///   gibi döner — "çeviri yok"u gizlemek yanlış bilgi üretirdi.
    /// </summary>
    public string SebepAdi(string kod) => kod switch
    {
        "PEP_BEYANI" => SebepPepBeyani,
        "NAKIT_AGIRLIK" => SebepNakitAgirlik,
        "ESIGE_YAKIN" => SebepEsigeYakin,
        "SUPHELI_GECMIS" => SebepSupheliGecmis,
        "KIMLIK_EKSIK" => SebepKimlikEksik,
        "YUKSEK_RISKLI_ULKE" => SebepYuksekRiskliUlke,
        "YAPTIRIM_ESLESMESI" => SebepYaptirimEslesmesi,
        _ => kod,
    };

    /// <summary>Seviye KODUNU okunur metne çevirir (UX-S31).</summary>
    public string SeviyeAdi(string? kod) => kod switch
    {
        "DUSUK" => SeviyeDusuk,
        "ORTA" => SeviyeOrta,
        "YUKSEK" => SeviyeYuksek,
        _ => kod ?? "",
    };

    /// <summary>Risk profili satırının kaynağını çevirir (UX-S31).</summary>
    public string RiskKaynakAdi(string? kod) => kod switch
    {
        "HESAP" => KaynakHesap,
        "ELLE" => KaynakElle,
        _ => kod ?? "",
    };
    public required string EkBeyan { get; init; }
    public required string EkBeyanBaslik { get; init; }
    public required string EkBeyanAciklama { get; init; }
    public required string EkBeyanBasarili { get; init; }
    public required string EkBeyanRozet { get; init; }
    public required string GonderildiDuzeltmeYolu { get; init; }
}

/// <summary>PAKET P-17 (Y-6) — ortak A4 çıktı şablonu.</summary>
public sealed class SozlukCikti
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Yazdir { get; init; }
    public required string YazdirilamadI { get; init; }
    public required string YazdirKilidi { get; init; }
    public required string BelgeNo { get; init; }
    public required string Dayanak { get; init; }
    public required string Duzenleyen { get; init; }
    public required string Tutarlar { get; init; }
    public required string DuzenlemeTarihi { get; init; }
    public required string SatirYok { get; init; }
    public required string KimlikEksik { get; init; }
    public required string KimlikEksikGerekce { get; init; }
    public required string TuretilmisGerekce { get; init; }
    public required string KaynaktaDuzelt { get; init; }
    /// <summary>{0} = gönderilen tür.</summary>
    public required string GecersizTur { get; init; }
    public required string BelgeAc { get; init; }
}

/// <summary>PAKET P-17 (Y-7) — vitrin / kur panosu.</summary>
public sealed class SozlukPano
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Kaynak { get; init; }
    public required string Kalem { get; init; }
    public required string Alis { get; init; }
    public required string Satis { get; init; }
    public required string Durum { get; init; }
    public required string BosDurum { get; init; }
    /// <summary>{0} = satır sayısı, {1} = eşik.</summary>
    public required string OkunabilirlikUyarisi { get; init; }
    /// <summary>{0} = tavan satır sayısı.</summary>
    public required string TavanNotu { get; init; }
    public required string AtlananBaslik { get; init; }
    public required string AtlamaSebebi { get; init; }
    public required string SebepKotasyonYok { get; init; }
    public required string SebepFiyatYok { get; init; }
    public required string SebepTersMakas { get; init; }
    public required string SebepTavanAsildi { get; init; }
    /// <summary>P-38 · B-18 — marj fiyatı sıfıra indirdi; satır vitrine çıkmadı.</summary>
    public required string SebepMarjGecersiz { get; init; }
    public required string SebepBilinmeyen { get; init; }
    public required string AyarBaslik { get; init; }
    public required string AyarAciklama { get; init; }
    public required string AyarYetkiYok { get; init; }
    public required string Etiket { get; init; }
    /// <summary>{0} = parite adı.</summary>
    public required string EtiketIpucu { get; init; }
    public required string Sira { get; init; }
    public required string AlisMarj { get; init; }
    public required string AlisMarjIpucu { get; init; }
    public required string SatisMarj { get; init; }
    public required string Yuvarlama { get; init; }
    public required string YuvarlamaKapali { get; init; }
    public required string YuvarlamaGecersiz { get; init; }
    public required string PanodaGoster { get; init; }
    public required string VarsayilanaDon { get; init; }
    public required string ZatenVarsayilan { get; init; }
    public required string SiraAralik { get; init; }
    public required string MarjAralik { get; init; }
    /// <summary>{0} = kalem adı, {1} = panodaki aktif kalem sayısı.</summary>
    public required string Kaydedildi { get; init; }
    /// <summary>{0} = kalem adı.</summary>
    public required string VarsayilanaDonuldu { get; init; }
    public required string VitrinAc { get; init; }
}

/// <summary>Gider pusulası ekranı ve VUK md.234 belgesi (PLAN-2 Adım 1).</summary>
public sealed class SozlukPusula
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Gecikmis { get; init; }
    public required string Yaklasan { get; init; }
    public required string SureGun { get; init; }
    public required string Baslangic { get; init; }
    public required string Bitis { get; init; }
    public required string KayitYok { get; init; }
    public required string PusulaNo { get; init; }
    public required string Satici { get; init; }
    public required string Tckn { get; init; }
    public required string Mal { get; init; }
    public required string Brut { get; init; }
    public required string Stopaj { get; init; }
    public required string Net { get; init; }
    public required string SonTarih { get; init; }
    public required string Detay { get; init; }
    public required string Yazdir { get; init; }
    public required string Gecikti { get; init; }
    public required string GunKaldi { get; init; }
    public required string MaskeNotu { get; init; }
    // ── paket S-3b: VUK md.234 düzeltme yolu (iptal + yeni pusula) ──
    public required string IptalEt { get; init; }
    public required string IptalBaslik { get; init; }
    public required string IptalOnay { get; init; }
    public required string IptalEdildiRozet { get; init; }
    public required string IptalYapildi { get; init; }
    public required string PusulaDetay { get; init; }
    public required string OkumaIziNotu { get; init; }
    public required string Adres { get; init; }
    public required string SaticiTipi { get; init; }
    public required string TeslimTarihi { get; init; }
    // ── PAKET S-3d / DP4: mali kimlik DIŞI alanlarda yerinde düzeltme ──
    public required string MalAciklamasi { get; init; }
    public required string IptalliDuzenlenemez { get; init; }
    public required string PusulaGuncellendi { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): boş adresle kayıt. VUK md.234 zorunlu unsuru.</summary>
    public required string AdresZorunlu { get; init; }
    public required string MalAciklamasiZorunlu { get; init; }
    public required string Belge { get; init; }
    public required string Dayanak { get; init; }
    public required string Duzenleyen { get; init; }
    public required string Cins { get; init; }
    public required string Gram { get; init; }
    public required string HasGram { get; init; }
    public required string Tutarlar { get; init; }
    public required string Yaziyla { get; init; }
    public required string DuzenlemeTarihi { get; init; }
    public required string DuzenleyenImza { get; init; }
    public required string SaticiImza { get; init; }
    public required string HurdaAlimiGir { get; init; }
}

/// <summary>Gün sonu POS–fatura mutabakatı (PLAN-2 Adım 1).</summary>
public sealed class SozlukSayim
{
    public required string Baslik { get; init; }
    public required string YeniOturum { get; init; }
    public required string Ad { get; init; }
    public required string Sube { get; init; }
    public required string Kapsam { get; init; }
    public required string KapsamTumu { get; init; }
    public required string KapsamReyon { get; init; }
    public required string KapsamKategori { get; init; }
    public required string Ac { get; init; }
    public required string Kapat { get; init; }
    public required string Iptal { get; init; }
    public required string Durum { get; init; }
    public required string Acik { get; init; }
    public required string Kapandi { get; init; }
    public required string Urun { get; init; }
    public required string Sistem { get; init; }
    public required string Sayilan { get; init; }
    public required string Guncel { get; init; }
    public required string Fark { get; init; }
    public required string Sayilmadi { get; init; }
    public required string HareketVar { get; init; }
    public required string BarkodOkut { get; init; }
    public required string Adet { get; init; }
    public required string Say { get; init; }
    public required string Ozet { get; init; }
    public required string Eksik { get; init; }
    public required string Fazla { get; init; }
    public required string NetHas { get; init; }
    public required string HareketliUygula { get; init; }
    public required string HareketliUyari { get; init; }
    public required string SayilmayanUyari { get; init; }
    public required string KapatOnay { get; init; }
    // UX-4c madde 11
    public required string KapanisSonucu { get; init; }
    public required string IptalEdildi { get; init; }
    public required string IptalOnayBaslik { get; init; }
    public required string IptalOnayMetin { get; init; }
    public required string IptalOnaySayilan { get; init; }

    // PAKET P-22 / B-44 — sayım oturumunun adı düzeltilebilir.
    public required string AdGuncellendi { get; init; }
    public required string AdIptalDegismez { get; init; }
    public required string AdIptalAlternatif { get; init; }
    public required string IptalOnayla { get; init; }
    // ── paket UX-1-C2 ──
    public required string SayfaAciklama { get; init; }
    public required string SayildiBandi { get; init; }
    public required string OturumDegismez { get; init; }
    public required string OturumDegismezYolu { get; init; }
    public required string SatirDegismez { get; init; }
    public required string SatirDegismezYolu { get; init; }
    // ── paket P-SAYIM (denetim K5, Y10) ──
    public required string OtomatikAd { get; init; }
    public required string GramMiktarYaz { get; init; }
    public required string BolumleBaslat { get; init; }
}

public sealed class SozlukFiyat
{
    public required string Baslik { get; init; }
    public required string Listeler { get; init; }
    public required string Kampanyalar { get; init; }
    public required string YeniListe { get; init; }
    public required string YeniKampanya { get; init; }
    public required string Ad { get; init; }
    public required string Tur { get; init; }
    public required string HasMarj { get; init; }
    public required string IscilikCarpan { get; init; }
    public required string Varsayilan { get; init; }
    public required string Aktif { get; init; }
    public required string Baslangic { get; init; }
    public required string Bitis { get; init; }
    public required string Hedef { get; init; }
    public required string HedefTumu { get; init; }
    public required string HedefKategori { get; init; }
    public required string HedefUrun { get; init; }
    public required string IndirimTuru { get; init; }
    public required string IscilikYuzde { get; init; }
    public required string TutarYuzde { get; init; }
    public required string TutarTl { get; init; }
    public required string Deger { get; init; }
    public required string ListeFiyati { get; init; }
    public required string IndirimOncesi { get; init; }
    public required string Indirim { get; init; }
    public required string Hediye { get; init; }
    public required string TeklifYok { get; init; }
    public required string ListeYok { get; init; }
    public required string SureUyari { get; init; }
    public required string FiyatListesi { get; init; }
    // ── paket UX-4a (madde 2-5): düzenleme yolu, ürün paneli, sessiz etkisizlik ──
    public required string PasifeAl { get; init; }
    public required string Aktiflestir { get; init; }
    public required string ListeKaydedildi { get; init; }
    public required string KampanyaKaydedildi { get; init; }
    public required string BosListe { get; init; }
    public required string BosKampanya { get; init; }
    public required string IlkListeVarsayilan { get; init; }
    public required string VarsayilanDegisir { get; init; }
    public required string VarsayilanKorunur { get; init; }
    public required string VarsayilanDevret { get; init; }
    // ── Makul aralık kapısı (paket S-3c · kurul tur 3 engel 20) ──
    public required string MarjEksiUyari { get; init; }
    public required string MarjYuksekUyari { get; init; }
    public required string CarpanYuksekUyari { get; init; }
    public required string RiskOnayiEtiket { get; init; }
    public required string RiskOnayiSart { get; init; }
    public required string MarjAralikDisi { get; init; }
    public required string CarpanAralikDisi { get; init; }
    public required string UrunPaneli { get; init; }
    public required string PanelIpucu { get; init; }
    public required string PanelKapat { get; init; }
    // ── paket UX-1-C2: sayfa başı · ham enum yerine insan okunur etiket ──
    public required string SayfaAciklama { get; init; }
    public required string TurPerakende { get; init; }
    public required string TurToptan { get; init; }
    public required string TurOzel { get; init; }
    public required string ListePasifeAlindi { get; init; }
    public required string ListeAktiflestirildi { get; init; }
    public required string KampanyaPasifeAlindi { get; init; }
    public required string KampanyaAktiflestirildi { get; init; }
    public required string TopluMarj { get; init; }
    public required string TopluMarjNot { get; init; }
    public required string Ayar { get; init; }
    public required string Gram { get; init; }
    public required string HasDegeri { get; init; }
    public required string MarjTl { get; init; }
    /// <summary>M1b — alış (maliyet) milyemiyle birim maliyet sütunu.</summary>
    public required string MaliyetTl { get; init; }
    public required string IscilikTl { get; init; }
    public required string SatisFiyati { get; init; }
    public required string SebepGumus { get; init; }
    public required string SebepListeYok { get; init; }
    public required string SebepFiyatYok { get; init; }
    public required string IscilikUyari { get; init; }
    public required string GumusUyari { get; init; }
    public required string KurYok { get; init; }
    public required string PanelBos { get; init; }
    // ── Paket 49 — liste × ürün görünümü ──
    public required string PanelKategori { get; init; }
    public required string PanelAra { get; init; }
    public required string PanelToplam { get; init; }
    public required string DahaFazlasiVar { get; init; }
    public required string YalnizGecersizKilinan { get; init; }
    public required string Kod { get; init; }
    public required string Kaynak { get; init; }
    public required string KaynakListe { get; init; }
    public required string KaynakUrunOzel { get; init; }
    public required string KaynakUrunSabit { get; init; }
    public required string UruneOzel { get; init; }
    public required string UruneOzelBaslik { get; init; }
    public required string SabitFiyatAlan { get; init; }
    public required string NotAlan { get; init; }
    public required string EnAzBirAlan { get; init; }
    public required string GumusSabitUyari { get; init; }
    public required string UruneOzelKaydedildi { get; init; }
    public required string GecersizKilmayiKaldir { get; init; }
    public required string GecersizKilmaKaldirildi { get; init; }
    public required string TopluUygulaSecili { get; init; }
    public required string TopluKategoriye { get; init; }
    public required string TopluKilit { get; init; }
    public required string TopluOzetBaslik { get; init; }
    public required string TopluOnayla { get; init; }
    public required string TopluUygulandi { get; init; }
    public required string EtkilenenUrun { get; init; }
    public required string ToplamFark { get; init; }
    public required string EskiFiyat { get; init; }
    public required string YeniFiyat { get; init; }
    public required string Fark { get; init; }
    public required string Atlananlar { get; init; }
    public required string SebepSabitFiyat { get; init; }
    public required string SebepGecersizKilmaSabit { get; init; }
    public required string SaltOkunurBasitMod { get; init; }
}

public sealed class SozlukVade
{
    public required string Baslik { get; init; }
    public required string UfukGun { get; init; }
    public required string GecikenToplam { get; init; }
    public required string YaklasanToplam { get; init; }
    public required string VadesizToplam { get; init; }
    // UX-4d madde 16
    public required string UfukOtesiToplam { get; init; }
    public required string UfukOtesiAciklama { get; init; }
    public required string AcikToplam { get; init; }
    public required string DonemdenBagimsiz { get; init; }
    public required string Musteri { get; init; }
    public required string Telefon { get; init; }
    public required string Geciken { get; init; }
    public required string Yaklasan { get; init; }
    public required string Vadesiz { get; init; }
    public required string EnEskiGecikme { get; init; }
    public required string EnYakinVade { get; init; }
    public required string Gun { get; init; }
    public required string Vade { get; init; }
    public required string VadeGun { get; init; }
    public required string VarsayilanVadeGun { get; init; }
    public required string AcikTutar { get; init; }
    public required string Durum { get; init; }
    public required string Gecikti { get; init; }
    public required string Yaklasiyor { get; init; }
    public required string Vadesinde { get; init; }
    public required string KayitYok { get; init; }
    public required string VadesizAciklama { get; init; }
}

public sealed class SozlukMutabakat
{
    public required string Baslik { get; init; }
    public required string Gun { get; init; }
    public required string Tahsilat { get; init; }
    public required string Nakit { get; init; }
    public required string SatisFaturasi { get; init; }
    public required string Fark { get; init; }
    public required string Durum { get; init; }
    public required string Mutabik { get; init; }
    public required string Mutabakatsiz { get; init; }
    public required string Satis { get; init; }
    public required string Adet { get; init; }
    public required string Faturali { get; init; }
    public required string Faturasiz { get; init; }
    public required string Hurda { get; init; }
    public required string Pusula { get; init; }
    public required string Stopaj { get; init; }
    public required string BelgesizHurda { get; init; }
    public required string FaturasizListe { get; init; }
    public required string Saat { get; init; }
    public required string Tutar { get; init; }
    public required string OdemeTuru { get; init; }
}

/// <summary>Veri dışa aktarımı — PLAN-2 Adım 4.</summary>
public sealed class SozlukDisaAktarim
{
    /// <summary>PAKET P-06 (B-54) — talep kanıt kaydıdır; düzeltme yolu.</summary>
    public required string TalepDuzeltmeYolu { get; init; }
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string YeniAktarim { get; init; }
    public required string Gerekce { get; init; }
    public required string GerekceOrnek { get; init; }
    public required string GerekceIpucu { get; init; }
    public required string Kapsam { get; init; }
    public required string KisiselVeri { get; init; }
    public required string TamKimlikIstiyorum { get; init; }
    public required string MaskeAciklama { get; init; }
    public required string OnayMetniniYazin { get; init; }
    public required string HazirlaVeIndir { get; init; }
    public required string ParolaBasligi { get; init; }
    public required string ParolaUyari { get; init; }
    public required string Kopyala { get; init; }
    public required string Kopyalandi { get; init; }
    public required string Gecmis { get; init; }
    public required string GecmisBos { get; init; }
    public required string SatirSayisi { get; init; }
    public required string Maskeli { get; init; }
    public required string TamKimlik { get; init; }
    public required string GiderPusulasi { get; init; }
}

/// <summary>Veri içe aktarımı — paket 47 Faz A (kapılı, kuru provalı).</summary>
public sealed class SozlukIceAktarim
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Kaynak { get; init; }
    public required string KaynakCari { get; init; }
    public required string KaynakUrun { get; init; }
    public required string SablonIndir { get; init; }
    public required string SablonIpucu { get; init; }
    public required string DosyaSec { get; init; }
    public required string DosyaIpucu { get; init; }
    public required string YinelenenPolitikasi { get; init; }
    public required string PolitikaHata { get; init; }
    public required string PolitikaAtla { get; init; }
    public required string PolitikaIpucu { get; init; }
    public required string ProvaCalistir { get; init; }
    public required string ProvaBasligi { get; init; }
    public required string ProvaAciklama { get; init; }
    public required string ToplamSatir { get; init; }
    public required string GecerliSatir { get; init; }
    public required string HataliSatir { get; init; }
    public required string AtlananSatir { get; init; }
    public required string YazilanKayit { get; init; }
    public required string BilinmeyenKolon { get; init; }
    public required string SatirNo { get; init; }
    public required string HataKodu { get; init; }
    public required string HataMesaji { get; init; }
    public required string HataYok { get; init; }
    public required string OnayMetniniYazin { get; init; }
    public required string Uygula { get; init; }
    public required string UygulandiMesaji { get; init; }
    public required string GeriAl { get; init; }
    public required string GeriAlOnay { get; init; }
    public required string GeriAlindiMesaji { get; init; }
    public required string Iptal { get; init; }
    public required string IptalOnay { get; init; }
    public required string Gecmis { get; init; }
    public required string GecmisBos { get; init; }
    public required string Durum { get; init; }
    public required string Dosya { get; init; }
    public required string SatirlarBos { get; init; }
    public required string SatirDegismez { get; init; }
    public required string SatirDegismezYol { get; init; }
    public required string GecmisHareketYok { get; init; }
    public required string Kaydet { get; init; }
}

/// <summary>Kuruluş sihirbazı + açılış bakiyeleri — paket T-2 Faz A.</summary>
public sealed class SozlukKurulum
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string AdimIsletme { get; init; }
    public required string AdimKullanicilar { get; init; }
    public required string AdimKur { get; init; }
    public required string AdimKasaBanka { get; init; }
    public required string AdimCariAcilis { get; init; }
    public required string AdimStokAcilis { get; init; }
    public required string AdimBitti { get; init; }
    public required string AdimEksik { get; init; }
    public required string AdimAtlandi { get; init; }
    public required string AdimAtla { get; init; }
    public required string AdimGeriAl { get; init; }
    public required string AcilisBasligi { get; init; }
    public required string AcilisAciklama { get; init; }
    public required string TurKasa { get; init; }
    public required string TurBanka { get; init; }
    public required string TurCari { get; init; }
    public required string HedefSec { get; init; }
    public required string TlTutar { get; init; }
    public required string HasGram { get; init; }
    public required string SatirEkle { get; init; }
    public required string SatirSil { get; init; }
    /// <summary>QA turu 5 (YÜKSEK): negatif kasa/banka açılışı geçerli sayılıyordu.</summary>
    public required string NegatifAcilisYok { get; init; }
    public required string ProvaCalistir { get; init; }
    public required string ProvaBasligi { get; init; }
    public required string ProvaAciklama { get; init; }
    public required string ProvaTemiz { get; init; }
    public required string Uygula { get; init; }
    public required string UygulandiMesaji { get; init; }
    public required string GeriAl { get; init; }
    public required string GeriAlOnay { get; init; }
    public required string GeriAlindiMesaji { get; init; }
    public required string Muhurle { get; init; }
    public required string MuhurOnay { get; init; }
    public required string MuhurlendiMesaji { get; init; }
    public required string MuhurGerekce { get; init; }
    public required string MuhurIsletmeAdiYaz { get; init; }
    public required string MuhurIsletmeAdiUyusmuyor { get; init; }
    public required string MuhurBilgisi { get; init; }
    public required string PencereBitis { get; init; }
    public required string PencereKapandi { get; init; }
    public required string LimitAsimiOnayi { get; init; }
    public required string KisiselVeriUyarisi { get; init; }
    public required string SatirNo { get; init; }
    public required string HataKodu { get; init; }
    public required string HataMesaji { get; init; }
    public required string HataYok { get; init; }
    public required string UyariBasligi { get; init; }
    public required string SinirNotu { get; init; }
    public required string BosDurum { get; init; }
    public required string AcilisDegismez { get; init; }
    public required string KurYok { get; init; }
    public required string KurEkle { get; init; }
    public required string AdimaGit { get; init; }
    public required string NegatifAcilisGerekce { get; init; }
    public required string HataKimligi { get; init; }
}

/// <summary>Ürün etiketleri — PLAN-2 Adım 5.</summary>
public sealed class SozlukEtiket
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string AramaIpucu { get; init; }
    public required string KopyaAdedi { get; init; }
    public required string TumunuSec { get; init; }
    public required string SecimiTemizle { get; init; }
    public required string Yazdir { get; init; }
    public required string Sec { get; init; }
    public required string BarkodYok { get; init; }
}

/// <summary>Kâr-zarar ekranı — iki bacaklı (TL + has).</summary>
public sealed class SozlukKarZarar
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string TlBacagi { get; init; }
    public required string HasBacagi { get; init; }
    public required string Hasilat { get; init; }
    public required string Maliyet { get; init; }
    public required string Kar { get; init; }
    public required string Marj { get; init; }
    public required string SatisAdedi { get; init; }
    public required string BuAy { get; init; }
    public required string GecenAy { get; init; }
    public required string Bugun { get; init; }
    public required string HasKaybiBasligi { get; init; }
    public required string HasKaybiMetin { get; init; }
    public required string MaliyetiBilinen { get; init; }
    public required string MaliyetiBilinmeyen { get; init; }
    public required string MaliyetEksik { get; init; }
    public required string UrunBazli { get; init; }
    public required string StokDegerlemesi { get; init; }
    public required string StokAdet { get; init; }
    public required string StokHas { get; init; }
    public required string StokMaliyeti { get; init; }
    public required string VeriYok { get; init; }
    public required string GuvenTam { get; init; }
    public required string GuvenYuksek { get; init; }
    public required string GuvenOrta { get; init; }
    public required string GuvenDusuk { get; init; }
    public required string GuvenVeriYok { get; init; }

    // ── Paket 35 Faz A — iade / takas / işçilik / köprü / karşılaştırma ──────
    public required string BrutHasilat { get; init; }
    public required string Iade { get; init; }
    public required string NetHasilat { get; init; }
    public required string BrutKar { get; init; }
    public required string DonemKari { get; init; }
    public required string IscilikGeliri { get; init; }
    public required string TakasFarki { get; init; }
    public required string TakasFarkiNotu { get; init; }
    public required string KarKoprusu { get; init; }
    public required string Kalem { get; init; }
    public required string Tutar { get; init; }
    public required string Karsilastir { get; init; }
    public required string KarsilastirmaYok { get; init; }
    public required string OncekiDonem { get; init; }
    public required string GecenYil { get; init; }
    public required string Degisim { get; init; }
    public required string Storno { get; init; }
    public required string StornoNotu { get; init; }
    public required string HasKar { get; init; }
    public required string HasDefterGram { get; init; }
    /// <summary>P-45 — "{0}" maden adıdır (Altın, Gümüş…). Gramlar toplanmaz.</summary>
    public required string HasDefterGramMaden { get; init; }
    public required string MadenToplanmaz { get; init; }
    public required string HasTanimNotu { get; init; }
    public required string IadeAdedi { get; init; }
}

/// <summary>Belge deposu (paket 4) — yükleme, listeleme, indirme.</summary>
public sealed class SozlukBelge
{
    public required string Baslik { get; init; }
    public required string DosyaSec { get; init; }
    public required string Yukle { get; init; }
    public required string Indir { get; init; }
    public required string PasifeAl { get; init; }
    public required string Tur { get; init; }
    public required string Boyut { get; init; }
    public required string BoyutAsildi { get; init; }
    public required string TurGecersiz { get; init; }
    public required string TavanAsildi { get; init; }
    public required string BelgeYok { get; init; }
    public required string Yuklendi { get; init; }
    public required string PasifeAlindi { get; init; }
    public required string TurSertifika { get; init; }
    public required string TurGorsel { get; init; }
    public required string TurFotograf { get; init; }
    public required string TurEk { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
// DÜZELTME SÖZLEŞMESİ — paket UX-1 Faz A.
// Politika haritası (Api/Modules/Meta/DuzeltmePolitikasi.cs) çıplak metin
// TAŞIMAZ, yalnız ANAHTAR taşır; metin burada TR+EN olarak durur.
// Yeni anahtar eklenince karşılığı buraya da eklenir —
// DuzeltmePolitikasiTestleri.Gerekce_anahtarlarinin_TR_ve_EN_karsiligi_VARDIR
// bunu zorlar.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SozlukDuzeltme
{
    public required string Baslik { get; init; }
    public required string DuzenlenemezBaslik { get; init; }
    public required string AlternatifYol { get; init; }
    public required string StornoDugme { get; init; }
    public required string StornoHenuzYok { get; init; }
    public required string Gerekce { get; init; }
    public required string EnAzKarakter { get; init; }

    public required string SerbestGerekce { get; init; }
    public required string DefterGerekce { get; init; }
    public required string DenetimGerekce { get; init; }
    public required string MasakGerekce { get; init; }
    public required string SistemGerekce { get; init; }
    public required string TuretilmisGerekce { get; init; }
    public required string KapanisGerekce { get; init; }
    public required string SayimGerekce { get; init; }
    public required string DisaAktarimGerekce { get; init; }
    public required string BelgeGerekce { get; init; }
    public required string FaturaGerekce { get; init; }
    public required string UcYokGerekce { get; init; }
    public required string StornoYokGerekce { get; init; }

    /// <summary>PAKET P-06 — DuzenlenebilirListe boş durumu (UX-S28).</summary>
    public required string ListeBos { get; init; }
    /// <summary>PAKET P-06 — sunucu liste sınırı yazısı (UX-S29, {0} = sınır).</summary>
    public required string ListeSiniri { get; init; }
    /// <summary>PAKET P-06 — iş kuralı aralık ihlali (B-18; {0} alt, {1} üst sınır).</summary>
    public required string AralikDisi { get; init; }
    /// <summary>Paket S-3b — VUK md.234 belgesi: iptal + yeni pusula.</summary>
    public required string PusulaGerekce { get; init; }
    /// <summary>Paket 47 — içe aktarım satırı kanıt kaydıdır.</summary>
    public required string IceAktarimGerekce { get; init; }

    /// <summary>Politika haritasından gelen gerekçe anahtarını metne çevirir.</summary>
    public string GerekceMetni(string? anahtar) => anahtar switch
    {
        "SERBEST" => SerbestGerekce,
        "DEFTER" => DefterGerekce,
        "DENETIM" => DenetimGerekce,
        "MASAK" => MasakGerekce,
        "SISTEM" => SistemGerekce,
        "TURETILMIS" => TuretilmisGerekce,
        "KAPANIS" => KapanisGerekce,
        "SAYIM" => SayimGerekce,
        "DISA_AKTARIM" => DisaAktarimGerekce,
        "BELGE" => BelgeGerekce,
        "FATURA" => FaturaGerekce,
        "UC_YOK" => UcYokGerekce,
        "STORNO_YOK" => StornoYokGerekce,
        "PUSULA" => PusulaGerekce,
        "ICE_AKTARIM" => IceAktarimGerekce,
        _ => "",
    };
}

// ═════════════════════════════════════════════════════════════════════════════
// SAYFA DÜZELTME SÖZLÜĞÜ — UX-1 Faz C, sayfa grubu 3.
// Gider pusulası / uyum / rapor / vade / denetim ekranlarının düzeltme yolu
// metinleri, rapor tanımları ve HAM KOD → görünen ad eşlemeleri (UX-S31).
// Ham veritabanı kodu (NIHAI_TUKETICI, TC_KIMLIK, KurSnapshot) ekrana basılmaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SozlukSayfaDuzeltme
{
    // ── Gider pusulası ──
    public required string PusulaDuzeltmeYolu { get; init; }
    public required string SaticiTipiNihai { get; init; }
    public required string SaticiTipiEsnaf { get; init; }
    public required string SaticiTipiMukellef { get; init; }
    public required string IsletmeKimligi { get; init; }
    public required string VergiDairesi { get; init; }
    public required string VergiNo { get; init; }
    public required string IsletmeAdresi { get; init; }
    public required string IsletmeTelefonu { get; init; }
    public required string IsletmeBilgisiEksik { get; init; }
    public required string IsletmeKartinaGit { get; init; }

    // ── Uyum (MASAK) ──
    public required string TespitDuzeltmeYolu { get; init; }
    public required string BildirimDuzeltmeYolu { get; init; }
    public required string IslemAciklamasi { get; init; }
    public required string TutarPozitif { get; init; }
    public required string GerekceEksik { get; init; }
    public required string KaydetKilidi { get; init; }
    public required Dictionary<string, string> BelgeTuruAdlari { get; init; }

    // ── Raporlar ──
    public required string RaporAciklamasi { get; init; }
    public required string KarKaynagi { get; init; }
    public required string KarKaynagiServis { get; init; }
    public required string KarKaynagiYok { get; init; }
    public required string SatisAlisFarki { get; init; }
    public required string SatisAlisFarkiAciklama { get; init; }
    public required string NetHasHareketi { get; init; }
    public required string NetHasAciklama { get; init; }
    public required string Veresiye { get; init; }
    public required string TahsilatToplam { get; init; }
    public required string FarkTanimi { get; init; }
    public required string BagsizFaturalar { get; init; }
    public required string BagliFaturalar { get; init; }
    public required string FaturaNo { get; init; }
    public required string RaporDuzeltmeYolu { get; init; }

    // ── Vade takibi ──
    public required string VadeAciklamasi { get; init; }
    public required string UfukAciklama { get; init; }
    public required string CariKartinaGit { get; init; }
    public required string TelefonuAra { get; init; }

    // ── Denetim izi ──
    public required string DenetimAciklamasi { get; init; }
    public required string RolEtiketi { get; init; }
    public required string YalnizHassasEtiket { get; init; }
    public required Dictionary<string, string> VarlikAdlari { get; init; }

    // ── Kâr-zarar ──
    public required string MaliyetEksikAria { get; init; }

    /// <summary>Kimlik belgesi kodunu görünen ada çevirir; bilinmeyen kod ham döner.</summary>
    public string BelgeTuruAdi(string? kod) =>
        kod is not null && BelgeTuruAdlari.TryGetValue(kod, out var a) ? a : (kod ?? "");

    /// <summary>Varlık sınıf adını görünen ada çevirir; bilinmeyen ad ham döner.</summary>
    public string VarlikAdi(string? kod) =>
        kod is not null && VarlikAdlari.TryGetValue(kod, out var a) ? a : (kod ?? "");

    /// <summary>Gider pusulası satıcı tipini görünen ada çevirir.</summary>
    public string SaticiTipiAdi(string? kod) => kod switch
    {
        "NIHAI_TUKETICI" => SaticiTipiNihai,
        "ESNAF_MUAF" => SaticiTipiEsnaf,
        "VERGI_MUKELLEFI" => SaticiTipiMukellef,
        _ => kod ?? "",
    };
}


// ═════════════════════════════════════════════════════════════════════════════
// YÖNETİM SÖZLÜĞÜ — UX-1 Faz C, sayfa grubu 4.
// Veri dışa aktarım · Kullanıcılar · Yetkiler · İşletme · Ayarlar · Kılavuz
// ve 45-UI (iki aşamalı giriş) metinleri. Çıplak metin YAZILMAZ (UX-S31);
// kilitli her düğmenin sebebi, düzenlenemeyen her alanın gerekçesi buradadır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SozlukYonetim
{
    // ── Sayfa başı açıklamaları ──
    public required string DisaAktarimAciklama { get; init; }
    public required string KullanicilarAciklama { get; init; }
    public required string YetkilerAciklama { get; init; }
    public required string IsletmeSayfaAciklama { get; init; }
    public required string AyarlarAciklama { get; init; }
    public required string KilavuzSayfaAciklama { get; init; }

    // ── Ortak ──
    public required string KilitSebebi { get; init; }
    public required string Kaydedildi { get; init; }
    public required string ZorunluAlan { get; init; }

    // ── Veri dışa aktarım ──
    public required string GerekceSayac { get; init; }
    public required string GerekceKisa { get; init; }
    public required string KapsamSecilmedi { get; init; }
    public required string OnayMetniEksik { get; init; }
    public required string KisiselVeriVarsayilanNotu { get; init; }
    public required string TumunuSec { get; init; }
    public required string GecmisSiniri { get; init; }

    // ── Kullanıcılar ──
    public required string Siz { get; init; }
    public required string MfaSutun { get; init; }
    public required string MfaVar { get; init; }
    public required string MfaYok { get; init; }
    public required string MfaBilinmiyor { get; init; }
    public required string KendiRolunDegismez { get; init; }
    public required string KendiRolunYolu { get; init; }
    public required string EpostaGecersiz { get; init; }
    public required string AdSoyadBos { get; init; }

    // ── Yetkiler ──
    public required string DegisiklikYok { get; init; }
    public required string KaydedilmemisDegisiklik { get; init; }
    public required string SahipKilitliGerekce { get; init; }
    public required string SahipKilitliYolu { get; init; }

    // ── İşletme ──
    public required string KimlikBaslik { get; init; }
    public required string KimlikAciklama { get; init; }
    public required string KimlikDuzenle { get; init; }
    public required string Adres { get; init; }
    public required string Telefon { get; init; }
    public required string VergiDairesi { get; init; }
    public required string VergiNo { get; init; }
    public required string AdBosOlamaz { get; init; }
    public required string VergiNoGecersiz { get; init; }
    public required string SubeAktifeAl { get; init; }
    public required string SubeAktifeAlmaGerekce { get; init; }
    public required string SubeAktifeAlmaYolu { get; init; }
    public required string KurKaynagi { get; init; }
    public required string MaliyetYontemi { get; init; }
    public required string TuretilenAyarGerekce { get; init; }
    public required string TuretilenAyarYolu { get; init; }
    public required string TelefonGecersiz { get; init; }

    // ── Ayarlar / profil ──
    public required string ProfilDuzenle { get; init; }
    public required string SifreAlanlariBos { get; init; }

    // ── 45-UI · iki aşamalı giriş ──
    public required string MfaBaslik { get; init; }
    public required string MfaAciklama { get; init; }
    public required string MfaDurumEtkin { get; init; }
    public required string MfaDurumKapali { get; init; }
    public required string MfaDurumBekliyor { get; init; }
    public required string MfaKur { get; init; }
    // ── P-09 (B-6) — yarım kalan MFA kurulumu ────────────────────────────
    public required string MfaYarimBaslik { get; init; }
    public required string MfaYarimAciklama { get; init; }
    public required string MfaSifirla { get; init; }
    public required string MfaSifirlandi { get; init; }
    public required string MfaAdim1 { get; init; }
    public required string MfaAdim2 { get; init; }
    public required string MfaAdim3 { get; init; }
    public required string MfaAnahtar { get; init; }
    public required string MfaAnahtarGizli { get; init; }
    public required string MfaAnahtarGoster { get; init; }
    public required string MfaAnahtarGizle { get; init; }
    public required string MfaOtpauth { get; init; }
    public required string MfaKod { get; init; }
    public required string MfaKodIpucu { get; init; }
    public required string MfaDogrula { get; init; }
    public required string MfaKurtarmaBaslik { get; init; }
    public required string MfaKurtarmaUyari { get; init; }
    public required string MfaKurtarmaIndir { get; init; }
    public required string MfaKurtarmaOnay { get; init; }
    public required string MfaTamamla { get; init; }
    public required string MfaKuruldu { get; init; }
    public required string MfaKapat { get; init; }
    public required string MfaKapatAciklama { get; init; }
    public required string MfaKapatOnay { get; init; }
    public required string MfaKapatildi { get; init; }
    public required string MfaSifre { get; init; }
    public required string MfaKalanKod { get; init; }
    public required string MfaKurtarmaYenile { get; init; }
    public required string MfaKilitli { get; init; }
    public required string MfaKodEksik { get; init; }
    public required string MfaSifreEksik { get; init; }
    public required string MfaGirisBaslik { get; init; }
    public required string MfaGirisAciklama { get; init; }
    public required string MfaGirisDogrula { get; init; }
    public required string MfaGirisVazgec { get; init; }
    public required string MfaGizUyari { get; init; }
    public required string KurKaynagiSec { get; init; }
    public required string MaliyetYontemiSec { get; init; }
    /// <summary>P-36c/UX-S30: salt okunur kiracıda seçicinin kilit sebebi.</summary>
    public required string SaltOkunurKilit { get; init; }
    public required string MaliyetFifo { get; init; }
    public required string MaliyetLifo { get; init; }
    public required string MaliyetOrtalama { get; init; }
    public required string AyarBelirlenmedi { get; init; }
    public required string MaliyetYontemiNotu { get; init; }
    public required string KurKaynagiNotu { get; init; }
}


/// <summary>
/// PAKET 34 — rapor seti sözlüğü. Rapor ADLARI burada TEK kaynaktan gelir;
/// Rapor Merkezi kartları ve ekran başlıkları aynı diziden beslenir (UX-S31).
/// Eksik anahtar DERLEME HATASI verir (required init).
/// </summary>
public sealed class SozlukRaporSeti
{
    public required string Merkez { get; init; }
    public required string MerkezAciklama { get; init; }
    public required string RaporSayisi { get; init; }
    public required string GrupGunluk { get; init; }
    public required string GrupSatis { get; init; }
    public required string GrupStok { get; init; }
    public required string GrupMali { get; init; }
    public required string AdGunluk { get; init; }
    public required string AdAylik { get; init; }
    public required string AdPosMutabakat { get; init; }
    public required string AdKasaDefteri { get; init; }
    public required string AdIslemDefteri { get; init; }
    public required string AdOdemeAraci { get; init; }
    public required string AdPersonel { get; init; }
    public required string AdSubeKarsilastirma { get; init; }
    public required string AdStokDegeri { get; init; }
    public required string AdReyonStok { get; init; }
    public required string AdStokDegerlemesi { get; init; }
    public required string AdCariYaslandirma { get; init; }
    public required string AdVadeTakibi { get; init; }
    public required string AdKarZarar { get; init; }
    public required string AdKarZararUrun { get; init; }
    public required string Tarih { get; init; }
    public required string Tur { get; init; }
    public required string Yon { get; init; }
    public required string Giris { get; init; }
    public required string Cikis { get; init; }
    // P-48 — yön türetimi: KARIŞIK (TL ve has zıt işaretli) ve YOK (iki bacak sıfır).
    public required string YonKarisik { get; init; }
    public required string YonYok { get; init; }
    public required string YonYokIpucu { get; init; }
    public required string YonTlBacagi { get; init; }
    public required string YonHasBacagi { get; init; }
    public required string TlTutar { get; init; }
    public required string HasGram { get; init; }
    public required string OdemeTuru { get; init; }
    public required string NakitPayi { get; init; }
    public required string VeresiyePayi { get; init; }
    public required string Fis { get; init; }
    public required string FaturaNo { get; init; }
    public required string CariAd { get; init; }
    public required string UrunAd { get; init; }
    public required string SubeAd { get; init; }
    public required string KaydedenKullanici { get; init; }
    public required string KaydedenUyarisi { get; init; }
    public required string Storno { get; init; }
    public required string StornoAdet { get; init; }
    public required string StornoAciklama { get; init; }
    public required string Bilinmiyor { get; init; }
    public required string BilinmiyorAciklama { get; init; }
    public required string Devir { get; init; }
    public required string Kapanis { get; init; }
    public required string SayilanNakit { get; init; }
    public required string Fark { get; init; }
    public required string DurumAcik { get; init; }
    public required string DurumKapali { get; init; }
    public required string DurumYenidenAcildi { get; init; }

    // PAKET P-22 / B-45 — gün sonu kasa sayımı kasa defterinden girilir.
    public required string SayimGir { get; init; }
    public required string SayimBaslik { get; init; }
    public required string SayimYalnizBugun { get; init; }
    public required string SayimNegatif { get; init; }
    public required string SayimKaydedildi { get; init; }
    public required string SayimKaydedildiFark { get; init; }
    public required string DefterBakiye { get; init; }
    public required string FarkGerekcesi { get; init; }
    public required string HareketSayisi { get; init; }
    public required string Arac { get; init; }
    public required string KasaSecimi { get; init; }
    public required string TumKasalar { get; init; }
    public required string TumTurler { get; init; }
    public required string TumSubeler { get; init; }
    public required string TumKullanicilar { get; init; }
    public required string TumAraclar { get; init; }
    public required string IslemSayisi { get; init; }
    public required string Ciro { get; init; }
    public required string OrtalamaSepet { get; init; }
    public required string IadeAdet { get; init; }
    public required string IadeOrani { get; init; }
    public required string TransferSutun { get; init; }
    public required string TransferNotu { get; init; }
    public required string Yazdir { get; init; }
    public required string Onceki { get; init; }
    public required string Sonraki { get; init; }
    public required string SayfaBilgisi { get; init; }
    public required string SinirUyarisi { get; init; }
    public required string BosDurum { get; init; }
    public required string DonemBagimsiz { get; init; }
    public required string Guvenilirlik { get; init; }
    public required string GuvenilirlikTam { get; init; }
    public required string GuvenilirlikYuksek { get; init; }
    public required string GuvenilirlikOrta { get; init; }
    public required string GuvenilirlikDusuk { get; init; }
    public required string GuvenilirlikVeriYok { get; init; }
    public required string MalFarki { get; init; }
    public required string HurdaTl { get; init; }
    public required string HurdaHas { get; init; }
    public required string AlisTl { get; init; }
    public required string SatisAdet { get; init; }
    public required string Baslangic { get; init; }
    public required string Bitis { get; init; }
    public required string Uygula { get; init; }
    public required string VeresiyeDogan { get; init; }
    public required string VeresiyeKapanan { get; init; }
    public required string Durum { get; init; }
    public required string ParaBirimi { get; init; }

    // ── P-25 / Y-16 — R15 STOK YAŞLANDIRMA + DEVİR HIZI ─────────────────────
    // ⚠ Alanlar `required`: yeni bir dil eklenirse ya da anahtar unutulursa
    // DERLEME HATASI olur (UX-S31 — eksik anahtar sessizce ham kod basmaz).
    public required string AdStokYaslandirma { get; init; }
    public required string YaslandirmaAciklama { get; init; }
    public required string Kova0_30 { get; init; }
    public required string Kova31_90 { get; init; }
    public required string Kova91_180 { get; init; }
    public required string Kova181_365 { get; init; }
    public required string Kova365Ustu { get; init; }
    public required string YasiBilinmeyen { get; init; }
    public required string YasiBilinmeyenAciklama { get; init; }
    public required string DevirHizi { get; init; }
    public required string DevirPenceresi { get; init; }
    public required string DevirTanimi { get; init; }
    public required string YillikTahmini { get; init; }
    public required string OrtalamaRafOmru { get; init; }
    public required string OluStok { get; init; }
    public required string OluEsik { get; init; }
    public required string TuketilenAdet { get; init; }
    public required string KalanAdet { get; init; }
    public required string MaliyetiBilinmeyenAdet { get; init; }
    public required string EnEskiGiris { get; init; }
    public required string YasGun { get; init; }
    public required string Hesaplanamadi { get; init; }
    public required string Adet { get; init; }

    /// <summary>Yaş kovası kodundan etiket. Ham kod ekrana BASILMAZ (UX-S31).</summary>
    public string Kova(string kod) => kod switch
    {
        "G0_30" => Kova0_30,
        "G31_90" => Kova31_90,
        "G91_180" => Kova91_180,
        "G181_365" => Kova181_365,
        "G365_USTU" => Kova365Ustu,
        _ => kod,
    };

    /// <summary>Katalog kodundan (R01…) rapor adı. Bilinmeyen kod ham DÖNMEZ, kodun kendisi döner.</summary>
    public string Ad(string kod) => kod switch
    {
        "R01" => AdGunluk,
        "R02" => AdAylik,
        "R03" => AdCariYaslandirma,
        "R04" => AdVadeTakibi,
        "R05" => AdPosMutabakat,
        "R06" => AdStokDegeri,
        "R07" => AdKarZarar,
        "R08" => AdKarZararUrun,
        "R09" => AdStokDegerlemesi,
        "R10" => AdIslemDefteri,
        "R11" => AdKasaDefteri,
        "R12" => AdOdemeAraci,
        "R13" => AdPersonel,
        "R14" => AdSubeKarsilastirma,
        "R15" => AdStokYaslandirma,
        "R20" => AdReyonStok,
        _ => kod,
    };

    /// <summary>Katalog grubundan bölüm başlığı.</summary>
    public string Grup(string grup) => grup switch
    {
        "GUNLUK" => GrupGunluk,
        "SATIS" => GrupSatis,
        "STOK" => GrupStok,
        "MALI" => GrupMali,
        _ => grup,
    };
}

/// <summary>Paket T-3 Faz A — yedek otomasyonu ekranı ("Yedekler").</summary>
/// <summary>
/// Sistem künyesi ve destek kodu (paket T-4a). Ham kod/dizge ekrana basılmaz;
/// her etiket buradan çözülür (UX-S31).
/// </summary>
public sealed class SozlukKunye
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    public required string Surum { get; init; }
    public required string Derleme { get; init; }
    public required string Ortam { get; init; }
    public required string OrtamUretim { get; init; }
    public required string OrtamGelistirme { get; init; }
    public required string MigrationSayisi { get; init; }
    public required string SonMigration { get; init; }
    public required string BekleyenMigration { get; init; }
    public required string SemaGuncel { get; init; }
    public required string SemaEski { get; init; }
    public required string Kopyala { get; init; }
    public required string Kopyalandi { get; init; }
    public required string KopyalanamadiElle { get; init; }
    public required string DestekKodu { get; init; }
    public required string Ayrinti { get; init; }
    public required string AyrintiSahip { get; init; }
    public required string DerlemeKaynagiDamga { get; init; }
    public required string DerlemeKaynagiDosya { get; init; }
    public required string Yuklenemedi { get; init; }
}

public sealed class SozlukYedek
{
    public required string Baslik { get; init; }
    public required string Aciklama { get; init; }
    /// <summary>Mehmet koşulu: yedek TÜM kiracıları kapsar — ekranda yazılı olur.</summary>
    public required string SistemGeneliUyari { get; init; }
    public required string AnahtarUyari { get; init; }
    public required string SonYedek { get; init; }
    public required string Boyut { get; init; }
    public required string Dogrulama { get; init; }
    public required string DisKopya { get; init; }
    public required string RpoRto { get; init; }
    public required string RpoOlculen { get; init; }
    public required string RpoTaahhut { get; init; }
    public required string RtoTaahhut { get; init; }
    public required string RtoOlculen { get; init; }
    public required string Olculmedi { get; init; }
    public required string DurumBekliyor { get; init; }
    public required string DurumDogrulandi { get; init; }
    public required string DurumUyusmadi { get; init; }
    public required string KopyaYok { get; init; }
    public required string KopyaVar { get; init; }
    public required string KopyaHata { get; init; }
    public required string DogrulanmamisUyari { get; init; }
    public required string Kosular { get; init; }
    public required string KosuBos { get; init; }
    public required string Tatbikatlar { get; init; }
    public required string TatbikatBos { get; init; }
    public required string TatbikatAciklama { get; init; }
    public required string Uyusan { get; init; }
    public required string Uyusmayan { get; init; }
    public required string SureSn { get; init; }
    public required string SemaDusuruldu { get; init; }
    public required string Tur { get; init; }
    public required string TabloSayisi { get; init; }
    public required string ElleYedek { get; init; }
    public required string ElleYedekAciklama { get; init; }
    public required string Gerekce { get; init; }
    public required string GerekceOrnek { get; init; }
    public required string TalepGonder { get; init; }
    public required string Talepler { get; init; }
    public required string TalepBos { get; init; }
    public required string TalepAcildi { get; init; }
    public required string IptalEt { get; init; }
    public required string IptalGerekce { get; init; }
    public required string IptalOnayBaslik { get; init; }
    public required string IptalOnayMetin { get; init; }
    public required string IptalEdildi { get; init; }
    public required string IndirmeYok { get; init; }
    public required string SaklamaNotu { get; init; }
    public required string Sifreli { get; init; }
    public required string SifreliDegil { get; init; }
    public required string IptalGerekceEtiket { get; init; }
}

/// <summary>
/// PAKET P-19 (Y-11/Y-12) — gider/masraf ve virman ekranı sözlüğü.
/// UX-S31: ham kod (KASA/BANKA) ekrana basılmaz; buradan çevrilir.
/// </summary>
public sealed class SozlukGider
{
    public required string Baslik { get; init; }
    public required string SayfaAciklama { get; init; }
    public required string SekmeGiderler { get; init; }
    public required string SekmeKategoriler { get; init; }
    public required string SekmeVirman { get; init; }
    // ── kategori
    public required string Kategori { get; init; }
    public required string KategoriAdi { get; init; }
    public required string YeniKategori { get; init; }
    public required string KategoriYok { get; init; }
    public required string KategoriKaydedildi { get; init; }
    public required string KategoriGuncellendi { get; init; }
    public required string KategoriDuzenle { get; init; }
    public required string KategoriPasifeAl { get; init; }
    public required string KategoriAktifeAl { get; init; }
    public required string KategoriPasifNotu { get; init; }
    public required string KategoriSilinmez { get; init; }
    public required string KullanimAdedi { get; init; }
    public required string KategoriToplami { get; init; }
    // ── gider
    public required string YeniGider { get; init; }
    public required string GiderKaydet { get; init; }
    public required string GiderKaydedildi { get; init; }
    public required string GiderYok { get; init; }
    public required string Kasa { get; init; }
    public required string ToplamGider { get; init; }
    public required string Storno { get; init; }
    public required string StornoOnay { get; init; }
    public required string StornoGerekcesi { get; init; }
    public required string Stornolandi { get; init; }
    public required string StornoSatiri { get; init; }
    public required string StornoYapildi { get; init; }
    public required string GiderDegismezNotu { get; init; }
    // ── virman
    public required string VirmanBaslik { get; init; }
    /// <summary>P-36c: virman hedef seçicisinin etiketi.</summary>
    public required string VirmanHedef { get; init; }
    public required string VirmanAciklama { get; init; }
    public required string Kaynak { get; init; }
    public required string Hedef { get; init; }
    public required string VirmanYap { get; init; }
    public required string VirmanKaydedildi { get; init; }
    public required string VirmanYok { get; init; }
    public required string VirmanStornoOnay { get; init; }
    public required string VirmanStornoYapildi { get; init; }
    public required string SubelerArasi { get; init; }
    public required string TurKasa { get; init; }
    public required string TurBanka { get; init; }
    public required string IkiBacakNotu { get; init; }
    // ── kilit sebepleri (UX-S30) ve sınır (UX-S29)
    public required string KilitKategoriSec { get; init; }
    public required string KilitKasaSec { get; init; }
    public required string KilitTutar { get; init; }
    public required string KilitKaynakSec { get; init; }
    public required string KilitHedefSec { get; init; }
    public required string KilitAyniTaraf { get; init; }
    public required string KilitGerekce { get; init; }
    public required string DahaFazlasiVar { get; init; }
    // ── PAKET P-50: negatif kasadan çıkış yolu (bakiye rozeti + eylem bağlantılı bant)
    public required string KasaBakiyesi { get; init; }
    public required string KasaBakiyeEksi { get; init; }
    /// <summary>{0} = biçimlendirilmiş bakiye.</summary>
    public required string KilitBakiyeYetersiz { get; init; }
    /// <summary>{0} = kasa adı · {1} = mevcut bakiye · {2} = istenen tutar (hepsi biçimlendirilmiş).</summary>
    public required string YetersizBakiye { get; init; }
    public required string YetersizBakiyeGenel { get; init; }
    public required string KasayaGirisYap { get; init; }
    public required string VirmanYapBaglanti { get; init; }
    /// <summary>{0} = biçimlendirilmiş eksi bakiye.</summary>
    public required string EksiKasaUyari { get; init; }
    public required string KasaRaporunaGit { get; init; }
}

/// <summary>PAKET P-20 (Y-10) — hurda stok / ramat çıkışı ekranı.</summary>
public sealed class SozlukHurda
{
    public required string Baslik { get; init; }
    public required string SayfaAciklama { get; init; }
    public required string BakiyeBasligi { get; init; }
    public required string BakiyeYok { get; init; }
    public required string BakiyeKaynagi { get; init; }
    public required string HasGram { get; init; }
    public required string GirenHas { get; init; }
    public required string CikanHas { get; init; }
    public required string Maden { get; init; }
    public required string YeniSatis { get; init; }
    public required string CikanHasGram { get; init; }
    public required string BirimFiyat { get; init; }
    public required string FiyatKurdan { get; init; }
    public required string FiyatElle { get; init; }
    public required string Ramatci { get; init; }
    public required string Kasa { get; init; }
    public required string OdemeTuru { get; init; }
    public required string TahsilEdilen { get; init; }
    public required string Tutar { get; init; }
    public required string Kaydet { get; init; }
    public required string Kaydedildi { get; init; }
    public required string KalanBakiye { get; init; }
    public required string DefterBasligi { get; init; }
    public required string DefterYok { get; init; }
    public required string FireNotuBasligi { get; init; }
    public required string DegismezNotu { get; init; }
    public required string KilitKurYok { get; init; }
    public required string KilitBakiyeYok { get; init; }
    public required string KilitKasaYok { get; init; }
    public required string TurAlis { get; init; }
    public required string TurSatis { get; init; }
    public required string TurBasligi { get; init; }
    public required string AracNakit { get; init; }
    public required string AracKart { get; init; }
    public required string AracHavale { get; init; }
    // ── PAKET P-20b (K-13/2) — TAHLİL / MUTABAKAT FARKI ──────────────────────
    // ⚠ Hiçbiri "fire" demez: eritme firesi AYRI bir kavramdır ve 1.0'a girmez
    //   (K-13/1). Aynı kavram tek ad, tek kaynak (UX-S31).
    public required string BizimKaydimiz { get; init; }
    public required string AliciKabul { get; init; }
    public required string AliciKabulNotu { get; init; }
    public required string TahlilFarki { get; init; }
    public required string TahlilFarkiGerekce { get; init; }
    public required string TahlilFarkiNotu { get; init; }
    public required string FarkLehte { get; init; }
    public required string FarkAleyhte { get; init; }
    public required string FarkYok { get; init; }
    public required string TurTahlilFarki { get; init; }
    public required string ToplamFark { get; init; }
    public required string Duzelt { get; init; }
    public required string DuzeltBaslik { get; init; }
    public required string EkFark { get; init; }
    public required string DuzeltmeKaydedildi { get; init; }
    public required string DuzeltmeNotu { get; init; }
    public required string KilitGerekce { get; init; }
    public required string KilitFarkSifir { get; init; }
}

public sealed class Sozluk
{
    /// <summary>
    /// PAKET DIL-EKRAN — bu sözlüğün dili ("tr" | "en"). BASİT mod katmanı sözlüğü KOPYALAR
    /// (kimlik değişir); dil bu alandan okunur, <c>ReferenceEquals(t, Sozlukler.En)</c> ile ASLA.
    /// </summary>
    public string DilKodu { get; init; } = "tr";
    public required SozlukOrtak Ortak { get; init; }
    public required SozlukNav Nav { get; init; }
    public required SozlukGider Gider { get; init; }
    public required SozlukHurda Hurda { get; init; }
    public required SozlukAuth Auth { get; init; }
    public required SozlukPanel Panel { get; init; }
    public required SozlukCari Cari { get; init; }
    public required SozlukUrun Urun { get; init; }
    public required SozlukIslem Islem { get; init; }
    public required SozlukPos Pos { get; init; }
    public required SozlukKasa Kasa { get; init; }
    public required SozlukRapor Rapor { get; init; }
    /// <summary>Paket 34 — rapor seti.</summary>
    public required SozlukRaporSeti RaporSeti { get; init; }
    public required SozlukBanka Banka { get; init; }
    public required SozlukFatura Fatura { get; init; }
    public required SozlukAyarlar Ayarlar { get; init; }
    public required SozlukKabuk Kabuk { get; init; }
    public required SozlukTema Tema { get; init; }
    public required SozlukForm Form { get; init; }
    public required SozlukGorunum Gorunum { get; init; }
    public required SozlukArayuz Arayuz { get; init; }
    public required SozlukKur Kur { get; init; }
    public required SozlukKullanici Kullanici { get; init; }
    public required SozlukYetki Yetki { get; init; }
    public required SozlukDenetim Denetim { get; init; }
    public required SozlukUyum Uyum { get; init; }
    public required SozlukPusula Pusula { get; init; }
    // PAKET P-17 — Y-6 ortak A4 çıktı · Y-7 vitrin panosu.
    public required SozlukCikti Cikti { get; init; }
    public required SozlukPano Pano { get; init; }
    public required SozlukMutabakat Mutabakat { get; init; }
    public required SozlukVade Vade { get; init; }
    public required SozlukFiyat Fiyat { get; init; }
    public required SozlukSayim Sayim { get; init; }
    public required SozlukDisaAktarim DisaAktarim { get; init; }
    /// <summary>Paket 47 Faz A — veri içe aktarımı.</summary>
    public required SozlukIceAktarim IceAktarim { get; init; }
    /// <summary>Paket T-2 Faz A — kuruluş sihirbazı ve açılış bakiyeleri.</summary>
    public required SozlukKurulum Kurulum { get; init; }
    public required SozlukEtiket Etiket { get; init; }
    public required SozlukKarZarar KarZarar { get; init; }
    public required SozlukUrunAgaci UrunAgaci { get; init; }
    public required SozlukYerlesim Yerlesim { get; init; }
    public required SozlukEmanet Emanet { get; init; }
    public required SozlukIsletme Isletme { get; init; }
    public required SozlukBelge Belge { get; init; }
    public required SozlukDuzeltme Duzeltme { get; init; }
    public required SozlukSayfaDuzeltme SayfaDuzeltme { get; init; }
    public required SozlukBildirim Bildirim { get; init; }
    public required SozlukSiparis Siparis { get; init; }
    /// <summary>Paket 29 Faz A — çek/senet portföyü.</summary>
    public required SozlukEvrak Evrak { get; init; }
    /// <summary>Paket 19 Faz A — vezne.</summary>
    public required SozlukVezne Vezne { get; init; }
    public required SozlukYonetim Yonetim { get; init; }
    /// <summary>Paket T-3 Faz A — yedek otomasyonu.</summary>
    public required SozlukYedek Yedek { get; init; }

    /// <summary>Paket T-4a — sistem künyesi / destek kodu.</summary>
    public required SozlukKunye Kunye { get; init; }
    public required string[] Aylar { get; init; }
    public required string[] AylarKisa { get; init; }
}

public static class Sozlukler
{
    public static readonly Sozluk Tr = new()
    {
        Ortak = new()
        {
            Kaydet = "Kaydet",
            Durum = "Durum",
            Kaydediliyor = "Kaydediliyor…",
            Iptal = "İptal",
            Sil = "Sil",
            Duzenle = "Düzenle",
            Ekle = "Ekle",
            Ara = "Ara",
            Istege = "isteğe bağlı",
            YeniSekmedeAc = "Kılavuzu yeni sekmede aç",
            KilavuzAciklama = "Kullanım kılavuzu ayrı bir sayfada açılır. Güvenlik ayarları (çerçeveye gömülmeye karşı koruma) kılavuzun uygulama içine gömülmesine izin vermez; aşağıdaki başlıklardan doğrudan ilgili bölüme gidebilirsiniz.",
            KilavuzBolumler = "Bölümler",
            OturumSuresiDoldu = "Oturum süresi doldu — lütfen tekrar giriş yapın.",
            SozlesmesizKaynakUyari = "Bu kur kaynağı sözleşmesizdir — yalnız pilot kullanım içindir. Ticari kullanımda kaynak sağlayıcıyla yazılı izin/abonelik gerekir.",
            DisKaynakKapali = "Dış kur kaynakları kapalı. Kuru aşağıdaki manuel giriş formundan girin.",
            Geri = "Geri",
            Tumu = "Tümü",
            Detay = "Detay",
            Kapat = "Kapat",
            Evet = "Evet",
            Hayir = "Hayır",
            Aktif = "Aktif",
            Pasif = "Pasif",
            Yukleniyor = "Yükleniyor...",
            Baslangic = "Başlangıç",
            Bitis = "Bitiş",
            Hata = "Bir hata oluştu",
            Basarili = "İşlem başarılı",
            Bulunamadi = "Kayıt bulunamadı",
            Aciklama = "Açıklama",
            Opsiyonel = "opsiyonel",
            Toplam = "Toplam",
            Tarih = "Tarih",
            Tutar = "Tutar",
            Adet = "Adet",
            Degistir = "Değiştir",
            CikisYap = "Çıkış",
            Gecmis = "Geçmiş",
            Eylemler = "Eylemler",
            Yenile = "Yenile",
            DegerYok = "değer yok",
        },
        Gider = new()
        {
            Baslik = "Gider ve Virman",
            SayfaAciklama = "İşletmenin masrafları ve kasa/banka arası para aktarımları. Virman ciroya girmez: para işletmeden çıkmaz, yer değiştirir.",
            SekmeGiderler = "Giderler",
            SekmeKategoriler = "Gider Kategorileri",
            SekmeVirman = "Virman",
            Kategori = "Kategori",
            KategoriAdi = "Kategori adı",
            YeniKategori = "Yeni kategori",
            KategoriYok = "Henüz gider kategorisi yok. Önce bir kategori açın (kira, elektrik, personel…).",
            KategoriKaydedildi = "Gider kategorisi açıldı",
            KategoriGuncellendi = "Gider kategorisi güncellendi",
            KategoriDuzenle = "Kategoriyi düzenle",
            KategoriPasifeAl = "Pasife al",
            KategoriAktifeAl = "Aktife al",
            KategoriPasifNotu = "Pasif kategoriye yeni gider yazılamaz; geçmiş giderlerin kategorisi yerinde kalır.",
            KategoriSilinmez = "Gider kategorisi SİLİNMEZ; pasife alınır. Geçmiş gider satırlarının sınıfı kaybolmasın diye.",
            KullanimAdedi = "Gider adedi",
            KategoriToplami = "Kategori toplamı",
            YeniGider = "Yeni gider",
            GiderKaydet = "Gideri kaydet",
            GiderKaydedildi = "Gider kaydedildi",
            GiderYok = "Seçilen aralıkta gider kaydı yok.",
            Kasa = "Kasa",
            ToplamGider = "Toplam gider",
            Storno = "Storno",
            StornoOnay = "Bu gider kaydı ters bir satırla hükümsüz kılınacak. Kayıt silinmez, geçmişte görünmeye devam eder. Onaylıyor musunuz?",
            StornoGerekcesi = "Storno gerekçesi (en az 20 karakter)",
            Stornolandi = "Stornolandı",
            StornoSatiri = "Storno satırı",
            StornoYapildi = "Gider stornolandı",
            GiderDegismezNotu = "Gider satırının tutarı yerinde DEĞİŞMEZ; düzeltme yolu STORNO + yeni kayıttır.",
            VirmanHedef = "Hedef",
            VirmanBaslik = "Virman (kasa ↔ banka aktarımı)",
            VirmanAciklama = "Para işletmenin içinde yer değiştirir: iki bacak tek işlemde yazılır, ciroya ve gidere girmez.",
            Kaynak = "Kaynak",
            Hedef = "Hedef",
            VirmanYap = "Virmanı kaydet",
            VirmanKaydedildi = "Virman kaydedildi",
            VirmanYok = "Henüz virman kaydı yok.",
            VirmanStornoOnay = "Virmanın İKİ BACAĞI birlikte geri alınacak. Onaylıyor musunuz?",
            VirmanStornoYapildi = "Virman stornolandı (iki bacak birlikte)",
            SubelerArasi = "Şubeler arası",
            TurKasa = "Kasa",
            TurBanka = "Banka hesabı",
            IkiBacakNotu = "Virmanın tek bacağı stornolanamaz; iki bacak birlikte döner.",
            KilitKategoriSec = "Gider kategorisi seçilmedi",
            KilitKasaSec = "Kasa seçilmedi",
            KilitTutar = "Tutar sıfırdan büyük olmalı",
            KilitKaynakSec = "Kaynak seçilmedi",
            KilitHedefSec = "Hedef seçilmedi",
            KilitAyniTaraf = "Kaynak ve hedef aynı olamaz",
            KilitGerekce = "Gerekçe en az 20 karakter olmalı",
            DahaFazlasiVar = "Sınıra değildi — daha fazla kayıt var; tarih aralığını daraltın.",
            KasaBakiyesi = "Kasa bakiyesi",
            KasaBakiyeEksi = "Eksi bakiye",
            KilitBakiyeYetersiz = "Tutar kasa bakiyesini aşıyor (kasada {0} var)",
            YetersizBakiye = "'{0}' kasasında yeterli para yok: kasada {1} var, {2} isteniyor. Nakit kasa eksiye düşemez.",
            YetersizBakiyeGenel = "Kasada yeterli para yok. Nakit kasa eksiye düşemez.",
            KasayaGirisYap = "Kasaya giriş yap",
            VirmanYapBaglanti = "Virman yap",
            EksiKasaUyari = "Bu kasanın bakiyesi eksi ({0}). Gider yazmadan önce kasaya giriş yapın ya da virmanla para aktarın.",
            KasaRaporunaGit = "Kasa raporunu aç",
        },
        Hurda = new()
        {
            Baslik = "Hurda Stoğu ve Ramat Çıkışı",
            SayfaAciklama = "İşletmede biriken hurdanın has bakiyesi ve rafineriye (ramatçıya) çıkışı. Bakiye ayrı bir kolondan değil, HURDA ALIŞ ve HURDA SATIŞ satırlarından türetilir.",
            BakiyeBasligi = "Elimizdeki hurda",
            BakiyeYok = "Bu şubede hurda hareketi yok: henüz hurda alışı girilmemiş.",
            BakiyeKaynagi = "Defterden türetildi",
            HasGram = "Has gram",
            GirenHas = "Giren has",
            CikanHas = "Çıkan has",
            Maden = "Maden",
            YeniSatis = "Ramata çıkış (hurda satışı)",
            CikanHasGram = "Çıkan has gram",
            BirimFiyat = "Birim fiyat (TL / has gram)",
            FiyatKurdan = "Kurdan geldi",
            FiyatElle = "Elle girildi",
            Ramatci = "Ramatçı / rafineri carisi",
            Kasa = "Tahsilat kasası",
            OdemeTuru = "Tahsil aracı",
            TahsilEdilen = "Tahsil edilen (TL)",
            Tutar = "Tutar",
            Kaydet = "Ramat çıkışını kaydet",
            Kaydedildi = "Hurda satışı kaydedildi",
            KalanBakiye = "Kalan hurda",
            DefterBasligi = "Hurda defteri",
            DefterYok = "Bu şubede hurda hareketi yok.",
            FireNotuBasligi = "Fire / ramat farkı",
            DegismezNotu = "Hurda satışı yerinde DEĞİŞMEZ ve bu sürümde stornolanmaz: hurdanın stok satırı yoktur, ters kayıt motoru bu hattı tanımaz. Yanlış çıkış, doğrusu girilerek değil, ramatçıyla mutabakat kaydıyla düzeltilir; düzeltme yolu açılana kadar kayıt olduğu gibi durur.",
            KilitKurYok = "Kur kaydı gelmedi (birkaç saniye bekleyin)",
            KilitBakiyeYok = "Bu madende hurda bakiyesi yok",
            KilitKasaYok = "Tahsilat kasası seçilmedi",
            TurAlis = "Hurda alış",
            TurSatis = "Hurda satış",
            TurBasligi = "Tür",
            AracNakit = "Nakit",
            AracKart = "Kart",
            AracHavale = "Havale",
            BizimKaydimiz = "Bizim kaydımız (has gram)",
            AliciKabul = "Alıcının kabul ettiği has gram",
            AliciKabulNotu = "Rafinerinin/toptancının tahlil raporundaki has. Boş bırakılırsa kaydımız aynen kabul edilmiş sayılır ve fark bacağı yazılmaz.",
            TahlilFarki = "Tahlil farkı",
            TahlilFarkiGerekce = "Tahlil farkının gerekçesi",
            TahlilFarkiNotu = "Fark ayrı satıra yazılır, para hareketi yapmaz. Eritme firesi DEĞİLDİR: alıcının tahlil ölçümüyle bizim kaydımız arasındaki farktır.",
            FarkLehte = "Lehimize",
            FarkAleyhte = "Aleyhimize",
            FarkYok = "Fark yok: alıcı kaydımızı aynen kabul etti.",
            TurTahlilFarki = "Tahlil farkı",
            ToplamFark = "Toplam tahlil farkı",
            Duzelt = "Tahlil farkını düzelt",
            DuzeltBaslik = "Tahlil farkı düzeltmesi",
            EkFark = "Ek fark (has gram; eksi = aleyhimize)",
            DuzeltmeKaydedildi = "Tahlil farkı düzeltmesi kaydedildi",
            DuzeltmeNotu = "Defter satırının üzerine yazılmaz: aynı hurda satışına ikinci bir fark bacağı eklenir, toplam fark bacakların toplamıdır. Tahsilat tutarı bu yolla değişmez; para tarafı için cari düzeltme fişini kullanın.",
            KilitGerekce = "Gerekçe en az 20 karakter olmalı",
            KilitFarkSifir = "Ek fark sıfır olamaz",
        },
        Nav = new()
        {
            Panel = "Panel",
            Cariler = "Cariler",
            Urunler = "Ürünler",
            Islemler = "İşlemler",
            Kasalar = "Kasalar",
            Pos = "POS Cihazları",
            PosTahsilat = "POS Hakedişleri",
            Banka = "Banka",
            Faturalar = "Faturalar",
            Raporlar = "Raporlar",
            IslemDefteri = "Hareket Dökümü",
            SatisAnalizi = "Satış analizi",
            KasaRaporu = "Kasa Hareketleri",
            Kullanicilar = "Kullanıcılar",
            Yetkiler = "Yetkiler",
            DenetimIzi = "Denetim İzi",
            VeriDisaAktarim = "Veri dışa aktar",
            VeriIceAktarim = "Veri İçe Aktarımı",
            VeriAktarimi = "Veri Aktarımı",
            Kurulum = "Kuruluş Sihirbazı",
            Etiketler = "Ürün Etiketleri",
            UrunAgaci = "Ürün Ağacı",
            Emanet = "Emanet",
            Isletme = "İşletme",
            Kur = "Kur",
            KarZarar = "Kâr-zarar",
            VadeTakibi = "Gecikmiş Alacaklar",
            Fiyatlandirma = "Fiyat Listeleri",
            Sayim = "Sayım",
            Uyum = "Uyum",
            GiderPusulalari = "Gider Pusulaları",
            Gider = "Gider ve Virman",
            Hurda = "Hurda ve Ramat",
            Kilavuz = "Kılavuz",
            Ayarlar = "Ayarlar",
            Bildirimler = "Bildirimler",
            Siparisler = "Siparişler",
            Vezne = "Vezne",
            PusulaBelgesi = "Gider Pusulası Belgesi",
            Giris = "Giriş",
            Bulunamadi = "Sayfa bulunamadı",
            Yedekler = "Yedekler",
            Evraklar = "Çek / Senet",
            EvrakTakvimi = "Vade Takvimi",
            // Paket P-14 (Y-9) — grup başlıkları.
            GrupGunluk = "Günlük İş",
            GrupMusteriStok = "Müşteri ve Stok",
            GrupParaBanka = "Para ve Banka",
            GrupBelgeUyum = "Belge ve Uyum",
            GrupRapor = "Dökümler",
            GrupYonetim = "Yönetim",
            MenuEtiketi = "Ana menü",
        },
        Auth = new()
        {
            UygulamaAdi = Marka.Ad,
            Slogan = "Kuyumculuk Yönetim Sistemi",
            GirisYap = "Giriş Yap",
            IsletmeKur = "İşletme Kur",
            Eposta = "E-posta",
            Sifre = "Şifre",
            SifreTekrar = "Yeni Şifre (Tekrar)",
            IsletmeAdi = "İşletme Adı",
            Ad = "Ad",
            Soyad = "Soyad",
            AdSoyad = "Ad Soyad",
            Rol = "Rol",
            GirisBasarisiz = "Giriş başarısız",
            KayitBasarisiz = "Kayıt başarısız",
            DavetJetonu = "Davet kodu",
            DavetAciklama = "İşletme kaydı yalnız davetle açılır. Kodunuz yoksa satış ekibimizden isteyin.",
            DavetPlaceholder = "Size iletilen davet kodunu yapıştırın",
            SifrePlaceholder = "En az 8 karakter",
            Hosgeldin = "Hoş geldin,",
            PanelBaslik = "Kuyumunuz güvenli ellerde",
            PanelAlt = "Satıştan stoğa, kasadan faturaya — işletmenizin tamamı tek ekranda.",
            Ozellik1 = "TL + has gram paralel defter",
            Ozellik2 = "Canlı altın kuru takibi",
            Ozellik3 = "Çoklu şube, kasa ve banka yönetimi",
            KisitBaslik = "Bu kurulumda HARİÇ olanlar",
            KisitYukleniyor = "Kısıt listesi alınıyor…",
            KisitBos = "Bu kurulum için ilan edilmiş kısıt yok.",
            KisitKaynak = "Tek kaynak:",
            KisitHatasi = "Kısıt listesi alınamadı — liste boş değil, okunamadı. Sayfayı yenileyin.",
            KisitDurumHaric = "HARİÇ",
            KisitDurumKapsamDisi = "KAPSAM DIŞI",
            KisitDurumSurum11 = "1.1",
            MfaBaslik = "İki aşamalı giriş",
            MfaAciklama = "Şifrenizin yanında telefonunuzdaki doğrulayıcı uygulamanın ürettiği 6 haneli kod istenir.",
            MfaKur = "Kur",
            MfaKapat = "Kapat",
            MfaKod = "Doğrulama kodu",
            MfaKodPlaceholder = "6 haneli kod",
            MfaKurtarmaKodu = "Kurtarma kodları",
            MfaKurtarmaUyari = "Bu kodlar bir daha gösterilmeyecek. İndirin ya da yazdırıp güvenli bir yerde saklayın.",
            MfaKurtarmaYenile = "Kodları yenile",
            MfaEtkin = "Etkin",
            MfaPasif = "Pasif",
            MfaGizElle = "Doğrulayıcı uygulamaya elle girilecek anahtar",
            MfaKilitli = "Çok fazla hatalı deneme: hesap geçici olarak kilitli",
            MfaKodHatali = "Doğrulama kodu hatalı",
            MfaKalanKod = "Kalan kurtarma kodu",
            MfaSaatUyarisi = "Telefonunuzun saatinin otomatik ayarlı olduğundan emin olun.",
        },
        Panel = new()
        {
            SelamGunaydin = "Günaydın",
            SelamIyiGunler = "İyi günler",
            SelamIyiAksamlar = "İyi akşamlar",
            SelamIyiGeceler = "İyi geceler",
            HavaBaslik = "Hava",
            HavaAlinamadi = "Hava bilgisi alınamadı",
            HavaKapali = "Hava servisi kapalı (Ayarlar)",
            DuzeniDuzenle = "Kutucukları düzenle",
            DuzenlemeyiBitir = "Düzenlemeyi bitir",
            DuzeniSifirla = "Düzeni sıfırla",
            KutucukGizle = "Kutucuğu gizle",
            KutucukGoster = "Kutucuğu göster",
            KutucukYukari = "Yukarı taşı (Alt + Yukarı)",
            KutucukAsagi = "Aşağı taşı (Alt + Aşağı)",
            GizliKutucuklar = "Gizlenen kutucuklar",
            Tahsilat = "Tahsilat",
            BekleyenYok = "Bekleyen iş yok — bugün temiz.",
            BekleyenRolKapali = "Bekleyen uyum işleri yalnız sahip, müdür ve muhasebe rollerinde görünür.",
            SonIslemYok = "Bugün henüz işlem yok.",
            KasaKaydiYok = "Tanımlı kasa yok.",
            HizliEylemler = "Hızlı eylemler",
            BekleyenIsler = "Bekleyen işler",
            SonIslemler = "Son işlemler",
            DuzenKaydedilemedi = "Karşılama düzeni kaydedilemedi",
            GunlukSatis = "Günlük Satış",
            GunlukAlis = "Günlük Alış",
            NetHasGram = "Net Has Gram",
            NetKasa = "Günün nakit akışı",
            NetKasaFormul = "(satış − alış − hurda)",
            ParalelDefter = "Paralel defter",
            MasakSuresiGecti = "şüpheli işlem bildiriminin 10 iş günlük süresi DOLDU.",
            MasakSuresiYaklasiyor = "şüpheli işlem bildiriminin süresi 3 gün içinde doluyor.",
            PusulaSuresiGecti = "gider pusulasının 7 günlük düzenleme süresi DOLDU.",
            YuksekRiskliCari = "cari yüksek riskli görünüyor (uyarıdır, engel değil)",
            UyumaGit = "Uyum ekranı",
            PusulalaraGit = "Gider pusulaları",
            Son7Gun = "Son 7 Günlük Satış (₺)",
            BugunOzet = "Bugün Özet",
            Satis = "Satış",
            Alis = "Alış",
            Iscilik = "İşçilik",
            HasGram = "Has Gram",
            Kur24k = "Kur (24K Altın)",
            Canli = "Canlı",
            KurTakip = "Header'dan takip edin →",
            Altin24k = "Altın 24K",
            Gram = "gram",
            KurAlis = "Alış",
            KurSatis = "Satış",
            HizliErisim = "Hızlı Erişim",
            SatisYap = "Satış Yap",
            AlisYap = "Alış Yap",
            MusteriBul = "Müşteri Bul",
            KasaOzeti = "Kasa Özeti",
            Aciklama = "Bugünün satış, alış ve kasa özeti.",
            BugunItibariyla = "(bugün itibarıyla — dönemden bağımsız)",
            BasitModNotu = "Basit modda yalnız bugünün üç sayısı gösterilir; ayrıntı için profesyonel moda geçin.",
            BugunOzeti = "Bugün Özeti",
        },
        Cari = new()
        {
            KrediLimiti = "Kredi Limiti",
            Notlar = "Notlar",
            NotlarIpucu = "Bu cariyle ilgili serbest not (en fazla 1000 karakter). Boş bırakılırsa mevcut not silinir.",
            VergiNo = "Vergi No",
            CariDuzenle = "Cari Düzenle",
            PasifeAl = "Pasife Al",
            // ── PAKET 40-A — PEP beyanı ─────────────────────────────────
            PepBeyani = "Siyasi nüfuz sahibi (PEP) beyanı",
            PepGorev = "Beyan edilen görev",
            PepGorevZorunlu = "Beyan işaretliyse görev alanı zorunludur.",
            PepBeyanTarihi = "Beyan tarihi",
            PepNotu = "Bu bir BEYANDIR, tespit değildir: sistem kimsenin siyasi nüfuz sahibi olup olmadığını kendi bilmez, müşterinin beyanını ve beyan anını kaydeder.",
            PepRozet = "PEP",
            RiskSeviyesi = "Risk seviyesi",
            PasifeAlOnay = "Cari pasife alınacak. Geçmiş hareketleri ve bakiyesi korunur, yeni işlemde seçilemez. Onaylıyor musunuz?",
            Aktiflestir = "Aktifleştir",
            Tahsilat = "Tahsilat",
            TahsilatIpucu = "Müşteriden alınan ödeme. Borcu azaltır.",
            Duzeltme = "Düzeltme Fişi", DuzeltmeIpucu = "Bakiyeyi elle düzeltir; deftere cari düzeltme fişi olarak yazılır ve GERİ ALINAMAZ. Gerekçe zorunlu (en az 20 karakter). Pozitif = borç artar.", TlDelta = "TL bakiye farkı (±)", HasDelta = "Has gram farkı (±)", Gerekce = "Gerekçe",
            Veresiye = "Veresiye",
            VeresiyeIpucu = "Müşteriye açılan kredi. Borcu artırır.",
            TlTutar = "Tutar (₺)",
            OdemeTuru = "Ödeme Şekli",
            Nakit = "Nakit",
            Kart = "Kart",
            Havale = "Havale / EFT",
            HasGramOdeme = "Has Gram ile Ödeme",
            HasGramIpucu = "Müşteri altınla ödüyorsa gram girin; yalnız TL ise boş bırakın.",
            HasGramVerilen = "Verilen Has Gram",
            Atolye = "Atölye",
            Sarraf = "Sarraf",
            DovizBakiye = "Döviz Bakiye",
            DovizVeresiye = "Döviz Veresiye",
            DovizTahsilat = "Döviz Tahsilat",
            ParaBirimi = "Para Birimi",
            EfektifMiktar = "Efektif Miktar",
            DovizBorcuVar = "Döviz bakiyesi var (vadelendirilmedi)",
            TlKarsiligiBilgiAmacli = "TL karşılığı (bilgi amaçlı — sunucu hesaplar)",
            KurYokDovizYapilamaz = "Seçilen para biriminin kuru güncel kur kaydında yok — döviz işlemi yapılamaz.",
            DovizVadesizUyari = "Döviz bakiyesinin vadesi ve yaşlandırması bu sürümde YOKTUR; gecikme listesinde görünmez.",
            DovizFisBacagiUyari = "Fişteki döviz bacağı bir ÖDEMEDİR, cariye döviz borcu bırakmaz. Döviz borcu yalnız bu formdan doğar.",
            DovizDelta = "Döviz bakiye farkı (±)",
            Baslik = "Cariler",
            Aciklama = "Müşteri, tedarikçi ve atölye hesapları; bakiyeler ve tahsilat.",
            YeniCari = "Yeni müşteri",
            AraPlaceholder = "Ad, telefon veya e-posta ara...",
            AdFirma = "Ad / Firma Adı",
            Tur = "Tür",
            Musteri = "Müşteri",
            Tedarikci = "Tedarikçi",
            HerIkisi = "Her İkisi",
            Telefon = "Telefon",
            Adres = "Adres",
            TlBakiye = "TL Bakiye",
            HasGram = "Has Gram",
            Hareketler = "Hareketler",
            SonHareketler = "Son Hareketler",
            HareketYok = "Hareket yok",
            CariBulunamadi = "Cari bulunamadı",
            Olusturulamadi = "Cari oluşturulamadı",
            CariDetay = "Cari Detayı",
            CariSecin = "Cari seçin...",
            CariAra = "Cari Ara",
            MusteriAdiYazin = "Müşteri adı yazın...",
            Alacak = "Alacak",
            Borc = "Borç",
                    PasifeAlBagli = "Bağlı kayıt: {0} hareket · TL bakiye {1} · has bakiye {2} g",
            PasifeAlBagliYukleniyor = "Bağlı kayıtlar okunuyor…",
            // ── PAKET P-16 (Y-27) ──
            KimlikBolumu = "Kimlik ve ticari bilgiler",
            Unvan = "Ticari ünvan",
            UnvanIpucu = "Faturaya yazılan tüzel ad; boşsa cari adı kullanılır.",
            KimlikTuru = "Kimlik türü",
            KimlikTuruBelirtilmedi = "Belirtilmedi",
            KimlikTckn = "T.C. Kimlik No (11 hane)",
            KimlikVkn = "Vergi Kimlik No (10 hane)",
            KimlikNoHaneIpucu = "T.C. Kimlik No 11 hane, Vergi Kimlik No 10 hanedir.",
            DogumTarihi = "Doğum tarihi",
            Uyruk = "Uyruk",
            UyrukIpucu = "Ülkeyi listeden seçin.",
            IskontoYuzde = "Standart iskonto (%)",
            IskontoYuzdeIpucu = "Satış fişinde iskonto boş bırakılırsa bu oran uygulanır.",
            Segment = "Segment",
            HesapKodu = "Muhasebe hesap kodu",
            HesapKoduIpucu = "Mali müşavirin hesap planındaki karşılık (ör. 120.01.0001). "
                + "Boş bırakılırsa muhasebe fişinde bu satır boş çıkar — uydurulmaz.",
            SegmentSecilmedi = "Seçilmedi",
            SegmentBireysel = "Bireysel",
            SegmentKurumsal = "Kurumsal",
            SegmentToptan = "Toptan",
            SegmentVip = "VIP",
        },
        Urun = new()
        {
            Baslik = "Ürünler",
            Aciklama = "Stoktaki ürün kartları; ayar, gram, has gram ve reyon bilgisi.",
            UrunGrubu = "Ürün grubu",
            StokGirisiYolu = "Stok bu ekrandan elle yazılmaz: yeni üründe \"Açılış stoğu\" alanı, mevcut üründe alış işlemi ya da satırdaki \"Sayım Düzelt\" kullanılır. Böylece her stok değişikliği deftere ve maliyet partisine düşer.",
            YeniUrun = "Yeni ürün",
            AraPlaceholder = "Ürün adı veya barkod ara...",
            UrunAdi = "Ürün Adı",
            Barkod = "Barkod",
            Kategori = "Kategori",
            Ayar = "Ayar",
            TumAyarlar = "Tüm Ayarlar",
            Maden = "Maden",
            Madenler = "Madenler",
            TumMadenler = "Tüm Madenler",
            Saflik = "Saflık (milyem)",
            SaflikTablosu = "Seçilen madenin saflık tablosu",
            RodyumKaplama = "Rodyum kaplama",
            RodyumKaplanmaz = "Bu madende rodyum kaplama işaretlenemez.",
            Kod = "Kod",
            RodyumKaplamaIpucu = "Rodyum bir stok madeni değildir; beyaz altın, platin ve paladyum yüzeye vurulan kaplamadır. Yalnız etiket ibaresi olarak taşınır.",
            HasHesaplanmaz = "Has gramı hesaplanmaz",
            HasHesaplanmazIpucu = "Bu maden kıymetli değildir: has gramı yoktur ve fiyatı has kurundan türetilemez — satış fiyatı zorunludur.",
            KotasyonYok = "Kotasyon yok",
            KotasyonYokIpucu = "Bu madenin gram fiyatı sistemde tanımlı değil; fiyat teklifi üretilmez. Ürüne sabit satış fiyatı girin.",
            MadenBakiyeleri = "Maden bazında bakiye",
            MadenDagilimi = "Maden dağılımı",
            MadenUyarisi = "Toplam has YALNIZ altındır; diğer madenler ayrı satırlardadır — farklı madenlerin gramları toplanmaz.",
            GramAgirlik = "Gram Ağırlık (g)",
            Birim = "Birim",
            BirimAdet = "Adet",
            BirimGram = "Gram",
            BirimGramIpucu = "Gramlı üründe stok ve satış gram üzerindendir; 1 birim = 1 g, gram ağırlık otomatik 1 olur.",
            Karat = "Karat (ct)", Renk = "Renk", Berraklik = "Berraklık", Kesim = "Kesim", SertifikaKurumu = "Sertifika kurumu", SertifikaNo = "Sertifika no", Sertifikalar = "Sertifikalar", SertifikaYukle = "Sertifika PDF yükle", PdfOlmali = "Yalnız PDF, en fazla 5 MB", DortC = "Pırlanta 4C",
            TasAgirlik = "Taş Ağırlık (g)",
            TasKokeni = "Taş Kökeni",
            TasKokeniIpucu = "Taş ağırlığı girildiyse zorunludur (Kuyum Ticareti Yönetmeliği md.11/e)",
            TasIbaresi = "Etiket İbaresi",
            TasIbaresiIpucu = "Boş bırakılırsa kökenin varsayılan ibaresi kullanılır",
            TasKokenDogal = "Doğal",
            TasKokenSentetik = "Sentetik",
            TasKokenLaboratuvar = "Laboratuvar üretimi",
            TasKokenYapay = "Yapay üretim",
            TasKokenYok = "Taşsız",
            GercekMilyem = "Gerçek Milyem",
            GercekMilyemIpucu = "Boş bırakılırsa ayarın standart milyemi kullanılır",
            SatisMilyemi = "Satış milyemi",
            SatisMilyemiIpucu = "Örn. 950 — fiyat: gram × milyem × has. Boşsa alış milyemi, o da boşsa fiyat listesi",
            FiyatOfset = "Fiyat farkı (₺)",
            FiyatOfsetIpucu = "Hesaplanan fiyata eklenir; eksi yazılabilir",
            AlisMilyemi = "Alış milyemi (maliyet)",
            AlisMilyemiIpucu = "Toptancıdan alış milyemi, örn. 925. Satış milyemi boşsa bu milyemle satılır",
            KdvMatrahBinde = "KDV'ye giren pay (binde)",
            KdvMatrahBindeIpucu = "Örn. 1 = toplamın binde 1'i KDV'ye girer, kalanı %0. Boşsa yasal hesap",
            SatisFiyati = "Satış Fiyatı (₺)",
            SatisFiyatiIpucu = "Boş bırakılırsa satışta has×kur + işçilik otomatik hesaplanır",
            Milyem = "Milyem",
            Gram = "Gram",
            HasGram = "Has Gram",
            Durum = "Durum",
            Stok = "Stok",
            SubeStoklari = "Şube Stokları",
            Sube = "Şube",
            ToplamHas = "Toplam Has",
            StokYok = "Stok yok",
            UrunBulunamadi = "Ürün bulunamadı",
            Olusturulamadi = "Ürün oluşturulamadı",
            UrunSecin = "Ürün seçin...",
            UrunAdiYazin = "Ürün adı yazın...",
            UrunEkle = "Ürün Ekle",
            UrunDuzenle = "Ürünü Düzenle",
            PasifeAl = "Pasife Al",
            AcilisStogu = "Açılış stoğu (adet)",
            AcilisStoguIpucu = "Elinizde bulunan adet. Boş bırakılırsa ürün stoksuz açılır ve satılamaz. Kaydedildikten sonra stok yalnız alış, satış ve sayımla değişir.",
            FormAlanHatasiGenel = "Kayıt yapılmadı. Kırmızı işaretli alanı düzeltin.",
            Rfid = "RFID etiket kodu",
            // KESIF-P8: kurucu kalemi K-21 (RFID donanımı kapsam dışı) — iç kalem kodu ekrana basılmaz (karar 2).
            RfidIpucu = "Etiketteki kodu elle yazın. Bu kurulumda RFID okuyucu bağlantısı yoktur; alan yalnız kodu saklar.",
            KmtsKodu = "KMTS kodu",
            KmtsKoduIpucu = "Kıymetli Madenler Takip Sistemi kodu. Opsiyonel; sunucu doğrulamaz, olduğu gibi saklanır. Boş bırakılırsa kayıttan silinir.",
            GorselUrl = "Görsel adresi (URL)",
            GorselUrlIpucu = "Yalnız http:// veya https:// ile başlayan adres kabul edilir. Boş bırakılırsa adres silinir; dosya yüklemek için aşağıdaki görsel yükleyiciyi kullanın.",
            PariteKodu = "Parite kodu",
            PariteKoduIpucu = "Sikke/ziynet kotasyon kodu (ör. yeni çeyrek, Reşat…). Boş bırakılırsa eşleşme kaldırılır ve fiyat has gram × kur + işçilik ile hesaplanır.",
            Gorsel = "Görsel",
            GorselYok = "Görsel yok",
            Galeri = "Ürün görselleri",
            GaleriBos = "Bu ürüne henüz görsel eklenmemiş.",
            GaleriIpucu = "Yüklenen GÖRSEL ve FOTOĞRAF türündeki belgeler burada gösterilir.",
            GorselUrlGecersiz = "Görsel adresi yalnız http:// veya https:// ile başlayabilir.",
            PasifeAlOnay = "Ürün pasife alınacak. Geçmiş hareketleri korunur, yeni satışta seçilemez. Onaylıyor musunuz?",
            BarkodAra = "Barkod okut",
            BarkodAraIpucu = "Barkodu okutun veya yazıp Enter'a basın",
            AraVeyaBarkod = "Ürün adı, kod veya barkod — barkodu okutup Enter'a basın",
            BarkodBulunamadi = "Bu barkodla aktif ürün bulunamadı",
            BarkodFiyatGirilmeli = "Ürün seçildi ancak fiyatı otomatik hesaplanamadı (gümüşün gram kotasyonu modelde yok). Birim fiyatı elle girip Ekle deyin.",
            Etiket = "Etiket",
            EtiketMetni = "Etikete basılacak metin",
            IbareZorunlu = "İbare zorunlu",
            Dayanak = "Dayanak",
            SayimDuzelt = "Sayım Düzelt",
            SayilanAdet = "Sayılan adet",
            SayimIpucu = "Fiziksel sayımda bulunan adet. Kayıtla farkı sayım düzeltmesi hareketi olarak deftere yazılır.",
            Fark = "Fark",
            Reyon = "Reyon",
            TumReyonlar = "Tüm reyonlar",
            TumKategoriler = "Tüm kategoriler",
            Konsinye = "Konsinye",
            KonsinyeSahibi = "Konsinye sahibi",
            KonsinyeBirimBedel = "Sahibine ödenecek birim bedel",
            KonsinyeIpucu = "Konsinye mal bize ait değildir; stok değerine katılmaz, satılınca sahibine borçlanılır.",
            // ── PAKET P-16 (Y-26) ──
            KartBolumu = "Tedarik, maliyet ve stok bilgileri",
            Tedarikci = "Tedarikçi",
            TedarikciIpucu = "Malı bize satan cari. Konsinye sahibinden farklıdır.",
            AlisMaliyeti = "Alış maliyeti (TL)",
            AlisMaliyetiIpucu = "Kârlılık raporunun tek girdisi. Boşsa kâr HESAPLANMAZ, sıfır varsayılmaz.",
            AlisMaliyetiYok = "Maliyet girilmedi — bu ürün kârlılık raporunda \"maliyeti bilinmiyor\" sayılır.",
            HedefMarj = "Hedef marj (%)",
            HedefMarjIpucu = "Yalnız fiyat önerisi içindir; satışı kısıtlamaz.",
            KdvOrani = "KDV oranı (%)",
            KdvOraniIpucu = "Yüzde olarak, örn. 20. Boşsa faturada seçilen oran. Binde pay için \"KDV'ye giren pay\" alanı",
            SeriNo = "Seri numarası",
            Olcu = "Ölçü / boy",
            OlcuIpucu = "Yüzük ölçüsü, bilezik/zincir boyu (örn. 17, 18 cm).",
            MinStok = "Kritik stok eşiği",
            MinStokIpucu = "Stok bu adedin altına düşünce ürün azalan stok sayılır. Boşsa eşik yoktur.",
        },
        Islem = new()
        {
            Baslik = "İşlemler",
            Aciklama = "Satış, alış, hurda alışı, takas, işçilik, iade ve şubeler arası transfer.",
            SonSatislar = "Son satışlar",
            SonSatisYok = "Bu kiracıda henüz satış kaydı yok",
            OdemeKirilimi = "Ödeme kırılımı",
            OdemeKirilimiGoster = "Ödeme kırılımını göster",
            OdemeKirilimiGizle = "Ödeme kırılımını gizle",
            KirilimKayitli = "Kırılım kayıtlı",
            KirilimBilinmiyor = "Kırılım bilinmiyor — bu fişin ödeme payları kaydedilmemiş; \"ödeme yok\" anlamına GELMEZ",
            KirilimKismen = "Kırılımın bir payı ölçülemedi",
            KirilimGeriDolduruldu = "Geçmiş kayıttan türetildi",
            KirilimYok = "Bu fiş için kayıtlı ödeme kalemi yok",
            IadeEt = "İade et",
            IadeHakkiBitti = "Bu fişin tamamı iade edilmiş",
            SatisDuzeltmeYolu = "Müşteri malı geri getirdiyse \"Satış İadesi\" sekmesinden iade girin; yalnız açıklama/ödeme bilgisi yanlışsa düzeltme fişiyle cari bakiyesi düzeltilir.",
            FisNo = "Fiş no",
            CariSecin = "Cari seçin",
            UrunSecinUyari = "Önce ürün seçin",
            Satis = "Satış",
            Alis = "Alış",
            HurdaAlis = "Hurda Alış",
            Takas = "Takas",
            Iscilik = "İşçilik",
            Transfer = "Transfer",
            Formu = "Formu",
            KasaId = "Kasa ID",
            KasaIdOpsiyonel = "Kasa ID (opsiyonel)",
            Kasa = "Kasa",
            KasaSecin = "Kasa seçin...",
            TekKasaOtomatik = "Tek kasa var — otomatik seçildi",
            KurdanGeldi = "Kurdan geldi",
            ElleGirildi = "Elle girildi",
            OdemeTuru = "Ödeme Türü",
            TevsikNakitOnerisi = "Bu tutar tevsik haddini ({0}) aşıyor — nakit ödeme reddedilir. HAVALE/EFT veya kart seçin.",
            OdemeNakit = "Nakit",
            OdemeKart = "Kart",
            OdemeHavale = "Havale",
            OdemeAltin = "Altın",
            OdemeDoviz = "Döviz (efektif)",
            DovizMiktar = "Döviz miktarı",
            DovizTlKarsiligi = "TL karşılığı (kur kaydından)",
            OdemeVeresiye = "Veresiye",
            KarmaOdeme = "Karma ödeme (birden fazla araç)",
            KarmaOdemeIpucu = "Aynı fişi nakit + kart + havale + veresiye olarak bölerek kapatın.",
            Bacaklar = "Ödeme bacakları",
            BacakEkle = "Bacak ekle",
            BacakSil = "Bacağı sil",
            Arac = "Araç",
            BacakPosCihazi = "POS cihazı",
            BacakArac = "Ödeme aracı",
            BacakTaksit = "Taksit",
            BacakKasaOtomatik = "Bu araç için tek uygun kasa var; kasa otomatik seçildi.",
            BacakPosTek = "Tanımlı tek POS cihazı var; cihaz otomatik seçildi.",
            BacakPosTaksit = "Taksit",
            BacakPosOtomatik = "Şubede tek aktif cihaz var; otomatik seçildi.",
            BacakPosCihazYok = "Aktif POS cihazı tanımlı değil — kart bacağı cihazsız yazılır.",
            FisToplami = "Fiş toplamı",
            BacakToplami = "Bacak toplamı",
            Kalan = "Kalan",
            KalaniAta = "Kalanı ata",
            BacakToplamiUyusmuyor = "Bacakların toplamı fiş tutarına birebir eşit olmalıdır.",
            SatisIade = "Satış İadesi",
            AlisIade = "Alış İadesi",
            IadeEdilecekIslem = "İade edilecek işlem",
            IslemSecin = "İşlem seçin",
            IadeAdedi = "İade adedi",
            IadeTutari = "İade tutarı",
            OrantiliTutar = "Orantılı tutar",
            KalanIadeHakki = "Kalan iade hakkı",
            IadeIslemiYok = "İade edilebilecek işlem bulunamadı",
            NakitKapali = "Fiş toplamı 30.000 TL haddini aştığı için NAKİT bacak eklenemez — ödemenin tamamı banka, kart veya havale ile yapılmalıdır (459 s. VUK Genel Tebliği §4.1.2).",
            KalanNakitHakki = "Bu müşterinin bugünkü kalan nakit hakkı",
            HasGramFiyati = "Has fiyatı (1 gram)",   // ISLEM-BANT-2: tek ad (BASİT Değişim ile aynı), Title Case ve "(₺)" yok — birim kutunun sağında
            SaticiKimligi = "Satıcı kimliği",
            SaticiKimligiNot = "Hurda alırken satıcının kimliği her tutarda istenir; gider pusulası bunun içindir.",
            SaticiTipi = "Satıcı tipi",
            SaticiTipiNihai = "Nihai tüketici (vatandaş — stopaj yok)",
            SaticiTipiEsnaf = "Esnaf muaf (%2 stopaj)",
            SaticiTipiMukellef = "Vergi mükellefi (pusula düzenlenmez, satıcı fatura keser)",
            SaticiAdSoyad = "Satıcı ad soyad",
            SaticiTckn = "Satıcı TCKN",
            SaticiAdres = "Satıcı adresi (isteğe bağlı)",
            TedarikciFaturaNo = "Tedarikçi fatura no",
            SaticiTipiZorunlu = "Satıcı tipi zorunludur.",
            SaticiAdZorunlu = "Satıcı ad soyad zorunludur.",
            SaticiTcknZorunlu = "Satıcı TCKN zorunludur.",
            SaticiTcknGecersiz = "TCKN 11 haneli rakam olmalıdır.",
            TedarikciFaturaNoZorunlu = "Mükellef satıcıdan alımda tedarikçi fatura numarası zorunludur.",
            SaticiKimligiEksik = "Satıcı kimliği eksik: satıcı tipini seçin ve kimlik alanlarını doldurun.",
            CaridenGeldi = "Cariden geldi",
            MasakKimlikEksik = "Kimlik tespiti eksik: MASAK eşiği aşıldı, müşteri için kimlik tespiti bilgilerini girin.",
            TakasEskiUrun = "Eski Ürün (Müşteriden Alınan)",
            TakasYeniUrun = "Yeni Ürün (Müşteriye Verilen)",
            Fark = "Fark",
            FarkMusteriOder = "Müşteri öder",
            FarkIadeEdilir = "Müşteriye iade edilir",
            KaynakSube = "Kaynak Şube",
            HedefSube = "Hedef Şube",
            SubeSecin = "Şube seçin...",
            BirimFiyat = "Birim Fiyat (₺)",
            Birim = "Birim",
            Urun = "Ürün",
            Kaydet = "Kaydet",
            Basarili = "İşlem başarıyla kaydedildi!",
            Basarisiz = "İşlem gerçekleştirilemedi",
            BilgiKontrol = "İşlem gerçekleştirilemedi. Bilgileri kontrol edin.",
            IscilikTutari = "İşçilik Tutarı (₺)",
            MasakBaslik = "MASAK Kimlik Tespiti",
            MasakAciklama = "Bu tutar yasal eşiği aştığı için müşteri kimliği zorunludur. Bilgiler şifreli saklanır, 8 yıl korunur.",
            MasakBelgeTuru = "Belge Türü",
            MasakBelgeNo = "Belge No (TCKN / Pasaport)",
            MasakAdSoyad = "Ad Soyad",
            MasakDogumTarihi = "Doğum Tarihi",
            MasakUyruk = "Uyruk (TUR)",
            MasakAdres = "Adres",
            MasakIslemAmaci = "İşlem Amacı",
            MasakBeyanAciklama = "Beyan Açıklaması",
            MasakFonKaynagi = "Fon Kaynağı",
            MasakEkBelge = "Ek Belge Referansı",
            MasakKademeKimlik = "Kimlik tespiti gerekli",
            MasakKademeAmac = "Kimlik + işlem amacı beyanı gerekli",
            MasakKademeForm = "Kimlik + beyan + nakit işlem beyan formu (fon kaynağı) gerekli",
            MasakKademeEkBelge = "Kimlik + beyan formu + ek belge gerekli",
            MasakEnAzKarakter = "en az {0} karakter",
            // ── PAKET P-16 (Y-4) ──
            KalemAyar = "Ayar",
            KalemMilyem = "Milyem",
            KalemGram = "Gram",
            KalemHas = "Has (g)",
            SatirIskonto = "Satır iskontosu (%)",
            FisIskonto = "Fiş iskontosu (%)",
            IskontoIpucu = "Yüzde girilir; TL karşılığını sunucu hesaplar.",
            IskontoOnizleme = "Tahmini iskonto",
            IskontoUygulandi = "Uygulanan iskonto",
            IskontoCariden = "cari kartındaki orandan",
            ListedenGeldi = "fiyat listesinden geldi",
            PasifCariGizlendi = "Pasif cariler bu listede görünmez; kartı Cariler ekranından aktifleştirin.",
            NetTutar = "Net tutar",
            GramAgirlik = "Gram Ağırlık",
            AciklamaOpsiyonel = "Açıklama (opsiyonel)",
        },
        Pos = new()
        {
            Baslik = "POS Cihazları",
            TahsilatBaslik = "POS Hakedişleri",   // P-14 (Y-9): menü ile TEK ad
            YeniCihaz = "Yeni POS cihazı",
            CihazAdi = "Cihaz Adı",
            BankaAdi = "Banka",
            UyeIsyeriNo = "Üye İşyeri No",
            TerminalNo = "Terminal No",
            BagliKasa = "Bağlı Kasa",
            BankaHesabi = "Banka Hesabı",
            KomisyonOran = "Komisyon Oranı",
            ValorGun = "Valör Günü",
            BekleyenAlacak = "Bekleyen POS Alacağı",
            BugunYatacak = "Bugüne Kadar Yatması Gereken",
            CihazSayisi = "Cihaz Sayısı",
            ValorTakvimi = "Valör Takvimi",
            ValorTarihi = "Valör Tarihi",
            Brut = "Brüt",
            Komisyon = "Komisyon",
            Net = "Net",
            Yatan = "Yatan",
            Kalan = "Kalan",
            Taksit = "Taksit",
            IslemTarihi = "İşlem Tarihi",
            Durum = "Durum",
            DurumBekliyor = "Bekliyor",
            DurumKismi = "Kısmi yattı",
            DurumYatti = "Yattı",
            DurumIptal = "İptal",
            Duzenle = "Düzenle",
            PasifeAl = "Pasife al",
            Aktiflestir = "Aktifleştir",
            PasifeAlOnay = "Bu POS cihazını pasife almak istiyor musunuz?",
            AcikAlacakUyarisi = "Açık POS alacağı olan cihaz pasife alınamaz",
            Iptal = "Tahsilatı iptal et",
            IptalGerekcesi = "İptal gerekçesi (en az 20 karakter)",
            IptalOnay = "Bu POS tahsilatını iptal etmek istiyor musunuz? İşlem geri alınamaz.",
            KayitYok = "Bu aralıkta POS tahsilatı yok",
            CihazYok = "Henüz POS cihazı tanımlanmadı",
            DefterGerekce = "POS alacak defteri satırı satıştan türetilir, elle değiştirilemez.",
            DefterAlternatif = "Düzeltme yolu: banka itirazı için iptal ucu (gerekçe ≥ 20 karakter) ya da ilgili satışın iadesi.",
            KasaKilitGerekce = "Bu cihazda POS tahsilatı yazılmış; tahsilat satırları bağlı kasayı ve şubeyi taşır, sonradan taşınırsa defter ile banka ekstresi ayrışır.",
            KasaKilitYolu = "Düzeltme yolu: cihazı pasife alın ve doğru kasaya bağlı yeni bir cihaz kurun. Henüz tahsilatı olmayan cihazda bu alan doğrudan düzenlenebilir.",
            KasaGocBaslat = "Yeni kasaya taşı",
            KasaGocBaslik = "POS cihazını yeni kasaya taşı",
            KasaGocAciklama = "Bu cihazda tahsilat var; bağlı kasa doğrudan değiştirilmez. Gerekçeli göç ile taşıyabilirsiniz.",
            KasaGocUyari = "Yazılmış tahsilatlar ESKİ kasada kalır; göç yalnız bundan sonraki tahsilatların düşeceği kasayı değiştirir. İşlem tutanağa yazılır ve silinmez.",
            KasaGocGerekce = "Göç gerekçesi (en az 20 karakter, denetim izine yazılır)",
            KasaGocOnayla = "Taşımayı onayla",
            KasaGocSebep = "Yeni kasa seçilmeli ve en az 20 karakterlik gerekçe yazılmalıdır.",
            KasaGocTamam = "POS cihazı yeni kasaya taşındı. Eski kasada kalan tahsilat: {0}",
            YeniKasa = "Yeni kasa",
            KomisyonAciklama = "Oran 0 ile 1 arasında yazılır (örn. %1,85 → 0,0185). Bu oran MEVZUAT DEĞİL, banka üye işyeri sözleşmesidir.",
            ValorAciklama = "Kaç gün sonra bankadan yatacağı. Takvim günü sayılır.",
            SinirUyarisi = "Sunucu sınırına ulaşıldı — daha fazlası var, tarih veya cihaz süzgecini daraltın.",
            DonemdenBagimsiz = "(bugün itibarıyla — dönemden bağımsız)",
            TaksitOranlari = "Taksit Oranları",
            TaksitOranEkle = "Taksit oranı ekle",
            Eslestir = "Banka yatmasıyla eşleştir",
            OtoOneri = "Öneri getir",
            OneriYok = "Tutarı bu harekete eşitlenen tek bir tahsilat kümesi bulunamadı; elle eşleştirin.",
            GeriAl = "Eşleşmeyi geri al",
            GeriAlmaGerekcesi = "Geri alma gerekçesi (en az 20 karakter)",
            Kaydedildi = "Kaydedildi",
            Eylemler = "İşlemler",
            AdCokKisa = "Cihaz adı en az 2 karakter olmalıdır",
            TumDurumlar = "Tüm durumlar",
            BankaHareketId = "Banka hareketi kimliği",
            TatilUyarisi = "Resmî tatil takvimi YOKTUR: valör günü takvim günü (ya da yalnız haftasonu atlanarak) sayılır; bayramda banka yatırmayabilir.",
        },
        Kasa = new()
        {
            Baslik = "Kasalar",
            YeniKasa = "Yeni Kasa",
            KasaAdi = "Kasa Adı",
            Doviz = "Döviz",
            ToplamTlBakiye = "Toplam TL Bakiye",
            KasaSayisi = "Kasa Sayısı",
            AktifKasa = "Aktif Kasa",
            Hareket = "Hareket",
            HareketGir = "Hareket Gir",
            KasaHareketi = "Kasa Hareketi",
            HareketTuru = "Hareket Türü",
            KasaGecmisi = "Kasa Geçmişi",
            KasaBulunamadi = "Kasa bulunamadı",
            Olusturulamadi = "Kasa oluşturulamadı",
            Duzenle = "Kasayı Düzenle",
            Duzenlenemedi = "Kasa güncellenemedi",
            AdDuzenle = "Kasa adını düzenle",
            SayfaAciklama = "Kasalar, bakiyeleri ve kasa hareketleri; kasa bilgileri düzenlenebilir, pasife alınan kasa geri açılabilir.",
            Sube = "Şube",
            DovizKilit = "Para birimi ve şube yalnız bakiyesi sıfır, hiç hareketi ve gün kapanışı olmayan kasada değiştirilebilir; aksi hâlde geçmiş hareketlerin anlamı bozulur.",
            PasifKasaUyari = "Bu kasa pasiftir: yeni hareket girilemez. Geçmişi okunabilir; yeniden kullanmak için önce aktife alın.",
            HareketStornoYok = "Bu satır bir defter işleminin kasa bacağıdır; tek başına ters kayıtla (storno) hükümsüz kılınamaz.",
            HareketStornoYolu = "Düzeltme yolu işlemin KENDİ stornosudur: fişi doğduğu ekrandan (satış · tahsilat · emanet) storno edin; kasa bacağı da birlikte geri alınır.",
            HareketAciklamaYok = "(açıklama girilmemiş)",
            HareketAciklamaGuncellendi = "Kasa hareketinin açıklaması güncellendi.",
            HareketModalBaslik = "{0} — kasa hareketi",
            HareketSonuc = "{0} {1} kaydedildi. Yeni bakiye: {2}",
            HareketStornoOnay = "Bu elle girilen kasa hareketi ters kayıtla hükümsüz kılınacak. Kaynak satır SİLİNMEZ; geçmişte kalır ve ters kaydıyla eşleşir.",
            HareketDuzenlenemez = "Kasa hareketi yazıldıktan sonra yerinde değiştirilemez: kasa bakiyesi ve gün kapanışı bu satıra dayanır.",
            KasaKaydedildi = "Kasa bilgileri kaydedildi.",
            GunKapatildi = "Gün kapatıldı.",
            PasifeAl = "Pasife Al",
            PasifeAlOnay = "Kasa pasife alınacak. Geçmiş hareketleri korunur, yeni işlemde seçilemez. Onaylıyor musunuz?",
            BakiyeliKasaUyarisi = "Bakiyesi sıfır olmayan kasa pasife alınamaz — para nereye gitti sorusu cevapsız kalır.",
            GunKapanisi = "Gün sonu kapanışı",
            GunKapat = "Günü Kapat",
            SayilanNakit = "Sayılan nakit",
            DefterBakiye = "Defter bakiyesi",
            Fark = "Fark",
            FarkGerekcesi = "Fark gerekçesi",
            FarkGerekcesiZorunlu = "Sayım defterle tutmuyor — gerekçe zorunludur.",
            GunKapali = "Bu kasa bugün için KAPATILDI. Yeni hareket yazılamaz.",
            GunuYenidenAc = "Günü Yeniden Aç",
            AcmaGerekcesi = "Yeniden açma gerekçesi (en az 20 karakter)",
            KapanisGecmisi = "Kapanış geçmişi",
            KapanisYok = "Bu kasada henüz gün sonu kapanışı yapılmadı",
            HareketGirilemedi = "Hareket girilemedi",
            HareketKaydedildi = "Hareket kaydedildi!",
            Gelir = "Gelir",
            Gider = "Gider",
            Tahsilat = "Tahsilat",
            Odeme = "Ödeme",
        },
        Rapor = new()
        {
            Baslik = "Raporlar",
            AylikSatis = "Aylık Satış",
            AylikAlis = "Aylık Alış",
            Kar = "Kâr (Satış - Alış)",
            NetKar = "Net Kâr",
            NetHasGram = "Net Has Gram",
            NetHas = "Net Has",
            HareketSayisi = "Hareket sayısı",
            GunlukSatisAlis = "Günlük Satış / Alış (₺)",
            StokDagilimi = "Stok Dağılımı (Ayara Göre)",
            CariYaslandirma = "Cari Yaşlandırma Raporu",
            StokDegeri = "Stok Değeri",
            Gun_0_30 = "0-30 gün",
            Gun_31_60 = "31-60 gün",
            Gun_61_90 = "61-90 gün",
            Gun_90_plus = "90+ gün",
            Bugun = "Bugün",
            AylikRapor = "Aylık Rapor",
            KarMarji = "Kâr Marjı",
            IscilikGeliri = "İşçilik Geliri",
            HurdaAlis = "Hurda Alış",
            ToplamAdet = "Toplam Adet",
            CariBasligi = "Cari",
            YasHasBakiye = "Has bakiyesi",
            YasToplamTl = "Toplam (₺)",
            YasHasBakiyeVar = "Has bakiyesi var",
            YasHasNotu = "Has bakiyesi TL'ye çevrilmez, ayrı okunur: artı = cari bize has borçlu, eksi = biz cariye has borçluyuz.",
            YasListelenenToplam = "Listelenen carilerin toplamı",
            YasSinirUyari = "Yalnız ilk {0} cari listelendi, toplam {1} cari var; alt toplam yalnız listelenenleri kapsar.",
        },
        RaporSeti = new()
        {
            Merkez = "Rapor Merkezi",
            MerkezAciklama = "Sevk edilmiş raporların tam listesi — kartına tıklayın.",
            RaporSayisi = "rapor",
            GrupGunluk = "Gün Sonu",
            GrupSatis = "Satış analizi",
            GrupStok = "Stok",
            GrupMali = "Mali",
            AdGunluk = "Günlük Özet",
            AdAylik = "Aylık Özet",
            AdPosMutabakat = "Gün Sonu Mutabakatı",
            AdKasaDefteri = "Kasa Hareketleri",
            AdIslemDefteri = "Hareket Dökümü",
            AdOdemeAraci = "Ödeme Aracı Kırılımı",
            AdPersonel = "Kaydeden Kullanıcı",
            AdSubeKarsilastirma = "Şube Karşılaştırma",
            AdStokYaslandirma = "Stok Yaşlandırma / Devir Hızı",
            YaslandirmaAciklama = "Mal ne kadar süredir rafta ve ne hızla dönüyor. Yaş, malın işletmeye giriş tarihinden ölçülür.",
            Kova0_30 = "0-30 gün",
            Kova31_90 = "31-90 gün",
            Kova91_180 = "91-180 gün",
            Kova181_365 = "181-365 gün",
            Kova365Ustu = "365 günden eski",
            YasiBilinmeyen = "Yaşı bilinmeyen",
            YasiBilinmeyenAciklama = "Maliyet partisi olmayan mal — giriş tarihi kayıtlı değil. Sıfır sayılmaz, kovalara dağıtılmaz.",
            DevirHizi = "Devir hızı",
            DevirPenceresi = "Devir penceresi (gün)",
            DevirTanimi = "Devir hızı = pencerede çıkan adet ÷ bugün rafta kalan adet.",
            YillikTahmini = "Yıllık tahmini devir",
            OrtalamaRafOmru = "Ortalama raf ömrü (gün)",
            OluStok = "Ölü stok",
            OluEsik = "Ölü stok eşiği (gün)",
            TuketilenAdet = "Çıkan adet",
            KalanAdet = "Rafta kalan adet",
            MaliyetiBilinmeyenAdet = "Maliyeti bilinmeyen adet",
            EnEskiGiris = "En eski giriş",
            YasGun = "Yaş (gün)",
            Hesaplanamadi = "Hesaplanamadı",
            Adet = "Adet",
            AdStokDegeri = "Stok Değeri",
            AdReyonStok = "Reyon Stoğu",
            AdStokDegerlemesi = "Stok Değerlemesi",
            AdCariYaslandirma = "Cari Yaşlandırma",
            AdVadeTakibi = "Gecikmiş Alacaklar",
            AdKarZarar = "Kâr-zarar",
            AdKarZararUrun = "Ürün Bazlı Kârlılık",
            Tarih = "Tarih",
            Tur = "Tür",
            Yon = "Yön",
            Giris = "Giriş",
            Cikis = "Çıkış",
            YonKarisik = "Karışık",
            YonYok = "—",
            YonYokIpucu = "Tutar ve has gramı sıfır — yön yok",
            YonTlBacagi = "Para",
            YonHasBacagi = "Has",
            TlTutar = "Tutar (₺)",
            HasGram = "Has (g)",
            OdemeTuru = "Ödeme",
            NakitPayi = "Nakit payı",
            VeresiyePayi = "Veresiye payı",
            Fis = "Fiş",
            FaturaNo = "Fatura No",
            CariAd = "Cari",
            UrunAd = "Ürün",
            SubeAd = "Şube",
            KaydedenKullanici = "Kaydeden Kullanıcı",
            KaydedenUyarisi = "Bu rapor işlemi KAYDEDEN kullanıcıyı sayar; satan kişi ile aynı olmayabilir.",
            Storno = "Storno",
            StornoAdet = "Storno adedi",
            StornoAciklama = "Storno çifti toplamlardan dışlanır; hükümsüz kılınan işlem ciroyu şişirmez.",
            Bilinmiyor = "Bilinmiyor",
            BilinmiyorAciklama = "Nakit payı kaydedilmemiş eski satırlar — sıfır sayılmaz.",
            Devir = "Devir",
            Kapanis = "Kapanış",
            SayilanNakit = "Sayılan nakit",
            Fark = "Fark",
            DurumAcik = "AÇIK (kapanış yapılmadı)",
            DurumKapali = "Kapalı",
            DurumYenidenAcildi = "Yeniden açıldı",
            SayimGir = "Sayımı gir",
            SayimBaslik = "Gün sonu kasa sayımı",
            SayimYalnizBugun = "Yalnız bugünün kasası sayılabilir.",
            SayimNegatif = "Sayılan nakit negatif olamaz.",
            SayimKaydedildi = "Gün sonu sayımı kaydedildi.",
            SayimKaydedildiFark = "Gün sonu sayımı kaydedildi. Fark: {0}",
            DefterBakiye = "Defter bakiyesi",
            FarkGerekcesi = "Fark gerekçesi (fark varsa zorunlu)",
            HareketSayisi = "Hareket",
            Arac = "Ödeme aracı",
            KasaSecimi = "Kasa",
            TumKasalar = "Tüm kasalar",
            TumTurler = "Tüm türler",
            TumSubeler = "Tüm şubeler",
            TumKullanicilar = "Tüm kullanıcılar",
            TumAraclar = "Tüm araçlar",
            IslemSayisi = "İşlem sayısı",
            Ciro = "Ciro",
            OrtalamaSepet = "Ortalama sepet",
            IadeAdet = "İade adedi",
            IadeOrani = "İade oranı",
            TransferSutun = "Transfer",
            TransferNotu = "Transfer ciroya KATILMAZ (aynı mal iki şubede sayılmasın).",
            Yazdir = "Yazdır",
            Onceki = "Önceki",
            Sonraki = "Sonraki",
            SayfaBilgisi = "satır",
            SinirUyarisi = "Sınıra ulaşıldı — daha fazlası var, süzgeçle daraltın.",
            BosDurum = "Bu dönemde kayıt yok.",
            DonemBagimsiz = "(bugün itibarıyla — dönemden bağımsız)",
            Guvenilirlik = "Güvenilirlik",
            GuvenilirlikTam = "Tam",
            GuvenilirlikYuksek = "Yüksek",
            GuvenilirlikOrta = "Orta",
            GuvenilirlikDusuk = "Düşük",
            GuvenilirlikVeriYok = "Veri yok",
            MalFarki = "Mal farkı",
            HurdaTl = "Hurda (₺)",
            HurdaHas = "Hurda (has g)",
            AlisTl = "Alış (₺)",
            SatisAdet = "Satış adedi",
            Baslangic = "Başlangıç",
            Bitis = "Bitiş",
            Uygula = "Uygula",
            VeresiyeDogan = "Doğan veresiye",
            VeresiyeKapanan = "Kapanan veresiye",
            Durum = "Durum",
            ParaBirimi = "Para birimi",
        },
        Banka = new()
        {
            Baslik = "Banka",
            YeniHesap = "Yeni hesap",
            BankaAdi = "Banka Adı",
            HesapAdi = "Hesap Adı",
            HesapSahibi = "Hesap Sahibi",
            Iban = "IBAN",
            HesapBulunamadi = "Banka hesabı bulunamadı",
            HesapOlusturulamadi = "Hesap oluşturulamadı",
            ToplamBakiye = "Toplam Bakiye",
            HesapSayisi = "Hesap Sayısı",
            Hareketler = "Hareketler",
            YeniHareket = "Yeni hareket",
            DekontGir = "Dekont Gir",
            Yon = "Yön",
            Gelen = "Gelen",
            Giden = "Giden",
            KarsiTaraf = "Karşı Taraf",
            KarsiIban = "Karşı IBAN",
            HareketEklenemedi = "Hareket eklenemedi",
            HareketEklendi = "Hareket kaydedildi!",
            FaturayaDonustur = "Faturaya Dönüştür",
            Faturalandi = "Faturalandı",
            Bekliyor = "Bekliyor",
            Eslesti = "Eşleşti",
            DurumFiltre = "Durum",
            CariSecInfo = "Fatura kesilecek cariyi seçin",
            SayfaAciklama = "Banka hesapları ve dekontlar; hesap bilgileri düzenlenebilir, pasife alınan hesap geri açılabilir.",
            HesapKaydedildi = "Banka hesabı kaydedildi.",
            DekontDegismez = "Dekontun tutarı ve yönü yerinde değiştirilemez: para hareketi bankada gerçekleşmiştir, defter satırı geriye dönük yazılmaz.",
            DekontDegismezYolu = "Tutar ya da yön yanlışsa dekontu STORNO edin (ters kayıt üretilir) ve doğrusunu yeni bir dekont olarak girin; açıklama, karşı taraf ve tarih doğrudan düzeltilebilir.",
            DekontGuncellendi = "Dekont güncellendi.",
            DekontStornolandi = "Stornolandı",
            DekontStornoYok = "Bu dekont stornolanamaz: ya zaten stornolanmış/ters kayıt satırıdır, ya faturaya dönüştürülmüştür, ya da banka ekstresinden gelmiştir.",
            DekontStornoYolu = "Faturalanmış dekont için önce faturayı iptal edin; ekstreden gelen satır bankanın kendi kaydıdır ve ekrandan hükümsüz kılınmaz.",
            DekontDuzenle = "Dekontu düzelt",
            DekontKilitliAlanlar = "Tutar, yön ve para birimi kilitlidir (finansal alan) — düzeltme yolu stornodur.",
            PasifHesapUyari = "Bu hesap pasiftir: yeni dekont girilemez. Geçmişi okunabilir; yeniden kullanmak için önce aktife alın.",
            BankadakiBakiye = "Bankadaki bakiye",
            BankaBloke = "bloke",
            BankaBakiyeFarki = "Mihenk kaydıyla fark: {0}",
            DonusturBasarili = "Fatura oluşturuldu!",
            DonusturBasarisiz = "Faturaya dönüştürülemedi",
            HesabiDuzenle = "Hesabı Düzenle",
            HesapGuncellendi = "Hesap güncellendi!",
            HesapGuncellenemedi = "Hesap güncellenemedi",
            DekontCari = "Müşteri / cari",
            DekontCariYok = "Seçilmedi (kümülatif kurulamaz)",
            DekontCariIpucu = "Cari seçilirse aynı gün aynı müşteriyle yapılan işlemler MASAK eşiğinde birlikte değerlendirilir.",
            MasakCariUyari = "Cari seçilmediği için bu tutar yalnız kendi başına değerlendirilir; aynı müşteriyle bugün yapılan diğer işlemler (bağlantılı işlem toplamı) hesaplanamaz. Kişiyi bağlamak için müşteri seçiniz.",
            MasakKilitSebep = "MASAK kimlik/beyan alanları eksik — kaydetmek için yukarıdaki zorunlu alanları doldurun.",
            MasakYukleniyorSebep = "MASAK gereksinimi sunucudan alınıyor — birkaç saniye içinde alanlar açılacak.",
            MasakTespitDegismez = "Bu tutarda girilen dekont için alınan MASAK kimlik tespiti, mevzuat gereği 8 yıl saklanan ve sonradan değiştirilemeyen bir yükümlülük kaydıdır.",
            // ── paket 32 — banka ekstresi içe aktarma ──
            EkstreYukle = "Ekstre Yükle",
            EkstreBaslik = "Banka ekstresi eşleştirme",
            EkstreAciklama = "Yüklenen ekstre satırlarını gözden geçirin, cariye bağlayın ve seçtiklerinizi banka hareketine çevirin.",
            EkstreDosyasi = "Ekstre dosyası (CSV)",
            EkstreFormat = "Dosya biçimi",
            Prova = "Prova",
            ToplamSatir = "Toplam satır",
            YeniSatir = "Yeni",
            Mukerrer = "Mükerrer",
            HataliSatir = "Hatalı",
            OnaylananSatir = "Onaylanan",
            Atla = "Atla",
            AtlamayiGeriAl = "Atlamayı geri al",
            SeciliOnayla = "Seçilileri onayla",
            Eslestir = "Eşleştir",
            OneriListesi = "Ünvan önerileri",
            CariBagla = "Cariye bağla",
            BagiKaldir = "Bağı kaldır",
            IbanOgrenildi = "IBAN bu cariye öğretildi; sonraki ekstrelerde otomatik eşleşecek.",
            AcilisBakiye = "Açılış bakiyesi",
            KapanisBakiye = "Kapanış bakiyesi",
            DosyaZatenYuklendi = "Bu dosya bu hesaba zaten yüklendi.",
            TekrarYukle = "Aynı dosyayı yine de yükle (satırlar mükerrer işaretlenir)",
            SatirBulunamadi = "Bu süzgeçle eşleşen ekstre satırı yok.",
            SablonIndir = "Şablon indir",
            TumunuSec = "Bu sayfadakileri seç",
            YuklemeYazmaz = "Yükleme hiçbir banka hareketi üretmez. Hareket yalnız seçtiğiniz satırları onayladığınızda oluşur.",
            CarisizSatirUyari = "Seçili {0} satırda cari eşleşmesi yok. Cari seçmeden onaylarsanız carisiz banka hareketi oluşur.",
            CarisizOnayla = "Carisiz onayla",
            CarisizOnaylaIpucu = "İşaretlenmezse cari eşleşmesi olmayan satırlar onaylanmaz; beklemede kalır ve sonra cari seçilebilir.",
            CarisizUretilen = "carisiz hareket: {0}",
            CarisizAtlanan = "cari yok diye beklemede kalan: {0}",
            MukerrerGecmisUyari = "Mükerrer koruması bu paketten SONRA girilen hareketleri kapsar; daha eski kayıtlar kapsam dışıdır.",
            OnaylananSatirDegismez = "Onaylanan ekstre satırı değişmez: bu satırdan bir banka hareketi üretildi ve sahne kaydı o hareketin kanıtıdır.",
            OnaylananSatirYolu = "Yanlışsa düzeltme yolu, üretilen banka hareketinin storno/düzeltme fişidir.",
            SecimYokSebep = "Onaylamak için en az bir satır seçin.",
            OnaySiniriSebep = "Tek seferde onaylanabilecek satır sınırı aşıldı.",
            HamSatir = "Dosyadaki ham satır",
            EkstreYuklendi = "Ekstre yüklendi",
            EkstreOnaylandi = "Seçili satırlar onaylandı",
            SinirUyari = "Sunucu bu listeyi sayfalar; sınıra değdiyseniz süzgeçle daraltın.",
            Mt940Yok = "MT940 ayrıştırıcısı bu sürümde yoktur; bankanızın CSV ekstresini kullanın.",
        },
        Fatura = new()
        {
            Baslik = "Faturalar",
            YeniFatura = "Yeni fatura",
            FaturaNo = "Fatura No",
            Tur = "Tür",
            Satis = "Satış",
            Alis = "Alış",
            AraToplam = "Ara Toplam",
            KdvOran = "KDV Oranı",
            KdvTutar = "KDV Tutarı",
            Toplam = "Toplam (KDV Dahil)",
            KdvTuru = "KDV Türü",
            KdvStandart = "Standart",
            KdvKuyum = "Kuyum (has hariç)",
            HasDeger = "Has Altın Değeri",
            HasDegerIpucu = "Has altın KDV'siz; KDV yalnız işçilik+kâra uygulanır",
            Matrah = "KDV Matrahı (İşçilik+Kâr)",
            Durum = "Durum",
            Taslak = "Taslak",
            Kesildi = "Kesildi",
            Odendi = "Ödendi",
            Iptal = "İptal",
            FaturaBulunamadi = "Fatura bulunamadı",
            Olusturulamadi = "Fatura oluşturulamadı",
            KaynakBanka = "Banka hareketinden",
            Manuel = "Manuel",
            Kaynak = "Kaynak",
            OzelMatrahBaslik = "Özel matrah neden kodu",
            OzelMatrahKapali = "HARİÇ: bu kurulumda KDV 23/e özel matrah neden kodu faturaya basılmaz ve muhasebe fişinde alan boş bırakılır. Anahtar, mali müşavir teyidi (K-10) gelene kadar kapalıdır; teyitten sonra yapılandırmada tek alanla açılır.",
            OzelMatrahAralikDisi = "Uyarı: kod GİB aralığının (801-812) dışında.",
            OzelMatrahOkunamadi = "Özel matrah ayarı okunamadı.",
            OdendiIsaretle = "Ödendi İşaretle",
            Kes = "Kes",
            IptalEt = "İptal Et",
            Kalemler = "Kalemler",
            Sira = "Sıra",
            Miktar = "Miktar",
            BirimFiyat = "Birim Fiyat",
            KdvMatrah = "KDV Matrahı",
            SatirToplam = "Satır Toplamı",
            Iscilik = "İşçilik",
            Indirim = "İndirim",
            TasIbaresi = "Taş İbaresi",
            KalemsizFatura = "Bu fatura başlık düzeyindedir: satır kalemi olmadan, yalnız toplam tutarla kesilmiştir.",
            SayfaAciklama = "Kesilen ve taslak faturalar; durum geçişleri onay ister, kesilmiş belge yerinde değiştirilmez.",
            KesOnay = "Fatura KESİLECEK. Kesilen belge yerinde değiştirilemez; yalnız iptal edilebilir. Onaylıyor musunuz?",
            OdendiOnay = "Fatura ÖDENDİ olarak işaretlenecek. Yanlışlıkla işaretlerseniz faturayı iptal edip yeniden kesmeniz gerekir. Onaylıyor musunuz?",
            IptalOnay = "Fatura İPTAL EDİLECEK. İptal geri alınamaz ve fatura numarası serisinde kullanılmış sayılır. Onaylıyor musunuz?",
            DurumDegisti = "{0} faturası {1} durumuna geçirildi.",
            FaturaDegismez = "Fatura bir belgedir: kesilen numara vergi dairesine karşı seriyi tüketir, bu yüzden başlığı, cari bilgisi, KDV'si ve kalemleri kaydedildikten sonra yerinde değiştirilmez.",
            FaturaDegismezYolu = "Yanlış faturayı iptal edin (numara serisi kullanılmış sayılır) ve doğru bilgilerle yeni bir fatura kesin; işlem bağı varsa fişten kesme yolunu kullanın.",
            KalemDegismez = "Fatura kalemleri belgenin parçasıdır: matrah, KDV ve has bedeli bu satırlardan hesaplanmıştır, sonradan değiştirilirse belge kendi toplamıyla çelişirdi.",
            NoKesimde = "Numara kesimde verilir",
            TaslakDuzenle = "Taslak faturayı düzenle",
            TaslakGuncellendi = "Taslak fatura güncellendi.",
            KalemGuncellendi = "Fatura kalemi güncellendi; belge toplamı yeniden hesaplandı.",
            SayiGecersiz = "Geçerli bir sayı giriniz.",
            ToplamKalemden = "Toplam kalemlerden hesaplanır; kalemleri düzenleyin.",
            KesilmisDegismez = "Kesilmiş fatura değişmez: numara serisi tüketilmiş ve belge dışarıya verilmiştir. TASLAK belge ise serbestçe düzenlenir.",
            KuyumIslemZorunlu = "Kuyum KDV'sinde has bedeli defterden türetilir: önce faturalanacak işlemi seçin. İşlem bağı olmadan kuyum faturası kesilemez.",
            ToplamUyusmuyor = "Kalem toplamı fatura toplamıyla uyuşmuyor — kalemli faturada tolerans yoktur.",
            KalemEkle = "Kalem Ekle",
            KalemSil = "Kalemi Sil",
            KalemToplami = "Kalem Toplamı",
            KalemliKes = "Kalemli Fatura Kes",
            KalemGoster = "Kalemleri Göster",
            KalemBulunamadi = "Bu faturanın satır kalemi yok",
            Urun = "Ürün",
            FistenKes = "Fişten Fatura Kes",
            FisKimligi = "Fiş Kimliği",
            Onizle = "Önizle",
            FisKalemSayisi = "Fişteki kalem sayısı",
            FisOnizlemeIpucu = "Fişin TÜM kalemleri faturaya girer; kalem seçilemez. Has bedeli sunucuda hesaplanır.",
            FaturalanacakIslem = "Faturalandırılacak işlem",
            IslemSecin = "İşlem seçin",
            IslemSecilmedi = "Kuyum KDV'sinde has bedeli defterden türetilir: önce faturalandırılacak işlemi seçin.",
            FaturalanabilirYok = "Son 30 günde faturalandırılmamış satış/alış işlemi yok",
            FaturalanabilirSinir = "Listede sunucu sınırına ulaşıldı — daha fazlası var; tarih aralığını daraltın.",
            HasDegerKaynagi = "Has bedelinin kaynağı",
            KaynakHareket = "Defterden türetildi",
            KaynakElle = "Elle girildi",
            HasKuru = "Kullanılan has kuru (ağırlıklı ortalama)",
            KapsananIslem = "Kapsanan işlem sayısı",
            DefterToplami = "Defterdeki tutar",
            OnizlemeyiGuncelle = "Önizlemeyi güncelle",
            OnizlemeYok = "Matrah kırılımı için işlem seçin ve önizlemeyi güncelleyin.",
            OzelMatrahKodu = "Özel matrah neden kodu",
        },
        Ayarlar = new()
        {
            Baslik = "Ayarlar",
            ProfilBilgileri = "Profil Bilgileri",
            SifreDegistir = "Şifre Değiştir",
            MevcutSifre = "Mevcut Şifre",
            YeniSifre = "Yeni Şifre",
            SifreGuncellendi = "Şifre başarıyla güncellendi!",
            SifreDegistirilemedi = "Şifre değiştirilemedi",
            SifrelerEslesmiyor = "Şifreler eşleşmiyor",
            SifreyiGuncelle = "Şifreyi Güncelle",
            Dil = "Dil",
            Turkce = "Türkçe",
            Ingilizce = "English",
        },
        Kabuk = new()
        {
            MenuAc = "Menüyü aç",
            MenuKapat = "Menüyü kapat",
            Kaydediliyor = "Kaydediliyor…",
            SunucuOnayiBekleniyor = "Sunucu onayı bekleniyor — kaydedildi yazılmadan önce yanıt beklenir.",
            DuzenlemekIcinTikla = "Düzenlemek için tıklayın · Enter kaydeder, Esc iptal eder, alandan çıkmak da kaydeder",
            KaydetKilitli = "Kaydet kilitli",
            BosBirakilamaz = "Bu alan boş bırakılamaz.",
            SayiGecersiz = "Geçerli bir sayı yazın.",
            AramaAc = "Ara",
            AramaBaslik = "Ara ve git",
            AramaIpucu = "Kayıt ya da ekran arayın",
            AramaEnAz = "Aramak için en az {0} karakter yazın.",
            AramaSonucYok = "Eşleşen kayıt ya da ekran yok.",
            AramaGozatma = "En son kayıtlar listeleniyor — daraltmak için yazmaya başlayın.",
            AramaHata = "Arama yapılamadı",
            AramaEkranlar = "Ekranlar",
            AramaKayitlar = "Kayıtlar",
            AramaKapat = "Aramayı kapat",
            MenuAramaEtiket = "Menüde ara",
            MenuAramaKisayol = "Ctrl K",
            MenuAramaYerTutucu = "Kayıt / ekran ara",
            AramaKayitHatasi = "Kayıt araması yapılamadı (ekran araması çalışmaya devam ediyor)",
            Favoriler = "Favoriler",
            FavoriEkle = "Favorilere ekle",
            FavoriCikar = "Favorilerden çıkar",
            FavoriIpucu = "Yıldıza bastığın ekranlar menünün en üstünde toplanır. Favoriler yalnız sana özeldir; başka kullanıcı görmez.",
            SiraYukari = "Yukarı taşı (Alt + Yukarı)",
            SiraAsagi = "Aşağı taşı (Alt + Aşağı)",
            SiraTutamac = "Sürükleyerek taşı — klavyeyle Alt + Yukarı / Alt + Aşağı",
            AramaTemizle = "Aramayı temizle",
            SirayiSifirla = "Sırayı sıfırla",
            SiraGrupKilidi = "Sıralama yalnız grubun içinde yapılır; başlıkları başka gruba taşımak bu sürümde yoktur.",
            SiraKaydedilemedi = "Menü sırası kaydedilemedi",
            YetkiYuklenemedi = "Ekran yetkileri yüklenemedi — menü son bilinen hâliyle gösteriliyor.",
            YetkiYenidenDene = "Yeniden dene",
            SeciciAra = "Aramak için yazın",
            SeciciTemizle = "Seçimi temizle",
            SeciciSecili = "Seçili kayıt",
            SeciciSecimYok = "Henüz kayıt seçilmedi.",
            SeciciListeAc = "Listeyi aç",
            SeciciListeKapat = "Kapat",
            SeciciEslesmeYok = "Eşleşen seçenek yok.",
            SeciciAramaYerTutucu = "Listede ara",
            SeciciSinir = "En çok {0} kayıt gösterilir; daraltmak için yazmaya devam edin.",
            SeciciCokSecenek = "{0} seçenekten ilk {1} tanesi listelendi; aramayla daraltın.",
            SeciciSec = "Seçin",
            SeciciKilitli = "Bu alan şu an değiştirilemiyor",
            SubeKapsami = "Şube kapsamı",
            SubeTumu = "Tüm şubeler",
            SubeOturumdan = "oturumdan",
            SubeTekKaynak = "Tek şube",
            TurCari = "Cari",
            TurUrun = "Ürün",
            TurFis = "Fiş",
            TurFatura = "Fatura",
            TurBankaHareket = "Banka hareketi",
            TurHareket = "Defter satırı",
            TabloBolgesi = "Veri tablosu",
            TabloKaydirIpucu = "Tablo yatay kaydırılabilir — parmağınızla sağa/sola kaydırın (klavye: Tab ile odaklanıp ok tuşları).",
            BaglantidanSuzgec = "Süzgeç bağlantıdan geldi",
            BaglantiSuzgeciniTemizle = "Süzgeci temizle",
            SayfaHatasi = "Bu sayfa beklenmeyen bir hatayla karşılaştı; gösterilen veri eksik olabilir. Kaydettiğiniz bilgiler kaybolmaz — sayfayı yeniden deneyin.",
            SayfaHatasiYeniden = "Sayfayı yeniden dene",
            ZorunluAlan = "Bu alan zorunludur.",
            SifirdanBuyuk = "Sıfırdan büyük bir değer girin.",
            NegatifOlamaz = "Negatif değer girilemez.",
            FormEksik = "Form eksik ya da hatalı — işaretli alanları düzeltin.",
            Kaydedildi = "Kaydedildi.",
            EpostaGecersiz = "Geçerli bir e-posta girin (örnek: ad@ornek.com).",
            TelefonGecersiz = "Telefonu rakamlarla girin (10-13 hane, başında + olabilir).",
            SinirUyarisi = "En çok {0} kayıt gösterilir; daraltmak için arama ve süzgeçleri kullanın.",
            SinirAsildi = "Sınıra ulaşıldı — gösterilmeyen kayıt olabilir, aramayı daraltın.",
            PasifleriGoster = "Pasifleri göster",
            AktifeAl = "Aktife al",
            AktifeAlindi = "Kayıt yeniden aktif edildi.",
            YetkiYok = "Bu işlem için yetkiniz yok.",
            IsKurali = "İş kuralı engelledi:",
            Storno = "Storno",
            StornoBaslik = "Ters kayıtla hükümsüz kıl",
            StornoGerekce = "Storno gerekçesi",
            StornoOnay = "Bu kayıt ters bir kayıtla hükümsüz kılınacak. Kaynak satır defterde kalır, yanına ters kayıt yazılır. Bu işlem geri alınamaz.",
            StornoYapildi = "Storno kaydı yazıldı. Ters kayıt fiş no: {0}",
            StornoluKayit = "Stornolandı",
            StornoKurYok = "Güncel kur kaydı olmadan storno yazılamaz: ters kayıt bugünün kuruna bağlanır.",
            VirmanBacagi = "Virman bacağı",
            VirmanBacagiGerekce = "Bu satır bir VİRMANIN bacağıdır: tek başına stornolanamaz, yoksa paranın yalnız yarısı geri döner ve karşı taraf şişmiş kalır.",
            VirmanStornosunaGit = "Virman stornosuna git",
            PasifeAl = "Pasife al",
            PasifeAlindi = "Kayıt pasife alındı.",
            PasifeAlOnay = "Bu kayıt pasife alınacak. Geçmiş kayıtlar korunur, yalnız yeni işlemlerde seçilemez. Onaylıyor musunuz?",
            Onayla = "Onayla",
            TarihAraligi = "Bitiş tarihi başlangıç tarihinden önce olamaz.",
            DegisiklikYok = "Hiçbir alan değişmedi — kaydedilecek bir şey yok.",
            RotaYokBaslik = "Sayfa bulunamadı",
            RotaYokAciklama = "Bu adreste bir ekran yok. Adres yanlış yazılmış ya da ekran taşınmış olabilir.",
            RotaYokAdres = "İstenen adres",
            RotaYokPanele = "Panele dön",
            RotaYokBenzer = "Aradığınız bunlardan biri olabilir:",
        },
        Form = new()
        {
            SayiAnlasilmadi = "Sayı anlaşılmadı. Örnek: 12,5 ya da 12.5",
            TelefonYerTutucu = "05XX XXX XX XX",
            TelefonIpucu = "Başında 0 ile, boşluklu ya da bitişik yazabilirsiniz.",
            EpostaYerTutucu = "ad@ornek.com",
            AralikBaslangic = "Başlangıç tarihi",
            AralikBitis = "Bitiş tarihi",
            AnahtarAcik = "Açık",
            AnahtarKapali = "Kapalı",
        },
        Gorunum = new()
        {
            Baslik = "Görünüm",
            Aciklama = "Renk şeması ve yazı boyutu tüm işletme için geçerlidir; seçince anında uygulanır.",
            RenkSemasi = "Renk şeması",
            YaziOlcegi = "Yazı boyutu",
            SemaAltin = "Altın klasik",
            SemaAltinAciklama = "Bugünkü palet — kehribar vurgu.",
            SemaKurumsal = "Kurumsal mavi",
            SemaKurumsalAciklama = "Sakin, kurumsal duruş; kontrast payı en rahat şema.",
            SemaZumrut = "Zümrüt",
            SemaZumrutAciklama = "Koyu yeşil vurgu.",
            OlcekYogun = "Yoğun",
            OlcekYogunAciklama = "Bugünkü ölçek: tek ekrana çok satır sığar (gövde 14 punto).",
            OlcekOkunakli = "Okunaklı",
            OlcekOkunakliAciklama = "Gövde 16 punto; uzaktan ve küçük ekranda daha rahat okunur.",
            SahipGerekli = "Görünümü yalnız işletme sahibi değiştirebilir.",
            Yuklenmedi = "Görünüm ayarı sunucudan okunamadı; seçim kaydedilemez.",
            Kaydedildi = "Görünüm kaydedildi.",
            KontrastNotu = "Listedeki her şema WCAG 2.1 AA kontrast eşiğini (4,5:1) ölçülerek geçer.",
        },
        Tema = new()
        {
            Baslik = "Tema",
            Aciklama = "Sistem seçilirse işletim sisteminin aydınlık/karanlık tercihi izlenir.",
            Sistem = "Sistem",
            Aydinlik = "Aydınlık",
            Karanlik = "Karanlık",
        },
        Arayuz = new()
        {
            Baslik = "Arayüz modu",
            Aciklama = "Basit mod alan sayısını azaltır; hiçbir yasal kontrolü atlamaz.",
            Basit = "Basit",
            Profesyonel = "Profesyonel",
            KiraciVarsayilani = "İşletme varsayılanı",
            ModAciklamaBasit = "Tezgâh için sade yüz: az alan, büyük düğme, adım adım satış.",
            ModAciklamaPro = "Tam güç: tüm işlem türleri, karma ödeme, vade ve ayrıntılı alanlar.",
            VarsayilanBaslik = "İşletme varsayılan arayüz modu",
            VarsayilanAciklama = "Kendi tercihini belirtmeyen kullanıcılar bu modda çalışır.",
            AdimUrun = "Ürün",
            AdimMusteri = "Müşteri",
            AdimOdeme = "Ödeme",
            AdimFis = "Fiş",
            YasalKapiAcildi = "Bu işlem için ek bilgi gerekiyor; açılan alanları doldurun.",   // P-TEZGAH-TUR-KIMLIK (denetim O10): iç dil ("yasal kapı") yerine düz Türkçe
            BasitAkisEtiket = "Satış adımları",
            BasitAkisIpucu = "Ürünü okut, müşteriyi seç, fişi kaydet. Gereken olursa ek alanlar kendiliğinden açılır.",
            BasitOdemeNakit = "Ödeme: Nakit",
            SistemBelirledi = "sistem belirledi",
            ProAlanIpucu = "Ayrıntılı alanlar profesyonel moddadır; yasal bir kural gerektirdiğinde burada da açılır.",
        },
        Kur = new()
        {
            GramAltin = "Gram Altın",
            HasAltin = "Has Altın",
            Ceyrek = "Çeyrek",
            Yarim = "Yarım",
            Tam = "Tam",
            Ata = "Ata",
            Usd = "Dolar",
            Eur = "Euro",
            Gbp = "Sterlin",
            Alis = "Alış",
            Satis = "Satış",
            Kaynak = "Kaynak",
            Guncelle = "Kuru güncelle",
            Guncelleniyor = "Güncelleniyor...",
            Guncellenemedi = "Kur güncellenemedi",
            Canli = "Canlı",
            Baslik = "Kur Yönetimi",
            Aciklama = "Her işlem, o andaki kurla kaydedilir. Kur yanlışsa has hesabı da yanlış çıkar.",
            GuncelKur = "Güncel kur",
            AltinHasFiyati = "Altın has fiyatı (TL/gram)",
            GramAltinFiyati = "Gram altın satış (24 ayar)",
            KaynakDegistir = "Kaynağı değiştir",
            SimdiCek = "Şimdi çek",
            Cekiliyor = "Çekiliyor...",
            KurGuncellendi = "Kur güncellendi",
            ManuelKaydedildi = "Manuel kur kaydedildi; bundan sonraki işlemler bu kura bağlanır.",
            IptalEt = "İptal et",
            IptalBaslik = "Manuel kur kaydını iptal et",
            IptalOnay = "Bu kur kaydı GEÇERSİZ kılınacak: satır silinmez, geçmişte 'geçersiz' rozetiyle kalır ve güncel kur seçiminden çıkarılır. Kayda bağlı işlem varsa sunucu iptali reddeder.",
            GecersizRozet = "Geçersiz",
            IptalYapildi = "Kur kaydı iptal edildi; geçmişte 'geçersiz' olarak görünür.",
            IptalYok = "Yalnız ELLE girilen (MANUAL) kur kaydı iptal edilebilir. Dış kaynaktan gelen kayıt düzeltilmez; 'Şimdi çek' ile yenisi alınır.",
            KayitDegismez = "Kur kayıtları yalnız eklenir: her işlem kaydedildiği andaki kura bağlanmıştır, o kur sonradan değiştirilirse geçmiş fişlerin has hesabı da değişmiş olurdu.",
            KayitDegismezYolu = "Yanlış girdiğiniz kuru düzeltmek için doğru değerle yeni bir manuel kur girin ya da kaynaktan yeniden çekin; eski kayıt geçmişte kalır ve hangi fişin hangi kura bağlandığı okunabilir olur.",
            ManuelGiris = "Manuel Kur Gir",
            ManuelUyari = "Manuel kur yalnız internet yokken veya özel fiyat uygulanırken kullanılır; kayıt «elle girildi» diye işaretlenir ve işlem geçmişine yazılır.",
            Gecmis = "Kur geçmişi",
            Pariteler = "Sarrafiye pariteleri",
            ParitelerAciklama = "Çeyrek, yarım, tam, külçe gibi kalemlerin kaynaktaki alış/satış fiyatları.",
            Kod = "Kod",
            Ad = "Ad",
            Birim = "Birim",
            AyarFiyatlari = "Ayar bazlı gram fiyatları",
            GramFiyati = "Gram fiyatı",
            GecmisYok = "Seçilen aralıkta kur kaydı yok",
            PariteYok = "Bu kaynak parite listesi vermiyor",
            SonGuncelleme = "Son güncelleme",
            KaynakSecimi = "Kur kaynağı",
            KaynakAciklama = "Kur hangi dış kaynaktan çekilsin? Seçim işletme geneli geçerlidir ve tüm fiyatlandırmayı etkiler; değiştirdiğinizde kur hemen yeni kaynaktan çekilir.",
            KaynakYok = "Dış kaynak tanımlı değil — Manuel kur",
            KaynakYokSebep = "Dış kur kaynakları kapalı olduğu için seçilecek kaynak yok. Kuru 'Manuel Kur Gir' ile girin; kaynakları açmak yapılandırma kararıdır (Kur:SozlesmesizKaynaklarAcik).",
            KaynakTekSebep = "Tanımlı tek kaynak bu; seçenek olmadığı için liste gösterilmez.",
            KaynakKaydedildi = "Kur kaynağı {0} olarak kaydedildi.",
            CekmeKapaliSebep = "Dış kur kaynakları kapalı — çekilecek kaynak yok; 'Manuel Kur Gir' ile girin.",
            GuncellenemediRozet = "Güncellenemedi",
            KaynakAltinkaynak = "Altınkaynak",
            KaynakAnkara = "Ankara Altın Borsası",
            KaynakTruncgil = "Truncgil",
            KaynakManuel = "Elle girilen (manuel)",
        },
        Kullanici = new()
        {
            Baslik = "Kullanıcılar",
            YeniKullanici = "Yeni kullanıcı",
            AdSoyad = "Ad Soyad",
            Eposta = "E-posta",
            Rol = "Rol",
            Sifre = "Şifre",
            Durum = "Durum",
            SonGiris = "Son Giriş",
            GirisYok = "Hiç giriş yok",
            RolSahip = "Sahip",
            RolMudur = "Müdür",
            RolMuhasebe = "Muhasebe",
            RolTezgahtar = "Tezgâhtar",
            Duzenle = "Düzenle",
            RolDegistir = "Rol Değiştir",
            SifreSifirla = "Parolayı sıfırla",
            PasifeAl = "Erişimi Kapat",
            PasifeAlOnay = "Kullanıcı pasife alınacak: bir daha giriş yapamaz. Geçmiş işlemleri ve denetim izi korunur. Onaylıyor musunuz?",
            KendiniPasifeAlamaz = "Kendi erişiminizi kapatamazsınız.",
            SahipligiDevret = "Sahipliği Devret",
            SahipligiDevretAciklama = "Bu kullanıcı SAHİP olur, siz seçtiğiniz role düşersiniz. İşlem tek adımda olur: işletme hiçbir an sahipsiz kalmaz.",
            SahipligiDevretOnay = "Sahipliği devretmek üzeresiniz. Devirden sonra sahip yetkilerini kaybedersiniz ve geri almak yeni sahibin onayına bağlıdır. Onaylıyor musunuz?",
            DevirdenSonrakiRolum = "Devirden sonraki rolünüz",
            SahiplikDevredildi = "Sahiplik devredildi.",
            SifrenizDevir = "Şifreniz (devri onaylamak için)",
            TekSahipUyari = "Bu işletmenin tek SAHİBİ pasife alınamaz. Önce sahipliği devredin.",
            SifreGerekli = "Şifrenizi girin.",
            SifreDegistir = "Şifre Değiştir",
            MevcutSifre = "Mevcut Şifre",
            YeniSifre = "Yeni Şifre",
            SifreIpucu = "En az 8 karakter",
            AktifEt = "Aktifleştir",
            PasifEt = "Pasifleştir",
            Aktif = "Aktif",
            Pasif = "Pasif",
            KullaniciBulunamadi = "Kullanıcı bulunamadı",
            ListeYuklenemedi = "Kullanıcı listesi yüklenemedi",
            YenidenDene = "Yeniden dene",
            OtomatikDeneniyor = "{0} saniye sonra kendiliğinden yeniden denenecek…",
            Olusturulamadi = "Kullanıcı oluşturulamadı",
            Guncellenemedi = "Kullanıcı güncellenemedi",
            Olusturuldu = "Kullanıcı oluşturuldu!",
            Guncellendi = "Kullanıcı güncellendi!",
            SifreSifirlandi = "Şifre sıfırlandı!",
            RolGuncellendi = "Rol güncellendi!",
            SilOnay = "Bu kullanıcı pasifleştirilsin mi?",
            KendiniSilemez = "Kendi hesabınızı pasifleştiremezsiniz",
            YetkiYok = "Bu işlem için yetkiniz yok",
            Sube = "Şube",
            SubeSecin = "Şube seçin",
            SifreKuralUzunluk = "En az 8 karakter",
            SifreKuralHarf = "En az bir harf",
            SifreKuralRakam = "En az bir rakam",
        },
        Yetki = new()
        {
            Baslik = "Yetkiler",
            Aciklama = "Her rolün erişebileceği ekranları seçin. SAHİP her zaman tam yetkilidir.",
            Rol = "Rol",
            TamYetkili = "Tam yetkili",
            Kaydedildi = "Yetkiler güncellendi!",
            Kaydedilemedi = "Yetkiler kaydedilemedi",
        },
        Denetim = new()
        {
            Baslik = "Denetim İzi",
            Aciklama = "Kim, ne zaman, hangi kaydı gördü veya değiştirdi. Bu kayıt silinemez ve değiştirilemez (KVKK md.12).",
            Tarih = "Tarih",
            Kullanici = "Kullanıcı",
            Eylem = "Eylem",
            Varlik = "Kayıt türü",
            Detay = "Detay",
            Hassas = "Kişisel veri",
            ToplamKayit = "Toplam kayıt",
            HassasKayit = "Kişisel veriye erişim",
            EnEski = "En eski kayıt",
            TumEylemler = "Tüm eylemler",
            TumVarliklar = "Tüm kayıt türleri",
            YalnizHassas = "Yalnız kişisel veri erişimleri",
            DahaFazla = "Daha fazla göster",
            DegistirilemezNot = "Denetim izi yalnız eklenen bir kayıttır: kayıt eklenir, asla silinmez veya değiştirilmez.",
            Baslangic = "Başlangıç",
            Bitis = "Bitiş",
            KayitYok = "Bu filtrelerle kayıt bulunamadı.",
            SuzgeceGore = "seçili süzgeçlere göre",
            BitisGunuDahil = "Bitiş günü kapsama dâhildir.",
            Sube = "Şube",
            Kasa = "Kasa",
            TumSubeler = "Tüm şubeler",
            DamgaYok = "—",
            Storno = "Storno",
            StornoGerekce = "Storno gerekçesi (en az 20 karakter)",
            StornoOnay = "Bu kaydı ters kayıtla hükümsüz kılmak istiyor musunuz? Kaynak satır DEĞİŞMEZ.",
            StornoYapildi = "Storno yapıldı: ters kayıt deftere yazıldı, kaynak satıra dokunulmadı.",
            StornoTurDesteklenmiyor = "Bu işlem türü storno ile geri alınamaz; ekranda yazılı düzeltme yolunu kullanın.",
        },
        Uyum = new()
        {
            Baslik = "Uyum",
            Aciklama = "MASAK kimlik tespitleri, şüpheli işlem bildirimleri ve saklama süresi yönetimi.",
            KimlikTespitleri = "Kimlik Tespitleri",
            SupheliIslemler = "Şüpheli İşlemler",
            SaklamaSuresi = "Saklama Süresi",
            AcikBildirim = "Açık bildirim",
            SuresiGecmis = "Süresi geçmiş",
            Yaklasan = "Süresi yaklaşan",
            Son30Gun = "Son 30 gün tespit",
            Tarih = "Tarih",
            AdSoyad = "Ad soyad",
            BelgeTuru = "Belge türü",
            BelgeNo = "Belge no",
            IslemTutari = "İşlem tutarı",
            SaklamaBitis = "Saklama bitişi",
            Goruntule = "Görüntüle",
            BelgeNoIleAra = "Belge no ile ara",
            TamBelgeNo = "Tam belge numarası",
            AramaNotu = "Arama tam numarayla yapılır; kısmi arama kişisel veriyi korumak için kapalıdır.",
            TespitYok = "Bu aralıkta kimlik tespiti kaydı yok.",
            MaskeNotu = "Listede belge numaraları maskelidir. Tam numara yalnız kayda tıklandığında gösterilir ve bu erişim denetim izine yazılır.",
            KimlikTespitiDetay = "Kimlik tespiti",
            Kaynak = "Kaynak",
            KaynakHareket = "Defter işlemi (satış, alış, tahsilat…)",
            KaynakBankaHareket = "Banka dekontu",
            KaynakBilinmiyor = "Belirtilmemiş",
            OkumaIziNotu = "Bu kaydı görüntülemeniz denetim izine yazıldı (KVKK md.12).",
            DogumTarihi = "Doğum tarihi",
            Uyruk = "Uyruk",
            Adres = "Adres",
            Beyan = "Beyan",
            IslemAmaci = "İşlem amacı",
            FonKaynagi = "Fon kaynağı",
            EkBelge = "Ek belge referansı",
            YeniBildirim = "Yeni bildirim",
            BildirimNotu = "Şüpheli işlem bildirimi oluşturulduktan sonra 10 iş günü içinde MASAK'a iletilmelidir.",
            HareketId = "İşlem kimliği",
            Gerekce = "Gerekçe",
            EnAzKarakter = "en az {0} karakter",
            TespitTarihi = "Tespit tarihi",
            SonBildirim = "Son bildirim tarihi",
            Durum = "Durum",
            SuresiDoldu = "SÜRE DOLDU",
            GunKaldi = "gün kaldı",
            TumDurumlar = "Tüm durumlar",
            DurumAcik = "Açık",
            DurumGonderildi = "Gönderildi",
            DurumKapatildi = "Kapatıldı",
            BildirimYok = "Bu filtreyle bildirim bulunamadı.",
            GonderildiIsaretle = "Gönderildi",
            Kapat = "Kapat",
            GonderimEldeNotu = "MASAK'ın yazılımlar için bağlantı arayüzü bulunmuyor. Bildirim MASAK Online sistemine elle yüklenir; buradaki kayıt süreyi takip eder ve gönderim referansını saklar.",
            GonderimReferansiSor = "MASAK Online gönderim referansını girin:",
            KapatmaGerekcesiSor = "Kapatma gerekçesini girin:",
            AramaSonucu = "Arama sonucu: {0} kayıt · toplam {1}",
            AramaSonucYok = "Bu belge numarasına ait kimlik tespiti bulunamadı.",
            GonderimOnayBaslik = "Bildirimi gönderildi olarak işaretle",
            GonderimOnayNotu = "Bu işlem GERİ ALINAMAZ: bildirim AÇIK durumundan çıkar ve süre sayacı durur. MASAK Online'a yüklediğiniz bildirimin referans numarasını girin.",
            KapatOnayBaslik = "Bildirimi kapat",
            KapatOnayNotu = "Şüphe kalktıysa bildirim gerekçesiyle kapatılır. Kayıt SİLİNMEZ (8 yıl saklanır) ve bir daha AÇIK duruma döndürülemez.",
            ReferansZorunlu = "Gönderim referansı zorunludur.",
            GerekceEnAzHata = "Gerekçe en az {0} karakter olmalıdır.",
            BildirimOlusturuldu = "Şüpheli işlem bildirimi oluşturuldu.",
            GonderildiIsaretlendi = "Bildirim gönderildi olarak işaretlendi.",
            BildirimKapatildi = "Bildirim kapatıldı.",
            SuresiDolmusTespit = "Süresi dolmuş kimlik tespiti",
            SuresiDolmusBeyan = "Süresi dolmuş beyan",
            EnYakinSaklamaBitisi = "En yakın saklama bitişi",
            MaskelemeUyariBaslik = "Bu işlem geri alınamaz.",
            MaskelemeUyari = "Süresi dolmuş kayıtların kimlik alanları maskelenir. Tutar, tarih ve beyan kademesi korunur; kayıt silinmez. MASAK 8 yıllık saklama yükümlülüğü sona erdiği için yapılır.",
            MaskelemeOnayi = "Anlıyorum, maskelemeyi uygulamak istiyorum.",
            MaskelemeyiUygula = "Maskelemeyi uygula",
            MaskelemeSonuc = "{0} kimlik tespiti ve {1} beyan maskelendi.",
            // ── PAKET 40-A — risk skorlama ──────────────────────────────
            RiskSekmesi = "Risk",
            RiskAciklama = "Risk skoru müşterinin profilini gösterir; hiçbir işlemi DURDURMAZ. Sıkılaştırılmış tedbir ve şüpheli işlem bildirimi için yol gösterir.",
            RiskKapaliNotu = "Risk temelli tedbirler KAPALI (MasakRisk:Etkin=false). Skor hesaplanır ve gösterilir, hiçbir alanı zorunlu yapmaz. Açılması mali müşavir/uyum görevlisi teyidine bağlıdır.",
            SeviyeDusuk = "Düşük",
            SeviyeOrta = "Orta",
            SeviyeYuksek = "Yüksek",
            PepSayisi = "PEP beyanı veren cari",
            Tazelenecek = "Tazelenmesi gereken profil",
            Profilsiz = "Profili hiç hesaplanmamış cari",
            YuksekRiskliler = "Yüksek riskli cariler",
            YuksekRiskliYok = "Yüksek riskli cari yok.",
            RiskCariSec = "Cari seç",
            RiskCariSecilmedi = "Cari seçilmeden profil ve tarihçe açılmaz; bir cari seçin. Her açılış kayda geçer (KVKK md.12).",
            Puan = "Puan",
            Seviye = "Seviye",
            RiskKaynak = "Kaynak",
            KaynakHesap = "Motor hesabı",
            KaynakElle = "Elle değiştirildi",
            Sebepler = "Sebepler",
            SebepYok = "Puan üreten sebep yok.",
            YenidenHesapla = "Yeniden hesapla",
            SeviyeyiElleDegistir = "Seviyeyi elle değiştir",
            ElleBaslik = "Risk seviyesini elle değiştir",
            ElleGerekce = "Gerekçe",
            ElleGerekceEnAz = "Gerekçe en az {0} karakter olmalıdır ({1}/{0}). Gerekçesiz elle değiştirme skoru anlamsız kılar.",
            ElleSeviyeSec = "Yeni seviye",
            ProfilTarihcesi = "Profil tarihçesi (son 10)",
            ProfilTarihceYok = "Bu cari için henüz profil hesaplanmadı.",
            ProfilDegismezBaslik = "Geçmiş profil satırı değiştirilemez",
            ProfilDegismezNot = "Risk profili yalnız eklenen bir denetim kaydıdır, değiştirilmez: denetimde 'o gün hangi skorla işlem yaptın' sorusu sorulur. Düzeltme yolu: yeniden hesaplayın ya da gerekçe yazarak skoru elle belirleyin — ikisi de yeni satır yazar, eskisi durur.",
            RiskHesaplandi = "Risk profili hesaplandı: {0} ({1} puan). Yeni tarihçe satırı yazıldı.",
            RiskElleDegistirildi = "Risk seviyesi elle {0} yapıldı; denetim izine gerekçeli yazma kaydı düştü.",
            RiskListeSiniri = "Liste en çok {0} cari gösterir; daralmak için cari seçin.",
            RiskSkorDurdurmaz = "Risk skoru hiçbir satışı, MASAK ya da tevsik kapısını durdurmaz — mevzuatta doğrulanmış 'yüksek riskliyle işlem yasaktır' hükmü yoktur.",
            SebepPepBeyani = "Siyasi nüfuz sahibi beyanı",
            SebepNakitAgirlik = "Nakit ağırlıklı işlem geçmişi",
            SebepEsigeYakin = "Kimlik eşiğine yakın tekrarlayan işlem",
            SebepSupheliGecmis = "Geçmişte şüpheli işlem bildirimi",
            SebepKimlikEksik = "Eşik üstü işlemde eksik kimlik tespiti",
            SebepYuksekRiskliUlke = "Yüksek riskli ülke uyruğu",
            SebepYaptirimEslesmesi = "Yaptırım listesi eşleşmesi",
            SaklamaDayanak = "Dayanak: MASAK Tedbirler Yönetmeliği md.46/1 (8 yıl) ve KVKK md.4 (amaçla sınırlılık).",
                    EkBeyan = "Ek beyan düzenle",
            EkBeyanBaslik = "Ek beyan — gönderilmiş bildirimin düzeltmesi",
            EkBeyanAciklama = "Gönderilmiş bildirim geri alınmaz ve üzerine yazılmaz (8 yıl saklama). Düzeltme, aslına bağlı YENİ bir bildirim olarak gönderilir; ikisi de defterde kalır.",
            EkBeyanBasarili = "Ek beyan düzenlendi; asıl bildirime bağlandı.",
            EkBeyanRozet = "Ek beyan",
            GonderildiDuzeltmeYolu = "Gönderilmiş bildirimin metni değiştirilemez; düzeltme “Ek beyan düzenle” düğmesiyle yapılır.",
        },
        // ── PAKET P-17 (Y-6) — ortak A4 çıktı şablonu ──────────────────────
        Cikti = new()
        {
            Baslik = "Belge",
            Aciklama = "Satış fişi, tahsilat makbuzu, emanet teslim belgesi, cari ekstresi ve irsaliye dökümü tek A4 şablonundan basılır.",
            Yazdir = "Yazdır",
            YazdirilamadI = "Yazdırma penceresi açılamadı — tarayıcı menüsünden “Yazdır” seçeneğini kullanabilirsiniz.",
            YazdirKilidi = "Basılacak belge yüklenmedi.",
            BelgeNo = "Belge no",
            Dayanak = "Dayanak",
            Duzenleyen = "Düzenleyen",
            Tutarlar = "Tutarlar",
            DuzenlemeTarihi = "Düzenleme tarihi",
            SatirYok = "Bu belgede satır yok.",
            KimlikEksik = "(işletme unvanı girilmemiş)",
            KimlikEksikGerekce = "Belgede işletmenizin adresi ya da vergi bilgisi eksik. Bir kez girin, bütün belgelerde çıksın.",
            TuretilmisGerekce = "Bu belge deftere yazılmış bir kayıttan türetilir; belgenin kendisi düzenlenmez.",
            KaynaktaDuzelt = "Kaydı düzeltmek için kaynağına gidin (işlem defteri / cari kartı) ve oradaki düzeltme yolunu kullanın.",
            GecersizTur = "Bilinmeyen belge türü: {0}",
            BelgeAc = "Belge",
        },
        // ── PAKET P-17 (Y-7) — vitrin / kur panosu ─────────────────────────
        Pano = new()
        {
            Baslik = "Vitrin Panosu",
            Aciklama = "Müşteriye dönük alış/satış panosu. Marj yalnız bu ekranın yönetim bölümünde görünür, panoya inmez.",
            Kaynak = "Kaynak",
            Kalem = "Kalem",
            Alis = "Alış",
            Satis = "Satış",
            Durum = "Durum",
            BosDurum = "Panoda gösterilecek kalem yok. Aşağıdaki yönetim bölümünden kalem açabilirsiniz.",
            OkunabilirlikUyarisi = "Panoda {0} satır var; {1} satırın üstü uzaktan okunmayabilir.",
            TavanNotu = "Sunucu sınırı: panoda en fazla {0} satır gösterilir.",
            AtlananBaslik = "Panoya çıkmayan kalemler",
            AtlamaSebebi = "Sebep",
            SebepKotasyonYok = "Kaynak bu kalemin fiyatını vermedi.",
            SebepFiyatYok = "Kalem geldi ama alış da satış da boştu.",
            SebepTersMakas = "Alış fiyatı satış fiyatına eşit ya da ondan büyük — kaynak verisi ya da marj hatalı.",
            SebepTavanAsildi = "Panonun satır tavanı doldu.",
            SebepMarjGecersiz = "Marj fiyatı sıfıra ya da altına indiriyor — satır vitrine basılmadı. Marjı küçültün.",
            SebepBilinmeyen = "Sebep çözülemedi.",
            AyarBaslik = "Pano ayarları (marj / yuvarlama)",
            AyarAciklama = "Marj ticari sırdır ve panoya inmez; fiyat sunucuda hesaplanır, panoya yalnız sonuç gider.",
            AyarYetkiYok = "Marj ayarlarını görmek için müdür ya da sahip yetkisi gerekir.",
            Etiket = "Panodaki etiket",
            EtiketIpucu = "Boş bırakılırsa sistemin adı kullanılır: {0}",
            Sira = "Sıra",
            AlisMarj = "Alış marjı (TL)",
            AlisMarjIpucu = "Toptancıdan aşağı almak için genelde eksi değer yazılır; eksi kasıtlıdır.",
            SatisMarj = "Satış marjı (TL)",
            Yuvarlama = "Yuvarlama adımı",
            YuvarlamaKapali = "Yuvarlama yok",
            YuvarlamaGecersiz = "Yuvarlama adımı listeden seçilmelidir.",
            PanodaGoster = "Panoda göster",
            VarsayilanaDon = "Varsayılana dön",
            ZatenVarsayilan = "Bu kalem zaten varsayılan değerlerde.",
            SiraAralik = "Sıra 0-99 arasında olmalıdır.",
            MarjAralik = "Marj değeri makul aralığın dışında (±1.000.000 TL).",
            Kaydedildi = "{0} kaydedildi — panoda {1} kalem açık.",
            VarsayilanaDonuldu = "{0} varsayılan değerlerine döndürüldü.",
            VitrinAc = "Vitrin panosunu aç",
        },
        Pusula = new()
        {
            Baslik = "Gider Pusulaları",
            HurdaAlimiGir = "Hurda alımı gir",
            Aciklama = "Faturası olmayan kişilerden (örn. eski altın satan müşteri) alım yapınca kesilen belge.",
            Gecikmis = "Süresi dolmuş",
            Yaklasan = "Süresi yaklaşan",
            SureGun = "Düzenleme süresi (gün)",
            Baslangic = "Başlangıç",
            Bitis = "Bitiş",
            KayitYok = "Bu aralıkta gider pusulası yok.",
            PusulaNo = "Pusula no",
            Satici = "Satıcı",
            Tckn = "T.C. Kimlik No",
            Mal = "Mal",
            MalAciklamasi = "Mal açıklaması",
            IptalliDuzenlenemez = "İptal edilmiş pusula düzenlenemez. Doğru bilgilerle yeni bir hurda alışı girin; yeni pusula otomatik düzenlenir.",
            PusulaGuncellendi = "Pusula güncellendi.",
            AdresZorunlu = "Satıcı adresi boş bırakılamaz: gider pusulasının zorunlu unsurudur (VUK md.234).",
            MalAciklamasiZorunlu = "Mal açıklaması boş bırakılamaz: belgenin konusudur (VUK md.234).",
            Brut = "Brüt tutar",
            Stopaj = "Stopaj",
            Net = "Net ödenen",
            SonTarih = "Son düzenleme tarihi",
            Detay = "Detay",
            Yazdir = "Yazdır",
            Gecikti = "GECİKTİ",
            GunKaldi = "gün",
            MaskeNotu = "Listede T.C. kimlik numaraları maskelidir. Tam numara yalnız detayda gösterilir ve bu erişim denetim izine yazılır.",
            IptalEt = "Pusulayı iptal et",
            IptalBaslik = "Gider pusulasını iptal et",
            IptalOnay = "Belge SİLİNMEZ: 'İPTAL' yazılarak saklanır ve doğru bilgilerle YENİ bir pusula kesilir. ⚠ Bu iptal yalnız belgeyi geçersiz yapar; hurda alışının stok ve maliyet kaydı DEĞİŞMEZ.",
            IptalEdildiRozet = "İPTAL",
            IptalYapildi = "Gider pusulası iptal edildi. Doğru bilgilerle yeni bir hurda alışı girin; yeni pusula otomatik düzenlenecektir.",
            PusulaDetay = "Gider pusulası",
            OkumaIziNotu = "Bu kaydı görüntülemeniz denetim izine yazıldı (KVKK md.12).",
            Adres = "Adres",
            SaticiTipi = "Satıcı tipi",
            TeslimTarihi = "Teslim tarihi",
            Belge = "Belge",
            Dayanak = "Dayanak",
            Duzenleyen = "Düzenleyen",
            Cins = "Malın cinsi",
            Gram = "Gram",
            HasGram = "Has gram",
            Tutarlar = "Tutarlar",
            Yaziyla = "Yazıyla",
            DuzenlemeTarihi = "Düzenleme tarihi",
            DuzenleyenImza = "Düzenleyen (kaşe / imza)",
            SaticiImza = "Satıcı (imza)",
        },
        Sayim = new()
        {
            Baslik = "Sayım Oturumları", YeniOturum = "Yeni sayım", Ad = "Ad", Sube = "Şube", Kapsam = "Kapsam",
            KapsamTumu = "Tüm ürünler", KapsamReyon = "Reyon", KapsamKategori = "Kategori", Ac = "Oturumu Aç",
            Kapat = "Kapat ve Deftere Yaz", Iptal = "İptal Et", Durum = "Durum", Acik = "AÇIK", Kapandi = "KAPANDI",
            Urun = "Ürün", Sistem = "Sistem", Sayilan = "Sayılan", Guncel = "Güncel", Fark = "Fark",
            Sayilmadi = "sayılmadı", HareketVar = "oturumda hareket var", BarkodOkut = "Barkod okut / ürün seç",
            Adet = "Adet", Say = "Say", Ozet = "Özet", Eksik = "Eksik", Fazla = "Fazla", NetHas = "Net has farkı",
            HareketliUygula = "Oturum sırasında hareket gören satırları da uygula",
            HareketliUyari = "Bu satırlarda oturum açıldıktan sonra satış/alış oldu; sayılan adedin hangi anı yansıttığı belirsiz. Onaysız uygulanmaz.",
            SayilmayanUyari = "Sayılmayan satırlar SIFIRLANMAZ; deftere yazılmaz ve raporda 'sayılmadı' kalır.",
            KapatOnay = "Fark ≠ 0 satırlar deftere sayım düzeltmesi olarak yazılacak. Geri alınamaz.",
            KapanisSonucu = "{0} satır stoğa uygulandı, {1} satır atlandı.",
            IptalEdildi = "Sayım oturumu iptal edildi; hiçbir satır stoğa uygulanmadı.",
            IptalOnayBaslik = "Sayımı iptal et",
            IptalOnayMetin = "Bu sayım oturumu iptal edilecek. İşlem GERİ ALINAMAZ: sayılan satırlar stoğa uygulanmaz ve oturum yeniden açılamaz.",
            IptalOnaySayilan = "Şu ana kadar sayılan satır: {0} / {1}",
            AdGuncellendi = "Sayım oturumunun adı güncellendi.",
            AdIptalDegismez = "İptal edilmiş sayım oturumu artık değiştirilemez.",
            AdIptalAlternatif = "Yeni bir sayım oturumu açın.",
            SayfaAciklama = "Sayım oturumları; ürünü barkodu, kodu ya da adıyla arayın, sayılan adedi satırın üzerinde düzeltin.",
            SayildiBandi = "Sayıldı: {0} × {1}",
            OturumDegismez = "Sayım oturumunun adı, kapsamı ve şubesi açıldıktan sonra değiştirilemez: satır kümesi açılış anındaki stoğa göre dondurulur, kapsamı sonradan değiştirmek sayımı kendi kendisiyle çelişkiye düşürürdü.",
            OturumDegismezYolu = "Yanlış kapsamla açılan oturumu iptal edin (sayılan satırlar stoğa uygulanmaz) ve doğru kapsamla yeni bir oturum açın.",
            SatirDegismez = "Sayılmış bir satır \"sayılmadı\" durumuna geri döndürülemez: sayım, o rafın o an fiilen sayıldığının kaydıdır.",
            SatirDegismezYolu = "Yanlış adet girdiyseniz doğru adedi yeniden sayın — satır güncellenir; satırın hiç sayılmamış olması gerekiyorsa oturumu iptal edip yeniden açın.",
            IptalOnayla = "Evet, sayımı iptal et",
            OtomatikAd = "Sayım {0}",
            GramMiktarYaz = "Gramla satılan üründe gramı yazın.",
            BolumleBaslat = "Reyon ya da kategori seçerek başlat",
        },
        Fiyat = new()
        {
            Baslik = "Fiyat Listeleri ve Kampanyalar", Listeler = "Fiyat Listeleri", Kampanyalar = "Kampanyalar",
            YeniListe = "Yeni liste", YeniKampanya = "Yeni kampanya", Ad = "Ad", Tur = "Tür",
            HasMarj = "Has marjı (%)", IscilikCarpan = "İşçilik çarpanı (%)", Varsayilan = "Varsayılan", Aktif = "Aktif",
            Baslangic = "Başlangıç", Bitis = "Bitiş", Hedef = "Hedef", HedefTumu = "Tüm ürünler",
            HedefKategori = "Kategori", HedefUrun = "Ürün", IndirimTuru = "İndirim türü",
            IscilikYuzde = "İşçilikte %", TutarYuzde = "Tutarda %", TutarTl = "Sabit TL", Deger = "Değer",
            ListeFiyati = "Liste fiyatı", IndirimOncesi = "İndirim öncesi", Indirim = "İndirim", Hediye = "Hediye",
            TeklifYok = "Bu ürün için otomatik fiyat üretilemedi (gümüş ya da kur yok) — elle girin.",
            ListeYok = "Fiyat listesi tanımlı değil — Fiyat Listeleri ekranından bir varsayılan liste açın.",
            SureUyari = "Kampanya en fazla üç ay sürebilir (Fiyat Etiketi Yönetmeliği md.11/3).",
            FiyatListesi = "Fiyat listesi",
            PasifeAl = "Pasife al", Aktiflestir = "Aktifleştir",
            ListeKaydedildi = "Fiyat listesi kaydedildi", KampanyaKaydedildi = "Kampanya kaydedildi",
            BosListe = "Henüz fiyat listesi yok — Fiyat listesi tanımlanmadan POS otomatik fiyat veremez.",
            BosKampanya = "Henüz kampanya yok.",
            IlkListeVarsayilan = "İlk liste zorunlu olarak varsayılandır.",
            VarsayilanDegisir = "Bu listeyi varsayılan yaparsanız mevcut varsayılan listenin varsayılanlığı kalkar.",
            VarsayilanKorunur = "Varsayılan işareti buradan kaldırılamaz: kiracı varsayılan listesiz kalırsa POS hiçbir ürüne otomatik fiyat veremez.",
            VarsayilanDevret = "Başka bir listeyi 'Varsayılan' işaretleyip kaydedin; varsayılanlık ona geçer.",
            MarjEksiUyari = "Has marjı eksi: bu liste satışı maliyetin altına indirir.",
            MarjYuksekUyari = "Has marjı %100'ün üzerinde: fiyat has değerinin iki katını aşar.",
            CarpanYuksekUyari = "İşçilik çarpanı %200'ün üzerinde: işçilik iki katından fazla yansır.",
            RiskOnayiEtiket = "Bu değeri bilerek giriyorum",
            RiskOnayiSart = "Kaydetmek için riskli değeri onaylayın.",
            MarjAralikDisi = "Has marjı %-100 ile %500 arasında olmalıdır.",
            CarpanAralikDisi = "İşçilik çarpanı %0 ile %500 arasında olmalıdır.",
            UrunPaneli = "Bu listeyle ürün fiyatları",
            PanelIpucu = "Fiyatlar sunucuda hesaplanır; sayfa başına en fazla 50 satır gelir. Tekil ve toplu geçersiz kılma bu panelden yapılır.",
            PanelKapat = "Paneli kapat",
            SayfaAciklama = "Fiyat listeleri ve kampanyalar; her satır düzenlenebilir, pasife alınabilir ve geri açılabilir.",
            TurPerakende = "Perakende",
            TurToptan = "Toptan",
            TurOzel = "Özel",
            ListePasifeAlindi = "Fiyat listesi pasife alındı.",
            ListeAktiflestirildi = "Fiyat listesi yeniden aktif edildi.",
            KampanyaPasifeAlindi = "Kampanya pasife alındı.",
            KampanyaAktiflestirildi = "Kampanya yeniden aktif edildi.",
            TopluMarj = "Toplu marj önizlemesi (%)",
            TopluMarjNot = "Salt hesap: kaydedilmez, hiçbir ürünün fiyatını değiştirmez.",
            Ayar = "Ayar", Gram = "Has gram", HasDegeri = "Has değeri", MarjTl = "Marj (TL)", MaliyetTl = "Maliyet",
            IscilikTl = "İşçilik (TL)", SatisFiyati = "Satış fiyatı",
            SebepGumus = "Gümüş — otomatik fiyat yok",
            SebepListeYok = "Fiyat listesi yok",
            SebepFiyatYok = "Fiyat üretilemedi",
            IscilikUyari = "Sabit satış fiyatı girilmiş ürünlerde işçilik indirimi uygulanmaz.",
            GumusUyari = "Gümüş ürünler altın has kuruyla otomatik fiyatlanmaz",
            KurYok = "Güncel kur kaydı yok — fiyat önizlemesi yapılamıyor.",
            PanelBos = "Bu kiracıda listelenecek ürün yok.",
            PanelKategori = "Kategori",
            PanelAra = "Ürün ara (kod ya da ad)",
            PanelToplam = "Süzgeçe uyan toplam ürün",
            DahaFazlasiVar = "Sayfa sınırına ulaşıldı — daraltmak için kategori seçin ya da arama yapın.",
            YalnizGecersizKilinan = "Yalnız ürüne özel fiyatı olanlar",
            Kod = "Kod",
            Kaynak = "Kaynak",
            KaynakListe = "Liste",
            KaynakUrunOzel = "Ürüne özel",
            KaynakUrunSabit = "Ürün kartı sabit fiyatı",
            UruneOzel = "Bu ürüne özel",
            UruneOzelBaslik = "Ürüne özel fiyat",
            SabitFiyatAlan = "Sabit fiyat (TL)",
            NotAlan = "Not",
            EnAzBirAlan = "Sabit fiyat, has marjı ya da işçilik çarpanından en az birini girin.",
            GumusSabitUyari = "Gümüş üründe yalnız SABİT fiyat girilebilir; marj/işçilik türetmesi altın has kuruna dayanır.",
            UruneOzelKaydedildi = "Ürüne özel fiyat kaydedildi.",
            GecersizKilmayiKaldir = "Ürüne özel fiyatı kaldır",
            GecersizKilmaKaldirildi = "Ürüne özel fiyat pasife alındı — fiyat listeye döndü.",
            TopluUygulaSecili = "Seçili ürünlere toplu uygula",
            TopluKategoriye = "Bu kategori ve alt dallarına uygula",
            TopluKilit = "Önce has marjı ya da işçilik çarpanı girin; seçili ürün gerekir.",
            TopluOzetBaslik = "Etki özeti — onayınız bekleniyor (henüz hiçbir fiyat değişmedi)",
            TopluOnayla = "Onayla ve uygula",
            TopluUygulandi = "Toplu fiyat uygulandı — etkilenen ürün",
            EtkilenenUrun = "Etkilenen ürün",
            ToplamFark = "Toplam fark (TL)",
            EskiFiyat = "Eski liste fiyatı",
            YeniFiyat = "Yeni liste fiyatı",
            Fark = "Fark",
            Atlananlar = "Atlananlar",
            SebepSabitFiyat = "Ürün kartında sabit fiyat — marj etkisiz",
            SebepGecersizKilmaSabit = "Ürüne özel sabit fiyat — marj etkisiz",
            SaltOkunurBasitMod = "Liste fiyatı salt okunur. Toplu uygulama ve ürüne özel fiyat profesyonel modda açılır.",
        },
        Vade = new()
        {
            Baslik = "Gecikmiş Alacaklar",   // P-14 (Y-9): menü/üst bar ile TEK ad
            UfukGun = "Kaç gün ilerisi",
            GecikenToplam = "Vadesi Geçen",
            YaklasanToplam = "Vadesi Yaklaşan",
            VadesizToplam = "Vadesi Bilinmeyen",
            UfukOtesiToplam = "Ufuk dışı (vadesi uzak)",
            UfukOtesiAciklama = "Vadesi seçili ufuktan sonra olan açık borç",
            AcikToplam = "Toplam açık alacak",
            DonemdenBagimsiz = "(bugün itibarıyla — ufuk seçiminden bağımsız)",
            Musteri = "Müşteri",
            Telefon = "Telefon",
            Geciken = "Geciken",
            Yaklasan = "Yaklaşan",
            Vadesiz = "Vadesiz",
            EnEskiGecikme = "En Eski Gecikme",
            EnYakinVade = "En Yakın Vade",
            Gun = "gün",
            Vade = "Vade",
            VadeGun = "Vade (gün)",
            VarsayilanVadeGun = "Varsayılan vade (gün)",
            AcikTutar = "Açık Tutar",
            Durum = "Durum",
            Gecikti = "Gecikti",
            Yaklasiyor = "Yaklaşıyor",
            Vadesinde = "Vadesinde",
            KayitYok = "Vadesi geçen veya yaklaşan borç yok.",
            VadesizAciklama = "Vadesi bilinmeyen açık bakiye — bu alan eklenmeden önce yazılmış kayıtlardan geliyor; bugüne yazılmaz.",
        },
        Mutabakat = new()
        {
            Baslik = "Gün Sonu Mutabakatı",
            Gun = "Gün",
            Tahsilat = "Toplam tahsilat",
            Nakit = "Nakit",
            SatisFaturasi = "Satış faturası",
            Fark = "Fark",
            Durum = "Durum",
            Mutabik = "MUTABIK",
            Mutabakatsiz = "FARK VAR",
            Satis = "Satış",
            Adet = "adet",
            Faturali = "Faturalı",
            Faturasiz = "Faturasız",
            Hurda = "Hurda alımı",
            Pusula = "Gider pusulası",
            Stopaj = "stopaj",
            BelgesizHurda = "Belgesiz hurda alımı",
            FaturasizListe = "Faturası kesilmemiş işlemler",
            Saat = "Saat",
            Tutar = "Tutar",
            OdemeTuru = "Ödeme türü",
        },
        DisaAktarim = new()
        {
            Baslik = "Veri dışa aktar",
            Aciklama = "Verileriniz sizindir. İstediğiniz an standart CSV olarak alın, başka bir programa taşıyın.",
            YeniAktarim = "Yeni aktarım",
            Gerekce = "Gerekçe",
            GerekceOrnek = "Örn: Mali müşavire yıl sonu devir dosyası hazırlanıyor",
            GerekceIpucu = "En az 20 karakter. Bu metin denetim izine yazılır ve silinemez.",
            Kapsam = "Kapsam",
            KisiselVeri = "kişisel veri",
            TamKimlikIstiyorum = "Kimlik bilgileri MASKESİZ olsun",
            MaskeAciklama = "Varsayılan olarak TCKN, telefon ve adres maskelenir. Maskesiz aktarım KVKK md.12 kapsamında sorumluluk doğurur.",
            OnayMetniniYazin = "Onaylamak için aşağıdaki metni birebir yazın:",
            HazirlaVeIndir = "Hazırla ve İndir",
            ParolaBasligi = "Arşiv Parolası",
            ParolaUyari = "Bu parola bir daha gösterilmeyecek. Dosyayı açmak için gereklidir; dosyayla AYNI kanaldan göndermeyin.",
            Kopyala = "Kopyala",
            Kopyalandi = "Kopyalandı",
            Gecmis = "Aktarım Geçmişi",
            GecmisBos = "Henüz dışa aktarım yapılmadı.",
            TalepDuzeltmeYolu = "Dışa aktarım talebi bir kanıt kaydıdır; içeriği değiştirilmez ve listeden silinmez. Yanlış kapsamla aktarım aldıysanız doğru kapsamı seçip YENİ bir talep açın — eski dosyanın indirme bağlantısı süresi dolunca kendiliğinden geçersizleşir.",
            SatirSayisi = "Satır",
            Maskeli = "Maskeli",
            TamKimlik = "Tam kimlik",
            GiderPusulasi = "Gider Pusulaları",
        },
        Kurulum = new()
        {
            Baslik = "Kuruluş Sihirbazı",
            Aciklama = "İşletmenin ilk gününü buradan kurun: işletme ve şube, kullanıcılar, güncel kur, kasa/banka açılış bakiyesi ve müşteri açılış bakiyeleri. Açılış kayıtları deftere \"Açılış devri\" olarak düşer; para el değiştirmez.",
            AdimIsletme = "İşletme ve şube",
            AdimKullanicilar = "Kullanıcılar ve roller",
            AdimKur = "Güncel kur",
            AdimKasaBanka = "Kasa ve banka açılışı",
            AdimCariAcilis = "Müşteri açılış bakiyeleri",
            AdimStokAcilis = "Stok açılışı (bu sürümde yok)",
            AdimBitti = "Tamamlandı",
            AdimEksik = "Eksik",
            AdimAtlandi = "Atlandı",
            AdimAtla = "Bu adımı atla",
            AdimGeriAl = "Atlamayı geri al",
            AcilisBasligi = "Açılış bakiyesi",
            AcilisAciklama = "Önce KURU PROVA çalıştırın: sunucu satırları kontrol eder ve hiçbir kayıt oluşturmaz. Sonuç temizse uygulayabilirsiniz.",
            TurKasa = "Kasa",
            TurBanka = "Banka hesabı",
            TurCari = "Müşteri / cari",
            HedefSec = "Kayıt",
            TlTutar = "TL tutarı",
            HasGram = "Has gram (altın borcu)",
            SatirEkle = "Satır ekle",
            SatirSil = "Satırı çıkar",
            NegatifAcilisYok = "Kasa ve banka açılış bakiyesi negatif olamaz; eksi bakiye borçtur, cari olarak girilir.",
            ProvaCalistir = "Kuru prova çalıştır",
            ProvaBasligi = "Kuru prova sonucu",
            ProvaAciklama = "Bu adımda hiçbir kayıt oluşturulmadı.",
            ProvaTemiz = "Tüm satırlar geçerli — uygulanabilir.",
            Uygula = "Açılışı uygula",
            UygulandiMesaji = "Açılış uygulandı",
            GeriAl = "Bu açılışı geri al",
            GeriAlOnay = "Bu oturumun açılış kayıtları TERS KAYITLA geri alınacak (defterden satır silinmez). Devam edilsin mi?",
            GeriAlindiMesaji = "Açılış geri alındı (ters kayıt yazıldı)",
            Muhurle = "Kurulumu mühürle",
            MuhurOnay = "Bu işlem geri alınamaz. Mühürden sonra açılış bakiyesi bir daha girilemez, değiştirilemez ve geri alınamaz; tek düzeltme yolu storno (ters kayıt ya da cari düzeltme fişi) olur.",
            MuhurlendiMesaji = "Kurulum mühürlendi",
            MuhurGerekce = "Mühür gerekçesi",
            MuhurIsletmeAdiYaz = "Onaylamak için işletme adını birebir yazın: {0}",
            MuhurIsletmeAdiUyusmuyor = "Yazdığınız ad işletme adıyla birebir aynı değil.",
            MuhurBilgisi = "Mühür tarihi",
            PencereBitis = "Açılış penceresi bitişi",
            PencereKapandi = "Açılış penceresi kapandı — bakiye düzeltmesi artık normal defter yollarıyla yapılır.",
            LimitAsimiOnayi = "Kredi limitini aşan satırları gördüm ve açılışı onaylıyorum",
            KisiselVeriUyarisi = "Bu liste müşteri bilgisi taşır; bu görüntüleme denetim izine kaydedilir (KVKK md.12).",
            SatirNo = "Satır",
            HataKodu = "Kod",
            HataMesaji = "Sebep",
            HataYok = "Hatalı satır yok.",
            UyariBasligi = "Uyarılar (satır uygulanır)",
            SinirNotu = "Bir açılış işleminde en fazla {0} satır gönderilebilir.",
            BosDurum = "Henüz açılış satırı eklemediniz.",
            AcilisDegismez = "Açılış kaydı deftere işlenmiştir; yerinde düzeltilemez (VUK md.219). Mühürlenmeden önce bu oturumu geri alabilir, mühürden sonra cari düzeltme fişi kesebilirsiniz.",
            KurYok = "Henüz kur kaydı yok — açılış yazılamaz.",
            KurEkle = "Kur ekranına git",
            AdimaGit = "Bu adıma git",
                    NegatifAcilisGerekce = "Kasa ve banka açılışı neden eksi olamaz: kasada eksi para, banka mevduatında eksi bakiye fiziken bulunmaz. Eksi bakiye bir BORÇTUR ve borcun yeri caridir. Kredili mevduat (KMH) ayrı bir üründür; kararı beklemede (K-24) olduğu için bu kural şimdilik tutucudur ve gevşetilmemiştir.",
            HataKimligi = "Hata kimliği",
        },
        IceAktarim = new()
        {
            Baslik = "Veri İçe Aktarımı",
            Aciklama = "Eski programınızdaki müşteri ve ürün kartlarını şablona yapıştırın; önce KURU PROVA yapıp satır satır kontrol edin, sonra tek tıkla uygulayın. Geçmiş satış/kasa hareketi aktarılmaz.",
            Kaynak = "Ne aktarılacak",
            KaynakCari = "Müşteri / cari kartları",
            KaynakUrun = "Ürün kartları",
            SablonIndir = "Boş şablonu indir (CSV)",
            SablonIpucu = "Excel'de açın, kartlarınızı yapıştırın, \"Farklı Kaydet → CSV UTF-8\" ile kaydedin. Kolon adlarını değiştirmeyin.",
            DosyaSec = "CSV dosyası",
            DosyaIpucu = "Yalnız UTF-8 kodlu CSV kabul edilir; .xlsx dosyası doğrudan okunmaz.",
            YinelenenPolitikasi = "Zaten kayıtlı olan satırlar",
            PolitikaHata = "Hata say (önerilen)",
            PolitikaAtla = "Atla ve devam et",
            PolitikaIpucu = "Mevcut kaydın ÜZERİNE YAZMA seçeneği bilerek sunulmuyor: üzerine yazılan kayıt geri alınamaz.",
            ProvaCalistir = "Kuru prova çalıştır",
            ProvaBasligi = "Kuru prova sonucu",
            ProvaAciklama = "Bu adımda hiçbir kayıt oluşturulmadı. Aşağıdaki sonucu inceleyip onaylarsanız uygulanır.",
            ToplamSatir = "Toplam satır",
            GecerliSatir = "Geçerli",
            HataliSatir = "Hatalı",
            AtlananSatir = "Atlanan",
            YazilanKayit = "Yazılan kayıt",
            BilinmeyenKolon = "Tanınmayan kolonlar (yok sayılacak)",
            SatirNo = "Satır",
            HataKodu = "Kod",
            HataMesaji = "Sebep",
            HataYok = "Hatalı satır yok.",
            OnayMetniniYazin = "Uygulamak için aşağıdaki metni birebir yazın:",
            Uygula = "Onayla ve uygula",
            UygulandiMesaji = "İçe aktarım uygulandı",
            GeriAl = "Bu aktarımı geri al",
            GeriAlOnay = "Bu oturumda oluşan kayıtlar PASİFE alınacak (silinmeyecek). Devam edilsin mi?",
            GeriAlindiMesaji = "Aktarım geri alındı; kayıtlar pasife alındı",
            Iptal = "Provayı iptal et",
            IptalOnay = "Bu kuru prova iptal edilecek. Devam edilsin mi?",
            Gecmis = "İçe Aktarım Geçmişi",
            GecmisBos = "Henüz içe aktarım yapılmadı.",
            Durum = "Durum",
            Dosya = "Dosya",
            SatirlarBos = "Gösterilecek satır yok.",
            SatirDegismez = "İçe aktarım satırı",
            SatirDegismezYol = "Satır sonucu bir kanıt kaydıdır; değiştirilmez. Düzeltmek için dosyayı düzeltip yeni bir kuru prova çalıştırın.",
            GecmisHareketYok = "Geçmiş satış, fatura ve kasa hareketi İÇE AKTARILMAZ: geriye dönük hareket tevsik ve MASAK toplamlarını yanlış hesaplatır.",
            Kaydet = "Gerekçeyi güncelle",
        },
        Etiket = new()
        {
            Baslik = "Ürün Etiketleri",
            Aciklama = "Barkodlu etiket bas. Sentetik/laboratuvar üretimi taş ibaresi etikete otomatik gelir (md.11/e).",
            AramaIpucu = "Ürün adı, kod veya barkod",
            KopyaAdedi = "Her üründen kaç adet",
            TumunuSec = "Tümünü seç",
            SecimiTemizle = "Seçimi temizle",
            Yazdir = "Etiketleri Yazdır",
            Sec = "Seç",
            BarkodYok = "barkod yok",
        },
        Belge = new()
        {
            Baslik = "Belgeler",
            DosyaSec = "Dosya seç",
            Yukle = "Yükle",
            Indir = "İndir",
            PasifeAl = "Pasife al",
            Tur = "Belge türü",
            Boyut = "Boyut",
            BoyutAsildi = "Dosya çok büyük — PDF en fazla 5 MB, görsel en fazla 2 MB olabilir.",
            TurGecersiz = "Bu dosya türü kabul edilmiyor — yalnız PDF, JPEG ve PNG yüklenebilir.",
            TavanAsildi = "Belge tavanı aşıldı.",
            BelgeYok = "Bu kayda henüz belge eklenmedi",
            Yuklendi = "Belge yüklendi.",
            PasifeAlindi = "Belge pasife alındı (silinmedi, kayıt korunur).",
            TurSertifika = "Sertifika (PDF)",
            TurGorsel = "Görsel",
            TurFotograf = "Fotoğraf",
            TurEk = "Ek",
        },
        Duzeltme = new()
        {
            Baslik = "Düzeltme yolu",
            DuzenlenemezBaslik = "Bu kayıt değiştirilemez",
            AlternatifYol = "Düzeltme şöyle yapılır:",
            StornoDugme = "Düzelt (ters kayıt)",
            StornoHenuzYok = "Bu işlem türü stok ve maliyet partisi geri alması gerektirdiği için ters kayıtla (storno) hükümsüz kılınamaz. Doğru yol: satış/alış için iade işlemi girilir, diğer türlerde ters yönde yeni bir kayıt açılır ve gerekçesi açıklamaya yazılır.",
            Gerekce = "Gerekçe",
            EnAzKarakter = "en az {0} karakter",

            SerbestGerekce = "Bu kayıt yerinde düzenlenebilir.",
            DefterGerekce = "Deftere yazılmış kayıt silinmez ve yerinde değiştirilmez (VUK md.219). Düzeltme yalnız ters kayıtla yapılır.",
            DenetimGerekce = "Denetim izi yalnız eklenir, hiç değiştirilmez — kim neye baktı/ne yazdı sorusunun cevabı budur.",
            MasakGerekce = "MASAK kayıtları yükümlülük kaydıdır; içeriği sonradan değiştirilemez. Hatalı kayıt için yeni kayıt açılır ve eskisi açıklanır.",
            SistemGerekce = "Bu satırı sistem üretir (kur, maliyet partisi, oturum kaydı); elle düzeltilmez.",
            TuretilmisGerekce = "Bakiye tek tek hareketlerden türetilir; doğrudan yazılamaz. Düzeltme manuel düzeltme fişiyle yapılır.",
            KapanisGerekce = "Gün kapanışı kesinleşmiştir; sayımı ve farkı sonradan değiştirilmez.",
            SayimGerekce = "Sayım oturumu ve satırları kapandıktan sonra değişmez; yeni bir sayım oturumu açılır.",
            IceAktarimGerekce = "İçe aktarım satırı, dosyadaki bir satırın sonucudur ve kanıt kaydıdır; değiştirilmez. Düzeltmek için dosyayı düzeltip yeni bir kuru prova çalıştırın.",
            DisaAktarimGerekce = "Veri dışa aktarım talebi bir kanıt kaydıdır (KVKK md.11); değiştirilmez.",
            BelgeGerekce = "Belgenin içeriği değiştirilmez; yanlış belge pasife alınır ve doğrusu yüklenir.",
            FaturaGerekce = "Faturanın tutarı ve kalemleri yerinde düzeltilmez; iptal edilir ya da ters kayıtla düzeltilir.",
            PusulaGerekce = "Gider pusulasının içeriği basıldıktan sonra yerinde değiştirilmez (VUK md.234); belge gerekçeyle iptal edilir ve doğrusu yeniden düzenlenir.",
            UcYokGerekce = "Bu kayıt türü şu an yalnız okunabilir; düzeltme için ters kayıt ya da düzeltme fişi kullanılır.",
            StornoYokGerekce = "Bu işlem türü stok ve maliyet partisi geri alması gerektirdiği için ters kayıtla (storno) hükümsüz kılınamaz.",
            ListeBos = "Bu listede henüz kayıt yok.",
            ListeSiniri = "Sunucu bu listede en çok {0} kayıt döndürür; hepsini görmek için süzgeçle daraltın.",
            AralikDisi = "Değer {0} ile {1} arasında olmalıdır.",
        },
        SayfaDuzeltme = new()
        {
            PusulaDuzeltmeYolu = "Gider pusulası basıldıktan sonra İÇERİĞİ değiştirilmez (VUK md.234); düzeltme yolu İPTAL + YENİ PUSULA'dır: aşağıdaki 'Pusulayı iptal et' ile belgeyi gerekçeli olarak hükümsüz kılın, sonra doğru bilgilerle yeni bir hurda alışı girin. ⚠ İptal yalnız BELGEYİ hükümsüz kılar; hurda alışının stok ve maliyet kaydı DEĞİŞMEZ — mal gerçekten alınmadıysa mali müşavirinize başvurun.",
            SaticiTipiNihai = "Vergi mükellefi olmayan (zati eşya satışı)",
            SaticiTipiEsnaf = "Esnaf muaflığından yararlanan",
            SaticiTipiMukellef = "Vergi mükellefi (satıcı fatura keser)",
            IsletmeKimligi = "İşletme kimliği",
            VergiDairesi = "Vergi dairesi",
            VergiNo = "Vergi no / TCKN",
            IsletmeAdresi = "Adres",
            IsletmeTelefonu = "Telefon",
            IsletmeBilgisiEksik = "İşletmenin adresi ve vergi dairesi/numarası girilmemiş; belge bu alanlar olmadan basılıyor (VUK md.234 belge içeriği eksik kalır).",
            IsletmeKartinaGit = "İşletme bilgilerini girin",

            TespitDuzeltmeYolu = "Kimlik tespiti işlem anında alınan bir yükümlülük kaydıdır; içeriği sonradan düzeltilmez. Bilgi yanlışsa aynı müşteri için yeni bir işlem kimlik tespitiyle kaydedilir; eski kayıt saklama süresi sonunda maskelenir.",
            BildirimDuzeltmeYolu = "Bildirim kaydının tutarı ve gerekçesi sonradan değiştirilmez. Yanlış açılan bildirim, gerekçesi yazılarak KAPATILIR; doğru bilgiyle yeni bildirim açılır.",
            IslemAciklamasi = "İşlem açıklaması",
            TutarPozitif = "İşlem tutarı 0'dan büyük olmalı.",
            GerekceEksik = "Gerekçe en az {0} karakter olmalı; {1} karakter daha gerekiyor.",
            KaydetKilidi = "Kaydet, tutar ve gerekçe doğru girilince açılır.",
            BelgeTuruAdlari = new()
            {
                ["TC_KIMLIK"] = "T.C. kimlik kartı",
                ["PASAPORT"] = "Pasaport",
                ["SURUCU_BELGESI"] = "Sürücü belgesi",
                ["MAVI_KART"] = "Mavi kart",
                ["VERGI_LEVHASI"] = "Vergi levhası",
            },

            RaporAciklamasi = "Aylık özet, gün sonu mutabakatı, cari yaşlandırma ve stok değeri.",
            KarKaynagi = "Kâr kaynağı",
            KarKaynagiServis = "Kâr-zarar hesabı",
            KarKaynagiYok = "Hesaplanamadı",
            SatisAlisFarki = "Alış-satış nakit farkı",
            SatisAlisFarkiAciklama = "Satış tutarı − alış tutarı. Bu bir KÂR değildir; kâr maliyet bazlıdır ve Kâr-zarar ekranında hesaplanır.",
            NetHasHareketi = "Net has gram hareketi",
            NetHasAciklama = "Dönemdeki tüm hareketlerin has gram toplamı (giriş − çıkış). Stokta duran has gram DEĞİLDİR.",
            Veresiye = "Veresiye satış",
            TahsilatToplam = "Tahsilat",
            FarkTanimi = "FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar",
            BagsizFaturalar = "Fişe bağlanmamış faturalar",
            BagliFaturalar = "Fişe bağlı faturalar",
            FaturaNo = "Fatura no",
            RaporDuzeltmeYolu = "Rapor rakamları işlem kayıtlarından türetilir; rapor ekranında düzeltilmez. Yanlış rakamın kaynağı olan işlem, kendi ekranından ters kayıtla düzeltilir.",

            VadeAciklamasi = "Vadesi geçen ve yaklaşan müşteri borçları.",
            UfukAciklama = "Kaç gün ilerisine bakılacağı. Varsayılan 7 gün; toplam açık alacak bu seçimden bağımsızdır.",
            CariKartinaGit = "Cari kartını aç",
            TelefonuAra = "Telefonu ara",

            DenetimAciklamasi = "Kim neye baktı, kim ne yazdı — yalnız eklenen, hiç değişmeyen kayıt (KVKK md.12).",
            RolEtiketi = "Rol",
            YalnizHassasEtiket = "Yalnız kişisel veri erişimleri",
            VarlikAdlari = new()
            {
                ["Tenant"] = "İşletme",
                ["Sube"] = "Şube",
                ["Kullanici"] = "Kullanıcı",
                ["Cari"] = "Cari",
                ["CariRiskProfili"] = "Risk profili",
                ["Urun"] = "Ürün",
                ["Stok"] = "Stok",
                ["Kasa"] = "Kasa",
                ["BankaHesap"] = "Banka hesabı",
                ["FiyatListesi"] = "Fiyat listesi",
                ["Kampanya"] = "Kampanya",
                ["FiyatListesiUrunFiyati"] = "Liste ürün fiyatı",
                ["UrunKategorisi"] = "Ürün kategorisi",
                ["RolYetki"] = "Rol yetkisi",
                ["Belge"] = "Belge",
                ["Hareket"] = "İşlem (defter)",
                ["KasaHareket"] = "Kasa hareketi",
                ["BankaHareket"] = "Banka hareketi",
                ["GiderPusulasi"] = "Gider pusulası",
                ["Fatura"] = "Fatura",
                ["FaturaKalemi"] = "Fatura kalemi",
                ["DenetimKaydi"] = "Denetim kaydı",
                ["MasakKimlikTespiti"] = "Kimlik tespiti",
                ["MasakBeyani"] = "Müşteri beyanı",
                ["MasakSupheliBildirim"] = "Şüpheli işlem bildirimi",
                ["DisaAktarimTalebi"] = "Veri dışa aktarım talebi",
                ["IceAktarimOturumu"] = "Veri içe aktarım oturumu",
                ["IceAktarimSatiri"] = "İçe aktarım satırı",
                ["KurulumDurumu"] = "Kurulum durumu",
                ["AcilisKalemi"] = "Açılış bakiyesi kalemi",
                ["GunKapanisi"] = "Gün kapanışı",
                ["SayimOturumu"] = "Sayım oturumu",
                ["SayimSatiri"] = "Sayım satırı",
                ["KurSnapshot"] = "Kur kaydı",
                ["RefreshToken"] = "Oturum yenileme kaydı",
                ["StokPartisi"] = "Stok partisi",
                ["StokPartiTuketimi"] = "Stok partisi tüketimi",
                ["CariBakiye"] = "Cari bakiye",
                ["KullaniciMfa"] = "İki adımlı doğrulama",
                ["MfaKurtarmaKodu"] = "Kurtarma kodu",
                ["Reyon"] = "Reyon",
                ["Bildirim"] = "Bildirim",
                ["IsCalismasi"] = "Zamanlanmış iş",
                ["Siparis"] = "Sipariş",
                ["SiparisKalemi"] = "Sipariş kalemi",
                ["SiparisGecisi"] = "Sipariş durum geçişi",
            },

            MaliyetEksikAria = "maliyeti eksik ürün",
        },
        Isletme = new()
        {
            Baslik = "İşletme ve Şubeler",
            Aciklama = "İşletme bilgisi, abonelik planı ve şube yönetimi. Bu ekran yalnız SAHİP rolüne açıktır.",
            IsletmeBilgisi = "İşletme bilgisi",
            IsletmeAdi = "İşletme adı",
            Plan = "Plan",
            PlanBaslangic = "Başlangıç",
            PlanPro = "Pro",
            PlanKurumsal = "Kurumsal",
            PlanDegistir = "Planı değiştir",
            AiModulu = "AI modülü",
            AiAciklama = "Kapalıyken yapay zekâ destekli öneriler üretilmez ve dışarıya veri gönderilmez.",
            AiAcik = "Açık",
            AiKapali = "Kapalı",
            Subeler = "Şubeler",
            YeniSube = "Yeni şube",
            SubeAdi = "Şube adı",
            Adres = "Adres",
            Telefon = "Telefon",
            SubeDetay = "Detay",
            SubeYok = "Henüz şube tanımlanmadı",
            PasifeAl = "Pasife Al",
            PasifeAlOnay = "Şube pasife alınacak. Geçmiş hareketleri korunur, yeni işlemde seçilemez. Onaylıyor musunuz?",
            SonSubeUyarisi = "Tek aktif şube pasife alınamaz — işlem yapılacak şube kalmaz.",
            YalnizSahip = "Bu ayarı yalnız işletme sahibi değiştirebilir.",
            DurumBasligi = "İşletme durumu",
            DurumDeneme = "Deneme",
            DurumAktif = "Aktif",
            DurumSaltOkunur = "Salt okunur",
            KalanGun = "kalan gün",
            DenemeBitti = "Deneme süreniz doldu.",
            SaltOkunurUyari = "Kayıtlarınız duruyor ve görüntülenebilir; yeni kayıt yapılamaz. Süre uzatımı için yöneticinize başvurun.",
            VerileriDisaAktar = "Verilerinizi dışa aktarın",
            SureUzatmaNotu = "Süre uzatımı yalnız sistem yöneticisi tarafından yapılır.",
            YazmaKapaliKilidi = "İşletme salt okunur durumda — kaydetme kapalı",
            AbonelikBasligi = "Abonelik",
            AbonelikAciklama = "Hangi plandasınız, dönem ne zaman başladı ve ne zaman bitiyor. Plan değişimlerinin tamamı denetim izine yazılır.",
            AbonelikDonemi = "Geçerli dönem",
            AbonelikBaslangic = "Başlangıç",
            AbonelikBitis = "Bitiş",
            AbonelikSuresiz = "Süresiz",
            AbonelikKalanGun = "Kalan gün",
            AbonelikSuresiDoldu = "Abonelik döneminiz sona erdi: kayıtlarınız duruyor, görüntüleyebilir ve dışa aktarabilirsiniz; yeni kayıt için aboneliğin yenilenmesi gerekir.",
            AbonelikYok = "Bu işletme için henüz abonelik dönemi kaydı yok; plan bilgisi işletme kaydından okunuyor.",
            PlanKaynakRozeti = "abonelik kaydından",
            PlanYukseltmeKapali = "Plan yükseltme işletme hesabından yapılamaz: abonelik hakkını genişletmek yetkili operatör onayı gerektirir. Talebinizi yöneticinize iletin.",
            PlanIndirmeYok = "Daha alt bir plan yok: zaten en düşük plandasınız.",
            PlanIndirOnay = "Plan indirilecek. Verileriniz silinmez, yalnız hak kapsamı daralır. Onaylıyor musunuz?",
            AbonelikGecmisi = "Dönem geçmişi",
            AbonelikGecmisYok = "Henüz dönem kaydı yok.",
            AbonelikSinirNotu = "En yeni 50 dönem listelenir.",
            AbonelikKaynakSistem = "operatör",
            AbonelikKaynakKiraci = "işletme",
            DonemdenBagimsiz = "(bugün itibarıyla — dönemden bağımsız)",
        },
        Emanet = new()
        {
            Baslik = "Emanet",
            Aciklama = "Müşterinin size bıraktığı altın. Bu mal SİZİN DEĞİLDİR: stok değerine katılmaz, yalnız has gram bacağında izlenir.",
            Musteri = "Müşteri",
            MusteriSecin = "Müşteri seçin",
            Giris = "Emanet Al",
            Cikis = "İade Et",
            Ayar = "Ayar",
            GramAgirlik = "Gram ağırlık",
            TasAgirlik = "Taş ağırlığı",
            HasKarsiligi = "Has karşılığı",
            Aciklamasi = "Açıklama",
            ElimizdekiEmanet = "Elimizdeki emanet",
            IadeEdilecekHas = "Geri verilecek altın (g has)", // P-DIL-BORC-KASA (Y18): giriş dili "gram · ayar", has ikincil
            IadeFazlaUyari = "İade, elinizdeki emanetten fazla olamaz.",
            TasGramdanKucuk = "Taş ağırlığı, toplam gram ağırlığından küçük olmalıdır.",
            GirisKaydedildi = "Emanet teslim alındı ve müşterinin emanet defterine işlendi.",
            CikisKaydedildi = "Emanet iadesi kaydedildi.",
            HareketDegismez = "Bu satır ters kayıtla hükümsüz kılınamaz: ya zaten stornolanmıştır, ya kendisi bir storno kaydıdır, ya da çok bacaklı bir fişin parçasıdır.",
            HareketDegismezYolu = "Çok bacaklı fişte fişin tamamı için düzeltme fişi kullanılır; zaten stornolanmış satır ikinci kez stornolanmaz.",
            AciklamaGuncellendi = "Emanet hareketinin açıklaması güncellendi.",
            AciklamaKilitliAlanlar = "Gram, ayar, maden ve cari kilitlidir (emanet bakiyesini oluştururlar) — düzeltme yolu stornodur.",
            Hareketler = "Emanet hareketleri",
            HareketYok = "Bu müşteride emanet hareketi yok",
            Tur = "Tür",
            Tarih = "Tarih",
            GirisTuru = "Giriş",
            CikisTuru = "Çıkış",
            KurYok = "Güncel kur alınamadı — emanet kaydı kur anlık görüntüsü olmadan yapılamaz.",
            SubeUyarisi = "Emanet için kullanıcının şubesi tanımlı olmalıdır.",
            EmanetMulkiyetUyarisi = "⚠ Emanet altın işletmenin malı değildir; kâr-zarar ve stok değerlemesine GİRMEZ.",
            MusteriAra = "Müşteri ara (ad, telefon)",
            AramaDaraltin = "Liste sunucu sınırına (50 kayıt) değdi — aradığınız müşteri görünmüyorsa aramayı daraltın.",
            HesaplananHas = "ayar ve gramdan hesaplandı",
        },
        UrunAgaci = new()
        {
            Baslik = "Ürün Ağacı ve Reyonlar",
            Aciklama = "Kategori ağacı ürün listesini kırılımlı süzmenizi sağlar; reyon malın fiziken nerede durduğunu söyler.",
            Kategoriler = "Kategoriler",
            Reyonlar = "Reyonlar",
            ReyonSubeDegismez = "Var olan reyonun şubesi değiştirilemez; yeni şubede yeni reyon açın.",
            YeniKategori = "Yeni kategori",
            YeniReyon = "Yeni reyon",
            KategoriAdi = "Kategori adı",
            UstKategori = "Üst kategori",
            KokSeviye = "— kök seviye —",
            Sira = "Sıra",
            ReyonAdi = "Reyon adı",
            SubeDegismez = "Reyonun şubesi sonradan değiştirilmez; başka şubede yeni reyon açıp ürünleri oraya taşıyın.",
            ReyonPasifOnay = "Bu reyon pasife alınacak. Kayıt silinmez; üzerinde ürün varsa işlem reddedilir ve istediğinizde yeniden aktif edebilirsiniz.",
            ReyonTipi = "Reyon tipi",
            TipVitrin = "Vitrin",
            TipKasa = "Kasa",
            TipDepo = "Depo",
            TipCanta = "Çanta",
            Sube = "Şube",
            UrunSayisi = "ürün",
            KategoriYok = "Henüz kategori tanımlanmadı",
            ReyonYok = "Henüz reyon tanımlanmadı",
            DonguUyarisi = "Bir kategori kendi alt ağacına taşınamaz.",
            KapsamUyarisi = "⚠ Reyon artık stok adedini de bölüyor (yerleşik adet sütunu); ancak SAYIM KAPSAMI hâlâ ürün KARTINDAN okunur — reyon kapsamlı sayım, o reyonu varsayılan reyon olarak taşıyan ürünleri sayar, reyonda fiilen duran malı değil.",
                    KategoriPasifeAl = "Pasife al",
            KategoriPasifOnay = "Kategori SİLİNMEZ, pasife alınır: ürün kartlarındaki kırılım korunur ve kategori ağaçta görünmez olur. Geri dönüş yolu aynı satırdaki “Aktife al” düğmesidir.",
            KategoriBagliKayit = "Bağlı kayıt: {0} ürün · {1} aktif alt kategori",
            KategoriBagliYok = "Bağlı ürün ya da aktif alt kategori yok.",
            KategoriPasifRozet = "Pasif",
        },
        Yerlesim = new()
        {
            Baslik = "Reyon yerleşimi",
            Aciklama = "Şubedeki stoğun reyon kırılımı. Toplam adedin tek gerçeği stok kaydıdır; burada o adedin vitrin/kasa/depo dağılımı tutulur.",
            Atanmamis = "Atanmamış",
            AtanmamisIpucu = "Fiziksel yeri henüz bildirilmemiş mal. Ayrı bir kayıt değildir: şube stoğu eksi reyonlara yerleştirilmiş toplam.",
            YerlesikAdet = "Yerleşik adet",
            StokAdedi = "Şube stoğu",
            Yerlestir = "Reyona yerleştir",
            Tasi = "Reyona taşı",
            GeriAl = "Atanmamışa geri al",
            KaynakReyon = "Kaynak reyon",
            HedefReyon = "Hedef reyon",
            Reyon = "Reyon",
            ReyonIcerigi = "Reyon içeriği",
            ReyonSecimi = "Reyon (isteğe bağlı)",
            ReyonSecimiIpucu = "Boş bırakırsanız önce atanmamış mal düşülür; yetmezse reyonlar ad sırasıyla kullanılır ve size hangi reyondan düşüldüğü bildirilir.",
            Adet = "Adet",
            YerlesimYok = "Bu üründen hiçbir reyona mal yerleştirilmemiş — tamamı atanmamış.",
            ReyonBos = "Bu reyonda mal yok.",
            YetersizYerlesim = "Bu reyonda yeterli mal yok.",
            ReyonBosaltilmali = "Reyonda mal duruyor; önce malı boşaltın.",
            HedefSecilmeli = "Hedef reyon seçilmelidir.",
            AdetGecersiz = "Adet sıfırdan büyük olmalıdır.",
            AdetAtanmamisiAsiyor = "Adet atanmamış maldan fazla olamaz.",
            ReyonStokRaporu = "Reyon stok raporu",
            MadenAyriToplanir = "Farklı madenlerin gramları toplanmaz — her maden ayrı satırdadır.",
            Sinirda = "Sunucu sınırına ulaşıldı — daha fazlası var; süzgeçle daraltın.",
            Sorumlu = "Sorumlu",
            UrunKodu = "Ürün kodu",
            UrunAdi = "Ürün",
            UrunSecilmeli = "Taşınacak ürün seçilmelidir.",
        },
        KarZarar = new()
        {
            Baslik = "Kâr-zarar",
            Aciklama = "TL kârı ve HAS kârı ayrı ayrı. Altın yükselirken TL'de kâr yazıp gramda zarar etmek mümkündür — bu ekran ikisini birden gösterir.",
            TlBacagi = "TL Bacağı — muhasebenin gördüğü",
            HasBacagi = "HAS Bacağı — kuyumcunun gördüğü",
            Hasilat = "Hasılat",
            Maliyet = "Maliyet",
            Kar = "Kâr",
            Marj = "Marj",
            SatisAdedi = "Satış",
            BuAy = "Bu ay",
            GecenAy = "Geçen ay",
            Bugun = "Bugün",
            HasKaybiBasligi = "⚠ TL'de kâr, gramda ZARAR",
            HasKaybiMetin = "Bu dönemde {0} kâr yazdınız ama {1} gram has KAYBETTİNİZ. Sattığınız malın yerine aynı gramı koyamazsınız.",
            MaliyetiBilinen = "Maliyeti bilinen",
            MaliyetiBilinmeyen = "Maliyeti bilinmeyen",
            MaliyetEksik = "maliyet eksik",
            UrunBazli = "Ürün Bazlı Kârlılık",
            StokDegerlemesi = "Stok Değerlemesi",
            StokAdet = "Stok adedi",
            StokHas = "Stok has gramı",
            StokMaliyeti = "Stok maliyeti",
            VeriYok = "Bu dönemde satış yok.",
            GuvenTam = "Güvenilirlik: TAM",
            GuvenYuksek = "Güvenilirlik: YÜKSEK",
            GuvenOrta = "Güvenilirlik: ORTA",
            GuvenDusuk = "Güvenilirlik: DÜŞÜK",
            GuvenVeriYok = "Veri yok",
            BrutHasilat = "Brüt hasılat",
            Iade = "İade",
            NetHasilat = "Net hasılat",
            BrutKar = "Brüt kâr",
            DonemKari = "Dönem kârı",
            IscilikGeliri = "İşçilik geliri",
            TakasFarki = "Takas farkı",
            TakasFarkiNotu = "Takas satırının tutarı el değiştiren malın brüt değeri DEĞİL, yalnız fark TL'sidir (negatif de olabilir). Brüt değer deftere yazılmadığı için takas cirosu ölçülemez — bu yüzden takas farkı hasılata KATILMAZ, burada ayrı gösterilir.",
            KarKoprusu = "Kâr Köprüsü",
            Kalem = "Kalem",
            Tutar = "Tutar",
            Karsilastir = "Karşılaştır",
            KarsilastirmaYok = "Karşılaştırma yok",
            OncekiDonem = "Önceki dönem",
            GecenYil = "Geçen yılın aynı dönemi",
            Degisim = "Değişim",
            Storno = "Storno",
            StornoNotu = "Storno çifti (hükümsüz kılınan kayıt ve ters kaydı) ciro, adet ve kâr toplamlarına katılmaz; burada ayrıca sayılır.",
            HasKar = "HAS kâr",
            HasDefterGram = "Defter has gramı",
            HasDefterGramMaden = "Defter has gramı ({0})",
            MadenToplanmaz = "Bu dönemde birden çok maden var. Her madenin has gramı AYRI gösterilir; altın ile gümüşün gramları toplanmaz — toplamları tek bir sayı hiçbir şeyin karşılığı olmaz.",
            HasTanimNotu = "HAS hasılatı iki farklı büyüklüktür: üstteki rakam, TL hasılatın satış günü kuruyla HAS KARŞILIĞIDIR (kâr bu bacaktan ölçülür). \"Defter has gramı\" ise fiilen el değiştiren malın defterdeki has gramıdır (aylık özette görünen değer). İkisinin eşit olması beklenmez.",
            IadeAdedi = "İade adedi",
        },
        Bildirim = new()
        {
            Baslik = "Bildirimler",
            EkranAciklama = "Sistemin ürettiği uyarı ve hatırlatmalar; kapatılan bildirimler kayıtta kalır.",
            Acik = "Açık",
            Kapali = "Kapalı",
            Tumu = "Tümü",
            Onem = "Önem",
            Kritik = "Kritik",
            Uyari = "Uyarı",
            Bilgi = "Bilgi",
            Kapat = "Kapat",
            KapatBaslik = "Bildirimi kapat",
            KapatOnay = "Bu bildirim kapatılacak. Kapatılan bildirim yeniden açılamaz; koşul sürüyorsa iş yarın yeni bir bildirim üretir.",
            KapatmaNotu = "Kapatma notu (isteğe bağlı)",
            KapatildiEtiket = "Kapatıldı",
            Git = "İlgili ekrana git",
            Yenile = "Yenile",
            BosDurum = "Bekleyen bildirim yok.",
            BosDurumKapali = "Bu pencerede kapatılmış bildirim yok.",
            Kapatildi = "Bildirim kapatıldı",
            SinirUyarisi = "Sunucu bu listeyi en fazla {0} kayıtla döndürür — daha fazlası var. Daraltmak için durum sekmesini değiştirin.",
            KapaliPencere = "Kapalı bildirimler yalnız son {0} gün için listelenir. Kayıtlar SİLİNMEZ, listeden düşer.",
            DegismezGerekce = "Kapatılmış bildirim değiştirilemez: kapatma kim ve ne zaman kapattığını gösteren bir damgadır.",
            DegismezAlternatif = "Koşul sürüyorsa ilgili iş yeni bir bildirim üretir; iş çatısından şimdi de çalıştırabilirsiniz.",
            Isler = "Zamanlanmış işler",
            IslerAciklama = "İşler kendiliğinden koşar; aşağıdaki düğme aynı işi ŞİMDİ çalıştırır. Aynı gün ikinci kez bildirim üretilmez.",
            IsAdi = "İş",
            IsDurumu = "Durum",
            SonCalisma = "Son çalışma",
            SimdiCalistir = "Şimdi çalıştır",
            Calisiyor = "Çalışıyor...",
            KapaliIs = "Kapalı",
            KapaliIsIpucu = "Bu iş yapılandırmada kapalı (Isler bölümü); açılmadan çalıştırılamaz.",
            CalismaGecmisi = "Çalışma geçmişi",
            Anahtar = "Çalışma anahtarı",
            Sure = "Süre (ms)",
            UretilenBildirim = "Üretilen bildirim",
            IncelenenKayit = "İncelenen kayıt",
            HataOzeti = "Hata özeti",
            ElleTetiklendi = "elle",
            RozetEtiket = "Açık bildirim",
            YalnizYoneticiIs = "İş çatısı yönetimi yalnız işletme sahibi ve müdür içindir.",
            Ayarlar = "Zamanlanmış iş ayarları",
            AyarlarAciklama = "Bu ayarlar YALNIZ bu işletme için geçerlidir. Boş bırakılan alan kurulum varsayılanına döner. Kurulumda kapalı bir iş buradan AÇILAMAZ; yalnız açık bir iş kapatılabilir.",
            BaslangicSaat = "Turun başlayacağı saat",
            BaslangicSaatIpucu = "Zamanlanmış tur bu saatten önce koşmaz (işletme saati). Elle çalıştırma bu kuraldan etkilenmez.",
            VadeUfukGun = "Vade hatırlatma ufku (gün)",
            VadeUfukGunIpucu = "Kaç gün içinde vadesi dolacak borçlar için hatırlatma üretilsin.",
            MasakUyariGun = "MASAK bildirim uyarı eşiği (gün)",
            MasakUyariGunIpucu = "Şüpheli işlem bildiriminin son tarihine bu kadar gün kalınca uyarı üretilir.",
            IsAcikKapali = "Açık",
            AyarKaydet = "Ayarları kaydet",
            AyarVarsayilanaDon = "Varsayılana dön",
            AyarKaydedildi = "Zamanlanmış iş ayarları kaydedildi",
            AyarAraligi = "Kabul edilen aralık: {0}-{1}",
            VarsayilanDeger = "Kurulum varsayılanı: {0}",
            KiraciKapatti = "Bu işletmede kapalı",
            KiraciKapattiIpucu = "Bu işi siz kapattınız; ayarlardan yeniden açabilirsiniz.",
            YapilandirmaKapaliIpucu = "Bu iş kurulum yapılandırmasında kapalı; ekrandan açılamaz (kurulum/mevzuat kapısı).",
            AyarDegismezGerekce = "Geçmiş çalışma kayıtları değiştirilemez: her satır bir koşumun kanıtıdır.",
            AyarDegismezYolu = "Davranışı değiştirmek için yukarıdaki ayarları düzenleyin, sonra işi yeniden çalıştırın.",
        },
        Vezne = new()
        {
            Baslik = "Vezne",
            FisTuru = "Fiş türü",
            TurSatis = "Satış",
            TurTahsilat = "Tahsilat",
            TurIscilik = "İşçilik",
            TurAciklama = "Satış fişi ürün kalemi taşır; tahsilat ve işçilik fişi tek tutar taşır.",
            TurDegismez = "Fiş türü sonradan değiştirilemez. Yanlış türde açıldıysa fişi iptal edip yenisini açın.",
            TakasEmanetYok = "Takas ve emanet vezne fişi taşımaz: takas doğrudan işlem ekranından girilir, emanet giriş/çıkışında para el değiştirmez.",
            TutarGerekli = "Tutar zorunlu ve sıfırdan büyük olmalı",
            CariGerekli = "Bu fiş türü cariye yazılır: cari seçimi zorunlu",
            TutarKalemden = "Tutar kalemlerden hesaplanır",
            TekBacak = "Bu fiş türü tek ödeme aracıyla tahsil edilir.",
            EkranAciklama = "Tahsilat fişleri: hazırlayan ile tahsil eden ayrımı burada uygulanır.",
            Kuyruk = "Bekleyen fişler",
            FisiAc = "Fişi aç",
            Yeni = "Yeni fiş",
            VezneyeGonder = "Vezneye gönder",
            TahsilEt = "Tahsil et",
            Iptal = "Fişi iptal et",
            IptalOnay = "Bu fiş iptal edilecek. Kayıt silinmez, iptal gerekçesiyle saklanır. Onaylıyor musunuz?",
            Gerekce = "İptal gerekçesi",
            GerekceKisa = "Gerekçe en az 10 karakter olmalı",
            Hazirlayan = "Hazırlayan",
            TahsilEden = "Tahsil eden",
            BosDurum = "Bekleyen fiş yok",
            KendiFisin = "Kendi hazırladığınız fişi tahsil edemezsiniz",
            AyrimZorunlu = "Vezne ayrımı zorunlu",
            AyrimAciklama = "Açıkken fişi hazırlayan kişi aynı fişin parasını alamaz; parayı başka biri alır.",
            AyrimAcik = "açık",
            AyrimKapali = "kapalı",
            Durum = "Durum",
            DurumAcik = "Hazırlanıyor",
            DurumVeznede = "Veznede",
            DurumTahsil = "Tahsil edildi",
            DurumIptal = "İptal",
            TumDurumlar = "Tüm durumlar",
            Cari = "Müşteri",
            Sube = "Şube",
            SubeOturumdan = "oturumdan",
            Aciklama = "Açıklama",
            Kalemler = "Kalemler",
            KalemEkle = "Kalem ekle",
            KalemSil = "Kalemi sil",
            Urun = "Ürün",
            Adet = "Adet",
            BirimFiyat = "Birim fiyat (TL)",
            Iscilik = "İşçilik (TL)",
            KalemSayisi = "Kalem",
            Toplam = "Toplam",
            DamgaliToplam = "Damgalı tutar",
            ToplamTahmini = "Tahmini — vezneye gönderilince kesinleşir",
            ToplamDamgali = "Kesin — vezneye gönderildiği an sabitlendi",
            Bacaklar = "Ödeme bacakları",
            BacakEkle = "Bacak ekle",
            BacakSil = "Bacağı sil",
            Arac = "Ödeme aracı",
            Tutar = "Tutar (TL)",
            Kasa = "Kasa",
            KasaOtomatik = "tek kasa — otomatik seçildi",
            Kalan = "Kalan",
            KalaniAta = "Kalanı ata",
            Duzenle = "Düzenle",
            Sec = "Seç",
            SinirUyarisi = "Sunucu bu listeyi en çok {0} satırla döndürür; daha fazlası var. Durum süzgeciyle daraltın.",
            KurYok = "Canlı kur kaydı bulunamadı; tahsil edilemez",
            KalemGerekli = "En az bir kalem gerekli",
            BacakGerekli = "En az bir ödeme bacağı gerekli",
            ToplamTutmuyor = "Bacak toplamı fiş tutarına tam eşit olmalı",
            Olusturuldu = "Açıldı",
            Veznelendi = "Vezneye gönderildi",
            TahsilEdildi = "Tahsil edildi",
            IptalEdildi = "İptal edildi",
            DefterFisi = "Defter fişi",
            MutabakatGerekli = "Defterde karşılığı görünmüyor — mutabakat gerekli",
            DegismezGerekce = "Tahsil edilmiş ya da iptal edilmiş fiş değiştirilemez.",
            DegismezAlternatif = "Para iade edilecekse İşlemler ekranındaki satış iadesi yolu kullanılır.",
            VezneleDegismez = "Vezneye gönderilmiş fişin tutarı damgalanmıştır: kasada o tutar bekletildiği için kalemleri, cari ve açıklaması artık yerinde değiştirilmez.",
            VezneleDegismezYolu = "Fişi gerekçeyle iptal edin (gerekçe denetim izine düşer) ve doğru kalemlerle yeni bir fiş açın.",
            DefterKapilari = "Tahsilatta belge ve kimlik kontrolleri, gün kapanışı ve stok denetimi kendiliğinden çalışır.",
            KurTahsilAninda = "Kur, tahsilat anındaki güncel kurdur",
            Guncellendi = "Fiş güncellendi",
            AracNakit = "Nakit",
            AracKart = "Kart",
            AracHavale = "Havale/EFT",
            AracVeresiye = "Veresiye",
        },
        Evrak = new()
        {
            Baslik = "Çek / Senet Portföyü",
            TakvimBaslik = "Vade Takvimi",
            SayfaAciklama = "Müşteriden alınan çek ve senetler. Evrak girişi müşterinin borcunu kapatır; karşılıksız çıkarsa borç geri doğar.",
            TakvimAciklama = "Hangi gün ne kadar evrak vadesi var. Bir güne tıklayınca o günün evrakları listelenir.",
            Yeni = "Evrak gir",
            EvrakTuru = "Evrak türü",
            TurDegismez = "Kayıtlı evrakın türü değiştirilemez; iptal edip yenisini açın.",
            Cek = "Çek",
            Senet = "Senet",
            Durum = "Durum",
            Tutar = "Tutar",
            VadeGunu = "Vade günü",
            EvrakNo = "Evrak no",
            BankaAdi = "Banka",
            BankaSube = "Banka şubesi",
            KesideYeri = "Keşide yeri",
            KesideTarihi = "Keşide tarihi",
            BorcluAd = "Borçlu adı",
            BorcluKimlikNo = "Borçlu TCKN / VKN",
            BorcluKimlikNoIpucu = "Şifreli saklanır; arama yalnız tam numarayla yapılır.",
            KaynakCari = "Evrakı veren müşteri",
            Notlar = "Notlar",
            Kaydet = "Kaydet",
            Vazgec = "Vazgeç",
            Duzenle = "Düzenle",
            TahsileVer = "Tahsile ver",
            PortfoyeAl = "Portföye al",
            TahsilEdildi = "Tahsil edildi",
            Karsiliksiz = "Karşılıksız",
            Ciro = "Ciro et",
            CiroDevredildi = "Ciro Faz B'ye devredildi; bu sürümde evrak üçüncü kişiye devredilemez.",
            Hedef = "Para nereye girdi",
            HedefKasa = "Kasa (nakit)",
            HedefBanka = "Banka hesabı",
            Kasa = "Kasa",
            BankaHesabi = "Banka hesabı",
            Gerekce = "Karşılıksız gerekçesi",
            GerekceIpucu = "En az 20 karakter. Bu metin denetim izine yazılır.",
            OnayBaslik = "Evrakı karşılıksız işaretle",
            OnayMetin = "Bu işlem müşterinin borcunu tutarı kadar GERİ DOĞURUR ve deftere yeni bir satır yazar. Kayıt silinmez; geri alma yolu tahsil ya da storno'dur.",
            Onayla = "Evet, karşılıksız işaretle",
            KayitYok = "Portföyde evrak yok",
            KayitYokAciklama = "Müşteriden çek ya da senet aldığınızda 'Evrak gir' ile kaydedin; borcu otomatik kapanır.",
            Gecikmis = "Vadesi geçti",
            Yakinda = "Vadesi yaklaşıyor",
            Gun = "gün",
            Adet = "adet",
            Tumu = "Tümü",
            PortfoydeToplam = "Portföydeki toplam",
            TahsileVerilenToplam = "Tahsile verilen",
            BuHaftaVadeli = "Bu hafta vadesi gelen",
            KarsiliksizToplam = "Karşılıksız toplamı",
            DonemdenBagimsiz = "(bugün itibarıyla — süzgeçten bağımsız)",
            SinirUyarisi = "Sunucu bir seferde en çok {0} evrak döndürür; daha fazlası var, süzgeçle daraltın.",
            Kaydedildi = "Evrak kaydedildi",
            Guncellendi = "Evrak güncellendi",
            DurumPortfoyde = "Portföyde",
            DurumTahsileVerildi = "Tahsile verildi",
            DurumTahsilEdildi = "Tahsil edildi",
            DurumKarsiliksiz = "Karşılıksız",
            DurumCiroEdildi = "Ciro edildi",
            DegismezGerekce = "Tutar ve evrakı veren müşteri değiştirilemez: ikisi de deftere yazılmış bir borç kapama satırını doğurdu; değiştirilseydi bakiye ile defter ayrışırdı.",
            DegismezYol = "Yanlış tutar ya da yanlış müşteri girildiyse evrakı karşılıksız işaretleyin (borç geri doğar) ya da giriş hareketini storno edin; sonra doğru evrakı yeniden girin.",
            TakvimeGit = "Vade takvimi",
            ListeyeGit = "Portföy listesi",
            AralikBosBaslik = "Seçili tarih aralığında vadesi gelen evrak yok",
            AralikBosAciklama = "Portföyde evrak var ama vadeleri bu aralığın dışında. Aralığı genişletin ya da tüm portföyü görüntüleyin.",
            PortfoyToplami = "Portföydeki toplam (dönemden bağımsız)",
            TumPortfoyuGoster = "Aralığı tüm portföye genişlet",
            SuzgecUygulandi = "Vade süzgeci bağlantıdan uygulandı",
            GenisletmeSiniri = "Portföyün vade aralığı sunucu sınırından ({0} gün) uzun; tarihleri elle daraltın.",
            SuzgeciTemizle = "Vade süzgecini temizle",
            KasaSecilmedi = "Önce kasa seçin",
            BankaSecilmedi = "Önce banka hesabı seçin",
            SonDurum = "Bu evrak son durumdadır; başka bir işlem yapılamaz.",
            BakiyeSonrasi = "Müşterinin güncel TL bakiyesi: {0}",
            Eylemler = "İşlemler",
            ZorunluAlan = "Bu alan zorunludur.",
            KurYok = "Güncel kur kaydı alınamadı; kur olmadan evrak kaydedilemez.",
        },
        Siparis = new()
        {
            Baslik = "Siparişler",
            EkranAciklama = "Müşteri siparişi, dış atölye siparişi ve tamir kayıtları. Sipariş deftere yazmaz.",
            DetayEkranBaslik = "Sipariş detayı",
            DetayEkranAciklama = "Siparişin kalemleri, durumu ve söz verilen günü bu ekrandan düzenlenir.",
            Yeni = "Yeni sipariş",
            DetayVeDuzenleme = "Detay ve düzenleme — kalemler, söz verilen gün ve açıklama detay ekranından değiştirilir",
            No = "Sipariş no",
            Tur = "Tür",
            Durum = "Durum",
            Cari = "Cari",
            Sube = "Şube",
            SozVerilenGun = "Söz verilen gün",
            Aciklama = "Açıklama",
            TahminiTutar = "Tahmini tutar",
            Kalemler = "Kalemler",
            KalemEkle = "Kalem ekle",
            KalemSil = "Kalemi çıkar",
            Urun = "Ürün",
            UrundenGeldi = "Ürün katalogdan geldi; kalemin ürün bağı düzenlemede korunur.",
            Tanim = "Serbest tanım",
            Ayar = "Ayar",
            Gram = "Gram",
            Adet = "Adet",
            KalemSayisi = "Kalem",
            Olustur = "Siparişi oluştur",
            Duzenle = "Düzenle",
            DuzenlemeyiBitir = "Vazgeç",
            Kaydet = "Değişiklikleri kaydet",
            Detay = "Detay",
            GecisGecmisi = "Durum geçmişi",
            AppendOnly = "Bu döküm yalnız eklenen bir kayıttır: satırlar sonradan değiştirilmez, silinmez.",
            Gerekce = "Gerekçe",
            GecisYap = "Durumu değiştir",
            IptalBaslik = "Siparişi iptal et",
            IptalOnay = "Sipariş iptal edilecek. İptal geri alınamaz; kayıt ve gerekçe defterde kalır.",
            BosDurum = "Bu süzgeçle eşleşen sipariş yok.",
            SinirUyarisi = "Sunucu en çok {0} sipariş döndürür; daha fazlası var. Tür, durum ya da cari süzgeciyle daraltın.",
            Geciken = "Gecikmiş",
            GecikenSuzgeci = "Yalnız gecikenler",
            TumTurler = "Tüm türler",
            TumDurumlar = "Tüm durumlar",
            DegismezGerekce = "Bu sipariş TASLAK durumunda değil; alanları artık değiştirilemez.",
            DegismezAlternatif = "Yanlışlık varsa siparişi gerekçeli olarak iptal edin ve yenisini açın.",
            EmanetUyarisi = "Emanet girişi yapılmadı: tamire alınan müşteri eşyası DEFTERDE GÖRÜNMEZ. Deftere almak için Emanet ekranından emanet girişi yapın.",
            EmanetHareketId = "Emanet hareketi",
            TeklifAl = "Fiyat teklifi al",
            TeklifKaynak = "otomatik hesap",
            KurYok = "Güncel kur kaydı yok; tahmini tutar girilemez.",
            CariGerekli = "Cari seçimi zorunlu.",
            KalemGerekli = "En az bir kalem gerekli: ürün seçin ya da serbest tanım yazın.",
            GerekceKisa = "Gerekçe en az {0} karakter olmalı.",
            IzinliGecisYok = "Bu durumdan gidilebilecek başka durum yok.",
            Olusturuldu = "Sipariş oluşturuldu",
            GecisYapildi = "Durum güncellendi",
            GecisOnayBaslik = "Durum değişikliğini onaylayın",
            GecisOnayMetni = "Sipariş “{0}” durumundan “{1}” durumuna geçirilecek. Onaylıyor musunuz?",
            GeriGecisGerekce = "Geri geçiş gerekçesi (denetim izine yazılır)",
            GeriAl = "Geri al: {0}",
            TaslagaDonNotu = "Düzenlemek için önce “Taslak” durumuna geri alın; geri alma gerekçeyle kaydedilir.",
            Guncellendi = "Sipariş güncellendi",
            SubeOturumdan = "oturumdan",
            DefterEtkisiYok = "Sipariş deftere yazmaz: para, stok ve kâr-zarar etkilenmez. Tahsilat ve teslim mevcut işlem uçlarından geçer.",
            TurMusteri = "Müşteri siparişi",
            TurAtolye = "Atölye siparişi",
            TurTamir = "Tamir / servis",
            DurumTaslak = "Taslak",
            DurumOnaylandi = "Onaylandı",
            DurumAtolyede = "Atölyede",
            DurumHazir = "Hazır",
            DurumTeslimEdildi = "Teslim edildi",
            DurumIptal = "İptal",
        },
        Yonetim = new()
        {
            DisaAktarimAciklama = "Verinizin tamamını şifreli bir arşiv olarak indirin. Her aktarım gerekçesiyle birlikte geçmişe yazılır.",
            KullanicilarAciklama = "Çalışan hesapları, rolleri ve erişim durumu. Ayrılan çalışanın erişimi silinmez, kapatılır.",
            YetkilerAciklama = "Hangi rolün hangi ekranı görebileceğini belirler. Son sözü sunucu söyler; buradaki matris kapının kendisidir.",
            IsletmeSayfaAciklama = "İşletme kimliği, abonelik planı ve şubeler. Kimlik alanları belgelerde satıcı bilgisi olarak basılır.",
            AyarlarAciklama = "Kendi hesabınızın dili, teması, arayüz modu, şifresi ve iki aşamalı girişi.",
            KilavuzSayfaAciklama = "Kullanım kılavuzu ayrı sekmede açılır; aşağıdaki başlıklardan doğrudan ilgili bölüme gidebilirsiniz.",

            KilitSebebi = "Eksik olan",
            Kaydedildi = "Kaydedildi",
            ZorunluAlan = "Bu alan zorunludur",

            GerekceSayac = "{0}/{1} karakter",
            GerekceKisa = "Gerekçe en az {0} karakter olmalıdır — dışa aktarım denetim izine bu gerekçeyle yazılır.",
            KapsamSecilmedi = "En az bir kapsam seçin",
            OnayMetniEksik = "Onay metnini birebir yazın",
            KisiselVeriVarsayilanNotu = "Seçili kapsamlar KİŞİSEL VERİ içeriyor (cariler, gider pusulaları). Açılışta hepsi seçili gelir; gerekmiyorsa işareti kaldırın — dışa aktarılan her satır denetim izine yazılır.",
            TumunuSec = "Tümünü seç",
            GecmisSiniri = "Son {0} aktarım gösteriliyor.",

            Siz = "siz",
            MfaSutun = "İki aşamalı",
            MfaVar = "Açık",
            MfaYok = "Kapalı",
            MfaBilinmiyor = "Yalnız kendi hesabınız için görünür",
            KendiRolunDegismez = "Kendi rolünüzü ve erişim durumunuzu kendiniz değiştiremezsiniz: son yöneticinin kendini yetkisiz bırakması sistemi kilitler.",
            KendiRolunYolu = "Rolünüzü başka bir SAHİP değiştirebilir.",
            EpostaGecersiz = "Geçerli bir e-posta adresi girin",
            AdSoyadBos = "Ad ve soyad boş bırakılamaz",

            DegisiklikYok = "değiştirilmiş bir yetki yok",
            KaydedilmemisDegisiklik = "Kaydedilmemiş yetki değişikliğiniz var.",
            SahipKilitliGerekce = "SAHİP rolü her ekrana açıktır ve kısıtlanamaz: kendi yetkisini kapatabilen bir sahip sistemden kilitlenirdi.",
            SahipKilitliYolu = "Kısıtlamak istediğiniz kişiye MÜDÜR ya da daha düşük bir rol verin.",

            KimlikBaslik = "İşletme kimliği",
            KimlikAciklama = "Bu alanlar fatura ve gider pusulasında satıcı bilgisi olarak basılır.",
            KimlikDuzenle = "İşletme kimliğini düzenle",
            Adres = "Adres",
            Telefon = "Telefon",
            VergiDairesi = "Vergi dairesi",
            VergiNo = "Vergi no / TCKN",
            AdBosOlamaz = "İşletme adı boş bırakılamaz",
            VergiNoGecersiz = "Vergi no 10 ya da 11 hane olmalıdır",
            SubeAktifeAl = "Aktife Al",
            SubeAktifeAlmaGerekce = "Pasife alınmış şube ekrandan geri açılamıyor: sunucuda şubeyi yeniden aktifleştiren bir uç henüz yok. Sessiz bir düğme koymak yerine durumu olduğu gibi yazıyoruz.",
            SubeAktifeAlmaYolu = "Yeni bir şube açabilir ya da bu şubenin geri açılmasını talep edebilirsiniz.",
            KurKaynagi = "Kur kaynağı",
            MaliyetYontemi = "Maliyet yöntemi",
            TuretilenAyarGerekce = "Bu ayarlar kurulumda belirlenir ve tüm geçmiş maliyet hesaplarının dayanağıdır; sonradan değiştirmek geçmiş kârı geriye dönük bozar.",
            TuretilenAyarYolu = "Değişmesi gerekiyorsa kurulum desteğiyle birlikte yapılır.",
            TelefonGecersiz = "Telefon yalnız rakam, boşluk ve + ( ) - işaretleri içerebilir",

            ProfilDuzenle = "Profili düzenle",
            SifreAlanlariBos = "üç şifre alanı da doldurulmalı",

            MfaBaslik = "İki aşamalı giriş",
            MfaAciklama = "Şifrenize ek olarak telefonunuzdaki doğrulama uygulamasından 6 haneli kod istenir. Şifreniz çalınsa bile hesabınıza girilemez.",
            MfaDurumEtkin = "Açık",
            MfaDurumKapali = "Kapalı",
            MfaDurumBekliyor = "Kurulum yarım kaldı",
            MfaKur = "İki aşamalı girişi kur",
            MfaYarimBaslik = "Kurulum yarım kaldı",
            MfaYarimAciklama = "Daha önce bir kurulum başlatılmış ama doğrulanmamış. Girişiniz bu yüzden kod SORMAZ. İsterseniz baştan kurun, isterseniz yarım kaydı temizleyin.",
            MfaSifirla = "Yarım kaydı temizle",
            MfaSifirlandi = "Yarım kalan kurulum temizlendi.",
            MfaAdim1 = "1. Doğrulama uygulamanıza (Google Authenticator, Microsoft Authenticator vb.) aşağıdaki anahtarı elle ekleyin.",
            MfaAdim2 = "2. Uygulamanın gösterdiği 6 haneli kodu girin.",
            MfaAdim3 = "3. Kurtarma kodlarınızı kaydedin — telefonunuzu kaybederseniz tek giriş yolunuz bunlardır.",
            MfaAnahtar = "Kurulum anahtarı",
            MfaAnahtarGizli = "Anahtar gizli — göstermek için tıklayın",
            MfaAnahtarGoster = "Anahtarı göster",
            MfaAnahtarGizle = "Anahtarı gizle",
            MfaOtpauth = "Bağlantı (otpauth)",
            MfaKod = "Doğrulama kodu",
            MfaKodIpucu = "6 hane",
            MfaDogrula = "Kodu doğrula",
            MfaKurtarmaBaslik = "Kurtarma kodları",
            MfaKurtarmaUyari = "Bu kodlar YALNIZ BİR KEZ gösterilir. Her biri tek kullanımlıktır. Ekranı kapatmadan güvenli bir yere kaydedin.",
            MfaKurtarmaIndir = "Kodları indir",
            MfaKurtarmaOnay = "Kurtarma kodlarını kaydettim",
            MfaTamamla = "Kurulumu tamamla",
            MfaKuruldu = "İki aşamalı giriş açıldı",
            MfaKapat = "İki aşamalı girişi kapat",
            MfaKapatAciklama = "Kapatmak için şifreniz VE geçerli bir doğrulama kodu (ya da kurtarma kodu) gerekir.",
            MfaKapatOnay = "İki aşamalı girişi kapatmak üzeresiniz. Hesabınız yalnız şifreyle korunacak.",
            MfaKapatildi = "İki aşamalı giriş kapatıldı",
            MfaSifre = "Şifreniz",
            MfaKalanKod = "Kalan kurtarma kodu: {0}",
            MfaKurtarmaYenile = "Kurtarma kodlarını yenile",
            MfaKilitli = "Çok fazla hatalı deneme yapıldı; hesap geçici olarak kilitli. Kilit süresi dolunca yeniden deneyin.",
            MfaKodEksik = "doğrulama kodu",
            MfaSifreEksik = "şifre",
            MfaGirisBaslik = "İkinci adım",
            MfaGirisAciklama = "Doğrulama uygulamanızdaki 6 haneli kodu ya da bir kurtarma kodunu girin.",
            MfaGirisDogrula = "Doğrula ve gir",
            MfaGirisVazgec = "Vazgeç",
            MfaGizUyari = "Anahtarı kimseyle paylaşmayın; bu anahtar şifreniz kadar değerlidir.",
                    KurKaynagiSec = "Kur kaynağı seç",
            MaliyetYontemiSec = "Maliyet yöntemi seç",
            SaltOkunurKilit = "İşletme salt okunur durumda; bu alan değiştirilemez.",
            MaliyetFifo = "FIFO — ilk giren ilk çıkar",
            MaliyetLifo = "LIFO — son giren ilk çıkar",
            MaliyetOrtalama = "Ağırlıklı ortalama",
            AyarBelirlenmedi = "henüz belirlenmedi — sistem varsayılanı kullanılıyor",
            MaliyetYontemiNotu = "Satılan malın maliyeti bu sırayla düşülür. Değişiklik yalnız bundan sonraki satışlara uygulanır; kapanmış partiler yeniden hesaplanmaz.",
            KurKaynagiNotu = "Has altın fiyatı bu kaynaktan okunur. Kaynak değiştirilince kur hemen yeniden çekilir.",
        },
        Kunye = new()
        {
            Baslik = "Sistem künyesi",
            Aciklama = "Destek çağrısında bu künyeyi ve hata ekranındaki destek kodunu iletin; hangi sürümün ve hangi şemanın çalıştığı tahminle değil, ürünün kendi beyanıyla belirlenir.",
            Surum = "Sürüm",
            Derleme = "Derleme zamanı (UTC)",
            Ortam = "Ortam",
            OrtamUretim = "Üretim",
            OrtamGelistirme = "Geliştirme",
            MigrationSayisi = "Uygulanmış şema adımı",
            SonMigration = "Son şema adımı",
            BekleyenMigration = "Bekleyen şema adımı",
            SemaGuncel = "Şema güncel",
            SemaEski = "Şema güncel değil — bekleyen şema adımı var",
            Kopyala = "Kopyala",
            Kopyalandi = "Kopyalandı",
            KopyalanamadiElle = "Kopyalanamadı — kodu seçip elle kopyalayın",
            DestekKodu = "Destek kodu",
            Ayrinti = "Şema adımı ayrıntısı",
            AyrintiSahip = "Ayrıntı yalnız işletme sahibine gösterilir.",
            DerlemeKaynagiDamga = "yayın damgası",
            DerlemeKaynagiDosya = "dosya zamanı (geliştirme derlemesi)",
            Yuklenemedi = "Sistem künyesi okunamadı",
        },
        Yedek = new()
        {
            Baslik = "Yedekler",
            Aciklama = "Yedeğin alındığı değil, GERİ YÜKLENEBİLDİĞİ kanıtlanır: her yedek şifrelenir, dış kopyası alınır ve aylık tatbikatta ayrı bir veritabanına geri yüklenip sağlama toplamları karşılaştırılır.",
            SistemGeneliUyari = "Bu, tüm işletmeleri (kiracıları) kapsayan SİSTEM yedeğidir; tek bir işletmenin yedeği değildir. Dosya indirme yolu bilinçli olarak yoktur.",
            AnahtarUyari = "Yedek şifreleme anahtarı kaybolursa tüm yedekler açılamaz. Anahtarın sunucu dışında güvenli bir kopyası bulunmalıdır.",
            SonYedek = "Son yedek",
            Boyut = "Boyut",
            Dogrulama = "Doğrulama",
            DisKopya = "Dış kopya",
            RpoRto = "RPO / RTO",
            RpoOlculen = "Son yedekten bu yana (ölçülen)",
            RpoTaahhut = "RPO taahhüdü",
            RtoTaahhut = "RTO taahhüdü",
            RtoOlculen = "Son tatbikatta ölçülen geri yükleme",
            Olculmedi = "ölçülmedi",
            DurumBekliyor = "Bekliyor",
            DurumDogrulandi = "Doğrulandı",
            DurumUyusmadi = "Uyuşmadı",
            KopyaYok = "Yok",
            KopyaVar = "Kopyalandı",
            KopyaHata = "Hata",
            DogrulanmamisUyari = "Son yedek henüz doğrulanmadı ya da doğrulama uyuşmadı. Doğrulanmamış yedek, yedek sayılmaz.",
            Kosular = "Yedek koşuları",
            KosuBos = "Henüz yedek koşusu yok.",
            Tatbikatlar = "Geri yükleme tatbikatları",
            TatbikatBos = "Henüz tatbikat kaydı yok.",
            TatbikatAciklama = "Tatbikat, yedeği AYRI bir veritabanına geri yükler ve tablo sağlama toplamlarını karşılaştırır; kaynak veritabanına hiçbir aşamada yazmaz.",
            Uyusan = "Uyuşan",
            Uyusmayan = "Uyuşmayan",
            SureSn = "Süre (sn)",
            SemaDusuruldu = "Tatbikat şeması düşürüldü",
            Tur = "Tür",
            TabloSayisi = "Tablo",
            ElleYedek = "Elle yedek al",
            ElleYedekAciklama = "Bu düğme sunucuda komut ÇALIŞTIRMAZ; kuyruğa bir talep yazar ve zamanlayıcı en geç 60 saniye içinde yedeği alır.",
            Gerekce = "Gerekçe",
            GerekceOrnek = "Örn. aylık denetim öncesi elle yedek alınıyor",
            TalepGonder = "Yedek talebi oluştur",
            Talepler = "Yedek talepleri",
            TalepBos = "Bekleyen ya da geçmiş yedek talebi yok.",
            TalepAcildi = "Yedek talebi kuyruğa alındı",
            IptalEt = "Talebi iptal et",
            IptalGerekce = "İptal gerekçesi",
            IptalOnayBaslik = "Yedek talebini iptal et",
            IptalOnayMetin = "Talep silinmez; durumu İPTAL olarak işaretlenir ve kim iptal etti izi kalır.",
            IptalEdildi = "Talep iptal edildi",
            IndirmeYok = "Yedek dosyası uygulamadan indirilemez: dosya tüm işletmelerin verisini taşır. Geri yükleme yalnız sunucuda, runbook ile yapılır.",
            SaklamaNotu = "Yedekler {0} gün dönüşümlü saklanır. Bu bir KAYIT saklama süresi değildir: defter ve belge verisi yasal süresi boyunca üretimde durur.",
            Sifreli = "Şifreli",
            SifreliDegil = "ŞİFRESİZ",
                    IptalGerekceEtiket = "İptal gerekçesi",
        },
        Aylar = new[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" },
        AylarKisa = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" },
    };

    public static readonly Sozluk En = new()
    {
        DilKodu = "en",
        Ortak = new()
        {
            Kaydet = "Save",
            Durum = "Status",
            Kaydediliyor = "Saving…",
            Iptal = "Cancel",
            Sil = "Delete",
            Duzenle = "Edit",
            Ekle = "Add",
            Ara = "Search",
            Istege = "optional",
            YeniSekmedeAc = "Open the guide in a new tab",
            KilavuzAciklama = "The user guide opens in a separate page. Security settings (framing protection) do not allow embedding the guide inside the application; you can jump directly to a section from the list below.",
            KilavuzBolumler = "Sections",
            OturumSuresiDoldu = "Your session has expired — please sign in again.",
            SozlesmesizKaynakUyari = "This rate source has no contract — pilot use only. Commercial use requires written permission or a subscription from the provider.",
            DisKaynakKapali = "External rate sources are disabled. Enter the rate manually in the form below.",
            Geri = "Back",
            Tumu = "All",
            Detay = "Details",
            Kapat = "Close",
            Evet = "Yes",
            Hayir = "No",
            Aktif = "Active",
            Pasif = "Inactive",
            Yukleniyor = "Loading...",
            Baslangic = "Start",
            Bitis = "End",
            Hata = "An error occurred",
            Basarili = "Operation successful",
            Bulunamadi = "No records found",
            Aciklama = "Description",
            Opsiyonel = "optional",
            Toplam = "Total",
            Tarih = "Date",
            Tutar = "Amount",
            Adet = "Qty",
            Degistir = "Change",
            CikisYap = "Log out",
            Gecmis = "History",
            Eylemler = "Actions",
            Yenile = "Refresh",
            DegerYok = "no value",
        },
        Gider = new()
        {
            Baslik = "Expenses and Transfers",
            SayfaAciklama = "Shop expenses and money moved between cash registers and bank accounts. A transfer is not revenue: the money does not leave the shop, it only changes place.",
            SekmeGiderler = "Expenses",
            SekmeKategoriler = "Expense Categories",
            SekmeVirman = "Transfers",
            Kategori = "Category",
            KategoriAdi = "Category name",
            YeniKategori = "New category",
            KategoriYok = "No expense category yet. Create one first (rent, electricity, payroll…).",
            KategoriKaydedildi = "Expense category created",
            KategoriGuncellendi = "Expense category updated",
            KategoriDuzenle = "Edit category",
            KategoriPasifeAl = "Deactivate",
            KategoriAktifeAl = "Activate",
            KategoriPasifNotu = "No new expense can be written to an inactive category; past expenses keep their category.",
            KategoriSilinmez = "An expense category is NEVER deleted; it is deactivated, so past expense rows keep their class.",
            KullanimAdedi = "Expense count",
            KategoriToplami = "Category total",
            YeniGider = "New expense",
            GiderKaydet = "Save expense",
            GiderKaydedildi = "Expense saved",
            GiderYok = "No expense records in the selected range.",
            Kasa = "Cash register",
            ToplamGider = "Total expenses",
            Storno = "Reverse",
            StornoOnay = "This expense will be voided with a reversing row. Nothing is deleted; it stays visible in history. Confirm?",
            StornoGerekcesi = "Reversal reason (at least 20 characters)",
            Stornolandi = "Reversed",
            StornoSatiri = "Reversal row",
            StornoYapildi = "Expense reversed",
            GiderDegismezNotu = "An expense amount is never edited in place; the correction path is REVERSE + new entry.",
            VirmanHedef = "Target",
            VirmanBaslik = "Transfer (register ↔ bank)",
            VirmanAciklama = "Money changes place inside the shop: both legs are written in one operation and count as neither revenue nor expense.",
            Kaynak = "Source",
            Hedef = "Target",
            VirmanYap = "Save transfer",
            VirmanKaydedildi = "Transfer saved",
            VirmanYok = "No transfers yet.",
            VirmanStornoOnay = "BOTH LEGS of the transfer will be reversed together. Confirm?",
            VirmanStornoYapildi = "Transfer reversed (both legs together)",
            SubelerArasi = "Between branches",
            TurKasa = "Cash register",
            TurBanka = "Bank account",
            IkiBacakNotu = "A single leg of a transfer cannot be reversed; both legs return together.",
            KilitKategoriSec = "No expense category selected",
            KilitKasaSec = "No cash register selected",
            KilitTutar = "Amount must be greater than zero",
            KilitKaynakSec = "No source selected",
            KilitHedefSec = "No target selected",
            KilitAyniTaraf = "Source and target cannot be the same",
            KilitGerekce = "Reason must be at least 20 characters",
            DahaFazlasiVar = "Limit reached — more records exist; narrow the date range.",
            KasaBakiyesi = "Register balance",
            KasaBakiyeEksi = "Negative balance",
            KilitBakiyeYetersiz = "Amount exceeds the register balance ({0} available)",
            YetersizBakiye = "Not enough cash in '{0}': {1} available, {2} requested. A cash register cannot go negative.",
            YetersizBakiyeGenel = "Not enough cash in the register. A cash register cannot go negative.",
            KasayaGirisYap = "Add cash to register",
            VirmanYapBaglanti = "Make a transfer",
            EksiKasaUyari = "This register has a negative balance ({0}). Add cash or transfer money before recording an expense.",
            KasaRaporunaGit = "Open register report",
        },
        Hurda = new()
        {
            Baslik = "Scrap Stock and Refinery Outflow",
            SayfaAciklama = "Fine-gold balance of scrap accumulated in the shop and its outflow to the refinery. The balance is not a separate column: it is derived from SCRAP PURCHASE and SCRAP SALE ledger rows.",
            BakiyeBasligi = "Scrap on hand",
            BakiyeYok = "No scrap movement in this branch: no scrap purchase has been entered yet.",
            BakiyeKaynagi = "Derived from the ledger",
            HasGram = "Fine grams",
            GirenHas = "Fine grams in",
            CikanHas = "Fine grams out",
            Maden = "Metal",
            YeniSatis = "Refinery outflow (scrap sale)",
            CikanHasGram = "Fine grams out",
            BirimFiyat = "Unit price (₺ / fine gram)",
            FiyatKurdan = "From live rate",
            FiyatElle = "Entered manually",
            Ramatci = "Refinery account",
            Kasa = "Collection register",
            OdemeTuru = "Collection method",
            TahsilEdilen = "Collected (₺)",
            Tutar = "Amount",
            Kaydet = "Save refinery outflow",
            Kaydedildi = "Scrap sale saved",
            KalanBakiye = "Remaining scrap",
            DefterBasligi = "Scrap ledger",
            DefterYok = "No scrap movement in this branch.",
            FireNotuBasligi = "Refining loss",
            DegismezNotu = "A scrap sale cannot be edited in place and cannot be reversed in this version: scrap has no stock row and the reversal engine does not cover this line. A wrong outflow is corrected by a reconciliation record with the refinery, not by entering the opposite row; until that path is opened the record stands as written.",
            KilitKurYok = "No exchange-rate snapshot yet (wait a few seconds)",
            KilitBakiyeYok = "No scrap balance for this metal",
            KilitKasaYok = "No collection register selected",
            TurAlis = "Scrap purchase",
            TurSatis = "Scrap sale",
            TurBasligi = "Type",
            AracNakit = "Cash",
            AracKart = "Card",
            AracHavale = "Bank transfer",
            BizimKaydimiz = "Our record (fine grams)",
            AliciKabul = "Fine grams accepted by the buyer",
            AliciKabulNotu = "Fine weight on the refiner's assay report. If left empty our own record is taken as accepted and no difference leg is written.",
            TahlilFarki = "Assay difference",
            TahlilFarkiGerekce = "Reason for the assay difference",
            TahlilFarkiNotu = "The difference is written on its own line and moves no money. It is NOT a refining loss: it is the gap between the buyer's assay and our own record.",
            FarkLehte = "In our favour",
            FarkAleyhte = "Against us",
            FarkYok = "No difference: the buyer accepted our record as is.",
            TurTahlilFarki = "Assay difference",
            ToplamFark = "Total assay difference",
            Duzelt = "Correct the assay difference",
            DuzeltBaslik = "Assay difference correction",
            EkFark = "Additional difference (fine grams; negative = against us)",
            DuzeltmeKaydedildi = "Assay difference correction saved",
            DuzeltmeNotu = "The ledger row is never overwritten: a second difference leg is appended to the same scrap sale and the total is the sum of the legs. This path does not change the collected amount; use the customer-account correction voucher for the cash side.",
            KilitGerekce = "The reason must be at least 20 characters",
            KilitFarkSifir = "The additional difference cannot be zero",
        },
        Nav = new()
        {
            Panel = "Dashboard",
            Cariler = "Accounts",
            Urunler = "Products",
            Islemler = "Transactions",
            Kasalar = "Cash Registers",
            Pos = "POS Terminals",
            PosTahsilat = "POS Settlements",
            Banka = "Bank",
            Faturalar = "Invoices",
            Raporlar = "Reports",
            IslemDefteri = "Movement Listing",
            SatisAnalizi = "Sales Analysis",
            KasaRaporu = "Cash Movements",
            Kullanicilar = "Users",
            Yetkiler = "Permissions",
            DenetimIzi = "Audit Log",
            VeriDisaAktarim = "Data Export",
            VeriIceAktarim = "Data Import",
            VeriAktarimi = "Data Transfer",
            Kurulum = "Setup Wizard",
            Etiketler = "Product Labels",
            UrunAgaci = "Product Tree",
            Emanet = "Safekeeping",
            Isletme = "Business",
            Kur = "Rates",
            KarZarar = "Profit & Loss",
            VadeTakibi = "Overdue Receivables",
            Fiyatlandirma = "Price Lists",
            Sayim = "Stock Count",
            Uyum = "Compliance",
            GiderPusulalari = "Expense Vouchers",
            Gider = "Expenses and Transfers",
            Hurda = "Scrap and Refinery",
            Kilavuz = "Guide",
            Ayarlar = "Settings",
            Bildirimler = "Notifications",
            Siparisler = "Orders",
            Vezne = "Cashier desk",
            PusulaBelgesi = "Expense Voucher Document",
            Giris = "Sign In",
            Bulunamadi = "Page not found",
            Yedekler = "Backups",
            Evraklar = "Cheques / Notes",
            EvrakTakvimi = "Due Calendar",
            // Paket P-14 (Y-9) — group headings.
            GrupGunluk = "Daily Work",
            GrupMusteriStok = "Customers and Stock",
            GrupParaBanka = "Money and Bank",
            GrupBelgeUyum = "Documents and Compliance",
            GrupRapor = "Listings",
            GrupYonetim = "Administration",
            MenuEtiketi = "Main menu",
        },
        Auth = new()
        {
            UygulamaAdi = Marka.Ad,
            Slogan = "Jewelry Management System",
            GirisYap = "Sign In",
            IsletmeKur = "Create Business",
            Eposta = "Email",
            Sifre = "Password",
            SifreTekrar = "New Password (Repeat)",
            IsletmeAdi = "Business Name",
            Ad = "First Name",
            Soyad = "Last Name",
            AdSoyad = "Full Name",
            Rol = "Role",
            GirisBasarisiz = "Sign in failed",
            KayitBasarisiz = "Registration failed",
            DavetJetonu = "Invitation code",
            DavetAciklama = "Business registration is invitation-only. Ask our sales team if you do not have a code.",
            DavetPlaceholder = "Paste the invitation code you received",
            SifrePlaceholder = "At least 8 characters",
            Hosgeldin = "Welcome,",
            PanelBaslik = "Your gold in safe hands",
            PanelAlt = "From sales to stock, cash to invoices — your entire business on one screen.",
            Ozellik1 = "TL + fine gold parallel ledger",
            Ozellik2 = "Live gold rate tracking",
            Ozellik3 = "Multi-branch, register and bank management",
            KisitBaslik = "NOT included in this installation",
            KisitYukleniyor = "Loading the exclusion list…",
            KisitBos = "No exclusions are declared for this installation.",
            KisitKaynak = "Single source:",
            KisitHatasi = "The exclusion list could not be loaded — it is not empty, it could not be read. Please refresh.",
            KisitDurumHaric = "EXCLUDED",
            KisitDurumKapsamDisi = "OUT OF SCOPE",
            KisitDurumSurum11 = "1.1",
            MfaBaslik = "Two-step sign-in",
            MfaAciklama = "In addition to your password, the 6-digit code from your authenticator app is required.",
            MfaKur = "Set up",
            MfaKapat = "Turn off",
            MfaKod = "Verification code",
            MfaKodPlaceholder = "6-digit code",
            MfaKurtarmaKodu = "Recovery codes",
            MfaKurtarmaUyari = "These codes will not be shown again. Download or print them and keep them safe.",
            MfaKurtarmaYenile = "Regenerate codes",
            MfaEtkin = "Enabled",
            MfaPasif = "Disabled",
            MfaGizElle = "Key to enter manually in the authenticator app",
            MfaKilitli = "Too many failed attempts: the account is temporarily locked",
            MfaKodHatali = "Verification code is incorrect",
            MfaKalanKod = "Remaining recovery codes",
            MfaSaatUyarisi = "Make sure your phone clock is set automatically.",
        },
        Panel = new()
        {
            SelamGunaydin = "Good morning",
            SelamIyiGunler = "Good afternoon",
            SelamIyiAksamlar = "Good evening",
            SelamIyiGeceler = "Good night",
            HavaBaslik = "Weather",
            HavaAlinamadi = "Weather data unavailable",
            HavaKapali = "Weather service is off (Settings)",
            DuzeniDuzenle = "Arrange tiles",
            DuzenlemeyiBitir = "Done arranging",
            DuzeniSifirla = "Reset layout",
            KutucukGizle = "Hide tile",
            KutucukGoster = "Show tile",
            KutucukYukari = "Move up (Alt + Up)",
            KutucukAsagi = "Move down (Alt + Down)",
            GizliKutucuklar = "Hidden tiles",
            Tahsilat = "Collection",
            BekleyenYok = "Nothing pending — you are clear today.",
            BekleyenRolKapali = "Pending compliance items are visible to owner, manager and accounting roles only.",
            SonIslemYok = "No transactions yet today.",
            KasaKaydiYok = "No cash register defined.",
            HizliEylemler = "Quick actions",
            BekleyenIsler = "Pending work",
            SonIslemler = "Recent activity",
            DuzenKaydedilemedi = "Home layout could not be saved",
            GunlukSatis = "Daily Sales",
            GunlukAlis = "Daily Purchases",
            NetHasGram = "Net Fine Gold",
            NetKasa = "Today's cash flow",
            NetKasaFormul = "(sales − purchases − scrap)",
            ParalelDefter = "Parallel ledger",
            MasakSuresiGecti = "suspicious transaction report(s) have passed the 10 business day deadline.",
            MasakSuresiYaklasiyor = "suspicious transaction report(s) are due within 3 days.",
            PusulaSuresiGecti = "expense voucher(s) have passed the 7 day issue deadline.",
            YuksekRiskliCari = "customer account(s) are rated high risk (a warning, not a block)",
            UyumaGit = "Compliance",
            PusulalaraGit = "Expense vouchers",
            Son7Gun = "Last 7 Days Sales (₺)",
            BugunOzet = "Today Summary",
            Satis = "Sales",
            Alis = "Purchases",
            Iscilik = "Labor",
            HasGram = "Fine Gold",
            Kur24k = "Rate (24K Gold)",
            Canli = "Live",
            KurTakip = "Track from header →",
            Altin24k = "Gold 24K",
            Gram = "gram",
            KurAlis = "Buy",
            KurSatis = "Sell",
            HizliErisim = "Quick Access",
            SatisYap = "New Sale",
            AlisYap = "New Purchase",
            MusteriBul = "Find Customer",
            KasaOzeti = "Cash Summary",
            Aciklama = "Today's sales, purchases and cash summary.",
            BugunItibariyla = "(as of today — independent of the period)",
            BasitModNotu = "Simple mode shows only today's three numbers; switch to professional mode for detail.",
            BugunOzeti = "Today Summary",
        },
        Cari = new()
        {
            KrediLimiti = "Credit Limit",
            Notlar = "Notes",
            NotlarIpucu = "Free-form note about this account (max 1000 characters). Leave empty to clear the existing note.",
            VergiNo = "Tax No",
            CariDuzenle = "Edit Account",
            PasifeAl = "Deactivate",
            // ── PACKAGE 40-A — PEP declaration ──────────────────────────
            PepBeyani = "Politically exposed person (PEP) declaration",
            PepGorev = "Declared position",
            PepGorevZorunlu = "If the declaration is ticked, the position field is mandatory.",
            PepBeyanTarihi = "Declaration date",
            PepNotu = "This is a DECLARATION, not a determination: the system does not know whether anyone is politically exposed; it records the customer's own statement and when it was given.",
            PepRozet = "PEP",
            RiskSeviyesi = "Risk level",
            PasifeAlOnay = "The account will be deactivated. Its history and balance are preserved but it cannot be selected in new transactions. Confirm?",
            Aktiflestir = "Activate",
            Tahsilat = "Collection",
            TahsilatIpucu = "Payment received from the customer. Reduces the balance.",
            Duzeltme = "Adjustment Voucher", DuzeltmeIpucu = "Manually adjusts the balance; posted to the ledger as an account correction slip and IRREVERSIBLE. Reason required (min 20 chars). Positive = debt increases.", TlDelta = "₺ balance delta (±)", HasDelta = "Fine-gold delta (±)", Gerekce = "Reason",
            Veresiye = "Credit",
            VeresiyeIpucu = "Credit extended to the customer. Increases the balance.",
            TlTutar = "Amount (₺)",
            OdemeTuru = "Payment Method",
            Nakit = "Cash",
            Kart = "Card",
            Havale = "Bank Transfer",
            HasGramOdeme = "Payment in Fine Grams",
            HasGramIpucu = "Enter grams if the customer pays with gold; leave empty for cash only.",
            HasGramVerilen = "Fine Grams Given",
            Atolye = "Workshop",
            Sarraf = "Bullion Dealer",
            DovizBakiye = "FX Balance",
            DovizVeresiye = "FX Credit",
            DovizTahsilat = "FX Collection",
            ParaBirimi = "Currency",
            EfektifMiktar = "Cash Amount",
            DovizBorcuVar = "Has an FX balance (no due date assigned)",
            TlKarsiligiBilgiAmacli = "₺ equivalent (informational — computed by the server)",
            KurYokDovizYapilamaz = "The selected currency has no rate in the current rate snapshot — FX transactions are not possible.",
            DovizVadesizUyari = "FX balances have no due date or ageing in this release; they do not appear in the overdue list.",
            DovizFisBacagiUyari = "An FX leg on a receipt is a PAYMENT; it leaves no FX debt on the account. FX debt arises only from this form.",
            DovizDelta = "FX balance delta (±)",
            Baslik = "Customers",   // PAKET DIL-SUNUCU: sekme başlığı (document.title) ile h1 AYNI ad ("Accounts" ≠ "Customers", UX-S31)
            Aciklama = "Customer, supplier and workshop accounts; balances and collections.",
            YeniCari = "New Account",
            AraPlaceholder = "Search by name, phone or email...",
            AdFirma = "Name / Company Name",
            Tur = "Type",
            Musteri = "Customer",
            Tedarikci = "Supplier",
            HerIkisi = "Both",
            Telefon = "Phone",
            Adres = "Address",
            TlBakiye = "TL Balance",
            HasGram = "Fine Gold",
            Hareketler = "Transactions",
            SonHareketler = "Recent Transactions",
            HareketYok = "No transactions",
            CariBulunamadi = "No accounts found",
            Olusturulamadi = "Failed to create account",
            CariDetay = "Account Details",
            CariSecin = "Select account...",
            CariAra = "Search Account",
            MusteriAdiYazin = "Type customer name...",
            Alacak = "Receivable",
            Borc = "Payable",
                    PasifeAlBagli = "Linked records: {0} transactions · ₺ balance {1} · fine balance {2} g",
            PasifeAlBagliYukleniyor = "Reading linked records…",
            // ── PACKAGE P-16 (Y-27) ──
            KimlikBolumu = "Identity and commercial details",
            Unvan = "Legal title",
            UnvanIpucu = "Legal name printed on invoices; falls back to the account name when empty.",
            KimlikTuru = "Identity number type",
            KimlikTuruBelirtilmedi = "Not stated",
            KimlikTckn = "National ID (11 digits)",
            KimlikVkn = "Tax ID (10 digits)",
            KimlikNoHaneIpucu = "National ID is 11 digits, tax ID is 10 digits.",
            DogumTarihi = "Date of birth",
            Uyruk = "Nationality",
            UyrukIpucu = "Pick the country from the list.",
            IskontoYuzde = "Standard discount (%)",
            IskontoYuzdeIpucu = "Applied on the sales receipt when the discount box is left empty.",
            Segment = "Segment",
            HesapKodu = "Accounting code",
            HesapKoduIpucu = "The matching account in your accountant's chart of accounts "
                + "(e.g. 120.01.0001). Left blank, the accounting export leaves this cell empty.",
            SegmentSecilmedi = "Not selected",
            SegmentBireysel = "Individual",
            SegmentKurumsal = "Corporate",
            SegmentToptan = "Wholesale",
            SegmentVip = "VIP",
        },
        Urun = new()
        {
            Baslik = "Products",
            Aciklama = "Product cards in stock; carat, gram, fine gram and section.",
            UrunGrubu = "Product group",
            StokGirisiYolu = "Stock is not typed in on this screen: use the \"Opening stock\" field on a new product, a purchase transaction, or \"Count adjustment\" on the row. Every stock change then reaches the ledger and the cost lot.",
            YeniUrun = "New Product",
            AraPlaceholder = "Search by product name or barcode...",
            UrunAdi = "Product Name",
            Barkod = "Barcode",
            Kategori = "Category",
            Ayar = "Karat",
            TumAyarlar = "All Karats",
            Maden = "Metal",
            Madenler = "Metals",
            TumMadenler = "All Metals",
            Saflik = "Fineness (millesimal)",
            SaflikTablosu = "Fineness table for the selected metal",
            RodyumKaplama = "Rhodium plated",
            RodyumKaplanmaz = "Rhodium plating cannot be set for this metal.",
            Kod = "Code",
            RodyumKaplamaIpucu = "Rhodium is not a stock metal; it is a plating applied to white gold, platinum and palladium surfaces. It is carried only as a label statement.",
            HasHesaplanmaz = "Pure weight is not calculated",
            HasHesaplanmazIpucu = "This metal is not precious: it has no pure (has) weight and its price cannot be derived from the bullion rate — a sale price is mandatory.",
            KotasyonYok = "No quotation",
            KotasyonYokIpucu = "No gram price is defined for this metal, so no price quote is produced. Enter a fixed sale price on the product.",
            MadenBakiyeleri = "Balance by metal",
            MadenDagilimi = "Breakdown by metal",
            MadenUyarisi = "Total pure weight is GOLD ONLY; other metals are listed separately — grams of different metals are never added together.",
            GramAgirlik = "Gram Weight (g)",
            Birim = "Unit",
            BirimAdet = "Piece",
            BirimGram = "Gram",
            BirimGramIpucu = "Gram-based items are stocked and sold by weight; 1 unit = 1 g, gross weight is set to 1 automatically.",
            Karat = "Carat (ct)", Renk = "Color", Berraklik = "Clarity", Kesim = "Cut", SertifikaKurumu = "Certificate lab", SertifikaNo = "Certificate no", Sertifikalar = "Certificates", SertifikaYukle = "Upload certificate PDF", PdfOlmali = "PDF only, max 5 MB", DortC = "Diamond 4C",
            TasAgirlik = "Stone Weight (g)",
            TasKokeni = "Stone Origin",
            TasKokeniIpucu = "Mandatory when stone weight is entered (Turkish Jewellery Trade Regulation art.11/e)",
            TasIbaresi = "Label Wording",
            TasIbaresiIpucu = "Leave empty to use the origin's default wording",
            TasKokenDogal = "Natural",
            TasKokenSentetik = "Synthetic",
            TasKokenLaboratuvar = "Lab-grown",
            TasKokenYapay = "Man-made",
            TasKokenYok = "No stone",
            GercekMilyem = "Actual Fineness",
            GercekMilyemIpucu = "Leave empty to use the karat's standard fineness",
            SatisMilyemi = "Selling fineness",
            SatisMilyemiIpucu = "E.g. 950 — price: gram × fineness × fine gold. Empty uses purchase fineness, then the price list",
            FiyatOfset = "Price adjustment (₺)",
            FiyatOfsetIpucu = "Added to the calculated price; may be negative",
            AlisMilyemi = "Purchase fineness (cost)",
            AlisMilyemiIpucu = "Fineness bought from the wholesaler, e.g. 925. Used for sale when selling fineness is empty",
            KdvMatrahBinde = "VAT-able share (per mille)",
            KdvMatrahBindeIpucu = "E.g. 1 = 1‰ of the total is subject to VAT, the rest 0%. Empty uses the legal method",
            SatisFiyati = "Sale Price (₺)",
            SatisFiyatiIpucu = "Leave empty to auto-calculate fine×rate + labor at sale time",
            Milyem = "Fineness",
            Gram = "Gram",
            HasGram = "Fine Gold",
            Durum = "Status",
            Stok = "Stock",
            SubeStoklari = "Branch Stock",
            Sube = "Branch",
            ToplamHas = "Total Fine",
            StokYok = "No stock",
            UrunBulunamadi = "No products found",
            Olusturulamadi = "Failed to create product",
            UrunSecin = "Select product...",
            UrunAdiYazin = "Type product name...",
            UrunEkle = "Add Product",
            UrunDuzenle = "Edit Product",
            PasifeAl = "Deactivate",
            AcilisStogu = "Opening stock (qty)",
            AcilisStoguIpucu = "Quantity you already have. If left empty the product is created with zero stock and cannot be sold. After saving, stock changes only through purchase, sale and stocktake.",
            FormAlanHatasiGenel = "Not saved. Fix the field marked in red.",
            Rfid = "RFID tag code",
            RfidIpucu = "Type the code from the tag by hand. This installation has no RFID reader connection; the field only stores the code.",
            KmtsKodu = "KMTS code",
            KmtsKoduIpucu = "Precious Metals Tracking System code. Optional; the server does not validate it, it is stored as given. Leave empty to clear it.",
            GorselUrl = "Image address (URL)",
            GorselUrlIpucu = "Only addresses starting with http:// or https:// are accepted. Leave empty to clear it; use the image uploader below to attach a file.",
            PariteKodu = "Parity code",
            PariteKoduIpucu = "Coin/bullion quotation code (e.g. new quarter, Reşat…). Leave empty to remove the match; the price is then computed as fine grams × rate + labour.",
            Gorsel = "Image",
            GorselYok = "No image",
            Galeri = "Product images",
            GaleriBos = "No image has been attached to this product yet.",
            GaleriIpucu = "Uploaded documents of type IMAGE and PHOTO are shown here.",
            GorselUrlGecersiz = "The image address must start with http:// or https://.",
            PasifeAlOnay = "The product will be deactivated. Its history is kept, but it can no longer be sold. Confirm?",
            BarkodAra = "Scan barcode",
            BarkodAraIpucu = "Scan the barcode, or type it and press Enter",
            AraVeyaBarkod = "Product name, code or barcode — scan and press Enter",
            BarkodBulunamadi = "No active product with this barcode",
            BarkodFiyatGirilmeli = "Product selected, but its price could not be computed automatically (no gram quotation for silver in the model yet). Enter the unit price by hand and press Add.",
            Etiket = "Label",
            EtiketMetni = "Text to print on the label",
            IbareZorunlu = "Wording required",
            Dayanak = "Legal basis",
            SayimDuzelt = "Adjust count",
            SayilanAdet = "Counted quantity",
            SayimIpucu = "Quantity found in the physical count. Any difference is written to the ledger as a stock-count correction movement.",
            Fark = "Difference",
            Reyon = "Display unit",
            TumReyonlar = "All display units",
            TumKategoriler = "All categories",
            Konsinye = "Consignment",
            KonsinyeSahibi = "Consignment owner",
            KonsinyeBirimBedel = "Unit amount payable to the owner",
            KonsinyeIpucu = "Consignment goods are not ours; they are excluded from stock value and owed to the owner once sold.",
            // ── PACKAGE P-16 (Y-26) ──
            KartBolumu = "Supply, cost and stock details",
            Tedarikci = "Supplier",
            TedarikciIpucu = "The account that sold us the item. Different from the consignment owner.",
            AlisMaliyeti = "Purchase cost (₺)",
            AlisMaliyetiIpucu = "The only input of the profitability report. When empty, profit is NOT computed and zero is not assumed.",
            AlisMaliyetiYok = "No cost entered — this item counts as \"cost unknown\" in the profitability report.",
            HedefMarj = "Target margin (%)",
            HedefMarjIpucu = "Used for price suggestions only; it does not restrict sales.",
            KdvOrani = "VAT rate (%)",
            KdvOraniIpucu = "As a percentage, e.g. 20. Empty uses the rate chosen on the invoice. For a per-mille share use \"VAT-able share\"",
            SeriNo = "Serial number",
            Olcu = "Size / length",
            OlcuIpucu = "Ring size, bracelet or chain length (e.g. 17, 18 cm).",
            MinStok = "Low stock threshold",
            MinStokIpucu = "The item counts as low stock below this quantity. Empty means no threshold.",
        },
        Islem = new()
        {
            DilKodu = "en",
            Baslik = "Transactions",
            Aciklama = "Sales, purchases, scrap buying, swaps, labour, returns and inter-branch transfers.",
            SonSatislar = "Recent sales",
            SonSatisYok = "No sales have been recorded for this tenant yet",
            OdemeKirilimi = "Payment breakdown",
            OdemeKirilimiGoster = "Show payment breakdown",
            OdemeKirilimiGizle = "Hide payment breakdown",
            KirilimKayitli = "Breakdown recorded",
            KirilimBilinmiyor = "Breakdown unknown — this receipt has no recorded payment legs; this does NOT mean \"no payment\"",
            KirilimKismen = "One share of the breakdown could not be measured",
            KirilimGeriDolduruldu = "Derived from historical records",
            KirilimYok = "No payment items recorded for this receipt",
            IadeEt = "Return",
            IadeHakkiBitti = "This receipt has been fully returned",
            SatisDuzeltmeYolu = "If the customer brought the goods back, enter a return on the \"Sales Return\" tab; if only the description or payment details are wrong, correct the account balance with a correction slip.",
            FisNo = "Receipt no",
            CariSecin = "Select an account",
            UrunSecinUyari = "Select a product first",
            Satis = "Sale",
            Alis = "Purchase",
            HurdaAlis = "Scrap Purchase",
            Takas = "Trade-in",
            Iscilik = "Labor",
            Transfer = "Transfer",
            Formu = "Form",
            KasaId = "Register ID",
            KasaIdOpsiyonel = "Register ID (optional)",
            Kasa = "Cash Register",
            KasaSecin = "Select register...",
            TekKasaOtomatik = "Only one register — selected automatically",
            KurdanGeldi = "From live rate",
            ElleGirildi = "Entered manually",
            OdemeTuru = "Payment Type",
            TevsikNakitOnerisi = "This amount exceeds the documentation threshold ({0}) — cash payment will be rejected. Choose bank transfer or card.",
            OdemeNakit = "Cash",
            OdemeKart = "Card",
            OdemeHavale = "Bank Transfer",
            OdemeAltin = "Gold",
            OdemeDoviz = "Foreign currency (cash)",
            DovizMiktar = "Currency amount",
            DovizTlKarsiligi = "₺ equivalent (from rate record)",
            OdemeVeresiye = "On Credit",
            KarmaOdeme = "Split payment (multiple methods)",
            KarmaOdemeIpucu = "Close one receipt with cash + card + transfer + credit combined.",
            Bacaklar = "Payment legs",
            BacakEkle = "Add leg",
            BacakSil = "Remove leg",
            Arac = "Method",
            BacakPosCihazi = "Card terminal",
            BacakArac = "Payment method",
            BacakTaksit = "Installments",
            BacakKasaOtomatik = "Only one cash drawer fits this method; it was selected automatically.",
            BacakPosTek = "Only one card terminal is defined; it was selected automatically.",
            BacakPosTaksit = "Instalments",
            BacakPosOtomatik = "Only one active terminal in this branch; selected automatically.",
            BacakPosCihazYok = "No active card terminal defined — the card leg is written without one.",
            FisToplami = "Receipt total",
            BacakToplami = "Legs total",
            Kalan = "Remaining",
            KalaniAta = "Assign remainder",
            BacakToplamiUyusmuyor = "The legs must add up to the receipt total exactly.",
            SatisIade = "Sales Return",
            AlisIade = "Purchase Return",
            IadeEdilecekIslem = "Transaction to return",
            IslemSecin = "Select a transaction",
            IadeAdedi = "Return quantity",
            IadeTutari = "Refund amount",
            OrantiliTutar = "Proportional amount",
            KalanIadeHakki = "Remaining returnable",
            IadeIslemiYok = "No returnable transaction found",
            NakitKapali = "The receipt total exceeds the 30,000 TRY threshold, so no CASH leg is allowed — the whole amount must go through a bank, card or transfer (General Communiqué 459 §4.1.2).",
            KalanNakitHakki = "Remaining cash allowance for this customer today",
            HasGramFiyati = "Fine gold price (1 gram)",   // ISLEM-BANT-2
            SaticiKimligi = "Seller identity",
            SaticiKimligiNot = "When you buy scrap, the seller's identity is required at any amount; the expense voucher needs it.",
            SaticiTipi = "Seller type",
            SaticiTipiNihai = "End consumer (private person — no withholding)",
            SaticiTipiEsnaf = "Exempt tradesman (2% withholding)",
            SaticiTipiMukellef = "Registered taxpayer (no voucher; seller issues an invoice)",
            SaticiAdSoyad = "Seller full name",
            SaticiTckn = "Seller national ID",
            SaticiAdres = "Seller address (optional)",
            TedarikciFaturaNo = "Supplier invoice no",
            SaticiTipiZorunlu = "Seller type is required.",
            SaticiAdZorunlu = "Seller full name is required.",
            SaticiTcknZorunlu = "Seller national ID is required.",
            SaticiTcknGecersiz = "The national ID must be 11 digits.",
            TedarikciFaturaNoZorunlu = "A supplier invoice number is required when buying from a registered taxpayer.",
            SaticiKimligiEksik = "Seller identity is missing: choose a seller type and fill in the identity fields.",
            CaridenGeldi = "From the account record",
            MasakKimlikEksik = "Identity verification missing: the AML threshold was exceeded, enter the customer's identity details.",
            TakasEskiUrun = "Old Item (Received from Customer)",
            TakasYeniUrun = "New Item (Given to Customer)",
            Fark = "Difference",
            FarkMusteriOder = "Customer pays",
            FarkIadeEdilir = "Refunded to customer",
            KaynakSube = "Source Branch",
            HedefSube = "Target Branch",
            SubeSecin = "Select branch...",
            BirimFiyat = "Unit Price (₺)",
            Birim = "Unit",
            Urun = "Product",
            Kaydet = "Save",
            Basarili = "Transaction saved successfully!",
            Basarisiz = "Transaction failed",
            BilgiKontrol = "Transaction failed. Please check the details.",
            IscilikTutari = "Labor Charge (₺)",
            MasakBaslik = "MASAK Customer Identification",
            MasakAciklama = "This amount exceeds the legal threshold; customer ID is mandatory. Data is stored encrypted and retained for 8 years.",
            MasakBelgeTuru = "Document Type",
            MasakBelgeNo = "Document No (ID / Passport)",
            MasakAdSoyad = "Full Name",
            MasakDogumTarihi = "Date of Birth",
            MasakUyruk = "Nationality (TUR)",
            MasakAdres = "Address",
            MasakIslemAmaci = "Purpose of Transaction",
            MasakBeyanAciklama = "Declaration Notes",
            MasakFonKaynagi = "Source of Funds",
            MasakEkBelge = "Supporting Document Ref.",
            MasakKademeKimlik = "Identification required",
            MasakKademeAmac = "Identification + purpose declaration required",
            MasakKademeForm = "Identification + declaration + cash transaction form (source of funds) required",
            MasakKademeEkBelge = "Identification + declaration form + supporting document required",
            MasakEnAzKarakter = "at least {0} characters",
            // ── PACKAGE P-16 (Y-4) ──
            KalemAyar = "Karat",
            KalemMilyem = "Fineness",
            KalemGram = "Grams",
            KalemHas = "Fine (g)",
            SatirIskonto = "Line discount (%)",
            FisIskonto = "Receipt discount (%)",
            IskontoIpucu = "Enter a percentage; the server computes the amount.",
            IskontoOnizleme = "Estimated discount",
            IskontoUygulandi = "Discount applied",
            IskontoCariden = "from the account's standard rate",
            ListedenGeldi = "came from the price list",
            PasifCariGizlendi = "Inactive accounts are hidden here; reactivate the card on the Accounts screen.",
            NetTutar = "Net amount",
            GramAgirlik = "Gram Weight",
            AciklamaOpsiyonel = "Description (optional)",
        },
        Pos = new()
        {
            Baslik = "POS Terminals",
            TahsilatBaslik = "POS Settlements",   // P-14 (Y-9): single name with the menu
            YeniCihaz = "New POS Terminal",
            CihazAdi = "Terminal Name",
            BankaAdi = "Bank",
            UyeIsyeriNo = "Merchant No",
            TerminalNo = "Terminal No",
            BagliKasa = "Linked Register",
            BankaHesabi = "Bank Account",
            KomisyonOran = "Commission Rate",
            ValorGun = "Value Date (days)",
            BekleyenAlacak = "Outstanding POS Receivable",
            BugunYatacak = "Due To Date",
            CihazSayisi = "Terminal Count",
            ValorTakvimi = "Settlement Calendar",
            ValorTarihi = "Settlement Date",
            Brut = "Gross",
            Komisyon = "Commission",
            Net = "Net",
            Yatan = "Settled",
            Kalan = "Remaining",
            Taksit = "Installments",
            IslemTarihi = "Transaction Date",
            Durum = "Status",
            DurumBekliyor = "Pending",
            DurumKismi = "Partially settled",
            DurumYatti = "Settled",
            DurumIptal = "Cancelled",
            Duzenle = "Edit",
            PasifeAl = "Deactivate",
            Aktiflestir = "Reactivate",
            PasifeAlOnay = "Do you want to deactivate this POS terminal?",
            AcikAlacakUyarisi = "A terminal with outstanding POS receivables cannot be deactivated",
            Iptal = "Cancel receivable",
            IptalGerekcesi = "Cancellation reason (at least 20 characters)",
            IptalOnay = "Do you want to cancel this POS receivable? This cannot be undone.",
            KayitYok = "No POS receivables in this range",
            CihazYok = "No POS terminal defined yet",
            DefterGerekce = "A POS ledger row is derived from a sale and cannot be edited by hand.",
            DefterAlternatif = "Correction path: the cancellation endpoint for a chargeback (reason ≥ 20 characters) or a return of the related sale.",
            KasaKilitGerekce = "This device already has POS collections; those rows carry the linked cash box and branch, so moving it now would split the ledger from the bank statement.",
            KasaKilitYolu = "Correction path: deactivate the device and create a new one linked to the correct cash box. On a device with no collections yet this field is directly editable.",
            KasaGocBaslat = "Move to another cash box",
            KasaGocBaslik = "Move POS device to another cash box",
            KasaGocAciklama = "This device has collections, so the linked cash box cannot be changed silently. You can move it with a recorded reason.",
            KasaGocUyari = "Existing collections stay with the OLD cash box; the move only changes where future collections land. The move is recorded and never deleted.",
            KasaGocGerekce = "Reason for the move (at least 20 characters, written to the audit trail)",
            KasaGocOnayla = "Confirm the move",
            KasaGocSebep = "Pick a new cash box and write a reason of at least 20 characters.",
            KasaGocTamam = "POS device moved to the new cash box. Collections left on the old one: {0}",
            YeniKasa = "New cash box",
            KomisyonAciklama = "Enter the rate between 0 and 1 (e.g. 1.85% → 0.0185). This rate is NOT statutory; it comes from the bank merchant agreement.",
            ValorAciklama = "Days until the bank settles. Counted in calendar days.",
            SinirUyarisi = "Server limit reached — there is more; narrow the date or terminal filter.",
            DonemdenBagimsiz = "(as of today — independent of the period)",
            TaksitOranlari = "Installment Rates",
            TaksitOranEkle = "Add installment rate",
            Eslestir = "Match with bank settlement",
            OtoOneri = "Get suggestion",
            OneriYok = "No single receivable set matches this transaction amount; match manually.",
            GeriAl = "Undo match",
            GeriAlmaGerekcesi = "Undo reason (at least 20 characters)",
            Kaydedildi = "Saved",
            Eylemler = "Actions",
            AdCokKisa = "Terminal name must be at least 2 characters",
            TumDurumlar = "All statuses",
            BankaHareketId = "Bank transaction id",
            TatilUyarisi = "There is NO public holiday calendar: value dates are counted in calendar days (or skipping weekends only); the bank may not settle on a holiday.",
        },
        Kasa = new()
        {
            Baslik = "Cash Registers",
            YeniKasa = "New Register",
            KasaAdi = "Register Name",
            Doviz = "Currency",
            ToplamTlBakiye = "Total TL Balance",
            KasaSayisi = "Register Count",
            AktifKasa = "Active Registers",
            Hareket = "Entry",
            HareketGir = "New Entry",
            KasaHareketi = "Register Entry",
            HareketTuru = "Entry Type",
            KasaGecmisi = "Register History",
            KasaBulunamadi = "No registers found",
            Olusturulamadi = "Failed to create register",
            Duzenle = "Edit Register",
            Duzenlenemedi = "Failed to update register",
            AdDuzenle = "Edit register name",
            SayfaAciklama = "Registers, balances and register movements; register details can be edited and a deactivated register can be reopened.",
            Sube = "Branch",
            DovizKilit = "Currency and branch can only be changed on a register with a zero balance, no movements and no day closing; otherwise past movements would lose their meaning.",
            PasifKasaUyari = "This register is inactive: no new movement can be entered. Its history is readable; reactivate it before using it again.",
            HareketStornoYok = "This row is the register leg of a ledger transaction; it cannot be voided on its own.",
            HareketStornoYolu = "The correction path is the transaction's OWN void: void the receipt from the screen where it was created (sale, collection, consignment); the register leg is rolled back with it.",
            HareketAciklamaYok = "(no description entered)",
            HareketAciklamaGuncellendi = "Register entry description updated.",
            HareketModalBaslik = "{0} — register movement",
            HareketSonuc = "{0} {1} saved. New balance: {2}",
            HareketStornoOnay = "This manually entered register movement will be voided by a reversing entry. The source row is NOT deleted; it stays in history paired with its reversal.",
            HareketDuzenlenemez = "A register movement cannot be changed in place once written: the register balance and the day closing rely on this line.",
            KasaKaydedildi = "Register details saved.",
            GunKapatildi = "The day has been closed.",
            PasifeAl = "Deactivate",
            PasifeAlOnay = "The till will be deactivated. Its history is kept, but it can no longer be selected. Confirm?",
            BakiyeliKasaUyarisi = "A till with a non-zero balance cannot be deactivated — the question of where the money went would go unanswered.",
            GunKapanisi = "End-of-day close",
            GunKapat = "Close Day",
            SayilanNakit = "Counted cash",
            DefterBakiye = "Book balance",
            Fark = "Difference",
            FarkGerekcesi = "Reason for difference",
            FarkGerekcesiZorunlu = "The count does not match the books — a reason is required.",
            GunKapali = "This till is CLOSED for today. No new movements can be recorded.",
            GunuYenidenAc = "Reopen Day",
            AcmaGerekcesi = "Reason for reopening (at least 20 characters)",
            KapanisGecmisi = "Close history",
            KapanisYok = "No end-of-day close recorded for this till yet",
            HareketGirilemedi = "Failed to add entry",
            HareketKaydedildi = "Entry saved!",
            Gelir = "Income",
            Gider = "Expense",
            Tahsilat = "Collection",
            Odeme = "Payment",
        },
        Rapor = new()
        {
            Baslik = "Reports",
            AylikSatis = "Monthly Sales",
            AylikAlis = "Monthly Purchases",
            Kar = "Profit (Sales - Purchases)",
            NetKar = "Net Profit",
            NetHasGram = "Net Fine Gold",
            NetHas = "Net Fine",
            HareketSayisi = "Entry count",
            GunlukSatisAlis = "Daily Sales / Purchases (₺)",
            StokDagilimi = "Stock Distribution (by Karat)",
            CariYaslandirma = "Account Aging Report",
            StokDegeri = "Stock Value",
            Gun_0_30 = "0-30 days",
            Gun_31_60 = "31-60 days",
            Gun_61_90 = "61-90 days",
            Gun_90_plus = "90+ days",
            Bugun = "Today",
            AylikRapor = "Monthly Report",
            KarMarji = "Profit Margin",
            IscilikGeliri = "Labor Income",
            HurdaAlis = "Scrap Purchases",
            ToplamAdet = "Total Qty",
            CariBasligi = "Account",
            YasHasBakiye = "Gold (has) balance",
            YasToplamTl = "Total (₺)",
            YasHasBakiyeVar = "Has a gold (has) balance",
            YasHasNotu = "The gold (has) balance is not converted to TL and is read separately: plus = the account owes us gold, minus = we owe the account gold.",
            YasListelenenToplam = "Total of listed accounts",
            YasSinirUyari = "Only the first {0} accounts are listed out of {1}; the subtotal covers the listed ones only.",
        },
        RaporSeti = new()
        {
            Merkez = "Report Center",
            MerkezAciklama = "Full list of shipped reports — click a card.",
            RaporSayisi = "reports",
            GrupGunluk = "Day End",
            GrupSatis = "Sales Analysis",
            GrupStok = "Stock",
            GrupMali = "Financial",
            AdGunluk = "Daily Summary",
            AdAylik = "Monthly Summary",
            AdPosMutabakat = "Day End Reconciliation",
            AdKasaDefteri = "Cash Movements",
            AdIslemDefteri = "Movement Listing",
            AdOdemeAraci = "Payment Method Breakdown",
            AdPersonel = "Recording User",
            AdSubeKarsilastirma = "Branch Comparison",
            AdStokYaslandirma = "Stock Aging / Turnover",
            YaslandirmaAciklama = "How long goods have been on the shelf and how fast they turn. Age is measured from the date the item entered the shop.",
            Kova0_30 = "0-30 days",
            Kova31_90 = "31-90 days",
            Kova91_180 = "91-180 days",
            Kova181_365 = "181-365 days",
            Kova365Ustu = "Older than 365 days",
            YasiBilinmeyen = "Age unknown",
            YasiBilinmeyenAciklama = "Items with no cost lot — entry date is not recorded. Not counted as zero, not spread across buckets.",
            DevirHizi = "Turnover rate",
            DevirPenceresi = "Turnover window (days)",
            DevirTanimi = "Turnover rate = quantity consumed in the window / quantity still on the shelf.",
            YillikTahmini = "Estimated annual turnover",
            OrtalamaRafOmru = "Average shelf life (days)",
            OluStok = "Dead stock",
            OluEsik = "Dead stock threshold (days)",
            TuketilenAdet = "Quantity out",
            KalanAdet = "Quantity on shelf",
            MaliyetiBilinmeyenAdet = "Quantity with unknown cost",
            EnEskiGiris = "Oldest entry",
            YasGun = "Age (days)",
            Hesaplanamadi = "Not computable",
            Adet = "Qty",
            AdStokDegeri = "Stock Value",
            AdReyonStok = "Shelf Stock",
            AdStokDegerlemesi = "Stock Valuation",
            AdCariYaslandirma = "Receivables Aging",
            AdVadeTakibi = "Overdue Receivables",
            AdKarZarar = "Profit & Loss",
            AdKarZararUrun = "Product Profitability",
            Tarih = "Date",
            Tur = "Type",
            Yon = "Direction",
            Giris = "In",
            Cikis = "Out",
            YonKarisik = "Mixed",
            YonYok = "—",
            YonYokIpucu = "Amount and fine gold are zero — no direction",
            YonTlBacagi = "Money",
            YonHasBacagi = "Fine",
            TlTutar = "Amount (₺)",
            HasGram = "Fine (g)",
            OdemeTuru = "Payment",
            NakitPayi = "Cash share",
            VeresiyePayi = "Credit share",
            Fis = "Receipt",
            FaturaNo = "Invoice No",
            CariAd = "Account",
            UrunAd = "Product",
            SubeAd = "Branch",
            KaydedenKullanici = "Recording User",
            KaydedenUyarisi = "This report counts the user who RECORDED the transaction; it may differ from the seller.",
            Storno = "Reversal",
            StornoAdet = "Reversal count",
            StornoAciklama = "Reversal pairs are excluded from totals; a voided transaction does not inflate turnover.",
            Bilinmiyor = "Unknown",
            BilinmiyorAciklama = "Legacy rows with no recorded cash share — not counted as zero.",
            Devir = "Opening",
            Kapanis = "Closing",
            SayilanNakit = "Counted cash",
            Fark = "Difference",
            DurumAcik = "OPEN (not closed)",
            DurumKapali = "Closed",
            DurumYenidenAcildi = "Reopened",
            SayimGir = "Enter count",
            SayimBaslik = "End-of-day cash count",
            SayimYalnizBugun = "Only today's drawer can be counted.",
            SayimNegatif = "Counted cash cannot be negative.",
            SayimKaydedildi = "End-of-day count saved.",
            SayimKaydedildiFark = "End-of-day count saved. Difference: {0}",
            DefterBakiye = "Ledger balance",
            FarkGerekcesi = "Reason for the difference (required when non-zero)",
            HareketSayisi = "Entries",
            Arac = "Payment method",
            KasaSecimi = "Cash register",
            TumKasalar = "All registers",
            TumTurler = "All types",
            TumSubeler = "All branches",
            TumKullanicilar = "All users",
            TumAraclar = "All methods",
            IslemSayisi = "Transactions",
            Ciro = "Turnover",
            OrtalamaSepet = "Average basket",
            IadeAdet = "Returns",
            IadeOrani = "Return rate",
            TransferSutun = "Transfer",
            TransferNotu = "Transfers are NOT counted in turnover (same goods would be counted twice).",
            Yazdir = "Print",
            Onceki = "Previous",
            Sonraki = "Next",
            SayfaBilgisi = "rows",
            SinirUyarisi = "Server limit reached — more rows exist, narrow the filters.",
            BosDurum = "No records in this period.",
            DonemBagimsiz = "(as of today — independent of the period)",
            Guvenilirlik = "Reliability",
            GuvenilirlikTam = "Complete",
            GuvenilirlikYuksek = "High",
            GuvenilirlikOrta = "Medium",
            GuvenilirlikDusuk = "Low",
            GuvenilirlikVeriYok = "No data",
            MalFarki = "Goods margin",
            HurdaTl = "Scrap (₺)",
            HurdaHas = "Scrap (fine g)",
            AlisTl = "Purchases (₺)",
            SatisAdet = "Sales count",
            Baslangic = "From",
            Bitis = "To",
            Uygula = "Apply",
            VeresiyeDogan = "Credit created",
            VeresiyeKapanan = "Credit settled",
            Durum = "Status",
            ParaBirimi = "Currency",
        },
        Banka = new()
        {
            Baslik = "Bank",
            YeniHesap = "New Account",
            BankaAdi = "Bank Name",
            HesapAdi = "Account Name",
            HesapSahibi = "Account Holder",
            Iban = "IBAN",
            HesapBulunamadi = "No bank accounts found",
            HesapOlusturulamadi = "Failed to create account",
            ToplamBakiye = "Total Balance",
            HesapSayisi = "Account Count",
            Hareketler = "Transactions",
            YeniHareket = "New Transaction",
            DekontGir = "Add Receipt",
            Yon = "Direction",
            Gelen = "Incoming",
            Giden = "Outgoing",
            KarsiTaraf = "Counterparty",
            KarsiIban = "Counterparty IBAN",
            HareketEklenemedi = "Failed to add transaction",
            HareketEklendi = "Transaction saved!",
            FaturayaDonustur = "Convert to Invoice",
            Faturalandi = "Invoiced",
            Bekliyor = "Pending",
            Eslesti = "Matched",
            DurumFiltre = "Status",
            CariSecInfo = "Select the account to invoice",
            SayfaAciklama = "Bank accounts and statements; account details can be edited and a deactivated account can be reopened.",
            HesapKaydedildi = "The bank account has been saved.",
            DekontDegismez = "The amount and direction of a bank entry cannot be edited in place: the money moved at the bank, and a ledger line is never rewritten retroactively.",
            DekontDegismezYolu = "If the amount or direction is wrong, reverse the entry (a counter entry is created) and enter the correct one as a new bank entry; description, counterparty and date can be corrected directly.",
            DekontGuncellendi = "Bank entry updated.",
            DekontStornolandi = "Reversed",
            DekontStornoYok = "This entry cannot be reversed: it is already reversed (or is itself a reversal), has been turned into an invoice, or came from a bank statement import.",
            DekontStornoYolu = "For an invoiced entry cancel the invoice first; an imported line is the bank's own record and cannot be voided from this screen.",
            DekontDuzenle = "Correct entry",
            DekontKilitliAlanlar = "Amount, direction and currency are locked (financial fields) — the correction path is a reversal.",
            PasifHesapUyari = "This account is inactive: no new statement line can be entered. Its history is readable; reactivate it before using it again.",
            BankadakiBakiye = "Balance at the bank",
            BankaBloke = "blocked",
            BankaBakiyeFarki = "Differs from Mihenk records by {0}",
            DonusturBasarili = "Invoice created!",
            DonusturBasarisiz = "Failed to convert to invoice",
            HesabiDuzenle = "Edit Account",
            HesapGuncellendi = "Account updated!",
            HesapGuncellenemedi = "Failed to update account",
            DekontCari = "Customer / account",
            DekontCariYok = "Not selected (no linked-transaction total)",
            DekontCariIpucu = "If a customer is selected, transactions made with the same customer on the same day are assessed together against the AML threshold.",
            MasakCariUyari = "No customer selected, so this amount is assessed on its own; other transactions made with the same customer today (linked-transaction total) cannot be computed. Select a customer to link the person.",
            MasakKilitSebep = "AML identity/declaration fields are incomplete — fill in the required fields above to save.",
            MasakYukleniyorSebep = "AML requirements are being fetched from the server — the fields will open in a few seconds.",
            MasakTespitDegismez = "The AML identity verification taken for a statement line of this size is a statutory record kept for 8 years and cannot be changed afterwards.",
            // ── package 32 — bank statement import ──
            EkstreYukle = "Import statement",
            EkstreBaslik = "Bank statement matching",
            EkstreAciklama = "Review the imported statement lines, link them to customers and turn the selected ones into bank transactions.",
            EkstreDosyasi = "Statement file (CSV)",
            EkstreFormat = "File format",
            Prova = "Dry run",
            ToplamSatir = "Total lines",
            YeniSatir = "New",
            Mukerrer = "Duplicate",
            HataliSatir = "Failed",
            OnaylananSatir = "Approved",
            Atla = "Skip",
            AtlamayiGeriAl = "Undo skip",
            SeciliOnayla = "Approve selected",
            Eslestir = "Match",
            OneriListesi = "Name suggestions",
            CariBagla = "Link to customer",
            BagiKaldir = "Remove link",
            IbanOgrenildi = "The IBAN was learned for this customer and will match automatically in later statements.",
            AcilisBakiye = "Opening balance",
            KapanisBakiye = "Closing balance",
            DosyaZatenYuklendi = "This file has already been imported for this account.",
            TekrarYukle = "Import the same file anyway (lines will be marked as duplicates)",
            SatirBulunamadi = "No statement line matches this filter.",
            SablonIndir = "Download template",
            TumunuSec = "Select the lines on this page",
            YuklemeYazmaz = "Importing creates no bank transaction. Transactions are created only when you approve the lines you selected.",
            CarisizSatirUyari = "{0} selected line(s) have no matched account. Approving without one creates a bank transaction with no account.",
            CarisizOnayla = "Approve without an account",
            CarisizOnaylaIpucu = "If unchecked, lines with no matched account are not approved; they stay pending so an account can be selected later.",
            CarisizUretilen = "transactions without an account: {0}",
            CarisizAtlanan = "left pending for a missing account: {0}",
            MukerrerGecmisUyari = "Duplicate protection covers transactions entered after this release; older records are out of scope.",
            OnaylananSatirDegismez = "An approved statement line cannot be changed: a bank transaction was created from it and the staged line is the evidence for that transaction.",
            OnaylananSatirYolu = "If it is wrong, correct it by reversing the bank transaction it created.",
            SecimYokSebep = "Select at least one line to approve.",
            OnaySiniriSebep = "The per-request approval line limit was exceeded.",
            HamSatir = "Raw line from the file",
            EkstreYuklendi = "Statement imported",
            EkstreOnaylandi = "Selected lines approved",
            SinirUyari = "The server pages this list; narrow it with a filter if you hit the limit.",
            Mt940Yok = "There is no MT940 parser in this release; use your bank's CSV statement.",
        },
        Fatura = new()
        {
            Baslik = "Invoices",
            YeniFatura = "New Invoice",
            FaturaNo = "Invoice No",
            Tur = "Type",
            Satis = "Sale",
            Alis = "Purchase",
            AraToplam = "Subtotal",
            KdvOran = "VAT Rate",
            KdvTutar = "VAT Amount",
            Toplam = "Total (VAT incl.)",
            KdvTuru = "VAT Type",
            KdvStandart = "Standard",
            KdvKuyum = "Jewelry (gold exempt)",
            HasDeger = "Fine Gold Value",
            HasDegerIpucu = "Fine gold is VAT-exempt; VAT applies only to labor+margin",
            Matrah = "VAT Base (Labor+Margin)",
            Durum = "Status",
            Taslak = "Draft",
            Kesildi = "Issued",
            Odendi = "Paid",
            Iptal = "Cancelled",
            FaturaBulunamadi = "No invoices found",
            Olusturulamadi = "Failed to create invoice",
            KaynakBanka = "From bank transaction",
            Manuel = "Manual",
            Kaynak = "Source",
            OzelMatrahBaslik = "Special VAT base reason code",
            OzelMatrahKapali = "EXCLUDED: this installation prints no special VAT base reason code and leaves the field empty in the accounting voucher. The switch stays off until the accountant confirmation (K-10); afterwards it is enabled with a single configuration field.",
            OzelMatrahAralikDisi = "Warning: code is outside the official range (801-812).",
            OzelMatrahOkunamadi = "Special VAT base setting could not be read.",
            OdendiIsaretle = "Mark as Paid",
            Kes = "Issue",
            IptalEt = "Cancel",
            Kalemler = "Line Items",
            Sira = "No",
            Miktar = "Quantity",
            BirimFiyat = "Unit Price",
            KdvMatrah = "VAT Base",
            SatirToplam = "Line Total",
            Iscilik = "Labor",
            Indirim = "Discount",
            TasIbaresi = "Stone Statement",
            KalemsizFatura = "This invoice is header-level: it was issued with a total amount only, without line items.",
            SayfaAciklama = "Issued and draft invoices; status changes ask for confirmation and an issued document is never edited in place.",
            KesOnay = "The invoice will be ISSUED. An issued document cannot be edited in place; it can only be cancelled. Do you confirm?",
            OdendiOnay = "The invoice will be marked PAID. If you mark it by mistake you will have to cancel and re-issue it. Do you confirm?",
            IptalOnay = "The invoice will be CANCELLED. This cannot be undone and the invoice number counts as used in the series. Do you confirm?",
            DurumDegisti = "Invoice {0} has been moved to {1}.",
            FaturaDegismez = "An invoice is a document: the issued number consumes the series towards the tax office, so its header, account, VAT and line items are not edited in place once saved.",
            FaturaDegismezYolu = "Cancel the wrong invoice (its number counts as used) and issue a new one with the correct details; if it is bound to a transaction, use the issue-from-receipt path.",
            KalemDegismez = "Invoice lines are part of the document: the base, VAT and fine-gold value are computed from them, and changing them later would make the document contradict its own totals.",
            NoKesimde = "Number is assigned when issued",
            TaslakDuzenle = "Edit draft invoice",
            TaslakGuncellendi = "Draft invoice updated.",
            KalemGuncellendi = "Invoice line updated; document total recalculated.",
            SayiGecersiz = "Enter a valid number.",
            ToplamKalemden = "The total is computed from the lines; edit the lines instead.",
            KesilmisDegismez = "An issued invoice cannot be changed: its number has been consumed and the document has left the building. A DRAFT can be edited freely.",
            KuyumIslemZorunlu = "For jewellery VAT the fine-gold value is derived from the ledger: select the transaction to invoice first. A jewellery invoice cannot be issued without that link.",
            ToplamUyusmuyor = "Line item total does not match the invoice total — itemised invoices allow no tolerance.",
            KalemEkle = "Add Line Item",
            KalemSil = "Remove Line Item",
            KalemToplami = "Line Item Total",
            KalemliKes = "Issue Itemised Invoice",
            KalemGoster = "Show Line Items",
            KalemBulunamadi = "This invoice has no line items",
            Urun = "Product",
            FistenKes = "Issue Invoice from Receipt",
            FisKimligi = "Receipt ID",
            Onizle = "Preview",
            FisKalemSayisi = "Line count on receipt",
            FisOnizlemeIpucu = "ALL lines of the receipt are invoiced; individual lines cannot be picked. The fine-gold amount is computed on the server.",
            FaturalanacakIslem = "Transaction to invoice",
            IslemSecin = "Select a transaction",
            IslemSecilmedi = "Under jewellery VAT the fine-gold amount is derived from the ledger: pick the transaction to invoice first.",
            FaturalanabilirYok = "No uninvoiced sale/purchase transactions in the last 30 days",
            FaturalanabilirSinir = "The server limit for this list was reached — there is more; narrow the date range.",
            HasDegerKaynagi = "Source of the fine-gold amount",
            KaynakHareket = "Derived from the ledger",
            KaynakElle = "Entered manually",
            HasKuru = "Fine-gold rate used (weighted average)",
            KapsananIslem = "Transactions covered",
            DefterToplami = "Amount in the ledger",
            OnizlemeyiGuncelle = "Refresh preview",
            OnizlemeYok = "Select a transaction and refresh the preview to see the VAT base breakdown.",
            OzelMatrahKodu = "Special VAT base reason code",
        },
        Ayarlar = new()
        {
            Baslik = "Settings",
            ProfilBilgileri = "Profile Information",
            SifreDegistir = "Change Password",
            MevcutSifre = "Current Password",
            YeniSifre = "New Password",
            SifreGuncellendi = "Password updated successfully!",
            SifreDegistirilemedi = "Failed to change password",
            SifrelerEslesmiyor = "Passwords do not match",
            SifreyiGuncelle = "Update Password",
            Dil = "Language",
            Turkce = "Türkçe",
            Ingilizce = "English",
        },
        Kabuk = new()
        {
            MenuAc = "Open menu",
            MenuKapat = "Close menu",
            Kaydediliyor = "Saving…",
            SunucuOnayiBekleniyor = "Waiting for server confirmation — nothing is reported as saved before the response arrives.",
            DuzenlemekIcinTikla = "Click to edit · Enter saves, Esc cancels, leaving the field also saves",
            KaydetKilitli = "Save is locked",
            BosBirakilamaz = "This field cannot be empty.",
            SayiGecersiz = "Enter a valid number.",
            AramaAc = "Search",
            AramaBaslik = "Search and go",
            AramaIpucu = "Search records or screens",
            AramaEnAz = "Type at least {0} characters to search.",
            AramaSonucYok = "No matching record or screen.",
            AramaGozatma = "Showing the most recent records — start typing to narrow down.",
            AramaHata = "Search failed",
            AramaEkranlar = "Screens",
            AramaKayitlar = "Records",
            AramaKapat = "Close search",
            MenuAramaEtiket = "Search the menu",
            MenuAramaKisayol = "Ctrl K",
            MenuAramaYerTutucu = "Records / screens",
            AramaKayitHatasi = "Record search failed (screen search still works)",
            Favoriler = "Favourites",
            FavoriEkle = "Add to favourites",
            FavoriCikar = "Remove from favourites",
            FavoriIpucu = "Screens you star are collected at the top of the menu. Favourites are yours only; no one else sees them.",
            SiraYukari = "Move up (Alt + Up)",
            SiraAsagi = "Move down (Alt + Down)",
            SiraTutamac = "Drag to reorder — keyboard: Alt + Up / Alt + Down",
            AramaTemizle = "Clear search",
            SirayiSifirla = "Reset order",
            SiraGrupKilidi = "Reordering works inside a group only; moving items between groups is not in this version.",
            SiraKaydedilemedi = "Menu order could not be saved",
            YetkiYuklenemedi = "Screen permissions could not be loaded — the menu is showing its last known state.",
            YetkiYenidenDene = "Try again",
            SeciciAra = "Type to search",
            SeciciTemizle = "Clear selection",
            SeciciSecili = "Selected record",
            SeciciSecimYok = "No record selected yet.",
            SeciciListeAc = "Open list",
            SeciciListeKapat = "Close",
            SeciciEslesmeYok = "No matching option.",
            SeciciAramaYerTutucu = "Search in list",
            SeciciSinir = "At most {0} records are shown; keep typing to narrow down.",
            SeciciCokSecenek = "First {1} of {0} options listed; narrow down by searching.",
            SeciciSec = "Select",
            SeciciKilitli = "This field cannot be changed right now",
            SubeKapsami = "Branch scope",
            SubeTumu = "All branches",
            SubeOturumdan = "from session",
            SubeTekKaynak = "Single branch",
            TurCari = "Account",
            TurUrun = "Product",
            TurFis = "Receipt",
            TurFatura = "Invoice",
            TurBankaHareket = "Bank entry",
            TurHareket = "Ledger line",
            TabloBolgesi = "Data table",
            TabloKaydirIpucu = "The table scrolls horizontally — swipe left/right (keyboard: focus with Tab, then arrow keys).",
            BaglantidanSuzgec = "Filter came from a link",
            BaglantiSuzgeciniTemizle = "Clear filter",
            SayfaHatasi = "This page hit an unexpected error; the data shown may be incomplete. Saved information is not lost — please retry the page.",
            SayfaHatasiYeniden = "Retry page",
            ZorunluAlan = "This field is required.",
            SifirdanBuyuk = "Enter a value greater than zero.",
            NegatifOlamaz = "A negative value is not allowed.",
            FormEksik = "The form is incomplete or invalid — fix the marked fields.",
            Kaydedildi = "Saved.",
            EpostaGecersiz = "Enter a valid e-mail address (example: name@example.com).",
            TelefonGecersiz = "Enter the phone number in digits (10-13 digits, optional leading +).",
            SinirUyarisi = "At most {0} records are shown; use search and filters to narrow the list.",
            SinirAsildi = "Limit reached — some records may be hidden, narrow your search.",
            PasifleriGoster = "Show inactive",
            AktifeAl = "Reactivate",
            AktifeAlindi = "The record has been reactivated.",
            YetkiYok = "You are not authorised for this action.",
            IsKurali = "A business rule blocked this:",
            Storno = "Reversal",
            StornoBaslik = "Void with a reversing entry",
            StornoGerekce = "Reason for the reversal",
            StornoOnay = "This record will be voided by a reversing entry. The source line stays in the ledger and the reversal is written beside it. This cannot be undone.",
            StornoYapildi = "Reversing entry written. Receipt no: {0}",
            StornoluKayit = "Reversed",
            StornoKurYok = "A reversal cannot be written without a current rate snapshot: the reversing entry is bound to today's rate.",
            VirmanBacagi = "Transfer leg",
            VirmanBacagiGerekce = "This row is one leg of a TRANSFER: it cannot be voided on its own, otherwise only half of the money would come back and the other side would stay inflated.",
            VirmanStornosunaGit = "Go to the transfer reversal",
            PasifeAl = "Deactivate",
            PasifeAlindi = "The record has been deactivated.",
            PasifeAlOnay = "This record will be deactivated. Past records are kept; it can only no longer be selected in new transactions. Do you confirm?",
            Onayla = "Confirm",
            TarihAraligi = "The end date cannot be earlier than the start date.",
            DegisiklikYok = "No field has changed — there is nothing to save.",
            RotaYokBaslik = "Page not found",
            RotaYokAciklama = "There is no screen at this address. The address may be mistyped, or the screen may have moved.",
            RotaYokAdres = "Requested address",
            RotaYokPanele = "Back to the dashboard",
            RotaYokBenzer = "You may be looking for one of these:",
        },
        Form = new()
        {
            SayiAnlasilmadi = "That is not a number. Example: 12.5",
            TelefonYerTutucu = "05XX XXX XX XX",
            TelefonIpucu = "Write it with the leading zero, spaced or not.",
            EpostaYerTutucu = "name@example.com",
            AralikBaslangic = "Start date",
            AralikBitis = "End date",
            AnahtarAcik = "On",
            AnahtarKapali = "Off",
        },
        Gorunum = new()
        {
            Baslik = "Appearance",
            Aciklama = "The colour scheme and text size apply to the whole company and take effect immediately.",
            RenkSemasi = "Colour scheme",
            YaziOlcegi = "Text size",
            SemaAltin = "Classic gold",
            SemaAltinAciklama = "Today's palette — amber accent.",
            SemaKurumsal = "Corporate blue",
            SemaKurumsalAciklama = "Calm, corporate tone; the widest contrast margin.",
            SemaZumrut = "Emerald",
            SemaZumrutAciklama = "Deep green accent.",
            OlcekYogun = "Dense",
            OlcekYogunAciklama = "Today's scale: more rows per screen (14px body).",
            OlcekOkunakli = "Readable",
            OlcekOkunakliAciklama = "16px body; easier to read from a distance and on small screens.",
            SahipGerekli = "Only the business owner can change the appearance.",
            Yuklenmedi = "The appearance setting could not be read from the server; the choice cannot be saved.",
            Kaydedildi = "Appearance saved.",
            KontrastNotu = "Every scheme in this list is measured against the WCAG 2.1 AA contrast threshold (4.5:1).",
        },
        Tema = new()
        {
            Baslik = "Theme",
            Aciklama = "With System the operating system light/dark preference is followed.",
            Sistem = "System",
            Aydinlik = "Light",
            Karanlik = "Dark",
        },
        Arayuz = new()
        {
            Baslik = "Interface mode",
            Aciklama = "Simple mode reduces the number of fields; it never bypasses a legal check.",
            Basit = "Simple",
            Profesyonel = "Professional",
            KiraciVarsayilani = "Company default",
            ModAciklamaBasit = "A plain counter view: fewer fields, larger buttons, step-by-step sale.",
            ModAciklamaPro = "Full power: all transaction types, split payment, terms and detailed fields.",
            VarsayilanBaslik = "Company default interface mode",
            VarsayilanAciklama = "Users who state no preference of their own work in this mode.",
            AdimUrun = "Item",
            AdimMusteri = "Customer",
            AdimOdeme = "Payment",
            AdimFis = "Receipt",
            YasalKapiAcildi = "This transaction needs more information; fill in the fields shown.",
            BasitAkisEtiket = "Sale steps",
            BasitAkisIpucu = "Scan the item, pick the customer, save the receipt. Extra fields open by themselves when they are needed.",
            BasitOdemeNakit = "Payment: Cash",
            SistemBelirledi = "set by the system",
            ProAlanIpucu = "Detailed fields live in professional mode; they also open here when a legal rule requires them.",
        },
        Kur = new()
        {
            GramAltin = "Gram Gold",
            HasAltin = "Fine Gold",
            Ceyrek = "Quarter",
            Yarim = "Half",
            Tam = "Full",
            Ata = "Ata",
            Usd = "Dollar",
            Eur = "Euro",
            Gbp = "Pound",
            Alis = "Buy",
            Satis = "Sell",
            Kaynak = "Source",
            Guncelle = "Refresh rates",
            Guncelleniyor = "Updating...",
            Guncellenemedi = "Failed to refresh rates",
            Canli = "Live",
            Baslik = "Rate Management",
            Aciklama = "Every transaction is recorded with the rate of that moment. A wrong rate makes the fine-gold maths wrong too.",
            GuncelKur = "Current rate",
            AltinHasFiyati = "Fine gold price (₺/gram)",
            GramAltinFiyati = "Gram gold ask (24K)",
            KaynakDegistir = "Change source",
            SimdiCek = "Fetch now",
            Cekiliyor = "Fetching...",
            KurGuncellendi = "Rate updated",
            ManuelKaydedildi = "The manual rate has been saved; later transactions are bound to it.",
            IptalEt = "Void",
            IptalBaslik = "Void the manual rate entry",
            IptalOnay = "This rate entry will be marked INVALID: the row is not deleted, it stays in history with an 'invalid' badge and is excluded from the current rate. If any transaction is bound to it, the server rejects the void.",
            GecersizRozet = "Invalid",
            IptalYapildi = "The rate entry has been voided; it now shows as 'invalid' in history.",
            IptalYok = "Only MANUALLY entered rate records can be voided. A record from an external source is not corrected; fetch a new one with 'Fetch now'.",
            KayitDegismez = "Rate records are only ever added, never edited: every transaction is bound to the rate at the moment it was saved, and changing that rate afterwards would change the fine-gold maths of past receipts.",
            KayitDegismezYolu = "To correct a wrong rate, enter a new manual rate with the right value or pull it from the source again; the old record stays in history so it remains readable which receipt used which rate.",
            ManuelGiris = "Enter Rate Manually",
            ManuelUyari = "Manual rates are for offline use or special pricing; the record is marked as entered by hand and is written to the activity history.",
            Gecmis = "Rate history",
            Pariteler = "Coin & bar parities",
            ParitelerAciklama = "Source bid/ask prices for quarter, half, full and bar items.",
            Kod = "Code",
            Ad = "Name",
            Birim = "Unit",
            AyarFiyatlari = "Per-karat gram prices",
            GramFiyati = "Gram price",
            GecmisYok = "No rate records in the selected range",
            PariteYok = "This source publishes no parity list",
            SonGuncelleme = "Last updated",
            KaynakSecimi = "Rate source",
            KaynakAciklama = "Which external source should rates come from? The choice applies to the whole business and drives all pricing; changing it fetches a new rate immediately.",
            KaynakYok = "No external source configured — manual rate",
            KaynakYokSebep = "External rate sources are disabled, so there is nothing to select. Enter the rate with 'Enter Rate Manually'; enabling sources is a configuration decision (Kur:SozlesmesizKaynaklarAcik).",
            KaynakTekSebep = "This is the only configured source; with no alternative the list is not shown.",
            KaynakKaydedildi = "Rate source saved as {0}.",
            CekmeKapaliSebep = "External rate sources are disabled — there is nothing to fetch; use 'Enter Rate Manually'.",
            GuncellenemediRozet = "Not refreshed",
            KaynakAltinkaynak = "Altinkaynak",
            KaynakAnkara = "Ankara Gold Exchange",
            KaynakTruncgil = "Truncgil",
            KaynakManuel = "Manual entry",
        },
        Kullanici = new()
        {
            Baslik = "Users",
            YeniKullanici = "New User",
            AdSoyad = "Full Name",
            Eposta = "Email",
            Rol = "Role",
            Sifre = "Password",
            Durum = "Status",
            SonGiris = "Last Login",
            GirisYok = "Never logged in",
            RolSahip = "Owner",
            RolMudur = "Manager",
            RolMuhasebe = "Accountant",
            RolTezgahtar = "Clerk",
            Duzenle = "Edit",
            RolDegistir = "Change Role",
            SifreSifirla = "Reset Password",
            PasifeAl = "Revoke Access",
            PasifeAlOnay = "The user will be deactivated and can no longer sign in. Their past transactions and audit trail are kept. Confirm?",
            KendiniPasifeAlamaz = "You cannot revoke your own access.",
            SahipligiDevret = "Transfer Ownership",
            SahipligiDevretAciklama = "This user becomes OWNER and you drop to the role you pick. It happens in one step: the business is never left without an owner.",
            SahipligiDevretOnay = "You are about to transfer ownership. Afterwards you lose owner rights and getting them back depends on the new owner. Confirm?",
            DevirdenSonrakiRolum = "Your role after the transfer",
            SahiplikDevredildi = "Ownership transferred.",
            SifrenizDevir = "Your password (to confirm the transfer)",
            TekSahipUyari = "The only OWNER of this business cannot be deactivated. Transfer ownership first.",
            SifreGerekli = "Enter your password.",
            SifreDegistir = "Change Password",
            MevcutSifre = "Current Password",
            YeniSifre = "New Password",
            SifreIpucu = "At least 8 characters",
            AktifEt = "Activate",
            PasifEt = "Deactivate",
            Aktif = "Active",
            Pasif = "Inactive",
            KullaniciBulunamadi = "No users found",
            ListeYuklenemedi = "The user list could not be loaded",
            YenidenDene = "Try again",
            OtomatikDeneniyor = "Retrying automatically in {0} seconds…",
            Olusturulamadi = "Failed to create user",
            Guncellenemedi = "Failed to update user",
            Olusturuldu = "User created!",
            Guncellendi = "User updated!",
            SifreSifirlandi = "Password reset!",
            RolGuncellendi = "Role updated!",
            SilOnay = "Deactivate this user?",
            KendiniSilemez = "You cannot deactivate your own account",
            YetkiYok = "You are not authorized for this action",
            Sube = "Branch",
            SubeSecin = "Select a branch",
            SifreKuralUzunluk = "At least 8 characters",
            SifreKuralHarf = "At least one letter",
            SifreKuralRakam = "At least one digit",
        },
        Yetki = new()
        {
            Baslik = "Permissions",
            Aciklama = "Choose which screens each role can access. The Owner always has full access.",
            Rol = "Role",
            TamYetkili = "Full access",
            Kaydedildi = "Permissions updated!",
            Kaydedilemedi = "Failed to save permissions",
        },
        Denetim = new()
        {
            Baslik = "Audit Log",
            Aciklama = "Who viewed or changed which record, and when. These entries cannot be edited or deleted (KVKK art.12).",
            Tarih = "Date",
            Kullanici = "User",
            Eylem = "Action",
            Varlik = "Record type",
            Detay = "Details",
            Hassas = "Personal data",
            ToplamKayit = "Total entries",
            HassasKayit = "Personal data access",
            EnEski = "Oldest entry",
            TumEylemler = "All actions",
            TumVarliklar = "All record types",
            YalnizHassas = "Personal data access only",
            DahaFazla = "Show more",
            DegistirilemezNot = "The audit log only grows: entries are added, never deleted or modified.",
            Baslangic = "From",
            Bitis = "To",
            KayitYok = "No entries match these filters.",
            SuzgeceGore = "for the selected filters",
            BitisGunuDahil = "The end date is included in the range.",
            Sube = "Branch",
            Kasa = "Cash register",
            TumSubeler = "All branches",
            DamgaYok = "—",
            Storno = "Reversal",
            StornoGerekce = "Reversal reason (at least 20 characters)",
            StornoOnay = "Void this entry with a reversing record? The source row stays UNCHANGED.",
            StornoYapildi = "Reversal posted: the reversing record was written, the source row untouched.",
            StornoTurDesteklenmiyor = "This transaction type cannot be reversed; use the correction path shown on screen.",
        },
        Uyum = new()
        {
            Baslik = "Compliance",
            Aciklama = "MASAK identity records, suspicious transaction reports and retention management.",
            KimlikTespitleri = "Identity Records",
            SupheliIslemler = "Suspicious Transactions",
            SaklamaSuresi = "Retention",
            AcikBildirim = "Open reports",
            SuresiGecmis = "Overdue",
            Yaklasan = "Due soon",
            Son30Gun = "Last 30 days",
            Tarih = "Date",
            AdSoyad = "Full name",
            BelgeTuru = "Document type",
            BelgeNo = "Document no",
            IslemTutari = "Amount",
            SaklamaBitis = "Retention ends",
            Goruntule = "View",
            BelgeNoIleAra = "Search by document no",
            TamBelgeNo = "Full document number",
            AramaNotu = "Search requires the full number; partial search is disabled to protect personal data.",
            TespitYok = "No identity records in this range.",
            MaskeNotu = "Document numbers are masked in the list. The full number is shown only when a record is opened, and that access is written to the audit log.",
            KimlikTespitiDetay = "Identity record",
            Kaynak = "Source",
            KaynakHareket = "Ledger transaction (sale, purchase, collection…)",
            KaynakBankaHareket = "Bank statement line",
            KaynakBilinmiyor = "Not specified",
            OkumaIziNotu = "Your viewing of this record was written to the audit log (KVKK art.12).",
            DogumTarihi = "Date of birth",
            Uyruk = "Nationality",
            Adres = "Address",
            Beyan = "Declaration",
            IslemAmaci = "Purpose",
            FonKaynagi = "Source of funds",
            EkBelge = "Supporting document",
            YeniBildirim = "New report",
            BildirimNotu = "Once created, a suspicious transaction report must reach MASAK within 10 business days.",
            HareketId = "Transaction ID",
            Gerekce = "Justification",
            EnAzKarakter = "at least {0} characters",
            TespitTarihi = "Detected",
            SonBildirim = "Reporting deadline",
            Durum = "Status",
            SuresiDoldu = "OVERDUE",
            GunKaldi = "days left",
            TumDurumlar = "All statuses",
            DurumAcik = "Open",
            DurumGonderildi = "Submitted",
            DurumKapatildi = "Closed",
            BildirimYok = "No reports match this filter.",
            GonderildiIsaretle = "Mark submitted",
            Kapat = "Close",
            GonderimEldeNotu = "MASAK provides no programmatic interface. Reports are uploaded manually to MASAK Online; this record tracks the deadline and stores the submission reference.",
            GonderimReferansiSor = "Enter the MASAK Online submission reference:",
            AramaSonucu = "Search result: {0} record(s) · total {1}",
            AramaSonucYok = "No identity record found for this document number.",
            GonderimOnayBaslik = "Mark notification as submitted",
            GonderimOnayNotu = "This action is IRREVERSIBLE: the notification leaves the OPEN state and the deadline counter stops. Enter the reference number of the notification you uploaded to MASAK Online.",
            KapatOnayBaslik = "Close notification",
            KapatOnayNotu = "If the suspicion is cleared, the notification is closed with a justification. The record is NOT deleted (kept 8 years) and cannot be reopened.",
            ReferansZorunlu = "Submission reference is required.",
            GerekceEnAzHata = "Justification must be at least {0} characters.",
            KapatmaGerekcesiSor = "Enter the reason for closing:",
            BildirimOlusturuldu = "Suspicious transaction report created.",
            GonderildiIsaretlendi = "Report marked as submitted.",
            BildirimKapatildi = "Report closed.",
            SuresiDolmusTespit = "Expired identity records",
            SuresiDolmusBeyan = "Expired declarations",
            EnYakinSaklamaBitisi = "Nearest retention end",
            MaskelemeUyariBaslik = "This action cannot be undone.",
            MaskelemeUyari = "Identity fields of expired records are masked. Amount, date and declaration tier are preserved; records are not deleted. Performed because the 8-year MASAK retention obligation has ended.",
            MaskelemeOnayi = "I understand and want to apply masking.",
            MaskelemeyiUygula = "Apply masking",
            MaskelemeSonuc = "{0} identity records and {1} declarations masked.",
            // ── PACKAGE 40-A — risk scoring ─────────────────────────────
            RiskSekmesi = "Risk",
            RiskAciklama = "The risk score describes the customer profile; it NEVER blocks a transaction. It guides enhanced due diligence and suspicious activity reporting.",
            RiskKapaliNotu = "Risk-based measures are OFF (MasakRisk:Etkin=false). The score is computed and displayed but makes no field mandatory. Enabling it requires sign-off from the accountant/compliance officer.",
            SeviyeDusuk = "Low",
            SeviyeOrta = "Medium",
            SeviyeYuksek = "High",
            PepSayisi = "Accounts with a PEP declaration",
            Tazelenecek = "Profiles due for refresh",
            Profilsiz = "Accounts never scored",
            YuksekRiskliler = "High-risk accounts",
            YuksekRiskliYok = "No high-risk accounts.",
            RiskCariSec = "Select account",
            RiskCariSecilmedi = "Select an account to see its profile and history. Nothing is fetched before you pick one: the single-record endpoint writes an access log entry (KVKK art.12).",
            Puan = "Score",
            Seviye = "Level",
            RiskKaynak = "Source",
            KaynakHesap = "Engine calculation",
            KaynakElle = "Manually overridden",
            Sebepler = "Reasons",
            SebepYok = "No scoring reason.",
            YenidenHesapla = "Recalculate",
            SeviyeyiElleDegistir = "Override level",
            ElleBaslik = "Override risk level",
            ElleGerekce = "Justification",
            ElleGerekceEnAz = "The justification must be at least {0} characters ({1}/{0}). An unjustified override makes the score meaningless.",
            ElleSeviyeSec = "New level",
            ProfilTarihcesi = "Profile history (last 10)",
            ProfilTarihceYok = "No profile has been calculated for this account yet.",
            ProfilDegismezBaslik = "Past profile rows cannot be edited",
            ProfilDegismezNot = "The risk profile is an audit record that only grows and is never changed: an auditor asks which score was in force on a given day. To correct it, recalculate or set the score by hand with a justification — both write a new row and keep the old one.",
            RiskHesaplandi = "Risk profile calculated: {0} ({1} points). A new history row was written.",
            RiskElleDegistirildi = "Risk level manually set to {0}; a justified write entry was added to the audit trail.",
            RiskListeSiniri = "The list shows at most {0} accounts; select an account to narrow it down.",
            RiskSkorDurdurmaz = "The risk score never blocks a sale or a MASAK/documentation gate — no verified regulation forbids transacting with a high-risk customer.",
            SebepPepBeyani = "Politically exposed person declaration",
            SebepNakitAgirlik = "Cash-heavy transaction history",
            SebepEsigeYakin = "Repeated transactions just below the ID threshold",
            SebepSupheliGecmis = "Previous suspicious activity report",
            SebepKimlikEksik = "Missing ID verification above threshold",
            SebepYuksekRiskliUlke = "High-risk country nationality",
            SebepYaptirimEslesmesi = "Sanctions list match",
            SaklamaDayanak = "Basis: MASAK Measures Regulation art.46/1 (8 years) and KVKK art.4 (purpose limitation).",
                    EkBeyan = "File a supplementary report",
            EkBeyanBaslik = "Supplementary report — correction of a submitted notification",
            EkBeyanAciklama = "A submitted notification is never withdrawn or overwritten (8-year retention). The correction is filed as a NEW notification linked to the original; both stay on record.",
            EkBeyanBasarili = "Supplementary report filed and linked to the original notification.",
            EkBeyanRozet = "Supplementary",
            GonderildiDuzeltmeYolu = "The text of a submitted notification cannot be changed; corrections are filed with the “File a supplementary report” button.",
        },
        // PACKAGE P-17 (Y-6) — shared A4 output template.
        Cikti = new()
        {
            Baslik = "Document",
            Aciklama = "Sales slip, payment receipt, consignment delivery note, customer statement and waybill printout all print from one A4 template.",
            Yazdir = "Print",
            YazdirilamadI = "The print dialog could not be opened — use your browser's “Print” menu item instead.",
            YazdirKilidi = "No document has been loaded to print.",
            BelgeNo = "Document no",
            Dayanak = "Legal basis",
            Duzenleyen = "Issued by",
            Tutarlar = "Amounts",
            DuzenlemeTarihi = "Issue date",
            SatirYok = "This document has no lines.",
            KimlikEksik = "(business name not filled in)",
            KimlikEksikGerekce = "Your shop's address or tax details are missing from this document. Enter them once and they appear on every document.",
            TuretilmisGerekce = "This document is derived from a ledger entry; the document itself is not editable.",
            KaynaktaDuzelt = "To correct the record, go to its source (entry journal / customer card) and use the correction path there.",
            GecersizTur = "Unknown document type: {0}",
            BelgeAc = "Document",
        },
        // PACKAGE P-17 (Y-7) — shop-window price board.
        Pano = new()
        {
            Baslik = "Price Board",
            Aciklama = "Customer-facing buy/sell board. Margins appear only in the management section of this screen and never reach the board.",
            Kaynak = "Source",
            Kalem = "Item",
            Alis = "Buy",
            Satis = "Sell",
            Durum = "Status",
            BosDurum = "No items to show on the board. You can enable items in the management section below.",
            OkunabilirlikUyarisi = "The board has {0} rows; more than {1} rows may not be readable from a distance.",
            TavanNotu = "Server limit: at most {0} rows are shown on the board.",
            AtlananBaslik = "Items left off the board",
            AtlamaSebebi = "Reason",
            SebepKotasyonYok = "The source did not quote a price for this item.",
            SebepFiyatYok = "The item arrived but both buy and sell prices were empty.",
            SebepTersMakas = "The buy price is equal to or above the sell price — either the source data or the margin is wrong.",
            SebepTavanAsildi = "The board's row limit was reached.",
            SebepMarjGecersiz = "The margin drives the price to zero or below — the row was not shown. Reduce the margin.",
            SebepBilinmeyen = "The reason could not be resolved.",
            AyarBaslik = "Board settings (margin / rounding)",
            AyarAciklama = "Margin is a trade secret and never reaches the board; the price is computed on the server and only the result is sent.",
            AyarYetkiYok = "Manager or owner permission is required to view margin settings.",
            Etiket = "Label on the board",
            EtiketIpucu = "Leave empty to use the system name: {0}",
            Sira = "Order",
            AlisMarj = "Buy margin (₺)",
            AlisMarjIpucu = "A negative value is usual and intentional — it buys below the wholesale quote.",
            SatisMarj = "Sell margin (₺)",
            Yuvarlama = "Rounding step",
            YuvarlamaKapali = "No rounding",
            YuvarlamaGecersiz = "The rounding step must be chosen from the list.",
            PanodaGoster = "Show on the board",
            VarsayilanaDon = "Reset to default",
            ZatenVarsayilan = "This item is already at its default values.",
            SiraAralik = "Order must be between 0 and 99.",
            MarjAralik = "The margin is outside the sensible range (±1,000,000 TRY).",
            Kaydedildi = "{0} saved — {1} items are enabled on the board.",
            VarsayilanaDonuldu = "{0} was reset to its default values.",
            VitrinAc = "Open the price board",
        },
        Pusula = new()
        {
            Baslik = "Expense Vouchers",
            HurdaAlimiGir = "Enter a scrap purchase",
            Aciklama = "The document you issue when you buy from someone who has no invoice (e.g. a customer selling old gold).",
            Gecikmis = "Overdue",
            Yaklasan = "Due soon",
            SureGun = "Issue deadline (days)",
            Baslangic = "From",
            Bitis = "To",
            KayitYok = "No expense vouchers in this range.",
            PusulaNo = "Voucher no",
            Satici = "Seller",
            Tckn = "National ID",
            Mal = "Goods",
            MalAciklamasi = "Goods description",
            IptalliDuzenlenemez = "A cancelled voucher cannot be edited. Enter a new scrap purchase with the correct details; a new voucher is issued automatically.",
            PusulaGuncellendi = "Voucher updated.",
            AdresZorunlu = "Seller address cannot be empty: it is a mandatory element of the expense voucher (TPL art.234).",
            MalAciklamasiZorunlu = "Goods description cannot be empty: it is the subject of the document (TPL art.234).",
            Brut = "Gross amount",
            Stopaj = "Withholding",
            Net = "Net paid",
            SonTarih = "Issue deadline",
            Detay = "Details",
            Yazdir = "Print",
            Gecikti = "OVERDUE",
            GunKaldi = "days",
            MaskeNotu = "National ID numbers are masked in the list. The full number is shown only in the detail view, and that access is written to the audit log.",
            IptalEt = "Void the voucher",
            IptalBaslik = "Void the expense voucher",
            IptalOnay = "The document is NOT deleted: it is kept marked 'VOID' and a NEW voucher is issued with the correct details. ⚠ This void invalidates only the DOCUMENT; the stock and cost records of the scrap purchase are UNCHANGED.",
            IptalEdildiRozet = "VOID",
            IptalYapildi = "The expense voucher has been voided. Enter a new scrap purchase with the correct details; a new voucher will be issued automatically.",
            PusulaDetay = "Expense voucher",
            OkumaIziNotu = "Your viewing of this record was written to the audit log (KVKK art.12).",
            Adres = "Address",
            SaticiTipi = "Seller type",
            TeslimTarihi = "Delivery date",
            Belge = "Document",
            Dayanak = "Legal basis",
            Duzenleyen = "Issued by",
            Cins = "Description",
            Gram = "Grams",
            HasGram = "Fine grams",
            Tutarlar = "Amounts",
            Yaziyla = "In words",
            DuzenlemeTarihi = "Issue date",
            DuzenleyenImza = "Issuer (stamp / signature)",
            SaticiImza = "Seller (signature)",
        },
        Sayim = new()
        {
            Baslik = "Stock Count Sessions", YeniOturum = "New Count", Ad = "Name", Sube = "Branch", Kapsam = "Scope",
            KapsamTumu = "All products", KapsamReyon = "Showcase", KapsamKategori = "Category", Ac = "Open Session",
            Kapat = "Close & Post to Ledger", Iptal = "Cancel", Durum = "Status", Acik = "OPEN", Kapandi = "CLOSED",
            Urun = "Product", Sistem = "System", Sayilan = "Counted", Guncel = "Current", Fark = "Diff",
            Sayilmadi = "not counted", HareketVar = "moved during session", BarkodOkut = "Scan barcode / pick product",
            Adet = "Qty", Say = "Count", Ozet = "Summary", Eksik = "Short", Fazla = "Over", NetHas = "Net fine-gold diff",
            HareketliUygula = "Also apply rows that moved during the session",
            HareketliUyari = "These rows had sales/purchases after the session opened; the counted quantity is ambiguous. Not applied without consent.",
            SayilmayanUyari = "Uncounted rows are NOT zeroed; they are not posted and stay marked as not counted.",
            KapatOnay = "Rows with a non-zero difference will be posted as stock-count corrections. Irreversible.",
            KapanisSonucu = "{0} rows applied to stock, {1} rows skipped.",
            IptalEdildi = "Stock count session cancelled; no rows were applied to stock.",
            IptalOnayBaslik = "Cancel stock count",
            IptalOnayMetin = "This stock count session will be cancelled. The action is IRREVERSIBLE: counted rows are not applied to stock and the session cannot be reopened.",
            IptalOnaySayilan = "Rows counted so far: {0} / {1}",
            AdGuncellendi = "Stock-count session name updated.",
            AdIptalDegismez = "A cancelled stock-count session can no longer be changed.",
            AdIptalAlternatif = "Open a new stock-count session.",
            SayfaAciklama = "Stock-count sessions; find a product by barcode, code or name and correct the counted quantity in place.",
            SayildiBandi = "Counted: {0} × {1}",
            OturumDegismez = "A count session's name, scope and branch cannot be changed once opened: the row set is frozen against the stock at opening time, and changing the scope later would make the count contradict itself.",
            OturumDegismezYolu = "Cancel a session opened with the wrong scope (counted rows are not applied to stock) and open a new one with the right scope.",
            SatirDegismez = "A counted row cannot be returned to \"not counted\": the count records that this shelf was in fact counted at that moment.",
            SatirDegismezYolu = "If you entered the wrong quantity, count it again with the right one — the row is updated; if the row must be uncounted, cancel the session and reopen it.",
            IptalOnayla = "Yes, cancel the count",
            OtomatikAd = "Count {0}",
            GramMiktarYaz = "For a product sold by weight, enter the grams.",
            BolumleBaslat = "Start for a shelf or category",
        },
        Fiyat = new()
        {
            Baslik = "Price Lists & Campaigns", Listeler = "Price Lists", Kampanyalar = "Campaigns",
            YeniListe = "New List", YeniKampanya = "New Campaign", Ad = "Name", Tur = "Type",
            HasMarj = "Fine-gold margin (%)", IscilikCarpan = "Labour multiplier (%)", Varsayilan = "Default", Aktif = "Active",
            Baslangic = "Start", Bitis = "End", Hedef = "Target", HedefTumu = "All products",
            HedefKategori = "Category", HedefUrun = "Product", IndirimTuru = "Discount type",
            IscilikYuzde = "% of labour", TutarYuzde = "% of price", TutarTl = "Fixed ₺", Deger = "Value",
            ListeFiyati = "List price", IndirimOncesi = "Pre-discount", Indirim = "Discount", Hediye = "Gift",
            TeklifYok = "No automatic price for this product (silver or no rate) — enter manually.",
            ListeYok = "No price list defined — create a default list in Price Lists.",
            SureUyari = "A campaign may last at most three months (Price Tag Regulation art. 11/3).",
            FiyatListesi = "Price list",
            PasifeAl = "Deactivate", Aktiflestir = "Activate",
            ListeKaydedildi = "Price list saved", KampanyaKaydedildi = "Campaign saved",
            BosListe = "No price list yet — without a price list the POS cannot quote automatically.",
            BosKampanya = "No campaigns yet.",
            IlkListeVarsayilan = "The first list is necessarily the default one.",
            VarsayilanDegisir = "Making this list the default removes the default flag from the current one.",
            VarsayilanKorunur = "The default flag cannot be cleared here: with no default list the POS cannot quote any product automatically.",
            VarsayilanDevret = "Mark another list as 'Default' and save; the default flag moves to it.",
            MarjEksiUyari = "Negative fine-gold margin: this list sells below cost.",
            MarjYuksekUyari = "Fine-gold margin above 100%: the price exceeds twice the fine-gold value.",
            CarpanYuksekUyari = "Labour multiplier above 200%: labour is charged more than twofold.",
            RiskOnayiEtiket = "I am entering this value deliberately",
            RiskOnayiSart = "Confirm the risky value before saving.",
            MarjAralikDisi = "The fine-gold margin must be between -100% and 500%.",
            CarpanAralikDisi = "The labour multiplier must be between 0% and 500%.",
            UrunPaneli = "Product prices with this list",
            PanelIpucu = "Prices are computed on the server; at most 50 rows per page. Single and bulk overrides are managed from this panel.",
            PanelKapat = "Close panel",
            SayfaAciklama = "Price lists and campaigns; every row can be edited, deactivated and reactivated.",
            TurPerakende = "Retail",
            TurToptan = "Wholesale",
            TurOzel = "Special",
            ListePasifeAlindi = "The price list has been deactivated.",
            ListeAktiflestirildi = "The price list has been reactivated.",
            KampanyaPasifeAlindi = "The campaign has been deactivated.",
            KampanyaAktiflestirildi = "The campaign has been reactivated.",
            TopluMarj = "Bulk margin preview (%)",
            TopluMarjNot = "Calculation only: nothing is saved, no product price changes.",
            Ayar = "Karat", Gram = "Fine grams", HasDegeri = "Fine-gold value", MarjTl = "Margin (₺)", MaliyetTl = "Cost",
            IscilikTl = "Labour (₺)", SatisFiyati = "Sale price",
            SebepGumus = "Silver — no automatic price",
            SebepListeYok = "No price list",
            SebepFiyatYok = "No price produced",
            IscilikUyari = "Labour discounts do not apply to products with a fixed sale price.",
            GumusUyari = "Silver products are not priced automatically from the gold fine-rate",
            KurYok = "No current rate snapshot — price preview unavailable.",
            PanelBos = "No products to list for this tenant.",
            PanelKategori = "Category",
            PanelAra = "Search products (code or name)",
            PanelToplam = "Total products matching the filter",
            DahaFazlasiVar = "Page limit reached — narrow down with a category or a search.",
            YalnizGecersizKilinan = "Only products with a per-product price",
            Kod = "Code",
            Kaynak = "Source",
            KaynakListe = "List",
            KaynakUrunOzel = "Per-product",
            KaynakUrunSabit = "Product card fixed price",
            UruneOzel = "Override for this product",
            UruneOzelBaslik = "Per-product price",
            SabitFiyatAlan = "Fixed price (₺)",
            NotAlan = "Note",
            EnAzBirAlan = "Enter at least one of fixed price, fine-gold margin or labour multiplier.",
            GumusSabitUyari = "Silver products accept a FIXED price only; margin/labour derivation relies on the gold fine-rate.",
            UruneOzelKaydedildi = "Per-product price saved.",
            GecersizKilmayiKaldir = "Remove the per-product price",
            GecersizKilmaKaldirildi = "Per-product price deactivated — the price falls back to the list.",
            TopluUygulaSecili = "Bulk apply to selected products",
            TopluKategoriye = "Apply to this category and its sub-branches",
            TopluKilit = "Enter a fine-gold margin or labour multiplier first; at least one product must be selected.",
            TopluOzetBaslik = "Impact summary — awaiting your approval (no price has changed yet)",
            TopluOnayla = "Approve and apply",
            TopluUygulandi = "Bulk price applied — products affected",
            EtkilenenUrun = "Products affected",
            ToplamFark = "Total difference (₺)",
            EskiFiyat = "Previous list price",
            YeniFiyat = "New list price",
            Fark = "Difference",
            Atlananlar = "Skipped",
            SebepSabitFiyat = "Fixed price on the product card — margin has no effect",
            SebepGecersizKilmaSabit = "Per-product fixed price — margin has no effect",
            SaltOkunurBasitMod = "The list price is read-only. Bulk apply and per-product prices open in professional mode.",
        },
        Vade = new()
        {
            Baslik = "Overdue Receivables",   // P-14 (Y-9): single name with the menu
            UfukGun = "Days ahead",
            GecikenToplam = "Overdue",
            YaklasanToplam = "Due Soon",
            VadesizToplam = "Unknown Due Date",
            UfukOtesiToplam = "Beyond horizon (due later)",
            UfukOtesiAciklama = "Open debt falling due after the selected horizon",
            AcikToplam = "Total open receivable",
            DonemdenBagimsiz = "(as of today — independent of the horizon selector)",
            Musteri = "Customer",
            Telefon = "Phone",
            Geciken = "Overdue",
            Yaklasan = "Due Soon",
            Vadesiz = "No Due Date",
            EnEskiGecikme = "Longest Overdue",
            EnYakinVade = "Nearest Due Date",
            Gun = "days",
            Vade = "Due Date",
            VadeGun = "Term (days)",
            VarsayilanVadeGun = "Default term (days)",
            AcikTutar = "Open Amount",
            Durum = "Status",
            Gecikti = "Overdue",
            Yaklasiyor = "Due soon",
            Vadesinde = "On time",
            KayitYok = "No overdue or upcoming debt.",
            VadesizAciklama = "Open balance with no known due date — from records written before this field existed; not assumed to be due today.",
        },
        Mutabakat = new()
        {
            Baslik = "End-of-Day Reconciliation",
            Gun = "Day",
            Tahsilat = "Total collected",
            Nakit = "Cash",
            SatisFaturasi = "Sales invoices",
            Fark = "Difference",
            Durum = "Status",
            Mutabik = "RECONCILED",
            Mutabakatsiz = "DISCREPANCY",
            Satis = "Sales",
            Adet = "items",
            Faturali = "Invoiced",
            Faturasiz = "Not invoiced",
            Hurda = "Scrap purchases",
            Pusula = "Expense vouchers",
            Stopaj = "withholding",
            BelgesizHurda = "Scrap purchases without voucher",
            FaturasizListe = "Transactions without an invoice",
            Saat = "Time",
            Tutar = "Amount",
            OdemeTuru = "Payment type",
        },
        DisaAktarim = new()
        {
            Baslik = "Data Export",
            Aciklama = "Your data is yours. Export it as standard CSV any time and move it to another program.",
            YeniAktarim = "New Export",
            Gerekce = "Reason",
            GerekceOrnek = "e.g. Preparing year-end handover file for the accountant",
            GerekceIpucu = "At least 20 characters. This text is written to the audit trail and cannot be deleted.",
            Kapsam = "Scope",
            KisiselVeri = "personal data",
            TamKimlikIstiyorum = "Export identity fields UNMASKED",
            MaskeAciklama = "National ID, phone and address are masked by default. Unmasked export creates responsibility under KVKK art.12.",
            OnayMetniniYazin = "To confirm, type the following text exactly:",
            HazirlaVeIndir = "Prepare and Download",
            ParolaBasligi = "Archive Password",
            ParolaUyari = "This password will not be shown again. It is required to open the file; do not send it over the SAME channel as the file.",
            Kopyala = "Copy",
            Kopyalandi = "Copied",
            Gecmis = "Export History",
            GecmisBos = "No export has been made yet.",
            TalepDuzeltmeYolu = "An export request is an evidence record; its content is not modified and it is not removed from the list. If you exported the wrong scope, pick the right scope and open a NEW request — the old download link expires on its own.",
            SatirSayisi = "Rows",
            Maskeli = "Masked",
            TamKimlik = "Full identity",
            GiderPusulasi = "Expense Vouchers",
        },
        Kurulum = new()
        {
            Baslik = "Setup Wizard",
            Aciklama = "Set up the shop's first day here: business and branch, users, current rates, cash/bank opening balances and customer opening balances. Opening records are posted to the ledger as \"Opening transfer\"; no money changes hands.",
            AdimIsletme = "Business and branch",
            AdimKullanicilar = "Users and roles",
            AdimKur = "Current rates",
            AdimKasaBanka = "Cash and bank opening",
            AdimCariAcilis = "Customer opening balances",
            AdimStokAcilis = "Stock opening (not in this release)",
            AdimBitti = "Completed",
            AdimEksik = "Missing",
            AdimAtlandi = "Skipped",
            AdimAtla = "Skip this step",
            AdimGeriAl = "Undo skip",
            AcilisBasligi = "Opening balance",
            AcilisAciklama = "Run the DRY RUN first: the server validates the rows and creates nothing. If the result is clean you can apply it.",
            TurKasa = "Cash register",
            TurBanka = "Bank account",
            TurCari = "Customer / account",
            HedefSec = "Record",
            TlTutar = "₺ amount",
            HasGram = "Fine gram (gold debt)",
            SatirEkle = "Add row",
            SatirSil = "Remove row",
            NegatifAcilisYok = "A cash or bank opening balance cannot be negative; a negative balance is a debt and is entered as a customer account.",
            ProvaCalistir = "Run dry run",
            ProvaBasligi = "Dry run result",
            ProvaAciklama = "Nothing was written in this step.",
            ProvaTemiz = "All rows are valid — ready to apply.",
            Uygula = "Apply opening",
            UygulandiMesaji = "Opening applied",
            GeriAl = "Undo this opening",
            GeriAlOnay = "The opening records of this session will be reversed with a counter-entry (no ledger row is deleted). Continue?",
            GeriAlindiMesaji = "Opening reversed (counter-entry written)",
            Muhurle = "Seal the setup",
            MuhurOnay = "This action cannot be undone. After sealing, opening balances can no longer be entered, changed or reversed; the only correction path is a storno (counter-entry or customer adjustment voucher).",
            MuhurlendiMesaji = "Setup sealed",
            MuhurGerekce = "Sealing reason",
            MuhurIsletmeAdiYaz = "To confirm, type the business name exactly: {0}",
            MuhurIsletmeAdiUyusmuyor = "The name you typed does not exactly match the business name.",
            MuhurBilgisi = "Seal date",
            PencereBitis = "Opening window ends",
            PencereKapandi = "The opening window is closed — balance corrections now go through the normal ledger paths.",
            LimitAsimiOnayi = "I have seen the rows exceeding the credit limit and approve the opening",
            KisiselVeriUyarisi = "This list carries customer data; this view is recorded in the audit log (KVKK art.12).",
            SatirNo = "Row",
            HataKodu = "Code",
            HataMesaji = "Reason",
            HataYok = "No invalid rows.",
            UyariBasligi = "Warnings (row is still applied)",
            SinirNotu = "At most {0} rows can be submitted in one opening operation.",
            BosDurum = "You have not added any opening rows yet.",
            AcilisDegismez = "The opening entry is posted to the ledger and cannot be edited in place (Turkish Tax Procedure Law art.219). Before sealing you can reverse this session; after sealing you can issue a customer adjustment voucher.",
            KurYok = "No exchange rate record yet — the opening cannot be written.",
            KurEkle = "Go to rates screen",
            AdimaGit = "Go to step",
                    NegatifAcilisGerekce = "Why a cash or bank opening cannot be negative: neither a cash drawer nor a bank deposit can physically hold a negative balance. A negative balance is a DEBT, and debts belong to a customer account. Overdraft (KMH) is a separate product; its decision is pending (K-24), so this rule stays conservative for now.",
            HataKimligi = "Error reference",
        },
        IceAktarim = new()
        {
            Baslik = "Data Import",
            Aciklama = "Paste your customer and product cards from your old program into our template; run a DRY RUN first, review row by row, then apply in one click. Past sales and cash movements are not imported.",
            Kaynak = "What to import",
            KaynakCari = "Customer / account cards",
            KaynakUrun = "Product cards",
            SablonIndir = "Download empty template (CSV)",
            SablonIpucu = "Open it in Excel, paste your cards, then save with \"Save As → CSV UTF-8\". Do not rename the columns.",
            DosyaSec = "CSV file",
            DosyaIpucu = "Only UTF-8 encoded CSV is accepted; .xlsx files are not read directly.",
            YinelenenPolitikasi = "Rows that already exist",
            PolitikaHata = "Treat as error (recommended)",
            PolitikaAtla = "Skip and continue",
            PolitikaIpucu = "Overwriting an existing record is deliberately not offered: an overwritten record cannot be undone.",
            ProvaCalistir = "Run dry run",
            ProvaBasligi = "Dry run result",
            ProvaAciklama = "Nothing was created in this step. Review the result below and confirm to apply.",
            ToplamSatir = "Total rows",
            GecerliSatir = "Valid",
            HataliSatir = "Invalid",
            AtlananSatir = "Skipped",
            YazilanKayit = "Records written",
            BilinmeyenKolon = "Unrecognised columns (will be ignored)",
            SatirNo = "Row",
            HataKodu = "Code",
            HataMesaji = "Reason",
            HataYok = "No invalid rows.",
            OnayMetniniYazin = "To apply, type the following text exactly:",
            Uygula = "Confirm and apply",
            UygulandiMesaji = "Import applied",
            GeriAl = "Undo this import",
            GeriAlOnay = "Records created in this session will be DEACTIVATED (not deleted). Continue?",
            GeriAlindiMesaji = "Import undone; records deactivated",
            Iptal = "Cancel dry run",
            IptalOnay = "This dry run will be cancelled. Continue?",
            Gecmis = "Import History",
            GecmisBos = "No import has been made yet.",
            Durum = "Status",
            Dosya = "File",
            SatirlarBos = "No rows to show.",
            SatirDegismez = "Import row",
            SatirDegismezYol = "A row result is an evidence record and is not modified. To correct it, fix the file and run a new dry run.",
            GecmisHareketYok = "Past sales, invoices and cash movements are NOT imported: backdated movements would miscalculate documentation and AML thresholds.",
            Kaydet = "Update reason",
        },
        Etiket = new()
        {
            Baslik = "Product Labels",
            Aciklama = "Print barcode labels. Synthetic / lab-grown stone wording is added automatically (art.11/e).",
            AramaIpucu = "Product name, code or barcode",
            KopyaAdedi = "Copies per product",
            TumunuSec = "Select all",
            SecimiTemizle = "Clear selection",
            Yazdir = "Print Labels",
            Sec = "Select",
            BarkodYok = "no barcode",
        },
        Belge = new()
        {
            Baslik = "Documents",
            DosyaSec = "Choose file",
            Yukle = "Upload",
            Indir = "Download",
            PasifeAl = "Deactivate",
            Tur = "Document type",
            Boyut = "Size",
            BoyutAsildi = "File too large — PDF up to 5 MB, image up to 2 MB.",
            TurGecersiz = "This file type is not accepted — only PDF, JPEG and PNG can be uploaded.",
            TavanAsildi = "Document quota exceeded.",
            BelgeYok = "No documents added to this record yet",
            Yuklendi = "Document uploaded.",
            PasifeAlindi = "Document deactivated (not deleted, the record is kept).",
            TurSertifika = "Certificate (PDF)",
            TurGorsel = "Image",
            TurFotograf = "Photo",
            TurEk = "Attachment",
        },
        Duzeltme = new()
        {
            Baslik = "How to correct this",
            DuzenlenemezBaslik = "This record cannot be changed",
            AlternatifYol = "Correct it like this:",
            StornoDugme = "Correct (reversing entry)",
            StornoHenuzYok = "This transaction type cannot be voided by a reversing entry because stock and cost lots would have to be rolled back. Use a return for sales/purchases; for other types enter an opposite record and write the reason in its description.",
            Gerekce = "Reason",
            EnAzKarakter = "at least {0} characters",

            SerbestGerekce = "This record can be edited in place.",
            DefterGerekce = "A posted ledger line is never deleted or edited in place (Turkish Tax Procedure Law art. 219). It is corrected only with a reversing entry.",
            DenetimGerekce = "The audit trail only grows and is never edited — it is the answer to who read and who wrote what.",
            MasakGerekce = "AML (MASAK) records are statutory records; their content cannot be altered afterwards. File a new record and explain the previous one.",
            SistemGerekce = "This row is produced by the system (rates, cost lots, session records) and is not edited by hand.",
            TuretilmisGerekce = "The balance is derived from individual movements and cannot be written directly. Correct it with a manual correction voucher.",
            KapanisGerekce = "The day-end closing is final; its count and variance cannot be changed afterwards.",
            SayimGerekce = "A stocktake session and its lines are frozen once closed; open a new session instead.",
            IceAktarimGerekce = "An import row is the result of a row in the file and is an evidence record; it is not modified. To correct it, fix the file and run a new dry run.",
            DisaAktarimGerekce = "A data export request is an evidence record (KVKK art. 11) and is not modified.",
            BelgeGerekce = "Document content is never modified; deactivate the wrong file and upload the correct one.",
            FaturaGerekce = "Invoice amounts and lines are not edited in place; the invoice is cancelled or corrected with a reversing entry.",
            PusulaGerekce = "The content of an expense voucher is not edited in place once issued (Turkish Tax Procedure Law art. 234); the document is voided with a reason and reissued correctly.",
            UcYokGerekce = "This record type is read-only for now; corrections are made with a reversing entry or a correction slip.",
            StornoYokGerekce = "This transaction type cannot be voided by a reversing entry because stock and cost lots would have to be rolled back.",
            ListeBos = "There are no records in this list yet.",
            ListeSiniri = "The server returns at most {0} records in this list; narrow it down with the filters to see the rest.",
            AralikDisi = "The value must be between {0} and {1}.",
        },
        SayfaDuzeltme = new()
        {
            PusulaDuzeltmeYolu = "The CONTENT of an expense voucher is not changed once issued (Turkish Tax Procedure Law art. 234); the correction path is VOID + NEW VOUCHER: void the document with a reason using 'Void the voucher' below, then enter a new scrap purchase with the correct details. ⚠ The void invalidates only the DOCUMENT; the stock and cost records of the scrap purchase are UNCHANGED — if the goods were never actually purchased, contact your accountant.",
            SaticiTipiNihai = "Not a taxpayer (sale of personal belongings)",
            SaticiTipiEsnaf = "Benefiting from the small-trader exemption",
            SaticiTipiMukellef = "Registered taxpayer (issues an invoice)",
            IsletmeKimligi = "Business identity",
            VergiDairesi = "Tax office",
            VergiNo = "Tax ID / national ID",
            IsletmeAdresi = "Address",
            IsletmeTelefonu = "Phone",
            IsletmeBilgisiEksik = "The business address and tax office/number are not filled in; the document is printed without them (art. 234 content stays incomplete).",
            IsletmeKartinaGit = "Enter business details",

            TespitDuzeltmeYolu = "An identity verification is a statutory record taken at the moment of the transaction and is not corrected afterwards. If the details are wrong, record a new transaction with a fresh verification; the old record is masked at the end of its retention period.",
            BildirimDuzeltmeYolu = "The amount and reason of a report are not changed afterwards. A report filed by mistake is CLOSED with a written reason; a new one is filed with the correct details.",
            IslemAciklamasi = "Transaction description",
            TutarPozitif = "The transaction amount must be greater than 0.",
            GerekceEksik = "The reason must be at least {0} characters; {1} more needed.",
            KaydetKilidi = "Save unlocks once the amount and the reason are valid.",
            BelgeTuruAdlari = new()
            {
                ["TC_KIMLIK"] = "National ID card",
                ["PASAPORT"] = "Passport",
                ["SURUCU_BELGESI"] = "Driving licence",
                ["MAVI_KART"] = "Blue card",
                ["VERGI_LEVHASI"] = "Tax registration certificate",
            },

            RaporAciklamasi = "Monthly summary, day-end reconciliation, receivable ageing and stock value.",
            KarKaynagi = "Profit source",
            KarKaynagiServis = "Profit & loss calculation",
            KarKaynagiYok = "Could not be calculated",
            SatisAlisFarki = "Sales-purchase cash difference",
            SatisAlisFarkiAciklama = "Sales total − purchase total. This is NOT profit; profit is cost based and is calculated on the Profit & Loss screen.",
            NetHasHareketi = "Net fine-gold movement",
            NetHasAciklama = "Sum of the fine-gold grams of every movement in the period (in − out). It is NOT the fine gold held in stock.",
            Veresiye = "Credit sales",
            TahsilatToplam = "Collections",
            FarkTanimi = "DIFFERENCE = sales total − invoices linked to receipts − invoices not linked to any receipt",
            BagsizFaturalar = "Invoices not linked to a receipt",
            BagliFaturalar = "Invoices linked to a receipt",
            FaturaNo = "Invoice no",
            RaporDuzeltmeYolu = "Report figures are derived from transaction records and are not edited on the report screen. Correct the underlying transaction on its own screen with a reversing entry.",

            VadeAciklamasi = "Overdue and upcoming customer debts.",
            UfukAciklama = "How many days ahead to look. Default is 7 days; the total open receivable is independent of this choice.",
            CariKartinaGit = "Open the account card",
            TelefonuAra = "Call the number",

            DenetimAciklamasi = "Who read what and who wrote what — a record that only grows and is never modified (KVKK art. 12).",
            RolEtiketi = "Role",
            YalnizHassasEtiket = "Personal-data access only",
            VarlikAdlari = new()
            {
                ["Tenant"] = "Business",
                ["Sube"] = "Branch",
                ["Kullanici"] = "User",
                ["Cari"] = "Account",
                ["CariRiskProfili"] = "Risk profile",
                ["Urun"] = "Product",
                ["Stok"] = "Stock",
                ["Kasa"] = "Cash drawer",
                ["BankaHesap"] = "Bank account",
                ["FiyatListesi"] = "Price list",
                ["Kampanya"] = "Campaign",
                ["FiyatListesiUrunFiyati"] = "List product price",
                ["UrunKategorisi"] = "Product category",
                ["RolYetki"] = "Role permission",
                ["Belge"] = "Document",
                ["Hareket"] = "Ledger transaction",
                ["KasaHareket"] = "Cash movement",
                ["BankaHareket"] = "Bank movement",
                ["GiderPusulasi"] = "Expense voucher",
                ["Fatura"] = "Invoice",
                ["FaturaKalemi"] = "Invoice line",
                ["DenetimKaydi"] = "Audit record",
                ["MasakKimlikTespiti"] = "Identity verification",
                ["MasakBeyani"] = "Customer declaration",
                ["MasakSupheliBildirim"] = "Suspicious transaction report",
                ["DisaAktarimTalebi"] = "Data export request",
                ["IceAktarimOturumu"] = "Data import session",
                ["IceAktarimSatiri"] = "Import row",
                ["KurulumDurumu"] = "Setup status",
                ["AcilisKalemi"] = "Opening balance item",
                ["GunKapanisi"] = "Day-end closing",
                ["SayimOturumu"] = "Stocktake session",
                ["SayimSatiri"] = "Stocktake line",
                ["KurSnapshot"] = "Rate snapshot",
                ["RefreshToken"] = "Refresh token",
                ["StokPartisi"] = "Stock lot",
                ["StokPartiTuketimi"] = "Stock lot consumption",
                ["CariBakiye"] = "Account balance",
                ["KullaniciMfa"] = "Two-step verification",
                ["MfaKurtarmaKodu"] = "Recovery code",
                ["Reyon"] = "Display case",
                ["Bildirim"] = "Notification",
                ["IsCalismasi"] = "Scheduled job",
                ["Siparis"] = "Order",
                ["SiparisKalemi"] = "Order line",
                ["SiparisGecisi"] = "Order status transition",
            },

            MaliyetEksikAria = "product with missing cost",
        },
        Isletme = new()
        {
            Baslik = "Shop & Branches",   // DIL-EKRAN: 375 px'te 36 px başlık "Business & Branches" iki satıra bölünüyordu (360 > 351 px)
            Aciklama = "Business details, subscription plan and branch management. This screen is for the OWNER role only.",
            IsletmeBilgisi = "Business details",
            IsletmeAdi = "Business name",
            Plan = "Plan",
            PlanBaslangic = "Starter",
            PlanPro = "Pro",
            PlanKurumsal = "Enterprise",
            PlanDegistir = "Change plan",
            AiModulu = "AI module",
            AiAciklama = "While off, no AI-assisted suggestions are produced and no data leaves the system.",
            AiAcik = "On",
            AiKapali = "Off",
            Subeler = "Branches",
            YeniSube = "New Branch",
            SubeAdi = "Branch name",
            Adres = "Address",
            Telefon = "Phone",
            SubeDetay = "Details",
            SubeYok = "No branches defined yet",
            PasifeAl = "Deactivate",
            PasifeAlOnay = "The branch will be deactivated. Its history is kept, but it can no longer be selected. Confirm?",
            SonSubeUyarisi = "The only active branch cannot be deactivated — no branch would remain for transactions.",
            YalnizSahip = "Only the shop owner can change this setting.",
            DurumBasligi = "Business status",
            DurumDeneme = "Trial",
            DurumAktif = "Active",
            DurumSaltOkunur = "Read only",
            KalanGun = "days left",
            DenemeBitti = "Your trial period has ended.",
            SaltOkunurUyari = "Your records are intact and viewable; no new entries can be made. Contact your administrator for an extension.",
            VerileriDisaAktar = "Export your data",
            SureUzatmaNotu = "Extensions are granted only by the system administrator.",
            YazmaKapaliKilidi = "Business is read only — saving is disabled",
            AbonelikBasligi = "Subscription",
            AbonelikAciklama = "Which plan you are on, when the period started and when it ends. Every plan change is written to the audit trail.",
            AbonelikDonemi = "Current period",
            AbonelikBaslangic = "Start",
            AbonelikBitis = "End",
            AbonelikSuresiz = "Open ended",
            AbonelikKalanGun = "Days left",
            AbonelikSuresiDoldu = "Your subscription period has ended: your records remain and can be viewed and exported; renewing is required before new records can be created.",
            AbonelikYok = "No subscription period recorded for this business yet; the plan is read from the business record.",
            PlanKaynakRozeti = "from subscription record",
            PlanYukseltmeKapali = "Upgrades cannot be made from the business account: widening subscription rights requires an authorised operator. Please contact your administrator.",
            PlanIndirmeYok = "There is no lower plan: you are already on the lowest one.",
            PlanIndirOnay = "The plan will be downgraded. No data is deleted, only the scope of rights narrows. Confirm?",
            AbonelikGecmisi = "Period history",
            AbonelikGecmisYok = "No period records yet.",
            AbonelikSinirNotu = "The 50 most recent periods are listed.",
            AbonelikKaynakSistem = "operator",
            AbonelikKaynakKiraci = "business",
            DonemdenBagimsiz = "(as of today — independent of the period filter)",
        },
        Emanet = new()
        {
            Baslik = "Safekeeping",
            Aciklama = "Gold the customer left with you. It is NOT YOURS: excluded from stock value, tracked only on the fine-gold leg.",
            Musteri = "Customer",
            MusteriSecin = "Select a customer",
            Giris = "Take In",
            Cikis = "Return",
            Ayar = "Karat",
            GramAgirlik = "Gross weight (g)",
            TasAgirlik = "Stone weight (g)",
            HasKarsiligi = "Fine gold equivalent",
            Aciklamasi = "Note",
            ElimizdekiEmanet = "Currently held",
            IadeEdilecekHas = "Gold to return (g fine)",
            IadeFazlaUyari = "You cannot return more than you hold.",
            TasGramdanKucuk = "The stone weight must be less than the total gram weight.",
            GirisKaydedildi = "The item has been taken into custody and recorded in the customer's custody ledger.",
            CikisKaydedildi = "The custody return has been recorded.",
            HareketDegismez = "This line cannot be voided by a reversing entry: it has either already been reversed, is itself a reversal, or belongs to a multi-leg receipt.",
            HareketDegismezYolu = "For a multi-leg receipt use a correction receipt for the whole document; an already reversed line is never reversed twice.",
            AciklamaGuncellendi = "Safekeeping entry description updated.",
            AciklamaKilitliAlanlar = "Weight, fineness, metal and customer are locked (they form the safekeeping balance) — the correction path is a reversal.",
            Hareketler = "Safekeeping movements",
            HareketYok = "No safekeeping movements for this customer",
            Tur = "Type",
            Tarih = "Date",
            GirisTuru = "In",
            CikisTuru = "Out",
            KurYok = "Could not read the current rate — safekeeping cannot be recorded without a rate snapshot.",
            SubeUyarisi = "The user must have a branch assigned to record safekeeping.",
            EmanetMulkiyetUyarisi = "⚠ Safekeeping gold does not belong to the business; it is EXCLUDED from profit & loss and stock valuation.",
            MusteriAra = "Search customer (name, phone)",
            AramaDaraltin = "The list hit the server limit (50 records) — narrow your search if the customer is missing.",
            HesaplananHas = "derived from karat and grams",
        },
        UrunAgaci = new()
        {
            Baslik = "Product Tree & Display Units",
            Aciklama = "The category tree lets you filter the product list by breakdown; the display unit says where the goods physically sit.",
            Kategoriler = "Categories",
            Reyonlar = "Display units",
            ReyonSubeDegismez = "An existing display unit cannot change branch; create a new one in the other branch.",
            YeniKategori = "New Category",
            YeniReyon = "New Display Unit",
            KategoriAdi = "Category name",
            UstKategori = "Parent category",
            KokSeviye = "— top level —",
            Sira = "Order",
            ReyonAdi = "Display unit name",
            SubeDegismez = "A section's branch cannot be changed later; create a new section in the other branch and move the items there.",
            ReyonPasifOnay = "This section will be deactivated. Nothing is deleted; if it still holds items the request is rejected, and you can reactivate it at any time.",
            ReyonTipi = "Type",
            TipVitrin = "Showcase",
            TipKasa = "Safe",
            TipDepo = "Warehouse",
            TipCanta = "Travel case",
            Sube = "Branch",
            UrunSayisi = "products",
            KategoriYok = "No categories defined yet",
            ReyonYok = "No display units defined yet",
            DonguUyarisi = "A category cannot be moved into its own subtree.",
            KapsamUyarisi = "⚠ Display units now also split the stock quantity (see the placed-quantity column); however STOCK-COUNT SCOPE is still read from the product CARD — a display-unit count covers products whose default display unit it is, not the goods physically placed there.",
                    KategoriPasifeAl = "Deactivate",
            KategoriPasifOnay = "The category is NOT deleted, it is deactivated: the breakdown on product cards is kept and the category disappears from the tree. The way back is the “Reactivate” button on the same row.",
            KategoriBagliKayit = "Linked records: {0} products · {1} active sub-categories",
            KategoriBagliYok = "No linked products or active sub-categories.",
            KategoriPasifRozet = "Inactive",
        },
        Yerlesim = new()
        {
            Baslik = "Display unit placement",
            Aciklama = "Branch stock broken down by display unit. The stock record remains the single source of the total; this screen keeps its showcase/safe/storeroom distribution.",
            Atanmamis = "Unassigned",
            AtanmamisIpucu = "Goods whose physical location has not been reported yet. It is not a separate record: branch stock minus the total placed into display units.",
            YerlesikAdet = "Placed quantity",
            StokAdedi = "Branch stock",
            Yerlestir = "Place into display unit",
            Tasi = "Move between display units",
            GeriAl = "Return to unassigned",
            KaynakReyon = "Source display unit",
            HedefReyon = "Target display unit",
            Reyon = "Display unit",
            ReyonIcerigi = "Display unit contents",
            ReyonSecimi = "Display unit (optional)",
            ReyonSecimiIpucu = "If left empty, unassigned goods are used first; if they are not enough, display units are used in name order and you are told which ones were used.",
            Adet = "Quantity",
            YerlesimYok = "No goods of this product have been placed into a display unit — everything is unassigned.",
            ReyonBos = "This display unit is empty.",
            YetersizYerlesim = "This display unit does not hold enough goods.",
            ReyonBosaltilmali = "Goods are still in this display unit; empty it first.",
            HedefSecilmeli = "A target display unit must be selected.",
            AdetGecersiz = "Quantity must be greater than zero.",
            AdetAtanmamisiAsiyor = "Quantity cannot exceed the unassigned amount.",
            ReyonStokRaporu = "Display unit stock report",
            MadenAyriToplanir = "Grams of different metals are not added together — each metal has its own row.",
            Sinirda = "Server limit reached — there is more; narrow it down with filters.",
            Sorumlu = "Responsible",
            UrunKodu = "Product code",
            UrunAdi = "Product",
            UrunSecilmeli = "A product to move must be selected.",
        },
        KarZarar = new()
        {
            Baslik = "Profit & Loss",
            Aciklama = "TL profit and FINE GOLD profit, separately. When gold rises you can book a TL profit while losing grams — this screen shows both.",
            TlBacagi = "TL leg — what accounting sees",
            HasBacagi = "FINE leg — what the jeweller sees",
            Hasilat = "Revenue",
            Maliyet = "Cost",
            Kar = "Profit",
            Marj = "Margin",
            SatisAdedi = "Sales",
            BuAy = "This month",
            GecenAy = "Last month",
            Bugun = "Today",
            HasKaybiBasligi = "⚠ Profit in TL, LOSS in grams",
            HasKaybiMetin = "You booked {0} profit this period but LOST {1} g of fine gold. You cannot replace what you sold with the same weight.",
            MaliyetiBilinen = "Cost known",
            MaliyetiBilinmeyen = "Cost unknown",
            MaliyetEksik = "cost missing",
            UrunBazli = "Profitability by Product",
            StokDegerlemesi = "Inventory Valuation",
            StokAdet = "Units in stock",
            StokHas = "Fine grams in stock",
            StokMaliyeti = "Inventory cost",
            VeriYok = "No sales in this period.",
            GuvenTam = "Confidence: FULL",
            GuvenYuksek = "Confidence: HIGH",
            GuvenOrta = "Confidence: MEDIUM",
            GuvenDusuk = "Confidence: LOW",
            GuvenVeriYok = "No data",
            BrutHasilat = "Gross revenue",
            Iade = "Returns",
            NetHasilat = "Net revenue",
            BrutKar = "Gross profit",
            DonemKari = "Period profit",
            IscilikGeliri = "Workmanship income",
            TakasFarki = "Trade-in difference",
            TakasFarkiNotu = "A trade-in row records only the DIFFERENCE in TL (which may be negative), not the gross value of the goods exchanged. That gross value is never written to the ledger, so trade-in turnover cannot be measured — this is why the difference is NOT added to revenue and is shown separately here.",
            KarKoprusu = "Profit Bridge",
            Kalem = "Line",
            Tutar = "Amount",
            Karsilastir = "Compare",
            KarsilastirmaYok = "No comparison",
            OncekiDonem = "Previous period",
            GecenYil = "Same period last year",
            Degisim = "Change",
            Storno = "Reversal",
            StornoNotu = "A reversal pair (the voided entry and its counter-entry) is excluded from turnover, count and profit totals; it is counted separately here.",
            HasKar = "Fine profit",
            HasDefterGram = "Ledger fine grams",
            HasDefterGramMaden = "Ledger fine grams ({0})",
            MadenToplanmaz = "This period contains more than one metal. Fine grams are shown SEPARATELY per metal; gold and silver grams are not added up — their sum would be meaningless.",
            HasTanimNotu = "Fine revenue is two different quantities: the figure above is the FINE EQUIVALENT of TL revenue at the exchange rate of the sale day (profit is measured on this leg). \"Ledger fine grams\" is the fine weight of the goods that actually changed hands (the value shown in the monthly summary). They are not expected to be equal.",
            IadeAdedi = "Returns count",
        },
        Bildirim = new()
        {
            Baslik = "Notifications",
            EkranAciklama = "System generated alerts and reminders; closed notifications stay on record.",
            Acik = "Open",
            Kapali = "Closed",
            Tumu = "All",
            Onem = "Severity",
            Kritik = "Critical",
            Uyari = "Warning",
            Bilgi = "Info",
            Kapat = "Close",
            KapatBaslik = "Close notification",
            KapatOnay = "This notification will be closed. A closed notification cannot be reopened; if the condition persists the job creates a new one tomorrow.",
            KapatmaNotu = "Closing note (optional)",
            KapatildiEtiket = "Closed",
            Git = "Go to related screen",
            Yenile = "Refresh",
            BosDurum = "No pending notifications.",
            BosDurumKapali = "No closed notifications in this window.",
            Kapatildi = "Notification closed",
            SinirUyarisi = "The server returns at most {0} records — there are more. Change the status tab to narrow it down.",
            KapaliPencere = "Closed notifications are listed for the last {0} days only. Records are NOT deleted, they drop off the list.",
            DegismezGerekce = "A closed notification cannot be edited: closing is a stamp showing who closed it and when.",
            DegismezAlternatif = "If the condition persists the related job creates a new notification; you can also run it now from the job panel.",
            Isler = "Scheduled jobs",
            IslerAciklama = "Jobs run automatically; the button below runs the same job NOW. No duplicate notification is created on the same day.",
            IsAdi = "Job",
            IsDurumu = "Status",
            SonCalisma = "Last run",
            SimdiCalistir = "Run now",
            Calisiyor = "Running...",
            KapaliIs = "Disabled",
            KapaliIsIpucu = "This job is disabled in configuration (Isler section); it cannot be run until enabled.",
            CalismaGecmisi = "Run history",
            Anahtar = "Run key",
            Sure = "Duration (ms)",
            UretilenBildirim = "Notifications created",
            IncelenenKayit = "Records examined",
            HataOzeti = "Error summary",
            ElleTetiklendi = "manual",
            RozetEtiket = "Open notifications",
            YalnizYoneticiIs = "Job management is for the owner and manager only.",
            Ayarlar = "Scheduled job settings",
            AyarlarAciklama = "These settings apply to THIS business only. A field left empty falls back to the installation default. A job disabled in the installation CANNOT be enabled here; only an enabled job can be switched off.",
            BaslangicSaat = "Hour the run may start",
            BaslangicSaatIpucu = "The scheduled run does not start before this local hour. Manual runs are not affected.",
            VadeUfukGun = "Due-date reminder horizon (days)",
            VadeUfukGunIpucu = "Reminders are produced for debts falling due within this many days.",
            MasakUyariGun = "AML filing warning threshold (days)",
            MasakUyariGunIpucu = "A warning is produced when this many days remain before the suspicious-transaction filing deadline.",
            IsAcikKapali = "Enabled",
            AyarKaydet = "Save settings",
            AyarVarsayilanaDon = "Reset to default",
            AyarKaydedildi = "Scheduled job settings saved",
            AyarAraligi = "Accepted range: {0}-{1}",
            VarsayilanDeger = "Installation default: {0}",
            KiraciKapatti = "Disabled for this business",
            KiraciKapattiIpucu = "You disabled this job; you can enable it again from the settings.",
            YapilandirmaKapaliIpucu = "This job is disabled in the installation configuration; it cannot be enabled from the screen (setup/regulatory gate).",
            AyarDegismezGerekce = "Past run records cannot be changed: each row is the evidence of one run.",
            AyarDegismezYolu = "To change the behaviour edit the settings above, then run the job again.",
        },
        Vezne = new()
        {
            Baslik = "Cashier desk",
            FisTuru = "Ticket type",
            TurSatis = "Sale",
            TurTahsilat = "Collection",
            TurIscilik = "Workmanship",
            TurAciklama = "A sale ticket carries product lines; collection and workmanship tickets carry a single amount.",
            TurDegismez = "The ticket type cannot be changed later. If opened with the wrong type, cancel it and open a new one.",
            TakasEmanetYok = "Exchange and consignment do not use cashier tickets: exchange is entered directly on the transactions screen, and consignment in/out moves no money.",
            TutarGerekli = "Amount is required and must be greater than zero",
            CariGerekli = "This ticket type is posted to an account: selecting an account is required",
            TutarKalemden = "The amount is calculated from the line items",
            TekBacak = "This ticket type is collected with a single payment method.",
            EkranAciklama = "Collection tickets: the preparer and collector segregation is enforced here.",
            Kuyruk = "Pending tickets",
            FisiAc = "Open ticket",
            Yeni = "New ticket",
            VezneyeGonder = "Send to cashier",
            TahsilEt = "Collect",
            Iptal = "Cancel ticket",
            IptalOnay = "This ticket will be cancelled. The record is not deleted; it is kept with the reason. Confirm?",
            Gerekce = "Cancellation reason",
            GerekceKisa = "Reason must be at least 10 characters",
            Hazirlayan = "Prepared by",
            TahsilEden = "Collected by",
            BosDurum = "No pending tickets",
            KendiFisin = "You cannot collect a ticket you prepared yourself",
            AyrimZorunlu = "Cashier separation required",
            AyrimAciklama = "When on, the person who prepared a ticket cannot take its payment; someone else does.",
            AyrimAcik = "on",
            AyrimKapali = "off",
            Durum = "Status",
            DurumAcik = "Preparing",
            DurumVeznede = "At cashier",
            DurumTahsil = "Collected",
            DurumIptal = "Cancelled",
            TumDurumlar = "All statuses",
            Cari = "Customer",
            Sube = "Branch",
            SubeOturumdan = "from session",
            Aciklama = "Note",
            Kalemler = "Line items",
            KalemEkle = "Add line",
            KalemSil = "Remove line",
            Urun = "Product",
            Adet = "Quantity",
            BirimFiyat = "Unit price (₺)",
            Iscilik = "Labour (₺)",
            KalemSayisi = "Lines",
            Toplam = "Total",
            DamgaliToplam = "Stamped total",
            ToplamTahmini = "Estimate — fixed when sent to the cashier",
            ToplamDamgali = "Final — fixed when sent to the cashier",
            Bacaklar = "Payment legs",
            BacakEkle = "Add leg",
            BacakSil = "Remove leg",
            Arac = "Payment method",
            Tutar = "Amount (₺)",
            Kasa = "Cash register",
            KasaOtomatik = "single register - selected automatically",
            Kalan = "Remaining",
            KalaniAta = "Assign remainder",
            Duzenle = "Edit",
            Sec = "Select",
            SinirUyarisi = "The server returns at most {0} rows; there are more. Narrow it with the status filter.",
            KurYok = "No live rate snapshot found; cannot collect",
            KalemGerekli = "At least one line item is required",
            BacakGerekli = "At least one payment leg is required",
            ToplamTutmuyor = "Leg total must equal the ticket total exactly",
            Olusturuldu = "Opened",
            Veznelendi = "Sent to cashier",
            TahsilEdildi = "Collected",
            IptalEdildi = "Cancelled",
            DefterFisi = "Ledger ticket",
            MutabakatGerekli = "No ledger counterpart visible - reconciliation required",
            DegismezGerekce = "A collected or cancelled ticket cannot be changed.",
            DegismezAlternatif = "To refund money, use the sales return path on the Transactions screen.",
            VezneleDegismez = "A ticket sent to the cashier has its amount stamped: the cash desk is holding that amount, so its lines, customer and description are no longer edited in place.",
            VezneleDegismezYolu = "Cancel the ticket with a reason (the reason is written to the audit trail) and open a new ticket with the correct lines.",
            DefterKapilari = "At collection the document and ID checks, the day close and the stock check run on their own.",
            KurTahsilAninda = "The rate is the current rate at collection time",
            Guncellendi = "Ticket updated",
            AracNakit = "Cash",
            AracKart = "Card",
            AracHavale = "Bank transfer",
            AracVeresiye = "On account",
        },
        Evrak = new()
        {
            Baslik = "Cheque / Note Portfolio",
            TakvimBaslik = "Due Calendar",
            SayfaAciklama = "Cheques and promissory notes received from customers. Recording a note closes the customer's debt; if it bounces the debt is recreated.",
            TakvimAciklama = "How much falls due on each day. Click a day to list that day's notes.",
            Yeni = "Record note",
            EvrakTuru = "Instrument type",
            TurDegismez = "A saved instrument's type cannot be changed; cancel it and create a new one.",
            Cek = "Cheque",
            Senet = "Promissory note",
            Durum = "Status",
            Tutar = "Amount",
            VadeGunu = "Due date",
            EvrakNo = "Instrument no",
            BankaAdi = "Bank",
            BankaSube = "Bank branch",
            KesideYeri = "Place of issue",
            KesideTarihi = "Date of issue",
            BorcluAd = "Debtor name",
            BorcluKimlikNo = "Debtor national / tax ID",
            BorcluKimlikNoIpucu = "Stored encrypted; search works only with the full number.",
            KaynakCari = "Customer who handed it over",
            Notlar = "Notes",
            Kaydet = "Save",
            Vazgec = "Cancel",
            Duzenle = "Edit",
            TahsileVer = "Send for collection",
            PortfoyeAl = "Return to portfolio",
            TahsilEdildi = "Collected",
            Karsiliksiz = "Bounced",
            Ciro = "Endorse",
            CiroDevredildi = "Endorsement is deferred to Phase B; instruments cannot be transferred to third parties in this release.",
            Hedef = "Where did the money go",
            HedefKasa = "Cash drawer",
            HedefBanka = "Bank account",
            Kasa = "Cash drawer",
            BankaHesabi = "Bank account",
            Gerekce = "Reason for bouncing",
            GerekceIpucu = "At least 20 characters. This text is written to the audit trail.",
            OnayBaslik = "Mark instrument as bounced",
            OnayMetin = "This RECREATES the customer's debt for the full amount and writes a new ledger row. Nothing is deleted; the way back is collection or reversal.",
            Onayla = "Yes, mark as bounced",
            KayitYok = "No instruments in portfolio",
            KayitYokAciklama = "When you receive a cheque or note from a customer, record it with 'Record note'; the debt closes automatically.",
            Gecikmis = "Overdue",
            Yakinda = "Due soon",
            Gun = "days",
            Adet = "count",
            Tumu = "All",
            PortfoydeToplam = "Total in portfolio",
            TahsileVerilenToplam = "Sent for collection",
            BuHaftaVadeli = "Due this week",
            KarsiliksizToplam = "Bounced total",
            DonemdenBagimsiz = "(as of today — independent of filters)",
            SinirUyarisi = "The server returns at most {0} instruments at once; there are more, narrow your filters.",
            Kaydedildi = "Instrument saved",
            Guncellendi = "Instrument updated",
            DurumPortfoyde = "In portfolio",
            DurumTahsileVerildi = "Sent for collection",
            DurumTahsilEdildi = "Collected",
            DurumKarsiliksiz = "Bounced",
            DurumCiroEdildi = "Endorsed",
            DegismezGerekce = "Amount and the handing-over customer cannot be changed: both produced a posted debt-closing ledger row; changing them would split the balance from the ledger.",
            DegismezYol = "If the amount or the customer is wrong, mark the instrument as bounced (the debt returns) or reverse the entry row, then record the correct instrument again.",
            TakvimeGit = "Due calendar",
            ListeyeGit = "Portfolio list",
            AralikBosBaslik = "No instrument falls due in the selected date range",
            AralikBosAciklama = "The portfolio is not empty, but the due dates fall outside this range. Widen the range or show the whole portfolio.",
            PortfoyToplami = "Portfolio total (period-independent)",
            TumPortfoyuGoster = "Widen range to the whole portfolio",
            SuzgecUygulandi = "Due-date filter applied from the link",
            GenisletmeSiniri = "The portfolio due-date span is longer than the server limit ({0} days); narrow the dates manually.",
            SuzgeciTemizle = "Clear due-date filter",
            KasaSecilmedi = "Select a cash drawer first",
            BankaSecilmedi = "Select a bank account first",
            SonDurum = "This instrument is in its final state; no further action is possible.",
            BakiyeSonrasi = "Customer's current ₺ balance: {0}",
            Eylemler = "Actions",
            ZorunluAlan = "This field is required.",
            KurYok = "Current exchange-rate record unavailable; an instrument cannot be saved without it.",
        },
        Siparis = new()
        {
            Baslik = "Orders",
            EkranAciklama = "Customer orders, external workshop orders and repairs. Orders do not post to the ledger.",
            DetayEkranBaslik = "Order detail",
            DetayEkranAciklama = "Order lines, status and the promised date are edited on this screen.",
            Yeni = "New order",
            DetayVeDuzenleme = "Details and editing — lines, promised date and description are changed on the detail screen",
            No = "Order no",
            Tur = "Type",
            Durum = "Status",
            Cari = "Account",
            Sube = "Branch",
            SozVerilenGun = "Promised date",
            Aciklama = "Note",
            TahminiTutar = "Estimated amount",
            Kalemler = "Items",
            KalemEkle = "Add item",
            KalemSil = "Remove item",
            Urun = "Product",
            UrundenGeldi = "Product comes from the catalogue; the item's product link is preserved when editing.",
            Tanim = "Free-text description",
            Ayar = "Karat",
            Gram = "Grams",
            Adet = "Quantity",
            KalemSayisi = "items",
            Olustur = "Create order",
            Duzenle = "Edit",
            DuzenlemeyiBitir = "Cancel editing",
            Kaydet = "Save changes",
            Detay = "Details",
            GecisGecmisi = "Status history",
            AppendOnly = "Rows are only ever added to this log: they are never edited or deleted.",
            Gerekce = "Reason",
            GecisYap = "Change status",
            IptalBaslik = "Cancel order",
            IptalOnay = "The order will be cancelled. This cannot be undone; the record and reason stay in the log.",
            BosDurum = "No orders match this filter.",
            SinirUyarisi = "The server returns at most {0} orders and there are more. Narrow by type, status or account.",
            Geciken = "Overdue",
            GecikenSuzgeci = "Overdue only",
            TumTurler = "All types",
            TumDurumlar = "All statuses",
            DegismezGerekce = "This order is no longer a draft; its fields can no longer be changed.",
            DegismezAlternatif = "If it is wrong, cancel the order with a reason and create a new one.",
            EmanetUyarisi = "No safekeeping entry was made: the customer's item is NOT VISIBLE IN THE LEDGER. Use the Safekeeping screen to record a safekeeping entry.",
            EmanetHareketId = "Safekeeping entry",
            TeklifAl = "Get a price quote",
            TeklifKaynak = "calculated",
            KurYok = "No current rate record; an estimated amount cannot be entered.",
            CariGerekli = "An account must be selected.",
            KalemGerekli = "At least one item is required: pick a product or type a description.",
            GerekceKisa = "The reason must be at least {0} characters.",
            IzinliGecisYok = "There is no further status available from here.",
            Olusturuldu = "Order created",
            GecisYapildi = "Status updated",
            GecisOnayBaslik = "Confirm the status change",
            GecisOnayMetni = "The order will move from “{0}” to “{1}”. Do you confirm?",
            GeriGecisGerekce = "Reason for moving back (written to the audit trail)",
            GeriAl = "Move back: {0}",
            TaslagaDonNotu = "To edit, first move the order back to “Draft”; the rollback is recorded with a reason.",
            Guncellendi = "Order updated",
            SubeOturumdan = "from session",
            DefterEtkisiYok = "Orders do not write to the ledger: cash, stock and profit/loss are unaffected. Payment and delivery still go through the existing transaction endpoints.",
            TurMusteri = "Customer order",
            TurAtolye = "Workshop order",
            TurTamir = "Repair / service",
            DurumTaslak = "Draft",
            DurumOnaylandi = "Approved",
            DurumAtolyede = "At workshop",
            DurumHazir = "Ready",
            DurumTeslimEdildi = "Delivered",
            DurumIptal = "Cancelled",
        },
        Yonetim = new()
        {
            DisaAktarimAciklama = "Download all your data as an encrypted archive. Every export is written to the history with its reason.",
            KullanicilarAciklama = "Staff accounts, roles and access status. A departing employee's access is disabled, never deleted.",
            YetkilerAciklama = "Defines which role can see which screen. The server has the final say; this matrix is the gate itself.",
            IsletmeSayfaAciklama = "Business identity, subscription plan and branches. Identity fields are printed on documents as seller details.",
            AyarlarAciklama = "Language, theme, interface mode, password and two-step verification for your own account.",
            KilavuzSayfaAciklama = "The user guide opens in a separate tab; the headings below take you straight to a section.",

            KilitSebebi = "Missing",
            Kaydedildi = "Saved",
            ZorunluAlan = "This field is required",

            GerekceSayac = "{0}/{1} characters",
            GerekceKisa = "The reason must be at least {0} characters — the export is written to the audit trail with it.",
            KapsamSecilmedi = "Select at least one scope",
            OnayMetniEksik = "Type the confirmation text exactly",
            KisiselVeriVarsayilanNotu = "The selected scopes contain PERSONAL DATA (accounts, expense vouchers). All scopes are selected on open; untick what you do not need — every exported row is written to the audit trail.",
            TumunuSec = "Select all",
            GecmisSiniri = "Showing the last {0} exports.",

            Siz = "you",
            MfaSutun = "Two-step",
            MfaVar = "On",
            MfaYok = "Off",
            MfaBilinmiyor = "Visible for your own account only",
            KendiRolunDegismez = "You cannot change your own role or access status: the last administrator locking themselves out would lock the whole system.",
            KendiRolunYolu = "Another OWNER can change your role.",
            EpostaGecersiz = "Enter a valid e-mail address",
            AdSoyadBos = "First and last name cannot be empty",

            DegisiklikYok = "no permission has been changed",
            KaydedilmemisDegisiklik = "You have unsaved permission changes.",
            SahipKilitliGerekce = "The OWNER role has access to every screen and cannot be restricted: an owner able to revoke their own rights would be locked out.",
            SahipKilitliYolu = "Give the person you want to restrict the MANAGER role or lower.",

            KimlikBaslik = "Business identity",
            KimlikAciklama = "These fields are printed as seller details on invoices and expense vouchers.",
            KimlikDuzenle = "Edit business identity",
            Adres = "Address",
            Telefon = "Phone",
            VergiDairesi = "Tax office",
            VergiNo = "Tax ID / National ID",
            AdBosOlamaz = "Business name cannot be empty",
            VergiNoGecersiz = "Tax ID must be 10 or 11 digits",
            SubeAktifeAl = "Reactivate",
            SubeAktifeAlmaGerekce = "A disabled branch cannot be re-enabled from this screen: the server has no endpoint for reactivating a branch yet. Rather than a button that does nothing, we state the situation as it is.",
            SubeAktifeAlmaYolu = "You can create a new branch or request this one to be re-enabled.",
            KurKaynagi = "Rate source",
            MaliyetYontemi = "Costing method",
            TuretilenAyarGerekce = "These settings are fixed at installation and underpin every historical cost calculation; changing them later would retroactively distort past profit.",
            TuretilenAyarYolu = "If they must change, it is done together with setup support.",
            TelefonGecersiz = "Phone may contain only digits, spaces and + ( ) - characters",

            ProfilDuzenle = "Edit profile",
            SifreAlanlariBos = "all three password fields must be filled",

            MfaBaslik = "Two-step verification",
            MfaAciklama = "In addition to your password, a 6-digit code from your authenticator app is required. Even a stolen password cannot get into your account.",
            MfaDurumEtkin = "On",
            MfaDurumKapali = "Off",
            MfaDurumBekliyor = "Setup unfinished",
            MfaKur = "Set up two-step verification",
            MfaYarimBaslik = "Setup left unfinished",
            MfaYarimAciklama = "A setup was started earlier but never verified, so sign-in does NOT ask for a code. You can start over or clear the unfinished record.",
            MfaSifirla = "Clear unfinished record",
            MfaSifirlandi = "The unfinished setup was cleared.",
            MfaAdim1 = "1. Add the key below manually to your authenticator app (Google Authenticator, Microsoft Authenticator, etc.).",
            MfaAdim2 = "2. Enter the 6-digit code shown by the app.",
            MfaAdim3 = "3. Save your recovery codes — if you lose your phone they are your only way in.",
            MfaAnahtar = "Setup key",
            MfaAnahtarGizli = "Key hidden — click to reveal",
            MfaAnahtarGoster = "Show key",
            MfaAnahtarGizle = "Hide key",
            MfaOtpauth = "Link (otpauth)",
            MfaKod = "Verification code",
            MfaKodIpucu = "6 digits",
            MfaDogrula = "Verify code",
            MfaKurtarmaBaslik = "Recovery codes",
            MfaKurtarmaUyari = "These codes are shown ONLY ONCE. Each one works a single time. Save them somewhere safe before closing this screen.",
            MfaKurtarmaIndir = "Download codes",
            MfaKurtarmaOnay = "I have saved the recovery codes",
            MfaTamamla = "Finish setup",
            MfaKuruldu = "Two-step verification enabled",
            MfaKapat = "Turn off two-step verification",
            MfaKapatAciklama = "Turning it off requires your password AND a valid verification code (or a recovery code).",
            MfaKapatOnay = "You are about to turn off two-step verification. Your account will be protected by the password alone.",
            MfaKapatildi = "Two-step verification turned off",
            MfaSifre = "Your password",
            MfaKalanKod = "Recovery codes left: {0}",
            MfaKurtarmaYenile = "Regenerate recovery codes",
            MfaKilitli = "Too many failed attempts; the account is temporarily locked. Try again once the lock expires.",
            MfaKodEksik = "verification code",
            MfaSifreEksik = "password",
            MfaGirisBaslik = "Second step",
            MfaGirisAciklama = "Enter the 6-digit code from your authenticator app, or a recovery code.",
            MfaGirisDogrula = "Verify and sign in",
            MfaGirisVazgec = "Cancel",
            MfaGizUyari = "Never share the key with anyone; it is as valuable as your password.",
                    KurKaynagiSec = "Select rate source",
            MaliyetYontemiSec = "Select costing method",
            SaltOkunurKilit = "The business is read-only; this field cannot be changed.",
            MaliyetFifo = "FIFO — first in, first out",
            MaliyetLifo = "LIFO — last in, first out",
            MaliyetOrtalama = "Weighted average",
            AyarBelirlenmedi = "not set yet — the system default is in use",
            MaliyetYontemiNotu = "Cost of goods sold is drawn down in this order. A change applies only to later sales; closed batches are not recalculated.",
            KurKaynagiNotu = "The fine gold price is read from this source. Changing it refetches the rate immediately.",
        },
        Kunye = new()
        {
            Baslik = "System build info",
            Aciklama = "When you contact support, pass this build info together with the support code shown on the error banner; the running version and schema are then established from the product's own statement rather than guesswork.",
            Surum = "Version",
            Derleme = "Build time (UTC)",
            Ortam = "Environment",
            OrtamUretim = "Production",
            OrtamGelistirme = "Development",
            MigrationSayisi = "Applied schema steps",
            SonMigration = "Last schema step",
            BekleyenMigration = "Pending schema steps",
            SemaGuncel = "Schema up to date",
            SemaEski = "Schema is not up to date — pending schema steps exist",
            Kopyala = "Copy",
            Kopyalandi = "Copied",
            KopyalanamadiElle = "Could not copy — select the code and copy it manually",
            DestekKodu = "Support code",
            Ayrinti = "Schema step detail",
            AyrintiSahip = "Detail is shown to the business owner only.",
            DerlemeKaynagiDamga = "release stamp",
            DerlemeKaynagiDosya = "file time (development build)",
            Yuklenemedi = "System build info could not be read",
        },
        Yedek = new()
        {
            Baslik = "Backups",
            Aciklama = "What matters is not that a backup was taken but that it can be RESTORED: every backup is encrypted, copied off-site and, in the monthly drill, restored into a separate database where table checksums are compared.",
            SistemGeneliUyari = "This is a SYSTEM-wide backup covering every business (tenant); it is not a single business's backup. There is deliberately no download path.",
            AnahtarUyari = "If the backup encryption key is lost, no backup can be opened. Keep a safe copy of the key outside the server.",
            SonYedek = "Last backup",
            Boyut = "Size",
            Dogrulama = "Verification",
            DisKopya = "Off-site copy",
            RpoRto = "RPO / RTO",
            RpoOlculen = "Since last backup (measured)",
            RpoTaahhut = "RPO target",
            RtoTaahhut = "RTO target",
            RtoOlculen = "Restore time measured in last drill",
            Olculmedi = "not measured",
            DurumBekliyor = "Pending",
            DurumDogrulandi = "Verified",
            DurumUyusmadi = "Mismatch",
            KopyaYok = "None",
            KopyaVar = "Copied",
            KopyaHata = "Failed",
            DogrulanmamisUyari = "The latest backup is not verified yet, or verification failed. An unverified backup is not a backup.",
            Kosular = "Backup runs",
            KosuBos = "No backup runs yet.",
            Tatbikatlar = "Restore drills",
            TatbikatBos = "No drill records yet.",
            TatbikatAciklama = "The drill restores the backup into a SEPARATE database and compares table checksums; it never writes to the source database.",
            Uyusan = "Matching",
            Uyusmayan = "Mismatching",
            SureSn = "Duration (s)",
            SemaDusuruldu = "Drill schema dropped",
            Tur = "Type",
            TabloSayisi = "Tables",
            ElleYedek = "Take a manual backup",
            ElleYedekAciklama = "This button runs NO command on the server; it writes a request to the queue and the timer takes the backup within 60 seconds.",
            Gerekce = "Reason",
            GerekceOrnek = "e.g. manual backup before the monthly audit",
            TalepGonder = "Create backup request",
            Talepler = "Backup requests",
            TalepBos = "No pending or past backup requests.",
            TalepAcildi = "Backup request queued",
            IptalEt = "Cancel request",
            IptalGerekce = "Cancellation reason",
            IptalOnayBaslik = "Cancel backup request",
            IptalOnayMetin = "The request is not deleted; it is marked CANCELLED and the trail of who cancelled it remains.",
            IptalEdildi = "Request cancelled",
            IndirmeYok = "Backup files cannot be downloaded from the application: a file carries every business's data. Restores happen on the server only, following the runbook.",
            SaklamaNotu = "Backups are rotated for {0} days. This is not a record retention period: ledger and document data stays in production for its full legal term.",
            Sifreli = "Encrypted",
            SifreliDegil = "UNENCRYPTED",
                    IptalGerekceEtiket = "Cancellation reason",
        },
        Aylar = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" },
        AylarKisa = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
    };

    public static Sozluk Getir(string dil) => dil == "en" ? En : Tr;
}
