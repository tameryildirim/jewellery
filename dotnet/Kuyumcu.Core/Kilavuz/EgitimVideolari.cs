// ÜRETİLMİŞ DOSYA — dokumanlar/09-uygulama-kayitlari/egitim-videolari-2026-10-21/betik/kilavuz-bolum.py; elle düzenlenmez.
// Y-01 (21.10): ekran başına eğitim videosu (Kılavuz ekranı › Eğitim videoları); dosyalar wwwroot/video/egitim-<kimlik>.mp4.
namespace Kuyumcu.Core.Kilavuz;

/// <summary>Bir eğitim videosu: kimlik (dosya adı), başlık, alt başlık, süre (sn) ve bölüm başlangıçları (sn, başlık).</summary>
public sealed record EgitimVideosu(string Kimlik, string Baslik, string Alt, string BaslikEn, double SureSn, IReadOnlyList<(double Sn, string Baslik)> Bolumler)
{
    public string Dosya => $"/video/egitim-{Kimlik}.mp4";
}

public static class EgitimVideolari
{
    public static readonly IReadOnlyList<EgitimVideosu> Hepsi =
    [
        new("satis", "Satış", "Barkoddan fişe, dört adım", "Sales", 69.5, [(2.6, "Tezgah ekranı"), (15.6, "Barkodu okut"), (27.7, "Ödeme"), (42.0, "Kaydet ve yazdır"), (52.5, "Kısayollar")]),
        new("hurda", "Hurda alışı", "Eski altını al, hurda stoğunu izle", "Scrap purchase", 52.8, [(2.6, "Alış sekmesi"), (18.9, "Belge ve kimlik"), (34.9, "Hurda ve ramat")]),
        new("emanet", "Emanet", "Müşterinin bıraktığı altın, kayıt altında", "Safekeeping", 47.7, [(2.6, "Emanet listesi"), (16.3, "Emanet al"), (30.2, "Geri ver")]),
        new("banka", "Banka", "Gelen para, açık hesaba ve satışa bağlanır", "Bank", 43.9, [(2.6, "Banka hesapları"), (14.2, "Gelen havale kararı"), (27.3, "Hareketler ve ekstre")]),
        new("fatura", "Faturalar", "Fişten faturaya tek adım, e-belge arkada", "Invoices", 49.7, [(2.6, "Fatura listesi"), (12.7, "Fişten fatura kes"), (30.2, "e-Fatura giden kutusu")]),
        new("masak", "Uyum ve MASAK", "Kimlik tespiti, şüpheli işlem, nakit sınırı", "Compliance (MASAK)", 46.8, [(2.6, "Kimlik tespitleri"), (16.5, "Şüpheli işlemler"), (28.9, "Nakit sınırı")]),
        new("kasa-kapanis", "Kasa kapanışı", "Çekmeceyi say, farkı yaz, günü kilitle", "Register closing", 51.1, [(2.6, "Kasa ekranı"), (16.1, "Günü kapat"), (33.3, "Kapanış fişi")]),
        new("etiket", "Etiketler", "Barkodlu etiket elle yazılmaz", "Labels", 40.7, [(2.6, "Ürün seç"), (15.8, "Şablon ve adet"), (26.9, "Yazdır")]),
        new("rapor", "Raporlar", "Gün sonu için hesap makinesi gerekmez", "Reports", 52.6, [(2.6, "Rapor merkezi"), (13.4, "Kar zarar"), (23.2, "İşlem defteri"), (34.5, "Satış analizi")]),
    ];

    /// <summary>mm:ss (kültürden bağımsız).</summary>
    public static string Sure(double sn) { var t = (int)System.Math.Round(sn); return $"{t / 60}:{t % 60:00}"; }
}
