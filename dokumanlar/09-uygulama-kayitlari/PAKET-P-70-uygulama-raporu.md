# PAKET P-70 — Kur yoklaması: push bağlıyken duraklat + tek paylaşılan okuma noktası

- Şerit: B (YEDEK + KUR + DENETİM İZİ), sıra P-56 → P-72 → **P-70** → P-73
- Rol: Taşkın (backend) + Ertuğ (frontend), uygulayıcı: Opus
- Tarih: 2026-09-24 02:05–02:35 (+03)
- Durum: **BİTTİ**. Paket kapısının 7 maddesi sağlandı. Tam süitteki 10 kırmızının hiçbiri bu paketin değil, sahipleri aşağıda yazılı.

## 1. Spesifikasyonu koda karşı doğrulama (önce ölçüm)

| İddia (TRIYAJ-TUR8 § P-70) | Kodda ne buldum | Sonuç |
|---|---|---|
| `CanliKurServisi.cs:24 YoklamaAraligi = 10 sn` | Satır 42'deydi (satırlar kaymış), `static readonly TimeSpan.FromSeconds(10)` | DOĞRU |
| `:92-100 YoklamaDongusu push bağlıyken de çalışıyor` | Satır 106-118: döngüde yalnız `oturum.GirisYapildi` kontrolü vardı, `Bagli` kontrolü yoktu | DOĞRU |
| `ApiIstemci.cs:88-96 "ZAMAN PENCERELİ ÖNBELLEK YOKTUR"` | Satır 88-96'da aynen var. Yalnız eşzamanlı kopya istekler birleşiyor (tek uçuş) | DOĞRU. ApiIstemci'ye dokunmadım; tazelik penceresi yalnız `CanliKurServisi.AlAsync` içinde |
| Dosya ayak izi `CanliKurServisi.cs`, `UX4bEkranTestleri.cs` | `test -e` ile ikisi de var (175 ve 454 satır) | DOĞRU |
| 154 isteğin rota bazlı dağılımı | DOĞRULANAMADI (spesifikasyon da öyle diyor). `/kur/guncel`'i kendisi çağıran 11 ekran var: Kasalar, Cariler ×2, Kurulum, SiparisDetay, Fiyatlandirma, Evraklar, Emanet, Vezne, Siparisler, Kur | Ekranların `AlAsync`'e taşınması açık kalanlarda (§ 10), çünkü o dosyalar başka şeritlerde |

Sapmalar ve gerekçeleri:
1. **Test kancası yerine olay tabanlı bağlantı durumu.** Eski `Bagli => hub?.State == Connected` testte taklit edilemiyordu. Yerine `BaglantiDurumunuIsle(bool)` adlı public yöntem geldi. Hub olayları (`StartAsync` sonrası, `Reconnecting`, `Reconnected`, `Closed`) bu yöntemi çağırıyor, testler de aynı yöntemi kullanıyor. Hub'ı sarmalayan ayrı bir soyutlama kurulmadı.
2. **Kopuşta telafi okuması.** Bağlantı koptuğu an bir REST okuması yapılıyor, çünkü kopuk kaldığı sürede kaçırılan güncelleme olabilir. Döngü de bir sonraki tikten itibaren istek atmaya başlıyor. Kabul koşulu "10 sn içinde" diyor. Uygulamada ilk istek hemen, sonrakiler her 10 sn'de bir atılıyor.
3. **Push bağlıyken "son sorgu" saati yine ilerliyor.** UstBar (`#ust-kur-saat`) `SonSorgu`'yu gösteriyor. Push varken hiç istek atılmazsa bu saat donar ve kullanıcı kurun bayat olduğunu sanar. Bu yüzden döngü push bağlıyken ağa gitmeden `SonSorgu`'yu günceller. Gerekçe: WebSocket açıksa sunucu yeni bir kur yazdığında zaten haber verir. `SonSorgu`'nun belge yorumu buna göre güncellendi. UstBar.razor'a dokunulmadı.
4. **Sekme görünürlüğü ES modülüyle izleniyor.** Yeni dosya `wwwroot/js/kur-gorunurluk.js`, `import()` ile yükleniyor. index.html'e `<script>` eklenmedi, denetim.sh'taki "index.html <script> sayısı (sabit 8)" kilidi değişmedi. Eval yok, CSP'de yeni istisna gerekmiyor. JS yoksa (ön-işleme ya da bUnit) sekme görünür sayılıyor, yani eski davranış korunuyor.
5. **Aralık yapılandırmadan okunuyor.** Anahtar `Kur:YoklamaAraligiSaniye`, geçerli aralık 2–300 sn. Anahtar yoksa ya da değer geçersizse varsayılan 10 sn. `IConfiguration` ve `TimeProvider` birincil kurucuya `IServiceProvider` üzerinden alındı. Böylece Program.cs'e (ortak dosya) dokunmak gerekmedi. appsettings.json'a anahtar yazılmadı, varsayılan zaten 10 sn.
6. **`AlAsync` hatayı yutmuyor (UX-S28).** Başarısız okumada `ApiHata` fırlatıyor ve `SonHata` yazılıyor. `TazeleAsync`'in hata yutan eski davranışı (çubuk ve döngü için) değişmedi. İkisi de tek ağ yolunu, `CekAsync`'i kullanıyor.

