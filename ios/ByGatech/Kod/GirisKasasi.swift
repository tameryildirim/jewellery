import Foundation
import Security
import LocalAuthentication

/// Face ID ile giriş kasası: e-posta + parola iOS Anahtar Zinciri'nde, yalnız bu cihazda,
/// biyometri (Face ID / Touch ID; yoksa cihaz şifresi) ile okunabilir. Sunucuya ayrıca bir şey gitmez.
enum GirisKasasi {
    private static let servis = "com.bygatech.kuyumcu.giris"
    /// Kayıtlı e-posta (gizli değil): aynı e-postayla yeniden girişte "kaydedilsin mi?" sorulmaz; uygulama silinip kurulsa da kalır.
    private static let epostaServisi = "com.bygatech.kuyumcu.giris.eposta"

    struct Giris: Codable { let eposta: String; let parola: String }

    private static var hesap: String { Ayarlar.sunucu.host ?? "bygatech" }

    static var kayitliMi: Bool {
        let sorgu: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: servis,
            kSecAttrAccount as String: hesap,
            kSecUseAuthenticationUI as String: kSecUseAuthenticationUIFail,
            kSecReturnData as String: false,
        ]
        let durum = SecItemCopyMatching(sorgu as CFDictionary, nil)
        return durum == errSecSuccess || durum == errSecInteractionNotAllowed
    }

    static func kaydet(_ g: Giris) -> Bool {
        sil()
        var bayrak: SecAccessControlCreateFlags = .biometryCurrentSet
        let lac = LAContext()
        if !lac.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: nil) { bayrak = .userPresence }
        guard let erisim = SecAccessControlCreateWithFlags(nil, kSecAttrAccessibleWhenUnlockedThisDeviceOnly, bayrak, nil),
              let veri = try? JSONEncoder().encode(g) else { return false }
        let kayit: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: servis,
            kSecAttrAccount as String: hesap,
            kSecAttrAccessControl as String: erisim,
            kSecValueData as String: veri,
        ]
        let tamam = SecItemAdd(kayit as CFDictionary, nil) == errSecSuccess
        if tamam {
            let e: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: epostaServisi,
                                    kSecAttrAccount as String: hesap, kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
                                    kSecValueData as String: Data(g.eposta.utf8)]
            SecItemAdd(e as CFDictionary, nil)
        }
        return tamam
    }

    static var kayitliEposta: String? {
        let sorgu: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: epostaServisi,
                                    kSecAttrAccount as String: hesap, kSecReturnData as String: true]
        var sonuc: CFTypeRef?
        guard SecItemCopyMatching(sorgu as CFDictionary, &sonuc) == errSecSuccess, let d = sonuc as? Data else { return nil }
        return String(data: d, encoding: .utf8)
    }

    /// Face ID / Touch ID ister; başarıda girişi döner.
    static func oku() async -> Giris? {
        await withCheckedContinuation { devam in
            DispatchQueue.global(qos: .userInitiated).async {
                let baglam = LAContext()
                baglam.localizedReason = "ByGatech'e giriş yapmak için"
                baglam.localizedCancelTitle = "Vazgeç"
                let sorgu: [String: Any] = [
                    kSecClass as String: kSecClassGenericPassword,
                    kSecAttrService as String: servis,
                    kSecAttrAccount as String: hesap,
                    kSecReturnData as String: true,
                    kSecUseAuthenticationContext as String: baglam,
                ]
                var sonuc: CFTypeRef?
                let durum = SecItemCopyMatching(sorgu as CFDictionary, &sonuc)
                guard durum == errSecSuccess, let veri = sonuc as? Data,
                      let g = try? JSONDecoder().decode(Giris.self, from: veri) else { devam.resume(returning: nil); return }
                devam.resume(returning: g)
            }
        }
    }

    @discardableResult
    static func sil() -> Bool {
        let sorgu: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: servis,
            kSecAttrAccount as String: hesap,
        ]
        let d = SecItemDelete(sorgu as CFDictionary)
        SecItemDelete([kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: epostaServisi, kSecAttrAccount as String: hesap] as CFDictionary)
        return d == errSecSuccess || d == errSecItemNotFound
    }
}

extension GirisKasasi.Giris: Equatable {}
