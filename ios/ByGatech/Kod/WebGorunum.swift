import SwiftUI
import WebKit

/// Web görünümünün durumu — SwiftUI ile WKWebView arasındaki köprü.
@MainActor
final class WebDurumu: ObservableObject {
    @Published var yukleniyor = true
    @Published var hata: String?
    @Published var yenidenDeneSayaci = 0
    /// Giriş formu ekranda mı (Face ID düğmesi için).
    @Published var girisFormuGorunuyor = false
    /// Kasada Face ID girişi kayıtlı mı.
    @Published var girisKayitli = GirisKasasi.kayitliMi
    /// Başarılı girişten sonra "Face ID ile giriş kaydedilsin mi?" sorusu.
    @Published var kaydetSorusu: GirisKasasi.Giris?
    @Published var bilgi: String?
    /// Son giriş denemesi (henüz doğrulanmadı).
    var adayGiris: GirisKasasi.Giris?
    /// Bu giriş Face ID ile dolduruldu → başarıda yeniden "kaydedilsin mi?" sorulmaz.
    var faceIdIleDolduruldu = false
    // Yerli kabuk durumu (web'den okunur): rota, seçili sekme, kur şeridi, kullanıcı kısaltması, dil, görünüm modu, giriş amblemi yeri.
    @Published var rota = "/"
    @Published var sekme: String?
    @Published var kur: String?
    @Published var kullanici: String?
    @Published var dil: String?
    @Published var mod: String?
    @Published var amblem: CGRect?
    /// Giriş/şifre ekranları: yerli çubuklar gizli, 3B elmas amblem yerine biner.
    var girisEkrani: Bool { girisFormuGorunuyor || rota.hasPrefix("/giris") || rota.hasPrefix("/sifre-sifirla") || rota.hasPrefix("/kayit") }
    weak var web: WKWebView?

    func yenidenDene() {
        hata = nil
        yukleniyor = true
        yenidenDeneSayaci += 1
    }

    /// Face ID → kasadan oku → formu doldur → gönder.
    func faceIdIleGiris() async {
        guard let g = await GirisKasasi.oku(), let web else { return }
        faceIdIleDolduruldu = true
        let js = "window.__bygatechGiris(\(Self.json(g.eposta)), \(Self.json(g.parola)))"
        _ = try? await web.evaluateJavaScript(js)
        Titresim.ver("secim")
    }

    // MARK: Yerli kabuk → web
    func git(_ s: Sekme) {
        web?.evaluateJavaScript("window.__bygatechGit(\(Self.json(s.id)), \(Self.json(s.yol)))")
        sekme = s.id
    }
    func tikla(_ id: String) { web?.evaluateJavaScript("window.__bygatechTikla(\(Self.json(id)))") }
    func dilSec(_ kod: String) { dil = kod; web?.evaluateJavaScript("window.__bygatechDil(\(Self.json(kod.uppercased())))") }
    func modSec(_ m: String) { mod = m; tikla(m == "basit" ? "yildiz-mod-basit" : "yildiz-mod-profesyonel") }

    /// Web'den kabuk durumunu okur (rota değişiminde ve periyodik).
    func durumuOku() {
        web?.evaluateJavaScript("window.__bygatechDurum ? window.__bygatechDurum() : null") { [weak self] sonuc, _ in
            guard let self, let metin = sonuc as? String, let veri = metin.data(using: .utf8),
                  let d = try? JSONSerialization.jsonObject(with: veri) as? [String: Any] else { return }
            Task { @MainActor in
                self.kur = d["kur"] as? String
                self.kullanici = (d["kullanici"] as? String).map { ad in
                    let p = ad.split(whereSeparator: { $0 == " " || $0 == "." }).filter { !$0.isEmpty }
                    return p.count >= 2 ? String(p[0].prefix(1) + p[1].prefix(1)).uppercased() : String(ad.prefix(2)).uppercased()
                }
                if let s = d["sekme"] as? String { self.sekme = s }
                if let m = d["mod"] as? String { self.mod = m }
                if let dl = d["dil"] as? String { self.dil = String(dl.prefix(2)).lowercased() }
                if let y = d["yol"] as? String { self.rota = y }
                if let r = d["amblem"] as? [Double], r.count == 4 { self.amblem = CGRect(x: r[0], y: r[1], width: r[2], height: r[3]) } else { self.amblem = nil }
            }
        }
    }