Mevzuat hükmü yok. Bu paket yalnız istemci tarafı yoklamayla ilgili, mali müşavir notu gerekmiyor.

## 2. Yapılanlar

- `Kuyumcu.Web/Servisler/CanliKurServisi.cs`
  - `YoklamaDongusu`: `if (!Gorunur) continue;` (satır 230) ve `if (Bagli) { SonSorgu = now; continue; }` (satır 231) eklendi. Push bağlıyken ya da sekme gizliyken ağ isteği atılmıyor.
  - `PeriodicTimer(Aralik, zaman)` kullanılıyor. `zaman` DI'daki `TimeProvider`, yoksa `TimeProvider.System`.
  - `public TimeSpan Aralik` ve `public static TimeSpan AralikOku(IConfiguration?)` eklendi. `YoklamaAraligi` artık VARSAYILAN değeri tutuyor ve hâlâ 10 sn.
  - `public Task<KurAnlik> AlAsync(bool zorla = false)`, satır 254. Kur taze sayılır ve ağ isteği atılmaz: ya push bağlıdır ya da son başarılı okumanın yaşı `Aralik`'ten küçüktür. `zorla=true` verilirse her çağrıda gerçek istek atılır.
  - `BaglantiDurumunuIsle(bool)` ve `[JSInvokable] GorunurlukDegisti(bool)` eklendi. Görünürlük aboneliği `DisposeAsync` ve `Dispose` içinde bırakılıyor.
- `Kuyumcu.Web/wwwroot/js/kur-gorunurluk.js` (YENİ): `izle(dotnetRef)` ve `birak()`.
- `Kuyumcu.Tests/UX4bEkranTestleri.cs`: dosyanın sonuna P-70 bölümü eklendi. İçinde elle ilerletilen `SahteSaat : TimeProvider` (ITimer ile) ve 6 test var. Mevcut 20 teste ve sahte API'ye dokunulmadı.
- Sozlukler.cs: DOKUNULMADI. Yeni ekran metni yok. `"Kur alınamadı"` literali zaten vardı, yalnız tek ağ yoluna taşındı.
- Migration: gerekmedi, şema değişikliği yok.

### Dokunulan sayfalar
Yok. Hiçbir `.razor` sayfası açılmadı.

### DoD sözleşmesi
Yeni varlık ya da kayıt türü yok. Yeni sunucu ucu, yeni form ve yeni düğme de yok. PATCH/DELETE/[RolGerekli]/denetim kaydı yükümlülüğü doğmuyor. Finansal varlıklara dokunulmadı.

## 3. Derleme

`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, temiz):

```
    2 Uyarı
    0 Hata
