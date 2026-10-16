namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ORTAK-1 — ORTAK GÖRÜNÜM ARTIKLARI · BASİT MOD METİNLERİ (tam tur v3)
//
// Ölçüm (başsız Chrome, BASİT mod, K8 jargon): /uyum?sekme=risk "cari,
// cariler" (12 durum) · /isletme?sekme=subeler "Pasife Al" (7 durum).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Sozlukler.<Alan>.cs). Burada YALNIZ bu paketin yeni metinleri durur.
// ⚠ Basit metin PROFESYONEL metnin yerine GEÇMEZ: sayfa basit modda bunu okur,
//   profesyonel mod muhasebe dilini ("cari", "pasife al") korur (D10 · K8).
// ⚠ Aynı kavram tek ad (UX-S31): basit modda "cari" = "müşteri", "pasife al" =
//   "kullanımdan kaldır" — menü ve diğer basit metinlerle (BasitDilSozlugu) aynı.
// ⚠ Hiçbir mevzuat hükmü/eşik/oran bu dosyada yazılı DEĞİLDİR.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>ORTAK-1 basit mod metinleri (bir dilde). Konumsal kayıt: eksik dil = derleme hatası.</summary>
public sealed record OrtakGorunumMetinleri(
    // ── /uyum?sekme=risk ──
    string UyumPepSayisiBasit,
    string UyumProfilsizBasit,
    string UyumYuksekRisklilerBasit,
    string UyumYuksekRiskliYokBasit,
    string UyumProfilTarihceYokBasit,
    string UyumRiskListeSiniriBasit,
    // ── /isletme?sekme=subeler ──
    string IsletmeSubePasifeAlBasit,
    // ── /vezne (hakem turu 28.09: BASİT modda "vezne" kökü ve çekimleri çıkmaz) ──
    string VezneDurumVeznedeBasit,
    string VezneyeGonderBasit,
    string VeznelendiBasit,
    string VezneTakasEmanetYokBasit,
    string VezneToplamTahminiBasit,
    string VezneToplamDamgaliBasit,
    string VezneleDegismezBasit,
    string VezneleDegismezYoluBasit,
    // Kilit gerekçesi satırda katlı durur (zorunlu önce kuralı); iki modda da aynı etiket.
    string VezneKilitNeden,
    // ── Panel (kıyas devri 28.09): "Bekleyen iş" yükleyicisi ağ hatası/zaman aşımında
    // sonsuz dönmez, kutucuğun bandına bu metni yazar (UX-S28). ──
    string PanelBaglantiHatasi,
    // ── Hakem turu 29.09 (tasarım hakemi): BASİT jargon artıkları ve ham anahtar ──
    // /uyum?sekme=risk: ham yapılandırma anahtarı HİÇBİR modda görünmez; BASİT tek sade cümle.
    string UyumRiskAciklamaBasit,
    string UyumRiskKapaliBasit,
    string UyumRiskKapaliPro,
    // /isletme?sekme=subeler: "pasife alınamaz" notunun BASİT karşılığı.
    string IsletmeSonSubeBasit,
    // /urunler/agac: HUB'daki geliştirici notunun yerine PRO'da tek cümle (BASİT'te gizli).
    string UrunAgaciReyonSayimNotu,
    // /raporlar/kar-zarar BASİT: bacak başlıkları ve defter gramı kartı.
    string KarZararParaKariBasit,
    string KarZararAltinKariBasit,
    string KarZararDefterGramBasit,
    // /siparisler BASİT: "işlem uçları" yerine ekran adı.
    string SiparisDefterNotuBasit,
    // /raporlar/vade-takibi BASİT: "ufuk" sözcüğü yerine gün seçimi.
    string VadeUfukOtesiBasit,
    string VadeUfukOtesiAciklamaBasit,
    string VadeDonemdenBagimsizBasit);

