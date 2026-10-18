using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuyumcu.Api.Modules.Meta;

// ═════════════════════════════════════════════════════════════════════════════
// DÜZELTME POLİTİKASI HARİTASI — paket UX-1 Faz A.
//
// HEDEF (UX-1): kullanıcının veri girdiği HER noktada bir düzeltme yolu olsun
// ve o yol EKRANDA GÖRÜNSÜN. Yol üç türden biridir:
//   DUZENLENEBILIR → yerinde PATCH/PUT/DELETE ucu vardır
//   STORNO         → deftere yazılmıştır; yalnız TERS KAYITLA düzeltilir
//   SALT_OKUNUR    → doğası gereği değiştirilemez, gerekçesi yazılıdır
//
// TEK KAYNAK SUNUCUDADIR (Bertan — risk: "haritanın iki kopyası sessizce
// ayrışır"). Ekran bu haritayı /api/v1/meta/duzeltme-politikalari ucundan
// okur; razor dosyasında sabit politika YAZILMAZ.
//
// ⚠ MEHMET K8: HedefUc YALNIZ BİR YOL DİZGESİDİR — belgeleme ve test içindir.
//   İstemci bu dizgeyi alıp dinamik istek KURMAZ (sunucu verisiyle yönlendirilen
//   istek deseni doğmasın). İstemci uçları kendi kodunda açıkça yazar.
//
// ⚠ K-UX-2: defter tablolarına (hareketler, kasa_hareketleri, banka_hareketleri,
//   denetim_kayitlari, masak_*, gun_kapanislari) PATCH/DELETE ucu AÇILMAZ.
//   Bu haritada onların HedefUc'u ya null'dır ya da bir POST ters-kayıt ucudur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Üç politika damgası — başka değer yoktur.</summary>
public static class Politikalar
{
    public const string Duzenlenebilir = "DUZENLENEBILIR";
    public const string Storno = "STORNO";
    public const string SaltOkunur = "SALT_OKUNUR";

    public static readonly string[] Tumu = [Duzenlenebilir, Storno, SaltOkunur];
}

/// <summary>
/// Gerekçe ANAHTARLARI — çıplak metin taşınmaz; ekran anahtarı sözlükten
/// (TR+EN) çözer. Yeni anahtar eklenince Sozlukler.Tr ve Sozlukler.En
/// karşılıkları da eklenir (DuzeltmePolitikasiTestleri bunu zorlar).
/// </summary>
public static class GerekceAnahtarlari
{
    public const string Serbest = "SERBEST";
    public const string Defter = "DEFTER";
    public const string Denetim = "DENETIM";
    public const string Masak = "MASAK";
    public const string Sistem = "SISTEM";
    public const string Turetilmis = "TURETILMIS";
    public const string Kapanis = "KAPANIS";
    public const string Sayim = "SAYIM";
    public const string DisaAktarim = "DISA_AKTARIM";
    public const string Belge = "BELGE";
    public const string Fatura = "FATURA";
    public const string UcYok = "UC_YOK";
    public const string StornoYok = "STORNO_YOK";

    /// <summary>
    /// PAKET S-3b — VUK md.234 belgesi: içeriği yerinde değiştirilmez, ama
    /// GEREKÇELİ İPTAL + YENİ PUSULA yolu vardır. "STORNO_YOK" artık doğru
    /// değildi (yol açıldı); "FATURA" ise başka bir belgenin adıdır (UX-S31:
    /// aynı kavram tek ad). Bu yüzden kendi anahtarı vardır.
    public const string Pusula = "PUSULA";
    /// <summary>Paket 47 — içe aktarım satırı bir kanıt kaydıdır.</summary>
    public const string IceAktarim = "ICE_AKTARIM";

    public static readonly string[] Tumu =
    [
        Serbest, Defter, Denetim, Masak, Sistem, Turetilmis, Kapanis,
        Sayim, DisaAktarim, Belge, Fatura, UcYok, StornoYok, IceAktarim, Pusula,
    ];
}

/// <param name="Varlik">Varlık sınıf adı (Data/Entities.cs) ya da hareket türü.</param>
/// <param name="Politika">DUZENLENEBILIR · STORNO · SALT_OKUNUR.</param>
/// <param name="GerekceAnahtari">Ekranın sözlükten çözeceği anahtar.</param>
/// <param name="HedefUc">Yol dizgesi — yalnız belge/test değeri (Mehmet K8).</param>
/// <param name="AlternatifYol">Düzeltmenin yapılabildiği başka bir yol (varsa).</param>
public sealed record VarlikPolitikasi(
    string Varlik,
    string Politika,
    string GerekceAnahtari,
    string? HedefUc = null,
    string? AlternatifYol = null);

public static class DuzeltmePolitikasi
{
    private static VarlikPolitikasi D(string ad, string uc) =>
        new(ad, Politikalar.Duzenlenebilir, GerekceAnahtarlari.Serbest, uc);

    private static VarlikPolitikasi S(string ad, string anahtar, string? alternatif = null) =>
        new(ad, Politikalar.SaltOkunur, anahtar, null, alternatif);

    private static VarlikPolitikasi T(string ad, string anahtar, string? uc = null) =>
        new(ad, Politikalar.Storno, anahtar, uc);

