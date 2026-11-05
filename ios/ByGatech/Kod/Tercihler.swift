import Foundation

/// iPhone Ayarlar › ByGatech altındaki anahtarlar (Settings.bundle). Varsayılanlar burada kayıt edilir.
enum Tercihler {
    static func kaydet() {
        UserDefaults.standard.register(defaults: [
            "faceIdKilit": false,
            "titresim": true,
            "sistemYaziBoyutu": true,
        ])
    }
    static var faceIdKilit: Bool { UserDefaults.standard.bool(forKey: "faceIdKilit") }
    static var titresim: Bool { UserDefaults.standard.bool(forKey: "titresim") }
    static var sistemYaziBoyutu: Bool { UserDefaults.standard.bool(forKey: "sistemYaziBoyutu") }
}
