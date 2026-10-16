# Paket notu — "Yıldız" teması: mevcut şemalara dokunmadan tema seçimine ekleme

**Tarih:** 8 Ekim 2026 · **Kaynak oturum:** tasarım taslağı (kod değişikliği yok, commit yok)
**Tuval (39 ekranın hepsi, koyu + 4 ekranın açık hali):** https://claude.ai/artifact/2eYLdsE7qXXZyGAaJZVGVo
Tuvalde "4. tur · Yıldız" başlıklı sıralar bu paketin kaynağıdır (y ≥ 4700). Üstteki 1–3. tur sıraları **reddedilmiş** denemelerdir; uygulanmaz.

**Durum uyarısı (Anayasa md. 1):** Tuval tarayıcıda ekran görüntüsüyle doğrulanmadı (tasarım oturumunda claude.ai girişi ve Chrome eklentisi bağlanamadı). Uygulama paketi başlamadan önce tuval **Play ile açılıp** bir kez gözle kontrol edilmeli; kontrast değerleri aşağıda formülle ölçüldü, görsel kontrol yerine geçmez.

---

## 1. Ne isteniyor

Kurucu, Mercedes'in "karanlık zeminde halkasız yıldız" reklamındaki etkiyi istiyor: **siyah zemin, çerçevesiz tek işaret, ince rakam, sessizlik.** Siyah hali onaylandı ("harika"). Açık hali de aynı dille istendi, çizildi.

Bu bir **yeni şema** olarak eklenecek: `data-sema="yildiz"`. Mevcut 8 şema (platin, zümrüt, grafit, bordo, okyanus, menekşe, zeytin, fildişi) ve mevcut aydınlık/karanlık mekanizması **değişmez**. Kullanıcı Ayarlar → Görünüm'den seçer; seçmeyen hiçbir şey fark etmez.

**Kurucu kararı (8 Ekim):** Yıldız **hem açık hem koyu** haliyle eklenecek; diğer 8 şema **olduğu gibi kalacak**. Aydınlık/karanlık düğmesi Yıldız'da da aynen çalışır: aydınlık → kırık beyaz `#F5F4F1`, karanlık/sistem karanlık → siyah `#000`. Tuvalde koyu hal 39 ekranda, açık hal 4 ekranda (Bugün, Tezgâh, Müşteriler, Tüm ekranlar) çizildi; kalan 35 ekranın açık hali yerleşimi değiştirmez, yalnız belirteçten renk alır.

## 2. Tasarım dili (uygulamada korunacak kurallar)

| Öğe | Yıldız kuralı |
|---|---|
| Zemin | Koyu: saf siyah `#000`. Açık: kırık beyaz `#F5F4F1`. Doku, degrade, cam yok. |
| Yüzey/kart | **Kart yok.** Ayrımı boşluk ve 1 px kıl çizgi yapar (`--h`). Kutu, gölge, dolgu yok. |
| İşaret | Elmas çizgisi, **halkasız ve kutusuz**, menünün solunda, yanında `MİHENK` yazısı; titanyum parıltılı 11 sn döngü. Ayrıntı ve **aynen uygulama talimatı: § 2a**. Mevcut 34 px mavi kare rozet bu şemada gizlenir. |
| Rakam | Baş roldür. Büyük sayı 200 ağırlık, sıkı harf aralığı; kuruş ve ₺ soluk ve yarı boy. |
| Yazı | Inter (değişken, 200–600). **Yeni yazı tipi dosyası gerekir** (OFL, latin + latin-ext, `wwwroot/font/`), CSP `font-src 'self'` korunur; Google Fonts bağlantısı YASAK (bkz. belirtecler.css D9). |
| Menü | Yan sütun **yok**. Üstte 72 px tek şerit: solda halkasız elmas işareti, yanında beş kelime (BUGÜN · TEZGÂH · MÜŞTERİLER · KASA · TÜM EKRANLAR, 14 px, harf aralıklı, gri; seçili olan beyaz + altında 1 px çizgi), sağda kur (HAS · 22 AYAR · saat) ve kullanıcı adı, altında kıl çizgi. 39 ekranın tamamı ve ekran arama "Tüm ekranlar" kare sayfasındadır. **Kurucu kararı: tasarımda ne varsa tema odur; yan menülü ara sürüm yok.** |
| Düğme | Ekranda **tek beyaz/siyah hap** birincil (`.b1`); ikinciller altı çizili yazı (`.b2`); satır eylemleri ince kenarlı küçük hap (`.b3`). Mavi/altın vurgu yok. |
| Renk | Yalnız durum için: iyi `#9AD3AE`/`#1C6A3E`, kötü `#F0A49C`/`#AE2A22`. Uyarı sarısı yok; uyarı metinle söylenir. |
| Etkileşim | Fare: kare 3 px kalkar, kenar platin griye döner, ikon hafif ışır; `prefers-reduced-motion` ile kapanır. |
| Kâğıt | Fiş/etiket/yazdırma **muaf** — mevcut kural (kâğıt daima beyaz) aynen. |

