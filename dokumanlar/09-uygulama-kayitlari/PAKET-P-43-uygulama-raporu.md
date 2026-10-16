# PAKET P-43 — Canlı duman kapısı (6 engellenen rota + /kilavuz) + QA ortalama bütünlüğü

**Durum: BİTTİ** (paket kapısının 7 maddesi sağlandı, aşağıda kanıtlı) · 2026-09-23 · Taşkın (backend) + Ertuğ (frontend) rolü
**Gerçek süre:** yaklaşık 20 dakika (bu oturumda 18:48'den 19:08'e kadar). Önceki, yarıda kalmış oturum `duman-testi.sh`, `qa-ozet.mjs` ve `qa-turu.js` bloğunu yazmıştı; bu oturum onları okudu, doğruladı ve güçlendirdi. Triyajdaki 2 saat bir tahmindi.

> ⚠ **ORKESTRATÖR/KURUCU İÇİN İLK MADDE:** `kapi.sh` bu paketten sonra **5199 üzerinde KIRMIZI (çıkış 7)** döner, çünkü **şu anki 5199 gerçekten bozuk yayın servis ediyor** (ölçüldü, aşağıda). Kapı doğru çalışıyor; bozuk olan yayının kendisi. Düzeltmek için 5199, `qa-sunucu.sh` ile P-42'nin kalıcı dizininden (`~/.kuyumcu-yayin`) yeniden açılmalı. 5199'a dokunmak bu paketin yetkisinde değil (kırmızı çizgi), o yüzden **bunu kurucu ya da orkestratör yapacak.**

## 1. Spesifikasyonu koda karşı doğrulama (sapmalar)

