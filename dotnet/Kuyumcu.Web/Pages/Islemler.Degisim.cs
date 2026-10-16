using Kuyumcu.Web.Bilesenler.Fis;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-GUNSONU — İŞLEMLER: DEĞİŞİM (TAKAS) FİŞİ
//
// (1) Hakem YÜKSEK "BASİT Değişim tuvaldeki Verdim/Aldım fiş kalıbında değil": BASİT'te takas
//     alanları FisTakasTuval'de iki sütun (VERDİM = verilen ürün, ALDIM = alınan eski altın) +
//     FARK + farkın ödemesi. Alanlar FisTakas'ın KENDİSİDİR (Bolum ile parça parça çizilir):
//     id'ler, bağlamalar, hesap ve Kaydet gövdesi DEĞİŞMEZ. PRO aynen (tek parça).
// (2) Vazgeç: kaydedilmemiş değişim fişini boşaltır (istek ATMAZ) — satış fişindeki kalıp.
// (3) Nakit sınırı (ek madde, yasal tutarlılık): farkın ödeme varsayılanı Satış/Alış'la AYNI
//     kaynaktan — sunucunun bildirdiği had (tevsikHaddi, /tevsik/gunluk; 459 s. VUK GT tevsik
//     haddi, TevsikKapisi). Sayı istemciye GÖMÜLMEZ; had bilinmiyorsa davranış eskisi gibi
//     (Nakit, son söz sunucuda). Kural: |fark| > had → müşteri öderse Kart, biz ödersek Havale
//     (TakasOdemeVarsayilani — NakitSiniriAsildi'nin TAKAS dalı; saf, sınır testli). Sunucu kapısı (SatisModulu.Takas → tevsik.Dogrula
//     |fark|) DEĞİŞMEDİ.
// ⚠ Islemler.razor ≤ 1.400 satır (S21) — parçalar burada RenderTreeBuilder ile kurulur.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>BASİT Değişim fişi Verdim/Aldım tuvalinde mi çizilir.</summary>
    internal bool TakasTuvali => Modu.BasitMi && tur == "TAKAS";

    /// <summary>Değişim fişinde vazgeçilecek bir şey var mı (yoksa Vazgeç çizilmez — boşa tıklanan düğme doğmaz).</summary>
    internal bool TakasDolu => !string.IsNullOrEmpty(takas.YeniUrunId) || takas.EskiGram is > 0m || !string.IsNullOrEmpty(aciklama);

    /// <summary>Vazgeç (değişim): girdileri boşaltır, istek ATMAZ. Satıcı kimliği bir sonraki değişime taşınmaz (TakasGirisi.Sifirla).</summary>
    private async Task TakasVazgec()
    {
        takas.Sifirla(hasFiyati); aciklama = ""; hata = null; masakSunucudanGerekli = false;
        cariId = ""; PesinOnSec(); giris.Sifirla(); form.Temizle();
        await TevsikOnizle();
    }

    private RenderFragment TakasYeniParcasi => TakasParcasi("YENI");
    private RenderFragment TakasEskiParcasi => TakasParcasi("ESKI");
    private RenderFragment TakasTamParcasi => TakasParcasi(null);

    /// <summary>FisTakas — PRO'da tek parça (bugünkü), BASİT tuvalde bölüm bölüm; parametreler TEK yerde.</summary>
    private RenderFragment TakasParcasi(string? bolum) => b =>
    {
        b.OpenComponent<FisTakas>(0);
        b.AddAttribute(1, nameof(FisTakas.Takas), takas);
        b.AddAttribute(2, nameof(FisTakas.Ayarlar), AyarSecenekleri);
        b.AddAttribute(3, nameof(FisTakas.UrunSecenekleri), UrunSecenekleri);
        b.AddAttribute(4, nameof(FisTakas.YeniUrunSecildi), EventCallback.Factory.Create(this, TakasYeniUrunSecildi));
        b.AddAttribute(5, nameof(FisTakas.Degisti), EventCallback.Factory.Create(this, Cizim));
        b.AddAttribute(6, nameof(FisTakas.Arama), KalemGirisParcasi);   // t3: tek arama kutusu
        b.AddAttribute(7, nameof(FisTakas.YeniUrunAdi), urunler.FirstOrDefault(u => u.Id == takas.YeniUrunId)?.Ad);
        b.AddAttribute(8, nameof(FisTakas.Bolum), bolum);
        b.AddAttribute(9, nameof(FisTakas.Sade), TakasTuvali);
        b.CloseComponent();
    };

    /// <summary>
    /// DEGISIM-GUNSONU tur 2 (doğrulayıcı DÜŞÜK "kuruş altı pencere"): değişimde tevsik (nakit sınırı) kapısına giden
    /// tutar — sunucunun kapıya verdiğinin AYNISI: SatisModulu.Takas farkTl = Yuvarla2(yeni − eski) (AwayFromZero),
    /// kapıya |farkTl|. İstemci yuvarlamadan karşılaştırınca hadden kuruş altı fazla bir farkta (had + 0,004) sunucu nakdi KABUL ederken
    /// ekran Kart öneriyordu. TevsikTutar (Islemler.Odeme.cs) ve NakitSiniriAsildi bunu okur — ÜRETİM yolu.
    /// </summary>
    public static decimal TakasTevsikTutari(decimal? fark) =>
        fark is decimal f ? Math.Abs(Math.Round(f, 2, MidpointRounding.AwayFromZero)) : 0m;

    /// <summary>
    /// Değişim farkının nakit dışı ödeme varsayılanı — ÜRETİM kuralı (NakitSiniriAsildi TAKAS dalı bunu çağırır; tur 2,
    /// doğrulayıcı DÜŞÜK "yardımcı üretimde çağrılmıyor"). Had yoksa/0 ise null (istemci eşik uydurmaz). Kapıya giden tutar
    /// sunucuyla aynı (TakasTevsikTutari, kuruşa yuvarlanmış |fark|); |fark| had'i AŞARSA (sunucu "olcut &gt; Had", tam hadde
    /// nakit serbest): fark &gt; 0 (müşteri öder) → KART, fark &lt; 0 (müşteriye ödenir) → HAVALE. Satış/Alış'taki
    /// TevsikOneri.Gerekli ile AYNI karşılaştırma.
    /// </summary>
    public static string? TakasOdemeVarsayilani(decimal? fark, decimal? had)
    {
        if (fark is not decimal f || had is not decimal h || h <= 0m) return null;
        if (!Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("NAKIT", "SATIS", TakasTevsikTutari(f), h)) return null;
        return TakasNakitDisiYon(f);
    }

    /// <summary>
    /// DEGISIM-GUNSONU tur 2 (tasarım hakemi KRİTİK "satıcı adı 'Peşin Müşteri' ön dolu, yasal kayda gidebilir"):
    /// satıcı ad soyadının ÖN DOLUM kaynağı. Peşin Müşteri bir kişi değil, sistem carisidir → ad önerilmez, kutu boş
    /// ve zorunlu gelir. Gerçek müşteri seçiliyse onun adı (UX-S24). Kural (VUK md.234 alanları zorunlu) DEĞİŞMEZ.
    /// </summary>
    internal string? SaticiOnDolumAdi => PesinSecili ? null : SeciliCariAdi;

    /// <summary>
    /// Müşteri sonradan değişirse cariden gelmiş (elle yazılmamış) satıcı adı yeni kaynağa uyar: Peşin'e dönülünce
    /// boşalır, başka müşteriye geçilince onun adı olur. Elle yazılan ad (SaticiAdCariden=false) EZİLMEZ.
    /// OnAfterRender kancası (Satis4Uygula) — değiştiyse true.
    /// </summary>
    internal bool TakasSaticiOnDolumEsitle()
    {
        var s = takas.Satici;
        if (tur != "TAKAS" || !s.SaticiAdCariden || s.SaticiAd == SaticiOnDolumAdi) return false;
        s.SaticiAd = SaticiOnDolumAdi;
        s.SaticiAdCariden = SaticiOnDolumAdi is not null;
        return true;
    }

    /// <summary>Farkın nakit dışı yönü: müşteri öderse (fark &gt; 0) Kart, biz ödersek (fark &lt; 0) Havale — NakitDisiVarsayilan da bunu okur.</summary>
    public static string TakasNakitDisiYon(decimal? fark) => fark is < 0m ? "HAVALE" : "KART";

    /// <summary>
    /// Değişime geçilince sunucunun nakit haddi (Satış/Alış'la AYNI uç: /tevsik/gunluk) henüz bilinmiyorsa alınır —
    /// sayfa doğrudan Değişim'le açıldığında (Satış'ta had hiç sorulmadan) otomatik Kart/Havale çalışmıyordu.
    /// Uç düşerse had bilinmez → ön seçim yok, kapı sunucuda (fail-closed) — hata "yok"a çevrilmez, kayıtta sunucu söyler.
    /// </summary>
    private async Task TakasHaddiYukle()
    {
        if (tevsikHaddi is not null) return;
        try { var y = await TevsikGetir(); if (y.Had > 0m) tevsikHaddi = y.Had; }
        catch (Kuyumcu.Web.Servisler.ApiHata) { /* had bilinmez → ön seçim yok; kapı sunucuda */ }
    }
}
