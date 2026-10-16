# Paket 4 — Dosya yükleme ve belge deposu (Faz A) — UYGULAMA RAPORU

> Tarih: 2026-09-16 · Roller: Taşkın (backend) + Ertuğ (frontend) · Dalga 1
> Kaynak spesifikasyon: `dokumanlar/02-mimari/kalan-paketler/PAKET-04-dosya-yukleme-ve-belge-deposu.md`
> (spesifikasyona ve `MIMARI-KALAN-PAKETLER-2026-09-15.md`'ye DOKUNULMADI)
> **COMMIT YAPILMADI** — çalışma ağacında duruyor.

## 1. Özet

Kuyumcu artık sisteme ekrandan dosya ekleyebiliyor. `urun_belgeleri` tablosu genel
bir belge deposuna (`belgeler`) dönüştü; sahip türü kapalı listeyle fail-closed
doğrulanıyor; içerik türü **beyana değil magic byte'a** göre kabul ediliyor
(PDF · JPEG · PNG); ürün düzenleme modalına ortak `BelgeYukleyici` bileşeni ile
yükleme/indirme/pasife alma girişi eklendi (A16'nın "yükleme ekranı yok" açığı
kapandı). Aynı modala 4C/sertifika alanları da eklendi (A16'nın ikinci kalan açığı).

**Durum: BİTTİ.** Derleme 0 hata / 0 uyarı · tam süit 1079/1079 yeşil (3 ardışık) ·
`./denetim.sh` TEMİZ · 15 mutasyonun tamamı kırmızı test üretti · canlı doğrulama
5198 üzerinde gerçek veriyle yapıldı · test kiracıları silindi, `tenants` = 5.

## 2. Spesifikasyonun koda karşı doğrulanması

| Spesifikasyon iddiası | Kodda bugün | Sonuç |
|---|---|---|
| `UrunBelgesi.cs:15-32` `urun_belgeleri`, LONGBLOB, `EnFazlaBayt = 5 MB` | DOĞRU | — |
| Uçlar `BelgeModulu.cs:71-79`, pasife alma `:54-61` | DOĞRU | — |
| `PirlantaKapisi.PdfDogrula` yalnız `%PDF-` denetler | DOĞRU | — |
| DbSet `KuyumcuDbContext.cs:73`, indeks `:125` | DOĞRU | — |
| Kuyumcu.Web'de `InputFile\|IBrowserFile\|multipart` = 0 isabet | DOĞRU | — |
| `indir.js → window.kuyumcuIndir` hazır | DOĞRU | GET için `ApiIstemci.GetBaytAsync` eklendi (yalnız `PostBaytAsync` vardı) |
| `Program.cs`'te `MaxRequestBodySize/RequestSizeLimit` yok | DOĞRU (0 isabet) → Kestrel varsayılanı 30 MB | küresel sınıra DOKUNULMADI |
| `deploy/nginx-kuyumcu.conf`'ta `client_max_body_size` yok, Node (4000) anlatıyor | DOĞRU | conf düzeltildi (aşağıda) |
| `PirlantaTestleri` "değişmeden yeşil kalır" | **YANLIŞ** (Aşkın haklı): `:32 UrunBelgesi.EnFazlaBayt`, `:80 kontrol.UrunBelgeleri` derlenmezdi | yalnız TANIMLAYICILAR değişti, Assert satırları birebir korundu |

## 3. Oylama koşullarının karşılanması

### Bertan (02 Baş Mimar)
| Koşul | Durum | Kanıt |
|---|---|---|
| Belge.cs AÇILMASIN, sınıf aynı dosyada yeniden adlandırılsın | ✅ | `Data/UrunBelgesi.cs` içinde `class Belge`, `[Table("belgeler")]`; ikinci dosya yok, boş .cs yok |
| Down SESSİZ VERİ KAYBI yapmasın, AÇIKÇA hata fırlatsın | ✅ | Migration Down'ında `belge_down_kapisi` CHECK kapısı; canlıda ISLETME satırıyla denendi → `Check constraint 'ck_belge_down_urun_disi' is violated` ve migration DURDU |
| Toplam SUM LONGBLOB'a dokunmasın | ✅ | `db.Belgeler.Where(...).SumAsync(b => (long?)b.Boyut)` — `Icerik` seçilmiyor |
| `denetim.sh` BEKLENEN_VARLIK değişmesin | ✅ | denetim.sh'e HİÇ dokunulmadı; yeni IKiraciVarlik doğmadı (yeniden adlandırma) |
| SIPARIS dalı kapalı listede kalsın + 400 testi yazılsın | ✅ | `SIPARIS_dali_ACIKCA_reddeder_siparis_modulu_henuz_yok` |
| M-B8 testi kodla BİRLİKTE yazılsın | ✅ | `Liste_yanitinda_ICERIK_alani_YOKTUR` + HTTP karşılığı |
| Gövde sınırı ÖNCE ÖLÇÜLSÜN | ✅ | `grep` 0 isabet (Program.cs) ve nginx conf 0 isabet; ölçüm sonucu aşağıda |

### Mehmet (06 Güvenlik — çelişkide öncelikli)
| Koşul | Durum | Kanıt |
|---|---|---|
| K4-1 nginx: yalnız belge yolunda `client_max_body_size 8m`; Kestrel'e `[RequestSizeLimit(8_000_000)]`, küresel sınır dokunulmaz | ✅ | `deploy/nginx-kuyumcu.conf` iki `location` bloğu (belgeler + urunler) 8m; `proxy_pass` 4000 → 5000 düzeltildi; Program.cs'e dokunulmadı |
| K4-2 ISLETME dalı `db.Tenants` değil `sahipId == islem.TenantId` | ✅ | `SahipDogrula` ISLETME dalı; test + M-B11 |
| K4-3 GIDER_PUSULASI çıkarılsın | ✅ **Faz A'da da kapatıldı** | dal 400 döner: "ADR-0008 blob karşılığı — henüz açık değil"; test var |
| K4-4 `Content-Disposition: attachment`, SVG/WebP/GIF kabul edilmez, SVG testi | ✅ | canlı başlık `attachment; filename=gercek.pdf`; `SVG_KABUL_EDILMEZ` testi; canlıda SVG → 400 |
| K4-5 POST/DELETE rol kapıları attribute düzeyinde, eski DELETE dâhil | ✅ | `[RolGerekli]` her iki denetleyicide; `TEZGAHTAR_yukleyebilir_ama_PASIFE_ALAMAZ_403` |
| K4-6 `belge` politikası `OranSiniri.Kullanici` anahtarıyla, yalnız POST, GlobalLimiter'a dokunulmaz | ✅ | OranSiniri.cs: 30/dk; `[EnableRateLimiting(OranSiniri.Belge)]` yalnız POST; canlıda 429 |
| K4-7 304 dalında kiracı/aktif şartı ÖNCE; yabancı kiracı 404 | ✅ | `Icerik_ucu_ETag_doner_304_calisir_YABANCI_KIRACI_404` + M-B13 |

### Aşkın (04 QA)
| Koşul | Durum | Kanıt |
|---|---|---|
| `urun_id` Faz A'da DÜŞÜRÜLMESİN (nullable kalsın) | ✅ | `Belge.UrunId` `string?`, XML yorumunda "sonraki migration'da düşecek" |
| M-B10 silinsin, yerine "sahibi yazılmış satıra dokunmaz" | ✅ | `Sahibi_ZATEN_YAZILMIS_satira_geri_doldurma_DOKUNMAZ` (M-B10 bu testi kırmızıya çevirdi) |
| M-B7 ve "yabancı kiracının ürünü" testleri `OlusturSistem()` (filtresiz) ile | ✅ | `BelgeDeposuTestleri` kiracı testleri filtresiz bağlamda; M-B7 kırmızı üretti (HTTP testi ise yeşil kaldı — Aşkın'ın uyarısı doğrulandı) |
| PirlantaTestleri: yalnız tanımlayıcılar değişsin, Assert satırları korunsun | ✅ | `UrunBelgesi.EnFazlaBayt → Belge.EnFazlaPdfBayt`, `kontrol.UrunBelgeleri → kontrol.Belgeler`; başka satır değişmedi |
| nginx conf dosya ayak izine eklensin | ✅ | değişen dosyalarda |
| bUnit InputFile testi `UploadFiles/InputFileContent`, WaitForAssertion, 3 ardışık koşu | ✅ | `BelgeYukleyiciEkranTestleri` (7 test), 3 tam süit koşusu yeşil |
| M-B8 testi JSON'a serialize + OrdinalIgnoreCase | ✅ | hem yansıma hem JSON üzerinden |
| Tavan testleri gerçek 500 MB yüklemesin; `BelgeAyarlari` ctor'a enjekte, varsayılan değerli | ✅ | `BelgeServisi(db, new BelgeAyarlari { KiraciToplamMb = 1 })`; mevcut çağrılar değişmedi |
| `belge` oran sınırı HTTP testi (31. POST → 429) | ✅ | `Otuz_birinci_yukleme_429_doner` + canlıda 429 |
| Canlı: her adımda HTTP kodu + SQL sayımı; API durdurulup kiracı silinir | ✅ | §8 |

## 4. Sapmalar ve gerekçeleri

1. **CSS sınıf öneki `.belge-*` DEĞİL `.dosya-*`.**
   Spesifikasyon `.belge-satiri/.belge-secici/.belge-onizleme` diyordu. Yazınca
   `TasarimBelirtecTestleri.Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez`
   KIRMIZI oldu: o test `.belge` ile başlayan her seçiciyi "kâğıt/çıktı bloğu"
   sayıyor ve tema belirteci kullanmasını yasaklıyor (Bertan KOŞUL 5 — kâğıt beyaz,
   mürekkep siyah). Ekran bileşeni ise belirteç kullanmak ZORUNDA (UX-2 Faz A1).
   Çözüm: sınıf öneki `.dosya-*` yapıldı. **Element id'leri (`belge-sec`,
   `belge-yukle`) spesifikasyondaki gibi KALDI**; yalnız bUnit'teki `.belge-satiri`
   kancası `.dosya-satiri` oldu.
2. **GIDER_PUSULASI Faz A'da da kapalı.** Mehmet K4-3 yalnız Faz B için yazılmıştı;
   ancak kapalı liste Faz A'da kuruluyor. Güvenlik önceliği gereği dal Faz A'da da
   400 döndürüyor (test + canlı kanıt). Kapalı listede KALDI ki paket 3/ADR-0008
   kararı geldiğinde dal görünür olsun.
3. **`Data/UrunBelgesi.cs` dosya adı değişmedi** (sınıf `Belge` oldu). `git mv`/`rm`
   hook tarafından bloklanıyor. **Kurucu elle yeniden adlandırabilir**; davranışa
   etkisi yok.
4. **6 MB PDF canlıda 400 değil 413 döndü.** Spesifikasyon "6 MB PDF → 400" diyordu;
   6 MB ham PDF base64'te 8,4 MB gövde eder ve Mehmet'in K4-1 sınırı (8 MB) uç
   kapısından ÖNCE devreye girer. Bu doğru davranıştır (blob hiç belleğe alınmaz).
   Uygulama tavanının 400'ü ayrıca **5,5 MB PDF** ile kanıtlandı (gövde 7,33 MB < 8 MB)
   → `"Belge en fazla 5 MB olabilir"`, HTTP 400.
5. **KAPSAM DIŞI BULUNAN HATA DÜZELTİLDİ:** 8 MB sınırı aşıldığında Kestrel'in
   `BadHttpRequestException(413)`'ü yakalanmıyordu ve kullanıcı **HTTP 500
   "Internal server error"** görüyordu (canlıda ölçüldü). `Common/ApiException.cs`
   içindeki `HataYakalayici`'ya eşleme eklendi → 413 "Payload Too Large".
   3 test (`GovdeSiniriTestleri`) + canlı kanıt.
   ⚠ Bu eşleme HTTP süitinde sınanamaz: `WebApplicationFactory`'nin TestServer'ında
   `[RequestSizeLimit]` devrede DEĞİL (8,4 MB gövdeli istek 413 değil 400 aldı —
   ölçüldü). Bu yüzden middleware düzeyinde sınandı ve gerçek 413 canlıda gösterildi.
6. **`ApiIstemci.GetBaytAsync` eklendi.** İndirme ucu GET; depoda yalnız
   `PostBaytAsync` vardı. Mevcut metotlara dokunulmadı.
7. **4C/sertifika alanları düzenleme modalına eklendi** (spesifikasyonun "Ekranlar"
   maddesi). PATCH ucu bunları zaten kabul ediyordu (`UrunModulu.Guncelle` ~515);
   yalnız ekran girişi yoktu. ⚠ Mevcut PATCH sözleşmesi bu alanlarda
   **null = "dokunma"** anlamındadır (silme değil); ekrandan 4C SİLİNEMEZ.
   PIRLANTA dışı tipte 4C gövdeye hiç konmaz (`PirlantaKapisi` aksi halde reddeder).

## 5. Migration

- **Ad:** `20260916060026_BelgeDeposu`
- **Gövde ELLE yazıldı.** EF'in ürettiği taslak `DropTable("urun_belgeleri") +
  CreateTable("belgeler")` idi — mevcut sertifika satırlarını **SİLERDİ**. Veri
  koruyan işlemlere çevrildi: `RenameTable` → `AddColumn(sahip_turu/sahip_id)` →
  `AlterColumn(urun_id nullable)` → geri doldurma → eski indeks düşer, iki yeni indeks.
  Designer + snapshot EF'in ürettiği hâliyle bırakıldı (hedef modeli doğru yansıtıyor).
- **Geri doldurma metni testle ORTAK:** `Data/GeriDoldurma/BelgeSahipGeriDoldurma.cs`.
- **kuyumcu_net'e uygulandı.** Kanıt (migration ÖNCESİ elle tohumlanan eski satırla):

| Adım | SQL | Sonuç |
|---|---|---|
| Önce | `SELECT COUNT(*) FROM urun_belgeleri` | `0` (tablo boştu) → kanıt üretmek için `P4-ESKI-SATIR` (urun_id=`P4-ESKI-URUN`) elle eklendi, sayı `1` |
| Up | `SELECT id,sahip_turu,sahip_id,urun_id FROM belgeler` | `P4-ESKI-SATIR · URUN · P4-ESKI-URUN · P4-ESKI-URUN` — satır korundu, sahip deterministik dolduruldu |
| İndeksler | `SHOW INDEX FROM belgeler` | `IX_belgeler_tenant_id_sahip_turu_sahip_id`, `IX_belgeler_tenant_id_sha256`, eski indeks yok |
| Down | `dotnet ef database update PirlantaVeBelge` | `Reverting…Done` · `SELECT id,urun_id FROM urun_belgeleri` → `P4-ESKI-SATIR · P4-ESKI-URUN` |
| Up (tekrar) | `dotnet ef database update` | `Applying…Done` · satır ve sahip aynı |
| **Down kapısı** | ISLETME sahipli satır eklenip Down denendi | `MySqlConnector.MySqlException: Check constraint 'ck_belge_down_urun_disi' is violated.` — **migration DURDU, veri kaybı olmadı** |

Tohum satır ve geçici kapı tablosu temizlendi: `SELECT COUNT(*) FROM belgeler` → `0`,
`SHOW TABLES LIKE 'belge%'` → yalnız `belgeler`.

## 6. Testler

| Kapsam | Önce | Sonra | Başarısız | Atlanan |
|---|---|---|---|---|
| Tam süit (`dotnet test`) | 1027 | **1079** | 0 | 0 |

3 ardışık tam koşum: 1079 / 1079 / 1079 — hepsi 0 başarısız (kararsızlık görülmedi).

**Yeni test dosyaları (52 yeni test):**

| Dosya | Tür | Test |
|---|---|---|
| `Kuyumcu.Tests/BelgeKapisiTestleri.cs` | saf | 14 |
| `Kuyumcu.Tests/BelgeDeposuTestleri.cs` | DB | 14 |
| `Kuyumcu.Tests/BelgeGeriDoldurmaTestleri.cs` | DB | 4 |
| `Kuyumcu.Tests/BelgeYukleyiciEkranTestleri.cs` | bUnit | 7 |
| `Kuyumcu.Tests/BelgeHttpTestleri.cs` | HTTP | 6 |
| `Kuyumcu.Tests/GovdeSiniriTestleri.cs` | saf (middleware) | 3 |
| `Kuyumcu.Tests/UrunEkranTestleri.cs` (mevcut dosyaya ek) | bUnit | 4 |

## 7. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M-B1 | `BelgeKapisi.MimeTani` her içeriğe `application/pdf` der (magic byte yok) | `BelgeKapisiTestleri.Rastgele_baytlar_REDDEDILIR_ve_mesaj_imza_kelimesini_icerir`, `.JPEG_imzasi_taninir`, `.PNG_imzasi_taninir`, `.SERTIFIKA_turune_JPEG_verilince_REDDEDILIR`, `.EK_turu_hem_PDF_hem_gorsel_kabul_eder`, `.Iki_MB_bir_bayt_JPEG_REDDEDILIR`, `.PirlantaKapisi_PdfDogrula_eski_mesajini_AYNEN_uretir`, `PirlantaKapisiTestleri.Pdf_imzasi_ve_boyut_denetlenir`, `BelgeHttpTestleri.Uzantisi_pdf_ama_icerigi_metin_olan_dosya_400_ve_SIFIR_SATIR` (9) | AYNI |
| M-B2 | SERTIFIKA "yalnız PDF" kısıtı → "imza tanınsın yeter" | `BelgeKapisiTestleri.SERTIFIKA_turune_JPEG_verilince_REDDEDILIR` | AYNI |
| M-B3 | `SahipDogrula` çağrısı kaldırıldı | `BelgeDeposuTestleri.Olmayan_sahip_404_ve_SATIR_YAZILMAZ`, `.Yabanci_kiracinin_urunune_yukleme_404_FILTRESIZ_baglamda`, `.ISLETME_sahibi_KENDI_kiracisidir_baska_kiracinin_kimligi_404`, `.SIPARIS_dali_ACIKCA_reddeder_siparis_modulu_henuz_yok`, `.GIDER_PUSULASI_dali_KAPALIDIR_ayri_karara_baglandi` (5) | AYNI |
| M-B4 | Sahip başına aktif belge tavanı kaldırıldı | `BelgeDeposuTestleri.Sahip_basina_aktif_belge_tavani_ASILAMAZ` | AYNI |
| M-B5 | Kiracı toplam blob tavanı kaldırıldı | `BelgeDeposuTestleri.Kiraci_toplam_tavani_asilinca_400_ve_mesaj_MEVCUT_kullanimi_yazar` | AYNI |
| M-B6 | `PasifeAl` gerçekten siler (`db.Belgeler.Remove`) | `BelgeDeposuTestleri.DELETE_sonrasi_liste_bos_satir_AKTIF_0_ve_SILINMEMIS`, `PirlantaBelgeTestleri.Sertifika_PDF_yuklenir_listelenir_icerik_ve_sha256_dogru_baska_kiraci_GOREMEZ` | AYNI |
| M-B7 | İçerik ucundan `TenantId` şartı kaldırıldı | `BelgeDeposuTestleri.Yabanci_kiracinin_belgesinin_icerigi_404_FILTRESIZ_baglamda` ⚠ HTTP testi YEŞİL kaldı (global filtre örtüyor) — Aşkın'ın uyarısı doğrulandı | AYNI |
| M-B8 | Liste ucu `Icerik` alanını da seçer | `BelgeDeposuTestleri.Liste_yanitinda_ICERIK_alani_YOKTUR`, `BelgeHttpTestleri.Liste_ucu_JSON_yanitinda_icerik_alani_YOKTUR` | AYNI |
| M-B9 | Ekranda boyut kapısı kaldırıldı | `BelgeYukleyiciEkranTestleri.Boyutu_asan_dosyada_istek_GONDERILMEZ_ve_sebep_ekranda_YAZAR` | AYNI |
| M-B10 | Geri doldurma SQL'inin WHERE koruması kaldırıldı (Aşkın'ın yeni tanımı) | `BelgeGeriDoldurmaTestleri.Sahibi_ZATEN_YAZILMIS_satira_geri_doldurma_DOKUNMAZ` | AYNI |
| M-B11 | ISLETME dalı her `sahipId`'yi kabul eder (Mehmet K4-2) | `BelgeDeposuTestleri.ISLETME_sahibi_KENDI_kiracisidir_baska_kiracinin_kimligi_404` | AYNI |
| M-B12 | DELETE ucundaki `[RolGerekli]` kaldırıldı (K4-5) | `BelgeHttpTestleri.TEZGAHTAR_yukleyebilir_ama_PASIFE_ALAMAZ_403` | AYNI |
| M-B13 | 304 dalı kiracı şartından ÖNCE çalışır (K4-7) | `BelgeHttpTestleri.Icerik_ucu_ETag_doner_304_calisir_YABANCI_KIRACI_404` | AYNI |
| M-B14 | 4C alanları PATCH gövdesinden çıkarıldı | `UrunEkranTestleri.PIRLANTA_4C_degisikligi_PATCH_govdesine_yansir` | AYNI |
| M-B15 | Düzenleme modalından `BelgeYukleyici` kaldırıldı | `UrunEkranTestleri.Duzenleme_modalinda_BELGE_YUKLEYICI_vardir_ve_urune_baglidir` | AYNI |

**Test açığı bulunmadı** — 15 mutasyonun tamamı en az bir testi kırmızıya çevirdi.
Son mutasyondan sonra temiz `dotnet build`: **0 hata, 0 uyarı**.

## 8. Canlı doğrulama (localhost:5198 — 5199'a DOKUNULMADI)

Test kiracıları: `PAKET4-TEST` (`01M2MDWD7JRNAWJKC28GBEG275`) ve `PAKET4-TEST-B`.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | Sağlık ucu | 200 | **200** | `curl /health` |
| 2 | Gerçek PDF yükle (`%PDF-1.7`, 69 B) | 200 + sha256 | **200**, `sha256=2cbd2d32…5ee9` | yanıt gövdesi |
| 3 | SHA-256 kaynakla karşılaştır | birebir | **birebir** | `shasum -a 256 gercek.pdf` = `2cbd2d32…5ee9` |
| 4 | İndir | `application/pdf`, `%PDF-`, ETag, attachment | **200**, `Content-Type: application/pdf`, `Content-Disposition: attachment; filename=gercek.pdf`, `ETag: "2cbd2d32…"`, `Cache-Control: private, must-revalidate`, `X-Content-Type-Options: nosniff`, indirilen sha = kaynak sha, ilk 5 bayt `%PDF-` | başlık dökümü |
| 5 | `If-None-Match` ile tekrar | 304 | **304** | — |
| 6 | Gerçek JPEG yükle (GORSEL) | 200 | **200**, `mime=image/jpeg` | — |
| 7 | Aynı JPEG'i SERTIFIKA türüyle | 400 | **400** `"Belge PDF değil (imza %PDF- bulunamadı)"` | — |
| 8 | Uzantısı `.pdf` içeriği metin | 400, mesajda "imza" | **400** aynı mesaj | — |
| 9 | SVG (`<svg onload=alert(1)>`) | 400 | **400** `"Dosya imzası tanınmadı: yalnız PDF, JPEG ve PNG kabul edilir"` | K4-4 |
| 10 | 6 MB PDF (gövde 8,4 MB) | reddedilsin, 0 satır | **413** `"İstek gövdesi çok büyük — belge tavanını aşıyor"` (düzeltme öncesi 500'dü) · `SELECT COUNT(*) FROM belgeler WHERE tenant_id=…` = **2** (değişmedi) | §4/5 |
| 11 | 5,5 MB PDF (gövde 7,33 MB, sınır altı) | 400 | **400** `"Belge en fazla 5 MB olabilir"` | uygulama tavanı |
| 12 | 2,5 MB JPEG | 400 | **400** `"Görsel en fazla 2 MB olabilir"` · satır sayısı değişmedi | — |
| 13 | ISLETME logosu (PNG, kendi kiracı kimliği) | 200 | **200**, `sahipTuru=ISLETME` | K4-2 |
| 14 | ISLETME + başka kiracı kimliği | 404 | **404** `"İşletme bulunamadı"` | K4-2 |
| 15 | SIPARIS dalı | 400 | **400** `"Sipariş modülü henüz yok — SIPARIS sahibine belge bağlanamaz"` | Bertan |
| 16 | GIDER_PUSULASI dalı | 400 | **400** `"Gider pusulası eki ayrı karara bağlandı (ADR-0008 blob karşılığı) — henüz açık değil"` | Mehmet K4-3 |
| 17 | Yabancı kiracının belgesinin içeriği | 404 | **404** `"Belge bulunamadı"` | — |
| 18 | Yabancı kiracının ürününe yükleme | 404 | **404** `"Ürün bulunamadı"` | — |
| 19 | Eski ürün yolu `GET /urunler/{id}/belgeler` | çalışır, içerik yok | **200**, iki satır, `icerik` alanı YOK | A16 kanıtı korundu |
| 20 | `DELETE /belgeler/{id}` | 200, satır durur | **200** `aktif:false` · SQL: `01M2MDWX…AGX · URUN · 0` (satır SİLİNMEDİ) | — |
| 21 | Oran sınırı | 429 | **429** (aynı dakikadaki 24. POST'ta; önceki istekler kotayı harcamıştı) | GlobalLimiter zincirlenmesi doğrulandı |
| 22 | API durduruldu | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** | — |
| 23 | Test kiracıları silindi | `tenants` = 5 | **5** · `SELECT COUNT(*) FROM belgeler` → **0** | 31 tablodan `tenant_id` ile silindi |

## 9. Denetim

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                31
  ✓ HasQueryFilter                                 2
═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı / HTTPS yönlendirme / sıkıştırma / sağlık ucu   var
  ✓ CORS                                           beyaz liste
DENETİM TEMİZ.
```

### ⚠ `./yetim-tara.sh` ÇIKIŞ 2 — PAKET 4'ten DEĞİL, MEVCUT KİRLİLİK (gizlenmiyor)

`./yetim-tara.sh` çıkış kodu **0 değil 2** verdi. Toplam yetim 0, **kiracı sızıntısı 2**:

| Tablo | Sütun | Sızıntı | Satır | Oluşturulma |
|---|---|---|---|---|
| `hareketler` | `cari_id` | 1 | `01M240M0TN4TQHKRMVJTYWPJ37` (tenant `01M24019NQ…`, cari `01M23ZP11E…`) | **2026-09-09 21:19:12** |
| `kasa_hareketleri` | `kasa_id` | 1 | `01M240M0SY6B83WH6SDNAWN167` | **2026-09-09 21:19:12** |

Bu iki satır **2026-09-09'da**, yani bu paketten bir hafta önce yazılmış geliştirme
verisidir; paket 4 `hareketler`/`kasa_hareketleri` tablolarına HİÇ dokunmadı.
Betiğin kendi kuralı gereği **sessizce düzeltilmedi**: Mehmet (06 Güvenlik) ve
Kurucu'ya eskale ediliyor. Ayrıca `yetim-tara.sh` `belgeler` tablosunu taramıyor
(eski `urun_belgeleri` de taranmıyordu) — ilişki listesine `belgeler.sahip_id`
polimorfik olduğu için doğrudan eklenemez; ayrı bir karar ister.

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından paket 4'e ait olanlar:

```
 M deploy/nginx-kuyumcu.conf
 M dotnet/Kuyumcu.Api/Common/ApiException.cs
 M dotnet/Kuyumcu.Api/Common/OranSiniri.cs
 M dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
 M dotnet/Kuyumcu.Api/Data/UrunBelgesi.cs            (sınıf UrunBelgesi → Belge)
 M dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs
 M dotnet/Kuyumcu.Api/Modules/Urun/PirlantaKapisi.cs
 M dotnet/Kuyumcu.Api/Program.cs
 M dotnet/Kuyumcu.Api/appsettings.json
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
 M dotnet/Kuyumcu.Tests/PirlantaTestleri.cs          (yalnız tanımlayıcılar)
 M dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs
 M dotnet/Kuyumcu.Web/Pages/Urunler.razor
 M dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? dotnet/Kuyumcu.Api/Data/GeriDoldurma/BelgeSahipGeriDoldurma.cs
?? dotnet/Kuyumcu.Api/Data/Migrations/20260916060026_BelgeDeposu.cs
?? dotnet/Kuyumcu.Api/Data/Migrations/20260916060026_BelgeDeposu.Designer.cs
 M dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
?? dotnet/Kuyumcu.Api/Modules/Urun/BelgeKapisi.cs
?? dotnet/Kuyumcu.Tests/BelgeKapisiTestleri.cs
?? dotnet/Kuyumcu.Tests/BelgeDeposuTestleri.cs
?? dotnet/Kuyumcu.Tests/BelgeGeriDoldurmaTestleri.cs
?? dotnet/Kuyumcu.Tests/BelgeYukleyiciEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/BelgeHttpTestleri.cs
?? dotnet/Kuyumcu.Tests/GovdeSiniriTestleri.cs
?? dotnet/Kuyumcu.Web/Bilesenler/BelgeYukleyici.razor
```

(Not: `dotnet/Kuyumcu.Api/Data/`, `Modules/Urun/`, `Kuyumcu.Tests/` ve
`Migrations/` dizinlerinin bir kısmı depoda zaten izlenmemiş (`??`) durumdadır;
yukarıdaki işaretler dosya bazında gerçek durumu anlatır.)

## 11. Açık kalanlar

1. **`Data/UrunBelgesi.cs` dosya adı** — sınıf `Belge`, dosya adı eski. `rm`/`mv`
   hook tarafından bloklu; **kurucu elle `Belge.cs` yapabilir**.
2. **`belgeler.urun_id`** — nullable, artık YAZILMIYOR. Geri-doldurma testi Faz A'da
   ona muhtaç; **sonraki migration'da düşürülmeli**.
3. **Faz B** (sonraki dalga): ürün birincil görselinin listede/POS'ta gösterimi,
   işletme logosu tüketimi (paket 5). **GIDER_PUSULASI Faz B'den ÇIKARILDI** (K4-3).
4. **Cari kimlik belgesi / gider pusulası fotoğrafı** — ADR-0008 (maskeleme) ile
   blob gerçeği çelişiyor; ayrı karar bekliyor. Kodda dal 400 döndürüyor.
5. **`yetim-tara.sh` 2 kiracı sızıntısı** (§9) — paket 4 kaynaklı değil, Mehmet +
   Kurucu kararı bekliyor.
6. **`yetim-tara.sh` `belgeler` tablosunu taramıyor** — `sahip_id` polimorfik;
   `denetim_kayitlari.varlik_id` gibi "kapsam dışı" listesine mi girsin, yoksa
   `sahip_turu`'na göre koşullu tarama mı yazılsın: karar gerekiyor.
7. **nginx conf yalnız DOSYA olarak düzeltildi** — canlı bir nginx örneği bu makinede
   çalışmıyor, dolayısıyla "nginx ÜZERİNDEN 5 MB PDF ile 200" doğrulaması
   **YAPILAMADI**. Doğrulama dağıtım ortamında yapılmalıdır. (Kestrel tarafı 5198
   üzerinde doğrudan doğrulandı.)
8. **`.mutorj` artıkları ÖNCEKİ oturumlardan** — proje ağacında 12 adet `*.mutorj`
   dosyası duruyor (`BelgeModulu.cs.mutorj`, `PirlantaKapisi.cs.mutorj` dâhil).
   Bu oturumda ÜRETİLMEDİ (ilk `grep`te zaten vardılar); bu paketin mutasyon
   yedekleri scratchpad altında tutuldu ve hepsi `cmp` ile geri alındı.
   Silme `rm` hook'u yüzünden kurucuya bırakılıyor.
9. **Sipariş sahibi (paket 3)** — dal 400 döndürüyor; paket 3 gelince açılacak,
   testi hazır.

## 12. Kurucuya / mali müşavire notlar

- **Mevzuat hükmü koda SABİT YAZILMADI.** Belge tavanları (PDF 5 MB, görsel 2 MB,
  kiracı toplamı 500 MB, sahip başına 20) mevzuat değil **işletme politikası**dır ve
  ADR-0005 gereği `appsettings.json → "Belge"` bölümünden okunur. Değiştirmek için
  kod değişmez.
- **Saklama süresi (MASAK 8 yıl) belge blob'ları için UYGULANMADI.** Belge silinmez,
  yalnız pasife alınır; ancak `SaklamaSuresiIsi`'nin blob davranışı tanımlı değil.
  Kimlik/pusula görüntüleri bu yüzden kapalı tutuldu. **Mali müşavire sorulmalı:**
  gider pusulası ekinin (satıcı TCKN'si + ıslak imza görüntüsü) saklama ve
  imha rejimi nedir? Karar gelmeden bu dal açılmayacak.
- **Dosyalar veritabanında** duruyor (diskte değil). Yedek betiği tabloları
  doğruladığı için belgeler yedeğin İÇİNDE. 500 MB/kiracı tavanı bu yüzden anlamlıdır.
- **Kurucunun elle yapacakları:** (a) `Data/UrunBelgesi.cs` → `Data/Belge.cs` yeniden
  adlandırma, (b) `deploy/nginx-kuyumcu.conf`'un dağıtım sunucusuna kopyalanıp
  `nginx -t` ile sınanması (eski conf hâlâ Node 4000'e vekillik ediyor ve 1 MB
  varsayılanıyla 5 MB PDF'i 413'e düşürür), (c) §9'daki iki sızıntı satırı hakkında
  Mehmet ile karar.
