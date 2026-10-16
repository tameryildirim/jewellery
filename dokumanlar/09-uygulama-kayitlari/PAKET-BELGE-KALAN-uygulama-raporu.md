# PAKET BELGE-KALAN — uygulama raporu (tur 2 · 01.10.2026 · şerit t1 · API 5160 · vekil 5161)

Rol: Taşkın (backend) ve Ertuğ (frontend). Hasımlı paket (tarih = yasal belge verisi). COMMIT YOK.
Bu belgenin ilk bölümü TUR 2'dir (hasım doğrulayıcının kalan bulguları). Tur 1 raporu aynen aşağıda, "TUR 1" başlığı altında duruyor.

## TUR 2 — Özet (geçerli kapı değerleri)
| Kapı | Sonuç |
|---|---|
| Build (son, temiz, `Kuyumcu.sln`) | **0 hata.** Tek uyarı `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`. Dosya başka şeridin (SUBEKAPSAMA doğrulayıcısı), bu paketle ilgisi yok |
| Kendi testlerim | BelgeKalan* 40/40 yeşil. İlgili geniş küme (BelgeKalan, Pusula, GiderPusula, KesifP3, Fatura*, P22, Arama, UX1C3, Q92, Bicim) **756/756** yeşil |
| Mutasyon (tur 2) | **14/14 kırmızı.** Hepsi geri alındı, `cmp` AYNI. Son mutasyondan sonra temiz build alındı |
| Tam süit (son kodla) | **8415 test · 8414 geçti · 0 başarısız · 1 atlandı** (trx: `/private/tmp/kuyumcu-serit/trx-pBELGE-KALAN-t2b`) |
| denetim.sh | rc=1. 302 ✓, tek ✗ `P-30 DEMO_INSAN_SURESI` (tarih tetikli demo provası; tur 1'de de vardı, bu paketle ilgisi yok). Bu paketin sayaçları ✓ |
| Başsız Chrome | 7 senaryo × 1440/1280/960/375 × BASİT/PRO × açık/koyu = **112/112 geçti** |
| Canlı API ve SQL | Tüm adımlar beklenen kodla döndü (tablo aşağıda) |
| Test kiracısı | `PAKETBELGE-KALAN-T2-TEST` silindi. tenant, fatura, hareket ve kullanıcı satırları 0 |

## TUR 2 — Kapatılan bulgular
| Bulgu (doğrulayıcı) | Ne yapıldı | Kanıt |
|---|---|---|
| **[KRİTİK] Gider pusulası detay çekmecesi:** iki birincil düğme, dolu kırmızı iptal, madde numarası düğme adında, KVKK ve açıklama paragrafı açıkta, gram 3 hane | **GiderPusulalari.razor** değişti:<br>• Yazdır `dugme-ikincil` oldu.<br>• İptal `dugme dugme-ikincil zo-ikincil-tehlike` oldu: ikincil zemin, tehlike renginde yazı. Tıklanınca ortak onay modalını (`#gp-iptal-onay`, gerekçe ≥ 20) açar; bu akış değişmedi.<br>• İptal düğmesinin adı iki modda da sade: "Pusulayı iptal et". PRO'da "(VUK md.234)" `title` ipucunda.<br>• Gram listeyle aynı, 2 hane.<br>• "Düzeltilebilen alanlar" açıklaması (`#gp-duzenlenebilir-aciklama`, kimlik korundu) ve PRO okuma izi notu (KVKK md.12, yeni kimlik `#gp-okuma-izi-notu`) kapalı `#gp-degismez` ipucunun içine taşındı.<br>• Açıklama metni kısaldı. Eski metin "aşağıda" diye ipucuna işaret ediyordu; artık ipucunun içinde.<br>• PRO düzeltme yolu metni düğmenin görünen adını anıyor.<br>**zo-belge.css** kural (15): `.zo-ikincil-tehlike`, yalnız belirteç kullanır | Çekmecede görünür birincil düğme yalnız `cekmece-gider-pusulasi-kaydet` (16/16).<br>Yazdır ikincil. İptalde `dugme-tehlike` yok. Yazı/zemin kontrastı açıkta 5,90, koyuda 9,16 (≥ 4,5).<br>Gram "50,00 g" = liste.<br>İpucu kapalı. Açıklama görünmez. KVKK görünür 0, "md." görünür 0.<br>PRO `title` "…(VUK md.234)", BASİT'te `title` yok.<br>Onay modalı açılıyor; Onayla gerekçesiz kilitli (16/16) |
| **[ORTA] Arama ucu 00:00–03:00 TSI belgesini bir gün geri basıyordu** (`AramaModulu.cs:289`) | `AramaModulu.cs` içindeki **beş** tarih basımı (fiş, hareket ×2, fatura, banka hareketi) `Zaman.UtcdenYerele(…)` üzerinden yapılıyor. Aynı kök, aynı dosya | Canlı: 30.09 22:30Z'de kesilen `FTR-2026-000001` için `GET /arama?q=…&tur=FATURA` ve `tur=GENEL` 200 döndü, `altBilgi` "**01.10.2026** · 1400.00 TRY" (eskisi 30.09.2026).<br>HTTP testi `BK2_Arama_fatura_gunu_isletme_gunudur`. Mutasyonda gerçek çıktı "30.09.2026 · 1200.00 TRY" ile kırmızı oldu |
| **[DÜŞÜK] İstemci günü tarayıcı saat dilimine bağlıydı** | **Bicim.cs**: yeni `Bicim.IsletmeZamani(DateTime)` (Europe/Istanbul; Kind Unspecified UTC sayılır; saat dilimi verisi yoksa tarayıcı yereline düşer). `Bicim.Tarih` bunu kullanıyor.<br>`Faturalar.BelgeGunu` = `Bicim.IsletmeZamani(t).Date`.<br>Liste, çekmece ve sunucu artık aynı kurala bağlı | Saf test `BK2_Bicim_isletme_gunu_Istanbul`. Kaynak sözleşme testi `BK2_istemci_gunu_isletme_dilimine_sabit` (test makinesi TSI'de; davranış testi `ToLocalTime`'ı tek başına ayırt edemez, bu yüzden kaynak ölçülür).<br>Tarayıcı: fatura-tarih-0130 liste 1.10.2026 = çekmece 2026-10-01; fatura-tarih-2330 30.09.2026 = 2026-09-30 (32/32) |
| **[DÜŞÜK] Kalem penceresinde para biçimi tutarsız** ("45.751,50 ₺" yanında "3.200 ₺") | Faturalar.razor kalem tablosunda bütün para hücreleri ve genel toplam `Bicim.TlKurus` (iki hane) kullanıyor: birim fiyat (salt okunur gösterim), işçilik, indirim, has değer, KDV matrahı, KDV, satır toplamı, `#fd-toplam` | Tarayıcı: kalem tablosunda iki haneli olmayan ₺ hücresi 0, toplam ",dd ₺" (32/32).<br>İlk ölçümde PRO'da "0 ₺" yakalandı (has değer, KDV matrahı). Düzeltildi ve yeniden ölçüldü.<br>bUnit `BK2_Fatura_kalem_para_hucreleri_iki_hane`: tablonun tüm ₺ hücreleri `,\d{2} ₺` |
| [DÜŞÜK] Tam süit ve denetim sayıları yeniden üretilemiyor | Bu turda son kodla yeniden koşuldu: 8415 / 0 başarısız. Denetimde tek ✗ P-30 | trx ve log `/private/tmp/kuyumcu-serit/BELGE-KALAN/tam-suit-t2b.log` |
| [DÜŞÜK] 5199 dinlemiyor | Bu paket 5199'a dokunmadı. Tur 2 sonunda da `lsof -iTCP:5199` boş | Orkestratör `qa-sunucu.sh` ile ele almalı |

## TUR 2 — Sapmalar ve gerekçeleri
- **Dosya alanı dışında üç dosya.**
  - `Kuyumcu.Api/Modules/Arama/AramaModulu.cs`: yalnız beş tarih basım satırı ve bir yorum. Doğrulayıcı "ayrı paket" önerdi. Görev ise "önce kalan bulguları kapat" diyor; eşzamanlı şeritlerin hiçbiri bu dosyanın sahibi değil (son değişiklik 30.09 01:47). Aynı kökün tek satırlık düzeltmesi olduğu için burada kapatıldı.
  - `Kuyumcu.Web/Servisler/Bicim.cs`: yeni `IsletmeZamani` ve `Tarih`'in tek satırı. TSI'deki kullanıcı için çıktı birebir aynı (Türkiye sabit +03); yalnız TSI dışındaki tarayıcıda gün değişir. 17 sayfa `Bicim.Tarih` kullanıyor; ilgili 756 test ve tam süit yeşil.
  - Testler: `Kuyumcu.Tests/PusulaEkranTestleri.cs` (bir satır: düzeltme yolu artık düğmenin görünen adını anıyor) ve `Kuyumcu.Tests/FaturaEkranTestleri.cs` (toplam ve satır biçimi beklentileri `Bicim.TlKurus`; 5 satır).
- **Fatura arama sonucu ekranda görünmüyor.** Fatura sonuçlarının `Rota` alanı null, `GlobalArama` ise yalnız rotası olan sonucu listeliyor. Bu yüzden arama düzeltmesinin kanıtı canlı API ve HTTP testi. Aynı dosyadaki fiş, hareket ve banka hareketi tarihleri `KayitSecici` (Faturalar "Fişten fatura", Uyum, POS tahsilatları) üzerinden ekrana çıkıyor. Bugünkü tohum verisi gün sınırında değil, bu yüzden tarayıcıda sınır durumu üretilemedi; HTTP testi ve kaynak kuralı aynı.
- **Tam süit iki kez koşuldu** (kural: tur başına bir). Birinci koşumdan sonra tarayıcı ölçümü PRO kalem tablosunda iki hücreyi ("0 ₺") yakaladı ve kod değişti. Raporlanan değer son kodla alınan ikinci koşumdur.
  - Birinci koşum: 8406 test · 4 başarısız, hiçbiri bu paketin değil: `SATIS2EkranTestleri.PRO_yasal_kapilar_uygunken_TEK_satir_asilinca_KART`, `KesifP5EkranTestleri.SATIS4a_…`, `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar`, `TS2TuvalProTestleri.Yasal_kapilar_sunucu_devrede_derse_Asildi`.
  - Bu dört test tek başına koşulunca 4/4 yeşil geçti ve ikinci koşumda da kırmızı değildi. Hepsi `WaitFor` zaman aşımıydı; aynı sırada t5 şeridi `Islemler.razor`'ı değiştiriyordu (03:56).
- **Ölçüm notu.** İlk koşuda arama senaryosunun açık bıraktığı palet, sonraki genişliklerde gider-pusulaları "örtülü" ölçümünü kirletti. Senaryo çıkarıldı ve tüm pusula ve liste senaryoları yeniden ölçüldü; tablo temiz koşudur.

## TUR 2 — Testler (yeni 7 yöntem, 9 durum; `Kuyumcu.Tests/BelgeKalanTestleri.cs`)
- `BelgeKalanAramaHttpTestleri.BK2_Arama_fatura_gunu_isletme_gunudur` (HTTP, gerçek boru hattı)
- `BelgeKalanTur2SafTestleri.BK2_Bicim_isletme_gunu_Istanbul`
- `BelgeKalanTur2SafTestleri.BK2_istemci_gunu_isletme_dilimine_sabit`
- `BelgeKalanTur2SafTestleri.BK2_css_ikincil_tehlike`
- `BelgeKalanPusulaEkranTestleri.BK2_Pusula_detay_tek_birincil_iptal_ikincil` (BASİT, PRO)
- `BelgeKalanPusulaEkranTestleri.BK2_Pusula_detay_gram_iki_hane_aciklama_kapali` (BASİT, PRO)
- `BelgeKalanEkranTestleri.BK2_Fatura_kalem_para_hucreleri_iki_hane`

Güncellenen: `BK_Fatura_cekmece_tarihi…` artık işletme gününü saat dilimine bağlı olmadan sınar; gün sınırı koşulu her makinede doğrulanır.

## TUR 2 — Mutasyon tablosu (kayıtlar `/private/tmp/kuyumcu-serit/BELGE-KALAN/mut2-*.log`, yedekler `/private/tmp/kuyumcu-serit/mut-yedek/BELGE-KALAN-t2/`)
| Kod | Kırılan kural | Kırmızı test | cmp |
|---|---|---|---|
| MBK2-1 | Arama fatura günü UTC (`{f.Tarih:dd.MM.yyyy}`) | BK2_Arama_fatura_gunu_isletme_gunudur | AYNI |
| MBK2-2 | `IsletmeZamani` → `utc.ToLocalTime()` | BK2_istemci_gunu_isletme_dilimine_sabit | AYNI |
| MBK2-3 | `BelgeGunu` → `ToLocalTime().Date` | BK2_istemci_gunu_isletme_dilimine_sabit | AYNI |
| MBK2-4 | Unspecified yerel sayılır | BK2_Bicim_isletme_gunu_Istanbul, BK_Fatura_cekmece_tarihi… | AYNI |
| MBK2-5 | Yazdır yeniden birincil | BK2_Pusula_detay_tek_birincil (BASİT, PRO) | AYNI |
| MBK2-6 | İptal yeniden `dugme-tehlike` | BK2_Pusula_detay_tek_birincil (BASİT, PRO) | AYNI |
| MBK2-7 | PRO `title` (madde no) kaldırılır | BK2_Pusula_detay_tek_birincil (PRO) | AYNI |
| MBK2-8 | Gram 3 hane | BK2_Pusula_detay_gram… (BASİT, PRO) | AYNI |
| MBK2-9 | Açıklama yeniden çekmece gövdesinde | BK2_Pusula_detay_gram… (BASİT, PRO) | AYNI |
| MBK2-10 | Okuma izi notu BASİT'te de | BK2_Pusula_detay_gram… (BASİT) | AYNI |
| MBK2-11 | İpucu açık (`open`) gelir | BK2_Pusula_detay_gram… (BASİT, PRO) | AYNI |
| MBK2-12 | Satır toplamı `Bicim.Tl` | BK2_Fatura_kalem…, FaturaEkranTestleri.KalemToplamiSunucuYanitindanOkunur | AYNI |
| MBK2-13 | CSS `.zo-ikincil-tehlike` rengi kaldırılır | BK2_css_ikincil_tehlike | AYNI |
| MBK2-14 | Has değer `Bicim.Tl` | BK2_Fatura_kalem_para_hucreleri_iki_hane ("0 ₺") | AYNI |

## TUR 2 — Canlı doğrulama (API 5160, kiracı `PAKETBELGE-KALAN-T2-TEST Kuyumculuk` 01M3TFG5T8N2AD1TWG8PCBF10E, `demo-tohumla.js --prova-kiraci`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris | 200 | 200 |
| POST /faturalar (Tarih 2026-09-30T22:30:00Z) + PATCH /faturalar/{id}/durum KESILDI | 200, numara | 200 · FTR-2026-000001 |
| GET /arama?q=FTR-2026-000001&tur=FATURA | 200, gün 01.10.2026 | 200 · "01.10.2026 · 1400.00 TRY" |
| GET /arama?q=FTR-2026-000001&tur=GENEL | 200, aynı gün | 200 · "01.10.2026 · 1400.00 TRY" |
| GET /faturalar | kayıt UTC anı korunur | 200 · tarih "2026-09-30T22:30:00", KESILDI |
| GET /gider-pusulalari | 200 | 200 · 2 pusula (ilk gram 50) |
| SQL (kuyumcu_net, tüm kiracılar) `faturalar` saat ≥ 21 UTC | etki ölçümü | KESILDI 6/6, TASLAK 2/8, IPTAL 1/9, ODENDI 0/2. 00:00:00'a yazılmış belge 0. Bu belgeler aramada artık işletme günüyle görünür; veri değişmedi, düzeltme fişi gerekmez |
| Kiracı temizliği `zsh kiraci-temizle.sh … --uygula` | 0 satır | rc=0 · tenants/faturalar/hareketler/kullanicilar = 0/0/0/0. tenants 12 → 13: fark eşzamanlı açılan `PAKETKESIF-P5-TEST Tur2` (t5), bu paketten kalan kiracı yok |

## TUR 2 — Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9704, kendi profil dizinleri; `olc.mjs` + `sikisma-olc.js`)
| Senaryo (her satır 16 ölçüm: 4 genişlik × BASİT/PRO × açık/koyu) | Geçen | Ölçüt |
|---|---|---|
| fatura-tarih-0130 | 16/16 | liste 1.10.2026 = çekmece 2026-10-01 |
| fatura-tarih-2330 | 16/16 | liste 30.09.2026 = çekmece 2026-09-30 |
| fatura-kalem-uzun (7 hane, uzun açıklama) | 16/16 | kap dışı 0, kesik 0, kaydırma 0, iki haneli olmayan ₺ 0, toplam "1.241.376,50 ₺" |
| fatura-kalem-fisten | 16/16 | aynı ölçüt, toplam "10.948,81 ₺" |
| gider-pusulalari | 16/16 | örtülü 0, kaydırma 0, son düzenleme tarihi görünür |
| pusula-detay-t2 | 16/16 | birincil yalnız Kaydet; Yazdır ikincil; iptal ikincil + tehlike, kontrast 5,90 / 9,16; gram = liste; ipucu kapalı; açıklama, KVKK ve "md." görünür 0; PRO title VUK md.234 |
| pusula-iptal-onay | 16/16 | ortak onay modalı açık, Onayla gerekçesiz kilitli |
- Genel sayaçlar 112 ölçümün tamamında 0: K1, K3, K5, K7, K8, K10, yatay kaydırma, ham ayar, BÜYÜK_ALT. K9 en çok 6: çekmece ve pencere arkasındaki sayfa, tur 1'de belgelenen ölçüm gürültüsü.
- Read ile bakılan görüntüler:
  - `olc-t3-b-k/foto/pusula-detay-t2__BASIT_karanlik_1440.png`: hakemin bulgusundaki görünümün düzeltilmiş hali. Alt çubukta tek dolu "Kaydet"; "Yazdır" ve "Pusulayı iptal et" çerçeveli ikincil; KVKK paragrafı yok; "50,00 g".
  - `olc-t3-p-a/foto/pusula-detay-t2__PRO_aydinlik_1440.png`: aynısı PRO açıkta; iptal adında madde numarası yok.
  - `olc-t5-p-k/foto/pusula-detay-t2__PRO_karanlik_375.png`: 375'te ızgara sığıyor, tek birincil Kaydet.
  - `olc-t4-p-a/foto/fatura-kalem-fisten__PRO_aydinlik_1440.png`: KDV Matrahı 9.124,01 ₺, KDV 1.824,80 ₺, Has Altın Değeri 0,00 ₺, Satır Toplamı 10.948,81 ₺. Tamamı iki hane, kap içinde.

## TUR 2 — DoD sözleşmesi
- Yeni varlık yok, yeni uç yok, migration yok.
- Pusula iptali yıkıcı eylemdir. Ortak onay modalı ve 20 karakterlik gerekçe korundu. Geri dönüş yolu iptal + yeni pusuladır; düzeltme yolu metninde ve kapalı ipucunda yazıyor.

## TUR 2 — Dokunulan sayfalar
- `GiderPusulalari.razor`: tur 1'de yalnız doğrulanmıştı. Bu turda KRİTİK bulgu yüzünden değişti; gerekçe aynı bulgu zincirinin hasımlı devri.
- `Faturalar.razor`: tur 1'de açılmıştı. Gerekçe doğrulayıcının iki DÜŞÜK bulgusu (istemci günü, para hücreleri).

## TUR 2 — Değişen dosyalar (bu turda)
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor`
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor` (BelgeGunu; kalem para hücreleri)
- `dotnet/Kuyumcu.Web/Servisler/Bicim.cs` (alan dışı, gerekçe yukarıda)
- `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (alan dışı, gerekçe yukarıda)
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css` (kural 15)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP3.cs` (`PusulaDuzenlenebilirAciklama` TR+EN kısaldı)
- Testler:
  - `dotnet/Kuyumcu.Tests/BelgeKalanTestleri.cs`
  - `dotnet/Kuyumcu.Tests/PusulaEkranTestleri.cs` (1 satır)
  - `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs` (5 satır)

## TUR 2 — Koşul karşılama
| Koşul | Durum |
|---|---|
| Tarih: liste, çekmece, basılı belge ve DB aynı gün; 23:30 TSI testi | Sağlandı. Arama ucu da artık işletme günü; istemci işletme saat dilimine sabit |
| Kesilmiş belge etkisi | SQL ile ölçüldü: geri yazılmış belge 0. Arama gösterimi düzeldi, veri değişmedi |
| Kırpık hücre 0, ham kod 0, yapışık değer 0 | 112/112 (tur 1: 192/192) |
| bUnit ve mutasyon | Tur 2: 7 yöntem / 9 durum, 14/14 mutasyon. Tur 1: 13 test, 15/15 |
| build 0/0 | 0 hata. Tek uyarı başka şeridin test dosyasında (CS8601) |
| denetim TEMİZ | Bu paketin sayaçları ✓. Tek ✗ P-30, başkasının ve tarih tetikli |
| Tam süit yeni kırmızı 0 | 8415 / 0 başarısız |
| Test kiracısı silindi | Sağlandı |
| KRİTİK pusula çekmecesi | Kapandı (16/16 + 4 bUnit durumu + 7 mutasyon) |

## TUR 2 — Açık kalanlar (sahibiyle)
1. **Arama ucu para biçimi (Arama modülü sahibi, ZO):** fatura ve hareket sonuçları "1400.00 TRY" ham biçimde. Fiş ve banka hareketi `EkranPara` kullanıyor, bunlar kullanmıyor. Fatura sonucu ekranda listelenmediği için (Rota null) bugün görünmüyor; hareket sonucu Uyum `KayitSecici`'de görünebilir. Bu turda dokunulmadı.
2. **Yinelenen kimlik (önceden kalma):** `gp-yazdir-{id}` hem satır eylem menüsünde hem çekmecede var (`GiderPusulalari.razor` SatirEylem ve çekmece düğmesi). Mevcut kimlikler değişmez kuralı yüzünden değiştirilmedi; testler çekmece kapsamıyla seçiyor. Sahibi: frontend (Ertuğ), ayrı ZO maddesi.
3. **Fatura arama sonucu genel paletten açılamıyor:** Rota null olduğu için palet "Sonuç yok" diyor (`olc-t3-p-k/foto/…375.png` ara görüntüsünde görüldü). Ürün kararı gerekir: fatura sonucuna `/faturalar` rotası verilsin mi? Sahibi: Arama modülü sahibi.
4. DENETİM ✗ P-30 DEMO_INSAN_SURESI: orkestratör ve demo sahibi.
5. 5199 dinlemiyor: orkestratör (`qa-sunucu.sh`).
6. Tur 1'den devreden DÜŞÜK maddeler (gün sonu 375 Tutar sütunu, kalem Ayar hücresi "—") aşağıdaki tur 1 listesinde aynen duruyor.

## TUR 2 — Kurucu / mali müşavir
- Yeni mevzuat hükmü eklenmedi.
- Tur 1 notu geçerli: belge günü ve seri yılı işletme (İstanbul) gününe göre. Mali müşavir teyidi istenmeli.

---

# TUR 1 (önceki rapor, değiştirilmeden)

# PAKET BELGE-KALAN — uygulama raporu (01.10.2026, şerit t1 · API 5160 · vekil 5161)

Rol: Taşkın (backend) ve Ertuğ (frontend). Paket hasımlı, çünkü tarih yasal belge verisidir. COMMIT YOK.
Kaynak: KESIF-P3 tasarım hakeminin tur 2 bulguları (TUR 2 YÜKSEK) ve tur 1'de "kapandı" diye beyan edilen maddelerin doğrulanması.

## Özet
| Kapı | Sonuç |
|---|---|
| Build (son, temiz) | **0 hata · 0 uyarı** |
| Kendi testlerim | 13/13 yeşil (BelgeKalan*) · P22Ekran dahil filtre 18/18 · ilgili geniş küme 953 testten tek kırmızı çıktı ve düzeltildi (aşağıda) |
| Mutasyon | **15/15 kırmızı.** Hepsi geri alındı, `cmp` AYNI |
| Tam süit (tek koşum, son kod) | 8346 test · 8335 geçti · **10 başarısız** · 1 atlandı. **Bu paketin yeni kırmızısı 0.** 10 kırmızının hepsi başka şeritlerin (§ Açık kalanlar) |
| denetim.sh | rc=1. Tek ✗ `P-30 DEMO_INSAN_SURESI` (tarih tetikli, bu paketle ilgisi yok; KESIF-P3 tur 2'de de vardı). Bu paketin sayaçları ✓ (302 ✓) |
| Başsız Chrome | 12 senaryo × 1440/1280/960/375 × BASİT/PRO × açık/koyu = **192 ölçüm.** Hepsi geçti, genel sayaçlar (K1, K3, K5, K7, K8, K10, Y, ham ayar, BÜYÜK_ALT) 0 |
| Canlı API ve SQL | Tüm adımlar beklenen kodla döndü |
| Test kiracısı | Silindi (`kiraci-temizle.sh … --uygula`). Kiracının tenant, fatura, denetim ve hareket satırlarının hepsi 0 |

## 1. Tarih hatası (önce yapıldı) — kök neden
- **Kayıt modeli.** `faturalar.tarih` bir UTC anıdır: `DateTime.UtcNow` ya da `dto.Tarih`. Sunucunun rapor gün sınırı işletme gününe (Europe/Istanbul) göredir (`Zaman.cs`, K5).
- **Liste.** `Bicim.Tarih` değeri `ToLocalTime()` ile yerel güne çevirip basar. Liste doğrudur.
- **Çekmece (hata).** `Faturalar.razor` `DuzenleAc` içinde `dTarih = f.Tarih` satırı ham UTC tarihini `<input type=date>`'e koyuyordu. Sonuç: 00:00–03:00 TSI arasında kesilen kayıt (UTC'de önceki gün 21:00–24:00) listede **1.10**, çekmecede **30.09** görünüyordu.
- **Veri etkisi (asıl risk).** `DuzenleKaydet` tarihi her seferinde gönderiyordu. Sunucu da `tarih.ToDateTime(00:00)` yazıyordu. Kullanıcı tarihe dokunmadan Kaydet'e basınca belge **bir gün geri** yazılıyordu.
- **Aynı kök, ikinci yer.** `FaturaSeriKapisi.Tuket(db, fatura, fatura.Tarih.Year)` seri yılı olarak UTC yılını kullanıyordu. 1 Ocak 00:00–03:00 TSI'de kesilen belge önceki yılın serisinden numara alırdı.

### Düzeltme
- **İstemci (Faturalar.razor).** Çekmece günü `BelgeGunu(t) = t.ToLocalTime().Date` ile hesaplanıyor; kural listeyle aynı. Tarih değişmediyse PATCH gövdesine **girmiyor** (PATCH sözleşmesi: gövdede yoksa koru).
- **Sunucu (FaturaModulu.cs, yalnız tarih okuma/yazma).**
  - PATCH'teki `tarih` işletme günüdür. Kaydın yerel günü aynıysa kayıt **değişmez**, anın saati korunur. Gün farklıysa eski davranış sürer (o günün 00:00 UTC'si = 03:00 TSI, yerel gün aynı).
  - Seri yılı artık `Zaman.UtcdenYerele(fatura.Tarih).Year`.
  - Taslak PATCH'in birim/has bölgesine dokunulmadı; o bölge CARI-BAKIYE'nin.

### Kesilmiş belgelerde etki (SQL ile ölçüldü, canlı kuyumcu_net)
| durum | toplam | UTC günü yerel günden farklı (saat ≥ 21 UTC) | 00:00:00'a yazılmış (çekmece izi) |
|---|---|---|---|
| KESILDI | 6 | 6 | 0 |
| TASLAK | 8 | 2 | 0 |
| IPTAL | 9 | 1 | 0 |
| ODENDI | 2 | 0 | 0 |

- Altı kesilmiş belgenin hepsinde `tarih ≈ olusturuldu`, yani özgün kesim anı duruyor. Çekmeceden geri yazılmış belge (saati 00:00:00 olan) **yok**. Kalıcı veri bozulması olmadı, düzeltme fişi gerekmiyor.
- Bu belgelerin listedeki günü doğru (10.09). Uygulama içinde basılı fatura çıktısı yok (`/cikti` yalnız fiş, makbuz, emanet ve ekstre basıyor). CSV dışa aktarım `…Z` damgalı UTC yazar, bu doğru.
- **Aynı kökün hâlâ açık iki yeri (dosya alanım dışında, açık kalanlarda):**
  - `AramaModulu.cs:289` `{f.Tarih:dd.MM.yyyy}` UTC gününü basıyor. Bu altı belge aramada 09.09 görünüyor.
  - SQL'de bu belgelerin UTC günü farklı, ama ekrandaki tüm okuyucular yerel güne çeviriyor.

### Kabul: 23:30 TSI ve 01:30 TSI kaydı
| Kayıt | DB (UTC) | Liste | Çekmece | PATCH aynı gün | PATCH başka gün |
|---|---|---|---|---|---|
| 01:30 TSI (30.09 22:30Z) | 2026-09-30 22:30 | 1.10.2026 (16/16 ölçüm) | 2026-10-01 (16/16) | 200, SQL 22:30 **aynı** (yerel gün 2026-10-01) | test: 29.09 → yerel gün 29.09 |
| 23:30 TSI (30.09 20:30Z) | 2026-09-30 20:30 | 30.09.2026 (16/16) | 2026-09-30 (16/16) | 200, kayıt aynı (test) | canlı: 29.09 → SQL 2026-09-29 00:00 → yerel 2026-09-29 |
| Yılbaşı 01:30 TSI (31.12 22:30Z) kesim | 2026-12-31 22:30 | — | — | — | canlı: **FTR-2027-000001** |

## 2. Diğer maddeler
| Bulgu | Durum öncesi (ölçüm) | Düzeltme | Sonra (192 ölçüm) |
|---|---|---|---|
| Düzenle çekmecesinde defter bağlı kalemin Miktar'ı düzenlenebilir (400 veriyordu) | Çekmecede `SatirIciDuzenle miktar` vardı | Kalemler penceresindeki kural çekmeceye de kondu: salt okunur `#fd-cekmece-kalem-adet-kilit-{id}`, sebep `#fd-cekmece-kalem-adet-sebep-{id}`, "Fişten değiştir" `#fd-cekmece-kalem-adet-fis-{id}` (tek satır). Bağsız kalem ve fiyatlar serbest | kilit 1 · miktar düzenleyici 0 · K3 0 (ilk turda "Fişten değiştir" iki satıra kırılıyordu, düzeltildi) |
| PRO 1440 Kalemler: Satır Toplamı kırpık | Taslak (düzenlenebilir) 11 sütun: tablo 1432 / kap 1296, kap dışında 6 hücre ("KDV Tutarı", "Satır Toplamı"). P3 tur 2 bunu yalnız kesilmiş/7 haneli senaryoda ölçmüştü | `zo-kalem-duzenlenir` sınıfı (PRO + TASLAK) ve `@container zokalem (max-width:1600px)` ile her genişlikte kart (pencere tavanı 84rem) | PRO 16/16 kart · kap dışı 0 · kesik 0 · kaydırma 0 |
| 375'te uzun açıklama kesik | Kap kaydırması 214 px (375), BASİT 960 kartta 59 px | Açıklama hücresi sarar (2. sütun `.satir-ici` ve `.si-tetik` normal/anywhere). Sayılar tek satır kalır | 16/16 kaydırma 0, açıklama görünür (görüntü aşağıda) |
| Stok değeri "Altın: … hasGümüş: … has" yapışık | Reyon stok hücresinde satır içi `<span>`'lar ayırıcısız | `zo-maden-satir` (CSS `display:block`), her maden ayrı satır | yapışık 0 · PRO'da 2 maden satırı, blok. BASİT'te reyon kartı yok, yapışık 0 |
| Gün sonu PRO ham ödeme kodu | "VERESIYE/KART/NAKIT" | `KatalogSozlugu.OdemeAraci` kullanılıyor, ham kod `title`'da | ham 0 · PRO 8 hücre "Veresiye/Kart/Nakit" |
| Denetim izinde ham ayar anahtarı | "Arayüz tercihi güncellendi: menu acik gruplar" (iki çeviri yolu da ş/ı'sız boşluklu yazıyordu) | `KesifP3Sozlugu.TercihAnahtarlariniCevir`: beyaz listenin 5 anahtarı (TR+EN), iki modda da. Ham kayıt değişmedi, PRO `title`'da duruyor | ham 0 · PRO 8/8 "menüde açık bölümler" görünür. BASİT bu kaydı sunucuda zaten süzüyor (`sistemHaric`, API ile doğrulandı) |
| Gider pusulası 1440 eylem sütunu (tur 1) | doğrulama | değişiklik yok | 16/16 örtülü 0 · kaydırma 0 · "Son düzenleme tarihi" görünür |
| Kuyum önizleme kilit kutusu (tur 1) | doğrulama | değişiklik yok | 16/16 iç boşluk 16/16 px · 14 px · kesişme yok · cümle 1 kez · kesik kenar |
| Kim ne yaptı BASİT cümle 3 kez (tur 1) | doğrulama | değişiklik yok | BASİT 8/8: "yalnız eklenir" 0, KVKK 0, düzeltme satırı 0. PRO: cümle 1 kez |
| Gider pusulası detay çekmecesi (tur 1) | doğrulama | değişiklik yok | 16/16 ızgara `grid`, ipucu kapalı, has 4 hane. BASİT'te madde numarası 0 |

## Sapmalar ve gerekçeleri
- **`Kuyumcu.Tests/P22EkranTestleri.cs` (dosya alanı dışında, tek beyan).** `B3_taslak_satirinda_Duzenle_dugmesi_PATCH_atar` testi, "tarih her zaman gönderilir" beyanını taşıyordu. Bu tam olarak veri hatasının kendisi. Test artık tarihe dokunulmadığında gövdede **olmadığını** ölçüyor. Tarihin değişince gönderildiği `BK_Fatura_cekmece_tarihi…` testinde. Mutasyon MBK-2 iki testi de kırmızı yaptı.
- **Seri yılı düzeltmesi.** Görev "tarih okuma/yazma" diyor. `Tuket(…, Year)` bir tarih okumasıdır ve kesilmiş belge etkisi kapsamında olduğu için düzeltildi.
- **PATCH'te farklı gün.** Yazılan değer eskisi gibi o günün 00:00 UTC'si (03:00 TSI). Hem UTC hem yerel okuyucuda gün aynı kalıyor, bu yüzden değiştirilmedi.
- **Ortak yardımcı.** Ortak bileşenler (P7) ve `Sozlukler.cs` HUB'ına dokunulmadı. Yeni metinler ve çeviriler `Sozlukler.KesifP3.cs` içinde.
- **Test sınıfları.** Mevcut ekran test sınıflarının sahte API'leri `private`. Bu yüzden `BelgeKalanTestleri.cs` kendi sahte API'siyle yazıldı; her uca gerçekçi yanıt veriyor.

## Migration
Gerekmedi (şema değişikliği yok).

## Testler (yeni 13, güncellenen 1)
`Kuyumcu.Tests/BelgeKalanTestleri.cs`:
- DB:
  - `BK_Taslak_tarih_PATCH_isletme_gunu_ayniysa_kayit_degismez`
  - `BK_Taslak_tarih_PATCH_2330_TSI_ayni_gun_korunur`
  - `BK_Seri_yili_isletme_yilidir`
- bUnit:
  - `BK_Fatura_cekmece_tarihi_liste_gunuyle_ayni_ve_dokunulmazsa_gonderilmez`
  - `BK_Fatura_cekmece_defter_bagli_kalem_adedi_salt_okunur`
  - `BK_Fatura_PRO_taslak_kalem_tablosu_duzenlenir_sinifi`
  - `BK_Fatura_BASIT_taslak_kalem_tablosu_duzenlenir_sinifi_yok`
  - `BK_Raporlar_reyon_stok_maden_ayri_satir`
  - `BK_Raporlar_gun_sonu_faturasiz_odeme_okunur_ad`
  - `BK_DenetimIzi_tercih_anahtari_okunur` (BASIT, PRO)
  - `BK_Tercih_anahtarlari_sozlukte_tam`
- CSS: `BK_css_kurallari`

Güncellenen: `P22EkranTestleri.B3_taslak_satirinda_Duzenle_dugmesi_PATCH_atar`.

Test sayısı: önceki tam süit (P3 tur 2) 8256, bu koşum 8346. Aradaki fark paralel şeritlerin ve bu paketin (+13) testleri.

## Mutasyon tablosu (hepsi geri alındı, `cmp` AYNI; kayıtlar `/private/tmp/kuyumcu-serit/BELGE-KALAN/mut-*.log`)
| Kod | Kırılan kural | Kırmızı test |
|---|---|---|
| MBK-1 | Çekmece ham UTC günü (`dTarih = f.Tarih`) | BK_Fatura_cekmece_tarihi… |
| MBK-2 | Tarih her zaman gönderilir | BK_Fatura_cekmece_tarihi…, P22 B3_taslak… |
| MBK-3 | Sunucu PATCH koşulsuz yazar | BK_Taslak_tarih_PATCH_isletme_gunu…, BK_Taslak_tarih_PATCH_2330… |
| MBK-3b | Sunucu UTC günüyle karşılaştırır (eski davranış) | BK_Taslak_tarih_PATCH_isletme_gunu… |
| MBK-4 | Seri yılı UTC yılı | BK_Seri_yili_isletme_yilidir |
| MBK-5 | Çekmecede defter bağlı adet düzenlenebilir | BK_Fatura_cekmece_defter_bagli… |
| MBK-6 | `zo-kalem-duzenlenir` yok | BK_Fatura_PRO_taslak… |
| MBK-6b | BASİT'te de `zo-kalem-duzenlenir` | BK_Fatura_BASIT_taslak… |
| MBK-13 | "Fişten değiştir" tek satır sınıfı yok | BK_Fatura_cekmece_defter_bagli… |
| MBK-7 | Reyon stok maden satırı sınıfsız (yapışık) | BK_Raporlar_reyon_stok… |
| MBK-8 | Ham ödeme kodu | BK_Raporlar_gun_sonu… |
| MBK-9 | Tercih çevirisi çağrılmaz | BK_DenetimIzi_tercih_anahtari_okunur (BASIT + PRO) |
| MBK-10 | `.zo-maden-satir` blok değil | BK_css_kurallari |
| MBK-11 | Açıklama yine nowrap | BK_css_kurallari |
| MBK-12 | Sözlükten `gorunum-subesi` silinir | BK_Tercih_anahtarlari_sozlukte_tam |

Son mutasyondan sonra razor dosyalarına `touch` yapılıp temiz build alındı: 0 hata, 0 uyarı. Not: bir ara derleme, eşzamanlı yazımda razor çıktısını eski kaynaktan üretti; bunu DLL içerik taramasıyla yakaladım ve yeniden derledim.

## denetim.sh
rc=1, 302 ✓, 1 ✗: `P-30 DEMO_INSAN_SURESI` ("son tarih 2026-09-30 geçti", demo provası; bu paketle ilgisi yok).

Bu paketin sayaçları ✓:
- IgnoreQueryFilters 0
- ExecuteUpdate 5/5
- Hareketler.Add 1
- record 3/3
- satır içi style 61/196
- ModalPencere 47/55 (artış yok)
- SABIT_RENK 0

## Canlı doğrulama (API 5160, vekil 5161, kiracı `PAKETBELGE-KALAN-TEST Kuyumculuk` 01M3T8WREY2AEJCTBFAJPMYAWZ)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris | 200 | 200 |
| GET /faturalar | 200, tz kayıtları UTC | 200 · 01:30 → "2026-09-30T22:30:00", 23:30 → "2026-09-30T20:30:00" |
| PATCH tz0130 `{"tarih":"2026-10-01"}` (aynı yerel gün) | 200, kayıt aynı | 200 · SQL `2026-09-30 22:30:00` (TSI günü 2026-10-01) |
| PATCH tz2330 `{"tarih":"2026-09-30"}` | 200 | 200 |
| PATCH tz2330 `{"tarih":"2026-09-29"}` | 200, gün değişir | 200 · SQL `2026-09-29 00:00:00` → TSI 2026-09-29 |
| PATCH `{"tarih":"30.09.2026"}` | 400 | 400 "Tarih formatı geçersiz (yyyy-MM-dd bekleniyor)" |
| GET /faturalar/{fişten}/kalemler | hareketId dolu | 200 · hareketId true |
| PATCH defter bağlı kalem miktar farklı | 400 | 400 "Bu satır satış fişinden geldi; adedi fişten değiştirin." |
| POST taslak 31.12 22:30Z, PATCH /durum KESILDI | 2027 serisi | 200 · **FTR-2027-000001** · SQL tarih 2026-12-31 22:30 (TSI 2027-01-01) |
| GET /raporlar/pos-fatura-mutabakati | sunucu ham kod döndürür (çeviri yalnız ekranda) | 200 · 8 faturasız · ["VERESIYE","KART","NAKIT"] |
| SQL denetim_kayitlari | ham kayıt değişmez | "Arayüz tercihi güncellendi: menu-acik-gruplar" AYNEN |

## Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9700–9703; kendi profil dizinleri)
- Araç: `sikisma-olc.js`, sayfa özel ölçüler ve ham ayar/BÜYÜK_ALT sayacı.
- Betikler: `/private/tmp/kuyumcu-serit/BELGE-KALAN/olc.mjs` ve `senaryo.json`. Ham veri `olc-t2-*/olcum.jsonl`.
- Koşumlar sırayla yapıldı (vekilin eşzamanlı istek sorunu yüzünden paralel koşulmadı).

| Senaryo (her satır 16 ölçüm: 4 genişlik × BASİT/PRO × açık/koyu) | Geçen | Ölçüt |
|---|---|---|
| fatura-tarih-0130 | 16/16 | liste 1.10.2026 = çekmece 2026-10-01 |
| fatura-tarih-2330 | 16/16 | liste 30.09.2026 = çekmece 2026-09-30 |
| fatura-cekmece-fisten | 16/16 | kilit 1, miktar düzenleyici 0, K3 0 |
| fatura-kalem-uzun (7 hane, 150 g uzun açıklama, taslak) | 16/16 | kap dışı 0, kesik 0, kaydırma 0. PRO her genişlikte kart, BASİT 1440/1280 tablo, 960/375 kart |
| fatura-kalem-fisten | 16/16 | aynı ölçüt |
| raporlar-stok | 16/16 | yapışık 0 (PRO 2 blok maden satırı) |
| raporlar-gunsonu | 16/16 | ham ödeme 0 (PRO 8 hücre) |
| denetim-izi | 16/16 | ham tercih 0 (PRO "menüde açık bölümler" 2) |
| gider-pusulalari | 16/16 | örtülü 0, kaydırma 0, son tarih görünür |
| pusula-detay | 16/16 | ızgara, kapalı ipucu |
| fatura-yeni-kuyum | 16/16 | pad 16/16, cümle 1, kesişme yok |
| denetim-izi-suzgec | 16/16 | düzeltme satırı 0. BASİT: yalnız eklenir 0, KVKK 0 |

- Genel sayaçlar, 192 ölçümün tamamında 0: K1, K3, K5, K7, K8, K10, yatay kaydırma, ham ayar, BÜYÜK_ALT.
- K9 (üst üste binme) çekmece ve pencere senaryolarında en çok 6. Kaynak, katmanın arkasındaki sayfa ve yapışkan alt çubuk; KESIF-P3'te belgelenen ölçüm gürültüsü. Pencere içi K9 0–2.

Read ile bakılan görüntüler:
- `olc-t2-p-a/foto/fatura-kalem-uzun__PRO_aydinlik_1440.png`: kart, Satır toplamı ve KDV kap içinde.
- `olc-t1-b-a/foto/fatura-kalem-uzun__BASIT_aydinlik_375.png`: ÖNCE, açıklama kesik.
- `olc-t2-b-k/foto/fatura-kalem-uzun__BASIT_karanlik_375.png`: SONRA, açıklama iki satır.
- `olc-t2-p-a/foto/fatura-cekmece-fisten__PRO_aydinlik_1440.png`: çekmece tarihi 01.10.2026 = liste 1.10.2026; adet salt okunur ve "Fişten değiştir".
- `foto-stok-reyon__PRO_aydinlik_1440.png`: "Altın: 159,2652 has" / "Gümüş: 83,2500 has" ayrı satır.
- `foto-gunsonu-faturasiz__PRO_karanlik_375.png`: Veresiye/Kart/Nakit.
- `olc-t2-p-k/foto/denetim-izi__PRO_karanlik_1440.png`: "Arayüz tercihi güncellendi: menüde açık bölümler".

Tüm dosyalar `/private/tmp/kuyumcu-serit/BELGE-KALAN/` altında.

## DoD sözleşmesi
- Yeni varlık yok.
- Defter bağlı kalemin adedi salt okunur. Sebep ekranda yazıyor, düzeltme yolu "Fişten değiştir" (UX-S25/S30). Sunucu kuralı zaten 400 veriyordu, değişmedi.
- Taslak tarihi düzenlenebilir kalıyor; PATCH sözleşmesi korundu: gövdede yoksa koru, tarih silinemez.

## Dokunulan sayfalar
- Faturalar.razor, Raporlar.razor, DenetimIzi.razor. Üçü de KESIF-P3'te açılmıştı. Gerekçe: aynı bulgu zincirinin hasımlı devri; kapı görevi açıkça bu dosyaları verdi.
- GiderPusulalari.razor değişmedi (yalnız doğrulandı).

## Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` içinden bu paketinkiler)
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`: çekmece tarihi, tarih gövdesi, çekmece adet kilidi, `zo-kalem-duzenlenir`
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`: maden satırı, gün sonu ödeme adı
- `dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor`: tercih çevirisi iki modda
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css`: kural (12), (13), (14)
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`: yalnız PATCH tarih bloğu ve seri yılı satırı
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP3.cs`: `TercihAnahtarlariniCevir` (TR+EN)
- `dotnet/Kuyumcu.Tests/BelgeKalanTestleri.cs` (yeni), `dotnet/Kuyumcu.Tests/P22EkranTestleri.cs` (tek beyan)

## Koşul karşılama
| Koşul | Durum |
|---|---|
| Tarih: liste, çekmece, DB aynı gün; 23:30 TSI testi | Sağlandı (tablo § 1). Uygulama içinde basılı fatura yok. Arama ucu açık kalanda |
| Kesilmiş belge etkisi SQL ile ölçüldü | Sağlandı: 6 kesilmiş belgenin hiçbiri geri yazılmamış |
| Kırpık hücre 0, ham kod 0, yapışık değer 0 (1440/1280/960/375, BASİT+PRO, açık+koyu) | Sağlandı: 192 ölçüm |
| bUnit ve mutasyon | 13 test · 15/15 mutasyon |
| build 0/0 | Sağlandı |
| denetim TEMİZ | Bu paketin sayaçları ✓. Tek ✗ P-30 başkasının ve tarih tetikli |
| Tam süit yeni kırmızı 0 | Sağlandı |
| Test kiracısı silindi | Sağlandı |

## Açık kalanlar (sahibiyle)
1. **Tam süitte bu paketin olmayan 10 kırmızı:**
   - CARI-BAKIYE (t9, Cari modülünde süren iş; `CariModulu.cs` 02:43'te değişti):
     - `CariBakiyeHttpTestleri.Liste_ve_ekstre_JSON_ayni_emanetsiz…`
     - `CariBakiyeTestleri.Emanetli_musteri_liste_detay…`
     - `CARIBAKIYESinirTestleri.Bos_null_veresiye_payi…`
     - `CARIBAKIYETur2SinirTestleri.Emanetsiz_has_4_hane`
     - `KesifP2HttpTestleri.CariEkstre_satir_cari_has_toplami_bakiyeye_esit`
   - KESIF-P5+SATIS-4 (t5; `Islemler.razor` 02:40'ta değişti): üç test, `KesifP5EkranTestleri.SATIS4b_Hurda_*`.
   - KESIF-P3 tur 2'den beri açık:
     - `GorunumSubesiTestleri.K3_Aylik_secili_sube_ve_tum_subeler` (SUBE-YASAL23)
     - `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar`
2. **Aynı saat dilimi kökü, dosya alanım dışında:** `Kuyumcu.Api/Modules/Arama/AramaModulu.cs:289` `{f.Tarih:dd.MM.yyyy}` UTC gününü basıyor. 00:00–03:00 TSI belgesi aramada bir gün geri görünüyor; canlıda 6 kesilmiş belge etkileniyor. Sahibi: Arama modülü sahibi. Önerilen düzeltme: `Zaman.UtcdenYerele(f.Tarih)`.
3. DÜŞÜK (görüldü, kapsam dışı): Gün sonu 375 px'te "Fişe bağlanmamış faturalar" tablosunun Tutar sütunu kesik (`foto-gunsonu-faturasiz__PRO_karanlik_375.png`, üst kısım).
4. DÜŞÜK: Kalem "Ayar" hücresi, POST'ta `AyarId` verilen elle kalemde "—" gösteriyor (sunucu kalem ayarını saklamıyor olabilir). Doğrulanmadı, varsayım yapılmadı; ayrı incelenmeli.
5. Ölçüm notu: tenants sayısı başlangıçta 13, sonda 14. Fark paralel şeritlerin açık test kiracılarından (PAKETKESIF-P5-TEST, PAKETYASAL3-TEST-T2 listede görünüyor). Bu paketin kiracısı silindi, bu paketten kalan kiracı 0. QA-TEST-* kiracılarına dokunulmadı.

## Kurucu / mali müşavir
- Yeni mevzuat hükmü eklenmedi.
- **Not:** Fatura tarihi ve seri yılı artık işletme (İstanbul) gününe göre. 1 Ocak 00:00–03:00 arasında kesilen belge yeni yılın serisinden numara alıyor. VUK'ta belge tarihinin düzenlendiği yerel gün olduğu varsayımı teknik olarak doğal kabul edildi; birincil kaynakla doğrulanmadı. Mali müşavirden bu yorumun teyidi istenmeli.
- Canlıda geçmişte bu sınırda kesilmiş belge yok (en erkeni 10.09).
