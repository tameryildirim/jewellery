using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4B — Tezgâh "Yıldız" yüzü (tuval Tezgah4 koyu / Tezgah4a açık · BİREBİR).
//
// YALNIZ GÖRÜNÜM (hasımsız paket): Yıldız şemasında SATIŞ fişi (BASİT ve PRO) tuvalin
// düzeninde çizilir; değerler ve eylemler sayfanın MEVCUT durumu ve yöntemleridir.
// Gövde, POST, karma ödeme, MASAK, nakit sınırı, Peşin Müşteri kapısı, kilit sebepleri
// AYNEN Islemler.razor'dadır — bu dosya hiçbir kurala dokunmaz, hesap yapmaz (Fark dahil
// FisFark'tan okunur). Öteki 8 şemada YildizTezgahMi = false → sayfa AYNEN.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private GorunumServisi? yildizKiraciGorunumu;
    private KullaniciGorunumServisi? yildizBenimGorunumu;
    private bool yildizMusteriAcik;
    private bool yildizTurlerAcik;

    /// <summary>Etkin şema Yıldız mı (kabukla AYNI karar noktası)?</summary>

    /// <summary>Yıldız tuval yüzü bu çizimde mi? Yalnız SATIŞ fişi (tuvalde yalnız satış çizildi); diğer türler özgün düzende.</summary>
    internal bool YildizTezgahMi => tur == "SATIS" && !DigerAcik && !yukleniyor;

    /// <summary>Sayfa kabının ek sınıfları (CSS: tuvalde olmayan özgün parçalar gizli; müşteri/tür açılınca görünür).</summary>
    private string YildizSinif => !YildizTezgahMi ? YildizTurSinif   // P-TEZGAH-TUR-KIMLIK: öteki türler (Islemler.TurKimlik.cs)
        : " yz-islem" + (YildizMusteriGorunur ? " yz-musteri-acik" : "") + (yildizTurlerAcik ? " yz-turler-acik" : "");

    /// <summary>
    /// Müşteri katmanı görünür mü? YALNIZ "Değiştir" açtıysa (YILDIZ-4C tur 3, tasarım hakemi KRİTİK: Peşin Müşteri haddi aşılınca
    /// katman kendiliğinden açılıp okutulan kalemleri ve toplamı örtüyordu). Kapı AYNI: eşik üstünde Kaydet kilitli ve sebebi
    /// ("önce müşteri seçin") Fark satırında; sunucu reddi hata satırında; Peşin yükleme hatası not satırında (UX-S28).
    /// </summary>
    private bool YildizMusteriGorunur => yildizMusteriAcik;

    private void YildizAboneOl()
    {
        yildizKiraciGorunumu = Hizmetler.GetService(typeof(GorunumServisi)) as GorunumServisi;
        yildizBenimGorunumu = Hizmetler.GetService(typeof(KullaniciGorunumServisi)) as KullaniciGorunumServisi;
        if (yildizKiraciGorunumu is not null) yildizKiraciGorunumu.Degisti += Yenile;
        if (yildizBenimGorunumu is not null) yildizBenimGorunumu.Degisti += Yenile;
    }

    private void YildizAboneliktenCik()
    {
        if (yildizKiraciGorunumu is not null) yildizKiraciGorunumu.Degisti -= Yenile;
        if (yildizBenimGorunumu is not null) yildizBenimGorunumu.Degisti -= Yenile;
    }

    /// <summary>Bacak aracının ekran adı (FisProOdemeHapi ile AYNI sözlük; ham kod basılmaz — UX-S31).</summary>
    private string YildizAracAdi(string? arac) => arac switch
    {
        "KART" => Dil.T.Islem.OdemeKart,
        "HAVALE" => Dil.T.Islem.OdemeHavale,
        "VERESIYE" => Dil.T.Islem.OdemeVeresiye,
        "DOVIZ" => Dil.T.Islem.OdemeDoviz,
        "ALTIN" => EskiAltinSozlugu.Al(Dil.Dil).EskiAltin,
        null or "" or "NAKIT" => Dil.T.Islem.OdemeNakit,
        _ => Dil.T.Islem.OdemeTuru,
    };

    private string? YildizKasaAdi(string? id) => string.IsNullOrEmpty(id) ? null : kasalar.FirstOrDefault(k => k.Id == id)?.Ad;

    private IReadOnlyList<YildizTezgahOdeme> YildizOdemeler()
    {
        if (!karmaOdeme)
            return [new(0, YildizAracAdi(odemeTuru), odemeTuru == "VERESIYE" || HavaleModu ? null : YildizKasaAdi(kasaId), Bicim.TlKurus(ToplamTl))];   // 17.10: havale kasaya girmez
        return [.. bacaklar.Select((b, i) => new YildizTezgahOdeme(i, YildizAracAdi(b.Arac),
            b.Arac == "VERESIYE" ? null : YildizKasaAdi(b.KasaId), Bicim.TlKurus(b.TutarTl)))];
    }

    /// <summary>
    /// Fark bloğunun altındaki tek satır (UX-S30): kalem varken Kaydet kilitliyse SEBEBİ; değilse ve sunucunun
    /// nakit haddi BİLİNİYORSA yasal durum (tuval "Yasal kurallar uygun · nakit <sunucunun haddi> sınırının altında" — sayı istemciye gömülmez).
    /// Had bilinmiyorsa "uygun" DENMEZ (uydurulmaz) — satır yazılmaz.
    /// </summary>
    private (string? Metin, bool Kotu) YildizNot()
    {
        if (kalemler.Count > 0 && !Gonderilebilir && KaydetKilitSebebi is { } kilit) return (kilit, YasalEngelMetni is not null || NakitBandiVar || MasakGerekli);
        if (YasalEngelMetni is { } yasal) return (yasal, true);
        // tur 2 (hakem KRİTİK): özgün yasal kapı bandı (#islem-yasal-kapi) Yıldız'da çizilmez — aynı cümle bu satırda (özgünde
        // bant yalnız BASİT'te çizilir; PRO'da da yazılırsa özgünde olmayan bir cümle eklenmiş olur).
        // Sunucu hatası satırı (#yildiz-tezgah-hata) varken GENEL "yasal kapı" cümlesi yazılmaz: özgün BASİT bandı her 400'de açılır
        // (stok hatasında da) — Yıldız'da gerçek sebep zaten hata satırında; özel cümle (BasitBantMesaji) varsa yine yazılır.
        // P-TEZGAH-TUR-KIMLIK (denetim K4): kimlik formu varken genel cümle yazılmaz (BasitBantKapiAcik — Platin bandıyla AYNI kural):
        // form eksikse Kaydet kilidi eksik alanları söyler; tamamsa "alanları doldurun" demek yanlış olur.
        if (Modu.BasitMi && BasitBantKapiAcik && (BasitBantMesaji is not null || hata is null)) return (BasitBantMesaji ?? Dil.T.Arayuz.YasalKapiAcildi, true);
        if (tevsikHaddi is not { } had || had <= 0m || MasakGerekli || NakitBandiVar) return (null, false);
        // TEMA TEMİZLİĞİ (17.10) bulgusu: PRO tek ödemede sunucunun nakit önizlemesi YALNIZ proTevsik'e yazılır
        // (TevsikOnizle → ProYasalTazele; ödeme kapısı bayrakları bilerek boş kalır). Eskiden bunu PRO yan sütunu
        // ("Aşıldı"/"Bilinmiyor") söylüyordu; tek yüz Yıldız'da o sütun yok → not satırı "uygun" DEMEZ, aynı kararı yazar.
        if (Modu.ProfesyonelMi && tur == "SATIS" && !karmaOdeme && NakitOdemede)
        {
            var p = SatisProSozlugu.Al(Dil.Dil);
            var kural = ProNakitHad is { } h && h > 0m ? string.Format(p.NakitKurali, Bicim.Tl(h)) : p.NakitKuraliSayisiz;
            if (ProNakitAsildi == true) return ($"{kural}: {p.Asildi}", true);
            if (proTevsik is null) return ($"{kural}: {p.Bilinmiyor}", true);   // sunucu yanıt vermedi — "uygun" tahmin edilmez (md.1)
        }
        var uygun = YildizTezgahSozlugu.YasalUygun(Dil.Dil);
        return (NakitOdemede ? $"{uygun} · {YildizTezgahSozlugu.NakitSinirAlti(Bicim.Tl(had), Dil.Dil)}" : uygun, false);
    }

    internal YildizTezgahBaglami YildizBaglami
    {
        get
        {
            var cari = cariler.FirstOrDefault(c => c.Id == cariId);
            var fark = karmaOdeme ? FisFark.Hesapla(ToplamTl, bacaklar) : 0m;
            var belirsiz = gecersizAlanlar.OnekleVar("islem-bacak-") || (karmaOdeme && FisFark.DovizKuruEksik(bacaklar));
            var acikHesapKilidi = FisFark.AcikHesapKilidi(fark, cariId, pesinCariId, bacaklar, TezgahFisiSozlugu.Al(Dil.Dil));
            var (not, kotu) = YildizNot();
            return new YildizTezgahBaglami
            {
                TurAdi = SeciliTurAdi,
                MusteriAdi = cari?.Ad,
                MusteriDurumu = YildizTezgahBicim.MusteriDurumu(cari, Dil.Dil),
                MusteriAcik = YildizMusteriGorunur,
                TurlerAcik = yildizTurlerAcik,
                Kalemler = [.. kalemler.Select((k, i) =>
                {
                    var u = urunler.FirstOrDefault(x => x.Id == k.UrunId);
                    return new YildizTezgahKalem(i, FisKalemTablosu.AyarKisa(u), k.UrunAdi,
                        YildizTezgahBicim.KalemAyrinti(k, u, Dil.Dil), Bicim.TlKurus(FisHesap.SatirNet(k)));
                })],
                ToplamAyrinti = YildizTezgahBicim.ToplamAyrinti(FisHesap.ToplamGram(kalemler, urunler), FisHesap.ToplamHasBilinen(kalemler, urunler), Dil.Dil),
                ToplamMetni = Bicim.TlKurus(ToplamTl),
                Odemeler = YildizOdemeler(),
                Karma = karmaOdeme,
                TekeIndirilebilir = karmaOdeme && bacaklar.Count == 1 && Array.IndexOf(BasitTekOdemeTurleri, bacaklar[0].Arac) >= 0,
                OdemeSecenekleri = BasitOdemeSecenekleri,
                OdemeTuru = odemeTuru,
                OdemeTuruSec = EventCallback.Factory.Create<string>(this, BasitOdemeSec),
                FarkMetni = belirsiz ? "—" : Bicim.Tl(fark),
                FarkTamam = !belirsiz && fark == 0m && kalemler.Count > 0 && Gonderilebilir,   // tur 2: Kaydet kilitliyken onay işareti YOK
                // TEZGAH-ODEME (kurucu 10.10): "Açık hesap olarak kaydet" birincil gibi durmaz (.b2) ve YALNIZ fark > 0 iken GERÇEK
                // müşteri seçiliyse görünür (Peşin Müşteri'de görünüp kilitli durmaz). Kapı/kilit/gövde AYNI (AcikHesapKaydet).
                AcikHesapGorunur = !belirsiz && FisFark.Durum(fark) == FarkDurumu.Eksik && GercekMusteri is not null,
                AcikHesapKilidi = acikHesapKilidi,
                AcikHesapEtiketi = TezgahFisiSozlugu.Al(Dil.Dil).AcikHesapKaydet,
                Not = not,
                NotKotu = kotu,
                KaydetEtiketi = kaydediliyor ? Dil.T.Ortak.Yukleniyor : KaydetEtiketi,
                KaydetKilitli = kaydediliyor || !Gonderilebilir,
                KaydetKilitSebebi = Gonderilebilir ? null : KaydetKilitSebebi,
                Kaydediliyor = kaydediliyor,
                FisDolu = FisDolu,
                KalemVar = kalemler.Count > 0,
                ParaUstuKullanilir = kalemler.Count > 0 && YildizParaUstuNakit > 0m && !NakitBandiVar,   // YILDIZ-4C: karmada NAKİT satırları (özgün karma para üstüyle aynı taban)
                NakitTutar = ToplamTl,
                ParaUstuNakit = YildizParaUstuNakit,
                Giris = YildizGiris,
                KalemDuzen = YildizKalemDuzen,
                OdemeDuzen = OdemeParcasi,
                Kaydet = EventCallback.Factory.Create(this, Kaydet),
                Vazgec = EventCallback.Factory.Create(this, Vazgec),
                BaskaOdeme = EventCallback.Factory.Create(this, OdemeyiBol),
                TekOdeme = EventCallback.Factory.Create(this, TekOdemeyeDonAracla),
                Indirim = EventCallback.Factory.Create(this, YildizIndirim),
                AcikHesap = EventCallback.Factory.Create(this, AcikHesapKaydet),
                MusteriDegistir = EventCallback.Factory.Create(this, () => yildizMusteriAcik = !yildizMusteriAcik),
                TurDegistir = EventCallback.Factory.Create(this, () => yildizTurlerAcik = !yildizTurlerAcik),
                Musteri = MusteriSatiriParcasi,
                Kimlik = MasakParcasi,
                KimlikGorunur = (MasakGerekli && !PesinEsikUstu && !NakitOnce) || (masakHata && MasakSorulur),   // YASAL-KAPI-3: nakit kuralı tetikliyken kimlik çekmecesi yok (önce ödeme türü)
                // P-TEZGAH-TUR-KIMLIK (denetim Y4 · K4): görünür tür sekmeleri; çekmecenin "Tamam"ı (kapı AYNI: MasakTamam).
                TurSecici = YildizTurSeciciParcasi,
                KimlikEksiksiz = MasakTamam,
                KimlikEksikGoster = EventCallback.Factory.Create(this, KimlikEksikGosterIste),
                Hata = hata,
                HataAyrinti = SunucuAyrinti,
                SonFis = SonFisNoGorunen is { } no
                    ? string.Format(SatisEkraniSozlugu.Al(Dil.Dil).SonFis, no) + (string.IsNullOrEmpty(sonIskontoNotu) ? "" : $" · {sonIskontoNotu}")
                      + (string.IsNullOrEmpty(sonFaturaNotu) ? "" : $" · {sonFaturaNotu}")   // M6: fatura no / taslak / oluşmadı
                    : null,
                // M6 — Fiş | Fatura seçimi (yalnız satışta; haddi aşınca Fatura kilitli seçili).
                BelgeGorunur = BelgeSecimiGorunur,
                BelgeNotu = BelgeNotu,
                BelgeSoruAcik = belgeSorusuAcik && BelgeSorulur,
                BelgeSinirMetni = belgeSinirUyarisi && kuyumcuFaturaHaddi is { } fh ? SatisBelgeSozlugu.SinirUyari(Bicim.Tl(fh), Dil.Dil) : null,
                BelgeOneriFatura = belgeVarsayilan == "FATURA",
                BelgeCevap = EventCallback.Factory.Create<string>(this, BelgeCevapla),
                Havaleler = HavaleOgeleri, SeciliHavaleId = HavaleModu ? havale?.Id : null, HavalelerAcik = havalelerAcik,
                HavalelerAcKapa = EventCallback.Factory.Create(this, () => havalelerAcik = !havalelerAcik),
                HavaleSec = EventCallback.Factory.Create<string>(this, HavaleSec), HavaleBirak = EventCallback.Factory.Create(this, HavaleBirak),
                YazdirAdresleri = [.. yazdirIdler.Select(FisYazdir.YazdirAdresi).OfType<string>()],
                UrunBelgesiAdresi = tur == "SATIS" && yazdirIdler.Count > 0
                    ? $"/cikti/{Kuyumcu.Contracts.Cikti.BelgeTurleri.GarantiKarti}/{Uri.EscapeDataString(yazdirIdler[0])}" : null,   // D1-13
                YazdirHata = yazdirHata is null ? null : string.Format(FisYazdirSozlugu.Al(Dil.Dil).YazdirHata, yazdirHata),
                Uyarilar = [.. new[] { yerlesimUyarisi, fisTaksitUyari }.OfType<string>(), .. new[] { fisTaksitHata, reyonHata, YildizMusteriGorunur ? null : pesinHata }.OfType<string>()],   // tur 3: katman kapalıyken Peşin yükleme hatası da görünür   // YILDIZ-4C: reyon yükleme uyarısı da (özgün bant Yıldız'da ekran dışında kalıyordu)
                // ── YILDIZ-4C — etkileşim hâlleri tuval dilinde ──
                Pro = Modu.ProfesyonelMi,
                KalemDuzenleri = [.. kalemler.Select((k, i) => new YildizKalemDuzenVerisi(i, k.UrunId, Bicim.Girdi(k.Adet, "0.###"),
                    FisKalemTablosu.BirimFiyatMetni(k.BirimFiyat), Bicim.Girdi(k.SatirIskontoYuzde, "0.##"), k.Hediye,
                    urunler.FirstOrDefault(u => u.Id == k.UrunId)?.Birim ?? ""))],
                Form = form,
                KalemYaz = EventCallback.Factory.Create<YildizKalemGirdi>(this, YildizKalemYaz),
                KalemHediye = EventCallback.Factory.Create<YildizKalemHediye>(this, YildizKalemHediyeYaz),
                KalemCikar = EventCallback.Factory.Create<int>(this, YildizKalemCikar),
                KalemEk = YildizKalemEk,
                IndirimGorunur = indirimAcik || fisIskontoYuzde is > 0,
                IndirimAlani = YildizIndirimAlani,
                BacakEkle = EventCallback.Factory.Create(this, YildizBacakEkle),
                BacakTavan = bacaklar.Count >= 5,
                MusteriSecenekleri = [.. cariler.Select(c => new YildizMusteriSecenegi(c.Id, c.Ad, YildizTezgahBicim.MusteriDurumu(c, Dil.Dil), c.SistemCarisi ? null : c.VergiNo))],
                MusteriSeciliId = cariId ?? "",
                MusteriSec = EventCallback.Factory.Create<string>(this, YildizMusteriSec),
                MusteriAc = EventCallback.Factory.Create<(string No, string Ad)>(this, YildizMusteriAc), MusteriAcHata = musteriAcHata,
                KimlikSor = MusteriKimlikSor, KimlikHata = musteriKimlikHata,
                KimlikKaydet = EventCallback.Factory.Create<string>(this, MusteriKimlikKaydet),
                PesinHata = pesinHata,
                PesinKimlikGerekli = pesinKimlikGerekli,
                GercekMusteriSec = EventCallback.Factory.Create(this, GercekMusteriSec),
                ProUstAlanlar = ProSatis ? ProUstAlanlar : null,
                ProUstAcikZorunlu = ProUstOzetAcikZorunlu,
                // YILDIZ-4C: Peşin Müşteri art arda sorusu (YASAL-3) — özgün yerinde (fişin altında) Yıldız'da ekran dışındaydı;
                // AYNI bileşen, AYNI geri çağrılar Fark bloğunda çizilir (özgün yer Yıldız'da çizilmez — id tekrarı yok).
                PesinSoru = pesinSoruAcik ? YildizPesinSoru : null,
                // D1-03 işlem bekletme
                BekletGorunur = BekletGorunur,
                Beklet = EventCallback.Factory.Create(this, Beklet),
                Bekleyenler = YildizBekleyenler,
                BekleyenlerAcik = bekleyenlerAcik,
                BekleyenlerAcKapa = EventCallback.Factory.Create(this, () => bekleyenlerAcik = !bekleyenlerAcik),
                BekleyenAl = EventCallback.Factory.Create<string>(this, BekleyenAl),
                BekleyenSil = EventCallback.Factory.Create<string>(this, BekleyenSil),
                BekleyenNot = bekleyenNot,
                BekleyenNotKotu = bekleyenNotKotu,
            };
        }
    }

    /// <summary>Satıra dokununca açılan düzenleme: kalem tablosu (+ indirim) · reyon · açıklama (özgün "Diğer ayrıntılar" alanları; tuval kalıbı "satıra dokun").</summary>
    private RenderFragment YildizKalemDuzen => b => { b.AddContent(0, KalemParcasi); b.AddContent(1, ReyonParcasi); b.AddContent(2, AciklamaParcasi); };

    /// <summary>
    /// Tezgâhın giriş satırı + fiyat listesi sorusu (EKSIK-KUCUK B2-12, UX-S24). Tema temizliği (17.10) bulgusu: soru
    /// yalnız özgün kalem tablosunun parçasındaydı ve Yıldız tezgâhında hiç çizilmiyordu; aynı bileşen, aynı kimlikler.
    /// </summary>
    private RenderFragment YildizGiris => b =>
    {
        b.AddContent(0, KalemGirisParcasi);
        b.OpenComponent<FisListeDegisti>(1);
        b.AddComponentParameter(2, nameof(FisListeDegisti.Sayi), BekleyenOneri.Count);
        b.AddComponentParameter(3, nameof(FisListeDegisti.Guncelle), EventCallback.Factory.Create(this, ListeFiyatlariniUygula));
        b.AddComponentParameter(4, nameof(FisListeDegisti.EskiKalsin), EventCallback.Factory.Create(this, ListeOnerisiniReddet));
        b.AddComponentParameter(5, nameof(FisListeDegisti.Hata), listeHata);
        b.AddComponentParameter(6, nameof(FisListeDegisti.Basari), listeBasari);
        b.AddComponentParameter(7, nameof(FisListeDegisti.Jeton), listeBasariJeton);
        b.CloseComponent();
    };

    // ═══ PAKET YILDIZ-4C — etkileşim hâlleri (hakem 4B tur 2 KRİTİK: eski tablo/karma formu ikinci kez açılıyordu) ═══

    /// <summary>Kalem düzenlemesinin altındaki fiş alanları: reyon · açıklama (özgün parçalar, id'ler aynı).</summary>
    private RenderFragment YildizKalemEk => b => { b.AddContent(0, ReyonParcasi); b.AddContent(1, AciklamaParcasi); };

    /// <summary>Peşin Müşteri art arda sorusu (Islemler.razor'daki PesinArtArdaSoru ile AYNI parametreler).</summary>
    private RenderFragment YildizPesinSoru => b =>
    {
        b.OpenComponent<PesinArtArdaSoru>(0);
        b.AddAttribute(1, nameof(PesinArtArdaSoru.Goster), pesinSoruAcik);
        b.AddAttribute(2, nameof(PesinArtArdaSoru.Calisiyor), kaydediliyor);
        b.AddAttribute(3, nameof(PesinArtArdaSoru.Evet), EventCallback.Factory.Create(this, PesinSoruEvet));
        b.AddAttribute(4, nameof(PesinArtArdaSoru.Hayir), EventCallback.Factory.Create(this, PesinSoruHayir));
        b.AddAttribute(5, nameof(PesinArtArdaSoru.Yildiz), true);
        b.CloseComponent();
    };

    /// <summary>"+ İndirim" alanı: özgün fiş iskontosu satırı (FisIskontoSatiri — KalemParcasi'ndaki bağla AYNI).</summary>
    private RenderFragment YildizIndirimAlani => b =>
    {
        b.OpenComponent<FisIskontoSatiri>(0);
        b.AddAttribute(1, nameof(FisIskontoSatiri.Yuzde), fisIskontoYuzde);
        b.AddAttribute(2, nameof(FisIskontoSatiri.YuzdeChanged), EventCallback.Factory.Create<decimal?>(this, v => fisIskontoYuzde = v));
        b.AddAttribute(3, nameof(FisIskontoSatiri.Onizleme), IskontoOnizleme);
        b.AddAttribute(4, nameof(FisIskontoSatiri.Net), ToplamTl);
        b.AddAttribute(5, nameof(FisIskontoSatiri.Form), form);
        b.CloseComponent();
    };

    /// <summary>Para üstünün tabanı: tek ödemede NAKİT ise fiş toplamı; karmada NAKİT satırlarının toplamı (FisOdemeBacaklari'nın para üstüyle AYNI).</summary>
    private decimal YildizParaUstuNakit => karmaOdeme
        ? bacaklar.Where(b => b.Arac == "NAKIT").Sum(b => b.TutarTl)
        : odemeTuru == "NAKIT" ? ToplamTl : 0m;

    private string? musteriAcHata;

    /// <summary>
    /// M6b — tezgâhta TC/VKN ile müşteri bul ya da aç (sunucu: bulursa o, yoksa ad + kimlik no ile açar). Yeni kart
    /// listeye eklenir ve seçim AYNI yoldan yapılır (YildizMusteriSec → MusteriDegisti).
    /// </summary>
    private async Task YildizMusteriAc((string No, string Ad) g)
    {
        musteriAcHata = null;
        try
        {
            var y = await Api.PostAsync<System.Text.Json.JsonElement>("/islemler/musteri-ac", new { kimlikNo = g.No, ad = g.Ad });
            var id = y.GetProperty("id").GetString()!;
            var ad = y.GetProperty("ad").GetString() ?? g.Ad;
            if (!cariler.Any(c => c.Id == id)) cariler.Add(new Kuyumcu.Contracts.Sayfalar.Islemler.CariVeri(id, ad, VergiNo: g.No));
            await YildizMusteriSec(id);
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException) { musteriAcHata = ex.Message; }
    }

    /// <summary>Müşteri katmanından seçim: FisMusteriSatiri.Sec ile AYNI sıra (cariId yazılır → MusteriDegisti).</summary>
    private async Task YildizMusteriSec(string id)
    {
        yildizMusteriAcik = false;   // P-TEZGAH-TUR-KIMLIK (denetim Y2): seçim katmanı seçimle kapanır ("Kapat" gerekmez); seçilen müşteri üst satırda
        cariId = id;
        await MusteriDegisti();
    }

    /// <summary>Kalem düzenlemesinden "Çıkar": sayfanın kalem silme yolu (KalemSil — özgün ✕ ile AYNI).</summary>
    private Task YildizKalemCikar(int sira) => sira >= 0 && sira < kalemler.Count ? KalemSil(kalemler[sira]) : Task.CompletedTask;

    /// <summary>Hediye işareti: FisKalemTablosu.HediyeYaz ile AYNI (k.Hediye → HediyeUygula → önizleme).</summary>
    private void YildizKalemHediyeYaz(YildizKalemHediye h)
    {
        if (h.Sira < 0 || h.Sira >= kalemler.Count) return;
        var k = kalemler[h.Sira];
        k.Hediye = h.Deger;
        k.HediyeUygula();
        HediyeDegisti();
    }

    /// <summary>
    /// Satır içi kalem düzeltmesi (PRO): FisKalemTablosu'nun AdetDegistir / FiyatDegistir / IskontoDegistir
    /// kurallarının BİREBİR aynası — aynı ayrıştırıcı (FormAlani.SayiCoz), aynı tavan (FisKalemGirisi.TavanAsildi),
    /// aynı alan anahtarı ve ileti; aynı sonuç geri çağrısı (KalemDegisti). Eşitlik Yildiz4CTestleri'nde sınanır.
    /// </summary>
    private Task YildizKalemYaz(YildizKalemGirdi g)
    {
        if (g.Sira < 0 || g.Sira >= kalemler.Count) return Task.CompletedTask;
        var k = kalemler[g.Sira];
        form.Temizle();
        switch (g.Alan)
        {
            case "adet":
            {
                var alan = $"si-kalem-adet-{k.UrunId}";
                var c = FormAlani.SayiCoz(g.Metin);
                if (c.Deger is not { } d || d <= 0m) { form.Ekle(alan, c.Mesaj(Dil.Dil) ?? Dil.T.Kabuk.SifirdanBuyuk); return KalemDegisti(false); }
                if (FisKalemGirisi.TavanAsildi(d, k.BirimFiyat)) { form.Ekle(alan, PatchDortSozlugu.Al(Dil.Dil).SatirTutariCokBuyuk); return KalemDegisti(false); }
                k.Adet = d;
                return KalemDegisti(true);
            }
            case "fiyat":
            {
                var alan = $"si-kalem-fiyat-{k.UrunId}";
                var c = FormAlani.SayiCoz(g.Metin);
                if (c.Deger is not { } d || d < 0m) { form.Ekle(alan, c.Mesaj(Dil.Dil) ?? Dil.T.Kabuk.NegatifOlamaz); return KalemDegisti(false); }
                if (FisKalemGirisi.TavanAsildi(k.Adet, d)) { form.Ekle(alan, FisKalemGirisi.TavanMesaji(Dil.Dil)); return KalemDegisti(false); }
                k.BirimFiyat = d;
                k.OncekiFiyat = d;
                k.IscilikTl = null;
                return KalemDegisti(true);
            }
            case "iskonto":
            {
                var alan = $"si-kalem-iskonto-{k.UrunId}";
                var c = string.IsNullOrWhiteSpace(g.Metin)
                    ? new FormAlani.SayiCozumu(FormAlani.SayiSonucu.Tamam, 0m)
                    : FormAlani.SayiCoz(g.Metin);
                if (c.Deger is not { } y || y is < 0m or > 100m) { form.Ekle(alan, c.Mesaj(Dil.Dil) ?? Dil.T.Islem.IskontoIpucu); return KalemDegisti(false); }
                k.SatirIskontoYuzde = y;
                return KalemDegisti(true);
            }
            default:
                return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Karmada "+ Başka ödeme": FisOdemeBacaklari.BacakEkle'nin BİREBİR aynası (en çok 5 satır, kalan önerilir, aynı
    /// araçla açılmaz, nakit yasaksa nakit açılmaz, POS/kasa otomatik, fişin kasası önerilir) + Bildir (OtoDengele →
    /// OdemeDegisti). Özgün düğme (#islem-bacak-ekle) Yıldız'da gizli; eşitlik Yildiz4CTestleri'nde sınanır.
    /// </summary>
    private Task YildizBacakEkle()
    {
        if (bacaklar.Count >= 5) return Task.CompletedTask;
        var kalan = ToplamTl - FisHesap.BacakToplami(bacaklar);
        var yeni = new FisBacagi
        {
            TutarTl = kalan > 0 ? kalan : 0m,
            Arac = FisFark.YeniBacakAraci(bacaklar, YeniOdemeNakitsiz || tevsikNakitKapali),
        };
        FisHesap.BacakPosSabitle(yeni, posCihazlari);
        FisHesap.BacakKasaSabitle(yeni, kasalar);
        if (string.IsNullOrEmpty(yeni.KasaId) && !string.IsNullOrEmpty(kasaId)
            && FisHesap.BacakKasalari(yeni, kasalar).Any(x => x.Id == kasaId)) yeni.KasaId = kasaId;
        bacaklar.Add(yeni);
        FisHesap.OtoDengele(bacaklar, ToplamTl);
        OdemeDegisti();
        return Task.CompletedTask;
    }

    /// <summary>"+ İndirim": BASİT'te mevcut İndirim anahtarı (fiş iskontosu kalem bölümünde açılır);
    /// PRO'da fiş iskontosu zaten "Diğer ayrıntılar" altındadır — aynı anahtar orada da açar.</summary>
    private void YildizIndirim() => IndirimDegistir();
}
