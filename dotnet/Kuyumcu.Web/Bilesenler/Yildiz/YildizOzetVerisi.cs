using Microsoft.AspNetCore.Components;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — "özet" kalıbı (tuval Y_Kasalar · Y_Kur · Y_Hurdaveramat · Y_Kasaraporu):
// solda üst satır + 40 px başlık (300) + 104 px ince rakam (eki 52 px soluk 200) + 20 px cümle,
// altta tek beyaz hap + altı çizili ikincil; sağda alta yaslı liste (.sat) ve soluk bilgi satırı
// (14 px · 0,06em · değer <b> 500 metin rengi). Bileşen İSTEK ATMAZ — her eylem sayfanın MEVCUT
// geri çağrısıdır (davranış değişmez). Satır/hücre/sütun tipleri YILDIZ-5A liste kalıbından.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>PAKET YILDIZ-5B — özet ekranının bağlamı: sayfa doldurur, <see cref="YildizOzet"/> çizer.</summary>
public sealed class YildizOzetBaglami
{
    /// <summary>Kök kimlik (ör. "yildiz-kasalar"); alt öğeler "{Kok}-ust", "{Kok}-baslik", "{Kok}-rakam" …</summary>
    public string Kok { get; init; } = "yildiz-ozet";
    /// <summary>Ekran sınıfı soneki (ızgara sütunları tema-yildiz.css'te: .yz-ozet--{Tur}).</summary>
    public string Tur { get; init; } = "";
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    /// <summary>Büyük rakamın ana parçası (tuval "318.916"); null = rakam çizilmez (veri yok).</summary>
    public string? Rakam { get; init; }
    /// <summary>Rakamın soluk yarı boy eki (tuval ",43 ₺" · " ₺ has" · " g has").</summary>
    public string RakamEki { get; init; } = "";
    public string Cumle { get; init; } = "";

    public string? B1Etiket { get; init; }
    public string B1Id { get; init; } = "";
    public EventCallback B1Tikla { get; init; }
    /// <summary>Doluysa b1 kilitli; sebep title + altında soluk satır (UX-S30).</summary>
    public string? B1Kilit { get; init; }
    public string? B2Etiket { get; init; }
    public string B2Id { get; init; } = "";
    public EventCallback B2Tikla { get; init; }
    public string? B2Kilit { get; init; }
    /// <summary>Doluysa ikincil eylem bir gezinmedir (başka ekranın mevcut yolu).</summary>
    public string? B2Href { get; init; }

    public IReadOnlyList<YildizSutun> Sutunlar { get; init; } = [];
    /// <summary>Son sütun satır eylemidir (başlıkta boş hücre — tuval &lt;span&gt;&lt;/span&gt;).</summary>
    public bool EylemSutunu { get; init; }
    public IReadOnlyList<YildizListeSatiri> Satirlar { get; init; } = [];
    /// <summary>Alt bilgi satırı (tuval "SON KAPANIŞ <b>dün yok</b>"). Etiket büyük harfle yazılır.</summary>
    public IReadOnlyList<(string Etiket, string Deger)> Bilgi { get; init; } = [];
    /// <summary>Kurucu 24.10: alt bilgi satırının BAŞINA çizilen etkileşimli parça (ör. /kur'da KAYNAK seçicisi).</summary>
    public RenderFragment? BilgiOnek { get; init; }

    public bool Yukleniyor { get; init; }
    /// <summary>İstek BAŞARILI ve sonuç boşken sağda yazılacak tek cümle (UX-S28).</summary>
    public string? BosMetni { get; init; }
    public RenderFragment? Bantlar { get; init; }

    /// <summary>"+ Tüm ayrıntılar" (tuval .ekle kalıbı): ekranın özgün tüm özellikleri arkasında.</summary>
    public EventCallback AyrintiAc { get; init; }
    public bool AyrintiAcik { get; init; }
    public EventCallback AyrintiKapat { get; init; }
    /// <summary>Ayrıntı görünümündeki dönüş bağlantısının metni.</summary>
    public string DonEtiketi { get; init; } = "";
}

/// <summary>PAKET YILDIZ-5B — boş durum kalıbı (tuval Y_POScihazlari · Y_Banka): ince çizgi + 40 px cümle + açıklama + tek hap.</summary>
public sealed class YildizBosBaglami
{
    public string Kok { get; init; } = "yildiz-bos";
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    public string BosBaslik { get; init; } = "";
    public string BosMetin { get; init; } = "";
    public string? B1Etiket { get; init; }
    public string B1Id { get; init; } = "";
    public EventCallback B1Tikla { get; init; }
    public string? B1Kilit { get; init; }
    public RenderFragment? Bantlar { get; init; }
    public EventCallback AyrintiAc { get; init; }
}

/// <summary>PAKET YILDIZ-5B — para ekranlarının ortak yazımı (saf).</summary>
public static class YildizParaBicim
{
    /// <summary>
    /// Tuval büyük rakamı: "318.916" + ",43 ₺" (kuruş ve ₺ soluk, yarı boy). Para birimi tutarla taşınır
    /// (UX-S32): TRY dışında sembol yerine kod yazılır ("1.250" + ",00 USD"). Eksi U+2212; isteğe bağlı "+".
    /// </summary>
    public static (string Ana, string Ek) BuyukPara(decimal tutar, string paraBirimi, bool artiIsareti = false)
    {
        var tam = Kurus(Math.Abs(tutar), paraBirimi);
        // Bicim.Para "318.916,43 ₺" ya da "1.250,00 USD" verir — ilk virgülde böl.
        var i = tam.IndexOf(',');
        var ana = i < 0 ? tam : tam[..i];
        var ek = i < 0 ? "" : tam[i..];
        if (i < 0)
        {
            var bosluk = tam.LastIndexOf(' ');
            if (bosluk > 0) { ana = tam[..bosluk]; ek = tam[bosluk..]; }
        }
        var isaret = tutar < 0m ? "−" : artiIsareti && tutar > 0m ? "+" : "";
        return (isaret + ana, ek);
    }

