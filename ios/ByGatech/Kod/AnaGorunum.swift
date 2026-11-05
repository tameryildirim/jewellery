import SwiftUI

/// Ana ekran: web görünümü + yüklenme çubuğu + çevrimdışı kartı + Face ID düğmesi + Face ID kilidi.
struct AnaGorunum: View {
    @StateObject private var durum = WebDurumu()
    @StateObject private var kilit = KilitDurumu()
    @Environment(\.scenePhase) private var sahne

    var body: some View {
        ZStack(alignment: .bottom) {
            Color("ArkaPlan").ignoresSafeArea()

            WebGorunum(durum: durum)
                .ignoresSafeArea(edges: .bottom)
                .opacity(durum.hata == nil ? 1 : 0)

            if durum.yukleniyor && durum.hata == nil {
                VStack { ProgressView().progressViewStyle(.linear); Spacer() }
            }

            if let hata = durum.hata {
                HataKarti(mesaj: hata) { durum.yenidenDene() }
                    .padding(24)
                    .frame(maxHeight: .infinity)
            }

            // Giriş formu açıkken ve kasada kayıt varken: Face ID ile giriş.
            if durum.girisFormuGorunuyor && durum.girisKayitli && durum.hata == nil {
                Button {
                    Task { await durum.faceIdIleGiris() }
                } label: {
                    Label("Face ID ile giriş yap", systemImage: "faceid")
                        .font(.body.weight(.semibold))
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                }
                .buttonStyle(.borderedProminent)
                .tint(.primary)
                .padding(.horizontal, 24)
                .padding(.bottom, 12)
                .transition(.move(edge: .bottom).combined(with: .opacity))
                .accessibilityIdentifier("faceid-giris")
            }

            if kilit.kilitli { KilitPerdesi(kilit: kilit).zIndex(10) }
        }
        .animation(.easeInOut(duration: 0.2), value: durum.girisFormuGorunuyor)
        .onChange(of: sahne) { yeni in
            switch yeni {
            case .active: kilit.oneGeldi()
            case .background: kilit.arkayaGitti()
            default: break
            }
        }
        .alert("Face ID ile giriş", isPresented: Binding(get: { durum.kaydetSorusu != nil }, set: { if !$0 { durum.kaydetVazgec() } })) {
            Button("Kaydet") { durum.kaydetOnayla() }
            Button("Şimdi değil", role: .cancel) { durum.kaydetVazgec() }
        } message: {
            Text("Bir dahaki girişte Face ID yeterli olsun mu? Bilgiler yalnız bu iPhone'un Anahtar Zinciri'nde saklanır.")
        }
        .alert("ByGatech", isPresented: Binding(get: { durum.bilgi != nil }, set: { if !$0 { durum.bilgi = nil } })) {
            Button("Tamam") { durum.bilgi = nil }
        } message: { Text(durum.bilgi ?? "") }
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
            Text("Bağlantı kurulamadı").font(.title2.weight(.semibold))
            Text(mesaj).font(.body).foregroundStyle(.secondary).multilineTextAlignment(.center)
            Button(action: dene) {
                Text("Yeniden dene").font(.body.weight(.semibold)).frame(maxWidth: .infinity).padding(.vertical, 14)
            }
            .buttonStyle(.borderedProminent)
        }
        .padding(24)
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
    }
}