    // ── Varlıklar ────────────────────────────────────────────────────────────
    // Kapsam: KuyumcuDbContext'teki HER DbSet varlığı. Yeni varlık eklenip
    // buraya damga konmazsa DuzeltmePolitikasiTestleri kırmızıya döner.
    private static readonly VarlikPolitikasi[] VarlikListesi =
    [
        // UX-1 Faz B: işletme kimliği (ad/adres/vergi dairesi/vergi no/telefon)
        // artık düzeltilebilir — kayıt sırasında girilen ad bir daha
        // değiştirilemiyordu. Plan ve AI kendi uçlarında kalır (toplu atama yok).
        D("Tenant", "api/v1/admin/tenant/kimlik"),
        // Paket F-2-min: kiracı daveti PATCH ile iptal edilir ve iptali geri
        // alınabilir (silinmez — "kim kimi davet etti" izi korunur). Uç sistem
        // anahtarıyla korunur; ekranı Faz B'dedir (dalga 6).
        D("KiraciDaveti", "api/v1/sistem/davetler/{id}"),
        // ── PAKET P-27 (F-1 abonelik + F-3 plan kapısı) ──────────────────
        // Abonelik DÖNEMİ append-only bir HAK KAYDIDIR: satırı yerinde
        // düzeltmek "geçmişte hangi hakka sahiptin" sorusunun cevabını
        // değiştirirdi. Düzeltme yolu YENİ DÖNEM açmaktır — kiracı yalnız
        // İNDİREBİLİR (PATCH /admin/plan), yükseltme operatör ucundadır.
        S("KiraciAbonelik", GerekceAnahtarlari.Sistem, "api/v1/admin/plan"),
        // Plan KATALOĞU kiracı-üstüdür: bir kiracının ekranından değiştirilemez
        // (değiştirilseydi tüm kiracıların hakkı değişirdi). Yolu operatördedir.
        S("AbonelikPlani", GerekceAnahtarlari.Sistem, "api/v1/sistem/kiracilar/{id}/abonelik"),
        // PAKET YASAL-1: yasal parametre ULUSAL rakamdır (kiracı-üstü, append-only).
        // Kiracı ekranından değiştirilemez; yeni değer İLERİ tarihle sistem ucundan eklenir.
        S("YasalParametre", GerekceAnahtarlari.Sistem, "api/v1/sistem/yasal-parametreler"),
        // ── PAKET P-28 (F-4 kiracı etiketli telemetri · R-2) ─────────────
        // TELEMETRİ ÖLÇÜMÜ ölçüm aracının kendi ürettiği TÜRETİLMİŞ satırdır:
        // kullanıcı girmez, elle düzeltmenin anlamı yoktur ve düzeltilebilir
        // olsaydı F-8 birim ekonomi paydası "ayarlanabilir" hâle gelirdi.
        // Doğru düzeltme yolu ölçümü DÜZELTMEK değil, yeniden ÖLÇMEKTİR.
        S("TelemetriOlcumu", GerekceAnahtarlari.Turetilmis),
        // DESTEK DAKİKASI ELLE girilen bir ölçüm kalemidir → yanlış girilen
        // dakika birim ekonomiyi bozar, bu yüzden DÜZELTİLEBİLİR olmak
        // zorundadır. Hard delete YOKTUR: DELETE ucu pasife alır, PATCH
        // aktif=true ile geri döndürür. Yol operatör (sistem anahtarı) ucudur.
        D("DestekDakikasi", "api/v1/sistem/kiracilar/{id}/destek-dakikasi/{kayitId}"),
        D("Sube", "api/v1/subeler/{id}"),
        D("Kullanici", "api/v1/kullanicilar/{id}"),
        D("Cari", "api/v1/cari/{id}"),
        D("Urun", "api/v1/urunler/{id}"),
        D("Stok", "api/v1/urunler/{id}/stok/{subeId}"),
        D("Kasa", "api/v1/kasalar/{id}"),
        D("BankaHesap", "api/v1/bankalar/{id}"),
        // PAKET P-19 (Y-12): gider kategorisi DÜZENLENEBİLİR (PATCH + pasife alma
        // + aktife alma). Hard DELETE YOKTUR: geçmiş gider satırlarının sınıfı
        // yerinde kalmalıdır.
        D("GiderKategorisi", "api/v1/gider-kategorileri/{id}"),
        // ── PAKET 31 — POS ALACAK DEFTERİ ────────────────────────────────
        // Cihaz ve taksit oranı DÜZENLENEBİLİR (PATCH + pasife alma).
        D("PosCihaz", "api/v1/pos/cihazlar/{id}"),
        D("PosTaksitOran", "api/v1/pos/cihazlar/{id}/taksit-oranlari/{oranId}"),
        // pos_tahsilatlari satırı SATIŞTAN TÜRETİLİR: elle değiştirilemez.
        // Düzeltme yolu chargeback/iptal ucu ya da ilgili satışın iadesidir.
        S("PosTahsilat", GerekceAnahtarlari.Defter, "api/v1/pos/tahsilatlar/{id}/iptal"),
        // Eşleşme APPEND-ONLY (ADR-0004): silinmez, gerekçeyle geri alınır.
        S("PosYatmaEslesme", GerekceAnahtarlari.Defter, "api/v1/pos/eslesmeler/{id}/geri-al"),
        D("FiyatListesi", "api/v1/fiyat/listeler/{id}"),
        D("Kampanya", "api/v1/fiyat/kampanyalar/{id}"),
        // Paket 49 — liste × ürün geçersiz kılması: PATCH ile düzeltilir, aktif=false
        // ile pasife alınır (silinmez; "fiyatı kim değiştirdi" izi korunur).
        D("FiyatListesiUrunFiyati", "api/v1/fiyat/listeler/{id}/urun-fiyatlari/{urunId}"),
        D("UrunKategorisi", "api/v1/urun-kategorileri/{id}"),
        D("RolYetki", "api/v1/yetkiler"),
        new("Belge", Politikalar.Duzenlenebilir, GerekceAnahtarlari.Belge,
            "api/v1/belgeler/{belgeId}"),

        // Defter — yerinde düzenleme YOK (VUK md.219, ADR-0004).
        T("Hareket", GerekceAnahtarlari.Defter),
        T("KasaHareket", GerekceAnahtarlari.Defter),
        T("BankaHareket", GerekceAnahtarlari.Defter),
        // ── PAKET P-23 (R-17) — ÖDEME KALEMİ ─────────────────────────────
        // Kalem FİŞTEN TÜRETİLİR (OdemeSepeti.Coz doğrulamasından geçmiş
        // bacak); elle düzenlenseydi fişin kalem toplamı fiş toplamından
        // ayrılır ve bakiye saptırılırdı. Finansal varlıktır: hard DELETE
        // YOKTUR. Düzeltme yolu STORNO'dur — storno motoru kalemi pasife alır
        // ve hükümsüz kılan hareketi satıra yazar (StornoServisi).
        T("OdemeKalemi", GerekceAnahtarlari.Defter),
        // ── PAKET P-26 (Y-34 · Y-35) — TEDARİK ZİNCİRİ ───────────────────
        // Satınalma siparişi bir SÖZDÜR, defter satırı değildir: TASLAK'ken
        // serbestçe düzenlenir, onaylandıktan sonra kalan kalem mal kabulüyle
        // kapanır ya da sipariş GEREKÇELİ İPTAL edilir. Hard DELETE YOKTUR.
        D("SatinalmaSiparisi", "api/v1/satinalma/{id}"),
        // Kalem başlığın PATCH'i ile (sil+yaz) düzenlenir; kendi ucu yoktur —
        // ikinci bir kalem ucu açmak, kabul edilmiş adedi elle değiştirilebilir
        // kılardı (mal kabul dışında kimse kabul adedine dokunamaz).
        D("SatinalmaKalemi", "api/v1/satinalma/{id}"),
        // İrsaliye sevkin BELGESİDİR: açıklama/hesap kodu/tarih düzeltilir,
        // kalem değişmez; vazgeçilirse GEREKÇELİ İPTAL edilir. Kalemleri
        // deftere bağlanmışsa (mal kabul) iptal REDDEDİLİR — düzeltme yolu
        // alış iadesi / stornodur.
        D("Irsaliye", "api/v1/irsaliyeler/{id}"),
        D("IrsaliyeKalemi", "api/v1/irsaliyeler/{id}"),
        // UX-1-C3 doğrulama düzeltmesi: pusulanın dayandığı hareket türü
        // HURDA_ALIS, StornoServisi.ErtelenenTurler icindedir — yani bu kaydın
        // ters kaydı (storno) SUNUCUDA YOKTUR. STORNO damgası kullanıcıya
        // olmayan bir yol vaat ediyordu; SALT_OKUNUR + STORNO_YOK gerçeğe uyar.
        // PAKET S-3b: pusula artık gerekçeli olarak İPTAL edilebilir (belge
        // silinmez, "İPTAL" damgasıyla saklanır ve yerine yenisi düzenlenir).
        // ⚠ İptal yalnız BELGEYİ hükümsüz kılar; HURDA_ALIS hareketinin stok ve
        // maliyet bacağı ErtelenenTurler gereği DEĞİŞMEZ — metin bunu söyler.
        T("GiderPusulasi", GerekceAnahtarlari.Pusula, "api/v1/gider-pusulalari/{id}/iptal"),
        T("Fatura", GerekceAnahtarlari.Fatura),
        // PAKET YASAL-4 / Y4-B — stopaj ödemesi APPEND-ONLY: kasa çıkışının belgesidir,
        // yerinde düzenlenmez. Düzeltme yolu kasa satırının STORNOSU (48-A motoru) —
        // "geri al" ucu; satır silinmez, "geri alındı" görünür.
        T("StopajOdemesi", GerekceAnahtarlari.Defter, "api/v1/stopaj/odemeler/{id}/geri-al"),

        // Fatura kalemi BELGE parçasıdır: basılmış belgenin satırı yerinde
        // düzeltilmez (VUK md.253 — içerik saklama süresince sabit). Düzeltme
        // yolu faturanın kendisidir: durum IPTAL'e alınır ve yeni fatura kesilir.
        S("FaturaKalemi", GerekceAnahtarlari.Fatura, "api/v1/faturalar/{id}/durum"),

        // Değişmezler — gerekçesi yazılı.
        S("DenetimKaydi", GerekceAnahtarlari.Denetim),
        S("MasakKimlikTespiti", GerekceAnahtarlari.Masak),
        S("MasakBeyani", GerekceAnahtarlari.Masak),
        S("MasakSupheliBildirim", GerekceAnahtarlari.Masak),
        // PAKET YASAL-2 (TB-16) — nakit sınırı aşımı defterden TÜRETİLİR (tutar/gün elle
        // değişmez); tek düzeltme yolu kaydı gerekçeyle KAPATMAKTIR (bildirildi / yanlış kayıt).
        S("TevsikIhlali", GerekceAnahtarlari.Turetilmis, "api/v1/tevsik/ihlaller/{id}/bildirildi"),
        // PAKET YASAL-3 (§ 4.3) — saklama bekletmesi: bitiş/gerekçe PATCH ile düzeltilir,
        // erken bitirme "kaldır" damgasıdır (satır silinmez).
        D("SaklamaBekletme", "api/v1/masak/saklama-bekletme/{id}"),
        // PAKET YASAL-3 (§ 4.4) — resmî yaptırım listesi KİRACI-ÜSTÜDÜR; kiracı ekranından
        // değiştirilemez, ByGatech sistem ucundan günceller (karar kaldırılırsa damga).
        S("YaptirimKaydi", GerekceAnahtarlari.Sistem, "api/v1/sistem/yaptirim-listesi"),
        // Eşleşme: MASAK bildirim kapatması / "aynı kişi değil" PATCH ile; kilit elle açılmaz.
        D("YaptirimEslesmesi", "api/v1/masak/yaptirim-eslesmeleri/{id}"),
        // PAKET 40-A — risk profili APPEND-ONLY denetim kaydıdır: "o gün hangi
        // skorla işlem yaptın" sorusu denetimde sorulur. Düzeltme yolu satırı
        // değiştirmek DEĞİL, yeni satır doğuran GEREKÇELİ elle ezmedir.
        S("CariRiskProfili", GerekceAnahtarlari.Masak, "api/v1/masak/risk/{cariId}/elle"),
        S("DisaAktarimTalebi", GerekceAnahtarlari.DisaAktarim),

        // ── Paket 47 Faz A — veri içe aktarımı ───────────────────────────────
        // Oturum: kuru prova aşamasında gerekçe ve politika PATCH ile düzeltilir,
        // "pasife alma" karşılığı IPTAL ucudur. Uygulanmış oturum denetim izinin
        // parçasıdır ve sunucu düzenlemeyi 409 ile reddeder.
        D("IceAktarimOturumu", "api/v1/ice-aktarim/{id}"),
        // Satır: dosyadaki bir satırın SONUCUDUR — kanıt kaydıdır, düzeltilmez.
        // Doğru yol dosyayı düzeltip YENİ bir kuru prova açmaktır.
        S("IceAktarimSatiri", GerekceAnahtarlari.IceAktarim, "api/v1/ice-aktarim/prova"),

        // ── Paket 32 Faz A — banka ekstresi içe aktarımı ─────────────────────
        // İthalat başlığı: SAHNE kaydıdır, defter değildir. "Pasife alma"
        // karşılığı satırların ATLANDI yapılmasıdır; başlık silinmez, çünkü
        // üretilen banka hareketlerinin kaynağı odur.
        S("EkstreIthalati", GerekceAnahtarlari.IceAktarim, "api/v1/bankalar/ekstre/satir/{id}"),
        // Satır: ONAYLANMADAN ÖNCE düzenlenebilir (cari bağla/kaldır, atla,
        // atlamayı geri al). Onaylandıktan SONRA sunucu 409 döner; o aşamada
        // doğru yol, üretilen banka hareketinin storno/düzeltme fişidir.
        D("EkstreSatiri", "api/v1/bankalar/ekstre/satir/{id}"),
        // Cari-IBAN bağı — M7a (kurucu 17.10: "müşteri bilgilerine bir IBAN listesi gelsin"): müşteri
        // kartından eklenir/kaldırılır. "Bir IBAN tek cariye" kuralı TEK yerde: (tenant, iban_hash)
        // benzersiz indeksi; uç da ekstre öğrenmesi de aynı hash'e bakar.
        D("CariIban", "api/v1/cari/{id}/ibanlar/{ibanId}"),
        // M4 (16.10) ürün kuralı: kuyumcunun ayarı, serbestçe düzenlenir (pasife alma = Aktif).
        D("UrunKurali", "api/v1/urun-kurallari/{id}"),
        // M9: alış işçilik belgesi — alışı değiştirmez, belge bilgisi düzeltilebilir.
        D("AlisIscilikBelgesi", "api/v1/alis-iscilik/{alisHareketId}"),
        // D1-03: bekleyen fiş — yerinde düzenlenmez; tezgâha geri alınır (…/{id}/al), orada düzeltilip
        // kaydedilir ya da yeniden bekletilir. Atmak için …/{id}/sil.
        S("BekleyenFis", GerekceAnahtarlari.Sistem, "api/v1/bekleyen-fisler/{id}/al"),
        // D1-19: KVKK aydınlatma metni — PUT yeni sürüm yazar (eski sürüm ispat için kalır).
        D("KvkkMetni", "api/v1/kvkk/metin"),
        // D1-19: müşteri KVKK beyanı — eklenerek tutulur; geri alma yeni satırdır (ispat yükü).
        S("KvkkKaydi", GerekceAnahtarlari.Denetim, "api/v1/cari/{cariId}/kvkk"),
        // M7: demo bankasının gelen havalesi — sağlayıcı kaydıdır; düzeltme banka hareketinin stornosuyla.
        S("SahteBankaHareketi", GerekceAnahtarlari.Sistem),
        // 17.10 mahsup (ödemenin hangi borçlu satışa sayıldığı): append-only; düzeltme = ödemenin stornosu.
        S("CariMahsup", GerekceAnahtarlari.Defter, "api/v1/bankalar/hareketler/{hareketId}/karar/geri-al"),
        S("GunKapanisi", GerekceAnahtarlari.Kapanis),
        S("SayimOturumu", GerekceAnahtarlari.Sayim),
        S("SayimSatiri", GerekceAnahtarlari.Sayim),
        S("KurSnapshot", GerekceAnahtarlari.Sistem),
        // KUR KAYNAKLARI (17.10): has bedeli gün kapanışı faturanın DAYANAĞIDIR — bir faturada kullanılınca
        // değişmez (409). Kullanılmamışsa aynı gün yeniden girilerek düzeltilir (POST /kur/kapanis, denetim izli).
        S("OzelMatrahKapanisi", GerekceAnahtarlari.Fatura, "api/v1/kur/kapanis"),
        S("RefreshToken", GerekceAnahtarlari.Sistem),
        // PAKET AUTH-SIFIRLA: "Parolamı unuttum" jetonu — sistem üretir, sistem tüketir
        // (tek kullanım, 30 dk, yeni istek eskiyi iptal eder). Düzeltme = yeni bağlantı iste.
        S("ParolaSifirlamaJetonu", GerekceAnahtarlari.Sistem),
        S("StokPartisi", GerekceAnahtarlari.Sistem),
        S("StokPartiTuketimi", GerekceAnahtarlari.Sistem),
        S("CariBakiye", GerekceAnahtarlari.Turetilmis, "api/v1/cari/{id}/duzeltme"),

        // Paket 45 — iki aşamalı giriş. MFA kaydı YERİNDE DÜZENLENMEZ: gizin
        // değiştirilebilir olması, çalınan bir oturumun ikinci faktörü kendi
        // telefonuna taşımasına izin verirdi. Düzeltme yolu kapatıp yeniden
        // kurmaktır (parola + geçerli kod ister).
        S("KullaniciMfa", GerekceAnahtarlari.Sistem, "api/v1/auth/mfa/kaldir"),
        // Kurtarma kodu tek kullanımlıktır ve düzenlenmez; yenileme yolu kodların
        // TAMAMINI değiştirir (kısmi düzeltme diye bir şey yoktur).
        S("MfaKurtarmaKodu", GerekceAnahtarlari.Sistem, "api/v1/auth/mfa/kurtarma-yenile"),

        // UX-1 Faz B: PATCH /reyonlar/{id} + DELETE (pasife alma) AÇILDI →
        // damga UC_YOK'tan DUZENLENEBILIR'e döndü (şubesi hariç: reyonun şubesi
        // hiç değişmez, DTO'da SubeId alanı YOKTUR).
        D("Reyon", "api/v1/reyonlar/{id}"),

        // ── Paket 25 Faz A — reyon yerleşimi ─────────────────────────────────
        // DÜZELTME YOLU VARDIR ama YERİNDE DEĞİLDİR: yanlış reyona konan mal
        // TERS HAREKETLE düzeltilir — taşıma (reyonlar/tasima) ya da atanmamışa
        // geri alma (reyonlar/geri-al). Satıra PATCH açılmaması bilinçlidir:
        // `adet` elle yazılan bir değer değil, yerleştirme/taşıma hareketlerinin
        // SONUCUDUR; doğrudan yazmak izsiz düzeltme olurdu (iz DenetimKaydi'nda).
        // Bu yüzden damga STORNO'dur (ters kayıt), DUZENLENEBILIR değil.
        T("StokYerlesimi", GerekceAnahtarlari.Turetilmis, "api/v1/reyonlar/tasima")
            with { AlternatifYol = "api/v1/reyonlar/geri-al" },

        // ── Paket 6 Faz A — bildirim + zamanlanmış iş çatısı ─────────────────
        // Bildirim KAPATILIR ama DÜZENLENMEZ: kapatma "kim, ne zaman, hangi
        // notla kapattı" damgasıdır; metni sonradan değiştirilseydi damganın
        // altı doldurulmuş olurdu. Koşul sürüyorsa iş yeni bildirim üretir.
        S("Bildirim", GerekceAnahtarlari.Sistem, "api/v1/bildirimler/{id}/kapat"),
        // İş çalışması bir ÖLÇÜM kaydıdır (ne zaman koştu, kaç kayıt incelendi).
        // Elle düzeltilebilseydi "iş koştu mu" sorusunun cevabı güvenilmez olurdu.
        // Yeniden koşturma yolu vardır; satır DÜZELTİLMEZ.
        S("IsCalismasi", GerekceAnahtarlari.Sistem, "api/v1/isler/{kod}/calistir"),

        // ── Paket 3 Faz A — sipariş çatısı ──────────────────────────────────
        // Sipariş başlığı DÜZENLENEBİLİR — ama yalnız TASLAK durumunda; sonraki
        // durumlarda sunucu 400 verir ve ekran DegismezIpucu çizer. Harita
        // VARLIK bazlıdır, durum bazlı değil; durum kapısı uçtadır.
        D("Siparis", "api/v1/siparisler/{id}"),
        // Kalem kimliği DIŞARIDAN gelmez: kalem kümesi siparişin PATCH ucundan
        // tümüyle değiştirilir (sil + yaz). Tek tek kalem ucu AÇILMAZ.
        S("SiparisKalemi", GerekceAnahtarlari.Sistem, "api/v1/siparisler/{id}"),
        // Geçiş izi APPEND-ONLY: KuyumcuDbContext SaveChanges sırasında
        // UPDATE/DELETE denemesini istisna ile reddeder. Düzeltme yolu yeni bir
        // geçiştir (gerekçesiyle), satırın kendisi değil.
        S("SiparisGecisi", GerekceAnahtarlari.Denetim, "api/v1/siparisler/{id}/gecis"),

        // ── Paket 19 Faz A — vezne modeli ───────────────────────────────────
        // Vezne fişi ACIK durumdayken PATCH ile düzeltilir; vezneye gönderildikten
        // sonra tutarı damgalıdır ve yalnız gerekçeli İPTAL yolu açıktır.
        D("VezneFisi", "api/v1/vezne/fisler/{id}"),
        // Kalem kimliği DIŞARIDAN gelmez: kalem kümesi fişin PATCH ucundan
        // tümüyle değiştirilir (sil + yaz). Tek tek kalem ucu AÇILMAZ.
        S("VezneFisKalemi", GerekceAnahtarlari.Sistem, "api/v1/vezne/fisler/{id}"),
        // Kiracı ayarı: anahtarın kendisi sistemindir, DEĞERİ kendi ucundan
        // değişir (vezne ayrımı: PATCH /admin/vezne — SAHİP).
        D("KiraciAyari", "api/v1/admin/vezne"),

        // ── Paket T-2 Faz A — kuruluş sihirbazı + açılış izi ─────────────────
        // Kurulum durumu DÜZENLENEBİLİR: adım atlama/geri dönme tercihi PATCH
        // ile değişir. Mühürlendikten sonra sunucu 409 verir — damga VARLIK
        // bazlıdır, durum kapısı uçtadır (Siparis ile aynı desen).
        D("KurulumDurumu", "api/v1/kurulum/durum"),

        // ── Paket 29 Faz A — kıymetli evrak (çek/senet portföyü) ────────────
        // Evrak kartı DÜZENLENEBİLİR — ama yalnız PORTFOYDE durumunda; sonraki
        // durumlarda sunucu 400 verir ve ekran DegismezIpucu çizer (Siparis ile
        // aynı desen: harita VARLIK bazlıdır, durum kapısı uçtadır).
        // ⚠ Tutar ve kaynak cari bu uçtan DA değişmez: ikisi de deftere yazılmış
        //   bir borç-kapama satırını doğurdu; düzeltme yolu karşılıksız ya da
        //   giriş hareketinin stornosudur.
        D("KiymetliEvrak", "api/v1/evraklar/{id}"),

        // ── PAKET P-21 (Y-23) — REZERVASYON ─────────────────────────────────
        // SÖZLEŞME-DoD: PATCH (gönderilmeyen alan korunur) + pasife alma (iptal)
        // + [RolGerekli] + otomatik denetim kaydı ile DOĞDU. Hard DELETE YOKTUR:
        // "bu mal kime, neden ayrılmıştı ve niye iptal edildi" izi kalır.
        // Durum kapısı uçtadır (Siparis ile aynı desen): yalnız AKTİF düzenlenir.
        D("Rezervasyon", "api/v1/islemler/rezervler/{id}"),

        // ── PAKET P-22 (B-55) — POS KASA GÖÇ TUTANAĞI ───────────────────────
        // APPEND-ONLY: "cihazın kasası ne zaman, kim tarafından, neden taşındı"
        // sorusunun cevabıdır. Değiştirilebilseydi, hangi tahsilatın hangi
        // kasaya düştüğü sonradan yazılabilir olurdu (denetim kaydıyla aynı
        // gerekçe). Düzeltme yolu YENİ bir göçtür — tutanağın kendisi değil.
        S("PosKasaGocu", GerekceAnahtarlari.Denetim, "api/v1/pos/cihazlar/{id}/kasa-goc"),

        // ── PAKET P-37 — KULLANICI TERCİHİ (P-36b kapanışında haritaya işlendi) ──
        // Menü favorisi / menü sırası / panel düzeni. İŞ VERİSİ DEĞİLDİR, hiçbir
        // deftere dokunmaz: silinince kullanıcı varsayılan düzene döner, tek bir
        // tutar değişmez. Bu yüzden PATCH (api/v1/tercihler/benim) ve hard DELETE
        // (…/benim/{anahtar}) ikisi de SERBESTTİR — K-UX-2'nin defter yasağı
        // buraya değmez. Uç KULLANICIYA bağlıdır ("benim"): bir kullanıcı
        // başkasının menüsünü düzenleyemez.
        D("KullaniciTercihi", "api/v1/tercihler/benim"),
        // Açılış kalemi bir DEFTER İZİDİR: yerinde düzeltilmez. Mühürlenmeden
        // önce oturum GERİ ALINIR (ters ACILIS kaydı), mühürden sonra düzeltme
        // yolu normal defter yoludur (cari düzeltme fişi).
        T("AcilisKalemi", GerekceAnahtarlari.Defter, "api/v1/kurulum/acilis/geri-al")
            with { AlternatifYol = "api/v1/cari/{id}/duzeltme" },

        // ── Paket T-3 Faz A — yedek otomasyonu ───────────────────────────────
        // Yedek KOŞUSU bir ÖLÇÜMDÜR (ne zaman koştu, kaç bayt, sağlaması ne):
        // elle düzeltilebilseydi "yedeğimiz var" iddiasının altı boşalırdı.
        // Düzeltme yolu yeni bir koşudur — satırın kendisi değil.
        S("YedekKaydi", GerekceAnahtarlari.Sistem, "api/v1/yedekler/talep"),
        // Tatbikat raporu geri-yükleme provasının KANITIDIR; denetim kaydı gibi
        // değişmezdir. Yeniden koşturma yolu aylık timer'dır (sunucuda).
        S("YedekTatbikati", GerekceAnahtarlari.Denetim),
        // Elle yedek TALEBİ düzenlenebilir: bekleyen talep PATCH ile İPTAL
        // edilir (hard DELETE yok — kim ne istedi izi korunur).
        D("YedekTalebi", "api/v1/yedekler/talep/{id}"),
    ];

