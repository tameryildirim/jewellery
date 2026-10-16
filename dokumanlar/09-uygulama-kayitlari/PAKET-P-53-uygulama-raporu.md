# PAKET P-53 — Fatura taslak düzenleme: boş cari listesi + kuyum toplam 400'ü · UYGULAMA RAPORU

- Tarih: 2026-09-24 (gece) · Şerit: **SERİT A** (Sipariş + Fatura) · VT `kuyumcu_test_a` · canlı port **5197**
- Rol: Taşkın (backend) + Ertuğ (frontend) · Kaynak: `TRIYAJ-TUR8-2026-09-23.md` (KRİTİK, tahmin 2,0 sa)
- **Durum: BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı — § 11)

## 1. Spesifikasyonun koda karşı doğrulanması (önce ölç)

| İddia (triyaj) | Ölçülen | Sonuç |
|---|---|---|
| Cari yüklemesi yalnız yeni-fatura modalında (`Faturalar.razor:1182`) | `YeniModalAc` içinde `try { cariler = await Api.GetAsync…("/cari?aktif=true&limit=200") }` — tek yükleme noktası | DOĞRU |
| `DuzenleAc` (:1067-1075) cari yüklemiyor | `DuzenleAc` yalnız alanları dolduruyordu; sayfa açılıp doğrudan Düzenle'ye basılınca `cariler` boş | DOĞRU |
| İstemci `ToplamKuyum => HasDegerKaynagi != "ELLE"` (:1077-1082) | `faturalar.FirstOrDefault(...) is { } cur && cur.HasDegerKaynagi != "ELLE"` | DOĞRU |
| Sunucu `KdvTuru == "KUYUM"` ise her durumda reddediyor (`FaturaModulu.cs:1108-1112`) | `TaslakGuncelle`: `if (fatura.KdvTuru == "KUYUM") throw new BadRequestException("Kuyum (özel matrah) faturasının toplamı…")` (satır 1107) | DOĞRU |
| (ek ölçüm) İstemci sözleşmesi `FaturaVeri` KdvTuru taşıyor mu? | **TAŞIMIYORDU** (`Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs`) — oysa sunucu `CariliYanit` `kdvTuru`'yu liste yanıtında dönüyor | Sözleşmeye `KdvTuru` eklendi (§ 3) |
| (ek ölçüm) PATCH 400 alan sözlüğü (`alanlar`) dönüyor mu? | Dönmüyor — `BadRequestException(string)` yalnız `message` taşıyor; alan bilgili istisna yalnız 409 (`AlanCakismasiException`) | İstemci tarafı alan eşlemesi (§ 4, sapma S-2) |

**Triyajın doğrulayamadıkları — bu şeritte ölçülebilen:**
- *Fatura taslak kalemi için DELETE ucu:* **YOK** (ölçüldü). `FaturaModulu.cs` uç listesi: `HttpGet` ×6, `HttpPost` ×2, `HttpPatch("{id}")`, `HttpPatch("{id}/kalemler/{kalemId}")`, `HttpPatch("{id}/durum")` — `HttpDelete` hiç yok. P-53 kapsamı dışında; kayda geçti (§ 12).
- *Sipariş SubeId, /yedekler gerekçe ucu, /kur/guncel dağılımı, KurSnapshot kökeni:* P-53 kapsamı dışı — ölçülmedi, tahmin edilmedi (SubeId P-52 raporunda MEVCUT olarak ölçülmüş).

**Mevzuat:** Bu paket hiçbir mevzuat hükmü yazmadı/değiştirmedi. Kural ("kuyum/özel matrah taslağında toplam başlıktan değişmez") zaten sunucudaydı; ekran yalnız **aynasını** kurdu. Web araştırması gerekmedi.

## 2. Yapılanlar

