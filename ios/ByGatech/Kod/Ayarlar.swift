import Foundation

/// Tek ayar noktası: uygulamanın açtığı sunucu adresi.
/// Info.plist içindeki `SunucuAdresi` anahtarından okunur; bozuksa sslip.io adresine düşer.
enum Ayarlar {
    static let varsayilanSunucu = "https://5-10-220-150.sslip.io"

    static var sunucu: URL {
        let metin = (Bundle.main.object(forInfoDictionaryKey: "SunucuAdresi") as? String)?
            .trimmingCharacters(in: .whitespacesAndNewlines)
        if let metin, let url = URL(string: metin), url.scheme != nil, url.host != nil {
            return url
        }
        return URL(string: varsayilanSunucu)!
    }

    /// Yalnız kendi sunucumuz uygulama içinde açılır; başka alanlar Safari'ye gider.
    static func kendiSunucumuzMu(_ url: URL) -> Bool {
        url.host?.lowercased() == sunucu.host?.lowercased()
    }
}
