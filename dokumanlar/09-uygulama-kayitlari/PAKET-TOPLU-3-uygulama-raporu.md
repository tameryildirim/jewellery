# PAKET TOPLU-3: uygulama raporu (tur 2 — 02.10.2026; tur 1 aşağıda korunuyor)

## TUR 2 — tasarım hakemi KRİTİK'lerinin kapanışı

Taşkın + Ertuğ. Hasımsız paket, bağlayıcı kanıt bu bölümdür. Ortam: API 5188 (PID ile açıldı/kapatıldı), vekil 5189, CDP 9981–9985, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, geçici dizin `/private/tmp/kuyumcu-serit/TOPLU-3/`. Test kiracısı `PAKETTOPLU-3-TEST T2 Kuyumculuk` (01M3X5NG9M1WFYRG3Y3TEF5A08), `demo-tohumla.js --prova-kiraci` ile kuruldu (ilk kur 34,5 sn'de geldi, `{"cari":6,"urun":12,"satis":9}`), iş sonunda silindi. Para/veri/yasal davranışa dokunulmadı.

Not: tur 2 kodunun bir kısmı (Kurulum akordeonu, `T3-2d`, `T3-12`, Toplu3Tur2 testleri) 01.10 21:39'da yazılmış ama doğrulaması yarım kalmıştı; geçici dizin silinmişti. Bu turda tamamı yeniden ölçüldü, eksikler düzeltildi.

### T2.1 Madde tablosu

| Hakem bulgusu | Önce | Sonra | Kanıt |
|---|---|---|---|
| **KRİTİK 1 — yaşlandırma 1280 has DOLUYKEN son sütun 83 px kırpık** | 1280 PRO açık: `#yas-tablo` sw 1045 / kap cw 962 (hakem ölçümü). | Has dolu fikstürde (açılış devri −125,5 g ve +12,25 g has, GBP 120): **1280 PRO açık ve koyu: sw 962 = cw 962, 8 sütun, has sütunu görünür, kırpılan hücre 0, komşu tutarlar arası en az 20 px.** 1440: 1122 = 1122, ara 32 px. 375: satır kartı, sw = cw = 349, has değerleri kartta. Ölçer K3 (kırık kısa etiket) **0**: ilk denemede başlıkların sözcük arasından sarması ("31-60 / GÜN") K3=3 verdi; T3-2d'den başlık sarma kuralı kaldırıldı, başlıklar tek satır, 10 px sayı dolgusu yetiyor (deney: nowrap + 10 px → sw 962 = cw 962). | `toplu3.css` T3-2d; testler `T3b_Yaslandirma_641_1439_arasi_dar_dolgu_basliklar_tek_satir_has_doluyken_de`, yeni bUnit `T3b_Hakem_fikesturu_has_dolu_8_kolon_has_degerleri_cizilir`; görüntüler `s2/foto/raporlar_sekme_cari__PRO_{aydinlik,karanlik}_1280__tablo.png`, `s3/foto/raporlar_sekme_cari__PRO_aydinlik_375__tablo.png` (Read ile bakıldı: HAS BAKİYESİ sütunu ve "-125,5000 has", "12,2500 has", toplam "-113,2500 has" tam görünüyor) |
| **KRİTİK 2 — /kurulum 375 uzun sayfa, açık formlar, iki birincil, jargon, yapışık kartlar** | 375 BASIT 2,15 / PRO 2,30 ekran; açılış ve mühür formları açık; iki birincil; "KURU PROVA", "bu sürümde yok"; 1440 kartlar arası 0 px; etiketler yapışık | **375 BASIT 1,05 ekran (852 px), PRO 1,15 (933 px); 1280/1440: 1,00.** Açılış bakiyesi ve "Kurulumu mühürle" **kapalı akordeon** (başlık + tek cümle özet + ok, 48 px dokunma). DOM'da **tek** birincil düğme (açılışta görünen birincil 0; satır eklenince "Kontrol et", sonuç gelince "Açılışı uygula", mühürlenebilir ve yarım iş yokken "Kurulumu mühürle"). "KURU PROVA" → "Kontrol et" / "Kontrol sonucu"; "Stok açılışı (bu sürümde yok)" → "Stok açılışı" + **Yakında** rozeti, sayı yok. Açıklama ve geri alınamazlık metni ⓘ içinde (onay penceresindeki tam metin değişmedi). Kartlar arası **16 px**, açık gövdede öğeler arası **12 px** ("Kayıt" ve "Gerekçe" etiketi üstündeki öğeden 12 px). Ekranda jargon taraması (KURU PROVA, bu sürümde yok, ters kayıt, müşteri düzeltme fişi, DRY RUN) → **0** (12 bileşim). | `Kurulum.razor`, `Sozlukler.Toplu3.cs`, `toplu3.css` T3-12; bUnit `Toplu3KurulumEkranTestleri` (6), CSS `T3b_Kurulum_bolum_akordeon_ve_kart_araligi`; görüntüler `s2/foto/kurulum__BASIT_aydinlik_375.png`, `kurulum__PRO_karanlik_375__acilis-acik.png`, `deneme/foto/kurulum__PRO_aydinlik_1280.png` (Read ile bakıldı) |
| Denetim P-13 IC_KOD_SIZINTISI (tur 2'nin kendi kırmızısı) | `Kurulum.razor:927` `YakindaMi(...) => a.Anahtar == "STOK_ACILIS"` satırı denetimin ">…BÜYÜK_ALT_ÇİZGİ" kalıbına takıldı (ekrana basılmıyor, yanlış pozitif) | Anahtar ayrı sabite alındı (`StokAdimAnahtari`); davranış aynı. P-13 ✓ | `denetim2.log` |

### T2.2 Gerçek tarayıcı ölçümü (başlıksız Chrome, `s2` + `s2b` + `s3`)

7 rota × {1440, 1280, 375} × {BASİT, PRO} × {açık, koyu} = **84 ölçüm** (`modGercek` ve `temaGercek` her satırda istenenle aynı; BASİT açık 375'te `/` ve `/kur` ilk koşuda tema nesnesi yüklenmeden ölçülmek istendi → hata kaydı, `s2b`'de yeniden ölçüldü). Rotalar: `/raporlar?sekme=cari`, `/kurulum`, `/`, `/kur`, `/hurda`, `/uyum?sekme=risk`, `/raporlar/vade-takibi`. Kurulum için ayrıca açılış bölümü açılıp ölçüldü ve görüntülendi. Son kod (P-13 düzeltmesinden sonra) `s3`'te /kurulum ve yaşlandırma 1280 + 375 PRO tekrar ölçüldü: sonuç aynı.

| Rota | 1440 | 1280 | 375 |
|---|---|---|---|
| Yaşlandırma (PRO, has dolu) | sw/cw 1122/1122 · kırpık 0 · ara 32 · K hepsi 0 (K12 hariç, aşağıda) | 962/962 · kırpık 0 · ara 20 · K3 0 | 349/349 kart · K hepsi 0 |
| Yaşlandırma (BASİT) | tablo BASİT'te çizilmez (tasarım); K hepsi 0 | K hepsi 0 | K hepsi 0 |
| /kurulum | 1,00 ekran · birincil DOM 1 / görünen 0 · iki bölüm kapalı · kart arası 16 | aynı | BASİT 1,05 · PRO 1,15 · aynı |
| /, /kur, /hurda, /uyum, /vade-takibi | K1/K5/K7–K11/K13/yatay 0 | aynı | aynı |

Sıfır olmayan ölçüler (hepsi bu turun dosya alanı dışında, sebepleri yazılı):
- K12 ezik = 2: `#rp-yil` "2026" 80 px, `#rp-ay` "Ekim" 76 px. Raporlar sayfa başlığının dönem seçicileri; yaşlandırma tablosu değil (paket alanı "yalnız yaşlandırma tablosu").
- `/` BASİT 1440 K3 taşan = 1 (`#panel-borclu-tutar-…` "24.423,82 ₺ · 12,3 g has") ve 375 K3 kırık = 1 (`span.panel-liste-ad "Tuna Akarsu"`): Panel borçlular listesi. Has dolu fikstür yüzünden ortaya çıktı. Panel.razor bu pakette yalnız yasal liste bandı için açık.

### T2.3 Testler
- Filtreli (Toplu3, KurulumEkran, SADE1, RaporYaslandirma, RaporlarEkran, UX4dEkran, Yasal3Ekran, P57, PD3): **217/217** (tur 1 sonu 216 + yeni bUnit 1).
- Toplu3Testleri.cs: tur 1'in 24 testi + tur 2'nin 9 testi (CSS 2, kaynak 1, bUnit 6) + yeni 1 = 34 metot.
- **Tam süit (tek koşum, son kodla; trx `/private/tmp/kuyumcu-serit/trx-pTOPLU-3/_Mac_2026-10-02_05_27_22.trx`): 9094 toplam, 9091 geçti, 1 başarısız, 2 atlandı.** Kırmızı: `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (bUnit WaitFor zaman aşımı, "Check count: 0" 1,99 sn'de; POS ekranı, bu paketin dosyası değil). Tek başına 3 kez koşuldu: **3/3 yeşil (7/7)** → yük altında kararsız, bu paketin ürünü değil. Tur 1'deki başkalarına ait 5 kırmızı (OL5, D2_01, Iade, SozlesmeKapsam ×2) bu koşumda yok (sahipleri kapatmış). **Bu paketin yeni kırmızısı: 0.**

### T2.4 Mutasyon (tur 2; hepsi kırmızı, geri alındı, `cmp` AYNI)

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M3-25 | T3-2d sayı dolgusu 10 → 16 px | T3b_Yaslandirma_641_1439_arasi_dar_dolgu_basliklar_tek_satir_has_doluyken_de |
| M3-26 | Dar masaüstünde başlık sarma (`white-space: normal`) geri eklendi | aynı |
| M3-27 | `.kur-bolum-govde` gap silindi (etiketler yapışır) | T3b_Kurulum_bolum_akordeon_ve_kart_araligi |
| M3-28 | Kartlar arası `margin-top` silindi | T3b_Kurulum_bolum_akordeon_ve_kart_araligi |
| M3-29 | `AcilisBolumuAcik => true \|\| …` (açılışta açık) | T3b_Acilista_…KAPALI… (BASIT, PRO), T3b_Muhurlenebilirken_… |
| M3-30 | Mühür `<details … open>` | T3b_Acilista_…KAPALI… (BASIT, PRO), T3b_Muhurlenebilirken_… |
| M3-31 | `ProvaBirincil => prova is null` (iki birincil) | T3b_Muhurlenebilirken_muhur_KAPALI_ama_birincil_… |
| M3-32 | `YakindaMi` → `false && …` | T3b_Stok_adimi_Yakinda_sayisiz_ve_aciklamalar_i_icinde (son kodla yeniden koşuldu) |
| M3-33 | `KurulumKontrolEt` → "Kuru prova" | T3b_Kontrol_sonrasi_tek_birincil_Uygula_jargon_yok, T3b_Sozluk_kurulum_TR_EN_dolu_jargonsuz |
| M3-34 | Negatif has hücresi `metin-kirmizi` → `metin-mavi` | T3b_Hakem_fikesturu_has_dolu_8_kolon_has_degerleri_cizilir |
| M3-35 | Tablo her zaman `yas-has-bos` (has dolu tabloda sütun gizlenir) | T3b_Has_dolu_tabloda_…, T3_Has_bakiyesi_yoksa_…, T3b_Hakem_fikesturu_… |

Mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-3/`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata** (`build4.log`). Not: ara bir tam derlemede (`build2.log`) başka şeritlerin iki test dosyasında 2 uyarı görüldü (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs` CS8601, `KesifP2Testleri.cs` xUnit2029). Bu paketin değil; son derleme 0/0.

### T2.5 Denetim
`bash dotnet/denetim.sh` (son kod): **303 ✓, 1 ✗ = P-30 DEMO_INSAN_SURESI** (kapsam dışı, kabul "P-30 hariç TEMİZ"). İlk koşumda P-13 vardı; bu turun satırıydı, düzeltildi (T2.1).

### T2.6 Canlı doğrulama (5188) ve SQL

| Adım | Beklenen | Gerçek |
|---|---|---|
| `demo-tohumla.js --prova-kiraci --api :5188` | TOHUMLANDI | kod 0, 34.486 ms, `{"cari":6,"urun":12,"satis":9}` |
| `POST /kurulum/acilis/prova` CARI (Nermin −125,5 g; Tuna 2.400 ₺ + 12,25 g; Derya 7.500 ₺) | 200, temiz | 200, `temiz:true`, `hasToplam:-113.25`, `tlToplam:9900` |
| `POST /kurulum/acilis/uygula` | 200 | 200, `uygulananSatir:3`, `geriAlinabilir:true` |
| `POST /cari/{Selin}/doviz-veresiye` GBP 120 (kursuz) | 400 | 400 "Kur kaydı zorunludur" (doğru); kur kimliğiyle → 200 |
| `GET /kurulum/durum` | adımlar | `STOK_ACILIS bitti:false zorunlu:false` → ekranda "Yakında"; `muhurlenebilir:true` → birincil "Kurulumu mühürle" (kapalı bölüm içinde) |
| SQL `hareketler` ACILIS satırları | 2 has satırı | Nermin `ACILIS 125.500000`, Tuna `ACILIS -12.250000` (ekranda defter yönüyle −125,5 / +12,25) |

### T2.7 Temizlik
API (PID 27923) ve vekil `kill` ile durduruldu; 5188 ve 5189 dinleyici yok. `zsh kiraci-temizle.sh 01M3X5NG9M1WFYRG3Y3TEF5A08 --uygula` → kod 0, 557 satır + tenants 1. SQL: tenants'ta 0, hareketler 0, cariler 0. **tenants: başlangıç 16 → son 16.** `anahtar`, `fik.json` ve `jeton` dosyaları `printf ''` ile boşaltıldı. QA-TEST-* kiracılarına dokunulmadı.

### T2.8 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/wwwroot/css/toplu3.css` (T3-2d: başlık sarma kaldırıldı; T3-12 kurulum akordeonu)
- `dotnet/Kuyumcu.Web/Pages/Kurulum.razor` (akordeon, tek birincil, Yakında, ⓘ; P-13 sabiti)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu3.cs` (Kurulum* anahtarları, TR+EN)
- `dotnet/Kuyumcu.Tests/Toplu3Testleri.cs` (tur 2 testleri + hakem fikstürü bUnit)

Dokunulan sayfalar (tur 2): Kurulum (TOPLU-2 YARIM devri, hakem gerekçeli), Raporlar yalnız CSS ile.

### T2.9 Açık kalanlar (tur 2)
- `/` Panel borçlular listesinde has metni taşması ve ad kırılması (yalnız has bakiyeli veride). Panel.razor bu pakette yalnız bant için açık → sonraki pakete.
- Raporlar dönem seçicileri `#rp-yil` ve `#rp-ay` K12 ezik (80/76 px). Yaşlandırma dışı → sonraki pakete.
- 641–1279 px arası (ör. 1024) yaşlandırma tablosu kendi kabında kayabilir. Kabul genişlikleri (1440/1280/375) sığıyor; ara genişlikte sticky ad sütunu ve kaydırma gölgesi önerilir.
- Kurulum PRO açık gövdesindeki "Kasa ve banka açılışı neden eksi olamaz…" paragrafı TOPLU-2 kararıyla PRO'da düz metin, testle sabit (`KurulumEkranTestleri`, `Toplu2Testleri`). Bu turda değiştirilmedi; bölüm açılışta kapalı olduğu için ilk görünümde yok.
- Kararsız test: `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (yük altında WaitFor zaman aşımı; POS sahibi).

---

# TUR 1 (01.10.2026)

Taşkın (backend) ve Ertuğ (frontend) rolüyle hazırlandı. Paket hasımsız, yalnız görünüme dokunuyor; bağlayıcı kanıt bu rapordaki ölçümlerdir.

Kaynak olarak kuyruk dosyasının (`ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md`) son üç bölümü kullanıldı: 01.10 15:40, 16:35 ve 19:35. Islemler.razor ödeme paneli maddesi (g) bu pakete dahil değil.

Ortam:
- API: 5188, vekil: 5189, CDP portları: 9981–9989.
- Test veritabanı: `KUYUMCU_TEST_DB=kuyumcu_test_t9`.
- Test kiracısı: `PAKETTOPLU-3-TEST Kuyumculuk` (01M3W57CYCXNY182S4HSB02NY8). Kiracı, yeni `demo-tohumla.js --prova-kiraci` ile kuruldu ve iş sonunda silindi.
- Geçici dosyalar: `/private/tmp/kuyumcu-serit/TOPLU-3/`.
- Para, veri ve yasal davranışa dokunulmadı. Hiçbir koşul, eşik, kaynak seçimi ya da rapor hesabı değişmedi.

## 1. Madde tablosu (önce ölçüm, sonra ölçüm, kanıt)

| # | Madde | Önce (başlıksız Chrome) | Sonra | Kanıt |
|---|---|---|---|---|
| 1 | Panel bandı: "Yasal liste henüz yüklenmedi — destekle görüşün." | **Neden:** `yaptirim_kayitlari` kuyumcu_net'te **0 satır** (SQL: `count=0, max(liste_tarihi)=NULL`). Liste sistem ucundan ByGatech tarafından yüklenir; dev veritabanında hiç yüklenmemiş. Bant DOĞRU bir yasal uyarı (fail-closed) olduğu için **gizlenmedi**. | Koşul ve sayım aynı. Metin sadeleşti ve eyleme dönük oldu: "Yasal liste henüz yüklenmedi. Yüklenmesi için destek hattını arayın." Yanına "Ayrıntı →" eklendi (`#uyb-liste-git`, /uyum?sekme=risk). Satır sayısı: 1280'de 1, 375'te 3. | `UyumYasalBantlari.razor`, `Sozlukler.Toplu3.cs`. Testler: `T3_Liste_yuklenmemisse_uyari_SADE_dille…`, `T3_Liste_guncelse_bant_yok_kosul_degismedi`. Görüntü: `s4/foto/panel__BASIT_aydinlik_375.png` |
| 2 | Kur ekranında "Deneme kur kaynağı" rozeti | **Neden:** `appsettings.Development.json` içinde `Kur:SozlesmesizKaynaklarAcik=true`; `appsettings.json` ve `appsettings.Production.json` içinde `false`. Rozet yalnız sözleşmesiz kaynak SEÇİLİYKEN çıkıyor (`kd.SeciliSozlesmesiz`). Üretimde bu kaynaklar kapalı olduğu için rozet çıkmaz. | Koşul aynı. Rozetin dili sadeleşti: "Ücretsiz kur kaynağı". İpucu: "Kur bilgisi ücretsiz, herkese açık bir kaynaktan geliyor…". SAHİP rolü tam hukuki metni yine title'da görüyor. Kur değerlerine ve kaynak seçimine dokunulmadı. | `Kur.razor` (3 satır). Testler: `T3_Kur_razor_rozet_ve_ipucu_Toplu3ten`, SADE1 ve UX4d testleri güncellendi |
| 3 | Kenar çubuğunda "Rapor merkezi" ile "Vade takibi" aynı anda seçili | **Güncel ağaçta YOK.** TOPLU-2 `DahaUzunOgeVar` ile kapanmış. Demo kaydı eski bir kopyadandı (uzun demo ADIMLAR.md md.7 de bunu yazıyor). | Ölçüm: /raporlar/vade-takibi'de seçili menü öğesi yalnız `["Vade takibi"]`. 12 bileşimin tamamı (BASIT/PRO × açık/koyu × 1440/1280/375) aynı. | Kod değişmedi. Kanıt: ölçüm tablosundaki `vade` satırları |
| 4 | demo-tohumla.js yeni kiracıda ilk kuru beklemiyor; işletme kartı boş | 40 × 1 sn yoklama yapılıyordu; zamanlayıcı 60 sn olduğu için düşüyordu. İşletme adresi ve vergi bilgisi doldurulmuyordu. | `kurBekle()` ayrı ve test edilebilir bir işlev oldu. En çok 150 sn (`KUR_BEKLEME_AZAMI_SN`) yokluyor, 2 sn aralıkla. 404 ara durum sayılıyor; başka her HTTP hatası yükseliyor. Yeni adım: `PATCH /admin/tenant/kimlik` ile kurgusal kart (adres "… (örnek adres)", "Örnek Vergi Dairesi", vergi no `0000000000`, telefon `05320000100`). Yanıt geri okunuyor, eksikse betik duruyor. Plan kapısında `ISLETME` kontrolü var. **Canlı:** yeni kiracıda kur 34,4 sn'de geldi, `isletme` adımı 26 ms sürdü. SQL: `vergi_dairesi='Örnek Vergi Dairesi'`, `vergi_no='0000000000'`, adres dolu. `TOHUMLANDI … {"cari":6,"urun":12,"satis":9}` | Testler: `T3_Ilk_kur_en_az_150_sn…`, `T3_Kur_404_disi_hata_yutulmaz…`, `T3_Isletme_karti_kurgusal_ve_plan_kapisi`. PD3 testleri yeşil |
| 5 | RY YÜKSEK: tablo altındaki iki not 12 px, karta yapışık ve teknik | `#yas-belirsiz-notu`, `#yas-doviz-ozet` ve `#yas-has-notu` 12 px, sol dolgu 0 | "Tarihsiz" açıklaması artık başlığın yanında, dokunmatikte açılan (i) içinde (`#yas-belirsiz-ipucu` details; `#yas-belirsiz-notu` id'si aynı, içindeki p'de). Döviz özeti tfoot satırına taşındı (`tr#yas-doviz-ozet`, (i) açıklamalı). Has notu 14 px, 16 px dolgulu. | `Raporlar.razor` (yalnız yaşlandırma tablosu), `toplu3.css` T3-1…T3-4 |
| 6 | RY YÜKSEK: 1280 px'te 8 sütun sıkışık | Sıfır hücreler "0 ₺" yazıyordu (5 hücre); sayı sütunlarına özel dolgu yoktu | Sıfır hücre "—" gösteriyor. CSS ::before kullanılıyor; "0 ₺" ekran okuyucu için DOM'da duruyor. Sayı sütunlarında 16 px yatay dolgu var. ≤1365 px'te HİÇBİR satırda has bakiyesi yoksa ikincil has sütunu gizleniyor (P-47: has varsa sütun her genişlikte görünür). 1280 PRO: sw=cw=962 (yatay kayma 0), komşu metin arası en az 26 px. | `T3_Sifir_hucre…`, `T3_Has_bakiyesi_yoksa…`, CSS testleri |
| 7 | RY YÜKSEK: 375 px'te tutarlar ilk görünümde yok, rozetler kesik | 375 PRO: sw 994 / cw 349, görünen tutar **0/5** | Satır kartı: ad ve rozetler, altında yalnız DOLU dilim(ler) ve toplam (`data-etiket` başlığı). sw=cw=349, görünen tutar 4/5 (beşinci Selin'in toplamı; 0 ₺ olduğu için "—" gizli, döviz rozeti görünüyor). Kesik rozet 0. | `T3_Yaslandirma_375_satir_karti`. Görüntü: `s4/foto/rapor-cari__PRO_aydinlik_375.png` |
| 8 | RY ORTA: döviz rozetinde yön yok; ipuçları yalnız title'da | "120,00 £" | "Bize borçlu 120,00 £" / "Biz borçluyuz …". Artı işaret cari bize borçlu demek; TL kuralıyla aynı. Tarihsiz ve döviz açıklamaları dokunmatikte açılan (i) içinde. | `T3_Doviz_rozeti_yon_tasir` (2 durum), `T3_Doviz_ozeti_tfoot…` |
| 9 | RY DÜŞÜK: tarihsiz toplamı ayrı satır | **Yapılmadı.** Toplam hesabı (rapor hesabı) kapsam dışı. | — | Açık kalanlara yazıldı |
| 10 | Fişteki "işletme kartı eksik" kutusu | "Bu kayıt değiştirilemez · Belgenin düzenleyen tarafı eksik. Eksik bilgi bu ekranda uydurulmaz… · Düzeltme şöyle yapılır: İşletme kartından doldurun · [İşletme kartından doldurun]" (aynı cümle 3 kez) | "Belgede dükkânınızın adresi ya da vergi bilgisi eksik. Bir kez girin, bütün belgelerde çıksın." + [İşletme bilgilerini girin]. Bu kutuda "değiştirilemez" başlığı, kilit ve tekrar satırı CSS ile gizlendi. Kutunun koşulu (`KimlikEksikMi`) aynı. | HUB'da yalnız iki **değer** değişti: `Cikti.KimlikEksikGerekce`, `SayfaDuzeltme.IsletmeKartinaGit` (yeni anahtar yok). Cikti.razor'a dokunulmadı (PARA-DEMO alanı). Görüntü: `s2/foto/cikti__PRO_karanlik_1280.png` |
| 11 | /cikti "Yazdır" koyu temada soluk | Koyu: zemin rgb(145,185,255), yazı rgb(9,21,44), **kontrast 9,16**. Açık: 6,06. Opaklık 1, etkin. Demo görüntüsündeki soluk görünüm eski kopyadandı (DUGME-2 öncesi). | Ölçümle KAPALI (AA ≥ 4,5); diğer ekranlardaki birincil düğmeyle aynı. Kod değişmedi. | Ölçüm tablosu `cikti` satırları |
| 12 | /kurulum 375 uzun sayfa | BASIT 375 kat 2,24 (1819 px), PRO 2,39 | Adım satırı ızgarası: ad ve eylem üst satırda, durum ve sayı alt satırda. BASIT 375: **2,15** (1749 px), PRO: **2,30**. K3, K9 ve yatay kayma 0. Kalan boy, zorunlu açılış ve mühür formlarından geliyor (3 bölüm; K4 "≥4 bölüm" eşiği tetiklenmiyor). | `T3_Kurulum_375_adim_izgarasi…`. Görüntü: `s1/foto/kurulum__PRO_aydinlik_375.png` |
| 13 | 375 genel arama katmanı serbest yüzüyor | 375: x8 y8, 359×160 (yüzen kart) | 375: x0 y0, **375×812** (tam ekran sayfa). 1280/1440 değişmedi (576×150, ortada). | `T3_Arama_katmani_375_tam_ekran` |
| 14 | Hurda "Daha fazla" (tuval ile çelişki) | Düz kenar, metin rengi = form alanı, seçim kutusu gibi | Ekip oyu: tuval ölçüleri KORUNDU (tam genişlik, 52/74 px, 9 px köşe, 16 px). Kenar kesik çizgili, metin ve ok vurgu renginde (açık rgb(36,87,214), koyu rgb(145,185,255)). Kapsam yalnız `#hrd-detay-ac/-gizle`. | `T3_Hurda_daha_fazla…`. Görüntü: `s4/foto/hurda__BASIT_karanlik_1280.png` |
| 15 | Vitrin pano çekmecesi 13 px ipucu | 14 metin düğümü 14 px'in altında (alan-istege 13, ipucu 13, kilit notu 12) | Ekip oyu (ortak ipucu kademesi): çekmece içi yardımcı yazı en az 14 px. Sonuç: **0** düğüm 14 px'in altında (12 bileşim). | `T3_Vitrin_cekmece_yardimci_yazi…`. Görüntü: `s4/foto/vitrin-cekmece__BASIT_karanlik_375.png` |
| 16 | Uyum "SİYASİ NÜFUZ…" etiketi 3 satır | BASIT 1280: 3 satır (179 px) | Yasal terim kısaltılmadı. Kart iki sütun genişliğinde. 1280 ve 1440'ta **1 satır** (424/390 px), 375'te 2 satır. | `Uyum.razor` (id + Renk sınıfı), `T3_Uyum_pep_karti…`. Görüntü: `s4/foto/uyum-risk__BASIT_aydinlik_1280.png` |

## 2. Gerçek tarayıcı ölçümü (son kodla, `olc/s4`, 120 ölçüm)

10 senaryo ölçüldü:
- Rotalar: `/`, `/kur`, `/raporlar/vade-takibi`, `/raporlar?sekme=cari`, `/cikti/SATIS_FISI/{id}`, `/kurulum`, `/hurda`.
- Etkileşimler: `/vitrin` çekmecesi, `/uyum?sekme=risk`, açık arama katmanı.
- Bileşimler: {1440, 1280, 375} × {BASİT, PRO} × {açık, koyu}. `modGercek` her ölçümde istenen modla aynı.

| Ölçüt | Sonuç |
|---|---|
| K1 gizli-görünen | 120/120 → 0 |
| K3 taşan / kırık | 120/120 → 0/0 |
| K5 TRY | 0 |
| K7 / K8 / K10 | 0 |
| Yatay kayma | 0 |
| K9 binen | 0. İstisna: vitrin çekmecesi 12 bileşimde 6. Ayrıntısı aşağıda. |

Vitrin çekmecesindeki K9=6, arka sayfanın düğmeleri (`#vitrin-kur-don`, `#vitrin-yenile`) ile çekmece içindeki ipucunun kesişmesinden geliyor. Çekmece arka planın üstünde durur; bu bir binme değil. Değer önce ölçümde de 6'ydı. TOPLU-2'de de aynı ölçüt ayrımı yapılmıştı.

Yaşlandırma tablosu BASİT modda çizilmiyor (yalnız PRO). PRO açık 1440 ilk koşuda yüklenmeden ölçülmüştü. Ayrı yeniden ölçüldü (`olc/s5`): tablo var, sw=cw=1122, K hepsi 0.

Dar sütun: belgeler ve tablolar ana alanın tamamını kullanıyor. Çekmece kendi genişliğinde.

Read ile bakılan görüntüler:
- Önce: `once/foto/rapor-cari__PRO_aydinlik_1280/375`, `kurulum__BASIT_aydinlik_375`, `uyum-risk__BASIT_aydinlik_1280`
- Ara: `s1/foto/rapor-cari__PRO_aydinlik_375`, `kurulum__PRO_aydinlik_375`, `arama__PRO_aydinlik_375`; `s2/foto/cikti__PRO_karanlik_1280`, `rapor-cari__PRO_karanlik_1280`
- Son: `s4/foto/rapor-cari__PRO_aydinlik_375`, `uyum-risk__BASIT_aydinlik_1280`, `hurda__BASIT_karanlik_1280`, `panel__BASIT_aydinlik_375`, `vitrin-cekmece__BASIT_karanlik_375`

Tüm görüntüler `/private/tmp/kuyumcu-serit/TOPLU-3/olc/<tur>/foto/` altında. Ölçüm JSON'u `olc/s4/olcum.jsonl`.

## 3. Sapmalar ve gerekçeleri

- **Fiş kutusu:** dil, HUB değerleriyle değiştirildi. Görünüm CSS ile düzenlendi (`#cikti-kimlik-uyari`). Sebep: Cikti.razor bu pakette yalnız Yazdır düğmesi için açık, ekstre kısmı PARA-DEMO'nun. `IsletmeKartinaGit` metni PusulaBelgesi'nde de aynı anlamla kullanılıyor.
- **Yazdır:** kod değişmedi. Ölçülen kontrast 9,16, kusur artık yok.
- **Menü (c):** kod değişmedi. Güncel ağaçta kusur yok.
- **Yaşlandırma testleri:** `RaporYaslandirmaEkranTestleri.GBP_bakiyesi_satirda_ve_doviz_ozetinde_gorunur` yeni yapıya güncellendi (rozet yönlü; özet tfoot TR'si). Test içinde gerekçesi yazılı. Diğer RY ve P-47 testleri değişmeden yeşil: sıfır hücre metni DOM'da "0 ₺" olarak duruyor.
- **SADE1 ve UX4d testleri:** rozet metni artık Toplu3'ten okunuyor (3 + 1 assert). Rozetin çıkma koşulu ve ipucu sözleşmesi aynı.
- **index.html:** toplu3.css önce dugme ile ortak arasına kondu. Denetimdeki DUGME2 sırası gereği **olcek.css'ten önceye** taşındı (kesif-p1'den sonra).

## 4. Testler

- Yeni: `Kuyumcu.Tests/Toplu3Testleri.cs`, **24 test** (CSS ve saf 14, node 3, bUnit 7).
- Filtreli son koşum: Toplu3, RaporYaslandirma, SADE1, Yasal3Ekran, RaporlarEkran, P57, PD3 ve UX4dEkran → **170/170 geçti**.
- Tam süit (tur 1, tek koşum; trx `/private/tmp/kuyumcu-serit/trx-pTOPLU-3/_Mac_2026-10-01_20_10_33.trx`): toplam 9039, geçen 9031, **başarısız 6**, atlanan 2.

Tam süitteki 6 kırmızı:

| Test | Sahibi | Durum |
|---|---|---|
| `UX4dEkranTestleri.M24_Sozlesmesiz_kaynak_seciliyken_PILOT_uyarisi_gorunur` | **BU PAKET** ("Deneme" metnini bekliyordu) | Test güncellendi, filtreli koşumda **yeşil** |
| `OlcekTekTestleri.OL5_index_olcek…`, `DugmeIkiTestleri.D2_01_index_dugme…` | SUBE-KAPSAM-B (`sube-kapsam-b.css` dugme.css ile ortak.css arasına eklendi) | Açık |
| `IadeTestleri.Kapanmis_gune_iade_yazilamaz` | PARA-DEMO (gün kapanışı farkı: "kasadaki kart … sayıma girmez") | Açık |
| `SozlesmeKapsamTestleri.Kapsam_orani_beyan…` (58→59), `Kilitsiz_tip_listesi…` (`GunKapanisiOnizleme`) | PARA-DEMO | Açık |

Bu paketin ürettiği yeni kırmızı: **0** (M24 düzeltildi).

## 5. Mutasyon tablosu (24/24 kırmızı, hepsi geri alındı, `cmp` birebir AYNI)

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M3-01 | Bant Toplu3 → eski Yasal3 metni | T3_Liste_yuklenmemisse_uyari_SADE_dille… |
| M3-02 | "Ayrıntı" bağlantısı silindi | T3_Liste_yuklenmemisse_uyari_SADE_dille… |
| M3-03 | Rozet → KurDenemeRozet | SADE1.Kur_sozlesmesiz_kaynak_gunluk…, T3_Kur_razor_rozet… |
| M3-04 | `YasSifirMi` → `== -1m` | T3_Sifir_hucre_yas_sifir_sinifi… |
| M3-05 | (i) id bozuldu | T3_Tarihsiz_aciklamasi_baslikta… |
| M3-06 | Döviz yönü ters | RY.GBP_bakiyesi…, T3_Doviz_rozeti_yon_tasir ×2, T3_Doviz_ozeti_tfoot… |
| M3-07 | `YasHasVar` ters | T3_Has_bakiyesi_yoksa… |
| M3-08 | colspan +1 | T3_Doviz_ozeti_tfoot… |
| M3-09 | ::before "—" silindi | T3_Yaslandirma_sifir_hucre_tire… |
| M3-10 | 1365 gizleme → opacity | T3_Yaslandirma_dar_masaustunde… |
| M3-11 | Kart grid → table-row | T3_Yaslandirma_375_satir_karti |
| M3-12 | Kimlik kutusu kilit görünür | T3_Kimlik_eksik_kutusunda… |
| M3-13 | Kurulum ızgara alanı bozuk | T3_Kurulum_375_adim_izgarasi… |
| M3-14 | Arama 375 tam ekran değil | T3_Arama_katmani_375_tam_ekran |
| M3-15 | Hurda dashed → solid | T3_Hurda_daha_fazla… |
| M3-16 | Vitrin ipucu → yazi-xs | T3_Vitrin_cekmece_yardimci_yazi… |
| M3-17 | PEP kartı span 2 → auto | T3_Uyum_pep_karti… |
| M3-18 | toplu3.css bağlantısı silindi | T3_toplu3_css_ortak_cssten_once… |
| M3-19 | KimlikEksikGerekce eski metin | T3_Sozluk_sade_dil_TR_EN_dolu |
| M3-20 | Kur bekleme 150 → 40 | T3_Ilk_kur_en_az_150_sn…, T3_Kur_404_disi… |
| M3-21 | Plan ISLETME kontrolü silindi | T3_Isletme_karti_kurgusal… |
| M3-22 | 404 dışı hata yutuldu | T3_Kur_404_disi_hata_yutulmaz… |
| M3-23 | PATCH /admin/tenant/kimlik adımı sahte | T3_Isletme_karti_kurgusal… |
| M3-24 | Uyum razor Renk sınıfı silindi | T3_Uyum_pep_karti… |

M3-06, 07 ve 24 ilk denemede derlenemedi: başka şeridin `SubeKapsamBPozisyonHttpTestleri.cs` dosyası o an yarımdı ve aynı anda WASM derlemesi çakıştı. Ağaç toparlanınca yeniden koşuldu; sonuçlar yukarıda. Son mutasyondan sonra yapılan temiz `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata**.

## 6. Denetim (`bash dotnet/denetim.sh`)

301 ✓, 3 ✗:

| ✗ | Sahibi |
|---|---|
| P-30 DEMO_INSAN_SURESI | Kapsam dışı (P-30, 23.09 provaları) |
| P-23 ODEME_KALEMSIZ_YOL = 1 | Hareket 01M3W4STS2PVR18X55ZQ558XQW, kiracı **PAKETPARA-DEMO-TEST**: PARA-DEMO şeridinin test verisi, SQL ile doğrulandı |
| DUGME-2 DUGME2_BAGLANTI_SIRASI = 1 | `sube-kapsam-b.css` dugme.css'ten sonra, ortak.css'ten önce. **SUBE-KAPSAM-B.** toplu3.css olcek'ten önceye alındıktan sonra yeniden ölçüldü; sayaç bu paketten değil. |

Bu paketin sayaçları: P-30 dışında ✗ yok.

## 7. Canlı doğrulama (5188) ve SQL

| Adım | Beklenen | Gerçek |
|---|---|---|
| Yeni kiracıda `demo-tohumla.js --prova-kiraci` | TOHUMLANDI | kod 0, `{"cari":6,"urun":12,"satis":9}`, kur 34.383 ms'de geldi (eski 40 sn sınırına 5,6 sn kalmıştı) |
| `isletme` adımı (`PATCH /admin/tenant/kimlik`) | 200 + geri okuma | 26 ms. SQL: `adres` dolu, `vergi_dairesi='Örnek Vergi Dairesi'`, `vergi_no='0000000000'` |
| `PATCH /admin/tenant/kimlik {"adres":null}` (kutu ölçümü için, kendi kiracım) | 200, adres null | `{'adres': None, 'vergiDairesi': 'Örnek Vergi Dairesi', …}` |
| `POST /cari/{Selin}/doviz-veresiye GBP 12500` | MASAK kapısı | 400: "MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur…". Kapı çalışıyor; tutar 120'ye indirildi |
| `POST …/doviz-veresiye GBP 120` | 200 | 200 (cari JSON döndü) |
| `POST /kurulum/acilis/prova` + `uygula` (Derya +7.500 TL) | 200 | prova `temiz:true`; uygula `{"uygulananSatir":1,"tlToplam":7500,…}`. İlk uygula denemesi 400 döndü: `kurSnapshotId` eksikti, yanıt okunup düzeltildi |
| `yaptirim_kayitlari` | Bandın nedeni | `count=0, max(liste_tarihi)=NULL` |

## 8. Değişen dosyalar (bu paket)

- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu3.cs` (yeni)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (yalnız iki değer: `KimlikEksikGerekce`, `IsletmeKartinaGit`; TR)
- `dotnet/Kuyumcu.Web/Bilesenler/Uyum/UyumYasalBantlari.razor` (Panel yasal liste bandı)
- `dotnet/Kuyumcu.Web/Pages/Kur.razor` (rozet ve ipucu)
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor` (yalnız yaşlandırma tablosu ve yardımcıları)
- `dotnet/Kuyumcu.Web/Pages/Uyum.razor` (risk özet ızgarasına id, PEP kartına sınıf)
- `dotnet/Kuyumcu.Web/wwwroot/css/toplu3.css` (yeni)
- `dotnet/Kuyumcu.Web/wwwroot/index.html` (bir bağlantı)
- `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js`
- `dotnet/Kuyumcu.Tests/Toplu3Testleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/RaporYaslandirmaTestleri.cs`, `SADE1EkranTestleri.cs`, `UX4dEkranTestleri.cs` (beklenti güncellemeleri, gerekçeli)

Dokunulan sayfalar: Raporlar (yaşlandırma), Kur, Uyum (risk ızgarası), Panel bandı bileşeni. Cikti, Kurulum, Hurda, Vitrin ve arama katmanı yalnız `toplu3.css` ile değişti.

## 9. Koşul karşılama

| Koşul | Durum |
|---|---|
| Önce her maddenin varlığını ölç, kapananı kanıtla | KARŞILANDI (menü ve Yazdır ölçümle kapalı) |
| Yasal uyarıyı gizleme; davranışı değiştirme | KARŞILANDI (koşul ve sayım aynı, test var) |
| Kur değerine ve kaynak seçimine dokunma | KARŞILANDI |
| Rapor hesabına dokunma | KARŞILANDI (Dilim, toplam ve sözleşme aynı; P-47 testleri değişmeden yeşil) |
| SİYASİ NÜFUZ metni kısaltılmaz | KARŞILANDI (sözlük aynı, testle sabit) |
| demo-tohumla en az 60 sn (yoklama, en çok 150 sn) ve kurgusal işletme | KARŞILANDI (canlıda koşuldu) |
| Başlıksız Chrome 1440/1280/375 × BASİT/PRO × açık/koyu, Read | KARŞILANDI (120 ölçüm, görüntüler §2) |
| Her değişiklik için test ve mutasyon | KARŞILANDI (24/24) |
| Build 0/0 | KARŞILANDI |
| Denetim TEMİZ (P-30 hariç) | Bu paketin sayaçları temiz. Ağaçta başka şeritlerin 2 ✗'i var (§6) |
| Tam süitte 0 yeni kırmızı | KARŞILANDI (tek yeni kırmızı M24 düzeltildi; diğer 5 başka şeritlerin) |
| Test kiracısı silindi | KARŞILANDI (§10) |
| COMMIT yok, dosya silme yok, sır yazılmadı | KARŞILANDI |
| Para, veri ve yasal davranış | Dokunulmadı; hasımlı pakete devir gerekmedi |

## 10. Temizlik

- API ve vekil PID'leri durduruldu; 5188 ve 5189'da dinleyici yok.
- `zsh kiraci-temizle.sh 01M3W57CYCXNY182S4HSB02NY8 --uygula` → kod 0. Kiracının tenants, hareketler ve cariler satırları 0.
- tenants sayısı: başlangıç 19, sonuç 15. Arada kendi kiracım +1 açıldı, sonra silindi. Kalan −4 fark başka şeritlerin kendi temizliği (PAKETTOPLU-2 Hakem2/3, PAKET-SKB-TEST ×3 gitti; yeni PAKETTOPLU-2-TEST geldi). QA-TEST-* kiracılarına dokunulmadı.
- `fik.json`, `jeton`, `sistem.anahtar` ve `prova.json` dosyaları `printf ''` ile boşaltıldı.

## 11. Açık kalanlar

- RY DÜŞÜK: "Tarihsiz toplamı ayrı satır" ve rapor tablolarında kuruş karışıklığı. Rapor hesabı/biçimi gerekiyor; kapsam dışı.
- RY'nin kapsam dışı maddesi: koyu temada rapor sekmeleri silik (1,16). Ortak sekme bileşeninde.
- Hurda'da yapılan kesik kenarlı açıcı kararı, aynı `.zo-detay-ac` kalıbını kullanan Banka ve Siparişler'e yayılmalı. Dosya alanı dışında kaldı; tutarlılık için sonraki pakete.
- Fiş kutusunun dilini razor'da (DegismezIpucu yerine düz uyarı kutusu) kalıcılaştırmak. Cikti.razor PARA-DEMO bitince açılmalı.
- Islemler.razor ödeme paneli: başarı bandını örtüyor (g). PARA-DEMO sonrası.
- Başkasının kırmızıları: OL5 ve D2_01 (SUBE-KAPSAM-B, `sube-kapsam-b.css` sırası); Iade Kapanmis_gune ve SozlesmeKapsam ×2 (PARA-DEMO). Denetimde DUGME2_BAGLANTI_SIRASI (SUBE-KAPSAM-B) ve P-23 (PARA-DEMO test kiracısı).

## 12. Kurucu / mali müşavir

Yasal hükme dokunulmadı. Yasal liste bandı gerçek bir eksikliği gösteriyor: geliştirme ve demo veritabanında yasal liste hiç yüklenmemiş. Gösterimde bandın çıkmaması isteniyorsa yapılması gereken, ByGatech'in sistem ucundan **resmî** listeyi yüklemesidir (`POST /sistem/yaptirim-listesi`, kaynak https). Uydurma bir liste yüklenmedi, çünkü bu tüm kiracılar için "liste güncel" yalanını üretir. Bu karar kurucuya kalıyor.
