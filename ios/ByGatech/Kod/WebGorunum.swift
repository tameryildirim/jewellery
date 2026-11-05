import SwiftUI
import WebKit

/// Web görünümünün durumu — SwiftUI ile WKWebView arasındaki köprü.
final class WebDurumu: ObservableObject {
    @Published var yukleniyor = true
    @Published var hata: String?
    /// WKWebView'e "yeniden yükle" demek için sayaç.
    @Published var yenidenDeneSayaci = 0

    func yenidenDene() {
        hata = nil
        yukleniyor = true
        yenidenDeneSayaci += 1
    }
}

/// Sunucudaki ByGatech'i açan WKWebView.
/// - Çerez/yerel depo kalıcı (varsayılan veri deposu) → giriş hatırlanır.
/// - Kamera/mikrofon isteği uygulama iznine bağlanır (fotoğraf, barkod).
/// - Aşağı çekerek yenileme; geri/ileri kaydırma hareketleri.
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

        context.coordinator.web = web
        web.load(URLRequest(url: Ayarlar.sunucu))
        return web
    }

    func updateUIView(_ web: WKWebView, context: Context) {
        if context.coordinator.sonDeneme != durum.yenidenDeneSayaci {
            context.coordinator.sonDeneme = durum.yenidenDeneSayaci
            if web.url == nil || durum.hata != nil {
                web.load(URLRequest(url: Ayarlar.sunucu))
            } else {
                web.reload()
            }
        }
    }

    final class Koordinator: NSObject, WKNavigationDelegate, WKUIDelegate {
        let durum: WebDurumu
        weak var web: WKWebView?
        var sonDeneme = 0

        init(durum: WebDurumu) { self.durum = durum }

        @objc func asagiCekildi(_ kontrol: UIRefreshControl) {
            web?.reload()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.6) { kontrol.endRefreshing() }
        }

        // MARK: Yükleme durumu
        func webView(_ webView: WKWebView, didStartProvisionalNavigation navigation: WKNavigation!) {
            durum.yukleniyor = true
        }

        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            durum.yukleniyor = false
            durum.hata = nil
        }

        func webView(_ webView: WKWebView, didFailProvisionalNavigation navigation: WKNavigation!, withError error: Error) {
            hataGoster(error)
        }

        func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
            hataGoster(error)
        }

        private func hataGoster(_ error: Error) {
            let kod = (error as NSError).code
            if kod == NSURLErrorCancelled { return } // kullanıcı başka sayfaya geçti
            durum.yukleniyor = false
            durum.hata = kod == NSURLErrorNotConnectedToInternet
                ? "İnternet bağlantısı yok. Bağlantıyı kontrol edip yeniden deneyin."
                : "Sunucuya ulaşılamadı. Birkaç saniye sonra yeniden deneyin."
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

        /// target=_blank bağlantıları yeni pencere yerine aynı görünümde açılır.
        func webView(_ webView: WKWebView, createWebViewWith configuration: WKWebViewConfiguration,
                     for navigationAction: WKNavigationAction, windowFeatures: WKWindowFeatures) -> WKWebView? {
            if let url = navigationAction.request.url {
                if Ayarlar.kendiSunucumuzMu(url) { webView.load(URLRequest(url: url)) }
                else { UIApplication.shared.open(url) }
            }
            return nil
        }

        // MARK: Kamera / mikrofon (iOS 15+)
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
