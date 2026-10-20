using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Layout;

/// <summary>
/// Rota şablonu → sayfa başlığı haritası (paket UX-2 Faz A1, Bertan KOŞUL 3).
/// <para>
/// Eskiden UstBar.razor içinde bir <c>switch</c> vardı; 12 rota tanınıyor,
/// kalan 15 rota <c>_ =&gt; "Kuyumcu"</c> geri dalına düşüyordu. O geri dal
/// KALDIRILDI: eksik rota artık sessizce yutulmaz, RotaBaslikTestleri
/// yansımayla bulduğu her <c>@page</c> rotasını burada arar ve buradaki her
/// anahtarın gerçek bir rotaya karşılık geldiğini de doğrular (iki yönlü).
/// </para>
/// <para>
/// ⚠ Eşleştirme ŞABLON iledir, ham yol karşılaştırması değil:
/// <c>/gider-pusulalari/{Id}/belge</c> çalışma zamanında somut bir kimlik
/// taşır ve hiçbir sabit anahtarla eşleşmezdi.
/// </para>
/// <para>
/// ⚠ Yeni sayfa ekleyen: <c>@page</c> yazdıysan buraya da bir satır yaz
/// (UX-S13). Yazmazsan RotaBaslikTestleri kırmızıdır.
/// </para>
/// <para>
/// ⚠ PAKET S1-g: menüdeki 40 rotanın başlığı artık MENÜ ADIDIR
/// (<see cref="MenuAdlari"/>, tuvalle birebir) — menüde "Kim ne yaptı" yazan
/// ekranın üst barında "Denetim İzi" yazamaz (UX-S31, S1gMenuTuvalTestleri).
/// </para>
/// </summary>
public static class RotaBasliklari
{
    /// <summary>Rota şablonu → sözlükten başlığı seçen işlev.</summary>
    public static readonly IReadOnlyDictionary<string, Func<Sozluk, string>> Harita =
        new Dictionary<string, Func<Sozluk, string>>(StringComparer.Ordinal)
        {
            ["/"] = t => MenuAdlari.Icin(t).Panel.Ad,
            ["/giris"] = t => t.Nav.Giris,
            // PAKET AUTH-SIFIRLA: "Parolamı unuttum" (kimlik öncesi).
            ["/sifre-sifirla"] = t => GirisSozlugu.ParolamiUnuttum(t.DilKodu),
            ["/cariler"] = t => MenuAdlari.Icin(t).Musteriler.Ad,
            ["/urunler"] = t => MenuAdlari.Icin(t).Urunler.Ad,
            ["/urunler/agac"] = t => MenuAdlari.Icin(t).UrunAgaci.Ad,
            ["/urunler/etiketler"] = t => MenuAdlari.Icin(t).Etiketler.Ad,
            ["/islemler"] = t => MenuAdlari.Icin(t).Satis.Ad,
            ["/emanet"] = t => MenuAdlari.Icin(t).Emanet.Ad,
            ["/kur"] = t => MenuAdlari.Icin(t).Kur.Ad,
            ["/kasalar"] = t => MenuAdlari.Icin(t).Kasalar.Ad,
            // Paket 31 — POS cihazları ve POS alacak defteri.
            ["/pos"] = t => MenuAdlari.Icin(t).PosCihazlari.Ad,
            ["/pos/tahsilatlar"] = t => MenuAdlari.Icin(t).PosAlacaklari.Ad,
            ["/vezne"] = t => MenuAdlari.Icin(t).KasaKuyrugu.Ad,
            ["/gise"] = t => MenuAdlari.Icin(t).GiseZimmeti.Ad,   // Y-03
            ["/banka"] = t => MenuAdlari.Icin(t).Banka.Ad,
            // PAKET P-19 (Y-11/Y-12) — gider + virman.
            ["/gider"] = t => MenuAdlari.Icin(t).GiderVirman.Ad,
            // PAKET P-20 (Y-10) — hurda stoğu / ramat çıkışı.
            ["/hurda"] = t => MenuAdlari.Icin(t).HurdaRamat.Ad,
            // Paket 32: ekstre eşleştirme ekranı menüde DEĞİLDİR ama üst bar
            // başlığı taşımak ZORUNDADIR — aksi hâlde "Sayfa bulunamadı" yazar.
            ["/banka/ekstre/{IthalatId}"] = t => t.Banka.EkstreBaslik,
            ["/faturalar"] = t => MenuAdlari.Icin(t).Faturalar.Ad,
            // 20.10 PAKET-E-BELGE: giden kutusu — menüde değil (Faturalar'dan bağlantı), üst bar başlığı taşır.
            ["/e-belgeler"] = t => Kuyumcu.Core.I18n.EBelgeSozlugu.KutuBaslik(t.DilKodu),
            ["/gider-pusulalari"] = t => MenuAdlari.Icin(t).GiderPusulalari.Ad,
            ["/gider-pusulalari/{Id}/belge"] = t => t.Nav.PusulaBelgesi,
            ["/raporlar"] = t => MenuAdlari.Icin(t).RaporMerkezi.Ad,
            ["/raporlar/kar-zarar"] = t => MenuAdlari.Icin(t).KarZarar.Ad,
            ["/raporlar/vade-takibi"] = t => MenuAdlari.Icin(t).VadeTakibi.Ad,
            // Paket 34 Faz A — rapor seti ekranları.
            ["/raporlar/islem-defteri"] = t => MenuAdlari.Icin(t).IslemDefteri.Ad,
            ["/raporlar/satis-analizi"] = t => MenuAdlari.Icin(t).SatisAnalizi.Ad,
            ["/raporlar/kasa"] = t => MenuAdlari.Icin(t).KasaRaporu.Ad,
            ["/fiyatlandirma"] = t => MenuAdlari.Icin(t).FiyatListeleri.Ad,
            ["/sayim"] = t => MenuAdlari.Icin(t).Sayim.Ad,
            // PAKET EKSIK-B1-SATINALMA (B1-1 · B1-2).
            ["/satin-alma"] = t => MenuAdlari.Icin(t).SatinAlma.Ad,
            ["/irsaliyeler"] = t => MenuAdlari.Icin(t).Irsaliyeler.Ad,
            ["/kullanicilar"] = t => MenuAdlari.Icin(t).Kullanicilar.Ad,
            ["/yetkiler"] = t => MenuAdlari.Icin(t).Yetkiler.Ad,
            ["/isletme"] = t => MenuAdlari.Icin(t).Isletme.Ad,
            ["/ayarlar"] = t => MenuAdlari.Icin(t).Ayarlar.Ad,
            ["/kilavuz"] = t => MenuAdlari.Icin(t).Kilavuz.Ad,
            ["/uyum"] = t => MenuAdlari.Icin(t).Uyum.Ad,
            ["/kvkk"] = t => MenuAdlari.Icin(t).Kvkk.Ad,
            ["/denetim-izi"] = t => MenuAdlari.Icin(t).KimNeYapti.Ad,
            ["/veri-disa-aktarim"] = t => MenuAdlari.Icin(t).VeriDisaAktar.Ad,
            ["/veri-ice-aktarim"] = t => MenuAdlari.Icin(t).VeriIceAktar.Ad,
            // Paket T-2 Faz A — kuruluş sihirbazı.
            ["/kurulum"] = t => MenuAdlari.Icin(t).Kurulum.Ad,
            ["/bildirimler"] = t => MenuAdlari.Icin(t).Bildirimler.Ad,
            // Paket 29 Faz A — çek/senet portföyü ve vade takvimi.
            ["/evraklar"] = t => MenuAdlari.Icin(t).CekSenet.Ad,
            ["/evraklar/takvim"] = t => MenuAdlari.Icin(t).VadeTakvimi.Ad,
            ["/siparisler"] = t => MenuAdlari.Icin(t).SiparisTamir.Ad,
            ["/siparisler/{Id}"] = t => MenuAdlari.Icin(t).SiparisTamir.Ad,
            // Paket T-3 Faz A — yedekler ekranı (yalnız profesyonel mod, SAHİP).
            ["/yedekler"] = t => MenuAdlari.Icin(t).Yedekler.Ad,
            // PAKET P-17 (Y-7) — vitrin panosu.
            ["/vitrin"] = t => t.Pano.Baslik,
            // PAKET EKSIK-KUCUK (B2-10) — vitrin dükkân ekranı (tam ekran).
            ["/vitrin/ekran"] = t => Kuyumcu.Core.I18n.EksikKucukSozlugu.Al(t.DilKodu).TamEkranRotaBaslik,
            ["/fiyat/{Anahtar}"] = t => Kuyumcu.Core.I18n.CanliFiyatSozlugu.Baslik(t.DilKodu),   // Y-14
            // PAKET S1-d — karo sayfası; başlık kapalı sözlükten (Sozlukler.cs
            // ortak hub'ına dokunulmadı). Dil, sözlük nesnesinin kimliğinden okunur.
            ["/ekranlar"] = t => EkranlarSozlugu.Baslik(t.DilKodu),
            // PAKET P-17 (Y-6) — ortak A4 çıktı; menüde DEĞİLDİR ama üst bar
            // başlığı taşımak ZORUNDADIR (aksi hâlde "Sayfa bulunamadı" yazar).
            ["/cikti/{Tur}/{Id}"] = t => t.Cikti.Baslik,
            // PAKET YASAL-1 — yasal sınırlar (salt okunur); başlık kapalı sözlükten.
            ["/yasal-sinirlar"] = t => YasalSinirSozlugu.Al(t.DilKodu).Baslik,
        };

