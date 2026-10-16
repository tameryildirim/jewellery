# PAKET MASAK-ESZAMAN — Uygulama raporu

**Tarih:** 09.10.2026 · **Rol:** Taşkın (backend) · Ertuğ (ön yüz — bu pakette ekrana dokunulmadı) · **Şerit:** t5 (API 5166 · KUYUMCU_TEST_DB=kuyumcu_test_t5)
**Kaynak:** KAPORA-ESZAMAN hasım tur 2 KRİTİK — `TahsilatGirHareket` içinde `masak.DogrulaKumulatif` işlem açılmadan ve cari kilidi alınmadan koşuyordu; aynı cariye eşzamanlı 10 × 40.000 TL HAVALE beyansız 10 × 200 (400.000 TL kimliksiz).
**COMMIT YOK. Migration: gerekmedi** (yeni tablo/kolon yok). **Eşik/kural değişmedi** (YasalParametreServisi / MasakKuralMotoru / TevsikKurali aynen).

## 1. Yapılanlar

| Ne | Nasıl | Nerede |
|---|---|---|
| Ortak yardımcı `YasalKilit` | `BaslatVeyaDevral()` işlem yoksa **READ COMMITTED** açar, varsa devralır. `CariIcinBaslat(tenant, cari, an)` işlemi açar/devralır ve **ilk iş olarak cari satırını kilitler** (`SELECT … FOR UPDATE`, mevcut `Kilitle(KilitTablosu.Cariler)`). `CariKilitle` devralınan dış işlem READ COMMITTED değilse **tazelik denetimi** yapar (carinin bugünkü `hareketler` + `banka_hareketleri` satır sayısı tutarlı okuma vs `FOR SHARE`; farklıysa `InvalidOperationException` — işlem yazılmaz, fail-closed). | `Api/Common/SatirKilidi.cs` (ek) |
| MASAK kilit altı yineleme | `MasakKapisi.KilitAltindaYinele(...)`: işlem yoksa fırlatır; carisiz/Peşin Müşteri → Faz 1 sonucu aynen (kümülatif yok); aksi hâlde **aynı** `GunlukIslemToplam` + **aynı** `Dogrula` ile güncel toplamla yeniden karar; Faz 1'de kademe yokken doğduysa kimlik taraması burada. Kural/eşik kodu değişmedi; yalnız okumanın ZAMANI. | `Api/Modules/Masak/MasakKapisi.cs` |
| Kimlik taraması işlem içinde | `KimlikTara`: açık işlem varsa ve belge no yasal listede TAM eşleşiyorsa işlem önce **geri alınır** (`RollbackTransactionAsync` + `BekleyenleriGeriAl`), sonra eşleşme kaydı + KRİTİK bildirim kalıcı yazılır, 400. Önceden sipariş kaporasında (tahsilat dış işleme katıldığından) bu kayıt reddedilen işlemle birlikte kayboluyordu (KAPORA-ESZAMAN'ın yan etkisi) — ME19 ile kanıtlandı. | `MasakKapisi.cs` |
| Her yolda sıra: Faz 1 (işlem dışı, aynen) → **işlem + cari kilidi** → **kümülatif kapılar kilit altında yinelenir** → yazma | Faz 1 korunur (kimlik taraması kaydı işlem dışında kalıcı kalsın diye); Faz 2'nin `masakGerek`'i yinelemeden gelir. Tevsik (`tevsik.Dogrula`) ve taksit (`TaksitDogrula`) aynı argümanlarla kilit altında yinelenir. | aşağıdaki tarama tablosu |
| Dış işlem açan uçlar READ COMMITTED | Sipariş kaporası ve siparişi satışa çevir dış işlemi `BaslatVeyaDevral()` (RC) ile açar: tahsilat/satış fişi devralınca kilit sonrası toplam, sipariş okumasının kurduğu anlık görüntüye takılmaz. | `SiparisModulu.cs` `Kapora`, `SatisModulu.P21.cs` `SiparisiSatisaCevir` |
| Nullable işlem | Devralma mümkün olduğundan `await tx.CommitAsync()` → `if (tx is not null)` (17 satır, derleyici uyarısıyla bulundu; davranış: tek başına çağrıda aynen). | ilgili modüller |

## 2. Sınıf taraması (kümülatif MASAK / tevsik / taksit kontrolü yapan TÜM yollar)

"Önce" sütunu: kontrol işlem açılmadan, kilitsiz. "Sonra": kontrol kilit altında yinelenir. Satır numaraları güncel koddan.

| Yol | Dosya:satır (kilit) | Kapı | Önce | Sonra | Eşzamanlı test · mutasyon |
|---|---|---|---|---|---|
| Cari tahsilat (+ vezne tahsilat dalı, sipariş kaporası) | Cari/CariModulu.cs:1122 | MASAK + tevsik + taksit | kilit öncesi | **kilit sonrası** | ME01 · ME02 · ME09 · M2 M17 M18 |
| Cari veresiye | CariModulu.cs:1012 | MASAK | kilit öncesi | **kilit sonrası** | ME03 · M3 |
| Döviz veresiye | CariModulu.cs (DovizVeresiyeGir) | MASAK | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Döviz tahsilat | CariModulu.cs (DovizTahsilatGir) | MASAK + tevsik + taksit | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Satış (tek kalem) | Satis/SatisModulu.cs:801 | MASAK + tevsik | kilit öncesi | **kilit sonrası** | ME04 · ME13 · M4 |
| Alış (tek kalem; Satınalma mal kabulü bunu dış işlemle çağırır) | SatisModulu.cs:922 | MASAK + tevsik | kilit öncesi | **kilit sonrası** | ME06 · M5 |
| Alış fişi | SatisModulu.cs (AlisFisi) | MASAK + tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Hurda alış | SatisModulu.cs:1242 | MASAK + tevsik | kilit öncesi | **kilit sonrası** | ME07 · M6 |
| Hurda satış | SatisModulu.cs (HurdaSatis) | MASAK + tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Satış fişi (+ vezne kalemli fiş, siparişi satışa çevir) | SatisModulu.cs:1761 | MASAK + tevsik | kilit öncesi | **kilit sonrası** | ME05 · M7 |
| Satış iadesi / Alış iadesi | SatisModulu.cs (SatisIade, AlisIade) — sıra: kaynak hareket kilidi → **cari kilidi** | MASAK + tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9; tutar kaynak satışın günü içinde kümülatife zaten girdiğinden eşik altı senaryo kurulamadı) |
| Takas | SatisModulu.cs (Takas) | MASAK + tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| İşçilik (+ vezne işçilik dalı) | SatisModulu.cs:2584 | MASAK + tevsik | kilit öncesi | **kilit sonrası** | ME08 · M8 |
| Avans al | Satis/SatisModulu.P21.cs:517 | tevsik | kilit öncesi | **kilit sonrası** | ME12 · M12 |
| Avans iade | SatisModulu.P21.cs (AvansIade) | tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Değişim (nakit fark) | SatisModulu.P21.cs (Degisim) | tevsik | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Siparişi satışa çevir | SatisModulu.P21.cs (dış işlem RC) → SatisFisi | MASAK + tevsik | — | **kilit sonrası (devralınan RC işlem)** | KE08 (komşu) yeşil |
| Sipariş kaporası | Siparis/SiparisModulu.cs:912 (dış işlem RC) → TahsilatGirHareket | MASAK + tevsik | sipariş kilidi var, cari kilidi yok | **sipariş → cari kilidi** | ME09 · M9 · ME19 · M16 |
| Banka dekontu (müşterili) | Banka/BankaModulu.cs:469 (sıra: cari → hesap) | MASAK | kilit öncesi | **kilit sonrası** | ME10 · M10 |
| Dekontu müşteriyle sonradan eşleştir | BankaModulu.cs:836 | MASAK | **işlemsiz** kontrol + ayrı SaveChanges | **tek işlem + cari kilidi** | ME11 · M11 |
| Evrak (çek/senet) nakit tahsili | Evrak/EvrakModulu.cs (TahsilEdildi) | tevsik + taksit | kilit öncesi | **kilit sonrası** | eşzamanlı test YOK (§ 9) |
| Emanet giriş/çıkış | Emanet/EmanetModulu.cs | — kümülatif yasal kapı YOK (EMANET MASAK'a tabi tür değil) | — | değişiklik gerekmedi | — |
| Gider · Virman · Kasa hareketi | Gider/*, Kasa/KasaModulu.cs | — yalnız kasa bakiye kapısı (`NakitDogrula`), yasal kümülatif değil | — | değişiklik gerekmedi | — |
| Vezne tahsil | Vezne/VezneModulu.cs | dış işlem açmaz → servisler kendi RC işlemini açar | — | değişiklik gerekmedi | — |
| Ekstre toplu dekont | Banka/EkstreModulu.cs | dış işlem yok → HareketEkleIc | — | değişiklik gerekmedi | — |
| Satınalma mal kabulü | Satinalma/SatinalmaModulu.cs:492 | dış işlem **REPEATABLE READ** → Alis devralır | — | tazelik denetimi (fail-closed) | ME16/ME17 (aynı mekanizma) · § 9 |

## 3. Sapmalar ve gerekçeleri

1. **Faz 1 korundu, kilit altında YİNELENDİ** (kontrol işlemin içine taşınmadı). Gerekçe: `YaptirimServisi.KimlikTara` TAM eşleşmede eşleşme kaydını + KRİTİK bildirimi yazıp 400 atar; kontrol işlem içine taşınsaydı bu kayıt reddedilen işlemle geri alınırdı (6415 md.10 bildirim izi kaybı). Bedeli: kümülatif sorgular iki kez koşar (ölçülen ek gecikme ihmal edilebilir, tek cari satırı kilidi).
2. **READ COMMITTED** (yalnız bu yolların açtığı işlemler): REPEATABLE READ'de anlık görüntü işlemin ilk tutarlı okumasında kurulur; dış işlem (kapora, satışa çevir) kilitten önce okuma yaptığından toplam eski kalırdı. MySQL `binlog_format=ROW` (ölçüldü) → RC güvenli. Genel `IsBaslatVeyaDevral` DEĞİŞTİRİLMEDİ (Sayım/Ürün yolları etkilenmesin).
3. **Satınalma dış işlemi REPEATABLE READ** ve dosya EKSIK-B1-SATINALMA şeridinin alanı → dokunulmadı; yerine tazelik denetimi (fail-closed) — eski anlık görüntüde kümülatif okunmaz, işlem yazılmaz (ME16). Kalıcı çözüm § 9.
4. **Evrak modülü** görev metninin dosya alanı listesinde yoktu; görev "SINIFI KAPAT … TÜM yollar" diyor ve evrak nakit tahsili tevsik kümülatifine yazan bir yol. Yalnız çağrı sırası (işlem + kilit + 2 satır yineleme) değişti; başka şeridin alanı değil.
5. **Sözlük:** yeni kullanıcı iletisi YOK (tazelik ihlali `InvalidOperationException` — programcı/yapılandırma hatası, ekrana sözlük metni gerekmez); `SUNUCU_CIPLAK_TURKCE_ILETI` 0.
6. Test eşiği teste gömülmedi: servislerin varsayılan motorundan (`KimlikIstenenEnDusukTutar` = 200.000, amaç beyanı kademesi dahil) ve `TevsikEsikleri().Had` (30.000) okunur; toplam kapının kendi ölçüsüyle (`GunlukIslemToplam` / `GunlukNakitToplam`) doğrulanır.

## 4. DoD sözleşmesi

Yeni varlık/kayıt türü YOK → yeni PATCH/DELETE/pasife alma ucu gerekmez. Yazılan kayıtlar aynı: hareketler/kasa_hareketleri/banka_hareketleri (finansal, düzeltme yalnız storno — 48-A), masak kimlik tespiti (mevcut uç), yaptırım eşleşmesi (mevcut Uyum ekranı). Ekrana dokunulmadı (yeni form/düğme yok) → gerçek tarayıcı ölçümü bu paket için gerekmez; reddin ekranda gösterimi mevcut MASAK/tevsik bant akışıyla aynı ileti (değişmedi).

## 5. Testler

Yeni: `Kuyumcu.Tests/MasakEszamanTestleri.cs` — **19 test** (13 eşzamanlı yol testi ME01–ME13 [10 paralel istek, ayrı DbContext/MySQL bağlantısı, başlangıç kapısı], ME14 RC + kilit NOWAIT, ME15 işlemsiz yineleme fırlatır, ME16 eski anlık görüntü yazmaz, ME17 taze dış işlem yazar, ME18 eşik altı 10 paralel hepsi kabul, ME19 işlem içinde TAM yaptırım eşleşmesi kaydı kalıcı).

| Kapsam | Sonuç |
|---|---|
| `MasakEszamanTestleri` (son kod, 3 ayrı koşum) | **19/19** ×2 (+18/18 ilk sürüm ×2) |
| Komşu: Masak · MASAK · Tevsik · Kapora · Banka · Evrak · Satinalma · Vezne · Yaptirim · Pesin · Yasal3 · Hurda · Takas · Avans · Degisim · Iade | **1762/1762** (2 atlanan) |
| **Tam süit** (trx `/private/tmp/kuyumcu-serit/trx-pMASAK-ESZAMAN-2/_Ramazan-MacBook-Air_2026-10-09_15_11_41.trx`) | **11827 toplam · 11824 geçti · 0 başarısız · 3 atlandı** |

Not (Anayasa md.1): ilk tam süit koşumu (`trx-pMASAK-ESZAMAN`) **geçersiz** sayıldı: 1 kırmızı (`ME02`). Kök neden ölçüldü — mutasyon sürücüsü yedeği `copy2` ile geri koyduğu için dosya zaman damgası eski kaldı, MSBuild son mutasyonu (M18: tahsilatta tevsik yinelemesi silinmiş) içeren ikiliyi "güncel" sanıp yeniden derlemedi; canlı API de o ikiliyle açılmıştı (canlı log'da kilit sonrası nakit toplam sorgusu YOK). Kaynak dosyalar `cmp` ile birebir aynıydı. `touch` + temiz derleme sonrası ME02 yeşil, tam süit yeniden koşuldu (yukarıdaki satır), canlı yeniden ölçüldü. Bu olay aynı zamanda hatanın canlıda yeniden üretimidir: yineleme yokken 10 × 5.000 NAKİT → **10 × 200 (50.000 TL, had 30.000 aşıldı)**.

## 6. Mutasyon kanıtı (sürücü `/private/tmp/kuyumcu-serit/MASAK-ESZAMAN/mut.py`; yedek `/private/tmp/kuyumcu-serit/mut-yedek/MASAK-ESZAMAN/`; filtre `MasakEszamanTestleri`)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1 | `CariKilitle`: cari satırı kilidi silindi (merkez) | 14 test: ME01–ME13 + ME16 | AYNI |
| M2 | Tahsilat: `CariIcinBaslat` → `BaslatVeyaDevral` (kilitsiz) | ME01 · ME02 · ME09 · ME16 | AYNI |
| M3 | Veresiye: kilit kaldırıldı | ME03 | AYNI |
| M4 | Satış: kilit kaldırıldı | ME04 · ME13 | AYNI |
| M5 | Alış: kilit kaldırıldı | ME06 | AYNI |
| M6 | Hurda alış: kilit kaldırıldı | ME07 | AYNI |
| M7 | Satış fişi: kilit kaldırıldı | ME05 | AYNI |
| M8 | İşçilik: kilit kaldırıldı | ME08 | AYNI |
| M9 | Kapora dış işlemi RC → `IsBaslatVeyaDevral` (REPEATABLE READ) | ME09 | AYNI |
| M10 | Banka dekontu: kilit kaldırıldı | ME10 | AYNI |
| M11 | Dekont eşleştir: kilit kaldırıldı | ME11 | AYNI |
| M12 | Avans al: kilit kaldırıldı | ME12 | AYNI |
| M13 | `BaslatVeyaDevral` RC → RepeatableRead | ME09 · ME14 | AYNI |
| M14 | Tazelik denetimi `if (false)` | ME16 | AYNI |
| M15 | `KilitAltindaYinele` işlem-yok denetimi `if (false)` | ME15 | AYNI |
| M16 | `KimlikTara` işlem içi geri alma silindi | ME19 | AYNI |
| M17 | Tahsilat: MASAK kilit altı yineleme satırı silindi | ME01 · ME09 | AYNI |
| M18 | Tahsilat: tevsik kilit altı yineleme silindi | ME02 | AYNI |

**18/18 kırmızı.** Ayrıca 7 kaynak dosya iş sonunda mutasyon öncesi kopyayla `cmp` → hepsi AYNI. Son temiz derleme `dotnet build Kuyumcu.sln`: **0 Hata**; Api'de **0 uyarı**; tam yeniden derlemede Kuyumcu.Tests'te 6 uyarı (SUBEKAPSAMASinirTestleri, KesifP2Testleri, SonKalanlarTestleri, Yildiz2Testleri — bu paketin dosyaları değil, KAPORA-ESZAMAN raporunda da kayıtlı).

## 7. Denetim (`bash denetim.sh`)

Yalnız **✗ P-30 DEMO_INSAN_SURESI** (görevde hariç). Diğer: `ExecuteUpdate/Delete 5 ✓` · `ModalPencere 48 (tavan 55) ✓` · `Sınırsız ToListAsync 140 (tavan 140) ✓` · `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 0 ✓`. Yeni `ExecuteSqlRaw` kullanımı yok (mevcut `Kilitle` yeniden kullanıldı; tazelik denetimi ADO komutu, tablo adları sabit, değerler parametre).

## 8. Canlı doğrulama (5166, kendi PID'im; kiracı `PAKETMASAK-ESZAMAN-TEST` `01M4G8TJ4HSPEZCWJG3HW8DV41`; sistem anahtarı rastgele üretilip yalnız kendi sürecime ortam değişkeniyle verildi, dosya iş sonunda boşaltıldı)

Canlı eşik tablodan: kimlik isteyen en düşük tutar 200.000 TL (amaç beyanı kademesi; md.5/1-b kimlik eşiği 370.000 TL 07.10.2026'dan) · tevsik haddi 30.000 TL.

| Adım | Beklenen | Gerçek |
|---|---|---|
| A: yeni cariye **10 eşzamanlı** `POST /cari/{id}/tahsilat` 40.000 HAVALE, beyansız | 4×200, 6×400 MASAK | **{200: 4, 400: 6}** — "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · …" |
| B: yeni cariye **10 eşzamanlı** 5.000 NAKİT tahsilat | 6×200, 4×400 nakit sınırı | **{200: 6, 400: 4}** — "Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor (önceki 30.000,00 TL …)" |
| C: aynı cariye 10 FARKLI siparişte **eşzamanlı** 40.000 HAVALE kapora | 4×200, 6×400 MASAK | **{200: 4, 400: 6}** |
| (yeniden üretim) eski ikiliyle B | — | {200: 10} — 50.000 TL nakit (§ 5 notu) |
| Sıralı kontrol: 8 × 5.000 NAKİT | 6×200, 2×400 | 6×200, 2×400 |

SQL (kuyumcu_net, kiracı):
```
ad              kabul  gunluk_toplam  nakit     siparise_bagli
Canli Havale A  4      160000.00      0.00      0
Canli Nakit B   6      30000.00       30000.00  0
Canli Kapora C  4      160000.00      0.00      4
masak_kimlik_tespitleri: 0   (beyansız hiçbir işlem eşiği aşmadan yazılmadı)
```

## 9. Açık kalanlar

1. **Satınalma mal kabulü dış işlemi REPEATABLE READ** (`SatinalmaModulu.cs:492`, EKSIK-B1-SATINALMA şeridi): bugün tazelik denetimiyle fail-closed (eşzamanlı aynı tedarikçi alışında nadiren `InvalidOperationException` → 500, hiçbir şey yazılmaz). Kalıcı düzeltme tek satır: `BeginTransactionAsync()` → `db.Database.BaslatVeyaDevral()` (RC). Sahibi: Satınalma şeridi.
2. **Eşzamanlı testi olmayan yollar** (kod aynı kalıp, kilit + yineleme eklendi, mutasyonla kanıtlanmadı): döviz veresiye/tahsilat, alış fişi, hurda satış, satış/alış iadesi, takas, avans iade, değişim, evrak nakit tahsili. Küçük ek test paketi önerisi (Aşkın).
3. **Hurda alışta pusula numarası yarışı** (bu paketin konusu değil, ölçüldü): nihai tüketici satıcıdan eşzamanlı 10 hurda alışında 9 istek `DbUpdateException` (pusula no `MAX+1` işlem dışında alınıyor → tekil indeks çakışması) — veri bozulmaz ama 500 döner. Sahibi: Hurda/gider pusulası.
4. **Peşin Müşteri pencere sorusu** (`PesinPencereKapisi`) kilit altında yinelenmedi (kümülatif değil, tezgâhtar-pencere sorusu; yinelemesi denetim kaydını çiftlerdi). Peşin kartı satırı kilitlenir; tek işlem eşik kuralı fail-closed aynen.
5. **Dekont eşleştirmede dekont satırı kilitlenmiyor** (aynı dekont iki farklı müşteriye eşzamanlı eşleştirme) — KAPORA-ESZAMAN sınıfı ("kontrol et → yaz"), bu paketin MASAK kapsamı dışında; kuyruk.
6. KAPORA-ESZAMAN'dan devreden: emanet giriş stornosu × emanet çıkışı, `storno_kaynak_id` tekil indeksi, bakiye kapılarının kilit öncesi kararı, 1213 → 409 eşlemesi.

## 10. Temizlik

API (PID 58296/58304 ve yeniden başlatılan 72349/72353) `kill`; `lsof` 5166 boş. Kendi kiracım `zsh kiraci-temizle.sh 01M4G8TJ4HSPEZCWJG3HW8DV41 --uygula` (666 satır + tenants 1) → ikinci çağrı "Kiracı YOK"; kendi 1 davet satırım silindi. **KAPORA-ESZAMAN tasarım hakeminin 3 kiracısı** (`01M4FYG4ZNAP4SKM7J1QBVG185`, `01M4FYGC3Z3D13VRDCQ85YQFJY`, `01M4FYHJPJ2D062S3NYJNG26AD`, ad PAKETKAPORA-HAKEM-TEST) zsh kiraci-temizle.sh ile silindi, her biri "Kiracı YOK" ile doğrulandı. **tenants: başlangıç 25 → son 19.** Fark: −3 hakem kiracısı (görev gereği) ve −3 **başka şeritlerin** kendi test kiracıları (PAKETYILDIZ4B-TEST ×2, PAKET7B-HAKEM-TEST `01M4G6WB…` — bu paket dokunmadı; yalnız yukarıdaki 4 kimlikle temizleme çalıştırıldı). Benim açtığım kiracı: 1 açıldı, 1 silindi. QA-TEST kiracıları (4) dokunulmadı. Sır dosyaları (anahtar, fikstur.json) `printf '' >` ile boşaltıldı. Proje ağacına geçici dosya bırakılmadı (`Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` bu paketin değil).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kümülatif MASAK + tevsik cari satırı kilitlendikten SONRA, yazmadan ÖNCE; ikinci istek bekler ve güncel toplamı görür | ✓ | § 2 · ME01–ME13 · canlı A/B/C |
| Eşik değeri DEĞİŞMEZ, tek kaynak | ✓ | eşik/kural dosyalarına dokunulmadı; yineleme aynı `Dogrula`/`Degerlendir` |
| KAPORA-ESZAMAN yardımcıları kullanıldı | ✓ | `Kilitle(KilitTablosu.Cariler)`, `BakiyeKilidi` sırası korunur |
| Sınıf taraması tablosu (dosya:satır, önce/sonra, düzeltildi mi) | ✓ | § 2 |
| Her yol için 10 paralel test | KISMİ | 13 yol testli (ME01–ME13); 10 yolun eşzamanlı testi yok → § 9 md.2 |
| Canlı 5166 paralel POST + SQL | ✓ | § 8 (5166 kullanıldı; görev metnindeki 5198 yerine şerit izolasyonu 5166) |
| Mutasyon ≥ 5, her yol için kilit kaldırma, cmp AYNI | ✓ | 18/18 (§ 6) |
| build 0/0 | ✓ | Api 0 uyarı 0 hata; Tests'teki 6 uyarı başka dosyaların |
| denetim TEMİZ (P-30 hariç) | ✓ | § 7 |
| Tam süit 0 yeni kırmızı | ✓ | 11827 / 0 başarısız (§ 5) |
| Temizlik: 3 hakem kiracısı + kendi kiracım | ✓ | § 10 |
| Ekran ölçümü | gerekmedi | ekrana dokunulmadı (§ 4) |

## 12. Kurucu / mali müşavir

Yeni mevzuat hükmü yazılmadı; eşikler (kimlik 370.000 / amaç beyanı 200.000 TEYİTSİZ / tevsik 30.000) değişmedi. Bilgi: eşzamanlı isteklerle "aynı gün aynı kişi" toplamının bölünmesi artık mümkün değil; eşik aşan istek beyansız reddedilir (kimlik formu istenir).

## Değişen dosyalar (bu paketin dokunduğu)

- `dotnet/Kuyumcu.Api/Common/SatirKilidi.cs` (YasalKilit eklendi)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (KilitAltindaYinele; KimlikTara işlem içi geri alma)
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` (veresiye, tahsilat, döviz veresiye, döviz tahsilat)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (satış, alış, alış fişi, hurda alış, hurda satış, satış fişi, satış/alış iadesi, takas, işçilik)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` (avans al, avans iade, değişim, satışa çevir RC)
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` (kapora dış işlemi RC)
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` (dekont, dekont eşleştir)
- `dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs` (nakit tahsil — § 3 md.4)
- `dotnet/Kuyumcu.Tests/MasakEszamanTestleri.cs` (yeni)

**Dokunulan sayfalar:** yok.
