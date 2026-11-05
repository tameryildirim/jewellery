# ByGatech iOS — kurulum ve kullanım

Sunucudaki ByGatech'i (`https://5-10-220-150.sslip.io`) açan yerel iOS uygulaması.
Oturum hatırlanır, kamera/fotoğraf izni verilir, bağlantı yoksa "Yeniden dene" kartı çıkar,
aşağı çekince yenilenir. Dal: `ios`. Kaynak: `ios/ByGatech/Kod/`.

## Sunucu adresini değiştirmek
`ios/project.yml` → `SunucuAdresi` (tek nokta). Sonra `xcodegen generate`.

## Derleme (Mac, Xcode 27)
```bash
cd ios && xcodegen generate
xcodebuild -project ByGatech.xcodeproj -scheme ByGatech -destination 'platform=iOS Simulator,name=iPhone 18 Pro' build
```

## Kendi iPhone'una kurmak (bir kez Apple hesabı gerekir)
1. Xcode → Settings → Accounts → `+` → Apple ID ile gir (ücretsiz hesap yeter; 7 günde bir yeniden imzalanır,
   ücretli geliştirici hesabı 1 yıl + TestFlight).
2. iPhone'u kabloyla bağla, telefonda "Bu bilgisayara güven" de.
3. Xcode'da `ios/ByGatech.xcodeproj` aç → üstte cihaz olarak iPhone'u seç → ▶ Run.
4. Telefonda Ayarlar → Genel → VPN ve Cihaz Yönetimi → geliştirici hesabına "Güven".

Komut satırından (hesap eklendikten ve telefon bağlandıktan sonra):
```bash
cd ios && xcodebuild -project ByGatech.xcodeproj -scheme ByGatech -destination 'generic/platform=iOS' -allowProvisioningUpdates -derivedDataPath build/DerivedData build
xcrun devicectl device install app --device <UDID> build/DerivedData/Build/Products/Debug-iphoneos/ByGatech.app
```

## Uygulama kurmadan (şu an hazır)
Safari → `https://5-10-220-150.sslip.io` → Paylaş → **Ana Ekrana Ekle** (PWA, tam ekran, D3-01).
