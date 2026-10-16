# PAKET MASAK-ESZAMAN-2 — Uygulama raporu

**Tarih:** 09.10.2026 · **Rol:** Taşkın (backend) · Ertuğ (ön yüz — bu pakette ekrana dokunulmadı) · **Şerit:** t5 (API 5166 · KUYUMCU_TEST_DB=kuyumcu_test_t5)
**Kaynak:** PAKET-MASAK-ESZAMAN-uygulama-raporu.md § 9 (uygulayıcı YARIM: 10 yolun eşzamanlı testi + mutasyon kanıtı yok; hasım MUT3 — takas yinelemesi silinince hiçbir test kırmızı olmadı).
**COMMIT YOK. Migration: gerekmedi** (yeni tablo/kolon yok). **Eşik ve kural DEĞİŞMEDİ** (YasalParametreServisi / MasakKuralMotoru / TevsikKurali / PesinPencereKapisi kuralı aynen; yalnız kilit, okuma zamanı, işlem seviyesi ve test).

## 1. Yapılanlar (5 madde)

| # | Ne | Nasıl | Nerede |
|---|---|---|---|
| 1 | 10 yol + siparişi satışa çevir için eşzamanlı test | `MasakEszaman2Testleri.cs` ME20–ME30: aynı cariye 10 eşzamanlı istek (ayrı DbContext = ayrı MySQL bağlantısı, başlangıç kapısı); tek tek eşik altı, toplam eşik üstü, beyansız → eşiği aşanlar 400, kabul toplamı eşik altında ve +1 işlem eşiği aşardı | Kuyumcu.Tests/MasakEszaman2Testleri.cs |
| 2 | Her yol için kilit altı yineleme silme mutasyonu | 19 mutasyon, 19/19 kırmızı, geri alma ters-değiştirme + `cmp` AYNI + `touch` (§ 5) | sürücü `/private/tmp/kuyumcu-serit/MASAK-ESZAMAN-2/mut.py` |
| 3 | Peşin Müşteri pencere sorusu kilit altında yinelenir | `MasakKapisi.KilitAltindaYinele` Peşin dalında `PesinPencereKapisi.Denetle(..., kilitAltinda: true)` aynı argümanlarla yeniden çağrılır (kart satırı `CariIcinBaslat` ile zaten kilitli). `Denetle(kilitAltinda)`: Faz 1'in bekleyen (kaydedilmemiş) `PESIN_ART_ARDA_FARKLI_KISI` izi çıkarılır, güncel pencereyle yeniden yazılır → **çift iz yok**. `PencereOku(ust)`: kilit altında pencerenin üst sınırı "şimdi" (kilidi bırakan eşzamanlı isteğin satırı tarihi bu isteğin `an`'ından ms sonra olabilir, pencereden düşmesin; M33 bunu kanıtlar). | Modules/Masak/MasakKapisi.cs, Modules/Masak/PesinPencereKapisi.cs |
| 4 | Dekont eşleştirmede dekont satırı kilidi | `DekontEslestir`: işlem RC açılır → **dekont satırı** `Kilitle(KilitTablosu.BankaHareketleri)` → "Bekliyor mu" ve "stornolandı mı" kilit ALTINDA yeniden okunur → sonra cari kilidi (sıra: kaynak satır → cari, mevcut kilit sırası kuralı) → MASAK. Aynı ileti metinleri iki yardımcıya taşındı (`BekliyorDegilseReddet`, `StornoluysaReddet`), metin değişmedi. | Modules/Banka/BankaModulu.cs |
| 5 | Satınalma mal kabulü dış işlemi READ COMMITTED | `BeginTransactionAsync()` → `BeginTransactionAsync(IsolationLevel.ReadCommitted)` (tek satır + yorum). `Alis` işlemi devralır; kilit sonrası kümülatif son commit'i görür → tazelik ihlali (InvalidOperationException → 500) yerine doğru karar. Tazelik denetimi (MASAK-ESZAMAN) savunma olarak yerinde kaldı. | Modules/Satinalma/SatinalmaModulu.cs:496 |

