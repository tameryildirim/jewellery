using System.Globalization;
using System.Text.Json;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-6 — PRO MODDA "DİĞER ▾" İŞLEMLERİNİN İSTEK KATMANI (Islemler sayfasının parçası)
//
// PLAN-SADELESTIRME § S-2 madde 2.9: P-21 ile sunucuda doğan uçlar (rezerv ·
// avans · konsinye · değişim · siparişten satış · ciro/rücu) ekrandan çağrılır
// (UX-S25 "uç var düğme yok" yasağı kapanır).
//
// ⚠ NEDEN SAYFANIN AYRI PARÇASI (partial) VE NEDEN Kaydet() DEĞİL:
//   • PLAN § 3 md.5 "TEK POST YOLU" POS FİŞİ GÖVDESİ içindir: satış/alış/takas
//     gövdesi ve kapı çağrıları yalnız Islemler.razor Kaydet()'te kalır — bu
//     dosya o gövdeye ve Kaydet()'e DOKUNMAZ, fişin POST'larını çoğaltmaz.
//   • Buradaki istekler fiş DEĞİLDİR; her biri kendi sunucu kapısı olan ayrı
//     uçlardır. Fiş bileşenleri (Bilesenler/Fis/FisDiger*) yine İSTEK ATMAZ:
//     niyeti DigerIstek ile bildirir, isteği SAYFA atar (S2-1 deseni).
//   • Islemler.razor ≤ 1.400 satır (S21 yapı testi): istek katmanı .razor'a
//     yazılsaydı sınır aşılırdı; sayfaya yalnız iki satır dokunuldu.
// ⚠ Hata YUTULMAZ (UX-S28): liste okunamazsa ListeHata bandı; yazma hatası
//   sunucunun mesajıyla Hata bandında. Başarı bandı sunucunun döndürdüğü
//   sonucu yazar (UX-S27): kalan adet, açık kapora, fark, sipariş no.
// ⚠ Eşik/mevzuat sayısı YOK; kurSnapshotId her yazmada zorunlu gönderilir.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Evrak listelerinin limiti — ekrandaki "daha fazlası var" uyarısıyla aynı sabit.</summary>
    private const int DigerListeLimit = FisDigerEvrak.EvrakLimit;

    // ── PAKET S1-i — SATIŞ EKRANI DİLİ, SON FİŞ VE DERİN BAĞLANTI ─────────────
    // Razor dosyası 1400 satır tavanındadır; yeni mantık bu parçalı sınıfta.

    [Inject] private NavigationManager Nav { get; set; } = default!;

    private SatisEkraniMetinleri SE => SatisEkraniSozlugu.Al(Dil.Dil);

    /// <summary>Sayfa başlığı = menü ve üst bar adı ("Satış"); iki ad YOK (UX-S31). SATIS-3 t6 (hakem ORTA "üst başlık
    /// her türde Satış"): satış dışı tür seçiliyse başlık o türün adını taşır — BASİT'te karo adı, PRO'da sekme adı.</summary>
    internal string SayfaAdi => tur == "SATIS" || DigerAcik ? MenuAdlari.Icin(Dil.T).Satis.Ad : SeciliTurAdi;

    /// <summary>t6: seçili türün ekran adı — BASİT karo adı (Değişim, Alış · eski altın, İade), PRO sekme adı.</summary>
    internal string SeciliTurAdi =>
        Modu.BasitMi ? FisTurleri.KaroEtiket(ST, tur, TurEtiket(tur)) : TurEtiket(tur);

    /// <summary>Basit yüzde jargonsuz tek cümle; profesyonel yüzde işlem türleri listesi.</summary>
    internal string SayfaAciklamasi => Modu.BasitMi ? SE.BasitAciklama : Dil.T.Islem.Aciklama;

    /// <summary>
    /// QA-9 YÜKSEK: başarı bandı 3 sn'de kaybolur; fiş numarası bir sonraki
    /// Kaydet'e kadar yazdırma bağlantısının YANINDA kalır. Kaydet() bu alanları
    /// zaten her denemede sıfırlar (yol DEĞİŞMEDİ); burada yalnız okunur.
    /// Ham ULID basılmaz (FisYazdir.OkunurNo).
    /// </summary>
    internal string? SonFisNoGorunen => basarili ? FisYazdir.OkunurNo(sonFisNo) ?? sonKayitOzeti : null;

    /// <summary>
    /// PAKET T-S1 (QA-10 kalanı): sunucu fiş numarasını ULID olarak döndürür (okunur
    /// numara YOK — OkunurNo ham ULID basmaz). Tezgâh fişinde "Son fiş" satırı bu
    /// yüzden boş kalıyordu; kaydedilenin SAATİ + TUTARI (kayıttan hemen önce,
    /// istemcinin zaten gösterdiği toplam) bir sonraki Kaydet'e kadar yazdır
    /// bağlantısının yanında kalır. İstek ATILMAZ; POST yolu değişmez; numara UYDURULMAZ.
    /// </summary>
    private string? sonKayitOzeti;
    // SATIS-4 (d) (KESIF-P5): yalnız BASİT tezgâh fişi değil — HER türde ve PRO'da da (Islemler.Satis4.cs SonKayitMetni).
    private void SonKaydiTut() => sonKayitOzeti = SonKayitMetni();

    /// <summary>
    /// /gider-pusulalari "Hurda alımı gir" → /islemler?tur=HURDA_ALIS. Yalnız
    /// ekranda GÖRÜNEN türler kabul edilir (basit yüzde BasitPos listesi);
    /// bilinmeyen değer TAHMİN EDİLMEZ, satış açık kalır.
    /// </summary>
    private void SorgudanSuzgecOku()
    {
        var istenen = DerinBaglanti.Oku(Nav.Uri, "tur");
        if (istenen is null || istenen == tur) return;
        if (TurListesi.Any(x => x.Deger == istenen)) TurSec(istenen);
    }


    private readonly DigerIslemDurumu diger = new();

    /// <summary>Diğer paneli açık mı — açıksa POS fişi çizilmez (tek ekran, tek birincil iş).</summary>
    private bool DigerAcik => diger.Secim is not null && !Modu.BasitMi;

    /// <summary>Bileşenlere giden bağlam: sayfanın ZATEN yüklediği veriler (yeni istek yok).</summary>
    private DigerBaglam DigerBaglami =>
        new(cariler, urunler, AnaKasalar, sonSatislar, !string.IsNullOrEmpty(kurId), Oturum.Kullanici?.Rol, Oturum.Kullanici?.SubeId, tevsikHaddi);

    /// <summary>Sekmeye dönülünce Diğer paneli kapanır (fiş geri gelir).</summary>
    private async Task TurSecDiger(string yeni)
    {
        yildizTurlerAcik = false;   // P-TEZGAH-TUR-KIMLIK: Yıldız'da "Diğer" listesinden seçilince liste kapanır (seçili tür sekmede görünür)
        diger.Sec(null);
        TurSec(yeni);
        // SATIS-3 (KAYNAK 2 md.4): hurda satıcı adresinin zorunluluğu SUNUCU yapılandırmasından
        // (GiderPusulasiKapisi ile aynı değer); yüklenemezse tutucu: zorunlu.
        if (tur == "HURDA_ALIS")
        {
            await AltinAyarlariYukle();
            hurda.AdresZorunlu = altinAyarlari?.SaticiAdresiZorunlu ?? true;
        }
        else if (tur == "TAKAS")
        {
            // PARA-DEMO (madde 3): takasta da aynı satıcı kimliği kuralı (aynı yapılandırma, tutucu varsayılan).
            await AltinAyarlariYukle();
            takas.Satici.AdresZorunlu = altinAyarlari?.SaticiAdresiZorunlu ?? true;
            await TakasHaddiYukle();   // DEGISIM-GUNSONU: nakit sınırı Satış/Alış'la aynı kaynaktan (Islemler.Degisim.cs)
        }
    }

    private FisDigerMetinleri DM => FisDigerSozlugu.Al(Dil.Dil);

    private static string Adet(decimal a) => a.ToString("0.###", CultureInfo.GetCultureInfo("tr-TR"));

    private static decimal Sayi(JsonElement e, string ad) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(ad, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal() : 0m;

    private static string? Metin(JsonElement e, string ad) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(ad, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    /// <summary>Bileşen niyetini karşılar. Her yazma eylemi TEK bir uca gider.</summary>
    private async Task DigerIstekIsle(DigerIstek istek)
    {
        switch (istek.Eylem)
        {
            case DigerEylemler.Kapat or DigerEylemler.Ciz:
                return;   // istek yok — EventCallback sayfayı yeniden çizer
            case DigerEylemler.Yukle:
                await DigerYukle();
                return;
            case DigerEylemler.AvansSorgula:
                await AvansSorgula(istek.Id);
                return;
            case DigerEylemler.SiparisSec:
                await SiparisDetayAl(istek.Id);
                return;
        }

        diger.Mesgul = true;
        diger.Hata = null;
        diger.Red = null;
        diger.Basari = null;
        try
        {
            diger.Basari = await DigerYaz(istek);
            diger.Jeton++;
        }
        // t22 (tasarım hakemi YÜKSEK): sunucunun nakit (tevsik) reddi sayfanın dibinde ham VUK paragrafı olarak DURMAZ —
        // düğmenin yanında kısa, büyük harfsiz tek cümle (Hurda.razor TevsikReddi ile aynı tanıma). YALNIZ ret metnini çizen
        // eylemler (AvansAl, Degisim) için; kapora iadesi / siparişten satış vb. eski hata bandında görünür (t23: sessiz etkisizlik YASAK).
        catch (ApiHata ex) when (ex.StatusCode == 400 && ex.Message.StartsWith("Tevsik", StringComparison.Ordinal)
                               && istek.Eylem is DigerEylemler.AvansAl or DigerEylemler.Degisim)
        { diger.Red = (istek.Eylem, Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).DigerNakitRed); }
        // YASAL-KAPI-2: siparişten satışta MASAK reddi ham metinle basılmaz — kimlik alanları formda açılır.
        catch (ApiHata ex) when (istek.Eylem == DigerEylemler.SiparisCevir && diger.Siparis.Masak is { } pm
                               && Kuyumcu.Web.Bilesenler.ParaMasakKapisi.MasakReddi(ex))
        { await pm.Reddet(ex, Dil.Dil); diger.Red = (istek.Eylem, ""); }
        catch (ApiHata ex) { diger.Hata = FormDurumu.SunucuMesaji(ex, Dil); }
        catch (JsonException ex) { diger.Hata = ex.Message; }
        finally { diger.Mesgul = false; }

        // Başarılı yazmadan sonra etkilenen liste AYNI ekranda tazelenir (UX-S23).
        if (diger.Hata is null && diger.Red is null) await DigerYukle();
    }

    /// <summary>Yazma eylemleri — dönüş: başarı bandına yazılacak sunucu sonucu.</summary>
    private async Task<string> DigerYaz(DigerIstek istek)
    {
        var m = DM;
        var kasa = diger.EtkinKasa(DigerBaglami);
        switch (istek.Eylem)
        {
            case DigerEylemler.RezervAc:
            {
                var r = await Api.PostAsync("/islemler/rezervler", diger.Rezerv.Govde());
                diger.Denenen.Remove(DigerEylemler.RezervAc);
                return string.Format(m.RezervBasari, Adet(Sayi(r, "adet")), Adet(Sayi(r, "satilabilirAdet")));
            }
            case DigerEylemler.RezervDuzenle:
            {
                // PATCH sözleşmesi: yalnız değişen alan (gönderilmeyen = korunur, null = sil).
                var r = await Api.PatchAsync($"/islemler/rezervler/{Uri.EscapeDataString(istek.Id ?? "")}", diger.RezervDuzen.Govde());
                diger.RezervDuzen.Kapat();
                diger.Denenen.Remove(DigerEylemler.RezervDuzenle);
                return string.Format(m.RezervGuncellendi, Adet(Sayi(r, "satilabilirAdet")));
            }
            case DigerEylemler.RezervIptal:
            {
                var r = await Api.PostAsync($"/islemler/rezervler/{Uri.EscapeDataString(istek.Id ?? "")}/iptal",
                    new { gerekce = diger.RezervIptalGerekce.Trim() });
                diger.RezervIptalGerekce = "";
                return string.Format(m.RezervKaldirildi, Adet(Sayi(r, "satilabilirAdet")));
            }
            case DigerEylemler.AvansAl:
            {
                var a = diger.Avans;
                var r = await Api.PostAsync("/islemler/avans", new
                {
                    cariId = a.CariId, tutar = a.Tutar, odemeTuru = a.OdemeTuru, kasaId = kasa,
                    kurSnapshotId = kurId, aciklama = string.IsNullOrWhiteSpace(a.Aciklama) ? null : a.Aciklama.Trim(),
                });
                a.AcikAvans = Sayi(r, "acikAvans");
                a.Tutar = null;
                diger.Denenen.Remove(DigerEylemler.AvansAl);
                return string.Format(m.AvansBasari, Bicim.Para(Sayi(r, "tutar"), "TRY"), Bicim.Para(a.AcikAvans.Value, "TRY"));
            }
            case DigerEylemler.AvansMahsup:
            {
                var a = diger.Avans;
                var r = await Api.PostAsync("/islemler/avans/mahsup", new
                {
                    satisHareketId = a.MahsupSatisId, tutar = a.MahsupTutar, kurSnapshotId = kurId,
                });
                a.AcikAvans = Sayi(r, "acikAvans");
                a.MahsupTutar = null;
                diger.Denenen.Remove(DigerEylemler.AvansMahsup);
                return string.Format(m.MahsupBasari, Bicim.Para(Sayi(r, "mahsupTutar"), "TRY"), Bicim.Para(a.AcikAvans.Value, "TRY"));
            }
            case DigerEylemler.AvansIade:
            {
                var a = diger.Avans;
                var r = await Api.PostAsync("/islemler/avans/iade", new
                {
                    cariId = a.CariId, tutar = a.IadeTutar, kasaId = kasa, kurSnapshotId = kurId,
                    gerekce = a.IadeGerekce.Trim(),
                });
                a.AcikAvans = Sayi(r, "acikAvans");
                a.IadeTutar = null;
                a.IadeGerekce = "";
                diger.Denenen.Remove(DigerEylemler.AvansIade);
                return string.Format(m.AvansIadeBasari, Bicim.Para(Sayi(r, "tutar"), "TRY"), Bicim.Para(a.AcikAvans.Value, "TRY"));
            }
            case DigerEylemler.KonsinyeCikis:
            {
                var k = diger.Konsinye;
                var r = await Api.PostAsync("/islemler/konsinye-cikis", new
                {
                    cariId = k.CariId, urunId = k.UrunId, adet = k.Adet, kurSnapshotId = kurId,
                    aciklama = string.IsNullOrWhiteSpace(k.Aciklama) ? null : k.Aciklama.Trim(),
                });
                diger.Denenen.Remove(DigerEylemler.KonsinyeCikis);
                return string.Format(m.KonsinyeBasari, Adet(Sayi(r, "adet")), Adet(Sayi(r, "satilabilirAdet")));
            }
            case DigerEylemler.KonsinyeIade:
            {
                var id = istek.Id ?? "";
                var r = await Api.PostAsync("/islemler/konsinye-iade", new
                {
                    cikisHareketId = id, adet = diger.Konsinye.GeriAdet.GetValueOrDefault(id), kurSnapshotId = kurId,
                });
                diger.Konsinye.GeriAdet.Remove(id);
                return string.Format(m.KonsinyeIadeBasari, Adet(Sayi(r, "adet")), Adet(Sayi(r, "kalanAcikAdet")));
            }
            case DigerEylemler.Degisim:
            {
                var d = diger.Degisim;
                var r = await Api.PostAsync("/islemler/degisim", new
                {
                    kaynakHareketId = d.KaynakHareketId, geriAlinanAdet = d.GeriAlinanAdet,
                    yeniUrunId = d.YeniUrunId, yeniAdet = d.YeniAdet, yeniBirimFiyatTl = d.YeniBirimFiyat,
                    farkOdemeTuru = d.FarkOdemeTuru, kasaId = d.FarkOdemeTuru == "NAKIT" ? kasa : null,
                    kurSnapshotId = kurId, aciklama = string.IsNullOrWhiteSpace(d.Aciklama) ? null : d.Aciklama.Trim(),
                });
                diger.Denenen.Remove(DigerEylemler.Degisim);
                d.KaynakHareketId = null;
                await SonSatislariTazele();   // kalan iade hakkı değişti — liste aynı ekranda tazelenir
                var yon = Metin(r, "farkYonu") switch
                {
                    "MUSTERI_ODER" => m.FarkMusteriOder,
                    "BIZ_IADE_EDERIZ" => m.FarkBizOderiz,
                    _ => m.FarkYok,
                };
                return string.Format(m.DegisimBasari, Bicim.Para(Math.Abs(Sayi(r, "farkTl")), "TRY"), yon);
            }
            case DigerEylemler.SiparisCevir:
            {
                var s = diger.Siparis;
                var sp = s.Secili ?? throw new ApiHata(400, m.ZorunluAlan);
                var r = await Api.PostAsync($"/siparisler/{Uri.EscapeDataString(sp.Id)}/satisa-cevir", new
                {
                    kalemler = (sp.Kalemler ?? []).Select(k => new { siparisKalemId = k.Id, birimFiyatTl = s.Fiyat.GetValueOrDefault(k.Id) }).ToList(),
                    odemeler = new[] { new { arac = s.Arac, tutarTl = s.Toplam, kasaId = s.Arac == "VERESIYE" ? null : kasa } },
                    kurSnapshotId = kurId,
                    avansMahsupTl = s.AvansMahsup is > 0m ? s.AvansMahsup : null,
                    masak = s.Masak?.Govde,   // YASAL-KAPI-2: yalnız kapı açıkken (eşik altında gövdede beyan yok)
                });
                s.Secili = null;
                s.Masak?.Sifirla();   // kişisel veri formda kalmaz
                s.Fiyat.Clear();
                s.AvansMahsup = null;
                diger.Denenen.Remove(DigerEylemler.SiparisCevir);
                await SonSatislariTazele();   // doğan satış bu ekranın son satışlarında GÖRÜNÜR
                return string.Format(m.SiparisBasari, Metin(r, "siparisNo") ?? sp.No,
                    Sayi(r, "kalemSayisi").ToString(CultureInfo.InvariantCulture), Bicim.Para(Sayi(r, "avansMahsupTl"), "TRY"));
            }
            case DigerEylemler.EvrakCiro:
            {
                var e = diger.Evrak;
                var no = diger.Devredilebilir?.FirstOrDefault(x => x.Id == istek.Id)?.EvrakNo ?? "";
                await Api.PostAsync($"/evraklar/{Uri.EscapeDataString(istek.Id ?? "")}/ciro", new
                {
                    ciroCariId = e.CiroCariId, kurSnapshotId = kurId,
                });
                e.CiroEvrakId = null;
                diger.Denenen.Remove(DigerEylemler.EvrakCiro);
                return string.Format(m.DevretBasari, no);
            }
            case DigerEylemler.EvrakRucu:
            {
                var e = diger.Evrak;
                var no = diger.Devredilmis?.FirstOrDefault(x => x.Id == istek.Id)?.EvrakNo ?? "";
                await Api.PostAsync($"/evraklar/{Uri.EscapeDataString(istek.Id ?? "")}/rucu", new
                {
                    aciklama = e.RucuAciklama.Trim(), kurSnapshotId = kurId,
                });
                e.RucuEvrakId = null;
                e.RucuAciklama = "";
                diger.Denenen.Remove(DigerEylemler.EvrakRucu);
                return string.Format(m.DonenBasari, no);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(istek), istek.Eylem, "Bilinmeyen Diğer eylemi");
        }
    }

    /// <summary>Açık panelin listeleri. Hata "kayıt yok"a çevrilmez (UX-S28).</summary>
    private async Task DigerYukle()
    {
        diger.ListeHata = null;
        // t22: nakit alan Diğer formu (Kapora, Değişim) sunucunun haddini bilmeli — sayfa Satış'ta zaten aldıysa istek yok.
        if (tevsikHaddi is null && diger.Secim is DigerTurleri.Avans or DigerTurleri.Degisim)
        {
            try { var y = await TevsikGetir(); if (y.Had > 0m) tevsikHaddi = y.Had; }
            catch (ApiHata) { /* had bilinmez → ön denetim yok; kapı sunucuda, reddi kısa metinle yazılır */ }
        }
        try
        {
            switch (diger.Secim)
            {
                case DigerTurleri.Rezerv:
                    diger.Rezervler = await Api.GetAsync<RezervListesi>("/islemler/rezervler?durum=AKTIF&limit=50");
                    break;
                case DigerTurleri.Konsinye:
                    diger.Konsinyeler = await Api.GetAsync<KonsinyeListesi>("/islemler/konsinye-acik?limit=50");
                    break;
                case DigerTurleri.Siparis:
                    diger.Siparisler = await Api.GetAsync<SiparisListesi>($"/siparisler?durum=HAZIR&limit={DigerListeLimit}");
                    break;
                case DigerTurleri.Evrak:
                    var portfoy = await Api.GetAsync<EvrakListesi>($"/evraklar?durum=PORTFOYDE&limit={DigerListeLimit}");
                    var ciro = await Api.GetAsync<EvrakListesi>($"/evraklar?durum=CIRO_EDILDI&limit={DigerListeLimit}");
                    // Devredilebilirliğe SUNUCU karar verir (izinli geçişlerinde CIRO_EDILDI olan).
                    diger.Devredilebilir = (portfoy.Satirlar ?? []).Where(e => e.IzinliGecisler.Contains("CIRO_EDILDI")).ToList();
                    diger.Devredilmis = ciro.Satirlar ?? [];
                    diger.EvrakDahaFazla = portfoy.DahaFazlasiVar || ciro.DahaFazlasiVar;
                    break;
                case DigerTurleri.Avans when diger.Avans.CariId is { } cari:
                    await AvansSorgula(cari);
                    break;
            }
        }
        catch (ApiHata ex) { diger.ListeHata = string.Format(DM.ListeHatasi, FormDurumu.SunucuMesaji(ex, Dil)); }
        catch (JsonException ex) { diger.ListeHata = string.Format(DM.ListeHatasi, ex.Message); }
    }

    private async Task AvansSorgula(string? cariId)
    {
        if (string.IsNullOrEmpty(cariId)) return;
        try
        {
            var r = await Api.GetAsync($"/islemler/avans/{Uri.EscapeDataString(cariId)}");
            diger.Avans.AcikAvans = Sayi(r, "acikAvans");
        }
        catch (ApiHata ex) { diger.ListeHata = string.Format(DM.ListeHatasi, FormDurumu.SunucuMesaji(ex, Dil)); }
        catch (JsonException ex) { diger.ListeHata = string.Format(DM.ListeHatasi, ex.Message); }
    }

    private async Task SiparisDetayAl(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        try
        {
            diger.Siparis.Secili = await Api.GetAsync<SiparisDetayi>($"/siparisler/{Uri.EscapeDataString(id)}");
            // YASAL-KAPI-2: siparişten satışın MASAK kapısı (ortak ParaMasakKapisi; yeni siparişte beyan sıfırlanır).
            diger.Siparis.Masak ??= Kuyumcu.Web.Bilesenler.ParaMasakKapisi.Olustur(Api);
            await diger.Siparis.Masak.Ac(diger.Siparis.Secili?.CariId);
        }
        catch (ApiHata ex) { diger.ListeHata = string.Format(DM.ListeHatasi, FormDurumu.SunucuMesaji(ex, Dil)); }
        catch (JsonException ex) { diger.ListeHata = string.Format(DM.ListeHatasi, ex.Message); }
    }
}
