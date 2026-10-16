# PAKET S1-e — Giriş ekranı: iki panel

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** S-1 KABUK
**Durum beyanı:** BİTTİ. Paket kapısının 7 maddesinin kanıtı aşağıda.
- Tam süitte kalan 4 kırmızı PARA şeridine ait (`RaporSetiTestleri.P48_*`).
- denetim.sh'taki tek ✗ `P-31` (Destek) şeridine ait.

Ayrıntılar § 7.3, § 9 ve § 14'te.

---

## 1. Kaynaklar ve spesifikasyonun koda karşı doğrulanması

**Okunan kaynaklar**
- Tek doğruluk kaynağı: `dokumanlar/02-mimari/sadelestirme/tuval/project/Giris.dc.html` ve `canvas.json` ("1 · Giriş", not n6).
- `mockup-S1.html` ile `mockup-tema-8.html` okunmadı (görev notu).
- Plan § 5A "Giriş ekranı" satırı ve S-1 tablosu 1.9: "Tek kart + sakin görsel; 375 px taşması kapanır; 4 genişlikte taşma 0; dokunma hedefi ≥ 44 px".

**Tuvalden okunan ölçüler**

| Öğe | Tuval |
|---|---|
| Izgara | `minmax(0,1fr) minmax(0,1.1fr)`, zemin `#f2f4f7`, gövde 18 px / 1,45 |
| Sol sütun | 56 px iç boşluk; kart 440 px, öğeler arası 30 px |
| Marka | 44 px rozet (`#2457d6`, 11 px yarıçap, 22 px elmas) + Sora 24/700 |
| Başlık | Sora 34/700 "Mihenk’e hoş geldin" + 18 px `#5a6577` "Dükkânın hesabıyla gir." |
| Form | Aralık 18 px; etiket 15 px `#5a6577`, etiketle alan arası 7 px; kontrol 54 px, 0 16 px iç boşluk, 9 px yarıçap, 18 px; "Göster" kutunun içinde 15 px vurgu rengi |
| Birincil düğme | 54 px, `#2457d6`, beyaz 18/600 |
| Bağlantılar | "Parolamı unuttum" (vurgu) / "Yeni dükkân kur" (`#5a6577`), 15 px |
| Sürüm satırı | 14 px |
| Sağ panel | `#14213d`, 64 px iç boşluk, içerik altta, 3 döndürülmüş süs şekli |
| Panel metinleri | Sora 36/700 cümle + 18 px `#aab7cf` alt cümle + 3 rakam (26/600; etiket 14 px `#8ea2c4`, tabular-nums) |