## 2. Spesifikasyon ↔ kod doğrulaması ve sapmalar

1. **Satır numaraları kaymış**; kalıp aynı (Faz 1 → `CariIcinBaslat` → yineleme). Güncel satırlar § 5 tablosunda.
2. **İadelerde MASAK yarışı yapısal olarak YOK** (ölçüldü, kod okundu): `SATIS_IADE`/`ALIS_IADE` `MasakKapisi.KapiyaTabiTurler` listesinde değil → iade, MASAK günlük toplamını BÜYÜTMEZ; seri de koşsa paralel de koşsa MASAK kararı aynı. Nakit iade ise `nakit_tutar` doldurur ve tevsik toplamına girer → yarış TEVSİK tarafında. ME24/ME25 bu yüzden tevsik haddiyle kuruldu (önceki raporun "eşik altı senaryo kurulamadı" notunun kök nedeni bu). Mutasyon: iade yolunun kilit altı tevsik yinelemesi silindi → kırmızı (M24/M25).
3. **Avans iade, değişim, evrak** yalnız tevsik kapısı taşır (MASAK yok) → testler tevsik; mutasyon tevsik yinelemesi silme.
4. **Döviz tahsilat ve hurda satış**: tek işlem tutarı küçük tutulmak zorunda (döviz efektifi nakittir → tevsik haddi önce patlar; hurda satış stoka bağlı). MASAK eşiğine ulaşmak için cari önce "eşiğe 2,5 işlem kalacak kadar" HAVALE tahsilatıyla doldurulur (`Doldur`) — eşik sayısı yine teste gömülmez (servis motorundan okunur).
5. **Fiyat bandı (ISLEM-BANT şeridi, eşzamanlı)**: hurda/takas testleri ilk koşumda bant reddi aldı (2.800 TL/has, bant 3.413,60–7.509,92). Test has fiyatı fikstür kurunun yakınına (6.800) çekildi; ISLEM-BANT dosyalarına dokunulmadı.
6. **Peşin pencere yinelemesi önceki pakette bilinçli atlanmıştı** ("denetim kaydını çiftlerdi"): çift iz sorunu Faz 1'in bekleyen izini kilit altında yenileyerek çözüldü (ME32 + M32 kanıtı).
7. **Satınalma dosyası** bu dalgada EKSIK-B1-SATINALMA ile ortak; tek satırlık değişiklik `Edit` ile, dosyanın güncel hâli üzerinde yapıldı (başka satır değişmedi; 6 satır fark = 1 değişen satır + 4 yorum satırı).

## 3. DoD sözleşmesi

Yeni varlık/kayıt türü YOK → yeni PATCH/DELETE/pasife alma ucu gerekmez. Yazılan kayıtlar aynı: hareketler / kasa_hareketleri / banka_hareketleri (finansal; düzeltme yalnız storno — 48-A), `PESIN_ART_ARDA_FARKLI_KISI` denetim izi (mevcut, salt okunur iz), `BankaHareketiEslestirme` denetim izi (mevcut). Yeni kullanıcı iletisi yok (dekont 409 metni birebir aynı; `SUNUCU_CIPLAK_TURKCE_ILETI` 0). **Ekrana dokunulmadı** (razor/css/bileşen/sözlük değişmedi) → gerçek tarayıcı ölçümü bu paket için gerekmez. Dokunulan sayfalar: **yok**.

## 4. Testler

Yeni: `Kuyumcu.Tests/MasakEszaman2Testleri.cs` — **16 test**.

