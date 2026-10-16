# PAKET DEGISIM-2 — Uygulama raporu

Taşkın (backend) + Ertuğ (frontend), Opus. 03.10.2026, tur 1. Dalga DEGISIM-2 · t2 · API 5162 / vekil 5163 / CDP 9912–9913 · `KUYUMCU_TEST_DB=kuyumcu_test_t2`. **Hasımsız paket** (yalnız görünüm) — kendi kanıtım bağlayıcı.
Kaynak: `PAKET-DEGISIM-GUNSONU-uygulama-raporu.md` + görevdeki tasarım hakemi t2 / hasım t2 bulguları.
Kanıtlar: `ham/DEGISIM-2-olcum/` (`A/` 66 ölçüm + görüntü, `C/` 12 ölçüm + görüntü, `canli/` API + SQL, `mut-degisim2.json`, `mut-tur2-yeniden.json`).

**Sunucu kodu, para hesabı, MASAK/VUK kimlik kuralı ve 30.000 nakit sınırı DEĞİŞMEDİ. Migration yok** (şema değişikliği istenmedi).

## 0. Önce: DEGISIM-GUNSONU raporuna "Tur 2" bölümü
`PAKET-DEGISIM-GUNSONU-uygulama-raporu.md` sonuna **"Tur 2"** bölümü eklendi: tur 2 kod değişiklikleri (Peşin ön dolum, tek adım kilit, 1280 ALDIM 2+1, TakasTevsikTutari, TakasOdemeVarsayilani üretimde, FarkGosterimi) ve MDG-21…30 mutasyonları. MDG-25 ve MDG-26 ağaçla birebir eşit kopyada (`DEGISIM-2/kok` + karşılaştırma görüntüsü `ana`, ikisi aynı anda `rsync`) YENİDEN koşuldu: ikisi de kırmızı verdi, **cmp AYNI**. MDG-27…30 da koşuldu (hepsi kırmızı, cmp AYNI). MDG-24 eşdeğer mutant olarak gerekçesiyle yazıldı (`PesinEsikUstu` erken dönüşü farkı zaten kapsıyor; davranış farkı yok).

## 1. Madde tablosu

