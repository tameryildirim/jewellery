# PAKET P-75: Vade takibi, geçersiz ufukta kartlar kaybolmuyor ve düğmelerde aria-pressed var

**Şerit:** SERİT C (tur 8 triyajı) · **Rol:** Ertuğ (frontend). Backend değişikliği gerekmedi. · **Tarih:** 2026-09-24
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı (§ 9). Tam süitteki 10 kırmızının hiçbiri bu pakete ait değil (§ 6).

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (TRIYAJ-TUR8 § P-75 + ek koşullar) | Kodda gördüğüm | Sonuç |
|---|---|---|
| Geçersiz süzgeçte TÜM kartlar kayboluyor | `Yukle()` içinde `catch (ApiHata) { yanit = null; … }` vardı. Ayrıca `yukleniyor` true iken kartların yerine `<Yukleniyor/>` çiziliyordu, yani her yenilemede kartlar kayboluyordu. | DOĞRU |
| 7/30/60 düğmeleri basılı durumu ekran okuyucuya bildirmiyor | Yalnız `secili` sınıfı vardı; `aria-pressed` ve `type="button"` yoktu. | DOĞRU |
| "P-74'ten sonra -1 sunucuya gitmez" | **SAPMA:** P-74 ağaca inmemiş. `SayisalAlan.Degisti` `EnAz/EnCok`'u denetlemiyor ve -1'i `DegerChanged(-1)` ile iletiyor. Sayfa -1'i sunucuya soruyordu. Sunucu 400 veriyordu (`ufukGun < 0`), catch de raporu siliyordu. Boş kutu `ufukGun = 0` sayılıp sessizce 0 günle sorgulanıyordu. Bu sessiz bir varsayımdı. | Uyarlandı (aşağıda) |
| Sunucu üst sınırı | `RaporModulu.VadeTakibi` yalnız `< 0` reddediyor, 366 → 200 veriyor. Ekranın sınırı `EnCok=365`. | Not edildi. Backend dokunulmadı; dosya sınırı dışında. |

**Uyarlama gerekçesi:** Hedef, kullanıcının geçersiz denemede verisini kaybetmiş sanmaması. P-74 ortak `Sayi` bileşenine iniyor ve o bileşen bu şeridin dosya sınırı dışında. Bu yüzden P-75 sayfa düzeyinde kendi kapısını kurdu. Boş, aralık dışı (0–365) ve kesirli değer sunucuya **gitmez**; son geçerli rapor ve `ufukGun` korunur, tek bir uyarı bandı basılır. Aralık içindeki değeri sunucu reddederse (4xx/5xx) son rapor yine ekranda kalır ve bant sunucunun mesajını yazar. P-74 indiğinde `Sayi` bileşeni aralık dışını kendisi durdurursa sayfa kapısı zararsız bir ikinci savunma olarak kalır. Sınırlar tek yerde durur: `UfukEnAz`/`UfukEnCok` sabitleri hem `Sayi`'nin `EnAz/EnCok`'unu hem uyarı metnindeki `{0}/{1}`'i besler.

Mevzuat: bu paket mevzuat hükmü içermiyor. Araştırma gerekmedi.

## 2. Yapılanlar

**`Pages/VadeTakibi.razor`**
- Hızlı düğmeler tek diziden (`HizliUfuklar = [7,30,60]`) üretiliyor. `id="vade-ufuk-{g}"` aynı kimlikleri verir. Hem `secili` sınıfı hem `aria-pressed="true|false"` tek kaynaktan türetiliyor: `Basili(g) => ufukGun == g`. Düğmelere `type="button"` eklendi.
- Durum ikiye ayrıldı:
  - `girdi`: kutuda yazan değer, geçersiz olabilir.
  - `ufukGun`: son geçerli ufuk. Hem sunucuya sorulan ufuk hem basılı düğmenin tek kaynağı budur.
