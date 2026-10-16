# Vitrin Panosu — Ekip Oyu ve Mühürlenen Kararlar

**Tarih:** 2026-09-11 · **Mühürleyen:** Tamer Bey · **Durum:** karar alındı, **KOD YAZILMADI**
**Girdi:** `projeler/arastirmalar/KUYUMCU-YAZILIM-FIRSAT-2026-09-07.md` · `FAZ0-DUZELTME-RAPORU-2026-09-10.md` §7–§11
**Kurucu isteği:** dükkân TV'sinde canlı fiyat; kuyumcunun kalem seçip **kendi eklemelerini/çıkarmalarını** ayarladığı yönetim ekranı + TV'ye yansıyan pano ekranı. Kararlar 3 ajanın oyuyla alındı.

---

## 0. Oylar

| Rol | Oy | Bağlayıcı koşulu |
|---|---|---|
| **Bertan** (Baş Mimar) | KOŞULLU EVET | Marj sunucuda · ham kotasyon vitrine çıkmaz · bayat fiyat ekrandan kalkar · `urunler.parite_kodu` ve işlem fiyatı Faz A'da hiç dokunulmaz |
| **Eray** (Satış) | KOŞULLU EVET | Marj alanı olmadan yayın yok · pano **ücretsiz kanca** · Harem bu fazda alınmaz · ilk 10 pilot bulunmadan genel pazara duyurulmaz |
| **Mehmet** (Güvenlik, veto) | KOŞULLU EVET **+ yayına VETO** | 10 zorunlu güvenlik maddesi ve 6 kabul kriteri karşılanmadan halka açık yayın yok · Faz 0 vetosu (global kiracı filtresi) hâlâ açık |

**Mühür:** Faz A geliştirilebilir. **Yayın ayrı bir kapıdır** ve iki veto birden karşılanmadan açılmaz.

---

## 1. Kurucu'nun üç sorusu — kararlar

### S1. Pano kaç kalem gösterecek?
**Karar: varsayılan 7 kalem açık, tavan 16 satır.** Kuyumcu hiç ayar yapmadan işe yarar bir pano görür.

> ⚠️ **Bu madde 2026-09-11'de piyasa ölçümüyle güncellendi.** İlk oyda "8 açık / ~12 tavan" yazıyordu ve dayanağı yoktu. Ölçüm ve gerekçe: **§7**.

| Sıra | Kalem | Birim |
|---|---|---|
| 1 | Gram Altın (24 ayar) | gram |
| 2 | Çeyrek (Yeni) | adet |
| 3 | Yarım (Yeni) | adet |
| 4 | Tam / Teklik (Yeni) | adet |
| 5 | Ata Lira | adet |
| 6 | 22 Ayar Bilezik | gram |
| 7 | Gümüş (gram) | gram |
| — | USD / EUR | adet | *(varsayılan **kapalı**, tek tıkla açılır)*

Bu 7'li küme piyasada "dükkânda gösterilen temel ürün seti" diye adlandırılan kümenin birebir kendisidir (§7, bulgu #3) — bizim tahminimiz değildir.

Faz B'de tıkla-aç: eski ziynet takımı, Reşat, Hamit, Beşli, 14/18 ayar, Hurda, ONS.
**Ölçüm notu:** fiziksel LED panolar 6 satırda duruyor (donanım kısıtı), kuyumcuların kendi listeleri 12–16 kaleme çıkıyor (§7). Tavan 16, ama 12 üstünde ekranda okunabilirlik uyarısı çıkar — **o eşik ölçülmedi, tahmindir**, pilotta metreyle doğrulanacak.
**Kaldırılan:** ONS Faz A listesinde yok (USD cinsinden, TL marjı/yuvarlaması anlamsız).

