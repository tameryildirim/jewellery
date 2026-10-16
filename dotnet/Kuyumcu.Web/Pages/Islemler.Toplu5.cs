using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-5 — İŞLEMLER YALNIZ GÖRÜNÜM (hasımsız paket)
//
// (1) Hakem ORTA "Değişim seçiliyken sayfa başlığı 'Satış'": YAPILMADI — üst bar başlığı rotadan gelir
//     (UstBar.razor, Layout — bu paketin dosya alanı DIŞI). Üst barın sayfa yuvası (UstBarSekmeleri)
//     denendi; yuva doluyken kabuk kur şeridini ve Basit/Profesyonel anahtarını GİZLİYOR (Ayarlar tuvali
//     kuralı, tuval-kabuk.css (2)) → geri alındı. Devir: UstBar başlığı /islemler'de seçili türü okumalı.
// (2) Hakem DÜŞÜK "kilit 'TCKN' ↔ alan 'Belge No'": Kaydet kilidinin eksik alan adı, kimlik
//     kutusundaki ALANIN KENDİ ETİKETİDİR (MasakFormu.BelgeNoEtiketi ile aynı kaynak).
// ⚠ Kaydet gövdesi, MASAK kapısı, eksik alan KOŞULLARI DEĞİŞMEZ — yalnız ad/metin.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private Toplu5Metinleri T5 => Toplu5Sozlugu.Al(Dil.Dil);

    /// <summary>Kimlik kutusundaki belge no alanının ekrandaki etiketi (MasakFormu ile aynı kural).</summary>
    // YASAL-PARAM-EKRAN tur 2: PRO da aynı (TCKN kısaltmasız) etiketi kullanır — tek kaynak MasakFormu.
    private string MasakBelgeNoAlanAdi() =>
        Kuyumcu.Web.Bilesenler.MasakFormu.BelgeNoEtiketiFor(masak.BelgeTuru, ZoIslemSozlugu.Al(Dil.Dil));
}