| # | Bulgu | Önce | Sonra | Kanıt |
|---|---|---|---|---|
| V | [YÜKSEK] Vazgeç'ten sonra boş formda kırmızı "Satıcı tipi zorunludur." | `FisTakasSatici` dokunuş kümesi (`dokunulan`) fiş sıfırlansa da duruyordu → boş "Kim getirdi?" kırmızı | Dokunuş kaydı FİŞE özgü: model sıfırlanınca (`HurdaGirisi.Sifirla` → `Dokunuldu=false`) eski dokunuşlar geçersiz; yeni fişte ilk dokunuşta temizlenir. Hata yalnız dokunulunca/Kaydet denemesinde. BASİT metin etiketle aynı: **"Kim getirdi? seçin."** | `DG2_V_BASIT_Vazgec_…`, `DG2_V_onceki_fiste_…_DIRILMEZ` · MDG2-1/2/20 · canlı prob `degisim-vazgec`: `sonra: []` (12 ölçüm) · `A/foto/degisim-vazgec__BASIT_aydinlik_1280.png` |
| O | [ORTA] BASİT Değişim eşik altında ödeme türü hiç yazmıyor; eşikte seçici birden beliriyor | `FisOdemeKasa` TAKAS dalı BASİT+NAKIT'te yalnız kasa satırı | TAKAS her durumda ödeme bölümünü çizer: eşik altında Satış'taki salt okunur satır **"Ödeme türü: Nakit · sistem belirledi"**, eşikte aynı satır seçiciye döner (yerleşim zıplamaz). Gövdeye giden değer aynen `odemeTuru` | `DG2_O_…` · MDG2-5 · prob: dolu `odemeSade: "Nakit sistem belirledi"`, eşik `odemeSecici: true` · `A/foto/degisim-dolu__BASIT_aydinlik_1440.png` |
| O-2 | [ORTA] Değişim düğmesi "Kaydet", Satış "Kaydet ve yazdır" | — | **Bilinçli UYGULANMADI** (gerekçe § 2): takas kaydı yazdırılabilir belge üretmiyor (`FisYazdir.BelgeliTurler` = SATIS/SATIS_IADE/ALIS/ALIS_IADE; TAKAS yok); etiket yazdırma vaat edip yazdırmazsa T-S1 kuralı çiğnenir. Belge türü + çıktı şablonu = sunucu/çıktı işi → devir | § 2, § 9 |
| K7 | [ORTA] VERDİM arama kutusu ALDIM kutularıyla aynı hizada değil (±29) | arama kutusu etiket satırısız | BASİT'te VERDİM'e ALDIM alanlarıyla AYNI sarmalayıcı (`.alan`) + **"Ürün"** etiketi (`for=islem-barkod`), satır aralığı ALDIM ızgarasıyla aynı 10 px. Ölçüldü: etiket 249–268 = 249–268, kutu 278–322 = 278–322 (1440 ve 1280) | `DG2_H_…`, `DG2_C_…` · MDG2-10/18 · sikisma K7 = 0 (78/78 ölçüm) |
| K17 | [ORTA] Açıklama satırları / tekrarlar | "Müşteriye verilecek ürünü arayın…", "Ürün ve eski altın girilince fark hesaplanır.", "Eski altını getiren kişinin kimliği yazılır."; gün sonu aynı bilgiyi üç kez | VERDİM boş metni ve kimlik notu BASİT'te görsel gizli (ekran okuyucuda); "fark hesaplanır" satırı kaldırıldı (görsel gizliyken ölçer onu başlıkla üst üste sayıyordu, K9) → boş fark çubuğunun `title`'ı. Gün sonu açıklama satırı görsel gizli: kartta yalnız başlık satırı + eylem satırı ("9 satışın faturası yok · Fatura kes") | `DG2_H_…`, `DG2_K_…`, `DG2_G_BASIT_…` · MDG2-14/21 · prob `aciklamaGorunur:false`, `gorunenAciklama: []` · `A/foto/gunsonu__BASIT_aydinlik_1440.png` |
| T | [ORTA] Kimlik bölümünde terim karışık ("satıcı") | "Satıcı ad soyad / Satıcı TCKN / Satıcı adresi", kilit "Eksik: satıcı türü." | BASİT: **"Getirenin adı soyadı", "TC kimlik no", "Adresi"**; kilit **"Eksik: kim getirdi."**, TCKN kilidi "TC kimlik no" (etiketle aynı). PRO terimleri AYNEN. Koşullar (HurdaGirisi, MasakFormu) aynen | `DG2_K_…`, `DG2_K_bilesen_kilit_metni_…` · MDG2-3/4/22 · `A/foto/degisim-kimlik__BASIT_karanlik_375.png` |
| C | [ORTA] PRO gün sonu C: aynı tutar için iki sarı uyarı, ikincisi yanıltıcı; üç ad | API: "1 satış faturası hiçbir satış fişine bağlı değil (toplam 5.000)" + "Faturalanmamış satış ile fişe bağlanmamış fatura arasında 5.000 fark var" | YALNIZ GÖSTERİM (`Raporlar.GunSonu.cs GunSonuUyarilari`): fark tamamen bağsız faturadan geliyorsa (faturasız 0, fark = −bağsız) ikinci uyarı basılmaz; bağsız uyarısı sunucunun yapısal alanlarından TEK adla: **"1 satışa bağlanmamış fatura (toplam 5.000 ₺) — elle kesilmiş olabilir"**; tablo başlığı ve özet satırı "Satışa bağlanmamış faturalar". Karışık farkta ikinci uyarı AYNEN kalır. Tutar hesabına dokunulmadı | `DG2_G_PRO_C_…`, `DG2_G_PRO_fark_KARISIKSA_…` · MDG2-11/12/16 · canlı § 4 · `C/foto/gunsonu-pro-C__PRO_karanlik_1280.png` |
| D1 | [DÜŞÜK] PRO bağsız fatura tablosu dolgusu ~5 px, FATURA NO boş | — | Kart dolgusu (1.5rem / dar ekranda 1rem) bu tabloya da; numarasız faturada "—" | `DG2_G_PRO_C_…`, `DG2_C_…` · MDG2-15 · görüntü C 1280 koyu |
| D2 | [DÜŞÜK] PRO stat kartlarında iki satırlık etiket değeri itiyor; 1280'de 5. kart yalnız | `auto-fit minmax(200px)` | `#mut-kartlar` beş kart geniş ekranda tek satır (`minmax(min(100%, 9.5rem), 1fr)`), etiket satırı sabit iki satır yüksekliği; telefonda tek sütun (app.css kuralı aynen). Ölçüldü: 1440 ve 1280'de 5 kartın üstü 477, değer üstü 530 (hepsi eşit) — A ve C senaryosu | `DG2_C_…` · MDG2-17 · prob `kartlar` |
| D3 | [DÜŞÜK] BASİT↔PRO tutarsızlık: fark rengi, ad | BASİT kırmızı / PRO yeşil, boş formda yeşil "—" | TEK renk anlamı: boş = nötr, açık fark = uyarı (amber), kapalı (0) = yeşil — iki modda (`FisTakasTuval.FarkDurumu` tek kaynak, PRO `FisTakas.FarkRenkSinifi` onu okur). Ad (BASİT "Değişim", PRO "Takas") **bilinçli aynen**: PARA-DEMO-2/t3 kararı — BASİT karo adı sade ad, PRO mesleki terim ("PRO terimi aynen kalsın" bulgunun kendi kuralıyla tutarlı) | `DG2_R_…`, `DG_T_BASIT_fark_…` · MDG2-7/8/9/19 · görüntüler dolu BASIT/PRO |
| D4 | [DÜŞÜK] Kasa iki biçimde | eşik altı "Ana Kasa · Tek kasa var — otomatik seçildi", eşik üstü rozetli | BASİT Değişim'de her durumda rozetli kısa hâl "Ana Kasa [sistem belirledi]" (O ile aynı dal) | `DG2_O_…` · MDG2-5 · prob `kasa` |
| D5 | [DÜŞÜK] "Ayrıntı 1/2" sayacı belirsiz | çıplak sayı rozeti | **"Ayrıntı (2 uyarı)"**, rozet yok; sayı süzülmüş uyarı listesinin sayısıdır | `DG2_G_BASIT_…`, `S34_10` (beklenti güncellendi) · MDG2-13 · görüntü gunsonu BASIT |
| D6 | [DÜŞÜK] KAPSAM DIŞI: PRO Takas eski uzun form | — | Dokunulmadı (bulgu "ayrı paket" diyor) → devir § 9 | — |
| D7 | [DÜŞÜK] BASİT "Ödeme Türü" büyük harf | ortak etiket | YALNIZ BASİT Değişim'de cümle düzeni **"Ödeme türü"** (`FisOdemeKasa.OdemeEtiketi`); diğer türler AYNEN (tek dokunuş; Satış/Alış'ın ortak etiketi ayrı görünüm turu) | `DG2_O_…` · MDG2-6 |

