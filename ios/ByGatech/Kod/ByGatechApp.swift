import SwiftUI

/// ByGatech iOS kabuğu — sunucudaki ByGatech'i açar (adres Info.plist → SunucuAdresi).
@main
struct ByGatechApp: App {
    init() { Tercihler.kaydet() }

    var body: some Scene {
        WindowGroup {
            AnaGorunum()
        }
    }
}
