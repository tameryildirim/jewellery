# PAKET T-M1: Müşteriler tuvale (Musteriler.dc.html), uygulama raporu

- Tarih: 2026-09-24, 20:36–22:10
- Roller: Taşkın (backend) + Ertuğ (frontend), Opus
- Dalga: TUVAL — PANEL + MÜŞTERİLER (v2 · vt `kuyumcu_test_t3` · API 5164 / vekil 5165)
- Durum: **BİTTİ** (tur 3, 25.09 00:20: doğrulayıcının KRİTİK yarış bulgusu ve 3 DÜŞÜK bulgusu § 0-T3'te kapatıldı ya da gerekçeyle ekip oyuna/ayrı pakete yazıldı). Şerit kapısının 7 maddesi sağlandı. Tam süitte kalan 8 kırmızı T-KABUK (Ayarlar) ve Q10-2 şeritlerine ait, sahipleriyle yazıldı. Bu paketin ürettiği yeni kırmızı: 0.
- Önceki durum satırı (tur 2): **BİTTİ** (tur 2, 22:30–23:05: hasım doğrulayıcının 4 bulgusu kapatıldı, adımlar yeniden koşuldu). Şerit kapısının 7 maddesi sağlandı. Tam süitte kalan 2 kırmızı Q10-5 şeridine ait (§ 6), sahibiyle yazıldı.

## 0-T3. Tur 3 (25.09, 23:40–00:20): doğrulayıcının 4 bulgusu

### 0-T3.1 Bulgu tablosu
| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| Panel önceki müşterinin alanlarıyla açılıyor, Kaydet açık. Hızlı kayıt başka müşterinin kartını eziyor (yarış) | KRİTİK | `Cariler.razor`, `DuzenleAc`: (1) açılışta `DuzAlanlariSifirla()` bütün `duz*` alanlarını ve PEP taban değerlerini sıfırlıyor. (2) `duzYuklendi=false` ve açılış sayacı `duzAcilis` artıyor. `Guncel()` = aynı açılış **ve** `duzenleCari?.Id == c.Id`. (3) GET /cari/{id} yanıtı yalnız `Guncel()` ise uygulanıyor; eski açılışın geç yanıtı atılıyor. (4) Kart gelene kadar 19 panel alanı kilitli: FormAlani türevlerinde `Yukleniyor="@DuzKilitli"`, Secici'lerde `Devredisi="@DuzKilitli"`. Kaydet `KaydetAktif=!DuzKilitli && …` ile kilitli; sebep yazıyor: "Kaydet kilitli: Müşteri kartı yükleniyor…". (5) GET başarısız olursa kilit kalkmıyor ("Müşteri kartı alınamadı. Paneli kapatıp yeniden açın."). Eksik kartla PATCH katlı alanları silerdi. (6) `CariGuncelle` başında `!duzYuklendi` ise çıkıyor (ikinci savunma). PATCH hedefi yerel `hedefId`. (7) `SonDegisiklikYukle` artık kart GET'iyle **paralel** çalışıyor ve sonucu `guncel()` koşuluyla yazıyor. | bUnit TM1_16/17/18/19 (gecikmeli sahte API, `TaskCompletionSource`). Mutasyon M1-1…M1-7 hepsi kırmızı. Canlı gerçek fare tıklaması: § 0-T3.3 |
| Telefon tuvalde zorunlu ("Telefon *"), kodda isteğe bağlı | DÜŞÜK | Kod değişmedi. Ekranda yıldız yok ve sunucu da zorunlu tutmuyor, yani ekran ile kural tutarlı. **Ekip oyuna (Ramazan-PM, Bertan) ürün kararı olarak yazıldı:** "Müşteri kartında telefon zorunlu olsun mu?" Zorunlu olursa sunucu (`CariModulu` doğrulaması) ve ekran birlikte değişir. Bu bir kayıt kuralı değişikliği; paketin "kayıt/POST yolları değişmez" koşulu nedeniyle burada yapılmadı. | § 11 madde 9 |
| Satır eylemi hep "Tahsilat"; tuvalde duruma göre Tahsilat / Ödeme / Satış | DÜŞÜK | Doğrulayıcının önerisiyle ayrı pakete bırakıldı (§ 11 madde 5). Ödeme ve satış akışları bu ekranda yok; eklemek davranış değişikliği olur. | § 11 madde 5 |
| "Son değişiklik" satırı 429 gecikmesinde kişiyi göstermiyor, yükleniyor ibaresi de yok | DÜŞÜK | Kişi bilgisi (SAHİP, /denetim) beklenirken ad yerine "kişi alınıyor…" (EN "loading person…") yazılıyor: `sonDegisiklikKisiBekleniyor`. Yanıt gelince "Ayse Y." yazılıyor. 403/hata gelirse yer tutucu kalkıyor (hata ayrıca yazılıyor, UX-S28). | TM1_19. Mutasyon M1-5. Canlıda +500 ms: "Son değişiklik: kişi alınıyor…, bugün 23:53", yüklenince "Son değişiklik: Ayse Y., bugün 23:53" |

Yeni sözlük anahtarları (`Sozlukler.MusteriTuval.cs`, konumsal record, TR+EN): `KartYukleniyor`, `KartYuklenemedi`, `KisiAliniyor`. TM1_15 (TR/EN dolu, farklı, jargonsuz) yeşil.

### 0-T3.2 Testler ve mutasyonlar
Yeni testler (`Kuyumcu.Tests/TM1MusterilerTuvalTestleri.cs`; sahte API'ye `bekle` sözlüğü eklendi, yolun sonu eşleşirse yanıt `TaskCompletionSource` tamamlanana kadar bekletiliyor):
- **TM1_16_Yaris_A_kapat_B_ac_yanit_gelmeden_alanlar_bos_Kaydet_kilitli_PATCH_B_verisiyle**: Hasan açılıyor, yükleniyor ve kapatılıyor. Zeynep açılıyor, kart yanıtı bekletiliyor. Kontroller: `#duz-ad` ve `#duz-telefon` boş, gövdede "Hasan" yok, `#duz-ad` ve Kaydet `disabled`, kilit metni `KartYukleniyor`. `#duz-ad`'e zorla `input` olayı ("Elle Yazılan": tarayıcı `disabled`'ını aşan betik senaryosu) sonrasında Kaydet hâlâ kilitli, tıklamada PATCH 0. Yanıt serbest bırakılınca Zeynep'in verisi doluyor ve kilit kalkıyor. PATCH `/cari/c3` gövdesi: `ad`="Zeynep Demir", `telefon`="5412089017", `vergiNo`=null, "Hasan" geçmiyor.
- **TM1_17_Yaris_gec_gelen_eski_kart_yaniti_yeni_panele_YAZILMAZ**: A'nın kart yanıtı bekletiliyor. A kapatılıyor, B açılıp yükleniyor. A'nın yanıtı sonra geliyor ve B'nin alanları, başlığı ve PATCH gövdesi değişmiyor ("Hasan" ve "Kapalıçarşı" yok).
- **TM1_18_Kart_alinamazsa_Kaydet_KILITLI_kalir_sebep_yazar**: /cari/c3 500 dönüyor. Kilit metni `KartYuklenemedi`, Kaydet `disabled`, tıklamada PATCH 0.
- **TM1_19_Kisi_bilgisi_gelene_dek_yer_tutucu_kart_PARALEL_yuklenir**: /denetim bekletiliyor. Bu sırada kart yüklenmiş ve Kaydet açık (sıralı bekleme kilidi uzatmıyor). Metinde `KisiAliniyor` var. Yanıt gelince "Ayşe Y." yazıyor ve yer tutucu kalkıyor.

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `TM1MusterilerTuvalTestleri` (tur 2: 32) | 36 | 36 | 0 (arka arkaya 3 koşu, taze derleme) |
| `FullyQualifiedName~TM1` (TM1 + TM1SinirTestleri) | 52 | 52 | 0 |
| Cariler ailesi, 12 sınıf (CariEkran, UX4b, TM1, UX4c, P13, P38Sahipsiz, UX1C1, P16, PesinMusteri, S32Musteriler, P10, TM1Sinir) | 219 | 219 | 0 |
| **Tam süit** (`trx-pT-M1`, 23:51) | 5667 | 5659 | **8**, hiçbiri bu paketin değil (aşağıda) |

Tam süitteki 8 kırmızı ve sahipleri (hiçbiri `Cariler.razor` / `Sozlukler.MusteriTuval.cs` / TM1 testlerine dokunmuyor):
- **T-KABUK** (`Pages/Ayarlar.razor`; koşum sırasında düzenleniyordu, 23:4x'te bir ara `AyarlarTuvalMetinleri` derleme hatası da verdi): `S1fGorunumKartiEkranTestleri.S1f_E15_…`, `S1hGorselSigmaTestleri.S1h_06/07/08/09`, `UX1C4EkranTestleri.C4_05_Ayarlar_sayfa_basi_vardir`, `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` (mesaj: `["Ayarlar.razor"]`).
- **Q10-2 / T-SATIŞ ya da Bicim (T-KABUK)**: `Dogrulayici.Q102EkranSinirTestleri.DQ102v2_onay_acikken_kur_degisirse_POST_onaylanan_fiyati_tasir` ("6.800,00" bulunamadı: para biçimi değişikliği).
- Tur 2'deki `Q105_B11/C03` bu koşumda yeşil. **Fark (bu paketin ürettiği yeni kırmızı) = 0.**

| Kod | Değişiklik (tek eşleşme, `perl -0777`) | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M1-1 | `DuzAlanlariSifirla();` çağrısı silindi | TM1_16 | cmp AYNI |
| M1-2 | `KaydetAktif` içinden `!DuzKilitli &&` silindi | TM1_16 (ilk denemede hayatta kaldı → test zorla `input` olayıyla güçlendirildi → kırmızı) | cmp AYNI |
| M1-3 | `if (!Guncel()) { await sonDegisiklik; return; }` silindi (geç yanıt yazılıyor) | TM1_17 | cmp AYNI |
| M1-4 | Denetim çağrısı yeniden sıralı yapıldı (`await SonDegisiklikYukle` önce) | TM1_19 | cmp AYNI |
| M1-5 | `if (sonDegisiklikKisiBekleniyor) return …KisiAliniyor…` silindi | TM1_19 | cmp AYNI |
| M1-6 | Kart GET hatasında `duzYuklendi = true` eklendi | TM1_18 | cmp AYNI |
| M1-7 | `#duz-ad`'den `Yukleniyor="@DuzKilitli"` silindi | TM1_16 | cmp AYNI |

Yedekler `scratchpad/mut-yedek/T-M1/`, loglar `scratchpad/mut-M1-*.log`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**. Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 başka şeridin dosyası; son derlemede yok.

Not (M1-2 ilk koşu): Mutasyon geri alındıktan sonra koşulan ilk test derlemesi TM1_16'da bir kez kırmızı verdi. Aynı kaynakla yeniden derlenip 3 kez koşuldu: 36/36. Kırmızının sebebi bayat (mutasyonlu) derleme çıktısı; kararsızlık gözlenmedi.

### 0-T3.3 Canlı doğrulama (API 5164, vekil 5165, kiracı `PAKETT-M1-TEST Kuyumculuk` id `01M3AK2ZD0B37ZSMSK5H1YDCX2`)
Kurulum: POST /sistem/davetler 201 · /auth/kayit 201 · /auth/giris 200 · POST /cari ×6 201 · PATCH segment ×2 200 · veresiye ×3 200 · kur VAR. SQL: `update hareketler … vade_tarihi = curdate() - interval 12 day` yalnız kendi kiracıda, 2 satır.

Doğrulayıcının senaryosu **gerçek fare olaylarıyla** (tarayıcı bölmesi, kendi sekmem, BASİT, 1280×800) yeniden koşuldu. Sayfada `fetch` sarmalanıp yalnız `GET /cari/{26 hane id}` 2,5 sn geciktirildi:

| Adım | Beklenen | Gerçek |
|---|---|---|
| Hasan satırına tıkla, 3,5 sn bekle | Hasan yüklü | `{b:'Hasan Kaya', ad:'Hasan Kaya'}` |
| × ile kapat | panel kapalı | `{acik:false}` |
| Zeynep satırına tıkla, +500 ms | başlık Zeynep, alanlar boş ve kilitli, Kaydet kilitli | `{b:'Zeynep Demir', ad:'', tel:'', kd:true}`, kilit metni "Kaydet kilitli: Müşteri kartı yükleniyor…" |
| Kilitli Kaydet'e gerçek tıklama (nokta `elementFromPoint` ile `cekmece-cari-kaydet` olarak doğrulandı) | PATCH 0 | PATCH 0 (sayfa kaydı ve API günlüğünde PATCH yok) |
| +3 sn | Zeynep'in verisi, Kaydet açık | `{ad:'Zeynep Demir', tel:'05412089017', kd:false}` |
| Kaydet'e tıkla | yalnız Zeynep'e PATCH | `["/cari/01M3AK32YB5YDPCBXJRCDHZ126"]`, panel kapandı |

SQL (tur 2'deki bozulmanın tersi):
```
Hasan Kaya   05324112233  Esinin dogum gunu ekim  2026-09-24 20:53:09.041512   (DOKUNULMADI)
Zeynep Demir 05412089017  NULL                    2026-09-24 20:56:17.946123   (yalnız kendisi PATCH'lendi)
```

### 0-T3.4 Gerçek tarayıcı ölçümü (tur 3; başsız Chrome + sikisma-olc.js, `ham/TUVAL-KARSILASTIRMA/T-M1-ekran/r3/` 36 görüntü; ham veri `scratchpad/canli/r3-basit.jsonl`, `r3-pro.jsonl`)
Yeni durum **"yükleniyor"**: panel açılıyor, kart GET'i 6 sn geciktiriliyor, ölçüm açılıştan 900 ms sonra alınıyor.

| Görünüm | K1 | K2 % | K3 | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay | Kilit durumu |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 1280 BASİT açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 375 BASİT açık/koyu, liste | 0 | 97 | 0/0 | 1.8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 1440 BASİT açık/koyu, **yükleniyor** | 0 | 61 | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | ad '' · ad kilitli · Kaydet kilitli · "Müşteri kartı yükleniyor…" |
| 1280 BASİT açık/koyu, **yükleniyor** | 0 | 55 | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | aynı |
| 375 BASİT açık/koyu, **yükleniyor** | 0 | — | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | aynı |
| 1440 / 1280 / 375 BASİT açık/koyu, panel yüklü | 0 | 61 / 55 / — | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | "Son değişiklik: Ayse Y., dün 23:53 · Kim ne yaptı" |
| 1440 / 1280 / 375 PRO açık/koyu, liste | 0 | 98 / 98 / 97 | 0/0 | 1 / 1 / 1.4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 1440 / 1280 / 375 PRO açık/koyu, **yükleniyor** | 0 | 98 / 98 / 97 | 0/0 | 1–1.4 | 0 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | ad '' · kilitli · "Müşteri kartı yükleniyor…" |
| 1440 / 1280 / 375 PRO açık/koyu, panel yüklü | 0 | 98 / 98 / 97 | 0/0 | 1–1.4 | 0 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | — |

Gerekçeler tur 2'dekiyle aynı (§ 5): K8 `e-posta` aracın yanlış pozitifi (tuval etiketi "Daha fazla (adres, TC, e-posta, limit)"). K2 %55/%61: liste, sağdaki 440 px panele yer bırakıyor. PRO K9 = 6: PRO çekmecesinin karartılmış örtüsü arkadaki sayfa öğeleriyle "binen" sayılıyor (PRO çekmecesi değişmedi). Tur 3'ün yeni durumu olan **yükleniyor** hiçbir sayacı artırmadı.

Tuval karşılaştırması (1440): `T-M1-ekran/r3/T-M1-Musteriler-1440-r3.png` (solda tuval, sağda bizim yüklü BASİT panelimiz). Yerleşim tur 2 ile aynı; bu turda görünüm değişikliği yok, § 4 tablosu geçerli. Görünür farklar ve gerekçeleri:
- Tuvalde "Vazgeç", bizde ortak Cekmece "İptal" (§ 11/4).
- "Telefon *" yıldızı yok (§ 0-T3.1 satır 2).
- Panel üst barın üstünden başlıyor (§ 11/3).
- Girdi kutusunda telefon ham yazılıyor (liste biçimli; girdi değeri veridir).

Yükleniyor durumunun görüntüsü: `r3-1280-basit-acik-yukleniyor.png`. Alanlar boş ve kilitli, Kaydet soluk, altında "Kaydet kilitli: Müşteri kartı yükleniyor…", son değişiklik satırında "kişi alınıyor…".

### 0-T3.5 denetim.sh ve temizlik
- `./denetim.sh` (00:0x): 287 ✓, **1 ✗**: `P-37c: SAYFA_BASI_KAPSAM toplam=47 serit=44 muaf=3 acik=0`. Üçüncü `SAYFA_BASI_MUAFIYETI` işareti `Pages/Ayarlar.razor`'da (T-KABUK şeridi, koşum sırasında değişti; betik ≤ 2 muafiyet kabul ediyor). `Cariler.razor` `<SayfaBasi` taşıyor. **Bu şeridin sayaçlarının hepsi ✓.** Sayacın sahibi T-KABUK.
- API (5164) ve vekil (5165) `kill` ile durduruldu. İkisinin dinleyicisi de 0.
- `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan yalnız `01M3AK2ZD0B37ZSMSK5H1YDCX2` silindi. 70 tabloda kalan satır 0. `PAKETT-M1%` = 0.
- `tenants`: başlangıç **14**, benim kiracımla 15, silindikten sonra beklenen 14, ölçülen **13**. Aradaki fark başka bir şeridin kendi kiracısını silmesi: başlangıç listesinde olan `PAKETQ10-2-TEST-DOG` (`01M3AJAC4QNBM6RY3QBNYE8S87`) artık yok. Ben yalnız kendi id'mi sildim. QA-TEST kiracılarına dokunulmadı.

### 0-T3.6 Bu turda değişen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`: yarış kilidi, alan kilitleri, paralel denetim çağrısı, kişi yer tutucusu.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.MusteriTuval.cs`: 3 anahtar, TR+EN.
- `dotnet/Kuyumcu.Tests/TM1MusterilerTuvalTestleri.cs`: gecikmeli sahte API ve TM1_16–19.
- `tuval-panel-musteri.css` bu turda değişmedi.

**Dokunulan sayfa: `Pages/Cariler.razor`.** Bu paketin kendi sayfası; aynı dalgadaki doğrulayıcı dönüşü olduğu için ikinci dokunuş gerekçesi bu tablodur.

## 0. Tur 2: doğrulayıcı bulgularının kapatılması
| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| 375 BASİT kartta tablo içte yatay kayıyor; uzun ad bütün Borç sütununu ekran dışına itiyor | KRİTİK | `tuval-panel-musteri.css` @media (≤768) bloğuna `.musteri-sade .veri-tablosu.musteri-tablo { min-width: 0; max-width: 100%; }` eklendi. Bu kural app.css:953'teki `.veri-tablosu { min-width: max-content }` kuralını daha özgül seçiciyle eziyor; app.css'e dokunulmadı. | 76 karakterlik ad + "Vade geçti" + "Toptancı" rozetli fikstürle 375 BASİT açık ve koyu: `#cari-liste-sar` scrollWidth 349 = clientWidth 349, kaydırma 0, ekran dışındaki Borç hücresi 0 (§ 5). Tarayıcı mutasyonu M-TM1-16: kural silinince scrollWidth 837, kaydırma 488 px, 6 Borç hücresi ekran dışında. Geri alındı, `cmp` AYNI, yeniden ölçüm 349/349. Görüntü: `T-M1-ekran/r2/r2-375-basit-acik-liste-tam.png` |
| Tezgâhtar "Vade geçti" rozetini ve bandı görmüyor | ORTA | § 4'e gerekçeli fark olarak yazıldı. Kod değişikliği yapılmadı: gecikme bilgisi `/raporlar/vade-takibi` raporundan geliyor ve bu rapor `RaporKatalogu.cs:62` içinde `MuhasebeUstu` rolüne açık. Rozeti Tezgâhtar'a açmak, rol-bağımsız dar bir alan eklemek anlamına gelir ve bir **yetki kararıdır**. Paketin kuralı ise "yetki ve yasal kapılar DEĞİŞMEZ". | TM1_03 (403 → rozet çizilmez, gecikme uydurulmaz) · § 4 satırı · § 11 madde 6 (ekip oyuna öneri) |
| Has sütunu tuvalden gerekçesiz farklı ("HAS", "0,0000 has"; tuvalde "Has g", "5,66") | DÜŞÜK | § 4'e gerekçeli fark olarak yazıldı. Değer tek merkez `Bicim.Has` ile basılıyor (T-KABUK şeridinin dosyası). Has hassasiyeti ürün genelinde 4 hane ve birim hücrede "has" olarak yazılıyor, bu yüzden başlığa ikinci bir birim eklenmedi. | § 4 |
| Uyarı bandındaki düğme tırnak işaretlerini de metin olarak basıyor | DÜŞÜK | Tırnaklar düğmenin dışına, sözlüğe alındı. TR: `…için “` + [Tahsilat] + `” düğmesini kullan.` EN: tırnaksız. Düğme metni artık yalnız "Tahsilat". | TM1_05'e iki assert eklendi (`#cari-panel-tahsilat` TextContent == "Tahsilat"; bant metni birleşik). Mutasyon M-TM1-15 (tırnaklar geri konunca TM1_05 kırmızı). Görüntü: `r2-1440-basit-acik-panel.png` |


## 1. Spesifikasyon ile kod karşılaştırması ve sapmalar
| Konu | Tuvalde | Kodda (öncesi) | Karar |
|---|---|---|---|
| Arama | "Ad ya da telefon", büyüteç, 460 px | `Arama` (ad/telefon/e-posta), ikon yok | BASİT'te ikonlu kutu içinde aynı `#cari-arama`. Yer tutucu ve etiket "Ad ya da telefon". Sunucu araması değişmedi. |
| Segment | Hepsi / Borçlu / Toptancı | yok | İstemcide çalışır ve sunucunun zaten döndürdüğü listeyi süzer. Borçlu: TL **ya da** has bakiyesi > 0 (K4 işareti). Toptancı: `segment == TOPTAN` (`CariSegmentleri`). Yeni uç yok. |
| Tür süzgeci ve "pasifleri göster" | yok | var | **Silinmedi.** PRO'da her zaman görünür. BASİT'te alt satırdaki "Diğer süzgeçler" ile açılır. Süzgeç etkinse, derin bağlantı dahil, her zaman görünür. |
| Tablo | MÜŞTERİ(+rozet) · TELEFON · BORÇ ₺ · Has g · Tahsilat/Düzenle/⋯ | Ad · Telefon (satır içi düzenleme) · TL (Alacak/Borç rozeti) · Has · ⋯ | BASİT: Müşteri · Telefon · Borç · Has · [Tahsilat · Düzenle · ⋯] (P-61 SatirEylemleri; ⋯ menüsü tüm eylemleri taşır). Satır düğmelerinde emoji yok. Rozetler: "Vade geçti" (`/raporlar/vade-takibi` → `enEskiGecikmeGun > 0`) ve "Toptancı". |
| Telefon | "0532 411 22 33" | ham "05324112233" + satır içi düzenleme | BASİT'te yalnız görünüm biçimlendiriliyor (`TelefonGoster`), veri değişmiyor (SQL § 8). Düzeltme panelden yapılır. PRO'da satır içi düzenleme aynı kaldı. |
| Sayaç | "184 müşteri · 5 gösteriliyor" | "En çok 100 kayıt…" satırı | BASİT'te `#cari-sayac`. Sınıra değilirse "100+ müşteri" yazar, `#cari-sinir` ve `#cari-sinir-asildi` görünür (UX-S29). Sınırın altında sayı gerçektir ve sınır cümlesi çizilmez. |
| Satıra dokunma | sağ panel açılır | yalnız Düzenle | BASİT'te hücrelere `@onclick` eklendi. Eylem hücresi hariç tutuldu, S32_01 kuralı korunuyor. Klavye yolu Düzenle düğmesi. |
| Panel başlığı | müşterinin adı | "Müşteriyi düzenle" | BASİT'te ad, PRO'da aynı |
| Uyarı bandı | "Vadesi 12 gün geçti. Para almak için listedeki "Tahsilat" düğmesini kullan." | yok | Gecikme > 0 ise bant çıkar. "Tahsilat" gerçek bir düğme (`#cari-panel-tahsilat`): panel kapanır, tahsilat penceresi açılır. Panel listeyi örttüğü için "listedeki" sözcüğü kaldırıldı. |
| Alanlar | Ad soyad*, Telefon*, Not, Daha fazla | Ad/Firma Adı*, Telefon (isteğe bağlı + ipucu), Notlar (2 satır + sayaç + ipucu) | BASİT: "Ad soyad", "Telefon", "Not" (tek satır). İpucu ve sayaç tuvalde yok, bu yüzden gizlendi. **Telefon'a yıldız konmadı**: sunucuda zorunlu değil, kayıt kuralı değişmez. |
| Daha fazla | "Daha fazla (adres, TC, e-posta, limit) ▾" | "(adres, kimlik, …) ▾" glifi | Metin tuvaldeki gibi. ▾ yerine ince çizgi `Ikon chevron-down`, açıkken döner. |
| Pasife al | panelde yok | panelde her zaman | BASİT'te "Daha fazla" içinde, jargonsuz "Kullanımdan kaldır" (id `c-pasife-al`, onay modalı aynı). PRO'da aynı kaldı. |
| Son değişiklik | "Son değişiklik: Ayşe Y., dün 16:20 · Kim ne yaptı" | yok | Zaman listedeki `guncellendi` alanından gelir. **Kim** bilgisi yalnız SAHİP'te `GET /denetim?varlikTuru=Cari&varlikId=…` ile okunur ve ilk **yazma** kaydı (EKLEME/GUNCELLEME/SILME) alınır; canlıda en yeni kayıt OKUMA idi (§ 8). Diğer rollerde ad uydurulmaz, yalnız zaman yazılır, bağlantı çizilmez. Hata durumunda sebep yazılır (UX-S28). |
| Vazgeç/Kaydet | "Vazgeç" | ortak Cekmece "İptal" | GEREKÇELİ FARK: ortak bileşen tüm çekmecelerde "İptal" der (UX-S31: aynı kavram tek ad). Cekmece bu şeridin dosya alanında değil. |

Mevzuat hükmü yok: eşik, vergi ya da MASAK kuralı kodlanmadı. Kaydet/POST/PATCH yolları, yetkiler ve yasal kapılar değişmedi (TM1_06b PATCH gövdesini sınıyor).

**Dosya alanı dışına tek dokunuş (gerekçeli):** `Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`. Bu dosya Cariler ekranının kendi sözleşmesi ve başka hiçbir şeridin listesinde yok. Mevcut `CariVeri` tipine `Segment` ve `Guncellendi` isteğe bağlı alanları eklendi; sunucu bu iki alanı zaten döndürüyordu (`CariServisi.Listele`). Yeni tip açılmadı. Sözleşme testleri tam süitte yeşil.

## 2. Değişen / eklenen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor` (BASİT tuval yüzü; PRO DOM'u korundu, yalnız `musteri-sutun-*` sınıfları eklendi)
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-panel-musteri.css` (dosyanın sonuna "PAKET T-M1" bloğu; T-P1 bloğuna dokunulmadı; sabit renk yok, yalnız `var(--…)`)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.MusteriTuval.cs` (yeni; TR+EN, konumsal record)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Musteriler.cs` (`DahaFazlaIcerik` TR metni: "adres, TC, e-posta, limit")
- `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs` (2 isteğe bağlı alan)
- `dotnet/Kuyumcu.Tests/TM1MusterilerTuvalTestleri.cs` (yeni; tur 2'de TM1_05'e 2 assert eklendi)
- Tur 2: `Cariler.razor` (bant düğmesi tırnaksız), `Sozlukler.MusteriTuval.cs` (`VadeUyariOnce`/`VadeUyariSonra`: TR tırnaklar, EN boşluklar), `tuval-panel-musteri.css` (375 `min-width: 0`), `ham/TUVAL-KARSILASTIRMA/T-M1-Musteriler-1440*.png` yeniden üretildi, `T-M1-ekran/r2/`
- Kanıt: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/T-M1-Musteriler-1440.png`, `…/T-M1-Musteriler-1440-liste.png`, `…/T-M1-ekran/` (24 görüntü + `olcum.jsonl`)
- Migration: **gerekmedi** (şema değişmedi).

Dokunulan sayfalar: `Pages/Cariler.razor`.

## 3. DoD sözleşmesi
Bu pakette yeni varlık ya da kayıt türü yok. Mevcut cari kartının yolları değişmedi:
- `PATCH /cari/{id}` (gönderilmeyen alan korunur, null silinir): panel Kaydet (ortak Cekmece).
- `DELETE /cari/{id}` (pasife alma, onay modalı): BASİT'te "Daha fazla" içinde "Kullanımdan kaldır", PRO'da görünür. Geri dönüş yolu pasif satırdaki "Aktife al".
- Peşin Müşteri sistem kartı: DegismezIpucu aynı kaldı (BASİT'te "Daha fazla" içinde).
- Salt okunur yeni bilgiler: "Son değişiklik" satırı (denetim kaydı değiştirilemez; uç yalnız SAHİP'e açık) ve "Vade geçti" rozeti (rapor, türetilmiş).
- Her yeni düğme istek atıyor ya da durum değiştiriyor: segment → süzme, "Diğer süzgeçler" → aç/kapa, `#cari-panel-tahsilat` → tahsilat penceresi + `GET /kasalar`. bUnit'te sınandı.

## 4. Tuval karşılaştırması (asıl kanıt)
Yan yana görüntü: `ham/TUVAL-KARSILASTIRMA/T-M1-Musteriler-1440.png`. Solda tuval png, sağda bizim BASİT görünümümüz; panel açık, 1440×900, açık tema, gerçek Chrome (başsız, kendi profili, vekil 5165).

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Kenar çubuğu / üst bar | — | T-KABUK şeridinin alanı | Kapsam dışı |
| Sayfa başlığı | yalnız üst barda | h2 görsel olarak gizli (DOM'da) | AYNI |
| Arama | büyüteç + "Ad ya da telefon", 460×50, köşe 9 | aynı | AYNI |
| Segment | Hepsi (koyu dolgu) / Borçlu / Toptancı hapı | aynı (`--kenar-yuzey`) | AYNI |
| Yeni müşteri | panel açıkken panelin altında kalıyor | panel açıkken gizli; kapalıyken ince "+" ile sağ üstte | AYNI |
| Tablo başı | MÜŞTERİ · TELEFON · BORÇ ₺ | MÜŞTERİ · TELEFON · BORÇ | GEREKÇELİ FARK: ₺ hücrede `Bicim.Tl` ile yazılıyor (UX-S32 + T-K1 tek biçim merkezi) |
| Has sütunu | başlık "Has g", değer "5,66" (2 hane, birimsiz) | başlık "HAS" (sözlük `SutunHas` = "Has", büyük harf CSS'ten), değer "0,0000 has" | GEREKÇELİ FARK: değer tek merkez `Bicim.Has` ile basılıyor (T-KABUK'un dosyası, bu şerit değiştiremez). Has ürün genelinde 4 hane hassasiyetle gösteriliyor (kasa, emanet, rapor aynı). Birim hücrede "has" olarak yazıldığı için başlığa ikinci birim ("g") eklenmedi; "Has g" + "… has" aynı birimi iki kez söylerdi. Tuval biçimine geçmek T-K1/T-KABUK'un tek merkez kararıdır |
| "Vade geçti" rozeti ve panel uyarı bandı, **Tezgâhtar** rolünde | tuvalin kişisi Tezgâhtar ve rozeti görüyor | Tezgâhtar'da çizilmiyor. SAHİP ve MUHASEBE rollerinde çiziliyor | GEREKÇELİ FARK: kaynak `/raporlar/vade-takibi` `MuhasebeUstu` rolüne açık (`RaporKatalogu.cs:62`). 403 gelirse rozet ve bant çizilmez, gecikme uydurulmaz (UX-S28, TM1_03). Rozeti Tezgâhtar'a açmak yetki kapsamını genişletir (cari listesine rol-bağımsız bir gecikme alanı). Paket kuralı "yetki kapıları DEĞİŞMEZ" olduğu için bu karar ekip oyuna bırakıldı (§ 11 madde 6) |
| Satırlar | 62 px, ad 500, soluk telefon, sağa yaslı kalın borç, eksi kırmızı | aynı; seçili satır `--vurgu-yuzey` | AYNI |
| Rozetler | "Vade geçti" kırmızı, "Toptancı" gri | aynı. Bizde ek olarak "Tedarikçi" türü var | AYNI (tür rozeti S3-2 bilgisi) |
| Sıra | örnek veri | sunucu sırası (ad) | Veri değil |
| Sayaç | "184 müşteri · 5 gösteriliyor" | "5 müşteri · 5 gösteriliyor · Diğer süzgeçler" | GEREKÇELİ FARK: tür/pasif süzgecinin BASİT'teki yolu (özellik silinmez) |
| Panel yeri | başlık çubuğunun altından (top 68) | tam boy (top 0) | GEREKÇELİ FARK: ortak Cekmece fixed; üst bar yüksekliği T-KABUK'ta değişiyor, sabit px ile bağlamak kırılgan olurdu. Genişlik 440, dolgu 24/26 tuvaldeki gibi |
| Panel başlığı + × | ad 21 px, 38×38 çerçeveli × | aynı | AYNI |
| Uyarı bandı | "…listedeki "Tahsilat" düğmesini kullan." | "…Para almak için “Tahsilat” düğmesini kullan.". Tırnaklar düğmenin dışında, düğme metni yalnız "Tahsilat" (tur 2) | GEREKÇELİ FARK: yol panelin içinde (UX-S25) |
| Ad soyad* | var | aynı | AYNI |
| Telefon* | yıldızlı, "0532 411 22 33" | yıldızsız, kutuda kayıtlı değer "05324112233" | GEREKÇELİ FARK: sunucuda telefon zorunlu değil, Kaydet kuralı değişmez. Kutu veriyi olduğu gibi gösterir (görünüm biçimi yalnız listede) |
| Not | tek satır | tek satırlık textarea, sayaç ve ipucu gizli | AYNI |
| Daha fazla | "(adres, TC, e-posta, limit) ▾" | aynı metin, ince çizgi ok | AYNI |
| Son değişiklik | "Ayşe Y., dün 16:20 · Kim ne yaptı" | "Ayse Y., bugün 20:59 · Kim ne yaptı" | AYNI (veri canlı) |
| Vazgeç / Kaydet | Vazgeç + geniş Kaydet (50 px, köşe 9) | İptal + geniş Kaydet | GEREKÇELİ FARK: ortak Cekmece metni (§ 1) |
| Arka plan karartması | yok | BASİT ≥ 1024 px'te saydam | AYNI |

## 5. Gerçek tarayıcı ölçümü (sikisma-olc.js; gerçek Chrome başsız, vekil 5165, PAKETT-M1-TEST kiracısı)
Koşum 21:58, son derlemeden sonra. Tüm görüntüler `ham/TUVAL-KARSILASTIRMA/T-M1-ekran/son-*.png` altında.

| Görünüm | K1 | K2 % | K3 | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık, liste | 0 | 98 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT koyu, liste | 0 | 98 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık/koyu, liste | 0 | 98 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT açık/koyu, liste | 0 | 97 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT açık/koyu, panel | 0 | 61 | 0/0 | 1.2×2 | 0 | 0 | 0 | **1** | 0 | 0 | 0 | 0 |
| 1280 BASİT açık/koyu, panel | 0 | **55** | 0/0 | 1.3×2 | 0 | 0 | 0 | **1** | 0 | 0 | 0 | 0 |
| 375 BASİT açık/koyu, panel | 0 | — | 0/0 | 1.0×1 | 0 | 0 | 0 | **1** | 0 | 0 | 0 | 0 |
| 1440 PRO açık/koyu, liste | 0 | 98 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO açık/koyu, liste | 0 | 98 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO açık/koyu, liste | 0 | 97 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Gerekçeler:
- **K8 = 1 (yalnız panel açıkken):** yakalanan sözcük `e-posta`. Kaynağı tuvalin kendi etiketi "Daha fazla (adres, TC, e-posta, limit)". Bu aracın yanlış pozitifi: K8 kebab deseni `\b[a-z]+(?:-[a-z]+)` TDK yazımlı "e-posta" sözcüğünü de yakalıyor. Q10-5 raporu aynı yanlış pozitifi /cariler için kaydetmişti. Jargon değil ve tuvalle birebir olduğu için metin değiştirilmedi. Araç önerisi (orkestratör): tek harfli önekleri (`e-`) dışlamak.
- **K2 %55/%61 (panel açık):** liste, sağdaki 440 px panele yer bıraktığı için bilerek daraldı. Tuvalde de liste panelin solunda kalıyor. Panel kapanınca %98.
- **QA-10 bulgusu (375 px'te /cariler tablosu 975 px taşıyordu):** BASİT'te satırlar karta dönüyor (ad ve rozetler üstte, telefon · borç, has, eylemler). Sayfa yatay kaydırması 0; tablo içinde de kaydırma yok (`son-375-basit-acik-liste-tam.png`). PRO'da tablo KartListe'nin kendi kaydırma kabında; sayfa yatayı 0.
- Panel açıkken K9 = 0: liste panelin altına girmiyor. ≥ 1024 px'te sağda panel genişliği kadar boşluk bırakılıyor; < 1024 px'te panel tam ekran açılıyor ve arkadaki liste çizilmiyor.
- PRO'da panel durumu ayrıca ölçülmedi: PRO çekmecesi değişmedi.

### 5b. Tur 2 ölçümü (22:45, uzun adlı fikstür, `T-M1-ekran/r2/` 24 görüntü + `olcum.jsonl`)
Fikstürde 6 cari var. Biri 76 karakterlik "Kaya Kuyumculuk Mucevherat Pirlanta Ithalat Ihracat Sanayi Ticaret Ltd. Sti." ve iki rozet taşıyor (Vade geçti 12 gün, Toptancı). `sar` sütunu: `#cari-liste-sar` iç kaydırma kabı, scrollWidth/clientWidth. Doğrulayıcının önerdiği iç kap ölçümü tara betiğine eklendi.

| Görünüm | K1 | K2 % | K3 | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Sayfa yatay | İç kap sw/cw | Ekran dışı Borç |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1122/1122 | 0 |
| 1280 BASİT açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 962/962 | 0 |
| **375 BASİT açık/koyu, liste** | 0 | 97 | 0/0 | 1.8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | **349/349** | **0** |
| 1440 BASİT açık/koyu, panel | 0 | 61 | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | 686/686 | 0 |
| 1280 BASİT açık/koyu, panel | 0 | 55 | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | 526/526 | 0 |
| 375 BASİT açık/koyu, panel | 0 | — | 0/0 | 1 | 0 | 0 | 0 | 1 (`e-posta`) | 0 | 0 | 0 | 0 | liste çizilmiyor | 0 |
| 1440 PRO açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1122/1122 | 0 |
| 1280 PRO açık/koyu, liste | 0 | 98 | 0/0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1016/962 | 0 |
| 375 PRO açık/koyu, liste | 0 | 97 | 0/0 | 1.4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1376/349 | 7 (kap içinde) |
| 1440/1280/375 PRO, panel | 0 | 97–98 | 0/0 | 1–1.4 | 0 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | liste ile aynı | — |

- K4 1.8 (375 BASİT liste): uzun ad karta 3 satır sarıyor. Bu kart yığınının beklenen boyu; taşma ya da kırık yok.
- **PRO iç kaydırma (1280'de 54 px, 375'te 1027 px):** PRO görünümü bu pakette değişmedi ("PRO DOM'u korundu", § 1). Tablo KartListe'nin ortak, erişilebilir kaydırma bölgesinde kalıyor (`role=region tabindex=0` ve kaydırma ipucu; uygulama geneli kalıp). Sayfa yatay kaydırması her görünümde 0. Tuval ve QA-10 kart görünümü BASİT'e ait. PRO tablosunu karta çevirmek PRO davranışını değiştirir, bu yüzden § 11 madde 7'ye açık iş olarak yazıldı. 1280'deki 54 px yalnız 76 karakterlik uç adla oluşuyor; kısa adlı fikstürde 0.
- PRO panel K9 = 6: PRO çekmecesi (değişmedi) karartmalı bir örtü. Ölçüm, çekmecenin "Tür" alanı ile arkadaki sayfa süzgecinin "Tür" alanını "binen" sayıyor; arkadaki öğe örtünün altında. BASİT panelde K9 = 0.

## 6. Testler
| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Filtre `TM1MusterilerTuval` (ilk) | 32 | 30 | 2 (testteki yol öneki hatası `/api/v1/api/v1`; test düzeltildi) |
| Filtre Cariler ailesi (TM1 + S32 + CariEkran + UX1C1 + P10/P13/P16 + PesinMusteri + UX4b/UX4c + P38) | 199 | 199 | 0 |
| **Tam süit** (`trx-pT-M1`, `KUYUMCU_TEST_DB=kuyumcu_test_t3`) | **5548** | **5546** | **2** |
| Tur 2: filtre TM1 + S32 + CariEkran | 86 | 86 | 0 |
| **Tur 2 tam süit** (`trx-pT-M1-r2`, 22:45, `Counters total=5605 passed=5603 failed=2`) | **5605** | **5603** | **2** |

Tur 2'deki 2 kırmızının sahibi **Q10-5** şeridi. İkisi de o şeridin dosyalarını sınıyor ve o dosyalar koşum sırasında değişiyordu (`q10.css` 22:48, `Kilavuz.razor` 22:49 değiştirildi):
- `Q105FormJargonTabloTestleri.Q105_B11_Kilavuz_mod_sonradan_cozulunce_Musteriler_olur` (`Pages/Kilavuz.razor` çiziliyor)
- `Q105FormJargonTabloTestleri.Q105_C03_q10_css_baslik_nowrap_mobil_yigin_ve_karsi_taraf_kurallari_var_sabit_renk_yok` (`css/q10.css` içinde regex; Assert.Matches başarısız)

Tur 1'in Q10-2 kırmızıları tur 2'de yeşil. **Bu paketin yeni kırmızısı 0.**

Tam süitteki 2 kırmızı, sahibi Q10-2 hasım doğrulayıcı şeridi (`Kuyumcu.Tests/Dogrulayici/Q102SinirTestleri.cs`; o şeridin kiracısı `PAKETQ10-2-TEST Dogrulama` 22:01'de açılmış):
- `Dogrulayici.Q102EkranSinirTestleri.DQ102_onay_ozeti_4_haneli_grami_aynen_gosterir` ("9,1604" beklenirken "9,160 g" basılıyor)
- `Dogrulayici.Q102EkranSinirTestleri.DQ102_asim_mesajindaki_mevcut_bakiye_girilen_degerle_celismez`

İkisi de gram biçimi ve emanet/onay özetiyle ilgili; Cariler ya da bu paketin dosyalarıyla ilgisi yok. **Bu paketin yeni kırmızısı 0.**

Yeni test sayısı: 32 koşum (19 `[Fact]` + 3 `[Theory]`, 13 satır).

## 7. Mutasyon kanıtı
Yöntem: kaynak `scratchpad/mut-yedek/T-M1/` altına kopyalandı → `perl -0777 -pi` ile tek eşleşmeli değişiklik → `dotnet test --filter TM1MusterilerTuval` → yedek geri kopyalandı → `cmp`. 14 yedeğin 14'ü sonda kaynakla **AYNI**. Ardından temiz derleme: 0 hata, 0 uyarı.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M-TM1-1 | Borçlu `TlBakiye > 0` → `< 0` | TM1_01, TM1_14 | AYNI |
| M-TM1-2 | Toptancı `==` → `!=` | TM1_01, TM1_01b, TM1_14 | AYNI |
| M-TM1-3 | Vade rozeti `> 0` → `>= 0` | TM1_02 | AYNI |
| M-TM1-4 | vade 403 yakalayıcısı silindi | TM1_03 | AYNI |
| M-TM1-5 | Satır tıklaması etkisiz | TM1_04, 05, 06, 06b, 07, 07b, 07c | AYNI |
| M-TM1-6 | Telefon biçimi uygulanmıyor | TM1_11 (3 satır), TM1_02 | AYNI |
| M-TM1-7 | SAHİP kapısı kaldırıldı | TM1_07b | AYNI |
| M-TM1-8 | Yazma eylemi süzgeci kaldırıldı (OKUMA seçilir) | TM1_07 | AYNI |
| M-TM1-9 | "Kaldır" düğmesi katlanmıyor | TM1_06 | AYNI |
| M-TM1-10 | Sınır "100+" → "100" | TM1_10 | AYNI |
| M-TM1-11 | Band Tahsilat'ta panel kapanmıyor | TM1_05 | AYNI |
| M-TM1-12 | BASİT'te Borç rozeti geri geliyor | TM1_02 | AYNI |
| M-TM1-13 | Uyarı bandı `> 0` → `>= 0` | TM1_05 | AYNI |
| M-TM1-14 | "dün" kuralı `-1` → `-2` gün | TM1_13 | AYNI |
| M-TM1-15 (tur 2) | Band düğmesine tırnaklar geri kondu (`"@T.TahsilatDugmesi"`) | TM1_05 (32'de 1 kırmızı) | AYNI |
| M-TM1-16 (tur 2, tarayıcı) | CSS `.musteri-sade .veri-tablosu.musteri-tablo { min-width: 0; … }` silindi | bUnit CSS düzenini ölçemediği için kanıt tarayıcıda: 375 BASİT `#cari-liste-sar` 837/349, kaydırma 488 px, 6 Borç hücresi ekran dışında. Geri alındıktan sonra 349/349 ve 0 | AYNI |

Tur 2'de M-TM1-15'ten sonra temiz derleme yapıldı: ilk deneme başka bir şeridin eşzamanlı derlemesine takıldı (`CreateAppHost` … `Could not set file permission 755` / `ConvertDllsToWebCil`). Yeniden denemede 0 uyarı, 0 hata.

İki deneme geçersiz sayılıp yeniden koşuldu:
- M-TM1-9'un ilk denemesinde regex eşleşmedi ("DEGISIKLIK UYGULANMADI").
- M-TM1-10'un ilk denemesinde perl `$"` değişkeni derleme hatası üretti. M-TM1-14 başka bir şeridin eşzamanlı WASM derlemesine takıldı (`tmp-webcil … being used by another process`).

Her üçü de düzeltilip yeniden koşuldu; tablodaki sonuçlar geçerli koşumlardır.

## 8. Canlı doğrulama (API 5164, test kiracısı `PAKETT-M1-TEST Kuyumculuk`)
Fikstür API ile kuruldu: 5 cari, Mehmet'e `segment TOPTAN`, iki veresiye, Mehmet'e düzeltme (−151.300 TL / −22 has). Hasan'ın veresiye vadesi SQL ile 12 gün geri çekildi (yalnız kendi kiracımız).

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris | 200 | 200 |
| GET /cari?limit=100&aktif=true | 200; `segment` ve `guncellendi` dolu | 200; Mehmet Oz `TOPTAN`, 5 satırda `guncellendi` var |
| GET /raporlar/vade-takibi | 200, Hasan 12 gün | 200, `(Hasan Kaya, 12)`, `(Zeynep Demir, None)` |
| PATCH /cari/{hasan} `{notlar}` | 200, not yazılır | 200, "T-M1 canli not" |
| GET /cari/{hasan} | telefon verisi değişmemiş | 200, telefon `05324112233` (ekranda "0532 411 22 33") |
| GET /denetim?varlikTuru=Cari&varlikId=… | 200; en yeni kayıt OKUMA, sonra GUNCELLEME | 200: `OKUMA`, `GUNCELLEME` (Ayse Yilmaz), … Yazma süzgecinin gerekçesi bu |
| GET /denetim jetonsuz | 401 | 401 |

SQL (`cariler`, kendi kiracımız): Hasan Kaya `05324112233`, notlar "T-M1 canli not", `guncellendi` 19:01:35 UTC; Mehmet Oz `segment=TOPTAN`. `denetim_kayitlari` (Cari): OKUMA 72, EKLEME 5, GUNCELLEME 2.

Tarayıcıda (§ 5): Hasan satırında "Vade geçti" ve bant "Vadesi 12 gün geçti…". Son değişiklik "Ayse Y., bugün 20:59 · Kim ne yaptı" (SAHİP). Segment sayaçları doğru.

Temizlik:
- API (5164) ve vekil (5165) yalnız kendi PID'leriyle durduruldu; iki port da boş.
- Kiracı satırları `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi, `kiraci_davetleri`'nden 1 satır silindi. Kalan satır: 0.
- `tenants`: başlangıç **14**, son **15**. Fark, iş sırasında başka şeritlerin açtığı iki kiracıdan geliyor: `PAKETT-K2-TEST Kuyumculuk` (22:02) ve `PAKETQ10-2-TEST Dogrulama` (22:01). Bunlara dokunulmadı. `PAKETT-M1%` sayısı 0. QA-TEST kiracılarına dokunulmadı.

### 8b. Tur 2 canlı (API 5164, vekil 5165, yeni `PAKETT-M1-TEST Kuyumculuk` kiracısı, 22:3x)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler (sistem anahtarı ortam değişkeninden) → /auth/kayit → /auth/giris | 201/201/200 | 201/201/200 (ilk denemede anahtar ortamda yoktu: 503 "Sistem yönetimi uçları bu kurulumda kapalıdır". API anahtarla yeniden başlatıldı) |
| POST /cari ×6, PATCH segment ×2, veresiye ×3 | 201/200/200 | hepsi başarılı |
| GET /cari?limit=100&aktif=true | 200, uzun ad 76 karakter, TOPTAN | 200: `Kaya Kuyumculuk…` 76 karakter `TOPTAN`, `Mehmet Oz` `TOPTAN`, telefonlar ham (`05329998877`) |
| GET /raporlar/vade-takibi | 200, Hasan 12, uzun ad 12 | 200: `(Hasan Kaya, 12)`, `(Zeynep Demir, None)`, `(Kaya Kuyumculuk…, 12)` |
| GET /raporlar/vade-takibi jetonsuz | 401 | 401 |

SQL: `update hareketler … set vade_tarihi = curdate() - 12` yalnız kendi kiracımızda, 2 satır. Sonuç: Hasan Kaya 2026-09-12, uzun ad 2026-09-12, Zeynep 2026-10-24.

Temizlik (tur 2):
- API PID ve vekil PID `kill` ile durduruldu. 5164 ve 5165 dinleyicisi 0.
- `tenant_id` kolonu olan 70 tablodan silindi. İlk denemede `group_concat` 1024 karakterde kesildi ve SQL hatası verdi; `group_concat_max_len` artırılıp tekrarlandı.
- `kiraci_davetleri`'nden 1 satır ve `tenants`'tan silindi. 70 tabloda kalan satır toplamı 0.
- `tenants`: başlangıç **15**, son **15**. `PAKETT-M1%` = 0. QA-TEST ve diğer şeritlerin kiracılarına dokunulmadı.

## 9. denetim.sh
`./denetim.sh` → **DENETİM TEMİZ.** (288 ✓, 0 ✗). Tur 2 yeniden koşumu (22:58): rc=0, **DENETİM TEMİZ.**, 288 ✓, 0 ✗.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Tuval birebir (BASİT) | Sağlandı, farklar gerekçeli | § 4 tablosu + yan yana png |
| Mevcut DOM id'leri korunur | Sağlandı | `cari-arama`, `cari-yeni-ac`, `cari-tur-filtre`, `c-satir-*`, `c-*` eylemleri, `duz-*`, `cekmece-cari-*`, `c-pasife-al` aynı; S32/CariEkran/UX1C1 199/199 |
| Kaydet/POST/yetki/yasal kapılar değişmez | Sağlandı | TM1_06b, S32_11; sunucu dosyası değişmedi |
| Özellik silinmez | Sağlandı | Tür/pasif süzgeci "Diğer süzgeçler"de; kaldır "Daha fazla"da; satır içi telefon ve Alacak/Borç rozeti PRO'da; ⋯ menüsü aynı |
| 375'te yatay kaydırma 0 (QA-10) | Sağlandı (BASİT; tur 2'de iç kap dahil) | § 0, § 5b: 76 karakterlik ad + 2 rozet ile `#cari-liste-sar` 349/349, sayfa yatay 0. PRO iç kap kaydırması gerekçeli (§ 5b, § 11 madde 7) |
| Telefon "0532 411 22 33" biçimli, veri değişmez | Sağlandı | TM1_11, TM1_02, SQL |
| Para biçimi tek merkez (Bicim), testte sabit para metni yok | Sağlandı | `Bicim.Tl(-151300m)` ile karşılaştırma |
| Sozlukler hub'ına ve app.css'e dokunulmaz | Sağlandı | yeni `Sozlukler.MusteriTuval.cs`; CSS yalnız şerit dosyasında |
| Sabit renk yok, satır içi style yok | Sağlandı | CSS'te yalnız `var(--…)`; razor'a style eklenmedi |
| Ekranın tek birincil düğmesi | Sağlandı | "Yeni müşteri"; panelde Kaydet |
| Boş durum ve hata bandı | Sağlandı | TM1_01b (segment boş), TM1_03b (500 → bant), TM1_07c |
| Ölçüm tablosu + ekran görüntüsü (1440/1280/375 × BASİT/PRO × açık/koyu) | Sağlandı, K8 ve K2 gerekçeli | § 5 |
| Mutasyon kanıtı | Sağlandı | 14/14 kırmızı, cmp AYNI |
| Test kiracısı silindi | Sağlandı | § 8 |
| Yeni kırmızı üretilmedi | Sağlandı | § 6 |
| Migration | Gerekmedi | — |

## 11. Açık kalanlar
1. Tam süitte 2 kırmızı (§ 6). Tur 1'de sahibi Q10-2 idi, tur 2'de o ikisi yeşil. Tur 2'deki `Q105_B11` ve `Q105_C03`'ün sahibi Q10-5; dosyaları koşum sırasında değişiyordu.
2. `sikisma-olc.js` K8 kebab deseni "e-posta" sözcüğünü jargon sayıyor (yanlış pozitif; Q10-5 raporunda da var). Araç düzeltmesi orkestratörde.
3. Panelin tuvaldeki gibi üst barın altından başlaması için T-KABUK'un üst bar yüksekliğini bir belirteçle (ör. `--ust-bar-yukseklik`) yayınlaması gerekiyor. Yayınlanırsa bu şerit tek satırla `top`'a bağlayabilir.
4. Ortak Cekmece'nin "İptal" metni tuvalde "Vazgeç". Bunu değiştirmek bütün çekmeceleri etkileyen ortak bir karar (UX-S31).
5. Tuvaldeki satır eylemi çeşitlemesi (borçluda "Tahsilat", biz borçluysak "Ödeme", sıfırda "Satış") uygulanmadı. Satırda bugünkü "Tahsilat" kaldı. Ödeme ve satış akışları bu ekranda yok; eklemek davranış değişikliği olur ve kapsam dışında.
6. **Tezgâhtar'da vade rozeti (ekip oyu önerisi):** tuvalin kişisi rozeti görüyor, bizde görmüyor. Bunun için cari listesine rolden bağımsız ve dar bir alan eklenmeli (ör. `gecikmeGun`, kaynağı `VadeKapisi.AcikBorclar`). Bu bir yetki kararı (bugün rapor `MuhasebeUstu`'ya açık), karar Bertan ve Mehmet'e.
7. PRO görünümünde /cariler tablosu dar ekranda (375) KartListe kabının içinde yatay kayıyor (sayfa yatayı 0). Uç uzunlukta adla 1280'de de 54 px kayıyor. PRO bu pakette değişmedi; PRO tablosuna kart görünümü ayrı bir karar.
8. `sikisma-olc.js`'ye iç kaydırma kabı ölçümü (`.tablo-sar` scrollWidth > clientWidth) eklenmesi orkestratöre öneriliyor. Bu şeritte tara betiğine eklendi.

9. **(Tur 3) Ekip oyu, telefon zorunluluğu:** Tuvalde "Telefon *". Bugün sunucu da ekran da telefonu isteğe bağlı tutuyor (tutarlı). Zorunlu yapmak kayıt kuralı değişikliği olur (sunucu doğrulaması + ekran + mevcut telefonsuz kartlar). Karar Ramazan (PM) ve Bertan'da.
10. **(Tur 3) Tam süitteki 8 kırmızı ve denetim.sh P-37c ✗** T-KABUK (`Pages/Ayarlar.razor`) ve Q10-2 (`DQ102v2`) şeritlerine ait (§ 0-T3.2, § 0-T3.5).

## 12. Kurucuya / mali müşavire notlar
Mevzuat içeren bir hüküm kodlanmadı. Not yok.
