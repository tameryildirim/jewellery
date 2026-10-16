using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-3 — değişim (takas) kimlik kilidi: TEK CÜMLE, EKSİK ALANIN TAMAMI
//
// Hakem (YÜKSEK, 02.10): BASİT Değişimde Kaydet altında "Eksik: TCKN, adres." yazıyor ama
// zorunlu (*) işlem amacı ve beyan açıklaması listede yoktu; TCKN + adres girilince metin
// "Kimlik tespiti eksik: MASAK eşiği aşıldı …" genel cümlesine dönüyordu (hangi alan eksik
// söylenmiyor, BASİT'te jargon). PRO'da da aynı eksiklik vardı.
// Kural: satıcının gider pusulası eksikleri (HurdaGirisi — sunucu kapısının aynası) ve müşteri
// kimlik tespiti eksikleri (MasakFormu.Eksiksiz ile AYNI koşullar) TEK listede, tekrarsız
// birleşir. BASİT: "Eksik: TCKN, adres, işlem amacı, beyan açıklaması (en az 20 karakter)."
// (MASAK kelimesi yok). PRO: aynı liste + gerekçe (VUK md.234 / MASAK).
// İkinci bir kural YAZILMAZ: koşullar HurdaGirisi ve MasakGereksinim'den okunur; burada yalnız
// alanların ADI üretilir.
// ⚠ AYRI DOSYA NEDENİ: Islemler.razor 1.400 satır tavanındadır (S21FisYapiTestleri).
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>Değişimde Kaydet kilidinin birleşik sebebi (UX-S30) — kilit yoksa null.</summary>
    internal string? TakasBirlesikKilit
    {
        get
        {
            if (tur != "TAKAS") return null;
            if (TakasEsnafKilitli) return TakasSaticiKilidi;   // esnaf muafiyeti: alan değil yol sorunu (PARA-DEMO md.3)
            // DEGISIM-GUNSONU tur 2 (tasarım hakemi KRİTİK "kilit aynı alanı iki kez istiyor, ekranda olmayan alanları
            // sayıyor"): bu tutarda Peşin Müşteri olmaz (PesinEsikUstu — sunucunun Peşin kapısının aynası) → kimlik
            // kutusu ekranda YOK; alan saymak yerine TEK adım söylenir. Kural AYNEN (Kaydet yine kilitli).
            if (PesinEsikUstu) return DegisimGunSonuSozlugu.Al(Dil.Dil).OnceMusteriSecin;
            // PAKET TCKN-DOGRULAMA: satıcı TCKN'si ya da kimlik kutusundaki T.C. kimlik no doğrulama kuralından
            // geçmiyorsa kilit, alanın altındaki metnin AYNISINI söyler (UX-S30).
            if (takas.Satici.TcknAlgoritmaHatasi) return KimlikNoSozlugu.Al(Dil.Dil).TcknGecersiz;
            if (MasakGerekli && MasakFormu.TcknKilitMetni(masak, Dil.Dil) is { } masakTckn) return masakTckn;
            var saticiEksik = !takas.Satici.SaticiTamam(Dil.T.Islem);
            var saticiAlanlari = saticiEksik ? SaticiEksikAlanlari() : [];
            var masakAlanlari = MasakGerekli && !masakHata && masakGereksinim is { } g && !MasakFormu.Eksiksiz(masak, g)
                ? MasakEksikAlanlari(g) : [];
            if (saticiAlanlari.Count == 0 && masakAlanlari.Count == 0)
                return saticiEksik ? TakasSaticiKilidi : null;   // adı çıkarılamayan eksik: eski cümle (uydurma yok)
            var pd3 = ParaDemo3Sozlugu.Al(Dil.Dil);
            // Tek kimlik formunda (KimlikBirlesik) TCKN/ad iki listede de vardır: tekrarsız birleşir; kimlik
            // tespitine ÖZGÜ eksik (amaç, beyan, uyruk …) yoksa cümle PARA-DEMO-2'deki satıcı cümlesinin aynısıdır.
            var masakEk = masakAlanlari.Where(a => !saticiAlanlari.Contains(a, StringComparer.Ordinal)).ToList();
            var liste = string.Join(", ", saticiAlanlari.Concat(masakEk).Distinct(StringComparer.Ordinal));
            if (Modu.BasitMi) return string.Format(pd3.TakasEksikBasit, liste);
            if (masakEk.Count == 0) return string.Format(ParaDemo2Sozlugu.Al(Dil.Dil).TakasEksikPro, liste);
            var gerekce = saticiAlanlari.Count > 0
                ? string.Format(pd3.GerekceBirlestir, pd3.GerekceGiderPusulasi, pd3.GerekceKimlikTespiti)
                : pd3.GerekceKimlikTespiti;
            return string.Format(pd3.TakasEksikPro, liste, gerekce);
        }
    }

    /// <summary>Satıcı bloğunun eksik alan adları (HurdaGirisi koşulları; FisTakasSatici.EksikMetni ile aynı adlar).</summary>
    private List<string> SaticiEksikAlanlari()
    {
        var s = takas.Satici;
        var t = Dil.T.Islem;
        var m = ParaDemo2Sozlugu.Al(Dil.Dil);
        var eksik = new List<string>();
        if (s.TipiHata(t) is not null) eksik.Add(Modu.BasitMi ? DegisimGunSonuSozlugu.Al(Dil.Dil).KilitKimGetirdi : m.AlanSaticiTuru);   // DEGISIM-2: BASİT kilit = alanın adı
        if (s.AdHata(t) is not null) eksik.Add(m.AlanAdSoyad);
        // TOPLU-5 (hakem DÜŞÜK "kilit 'TCKN' ↔ alan 'Belge No'"): tek kimlik formunda (KimlikBirlesik) satıcının
        // kimlik no'su kimlik kutusundaki alana girilir — kilit o ALANIN ETİKETİYLE yazar (koşul AYNEN).
        if (s.TcknHata(t) is { } th)
            eksik.Add(KimlikBirlesik
                ? (th == t.SaticiTcknZorunlu ? MasakBelgeNoAlanAdi() : string.Format(T5.AlanHane, MasakBelgeNoAlanAdi()))
                : Modu.BasitMi ? (th == t.SaticiTcknZorunlu ? DegisimGunSonuSozlugu.Al(Dil.Dil).TcKimlikNo : string.Format(T5.AlanHane, DegisimGunSonuSozlugu.Al(Dil.Dil).TcKimlikNo))   // DEGISIM-2: BASİT kilit = etiket
                : (th == t.SaticiTcknZorunlu ? m.AlanTckn : m.AlanTcknHane));
        if (s.AdresHata("x") is not null) eksik.Add(m.AlanAdres);
        if (s.FaturaNoHata(t) is not null) eksik.Add(m.AlanFaturaNo);
        return eksik;
    }

    /// <summary>Kimlik tespiti formunun eksik alan adları — MasakFormu.Eksiksiz ile birebir aynı koşullar.</summary>
    private List<string> MasakEksikAlanlari(MasakGereksinim g)
    {
        var m = ParaDemo2Sozlugu.Al(Dil.Dil);
        var p = ParaDemo3Sozlugu.Al(Dil.Dil);
        var eksik = new List<string>();
        if (!g.KimlikGerekli) return eksik;
        if (string.IsNullOrWhiteSpace(masak.BelgeTuru) || string.IsNullOrWhiteSpace(masak.BelgeNo))
            eksik.Add(MasakBelgeNoAlanAdi());   // TOPLU-5: kilitteki ad = alanın etiketi (koşul AYNEN)
        if (string.IsNullOrWhiteSpace(masak.AdSoyad)) eksik.Add(m.AlanAdSoyad);
        if (!UlkeListesi.KodGecerli(masak.Uyruk)) eksik.Add(p.AlanUyruk);
        if (g.AmacGerekli && string.IsNullOrWhiteSpace(masak.IslemAmaci)) eksik.Add(p.AlanIslemAmaci);
        if (g.AmacGerekli && (masak.Aciklama?.Trim().Length ?? 0) < g.AsgariAciklamaUzunlugu)
            eksik.Add(string.Format(p.AlanBeyanAciklama, g.AsgariAciklamaUzunlugu));
        if (g.BeyanFormuGerekli && string.IsNullOrWhiteSpace(masak.FonKaynagi)) eksik.Add(p.AlanFonKaynagi);
        if (g.EkBelgeGerekli && string.IsNullOrWhiteSpace(masak.EkBelgeReferansi)) eksik.Add(p.AlanEkBelge);
        return eksik;
    }
}
