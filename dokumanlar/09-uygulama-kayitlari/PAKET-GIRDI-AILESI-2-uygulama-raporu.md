# PAKET GIRDI-AILESI-2 — uygulama raporu (08.10.2026)

> Anlamsal girdi bileşen ailesi + tek doğrulayıcı (Core) + sunucu öznitelikleri + cırcır sayaçlar + fikstür düzeltmesi.
> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t9 · canlı API 5188 · vekil 5189 · CDP 9981–9984 · `KUYUMCU_TEST_DB=kuyumcu_test_t9`.
> Kaynak (tek doğruluk): `dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md` (B) K1–K34, (E) P1, (F) F2/F3, (D).
> COMMIT YOK. Dosya silinmedi. 5199/5200 portlarına dokunulmadı.

## 00. TUR 3+4 — doğrulayıcı tur 2 bulgularının kapatılması (08.10, 06:40–12:45)

> Tur 3 (06:40–07:06) kodu yazdı, filtreli testleri yeşile çekti; canlı tarayıcı ölçümü betik hatasıyla yarıda kaldı (cari adımında `null.scrollIntoView`) ve oturum kesildi. Tur 4 (bu bölüm, 11:25–12:45) tur 3 kodunu koda karşı doğruladı, gerçek Chrome'da yeni bir üst üste binme kusuru buldu ve düzeltti (§00.1 md.3b), mutasyon/tam süit/denetim/canlı/temizlik adımlarını son kodla baştan koştu. Bu bölümdeki sayılar §0 ve §1–§12'nin ÜSTÜNDEDİR.