- Girdiden 30 yazılınca `ufukGun = 30` olur ve `#vade-ufuk-30` basılı görünür. Düğmeye basılınca `girdi` ve `ufukGun` birlikte değişir.
- `GirdiDegisti`: boş, aralık dışı ve kesirli değer sunucuya gitmez. `ufukGun` ve rapor değişmez. `uyari` doldurulur.
- `Yukle()` hata durumunda rapor ekrandaysa onu **korur**, `uyari` bandına sunucu mesajını yazar ve `ufukGun`'u ekrandaki raporun ufkuna (`yanit.UfukGun`) geri çeker. Böylece basılı düğme ekrandaki rakamların ufkunu söyler. Hiç rapor yoksa eskisi gibi `HataBandi` basılır (UX-S28).
- Uyarı bandı `<div class="bant bant-uyari" role="alert" id="vade-uyari">` tektir. İçinde neden ve `#vade-uyari-son-gecerli` yazar: "Ekrandaki rakamlar son geçerli rapora aittir (N günlük ufuk)."
- `<Yukleniyor/>` yalnız ilk yüklemede (`yanit is null`) çizilir. Yenilemede kartlar yanıp sönmez.
- `#vade-ufuk`, `#vade-ufuk-7/30/60` ve `#vade-ufuk-aciklama` kimlikleri korundu.

**`Kuyumcu.Core/I18n/Sozlukler.VadeUfuk.cs` (YENİ).** Hub'a dokunulmadı. Konumsal `VadeUfukMetinleri` record'u var; TR ve EN metinleri aynı kurucudan geçer, eksik anahtar derleme hatası verir. Anahtarlar: `UfukBos` · `UfukAralikDisi` · `UfukTamSayiDegil` · `YuklemeBasarisiz` · `SonGecerliRapor`.

**`Kuyumcu.Tests/VadeEkranTestleri.cs`**
- Sahte API, sorulan ufka göre yanıt veriyor (`ufukGun` ve `yaklasanToplam = ufuk*1000+11`). Böylece "hangi raporun ekranda kaldığı" ayırt edilebiliyor.
- `ufukGun=45` sorusuna 400 dönülüyor; bu, sunucu reddi yolunu sınıyor.
- 5 yeni test eklendi.

Migration: **gerekmedi** (şema değişikliği yok). Backend, uç ve Sozlukler.cs hub'ı değişmedi. ModalPencere eklenmedi. Yeni satır içi `style` yok.

## 3. DoD sözleşmesi

Yeni varlık ya da kayıt türü **yok**. Vade takibi türetilmiş bir rapor olarak kalıyor ve düzenlenmiyor. Sayfadaki `DegismezIpucu` (UX-S25) gerekçesiyle korundu.

| DoD maddesi | Durum |
|---|---|
| Her düğme ya istek atar ya da kilitlidir | 7/30/60 her tıklamada GET atıyor (testlerde `yollar` kaydı). Kilitli düğme yok. |
| Boş veya geçersiz girdide Türkçe mesaj, sessiz etkisizlik yok | Boş, aralık dışı ve kesirli girdide `#vade-uyari` bandı çıkıyor ve nedeni yazıyor. |
| Hata "kayıt yok"a çevrilmiyor (UX-S28) | Hata olunca son rapor kalıyor ve bant basılıyor. İlk yüklemede hata olursa `HataBandi` çıkıyor. |
| Dönemden bağımsız kart (UX-S33) | Mevcut `DonemdenBagimsiz` / `UfukOtesiAciklama` alt metinleri korundu. |

**Dokunulan sayfalar:** `Pages/VadeTakibi.razor`. P-63 aynı dosyayı daha sonra açacak; çakışma haritasındaki sıra P-75 → P-63.

## 4. Test sayıları

| Kapsam | Önce | Sonra |
|---|---|---|
| `VadeEkranTestleri` | 5 | **10** (+5) |
| Filtreli koşum (`VadeEkranTestleri` + `UX1C3EkranTestleri`) | – | **40/40 geçti** |
| Tam süit (izole anlık görüntü, § 6) | – | 4761 test: 4751 geçti, 10 başarısız (hepsi başka şeritlerin) |