| Test | Yol | Kapı | Beklenen |
|---|---|---|---|
| ME20 | Döviz veresiye | MASAK (TL karşılığı) | 10 × 1.000 USD ×40 → kabul toplamı eşik altı |
| ME21 | Döviz tahsilat (dolgu) | MASAK | 10 × 100 USD → 2 kabul |
| ME22 | Alış fişi | MASAK | 10 × 40.000 HAVALE |
| ME23 | Hurda satış (dolgu) | MASAK | 10 × 1 has |
| ME24 | Satış iadesi (10 farklı kaynak) | tevsik | 10 × 5.000 NAKİT iade → 6 kabul |
| ME25 | Alış iadesi (10 farklı kaynak) | tevsik | 10 × 5.000 NAKİT iade |
| ME26 | Takas (hasım MUT3 yolu) | MASAK (max(eski,yeni)) | 10 × 40.000 |
| ME27 | Avans iade | tevsik | 10 × 5.000 |
| ME28 | Değişim nakit farkı (10 farklı kaynak) | tevsik | 10 × 5.000 fark |
| ME29 | Evrak nakit tahsili (10 farklı senet) | tevsik + taksit | 10 × 5.000 |
| ME30 | Siparişi satışa çevir (10 farklı sipariş) | MASAK | 10 × 40.000; TESLIM_EDILDI sayısı = kabul |
| ME31 | Peşin art arda fiş, beyansız | pencere sorusu | 1 kabul, 9 × 400 PESIN_ART_ARDA_SORU |
| ME32 | Peşin, önceki fişten sonra 10 × "başka kişi" beyanı | pencere izi | 10 kabul, tam 10 iz (çift yok) |
| ME33 | Aynı dekont 2 cariye 10 eşzamanlı | dekont kilidi | 1 × başarı, 9 × 409; denetim izi 1 |
| ME34 | Mal kabul, tedarikçi kilidi beklerken başkası eşiği doldurur | RC | MASAK 400 (InvalidOperationException YOK), irsaliye/alış yazılmaz |
| ME35 | Aynı, eşik altı | RC | mal kabul yazılır (500 yok) |

| Kapsam | Sonuç |
|---|---|
| `MasakEszaman2Testleri` (son kod; ilk koşum + 3 tekrar) | **16/16** ×4 |
| Komşu: MasakEszaman (eski 19 + yeni 16) · KaporaEszaman · Yasal3 · Banka · UrunAcilisAtomik · P26Tedarik · Pesin · Masak · Evrak · Iade · Takas · Doviz · P21 | **1346 toplam · 1344 geçti · 0 başarısız · 2 atlandı** |
| **Tam süit** | § 7 |

## 5. Mutasyon kanıtı (sürücü `/private/tmp/kuyumcu-serit/MASAK-ESZAMAN-2/mut.py`; yedek `/private/tmp/kuyumcu-serit/mut-yedek/MASAK-ESZAMAN-2/`; günlükler `mut-<kod>.log`)

Geri alma: başka şeridin eşzamanlı düzenlemesini ezmemek için yedek kopyalanmadı, mutasyon TERS değiştirildi; sonra `os.utime` (touch — MSBuild eski ikili tuzağı) ve yedekle `cmp`.

