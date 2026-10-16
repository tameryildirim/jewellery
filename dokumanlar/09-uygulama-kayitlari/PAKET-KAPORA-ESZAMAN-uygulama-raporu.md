# PAKET KAPORA-ESZAMAN — Uygulama raporu

**Tarih:** 09.10.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** t5 (API 5166 · vekil 5167 · CDP 9508 · KUYUMCU_TEST_DB=kuyumcu_test_t5)
**Kaynak:** PAKET-EKSIK-B1-YAZDIR doğrulayıcı tur 3 bulguları (KRİTİK eşzamanlı çift kapora · ORTA açık yönlendirme · DÜŞÜK kapora geçmişi). **COMMIT YOK.** Migration: **gerekmedi** (yeni tablo/kolon yok; `hareketler.siparis_id` zaten vardı).

## 1. Yapılanlar

| Bulgu | Düzeltme | Nerede |
|---|---|---|
| **KRİTİK** eşzamanlı çift kapora → iki tahsilat, biri siparişe bağsız | Kilit + kontrol + tahsilat + sipariş bağı **tek veritabanı işleminde**. İşlem açılır (`IsBaslatVeyaDevral`), **ilk deyim** `SELECT id FROM siparisler WHERE id=? AND tenant_id=? FOR UPDATE` (ikinci istek burada bekler), sipariş ve kapora durumu kilitten **sonra** okunur → ikinci istek mevcut `KaporaKilidi` cümlesiyle **409** alır. Commit en sonda (kilit orada bırakılır). | `Api/Modules/Siparis/SiparisModulu.cs` `Kapora` |
| TahsilatGirHareket'in kendi commit'i | `BeginTransactionAsync` → `IsBaslatVeyaDevral`, `CommitAsync` → `if (tx is not null)`. **Geriye uyumlu:** tek başına çağrıldığında aynen işlem açıp commit eder (cari tahsilat ucu, vezne `TahsilatDali`); dış işlem varsa ona katılır. | `Api/Modules/Cari/CariModulu.cs` (yalnız 2 satır) |
| Satır kilidi yardımcısı | `db.Database.Kilitle(KilitTablosu, id, tenantId)` — tablo adı kapalı listeden (enum), kimlik/kiracı parametre; işlem yoksa `InvalidOperationException` (sessiz geçmez). | `Api/Common/SatirKilidi.cs` (yeni) |
| **DÜŞÜK** kapora geçmişi kayboluyor | Kapora tahsilat satırı defterde de siparişe bağlanır (`hareketler.siparis_id`, izlenen varlık — `SiparisiSatisaCevir` ile aynı yol; ExecuteUpdate yok). `Detay` yeni `kaporaGecmisi` döner: siparişe bağlı TAHSILAT satırları + güncel bağ (eski veri), her biri için storno durumu/tarihi, en yeni önce, sınır 100 (B-8). | `SiparisModulu.cs` `KaporaGecmisi` |
| Kapora geçmişi ekranda | Kapora kartında **kapalı** `Katlanir` "Kapora geçmişi (n)"; yalnız geri verilmiş kapora varsa çizilir (zorunlu önce — tek canlı kaporada özet yeterli). Satır: tutar (Bicim.Para), tarih-saat · ödeme şekli, rozet "Güncel" / "Geri verildi · tarih". Veri kartla gelir, açılışta istek yok. | `Web/Pages/SiparisDetay.razor`, `Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs` (`KaporaGecmisSatiri`), `Core/I18n/Sozlukler.KaporaEszaman.cs` (TR+EN konumsal) |
| **ORTA** açık yönlendirme `?donus=` | Kural TEK yerde: `Core/Domain/GuvenliYol` — `/` ile başlar, 2. karakter `/` ya da `\` değil, hiçbir yerde kontrol karakteri (\x00–\x1F, \x7F), `\` ve `:` yok; URL-çözülmüş hâli (en çok 3 kat) de aynı koşulu sağlar, daha derin kodlama reddedilir. Aksi → çağıranın uygulama içi varsayılanı, o da yoksa `/`. `Cikti.GuvenliDonus` artık bu yardımcıya devreder. | `Core/Domain/GuvenliYol.cs` (yeni), `Web/Pages/Cikti.razor` |
| Doğrulayıcının Skip'li 2 testi | `Sinir_eszamanli_cift_kapora_tek_tahsilat` ve `Guvenlik_donus_kontrol_karakteri_dis_siteye_cikamaz` (×3) — Skip kaldırıldı (yalnız öznitelik satırı), yeşil. | `Tests/Dogrulayici/EKSIKB1YAZDIRSinirTestleri.cs` |

## 2. SINIFI KAPAT — "kontrol et → ayrı işlemde yaz" taraması

| Uç | Bulgu (eski hâl) | Karar | Kanıt |
|---|---|---|---|
| Sipariş kaporası `POST /siparisler/{id}/kapora` | kontrol işlem dışı, tahsilat kendi işlemi, bağ ayrı SaveChanges | **KAPATILDI** (siparisler FOR UPDATE + tek işlem) | KE01 · doğrulayıcı testi · canlı 1×200/9×409 |
| Hareket stornosu `POST /islemler/{id}/storno` (48-A) | "zaten stornolanmış" işlem dışı; `storno_kaynak_id` tekil değil → iki ters kayıt mümkündü | **KAPATILDI** (kaynak hareket kilidi + kontrol yinelenir; red denetim kaydı işlem geri alındıktan sonra yazılır) | KE06 · canlı 1×200/9×409 |
| Kasa hareketi stornosu | aynı | **KAPATILDI** (kasa_hareketleri kilidi) | KE07 |
| Banka hareketi stornosu | aynı | **KAPATILDI** (banka_hareketleri kilidi) | eşzamanlı test YOK → § 9 |
| Virman stornosu | aynı (iki bacak) | **KAPATILDI** (iki bacak kimlik sırasıyla kilitlenir — çapraz istekte deadlock yok) | eşzamanlı test YOK → § 9 |
| Siparişi satışa çevir | "zaten satışa çevrilmiş" + durum işlem dışı | **KAPATILDI** (sipariş kilidi + Reload + iki kapı yinelenir) | KE08 |
| Satış iadesi / Alış iadesi | kalan iade hakkı işlem dışı | **KAPATILDI** (kaynak hareket kilidi + hak yeniden hesaplanır) | KE09 (satış); alış iadesi testi YOK → § 9 |
| Emanet teslimi `POST /emanet/{cariId}/cikis` | elde kalan emanet işlem dışı → eksiye düşebilirdi | **KAPATILDI** (cari satırı kilidi + bakiye yeniden okunur) | KE10 · canlı 1×200/9×400 |
| Vezne tahsil | — | **ZATEN KORUNUYOR** (önce `IsConcurrencyToken` ile fiş sahiplenilir, 409) | kod okuması (VezneModulu.Tahsil ADIM 1) |
| Cari tahsilat ucu (doğrudan) | tekil kısıt yok — her istek meşru yeni tahsilattır | kapsam dışı (idempotans anahtarı ayrı konu) | — |
| Emanet GİRİŞ stornosu × eşzamanlı emanet çıkışı | storno içindeki `EldeKalan` cari kilidi almıyor (kaynak hareketi kilitli, cari değil) | **KUYRUK** — § 9 madde 3 | — |
| Hurda/Stopaj stornosu | `KasaHareketiStorno` üzerinden | dolaylı kapandı | — |

## 3. Sapmalar ve gerekçeleri

1. **Açık yönlendirmede varsayılan:** koşul metni "aksi `/`" diyor. Çıktı sayfasının bağlamlı varsayılanı (ekstre → `/cariler`, diğer → `/raporlar/islem-defteri`) korundu — ikisi de aynı köken; `GuvenliYol.Donus(yol, varsayilan)` varsayılan da güvensizse `/` verir. Yalnız `/` tek başına "dönüş hedefi" sayılmaz (mevcut B14 testi bunu bekliyordu, korundu).
2. **Tarama:** projede `?donus=` dışında sorgu parametresinden yönlendirme yok (`SupplyParameterFromQuery` tüm kullanımlar okundu: sekme/kasa/yon/ac/hedef — hepsi ekran içi seçim, gezinme hedefi değil; sunucuda `Redirect(` yok). Ortak yardımcı yine Core'da; yeni dönüş parametresi bunu kullanmak zorunda (Cikti örneği).
3. **Raw SQL:** projede ilk `ExecuteSqlRawAsync` kullanımı. EF'de `FOR UPDATE` için başka yol yok; tablo adı enum'dan, değerler parametre. `IgnoreQueryFilters/Find` kullanılmadı; ExecuteUpdate/Delete sayacı değişmedi (5).
4. **Geçmiş akordeonu koşulu:** ilk sürüm `Count > 1 || Any(Stornolu)` idi; mutasyon M12 `Count > 1` dalının gereksiz olduğunu gösterdi (aynı anda tek canlı kapora → iki satır ancak biri geri verilmişse doğar). Koşul `Any(Stornolu)` olarak sadeleşti, mutasyon yeniden tanımlandı.
5. **Satır içi boşluk:** rozet ilk ölçümde "Havale" metnine yapışıktı (gerçek Chrome 1280'de görüldü). Ortak CSS dosyaları (`app.css`, `zo-islem.css`) başka şeritlerin alanında olduğundan boşluk işaretlemede `@(" ")` ile verildi; yeniden ölçüldü.

## 4. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Kapora (hareketler TAHSILAT) | Düzenlenmez — **storno** (48-A, ekranda "Kaporayı geri ver", SAHİP/MÜDÜR, gerekçe ≥ 20) | Finansal satır: hard DELETE yok |
| `hareketler.siparis_id` (kapora satırında) | Sunucu yazar; salt okunur | geçmiş bağı; storno satırı listelenmez |
| `siparisler.kapora_hareket_id` | Sunucu yazar; yeniden kaporada güncellenir, eskisi geçmişte kalır | veri kaybı yok |
| Kapora geçmişi listesi | Türetilmiş (defterden), salt okunur | yeni varlık yok → yeni PATCH/DELETE gerekmez |
Yeni varlık/tablo yok → migration gerekmedi. Yeni form yok; tek yeni düğme `#sipd-kapora-gecmis-ac` (akordeon aç/kapa, ortak Katlanir — istek atmaz, veri kartla gelir).

## 5. Testler

Önce: kendi testim 0 (doğrulayıcının 2 testi Skip). Sonra: **+43 yeni** — `KaporaEszamanTestleri.cs` (DB: KE01–KE11 = 11; saf: KS01 23 satır + KS02 4 satır + KS03 = 28) · `KaporaEszamanEkranTestleri.cs` (bUnit 4) + doğrulayıcının 4 satırı Skip'ten çıktı.

| Kapsam | Sonuç |
|---|---|
| `KaporaEszaman` + `EKSIKB1` + `EksikB1` (son kodla) | **108/108** |
| Komşu süitler (Kapora · Storno · Emanet · Iade · KasaBakiye · Vezne · Siparis · Virman · Banka · Tahsilat · P21 · P26 · Cikti) | **1279/1279** |
| **Tam süit** (trx `/private/tmp/kuyumcu-serit/trx-pKAPORA-ESZAMAN/_Ramazan-MacBook-Air_2026-10-09_11_32_41.trx`) | **11730 toplam · 11726 geçti · 2 başarısız · 2 atlandı** |

Tam süitteki 2 kırmızı: `SATIS3EkranTestleri.T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ`, `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` (İşlemler ekranı bUnit). **Bu paketin ürünü değil:** aynı sınıf izole koşumda **157/157 geçti** (1 Skip); dosyalar (`Islemler.razor` 03:33, `SATIS3EkranTestleri.cs` 06:50) bu paketin dokunmadığı ve benim yardımcılarımı (`GuvenliYol`, `KaporaGecmis`, `SatirKilidi`) kullanmayan dosyalar → yük altında zamanlamaya bağlı kararsızlık. Sahibi: SATIS-3/İşlemler ekranı (t1, YILDIZ-4 şeridi İşlemler'e dokunuyor). **Bu paketin yeni kırmızısı: 0.**

## 6. Mutasyon kanıtı (perl tek eşleşme → filtreli test → yedekten geri → cmp; yedek `/private/tmp/kuyumcu-serit/mut-yedek/KAPORA-ESZAMAN/`)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1 | Kapora: `Kilitle(Siparisler…)` satırı silindi | KE01_on_paralel_kapora_tek_tahsilat_digerleri_409 · Dogrulayici.Sinir_eszamanli_cift_kapora_tek_tahsilat | AYNI |
| M2 | TahsilatGirHareket: `IsBaslatVeyaDevral` → `BeginTransactionAsync` | KE01 · KE03_tahsilat_dis_isleme_katilir… · KE05 · KE06 · Sinir_eszamanli… · Sinir_kurus_yuvarlamasi_kasa_ve_cari_ayni (+diğer kapora testleri) | AYNI |
| M3 | TahsilatGirHareket: koşullu commit silindi | KE02_tahsilat_tek_basina_kendi_islemini_commit_eder | AYNI |
| M4 | Storno: hareket kilidi silindi | KE06_on_paralel_storno_tek_ters_kayit | AYNI |
| M5 | Kasa hareketi stornosu: kilit silindi | KE07_on_paralel_kasa_hareketi_storno_tek_ters_satir | AYNI |
| M6 | Satışa çevir: sipariş kilidi silindi | KE08_on_paralel_satisa_cevir_tek_satis | AYNI |
| M7 | Satış iadesi: kilit olmayan satıra ("yok") | KE09_on_paralel_satis_iadesi_tek_iade | AYNI |
| M8 | Emanet çıkışı: cari kilidi silindi | KE10_on_paralel_emanet_teslimi_tek_cikis | AYNI |
| M9 | GuvenliYol: kontrol karakteri denetimi silindi | Dogrulayici.Guvenlik_donus_kontrol_karakteri… (\t \n \r) · KS01 | AYNI |
| M10 | GuvenliYol: URL-çözme döngüsü atlandı | KS01_kotu_donus_reddedilir_koke_duser (%2F/%5C/%09… satırları) | AYNI |
| M11 | Kapora satırına `SiparisId` yazılmıyor | KE01 · KE05_kapora_gecmisi_geri_verilen_kaybolmaz | AYNI |
| M12 | Akordeon koşulu `Any(Stornolu)` → `Any(!Stornolu)` | KEE02_tek_canli_kapora_gecmis_cizilmez · KEE03_tek_geri_verilmis_kapora_gecmiste_gorunur | AYNI |
| M13 | Kilitle: işlem-yok denetimi `if (false)` | KE11_satir_kilidi_islem_disinda_firlatir | AYNI |

Not: M5–M7 ilk koşumda paralel şeridin Web derlemesiyle çakıştı (MSB4018 webcil dosya kilidi → eski ikili koştu, sonuç geçersiz); derleme yeniden denemeli betikle yeniden koşuldu, tablo yeniden koşum sonucudur. İlk M12 (`Count > 1` → `> 99`) hiçbir testi kırmadı → § 3 madde 4. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

## 7. Denetim

`bash denetim.sh` → yalnız **✗ P-30 DEMO_INSAN_SURESI** (görev metninde hariç). İlk koşumda benim kaynaklı iki ✗ vardı ve kapatıldı: `Sınırsız ToListAsync 142 > 140` (geçmiş sorgularına `.Take(100)`; yorumdaki `;` sayacın bölmesini bozuyordu, düzeltildi → **140 ✓**) ve `P-03 CONTRACTS_DISI_MODEL 1` (Api'deki kayıt `KaporaGecmisKaydi` adını aldı → **0 ✓**). Diğer: `ExecuteUpdate/Delete 5 ✓` · `ModalPencere 48 (tavan 55) ✓` · `P-11 …_TOPLAM 47 ✓` · `P-26 DEFTERE_YAZMAYAN_PARA_MODULU 0 ✓` · `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 0 ✓` (yeni istisna iletisi yok — mevcut cümleler yeniden kullanıldı).

## 8. Canlı doğrulama (5166, kendi PID'im; vekil 5167; kiracı PAKETKAPORA-ESZAMAN-TEST-A `01M4FWAW5VSNDA6XC1TPVJ36J9`)

Sistem anahtarı: kendi API sürecime rastgele üretilmiş anahtar ortam değişkeniyle verildi (dosya chmod 600, iş sonunda boşaltıldı; değer hiçbir çıktıya yazılmadı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| s1'e **10 eşzamanlı** kapora 1000 HAVALE (iş parçacığı bariyeri) | 1×200, 9×409 | **{200: 1, 409: 9}**; 409: "Bu siparişte kapora zaten alındı. Düzeltme için kaporayı geri verin (storno); …" |
| aynı kaporaya **10 eşzamanlı** storno | 1×200, 9×409 | **{200: 1, 409: 9}**; "Bu hareket zaten stornolanmıştır; …" |
| s1 yeniden kapora 1500 | 200 | 200 |
| GET s1 `kaporaGecmisi` | 2 satır, en yeni önce | [1500 HAVALE güncel] · [1000 HAVALE stornolu] |
| s3 kapora 500, ardından ikinci istek **EN** | 200 / 409 İngilizce | 200 / 409 "A deposit has already been taken on this order. To correct it, return the deposit (reversal); …" |
| emanet giriş 10 g K22 (9,16 has) → **10 eşzamanlı** tam teslim | 1×200, 9×400 | **{200: 1, 400: 9}** |

SQL (kuyumcu_net, kiracı A):
```
no             kapora_satiri bagli guncel_tl
S-2026-000001  2             1     -1500.00
S-2026-000002  1             1      -500.00
TAHSILAT: -1000 (sipariste=1) · +1000 (storno=1) · -1500 (sipariste=1) · -500 (sipariste=1)
kasa_hareketleri: 4 satır, toplam 2000.00 · kasa bakiyesi 2000.00 (= +1000 −1000 +1500 +500; iki kat YOK)
EMANET_GIRIS 1 × 9.160000 · EMANET_CIKIS 1 × −9.160000 (net 0, eksiye inmedi)
```

## 9. Gerçek tarayıcı ölçümü (başsız Chrome CDP 9508, sikisma-olc.js; `olcum.json`, foto `/private/tmp/kuyumcu-serit/KAPORA-ESZAMAN/canli/foto/`)

| Rota × {1280, 375} × {BASİT, PRO} × {açık, koyu} | K1 | K3 | K5 TRY | K9 | K10 | K11 | yatay | Not |
|---|---|---|---|---|---|---|---|---|
| s1 — geçmiş KAPALI | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 akordeon görünür, gövde YOK, sayı "2" |
| s1 — geçmiş AÇIK | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 liste görünür: "1.500 ₺ … · Havale Güncel" / "1.000 ₺ … · Havale Geri verildi · …" |
| s3 — tek canlı kapora | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 akordeon YOK, özet tutarı görünür |

**24/24 ölçüm temiz.** (K12 alanı bu ölçer sürümünde yok.) Dar sütun: kapora kartı ana alanın tam genişliğinde.

Açık yönlendirme — gerçek Chrome, `/cikti/TAHSILAT_MAKBUZU/{hk}?donus=<v>` → `#cikti-geri` tıklandı:

| donus | Geri sonrası | köken aynı |
|---|---|---|
| `%2F%09%2Fkotu.example.com` | /raporlar/islem-defteri | ✓ |
| `%2F%0A%2Fkotu.example.com` | /raporlar/islem-defteri | ✓ |
| `%2F%0D%2Fkotu.example.com` | /raporlar/islem-defteri | ✓ |
| `%2F%5Ckotu.example.com` | /raporlar/islem-defteri | ✓ |
| `%2F%2Fkotu.example.com` (`//`) | /raporlar/islem-defteri | ✓ |
| `%2F%252Fkotu.example.com` (`/%2F`) | /raporlar/islem-defteri | ✓ |
| `https%3A%2F%2Fkotu.example.com` | /raporlar/islem-defteri | ✓ |
| `%2Fsiparisler%2F{s1}` (meşru) | /siparisler/{s1} | ✓ |

Read ile bakılan görüntüler (3): `s1-gecmis-acik__PRO_karanlik_375` (akordeon açık, iki satır, rozetler sarıyor, taşma yok), `s1-gecmis-acik__BASIT_aydinlik_1280` (rozet "Havale"ye yapışık → § 3 madde 5 ile düzeltildi), düzeltme sonrası `s1-gecmis-acik__BASIT_karanlik_1280` (boşluk var, koyu temada rozet okunur).

## 10. Temizlik

API (PID 21474 ve yeniden başlatılan süreç) ve vekil `kill`; `lsof` 5166/5167 boş. Kiracı `zsh kiraci-temizle.sh 01M4FWAW5VSNDA6XC1TPVJ36J9 --uygula` ile silindi (ikinci çağrı "Kiracı YOK" doğruladı); kendi 1 davet satırım (`kiraci_davetleri`, email `paketkaporaeszaman%`) silindi. **tenants 20 → 20**, `PAKETKAPORA%` = 0, QA-TEST kiracıları (4) dokunulmadı. Sır dosyaları (sistem.anahtar, fikstur.json, canli-durum.json) `printf '' >` ile boşaltıldı. Proje ağacına geçici dosya bırakılmadı (yedekler `/private/tmp/kuyumcu-serit/` altında).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tek işlem + satır kilidi, ikinci istek 409, kasada tek hareket | ✓ | KE01 (10 paralel) · canlı 1/9 + SQL |
| TahsilatGirHareket geriye uyumlu, tüm çağrı yerleri | ✓ | KE02 (tek başına commit) · KE03 (dış işleme katılım) · vezne/cari uçları değişmeden (komşu süit 1279/1279) |
| Sınıf taraması tablosu, bulunanlar kapandı ya da kuyrukta | ✓ | § 2 (7 uç kapatıldı, 1 kuyruk) |
| Açık yönlendirme: %09/%0A/%0D/%5C/`//`/`/%2F` → uygulama içi, köken aynı | ✓ | KS01 (23 satır) · gerçek Chrome 8/8 |
| Ortak Core yardımcısı + diğer dönüş parametreleri | ✓ | `GuvenliYol`; tarama: başka dönüş parametresi yok (§ 3 madde 2) |
| Kapora geçmişi sipariş kartında, veri kaybı yok | ✓ | KE05 · KEE01–04 · canlı `kaporaGecmisi` 2 satır · tarayıcı 16/16 |
| Mutasyon ≥ 4 kırmızı, cmp AYNI | ✓ | 13/13 (§ 6) |
| build 0/0 · denetim TEMİZ (P-30 hariç) | ✓ | § 6 son satır · § 7 |
| Tam süit 0 yeni kırmızı | ✓ | § 5 (2 kırmızı başkasının, izole 157/157) |
| Gerçek tarayıcı 1280/375 × BASİT/PRO × açık/koyu | ✓ | § 9 (24 ölçüm + 8 yönlendirme) |
| Test kiracısı silindi, tenants başlangıç değerinde | ✓ | § 10 (20 → 20) |

## 12. Açık kalanlar

1. **Banka hareketi stornosu, virman stornosu ve alış iadesi** için kilit kodu eklendi (aynı yardımcı, aynı kalıp) ama **eşzamanlı testi yazılmadı** — mutasyonla kanıtlanmadılar. Küçük ek test paketi önerisi (Aşkın).
2. Tam süitte başkasına ait 2 kararsız kırmızı: `SATIS3EkranTestleri.T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ`, `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` — izole yeşil; sahibi İşlemler ekranı (t1).
3. **Emanet GİRİŞ stornosu × eşzamanlı emanet çıkışı:** storno `EldeKalan` kontrolünü kaynak hareket kilidiyle yapıyor, cari satırını kilitlemiyor; emanet çıkışı ise cari satırını kilitliyor → iki farklı kilit, aynı anda gelirse emanet eksiye inebilir. Düzeltme: storno'da EMANET_GIRIS dalında da `Kilitle(Cariler, kaynak.CariId)` + yeniden okuma. Satis/Storno sahibi ile (bu pakette dokunulmadı — kapsamı büyütmemek için).
4. Kalıcı çözüm önerisi (Bertan): `hareketler.storno_kaynak_id` üzerinde **tekil indeks** (NULL'lar serbest) — veritabanı düzeyinde ikinci savunma; migration gerektirir, bu paketin kapsamı dışında.

## 13. Kurucu / mali müşavir

Mevzuat hükmü yazılmadı, eşik/oran değişmedi (MASAK/tevsik kapıları aynı `TahsilatGirHareket` gövdesinde, artık siparişin işlemi içinde koşuyor — red hâlinde hiçbir satır kalmaz).

## Değişen dosyalar
(`git status --porcelain`, bin/obj hariç — bu paketin dokunduğu dosyalar)
- `dotnet/Kuyumcu.Api/Common/SatirKilidi.cs` (yeni)
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` (Kapora, Detay, KaporaGecmisi)
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` (TahsilatGirHareket: 2 satır, işlem devralma)
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs` (4 storno: kilit + yineleme)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (satış/alış iadesi: kilit + yineleme)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` (SiparisiSatisaCevir: kilit + yineleme)
- `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs` (Cikis: kilit + yineleme)
- `dotnet/Kuyumcu.Core/Domain/GuvenliYol.cs` (yeni)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KaporaEszaman.cs` (yeni)
- `dotnet/Kuyumcu.Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs` (KaporaGecmisi alanı + kayıt)
- `dotnet/Kuyumcu.Web/Pages/Cikti.razor` (GuvenliDonus → Core)
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor` (kapora geçmişi akordeonu)
- `dotnet/Kuyumcu.Tests/KaporaEszamanTestleri.cs` (yeni), `KaporaEszamanEkranTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/Dogrulayici/EKSIKB1YAZDIRSinirTestleri.cs` (yalnız 2 Skip özniteliği kaldırıldı; dosya doğrulayıcınındır)

**Dokunulan sayfalar:** `SiparisDetay.razor` (gerekçe: aynı dalga, doğrulayıcı DÜŞÜK bulgusu — EKSIK-B1-YAZDIR'ın açtığı sayfa), `Cikti.razor` (gerekçe: doğrulayıcı ORTA bulgusu; tek satır devir).

---

# TUR 2 — doğrulayıcı bulgularının kapatılması (09.10.2026, t5 · API 5166 · vekil 5167 · CDP 9508–9510)

Migration: **gerekmedi** (yeni tablo/kolon yok). COMMIT YOK. Mevzuat hükmü/eşik yazılmadı (MASAK eşikleri sunucudan `/masak/esikler` + `/masak/gereksinim`; istemciye sayı gömülmedi).

## T2-1. Bulgu → düzeltme

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| **[ORTA→KRİTİK] Kasa bakiyesi kayıp güncelleme** (oku-değiştir-yaz) | Yeni `Common/SatirKilidi.cs` › `BakiyeKilidi.Kilitle(db, Kasa / BankaHesap / CariBakiye)`: açık işlemde **kilitli okuma** (`SELECT bakiye … FOR UPDATE`, ADO komutu işleme bağlı) → değer izlenen varlığın özgün+güncel değerine yazılır, ardından mevcut `Bakiye = Bakiye + d` taze değer üzerinden hesaplanır. Kilitli okuma InnoDB'de anlık görüntüyü değil son commit'i döndürür (REPEATABLE READ altında EF Reload/düz SELECT yanlış olurdu). Aynı istekte ikinci delta: özellik "değişti" ise yeniden okunmaz. İşlem yoksa no-op (davranış değişmez). **Sınıfın tamamı:** kasa/banka bakiyesine yazan 16 satırın hepsine eklendi (CariModulu tahsilat + döviz tahsilat, KasaModulu.HareketGir, BankaModulu.HareketEkleIc, GiderModulu.Kaydet, VirmanModulu.BakiyeIsle ×2, EvrakModulu.TahsilEdildi ×2, AcilisServisi ×2, StornoServisi ×5, SatisModulu.KasaGuncelle). **Cari bakiye:** tek yazıcı `CariBakiyeYazici.Uygula` aynı kilitli okumayı yapar (TL + has + USD/EUR/GBP). | KE12 (10 paralel tahsilat aynı kasa+cari → kasa +1000, SUM(kasa_hareketleri)=1000, cari −1000) · KE18 (10 paralel yalnız-altın tahsilat → has −10 g; kasa kilidinin serileştirmediği yol) · doğrulayıcının `Sinir_ayni_kasaya_farkli_siparis_eszamanli_kapora_kasa_toplami_dogru` **Skip kaldırıldı, yeşil** · canlı A/B + SQL |
| [DÜŞÜK] Banka/virman stornosu + alış iadesi kilitlerinin testi yok | KE13 (banka storno 10 paralel → 1 ters satır, hesap 0), KE14 (virman storno 10 paralel → 1 ters çift, iki bakiye döner), KE15 (tam alış iadesi 10 paralel → 1 iade, kasa döner) | M16/M17/M18 kırmızı |
| [DÜŞÜK] Satışa çevirde kilit sonrası durum makinesi kapısı yinelenmiyordu | Kapı `SatisaCevirDurumKapisi` yardımcısına alındı, kilit+Reload SONRASI ikinci kez çağrılır (ileti aynı, yeni çıplak metin yok) | KE16 (kilit tutulurken sipariş HAZIR→ONAYLANDI, bekleyen satışa çevir 400, SATIS 0) · M19 |
| [DÜŞÜK] TestOrtami.cs'de gömülü test MySQL parolası | Yedek dizge koddan **kaldırıldı**; sıra: `KUYUMCU_TEST_MYSQL_SIFRE` → yerel `docker-compose.yml`'nin `MYSQL_ROOT_PASSWORD:` ile BAŞLAYAN satırı (yorum satırı seçilmez) → yoksa açık `InvalidOperationException`. Eski değer ile compose değerinin **eşit olduğu** çıktısız (yalnız true/false) doğrulandı → paralel şeritlerin davranışı değişmedi. Değer hiçbir çıktıya yazılmadı. | tam süit bu yolla koştu (11790 test) |
| [DÜŞÜK] GuvenliYol `:` her yerde reddediliyordu | `:` yalnız YOL kısmında yasak, ilk `?`'ten sonra serbest; kontrol karakteri/`\`/kodlama kuralları aynen | KS02 `/x?saat=10:00`, `/x?saat=10%3A00` korunur; KS01 `/a:b?x=1`, `/x?a=1%0A:2` reddedilir · M21 · gerçek Chrome |
| **[KRİTİK] Tasarım hakemi — eşik üstü kaporada çıkış yolu yok, ham `masak.aciklama`** | (1) Sunucu: `SiparisKaporaDto.Masak` (MasakBilgiDto) eklendi ve `TahsilatGirHareket`'e aynen geçer (kapı orada — yeni kapı yazılmadı). (2) Ekran: kapora çekmecesine Hurda/Banka ile **aynı** `MasakFormu` + aynı uçlar; tutar eşikte ya da sunucu MASAK'la reddettiğinde (bugünkü toplam) alanlar açılır; tek sade cümle `#sipd-kapora-masak-not` "Bu tutarda müşterinin kimliği ve işlemin amacı yazılmalı."; Kaydet kilidi sebebi "Kaydetmek için kimlik bilgilerini doldurun." (UX-S30); gereksinim gelirken "Kimlik alanları hazırlanıyor…". MASAK reddinde ham sunucu metni **hiçbir koşulda** basılmaz (alanlar tamken ret → `MasakRet` sade cümlesi; eşik alınamadıysa `MasakNot`). Başarı/sıfırlamada kişisel veri formdan silinir. Sözlük TR+EN konumsal (`Sozlukler.KaporaEszaman.cs`). | KE17 (400.000 beyansız 400 "MASAK…", beyanla 200, defterde siparişe bağlı) · KEE05/KEE06 (bUnit, iki mod) · M20/M22 · canlı D · gerçek Chrome 16/16 |

## T2-2. Sınıf taraması (kayıp güncelleme — bakiye yazıcıları)

| Yazıcı | Önce | Şimdi |
|---|---|---|
| kasalar.bakiye (11 satır) · banka_hesaplari.bakiye (5 satır) | oku-değiştir-yaz, kilitsiz | kilitli taze okuma (işlem içinde) |
| cari_bakiyeler (tek yazıcı Uygula) | aynı | kilitli taze okuma |
| İşlem açmayan çağıranlar | — | yardımcı no-op; özel yardımcılar (VirmanModulu.BakiyeIsle, AcilisServisi, StornoServisi.BacakBakiyesiIsle, SatisModulu.KasaGuncelle) çağıranlarının işlemi içinde koşar (testler KE14/KE15 ile kanıtlı olanlar: virman storno, alış iadesi). |
| Kalan risk (kuyruk) | Bakiye KAPILARI (KasaBakiyeKapisi.NakitDogrula, BakiyeSigar) kilitten ÖNCE eski değerle karar veriyor → eşzamanlı çıkışta kapı eşiği aşılabilir (defter/özet tutarlı kalır, yalnız "eksiye düşme" kapısı). Düzeltme: kapı kontrolünü kilit sonrasına taşımak — Kasa modülü sahibi ile. |

## T2-3. Mutasyon (perl tek satır `if $.==N` → filtreli `KaporaEszaman|KAPORAESZAMAN` → yedekten geri → cmp; yedek `/private/tmp/kuyumcu-serit/mut-yedek/KAPORA-ESZAMAN-t2/`)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M14 | TahsilatGirHareket kasa `BakiyeKilidi.Kilitle` yorumlandı | KE12 · Dogrulayici.Sinir_ayni_kasaya_farkli_siparis_eszamanli_kapora_kasa_toplami_dogru | AYNI |
| M15 | Uygula'daki cari kilidi yorumlandı | ilk koşumda **hiçbiri** (test açığı: KE12'de kasa kilidi serileştiriyordu) → KE18 yazıldı → **KE18** kırmızı | AYNI |
| M16 | Banka storno `Kilitle` kaldırıldı | KE13 | AYNI |
| M17 | Virman storno kilit döngüsü `.Take(0)` (ilk biçim derlenmedi → yeniden tanımlandı) | KE14 | AYNI |
| M18 | Alış iadesi `Kilitle` kaldırıldı | KE15 | AYNI |
| M19 | Kilit sonrası `SatisaCevirDurumKapisi` kaldırıldı | KE16 | AYNI |
| M20 | Kapora → tahsilat `Masak = dto.Masak` kaldırıldı | KE17 | AYNI |
| M21 | GuvenliYol `sorguda = true` → `false` | KS02 (`/x?saat=10:00`, `/x?saat=10%3A00`) | AYNI |
| M22 | Razor MASAK ret dalı `&& false` | KEE06 | AYNI |

9/9 kırmızı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata**. (Tam yeniden derlemede Kuyumcu.Tests'te 6 xUnit/nullable uyarısı görüldü: SUBEKAPSAMASinirTestleri, KesifP2Testleri, SonKalanlarTestleri ×2, Yildiz2Testleri ×2 — bu paketin dosyaları değil.)

## T2-4. Testler

Kapsamım (`KaporaEszaman|KAPORAESZAMAN`): tur 2 ilk koşum (Skip kaldırıldıktan sonra) 79 → **83 test, 0 başarısız, 0 atlanan** (M15/M17 koşumunda ölçüldü, KE18 dahil) (yeni: KE12–KE18 = 7 DB, KEE05–06 = 2 bUnit, KS satırı +4; doğrulayıcının Skip'i kalktı).

**Tam süit** (trx `/private/tmp/kuyumcu-serit/trx-pKAPORA-ESZAMAN-t2/_Ramazan-MacBook-Air_2026-10-09_13_46_48.trx`): **11790 toplam · 11785 geçti · 3 başarısız · 2 çalıştırılmadı**. Kırmızılar başkasına ait (bu paket ilgili dosyalara dokunmadı):
- `Yildiz4BTestleri.Y4B_05_kare_adi_tuval_metni_gider_ve_virman`, `Y4B_14_tum_ekranlar_39_kare_6_bolum_tuval_metni` — beklenen "Gider ve virman", gerçek "Gider ve para aktarımı" (sözlük metni; test dosyası koşum sırasında 13:48'de YILDIZ-4 şeridince düzenleniyordu) → sahibi YILDIZ-4 (t1) / TOPLU-7 metin.
- `OlcekTekTestleri.OL6_olcek_css_yalniz_olcu_yazar_sabit_renk_yok_ve_moddan_bagimsizdir` — ölçek CSS'te `--dugme-yuksekligi` bildirimi (CSS; bu paket CSS'e dokunmadı) → sahibi YILDIZ-4/TOPLU-7 düğme ölçeği.
Tur 1'in iki SATIS3 kararsız kırmızısı bu koşumda yeşil. **Bu paketin yeni kırmızısı: 0.**

## T2-5. Denetim (`bash denetim.sh`)

✗ P-30 DEMO_INSAN_SURESI (görevde hariç) · ✗ **B-3 ESIK_ISTEMCIDE 5 (tavan 4)** — eşleşen satırlar: `TevsikOneri.cs:8`, `Islemler.razor:517`, **`Islemler.Yildiz.cs:81`** (YILDIZ-4 şeridinin yeni dosyası, "30.000 ₺" yorum metni), `FisOdemeKasa.razor:15/85` (TOPLU-7 alanı). Hiçbiri bu paketin dosyası değil; benim eklediğim kodda eşik sayısı yok (eşik `/masak/esikler`'den). Diğer: ExecuteUpdate/Delete 5 ✓ · ModalPencere 48 (tavan 55) ✓ · Sınırsız ToListAsync 140 ✓ · CARI_BAKIYE_DOGRUDAN_YAZIM 0 ✓ · CARI_BAKIYE_AYNA_UYUSMAZ yeni 0 ✓ · SUNUCU_CIPLAK_TURKCE_ILETI 0 ✓.

## T2-6. Canlı (5166, kiracı `PAKETKAPORA-ESZAMAN-TEST-T2` `01M4G4C5979DPKJ12ADAEVEE71`, sistem anahtarı rastgele üretilip yalnız kendi sürecime ortam değişkeniyle verildi)

| Adım | Beklenen | Gerçek |
|---|---|---|
| A: 10 FARKLI siparişe aynı kasaya eşzamanlı 100 TL kapora | 10×200, kasa +1000 | `{200:10}`, kasa 0 → **1000** |
| B: düz `/cari/{id}/tahsilat` 10 eşzamanlı 100 TL (aynı kasa+cari) | 10×200, kasa +1000 | `{200:10}`, kasa 1000 → **2000** |
| C: aynı siparişe 10 eşzamanlı kapora | 1×200, 9×409 | `{200:1, 409:9}` "Bu siparişte kapora zaten alındı…" |
| D: 400.000 kapora beyansız / tam beyanla | 400 MASAK / 200 | 400 "MASAK: 370.000 TL ve üstü…" / **200, kaporaTutari 400000** |
| E: banka hareketi 500 → 10 eşzamanlı storno | 1×200, 9×409, bakiye 0 | `{200:1, 409:9}`, bakiye **0** |

SQL (kuyumcu_net, kiracı):
```
kasalar: Ana Kasa  bakiye 403000.00 | SUM(kasa_hareketleri) 403000.00 | 22 satır   (eşit — kayıp güncelleme YOK)
TAHSILAT: 22 satır · siparişe bağlı 12 · toplam -403000.00
cari_bakiyeler (B carisi): tl_bakiye -2000.00  (= 10×-100 tahsilat + -1000 kapora)
siparisler S-…01..12: kapora_hareket_id bağlı=1, siparişe bağlı kapora satırı=1 (her biri)
banka_hesaplari: bakiye 0.00 | defter 0.00 | storno satırı 1
```

## T2-7. Gerçek tarayıcı (başsız Chrome CDP 9508, `betikler/tur/sikisma.js` + ek denetim; `scn.jsonl`, görüntüler `/private/tmp/kuyumcu-serit/KAPORA-ESZAMAN/tur2/canli/gor/`)

Senaryo 1 `esik-ustu`: çekmece aç → 400.000 yaz. Senaryo 2 `sunucu-ret`: bugünkü toplamı 400.000 olan müşteride 500 yaz → Kaporayı al (sunucu MASAK reddi).

| Senaryo × {1280, 375} × {BASİT, PRO} × {açık, koyu} | K1 | K3 | K5 TRY | K10 | K11 | yatay | ham `masak.aciklama`/`(şu an` | kimlik formu | Kaydet sebepli kilitli | hata bandı |
|---|---|---|---|---|---|---|---|---|---|---|
| esik-ustu (8 kombinasyon) | 0 | 0/0 | 0 | 0 | 0 | 0 | YOK | açık | ✓ "Kaydetmek için kimlik bilgilerini doldurun." | boş |
| sunucu-ret (8 kombinasyon) | 0 | 0/0 | 0 | 0 | 0 | 0 | YOK | açık (başlık "…bu müşterinin bugünkü toplamı 400.500 ₺") | ✓ | boş |

**16/16 temiz.** Gerekçeli iki ölçer değeri: **K9 = 6** (her kombinasyonda) — örnekler `#sayfa-basi-…baslik ⟂ "Kapora tutarı"`, `dd ⟂ "Kasa:"`: çekmece KATMANININ alttaki sayfanın üstüne binmesi (çekmece kapalıyken K9 = 0, boş çekmece tur 1 bileşeniyle K9 = 2; bileşen içi binme yok). **K8 = 1** (yalnız BASİT esik-ustu): eşleşen "Yildirimoglu-Karabulutlar" — test müşterisinin tireli adı (tohum verisi), arayüz metni değil. Çekmece genişliği 1280'de 480 px, 375'te tam ekran. K12 bu ölçer çağrısında dönmedi (alan yok).
Read ile bakılan görüntüler (3/4): `01-esik-ustu__BASIT_aydinlik_1280` (sade not + kimlik kartı, sebepli kilit, taşma yok), `02-sunucu-ret__PRO_karanlik_375` (koyu temada okunur, ham metin yok; PRO'da paylaşılan MasakFormu'nun kendi açıklaması), `02-sunucu-ret__BASIT_aydinlik_375` (BASİT'te açıklama yok, yalnız alanlar).

Açık yönlendirme — gerçek Chrome, `#cikti-geri` tıklandı: `%2F%09%2F…`, `%2F%0A%2F…`, `%2F%0D%2F…`, `%2F%5C…`, `%2F%2F…`, `%2F%252F…`, `https%3A%2F%2F…`, `%2Fa%3Ab` → hepsi `localhost:5167/raporlar/islem-defteri` (köken aynı); `%2Fsiparisler%3Fsaat%3D10%3A00` → `/siparisler?saat=10:00` (yeni meşru kalıp); `%2Fsiparisler%2F{s}` → `/siparisler/{s}`.

## T2-8. Temizlik

API (kendi PID) ve vekil `kill`; `lsof` 5166/5167 boş. Kiracı `zsh kiraci-temizle.sh 01M4G4C5979DPKJ12ADAEVEE71 --uygula` → ikinci çağrı "Kiracı YOK" · kendi 1 davet satırım silindi · sır dosyaları (anahtar, fikstur.json, durum.json) `printf '' >` ile boşaltıldı. **tenants: başlangıç 25 → son 24.** Benim kiracım silindi (doğrulandı); aradaki **−1 bu paketin değil**: aynı sürede başka şerit kendi test kiracısını silmiş (şu an kalan test kiracıları: PAKET7B-HAKEM-TEST, PAKETYILDIZ4B-TEST, PAKETKAPORA-HAKEM-TEST ×3 — tasarım hakeminin, dokunulmadı —, PAKET7B2-TEST). QA-TEST kiracılarına dokunulmadı.

## T2-9. Değişen dosyalar (tur 2)

- `dotnet/Kuyumcu.Api/Common/SatirKilidi.cs` (BakiyeKilidi) · `Common/CariBakiyeServisi.cs` (Uygula: 1 satır)
- Bakiye yazıcıları (yalnız `await BakiyeKilidi.Kilitle(db, x);` satırı): `Modules/Cari/CariModulu.cs` ×2, `Kasa/KasaModulu.cs`, `Banka/BankaModulu.cs`, `Gider/GiderModulu.cs`, `Gider/VirmanModulu.cs` ×2, `Evrak/EvrakModulu.cs` ×2, `Kurulum/AcilisServisi.cs` ×2, `Satis/StornoServisi.cs` ×5, `Satis/SatisModulu.cs` ×1
- `Modules/Satis/SatisModulu.P21.cs` (SatisaCevirDurumKapisi) · `Modules/Siparis/SiparisModulu.cs` (SiparisKaporaDto.Masak + geçiş)
- `Kuyumcu.Core/Domain/GuvenliYol.cs` · `Kuyumcu.Core/I18n/Sozlukler.KaporaEszaman.cs` (+3 metin TR+EN)
- `Kuyumcu.Web/Pages/SiparisDetay.razor` (kapora çekmecesi MASAK) — **tek dokunuş gerekçesi:** aynı paketin tur 2'si, tasarım hakeminin KRİTİK bulgusu
- `Kuyumcu.Tests/KaporaEszamanTestleri.cs`, `KaporaEszamanEkranTestleri.cs`, `TestOrtami.cs` (sır hijyeni), `Dogrulayici/KAPORAESZAMANSinirTestleri.cs` (yalnız 1 Skip özniteliği)

## T2-10. Açık kalanlar

1. Bakiye **kapıları** (NakitDogrula/eksiye düşme, BakiyeSigar) kilitten önce eski değerle karar veriyor (§ T2-2) — Kasa modülü sahibi.
2. Emanet GİRİŞ stornosu × eşzamanlı emanet çıkışı (tur 1 § 12 md. 3) ve `storno_kaynak_id` tekil indeksi (tur 1 § 12 md. 4) — kuyrukta.
3. Kilitlenme sırası: tahsilat yolunda sipariş → kasa → cari sırası sabit; farklı yollar (örn. satışta cari önce, kasa sonra) aynı kasa+cariyi ters sırada kilitlerse MySQL 1213 ile biri geri alınır (veri bozulmaz, istek hata alır). Ölçülmedi; global 1213 → 409 eşlemesi önerilir (Bertan).
4. Test parolası artık kodda yok ama docker-compose.yml'de (ve tarih boyunca git geçmişinde) duruyor; üretimde aynı değer kullanılıyorsa döndürülmeli — Mehmet / kurucu.
5. Tam süitteki 3 başkasına ait kırmızı ve denetimdeki B-3 artışı (§ T2-4, § T2-5) — sahipleri YILDIZ-4 / TOPLU-7.

## T2-11. Kurucu / mali müşavir

Yeni mevzuat hükmü yok. Kapora artık cari tahsilatla aynı MASAK beyanını taşıyabiliyor; eşik ve kademe kararı sunucudaki mevcut MasakKapisi'nde (değişmedi).