| İddia (triyaj) | Ölçülen | Karar |
|---|---|---|
| `qa-yayin/wwwroot/kilavuz.html` YOK, kaynakta VAR | Kaynak 60.845 bayt VAR. `~/.kuyumcu-yayin` (P-42) içinde VAR, 60.845 bayt. **Ancak 5199 süreci (PID 12577, 22/09'dan beri ayakta) hâlâ ESKİ scratchpad `qa-yayin`'den koşuyor** (`lsof -d cwd`); orada kilavuz.html YOK. Canlıda `/kilavuz.html` = **404 / 0 bayt** | Kapı bunu yakalıyor. Ek olarak **yeni test** yazıldı: `test_canli_yayin_diskteki_yayinla_ayni`. Canlı boot.json karması `d2fa9d5e…`, diskteki `3c635cdd…`, yani 5199 doğrulanan dizini değil **başka bir yayını** servis ediyor. Bu kontrol olmasaydı dosya katmanı yeşil yanar ama yanıltırdı. |
| 66 boot.json varlığı | Yayında 66 varlık (jsModuleNative 1, jsModuleRuntime 1, wasmNative 1, icu 3, assembly 60). Debug/Release derleme çıktısında pdb'lerle birlikte 211 | Uyuldu |
| Kilavuz.razor'da 14 bağlantı | 1 düz href (`/kilavuz.html`) + 13 bölüm çıpası = 14 | Uyuldu. Test alt sınırı ≥ 14 |
| "menü statik bağlantıları" | `MenuTanimi.Ogeler` (36 bağlantı), uzantılı dosya yok; hepsi SPA rotası | Yorum: her menü yolu için bir `@page` olmalı, uzantılı yol varsa wwwroot'ta dosya bulunmalı. SPA yedeği olan `index.html` ve onun yerel css/js bağlantıları da wwwroot'ta aranıyor |
| Duman testi 5199'da koşar | Kurala göre 5199'a dokunamam, orayı yeşile de çeviremem | Betik `--taban/--yayin` ve `KUYUMCU_DUMAN_TABAN`/`KUYUMCU_YAYIN_DIZINI` ile hedeflenebiliyor. Yeşil kanıt, sürümlü `yayinla.sh` ile scratchpad'e üretilen **tam fikstür yayını** 5198'de servis edilerek alındı. 5199'da kırmızı kanıt alındı |
| qa-turu.js'e CLI `--ozet` eklenmesi | `qa-turu.js` koşum sandbox'ında çalışıyor (gövdesi sarılıyor, `return` içeriyor, `require` yok), node ile doğrudan çalıştırılamıyor | Önceki oturumun kararı korundu: CLI ayrı bir dosyada (`qa-ozet.mjs --ozet`). Hesap bloğu **iki dosyada bayt bayt aynı**, `test_ortalama_blogu_iki_dosyada_ayni` bunu ölçüyor. **Güçlendirme:** `test_engellenen_grup_ortalamadan_ayrilir` artık **qa-turu.js'in kendi bloğunu** node'da çalıştırıyor, çıktısını CLI'ninkiyle karşılaştırıyor |
| denetim.sh sayacı | denetim.sh DALGA 7c'nin dosyası, dokunulamaz | P-43'ün denetim.sh sayacı YOK. Makine kapısı `kapi.sh` 1b/4 aşaması ve `test_kapi_duman_kirmiziyken_temiz_yazmaz` |

Mevzuat: bu paketin mevzuatla bir ilgisi yok.
Migration: gerekmedi.
Sözleşme-DoD: yeni varlık ya da uç yok. Ekrana dokunulmadı, `Kilavuz.razor` ve `RotaBasliklari.cs` yalnız okundu.

## 2. Yapılanlar

1. **`betikler/duman-testi.sh`** (yeni, 331 satır). İki katmanı var:
   - Dosya katmanı: yayın dizini diskte.
   - Canlı katman: sunucu HTTP ile gerçekten veriyor mu.

   Sunucu yoksa çıkış 4 olur ("atlandı" sayılmaz). Testler:
   - `test_kilavuz_html_200_ve_60kb`
   - `test_canli_yayin_diskteki_yayinla_ayni` **(YENİ)**
   - `test_index_html_200`
   - `test_bootjson_200`
   - `test_bootjson_varliklari_content_length_sifirdan_buyuk` (her varlık için HEAD 200 ve Content-Length > 0)
   - `test_engellenen_alti_rota_200`
   - `test_kapi_duman_kirmiziyken_temiz_yazmaz`
   - `test_engellenen_grup_ortalamadan_ayrilir`
   - `test_ortalama_blogu_iki_dosyada_ayni`
2. **`betikler/kapi.sh`**: yeni **1b/4 duman aşaması**. Derlemeden sonra, `denetim.sh`'ten ÖNCE koşar. Çıkış kodu `DUMAN=$?` ile alınır; sıfır değilse kapı `exit 7` ile kapanır ve denetim ile test hiç koşmaz. Bu yüzden çıktıda "TEMİZ" kelimesi oluşamaz. Çıkış kodu `|| true` ile yutulmuyor.
3. **`betikler/qa-turu.js`**: `P-43-ORTALAMA` bloğu, yani `gruplariAyir`, `ortalamaButunlugu` ve `ozetMetni`.
   - Bir grubun tüm sayfaları ENGELLENDİ ise ve hepsinde ortak bir önyükleme imzası varsa, o grup puan ortalamasına katılmıyor.
   - Bulgu, kritik ve düzenlenemeyen sayımları yine tüm sayfalardan yapılıyor.
   - `OLCUM` nesnesi iki ortalamayı da taşıyor; tur günlüğüne iki ortalama ayrı basılıyor.
4. **`betikler/qa-ozet.mjs`**: `--ozet <ham/qa-turu-N.json>` komut satırı aracı; hesap bloğu `qa-turu.js` ile birebir aynı.
5. **`dotnet/Kuyumcu.Tests/YayinVarlikDumanTestleri.cs`** (yeni, 4 test). Sunucu gerektirmeyen derleme-zamanı ikizi.
6. **`ham/TEK-EL-KILIT`**: P-43 iki zinciri açtı (`qa-turu.js+hazirlik-kurulu.js`, `kapi.sh`) ve aynı oturumda kapattı; satırlar yorum olarak kayda geçti. Etkin satır sayısı 0.

## 3. Test sayıları

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Önce (devir notu, orkestratör 18:55) | 4077 | 4076 | 1 |
| Filtreli `YayinVarlikDumanTestleri` | 4 | 4 | 0 |
| duman-testi.sh, tam fikstür yayını, 5198 | 21 kontrol | 21 | 0 (çıkış 0) |
| **Sonra, tam süit** (`trx-pP-43/_Mac_2026-09-23_19_04_46.trx`) | **4089** | **4088** | **1** |

**Kırmızı farkı = 0.** Tek kırmızı `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`; devir notundaki kırmızının aynısı, sahibi P-30b (DALGA 7c). +12 testin 4'ü bu paketin, kalanı paralel şeritlerin.

Derleme sonucu 0 hata, 2 uyarı. İki uyarı da `OlcumKapisiTestleri.cs(525)` satırında (CS8604). O dosya DALGA 7c'nin; bu paket dokunmadı.

`denetim.sh` sonucu **DENETİM TEMİZ** (çıkış 0).

## 4. Mutasyon tablosu (yedek `scratchpad/mut-yedek/P-43/`, her birinden sonra `cmp` ile birebir aynı olduğu doğrulandı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M43-1 | `Kuyumcu.Web.csproj`'a `<Content Remove="wwwroot\kilavuz.html" />` eklendi, yayın fikstüre yeniden üretildi, 5198'de servis edildi | `YayinVarlikDumanTestleri.KilavuzHtml_KaynaktaVarVeBosDegil` ("Kuyumcu.Web.csproj: <Content Remove=…>") · `duman-testi.sh::test_kilavuz_html_200_ve_60kb` → **`✗ kilavuz.html: 404 / 0 bayt`**, çıkış 6. Ayrıca P-41'in `yayinla.sh` kontrolü de "YAYINDA OLMAYAN KAYNAK DOSYASI: wwwroot/kilavuz.html" ile reddetti (iki katmanlı savunma) | cmp AYNI |
| M43-2 | Fikstür yayınında `_framework/Kuyumcu.Core.wasm` `printf '' >` ile 0 bayta indirildi | `YayinVarlikDumanTestleri.BootJsonVarliklari_SifirBaytDegil` ("Kuyumcu.Core.wasm (assembly) 0 bayt") · `duman-testi.sh::test_bootjson_varliklari_content_length_sifirdan_buyuk` ("200 / Content-Length 0", "65/66 varlık sağlam") | cmp AYNI; fikstür bütünlüğü TEMİZ (76 kayıt) |
| M43-3a | Kilavuz.razor'a `("yok-bolum", …)` çıpası eklendi | `YayinVarlikDumanTestleri.KilavuzRazor_TumBaglantilarininKarsiligiVar` ("15 bağlantıdan 1'i karşılıksız") | cmp AYNI |
| M43-3b | Kilavuz.razor'daki düz href `/kilavuz-eski.html` yapıldı | `KilavuzRazor_TumBaglantilarininKarsiligiVar` ("wwwroot/kilavuz-eski.html YOK") | cmp AYNI |
| M43-4 | kapi.sh'teki duman çağrısının sonuna `\|\| true` eklendi | `duman-testi.sh::test_kapi_duman_kirmiziyken_temiz_yazmaz` ("'\|\| true' ile YUTUYOR"), çıkış 6 | cmp AYNI |
| M43-5 | qa-turu.js'te `if (hepsiEngelli && ortak.length > 0)` → `if (false && …)` (engellenen grup filtresi kaldırıldı) | `duman-testi.sh::test_engellenen_grup_ortalamadan_ayrilir` (3 alt kontrol: "FARKLI metin", "engellenen grup ayrı raporlanmıyor", "3,50 / 3,00 çıkmadı"; ölçülebilen ortalama 3,17/2,74'e düştü) + `test_ortalama_blogu_iki_dosyada_ayni` | cmp AYNI |