    /// <summary>Satır tutarı "186.030,86 ₺"; eksi U+2212 ile ("−30.028,39 ₺").</summary>
    public static string Tutar(decimal tutar, string paraBirimi, bool artiIsareti = false)
    {
        var s = Kurus(Math.Abs(tutar), paraBirimi);
        return tutar < 0m ? "−" + s : artiIsareti && tutar > 0m ? "+" + s : s;
    }

    /// <summary>Tuval tutarı kuruşu HER ZAMAN iki hane yazar ("25.024,00 ₺"); döviz kendi simgesiyle (Bicim.Para).</summary>
    public static string Kurus(decimal tutar, string? paraBirimi) =>
        paraBirimi is null or "" or "TRY" ? Bicim.TlKurus(tutar) : Bicim.Para(tutar, paraBirimi);
}

/// <summary>PAKET YILDIZ-5B — sayfanın MEVCUT hata/başarı bantları (aynı kimliklerle; özgün ekran Yıldız'da çizilmediği için tek kez).</summary>
public static class YildizBant
{
    public static RenderFragment Ciz(string? hata, string? basari, int jeton, int sureliMs = 0,
                                     string? hataId = "hata-bandi", string? basariId = "basari-bandi") => b =>
    {
        b.OpenComponent<HataBandi>(0);
        b.AddAttribute(1, nameof(HataBandi.Mesaj), hata);
        b.AddAttribute(2, nameof(HataBandi.Id), hataId);
        b.CloseComponent();
        b.OpenComponent<BasariBandi>(3);
        b.AddAttribute(4, nameof(BasariBandi.Mesaj), basari);
        b.AddAttribute(5, nameof(BasariBandi.Jeton), jeton);
        if (sureliMs > 0) b.AddAttribute(6, nameof(BasariBandi.SureliMs), sureliMs);
        b.AddAttribute(7, nameof(BasariBandi.Id), basariId);
        b.CloseComponent();
    };
}

/// <summary>PAKET YILDIZ-5B — takvim kalıbı (tuval Y_Vadetakvimi): solda ay ızgarası, sağda "Bu ay" listesi.</summary>
public sealed class YildizTakvimBaglami
{
    public string Kok { get; init; } = "yildiz-takvim";
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    /// <summary>Ay başlığı (tuval "Ekim 2026").</summary>
    public string AyAdi { get; init; } = "";
    /// <summary>Gün adları tek satır (tuval "PZT SAL ÇAR PER CUM CMT PAZ").</summary>
    public string GunAdlari { get; init; } = "";
    /// <summary>Ayın ilk gününden önceki boş hücre sayısı (pazartesi başlangıç).</summary>
    public int Bosluk { get; init; }
    public int GunSayisi { get; init; }
    /// <summary>Vadesi olan günler → o günün evrak listesine giden bağlantı (UX-S23).</summary>
    public IReadOnlyDictionary<int, string> VadeliGunler { get; init; } = new Dictionary<int, string>();
    public string ListeBaslik { get; init; } = "";
    public IReadOnlyList<YildizSutun> Sutunlar { get; init; } = [];
    public IReadOnlyList<YildizListeSatiri> Satirlar { get; init; } = [];
    public bool Yukleniyor { get; init; }
    public string? BosMetni { get; init; }
    public string? SinirNotu { get; init; }
    public RenderFragment? Bantlar { get; init; }
    public EventCallback AyrintiAc { get; init; }
    public bool AyrintiAcik { get; init; }
    public EventCallback AyrintiKapat { get; init; }
    public string DonEtiketi { get; init; } = "";
}

/// <summary>PAKET YILDIZ-5B — form kalıbı (tuval Y_Gidervevirman): solda .k satırları + "Ayrıntı ekle" + Kaydet/Vazgeç, sağda not.</summary>
public sealed class YildizFormBaglami
{
    public string Kok { get; init; } = "yildiz-form";
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    /// <summary>İlk satır "Ne yapıyorsun": seçenekler (kod, ad, seçili, tıkla).</summary>
    public string? SecimEtiketi { get; init; }
    public IReadOnlyList<(string Kod, string Ad, bool Secili, EventCallback Tikla)> Secimler { get; init; } = [];
    public string? EkleEtiketi { get; init; }
    public EventCallback EkleTikla { get; init; }
    public string B1Etiket { get; init; } = "";
    public string B1Id { get; init; } = "";
    public EventCallback B1Tikla { get; init; }
    public bool B1Kilitli { get; init; }
    public string? B1Kilit { get; init; }
    public string? B2Etiket { get; init; }
    public string B2Id { get; init; } = "";
    public EventCallback B2Tikla { get; init; }
    public string NotBaslik { get; init; } = "";
    public string Not { get; init; } = "";
    public RenderFragment? Bantlar { get; init; }
    public bool AyrintiAcik { get; init; }
    public EventCallback AyrintiKapat { get; init; }
    public string DonEtiketi { get; init; } = "";
}
