import SwiftUI

/// Yerli iOS kabuğu: üst çubuk (3B amblem · marka · kur · kullanıcı) ve alt sekme çubuğu (Apple sekme çubuğu düzeni).
/// Renkler marka belirteçlerinden (Assets: Marka, ArkaPlan, Ayrac, MetinSoluk, Kenarlik).
struct Sekme: Identifiable, Equatable {
    let id: String   // web menü kimliği: bugun · tezgah · musteriler · kasa · tum-ekranlar
    let yol: String
    let ad: String
    let simge: String

    static let hepsi: [Sekme] = [
        .init(id: "bugun", yol: "/", ad: "Bugün", simge: "sun.max"),
        .init(id: "tezgah", yol: "/islemler", ad: "Tezgâh", simge: "scalemass"),
        .init(id: "musteriler", yol: "/cariler", ad: "Müşteriler", simge: "person.2"),
        .init(id: "kasa", yol: "/kasalar", ad: "Kasa", simge: "banknote"),
        .init(id: "tum-ekranlar", yol: "/ekranlar", ad: "Tümü", simge: "square.grid.2x2"),
    ]
}

struct SekmeCubugu: View {
    let secili: String?
    let sec: (Sekme) -> Void

    var body: some View {
        HStack(spacing: 0) {
            ForEach(Sekme.hepsi) { s in
                let aktif = s.id == secili
                Button {
                    Titresim.ver("secim"); sec(s)
                } label: {
                    VStack(spacing: 3) {
                        Image(systemName: s.simge)
                            .symbolVariant(aktif ? .fill : .none)
                            .font(.system(size: 22, weight: .regular))
                        Text(s.ad).font(.system(size: 10, weight: .medium))
                    }
                    .foregroundStyle(aktif ? Color("Marka") : Color("MetinSoluk"))
                    .frame(maxWidth: .infinity)
                    .padding(.top, 7)
                    .contentShape(Rectangle())
                }
                .buttonStyle(.plain)
                .accessibilityIdentifier("sekme-\(s.id)")
                .accessibilityAddTraits(aktif ? .isSelected : [])
            }
        }
        .padding(.bottom, 2)
        .background(.bar)
        .overlay(alignment: .top) { Rectangle().fill(Color("Ayrac")).frame(height: 0.5) }
    }
}

struct UstCubuk: View {
    let kur: String?
    let kullanici: String?
    let kullaniciAc: () -> Void

    var body: some View {
        HStack(spacing: 10) {
            ElmasGorunumu(donuyor: true, kenarKalinligi: 0.03).frame(width: 36, height: 36)
            Text("BYGATECH")
                .font(.system(size: 13, weight: .medium))
                .tracking(4)
                .foregroundStyle(Color("Marka"))
            Spacer(minLength: 8)
            if let kur, !kur.isEmpty {
                Text(kur)
                    .font(.system(size: 11, weight: .regular)).tracking(1)
                    .foregroundStyle(Color("MetinSoluk"))
                    .lineLimit(1).minimumScaleFactor(0.7)
            }
            // Kurucu 05.11: kısaltma değil, fotoğraf yokken Apple'daki gibi kişi silueti.
            Button(action: kullaniciAc) {
                Image(systemName: "person.crop.circle")
                    .font(.system(size: 28, weight: .regular))
                    .foregroundStyle(Color("Marka"))
                    .frame(width: 34, height: 34)
            }
            .accessibilityLabel(kullanici ?? "Hesap")
            .accessibilityIdentifier("ust-kullanici")
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 6)
        .background(.bar)
        .overlay(alignment: .bottom) { Rectangle().fill(Color("Ayrac")).frame(height: 0.5) }
    }
}

/// Kullanıcı sayfası: web üst menüsünün işlevleri yerli biçimde (gruplu liste).
struct KullaniciSayfasi: View {
    @ObservedObject var durum: WebDurumu
    @Environment(\.dismiss) private var kapat
    @AppStorage("faceIdKilit") private var faceIdKilit = false
    @AppStorage("titresim") private var titresim = true
    @AppStorage("sistemYaziBoyutu") private var sistemYazi = true
    @State private var cikisOnay = false

    var body: some View {
        NavigationStack {
            Form {
                Section("Görünüm") {
                    Picker("Dil", selection: Binding(get: { durum.dil ?? "tr" }, set: { v in durum.dilSec(v) })) {
                        Text("Türkçe").tag("tr"); Text("English").tag("en")
                    }
                    Picker("Görünüm", selection: Binding(get: { durum.mod ?? "profesyonel" }, set: { v in durum.modSec(v) })) {
                        Text("Basit").tag("basit"); Text("Profesyonel").tag("profesyonel")
                    }
                    .pickerStyle(.segmented)
                }
                Section("iPhone") {
                    Toggle("Face ID ile kilitle", isOn: $faceIdKilit)
                    Toggle("Dokunma titreşimi", isOn: $titresim)
                    Toggle("Sistem yazı boyutu", isOn: $sistemYazi)
                    if durum.girisKayitli {
                        Button("Face ID girişini unut", role: .destructive) { durum.kasayiSil() }
                    }
                }
                Section {
                    Button { durum.tikla("yildiz-alt-kilavuz"); kapat() } label: { Label("Kılavuz", systemImage: "book") }
                    Button { durum.tikla("yildiz-alt-ayarlar"); kapat() } label: { Label("Ayarlar", systemImage: "gearshape") }
                }
                Section {
                    Button("Çıkış yap", role: .destructive) { cikisOnay = true }
                }
            }
            .navigationTitle(durum.kullanici ?? "Hesap")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .confirmationAction) { Button("Bitti") { kapat() } } }
            .confirmationDialog("Çıkış yapılsın mı?", isPresented: $cikisOnay, titleVisibility: .visible) {
                Button("Çıkış yap", role: .destructive) { durum.tikla("yildiz-cikis"); kapat() }
                Button("Vazgeç", role: .cancel) {}
            }
        }
        .tint(Color("Marka"))
    }
}