```

İki uyarının ikisi de `Kuyumcu.Tests/S36KasaBildirimTestleri.cs` (123 ve 451) içinde: CS8631 ve CS8602. Bu dosya PARA şeridinin, P-70 dokunmadı. P-70 dosyalarında uyarı 0.

Not: Bir ara derleme 19 hatayla kırıldı. Hataların hepsi `Pages/Kasalar.razor`'daydı ve dosya o sırada başka bir şerit tarafından düzenleniyordu (02:12). Yaklaşık 60 sn sonra dosya tamamlanınca derleme yeşile döndü. Bu paketin ürettiği bir hata değildi.

## 4. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| `UX4bEkranTestleri`, önce | 20 | — | — |
| `UX4bEkranTestleri`, sonra (3 ardışık koşu) | 26 | 26 / 26 / 26 | 0 / 0 / 0 |
| `CanliKurServisi_` süzgeci | 6 | 6 | 0 |
| Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_q`, trx) | 4756 | 4746 | 10 (hiçbiri P-70'in değil, § 7) |

Yeni testler (6):
1. `CanliKurServisi_PushBagliykenYoklamaIstekAtmaz`: hub bağlıyken sahte saatle 60 sn geçiriliyor, `/kur/guncel` GET sayısı **1** (yalnız ilk REST okuma). `SonSorgu` da ilerliyor.
2. `CanliKurServisi_PushKopuncaYoklamaDevreyeGirer`: bağlıyken 30 sn boyunca 1 istek. Kopuşta hemen bir telafi okuması (2). Sonra +10 sn'de 3, +10 sn'de 4.
3. `CanliKurServisi_AlAsync_ArtArdaCagrilardaTekIstek`: `AlAsync()` 5 kez çağrılıyor, **1** istek gidiyor. Aralık aşılınca 6. çağrı gerçekten sunucuya gidiyor (2).
4. `CanliKurServisi_AlAsyncZorla_HerCagridaIstekAtar`: kur taze olduğu hâlde `AlAsync(zorla:true)` her çağrıda +1 istek atıyor.
5. `CanliKurServisi_SekmeGizliykenYoklamaDuraklar` (ek): sekme gizliyken 60 sn boyunca 0 yeni istek. Görünür olunca bayat kur hemen tazeleniyor.
6. `CanliKurServisi_YoklamaAraligiYapilandirmadanOkunur` (ek): varsayılan 10 sn, "30" okunuyor, "1", "301" ve "on" reddediliyor.

## 5. Mutasyon kanıtı

Yöntem: yedek `scratchpad/mut-yedek/P-70/yeni/` altında. Her mutasyon `perl -0777 -pi` ile tek eşleşme olarak uygulandı, ardından `dotnet test --filter CanliKurServisi_` koşuldu, yedek geri kopyalandı ve `cmp` ile doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M70-1 | `if (Bagli)` yerine `if (false)` (döngüdeki Bagli kontrolü kaldırıldı) | `CanliKurServisi_PushBagliykenYoklamaIstekAtmaz`, `CanliKurServisi_PushKopuncaYoklamaDevreyeGirer` | cmp=AYNI |
| M70-2 | `if (!zorla && Taze)` yerine `if (!zorla && Son is not null)` (tazelik/yaş kontrolü silindi) | `CanliKurServisi_AlAsync_ArtArdaCagrilardaTekIstek` | cmp=AYNI |
| M70-2b | `if (!zorla && Taze) return Son!;` satırı tamamen silindi | `CanliKurServisi_AlAsync_ArtArdaCagrilardaTekIstek` | cmp=AYNI |
| M70-3 | `if (!zorla && Taze)` yerine `if (Taze)` (zorla yolunda da önbellek dönüyor) | `CanliKurServisi_AlAsyncZorla_HerCagridaIstekAtar` | cmp=AYNI |
| M70-4 | `if (!Gorunur) continue;` kaldırıldı | `CanliKurServisi_SekmeGizliykenYoklamaDuraklar` | cmp=AYNI |
| M70-5 | Kopuştaki telafi okuması (`_ = TazeleAsync()`) kaldırıldı | `CanliKurServisi_PushKopuncaYoklamaDevreyeGirer` | cmp=AYNI |
| M70-6 | Aralık sınırı `>= 2 and <= 300` yerine `>= 1` | `CanliKurServisi_YoklamaAraligiYapilandirmadanOkunur` | cmp=AYNI |

⚠ Anayasa md.1 gereği açık kayıt: M70-1'in İLK koşusu yeşil çıktı (0/6 kırmızı). Aynı mutasyon elle yeniden uygulanıp hata ayıklama iddiasıyla koşulduğunda 7 istek sayıldı. `mut.sh` ile tekrarlandığında 2 test kırmızıya döndü. İlk koşunun neden yeşil kaldığını DOĞRULAMADIM. Aynı `bin/obj` ağacını paylaşan paralel bir şerit derlemesinin mutasyonsuz DLL'i üzerine yazmış olması bir olasılık, ama kanıtlanmadı. Tablodaki sonuçlar tekrarlanan koşunun sonuçları. Son mutasyondan sonra kaynak `cmp` ile birebir aynı ve temiz `dotnet build Kuyumcu.sln` 0 hata verdi.

## 6. Denetim

`dotnet/denetim.sh` çıkış kodu 0, son satır `DENETİM TEMİZ.` (✗ satırı 0). "index.html <script> sayısı (sabit 8)" değişmedi.

## 7. Tam süit kırmızıları ve sahipleri (P-70'in ürettiği fark: 0)

| Test | Hata | Sahip / neden P-70 değil |
|---|---|---|
| `KasaKapatmaTestleri.BAKIYELI_kasa_pasife_ALINAMAZ`, `.Onaylaninca_DELETE_dogru_kasaya_gonderilir`, `.Bos_kasa_pasife_ALINABILIR` | `Cannot provide a value for property 'Modu' on type 'ProAlan'` (ArayuzModuServisi kayıtlı değil) | PARA şeridi. `Pages/Kasalar.razor` koşum sırasında düzenleniyordu (02:12 ve 02:27), içinde 2 yeni `<ProAlan` var. Bu testlerin DI'ında ArayuzModuServisi yok. P-70 Kasalar'a ve bu testlere dokunmadı |
| `GunKapanisiEkranTestleri` (6 test: `Fark_varsa_GEREKCE_…`, `Kapanis_dogru_uca_…`, `Sayim_alani_BOS_…`, `Her_aktif_kasada_…`, `Sayim_defterle_tutarsa_…`, `Gerekce_girilince_…`) | Aynı `ProAlan.Modu` DI hatası | PARA şeridi (Kasalar.razor ve gün kapanışı). Neden aynı |
| `S33UrunlerEkranTestleri.S33_18_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez` | `Expected "k2" Actual null` | Ürünler ekranı / S33. Tek başına koşulunca 23/23 yeşil. Yük altında kararsız, P-72 raporunda da aynı sınıf kararsız olarak kayıtlı. Kur servisini kullanmıyor |

## 8. Canlı doğrulama (port 5187, izole şerit portu; 5199'a dokunulmadı)

API: `dotnet run --no-build --urls http://localhost:5187`, PID 83154. Test kiracısı `PAKETP-70-TEST`: tek kullanımlık davet satırı SQL ile açıldı, sonra silindi.

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /api/v1/auth/kayit` (davetsiz) | 403 | 403 "Geçerli bir davet jetonu gerekiyor…" |
| `POST /api/v1/auth/kayit` (davetli) | 201 | 201, tenantId `01M389E6RDTXAWEFXZV7V9W49K`, accessToken VAR |
| `GET /js/kur-gorunurluk.js` | 200, JS | 200, `Content-Type: text/javascript`, 1590 bayt, `nosniff` |
| `GET /api/v1/kur/guncel` (Bearer) | 200 ve kur alanları | 200, `id` dolu, `altinHasFiyati` 6756.86, `usdTry` 48.967, `kaynak` ALTINKAYNAK, 6 ayar fiyatı |
| `POST /hubs/kur/negotiate?negotiateVersion=1` (Bearer) | 200, WebSockets var | 200, connectionToken VAR, taşıyıcılar [WebSockets, ServerSentEvents, LongPolling]. Tarayıcıda push kurulabilir, yani P-70'in duraklatma yolu gerçek ortamda devreye girer |
| Aynı negotiate (jetonsuz) | 401 | 401 |

SQL kanıtı: triyajın doğrulayamadığı "KurSnapshot kaynağı" maddesi burada ölçüldü, salt okuma yapıldı.
```
select count(*) from kur_snapshots;                          → 159808
select kaynak,count(*) from kur_snapshots group by kaynak;   → ALTINKAYNAK 156597 · ANKARA 2712 · TRUNCGIL 483 · MANUAL 14 · HAREM 2
select kaynak,count(*) … where tenant_id='<P70 kiracısı>';   → ALTINKAYNAK 2 (kayıttan sonraki ilk dakikalarda zamanlayıcı yazdı)
```
Yorum: 159 808 satırın 14'ü MANUAL. Geri kalanı dış kaynaklı; kiracı başına zamanlayıcının yazdığı kayıtlarla tutarlı. Snapshot yazımı sunucu tarafı ve P-70 onu değiştirmiyor. P-70 yalnız istemcinin OKUMA sayısını azaltıyor.

Tarayıcıda 60 sn'lik ağ sayımı YAPILMADI. Uygulamaya girmek için oturum jetonunu ya da parolayı tarayıcıya elle koymak gerekiyordu, bu da ajan güvenlik kuralına göre yasak. Bu davranış bUnit'te sahte saatle ölçüldü (§ 4, test 1–5). Kurucunun 5199'da DevTools → Network süzgeci `kur/guncel` ile 60 sn'lik sayım yapması önerilir. Beklenen sonuç: push bağlıyken 0 yeni istek.

Temizlik:
- API durduruldu, `lsof -nP -iTCP:5187 -sTCP:LISTEN` boş.
- `information_schema`'da `tenant_id` kolonu olan bütün tablolarda bu kiracının satırları silindi, kalan satır **0**.
- `tenants` satırı ve `kiraci_davetleri` satırı silindi.
- `email` kolonu olan tablolarda (cariler, kiraci_davetleri, kullanicilar) test e-postasıyla eşleşen satır 0.
- **tenants sayısı: başlangıçta 10, sonda 10.**
- `QA-TEST-` önekli kiracılara dokunulmadı.
- Scratchpad'deki geçici jeton ve parola dosyaları `printf '' >` ile boşaltıldı.

## 9. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından P-70'e ait satırlar:
```
 M dotnet/Kuyumcu.Web/Servisler/CanliKurServisi.cs
?? dotnet/Kuyumcu.Web/wwwroot/js/kur-gorunurluk.js
?? dotnet/Kuyumcu.Tests/UX4bEkranTestleri.cs      (dosya zaten izlenmiyordu; P-70 sonuna ekledi)
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-70-uygulama-raporu.md
```

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Hub bağlıyken 60 sn'de `/kur/guncel` = 1 | KARŞILANDI | `CanliKurServisi_PushBagliykenYoklamaIstekAtmaz`, M70-1 kırmızı |
| Hub kopunca 10 sn içinde yeniden istek | KARŞILANDI | `CanliKurServisi_PushKopuncaYoklamaDevreyeGirer` (kopuşta hemen, sonra her 10 sn), M70-1 ve M70-5 kırmızı |
| `AlAsync(zorla:false)` 5 kez → 1 istek | KARŞILANDI | `CanliKurServisi_AlAsync_ArtArdaCagrilardaTekIstek`, M70-2 ve M70-2b kırmızı |
| `AlAsync(zorla:true)` her çağrıda istek | KARŞILANDI | `CanliKurServisi_AlAsyncZorla_HerCagridaIstekAtar`, M70-3 kırmızı |
| Aralık yapılandırılabilir, varsayılan 10 sn | KARŞILANDI | `Kur:YoklamaAraligiSaniye`, `CanliKurServisi_YoklamaAraligiYapilandirmadanOkunur`, M70-6 kırmızı |
| Sekme görünmezken yoklama duraklar | KARŞILANDI | `js/kur-gorunurluk.js` ve `GorunurlukDegisti`, `CanliKurServisi_SekmeGizliykenYoklamaDuraklar`, M70-4 kırmızı |
| Paket kapısı (1) build 0 hata | KARŞILANDI | § 3 |
| (2) kendi testleri 0 başarısız | KARŞILANDI | 26/26 ×3 |
| (3) denetim.sh | KARŞILANDI | DENETİM TEMİZ |
| (4) mutasyon | KARŞILANDI | 7/7 kırmızı, cmp aynı |
| (5) canlı | KARŞILANDI (sunucu tarafı ve uçlar) | § 8. Tarayıcı içi sayım bUnit'te ölçüldü, gerekçe § 8 |
| (6) test kiracısı silindi | KARŞILANDI | kalan 0, tenants 10 → 10 |
| (7) yeni kırmızı yok | KARŞILANDI | 10 kırmızının hepsi başka şeritlerde (§ 7) |
| Dosya sınırı (Sozlukler, başka şeritlerin sayfaları) | KARŞILANDI | Yalnız kur servisi, yeni JS modülü ve UX4b testleri |

## 11. Açık kalanlar

1. **Ekranların `AlAsync`'e taşınması.** Tek okuma noktası hazır, ama `/kur/guncel`'i kendisi çağıran 11 ekran var ve bunlar bu şeridin dosya sınırı dışında: Kasalar ve Vezne (PARA), Cariler ×2, Emanet, Evraklar, Fiyatlandirma, Siparisler ve SiparisDetay (Sipariş/Fatura kardeş şeridi), Kurulum (YAYIN). QA'nın 154 isteğinin asıl kaynağı muhtemelen bunlar, ama rota dağılımı DOĞRULANAMADI. Her ekranın kendi sahibi, sırası gelince `Api.GetAsync<…>("/kur/guncel")` çağrısını `Canli.AlAsync()` ile değiştirmeli. Yenile düğmeleri `AlAsync(zorla: true)` kullanmalı. Kur.razor bu şeritte ama kendi `KurVeri` sözleşme türünü kullanıyor ve "Şimdi çek" POST'u zaten yalancı değil. Tür eşlemesi P-73 (Kur dosyaları) ile birlikte yapılmalı. Orkestratör sırayı ayarlamalı.
2. **UstBar'daki "son sorgu" metni.** Push bağlıyken saat "canlı teyit" anlamına geliyor (§ 1 sapma 3). Bunu UstBar'da ayrı bir "canlı" rozetiyle söylemek Sozlukler'e yeni metin gerektirir. Bu şerit Sozlukler'e dokunamıyor, orkestratöre bırakıldı.
3. **P-71** aynı test dosyasına (`UX4bEkranTestleri.cs`) sonra dokunacak. P-70 bölümü dosyanın sonunda, "PAKET P-70" başlığı altında ayrı duruyor.
4. **Tarayıcıda 60 sn'lik ağ sayımı** kurucu ya da QA turu tarafından 5199'da yapılmalı (§ 8).
5. **Başkasına ait kırmızılar** (§ 7): PARA şeridi (Kasalar/GunKapanisi ProAlan DI, 9 test) ve S33_18 kararsızlığı.

## 12. Kurucuya / mali müşavire notlar

Mevzuat hükmü yok, mali müşavir teyidi gerekmiyor. Kurucuya tek not: yoklama aralığı artık `Kur:YoklamaAraligiSaniye` ile değiştirilebilir (2–300 sn). Varsayılan, kurucu isteği olan 10 sn olarak korundu.
