# PAKET KESIF-P4 · ÜRÜN — uygulama raporu (30.09.2026, t5 şeridi · 5166/5167 · CDP 9505-9507)

# TUR 2 (01.10.2026, 00:15–01:00) — önceki doğrulamada kalan 6 bulgunun kapanışı

Tur 1 raporu aşağıda (§ 1–14) değiştirilmeden duruyor; bu bölüm tur 2'yi anlatır ve bağlayıcıdır.

## T2-1. Bulgu → yapılan → kanıt

| Bulgu (tasarım hakemi) | Yapılan | Tarayıcı kanıtı (16 kombinasyon: 1440/1280/960/375 × BASİT/PRO × açık/koyu) |
|---|---|---|
| [KRİTİK] #16 dosya kutusu 375'te çekmeceden 55 px taşıyor, tarayıcının ham "Dosya Seç / Dosya seçilmedi" kontrolü | `BelgeYukleyici.razor`: `InputFile#belge-sec` görsel olarak gizli (`.gorunmez` — ekran okuyucu ve klavye için erişilebilir kalır); görünen yüz `label#belge-sec-dugme.btn.btn-ikincil` ("Dosya seç", tam genişlik) + altında `p#belge-sec-ad` ("Henüz dosya seçilmedi" / "ad — KB"). Eski ayrı önizleme satırı bu satıra katıldı. | 16/16: ham girdi görünmez; düğme 375'te x=30..346 (pencere 375, taşma 0), 1440'ta 990..1411; ad satırı "Henüz dosya seçilmedi"; pencere dışı öğe 0. Görüntü `duzenle_PRO_karanlik_1440_belge2.png`, `duzenle_BASIT_aydinlik_375_belge2.png` |
| [KRİTİK] #15 Tedarikçi değeri 2-3 satıra kırılıyor, metin kenara değiyor (K3) | Seçici bileşeni ve ortak css P7'nin; **sayfa tarafı geçici çözüm** (hakemin önerdiği "kısa ad"): dört cari seçicisinin (u/d-tedarikci, u/d-kons-cari) seçenekleri TEK diziden (`CariSecenekleri`) gelir; seçenek ADI kısa unvan (`KisaUnvan`: "Anonim Şirketi"→"A.Ş.", "Limited Şirketi"→"Ltd. Şti.", "Sanayi ve Ticaret"→"San. ve Tic."; 26 karakteri aşarsa sözcük sınırında "…"), TAM unvan seçenek listesinde alt bilgi olarak görünür ve aranır. Değer (cari kimliği) değişmedi. | 16/16: "Anadolu Kapalıçarşı…" tek satır, `scrollHeight ≤ clientHeight`, kutu 44 px. Görüntü `duzenle_BASIT_aydinlik_1440_tedarik2.png`, `duzenle_BASIT_aydinlik_375_tedarik2.png`. Canlı: PATCH gövdesinde `tedarikciId` kimlik (200) |
| [KRİTİK] #37 ipucunda ham kod ("CEYREK_YENI, RESAT…"), etiket "Parite kodu" | Etiket ve ipucu `Sozlukler.KesifP4.cs`'e taşındı: "Altın cinsi" / "Çeyrek, yarım, Reşat gibi sikke ve ziynetlerde fiyat bu cinsin kurundan gelir. Boş bırakılırsa fiyat gram ve işçilikten hesaplanır." (EN karşılığı var). Hem Yeni Ürün hem Düzenle. | 16/16: `label[for=d-parite]`/`[for=u-parite]` = "Altın cinsi"; ipucunda `[A-Z]{3,}_…` kalıbı 0 |
| [KRİTİK] #14 375'te reyon tablosunda ADET ekran dışında, uzun reyon adı kesik, üstünde "Tablo yatay kaydırılabilir…" | Reyon bölümündeki iki sütunlu `veri-tablosu` (375'te `min-width:max-content` yüzünden genişliyordu) mevcut `panel-liste` / `panel-liste-satir--is` (ad + tutar) liste satırına çevrildi: ad kalan yere SARAR, adet sağda `nowrap`. KartListe sarmalayıcısı bu bölümden kalktı (tablo yok → yatay kaydırma ipucu yok). `#urun-tas-liste` ve `#yerlesim-atanmamis-{sube}` kimlikleri korundu. | 16/16: adet sütunu görünür (375'te sağ kenar 330 < 375), uzun ad 375'te 2 satıra sarar, 1440'ta 1 satır; bölümde tablo 0, yatay kaydırma 0, kaydırma ipucu 0. Görüntü `stok_PRO_karanlik_375_reyonliste.png`, `stok_BASIT_aydinlik_375_reyonliste.png` |
| [KRİTİK · KAPSAM DIŞI] ad araması + Enter → "Bu barkodla aktif ürün bulunamadı" (3 eşleşme görünürken) | `Urunler.razor BarkodBul`: (a) boşluk içeren girdi barkod ucuna HİÇ gitmez (`BarkodBicimindeMi`); (b) 404'te bant yalnız liste BOŞSA çıkar; (c) 404 dışı hata gerçek mesajıyla çıkar (UX-S28). Gerçek barkod yine kartı açar. | 16/16: "Burma" → bant 0 (1 kalem), "Burma Bilezik" → bant 0, "zzzqqq" → bant 1 (liste boş). Canlı: `GET /urunler/barkod/Burma` 404, `GET /urunler?q=Burma` 200 [P4-BLZ] |
| [KRİTİK · KAPSAM DIŞI] RFID ipucunda "K-21" ve "DIŞINDADIR" | **Yapılmadı — P8'e devir.** Önce sayfada P4 metniyle değiştirildi, ancak `denetim.sh` **P-10: K21_DONANIM_BEYANI** kuralı HUB metninde "K-21" geçmesini ve `u-rfid`'in `Dil.T.Urun.RfidIpucu` taşımasını ŞART koşuyor (✗ verdi) ve `P10EkranTestleri.RFID_alaninin_altinda_DONANIM_KAPSAM_DISI_beyani_yazar` "K-21" arıyor. Kural ve test başkasının; değişiklik geri alındı (denetim ✓). | Ölçer hamKod = 4 (her kombinasyonda "K-21" ×2, "DIŞINDADIR" ×2 — Yeni + Düzenle). Sahibi KESIF-P8: HUB metni + denetim P-10 kuralı + P10 testi birlikte değişmeli |

