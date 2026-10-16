# PAKET S1-c — Üst bar: kur şeridi + Yardım + Basit/Profesyonel

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** S-1 KABUK
**Durum beyanı:** BİTTİ (paket kapısının 7 maddesi aşağıda kanıtlı; ağaçtaki tek kırmızı P-30b'nin — § 7.3)

---

## 1. Kaynaklar ve spesifikasyon doğrulaması

- **Tek doğruluk kaynağı (tuval):** `dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html`, `Musteriler.dc.html`, `Tezgah.dc.html` `<header>` (basit yüz) ve `Profesyonel.dc.html` `<header>` (profesyonel yüz); `canvas.json` notu n6. `mockup-S1.html` / `mockup-tema-8.html` okunmadı (görev notu).
- **Tuvalden okunan üst bar (basit):** 68 px, beyaz, alt çizgi · başlık Sora 22/600 · şerit `HAS · GRAM 22 · USD` (etiket 14 px BÜYÜK HARF .04em ikincil renk, değer 19/600 tabular) · saat `09:02` 14 px · boşluk · `Yardım` 16 px vurgu rengi · hap `Basit|Profesyonel` (1 px kenar, 999 px yarıçap, 3 px iç boşluk, seçili = menü zemini + beyaz yazı, 15 px, 6×14 px).
- **Profesyonel yüz:** etiket 13 · değer 18 · aralık 22 · `+ EUR` · saatin yanında `14:32 · Altınkaynak` · Yardım 15 px.
- **Plan (§ S-1 md. 1.2) ile tuval arasındaki çelişki:** plan "sağda bildirim + hesap menüsü (mod/tema/dil/çıkış), mod id'leri hesap menüsünün içinde" diyor; tuval hesap menüsü çizmiyor, hap açıkta. Görev hükmü "TASLAK KAZANIR" → **hap açıkta**, hesap menüsü açılmadı (§ 4 S1).
- **"Mevcut durum" iddiaları koda karşı:** `UstBar.razor` şeridi `Gram Altın · Has Altın · Dolar · Euro · Sterlin` sırasıyla, vurgu zeminli kutuda basıyordu; son sorgu ayrı kutuda `dd.MM.yyyy HH:mm:ss` + satır içi çıplak "Son sorgu"/"Last query" metni (UX-S31 ihlali); hap ikonlu. Doğrulandı.
- **"Gram 22" kaynağı:** sunucu `/kur/guncel` yanıtı (`KurSnapshotCevap`) `ayarFiyatlari[]` içinde `K22` (milyem 916) gram fiyatını ZATEN hesaplıyordu (`KurModulu.cs` `SnapshotZenginlestir`); istemci okumuyordu. UX-S21 gereği istemcide çarpım YAPILMADI, sunucunun değeri okundu. Aynı yanıttaki `kaynak` alanı da okunmaya başlandı. **Sunucu kodu değişmedi.**
- **Mevzuat:** Paket arayüz paketidir, mevzuat hükmü içermez → mevzuat kuralı tetiklenmedi.

## 2. Yapılanlar

| Dosya | İş |
|---|---|
| `Kuyumcu.Web/Layout/UstBar.razor` | Kur şeridi tuval sırasına alındı (`#ust-kur-has`, `#ust-kur-gram22`, `#ust-kur-usd`, profesyonelde `#ust-kur-eur`), saat `HH:mm` (`#ust-kur-saat`) + profesyonelde kaynak adı (`#ust-kur-kaynak`, sözlükten; tanınmayan kod **çizilmez**, ham kod basılmaz). Gizlenen kurlar (gram altın 24, EUR, GBP) şeridin `title` ipucunda TAM listelenir, son sorgunun tam zamanı ve hata sebebi saatin `title`'ında. Kur hiç alınamadıysa şerit boş kalmaz: `#ust-kur-yok` "Kur alınamadı" + sebep (UX-S28). Sağ kümeye **Yardım** (`#ust-yardim`) eklendi. Hap ikonsuz, yalnız metin (tuval). Tüm eski id'ler (`#ust-menu-ac`, `#ust-bildirim-rozet(-hata)`, `#ust-mod-anahtar`, `#ust-mod-basit`, `#ust-mod-profesyonel`) ve `.kur-bandi`/`.son-sorgu`/`.kur-guncelle-btn` sınıfları AYNEN duruyor. ⟳ kur güncelle, şube satırı, bildirim rozeti **korundu** (özellik silinmez). |
| `Kuyumcu.Web/Layout/YardimBaglantisi.cs` (YENİ) | Rota ilk kesimi → `kilavuz.html` çıpası (22 satır). Tanınmayan ekran kılavuzun başına gider. Bağlantı **yeni sekmede** açılır: tezgâhta yarım fiş Yardım yüzünden kaybolmaz. |
| `Kuyumcu.Web/Servisler/CanliKurServisi.cs` | `KurAnlik` kaydına varsayılanlı `Kaynak` ve `AyarFiyatlari` + `AyarGramFiyati(id)` yardımcısı; yeni `AyarFiyatiAnlik` kaydı. Varsayılanlı olduğu için eski yanıtlar/sahte API'ler kırılmaz. |
| `Kuyumcu.Core/I18n/UstBarSozlugu.cs` (YENİ) | Üst bar metinleri TR+EN (`Sozlukler.cs` ortak hub'ına dokunulmadı — MenuSozlugu deseni). |
| `Kuyumcu.Web/wwwroot/css/app.css` | Sonda tek **"S1-c"** bloğu (geri dönüş = bloğu kaldır). Sabit renk yok; tipografi/iç boşluk yalnız `≥769 px` medya sorgusunda (B-9 ve P-39 K3 dar ekran kuralları ezilmez). Profesyonel ölçüler `html[data-mod="profesyonel"]` ile. |
| `Kuyumcu.Tests/S1cUstBarTestleri.cs` (YENİ) | 17 test yöntemi / 21 vaka (bUnit + kaynak kilitleri). |

## 3. Tuval değerlerinin belirteç karşılığı (sabit renk yok)

| Tuval | Belirteç | Canlı ölçüm (Chrome, Platin, aydınlık) |
|---|---|---|
| bar `#ffffff`, alt çizgi `#d7dce4` | `--yuzey`, `--ayrac` | — |
| etiket `#5a6577` | `--metin-soluk` | 14 px, uppercase |
| değer `#172033` 19/600 | `--metin` | 19 px / 600 / tabular-nums |
| Yardım `#2457d6` | `--vurgu` | 16 px; Fildişi'de `rgb(138,90,30)` = tuval Fildişi vurgusu |
| seçili hap `#14213d` / `#fff` | `--kenar-yuzey` / `--kenar-metin-guclu` | **rgb(20,33,61) / rgb(255,255,255)** (birebir) |
| başlık Sora 22/600 | `--yazi-marka` | Sora, 22 px, 600; `document.fonts` Manrope+Sora loaded |

## 4. Sapmalar ve gerekçeleri

| # | Tuval / plan | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | Plan: hap hesap menüsünün içinde | Hap açıkta, hesap menüsü yok | Görev: "TASLAK KAZANIR"; tuvalde hesap menüsü çizilmemiş |
| S2 | Tuvalde şube satırı, bildirim rozeti, ⟳ yok | **Korundu** | Plan § 3 md.1 "özellik silinmez"; ⟳ yalnız SAHİP/MÜDÜR'de, rozet yalnız açık bildirim varken çizilir |
| S3 | Saat rengi `#8a94a6` | `--metin-soluk` (`#5a6577`) | `#8a94a6` beyaz üstünde ≈3:1, 14 px metin için WCAG AA altı; en yakın ölçülü belirteç |
| S4 | Fildişi'de seçili hap `menu` zemini + beyaz yazı | `--menu-aktif-yuzey` / `--menu-aktif-metin` (rgb(59,47,30)/beyaz) | Tuvalin kendi paletinde Fildişi menüsü krem (`#f1ebe0`) → beyaz yazı okunamaz; kontrast hatası tuvalde |
| S5 | Tuvalde EUR/GBP/gram altın(24) yok | Şeridin `title` ipucunda tam liste | Özellik silinmez, katlanır (plan § 3 md.1; plan 1.2 "EUR/GBP ipucuna") |
| S6 | Tuvalde `Yardım` → `#kilavuz` | Bulunulan ekranın `kilavuz.html#<bölüm>`, yeni sekme | Kılavuz zaten ekran başına çıpa taşıyor; yeni sekme iş kaybını önler |
| S7 | 1440 px'te tek satır (68 px) | Şube satırı + rozet + ⟳ varken sağ küme ikinci satıra iner (≈98 px); **taşma 0** | Tuvalde olmayan korunmuş öğeler yer kaplıyor; P-39 K1 sarma kuralı gereği kayma yerine sarma. Tuvaldeki öğe kümesi (başlık+şerit+Yardım+hap) tek satıra sığıyor |
| S8 | Plan § 5A "ikon her zaman metinle" | Hap ikonsuz | Tuval hapta ikon çizmiyor; tuval üstün |

## 5. Migration
**Gerekmedi.** Şema değişikliği yok; sunucunun mevcut `ayarFiyatlari` ve `kaynak` alanları okundu.

## 6. DoD sözleşmesi (Bertan koşulu)
Bu paket **yeni varlık/kayıt türü doğurmaz**.

| Kayıt | Düzenleme yolu | Not |
|---|---|---|
| Kullanıcının arayüz modu (`kullanicilar.arayuz_modu`) | `PATCH /kullanicilar/ben` — hap düğmeleri | Mevcut uç; canlıda 200/400 + SQL + denetim izi (GUNCELLEME Kullanici 2) |
| Kur anlık görüntüsü | `POST /kur/guncelle` (⟳, `[RolGerekli("SAHIP","MUDUR")]`) | Mevcut uç; salt okunur şeritten yalnız tazeleme |
| Yardım bağlantısı | — | Kayıt değil, gezinme |

Her düğme istek atar: hap → PATCH (test `Profesyonel_e_basinca_PATCH…`), ⟳ → POST (test `Kur_guncelle_SAHIP_te_POST_atar…`). Yardım bir `<a>`'dır.

## 7. Testler

### 7.1 Paket testleri — `S1cUstBarTestleri` **21/21 yeşil**
Kur şeridi tuval sırası (basit), Gram 22 sunucudan (5555 ≠ has×0,916), K22 yoksa çizilmez, profesyonel EUR + kaynak adı, tanınmayan kaynak kodu basılmaz, gizlenen kurlar ipucunda, saat HH:mm + tam tarih title, kur alınamazsa `#ust-kur-yok` + hatalı saat, Yardım ×5 rota (Theory), Yardım haritası iki yönlü (kilavuz.html çıpası + gerçek @page rotası), hap id/aria/ikonsuz, PATCH gövdesi ayrıştırılarak, ⟳ SAHİP POST / TEZGAHTAR yok, düzen sırası, EN dil, çıplak metin/inline style yok, CSS bloğu tuval ölçüleri + belirteç + ≥769 px kapsamı.

### 7.2 Etkilenen mevcut testler — **316/316 yeşil**
`RotaBaslikTestleri · BildirimEkranTestleri · P39TasarimKokleriTestleri · TemaEkranTestleri · KabukAramaEkranTestleri · S3eKurKaynagi* · SozlesmeKapsam* · TasarimBelirtec* · BasitMod* · MenuKatlama* · YaziTipiVeSema* · Evrak*` (KurAnlik'ı Evraklar.razor da kullanıyor).

### 7.3 Tam süit
`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory scratchpad/trx-pS1-c` (KUYUMCU_TEST_DB=kuyumcu_test_s, `dotnet build-server shutdown` sonrası, son değişiklikten sonraki derlemeyle):
**Toplam 4196 · Geçti 4195 · Başarısız 1** (S1-b sonu: 4168 / 4166 / 2).

| Kırmızı | Sahibi | Bu paketin ürünü mü? |
|---|---|---|
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` — `Not found: "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı"` | **P-30b (DALGA 7c)** | HAYIR — devir notundaki kırmızı, aynı mesaj; S1-c `denetim.sh`'e dokunmadı |

**Madde (7) — yeni kırmızı üretmeme:** devir listesi {`denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`} ; bu koşumun listesi aynı küme → fark = ∅. **Sağlandı.** (S1-b'deki `AltinPozisyonuTestleri.B2_08…` artık yeşil.)

## 8. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/S1-c/` altına kopyalandı → tek eşleşmeli değişiklik (betik eşleşme sayısı ≠1 ise durur) → `dotnet test --filter S1cUstBarTestleri -v n` → yedek geri kopyalandı + `touch` (S1-b tuzağı: eski mtime ile artımlı derleme mutasyonlu dll'i bırakıyordu) → `cmp`. Son mutasyondan sonra `dotnet build --no-incremental` 0/0.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1c-1 | Gram 22 istemcide `has × 0,916` hesaplanır | `Gram22_SUNUCUNUN_ayar_fiyatindan_gelir_istemcide_HESAPLANMAZ`, `Yanitta_K22_yoksa_Gram22_CIZILMEZ_tahmin_yok` | AYNI |
| M1c-2 | EUR basit modda da çizilir | `Basit_modda_serit_TUVAL_SIRASIYLA_has_gram22_usd_cizer_EUR_yok`, `Yanitta_K22_yoksa…` | AYNI |
| M1c-3 | Tanınmayan kaynakta ham kod basılır | `Taninmayan_kaynak_kodu_TAHMIN_EDILMEZ_ham_kod_basilmaz` | AYNI |
| M1c-4 | Harita `cariler → cari` (ölü çıpa) | `Yardim_haritasi_IKI_YONLU…`, `Yardim_bulunulan_ekranin…(/cariler)` | AYNI |
| M1c-5 | Yardım `target="_blank"` kaldırılır | `Yardim_bulunulan_ekranin_kilavuz_bolumune_YENI_SEKMEDE_gider` ×5 vaka | AYNI |
| M1c-6 | İpucundan GBP düşer | `Gizlenen_kurlar_SILINMEZ_ipucunda_TAM_listelenir` | AYNI |
| M1c-7 | Kur hatasında `#ust-kur-yok` çizilmez | `Kur_alinamazsa_serit_BOS_KALMAZ_hata_gorunur_UX_S28` | AYNI |
| M1c-8 | Seçili hap `--kenar-yuzey` → `--vurgu` | `S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` | AYNI |
| M1c-9 | Saat `HH:mm` → tam tarih | `Saat_yalniz_SS_DD_tam_tarih_title_da` | AYNI |

⚠ Dürüstlük notu: M1c-4'ün ilk koşumu **test sonucu vermedi** — eşzamanlı şeridin derlemesiyle `Kuyumcu.Web/obj` dosya kilidi çakıştı (`MSB4018 GetFileHash … The process cannot access the file`). Kırmızı sayılmadı; yedek geri kondu (cmp AYNI), mutasyon yeniden koşuldu, tablodaki sonuç ikinci koşumdur. Hayatta kalan mutasyon: **0**.

## 9. Denetim
`bash dotnet/denetim.sh` → **DENETİM TEMİZ** (EXIT 0).
İlk koşumda `app.css ham hex 29 (tavan 25)` **benim** ihlalimdi: S1-c blok başlığı yorumunda 4 hex değeri anmıştım (sayaç yorumları da sayar). Yorumdaki hex'ler kaldırıldı → temiz. Aynı koşumdaki `Sınırsız ToListAsync 146 > 145` başka şeridin (Modules) ara hâliydi; ikinci koşumda 145/145 ✓. Yeni sayaç eklenmedi (`denetim.sh` başka şeridin açık dosyası); S1-c kuralları bUnit + 9 mutasyonla kilitli.

## 10. Canlı doğrulama

### 10.1 API (port **5194**, `--no-build`, Development, gerçek MySQL `kuyumcu_net`)
Kiracı **PAKETS1-c-TEST** (`01M37JP4WNFDR7Q25MMQZB40CH`): davet `POST /sistem/davetler` (geçici rastgele `Sistem__Anahtari` ortam değişkeni; değer yazılmadı) — ilk deneme **400** "Gerekçe zorunludur" (gerekçesiz gövde benim hatam), gerekçeyle **201** → `POST /auth/kayit` **201**.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /kur/guncel` (SAHİP) | 200; `kaynak` + `ayarFiyatlari[K22]` | **200** ilk denemede; has 6746.75, `kaynak` ALTINKAYNAK, K22 `gramFiyati` **6180.02** (= has×0,916 sunucuda), usd 48.948, eur 55.751, gbp 64.995 |
| 2 | Kimliksiz `GET /kur/guncel` | 401 | **401** "Token bulunamadı" |
| 3 | `POST /kur/guncelle` (⟳) | 200 | **200**, kaynak ALTINKAYNAK, K22 6180.02 |
| 4 | `PATCH /kullanicilar/ben {"arayuzModu":"BASIT"}` (hap) | 200 | **200**, `arayuzModu` BASIT |
| 5 | `GET /auth/ben` | etkin mod BASIT | **200**, rol SAHIP, `etkinArayuzModu` BASIT |
| 6 | `PATCH {"arayuzModu":"XYZ"}` | 400 | **400** "Geçersiz arayüz modu" |
| 7 | `PATCH {"arayuzModu":"PROFESYONEL"}` | 200 | **200** |
| 8 | SQL `kullanicilar.arayuz_modu` | PROFESYONEL | **PROFESYONEL** |
| 9 | SQL `kur_snapshots` | ALTINKAYNAK satırları | 3+ satır ALTINKAYNAK (6746.97 / 6746.75 / 6746.56) |
| 10 | SQL `denetim_kayitlari` | mod değişimi izli | `GUNCELLEME Kullanici 2`, `EKLEME KurSnapshot 4`, Tenant/Sube/Kullanici/RefreshToken 1'er |
| 11 | `GET /css/app.css`, `/kilavuz.html` | 200, kaynakla birebir, S1-c bloğu yayında | **200/200**, `cmp` **AYNI**, "S1-c — ÜST BAR: KUR" 1 eşleşme |

### 10.2 Tarayıcı ölçümü (gerçek Chrome, statik düzenek)
⚠ **Dürüst not:** üst bar oturum açılmadan çizilmez; tarayıcıda oturum açmak parola girmeyi gerektirir, ajan güvenlik kuralı gereği **parola girilmedi** (S1-b ile aynı sınır). Bunun yerine `scratchpad/harness/` altında **ağaçtaki gerçek `app.css` + `belirtecler.css` + vendored fontlar** ve UstBar'ın ürettiği DOM yapısının (canlı API değerleriyle, en kalabalık durum: SAHİP ⟳ + şube satırı + bildirim rozeti) kopyası 127.0.0.1:**5193**'te `python3 -m http.server` ile sunuldu (5195-5200'e dokunulmadı).

| Ölçüm | Sonuç |
|---|---|
| Yatay taşma — 3 sayfa (basit / profesyonel / Fildişi) × 5 genişlik (1440/1280/**1024**/768/375) × 2 başlık (uzun/"Panel") = 30 ölçüm | **30/30: `scrollWidth == clientWidth`, bar dışına taşan öğe 0** |
| Başlık | Sora, 22 px, 600 |
| Etiket / değer | 14 px uppercase / 19 px 600 tabular-nums |
| Saat, Yardım | 14 px; 16 px vurgu |
| Seçili hap (Platin) | rgb(20,33,61) + beyaz — tuval `#14213d` birebir; 6px 14px, 15 px, 999 px |
| Seçili hap (Fildişi) | rgb(59,47,30) + beyaz (S4) |
| Profesyonel kaynak aralığı | ilk ölçümde "19:48·Altınkaynak" bitişikti (inline-flex baş boşluğu yuttu) → `.kur-kaynak { margin-left: .3em }` eklendi → aralık 3,9 px, metin " · Altınkaynak" |
| Bar yüksekliği | tuvaldeki öğe kümesiyle tek satır; korunan şube+rozet+⟳ ile 1440 px'te 98 px (S7) |

### 10.3 Temizlik
API (PID) ve düzenek sunucusu `kill` ile durduruldu; `lsof -iTCP:5194` ve `:5193` **boş**. Test kiracısı `tenant_id` kolonu olan **70 tablodan** + `kiraci_davetleri` + `tenants`'tan silindi; kalan satır toplamı **0**. `tenants`: başlangıç **10** → açılınca 11 → son **10**. `QA-TEST-` fikstürlerine dokunulmadı. Geçici sır dosyaları (sistem anahtarı, parola, jeton, kayıt gövdesi) `printf "" >` ile boşaltıldı.

## 11. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/` altında **hiçbir sayfa açılmadı**. Açılan razor: `Layout/UstBar.razor` (S-1 dosya listesinde). Dokunulmayan şerit dosyaları: Modules/Kasa·Vezne·Maliyet·Rapor, Raporlar/Gider.razor, betikler/*.sh, Kurulum.razor, Siparis*/Faturalar.razor, Panel.razor, Sozlukler.Marka.cs, **Sozlukler.cs**, `denetim.sh`, sunucu kodu.

## 12. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (S1-c'ye ait olanlar):
```
 M dotnet/Kuyumcu.Web/Layout/UstBar.razor
 M dotnet/Kuyumcu.Web/Servisler/CanliKurServisi.cs
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? dotnet/Kuyumcu.Core/I18n/UstBarSozlugu.cs
?? dotnet/Kuyumcu.Web/Layout/YardimBaglantisi.cs
?? dotnet/Kuyumcu.Tests/S1cUstBarTestleri.cs   (Kuyumcu.Tests dizini izlenmiyor; dosya yeni)
?? dokumanlar/09-uygulama-kayitlari/PAKET-S1-c-uygulama-raporu.md
```
Proje ağacına .bak/.tmp/.mutorj bırakılmadı (yedekler scratchpad'de).

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tuval birebir: başlık Sora 22 · şerit HAS/Gram 22/USD + saat, tabular, 19 px kalın · sağda Yardım · pill 999 px | KARŞILANDI | § 3, § 10.2 canlı ölçüm |
| `#ust-mod-anahtar/-basit/-profesyonel` aynen yaşar | KARŞILANDI | `Mod_hapi_id_leri_AYNEN_yasar…`, `BildirimEkranTestleri` yeşil |
| 1024 px'te taşma 0 | KARŞILANDI | 30/30 ölçüm; P-39 K1 testleri + denetim ✓ |
| Özellik silinmez | KARŞILANDI | EUR/GBP/gram 24 ipucunda, ⟳/şube/rozet korundu (S2, S5) |
| Mevcut DOM id'leri değişmez | KARŞILANDI | Yalnız eklendi: `ust-kur-serit/has/gram22/usd/eur/saat/kaynak/yok`, `ust-yardim` |
| Sabit renk / inline style yok | KARŞILANDI | CSS testi + `UstBar_da_CIPLAK…` + denetim SABIT_RENK ✓ |
| CDN/npm/inline script yok | KARŞILANDI | Hiç script/link eklenmedi |
| Sözlük TR+EN, çıplak metin yok, Sozlukler.cs'e dokunma | KARŞILANDI | `UstBarSozlugu.cs`; EN testi |
| UX-S21 (sistemin bildiği değer) | KARŞILANDI | Gram 22 sunucudan (M1c-1) |
| UX-S28 (hata boşa çevrilmez) | KARŞILANDI | `#ust-kur-yok` (M1c-7) |
| UX-S31 (ham kod basılmaz) | KARŞILANDI | Kaynak adı sözlükten, bilinmeyen çizilmez (M1c-3) |
| DoD EK md.1 (her düğme istek atar) | KARŞILANDI | PATCH/POST testleri |
| İzolasyon (KUYUMCU_TEST_DB=kuyumcu_test_s, 5194, build-server shutdown) | KARŞILANDI | tüm koşumlar |
| Paket kapısı 1-7 | KARŞILANDI | build 0/0 · paket testleri 21/21 · denetim TEMİZ · 9/9 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 14. Açık kalanlar
1. **Ağaçtaki tek kırmızı (bu paketin DEĞİL):** `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` → sahibi **P-30b (DALGA 7c)**. Ayrıca koşum sırasında `Kuyumcu.Api/Modules/Pozisyon/PozisyonModulu.cs:92` ~2 dk derlenemez hâldeydi (başka bir şeridin yarım düzenlemesi — satırda `)$1 {` artığı vardı; sahibi doğrulanmadı); dokunulmadı, kendiliğinden düzeldi, bekleyip devam edildi.
2. **Tarayıcıda oturumlu uçtan uca görüntü** ajan tarafından alınamadı (parola girilmez); 5199 QA turunda (orkestratör) görsel kontrol önerilir.
3. **Şube satırı + rozet + ⟳ ile 1440 px'te iki satır** (S7): tuvalin öğe kümesi tek satıra sığıyor; korunan öğelerin yeri için S1-e/S1-f'de ekip kararı gerekebilir (ör. şube satırını tek şubede menü kartına taşımak) — bu pakette özellik taşınmadı.
4. `OlcumKapisiTestleri.cs(525)` CS8604 uyarısı ara derlemelerde görüldü (P-30b şeridinin dosyası); son derlemede 0 uyarı.

## 15. Kurucuya / mali müşavire notlar
Mali müşavire not **yok** (mevzuat hükmü içermez). Kurucuya: görsel tercih gerektiren bir karar açılmadı; S3/S4 sapmaları tuvaldeki iki okunaklılık sorununun (soluk saat, Fildişi hap) belirteçle giderilmesidir.
