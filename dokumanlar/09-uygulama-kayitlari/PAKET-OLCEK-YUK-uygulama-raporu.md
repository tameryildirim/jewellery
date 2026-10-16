# PAKET OLCEK-YUK — Yük/ölçek testi (10/50/100 kiracı) + TB-19 sınırsız liste · Uygulama raporu

> **TUR 2 (2026-10-07 16:55–17:55, düzeltme turu).** Doğrulayıcının 6 bulgusu bu turda ele alındı; tur 1 metni aşağıda (§ 0–13) AYNEN durur, tur 2'de değişen hükümler § T2 içinde ve § 12/13'ün sonundaki "t2" notlarıyla işaretlidir. **COMMIT YOK. Dosya silinmedi.**

## § T2 — Tur 2: kalan bulguların kapanışı

### T2.0 Ortam ve izolasyon notu (Anayasa md.1)
- Görev "eşzamanlı şerit YOK" diyordu; tur 2 sırasında ağaçta **MASAK-370 şeridi çalışıyordu** (`/private/tmp/kuyumcu-serit/MASAK-370`, 17:02–17:07 build/ef add/ef down; `Migrations/20261007140228_MasakKimlikEsigi370.cs`, `Masak*Testleri.cs`, `MasakKapisi.cs` 16:07 sonrası değişti; `kuyumcu_net.tenants`'a `PAKETMASAK370-TEST` kiracısı 14:31'de eklendi). Bu şeride DOKUNULMADI. Etkisi: (a) bir mutasyon koşumunda `NETSDK1177 apphost is already signed` yarışı (tekrar koşuldu, geçti); (b) filtreli koşumda MASAK kaynaklı yabancı kırmızı (§ T2.5). Yük ölçümü bu turda YENİDEN koşulmadı (tur 1 sonuçları, tek başına koşum altında alınmıştı ve değişen kod tezgâh profilinin çağırdığı uçlara dokunmuyor).
- Canlı doğrulama `kuyumcu_net`'e değil, **migration ile sıfırdan kurulan `kuyumcu_test_yuk`** DB'sine bağlı geliştirme API'siyle (5166) ve vekille (5167) yapıldı; iş sonunda DB DROP edildi (`sql_log_bin=0`). 5198/5199/5200'e, tarayıcı bölmesine dokunulmadı. CDP portları 9501–9508.

### T2.1 [KRİTİK · tasarım hakemi] Kâr-zarar > Ürünler 50'de sessizce kesiliyordu → KAPANDI
**Sunucu** (`Modules/Maliyet/KarZararServisi.cs` `UrunBazli` + uç):
- Yeni sorgu parametresi `ofset` (`[Range(0, 100_000)]`, servis içinde de `ofset < 0` → 400; DataAnnotations yalnız controller yolunda olduğundan servis içi elle kontrol).
- Sıra kararlı: `OrderByDescending(karTl.HasValue).ThenByDescending(karTl).ThenBy(urunId, Ordinal)` → `Skip(ofset).Take(limit)`. Eşit kârlı ürünler sayfalar arasında yer değiştirmez (örtüşme 0).
- Yanıta `ofset` ve **`toplamUrunSayisi`** (dönemde satılan farklı ürün sayısı, SUNUCUDAN — ekran türetmez) eklendi; `dahaFazlasiVar = toplamUrunSayisi > ofset + satirlar.Count`. Mevcut alanlar ve sırası korunur (geriye uyumlu).
**Sözleşme** (`Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs`): `UrunYanit(Satirlar, DahaFazlasiVar = false, ToplamUrunSayisi = 0, Limit = 0, Ofset = 0)` — eski gövdeler (yalnız `satirlar`) aynen ayrışır.
**Ekran** (`Pages/KarZarar.razor`, Ürünler sekmesi):
- Sınıra değdiyse tablonun altında `#kz-urun-sinir` (`bant bant-uyari`, `role=status`): "İlk 50 ürün gösteriliyor, bu dönemde 71 ürün satıldı. Daha fazlası var." ve `#kz-urun-daha` "Daha fazla göster" (ikincil düğme). Düğme **gerçek istek atar**: aynı dönem + şube, `ofset = gösterilen satır sayısı`; satırlar listeye EKLENİR. İstek sürerken disabled.
- Son sayfadan sonra bant ve düğme kalkar; yalnız genişletilmiş listede `#kz-urun-tamami` "71 ürünün tamamı gösteriliyor." (soluk küçük metin). Sınıra değmeyen listede hiçbir şey çizilmez (Pusula sadeliği / zorunlu-önce).
- Sonraki sayfa hatası listeyi BOŞALTMAZ (UX-S28): `HataBandi Id="kz-urun-daha-hata"`, bant ve düğme durur → yeniden denenebilir.
- Sayılar `Bicim.Girdi(n, "N0")` (tr-TR). Metinler `Sozlukler.KarRapor.cs` TR+EN (`UrunListeSiniri`, `UrunListeTamami`, `DahaFazlaGoster`; record kurucusu → eksik anahtar derleme hatası). Yeni CSS yok (mevcut `bant`, `bant-uyari`, `dugme dugme-ikincil`, `satir`, `mt-2`, `metin-soluk metin-kucuk`). Satır içi style yok. Mevcut id'ler değişmedi; yeni id'ler `kz-urun-*`.
**Dokunulan sayfalar:** `Pages/KarZarar.razor` (tek dokunuş; gerekçe: tasarım hakemi KRİTİK, paketin kendi ölçek senaryosunun ucu).

### T2.2 [ORTA] FisAra "en yeni fiş önce" sırası testsizdi → KAPANDI
`OY07` genişletildi: üç fişin `tarih`i test kurulumunda ham SQL ile (sistem bağlamı, yalnız test DB) −3/−1/−2 gün yapıldı; bu sıra kesim sırasından, fiş kimliği sırasından ve tersinden FARKLIDIR. Sayfalar birleşince sonuç `Assert.Equal(new[] { kendi[1], kendi[2], kendi[0] }, gelen)` ile SABİT listeyle sınanır. Doğrulayıcının MX1 mutasyonu bu turda yeniden uygulandı → **OY07 KIRMIZI** (M2-1).