### 00.1 Bulgu → yapılan
| # | Bulgu (önem) | Yapılan | Kanıt |
|---|---|---|---|
| 1 | KRİTİK — /isletme telefon: ekran geçersiz diyor ama Kaydet açık, sunucu 200 kaydediyor | Telefon kuralı SAYFADAN kalktı: `Isletme.razor` `KimlikTelefonHatali` / `TelefonHatali` artık `GecersizAlanKapisi.OnekleVar("ikimlik-telefon"/"s-telefon")` — ortak `<Telefon>` Core `TelefonDogrulayici`'yı uygular ve kapıya alan altı METNİYLE bildirir; kilit sebebi `girdiKapisi.Sebep(...)`. Sunucu: `TenantKimlikGuncelleDto.Telefon`, şube oluştur/güncelle DTO'ları ve cari oluştur/güncelle DTO'larına `[Telefon]`; `AdminServisi.KimlikGuncelle` servis yolunda aynı Core kuralı + aynı sözlük metni (alan `telefon`). `Iletisim.TelefonGecerli` (Core/Sozlesmeler.cs) artık `TelefonDogrulayici.Denetle(t) is null` — ikinci kural kalmadı. | bUnit `T3_Isletme_kimlik_telefon_gecersiz_Kaydet_kilitli_kilit_metni_alan_alti_ile_ayni` (3), `T3_Isletme_sube_telefon_aralik_disi_…`; HTTP `T3_Isletme_kimlik_PATCH_gecersiz_telefon_400_ekranla_ayni_metin` (3), `T3_Sube_POST_ve_PATCH_…`, `T3_Cari_POST_aralik_disi_telefon_400_…`; servis yolu `T4_Isletme_kimlik_servis_yolu_gecersiz_telefon_reddeder_telefon_DEGISMEZ` (3) + `…gecerli_telefon_yazilir`. Canlı §00.3; Chrome §00.4 TA/TB/TC/TD 48/48. |
| 2 | KRİTİK — kilit metni alan altından farklı ve yanlış ("yalnız rakam, boşluk ve + ( ) -") | Kilit metni kapıdan = bileşenin `GirdiHatasi` metni (IBAN'daki gibi). | Chrome: '5321234' → alan altı "Telefon alan koduyla 10 haneli olmalı (şu an 7)." ve "Kaydet kilitli: Telefon alan koduyla 10 haneli olmalı (şu an 7)." — 12/12 yapılandırmada `kilitAyni=true`; kilit rengi `--metin-soluk` 12/12. |
| 3 | KRİTİK — yan yana Tarih'te yıl kesiliyor ("08.10.202") | (a) Tur 3: `girdi.css` `.alan-girdi-satir > .girdi.tarih-girdi { min-width: calc(10ch + 1.5rem + 2px) }`. **(b) Tur 4 — gerçek Chrome'da YENİ kusur:** kutu artık daralmıyordu ama süzgeç yuvasındaki sarmalayıcı `.alan` (`flex: 0 1 var(--arac-alan-genislik)` = 9,5rem) kutu + takvim simgesinden dar kaldığı için satır taştı ve **Bitiş kutusu Başlangıç'ın takvim simgesinin altına girdi** (1440 BASIT, görüntü `canli4/DA-islem-defteri__BASIT_aydinlik_1440.png`; `sikisma-olc` K9 bunu yakalamadı = 0). Düzeltme `girdi.css`: `.alan:has(> .alan-kontrol .tarih-girdi) { min-width: min-content; }` (etiket sözcükten kırılabilir, kutu + simge kırılmaz; süzgeçte de form ızgarasında da). Ölçere kendi "binen" denetimi eklendi (tarih kutusu ↔ görünür buton/girdi/takvim dikdörtgen kesişimi). | `T3_Tarih_kutusu_en_kucuk_genislik_10_karakter_artı_dolgu` (iki kural); Chrome §00.4: İşlem defteri 12/12 `kesik=0 binen=0`; 7 süzgeç rotasında 56 ölçüm / 112 tarih kutusu `kesik=0 binen=0` (en dar kutu 1280'de 202 px). |
| 4 | YÜKSEK — Para 2'den fazla ondalığı kabul ediyor, sunucu yuvarlıyor | `Para.razor` `KuralDenetle`: `Tur` verilmese de (ve çağıran `EnCokOndalik` vermediyse) Core `SayiKurali.Ondalik(d, 2)` → alan altı "Virgülden sonra en çok 2 hane yazılır." + kapı kilidi (aynı metin), çağırana null. Sunucu `BankaServisi.HareketEkle`: 2'den fazla ondalık → 400 `alan:tutar`, aynı metin (yuvarlama YOK). | bUnit `T3_Para_Tur_verilmese_de_2_ondalik_fazlasi_hata_ve_kilit`, `T3_Para_EnCokOndalik_veren_cagiranin_siniri_korunur`; HTTP `T3_Banka_hareketi_2den_fazla_ondalik_400_yuvarlanmaz`; canlı 1234.5678/1234.567/0.005 → 400, SQL'de satır yok (§00.3); Chrome PA 12/12. |
| 5 | YÜKSEK — bileşenler alan altına varsayılan ipucu satırı ekliyor | `KuralliMetinAlani.IpucuMetni => Ipucu` (varsayılan YOK); Telefon/IBAN varsayılan ipucu metinleri kaldırıldı. İpucu yalnız çağıran açıkça verirse. | `T3_Telefon_ve_Iban_varsayilan_ipucu_satiri_EKLEMEZ`, `GirdiAilesiBilesenTestleri.G12_…` (güncellendi); Chrome: `#…-ipucu` 12/12 TA/TB/TC + IA + CA'da yok (null). |
| 6 | ORTA — "Tel: +90 (532) …" yapıştırmada haneler kayboluyor | `Telefon.Maskele` + `girdi-maske.js` `telefon`: ilk rakamdan ÖNCE '+' varsa uluslararası yazım. | `T3_Telefon_maske_00_onek_ve_yapistirma_oneki`, `T3_Telefon_yapistirma_arti90_onekli_haneler_kaybolmaz`; node paritesi 13/13 AYNI (`parite-t3.mjs`); Chrome TE 12/12 → "0532 123 45 67", hata yok, Kaydet açık. |
| 7 | ORTA — eksik tarih ('31.12') alandan çıkınca hata yok | `Tarih.razor` / `TarihAralik.razor` `OnParametersSet`: çağıranın değeri DEĞİŞMEDİYSE kullanıcının metni ve hatası korunur (rapor süzgeçleri null'u yok sayıp eski değeri geri verince hata siliniyordu). | `T3_Tarih_eksik_tarih_alandan_cikinca_hata_…`, `T3_TarihAralik_eksik_baslangic_cikista_hata_…`; Chrome DB 12/12 → "Tarihi gün.ay.yıl olarak yazın (ör. 08.10.2026)." |
| 8 | ORTA — '00' önekli yabancı numara 11 haneye sessizce kırpılıyor; '0090' ile TR numarası girilemiyor | **Kural:** '00' = uluslararası önek ('+' ile aynı; ülke kodu 0 ile başlamaz). '0090' + 10 hane = TR numarası (TR kuralı uygulanır, "0532 411 22 33" olarak saklanır); '00' + başka ülke kodu → E.164 ≤ 15 hane, kırpılmaz, ülke kodlu kabul. C# ve JS aynı değişiklik. | `T3_Telefon_0090_yazimi_gecerli_TR_numarasi_olarak_kaydedilir`, `T3_Telefon_00_yabanci_numara_kirpilmadan_gider_hata_yok`, `TM1SinirTestleri.DTM1_…("00905324112233")` (güncellendi); canlı cari POST 0044 → 201 `00442079460958`; Chrome TF/TG 12/12. |
| 9 | DÜŞÜK — yabancı IBAN ipucu/sunucu mesajı yanıltıcı | Varsayılan ipucu kalktı (md.5); Core `IbanDogrulayici`: TR dışı ülke kodunda yeni kod `IbanYabanciBicim` (TR + EN): "Yabancı IBAN ülke kodu, 2 kontrol hanesi ve en az 10 harf ya da rakamdan oluşur (şu an n karakter)." | `T3_Yabanci_IBAN_eksik_ayri_kod_TR_ile_baslar_demez`, HTTP `T3_Banka_yabanci_eksik_IBAN_400_TR_ile_baslar_demez`; canlı 400 aynı metin. |

### 00.2 Tur 3+4 sapmaları / sözleşme değişiklikleri (gerekçeli)
1. **Başka şeritlerin testleri yeni sözleşmeye güncellendi** (davranış değişikliği doğrulayıcının açık talebi; hiçbiri gevşetilmedi, her birinde gerekçe satırı): `ORTAK1SinirTestleri.K4S_…` (0,005 artık geçersiz; yeni `K4S_kurus_alti_0_005_kapiya_yazilir_yuvarlanmaz_null_gider`), `SATIS3SinirTestleri.T17S_kurus_0_005_girdisi_yuvarlanmaz_kapi_kilitler_null_gider`, `SonKalanlarTestleri.SK12_…` (banka bacağı 0,005/0,0049 → 400; kasa ve tahsilat bacakları ESKİ TEK KURAL'da kaldı — K9 göçü GOC'nin), `TM1SinirTestleri.DTM1_…` (0090), `UX1C4EkranTestleri.C4_15_…` (şube telefonu ortak bileşenden), `GirdiAilesiBilesenTestleri.G12_…` (ipucu yok). **Tur 4'te eklenen iki güncelleme:** `Q105SinirTestleri.C_Vezne_kurus_alti_ve_4_hane_istemcide_yuvarlanmadan_gider` (vezne birim fiyatı "0,005" artık alan altında hata, POST YOK — eski hâli 0,005'in gidip sunucuda 0,01'e yuvarlanmasını kilitliyordu; ad korundu çünkü "istemci yuvarlamaz" kuralı aynen geçerli) ve `MASAK370SinirHttpTestleri.H1_…` (199.999,994 / ,995 artık 400 ondalık, satır yok; MASAK eşiği 199.999,99 → 200 / 200.000,00 → MASAK 400 ile AYNEN ölçülür).
2. **Kasa hareketi ve tahsilat uçları** hâlâ 0,005'i 0,01'e yuvarlıyor (SK12'de belgelendi). Bu paket yalnız örnek ekranın ucu (banka hareketi) için 400'e geçti; tüm tutar uçlarının K9'a inmesi GOC paketlerinin işidir (görev sınırı: ekran/uç göçü yok).
3. Cari DTO'larına da `[Telefon]` eklendi ve `Iletisim.TelefonGecerli` Core kuralına bağlandı: bulgunun "ekran ve sunucu TEK kural" kabulü, cari ekranı da aynı `<Telefon>` bileşenini kullandığı için ikinci kuralın kalmasına izin vermiyordu. Eski 10–13 hane kuralının kabul ettiği ama TR numara planında olmayan numaralar (ör. 0500…) artık 400.
4. **Mutasyon betiği zaman aşımı:** ilk koşum (`mutlar-t4.json`) arka plan zaman sınırına (600 sn) M3-13 sırasında takıldı ve süreç öldürüldü; `BankaModulu.cs` mutasyonlu kaldı. **Hemen** yedekten geri kopyalandı (`cmp` AYNI, `if (false &&` sayısı 0), diğer 10 hedef dosyada mutasyon artığı arandı (yok). M3-7/12/13/14 ikinci koşumda (`mutlar-t4b.json`, uzun süre) yeniden koşuldu. M3-12'nin ilk denemesi başka şeridin eşzamanlı WebAssembly paketlemesi yüzünden DERLEME HATASI vermişti — ikinci koşumda temiz.

### 00.3 Canlı API + SQL (API 5188, kiracı `PAKETGIRDI-AILESI-2-TEST T3`, `canli-api3.py` + `canli-api4b.py`, çıktı `canli-api4.out`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| PATCH /admin/tenant/kimlik telefon=0000000000 | 400, ekranla aynı metin | 400 `{telefon:["Telefon alan koduyla 10 haneli olmalı (şu an 9)."]}` |
| … telefon=05001112233 | 400 | 400 "Bu numara Türkiye'de kullanılan bir telefon numarası değil." |
| … telefon=5321234 | 400 | 400 "… (şu an 7)." |
| … telefon="0212 555 01 01" | 200 | 200 `telefon: 0212 555 01 01` |
| POST /subeler telefon=05001112233 | 400 | 400 aynı metin |
| PATCH /subeler/{id} telefon=0000000000 / "0532 411 22 33" | 400 / 200 | 400 / 200 `0532 411 22 33` |
| POST /cari telefon=05001112233 / 00442079460958 / 05324112233 | 400 / 201 / 201 | 400 / 201 `00442079460958` / 201 `05324112233` |
| POST /bankalar/{id}/hareket tutar 1234.5678 · 1234.567 · 0.005 | 400 (yuvarlanmaz) | 400 × 3 `{tutar:["Virgülden sonra en çok 2 hane yazılır."]}` |
| … tutar 1234.57 | 200 | 200 |
| POST /bankalar iban "GB82 WEST 1234" | 400, yabancı metin | 400 "Yabancı IBAN ülke kodu, 2 kontrol hanesi ve en az 10 harf ya da rakamdan oluşur (şu an 12 karakter)." |

SQL: `banka_hareketleri` (hesap 01M4CTVBNTN44771MJZPF00ACX) → yalnız `1234.57 | NULL` (tur 3) ve `1234.57 | T4 1234.57`; 1234.5678/1234.567/0.005 için SATIR YOK (yuvarlanıp yazılmadı). `tenants.telefon = 0212 555 01 01`; `subeler.telefon = 0532 411 22 33`; `cariler`: `T3 Musteri B | 00442079460958`, `T3 Musteri C | 05324112233` (05001112233'lü cari yok).

### 00.4 Gerçek tarayıcı — başsız Chrome (CDP 9986, vekil 5189), 3 genişlik (1440 · 1280 · 375) × BASİT/PRO × açık/koyu = 12 yapılandırma, 236 ölçüm satırı, 108 ekran görüntüsü (`/private/tmp/kuyumcu-serit/GIRDI-AILESI-2/canli6/`, betik `canli6.mjs`, ham `canli6/sonuc.jsonl`, özet `canli6-ozet.txt`)
| Senaryo (rota) | 1440 | 1280 | 375 | Ölçüt |
|---|---|---|---|---|
| TA '0000000000' (/isletme kimlik) | 4/4 | 4/4 | 4/4 | alan altı hata · Kaydet kilitli · kilit metni = alan altı · ipucu yok · kilit soluk ton |
| TB '5321234' | 4/4 | 4/4 | 4/4 | aynı |
| TC '05001112233' | 4/4 | 4/4 | 4/4 | aynı |
| TD yazıp alandan çıkmadan gerçek fare ile Kaydet | 4/4 | 4/4 | 4/4 | çekmece açık kalır, Kaydet kilitli, istek yok |
| TE yapıştır "Tel: +90 (532) 123 45 67" | 4/4 | 4/4 | 4/4 | "0532 123 45 67", hata yok |
| TF '00905324112233' | 4/4 | 4/4 | 4/4 | "0532 411 22 33", hata yok |
| TG '00442079460958' | 4/4 | 4/4 | 4/4 | kırpılmaz, hata yok |
| DA İşlem defteri yan yana tarih | 4/4 | 4/4 | 4/4 | kesik 0 · binen 0 · yatay kaydırma 0 · K1/K3/K5/K9 = 0 |
| DB '3112' → çık | 4/4 | 4/4 | 4/4 | '31.12' + "Tarihi gün.ay.yıl olarak yazın (ör. 08.10.2026)." |
| DC tarih kullanan süzgeçler (/denetim-izi, /raporlar/kasa, /evraklar/takvim, /evraklar, /pos/tahsilatlar, /gider-pusulalari) | 24/24 | 21/21 | 11/11 | 112 tarih kutusu: kesik 0, binen 0 (kapalı "Süzgeç/Geçmiş kayıtlar" akordeonları açılarak) |
| PA dekont '1234,5678' (/banka) | 4/4 | 4/4 | 4/4 | alan altı "Virgülden sonra en çok 2 hane yazılır." · Kaydet kilitli · sebep aynı · soluk |
| PB dekont '1234,56' | 4/4 | 4/4 | 4/4 | "1.234,56", hata yok, Kaydet açık |
| IA IBAN 'GB82 WEST 1234' | 4/4 | 4/4 | 4/4 | ipucu satırı yok · alan altı hata · Kaydet kilitli |
| CA /cariler telefon '00442079460958' | 3/4 | 3/4 | 3/4 | kırpılmaz, hata yok, ipucu yok |

- **CA 3 eksik ölçüm** (BASİT açık 1280, PRO açık 375, PRO koyu 1440): betik "+ Yeni müşteri"ye sayfa olay bağlamadan tıkladı, çekmece açılmadı (görüntü `CA-hata__BASIT_aydinlik_1280.png`: liste ekranı, hata yok). Aynı senaryo kalan 9 yapılandırmada geçti — ölçüm betiği zamanlaması, ürün kusuru değil.
- **/kur ve /fiyatlandirma**: tarih kutuları ayrı eylemle açılan bölümlerde (kur geçmişi, kampanya formu) — bu turda ölçülmedi (n=0, açık kalan).
- **sikisma-olc K9 = 4–6** yalnız çekmece/modal açıkken: örnekler (`k9.mjs`) `h1 "İşletme ve Şubeler" ⟂ span.alan-etiket-metin "Adres"`, `p.stat-deger "2.469,14 ₺" ⟂ #modal… "Dekont Gir"` — ARKA PLAN sayfa öğesi ile üstteki çekmece/modal arasında (tasarım gereği örtü); çekmece/modal içinde binme yok (görüntüler `TA-*`, `PA-*`). Süzgeç ekranlarında K9 = 0. Yatay kaydırma tüm ölçümlerde 0; K1 (gizli görünen) 0; K3 taşan/kırık 0; K5 'TRY' 0.
- Görüntüler Read ile görüldü: `DA-islem-defteri__BASIT_aydinlik_1440.png` (tur 4 düzeltmesi sonrası, iki kutu + iki takvim simgesi ayrık; düzeltme öncesi `canli4/` aynı ad: Bitiş kutusu simgenin altında), `TB-5321234__BASIT_aydinlik_375.png`, `TA-0000000000__BASIT_aydinlik_1280.png`, `PA-dekont-ondalik__BASIT_karanlik_375.png`, `DB-eksik-tarih__BASIT_karanlik_375.png`, `DC_raporlar_kasa__BASIT_aydinlik_1280.png`, `DC_denetim-izi__PRO_karanlik_1440.png`, `CA-hata__BASIT_aydinlik_1280.png`.

### 00.5 Mutasyon (tur 3+4; yedek `/private/tmp/kuyumcu-serit/mut-yedek/GIRDI-AILESI-2`, betik `mut2.py` — tek eşleşme → derle → filtreli test → geri kopya + utime → cmp)
| Kod | Dosya / değişiklik | Kırmızı test (ADIYLA) | cmp |
|---|---|---|---|
| M3-1 | Para.razor — varsayılan 2 ondalık kuralı → null | `GirdiAilesi2Tur3Testleri.T3_Para_Tur_verilmese_de_2_ondalik_fazlasi_hata_ve_kilit` | AYNI |
| M3-2 | Telefon.razor — '00' uluslararası dalı → `if (false)` | `T3_Telefon_0090_yazimi_gecerli_TR_numarasi_olarak_kaydedilir`, `T3_Telefon_00_yabanci_numara_kirpilmadan_gider_hata_yok`, `T3_Telefon_maske_00_onek_ve_yapistirma_oneki` | AYNI |
| M3-3 | Telefon.razor — '+' yalnız baştaysa (yapıştırma öneki) | `T3_Telefon_maske_00_onek_ve_yapistirma_oneki` | AYNI |
| M3-4 | Isletme.razor — `KimlikTelefonHatali => false` | `GirdiAilesi2Tur3IsletmeEkranTestleri.T3_Isletme_kimlik_telefon_gecersiz_Kaydet_kilitli_kilit_metni_alan_alti_ile_ayni` | AYNI |
| M3-5 | Isletme.razor — `TelefonHatali => false` (şube) | `T3_Isletme_sube_telefon_aralik_disi_Kaydet_kilitli_kilit_metni_alan_alti_ile_ayni` | AYNI |
| M3-6 | GirdiOznitelikleri.cs — `[Telefon]` → null | `GirdiAilesi2Tur3SunucuTestleri.T3_Cari_POST_aralik_disi_telefon_400_…`, `T3_Sube_POST_ve_PATCH_gecersiz_telefon_400_…` | AYNI |
| M3-7 | AdminModulu.cs — servis yolu telefon denetimi → `if (false && …)` | ilk koşumda **0 kırmızı (TEST AÇIĞI**: HTTP testi özniteliğe takılıyordu) → yeni `GirdiAilesi2Tur4ServisTestleri.T4_Isletme_kimlik_servis_yolu_gecersiz_telefon_reddeder_telefon_DEGISMEZ` eklendi → tekrar: **kırmızı** | AYNI |
| M3-8 | Sozlesmeler.cs — `Iletisim.TelefonGecerli` → `return true` | `T3_Iletisim_TelefonGecerli_Core_TelefonDogrulayici_ile_AYNI` | AYNI |
| M3-9 | Tarih.razor — "çağıran değişmediyse kullanıcı metni korunur" → `if (false)` | `T3_Tarih_eksik_tarih_alandan_cikinca_hata_cagiran_eski_degeri_geri_verse_de` | AYNI |
| M3-10 | girdi.css — tarih kutusu `min-width` → 0 | `T3_Tarih_kutusu_en_kucuk_genislik_10_karakter_artı_dolgu` | AYNI |
| M3-14 | girdi.css — tur 4 sarmalayıcı `min-width: min-content` → 0 | `T3_Tarih_kutusu_en_kucuk_genislik_10_karakter_artı_dolgu` | AYNI |
| M3-11 | IbanDogrulayici.cs — `IbanYabanciBicim` → `IbanBicim` | `T3_Yabanci_IBAN_eksik_ayri_kod_TR_ile_baslar_demez`, `T3_Banka_yabanci_eksik_IBAN_400_TR_ile_baslar_demez` | AYNI |
| M3-12 | KuralliMetinAlani.cs — `IpucuMetni => Ipucu ?? "ipucu"` | `T3_Telefon_ve_Iban_varsayilan_ipucu_satiri_EKLEMEZ` | AYNI |
| M3-13 | BankaModulu.cs — hareket 2 ondalık denetimi → `if (false && …)` | `T3_Banka_hareketi_2den_fazla_ondalik_400_yuvarlanmaz` | AYNI (zaman aşımı sonrası elle geri kopya, §00.2 md.4) |
| MJ3-1 | girdi-maske.js — '00' dalı → `if (false)` (node paritesi) | `parite-t3.mjs`: 3 FARK ("00442079460958" → "0044 207 94 60" …) | AYNI |
| MJ3-2 | girdi-maske.js — '+' yalnız baştaysa | `parite-t3.mjs`: 1 FARK ("Tel: +90 …" → "905 321 23 45") | AYNI |
Ham: `mut-sonuc-t4.tsv`, `mut-sonuc-t4b.tsv`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata**, 4 Uyarı (Tests projesinin önceden var olan analizör uyarıları: SUBEKAPSAMASinir:49, KesifP2:455, SonKalanlar:587/818 — bu paketin satırları değil).

### 00.6 Testler ve tam süit (tur 4, son kod)
- Yeni test (tur 3+4): `GirdiAilesi2Tur3Testleri.cs` → 38 durum (tur 3) + `GirdiAilesi2Tur4ServisTestleri` 4 durum (tur 4) = **42**. Paket toplamı 154 + 42 = **196**.
- Filtreli: `~GirdiAilesi2Tur` 77/77 ✓; `~Q105SinirTestleri|~MASAK370Sinir|~UrunAcilisAtomik|~GirdiAilesi2Tur` 182/182 ✓; t3 kırmızıları (`~ORTAK1K4Sinir|~G12_Telefon|~TM1Sinir|~SATIS3SinirTestleriT17Kapi|~C4_15|~SKC03|~SK12_`) 92/92 ✓.
- Tam süit A (trx `/private/tmp/kuyumcu-serit/trx-pGIRDI-AILESI-2-tur4`): 11015 · geçen 11010 · başarısız 3 · atlanan 2. Kırmızılar: `Q105SinirTestleri.C_Vezne_kurus_alti_ve_4_hane_istemcide_yuvarlanmadan_gider` ve `MASAK370SinirHttpTestleri.H1_…` — **BU PAKETİN** (Para/banka 2 ondalık sözleşmesi) → §00.2 md.1'deki gibi güncellendi; `UrunAcilisAtomikEkranTestleri.E2_…` — başka şerit aynı dakikada `~UrunAcilisAtomik` koşuyordu; filtreli tekrar yeşil.
- **Tam süit B (son kod, trx `/private/tmp/kuyumcu-serit/trx-pGIRDI-AILESI-2-tur4b`): toplam 11023 · geçen 11019 · başarısız 2 · atlanan 2.** Kırmızılar BU PAKETİN DEĞİL: `Toplu5GunSonuTestleri.T5_PRO_gun_sonu_durumu_cumle_duzeninde_formul_ipucunda` — `Pages/Raporlar.razor` + `Sozlukler.KarRapor.cs` 12:24'te başka şeritçe değiştirildi (o dakikada çözüm derlenmiyordu: `KarRaporMetinleri.NakitFarkiAciklama` yok); tam süit A'da yeşildi, bu paket o dosyalara dokunmadı; filtreli 2/2 kırmızı (deterministik, sahibi Raporlar/KarRapor şeridi). `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` — kararsız: filtreli 2/2 yeşil. → **Paket farkı (yeni kırmızı) = 0.**

### 00.7 denetim.sh (tur 4, son kod)
Tek ✗ = **P-30 DEMO_INSAN_SURESI** (kapsam dışı). GIRDI-2: SAYISAL_OLU_SINIR 0 ✓ · IBAN_REGEX_DTO 0 ✓ · TEST_GECERSIZ_FIKSTUR 0 ✓ · HAM_TARIH_GIRDISI 7/7 ✓ · GIRDI_TUR_GENEL_BILESEN 15/15 ✓ · DTO_DECIMAL_RANGESIZ 26/26 ✓ · EKRAN_SUNUCU_UZUNLUK_FARKI 168/168 ✓.

### 00.8 Süreçler ve test kiracısı
- Tur 3'ten kalan kendi süreçlerim (API 5188 PID 46914/46917, vekil 5189 PID 48260, Chrome CDP 9985) tur 4 başında durduruldu; tur 4 API/vekil/Chrome iş bitince durduruldu: `lsof` 5188/5189/9986/9987 boş.
- `PAKETGIRDI-AILESI-2-TEST T3` (01M4CTTWCM6Z46J4K5HBGK95X7) `zsh kiraci-temizle.sh … --uygula` ile silindi; SQL: tenants'ta bu id 0, banka_hesaplari 0, cariler 0, kullanicilar 0; `kiraci_davetleri` satırı silindi (1). tenants: tur 4 başında **18** (T3 dâhil) → silme öncesi 17 (bu arada başka şerit kendi `PAKETGRID1-TEST Hakem` kiracısını sildi) → son **16**. `QA-TEST-*` kiracıları (4) dokunulmadı.
- Dokunulan sayfalar (TEK DOKUNUŞ): `Pages/Isletme.razor` (tur 3 — doğrulayıcının KRİTİK bulgusunun açık talebi: sayfadaki ikinci telefon kuralı ortak bileşenin kapısına bağlandı; gerekçe bu satır). `Pages/Banka.razor` (tur 1, değişmedi).


## 0. TUR 2 — doğrulayıcı + tasarım hakemi bulgularının kapatılması (08.10, 04:20–05:50)

> Bu bölüm tur 1 raporunun ÜSTÜNDEDİR; aşağıdaki bölümlerde değişen sayılar (§4 test, §5 mutasyon, §6 canlı, §7 denetim, §10 açık kalanlar) burada güncel hâliyle verilir.

### 0.1 Bulgu → yapılan
| # | Bulgu (önem) | Yapılan | Kanıt |
|---|---|---|---|
| 1 | TEST_GECERSIZ_FIKSTUR yanlış 0; `PosYardim.cs:115` `$"TR{…}"` üreticisi (ORTA) | `PosYardim.HesapEkle` → `TestIban.Uret()`. denetim.sh deseni: `"TR"+(Random…)` yanında araya ekleme kalıbı `\$@?"TR\{` de üretici sayılır. | **Sayaç mutasyonu:** PosYardim eski satıra döndürüldü → sayaç **0 → 1** (`PosYardim.cs:115` listelendi) → geri kopya, cmp AYNI → 0. |
| 2 | IBAN ön ekli yapıştırma ("iban: tr33-…") `IB33…` üretiyordu, Kaydet açık (ORTA) | `Iban.BaslangicaKes` (C#) + `baslangicaKes` (JS, aynı algoritma): metin 2 harf + 2 rakamla başlıyorsa olduğu gibi; değilse ilk `TR`+2 rakam; o da yoksa ilk 2 harf + 2 rakam. Yabancı IBAN yazılırken (4+ karakter) tam kural geçene dek alan altında **"IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an n karakter)."** + Kaydet KİLİTLİ (yeni kod `IbanEksik`, TR + EN). | bUnit `T2_Iban_on_ekli_yapistirma_…` (5 satır, "Ref AB12 / IBAN TR33…" dâhil), `T2_Iban_on_ekli_bozuk_…`, `T2_Yabanci_IBAN_…` (2); node paritesi 9/9 AYNI (`parite2.mjs`); gerçek Chrome S1/S2/S3 8/8 yapılandırma (§0.4). |
| 3 | PATCH `iban:"   "` 200 + sessiz yok sayma (ORTA) | `BankaModulu.HesapGuncelle`: `Length == 0` → `string.IsNullOrWhiteSpace` → 400 `alan:iban` "IBAN boşaltılamaz". | Test `Banka_PATCH_bos_ya_da_yalniz_bosluk_IBAN_400_eski_deger_korunur` ("", "   ", "\t "; ardından gövdede yoksa KORU). Canlı: 3 × 400 (§0.3). |
| 4 | Gram K10 uygulanmıyor; Ayar/Gerekçe bileşeni yok (ORTA) | `Gram` → `[Parameter] AgirlikTuru? Tur` (Gram / Tas / Has) + `Brut`; Tur verilirse Core `AgirlikKurali.Denetle` (yeni tek giriş) uygulanır ve eksi tuşu kapanır. Tur verilmezse BUGÜNKÜ davranış (17 mevcut kullanım; hangisinin hangi tür olduğu ekran göçü kararı). **Ayar** ve **Gerekçe** → sapma §0.2 md.2 (GOC-C'ye devir). | `T2_Gram_Tur_Gram_…`, `T2_Gram_Tur_Tas_…`, `T2_Gram_Tur_Has_…`, `T2_Gram_Tur_yoksa_bugunku_davranis_korunur`, `T2_AgirlikKurali_Denetle_ture_gore` (4). |
| 5 | Bozuk eski IBAN'lı hesap yalnız IBAN düzeltilince düzenlenebiliyor (DÜŞÜK) | Bilinçli fail-closed davranış — sapma §0.2 md.3'te gerekçesiyle. Kod değişmedi. | — |
| 6 | Telefon 444 + 4 hane reddediliyordu (DÜŞÜK) | `TelefonDogrulayici`: ulusal kısım `444` + 4 hane (toplam 7) → geçerli (`TekNumaraOnek` / `TekNumaraHane`). Maske "444 12 34" / "0444 12 34" (C# + JS; ilk denemede Chrome'da "444 123 4" okunaksız göründüğü için eklendi). | `T2_Telefon_444_dort_hane_gecerli` (4), `T2_Telefon_444_disi_kisa_numara_hane_hatasi` (3), `T2_Telefon_444_maskesi_okunur_gruplar` (5); node 7/7 AYNI (`parite3.mjs`); Chrome S4 8/8 "444 12 34", hata yok. |
| 7 | Cırcır tavanı 169 (ölçüm 168); FIKSTUR muafiyeti geniş; TcknVeyaVkn hane mesajı hep 11 (DÜŞÜK) | Tavan **168**. Muafiyet yalnız `olumsuz-fikstür` işareti, ±4 satırda `Throws` / `400` / `BadRequest`, ya da AYNI satırda `Assert.False(` (serbest "hata/geçersiz" sözcüğü artık muaf DEĞİL). `KimlikKurali.TcknVeyaVkn` hane hatası → yeni kod `KimlikHaneIkili`: "Numara 10 (vergi no) ya da 11 (TC kimlik no) haneli olmalı (şu an n)." | Muafiyet daraltma kanıtı: "// hata ile ilgili" yorumunun yanına geçersiz IBAN konunca sayaç 0 → 1 (eski desenle 0 kalırdı). `T2_TcknVeyaVkn_hane_mesaji_10_ya_da_11_der` (2), `T2_Tckn_tek_basina_hane_mesaji_11_kalir`. Denetim: 168/168 ✓. |
| 8 | **KRİTİK (tasarım hakemi)** — Kaydet kilit sebebi bağlantı mavisiyle (`metin-amber` = `var(--vurgu)`) | Düzeltme BİLEŞENİN kendisinde: `SayiKapisiSebebi.razor` → `metin-kucuk metin-soluk kilit-sebebi` (dekont-kaydet-sebep ile aynı soluk ton). 16 kullanım + gelecekteki GOC göçleri aynı görünümü alır. | `T2_Kaydet_kilit_sebebi_soluk_tonda_baglanti_mavisi_degil`; Chrome: 8/8 yapılandırmada `getComputedStyle(#hesap-kilit-sebep).color` = `var(--metin-soluk)` (açık tema rgb(90,101,119); vurgu rgb(36,87,214) DEĞİL). Görüntü: `S2-onekli-bozuk__*`. |

### 0.2 Tur 2 sapmaları (gerekçeli)
1. **Yabancı IBAN ülke uzunluk çizelgesi kodlanmadı.** Birincil kaynak (SWIFT ISO 13616 IBAN Registry) bu turda erişilemedi (swift.com 403). Ezberden ~80 ülke uzunluğu yazmak tahminle kural olurdu. Yerine **fail-closed**: ülke kodu + kontrol hanesinden sonra tam kural (mevcut yabancı desen + MOD 97-10) geçene dek alan altında neden yazar ve Kaydet kilitli kalır; geçtiği an kalkar (Chrome S3/S3b). Sunucuda kısa yabancı IBAN hâlâ `IbanBicim` metniyle ("IBAN TR ile başlar…") reddediliyor — metin yabancı numara için yanıltıcı (açık kalan, DÜŞÜK).
2. **`Ayar` (Secici sarmalayıcı) ve `Gerekce` (GerekceAlani taşınması) bu pakette yazılmadı → GOC-C'ye devredildi.** İkisi de yeni kural taşımıyor (K13 `AyarKurali`, K30 `GerekceKurali` Core'da hazır ve testli; `[Ayar]` / `[Gerekce]` öznitelikleri hazır); ayar seçimi bugün zaten kapalı liste (Secici); GerekceAlani çok sayıda ekranda — taşıma ekran göçüdür (GÖREV SINIRI: bu paket ekran göçü yapmaz). Gram'ın `Tur` parametresi de aynı gerekçeyle 17 mevcut kullanıma bağlanmadı.
3. **Bozuk eski IBAN'lı hesap düzenleme çekmecesinde kilitli** (geliştirme verisinde 11 hesaptan 6'sı MOD 97'den geçmiyor). Çekmece açılınca kayıtlı IBAN aynı kuraldan geçer, alan altında hata görünür, Kaydet kilitlenir; yalnız hesap adı değiştirmek için de IBAN düzeltilmeli. **Bilinçli**: bozuk IBAN'a dekont/ödeme bağlanmasını sürdürmek yerine düzeltmeye zorlar; sebep görünür (sessiz değil). İstenirse ayrı karar: çekmece yalnız DEĞİŞEN IBAN'ı gönderir ve kayıtlı bozuk değer kilit değil uyarı olur — ekip oyu / kurucu tercihi.
4. **Telefon 444 kaynağı:** envanter K2 ("… 850, 800, 444") ve BTK'nın alan kodsuz tek numara uygulaması; ITU bildirimi (10 hane) bu istisnayı yazmıyor, BTK birincil belgesi bu turda bulunamadı. Kural GENİŞLETİCİdir (yalnız 444 + tam 4 hane); vergi/mevzuat hükmü değildir. Sunucudaki eski cari telefon kuralı (`Iletisim.TelefonGecerli`, 10–13 hane) 7 haneli 444'ü hâlâ reddeder → tek kurala iniş GOC-B/D.
5. **Mutasyon yöntemi düzeltmesi:** tur 1 betiği (`mut.py`) geri kopyayı `shutil.copy2` ile yapıyordu; bu dosyanın ESKİ değiştirilme zamanını da geri yazdığı için artımlı derleme geri alınan dosyayı görmüyor, sonraki mutasyonun testleri bir önceki mutasyonun ikili dosyasıyla koşuyordu (tur 2'nin ilk koşumunda yakalandı: ilgisiz testler kırmızıydı). `mut2.py` geri kopyadan sonra `os.utime` ile zamanı tazeler. **Tur 1'in 36 mutasyonu da düzeltilmiş betikle YENİDEN koşuldu** (§0.5): hepsi kırmızı, hepsi cmp AYNI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 Uyarı 0 Hata.
6. Boş artık dosya: `dotnet/Kuyumcu.Tests/ZzMutFikstur.cs` (0 bayt) — tur 2'de muafiyet daraltma kanıtı için açıldı ve `printf '' >` ile boşaltıldı; silme kancası nedeniyle kaldırılamadı, derlemeye etkisi yok. **Kurucu silsin.**

### 0.3 Canlı API + SQL (API 5188, kiracı `PAKETGIRDI-AILESI-2-TEST T2`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /bankalar `iban:"tr33 0006 1005 1978 6457 8413 26"` | 200, kayıt boşluksuz | 200, `iban: TR330006100519786457841326` |
| POST /bankalar `iban:"IB33 0006 …"` (tur 1'deki bozuk yapıştırma çıktısı) | 400 alan iban | 400 `{iban:["IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin."]}` |
| PATCH `{iban:"   ", hesapAdi:"Degisti"}` | 400, hiçbir alan değişmez | 400 `{iban:["IBAN boşaltılamaz"]}` |
| PATCH `{iban:""}` / `{iban:"\t "}` | 400 | 400 / 400, aynı metin |
| PATCH `{hesapAdi:"T2 Vadesiz Yeni"}` (iban yok) | 200, IBAN korunur | 200 `hesapAdi: T2 Vadesiz Yeni, iban: TR330006100519786457841326` |
| POST yabancı `GB82 WEST 1234 5698 7654 32` | 200, boşluksuz | 200 `GB82WEST12345698765432` |
| POST yabancı eksik `GB82 WEST 1234` | 400 | 400 (metin `IbanBicim` — §0.2 md.1) |

SQL (`banka_hesaplari ⨝ tenants`, ad `PAKETGIRDI-AILESI-2-TEST%`): `T2 Vadesiz Yeni | TR330006100519786457841326 | TRY` · `T2 Yabanci | GB82WEST12345698765432 | GBP` — PATCH 400'lerinden sonra hesap adı `Degisti` OLMADI.

### 0.4 Gerçek tarayıcı (başsız Chrome, CDP 9981–9984, vekil 5189) — 2 genişlik × 2 mod × 2 tema × 5 senaryo = 40 ölçüm, 32 görüntü
Senaryolar: **S1** "iban: tr33-0006 1005/1978 6457 8413 26" yapıştır · **S2** "IBAN: TR34-0006 1005 1978 6457 8413 26" yapıştır (kontrol hanesi bozuk) · **S3** "GB82 WEST 1234" tuş tuş (yabancı, eksik) · **S3b** kalanı yaz (geçerli) · **S4** /cariler telefon "444 1 234" tuş tuş + çıkış. Ölçüm `sikisma-olc.js`. Beklentiye uymayan satır: **0/40**. Kilit sebebi rengi 8/8 S2 ölçümünde `--metin-soluk` (VURGU 0).
*K9 "binen" 3–6: tümü açık pencerenin (Yeni hesap / Yeni müşteri) arkasında kalan sayfa öğeleriyle kesişme (ör. `p.stat-etiket "TOPLAM BAKİYE" ⟂ span "Banka Adı"`) — katman üstü pencere, kusur değil; tur 1 tabanıyla aynı sayılar.*

| Mod | Tema | px | Senaryo | Kutudaki değer | Alan altı hata | Kaydet kilitli | Kilit sebebi = alan altı | Sebep rengi | K1 | K3 taşan/kırık | K5 TRY | Yatay | K9 binen* |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASIT | aydinlik | 1280 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | aydinlik | 1280 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | aydinlik | 1280 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | aydinlik | 375 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | aydinlik | 375 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | aydinlik | 375 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 4 |
| BASIT | karanlik | 1280 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | karanlik | 1280 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | karanlik | 1280 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | karanlik | 375 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | karanlik | 375 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | karanlik | 375 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 4 |
| PRO | aydinlik | 1280 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 3 |
| PRO | aydinlik | 1280 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 3 |
| PRO | aydinlik | 1280 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 0 |
| PRO | aydinlik | 375 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 6 |
| PRO | aydinlik | 375 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 6 |
| PRO | aydinlik | 375 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 3 |
| PRO | karanlik | 1280 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 3 |
| PRO | karanlik | 1280 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 3 |
| PRO | karanlik | 1280 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 0 |
| PRO | karanlik | 375 | S1-onekli-yapistir-gecerli | `TR33 0006 1005 1978 6457 8413 26` | — | False | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | S2-onekli-yapistir-bozuk | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | True | AYNI | SOLUK | 0 | 0/0 | 0 | 0 | 6 |
| PRO | karanlik | 375 | S3-yabanci-eksik | `GB82 WEST 1234` | IBAN henüz tamamlanmadı ya da kontrol hanesi tutmuyor (şu an 12 karakter). | True | AYNI | — | 0 | 0/0 | 0 | 0 | 6 |
| PRO | karanlik | 375 | S3b-yabanci-tamam | `GB82 WEST 1234 5698 7654 32` | — | False | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | S4-telefon-444 | `444 12 34` | — | — | — | — | 0 | 0/0 | 0 | 0 | 3 |

Görüntüler (Read ile bakıldı): `/private/tmp/kuyumcu-serit/GIRDI-AILESI-2/canli2/` — `S2-onekli-bozuk__BASIT_aydinlik_1280.png` (kilit sebebi gri, Kaydet soluk), `S2-onekli-bozuk__PRO_karanlik_1280.png`, `S3-yabanci-eksik__PRO_karanlik_375.png` (neden alan altında ve Kaydet altında aynı metin, 375'te taşma yok), `S1-onekli-gecerli__BASIT_karanlik_375.png` (IBAN temiz, Kaydet açık), `S4-telefon-444__PRO_karanlik_1280.png` / `S4-telefon-444__BASIT_aydinlik_375.png` ("444 12 34", hata yok).

### 0.5 Mutasyon (tur 2: 11 yeni + tur 1'in 36'sı düzeltilmiş betikle yeniden; yedek `/private/tmp/kuyumcu-serit/mut-yedek/GIRDI-AILESI-2`, betik `mut2.py`)
Filtre: `~GirdiAilesi2Tur2|~GirdiBanka|~GirdiBilesen|~GirdiKural` (tur 2) / tur 1 maddelerinin kendi filtreleri. **47/47 kırmızı, 47/47 cmp AYNI.** (M2-2 ilk koşumda başka şeridin eşzamanlı derlemesinden NETSDK1177 apphost hatası aldı → tek başına yeniden koşuldu: kırmızı. MW-2'nin tur 1 arama metni değişen `YazarkenDenetle` gövdesinde artık yoktu → yeni gövdenin TR dalında aynı mutasyon: kırmızı.)

| Mutasyon (tur 2) | Değişiklik |
|---|---|
| MT2-1 | Iban.BaslangicaKes: "önce TR + 2 rakam" araması silindi |
| MT2-2 | Iban yabancı yazarken kuralı → `return null` |
| MT2-3 | BankaModulu PATCH `IsNullOrWhiteSpace` → `Length == 0` |
| MT2-4 | Telefon 444 dalı silindi |
| MT2-5 | TcknVeyaVkn hane dalı devre dışı |
| MT2-6 | Gram `KuralDenetle` → yalnız base |
| MT2-7 | SayiKapisiSebebi sınıfı → `metin-amber` |
| MT2-8 | Taş `x >= brüt` → `x > brüt` |
| MT2-9 | Gram Tur verilince eksi tuşu açık |
| MT2-10 | `AgirlikKurali.Denetle` Has dalı → null |
| MT2-11 | Telefon 444 maskesi silindi |
| (sayaç) | PosYardim eski `$"TR{…}"` → TEST_GECERSIZ_FIKSTUR 0→1; "hata" yorumlu geçersiz IBAN → 0→1 (cmp AYNI) |

| Kod | Dosya | Kırmızı test (ADIYLA, ilk 2) | Sayı | cmp |
|---|---|---|---|---|
| MT2-1 | `Iban.razor` | GirdiAilesi2Tur2Testleri.T2_Iban_on_ekli_yapistirma_IBAN_sozcugunu_ulke_kodu_saymaz(yapistirilan: | 1 | AYNI |
| MT2-2 | `Iban.razor` | GirdiAilesi2Tur2Testleri.T2_Yabanci_IBAN_tamamlanana_dek_alan_altinda_neden_ve_Kaydet_kilitli | 1 | AYNI |
| MT2-3 | `BankaModulu.cs` | GirdiBankaSunucuTestleri.Banka_PATCH_bos_ya_da_yalniz_bosluk_IBAN_400_eski_deger_korunur(bos: | 1 | AYNI |
| MT2-4 | `TelefonDogrulayici.cs` | GirdiAilesi2Tur2Testleri.T2_Telefon_444_dort_hane_gecerli(tel: | 1 | AYNI |
| MT2-5 | `MetinKurallari.cs` | GirdiAilesi2Tur2Testleri.T2_TcknVeyaVkn_hane_mesaji_10_ya_da_11_der(no: | 1 | AYNI |
| MT2-6 | `Gram.razor` | GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Gram_sifir_ve_dort_ondalik_hata_kilit_eksi_tusu_kapali; GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Has_alti_ondalik_gecer_yedisi_hata | 3 | AYNI |
| MT2-7 | `SayiKapisiSebebi.razor` | GirdiAilesi2Tur2Testleri.T2_Kaydet_kilit_sebebi_soluk_tonda_baglanti_mavisi_degil | 1 | AYNI |
| MT2-8 | `SayiKurallari.cs` | GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Tas_brutten_kucuk_olmali; GirdiKuralTestleri.K10_Gram_ve_tas_brutten_kucuk | 2 | AYNI |
| MT2-9 | `Gram.razor` | GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Gram_sifir_ve_dort_ondalik_hata_kilit_eksi_tusu_kapali | 1 | AYNI |
| MT2-10 | `SayiKurallari.cs` | GirdiAilesi2Tur2Testleri.T2_AgirlikKurali_Denetle_ture_gore(tur:; GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Has_alti_ondalik_gecer_yedisi_hata | 2 | AYNI |
| MT2-11 | `Telefon.razor` | GirdiAilesi2Tur2Testleri.T2_Telefon_444_maskesi_okunur_gruplar(girilen: | 1 | AYNI |
| M1-1 | `IbanDogrulayici.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Iban_bilinen_gecerli_ornek_ve_son_hane_degisimi; Dogrulayici.GIRDIAILESI2SinirTestleri.Oznitelik_0_005_tutar_ve_bozuk_IBAN_ayni_sozluk_metniyle_reddeder | 6+ | AYNI |
| M1-2 | `IbanDogrulayici.cs` | GirdiKuralTestleri.K1_Iban_banka_kodunda_ya_da_rezervde_harf_IbanBicim | 1 | AYNI |
| M2-1 | `TelefonDogrulayici.cs` | GirdiBilesenTestleri.TelefonBicimli_artik_Telefon_ile_ayni_bilesen; GirdiBilesenTestleri.Telefon_10_hane_dolunca_aralik_hatasi_ve_kilit | 4 | AYNI |
| M2-2 | `TelefonDogrulayici.cs` | GirdiKuralTestleri.K2_Telefon_gecersiz(tel: | 1 | AYNI |
| M3-1 | `EpostaDogrulayici.cs` | GirdiBilesenTestleri.Eposta_bosluk_yazilmaz_cikista_kucuk_harf_ve_kural; GirdiKuralTestleri.K3_Eposta_bicim_hatasi(e: | 3 | AYNI |
| M8-1 | `TarihPenceresi.cs` | GirdiBilesenTestleri.Tarih_08102026_yazilinca_08_10_2026_olur_ve_deger_gider; GirdiBilesenTestleri.Tarih_olmayan_gun_ve_pencere_disi_yil_hata_ve_kilit | 3 | AYNI |
| M8-2 | `TarihPenceresi.cs` | GirdiBilesenTestleri.Tarih_Islem_turunde_ileri_tarih_ve_Dogum_turunde_18_yas; GirdiKuralTestleri.K8b_Dogum_18_yas_TMK_md11_ve_120_yil | 2 | AYNI |
| M8-3 | `TarihPenceresi.cs` | GirdiBilesenTestleri.Tarih_Islem_turunde_ileri_tarih_ve_Dogum_turunde_18_yas; GirdiBilesenTestleri.Tarih_takvim_yerli_secici_ayni_yoldan_ve_dugme_degil | 3 | AYNI |
| M8-4 | `TarihPenceresi.cs` | GirdiKuralTestleri.K8e_Aralik_ters_ve_5_yil | 1 | AYNI |
| M8-5 | `TarihPenceresi.cs` | GirdiBilesenTestleri.Tarih_olmayan_gun_ve_pencere_disi_yil_hata_ve_kilit; GirdiKuralTestleri.K8_Tarih_coz_gecersiz(ham: | 2 | AYNI |
| M9-1 | `SayiKurallari.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Kurus_0_005_sessizce_yuvarlanmaz_hata_0_01_gecer; Dogrulayici.GIRDIAILESI2SinirTestleri.Oznitelik_0_005_tutar_ve_bozuk_IBAN_ayni_sozluk_metniyle_reddeder | 4 | AYNI |
| M9-2 | `SayiKurallari.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Eksi_deger_reddedilir_Isaretli_tutarda_isaret_korunur; GirdiKuralTestleri.K9_Tutar_pozitif_sinirlar | 3 | AYNI |
| M10-1 | `SayiKurallari.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Has_0_0001_gecer_7_hane_red_Gram_4_hane_red; GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Tas_brutten_kucuk_olmali | 3 | AYNI |
| M11-1 | `SayiKurallari.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Has_0_0001_gecer_7_hane_red_Gram_4_hane_red; GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Has_alti_ondalik_gecer_yedisi_hata | 5 | AYNI |
| M12-1 | `SayiKurallari.cs` | GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K12_Milyem_1_1000_ve_sapma_uyarisi | 3 | AYNI |
| M13-1 | `SayiKurallari.cs` | GirdiKuralTestleri.K13_Ayar_yalniz_liste; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | 2 | AYNI |
| M14-1 | `SayiKurallari.cs` | GirdiKuralTestleri.K14_Adet_tam_sayi_en_az_1; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | 2 | AYNI |
| M15-1 | `SayiKurallari.cs` | GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K15_Yuzde_ve_marj | 3 | AYNI |
| M16-1 | `SayiKurallari.cs` | GirdiKuralTestleri.K16_Kur_pozitif_6_ondalik | 1 | AYNI |
| M17-1 | `SayiKurallari.cs` | GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K17_Gun_sayisi_tam_ve_aralik | 3 | AYNI |
| M18-1 | `MetinKurallari.cs` | GirdiKuralTestleri.K18_Barkod_ascii_ve_ean13_uyarisi; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | 2 | AYNI |
| M19-1 | `MetinKurallari.cs` | GirdiBilesenTestleri.Pasaport_Barkod_AdSoyad_Adres_maske_ve_kural; GirdiKuralTestleri.K19_K20_Ad_ve_unvan | 3 | AYNI |
| M21-1 | `MetinKurallari.cs` | GirdiBilesenTestleri.Pasaport_Barkod_AdSoyad_Adres_maske_ve_kural; GirdiKuralTestleri.K21_Adres_10_500 | 3 | AYNI |
| M7-1 | `MetinKurallari.cs` | GirdiKuralTestleri.K7_Pasaport | 1 | AYNI |
| M30-1 | `GirdiHatasi.cs` | GirdiKuralTestleri.K30_Gerekce_20_1000; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | 2 | AYNI |
| MW-1 | `SayisalAlan.cs` | GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Gram_sifir_ve_dort_ondalik_hata_kilit_eksi_tusu_kapali; GirdiAilesi2Tur2Testleri.T2_Gram_Tur_Has_alti_ondalik_gecer_yedisi_hata | 6+ | AYNI |
| MW-2 | `Iban.razor` | GirdiAilesi2Tur2Testleri.T2_Iban_on_ekli_bozuk_IBAN_26_karakterde_hata_ve_kilit; GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar | 5 | AYNI |
| MW-3 | `Tarih.razor` | GirdiBilesenTestleri.Tarih_08102026_yazilinca_08_10_2026_olur_ve_deger_gider; GirdiBilesenTestleri.Tarih_eksik_yazim_cikista_bicim_hatasi | 5 | AYNI |
| MW-4 | `GecersizAlanKapisi.cs` | GirdiAilesi2Tur2Testleri.T2_Iban_on_ekli_bozuk_IBAN_26_karakterde_hata_ve_kilit; GirdiAilesi2Tur2Testleri.T2_Kaydet_kilit_sebebi_soluk_tonda_baglanti_mavisi_degil | 6+ | AYNI |
| MW-5 | `Telefon.razor` | GirdiAilesi2Tur2Testleri.T2_Telefon_444_maskesi_okunur_gruplar(girilen:; GirdiBilesenTestleri.TelefonBicimli_artik_Telefon_ile_ayni_bilesen | 5 | AYNI |
| MW-6 | `Banka.razor` | GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok | 2 | AYNI |
| MA-1 | `BankaModulu.cs` | GirdiBankaSunucuTestleri.Banka_POST_bosluklu_kucuk_harf_ve_harfli_hesap_no_gecer_normallestirilir; GirdiBankaSunucuTestleri.Banka_servis_yolu_da_Core_kuralini_cagirir | 2 | AYNI |
| MA-2 | `GirdiOznitelikleri.cs` | Dogrulayici.GIRDIAILESI2SinirTestleri.Oznitelik_0_005_tutar_ve_bozuk_IBAN_ayni_sozluk_metniyle_reddeder; GirdiBankaSunucuTestleri.Banka_PATCH_mod97_bozuk_IBAN_400_eski_deger_korunur | 3 | AYNI |

### 0.6 Testler ve tam süit (tur 2, son kod, BİR kez)
- Yeni dosya `Kuyumcu.Tests/GirdiAilesi2Tur2Testleri.cs` (**35** test durumu, Theory satırları dâhil; trx'te 35/35 geçti) + `GirdiBankaTestleri.cs` içinde `Banka_PATCH_bos_ya_da_yalniz_bosluk_IBAN_400_eski_deger_korunur` (3 satır). Tur 2 yeni test: **38**. Paket toplamı: 116 + 38 = **154**.
- Filtreli: `~GirdiAilesi2Tur2|~GirdiBanka|~GirdiBilesen|~GirdiKural` 149/149 ✓ (444 maskesi testinden önce); `~Girdi|~Telefon|~CumaM|~Cari` 836/838 — 2 kırmızı `GridKartCssTestleri.GC02/GC03` (dosya `GridTestleri.cs`, 04:53'te **GRID-1** şeridi tarafından değiştiriliyordu; tam süitte yeşil).
- **Tam süit (trx `/private/tmp/kuyumcu-serit/trx-pGIRDI-AILESI-2-tur2`): toplam 10898 · çalışan 10896 · geçen 10895 · başarısız 1 · atlanan 2.**
  Tek kırmızı: `SATIS2EkranTestleri.PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet_kutular_KATLI_tiklaninca_acilir` (satır 255, tıklamadan sonra `#islem-ust-ayrinti` hâlâ `hidden` — yeniden çizim yarışı). **Kararsızlık kanıtı:** aynı ikili dosyayla `~SATIS2EkranTestleri` 3 ardışık koşum: 25/25, 25/25, 25/25 yeşil. Tur 2 bu ekrana dokunmadı (değişiklikler: Iban, Gram, Telefon maskesi, SayiKapisiSebebi, Core Girdi, BankaModulu PATCH, denetim.sh, PosYardim). Tur 1'in iki GRID-1 kırmızısı (B9, S1i_13) bu koşumda yeşil. → **Paket farkı (yeni kırmızı) = 0.**
- Build: son `dotnet build Kuyumcu.sln` **0 Uyarı 0 Hata**.

### 0.7 denetim.sh (tur 2, son kod)
Tek ✗ = **P-30 DEMO_INSAN_SURESI** (kapsam dışı, kurucu kalemi). GIRDI-2: SAYISAL_OLU_SINIR 0 ✓ · IBAN_REGEX_DTO 0 ✓ · TEST_GECERSIZ_FIKSTUR 0 ✓ (yeni desen + dar muafiyetle) · HAM_TARIH_GIRDISI 7/7 ✓ · GIRDI_TUR_GENEL_BILESEN 15/15 ✓ · DTO_DECIMAL_RANGESIZ 26/26 ✓ · EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) **168/168** ✓.
Not: tavan ölçüme eşit. `YasalParamFormu.razor` (YASAL-PARAM-EKRAN şeridi, t2) yeni sabit `EnFazla` eklerse sayaç 169'a çıkar ve ✗ verir — cırcırın amacı bu; o şeridin `AlanSinirlari` sabitini kullanması gerekir.

### 0.8 Test kiracısı (tur 2)
`PAKETGIRDI-AILESI-2-TEST T2` (01M4CKM2G8QYMYWJFGB8CVW32K) `zsh kiraci-temizle.sh … --uygula` ile silindi; SQL: tenants'ta bu id 0, banka_hesaplari 0, kullanicilar 0; `kiraci_davetleri` satırı silindi (1). tenants: başlangıç **17** → kiracı açılınca 18 → son **16**. Fark (−1) başka bir şeridin aynı zaman diliminde kendi test kiracısını silmesidir; bu paket yalnız kendi id'sini sildi (kalan listede PAKET* ve QA-TEST-* kiracıları duruyor). 5188/5189 dinleyen yok (lsof boş). `QA-TEST-` kiracılarına dokunulmadı.

## 1. Yapılanlar

### 1.1 Core — tek kural kaynağı (`Kuyumcu.Core/Domain/Girdi/`, YENİ)
| Dosya | Kural (katalog) | Kaynak sınıfı |
|---|---|---|
| `GirdiHatasi.cs` | `GirdiKodu` (45 kod), `GirdiHatasi(Kod,S1,S2,Gun)`, `AlanSinirlari` (ekran = DTO uzunluk sabitleri), `GirdiYardim` | — |
| `IbanDogrulayici.cs` | K1: TR = `TR`+2 kontrol+5 banka+1 rezerv+16 hesap (hesap bölümü harf olabilir), ISO 7064 MOD 97-10; yabancı IBAN bugünkü desen + MOD 97-10; `Normallestir`, `TrTamamla` | BİRİNCİL — TCMB Tebliğ 2008/6 md.3–4, RG 10.10.2008/27020 |
| `TelefonDogrulayici.cs` | K2: ulusal anlamlı numara tam 10 hane; 2xx/3xx/4xx coğrafi, 5xx yalnız BTK listesi (501, 505–507, 510, 516, 524, 530–549, 551–555, 559, 592), 800/850; `+`/`00` uluslararası, E.164 ≤ 15 | BİRİNCİL — ITU-T E.164 TR bildirimi (BTK 2.III.2018), envanter K2 |
| `EpostaDogrulayici.cs` | K3: yerel ≤ 64, alan ≤ 255, adres ≤ 254, tek `@`, alan adında nokta, boş etiket yok | STANDART — RFC 5321 §4.5.3.1, RFC 5322 §3.4.1 |
| `TarihPenceresi.cs` | K8 ortak 1900–2100 · K8a İşlem: son 1 yıl..bugün · K8b Doğum: 120..18 yıl önce · K8c Vade: bugün..10 yıl · K8d Keşide: TEYİTSİZ → bugünkü davranış (yalnız ortak pencere) · K8e Rapor 2000..gelecek yıl, aralık ≤ 5 yıl; `Coz` (ggaayyyy / gg.aa.yyyy / yyyy-MM-dd) | K8b BİRİNCİL — 4721 TMK md.11; diğerleri İŞ KURALI |
| `SayiKurallari.cs` | K9–K9b Tutar (Pozitif/SifirOlabilir/Isaretli, 2 ondalık, decimal(15,2)), K10 Gram + taş < brüt, K11 Has (6 ondalık), K12 Milyem 1–1000 (+ ±50 sapma uyarısı), K13 Ayar listesi, K14 Adet tam ≥ 1, K15 Yüzde 0–100 / Marj ≤ 500, K16 Kur > 0 (6 ondalık), K17 Gün sayısı; `SayiKurali.Aralik` (EnAz/EnCok) | İŞ KURALI + kolon tavanı (KolonTavani.cs ile aynı sayı) |
| `MetinKurallari.cs` | K18 Barkod ASCII 32–126, 1–50, boşlukla başlamaz, EAN-13 uyarı · K19/K20 Ad/Ünvan ≥ 2 + harf · K21 Adres ≥ 10 (TEYİTSİZ eşik, hiçbir DTO'ya UYGULANMADI) · K7 Pasaport `^[A-Z0-9]{5,20}$` · K30 Gerekçe 20–1000 | STANDART (GS1, ISO/IEC 15417, ICAO 9303) + İŞ KURALI |

Metinler: `Kuyumcu.Core/I18n/Sozlukler.Girdi.cs` (YENİ, `GirdiSozlugu` TR + EN, konumsal kayıt → eksik dil = derleme hatası; her kodun iki dilde metni `Sozluk_her_kod_iki_dilde` testiyle kilitli). Ekran ve sunucu AYNI anahtardan aynı cümleyi basar.

### 1.2 Sunucu öznitelikleri (`Kuyumcu.Api/Common/Dogrulama/GirdiOznitelikleri.cs`, YENİ)
`[Iban] [Telefon] [Eposta] [TarihPenceresi(TarihTuru)] [Tutar(TutarTuru)] [Gram] [Has] [Milyem] [Ayar] [Adet] [Yuzde(n)] [Kur] [GunSayisi(a,b)] [Barkod] [Ad(n)] [Unvan] [Adres] [Pasaport] [Gerekce]` + `TarihAraligi.Denetle` (sorgu parametresi). Hepsi Core kuralını çağırır, mesaj `GirdiSozlugu.Tr`; null/boş geçer (zorunluluk `[Required]`'ın). Kabul örneği: `BankaModulu` `HesapOlusturDto.Iban` / `HesapGuncelleDto.Iban` eski `[RegularExpression]` (kontrol hanesine bakmıyordu) → `[Iban]`; servis yolu da (DataAnnotations yalnız controller yolunda çalıştığı için) `IbanDenetle` ile AYNI Core kuralını çağırır ve IBAN'ı kayıt biçimine (boşluksuz, büyük harf) getirir.

### 1.3 Ekran bileşenleri (`Kuyumcu.Web/Bilesenler/Form/`)
- **YENİ ortak beyin** `KuralliMetinAlani.cs`: maske (Temizle — JS ile aynı, idempotent) · tam kural (çıkışta ve dokunulduktan sonra her tuşta) · yazarken kural (uzunluk dolunca) · alan altı hata · `GecersizAlanKapisi.Bildir(…, mesaj)` → Kaydet kilidi sebebi = alan altı metni BİREBİR.
- **YENİ:** `Iban.razor` (TR öneki, banka/rezerv bölümüne harf yazılamaz, 4'lü grup, 26. karakterde MOD 97-10), `Pasaport.razor`, `BarkodAlani.razor` (EAN-13 yalnız ipucu satırında uyarı), `AdSoyad.razor`, `Unvan.razor`, `Adres.razor`, `AnlamsalSayilar.cs` (`Adet`, `Milyem`, `Yuzde`, `KurAlani`, `GunSayisi`, `Has` — `Sayi` görünümünü + SayisalAlan beynini kullanır).
- **DEĞİŞTİ:** `Telefon.razor` (harf engeli, yapıştırma temizliği, "0532 411 22 33" maskesi, 10 ulusal hane dolunca aralık kuralı; çağırana CUMA-M saklama biçimi `05324112233`) · `TelefonBicimli.razor` artık `<Telefon>`'u sarar (envanter: "birleşir"; ad geriye uyum için korundu) · `Eposta.razor` (boşluk yazılamaz, çıkışta küçük harf + K3) · `Tarih.razor` (yerli `type=date` → metin + gg.aa.yyyy maskesi; pencere dışı yıl yazılamaz; `Tur` parametresi; `EnErken/EnGec` artık UYGULANIYOR; takvim simgesinin üstünde saydam yerli seçici aynı `Bitti` yolundan) · `TarihAralik.razor` (aynı maske, Rapor penceresi, ≤ 5 yıl) · `SayisalAlan.cs` (**EnAz/EnCok artık UYGULANIYOR** — envanter açık #4; `KuralDenetle` kancası; `data-maske/data-eksi/data-tam`) · `Para.razor` (`Tur` = K9 türü) · `Sayi.razor`/`Gram.razor` (maske öznitelikleri) · `GecersizAlanKapisi.cs` (mesajlı `Bildir`; `Sebep` mesajı aynen döndürür).
- **YENİ** `wwwroot/js/girdi-maske.js`: belge düzeyinde tek dinleyici (window, capture): `beforeinput` (maskeden geçmeyen tuş yazılmaz; tarihte pencere dışı yıl öneki engellenir), `paste` (temizlenip yazılır), `input` (maskeli biçim + imleç anlamlı karakter sayısıyla korunur). JS ↔ C# maske paritesi node ile ölçüldü (§ 6.3).
- **YENİ** `wwwroot/css/girdi.css` (yalnız belirteç; takvim simgesi 44 px dokunma hedefi). `index.html`: girdi.css (olcek.css'ten önce) + girdi-maske.js. `img/ikon.svg` + `Ikonlar.Takvim` (lucide calendar).
- **Kabul örneği ekranı** `Pages/Banka.razor`: hesap ekle/düzenle IBAN `<Metin>` → `<Iban>`; `hesap-kaydet` kapıya bağlandı + `#hesap-kilit-sebep`; düzenleme çekmecesi `KaydetAktif/KilitSebebi` kapıdan. Dekont tarihi (`dekont-tarih`, mevcut `EnGec=Bugun`) ortak `<Tarih>` üzerinden yeni davranışı aldı (sayfa kodu değişmedi).

### 1.4 denetim.sh — F3 cırcır sayaçları
`GIRDI-2:` bloğu (python, fail-closed — ölçülemezse ✗): `SAYISAL_OLU_SINIR = 0` · `IBAN_REGEX_DTO = 0` · `TEST_GECERSIZ_FIKSTUR = 0` · `HAM_TARIH_GIRDISI ≤ 7` · `GIRDI_TUR_GENEL_BILESEN ≤ 15` · `DTO_DECIMAL_RANGESIZ ≤ 26` · `EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) ≤ 168` (tur 2: 169 → 168). `HAM_TABLO_ORTAK_GRID_DISI` GRID-1 şeridinin (envanter P2 dosya alanı) — eklenmedi. Betik sayısı sabiti 10 → 11 (girdi-maske.js; gerekçe satırı denetim.sh'ta ve `TasarimBelirtecTestleri`'nde).

### 1.5 Testler ve fikstür düzeltmesi
- **YENİ** `GirdiKuralTestleri.cs` (saf: K1–K3, K7–K21, K30 alt/üst/tam sınır/bir fazla + öznitelik metni = sözlük), `GirdiBilesenTestleri.cs` (bUnit: maske, alan altı hata, kapı sebebi = alan altı metni), `GirdiBankaTestleri.cs` (bUnit Banka ekranı + HTTP: 400 metni, normalleştirme, PATCH, servis yolu), `TestIban.cs` (`Uret()`, `Uret(tohum)`, `Tamamla(bban22)`).
- **Fikstür (envanter D):** IBAN sabit + `"TR"+rastgele` üreticiler (28 dosya) → geçerli IBAN / `TestIban`; TCKN `12345678901 / 12345678902 / 99999999998` → `12345678950 / 10000000214 / 99999999990` (AlanSifreleme, MasakServis, MasakKural); telefon `05001112233` → `05321112233` (PepBeyani); KesifP5 sahte cari "VKN" 11 hane → 10 hane. VKN (algoritma TEYİTSİZ) ve adres (< 10, eşik TEYİTSİZ) fikstürlerine DOKUNULMADI (sayaç K1/K2/K5'i sayar).
- **Davranış değişikliğiyle güncellenen mevcut testler (gerekçe satırı her birinde):** `GirdiAilesiBilesenTestleri.G10` (değer gg.aa.yyyy), `Q91BankaEkranTestleri.K1_dekont…` (type=text + data-maske), `S3fEkranTestleri.Vade_sorgu…`, `FiyatlandirmaUX4aEkranTestleri.Kampanya…` (gg.aa.yyyy), `CumaMMusterilerEkranTestleri.CUMA_M_14` (yazarken maske; yabancı numarada '+' yalnız başta), `UX1C1…Cariler_gecersiz_telefon`, `UX1C4…C4_15` (harf yazılamaz → geçersiz numara rakamla), `VadeEkranTestleri.VadeTakibi_GecersizUfuk` (alan altı hata + TEK bant: `[role=alert]:not(.alan-hata)`), `TasarimBelirtecTestleri` (betik 10 → 11). Hiçbiri gevşetilmedi: her birine yeni davranışın iddiası eklendi.

## 2. Sapmalar ve gerekçeleri
1. **Telefon mobil aralık listesi Core sabitinde, yapılandırmada değil** (envanter K2 "yapılandırmadan okunmalı" diyor). Gerekçe: liste yapılandırmadan okunursa istemci (WASM) ile sunucu ayrışır — paketin temel kuralı "ekran ve sunucu AYNI kural". Liste tek dosyada (TelefonDogrulayici.MobilOnekler), kaynak ITU bildirimi. Yapılandırmaya taşımak istemciye ayrı uç gerektirir → göç/ekip oyu.
2. **Mevcut DTO'lara öznitelik GÖÇÜ yapılmadı** (GOC-A..D'nin işi); yalnız kabul örneği Banka IBAN (`[Iban]`, envanter açık #1). Diğer öznitelikler hazır ve testli, hiçbir DTO'da kullanılmıyor.
3. **K21 adres eşiği (10) ve VKN** TEYİTSİZ: kural Core'da var, hiçbir DTO/ekrana bağlanmadı; adres/VKN fikstürleri değiştirilmedi.
4. **K8d keşide** TEYİTSİZ: pencere = ortak 1900–2100 (bugünkü davranış korunur).
5. **SayisalAlan EnAz/EnCok uygulanırken kapısız formda değer çağırana yine gider** (eski akış; sunucu aynı kuralla reddeder); kapılı formda null gider + Kaydet kilitli. Gerekçe: kapısız formda gitmezse sayfa ESKİ değeri POST eder (görünen ≠ giden).
6. **`EKRAN_SUNUCU_UZUNLUK_FARKI` vekil ölçü**: ekran alanı ↔ DTO özelliği eşlemesi kodda yok; sayaç AlanSinirlari sabitinden okunmayan sayısal `EnFazla/maxlength` sayısını tutar (169, yalnız azalır). Gerçek fark eşlemesi göç paketlerinde.
7. **Tarih takvimi `<button>` değil**: DoD EK md.1 ("her düğme istek atar ya da kilitli") ve `RaporEkranTestleri.IslemDefteri_her_dugme…` — takvim simgesinin üstünde saydam yerli seçici; seçilen gün metin kutusuyla aynı `Bitti()` yolundan geçer.
8. `TarihIleri` metni "Tarih bugünden ileri olamaz." — `EnGec = bugün` veren her ekranda (ör. dekont) aynı cümle.
9. EAN-13 yalnız ipucu satırında uyarı (envanter: "uyarı, engel değil").
10. Dokunulan sayfa (TEK DOKUNUŞ): **Pages/Banka.razor** (kabul örneği). Paylaşılan bileşen değişikliği (Tarih/Telefon/SayisalAlan) sayfa dosyası açmadan 30+ ekrana yansır.

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Testler
- Yeni test: **116** test durumu (GirdiKuralTestleri + GirdiBilesenTestleri + GirdiBankaEkranTestleri + GirdiBankaSunucuTestleri; Theory satırları dâhil).
- Filtreli (son kod): `FullyQualifiedName~Girdi` → **244/244 geçti**; `~Girdi|~TcknDogrulama` 273/273; `~P36|~GirdiAilesi|~TasarimBelirtec|~Kimlik` 299/299.
- Not: TCKN/VKN bileşenleri tam süit tur 2'den SONRA eklendi (yalnız yeni dosyalar + Core/Api eklemesi, hiçbir ekranda kullanılmıyor); ilgili filtreli kümeler yeşil.
- Tam süit tur 1 (ara, eski kod): 10686 · 17 kırmızı — hepsi bu paketin davranış değişikliği; 16'sı mevcut testin yeni sözleşmeye güncellenmesiyle, 1'i (betik sayısı) gerekçeyle kapandı (§1.5).
- **Tam süit tur 2 (son kod): toplam 10801 · çalışan 10799 · geçen 10797 · başarısız 2 · atlanan 2** (trx: /private/tmp/kuyumcu-serit/trx-pGIRDI-AILESI-2). İki kırmızı BU PAKETİN DEĞİL: `MobilPariteCssTestleri.B9_Veri_tablolari_TEK_sarmalayici_kalibini_kullanir` ve `S1iGorselSigma2Testleri.S1i_13_Kirik_hucre_kilitleri_telefon_yetki_denetim` — ikisi de `Pages/Cariler.razor` kaynağında `<KartListe` / `hucre-tek-satir` arıyor; Cariler.razor 03:42'de **GRID-1 şeridi** (t1, `<Izgara>` göçü) tarafından değiştirildi (13 Izgara geçişi). Bu paket Cariler.razor'a dokunmadı. → Paket farkı (yeni kırmızı) = 0.

## 5. Mutasyon kanıtı (33 madde; yedek /private/tmp/kuyumcu-serit/mut-yedek/GIRDI-AILESI-2, her biri perl/py tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Dosya / değişiklik | Kırmızı (ADIYLA, ilk 3) | cmp |
|---|---|---|---|
| M1-1 | `IbanDogrulayici.cs` | 6 — GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok; GirdiBankaEkranTestleri.Banka_duzenleme_cekmecesi_de_ortak_Iban_ve_kilit | AYNI |
| M1-2 | `IbanDogrulayici.cs` | 1 — GirdiKuralTestleri.K1_Iban_banka_kodunda_ya_da_rezervde_harf_IbanBicim | AYNI |
| M2-1 | `TelefonDogrulayici.cs` | 4 — GirdiBilesenTestleri.TelefonBicimli_artik_Telefon_ile_ayni_bilesen; GirdiBilesenTestleri.Telefon_10_hane_dolunca_aralik_hatasi_ve_kilit; GirdiKuralTestleri.K2_Telefon_gecersiz(tel: | AYNI |
| M2-2 | `TelefonDogrulayici.cs` | 1 — GirdiKuralTestleri.K2_Telefon_gecersiz(tel: | AYNI |
| M3-1 | `EpostaDogrulayici.cs` | 3 — GirdiBilesenTestleri.Eposta_bosluk_yazilmaz_cikista_kucuk_harf_ve_kural; GirdiKuralTestleri.K3_Eposta_bicim_hatasi(e:; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M8-1 | `TarihPenceresi.cs` | 3 — GirdiBilesenTestleri.Tarih_08102026_yazilinca_08_10_2026_olur_ve_deger_gider; GirdiBilesenTestleri.Tarih_olmayan_gun_ve_pencere_disi_yil_hata_ve_kilit; GirdiKuralTestleri.K8_Serbest_pencere_1900_2100_sinirlari | AYNI |
| M8-2 | `TarihPenceresi.cs` | 2 — GirdiBilesenTestleri.Tarih_Islem_turunde_ileri_tarih_ve_Dogum_turunde_18_yas; GirdiKuralTestleri.K8b_Dogum_18_yas_TMK_md11_ve_120_yil | AYNI |
| M8-3 | `TarihPenceresi.cs` | 3 — GirdiBilesenTestleri.Tarih_Islem_turunde_ileri_tarih_ve_Dogum_turunde_18_yas; GirdiBilesenTestleri.Tarih_takvim_yerli_secici_ayni_yoldan_ve_dugme_degil; GirdiKuralTestleri.K8a_Islem_ileri_tarih_yok_geri_bir_yil | AYNI |
| M8-4 | `TarihPenceresi.cs` | 1 — GirdiKuralTestleri.K8e_Aralik_ters_ve_5_yil | AYNI |
| M8-5 | `TarihPenceresi.cs` | 2 — GirdiBilesenTestleri.Tarih_olmayan_gun_ve_pencere_disi_yil_hata_ve_kilit; GirdiKuralTestleri.K8_Tarih_coz_gecersiz(ham: | AYNI |
| M9-1 | `SayiKurallari.cs` | 2 — GirdiBilesenTestleri.Para_Tur_Pozitif_sifir_ve_ondalik_hatasi; GirdiKuralTestleri.K9_Tutar_pozitif_sinirlar | AYNI |
| M9-2 | `SayiKurallari.cs` | 2 — GirdiKuralTestleri.K9_Tutar_pozitif_sinirlar; GirdiKuralTestleri.K9a_K9b_Tutar_sifir_olabilir_ve_isaretli | AYNI |
| M10-1 | `SayiKurallari.cs` | 1 — GirdiKuralTestleri.K10_Gram_ve_tas_brutten_kucuk | AYNI |
| M11-1 | `SayiKurallari.cs` | 3 — GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K11_Has_6_ondalik; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M12-1 | `SayiKurallari.cs` | 3 — GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K12_Milyem_1_1000_ve_sapma_uyarisi; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M13-1 | `SayiKurallari.cs` | 2 — GirdiKuralTestleri.K13_Ayar_yalniz_liste; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M14-1 | `SayiKurallari.cs` | 2 — GirdiKuralTestleri.K14_Adet_tam_sayi_en_az_1; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M15-1 | `SayiKurallari.cs` | 3 — GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K15_Yuzde_ve_marj; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M16-1 | `SayiKurallari.cs` | 1 — GirdiKuralTestleri.K16_Kur_pozitif_6_ondalik | AYNI |
| M17-1 | `SayiKurallari.cs` | 3 — GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiKuralTestleri.K17_Gun_sayisi_tam_ve_aralik; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M18-1 | `MetinKurallari.cs` | 2 — GirdiKuralTestleri.K18_Barkod_ascii_ve_ean13_uyarisi; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M19-1 | `MetinKurallari.cs` | 3 — GirdiBilesenTestleri.Pasaport_Barkod_AdSoyad_Adres_maske_ve_kural; GirdiKuralTestleri.K19_K20_Ad_ve_unvan; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M21-1 | `MetinKurallari.cs` | 3 — GirdiBilesenTestleri.Pasaport_Barkod_AdSoyad_Adres_maske_ve_kural; GirdiKuralTestleri.K21_Adres_10_500; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| M7-1 | `MetinKurallari.cs` | 1 — GirdiKuralTestleri.K7_Pasaport | AYNI |
| M30-1 | `GirdiHatasi.cs` | 2 — GirdiKuralTestleri.K30_Gerekce_20_1000; GirdiKuralTestleri.Oznitelik_her_kural_400_metni_sozlukle_ayni | AYNI |
| MW-1 | `SayisalAlan.cs` | 5 — GirdiBilesenTestleri.Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular; GirdiBilesenTestleri.Para_Tur_Pozitif_sifir_ve_ondalik_hatasi; GirdiBilesenTestleri.Sayi_EnAz_0_eksi_hata_kilit_ve_null | AYNI |
| MW-2 | `Iban.razor` | 4 — GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok; GirdiBankaEkranTestleri.Banka_duzenleme_cekmecesi_de_ortak_Iban_ve_kilit | AYNI |
| MW-3 | `Tarih.razor` | 5 — GirdiBilesenTestleri.Tarih_08102026_yazilinca_08_10_2026_olur_ve_deger_gider; GirdiBilesenTestleri.Tarih_eksik_yazim_cikista_bicim_hatasi; GirdiBilesenTestleri.Tarih_makine_bicimi_change_geri_uyum | AYNI |
| MW-4 | `GecersizAlanKapisi.cs` | 6 — GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok; GirdiBankaEkranTestleri.Banka_duzenleme_cekmecesi_de_ortak_Iban_ve_kilit; GirdiBilesenTestleri.Eposta_bosluk_yazilmaz_cikista_kucuk_harf_ve_kural | AYNI |
| MW-5 | `Telefon.razor` | 4 — GirdiBilesenTestleri.TelefonBicimli_artik_Telefon_ile_ayni_bilesen; GirdiBilesenTestleri.Telefon_harf_yazilmaz_yapistirma_temizlenir_ve_gruplanir; UX1C1EkranTestleri.Cariler_gecersiz_telefon_ALAN_ALTI_mesaj_verir_ve_POST_ATILMAZ | AYNI |
| MW-6 | `Banka.razor` | 2 — GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok | AYNI |
| MA-1 | `BankaModulu.cs` | 4 — GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok; GirdiBankaSunucuTestleri.Banka_POST_bosluklu_kucuk_harf_ve_harfli_hesap_no_gecer_normallestirilir | AYNI |
| MA-2 | `GirdiOznitelikleri.cs` | 4 — GirdiBankaEkranTestleri.Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar; GirdiBankaEkranTestleri.Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok; GirdiBankaSunucuTestleri.Banka_PATCH_mod97_bozuk_IBAN_400_eski_deger_korunur | AYNI |

| MK-1 | `MetinKurallari.cs` (KimlikKurali: TcknGecersiz → null) | 2 — GirdiBilesenTestleri.TcKimlikNo_harf_yazilmaz_11_hanede_algoritma_ve_kilit; GirdiKuralTestleri.K5_K4_Kimlik_koprusu_KimlikNoDogrulayici_sonucunu_cevirir | AYNI |

Not: MA-1/MA-2 satırlarında görünen GirdiBankaEkran (bUnit) kırmızıları, bir önceki MW-6 mutasyonunun geri kopyasının eski mtime'ı yüzünden Web derlemesinin güncellenmemesindendir (shutil.copy2 mtime korur); bu iki maddenin KANITI sunucu testleridir (`Banka_servis_yolu_da_Core_kuralini_cagirir`, `Banka_PATCH_mod97_bozuk_IBAN_400_eski_deger_korunur`). Bu fark fark edildi: tüm dosyalara `touch` + temiz `dotnet build Kuyumcu.sln` → 0 uyarı 0 hata → `~Girdi` 242/242 yeşil; tam süit tur 2 bu derlemeyle koşuldu.

**Sayaç mutasyonları (denetim.sh GIRDI-2 betiğiyle aynı python):** D1 PepBeyani'ye `05001112233` geri → `TEST_GECERSIZ_FIKSTUR` 0→1 · D2 SayisalAlan `KuralDenetle => null` → `SAYISAL_OLU_SINIR` 0→104 · D3 BankaModulu'na `RegularExpression(@"^TR\d{24}$")` → `IBAN_REGEX_DTO` 0→1; üçü de geri alındı, cmp AYNI, sayaçlar 0.

## 6. Canlı doğrulama (API 5188 · vekil 5189 · başsız Chrome CDP 9981–9984)
### 6.1 API + SQL
| İstek | Beklenen | Gerçek |
|---|---|---|
| POST /bankalar iban=TR340006100519786457841326 (mod-97 bozuk) | 400 + sözlük metni | **400** `alanlar.iban = "IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin."` |
| POST /bankalar iban=TR000000000000000000000001 (eski regex kabul ediyordu) | 400 | **400** aynı metin |
| POST /bankalar iban="tr33 0006 1005 1978 6457 8413 26" | 201/200 + normalleşmiş | **200**, yanıt `iban":"TR330006100519786457841326"` |
| PATCH /bankalar/{id} iban=…327 | 400, kayıt değişmez | **400** aynı metin |
SQL: `select id,iban from banka_hesaplari where tenant_id=<test>` → 1 satır, `TR330006100519786457841326` (PATCH sonrası değişmedi; 400'lü POST'lar satır yazmadı).

### 6.2 Gerçek tarayıcı — 2 genişlik × 2 mod × 2 tema × 10 senaryo = 80 ölçüm, 48 ekran görüntüsü
Tuşlar CDP `Input.dispatchKeyEvent` ile TEK TEK basıldı (beforeinput gerçek); yapıştırma `ClipboardEvent('paste')`. Ölçer: betikler/sikisma-olc.js (`__olc`).
| Mod | Tema | Gen. | Senaryo | Kutudaki değer | Alan altı hata | Kaydet kilitli | Sebep = alan altı | K1 | K3 taşan/kırık | K5 TRY | Yatay | K9* |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASIT | aydinlik | 1280 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | aydinlik | 1280 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 4 |
| BASIT | aydinlik | 1280 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 2 |
| BASIT | aydinlik | 1280 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 1280 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 4 |
| BASIT | aydinlik | 1280 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | aydinlik | 375 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | aydinlik | 375 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | aydinlik | 375 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| BASIT | aydinlik | 375 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 5 |
| BASIT | aydinlik | 375 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| BASIT | karanlik | 1280 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 4 |
| BASIT | karanlik | 1280 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 2 |
| BASIT | karanlik | 1280 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 1280 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 4 |
| BASIT | karanlik | 1280 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | karanlik | 375 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | karanlik | 375 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| BASIT | karanlik | 375 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| BASIT | karanlik | 375 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 5 |
| BASIT | karanlik | 375 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| PRO | aydinlik | 1280 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 4 |
| PRO | aydinlik | 1280 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 2 |
| PRO | aydinlik | 1280 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 1280 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 0 |
| PRO | aydinlik | 1280 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| PRO | aydinlik | 375 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 6 |
| PRO | aydinlik | 375 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| PRO | aydinlik | 375 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| PRO | aydinlik | 375 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| PRO | aydinlik | 375 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| PRO | karanlik | 1280 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 4 |
| PRO | karanlik | 1280 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 2 |
| PRO | karanlik | 1280 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 1280 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 0 |
| PRO | karanlik | 1280 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | iban-yazim | `TR34 0006 1005 1978 6457 8413 26` | IBAN kontrol hanesi tutmuyor. Numarayı tekrar kontrol edin. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| PRO | karanlik | 375 | iban-yapistir | `TR33 0006 1005 1978 6457 8413 26` | — | hayır | — | 0 | 0/0 | 0 | 0 | 6 |
| PRO | karanlik | 375 | tarih-08102026 | `08.10.2026` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | tarih-3000 | `01.01` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | tarih-1899 | `01.01.199` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | tarih-31-02 | `31.02.2026` | Takvimde böyle bir gün yok. | evet | AYNI | 0 | 0/0 | 0 | 0 | 6 |
| PRO | karanlik | 375 | tarih-yapistir-iso | `01.10.2026` | — | hayır | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | telefon-harf | `0532 411 22 33` | — | — | — | — | —/— | — | — | — |
| PRO | karanlik | 375 | telefon-500 | `0500 111 22 33` | Bu numara Türkiye'de kullanılan bir telefon numarası değil. | evet | AYNI | 0 | 0/0 | 0 | 0 | 3 |
| PRO | karanlik | 375 | telefon-yapistir | `0532 411 22 33` | — | hayır | — | — | —/— | — | — | — |

\* K9 "binen": tüm çiftler modal/çekmece içeriği ⟂ katmanın ALTINDAKİ sayfa öğesi (ör. `p.stat-deger "0 ₺" ⟂ span.alan-etiket-metin "Banka Adı"`, `#cari-sayac-metin ⟂ #alan-hata-cari-telefon`) — üst katman ile arka sayfa arasındaki geometrik kesişim, gerçek binme değil (ekran görüntülerinde görülür). Taşan/kırık/TRY/yatay/[hidden] = 0 her satırda.

Görülen ekran görüntüleri (Read ile): `iban-hata__BASIT_aydinlik_1280.png` (IBAN alan altı hata + Kaydet soluk + altta aynı metin), `iban-hata__PRO_karanlik_375.png`, `tarih-hata__PRO_karanlik_375.png` (31.02.2026 → "Takvimde böyle bir gün yok." alan altı ve Kaydet üstü aynı), `tarih-gecerli__BASIT_aydinlik_1280.png` (01.10.2026 + takvim simgesi), `telefon-hata__PRO_aydinlik_1280.png` (0500 111 22 33 → aralık hatası). Tümü: /private/tmp/kuyumcu-serit/GIRDI-AILESI-2/canli/*.png (48).
Kabul 2 maddeleri: harf tuşu yazılmaz (IBAN "abc", tarih "a", telefon "abc" → kutuda yok; `data-maske-engel` izi) ✓ · yapıştırma temizlenir ✓ · 08102026 → 08.10.2026 ✓ · 3000 yazılamaz (01.01'de kalır) ✓ · 1899 yazılamaz (8 tuşu engellendi → 01.01.199) ✓ · IBAN 4'lü grup + 26. karakterde mod-97 hatası ✓ · Kaydet kilidi sebebi alan altı metniyle BİREBİR (24/24 AYNI) ✓.

### 6.3 JS ↔ C# maske paritesi (node, girdi-maske.js `maskele`)
13 örnek, bUnit'in C# `Maskele` ile doğruladığı çıktılarla birebir: `330006100519→TR33 0006 1005 19`, `tr33ABC0006→TR33 0006`, `0532abc4112233→0532 411 22 33`, `+90 (532) 411-22-33→+90 532 411 22 33`, `08102026→08.10.2026`, `2026-05-01→01.05.2026`, `u 12-345 678→U12345678`, ` kçay 12→KAY12`, `  Ayşe   Nur→Ayşe Nur` …

### 6.4 Test kiracısı
`PAKETGIRDI-AILESI-2-TEST` (01M4CDA6XKGS8Y65TRMRWJ9TNH) `zsh kiraci-temizle.sh … --uygula` ile silindi (252 satır + tenants 1); `kiraci_davetleri` satırı silindi (1). tenants: başlangıç **18** → son **18**. 5188/5189 dinleyen yok (lsof boş). Davet için API kendi şerit anahtarıyla (Sistem__Anahtari, rastgele, dosyada chmod 600, ekrana basılmadı) açıldı.

## 7. denetim.sh
Son koşum (`bash dotnet/denetim.sh`, TCKN/VKN bileşenleri eklendikten sonra): **tek ✗ = P-30 DEMO_INSAN_SURESI** (görev metni gereği hariç, kurucu kalemi). GIRDI-2: SAYISAL_OLU_SINIR 0 ✓ · IBAN_REGEX_DTO 0 ✓ · TEST_GECERSIZ_FIKSTUR 0 ✓ · HAM_TARIH_GIRDISI 7/7 ✓ · GIRDI_TUR_GENEL_BILESEN 15/15 ✓ · DTO_DECIMAL_RANGESIZ 26/26 ✓ · EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) ≤ 169 ✓ · betik sayısı 11 ✓.
Ara koşumda (03:4x) vekil sayaç 170 > 169 vermişti: artış `Bilesenler/MasakFormu.razor` içinde başka bir şeridin 03:37'de eklediği `masak-adres … EnFazla="500"` satırıydı (bu paket MasakFormu'na dokunmadı); son koşumda o şerit satırı giderdiği için sayaç tavanda. Cırcırın başka şeridin artışını yakaladığı böylece canlı görüldü.

## 8. DoD sözleşmesi
Yeni varlık/kayıt türü YOK (yalnız kural, bileşen, öznitelik). Banka hesabı mevcut PATCH/DELETE (pasife alma) uçları ve ekrandaki Düzenle/Pasife al eylemleri aynen; IBAN düzenleme çekmecesi de ortak `<Iban>` + kapı kilidi taşır. Yeni düğme yok (takvim simgesi düğme değil; işlevsiz düğme 0).

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Kabul 1 — K1–K3, K7–K21 Core'da tek kural + sınır testleri | KARŞILANDI | GirdiKuralTestleri (alt/üst/tam sınır/bir fazla) |
| Kabul 2 — gerçek tarayıcı 1280+375 · Basit+Pro · açık+koyu | KARŞILANDI | §6.2, 80 ölçüm, 48 görüntü |
| Kabul 3 — sunucu aynı kuralla 400, aynı sözlük metni; mutasyon | KARŞILANDI | §6.1, Oznitelik_her_kural_400…, M/MW/MA tablosu |
| Kabul 4 — SAYISAL_OLU_SINIR 30 → 0 | KARŞILANDI | denetim ✓, D2 mutasyonu 0→104 |
| Kabul 5 — TEST_GECERSIZ_FIKSTUR açıldı, D listesi (K1/K2/K5) 0 | KARŞILANDI (tur 2'de düzeltildi: tur 1'de PosYardim `$"TR{…}"` kör noktadaydı) | denetim ✓ (yeni desen), PosYardim → TestIban.Uret, sayaç mutasyonu 0→1 (§0.1 md.1) |
| Kabul 6 — test 0 yeni kırmızı, denetim TEMİZ (P-30 hariç) | KARŞILANDI | tur 2: 2 kırmızı GRID-1'in (Cariler.razor); son denetim: yalnız P-30 ✗ |
| Build 0/0 | KARŞILANDI | son `dotnet build Kuyumcu.sln`: 0 Uyarı 0 Hata; build8: 0 Uyarı 0 Hata (Tests projesinin önceden var olan 4 analizör uyarısı — SUBEKAPSAMASinir:49, KesifP2:455, SonKalanlar:584/813 — bu paketin satırları değil) |
| Mevzuat birincil kaynak | KARŞILANDI | TCMB 2008/6 (RG), TMK md.11, ITU E.164 TR; TEYİTSİZ'ler (K8d, K21, VKN) bugünkü davranışla |
| TCKN-DOGRULAMA doğrulayıcısı KULLANILDI, yeniden yazılmadı; TcKimlikNo/Vkn bileşenleri onu sarar | KARŞILANDI | `KimlikKurali` köprüsü (MetinKurallari.cs) → `KimlikNoDogrulayici.TcknDenetle/TcknVeyaVknDenetle/VknBicimGecerli`; bileşenler `TcKimlikNo.razor`, `Vkn.razor`, `TcknVeyaVkn.razor`; öznitelikler `[Tckn] [Vkn] [TcknVeyaVkn]`; testler K5_K4_Kimlik_koprusu…, TcKimlikNo_harf_yazilmaz…; mutasyon MK-1 (köprüde TcknGecersiz → null) → 2 adı yazılı test kırmızı, cmp AYNI |

## 10. Açık kalanlar
- (Tur 4) Tam süitte başkasına ait kırmızı: `Toplu5GunSonuTestleri.T5_PRO_gun_sonu_durumu_cumle_duzeninde_formul_ipucunda` — sahibi `Pages/Raporlar.razor` / `Sozlukler.KarRapor.cs` üzerinde 12:24'te çalışan şerit; `SATIS3EkranTestleri.Barkod_dizisi_…` kararsız (filtreli yeşil); `UrunAcilisAtomikEkranTestleri.E2_…` kararsız (filtreli yeşil).
- (Tur 4) Kasa hareketi ve cari tahsilat uçları 0,005'i hâlâ 0,01'e yuvarlıyor (SK12'de belgeli) — K9'a iniş GOC paketleri.
- (Tur 4) /kur (kur geçmişi) ve /fiyatlandirma (kampanya formu) tarih kutuları ayrı eylemle açıldığı için tarayıcıda ölçülmedi; aynı `<Tarih>` + aynı CSS.
- (Tur 4) `sikisma-olc.js` K9 süzgeç satırında tarih kutusunun yan alanın takvim simgesi altına girmesini yakalamadı (yalnız yaprak metin öğelerini karşılaştırıyor; input/düğme değil). Ölçer betikler/ altında, bu paketin dosya alanı dışında → orkestratöre öneri: K9'a girdi/düğme dikdörtgenleri eklensin.
- (Tur 4) İşletme kimlik çekmecesindeki "Şirketse 10 haneli vergi no …" ve alt bilgi satırları sayfanın kendi metni (Vergi no alanı henüz `<Metin>`; `<TcknVeyaVkn>` göçü GOC-B).
- `TcKimlikNo` / `Vkn` / `TcknVeyaVkn` bileşenleri hazır ama hiçbir ekranda kullanılmıyor (13 kimlik/IBAN `Metin` alanının göçü GOC-B'nin; bu bileşenler canlı tarayıcıda ayrıca ölçülmedi — ekrana bağlandıkları göç paketinde ölçülür; maskesi (`data-maske="rakam"`) IBAN/telefon ile aynı JS yolundan geçer).
- `GerekceAlani` → `Gerekce` taşınması ve `Ayar` sarmalayıcısı yapılmadı → GOC-C'ye devredildi (§0.2 md.2). `Gram Tur=` 17 mevcut kullanıma bağlanmadı (GOC-C).
- Sunucuda kısa yabancı IBAN `IbanBicim` ("IBAN TR ile başlar…") metniyle reddediliyor — yabancı numara için yanıltıcı (DÜŞÜK, §0.2 md.1).
- Tam süit tur 2: tek kırmızı `SATIS2EkranTestleri.PRO_ust_satir_…_tiklaninca_acilir` — kararsız (filtreli 3/3 yeşil), sahibi SATIS-2 ekranı; bu paketin değil.
- `dotnet/Kuyumcu.Tests/ZzMutFikstur.cs` (0 bayt) — kurucu silsin (§0.2 md.6).
- Göç: 15 anlamsal alan hâlâ genel bileşende, 7 ham `type=date`, 26 Range'siz decimal DTO, 169 sabitsiz uzunluk → GOC-A..D.
- Mevcut sayfa içi telefon kuralı (`Iletisim.TelefonGecerli`, 10–13 hane) ile yeni K2 kuralı ekranda yan yana; tek kurala inmesi GOC-B/D.
- Milyem ±50 sapma uyarısı Core'da (`MilyemKurali.SapmaVar`), bileşende henüz gösterilmiyor.
- (Tur 1) Tam süitte başkasına ait 2 kırmızı: `MobilPariteCssTestleri.B9_…` ve `S1iGorselSigma2Testleri.S1i_13_…` — sahibi GRID-1; tur 2 tam süitinde ikisi de yeşil.

## 11. Kurucuya / mali müşavire
- **K8d keşide tarihi:** ileri tarihli çek (5941 s. Çek Kanunu) birincil metinden doğrulanmadı → bugünkü davranış. Hukuk/mali müşavir teyidi.
- **VKN kontrol hanesi:** GİB birincil kaynağı yok → yalnız 10 hane (TEYİTSİZ).
- **Yabancı IBAN ülke uzunlukları** (SWIFT ISO 13616 kaydı) birincil kaynaktan alınamadı → ülke uzunluğuna göre denetim yok, fail-closed (tam kural geçene dek kilit).
- **Telefon 444 + 4 hane**: BTK birincil belgesi bulunamadı; envanter K2'ye dayanır (vergi/mevzuat hükmü değil).
- **K21 adres ≥ 10 karakter** ve **18 yaş altı müşteri** (yasal temsilci uyarısı mı engel mi) — ekip oyu.

## 12. Değişen / yeni dosyalar (bu paket)
TUR 3+4: YENİ `dotnet/Kuyumcu.Tests/GirdiAilesi2Tur3Testleri.cs` (tur 4'te `GirdiAilesi2Tur4ServisTestleri` sınıfı + CSS testi genişletmesi) · DEĞİŞEN `Kuyumcu.Core/Domain/Girdi/{GirdiHatasi,IbanDogrulayici}.cs`, `Kuyumcu.Core/Domain/Sozlesmeler.cs`, `Kuyumcu.Core/I18n/Sozlukler.Girdi.cs`, `Kuyumcu.Api/Modules/{Admin/AdminModulu,Sube/SubeModulu,Cari/CariModulu,Banka/BankaModulu}.cs`, `Kuyumcu.Web/Bilesenler/Form/{Telefon,Tarih,TarihAralik,Para,Iban}.razor`, `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs`, `Kuyumcu.Web/Servisler/TelefonBicimi.cs`, `Kuyumcu.Web/Pages/Isletme.razor`, `Kuyumcu.Web/wwwroot/css/girdi.css` (tur 4: sarmalayıcı kuralı), `Kuyumcu.Web/wwwroot/js/girdi-maske.js` · Testler (sözleşme güncellemesi, gerekçeli): `Dogrulayici/{ORTAK1SinirTestleri,SATIS3SinirTestleri,TM1SinirTestleri,Q105SinirTestleri,MASAK370SinirTestleri}.cs`, `SonKalanlarTestleri.cs`, `UX1C4EkranTestleri.cs`, `GirdiAilesiBilesenTestleri.cs`.

TUR 2: YENİ `dotnet/Kuyumcu.Tests/GirdiAilesi2Tur2Testleri.cs` · DEĞİŞEN `Kuyumcu.Core/Domain/Girdi/{GirdiHatasi,TelefonDogrulayici,SayiKurallari,MetinKurallari}.cs`, `Kuyumcu.Core/I18n/Sozlukler.Girdi.cs`, `Kuyumcu.Web/Bilesenler/Form/{Iban,Gram,Telefon,SayiKapisiSebebi}.razor`, `Kuyumcu.Web/wwwroot/js/girdi-maske.js`, `Kuyumcu.Api/Modules/Banka/BankaModulu.cs`, `dotnet/denetim.sh`, `Kuyumcu.Tests/{PosYardim,GirdiBankaTestleri}.cs` · boş artık `Kuyumcu.Tests/ZzMutFikstur.cs`.

YENİ: dotnet/Kuyumcu.Core/Domain/Girdi/{GirdiHatasi,IbanDogrulayici,TelefonDogrulayici,EpostaDogrulayici,TarihPenceresi,SayiKurallari,MetinKurallari}.cs · dotnet/Kuyumcu.Core/I18n/Sozlukler.Girdi.cs · dotnet/Kuyumcu.Api/Common/Dogrulama/GirdiOznitelikleri.cs · dotnet/Kuyumcu.Web/Bilesenler/Form/{KuralliMetinAlani.cs,AnlamsalSayilar.cs,Iban.razor,TcKimlikNo.razor,Vkn.razor,TcknVeyaVkn.razor,Pasaport.razor,BarkodAlani.razor,AdSoyad.razor,Unvan.razor,Adres.razor} · dotnet/Kuyumcu.Web/wwwroot/js/girdi-maske.js · dotnet/Kuyumcu.Web/wwwroot/css/girdi.css · dotnet/Kuyumcu.Tests/{GirdiKuralTestleri,GirdiBilesenTestleri,GirdiBankaTestleri,TestIban}.cs
DEĞİŞEN: Kuyumcu.Web/Bilesenler/Form/{Telefon,TelefonBicimli,Eposta,Tarih,TarihAralik,Para,Sayi,Gram}.razor, SayisalAlan.cs, GecersizAlanKapisi.cs · Kuyumcu.Web/Bilesenler/Ikonlar.cs · Kuyumcu.Web/wwwroot/{index.html,img/ikon.svg} · Kuyumcu.Web/Pages/Banka.razor · Kuyumcu.Api/Modules/Banka/BankaModulu.cs · dotnet/denetim.sh · Testler (fikstür/sözleşme): AlanSifreleme, MasakServis, MasakKural, PepBeyani, KesifP5, EkstreIceAktarma ve IBAN üreticili 27 dosya (§1.5), GirdiAilesiBilesen, Q91BankaEkran, S3fEkran, FiyatlandirmaUX4aEkran, CumaMMusteriler, UX1C1Ekran, UX1C4Ekran, VadeEkran, TasarimBelirtec.
