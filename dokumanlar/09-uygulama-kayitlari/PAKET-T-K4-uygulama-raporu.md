# PAKET T-K4 — TÜM EKRANLAR + GİRİŞ TUVALE · uygulama raporu

Tarih: 2026-09-25 · Şerit: T-KABUK (vt `kuyumcu_test_t1` · canlı API 5160 · vekil 5161) · Rol: Taşkın + Ertuğ
Durum beyanı: **BITTI**. Paralel şerit kapısının 7 maddesi sağlandı (§ 13).

## 1. Kaynak ve koda karşı doğrulama
Tuval: `tuval/project/Ekranlar.dc.html` ve `Giris.dc.html` (satır içi ölçüler). Orkestratörün `ham/TUVAL-png/Ekranlar.png` ve `Giris.png` görüntüleri Read ile açılıp incelendi. Ayrıca iki tuval başsız Chrome'da `file://` ile açıldı ve ürünle aynı ölçüm betiğiyle ölçüldü (`scratchpad/tk4/olc-geo.js`).

Başlangıçtaki ölçüm (`scratchpad/tk4/png/once-*.png`, `once.txt`):
- **/ekranlar:** Izgara 5 sütundu, app.css `auto-fill minmax(14rem)` kuralından geliyordu (tuval 8 sütun, 163 px). Marka "MİHENK" yazılıyordu, tuval "Mihenk". Arama kutusu 52 px'ti, tuvalde 44 px. İpucu rengi `--menu-baslik` (#8ea2c4), tuvalde #aab7cf. Alt karo "Vitrin Panosu", tuvalde "Vitrin panosu". Yönetim grubu 900 px'in altına taşıyordu.
- **/giris:** Marka "MİHENK", tuval "Mihenk". Parola etiketi "Şifre", düğme "Giriş Yap". Etiketler yıldızlı ve 500 ağırlıktı. Her alanda boş üçüncü grid satırı 7 px fazla boşluk açıyordu. Bağlantılar 44 px yer kaplıyordu. Dil seçici ve katlı kısıt beyanı kartın akışındaydı, bu yüzden kart tuvaldeki konumundan 80 px yukarıdaydı (marka y=108, tuvalde 188).

Spesifikasyonun "mevcut durum" varsayımı tuttu: iki sayfa S1-d ve S1-e'de tuvale göre kurulmuştu. Bu paket onları ölçü ve metin düzeyinde tuvalle birebir hâle getirdi. Yeni özellik yok, kaldırılan özellik de yok.

