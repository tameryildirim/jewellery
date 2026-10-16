# PAKET T-S2 — Satış PROFESYONEL modu tuvale (Profesyonel.dc.html) · uygulama raporu (25.09.2026)

Rol: Taşkın (backend) + Ertuğ (frontend). Şerit: T-SATIŞ (VT `kuyumcu_test_t2`, canlı API 5162, vekil 5163).
Durum: **BİTTİ** (3. tur, 25.09 04:2x yeniden kapı — § 0-C). Doğrulayıcının 3. tur bulguları (1 KRİTİK, 1 ORTA, 3 DÜŞÜK) kapatıldı ya da sahibiyle açık kalana yazıldı. Tam süit `trx-pT-S2f` **5868/5868**, TS2 98/98, `denetim.sh` **DENETİM TEMİZ**, tenants 11→11. (2. tur durumu: **BİTTİ** (25.09 03:2x yeniden kapı). Doğrulayıcının 1 KRİTİK ve 1 DÜŞÜK bulgusu kapatıldı. Kapanış çalışması sırasında gerçek Chrome'da yeni bir kusur daha görüldü ve o da kapatıldı (§ 0). Son tam süit (`trx-pT-S2e`) **5845/5845 yeşil**. Derleme **0 uyarı, 0 hata**. `denetim.sh` sonucu **DENETİM TEMİZ**.)

## 0-C. Üçüncü tur: doğrulayıcı bulgularının kapanışı (25.09.2026, 03:30–04:20)

**Durum: BİTTİ.** Paket kapısının 7 maddesi bu turda yeniden ölçüldü:
- derleme 0 hata; tek uyarı başka şeridin (`EtiketEkranTestleri.cs(145)` xUnit1031);
- TS2 testleri **98/98**;
- `denetim.sh` → **DENETİM TEMİZ**;
- yeni mutasyonlar **5/5** kırmızı; hepsi geri alındı, `cmp` ile birebir aynı;
- canlı doğrulama 5162/5163 ile yapıldı;
- test kiracısı silindi, **tenants 11 → 11**;
- tam süit `trx-pT-S2f` **5868/5868**, başarısız 0.

### Bulgu → düzeltme → kanıt

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| **[KRİTİK] Sabit fiyatlı üründe işçilik "0 ₺" uyduruluyordu.** | `FisProKalemKarti.TeklifIscilik` artık `SabitFiyat: false` şartını arıyor. Sabit fiyatlı teklifte sunucu fiyatı parçalara ayırmaz; `iscilikTl=0` "ayrıştırılmadı" demektir. Bu yüzden değer `null` olur: hücrede "—", şeritte "—" ya da "N ₺ +" görünür. Hücrenin `title`'ı nedeni söyler. Tooltip metni de düzeltildi (`Sozlukler.SatisPro.cs`, TR+EN): "fiyat elle yazıldı **ya da ürünün sabit satış fiyatı var**". | Doğrulayıcı testi `TS2SinirTestleriTur2.Sabit_fiyatli_teklifte_iscilik_0_UYDURULMAZ_bilinmiyor_null` **yeşile döndü**. Yeni bUnit testi: `Sabit_fiyatli_kalemde_iscilik_hucre_ve_serit_TIRE_0_TL_uydurulmaz` (sahte `/fiyat/teklif` sabit yanıt). Saf teste `SabitFiyat=true → null` satırı eklendi. **Canlıda** Burma bilezik (kart iscilikTl=1600, satisFiyati=28750; sunucu `sabitFiyat:true, iscilikTl:0`) hücrede **"—"** gösterdi. Alyans (liste fiyatlı, `sabitFiyat:false`) **1.200 ₺**, Kolye **70 ₺** gösterdi. Şerit **"1.270 ₺ +"** yazdı ve title'ı taşıdı. Mutasyon M-TS2-27. |
| **[ORTA] F-tuşu testi tam süitte kararsızdı.** | Test artık önce `#islem-barkod`'u ve açılıştaki asenkron barkod odağını (`Odak("islem-barkod")==1`) bekliyor. Her tuştan sonra sayaç `WaitForAssertion` ile bekleniyor. Sonda "açılış 1 + F2 1 = 2" denetleniyor: fazladan odak çağrısı olmamalı. | İlgili 293 testlik küme (TS2TuvalPro + IslemEkran + Fis) **8 ardışık koşumda 8/8 yeşil**. Tam süit `trx-pT-S2f` yeşil. Mutasyon M-TS2-28 (yalnız F2'nin odak çağrısı kesildi) testi kırmızıya çevirdi. |
| **[DÜŞÜK] 1100–1399 px'te Yasal kapılar kartı ekranın altına iniyordu.** | Bu aralıkta `#islem-pro-yan` `display: contents` oluyor. **Yasal kapılar** kartı sekmelerin altına, formun üstüne tek satırlık özet şerit olarak çıkıyor (`order: 1`, tam genişlik). Müşteri kartı, Kısayollar ve not formun altında üç sütun kalıyor. DOM sırası değişmedi; ekran okuyucu sırası aynı. ≥ 1400'de sağ sütun, < 1100'de eski yığın aynen duruyor. | Gerçek Chrome, 1280×800 PRO açık ve koyu: `#islem-pro-yasal` **top=158**, 964×44 (eskiden top=1087). Form top=217. Kendi sekmemde (tab-72) aynı ölçüm: yasalTop 227 < formTop 286 (belge koordinatı). Görüntü: `ham/TUVAL-KARSILASTIRMA/T-S2-Profesyonel-1280-tur3.png`. CSS testi `Css_1100_1399_yasal_kapilar_formun_USTUNDE_karma_sil_dugmesi_kutu_altinda`. Mutasyon M-TS2-29. |
| **[DÜŞÜK] PRO karma kipte bacak satırı hizasızdı (K7).** | Doğrulayıcının önerdiği `align-self: flex-end` ilk denemede ölçüldü ve **yanlış çıktı**: "Merkez Kasa — Tek kasa var" notu satırı sarıyordu ve sil düğmesi alt satıra iniyordu (alt kenar 570 ↔ 509). Ayrıca "Tutar (isteğe bağlı)" etiketi 3 px kısaydı. Son çözüm: etiket yüksekliği tek belirteçten geliyor (`--bacak-etiket = tp-16 × 1,4`). Sil düğmesi etiket payı kadar aşağıda ve kutu boyunda (`--kontrol-yuksekligi`). Not, satırın altına tam genişlikte iniyor. `FisOdemeBacaklari.razor` değişmedi; yalnız `tuval-satis.css` değişti. | Gerçek Chrome 1440 karma: kutu altları **[302, 302, 302, 302]** (araç, tutar, kasa, sil), etiket üstleri eşit. 1280 karma koyu: **[180, 180, 180, 180]**. 375'te satır sarar (dikey yığın), K7=0. Görüntü: `T-S2-Profesyonel-1440-karma-tur3.png`. Mutasyon M-TS2-30 ve 30b. |
| **[DÜŞÜK] "Toplam alışveriş" satırı yok.** | Değişmedi: bu paketin dosya alanında cari ucu yok. `GET /cari/{id}/ozet` ucu **Cari şeridine (Taşkın) açık iş** olarak bırakıldı (§ 11). Satır uydurulmadı. | — |

