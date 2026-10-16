# PAKET Q10-3: İşletme kimliği (adres, telefon, vergi dairesi, VKN/TCKN) uygulama raporu

**Rol:** Taşkın (backend) ve Ertuğ (frontend), Opus uygulayıcı · **Dalga:** DEMO DÜZELTME Q10-A · **Tarih:** 25.09.2026 (gece)
**İzolasyon:** test veritabanı `kuyumcu_test_l`, canlı API 5174, vekil 5173, CDP Chrome 9348 (üçü de kapatıldı). 5199, 5177, 5178, 5179, 5181 ve 5200 portlarına dokunulmadı.
**Durum (tur 2, 25.09.2026 02:25):** **BİTTİ.** Paket kapısının yedi maddesi yeniden karşılandı; tam süit **5782/5782 geçti, 0 başarısız**; `dotnet/denetim.sh` **DENETİM TEMİZ** (288 ✓). Tur 2 ayrıntısı hemen aşağıda (§ T2); §1–§9 tur 1 kaydıdır, tur 2 ile değişen yerler § T2'de yazılıdır.

---

## § T2 — Tur 2: doğrulayıcı bulgularının kapanışı (25.09.2026 01:45–02:25)

**İzolasyon:** test DB `kuyumcu_test_l`, canlı API 5174 (`kuyumcu_net`), vekil 5173, tarayıcı bölmesinde KENDİ sekmem `tab-64`, PNG için ayrı headless Chrome CDP 9352. Üçü de iş sonunda durduruldu (`lsof` 5174/5173/9352 boş). 5199, 5177/5178, 5179/5181 ve 5200'e dokunulmadı.

### T2.1 Bulgu bulgu kapanış

| Bulgu | Tur 2'de yapılan | Kanıt |
|---|---|---|
| **[KRİTİK] Hakem çelişkisi: gerçek tarayıcı ekran görüntüsü** | Ölçüm bu turda **tarayıcı bölmesinde, kendi sekmemde (tab-64)** yeniden yapıldı. `sikisma-olc.js` çalıştırıldı ve her kombinasyonda bölmenin kendi ekran görüntüsü alındı: 1280 ve 375 × BASİT ve PRO × açık ve koyu, sayfa ve çekmece ayrı kökle. Görüntü dosyası olarak saklanabilsin diye aynı 8 kombinasyon ile 4 çıktı belgesi headless Chrome (CDP) ile de PNG'ye yazıldı. | § T2.4 tablosu · `olcumler/Q10-3/q103b-*.png` (12 dosya) · `olcumler/Q10-3/sikisma-olcum-q103b.txt` |
| **[DÜŞÜK] Tek karakterli işletme adı** (`PATCH {ad:' x '}` → 200, ad=`x`) | **Kök neden:** Controller'daki `StringLength(2..200)` kırpılmamış metni ölçüyordu; servis ise yalnız boşluk kontrolü yapıyordu. **Düzeltme:** Kural tek kaynağa taşındı: `Kuyumcu.Core.Domain.IsletmeAdi` (`EnAz=2`, `EnCok=200`, uzunluk kırpılmış metinde ölçülür). Servis (`AdminServisi.KimlikGuncelle`) bu kuralı elle çağırır; ekran (`Isletme.razor`) da aynı kuralı çağırır. Hatalı adda alan altında Türkçe mesaj çıkar, Kaydet kilitlenir ve sebebi yazılır, istek gitmez. | Canlı: `' x '` → **400** "İşletme adı 2-200 karakter olmalıdır", ad değişmedi. `' Q103 Kuyumculuk '` → 200, kırpılarak yazıldı. Tarayıcıda (1280 PRO koyu ve 375 BASİT koyu) alan altında "İşletme adı en az 2, en çok 200 karakter olmalı." çıktı, Kaydet kilitlendi. Testler: `Q103_S04` ×8, `Q103_S05`, `Q103_D02` ×2, `Q103_H06`, `Q103_E05` |
| **[DÜŞÜK] Adres tek satırlık input'ta kayıyor** | `CokSatir` (çok satırlı alan) **denendi ve gerçek tarayıcıda GERİ ALINDI.** Ölçüm: `.girdi-cok-satir` yazı ailesini devralmıyor; computed `font-family: monospace`, komşu alanlarda ise `Manrope`. Adres alanı çekmecede tek başına eşit aralıklı yazıyla çıkıyordu. Düzeltmesi `app.css` / `Bilesenler/Form/*` alanında; ikisi de Q10-B şeridinin dosyası ve bu şeride yasak. Alan `Metin` olarak kaldı; yalnız razor yorumu eklendi. Bulgunun kendisi öneri düzeyindeydi ("ileride düşünülebilir"). | § T2.6 madde 2 (sahibi Q10-B) |
| **[DÜŞÜK] denetim.sh P-37c muaf=3** | Bu şeridin işi değildi. Tur 2 koşumunda sayaç kendiliğinden **temiz**: `DENETİM TEMİZ.`, 288 ✓, çıkış 0. Sahibi düzeltmiş. | `denetim.sh` çıktısı § T2.3 |
| **[DÜŞÜK] Denetim izinde maskeli eski/yeni değer** | Kod değiştirilmedi. Sapma bilinçli: tam VKN ya da telefon detaya hiç düşmüyor (SQL: tam VKN geçen satır 0, tam telefon geçen satır 0). Onay kararı **Mehmet'e (06 Güvenlik)** ait; bu uygulayıcı onay yerine geçemez. Açık kalan olarak yazıldı. | § T2.6 madde 3 |

### T2.2 Değişen dosyalar (tur 2)

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Core/Domain/VergiKimlik.cs` | Yeni `IsletmeAdi` sınıfı eklendi (`Gecerli`, `Mesaj`, `EnAz`, `EnCok`); kırpılmış uzunluk kuralı. |
| `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs` | `KimlikGuncelle` içinde servis içi elle kontrol: `IsletmeAdi.Gecerli` yanlışsa `BadRequestException(IsletmeAdi.Mesaj)`. |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.IsletmeKimlik.cs` | `AdUzunluk` metni TR ve EN olarak eklendi. Konumsal kayıt olduğu için eksik dil derleme hatası verir. Hub dosyasına dokunulmadı. |
| `dotnet/Kuyumcu.Web/Pages/Isletme.razor` | `AdUzunlukHatali` ve `AdMesaji` eklendi. `ikimlik-ad` alanının hata metni bu mesajdan geliyor. `KimlikGecerli` ve `KimlikKilitSebebi` ad mesajını kullanıyor. Adres alanına geri alma gerekçesini anlatan yorum eklendi. Id'ler, rota ve CSS değişmedi; satır içi style yok. |
| `dotnet/Kuyumcu.Tests/Q103IsletmeKimlikTestleri.cs` | 13 yeni test: `Q103_S04` ×8, `Q103_S05`, `Q103_D02` ×2, `Q103_H06`, `Q103_E05`. `Q103_E06` (textarea) CokSatir geri alındığı için kaldırıldı. |

