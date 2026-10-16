# PAKET TOPLU-1 — Uygulama raporu (şerit t5 · 01.10.2026)

Rol: Taşkın (backend) + Ertuğ (frontend). Kaynak: `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` — bölümler
'01.10 05:40 KESIF-P5', '01.10 04:30 BELGE-KALAN', '01.10 03:10 CARI-BAKIYE', '30.09 22:40 KESIF-P7' (YÜKSEK) + QA10 gider kategorisi.
COMMIT YOK. Dosya silme YOK. Migration YOK (şema değişmedi; backend `hareketId` ile fişten-kes ve
`/faturalar/faturalanabilir-islemler` zaten vardı — yalnız ekran onu kullanmıyordu).

## 0. Devir (internet kesintisi)
İlk uygulayıcı 05:44–07:15 arası işi neredeyse bitirdi (kod, 22 test, 15 mutasyon, 4 mod/tema × 4 genişlik ölçüm)
ama rapor yazamadan düştü. Bu tur (08:59–09:40): ağaçtaki iş KORUNDU, yeniden yazılmadı. Devirde bulunan:
- Yarım mutasyon YOK: 15 yedeğin hepsi `cmp AYNI` (mut-sonuc.txt). Test kiracısı 01M3TNKV… duruyordu → iş sonunda silindi.
- Tek kırmızı: `TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok` — `Pages/Panel.razor` (8 satır) +
  `Bilesenler/Fis/FisProYanSutun.razor` (1 satır) dönüştürülmemişti → bu turda dönüştürüldü (§2.2).
- `MT1-11` mutasyonu DERLEME HATASI verdi (geçersiz kanıt); aynı kural `MT1-11b` ile geçerli biçimde kırıldı.

## 1. Yapılanlar (kuyruk maddesi → çözüm)
| # | Madde (önem) | Çözüm | Dosya |
|---|---|---|---|
| 1 | KRİTİK · PRO satışta müşteri seçicisi ezik, uzun ad üstten/alttan kırpık | P7 kuralı HER Secici'de: seçili değer kutuyu aşarsa kutu BÜYÜR (`:not(#_):not(#__)` özgüllüğü, !important yok); tuval-satis.css sabit 46 px kuralıyla çatışmaz | ortak.css (yalnız secici kuralı) |
| 2 | YÜKSEK · PRO 960 '+ Ayrıntı ekle' açıkken Vazgeç/Kaydet ayrı satır, ödeme hapı 3 satır | `#islem-alt-satir > .fis-pro-odeme-ozet-sar { flex: 1 1 auto }` (içerik tabanı) | tuval-satis.css |
| 3 | YÜKSEK (TARIH-TEK) · faturalandırılacak işlem seçicisinde ham UTC | `Bicim.IsletmeZamani` (Europe/Istanbul) + `TarihSaat/TarihTam/IsletmeSaati/IsletmeBicim/IsletmeBugun/Gun/GunBicim/Makine`; Razor/sayfa/bileşen/yerleşim kodunda ham biçim 0 (beyaz liste yok); denetim.sh `TARIH_TEK` sayacı | Bicim.cs, denetim.sh, ~40 razor (§2.2) |
| 4 | YÜKSEK (md.1) · taslak fatura 'Toplam' kilitli ama düzenlenebilir görünüp sahte başarı bandı | Kalemli taslakta Toplam SALT OKUNUR satır + "Toplam kalemlerden hesaplanır; kalemleri düzenleyin." notu; kalemsiz taslakta zorunlu girdi ve gerçekten gönderilir. Sunucu kalemli toplamı zaten 400 ile reddediyor (canlı §5) | Faturalar.razor (taslak çekmece) |
| 5 | YÜKSEK · düzenlenebilir taslağın Kalemler penceresinde 'Bu kayıt değiştirilemez' amber bandı | Amber DegismezIpucu yerine tek satır silme notu (`fatura-kalem-silme-notu`) | Faturalar.razor (kalem bandı) |
| 6 | YÜKSEK · Gün sonu 375'te tutarlar kesik | ≤560 px: `#mut-ozet-tablo` kaba sığar, tutar tek satır | ortak.css |
| 7 | YÜKSEK · 'Fişten Fatura Kes' fişsiz satışları bulamıyor | Sunucunun faturalanabilir listesi seçici olarak: fişli kayıt FİŞ (tüm kalemler), fişsiz kayıt tek işlem (`hareketId`); önizleme sunucu satırından. **Tur 2:** fişsiz seçimde eski fiş araması + Önizle düğmesi gizlenir (önizleme hazır; "fiş yok" boş durumu ve sebepsiz kilitli düğme kafa karıştırıyordu, UX-S30) | Faturalar.razor (işlem seçici) |
| 8 | YÜKSEK · cari detay uyuşmazlık uyarısı teknik, iki kez, bakiyenin yanında değil | Tek bant, bakiye kutularından hemen sonra; BASİT tek sade cümle; PRO iki fark (TL + has); TL uyuşmazlığı da bandı açar | Cariler.razor, CarilerSozlesmeleri.cs |
| 9 | YÜKSEK · BASİT cari detayda 'STORNO:' jargonu, iptal satırı 'Veresiye' adıyla | BASİT: "İptal" adı, STORNO öneki yok; PRO: storno etiketi | Cariler.razor, Sozlukler.Toplu1.cs |
| 10 | YÜKSEK · 375 PRO'da müşteri bakiyesi görünmüyor | ≤560 px PRO tablo satırı kart olur, TL/has sağda | ortak.css |
| 11 | YÜKSEK · /cariler BASİT 'Diğer süzgeçler' açıkken '+ Yeni müşteri' 14 px kayık | Süzgeç açıkken şerit `align-items: flex-end` + sarma | ortak.css |
| 12 | YÜKSEK · /vezne birim fiyat binliksiz, Toplam 12 px | Binlikli girdi; `form-toplam-satir` gövde boyu, kalın | Vezne.razor, ortak.css |
| 13 | YÜKSEK · /vezne BASİT açıklama/tasarım notu açık | BASİT'te katlı (`zo-ipucu` details), PRO açık | Vezne.razor |
| 14 | YÜKSEK · Hurda hata bandında 'TRY' ve biçimsiz tutar | `KasaBakiyeMesaji.Bicimle`: sunucu metni ayrıştırılır, `Bicim.Para` ile tr-TR cümle (sunucu metni değişmez — Gider P-50 onu ayrıştırıyor) | KasaBakiyeMesaji.cs (yeni), Hurda.razor, Islemler.Odeme.cs |
| 15 | YÜKSEK · MASAK 'Beyan Açıklaması' monospace | Kök neden `input, select { font: inherit }` textarea'yı saymıyordu → `textarea { font-family: inherit }` | ortak.css |
| 16 | QA10 · Gider kategorisi tek seçenekte '—' açılır | UX-S22: tek aktif kategori → seçici gizli, salt okunur satır, gövdeye otomatik | Gider.razor |
| — | DÜŞÜK · /denetim-izi K9, /islemler BASİT 1280 'Kasa seçin…' sarması | Bu pakette ele alınmadı (DÜŞÜK; kuyrukta kalır) | — |

## 2. Sapmalar ve gerekçeleri
### 2.1 Şerit sınırı (TARIH-TEK) — BAŞKA ŞERİTLERİN DOSYALARINA DOKUNULDU
TARIH-TEK kuralı "ekrandaki HER tarih, sayaç 0, beyaz liste yok" dediği için dönüşüm tüm Razor'a uygulandı.
İlk uygulayıcı ~37 dosyayı (yedekleri `/private/tmp/kuyumcu-serit/TOPLU-1/tarih-yedek/`) dönüştürdü; bunların bir
kısmı başka şeritlerin alanında: **KESIF-P6** (Kur, Siparisler, Uyum, Banka), **YASAL-4** (GiderPusulalari). Değişiklik
yalnız tarih biçimleme çağrısının `Bicim.*`'e çevrilmesidir (davranış: UTC → işletme saati). Bu turda ek olarak
`Panel.razor` (8 satır: son sorgu saati, son işlemler saati, bugün başlığı, 2 sorgu günü, grafik günleri) ve
`FisProYanSutun.razor` (1 satır) dönüştürüldü. Panel'in pusula satırlarına (YASAL-4 bölgesi) dokunulmadı.

### 2.2 ⚠ Eşzamanlı mutasyon yarışı (Anayasa md.1 — olduğu gibi)
- 09:02–09:04: KESIF-P6 Panel.razor üzerinde M6-12 mutasyonu koşarken ben Panel'i `cp` ile yedekleyip geri
  yükledim; benim geri yüklemem onların mutasyon artığını (`<div id="kur-kaynak" role="combobox"></div>`, satır 220)
  ağaca geri koydu. Fark edildi (KesifP6Panel testleri kırmızı), artık `perl` ile söküldü ve dosya KESIF-P6'nın
  kendi yedeği `mut-yedek/KESIF-P6/t3/M6-12-Panel.razor` ile **cmp birebir aynı** (o yedek benim tarih
  değişikliğimi zaten içeriyordu). KesifP6Panel 7/7 + filtreli 162/162 yeşil.
- 09:16–09:17: YASAL-4 Faturalar.razor'da mutasyon (`@if (false)`, satır ~1034 özel matrah) koşarken benim fisten
  düzenlemem onların geri yüklemesiyle silindi; ağaç onların temiz hâline döndü, düzenleme `perl` ile YERİNDE
  yeniden uygulandı (cp yok). Fatura+Toplu1 filtreli 265/265 yeşil.