Yeni testler:
- `VadeTakibi_GecersizUfuk_KartlarEkrandaKalir`: -1, 366, boş ve 7,5 girdileri ile sunucu reddini (45) dener. 5 kart ve tablo DOM'da kalır ve son geçerli değeri (30.011) gösterir. Geçersiz değer sunucuya hiç gitmez.
- `VadeTakibi_GecersizUfuk_RoleAlertUyarisiBasilir`: geçerli durumda role=alert sayısı 0 olmalı. Her geçersiz denemede **tek** bir `role=alert` (`#vade-uyari`) çıkmalı, nedeni ve ufku yazmalı. Geçerli değere dönülünce bant kalkmalı.
- `VadeTakibi_HizliSecim_AriaPressedDogru`: seçili düğmede `aria-pressed=true`, diğerlerinde false olmalı; `secili` sınıfıyla tutarlı olmalı; düğmelerde `type=button` bulunmalı.
- `VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron`: 30 yazılınca `#vade-ufuk-30` basılı olmalı. 14 yazılınca hiçbir düğme basılı olmamalı. Düğmeye basılınca girdi 60'a dönmeli. Geçersiz girdi basılı düğmeyi değiştirmemeli. Sunucu reddinde basılı düğme ekrandaki raporun ufkunda kalmalı.
- `VadeTakibi_AlanKimlikleri_Korundu`: `#vade-ufuk` bir INPUT, `#vade-ufuk-7/30/60` BUTTON olmalı. `#vade-ufuk-aciklama` dolu olmalı. `label[for=vade-ufuk]` bulunmalı.

## 5. Mutasyon kanıtları

Yöntem: yedek `scratchpad/mut-yedek/P-75/VadeTakibi.razor` → `perl -0777 -pi` ile tek eşleşmeli değişiklik (eşleşme sayısı 1 ölçüldü) → `dotnet test --filter VadeEkranTestleri -v n` → yedek geri kopyalandı → `cmp` birebir aynı çıktı. Betik: `scratchpad/p75-mut.sh`. Bazı koşumlarda kardeş şeritlerin eşzamanlı derlemesi yüzünden MSB3030/MSB4018 dosya kilidi hatası çıktı; betik yeniden denedi ve ölçüm ikinci denemede alındı.

| Kod | Değişiklik | Kırmızıya dönen testler | cmp |
|---|---|---|---|
| M75-1 | Hata durumunda rapor nesnesi null'a çekildi (catch içinde `yanit = null;`) | **VadeTakibi_GecersizUfuk_KartlarEkrandaKalir**, VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron | AYNI |
| M75-2 | `aria-pressed` özniteliği silindi | **VadeTakibi_HizliSecim_AriaPressedDogru**, VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron | AYNI |
| M75-3 | `ElemanId="vade-ufuk"` → `"vade-ufuk-girdi"` | **VadeTakibi_AlanKimlikleri_Korundu**, Ufuk_gun_degisince_sunucuya_YENIDEN_sorulur, KartlarEkrandaKalir, RoleAlertUyarisiBasilir, GirdiVeDugmeler_TekKaynaktanSenkron | AYNI |
| M75-4 (ek) | Aralık kapısı kapatıldı (`d < UfukEnAz \|\| d > UfukEnCok` → `false`) | KartlarEkrandaKalir, RoleAlertUyarisiBasilir, GirdiVeDugmeler_TekKaynaktanSenkron | AYNI |
| M75-5 (ek) | Sunucu reddinde `ufukGun = yanit.UfukGun` geri çekme satırı silindi | GirdiVeDugmeler_TekKaynaktanSenkron | AYNI |
| M75-6 (ek) | Tam sayı kapısı kapatıldı (`d != decimal.Truncate(d)` → `false`) | KartlarEkrandaKalir, RoleAlertUyarisiBasilir | AYNI |

6/6 mutasyonda en az bir test kırmızıya döndü. Son mutasyondan sonra `dotnet build Kuyumcu.sln` temiz çıktı: **0 Uyarı, 0 Hata**.

Test açığı (dürüstlük notu): "`<Yukleniyor/>` yalnız ilk yüklemede" koşulunu geri almak (`@if (yukleniyor)`) bUnit'te ayırt edilemiyor. Sahte API eşzamanlı tamamlandığı için ara durum hiç çizilmiyor. Bu koşul mutasyon listesinde yoktu; asıl kabul koşulu (hata/geçersizlikte kartlar kalır) M75-1 ve M75-4 ile kanıtlandı.