## 2. Sapmalar (gerekçeli)
- **"Kaydet ve yazdır" Değişim'e getirilmedi** (O-2). Tur 1'in gerekçesi doğrulandı: `POST /islemler/takas` yanıtı ve `FisYazdir.HareketId` TAKAS türünü yazdırılabilir saymıyor; çıktı şablonu yok. Etiket değişikliği tek başına "yazdırıyor" yalanı olur (T-S1, Anayasa md.1). Hasımsız/görünüm paketinin sınırı dışında (belge türü + çıktı + muhtemelen gider pusulası çıktısı = veri/yasal belge) → **hasımlı/çıktı paketine devir**. Ödeme satırı kısmı (bulgunun ikinci yarısı) yapıldı.
- **PRO "Takas" adı aynen** (D3): mevcut ürün kararı; değiştirmek FisTurleri'nin ortak adını (Satış/İşlem defteri…) etkiler, kapsam dışı.
- **Görsel gizli yerine kaldırma (fark-bekle)**: ölçer `.gorsel-gizli` öğeyi VERDİM başlığıyla üst üste saydı (K9 1). Satır kaldırıldı, metin boş çubuğun `title`'ında (anahtar boşa düşmedi).
- **Dosya alanı**: Raporlar.razor yalnız gün sonu kartı + gün sonu sekmesi; ortak `FisOdemeKasa.razor`'da yalnız TAKAS koşulu + TAKAS/BASİT etiketi; `S34RaporBugunTestleri.cs` ve `ParaDemo2EkranTestleri.cs`'te yalnız kasıtlı metin değişikliğinin beklentisi (önce-yedekleri `/private/tmp/kuyumcu-serit/DEGISIM-2/`).

## 3. Mevzuat
Yeni yasal kural/eşik yazılmadı. MASAK/VUK md.234 kimlik koşulları (HurdaGirisi, MasakFormu.Eksiksiz) ve nakit sınırı kapısı aynen; yalnız etiket/kilit ADLARI ve hata gösterim ZAMANI değişti. Sayı istemciye gömülmedi (had sunucudan). Canlıda sınır davranışı aynen (§ 4).