**"Mevcut durum" koda karşı**
- `Pages/Giris.razor` eski düzendeydi:
  - sol yarıda koyu gradyanlı illüstrasyon ve özellik listesi (≥1024 px'te görünür, altında gizli),
  - sağda sekme hapları ile giriş/kayıt formu.
- Satır içi `style=` ve emoji rozeti vardı.
- Doğrulandı. Paralel şerit yasak listesinde değil.

**Bağımlılıklar koda karşı doğrulandı**
- Kendi kendine parola sıfırlama ucu **yok**. `Modules/Auth` içinde yalnız MFA sıfırlama var.
- Yöneticinin verdiği sıfırlama var: `PATCH /kullanicilar/{id}/sifre`, `[RolGerekli("SAHIP","MUDUR")]`.
- `GirisDto` yalnız e-posta kabul eder (`[EmailAddress]`); telefonla giriş yoktur.
- `/meta/surum` oturum ister (`[AllowAnonymous]` değil).

**Mevzuat:** Arayüz paketi. Mevzuat hükmü yok, kural tetiklenmedi. "Verileriniz Türkiye'de saklanır" cümlesi bu yüzden **yazılmadı** (§ 4 S4).

## 2. Yapılanlar

| Dosya | İş |
|---|---|
| `Kuyumcu.Web/Pages/Giris.razor` | Tuvale göre yeniden kuruldu. Sol `main#giris-sol > #giris-kart`: marka (`h1.giris-marka-ad`), `SayfaBasi` ("… hoş geldin"), e-posta, parola (Göster'li), `#giris-gonder`, `#giris-parola-unuttum` / `#giris-yeni-dukkan`, `#giris-surum`. Sağ `aside#giris-panel`: `#giris-panel-baslik`, `#giris-panel-alt`, `#giris-rakamlar` (3 rakam) ve 3 süs şekli (`aria-hidden`). Kayıt formu "Yeni dükkân kur"un arkasında (`#kayit-form`, `#kayit-gonder`, `#giris-girise-don`). MFA ikinci adımı ve P-30 kısıt beyanı (katlanır `details#giris-kisit`) aynen yaşıyor. **Satır içi style 0, sabit renk 0, SVG illüstrasyon kalktı.** Kod tarafı (giriş, MFA, kayıt, kısıt çekimi) değişmedi. Eklenenler: parola yardımı anahtarı, `SurumMetni()` (derleme sürümünden) ve sekme değişince yardımın kapanması. |
| `Kuyumcu.Web/Bilesenler/Form/Sifre.razor` | İsteğe bağlı `GosterEtiketi` / `GizleEtiketi` parametreleri eklendi. Doluysa kutunun içinde `#<id>-goster` düğmesi çizilir (`aria-pressed`, `aria-controls`) ve girdi `text`/`password` arasında değişir. **Boşsa bileşen eskisiyle birebir aynıdır**; Kullanıcılar ve Ayarlar ekranları etkilenmez (test ile kilitli). |
| `Kuyumcu.Core/I18n/GirisSozlugu.cs` (YENİ) | Tüm yeni metinler TR+EN. `Sozlukler.cs` hub'ına dokunulmadı. |
| `Kuyumcu.Web/wwwroot/css/app.css` | Sona tek **"S1-e"** bloğu eklendi (geri dönüş = bloğu kaldırmak). Eski `.giris-*` kuralları silinmedi, geçersiz kılındı. Renkler yalnız belirteç. Sağ panel zemini `--kenar-yuzey` olduğu için 8 şemada kendiliğinden değişir. ≤1199 px'te boşluklar daralır. ≤768 px'te tek sütun olur: kart önce, panel altta, süsler gizli. 44 px dokunma hedefi her genişlikte geçerli. |
| `Kuyumcu.Tests/S1eGirisTestleri.cs` (YENİ) | 22 test yöntemi, 27 test durumu (§ 7). |
| `Kuyumcu.Tests/MobilPariteTestleri.cs` | `B32` Razor sabit renk tavanı 14 → **0** (cırcır gerçeğe indi; denetim.sh P-18 tavanı zaten 0'dı). |

## 3. Tuval ↔ canlı ölçüm (Chrome, Platin aydınlık, 1440×900, 5194 gerçek sunucu)

| Tuval | Belirteç | Ölçülen |
|---|---|---|
| Gövde zemini `#f2f4f7`, metin `#172033` | `--yuzey-govde`, `--metin` | rgb(242,244,247) · rgb(23,32,51) |
| Panel `#14213d` | `--kenar-yuzey` | rgb(20,33,61) |
| Düğme `#2457d6` | `--vurgu` / `--vurgu-metin` | rgb(36,87,214) |
| Kart 440 px | — | 440 px |
| Kontrol 54 px (e-posta, parola kutusu, düğme) | — | 440×54 · 440×54 · 440×54 |
| Başlık Sora 34 · panel cümlesi 36 · gövde Manrope 18 | `--yazi-marka` / `--yazi-ailesi` | Sora 34px · 36px · Manrope 18px |
| Fontlar vendored | `font-src 'self'` (CSP başlığında doğrulandı) | `document.fonts`: Manrope + Sora loaded |
| Sütunlar 1fr / 1.1fr | — | 685,71 / 754,29 px |
| Sayfa 900 px'e sığar | — | `scrollHeight` 900 |

## 4. Sapmalar ve gerekçeleri

| # | Tuval | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | "Mihenk" | `Marka.Ad` ("MİHENK"); başlık "MİHENK’e hoş geldin" | Marka tek kaynaktan gelir (S1-b S3, S1-d S2 ile aynı). |
| S2 | Etiket "E-posta ya da telefon" | "E-posta" (`Dil.T.Auth.Eposta`) | Sunucu yalnız e-posta kabul eder (`GirisDto [EmailAddress]`). Telefon yazan kullanıcı 400 alırdı. Özellik eklemek bu paketin kapsamı dışında. |
| S3 | "Mihenk 1.0 · ByGatech · …" | `MİHENK 1.0 · ByGatech`; sürüm **derlemeden** türer | Sürüm sabiti yazılmaz (T-4a). `/meta/surum` oturumsuz okunamıyor. Onu anonime açmak ortam ve migration bilgisini sızdırırdı. İstemci derlemesinin `InformationalVersion`'ı kullanıldı; API sürümüyle eşitliği `Istemci_surumu_API_surumuyle_AYNI` testiyle kilitli. |
| S4 | "Verileriniz Türkiye’de saklanır." | **Yazılmadı** | Ürün müşterinin kendi sunucusunda kuruluyor (`docs/DEPLOY.md`: yerel MySQL). Verinin nerede saklandığı kuruluma bağlı ve koddan doğrulanamaz. Bu bir KVKK beyanı sayılabilir; mevzuat kuralı gereği kaynaksız yazılmaz. Kurucu teyit ederse `GirisSozlugu.SurumSatiri`'na tek satırla eklenir. |
| S5 | Parola kutusunda 15 px "Göster" | Aynı, 44 px yükseklikte | Plan 1.9: dokunma hedefi ≥ 44 px. |
| S6 | Bağlantılar düz `<a>` | Bağlantı görünümlü `<button>`, 44 px kutu | "Parolamı unuttum" için gidilecek sayfa yok. Düğme, doğru yolu (SAHİP/MÜDÜR → Kullanıcılar → Şifre Sıfırla) **görünür yazar**; sahte bir sıfırlama ucu açılmadı (DoD EK md.1). "Yeni dükkân kur" kayıt formunu açar. |
| S7 | Girdi kenarlığı `#c9d0db` | `--kenarlik` (#82868e) | `#c9d0db` beyaz üstünde yaklaşık 1,5:1'dir ve WCAG 1.4.11'in 3:1 eşiğini geçmez. Mevcut `.girdi` kuralı ve kontrast testi ≥3:1 istiyor. Tuvalle tek görsel fark budur. |
| S8 | Sürüm satırı `#8a94a6` | `--metin-soluk` (#5a6577) | `#8a94a6` açık zeminde yaklaşık 2,8:1'dir; 14 px metin için AA (4,5:1) altında kalır. Ölçülen 5,35:1. |
| S9 | Orta kare kenarı `#3b5088` | `--menu-aktif-yuzey` (#2b3f6b) | Bu renk için belirteç yok ve yeni belirteç 8 şema × 2 tema demektir. Süs şeklidir, anlam taşımaz; şemayla birlikte değişir. |
| S10 | Sayfanın h1'i yok (tuval `div`) | Marka adı `h1`, başlık `SayfaBasi` (h2) | `#sayfa-basi-giris-baslik` id'si ve P-37c SAYFA_BASI_KAPSAM korunmalı. Sayfanın bir h1'i olmalı. |
| S11 | Tuvalde dil seçici, kayıt formu, MFA, kısıt beyanı yok | Dil seçici kartın sağ üstünde. Kayıt formu "Yeni dükkân kur" arkasında. MFA aynı yerde. Kısıt beyanı sürüm satırının altında **katlanır** (`details`); özet "Bu kurulumda HARİÇ olanlar (8)". Liste okunamazsa beyan **açık** başlar. | Plan § 3 md.1: özellik silinmez, katlanır. P-30/BK-47 "giriş ekranında okunur" şartı sürüyor. Eski düzende beyan <1024 px'te hiç görünmüyordu; şimdi her genişlikte bir dokunuşla açılıyor. |
| S12 | "Giriş yap" | "Giriş Yap" (`Dil.T.Auth.GirisYap`) | Hub'a dokunmak yasak. Aynı kavram tek addan gelir (UX-S31). Göç (plan 1.8) yapılınca düzelir. |
| S13 | Sağ panelde `overflow:hidden` | `contain: paint` | Etki aynı: süs şekilleri kırpılır, canlı ölçümde taşma 0. Ancak S1-c ve S1-d CSS testleri blok başından dosya sonuna kadar tarar ve S1-c bloğunda "overflow" yasaktır. İlk tam süitte `S1c_CSS_blogu…` bu yüzden kırmızıydı (§ 7.3); düzeltildi. |

**Tuval metin iddiaları (kurucuya not):** "3 tık nakit satış" ve "30 dk öğrenme" tuvalden birebir alındı, kurucu onaylı metindir. Ancak ikisi de henüz **ölçülmedi**: S-2 tezgâh fişi ve S-5 kronometre ölçümü yapılmadı. "0 silinen kayıt" bugün doğrudur: finansal kayıtlarda kalıcı silme yasak, düzeltme storno ile yapılıyor. Müşteriye göstermeden önce ilk ikisinin plan § 9 ölçüm defterinde doğrulanması gerekir.

## 5. Migration
**Gerekmedi.** Şema değişikliği yok.

## 6. DoD sözleşmesi (Bertan koşulu)
Bu paket **yeni varlık ya da kayıt türü doğurmaz**.

| Kayıt | Düzenleme | Silme / pasif | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|
| Kiracı + sahip kullanıcı (kayıt formu, mevcut `POST /auth/kayit`) | Mevcut Ayarlar/İşletme ve Kullanıcılar ekranları (değişmedi) | Mevcut yollar (değişmedi) | Davet jetonu | `EKLEME Tenant/Sube/Kullanici` (canlıda görüldü) | Form alanları eskisiyle aynı |
| Parola | `PATCH /kullanicilar/{id}/sifre` (SAHİP/MÜDÜR) ve `/ben/sifre` | — | `[RolGerekli("SAHIP","MUDUR")]` | Mevcut | "Parolamı unuttum" bu yolu yazar |

**Her düğme iş yapar** (DoD EK md.1):
- Göster: tipi değiştirir.
- Parolamı unuttum: yardımı açar ve kapatır.
- Yeni dükkân kur / Girişe dön: formu değiştirir.
- Giriş yap: POST atar; form boşsa alan altına mesaj yazar.

`Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar` testi her düğmeyi tek tek tıklar.

## 7. Testler

### 7.1 Paket testleri — `S1eGirisTestleri` 27/27 yeşil (22 yöntem; bir Theory 6 durum)
- **Tuval:** `Tuval_IKI_PANEL…`, `Sag_panel_TUVAL_metinleri_ve_UC_rakam`, `Giris_formu_EPOSTA_PAROLA_ve_TEK_birincil_dugme`
- **Sürüm:** `Surum_satiri_DERLEMEDEN_turer…`, `SurumMetni_cekirdek_surumu_Major_Minor_verir` (6 durum), `Istemci_surumu_API_surumuyle_AYNI`
- **Göster:** `Goster_parolayi_GORUNUR_yapar_ve_GERI_gizler`, `Goster_yazilan_degeri_KORUR_ve_gonderir` (JSON gövde ayrıştırılıyor)
- **Bağlantılar:** `Parolami_unuttum_DOGRU_YOLU_yazar_istek_atmaz_ve_kapanir`, `Yeni_dukkan_kur_KAYIT_formunu_acar_Girise_don_geri_getirir`
- **POST yolları:** `Kayit_formu_POST_auth_kayit_atar_govde_ayristirilir`, `Giris_POST_auth_giris_atar_ve_panele_gider`, `Giris_reddedilirse_HATA_BANDI_cizilir_yonlenmez`
- **Düğme sözleşmesi:** `Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar`
- **Kısıt beyanı:** `Kisit_beyani_KATLANIR_ozet_sunucu_sayisini_tasir`, `Kisit_ucu_patlarsa_beyan_ACIK_baslar_hata_gorunur`
- **Id ve dil:** `Mevcut_id_envanteri_KORUNUR`, `Ingilizce_dilde_metinler_EN_sozlukten`, `Sozluk_IKI_DILDE_dolu_ve_farkli`
- **Ortak bileşen:** `Sifre_bileseni_GosterEtiketi_yoksa_ESKISI_GIBI_dugmesiz`
- **Kaynak ve CSS:** `Sayfada_satir_ici_style_ve_SABIT_renk_YOK`, `S1e_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir`

Sahte API'nin `{}` varsayılanı yok: `/genel/kisitlar`, `/auth/giris` (200 ve 401, sunucunun `message` şekliyle) ve `/auth/kayit` için gerçek yanıtlar veriliyor.

### 7.2 Etkilenen mevcut testler — son koşum 188/188 yeşil
Kapsam: `S1cUstBarTestleri`, `S1dEkranlarTestleri`, `S1eGirisTestleri`, `P30GirisEkranTestleri` (kısıt listesi / ham kod / hata), `UX1C1EkranTestleri` (Giriş SayfaBasi, alan altı mesaj), `UX1C4EkranTestleri` (C4_60 MFA ikinci adım), `MobilPariteTestleri`, `TasarimBelirtecTestleri`, `GirdiGocuTestleri`.

### 7.3 Tam süit
`dotnet test … -v n --logger trx --results-directory scratchpad/trx-pS1-e` (KUYUMCU_TEST_DB=kuyumcu_test_s, `build-server shutdown` sonrası):

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| S1-d sonu (devir) | 4241 | 4239 | 2 (`SozlesmeKapsamTestleri` ×2) |
| S1-e (`trx-pS1-e/…21_36_24.trx`) | **4361** | **4356** | **5** |

| Kırmızı | Sahibi | Bu paketin ürünü mü? |
|---|---|---|
| `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` ("overflow" bulundu) | **S1-e** (benim bloğum; S1-c testi dosya sonuna kadar tarıyor) | EVET, **düzeltildi** (S13). Yeniden koşumda yeşil (§ 7.2, 188/188). |
| `RaporSetiTestleri.P48_01_TL_sifir_has_negatif_satir_CIKIS_doner` (beklenen CIKIS, gelen GIRIS) | PARA şeridi P-48. `RaporSetiTestleri.cs` 21:11'de, `Modules/Rapor/SatisRaporlari.cs` 21:17'de değişti. | HAYIR (S1-e Rapor'a dokunmadı) |
| `RaporSetiTestleri.P48_02_Zit_isaretli_satir_KARISIK_doner` | Aynı | HAYIR |
| `RaporSetiTestleri.P48_03_Iki_bacak_sifirsa_yon_null_doner` | Aynı | HAYIR |
| `RaporSetiTestleri.Islem_defteri_TL_isaretini_korur` | Aynı | HAYIR |

**Madde (7) — yeni kırmızı üretmeme:** Bu paketin ürettiği tek kırmızı düzeltildi ve yeşil olduğu doğrulandı. Kalan dördü PARA şeridinin yarım işi. Fark = ∅. **Sağlandı.** Devir listesindeki iki `SozlesmeKapsam` kırmızısı bu koşumda yeşil.

## 8. Mutasyon kanıtları
Yöntem:
1. Kaynak `scratchpad/mut-yedek/S1-e/<kod>.orj` olarak kopyalandı.
2. `perl -0777 -pi` ile değişiklik yapıldı; eşleşme sayısı önce **1** olarak doğrulandı.
3. `dotnet test --filter S1eGirisTestleri` koşuldu.
4. Yedek geri kopyalandı ve `cmp` ile doğrulandı. **15/15 AYNI.**

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M1e-1 | `Sifre.GorunurlukDegistir` → etkisiz | `Goster_parolayi_GORUNUR_yapar_ve_GERI_gizler`, `Her_dugme_…` |
| M1e-2 | `ParolaYardimDegistir` → boş | `Parolami_unuttum_DOGRU_YOLU…`, `Her_dugme_…` |
| M1e-3 | "Yeni dükkân kur" → `SekmeSec("giris")` | `Yeni_dukkan_kur…`, `Kayit_formu_POST…`, `Mevcut_id_envanteri_KORUNUR`, `Her_dugme_…` |
| M1e-4 | kısıt `open` → `@(false)` | `Kisit_ucu_patlarsa_beyan_ACIK_baslar_hata_gorunur` |
| M1e-5 | `SurumMetni` → Major.Minor.Build | `SurumMetni_cekirdek…` (3 durum) |
| M1e-6 | CSS kart 440 → 480 px | `S1e_CSS_blogu_TUVAL…` |
| M1e-7 | CSS ≤768 tek sütun kaldırıldı | `S1e_CSS_blogu_TUVAL…` |
| M1e-8 | `HosGeldin` → "Hoş geldin" (marka yok) | `Tuval_IKI_PANEL…`, `Sozluk_IKI_DILDE…` |
| M1e-9 | 3. rakam ("0 silinen kayıt") silindi | `Sag_panel_TUVAL…`, `Sozluk_IKI_DILDE…` |
| M1e-10 | Sifre: göster düğmesi her zaman | `Sifre_bileseni_GosterEtiketi_yoksa_ESKISI_GIBI_dugmesiz` |
| M1e-11 | `SekmeSec` yardımı kapatmıyor | `Yeni_dukkan_kur…` |
| M1e-12 | kayıt düğmesi → `Dil.T.Auth.IsletmeKur` (iki ad) | `Yeni_dukkan_kur…` |
| M1e-13 | `KisitOzet` sayıyı yazmıyor | `Kisit_beyani_KATLANIR…` (**2. koşum**, aşağıda) |
| M1e-14 | sürüm satırı `SurumMetni(null)` | `Surum_satiri_DERLEMEDEN…` |
| M1e-15 | süs şeklinde `aria-hidden` kaldırıldı | `Sag_panel_TUVAL…` |

**Test açığı bulundu ve kapatıldı:** M1e-13'ün ilk koşumunda 27/27 yeşil kaldı. Sebep: beklenen değer, mutasyona uğrayan `GirisSozlugu.KisitOzet` ile hesaplanıyordu (S1-d M1d-2 ile aynı tuzak). Test bağımsız beklenenle güçlendirildi (`"{baslık} (8)"`, `KisitOzet("A",8)=="A (8)"`) ve mutasyon yeniden koşuldu; kırmızı oldu.

**Eşzamanlı şerit çakışması:** M1e-8 ve M1e-15'in ilk koşumları derleme kilidinden sonuç vermedi:
- `MSB3030 apphost bulunamadı`
- `blazor.boot.json being used by another process`

Bu koşumlar kırmızı sayılmadı. Yedekler geri kondu (cmp AYNI) ve mutasyonlar yeniden koşuldu. Tablo ikinci koşumları gösteriyor.

Son mutasyondan sonra `dotnet build Kuyumcu.sln` **0 uyarı, 0 hata** verdi.
- İlk derlemede tek uyarı vardı: `Islemler.razor(1128) CS0162`. Başka şeridin dosyası; S1-e açmadı.

## 9. Denetim
`bash dotnet/denetim.sh`, üç koşum:

**1. koşum (kod sonrası):** paketin sayaçları ✓. ✗ olarak `Doviz kolonu atama 4` ve `P-08 HARETURU_DISI_SABIT` görüldü; ikisi de başka şeritlerin (S1-d raporunda da vardı).

**2. koşum (CSS düzeltmeleri sonrası):** **DENETİM TEMİZ** (278 ✓).

**3. (son) koşum:** `DENETİM BAŞARISIZ`, tek ✗: `P-31: TANILAMA_PAKETINDE_SIR 1`. Bu `Modules/Destek` (DALGA 7c P-31) şeridinin; S1-e oraya dokunmadı.

**Paketin kendi sayaçları (son koşumda hepsi ✓):**

| Sayaç | Değer |
|---|---|
| `P-18 SABIT_RENK` | 0 (Giris.razor'da sabit renk 0) |
| `P-11 SATIR_ICI_STYLE` | 116 / 143 (S1-d'de 125; Giris.razor 9 → 0) |
| `P-36 HAM_INPUT` | 42/42 |
| `HAM_INPUT_SERBEST` | 0 |
| `P-36b GIRDI_AILESI_TEK_KAYNAK` | 1 (satır içi renk/px 0) |
| `P-37c SAYFA_BASI_KAPSAM` | 45/45 |
| `SAYFA_BASI_DISI_DUZEN` | 0 |
| `@onclick'siz <button>` | 71/71 |
| `app.css ham hex` | 25/25 |
| `index.html satır içi script` | 0 |

## 10. Canlı doğrulama

### 10.1 API (port **5194**, `--no-build`, Development, gerçek MySQL `kuyumcu_net`)
Kiracı **PAKETS1-e-TEST Kuyumculuk** (`01M37RK83CKBJN3RQ41ATKW2GN`). Sistem anahtarı ve parola rastgele üretildi, yalnız scratchpad dosyasında tutuldu ve ekrana yazılmadı; iş sonunda dosyalar boşaltıldı.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /giris` | 200, Blazor kabuğu; CSP `font-src 'self'` | **200**, `<title>MİHENK</title>` + `blazor.webassembly.js`; CSP `font-src 'self'`, `script-src 'self' 'wasm-unsafe-eval'` |
| 2 | `GET /css/app.css` | 200; kaynakla birebir; S1-e bloğu | **200**, `cmp` **AYNI**, "S1-e — GİRİŞ EKRANI: İKİ PANEL" 1 eşleşme |
| 3 | `GET /font/manrope-latin.woff2`, `/font/sora-latin.woff2` | 200 woff2 | **200** font/woff2 24576 B · **200** 25240 B |
| 4 | `GET /api/v1/genel/kisitlar` (oturumsuz; katlanır beyanın kaynağı) | 200, 8 satır | **200**, toplam 8, kaynak `docs/HARIC-LISTESI.md`, ilk `H-1` |
| 5 | `POST /auth/giris`, yanlış parola | 401 | **401** `{"message":"E-posta veya şifre hatalı",…}` → ekranda HataBandi |
| 6 | `POST /auth/giris`, boş gövde | 400 + `alanlar.email/sifre` | **400**, `alanlar` anahtarları `email`, `sifre` (= `GirisEsleme` → alan altı mesaj) |
| 7 | `POST /sistem/davetler` (gerekçeli) | 201 | **201**, durum `BEKLIYOR`, jeton döndü (yazılmadı) |
| 8 | `POST /auth/kayit` (kayıt formunun **birebir gövde şekli**) | 201 | **201**, `accessToken` + `tenantId` |
| 9 | `POST /auth/giris` (giriş formunun gövde şekli) | 200 | **200**, `accessToken` var, `mfaGerekli` yok, rol `SAHIP` |
| 10 | SQL `tenants` / `kullanicilar` | kayıt + son giriş | `PAKETS1-e-TEST Kuyumculuk`; `paket-s1e-test@ornek.local SAHIP son_giris dolu` |
| 11 | SQL `denetim_kayitlari` | kayıt izi | `EKLEME Tenant 1 · Sube 1 · Kullanici 1 · RefreshToken 2 · KurSnapshot 2` |
| 12 | SQL `kiraci_davetleri` | davet kullanıldı | `kullanildi_mi=1`, `kullanan_tenant_id` = test kiracısı |

### 10.2 Tarayıcı ölçümü (gerçek sunucu 5194, Chrome, Browser bölmesi)
Giriş sayfası oturum istemediği için gerçek sayfa ölçüldü; parola ya da jeton girilmedi.

| Ölçüm | Sonuç |
|---|---|
| Yatay taşma, 4 genişlik: 1440×900 / 1024×768 / 768×1024 / 375×812 | Her birinde `scrollWidth == clientWidth` (1440/1024/768/375). Sayfa dışına taşan öğe **0**. |
| 375 px kayıt formu açıkken | taşma 0 (`scrollWidth` 375) |
| Düzen | 1440: 685,71 / 754,29 px; 1024: 487,62 / 536,38 px (kart 439,62); 768 ve 375: **tek sütun**, panel kartın altında (top 741 / 703); 375'te kart 343 px |
| Kontrol yüksekliği | e-posta, parola kutusu ve düğme her genişlikte **54 px** |
| Dokunma hedefi | İlk ölçümde 1440 px'te 6 öğe 44 px altındaydı: TR/EN 32, Göster 22, iki bağlantı 22, kısıt özeti 20. CSS düzeltmesinden sonra **4 genişliğin hepsinde 44 px altı öğe 0**. |
| 375'te davranış | Göster → `type=text`, "Gizle". Parolamı unuttum → yardım metni. Yeni dükkân kur → `#kayit-davet` görünür, başlık "Yeni dükkân kur". Girişe dön → giriş formu. Boş gönderim → `alan-hata-giris-eposta/sifre` "Bu alan zorunludur." |
| Fontlar | Manrope + Sora loaded |
| 8 şema × aydınlık/karanlık (16 birleşim) | Taşma 0. Panel cümlesi kontrastı 11,11–16,86. Panel alt cümlesi ve rakam etiketi 4,50–7,87. Düğme 4,81–9,53. Sürüm satırı 5,34–8,19. Bağlantı 4,44–9,94 (**grafit/aydınlık 4,44**, § 14). |
| `overflow:hidden` → `contain: paint` değişikliğinden sonra yeniden ölçüldü | 1440: panel `scrollWidth` 754 = `clientWidth`, sayfa 1440/900. 1024 ve 375: taşma 0. Ekran görüntüsünde süsler kırpık. |

### 10.3 Temizlik
- API iki kez başlatıldı ve iki kez yalnız kendi PID'leri `kill` ile durduruldu; `lsof -iTCP:5194` **boş**. 5195–5200'e dokunulmadı.
- Test kiracısı `tenant_id` kolonu olan **70 tablodan**, kendi davetim `kiraci_davetleri`'nden ve `tenants`'tan silindi. 70 tabloda kalan satır **0**; davet kalıntısı **0**.
- `tenants`: başlangıç **10** → paket sırasında 11 → son **10**. `QA-TEST-` fikstürlerine (4 kiracı) dokunulmadı.
- Geçici sır dosyaları (anahtar, parola, gövdeler, yanıtlar) `printf "" >` ile boşaltıldı. Tarayıcı öykünmesi `desktop`a döndürüldü, sekmeler kapatıldı.

## 11. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/` altında yalnız **`Pages/Giris.razor`** açıldı. Ortak bileşen `Bilesenler/Form/Sifre.razor` geriye uyumlu bir parametreyle açıldı.

**Dokunulmayan şerit dosyaları:**
- Modules/Kasa·Vezne·Maliyet·Rapor
- Raporlar.razor, Gider.razor
- betikler/*.sh, Kurulum.razor
- SiparisDetay/Siparisler/Faturalar.razor
- Panel.razor, Sozlukler.Marka.cs, **Sozlukler.cs**
- Sunucu kodu (Kuyumcu.Api)

## 12. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından S1-e'ye ait olanlar. `Kuyumcu.Tests/`, `Bilesenler/Form/` ve `Kuyumcu.Core/I18n/` git'te izlenmediği için dosya düzeyinde yazıldı.
```
 M dotnet/Kuyumcu.Web/Pages/Giris.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                  (sonda "S1-e" bloğu)
?? dotnet/Kuyumcu.Core/I18n/GirisSozlugu.cs                 (YENİ)
?? dotnet/Kuyumcu.Web/Bilesenler/Form/Sifre.razor           (GosterEtiketi/GizleEtiketi, isteğe bağlı)
?? dotnet/Kuyumcu.Tests/S1eGirisTestleri.cs                 (YENİ)
?? dotnet/Kuyumcu.Tests/MobilPariteTestleri.cs              (B32 tavanı 14→0)
?? dokumanlar/09-uygulama-kayitlari/PAKET-S1-e-uygulama-raporu.md
```
Proje ağacına .bak, .tmp ya da .mutorj bırakılmadı; yedekler scratchpad'de.

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Giriş.razor tuval birebir: sol 440 px kart | KARŞILANDI (sapmalar § 4) | § 3; `Tuval_IKI_PANEL…`, CSS testi |
| Kart içeriği: marka, hoş geldin, e-posta/parola, Giriş yap, iki bağlantı, sürüm satırı | KARŞILANDI | § 3 |
| Sağ `#14213d` panel: cümle, alt cümle, 3 rakam | KARŞILANDI | `Sag_panel_TUVAL…`; ölçüm rgb(20,33,61) |
| Kontrol yüksekliği 54 px | KARŞILANDI | Canlı 440×54 (3 kontrol, 4 genişlik) |
| 375 px'te tek sütun, taşma 0 (şartname 12) | KARŞILANDI | § 10.2: 4 genişlik × taşma 0; M1e-7 |
| Dokunma hedefi ≥ 44 px (plan 1.9) | KARŞILANDI | § 10.2: 44 px altı öğe 0 |
| Özellik silinmez | KARŞILANDI | Kayıt, MFA, kısıt beyanı, dil seçici yaşıyor (S11); `Mevcut_id_envanteri`, P30 ve C4_60 testleri yeşil |
| Mevcut DOM id'leri değişmez; yeniler `giris-*` | KARŞILANDI | `Mevcut_id_envanteri_KORUNUR`; yeni id'ler `giris-*`, `kayit-form`, `kayit-gonder`, `<id>-goster/-kutu` |
| Sabit renk / inline style yok; belirteç | KARŞILANDI | Razor testi; P-18 0; CSS bloğunda hex/rgba yok (test) |
| CDN / npm / inline script yok; fontlar vendored | KARŞILANDI | CSP `font-src 'self'`; `/font/*.woff2` 200 |
| Tek POST yolu (Islemler Kaydet) | KARŞILANDI | Islemler.razor açılmadı; giriş sayfasının auth POST'ları eskisiyle aynı |
| Sozlukler.cs'e dokunma; ayrı dosya | KARŞILANDI | `GirisSozlugu.cs` |
| 8 şema | KARŞILANDI | Panel `--kenar-yuzey`; 16 birleşimde taşma 0 ve kontrast ölçümü (§ 10.2) |
| UX-S21 (sistemin bildiği alan istenmez) | KARŞILANDI | Sürüm derlemeden türer, elle girilmez; kısıt sayısı sunucudan |
| UX-S25/S27 | KARŞILANDI | Giriş/kayıt başarıda panele gider; hata bandı sunucu mesajını yazar |
| UX-S28 (hata boşa çevrilmez) | KARŞILANDI | Kısıt hatası: beyan açık, "(n)" uydurulmaz (M1e-4); 401 → HataBandi |
| UX-S30 (kilitli düğme sebebi) | KARŞILANDI | MFA kilit sebebi `#giris-mfa-kilit` korundu |
| UX-S31 (tek ad) | KARŞILANDI | "Yeni dükkân kur": bağlantı, başlık ve düğme aynı (M1e-12). "Giriş Yap" hub'dan (S12). |
| DoD EK md.1 (her düğme iş yapar) | KARŞILANDI | `Her_dugme_…`; M1e-1/2/3 |
| DoD EK md.3 (alan altı mesaj) | KARŞILANDI | `UX1C1…Giris_bos_formda…`; canlı 375 ölçümü |
| Mevzuat kuralı | KARŞILANDI | Doğrulanamayan veri yeri beyanı yazılmadı (S4) |
| İzolasyon (kuyumcu_test_s, 5194, build-server shutdown; 5195–5200 dokunulmadı) | KARŞILANDI | Tüm koşumlar |
| Paket kapısı 1–7 | KARŞILANDI | Build 0/0 · paket testleri 27/27 + etkilenen 188/188 · kendi sayaçları ✓ · 15/15 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 14. Açık kalanlar
1. **Ağaçtaki kırmızılar (bu paketin DEĞİL):**
   - `RaporSetiTestleri.P48_01/02/03` ve `Islem_defteri_TL_isaretini_korur`, sahibi PARA şeridi (P-48).
   - `denetim.sh` son koşumunda `P-31 TANILAMA_PAKETINDE_SIR 1`, sahibi Destek (P-31, DALGA 7c).
2. **Grafit şeması, aydınlık:** `--vurgu` 15 px bağlantı metninde gövde zemini üstünde **4,44:1**; AA eşiği 4,5. Bu bir şema belirteci sorunu. Aynı renk tüm bağlantılarda kullanılıyor; belirteç sahibi (tema sistemi) `--vurgu`'yu ya da bağlantı rengini ayarlamalı. Diğer 15 birleşim ≥ 4,52.
3. **S1-c ve S1-d CSS testleri dosya sonuna kadar tarıyor.** Sonra eklenen her blok onların kurallarına tabi oluyor (bu pakette "overflow" yasağı çarptı). Testlerin kendi bloklarının sonunda durması önerilir; o dosyalar başka paketlerin testleri olduğu için dokunulmadı.
4. **Tuval metinleri "3 tık" ve "30 dk" ölçülmedi** (§ 4 not). Demo öncesi ölçüm defterinde doğrulanmalı.
5. **Oturumlu uçtan uca akış** (girişten sonra panelin açılması) tarayıcıda denenmedi, çünkü forma parola yazmak yasak. Aynı akışın sunucu tarafı canlıda (§ 10.1 #8–9), istemci tarafı bUnit'te (`Giris_POST…`) sınandı.

## 15. Kurucuya / mali müşavire notlar
- **Mali müşavire not yok** (mevzuat hükmü yok).
- **Kurucuya:**
  - (a) Tuvaldeki "Verileriniz Türkiye’de saklanır." cümlesi yazılmadı. Ürün müşterinin kendi sunucusunda çalışıyor ve verinin yeri kuruluma bağlı. Doğru olduğu bir kurulum modeli (ör. bizim barındırdığımız sunucu) kesinleşirse tek satırla eklenir.
  - (b) "3 tık / 30 dk" rakamları ölçülünce teyit edilmeli.
  - (c) "Parolamı unuttum" bugün e-postayla sıfırlama yapmıyor; kullanıcıyı dükkân sahibine yönlendiriyor. E-postayla sıfırlama yeni bir özellik olur ve kapsam dondurma nedeniyle ekip oyu ister.