## 6. Tam süit ve denetim

**1. deneme** (depo içinden, istenen komutla: `--logger trx --results-directory scratchpad/trx-pP-75`): **4687 başarısız**. Bu sonuç geçersizdir: koşum sırasında kardeş şeritlerin eşzamanlı `dotnet build`'i `bin/` dosyalarını yeniden yazdı (ölçüm anında 45–51 derleme/test süreci çalışıyordu). Örnek hata: `System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.JSInterop, Version=8.0.0.0'`. Ölçüm gizlenmedi; aşağıdaki izole koşum bu sorunu gidermek için yapıldı.

**2. deneme (izole anlık görüntü):**
- `Kuyumcu.Tests/bin/Debug/net8.0` `scratchpad/iz/jewellery/dotnet/Kuyumcu.Tests/bin/Debug/net8.0`'a kopyalandı. Deponun geri kalanı sembolik bağla yansıtıldı; bu, kaynak dosya okuyan testler için gerekti.
- Anlık görüntüde P-75 kodunun bulunduğu doğrulandı: `Kuyumcu.Web.dll` içinde "vade-uyari", `Kuyumcu.Tests.dll` içinde yeni test adı VAR.
- Komut: `KUYUMCU_TEST_DB=kuyumcu_test_k dotnet test <snapshot>/Kuyumcu.Tests.dll --logger trx --results-directory scratchpad/trx-pP-75-iz`
- Sonuç: **Başarısız: 10, Başarılı: 4751, Toplam: 4761** (2 dk 37 sn).

| Kırmızı | Sahibi / neden |
|---|---|
| GunKapanisiEkranTestleri × 6 (Fark_varsa_GEREKCE…, Gerekce_girilince…, Her_aktif_kasada_GUN_KAPAT…, Kapanis_dogru_uca…, Sayim_alani_BOS…, Sayim_defterle_tutarsa…) | **S-3B** (Kasalar.razor). `Kasalar.razor` 02:40'ta değişti ve `ProAlan` eklendi. Testler `ArayuzModuServisi` kaydını içermiyor: "no registered service of type ArayuzModuServisi". |
| KasaKapatmaTestleri × 3 (BAKIYELI_kasa…, Bos_kasa…, Onaylaninca_DELETE…) | **S-3B**, aynı neden |
| P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ | Ağaçta eski dosyalar var: `dotnet/denetim.sh.mutbak` (18.09), `AramaHttpTestleri.cs.bak`, `MadenDefteriTestleri.cs.bak`, `P21CiroRucuTestleri.cs.bak` (21.09). Hepsi P-75'ten önceki tarihli ve bu paketin değil. Dosya silmek yasak olduğu için orkestratöre bırakıldı. |

**Madde (7), P-75'in yeni kırmızı üretmediği:** 10 kırmızının hiçbiri VadeTakibi veya Vade ekranına ait değil; hepsi başka şeritlerin dosyalarında. VadeEkranTestleri (10) ve UX1C3EkranTestleri tam süitte yeşil. **Fark = 0.**

**Denetim:** `dotnet/denetim.sh` çıkış kodu 0, sonuç **DENETİM TEMİZ.** (286 ✓). İlgili sayaçlar:
- satır içi style 110 (tavan 196 / 143)
- Pages ModalPencere 48 (tavan 55)
- ALAN_ANAHTARSIZ_DOGRULAMA 0
- SOZLUK_SAYI 0

## 7. Canlı doğrulama

Ortam: port **5186**, `kuyumcu_net`, kiracı `PAKETP75-TEST Kuyumculuk`. API, depodaki `bin` kardeş derlemeler yüzünden apphost'suz kaldığı için (`dotnet run --no-build` "An error occurred trying to start process … Kuyumcu.Api" verdi) Api `bin` anlık görüntüsünden başlatıldı: `dotnet scratchpad/api-snap/Kuyumcu.Api.dll --urls http://localhost:5186`.