## 4. Canlı doğrulama (API 5162 Release yayın, vekil 5163; kiracı `PAKETDEGISIM-2-TEST Kuyumculuk` 01M3ZJKNCYTHKY753EPWXMJ3EG, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış)

| Adım | Beklenen | Gerçek | Dosya |
|---|---|---|---|
| A: `GET /raporlar/pos-fatura-mutabakati` | fark = faturasız 98.766,52; tek uyarı | 200, fark 98766.52, uyarı "9 satış faturalandırılmamış (toplam 98.766,52 ₺)"; ekran BASİT "Faturası kesilmemiş satış: 98.766,52 ₺" + "9 satışın faturası yok · Fatura kes" | `canli/api-A.json` |
| Takas 30.000,01 NAKIT | 400 NAKIT_SINIRI (sınır aynen) | **400** `NAKIT_SINIRI` "30.000,01 TL nakit, 30.000,00 TL nakit sınırını 0,01 TL aşıyor…"; SQL satır yok | `canli/api-takas.txt`, `sql-takas.txt` |
| Takas 30.000,00 NAKIT | 200 | **200** (GP-2026-000001); SQL `TAKAS NAKIT 30000.00`, 30000.01 satırı 0 | aynı |
| C kurulumu: 9 fiş `POST /faturalar/fisten` + bağsız `POST /faturalar` 5.000 | 200 | hepsi **200**; bağsız faturanın `faturaNo` null | `canli/api-C-fisten.txt`, `api-C-bagsiz.txt` |
| C: mutabakat | API iki uyarı; ekran tek uyarı, tek ad | API 200: fark −5000, faturasız 0, bağsız 1·5000, uyarılar = [bağsız, "…arasında 5.000 fark var"]. SQL: satış 98.766,52 − satış faturası 103.766,52 = −5.000 (API ile aynı). Ekran PRO (12 ölçüm): tek uyarı "1 satışa bağlanmamış fatura (toplam 5.000 ₺) — elle kesilmiş olabilir", tablo "Satışa bağlanmamış faturalar", FATURA NO "—" | `canli/api-C.json`, `sql-C.txt`, `C/olcum.jsonl` |

Kapanış: API (PID 14977 son) ve vekil `kill`; 5162/5163/9912/9913 dinleyici yok. `zsh kiraci-temizle.sh 01M3ZJKNCYTHKY753EPWXMJ3EG --uygula` kod 0; SQL: bu kimlik için tenants 0, hareketler 0, faturalar 0. tenants başlangıç **16** → son **17**: fark bu paketin değil (temizlik çıktısının listelediği eşzamanlı şerit kiracıları PAKETTOPLU-6-TEST, PAKETMADEN-2-TEST-DOG vb. arada açıldı); bu paket +1/−1. QA-TEST-* kiracılarına dokunulmadı. Fikstür ve anahtar dosyaları `printf ''` ile boşaltıldı.

## 5. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma-olc.js`, parolasız oturum `inceleme-vekili.py`)
Matris: BASİT {gün sonu, Değişim boş, dolu (fark 3.933,98 — eşik altı), eşik (38.933,98), Vazgeç, kimlik (Vatandaş), MASAK (146.749,10 iade)} · PRO {gün sonu sekmesi, Değişim boş, dolu, MASAK} × {1440, 1280, 375} × {açık, koyu} + C senaryosu gün sonu (BASİT+PRO × 3 genişlik × 2 tema).

**Tur `A/` — 66 ölçüm, hata 0, mod uyuşmazlığı 0**

| Senaryo | Mod | Tema | 1440 | 1280 | 375 |
|---|---|---|---|---|---|
| gunsonu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-esik | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-vazgec | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-kimlik | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-esik | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-vazgec | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-kimlik | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |

**Tur `C/` — 12 ölçüm, hata 0, mod uyuşmazlığı 0**

| Senaryo | Mod | Tema | 1440 | 1280 | 375 |
|---|---|---|---|---|---|
| gunsonu-C | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-C | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro-C | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro-C | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K7 0 · K8 0 · K9 0 · Y 0 |