**Test açığı bulundu ve kapatıldı:** M43-5'in ilk koşumunda "ölçülemedi raporlandı" kontrolü yeşil kaldı. Sebebi: `ölçülemedi: YOK` metni de kalıpla eşleşiyordu. Kontrol `ölçülemedi (ortalamaya KATILMADI): grup 1 · 6 rota` olarak sıkılaştırıldı ve mutasyon yeniden koşturuldu; kontrol kırmızıya döndü.

Son mutasyondan sonra temiz derleme yapıldı: 0 hata.

## 5. Canlı doğrulama

| Adım | Beklenen | Gerçek |
|---|---|---|
| `duman-testi.sh`, tam yayın (sürümlü `yayinla.sh` → `scratchpad/p43-yayin`, bütünlük TEMİZ 76 kayıt), 5198 | çıkış 0 + "66/66 varlık — Content-Length > 0" | **çıkış 0**, `✓ 66/66 varlık — Content-Length > 0`, `✓ canlı: /kilavuz.html 200 / 60845 bayt`, 6 rotanın 6'sı 200, "geçen 21 · ihlal 0" |
| kilavuz.html yayından çıkarıldı (M43-1), 5198 | çıkış ≠ 0 + "kilavuz.html: 404 / 0 bayt" | **çıkış 6**, `✗ kilavuz.html: 404 / 0 bayt` |
| Tarayıcı (uygulama içi bölme), 5198: /cikti/satis/1, /kasalar, /sayim, /banka, /pos/tahsilatlar, /yetkiler | 200 + konsolda "Failed to start platform" YOK | 6'sında da `Blazor=object`, `#app` dolu, `#blazor-error-ui` görünmüyor. Yönlendirici oturum olmadığı için `/giris`'e yönlendirdi; bu da uygulamanın açıldığının kanıtı. Konsolda "platform" kalıbıyla **0 kayıt**, hata düzeyinde **0 kayıt** |
| **5199 (kurucunun sunucusu; yalnız okundu)** | Kapı gerçeği yazmalı | **çıkış 6.** `✗ kilavuz.html: 404 / 0 bayt` ve `✗ canlı sunucu BAŞKA bir yayını servis ediyor (d2fa9d5e… ≠ 3c635cdd…)`. 66/66 varlık sağlam, 6 rota 200 |
| `kapi.sh`, duman kırmızıyken (5199) | "TEMİZ" yok, çıkış ≠ 0 | **çıkış 7**, çıktıda `grep -c TEMİZ` = **0**, "kapı KAPANDI, denetim/test koşturulmadı" |
| `qa-ozet.mjs --ozet ham/qa-turu-8.json` | İki ortalama ayrı; ölçülebilen 3,50 / 3,00 | `tüm rotalar n=46 · 3,17 / 2,74` · `ölçülebilen rotalar n=40 · 3,50 / 3,00` · `ölçülemedi: grup 1 · 6 rota` |