    // ── Hareket türleri ──────────────────────────────────────────────────────
    // Sabitler.HareketTurleri'nin TAMAMI burada olmalıdır ve HİÇBİRİ
    // DUZENLENEBILIR olamaz (K-UX-2 / VUK md.219).
    private static readonly VarlikPolitikasi[] HareketListesi =
    [
        T("SATIS", GerekceAnahtarlari.Defter, "api/v1/islemler/satis-iade"),
        T("ALIS", GerekceAnahtarlari.Defter, "api/v1/islemler/alis-iade"),
        T("CARI_DUZELTME", GerekceAnahtarlari.Defter, "api/v1/cari/{id}/duzeltme"),
        new("VERESIYE", Politikalar.Storno, GerekceAnahtarlari.Defter,
            "api/v1/islemler/{hareketId}/storno", "api/v1/cari/{id}/duzeltme"),
        new("TAHSILAT", Politikalar.Storno, GerekceAnahtarlari.Defter,
            "api/v1/islemler/{hareketId}/storno", "api/v1/cari/{id}/duzeltme"),

        // PAKET 48 Faz A — ters kayıt motoru GELDİ. Beyaz listedeki türlerin
        // HedefUc'u artık storno ucudur (yol dizgesi; istemci dinamik istek
        // KURMAZ — Mehmet K8). Stok/maliyet partisi bacağı olan türler
        // (TAKAS, HURDA_ALIS, TRANSFER) motorun beyaz listesinde DEĞİLDİR ve
        // bu yüzden damgaları STORNO_YOK olarak KALIR — "sunucuda uç var,
        // ekranda düğme yok"un tersi de kabul edilmez (UX-S25).
        T("TAKAS", GerekceAnahtarlari.StornoYok),
        T("HURDA_ALIS", GerekceAnahtarlari.StornoYok),
        // PAKET P-20 (Y-10) — HURDA SATIŞI / RAMAT ÇIKIŞI.
        // ⚠ STORNO_YOK ve bu HURDA_ALIS ile PARİTEDİR: ters kayıt motoru
        //   (StornoServisi.StornolanabilirTurler) bu türü tanımaz, çünkü ters
        //   kaydın cari etkisi türe göre AÇIK eşlemeden okunur ve kısmi tahsilatlı
        //   bir hurda satışında o eşleme tutmaz (tahsil edilmeyen kısım cariye
        //   yazılır, TL bacağının TAMAMI değil). "Uç var, düğme yok"un TERSİ de
        //   kabul edilmez (UX-S25): olmayan bir storno ucu buraya YAZILMAZ.
        //   Ekran bunun yerine DegismezIpucu ile gerekçeyi gösterir.
        //   DEVREDİLDİ: storno motorunun hurda hattına açılması → P-21/48 dilimi.
        T("HURDA_SATIS", GerekceAnahtarlari.StornoYok),
        // PAKET P-20b (K-13) — TAHLİL / MUTABAKAT FARKI.
        // ⚠ HURDA_SATIS'tan FARKLI olarak bu satırın GERÇEK bir düzeltme yolu
        //   VARDIR ve aşağıda yazılıdır: tahlil raporu sonradan düzelirse
        //   satırın üzerine yazılmaz, aynı hurda satışına İKİNCİ bir fark
        //   bacağı eklenir (defter append-only kalır, toplam fark bacakların
        //   toplamıdır). "Sunucuda uç var, ekranda düğme yok" olmaması için
        //   ekranda da düzeltme çekmecesi vardır (UX-S25).
        //   Hard DELETE YOKTUR: finansal/has bacağı olan satır silinmez.
        T(Data.TahlilFarkiFabrikasi.Tur, GerekceAnahtarlari.Defter,
            "api/v1/islemler/hurda-satis/{hareketId}/tahlil-duzeltme"),
        T("EMANET_GIRIS", GerekceAnahtarlari.Defter, "api/v1/islemler/{hareketId}/storno"),
        T("EMANET_CIKIS", GerekceAnahtarlari.Defter, "api/v1/islemler/{hareketId}/storno"),
        T("TRANSFER", GerekceAnahtarlari.StornoYok),
        T("SAYIM_DUZELTME", GerekceAnahtarlari.StornoYok),
        T("ISCILIK", GerekceAnahtarlari.Defter, "api/v1/islemler/{hareketId}/storno"),
        T("SATIS_IADE", GerekceAnahtarlari.StornoYok),
        T("ALIS_IADE", GerekceAnahtarlari.StornoYok),

        // PAKET 27 — döviz defteri. StornoServisi beyaz listesinde DEĞİLLER
        // (ters kayıt döviz kasası bacağını da çevirmek zorunda; Faz A bunu
        // açmıyor). Düzeltme yolu VARDIR ve yazılıdır: ters yönlü döviz
        // düzeltme fişi — "sunucuda uç var, ekranda düğme yok" değil (UX-S25).
        T("DOVIZ_VERESIYE", GerekceAnahtarlari.Defter, "api/v1/cari/{id}/duzeltme"),
        T("DOVIZ_TAHSILAT", GerekceAnahtarlari.Defter, "api/v1/cari/{id}/duzeltme"),

        // ── PAKET 29 FAZ A — KIYMETLİ EVRAK DEFTER SATIRLARI ────────────────
        // ⚠ Hiçbiri StornoServisi beyaz listesinde DEĞİLDİR (StornoServisi.cs:71)
        //   ve bu bilinçlidir: ters kayıt evrak tablosunun DURUMUNU da çevirmek
        //   zorundadır; genel storno motoru onu bilmez. Bu yüzden burada VAR
        //   OLMAYAN bir storno ucu değil, VAR OLAN gerçek düzeltme yolu yazılıdır
        //   (Anayasa md.1 + UX-S25: "sunucuda uç var, ekranda düğme yok"un tersi
        //   de kabul edilmez — yazılan uç GERÇEKTEN çalışır).
        //
        // Evrak girişi: yanlış girilen evrak KARŞILIKSIZ işaretlenerek geri
        // alınır — borç tam tutarıyla yeniden doğar.
        T("EVRAK_GIRIS", GerekceAnahtarlari.Defter, "api/v1/evraklar/{id}/karsiliksiz"),
        // Karşılıksız kaydı: evrak sonradan ödendiyse TAHSİL yolu açıktır.
        T("EVRAK_KARSILIKSIZ", GerekceAnahtarlari.Defter, "api/v1/evraklar/{id}/tahsil-edildi"),
        // Ciro FAZ B'ye devredildi: bugün bu türde satır YAZILMAZ ve geri alma
        // yolu da YOKTUR. Tür listede belge değeri taşır (Entities.cs notu);
        // uydurma bir uç yazmak yerine yokluğu açıkça damgalanır.
        // ⚠ PAKET P-21 (Y-21) İLE DEĞİŞTİ: ciro FAZ B AÇILDI ve artık bu türde
        // satır YAZILIR. Geri alma yolu da GERÇEKTEN VARDIR: rücu ucu — ciro
        // edilen evrak karşılıksız çıkınca hem bizim borcumuz hem keşidecinin
        // borcu geri doğar. Eski damga (StornoYok) artık YANLIŞ olurdu.
        T("EVRAK_CIRO", GerekceAnahtarlari.Defter, "api/v1/evraklar/{id}/rucu"),
        // Rücu satırının düzeltme yolu: keşideci sonradan öderse TAHSİL ucu.
        // ⚠ StornoServisi beyaz listesinde DEĞİL (evrak durumunu da çevirmesi
        //   gerekirdi; genel motor onu bilmez) — var OLAN uç yazılıdır (UX-S25).
        T("EVRAK_RUCU", GerekceAnahtarlari.Defter, "api/v1/evraklar/{id}/tahsil-edildi"),

        // ── PAKET P-21 (Y-17) — AVANS HATTI ─────────────────────────────────
        // Avans tahsilinin GERÇEK geri alma yolu avans iadesidir (para geri
        // verilir, cari borcumuz kapanır). Uydurma bir storno ucu YAZILMADI.
        T("AVANS_ALINDI", GerekceAnahtarlari.Defter, "api/v1/islemler/avans/iade"),
        // Mahsup ve iade satırlarının ters kaydı bu sürümde YOKTUR: mahsubun
        // geri alınması satışın kendisini de çevirmeyi gerektirir (satış iadesi
        // yolu zaten vardır ve avans havuzu oradan yeniden doğmaz — DEVREDİLDİ:
        // "mahsup geri alma" → P-48 storno dilimi). Yokluğu açıkça damgalanır.
        T("AVANS_MAHSUP", GerekceAnahtarlari.StornoYok),
        T("AVANS_IADE", GerekceAnahtarlari.StornoYok),

        // ── PAKET P-21 (Y-20) — KONSİNYE ÇIKIŞ / İADE ───────────────────────
        // Çıkışın gerçek geri alma yolu İADEdir (mal geri gelir, stok artar).
        T("KONSINYE_CIKIS", GerekceAnahtarlari.Defter, "api/v1/islemler/konsinye-iade"),
        // İadenin geri alınması = yeni bir çıkış; ters kayıt ucu YOKTUR.
        T("KONSINYE_IADE", GerekceAnahtarlari.StornoYok),

        // ── PAKET T-2 Faz A — açılış devri ──────────────────────────────────
        // ⚠ Bertan'ın koşulu: "Defter|StornoYok" diye bir BİT BİRLEŞİMİ YOKTUR;
        //   tek anahtar seçilir. Burada Defter seçildi ÇÜNKÜ gerçek bir ters
        //   kayıt yolu VARDIR: mühürlenmemiş oturum /kurulum/acilis/geri-al ile
        //   ters ACILIS kaydı yazılarak geri alınır. Mühürden sonra o uç 409
        //   verir ve düzeltme yolu alternatiftir (cari düzeltme fişi).
        // ⚠ StornoServisi (48-A) beyaz listesinde DEĞİLDİR: açılışın stok/parti
        //   bacağı Faz B'de doğacak ve genel storno motoru onu çeviremez.
        //   "Sunucuda uç var, ekranda düğme yok"un tersi de kabul edilmez —
        //   bu yüzden var OLMAYAN uç değil, var OLAN geri-al ucu yazılıdır.
        new("ACILIS", Politikalar.Storno, GerekceAnahtarlari.Defter,
            "api/v1/kurulum/acilis/geri-al", "api/v1/cari/{id}/duzeltme"),
    ];

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-06 — EKRAN DÜZENLEME KALIBI İSTİSNA KÜTÜĞÜ (kimlik B-54)
    //
    // KURAL (PLAN § 2 P-06 DoD): yazma yapan HER sayfa ya mühürlü bir düzenleme
    // kalıbı taşır (SatirIciDuzenle · Cekmece · DuzenlenebilirListe) ya da
    // DegismezIpucu ile gerekçe gösterir. Bu kütük, üçüncü bir hâli — "gerekçesi
    // YAZILI istisna" — tek ve SAYILABİLİR yere toplar.
    //
    // ⚠ Anayasa md.1 + plan DoD: "gizleyerek sayaç düşürmek YASAK". Bu yüzden
    //   kütük bir CIRCIRDIR: denetim.sh'taki P06_ISTISNA_TAVAN yalnız AZALIR.
    //   Yeni bir sayfayı buraya yazmak sayacı kırar; kalıbı kurmak gerekir.
    // ⚠ Her satır ÜÇ ŞEY söyler: neden istisna, kullanıcı düzeltmeyi NEREDE
    //   yapıyor, ve borç hangi pakete devredildi (boşsa borç yok, kalıcı istisna).
    // ═════════════════════════════════════════════════════════════════════════

