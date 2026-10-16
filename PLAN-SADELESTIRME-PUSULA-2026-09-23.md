# SADELEŞTİRME PLANI — "Pusula kadar sade, bizden fazla" (23 Eylül 2026)

> **Kime:** kendime (sonraki oturumlar) ve ekibe. **Yazan:** Tamer Bey koordinasyonu, Ertuğ (03 Frontend) + Bertan (02 Mimari) merceğiyle.
> **Neden:** Kurucu ürünü müşteriye gösterdi. Müşterinin cümlesi: *"çok karışık abi — menüler karışık, ekranlar karışık, renkleri iyi değil; Pusula çok sade ve güzel."*
> **Kurucu talimatı (23 Eylül):** *"Mevcut özellikleri bozmadan daha sade nasıl yaparız; Pusula gibi albenisi olan görsel ve sadeliğe sahip ama daha iyi özelliklere sahip bir proje; mevcut projeyi emin adımlarla çevireceğiz; planı kendin için yaz."*
> **Durum:** PLAN — kod yazılmadı, mevcut dosya değiştirilmedi, commit yok. Uygulama, § 6'daki ekip oyu (Tamer Bey mührü) ve kurucunun § 8'deki görsel tercihleriyle başlar.

---

## 0. Bu belge nasıl kullanılır (devralma algoritması)

1. Önce `CLAUDE.md` → `SONRAKI-OTURUM-PROMPT.md` → `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` (kod hattının tek kaynağı). Bu plan o hattı **iptal etmez**, ona "S dalgası" olarak eklenir (§ 6.0).
2. § 9'daki ölçüm defteri boşsa **S-0'dasın**: önce ölç, sonra yaz. Doluysa son dolu dalganın kapısına bak, sıradaki dalgayı aç.
3. Her dalga aynı ritüelle biter (§ 6.9): uygulayıcı → hasım doğrulayıcı → `kapi.sh` → yayın 5199 → `qa-turu.js` → tasarım denetçisi → kurul puanı. Ritüelden geçmeyen dalga "bitti" sayılmaz (Anayasa md. 1).
4. Bu plan **özellik silmez**. Bir şey "kaldırıldı" görünüyorsa katlanmıştır ya da profesyonel moda taşınmıştır; § 7.1'deki "özellik kaybı = 0" sayacı bunu ölçer.

---

## 1. Müşterinin dört cümlesi → ölçülmüş teşhis

Önceki oturumların ölçümleri (21–22 Eylül) müşterinin her cümlesine bir sayı veriyor. Hiçbiri tahmin değil; kaynağı yanında.

| Müşteri ne dedi | Ölçülmüş gerçek | Kaynak |
|---|---|---|
| **"Menüler karışık"** | 6 grup, **36–40 bağlantı hep açık duruyor** (gruplar katlanmıyor). Pusula'da 7 dal kapalı başlar, ekranda 7 satır görünür. Basit modda 8 rota var ama varsayılan mod PROFESYONEL; müşteri 36 bağlantıyı gördü. | `PUSULA-KIYAS-RAPORU-2026-09-21.md` § 3, `Layout/MenuTanimi.cs`, `Servisler/BasitMenu.cs` |
| **"Ekranlar karışık"** | POS'ta **8 sekme + form yığını**, aynı formda 4 düğme yüksekliği; Cariler'de satırda 7–8 düğme, satır yüksekliği 329 px, ekranda 2 satır; 22 ekranda birden fazla birincil düğme. `Islemler.razor` **2.576 satır**. Sipariş→irsaliye→fatura zinciri yok ama tezgâhtaki "verdim/aldım" fişi de yok: kullanıcı kafasındaki fişi bizim forma **çeviriyor**. | `FRONTEND-KIMLIK-VE-MENU-2026-09-21.md` § 3.6, QA turu 7 satır 4, Pusula raporu § 4 |
| **"Renkleri iyi değil"** | Palet WCAG AA'yı geçiyor (27 çift ölçüldü) — sorun renk değeri değil **disiplin**: 31 farklı ham dolgu, 27 farklı yazı boyutu, 3 düğme sistemi, koyu menü + sarı düğme + gri sistem dropdown'u yan yana. Dropdown/girdi göçü (P-36) 21 Eylül'de büyük ölçüde bitti; görsel tek tipleştirme (genişlik/yarıçap/punto/düğme sırası) **yapılmadı**. | Kimlik belgesi § 3.2–3.5, `PAKET-P-36c-uygulama-raporu.md` § 9 |
| **"Pusula sade ve güzel"** | Pusula'nın sattığı şey **çevirisizlik**: satış ekranında 3 alan + 13 büyük düğme + tek grid + "fark 0,00 → Kaydet". Menüyü küçültmüyor, **baypas ediyor**. Eğitim: menü 1:52, satış 3:54, toplam ~45 dk. | Pusula raporu § 1, § 4, § 5 |

**Tek cümlelik teşhis:** Özelliklerimiz fazla değil, **yüzümüz fazla**. Her özellik ilk bakışta görünüyor; Pusula ise günlük işi tek ekrana, geri kalanı kapalı dallara koyuyor.

---

## 2. Pusula'nın sadelik formülü — ve bizde durumu

| # | Pusula ilkesi | Bizde | Ne yapılacak |
|---|---|---|---|
| F1 | **Menü kapalı dallarla başlar**, 7 satır görünür, üstte arama | Gruplar var, katlanmıyor; arama var (P-37) | Katlanır gruplar (S-1) |
| F2 | **Günlük iş tek ekranda**: 3 alan + büyük düğmeler + tek grid + fark 0 | 8 sekme + ayrı ödeme/kasa/vade blokları; müşteri zorunlu; karma ödemede altın bacağı yok | Tezgâh fişi (S-2) |
| F3 | **"Peşin müşteri" ile açılır**, cari seçmek istisna | Cari zorunlu; nakit satış 5–7 tık | Sistem carisi + ön seçim (S-2, karar D2) |
| F4 | **Müşterinin altında bakiye rozeti** (HAS) | Veri ekranda yüklü, rozet yok | Rozet (S-2) |
| F5 | **Kaydet → yazdır**, tek hamle | Belge motoru var (`/cikti`), POS'tan bağlantı yok | Bağlantı (S-2) |
| F6 | **Öğrenme dakikayla ölçülür** (1:52 / 3:54 / 45 dk) | Ölçülmedi | Ölçüm defteri (S-0), hedefler (S-5) |
| F7 | Az renk: beyaz zemin, tek vurgu, gri gövde | Palet iyi, kullanım dağınık | Görsel dil (S-1) |

**Pusula ezberi (23 Eylül, kurucu talimatı "gerekirse ezberle"):** ayrıntılı referans kartı `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md` — iki Pusula (klasik karo-ızgaralı masaüstü ↔ mor menülü PusulaX), Ana Ekran'ın 8 kutucuğu, satış fişinin 9 kolonu + 13 düğmesi + "ödeme bacağı da satırdır" kuralı, üretim ekranı, mobil uygulamanın "tek iş, büyük sayı" dili, 10:51'lik PusulaX demosunun akışı, klasik satış videosunun anlatım senaryosu, alınacak 9 / alınmayacak 6 madde. **Müşteriye sorulacak ilk soru:** "gördüğün Pusula karo karo büyük düğmeli olan mı, mor menülü olan mı?"

Pusula'da olmayıp bizde olan ve **korunacak** şeyler (silinmez, yalnız katlanır): emanet, MASAK/tevsik/KMTS kapıları, gider pusulası, storno motoru, denetim izi, çok kiracılı web + MFA, kur beslemesi, pırlanta 4C, sipariş/tamir, çek/senet, banka ekstresi, vezne, fiyat listeleri, sayım oturumu, basit/profesyonel mod. Bunlar müşterinin "fazlası" değil, satış argümanımız; ama **ilk bakışta görünmeyecek**.

---

## 3. Bağlayıcı sınırlar (planın hiçbir dalgası bunları aşmaz)

