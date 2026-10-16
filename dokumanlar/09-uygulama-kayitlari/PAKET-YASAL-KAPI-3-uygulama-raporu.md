# PAKET YASAL-KAPI-3: uygulama raporu (tur 2 — doğrulayıcı ve tasarım hakemi bulgularının kapanışı)

Roller: Taşkın (backend) ve Ertuğ (frontend). 10.10.2026, şerit t9. API 5188, vekil 5189, CDP 9981, test DB `kuyumcu_test_t9`. COMMIT YOK.
Sunucu koduna, migration'a ve veri modeline dokunulmadı (migration: gerekmedi). Tur 1 raporu aşağıda "Tur 1" başlığıyla aynen duruyor.

## T2-1. Kapatılan bulgular

| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| K1 | **KRİTİK** Fişte nakit + eşik üstü → 15 alanlı kimlik formu açılıyor, Kaydet pasif | Nakit kuralı tetikliyken (`NakitBandiVar`, kural sunucudan) `MasakParcasi` çizilmez (`Islemler.razor` `&& !NakitOnce`); Yıldız'da "+ Kimlik bilgisi" düğmesi de çıkmaz (`Islemler.Yildiz.cs` `KimlikGorunur`). Engel tek cümle: `NakitKilitCumlesi` = "{had} üstündeki fişte nakit kullanılamaz — ödeme türünü kart ya da havale yapın." (had sunucunun `/tevsik/gunluk` sayısı). "Kimlik bilgilerini girin ve…" cümlesi bu durumda kullanılmaz. Kart/Havale seçilince form açılır, Nakit'e dönünce kapanır. | Chrome 32 bileşim: nakit adımında form alanı 0, kart/havalede 4 (zorunlu girdi). bUnit YK3_F01/F02, T24 (güncellendi) |
| K2 | **KRİTİK** Aynı yasal uyarı 2–5 kez | Tek yer: BASİT'te üst bant, PRO'da Kaydet'in yanı, kimlik formu açıkken formun başlığı. Kaldırılanlar (DOM'dan da — hakemin sayacı görünmez kopyaları da sayıyordu): kilit satırının görünmez kopyası (metinsiz kalır, sebep düğmenin `title`'ında — UX-S30), ödeme sütunundaki kısa durum + "Neden?" + ipucu (`FisOdemeKasa` `YasalBaskaYerde`), "Kart olarak seçildi" notu başka yasal mesaj varken (PRO satışta yalnız panelde), form açıkken "gereken alanlar açıldı" bandı (`BasitBantKapiAcik`), form açıkken "Kaydetmek için kimlik bilgilerini doldurun" kopyası. PRO "Yasal kapılar" paneli engel fişte söylenirken katlı (`FisteSoylendi`, bilgi tıklayınca). | Chrome sayacı (checkVisibility ile görünen yasal cümle): **40 fiş ölçümünde her adımda 1** (`olcum-fis.txt`). bUnit `TumMetin` sayacı görünmez kopyaları da sayar: YK3_F01/F03, CUMA_S_01/02/04 |
| K3 | **KRİTİK** PRO teknik not "· Tek kasa var — otomatik seçildi", ödeme bilgisi 3 yerde | Teknik not 4 yerden silindi (`FisOdemeKasa` ×3, `FisOdemeBacaklari`). PRO satışta tek kasa bloğu çizilmez (`KasaOzette`), kasa alt çubuktaki özette yazar; ödeme satır içindeyken özet türü tekrarlamaz: "Hakem Ana Kasa Şube Merkez · 400.000 ₺" (`FisProOdemeHapi` `SeciciGorunur`). Panel satırındaki "— teyit bekleniyor" iç notu sözlükten kaldırıldı (TR+EN). | Görüntü `f-platin-1280-PROFESYONEL-karanlik-400000-nakit.png` (Read ile görüldü): Ödeme Türü + özet (kasa · tutar) + tek kırmızı cümle. UX4b POS_TEK… güncellendi, YK3_F04/F05 |
| K4 | **KRİTİK** Fişte eşik üstü formun zorunluları ilk ekranda değil | Form fişte İLK KEZ açıldığında başlığı üste kaydırılır (`scroll-margin-top` yapışkan üst çubuk payı) ve kullanıcı yazı kutusunda değilse ilk boş zorunlu alana odaklanılır (barkod okurken odak çalınmaz). Fişte ızgara aralığı sık, beyan açıklaması tek satır başlar (`yasal.css` (7)). | Chrome: 16 platin bileşimin kart/havale adımlarında `zorunluIlkEkran` = belge-no ad-soyad amac aciklama, DISI 0 (1280 ve 375). Görüntü `f-platin-375-BASIT-aydinlik-400000-kart.png` (Read): 5 zorunlu + "Kaydet ve yazdır" aynı ekranda |
| O1 | ORTA 375 çekmecede beyan açıklaması ilk ekranda değil | Çekmecede form ilk kez açılınca beyan açıklaması en az kaydırmayla görünür bölgeye alınır (`Cariler.YasalKapi3.cs` `CekmeceKimlikKaydir`, `block: nearest`; odak çalınmaz). İki sütun denemesi K3=2 / K7=1 ürettiği için geri alındı. | `olcum-cekmece.txt`: 16/16 bileşimde 5 zorunlu çekmece alt sınırının içinde; ör. platin 375 açıklama 596–708 / 716, Yıldız 375 607–706 / 718 |
| O2 | ORTA SiparisDetay kapora: uyarı iki kez | `#sipd-kapora-masak-not` yalnız form gelene dek çizilir (id korundu). | KEE05/KEE06 güncellendi; M2-8 |
| D1 | DÜŞÜK YK2_06 yük altında zaman aşımı | Bekleyişlere açık 5 sn zaman aşımı (YK2_06, YK2_07). | tam süitte YK2_06 yeşil |
| D2 | DÜŞÜK döviz nakit ön kontrolü istemci kuruyla | Yapılmadı (açık kalan). Sınırda kuruş farkında ekran izin verip sunucu reddederse ret alan altına düşer (çıkışsız değil, tur 1 §2.1). Sunucunun TL karşılığı gereksinim yanıtında yok; uç değişikliği gerekir. | — |

### Değişiklik sonrası iki ek düzeltme
- Tam süitte `DegisimEkranTestleri.DG_N_PRO_degisimde_biz_odersek_HAVALE_otomatik` kırmızı oldu (bu paketin): PRO değişimde panelin yasal kartı çizilmediği için not kayboluyordu. `OtomatikNotPanelde` panel koşuluyla (`tur == "SATIS" || MasakGerekli`) sınırlandı; filtreli koşumda yeşil (aşağıda).
- denetim `B-3 ESIK_ISTEMCIDE` yorumdaki "30.000" yüzünden 5'e çıkmıştı; yorum "{had}" yapıldı, sayaç tavanda.

## T2-2. Mevzuat
Yeni yasal sayı ya da hüküm yazılmadı. Had (30.000), kalan hak, kural etkinliği ve MASAK kademeleri sunucudan gelir (tur 1 §2.7, YASAL-1). İstemciye eşik sayısı gömülmedi (denetim B-3 ✓). Web araştırması gerekmedi.

## T2-3. Testler
Yeni dosya `Kuyumcu.Tests/YasalKapi2FisTekUyariTestleri.cs` (partial `SATIS3EkranTestleri`): YK3_F01 ×2, YK3_F02 ×5 (had 9.999,99 / 10.000 / 10.000,01 — fiş 10.000; sunucunun "toplam > had" kuralı), YK3_F03 ×2, YK3_F04, YK3_F05 = **11 vaka**.
Yeni davranışa göre güncellenen testler (eski tasarımı kodluyordu; gerekçe hakem bulgusu): SATIS3T24 `T24_gercek_musteride_…` (adı: `…nakit_once_odeme_turu_form_ACILMAZ`), SATIS3EkranTestleri T16, CumaS 01/02/04 (adları değişti), UX4b POS_TEK / Karma_acikken / Karma_bacak, BasitModPos YASAL_KAPI_masak…, KesifP5 SATIS4a (PRO'da not panelde), YasalKapiGeri YKG01, KaporaEszaman KEE05/06, YasalKapi2Testleri YK2_06/07 (zaman aşımı).

Filtreli koşumlar (son kod): ilgili 28 ailede 2306/2309 (2 atlandı) → son düzeltmeden sonra `Degisim|KesifP5|YK3_|SATIS3|CumaS|UX4b|YasalKapi|BasitModPos|Kapora`: **1103 başarılı, 1 başarısız (RD_KH02 — RAPOR-DORT, ad eşleşmesiyle süzgece girdi), 2 atlandı**.

**Tam süit** (tur başına bir kez; `trx-pYASAL-KAPI-3-t2`, `tam.log`): 12.412 test, 12.405 geçti, 4 başarısız, 3 atlandı.
- `DegisimEkranTestleri.DG_N_PRO…` — bu paketin; yukarıdaki düzeltmeyle filtreli koşumda yeşil.
- `Yildiz1Testleri.Y1_10_kural_govdelerinde_sabit_renk_yok` — `tema-yildiz.css` `.yz-tezgah-odeme-govde` kuralı (YILDIZ-4D/5A şeridi t1'in süren işi; `yasal.css`'te bu kural yok).
- `RaporDortTestleri.RD_KH01`, `RD_KH02` — RAPOR-DORT (devirde bilinen).
- Devirdeki `S24EskiAltinEkranTestleri` bu koşumda kırmızı değildi.
→ **Bu paketin yeni kırmızısı 0** (DG_N_PRO kapatıldı).

## T2-4. Mutasyonlar (yedek `mut-yedek/YASAL-KAPI-3-t2/`, betik `t2/mut2.sh`, özet `t2/mut-ozet.txt`)

| Kod | Değişiklik | Kırmızı | cmp |
|---|---|---|---|
| M2-1 | `MasakParcasi` `&& !NakitOnce` silindi (nakitte form açılır) | YK3_F01 ×2, YK3_F02(9999.99) ×2, T24_gercek… ×2 | AYNI |
| M2-2 | kilit satırı yine metinli (`@kilit`) | T16(gizli), YK3_F01 ×2, YK3_F03(BASIT) | AYNI |
| M2-3 | `FisOdemeKasa` `!YasalBaskaYerde` silindi | CUMA_S_01/02/04, YK3_F01 ×2 | AYNI |
| M2-4 | otomatik not yasal mesaj varken de çizilir | YK3_F03(BASIT) | AYNI |
| M2-5 | panel katlama dalı kapatıldı | YK3_F04 | AYNI |
| M2-6 | PRO tek kasa bloğu yine çizilir | UX4b POS_TEK_kasada… | AYNI |
| M2-7 | eski "Kimlik bilgilerini girin ve…" cümlesi geri | YK3_F02 ×2, YK3_F04, T24… (7 kırmızı) | AYNI |
| M2-8 | kapora notu form açıkken de çizilir | KEE05, KEE06 | AYNI |
| M2-9 | `BasitBantKapiAcik` → `BasitBantAcik` | BasitModPos YASAL_KAPI_masak…, YK3_F01(BASIT) | AYNI |

9/9 kırmızı, hepsi cmp AYNI. Son mutasyondan sonra temiz build: 0 uyarı 0 hata (`build-son3.log`).

## T2-5. denetim.sh
`DENETİM BAŞARISIZ` — 314 ✓, 3 ✗, hiçbiri bu paketin değil: `P-30 DEMO_INSAN_SURESI` (kabulde hariç), `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK-ESZAMAN), `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 1` (`Kuyumcu.Api/Modules/Sayim/SayimModulu.cs:146`, başka şeridin süren işi; bu paket API'ye dokunmadı). Bu paketin sayaçları: B-3 ✓ (tavanda), ModalPencere 48/55, satır içi stil 24, sabit renk 0.

## T2-6. Canlı (API 5188, `kuyumcu_net`, kiracı `PAKETYASAL-KAPI-3-TEST`)

| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| T1 | tahsilat NAKİT 29.999,99 | 200 | 200 |
| T2 | tahsilat NAKİT 30.000,01 | 400 | 400 `NAKIT_SINIRI` "30.000,01 TL nakit, 30.000,00 TL nakit sınırını 0,01 TL aşıyor…" |
| T3 | tahsilat NAKİT 30.000 | 200 | 200 |
| S1 | satış NAKİT 29.999,99 | 200 | 200 |
| S2 | satış NAKİT 30.000,01 | 400 | 400 `NAKIT_SINIRI` |
| S3 | satış NAKİT 30.000 | 200 | 200 |
| S4 | satış NAKİT 400.000 beyanlı | 400 (ekran artık buraya gelmeden durdurur) | 400 `NAKIT_SINIRI` |
| S5 | satış KART 400.000 beyansız | 400 MASAK | 400 "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur…" |
| S6 | satış KART 400.000 beyanlı | 200 | 200 |

SQL (`hareketler` ⋈ `cariler`, kiracı süzgeçli; betik yanlışlıkla iki kez koştu, iki koşu aynı sonucu verdi): yalnız kabul edilenler yazıldı — TAHSILAT NAKİT −29.999,99 / −30.000,00; SATIŞ NAKİT 29.999,99 / 30.000,00; SATIŞ KART 400.000,00; reddedilen 30.000,01 ve NAKİT 400.000 için kayıt 0; `masak_beyanlari` = 2 (iki koşunun S6'sı).
Temizlik: `zsh kiraci-temizle.sh 01M4KHQQ6RJW1FPW3Y30CT8ANM --uygula` → "Kiracı YOK"; davet satırı silindi; **tenants 23 → 23**. API, vekil, Chrome yalnız kendi PID'lerimle durduruldu; 5188/5189/9981 boş.

## T2-7. Gerçek tarayıcı (başsız Chrome, CDP 9981, profil tek dizin)
Betikler `t2/r2.mjs` (fiş akışı: kalem → otomatik tür → Nakit → Kart → Nakit → Havale; her adımda yasal cümle sayacı, form alanı, ilk ekran; Kart adımında `sikisma-olc.js` `__olcTam`), `t2/r4.mjs` (çekmece). Ham: `olcum-fis.txt`, `olcum-cekmece.txt`, `analiz.py`.

| Bileşim (×aydınlık/koyu) | Müşteri 400.000: sayaç her adımda | Nakit adımı form alanı | Kart/Havale ilk ekran | K3/K3c/K10c/K5/K7/K9/K12/Y (Kart) |
|---|---|---|---|---|
| platin 1280 BASIT | 1 (bant / form başlığı) | 0 | 4/4 içeride | 0 |
| platin 375 BASIT | 1 | 0 | 4/4 | 0 |
| platin 1280 PRO | 1 (Kaydet yanı / form başlığı) | 0 | 4/4 | 0 |
| platin 375 PRO | 1 | 0 | 4/4 | 0 |
| yıldız 1280 BASIT/PRO | 1 (`yildiz-tezgah-not`; PRO kart/havalede 0 — kimlik çekmecede) | 0 | çekmecede | 0 |
| yıldız 375 BASIT/PRO | 1 | 0 | çekmecede | K9=6 (YILDIZ-4D opak yapışkan alt şerit, tur 1 §2.7; diğerleri 0) |

Peşin 40.000 (8 bileşim, aydınlık): her adımda sayaç 1, form 0. Çekmece 250.000 Kart (16 bileşim): 5 zorunlu çekmece sınırı içinde, çekmece içi K3/K7/K12/Y 0 (K9=6 çekmecenin arkasındaki sayfa, tur 1 §8.2).
Görüntüler (Read: 4): hakemin `f-Basit-aydinlik-1440-kart.png` (eski hâl), `f-platin-1280-PROFESYONEL-karanlik-400000-nakit.png`, `f-platin-375-BASIT-aydinlik-400000-kart.png`, + tur 1. Not: matris koşusundaki `f-*-kart.png` görüntülerinin çoğu ölçerin kaydırmayı sıfırlamasından sonra alındı (betik hatası, sayısal ölçüm ondan ÖNCE); düzeltilmiş betikle alınan 375 BASIT görüntüsü esastır.

Yıldız koruması: Yıldız'a yeni öğe/CSS eklenmedi. `tuval-kiyas.mjs` YILDIZ-4D eşlemeleriyle bu kiracıda koşuldu (`t2/kiyas/`): Tüm ekranlar 66/67 (FARK: sayfa yüksekliği), Müşteriler 21/31, Tezgâh 25/36 + 9 bulunamadı, Bugün 7 bulunamadı. FARK'ların hepsi VERİ kaynaklı (tuval fikstürünün müşteri/ürün/“Toptancı ·” verisi bu kiracıda yok, liste uzunluğu); bu paketin dokunduğu öğe değil — ama temiz fikstürde FARK 0 kanıtı üretilemedi, açık kalanlara yazıldı. `yildiz-tipo-tara.mjs` BASIT+PRO: kural dışı 16+16, hepsi bu paketin dışındaki ekranlarda (`/urunler` modal İptal, `/banka` `/pos` boş cümle, `/uyum` maskeli no — canlı adımın verisi); `/islemler`, `/cariler`, `/siparisler`'de 0.

## T2-8. DoD sözleşmesi
Yeni varlık/uç yok. Yeni düğme yok; kaldırılan görünür metinlerin bilgisi kaybolmadı: Kaydet sebebi `title`'da, panel kademeleri katlı ayrıntıda, kasa özette.

## T2-9. Dokunulan sayfalar (TEK DOKUNUŞ — gerekçe: YASAL-KAPI-2/3 kapanış devri, aynı hakem bulguları)
`Pages/Islemler.razor` (satır sayısı 1399 korundu), `Islemler.Odeme.cs`, `Islemler.Yildiz.cs` (tek koşul), yeni `Islemler.YasalKapi3.cs`; `Pages/Cariler.razor` (tek satır), `Cariler.YasalKapi3.cs`; `Pages/SiparisDetay.razor` (tek satır); `Bilesenler/Fis/FisOdemeKasa.razor`, `FisProYanSutun.razor`, `FisProOdemeHapi.razor`, `FisOdemeBacaklari.razor`; `wwwroot/css/yasal.css`; `Kuyumcu.Core/I18n/Sozlukler.YasalParam.cs` (PanelAmac TR/EN).
Alan dışı dokunuş gerekçesi: Fiş bileşenleri ve `Islemler.*` hakem KRİTİK'lerinin kaynağı; ISLEM-BANT-2 dosyalarına (Para/Tutar, Fis hurda/takas/eski altın) dokunulmadı.

## T2-10. Açık kalanlar
- D2 döviz ön kontrolü istemci kuruyla (sunucu TL karşılığı uca eklenmeli) — Taşkın.
- YILDIZ-4D: Yıldız 375 opak yapışkan alt şerit K9=6; Y1_10 sabit renk (`tema-yildiz.css` `.yz-tezgah-odeme-govde`).
- RAPOR-DORT: RD_KH01/02. MASAK-ESZAMAN: GIRDI-2. SAYIM şeridi: DIL-SUNUCU (`SayimModulu.cs:146`). P-30 bilinen.
- tuval-kiyas FARK 0 temiz tuval fikstürüyle yeniden ölçülmeli (bu kiracıda veri farkı).
- Proje ağacında önceden duran `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` (bu paketin değil; silme kurucuda).
- Çekmecede form açıkken "Kaydetmek için kimlik bilgilerini doldurun" altlıkta duruyor (çekmece kalıbı; hakem bulgusu değildi).

## T2-11. Kurucu / mali müşavir
Yeni hüküm yok. "200.000 ₺ amaç beyanı kademesi" teyit sorusu KURUCU-KARAR-BEKLEYENLER'de; ekrandan "teyit bekleniyor" iç notu kaldırıldı.

---

# Tur 1 (önceki rapor, aynen)

# PAKET YASAL-KAPI-3: uygulama raporu

Roller: Taşkın (backend) ve Ertuğ (frontend). Tarih 10.10.2026, şerit t9. Portlar: API 5188, vekil 5189, CDP 9981. Test veritabanı `kuyumcu_test_t9`.

Kaynak, YASAL-KAPI-2 tasarım hakeminin KALDI bulgularıdır (`ham/AKTIF-KOSUM` 10.10 17:21 satırı ve `betikler/serit/tanim-yasalkapi3.json`).
- Hakemin kendi ölçüm dökümleri `/private/tmp/kuyumcu-serit/yk2-hakem/` altındadır (`olc-sonuc.txt`, `b1.mjs`, `i2–i4.mjs`).
- Bu pakette sunucu koduna, migration'a ve veri modeline dokunulmadı. Değişiklikler ekran, CSS, sözlük ve testlerle sınırlıdır.
- COMMIT YOK.

## 1. Önce: kusurlar gerçek Chrome'da yeniden üretildi (eski kod, 5188)

| # | Kusur (hakem) | Eski kodda ölçüm |
|---|---|---|
| Ö1 | **KRİTİK:** Ödeme tablosunda kasa adı tutarın üstüne biniyor | Yıldız `/islemler` 1280 px: `span.yz-tezgah-kasa` 875→1100 px, `yz-tezgah-odeme-tutar` 1085→1216 px. Kasa kutusu tutar sütununa 15 px giriyor (K9). |
| Ö2 | **KRİTİK:** Nakitte kimlik formu doldurtuluyor, Kaydet'te nakit sınırıyla reddediliyor. Ödeme türü değişince hata silinmiyor. | `/cariler` Tahsilat, 250.000 Nakit: alan altında nakit uyarısı yok. MASAK formu açılıyor (5 zorunlu alan). Sunucu sırası `CariModulu.cs` satır 1096: önce MASAK, sonra tevsik (satır 1113). Form doldurulunca 400 `NAKIT_SINIRI` geliyor. Bu ret `islemHata` bandına yazılıyordu. `thOdeme` `@bind` olduğu için ödeme türü değişince bant da `thTutarSunucuHata` da temizlenmiyordu. |
| Ö3 | Eşik üstü form ağır: 2 zorunlu alan ilk ekranda değil | Platin 1280×800 çekmece (alt sınır y=717): amaç y=861, açıklama y=953, ad soyad 701–745 (yarım). "Diğer bilgiler" katlanırı amaç ve açıklamadan ÖNCE geliyordu (zorunlu-önce ihlali). |
| Ö4 | PRO'da teknik not ve çift uyarı | PRO `#masak-kural` satırı: "…işlem amacı beyanıyla birlikte de kimlik istenir (**mali müşavir teyidi bekleniyor**). Bilgiler şifreli saklanır." Eşik başlıkta ve bu satırda olmak üzere iki kez geçiyordu. |
| Ö5 | Panel olmayan PRO çekmecelerde eşik iki blokta | Ö4 ile aynı kaynak: Cariler'in 4 çekmecesi ve SiparisDetay kapora çekmecesi. |
| Ö6 | 375 px'te "Kart" ile "FARK" binmesi | Yıldız 375, kaydırma 0: K9=6 (`Kart ⟂ #yildiz-tezgah-not`, `ÖDEME ⟂ #yildiz-tezgah-fark-tutar`). Kasa adı kutusu da tutara biniyordu (Ö1). |
| Ö7 | Tutar kutusu hizası | `#th-tutar` sağ kenarı 1247 px, alttaki seçicinin sağ kenarı 1264 px. "₺" eki kutunun dışında durduğu için kutu 17 px kısa kalıyordu. |

## 2. Yapılanlar

### 2.1 Nakit sınırı önce söyleniyor (Ö2)
Yeni dosya `Pages/Cariler.YasalKapi3.cs` içinde saf sınıf `CariNakitSiniri` ve `Cariler` için partial bulunur.

**Kural sunucuyla aynıdır, ekran tahmin etmez.**
- Ekran zaten `/tevsik/gunluk?cariId` soruyordu, ama yanıttan yalnız `Taksit` kullanılıyordu. Artık aynı yanıtın `KalanNakitHakki`, `KuralEtkin` ve `KumulatifHesaplandi` alanları da tutuluyor. Ek istek yok.
- Sunucunun kuralı `TevsikKurali.Degerlendir`: `bugünkü + bu işlem > had` olursa ret. Bu, `bu işlem > kalan hak` ile aynıdır; tam had (30.000) serbesttir.
- Had sayısı istemciye gömülü değildir. Cümledeki sayı sunucunun söylediği kalan haktır.

**Ekrandaki davranış:**
- Nakit seçili ve tutar kalan hakkı aşıyorsa:
  - Tutar alanının altında tek cümle çıkar: "Nakit en çok 30.000 ₺ olabilir; kalanı kartla ya da havaleyle alın."
  - Kaydet kilitlenir.
  - Kimlik formu ÇİZİLMEZ ve MASAK ret bandı gösterilmez.
  - Altlıktaki kilit sebebi "kimlik bilgilerini doldurun" demez.
- Kalan hak 0 ise "Nakit" seçicide görünür ama seçilemez. Gerekçesi yazar: "Bugünkü nakit sınırı doldu". Varsayılan Kart olur.
- Sunucu yine `NAKIT_SINIRI` derse (ör. çekmece açıkken başka kasada nakit alındıysa):
  - Ret `YasalKapiIzi` deseniyle tutulur ve bantta değil alan altında yazılır.
  - Ödeme türü değişince, tutar reddin ölçüsünün altına inince ya da müşteri değişince kalkar.
  - Nakit ve aynı tutara dönülünce geri gelir.
- Ödeme türü değişince sunucunun eski alan reddi (`thTutarSunucuHata`) ve bant temizlenir (`ThOdemeDegisti`).
- Kural kapalıysa (`KuralEtkin=false`) ya da kümülatif hesaplanamadıysa ön kontrol yapılmaz; son söz sunucudadır.

**Döviz tahsilat:** Efektif nakit olduğu için aynı kural TL karşılığına uygulanır. Sunucuda karşılığı `tevsik.Dogrula(tlKarsiligi)`. Alan altındaki cümle: "Nakit en çok {0} karşılığı olabilir; tutarı azaltın." Form yoktur, Kaydet kilitlidir.

### 2.2 Kimlik formu sık (Ö3)
- **`MasakFormu.razor`:** İsteğe bağlı "Diğer bilgiler" katlanırı zorunluların sonuna alındı (amaç, açıklama, fon, ek belge ondan önce gelir). İçerik ve kimlikler aynen. Değişiklik bütün ekranlarda geçerlidir ve zorunlu-önce kuralına uyar.
- **`yasal.css`**, yalnız çekmece içindeki kimlik formu için:
  - Geniş ekranda belge türü ve belge no yan yana, ad soyad tam satır. ≥769 px; Banka penceresindeki `zo-islem.css` tek sütun kuralı aynen kalır.
  - Izgara satır aralığı `.25rem`, alanlar arası boşluk `.625rem`.
  - Beyan açıklaması tek satır boyunda başlar, yazdıkça uzar (`field-sizing: content`, en çok 8rem).
  - Platin'de kart dolgusu ve başlık ölçeği küçüldü (`1rem`). Uyarı rengi ve kenarı aynen. Yıldız'da kart zaten kartsız; dokunulmadı.
- **`Cariler.razor`:** Kimlik formu açıkken `#th-ipucu` açıklama satırı çizilmez.

### 2.3 PRO teknik not, çift uyarı ve iki blok (Ö4, Ö5)
- `MasakFormu` yeni parametre aldı: `KuralGizli`. Varsayılanı `false`; öteki ekranlarda davranış aynen kalır.
- Panel olmayan PRO çekmecelerde `true` verildi: Cariler'in 4 çekmecesi (Tahsilat, Veresiye, Döviz tahsilat, Döviz veresiye) ve SiparisDetay kapora çekmecesi.
- Sonuç: Eşik ve gereken alanlar yalnız başlıkta, tek kez söylenir. "Mali müşavir teyidi bekleniyor" iç notu tezgâhtara gösterilmez.

### 2.4 Ödeme tablosu (Ö1, Ö6)
`yasal.css` içine yalnız Yıldız seçicisiyle kural eklendi. Gerekçe: `tema-yildiz.css` ve `YildizTezgah.razor` YILDIZ-4D şeridinin dosyalarıdır, dokunulmadı.
- `.yz-tezgah-odeme-ad` esnek satır oldu.
- Araç adı ("Kart") hiç küçülmez.
- Kasa adı kalan yere sığar ve üç noktayla kısalır. Kendi kutusu da sütunun içinde kalır.
- Tutar sütunu tam ve tek satırdır.

Tuval geometrisi korundu. Eski ve yeni hâl 1280 ve 375 px'te ölçüldü (`e7.mjs`):

| Öğe | Eski (x, y) | Yeni (x, y) |
|---|---|---|
| Araç metni | 836, 194,8 | 836, 195 |
| Kasa metni | 875,4, 196,8 | 875,6, 195,8 |
| Tutar | 1085,3, 191,4 (130,7×29) | aynı |
| Ad kutusu | 225,3×26,1 | aynı |

Kasa kutusunun genişliği 224,7'den 185,7'ye indi; fark kısalmanın kendisidir.

### 2.5 Tutar kutusu hizası (Ö7)
Yalnız Müşteriler çekmecelerinde `#th-/vr-/dt-/dv-tutar-sar` kutularında "₺" eki kutunun içine alındı (absolute, `padding-inline-end`). Ortak `Para` bileşenine dokunulmadı; o dosya ISLEM-BANT-2 şeridinindir.

Ölçüm: tutar sağ kenarı = ödeme sağ kenarı. 1280 px'te 1264 = 1264 (8/8), 375 px'te 359 = 359 (8/8).

### 2.6 Sözlük
Yeni dosya `Core/I18n/Sozlukler.YasalKapi3.cs` (TR ve EN): `NakitBugunYok`, `NakitBugunPasif`, `DovizNakitSinirAsildi`. Mevcut `TevsikTaksit.TahsilatNakitSinirAsildi` yeniden kullanıldı.

### 2.7 Sapmalar ve gerekçeleri
- **Tutar ve Ödeme şekli tek satır denemesi geri alındı.** İlk sürümde ikisi `form-grid-2` ile yan yanaydı. Tam süitte `SonKalanlarTahsilatEkranTestleri.SKC01_…_tutar_kutusu_TAM_genislik` kırmızı oldu. Bu test SON-KALANLAR kararının bekçisidir: dev tutar iki sütunda kesiliyordu. Karar korundu, tutar tam genişlikte kaldı. Yer, form sıklaştırmasıyla (§2.2) açıldı.
- **Sunucu sırası değiştirilmedi.** `CariModulu` hâlâ önce MASAK, sonra tevsik denetler. Dosya alanım ekran tarafıdır. İstemci aynı kuralı sunucunun kendi yanıtından önceden uygular. Sunucu reddi ayrıca iz olarak tutulur. Sunucu davranışı canlıda doğrulandı (§7).
- **375 px'te sticky alt şerit.** Yıldız Tezgâh'ta alt şerit (Fark ve Kaydet) YILDIZ-4D tasarımı gereği yapışkandır ve opaktır. Kaydırma 0'da ödeme satırını örter; tam sayfa K9 = 6 çıkar.
  - Bu, iki yazının görünür binmesi değildir. Ölçüm YILDIZ-4D raporuyla aynı sözleşmeyle yapıldı.
  - Ödeme satırı görünürken K9 = 0, sayfa sonunda K9 = 0, görünür binme (elementFromPoint ve opak zemin denetimi) kaydırma 0'da da sonda da 0.
  - Şeridin kendisi YILDIZ-4D alanıdır; açık kalanlara yazıldı.
- **Hurda ve Banka penceresi `KuralGizli` almadı.** Hurda sayfa formudur, çekmece değil. Banka bir `ModalPencere`'dir. Hakemin maddesi "panel olmayan PRO çekmeceler"i kapsıyordu. SatınAlma çekmecesi de aynı sınıfta ama SATINALMA-EKRAN şeridinin alanı olduğu için dokunulmadı (§12).
- **Mevzuat:** Yeni bir yasal sayı ya da hüküm yazılmadı. Had, kalan hak ve kural etkinliği sunucudan gelir (`/tevsik/gunluk`; YASAL-1 tarihli tablo, 459 s. VUK GT). Mevcut kural birebir korundu. Web araştırması gerekmedi, çünkü yeni hüküm yok.

## 3. Migration
Gerekmedi. Şema değişikliği yok.

## 4. Testler
Yeni dosya: `Kuyumcu.Tests/YasalKapi2NakitSiniriTestleri.cs`. Toplam 20 vaka: 5 saf metot (4 satırlı Theory ile 8 vaka) ve 9 bUnit ekran metodu (Theory'lerle 12 vaka).

| Test | Kanıtladığı |
|---|---|
| YK3_S01 | 29.999,99 / 30.000 / 30.000,01 / 250.000 sınır tablosu. Kart ve havalede nakit sınırı yok. |
| YK3_S02 | Bugünkü nakit düşülür (kalan 12.000). Kalan 0 olunca nakit kapanır ve tek cümle çıkar. |
| YK3_S03 | Kural kapalıysa ya da kümülatif hesaplanamadıysa ön kontrol yok. |
| YK3_S04 | Sunucu reddi izi: tutar aşağı, ödeme türü değişince, müşteri değişince kalkar; yukarı çıkınca geri gelir. Kodla tanınır. |
| YK3_S05 | Sözlük TR ve EN dolu. |
| YK3_01 | 250.000 Nakit: alan altı cümle, form yok, kilit, "kimlik" denmez. Kart → hata kalkar, form açılır. Nakit → geri gelir. Havale → kalkar. Formsuz POST yok. |
| YK3_02 ×3 | Sınırda kilit ve POST: 29.999,99 ve 30.000 POST gider; 30.000,01 POST yok. |
| YK3_03 | Yukarı-aşağı 6 adım; her değişimde yeniden değerlendirilir. |
| YK3_04 | Sunucu `NAKIT_SINIRI` → bantta değil alan altında. Kart → kalkar, Nakit → geri, tutar inince kalkar. |
| YK3_05 | Hak 0 → Nakit seçilemez, gerekçeli. Varsayılan Kart. |
| YK3_06 | Kural kapalıysa ön kontrol yok; MASAK formu açılır. |
| YK3_07 ×2 | BASİT ve PRO: `#masak-kural` yok, "teyidi" yok, eşik tek kez, açıklama katlanırdan önce, form açıkken `#th-ipucu` yok. |
| YK3_08 | Döviz: 10.000 USD (340.000 TL) → cümle; gereksinim geldikten sonra da form yok; kilit. 800 USD → kalkar, POST gider. |
| YK3_09 | Ödeme türü değişince sunucunun eski tutar reddi alan altından kalkar. |

Ayrıca `YasalKapi2Testleri.YK2_01` PRO beklentisi 2'den 1'e indirildi (aynı dosya ailesi, dosya alanında). Yeni beklenti: PRO çekmecede eşik yalnız başlıkta.

**Filtreli koşumlar (son kod, yeşil):**
- `YasalKapi2` (YK2 ve YK3): 79/79.
- `YasalKapi2|TevsikEkran|CariEkran|SiparisDetay|Kapora`: 303 başarılı, 1 atlandı, 0 başarısız.
- `SonKalanlarTahsilat|YasalKapi2|Y4C_44|Y4C_K12|P37c|S1iGorselSigma2`: 122/122.

**Tam süit** (`/private/tmp/kuyumcu-serit/trx-pYASAL-KAPI-3`, son kodla 2. koşum, `tam2.log`):
- 12.277 test: 12.270 geçti, 4 başarısız, 3 atlandı. Kırmızıların hiçbiri bu paketin değil:
  - `RaporDortTestleri.RD_KH01` ve `RD_KH02`: RAPOR-DORT şeridine ait, devirde bilinen; tarihe bağlı 400 gün sınırı.
  - `Yildiz4ATestleri.Y4A_10_Yildiz_BASIT_Panel…` ve `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12…`: yalıtık koşumda 2/2 yeşil (`t5.log`). Yük altındaki kararsızlık. Bu paketin dosyalarına ve ekranlarına dokunmuyor.
- 1. koşum (ara sürüm, `tam.log`): 10 kırmızı. Bunların 2'si bu paketindi (SKC01 BASİT ve PRO). Kök neden §2.7'deki tutar satırı; kaldırıldı ve 2. koşumda yeşil. Kalan 8'in sahipleri:
  - `T5_Islemler_razor_1400…` ve `S21…1400_satiri_asmaz`: başka şeridin o anki `Islemler.razor`'u 1.413 satırdı; 2. koşumda yeşil.
  - P37c, Y4C_K12, S1i_06, Y4C_44: yalıtık koşumda yeşil.
  - RD_KH01 ve RD_KH02.
- Devir notundaki `S24EskiAltinEkranTestleri` (ISLEM-BANT-2) bu koşumda kırmızı değildi.
- **Bu paketin yeni kırmızısı 0.**

## 5. Mutasyon kanıtları
Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-KAPI-3/` altına alındı. Tek eşleşmeli `perl -0777` değişikliği yapıldı. Ardından `--filter YasalKapi2` koşuldu, yedek geri kopyalandı ve `cmp` ile denetlendi. Betik `mut.sh`, özet `mut-ozet.txt`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | `olcuTl > k` → `>=` (tam had serbestliği) | YK3_S01(30000), YK3_S02, YK3_02(30000), YK3_03 | AYNI |
| M1-2 | `ThMasakCizilir` → yalnız `PM.FormCizilir` (nakitte form doldurtma) | YK3_01 | AYNI |
| M1-3 | `ThNakitSinirAsildi` günlük hak dalı silindi | YK3_01 | AYNI |
| M1-4 | `NAKIT_SINIRI` yakalayıcısı kapatıldı (`false &&`) | YK3_04 | AYNI |
| M1-5 | `MasakFormu` `!KuralGizli` koşulu silindi | YK3_07(PRO), YK2_01(PRO) | AYNI |
| M1-6 → M1-6b | Döviz `DtNakitAsildi` → `false` | ilk koşumda 0 kırmızı (TEST AÇIĞI) → YK3_08 güçlendirildi (gereksinim yanıtından sonra da form yok) → **YK3_08** | AYNI |
| M1-7 → M1-7b | Ödeme türü değişince `thTutarSunucuHata = null` silindi | ilk koşumda 0 kırmızı (TEST AÇIĞI) → YK3_09 eklendi → **YK3_09** | AYNI |

7 kural mutasyonunun 7'si kırmızı test üretti ve hepsinde `cmp` AYNI çıktı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata, 0 uyarı (`build-son.log`). Tutar satırı geri alındıktan sonra `build5.log`: 0 hata, 0 uyarı.

Not: Tam yeniden derlemede başka şeritlerin 6 bilinen uyarısı görünür (`build1.log`): SUBEKAPSAMA CS8601, KesifP2 xUnit2029, SonKalanlar xUnit2013 ×2, Yildiz2 xUnit1031 ×2. Bu paketin dosyalarından uyarı yok.

## 6. denetim.sh
`bash ./denetim.sh` → `DENETİM BAŞARISIZ`. 315 ✓ ve 2 ✗; ikisi de bu paketin değil:
- `P-30 DEMO_INSAN_SURESI`: kabulde hariç tutulmuş, bilinen.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1`: `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN. Sahibi MASAK-ESZAMAN; YASAL-KAPI-2 raporunda da açık.

Bu paketin sayaçlarında ihlal yok: ModalPencere düzenleme artışı 0, satır içi stil 0, sabit renk 0.

## 7. Canlı doğrulama (API 5188, `kuyumcu_net`, kiracı `PAKETYASAL-KAPI-3-TEST`)

| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| 1 | GET `/tevsik/gunluk?cariId=A` | 200, had ve kalan hak | 200 `{had: 30000, kuralEtkin: true, kumulatifHesaplandi: true, bugunkuNakitToplam: 0, kalanNakitHakki: 30000}` |
| 2 | POST tahsilat A NAKİT 29.999,99 | 200 | 200 |
| 3 | POST tahsilat B NAKİT 30.000,01 | 400 NAKIT_SINIRI | 400 `kod=NAKIT_SINIRI` "30.000,01 TL nakit, 30.000,00 TL nakit sınırını 0,01 TL aşıyor; bu ödeme kartla ya da havaleyle yapılmalı." |
| 4 | POST tahsilat B NAKİT 30.000 | 200 (tam had serbest) | 200 |
| 5 | GET gunluk B | kalan 0 | 200 `bugunkuNakitToplam: 30000, kalanNakitHakki: 0` |
| 6 | POST B NAKİT 1 | 400 (kümülatif) | 400 `NAKIT_SINIRI` "Bu kişiyle bugün nakit toplamı 30.001,00 TL oluyor …" |
| 7 | POST B KART 1 | 200 | 200 |
| 8 | POST C KART 250.000, masaksız | 400 MASAK | 400 "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur …" |
| 9 | POST C NAKİT 250.000, beyanlı | 400 NAKIT_SINIRI (ekran artık buraya gelmeden durdurur) | 400 `NAKIT_SINIRI` "250.000,00 TL nakit, 30.000,00 TL nakit sınırını 220.000,00 TL aşıyor …" |
| 10 | POST C KART 250.000, beyanlı | 200 | 200 |

SQL (`hareketler` ⋈ `cariler`, kiracı süzgeçli) yalnız kabul edilen 4 yazmayı gösterdi:

```
Canli Sinir Ali Kaya    NAKIT  -29999.99  TAHSILAT
Canli Sinir Veli Demir  NAKIT  -30000.00  TAHSILAT
Canli Sinir Veli Demir  KART       -1.00  TAHSILAT
Mehmet Ali Öztürk       KART  -250000.00  TAHSILAT
masak_beyanlari (kiracı) = 1
```

Reddedilen hiçbir istek kayıt bırakmadı (30.000,01, 1, NAKİT 250.000).

Temizlik:
- `zsh betikler/kiraci-temizle.sh 01M4K5VZHS6329CZJTXWTKCSR3 --uygula` çalıştırıldı. Kiracı sorgusu 0 döndü. Davet satırı silindi (0).
- `tenants` başlangıçta 20 idi, sonda 22. Fark başka şeritlerin o arada açtığı test kiracılarıdır (ör. `PAKETYILDIZAMBLEM3-TEST`). Bu paketin kiracısı kalmadı.
- API, vekil ve Chrome süreçleri yalnız kendi PID'lerimle durduruldu. 5188, 5189 ve 9981 portları boş.

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9981, profil tek dizin `pYASAL-KAPI-3/profil`)

Ölçüm `sikisma-olc.js` (`__olcTam`) ve pakete özel denetimlerle yapıldı. 26 karakterlik kasa adı: "Hakem Ana Kasa Şube Merkez". Ham dosyalar:
- `olcum-cekmece.txt` (16 bileşim; 375 satırlarının en son koşumu esastır)
- `olcum-odeme.txt` (16 bileşim)
- 34 ekran görüntüsü (`m-*.png`, `t-*.png`, `d-*.png`)

### 8.1 Tahsilat çekmecesi: nakit sınırı değişimde (16/16 bileşim, müşteri Hatice, bugünkü nakit 0)
Platin ve Yıldız × BASİT ve PRO × açık ve koyu × 1280 ve 375 px. Her bileşimde 9 adım aynı sonucu verdi.

| Adım | Alan altı hata | Kimlik formu | Kaydet |
|---|---|---|---|
| Nakit 30.000,01 | VAR | yok | kilitli |
| Nakit 30.000 | yok | yok | açık |
| Nakit 29.999,99 | yok | yok | açık |
| Nakit 250.000 | VAR | yok | kilitli |
| Kart 250.000 | yok | AÇIK | kilitli (kimlik) |
| Nakit'e geri 250.000 | VAR | yok | kilitli |
| Havale 250.000 | yok | AÇIK | kilitli (kimlik) |
| Nakit 20.000 | yok | yok | açık |
| Nakit 250.000 (yukarı) | VAR | yok | kilitli |

Önceki koşumda aynı ekran, bugün 29.999,99 nakit almış bir müşteride doğru biçimde "Nakit en çok 0,01 ₺ olabilir" yazdı. Bu, sunucunun kümülatif kuralının kanıtıdır (`olcum-cekmece-kalan001.txt`).

Döviz tahsilat (`d.mjs`), platin 1280 BASİT açık ve Yıldız 375 PRO koyu:
- 10.000 USD (492.900 ₺) → "Nakit en çok 30.000 ₺ karşılığı olabilir; tutarı azaltın.", form yok, Kaydet kilitli.
- 500 USD → hata yok, Kaydet açık.
- 10.000'e geri → hata geri geliyor.

### 8.2 Kart 250.000: kimlik formu, ilk ekran ve sayaçlar

| Bileşim | Zorunluların alt kenarı / çekmece alt sınırı | Eşik "200.000" tekrarı | `#masak-kural` | İç not | K3/K3c/K10c/K5/Y | Çekmece içi binme | Tutar ve ödeme sağ kenarı |
|---|---|---|---|---|---|---|---|
| platin 1280 ×4 (B/P × a/k) | 5/5 içeride: açıklama 634–678 / 704 | 1 | yok | yok | 0/0/0/0/0 | 0 | 1264 = 1264 |
| yıldız 1280 ×4 | 5/5 içeride: açıklama 646–690 / 731 | 1 | yok | yok | 0/0/0/0/0 | 0 | 1264 = 1264 |
| platin 375 ×4 | 4/5: açıklama 739–783 / 716 (kısa kaydırma) | 1 | yok | yok | 0/0/0/0/0 | 0 | 359 = 359 |
| yıldız 375 ×4 | 4/5: açıklama 717–761 / 718 (kısa kaydırma) | 1 | yok | yok | 0/0/0/0/0 | 0 | 359 = 359 |

Sayfa geneli K9 = 6 (Yıldız 1280'de 4) ve Yıldız 1280'de K7 = 1. Bunlar çekmecenin ARKASINDAKİ sayfa öğeleridir: `#cari-segment-hepsi ⟂ #th-guncel` ve `div.yz-bas` başlık hizası. Örtü katmanının altında kalırlar. YASAL-KAPI-2 ölçümünde de aynı değerler vardı.

Çekmece içi binme ayrıca ölçüldü: kaydırma kabında görünen ve en üstte duran yazılar karşılaştırıldı. Sonuç 16/16 bileşimde 0.

### 8.3 Ödeme tablosu (`/islemler`, Kart, 220.000 ₺, 26 karakterlik kasa adı; 16/16)

| Bileşim | Ödeme satırı K9 (satır görünürken) | Tutar kesik | "Kart" dikey | Tam sayfa K9 (kaydırma 0) | K9 (sayfa sonu) | Görünür binme (başta/sonda) |
|---|---|---|---|---|---|---|
| platin 1280/375 × B/P × a/k (8) | 0 | 0 | 0 | 0 | 0 | 0/0 |
| yıldız 1280 × B/P × a/k (4) | 0 | 0 | 0 | 0 | 0 | 0/0 |
| yıldız 375 × B/P × a/k (4) | 0 | 0 | 0 | 6 (opak sticky alt şerit, §2.7) | 0 | 0/0 |

### 8.4 Görülen ekran görüntüleri (Read: 3)
- `e4-yildiz-BASIT-aydinlik-375-1.png` (eski hâl, 375): Ödeme satırı opak alt şeridin altında; yazı yazıya binmiyor.
- `t-yildiz-1280-BASIT-aydinlik.png`: "Kart · Hakem Ana Kasa Şube …" kısalıyor, "220.000,00 ₺" tam ve tek satır.
- `m-platin-1280-PROFESYONEL-karanlik.png`: 250.000 Kart çekmecesi. Tek satırlık başlık, belge türü ile T.C. no yan yana, ad soyad, amaç ve açıklama ilk ekranda. ₺ kutunun içinde, kural satırı yok. Kilit sebebi altlıkta tek cümle.

### 8.5 Yıldız koruması
- Yeni öğe eklenmedi. Yıldız'a giden tek kural ödeme adı sütununun yerleşimidir ve Yıldız seçicisiyle yazıldı.
- Eski ve yeni geometri ölçüldü; metin konumu ≤1 px değişti (§2.4).
- `tuval-kiyas.mjs` eşleme JSON'ları ağaçta bulunamadı, bu yüzden koşulamadı. YILDIZ-AMBLEM-2 raporu da aynı durumu not etmişti.
- Eşdeğer kanıt: Tezgâh tuvali (`Tezgah4.dc.html`) ödeme satırı `grid-template-columns: minmax(0,1fr) auto`, "Kart" + boşluk + `· Ana Kasa`. Uygulamada metin konumları değişmedi. Kalan tek fark, uzun kasa adının "…" ile kısalmasıdır.

## 9. DoD sözleşmesi
Yeni varlık ya da kayıt türü yok. Var olan uçlar ve düzenleme yolları değişmedi:
- Tahsilat ve döviz tahsilat finansal harekettir. Storno ve düzeltme fişi yolu var olduğu gibi kalır.
- MASAK beyanı tahsilatla birlikte yazılır.
- Yeni düğme yok. Kaydet kilidi sebebini alan altında yazar (UX-S30). Nakit seçeneği pasifken gerekçesini kendi satırında yazar.

## 10. Dokunulan sayfalar (TEK DOKUNUŞ kaydı)
- `Pages/Cariler.razor`: YASAL-KAPI-2'de de açılmıştı. Gerekçe: bu paket o paketin kapanış devridir; aynı çekmecedeki hakem bulguları.
- `Pages/SiparisDetay.razor`: yalnız `MasakFormu` bağlama parametresi (`KuralGizli="true"`), dosya alanı izniyle.
- `Bilesenler/MasakFormu.razor`: ortak bileşen, dosya alanında.

## 11. Kurucu / mali müşavir / Mehmet (güvenlik) notları
- Yeni yasal hüküm yazılmadı. Nakit sınırı, kalan hak ve kural etkinliği sunucunun tarihli tablosundan gelir (459 s. VUK GT, YASAL-1).
- Mali müşavir sorusu (önceki paketlerden açık): "200.000 ₺ amaç beyanı kademesi". PRO'da tezgâhtara "teyit bekleniyor" diye gösterilmesi bu pakette kaldırıldı. Teyit sorusu `KURUCU-KARAR-BEKLEYENLER` belgesinde duruyor.
- Güvenlik: kişisel veri kalıcı hâle getirilmedi. Kimlik formu çizilmediğinde model gövdeye gitmez (`PM.Govde` yalnız kapı açıkken).

## 12. Paket kapısı (paralel şerit kuralı)

| Madde | Durum |
|---|---|
| (1) build 0 hata | KARŞILANDI (0/0) |
| (2) bu paketin testleri 0 başarısız | KARŞILANDI (79/79) |
| (3) sayaçlar | KARŞILANDI (P-30 ve GIRDI-2 başkasının) |
| (4) mutasyon | KARŞILANDI (7/7 kırmızı, cmp AYNI) |
| (5) canlı | KARŞILANDI (10 adım ve SQL) |
| (6) kiracı silindi | KARŞILANDI |
| (7) yeni kırmızı | 0 |

## 13. Açık kalanlar (sahipleriyle)
- **YILDIZ-4D:** Yıldız 375 Tezgâh'ta opak, yapışkan alt şerit kaydırma 0'da ödeme satırını örtüyor (tam sayfa K9 = 6; görünür binme 0, sayfa sonunda 0).
  - Kurucunun "alt şerit içeriğin üstüne binmez" sözü kaydırma ortasında tartışmaya açıktır.
  - Kalıcı çözüm `YildizTezgah.razor` yapısıyla olur (ödeme satırı şeridin içine ya da içerik kendi kabında kayar); bu paketin alanı dışında.
- **375 px kimlik formu:** 5 zorunlunun 4'ü ilk ekranda, beyan açıklaması kısa kaydırmayla geliyor (platin 23 px, Yıldız 43 px). Tutar tam genişlik kararı (SKC01) korunduğu için daha fazla sıkıştırma yapılmadı. Karar tasarım hakemine.
- **SATINALMA-EKRAN:** SatınAlma çekmecesindeki `MasakFormu` (`SatinAlma.razor:232`) için `KuralGizli="true"` önerilir; o şeridin dosyası.
- **Ayrı sunucu işi (öneri, ORTA):** `CariModulu` tahsilatında tevsik denetimi MASAK'tan önce yapılabilir. Böylece istemci ön kontrolü olmayan yollarda da (API) nakit reddi önce gelir. Mehmet ve Taşkın görüşü gerekir.
- **RAPOR-DORT:** `RD_KH01`, `RD_KH02` (tarihe bağlı).
- **MASAK-ESZAMAN:** denetim `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`MASAKESZAMANSinirTestleri.cs:187`).
- **Bilinen:** denetim `P-30`.
- **Kararsız (yük altında):** `Y4A_10_Yildiz_BASIT_Panel…`, `Y4A_tur4_eszamanli_12…`. Yalıtık koşumda yeşil.

## 14. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden bu paketin dosyaları (çoğu dizin depoda izlenmediği için `??` görünür):
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalKapi3.cs` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Cariler.YasalKapi3.cs` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor` (tek öznitelik)
- `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/yasal.css`
- `dotnet/Kuyumcu.Tests/YasalKapi2NakitSiniriTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/YasalKapi2Testleri.cs` (YK2_01 PRO beklentisi)

Proje ağacına geçici dosya bırakılmadı. Geçici dosyalar `/private/tmp/kuyumcu-serit/pYASAL-KAPI-3/` ve `mut-yedek/YASAL-KAPI-3/` altında. `betikler/sikisma-olc.js` değiştirilmedi; pakete özel ölçümler `m.mjs`, `t.mjs`, `d.mjs` ile geçici dizinde yapıldı.