## T2-2. Sapmalar ve gerekçeler

1. **#15 geçici çözüm sayfa tarafında.** Kalıcı çözüm (seçici değeri tek satır + ellipsis + title, dikey dolgu) `Secici.razor` / `ortak.css` (P7) işidir; P7'nin bugünkü ilkesi "seçici değeri KESİLMEZ, sarar" (ortak.css P7-K10). Sayfada kısa unvan + listede tam unvan bu ilkeyi bozmadan K3'ü kapatır. İstek satırı aşağıda (sahibi: KESIF-P7).
2. **RFID metni geri alındı** (üstte). Hakemin bulgusu zaten "Sahibi: KESIF-P8" diyordu.
3. **Kapsam ölçerinde K3 yöntemi düzeltildi:** tur 1'in `olc-p4.mjs` kapsam K3'ü yüksekliğe bakıyordu; 44 px'lik düğme-etiketi (`label.btn`) ve "(isteğe bağlı)" alt yazılı etiketleri yanlış "kırık" sayıyordu. `olc-p4b.mjs` resmi `sikisma-olc.js` ile aynı metin-satırı sayımını kullanır. Resmî ölçer K3 = [0,0] her adımda.
4. **K12-ezik `#belge-sec w=1`:** ekstra.js görsel olarak gizli (clip, 1×1 px) erişilebilir girdiyi "ezik kutu" sayıyor (opacity/clip'i dışlamıyor). Görüntüde kutu yok. `hidden` (display:none) yapılırsa ölçer susar ama klavye kullanıcısı dosya seçemez; erişilebilirlik tercih edildi. Ölçüm aracına istek: `clip`/`.gorunmez` öğeleri K12'den dışlansın.
5. **K9 = 6 (sayfa geneli) modal/çekmece açıkken** tur 1'deki gibi katmanlar arası (arka plan süzgeç etiketleri ⟂ önde öğeler); kapsam K9 (çekmece/modal içi) 0.

## T2-3. Derleme · testler · mutasyon · denetim

- `dotnet build Kuyumcu.sln` (mutasyonlardan sonra, temiz): **0 hata**, 2 uyarı — ikisi de başka şeridin test dosyasında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(451)` xUnit2029). Bu paketin dosyalarında uyarı 0.
- Filtreli (KesifP4 + P10Ekran + UrunEkran + Yerlesim + UrunEtiket + BelgeYukleyici + P37Revizyon): **143/143 geçti**.
- Yeni testler (tur 2, 6 yöntem / 7 çalıştırma, `KesifP4EkranTestleri.cs`): `Urunler_parite_etiket_ve_ipucu_ham_kodsuz` (BASİT+PRO), `Urunler_tedarikci_secici_kisa_unvan_govdede_kimlik`, `Urunler_belge_dosya_secici_gizli_girdi_etiketli_dugme`, `Urunler_yerlesim_reyon_listesi_tablosuz_ad_ve_adet`, `Urunler_ad_aramasinda_enter_barkod_hatasi_basmaz`. Sahte API: uzun unvanlı ikinci cari, `/urunler/barkod/869001` → kart, diğer barkodlar 404, `q=zzz` → [].
- **Tam süit** (`--results-directory /private/tmp/kuyumcu-serit/trx-pKESIF-P4`, `_Mac_2026-10-01_00_40_39.trx`): 8240 toplam · 8231 geçti · **8 başarısız** · 1 atlandı. Sekizinin hiçbiri bu paketin dosyalarına erişmiyor (`BelgeYukleyici` yalnız `Urunler.razor`'da, `KesifP4Sozlugu` yalnız bu ikisinde kullanılıyor — grep). Tur 1 sonunda (23:44) tam süit 0 kırmızıydı; aradaki kırmızılar paralel şeritlerin yarım işi → **bu paketin yeni kırmızısı 0**. Liste ve sahip tahmini "açık kalanlar"da.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M9-1 | dosya girdisi yeniden görünür (`class="girdi"`) | Urunler_belge_dosya_secici_gizli_girdi_etiketli_dugme | AYNI |
| M9-2 | "Henüz dosya seçilmedi" satırı boş | Urunler_belge_dosya_secici_gizli_girdi_etiketli_dugme | AYNI |
| M10-1 | seçenek adı tam unvan (kısa ad yok) | Urunler_tedarikci_secici_kisa_unvan_govdede_kimlik | AYNI |
| M10-2 | kısa unvan tavanı 26 → 80 | Urunler_tedarikci_secici_kisa_unvan_govdede_kimlik | AYNI |
| M11-1 | Yeni Ürün etiketi "Parite kodu"na geri | Urunler_parite_etiket_ve_ipucu_ham_kodsuz (BASIT, PROFESYONEL) | AYNI |
| M11-2 | `u-parite-ipucu` ham kodlu HUB metnine geri | Urunler_parite_etiket_ve_ipucu_ham_kodsuz (×2) | AYNI |
| M11-3 | `d-parite-ipucu` ham kodlu HUB metnine geri | Urunler_parite_etiket_ve_ipucu_ham_kodsuz (×2) | AYNI |
| M12-1 | reyon listesi yeniden KartListe içinde (kaydırma ipucu) | Urunler_yerlesim_reyon_listesi_tablosuz_ad_ve_adet | AYNI |
| M12-2 | reyon satırı ad+tutar ızgarasından çıkar | Urunler_yerlesim_reyon_listesi_tablosuz_ad_ve_adet | AYNI |
| M13-1 | barkod biçim denetimi kaldırılır (ad barkod ucuna gider) | Urunler_ad_aramasinda_enter_barkod_hatasi_basmaz | AYNI |
| M13-2 | 404'te bant liste doluyken de çıkar | Urunler_ad_aramasinda_enter_barkod_hatasi_basmaz | AYNI |

11/11 kırmızı, 11/11 geri alındı (cmp AYNI; ayrıca `Urunler.razor` son hâli mutasyon öncesi yedekle `cmp` AYNI). Günlük: `/private/tmp/kuyumcu-serit/KESIF-P4/mut-tur2-ozet.txt`, `mut-M9-1.log` … `mut-M13-2.log`. Tur 1 mutasyonları (M1–M8, 14/14) dokunulmayan bölgelerde geçerli.

**denetim.sh** (`t2-denetim2.log`): bu paketin sayaçları ✓ — P-10 K21_DONANIM_BEYANI ✓ (RFID geri alındıktan sonra), P-18 MOBIL_TABLO_SARMALAYICISIZ ✓ (0 elle sarmalayıcı), SATIR_ICI_STYLE ✓, SABIT_RENK ✓, MODAL_GOVDE_DISI_KAYDIRMA ✓. ✗ olanlar başkasının: "Pages+Bilesenler+Layout record tanımı 4 > 3", "P-10: BU_PAKETTE_EKLENEN_RECORD +1" ve "P-02: OLCUM_TABANI_KOPYA 1 sapma" — üçü de aynı kök: `Bilesenler/Uyum/SaklamaBekletmePaneli.razor:234 private sealed record TespitOzet` (00:40'ta değişmiş, /uyum şeridi — KESIF-P6); "P-30 DEMO_INSAN_SURESI" tarih kaynaklı (kurucu kronometre ölçümü).

## T2-4. Canlı doğrulama (API 5166 + vekil 5167, yeni test kiracısı PAKETKESIF-P4-TEST, `01M3T3FEESZ2B5N3CXB9DC7EW9`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /cari (uzun unvan) | 201 | 201 · GET /cari/{id} 200 ad = "Anadolu Kapalıçarşı Kuyumculuk Sanayi ve Ticaret Anonim Şirketi" (sunucuda tam unvan saklanır) |
| POST /reyonlar (uzun ad) + POST /reyonlar/yerlesim adet 1 | 201 / 200 | 201 / 200 · GET yerlesim: [("Ön Vitrin Sol Köşe Pırlanta ve Tektaş Bölümü", 1)] atanmamış 2 |
| GET /urunler/{u1} | kod alanları | 200 · ayarId K22, pariteKodu CEYREK_YENI, tedarikciId = cari kimliği |
| PATCH /urunler/{u1} {tedarikciId} | 200, kimlik | 200 · tedarikciId aynı kimlik |
| GET /urunler/barkod/Burma | 404 | 404 "'Burma' barkodlu ürün bulunamadı" |
| GET /urunler?q=Burma | 200, eşleşme | 200 · [P4-BLZ] |
| SQL `urunler ⋈ cariler` (kiracı) | kod + tam unvan | P4-BLZ K22 CEYREK_YENI + tam unvan · P4-GMS G925 · P4-KLC K24 |

Sunucular iş sonunda durduruldu: `lsof` 5166 ve 5167 boş; başsız Chrome (CDP 9506/9507) kapandı.

## T2-5. Gerçek tarayıcı ölçümü (başsız Chrome, `olc-p4b.mjs` = sikisma-olc + ekstra + keşif ölçerleri + tur 2 yoklamaları; 96 ölçüm, 0 HATA)

Ham: `/private/tmp/kuyumcu-serit/KESIF-P4/olc4/olcum.jsonl`, tablo `olc4/tablo-tur2.md`, 208 görüntü `olc4/*.png`.

| kombinasyon | #14 reyon: adet görünür / ad satır / yatay / ipucu | #15 tedarikçi: metin · satır · sh≤ch | #16 belge: girdi gizli · düğme sol-sağ · ad | #37 etiket · hamKod | pencereDışı (çekmece/modal) | arama bant (tek/iki/yok) | kapsam K9/K3/taşan (tedarik·belge) | sayfa K1/K5/yatay |
|---|---|---|---|---|---|---|---|---|
| BASIT_aydinlik_1440 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 990-1411/1440 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_karanlik_1440 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 990-1411/1440 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_aydinlik_1440 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 990-1411/1440 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_karanlik_1440 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 990-1411/1440 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_aydinlik_1280 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 830-1251/1280 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_karanlik_1280 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 830-1251/1280 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_aydinlik_1280 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 830-1251/1280 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_karanlik_1280 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 830-1251/1280 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_aydinlik_960 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 510-931/960 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_karanlik_960 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 510-931/960 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_aydinlik_960 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 510-931/960 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| PRO_karanlik_960 | True / 1 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 510-931/960 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/5/0 · 0/0/0 | 0/0/0 |
| BASIT_aydinlik_375 | True / 2 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 30-346/375 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/4/0 · 0/0/0 | 0/0/0 |
| BASIT_karanlik_375 | True / 2 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 30-346/375 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/4/0 · 0/0/0 | 0/0/0 |
| PRO_aydinlik_375 | True / 2 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 30-346/375 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/4/0 · 0/0/0 | 0/0/0 |
| PRO_karanlik_375 | True / 2 / False / False | Anadolu Kapalıçarşı… · 1 · True | True · 30-346/375 · Henüz dosya seçilmedi | Altın cinsi/Altın cinsi · 4 | 0 | 0/0/1 | 0/4/0 · 0/0/0 | 0/0/0 |

Sütun notları: "kapsam K3" sütunundaki 4-5 değeri kendi kapsam yoklamamın "(isteğe bağlı)" alt yazısını ayrı satır saymasıdır (görüntüde tek satır; resmî `sikisma-olc` K3 = [0,0] tüm adımlarda). "hamKod 4" = RFID "K-21"/"DIŞINDADIR" (P8'e devir, üstte); parite ham kodu 0.

Resmî ölçer özeti (tüm kombinasyonlar): K1 0 · K3 [0,0] · K5 (TRY) 0 · K10 0 · K10b 0 · K7x [] · yatay 0. K12-ezik: `#urun-durum-filtre "Hepsi" w=84` (tur 1'den, P7) + `#belge-sec w=1` (gizli erişilebilir girdi, § T2-2.4). K9 6 yalnız modal/çekmece açıkken katmanlar arası.

Read ile bakılan görüntüler: `olc3a/duzenle_BASIT_aydinlik_375_belge2.png`, `olc3a/duzenle_BASIT_aydinlik_375_tedarik2.png`, `olc3a/stok_BASIT_aydinlik_375_reyonliste.png`, `olc4/stok_PRO_karanlik_375_reyonliste.png`, `olc4/duzenle_PRO_karanlik_1440_belge2.png`, `olc4/duzenle_BASIT_aydinlik_1440_tedarik2.png`, `olc4/arama_PRO_aydinlik_1280.png`.

## T2-6. Test kiracısı

`zsh betikler/kiraci-temizle.sh 01M3T3FEESZ2B5N3CXB9DC7EW9 --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı)". SQL: tenants'ta bu kimlik 0, urunler 0, cariler 0; **tenants toplamı 13** (başlangıç 13). QA-TEST-* fikstürlerine dokunulmadı.

## T2-7. Değişen dosyalar (tur 2)

- `dotnet/Kuyumcu.Web/Pages/Urunler.razor` (parite etiket/ipucu, CariSecenekleri + KisaUnvan, reyon listesi, BarkodBul)
- `dotnet/Kuyumcu.Web/Bilesenler/BelgeYukleyici.razor` (gizli girdi + etiketli düğme + dosya adı satırı)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP4.cs` (+ PariteEtiketi, PariteIpucu, DosyaSecilmedi; TR+EN)
- `dotnet/Kuyumcu.Tests/KesifP4EkranTestleri.cs` (+6 test yöntemi, sahte API genişletildi)
- `dotnet/Kuyumcu.Tests/P10EkranTestleri.cs`: geçici olarak değiştirildi, sonra **birebir eski hâline** döndü (RFID geri alımı).

Dokunulan sayfalar: Urunler.razor (tur 1'de de açıktı — gerekçe: tasarım hakeminin tur 1 bulguları, aynı paket), BelgeYukleyici.razor.

## T2-8. Koşul karşılama (tur 2)

| Koşul | Durum |
|---|---|
| #16 ham dosya kontrolü yok, 375'te taşma 0, düğme + dosya adı | KARŞILANDI (16/16) |
| #15 tedarikçi değeri tek satır, K3 0 | KARŞILANDI — sayfa tarafı geçici çözüm; kalıcı çözüm P7'ye istek |
| #37 ham kod 0 (seçenek + ipucu), etiket sade | KARŞILANDI (16/16) |
| #14 375'te adet görünür, ad sarılır, yatay kaydırma 0 | KARŞILANDI (16/16) |
| Ad aramasında Enter bandı | KARŞILANDI (16/16 + bUnit) |
| RFID "K-21" | KARŞILANMADI — P8'e devir (denetim kuralı + başkasının testi) |
| Build 0 hata · filtreli 143/143 · mutasyon 11/11 · yeni kırmızı 0 · paket sayaçları ✓ · canlı · kiracı silindi | KARŞILANDI |

## T2-9. Açık kalanlar / istek satırları

- **Sahibi: KESIF-P8** — RFID ipucu (HUB `Sozlukler.cs:3981`/EN `:6894`) "K-21" ve "DIŞINDADIR" taşıyor; değiştirilirken `denetim.sh` P-10 K21_DONANIM_BEYANI kuralı (HUB'da "K-21" arıyor) ve `P10EkranTestleri.RFID_alaninin_altinda_DONANIM_KAPSAM_DISI_beyani_yazar` ("K-21" arıyor) BİRLİKTE güncellenmeli. Önerilen metin: "Etiketteki kodu elle yazın. Bu kurulumda RFID okuyucu bağlantısı yoktur." HUB'daki kullanılmayan `Urun.PariteKodu`/`PariteKoduIpucu` metinleri de ham kod taşıyor (artık ekranda kullanılmıyor).
- **Sahibi: KESIF-P7** — Secici değeri için tek satır + ellipsis + `title` (tam metin) + dikey dolgu seçeneği; gelince sayfadaki `KisaUnvan` kısaltması kaldırılabilir. `KartListe` ipucu metni ("klavye: Tab ile odaklanıp ok tuşları") teknik; Şube Stokları'nın ÜST stok tablosunda hâlâ görünüyor (reyon bölümünde artık yok). `#urun-durum-filtre` 84 px (tur 1'den).
- **Ölçüm aracı** — `tur/ekstra.js` K12: görsel olarak gizli (clip 1×1) erişilebilir girdiler dışlanmalı.
- **Tam süitte başkasına ait 8 kırmızı** (bu paketin dosyalarına erişmiyor): `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` (SaklamaBekletme, YaptirimKaydi, YaptirimEslesmesi damgasız — /uyum şeridi, KESIF-P6), `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar` (tek başına da kırmızı; KarZarar.razor bu paketin değil), `GorunumSubesiTestleri.K3_Aylik_secili_sube_ve_tum_subeler` (SUBE-YASAL23 GorunumSubesi), `AlisOdemeAraciTestleri.NAKIT_buyuk_alis_HALA_DURDURULUR`, `NakitPayiYazmaTestleri.Haddi_asan_alista_KISMI_NAKIT_odeme_REDDEDILIR`, `SatisFisiTestleri.Haddi_asan_fiste_NAKIT_bacak_REDDEDILIR` (tevsik metni "459 s. VUK Genel Tebliği" bekleniyor — SUBE-YASAL23 TevsikKapisi), `Dogrulayici.SUBEKAPSAMASinirTestleri.S3_Kurus_siniri_bir_kurus_asim_400_ve_satir_yazilmaz` (SUBE-KAPSAM), `KesifP2HttpTestleri.CariEkstre_satir_cari_has_toplami_bakiyeye_esit` (KESIF-P2).
- Ürün listesi PRO 1280'de DURUM sütunu kaydırma alanında kısmen görünüyor (tablo kendi kabında yatay kayar; bu turun bulgusu değil, not).

## T2-10. Kurucu / mali müşavir

Mevzuat hükmü içeren değişiklik yok; soru yok. Parite ipucundaki cümle mevcut davranışı anlatır (fiyat kurdan / boşsa gram+işçilik), yeni kural değildir.

---

# TUR 1

Rol: Taşkın (backend, bu pakette sunucu değişikliği yok) + Ertuğ (ön yüz). Hasımsız paket: kanıt bu rapordadır.
Kaynak: `ham/KESIF-TRIYAJ-2026-09-30.md` § KESIF-P4 + § Ortak kurallar. Birleşen maddeler: KIYAS URUN-SADE, TOPLU KATMAN-1b ürün çekmecesi (YÜKSEK).

## 1. Özet

| Bulgu | Ne yapıldı | Kanıt |
|---|---|---|
| #14 Şube Stokları > Reyon yerleşimi, Adet kutusu +23 px, açıklama pencere kenarında kesik | Reyon (Secici) ve Adet (Sayi) `form-grid-2` ızgarasının DOĞRUDAN çocuğu oldu (ara `.alan` sarmalayıcısı kalktı). Aşım uyarısı ızgaranın altına alındı. "Atanmamış" açıklaması tablo hücresinden çıkarılıp tablonun altında sarılan paragraf oldu (`#yerlesim-atanmamis-ipucu-{sube}`). | Tarayıcı: kutuÜst/kutuAlt/etiketÜst farkı **0 px** (1280/1440/960, BASİT+PRO, açık+koyu). 375'te ızgara tek sütuna iner (alt alta, satır hizası söz konusu değil). K7x = [] tüm kombinasyonlarda |
| #15 Düzenle > Tedarik, Tedarikçi seçicisi 3 satır + etikete binme, ±26 px | Çekmecede tüm `form-grid-2` blokları `form-dikey` (tam genişlik) oldu; `#d-p16` fieldset dahil | Çekmece kapsamında K9 0, taşan 0, K7x [] (tüm kombinasyonlar). 1280+: Tedarikçi değeri tek satır (44 px) |
| #16 Belgeler "Belge türü" 48 px, "BeDosya seç" | `BelgeYukleyici.razor` iki alanı `form-dikey` ile alt alta, tam genişlik | `#d-belge-tur` genişliği 421 px (1280+) / 316 px (375) ≥ 12rem (192 px) → K12-ezik 0 (çekmecede) |
| #37 Parite kodu ham ("ATA_YENI" …) | Seçenek METNİ `/kur/pariteler` yanıtının `ad` alanı, DEĞERİ kod. Kaynakta olmayan eski kod uydurma adla değil kendisiyle görünür (md.1) | Canlı: seçenekler "Yeni Ata Lira, 14 Ayar Altın, …", düzenlemede "Yeni Çeyrek"; gövdede `pariteKodu=CEYREK_YENI` (bUnit + SQL) |
| #38 Ayar değeri 2 satır, Gram etiketi ±13 | Tam genişlik (üstteki) + ham kodsuz, kısa seçenek metni | "22 Ayar" / PRO "22 Ayar (916‰)" tek satır, 44 px |
| #40 Açıklama etiketi = sayfa açıklaması | `u-aciklama` ve `d-aciklama` etiketi `KesifP4Sozlugu.AciklamaEtiketi` ("Açıklama" / "Description") | Tarayıcı: `label[for=u-aciklama]` = "Açıklama (isteğe bağlı)" tüm kombinasyonlarda |
| #79 liste 'K22' | Liste rozeti `KurKaynakAdi.AyarAdi(u.AyarId)` (tek kaynak) | Tarayıcı: ["22 Ayar","925 Gümüş","24 Ayar"] 16/16 kombinasyon |
| #84 Yeni Ürün çift ipucu + 'K18 — …' | Secici'ye verilen `DevredisiSebebi` kaldırıldı; sebep yalnız `#u-maden-kilit` satırında. Ayar seçeneği BASİT "18 Ayar", PRO "18 Ayar (750‰)"; değer kod | Tarayıcı: kilit cümlesi diyalogda **1** kez (16/16) |
| #85 /urunler/etiketler 'K22' | Tablo rozeti ve seçili satır `KurKaynakAdi.AyarAdi` | Tarayıcı: ["22 Ayar","925 Gümüş","24 Ayar"] 16/16 |
| KIYAS URUN-SADE | `Urunler.razor` 6 emoji → Ikon / kaldırıldı (🧮 → `Ikonlar.Sayim`; satır eylemlerindeki 📦 ♻ ✏️ 🏷️ 🚫 kaldırıldı). Her satırdaki "Görsel yok" METNİ → soluk `Ikonlar.Urunler` + `aria-label`/`title` | Tarayıcı: tbody'de "Görsel yok" metni 0 (16/16) |
| KIYAS UX-5 UrunAgaci (4) + UrunEtiketleri (1) emoji | Koda bakıldı: iki dosyada da görünür emoji YOK (yalnız yorum satırlarında ⚠). 28.09 kıyas devri/hakem turunda kapanmış | `perl` taraması: 0 |

## 2. Sapmalar ve gerekçeler

1. **Önceki deneme devralındı.** Bu paketin 19:44–20:40 arasında yarım kalmış bir koşumu vardı (kod + testler yazılmış, ölçüm yapılmış ama stok adımı `null` dönmüş, mutasyon ve rapor yok). Kod satır satır okundu (`/private/tmp/kuyumcu-serit/KESIF-P4/*.razor` 19:45 yedekleriyle `diff`), spesifikasyona uygun bulundu; eksik kalan ölçüm, mutasyon, tam süit, denetim, canlı doğrulama ve temizlik bu koşumda yapıldı. Aynı test kiracısı (PAKETKESIF-P4-TEST) kullanıldı ve silindi.
2. **Mevcut test uyarlaması:** `UrunEkranTestleri.Maden_secilince_YALNIZ_o_madenin_ayarlari_ve_milyemleri_listelenir` ham kodu ("K22") seçenek METNİNDE arıyordu; #79/#84 gereği metin artık ad. Test madene göre süzmeyi seçenek DEĞERİ (kod) üzerinden sınıyor, milyem ("916", "950") metinde aranmaya devam ediyor. Zayıflatma değil: M1-3 ve M1-4 mutasyonları bu testi kırmızıya çeviriyor.
3. **Parite örnek adı:** spesifikasyon "Çeyrek (yeni)" diyor; bu bUnit fikstürünün adı. Canlı `/kur/pariteler` aynı kod için "Yeni Çeyrek" döndürüyor; ekran sunucunun adını basıyor (tek kaynak), kod uydurmuyor.
4. **Çözüm yalnız sayfa işaretlemesiyle** (triyajın istediği gibi). `katman.css`, `ortak.css`, `app.css`, Secici, SatirIciDuzenle, SayfaBasi dosyalarına dokunulmadı (P7'nin). Kullanılan `.form-dikey` sınıfı `app.css:250`'de zaten var.
5. **Ölçüm genişlikleri:** görev 1280+375, triyaj 1440/960/375 istiyor → dördü birden ölçüldü (4 genişlik × BASİT/PRO × açık/koyu = 16 kombinasyon × 5 adım = 80 ölçüm).

## 3. DoD sözleşmesi

Yeni varlık / uç / şema değişikliği YOK. Mevcut ürün düzenleme yolu (Cekmece + PATCH /urunler/{id}) değişmedi; parite ve ayar seçenekleri yalnız GÖRÜNEN metinde değişti, gövdedeki `ayarId` ve `pariteKodu` kod kalır (bUnit POST gövdesi + canlı PATCH ile sınandı). Yeni ModalPencere düzenleme kullanımı 0. Para/veri/yasal davranışa dokunulmadı.

## 4. Migration

Gerekmedi (şema değişikliği yok).

## 5. Derleme ve testler

| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` (mutasyonlardan sonra, temiz) | 0 uyarı · 0 hata (`build5.log`) |
| Filtreli (KesifP4 + UrunEkran + Yerlesim + UrunEtiket + UrunAgac + BelgeYukleyici) | 138/138 geçti |
| Tam süit (tur başına bir kez, son kodla) `--results-directory /private/tmp/kuyumcu-serit/trx-pKESIF-P4` | **8173 toplam · 8172 geçti · 0 başarısız · 1 atlandı** (`_Mac_2026-09-30_23_44_20.trx`) |

Yeni testler (`Kuyumcu.Tests/KesifP4EkranTestleri.cs`, 13 test çalıştırması / 11 yöntem): `Urunler_liste_ve_secici_ayar_adi` (BASİT+PRO), `Urunler_ayar_suzgeci_ad_gosterir_sorguda_kod_gider`, `Urunler_etiket_penceresi_ayar_adi`, `Urunler_parite_adi_gorunur_govdede_kod`, `Urunler_duzenle_parite_mevcut_kod_adla_secili`, `Urunler_aciklama_etiketi_sayfa_aciklamasi_degil`, `Urunler_maden_kilidi_ipucu_tek` (BASİT+PRO), `Urunler_duzenle_cekmece_belge_turu_tam_satir`, `Urunler_yerlesim_reyon_ve_adet_ayni_izgara_satirinda`, `Urunler_liste_emoji_ve_gorsel_yok_tekrari_yok`, `UrunEtiketleri_ayar_adi`. Triyajın zorunlu 6 testinin hepsi adıyla var.

**Yeni kırmızı:** 0 (tam süitte 0 başarısız).

## 6. Mutasyon kanıtı

Yöntem: `mut.zsh` — kaynak `/private/tmp/kuyumcu-serit/KESIF-P4/mut-yedek/<kod>.orj`'a kopyalanır → `perl -0777 -pi -e` tek eşleşme → filtreli `dotnet test` → yedek geri kopyalanır → `cmp`. Her çapa önceden `grep -c` = 1 ile doğrulandı. Günlük: `mut-ozet.txt`.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M1-1 | liste rozeti `@u.AyarId` (ham kod) | Urunler_liste_ve_secici_ayar_adi (BASIT, PROFESYONEL) | AYNI |
| M1-2 | BASİT ayar seçeneği `K22 — 22 Ayar` | Urunler_liste_ve_secici_ayar_adi (BASIT) | AYNI |
| M1-3 | PRO'da milyem düşer | Urunler_liste_ve_secici_ayar_adi (PRO), UrunEkranTestleri.Maden_secilince_YALNIZ…, UrunEkranTestleri.Duzenleme_formu_da_maden_saflik_ve_rodyum_tasir | AYNI |
| M1-4 | seçenek DEĞERİ ad olur (gövdeye ad gider) | Urunler_liste_ve_secici_ayar_adi (×2), Maden_secilince_YALNIZ…, Duzenleme_formu_da_maden_saflik_ve_rodyum_tasir | AYNI |
| M2-1 | parite metni kod | Urunler_parite_adi_gorunur_govdede_kod, Urunler_duzenle_parite_mevcut_kod_adla_secili | AYNI |
| M3-1 | `u-aciklama` etiketi sayfa açıklaması | Urunler_aciklama_etiketi_sayfa_aciklamasi_degil | AYNI |
| M4-1 | Secici'ye `DevredisiSebebi` geri (çift ipucu) | Urunler_maden_kilidi_ipucu_tek (BASIT, PROFESYONEL) | AYNI |
| M5-1 | `#d-p16` yarım sütun (`form-grid-2`) | Urunler_duzenle_cekmece_belge_turu_tam_satir | AYNI |
| M5-2 | BelgeYukleyici yarım sütun | Urunler_duzenle_cekmece_belge_turu_tam_satir | AYNI |
| M6-1 | Atanmamış açıklaması tablo hücresine geri | Urunler_yerlesim_reyon_ve_adet_ayni_izgara_satirinda | AYNI |
| M6-2 | Reyon Secici'si `.alan` sarmalayıcısına geri | Urunler_yerlesim_reyon_ve_adet_ayni_izgara_satirinda | AYNI |
| M7-1 | etiketler rozeti ham kod | KesifP4EtiketTestleri.UrunEtiketleri_ayar_adi | AYNI |
| M8-1 | "Görsel yok" satır metni geri | Urunler_liste_emoji_ve_gorsel_yok_tekrari_yok | AYNI |
| M8-2 | Düzenle eylemine ✏️ geri | Urunler_liste_emoji_ve_gorsel_yok_tekrari_yok | AYNI |

14/14 mutasyon kırmızı, 14/14 geri alındı (cmp AYNI). Son mutasyondan sonra temiz build 0/0.

## 7. denetim.sh

`denetim2.log`: tek ✗ **P-23 ODEME_KALEMSIZ_YOL = 4** (canlı veri sayacı). SQL ile sahibi bulundu: 4 satırın hepsi `PAKETYASAL2-TEST-HAKEM4 …` kiracısında (`01M3T084H15CNMTZWCEPV3NHM6`, SATIS/KARMA) — başka şeridin (YASAL-2 hakem) test kiracısı; bu paket ödeme yazmadı. Kaynak kod sayaçlarının hepsi ✓ (emoji, ModalPencere düzenleme, satır içi stil, sabit renk vb.). Paralel şerit kapısı maddesine göre bu paketin sayaçları temiz; ✗ orkestratör kapısına sahibiyle devredildi.

## 8. Canlı doğrulama (API 5166, vekil 5167 — kendi süreçleri; iş sonunda durduruldu, iki port da boş)

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /kur/pariteler | 200, ad alanı dolu | 200 · 21 parite · CEYREK_YENI → "Yeni Çeyrek" |
| GET /urunler/{u1} | ayarId/pariteKodu KOD | 200 · `ayarId=K22`, `pariteKodu=CEYREK_YENI`, `aciklama=Vitrin 1` |
| GET /urunler?ayarId=K22 | süzgeç kodla | 200 · [(P4-BLZ, K22)] |
| GET /urunler/{u1}/yerlesim | atanmamış 3 | 200 · stokAdedi 3, atanmamis 3, reyonlar [] |
| PATCH /urunler/{u1} {pariteKodu: CEYREK_ESKI} → geri CEYREK_YENI | 200/200, kod | 200 CEYREK_ESKI · 200 CEYREK_YENI |
| SQL `select kod, ayar_id, parite_kodu from urunler where tenant_id=<P4>` | kod saklanır | P4-BLZ K22 CEYREK_YENI · P4-GMS G925 NULL · P4-KLC K24 NULL |

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `olc-p4.mjs` = sikisma-olc.js + ekstra.js + kesif K7x/K9x ölçerleri)

Tam tablo: `/private/tmp/kuyumcu-serit/KESIF-P4/olc2/tablo.txt`, ham: `olc2/olcum.jsonl`, görüntüler `olc2/*.png` (80 ölçüm, 16 kombinasyon).

| Rota / adım | 1280 | 1440 | 960 | 375 | Not |
|---|---|---|---|---|---|
| /urunler liste | K1 0 · K3 0 · TRY 0 · K9 0 · K10 0 · yatay 0 · ayar "22 Ayar…" · "Görsel yok" metni 0 | aynı | aynı | aynı | K12-ezik 1 = `#urun-durum-filtre "Hepsi" w=84` (üst süzgeç, bu paketin bulgusu değil; SayfaBasi/Secici P7) |
| Yeni Ürün (modal kapsamı) | K9 0 · K3 0 · taşan 0 · K7x [] · kilit ipucu 1 · Açıklama etiketi doğru · ayar/parite adları | aynı | aynı | aynı | PRO'da "18 Ayar (750‰)" |
| Ürünü Düzenle (çekmece kapsamı, üst/tedarik/belge) | K9 0 · K3 0 · taşan 0 · K7x [] · belge türü 421 px · ayar 44 px tek satır · yazı 18 px | aynı | aynı | K9 0 · taşan 0 · K7x [] · belge türü 316 px | 375 tedarik: uzun tedarikçi adı ("Anadolu Holding Kuyumculuk Ticaret A.Ş.") seçici içinde 2 satıra SARILIR (45 px), etikete binmez, kesilmez — görüntü `duzenle_BASIT_aydinlik_375_tedarik.png` |
| Şube Stokları > Reyon yerleşimi | Reyon↔Adet kutuÜst 0 · kutuAlt 0 · etiketÜst 0 · K7x [] | 0/0/0 | 0/0/0 | tek sütun (alt alta) · K7x [] | 375'teki kapsam K9 5 = kaydırılan içerik yapışkan modal başlığının altından geçiyor (görüntüde binme yok); "TD Sayım Düzelt" = üst stok tablosu kendi kabında yatay kayar (KartListe ipucu var) |
| /urunler/etiketler | ayar "22 Ayar/925 Gümüş/24 Ayar" · K3 0 · K9 0 · K12 0 · yatay 0 | aynı | aynı | aynı | |

Sayfa geneli K9 = 6 değeri modal/çekmece AÇIKKEN ölçülüyor ve arka plandaki sayfa öğeleriyle (süzgeç etiketleri, `#urun-sinir`) katman öndeki öğeler arasında çıkıyor (katmanlar arası; kapsam ölçüsü 0). Görüntülerde binme yok.

Read ile bakılan görüntüler: `stok_BASIT_aydinlik_1280_yerlesim.png`, `stok_PRO_karanlik_375_yerlesim.png`, `duzenle_BASIT_aydinlik_375_tedarik.png`, `duzenle_BASIT_aydinlik_1280_belge.png`, `duzenle_PRO_karanlik_1440_ust.png`, `yeni_BASIT_aydinlik_1280_parite.png`, `yeni_BASIT_aydinlik_1440_ayar.png`, `urunler_BASIT_karanlik_375_acilis.png`, `etiketler_PRO_aydinlik_1280.png`.

## 10. Test kiracısı

PAKETKESIF-P4-TEST Kuyumculuk (`01M3SM286S054C43J55F6S4NNM`) `zsh betikler/kiraci-temizle.sh … --uygula` ile silindi: 545 satır + tenants 1 satır. SQL: kiracı 0 satır, ürünleri 0 satır. Kalan kiracı sayısı **13** (paralel şeritler de kendi kiracılarını açıp sildiği için "başlangıç değeri" sabit değil; P4 kiracısı kalmadı, QA-TEST-* fikstürlerine dokunulmadı).

## 11. Değişen dosyalar (bu paket)

- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/BelgeYukleyici.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP4.cs` (yeni; `AciklamaEtiketi`, `AyarMilyemKalibi`, TR+EN konumsal record)
- `dotnet/Kuyumcu.Tests/KesifP4EkranTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs` (bir test uyarlandı, § 2.2)

Not: ağaçta ilk commit dışında her şey commitsiz olduğundan `git status --porcelain` bu paketi ayırt edemiyor; liste 19:45 yedekleriyle `diff` üzerinden verildi. Dokunulan sayfalar: Urunler.razor, UrunEtiketleri.razor (UrunAgaci.razor'a dokunmak gerekmedi).

## 12. Koşul karşılama

| Koşul | Durum |
|---|---|
| #14 K7x ≤ 3 px, kesik açıklama 0 | KARŞILANDI (0 px; açıklama sarılan paragraf) |
| #15/#16/#38 çekmecede K3 0, K7x ≤ 3, K9 0, belge türü ≥ 12rem, sayfa işaretlemesiyle | KARŞILANDI (375 tedarikçi değeri sarılır — bkz. § 9) |
| #37 parite adı, ham kod 0, gövdede kod | KARŞILANDI |
| #79/#85/#84 ham ayar kodu 0, milyem yalnız PRO, gövdede kod | KARŞILANDI |
| #84 kilit ipucu 1 kez | KARŞILANDI |
| #40 Açıklama etiketi | KARŞILANDI |
| KIYAS URUN-SADE emoji + "Görsel yok" tekrarı | KARŞILANDI |
| Zorunlu 6 bUnit testi | KARŞILANDI (+5 ek) |
| Mutasyon | 14/14 kırmızı, cmp AYNI |
| Build 0/0, tam süit 0 yeni kırmızı | KARŞILANDI (8172/8173, 1 atlandı) |
| denetim.sh | Paket sayaçları ✓; tek ✗ P-23 canlı veri, sahibi YASAL-2 hakem kiracısı |
| Gerçek tarayıcı 1280+375 (+1440/960) × BASİT/PRO × açık/koyu | KARŞILANDI (80 ölçüm + görüntü) |
| Test kiracısı silindi | KARŞILANDI |
| Commit yok, dosya silme yok, 5199/5200'e dokunulmadı | KARŞILANDI |

## 13. Açık kalanlar / başka paketlere istek

- **P8 (metin):** Parite alanının ipucu (HUB `Sozlukler.cs`) hâlâ ham kod örnekleri taşıyor: "(ÇEYREK_YENİ, REŞAT…)"; RFID ipucunda "(K-21)" iç kodu. "Yeni Ürün" → "Yeni ürün", "Pasife Al", "Malı bize satan cari" (#81/#83) P8'de.
- **P7 (sahibi: KESIF-P7):** `#urun-durum-filtre` "Hepsi" seçicisi 84 px (K12-ezik 1, SayfaBasi/Secici). 375'te uzun seçici değerinin sarılması Secici sarma kuralına bağlı; kesmek yerine sarmak bilinçli bırakıldı.
- **Orkestratör kapısı:** denetim P-23 = 4, `PAKETYASAL2-TEST-HAKEM4` kiracısının verisi (sahibi YASAL-2 hakem şeridi).
- ŞUBE-KAPSAM `Urunler.razor` stok çağrısı: bu pakette dokunulmadı; kapıda birleştirilir.

## 14. Kurucu / mali müşavir

Mevzuat hükmü içeren değişiklik yok; soru yok.