1. **Ortak cari yükleyici `CarileriGetir()`** — yeni-fatura modalı ve düzenleme çekmecesi AYNI yardımcıyı kullanır. Başarısızlıkta liste boşaltılır ve hata metni **döner** (çağıran bandına yazar; UX-S28: hata 'kayıt yok'a çevrilmez).
2. **`DuzenleAc` artık async ve cari listesini yükler.** Yükleme başarısızsa: çekmecenin ÜSTÜNDE hata bandı ("Müşteri listesi yüklenemedi…" + sunucu metni) + **Kaydet kilitli** (`Cekmece KaydetAktif=false`, `KilitSebebi` yazılı — UX-S30).
3. **Faturanın mevcut carisi ön seçili**; aktif listede yoksa (ör. sonradan pasife alınmış cari) seçenek olarak başa eklenir — seçici sessizce ilk cariye kaymaz.
4. **`ToplamKuyum` sunucu kuralının aynası:** `duzenlenen is { KdvTuru: "KUYUM" }` — HasDegerKaynagi'ndan bağımsız. Kuyum taslağında toplam alanı DAİMA `disabled`, gövdeye `toplam` konmaz.
5. **Kilit nedeni `DegismezIpucu` ile** (`#degismez-ipucu-FaturaTaslakToplam`): gerekçe + düzeltme yolu ("Taslağı iptal edip doğru toplamla yeniden kesin. Taslak, fatura numarası harcamaz."). Bileşen düğme üretmez.
6. **Sunucu 400'ü alan düzeyinde:** `DuzenleSunucuHatasi` — önce `alanlar` sözlüğü (ileride sunucu dönerse), yoksa mesajın andığı alana (`toplam`/`cari`/`tarih`/`açıklama`) `AlanHatasi` (`#alan-hata-fd-duzenle-*`) + girdiye `hatali` sınıfı; bant yalnız "Form eksik ya da hatalı — işaretli alanları düzeltin." der. Eşlenemeyen mesaj (ör. "Yalnız TASLAK fatura düzenlenebilir…") bandın kendisine yazılır. **Çekmece kapanmaz.** `FormatException` da toplam alanının altına yazılır.
7. **Boş gönderim sessiz değil:** cari boşsa `Zorunlu` → alan altı mesaj, istek gitmez (DoD EK md.3).

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | Yeni ekran metni **ayrı dosyada**: `Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs` (TR+EN, konumsal record — eksik anahtar derleme hatası) | Görev: "Sozlukler.cs'ye DOKUNMA; yeni metin gerekiyorsa DUR ve raporla". Kuyum gerekçesi/düzeltme yolu ve cari-yükleme hatası için sözlükte uygun anahtar yoktu (`FaturaDegismezYolu` KESİLMİŞ belge içindir ve "numara serisi kullanılmış sayılır" der — taslak için YANLIŞ olurdu). Orkestratörün önceki dalgalarda koyduğu desen ("yeni metin için AYRI dosya — `Kuyumcu.Core/I18n/Sozlukler.<Alan>.cs`", bkz. `Sozlukler.Musteriler.cs`, `PesinMusteriSozlugu.cs`) uygulandı; hub'a **dokunulmadı**, yeni dosya hiçbir şeritle çakışmaz. Orkestratör isterse hub'a taşır. |
| S-2 | 400 → alan eşlemesi istemcide, mesajdaki anahtar kelimeyle | Sunucu bu uçta `alanlar` dönmüyor; alan-bilgili 400 istisnası `Common/ApiException.cs`'e (ortak hub, şerit sınırı dışı) eklenmeden kurulamaz. Önce `alanlar` okunur (geleceğe dayanıklı), yoksa eşleme; eşlenemeyen mesaj bantta — sessiz kayıp yok. Kalıcı çözüm önerisi § 12. |
| S-3 | `Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs` → `FaturaVeri`'ye `string KdvTuru = "STANDART"` (sonda, varsayılanlı) | Aynanın kurulabilmesi için ekranın KdvTuru'yu okuması şart; sunucu zaten gönderiyordu. Dosya `Faturalar.razor`'un sözleşme dosyasıdır (P-03 taşıması). Varsayılan sunucu varlığının varsayılanıyla aynı. |
| S-4 | Spesifikasyonun "StandartTaslaktaToplamDuzenlenebilir" testi iki katmanda: HTTP (`P53HttpTestleri`, adı birebir) + bUnit (`…_Ekranda`) | Kabul koşulu "200 döner" (sunucu) ve "toplam düzenlenebilir" (ekran) iki ayrı yüzey. |
| S-5 | Ek testler: `Cari_listesi_yuklenemezse_cekmecede_HATA_BANDI_ve_Kaydet_KILITLI`, `Alana_eslenemeyen_sunucu_hatasi_cekmece_BANDINA_yazilir` | Kapsam maddesi "yükleme başarısızsa hata bandı + Kaydet kilitli" ve UX-S28 için ölçüm. |