- **SQL:** Bu paket veri yazmıyor ve test kiracısı AÇILMADI. Kayıt için davet jetonu gerekiyor; önyükleme kanıtı oturumsuz alındı.
- **tenants:** Başlangıçta 9, sonda 15. Artış bu paketten gelmiyor: yeni 6 satırın hepsi `PAKET-P-D3-TEST` (16:00–16:05). Bu, paralel P-D3 şeridinin temizlemediği kiracılar. P-43 hiçbir kiracı açmadı ve silmedi; `QA-TEST-*` kiracılarına dokunulmadı.
- **5198:** Kendi süreçlerim (17201, 34866 ve son koşum) `kill` ile durduruldu. `lsof :5198 LISTEN` boş.
- **Geçici dosyalar:** 5198 için yazılan geçici ortam dosyaları `printf ''` ile boşaltıldı.

## 6. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| duman-testi.sh tam yayında çıkış 0 + "66/66 varlık - Content-Length > 0" | KARŞILANDI | 5198 fikstür koşumu (`duman-son.log`) |
| kilavuz.html yayından çıkarılınca çıkış ≠ 0 + "kilavuz.html: 404 / 0 bayt" | KARŞILANDI | M43-1 (`m1d.log`) |
| 6 rota 200 + konsolda "Failed to start platform" yok | KARŞILANDI (5198). 5199'da 6 rota 200 ama 5199 eski yayını servis ediyor | Tarayıcı ölçümü, §5 |
| kapi.sh duman kırmızıyken "TEMİZ" içermez | KARŞILANDI | 5199'a karşı kapi.sh çıkış 7, TEMİZ sayısı 0 |
| Grup tamamen ENGELLENDİ → `--ozet` iki ortalamayı ayrı basar | KARŞILANDI | qa-ozet.mjs çıktısı |
| Tur 8 verisi yeniden işlenince "ölçülebilen" 3,50 / 3,00 | KARŞILANDI | aynı çıktı |
| Test listesi (4 C# testi + 5 betik testi) | KARŞILANDI (+1 ek test) | §2 |
| Mutasyon listesi (5 madde) | KARŞILANDI (6 koşum) | §4 |
| Paket kapısı (7): yeni kırmızı yok | KARŞILANDI | 4089/4088/1, fark 0 |

## 7. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeci; betikler dizininin tamamı git'te izlenmiyor, `??`)

- `dokumanlar/09-uygulama-kayitlari/betikler/duman-testi.sh` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-ozet.mjs` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js` (P-43 bloğu ve OLCUM entegrasyonu)
- `dokumanlar/09-uygulama-kayitlari/betikler/kapi.sh` (1b/4 aşaması)
- `dokumanlar/09-uygulama-kayitlari/ham/TEK-EL-KILIT` (aç-kapa kaydı)
- `dotnet/Kuyumcu.Tests/YayinVarlikDumanTestleri.cs` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-43-uygulama-raporu.md` (bu rapor)
- *(Mutasyonla geçici olarak açılıp birebir geri alınanlar: `Kuyumcu.Web.csproj`, `Pages/Kilavuz.razor`; son içerik yedekle `cmp` aynı.)*

Dokunulan sayfalar: yok (Kilavuz.razor yalnız mutasyon için açıldı, birebir geri alındı).

## 8. Açık kalanlar (sahibiyle)

1. **5199 eski yayını servis ediyor** — sahibi **kurucu/orkestratör.**
   - 5199 süreci (PID 12577, dotnet + TLS köprüsü PID 12590) `…/f8597a15…/scratchpad/qa-yayin`'den koşuyor; P-42'nin kalıcı dizinini (`~/.kuyumcu-yayin`) servis etmiyor.
   - Bu yüzden canlıda `/kilavuz.html` 404 ve kapı kırmızı.
   - Çözüm: `zsh betikler/qa-sunucu.sh` ile 5199 yeniden açılmalı.
   - O yapılana dek `kapi.sh` çıkış 7 verir. Bu bilinçli bir fail-closed davranış: kapı gerçeği söylüyor.
2. **Tam süitteki tek kırmızı** — `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`, sahibi **P-30b (DALGA 7c)**. Devirden kalmış; P-43 dokunmadı.
3. **2 derleme uyarısı** — CS8604, `OlcumKapisiTestleri.cs:525`. Sahibi **DALGA 7c.**
4. **6 PAKET-P-D3-TEST kiracısı** — DB'de kalmışlar. Sahibi **P-D3 şeridi.** tenants 9 → 15.
5. **Tur 8'de engellenen 6 rotanın 40 eski bulgusu** — hâlâ DOĞRULANAMADI. Bu paket ölçüm aracını onardı, bulguları kapatmadı. Tur 9'da bu rotalar yeniden taranmalı; aracın "ölçülemedi" satırı da bunu hatırlatıyor.
6. **Oturumlu sayfa gövdesi doğrulanmadı** — 5198 tarayıcı doğrulaması oturumsuz yapıldı (önyükleme kanıtı). Kiracı açmak davet jetonu gerektirdiği için sayfaların oturumlu hâli bu pakette görülmedi. Ölçülen şey "Failed to start platform"un yokluğu; bu önyükleme düzeyinde bir hata ve oturumdan bağımsız.
7. **P-43 için denetim.sh sayacı yok** — denetim.sh DALGA 7c'nin dosyası. İstenirse orkestratör sırası geldiğinde `kapi.sh`'te 1b aşamasının bulunduğunu sayan bir sayaç eklenebilir.

## 9. Kurucuya notlar

- Mali müşavire bir not yok; paketin mevzuatla ilgisi yok.
- Kurucuya tek eylem kalıyor: 5199'u `qa-sunucu.sh` ile yeniden başlatın. Tur 9 bundan sonra koşmalı, çünkü kapı yeşile dönmeden tur rakamları yine şüpheli olur.