### Mutasyonlar (3. tur)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M-TS2-27 | `TeklifIscilik` içinden `SabitFiyat: false` kaldırıldı | `TS2SinirTestleriTur2.Sabit_fiyatli_teklifte_iscilik_0_UYDURULMAZ_bilinmiyor_null`, `Sabit_fiyatli_kalemde_iscilik_hucre_ve_serit_TIRE_0_TL_uydurulmaz`, `Iscilik_yalniz_teklif_fiyatiyla_hediye_sifir_bilinmeyen_EKSIK_isaretli` | cmp AYNI |
| M-TS2-28 | `ProKisayol`: F2 için odak çağrısı atlandı (`&& e.Key != "F2"`) | `Kisayollar_karti_dort_tus_ve_F2_F4_F8_gercekten_odaklar` | cmp AYNI |
| M-TS2-29 | 1100–1399 bloğunda yasal kart `order: 1 → 3` | `Css_1100_1399_yasal_kapilar_formun_USTUNDE_karma_sil_dugmesi_kutu_altinda` | cmp AYNI |
| M-TS2-30 | Sil düğmesi hizası (ilk deneme `flex-end` → `flex-start`) | aynı CSS testi | cmp AYNI |
| M-TS2-30b | Son kuralda sil düğmesinin `margin-top` payı kaldırıldı | aynı CSS testi | cmp AYNI |

Son mutasyondan sonra `dotnet build Kuyumcu.sln` temiz: 0 hata, 0 uyarı. Tooltip ve CSS değişikliğinden sonraki derlemede 1 uyarı çıktı; o da başka şeridin xUnit1031 uyarısı.

### Gerçek tarayıcı ölçümü (3. tur)

Ölçüm başsız gerçek Chrome'da CDP ile yapıldı (`ts2e.mjs` + `sikisma-olc.js`). Kiracı `PAKETT-S2-TEST Tur3`, vekil 5163. Fişte 3 kalem vardı (ALY-00098, BLZ-00412 sabit fiyatlı, KLY-7) ve müşteri Hasan Kaya idi. İlk matris (`e-*`) başka bir nedenle geçersiz sayıldı: aralar 20 sn olduğu için sunucunun istek sınırı devreye girdi (429) ve ekranda "Sunucu şu anda çok yoğun" bandı çıktı. Matris 45 sn arayla (`g-*`) yeniden koşuldu ve **tüm koşumlarda 4xx sayısı 0** oldu.

| Genişlik | Mod | Tema | K1 | K3 taşan/kırık | K5 TRY | K6 | K7 | K9 | K10 | K11 | Yatay | Not |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yasal sağ sütunda (top 227, sol 1096) |
| 1440 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı |
| 1280 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yasal şerit top 158, form 217 |
| 1280 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı |
| 375 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yığın |
| 375 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yığın |
| 1440 | PRO karma | açık | 0 | 0/0 | 0 | 0 | **0** | 0 | 0 | 0 | 0 | bacak altları 302×4 |
| 1280 | PRO karma | koyu | 0 | 0/0 | 0 | 0 | **0** | 0 | 0 | 0 | 0 | bacak altları 180×4 |
| 375 | PRO karma | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | bacak sarar |
| 1280 | BASİT | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | basit yüz etkilenmedi |
| 1280 | BASİT | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | |
| 375 | BASİT | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | |
| 375 | BASİT | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | |
| 1440 | PRO + F2/F4/F8 (CDP tuşu) | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | activeElement: islem-barkod / islem-cari / islem-odeme |

Ek notlar:
- **Kesik hücre:** her PRO koşumunda yalnız "Eylemler 58>32" görüldü. Bu, görsel olarak gizli ekran okuyucu başlığıdır; önceki turlarda da hariç tutuldu.
- **K2:** PRO 97–98, BASİT 1280'de 69. BASİT yüz T-S1'indir ve bu turda değişmedi.
- **Kendi sekmem (tab-72, 1280×800 PRO açık, karma):** K1 0 · K3 0/0 · K5 0 · K7 [] · K9 [] · K11 0 · yatay 0 · Burma işçilik "—" · şerit "1.200 ₺ +". Sekme masaüstü boyutuna döndürüldü ve kapatıldı.
- **Tuval yan yana:** `ham/TUVAL-KARSILASTIRMA/T-S2-Profesyonel-1440.png` yenilendi (2900×940). Bölge tablosu § 2'deki gibidir. 1440'ta yerleşim değişmedi; bu turun değişiklikleri 1440 dışındaki aralıklarda ve karma kiptedir.

### Canlı doğrulama (3. tur, 5162 API, 5163 vekil)

Kiracının kurulumu:

| Adım | HTTP |
|---|---|
| davet (SQL) | — |
| `/auth/kayit` | 201 |
| `/auth/giris` | 200 |
| kasa | 200 |
| Vitrin listesi | 200 |
| 3 ürün | 201 |
| cari | 201 |

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /fiyat/teklif` BLZ-00412 | sabit fiyat, ayrıştırılmamış | `{teklifVar:true, sabitFiyat:true, hasDegeri:0, iscilikTl:0, satisFiyati:28750}` |
| `GET /fiyat/teklif` ALY-00098 | liste fiyatı, işçilik kartından | `{sabitFiyat:false, hasDegeri:24002.44, iscilikTl:1200, satisFiyati:26402.56}` |
| SQL `urunler` | kart değerleri | `ALY-00098 1200.00 NULL 3.5685` · `BLZ-00412 1600.00 28750.00 3.9388` · `KLY-7 70.00 NULL 0.000916` |
| PRO tablo | Burma "—", diğerleri teklif işçiliği | `Alyans=1.200 ₺ · Burma bilezik=— [title] · Kolye ince zincir=70 ₺` |
| PRO şerit | bilinmeyen var → "+", title | `1.270 ₺ +`, title "İşçilik bilinmiyor: … sabit satış fiyatı var …" |
| SQL `hareketler` | kayıt yok (bu turda F9 yok) | 0 |

`Kaydet()`/POST yolu bu turda değişmedi. F9 kanıtı 1. turda (§ 8) duruyor.

### Kapanış

- API (PID 899) ve vekil (PID 1947) `kill` ile durduruldu. `lsof` sonucu 5162, 5163 ve 9471 için boş.
- `kiraci-temizle.sh 01M3AZXM3NJ1NKE3S5QK8R0V37 --uygula` çalıştırıldı. Tekrar koşumu "Kiracı YOK (bağlantı sınandı)" dedi.
- **Tespit:** tenant_id taşıyan 70 tabloyu taradıktan sonra `kur_snapshots`'ta bu kiracıya ait **1 satır kaldığı** görüldü (01:13:40 UTC). Satır SQL ile silindi (ROW_COUNT 1); tarama tekrarında 0 çıktı. Temizlik betiği bu tabloyu atlıyor olabilir. Betik orkestratörün, bu yüzden § 11'e yazıldı.
- Davet satırı silindi (ROW_COUNT 1). Not araması 0 döndü.
- Fikstür dosyası `printf ''` ile boşaltıldı.
- **tenants: başta 11, sonda 11.** QA-TEST-*, DEMO-* ve PAKETQ10-4-TEST kiracılarına dokunulmadı.

### Bu turda değişen dosyalar

- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisProKalemKarti.razor`: `TeklifIscilik`, sabit fiyat şartı.
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css`: karma bacak satırı hizası ve 1100–1399 yasal şerit.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisPro.cs`: `IscilikBilinmiyor` TR+EN.
- `dotnet/Kuyumcu.Tests/TS2TuvalProTestleri.cs`:
  - `teklifSabit` sahte yanıtı;
  - 2 yeni test: sabit fiyatlı ekran testi ve 1100–1399/karma CSS testi;
  - F-tuşu testinin kararsızlığı giderildi;
  - saf teste sabit fiyat satırı eklendi.
- `Dogrulayici/TS2SinirTestleri.cs`'ye dokunulmadı.

`git status --porcelain | grep -v -E "/(bin|obj)/"` sonucunda bu paketin izleri şunlar:
- ` M Pages/Islemler.razor`;
- `?? Bilesenler/Fis/`;
- `?? Pages/Islemler.{Diger,Pro,Yazdir}.cs`;
- `?? Core/I18n/Sozlukler.SatisPro.cs`;
- `?? wwwroot/css/tuval-satis.css`.