- Ders: paylaşılan dosyada `cp` ile geri yükleme YAPILMAZ; MT1-16/MT1-17 bu yüzden "perl ile boz → perl ile
  ters çevir → cmp" yöntemiyle koşuldu. Orkestratöre öneri: mut.sh'lar paylaşılan razor'da cp-geri yükleme yerine
  ters ikame kullanmalı.

### 2.3 Diğer
- Katlanir.razor (SATIS-2) beklenmedi; Vezne açıklaması mevcut `zo-ipucu` details kalıbıyla katlandı.
- Backend değişikliği gerekmedi: `/faturalar/fisten` `hareketId` alıyordu, faturalanabilir uç vardı.

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Testler
Yeni dosya `dotnet/Kuyumcu.Tests/Toplu1Testleri.cs` — 22 test (Toplu1SafTestleri, Toplu1FaturaTestleri, Toplu1CariTestleri,
Toplu1VezneGiderTestleri, + fişsiz faturalanabilir HTTP testi). Ayrıca mevcut testlerde Toplu1'e uyarlanan beklentiler
(FaturaEkranTestleri, GiderEkranTestleri, P12/P19 ekran, CariBakiyeTestleri, Bicim2GirdiBicimi, KesifP2, Q105Sinir).
- Filtreli (Toplu1|CariBakiyeEkran|Panel|FisPro): **162/162** · Filtreli (Toplu1|Fatura): **265/265**.
- Tam süit: §8.

## 5. Mutasyon tablosu (her biri: kuralı kır → test kırmızı → geri al → cmp AYNI)
| Kod | Bozulan kural | Kırmızı test(ler) | cmp |
|---|---|---|---|
| MT1-1 | Secici büyür (overflow visible) | Secici_P7_kurali_sayfa_sabit_boyunu_asar_kutu_buyur_kirpmaz | AYNI |
| MT1-2 | Secici özgüllük (#__) | Secici_P7_kurali_sayfa_sabit_boyunu_asar_kutu_buyur_kirpmaz | AYNI |
| MT1-3 | PRO ödeme özeti flex tabanı | Satis_PRO_dar_seritte_odeme_ozeti_icerik_tabani_alir | AYNI |
| MT1-4 | Bicim.TarihSaat işletme saati | TARIH_TEK_isletme_saati_UTC_gece_yarisi_ertesi_gun; Fisten_fatura_kes_fissiz_satisi_bulur_ve_hareketId_gonderir | AYNI |
| MT1-5 | Kilitli toplam salt okunur | Kilitli_toplam_salt_okunur_satir_duzenlenebilir_gorunmez | AYNI |
| MT1-6 | fişsizde fisId=null | Fisten_fatura_kes_fissiz_satisi_bulur_ve_hareketId_gonderir | AYNI |
| MT1-7 | fişsiz seçenek listede | Fisten_fatura_kes_fissiz_satisi_bulur_ve_hareketId_gonderir | AYNI |
| MT1-8 | BASİT iptal adı (STORNO yok) | Iptal_satiri_BASIT_iptal_adiyla_STORNO_jargonu_yok; Iptal_satiri_PRO_storno_etiketli | AYNI |
| MT1-9 | BASİT uyuşmazlık sade cümle | Uyusmazlik_BASIT_teknik_degil_tek_sade_cumle | AYNI |
| MT1-10 | TL uyuşmazlığı bandı açar | CariBakiyeEkranTestleri.TL_uyusmazlik_bandi_yalniz_fark_varken_ve_dogru_metinle | AYNI |
| MT1-11 | (geçersiz: derleme hatası) | — | AYNI |
| MT1-11b | Vezne BASİT notlar katlı | Vezne_BASIT_notlar_ve_aciklama_katli_PRO_acik; Vezne_PRO_notlar_ve_aciklama_acik | AYNI |
| MT1-12 | Gider tek kategori gizli | Gider_tek_kategori_secici_gizli_otomatik_gonderilir | AYNI |
| MT1-13 | Kasa bakiye reddi biçimli | Kasa_bakiye_reddi_tr_TR_bicimli_TRY_yok | AYNI |
| MT1-14 | Vezne toplam belirgin | Vezne_birim_fiyat_binlikli_toplam_belirgin_ve_govde_dogru | AYNI |
| MT1-15 | Kalemler penceresinde değiştirilemez bandı yok | Taslak_kalemler_penceresinde_degistirilemez_bandi_yok | AYNI |
| MT1-16 | TARIH-TEK (FisProYanSutun ham ToString) | TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok | AYNI |
| MT1-17 | fişsiz seçimde Önizle gizli (tur 2) | Fisten_fatura_kes_fissiz_satisi_bulur_ve_hareketId_gonderir | AYNI |

Not: MT1-1…MT1-15 koşumunda `TARIH_TEK_*` her mutasyonda da kırmızıydı — o anda ağaçta başka dosyalarda (Panel,
FisProYanSutun, eşzamanlı şeritlerin yeni satırları) ihlal vardı; mutasyona özgü kırmızı yukarıda ayrıca adlandırıldı.
Son mutasyondan sonra TEMİZ build: 0 hata.

## 6. denetim.sh
`TOPLU-1: TARIH_TEK (ham tarih biçimi/ToLocalTime) 0 ✓`. Tek ✗: **P-30 DEMO_INSAN_SURESI** (kronometreli demo ölçümü —
kapsam dışı, görevde "P-30 hariç" denmişti). Diğer tüm sayaçlar ✓.

## 7. Canlı doğrulama (API 5166, vekil 5167, kiracı PAKETTOPLU1-TEST 01M3TNKV…)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /giderler tutar 999999 (kasa 13.407,25) | 400 + sunucu metni KasaBakiyeMesaji kalıbına uyar | 400 `'Ana Kasa' kasasının bakiyesi yetersiz: 13407.25 TRY var, 999999.00 TRY isteniyor…` → kalıp TUTTU (ad=Ana Kasa, b=13407.25, pb=TRY, i=999999.00) |
| GET /faturalar/faturalanabilir-islemler?gun=30&limit=50 | fişsiz satışlar listede | 200 · 10 işlem, 10'u fisId=null; tarih `2026-10-01T02:46:17` (UTC) |
| Tarayıcı fişten-kes seçicisi | aynı işlem TSİ ile | "01.10.2026 05:46 · Satış · …" (02:46 UTC + 3) |
| POST /faturalar/fisten {hareketId:01M3TNP0H3F0…} | 200 taslak, hareket damgalı | 200 TASLAK 01M3V219…, toplam 10.976,51; SQL `hareketler.fatura_no = TASLAK:01M3V219…`, fis_id NULL |
| PATCH /faturalar/{kalemli taslak} {toplam:1} | sunucu reddeder (ekran salt okunur olmalı) | 400 "Kalemli faturanın toplamı başlıktan değiştirilemez…"; SQL toplam 10976.51 DEĞİŞMEDİ |
| Seçimden sonra liste | damgalanan işlem listeden düşer | ilk seçenek artık "22 Ayar Yarım Altın" (Çeyrek düştü) |

Temizlik: API (5166) ve vekil (5167) durduruldu (`lsof` boş). Kiracı `zsh kiraci-temizle.sh 01M3TNKVXHKTRV0K08WSPFBVTQ --uygula`
ile 74 tablodan silindi; SQL: tenants'ta 0, hareketler 0, faturalar 0. tenants sayısı 14 (başlangıçta bizden önce 12 kiracı vardı;
aradaki fark eşzamanlı şeritlerin kiracıları: PAKETYASAL4-TEST-T1, PAKETKESIFP8-TEST — onlara dokunulmadı).

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, cdp.mjs, sikisma-olc.js)
Tur 1 (önceki uygulayıcı, `olcum-tur1.jsonl`, 07:01–07:15): 9 ekran × 1440/1280/960/375 × BASİT/PRO × açık/koyu = 288 ölçüm
+ cari uyuşmazlık (CDP Fetch ile fark enjekte) 1280/375 × 4 + satış ayrıntı 960/1280.
Tur 2 (bu tur): panel, satis-pro-yan, fisten-fissiz × 1280/375 × BASİT/PRO × açık/koyu + textarea yazı ailesi.

| Ekran | Gen. | Mod×Tema | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10 | yatay | Not |
|---|---|---|---|---|---|---|---|---|---|---|
| satis-musteri | 1440/1280/960/375 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 70 karakterlik ad 2 satır, kırpılmıyor |
| cariler-liste | 4 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 375'te bakiye hücresi görünür (x 177–346) |
| cariler-diger | 4 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | '+ Yeni müşteri' kutularla alttan hizalı |
| cari-detay | 4 | 4 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | |
| cari-uyusmazlik | 1280/375 | 4 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | BASİT: "Bu müşterinin bakiyesi kontrol edilmeli. Yöneticinize haber verin." |
| vezne-yeni | 4 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 125.000 ₺, toplam 18 px |
| gunsonu | 4 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 375'te tutarlar sığıyor |
| gider | 4 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | tek kategori "Kira" sabit, seçici yok |
| fisten / fisten-fissiz | 4 / 1280,375 | 4 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | fişsiz seçimde fiş araması+Önizle yok, toplam 21.953,04 ₺ |
| fatura-taslak | 4 | 4 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | Toplam salt okunur "20.000,00 ₺" (P etiketi) |
| panel | 1280/375 | 4 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | "Son sorgu: 09:13:41", "1 Ekim 2026, Perşembe", son işlem "06:02" |
| satis-pro-yan | 1280/375 | PRO×2 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | son işlem "1 Ekim" |
| textarea | 1280 | PRO açık | — | — | — | — | — | — | — | textarea = ebeveyn ailesi (Manrope…), monospace değil |