**Dokunulan sayfalar (TEK DOKUNUŞ):** yalnız `Pages/Isletme.razor`. Bu, tur 1'de açılan aynı sayfanın aynı paket içinde doğrulayıcı bulgusuyla yeniden açılmasıdır; gerekçe bu tablo.
**Dokunulmayanlar:** Q10-B dosyaları, `app.css`, `Bilesenler/Form/*`.
**Migration:** gerekmedi, şema değişmedi.

### T2.3 Derleme, test ve denetim

- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 hata**, 1 uyarı. Uyarı başka dosyada: `EtiketEkranTestleri.cs(145) xUnit1031`.
  - Ara derlemede bu paketin testinde bir `xUnit1041` uyarısı çıktı. `[Collection("veritabani")]` yanlış sınıfa kaymıştı; düzeltildi.
- **Filtreli koşum** (Q103, IsletmeBilgileri, UX1C4Ekran, IsletmeEkran, S3f, P13KartAyarUyum): **163/163 geçti** (tur 1'de 150).
  - Yalnız Q103: **49/49** (36'dan 49'a).
- **Tam süit** (`trx-pQ10-3/pQ10-3-son.trx`): **5782 toplam, 5782 geçti, 0 başarısız**, süre 2,06 dk.
  - Tur 1'deki 12 başka şerit kırmızısı bu koşumda yok.
  - Paketin yeni kırmızı ürettiği yok: fark 0.
- **`dotnet/denetim.sh`:** çıkış 0, `DENETİM TEMİZ.`, 288 ✓.
  - `IgnoreQueryFilters 0`, satır içi `style=` 105 (tavan 196), `Pages: ModalPencere` 48 (tavan 55), `SABIT_RENK 0`.

### T2.4 Mutasyonlar (tur 2)

**Yöntem:**
1. Yedek alındı: `scratchpad/mut-yedek/Q10-3b/`.
2. `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı.
3. `dotnet build Kuyumcu.Tests` ve filtreli `dotnet test -v n` koşuldu.
4. Yedek geri kopyalandı ve `cmp` ile aynı olduğu doğrulandı.

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` 0 hata verdi.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M2-1 | Servis içi ad kontrolü `false &&` ile kapatıldı | `Q103_H06_Tek_karakterli_ad_kirpilinca_400_ve_ad_DEGISMEZ`, `Q103_D02(" x ")`, `Q103_D02("x")` | AYNI |
| M2-2 | `IsletmeAdi.Gecerli` kırpmadan ölçüyor (`ad?.Trim()` → `ad`) | `Q103_S04` (`"   "`, `" x "`, `"  x  "`), `Q103_S05`, `Q103_E05`, `Q103_H06`, `Q103_D02(" x ")`: 7 kırmızı | AYNI |
| M2-3 | İstemci `AdUzunlukHatali => false` | `Q103_E05_Tek_karakterli_ad_alan_alti_mesaj_kilit_ve_istek_GITMEZ` | AYNI |
| M2-4 | Adres `CokSatir` → `Metin`. Bu mutasyon, sonradan geri alınan `CokSatir` sürümü için koşuldu. | `Q103_E06`: test, CokSatir geri alınınca kaldırıldı | AYNI |
| M2-5 | Üst sınır `EnCok = 200` → `201` | `Q103_S05_Isletme_adi_ust_siniri_200_KIRPILMIS` | AYNI |

Tur 1 mutasyonları (M1-1 … M1-11) § 5'te. İlgili kod tur 2'de değişmedi; kilitleyen testler 163/163 yeşil.

### T2.5 Canlı doğrulama (API 5174, `kuyumcu_net`, kiracı `PAKETQ10-3-TEST` → id `01M3ATJP4H7P0DGW64CAH2XSVF`)

Kurulum:
- Rastgele sistem anahtarı üretildi; değeri hiçbir çıktıya yazılmadı. Dosyası iş sonunda `printf ''` ile boşaltıldı.
- `POST /sistem/davetler` **201** döndü, ardından `POST /auth/kayit` **201**.
- Başlangıçta tenants **12**.

**API sınır tablosu** (yanıt gövdeleri okundu):

| İstek (PATCH `/admin/tenant/kimlik`) | HTTP | Yanıt |
|---|---|---|
| `ad=' x '` | **400** | "İşletme adı 2-200 karakter olmalıdır" (servis yolu, yeni) |
| `ad='x'` | 400 | aynı mesaj (controller özniteliği) |
| `ad=' Q103 Kuyumculuk '` | 200 | `ad: "Q103 Kuyumculuk"` (kırpıldı) |
| `vergiNo=''` | 200 | `vergiNo: null` |
| 9 hane | 400 | "Vergi no 10 hane, TC kimlik no 11 hane olmalıdır" |
| 10 hane / 11 hane | 200 / 200 | `1234567890` / `12345678901` |
| 12 hane | 400 | hane mesajı |
| harfli `12345abcde` / boşluklu `12345 67890` | 400 / 400 | "…yalnız rakamlardan oluşmalıdır (harf, boşluk ya da işaret olamaz)" |
| dış boşluklu `' 1234567890 '` | 200 | `1234567890` |
| dört alan birlikte | 200 | dört alan dolu döndü |
| yalnız `adres` | 200 | telefon, vergi dairesi ve vergi no **korundu** |
| `telefon=abc` | 400 | mevcut telefon kalıbının mesajı |
| GET `/admin/tenant` | 200 | yeni değerler |

**Gerçek tarayıcı senaryosu** (tab-64, 1280 PRO koyu):
1. Çekmece açıldı ve beş alan göründü.
2. Ad alanına ` x ` yazıldı: alan altında ad mesajı çıktı, Kaydet kilitlendi, sebep yazıldı.
3. VKN alanına `123456789012` yazıldı: hane mesajı çıktı, Kaydet kilitli kaldı.
4. VKN alanına `…12345 67…` yazıldı: rakam mesajı çıktı, Kaydet kilitli kaldı.
5. Hatalı girdiler boyunca `TenantKimlik` denetim satırı **6'da kaldı**; istek gitmedi.
6. Geçerli değerler yazılıp Kaydet'e basıldı. "Kaydedildi" bandı çıktı ve kart yeni değerleri gösterdi. Denetim satırı **7**'ye çıktı.

**375 BASİT koyu:** ` x ` ve 9 hane birlikte girildi. İki alan altı mesajı çıktı, kilit sebebi yazıldı. Çekmece sayaçları hata durumunda da 0. İptal edilince kayıt değişmedi; yeniden açılışta kayıtlı değerler geldi.

**SQL kanıtı:**
```
tenants: ad=Q103 Kuyumculuk Ltd. · adres=Kapalıçarşı Q103 Sok. No:21 Fatih/İstanbul · telefon=02125550177 · vergi_dairesi=Galata · vergi_no=98765432109
denetim_kayitlari (TenantKimlik): n=7 · tam VKN geçen 0 · tam telefon geçen 0
son satır: işletme kimliği güncellendi · değişen alanlar: ad, adres, vergi_dairesi, vergi_no, telefon · eski → yeni: ad: "Q103 Kuyumculuk" → "Q103 Kuyumculuk Ltd."; adres: "Kuyumcular Ç…" → "Kapalıçarşı …"; vergi_dairesi: "Beyazıt" → "Galata"; vergi_no: ******7890 → *******2109; telefon: *******0142 → *******0177
```

**Çıktı belgeleri:** tur 1'de ölçülmeyen gider pusulası bu turda ölçüldü.
- **Müşteri ekstresi:** `POST /cari` 201, ardından `GET /cikti/CARI_EKSTRE/{cari}` 200. `duzenleyen` alanı yeni unvan, adres, VD, VKN ve telefonu taşıyor.
- **Gider pusulası:** `POST /islemler/hurda-alis` 200 döndü ve `GP-2026-000001` üretildi. Satıcı hayali; TCKN algoritmasından geçen test numarası kullanıldı. `GET /gider-pusulalari/{id}/belge` 200; `duzenleyen` yeni beş değeri taşıyor.
- **Tarayıcıda:** iki belge sayfasının metninde beş yeni değer var (true×5, 375 ve 1280). Görüntüler: `q103b-cari-ekstre-*.png` ve `q103b-gider-pusulasi-*.png`.

**Sıkışma ölçümü:** tarayıcı bölmesi, kendi sekme tab-64. Sayfa ölçümü çekmece KAPALI yapıldı, çekmece `#cekmece-isletme-kimlik` kökünden ayrıca ölçüldü.

| Rota × genişlik × mod × tema | alan | Sayfa K1/K3/K5/K6/K7/K8/K9/K10/K11/yatay · K2 | Çekmece aynı sayaçlar | Bölme görüntüsü | PNG |
|---|---|---|---|---|---|
| /isletme 1280 PRO koyu | 5 | hepsi 0 · K2 %98 | hepsi 0 | alındı | `q103b-profesyonel-karanlik-1280.png` |
| /isletme 1280 PRO açık | 5 | hepsi 0 · K2 %98 | hepsi 0 | alındı | `q103b-profesyonel-aydinlik-1280.png` |
| /isletme 1280 BASİT açık | 5 | hepsi 0 · K2 %98 | hepsi 0 | alındı | `q103b-basit-aydinlik-1280.png` |
| /isletme 1280 BASİT koyu | 5 | hepsi 0 · K2 %98 | hepsi 0 | alındı | `q103b-basit-karanlik-1280.png` |
| /isletme 375 BASİT koyu (+hata durumu) | 5 | hepsi 0 · K2 %97 | hepsi 0 (hata durumunda da) | alındı (2) | `q103b-basit-karanlik-375.png` |
| /isletme 375 BASİT açık | 5 | hepsi 0 · K2 %97 | hepsi 0 | alındı | `q103b-basit-aydinlik-375.png` |
| /isletme 375 PRO açık | 5 | hepsi 0 · K2 %97 | hepsi 0 | alındı | `q103b-profesyonel-aydinlik-375.png` |
| /isletme 375 PRO koyu | 5 | hepsi 0 · K2 %97 | hepsi 0 | alındı | `q103b-profesyonel-karanlik-375.png` |
| /cikti/CARI_EKSTRE 375 PRO koyu · 1280 PRO açık | — | hepsi 0 · K2 %97 / %90 | — | alındı | `q103b-cari-ekstre-375/1280.png` |
| /gider-pusulalari/{id}/belge 375 PRO koyu · 1280 PRO açık | — | hepsi 0 · K2 %97 / %90 | — | alındı | `q103b-gider-pusulasi-375/1280.png` |

**Ölçüm notları:**
- K5 (TRY) her ölçümde 0. K2 her ölçümde %60'ın üzerinde.
- Tur 1'de "1280'de sayfa K9=3" gibi görünebilecek değerler, çekmece açıkken sayfanın ölçülmesinden gelir: çekmece sayfa başlığının üstüne biner. Bu tur sayfa ölçümü çekmece kapalıyken yapıldı ve K9=0.
- **Tema değiştirme:** uygulamanın kendi `kuyumcuTema.uygula` işleviyle yapıldı; `localStorage` 'kuyumcu-tema' ile birlikte. Ölçümde `data-tema` doğrulandı.
- **Mod değiştirme:** üst çubuktaki `#ust-mod-basit` / `#ust-mod-profesyonel` düğmeleriyle yapıldı; ölçüm çıktısının `mod` alanı doğruladı.
- **375 px'te BASİT ve PRO:** çekmece ekranın tamamını kaplıyor, bu yüzden iki PNG piksel olarak aynı (dosya boyutları eşit). Tur 1'deki eşit boyutlu PNG'lerin açıklaması da budur.
- **İstek sınırı (429):** PNG turunda bir kez `/auth/yenile` sınırına takılındı (`profesyonel-aydinlik-1280`). 40 sn beklenip yeniden çekildi; dosya 429 bandı içermiyor.

**Temizlik:**
- API 5174, vekil 5173 ve Chrome 9352 durduruldu; `lsof` üçü için boş.
- Sekme tab-64 kapatıldı; görüntü alanı `desktop`'a döndü.
- Kiracı `01M3ATJP4H7P0DGW64CAH2XSVF`, `tenant_id` kolonu olan **70 tablodan** silindi; kalan satır **0**. `tenants` satırı ve `kiraci_davetleri` satırı da silindi.
- **tenants 12 → 12.** `QA-TEST-%` fikstürleri (4 kiracı) dokunulmadan duruyor.
- Fikstür ve anahtar dosyaları `printf ''` ile boşaltıldı.
- Davet üretiminin sistem kiracısı altındaki denetim izi satırı silinmedi (denetim izidir).

### T2.6 Açık kalanlar (tur 2, sahipleriyle)

1. **Tam süit ve denetim:** açık yok. 5782/5782 geçti, DENETİM TEMİZ.
2. **`.girdi-cok-satir` yazı ailesini devralmıyor** (computed `monospace`).
   - **Etki:** `CokSatir` kullanan her ekran (Cariler notlar, POS iptal gerekçesi…) komşu alanlardan farklı yazıyla çiziliyor.
   - **Sahibi:** `app.css` / `Bilesenler/Form` → **Q10-B şeridi**.
   - **Öneri:** `.girdi-cok-satir { font: inherit; }`. Düzeltme gelince `ikimlik-adres` çok satırlı alana alınabilir (500 karakter adres).
3. **Denetim izinde maskeli eski/yeni değer:** DenetimIzi "değerler yazılmaz" kuralından bilinçli sapma. **Mehmet (06 Güvenlik) onayı bekliyor.** Tam değer 0 satırda geçiyor (SQL).
4. **Mevzuat tutarlılık notu (orkestratör / mali müşavir):**
   - İşletme VKN/TCKN'sine sağlama uygulanmadı; birincil kaynak yok (§ 8).
   - Ancak kod tabanında `GiderPusulasiKapisi.TcknGecerli` (`Modules/Hurda/GiderPusulasiKapisi.cs:207`) gider pusulası **satıcısının** TCKN'sine sağlama algoritmasını zaten uyguluyor ve yorumunda birincil kaynak yok.
   - Aynı kavrama iki ekranda iki kural uygulanıyor (UX-S31). Karar orkestratöre ve mali müşavire bırakıldı: ya o algoritma birincil kaynakla belgelenir ve buraya da uygulanır, ya da orada da yapılandırmaya alınır. Bu şerit o dosyaya dokunmadı.
5. **Paket dışı gözlemler** (ekranda görüldü, bu şeridin dosyası değil):
   - `/isletme` sayfasında "İşletme ve Şubeler" başlığı iki kez görünüyor: sayfa başı ve h1.
   - 375 px'te çıktı ve gider pusulası sayfalarındaki "← Geri" / "Pusulaya dön" düğmeleri tam genişlikte, metin alta yaslı (ekran görüntüsü `q103b-*-375.png`).

---

# TUR 1 KAYDI (önceki koşum — değişmeden korunur)

## 1. Bulgunun yeniden üretimi (koda karşı doğrulama)

QA turu 10 KRİTİK bulgusu: "İşletme kimliğini düzenle" çekmecesinde yalnız `ikimlik-ad` vardı; dört alan hiçbir yerden düzenlenemiyordu.

**Kök neden (kodda doğrulandı):** Sunucu ucu `PATCH /admin/tenant/kimlik` ve ekrandaki dört alan zaten vardı. Ancak `Isletme.razor` bu dört alanı `<ProAlan>` içine koymuştu.
- **BASİT modda:** alanlar çizilmiyordu. `KimlikKaydet` de gövdeye yalnız `{ ad }` koyuyordu.
- **QA ölçümü:** QA oturumu BASİT moddaydı ve DOM'da tek girdi saydı. Kod ile bulgu birebir tutuyor.
- **Doğrulama:** CDP ölçümünde düzeltmeden sonra her modda beş alan sayıldı (§7).

Koda bakınca ortaya çıkan ek kusurlar da giderildi:
1. **Unicode rakam kabulü:** Sunucu kuralı `^\d{10,11}$` idi. .NET'te `\d` Unicode rakamlarını da eşler; `١٢٣٤٥٦٧٨٩٠` (Arapça-Hint rakamları) 200 ile kaydediliyordu.
2. **Tek tip hata mesajı:** Harfli ya da boşluklu girdide de "10 ya da 11 hane" mesajı dönüyordu; hatanın türü ayrılmıyordu.
3. **Sessiz kırpma (gerçek tarayıcıda bulundu):** `maxlength=11` idi. 12 haneli bir girdi sessizce 11 haneye kırpılıyor ve **başka bir numara olarak kaydedilebiliyordu.** Ölçüm: `123456789012` yazıldı, kutuda `12345678901` kaldı, hata yok, Kaydet açık.
4. **Değersiz denetim izi:** Denetim kaydı yalnız değişen alan adlarını yazıyordu. Paket koşulu eski ve yeni değeri istiyor.

## 2. Yapılanlar

| Katman | Dosya | Değişiklik |
|---|---|---|
| Core (tek kural) | `dotnet/Kuyumcu.Core/Domain/VergiKimlik.cs` (yeni) | `VergiKimlik.Denetle/Mesaj`, `VergiKimlikHatasi {Yok, RakamDisi, HaneSayisi}`. Yalnız ASCII `[0-9]`, 10 ya da 11 hane. Dıştaki boşluk kırpılır, içteki boşluk hatadır. Boş girdi alanı siler. |
| API | `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs` | **DTO:** `[RegularExpression(\d…)]` yerine `[VergiKimlikBicimi]` özniteliği; controller yolunda da servisle aynı mesaj döner.<br>**Servis:** `KimlikGuncelle` içinde elle kontrol `VergiKimlik.Mesaj`'ı çağırır.<br>**Denetim:** kayıt artık `eski → yeni` taşır (`DenetimDegeri`): vergi_no ve telefon maskeli (son 4 hane, MASAK `Maskele` kalıbı), adres ilk 12 karakter, ad ve vergi dairesi en çok 60 karakter. `DenetimServisi.YazmaKaydet` çağrılır, yeniden yazılmadı. |
| Sözlük | `dotnet/Kuyumcu.Core/I18n/Sozlukler.IsletmeKimlik.cs` (yeni) | TR ve EN: `VergiNoRakamDisi`, `VergiNoHane`, `VergiNoIpucu`, `BelgeNotu`. Konumsal kayıt olduğu için eksik dil derleme hatası verir. Ana sözlük dosyasına (HUB) dokunulmadı. |
| Ekran | `dotnet/Kuyumcu.Web/Pages/Isletme.razor` | `<ProAlan>` sarmalı kaldırıldı: beş alan her modda çiziliyor (≤ 8 alan, UX-3).<br>Gövde her modda beş alanı taşıyor.<br>VKN alanında ipucu ve türüne göre alan altı mesaj var; Kaydet kilidinin sebebi aynı mesaj.<br>`maxlength` 11'den 20'ye çıkarıldı (sessiz kırpma yok).<br>`#ikimlik-belge-notu` belgelerde nereye basıldığını söylüyor.<br>Mevcut id'ler değişmedi, rota değişmedi, CSS eklenmedi, satır içi style yok. |
| Test | `dotnet/Kuyumcu.Tests/Q103IsletmeKimlikTestleri.cs` (yeni) | 36 test: saf 14 · servis/DB 3 · HTTP 11 · bUnit 8 (ayrıntı §4) |

**Dokunulan sayfalar (TEK DOKUNUŞ):** yalnız `Pages/Isletme.razor`. Eşzamanlı Q10-B şeridinin dosyalarına ve `app.css`'e dokunulmadı.

## 3. Sapmalar ve gerekçeleri

1. **Denetim izine "eski/yeni" yazmak ile `DenetimIzi.Detay` kuralının çatışması.** Paket koşulu eski ve yeni değeri istiyor. `DenetimIzi.cs` ise "DEĞERLER YAZILMAZ" diyor; bu kural Mehmet K3 ve KVKK gerekçesine dayanıyor.
   - **Çözüm:** değerler maskeli ya da kısaltılmış yazılır; tam VKN, TCKN, telefon ve tam adres hiçbir koşulda detaya düşmez.
   - **Kilit:** mevcut test `Kimlik_degisimi_DENETIM_KAYDI_yazar` ("1234567890 detayda yok") hâlâ yeşil. `Q103_H04` iki şeyi birlikte kilitler: eski→yeni metni var, tam değer yok.
   - **Onay gerekiyor:** Mehmet'in (06 Güvenlik) onayına sunulmalı.
2. **`maxlength` 11'den 20'ye çıktı.** Spesifikasyonda yok. Gerçek tarayıcıda sessiz kırpma bulundu (§1 madde 3); UX-S28'in ruhuna aykırıydı.
3. **Sağlama (kontrol hanesi) algoritması UYGULANMADI.** Gerekçe §8'de: birincil kaynak bulunamadı.
4. **"Önce" görüntüsü alınmadı.** Kusurlu derleme yeniden kurulmadı. Önceki durumun kanıtı QA turu 10'un DOM ölçümüdür (`qa-turu-10-bulgular.json`, rota `/isletme`): dialog metni yalnız 'İşletme kimliği · İşletme adı · İptal · Kaydet' idi ve tek girdi `ikimlik-ad` sayıldı. "Sonra" görüntüleri §7'de.

## 4. Testler

- **Filtreli koşum (paket kapsamı):**
  - Q103 · IsletmeBilgileri · UX1C4Ekran · IsletmeEkran · S3f · P13KartAyarUyum: **150/150 geçti, 0 başarısız.**
  - Paketten önce aynı filtre 114 test sayıyordu (150 − 36 Q103; hepsi yeşil kaldı).
  - Yalnız Q103: 36 test, her koşumda 0 başarısız. Maxlength iddiası mevcut `Q103_E01` içine eklendi; test sayısı değişmedi.
- **Yeni testler:**
  - **Saf:** `Q103_S01` sınırlar (12 satır: boş/null/boşluk, 9, 10, 11 ve 12 hane, harfli, iç boşluk, dış boşluk, tire, Unicode) · `Q103_S02` mesaj türü ve TR≠EN · `Q103_S03` maskeleme.
  - **Servis/DB:** `Q103_D01` ×3: servis yolu harfli, 12 hane ve Unicode girdiyi reddeder, satır yazılmaz.
  - **HTTP:**
    - `Q103_H01` ×5: 9 hane, 12 hane, harfli, boşluklu ve Unicode girdi 400 döner; mesaj Türkçe ve türüne göre; satır değişmez.
    - `Q103_H02` ×3: 10 hane, 11 hane ve dış boşluklu girdi 200 döner; kırpılarak yazılır.
    - `Q103_H03`: boş VKN siler, gönderilmeyen alan korunur.
    - `Q103_H04`: denetim izi eski→yeni, maskeli.
    - `Q103_H05`: çıktı belgesi (CARI_EKSTRE) yeni adresi, VKN'yi ve vergi dairesini basar.
  - **bUnit:**
    - `Q103_E01` ×2: BASİT ve PRO modda beş alan ve belge notu; maxlength > 12.
    - `Q103_E02`: BASİT modda gövde dört alanı taşır.
    - `Q103_E03` ×4: harfli, boşluklu, 9 ve 12 hane için alan altı Türkçe mesaj; Kaydet kilitli ve sebebi yazılı; tıklansa da PATCH gitmez.
    - `Q103_E04`: boş VKN açık `null` gönderir.
- **PATCH sözleşmesi** ("gövdede yoksa koru, null ise sil"): `IsletmeBilgileriTestleri.Alan_govdede_yoksa_korunur` ×4 ve `Null_gonderilen_alan_silinir`, ayrıca `Q103_H03`.
- **Tam süit** (`--logger trx`, `trx-pQ10-3`): **5766 test · 5754 geçti · 12 başarısız.** 12 kırmızının hiçbiri bu paketin kodunu kullanmıyor (§9).

## 5. Mutasyon kanıtları

Yöntem: yedek `scratchpad/mut-yedek/Q10-3/` altına alındı → `perl -0777 -pi` ile tek eşleşmeli değişiklik → `dotnet test --filter` → yedek geri kopyalandı → `cmp` birebir aynı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | Dört alan yeniden `<ProAlan>` içine alındı | `Q103_E01(basit:True)`, `Q103_E02`, `Q103_E03` ×4 | AYNI |
| M1-2 | Gövdeden `vergiNo` çıkarıldı | `Q103_E02`, `Q103_E04`, `UX1C4EkranTestleri.C4_10_Isletme_kimligi_cekmeceden_PATCH_edilir` | AYNI |
| M1-3 | `c is < '0' or > '9'` → `!char.IsDigit(c)` (Unicode açığı) | `Q103_S01(Unicode)`, `Q103_H01(Unicode)`, `Q103_D01(Unicode)` | AYNI |
| M1-4 | `Length is 10 or 11` → `>= 10 and <= 12` | `Q103_S01(12 hane)`, `Q103_H01(12)`, `Q103_D01(12)`, `Q103_E03(12)` | AYNI |
| M1-5 | Servis içi VKN kontrolü `false &&` ile kapatıldı | `Q103_D01` ×3 | AYNI |
| M1-6 | Controller özniteliği her hatada "hane" mesajı veriyor (eski kusur) | `Q103_H01` harfli, boşluklu ve Unicode | AYNI |
| M1-7 | Denetim detayından `eski → yeni` eki silindi | `Q103_H04` | AYNI |
| M1-8 | vergi_no maskelenmiyor | `IsletmeBilgileriTestleri.Kimlik_degisimi_DENETIM_KAYDI_yazar`, `Q103_H04`, `Q103_S03` | AYNI |
| M1-9 | İstemci `VergiNoHatali => false` | `Q103_E03` ×4, `UX1C4EkranTestleri.C4_12_Vergi_no_hatali_bicimde_kaydet_KILITLI` | AYNI |
| M1-10 | Sözlükte "rakam dışı" mesajı "hane" mesajına çevrildi | `Q103_E03` harfli ve boşluklu, `Q103_S02` | AYNI |
| M1-11 | `EnFazla="20"` → `"11"` (sessiz kırpma geri) | `Q103_E01(basit:True)`, `Q103_E01(basit:False)` | AYNI |

**M1-11 notu:** İlk koşum GEÇERSİZ sayıldı. Web projesinin `CoreCompile` adımı "güncel" diye atlanmıştı; muhtemel sebep aynı ağaçta koşan paralel derleme. Mutasyon açıkça derlenerek (`dotnet build Kuyumcu.Tests`) yeniden koşuldu ve kırmızı döndü (M1-11b). Diğer on mutasyonun hepsi kırmızı döndüğü için derlendikleri kanıtlı.

**Son mutasyondan sonra:** temiz `dotnet build Kuyumcu.sln` sonucu **0 uyarı, 0 hata.** Ardından filtreli 150/150 geçti.

## 6. Derleme ve denetim

- **Derleme:** `dotnet build Kuyumcu.sln` → 0 hata. Son derlemede 0 uyarı. İlk derlemede görülen 2 uyarı başka dosyalarda: `EtiketEkranTestleri.cs:145 xUnit1031`, `TS2TuvalProTestleri.cs:211 CS8631`.
- **Migration:** gerekmedi. Şema değişmedi; `tenants` kolonları zaten vardı.
- **`dotnet/denetim.sh`:** 287 ✓ / 1 ✗ (çıkış 1). Tek ihlal:
  - `✗ P-37c: SAYFA_BASI_KAPSAM toplam=47 serit=44 muaf=3 acik=0`
  - Muafiyet işareti taşıyan sayfalar: `Ayarlar.razor`, `Ekranlar.razor`, `Panel.razor`.
  - Tavan 2; üçüncü işaret `Ayarlar.razor`'a başka bir şeritte eklendi (dosya zamanı 25.09 01:12). Bu paket `Ayarlar.razor`'a dokunmadı.
- **Bu paketin sayaçları ✓:**
  - `Pages: ModalPencere kullanımı 48 (tavan 55)`: artış yok, Çekmece kullanıldı.
  - `SABIT_RENK 0` · `satır içi style= 105 (tavan 196)` · `IgnoreQueryFilters 0` · `GIRDI_AILESI_TEK_KAYNAK 1`.

## 7. Canlı doğrulama (API 5174, `kuyumcu_net`, kiracı `PAKETQ10-3-TEST`)

Kurulum: kendi API sürecimde rastgele sistem anahtarı kullanıldı (değeri hiçbir çıktıya yazılmadı) → `POST /sistem/davetler` → `POST /auth/kayit` 201. Başlangıçta tenants sayısı **12** idi.

### 7.1 API sınır tablosu (yanıt gövdeleri okundu)

| İstek | HTTP | Yanıt |
|---|---|---|
| PATCH `vergiNo=""` | 200 | `vergiNo: null` |
| PATCH `123456789` (9 hane) | 400 | "Vergi no 10 hane, TC kimlik no 11 hane olmalıdır" |
| PATCH `1234567890` (10) | 200 | `1234567890` |
| PATCH `12345678901` (11) | 200 | `12345678901` |
| PATCH `123456789012` (12) | 400 | "Vergi no 10 hane, TC kimlik no 11 hane olmalıdır" |
| PATCH `12345abcde` (harfli) | 400 | "…yalnız rakamlardan oluşmalıdır (harf, boşluk ya da işaret olamaz)" |
| PATCH `12345 67890` (boşluklu) | 400 | aynı rakam mesajı |
| PATCH `١٢٣٤٥٦٧٨٩٠` (Unicode) | 400 | aynı rakam mesajı (eskiden 200 idi) |
| PATCH dört alan birlikte | 200 | adres, telefon, vergiDairesi ve vergiNo dolu döndü |
| PATCH yalnız `adres` | 200 | telefon, vergi dairesi ve vergi no **korundu** |
| PATCH `telefon=abc` | 400 | mevcut telefon kalıbının mesajı |
| GET `/admin/tenant` | 200 | yeni değerler |
| GET `/cikti/CARI_EKSTRE/{cari}` | 200 | `duzenleyen`: adres, vergiDairesi, vergiNo ve telefon yeni değerlerle |

### 7.2 Gerçek tarayıcı senaryosu

Tarayıcı bölmesinde kendi sekmem (`tab-59`) açıldı, **BASİT mod**, karanlık tema, 1280 px. Adımlar:
1. Çekmece açıldı: beş alan çizildi (eskiden yalnız ad vardı).
2. VKN alanına `12345abcde` yazıldı: alan altında "Yalnız rakam girin: harf, boşluk ya da işaret olamaz."; Kaydet kilitli, sebebi yazılı.
3. `12345 6789` yazıldı: aynı mesaj, Kaydet kilitli.
4. `123456789` yazıldı: "Vergi no 10 hane, TC kimlik no 11 hane olmalı." Kaydet kilitli.
5. `123456789012` yazıldı: 12 hanenin tamamı kutuda kaldı (sessiz kırpma yok), hane mesajı çıktı, Kaydet kilitli.
6. Geçerli değerler yazıldı ve Kaydet'e basıldı: adres "Kuyumcular Çarşısı Q103 Blok B No:12 Beyoğlu/İstanbul", telefon `02125550199`, vergi dairesi `Galata`, VKN `98765432109`. Başarı bandı "Kaydedildi" çıktı; kimlik kartında yeni değerler görüldü.

**Hatalı girdide istek gitmedi, SQL kanıtı:** geçersiz girdiler sırasında `TenantKimlik` denetim satırı sayısı 3'te kaldı. Kaydetten sonra **4** oldu; tek yeni satır yalnız geçerli kayıttan geldi.

### 7.3 SQL kanıtı

```
tenants (id=01M3AR35MS4P9JHMMNX9NBAJ1S):
  adres         = Kuyumcular Çarşısı Q103 Blok B No:12 Beyoğlu/İstanbul
  telefon       = 02125550199
  vergi_dairesi = Galata
  vergi_no      = 98765432109

denetim_kayitlari (TenantKimlik, son satır):
  işletme kimliği güncellendi · değişen alanlar: adres, vergi_dairesi, vergi_no, telefon
  · eski → yeni: adres: "Kapalıçarşı …" → "Kuyumcular Ç…"; vergi_dairesi: "Beyazıt" → "Galata";
    vergi_no: ******7890 → *******2109; telefon: **********1 03 → *******0199
```

**Çıktı belgesi:** `/cikti/CARI_EKSTRE/…` sayfasında "Düzenleyen" bölümü tarayıcıda kontrol edildi. Yeni adres, "Vergi dairesi: Galata" ve "Vergi no / TCKN: 98765432109" görünüyor. Gider pusulası belgesi de aynı `tenants` satırını canlı okuyor (`HurdaModulu` satır 87–116); gider pusulası oluşturulmadığı için o belge ayrıca ölçülmedi.

### 7.4 Sıkışma ölçümü (`sikisma-olc.js` K1–K11)

Ölçüm betiği yalnız kök seçimini değiştiren bir kopyayla koşuldu: sayfa `main` üzerinden, çekmece `#cekmece-isletme-kimlik` üzerinden ölçüldü. Ekran görüntüleri CDP ile gerçek Google Chrome'da (headless=new) çekildi.
- **Neden CDP:** tarayıcı bölmesi ölçümün ortasında gizlendi ve ekran görüntüsü alınamadı ("Browser pane is not displayed").
- **429 notu:** ilk CDP turunda `/auth/yenile` oran sınırına takılındı; sayfada 429 bandı görüldü. Ölçüm her durum arasında 15 sn, mod değişiminde 20+20 sn beklenerek yeniden koşuldu. Aşağıdaki tablo bu ikinci turdur.

| Rota × genişlik × mod × tema | alanlar | Sayfa K1/K3/K5/K7/K8/K9/K10/K11/yatay | Çekmece aynı sayaçlar | Görüntü |
|---|---|---|---|---|
| /isletme 1280 BASİT açık | 5 | 0/0-0/0/0/0/0/0/0/0 · K2 %98 | hepsi 0 | `q103-Basit-aydinlik-1280.png` |
| /isletme 375 BASİT açık | 5 | hepsi 0 · K2 %97 | hepsi 0 | `q103-Basit-aydinlik-375.png` |
| /isletme 1280 BASİT koyu | 5 | hepsi 0 · K2 %98 | hepsi 0 | `q103-Basit-karanlik-1280.png` |
| /isletme 375 BASİT koyu | 5 | hepsi 0 · K2 %97 | hepsi 0 | `q103-Basit-karanlik-375.png` |
| /isletme 1280 PRO açık | 5 | hepsi 0 · K2 %98 | hepsi 0 | `q103-Profesyonel-aydinlik-1280.png` |
| /isletme 375 PRO açık | 5 | hepsi 0 · K2 %97 | hepsi 0 | `q103-Profesyonel-aydinlik-375.png` |
| /isletme 1280 PRO koyu | 5 | hepsi 0 · K2 %98 | hepsi 0 | `q103-Profesyonel-karanlik-1280.png` |
| /isletme 375 PRO koyu | 5 | hepsi 0 · K2 %97 | hepsi 0 | `q103-Profesyonel-karanlik-375.png` |
| /cikti/CARI_EKSTRE 1280 PRO açık | — | hepsi 0 · K2 %90 | — | `q103-cikti-aydinlik-1280.png` |
| /cikti/CARI_EKSTRE 375 PRO açık | — | hepsi 0 · K2 %97 | — | `q103-cikti-aydinlik-375.png` |

- K5 (TRY) her ölçümde 0. K2 (dar sütun) her ölçümde %60'ın üzerinde.
- **Kanıt dosyaları:** `dokumanlar/09-uygulama-kayitlari/olcumler/Q10-3/`: 10 PNG ve ham ölçüm çıktısı `sikisma-olcum.txt`.
- **Ek (paket dışı):** Çıktı sayfası ilk turda BASİT modda ölçüldüğünde K8=3 verdi ("CARİ EKSTRESİ", "cari ekstresi" metinleri). `Cikti` sayfası bu pakette açılmadı; §9'a yazıldı.

### 7.5 Temizlik

- API (5174), vekil (5173) ve CDP Chrome (9348) durduruldu; `lsof` üçü için boş.
- Tarayıcı sekmesi kapatıldı, görüntü alanı `desktop`'a döndürüldü.
- Kiracı `01M3AR35MS4P9JHMMNX9NBAJ1S`, `tenant_id` kolonu olan **70 tablodan** silindi; kalan satır **0**. Davet satırı (`01M3AR35F2HJDNDPTC52NMA44A`) ve `tenants` satırı da silindi.
- **tenants sayısı 12 → 12.** `PAKETQ10-3%` = 0. `QA-TEST-` fikstürlerine dokunulmadı.
- Fikstür, davet ve anahtar dosyaları `printf ''` ile boşaltıldı.

## 8. Mevzuat (MEVZUAT KURALI)

- **Uygulanan hüküm:** yalnız BİÇİM. VKN 10 hane, TCKN 11 hane, yalnız rakam. Hane sayıları kimlik numaralarının tanımıdır; tahmin değildir. NVİ resmî sayfası TCKN'nin 11 basamaklı olduğunu ve son iki basamağın ilk dokuzdan bir algoritmayla hesaplandığını söyler: https://www.nvi.gov.tr/tokat/tc-kimlik-no-islemleri.
- **SAĞLAMA (kontrol hanesi) → DOĞRULANAMADI, UYGULANMADI.**
  - **TCKN:** NVİ sayfası algoritmanın varlığını söyler ama formülü yayımlamaz. Aynı sayfaya göre algoritma kamu kurumlarıyla paylaşılır; kamuya açık bir birincil kaynak değildir. Formülün geçtiği kaynaklar (Wikipedia, gist, gazete haberleri, tcknvkn.com) birincil değildir.
  - **VKN:** GİB'in yayımladığı birincil bir formül bulunamadı. Yalnız üçüncü taraf gist ve Excel forumları var; birincil değiller. GİB'in resmî yolu, türkiye.gov.tr "Vergi Kimlik Numarası Doğrulama" sorgu hizmetidir; algoritma yayını değildir.
  - Sağlama koda sabit yazılmadı. **Mali müşavir / orkestratör notu:** birincil kaynak (GİB ya da NVİ resmî teknik belgesi) sağlanırsa sağlama `VergiKimlik.Denetle` içine tek noktadan eklenebilir. O zamana kadar biçimi doğru ama sağlaması tutmayan bir numara kabul edilir.

## 9. Açık kalanlar (sahipleriyle)

1. **Tam süitte 12 kırmızı.** Hiçbiri bu paketin koduna dokunmuyor. Kırmızı testlerde `Isletme`, `VergiKimlik`, `tenant/kimlik` ya da `AdminServisi` kullanan yok.
   - **IadeEkranTestleri ×7:** `Iade_dogru_uca_KAYNAK_ve_ADET_ile_gonderilir`, `Iade_odeme_secicisinde_ALTIN_YOKTUR`, `Iade_sekmesi_adaylari_SUNUCUDAN_ceker`, `Kalan_hakki_asan_adette_KAYDET_kapanir`, `Kaynak_secilince_KALAN_hak_ve_ORANTILI_tutar_gosterilir`, `Kaynak_secilmeden_KAYDET_kapali`, `Kismi_adette_orantili_tutar_DUSER`. Bu testler Islemler/Pos ekranlarına ait → **Q10-B şeridi.**
   - **PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir** → Pos, **Q10-B.**
   - **UX4bEkranTestleri ×2:** `POS_elle_girilen_hurda_fiyati_ELLE_GIRILDI_rozeti_alir`, `POS_hurda_has_gram_fiyati_KURDAN_gelir_ve_rozeti_soyler` → Pos, **Q10-B.**
   - **SozlesmeKapsamTestleri ×2:** `Kapsam_orani_beyan_edilen_degerle_ayni` (59 beklendi, 58 bulundu) ve `Kilitsiz_tip_listesi_circir_ile_donduruldu` ("TevsikOnizleme artık kilitli — listeden silin"). `TevsikOnizleme` tipi `Contracts/Sayfalar/IslemlerSozlesmeleri.cs` içinde ve `Islemler.Pro.cs` tarafından kullanılıyor → **Islemler sahibi (Q10-B).**
2. **denetim.sh `P-37c SAYFA_BASI_KAPSAM` muaf=3 (tavan 2):** `Ayarlar.razor`'a başka şeritte eklenen üçüncü `SAYFA_BASI_MUAFIYETI` işareti; dosya 25.09 01:12'de değişti. Sahibi: `Ayarlar.razor`'u açan şerit / orkestratör.
3. **`/cikti/*` BASİT modda K8=3.** "Cari ekstresi" başlığı ve açıklamasında "cari" geçiyor. Sayfa bu paketin dosya alanında değil; önceden var olan bir durum.
4. **Denetimde telefon maskesi:** telefon boşluklu girilmişse maske son 4 **karakteri** gösterir (`**********1 03`). Tam değer sızmıyor; yalnız görünüm meselesi.
5. **Mehmet onayı:** denetim izine maskeli eski/yeni yazılması (§3 madde 1).

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Önce bulguyu yeniden üret | ✓ | §1: kök neden `<ProAlan>`; QA DOM kanıtı ve kod birebir tutuyor |
| Modalda adres, telefon, vergi dairesi, VKN/TCKN | ✓ | Her modda 5 alan: `Q103_E01`, CDP tablosu 8/8 |
| PATCH gönderilmeyen alanı korur, null siler | ✓ | `Q103_H03`, `Alan_govdede_yoksa_korunur` ×4, canlı "yalnız adres" satırı |
| Denetim izi eski/yeni | ✓ (maskeli) | `Q103_H04`, SQL §7.3; sapma §3 |
| VKN/TCKN biçim doğrulaması zorunlu | ✓ | Core tek kural; sunucu, servis ve ekran aynı kaynaktan |
| Sağlama yalnız birincil kaynakla | ✓ (uygulanmadı) | §8: DOĞRULANAMADI |
| Telefon mevcut kalıpla | ✓ | `Iletisim.TelefonGecerli` değişmedi; canlıda `abc` → 400 |
| Hatalı biçimde alan altı Türkçe mesaj, istek gitmez | ✓ | `Q103_E03` ×4; canlı denetim sayacı 3 → 3 |
| Çıktı belgesinde yeni adres ve VKN | ✓ | `Q103_H05`; canlı CARI_EKSTRE; PNG'ler |
| Sınırlar: boş, 9/10/11/12 hane, harfli, boşluklu | ✓ | §7.1 tablosu, `Q103_S01`, `Q103_H01/H02` |
| Gerçek tarayıcı 1280 ve 375 · BASİT ve PRO · açık ve koyu | ✓ | §7.4: 8 kombinasyon ve belge; tüm sayaçlar 0 |
| Özellik silinmedi, DOM id ve rota değişmedi | ✓ | Yalnız sarmal kaldırıldı; id'ler aynı |
| Sabit renk ve satır içi style yok, app.css'e dokunulmadı | ✓ | denetim.sh sayaçları |
| Ana sözlük dosyasına (HUB) dokunulmadı | ✓ | Yeni `Sozlukler.IsletmeKimlik.cs` |
| Tek düzenleme kalıbı (Çekmece) | ✓ | ModalPencere 48, artış yok |
| Test kiracısı silindi, tenants sayısı başlangıçtaki değere döndü | ✓ | 12 → 12 |
| Paket yeni kırmızı üretmedi | ✓ | 12 kırmızının hepsi başka şeritte (§9) |

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)

```
 M dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs
?? dotnet/Kuyumcu.Core/Domain/VergiKimlik.cs
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.IsletmeKimlik.cs
?? dotnet/Kuyumcu.Web/Pages/Isletme.razor        (dosya depoda zaten izlenmiyor; bu pakette düzenlendi)
?? dotnet/Kuyumcu.Tests/Q103IsletmeKimlikTestleri.cs
?? dokumanlar/09-uygulama-kayitlari/olcumler/Q10-3/   (10 PNG ve ölçüm çıktısı)
?? dokumanlar/09-uygulama-kayitlari/PAKET-Q10-3-uygulama-raporu.md
```

## 12. Kurucuya / mali müşavire notlar

- **Mali müşavir:** VKN/TCKN kontrol hanesi algoritmasının birincil kaynağı (GİB ya da NVİ resmî teknik doküman) sağlanırsa sağlama eklenir. Şu an yalnız hane ve rakam denetleniyor (§8).
- **Mehmet (Güvenlik):** denetim izinde maskeli eski→yeni biçiminin onayı (§3 madde 1).
