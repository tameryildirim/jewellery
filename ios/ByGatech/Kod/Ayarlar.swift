import Foundation

/// Tek ayar noktası: uygulamanın açtığı sunucu adresi.
/// Info.plist içindeki `SunucuAdresi` anahtarından okunur; bozuksa sslip.io adresine düşer.
enum Ayarlar {
    static let varsayilanSunucu = "https://5-10-220-150.sslip.io"

    /// Sıra: iPhone Ayarlar › ByGatech › Sunucu adresi (boş değilse) → Info.plist SunucuAdresi → varsayılan.
    static var sunucu: URL {
        for aday in [UserDefaults.standard.string(forKey: "sunucuAdresi"),
                     Bundle.main.object(forInfoDictionaryKey: "SunucuAdresi") as? String] {
            let metin = (aday ?? "").trimmingCharacters(in: .whitespacesAndNewlines)
            if !metin.isEmpty, let url = URL(string: metin), url.scheme != nil, url.host != nil { return url }
        }
        return URL(string: varsayilanSunucu)!
    }

    /// Yalnız kendi sunucumuz uygulama içinde açılır; başka alanlar Safari'ye gider.
    static func kendiSunucumuzMu(_ url: URL) -> Bool {
        url.host?.lowercased() == sunucu.host?.lowercased()
    }
}