1. **Özellik silinmez.** Her ekran, düğme, API ucu ya görünür, ya katlanmış (`ProAlan` / "⋯" menüsü / katlanır grup), ya profesyonel modda. § 7.1 sayacı.
2. **Kapsam dondurma (17 Eylül) yürürlükte.** Bu plan yeni modül açmaz. Pusula raporu § 10'daki 15 madde "kablo işi"dir; mühürlü kuyrukta olmayan üçü (Peşin Müşteri carisi, para üstü, Açık Pozisyon kutucuğu) **oya sunulur** (§ 6.0 D2, D7), oy çıkmazsa yapılmaz.
3. **Commit yok.** Çalışma ağacında kalınır. `git checkout/stash/reset` yasak (hook).
4. **Anayasa md. 1.** "Bitti" = `dotnet build` 0/0 + `dotnet/denetim.sh` TEMİZ + tam süit 0 başarısız + canlıda gerçek veriyle doğrulama + mutasyon kanıtı + test kiracısı silindi.
5. **Tek POST yolu** (Bertan KOŞUL 4): POS gövde kurulumu ve kapı çağrıları yalnız `Pages/Islemler.razor` `Kaydet()` içinde kalır. Yeni fiş bileşenleri **POST etmez**, `ApiIstemci` enjekte etmez.
6. **Yetki ≠ mod** (M-UX3-06, Mehmet K8): menü daraltması yetkiyi gevşetmez; basit mod hiçbir yasal kapıyı (tevsik, MASAK, taş kökeni) atlamaz; eşik sayıları istemciye gömülmez.
7. **DOM id sözleşmesi:** mevcut hiçbir `id` değişmez, yalnız eklenir; dalga öncesi/sonrası id envanteri diff'i boş. bUnit id'leri (`#islem-cari`, `#islem-kasa`, `#islem-bacak-ekle` …) korunur.
8. **Saf CSS, vendored dosya olabilir; CDN/npm/inline script YOK**; `index.html` script sayısı artmaz; CSP'ye istisna eklenmez. Bileşen kiti alınmaz (MudBlazor/Radzen CSP'de kanıtlı uyumsuz).
9. **Yaz-seri.** Aynı dosyaya iki dalga yazmaz. Kod hattında **dalga 7** (P-27…P-33: abonelik, telemetri, ticari paket, demo, destek, pilot, kılavuz) koşuyor; S-1/S-2 `app.css`, `Layout/*`, `Islemler.razor`'a dokunur. Orkestratör dalga 7'nin bu dosyalara dokunmadığını **teyit etmeden** S-1 başlamaz.
10. **Kurucuya yalnız görsel tercih** (talimat 16): renk şeması, yazı ölçeği. Düzen/akış kararları ekip oyuyla; kurucuya "onaylar mısın" diye gidilmez, karar verilir ve raporlanır.
11. **İki oturum aynı ağaçta** (kurucu 23 Eylül: "başka session'da yapmaktayız"). Bu oturum yalnız belge yazdı. Kod yazacak oturum, `ORKESTRATOR-DURUM.md`'ye "S-dalgası başladı, dokunulan dosyalar: …" satırı düşmeden başlamaz.

---

## 4. Hedef: "Tezgâh yüzü" — ölçülebilir kabul ölçütleri

"Sade" his değildir; aşağıdaki sayılar tutunca sadedir. Pusula'nın sayıları eşik, bizimkiler hedef.

