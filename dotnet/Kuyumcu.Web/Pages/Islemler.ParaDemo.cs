using System.Text.Json;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO — satış ekranı kilitleri ve takas satıcı gövdesi (madde 3, 4)
//
// (4) Peşin Müşteri + Veresiye: sunucu doğru reddediyordu (HTTP 400, SistemCarisi
//     .BakiyeYasakMesaji) ama Kaydet önceden kilitlenmiyordu; tezgâhtar ancak
//     düğmeye basınca öğreniyordu (demo satis-15). Artık düğme kilitli ve sebebi
//     yazılı (UX-S30). Sunucu kapısı AYNEN durur — istemci kilidi yalnız ayna.
// (3) Takas: müşteriden alınan eski altının satıcı kimliği (VUK md.234 · TBK
//     md.283) eksikse ya da satıcı esnaf muafiyetliyse (stopaj farkı bozar)
//     Kaydet kilitli; gövdeye satıcı bloğu konur; pusula no başarı bandına.
// ⚠ AYRI DOSYA NEDENİ: Islemler.razor 1.400 satır tavanındadır (S21FisYapiTestleri).
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private ParaDemoMetinleri PdKilit => ParaDemoSozlugu.Al(Dil.Dil);

    /// <summary>
    /// Peşin Müşteri seçiliyken fişte veresiye (borç) doğuracak ödeme var mı:
    /// tek ödemede tür VERESIYE, karma ödemede tutarı olan bir VERESIYE satırı.
    /// </summary>
    internal bool PesinVeresiyeVar =>
        tur == "SATIS" && PesinSecili
        && (karmaOdeme
            ? bacaklar.Any(b => b.Arac == "VERESIYE" && b.TutarTl > 0m)
            : odemeTuru == "VERESIYE");

    /// <summary>Takasta esnaf muafiyetli satıcı: sunucu stopaj farkı yüzünden reddeder (fail-closed ayna).</summary>
    internal bool TakasEsnafKilitli => tur == "TAKAS" && takas.Satici.SaticiTipi == "ESNAF_MUAF";

    /// <summary>Takasta satıcı kimliği kilidinin SEBEBİ (UX-S30) — yoksa null.</summary>
    internal string? TakasSaticiKilidi =>
        tur != "TAKAS" ? null
        : TakasEsnafKilitli ? Kuyumcu.Web.Bilesenler.Fis.FisTakasSatici.EsnafKilitMetni(Dil.Dil, Modu.BasitMi, Dil.T.Islem)
        // PARA-DEMO-2 (madde 8): sebep EKSİK ALANA göre güncellenir ("Eksik: TCKN, adres.").
        : !takas.Satici.SaticiTamam(Dil.T.Islem)
            ? Kuyumcu.Web.Bilesenler.Fis.FisTakasSatici.EksikMetni(takas.Satici, Dil.Dil, Modu.BasitMi, Dil.T.Islem)
              ?? (Modu.BasitMi ? Satis3Sozlugu.Al(Dil.Dil).SaticiKimligiSade : Dil.T.Islem.SaticiKimligiEksik)
        : null;

    /// <summary>TakasDto.Satici (HurdaSaticiDto) gövdesi — hurda alışıyla AYNI alanlar.</summary>
    private object TakasSaticiGovdesi() => new
    {
        saticiTipi = takas.Satici.SaticiTipi,
        adSoyad = string.IsNullOrWhiteSpace(takas.Satici.SaticiAd) ? null : takas.Satici.SaticiAd.Trim(),
        tckn = takas.Satici.TcknRakam.Length == 0 ? null : takas.Satici.TcknRakam,
        adres = string.IsNullOrWhiteSpace(takas.Satici.SaticiAdres) ? null : takas.Satici.SaticiAdres.Trim(),
        tedarikciFaturaNo = string.IsNullOrWhiteSpace(takas.Satici.TedarikciFaturaNo) ? null : takas.Satici.TedarikciFaturaNo.Trim(),
    };

    /// <summary>Takas yanıtındaki gider pusulası numarası (mükellefte pusula yok → null).</summary>
    private string? TakasPusulaNotu(JsonElement y) =>
        y.ValueKind == JsonValueKind.Object && y.TryGetProperty("giderPusulasi", out var gp)
        && gp.ValueKind == JsonValueKind.Object && gp.TryGetProperty("pusulaNo", out var no)
        && no.GetString() is { Length: > 0 } pn
            ? string.Format(PdKilit.TakasPusulaNotu, pn)
            : null;
}