    func kaydetOnayla() {
        guard let g = kaydetSorusu else { return }
        kaydetSorusu = nil
        if GirisKasasi.kaydet(g) { girisKayitli = true; Titresim.ver("basari") }
        else { bilgi = "Face ID girişi kaydedilemedi." }
    }

    func kaydetVazgec() { kaydetSorusu = nil }

    func kasayiSil() {
        GirisKasasi.sil(); girisKayitli = false
    }

    static func json(_ s: String) -> String {
        String(data: try! JSONSerialization.data(withJSONObject: [s]), encoding: .utf8)!.dropFirst().dropLast().description
    }
}

/// Sunucudaki ByGatech'i açan WKWebView.
/// - Çerez/yerel depo kalıcı → giriş hatırlanır.
/// - Belge başında kabuk betiği: SF Pro, sistem yazı boyutu, titreşim, rota, giriş köprüsü.
/// - Kamera/mikrofon isteği uygulama iznine bağlanır; aşağı çekerek yenileme; geri/ileri hareketleri.
/// - Başka alan adları Safari'de açılır; kendi sunucumuz uygulama içinde kalır.
struct WebGorunum: UIViewRepresentable {
    @ObservedObject var durum: WebDurumu

    func makeCoordinator() -> Koordinator { Koordinator(durum: durum) }

    func makeUIView(context: Context) -> WKWebView {
        let ayar = WKWebViewConfiguration()
        ayar.websiteDataStore = .default()
        ayar.allowsInlineMediaPlayback = true
        ayar.mediaTypesRequiringUserActionForPlayback = []
        ayar.defaultWebpagePreferences.allowsContentJavaScript = true
        ayar.preferences.javaScriptCanOpenWindowsAutomatically = true

        let betik = WKUserScript(source: KabukBetigi.betik(olcek: Koordinator.yaziOlcegi),
                                 injectionTime: .atDocumentStart, forMainFrameOnly: true)
        ayar.userContentController.addUserScript(betik)
        ayar.userContentController.add(context.coordinator, name: "kabuk")

        let web = WKWebView(frame: .zero, configuration: ayar)
        web.navigationDelegate = context.coordinator
        web.uiDelegate = context.coordinator
        web.allowsBackForwardNavigationGestures = true
        web.isOpaque = false
        web.backgroundColor = UIColor(named: "ArkaPlan")
        web.scrollView.backgroundColor = UIColor(named: "ArkaPlan")
        web.scrollView.contentInsetAdjustmentBehavior = .automatic
        if let ua = web.value(forKey: "userAgent") as? String {
            web.customUserAgent = ua + " ByGatechiOS/1.0"
        }

        let yenile = UIRefreshControl()
        yenile.addTarget(context.coordinator, action: #selector(Koordinator.asagiCekildi(_:)), for: .valueChanged)
        web.scrollView.refreshControl = yenile

        // Kullanıcı iPhone yazı boyutunu değiştirince ölçek yenilenir.
        NotificationCenter.default.addObserver(context.coordinator, selector: #selector(Koordinator.yaziBoyutuDegisti),
                                               name: UIContentSizeCategory.didChangeNotification, object: nil)

        context.coordinator.web = web
        durum.web = web
        context.coordinator.zamanlayici = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: true) { [durum] _ in durum.durumuOku() }
        web.load(URLRequest(url: Ayarlar.sunucu))
        return web
    }

    func updateUIView(_ web: WKWebView, context: Context) {
        if context.coordinator.sonDeneme != durum.yenidenDeneSayaci {
            context.coordinator.sonDeneme = durum.yenidenDeneSayaci
            if web.url == nil || durum.hata != nil { web.load(URLRequest(url: Ayarlar.sunucu)) } else { web.reload() }
        }
    }

    static func dismantleUIView(_ web: WKWebView, coordinator: Koordinator) {
        coordinator.zamanlayici?.invalidate()
        web.configuration.userContentController.removeScriptMessageHandler(forName: "kabuk")
        NotificationCenter.default.removeObserver(coordinator)
    }

    final class Koordinator: NSObject, WKNavigationDelegate, WKUIDelegate, WKScriptMessageHandler {
        let durum: WebDurumu
        weak var web: WKWebView?
        var sonDeneme = 0
        var zamanlayici: Timer?
        #if DEBUG
        var testGirisiKullanildi = false
        #endif

        init(durum: WebDurumu) { self.durum = durum }