**Migration:** gerekmedi (şema değişmedi; sunucu kodu değişmedi).

## 4. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Fatura (TASLAK başlık) | `PATCH /faturalar/{id}` (P-22; `[RolGerekli]` — TEZGAHTAR 403, `P22HttpTestleri.B3_…`) · ekranda **Düzenle** → Cekmece | P-53 çekmeceyi çalışır kıldı (cari listesi + doğru kilit) |
| Fatura toplamı — KUYUM taslak | **Salt okunur** — `DegismezIpucu` gerekçe + yol: taslağı iptal (`PATCH /faturalar/{id}/durum` IPTAL, ekranda **İptal Et** + onay modalı) ve yeniden kes | Has bedeli dayanağı başlıktan yeniden hesaplanamaz (sunucu kuralı) |
| Fatura toplamı — KALEMLİ taslak | Salt okunur başlıkta; kalemler `PATCH /faturalar/{id}/kalemler/{kalemId}` ile | Değişmedi |
| Fatura (KESİLDİ/ÖDENDİ) | Değişmez — `DegismezIpucu` (KesilmisDegismez + FaturaDegismezYolu) | Değişmedi; finansal varlık → hard DELETE yok |
| Yeni varlık | **YOK** | — |

Yeni düğme yok. Mevcut "Kaydet" ya gerçek PATCH atar ya da `disabled` + kilit sebebi yazılı (cari yüklenemedi). Pages/ altında yeni `ModalPencere` kullanımı **0**; tek düzenleme kalıbı Cekmece korundu. Mevcut element id'lerine dokunulmadı; yeni id'ler: `#degismez-ipucu-FaturaTaslakToplam`, `#alan-hata-fd-duzenle-{cari|tarih|aciklama|toplam}`. Satır içi style / sabit renk yok.

**Dokunulan sayfalar:** `Pages/Faturalar.razor` (yalnız bu).

## 5. Test sayıları

- Yeni test: **8** (bUnit 6 — `FaturaEkranTestleri`; HTTP 2 — `P53HttpTestleri`).
- Filtreli koşum (P-53 testleri, `-v n`): **8/8 Başarılı** — `DuzenleCekmecesiCarileriYukler`, `Cari_listesi_yuklenemezse_cekmecede_HATA_BANDI_ve_Kaydet_KILITLI`, `KuyumTaslaginaToplamGonderilmez`, `StandartTaslaktaToplamDuzenlenebilir_Ekranda`, `SunucuHatasiAlanDuzeyindeBasilir`, `Alana_eslenemeyen_sunucu_hatasi_cekmece_BANDINA_yazilir`, `P53HttpTestleri.KuyumTaslaginaCariGuncellemesi200Doner`, `P53HttpTestleri.StandartTaslaktaToplamDuzenlenebilir`.
- Faturalar'ı çizen tüm dosyalar (FaturaEkran + P22Ekran + P22Http + UX1C2Ekran + UX4cEkran + P53Http): **108/108 Başarılı**.
- Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_a`, trx: `scratchpad/trx-pP-53/_Mac_2026-09-24_00_52_39.trx`): **Toplam 4638 · Geçti 4636 · Başarısız 2** (ikisi de başka şeridin — § 11 madde 7).
- Önce: bu şeritte P-53 öncesi taze tam süit koşulmadı; son kayıtlı ölçüm P-52 raporu 4587 (ağaca paralel şeritler test ekliyor, fark P-53'e mal edilemez).

## 6. Mutasyon kanıtları (yöntem: `scratchpad/mut-yedek/P-53/` yedek → `perl -0777 -pi` tek eşleşme → filtreli test → yedeği geri kopyala → `cmp`)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M53-1 | `DuzenleAc`'taki `if (await CarileriGetir() is { } hata) {…}` bloğu silindi | **`DuzenleCekmecesiCarileriYukler`**, `Cari_listesi_yuklenemezse_…_Kaydet_KILITLI` | `cmp` birebir (0) |
| M53-2 | `ToplamKuyum => duzenlenen is { KdvTuru: "KUYUM" }` → eski `… cur.HasDegerKaynagi != "ELLE"` | **`KuyumTaslaginaToplamGonderilmez`** | `cmp` birebir (0) |
| M53-3 | `DuzenleSunucuHatasi`'ndaki `duzenleForm.Ekle(alan, mesaj);` silindi | **`SunucuHatasiAlanDuzeyindeBasilir`** | `cmp` birebir (0) |
| M53-4 | Cekmece `KaydetAktif="@(cariYuklemeHatasi is null)"` → `"@(true)"` | **`Cari_listesi_yuklenemezse_cekmecede_HATA_BANDI_ve_Kaydet_KILITLI`** | `cmp` birebir (0) |

4/4 mutasyon kırmızı. Son mutasyondan sonra `cmp` → "ayni"; temiz `dotnet build Kuyumcu.sln` → **0 Hata · 0 Uyarı**.

## 7. Derleme

`dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı** (ilk derleme ve mutasyon sonrası temiz derleme).
Not (gerçek çıktı, P-53 dışı): ilk filtreli test denemesi `Kuyumcu.Web/Pages/Kur.razor(106,73): error CS0103: 'M' adı geçerli bağlamda yok` ile düştü (Yedek/Kur şeridinin o anki yarım işi); ikinci deneme `StaticWebAssets … The process cannot access the file` (eşzamanlı derleme kilidi). Üçüncü denemede ağaç derlendi, koşum yeşil. Kur.razor'a dokunulmadı.