Davet jetonu yerelde üretildi ve DB'ye yalnız SHA-256 özeti yazıldı. Jeton, parola ve erişim jetonu ekrana basılmadı. Bunları tutan scratchpad dosyaları `printf ""` ile boşaltıldı. Yanıt gövdeleri okundu.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /api/v1/auth/kayit` (davetli) | 201 | **201**; tenantId, accessToken ve refreshToken döndü |
| 2 | `POST /api/v1/cari` (MUSTERI "P75 Canli Musteri") | 201 | **201** |
| 3 | `POST /api/v1/cari/{id}/veresiye` 1000 TL, vadeGun 20 | 200 | **200** |
| 4 | `GET /raporlar/vade-takibi?ufukGun=7` | 200; borç ufkun ötesinde | **200** `ufukGun:7, yaklasanToplam:0, ufukOtesiToplam:1000.00, acikToplam:1000.00` |
| 5 | `?ufukGun=30` | 200; borç "yaklaşan" | **200** `ufukGun:30, yaklasanToplam:1000.00, ufukOtesiToplam:0` |
| 6 | `?ufukGun=60` | 200 | **200** `ufukGun:60, yaklasanToplam:1000.00` |
| 7 | `?ufukGun=-1` | 400 + Türkçe mesaj (ekranın banda yazdığı metin) | **400** `"Ufuk gün sayısı negatif olamaz"`, istekKimligi mevcut |
| 8 | `?ufukGun=366` | Sunucu kabul eder (üst sınır yok) | **200** `ufukGun:366`. Ekran 365 üstünü sunucuya göndermiyor (bUnit, M75-4). |
| 9 | Servis edilen WASM paketi (`GET /_framework/Kuyumcu.Web.wasm`) | P-75 kodu yayında | **200**, 2.847.513 B. İçinde `vade-uyari`, `aria-pressed` ve `vade-uyari-son-gecerli` VAR. |

