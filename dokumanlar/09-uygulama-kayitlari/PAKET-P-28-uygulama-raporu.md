# PAKET P-28 — F-4 KİRACI ETİKETLİ TELEMETRİ + DESTEK DAKİKASI KAYDI (R-2)

**Tarih:** 2026-09-23 · **Dalga:** DALGA 7 · **Sürüm:** 1.0 (KOŞULSUZ) · **Roller:** Taşkın (backend) + Ertuğ (frontend payı — bu pakette EKRAN YOK, § Sapmalar'a bakınız)
**Durum:** BİTTİ — derleme 0 uyarı 0 hata · tam süit **3938/3938** yeşil · `dotnet/denetim.sh` **TEMİZ** · canlı doğrulama 12 adım · 10 mutasyon kanıtı · test kiracısı silindi (tenants 9 → 9)

---

## § 0 — DEVİR NOTU: NE DEVRALINDI, NE DOĞRULANDI

Bu paket bir kez başlatılmış, uygulayıcı ajan hesap limitine takılıp **işin ortasında kesilmişti**. Devralınan ağaçta P-28'in yarım izleri vardı. Orkestratörün ölçtüğü gerçek durum (23.09 11:56): derleme temiz, ancak **3 test KIRMIZI**:

| # | Kırmızı test | Mesaj | Gerçek kök neden (bu oturumda bulundu) |
|---|---|---|---|
| 1 | `OlcumKapisiTestleri.P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu` | `Expected: 71 Actual: 69` | `denetim.sh` `BEKLENEN_VARLIK` tabanı P-28'in iki yeni varlığı için **güncellenmemişti** |
| 2 | `OlcumKapisiTestleri.BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit` | `Expected: 71 Actual: 69` | aynı |
| 3 | `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` | `Politika haritasında damgası olmayan varlık(lar): TelemetriOlcumu, DestekDakikasi` | düzeltme politikası haritasına **damga konmamıştı** |

**Orkestratörün okuması düzeltildi (Madde 1):** devir notu "varlıklar IKiraciVarlik sözleşmesini KARŞILAMIYOR" diye yorumlamıştı. **Ölçüm bunu doğrulamadı:** `TelemetriVarliklari.cs:42` ve `:78` zaten `: IKiraciVarlik` uyguluyor, `KuyumcuDbContext` global filtresi de kuruluydu. Testlerin beklediği 71 **gerçek** sayıydı; eksik olan **sayaç tabanı** ve **politika damgasıydı**. Yani "sayacı 69'a düşürerek yeşile boyama" tuzağına düşülmedi — taban **taze ölçümle** 71'e YÜKSELTİLDİ (ölçüm komutu `denetim.sh:374-379` yorumunda yazılı).

Devralınan iş **olduğu gibi kabul edilmedi**: spesifikasyona karşı satır satır denetlendi, eksikler tamamlandı, tüm test ve mutasyon listesi baştan koşuldu (§ 4, § 5).

**Önkoşul P-27 kontrolü:** diskte VAR ve uygulanmış — `Modules/Abonelik/`, migration `20260922031115_AbonelikVePlanKapisi`, `denetim.sh` P-27 çırçır bloğu (`YETKISIZ_PLAN_YAZAN_UC`, `PLAN_DEGISIMI_IZSIZ`, `ABONELIK_FIYAT_KOLONU`) üçü de yeşil. Kapsam daraltılmadı.

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-2** — F-4 telemetri sürümü: KOŞULSUZ 1.0 | **KAPANDI** | Kiracı etiketli sayaç canlıda ölçüldü: `telemetri_olcumleri` 1 satır / 7 istek (canlı adım A2 + SQL) · sır/kişisel veri taşımadığı `TELEMETRIDE_KISISEL_VERI=0` sayacı ve `P28DenetimKapisiTestleri` ile makineyle kanıtlandı · destek dakikası kaydı uçtan uca (POST/PATCH/DELETE/aktife al) canlı doğrulandı · F-8 Faz B'nin iki satırının **ölçüm hattı** belgeye yazıldı, **rakam yazılmadı** (FAIL-CLOSED 1 aktif) |

**F-8 Faz B satırlarının DOLDURULMASI kısmen DEVREDİLDİ** ve bu bilerek yapıldı: plan bloğu "bu telemetri **+ k6 (P-25) + pilot (P-32)** ile doldurulur" der. P-28 ölçüm altyapısını kurar; **rakamı ölçüm üretir**. Bugün üretim yükü ve pilot verisi YOKTUR, dolayısıyla "rakam ölçülmeden F-8'e yazılmaz" kuralı gereği iki satır `OLCULMEDI` kalmıştır. → **devredildi: P-25 (kapasite), P-32 (destek)**.

---

## § 2 — YAPILANLAR

### 2.1 Devralınan ve DOĞRULANAN dilim (kod okunarak denetlendi, değiştirilmedi)
- `Modules/Telemetri/TelemetriVarliklari.cs` — `TelemetriOlcumu` (kiracı+UTC saat penceresi: istek, 5xx hata, işlem ms, yanıt bayt) ve `DestekDakikasi` (tarih, dakika 1-600, kanal, konu, kaynak, aktif, operatör). İkisi de `IKiraciVarlik`. Beyaz listeler: `DestekKanallari`, `DestekKonulari`, `DestekKaynaklari`.
- `Modules/Telemetri/TelemetriAraci.cs` — ara katman; **tek okuduğu** kiracı etiketi (`tenantId` claim), süre (Stopwatch) ve yazılan bayt (`SayanAkis`). WebSocket muaf.
- `Modules/Telemetri/TelemetriToplayici.cs` — singleton bellek içi toplayıcı, 20.000 pencere tavanı, düşürülen ölçüm sayacı (sessiz kayıp yok), `BosaltVeAl`/`GeriKoy`.
- `Modules/Telemetri/TelemetriYazici.cs` + `TelemetriZamanlayici` — periyodik upsert; bilinmeyen kiracı yazılmaz; yazılamayan birikim geri konur.
- `Modules/Telemetri/TelemetriModulu.cs` — `[RolGerekli("SAHIP")]` kiracı ucu (salt okunur) + `SistemAnahtariKapisi` operatör uçları (liste/POST/PATCH/DELETE).
- `Modules/Telemetri/TelemetriSistemServisi.cs` — kapasite özeti, kiracı özeti, destek CRUD + üç durumlu PATCH.
- Migration `20260922034537_P28TelemetriVeDestekDakikasi` (uygulanmış).
- `Kuyumcu.Tests/P28TelemetriTestleri.cs` — 38 test (toplayıcı/yazıcı/uçlar).

### 2.2 BU OTURUMDA EKLENEN / DÜZELTİLEN
1. **Düzeltme politikası haritası** (`Modules/Meta/DuzeltmePolitikasi.cs:119-130`): `TelemetriOlcumu` → `SALT_OKUNUR` / gerekçe `TURETILMIS`; `DestekDakikasi` → `DUZENLENEBILIR`, hedef uç `api/v1/sistem/kiracilar/{id}/destek-dakikasi/{kayitId}`. → kırmızı test 3 kapandı.
2. **`BEKLENEN_VARLIK` tabanı 69 → 71** (`denetim.sh:374-380`), ölçüm komutu ve taze ölçüm yorumda; `ham/olcum-2026-09-18.json` aynı değere çekildi ve değişiklik notu düşüldü. → kırmızı test 1-2 kapandı.
3. **P-28 çırçır bloğu `denetim.sh`'a eklendi** (`# >>> P-28` … `# <<< P-28`, satır 5391-5471), üç sayaç:
   - `TELEMETRIDE_KISISEL_VERI = 0` — otomatik toplama yolunda (`Araci`+`Toplayici`+`Yazici`) `Request.` / `RemoteIpAddress` / `ClaimTypes` / `FindFirst("` / `.Identity` / `Cookies` / `Headers[` **sıfır**, ayrıca şemada kişisel veri kolonu sıfır.
   - `F8_OLCULMEMIS_SATIR` — tavan **2**, **yalnız azalır**; >0 iken F-8'de `FAIL-CLOSED 1 AKTİF` ibaresinin varlığı **zorunlu** (yoksa kapı kırılır).
   - `TELEMETRI_ELLE_F8_RAKAMI = 0` — telemetriye ücret/fiyat/tutar/maliyet kolonu girmesi yasak (K-16).
4. **B-8 ihlali düzeltildi** (P-28'in kendi getirdiği): `TelemetriServisi.Ozet` ve `DestekOzet` sınırsız `ToListAsync()` yapıyordu → `SINIRSIZ_TOLIST` 145 → 147 ARTMIŞTI. Satırları çekmek yerine **sunucuda toplama** (`GroupBy(x => 1)` + `Sum/Count`) uygulandı; `.Take()` ile kırpmak **bilerek seçilmedi** çünkü destek dakikasını eksik gösterip F-8'in tek meşru kaynağını yanlışlardı. Sayaç 145'e döndü.
5. **B-62 ihlali düzeltildi** (P-28'in kendi getirdiği): `DestekOzet` ham `d.Tarih < bitis` desenini geri getirmişti. Üst sınır tek kaynaktan alınır oldu: `Rapor.Mutabakat.UstSinirKapsayici(now.Date)`, değişken adı `ustSinirHaric` (hariç olduğu adında yazılı).
6. **9 yeni yapısal test** (`P28DenetimKapisiTestleri`, aynı dosyanın sonunda): sayaç blokları yerinde mi, otomatik toplama yolu istek içeriğine bakıyor mu (3 dosya × Theory), şemada kişisel veri kolonu, F-8 tavanı yükselmiş mi, ölçülmemiş satır varken fiyat kilidi yazılı mı, telemetride para kolonu, politika damgaları.
7. **Bir test GÜÇLENDİRİLDİ** (mutasyon M28-9 test açığı buldu): `Olcum_yoksa_olcumVarMi_FALSE_doner` yalnız `destek` dalını sınıyordu; `kullanim.OlcumVarMi` sabite çevrildiğinde yeşil kalıyordu. Kullanım dalı için `olcumVarMi=false`, `pencereSayisi=0`, `istekSayisi=0` kilitleri eklendi.
8. **F-8 belgesi güncellendi** (rakam YAZILMADAN): iki `OLCULMEDI` satırının "Dayanak" hücresi artık gerçek ölçüm hattını gösterir (eski hücre "Program.cs:219/313 dışında telemetri yok" diyordu — BAYATTI); yeni bölüm **"F-8 FAZ B — ÖLÇÜM HATTI (P-28)"**; BK-41 için **"GELİŞTİRME MALİYETİ"** bölümü (yöntem yazılı, rakam ölçülmedi).

---

## § 3 — SAPMALAR VE GEREKÇELERİ

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **Ekran (razor sayfası) AÇILMADI** | Plan bloğunun "Dosyalar" listesinde `Pages/` YOKTUR (`Program.cs` · `Modules/Telemetri/` · F-8 belgesi). Kapsam kilidi gereği yeni ekran açılmadı. Destek dakikası uçları **operatör (sistem anahtarı)** uçlarıdır; kiracı ekranından erişilemez ve erişilmemelidir. Aynı desen P-27'de de uygulanmıştı (`KiraciDaveti`: "ekranı Faz B'dedir"). → **devredildi: operatör ekranı (F-2/Faz B hattı)** |
| S2 | **F-8 Faz B rakamları yazılmadı** | "Rakam ölçülmeden F-8'e yazılmaz" (plan DoD + FAIL-CLOSED 1). Üretim yükü ve pilot verisi bugün yok. Altyapı kuruldu, ölçüm hattı belgelendi. → **devredildi: P-25, P-32** |
| S3 | **`F8_OLCULMEMIS_SATIR` bugün 0 değil, 2** | Plan bloğunun kendisi "`F8_OLCULMEMIS_SATIR = 0` **(dalga sonunda)**" der. Sayacı bugün 0 göstermek için satırları silmek/damgayı kaldırmak Madde 1 ihlali olurdu. Sayaç **tavan çırçırı** olarak kuruldu: 2'de mühürlü, **yalnız azalır**; yeni ölçülmemiş satır açmak kapıyı kırar; ayrıca >0 iken fiyat kilidinin belgede yazılı olması zorunlu tutuldu. Bu, koşulun ruhunu (ölçülmeden rakam ilan edilemez) mekanizmaya çevirir. |
| S4 | **BK-41 "GELİŞTİRME MALİYETİ" rakamsız yazıldı** | Ajan-saat ölçülebilir (plan § 9 / ORKESTRATOR-DURUM), **token tüketimi bu depoda kayıtlı değil**, **₺ tutarı kurucudadır** (fatura). Formül ve %25 sapma kuralı belgeye yazıldı, rakam uydurulmadı. → **kurucuya notlar** |
| S5 | Sistem anahtarı canlı doğrulama için **ortam değişkeniyle** verildi | `appsettings.json`'daki değer yer tutucudur → sistem uçları 503 döner. Doğrulama için oturumluk rastgele bir anahtar `Sistem__Anahtari` ortam değişkeniyle geçildi; **hiçbir yere yazılmadı**, çıktıya basılmadı, iş sonunda scratchpad dosyası boşaltıldı. Depo yapılandırması **değiştirilmedi**. |

**Plan ile koşulların çelişkisi:** EK KOŞULLAR "Sayaçlar: … `F8_OLCULMEMIS_SATIR=0`" derken plan bloğu "**(dalga sonunda)**" kaydını taşır. **PLAN ÜSTÜNDÜR** (S3'teki çözüm plana göredir) ve çelişki burada kayda geçirilmiştir.

---

## § 4 — DERLEME, MIGRATION, TESTLER

**Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son koşum 23.09 12:14).

**Migration:** **YENİ ÜRETİLMEDİ** — devralınan `20260922034537_P28TelemetriVeDestekDakikasi` şemayı zaten karşılıyor (`destek_dakikalari`, `telemetri_olcumleri` + 3 indeks; hepsi `tenant_id` önekli).
- `…_P28TelemetriVeDestekDakikasi.cs` = **4.639 bayt** · `….Designer.cs` = **235.071 bayt** (ikisi de 0'dan büyük, `Up()` gövdesi iki `CreateTable` + üç `CreateIndex` içeriyor — okundu).
- `kuyumcu_net` uygulanmış: `__EFMigrationsHistory` = **68** satır, en üstteki `20260922034537_P28TelemetriVeDestekDakikasi`; `SHOW TABLES` iki tabloyu da doğruladı.

**Test sayıları:**

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| ÖNCE (orkestratör, 23.09 11:56) | 3929 | 3926 | **3** | § 0'daki üç kırmızı |
| P-28 filtreli (`~P28`) | 43 | 43 | 0 | 34 devralınan + 9 yeni |
| `~P28 / ~OlcumKapisi / ~DuzeltmePolitikasi` | 73 | 73 | 0 | üç kırmızı kapandı |
| **SONRA — tam süit** | **3938** | **3938** | **0** | trx: `scratchpad/trx-pP-28/_Ramazan-MacBook-Air_2026-09-23_12_14_44.trx`, süre 1,43 dk |

**Yeni test sayısı: 9** (3938 − 3929 = 9). Ayrıca 1 mevcut test güçlendirildi.

---

## § 5 — MUTASYON TABLOSU (10/10 kırmızı)

Yöntem: kaynak scratchpad'e kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` birebir doğrulandı.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|---|
| M28-1 | `TelemetriToplayici.cs` | kiracısız istek muhafızı iptal (`if (false) return;`) | `P28TelemetriTestleri.Kiracisiz_istek_OLCULMEZ` | ✅ birebir |
| M28-2 | `TelemetriToplayici.cs` | pencere yuvarlaması dakikayı korur | `P28TelemetriTestleri.Pencere_saate_yuvarlanir` | ✅ birebir |
| M28-3 | `TelemetriSistemServisi.cs` | PATCH `KanalVerildi` → `true` (gönderilmeyen alanı ezer) | `P28TelemetriTestleri.Patch_gonderilmeyen_alani_KORUR` | ✅ birebir |
| M28-4 | `TelemetriModulu.cs` | DELETE `aktif: false` → `true` (pasife alma bozulur) | `P28TelemetriTestleri.Delete_PASIFE_alir_satir_durur_ve_geri_alinir` | ✅ birebir |
| M28-5 | `TelemetriVarliklari.cs` | `DestekKanallari.Gecerli` → `true` | `P28TelemetriTestleri.Gecersiz_kanal_400_doner` | ✅ birebir |
| M28-6 | `DuzeltmePolitikasi.cs` | `DestekDakikasi` damgası `DUZENLENEBILIR` → `SALT_OKUNUR` | `P28DenetimKapisiTestleri.Telemetri_varliklari_DUZELTME_POLITIKASINDA_damgalidir` | ✅ birebir |
| M28-7 | `denetim.sh` | `P28_F8_TAVAN=2` → `3` (tavan yükseltme) | `P28DenetimKapisiTestleri.F8_olculmemis_satir_tavani_gercek_olcume_esit_ve_YUKSELMEZ` | ✅ birebir |
| M28-8 | `TelemetriAraci.cs` | ara katmana `Request.Path` okuması eklendi | `P28DenetimKapisiTestleri.Otomatik_toplama_yolu_ISTEK_ICERIGINE_bakmaz(dosya: "TelemetriAraci.cs")` | ✅ birebir |
| M28-9 | `TelemetriServisi.cs` | `OlcumVarMi: pencere > 0` → `true` | **İLK KOŞUMDA HİÇBİR TEST KIRILMADI = TEST AÇIĞI.** Test güçlendirildi (§ 2.2/7), M28-9b ile tekrarlandı → `P28TelemetriTestleri.Olcum_yoksa_olcumVarMi_FALSE_doner` KIRMIZI | ✅ birebir |
| M28-10 | `TelemetriSistemServisi.cs` | düzeltme sorgusundan `d.TenantId == tenantId` kaldırıldı (kiracı sızıntısı) | `P28TelemetriTestleri.Baska_kiracinin_kaydi_bu_yoldan_DUZELTILEMEZ` | ✅ birebir |

Son mutasyondan sonra temiz derleme: **0 uyarı 0 hata**.

---

## § 6 — DENETİM ÇIKTISI

`cd dotnet && ./denetim.sh` → çıkış kodu **0**, son satır **`DENETİM TEMİZ.`**

```
  ✓ IKiraciVarlik uygulayan varlık                71
  ✓ Sınırsız ToListAsync (Modules · B-8)          145 (tavan 145)
  ✓ P-24: BITIS_GUNU_OFF_BY_ONE                   0 (kapsayıcı üst sınır tek yerde)
  ✓ P-28: TELEMETRIDE_KISISEL_VERI                0 (ara katman + toplayıcı + yazıcı + şema)
  ✓ P-28: F8_OLCULMEMIS_SATIR                     2/2 (fiyat ilanı KİLİTLİ — P-25/P-32 doldurur)
  ✓ P-28: TELEMETRI_ELLE_F8_RAKAMI                0 (miktar ölçülür, TL kurucuda — K-16)
```

**Madde 1 notu — kendi hatam, gizlenmedi:** P-28 bloğunu eklerken kullandığım `perl -pi -e 's{\Q$KOK/dokumanlar/02-mimari\E}{...}'` ifadesinde `$KOK` kabuk tarafından **boş** yorumlandı ve desen `denetim.sh`'ta iki ilgisiz satırı (`:1334` P-01, `:2289` P-08) `..../dokumanlar` hâline bozdu; denetim `✗ P-01: SURUM_IKINCI_KAYNAK` ve `✗ P-08: BAG_BUTUNLUGU` verdi. Hata ölçümle yakalandı, iki satır onarıldı, `bash -n` sözdizimi kontrolü + yeniden koşumla doğrulandı.

---

## § 7 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

Kiracı: **PAKET28-TEST** (`01M36RZ0RBMX98PGZBS3W2ST2P`), davet → kayıt yoluyla açıldı, iş sonunda silindi.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| A1 | `GET /api/v1/telemetri?gun=7` (SAHIP, yazma turundan önce) | 200, `olcumVarMi=false` | **200**, `kullanim.olcumVarMi=false`, `pencereSayisi=0`, `degismezIpucu` dolu | UX-S25: değişmezlik gerekçesi yanıtta |
| A2 | 5 kiracı isteği + 8 sn bekleme → aynı uç | 200, ölçüm görünür | **200**, `olcumVarMi=true`, `pencere=1`, `istek=6`, `islemMs>0`, `yanitBayt>0` | zamanlayıcı yazma turu çalışıyor |
| A2s | SQL | kiracı etiketli satır | `SELECT COUNT(*),SUM(istek_sayisi),SUM(hata_sayisi) FROM telemetri_olcumleri WHERE tenant_id=…` → **1 / 7 / 0** | — |
| A2k | SQL kolon envanteri | kişisel veri kolonu YOK | `guncellendi,hata_sayisi,id,islem_ms,istek_sayisi,olcum_saati,olusturuldu,tenant_id,yanit_bayt` | **BK-12 canlı kanıt** |
| A3 | `GET /api/v1/sistem/telemetri` **JWT ile** | JWT bu uçları AÇMAZ | **403** "Sistem yönetimi anahtarı geçersiz." | — |
| A4 | aynı uç **anahtarsız** | 403 | **403** | tek tip mesaj (jeton var/yok sızmaz) |
| A5 | aynı uç **anahtarla** | 200, kapasite özeti | **200**; alanlar: `kiraciSayisi, olculenKiraciSayisi, olcumVarMi, toplamIstek, toplamIslemMs, toplamYanitBayt, toplamSatir, toplamDestekDakika, kiraciBasiIstek, …, sinir, sinirinaDegdi, kisit` | UX-S29: sınır yanıtta |
| A6 | `POST …/destek-dakikasi` (45 dk, TELEFON, KURULUM) | kayıt oluşur | **200**, `dakika=45`, `aktif=true` | — |
| A7 | geçersiz kanal `GUVERCIN` | 400 + kabul edilen değerler | **400** "Geçersiz kanal. Geçerli kanallar: TELEFON, UZAKTAN, YERINDE, YAZILI" | UX-S30/S31 |
| A8 | `dakika=601` | 400 | **400** "Dakika 1 ile 600 arasında olmalıdır" | Türkçe mesaj |
| A9 | `PATCH {"dakika":60}` | gönderilmeyen alan **KORUNUR** | **200**, `dakika=60`; `kanal=TELEFON`, `konu=KURULUM`, `aciklama` **korundu** | PATCH sözleşmesi |
| A10 | `PATCH {"aciklama":null}` | null → **SİLER**, diğerleri korunur | **200**, `aciklama=null`, `dakika=60` korundu | PATCH sözleşmesi |
| A11 | `DELETE …/{kayitId}` | **pasife alır**, satır durur | **200**, `aktif=false` | finansal/ölçüm kaydında hard delete YOK |
| A12 | `PATCH {"aktif":true}` | geri dönüş yolu | **200**, `aktif=true`; SQL: `COUNT=1, SUM(dakika)=60, SUM(aktif)=1` | UX-S25 |
| A13 | denetim izi SQL | yazma izi düşer | `denetim_kayitlari`: `TelemetriOlcumu EKLEME 1 / GUNCELLEME 2`, `DestekDakikasi EKLEME 1 / GUNCELLEME 9` | `DenetimServisi` çağrılıyor |

**Temizlik:** API PID öldürüldü, `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. `tenant_id` kolonu olan 69 tablo + `tenants` + `kiraci_davetleri` temizlendi. **`tenants` sayısı 9 → 9** (başlangıçtaki değer). `QA-TEST-*` fikstürlerine **dokunulmadı** (listede dördü de duruyor).

---

## § 8 — DoD SÖZLEŞMESİ (hangi varlık nasıl düzeltilir)

| Varlık | Politika | Düzeltme yolu | Gerekçe |
|---|---|---|---|
| `TelemetriOlcumu` | **SALT_OKUNUR** (`TURETILMIS`) | Yok — doğru yol **yeniden ölçmektir**. Kullanıcıya `OlcumDegismezIpucu` metniyle söylenir ve `GET /api/v1/telemetri` yanıtında `degismezIpucu` alanı olarak döner. | Ölçüm aracının kendi ürettiği türetilmiş satır; düzeltilebilir olsaydı F-8 birim ekonomi paydası "ayarlanabilir" olurdu. |
| `DestekDakikasi` | **DÜZENLENEBİLİR** | `PATCH /api/v1/sistem/kiracilar/{id}/destek-dakikasi/{kayitId}` (üç durumlu: yok=koru, null=sil) · `DELETE` aynı yol = **pasife alır** · geri dönüş: `PATCH {"aktif":true}` | Elle girilen ölçüm kalemi; yanlış girilen dakika birim ekonomiyi bozar. **Hard DELETE YOK** — ölçüm geçmişi silinmez. |

- **Yetki:** kiracı ucu `[Authorize] + [RolGerekli("SAHIP")]` (TAM eşleşme, sınıf düzeyinde); operatör uçları `SistemAnahtariKapisi` (JWT ile açılmaz — A3 canlı kanıt).
- **Denetim kaydı:** mevcut `DenetimServisi`/yazma dinleyicisi **çağrılır**, yeniden yazılmadı (A13 canlı kanıt).
- **Ekran eylemi:** bu pakette ekran yok (§ 3/S1). Kiracı tarafındaki değişmezlik gerekçesi API yanıtında `degismezIpucu` olarak taşınır; operatör ekranı devredildi.
- **Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** **HİÇBİRİ** — `Pages/` altında tek satır değişmedi.

---

## § 9 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu "Dosyalar" | ✅ | `Program.cs` (kayıt: `TelemetriZamanlayici` + `UseMiddleware<TelemetriAraci>`), `Modules/Telemetri/` (7 dosya), F-8 belgesi güncellendi |
| DoD: kiracı etiketli sayaçlar (istek/CPU/depolama) | ✅ | istek+hata+işlem ms (CPU vekili)+yanıt bayt; depolama **satır sayısıyla** (bayt iddia edilmiyor) — A2/A5 |
| DoD: sır/kişisel veri taşımaz, **denetimle kanıtlı** (Mehmet · BK-12) | ✅ | `TELEMETRIDE_KISISEL_VERI=0` + `P28DenetimKapisiTestleri` (4 test) + canlı kolon envanteri A2k + mutasyon M28-8 |
| DoD: F-8 Faz B iki satır doldurulur | ⚠ **KISMEN** | Ölçüm hattı kuruldu ve belgelendi; rakam **P-25/P-32'ye devredildi** (§ 3/S2) |
| DoD: rakam ölçülmeden F-8'e yazılmaz (FAIL-CLOSED 1) | ✅ | Belgeye rakam yazılmadı; `F8_OLCULMEMIS_SATIR` kapısı kilidi mekanize etti (M28-7) |
| Sayaç: `TELEMETRIDE_KISISEL_VERI = 0` | ✅ | denetim çıktısı § 6 |
| Sayaç: `F8_OLCULMEMIS_SATIR = 0` (dalga sonunda) | ⚠ bugün **2/2**, tavan mühürlü | § 3/S3 |
| Mekanizma kuralı: sayaç `denetim.sh`'a eklendi, ölçüm komutu yorumda, taban taze | ✅ | `# >>> P-28` bloğu; üç ölçüm komutu yorumda yazılı |
| Tavan/eşik artırılmaz | ✅ | `SINIRSIZ_TOLIST` 147 → **145**'e geri indirildi (yükseltilmedi); `F8` tavanı 2'de mühürlü ve test kilidi var |
| Tip/sözleşme, Pages'te yeni record yok | ✅ | `Pages/` hiç değişmedi |
| Değişmezler (tek yazma yolu, storno motoru, kiracı izolasyonu, `--no-build` yasak, commit yok) | ✅ | Deftere yazma yok (telemetri defter değil); `IgnoreQueryFilters`/`Find` yok; migration üretilmedi; **hiç git komutu çalıştırılmadı** |
| BK-41 (F-8 geliştirme maliyeti) | ⚠ **yöntem yazıldı, rakam ölçülmedi** | § 3/S4 |
| BK-49 (ölçülmeden rakam telaffuz edilmez) | ✅ | F-8'e rakam girmedi; kapı ibareyi zorunlu tutuyor |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | tenants 9 → 9; dört QA fikstürü listede duruyor |
| 5199'a dokunulmadı | ✅ | yalnız 5198 kullanıldı, süreç durduruldu |

---

## § 10 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` (bu paketin dokunduğu satırlar):

```
 M dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs      (politika damgaları)
 M dotnet/Kuyumcu.Api/Modules/Telemetri/TelemetriServisi.cs   (sunucuda toplama + üst sınır tek kaynak)
 M dotnet/Kuyumcu.Tests/P28TelemetriTestleri.cs               (9 yeni test + 1 güçlendirme)
 M dotnet/denetim.sh                                          (BEKLENEN_VARLIK 71 + P-28 bloğu + iki onarım)
 M dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json (BEKLENEN_VARLIK 71)
 M dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md       (ölçüm hattı + BK-41 bölümü, RAKAMSIZ)
 A dokumanlar/09-uygulama-kayitlari/PAKET-P-28-uygulama-raporu.md
```
> Not: `denetim.sh`, `Modules/Telemetri/`, `Kuyumcu.Tests/` git'te **izlenmeyen** (`??`) yollardır; yukarıdaki damgalar değişikliğin niteliğini anlatır. **Hiçbir git komutu çalıştırılmadı** (COMMIT YOK).

---

## § 11 — AÇIK KALANLAR

1. **F-8 Faz B iki satırı hâlâ `OLCULMEDI`** → P-25 (k6 kapasite) ve P-32 (pilot destek dakikası). Dolunca `P28_F8_TAVAN` 0'a indirilmeli (kapı testi tavanı yükseltmeyi engeller, indirmeyi engellemez).
2. **Operatör ekranı yok** — destek dakikası bugün yalnız API'den girilir/düzeltilir. Kiracı ekranında telemetri kartı da yok.
3. **P-31 tanılama kanalı** destek dakikasını `kaynak=TANILAMA` ile aynı kayda yazacak; alan hazır, bağlantı P-31'de.
4. **Telemetri yazma turu denetim izi üretiyor** (A13: `TelemetriOlcumu EKLEME/GUNCELLEME`). Yük altında `denetim_kayitlari` büyümesi ölçülmedi; P-25 yük testinde izlenmeli.
5. **Depo ağacında eski mutasyon artıkları var** (bu oturumda üretilmedi, önceki paketlerden kalma): `dotnet/denetim.sh.mutbak` ve `Pages/`/`Modules/` altında 9+ `.mutorj` dosyası. Silme kurucuya bırakıldı (hook `rm`'i bloklar).

---

## § 12 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **(Kurucu · BK-41)** Geliştirme maliyeti hesabı iki girdinizi bekliyor: (a) dönem **token tüketimi**, (b) dönem **fatura tutarı (₺)**. İkisi de üründe kayıtlı değil, hesabınızda. Gelince F-8'deki formül tek satırda kapanır. **Tahmin yazılmadı.**
2. **(Kurucu · K-16)** Fiyat ilanı **hâlâ kilitli** ve bu artık bir slogan değil, makine kapısı: `F8_OLCULMEMIS_SATIR` sıfırlanmadan F-8'den "TASLAK" damgası düşerse `denetim.sh` kırılır.
3. **(Kurucu · K-5 / Mehmet)** `appsettings.json`'daki `Sistem:Anahtari` **yer tutucudur** → sistem uçları üretimde 503 döner. Destek dakikası kaydı ve operatör telemetrisi, anahtar rotasyonu (K-5) yapılmadan **canlıda kullanılamaz**.
4. **(Kurucu · KVKK)** Telemetri kişisel veri taşımaz ve bu makineyle kanıtlanır. **Tek istisna operatörün kendi yazdığı** `destek_dakikalari.aciklama` serbest metnidir (300 karakter). Operatör talimatına "açıklamaya müşteri adı/telefonu yazılmaz" maddesi girmelidir — şema bunu engelleyemez, yalnız uyarabilir.
5. **(Mali müşavir)** Bu pakette vergi/mevzuat hükmü **yoktur**; telemetri ticari bir ölçümdür, mali kayıt değildir. Defterle (hareketler/kasa/banka/fatura) hiçbir bağı yoktur.