Dosyalar izlenmediği için bu turun değişiklikleri git düzeyinde ayrı görünmez; yukarıdaki liste kesin kayıttır.

### Açık kalanlara eklenenler (3. tur)

- `GET /cari/{id}/ozet` ("Toplam alışveriş"). Sahibi: Cari/Taşkın.
- `kiraci-temizle.sh`, `kur_snapshots` satırını bırakmış olabilir. Sahibi: orkestratör. Bu turda elle silindi ve kanıtlandı.
- 1100–1399 aralığındaki yasal şerit **responsive bir karardır**; tuvalde bu genişlik yok. Kurucu ya da orkestratör "sağ sütun 1280'de de dursun" derse bu blok geri alınabilir. O durumda geniş tablo 1280'de sığmaz: sabit kolonlar ~636 px, ana sütun ~618 px.


## 0. Doğrulayıcı bulgularının kapanışı (25.09.2026, ikinci tur)

| Bulgu | Kök neden | Düzeltme | Kanıt |
|---|---|---|---|
| **[KRİTİK] PRO ekranında has/gram haneleri kayboluyordu.** 0,0003 g has "0,00 g" görünüyordu. Kart "0,00 g borçlu" yazıyordu. | PRO toplam şeridi (`FisProKalemKarti`), kalem tablosunun PRO satırı (`FisKalemTablosu`) ve müşteri kartı (`FisProYanSutun`) biçimi `"#,0.00"` ile sabit iki haneye kırpıyordu. Önceki turda `P16EkranTestleri` "2 hane" diye değiştirilerek bu kırpma kabul ettirilmişti. | `FisHesap.HasSayi` = `"#,0.00##"` (en az 2, en çok 4 hane; P-57 Bicim.Has=F4) ve `FisHesap.GramSayi` = `"#,0.00#"` (en çok 3 hane; Bicim.Gram=F3). İkisi de tr-TR ve AwayFromZero. Tuvalin iki haneli görünümü değer sığdığında aynen kalır ("5,66", "4,30"). Sıfır olmayan değer "0,00" diye basılmaz. Müşteri kartında borç yönü artık **gösterilen (yuvarlanmış)** değerden okunuyor: yuvarlanınca 0 ise "hesap kapalı" yazılır, "0,00 g alacaklı" yazılmaz. TL satırı için de aynı kural geçerli. `Bicim.cs` T-KABUK şeridine ait olduğu için yardımcılar Fiş alanına (`FisModelleri.cs`) yazıldı. | `TS2SinirTestleri.Serit_has_4_hane_ekranda_kaybolmaz` ve `Kart_kucuk_has_borcu_0_00_borclu_yazilmaz` **yeşile döndü**. Yeni testler: `HasSayi_…` (7 veri), `GramSayi_…` (5 veri), `Kalem_tablosu_ve_toplam_serit_kucuk_has_ve_grami_KAYBETMEZ`, `Musteri_karti_kucuk_bakiye_…hesap_kapali`. Canlıda KLY-7 satırı **gram 0,001 · has 0,0009** gösterdi; SQL'de `has_grami=0.000916`. Toplam has **3,9397 g** oldu (eskiden "3,94 g" idi). Mutasyonlar M-TS2-18…22 ve 24. |
| **[YENİ, bu turda bulundu] 4 haneli has, 1440'ta "3,93…" diye kesiliyordu.** Hanelerin görünmesi yetmedi, çünkü 56 px'lik has sütununa sığmadılar. | Sabit kolon bütçesi. | Has sütunu 56'dan 72'ye, gram sütunu 56'dan 62'ye çıktı (tuvalde 66). Fark ayar sütunundan (48'den 44'e) ve ürün adının 1fr payından alındı. Barkod önce 104'e indirildi, ama "BLZ-004…" diye kesildiği ölçüldü ve 118'e geri alındı. Hücreler `title` ile tam değeri taşır. | `Css_Diger_etiketi_ok_ile_tek_satir` testi has ≥ 72, gram ≥ 62 ve barkod ≥ 118 sınar (M-TS2-25). Title sınaması M-TS2-26 ile yapıldı. Canvas ile ölçülen **kesik hücre sayısı 0**; ölçüm 1440, 1280 ve 375 genişlikte, açık ve koyu temada yapıldı (§ 7b). |
| **[DÜŞÜK] 375'te "Diğer ▾" oku ayrı satıra kırılıyordu.** | Düğme `white-space: normal` idi (özet sarabilsin diye). Etiket ve ok iki ayrı metin düğümüydü. K3 bunu yakalamadı, çünkü düğmenin 2 çocuğu vardı ve K3 yalnız ≤ 1 çocuklu öğeye bakıyor. | "Diğer ▾" `<span class="islem-diger-etiket">` içine alındı ve `white-space: nowrap` verildi. Özet sarmaya devam eder. id'ler aynı kaldı. | Kendi sekmemde 375×812 PRO açık temada etiketin satır sayısı **1**. Yeni span tek çocuklu olduğu için artık K3 kapsamında; 375 PRO açık ve koyu temada K3 kırık = 0. Mutasyon M-TS2-23. |
| [DÜŞÜK] 5198'de yabancı API süreci (PID 59766) | T-S2'nin değil; bu şerit 5162/5163 kullanır. | Dokunulmadı. Orkestratöre bırakıldı. | `lsof`: PID 59766 hâlâ 5198'i dinliyor (24.09 20:36 başlangıçlı). |
| [DÜŞÜK] Tam süit sayısı yeniden üretilemedi | Paralel şeritler süite test ekleyip çıkarıyor. | Bu tur tek koşumla sabitlendi. | `trx-pT-S2e`: **5845/5845**. Bir önceki koşumda (`trx-pT-S2d`) 5844/5845 çıkmıştı. Tek kırmızı `UX1C1EkranTestleri.Giris_bos_formda_ALAN_ALTI…` idi: T-KABUK şeridinin `Giris.razor` değişikliği (02:25) ile test dosyası (02:41) arasındaki anlık fark. Sahibi T-KABUK; test dosyası güncellenince yeşile döndü. |
| [DÜŞÜK] Tuvaldeki "Toplam alışveriş" satırı yok | Cari uçlarında bu toplam yok. | Değişmedi, açık kalan olarak duruyor (§ 11). | — |

**Kapsam kararı (Madde 1: geri alınan deneme).** İlk denemede aynı biçim basit yüzdeki gram hücresine ve "Toplam · N g" satırına da uygulandı. Bu, T-S1 doğrulayıcı testlerinin bağlayıcı beklentisini kırdı:
- `TS1SinirTestleri.Verdim_toplam_gram_tr_TR(0.005 → "0,01 g")`
- `Dog3_basit_satir_gram_0_005_yukari_0_004_asagi_2_hane`

Basit yüz T-S1'in onaylı sözleşmesidir. Bu yüzden basit yüzdeki iki değişiklik **geri alındı**; dosyalar önceki hâliyle birebir aynı (diff ile doğrulandı). Düzeltme yalnız PRO yüzde. Basit yüzde 0,004 g altı gramın "0,00" görünmesi T-S1 kararıdır; açık kalanlara yazıldı.



## 1. Yapılanlar