    /// <param name="Sayfa">Pages/ altındaki dosya adı.</param>
    /// <param name="Gerekce">Neden mühürlü kalıp yok (tek cümle, Türkçe).</param>
    /// <param name="DuzeltmeYeri">Kullanıcının düzeltmeyi yaptığı GERÇEK yer.</param>
    /// <param name="DevredilenPaket">Borç varsa devredildiği paket; yoksa null.</param>
    public sealed record EkranIstisnasi(
        string Sayfa,
        string Gerekce,
        string DuzeltmeYeri,
        string? DevredilenPaket = null);

    public static readonly EkranIstisnasi[] EkranIstisnalari =
    [
        new("Giris.razor",
            "Kimlik doğrulama yüzeyidir; burada bir KAYIT oluşmaz (oturum açma) "
            + "ya da oluşan kayıt başka ekranda düzenlenir (kayıt olma → Tenant).",
            "İşletme kimliği /isletme ekranından düzenlenir (PATCH /admin/tenant/kimlik)."),

        new("Panel.razor",
            "Salt okunur panodur; tek yazması bir AYAR tercihidir (kur kaynağı), "
            + "kayıt değildir — tercih aynı kontrolden geri alınır.",
            "Kur kaynağı seçicisi tercihi anında değiştirir; /kur ekranı geçmişi gösterir."),

        new("Fiyatlandirma.razor",
            "Sayfada GERÇEK düzenleme vardır (#fl-duzenle-{id}, #km-duzenle-{id} → PATCH) "
            + "ama mühürlü kalıp yerine kendi 'duzenlenenId' formunu kullanır; bu kalıp "
            + "P-06'DAN ÖNCE vardı ve tek dokunuş kuralı gereği bu pakette yeniden yazılmaz.",
            "Liste ve kampanya satırlarındaki Düzenle düğmesi aynı formu PATCH kipinde açar.",
            "P-17 (ortak çıktı/şablon dalgası)"),
    ];