Hedefli prob sonuçları (A, `olcum.jsonl`): Vazgeç sonrası görünen hata 0 (12/12); dolu BASİT ödeme "Nakit sistem belirledi", kasa "Ana Kasa sistem belirledi", fark sınıfı `dg-fark-acik`, boş `dg-fark-bos`; eşikte seçici açık, kasa aynı biçim; kimlik etiketleri "Getirenin adı soyadı / TC kimlik no / Adresi", kilit "Eksik: TC kimlik no, adres."; PRO kartlar 1440/1280 tek satır, değer üstü eşit (530).
Read ile bakılan görüntüler: `A/foto/degisim-dolu__BASIT_aydinlik_1440.png`, `A/foto/degisim-vazgec__BASIT_aydinlik_1280.png`, `A/foto/gunsonu__BASIT_aydinlik_1440.png`, `A/foto/gunsonu-pro__PRO_karanlik_1280.png`, `A/foto/degisim-dolu__PRO_aydinlik_1440.png`, `A/foto/degisim-kimlik__BASIT_karanlik_375.png`, `C/foto/gunsonu-pro-C__PRO_karanlik_1280.png`, `C/foto/gunsonu-C__BASIT_aydinlik_1440.png`.
Ara ölçüm (deneme): ilk sürümde K7 1 (kutu ±5) ve K9 1 (görsel gizli satır) görüldü → etiket `.alan` sarmalayıcısına alındı + satır aralığı 10 px, fark-bekle satırı kaldırıldı; son matriste ikisi de 0.

## 6. Testler
Yeni: `Kuyumcu.Tests/DegisimIkiTestleri.cs` — 12 test metodu / 12 koşum, 4 sınıf (`DegisimIkiEkranTestleri` 7, `DegisimIkiGunSonuTestleri` 3, `DegisimIkiSafTestleri` 1, `DegisimIkiCssTestleri` 1).
Beklentisi güncellenen (kasıtlı görünüm değişikliği): `DegisimGunSonuTestleri.cs` (`DG_T_BASIT_fark_…` sınıf adı, `DG_T_BASIT_degisim_VERDIM_ALDIM_…` bekleme satırı → title), `S34RaporBugunTestleri.S34_10_…` ("Ayrıntı (1 uyarı)"), `ParaDemo2IslemEkranTestleri.PD2E_8_takas_kilit_…` ("TC kimlik no").
Filtreli (son kod): Degisim + DEGISIMGUNSONU + ZB34 + S34 + PDE1 + Toplu5 + T17_Takas + KesifP5 + TK2 + ParaDemo + SATIS3Ekran + S3dEkran + S24EskiAltin + Takas + Hurda → **901/902 başarılı, 1 atlandı (önceden var: `DOG_T24_SONDA_…` Skip), 0 başarısız**.
**Tam süit (son kod, gerçek ağaç, BİR kez, `trx-pDEGISIM-2`): 10063 test · 10057 geçti · 4 başarısız · 2 atlandı.** Dört kırmızının hiçbiri bu paketin dosyasında değil — hepsi eşzamanlı TOPLU-6 şeridinin (listeler sınır bandı, Kur) yarım işi: `UX1C1EkranTestleri.Etiketler_sunucu_SINIRINI_ekranda_soyler` ("2 kayıt gösteriliyor" içinde "500" yok), `S33UrunlerEkranTestleri.S33_10_Durum_suzgeci_…` ("3 kayıt gösteriliyor" içinde "50" yok), `Toplu6KurTestleri.T6_Kur_sayisal_sutun_basliklari_…` (BASIT + PROFESYONEL; başlık sınıfı boş). Bu paket Etiketler/Urunler/Kur sayfalarına ve ortak liste bileşenine dokunmadı (`Urunler.razor` 03:34, `Kur.razor` 03:32, `Toplu6Testleri.cs` 03:52 TOPLU-6 tarafından değişti). Tur 1'in tek kırmızısı (`Yasal4…` saat dilimi) bu koşumda yeşil. **Bu paketin yeni kırmızısı: 0.**