## 2a. Amblem — KURUCU KARARI (9 Ekim): tuvaldeki gibi, AYNEN ve EKSİKSİZ uygulanacak

Kaynak: tuvalde "Amblem" sırası → `Amblem_sol.dc.html` (koyu) ve `Amblem_sol_acik.dc.html` (açık). `Amblem_orta` yalnız karşılaştırmaydı, **uygulanmaz**.

**Yer:** sol üst, menü şeridinin başında. İşaret (40 px halkasız elmas çizgisi) + 14 px boşluk + `MİHENK` yazısı (13 px, 400, harf aralığı 4,4 px, büyük harf). İkisi tek `<a>` içinde, Bugün'e gider. Her ekranda sabit.

**Malzeme:** işaret ve yazı aynı SVG gradyanla boyanır (`url(#ti)`), tek parça gibi parlar.
- Koyu: gövde `#6A6A6A` → `#9A9A9A`, parıltı `#FFFFFF`; hale beyaz %16.
- Açık: gövde `#3A3A3A` → `#5E5E5E`, parıltı `#C9C6BE`; hale koyu `#141414` %10.

**Döngü, 11 sn, sonsuz (saniyeler değiştirilmez):**
1. 0–1,3 sn: işaret karanlıktan çıkar — opaklık 0→1, bulanıklık 3 px→0; arkadaki hale 0→1→0 (0–2,6 sn).
2. 0,7–2,4 sn: gradyan soldan sağa tek geçiş (SMIL `animateTransform`, `values="-1.2 0;-1.2 0;1.2 0;1.2 0" keyTimes="0;0.06;0.22;1" dur="11s"`).
3. 1,5–2,6 sn: `M İ H E N K` harf harf belirir; her harf ayrı `<tspan>`, 0,14 sn gecikmeyle (`.h1`…`.h5`).
4. ~6 sn sabit.
5. 9–10 sn: işaret ve yazı birlikte karanlığa döner (opaklık 0, bulanıklık 2 px); 1 sn boş; baştan.