| Kod | Dosya:satır | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|---|
| M20 | Cari/CariModulu.cs:1503 | Döviz veresiye `KilitAltindaYinele` silindi | ME20 | AYNI |
| M21 | Cari/CariModulu.cs:1589 | Döviz tahsilat `KilitAltindaYinele` silindi | ME21 | AYNI |
| M22 | Satis/SatisModulu.cs:1030 | Alış fişi `KilitAltindaYinele` silindi | ME22 | AYNI |
| M23 | Satis/SatisModulu.cs:1428 | Hurda satış `KilitAltindaYinele` silindi | ME23 | AYNI |
| M24 | Satis/SatisModulu.cs:2061 | Satış iadesi kilit altı tevsik yinelemesi silindi | ME24 | AYNI |
| M25 | Satis/SatisModulu.cs:2192 | Alış iadesi kilit altı tevsik yinelemesi silindi | ME25 | AYNI |
| M26 | Satis/SatisModulu.cs:2490 | **Takas `KilitAltindaYinele` silindi (hasım MUT3)** | ME26 | AYNI |
| M27 | Satis/SatisModulu.P21.cs:662 | Avans iade kilit altı tevsik silindi | ME27 | AYNI |
| M28 | Satis/SatisModulu.P21.cs:777 | Değişim kilit altı tevsik silindi | ME28 | AYNI |
| M29 | Evrak/EvrakModulu.cs:546 | Evrak nakit tahsili kilit altı tevsik silindi | ME29 | AYNI |
| M30 | Satis/SatisModulu.P21.cs:975 | Satışa çevir dış işlemi RC → `IsBaslatVeyaDevral` (RR) | ME30 | AYNI |
| M30b | Satis/SatisModulu.cs:1783 | Satış fişi (satışa çevirin yazdığı yol) `KilitAltindaYinele` silindi | ME30 | AYNI |
| M31 | Masak/MasakKapisi.cs:298 | Peşin pencere kilit altı yinelemesi silindi | ME31 | AYNI |
| M32 | Masak/PesinPencereKapisi.cs:114 | Faz 1 bekleyen izinin atılması `if (false)` | ME32 | AYNI |
| M33 | Masak/PesinPencereKapisi.cs:76 | Kilit altı pencere üst sınırı "şimdi" → `an` | ME31 | AYNI |
| M34 | Banka/BankaModulu.cs:834 | Dekont satırı kilidi silindi | ME33 | AYNI |
| M35 | Banka/BankaModulu.cs:835 | Kilit altı "Bekliyor mu" yeniden okuması etkisiz | ME33 | AYNI |
| M36 | Satinalma/SatinalmaModulu.cs:496 | Mal kabul dış işlemi RC → varsayılan (RR) | ME34 · ME35 (`InvalidOperationException: MASAK-ESZAMAN: dış işlemin anlık görüntüsü eski`) | AYNI |

**19/19 kırmızı, 19/19 cmp AYNI.** Not (Anayasa md.1): M22 ve M36'nın ilk koşumu kırmızı test vermeden rc=1 döndü — kök neden ölçüldü: aynı ağaçta başka şeridin eşzamanlı derlemesi (`ConvertDllsToWebCil` dosya kilidi; `obj/.../apphost` bulunamadı, MSB3030) → test HİÇ koşmadı. Sürücüye "test sayısı yoksa 20 sn bekle, en çok 4 kez yeniden dene" eklendi, ikisi yeniden koşuldu: M22 → ME22 kırmızı, M36 → ME34+ME35 kırmızı. Son mutasyondan sonra `touch` + derleme: Api **0 uyarı 0 hata**; Tests 0 hata (ilk sln derlemesinde YILDIZ-4C şeridinin yarım `Yildiz4CTestleri.cs` dosyası 6 hata verdi — o şerit düzeltince Tests 0 hata derlendi; § 7).

## 6. Denetim (`bash denetim.sh`)