## 7. Mutasyon tablosu (DEGISIM-2; yöntem: ağacın kopyası `DEGISIM-2/mk` + karşılaştırma görüntüsü `ana2` → yedek → TEK eşleşmeli değişiklik → derle → filtreli test (Degisim|DEGISIMGUNSONU|S34|ParaDemo2IslemEkran, 162–163 test) → geri yükle → `cmp` yedekle VE görüntüyle; betik `mut4.py`, sonuç `ham/DEGISIM-2-olcum/mut-degisim2.json`)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| MDG2-1 | `FisTakasSatici` — Goster eski dokunuşu sayar | DG2_V_BASIT_Vazgec_sonrasi_bos_formda_kim_getirdi_hatasi_YOK_dokununca_etiket_adiyla | AYNI |
| MDG2-2 | `FisTakasSatici` — BASİT hata yine "Satıcı tipi zorunludur." | DG2_V_BASIT_Vazgec_… | AYNI |
| MDG2-3 | `Islemler.ParaDemo3` — BASİT kilit yine "satıcı türü" | DG2_K_BASIT_kimlik_etiketleri_getiren_dili_kilit_kim_getirdi_PRO_AYNEN | AYNI |
| MDG2-4 | `FisTakasSatici` — BASİT ad etiketi yine "Satıcı ad soyad" | DG2_K_BASIT_kimlik_… | AYNI |
| MDG2-5 | `FisOdemeKasa` — TAKAS eski koşul (eşik altında ödeme satırı yok) | DG2_O_BASIT_esik_altinda_salt_okunur_Odeme_turu_Nakit_esikte_secici_kasa_TEK_bicim | AYNI |
| MDG2-6 | `FisOdemeKasa` — etiket yine "Ödeme Türü" | DG2_O_… | AYNI |
| MDG2-7 | `FisTakasTuval` — açık fark yine kırmızı | DG2_R_fark_rengi_iki_modda_TEK_anlam_bos_notr_acik_uyari_kapali_yesil, DG_T_BASIT_fark_ALDIM_altinda_yon_ve_tutarla_odeme_bolumu_acilir | AYNI |
| MDG2-8 | `FisTakasTuval` — boş fark yeşil "tamam" | DG2_R_… | AYNI |
| MDG2-9 | `FisTakas` — PRO fark yine yön rengi | DG2_R_… | AYNI |
| MDG2-10 | `FisTakas` — VERDİM etiketi `.alan` dışında | DG2_H_BASIT_VERDIM_arama_etiket_satiri_tekrar_aciklamalar_gorsel_gizli_PRO_etiket_YOK | AYNI |
| MDG2-11 | `Raporlar.GunSonu` — C'de ikinci uyarı geri | DG2_G_PRO_C_fark_tamamen_bagsizdan_IKINCI_uyari_YOK_bagsiz_TEK_ad_fatura_no_bos_tire, DG2_G_BASIT_aciklama_gorsel_gizli_notlar_basligi_uyari_SAYISIYLA_rozet_YOK | AYNI |
| MDG2-12 | `Raporlar.GunSonu` — bağsız uyarı yine sunucu adıyla | DG2_G_PRO_C_…, DG2_G_PRO_fark_KARISIKSA_ikinci_uyari_AYNEN_kalir_uydurma_yok | AYNI |
| MDG2-13 | `Raporlar.razor` — "Ayrıntı" + çıplak sayaç | DG2_G_BASIT_…, S34_10_Gun_sonu_FARK_VAR_ise_tutar_ve_DUZELTME_baglantisi_cizilir | AYNI |
| MDG2-14 | `Raporlar.razor` — açıklama satırı görünür | DG2_G_BASIT_… | AYNI |
| MDG2-15 | `Raporlar.razor` — boş fatura no hücresi boş | DG2_G_PRO_C_… | AYNI |
| MDG2-16 | `Raporlar.razor` — tablo başlığı yine "Fişe bağlanmamış faturalar" | DG2_G_PRO_C_… | AYNI |
| MDG2-17 | css — kart ızgarası kuralı yok (1280 4+1) | DG2_C_fark_renkleri_belirtecle_kartlar_tek_satir_bagsiz_tablo_dolgusu | AYNI |
| MDG2-18 | css — VERDİM satır aralığı 4 px (hizasız) | DG2_C_… | AYNI |
| MDG2-19 | css — açık fark tehlike renginde | DG2_C_… | AYNI |
| MDG2-20 | `FisTakasSatici` — önceki fişin dokunuşu temizlenmez | DG2_V_onceki_fiste_dokunulan_alanin_hatasi_yeni_fiste_DIRILMEZ | AYNI |
| MDG2-21 | `FisTakasSatici` — BASİT kimlik notu görünür | DG2_K_BASIT_kimlik_… | AYNI |
| MDG2-22 | `FisTakasSatici.EksikMetni` — BASİT yine "satıcı türü" | ilk koşumda KIRMIZI YOK (test açığı) → `DG2_K_bilesen_kilit_metni_BASIT_alan_adiyla_PRO_AYNEN` eklendi → yeniden koşum: o test kırmızı | AYNI |