    public static readonly IReadOnlyDictionary<string, VarlikPolitikasi> Varliklar =
        VarlikListesi.ToDictionary(v => v.Varlik, StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, VarlikPolitikasi> HareketTurleri =
        HareketListesi.ToDictionary(v => v.Varlik, StringComparer.Ordinal);
}

// ── Controller ───────────────────────────────────────────────────────────────

/// <summary>
/// Meta uçları — kiracıya özel veri taşımaz, yalnız sözleşme tarifi döner.
/// [Authorize] altında kalır (Mehmet K8): harita uç envanteridir, anonim
/// istemciye API yüzeyinin dökümünü vermeyiz.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/meta")]
[Kuyumcu.Api.Auth.HerkeseAcik("Sabit politika metni — kiracı verisi taşımaz")]
public sealed class MetaController : ControllerBase
{
    /// <summary>GET /api/v1/meta/duzeltme-politikalari — varlık → düzeltme yolu haritası</summary>
    [HttpGet("duzeltme-politikalari")]
    public object DuzeltmePolitikalari() => new
    {
        varliklar = DuzeltmePolitikasi.Varliklar.Values.OrderBy(v => v.Varlik, StringComparer.Ordinal),
        hareketTurleri = DuzeltmePolitikasi.HareketTurleri.Values.OrderBy(v => v.Varlik, StringComparer.Ordinal),
    };
}
