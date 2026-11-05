import UIKit

/// Apple dokunma titreşimi (Taptic Engine). Web tarafı `titresim` köprüsüyle ister.
enum Titresim {
    private static let hafif = UIImpactFeedbackGenerator(style: .light)
    private static let orta = UIImpactFeedbackGenerator(style: .medium)
    private static let secim = UISelectionFeedbackGenerator()
    private static let bildirim = UINotificationFeedbackGenerator()

    static func ver(_ tur: String) {
        guard Tercihler.titresim else { return }
        switch tur {
        case "hafif": hafif.impactOccurred()
        case "orta": orta.impactOccurred()
        case "secim": secim.selectionChanged()
        case "basari": bildirim.notificationOccurred(.success)
        case "uyari": bildirim.notificationOccurred(.warning)
        case "hata": bildirim.notificationOccurred(.error)
        default: break
        }
    }
}