### S2. Marj lira mı yüzde mi?
**Karar: kalem başına sabit TL fark. Yüzde Faz B.**
**Kanıt:** rakip KuyumcuPano kendi ürün sayfasında *"Milyem ve TL bazında özelleştirilebilir kâr"* diyor — yüzde değil. Sektör mantığı da bunu destekliyor: kuyumcunun kâr hedefi işçilik ve dükkân masrafına göre **TL cinsinden sabittir**; yüzde olsa altın yükseldikçe kâr kendiliğinden büyürdü, kuyumcunun istediği bu değil.

Ek kararlar:
- **Alış ve satış için ayrı marj.** Alış marjı **negatif olabilir** (kuyumcu kotasyonun altına iner).
- **Sarrafiye/gram ayrımı korunur:** adet kotasyonlu kaleme adet başına TL, gram kotasyonlu kaleme gram başına TL. Faz 0 §10'da kanıtlanan birim hatası tekrarlanmaz.
- **Yuvarlama:** adım kullanıcı seçimli (0/1/5/10/50/100), **varsayılan 10 TL**. Yön: **satışta yukarı, alışta aşağı** (kuyumcu kendi zararına yuvarlamaz).
- **Dürüst not:** yuvarlama alışkanlığının sektör standardı olduğu **doğrulanmadı**; bu yüzden sabitlenmedi, ayar bırakıldı ve en düşük adım varsayılan yapıldı.

### S3. TV'ye ne bağlanacak?
**Karar: birincil fansız mini PC + Chromium kiosk. Yedek: mevcut akıllı TV tarayıcısı (yalnız demo).**

| Seçenek | Karar | Gerekçe |
|---|---|---|
| Fansız mini PC + Chromium kiosk | **BİRİNCİL** | Tarayıcı sürümü bizde, elektrik kesintisinden sonra kendi gelir, uzaktan destek verilebilir, TV yalnız HDMI ekran olur — kuyumcunun elindeki TV ne olursa olsun çalışır |
| Akıllı TV tarayıcısı | **YEDEK / demo** | Bedava geliyor (bkz. S3-teknik), ama tüketici TV'de **açılışta otomatik URL yükleme yok** — her kapanışta elle açmak gerekir. Kalıcı kurulum için uygun değil |
| Android TV kutusu | HAYIR | Standart dışı firmware, güncellenmeyen WebView, destek yükü mini PC'den yüksek |
| Raspberry Pi | HAYIR (Faz A) | Plug-and-play değil, her kurulum bizim elimizden çıkmak zorunda. Filo 20 dükkânı geçerse yeniden bakılır |
| Ticari tabela paneli (Samsung SSSP / LG Signage URL Launcher) | Faz B | Teknik olarak en temiz ama panel maliyeti kuyumcuyu ürkütür |

**Kurulum tasarımı:** mini PC **ön-yapılandırılmış** teslim edilir (kiosk servisi + cihaz anahtarı + watchdog kurulu). Kuyumcunun işi iki adım: HDMI'ı tak, elektriğe ver.
**Dürüst not:** Türkiye donanım fiyatları **doğrulanmadı**, Mustafa'nın yerel teklif almasi gerekir.

### S3-teknik. Pano ekranı hangi teknolojiyle yazılacak?
**Karar: Blazor WebAssembly'nin DIŞINDA, sunucudan servis edilen tek dosya sade HTML + saf JS (~15 KB, ES5). Veri SSE (`EventSource`), fallback olarak 10 sn'lik XHR yoklaması. SignalR JS istemcisi kullanılmayacak.**

Bertan'ın **bu repoda ölçtüğü** sayılar bu kararı zorunlu kıldı:

| Ölçüm | Değer |
|---|---|
| Blazor WASM ilk yükleme seti | **9,26 MB ham · 2,79 MB brotli** (61 dosya) |
| Sunucu brotli/gzip ile WASM servis ediyor mu | **HAYIR** (`Program.cs`'de `UseResponseCompression` yok; nginx `gzip_types`'da `application/wasm` yok) |
| `wasm-tools` workload | **kurulu değil** (publish "without optimizations" uyarısı veriyor) |