        @objc func asagiCekildi(_ kontrol: UIRefreshControl) {
            web?.reload()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.6) { kontrol.endRefreshing() }
        }

        @objc func yaziBoyutuDegisti() {
            web?.evaluateJavaScript("window.__bygatechOlcek && window.__bygatechOlcek(\(Self.yaziOlcegi))")
        }

        /// Sistem yazı boyutu (iPhone Ayarlar › Ekran ve Parlaklık › Metin Boyutu):
        /// gövde yazı stili 17 pt = 1,0; 0,9–1,25 arası; "Sistem yazı boyutu" kapalıysa 1,0.
        static var yaziOlcegi: Double {
            guard Tercihler.sistemYaziBoyutu else { return 1 }
            let punto = UIFont.preferredFont(forTextStyle: .body).pointSize
            return min(1.25, max(0.9, Double(punto) / 17))
        }

        // MARK: Kabuk köprüsü (web → iOS)
        func userContentController(_ controller: WKUserContentController, didReceive mesaj: WKScriptMessage) {
            guard let govde = mesaj.body as? [String: Any], let tur = govde["tur"] as? String else { return }
            let veri = govde["veri"]
            Task { @MainActor in
                switch tur {
                case "titresim":
                    Titresim.ver(veri as? String ?? "hafif")
                case "girisFormu":
                    durum.girisFormuGorunuyor = veri as? Bool ?? false
                    #if DEBUG
                    // Yalnız geliştirme derlemesi: simülatör kanıtı için ortamdan test girişi
                    // (BYGATECH_TEST_GIRIS="eposta<TAB>parola"); Release'e girmez.
                    if durum.girisFormuGorunuyor, !testGirisiKullanildi,
                       let t = ProcessInfo.processInfo.environment["BYGATECH_TEST_GIRIS"]?.split(separator: "\t", maxSplits: 1), t.count == 2 {
                        testGirisiKullanildi = true
                        _ = try? await web?.evaluateJavaScript("window.__bygatechGiris(\(WebDurumu.json(String(t[0]))), \(WebDurumu.json(String(t[1]))))")
                    }
                    #endif
                case "girisDenemesi":
                    if let d = veri as? [String: Any], let e = d["eposta"] as? String, let p = d["parola"] as? String {
                        durum.adayGiris = .init(eposta: e, parola: p)
                    }
                case "rota":
                    let yol = veri as? String ?? "/"
                    durum.rota = yol
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.4) { [durum] in durum.durumuOku() }
                    if let aday = durum.adayGiris, !yol.hasPrefix("/giris") {
                        durum.adayGiris = nil
                        let faceId = durum.faceIdIleDolduruldu; durum.faceIdIleDolduruldu = false
                        // Face ID ile girildiyse ya da aynı e-posta zaten kayıtlıysa yeniden sorulmaz
                        // (parola değiştiyse Face ID girişi düşer, kasa silinir, bir sonraki girişte sorulur).
                        if faceId { return }
                        if durum.girisKayitli, GirisKasasi.kayitliEposta?.lowercased() == aday.eposta.lowercased() { return }
                        durum.kaydetSorusu = aday
                    }
                case "girisBasarisiz":
                    if durum.girisKayitli {
                        durum.kasayiSil()
                        durum.bilgi = "Kayıtlı Face ID girişi kabul edilmedi ve silindi. Yeniden giriş yapınca tekrar kaydedebilirsiniz."
                        Titresim.ver("hata")
                    }
                default: break
                }
            }
        }

        // MARK: Yükleme durumu
        func webView(_ webView: WKWebView, didStartProvisionalNavigation navigation: WKNavigation!) {
            durum.yukleniyor = true
        }

        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            durum.yukleniyor = false
            durum.hata = nil
            for g in [0.8, 2.5, 6.0] { DispatchQueue.main.asyncAfter(deadline: .now() + g) { [durum] in durum.durumuOku() } }
            #if DEBUG
            // Yalnız geliştirme: BYGATECH_TEST_JS ortam değişkenindeki ifadeyi 4 sn sonra çalıştırıp stdout'a yazar.
            if let js = ProcessInfo.processInfo.environment["BYGATECH_TEST_JS"] {
                DispatchQueue.main.asyncAfter(deadline: .now() + 4) {
                    webView.evaluateJavaScript(js) { sonuc, hata in NSLog("TEST_JS: %@ %@", sonuc.map { "\($0)" } ?? "nil", hata.map { "HATA \($0)" } ?? "") }
                }
            }
            #endif
        }

        func webView(_ webView: WKWebView, didFailProvisionalNavigation navigation: WKNavigation!, withError error: Error) { hataGoster(error) }
        func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) { hataGoster(error) }

        private func hataGoster(_ error: Error) {
            let kod = (error as NSError).code
            if kod == NSURLErrorCancelled { return }
            durum.yukleniyor = false
            durum.hata = kod == NSURLErrorNotConnectedToInternet
                ? "İnternet bağlantısı yok. Bağlantıyı kontrol edip yeniden deneyin."
                : "Sunucuya ulaşılamadı. Birkaç saniye sonra yeniden deneyin."
            Titresim.ver("hata")
        }

        // MARK: Yönlendirme — kendi sunucumuz içeride, başkası Safari'de
        func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction,
                     decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
            guard let url = navigationAction.request.url else { decisionHandler(.allow); return }
            let dis = ["http", "https"].contains(url.scheme?.lowercased() ?? "") == false
            if dis || (navigationAction.navigationType == .linkActivated && !Ayarlar.kendiSunucumuzMu(url)) {
                UIApplication.shared.open(url)
                decisionHandler(.cancel)
                return
            }
            decisionHandler(.allow)
        }

        func webView(_ webView: WKWebView, createWebViewWith configuration: WKWebViewConfiguration,
                     for navigationAction: WKNavigationAction, windowFeatures: WKWindowFeatures) -> WKWebView? {
            if let url = navigationAction.request.url {
                if Ayarlar.kendiSunucumuzMu(url) { webView.load(URLRequest(url: url)) } else { UIApplication.shared.open(url) }
            }
            return nil
        }

        // MARK: Kamera / mikrofon
        func webView(_ webView: WKWebView, requestMediaCapturePermissionFor origin: WKSecurityOrigin,
                     initiatedByFrame frame: WKFrameInfo, type: WKMediaCaptureType,
                     decisionHandler: @escaping (WKPermissionDecision) -> Void) {
            decisionHandler(origin.host.lowercased() == Ayarlar.sunucu.host?.lowercased() ? .grant : .deny)
        }

        // MARK: JS uyarıları → yerel iOS pencereleri
        func webView(_ webView: WKWebView, runJavaScriptAlertPanelWithMessage message: String,
                     initiatedByFrame frame: WKFrameInfo, completionHandler: @escaping () -> Void) {
            uyari(message, dugmeler: [("Tamam", { completionHandler() })])
        }

        func webView(_ webView: WKWebView, runJavaScriptConfirmPanelWithMessage message: String,
                     initiatedByFrame frame: WKFrameInfo, completionHandler: @escaping (Bool) -> Void) {
            uyari(message, dugmeler: [("Vazgeç", { completionHandler(false) }), ("Tamam", { completionHandler(true) })])
        }

        func webView(_ webView: WKWebView, runJavaScriptTextInputPanelWithPrompt prompt: String,
                     defaultText: String?, initiatedByFrame frame: WKFrameInfo,
                     completionHandler: @escaping (String?) -> Void) {
            let pencere = UIAlertController(title: nil, message: prompt, preferredStyle: .alert)
            pencere.addTextField { $0.text = defaultText }
            pencere.addAction(UIAlertAction(title: "Vazgeç", style: .cancel) { _ in completionHandler(nil) })
            pencere.addAction(UIAlertAction(title: "Tamam", style: .default) { _ in completionHandler(pencere.textFields?.first?.text) })
            sun(pencere)
        }

        private func uyari(_ mesaj: String, dugmeler: [(String, () -> Void)]) {
            let pencere = UIAlertController(title: nil, message: mesaj, preferredStyle: .alert)
            for (i, (ad, eylem)) in dugmeler.enumerated() {
                pencere.addAction(UIAlertAction(title: ad, style: i == 0 && dugmeler.count > 1 ? .cancel : .default) { _ in eylem() })
            }
            sun(pencere)
        }

        private func sun(_ pencere: UIAlertController) {
            guard let sahne = UIApplication.shared.connectedScenes.first(where: { $0.activationState == .foregroundActive }) as? UIWindowScene,
                  var kok = sahne.windows.first(where: \.isKeyWindow)?.rootViewController else { return }
            while let ust = kok.presentedViewController { kok = ust }
            kok.present(pencere, animated: true)
        }
    }
}
