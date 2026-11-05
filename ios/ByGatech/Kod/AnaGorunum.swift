import SwiftUI

/// Ana ekran: web görünümü + yüklenme çubuğu + çevrimdışı kartı + Face ID düğmesi + Face ID kilidi.
struct AnaGorunum: View {
    @StateObject private var durum = WebDurumu()
    @StateObject private var kilit = KilitDurumu()
    @Environment(\.scenePhase) private var sahne

    @State private var kullaniciAcik = false

    var body: some View {
        let giris = durum.girisEkrani
        ZStack {
            Color("ArkaPlan").ignoresSafeArea()
            VStack(spacing: 0) {
                if !giris && durum.hata == nil {
                    UstCubuk(kur: durum.kur, kullanici: durum.kullanici) { kullaniciAcik = true }
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
                ZStack(alignment: .topLeading) {
                    WebGorunum(durum: durum)
                        .opacity(durum.hata == nil ? 1 : 0)
                    // Giriş ekranı: web amblemi gizli, yerine aynı taşın gerçek 3B hâli.
                    if giris, let r = durum.amblem, durum.hata == nil {
                        ElmasGorunumu(donuyor: true, kenarKalinligi: 0.02)
                            .frame(width: r.width * 1.25, height: r.height * 1.25)
                            .offset(x: r.minX - r.width * 0.125, y: r.minY - r.height * 0.125)
                            .allowsHitTesting(false)
                    }
                    if durum.yukleniyor && durum.hata == nil {
                        ProgressView().progressViewStyle(.linear).tint(Color("Marka"))
                    }
                    if let hata = durum.hata {
                        HataKarti(mesaj: hata) { durum.yenidenDene() }
                            .padding(24)
                            .frame(maxWidth: .infinity, maxHeight: .infinity)
                    }
                    // Giriş formu açıkken ve kasada kayıt varken: Face ID ile giriş.
                    if durum.girisFormuGorunuyor && durum.girisKayitli && durum.hata == nil {
                        VStack { Spacer()
                            Button { Task { await durum.faceIdIleGiris() } } label: {
                                Label("Face ID ile giriş yap", systemImage: "faceid")
                                    .font(.body.weight(.semibold)).frame(maxWidth: .infinity).padding(.vertical, 14)
                            }
                            .buttonStyle(.borderedProminent).tint(Color("Marka"))
                            .padding(.horizontal, 24).padding(.bottom, 12)
                            .accessibilityIdentifier("faceid-giris")
                        }
                    }
                }
                .ignoresSafeArea(edges: giris ? [.bottom] : [])
                if !giris && durum.hata == nil {
                    SekmeCubugu(secili: durum.sekme) { s in durum.git(s) }
                        .transition(.move(edge: .bottom).combined(with: .opacity))
                }
            }
            if kilit.kilitli { KilitPerdesi(kilit: kilit).zIndex(10) }
        }
        .animation(.easeInOut(duration: 0.2), value: durum.girisFormuGorunuyor)
        .animation(.easeInOut(duration: 0.2), value: giris)
        .sheet(isPresented: $kullaniciAcik) { KullaniciSayfasi(durum: durum).presentationDetents([.large]) }
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