    /// <summary>
    /// Somut yolu haritadaki şablonla eşleştirir. Eşleşme yoksa <c>null</c>
    /// döner — bu bir "geri dal" DEĞİLDİR: hiçbir gerçek rota buraya düşemez
    /// (test iki yönlü zorlar), yalnız var olmayan bir adres (Router'ın
    /// NotFound dalı) buraya düşer.
    /// </summary>
    public static Func<Sozluk, string>? Bul(string yol)
    {
        var duzgun = Duzgunlestir(yol);
        if (Harita.TryGetValue(duzgun, out var dogrudan)) return dogrudan;

        var parcalar = Parcala(duzgun);
        foreach (var (sablon, secici) in Harita)
        {
            var sablonParcalari = Parcala(Duzgunlestir(sablon));
            if (sablonParcalari.Length != parcalar.Length) continue;

            var uyar = true;
            for (var i = 0; i < parcalar.Length && uyar; i++)
            {
                var s = sablonParcalari[i];
                var parametre = s.StartsWith('{') && s.EndsWith('}');
                uyar = parametre
                    ? parcalar[i].Length > 0
                    : string.Equals(s, parcalar[i], StringComparison.OrdinalIgnoreCase);
            }
            if (uyar) return secici;
        }
        return null;
    }

    /// <summary>Üst barda gösterilecek başlık. Bilinmeyen adres = "Sayfa bulunamadı".</summary>
    public static string Baslik(string yol, Sozluk sozluk) =>
        (Bul(yol) ?? (t => t.Nav.Bulunamadi))(sozluk);

    /// <summary>Rota şablonlarını tek biçime indirger: baştaki '/' var, sondaki yok.</summary>
    public static string Duzgunlestir(string yol)
    {
        var y = yol.Trim();
        if (!y.StartsWith('/')) y = "/" + y;
        if (y.Length > 1) y = y.TrimEnd('/');
        return y.Length == 0 ? "/" : y;
    }

    private static string[] Parcala(string yol) =>
        yol == "/" ? [] : yol.TrimStart('/').Split('/');
}