SQL kanıtı:
```
select c.ad, h.tur, h.tl_tutari, date(h.vade_tarihi) vade, datediff(h.vade_tarihi, utc_date()) kalan_gun
from hareketler h join cariler c on c.id=h.cari_id where h.tenant_id='<P75>';
P75 Canli Musteri  VERESIYE  1000.00  2026-10-14  21
```
21 günlük vade 7 günlük ufkun ötesinde kalıyor (#4) ve 30 günlük ufukta yaklaşan sayılıyor (#5). Bu, rakamların ufukla gerçekten değiştiğini ve "son geçerli rapor"un anlamlı olduğunu gösteriyor.

Dürüstlük notu: ekran (Blazor) canlıda tarayıcıyla denenmedi, çünkü oturum açmak parola girmeyi gerektiriyor. Ekran davranışı bUnit ile kanıtlandı (§ 4, § 5). Yayındaki paketin yeni kodu taşıdığı #9 ile doğrulandı.

**Temizlik:**
- API durduruldu: PID 27828 `kill`, `lsof -nP -iTCP:5186 -sTCP:LISTEN` boş. İlk deneme PID 27173 hiç dinlemeye geçmemişti.
- Test kiracısı `information_schema`'dan `tenant_id` kolonu olan 70 tablonun hepsinden silindi: cari_bakiyeler 1, cariler 1, denetim_kayitlari 22, hareketler 1, is_calismalari 3, kullanicilar 1, kur_snapshots 7, odeme_kalemleri 1, refresh_tokenler 1, subeler 1, telemetri_olcumleri 1. Ayrıca `tenants` satırı ve tüketilen `kiraci_davetleri` satırı silindi.
- Silme sonrası kalan satır 0, davet 0. **tenants 10 → 11 → 10.** `QA-TEST-*` kiracılarına (4) dokunulmadı.
- API günlüğünde `is_calismalari` benzersiz anahtar ihlali (`MASAK_SIB_SURESI`, `SAKLAMA_ONIZLEME`, 2026-09-24) görüldü. Bunlar başka bir sunucunun aynı gün koşturduğu işlerin idempotensi; bu pakete ait değil.

## 8. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"`: ağacın tamamı ilk commit'ten sonra izlenmediği için bu paketin dosyaları da `??` görünüyor. P-75'in dokunduğu dosyalar:
- `dotnet/Kuyumcu.Web/Pages/VadeTakibi.razor` (değişti)
- `dotnet/Kuyumcu.Tests/VadeEkranTestleri.cs` (değişti)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.VadeUfuk.cs` (YENİ)

Ağaca geçici, yedek ya da .bak dosyası bırakılmadı. Yedekler `scratchpad/mut-yedek/P-75/` ve `scratchpad/p75-orj/` altında.

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Geçersiz ufuk denemesinde StatKart'lar DOM'da kalır ve son geçerli değerleri gösterir | KARŞILANDI | `VadeTakibi_GecersizUfuk_KartlarEkrandaKalir`; M75-1, M75-4 |
| Geçersiz denemede role=alert taşıyan tek bir uyarı bandı görünür | KARŞILANDI | `VadeTakibi_GecersizUfuk_RoleAlertUyarisiBasilir` (`Assert.Single`) |
| Seçili hızlı düğmede aria-pressed=true, diğerlerinde false | KARŞILANDI | `VadeTakibi_HizliSecim_AriaPressedDogru`; M75-2 |
| Girdiden 30 yazılınca #vade-ufuk-30 aria-pressed=true olur | KARŞILANDI | `VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron`; M75-5 |
| #vade-ufuk, #vade-ufuk-7/30/60, #vade-ufuk-aciklama korunur | KARŞILANDI | `VadeTakibi_AlanKimlikleri_Korundu` + mevcut `UX1C3EkranTestleri`; M75-3 |
| Paket kapısı (1) build 0 hata | KARŞILANDI | 0 Uyarı, 0 Hata |
| (2) kendi testleri 0 başarısız | KARŞILANDI | 40/40 filtreli; tam süitte VadeEkranTestleri yeşil |
| (3) kendi sayaçları ✓ | KARŞILANDI | DENETİM TEMİZ |
| (4) mutasyon kanıtı | KARŞILANDI | 6/6 kırmızı, cmp AYNI |
| (5) canlı doğrulama | KARŞILANDI | § 7 (#1–#9 + SQL) |
| (6) test kiracısı silindi | KARŞILANDI | tenants 10 → 10 |
| (7) yeni kırmızı yok | KARŞILANDI | Fark 0 (§ 6) |
| Dosya sınırı (Sozlukler.cs hub'ı ve ortak bileşenler değişmez) | KARŞILANDI | Yeni metin `Sozlukler.VadeUfuk.cs` (partial desen) |

## 10. Açık kalanlar (sahipleriyle)

1. **S-3B:** `GunKapanisiEkranTestleri` (6) ve `KasaKapatmaTestleri` (3) kırmızı. `Kasalar.razor`'a `ProAlan` girdi; test bağlamları `ArayuzModuServisi` kaydetmiyor.
2. **Orkestratör / kurucu:** `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` kırmızı. Nedeni 4 eski yedek dosya: `dotnet/denetim.sh.mutbak`, `Kuyumcu.Tests/{AramaHttp,MadenDefteri,P21CiroRucu}Testleri.cs.bak`. Dosya silme yasak olduğu için dokunulmadı.
3. **P-74 (Sayi bileşeni):** ağaca inmemiş. İnince `Sayi` aralık dışında `DegerChanged`'i hiç çağırmazsa sayfa bandı çıkmaz, yalnız alan altı hata görünür. Bu durumda "tek role=alert" koşulu yine sağlanır (alan hatası da role=alert, sayfa bandı çıkmaz). Yine de P-74 inince `VadeEkranTestleri` yeniden koşulmalı.
4. **Backend üst sınırı (dosya sınırı dışında):** `/raporlar/vade-takibi` 365 üstünü kabul ediyor (366 → 200). Ekran göndermiyor ama API doğrudan çağrılabilir. Sınır sunucuda da tek kaynaktan konmalı. Önerilen sahip: Rapor modülüne dokunan sonraki backend paketi.
5. **Ortam:** paralel şeritler aynı `bin/obj`'u derlediği için depo içinden koşulan tam süit geçersiz sonuç veriyor (4687 sahte kırmızı) ve `dotnet run --no-build` apphost'u bulamıyor. Orkestratörün dalga kapısı ölçümünü şeritler dururken ya da `bin` anlık görüntüsünden yapması önerilir.

Kurucuya / mali müşavire not: yok (mevzuat içeriği yok).