\* K9=6: çekmece/modal AÇIKKEN arka plandaki tablo başlıklarının (th MÜŞTERİ/TELEFON) çekmeceyle kesişmesi — örtü
tasarımı gereği; görüntüde metin üstüste binmesi yok (bkz. fatura-taslak, fisten-fissiz görüntüleri).
Görüntüler `/private/tmp/kuyumcu-serit/TOPLU-1/foto/` (179 PNG); Read ile görülenler: satis-musteri PRO 1280, panel PRO koyu 375,
fatura-taslak BASİT 1280, gunsonu PRO koyu 375, fisten PRO 375, satis-ayrinti PRO 960, fisten-fissiz PRO koyu 375 ve BASİT 1280.

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Fatura (taslak) mevcut PATCH/iptal uçlarıyla düzenlenir; kalemli taslakta toplam
neden düzenlenemez ekranda yazılı ("Toplam kalemlerden hesaplanır; kalemleri düzenleyin."). Fişsiz satıştan kesilen
taslak, mevcut taslak düzenle/iptal yoluna düşer. Finansal hard DELETE yok.

## 10. Değişen dosyalar (TOPLU-1'in)
Yeni: `Kuyumcu.Core/I18n/Sozlukler.Toplu1.cs`, `Kuyumcu.Web/Servisler/KasaBakiyeMesaji.cs`, `Kuyumcu.Tests/Toplu1Testleri.cs`.
Değişen: `Kuyumcu.Web/Servisler/Bicim.cs`, `wwwroot/css/ortak.css`, `wwwroot/css/tuval-satis.css`, `Pages/Faturalar.razor`
(taslak çekmece + kalem bandı + işlem seçici), `Pages/Cariler.razor`, `Contracts/Sayfalar/CarilerSozlesmeleri.cs`,
`Pages/Vezne.razor`, `Pages/Hurda.razor`, `Pages/Gider.razor`, `Pages/Islemler.Odeme.cs`, `Pages/Panel.razor`,
`Bilesenler/Fis/FisProYanSutun.razor`, `denetim.sh` + TARIH-TEK dönüşümü (tarih-yedek listesi, 37 dosya) + uyarlanan testler (§4).
(Ağaç git'te tek "first commit" üzerinde, diğer şeritlerle iç içe; `git status` tek başına pakete ait değişikliği ayırmaz.)

## 11. Koşul karşılama
| Koşul | Durum |
|---|---|
| build 0 hata (TOPLU-1 kaynaklı uyarı 0) | ✓ (kalan 2 uyarı: SUBEKAPSAMASinirTestleri CS8601, KesifP2Testleri xUnit2029 — başka paketlerin) |
| TOPLU-1 testleri 0 başarısız | ✓ |
| denetim TARIH_TEK 0, TEMİZ (P-30 hariç) | ✓ |
| mutasyon (her kritik kural) | ✓ 17 geçerli |
| canlı API + SQL | ✓ |
| tarayıcı ölçümü + görüntü | ✓ |
| test kiracısı silindi | ✓ |
| yeni kırmızı üretmedi | §12 |

## 12. Tam süit
`dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pTOPLU-1` (KUYUMCU_TEST_DB=kuyumcu_test_t5, 09:27–09:31, son kodla):
**Toplam 8571 · Geçti 8570 · Başarısız 0 · Atlandı 1** (trx: `_Ramazan-MacBook-Air_2026-10-01_09_27_17.trx`).
Yeni kırmızı 0 → (7) sağlandı. Başkasına ait kırmızı da bu koşumda yok.

## 13. Açık kalanlar
- DÜŞÜK (kuyrukta kalır): /denetim-izi K9 (P7 öncesinden), /islemler BASİT 1280 "Kasa seçin…" sarması.
- denetim.sh P-30 DEMO_INSAN_SURESI ✗ — kronometreli demo ölçümü (kurucu/orkestratör işi; kapsam dışı).
- Uyarılar (başka paketlerin): SUBEKAPSAMASinirTestleri.cs(49) CS8601, KesifP2Testleri.cs(454) xUnit2029; MSB3026 kopyalama yeniden denemesi (başka şeritlerin API süreçleri Api.dll’i tutuyor; yeniden denemeyle kopyalandı, cmp aynı).
- Eşzamanlı mutasyon yarışı (§2.2): KESIF-P6 ve YASAL-4 şeritlerinin bilgisine — Panel.razor ve Faturalar.razor şu an temiz (mutasyon artığı yok).

## 14. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı ya da değiştirilmedi (TARIH-TEK yalnız gösterim saat dilimi; işletme günü kuralı sunucunun `Zaman.UtcdenYerele` kuralıyla aynı). Soru yok.

---

# TUR 2 — doğrulayıcı ve tasarım hakemi bulguları (01.10.2026 10:26–11:50, şerit t5)

Ağaçtaki iş korundu, yeniden yazılmadı. Devirde yarım mutasyon yoktu (tur 1'in 17 yedeği cmp AYNI). Hakemin test kiracısı
(01M3V4AB…) zaten silinmişti. Başlangıçta tenants = 13.

## T2.1 Bulgu → çözüm
| Bulgu (önem) | Çözüm | Dosya |
|---|---|---|
| KRİTİK · BASİT satışta uzun ad "Borcu" rozetinin altında kesik (K9=1) | BASİT tuvalde bakiye rozeti artık kutunun İÇİNE bindirilmiyor: kendi satırında (grid-row 2, sola yaslı), PRO 375'teki kuralla aynı. Ad kutunun tamamını kullanır, sarar. Rozetin genişliği tutara göre değiştiği için sabit sağ boşluk ayrılmadı, o çözüm bir sonraki uzun tutarda yine bindirirdi. | tuval-satis.css |
| KRİTİK · taslak çekmecede "toplam sunucuda yeniden hesaplanır" jargonu | Yeni metin `T1.SatirDuzeltIpucu`: "…toplam kendiliğinden güncellenir." (EN "updates by itself"). `FT.SatirDuzeltIpucu` (FaturaTaslak dosyası, başka paketin) değişmedi, yalnız ekrandaki çağrı değişti. | Sozlukler.Toplu1.cs, Faturalar.razor |
| KRİTİK · Fişten Fatura Kes'te Alış "Tek satış" | Rozet işlem türünden geliyor: ALIS/HURDA_ALIS → "Tek alış", diğerleri → "Tek satış". Alışın listede kalıp kalmayacağı aşağıda (T2.6) yazılı. | Faturalar.razor |
| KRİTİK · fişsiz satıştan kesilen taslakta "Adet satış fişinden gelir … Fişten değiştir", listede "Manuel" yazıyor | Sunucu iki alan türetiyor (yeni kolon yok, kalemlerin defter bağından): fatura `islemdenGeldi` ve kalem `hareketFissiz`. Liste PRO kaynak satırında "Satıştan geldi"/"Alıştan geldi" yazıyor. Fişsiz kalemin adet kilidinde "Adet satış kaydından gelir; burada değiştirilemez." ve **İşlem kaydını gör** → /raporlar/islem-defteri. Fişli kalemde eski metin ve /islemler bağlantısı duruyor. Kilit sunucunun 400 kuralı. | FaturaModulu.cs, FaturalarSozlesmeleri.cs, Faturalar.razor |
| KRİTİK · TARIH-TEK: aynı sütunda "30.09.2026" ile "1.10.2026" | `Bicim.Tarih` artık tek kalıp `dd.MM.yyyy` (TarihTam), dilden bağımsız. `IsletmeBicim`/`GunBicim` kültürün kısa kalıplarını ("d", "g", "G") tek kalıba çeviriyor. /raporlar/kasa gün anahtarı `Bicim.Gun(g)` ile basılıyor: takvim günü saat dilimine çevrilmiyor. Eski kodda gün Local'a damgalanıp çevriliyordu. denetim.sh TARIH_TEK sayacına iki desen eklendi: Bicim'e verilen `"d.MM"` kalıbı ve `{x:d}/{x:g}`. | Bicim.cs, KasaRaporu.razor, denetim.sh |
| KRİTİK · cari detay ve /cariler maddeleri tarayıcıda doğrulanamamıştı | Bu turda canlı ölçüldü (T2.4): gerçek storno, SQL ile verilen +250 ₺ ve +0,5 has uyuşmazlık, 86 karakterlik ad. | — (ölçüm) |
| ORTA · KUYUM taslak çekmecesinde amber "Bu kayıt değiştirilemez" bandı | Kayıt düzeyindeki DegismezIpucu kaldırıldı, yerine alan düzeyinde nötr not var. Satırlı taslakta "Toplam satırlardan hesaplanır.", satırsız kuyum taslağında gerekçe ve yol düz metin olarak (`#fd-duzenle-toplam-sebep`). | Faturalar.razor |
| DÜŞÜK · yarışta ham "zaten  ile faturalandırılmış" (boş numara) | Sunucu: TASLAK'ın numarası yok, mesaj artık "Bu hareket zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)". Mevcut sözleşme testlerinin aradığı "zaten" ve "faturalandırılmış" sözcükleri korundu. İstemci: `FistenRetMetni` reddi FistenFaturaliMesaj kalıbıyla sade cümleye çeviriyor, numara varsa cümlenin içinde. | FaturaModulu.cs, Faturalar.razor, Sozlukler.Toplu1.cs |
| DÜŞÜK · Vezne ₺ son eki ~10 px, "Fiş toplamı" "Kalem ekle"ye yapışık | `.form-toplam-satir` üst boşluğu `var(--bosluk-3)` (ölçülen 12 px). `#vz-form .alan-son-ek` gövde boyunda (18 px). | ortak.css |
| (canlı ölçümde bulundu) Fişten seçicide K3 1 taşan + 9 kırık | Seçenek alt bilgisi (cari adı) `nowrap` olduğu için taşıyordu, rozet kelimesinden kırılıyordu. Artık alt bilgi sarıyor, rozet tek satırda kalıyor (yalnız seçici kuralı). | ortak.css |
| DÜŞÜK · DateTime.Today/Now 34 kullanım (ayrı paket önerisi) | **Yapılmadı.** Kullanımların çoğu diğer şeritlerin dosyalarında (Panel, Banka, IslemDefteri…). Ayrı paket olarak açık kalıyor (T2.7). | — |
| DÜŞÜK · paylaşılan dosyada mutasyon geri yükleme yarışı | Bu turdaki bütün mutasyonlar ters ikameyle geri alındı (perl ile boz → ters perl → yedekle cmp). cp ile geri yükleme kullanılmadı. | mut1.zsh |

## T2.2 Uyarlanan mevcut testler (TARIH-TEK kuralı değiştiği için, gerekçe test içinde yazılı)
- BelgeKalanTestleri.BK2_Bicim_isletme_gunu_Istanbul ve BK2_istemci_gunu_isletme_dilimine_sabit: "1.10.2026" yerine "01.10.2026"; kaynak sözleşmesi TarihTam'a bağlandı.
- Dogrulayici/BELGEKALANSinirTestleri: DGR_BK2_isletme_gunu_sinir beklentileri "01.10.2026" ve "01.01.2027". DGR_BK_istemci_gunu_liste_gunuyle_ayni karşılaştırması dd.MM.yyyy ile yapılıyor. Eski beklenti iki biçimli hatayı test içinde sabitliyordu.
- TM1MusterilerTuvalTestleri.TM1_13: EN de "20.09.2026 08:00" (eskiden en-GB "20/09/2026").
- FaturaEkranTestleri.KuyumTaslaginaToplamGonderilmez: amber bant yerine `#fd-duzenle-toplam-sebep`.

## T2.3 Testler
Yeni test yöntemi 13 (16 durum), hepsi `Toplu1Testleri.cs` içinde:
- Saf (5): T2_BASIT_bakiye_rozeti_kutuya_bindirilmez_kendi_satirinda, T2_Vezne_toplam_ust_bosluk_ve_para_son_eki_govde_boyu, T2_Gun_sonu_gecmis_gun_anahtari_tek_kalip, T2_Secici_secenek_alt_bilgi_sarar_rozet_tek_satir, TARIH_TEK_tek_kalip_sifirli_gun_dilden_bagimsiz.
- bUnit (8): T2_Kuyum_taslak_cekmecesinde_degistirilemez_bandi_yok_notr_not, T2_Satirsiz_kuyum_taslakta_gerekce_notr_metin, T2_Taslak_satir_ipucunda_sunucu_jargonu_yok, T2_Adet_kilidi_gercek_kaynagi_soyler (fişsiz/fişli), T2_Liste_islemden_gelen_belge_kaynak_rozeti, T2_Fisten_secici_alis_islemi_tek_alis_rozeti, T2_Fisten_yaris_reddi_sade_cumle (3 durum), T2_Liste_tarihi_tek_kalip.
- HTTP: Fissiz_satis_faturalanabilir_listede_ve_hareketId_ile_faturalanir genişletildi. İçinde islemdenGeldi (liste ve tekil), hareketFissiz ve ikinci POST'un 400 mesajı (birebir) sınanıyor.
- TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok, denetim.sh'taki iki yeni desenin aynısını taşıyor.

Filtreli koşumlar: Toplu1 + FaturaFisten + FaturaKalem + AuthSifirlaEkran **147/147**. Toplu1 + KasaRaporu + GunSonu + Secici **201/201**. Toplu1 + FaturaEkran + BelgeKalan + BELGEKALAN + ZoBelge + Bicim + Tarih: 699/700 çıktı; tek kırmızı TM1_13 idi, uyarlandı.

## T2.4 Gerçek tarayıcı ölçümü (başsız Chrome, cdp.mjs + sikisma-olc.js; API 5166, vekil 5167, CDP 9501–9513)
Kiracı: PAKETTOPLU-1-TEST Tur3 (01M3V65B…). Veri:
- "Tuna Akarsu" PATCH ile "Abdurrahman Hüsnügüzel Karaosmanoğlu Mücevherat ve Kuyumculuk Ticaret Limited Şirketi" adını aldı (86 karakter, borcu 21.991,14 ₺).
- Kemal'in tahsilatı 48-A storno ucuyla iptal edildi.
- Kemal'in `cari_bakiyeler` satırına SQL ile +250 ₺ ve +0,5 has uyuşmazlık verildi.
- Fişsiz satıştan /faturalar/fisten ile KUYUM taslak kesildi.

| Ekran | Genişlik · mod · tema | K1 | K3 | K5 | K7 | K8 | K9 | K10 | yatay | Gözlem |
|---|---|---|---|---|---|---|---|---|---|---|
| /islemler müşteri (86 karakter ad) | 1440 BASİT açık · 1440 BASİT koyu · 1280 BASİT koyu · 960 BASİT açık · 375 BASİT açık · 1440/1280 PRO açık · 960/375 PRO koyu | 0 | 0/0 | 0 | 0 | 0 | **0** | 0 | 0 | BASİT 1440: rozet x292–429, y183–211, kutunun altında; ad tek satıra sığıyor. 1280: ad 2 satır (metinH 38). Çakışma yok. Tur 1'de K9=1 idi. |
| /faturalar liste | 1440 PRO açık, 1280 BASİT açık, 375 BASİT koyu, 375 PRO koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | PRO kaynak satırı "Satıştan geldi"; tarih "01.10.2026" |
| /faturalar taslak çekmece | aynı 4 + 1440 BASİT açık | 0 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | Sebep notu "Toplam satırlardan hesaplanır.", amber bant yok, "sunucu" sözcüğü yok. Adet: "Adet satış kaydından gelir…" + "İşlem kaydını gör" → /raporlar/islem-defteri |
| /faturalar Kalemler penceresi | 1440 PRO açık, 1440 BASİT açık, 375 PRO koyu | 0 | 0/0 | 0 | 0 | 0 | 2–6* | 0 | 0 | Bağlantı /raporlar/islem-defteri, title "Adet satış kaydından gelir…" |
| /faturalar Fişten seçici (açık) | 1440 PRO açık, 375 BASİT koyu | 0 | **0/0** | 0 | 0 | 0 | 6* | 0 | 0 | "01.10.2026 10:35 · Alış · 925 Gümüş Bileklik · 3.840 ₺ · Anadolu Döküm … **Tek alış**"; uzun cari adı sarıyor. CSS düzeltmesinden önce K3 1/9 idi. |
| /cariler (BASİT "Diğer süzgeçler" açık) | 1440 BASİT açık, 375 BASİT koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | "+ Yeni müşteri" ikinci satırda, sağda; K7=0 |
| /cariler | 1280 PRO açık, 375 PRO koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 375 PRO: kartta "21.991,14 ₺ Bize borçlu" görünüyor |
| cari detay (Kemal) | 1440 BASİT açık, 375 BASİT koyu, 1440 PRO açık, 375 PRO koyu | 0 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | BASİT: tek sade bant "Bu müşterinin bakiyesi kontrol edilmeli. Yöneticinize haber verin."; satır "Tahsilat iptali · İptal sebebi: …"; "STORNO" yok. PRO: "(altın farkı 0,5000 has · TL farkı 250 ₺)" |
| /vezne yeni fiş | 1280/1440 BASİT açık, 1280 PRO koyu, 375 BASİT koyu, 375 PRO açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | ₺ son eki 18 px; toplam ile "Kalem ekle" arası 12 px; "Fiş toplamı 125.000 ₺" |
| /raporlar/kasa geçmiş kayıtlar | aynı 5 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | Tarih sütunu: 25.09.2026 … 30.09.2026, 01.10.2026 (tek kalıp) |
| /gider | aynı 5 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | — |

\* K9: çekmece ya da modal AÇIKKEN arka plandaki sayfa öğeleri örtüyle kesişiyor (tasarım gereği, tur 1 ile aynı). Görüntüde ön plandaki metin üst üste binmiyor.

Read ile bakılan görüntüler (`/private/tmp/kuyumcu-serit/TOPLU-1/tur3/f/`): satis_BASIT_aydinlik_1440 (önce), satis_BASIT_karanlik_1280, fisten_PRO_aydinlik_1440_acik, fatura_BASIT_karanlik_375_cekmece, fatura_BASIT_aydinlik_1280_cekmece, taslakust_BASIT_aydinlik_1440, cari_BASIT_aydinlik_1440_liste, cari_PRO_karanlik_375_liste, cari_BASIT_aydinlik_1440_detay, vezne_BASIT_aydinlik_1280, gunsonu_BASIT_karanlik_375. Ölçüm satırları: `tur3/olcum.jsonl`.

## T2.5 Canlı API + SQL (5166)
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar/faturalanabilir-islemler?gun=30&limit=50 | fişsiz satış + alış listede | 200 · 10 işlem, hepsi fisId=null, biri `ALIS` (Anadolu Döküm 3.840) |
| POST /faturalar/fisten {hareketId 01M3V67M8GFA…, KUYUM} | 200 taslak, `islemdenGeldi:true` | 200 TASLAK 01M3V6SDTX6H…, toplam 21.991,14, islemdenGeldi true |
| Aynı gövdeyle ikinci POST (yarış) | 400, boş numara yok | 400 "Bu hareket taslak bir faturaya bağlı (numara kesimde verilir)". Bu yanıt ara sürümden; son sürüm sözleşme sözcüklerini koruyan "zaten taslak bir faturayla faturalandırılmış (numara kesimde verilir)" metnini veriyor, HTTP testinde birebir sınandı. |
| GET /faturalar/{id}/kalemler | `hareketFissiz:true` | 200 · hareketId 01M3V67M8GFA…, hareketFissiz true |
| GET /faturalar?limit=5 | islemdenGeldi true | true |
| SQL | hareket damgalı, fiş yok | `hareketler`: fis_id NULL, fatura_no `TASLAK:01M3V6SDTX6H…`; `faturalar` TASLAK/KUYUM/KALEM 21991.14; `fatura_kalemleri`.hareket_id = 01M3V67M8GFA… |
| POST /islemler/{tahsilat}/storno | 200 (cari detay iptal satırı için) | 200 stornoHareketId 01M3V75CGX…, cariBakiyesi 14.847,11 |

Temizlik: API (dotnet run ve Kuyumcu.Api alt süreci) ile vekil durduruldu; `lsof` 5166 ve 5167 boş. Başsız Chrome süreçleri (9501–9513) kapatıldı. fik.json ve jeton `printf ''` ile boşaltıldı. `zsh kiraci-temizle.sh 01M3V65B4G3ZP4RFWFE0FRG0M5 --uygula`: 75 tablolu taramada satırlar silindi. SQL: tenants, hareketler, faturalar, cari_bakiyeler için bu kiracı 0. **tenants = 13** (başlangıçla aynı).

## T2.6 Ekip kararı: alış işlemleri Fişten Fatura Kes listesinde kalıyor (mevzuat hükmü yazılmadı)
Fişten kesilen belgenin türü fişten türetiliyor (`FistenTur`: bütün kalemler ALIS/HURDA_ALIS ise **ALIŞ faturası**). Yani tedarikçiden yapılan alış için "müşteri faturası" kesilmiyor; satıcının faturası alış faturası olarak deftere bağlanıyor. Bu, tur 1 öncesinden beri sunucu davranışı ve değiştirilmedi. Ekranda yanlış olan "Tek satış" etiketiydi, düzeltildi. Mükellef olmayan kişiden hurda alışında belge gider pusulasıdır (YASAL-4 alanı). Bu işlemlerin bu listede gösterilmesi gerekip gerekmediği **mali müşavire soru** (T2.8). Bu pakette yasal kural sabitlenmedi.

## T2.7 Mutasyon tablosu (tur 2)
Her mutasyon: yedek → perl ile tek eşleşmeli değişiklik → derleme → filtreli test → **ters ikame** → yedekle cmp.

| Kod | Bozulan kural | Kırmızı test | cmp |
|---|---|---|---|
| MT1-18 | BASİT rozet yeniden kutu içinde (grid-row 1) | T2_BASIT_bakiye_rozeti_kutuya_bindirilmez_kendi_satirinda | AYNI |
| MT1-19 | Bicim.Tarih yeniden kültürün "d"si | T2_Liste_tarihi_tek_kalip; TARIH_TEK_tek_kalip_sifirli_gun_dilden_bagimsiz | AYNI |
| MT1-20 | TekKalip "d" eşlemesi kaldırıldı | TARIH_TEK_tek_kalip_sifirli_gun_dilden_bagimsiz | AYNI |
| MT1-21 | KUYUM satırlı notu yerine eski gerekçe (Api apphost kopyası başka şeridin derlemesiyle çakıştı; Web derlendi, kırmızı mutasyona özgü) | T2_Kuyum_taslak_cekmecesinde_degistirilemez_bandi_yok_notr_not | AYNI |
| MT1-22 | jargonlu FT.SatirDuzeltIpucu geri | T2_Taslak_satir_ipucunda_sunucu_jargonu_yok | AYNI |
| MT1-23 | fişsiz kalem bağlantısı yeniden /islemler | T2_Adet_kilidi_gercek_kaynagi_soyler(fissiz: True) | AYNI |
| MT1-24 | sunucu hareketFissiz tersine (FisId != null) | Fissiz_satis_…_faturalanir (satır 845) | AYNI |
| MT1-25 | sunucu liste islemdenGeldi = false | Fissiz_satis_…_faturalanir (satır 840) | AYNI |
| MT1-26 | "Tek alış" eşlemesi bozuldu | T2_Fisten_secici_alis_islemi_tek_alis_rozeti | AYNI |
| MT1-27 | liste kaynak satırı "Manuel"e düşer | T2_Liste_islemden_gelen_belge_kaynak_rozeti | AYNI |
| MT1-28 / 28b | ret metni ham basılır | T2_Fisten_yaris_reddi_sade_cumle (3/3) | AYNI |
| MT1-29 (geçersiz) / 29b | taslak dalı kapatıldı → "zaten  ile" | Fissiz_satis_…_faturalanir (satır 850: Beklenen "…taslak bir faturayla…", Gerçek "Bu hareket zaten  ile faturalandırılmış") | AYNI |
| MT1-30 | gün anahtarı "d.MM.yyyy" ile | T2_Gun_sonu_gecmis_gun_anahtari_tek_kalip; TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok (yeni desen sayaçta tutuyor) | AYNI |
| MT1-31 | form-toplam-satir margin 0 | T2_Vezne_toplam_ust_bosluk_ve_para_son_eki_govde_boyu | AYNI |
| MT1-32 | seçenek alt bilgisi yeniden nowrap | T2_Secici_secenek_alt_bilgi_sarar_rozet_tek_satir | AYNI |

MT1-29'un ilk koşumu **geçersiz**: başka bir şeridin GiderModulu.cs'deki yarım düzenlemesi (CS7094) derlemeyi kırdı, test eski MT1-25 ikilisiyle koştu. Derleme düzelince MT1-29 yeniden koşuldu ve geçerli kırmızı verdi. Sunucu mesajı değişince MT1-28b ve MT1-29b bir kez daha koşuldu. Son mutasyondan sonra kaynakta `MUT*/` izi 0; TEMİZ `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata**.

## T2.8 denetim.sh (11:46, kiracı silindikten sonra)
- `✓ TOPLU-1: TARIH_TEK (ham tarih biçimi/ToLocalTime) 0`. Sayaç artık "d.MM" ve {x:d}/{x:g} desenlerini de sayıyor.
- ✗ P-30 DEMO_INSAN_SURESI: kapsam dışı, görevde "P-30 hariç" deniyordu.
- ✗ Pages onclick'siz/disabled'sız button 72 > 71: **benim değil**. Tur 1 sonunda 71'di. Bu turda Faturalar.razor'daki sayı değişmedi (7 = 7), KasaRaporu 0. Artış 11:39'da değişen SifreSifirla.razor'dan (AUTH-SIFIRLA şeridi) görünüyor.
- ✗ P-08 FATURA_NO_SERBEST_METIN 3 > 2: **benim değil**. Yeni satır `Modules/Hurda/HurdaModulu.cs:259`, `h.FaturaNo == null || h.FaturaNo == ""` (YASAL-4, "§ 4.1 satıcı faturası bekleniyor", 11:03). FaturaModulu değişikliğim bu sayaca girmiyor.
- CARI_BAKIYE_AYNA_UYUSMAZ: ölçüm sırasında benim verdiğim +250 ₺ / +0,5 has farkı yüzünden "YENİ 1" vermişti. Kiracı silindikten sonra ✓.

## T2.9 Tam süit (son kodla, KUYUMCU_TEST_DB=kuyumcu_test_t5)
`trx-pTOPLU-1-t3b/_Ramazan-MacBook-Air_2026-10-01_11_38_46.trx`: **Toplam 8740 · Geçti 8738 · Başarısız 1 · Atlandı 1.**
- Tek kırmızı `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` (Beklenen 0, Gerçek 1). Zamanlamaya bağlı bir bUnit testi. Tek başına 3 kez koşuldu, **3/3 yeşil**. Bir önceki tam koşumda (11:26) da yeşildi. Bu paketin dokunduğu kodla bağı yok: tuval-satis.css bUnit'te yüklenmiyor. Kararsız, sahibi SATIS-3.
- Bir önceki tam koşumda (11:26) 3 kırmızı vardı. İkisi benim taslak mesajı değişikliğimdi (FaturaKalemTestleri.Ayni_fisten_ikinci_kalemli_fatura_400_doner, FaturaFistenTestleri.Ayni_fisten_ikinci_fatura_400_ve_mesaj_fatura_numarasini_tasir). Mesaj "zaten … faturalandırılmış" sözleşme sözcüklerini koruyacak biçimde düzeltildi ve son koşumda yeşil. Üçüncüsü AuthSifirlaEkranTestleri.Jeton_reddedilirse_… (AUTH-SIFIRLA), filtreli koşumda yeşil.
- **Bu paketin ürettiği yeni kırmızı: 0.**

## T2.10 Değişen dosyalar (tur 2)
- Kuyumcu.Web/Servisler/Bicim.cs
- Kuyumcu.Web/Pages/Faturalar.razor (yalnız taslak çekmece toplam notu, satır ipucu, adet kilidi, liste kaynak satırı, işlem seçici rozeti, fisten ret metni; özel matrah bölümüne dokunulmadı)
- Kuyumcu.Web/Pages/KasaRaporu.razor (GunMetni)
- Kuyumcu.Web/wwwroot/css/ortak.css (vezne toplam ve son ek, seçici seçeneği)
- Kuyumcu.Web/wwwroot/css/tuval-satis.css (BASİT rozet satırı)
- Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs (islemdenGeldi, hareketFissiz, taslak mesajı)
- Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs (iki isteğe bağlı alan, varsayılanı eski davranış)
- Kuyumcu.Core/I18n/Sozlukler.Toplu1.cs (10 anahtar, TR ve EN)
- dotnet/denetim.sh (TARIH_TEK iki desen)
- Testler: Toplu1Testleri.cs, BelgeKalanTestleri.cs, Dogrulayici/BELGEKALANSinirTestleri.cs, FaturaEkranTestleri.cs, TM1MusterilerTuvalTestleri.cs

Migration yok: şema değişmedi, iki alan mevcut kolonlardan türetiliyor. Yeni varlık yok. DoD: fişten kesilen taslak mevcut PATCH, iptal ve kalem uçlarıyla düzenleniyor. Adet kilidi sunucu kuralı; ekran sebebini ve doğru yolu (işlem kaydı) gösteriyor.

## T2.11 Açık kalanlar
- DÜŞÜK (ayrı paket): Pages/Bilesenler altında `DateTime.Today/Now` ile tarayıcı yerelinin "bugün"ü (34 kullanım, çoğu diğer şeritlerin dosyalarında). `Bicim.IsletmeBugun` hazır. Sayaca eklenmedi, çünkü eklenince başka şeritlerin dosyaları kırmızıya düşer. Orkestratörün ayrı paketi olmalı.
- DÜŞÜK: /faturalar Vezne birim fiyat kutusunda başsız ölçümde odak kaybı olayı tetiklenmediği için "125000" binliksiz kaldı. Tur 1'de "125.000" ölçülmüştü; bu turda bu alana dokunulmadı.
- denetim.sh'taki başka şeritlerin ✗'leri: SifreSifirla button sayısı (AUTH-SIFIRLA), HurdaModulu FATURA_NO (YASAL-4), P-30 (kapsam dışı).
- Kararsız test SATIS3 barkod (SATIS-3).
- Tur 1'den kalan DÜŞÜK'ler: /denetim-izi K9 ve /islemler BASİT 1280 "Kasa seçin…" sarması.

## T2.12 Kurucu / mali müşavir
- **Mali müşavire:** "Fişten Fatura Kes" listesinde tedarikçiden yapılan ALIŞ işlemleri gösterilsin mi? Bugün bu işlemden ALIŞ faturası kaydı oluşuyor, satıcının faturasının defterdeki karşılığı. Hurda alışında mükellef olmayan satıcı için belge gider pusulası (YASAL-4). Yanıt gelene kadar davranış korunuyor, yalnız etiket doğru: "Tek alış".
- Mevzuat hükmü yazılmadı ya da değiştirilmedi. TARIH-TEK yalnız gösterim kalıbı ve saat dilimiyle ilgili.

---

# TUR 3 — orkestratör kapısı bulguları (01.10.2026 12:45 → 13:50, şerit t5)

Ağaçtaki iş korundu. Başlangıçta yarım mutasyon yoktu; tasarım hakemi 3'ün kiracısı (01M3VBZ2…) temizlenmişti
(`TOPLU-1-tasarim3/temizle.log`). Başlangıç tenants = 13. Geçici dosyalar: `/private/tmp/kuyumcu-serit/TOPLU-1/tur4/`.

## T3.1 Bulgu → çözüm
| Bulgu (orkestratör 12:45) | Çözüm | Dosya |
|---|---|---|
| Secici uzun ad kuralı BİLEŞEN düzeyinde TEK yerde; kutu en çok 2 satır, sonra "…" + title tam ad | `ortak.css` bileşen kuralı: `.secici-kap > .secici-tetik > .secici-metin--tam:not(#_):not(#__)` → `display:-webkit-box; -webkit-line-clamp:2; line-clamp:2; overflow:hidden; overflow-wrap:anywhere; line-height:1.2`. Tetik kuralına `padding-block: 3px` (iki satırlı değer çerçeveye yapışmasın). Tam ad zaten `Secici.razor`'daki span `title`'ında (KesifP7 sözleşmesi); razor'a yalnız açıklama eklendi. **Sayfaya özgü uzun-ad düzeltmesi kaldırıldı:** `ortak.css`'teki `.secici-kap > #islem-cari.secici-tetik { padding-block: 2px }` silindi (yerini bileşen kuralı aldı). Satış'ın `white-space/text-overflow` kuralları ezilmiyor (bildirilmiyor). | ortak.css, Secici.razor (yorum) |
| Rozet kutunun içine binmez | PRO satışta bakiye rozeti ≥901 px'te kutunun İÇİNE bindiriliyordu (`grid-row:1; justify-self:end; z-index:1` + kutuda `padding-right: calc(46% + 36px)`): 86 karakterlik ad 489 px kutuda 4 satıra sarıyor, kutu 83 px oluyordu (hakem 3 ölçümü). Rozet artık kutunun ALTINDA kendi satırında (BASİT tur 2 ve PRO ≤900 ile aynı). `calc(46%+36px)` kuralı kaldırıldı. | tuval-satis.css (yalnız Satış müşteri kutusu/rozet bloğu) |
| Komşu kutuyla K7x ≤ 3 px | Ölçüldü: en büyük alt/üst fark **2 px** (/islemler PRO `#islem-ust-ozet` 44 ↔ müşteri 46; /raporlar/kasa `kr-baslangic/kr-bitis` 44 ↔ `kr-kasa` 46). | — (ölçüm) |
| Satırsız taslakta "değiştirilemez" bandı YOK (yalnız KESİLDİ/İPTAL'de) | Satırsız taslak çekmecesinde `<DegismezIpucu Varlik="FaturaSatirsizTaslak">` ("Bu kayıt değiştirilemez" amber bandı) yerine aynı id ile nötr not `<p class="… fatura-satirsiz-notu" id="degismez-ipucu-FaturaSatirsizTaslak" role="note">` (gerekçe + yol, sözlük metni aynı). Kesilmiş/iptal belgenin `degismez-ipucu-Fatura` ve `degismez-ipucu-FaturaKalemi` bantları DEĞİŞMEDİ (`kd != "TASLAK"` / `!KalemDuzenlenebilir` koşulları). Canlıda satırsız taslak gerçekten düzenleniyor (T3.5). | Faturalar.razor (yalnız taslak çekmece "Satırlar" bölümü) |

## T3.2 Testler (yeni 4 yöntem / 5 durum, Toplu1Testleri.cs) + 1 uyarlanan
- Saf: `T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali` (clamp 2, -webkit-box, vertical, overflow hidden, line-height 1.2, padding-block 3px, `#islem-cari.secici-tetik` sayfa kuralı yok, Secici.razor title), `T3_PRO_bakiye_rozeti_kutuya_bindirilmez` (calc(46%) yok; PRO rozet bloklarında grid-row 1 / justify-self end / z-index yok; grid-row 2 var).
- bUnit: `T3_Satirsiz_taslakta_degistirilemez_bandi_yok_notr_not(STANDART|KUYUM)` — çekmecede "Bu kayıt değiştirilemez" metni ve `.degismez-ipucu` yok; aynı id'li not `<p>`, gerekçe+yol; STANDART'ta Toplam girdisi var. (Mevcut `Q92_satirsiz_taslakta_EKLEME_YOK…` bant varken de geçiyordu — açığı bu test kapattı, MT1-36.)
- **Uyarlanan:** `OrtakGorunumSaflik.O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz` — tam süitte bu paketin ürettiği TEK kırmızıydı (`overflow: hidden` ortak.css'te yasak). Orkestratörün açık "2 satır + …" emri `overflow:hidden` gerektirdiği için test DAR istisnayla uyarlandı: yalnız `-webkit-line-clamp: 2` taşıyan Secici bloğu çıkarılır, istisnanın tam 1 `overflow: hidden` kaldırdığı ve geri kalanda 0 olduğu sınanır. Gerekçe test içinde yazılı. (`overflow: clip` ile testi atlatmak yerine istisna açıkça yazıldı.)
- Filtreli son koşum (Toplu1 + KesifP7 + Q92FaturaKalem + TS1/TS2 + OlcekTek + SATIS3Ekran + KesifP5 + FaturaEkran + OrtakGorunum + Secici): **683/684 geçti, 2 atlandı**; tek kırmızı `SATIS3EkranTestleri.T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ` — tek başına 3/3 yeşil, SATIS3Ekran sınıfı 150/150 yeşil, tam süitte yeşil → zamanlamaya bağlı kararsız bUnit (sahibi SATIS-3); bu turun dosyaları (CSS bUnit'te yüklenmez; Secici.razor'da yalnız yorum) ile bağı yok.

## T3.3 Mutasyon tablosu (tur 3) — yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-1/tur4/`, geri alma TERS İKAME + cmp
| Kod | Bozulan kural | Kırmızı test | cmp |
|---|---|---|---|
| MT1-33 | ortak.css `-webkit-line-clamp: 2; line-clamp: 2` → `none` | Toplu1SafTestleri.T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali | AYNI |
| MT1-34 | ortak.css tetik `padding-block: 3px` kaldırıldı | T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali | AYNI |
| MT1-35 | tuval-satis.css PRO rozet yeniden kutu içinde (`grid-row:1; justify-self:end; z-index:1; max-width:46%`) | Toplu1SafTestleri.T3_PRO_bakiye_rozeti_kutuya_bindirilmez | AYNI |
| MT1-36 | Faturalar.razor satırsız taslakta `DegismezIpucu` geri (derlemeli) | Toplu1FaturaTestleri.T3_Satirsiz_taslakta_degistirilemez_bandi_yok_notr_not (KUYUM + STANDART, 2/2) | AYNI |
| MT1-37 | Secici.razor seçili değer `title`'ı kaldırıldı (derlemeli) | T3_Secici_en_cok_iki_satir_uc_nokta_bilesen_kurali + KesifP7Testleri.Secici_uzun_deger_tam_metin_ve_title | AYNI |

Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata** (`tur4/b-son.log`). Not: Kuyumcu.Tests yeniden derlendiğinde başka şeritlerin dosyalarından 2 uyarı çıkıyor (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(454)` xUnit2029) — bu paketin değil.

## T3.4 Gerçek tarayıcı ölçümü (başsız Chrome, cdp.mjs + sikisma-olc.js; API 5166, vekil 5167, CDP 9500–9519)
Kiracı `PAKETTOPLU-1-TEST Tur4 Kuyumculuk` (01M3VE2A4C5FG0D9AFZX11QJ7Y, demo-tohumla.js --prova-kiraci). Veri: "Tuna Akarsu" PATCH ile
**"Abdurrahman Şahinoğlu Kuyumculuk Altın ve Pırlanta Mücevheratı Ticaret Limited Şirketi" (86 karakter, borcu 21.925,63 ₺)**; ikinci kasa
**"Merkez Şube Altın ve Döviz Ana Kasası Emanet ve Hurda Bölümü Kuyumculuk Ticaret Kasası" (86 karakter)** — tek kasada Hurda/Kasa raporu
seçicisi UX-S22 gereği gizli olduğundan kasa seçicileri bu adla ölçüldü; test banka hesabı (dekont penceresi için); satırsız STANDART taslak fatura.

Kapsam: grep `<Secici` kullanımlarından orkestratörün saydığı ekranlar — /islemler (BASİT+PRO), /faturalar Yeni Fatura + taslak çekmece,
/vezne, /emanet, /banka dekont, /siparisler, /hurda (kasa; müşteri KayitSecici bileşeniyle — Secici değil), /raporlar/kasa süzgeç,
/cariler süzgeç, /raporlar süzgeç. Her ekran 11 bileşim: 1440 BASİT açık/koyu, 1440 PRO açık/koyu, 1280 BASİT açık, 1280 PRO açık,
960 BASİT açık, 960 PRO koyu, 375 BASİT koyu, 375 PRO açık/koyu → **116 ölçüm** (`tur4/secici.jsonl`, görüntüler `tur4/f/`).

Özet: **116/116'da seçili değer ≤ 2 satır** (73 ölçümde 2, 43'ünde 1); taşma olan her durumda "…" + `title` = tam ad (100 %);
K3 0/0 · K7 0 · K10 0 · yatay 0 · K5 0 her ölçümde; komşu kutu alt/üst farkı en çok **2 px**; rozet kutu içinde **0** (†: /banka 1280/960'ta
`bh-gecmis-sayi` arka plandaki sayfa öğesi, dekont penceresinin örtüsü altında — kutuya binen rozet değil, görüntüde doğrulandı).
Kutu yüksekliği: 16 px'lik formlarda 44–46 px, çekmecede (18 px yazı) 51 px — çekmecede müşteri alanı satırında tek (komşusuz).

\* K9: çekmece/pencere AÇIKKEN arka plandaki sayfa öğeleri örtüyle kesişiyor (tur 1–2 ile aynı tasarım gereği; ön plandaki metin binmiyor).

**K6 (açılan listede seçenek > 2 satır) — ölçülmüş gerekçe, açık karar:** 375 px'te 345 px'lik listede 86 karakterlik ad 2–3 satıra +
telefon/alt bilgi satırı → 3–4 satır (12 ekran×genişlik; /emanet'te alt bilgi (telefon) yüzünden 1440'ta da 3). Bu tur kutuyu (seçili değer)
2 satıra bağladı; LİSTEDE tam ad bilerek bırakıldı: dokunmatik ekranda `title` görünmez, seçim yapılan tek yerde adın kesilmesi
benzer adlı iki müşteriyi ayırt etmeyi engeller. Ayrıca ölçer `getClientRects` ile line-clamp'in gizlediği satırları da saydığı için
(deney: 2 satıra kırpılan blokta 3 satır sayıldı, `tur4/deney.*`) listede clamp K6'yı zaten 0'a indirmez; 0 için seçenekte tek satır +
"…" gerekir. Bu önceden de vardı (hakem 3: `#fd-duzenle-cari enCokSatir=4`). Orkestratör/kurucu kararı gerekir (açık kalanlar).

Read ile bakılan görüntüler: islemler_PRO_aydinlik_1440 (2 satır, rozet altta), z_islemler_pro_1440 (yakın: komşuyla aynı alt çizgi),
islemler_BASIT_karanlik_375 (2 satır + "…", rozet altta), fatura_taslak_PRO_aydinlik_375 (2 satır + "…", amber bant YOK, nötr not),
fatura_yeni_BASIT_aydinlik_1440 + z_fatura_yeni_1440 (2 satır, iç boşluklu), banka_BASIT_aydinlik_1280 (dekont penceresi),
rapor_kasa_PRO_aydinlik_1440 (kasa adı 2 satır + "…", tarih kutularıyla hizalı), vezne_PRO_karanlik_960 (koyu, 2 satır),
emanet_dbg_liste (açık listede tam ad 2 satır + telefon), banka_BASIT_aydinlik_1440 (hesapsız boş durum — hesap açılıp yeniden ölçüldü).

### Ölçüm tablosu (rota × genişlik × mod × tema; her satır son koşum)
| Ekran (Secici) | Genişlik · mod · tema | kutu H px | satır | "…" | title=tam ad | komşu alt/üst fark px | rozet kutuda | K3 | K7 | K9 | K10 | K6 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| islemler (#islem-cari) | 1440 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 1440 BASIT karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 1440 PRO aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 1440 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 1280 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 1280 PRO aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| islemler (#islem-cari) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| islemler (#islem-cari) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| islemler (#islem-cari) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| fatura_yeni (#fatura-cari) | 1440 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 5* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 1440 BASIT karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 5* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 1440 PRO aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 1440 PRO karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 1280 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 4* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 1280 PRO aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 4* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 960 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 5* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 960 PRO karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_yeni (#fatura-cari) | 375 BASIT karanlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| fatura_yeni (#fatura-cari) | 375 PRO aydinlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| fatura_yeni (#fatura-cari) | 375 PRO karanlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1440 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1440 BASIT karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1440 PRO aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1440 PRO karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1280 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 3* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 1280 PRO aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 3* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 960 BASIT aydinlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 5* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 960 PRO karanlik | 51 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 5* | 0 | 0 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 375 BASIT karanlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 375 PRO aydinlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| fatura_taslak (#fd-duzenle-cari) | 375 PRO karanlik | 51 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| vezne (#vz-f-cari) | 1440 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 1440 BASIT karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 1440 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 1440 PRO karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 1280 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 1280 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| vezne (#vz-f-cari) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| vezne (#vz-f-cari) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| vezne (#vz-f-cari) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1440 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1440 BASIT karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1440 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1440 PRO karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1280 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 1280 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| emanet (#e-cari) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| banka (#dekont-cari) | 1440 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 2* | 0 | 0 | 0 |
| banka (#dekont-cari) | 1440 BASIT karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 2* | 0 | 0 | 0 |
| banka (#dekont-cari) | 1440 PRO aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 2* | 0 | 0 | 0 |
| banka (#dekont-cari) | 1440 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 2* | 0 | 0 | 0 |
| banka (#dekont-cari) | 1280 BASIT aydinlik | 46 | 2 | hayır | evet | — | 1† | 0/0 | 0 | 5* | 0 | 0 | 0 |
| banka (#dekont-cari) | 1280 PRO aydinlik | 46 | 2 | hayır | evet | — | 1† | 0/0 | 0 | 3* | 0 | 0 | 0 |
| banka (#dekont-cari) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 1† | 0/0 | 0 | 4* | 0 | 0 | 0 |
| banka (#dekont-cari) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 |
| banka (#dekont-cari) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| banka (#dekont-cari) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| banka (#dekont-cari) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 6* | 0 | 1 | 0 |
| siparisler (#sip-f-cari) | 1440 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 1440 BASIT karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 1440 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 1440 PRO karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 1280 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 1280 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| siparisler (#sip-f-cari) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| siparisler (#sip-f-cari) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| siparisler (#sip-f-cari) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| hurda (#hrd-kasa) | 1440 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 1440 BASIT karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 1440 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 1440 PRO karanlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 1280 BASIT aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 1280 PRO aydinlik | 44 | 1 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| hurda (#hrd-kasa) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| hurda (#hrd-kasa) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| hurda (#hrd-kasa) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| rapor_kasa (#kr-kasa) | 1440 BASIT aydinlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 1440 BASIT karanlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 1440 PRO aydinlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 1440 PRO karanlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 1280 BASIT aydinlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 1280 PRO aydinlik | 46 | 2 | evet | evet | 2/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 960 BASIT aydinlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 960 PRO karanlik | 46 | 2 | hayır | evet | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| rapor_kasa (#kr-kasa) | 375 BASIT karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| rapor_kasa (#kr-kasa) | 375 PRO aydinlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| rapor_kasa (#kr-kasa) | 375 PRO karanlik | 46 | 2 | evet | evet | — | 0 | 0/0 | 0 | 0 | 0 | 1 | 0 |
| cariler (#cari-tur-filtre) | 1440 BASIT aydinlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 1440 BASIT karanlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 1440 PRO aydinlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 1440 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 1280 BASIT aydinlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 1280 PRO aydinlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 960 BASIT aydinlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 960 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 375 BASIT karanlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 375 PRO aydinlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| cariler (#cari-tur-filtre) | 375 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 1440 PRO aydinlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 1440 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 960 BASIT aydinlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 960 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 375 PRO aydinlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| raporlar (#rp-yil) | 375 PRO karanlik | 44 | 1 | hayır | — (kısa değer) | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |

## T3.5 Canlı API + SQL (5166)
| Adım | Beklenen | Gerçek |
|---|---|---|
| PATCH /cari/01M3VE320RPA… {ad: 86 karakter} | 200, ad tam | 200, `ad` 86 karakter birebir |
| POST /faturalar {cariId, SATIS, toplam 1.234.567,89, STANDART, 0,2} | 200 TASLAK, satırsız | 200 TASLAK 01M3VE4PXX82…, faturaNo null |
| PATCH /faturalar/01M3VE4PXX82… {toplam: 1250000} (satırsız taslak = düzenlenebilir kayıt) | 200, yeni toplam | 200 TASLAK · kalemKaynagi BASLIK · toplam 1.250.000 · kdvTutar 208.333,33 |
| SQL `faturalar` | aynı | `TASLAK BASLIK 1250000.00 208333.33`; `fatura_kalemleri` bu fatura için 0 satır |
| POST /bankalar (test IBAN), POST /kasalar (86 karakterlik ad) | 200 | 200 / 200 (yalnız ölçüm verisi) |
Sonuç: satırsız taslak sunucuda gerçekten düzenleniyor → eski "Bu kayıt değiştirilemez" bandı yanlıştı; yeni nötr not doğru.

Temizlik: vekil ve API (`dotnet run` + alt süreç) durduruldu, `lsof` 5166/5167 boş; kendi başsız Chrome süreçleri 0. fik.json ve jeton `printf ''`
ile boşaltıldı. `zsh kiraci-temizle.sh 01M3VE2A4C5FG0D9AFZX11QJ7Y --uygula` (75 tablo) → SQL: tenants/faturalar/hareketler/cariler bu kiracı için 0;
**tenants = 13** (başlangıçla aynı). QA-TEST-* ve diğer şeritlerin kiracılarına dokunulmadı.

## T3.6 denetim.sh (13:45, kiracı silindikten sonra, `bash denetim.sh` → `tur4/denetim.txt`)
- `✓ TOPLU-1: TARIH_TEK (ham tarih biçimi/ToLocalTime) 0` · `✓ Pages: ModalPencere kullanımı 48 (tavan 55)` (bu tur yeni ModalPencere yok).
- ✗ P-30 DEMO_INSAN_SURESI: kapsam dışı ("P-30 hariç").
- ✗ P-23 ODEME_KALEMSIZ_YOL 3: **benim değil** — 3 satırın hepsi `PAKETYASAL4-TEST Kuyumculuk …` kiracısında (01M3VGPYSM6M…, 10:39 UTC), YASAL-4 şeridinin canlı verisi. Benim kiracım ölçümden önce silinmişti.
- Tur 2'deki başka şerit ✗'leri (button 72>71, P-08 FATURA_NO) bu koşumda ✓.

## T3.7 Tam süit (son kodla, KUYUMCU_TEST_DB=kuyumcu_test_t5)
`/private/tmp/kuyumcu-serit/trx-pTOPLU-1-t4/_Ramazan-MacBook-Air_2026-10-01_13_40_04.trx`: **Geçti 8791 · Başarısız 3 · Atlandı 2.**
- `OrtakGorunumSaflik.O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz` — **bu paketin** (line-clamp için `overflow:hidden`). Dar istisnayla uyarlandı (T3.2); filtreli koşumda yeşil (OrtakGorunum 118 testlik koşumda O7 geçti).
- `Dogrulayici.GOC47BSinirTestleri.Turkce_virgul_1_5_has_ON_KAT_okunmaz`, `…Turkce_virgul_1500_50_TL_YUZ_KAT_okunmaz` (Beklenen 1,5 / Gerçek 15) — **GOC-47B şeridi** (yarım iş; birkaç dakika sonra filtreli koşumda bu ikisi yeşil, yerine `Goc47BSayiTestleri.Has_tavani_DB_kolonuna_sigar_…` kırmızı — şerit aktif değiştiriyor). Bu paketin dosyalarıyla bağı yok (sayı ayrıştırma).
- **Bu paketin ürettiği ve açık kalan yeni kırmızı: 0** (tek kırmızısı O7 düzeltildi). Tur başına tek tam süit kuralı nedeniyle O7 uyarlamasından sonra tam süit yeniden koşulmadı; uyarlama yalnız O7 test gövdesini değiştirdi ve filtreli koşumlar yeşil.

## T3.8 Değişen dosyalar (tur 3)
- `dotnet/Kuyumcu.Web/wwwroot/css/ortak.css` — yalnız Secici kuralı (clamp bloğu + tetik `padding-block: 3px`; `#islem-cari` sayfa kuralı silindi)
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` — yalnız PRO müşteri rozeti bloğu (b) + başlık yorumu
- `dotnet/Kuyumcu.Web/Bilesenler/Secici.razor` — yalnız açıklama yorumu (işaretleme ve id'ler aynı)
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor` — yalnız taslak çekmece "Satırlar" bölümündeki satırsız not (id aynı; özel matrah bölümüne dokunulmadı)
- `dotnet/Kuyumcu.Tests/Toplu1Testleri.cs` (3 yeni yöntem), `dotnet/Kuyumcu.Tests/OrtakGorunumTestleri.cs` (O7 dar istisna)
Migration yok (şema değişmedi). Sözlük değişmedi (mevcut `FT.SatirsizGerekce/SatirsizYol` TR+EN). Yeni varlık yok → DoD sözleşmesi: satırsız taslak mevcut PATCH /faturalar/{id} ile düzenlenir (canlıda 200), iptal ucu aynı; kesilmiş/iptal fatura DegismezIpucu ile salt okunur kalır.
Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Faturalar.razor tur 1–2'de de bu paketin alanıydı (taslak çekmece) — aynı dalga, aynı bölge; Secici.razor yorum.

## T3.9 Koşul karşılama (tur 3)
| Koşul | Durum | Kanıt |
|---|---|---|
| Uzun ad kuralı bileşen düzeyinde TEK yerde, sayfaya özgü düzeltme yok | KARŞILANDI | ortak.css tek blok; `#islem-cari` kuralı silindi; T3_Secici testi; MT1-33/34 |
| Tüm Secici kullanımları 86 karakterle 1440/1280/960/375 BASİT+PRO ölçüldü | KARŞILANDI | 11 ekran × 11 bileşim = 116 ölçüm, tablo T3.4 |
| Kutu en çok 2 satır, sonra "…" + title tam ad | KARŞILANDI | 116/116 satır ≤ 2; kesik olan her durumda title = tam ad; MT1-37 |
| Rozet kutunun içine binmez | KARŞILANDI | rozetIcinde 0 (†arka plan); T3_PRO testi; MT1-35; görüntü |
| Komşu kutuyla K7x ≤ 3 px | KARŞILANDI | en çok 2 px; K7 0 her ölçümde |
| Satırsız taslakta değiştirilemez bandı yok (yalnız KESİLDİ/İPTAL) | KARŞILANDI | T3_Satirsiz (2 durum), MT1-36, canlı PATCH 200, görüntü |
| build 0/0 · denetim (P-30 hariç) · tam süitte bu paketten yeni kırmızı 0 | KARŞILANDI (paket kapısı) | b-son.log 0/0; TOPLU-1 sayaçları ✓, ✗'ler başka şerit/kapsam dışı; O7 düzeltildi |
| Test kiracısı silindi, tenants başlangıç değerinde | KARŞILANDI | tenants 13 = 13 |

## T3.10 Açık kalanlar
- **K6 açılan liste (orkestratör/kurucu kararı):** listede tam ad 3–4 satır (375) — gerekçe T3.4. Tercih "listede de tek satır + …" ise tek CSS satırı (`.secici-secenek .secici-ad` ilk metin nowrap+ellipsis) ve sikisma-olc'nin K6 sayımı değişmeden 0 olur; dokunmatikte tam ad görünmez olur.
- Başka şeritlerin kırmızıları: GOC47B (GOC-47B şeridi, sayı ayrıştırma), SATIS3 T13 kararsız (SATIS-3), denetim P-23 (YASAL-4 kiracısı verisi), P-30 (kapsam dışı).
- Tur 1–2'den kalan DÜŞÜK'ler (değişmedi): `DateTime.Today/Now` ayrı paket, /denetim-izi K9, /islemler BASİT 1280 "Kasa seçin…" sarması.

## T3.11 Kurucu / mali müşavir
Bu turda mevzuat hükmü yazılmadı/değiştirilmedi. T2.12'deki mali müşavir sorusu (Fişten Fatura Kes listesinde ALIŞ işlemleri) açık.
