# ByGatech iOS — kurulum ve kullanım

Sunucudaki ByGatech'i (`https://5-10-220-150.sslip.io`) açan yerel iOS uygulaması (SwiftUI + WKWebView).
Dal: `ios`. Kaynak: `ios/ByGatech/Kod/`.

## Yerli kabuk (Apple tasarım dili)
- **Alt sekme çubuğu** (SF Symbols): Bugün · Tezgâh · Müşteriler · Kasa · Tümü — web'in 5 kelimelik üst menüsüyle aynı rotalar (`Kabuk.swift`, `Sekme.hepsi`). Seçili sekme web'den okunur (`#yildiz-menu .aktif`).
- **Üst çubuk:** 3B amblem · BYGATECH · kur şeridi (HAS/22 ayar) · kullanıcı kısaltması → yerli hesap sayfası (dil, Basit/Profesyonel, iPhone ayarları, Kılavuz, Ayarlar, Çıkış). Web'in kendi üst menüsü uygulama içinde gizlenir (`.ios-yerli`), işlevleri gizli düğmelere köprüyle (`__bygatechTikla`, `__bygatechGit`, `__bygatechDil`, `__bygatechDurum`) sürülür.
- **3B amblem:** web'deki çizimle BİREBİR aynı taş — `tema-yildiz.css .amb-f0…41` matrix3d şeritlerinden çözülen 11 köşe / 26 kenar / 20 yüzey (`TasVerisi.swift`, üretim: scratchpad amb-geo), SceneKit'te cam yüzeyler (fizik tabanlı, stüdyo ortam yansıması) + web'deki gibi parlayan kenar şeritleri ve 11 sn'lik parlama; giriş ekranında web ambleminin yerine biner (`.amb-ik` kutusu ölçülür), üst çubukta 36 pt.
- Giriş/şifre ekranlarında yerli çubuklar gizlenir.

## Apple'a özgü özellikler
- **SF Pro yazı tipi**: uygulama içinde web yazı tipleri (Inter/Manrope/Sora) yerine Apple'ın sistem yazı tipi (`KabukBetigi.swift` biçemi).
- **Sistem yazı boyutu**: iPhone Ayarlar › Ekran ve Parlaklık › Metin Boyutu izlenir (0,9–1,25; WebKit `text-size-adjust`, yalnız metin büyür, düzen akar). Kapatılabilir.
- **Face ID ile giriş**: ilk başarılı web girişinden sonra "kaydedilsin mi?" sorulur; e-posta+parola yalnız bu iPhone'un Anahtar Zinciri'nde, Face ID/Touch ID (yoksa cihaz şifresi) korumalı saklanır (`GirisKasasi.swift`). Giriş ekranında "Face ID ile giriş yap" düğmesi formu doldurup girer. MFA varsa ikinci adım yine sorulur. Parola değişip giriş reddedilirse kayıt silinir, bir sonraki girişte yeniden sorulur.
- **Face ID ile kilit** (isteğe bağlı): açıkken uygulama her öne gelişinde kimlik ister; içerik buzlu camla örtülür (`Kilit.swift`).
- **Dokunma titreşimi** (Taptic): düğme hafif, form gönderimi orta, hata/başarı bildirimi (`Titresim.swift`).
- **iPhone Ayarlar › ByGatech**: Face ID ile kilitle · Dokunma titreşimi · Sistem yazı boyutu · Sunucu adresi (`Settings.bundle`).
- Oturum/çerez kalıcı, kamera/mikrofon izni, aşağı çek yenile, geri/ileri kaydırma, çevrimdışı "Yeniden dene" kartı, JS uyarıları yerli pencere, dış bağlantılar Safari'de.

## Geliştirme kancaları (yalnız Debug derlemesi; Release'e girmez)
- `BYGATECH_TEST_GIRIS="eposta<TAB>parola"` ortam değişkeni: giriş formu görünür görünmez doldurup gönderir (simülatör kanıtı).
- `BYGATECH_TEST_JS="<ifade>"`: sayfa yüklendikten 4 sn sonra çalıştırılır, sonucu sistem günlüğüne `TEST_JS:` ile yazar.
- Simülatörde sunucu adresi: `xcrun simctl spawn booted defaults write com.bygatech.kuyumcu sunucuAdresi http://localhost:5199`.
- Simülatörde Face ID: `notifyutil -s com.apple.BiometricKit.enrollmentChanged 1` + `-p …enrollmentChanged`; eşleşme `-p com.apple.BiometricKit_Sim.pearl.match`.

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