Tur 2 (DEGISIM-GUNSONU) mutasyonları MDG-21…30: § 0 ve DEGISIM-GUNSONU raporu "Tur 2" bölümü (9/9 kırmızı, cmp AYNI; MDG-24 eşdeğer mutant).
Son mutasyondan sonra kopyada değil gerçek ağaçta temiz `dotnet build Kuyumcu.sln` (§ 8).

## 8. Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (gerçek ağaç, son kod, mutasyonlar kopyada koşuldu): **0 Uyarı, 0 Hata**. (Ara derlemelerde görülen 2 uyarı başka şeritlerin test dosyalarındaydı: `SUBEKAPSAMASinirTestleri.cs` CS8601, `KesifP2Testleri.cs` xUnit2029 — son derlemede yok.)
- `bash denetim.sh`: **302 ✓, 2 ✗**: (1) `P-30 DEMO_INSAN_SURESI` (kabulde hariç); (2) `Pages: @onclick'siz ve disabled'sız <button> 72 (tavan 71)` — **bu paketin değil**: aynı sayaç paket öncesi anlık görüntüde (`DEGISIM-2/ana`) 71, ağaçta 72; fark dosya bazında yalnız `Pages/Siparisler.razor` 2 → 3 (TOPLU-6 şeridinin dosyası; bu paket Pages altında hiç `<button>` eklemedi). Bu paketin sayaçları ✓: ModalPencere 48 (tavan 55), duzenlenenId 1 (1), satır içi style 57, SABIT_RENK 0, OLCEK-1 0.

## 9. DoD sözleşmesi
Yeni varlık / kayıt türü / uç YOK. Değişim kaydı değişmez finansal kayıttır (düzeltme storno/iade, DegismezIpucu aynen); kaydedilmemiş fiş için Vazgeç (istek atmaz, artık dokunuş durumunu da sıfırlar). Gün sonu raporu türetilmiş veridir (düzeltme yolu "Fatura kes" / "Faturalara bak" aynen). Yeni metinler `Sozlukler.Degisim.cs` (TR+EN, konumsal record). Yeni CSS yalnız belirteç. Mevcut id'ler korundu; yeni id `#takas-verdim-etiket`; kaldırılan `#takas-tuval-fark-bekle` (satır kaldırıldı, metin title). Yeni düğme yok.
Dokunulan sayfalar/bileşenler (TEK DOKUNUŞ): `Pages/Raporlar.razor` (yalnız gün sonu kartı + sekmesi), `Pages/Raporlar.GunSonu.cs`, `Pages/Islemler.ParaDemo3.cs`, `Bilesenler/Fis/FisTakas.razor`, `FisTakasTuval.razor`, `FisTakasSatici.razor`, `FisOdemeKasa.razor` (yalnız TAKAS dalı/etiketi — gerekçe: bulgu O ortak bileşenin TAKAS koşulunda). Hepsi DEGISIM-GUNSONU dalgasının ikinci dokunuşu; gerekçe = bu paketin kendisi (DEGISIM-GUNSONU tur 2 hakem/hasım bulguları).