| Tuval bölgesi | Uygulama |
|---|---|
| Tür sekmeleri | Tuval sırasıyla: Satış · Alış · Hurda Alış · Tahsilat · Ödeme · Takas · Satış İadesi · Alış İadesi (`FisTurleri.ProSekmeler`). **Tahsilat** Müşteriler sayfasına bağlantıdır; gerçek müşteri seçiliyse `?cariId=` ekler. **Ödeme** kilitlidir ve sebebini yazar (`#islem-git-odeme-kilit`, UX-S30). Basit karolarla aynı kaynaktan gelir (`BasitPos.Kisayollar`). |
| "Diğer ▾" | Kesik kenarlı düğme ve altında küçük bir özet (`#islem-diger-ozet`). **İşçilik** ve **Transfer** tuvalde sekme olmadığı için bu menüye taşındı; id'ler aynı kaldı (`#islem-tur-ISCILIK/TRANSFER`). Bunlardan biri seçiliyse düğme vurgulanır ve "Seçili: İşçilik" yazar. S2-6 menüsü aynı yerde durur. |
| Alan satırı | Sıra: Müşteri (etiketi "Müşteri") · Tarih · Para · **Satıcı** · **Fiyat listesi**. Izgara `1.4fr 140 100 160 170`, kutular 46 px, köşe 8. S2-5b satıcı seçicisi bu satıra taşındı (`FisSatici` + yeni `Etiket` parametresi). **Fiyat listesi** salt okunur bir kutudur: sunucunun `ListeCoz` kuralını birebir yansıtır (müşteriye atanmış aktif liste, yoksa varsayılan) ve "müşteriden" ya da "varsayılan" rozeti taşır (UX-S21/S22). Liste okunamazsa sebep yazılır (UX-S28). |
| Geniş kalem tablosu | Kolonlar: Barkod · Ürün · Ayar · Milyem · Gram · Has · Adet · İşçilik · Tutar · ✎. Barkod ürün kartından gelir (barkod, yoksa kod). Ayar ham kodla basılmaz ("K22" → "22"). Adet satır içinde düzenlenir. ✎ düğmesi satırın altında düzenleme satırını açar: birim fiyat, satır iskontosu, hediye ve sil. Bunların eski id'leri aynı kaldı. İşçilik yalnız **sunucu teklifinden** gelir; fiyat elle değişirse "—" yazılır (`FisKalemi.IscilikTl`, gövdeye gitmez). |
| Alt toplam şeridi | Toplam has · İşçilik · **KDV (yalnız işçilik+kâr) = "Faturada"** · Genel toplam (22 px). Boş fişte kart boş durum metniyle çizilir (`#islem-pro-kalem-bos`). |
| En alt satır | Karma/Tek ödeme hapı (`#islem-pro-karma/-tek`) mevcut `OdemeyiBol` ve `TekOdemeyeDon` yollarını kullanır. Yanında özet durur ("Nakit … → Fark 0 ₺"). Ardından Vazgeç (yalnız dolu fişte) ve **Kaydet ve yazdır** gelir. Tek belgede yazdırma sekmesi kayıttan sonra kendiliğinden açılır; bu, T-S1 ile aynı kuraldır (`SatisTuvali`). |
| Sağ sütun 320 px | **Yasal kapılar**: nakit kuralının tutarı ve durumu `/tevsik/gunluk`, kimlik eşiği `/masak/esikler` üzerinden okunur. KDV özel matrah satırı "Faturada" yazar. **Müşteri kartı**: has hesabı, TL hesabı, son işlem (`/cari/{id}/bakiye`) ve "Kartı aç". **Kısayollar**: F2, F4, F8, F9. Altta not durur. |
| F-tuşları (yeni) | Kısayol yoktu, eklendi (`ProKisayol`, sayfa kabında `@onkeydown`). F2 → `#islem-barkod`, F4 → `#islem-cari`, F8 → `#islem-odeme` (karmada `#islem-pro-karma`), F9 → `Kaydet()`; F9 yalnız `Gonderilebilir` ise çalışır. Odak mevcut `kuyumcuOdak.alana` ile taşınır; yeni JS eklenmedi. |

**Değişmeyenler.** `Kaydet()`, POST uçları ve gövdeleri, yetki ve yasal kapılar değişmedi. Yeni istekler yalnız okumadır: `/fiyat/listeler`, `/cari/{id}/bakiye` ve PRO'da `/tevsik/gunluk` önizlemesi. PRO'da karma kapalıyken ödeme kapısı bayrakları (`tevsikKalan`/`tevsikNakitKapali`) eskisi gibi boş kalır; önizleme sonucu yalnız kartın ayrı durumuna yazılır.

**Migration:** gerekmedi. Şema değişikliği yok, backend kodu değişmedi.

## 2. Sapmalar ve gerekçeler (bölge bölge fark tablosu)