## 2. Yapılanlar
| # | Ekran | Madde | Uygulama |
|---|---|---|---|
| 1 | Ekranlar | Marka "Mihenk" | `KabukTuvalSozlugu.MarkaYazimi` (T-K2 ile aynı kaynak; Marka.Ad'dan tr-TR ile türer, ikinci kaynak yok) |
| 2 | Ekranlar | 8 sütun, 163×68 karo | `tuval-kabuk.css` T-K4 (2): ≥1400 px'te 8 sütun. 1024–1399 px'te 6 sütun: 1280 px'te 8 sütun 143 px karo bırakır ve kısa adlar iki satıra iner (K3). Daha dar ekranda app.css kuralı geçerli. |
| 3 | Ekranlar | Günlük iş ikonlu satırı | Ekranlar.razor: yalnız Günlük grubunun ızgarası `ekranlar-izgara-ikonlu` sınıfını taşır. CSS (3): karo en az bir sütun genişliğinde; yazı sığmazsa yazıya göre genişler, yazı `nowrap`. |
| 4 | Ekranlar | Yıldız yer ayırmaz | CSS (4), yalnız `(hover: hover) and (min-width: 769px)` için: karo sağ dolgusu 14 px (tuval); yıldız sağ üst köşede 24 px. Dokunmatik ekranda app.css kuralı değişmedi (yıldız her zaman görünür, yeri ayrılır). |
| 5 | Ekranlar | Arama 360×44, ipucu #aab7cf | CSS: girdi 24 px satır; `--tk-soluk-acik` (kenar-metin ile menu-baslik yarı yarıya karışır, T-K2 kuralı). İpucu `nowrap`. |
| 6 | Ekranlar | "Vitrin panosu" | `EkranlarGirisTuvalSozlugu.VitrinPanosu` (TR "Vitrin panosu" / EN "Showcase board") |
| 7 | Giriş | Marka ve başlık | h1 `MarkaYazimi` ("Mihenk"); `GirisSozlugu.HosGeldin` → "Mihenk’e hoş geldin" / "Welcome to Mihenk"; sürüm satırı "Mihenk 1.0 · ByGatech" |
| 8 | Giriş | "Parola", "Giriş yap" | `EkranlarGirisTuvalSozlugu.Parola` / `GirisYap`; kayıt formundaki parola alanı da "Parola" (aynı ekranda tek ad). PageTitle da aynı anahtarı kullanır. |
| 9 | Giriş | Etiketler 15/400, yıldızsız | CSS (6)(9): `.zorunlu-isaret` gizli (işaret zaten `aria-hidden`; boş gönderimde alan altı mesaj çıkar, § 10). Etiket ağırlığı 400. |
| 10 | Giriş | Alan arası 18 px | CSS (9): alt satırı boş alanda grid 2 satır (`.alan:not(:has(> .alan-alt > *))`). Hata gelince 3. satır geri döner. |
| 11 | Giriş | Bağlantılar 22 px satır | CSS (7): `margin: -11px 0`. 44 px dokunma hedefi korunur, yerleşimde 22 px yer tutar. |
| 12 | Giriş | Kart dikeyde tuval konumunda | CSS (8), yalnız ≥769 px genişlik ve ≥700 px yükseklikte: dil seçici sol bölmenin sağ üstünde (absolute 24/24). Kısıt beyanı kapalıyken sol bölmenin dibinde, açılınca akışa döner. |
| 13 | Giriş | Marka satır yüksekliği | CSS (10): 1.45 (tuvalin gövde satırı) |

Kaydet/POST yolları, yetki ve yasal kapılar DEĞİŞMEDİ: `POST /auth/giris {email, sifre}`, MFA adımı, kayıt (davet jetonu), kısıt beyanı çekimi ve yıldızın `PATCH /tercihler/benim` yolu aynı. Mevcut DOM id'lerinin hepsi korundu (TK4_06 listesi). Yeni id eklenmedi.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Ekranlar.razor`, `Pages/Giris.razor` (bu dalgada T-K4). app.css, Sozlukler hub'ı, Bilesenler/Form/* (Q10-5 alanı), ikon sprite'ı ve MenuTanimi DEĞİŞMEDİ.

## 3. Sapmalar ve gerekçeler (GEREKÇELİ FARK)
| Öğe | Tuval | Bizde | Gerekçe |
|---|---|---|---|
| E-posta etiketi | "E-posta ya da telefon" | "E-posta" | Sunucu yalnız e-posta kabul ediyor (`GirisDto [EmailAddress]`). Canlıda doğrulandı: telefon biçimli giriş **400** "Geçerli bir e-posta adresi giriniz" döndü (§ 10). Etiket yalan söylemez. Telefonla giriş ayrı bir özellik (kurucu kararı). |
| Sürüm satırı | "… · Verileriniz Türkiye’de saklanır." | yazılmadı | KVKK beyanı sayılır. Ürün müşterinin sunucusuna kurulur, saklama yeri koddan doğrulanamaz (S1-e S4 gerekçesi geçerli). Mevzuat kuralı: kaynaksız yazılmaz. Kurucu teyidi gerekir. |
| Dil seçici (TR/EN) | yok | sol bölmenin sağ üst köşesi | Özellik silinmez. Kartın akışından çıkarıldı, böylece kart tuvaldeki konumunda. |
| Kısıt beyanı (P-30/BK-47) | yok | sol bölmenin dibinde katlı özet | Özellik silinmez ("giriş ekranında okunur" şartı). Kapalıyken akış dışında, açılınca akışta. |
| Girdi kenarlığı / sürüm rengi | #c9d0db / #8a94a6 | `--kenarlik` / `--metin-soluk` | Kontrast (WCAG 1.4.11 ve AA). S1-e S7/S8 gerekçesi geçerli. |
| Günlük iş ikon glifleri | ev, kart, kilit-çanta, pano-onay, zil | sprite glifleri (layout-dashboard, coins, handshake, square-pen, bell) | İkon biçimi aynı: ince çizgi, 20 px, vurgu rengi. Glifler tek sprite'tan (`wwwroot/img/ikon.svg`) ve kenar çubuğuyla aynı kaynaktan (`MenuTanimi`) gelir (UX-S31). Sprite ve MenuTanimi bu şeridin dosya alanı DIŞINDA. Tuval Panel kenar çubuğunda da ev glifi var ve T-K2 de sprite'ı değiştirmedi. Glif değişikliği ayrı iş (§ 14). |
| "Sipariş ve tamir" karosu | 163 px, 2 satır | 182 px, 1 satır (sonraki karo +18 px) | K3 kuralı (kısa etiket bölünmez) bağlayıcı; T-K3'teki emsal karar da aynı. Diğer 37 karo 163×68 birebir. |
| Satış karosu kenarlığı | 0 | 1 px saydam (app.css) | Kutu 163×68 birebir, görünür fark yok. |
| Tuval h1 | "… hoş geldin" div/h1 | marka h1, başlık SayfaBasi h2 | `#sayfa-basi-giris-*` id'leri ve P-37c kapsamı korunur (S1-e S10). |
| Mod (BASİT/PRO) | — | /ekranlar modu süzmez; /giris oturum öncesidir | Ekranlar'da mod süzgeç değil (M-UX3-06). İki modda aynı ölçüldü (§ 9). |
| Tuval örnek verisi | ayse@ozkuyum.com, dolu parola | boş, yer tutucu "ornek@isletme.com" | Veri değil. |
| İpucu rengi | #aab7cf | rgb(178,192,215) | Belirteç karışımı (T-K2 ile aynı kural). ±8 düzey. |

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. DoD sözleşmesi
Yeni varlık ya da kayıt türü **yok**. Yeni düğme yok. Mevcut düğmelerin hepsi gerçek istek atar ya da gezinir: Giriş yap → POST /auth/giris; Göster → kutu türünü değiştirir; Parolamı unuttum → doğru yolu yazan yardım metnini açar; Yeni dükkân kur → kayıt formu; yıldız → PATCH /tercihler/benim. Boş ya da geçersiz gönderimde alan altına Türkçe mesaj çıkar (TK4_08, canlı § 10). Mevzuat hükmü yok.

## 6. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`: **0 uyarı, 0 hata** (başlangıç `b0.log`, son mutasyondan sonra temiz `b4.log`).
- Yeni test sınıfı `TK4EkranlarGirisTuvalTestleri`: **15 test**, hepsi yeşil. Kapsamı:
  - yazımlar TR+EN
  - ikonlu ızgara sınıfı
  - id'lerin korunması
  - POST gövdesi `{email, sifre}` (JSON ayrıştırılarak)
  - boş gönderimde mesaj ve istek yokluğu
  - CSS bloğu: sabit renk yok; sütunlar; yıldız; arama; giriş yerleşimi
  - Razor'da satır içi stil ve düz marka metni yok
- Güncellenen testler (tuval yazımı gereği; sabit metin yerine sözlükten):
  - `S1dEkranlarTestleri`: marka, Vitrin panosu
  - `S1eGirisTestleri`: marka, "Mihenk’e hoş geldin", "Giriş yap" TR/EN, sürüm satırı
  - `UX1C1EkranTestleri`: düğme metni artık `EkranlarGirisTuvalSozlugu.GirisYap`
- Filtreli koşum (TK4, S1d, S1e, P30, UX1C1, UX1C4, MobilParite, MarkaAdi, P37c, Ikon, MenuKatlama, RotaBaslik, TK2, TK3, GirdiGocu, Tasarim): **374/374**.
- Tam süit (`trx-pT-K4/_Mac_2026-09-25_02_45_21.trx`): **5845 toplam · 5845 geçti · 0 başarısız**. Yeni kırmızı yok; tam süitte başka şeride ait kırmızı da yok.

## 7. Mutasyon kanıtları
Yedekler `scratchpad/mut-yedek/T-K4/` altında; her mutasyondan sonra dosya geri kopyalandı, `cmp` 15/15 AYNI. Son mutasyondan sonraki temiz derleme 0/0.

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M-TK4-1 | Ekranlar marka `MarkaYazimi` → `Marka.Ad` | TK4_01, S1d `Ust_serit_marka_PANELE_doner…` |
| M-TK4-2 | Vitrin → `Dil.T.Pano.Baslik` | TK4_02, S1d `Alt_baglantilar_Kilavuz_Ayarlar_Vitrin_panosu` |
| M-TK4-3 | `ekranlar-izgara-ikonlu` sınıfı kaldırıldı | TK4_03 |
| M-TK4-4 | ≥1400 px'te 8 → 7 sütun | TK4_10 |
| M-TK4-5 | Yıldız payı 14 → 36 px | TK4_12 |
| M-TK4-6 | İkonlu karo `nowrap` kaldırıldı | TK4_11 |
| M-TK4-7 | Arama girdisi 24 px satırı kaldırıldı | TK4_13 |
| M-TK4-8 | Giriş parola etiketi → `Auth.Sifre` | TK4_04 |
| M-TK4-9 | Giriş düğmesi → `Auth.GirisYap` | TK4_04, TK4_05, S1e `Giris_formu_EPOSTA…`, S1e `Ingilizce_dilde…`, UX1C1 `Giris_bos_formda…` |
| M-TK4-10 | HosGeldin → `Marka.Ad` | TK4_04, S1e `Tuval_IKI_PANEL…`, S1e `Sozluk_IKI_DILDE…` |
| M-TK4-11 | Dil seçici absolute kuralı silindi | TK4_14 |
| M-TK4-12 | Boş alt satır grid kuralı silindi | TK4_14 |
| M-TK4-13 | Bağlantı −11 px kuralı silindi | TK4_14 |
| M-TK4-14 | Zorunlu yıldızı gizleme silindi | TK4_14 |
| M-TK4-15 | Sürüm satırı → `Marka.Ad` | TK4_04, S1e `Surum_satiri_DERLEMEDEN…` |

Sonuç: 15/15 mutasyon kırmızı, test açığı yok (`scratchpad/tk4/mut-sonuc.txt`).

## 8. Görsel karşılaştırma (ASIL kanıt) — 1440×900
Görüntüler (`ham/TUVAL-KARSILASTIRMA/`):
- `T-K4-Ekranlar-1440.png`: solda tuval, sağda ürün (BASİT, açık)
- `T-K4-Giris-1440.png`: solda tuval, sağda ürün (açık)
- `T-K4-koyu-1440.png`: /ekranlar PRO koyu + /giris koyu

Ölçüm başsız Chrome + CDP ile yapıldı (`scratchpad/tk4/cek.mjs`); aynı betik tuvalde ve üründe koşuldu. Değerler x, y, genişlik, yükseklik biçimindedir.

**Tüm ekranlar**
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Koyu üst şerit | 0,0,1440×68 | 0,0,1440×68 | AYNI |
| Marka (34 px kare + "Mihenk" 21/700 Sora) | bağlantı 32,17 124×34 | 32,17 124×34 | AYNI |
| "Tüm ekranlar" 22/600 Sora | 184,19 145×31 | 184,19 145×31 | AYNI (başlangıçta 194) |
| Arama kabı / girdi | 365,12 360×44 / 405,22 305×24 | 365,12 360×44 / 405,22 305×24 | AYNI (başlangıçta 52 px) |
| İpucu 15 px | 1098,24 310×21, #aab7cf | 1098,24 310×21, belirteç karışımı | AYNI (renk ±8, § 3) |
| Grup başlıkları 15/600 BÜYÜK HARF | y 88/199/310/499/610/721 | y 88/199/310/499/610/721 | AYNI |
| Karolar 163×68, r10, 1 px çerçeve, 17/500 | 37 karo | 37 karo birebir konumda | AYNI |
| Satış karosu (vurgu dolu, 17/600) | 205,117 163×68 | 205,117 163×68 | AYNI |
| Sipariş ve tamir / Bildirimler | 552 163 (2 satır) / 725 | 552 182 (1 satır) / 743 | GEREKÇELİ FARK (K3) |
| Günlük iş ikonları | 20 px ince çizgi, vurgu | 20 px ince çizgi, vurgu (sprite glifi) | GEREKÇELİ FARK (glif, § 3) |
| Alt küçük karolar | Kılavuz 32,834 86×44 · Ayarlar 128 85×44 · Vitrin panosu 222 131×44 | birebir | AYNI |
| Sayfa 900 px'e sığar | evet | `scrollHeight` 900, yatay 0 | AYNI |

**Giriş**
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Sütunlar 1fr / 1.1fr | 686 / 754 | 686 / 754 | AYNI |
| Marka satırı ("Mihenk" 24/700) | 179,192 91×35 | 179,192 91×35 | AYNI (başlangıçta y=108) |
| Başlık 34/700 | 123,262 440×41 | 123,262 440×41 | AYNI |
| Alt cümle 18 | 123,311 | 123,310 | AYNI (±1) |
| E-posta etiketi / kutu | 367 / 395 440×54 | 367 / 395 440×54 | AYNI konum; metin GEREKÇELİ FARK |
| Parola etiketi 15/400 / kutu | 467 / 496 440×54 | 467 / 497 (kutu 440×54) | AYNI |
| "Göster" 15 px vurgu | 499,512 | 499 (44 px dokunma kutusunda ortalı) | AYNI (S1-e S5) |
| "Giriş yap" 440×54 r9 18/600 | 123,568 | 123,568 | AYNI |
| Bağlantılar (satır merkezi) | 651 | 651 | AYNI |
| Sürüm satırı 14 px | 123,692 | 123,692 | AYNI konum; metin GEREKÇELİ FARK |
| Sağ panel: şekiller, cümle 36/700, alt cümle, 3 rakam 26/600 + 14 | 750,594 / 778 / 816 | birebir | AYNI |
| Dil seçici / kısıt özeti | yok | sağ üst / dip | GEREKÇELİ FARK |

## 9. Gerçek tarayıcı ölçümü — `betikler/sikisma-olc.js` (K1–K11 + yatay)
Başsız Chrome'da aracın TAMAMI yüklendi ve `__olcTam()` çalıştı (`olc-sikisma.js`). Mod, "/" sayfasında gerçek üst bar düğmesiyle seçildi, `data-mod` doğrulandı ve sonra `/ekranlar`'a geçildi. Tema `kuyumcuTema.uygula` ile ve `prefers-color-scheme` öykünmesiyle verildi. Kayıtlar: `matris-e.txt`, `matris-g.txt`, `tek.txt`; görüntüler `scratchpad/tk4/png/matris/`.

| Rota | Genişlik | Mod | Tema | K1 | K3 taşan/kırık | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /ekranlar | 1440 | BASİT, PRO | açık, koyu | 0 | 0/0 | 1.0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar | 1280 | BASİT, PRO | açık, koyu | 0 | 0/0 | 1.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar | 375 | BASİT, PRO | açık, koyu | 0 | 0/0 | 4.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /giris | 1440 / 1280 / 375 | (oturum öncesi) | açık, koyu | 0 | 0/0 | 1.0 / 1.0 / 1.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Toplam 18 kombinasyon, hepsinde K sayaçları 0 ve yatay kaydırma 0. K2 ölçülmedi: 120 px'ten uzun çerçeveli kart yok; tam genişlik ızgara, dar sütun durumu yok.

- **K8 notu:** Araç K8'i yalnız ekranda seçili bir "Basit" düğmesi varken sayar. /ekranlar ve /giris tam ekran olduğu için bu düğme yok, araç 0 döndürür. Bu yüzden jargon deseni zorla da koşuldu (`K8zorla`). Yalnız aracın bilinen yanlış pozitifleri çıktı:
  - "Kâr-zarar" → `r-zarar` (â harfi ASCII `\b` sınırını bozuyor; tuvalin kendi etiketi)
  - "E-posta" (T-K3 § 14 md.2'deki aynı kural)
  - Gerçek jargon 0.
- 375 px: tek sütun karo yığını (app.css), yatay 0. Giriş tek sütun: kart önce, panel altta.
- K4 = 4.2 (375 /ekranlar): 38 karo tek sütunda. Tuvalde 375 yok; app.css ≤600 px kuralı değişmedi.
- **Tarayıcı bölmesi çapraz kontrolü:** kendi sekmem `tab-69`, 1440×900, vekil 5161 üzerinden /ekranlar.
  - K1 0 · K3 0/0 · K5 0 · K9 0 · yatay 0 · üst şerit 68 px.
  - İlk 6 karo [32,117,163,68] [205,…] [379,…] [552,117,182,68] [743,…] [32,228,…]; başsız ölçümle birebir.
  - Bölme gizli olduğu için bölmede ekran görüntüsü alınamadı ("pane is not displayed"). Görsel kanıt başsız Chrome görüntüleridir. Sekme kapatıldı, görünüm öykünmesi sıfırlandı.

## 10. Canlı doğrulama (API 5160 · vekil 5161 · kuyumcu_net)
Kiracı `PAKETT-K4-TEST Kuyumculuk` (`01M3AVETE6QMNR7ABVTQZWPZFQ`), `demo-tohumla.js --prova-kiraci` ile ürün uçlarından kuruldu: 6 cari, 12 ürün, 9 satış.

| Adım | Beklenen | Gerçek |
|---|---|---|
| /giris boş "Giriş yap" (başsız Chrome, gerçek tıklama) | istek yok, alan altı Türkçe mesaj + bant | `alan-hata-giris-eposta: Bu alan zorunludur.` · `alan-hata-giris-sifre: Bu alan zorunludur.` · bant "Form eksik ya da hatalı — işaretli alanları düzeltin." · yol /giris |
| /giris doğru e-posta + parola, "Giriş yap" | POST /auth/giris 200 → Panel | yol `/`, başlık "Panel — MİHENK", üst bar var |
| `POST /api/v1/auth/giris` (curl, doğru) | 200 | 200; gövde anahtarları `accessToken, kullanici, refreshToken`; rol SAHIP (jeton yazılmadı) |
| Yanlış parola | 401 | 401 `{"message":"E-posta veya şifre hatalı",…}` |
| Telefon biçimli kullanıcı adı | 400 (e-posta etiketi gerekçesi) | 400 `{"alanlar":{"email":["Geçerli bir e-posta adresi giriniz"]}}` |
| `GET /tercihler/benim` (yıldızın kaynağı) | 200 | 200 `{"tercihler":{}}` |
| `GET /yetkiler/benim` (karo kapısı) | 200 | 200 `{"moduller":["banka","bildirim","cari",…]}`; ipucu "38 ekranın hepsi burada" |
| `GET /genel/kisitlar` (oturumsuz) | 200 | 200 `{"kaynak":"docs/HARIC-LISTESI.md","toplam":8,…}`; ekranda "Bu kurulumda HARİÇ olanlar (8)" |

SQL kanıtı:
```
refresh_tokenler WHERE tenant_id=… → adet 12 · son 2026-09-24 23:46:39 UTC · HeadlessChrome UA 1
```
UI girişi ve curl girişleri gerçek oturum kaydı üretti.

Temizlik:
- 5160 (PID 66300) ve 5161 (PID 66979) `kill` ile durduruldu; `lsof` ikisi için de boş.
- `kiraci-temizle.sh --uygula` çalıştırıldı. Ardından elle doğrulandı: 70 base tabloda (tenant_id kolonlu) kiracı satırı **0**.
- `kiraci_davetleri`'nde bu kiracının 1 davet satırı kalmıştı (`kullanan_tenant_id`); `DELETE` ile silindi (row_count 1).
- tenants: başlangıç **12**, son **12**. Kümede başka şeritlerin değişikliği görüldü: listede `PAKETT-S2-TEST Duzeltme` belirdi. Bu paket yalnız kendi kiracısını açtı ve sildi. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.

## 11. Denetim
`dotnet/denetim.sh` → rc=0, **DENETİM TEMİZ** (288 ✓). `P-37c: SAYFA_BASI_KAPSAM 44/44 ekran (+3 yazılı muafiyet)`: Ekranlar muafiyeti değişmedi, Giriş SayfaBasi taşıyor.

## 12. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Web/Pages/Ekranlar.razor`: marka, Vitrin yazımı, ikonlu ızgara sınıfı, başlık notu
- `dotnet/Kuyumcu.Web/Pages/Giris.razor`: marka, Parola, Giriş yap, PageTitle
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-kabuk.css`: sona T-K4 bloğu (tek parça, geri dönüş = bloğu silmek)
- yeni `dotnet/Kuyumcu.Core/I18n/Sozlukler.EkranlarGirisTuval.cs`
- `dotnet/Kuyumcu.Core/I18n/GirisSozlugu.cs`: HosGeldin ve SurumSatiri `MarkaYazimi`'ndan
- yeni `dotnet/Kuyumcu.Tests/TK4EkranlarGirisTuvalTestleri.cs`
- testler: `S1dEkranlarTestleri.cs`, `S1eGirisTestleri.cs`, `UX1C1EkranTestleri.cs` (yalnız düğme metni satırı)
- görseller: `ham/TUVAL-KARSILASTIRMA/T-K4-Ekranlar-1440.png`, `T-K4-Giris-1440.png`, `T-K4-koyu-1440.png`
- bu rapor

Ekranlar.razor, tuval-kabuk.css, GirisSozlugu.cs ve EkranlarSozlugu.cs git'te zaten izlenmeyen dosyalar (önceki paketler). app.css, Sozlukler.cs hub'ı, Form bileşenleri, ikon sprite'ı, MenuTanimi ve UstBar/AnaYerlesim DEĞİŞMEDİ. Proje ağacına yedek ya da geçici dosya bırakılmadı; ağaçta görülen `*.mutorj` / `*.bak` dosyaları bu paketin değil.

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Ekranlar: koyu şerit (Mihenk, Tüm ekranlar, arama yer tutucusu, sağda ipucu) | SAĞLANDI | § 8, TK4_01/13, S1d |
| Grup başlıkları BÜYÜK HARF, karo genişlik/yükseklik/ikon kuralı | SAĞLANDI (Sipariş karosu K3 gerekçeli) | § 8, TK4_03/10/11 |
| Alt küçük karolar Kılavuz · Ayarlar · Vitrin panosu | SAĞLANDI | § 8, TK4_02 |
| Kenar çubuğu yok, geri dönüş yolu üst şeritte | SAĞLANDI | marka bağlantısı `/`, aria "Panele dön" (S1d testi) |
| Giriş: sol form, sağ koyu panel, metinler | SAĞLANDI (E-posta ve saklama cümlesi gerekçeli) | § 8, TK4_04/05 |
| Giriş davranışı ve güvenlik değişmez | SAĞLANDI | TK4_07/08, canlı § 10 |
| Tuvalde olmayan özellik silinmez | SAĞLANDI | dil seçici, kısıt beyanı, kayıt, MFA yaşıyor |
| Mevcut id'ler korunur | SAĞLANDI | TK4_06 |
| app.css / hub dokunulmaz; sabit renk ve satır içi stil yok | SAĞLANDI | TK4_09, TK4_15 |
| Gerçek tarayıcı 1440/1280/375 × BASİT/PRO × açık/koyu, K1..K11 = 0 | SAĞLANDI | § 9 |
| Görsel karşılaştırma + bölge tablosu | SAĞLANDI | § 8 |
| Paralel kapı (1) build 0 hata | SAĞLANDI | § 6 |
| (2) testlerim 0 başarısız | SAĞLANDI | 15/15, 374/374 |
| (3) sayaçlarım denetim.sh'ta ✓ | SAĞLANDI | § 11 |
| (4) mutasyon | SAĞLANDI | 15/15 (§ 7) |
| (5) canlı | SAĞLANDI | § 10 |
| (6) test kiracısı silindi | SAĞLANDI | § 10 |
| (7) yeni kırmızı yok | SAĞLANDI | tam süit 5845/5845 |

## 14. Açık kalanlar (sahibiyle)
1. **Günlük iş karo glifleri** (tuval: ev / kart / kilit-çanta / pano-onay): sprite'a (`wwwroot/img/ikon.svg`) sembol eklenmesi ve `Ikonlar`/`MenuTanimi` değişikliği gerekir. Kenar çubuğunu da etkiler. Sahibi **orkestratör / T-KABUK sprite sahibi**.
2. **`sikisma-olc.js` K8:**
   - Tam ekran sayfalarda (Basit düğmesi yok) K8 hiç sayılmıyor.
   - Tire kuralı Türkçe harfte (`Kâr-zarar` → `r-zarar`, `E-posta`) yanlış pozitif veriyor.
   - Sahibi **ölçüm aracı sahibi / orkestratör**. Öneri: mod `html[data-mod]`'dan okunsun, harf sınıfı `\p{L}` olsun.
3. **"Şifre" / "Parola" tutarlılığı:** giriş ekranı artık tuval gereği "Parola" diyor. Ayarlar, Kullanıcılar ve sunucu hata metinleri "Şifre" (hub). Ürün genelinde tek ad için hub göçü gerekir. Sahibi **orkestratör** (plan 1.8 göçü).
4. **Kurucu teyidi:**
   - "Verileriniz Türkiye’de saklanır" cümlesi (KVKK beyanı, § 3).
   - "E-posta ya da telefon" ile giriş (telefonla giriş özelliği yok).

## 15. Kurucuya / mali müşavire notlar
Mevzuat hükmü içeren değişiklik yok; mali müşavir teyidi gerekmez.

Kurucuya: Tüm ekranlar ve Giriş sayfaları tuvalle aynı ölçüde (1440×900 yan yana görüntüler § 8). Kalan farklar:
- "E-posta" etiketi, çünkü sistem telefonla girişi kabul etmiyor.
- Veri saklama cümlesi, doğrulanamadığı için yazılmadı.
- Dil seçici ve "Hariç olanlar" özeti köşelerde duruyor.
- "Sipariş ve tamir" karosu yazı tek satır kalsın diye biraz geniş.
- Dört karo ikonu tuvaldekinden farklı çiziliyor; ikon dosyası ortak (açık kalan 1).