## 10. Değişen dosyalar
Yeni: `dotnet/Kuyumcu.Tests/DegisimIkiTestleri.cs`, bu rapor, `ham/DEGISIM-2-olcum/`.
Değişen: `dotnet/Kuyumcu.Core/I18n/Sozlukler.Degisim.cs`, `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`, `Pages/Raporlar.GunSonu.cs`, `Pages/Islemler.ParaDemo3.cs`, `Bilesenler/Fis/FisTakas.razor`, `FisTakasTuval.razor`, `FisTakasSatici.razor`, `FisOdemeKasa.razor`, `wwwroot/css/degisim-gunsonu.css`; testler (yalnız beklenti): `DegisimGunSonuTestleri.cs`, `S34RaporBugunTestleri.cs`, `ParaDemo2EkranTestleri.cs`; rapor: `PAKET-DEGISIM-GUNSONU-uygulama-raporu.md` (Tur 2 bölümü).
(`git status --porcelain | grep -v -E "/(bin|obj)/"` bu ağaçta bu dosyaların çoğunu `??` gösteriyor — ilk commit sonrası izlenmiyor; `Raporlar.razor` ` M`. Liste elle tutuldu.) COMMIT yok, dosya silme yok.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Önce: DEGISIM-GUNSONU raporu Tur 2 bölümü + MDG-25 ağaçla eşit kopyada cmp AYNI + MDG-26..30 | Sağlandı | § 0 |
| Vazgeç sonrası boş formda hata YOK; hata dokunuş/Kaydet sonrası, etiket adıyla | Sağlandı | V |
| BASİT eşik altında "Ödeme türü: Nakit" salt okunur; kasa tek biçim | Sağlandı | O, D4 |
| "Kaydet ve yazdır" | Gerekçeyle uygulanmadı (yazdırılacak belge yok) — devir | § 2 |
| K7 hiza | Sağlandı (K7 0/78) | K7 |
| K17 tekrar satırları | Sağlandı | K17 |
| Kimlik tek dil (BASİT), PRO aynen | Sağlandı | T |
| PRO C ikinci uyarı yok, tek ad, tutar hesabına dokunulmadı | Sağlandı | C, § 4 |
| Bağsız tablo dolgusu + "—"; kart ızgarası/etiket yüksekliği | Sağlandı | D1, D2 |
| Fark rengi iki modda tek anlam; boşta nötr | Sağlandı | D3 |
| "Ayrıntı (N uyarı)" | Sağlandı | D5 |
| MASAK/VUK kimlik kuralı ve 30.000 sınırı değişmedi | Sağlandı | § 3, § 4 (400/200) |
| Başsız Chrome 1440/1280/375 × BASİT+PRO × açık+koyu, Read | Sağlandı | § 5 (66 + 12) |
| Her değişiklik için test + mutasyon, cmp AYNI | Sağlandı (22/22) | § 7 |
| build 0/0 | Sağlandı | § 8 |
| denetim TEMİZ (P-30 hariç) | Bu paketin sayaçları ✓; tek ek ✗ TOPLU-6'nın `Siparisler.razor` düğmesi (ağaç ölçüsü, orkestratör kapısı) | § 8 |
| Tam süit 0 yeni kırmızı | Sağlandı (4 kırmızı TOPLU-6'nın) | § 6 |
| Test kiracısı silindi | Sağlandı | § 4 |
| Bellek: en çok 1 API + 1 başsız Chrome; tam süit bir kez | Uyuldu (prob Chrome'u ölçüm Chrome'uyla aynı anda açılmadı) | |

## 12. Açık kalanlar / devir
- **Değişim fişi yazdırma ("Kaydet ve yazdır")**: TAKAS için belge türü + çıktı şablonu yok → çıktı/sunucu (hasımlı) paketi.
- **PRO Takas formu** (eski uzun form, Vazgeç yok, kilit "gider pusulası için gerekli (VUK md.234)") → ayrı PRO düzenleme paketi (bulgu D6).
- **Ortak "Ödeme Türü" etiketi** Satış/Alış/Hurda BASİT'te hâlâ title-case → ortak bileşen görünüm turu.
- PRO kartta fark adı "Satıştan fazla kesilen fatura" ile bağsız fatura "Satışa bağlanmamış fatura" aynı tutarı gösterebilir (C senaryosunda ikisi de 5.000): biri FARK'ın, öteki fatura kümesinin adı — kavram farklı olduğu için birleştirilmedi; ürün kararı istenirse tek satır.
- Gün sonu uyarı süzgeci sunucunun Türkçe uyarı METNİNE (iki kalıp, `Raporlar.GunSonu.cs`) dayanır: RaporModulu metni değişirse süzgeç sessizce etkisizleşir (uyarı yine görünür, kaybolmaz). Kalıcı çözüm: sunucunun uyarıları kodlu döndürmesi (RaporModulu — MADEN-2 şeridinin dosyası; hasımlı devir).
- **Başkasına ait kırmızılar (sahibiyle)**: TOPLU-6 — `UX1C1EkranTestleri.Etiketler_sunucu_SINIRINI_ekranda_soyler`, `S33UrunlerEkranTestleri.S33_10_…`, `Toplu6KurTestleri.T6_Kur_sayisal_sutun_basliklari_…` (×2); denetim `Pages: @onclick'siz … <button>` 72/71 (`Siparisler.razor`).

## 13. Kurucu / mali müşavir
Yeni soru yok. Mevcut açık soru aynen: takasta 459 s. VUK GT işlem tutarı kuralının (fişin toplamı) uygulanıp uygulanmayacağı — bugün yalnız el değiştiren NAKİT FARK denetlenir; bu paket bu davranışı değiştirmedi.
