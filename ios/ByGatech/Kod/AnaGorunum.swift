import SwiftUI

/// Ana ekran: web görünümü + yüklenme çubuğu + çevrimdışı/hata kartı ("Yeniden dene").
struct AnaGorunum: View {
    @StateObject private var durum = WebDurumu()

    var body: some View {
        ZStack {
            Color("ArkaPlan").ignoresSafeArea()

            WebGorunum(durum: durum)
                .ignoresSafeArea(edges: .bottom)
                .opacity(durum.hata == nil ? 1 : 0)

            if durum.yukleniyor && durum.hata == nil {
                VStack {
                    ProgressView().progressViewStyle(.linear).tint(.accentColor)
                    Spacer()
                }
            }

            if let hata = durum.hata {
                HataKarti(mesaj: hata) { durum.yenidenDene() }
                    .padding(24)
            }
        }
    }
}

/// Bağlantı yokken ya da sunucu cevap vermezken görünen sade kart.
struct HataKarti: View {
    let mesaj: String
    let dene: () -> Void

    var body: some View {
        VStack(spacing: 16) {
            Image(systemName: "wifi.exclamationmark")
                .font(.system(size: 44, weight: .light))
                .foregroundStyle(.secondary)
            Text("Bağlantı kurulamadı")
                .font(.title2.weight(.semibold))
            Text(mesaj)
                .font(.body)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
            Button(action: dene) {
                Text("Yeniden dene")
                    .font(.body.weight(.semibold))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 14)
            }
            .buttonStyle(.borderedProminent)
        }
        .padding(24)
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
    }
}
