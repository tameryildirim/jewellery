import SwiftUI
import LocalAuthentication

/// Face ID / Touch ID kilidi: "Face ID ile kilitle" açıksa uygulama öne her gelişinde kimlik ister.
/// Doğrulanana kadar içerik buzlu camla örtülür (uygulama değiştiricide de içerik görünmez).
@MainActor
final class KilitDurumu: ObservableObject {
    @Published var kilitli = false
    @Published var hata: String?
    private var dogruluyor = false

    func oneGeldi() {
        guard Tercihler.faceIdKilit else { kilitli = false; return }
        kilitli = true
        ac()
    }

    func arkayaGitti() {
        if Tercihler.faceIdKilit { kilitli = true }
    }

    func ac() {
        guard kilitli, !dogruluyor else { return }
        dogruluyor = true
        hata = nil
        let baglam = LAContext()
        baglam.localizedCancelTitle = "Vazgeç"
        var sebep: NSError?
        // Biyometri yoksa cihaz şifresine düşer; o da yoksa kilit anlamsız → aç.
        guard baglam.canEvaluatePolicy(.deviceOwnerAuthentication, error: &sebep) else {
            kilitli = false; dogruluyor = false; return
        }
        baglam.evaluatePolicy(.deviceOwnerAuthentication, localizedReason: "ByGatech'i açmak için kimliğinizi doğrulayın") { basarili, _ in
            Task { @MainActor in
                self.dogruluyor = false
                if basarili { self.kilitli = false } else { self.hata = "Doğrulanamadı." }
            }
        }
    }
}

struct KilitPerdesi: View {
    @ObservedObject var kilit: KilitDurumu

    var body: some View {
        ZStack {
            Rectangle().fill(.ultraThinMaterial).ignoresSafeArea()
            VStack(spacing: 18) {
                Image(systemName: "faceid")
                    .font(.system(size: 56, weight: .light))
                Text("ByGatech kilitli")
                    .font(.title3.weight(.semibold))
                if let hata = kilit.hata {
                    Text(hata).font(.footnote).foregroundStyle(.secondary)
                }
                Button("Kilidi aç") { kilit.ac() }
                    .buttonStyle(.borderedProminent)
            }
        }
    }
}