/// <summary>ORTAK-1 sözlüğü: <c>Al(dil)</c> ile dil seçilir.</summary>
public static class OrtakGorunumSozlugu
{
    public static readonly OrtakGorunumMetinleri Tr = new(
        UyumPepSayisiBasit: "Siyasi nüfuz sahibi olduğunu beyan eden müşteri",
        UyumProfilsizBasit: "Profili hiç hesaplanmamış müşteri",
        UyumYuksekRisklilerBasit: "Yüksek riskli müşteriler",
        UyumYuksekRiskliYokBasit: "Yüksek riskli müşteri yok.",
        UyumProfilTarihceYokBasit: "Bu müşteri için henüz profil hesaplanmadı.",
        UyumRiskListeSiniriBasit: "Liste en çok {0} müşteri gösterir; daraltmak için müşteri seçin.",
        IsletmeSubePasifeAlBasit: "Kullanımdan kaldır",
        VezneDurumVeznedeBasit: "Kuyrukta",
        VezneyeGonderBasit: "Kasa kuyruğuna gönder",
        VeznelendiBasit: "Kasa kuyruğuna gönderildi",
        VezneTakasEmanetYokBasit: "Takas ve emanet kasa kuyruğuna fiş düşürmez: takas doğrudan işlem ekranından girilir, emanet giriş/çıkışında para el değiştirmez.",
        VezneToplamTahminiBasit: "Tahmini — kuyruğa gidince kesinleşir",
        VezneToplamDamgaliBasit: "Kesin — kuyruğa gidince sabitlendi",
        VezneleDegismezBasit: "Kasa kuyruğuna gönderilmiş fişin tutarı sabitlenmiştir: kasada o tutar beklediği için kalemleri, müşterisi ve açıklaması artık değiştirilmez.",
        VezneleDegismezYoluBasit: "Fişi gerekçesini yazarak iptal edin ve doğru kalemlerle yeni bir fiş açın.",
        VezneKilitNeden: "Neden değiştirilemez?",
        PanelBaglantiHatasi: "Sunucuya ulaşılamadı. Bağlantınızı kontrol edip sayfayı yenileyin.",
        UyumRiskAciklamaBasit: "Risk puanı yalnız bilgi içindir; hiçbir satışı durdurmaz.",
        UyumRiskKapaliBasit: "Risk kuralları şu an kapalı. Açmadan önce mali müşavirinize danışın.",
        UyumRiskKapaliPro: "Risk temelli tedbirler KAPALI. Skor hesaplanır ve gösterilir, hiçbir alanı zorunlu yapmaz. Açılması mali müşavir/uyum görevlisi teyidine bağlıdır.",
        IsletmeSonSubeBasit: "Tek şube kaldığı için kaldırılamaz — satış yapılacak şube kalmaz.",
        UrunAgaciReyonSayimNotu: "Reyon sayımı, varsayılan reyonu bu reyon olan ürünleri sayar; reyonda fiilen duran malı değil.",
        KarZararParaKariBasit: "Para kârı",
        KarZararAltinKariBasit: "Altın kârı (has gram)",
        KarZararDefterGramBasit: "Defterdeki {0} gramı",
        SiparisDefterNotuBasit: "Sipariş kaydı parayı ve stoğu değiştirmez. Tahsilatı ve teslimi Satış ekranından yapın.",
        VadeUfukOtesiBasit: "Daha ileri vadeli",
        VadeUfukOtesiAciklamaBasit: "Vadesi seçtiğiniz günlerden sonra gelen borç",
        VadeDonemdenBagimsizBasit: "(bugün itibarıyla — gün seçiminden bağımsız)");

    public static readonly OrtakGorunumMetinleri En = new(
        UyumPepSayisiBasit: "Customers who declared political exposure",
        UyumProfilsizBasit: "Customers never scored",
        UyumYuksekRisklilerBasit: "High-risk customers",
        UyumYuksekRiskliYokBasit: "No high-risk customers.",
        UyumProfilTarihceYokBasit: "No profile has been calculated for this customer yet.",
        UyumRiskListeSiniriBasit: "The list shows at most {0} customers; select a customer to narrow it down.",
        IsletmeSubePasifeAlBasit: "Retire",
        VezneDurumVeznedeBasit: "Queued",
        VezneyeGonderBasit: "Send to till queue",
        VeznelendiBasit: "Sent to till queue",
        VezneTakasEmanetYokBasit: "Trade-ins and safekeeping do not create a till-queue slip: trade-ins are entered on the transactions screen, and no money changes hands on safekeeping in/out.",
        VezneToplamTahminiBasit: "Estimated — fixed once queued",
        VezneToplamDamgaliBasit: "Final — fixed when queued",
        VezneleDegismezBasit: "A slip sent to the till queue has a fixed amount: since the till is waiting for that amount, its lines, customer and note can no longer be changed.",
        VezneleDegismezYoluBasit: "Cancel the slip with a reason and open a new slip with the correct lines.",
        VezneKilitNeden: "Why can't it be changed?",
        PanelBaglantiHatasi: "Could not reach the server. Check your connection and refresh the page.",
        UyumRiskAciklamaBasit: "The risk score is for information only; it never blocks a sale.",
        UyumRiskKapaliBasit: "Risk rules are currently off. Ask your accountant before turning them on.",
        UyumRiskKapaliPro: "Risk-based measures are OFF. The score is computed and displayed but makes no field mandatory. Enabling it requires sign-off from the accountant/compliance officer.",
        IsletmeSonSubeBasit: "The only branch cannot be retired — no branch would remain for sales.",
        UrunAgaciReyonSayimNotu: "A display-unit count covers products whose default display unit it is, not the goods physically placed there.",
        KarZararParaKariBasit: "Money profit",
        KarZararAltinKariBasit: "Gold profit (fine grams)",
        KarZararDefterGramBasit: "{0} grams in the books",
        SiparisDefterNotuBasit: "An order does not change money or stock. Take payment and hand over the goods on the Sales screen.",
        VadeUfukOtesiBasit: "Due later",
        VadeUfukOtesiAciklamaBasit: "Debt due after the days you selected",
        VadeDonemdenBagimsizBasit: "(as of today — independent of the day selection)");

    public static OrtakGorunumMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