Görsel karşılaştırma dosyaları:
- `ham/TUVAL-KARSILASTIRMA/T-S2-Profesyonel-1440.png`: solda tuval, sağda biz, 1440×900, gerçek Chrome, canlı test kiracısı.
- `…-1440-tam.png`: tam sayfa görünümü.

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Kenar çubuğu / üst bar | — | T-KABUK şeridi | Kapsam dışı |
| Sekme şeridi | 8 sekme, 9/16 dolgu, köşe 8; seçili sekme lacivert | Aynı ölçüler, aynı sıra | AYNI |
| Sekme adları | "Hurda alış", "Satış iadesi" | "Hurda Alış", "Satış İadesi" (hub adı) | GEREKÇELİ: UX-S31 tek ad. Form başlığı ve Kaydet etiketi hub adını kullanıyor; başka paketlerin testleri de sekmeyi bu adla buluyor (`UX4b`, `Iade`). |
| Ödeme sekmesi | Etkin görünüyor | Kilitli, altında sebep var | GEREKÇELİ: tedarikçiye ödeme ucu yok. DoD EK md.1 ve UX-S30. |
| Diğer özeti | "rezerv · avans · konsinye · siparişten satış · ciro" | "ürün ayır · kapora · konsinye · siparişten satış · çek/senet devri" | GEREKÇELİ: menünün gerçek adları kullanılıyor (UX-S31, jargon yok). |
| Alan satırı | Müşteri · Tarih · Para · Satıcı · Fiyat listesi | Aynı ölçüler | AYNI |
| Borç rozeti | Müşteri kutusunun içinde | Kutunun altında, tam metin | GEREKÇELİ: bizim rozet has + TL + döviz taşıyor ve ~170 px'lik kutuda kesiliyordu. İlk ölçümde "Peşin !" diye kırpılmıştı. |
| Müşteri notları | Yok | Pasif cari ve Peşin notları küçük yazıyla | GEREKÇELİ: UX-S29/S30. Başka paketlerin testleri de bu notları arıyor. |
| Fiyat listesi değeri | "Vitrin (×1,40)" | "Toptan" + "müşteriden" rozeti | GEREKÇELİ: çarpan tek sayı değil (has marjı ve işçilik çarpanı ayrı alanlar). Kaynak rozeti UX-S21 gereği. |
| Barkod ve elle ekleme satırı | Yok | Alan satırının altında | GEREKÇELİ: kalem eklemenin tek yolu; özellik silinmez. F2 bu kutuya odaklanır. |
| Kalem tablosu kolonları | 120 · 1fr · 56 · 66 · 66 · 66 · 60 · 90 · 110 · 44 | 118 · 1fr · 48 · 58 · 56 · 56 · 52 · 80 · 118 · 52 | GEREKÇELİ: tuvalin kolonları 1440'ta sığmıyor ve "Burma/22" üst üste biniyor (tuval hatası). Hücreler tek satır, ürün adı `title` taşıyor. |
| Tutar ve işçilik hücreleri | "28.750" | "28.660,22 ₺" | GEREKÇELİ: UX-S32 (para birimi tutarla taşınır, Bicim merkezi). |
| ✎ düzenleme | Düğme var | Düğme var; açınca fiyat, iskonto, hediye ve sil | AYNI. Tuvalde gösterilmeyen düzenleme yolları UX-S25 gereği. |
| Fiş iskontosu | Yok | Kartın içinde, toplamın üstünde | GEREKÇELİ: P-16 özelliği, silinmez. |
| KDV hücresi | "860 ₺" | "Faturada" (title'da sebep) | GEREKÇELİ, MEVZUAT: özel matrah hükmü (KDVK 23/e) sunucu motorunda "DOGRULANMALI" işaretli. Fiş için önizleme ucu yok (`/faturalar/ozel-matrah-onizleme` hareket ister ve MUHASEBE+ yetkisi gerektirir). İstemci hesabı yasak. |
| Yasal kapılar: KDV satırı | "23/e" | "Faturada" | GEREKÇELİ, MEVZUAT: madde numarası doğrulanmadığı için yazılmadı. |
| Ödeme ayrıntısı | Yok | Tablonun altında: ödeme türü/kasa ya da karma bacakları, alınan, para üstü | GEREKÇELİ: POS, kasa, döviz ve eski altın alanları özelliktir. Karma kutusu hap ile aynı soruyu sorduğu için gizlendi (DOM'da duruyor, testler kullanıyor). |
| Açıklama | Yok | Alt satırın üstünde | GEREKÇELİ: özellik (T-S1 ile aynı). |
| Müşteri kartı: toplam alışveriş | "412.300 ₺" | Satır yok; yerine "TL hesabı" | GEREKÇELİ: mevcut cari uçlarında bu toplam yok. Uydurulmadı; açık kalan olarak yazıldı. |
| Son satışlar | Yok | Formun altında, tam liste | GEREKÇELİ: PRO özelliği (S2-1), silinmez. |
| 1100–1399 px | — | Sağ sütun formun altına iniyor, kartlar yan yana | GEREKÇELİ: 1280'de ana sütun ~618 px, sabit kolonların toplamı ise ~630 px. |
| Koyu tema, seçili sekme ve hap | — | `--menu-aktif-yuzey` | GEREKÇELİ: `--kenar-yuzey` koyu zeminde görünmüyordu (ölçüldü). |
| Örnek veri (Hasan Kaya, 50.650 ₺…) | — | Gerçek test kiracısı verisi | Kural gereği |

## 3. DoD sözleşmesi

Yeni varlık ya da kayıt türü **yok**. Yeni uç ve migration da yok. Düzenlenebilirlik durumu:

- **Kalem satırı** (istemci durumu):
  - adet satır içinde düzenlenir;
  - fiyat, iskonto ve hediye ✎ satırında düzenlenir;
  - sil düğmesi ✎ satırındadır.
- **Fiyat listesi kutusu** salt okunurdur; liste Fiyatlandırma ekranında değiştirilir (title).
- **Yasal kapılar ve müşteri kartı** salt okunurdur; "Kartı aç" Cariler sayfasını açar.
- **Kısayollar kartı** salt okunurdur.

Düğmeler:
- ✎ ve hap yerinde istek atmayan görünüm eylemleridir; Karma/Tek mevcut yolları çağırır.
- Ödeme sekmesi kilitlidir ve sebebini yazar.
- Vazgeç yalnız dolu fişte çizilir.

## 4. Testler

- **Yeni:** `Kuyumcu.Tests/TS2TuvalProTestleri.cs`, **27 test**. İçerik: saf mantık, CSS, bUnit ekran ve F-tuşu testleri.
- **Değişen başka testler.** Tuval davranışı bilerek değiştiği için güncellendi; sayfa id'leri aynı:
  - `TS1TuvalSatisTestleri`: iki PRO testi yeni gerçeğe göre yazıldı ("PRO yazdırma vaat etmez" iddiası → PRO'da da "Kaydet ve yazdır" ve sekme açılması);
  - `S22TezgahFisiTestleri.Pro_modda_izgara_YOK_sekiz_sekme_AYNEN`: İşçilik ve Transfer menüde;
  - `P16EkranTestleri.Kalem_satiri…`: "K22" yerine "22", gram 2 hane;
  - `P78…FisKalemTablosuTurkceTestleri` yardımcısı ve `PosTeklifEkranTestleri` hediye testi: önce ✎ tıklanıyor.

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Filtreli (TS2, TS1, S2*, İade, UX4b, PosBarkod, Sözleşme, Peşin, P16, PosTeklif, P78, UX1C1, S1i, İşlem) | 646 | 646 | 0 |
| TS2 (son) | 27 | 27 | 0 |
| Tam süit 1 (`trx-pT-S2`) | 5768 | 5756 | **12, hepsi bu paketin**: sekme adı (İade ×7, UX4b ×2), tutar hücresinde ₺ yoktu (PosBarkod), `TevsikOnizleme` isteği .cs'ye taşındığı için sözleşme tarayıcısı göremedi (SozlesmeKapsam ×2). Üçü de düzeltildi (§ 2). |
| Tam süit 2 (`trx-pT-S2b`) | 5768 | 5768 | 0 |
| Tam süit 3 (`trx-pT-S2c`, 1. tur) | 5783 | 5783 | 0 |
| **2. tur** — filtreli (TS2, TS1, P16, S2*, PosTeklif, P78, İade, UX4b, PosBarkod, İşlem, Sözleşme) | 604 | 604 | 0 |
| 2. tur — TS2 (TS2TuvalPro + Doğrulayıcı/TS2Sinir) | 75 | 75 | 0 (iki doğrulayıcı kırmızısı yeşile döndü) |
| 2. tur — tam süit (`trx-pT-S2d`) | 5845 | 5844 | 1: `UX1C1EkranTestleri.Giris_bos_formda…`, sahibi T-KABUK (Giris.razor ↔ test dosyası anlık farkı; bu paketin değil) |
| **2. tur — tam süit, son (`trx-pT-S2e`)** | **5845** | **5845** | **0** |

Derleme: `dotnet build Kuyumcu.sln` **0 hata**. İkinci turun son derlemesi **0 uyarı, 0 hata**. Ara derlemelerdeki tek uyarı başka şeride aitti (`EtiketEkranTestleri.cs(145) xUnit1031`).

**2. tur test değişiklikleri** (`TS2TuvalProTestleri.cs`):
- 5 yeni test yöntemi eklendi: 2 Theory (12 veri satırı) ve 3 Fact.
- `Css_Diger_etiketi_ok_ile_tek_satir` kolon genişliklerini de sınıyor.
- Sahte API'nin ürün has ve gram değeri artık ayarlanabilir; varsayılanlar aynı kaldı.

`P16EkranTestleri`'nin beklentisi ("5,00" / "4,58") yeni biçimle de doğrudur. Test bu turda değiştirilmedi; yorumu "en az 2 hane" olarak okunmalıdır. Doğrulayıcı testlerine (`Dogrulayici/TS2SinirTestleri.cs`) dokunulmadı.

## 5. Mutasyon

Yöntem:
1. Kaynak `mut-yedek/T-S2/` altına kopyalandı.
2. Tek eşleşme sınanarak değişiklik yapıldı, derlendi ve filtreli testler koşuldu.
3. Yedek geri kopyalandı ve `cmp` ile doğrulandı.

**Bulunan tuzak:** `copy2` dosyanın eski mtime'ını koruyor ve MSBuild son mutasyondan sonra yeniden derlemiyordu (Vazgeç testi bu yüzden sahte kırmızı verdi). Betiğe `utime` eklendi, dosyalara `touch` yapılıp temiz derleme alındı.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M-TS2-1 | ProSekmeler: Tahsilat ↔ Ödeme | Pro_sekmeler_TUVAL_SIRASIYLA…, Sekmeler_tuval_sirasi… | AYNI |
| M-TS2-2 | ProEkTurler → boş | Diger_dugmesi…Iscilik…, S22 Pro_modda_izgara… | AYNI |
| M-TS2-3 | FiyatListesiCoz cari dalı kapalı | Fiyat_listesi_cozumu…, Alan_satiri… | AYNI |
| M-TS2-4 | F2 → islem-urun | Kisayol_hedefleri…, Kisayollar_karti… (test güçlendirildi: tuşun kendi çağrısı sayılıyor) | AYNI |
| M-TS2-5 | F9 `Gonderilebilir` bekçisi kaldırıldı | F9_kilitli_Kaydette_HICBIR_SEY_gondermez… (açık bulundu, yeni test eklendi) | AYNI |
| M-TS2-6 | Hediye işçiliği 0 değil | Iscilik_yalniz_teklif… | AYNI |
| M-TS2-7 | TeklifIscilik fiyat eşitliği kaldırıldı | Iscilik_yalniz_teklif… | AYNI |
| M-TS2-8 | Nakit durumu Uygun ↔ Aşıldı | Yasal_kapilar_…Uygun…, …devrede_derse_Asildi | AYNI |
| M-TS2-9 | Kart haddi istemci varsayılanına düşer (`TevsikHaddi`) | Yasal_kapilar_sunucu_yanit_vermezse_SAYISIZ… | AYNI |
| M-TS2-10 | Peşin'de /bakiye isteği atılır | Musteri_karti_Vazgec_ile_Pesine_donunce… (açık bulundu, yeni test eklendi) | AYNI |
| M-TS2-11 | CSS sağ sütun 320 → 300 | Css_tuval_pro_kapsami… | AYNI |
| M-TS2-12 | Etiket `SatisTuvali` → `TezgahFisi` | TS1 Pro_modda_tek_belgede…, Pro_yuz_…, TS2 Kaydet_ve_yazdir_etiketi… | AYNI |
| M-TS2-13 | Elle fiyatta `IscilikTl = null` kaldırıldı | Genis_tablo… | AYNI |
| M-TS2-14 | Satıcı etiketi (null) | Alan_satiri… | AYNI |
| M-TS2-15 | KDV hücresi istemci hesabı | Alt_toplam…KDV_faturada… | AYNI |
| M-TS2-16 | otoYazdir yalnız basit | TS1 Pro_modda_tek_belgede… | AYNI |
| M-TS2-17 | Kümülatif kalan hak kuralı kaldırıldı | Yasal_kapilar_gunluk_KALAN_nakit_hakkini_asan_fiste_Asildi | AYNI |

| M-TS2-18 | `HasSayi` "#,0.00##" → "#,0.00" | TS2Sinir.Serit_has_4_hane…, TS2Sinir.Kart_kucuk_has_borcu…, HasSayi_… (4 veri), Kalem_tablosu_ve_toplam…, Musteri_karti_kucuk… | AYNI |
| M-TS2-19 | `GramSayi` "#,0.00#" → "#,0.00" | GramSayi_… (2 veri), Kalem_tablosu_ve_toplam… | AYNI |
| M-TS2-20 | Kartın has yönü ham bakiyeden (yuvarlanmamış) | Musteri_karti_kucuk_bakiye…hesap_kapali | AYNI |
| M-TS2-21 | PRO tablo has hücresi "#,0.00" | Kalem_tablosu_ve_toplam_serit_kucuk_has_ve_grami_KAYBETMEZ | AYNI |
| M-TS2-22 | PRO toplam has "#,0.00" | TS2Sinir.Serit_has_4_hane…, Kalem_tablosu_ve_toplam… | AYNI |
| M-TS2-23 | "Diğer ▾" etiketi `white-space: normal` | Css_Diger_etiketi_ok_ile_tek_satir | AYNI |
| M-TS2-24 | Kartın TL yönü ham bakiyeden | Musteri_karti_kucuk_bakiye…hesap_kapali | AYNI |
| M-TS2-25 | Has sütunu 72 → 56 px | Css_Diger_etiketi_ok_ile_tek_satir | AYNI |
| M-TS2-26 | Has hücresinin `title`'ı kaldırıldı | Kalem_tablosu_ve_toplam… | AYNI |

17 mutasyonun 17'si öldürüldü (1. tur). 2. turdaki 9 mutasyonun 9'u öldürüldü (M-18…26). Toplam **26/26**. 2. tur yedekleri `mut-yedek/T-S2-d/` altında. Her mutasyondan sonra dosya `cp` ile geri kopyalandı, `touch` yapıldı ve `cmp` ile birebir aynı olduğu doğrulandı. Son mutasyondan sonra temiz `dotnet build`: 0 hata. M-1, M-9 ve M-13 son düzeltmelerden sonra yeniden koşuldu ve yine kırmızı verdi. Son mutasyondan sonra temiz `dotnet build` alındı: 0 hata.

## 6. Denetim

`bash denetim.sh` → rc=0, **DENETİM TEMİZ**. İlgili sayaçlar:
- B-3 ESIK_ISTEMCIDE 4 (tavan 4)
- S2-3 FIS_BILESENI_ISTEK 0
- P-26 OZEL_MATRAH_BEYANSIZ_EKRAN 0
- record tanımı 3/3
- Kültürsüz ToString 26/27
- SOZLESME_KILITSIZ_TIP 59/59
- Pages ModalPencere 48/55
- P-37 KESIK_ETIKET 0

Denetimin ilk koşumunda P-26 kırmızı verdi. Sebebi, sözlük anahtarının adıydı (`KdvOzelMatrah`). Ekran kod göstermiyor ama anahtar adı sayacı tetikliyordu; anahtar `KdvMatrahSatiri` olarak yeniden adlandırıldı ve bu değişiklik burada beyan ediliyor.

## 7. Gerçek tarayıcı ölçümü

Tarayıcı bölmesinde kendi sekmem (tab-58) açıldı. Bu sekmede alt satır hizası ölçüldü: kutuların üst kenarları arası 58 px çıktı, düzeltme bunun üzerine yapıldı. Tam matris ve görüntüler kendi başsız Chrome'umla alındı: kendi profili, CDP 9471, vekil 5163 → API 5162, `sikisma-olc.js`. Her görünümde Hasan Kaya seçili, 3 kalem var ve karma açık. İstekler oran sınırı için 30 sn aralıklı; 4xx sayısı her satırda 0.

| Görünüm | K1 | K2 % | K3 taşan/kırık | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 PRO açık | 0 | 98 | 0/0 | 2.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO koyu | 0 | 98 | 0/0 | 2.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO boş fiş | 0 | 98 | 0/0 | 1.7×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO açık | 0 | 98 | 0/0 | 2.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO koyu | 0 | 98 | 0/0 | 2.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO açık | 0 | 97 | 0/0 | 4.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO koyu | 0 | 97 | 0/0 | 4.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık/koyu | 0 | 69 | 0/0 | 1.3×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT açık/koyu | 0 | 97 | 0/0 | 2.9×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

**K7 geçmişi:** ilk turda 1–2 çıktı. Sebepler iki taneydi: Açıklama kutusu ile alt satırın 60 px'ten yakın durması ve bacak satırının ortadan hizalanması. İkisi de düzeltildi; son turda K7 = 0.

**K4 gerekçesi:** tuval 1 ekrana sığıyor, çünkü barkod/elle ekleme, ödeme ayrıntısı, iskonto, açıklama ve son satışlar tuvalde yok. Bunlar özelliktir ve silinmez. 375 genişlikte tuval yok; orada makul yığın hedefleniyor ve yatay kaydırma 0.

**K2 (BASİT 1280 %69):** basit yüz bu paketin dokunmadığı T-S1 düzenidir. Değer %60 eşiğinin üstünde.

Görüntüler ve ham JSON: `ham/TUVAL-KARSILASTIRMA/T-S2-olcum/` (29 dosya).

### 7b. İkinci tur ölçümü (25.09 02:53–03:00, düzeltmelerden sonra)

Ölçüm düzeni:
- Kendi başsız Chrome'um (CDP 9471, kendi profili) ve `sikisma-olc.js` K1–K11.
- Ek ölçüm **kesikHucre**: PRO tablo hücrelerinde canvas `measureText` ile hesaplanan metin genişliği, iç genişlikle karşılaştırıldı. Sebep: `scrollWidth`, ellipsis'li tablo hücresinde taşmayı göstermiyor; bunu kendi sekmemde gördüm.
- Kiracı: `PAKETT-S2-TEST Duzeltme`. Kalemler KLY-7 (0,001 g) ve BLZ-00412; müşteri Hasan Kaya. İstekler 30 sn aralıklı, 4xx = 0.

| Görünüm | K1 | K2 % | K3 taşan/kırık | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay | Kesik hücre |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 PRO açık | 0 | 98 | 0/0 | 1.7×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO koyu | 0 | 98 | 0/0 | 1.7×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO açık | 0 | 98 | 0/0 | 2.3×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO koyu | 0 | 98 | 0/0 | 2.3×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO açık | 0 | 97 | 0/0 | 3.9×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO koyu | 0 | 97 | 0/0 | 3.9×6 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık | 0 | 69 | 0/0 | 1.0×5 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 1280 BASİT koyu | 0 | 69 | 0/0 | 1.0×5 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 375 BASİT açık | 0 | 97 | 0/0 | 2.5×5 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 375 BASİT koyu | 0 | 97 | 0/0 | 2.5×5 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | — |

Notlar:
- Tek kesik adayı "Eylemler" başlığıydı; o `.gorunmez` ile görsel olarak gizli, erişilebilirlik metni. Tabloda sayılmadı.
- Ölçüm bu turda **iki kez yeniden koşuldu**, çünkü ilk iki koşum yeni kusurlar gösterdi: önce has "3,93…" diye kesiliyordu, sonra barkod "BLZ-004…" diye kesildi. İkisi de düzeltildi. Tablo üçüncü (son) koşumdur.
- BASİT yüz bu paketin kapsamı değil; kontrol amacıyla ölçüldü.
- 375 PRO görüntüsünde "Diğer ▾" tek satırda.

Görsel karşılaştırma yenilendi: `ham/TUVAL-KARSILASTIRMA/T-S2-Profesyonel-1440.png` (solda tuval, sağda biz) ve `…-1440-tam.png`. Görüntüler ve JSON'lar `ham/TUVAL-KARSILASTIRMA/T-S2-olcum/duzeltme-25-09/` altında.

**Bölge farkı (2. turda değişenler):**

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Kalem tablosu Has hücresi | "3,94" (2 hane) | "3,9388" (4 hane gerektiğinde) | GEREKÇELİ: P-57 Bicim.Has=F4. Doğrulayıcı KRİTİK bulgusu; iki hane has gramını kaybettiriyor. Değer iki haneye sığıyorsa ("4,30", "5,66") tuvalle aynı görünür. |
| Toplam has | "7,51 g" | "3,9397 g" | GEREKÇELİ: aynı kural. |
| Kolon genişlikleri | 120 · 1fr · 56 · 66 · 66 · 66 · 60 · 90 · 110 · 44 | 118 · 1fr · 44 · 58 · 62 · 72 · 52 · 80 · 118 · 52 | GEREKÇELİ: ölçülerek; kesik hücre 0. |
| Diğer ▾ | Tek satır | Tek satır (375'te de) | AYNI |

## 8. Canlı doğrulama

Test kiracısı `PAKETT-S2-TEST Kuyumculuk` (tenant `01M3AQH0ZV2WD3A3H2XR6KTGTQ`) davet + `/auth/kayit` ile açıldı (201). Ardından API ile kuruldu:
- kasa (200);
- iki fiyat listesi: Vitrin (varsayılan) ve Toptan (200);
- iki ürün, barkodlu (201);
- Hasan Kaya carisi, Toptan listesi atanmış (201) + 38.900 TL veresiye (200);
- stok için iki HAVALE alışı (200).

| Adım | Beklenen | Gerçek |
|---|---|---|
| PRO /islemler, 3 barkod, Hasan Kaya | Fiyat listesi = Toptan, rozet "müşteriden" | "Toptan müşteriden" |
| Yasal kapılar | Eşik sayıları sunucudan | "30.000 ₺ nakit kuralı · 185.000 ₺ kimlik eşiği". Durum 80.111 ₺ fişte "Aşıldı", boş fişte "Uygun". |
| Müşteri kartı | /cari/{id}/bakiye | "TL hesabı 38.900 ₺ borçlu · Son işlem 24 Eylül". Tarih UTC gün; tr-TR saatiyle 25.09 00:1x. |
| F2 / F4 / F8, gerçek CDP tuşu | Odak taşınır | activeElement: `islem-barkod` / `islem-cari` / `islem-odeme` |
| F9 (1 kalem, Hasan Kaya, NAKIT 28.650,74) | POST 200, yazdırma sekmesi kendiliğinden açılır | Yeni sekme `/cikti/SATIS_FISI/01M3ASR0277WJHA3E4FPBTPR9Z?yazdir=1`, fiş boşaldı |
| SQL | Hareket satırı | `01M3ASR0277WJHA3E4FPBTPR9Z · SATIS · NAKIT · 28650.74 · cari dolu` |
| F9, aynı müşteriye aynı gün ikinci nakit | Sunucu kümülatif tevsik kapısı reddeder | Hata bandında sunucu metni (57.311,65 > 30.000). **Bu ölçüm kart hatasını ortaya çıkardı:** kart "Uygun" yazıyordu. Düzeltme: kart artık sunucunun kalan hakkını da okuyor (M-TS2-17). |
| F9, Peşin, 30.089,33 ₺ | Kart önceden "Aşıldı", sunucu reddeder | Kart "Aşıldı"; sunucu 400 ile "Peşin Müşteri ile bu tutarda işlem yapılamaz…" |

**Kapanış:**
- API ve vekil `kill` ile durduruldu; 5162, 5163 ve 9471 dinlemiyor.
- Tarayıcı sekmem kapatıldı.
- `kiraci-temizle.sh --uygula` → "Kiracı YOK".
- tenant_id taşıyan 70 tablonun toplamında bu kiracının satırı **0**.
- Davet satırı silindi (row_count 1).
- Fikstür dosyaları boşaltıldı.
- **tenants: başta 11, sonda 12.** Fark başka şeridin açtığı `Q103 Kuyumculuk Ltd.` kiracısıdır; bu paketin kiracısı 0.
- QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.

### 8b. İkinci tur canlı doğrulama (5162 API, 5163 vekil, kendi sekmem tab-68 + başsız Chrome)

Test kiracısı `PAKETT-S2-TEST Duzeltme` (tenant `01M3AWRP3GTP2K73QV2JHHK8P6`) davet + `/auth/kayit` ile açıldı (201). Kurulum API ile yapıldı:
- kasa (200);
- iki ürün (201) ve iscilikTl PATCH (200);
- Vitrin ve Toptan listeleri (200);
- Hasan Kaya carisi (201) ve Toptan liste ataması (PATCH 200).

| Adım | Beklenen | Gerçek |
|---|---|---|
| PRO /islemler, KLY-7 (0,001 g, 22 ayar) barkodla eklendi | Has 4 hane, "0,00" değil | Satır: `KLY-7 · 22 · 916 · 0,001 · 0,0009 · 1 · 70 ₺ · 76,48 ₺` |
| + BLZ-00412 | Toplam has 4 hane | "3,9397 g" (0,000916 + 3,9388) |
| SQL | Ürün has gramı | `KLY-7 0.0010 0.000916` · `BLZ-00412 4.3000 3.938800` |
| 375×812 PRO açık, "Diğer ▾" | Tek satır | Range satır sayısı 1, yatay kaydırma 0 |
| 1440 PRO, hücre kesilmesi | Kesik 0 | kesikHucre = [] (başlık "Eylemler" görsel gizli, hariç) |
| Müşteri kartı (Hasan, bakiyesiz) | "Borcu yok" | "Has hesabı Borcu yok · TL hesabı Borcu yok" |

Kapanış:
- API (5162) ve vekil (5163) `kill` ile durduruldu; Chrome (9471) kapandı. `lsof` üçü için de boş.
- Tarayıcı sekmem tab-68 masaüstü boyutuna döndürülüp kapatıldı.
- `kiraci-temizle.sh 01M3AWRP3GTP2K73QV2JHHK8P6 --uygula` → SİLİNDİ. Tekrar koşumu → "Kiracı YOK (bağlantı sınandı)".
- tenant_id taşıyan **70 tablonun** toplamında bu kiracının satırı **0**.
- Davet satırı silindi (row_count 1; not araması 0).
- Fikstür dosyası boşaltıldı.
- **tenants: başta 12, sonda 11.** Bu paketin kiracısı açıldı ve silindi. Aradaki −1 başka şeridin kendi kiracısını silmesidir (önceki turda sayılan `Q103 Kuyumculuk Ltd.` artık listede yok). QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Canlı F9 (kayıt) bu turda tekrarlanmadı. Değişiklik yalnız görünüm/biçimdir; `Kaydet()`/POST yolu değişmedi. 1. turun F9 kanıtı § 8'de duruyor.

## 9. Değişen dosyalar

**Web:**
- `Kuyumcu.Web/Pages/Islemler.razor` (1400 satır sınırı içinde, 1387)
- `Pages/Islemler.Pro.cs` (yeni)
- `Pages/Islemler.Yazdir.cs`
- `Kuyumcu.Web/Bilesenler/Fis/`:
  - yeni: `FisProYanSutun.razor`, `FisProKalemKarti.razor`, `FisProOdemeHapi.razor`, `FisProFiyatListesi.razor`
  - değişen: `FisIslemIzgarasi.razor`, `FisDigerIslemler.razor`, `FisKalemTablosu.razor`, `FisMusteriSatiri.razor`, `FisSatici.razor`, `FisTurleri.cs`, `FisModelleri.cs`
- `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (T-S2 bölümü eklendi)

**2. turda değişenler:**
- `Bilesenler/Fis/FisModelleri.cs`: `FisHesap.HasYuvarla/HasSayi/GramYuvarla/GramSayi`.
- `FisKalemTablosu.razor`: yalnız PRO gram/has hücreleri + `title`. Basit satır birebir önceki hâlinde.
- `FisProKalemKarti.razor`: toplam has.
- `FisProYanSutun.razor`: has biçimi ve yönün yuvarlanmış değerden okunması.
- `FisDigerIslemler.razor`: `islem-diger-etiket` span'ı.
- `FisVerdimAldim.razor`: denendi ve geri alındı; içerik önceki hâliyle aynı.
- `wwwroot/css/tuval-satis.css`: nowrap ve kolon genişlikleri.
- `Kuyumcu.Tests/TS2TuvalProTestleri.cs`.

**Core:** `Kuyumcu.Core/I18n/Sozlukler.SatisPro.cs` (yeni, TR+EN)

**Contracts:** `Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs`
- `UrunVeri`'ye `Barkod` ve `Kod`, `CariVeri`'ye `FiyatListesiId` eklendi. Üçü de isteğe bağlı ve sunucu yanıtında zaten var olan alanlar.
- **Dosya alanı dışı; gerekçe:** bu dosya sayfanın kendi sözleşmesidir ("Kaynak: Islemler.razor @code"). Başka şerit bu dosyaya dokunmuyor.

**Tests:**
- yeni: `TS2TuvalProTestleri.cs`
- güncellenen: `TS1TuvalSatisTestleri.cs`, `S22TezgahFisiTestleri.cs`, `P16EkranTestleri.cs`, `P78SayiGirisiTestleri.cs`, `PosTeklifEkranTestleri.cs`

## 10. Koşul karşılama

| Koşul | Durum |
|---|---|
| Tuval bölgeleri birebir, farklar gerekçeli (§ 2) | Karşılandı |
| Eşikler istemciye gömülmedi (MEVZUAT) | Karşılandı. Sunucu yanıt vermezse metin sayısız kalır (test + M-9). |
| KDV / 23/e koda yazılmadı | Karşılandı: "Faturada" |
| Kısayollar gerçek (yoksa eklendi, test edildi) | Karşılandı: F2/F4/F8/F9, bUnit + CDP |
| Diğer ▾ ve satıcı seçicisi yerleşime taşındı | Karşılandı |
| id'ler korundu, Kaydet/POST/yetki değişmedi | Karşılandı (S21 id envanteri yeşil) |
| app.css ve Sozlukler hub'ına dokunulmadı | Karşılandı |
| 1440/1280/375 × BASİT/PRO × açık/koyu ölçüm, K1–K11 = 0 | Karşılandı (§ 7) |
| Paket kapısı 7 madde | Karşılandı (2. tur): build 0 uyarı / 0 hata · TS2 75/75, filtreli 604/604 · denetim TEMİZ · 26/26 mutasyon · canlı (§ 8b) · kiracı silindi, 70 tabloda 0 satır · tam süit 5845/5845 |
| Doğrulayıcı KRİTİK: has/gram 4/3 hane, "0,00 g borçlu" yok | Karşılandı: iki doğrulayıcı testi yeşil, M-18…22 ve 24, canlı "0,0009" |
| Doğrulayıcı DÜŞÜK: "Diğer ▾" 375'te tek satır | Karşılandı: M-23, canlı satır sayısı 1 |
| Para/sayı tr-TR, testlerde sabit para metni yok | Karşılandı: yeni testler `Bicim.Tl(..)` ile üretiyor; has/gram metinleri para değil |

## 11. Açık kalanlar

- **Müşteri kartındaki "Toplam alışveriş"** satırı yok, çünkü cari uçlarında bu değer yok. Sahibi: Cari/Taşkın. Gerekirse salt okunur `GET /cari/{id}/ozet` ucu açılmalı.
- **KDV kırılımı (fiş anında)** için sunucuda özel matrah önizleme ucu gerekir; mevcut uç hareket istiyor. Sahibi: Fatura/Mehmet. Hüküm 23/e için **mali müşavir teyidi gerekir** (motor "DOGRULANMALI" işaretli).
- **Çok kalemli düz satış kalem başına ayrı POST atıyor** (T-S1 § V3.4 ile aynı). PRO'da da bu yüzden etiket "Kaydet" oluyor ve kaç fiş doğacağı yazıyor. Sahibi: satış/tevsik şeridi.
- F-tuşları odak sayfanın içindeyken çalışır (sayfa kabına bağlı). Açılışta odak barkod kutusunda olduğu için pratikte hep çalışıyor. Global dinleyici eklenmedi, çünkü yeni JS/CSP gerektirirdi.
- Başka şeride ait uyarı: `EtiketEkranTestleri.cs(145)` xUnit1031. 2. turun son derlemesinde görünmedi (0 uyarı).
- **Basit yüzde gram iki hane** (T-S1 kararı; `TS1SinirTestleri` 0,004 → "0,00" bekliyor). PRO'daki kuralın basit yüze de taşınıp taşınmayacağı T-S1 sahibinin ve orkestratörün kararı. Bu paket T-S1'in onaylı sözleşmesini değiştirmedi.
- **5198'deki yabancı API süreci** (PID 59766, 24.09 20:36): T-S2'nin değil; sahibini orkestratör bulmalı.
- `sikisma-olc.js` K3 iki boşluk taşıyor: (a) 2+ çocuklu düğme içindeki kırılmayı görmüyor, (b) ellipsis'li tablo hücresinde `scrollWidth` taşmayı göstermiyor. Bu paket ölçümü canvas ile tamamladı. Betiğin kendisi orkestratörün; önerim bu iki boşluğun betiğe eklenmesi.

## 12. Kurucuya / mali müşavire notlar

- Yasal kapılar kartındaki "30.000 ₺" ve "185.000 ₺" sunucu parametresidir; kodda sabit olarak yazılmadı.
- "KDV özel matrah" satırı madde numarası göstermiyor. 23/e hükmünün ve 805 kodunun teyidi mali müşavirde.