**Kurallar:** yalnız amblem hareket eder, kabuğun gerisi kıpırdamaz. `prefers-reduced-motion: reduce` → bütün animasyonlar kapalı, işaret ve yazı tam opak sabit, hale gizli. Ek dosya, görsel ya da JavaScript yok: SVG + CSS keyframes; kabuk bileşenine (adım D) girer. Performans etkisi önemsiz (GPU üzerinde opaklık/transform; 11 sn'de bir).

**Kabul:** uygulamadaki amblem, tuvaldeki panoyla yan yana Play'de izlenir; yer, ölçü, renkler, sıra ve saniyeler bire bir aynı olmadan GEÇTİ sayılmaz ([[tasarim-birebir-tuval]]). Ekran görüntüsü: 0,5 sn (işaret yalnız), 2,0 sn (harfler geliyor), 6 sn (sabit), 9,5 sn (sönüyor); koyu ve açık.

## 3. Belirteçler (ölçüldü)

```css
:root[data-sema="yildiz"]            /* açık */  { --z:#F5F4F1; --m:#141414; --s:#66645F; --h:#DCDAD4; --p:#8A8780; --iyi:#1C6A3E; --kotu:#AE2A22 }
:root:not([data-tema="aydinlik"])[data-sema="yildiz"],
:root[data-tema="karanlik"][data-sema="yildiz"] /* koyu */ { --z:#000000; --m:#F3F2EF; --s:#9A9893; --h:#2A2A2A; --p:#C9C6BE; --iyi:#9AD3AE; --kotu:#F0A49C }
```
Mevcut semantik adlara eşleme: `--z→--yuzey-govde/--yuzey`, `--m→--metin`, `--s→--metin-soluk`, `--h→--ayrac`, `--p→--kenarlik`, birincil düğme `--vurgu = --m`, `--vurgu-metin = --z`.

Kontrast (WCAG formülü): koyu metin/zemin **18,4:1**, ikincil **7,6:1**; açık metin **15,9:1**, ikincil **5,6:1**; kenar `--p` ≥ 3:1 iki modda da. Durum renkleri ≥ 5,6:1. Tüm metin çiftleri `TasarimBelirtecTestleri.Kontrast_AA_saglanir`e eklenmeli.

**Okunurluk kararı:** Tuvaldeki 200 ağırlık rakam **sunum içindir**; uygulamada büyük sayı ≥ 300, gövde 400, 15 px altı 500. Parlak tezgâh ışığında ince yazı kaybolur.

## 4. İş bölümü ve büyüklük (ölçülmüş paket süresine dayanmaz, kaba tahmin)

| Adım | İçerik | Nereden gelir | Tahmin |
|---|---|---|---|
| A | Inter'i vendor'la (woff2 ×2 + OFL), `@font-face`, şema seçiliyken `--yazi-govde` Inter | belirtecler.css, font/ | küçük |
| B | `yildiz` şema bloğu (açık + koyu, 6 katman kuralına uygun), tema.js `SEMALAR` beyaz listesine ekleme, Ayarlar › Görünüm'de `.gorunum-sema-onizleme[data-sema="yildiz"]` | belirtecler.css, tema.js, Ayarlar.razor | 1 paket |
| C | Ortak bileşenlerin Yıldız varyantı: kart→çizgili bölüm, düğmeler (b1/b2/b3), sekme→altı çizili, çip→metin, liste satırı, boş durum, adım listesi, takvim | ortak.css / dugme.css / katlanir.css | 1–2 paket |
| D | **Kabuk:** `[data-sema="yildiz"]` iken KenarCubugu yan sütun yerine üst şeridi çizer (amblem § 2a + 5 kelime + kur + kullanıcı); "Tüm ekranlar" kare sayfası (39 kare, hover). Diğer 8 şemada kabuk **aynen kalır**. | KenarCubugu, tuval-kabuk.css, Ekranlar.razor | 1 paket — en çok iş |
| E | Ekran ekran ince ayar: Bugün (cümle + büyük rakam), Tezgâh (ayar rakamı başta, Fark bloğu), Müşteriler, Kasalar/Kur/Sayım gibi "özet" ekranları | ilgili .razor/.css | 1 paket |
| F | Kapı: kontrast testleri, 1440 + 375 ekran görüntüsü (gerçek tarayıcı kuralı), kâğıt muafiyeti testi, 8 eski şemada görsel regresyon | Aşkın | kapı |

Toplam kaba tahmin **4–5 paket**. %70'i A–C (belirteç + ortak bileşen), %30'u D–E.

**Risk 1 — kabuk:** Üst menü yalnız Yıldız'da; kabuk bileşeni iki yerleşimi de taşır. Ara sürüm (yan menülü Yıldız) **yapılmaz**, kurucu kararı. Diğer 8 şemada tek piksel değişmediği ekran görüntüsü karşılaştırmasıyla kanıtlanır.
**Risk 2 — telefon:** Üst menü 375 px'te sığmaz; hamburger'a katlanır. Tuvalde telefon hali çizilmedi.
**Risk 3 — kalabalık ekranlar:** Pro modda 8 sekmeli Satış, kolon dolu tablolar "sessizlik" dilini zorlar. Yıldız **Basit modda** tam etki verir; Pro modda yalnız renk/yazı uygulanır.
**Risk 4 — ince yazı:** § 3'teki ağırlık kararı uygulanmazsa okunurluk kapısından geçmez.

## 5. Tuvaldeki ekranlar (39)

Satır başına bir bölüm, tuvalde `y = 8300`'den itibaren:
Günlük iş 5 · Müşteri ve stok 6 · Para ve banka 10 · Belge ve uyum 4 · Raporlar 6 · Yönetim 8.
Panel→`Bugun4`, Satış→`Tezgah4`, Müşteriler→`Musteri4` (y=5000 sırasında); diğer 36 ekran `Y_*` dosyaları. Açık hal 4 ekran için (`*4a`), gerisi belirteçten türer.

Beş kalıp kullanıldı; uygulamada da beş ortak bileşene karşılık gelir:
**liste** (başlık + arama + tek düğme + sekme + satırlar), **özet** (büyük rakam + cümle + sağda liste), **form** (etiket/değer çizgili alanlar + "ekle" + tek düğme + sağda not), **boş durum** (tek cümle + tek düğme), **adımlar** (Kurulum), **takvim** (Vade).

İçerik ve alanlar mevcut ekranlardan alındı; **hiçbir alan silinmedi**, isteğe bağlılar "+ ekle" arkasına katlandı (sadeleştirme kuralı). Veri TUNALILAR demo dükkânı.

## 6. Kapsam dışı

- Mevcut 8 şemanın hiçbir değeri değişmez; `--yuzey*`, `--vurgu*` eşlemeleri yalnız `[data-sema="yildiz"]` altında.
- Kâğıt (`.belge*`, `.urun-etiket*`, `@media print`) dokunulmaz.
- 3. tur "Mihenk Taşı" (niyet çubuğu, sesli komut, terazi) **bu paketin parçası değil**; ayrı ürün kararı.
- Veri göçü yok; şema kaydı mevcut `kiraci_ayarlari`/kullanıcı tercihi yoluyla.

## 7. Bitti sayılma ölçütü

0. **Amblem § 2a aynen ve eksiksiz** (yer, yazı, malzeme, 11 sn döngü, açık/koyu, hareket azaltma); tuvalle yan yana izlenip ekran görüntüleriyle kanıtlanmış.

1. Ayarlar → Görünüm'de "Yıldız" seçilince 39 ekran koyu ve açıkta şemayı alır; başka hiçbir şema piksel değiştirmez (8 şema × 2 mod ekran görüntüsü karşılaştırması).
2. Kontrast testleri yeşil; Yıldız çiftleri listeye eklenmiş.
3. 1440 ve 375'te Bugün, Tezgâh, Müşteriler, Tüm ekranlar ekran görüntüsü alınmış (gerçek tarayıcı kuralı).
4. Fiş ve etiket çıktısı beyaz kâğıt, siyah mürekkep.
5. İnternet kesikken Inter yüklenir (vendor dosyadan).