## 8. denetim.sh

`./denetim.sh` → çıkış 1, **DENETİM BAŞARISIZ** — tek ihlal:
```
✗ P-15: DUZ_SELECT_LIMIT    ölçülen 13 · tablo 12 · açık 9 (tavan 9)
```
Ölçüm (dosya başına `limit=200`): `Islemler.razor:3` (sınıflandırma tablosunda 2) → fazladan nokta **Islemler.razor**'da (POS/Islemler şeridi). `Faturalar.razor`: **önce 2 · sonra 2** (P-53 `limit=200` sayısını değiştirmedi; tek çağrı `CarileriGetir`'e taşındı). P-53'ün ilgili sayaçları ✓ (ModalPencere düzenleme, çıplak metin, satır içi stil vb. — ihlal listesinde yok).

## 9. Canlı doğrulama (5197, `--no-build`, Development, `kuyumcu_net`)

Test kiracısı `PAKETP53-TEST` (tenant `01M384BVE2WPFRQ23B6F5SDWDK`), tek kullanımlık davet `p53-davet-0001` (jeton yalnız scratchpad'de, SHA-256 özeti tabloda; jeton/parola/erişim jetonu ekrana basılmadı, iş sonunda dosyaları boşaltıldı).

| # | Adım | Beklenen | Gerçek | Kanıt (yanıt okundu) |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | `tenantId=01M384BVE2WPFRQ23B6F5SDWDK`, jetonlar `<gizli>` |
| 2 | `POST /cari` ×2 | 201 | **201 / 201** | `P53 Birinci Musteri`, `P53 Ikinci Musteri` |
| 3 | `GET /cari?aktif=true&limit=200` (çekmecenin çağırdığı uç) | 200, ≥1 | **200**, 2 kayıt | `['P53 Birinci Musteri','P53 Ikinci Musteri']` |
| 4 | `POST /faturalar` KUYUM, `hasDeger=500`, toplam 1200 | 200 | **200** | `kdvTuru=KUYUM`, `faturaNo=null`, `durum=TASLAK`, kdvTutar 116.67 |
| 5 | `POST /faturalar` STANDART, toplam 1200 | 200 | **200** | `kdvTuru=STANDART`, kdvTutar 200.00 |
| 6 | KUYUM taslağa **ekranın gönderdiği gövde** (`cariId`+`tarih`+`aciklama`, toplam YOK) | 200 | **200** | `cariId=01M384C7F4…(ikinci)`, `aciklama='P53 kuyum duzeltme'`, `tarih=2026-09-20`, `hasDegerKaynagi=ELLE`, toplam 1200 korunmuş |
| 7 | KUYUM taslağa `{"toplam":1500}` (ekranın artık GÖNDERMEDİĞİ) | 400 | **400** | `Kuyum (özel matrah) faturasının toplamı başlıktan değiştirilemez: … Taslağı iptal edip yeniden kesin (taslak numara tüketmez).` |
| 8 | STANDART taslağa cari+tarih+`aciklama:null`+`toplam:1500` | 200 | **200** | toplam 1500, kdvTutar 250.00, araToplam 1250.00, aciklama `null` (null = sil) |
| 9 | STANDART taslağa `{"toplam":0}` (alan hatası kaynağı) | 400 | **400** | `Toplam sıfırdan büyük olmalıdır` (ekranda `#alan-hata-fd-duzenle-toplam`'a düşen metin — bUnit `SunucuHatasiAlanDuzeyindeBasilir`) |
| 10 | `GET /faturalar` (liste yansıması) | yeni cari | **200** | KUYUM satırı `cariAd='P53 Ikinci Musteri'`, `aciklama='P53 kuyum duzeltme'`; STANDART satırı toplam 1500 |

**SQL kanıtı:**
```
id                          cari_id                     kdv_turu  has_deger_kaynagi toplam  kdv_tutar ara_toplam aciklama            tarih       durum   fatura_no
01M384C7RKW29DY6VAFNC9G31X  01M384C7F4BC2XM3SSKF74AFAP  KUYUM     ELLE              1200.00 116.67    1083.33    P53 kuyum duzeltme  2026-09-20  TASLAK  NULL
01M384C85JAM6D4QGTGMBSPHND  01M384C75HC9YW04G3HZFMVCS1  STANDART  ELLE              1500.00 250.00    1250.00    NULL                2026-09-21  TASLAK  NULL
```
(7. adımın 400'ü KUYUM toplamını 1200'de bıraktı → reddedilen yazma iz bırakmadı; taslaklar numara tüketmedi.)

**API logu (P-53 dışı):** `is_calismalari` benzersiz anahtar çakışması (`…-MASAK_SIB_SURESI-2026-09-24`, `SAKLAMA_ONIZLEME`, `VADE_HATIRLATMA`) — aynı VT'de 5199 zamanlayıcısıyla eşzamanlı iş (P-52 raporunda da ölçülmüş). P-53 akışını etkilemedi.

**Temizlik:** API PID'leri (79406 sarmalayıcı, 79460 dinleyici) `kill` → `lsof -nP -iTCP:5197 -sTCP:LISTEN` = **0 satır**. `tenant_id` sütunlu **70 tablodan** kiracı satırları + `tenants` satırı + davet satırı silindi (silme komutunun çıkış kodu 1'i `grep -v Warning`'in boş çıktısından; sonuç sayımla doğrulandı): 70 tablonun hepsinde sayım **0**, `kiraci_davetleri(p53-davet-0001)=0`, `kullanicilar(p53@test.local)=0`, **`tenants` 10 → 10**. `QA-TEST-` kiracıları (4) ve 5199'a dokunulmadı.

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, P-53'e ait olanlar)

```
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs              (yeni)
?? dotnet/Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs      (dizin git'te izlenmiyor; FaturaVeri'ye KdvTuru eklendi)
?? dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs                     (dizin izlenmiyor; sahte API P-53 senaryoları + 6 test)
?? dotnet/Kuyumcu.Tests/P53HttpTestleri.cs                         (yeni)
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-53-uygulama-raporu.md  (bu dosya)
```
`FaturaModulu.cs` git'te `M` görünür ama **P-53 ona dokunmadı** (önceki paketlerin değişikliği). Proje ağacına geçici dosya bırakılmadı; COMMIT yok.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Düzenle çekmecesi açıldığında cari seçicisi ≥1 seçenek + mevcut cari ön seçili | ✅ | `DuzenleCekmecesiCarileriYukler` (2 seçenek, `value=c2`); canlı #3 |
| Kuyum taslağında toplam devre dışı + nedeni ekranda | ✅ | `KuyumTaslaginaToplamGonderilmez` (`disabled`, DegismezIpucu "iptal edip… numarası harcamaz") |
| Kuyum taslağında cari+tarih+açıklama 200 + listede yansır | ✅ | `P53HttpTestleri.KuyumTaslaginaCariGuncellemesi200Doner`; canlı #6, #10, SQL |
| Standart taslakta toplam 200 | ✅ | `P53HttpTestleri.StandartTaslaktaToplamDuzenlenebilir`, `…_Ekranda`; canlı #8 |
| Sunucu 400'ü çekmecede okunur Türkçe metinle, çekmece kapanmaz | ✅ | `SunucuHatasiAlanDuzeyindeBasilir`, `Alana_eslenemeyen_…`; canlı #7, #9 |
| Yükleme başarısızsa hata bandı + Kaydet kilitli | ✅ | `Cari_listesi_yuklenemezse_…` |
| Mutasyon listesi (2 zorunlu + 2 ek) | ✅ | § 6 |
| Paralel kapı (1) build 0 hata | ✅ | § 7 |
| (2) kendi testlerim 0 başarısız | ✅ | 8/8 |
| (3) kendi sayaçlarım denetim.sh'ta ✓ | ✅ | tek ✗ P-15 Islemler.razor kaynaklı; Faturalar 2→2 |
| (4) mutasyon kanıtı | ✅ | 4/4 |
| (5) canlı doğrulama | ✅ | § 9 |
| (6) test kiracısı silindi | ✅ | tenants 10→10 |
| (7) yeni kırmızı üretilmedi | ✅ | Tam süitteki 2 kırmızı Faturalar dışı dosyalarda; ikisinin de kaynağı koşum sırasında (00:56) başka şeritçe değiştirilen dosyalar |

## 12. Açık kalanlar (sahibiyle)

1. **Tam süit kırmızısı — `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir`**: `Assert.Contains("K995")` başarısız, seçici metni "8 Ayar 14 Ayar 18 Ayar…" (ayar etiketleri değişmiş). Sahibi: `Pages/Emanet.razor` + `Kuyumcu.Core/Domain/Ayarlar.cs` değiştiren şerit (dosyalar 00:56'da değişti; P-53 dokunmadı).
2. **Tam süit kırmızısı — `S32MusterilerEkranTestleri.S32_09_Basit_modda_yeni_form_uc_alan_Daha_fazla_ile_hepsi_acilir`**: `Assert.Empty()` başarısız. Sahibi: `Pages/Cariler.razor` şeridi (S3, dosya 00:56'da değişti).
3. **denetim.sh P-15 DUZ_SELECT_LIMIT**: `Islemler.razor`'da sınıflandırılmamış 3. `limit=200` noktası. Sahibi: Islemler/POS şeridi.
4. **Fatura taslak KALEMİ için DELETE/pasife alma ucu YOK** (ölçüldü). Taslak kalem yanlış eklendiyse tek yol taslağı iptal edip yeniden kesmek. P-53 kapsamı dışı; ayrı paket önerilir (Taşkın).
5. **S-2 kalıcı çözüm önerisi:** `Common/ApiException.cs`'e alan-bilgili 400 istisnası (`AlanHatasiException(alan, mesaj)` → gövdede `alanlar`) eklenip `TaslakGuncelle` onu atarsa istemcideki anahtar-kelime eşlemesi gereksizleşir. Ortak hub olduğu için orkestratör sıralamasına bırakıldı.
6. **S-1:** `Sozlukler.FaturaTaslak.cs` metinleri hub'a taşınmak istenirse orkestratör sırası.
7. **`TaslakGuncelle` denetim kaydı:** bu uçta `DenetimServisi.YazmaKaydet` çağrısı P-53'te incelenmedi (uç P-22'nin; P-53 sunucuya dokunmadı) — doğrulama önerilir.

## 13. Kurucuya / mali müşavire notlar

- Mevzuat hükmü yazılmadı. "Kuyum (özel matrah, KDVK 23/e) faturasının toplamı başlıktan değişmez" kuralı sunucudaki mevcut iş kuralıdır; ekran yalnız aynasını kurdu. Mali müşavir teyidi gerektiren yeni bir eşik/oran/kod eklenmedi.
- Ücretsiz pilot kuralı: fiyat/teklif/tahsilat metni üretilmedi.