`DENETİM BAŞARISIZ` — 4 ✗, **hiçbiri bu paketin değil** (sahipleriyle):
- ✗ P-30 DEMO_INSAN_SURESI — görevde hariç.
- ✗ P-23 FIS_TOPLAM_ODEME_KALEMI_FARKI (canlı DB): fiş `01M4GDP08HXKVSRTJHQ9KAD8RG`, kiracı **PAKETYILDIZ4C-TEST**, SATIS, 13:29:45 (fiş 6.920,16 ≠ kalem 18.013,17) — YILDIZ-4C şeridinin test verisi (sorgu ile ölçüldü).
- ✗ TOPLU-1 TARIH_TEK — `Bilesenler/Yildiz/YildizBugunVerisi.cs:141` (YILDIZ-4C şeridi).
- ✗ GIRDI-2 TEST_GECERSIZ_FIKSTUR — `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (önceki MASAK-ESZAMAN paketinin **hasım doğrulayıcı** dosyası; geçersiz IBAN `TR2000…0002`). Bu paketin alanı dışında (DOSYA ALANI: MasakEszaman*.cs); dokunulmadı.

Bu paketin sayaçları: `ExecuteUpdate/Delete 5 ✓` · `ModalPencere 48 (tavan 55) ✓` · `Sınırsız ToListAsync 140 (tavan 140) ✓` · `SUNUCU_CIPLAK_TURKCE_ILETI 0 ✓`. Yeni `ExecuteSqlRaw` yok (mevcut `Kilitle` kapalı tablo listesi — `BankaHareketleri` zaten listede).

## 7. Tam süit

`KUYUMCU_TEST_DB=kuyumcu_test_t5 dotnet test … --logger trx` (son kod, derlemeli) — trx `/private/tmp/kuyumcu-serit/trx-pMASAK-ESZAMAN-2b/_Ramazan-MacBook-Air_2026-10-09_16_58_29.trx`:
**11942 toplam · 11931 geçti · 8 başarısız · 3 atlandı.** Bu paketin testleri: MasakEszaman2Testleri 16/16, MasakEszamanTestleri 19/19 yeşil.

8 kırmızının HİÇBİRİ bu paketin dokunduğu dosyalardan geçmiyor (sahipleriyle):

| Kırmızı | Neden (ölçüldü) | Sahibi |
|---|---|---|
| Toplu1SafTestleri.TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok | `Bilesenler/Yildiz/YildizBugunVerisi.cs:141` ham tarih biçimi (denetim TOPLU-1 ile aynı) | YILDIZ-4C (t1) |
| IslemBantServisTestleri.IB09 (MUHASEBE) · IB09 (TEZGAHTAR) | `Assert.Throws() … ForbiddenException` atılmadı | ISLEM-BANT (t2) |
| IslemBantHttpTestleri.IB13 · IB16 · IB17 · IB18 | rol/onay 403 beklentisi (`Values differ`) | ISLEM-BANT (t2) |
| Dogrulayici.ISLEMBANTSinirServisTestleri.IBS_TEZGAHTAR_onaysiz_bant_disi_400_onayli_403 | aynı rol kapısı | ISLEM-BANT (t2) hasımı |

Kanıt: rol/bant kararını veren `Kuyumcu.Api/Modules/Satis/IslemFiyatKapisi.cs` **16:59:07'de** (tam süit 16:58'de başladıktan sonra) başka şerit tarafından değiştirildi; bu paket o dosyaya ve IslemBant test dosyalarına dokunmadı. Bu paketin değiştirdiği 4 kaynak dosya (MasakKapisi, PesinPencereKapisi, BankaModulu, SatinalmaModulu) rol/bant kapısını çağırmaz. → **Bu paketin ürettiği yeni kırmızı: 0** (madde 7). Derleme: Api 0 uyarı 0 hata; Tests 0 hata, 7 uyarı (IslemBantTestleri.cs:639, KesifP2Testleri.cs:455, SUBEKAPSAMASinirTestleri.cs:49, SonKalanlarTestleri.cs:593/824, Yildiz2Testleri.cs:193/232 — hiçbiri bu paketin dosyası değil).

## 8. Canlı doğrulama (5166, kendi PID'im 91859/91874; kiracı `PAKETMASAK-ESZAMAN-2-TEST` `01M4GEXSB9Q6N7YFS2V1TG42KC`; sistem anahtarı rastgele üretildi, yalnız kendi sürecime ortam değişkeniyle verildi, dosya iş sonunda boşaltıldı)

Canlı kur: `usdTry` 49,30 → döviz tutarı 811,36 USD ≈ 40.000,02 TL.

| Adım | Beklenen | Gerçek |
|---|---|---|
| A: yeni cariye **10 eşzamanlı** `POST /cari/{id}/doviz-veresiye` 811,36 USD, beyansız | 4×200, 6×400 MASAK | **{200: 4, 400: 6}** — "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · …" |
| C (ilk deneme): 100.000 HAVALE avans → **10 eşzamanlı** `POST /islemler/avans/iade` 5.000 | — | {400: 10} "kasada nakit yetersiz: çekmecede 0,00 ₺" — **fikstür eksiği** (yeni kiracının kasasında nakit yok; havale nakit verilemez). Kapı doğru çalıştı; senaryo kurulumu yetersizdi. |
| C2: kasaya iki AYRI müşteriden 25.000'er NAKİT tahsilat (fikstür) → yeni cariye 100.000 HAVALE avans → **10 eşzamanlı** 5.000 nakit avans iadesi | 6×200, 4×400 tevsik | **{200: 6, 400: 4}** — "Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor (önceki 30.000,00 TL …) 30.000,00 TL nakit sınırını … aşıyor" |
| D: müşterisiz 1.000 TL dekont → **10 eşzamanlı** `POST /bankalar/{id}/hareketler/{dekont}/eslestir`, iki farklı cari dönüşümlü | 1×200, 9×409 | **{200: 1, 409: 9}** — "Yalnız 'Bekliyor' durumundaki dekont eşleştirilir (şu an: ESLESTI) …" |

SQL (kuyumcu_net, kiracı):
```
ad                      tur             adet  tl         doviz    nakit
Canli Avans Iade C      AVANS_ALINDI    1     100000.00  NULL     0.00
Canli Avans Iade C2     AVANS_ALINDI    1     100000.00  NULL     0.00
Canli Avans Iade C2     AVANS_IADE      6     30000.00   NULL     30000.00
Canli Doviz Veresiye A  DOVIZ_VERESIYE  4     160000.20  3245.44  0.00
Canli Kasa Nakdi X      TAHSILAT        1     25000.00   NULL     25000.00
Canli Kasa Nakdi Y      TAHSILAT        1     25000.00   NULL     25000.00
dekont 01M4GEY3NTES62M9BKRJVV8X1Y  durum ESLESTI  cari "Canli Dekont D2"
denetim_kayitlari (BankaHareketiEslestirme, bu dekont): 1
masak_kimlik_tespitleri: 0   (beyansız hiçbir işlem eşiği aşarak yazılmadı)
```

## 9. Temizlik

API `kill 91874 91859`; `lsof -iTCP:5166 -sTCP:LISTEN` boş. Kiracı `zsh kiraci-temizle.sh 01M4GEXSB9Q6N7YFS2V1TG42KC --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)". Kendi davet satırım (`kiraci_davetleri`, not `masak-eszaman-2`) silindi (1 satır). **tenants: başlangıç 21 → son 20.** Benim açtığım kiracı: 1 açıldı, 1 silindi; −1 fark başka bir şeridin kendi test kiracısını silmesinden (bu paket yalnız yukarıdaki kimlikle temizlik çalıştırdı). QA-TEST kiracılarına dokunulmadı. Sır dosyaları (`anahtar`, `fikstur.json`) `printf '' >` ile boşaltıldı. Proje ağacına geçici dosya bırakılmadı (mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/MASAK-ESZAMAN-2/`).

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| 11 yol için eşzamanlı testler yeşil (döviz veresiye, döviz tahsilat, alış fişi, hurda satış, satış iadesi, alış iadesi, takas, avans iade, değişim, evrak nakit tahsili + satışa çevir) | ✓ | ME20–ME30, 16/16 ×4 |
| 11+ mutasyon kırmızı, cmp AYNI (takas dahil) | ✓ | 19/19 (§ 5), M26 = hasım MUT3 |
| Peşin pencere kapısı kilit altında yinelenir | ✓ | MasakKapisi.KilitAltindaYinele · ME31/ME32 · M31/M32/M33 |
| Dekont eşleştirmede dekont satır kilidi; aynı dekont iki cariye → 1×200, 1×409 | ✓ | ME33 (1 başarı / 9 × 409) · M34/M35 · canlı D {200:1, 409:9} |
| Satınalma mal kabul RC, 500 yok | ✓ | SatinalmaModulu.cs:496 · ME34/ME35 · M36 |
| Canlı 5166 en az 3 yeni yol paralel POST + SQL | ✓ | döviz veresiye · avans iade · dekont eşleştirme (§ 8) |
| build 0/0 | ✓ (bu paket) | Api 0 uyarı 0 hata; Tests 0 hata, 7 uyarı başka şeritlerin dosyalarında (§ 7) |
| denetim TEMİZ (P-30 hariç) | ✗ (başkasının) | 3 ek ✗ bu paketin değil, sahipleriyle § 6 |
| tam süit 0 yeni kırmızı | ✓ | 11942 / 8 kırmızı — hepsi başka şeridin (§ 7); bu paketin 35 testi yeşil |
| Mevzuat eşiği/kural DEĞİŞMEZ | ✓ | eşik dosyalarına dokunulmadı; yinelemeler aynı `Dogrula`/`Denetle` |
| Ekran ölçümü | gerekmedi | ekrana dokunulmadı |