### T2.3 [DÜŞÜK] TB-19 sınıflandırıcısı sezgiseldi → İNCELTİLDİ, tek kaynağa taşındı
`betikler/yuk/tolist-siniflandir.pl` yeniden yazıldı ve `denetim.sh` TB-19 artık BU betiği `--say` kipiyle çağırıyor (kural iki yerde değil TEK yerde; betik yoksa sayaç 9999 → fail-closed).
- `Contains` yalnız argümanı bir kimlik alanıysa (`…Id`/`…Hash`) "kimlik kümesi"dir. Metin araması (`c.Ad.Contains(q)`) ve tür listesi (`tipler.Contains(x.Tur)`) artık kimlik SAYILMAZ.
- `GroupBy` anahtarı `CariId/UrunId/FisId` olan ve kimlik kümesiyle daraltılmamış toplama → **BÜYÜYEN:toplama**, ayrı sayaç ve ayrı tavan.
- Sıra: SAYFALI → kimlik → BÜYÜYEN toplama → doğal toplama → üst kayıt → yapılandırma → SINIRSIZ. Yapılandırma listesine `KiraciAyarlari` eklendi (sabit anahtar listesi).
- Yeni döküm (`ham/OLCEK-YUK-tolist-siniflandirma.tsv`, 342 deyim): SAYFALI 140 · DOĞAL:kimlik 60 · DOĞAL:toplama 37 · DOĞAL:üst-kayıt 49 · DOĞAL:yapılandırma 29 · **BÜYÜYEN:toplama 1** · **SINIRSIZ 26**.
- Doğrulayıcının iki örneği: `PozisyonModulu.cs:103` (cari×maden bakiyesi, HAVING ≠ 0) → **BÜYÜYEN:toplama** (tavan 1). `CariBakiyeServisi.cs:220` `EmanetEtkileri` → çağıranın verdiği `cariIdler` kümesiyle `Contains(h.CariId)` daraltılıyor; dönen satır ≤ |cariIdler| → DOĞAL:kimlik (gerekçe kodda okundu: satır 210–221).
- Tavanlar: sınırsız 26 (değişmedi), büyüyen 1 (yeni, ölçülen değer). Gevşetme yok.
- Mutasyon kanıtı (M2-9, M2-10): eski (t1) kuralın KAÇIRDIĞI iki kalıp yeni sayaçta ✗ (§ T2.6).

### T2.4 [DÜŞÜK] Yük betiklerinde düz metin varsayılan parola → KALDIRILDI
`k6-cok-kiraci.js`, `k6-agir-rapor.js`, `yuk-kiraci-kur.mjs`: `YUK_SIFRE || '<metin>'` kaldırıldı; değişken yoksa betik durur (k6: init'te `throw` → "YUK_SIFRE ortam değişkeni zorunlu"; node: çıkış 2, ölçüldü). `grep -rn Gecici betikler/yuk` = 0. **SIR-2 paketi ağaçta henüz yok** (denetim.sh'ta "SIR-2" bölümü bulunmuyor); bu yüzden kapı burada, `denetim.sh` TB-19c sayacı olarak eklendi: `betikler/yuk/` altında `YUK_SIFRE ||/?? '…'` ya da `sifre: '…'` kalıbı = 0. SIR-2 paketi açıldığında bu dizin onun taramasına taşınmalı (açık kalan 6).