| Ölçüt | Bugün (ölçülen) | Pusula | Hedef | Nerede ölçülür |
|---|---|---|---|---|
| Menüde ilk bakışta görünen satır (basit / pro) | 8 / 36–40 | 7 | **≤ 7 / ≤ 8** (gruplar kapalı) | `read_page` + denetim sayacı MENU_ACIK_SATIR |
| POS'ta ilk bakışta görünen kontrol (basit mod) | ölçülecek (S-0) | 3 alan + 13 düğme | **≤ 3 alan + ≤ 12 düğme**, tek grid | tasarım denetçisi betiği |
| Nakit barkodlu satış tık sayısı | 5 (elle ürün: 7) | bilinmiyor (S-0'da videodan sayılır) | **≤ 3** (okut → Kaydet → Yazdır) | canlı senaryo, Aşkın |
| Satırdaki eylem düğmesi (liste ekranları) | Cariler 7–8 | — | **≤ 3 + "⋯"** | tasarım denetçisi |
| Liste satır yüksekliği (Cariler) | 329 px | — | **≤ 64 px** | tasarım denetçisi |
| Ekran başına birincil düğme | 22 ekranda >1 | 1 | **1** | denetim sayacı BIRINCIL_DUGME |
| Form alanı (basit mod, cari/ürün) | ölçülecek | cari 3 zorunlu | **≤ 8 görünür**, gerisi katlanır | bUnit + denetçi |
| Düğme sistemi | 3 (+12 ad-hoc) | — | **1** (`.dugme`, 5 varyant) | denetim sayacı |
| Ham `font-size` / `padding` bildirimi (`app.css`) | 68 / 59 | — | **≤ 10 / ≤ 10** (yalnız print bloğu) | denetim sayacı |
| Kontrast AA (metin çiftleri) | 27/27 + 1 kod kusuru (kapandı, P-39) | — | **tüm çiftler**, seçilen şemada yeniden ölçülür | `TasarimBelirtecTestleri` |
| Menü anlatım süresi | ölçülmedi | 1:52 | **< 2 dk** | Aşkın senaryosu + kronometre |
| Uçtan uca satış eğitimi | ölçülmedi | 3:54 | **< 4 dk** | aynı |
| Yeni tezgâhtarın ilk satışı | ölçülmedi | — | **< 30 dk** | kurucu/müşteri kronometre |
| QA turu puanı (görsel / kullanıcı dostu, 5 üzerinden) | tur 1: 3,85 / 3,07; tur 8 bekleniyor | — | **≥ 4,3 / ≥ 4,0** | `qa-turu.js` |
| Özellik kaybı | — | — | **0** (§ 7.1) | envanter diff |
| 46 rota × 4 genişlik taşma | 0/0/0 (P-36c) | — | **0 kalır** | denetçi |
| Ekranda ham kod/enum/ULID | QA tur 7: ≥ 9 yer | 0 (Pusula ürün kodu gösterir, biz göstermeyiz) | **0** | denetim sayacı HAM_KOD_EKRANDA |
| Jargon (§ 5B sözlüğü) basit modda | ölçülecek | — | **0** | Aşkın okunabilirlik kapısı |
| Sayfada vurgu rengi taşıyan öğe | ölçülecek | — | **≤ 3** | denetçi VURGU_OGE_MAX |
| Yalnız-ikon düğme (metinsiz) | ölçülecek | — | **0** (kapat × ve tutamaç hariç) | denetçi |

Kurucunun "en güzel arayüz" isteği bu tabloya **bağlandı**; "güzel" ölçülemez, tık sayısı ve kontrast ölçülür (16 Eylül brifinin kararı, korunuyor).

---

## 5. Mimari karar: **iki yüz, tek gövde**

Yeni bir uygulama, yeni bir POS sayfası, yeni bir tablo **yazılmaz**. Var olan `BASIT / PROFESYONEL` mod mekanizması (UX-3, sunucuda çözülür, `ArayuzModuServisi`) iki yüzün anahtarı olur:

```
                 ┌──────────────── TEK GÖVDE ────────────────┐
                 │  API 332 uç · 66 tablo · storno · kapılar │
                 └───────────────┬───────────────────────────┘
        ┌────────────────────────┴─────────────────────────┐
  BASİT = "TEZGÂH YÜZÜ"                          PROFESYONEL = "TAM YÜZ"
  • menü 7 satır, grupsuz                        • 6 grup, KATLANIR (kapalı başlar)
  • POS = Pusula fişi (3 alan+düğme ızgarası     • POS = bugünkü 8 sekme + P-21 işlemleri
    + tek grid + fark 0 → Kaydet)                • tüm alanlar açık (Ö1 yoğun ölçek)
  • formlar ≤ 8 alan, gerisi ProAlan'da katlı    • 5 şablon (liste+çekmece / fiş / rapor /
  • Ö2 okunaklı ölçek (16 px)                      ayar-form / sihirbaz)
  • yeni kiracı VARSAYILANI (karar D1)           • SAHİP/MUHASEBE varsayılanı
```

**Neden bu yol (alternatifler ve reddi):**
- *Yeni "Tezgâh" sayfası yazmak* → ikinci POST yolu (Bertan KOŞUL 4 ihlali), 2.576 satırın kopyası, bUnit'in ikiye katlanması. **Red.**
- *Yalnız CSS ile "güzelleştirmek"* → menü ve fiş karmaşası kalır; müşterinin üç cümlesinden ikisi cevapsız. **Yetmez.**
- *Özellik budamak* → kurucu "mevcut özellikleri bozmadan" dedi; satış argümanımız uyum derinliği. **Yasak.**
- *İki yüz tek gövde* → mevcut mod mekanizması, mevcut testler, mevcut kapılar; geri dönüş anahtarı hazır (profesyonel = bugünkü ekran). **Seçildi.**

**Üç görünürlük kuralı (her ekranda, her dalgada uygulanır):**
- **≤ 7**: menüde ilk bakışta görünen satır.
- **≤ 5**: bir ekranda ilk bakışta görünen birincil öğe (başlık + arama/süzgeç + 1 birincil düğme + liste/fiş + 1 durum/özet).
- **≤ 3**: bir satırda görünen eylem; fazlası "⋯" menüsüne.

### 5A. Görsel yön — "en yeni, en şık, kurumsal" (kurucu talimatı, 23 Eylül)

Kurucu: *"görsellerde kurumsal olarak en yeni, en şık tasarımı istiyorum."* "Şık" ölçülemez; aşağıdaki **yön** ölçülebilir kurallara bağlandı. 2026'nın kurumsal SaaS dili (Linear, Stripe, Shopify Polaris, Supabase; sektör yazıları: "stratejik minimalizm", 20–30 çekirdek belirteç, progressive disclosure, karanlık tema birinci sınıf) ile Pusula'nın alınacak yanları (§ 2) birleştirildi. Adı: **"Sakin Lüks"** — kuyumcu vitrini gibi: az şey, iyi ışık, değerli olan öne.

| Öğe | Kural (S-1'de belirtece bağlanır) | Neden |
|---|---|---|
| **Yüzey** | Beyaz kart, çok açık gri zemin (`--yuzey-govde`), kartlar **1 px hafif kenarlık + çok hafif gölge** (gölge 2 kademe: duran / uçan). Gradyan yok, cam efekti yok, doku yok. | 2026 kurumsal dilin ortak paydası; Pusula'nın mor gradyanı ve gri sistem dropdown'ı tam tersidir |
| **Renk** | 1 marka vurgusu (altın amber, kurucu A/B/C'den seçer) + 5 kademeli nötr + 4 durum rengi. Vurgu **yalnız** birincil düğme, aktif menü, odak halkası ve KPI'da. Sayfada vurgu rengi taşıyan öğe ≤ 3. | Renk az olunca "renkleri iyi değil" şikâyeti biter; ölçü: VURGU_OGE_MAX = 3 |
| **Yazı** | Ö2 ölçeği (16 px gövde), 5 kademe, 3 ağırlık (400/500/600). Para/gram/has **tabular-nums, sağa dayalı, gövdeden 1 kademe büyük**. Karar D9: vendored açık lisanslı grotesk (Inter / IBM Plex Sans, OFL) `font-src 'self'` ile — Mehmet CSP onayı; onay yoksa sistem yığını kalır | Rakam en okunur öğe olmalı (tezgâhın işi rakam) |
| **Boşluk** | 8 px tabanlı 6 kademe; kart içi 16, kartlar arası 24, bölümler arası 32. Yoğunluk anahtarı yalnız pro modda. | 31 ham dolgu → 6 kademe |
| **Köşe** | 8 px kontrol, 12 px kart/çekmece, tam yuvarlak yalnız rozet. | Tek aile |
| **İkon** | Tek çizgi seti (mevcut vendored sprite, 1,5 px kontur), ikon **her zaman metinle** (yalnız ikon düğme yok — istisna: kapat ×, sürükle tutamacı). | Hedef kitle (§ 5B) ikonu tek başına okumaz |
| **Kabuk** | Sol menü **açık zemin** (bugünkü koyu menü yerine; karanlık temada koyu) ya da koyu kalır — kurucu seçer (mockup 1). Menü ikon+metin, kapalı gruplar, 240 px; üst bar tek satır. | Koyu menü + beyaz gövde kontrastı "iki program yan yana" hissi veriyor; müşterinin 2. cümlesi |
| **Hareket** | 120–200 ms geçiş, yalnız açılır/kapanır ve çekmece; `prefers-reduced-motion` saygılı. Süs animasyonu yok. | Şıklık = hız + sakinlik |
| **Durumlar** | İskelet yükleme (var), boş durum = 1 cümle + 1 düğme, başarı = 3 sn yeşil bant (Toast), hata = bant "ne oldu + ne yap". | UX-S28 korunur |
| **Giriş ekranı** | Sol: tek kart (e-posta, parola, Giriş); sağ: sakin görsel (vitrin fotoğrafı / soyut altın doku) + tek cümle. PusulaX demosundaki iki panelli düzen, ama pazarlama sloganı değil. | İlk izlenim |
| **Karanlık tema** | Aynı belirteçler, aynı hiyerarşi; kâğıt daima beyaz (korunur). | Birinci sınıf tema |
| **Tablo** | Yapışkan başlık, zebra yok, satır ayracı 1 px, satır yüksekliği 48 px (basit) / 40 px (pro), sayılar sağa. | Okunaklılık + yoğunluk dengesi |

**MİHENK İLKESİ (kurucu, 23 Eylül):** *"Sadelikte ve kullanımda Google gibi olacağız. Projemize bakan 'ne kadar kolay ve kullanışlı, bir o kadar da çok özelliği var; rakiplerinden fazla iş yapıyor ama daha kolay' desin."* Her dalganın kabul cümlesi budur: ilk bakışta az şey, bir tık altında her şey. Ölçüsü § 4 tablosu (≤7 / ≤5 / ≤3 kuralı, tık sayısı, özellik kaybı 0).

**TEMA SEÇİMİ ÜRÜN ÖZELLİĞİDİR (kurucu, 23 Eylül: "renkleri seçilebilir olması lazım, müşteri tema olarak seçebilsin"; sonra "daha fazla renk seçeneği ekle").** Tek şema seçilmez; ürün **8 renk şeması** ile gelir (Platin Mavi · Zümrüt · Grafit · Bordo · Okyanus · Menekşe · Zeytin · **Fildişi = açık menü**; altın/amber yok, kurucu ve müşteri beğenmedi; her şema 13 semantik belirteçle tanımlı, açık menü için `menuBaslik`/`menuAktifMetin` belirteçleri ayrıca) × aydınlık/karanlık × 3 yazı boyutu (16/18/20 px). Her kullanıcı kendi tercihini Ayarlar → Görünüm'den seçer; sahip dükkân varsayılanını belirler. Teknik yol: mevcut `TemaServisi` (açık/koyu, `data-tema`) yanına `data-sema` özniteliği; `belirtecler.css` 2. katman (semantik) şema başına 4 blok, ham palet ortak; kontrast testi 4 × 2 = 8 kombinasyonu ölçer; yazı boyutu `--yazi-govde` üç değer. Tercih kullanıcı satırında (`kullanicilar.tema/sema/yazi_boyutu`) + kiracı varsayılanı — UX-3 mod deseninin aynısı. Kararlar D6/D11 buna göre kapandı: kurucu tek şema seçmez, varsayılanı seçer (öneri: Platin Mavi).

**İlk taslak (23 Eylül, kurucu isteği "Figma gibi çizim"):** tasarım tuvali https://claude.ai/artifact/XZha3bXRKPVz3YPn4jKtQx — 5 artboard (Giriş · Panel · Satış fişi basit mod · Müşteriler liste+çekmece · Profesyonel mod), A Altın Klasik şema, açık menü, 16 px. B/C şemaları ve koyu tema kurucu isterse aynı tuvale eklenir.

**Mockup teslimi (S-1 öncesi, kurucu görsel onayı için):** 4 ekran (Giriş · Panel · Tezgâh fişi · Müşteriler listesi) × 3 şema (A/B/C) × açık/koyu = 24 statik HTML, `dokumanlar/02-mimari/sadelestirme/mockup/`, saf CSS, belirteçlerle; 5199'daki gerçek ekranın "önce/sonra" yan yana görüntüsüyle. Kurucu bir şema seçer; **düzen ekip oyuyla** mühürlenir. Mockup'lar üretim koduna kopyalanmaz, belirteç değerleri `belirtecler.css`'e taşınır.

### 5B. Kullanıcı profili — ilkokul/lise mezunu tezgâhtar, **sıkılmadan** kullanmalı (kurucu talimatı, 23 Eylül)

Kurucu: *"bu programı kullanacak adamlar ilkokul ya da lise mezunu olacak; onları sıkmadan kullanmaları gerek."* Bu, planın **en bağlayıcı** tasarım kısıtıdır; "şık"tan önce gelir. Persona: **Kapalıçarşı'da 3 yıldır tezgâhta çalışan, 20–50 yaş, telefonu iyi kullanan, bilgisayar formundan çekinen tezgâhtar.** Her ekran bu kişiyle test edilir (Aşkın senaryosu).

**12 kural (her dalganın kabul ölçütüne girer):**
1. **Sade Türkçe, jargon sıfır.** Ekranda muhasebe/yazılım terimi görünmez; teknik terim gerekiyorsa yanında sade karşılığı. Sözlük göçü aşağıda.
2. **Kod gösterilmez.** ULID, enum (`SATIS_IADE`), ham slug, "MANUAL" gibi değerler ekranda **0** (QA turu 7'de 9 yerde vardı). Denetim sayacı HAM_KOD_EKRANDA = 0.
3. **Etiket ≤ 3 kelime, cümle ≤ 10 kelime, mesaj ≤ 2 cümle.** Bant metni: birinci cümle ne oldu, ikinci cümle ne yapmalı.
4. **Tek ekranda tek iş, tek birincil düğme.** Karar sorulacaksa iki seçenek, ikisi de düz cümle ("Fişi kaydet" / "Vazgeç"); "İptal" kelimesi yalnız "vazgeç" anlamında.
5. **İkon + metin her zaman birlikte.**
6. **Rakam büyük, birim yanında** ("12.500 ₺", "2,30 g", "1,85 g has"); nokta/virgül Türkçe.
7. **Form yerine fiş.** Veri girişi mümkün olduğunca tablo satırında (barkod okut → satır gelir), form açılırsa ≤ 8 alan, zorunlular en üstte ve yıldızlı, gerisi "Daha fazla ▾".
8. **Hata korkutmaz, yol gösterir.** Kırmızı bant yerine sarı uyarı + düzeltme bağlantısı; kilitli düğmenin sebebi düğmenin altında yazar (UX-S30 var, yaygınlaşır).
9. **Geri alınabilirlik güven verir.** "Yanlış kaydettim" → storno düğmesi her fişte görünür ve adı sade: **"Geri al (düzeltme fişi)"**; onay penceresi tek cümle.
10. **Hatırlar.** Son kullanılan kasa, ödeme türü, müşteri, ayar; boş form açılmaz (Pusula'nın hurda alışta satıcıyı hatırlamaması raporda eleştirilmişti).
11. **Klavye zorunlu değil, barkod birinci.** Fare/dokunma ile her iş biter; F-tuşları ve Ctrl+K pro modda ek.
12. **30 dakikada öğrenilir, 4 dakikada satış.** Ölçüm § 4; öğreten şey uygulama içi tur (S-5), PDF değil.

**"Sıkılmadan" = hız + az adım + tekrar yok:** arama < 300 ms, sayfa geçişi < 1 s, aynı bilgi iki kez sorulmaz (kasa iki kez sorulmuyor — UX-4b), onay pencereleri yalnız geri dönülemez işlemde, satış sonrası ekran temizlenip barkod kutusuna döner.

**Sözlük göçü (S-1b, tek dosya `Kuyumcu.Core/I18n/Sozlukler.cs` TR katmanı; İngilizce dokunulmaz):**

| Bugün | Sade karşılığı (basit mod etiketi) | Not |
|---|---|---|
| Cari | **Müşteri** (tedarikçi için "Toptancı") | pro modda "Cari" parantezde kalabilir |
| Storno / ters kayıt | **Geri al (düzeltme fişi)** | mevzuat metninde "storno" kalır |
| Tevsik haddi | **30.000 ₺ üstü nakit kuralı** | eşik sayısı sunucudan gelir, metin sabit değil |
| MASAK kimlik tespiti | **Kimlik gerekli (yasal)** | |
| Konsinye | **Emanet mal (satılırsa öde)** | |
| Ciro / rücu | **Çeki devret / geri al** | |
| Has | **Has** (sektör terimi, kalır) | yanına "24 ayar karşılığı" ipucu |
| Milyem | **Milyem** (kalır) | pro modda |
| Vezne | **Kasa kuyruğu** | |
| Gider pusulası | **Gider pusulası** (yasal ad, kalır) + "hurda alış belgesi" ipucu | |
| Mutabakat | **Karşılaştırma** | |
| Pasife al | **Kullanımdan kaldır** | |
| Denetim izi | **Kim ne yaptı** | |
| Fiş türü enum'ları (SATIS_IADE …) | ekranda **0** | UX-4d sözlüğü tamamlanır |

Risk: bUnit testlerinin bir kısmı metinle bulur (çoğu id ile); göç öncesi `grep -c "Find(\"button:contains"` sayımı, kırılan testler etiket sabitine bağlanır. Sözlük göçü **ekip oyuyla** (Köprü teslimat + Eray satış merceği: müşteri diliyle uyum).

**Okunabilirlik kapısı (Aşkın, her dalga):** rastgele 10 ekran → her bant/etiket/başlık "lise mezunu 5 saniyede anlar mı" ölçütüyle puanlanır; 1 jargon = KALDI. Ayrıca kurucu ya da müşteri dükkânından bir tezgâhtarla **kronometreli ilk satış** (S-5).

---

## 6. Dalgalar (S-0 → S-6) — emin adımlar

### 6.0 Konum ve mühür

Bu plan **"Katman 8 deneyim"in yeniden tanımıdır**: dalga 8'de bekleyen UX-2-B (27 sayfa şablon yenileme), UX-3-C (kalan sayfaların basit mod adaptasyonu) ve UX-2-D (erişilebilirlik/ölçüm) S-dalgalarına **çekilir**; UX-1 (düzenlenebilirlik) ve dalga 7 (ticari) olduğu gibi sürer. Kurul tur 4'ün "Dalga 7 mekanizma borcu" kalemleri (S-3c 375 px kartlaştırma, K4 düğme yükseklikleri) S-1/S-3'ün içinde kapanır — çift iş açılmaz.

**Ekip oyuna sunulacak kararlar (Tamer Bey mühürler; oy çıkmadan ilgili paket başlamaz):**

| # | Karar | Öneri | Mercekler |
|---|---|---|---|
| D1 | Yeni kiracı varsayılan arayüz modu | **BASİT** (`tenants.varsayilan_arayuz_modu`); TEZGÂHTAR rolü her kiracıda BASİT; SAHİP ilk girişte seçer | Bertan, Aşkın, Eray |
| D2 | "Peşin Müşteri" sistem carisi | Kiracı başına tek, silinemez/pasife alınamaz sistem carisi; satışta ön seçili; **tevsik/MASAK eşiği üstünde kabul edilmez** (sunucu 400: "kimlik tespiti için gerçek cari seçin") — fail-closed | Mehmet (veto alanı), Bertan |
| D3 | Karma ödemeye ALTIN bacağı | Önce bugünkü davranış belgelenir (has defteri hangi yönle işler), sonra bacak eklenir; basit modda karma **her zaman** açık (`ZorunluGorunur`) | Bertan, Mehmet, Aşkın |
| D4 | Üst bar sadeleştirme | Sağ blok tek "hesap" menüsüne iner (mod, tema, dil, çıkış); kur şeridi HAS önde tek satır; şube seçici yalnız çok şubeli kiracıda çizilir. Mevcut id'ler korunur, yer değişir | Ertuğ, Aşkın |
| D5 | UX-2-B / UX-3-C / UX-2-D'nin dalga 8'den S-dalgalarına çekilmesi | Evet; `MIMARI-KALAN-PAKETLER` dalga tablosu güncellenir | Bertan, Ramazan-ajan |
| D6 | Renk şeması B/C seçilirse | Başarı rengi maviye kayar (C'de yeşil çakışması); kontrast testleri yeniden ölçülür | Ertuğ |
| D7 | Kapsam dondurma istisnası | Peşin Müşteri (D2), para üstü önizlemesi, "Açık Pozisyon" kutucuğu — yeni modül değil, kablo; oya sunulur | Tümü |
| D8 | Menü grup katlama tercihi | Kapalı başlar; aktif rotanın grubu açık; kullanıcı açık/kapalı tercihi `MenuTercihServisi` ile kalıcı (mevcut `/tercihler/benim` ucu, yeni uç yok) | Ertuğ, Bertan |
| D9 | Vendored yazı tipi (Inter / IBM Plex Sans, OFL) | `wwwroot/font/` altına lisans başlığıyla; CSP `font-src 'self'`; `index.html` script sayısı değişmez. Mehmet onaylamazsa sistem yığını kalır | Mehmet (veto), Ertuğ |
| D10 | Sözlük göçü (§ 5B tablosu, TR basit-mod etiketleri) | Tek dosya `Sozlukler.cs`; mevzuat adları (gider pusulası, MASAK) korunur; müşteri diliyle uyum | Köprü, Eray, Aşkın |
| D11 | Kenar çubuğu zemini | Açık zemin (öneri) ↔ bugünkü koyu; mockup 1'de ikisi de gösterilir, **kurucu seçer** (görsel tercih) | Kurucu |

### S-0 · Ölçüm tabanı ve altın standart kartı — **kod yok, 1 gün**

Amaç: "sade oldu mu" sorusunun cevabı sayı olsun; Pusula'ya dair boşluklar kapansın.

1. **QA turu 8 sonucunu al** (`ham/qa-turu-8.json` varsa; yoksa `betikler/qa-turu.js` ile koş, yayın = ağaç şartıyla). Görsel/kullanıcı dostu puanı § 9'a yaz.
2. **Pusula referans kartı — 23 Eylül'de BÜYÜK ÖLÇÜDE YAPILDI:** `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md` (6 görsel gözle incelendi, PusulaX demosu 10:51 kare şeridiyle, klasik satış videosu altyazı parçalarıyla). **KALAN:** tık sayısı hâlâ sayılamadı (video kareleri tarayıcı bölmesinde siyah, kare şeridi yazı okutmuyor). Kapatma yolu: kurucu müşterinin Pusula'sını 10 dk izletir ya da video kurucunun makinesinde açılıp "nakit satış" ve "ana menü" dersleri tık tık sayılır. Sayılmazsa defterde "SAYILAMADI" kalır, uydurulmaz.
3. **Bizim taban ölçümü** — tasarım denetçisi betiği (`TASARIM-DENETIMI-2026-09-21.md` § 10'daki JSON üreticisi) yeniden koşar: 46 rota × basit/pro × 4 genişlik; her rota için ilk bakışta görünen kontrol sayısı, satır eylemi, birincil düğme sayısı, form alanı. Basit modda nakit barkodlu satış tık sayısı canlıda sayılır (5198, test kiracısı, sonunda silinir).
4. **Özellik envanteri dondurulur** (§ 7.1): `KAPSAM-ENVANTERI.md` + `MenuTanimi.Ogeler` (36) + `MenusuzRotalar` (6) + POS `TumTurler` (8) + her sayfadaki düğme/eylem listesi (`frontend-envanter.json` yeniden üretilir) → `sadelestirme/ENVANTER-S0.json`. S-6'da aynı betik koşar, diff boş olmalı (yalnız "görünürlük" sütunu değişebilir: gorunur / katli / pro).
5. **Kurucu müşteriye üç soru sorar** (§ 8.3) — cevaplar `sadelestirme/MUSTERI-SESI.md`'ye.
6. **Kabul eşikleri mühürlenir** (§ 4 tablosu, ekip oyu).

Kapı: § 9 defterinin "S-0" sütunu dolu; envanter dosyası var; PUSULA-REFERANS'ta en az 3 videonun tık sayısı var.

### S-1 · Kabuk ve görsel dil — **46 ekranı tek hamlede, 2–3 gün**

Dosyalar: `wwwroot/css/app.css`, `wwwroot/css/belirtecler.css`, `Layout/KenarCubugu.razor`, `Layout/UstBar.razor`, `Layout/AnaYerlesim.razor`, `Servisler/MenuTercihServisi.cs`, `dotnet/denetim.sh`, testler. Sayfa dosyalarına **dokunulmaz**.

| # | İş | Ayrıntı | Kabul |
|---|---|---|---|
| 1.1 | **Katlanır menü** (F1, D8) | Grup başlığı `<button aria-expanded>` olur; kapalı başlar, aktif rotanın grubu açık; basit modda grup başlığı çizilmez (7 satır düz). Favoriler en üstte kalır, arama kalır. Klavye: Enter/Space açar, Alt+↑/↓ sıralama korunur. | `read_page`: pro modda ilk bakışta ≤ 8 satır + grup başlıkları; basit ≤ 7; bUnit `MenuKatlamaTestleri` (açık grup hatırlanır, yetkisiz öğe kapalı grupta da yok — M-UX3-06 mutasyonu) |
| 1.2 | **Üst bar** (D4) | Kur şeridi: `HAS` önde, `Gram`, `USD`; EUR/GBP ipucuna; "son sorgu" ikonun `title`'ına; sağda bildirim + hesap menüsü (mod/tema/dil/çıkış). `#ust-mod-anahtar`, `#ust-mod-basit/-profesyonel` id'leri hesap menüsünün içinde **aynı id ile** yaşar. | 1024 px'te taşma 0 (P-39 sayacı); üst barda ilk bakışta ≤ 5 öğe; mevcut UstBar testleri yeşil |
| 1.3 | **Tek düğme sistemi** | `FRONTEND-KIMLIK` § 5: `.dugme` + 5 varyant (birincil/ikincil/tehlike/sessiz/bağlantı); `.btn*` → `.dugme*` takma adı CSS'te, kaynakta kademeli değişim S-3/S-4'te; ekran başına 1 birincil kuralı denetim sayacı olur | denetim: DUGME_SISTEM_SAYISI = 1, BIRINCIL_DUGME_MAX = 1 (sayfa dosyaları S-3/S-4'te düzelirken tavan azalır) |
| 1.4 | **Tipografi Ö2 varsayılan, Ö1 profesyonel** | `--yazi-govde: 1rem` (16 px), 5 kademe; pro modda `:root[data-yogunluk="yogun"]` 14 px; 27 farklı boyut → 5; `pt` birimleri yalnız `@media print` | HAM_FONT_SIZE ≤ 10; iOS otomatik yakınlaştırma yok (girdi ≥ 16 px) |
| 1.5 | **Boşluk / yarıçap / gölge disiplini** | 6 boşluk kademesi, 3 yarıçap, 2 gölge; 31 ham dolgu → belirteç; `720px` → `768px` | HAM_PADDING ≤ 10, KIRILMA_NOKTASI = 3 |
| 1.6 | **Renk şeması** (kurucu seçer, § 8.1) | Seçilen şema yalnız `belirtecler.css` 2. katmana; kenarlık `#8592a5 → #7c8a9e`; `--kenar-ayrac` 3:1'e; işlevsiz bant kenarları kaldırılır; üç "koyu tema" tanımı tek kaynağa | `TasarimBelirtecTestleri.Kontrast_AA_saglanir` tüm çiftler; karanlık tema 46/46 rota temiz (QA tur 7 çapraz kontrolü tekrarlanır) |
| 1.7 | **Dokunma hedefi + kontrol yüksekliği** | tek `--kontrol-yuksekligi` (40 px masaüstü / 44 px ≤768); `.dugme/.girdi/.secim/.sekme` aynı yükseklik | denetçi: yükseklik grubu ≤ 2 (bugün 10, hedef ≤ 3 kurul tur 4'te yazılı) |

| 1.8 | **S-1b Sözlük göçü** (D10, § 5B) | `Sozlukler.cs` TR katmanında basit-mod etiketleri; enum/ham kod gösteren 9 yer sözlüğe bağlanır; bant metinleri "ne oldu + ne yap" kalıbına | HAM_KOD_EKRANDA = 0; Aşkın okunabilirlik kapısı 10/10 ekran; metinle bulan bUnit testleri sabite bağlandı, sayı düşmedi |
| 1.9 | **Giriş ekranı** (§ 5A) | Tek kart + sakin görsel; `Giris.razor` 375 px taşması (şartname 12) burada kapanır | 4 genişlikte taşma 0; dokunma hedefi ≥ 44 px |

Geri dönüş güvenliği: CSS değişiklikleri `app.css` sonuna tek blok ("S-1 — 2026-09-xx") olarak; eski kurallar geçersiz kılınır, silinmez; blok kaldırılınca eski görünüm döner. Sözlük göçü tek dosyadır; eski etiketler yorumda saklanır.

### S-2 · Tezgâh fişi — Pusula formülü, **4–6 gün, en riskli paket**

Dosyalar: `Pages/Islemler.razor` (basit mod dalı), `Servisler/BasitPos.cs`, yeni `Bilesenler/Fis/*.razor` (POST etmez), `Bilesenler/BasitSatisAkisi.razor`, API'de yalnız D2/D3 için küçük dokunuşlar (`SatisModulu`, `CariModulu`), migration (Peşin Müşteri bayrağı: `cariler.sistem_carisi` bool — D2 geçerse).

**Basit mod POS'un yeni yüzü (ilk bakışta görünen her şey):**

```
┌ Müşteri [Peşin Müşteri ▾]  Bakiye: 0,00 g HAS / 0 ₺   Tarih [23.09.2026]  Para [TL ▾] ┐
│                                                                                       │
│ [ SATIŞ (barkod) ] [ SATIŞ (elle) ] [ ALIŞ / HURDA ] [ TAHSİLAT ] [ ÖDEME ] [ İADE ]  │
│ [ DEĞİŞİM ]        [ EMANET ]       [ İSKONTO ]      [ AÇIKLAMA ]                      │
│                                                                                       │
│  VERDİM (çıkış)                          │  ALDIM (giriş)                             │
│  Ürün · Ayar · Gram · Adet · Tutar       │  Nakit / Kart / Havale / Eski altın / Döviz│
│  …                                       │  …                                         │
│                                          FARK: 0,00 ₺        [ Kaydet ve Yazdır ]     │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

| # | İş | Ayrıntı | Kabul / kanıt |
|---|---|---|---|
| 2.1 | **Fiş gövdesinin bileşenlere ayrılması** | `Islemler.razor` 2.576 satır: kalem tablosu, ödeme bacakları, hurda satıcı bloğu, takas bloğu → `Bilesenler/Fis/` (parametre + `EventCallback`, `ApiIstemci` yok). `Kaydet()` yerinde kalır. | Satır sayısı ≤ 1.400 (Bertan'ın eski 1.390 hedefi); id envanteri diff boş; POS testleri (Karma, Iade, Barkod, Teklif, BasitMod) yeşil |
| 2.2 | **Sekmeler → düğme ızgarası** (basit modda) | `BasitPos.Turler` 3 → 6 görünür işlem (SATIS, ALIS/HURDA_ALIS, TAHSILAT, ODEME, SATIS_IADE, TAKAS=değişim); EMANET düğmesi `/emanet`'e derin bağlantı (`DerinBaglanti.cs` mevcut). Pro modda 8 sekme aynen. Düğme ≥ 44 px, ikonlu (`Ikonlar`). | basit modda görünen kontrol ≤ 3 alan + ≤ 12 düğme; her düğme `#islem-tur-*` id'sini korur |
| 2.3 | **Tek grid: VERDİM / ALDIM + FARK** | Kalemler (çıkış) ve ödeme bacakları (giriş) aynı kartta iki sütun; altta FARK; fark 0 → Kaydet aktif; fark > 0 ve gerçek cari seçili → "Açık hesap olarak kaydet" (mevcut VERESİYE yolu); Peşin Müşteri'de fark ≠ 0 kaydedilemez (sunucu da reddeder). | canlı fiş: nakit + eski altın + kart üçlü bacak 200; fark ≠ 0 sistem carisiyle 400; mutasyon: FARK kuralı bozulunca test kırmızı |
| 2.4 | **Peşin Müşteri** (D2) | Kiracı tohumlama (mevcut ADR-0027 modül-başı tohumlama mekanizması); satışta ön seçili; tevsik/MASAK eşiğinde sunucu 400 + ekranda "gerçek müşteri seçin" bandı. | canlı: 185.000 TL üstü Peşin Müşteri → 400; UX-3 yasal kapı testleri yeşil |
| 2.5 | **Karma ödeme basit modda açık + ALTIN bacağı** (D3) | `ZorunluGorunur` bağlanır; bacak türü ALTIN: gram·ayar → has, has defterine giriş (mevcut HURDA_ALIS yolu ile aynı defter kuralı; belgelenir, sonra yazılır). | mutasyon: ALTIN bacağı has defterine yazmazsa test kırmızı; 35.000 TL = 30.000 nakit + 5.000 kart tevsik kapısıyla kaydedilir (bugün kaydedilemiyor) |
| 2.6 | **Bakiye rozeti** (F4) | Cari seçilince HAS + TL bakiyesi rozet (veri zaten yükleniyor). | bUnit: rozet metni cari bakiyesiyle aynı |
| 2.7 | **Kaydet → Yazdır** (F5) | Başarı bandında "Fiş yazdır" → `/cikti/{Tur}/{Id}`; ham ULID gösterilmez (QA tur 7 bulgusu). | canlı: bağlantı 200 |
| 2.8 | **Barkod odağı + para üstü + satıcı personel** | Sayfa açılınca `#islem-barkod` odaklı (mekanizma mevcut); nakit bacağında "alınan" alanı → para üstü satırı (D7); `personel_id` oturum kullanıcısı varsayılan, pro modda değiştirilebilir. | 3 tık senaryosu canlıda: okut → Kaydet → Yazdır |
| 2.9 | **Pro modda P-21 işlemleri** | Rezerv · avans · konsinye çıkış/iade · siparişten satış · ciro/rücu — sunucuda 14 uç hazır; pro modda 8 sekmenin yanına "Diğer ▾" açılır listesi. **Basit modda görünmez.** | her uç ekrandan çağrılabilir (UX-S25 "uç var düğme yok" yasağı kapanır) |

Riskler: `Islemler.razor` bölünmesi bUnit'i kırabilir → önce id envanteri, sonra bileşen çıkarma, her adımda tam süit. D2/D3 Mehmet vetosu alanında → oy çıkmadan 2.4/2.5 yazılmaz, 2.1–2.3 ve 2.6–2.8 yazılır.

### S-3 · Günlük 7 ekran (basit modun ekranları) — **4–5 gün**

Sıra (her gün en çok açılan önce): Panel → Cariler → Ürünler → Kasalar → Raporlar (günlük özet) → Etiketler → Bildirimler. Her ekranda aynı dört hamle:

1. **İlk bakışta ≤ 5 öğe**: başlık şeridi (`SayfaBasi`: süzgeç yuvası + tek birincil düğme), liste/fiş, tek özet.
2. **Satırda ≤ 3 eylem + "⋯"**: Cariler'deki 7–8 düğme → Düzenle · Tahsilat · ⋯ (satış, veresiye, emanet, pasife al, storno…). Satır yüksekliği ≤ 64 px.
3. **Düzenleme çekmecede** (`Cekmece.razor` mevcut): liste bağlamı kaybolmaz; basit modda form ≤ 8 alan, gerisi `ProAlan` içinde katlı ("Gelişmiş ▾").
4. **Boş durum = tek CTA** (`BosDurum` CTA yuvası).

Ekrana özel:
- **Panel:** basit modda varsayılan 5 kutucuk (gün özeti · kasa · kur (HAS önde) · bekleyen · hızlı eylem); hava kutucuğu kalır (kurucu talimatı) ama selam satırının içinde küçük; "Açık Pozisyon" kutucuğu D7 geçerse hızlı eylemin yanına. Pro modda 9 kutucuk aynen.
- **Cariler:** QA tur 7'nin "aktif cari pasife alınamıyor" kritiği P-38'de satıra taşındı; "⋯" menüsüne girer.
- **Ürünler:** süzgeç satırı tek `Secici` + "Süzgeç ▾" (4 dropdown katlanır); 375 px kırpılma bulgusu böylece kapanır.
- **Raporlar (basit):** "Bugün" tek sayfa — sattım / aldım / kasa / has pozisyonu / bekleyen; rapor merkezi kartları pro modda.

Kabul: 7 ekranda tasarım denetçisi 0 hiza kusuru; satır eylemi ≤ 3; form alanı ≤ 8 (basit); mevcut sayfa testleri yeşil; envanter diff'inde yalnız "görünürlük" sütunu değişmiş.

### S-4 · Profesyonel yüz — kalan 39 ekran, 5 şablon — **8–12 gün** (eski UX-2 Faz B)

Şablonlar: **Liste+Çekmece** (Cariler, Ürünler, Faturalar, Evraklar, Siparişler, Kullanıcılar, Banka, Kasalar, Gider pusulaları …) · **Fiş** (İşlemler, Vezne, Hurda, Gider, Emanet) · **Rapor** (Raporlar/*, Denetim izi, Satış analizi, Kâr-zarar) · **Ayar/Form** (Ayarlar, İşletme, Yetkiler, POS, Kur) · **Sihirbaz** (Kurulum, Veri içe/dışa aktarım, Yedekler, Sayım).

Kurallar S-3 ile aynı; ek olarak `FRONTEND-SARTNAME § 5` öncelik listesi (sayı hizası `.sayi`, `Bicim.Para/Gram`, dropdown boş seçenek dili, zorunluluk yıldızı, etiket bağlama) bu dalgada tamamlanır. 4 alt dalga (S-4a…d), her biri ~10 ekran, her birinin sonunda kapı + yayın + QA. `FRONTEND-SARTNAME § 4.2` kilitli listesi dalga 7'nin dokunduğu dosyalara göre **yeniden** çıkarılır.

Kabul: denetim sayaçları (HAM_INPUT_SERBEST=0 korunur, BIRINCIL_DUGME_MAX=1, DUGME_SISTEM=1, SAYI_SINIFI eksik=0); 46 rota × 4 genişlik taşma 0; QA puanı ≥ 4,3 / ≥ 4,0.

### S-5 · Öğrenme ve öğretme — **2–3 gün**

- **İlk giriş sihirbazı** (eski UX-3-C, 3 soru): "Kaç kişi çalışıyor? / Barkod okuyucu var mı? / Günde kaç satış?" → mod ve panel düzeni önerisi; Ayarlar'dan değiştirilebilir. Kuruluş sihirbazı (T-2) ile birleşmez, ondan sonra bir kez çıkar.
- **Uygulama içi 7 adımlı tur** (menü < 2 dk hedefi): `Kilavuz.razor` içinde "Tur başlat" → her adım tek ekran + tek cümle; kaydedilmez, tekrar açılır.
- **Bağlam duyarlı yardım:** her `SayfaBasi`'nda "?" → kılavuzun ilgili bölümüne çapa (27 bölüm tek kaynak; P-33 kılavuz paketiyle koordine, çift iş yok).
- **Ölçüm:** Aşkın senaryoları "menü anlatımı", "uçtan uca satış", "yeni tezgâhtar ilk satış" kronometreyle; sonuç § 9'a. Pusula'nın 1:52 / 3:54 / 45 dk eşikleri geçilmezse dalga kapanmaz.

### S-6 · Müşteriye yeniden gösterim + kurul — **1 gün + müşteri takvimi**

- Demo kiracısı (P-30, QA fikstüründen türetilmemiş) ile kurucu **aynı müşteriye** gösterir; § 8.3'teki üç soru yeniden sorulur; puan ve cümleler `MUSTERI-SESI.md`'ye.
- Envanter diff (§ 7.1) koşar: özellik kaybı = 0 kanıtı.
- Hazırlık kurulu (7 mercek) "sadelik" merceğini ayrıca puanlar; `RAKIP-KIYAS-RAPORU` ve `PUSULA-KIYAS-RAPORU` § 3–7 güncellenir.

### 6.9 Her dalganın ritüeli (değişmez)

1. **Taslak/mockup** (statik HTML, `dokumanlar/02-mimari/sadelestirme/mockup-S<n>.html`, saf CSS, belirteçlerle) → ekip oyu düzen için; **yalnız renk/yazı kurucuya**.
2. **Uygulayıcı** (Opus, çaba medium, eşzamanlılık ≤ 4, yaz-seri) → her paket sonunda `dotnet build` 0/0, tam süit, mutasyon kanıtı (kuralı boz → kırmızı → geri al), canlı doğrulama 5198'de, test kiracısı silinir.
3. **Hasım doğrulayıcı** (farklı model) → ≤ 2 düzeltme turu.
4. **Orkestratör kapısı** `betikler/kapi.sh` → yayın 5199 (`betikler/qa-sunucu.sh`, yayın damgası = ağaç damgası).
5. **QA turu** `betikler/qa-turu.js` (46 rota, öykünme kapalı) + **tasarım denetçisi** (267 ölçüm betiği) → § 9 defteri güncellenir.
6. **Gerileme yasağı:** test sayısı düşmez; denetim sayaçları tavanı aşmaz; taşma 0 kalır; QA puanı düşmez. Düşerse dalga geri alınır (CSS bloğu / bileşen sarmalayıcı kaldırılır), sebebi rapora yazılır.
7. `ORKESTRATOR-DURUM.md`'ye tarih-saatli satır; `SONRAKI-OTURUM-PROMPT.md` § A19'a özet.

**Dürüst takvim (DOĞRULANMALI, her dalga sonunda yeniden ölçülür):** S-0 1 gün · S-1 2–3 · S-2 4–6 · S-3 4–5 · S-4 8–12 · S-5 2–3 · S-6 1 = **22–31 ajan-günü**; dalga 7 ile dosya çakışması yoksa paralel belge hattıyla **4–6 takvim haftası**. Bu rakam 16 Eylül'den beri üç kez şişen takvimlerin dersiyle (kurul tur 4) **ilan edilmez**, yalnız iç plandır.

---

## 7. Riskler ve geri dönüş güvenliği

### 7.1 "Özellik kaybı = 0" sayacı
S-0'da üretilen `ENVANTER-S0.json` (ekranlar, menü öğeleri, POS türleri, satır eylemleri, düğmeler, API uçları) S-3/S-4/S-6 sonunda aynı betikle yeniden üretilir. Her kayıt üç durumdan birinde olmalı: `gorunur` / `katli` (ProAlan, ⋯, katlanır grup) / `pro` (yalnız profesyonel modda). **Dördüncü durum yok**; kaybolan kayıt = dalga KALDI.

### 7.2 Diğer riskler
| Risk | Önlem |
|---|---|
| İki oturum aynı ağaca yazar | § 3.11: kod yazan oturum ORKESTRATOR'a "S-dalgası, dosyalar" satırı düşer; diğer oturum o dosyalara dokunmaz |
| `Islemler.razor` bölünmesi bUnit'i sessizce kırar | id envanteri diff'i kabul ölçütü; bileşen çıkarma adım adım, her adımda tam süit `--logger trx` |
| Basit mod yasal kapıyı gevşetir | Kapı bayrakları sunucu kaynaklı kalır (UX-3-B deseni); Peşin Müşteri eşik üstünde sunucuda reddedilir; Mehmet vetosu D2/D3'te |
| "Sade" diye özellik saklanır, müşteri bulamaz | Arama (Ctrl+K) her şeyi bulur; "⋯" ve "Gelişmiş ▾" etiketleri sözlükten; kılavuz çapaları (S-5) |
| Renk değişimi 27 kontrast çiftini bozar | Şema yalnız semantik katmana; `Kontrast_AA_saglanir` tüm çiftleri yeniden ölçer; B/C'de başarı rengi kaydırılır (D6) |
| Dalga 7 ile dosya çakışması | S-1 başlamadan orkestratör dalga 7'nin dokunduğu dosya listesini çıkarır; çakışan dosya varsa S-1 dalga 7'nin o paketi bitene kadar bekler |
| Pusula videoları izlenemez / ekran görüntüleri inmez | S-0'da "İZLENEMEDİ" yazılır; hedefler mevcut rapordaki 5 ekran görüntüsü + ders sürelerine dayanır; uydurma tık sayısı yazılmaz |
| Kurucu renk seçmez | Varsayılan A (Altın Klasik, kenarlık koyulaştırılmış) + Ö2 ile ilerlenir; seçim gelince yalnız `belirtecler.css` değişir |
| Takvim şişer | Her dalga sonunda yeniden ölçülür; S-4 dört alt dalgaya bölündüğü için yarım kalsa da ilk 7 + 10 ekran sevk edilebilir durumda kalır |

### 7.3 Geri dönüş anahtarları
- **Mod anahtarı:** profesyonel mod = bugünkü tam yüz; basit yüz beğenilmezse kullanıcı/kiracı pro'ya döner, veri değişmez.
- **CSS bloğu:** S-1 tek blok; kaldırılınca eski görünüm.
- **Bileşen sarmalayıcıları:** `ProAlan`, `Cekmece`, katlanır grup — parametreyle kapatılabilir.
- Migration'lar (D2) `Down()` tatbikatıyla gelir (P-23c'de kurulan kural).

---

## 8. Kurucudan istenenler (yalnız görsel tercih + müşteri erişimi)

### 8.1 Renk şeması — üçten biri (mockup S-0'da hazırlanır, 5199'da canlı önizleme)
| | Karakter | Not |
|---|---|---|
| **A — Altın Klasik** *(ekip önerisi)* | amber vurgu, beyaz zemin, koyu menü | bugünkü palet; tek dokunuş kenarlık `#7c8a9e`; sektöre gönderme |
| **B — Platin Mavi** | kurumsal, nötr mavi vurgu | kontrast payı en rahat; altın kimliği kaybolur |
| **C — Zümrüt Kasa** | koyu yeşil vurgu | başarı rengi maviye kayar (D6) |
Her üçünün karanlık karşılığı hazır (D — Gece Altını yapısı). **Müşteri "renkleri iyi değil" dediği için** öneri: üç mockup'ı **müşteriye** de göstermek (§ 8.3, soru 2).

### 8.2 Yazı ölçeği — Ö2 (16 px okunaklı) varsayılan, Ö1 (14 px yoğun) profesyonel modda. Onay yoksa böyle ilerlenir.

### 8.3 Müşteriye üç soru (kurucu sorar, cevap `sadelestirme/MUSTERI-SESI.md`)
1. "Pusula'da en çok neyi beğendin: **satış ekranını mı, menüyü mü, renkleri mi**?" (hangi dalganın önce sevk edileceğini belirler)
2. "Bizim renklerde seni ne rahatsız etti: **koyu menü mü, sarı düğme mi, çok renk mi**?" — yanında A/B/C mockup'ı.
3. "Bizim programı ilk açtığında **ne aradın da bulamadın**?" (S-3 sırasını belirler)
Ek: müşterinin Pusula'sını **10 dakika izleme izni** (ya da ekran videosu) — S-0'daki tık sayımı için en güvenilir kaynak.

### 8.4 Demo tarihi (S-6) — kurucu takvimi. Ekip "hazır" demeden randevu alınmaz (kurul tur 4 kuralı).

### 8.5 Kenar çubuğu zemini (D11) — açık mı koyu mu; mockup 1'de ikisi yan yana.

### 8.6 Bir tezgâhtar (S-5 ölçümü için) — kurucunun ya da müşterinin dükkânından, ilk satışı kronometreyle yapacak bir kişi; 30 dakika.

**Kurucudan istenmeyenler:** düzen/akış onayı, paket sıralaması, "başlayayım mı" — bunlar ekip oyuyla (§ 6.0) karara bağlanır ve raporlanır.

---

## 8A. Ürün adı (kurucu isteği, 23 Eylül: "Google gibi akılda kalan, sade ama özellikli")

Ölçüt: 2 hece, Türkçe, kuyumcunun kendi dilinden, sade ama "içi dolu" bir anlam; rakip adlarıyla (Pusula, Dezy, Gramino, Milyem, Kuyumex, Ariyen, ZarifSoft, CetaSoft, KuyumHub, Terminal Gold) çakışmasın; "Kuyum-" öneki YOK (herkes kullanıyor, ayırt etmiyor).

| Aday | Anlamı | Neden | Risk (23 Eylül web taraması) |
|---|---|---|---|
| **Mihenk** *(öneri)* | Mihenk taşı: kuyumcunun altının ayarını sürtüp ölçtüğü taş; "mihenk" = ölçüt, standart | Kuyumcunun bildiği, dükkânda duran gerçek bir nesne; "altını ölçen taş, dükkânı ölçen program"; 2 hece, kolay yazılır | Kuyumcu yazılımı olarak çakışma yok. Farklı sektörde küçük bir kişisel ürün var ("Mihenk", uygulama-fikri aracı, mihenk.omersaidakcin.com, 27 USD tek ödeme) — tescilli marka görünmüyor ama TÜRKPATENT'te bakılmalı. mihenk.com dolu/park (HTTP 530), **mihenk.app boş** (DNS kaydı yok, 23 Eylül). **TÜRKPATENT sorgusu + alan adı kurucuda** |
| Sade | Sade = basit; **sade altın** = taşsız som altın | Planın tezi adın kendisi; çift anlam | "Sade Software" adlı genel yazılım ajansı var (sadesoftware.com); genel kelime, tescili zayıf |
| Ayar | 14/22 ayar; "ayar" = düzen, ölçü | Her gün söylenen kelime | Genel kelime, tescili zayıf; anlamı yazılım için belirsiz |
| Dirhem / Okka | Eski ağırlık birimleri | Nostaljik, kısa | Yaşlı kullanıcıya tanıdık ama "eski" çağrışımı; kurumsal duruşla çelişebilir |
| Zerre | Altın tozu, en küçük parça | Kısa, güzel | **Zerre Elektronik A.Ş.** (Happy.com.tr işletmecisi, yazılım sınıfında) var → çakışma riski yüksek, elenir |

**KARAR (kurucu, 23 Eylül: "yaz"): ürün adı MİHENK.** Firma ByGatech. Slogan adayları: *"Kuyumcunun ölçüsü."* / *"Bir fiş, her şey."* Taslak ekranlara ve tuvale yazıldı. Kurucuda kalan: TÜRKPATENT başvurusu (sınıf 9/42) + alan adı (mihenk.app 23 Eylül'de boş). Koddaki "Kuyumcu ERP" marka metinleri (PageTitle, giriş ekranı, kılavuz, belge başlıkları) S-1'de tek sözlük anahtarıyla "Mihenk"e çevrilir; yasal belge şablonlarında (gider pusulası, fatura) firma unvanı değişmez, yalnız yazılım adı.

**Yazı tipi (kurucu, 23 Eylül: "yazı tipi değişik ve modern olsun"):** gövde **Manrope** (400/500/600/700; geniş, rakamları net, Türkçe tam), marka adı ve büyük başlıklar **Sora** (600/700). İkisi de OFL lisanslı, vendored dosya olarak `wwwroot/font/` altına (D9, Mehmet CSP onayı `font-src 'self'`). IBM Plex Sans taslaktan kaldırıldı; Inter/Roboto kullanılmaz.

## 9. Ölçüm defteri (S-0'da doldurulur, her dalga sonunda bir sütun eklenir)

| Ölçüt | S-0 (taban) | S-1 | S-2 | S-3 | S-4 | S-5 | S-6 | Hedef |
|---|---|---|---|---|---|---|---|---|
| Menü ilk bakış satır (basit/pro) | | | | | | | | ≤7 / ≤8 |
| POS ilk bakış kontrol (basit) | | | | | | | | ≤3 alan + ≤12 düğme |
| Nakit barkodlu satış tık | | | | | | | | ≤3 |
| Pusula nakit satış tık (videodan) | | — | — | — | — | — | — | referans |
| Satır eylemi maks (liste ekranları) | | | | | | | | ≤3 |
| Cariler satır yüksekliği (px) | | | | | | | | ≤64 |
| Ekran başına birincil düğme >1 olan ekran | | | | | | | | 0 |
| Düğme sistemi sayısı | | | | | | | | 1 |
| Ham font-size / padding (app.css) | | | | | | | | ≤10 / ≤10 |
| Kontrast AA başarısız çift | | | | | | | | 0 |
| Taşma (46 rota × 4 genişlik) | | | | | | | | 0 |
| Menü anlatım süresi | | — | — | — | — | | | <2 dk |
| Uçtan uca satış süresi | | — | | — | — | | | <4 dk |
| Yeni tezgâhtar ilk satış | | — | — | — | — | | | <30 dk |
| QA görsel / kullanıcı dostu | | | | | | | | ≥4,3 / ≥4,0 |
| Özellik kaybı | 0 (taban) | | | | | | | 0 |
| Test sayısı (0 başarısız) | | | | | | | | düşmez |
| Müşteri puanı (1–5, 3 soru) | | — | — | — | — | — | | ≥4 |

---

## 10. Kaynak belgeler (bu plan bunların üstüne yazıldı; tekrar keşfetme)

- `PUSULA-KIYAS-RAPORU-2026-09-21.md` — Pusula formülü, menü kıyası, 15 maddelik "ne yapmalı", fiyat.
- `dokumanlar/02-mimari/ARAYUZ-ARASTIRMASI-2026-09-16.md` — benchmark, UX-2 fazları (A2/B/C/D), 5 şablon fikri, kullanıcı dostu ilkeler, mühür.
- `dokumanlar/09-uygulama-kayitlari/FRONTEND-SARTNAME-2026-09-21.md` — 365 girdi/176 dropdown/418 düğme envanteri, tek tip kuralları, öncelik sırası, renk şemaları A/B/C/D ve Ö1/Ö2/Ö3 ölçekleri (kontrast hesaplı).
- `dokumanlar/09-uygulama-kayitlari/FRONTEND-KIMLIK-VE-MENU-2026-09-21.md` — düğme dili, menü/arama bulguları, görsel kimlik teşhisi (ham değer sayıları), önerilen kurumsal dil, `<Secici>` şartnamesi.
- `dokumanlar/09-uygulama-kayitlari/TASARIM-DENETIMI-2026-09-21.md` — 267 ölçümlük betik ve 8 kök neden (P-39'da kapandı).
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-3-B-uygulama-raporu.md` — basit mod POS'un mevcut hâli, tek POST yolu, yasal kapı deseni.
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-36c-uygulama-raporu.md` — select/girdi göçü sonrası sayılar; görsel tek tipleştirmenin yapılmadığı § 9.
- `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur7.md` — 41 sayfa bulgu tabanı.
- `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md` — 20 engel; sadelik dalgalarının hangi engelleri kapattığı S-6'da eşlenir.
- `NASIL-BAKARSIN-2026-09-17.md` — "30 ekranın 7'si her gün" haritası (S-3 sırasının kaynağı).
- Kod: `Layout/MenuTanimi.cs`, `Layout/KenarCubugu.razor`, `Layout/UstBar.razor`, `Servisler/BasitMenu.cs`, `Servisler/BasitPos.cs`, `Servisler/ArayuzModuServisi.cs`, `Pages/Islemler.razor`, `Pages/Panel.razor`, `Bilesenler/Form/*`, `Bilesenler/ProAlan.razor`, `Bilesenler/Cekmece.razor`, `wwwroot/css/belirtecler.css`, `wwwroot/css/app.css`.

- `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md` — Pusula ezberi (23 Eylül, bu oturum).
- `dokumanlar/02-mimari/sadelestirme/brosur/index.html` (+ `ekran/` görselleri; zip: `MIHENK-BROSUR-HTML-2026-09-28.zip`) ve `MIHENK-BROSUR-2026-09-28.pdf` — tanıtım broşürü (28 Eylül, canlı 5199 ogle-1210 ekranları, Platin Mavi, Manrope+Sora; iletişim alanları [TELEFON]/[E-POSTA] yer tutucu). Çekim sırasında görülen 9 tasarım kusuru 'son kuyumö' oturumuna 28.09 23:5x mesajıyla bildirildi (tema sayfa sayfa değişiyor, aktif grup kapalı/menü kesik, /raporlar Bugün kutucukları yüklenmiyor, emoji düğmeler, çift başlık, 'Görsel yok' sütunu, Bekleyen iş tekrar satırları).
- `dokumanlar/02-mimari/sadelestirme/PUSULA-MIHENK-KAFA-KAFAYA-2026-09-28.pdf` — 10 sayfalık yan yana kıyas (Pusula görselleri ↔ Mihenk taslağı ↔ 28 Eylül canlı ürün). Üretici betikler oturum scratchpad'inde (`kiyas/pdf-uret.py`, `render-mihenk.py`, `bugun-cdp.mjs`); canlı ekranlar 5199'dan parolasız vekille alındı. **Not (28 Eylül):** canlı ürün bu tarihte taslağın büyük kısmını uygulamış durumda (Mihenk adı, katlanır menü, Tüm ekranlar, VERDİM/ALDIM fişi, Peşin Müşteri, 8 düğme).

**Bu oturumda ölçülmeyenler (dürüst kayıt):** hiçbir derleme/test koşulmadı (kod hattı başka oturumda); 5199/5198'e dokunulmadı; QA turu 8 sonucu okunmadı (dosya henüz yok); Pusula videolarının kareleri tarayıcı bölmesinde siyah geldi, yalnız kare şeridi (320×180) ve altyazı parçaları okundu, tık sayısı sayılamadı; müşteri cümleleri kurucunun aktarımıdır, doğrudan görüşme yapılmadı. Kalanlar **S-0'ın işidir**.