## 11. Açık kalanlar

1. **Avans iadede havuz (açık avans) kontrolü kilit öncesi** (`AvansIade`: `AcikAvans` + `MahsupEdilebilir` işlem dışı okunur, kilit altında yinelenmez) — eşzamanlı iadeler açık avansı aşabilir. Bakiye-kapısı sınıfı (KAPORA-ESZAMAN'dan devreden "bakiye kapılarının kilit öncesi kararı"); bu paketin kapsamı (yasal kümülatif) dışında. Sahibi: KAPORA-ESZAMAN devri / Taşkın kuyruğu.
2. **Değişim yolu yeni ürün için SATIS satırı yazar ama MASAK kapısı çağırmaz** (yalnız tevsik). Kural/mevzuat değişikliği gerektirir — bu pakette "kural DEĞİŞMEZ" gereği dokunulmadı; mali müşavir/Mehmet incelemesine (§ 12).
3. **Denetim ✗ (başkasının)**: P-23 ve TOPLU-1 → YILDIZ-4C şeridi; GIRDI-2 → `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (önceki paketin hasım dosyası, geçersiz IBAN fikstürü) — sahibi orkestratör/hasım.
4. Önceki raporun § 9 md.3 (hurda alışta pusula numarası yarışı) ve md.6 (KAPORA-ESZAMAN devri) aynen açık.

## 12. Kurucu / mali müşavir

Yeni mevzuat hükmü yazılmadı; eşikler (kimlik 370.000 / amaç beyanı 200.000 TEYİTSİZ / tevsik 30.000 / Peşin pencere 60 dk politika) değişmedi. Soru (mali müşavir): ürün DEĞİŞİMİNDE yeni ürünün satış bedeli MASAK bağlantılı işlem toplamına sayılmalı mı? Bugün yalnız nakit fark tevsik haddine bakılıyor (§ 11 md.2). Tahminle kural eklenmedi.

## Değişen dosyalar (bu paketin dokunduğu)

- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (Peşin pencere kilit altı yineleme)
- `dotnet/Kuyumcu.Api/Modules/Masak/PesinPencereKapisi.cs` (`kilitAltinda`, `ust`)
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` (DekontEslestir dekont kilidi + kilit altı yeniden okuma)
- `dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs` (mal kabul dış işlemi RC — tek satır)
- `dotnet/Kuyumcu.Tests/MasakEszaman2Testleri.cs` (yeni)

**Dokunulan sayfalar:** yok.