### T2.5 [ORTA] Kâr-zarar ürün bazlı 3 yıl p95 > 800 ms ve [DÜŞÜK] fiş araması indeksi → AÇIK NOT OLARAK TAŞINDI
Doğrulayıcının yönlendirmesiyle aynı: ikisi de ayrı paket (ön-toplam tablosu / aralık tavanı ve `(tenant_id, fis_id, tarih, cari_id)` kapsayan indeks, migration'lı). Tur 2'de bu uçların sorgu gövdesine dokunulmadı; `UrunBazli`'ya yalnız `Skip/ThenBy` ve bir `Distinct().Count()` (zaten bellekteki liste üzerinde) eklendi — ek SQL yok. **Kurul "yük kanıtı" maddesini kapatırken bu iki not açık taşınmalı.**

### T2.6 Testler ve mutasyonlar (tur 2)
Yeni testler: **OY08_Urun_bazli_kar_toplam_sayi_ve_ofset_sayfasi_sunucudan** (DB), **OYE01–OYE04** (`OlcekYukEkranTestleri.cs`, bUnit: sınır bandı + düğme gerçek istek + ekleme + son sayfa; sınırın altında bant yok; hata listeyi boşaltmaz; EN sözlük). Güçlendirilen: **OY07** (sabit sıra).

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `FullyQualifiedName~OlcekYuk` (OY01–OY08, OYE01–04, doğrulayıcının SNR01–07) | 23 | 23 | 0 |
| İlgili sınıflar (KarZarar, KarRapor, Maden*, RaporGorunum, RaporIki/RAPOR2, GorunumSubesi, SozlesmeKapsam, Arama, ORTAK1, UX1C3, UX4d, RaporYetki, Sozluk, P69) | 1022 | 1021 | 1 — `UX4dServisTestleri.M4_Kumulatif_mesaj_ILK_denemede_de_toplami_ve_esigi_verir` ("eşik 185.000" bekliyor, mesaj "200.000 TL"): MASAK-370 şeridinin yarım işi, bu paketin dosyalarıyla ilgisiz |

| Kod | Değişiklik (perl, tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| M2-1 | `AramaModulu` FisAra `.OrderByDescending(g => g.Tarih)` → `.OrderBy(…)` (doğrulayıcının MX1'i) | **OY07** (724 testlik Arama/Fis/OlcekYuk filtresinde tek kırmızı) | AYNI |
| M2-2 | `UrunBazli` `.Skip(ofset)` kaldırıldı | **OY08** | AYNI |
| M2-3 | `dahaFazlasiVar = toplam > ofset + count` → `> count` | **OY08** | AYNI |
| M2-4 | `toplamUrunSayisi` → `satirlar.Count` | **OY08** | AYNI |
| M2-5 | Razor `@if (urunDahaVar)` → `@if (false && urunDahaVar)` | **OYE01, OYE03, OYE04** | AYNI |
| M2-6 | Razor sonraki sayfa EKLEME yerine DEĞİŞTİRME (`urunler = [.. uy.Satirlar]`) | **OYE01** | AYNI |
| M2-7 | Razor hata yolunda `urunler = []` (hata→boş liste) | **OYE03** | AYNI |
| M2-8 | Razor `ofset={urunler.Count}` → `ofset=0` | **OYE01, OYE03** | AYNI |
| M2-9 | AramaModulu'na `db.Cariler…Where(c => c.Ad.Contains(q)).ToListAsync()` | denetim **TB-19 ✗ 27 > 26** (P-25-1 de ✗ 143>142). Eski t1 kuralıyla SINIRSIZ = 26 kalıyordu (kaçırıyordu) | AYNI |
| M2-10 | AramaModulu'na `Hareketler.GroupBy(h => h.CariId)…ToListAsync()` | denetim **TB-19 büyüyen ✗ 2 > 1**; P-25-1 ✓ 142 ve eski t1 kuralı (DOĞAL:toplama) KAÇIRIYORDU — yalnız yeni sayaç yakaladı | AYNI |
| M2-11 | `k6-cok-kiraci.js` `YUK_SIFRE || 'gecici'` | denetim **TB-19c ✗ 1** | AYNI |

Kayıtlar: `/private/tmp/kuyumcu-serit/OLCEK-YUK/mut2-M2-*.txt`, `mutd-M2-*.txt`; yedekler `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-YUK-t2/`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata**, 4 Uyarı — dördü de bu paketin DIŞINDA (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029, `SonKalanlarTestleri.cs:584/813` xUnit2013; tam yeniden derlemede görünen, başka paketlere ait mevcut uyarılar).

### T2.7 Denetim (tur 2)
`dotnet/denetim.sh` → 306 ✓, **yalnız P-30 ✗** (DEMO_INSAN_SURESI, kabulde istisna). Bu paketin sayaçları: ✓ Sınırsız rapor dökümü 20/20 · ✓ Sınırsız ToListAsync (Modules) 142/142 · ✓ TB-19 sınırsız 26/26 · ✓ TB-19 büyüyen toplama 1/1 (yeni) · ✓ TB-19c yük betiklerinde düz metin parola 0 (yeni) · ✓ Pages ModalPencere 47/55 · ✓ Pages/*.razor 49/49.

### T2.8 Canlı doğrulama (5166, geliştirme API'si, `kuyumcu_test_yuk` — migration ile kuruldu)
Test kiracısı `PAKETOLCEK-YUK-TEST Kuyumculuk` (`demo-tohumla.js --prova-kiraci`) + uçlardan 70 ürün + 65 satış (`HareketFabrikasi` yolu, doğrudan INSERT yok).

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /raporlar/kar-zarar/urun?baslangic=2026-10-01&bitis=2026-10-07` | 200 · 50 satır · toplam 71 · dahaFazlasiVar true | 200 · urunSayisi 50 · limit 50 · ofset 0 · **toplamUrunSayisi 71** · dahaFazlasiVar **true** |
| aynı + `&ofset=50` | 200 · 21 satır · dahaFazlasiVar false · örtüşme 0 | 200 · urunSayisi 21 · ofset 50 · toplam 71 · **false** · örtüşme **0** · birleşik 71 farklı ürün |
| aynı + `&ofset=-1` | 400, Türkçe alan mesajı | 400 · `ofset 0 ile 100000 arasında olmalıdır` (ilk denemede `2.147.483.647` yazıyordu → aralık 100.000'e indirildi, yeniden derlenip doğrulandı) |
| aynı + `&ofset=500` | 200 · boş · false | 200 · urunSayisi 0 · dahaFazlasiVar false |
| **SQL** `COUNT(DISTINCT urun_id)` SATIS/SATIS_IADE, 01.10 00:00 (TR) sonrası, kiracı `01M4BC7SDJZY0H93J6AH6NN979` | 71 | **71** (74 satış satırı) |

**Gerçek tarayıcı ölçümü** (başsız Chrome, `betikler/tur/cdp.mjs`, parolasız oturum `inceleme-vekili.py`, `sikisma-olc.js` `__olcTam`; rota `/raporlar/kar-zarar` → Ürünler sekmesi; önce = ilk sayfa, sonra = "Daha fazla göster" tıklandıktan sonra):

| Mod · tema · genişlik | Satır önce→sonra | Bant / düğme önce | Bant / düğme sonra | K1 | K3 taşan/kırık | K5 TRY | K7 | K9 | K10 | K11 | K12 | yatay | K2 % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASİT · açık · 1280 | 50→71 | görünür / 164 px | yok / yok, "71 ürünün tamamı" | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| BASİT · açık · 375 | 50→71 | görünür / 164 px | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| BASİT · koyu · 1280 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| BASİT · koyu · 375 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| PRO · açık · 1280 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| PRO · açık · 375 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| PRO · koyu · 1280 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| PRO · koyu · 375 | 50→71 | görünür | yok / yok | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |

K8 jargon 0, K6 seçici dar 0 (16 ölçümün hepsinde). K4 (sayfa boyu) bu tablo raporunda doğal olarak uzun (1 bölüm; 375'te ürün kartları alt alta) — yeni bir bölüm eklenmedi. Ekran görüntüleri (Read ile bakıldı): `/private/tmp/kuyumcu-serit/OLCEK-YUK/t2/ekran/{once,sonra}__{BASIT,PRO}_{aydinlik,karanlik}_{1280,375}.png` — bant amber yüzeyde okunur, düğme bandın altında, koyu temada belirteç renkleri doğru; ölçüm JSON'ları aynı dizinde `olcum__*.json`.

### T2.9 Temizlik, disk, binlog (tur 2)
- API 5166 ve vekil 5167 durduruldu (`lsof` ikisi de boş); Chrome süreçleri kapandı (`pgrep remote-debugging-port=95` boş).
- `kuyumcu_test_yuk` (3,7 MB) `sql_log_bin=0` ile DROP edildi; `information_schema` = 0. Test kiracısı yalnız bu DB'deydi, onunla silindi. Vekil fikstürü `printf '' >` ile boşaltıldı.
- `kuyumcu_net.tenants`: tur 2 başında **15**, sonunda **16** — fark **bu paketin değil**: `PAKETMASAK370-TEST` (oluşturulma 14:31, MASAK-370 şeridi). Bu pakete ait kiracı `kuyumcu_net`'te hiç açılmadı. QA-TEST kiracılarına dokunulmadı.
- Boş disk: tur 2 başında 12 GiB (`df`), sonunda 13 GiB; 8 GB sınırına inilmedi. Binlog tur 2 başında 12,39 GB (tur 1 sonu 11,48 GB → aradaki artış başka şeritlerin); bu turun DB yazımları test DB'sindeki birkaç yüz satır + şema kurulumu.
- `kuyumcu_test_yuk_t` (test süreci DB'si) testler tarafından süreç başına yeniden kurulur.

### T2.10 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` — `UrunBazli` ofset + toplamUrunSayisi + kararlı sıra; uç `ofset` parametresi.
- `dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` — `UrunYanit` alanları.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` — 3 anahtar TR+EN.
- `dotnet/Kuyumcu.Web/Pages/KarZarar.razor` — sınır bandı, "Daha fazla göster", hata bandı.
- `dotnet/Kuyumcu.Tests/OlcekYukTestleri.cs` — OY07 sıra, OY08 yeni.
- `dotnet/Kuyumcu.Tests/OlcekYukEkranTestleri.cs` — YENİ (OYE01–04).
- `dotnet/denetim.sh` — TB-19 tek kaynak + büyüyen toplama + TB-19c.
- `dokumanlar/09-uygulama-kayitlari/betikler/yuk/tolist-siniflandir.pl` — inceltilmiş kural, `--say` kipi.
- `dokumanlar/09-uygulama-kayitlari/betikler/yuk/{k6-cok-kiraci.js,k6-agir-rapor.js,yuk-kiraci-kur.mjs}` — YUK_SIFRE zorunlu.
- `dokumanlar/09-uygulama-kayitlari/ham/OLCEK-YUK-tolist-siniflandirma.tsv` — yeniden üretildi.
- Bu rapor.
`git status --porcelain | grep -v -E "/(bin|obj)/"`: bu dosyaların çoğu izlenmeyen dizinlerde (`?? dotnet/Kuyumcu.Core/I18n/…`, `?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor`, `?? dotnet/denetim.sh`); `dotnet/denetim.sh.mutbak` (18.09, 0 bayt) ve `Servisler/KarZararModelleri.cs` (03.10) bu pakete ait değil.

### T2.11 DoD sözleşmesi (tur 2)
Yeni varlık/kayıt türü yok, migration yok. Değişen uç `GET /raporlar/kar-zarar/urun` salt okunur rapordur (kayıtlar türetilmiştir; düzeltme kaynağı olan işlemin ekranından ters kayıtla — mevcut `RaporTuretilmisGerekce`); `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` değişmedi. Yeni düğme `#kz-urun-daha` gerçek istek atar (OYE01), istek sürerken disabled.

### T2.12 Tur 2 sonrası açık kalanlar (sahipleriyle)
1. **KALDI (ayrı paket, Taşkın + Bertan):** `/raporlar/kar-zarar/urun` büyük kiracıda 3 yıllık aralıkta p95 1,37–1,54 s. Ön-toplam tablosu ya da aralık tavanı + UX-S29 uyarısı. Kurul "yük kanıtı" maddesini kapatırken açık not.
2. Stok yaşlandırma 50.000 ürünlü kiracıda p99 ~1,2 s (izleme).
3. Fiş araması `(tenant_id, fis_id, tarih, cari_id)` kapsayan indeks (migration'lı ayrı paket); ağır yükte p95 505 ms, eşiğin altında.
4. TB-19 sınıflandırıcısı hâlâ sözdizimsel: `ToList()` sonrası bellek içi toplama ve çağıranın verdiği kimlik kümesinin büyüklüğü ölçülmez (CariBakiyeServisi.EmanetEtkileri gibi). Gerçek kanıt yük ölçümüdür.
5. Tam süitte bu paketin dışındaki kırmızı(lar): § T2.13'te adıyla — sahibi MASAK-370 şeridi.
6. SIR-2 paketi açıldığında `betikler/yuk/` dizini sır taramasına alınmalı (bugün TB-19c taşıyor).
7. Mevzuat: bu turda mevzuat hükmü yok; kurucuya/mali müşavire not yok.

### T2.13 Tam süit (tur 2, son kodla, bir kez)
`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pOLCEK-YUK-t2` → trx `_Ramazan-MacBook-Air_2026-10-07_17_36_40.trx`: **toplam 10.354 · geçti 10.350 · başarısız 2 · atlandı 2** (tur 1: 10.325 / 0 kırmızı; +29 test = bu turun 5'i + başka şeritlerin).

| Kırmızı | Kök neden (trx mesajı) | Sahibi | Bu paketin ürünü mü? |
|---|---|---|---|
| `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` | `YASAL_PARAM_AYRISMA — MASAK_KIMLIK_ESIGI_TL: yapılandırma 370000.0 ≠ tablo 185000`. `Kuyumcu.Api/appsettings.json:53` `KimlikEsigi: 370000.0`, dosya 17:27'de MASAK-370 şeridince değişti | MASAK-370 şeridi | **Hayır** (bu paket appsettings'e ve yasal parametrelere dokunmadı) |
| `KesifP5EkranTestleri.SATIS4c_arama_satirinda_FIYAT_ve_STOK_iki_toplu_istek_satir_basina_istek_YOK(mod: "BASIT")` | bUnit `WaitForAssertion` zaman aşımı (yük altında). Tek başına 3 kez koşuldu: **3/3 yeşil** → kararsız; eşzamanlı MASAK-370 derlemeleri makineyi yüklüyordu | Satış ekranı (KESIF-P5), kararsız test | **Hayır** (bu paket Satış ekranına dokunmadı) |

Filtreli koşumda görülen `UX4dServisTestleri.M4_Kumulatif…` tam süitte YEŞİL çıktı (MASAK-370 şeridi arada kendi değişikliğini ilerletti). **Bu paketin yeni kırmızısı: 0** (PARALEL ŞERİT KAPISI madde 7 sağlandı).

---

# TUR 1 METNİ (2026-10-07 15:20–16:20; değişmeden korunur, tur 2'de değişen hükümler yukarıda)

**Tarih:** 2026-10-07 15:20–16:20 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** yük API 5166 (Release yayın kopyası), yük DB `kuyumcu_test_yuk`, test DB `kuyumcu_test_yuk_t`. 5198/5199/5200'e, tarayıcı bölmesine ve `kuyumcu_net` satırlarına dokunulmadı. Eşzamanlı şerit yoktu.
**Kaynak:** `HAZIRLIK-KURULU-5.md` madde 13 (YÜKSEK). **COMMIT YOK.** Dosya silinmedi.

## 0. Devralma (03.10'da düşen uygulayıcının bıraktıkları)

| Ağaçta bulunan | Değerlendirme | Ne yapıldı |
|---|---|---|
| `KarZararServisi.UrunBazli`: ürün adları yalnız sayfadaki ürünler için (`sayfaUrunIdleri.Contains`) | Doğru. Sıralama/limit addan bağımsız, sözleşme aynı | Korundu, mutasyonla kanıtlandı (M1-1) |
| `RaporModulu.VadeTakibi`: cari kartları yalnız borçlular için | Doğru | Korundu, M1-2 ve M1-3 |
| `AramaModulu.FisAra`: cari ad sözlüğü yalnız fişlerdeki cariler için | Doğru ama **yetersiz**. Kiracının TÜM fişleri yine gruplanıp belleğe alınıyordu, eşleşme ve Skip/Take bellekte yapılıyordu (aşağıda ölçüldü) | **Yeniden yazıldı**: eşleşme ve sayfalama SQL'e taşındı (bkz. § 3) |
| `Kuyumcu.Tests/OlcekYukTestleri.cs` (6 test) | **3'ü KIRMIZIYDI.** Hiç koşulmamış. OY01/OY03: EF tek elemanlı kümeyi `IN (` değil `= 'x'` yazıyor. OY05: `J()` PascalCase serileştiriyor, `"sonuclar"` anahtarı yok | Düzeltildi: `KimlikleDaraltilmis` (regex, `IN (` ya da `= `); OY05 tipli yanıtla. Yeni test **OY07** eklendi. 7/7 yeşil |
| `denetim.sh` TB-19 bölümü (tavan 26) + P-25-1 tavanı 145→142 + rapor tavanı 22→20 | Sayaç doğru çalışıyor. Ancak andığı `ham/OLCEK-YUK-tolist-siniflandirma.tsv` YOKTU | Sınıflandırıcı betiğe bağlandı (`betikler/yuk/tolist-siniflandir.pl`), döküm yeniden üretildi. M1-8 ile kanıtlandı |
| `betikler/yuk/` (k6-cok-kiraci.js, k6-agir-rapor.js, yuk-kiraci-kur.mjs, yuk-kos.sh, yuk-tohum.sql) | Kullanılabilir. `kiracilar.json` /private/tmp ile birlikte kaybolmuştu | Yeniden kullanıldı. Kiracı dosyasını DB'den kuran `yuk-kiraci-dosyasi.py` ve yayın başlatıcı `yuk-api-baslat.sh` eklendi |
| `kuyumcu_test_yuk` (03.10'dan kalma) | **Kullanılabilirdi:** 3.149 MB (sınır 4 GB). 100 kiracı, 1.103.709 hareket, 1.085.747 kasa hareketi, 335.437 ürün, 47.314 cari. Şema güncel (86 migration, son `20261001093846_Yasal4GecerliPusulaKilidi`, kodla aynı) | Yeniden tohumlanmadı, kullanıldı. İş sonunda DROP edildi |

**Spesifikasyon sapması:** madde 13 "Take'siz 209 ToListAsync" diyor. Bugünkü ağaçta Migrations hariç **202** Take'siz deyim var (341 toplam, 139 sayfalı; FisAra sonrası 140). Fark, 03.10'dan beri yazılan kod. Sınıflandırma bu güncel sayı üzerinden yapıldı.

## 1. Ortam ve yöntem

- **Yayın:** `dotnet publish -c Release` → `/private/tmp/kuyumcu-serit/OLCEK-YUK/yayin`, 5166. Başlatıcı `betikler/yuk/yuk-api-baslat.sh`.
- **Ortam: Staging.** `Program.cs` yalnız `IsDevelopment()` ile dallanıyor, yani istek yolu Production ile birebir aynı. Production'da açılış denetimi yerel root parolasını kara listede bulup süreci durduruyor (fail-closed, ölçüldü). MySQL'de kullanıcı ya da yetki DEĞİŞTİRMEMEK için root ile Staging seçildi. `Uretim:TersVekilArkasinda=true`.
- **Sırlar:** JWT ve alan şifreleme anahtarı `~/.kuyumcu`'dan değişkene alındı. Sistem anahtarı rastgele üretildi (chmod 600). Hiçbiri basılmadı.
- **Veri:** büyük kiracı K001–K003 (50.000 ürün · 5.000 cari · 180.000 satış hareketi, 1.095 güne yayılı). Olağan kiracı K004–K100 (2.000 ürün · 300 cari · 6.000 hareket).
- **Profil (k6-cok-kiraci.js):** kiracı başına **2 eşzamanlı personel** (SAHİP + MÜDÜR, ayrı hesaplar). Döngü: panel · kur · ürün arama · fiyat teklifi · satış fişi (2 turda 1) · cari arama · kasa hareketleri · rapor (4 turda 1: günlük/aylık/stok yaşlandırma/stok değeri/işlem defteri). İstek arası düşünme süresi 2–5 sn. Her kiracı kendi IP'sinden gelir (X-Forwarded-For). **Oran sınırı gevşetilmedi.**
- **Koşum:** 60 sn rampa, ardından **5 dk kararlı durum**. Eşikler yalnız kararlı fazda ölçüldü. Kaynak örneklemesi 5 sn'de bir: `ps` (API süreci) ve `docker stats` (MySQL konteyneri). Yavaş sorgu için `performance_schema` özet farkı (önce/sonra).
- **Makine:** 10 çekirdek, 16 GB. Docker 6 CPU, 8 GB. CPU % = tek çekirdeğe göre (100% = 1 çekirdek).

## 2. Sonuç tablosu (KABUL)

| Kiracı | VU | İstek (rps) | p50 ms | p95 ms | p99 ms | Hata % | 429 | 5xx | API CPU ort/tepe | API RSS tepe | MySQL CPU ort/tepe | MySQL bellek tepe | DB boyutu |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 10 | 20 | 1.904 (5,0) | 27,3 | **102,0** | 161,7 | **0** | **0** | 0 | %10 / %26 | 365 MB | %11 / %71 | 725 MiB | 3.149 MB |
| 50 | 100 | 9.775 (25,7) | 20,1 | **47,0** | 104,1 | **0** | **0** | 0 | %34 / %75 | 288 MB | %22 / %108 | 810 MiB | 3.149 MB |
| 100 | 200 | 19.508 (51,3) | 13,8 | **32,8** | 66,5 | **0** | **0** | 0 | %55 / %104 | 270 MB | %26 / %64 | 868 MiB | 3.149 MB |

**Eşikler: p95 < 800 ms ✓ · failed < %1 ✓ · normal kullanımda 429 = 0 ✓, üç koşumda da.** k6 çıkış kodu 0, eşik ihlali yok. 10 kiracılı koşum ilk koşumdur ve soğuk önbellekle başladı; bu yüzden gecikmesi 50 ve 100 kiracıdan yüksek. 100 kiracıda API ortalama 0,55 çekirdek kullandı.

**Uç kırılımı (100 kiracı, ms):** giriş p95 242 (BCrypt) · panel 17 · kur 16 · ürün arama 33 (p99 159) · fiyat teklifi 18 · satış fişi 38 · cari arama 35 · kasa hareketleri 23 · rapor 80 (p99 488) · stok yaşlandırma p95 90, **p99 1.226** (büyük kiracı) · stok değeri p99 488.

**En pahalı SQL (100 kiracı, toplam süre):** ürün listesi/arama SELECT, 2.875 kez, ort. 13 ms · cari has bakiyesi toplamı, ort. 4,8 ms · kasa hareketleri, ort. 3 ms. 100 ms'yi aşan tezgâh sorgusu yok.

**429 / S-3:** genel sınır kullanıcı başına 300/dk (kayan pencere). Tezgâh profili kişi başına ~15 istek/dk üretiyor, yani 429 = 0. Önceki ölçümdeki %11,2 tek kiracı, 10 VU ve düşünme süresiz bir profilden geliyordu. **Oran sınırı değiştirilmedi.** İstemcide Retry-After'a uyan üstel geri çekilme zaten var (`ApiIstemci.cs`, P-07 / ISTEK-1, `GeriCekilmeDenemeTavani = 3`). Değişiklik gerekmedi.

### 2b. Ağır rapor yükü (k6-agir-rapor.js · büyük kiracı K001–K003 · 6 VU · 2 dk)

| Uç | Önce (03.10 kodu + bu oturumun ilk koşumu) p50/p95/p99 | Sonra p50/p95/p99 | Eşik |
|---|---|---|---|
| Vade takibi | 27 / 45 / 49 | 26 / 41 / 70 | ✓ |
| Fiş araması (K001'de 180.000 fiş) | Tek istek 466–530 ms (eski FisAra, fişli veride) | 34 / **505** / 701 | ✓ |
| Kâr-zarar ürün bazlı, **3 yıl** | 957 / **1.543** / 1.676 | 982 / **1.369** / 2.188 | **✗ KALDI** |

Hata 0, 429 0. İlk ağır koşumda klon hareketlerin `fis_id`'si boştu ve fiş araması boş veride ölçülüyordu (33 ms). Gerçek fiş araması için K001'in 180.000 klon satırına yük DB'sinde fiş kimliği verildi (`SET sql_log_bin=0`, yalnız `kuyumcu_test_yuk`).

**KALDI · kâr-zarar ürün bazlı, 3 yıllık aralık, büyük kiracı.** Kök neden (ölçüldü): uç dönemdeki TÜM satış satırlarını çekiyor (`KarZararServisi.cs:673`, 180.000 satır). SQL tek başına ort. 736 ms (`performance_schema`). Maliyet-bilinen ve has çevrimi kuralları satır başına bellekte hesaplanıyor. Tek istekte aralığa göre süre: 30 gün 27 ms · 90 gün 61 ms · 365 gün 414 ms · 1.095 gün 555 ms. Yani gecikme satır sayısıyla doğrusal büyüyor. Normal tezgâh profili bu ucu çağırmıyor. Finansal kâr kuralını SQL'e taşımak bu pakette yapılmadı: kural karmaşık ve kuruş kuralları var, iki turda güvenle yeniden yazılamaz. Öneri ayrı bir paket: gün × ürün ön-toplam tablosu, ya da tarih aralığı tavanı + uyarı (UX-S29).

**Gözlem · stok yaşlandırma, büyük kiracı:** p99 1,0–1,3 s (50.000 ürünlü kiracı). Toplam p95'i bozmuyor. Ayrı izlenmeli.

## 3. Kod değişikliği · fiş araması (TB-19, ölçülmüş darboğaz)

`Modules/Arama/AramaModulu.cs` `FisAra`: eski hâl kiracının tüm fişlerini `GroupBy` ile belleğe alıyor, eşleşmeyi ve sayfalamayı bellekte yapıyordu. TB-19 sayacı bunu "DOĞAL:toplama" saydığı için yakalamadı. Yeni hâl:
1. Adı eşleşen cariler SQL alt sorgusu olarak çalışır. Diğer aramalarla aynı kural: `Ad.Contains`.
2. Eşleşen satırlar gruplanır. `ORDER BY MAX(tarih) DESC, fis_id` ve `LIMIT/OFFSET` SQL'de uygulanır.
3. Kalem sayısı ve tutar yalnız sayfadaki fişler için hesaplanır, fişin TÜM satırlarından. Anlam değişmedi.
4. Cari adı yalnız sayfadaki cariler için okunur.

Yanıt sözleşmesi (`AramaYaniti`/`AramaSonucu`) aynı kaldı. "Daha var" bilgisi hâlâ bir fazla kayıt istenerek, sayılarak veriliyor. Tek istek 500 → 310–390 ms. Ağır yükte p95 505 ms.
**Kalan sınır:** `fis_id LIKE '%q%'` indeks kullanamaz ve kiracının fişli satırları taranır. `(tenant_id, fis_id, tarih, cari_id)` kapsayan indeks ek kazanç sağlar. Şema değişikliği olduğu için bu pakete alınmadı (açık kalan).

**Migration:** gerekmedi.

## 4. TB-19 sınırsız liste sınıflandırması

Döküm: `dokumanlar/09-uygulama-kayitlari/ham/OLCEK-YUK-tolist-siniflandirma.tsv` (dosya · satır · sınıf · deyim). Betik: `betikler/yuk/tolist-siniflandir.pl`. Kurallar `denetim.sh` TB-19 sayacıyla aynı.

| Sınıf | Adet |
|---|---|
| SAYFALI (.Take/.Skip) | 140 |
| DOĞAL: toplama (GroupBy) | 54 |
| DOĞAL: üst kayda bağlı | 48 |
| DOĞAL: kimlik kümesi (Contains) | 46 |
| DOĞAL: yapılandırma (şube/kasa/rol…) | 28 |
| **GERÇEKTEN SINIRSIZ** | **26** (tavan 26, gevşetilmedi) |

Sınırsız 26'nın yük açısından önemlileri:
- `KarZararServisi` 306, 673 ve 680: dönem satırları; yukarıda KALDI.
- `Iceaktarim/Kaynaklar.cs:180`: tüm cariler, içe aktarım anı.
- `FiyatModulu.cs:911` ve `SayimModulu.cs:74`: ürün listeleri.
- `Hurda`/`Gider`/`SatisRaporlari`: storno kimlik listeleri.

Kalanlar yapılandırma benzeri küçük kümeler.

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| OlcekYukTestleri (önce, devralınan) | 6 | 3 | **3** | 0 |
| OlcekYukTestleri (sonra) | 7 | 7 | 0 | 0 |
| Arama/fiş/ekran sınıfları (12 sınıf: AramaHttp, ZoBelgeAramaFis, ZOISLEMAArama, KabukAramaEkran, SeciciUzakKip …) | 252 | 252 | 0 | 0 |
| **Tam süit** (trx `/private/tmp/kuyumcu-serit/trx-pOLCEK-YUK/…16_07_36.trx`) | **10.325** | **10.323** | **0** | 2 |

Yeni test: **OY07_Fis_aramasi_eslesme_ve_sayfalama_sunucuda_yapilir**. Düzeltilen testler: OY01, OY03 ve OY05. Bu paket yeni kırmızı üretmedi: tam süitte kırmızı 0.

## 6. Mutasyon kanıtı (kaynak `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-YUK/` → perl tek değişiklik → filtreli test → geri kopya → cmp)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1-1 | KarZarar ürün adı sorgusundan `sayfaUrunIdleri.Contains` kaldırıldı | OY01 | AYNI |
| M1-2 | Vade takibi kart sorgusundan `bakiyeliler.Contains` kaldırıldı | OY03 | AYNI |
| M1-3 | Vade takibi "borçlu yoksa cari tablosuna gitme" kısa devresi kırıldı | OY04 | AYNI |
| M1-4 | FisAra ad sözlüğünden `fisCarileri.Contains` kaldırıldı | OY05, OY07 | AYNI |
| M1-5 | FisAra SQL sayfasından `.Skip(atla)` kaldırıldı | OY07 | AYNI |
| M1-6 | FisAra cari-adı eşleşme alt sorgusu `false` yapıldı | OY05, OY07 | AYNI |
| M1-7 | k6 eşiği `p(95)<800` → `<900` | OY06 | AYNI |
| M1-8 | AramaModulu'na sınırsız `Cariler…ToListAsync()` eklendi → `denetim.sh` | TB-19 ✗ 27 > 26 ve P-25-1 ✗ 143 > 142 | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata, 0 Uyarı.**

## 7. Denetim

`dotnet/denetim.sh` → yalnız **P-30 ✗** (DEMO_INSAN_SURESI; kronometreli insan ölçümü, bu paketin dışında, kabulde istisna). Bu paketin sayaçları:
- ✓ Sınırsız rapor dökümü 20 (üst sınır 20)
- ✓ Sınırsız ToListAsync (Modules) 142 (tavan 142)
- ✓ TB-19 gerçekten sınırsız 26 (tavan 26)
- ✓ Sınırsız okuma beyaz listesi 15

## 8. Canlı doğrulama (5166, Release, yük DB'si, K001)

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| `GET /arama?q=OYF0010000123&tur=FIS` | 200, tek fiş, 1 kalem 6.300 ₺, carisiz "—" | 200 · sayi 1 · "— · 02.06.2026" · "1 kalem · 6.300 ₺" | `COUNT=1, SUM=6300.00, cari NULL` |
| `GET /arama?q=Yük Müşteri 001-17&tur=FIS&limit=5&ofset=0` ve `ofset=5` | 200, 5+5 kayıt, örtüşme 0, dahaVar=true | 200 · 5/5 · örtüşme 0 · dahaVar true/true; ilk kayıt "Yük Müşteri 001-1795" | eşleşen fiş sayısı **792** (>10 → dahaVar doğru); `OYF0010066795` carisi "Yük Müşteri 001-1795" |
| `GET /arama?q=OY&tur=FIS&limit=0` | 200, limit en az 1'e kıstırılır | 200 · limit 1 · dahaVar true | — |
| `GET /raporlar/vade-takibi?ufukGun=30` | 200 | 200 · cariler [] (klon satırlar bakiye üretmez) | — |
| `GET /raporlar/kar-zarar/urun?…&limit=3` | 200, ad sayfadaki ürünle eşleşir | 200 · OYU0010031787 / OY001-31787 / "Zincir Kolye 001-2 #31787" | `urunler` satırı birebir aynı |
| Duman (satış fişi POST) | 200, fiş no | 200 · fisId döndü | — |

**Gerçek tarayıcı kuralı:** razor, css, bileşen, sözlük ve `ApiIstemci` dosyalarına **dokunulmadı**. Ekran yüzeyi değişmedi, yanıt sözleşmesi aynı. Arama seçicisini kullanan ekran testleri (KabukAramaEkran, SeciciUzakKip, ZOISLEMAEkran, FaturaEkran) yeşil. Bu yüzden tarayıcı ölçümü yapılmadı. Dokunulan sayfalar: **yok.**

## 9. Temizlik, disk ve binlog

- API 5166 durduruldu (`lsof` boş).
- `kuyumcu_test_yuk` (3.149 MB) ve `kuyumcu_test_yuk_t` DROP edildi (`sql_log_bin=0`). `information_schema`'da `kuyumcu_test_yuk%` = 0.
- Test kiracıları yalnız yük DB'sindeydi ve DB ile birlikte silindi. **`kuyumcu_net.tenants` = 15 → 15.** QA-TEST kiracılarına dokunulmadı.
- **Boş disk:** ölçüm başında 16,79 GB. En düşük 12,63 GB, DROP öncesi; 8 GB sınırına inilmedi. İş sonunda 16,03 GB. Geçici dosyalar `/private/tmp/kuyumcu-serit/OLCEK-YUK` 132 MB ve trx 14 MB; dosya silme yasak olduğu için bırakıldı.
- **Binlog:** 11,434 GB → 11,483 GB (+~49 MB: yük altındaki gerçek satış fişleri ve tam süit). Toplu UPDATE ve DROP binlog'a yazılmadı.

## 10. DoD sözleşmesi

Yeni varlık ya da kayıt türü yok. Yeni uç ve migration yok. Değişen uç `GET /arama?tur=FIS` salt okunur bir arama ucudur, PATCH/DELETE gerekmez. `[ModulYetkisi]`/rol denetimi değişmedi (`yetki.ErisebilirMi` yolu aynı).

## 11. Değişen dosyalar (bu oturum)

- `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs`: FisAra SQL'e taşındı.
- `dotnet/Kuyumcu.Tests/OlcekYukTestleri.cs`: OY01/OY03/OY05 düzeltildi, OY07 eklendi.
- `dokumanlar/09-uygulama-kayitlari/betikler/yuk/yuk-api-baslat.sh` (yeni)
- `dokumanlar/09-uygulama-kayitlari/betikler/yuk/yuk-kiraci-dosyasi.py` (yeni)
- `dokumanlar/09-uygulama-kayitlari/betikler/yuk/tolist-siniflandir.pl` (yeni)
- `dokumanlar/09-uygulama-kayitlari/ham/OLCEK-YUK-tolist-siniflandirma.tsv` (yeni)
- Bu rapor.

Devralınan ve doğrulanan: `RaporModulu.cs` (VadeTakibi), `KarZararServisi.cs` (UrunBazli), `denetim.sh` (TB-19 bölümü), `betikler/yuk/*` (k6 ve tohum betikleri). `git status --porcelain | grep -v -E "/(bin|obj)/"` bu dosyaları M ya da ?? (izlenmeyen dizin) olarak gösteriyor.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| 10/50/100 kiracı, her biri ≥5 dk kararlı | ✓ | § 2 tablosu, `kosum-k{10,50,100}/k6-ozet.json` |
| p95 < 800 · failed < %1 · 429 = 0 (normal kullanım) | ✓ (3/3) | p95 102 / 47 / 33 ms; hata 0; 429 0 |
| CPU/bellek (API + MySQL), yavaş sorgu | ✓ | `kaynak.txt`, `sql-en-pahali.txt` |
| S-3 ayarı + istemci 429 geri çekilmesi | Gerekmedi | 429 = 0. İstemci geri çekilmesi P-07'den mevcut |
| Sınırsız liste sınıflandırması + yük altındaki ucu düzeltme | ✓ (FisAra) / ✗ (KarZarar 3 yıl) | § 2b, § 3, § 4 |
| TB-19 sayacı (gevşetme yok) | ✓ 26/26 | denetim + M1-8 |
| Test + mutasyon + build 0/0 + denetim (P-30 hariç) + tam süit 0 yeni kırmızı | ✓ | § 5–7 |
| Test DB silindi, disk önce/sonra | ✓ | § 9 |
| Ağır rapor eşiği (kâr-zarar ürün, 3 yıl) | **✗ KALDI** | p95 1.369 ms; kök neden § 2b |

## 13. Açık kalanlar

> **t2:** bu listenin güncel hâli § T2.12'dedir (madde 4 inceltildi; tasarım hakemi KRİTİK, YUK_SIFRE ve FisAra sıra testi kapandı).


1. **KALDI:** `/raporlar/kar-zarar/urun` büyük kiracıda 3 yıllık aralıkta p95 1,4–1,5 s, p99 2,2 s. Çözüm ayrı paket: ön-toplam tablosu ya da aralık tavanı. Sahibi: Taşkın + Bertan.
2. Stok yaşlandırma 50.000 ürünlü kiracıda p99 ~1,2 s.
3. Fiş araması için `(tenant_id, fis_id, tarih, cari_id)` kapsayan indeks önerisi (migration, ayrı paket).
4. TB-19 sayacı GroupBy'lı sorguları "doğal" sayıyor. Eski FisAra gibi tüm fişleri gruplayan sınırsız toplama yakalanmıyor; sayaç kuralı inceltilmeli.
5. Mevzuat: bu pakette mevzuat hükmü yok. Kurucuya/mali müşavire not yok.
