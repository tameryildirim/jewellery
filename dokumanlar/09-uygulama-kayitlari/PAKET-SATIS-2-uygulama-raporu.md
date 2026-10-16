# PAKET SATIS-2 — Satış ekranı = tuval + Pusula sadeliği (CUMA-S birleşik) · uygulama raporu (r2)

Tarih: 25.09.2026 · Şerit v2 · test DB kuyumcu_test_t2 · canlı API 5162 / vekil 5163 · CDP 9900–9914 · COMMIT YOK.
Rol: Taşkın (backend — değişiklik gerekmedi) + Ertuğ (frontend).
Bu bölüm (r2) **önceki doğrulamada kalan bulguları** kapatır; r1 raporu aşağıda (§ R1) olduğu gibi durur, r2'nin
değiştirdiği yerler orada "⟶ r2" notuyla değil, bu bölümdeki tablolarla geçersiz kılınır.

## R2-0. Devralma
- Ağaç: önceki SATIS-2 koşusunun dosyaları (Katlanir, Fis/*, tuval-satis.css, SatisSade sözlüğü, SATIS2 testleri) yerinde.
  `mut-yedek/SATIS-2/*.orj` r1'de cmp'lenmişti; r2'nin tüm mutasyonları da cmp BİREBİR (aşağıda).
- 5162/5163 başlangıçta boştu. PAKETSATIS-2-TEST kiracısı yoktu (r1 silmişti). tenants başlangıç: **17**.
- `Pages/Islemler.Sade.cs.tmp` (0 bayt, r1 öncesinden) hâlâ duruyor — dosya silme yasak/hook blokluyor → **kurucu silmeli**.

## R2-1. Bulgu → çözüm (hakem çelişkisi dahil)
| # | Bulgu (doğrulayıcı) | Kök neden (ölçülerek) | Çözüm | Kanıt (gerçek tarayıcı, dolu fiş) |
|---|---|---|---|---|
| 1 | [ORTA] PRO 960/1024 dolu fişte ÜRÜN kolonu 0 / 35 px; 1440'ta ad ve barkod kesik | r1'in 960/1024 ölçümü **boş fişte** yapılmıştı (hakem çelişkisi doğru: "hepsi 0" dolu fişte ölçülmemişti). Yeniden üretildi: 960 ürün 0 px, 1024 50 px, 1440 128 px + 3 kesik hücre (barkod ×2, "925 Gümüş Bileklik"). Sabit kolonlar 656 px; Range ölçüsüyle içerik: barkod 13 hane 149 px (16 px yazı), ürün 149, gram 47, has 62, adet 43 (375'te 52), işçilik 65, tutar 101 | `tuval-satis.css` r2 bloğu: barkod hücresi 14 px tabular + kolon 134; gram 56 · has 64 · işçilik 70 · tutar 112 (adet 52 kalır); ürün adı satır kırar (kesilmez, title tam ad); tablo en az 780 px (≤1100 px pencerede 800 px) ve **kalem kartı kendi içinde yatay kaydırılır** (sayfa yatay kaydırması 0) | 1440 ürün **142 px**, 1280 320, 1024/960 **158**, 768/375 218; kesik hücre **0**, başlık binmesi 0 — tüm {açık,koyu} (tablo aşağıda). `olc3.mjs`'e "dolu fişte ürün kolonu genişliği + kesik hücre + başlık binmesi" kontrolü EKLENDİ |
| 2 | [ORTA] 960 PRO özet açılınca Fiyat listesi kutusu kırpılıyor (h 32, scrollHeight 36 > clientHeight 30) | Kutu iki satır (ad + "varsayılan") taşır; **OLCEK-1 şeridinin** `olcek.css` kuralı kutuyu `height: var(--kontrol-yuksekligi)` yapıyor, **sık yoğunlukta 2rem = 32 px**. Doğrulayıcının sayıları BİREBİR yeniden üretildi (kural devre dışı + sık: h 32 · sh 36 · ch 30 · ad ve rozet taşıyor) | Kendi dosyamda (`tuval-satis.css`, iki id'li daha özgül seçici) kutu `height:auto; min-height: kontrol yüksekliği; overflow: visible`, ad/rozet sola yaslı iki satır | Özet AÇIK ölçüm, sık yoğunluk: 960/1024 h 49 · sh 47 = ch 47 · taşan çocuk 0; normal yoğunlukta 1440–375 `flKirpik` hep False. Görüntü: `SATIS-2-r2-dolu-960-1024.png` (3. ve 4. kare) |
| 3 | [ORTA] BASİT'te "Son 3 satış" ilk yükleme hata verirse yeniden deneme yolu yok | Katlanir `yuklendi=true` IlkAcilis'ten önce; BASİT dalında Yenile yok | Katlanir'e `Hatali` parametresi: son yükleme hatalıysa kart yeniden açılınca **IlkAcilis tekrar çağrılır** (hata yokken yine BİR KEZ). FisSonSatislar BASİT dalına hata varken `#islem-son-tekrar` "Tekrar dene" (sözlük `SatisSadeSozlugu.TekrarDene`, TR+EN) | bUnit: `Katlanir_HATALI_yuklemeden_sonra_yeniden_acilista_IlkAcilis_TEKRAR_cagrilir`, `BASIT_son_satis_HATASINDA_hata_bandi_ve_Tekrar_dene_istek_atar_kapat_ac_da_yeniden_dener` (hata bandı görünür, "kayıt yok" çıkmaz; Tekrar dene → 2. istek; kapat-aç → 3. istek; düzelince liste gelir, düğme ve bant kalkar) |
| 4 | [ORTA] 375 PRO arama kutusu yer tutucusu kesik (iç 99 px, metin 229 px) | Arama kutusu "Elle ekle" için 7rem sağ pay + kartın 16 px iç payı + girişin 16 px dış payı | ≤600 px: arama kutusu tam genişlik, "Elle ekle" kutunun ALTINA iner, giriş bloğu dış payı 0, arama ikonu payı 40/8 px (OLCEK'i ezmek için `#islem-barkod` id'li seçici) | 375 PRO iç genişlik **275 px ≥ 229** (yer tutucu kesik False, açık/koyu, boş/dolu); görüntü `SATIS-2-r2-dar-koyu.png` 1. kare |
| 5 | [ORTA] Tam süitte 83 kırmızı (BICIM-2, ISTEK-1) | — | Bu paketin değil. r2 ara koşum (bu paketin kodu, 15:0x): **6236/6236 yeşil** (BICIM-2 düzelmiş). Son koşum: aşağıda R2-5 | — |
| 6 | [DÜŞÜK] P-23 ODEME_KALEMSIZ_YOL çok kalemli satis-fisi'de canlı ✗ | backend (L-1 / P-23) | Bu paket backend'e dokunmadı; son denetimde (kiracı silindikten sonra) P-23 canlı 0. Sahibine not (§ R2-8) | — |
| 7 | [DÜŞÜK] `Islemler.Sade.cs.tmp` 0 bayt | — | Hook silmeyi blokluyor → kurucu silmeli | — |
| 8 | [DÜŞÜK] BASİT sağ sütunda nakit eşiği uyarısı sürekli duruyor | Kurucu 11:35 kuralıyla gerilimli | **Bu pakette kapatıldı** (madde 6 kalıbı): `FisNakitUyari` yalnız fiş toplamı sunucu eşiğinin `FisProYanSutun.YaklasmaOrani`'nı (0,8 — yalnız EKRAN oranı, yasal hüküm değil) geçince ya da eşik henüz bilinmiyorken (tutucu) görünür; aksi hâlde DOM'da `[hidden]` (metin silinmez, eşik sayısı yine sunucudan). İlk denemede `gorsel-gizli` kullanılmıştı → 1 px kutu tarayıcıda "Ne yapıyorsun?" başlığıyla BİNDİ (K9=1, 1024/960/768/375) → `[hidden]`'a çevrildi, K9=0 | BASİT boş fiş: kutu görünmez (her genişlik/tema); 1 kalem (≈10 bin ₺): görünmez; 2–3 kalem ≥ 24 bin ₺: görünür. bUnit ×3 |
| K | [KRİTİK] Hakem çelişkisi — 960/1024 dolu fiş | Doğrulayıcı haklıydı: r1 ölçümü boş fişteydi | Yukarıdaki 1 ve 2; ölçüm kümesine **dolu fiş (2 kalem, 13 haneli barkod, uzun ad) × 6 genişlik × 2 tema** ve **özet açık** ve **sık yoğunluk** eklendi | Tablo R2-4: 54+3 satır, K1..K13 + yatay hepsi 0; ürün kolonu hiçbir PRO durumda < 142 px; kesik hücre 0 |

Ek (tur SONRA'nın yakaladığı): **kapalı "Elle ekle" `<details>` içeriği** Chrome'da (::details-content) yerleşim kutusu
taşıyordu; içindeki Ürün seçicisi görünmezken açılabiliyor, KATMAN'ın 13:40 `katman.js` değişikliğiyle listesi `fixed`
konumlanıp tur'da **SERBEST-YÜZEN (8 durum) / ekran dışı (6 kalıp)** ölçülüyordu (sonda denedim: açıcı 325×388 w259 h44,
liste 327 px, üstünde başka öğe). Kural: `.islem-sayfa #islem-elle:not([open]) > :not(summary) { display:none }` ("+ Ayrıntı ekle"
ile aynı kalıp) → açıcı 0×0, liste 0×0; tur'da bu bulguların hepsi 0.

## R2-2. Derleme · migration
`dotnet build Kuyumcu.sln` → **0 Hata**; uyarılar bu paketin değil (Kuyumcu.Tests/EtiketEkranTestleri.cs(145) xUnit1031,
ZOISLEMAEkranTestleri.cs(247/322/336) xUnit2013/2031 — başka şeritlerin test dosyaları). Migration: **gerekmedi** (şema değişikliği yok).
Not: paralel şeritlerle aynı ağaçta iki derleme çakışması yaşandı (`ConvertDllsToWebCil` dosya kilidi, `apphost` bulunamadı) —
geçersiz koşum sayıldı ve yinelendi. **mut.sh tuzağı:** yedek aynı saniyede geri kopyalanınca artımlı Razor derlemesi
dosyayı "güncel" sanıp MUTASYONLU dll'i bıraktı (M-S2-11b sonrası `#islem-son-tekrar` onclick'siz derlenmiş kaldı; test
MissingEventHandler ile 3/3 kırmızı). Çözüm: geri kopyadan sonra `touch` + yeniden derleme; sonrasında 224/224 yeşil.

## R2-3. Testler
- Yeni (r2, `Kuyumcu.Tests/SATIS2EkranTestleri.cs`): 5 metot — Katlanir HATALI yeniden yükleme · BASİT Tekrar dene ·
  BASİT nakit kutusu boş fişte gizli · eşiğe yaklaşınca görünür · eşik bilinmiyorsa tutucu görünür (+ bileşen sınırı 23.999/24.000).
  SATIS2EkranTestleri: **25/25** yeşil.
- Filtreli koşum (SATIS2 + TS1 + TS2 + S22 + KatalogK1 + UX1C1 + CumaS + L1b + L1Satis): **224/224 yeşil** (test-r2i.txt).
- Tam süit (ara, bu paketin son kodundan önceki CSS-dışı durum, 15:0x): **6236 / 6236 yeşil**.
- Tam süit (son, `trx-pSATIS-2/r2-son.trx`): **6287 · Geçti 6278 · Başarısız 9** — hiçbiri bu paketin değil:
  `DenetimIziEkranTestleri` ×7 (CUMA-J: DenetimIzi), `ZoBelgeTestleri.ZB02_Kasa_raporu_…` (ZO-BELGE: KasaRaporu),
  `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` (Faturalar ×2, Evraklar, DenetimIzi, IslemDefteri,
  KasaRaporu'ndaki sarmalayıcısız `<table>` — CUMA-J / ZO-BELGE). **SATIS-2'nin ürettiği yeni kırmızı: 0**
  (SATIS2/TS1/TS2/S22/CumaS/L1b sınıflarının hiçbiri kırmızı listesinde yok).

## R2-4. Mutasyonlar (r2) — kaynak → `scratchpad/mut-yedek/SATIS-2/<kod>.orj` → perl tek eşleşme → filtreli test → geri kopya → cmp
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S2-10 | Katlanir `if (yuklendi && !Hatali) return;` → `if (yuklendi) return;` | Katlanir_HATALI_yuklemeden_sonra…, BASIT_son_satis_HATASINDA… | BİREBİR |
| M-S2-11b | `#islem-son-tekrar` onclick kaldırıldı (tıklanınca hiçbir şey olmayan düğme) | BASIT_son_satis_HATASINDA… | BİREBİR |
| M-S2-12d | `Yakin => true` (kutu hep görünür — kurucu 11:35 ihlali) | BASIT_nakit_uyarisi_BOS_fiste…GIZLI…, Nakit_uyarisi_esik_BILINMIYORSA… | BİREBİR |
| M-S2-13b | yaklaşma oranı düşürüldü (`Toplam >= Had`) | BASIT_nakit_uyarisi_fis_toplami_esige_YAKLASINCA…, Nakit_uyarisi_esik_BILINMIYORSA… | BİREBİR |
| M-S2-14b | sayfa `Toplam="ToplamTl"` göndermiyor | BASIT_nakit_uyarisi_fis_toplami_esige_YAKLASINCA… | BİREBİR |
| M-S2-1r | (spesifikasyon) Katlanir varsayılanı AÇIK (`acik = true`) | Katlanir_varsayilan_KAPALI…, Katlanir_acilinca_IlkAcilis_BIR_KEZ…, Acilista_son_satislar_ISTENMEZ… (BASIT+PRO), BASIT_Duzelt…, BASIT_degismezlik…, Kirilim_AYRI_SATIR_DEGIL…, BASIT_son_satis_HATASINDA… | BİREBİR |
| M-S2-2r | (spesifikasyon) BASİT dalı `@if (false)` → BASİT'te Düzelt görünür | BASIT_Duzelt_ters_kayit_YOK…, BASIT_degismezlik_TEK_KISA…, BASIT_son_satis_HATASINDA… | BİREBİR |
Geçersiz sayılan koşumlar (yinelendi): M-S2-12 ilk koşum (paralel derleme bayat dll — nakit testleri yeşil kaldı), M-S2-12b
(ConvertDllsToWebCil kilidi). İlk HATASINDA testi bayat öğe tıklıyordu (bUnit MissingEventHandler) → `WaitForAssertion(() => Find(..).Click())`
ile sağlamlaştırıldı. Son mutasyondan sonra `touch` + temiz derleme **0 Hata**, SATIS2 25/25.
r1 mutasyonları (M-S2-1…9) r1 raporunda (aşağıda) — BİREBİR.

## R2-5. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra) → **rc=1, tek ✗: `P-39: TABLO_SARMALAYICISIZ 6`** —
Faturalar.razor:67 ve :587, Evraklar.razor:139, DenetimIzi.razor:131 (CUMA-J), IslemDefteri.razor:157, KasaRaporu.razor:141
(ZO-BELGE / rapor şeridi). Bu paketin dosyası değil (Pages/Islemler*, Fis/* içinde sarmalayıcısız tablo 0). Bu paketin
sayaçları: S2-3 FIS_BILESENI_ISTEK 0 ✓ · S2-3 SOZLUK_SAYI 0 ✓ · P-23 canlı sayaçlar 0 ✓ · OLCEK-1 MOD_OLCU_BILDIRIMI 0 ✓.
Çıktı: `scratchpad/SATIS-2/denetim-r2.txt`.

## R2-6. Gerçek tarayıcı ölçümü (başsız Chrome, CDP; tarayıcı bölmesi KULLANILMADI; ağ önbelleği kapalı)
Tohum: kendi kiracım PAKETSATIS-2-TEST — 12 satis-fişi (2'si çok kalemli) + canlıda eklenenler (son: 19 satış hareketi),
müşteri "Anadolu Holding A.Ş.", fiyat listesi "Perakende Varsayilan", ürün adı "925 Gümüş Bileklik", **13 haneli barkodlar**.
Betik `scratchpad/SATIS-2/olc3.mjs` (her genişlikte sayfa yeniden yüklenir; sikisma.js + ekstra.js + r2 ekleri: ürün kolonu,
kesik hücre, başlık binmesi, tablo iç kaydırma, yer tutucu sığması, **özet açıkken** fiyat listesi kırpması, nakit kutusu;
`YOG=sik` = sık yoğunluk). Ham: `scratchpad/SATIS-2/r2-son3.jsonl` (+ BASİT koyu 960/768/375 dolu ek koşum: hepsi 0).
Senaryolar: bos · k1 (1 kalem) · k2b (2 kalem, barkodla) · k3 (3 kalem) · k3t (3 kalem, tevsik aşıldı).

| Mod | Tema | Genişlik | Fiş | K1..K13 + yatay | boy (ekran) | Kaydet alt / vh | müşteri kutusu | ürün kolonu px | kesik hücre | tablo iç kaydırma | yer tutucu kesik | fiyat listesi kırpık (özet açık) | nakit kutusu görünür |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASIT | ayd | 1440 | bos | hepsi 0 | 1 | 876/900 | 832 | — | 0 | — | False | — | False |
| BASIT | ayd | 1280 | bos | hepsi 0 | 1 | 776/800 | 672 | — | 0 | — | False | — | False |
| BASIT | ayd | 1024 | bos | hepsi 0 | 1.35 | 948/768 | 708 | — | 0 | — | False | — | False |
| BASIT | ayd | 960 | bos | hepsi 0 | 1.26 | 948/820 | 644 | — | 0 | — | False | — | False |
| BASIT | ayd | 768 | bos | hepsi 0 | 1.02 | 964/1024 | 744 | — | 0 | — | False | — | False |
| BASIT | ayd | 375 | bos | hepsi 0 | 1.57 | 1196/812 | 351 | — | 0 | — | False | — | False |
| BASIT | ayd | 1440 | k2b | hepsi 0 | 1 | 876/900 | 832 | 125 | 0 | False | False | — | True |
| BASIT | ayd | 1280 | k2b | hepsi 0 | 1 | 776/800 | 672 | 100 | 0 | False | False | — | True |
| BASIT | ayd | 1024 | k2b | hepsi 0 | 1.67 | 1115/768 | 708 | 118 | 0 | False | False | — | True |
| BASIT | ayd | 960 | k2b | hepsi 0 | 1.56 | 1115/820 | 644 | 86 | 0 | False | False | — | True |
| BASIT | ayd | 768 | k2b | hepsi 0 | 1.25 | 1131/1024 | 744 | 136 | 0 | False | False | — | True |
| BASIT | ayd | 375 | k2b | hepsi 0 | 2.2 | 1615/812 | 351 | 98 | 0 | False | False | — | True |
| PRO | ayd | 1440 | k2b | hepsi 0 | 1 | 792/900 | 360 | 142 | 0 | False | False | False | — |
| PRO | ayd | 1280 | k2b | hepsi 0 | 1.22 | 850/800 | 360 | 320 | 0 | False | False | False | — |
| PRO | ayd | 1024 | k2b | hepsi 0 | 1.54 | 939/768 | 360 | 158 | 0 | True | False | False | — |
| PRO | ayd | 960 | k2b | hepsi 0 | 1.48 | 973/820 | 360 | 158 | 0 | True | False | False | — |
| PRO | ayd | 768 | k2b | hepsi 0 | 1.3 | 1101/1024 | 744 | 218 | 0 | True | False | False | — |
| PRO | ayd | 375 | k2b | hepsi 0 | 2.04 | 1395/812 | 351 | 218 | 0 | True | False | False | — |
| BASIT | kar | 1440 | bos | hepsi 0 | 1 | 876/900 | 832 | — | 0 | — | False | — | False |
| BASIT | kar | 1280 | bos | hepsi 0 | 1 | 776/800 | 672 | — | 0 | — | False | — | False |
| BASIT | kar | 1024 | bos | hepsi 0 | 1.35 | 948/768 | 708 | — | 0 | — | False | — | False |
| BASIT | kar | 960 | bos | hepsi 0 | 1.26 | 948/820 | 644 | — | 0 | — | False | — | False |
| BASIT | kar | 768 | bos | hepsi 0 | 1.02 | 964/1024 | 744 | — | 0 | — | False | — | False |
| BASIT | kar | 375 | bos | hepsi 0 | 1.57 | 1196/812 | 351 | — | 0 | — | False | — | False |
| BASIT | kar | 1440 | k2b | hepsi 0 | 1 | 876/900 | 832 | 125 | 0 | False | False | — | True |
| BASIT | kar | 1280 | k2b | hepsi 0 | 1 | 776/800 | 672 | 100 | 0 | False | False | — | True |
| BASIT | kar | 1024 | k2b | hepsi 0 | 1.67 | 1115/768 | 708 | 118 | 0 | False | False | — | True |
| PRO | kar | 1440 | k2b | hepsi 0 | 1 | 792/900 | 360 | 142 | 0 | False | False | False | — |
| PRO | kar | 1280 | k2b | hepsi 0 | 1.22 | 850/800 | 360 | 320 | 0 | False | False | False | — |
| PRO | kar | 1024 | k2b | hepsi 0 | 1.54 | 939/768 | 360 | 158 | 0 | True | False | False | — |
| PRO | kar | 960 | k2b | hepsi 0 | 1.48 | 973/820 | 360 | 158 | 0 | True | False | False | — |
| PRO | kar | 768 | k2b | hepsi 0 | 1.3 | 1101/1024 | 744 | 218 | 0 | True | False | False | — |
| PRO | kar | 375 | k2b | hepsi 0 | 2.04 | 1395/812 | 351 | 218 | 0 | True | False | False | — |
| PRO | ayd | 1440 | bos | hepsi 0 | 1 | 635/900 | 360 | — | 0 | — | False | False | — |
| PRO | ayd | 1280 | bos | hepsi 0 | 1.02 | 688/800 | 360 | — | 0 | — | False | False | — |
| PRO | ayd | 1024 | bos | hepsi 0 | 1.11 | 710/768 | 360 | — | 0 | — | False | False | — |
| PRO | ayd | 960 | bos | hepsi 0 | 1.08 | 744/820 | 360 | — | 0 | — | False | False | — |
| PRO | ayd | 768 | bos | hepsi 0 | 1 | 798/1024 | 744 | — | 0 | — | False | False | — |
| PRO | ayd | 375 | bos | hepsi 0 | 1.49 | 1017/812 | 351 | — | 0 | — | False | False | — |
| BASIT | ayd | 1440 | k1 | hepsi 0 | 1 | 876/900 | 832 | 83 | 0 | False | False | — | False |
| BASIT | ayd | 1280 | k1 | hepsi 0 | 1 | 776/800 | 672 | 68 | 0 | False | False | — | False |
| BASIT | ayd | 1440 | k3 | hepsi 0 | 1 | 876/900 | 832 | 87 | 0 | False | False | — | True |
| BASIT | ayd | 1280 | k3 | hepsi 0 | 1 | 776/800 | 672 | 71 | 0 | False | False | — | True |
| BASIT | ayd | 1440 | k3t | hepsi 0 | 1 | 876/900 | 832 | 90 | 0 | False | False | — | True |
| BASIT | ayd | 1280 | k3t | hepsi 0 | 1 | 776/800 | 672 | 74 | 0 | False | False | — | True |
| PRO | kar | 1440 | bos | hepsi 0 | 1 | 635/900 | 360 | — | 0 | — | False | False | — |
| PRO | kar | 1280 | bos | hepsi 0 | 1.02 | 688/800 | 360 | — | 0 | — | False | False | — |
| PRO | kar | 1024 | bos | hepsi 0 | 1.11 | 710/768 | 360 | — | 0 | — | False | False | — |
| PRO | kar | 960 | bos | hepsi 0 | 1.08 | 744/820 | 360 | — | 0 | — | False | False | — |
| PRO | kar | 768 | bos | hepsi 0 | 1 | 798/1024 | 744 | — | 0 | — | False | False | — |
| PRO | kar | 375 | bos | hepsi 0 | 1.49 | 1017/812 | 351 | — | 0 | — | False | False | — |
| BASIT | kar | 1440 | k3t | hepsi 0 | 1 | 876/900 | 832 | 90 | 0 | False | False | — | True |
| PRO | ayd | 1024 | k2b (sık yoğunluk) | hepsi 0 | 1.44 | 863/768 | 360 | 158 | 0 | True | False | False | — |
| PRO | ayd | 960 | k2b (sık yoğunluk) | hepsi 0 | 1.39 | 897/820 | 360 | 158 | 0 | True | False | False | — |
| BASIT | kar | 960 | k2b | hepsi 0 | 1.56 | 1115/820 | 644 | 86 | 0 | False | False | — | True |
| BASIT | kar | 768 | k2b | hepsi 0 | 1.25 | 1131/1024 | 744 | 136 | 0 | False | False | — | True |
| BASIT | kar | 375 | k2b | hepsi 0 | 2.2 | 1615/812 | 351 | 98 | 0 | False | False | — | True |

Özet sayaçlar (57 satır): yer tutucu kesik 0 · fiyat listesi kırpık 0 · kesik hücre 0 · BASİT'te Düzelt 0 · açılışta geçmiş açık 0 ·
PRO ürün kolonu < 60 px durum 0.
Kabul eşikleri: PRO 1440 boş ve 2 kalem **1,00 ekran**, Kaydet 635/792 < 900 ✓ · BASİT 1440 **1,00 (≤1,3)** ✓ ·
CUMA-S {boş, 1, 3, 3+tevsik} × {1440×900, 1280×800} BASİT Kaydet satırı 876/900 · 776/800 (viewport İÇİNDE) ✓ ·
960 PRO müşteri kutusu 360 px (≥220) ✓.
Bilinen/gerekçeli: PRO 1280 2 kalem 1,22 ekran (Kaydet 850/800 — kabul eşiği 1440 için; 1280'de sağ sütun ALTA iner) ve
PRO/BASİT 1024–375 dolu fişte 1,25–2,2 ekran (dikey yığılım; yatay kaydırma 0). PRO ≤1100 px'te kalem tablosu kart içinde
yatay kaydırılır: 1024'te Tutar kolonu kaydırma ile görünür (Genel toplam alt şeritte her zaman görünür) — doğrulayıcının
önerdiği iki yoldan biri; ürün adı ≥158 px okunur.
Sık yoğunlukta Müşteri kutusundaki "Borcu yok" rozeti 32 px kutunun alt kenarına değiyor (K sayaçları 0; OLCEK-1'in
yoğunluk ölçüsüyle ilgili) — açık kalan olarak yazıldı.

### tur.mjs SONRA (resmî `betikler/tur/tur.mjs`, taze profil `tur/sonra4`, 24 kombinasyon, CDP 9900–9903)
Önce `tur/sonra3` koşumu 13 bulgu verdi: 8× SERBEST-YÜZEN + 6× ekran dışı (kapalı "Elle ekle" içindeki Ürün seçicisi — yukarıdaki
ek düzeltme) + K9 1024/768 + K4 tren. Ayrıca scratch kopyası tur'un önbelleği kapatmadığı görüldü → resmî betik + TAZE profille
yinelendi. **SONRA (sonra4): 3 bulgu, hepsi K4 tren** (PRO 1280 2,2 · 960 2,5 · 375 3,3 ekran): tur tüm açıcıları (özet, karma,
geçmiş, ayrıntı) sırayla AÇIK bırakıp aynı sayfada ölçer — durum taşıması (r1'de de aynı, gerekçeli); taze durumda aynı
genişlikler 1,02–1,49 (tablo). K1..K3, K5..K13 = 0; SERBEST-YÜZEN 0; ekran dışı 0.

### Görüntüler (Read ile bakıldı) — `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/`
- `SATIS-2-pro-1440.png` — tuval Profesyonel.png | PRO 1440 boş | PRO 1440 2 kalem (13 haneli barkod tam, "925 Gümüş Bileklik" iki satır, kesik yok).
- `SATIS-2-basit-1440.png` — tuval Tezgah.png | BASİT 1440 boş (nakit kutusu YOK) | BASİT 1440 3 kalem + tevsik (nakit kutusu eşikte görünür).
- `SATIS-2-r2-dolu-960-1024.png` — PRO 960 2 kalem (ürün 158 px, ÖNCE 0) | 1024 (158, ÖNCE 35–50) | 960 özet AÇIK sık yoğunluk (fiyat listesi tam) | 960 koyu özet açık.
- `SATIS-2-r2-dar-koyu.png` — PRO 375 (arama tam genişlik, yer tutucu tam) | PRO 375 koyu 2 kalem | BASİT 375 koyu | BASİT 1440 koyu boş.
- Ayrıca bakılan tek kareler: `scratchpad/SATIS-2/foto/taze-PRO-aydinlik-960-k2b.png`, `…-375-bos.png` (düzeltme öncesi/sonrası),
  `…-1440-k2b.png`, `ozet-PRO-aydinlik-960-k2b-sik.png`, `ozet-PRO-karanlik-960-k2b.png`, `tur/sonra3/foto/islemler__PRO_aydinlik_1440__acici5.png`
  (≥ 12 görüntü). r1'in önce/sonra görüntüleri (`SATIS-2-once-sonra-*.png`, `SATIS-2-dar-koyu.png`) yerinde.
Ölçüm önce/sonra (bulgular): ürün kolonu 960 **0 → 158**, 1024 **50 → 158**, 1440 **128 (3 kesik) → 142 (0 kesik)** ·
fiyat listesi (sık, özet açık) **h32/sh36/ch30 kırpık → h49/sh47/ch47** · 375 yer tutucu **iç 99 px kesik → 275 px tam** ·
BASİT boş fişte nakit kutusu **görünür → gizli**.

### Bölge tablosu (1440) — r1 tablosu geçerli; r2 değişiklikleri
| Bölge | Tuvalde | Bizde (r2) | Durum |
|---|---|---|---|
| Kalem tablosu kolonları | 120 · 1fr · 56 · 66 · 66 · 66 · 60 · 90 · 110 · 44 (tuvalde "Burma/bilezik" üst üste — tuval hatası) | 134 (barkod 14 px) · 1fr (≥142, satır kırar) · 44 · 58 · 56 · 64 · 52 · 70 · 112 · 52 | GEREKÇELİ FARK — 13 haneli barkod ve ürün adı kesilmesin (ölçüldü) |
| BASİT sağ sütun nakit kutusu | her zaman | yalnız eşiğe yaklaşınca | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| Diğer bölgeler | — | r1 ile aynı | r1 tablosu |

## R2-7. Canlı doğrulama (API 5162 + vekil 5163, Development, `canli2.mjs`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| PRO /islemler açılış | iade-adaylari isteği 0 | **0** |
| PRO "Son satışlar" aç | GET /api/v1/islemler/iade-adaylari?tur=SATIS&limit=20 → 200 | **200**, 17 kayıt, 5 satır; kilitli Düzelt 5 (PRO'da kalır, title sebebi) |
| PRO kırılım ikonu | GET /api/v1/islemler/fis/{id} → 200 | **200** (01M3CB1DPPS2YZ037TDJ99TRT0) |
| PRO 1 kalem Kaydet (ödeme katmanı KAPALI) | POST /islemler/satis 2xx, gövde eski alanlarla, NAKIT + tek kasa otomatik | **200**; `odemeTuru:"NAKIT"`, `kasaId` dolu, `reyonId:null`, `aciklama:null`; bant "İşlem başarıyla kaydedildi!" |
| SQL | hareket + ödeme kalemi | `hareketler 01M3CB2RH19H6P3E7095XFNYEK SATIS NAKIT 10245.31` · `odeme_kalemleri NAKIT 10245.31` |
| BASİT açılış / aç | 0 istek → 1 istek 200 | **0 → 200** (18 kayıt, 3 satır), Düzelt **0** |
| BASİT 1 kalem Kaydet | POST /islemler/satis 2xx | **200** (NAKIT, kasa otomatik); kiracının SATIS hareketi 19 |
Sonra: 5162/5163 süreçleri `kill` (5587, 43957, 5389) → `lsof` boş. Kiracı PAKETSATIS-2-TEST (01M3C4V39PJ0PTGNK8DAXAD6H0)
tenant_id kolonlu tüm tablolardan silindi (API kapanırken yazılmış 1 `kur_snapshots` satırı ayrıca silindi → kalan 0) + davet satırı silindi;
**tenants 17 → 17** (başlangıç). QA-TEST ve diğer şeritlerin kiracılarına dokunulmadı.

## R2-8. Değişen dosyalar (r2)
`dotnet/Kuyumcu.Web/Bilesenler/Katlanir.razor` (Hatali) · `Bilesenler/Fis/FisSonSatislar.razor` (Hatali + BASİT Tekrar dene) ·
`Bilesenler/Fis/FisNakitUyari.razor` (Toplam, Yakin, [hidden]) · `Pages/Islemler.razor` (tek satır: `Toplam="ToplamTl"`, 1399 satır — tavan aşılmadı) ·
`wwwroot/css/tuval-satis.css` (r2 blokları: kalem tablosu, fiyat listesi, 375 arama, Elle ekle kapalı içerik, nakit [hidden]) ·
`Kuyumcu.Core/I18n/Sozlukler.SatisSade.cs` (TekrarDene TR+EN) · `Kuyumcu.Tests/SATIS2EkranTestleri.cs` (+5 test, sahte API hata anahtarı) ·
görüntüler `ham/TUVAL-KARSILASTIRMA/SATIS-2-{pro-1440,basit-1440,r2-dolu-960-1024,r2-dar-koyu}.png`.
Dokunulmayan: app.css, Sozlukler.cs HUB, FisDigerIslemler, olcek.css (OLCEK-1), katman.* (KATMAN), POST gövdeleri, id'ler.
Tek dokunuş: Pages/Islemler.razor bu dalgada zaten açıktı (r1) — r2'deki tek satır aynı paketin kapanışıdır.

## R2-9. Koşul karşılama (paket kapısı, r2)
| Koşul | Durum | Kanıt |
|---|---|---|
| 1 derleme 0 hata | ✓ | build-r2f.txt |
| 2 kendi testlerim 0 başarısız | ✓ | SATIS2 25/25; ilgili 9 sınıf 224/224 |
| 3 kendi sayaçlarım denetim.sh'ta ✓ | ✓ | S2-3 ×2, P-23 canlı; tek ✗ P-39 başka şeridin 6 tablosu |
| 4 mutasyon kanıtı | ✓ | 7/7 kırmızı + cmp BİREBİR (r1: 9/9) |
| 5 canlı doğrulama | ✓ | R2-7 |
| 6 test kiracısı silindi | ✓ | kalan 0, tenants 17 |
| 7 yeni kırmızı üretilmedi | ✓ | son tam süit 9 kırmızı, hepsi CUMA-J / ZO-BELGE / P-39 tablo sarmalayıcısı |
| Gerçek tarayıcı (dolu fiş dahil) | ✓ | R2-6 tablo + ≥12 görüntü |
| Önceki bulgular | ✓ | R2-1 (1–4, 8, K kapandı; 5–7 sahipli) |

## R2-10. Açık kalanlar (sahipli)
- **CUMA-J / ZO-BELGE:** P-39 TABLO_SARMALAYICISIZ 6 (Faturalar ×2, Evraklar, DenetimIzi, IslemDefteri, KasaRaporu) → `P39TasarimKokleriTestleri.K5` kırmızı;
  `DenetimIziEkranTestleri` ×7 ve `ZoBelgeTestleri.ZB02` kırmızı (son tam süit).
- **L-1 / P-23 sahibi:** çok kalemli satis-fisi'de ödeme kalemi yalnız 1. harekete bağlı; ODEME_KALEMSIZ_YOL canlı sayacı çok kalemli fişte ✗ verir (r2'de kiracı silinince 0).
- **Kurucu:** `dotnet/Kuyumcu.Web/Pages/Islemler.Sade.cs.tmp` (0 bayt) silinmeli (hook blokluyor).
- **OLCEK-1:** sık yoğunlukta (2rem) Müşteri kutusundaki bakiye rozeti 32 px kutunun alt kenarına değiyor; `olcek.css` sabit yükseklik kuralı iki satırlı salt kutularda (fiyat listesi) kırpma üretiyordu — bu pakette kendi dosyamdan daha özgül seçiciyle ezildi, kök kural OLCEK'te.
- **Tur aracı:** `tur.mjs` ağ önbelleğini kapatmıyor (profil yeniden kullanılırsa eski CSS ölçülür) — taze profil ya da `Network.setCacheDisabled` önerilir (betik sahibine).
- **KATMAN:** `katman.js` (13:40) kapalı `<details>` içindeki seçicinin listesini `fixed` açabiliyor; /islemler'de kapalı içerik artık yerleşimden çıkarıldı, diğer ekranlarda aynı kalıp aranmalı.
- Uyarılar: EtiketEkranTestleri.cs(145) xUnit1031, ZOISLEMAEkranTestleri.cs xUnit2013/2031 (başka şeritler).
- Kurucuya/mali müşavire: mevzuat hükmü eklenmedi; nakit kutusunun 0,8 yaklaşma oranı yalnız ekran davranışıdır, eşik sayısı sunucudan (`/tevsik/gunluk`).

---

# § R1 — önceki koşunun raporu (değiştirilmedi; r2 tabloları önceliklidir)


Tarih: 25.09.2026 · Şerit: v2 · test DB kuyumcu_test_t2 · canlı API 5162 / vekil 5163 · CDP 9910–9913
Rol: Taşkın (backend — değişiklik gerekmedi) + Ertuğ (frontend). COMMIT YOK.

## 0. Devralma (devir notu gereği)
- Bu koşu, 11:15–11:36 arasında yarıda kalan önceki SATIS-2 koşusunun ağaçtaki işini devraldı (Katlanir.razor,
  katlanir.css, Sozlukler.SatisSade.cs, FisSonSatislar/FisProOdemeHapi/FisMusteriSatiri/… değişiklikleri,
  `scratchpad/SATIS-2/once-kaynak` yedekleri ve `tur/once` ÖNCE ölçümü). Önceki koşunun API'si kapanmıştı (5162/5163 boş).
- CUMA-S: `mut-yedek/CUMA-S/M-CS-1.orj` ve `M-CS-2.orj` ağaçtaki `FisOdemeKasa.razor` ile `cmp` → BİREBİR (yarım mutasyon YOK).
  PAKETCUMA-S-TEST kiracısı (01M3BQ2S…) veritabanında yoktu. L-1b (alış fişi) dosyalarına dokunulmadı; L1b testleri yeşil.
- ⚠ Önceki koşunun bıraktığı 0 baytlık `dotnet/Kuyumcu.Web/Pages/Islemler.Sade.cs.tmp` proje ağacında duruyor —
  dosya silme YASAK/hook blokluyor; **kurucunun silmesi gerekir** (derlemeye girmez, denetim.sh işaretlemiyor).

## 1. Yapılanlar (kurucu temel kuralı 11:35 + KAYNAK 1–4 + KAPSAM 1–7)
| # | Madde | Uygulama |
|---|---|---|
| 1 | Ortak **Katlanir** | `Bilesenler/Katlanir.razor` + `css/katlanir.css` (index.html'e link). Varsayılan KAPALI, kapalıyken gövde DOM'da YOK, ilk açılışta `IlkAcilis` BİR KEZ, `<button aria-expanded aria-controls>`; Sayi null → rozet yok. |
| 2 | Son satışlar → Katlanir | `#islem-son-satislar` Katlanir; açılışta `/islemler/iade-adaylari` İSTENMEZ (canlı: 0 istek), açınca 1 istek. Kayıt/iade sonrası yalnız açılmışsa tazelenir (`SonSatislariTazele`); Diğer paneli (avans/değişim) listeyi kendisi ister. BASİT/PRO ve tüm türlerde aynı bileşen. |
| 3 | Kırılım ayrı satır değil | Eylem hücresinde ikon düğme `#islem-kirilim-{id}` (aria-expanded, title); kırılım satırın ALTINDA `.islem-kirilim-satir`. |
| 4 | BASİT'te Düzelt yok | BASİT kısa liste (saat · müşteri · tutar), storno düğmesi 0; PRO'da kilitli + `title` sebebi (UX-S30). |
| 5 | "Bu kayıt değiştirilemez" | BASİT: tek satır "Yanlış satışı Satış iadesi ile geri alın." (jargonsuz) + "Neden değiştirilemez?" katlı; PRO: DegismezIpucu aynı katlı ayrıntıda. |
| 6 | KAYNAK 2 metinleri | Pasif cari notu / Peşin notu / "Ödeme: Tedarikçiye…" / "İndirim: önce…" / "Kasa seçin…" ekranda değil: `title` + ekran okuyucu; Peşin kimlik bilgisi eşik geldiğinde bant (`EsikYakin` = sunucu eşiğiyle MasakGerekli). |
| 7 | KAYNAK 3 | 960 PRO müşteri kutusu 360 px (ölçüldü); fiyat listesi adı özet satırında tam; eşik etiketleri tek satır; PRO 1440 boy 1,0. |
| a | Tedarikçi satırı | kalktı (kilitli sekmenin title + aria-describedby). |
| b | Borç rozeti | Müşteri kutusunun İÇİNDE (PRO ve BASİT). |
| c | Pasif cari satırı | kalktı (title). |
| d+e | Tek giriş | PRO kalem kartının üst satırında tek "Ürün ara ya da barkod okut (F2)"; Enter satır ekler; elle satırı (ürün/adet/fiyat/Ekle, id'ler aynı) "Elle ekle" altında katlı. |
| f | Ödeme türü + kasa | alt çubukta tıklanabilir özet "Nakit · Merkez Kasa · 0 ₺ ›" → bağlı açılır katman (`#islem-pro-odeme`, kapalıyken DOM'da `[hidden]`); karma ya da yasal kapı açıkken satır içi (kısa durum + "Neden?"). |
| g+h | Reyon/Açıklama | alt çubukta "+ Ayrıntı ekle" (tuvalin "⋯"su) — reyon, açıklama, PRO fiş iskontosu; dolu/uyarılıysa açık gelir. BASİT'te de açıklama burada. |
| i | "Kasa seçin…" satırı | boş fişte görünmez; kilit sebebi Kaydet `title` + dolu fişte Kaydet yanında kısa metin. |
| j | Tasarım notu kutusu | kaldırıldı (`#islem-pro-not` yok). Diğer ekranlarda arandı: `GorunumKartiSozlugu.ModNotu` ("Hiçbir özellik kapanmaz…" — Ayarlar tuvalinin notu) KATMAN şeridinin dosyası → DOKUNULMADI, orkestratöre. |
| K | Kurucu 11:35 — PRO üst satır | Müşteri + TEK satır özet "Bugün · TL · Ertuğ U. · Perakende Varsayılan" (`#islem-ust-ozet`); Tarih/Para/Satıcı/Fiyat listesi kutuları (id'ler aynı) özetin altında katlı; satıcı/liste hatasında açık. |
| K | BASİT Tarih/Para | hiç görünmez (gövdeye zaten gitmiyordu). |
| K | Karma anahtarı | "Karma ödeme / Tek ödeme" hapı yerine "+ Başka ödeme ekle" (`#islem-pro-karma`), karmada "Tek ödemeye dön" (`#islem-pro-tek`); BASİT "Ödemeyi böl" → "+ Başka ödeme ekle". F8 karmada `#islem-pro-tek`. |
| K | Sağ sütun | Yasal kapılar uygunken TEK yeşil satır (satırlar katlı, id'ler aynı); aşılınca/yaklaşınca/bilinmiyorken kart. Müşteri kartı YALNIZ gerçek müşteride. Kısayollar tek satır. |

POST gövdeleri, Kaydet() yolları (satis, satis-fisi, alis, alis-fisi, karma), yasal kapılar, F2/F4/F8/F9 ve mevcut id'ler DEĞİŞMEDİ (yalnız görünüm). `Islemler.razor` 1399 satır (S21 tavanı).

### Sapmalar ve gerekçe
- **Yaklaşma oranı 0,8** (`FisProYanSutun.YaklasmaOrani`): yalnız EKRAN (kartın açık çizilmesi); yasal hüküm değil, eşik sayıları yine sunucudan. Mevzuat hükmü koda yazılmadı.
- **Katlı alanlar DOM'da `[hidden]`** (ödeme katmanı, üst özet ayrıntısı): F8 odağı, bUnit ve form doğrulaması aynı id'leri bulur; görünmez. Katlanir (geçmiş) ise sözleşme gereği kapalıyken DOM'da yok.
- **Karma anahtarı (`#islem-karma`) PRO satışta `gorsel-gizli`** — aynı soru alt çubuktaki bağlantıyla soruluyor (UX-S22); test ve ekran okuyucu için DOM'da.
- `S22 Basit_satista_ilk_bakista…` testi: açıklama alanı katlandığı için ilk bakış alan ≤ 2 (sıkılaştı), açıcı eklendiği için düğme ≤ 13; toplam (alan+düğme ≤ 15) aşılmadı.

## 2. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK, migration YOK. Satış fişi (hareketler) finansal kayıt: salt okunur; düzeltme yolu DegismezIpucu ile (Satış iadesi / 48-A storno), PRO'da kilitli "Düzelt (ters kayıt)" + gerekçe title. Her düğme ya istek atar ya disabled+sebep (kırılım → GET /islemler/fis/{id}; Katlanir → ilk açılışta GET iade-adaylari).

## 3. Katlanir API (GECMIS-1 için)
`<Katlanir Id="…" Baslik="…" Sayi="int?" Sinif="…" BaslangictaAcik="false" IlkAcilis="EventCallback" Degisti="EventCallback<bool>">içerik</Katlanir>`
id'ler: kök=`Id`, başlık düğmesi=`{Id}-ac`, gövde=`{Id}-govde`, rozet=`{Id}-sayi`. `Acik` salt okunur özellik. Bileşen istek atmaz; veriyi `IlkAcilis`'te sayfa yükler (bir kez). Stil `css/katlanir.css` (belirteç renkleri, 1 px ayraç, 10 px köşe).

## 4. Derleme · migration
`dotnet build Kuyumcu.sln` → 0 Hata, 0 Uyarı (son: build-son.txt). Migration: gerekmedi.

## 5. Testler
- Yeni: `Kuyumcu.Tests/SATIS2EkranTestleri.cs` — 19 metot / 20 koşum (Katlanir ×3, son satışlar ×(2 teori), BASİT Düzelt, kırılım, BASİT değişmezlik, PRO özet ×2, KısaAd, yasal ×2, müşteri kartı+kısayol, tasarım notu yok, ödeme katmanı + POST gövdesi, Ayrıntı ekle, BASİT açıklama/tarih/+Başka ödeme, KAYNAK 2, sözlük) → **20/20 yeşil** (tam süitte de).
- Güncellenen (davranış değişikliği gereği): TS1 (3), TS2 (4), S22 (2), KatalogK1 KAT_K4_02/03, UX1C1 (6 → `PosSonAcik`), CumaS CUMA_S_04 (PRO'da da kısa durum).
- Filtreli koşum 12:1x (Islemler'i çizen 28 sınıf): **354/354 yeşil** (test-f2.txt).
- Tam süit (son, `--logger trx`, `scratchpad/trx-pSATIS-2`): 6180 · Geçti 6076 · **Başarısız 104** — hepsi BAŞKA ŞERİT (bkz. §9): 98 × bUnit WaitFor zaman aşımı + 6 × "Expected 10000 / Actual 10.000", "6800/6.800", "12,5/12,500". Kök: **PAKET BICIM-2** `Bilesenler/Form/SayisalAlan.cs` (13:12) "odak dışında biçimli" girdi — ham değer bekleyen testler (Q102 Hurda, Kur, P16 ürün, P20, TS1/TS2…) kırılıyor; aynı sınıflar BICIM-2 değişikliğinden ÖNCE (12:1x) yeşildi, SATIS-2'nin dokunmadığı ekranlar (Kur, Hurda onayı, Ürün düzenleme, Sipariş) da aynı kalıpla kırmızı. SATIS2EkranTestleri biçimden bağımsız yazıldı ve yeşil.
- Önceki denemede ara koşum (test-tam3 öncesi) 2155 kırmızı: başka şeridin derlemesi bin'i silmişti (NUlid/BCrypt FileNotFound) — geçersiz koşum, tekrarlandı.

## 6. Mutasyonlar (kaynak → scratchpad/mut-yedek/SATIS-2 → perl tek eşleşme → test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S2-1 | Katlanir `acik = BaslangictaAcik` → `true` | Katlanir_varsayilan_KAPALI…, Katlanir_acilinca_IlkAcilis_BIR_KEZ…, Acilista_son_satislar_ISTENMEZ… (×2), BASIT_Duzelt…, BASIT_degismezlik…, Kirilim_AYRI_SATIR_DEGIL… | BİREBİR |
| M-S2-2 | FisSonSatislar `@if (Basit)` → `@if (false)` (BASİT'te Düzelt görünür) | BASIT_Duzelt_ters_kayit_YOK…, BASIT_degismezlik_TEK_KISA… | BİREBİR |
| M-S2-3 | Katlanir `if (yuklendi) return;` → `if (false)` | Katlanir_acilinca_IlkAcilis_BIR_KEZ…, Acilista_son_satislar… (×2) | BİREBİR |
| M-S2-4 | `YasalDikkat => true \|\| …` | PRO_yasal_kapilar_uygunken_TEK_satir… | BİREBİR |
| M-S2-5 | müşteri kartı `Cari is not null && !PesinSecili` → `Cari is not null` | PRO_musteri_karti_Pesinde_YOK… (ilk koşum başka şeridin derleme kilidiyle GEÇERSİZ, tekrarlandı) | BİREBİR |
| M-S2-6 | üst özet `hidden="@(!OzetGorunur)"` → `@(false)` | PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet… | BİREBİR |
| M-S2-7 | ödeme katmanı `hidden="@(!KatmanAcik)"` → `@(false)` | PRO_odeme_katmani_KAPALIYKEN_gizli… | BİREBİR |
| M-S2-8 | Katlanir `IlkAcilis` → `Task.CompletedTask` (liste hiç yüklenmez) | Acilista_son_satislar… (×2), BASIT_Duzelt…, BASIT_degismezlik…, Kirilim… | BİREBİR |
| M-S2-9 | `tuval-pro-sekme-not gorunmez` → görünür | KAYNAK2_aciklama_metinleri_sade_yuzde_GORSEL_GIZLI… | BİREBİR |
Son mutasyondan sonra temiz derleme: 0 Hata 0 Uyarı.

## 7. Denetim
`bash dotnet/denetim.sh` → **DENETİM TEMİZ** (rc=0). Ara koşumda 2 ✗ vardı: (1) "Sınırsız ToListAsync 146/145" — API Modules, bu paketin değil (son koşumda temiz); (2) "P-23 ODEME_KALEMSIZ_YOL 3" — test kiracımın çok kalemli satış fişlerinin 2./3. satırları (fiş başına tek ödeme kalemi 1. harekete bağlı; FIS_TOPLAM farkı 0). Kiracı silinince 0. **Not orkestratöre:** çok kalemli satis-fisi her hareket satırına kalem yazmıyor; denetimin hareket-başı ölçüsü canlı çok kalemli fişte ✗ verir (L-1 / P-23 sahiplerine).

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, tarayıcı bölmesi kullanılmadı)
### 8.1 Taze durum matrisi (her genişlikte sayfa yeniden yüklenir; sikisma.js + ekstra.js) — `scratchpad/SATIS-2/taze-son.jsonl`
| Mod | Tema | Genişlik | K1 K3 K5 K7 K8 K9 K9b K10 K11 K12 K13 yatay | boy (ekran) | Kaydet alt / vh | müşteri kutusu | Düzelt | geçmiş açık |
|---|---|---|---|---|---|---|---|---|
| PRO | açık/koyu | 1440 boş | hepsi 0 | 1,00 | 633/900 | 360 | 0 | hayır |
| PRO | açık/koyu | 1440 2 kalem (tevsik aşıldı) | hepsi 0 | **1,00** | 790/900 | 360 | 0 | hayır |
| PRO | açık/koyu | 1280 | hepsi 0 | 1,01 | 686/800 | 360 | 0 | hayır |
| PRO | açık/koyu | 1024 | hepsi 0 | 1,11 | 706/768 | 360 | 0 | hayır |
| PRO | açık/koyu | 960 | hepsi 0 | 1,04 | 706/820 | **360 (≥220)** | 0 | hayır |
| PRO | açık/koyu | 768 | hepsi 0 | 1,00 | 794/1024 | 744 | 0 | hayır |
| PRO | açık/koyu | 375 | hepsi 0 | 1,43 | 971/812 | 351 | 0 | hayır |
| BASİT | açık/koyu | 1440 boş | hepsi 0 | **1,00 (≤1,3)** | 876/900 | 832 | 0 | hayır |
| BASİT | açık/koyu | 1280 | hepsi 0 | 1,00 | 776/800 | 672 | 0 | hayır |
| BASİT | açık/koyu | 1024 | hepsi 0 | 1,45 | 950/768 | 708 | 0 | hayır |
| BASİT | açık/koyu | 960 | hepsi 0 | 1,36 | 950/820 | 644 | 0 | hayır |
| BASİT | açık/koyu | 768 | hepsi 0 | 1,09 | 966/1024 | 744 | 0 | hayır |
| BASİT | açık/koyu | 375 | hepsi 0 | 1,69 | 1197/812 | 351 | 0 | hayır |
| BASİT (CUMA-S kabulü) | açık | {1, 2, 3, 3+tevsik} × {1440×900, 1280×800} + koyu 1440 3+tevsik | hepsi 0 | 1,00 | 876/900 · 776/800 (Kaydet satırı viewport İÇİNDE) | | 0 | hayır |
K4 (tren) 1440'ta 1,0 (≤1,6 hedefi); K5 TRY 0.

### 8.2 tur.mjs SONRA (24 kombinasyon, `tur/sonra2`) vs ÖNCE (`tur/once`)
ÖNCE 14 bulgu (kesik "Perakende Varsayılan" 8 durum, 960 #islem-cari 50 px EZİK, eşik etiketleri kırık, PRO tren 2,2–6,1 ekran…) → SONRA 6 bulgu, hepsi turun DURUM TAŞIMASI (aynı sayfada genişlikler arası açıcılar açık kalıyor):
- K9 1024/768 PRO: önceki genişlikte açılan ödeme aracı açılır listesi (bağlı açılır) içeriğin üstünde — tasarım gereği; görüntüyle doğrulandı (yanlış pozitif).
- K4 tren 1280/960/375 PRO: karma + geçmiş + özet açık bırakılmış durum; taze durumda 1,0–1,4.
- K7 #islem-elle-satir 768 BASİT ±4 (ÖNCE'de de vardı): taze durumda, elle satırı açıkken ve ürün seçiliyken sikisma.js ile yeniden ölçüldü → K7=0, kutu altları 698/698/698. Yeniden üretilemedi; turun taşınan durumuna bağlı.
- Ara koşumda bulunan ve DÜZELTİLEN: ödeme katmanında 13 px yazı, 375'te fiyat listesi adı taşması, kapalı `<details>` içeriğinin (Chrome ::details-content) ezik/hizasız ölçülmesi, 1440 2 kalemde 1,04 ekran.

### 8.3 Karşılaştırma görüntüleri (`dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/`)
`SATIS-2-pro-1440.png` (tuval | boş | 2 kalem), `SATIS-2-basit-1440.png` (tuval | boş | 3 kalem+tevsik), `SATIS-2-once-sonra-pro-1440.png` (kurucunun gördüğü ÖNCE tam sayfa | SONRA | geçmiş açılınca | tuval), `SATIS-2-once-sonra-basit-1440.png`, `SATIS-2-dar-koyu.png` (375 PRO/BASİT koyu, 960 PRO koyu, 1440 BASİT koyu, 1280 BASİT). Okunan (Read) görüntüler: tuval Profesyonel/Tezgah, ÖNCE PRO/BASİT 1440, SONRA PRO boş/2 kalem 1440, PRO 960, PRO 1280 2 kalem, BASİT boş/3+tevsik 1440, geçmiş açık PRO/BASİT, tur 1024 kırpım, BASİT 768 kırpım, dar-koyu matrisi (≥ 14).

### 8.4 Bölge tablosu (1440)
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Sekmeler + Diğer | 8 sekme + Diğer ▾ | aynı (Ödeme kilitli, sebep title) | AYNI |
| Alan satırı | 5 kutu (Müşteri+borç rozeti, Tarih, Para, Satıcı, Fiyat listesi) | Müşteri (rozet kutu içinde) + tek satır özet; kutular tıklanınca | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| Ürün girişi | tabloda satır | kalem kartı başında tek "Ürün ara ya da barkod okut (F2)" + "Elle ekle" | AYNI (KAPSAM d+e) |
| Kalem tablosu | barkod·ürün·milyem·gram·has·adet·işçilik·tutar ✎ | aynı (+ayar) | AYNI |
| Alt toplam şeridi | has · işçilik · KDV · genel toplam | aynı | AYNI |
| Alt çubuk | [Karma/Tek] + özet + Vazgeç + Kaydet | "+ Ayrıntı ekle" + tıklanır ödeme özeti + "+ Başka ödeme ekle" + Vazgeç + Kaydet | GEREKÇELİ FARK — kurucu kuralı 11:35 (anahtar yerine bağlantı; reyon/açıklama "⋯") |
| Ödeme türü/kasa | alt çubuk özeti | özetten bağlı açılır; yasal kapıda satır içi kısa durum | AYNI / GEREKÇELİ (yasal bilgi gizlenmez) |
| Yasal kapılar | 3 satırlı kart | uygunken tek yeşil satır, aşılınca/yaklaşınca kart | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| Müşteri kartı | her zaman | yalnız gerçek müşteride | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| Kısayollar | 4 satırlık kart | tek satır | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| Gri tasarım notu | var | yok | GEREKÇELİ FARK — tasarım notu, ürün metni değil (j) |
| Son satışlar | tuvalde yok | katlı "Son satışlar (15)" | GEREKÇELİ FARK — özellik silinmez, kurucu 11:15 |
| BASİT üst satır | Müşteri · Tarih · Para | yalnız Müşteri | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| BASİT alt satır | Açıklama · Vazgeç · Kaydet | "+ Ayrıntı ekle" · Vazgeç · Kaydet | GEREKÇELİ FARK — kurucu kuralı 11:35 |
| BASİT sağ sütun | ızgara · Son 3 satış · nakit uyarısı | aynı, "Son 3 satış" katlı | GEREKÇELİ FARK — kurucu 11:15 |

## 9. Canlı doğrulama (API 5162 + vekil 5163, Development, `canli.mjs`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| /islemler PRO açılış | iade-adaylari isteği 0 | **0** |
| "Son satışlar" aç | GET /api/v1/islemler/iade-adaylari?tur=SATIS&limit=20 → 200, 15 kayıt | 200, 15 kayıt, 5 satır (+Hepsini göster) |
| kırılım ikonu | GET /api/v1/islemler/fis/{id} → 200 | 200 (01M3BTCNY4AGCQPNGCCRTK35JS) |
| 1 kalem (KPE-1) Kaydet, ödeme katmanı KAPALI | POST gövdesi eskisiyle aynı alanlar; NAKIT + tek kasa | POST /api/v1/islemler/satis → **200**; `odemeTuru:"NAKIT"`, `kasaId:01M3BTBPK1…` |
| SQL | hareket + ödeme kalemi | `hareketler 01M3C1VENQAFQW7K6ZT6ENPF9P SATIS NAKIT 10274.42` · `odeme_kalemleri NAKIT:10274.42 (1 aktif)` |
Sonra: 5162/5163 süreçleri `kill` (85489/85493/85715) → `lsof` boş. Test kiracısı PAKETSATIS-2-TEST (01M3BTBM4CA2DQZNTKTWPMDGNE) tenant_id kolonlu TÜM tablolardan silindi (kalan satır 0) + davet satırı silindi; tenants 19 → **18** (19'un 1'i başka şeridin o sırada açtığı kiracı). QA-TEST kiracılarına dokunulmadı.

## 10. Değişen dosyalar (bu paket; `git status` hepsi untracked/M)
Web: `Bilesenler/Katlanir.razor` (yeni), `wwwroot/css/katlanir.css` (yeni), `wwwroot/index.html` (link), `wwwroot/css/tuval-satis.css`, `Pages/Islemler.razor`, `Pages/Islemler.Sade.cs` (yeni), `Pages/Islemler.Pro.cs`, `Pages/Islemler.Diger.cs`, `Bilesenler/Fis/{FisSonSatislar, FisProOdemeHapi, FisMusteriSatiri, FisKalemGirisi, FisIslemIzgarasi, FisProKalemKarti, FisProYanSutun, FisVerdimAldim, FisOdemeKasa}.razor`. Core: `I18n/Sozlukler.SatisSade.cs` (yeni, TR+EN). Testler: `SATIS2EkranTestleri.cs` (yeni), `TS1TuvalSatisTestleri.cs`, `TS2TuvalProTestleri.cs`, `S22TezgahFisiTestleri.cs`, `KatalogK1Testleri.cs`, `UX1C1EkranTestleri.cs`, `CumaSAldimSadeTestleri.cs`. FisDigerIslemler, app.css, Sozlukler.cs HUB'ına DOKUNULMADI. Tek dokunuş: Pages/Islemler.razor bu dalgada açıldı.

## 11. Koşul karşılama (paket kapısı)
1 derleme 0 hata ✓ · 2 kendi testlerim 0 başarısız (SATIS2 20/20; Islemler sınıfları 354/354 BICIM-2 öncesi) ✓ · 3 denetim TEMİZ, S2-3 sayaçları ✓ · 4 mutasyon 9/9 kırmızı + cmp ✓ · 5 canlı ✓ · 6 kiracı silindi ✓ · 7 yeni kırmızı: tam süitteki 104 kırmızının tamamı BICIM-2 (SayisalAlan odak dışı biçim) kalıbında; SATIS-2 sınıfı 0.

## 12. Açık kalanlar
- **BICIM-2 şeridi**: `SayisalAlan.cs` odak dışı biçim ~104 testi kırıyor (TS1/TS2/S22/S23/S24/S25/L1/Q102/Kur/P16/P20/UX4b/CumaS…) — testlerin ham değer ("10000") beklentileri o paketin işi.
- `Pages/Islemler.Sade.cs.tmp` (0 bayt, önceki koşudan) — kurucu silmeli.
- `GorunumKartiSozlugu.ModNotu` Ayarlar tuvalinin tasarım notu — KATMAN şeridi değerlendirmeli.
- P-23 denetimi çok kalemli satis-fisi hareket-başı kalem ölçüsü (bkz. §7).
- "↩ İade et" / "⟲ Düzelt" düğmelerindeki tipografik oklar (önceki paketlerden) — Ikon setinde karşılık yok.
- Kurucuya/mali müşavire: mevzuat hükmü eklenmedi; yaklaşma oranı yalnız görsel.