Dış kanıt: Samsung Tizen 2018 ve öncesi ile LG webOS 4.x, Chromium M56/M53 taşıdığı için **WebAssembly'yi hiç çalıştırmıyor**; motor sürümü firmware ile yükselmiyor. Blazor'un raporlanan 170–260 MB çalışma belleği, 1 GB sınıfı TV yongasında çökme riski. 7/24 açık kalan SPA'larda bellek sızıntısı yaygın.
**Sonuç:** panonun ihtiyacı 12 satır rakam, bir saat ve bir perde. Sade HTML bunu 2016 model bir TV'de bile yapar.
**Dürüst not:** hiçbir ölçüm **gerçek TV üzerinde** yapılmadı; karar üretici belgelerine ve ölçülen boyuta dayanıyor. `EventSource`un eski TV motorlarında çalıştığı doğrulanmadı, bu yüzden XHR fallback **seçenek değil zorunluluk.**

---

## 2. Çapraz kararlar

1. **Marj ve yuvarlama hesabı SUNUCUDA.** Panoda marj hesabı yapan kod yazılmayacak. Gerekçe: marj ticari sırdır ve pano HTML'i herkese açıktır; ayrıca aynı kural Faz B'de ürün fiyatına da açılacak, iki ayrı uygulama (C# ve JS) iki farklı kuruş üretir.
2. **Bayat fiyat ekrandan KALKAR.** Eski fiyat soluklaştırılarak bırakılmaz. Perde iner: *"Fiyatlar geçici olarak gösterilemiyor — saat 14:32'den beri güncellenemiyor."* İki taraflı ölçüm: sunucu kaynaktan son **başarılı çekim** 60 sn'yi geçtiyse `bayat` bayrağı basar, pano son paketten 20 sn geçtiyse kendini karartır.
3. **Ankara'nın Socket.IO canlı kanalı Faz A'da kullanılmayacak.** 10 sn vitrin için zaten fazla hızlı; üç kaynaktan yalnız birinde var, mimari asimetri yaratır; .NET için resmî Socket.IO istemcisi yok. Faz B'de "canlı borsa modu" olarak eklenebilir, `IKurAdaptoru` sözleşmesi değişmez.
4. **Pano kalemi stok kartı DEĞİLDİR.** Vitrinde "Yeni Çeyrek" yazar ama stokta 0 olabilir; 200 stok kartı varken panoda 12 satır olur. `urunler.parite_kodu` Faz A'da hiç kullanılmaz, Faz 0 §11 mührü korunur.
5. **Uykudaki parite altyapısı burada uyanır.** 22 kanonik parite, alış/satış ayrımı ve fiyat birimi (ADET/GRAM/HAS_GRAM) hiç değişmeden panonun temeli olur. Uyanan yalnız **okuma** tarafı; deftere dokunan hiçbir şey açılmaz. Kaynak Panel'den değiştirilince pano kendiliğinden yeni kaynağa geçer, **kanonik kodlar sayesinde yapılandırma bozulmaz** — bu yatırımın karşılığını ilk kez ödediği yer.
6. **İlk açılış boş.** Pano uydurma fiyatla açılmaz. "Önerilen seti yükle" denirse 8 kalem **marj 0** ile gelir ve yönetim ekranı açık uyarı basar: *"Marj girilmedi — bu fiyat kaynağın toptan kotasyonudur, sizin satış fiyatınız değil."*
7. **Fiyatlama: pano ücretsiz kanca.** Ayrı ücretli modül değil, ERP'nin içine kilitli özellik de değil. Küçük marka ibaresiyle bedava verilir; ERP'ye geçişte ibare kalkar ve pano paketin standart parçası olur. Rakiplerin hiçbiri fiyat yayınlamıyor ("arayın" diyorlar); bizim "ücretsiz, 5 dakikada kurulur" cümlesi giriş bariyerini sıfırlar.
8. **Satış cümlesi:** *"Rakipler size bir ekran satar; biz o ekranı ücretsiz veririz, çünkü asıl işimiz dükkânınızın has-cari defterini ve 2026 mevzuat yükünü tek yerden çözmek."*
9. **Harem bu fazda alınmaz.** Ankara'lı kuyumcuya Ankara Altın Borsası, İstanbul'a Altınkaynak (serbest piyasa) yeterli. Harem'in ücretsiz kademesi ayda 30 istek, 10 sn'lik akışa yapısal olarak uymuyor; doğrudan ücretli sözleşme gerekir. **Dürüst not:** Harem eksikliğinin müşteri kaybı oranı **ölçülmedi**; pilotta bir kuyumcu bu sebeple vazgeçerse rakamla Kurucu'ya çıkılır.
10. **İlk pilotlar:** Ankara Ulus/Samanpazarı çevresi (kaynağı zaten elimizde) ve İstanbul Kapalıçarşı perakende vitrin sahipleri. Toptancı geçişi sonra.

---

## 3. Mehmet'in zorunlu güvenlik gereksinimleri

1. Ayrı `PanoController`, **yalnız GET**, `[AllowAnonymous]` yalnız bu controller'da.
2. Cihaz anahtarı: **256 bit rastgele**, veritabanında **yalnız SHA-256 hash** (emsal: refresh token, `Auth/TokenServisi.cs`). Düz metin bir kez gösterilir, tekrar gösterilmez.
3. **Tenant anahtardan çözülür, istekten ASLA.** Faz 0'daki K1 dersi tersine uygulanır.
4. Anahtar **fragment'te** taşınır (`/pano#k=…`) — sunucu erişim logu, `Referer` ve proxy logları anahtarı görmez. `Referrer-Policy: no-referrer`.
5. Kiracı başına **birden çok cihaz anahtarı** (bir TV sızınca yalnız o iptal edilir), tek tıkla iptal, son kullanım takibi.
6. **Pano ucu marjı, ham kotasyonu, kiracı kimliğini, cari/kasa/stok/personel verisini ASLA döndürmez.** Anahtarı ele geçiren yalnız o dükkânın vitrinde zaten halka açık olan fiyatlarını görür; rakip marjı öğrenemez.
7. **Oran sınırı ZORUNLU** — kod tabanında şu an `AddRateLimiter` hiç yok (ölçüldü). Anahtar başına + IP başına limit.
8. Pano ucu **dış kaynağa asla doğrudan gitmez**, yalnız veritabanındaki son snapshot'ı okur. Kısa TTL önbellek zorunlu.
9. Ekranda **zorunlu:** bayat veri uyarısı, son güncelleme saati, **kaynak atfı** ("Kaynak: Altınkaynak").
10. Anahtar oluşturma/iptal **denetim kaydı**; loglara anahtar düz metni asla yazılmaz.
11. **Lisans:** kaynağın verisini kamuya açık vitrinde yeniden yayınlamak, iç kullanımdan farklı bir haktır. Yazılı izin (ya da kullanım şartlarının bunu yasaklamadığının belgelenmesi) **alınmadan gerçek müşteriye kurulmaz.** Para/sözleşme → **Kurucu'nun alanı.**

**Kabul kriteri (bunlar yeşil olmadan yayın yok):** başka kiracının anahtarıyla veri gelmiyor · iptal edilen anahtar 401/404 · yanıt şemasında marj/maliyet/tenant kimliği yok (regresyon testi) · oran sınırı 429 veriyor ve dış kaynağa ek istek gitmiyor · bayat eşiği aşılınca fiyat alanı null dönüyor ve ekran kararıyor · marj TL/yüzde, negatif alış marjı, yuvarlama 0/1/5/10/50/100 × yukarı/aşağı/yakın hesapları doğru.

---

## 4. Faz A yapılacaklar (sıralı, henüz başlanmadı)

1. Migration: `pano_ayarlari`, `pano_kalemleri`, `pano_cihazlar`.
2. `KurKaynakDefteri`ne **kaynak başına son başarılı çekim zaman damgası** — bayat tespitinin ön şartı, şu an yok (ölçüldü). **Tuzak:** ölçüt *çekim* zamanı olmalı, *yazım* zamanı değil; `KaydetVeyaMevcut` fiyat değişmediyse satır yazmadığı için sakin piyasada pano kendini boşuna karartır.
3. `PanoFiyatServisi` — saf fonksiyon: parite + kalem ayarı + marj + yuvarlama → pano satırı. I/O yok, tamamen testlenebilir.
4. Yönetim uçları (JWT'li; yazma SAHIP+MUDUR), cihaz uçları (anahtar üret/listele/iptal).
5. `PanoTokenFiltresi` + oran sınırı + `no-store`.
6. Pano veri uçları: tek çekim + SSE akışı (5 sn ping, `bayat` bayrağı).
7. `/pano` sayfası — tek dosya HTML+CSS+JS, ES5, `EventSource` + XHR fallback, bayat perdesi, günlük 04:00 yeniden yükleme, TV mesafesi için büyük tipografi. `MapFallbackToFile` **öncesine** eşlenmeli.
8. `Pages/Pano.razor` yönetim ekranı — kalem listesi (mevcut `suruklenebilir.js` ile sürükle-sırala), marj/yuvarlama alanları, **sunucudan çekilen canlı önizleme**, cihaz listesi + QR.
9. `UseResponseCompression` (br + gzip) + nginx `gzip_types`'a `application/wasm` — ölçülmüş mevcut eksiklik, pano da Blazor da kazanır.
10. Testler (Aşkın'ın fail-closed kapısı, §3 kabul kriterleri).
11. Kurulum runbook'u (Batuhan): mini PC kiosk servisi, watchdog, otomatik açılış.

**Faz B:** 6 haneli cihaz eşleştirme kodu · dönen sayfalar, logo, reklam · ONS ve döviz satırları · Ankara canlı borsa modu · Samsung/LG tabela paketi · pano uptime bildirimi · çoklu şube · müşteri telefonuna QR fiyat listesi · **ürün fiyatına marjın geri açılması** (Faz 0 §11'in üç koşulu — Faz A'nın marj tablosu bunun ilk şıkkını zaten inşa ediyor).

---

## 5. Kurucu'ya kalanlar

1. **Kaynak kullanım izni** (Altınkaynak, Ankara Altın Borsası) — pano bunu artık zorunlu kılıyor. Kod yazılabilir, **satılamaz.**
2. **Harem/Hasfiyat ücretli anahtar kararı** — şimdilik gerek yok, pilot verisi beklenecek.
3. **Donanım tedariki ve fiyatı** — fansız mini PC, Türkiye fiyatı doğrulanmadı.
4. **Pilot müşteri erişimi** — Ankara ve Kapalıçarşı.

## 6. Açık kalan iki eski hata (pano bunları kapatmıyor)
Faz 0 §7'de raporlanan **boş satış grafikleri** ve **yanıltıcı "Net Kasa" kartı** hâlâ düzeltilmedi. Panel'e yeni bir kart eklenecekse aynı ekranda duran yanlış kartın aynı turda düzeltilmesi gerekir.

---

## 7. S1 DÜZELTMESİ — piyasa ölçümü (2026-09-11, Kurucu talebi)

Kurucu "ezberden konuşma, piyasadan araştır" dedi. §1/S1'deki *"8 varsayılan, ~12 tavan; sektöre özel doğrulanmadı"* notu artık **ölçülmüş veriyle** değiştirildi. Sayılar aşağıda kaynağıyla birlikte.

### Bulgular

| # | Kaynak tipi | Ne ölçüldü | Kalem sayısı |
|---|---|---|---|
| 1 | Fiziksel LED pano — Dolunay Led 130×110 | teknik özellik: **"6 Satır, 2 Sütun"**, hücre 5 haneli | **6** |
| 2 | Fiziksel LED pano — LedTabelaKart 80×80 / 65×70 | ürün metni: **"6 Satırdan Oluşan Panomuz"** | **6** |
| 3 | KuyumAltın — "dükkânda gösterilen temel ürün seti" | gram, çeyrek, yarım, tam, cumhuriyet, 22 ayar bilezik, gram gümüş | **7** |
| 4 | Uygun Kuyumculuk — gerçek bir kuyumcunun müşteriye açık listesi | çeyrek…24 ayar gram, ufaklıklar dâhil | **12–13** |
| 5 | Kuyumcular Odası "Kuyumcu Ekranı" listesi | 13 altın kalemi + kilo/USD, kilo/EUR, ONS $ | **16** |
| 6 | Harem Altın (toptancı kotasyonu, vitrin değil) | platin/paladyum/oran dâhil | **28** |

Rakip TV yazılımları (KuyumcuPano, SarrafPano, KuyumcuEkran, KuyumcuPanel) **kalem sayısı yayınlamıyor**; hepsi "göstermek istediğiniz ürünleri seçin" diyor. KuyumcuEkran HTTP 403 döndü, içerik alınamadı. Yani §1'deki "rakip sayı vermiyor" tespiti doğrulandı — ama artık tek dayanağımız o değil.

### Okuma

İki farklı üretici bağımsız olarak **6 satır**a yakınsamış. Bu rastgele değil: mesafeden okunabilirlik rakam yüksekliğini, rakam yüksekliği de satır sayısını sınırlıyor. Kuyumcunun **bugün duvarında asılı olan** sayı 6'dır.

Buna karşılık mecra TV olunca tavan yükseliyor: gerçek bir kuyumcunun kendi listesi 12–13, oda listesi 16. Yani 6 bir *donanım kısıtı*, kuyumcunun *isteği* değil.

### Yeni karar (S1 yerine geçer)

- **Varsayılan: 7 kalem açık** — 8 değil. Sebep: bulgu #3, piyasada "dükkân seti" diye adlandırılan kümenin birebir kendisi. Gram altın, çeyrek, yarım, tam, Ata/Cumhuriyet, 22 ayar bilezik, gram gümüş.
- **USD/EUR varsayılan KAPALI**, tek tıkla açılır. §1'de 8. satır olarak açıktı; bulgu #3'te dükkân setinde yok, #5'te ayrı blokta. Döviz isteyen açar.
- **Tavan: 16 satır** — ~12 değil. Sebep: bulgu #5, kuyumcuların resmî ekranında bu kadarı zaten var.
- **12 satırın üstünde ekranda uyarı:** "13+ satırda yazı küçülür, 4 m mesafeden okunmayabilir." Bu eşik **ölçülmedi**, LED sektörünün 6 satırda durmasından çıkarılan tahmindir — pilotta gerçek TV'de metreyle doğrulanacak. (Madde 1: tahmin olduğu burada yazılıdır.)
- ONS kararı **değişmedi**: Faz A'da yok.

### Kaynaklar
- https://www.dolunayled.com/urun/kuyumcu-fiyat-panosu-130cm-x-110cm
- https://www.ledtabelakart.com/?pnum=14
- https://kuyumaltin.com/
- https://www.uygunkuyumculuk.com/altin-fiyatlari
- https://canlialtinfiyatlari.com/kuyumcular-odasi-altin-fiyatlari.html
- https://www.haremaltin.com/
- https://kuyumcupano.com/kuyumcu-fiyat-ekrani · https://sarrafpano.com/ · https://kuyumcupanel.com/ (sayı yayınlamıyor)
