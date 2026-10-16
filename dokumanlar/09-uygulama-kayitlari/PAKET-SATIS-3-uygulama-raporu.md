# PAKET SATIS-3 — Tüm işlem türleri aynı sadelikte · uygulama raporu

Tarih: 27–28.09.2026 (son koşum 28.09 12:15–14:20) · Şerit t2 · test DB `kuyumcu_test_t2` · canlı API **5162** (`dotnet run --no-build`, Debug) · vekil **5163** · CDP 9905–9920 · **COMMIT YOK**.
Rol: Taşkın (backend: ürün araması barkod) + Ertuğ (frontend). Yetki: kurucu 27.09 "devam et koçum" + KURALLAR YETKİ satırı.

## 0. Özet (dürüst durum — 28.09 14:20, devam koşumu sonrası)
- **29.09 23:40 eki (§ 34, t21):** hasım doğrulayıcının KRİTİK bulgusu kapandı — kur yokken DÖVİZ ödeme satırı artık gövdeden sessizce DÜŞMEZ: Kaydet PASİF + "Döviz kuru alınamadı — kur gelince kaydedebilirsiniz.", satırda "Kuru yeniden al" (TEK `/kur/guncel`), fark "0 ₺ ✓" demez; kur gelince TL kurdan türetilir ve gövdeye girer (canlı SQL: DOVIZ · GBP 10,00 · 652,08 ₺ · Sterlin Kasa). ORTA MASAK: kural alınamadıysa kilit sebebi gerçek engeli söyler, BASİT bandı "alanlar açıldı" demez. DÜŞÜK VERESİYE koşulları gönderilen satırlara bakar. Ölçümde bulunan 3 ek kusur (kasasız döviz satırının belirsiz sebebi, birim değişince eski kasa, 375 döviz satırı sıkışması/rozet binmesi) düzeltildi. Yeni test 8 metot, mutasyon 10/10 + cmp, 78 tarayıcı ölçümü K = 0, tam süit 7365/7365, denetim TEMİZ, tenants 14 → 14. **Durum: BİTTİ, doğrulamaya hazır.**
- **29.09 13:45 eki (§ 30, t17):** "ekranda görünen ≠ gönderilen" HATA SINIFI tek kapıyla kapatıldı (orkestratör 12:10 + doğrulayıcının 2 KRİTİK'i): `GecersizAlanKapisi` (CascadingValue) — sayfadaki ve Fis/* + AltinBacagiPaneli içindeki HER Sayi/Para/Gram kutusu anlaşılamayan/belirsiz metinde (tuş tuş ara değerler dahil) kendini kapıya yazar, çağırana null bildirir, kur/teklif/otomatik kalan kutunun üstüne yazamaz; Kaydet (ve Diğer formlarının birincil düğmesi) PASİF, sebep kutunun etiketiyle; ödeme satırı geçersizken Fark "0 ₺ ✓" demez. T16D ×2 yeşil, yeni 18 test, mutasyon 17/17 kırmızı + cmp, canlıda 6 kutu × BASİT/PRO gerçek tuşlarla "1.500" → POST 0, "1,5" → gövde 1.5 (SQL TAKAS·KART 86.156,45; AVANS_ALINDI 1,50), 25 ölçüm K1..K13 = 0, tam süit 7170 · 3 kırmızı (ORTAK-1, ZO-ISLEM-A ×2; bu paketten 0), denetim TEMİZ, tenants 15 → 15. **Durum: BİTTİ, doğrulamaya hazır.**
- **29.09 07:05 eki (§ 28, t15):** istemdeki 4 hakem bulgusu kapatıldı — KRİTİK Takas: fark nakit haddini aşınca ödeme türü seçicisi çıkar, Kaydet nakitte kilitli + bantla aynı cümle, `odemeTuru` gövdede (canlı NAKİT 400 / KART 200, SQL TAKAS·KART); PRO 1024 birincil düğme sağda; MASAK Uyruk hizası ve fiyat rozeti bindirmesi 0; nakit notu/PRO "Aşıldı" yalnız ödemede nakit varken, iade/takas tek metin. Yeni test 6, mutasyon 7/7, tam süit 7037 · 2 kırmızı (ORTAK-1, ZO-ISLEM-A; bu paketten 0), denetim TEMİZ, 96 tarayıcı ölçümü, kiracı silindi. Chrome profilleri `/private/tmp/kuyumcu-serit/SATIS-3/t15/profil-9908|9909` silinemedi (hook) — kurucu silebilir. **Durum: BİTTİ, doğrulamaya hazır.**
- **29.09 03:25 eki (§ 25, t13):** tasarım hakeminin 2 YÜKSEK bulgusu kapatıldı: ürün listesi artık yalnız tıklama/yazma/↓/F2 ile açılır (mod geçişi ve sekme dönüşünde kapalı, K9 0); elle satırı "Ürünü seçip fiyatı elle yaz" adını taşır ve aranan metin ürün seçicisine taşınır. Ölçümde bulunan PRO sekme dönüşü "Aşıldı" artığı düzeltildi. SATIS3 261/261, mutasyon 6/6, tam süit 6928 · 2 kırmızı (ORTAK-1, ZO-BELGE; bu paketten 0), denetim TEMİZ, 70 tarayıcı ölçümü, tenants 14→14. **Durum: BİTTİ, doğrulamaya hazır.**
- **28.09 22:05 eki (§ 20, t8 — kod yazılmadı):** derleme 0/0, denetim TEMİZ, yarım mutasyon yok. Küme 1386 · 3 kırmızı: biri kararsız (tek başına yeşil), biri ZO-ISLEM-A, **biri BU PAKETİN** — hasım doğrulayıcı tur 7 testi `Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` (Islemler.Arama.cs:137 fiyatı çıkmayan yeni üründe eski fiyat kalıyor). **Durum: YARIM — tek satırlık düzeltme bekliyor.**
- **28.09 21:30 eki (§ 19, t7):** t6 doğrulamasının bulguları: fiyat hesaplanamayınca sunucunun sebebi yazılır ve kalem eklenince bant kalkar; PRO→BASİT geçişte nakit uyarı kutusu gizli kalır (had saklanır); FisTakas satır içi style → sınıf; ölçümde bulunan elle satırı ad kesilmesi (BASİT 1280 K10) düzeltildi. **Görünen üst bar başlığı "Satış" — koşul görünen ekranda KARŞILANMADI, hakem #11 AÇIK (Layout, alan dışı).** Testler 1445/1445, mutasyon 5/5, tam süit 6784 · 2 kırmızı (ZO-ISLEM-A, ORTAK-1; bu paketin 0), 387 tarayıcı ölçümü, test kiracısı silindi. **Durum: BİTTİ (alan dışı açıklarla).**
- **28.09 20:20 eki (§ 18, t6):** t5 doğrulamasının 5 bulgusu ve tasarım hakemi t3'ün 15 bulgusu güncel ağaçta önbelleksiz tarayıcıda yeniden ölçüldü: 12 kapandı, 1 gerekçeli fark (K-KUR-4), 2 alan dışı açık (üst bar başlığı Layout; stok/fiyat Api). Şube bazlı varsayılan kasa, kaynak rozeti modda korunur, karma para üstü katlı, sistem karanlığında seçili sekme, tevsik bandı çelişkisi (canlıda bulundu) düzeltildi. Testler 1384/1384, mutasyon 11/11, tam süit bu paketin yeni kırmızısı 0, denetim TEMİZ, test kiracısı silindi (70 tabloda 0 satır). **Durum: BİTTİ.**
- **28.09 16:10 eki (§ 17):** dördüncü doğrulamanın KALDI bulguları kapatıldı. Ad üç noktası kalktı (en çok iki satır, sözcük bölünmez), BASİT sütunları 1,25:1 dengelendi, 375 px iç dolgusu ve dar PRO kartta Tutar sütunu daraltıldı → 117 tarayıcı ölçümünde "22 Ayar Burma Bilezik" her genişlikte TEK satır, "14 Ayar Zirkon Taşlı Kolye" en çok iki satır, K1..K13 = 0 (yalnız bilinen açılır liste K9). Hurda formuna salt okunur "Has karşılığı · Tutar" satırı (sunucuyla aynı formül; canlıda istemci 61.753,97 = sunucu 61.753,97). Tam süit 6584/6582 (2 kırmızı ORTAK-1 ve EMANET-1'in), bu paketin yeni kırmızısı 0; mutasyon 8/8. Stok sütunu açık kalan (Api/Urun).
- **28.09 15:10 eki (§ 16):** üçüncü doğrulamanın KALDI bulguları kapatıldı (ALDIM tablosu Kasa+Tutar görünür, öneri fiyatı "—", stok gerekçeli fark, test sayıları ayrıldı: uygulayıcı 48 / doğrulayıcı 27, davet artığı 0). Tam süit 19 kırmızı — hepsi başka şeritlerin (EMANET-1, OLCEK-1, paylaşılan bin yarışı), bu paketin yeni kırmızısı 0. BASİT 1280/375 ad üç noktası OLCEK-1 14:47 değişikliğinden (A/B kanıtı § 16.3).
- **Durum: BİTTİ.** Paket kapısının 7 maddesi sağlandı (§ 15.9). Önceki koşumun YARIM kalemleri (Docker kesintisi, ölçülemeyen düzeltmeler, silinemeyen test kiracısı, `tur.mjs`/`tutarlilik.mjs`) ve SON DOĞRULAMA'nın (KALDI) 5 bulgusu kapatıldı; ölçüm sırasında bulunan 9 yeni kusur da giderildi (§ 15).
- **Yapıldı (27–28.09 gecesi, § 1–5):** KAYNAK 0 (tek arama kutusu + öneri listesi, ayrı "Elle ekle" yok), KAYNAK 2'nin 10 maddesi, KAYNAK 3, KAYNAK 5, tüm türlerde isteğe bağlılar "+ Ayrıntı ekle", Diğer formlarında ikincil bölümler katlı, tek kasada salt okunur satır.
- **Doğrulama bulguları (KALDI → kapandı):** ürün adı BASİT 1280 tek satır (kalem tablosunda ayar/gram ikinci satır) · K7 ±4 karma ödeme = 0 · boş odak listesi "En çok satılanlar" (sunucu sıralaması `GET /urunler?aktif=true&siralama=cok-satan`) · PRO→BASİT geçişi karma ödemeyi koruyor · test kiracısı silindi (tenants 22 → 21; kalanlar başka şeritlerin).
- **Bu koşumda ek düzeltmeler:** zaten odaktaki kutuya tıklama/↓ boş odak listesini açar · kalemsiz türlerde (Hurda/Değişim/İade) Kaydet kilidinin sebebi görünür · cariden dolan satıcı adı elle değişince "Cariden geldi" rozeti düşer · karma kilit sebebi "Ödeme tutarını girin" · 960 px BASİT tuval tek sütun (kap sorgusu), PRO dar tabloda Milyem gizlenir · 375 px ödeme satırı ve öneri adı · Kapora/Değişim açılışı 1,16/1,21 → 1,0 · Son 3 satışta eski günde saat yazılmaz · PRO ≤1100 px müşteri seçici kesilmez · denetim P-38 yorum satırı.
- **Testler:** SATIS3 kümesi **56/56** (40 metot); tam süit **6498 · 6497 geçti · 1 kırmızı** (`ZOISLEMAEkranTestleri.ZO28_banka…` — ZO-ISLEM-A, Banka; tek başına da kırmızı). **Bu paketin yeni kırmızısı: 0.**
- **Mutasyon:** bu koşumda 14 mutasyon (M-S3-1..4 güncel ağaçta yeniden + M-S3-11..20 yeni), **14/14 kırmızı, hepsi cmp BİREBİR**; önceki koşumun M-S3-5..10'u § 9'da.
- **Gerçek tarayıcı:** 44 durum×mod × 9 genişlik/tema = **396 ölçüm, kusur 0** (K9 yalnız açık öneri listesinin altındaki metinle — bağlı açılır katman), `tur.mjs` 24 kombinasyon + `tutarlilik.mjs` BASİT↔PRO ölçü farkı **0** (1440 ve 1280). **denetim.sh TEMİZ.**

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar ve gerekçe)
| İddia / istek | Kodda bulunan | Karar |
|---|---|---|
| "Hurda satıcı kimliği YALNIZ yasal eşik tetiklenince" (dogrula) | `GiderPusulasiKapisi.Dogrula` (Api/Modules/Hurda/GiderPusulasiKapisi.cs:104-155) kimliği **eşiksiz** zorunlu kılar; VUK md.234 gider pusulasında eşik yok | Satıcı kimliği açılışta görünür (yasal zorunlu → katlanmaz). Mutasyon "eşik altında zorunlu yap" anlamsız → yerine **adres aynası** ve **teknik not gizliliği** mutasyonları (M-S3-3, M-S3-9) |
| "Satıcı adresi (isteğe bağlı) *" çelişkisi | Sunucu `HurdaAyarlari.SaticiAdresiZorunlu` = **true** (GiderPusulasiKapisi.cs:78, :147); istemci aynası adresi hiç denetlemiyordu → hurda POST canlıda **400** | Etiket tek doğru: "Satıcı adresi" + yıldız yapılandırmadan (`/islemler/altin-bacagi/ayarlar` → `saticiAdresiZorunlu`); `HurdaGirisi.AdresZorunlu/AdresHata` aynası eklendi (bilinmiyorsa tutucu: zorunlu) |
| TAHSİLAT / ÖDEME sekmeleri form olsun | `FisTurleri.ProSekmeler`: Tahsilat → Müşteriler sayfasına bağlantı, Ödeme kilitli + sebep (FisIslemIzgarasi.razor:15-50) | Dokunulmadı: Müşteriler sayfası bu şeridin dosya alanında değil; ayrı paket gerekir (açık kalan) |
| "Ürün satırında stok" | `/urunler` yanıtı stok taşımıyor | Liste satırı: ad · ayar adı · gram · (sunucu biliyorsa) fiyat; stok gösterilmedi, "—" sütunu basılmaz (gürültü) |
| Liste KATMAN kalıbında | `katman.js` KATMAN şeridinin | Kutuya bağlı `position:absolute` liste (css/tuval-satis.css), ekrana sığar (`max-height:min(60vh,26rem)`), kutunun altında; KATMAN'ın ortak açıcısına bağlanması KATMAN şeridine not |

**Mevzuat (birincil kaynak):** 213 sayılı VUK md.234 — "Gider pusulası, … emtiayı satın alan ile satanın adlariyle soyadlarını … ve **adreslerini** ve tarihi ihtiva eder" (T.C. Çevre, Şehircilik ve İklim Değişikliği Bakanlığı'nın yayımladığı kanun metni: https://webdosya.csb.gov.tr/db/donersermaye/editordosya/vergi_usul_kanunu.pdf ; mevzuat.gov.tr ve gib.gov.tr sayfaları sertifika/boş gövde nedeniyle okunamadı). Hüküm koda sabit YAZILMADI; zorunluluk sunucu yapılandırmasından okunur (ADR-0005 ile uyumlu).

## 2. Yapılanlar (dosya dosya)
- **Sozlukler.Satis3.cs (yeni, TR+EN):** öneri listesi, "Bulunamadı", "Listede olmayan ürünü elle ekle", arama hatası/sınır, "Ödeme", "+ Taksit", fark anlamı (Eksik / Fazla / Ödeme tamam), "Satıcı adresi", "Alınan toplam".
- **UrunModulu.cs (Api):** `q` araması barkod önekini de kapsar (tek kutu okuyucu dizisinde daralır).
- **FisKalemGirisi.razor:** tek kutu `#islem-barkod` (id aynı) combobox: odakta boşken yüklü listenin son 8 ürünü (istek yok), yazdıkça 220 ms debounce ile TEK `/urunler?aktif=true&q=…&limit=21` (sayfa atar, bileşen istek atmaz), ↑/↓/Enter/Esc, fare `mousedown`; Enter seçim yoksa barkodu doğrudan okur ve bekleyen aramayı iptal eder; aranırken klavye "elle" satırına gitmez; "Bulunamadı" + son satır "Listede olmayan ürünü elle ekle" (elle satırını açar — özellik silinmedi); arama hatası ayrı hata bandı (UX-S28); sınır "daha fazlası var" (UX-S29). Ayrı "Elle ekle" `summary` düğmesi KALKTI (gizli, metinsiz `summary` — tarayıcı "Ayrıntılar" basmasın). Ayar ham kodu yerine ad.
- **Islemler.Arama.cs (yeni):** `UrunAra` (eski yanıt yeni aramanın üstüne yazılmaz), `OneriSec` (barkodla aynı kapı: teklif → kalem; fiyat yoksa elle satırı açılır + sebep), `IadeAdaylariYukleCiz` (canlıda bulunan hata: iade aday listesi yüklenince sayfa çizilmiyordu).
- **Islemler.razor (1395 satır ≤ 1400):** arama parametreleri; tüm türlerde reyon + açıklama "+ Ayrıntı ekle" içinde; FisNakitUyari toplamı YALNIZ nakit kısmı (Kart+Altın'da uyarı çıkmaz); iade kaynağı seçilince müşteri kaynaktan (UX-S24); ürün seçenek etiketi ad + ayar ADI; müşteri satırı PRO'da tüm türlerde sade (not ipucuda); Basit/VarsayilanKasaId parametreleri. Kaydet()/POST gövdeleri DEĞİŞMEDİ.
- **Islemler.Diger.cs:** Hurda seçilince adres zorunluluğu sunucu ayarından.
- **FisOdemeBacaklari.razor:** "Ödeme" / "+ Başka ödeme ekle"; kasa seçicisi yalnız birden çok uygun kasada (fişin kasası önerilir), tek kasada PRO'da kısa satır; POS 0 cihaz → hiçbir şey, 1 → otomatik, >1 → seçici; taksit "+ Taksit" ile; **otomatik kalan** (`FisHesap.OtoDengele`: elle yazılmayan son TL bacağı kalanı üstlenir); fark satırı anlamını yazar; tutar/araç zorunlu işaretli; BASİT'te "bacak" kelimesi yok.
- **FisModelleri.cs:** `FisBacagi.ElleYazildi` (gövdeye girmez), `OtoDengele`, `HurdaGirisi.AdresZorunlu/AdresHata`, `AyarSecenekleri` (değer kod, etiket ad), `UrunEtiketi`.
- **AltinBacagiPaneli.razor (Bilesenler/, KAYNAK 2'nin konusu):** BASİT'te teknik metinler `hidden` (PRO'da görünür), has karşılığı tek yerde, kaynak rozeti etiket satırında (`#altin-fiyat-kaynak` aynı), fiyat kutusu Ayar/Gram ile aynı ızgara hücresi, TCKN `inputmode=numeric`, adres etiketi tek doğru.
- **FisHurdaAlis.razor / FisTakas.razor:** çift `<label>` ve iç içe `.alan` kalktı (subgrid hizası), rozetler `KaynakRozeti` ile etiket satırında (id'ler aynı), hatalar `Hata/HataKimligi` ile (id'ler aynı), büyük harf satır içi stil kalktı, ayar seçenekleri "18 Ayar", zorunlular işaretli.
- **FisOdemeKasa.razor:** tek TL kasasında kilitli seçici + iki açıklama yerine tek salt okunur satır (`#islem-kasa-tek`).
- **FisVerdimAldim.razor:** ≥2 ödemede VERDİM üstte, ALDIM altta tam genişlik (KAYNAK 2 md.10).
- **FisKalemTablosu.razor:** PRO'da barkod ürün adının altında ikinci satır, ad tek satır; gram/has kesilmez.
- **FisDigerRezerv/Avans/Konsinye/Degisim/Evrak.razor:** isteğe bağlı alanlar (son gün, açıklama) "+ Ayrıntı ekle" altında; geçmiş/ikincil bölümler (liste, mahsup, iade, dönen evrak) kapalı katlanır (`details.katlanir` — içerik DOM'da, id'ler aynı).
- **Form/Metin.razor:** `GirisModu` (inputmode) parametresi — ek, geriye uyumlu.
- **css/tuval-satis.css:** yalnız belirteç; öneri listesi, rozet, kasa satırı, karma tek sütun, 112 px kutu kök nedeni (`app.css .bacak-satiri .girdi {flex:1 1 7rem}` iç içe dikey alana iniyordu), karo kalınlığı, PRO tablo dengesi, 375'te seçici sarması, kapalı details içeriği.

## 3. Tür bazında zorunlu küme (kanıt) · sunucunun bildiği · gizlenen · açılış (1440, SONRA3)
| Tür | Zorunlu (kanıt) | Sunucunun/sistemin bildiği (sorulmaz) | Gizlenen (nasıl) | Açılış öğesi BASİT / PRO · boy |
|---|---|---|---|---|
| SATIŞ | kalem ≥1, ödeme ≥1, kur (SatisModulu.cs:442-462 `SatisFisiDto`) | tarih, TL, satıcı=oturum, kasa (tek), fiyat=teklif | tarih/para/satıcı/liste özet (PRO) · reyon/açıklama/iskonto "+ Ayrıntı ekle" · ikinci ödeme "+ Başka ödeme ekle" · taksit "+ Taksit" · elle kalem listenin son satırı | 6 / 8 · 1,0 |
| ALIŞ | kalem ≥1, kur, cari (AlisFisiDto :201; `CariGerekli` Islemler.razor:756) | kasa (tek), fiyat=teklif | reyon/açıklama "+ Ayrıntı ekle" | BASİT'te tür yok (BasitPos) / 6 · 1,0 |
| HURDA ALIŞ | ayar, gram, has fiyatı, kur, cari (HurdaAlisDto :232) + satıcı tipi/ad/TCKN/adres (GiderPusulasiKapisi.cs:104-150, VUK 234 — eşiksiz) | has fiyatı=kur (rozet), kasa | teknik not (BASİT gizli), açıklama | 7 / 8 · 1,0 |
| TAKAS | eski ayar/gram, yeni ürün/adet, kur (TakasDto :538) | eski has fiyatı=kur, yeni fiyat=teklif | açıklama/reyon | 9 / 9 · 1,0 |
| SATIŞ/ALIŞ İADESİ | kaynak hareket, adet, kur (IadeDto :501) | tutar ve müşteri kaynaktan (SATIS-3: müşteri dolar) | adet/tutar kaynak seçilmeden çizilmez; açıklama | 4 / 5 · 1,0 (ALIŞ İADESİ yalnız PRO) |
| İŞÇİLİK | cari, tutar (IscilikDto :603) | — | açıklama | yalnız PRO: 4 · 1,0 |
| TRANSFER | kaynak/hedef şube, ürün, adet, kur (TransferDto :378) | — | açıklama | yalnız PRO: 8 · 1,0 |
| Diğer · ürün ayır | ürün, müşteri, adet | — | son gün, açıklama "+ Ayrıntı ekle"; liste kapalı | PRO 7 · 1,0 |
| Diğer · kapora | müşteri, tutar, ödeme şekli | kasa (tek) | açıklama; mahsup ve geri ödeme bölümleri kapalı | PRO · **1,0** (SONRA4: Tutar · Ödeme şekli · Kasa tek satır) |
| Diğer · konsinye | müşteri, ürün, adet | — | açıklama; liste kapalı | PRO 7 · 1,0 |
| Diğer · değişim | kaynak satış, adetler | fark = hesap | açıklama | PRO · **1,0** (SONRA4: üç satırlık ızgara) |
| Diğer · siparişten satış / çek-senet | liste seçimi | — | dönen evrak bölümü kapalı | PRO 1–2 · 1,0 (boş durum) |
Not: BASİT'te ALIŞ, ALIŞ İADESİ, İŞÇİLİK, TRANSFER ve Diğer menüsü sunulmuyor (BasitPos kararı, bu paket değiştirmedi) — BASİT ölçümünde bu durumlar SATIŞ ekranını ölçtü, "geçersiz/kapsam dışı" sayıldı.

## 4. Kurucunun 25.09 ~12:00 ekranındaki 10 madde — ÖNCE / SONRA
Görüntüler: `ham/TUVAL-KARSILASTIRMA/SATIS-3-KURUCU-1200-once-sonra-BASIT-karanlik.png`, `…-PRO-aydinlik.png` (önce = BICIM-2 görüntüsü, ağaçta SATIS-2 hâli).
| # | Önce | Sonra (ölçüm) |
|---|---|---|
| 1 | "Elle girildi" koca mavi hap | rozet etiket satırında, içerik boyunda (Read ile görüldü) |
| 2 | Has gram fiyatı kutusu Ayar/Gram'dan yukarıda | aynı ızgara hücresi; SONRA3 hiza/K7 = 0 (hurda, takas, altın paneli) |
| 3 | Satıcı ad/TCKN ~95 px | kök neden `app.css .bacak-satiri .girdi {flex:1 1 7rem}` → 112 px ölçüldü; düzeltildi: kontrol yüksekliği SONRA3'te tüm durumlarda {44}; TCKN `inputmode=numeric` |
| 4 | "Satıcı adresi (isteğe bağlı) *" | "Satıcı adresi *" (zorunluluk sunucu ayarından; bileşen testi iki yönü sınar) |
| 5 | Kart 113.363,16 + Eski altın 7.500 → Fark 7.500 | otomatik kalan: Kart 67.782,04 + altın 29.770 = 97.552,04 → "Ödeme tamam"; fark varsa "Eksik …" / "Fazla … — ödeme tutarını azaltın" |
| 6 | POS cihazı/taksit/"cihaz tanımlı değil" notu | 0 cihaz → hiçbiri; 1 → otomatik; taksit "+ Taksit" |
| 7 | teknik metinler + BÜYÜK HARF vurgu | BASİT'te `hidden` (PRO'da görünür); takas başlığı büyük harf değil |
| 8 | "7,5000 has" iki kez | yalnız "Has karşılığı" kutusunda |
| 9 | sağ sütunda sürekli nakit uyarısı | yalnız nakit tutar eşiğe yaklaşınca (Kart+Altın'da görünmez — Read ile görüldü) |
| 10 | sol yarı boş, form sağda sıkışık | ≥2 ödemede VERDİM üst / ALDIM alt tam genişlik; ürün adı tek satır |

## 5. Tuval karşılaştırması (yalnız SATIŞ'ın tuvali var)
`SATIS-3-SATIS-1440-BASIT-tuval-vs-biz.png`, `SATIS-3-SATIS-1440-PRO-tuval-vs-biz.png` (Read ile görüldü).
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Arama kutusu | "Barkodu okut ya da ürün adı yaz" | aynı metin + yazdıkça liste | AYNI + GEREKÇELİ FARK (KAYNAK 0) |
| Tarih / Para | açık kutular | BASİT'te yok, PRO'da tek satır özet | GEREKÇELİ FARK (kurucu kuralı 11:35) |
| ALDIM karma | iki sütun, kompakt satırlar | ≥2 ödemede alt alta tam genişlik | GEREKÇELİ FARK (KAYNAK 2 md.10: altın paneli dar sütuna sığmıyor) |
| Son 3 satış / nakit uyarısı | sürekli | katlanır / yalnız ilgili durumda | GEREKÇELİ FARK (SATIS-2 + kurucu 11:35) |
| PRO barkod sütunu | ayrı sütun | ürün adının altında | GEREKÇELİ FARK (KAYNAK 5: ad tek satır, sayılar kesilmesin) |
| PRO sağ sütun | yasal kapılar + müşteri + kısayollar | "Yasal kapılar uygun" tek satır; müşteri kartı gerçek müşteride | GEREKÇELİ FARK (SATIS-2 kuralı) |

## 6. Gerçek tarayıcı ölçümü (başsız Chrome + ham CDP, tarayıcı bölmesi KULLANILMADI)
Kiracı PAKETSATIS-3-TEST: 8 ürün (13 haneli barkod, "14 Ayar Hasır Bilezik Uzun Adlı Model Özel Seri"), müşteri "Anadolu Holding A.Ş.", fiyat listesi "Perakende Varsayilan" (kiracının varsayılanı), 21 satış fişi (4'ü çok kalemli), 1 gider pusulası.
Betik: `/private/tmp/kuyumcu-serit/SATIS-3/senaryo.mjs` (sikisma.js K1..K11 + ekstra.js K12/K13 + ek: katman hariç K9, satır hizası ±3, kontrol yükseklikleri, açılış öğeleri, bacak farkı). 27 durum × {BASİT,PRO} × {aydınlık 1440/1280/375, karanlık 1440/375} + {1024,960,768} alt kümesi = **324 ölçüm (SONRA3, hata satırı 0)**. Ham: `senaryo-SONRA3/olcum.jsonl`, görüntüler `senaryo-SONRA3/foto/` (54'ü `ham/TUVAL-KARSILASTIRMA/SATIS-3-*.png`).

### 6.1 Durum × ölçüm (kusurlu ölçüm sayısı / toplam ölçüm)
| durum | mod | ölçüm sayısı | K1 | K3 | K5 | K7 | K9 (katman hariç) | K9 ham | K10 | K11 | K12 | K13 | yatay | en yüksek kontrol px |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| acilis-ALIS | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-ALIS | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-ALIS_IADE | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-ALIS_IADE | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-HURDA_ALIS | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-HURDA_ALIS | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-ISCILIK | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-ISCILIK | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-SATIS | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-SATIS | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-SATIS_IADE | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-SATIS_IADE | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-TAKAS | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-TAKAS | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-TRANSFER | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| acilis-TRANSFER | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-bos-odak | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-bos-odak | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-bulunamadi | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-bulunamadi | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-liste | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 8 | 0 | 0 | 0 | 0 | 0 | 44 |
| arama-liste | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 8 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-AVANS | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-AVANS | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-DEGISIM | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-DEGISIM | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-EVRAK | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-EVRAK | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| diger-KONSINYE | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-KONSINYE | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-REZERV | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-REZERV | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-SIPARIS | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| diger-SIPARIS | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| hurda-dolu | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| hurda-dolu | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| iade-kaynak-secili | BASIT | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| iade-kaynak-secili | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-altin-elle-fiyat | BASIT | 5 | 0 | 0 | 0 | 3 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-altin-elle-fiyat | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-kart-altin | BASIT | 8 | 0 | 0 | 0 | 6 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-kart-altin | PRO | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-nakit-kart-altin | BASIT | 5 | 0 | 0 | 0 | 3 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| karma-nakit-kart-altin | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| satis-havale | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-havale | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-kart | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-kart | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-nakit | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-nakit | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-veresiye | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| satis-veresiye | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 44 |
| takas-iki-tarafli | BASIT | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |
| takas-iki-tarafli | PRO | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 44 |

"K9 ham" = sikisma.js'in ölçüsü; açık öneri listesi (bağlı açılır katman) altındaki metinle örtüştüğü için tasarım gereği >0 — "katman hariç" sütunu listeyi (ve açık seçici listesini) dışarıda sayar ve **tüm durumlarda 0**.

### 6.2 Kalan kusurlar (SONRA3 — son yayın öncesi) — ⟶ KAPANDI, bkz. § 15.4 (SONRA4)
- **K7 ±4 px** (tolerans ±3): BASİT karma durumlarında (<1400 px — 1280/1024/960/768/375) ödeme satırında "Ödeme aracı" ile "Tutar *" etiket yüksekliği farkı. Düzeltme: araç seçicisi de zorunlu işaretlendi (iki etikette yıldız) + satır `align-items:flex-end` — **derlendi, testler yeşil, canlı ölçülemedi (DB kesintisi)**.
- **K10** 375 px: uzun seçili değer ("Nihai tüketici (vatandaş — stopaj yok)", "Anadolu Holding A.Ş.", iade kaynağı) üç noktayla. Düzeltme: ≤600 px'te `.islem-sayfa` seçicisi büyür, metin sarar — **ölçülemedi**.
- Kapora (PRO) açılış boyu 1,17: kapalı `details` içeriği ölçüye düşüyordu; `display:none` kuralı eklendi — **ölçülemedi**.

### 6.3 Görüntüler (Read ile bakıldı)
arama-bos-odak-BASIT-1440, arama-bulunamadi-BASIT-1440, arama-liste-PRO-1440 (DENEME), karma-kart-altin-BASIT-1440 / -375, hurda-dolu-PRO-1440 (önce/sonra), acilis-HURDA_ALIS-BASIT-1440, iade-kaynak-secili-PRO-1440 (hata bulundu → düzeltildi), satis-veresiye-PRO-1440 (tablo kesikliği bulundu → düzeltildi), diger-SIPARIS-PRO-1440, diger-AVANS-PRO-1440, Tezgah.png, SATIS-3-SATIS-1440-PRO-tuval-vs-biz.png.

### 6.4 Canlıda ölçülemeyen son düzeltmeler (DB kesintisi) — ⟶ 28.09 ÖLÇÜLDÜ, bkz. § 15.4
araç seçicisi Zorunlu + `align-items:flex-end` (K7) · ≤600 px seçici sarması (K10) · kapalı details `display:none` · "Alınan toplam". Kod derlendi, ilgili 1707 test yeşil.

## 7. Canlı doğrulama (API 5162, kiracı 01M3J7APJHQFKCQYNNBBJ90KSE)
| İstek | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| GET /urunler?aktif=true&q=bil&limit=21 | 200, "bil" içerenler | 200 — 22 Ayar Burma Bilezik, 925 Gümüş Bileklik, 14 Ayar Hasır Bilezik … | yanıt okundu |
| GET /urunler?q=<13 haneli barkod> | 200, tek ürün (yeni barkod araması) | 200 — 22 Ayar Burma Bilezik | yanıt okundu |
| GET /urunler?q=zzqx | 200, [] | 200, [] | — |
| POST /islemler/satis-fisi KART 5.006,44 + NAKİT 2.000 = 7.006,44 | 200, iki ödeme kalemi | 200, fisId 01M3JC4R3D3N6T1B5WV8FBTF22 | SQL `odeme_kalemleri`: KART 5006.44 · NAKIT 2000.00 |
| POST /islemler/satis-fisi bacaklar 1.000 fazla | 400 | 400 "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır…" | istemci otomatik kalanı bu 400'e düşmemek içindir |
| POST /islemler/hurda-alis ADRESSİZ | 400 (VUK 234) | 400 "Satıcı adresi zorunludur (VUK md.234 — gider pusulası)" | istemci aynası artık aynı kilidi ekranda söylüyor |
| POST /islemler/hurda-alis adresli | 200 + gider pusulası | 200, GP-2026-000001 | SQL `gider_pusulalari.satici_adres` = "Kapalıçarşı Cad. No:12 Fatih" |
| GET /islemler/altin-bacagi/ayarlar | 200, saticiAdresiZorunlu | 200, `saticiAdresiZorunlu: true` | — |

## 8. Testler
- Yeni: `Kuyumcu.Tests/SATIS3EkranTestleri.cs` (24 metot, 27 koşum), `SATIS3BilesenTestleri.cs` (7 metot, 9 koşum) → **36/36**.
- Kurucu kuralına göre güncellenen (davranış bilinçli değişti): P10PosBacagiEkranTestleri (POS 0/1 cihaz, "+ Taksit"), DovizBacagiEkranTestleri, UX4bEkranTestleri, KarmaOdemeEkranTestleri, S22TezgahFisiTestleri, IadeTestleri, L1bAlisFisiEkranTestleri, PosBarkodTestleri, PosTeklifEkranTestleri, S25YazdirParaUstuTestleri (tek kasada seçici yok), S24EskiAltinEkranTestleri (otomatik kalan — fark kilidi elle yazılan tutarla sınanıyor), TS2TuvalProTestleri (barkod adın altında), S21FisBilesenTestleri (id envanteri: `HataKimligi`/`KaynakRozeti` ile üretilen id'ler kabul; adres aynası).
- Filtreli küme (Islem/Fis/S21/S22/S24/S25/TS1/TS2/SATIS2/SATIS3/P10/B4/Karma/Doviz/PosBarkod/UX4b/L1/CumaS/P16/S1i/OlcekTek/Hurda/Takas/Iade/P39/Urun…): **1707/1707**.
- Tam süit (`trx-pSATIS-3/satis3-tam.trx`, 01:14): **6441 · 6435 · 6 kırmızı** — `IadeEkranTestleri.Kaynak_secilince_KALAN…`, `B4AltinBacagiFisEkranTestleri.AltinBacagi_Karma_Listesinde_Var…`, `SATIS3EkranTestleri.Barkod_dizisi…`, `S22TezgahFisiTestleri.FARK_kapanmadan…`, `UX4bEkranTestleri.Karma_fis_govdesinde…` → yük altında `WaitFor` zaman aşımı, tek başına **5/5 yeşil**; `UX4cEkranTestleri.M10_Liste_sunucu_sinirina…` (Emanet.razor — EMANET-1) tek başına da kırmızı → **sahibi EMANET-1**. Önceki tam koşumda (00:42) başka şeritlerin `P39…K5` (PosTahsilatlar, ZO-ISLEM-A) ve `ZOISLEMASube…` kırmızıları vardı; son koşumda yoklar.
- **Bu paketin yeni kırmızısı: 0** (yükte zaman aşımına düşen kendi testim `Barkod_dizisi…` tek başına yeşil; bekleme 5 sn'ye çıkarıldı).

## 9. Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3/<kod>.orj` → tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-1 | `DigerAyrintiAcik => true \|\| …` (gizli açıklama/reyon açılışta açık) | SATIS3.Aciklama_TUM_turlerde… (×4), SATIS2.BASIT_Aciklama_KATLI…, SATIS2.PRO_Ayrinti_ekle_KATLI… | BİREBİR |
| M-S3-2 | `bacaklar.Take(1).Select(` (eklenen ödeme gövdeden düşer) | SATIS3.Otomatik_KALAN…iki_bacak_GOVDEYE_girer | BİREBİR |
| M-S3-3 | hurda adres aynası `SaticiTamam`'dan düşer | S21FisSafTestleri.HurdaGirisi_satici_kimligi…, SATIS3.Hurda_ADRESSIZ_kaydet_KILITLI… | BİREBİR |
| M-S3-4 | iade `=> true` (kaynak seçimi atlanır) | SATIS3.Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
| M-S3-5 | debounce kaldırıldı | SATIS3.Arama_harf_harf…TEK_istek…, SATIS3.Barkod_dizisi…IPTAL | BİREBİR |
| M-S3-6 | ayrı "Elle ekle" `summary` geri | SATIS3.Ayri_ELLE_EKLE_dugmesi_DOMda_YOK (×2) | BİREBİR |
| M-S3-7 | otomatik kalan kapalı | SATIS3Bilesen.OtoDengele…, SATIS3.Otomatik_KALAN… | BİREBİR |
| M-S3-8 | POS seçici 0 cihazda da çizilir | P10PosBacagi.CIHAZ_YOKSA…, P10PosBacagi.TEK_CIHAZ…, SATIS3.POS_cihazi_YOKSA…, SATIS3.TEK_POS… | BİREBİR |
| M-S3-9 | BASİT'te hurda teknik notu görünür | SATIS3.Hurda_BASIT_teknik_not_GIZLI… | BİREBİR |
| M-S3-10 | aranırken klavye elle satırını açar | SATIS3.ARANIRKEN_ok_ve_Enter… | BİREBİR |
Geçersiz sayılan koşumlar (yinelendi): M-S3-10'un ilk iki koşumu — biri paralel derleme kilidiyle takıldı (süreç durduruldu, dosya cmp BİREBİR geri alındı), biri paralel şeridin eşzamanlı derlemesi yüzünden bayat dll ile "0 kırmızı" verdi; üçüncü temiz koşumda kırmızı. Son mutasyondan sonra temiz derleme **0 Hata**.

## 10. Denetim · derleme · migration
- `dotnet build Kuyumcu.sln` → **0 Hata** (tek uyarı başka şeridin: EtiketEkranTestleri.cs(145) xUnit1031).
- `bash dotnet/denetim.sh` (00:4x, test kiracısı dururken) → **DENETİM TEMİZ, rc=0** (S2-3 FIS_BILESENI_ISTEK 0 ✓, SOZLUK_SAYI 0 ✓, OLCEK-1 MOD_OLCU_BILDIRIMI 0 ✓). Kiracı silindikten sonra yeniden koşulamadı (DB kesintisi).
- Migration: **gerekmedi** (şema değişikliği yok).

## 11. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Yeni uç YOK (yalnız `GET /urunler` araması barkod önekini de kapsar). Oluşturulabilen kayıtların düzenleme/değişmezlik yolları DEĞİŞMEDİ (Diğer formlarındaki Düzenle/Kaldır düğmeleri ve DegismezIpucu'lar yerinde, yalnız katlanır bölüm içinde). Her yeni düğme istek atar ya da yerel eylemdir: "+ Taksit" (taksit seçicisini açar), öneri satırları (teklif isteği + kalem), "Listede olmayan ürünü elle ekle" (elle satırını açar).

## 12. Dokunulan sayfalar (TEK DOKUNUŞ)
Pages/Islemler.razor (+ Islemler.Arama.cs yeni, Islemler.Diger.cs). Alan dışı: `Bilesenler/AltinBacagiPaneli.razor` (KAYNAK 2'nin doğrudan konusu, hiçbir şeridin listesinde değil), `Bilesenler/Form/Metin.razor` (ek parametre, geriye uyumlu), `Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (arama).

## 13. Açık kalanlar (sahibiyle — 28.09 14:20 güncel)
1. **ZO-ISLEM-A:** `ZOISLEMAEkranTestleri.ZO28_banka_hesap_sahibi_acicisi_IBAN_ve_DOVIZden_SONRA_gelir` tam süitte ve tek başına kırmızı ("yeni: IBAN açıcıdan önce olmalı") — Banka ekranı, bu paketin dosyası değil.
2. **OLCEK-1:** tam süitin ilk koşumunda (13:3x) `OlcekTekTestleri.OL14…`, `OL21_v8…` kırmızıydı — `css/olcek.css` o sırada (13:32) değişiyordu; tek başına koşumda ve ikinci tam koşumda yeşil. Bilgi.
3. **KATMAN:** `tur.mjs` 375/768 px'te üst çubuk `#ust-kayit-ara` ("Müşteri, ürün ya da fiş bul") katmanı SERBEST-YÜZEN — GlobalArama, KATMAN şeridinin. Öneri listesinin KATMAN ortak açıcısına bağlanması da KATMAN'a (liste kutuya bağlı `position:absolute`; K9 ölçümü yalnız bu listenin altındaki metin).
4. **KATMAN (menü dosyası):** Diğer panelinin başlığındaki açıklama cümlesi ("Müşteriden ön ödeme alın; satışta düşülür.") `FisDigerIslemler.razor`'da — kural "açıklama satırı yok, (i) ipucu" ama dosya KATMAN'ın; dokunulmadı.
5. **TAHSİLAT/ÖDEME formları** Müşteriler sayfasında — bu şeridin alanı dışında (ayrı paket).
6. **ISTEK-1 notu:** kutu açılışta OdakJeton ile odaklanır; tarayıcı bu odağı olay olarak ilettiğinde (ölçüldü: `tur.mjs` 375 px mobil öykünme) boş odak listesi ve TEK `GET /urunler?aktif=true&siralama=cok-satan&limit=8` isteği açılışta gelir; masaüstü başsız ölçümde odak olayı gelmedi, liste tıklama/↓ ile açıldı (§ 15.2). Kurucu isteği "oraya liste gelsin" ile uyumlu; ISTEK-1 sahibinin bilgisine.
7. **Artık dosyalar (0 bayt, önceki koşulardan):** `Pages/Islemler.Sade.cs.tmp`, `Pages/Islemler.razor.mutorj` (+ başka şeritlerin `Fiyatlandirma/Sayim/Urunler/VadeTakibi.razor.mutorj`) — hook silmeyi engelliyor → kurucu silmeli. Bu koşum proje ağacına geçici dosya bırakmadı (yedekler `/private/tmp/kuyumcu-serit/`).
8. **Tuval bölge tablosu** yalnız SATIŞ için (diğer türlerin tuvali yok) — § 5 ve § 15.6.

## 14. Kurucuya / mali müşavire notlar
- VUK md.234 satıcı adresini gider pusulasının unsuru sayıyor (alıntı § 1); sistem varsayılanı "zorunlu" ve yapılandırmadan kapatılabilir — mali müşavir teyidi önerilir.
- Satıcı kimliği hurda alışta tutar eşiği olmadan istenir (sunucu kuralı, değişmedi).

## 15. 28.09 devam koşumu (12:15–14:20) — SON DOĞRULAMA bulguları + SONRA4 ölçümü
Başlangıç: ağaçta 06:59–11:05 arasında yazılmış ama raporlanmamış düzeltmeler vardı (cok-satan sıralaması, `Islemler.Odeme.cs`, `SATIS3CokSatanTestleri.cs`, tuval-satis.css K7/ad kuralları). `/private/tmp` yeniden başlatmada silinmişti → yarım mutasyon yedeği yoktu; ağaçta `.orj/.bak` yok, ilgili testler ilk koşumda **52/52 yeşil**, derleme 0 hata. Ağaçtaki iş KORUNDU, üstüne kuruldu. İşlemci kurucunun isteğiyle izlendi (20 sn aralıkla `top`; bu şeridin yükü tek başsız Chrome + `nice -n 10` derleme/test; tepe yükler başka şeritlerin test koşularıydı).

### 15.1 SON DOĞRULAMA (KALDI) bulguları — kapanış kanıtı
| # | Bulgu | Kök neden / yapılan | Kanıt (canlı, 5162/5163, başsız Chrome) |
|---|---|---|---|
| KRİTİK | BASİT 1280 tek ödemede ürün adı 3 satır | ağaçtaki kural (Ayar/Gram sütunu gizli, `data-ayrinti` ikinci satır) doğrulandı; 960'ta yeni kesilme bulundu → (a) kap sorgusu | `td.fis-tuval-ad` 1280 açık/koyu: w 181/171 px, **satır 1**, ikinci satır "22 · 15,20 g"; 1440 w 218, 375 w 199–160, hepsi 1 satır; SONRA4'te K10b 0 |
| ORTA | Test kiracısı silinmemiş | `kiraci-temizle.sh 01M3J7APJHQFKCQYNNBBJ90KSE --uygula` | 4866 satır + tenants 1 satır silindi; `tenants` 22 → **21**; hareketler/urunler/kullanicilar/gider_pusulalari/odeme_kalemleri'nde bu kiracıya ait **0** satır |
| ORTA | K7 ±4 BASİT karma | ağaçtaki düzeltme (ızgara satırı `auto auto`, etiket tek ölçü) doğrulandı; 375'te yeni ±52 bulundu → (b) | SONRA4 karma-kart-altın / nakit-kart-altın BASİT+PRO × 9 = **K7 0**, bacak satırı alt kenar farkı ≥768'de 0 |
| ORTA | "Son eklenen" başlığı / en çok satılan yok | sunucu `siralama=cok-satan` (son 90 gün SATIS sayısı, eşitlikte kod), istemci ilk odakta BİR KEZ ister | Canlı `GET /urunler?aktif=true&siralama=cok-satan&limit=8` → 200: GRM-24-001, ZYN-CYR-22, YZK-14-003, GMS-BLK-925, BLZ-22-015…; SQL satış sayıları 9 · 7 · 5 · 4 · 0… **aynı sıra**; `siralama=yanlis` → 400; başlık "En çok satılanlar" |
| ORTA | PRO→BASİT karma ödeme sessizce Nakit'e dönüyor | `TekOdemeyiBasitteKoru` + karma korunur | CDP: PRO'da Kart 44.604,28 + Eski altın 10 g (50.380 ₺, satıcı Mehmet Yılmaz) → Basit → **aynı iki satır, aynı tutarlar, "Ödeme tamam"** → PRO'ya dönüş de aynı |

### 15.2 KAYNAK 0 kabul (CDP, harf harf, BASİT 1440)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Kutuya tıkla (boş) | liste + 1 istek | 8 öneri "En çok satılanlar", istek `…siralama=cok-satan&limit=8` × **1** |
| "bil" yaz | ≥1 öneri, 1 arama isteği (debounce) | 3 öneri + "Listede olmayan ürünü elle ekle"; istek `…q=bil&limit=21` × **1** |
| ↓ + Enter | kalem eklenir | kalem 1 ("22 Ayar Burma Bilezik") |
| 13 haneli barkod dizisi + Enter | doğrudan eklenir, arama isteği 0 | kalem 2 ("925 Gümüş Bileklik"), q= isteği **0** |
| "zzqx" | Bulunamadı + elle ekle satırı | "Bulunamadı" + tek satır "Listede olmayan ürünü elle ekle" |
| Ayrı "Elle ekle" düğmesi | DOM'da 0 | **0** (tüm adımlarda) |
**Bu koşumda bulunan kusur:** kutu açılışta OdakJeton ile ZATEN odakta olduğundan tıklama yeni focus olayı doğurmuyor, liste hiç açılmıyordu (ölçüldü: başsız Chrome, `document.activeElement = islem-barkod`, tık sonrası istek 0). Düzeltme: `FisKalemGirisi` `onclick` (odakta değilse aç) + boş kutuda ↓ listeyi açar. Görüntüler: `SATIS-3-arama-bos-odak-BASIT-1440.png`, `…-liste-bil-…`, `…-bulunamadi-…` (Read ile görüldü).

### 15.3 Bu koşumda bulunan ve düzeltilen ek kusurlar
| Kod | Kusur (nerede ölçüldü) | Düzeltme (dosya) |
|---|---|---|
| E1 | Zaten odaktaki kutu tıklanınca boş odak listesi açılmıyor | `Bilesenler/Fis/FisKalemGirisi.razor` `Tiklandi` + ↓ |
| E2 | Hurda/Değişim/İade'de gram yazılsa da Kaydet kilidinin sebebi gizli (yalnız `FisDolu` iken görünüyordu) — UX-S30 | `Pages/Islemler.Odeme.cs` `KilitGorunur`, `Islemler.razor` (tek ifade) |
| E3 | Cariden dolan satıcı adı elle değişince "Cariden geldi" rozeti kalıyor (görüntü: hurda-dolu) — UX-S21 | `FisHurdaAlis.razor` `AdElleYazildi` |
| E4 | Karma kilit sebebi "Ödeme bacakları" (başlık, eksik koşul değil; "bacak" jargonu) | `Islemler.Odeme.cs` `KarmaKilitSebebi` + `Sozlukler.Satis3.cs` `OdemeTutariGirin` (TR+EN) |
| E5 | BASİT 960: iki sütunlu tuvalde ad "22 Ayar Burma Bil…" (K10b) | css (a): `#islem-verdim-aldim` kap sorgusu ≤660 px → tek sütun (1280'de kart 672 px, iki sütun kalır) |
| E6 | 375 karma: Sil düğmesi tutarın alt satırına düşüyor (K7 kutuAlt ±52) | css (b): dar ekranda araç tam genişlik, tutar + ₺ + Sil tek satır |
| E7 | 375 öneri listesinde uzun ad üç nokta (K10b) | css (c): ≤600 px ad sarar |
| E8 | PRO 960/1024 kalem tablosunda ad kesik (K10b) | css (d): tablo kabı ≤720 px → Milyem sütunu gizli (Ayar yerinde, geniş kartta görünür) |
| E9 | Kapora 1,16 / Değişim 1,21 ekran (PRO 1440) — `form-izgara` sınıfının HİÇ kuralı yoktu | css (e) ızgara + `FisDigerAvans` (Kasa aynı satırda) + `FisDigerDegisim` (üç satır) — id'ler, sıra, gövde aynı |
| E10 | `tur.mjs`: BASİT 1280 Son 3 satış "27.09 23:02 · Anadol…" (K10); PRO 960 Son satışlar adı iki satır (K3); PRO ≤1100 "Peşin Müşt…" | `FisSonSatislar.Zaman` eski günde yalnız "27.09"; css (f) tablo adı tek satır, ≤1100 px özet alt satıra |
| E11 | denetim.sh P-38 `PASIF_KAYIT_SECILEBILIR = 1` — `Islemler.Arama.cs` YORUM satırındaki `/urunler?siralama=` | yorum `?aktif=true&siralama=` |

### 15.4 Gerçek tarayıcı — SONRA4 senaryo matrisi (son derleme, başsız Chrome, tarayıcı bölmesi KULLANILMADI)
Betik: `/private/tmp/kuyumcu-serit/SATIS-3/s4/senaryo4.mjs` (tur/sikisma.js K1..K11 + tur/ekstra.js K12/K13 + kontrol yükseklikleri + ödeme satırı hizası + bacak tutarları + fark metni + açılış ekran boyu). Her durum × mod: {aydınlık 1440, 1280, 1024, 960, 768, 375} + {karanlık 1440, 1280, 375} = 9 ölçüm; **396 ölçüm, kusur 0, yatay kaydırma 0, kontrol yüksekliği tek ölçü 44 px** (Sipariş/Evrak boş durumunda kontrol yok). K9 yalnız `arama-*` durumlarında ve her örnekte açık öneri listesinin (`#islem-barkod-*`, `.fis-arama-*`) altındaki metin — bağlı açılır katman, tasarım gereği. Ham: `/private/tmp/kuyumcu-serit/SATIS-3/olcum-SONRA4-SON.jsonl`.
Kiracı verisi: 8 ürün (13 haneli barkodlar, "14 Ayar Hasır Bilezik Uzun Adlı Model Özel Seri"), müşteri "Anadolu Holding A.Ş.", fiyat listesi "Perakende Varsayilan", 23 satış fişi (4'ü çok kalemli), 2 gider pusulası. BASİT'te ALIŞ / ALIŞ İADESİ / İŞÇİLİK / TRANSFER / Diğer sunulmuyor (BasitPos) — o satırlar SATIŞ açılışını ölçtü.

| durum | mod | ölçüm | K1 | K3 | K3 | K5 | K7 | K9 | K9b | K10 | K10b | K11 | K12 | K13 | yatay | kontrol px | bacak hiza maks (≥768) | açılış boyu 1440 aydınlık | not |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| acilis-SATIS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-HURDA_ALIS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-SATIS_IADE | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-TAKAS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-ALIS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-ALIS_IADE | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-ISCILIK | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-TRANSFER | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| arama-bos-odak | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| arama-liste | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| arama-bulunamadi | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| satis-nakit | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| satis-kart | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:KART |
| satis-havale | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:HAVALE |
| karma-kart-altin | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 2.15 | ok ok:KART ok:ALTIN ok ok:K22 altin-dolu |
| karma-nakit-kart-altin | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 2.54 | ok ok:NAKIT ok ok:KART ok ok:ALTIN ok:K2 |
| hurda-dolu | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1.08 | ok ok ok:01M3J7AY2SFAQF5P7ZFYV7B9N5 ok:N |
| iade-kaynak-secili | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:01M3KPSNPR4YT9V55Y5345PGFN |
| takas-iki-tarafli | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok ok ok:01M3J7AYGPKAM0XRKZXRNYX0AC |
| diger-REZERV | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| diger-AVANS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| diger-KONSINYE | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| diger-DEGISIM | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| diger-SIPARIS | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [] | 0 | 1 | ok |
| diger-EVRAK | PRO | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [] | 0 | 1 | ok |
| acilis-HURDA_ALIS | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-SATIS_IADE | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-TAKAS | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| acilis-ALIS | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | yok:islem-tur-ALIS |
| acilis-ALIS_IADE | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | yok:islem-tur-ALIS_IADE |
| acilis-ISCILIK | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | yok:islem-tur-ISCILIK |
| acilis-TRANSFER | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | yok:islem-tur-TRANSFER |
| arama-bos-odak | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| arama-liste | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| arama-bulunamadi | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| satis-kart | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:KART |
| satis-havale | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:HAVALE |
| karma-nakit-kart-altin | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 2.01 | ok ok:NAKIT ok ok:KART ok ok:ALTIN ok:K2 |
| iade-kaynak-secili | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok:01M3KPSNPR4YT9V55Y5345PGFN |
| takas-iki-tarafli | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok ok ok:01M3J7AYGPKAM0XRKZXRNYX0AC |
| hurda-dolu | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok ok ok:01M3J7AY2SFAQF5P7ZFYV7B9N5 ok:N |
| acilis-SATIS | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | ok |
| satis-nakit | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1 | None |
| karma-kart-altin | BASIT | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | [44] | 0 | 1.76 | ok ok:KART ok:ALTIN ok ok:K22 altin-dolu |

Otomatik kalan (bacak toplamı = fiş toplamı): karma-kart-altın Kart 44.379,98 + altın 10 g × 5.500 (50.380 ₺) → "Ödeme tamam"; nakit-kart-altın Nakit 20.000 (elle) + Kart 49.540,45 (kalan) + altın 5 g (25.190 ₺) → "Ödeme tamam".
Açılış ekran boyu (1440 aydınlık): tüm açılış ve Diğer durumları **1,0**; >1 olanlar dolu durumlar (karma 1,76–2,54: eski altın paneli + satıcı kimliği; PRO hurda-dolu 1,08: satıcı kimliği) — açılış değil.

### 15.5 tur.mjs /islemler + tutarlilik.mjs (son derleme)
- `tur.mjs` {BASİT, PRO} × {aydınlık, karanlık} × {1440…375}: 24 sayfa, 202 katman denemesi (106 açıldı), hata 0, geçersiz 0. Kalan bulgular ve sahibi: SERBEST-YÜZEN `#ust-kayit-ara` 375/768 (**KATMAN**, GlobalArama); K9 375'te açık boş odak listesi (katman, § 13.6); "K4 tren" PRO 1280/960/375 — `tur.mjs` her genişlikte önceki genişlikte tıkladığı açıcıların (+ Başka ödeme ekle, özet ayrıntısı, Son satışlar) açık kaldığı BİRİKMİŞ durumu ölçüyor (görüntü `tur3/foto/islemler__PRO_aydinlik_1280.png` Read ile görüldü); SONRA4 senaryosunda PRO açılış 1280/960 = 1,0. E10 düzeltmesinden sonra aydınlık yeniden koşuldu: K3/K10 **0**.
- `tutarlilik.mjs /islemler` 1440 ve 1280: **BASİT↔PRO ölçü farkı 0** (düğme sınıf grubu 5).

### 15.6 Tuval karşılaştırması (güncel ağaç)
`ham/TUVAL-KARSILASTIRMA/SATIS-3-SATIS-1440-BASIT-tuval-vs-biz.png` (Tezgah.png ↔ biz) ve `…-PRO-tuval-vs-biz.png` (Profesyonel.png ↔ biz) yeniden üretildi, Read ile görüldü. Bölge farkları § 5'teki tabloyla aynı; ek: PRO sağ panelde "Yasal kapılar" eşik yokken tek satır "Yasal kapılar uygun" (kurucu kuralı 11:35), Kısayollar tek satır ipucu; Müşteri kartı yalnız gerçek müşteri seçilince.
Senaryo görüntüleri (1440 aydınlık, 375 karanlık, 960 aydınlık, mod geçişi, arama): **140 dosya** `ham/TUVAL-KARSILASTIRMA/SATIS-3-senaryo-*.png`. Read ile bakılanlar: arama-bos-odak BASİT 1440 (koyu), satis-nakit BASİT 1280/960, karma-kart-altin BASİT 1280 (tam sayfa), karma-nakit-kart-altin BASİT 375, hurda-dolu BASİT 1440 (önce/sonra), acilis-HURDA_ALIS BASİT 1440, diger-AVANS / diger-DEGISIM PRO 1440 (önce/sonra), iade-kaynak-secili PRO 1440, takas-iki-tarafli BASİT 1440, satis-nakit PRO 960, tur PRO 960/1280, tur BASİT 1280 Son 3 satış.

### 15.7 Canlı API doğrulaması (5162, kiracı 01M3J7APJHQFKCQYNNBBJ90KSE — yanıtlar okundu)
| İstek | Beklenen | Gerçek |
|---|---|---|
| GET /urunler?aktif=true&q=bil&limit=21 | 200, "bil" içerenler | 200 — BLZ-22-015 Burma Bilezik, GMS-BLK-925 Gümüş Bileklik, HSR-14-004 Hasır Bilezik |
| GET /urunler?aktif=true&q=zzqx&limit=21 | 200 [] | 200 [] |
| GET /urunler?aktif=true&q=<13 haneli barkod> | 200 tek ürün | 200 — BLZ-22-015 |
| GET /urunler?aktif=true&siralama=cok-satan&limit=8 | 200, satış sayısına göre | 200 — GRM-24-001 · ZYN-CYR-22 · YZK-14-003 · GMS-BLK-925 · BLZ-22-015 · HSR-14-004 · KNY-22-010 · ZNC-22-008 (SQL: 9 · 7 · 5 · 4 · 0 · 0 · 0 · 0, eşitlikte kod) |
| GET /urunler?aktif=true&siralama=yanlis | 400, sessizce koda düşmez | 400 "Geçersiz sıralama: 'yanlis'. Geçerli: cok-satan" |
| POST /islemler/satis-fisi KART 3.000 + NAKİT 2.000 = 5.000 | 200, iki ödeme kalemi | 200 fisId 01M3KPSNPMXPM0RQJWPFQGWAMS; SQL `odeme_kalemleri`: KART 3000.00 · NAKIT 2000.00 |
| aynı, bacaklar 1.000 fazla | 400 | 400 "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır: … (fark 1.000,00 TL)" |
| POST /islemler/hurda-alis adressiz | 400 (VUK 234) | 400 "Hurda alış: Satıcı adresi zorunludur (VUK md.234 — gider pusulası)" |
| POST /islemler/hurda-alis adresli | 200 + gider pusulası | 200 GP-2026-000002, net 12.595; SQL `gider_pusulalari.satici_adres` dolu |
| GET /islemler/altin-bacagi/ayarlar | 200 | 200, `saticiAdresiZorunlu: true` |
Oturum parolasız: test kullanıcısına (yalnız bu test kiracısı) yeni rastgele parola bcrypt ile yazıldı, fikstür chmod 600 (değer hiçbir çıktıya basılmadı, iş sonunda dosya boşaltıldı), `inceleme-vekili.py` 5163. API ve vekil durduruldu: `lsof` 5162/5163 **boş**; bu şeridin Chrome süreçleri 0.

### 15.8 Testler · mutasyonlar · derleme · denetim
- **Yeni testler bu koşumda:** `SATIS3EkranTestleri.Zaten_odaktaki_kutuya_TIKLAMA_ve_OK_ASAGI_bos_odak_listesini_ACAR`, `Hurda_gram_yazilinca_KILIT_SEBEBI_GORUNUR_bos_acilista_GIZLI` (BASİT, PRO), `Hurda_cariden_dolan_ad_ELLE_degisince_CARIDEN_GELDI_rozeti_DUSER`, `SATIS3BilesenTestleri.Son_satis_ZAMANI_bugun_saat_eski_gun_YALNIZ_tarih`; `Baska_odeme_ekle_ILK_tikta_IKI_satir…` kilit sebebi iddiasıyla güçlendirildi. SATIS3 toplam **40 metot / 56 koşum, 56/56**.
- İlgili kümeler: Islem/Fis/S21/S22/S24/S25/TS1/TS2/SATIS2/SATIS3/P10/B4/Karma/Doviz/PosBarkod/UX4b/L1/Hurda/Takas/Iade/Urun/Katlanir **1452/1454** (2 kırmızı başkasının: OL16 → olcek/belirtecler, ZO28 → Banka); S26/Diğer/Avans/Değişim **130/130**; SATIS3+TS1+SATIS2 **174/174**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pSATIS-3/satis3-tam-28b.trx`): **6498 · 6497 · 1 kırmızı** (ZO28 — ZO-ISLEM-A). Devir notundaki EMANET-1 `UX4cEkranTestleri.M10…` artık yeşil. **Fark (bu paketin ürünü) = 0.**
- **Mutasyonlar** (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3/<kod>.orj` → TEK eşleşme → filtreli test → geri kopya → `cmp`):

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-1 (yeniden) | `DigerAyrintiAcik => true \|\| …` — gizli alan açılışta görünür | SATIS3.Aciklama_TUM_turlerde_ARTI_AYRINTI_altinda_KAPALI (ALIS, HURDA_ALIS, SATIS_IADE, TAKAS), SATIS2.BASIT_Aciklama_KATLI…, SATIS2.PRO_Ayrinti_ekle_KATLI… | BİREBİR |
| M-S3-2 (yeniden) | `odemeler = bacaklar.Take(1).Select(` — eklenen ödeme gövdeden düşer | SATIS3.Otomatik_KALAN…GOVDEYE_girer, SATIS3.PRO_dan_BASIT_e_geciste_KARMA…, SATIS3.Tek_kasada_bacak_basina_KASA_SECICI_YOK… | BİREBİR |
| M-S3-3 (yeniden) | hurda adres aynası `SaticiTamam`'dan düşer (eşik altında zorunluluk mutasyonu yasal olarak anlamsız — § 1) | S21FisSafTestleri.HurdaGirisi_satici_kimligi…, SATIS3.Hurda_ADRESSIZ_kaydet_KILITLI… | BİREBİR |
| M-S3-4 (yeniden) | `IadeGonderilebilir => true \|\| …` — iade kaynak seçimi atlanır | SATIS3.Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
| M-S3-11 | sunucu `OrderByDescending(satış sayısı)` → `OrderBy` | SATIS3CokSatan.Cok_satan_siralamasi…, SATIS3CokSatan.Cok_satan_LIMIT… | BİREBİR |
| M-S3-12 | `if (enCokSatanIstendi) return;` silindi (her odakta istek) | SATIS3.Bos_odakta_EN_COK_SATANLAR…TEK_istekle…, SATIS3.Zaten_odaktaki_kutuya_TIKLAMA… | BİREBİR |
| M-S3-13 | `Tiklandi` etkisiz | SATIS3.Zaten_odaktaki_kutuya_TIKLAMA… | BİREBİR |
| M-S3-14 | `TekOdemeyiBasitteKoru()` çağrısı kapalı | SATIS3.PRO_tek_KART_odeme_BASIT_e_geciste_Nakite_DONMEZ | BİREBİR |
| M-S3-15 | BASİT'e geçişte `karmaOdeme=false; bacaklar.Clear()` | SATIS3.PRO_dan_BASIT_e_geciste_KARMA_odeme_KORUNUR… | BİREBİR |
| M-S3-16 | "+ Başka ödeme ekle" ikinci satırı eklemez | SATIS3 10 test (Baska_odeme_ekle… ×2, Otomatik_KALAN…, Iki_bacak…, POS ×3, PRO_dan_BASIT…, Odeme_dili…, Tek_kasada…) | BİREBİR |
| M-S3-17 | kilit sebebi yine yalnız `FisDolu` | SATIS3.Hurda_gram_yazilinca_KILIT_SEBEBI_GORUNUR… (BASİT, PRO) | BİREBİR |
| M-S3-18 | `AdElleYazildi` rozeti düşürmez | SATIS3.Hurda_cariden_dolan_ad_ELLE_degisince… | BİREBİR |
| M-S3-19 | "Ödeme tutarını girin" dalı silindi | SATIS3.Baska_odeme_ekle_ILK_tikta… (BASİT, PRO) | BİREBİR |
| M-S3-20 | eski günde yine "dd.MM HH:mm" | SATIS3Bilesen.Son_satis_ZAMANI… | BİREBİR |
Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln` **0 Hata, 0 Uyarı** (tam derlemede tek uyarı başka şeridin: `EtiketEkranTestleri.cs(145)` xUnit1031).
- **denetim.sh:** ilk koşum `✗ P-38 PASIF_KAYIT_SECILEBILIR 1` (bu paketin yorum satırı — E11) → düzeltildi → **DENETİM TEMİZ, rc=0** (son koşum 14:0x): S2-3 FIS_BILESENI_ISTEK 0 ✓, SOZLUK_SAYI 0 ✓, OLCEK-1 MOD_OLCU_BILDIRIMI 0 ✓, P-38 PASIF 0 ✓, Pages ModalPencere 48 (tavan 55, artış yok).
- **Migration:** gerekmedi (şema değişikliği yok; `siralama=cok-satan` mevcut `hareketler` üzerinde sorgu).

### 15.9 Paket kapısı (7 madde)
| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | dotnet build 0 hata | ✓ | build10: 0 Hata 0 Uyarı |
| 2 | Bu paketin testleri 0 başarısız | ✓ | SATIS3 56/56; ilgili kümeler § 15.8 |
| 3 | Sayaçlar denetim.sh'ta ✓ | ✓ | DENETİM TEMİZ |
| 4 | Mutasyon kanıtı | ✓ | 14/14 kırmızı, cmp BİREBİR (+ önceki M-S3-5..10) |
| 5 | Canlı doğrulama | ✓ | § 15.2, 15.4, 15.5, 15.7 |
| 6 | Test kiracısı silindi | ✓ | tenants 22 → 21, kiracı satırı 0 |
| 7 | Yeni kırmızı yok | ✓ | tam süit tek kırmızı ZO28 (ZO-ISLEM-A) |

### 15.10 Değişen dosyalar (bu koşum)
`dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` · `FisHurdaAlis.razor` · `FisDigerAvans.razor` · `FisDigerDegisim.razor` · `FisSonSatislar.razor` · `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (tek ifade ×2; 1386 satır ≤ 1400) · `Pages/Islemler.Odeme.cs` · `Pages/Islemler.Arama.cs` (yorum) · `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` (yalnız belirteç, satır içi stil yok) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (TR+EN 1 anahtar) · `dotnet/Kuyumcu.Tests/SATIS3EkranTestleri.cs` · `SATIS3BilesenTestleri.cs`. Paketin tüm ağaç izi (`git status --porcelain | grep -v -E "/(bin|obj)/"`, paket dosyaları): `M UrunModulu.cs`, `M Pages/Islemler.razor`, `?? Sozlukler.Satis3.cs`, `?? Bilesenler/AltinBacagiPaneli.razor`, `?? Bilesenler/Fis/`, `?? Pages/Islemler.Arama.cs / .Diger.cs / .Odeme.cs / .Sade.cs`, `?? css/tuval-satis.css`, `?? css/katlanir.css`, `?? Kuyumcu.Tests/` (tüm dizin izlenmiyor). **Dokunulan sayfa (TEK DOKUNUŞ):** yalnız `Pages/Islemler.razor` (+ partial'ları) — § 12 ile aynı; Diğer formları (`FisDigerAvans/Degisim`) şeridin dosya alanında, `FisDigerIslemler.razor` (KATMAN) açılmadı.

### 15.11 DoD sözleşmesi (bu koşum)
Yeni varlık/kayıt türü YOK, yeni uç YOK (`GET /urunler` yalnız `siralama=cok-satan` parametresi — salt okuma, [ModulYetkisi] mevcut uçla aynı; geçersiz değer 400). Yeni düğme YOK; tıklama/↓ mevcut kutunun davranışı. Kaydet()/POST gövdeleri, yasal kapılar, id'ler DEĞİŞMEDİ (Diğer formlarında yalnız sarmalayıcı `div.form-izgara` sırası).

## 16. 28.09 üçüncü doğrulama turu (14:35–15:10) — KALDI bulgularının kapanışı
Kurucu talimatı (bu koşum): işlemci %90'ı geçmesin → her ağır adımdan önce `top` ile yük ölçüldü (%70 altı beklendi, `bekle.sh`), derleme `nice -n 10 -m:3`, testler `MaxCpuCount=2–3`, tek başsız Chrome. Ölçülen tepe toplam yük %95'e başka şeritlerin eşzamanlı test/derlemeleriyle çıktı (ps: EMANET-1 t31 test süreci, Docker VM); bu şeridin payı ~%18–19 (tek test süreci, 10 çekirdekte 185 %CPU).

### 16.1 Bulgular → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| KRİTİK | BASİT ALDIM tablosunda Kasa ve Tutar gizli (`.fis-tuval-tablo :nth-child(2/3)` ALDIM'a da uyuyordu) | `tuval-satis.css` kural `#islem-kalem-tablo` ile sınırlandı (ad tek satır kuralı da) | Canlı 5162/5163, başsız Chrome: `#islem-aldim-tablo` th/td **6/6 `table-cell`**: "Ödeme · Ayrıntı · Tutar / Nakit · Ana Kasa · 95.089,99 ₺" — BASİT 1440/1280/1024/960/768/375 açık + 1440/1280/375 koyu, Nakit/Kart/Havale (27 ölçüm). Görüntü: `t3/foto/SATIS-3-senaryo-satis-nakit-BASIT-aydinlik-1280.png`, `…-karanlik-375.png` (Read ile görüldü: ALDIM satırı tuvaldeki gibi üç hücre) |
| ORTA | Öneri satırında fiyat bilinmiyorsa "—" yok; stok yok | `FisKalemGirisi.razor`: fiyat sütunu her satırda, bilinmeyen **"—"**. **Stok: GEREKÇELİ FARK** — `GET /urunler` `List<Data.Urun>` döner, `Urun` varlığında stok alanı yok (stok `stoklar` tablosunda, şube bazlı; `Entities.cs:500`, `UrunModulu.cs:516`). Eklemek 20+ ekranın tükettiği `/urunler` sözleşmesini değiştirir (Api/Urun bu şeridin dosya alanı dışında) → ayrı küçük backend paketi önerisi | bUnit `SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE_ile_TEK_istek_ve_liste_gorunur` ("—" beklenir); canlı "bil" listesi fiyat hücreleri `—,—` (tohum kiracısında `satisFiyati` boş, fiyat listeden gelir) |
| DÜŞÜK | Test sayısı yeniden üretilemedi | Ayrı sayım: **uygulayıcı 48 vaka / 41 metot** (`SATIS3CokSatan` 3 + `SATIS3Ekran` 30 + `SATIS3Bilesen` 8; filtre `FullyQualifiedName~SATIS3&FullyQualifiedName!~Dogrulayici` → 48/48) · **doğrulayıcı 27 vaka** (`Dogrulayici/SATIS3SinirTestleri.cs`, 20 metot) · birlikte 75/75 | `t3/t-uyg.log`, `t3/t-satis3.log` |
| DÜŞÜK | `kiraci_davetleri`'nde 2 PAKETSATIS-3 satırı | `DELETE … LIKE 'PAKETSATIS-3%'` → 2 satır; bu koşumun daveti de silindi (1) → kalan **0** | SQL `SELECT COUNT(*)` = 0 |

### 16.2 Mutasyon (bu tur)
| Kod | Değişiklik | Kırmızı test | Geri alma |
|---|---|---|---|
| M-S3-21 | `FisKalemGirisi.razor` bilinmeyen fiyat `"—"` → `""` | `SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE_ile_TEK_istek_ve_liste_gorunur` | yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3/M-S3-21.orj` → cmp BİREBİR; sonra temiz derleme 0 hata 0 uyarı |
ALDIM css kuralı bUnit'le sınanamaz (css) → tarayıcı ölçümüne `aldim` denetimi eklendi (`t3/senaryo.mjs`: `#islem-aldim-tablo :is(th,td)` display + metin).

### 16.3 Gerçek tarayıcı (başsız Chrome, CDP 9906/9907, tarayıcı bölmesi KULLANILMADI) — 81 ölçüm
Durumlar BASİT: satis-nakit, satis-kart, satis-havale, karma-kart-altin, arama-liste, acilis-SATIS · PRO: satis-nakit, satis-kart, karma-kart-altin, arama-liste, acilis-SATIS; her biri 1440/1280/1024/960/768/375 açık + 1440/1280/375 koyu. Yatay kaydırma **0** (81/81). K1..K13: K9 yalnız açık öneri listesinin altı (bilinen, § 13.3 — KATMAN). **K10b = 1** şu ölçümlerde: BASİT 1280 ve 375 (tek ödeme, iki tema) `td.fis-tuval-ad "22 Ayar Burma Bilezik"` tek satır ama 6 px kısa → üç nokta; PRO 1024/960 `span.fis-pro-ad-metin`.
**Kök neden bu paket DEĞİL — A/B kanıtı (`t3/ab.mjs`, BASİT 1280):** şimdiki durum w 169 / scrollWidth 175 / padding-left 16 px; ALDIM kuralının ESKİ hali geri enjekte edilince **aynı** (169/175) → ALDIM düzeltmesinin etkisi yok; yalnız `olcek.css` (10b) `td.fis-tuval-ad { padding-left: 1rem }` kaldırılınca scrollWidth 169 = clientWidth (kesilme yok). `olcek.css` 28.09 **14:47**'de OLCEK-1 şeridince değiştirildi (§ 10b "fiş KALEM satırı tek ölçü"); önceki SONRA4 ölçümü (14:09) bu durumlarda K10b 0'dı. Başka şeridin bilerek koyduğu ölçüyü ezmedim → açık kalan, sahibi OLCEK-1.

### 16.4 Tam süit ve denetim (bu tur, güncel ağaç)
- `dotnet build Kuyumcu.sln` → **0 hata 0 uyarı** (son mutasyondan sonra).
- Tam süit (`trx-pSATIS-3-t3`): **6539 · 6520 geçti · 19 kırmızı**. Hiçbiri bu paketin dosyası değil; tek başına yeniden koşumda da kırmızı olanlar ve sahipleri:
  - `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir…` (47 → 48 sayfa) ve `OrtakGorunumSaflik.O8…` (15 → 16): yeni sayfa `Pages/EmanetHamKap.razor` (14:54) → **EMANET-1**.
  - `OlcekTekTestleri.OL6…`, `OL13…`: `olcek.css` `#cekmece-cari` kuralları (14:47) → **OLCEK-1**.
  - `P31DestekPaketiTestleri.*` (11), `SurumUcuTestleri.*` (4): `Kuyumcu.Tests/bin/Debug/net8.0/Kuyumcu.Api.deps.json` ve `NUlid` bulunamadı — koşum sırasında paylaşılan `bin` başka şeridin derlemesiyle yeniden yazıldı (ortam yarışı; kod kusuru değil) → orkestratör tam süitinde yeniden ölçülmeli.
  - **Bu paketin yeni kırmızısı: 0** (SATIS3 75/75; İşlem/Fiş kümeleri tam süitte yeşil).
- `bash denetim.sh` → **BAŞARISIZ, 3 ✗ (288 ✓)**, hiçbiri bu şeridin: Pages sayısı 48 (tavan 47) ve P-37c `acik=1` → EMANET-1 `EmanetHamKap.razor`; `.mutorj artığı Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj` (0 bayt, 21.09 21:40 — eski koşu artığı, hook silmeyi engelliyor → kurucu silmeli). Bu şeridin sayaçları (ModalPencere 48/55, P-38 ×3, P-16, P-36) ✓.

### 16.5 Canlı doğrulama ve temizlik
- API 5162 (`Sistem__Anahtari` bu koşum için üretildi, dosyası sonra boşaltıldı), vekil 5163; kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3KX7X9RHT5GYD0Y1KTTM57D) `demo-tohumla.js --prova-kiraci` ile ürünün kendi uçlarından: 6 cari + "Anadolu Holding A.Ş.", 12 ürün, 12 satış (1 ek satış tevsik kapısında 400 döndü — beklenen yasal kapı: 95.049,58 TL nakit > 30.000 TL).
- Durdurma: API ve vekil PID'leri `kill`; `lsof` 5162/5163 **boş**; kendi başsız Chrome'um kapatıldı.
- Silme: `kiraci-temizle.sh 01M3KX7X9RHT5GYD0Y1KTTM57D --uygula` → rc 0; SQL: tenants'ta PAKETSATIS-3 **0**, urunler/hareketler'de bu kiracı **0**; davet 0. `tenants` 17 (başlangıç) → **19**: artış bu koşumda açılan başka şeritlerin kiracıları (`PAKETEMANET-1-TEST T31`, `PAKETZO-ISLEM-A-TEST Dogrulayici`), bu şeridinki 0.

### 16.6 Değişen dosyalar (bu tur)
`dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` (ALDIM kapsamı) · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` (fiyat "—") · `dotnet/Kuyumcu.Tests/SATIS3EkranTestleri.cs` (beklenti "—"). Not: `Pages/Islemler.razor` 14:31 damgalı — bu turda dokunulmadı (14:20 koşumunun sonu ya da başka şerit; kaynağı doğrulanamadı).

### 16.7 Açık kalanlar (bu tur eki, sahibiyle)
1. **OLCEK-1:** `olcek.css` (10b) 1rem ad dolgusu BASİT 1280/375 ve PRO 960/1024'te ürün adını üç noktaya düşürüyor (A/B § 16.3). OLCEK-1 kendi ölçüsünü /islemler dolu fişte K10b ile yeniden ölçmeli ya da ad hücresi için genişlik payı bırakmalı.
2. **EMANET-1:** yeni sayfa `EmanetHamKap.razor` → P37c/O8 testleri + denetim sayfa tavanı.
3. **Orkestratör:** P31/SurumUcu kırmızıları paylaşılan `bin` yarışı — sakin ağaçta tam süit.
4. **Kurucu:** `Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj`, `Pages/Islemler.Sade.cs.tmp`, `Pages/Islemler.razor.mutorj` (0 bayt artıklar) silinmeli.
5. **Ayrı paket önerisi:** öneri listesinde stok — `/urunler?…&stok=true` gibi ek alan (Api/Urun sahibi).

## 17. 28.09 dördüncü doğrulama turu (15:25–16:10) — KALDI bulgularının kapanışı
Kurucu talimatı (bu koşum): işlemci %90'ı geçmesin → ağır her adımdan önce `bekle.sh` (%70 altı beklenir), derleme `nice -n 10 -m:3`, testler `MaxCpuCount=2`, tek başsız Chrome. Ölçülen tepe (%92, 15:44) başka şeritlerin süreçlerinden (Docker VM `Virtualization.xpc` %93, başka şeridin VBCSCompiler/testhost'u); o sırada bu şeridin yalnız boşta API'si vardı → sonraki adım `bekle.sh` ile ertelendi.

### 17.1 Bulgular → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt (canlı 5162/5163, başsız Chrome, tarayıcı bölmesi KULLANILMADI) |
|---|---|---|---|
| KRİTİK | BASİT 1280/375'te ad kesik; "14 Ayar Zirkon Taşlı Kolye" dolgusuz da sığmıyor; sütunlar dengelenmemiş | (1) Üç nokta KALKTI: ad `FisKalemTablosu.razor`'da kendi `span.fis-tuval-ad-metin`'inde, **en çok iki satır** (`line-clamp:2`, sözcük bölünmez), ayar·gram yine altta. (2) **Sütunlar dengelendi:** BASİT tuvalde VERDİM:ALDIM = **1,25 : 1** (`tuval-satis.css:142`) — ad 1280'de 145 → **182 px**. (3) 375 px (tek sütun): Tutar↔Sil↔ad arası iç dolgu 8 → 4 px (yalnız bu tabloda, ≤600 px; OLCEK-1'in satır boyu, yazı ölçüsü, ilk/son hücre 1rem dolgusu DEĞİŞMEDİ) — ad 149 → **161 px** (metin 159 px) | s5 ölçümü 72 BASİT ölçüm: `satis-nakit`, `satis-zirkon` (Zirkon + Burma iki kalem), `satis-kart`, karma ×2, hurda, açılış, arama × {1440,1280,1024,960,768,375 açık + 1440,1280,375 koyu}: **K10b 0, K3 0, yatay 0**; Burma her genişlikte **1 satır**, Zirkon (26 kr.) 1280 ve 375'te **2 satır**, diğerlerinde 1; kesik (satır > 2 ya da gizli taşma) **0**. Görüntüler Read ile görüldü: `t4/foto/SATIS-3-senaryo-satis-zirkon-BASIT-aydinlik-1280.png`, `t4/s5/foto/SATIS-3-senaryo-satis-zirkon-BASIT-karanlik-375.png` |
| ORTA | PRO 1024/960 ad kesik; (d) yorumu yanlış | PRO ad da `line-clamp:2` (üç nokta yok); ölçüm sırasında OLCEK-1'in 15:45 `olcek.css` değişikliğiyle ad 150–156 px'e düşüp Burma iki satıra kırılınca (K3) dar kalem kartında (kap sorgusu ≤720 px) **Tutar sütunu 136 → 120 px** (8 haneli tutar + dolgu sığar; geniş kartta OLCEK-1 ölçüsü aynen). (d) yorumu ölçümle düzeltildi | `olcpro.mjs`: 1024 kart 706 → ÜRÜN 196 / ad 172 px **1 satır**; 960 kart 642 → ad 166 px **1 satır** (önce 156/150, 2 satır). s6 PRO 45 ölçüm (satış, zirkon, karma ×2, hurda × 9): **K1..K13 = 0, yatay 0**. Görüntü `t4/s6/foto/SATIS-3-senaryo-satis-zirkon-PRO-aydinlik-1024.png` (Read ile görüldü: iki kalem, adlar tam) |
| ORTA | Öneri listesinde stok yok | **Açık kalan (sahibi Api/Urun)** — `/urunler` yanıtında stok alanı yok (`Urun` varlığı stok taşımaz, stok `stoklar` tablosunda şube bazlı). Ayrı backend paketi kuyruğa önerildi: `GET /urunler?…&stok=true` → satır başına şube stoku; gelince `FisKalemGirisi.razor` öneri satırına tek sütun | § 16.1 gerekçe aynen geçerli |
| DÜŞÜK | Hurda alışta has karşılığı ve tutar görünmüyor | `FisHurdaAlis.razor`: salt okunur tek satır `#hurda-hesap` — "Has karşılığı: 9,3750 has · Tutar: 61.753,97 ₺"; hesaplanamıyorsa "—". Formül `HurdaGirisi.HasKarsiligi/HesaplananTutar` = sunucu (`SatisModulu.HurdaAlis`: `HasHesap.ToHasGram` × fiyat, 2 hane AwayFromZero). Stopaj oranı yapılandırmadan geldiği için istemcide HESAPLANMAZ; satıcı tipi esnaf muaf seçilince tek not: "Esnaf muaflığında kesinti düşülür; kesin tutar kaydedince yazılır." (TR+EN `Sozlukler.Satis3.cs`) | Canlı eşlik: 12,5 g · K18 · 6.587,09 → istemci 9,375000 / **61.753,97**; `POST /islemler/hurda-alis` → **200**, `hasGrami 9.375`, `brutTutarTl 61753.97`, `odenecekTl 61753.97`, gider pusulası `GP-2026-000001`. SQL: `hareketler` HURDA_ALIS `has_grami 9.375000`, `tl_tutari -61753.97`. Ekranda BASİT/PRO tüm genişliklerde satır görünür (`t4/foto/SATIS-3-senaryo-hurda-dolu-BASIT-aydinlik-1280.png` Read ile görüldü) |
| DÜŞÜK | Boş odak/arama listesi alttakinin üstüne biniyor (K9) | Değişmedi — açılır liste; **KATMAN** şeridi listeyi ortak katman açıcısına bağlayınca ölçüm dışı kalır | s5 `arama-liste` 9/9 ölçümde K9 (yalnız liste ⟂ "VERDİM · ÜRÜNLER" başlığı) |
| DÜŞÜK | Ağaçta eski artıklar | Dokunulmadı (hook silmeyi engelliyor) → **kurucu silmeli**: `Pages/Islemler.razor.mutorj`, `Pages/Islemler.Sade.cs.tmp`, `betikler/tur/profil-1`, `betikler/tur/private`. Ayrıca bu koşumda `tutarlilik.mjs` çıktılarını çalışma dizinine yazdı: `betikler/tur/tutarlilik-{1440,1280}.json`, `tutarlilik-dugme-*.txt`, `tutarlilik-fark-*.txt` (önceden var mıydı doğrulanamadı — dizin izlenmiyor; kopyaları `/private/tmp/kuyumcu-serit/SATIS-3/t4/tut/`) → kurucu silebilir | — |

### 17.2 Testler
- Yeni: `SATIS3BilesenTestleri.Hurda_hesap_satiri_has_ve_tutar_SUNUCU_formuluyle_eksikse_tire_kesinti_notu_YALNIZ_esnafta`, `SATIS3BilesenTestleri.Kalem_tablosu_BASIT_ad_KENDI_span_inda_ayar_gram_ikinci_satir_verisinde` (2 yeni vaka).
- SATIS3 kümesi: **87/87** (önce 85). İlgili kümeler (SATIS3/SATIS2/TS1/TS2/Hurda/Islem/Fis/L1): **707/708** — tek kırmızı `OlcekTekTestleri.OL22_v9_cekmece…` (beklenen `var(--yazi-cekmece-baslik)`, gelen "(yok)": `olcek.css` `#cekmece-cari` kuralı — **OLCEK-1**, dosya 15:30'da o şeritçe değiştirildi; bu paket olcek.css'e dokunmadı).

### 17.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3/<kod>.orj` → tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-1r | `DigerAyrintiAcik => true \|\| …` (gizli alan açılışta görünür) | SATIS3.Aciklama_TUM_turlerde_ARTI_AYRINTI_altinda_KAPALI (ALIS, HURDA_ALIS, SATIS_IADE, TAKAS), SATIS2.BASIT_Aciklama_KATLI…, SATIS2.PRO_Ayrinti_ekle_KATLI… | BİREBİR |
| M-S3-2r | `odemeler = bacaklar.Take(1).Select(` (eklenen ödeme gövdeden düşer) | SATIS3.Otomatik_KALAN…GOVDEYE_girer, SATIS3.PRO_dan_BASIT_e_geciste_KARMA…, SATIS3.Tek_kasada_bacak_basina_KASA_SECICI_YOK… | BİREBİR |
| M-S3-3r | hurda adres aynası `SaticiTamam`'dan düşer | S21FisSafTestleri.HurdaGirisi_satici_kimligi_kurallari_sunucu_kapisinin_aynasi, SATIS3.Hurda_ADRESSIZ_kaydet_KILITLI… | BİREBİR |
| M-S3-4r | `IadeGonderilebilir => true \|\| …` (iade kaynak seçimi atlanır) | SATIS3.Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
| M-S3-22 | hurda tutarında 2 hane yuvarlama kaldırıldı | SATIS3Bilesen.Hurda_hesap_satiri… (ilk koşumda **yeşil kaldı = test açığı**: ekran biçimi yuvarlamayı gizliyordu → teste model değeri iddiası eklendi → yeniden koşumda kırmızı) | BİREBİR |
| M-S3-23 | ad span'ı kaldırıldı | SATIS3Bilesen.Kalem_tablosu_BASIT_ad_KENDI_span_inda… | BİREBİR |
| M-S3-24 | kesinti notu koşulu tersine (`!= "ESNAF_MUAF"`) | SATIS3Bilesen.Hurda_hesap_satiri… | BİREBİR |
| M-S3-25 | has karşılığı yerine ham gram | SATIS3Bilesen.Hurda_hesap_satiri… | BİREBİR |
Son mutasyondan sonra temiz derleme: **0 Hata, 0 Uyarı**; son tam derleme 0 Hata, 1 Uyarı (başka şeridin `EtiketEkranTestleri.cs(145)` xUnit1031).
CSS kuralları bUnit'le sınanamaz → tarayıcı ölçümüne `adKontrol` (satır sayısı, gizli taşma) eklendi (`t4/senaryo.mjs`).

### 17.4 Gerçek tarayıcı — özet (s5 BASİT 72 + s6 PRO 45 = 117 ölçüm, son CSS ile)
| Durum | Mod | Ölçüm | Kusur (K1..K13, yatay, ad>2 satır) | 1440 açılış boyu |
|---|---|---|---|---|
| satis-nakit | BASİT / PRO | 9 / 9 | 0 / 0 | 1,0 / 1,0 |
| satis-zirkon (26 kr. ad + Burma) | BASİT / PRO | 9 / 9 | 0 / 0 | 1,0 / 1,0 |
| satis-kart | BASİT | 9 | 0 | 1,0 |
| karma-kart-altin | BASİT / PRO | 9 / 9 | 0 / 0 (K7 0; 375'te araç seçici kasıtlı ayrı satır) | 1,75 / 2,15 (dolu karma, açılış değil) |
| karma-nakit-kart-altin | BASİT / PRO | 9 / 9 | 0 / 0 | 2,0 / — |
| hurda-dolu | BASİT / PRO | 9 / 9 | 0 / 0 — hesap satırı görünür | 1,0 / 1,13 |
| acilis-SATIS | BASİT | 9 | 0 | 1,0 |
| arama-liste | BASİT | 9 | K9 9 (açılır liste — KATMAN) | 1,0 |
`tutarlilik.mjs /islemler` 1440 ve 1280: **BASİT↔PRO ölçü farkı 0** (düğme sınıf grubu 5). Ham veri: `/private/tmp/kuyumcu-serit/SATIS-3/t4/{olcum.jsonl, s5/olcum.jsonl, s6/olcum.jsonl, tut/}`.

### 17.5 Canlı doğrulama ve temizlik
- API 5162 (`--no-build`, Development; sistem anahtarı bu koşum için üretildi, dosyası sonra boşaltıldı), vekil 5163; kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3M0CW1GZQ71BVDQTZV6F2B0) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py`: 6+1 cari ("Anadolu Holding A.Ş."), 12 ürün, 12 satış (1 ek satış tevsik kapısında **400** — beklenen yasal kapı: 95.068,41 TL nakit > 30.000 TL).
- Uçlar (yanıt okundu): `GET /urunler?aktif=true&q=bil` 200 [Burma Bilezik, Gümüş Bileklik] · `q=zirkon` 200 [Zirkon Taşlı Kolye, Zirkon Taşlı Yüzük] · `q=zzqx` 200 [] · `siralama=cok-satan&limit=8` 200 (8 ürün) · `POST /islemler/hurda-alis` 200 (§ 17.1).
- Durdurma: API ve vekil PID'leri `kill`; `lsof` 5162/5163/9906/9907/9908 **boş**.
- Silme: `kiraci-temizle.sh 01M3M0CW1GZQ71BVDQTZV6F2B0 --uygula` → rc 0; SQL: tenants'ta PAKETSATIS-3 **0**, `hareketler` bu kiracı **0**, `kiraci_davetleri` PAKETSATIS-3 satırı 1 → silindi → **0**. `tenants` 17 (başlangıç) → **19**: artış başka şeritlerin koşumdaki kiracıları (PAKET-ORTAK-1-TEST 28, PAKET-OLCEK-1H-TEST, PAKETZO-ISLEM-A-TEST, PAKETEMANET-1-TEST T33 — liste `kiraci-temizle.sh` çıktısında); bu şeridinki **0**.

### 17.6 Değişen dosyalar (bu tur)
`dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` (ad iki satır BASİT+PRO, 1,25:1 sütun, 375 iç dolgu, dar PRO kartta Tutar 120 px, hurda satırı, (d) yorumu) · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor` (ad span'ı) · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisHurdaAlis.razor` (#hurda-hesap) · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisModelleri.cs` (HasKarsiligi, HesaplananTutar) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (HurdaHas, HurdaTutar, HurdaKesintiNotu TR+EN) · `dotnet/Kuyumcu.Tests/SATIS3BilesenTestleri.cs` (2 test). Yeni DOM id'leri: `hurda-hesap`, `hurda-hesap-has`, `hurda-hesap-tutar`, `hurda-hesap-not`; mevcut id değişmedi. POST gövdeleri değişmedi. Migration: gerekmedi.

### 17.7 DoD sözleşmesi (bu tur)
Yeni varlık/kayıt türü yok. `#hurda-hesap` salt okunur hesap gösterimidir (kaynak girdiler aynı formda düzenlenebilir; kayıt hareketi değişmez — düzeltme yolu mevcut storno/ters hareket). Yeni düğme yok.

### 17.8 Tam süit ve denetim (bu tur, güncel ağaç, 16:0x)
- `dotnet build Kuyumcu.sln` → **0 Hata**, 1 Uyarı (başka şeridin `EtiketEkranTestleri.cs(145)` xUnit1031).
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pSATIS-3/satis3-t4.trx`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`, MaxCpuCount=2): **6584 · 6582 geçti · 2 kırmızı**, hiçbiri bu paketin:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` — beklenen 15, gelen 16: `OrtakGorunumMetinleri` alan sayısı → **ORTAK-1** (sözlük o şeridin; bu paket dokunmadı).
  - `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` — 47 → 48 sayfa: `Pages/EmanetHamKap.razor` → **EMANET-1** (§ 16.4'te de aynı).
  - Filtreli koşumdaki `OlcekTekTestleri.OL22…` tam süitte YEŞİL (OLCEK-1 kendi dosyasını düzeltmiş).
  - **Bu paketin yeni kırmızısı: 0** (SATIS3 87/87; İşlem/Fiş/TS1/TS2/SATIS2/L1/Hurda kümeleri yeşil).
- `denetim.sh` → **BAŞARISIZ, 2 ✗**, ikisi de EMANET-1'in: Pages sayısı 48 (tavan 47) ve P-37c `acik=1` (`EmanetHamKap.razor`). Bu şeridin sayaçları ✓ (Pages ModalPencere 48/55, P-16 ×4, P-36 HAM_SELECT 28/30 · HAM_INPUT 32/42, beyaz liste dışı .mutorj 0).

### 17.9 Paket kapısı (7 madde, bu tur)
| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | build 0 hata | ✓ | § 17.8 |
| 2 | bu paketin testleri 0 başarısız | ✓ | SATIS3 87/87; ilgili kümeler 707/708 (tek kırmızı OLCEK-1'in, tam süitte yeşil) |
| 3 | sayaçlar ✓ | ✓ | § 17.8 |
| 4 | mutasyon | ✓ | § 17.3 — 8/8 kırmızı, cmp BİREBİR (M-S3-22 test açığı kapatıldı) |
| 5 | canlı doğrulama | ✓ | § 17.1, § 17.4, § 17.5 (117 tarayıcı ölçümü + API + SQL) |
| 6 | test kiracısı silindi | ✓ | § 17.5 |
| 7 | yeni kırmızı yok | ✓ | § 17.8 — fark 0 |

### 17.10 Açık kalanlar (bu tur, sahibiyle)
1. **Api/Urun (ayrı backend paketi):** öneri listesinde stok sütunu — `/urunler` stok taşımıyor.
2. **KATMAN:** öneri listesinin ortak katman açıcısına bağlanması (K9 ölçüm dışı kalır).
3. **ORTAK-1:** `O8` (OrtakGorunumMetinleri 16 alan). **EMANET-1:** `P37c` + denetim sayfa tavanı (`EmanetHamKap.razor`).
4. **Kurucu:** 0 baytlık artıklar ve `betikler/tur/{profil-1,private,tutarlilik-*}` silinmeli (hook engelliyor).
5. PRO→BASİT karma geçişi (önceki bulgu) § 15'te kapatılmıştı; bu turda yeniden ölçülmedi.

## 18. 28.09 t6 koşumu (19:25–20:20) — t5 doğrulamasının (KALDI) bulguları + tasarım hakemi t3'ün 15 bulgusu, GÜNCEL ağaçta yeniden ölçüldü

Önceki t5 koşumu (17:05–18:10) kod değiştirdi ama raporu güncellemedi, test kiracısını ve CDP sürücüsünü açık bıraktı. Bu bölüm t5+t6'nın tek kaydıdır.
Ortam: API **5162** (`--no-build`, Development), vekil **5163**, başsız Chrome CDP **9911–9916** (tarayıcı bölmesi KULLANILMADI), `KUYUMCU_TEST_DB=kuyumcu_test_t2`, geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t6/`. **COMMIT YOK.**
⚠ Ölçüm notu: t6'nın ilk yeniden ölçümünde Chrome ESKİ `tuval-satis.css`'i önbellekten sundu (sayfadaki kural `order:0` dönüyordu). Bundan sonraki TÜM ölçümler `Network.setCacheDisabled` ile yapıldı (t6/ortak.mjs). t5'in ölçümlerinde bu önlem yoktu — o yüzden hakem bulgularının hepsi bu koşumda baştan ölçüldü.

### 18.1 Doğrulama bulguları (t5 KALDI) → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| KRİTİK | `KarmaOdemeEkranTestleri` ×2 kırmızı (eski metin) | İki test sade metne göre yeniden yazıldı: beklenen `Satis3Sozlugu.Tr.NakitAlinamaz` ve `string.Format(FarkEksik/FarkFazla, Bicim.Tl(1000m))`; eski metnin çizilmediği (`DoesNotContain`) ve **kilidin Kaydet'i kapattığı** ölçülüyor: nakit→kart çevrilince Kaydet açılır; 9.000 → Eksik, 11.000 → Fazla, 10.000 → açık | 20/20 (`t6/test2.log`); mutasyon M-S3-T6-6b (`&& !NakitKapali` → `&& true`) → `Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` KIRMIZI |
| KRİTİK | t5 test kiracısı silinmemiş, CDP sürücüsü açık | Sürücü `/kapat` ile kapatıldı (PID 71462 bitti, Chrome 0). Kiracı `01M3M5CVCRQ2PGRYPHDWMV8X3T` bu koşumda ölçüm kiracısı olarak kullanıldı, sonunda `kiraci-temizle.sh … --uygula` (rc 0) | § 18.6 SQL: 70 tenant_id'li tabloda **0 satır** |
| ORTA | Varsayılan kasa şubeyi dikkate almıyor, sıra belirsiz | (1) `KasaVeri`'ye `SubeId` (yanıtta zaten var; opsiyonel, eski gövdeler kırılmaz). (2) Saf kural `FisHesap.VarsayilanKasa(kasalar, oturumSubeId)`: oturum şubesinin (ya da şubesi bilinmeyen) ilk TL kasası; şubede kasa yoksa **null** → BASİT'te de seçici görünür (fail-closed); şubesiz oturumda kasalar birden çok şubeye dağılmışsa null. Sayfa (`Islemler.razor` iki yer) ve Diğer formları (`DigerBaglam.OturumSubeId` → `FisDigerKasa`) aynı kuralı kullanır. (3) Sunucu `KasaModulu.Listele` → `OrderBy(k => k.Id)` (ULID = açılış sırası) | 5 yeni test (T6_Varsayilan_kasa…, T6_Subede_kasa_YOKSA…, T6_VarsayilanKasa_saf_kural ×3, T6_Diger_formlari…); mutasyon M-S3-T6-1 (şube süzgeci kaldırıldı) → 5 kırmızı, M-S3-T6-2b (Diğer'de ilk kasa) → 1 kırmızı. Canlı: `GET /kasalar` 200 `[Ana Kasa, Vitrin Kasası]` (Id sırası, `subeId` dolu); BASİT kart satışında POST gövdesi `kasaId=01M3M5DZ…` (Ana Kasa); SQL `kasa_hareketleri` son satır Ana Kasa 94.944,46 KART |
| ORTA | "Kurdan geldi" rozeti mod değişince "Elle girildi" | Bayrak bileşenden `AltinBacagiGirisi.FiyatKurdan`'a taşındı (elle yazınca düşer) | T6_Kurdan_geldi_rozeti_PRO_dan_BASIT_e_geciste_KORUNUR; mutasyon M-S3-T6-3 (yerel bayrağa geri dön) → 2 kırmızı. Tarayıcı: PRO'da "Güncel fiyatı kullan" → `Kurdan geldi`; BASİT → `Kurdan geldi` (6.569,81); PRO'ya dönüş → `Kurdan geldi` |
| ORTA | Değişim/Hurda/Takas'ta başlık "Satış" | `SayfaAdi` seçili türü taşır (BASİT karo adı, PRO sekme adı; tek kaynak `FisTurleri.KaroEtiket`), form başlığı `SeciliTurAdi Formu`, `<title>` de. **Görünen üst bar başlığı** (`Layout/UstBar.razor` → `RotaBasliklari`, rota bazlı) bu şeridin alanı DIŞINDA → açık kalan (§ 18.8) | T6_Sayfa_basligi ×2; mutasyon M-S3-T6-5 → 2 kırmızı. Tarayıcı: `#sayfa-basi-islemler-baslik` = "Değişim", h2 "Değişim Formu" |
| ORTA | Karmada "Alınan / Para üstü" açık | `FisParaUstu Katli` — karmada kapalı "+ Para üstü hesapla" (TR+EN `ParaUstuHesapla`); tek ödemede eski davranış | T6_Karmada_para_ustu…, S25_E7 güncellendi; mutasyon M-S3-T6-4 → 2 kırmızı |
| ORTA | PRO karmada eski altın satırının öğesi fieldset dışında, sol altta | Ölçüldü: "Ödeme aracı" seçicisi fieldset'in ÜSTÜNDE (y777 < 841); sol-alta düşen **sil düğmesi** idi (y1454). `tuval-satis.css (r)`: eski altın paneli satırın son öğesi, tam genişlik → sil aracın yanında (x475, y777) | `SATIS-3-t6-karma-kart-altin-PRO-1440.png` (Read ile görüldü) |
| ORTA | Öneri listesinde fiyat "—", stok yok | Değişmedi — gerekçeli açık kalan: satış fiyatı sabit değil, `/fiyat/teklif` (cari + fiyat listesi + canlı kur) ile hesaplanır; sabit `satisFiyati` olan ürün fiyatla görünür. Öneri başına teklif = N istek (ISTEK-1 oran kuralı). Stok `/urunler` yanıtında yok | Sahip: Api/Urun + Api/Fiyat (toplu teklif/stok ucu) |
| DÜŞÜK | Başka şeritlerin kırmızıları | § 18.5'te sahipleriyle | — |

### 18.2 Tasarım hakemi t3 — 15 bulgu, GÜNCEL ağaçta bulgu bulgu (başsız Chrome, önbelleksiz)
| # | Bulgu | Durum | Kanıt (ölçüm / görüntü) |
|---|---|---|---|
| 1 KRİTİK | PRO Kaydet alt satıra/sola düşüyor, gerekçe kesik/minik | **KAPANDI** | PRO tek ödeme 1440: Vazgeç (799,702) · Kaydet (901,698) aynı satır, sağda; karmada (799,1567)/(901,1563). Gerekçe Kaydet'in altında 14 px, sağa yaslı; renk vurgu mavisi yerine **uyarı tonu** (`(t)`, rgb 151,78,0 aydınlık) — bağlantı sanılmıyor. "Ödeme bacakları" metni yok |
| 2 KRİTİK | Teknik metinler ekranda ("Kaydedince İKİ belge doğar" vb.) | **KAPANDI** | BASİT karma: görünür metinde yok; "Kaydedince…" yalnız kapalı (i) Bilgi içinde ve ekran okuyucu satırında (0×0 / 1×1). PRO'daki görünür tekrar da gizlendi (`#islem-kaydet-belgeler` iki modda `gorsel-gizli`). Büyük harf vurgusu yok |
| 3 YÜKSEK | Kasa otomatik değil | **KAPANDI** | İki kasalı kiracıda BASİT Satış/Hurda/İade/Değişim: "Ana Kasa" salt okunur (seçici yok); PRO'da değiştirilebilir seçici dolu gelir; Diğer (Kapora) "Ana Kasa" dolu. Şube kuralı § 18.1 |
| 4 YÜKSEK | Takas/Değişim'de ayrı ürün seçici | **KAPANDI** | Değişim "Yeni ürün" = ortak `#islem-barkod` kutusu (öneri listesi, "Ürün: 22 Ayar Burma Bilezik"); Alış'ta ayrı "Barkod okut" düğmesi yok |
| 5 YÜKSEK | K7 hizası | **KAPANDI** | 225 + 117 ölçümde sikisma K7 = 0 (375'te araç/tutar kasıtlı alt alta: `bacakHiza` 78/157 yalnız yığın düzeninde) |
| 6 YÜKSEK | PRO karanlıkta seçili sekme ayırt edilmiyor | **KAPANDI (bu koşumda bulunan kök neden)** | t3'ün kuralı yalnız ZORLANMIŞ karanlıkta (`[data-tema=karanlik]`) çalışıyordu; sistem karanlığında seçili sekme zemini rgb(9,13,22) = sayfa. `(s)` kuralı: sekme `var(--vurgu)` dolgulu → ölçüm rgb(145,185,255); görüntü `PRO-karma-kart-altin-1440-t6.png` |
| 7 ORTA | "bacak" jargonu, büyük harf | **KAPANDI** | Band metni `NakitAlinamaz` ("Bu tutarda nakit alınamaz — ödemeyi kart ya da havaleyle alın."); K8 jargon 0 |
| 8 ORTA | Tekrar eden ödeme bilgisi, açık isteğe bağlılar | **KAPANDI** | Tek ödeme: tek "Nakit · Ana Kasa" satırı; karma: tek özet (Fiş toplamı / Alınan toplam / Kalan); para üstü "+ Para üstü hesapla" arkasında |
| 9 ORTA | Eski altın has fiyatı kurdan gelmiyor | **GEREKÇELİ FARK** | K-KUR-4: alış fiyatı dükkânın ticari kararı → alan kendiliğinden dolmaz; tek dokunuş "Güncel fiyatı kullan: 6.569,81 ₺" → "Kurdan geldi" (artık mod geçişinde korunur). Hurda/Takas'ta kur varsayılanı eski davranış |
| 10 ORTA | Öneri listesinde stok yok, fiyat "—" | **AÇIK (alan dışı)** | § 18.1 |
| 11 ORTA | Tür adları/başlık tutarsız | **AÇIK — görünen başlık KARŞILANMADI** (t7 düzeltmesi, § 19.1: kullanıcının gördüğü tek başlık üst bar h1 "Satış"; `#sayfa-basi-islemler-baslik` ve form h2 ekranda 1×1 görsel-gizli. Kaydet adı ve `<title>` kapandı) | BASİT Kaydet = "Kaydet", PRO = "Takas Kaydet" (sekme adıyla aynı); sayfa başı ve form başlığı karo/sekme adı. Üst bar rota adı "Satış" (Layout — açık kalan) |
| 12 ORTA | PRO Yasal kapılar açılışta hep 3 satır | **KAPANDI** | PRO Takas/Hurda/İade/Alış açılışında kart YOK (`SATIS-3-t6-acilis-TAKAS-PRO-1440.png`); Satış'ta yalnız eşik aşılınca |
| 13 ORTA | Diğer formlarında açıklama satırı, kilit kutusu, "Henüz kayıt seçilmedi" | **KAPANDI** | Kapora: açıklama yalnız (i) Bilgi, kilit kutusu yok, ipucu "Müşteri seçin", kasa dolu (`SATIS-3-t6-diger-avans-PRO-1440.png`) |
| 14 ORTA | Tevsik çelişkisi | **KAPANDI (bu koşumda bulunan ek kusur)** | Canlıda bulundu: Peşin Müşteri + KART 94.944 TL → sunucu 400 (kimlik), ekranda AYNI ANDA "nakit alınamaz — kartla alın" bandı. `YasalBantAcik`/`NakitOdemede`: nakit bandı yalnız ödemede nakit varken; 400'ü Peşin kimlik bandı söylüyorsa ikinci bant yok. Tarayıcı: kart + 400 sonrası `#islem-yasal-kapi` YOK, `#islem-pesin-esik` + hata bandı var; gerçek müşteri seçilince **200** "İşlem başarıyla kaydedildi" · Son fiş 94.944,46 ₺. Test T6_Nakit_kurali_bandi…; mutasyon M-S3-T6-7 → kırmızı. Mevzuat hükmü DEĞİŞMEDİ (eşik sunucudan) |
| 15 DÜŞÜK | Kenar çubuğu boyu / küresel arama 375 | **KAPSAM DIŞI** → KATMAN | — |
Ek (bu koşum): Değişim/İade/Tek-kasa satırındaki "Kasa" etiketi 18 px (diğer etiketler 14 px) → `<label class="alan-etiket">` (ölçüm 14 px = 14 px).

### 18.3 Gerçek tarayıcı — senaryo matrisi (her durum × {1440,1280,1024,960,768,375 aydınlık + 1440,1280,375 karanlık})
İlk tur `t6/m` (31 durum×mod = 279 ölçüm) + son derlemeyle tekrar `t6/m2` (13 durum×mod = 117 ölçüm):
| Durum | Mod | Ölçüm | Kusur (K1..K13, yatay, ad>2 satır) | 1440 boy | Görünen etkileşim |
|---|---|---|---|---|---|
| acilis-SATIS | BASİT / PRO | 9 / 9 | 0 / 0 | 1,0 / 1,0 | 13 / 16 |
| satis-nakit | BASİT / PRO | 9+9 / 9+9 | 0 / 0 | 1,0 / 1,0 | 18 / 21 |
| satis-zirkon · satis-kart | BASİT | 9 · 9 | 0 · 0 | 1,0 | 19 · 18 |
| karma-kart-altin | BASİT / PRO | 9+9 / 9+9 | 0 / 0 (375: yığın düzeni, K7 0) | 1,65 / 1,89 (dolu) | 29 / 34 |
| karma-nakit-kart-altin | BASİT / PRO | 9+9 / 9+9 | 0 / 0 | 1,84 / 2,15 (dolu) | 33 / 39 |
| hurda-dolu | BASİT / PRO | 9+9 / 9 | 0 / 0 | 1,0 / 1,13 | 16 / 20 |
| iade-kaynak-secili | BASİT / PRO | 9+9 / 9 | 0 / 0 | 1,0 / 1,0 | 11 / 15 |
| takas-iki-tarafli | BASİT / PRO | 9+9 / 9+9 | 0 / 0 | 1,0 / 1,12 | 16 / 19 |
| acilis-TAKAS | BASİT / PRO | 9+9 / 9 | 0 / 0 | 1,0 / 1,0 | 14 / 17 |
| acilis-ALIS · HURDA_ALIS · ALIS_IADE | PRO | 9 · 9+9 · 9 | 0 | 1,0 | 15 · 17 · 14 |
| kapora-dolu · diger-avans | PRO | 9 · 9+9 | 0 | 1,0 | 14 |
| diger-rezerv · konsinye · degisim · siparis · evrak | PRO | 9 ×5 | 0 | 1,0 | 13 · 13 · 17 · 9 · 9 |
| arama-liste · arama-bulunamadi | BASİT | 9 · 9 | yalnız K9 (açık öneri listesi alttaki başlığın üstünde — bağlı açılır katman; KATMAN) | 1,0 | 13 |
(`acilis-ALIS` BASİT: BASİT'te ayrı Alış karosu yok — "Alış · eski altın" = HURDA_ALIS; ölçüm Satış açılışıdır.)
Ham veri: `/private/tmp/kuyumcu-serit/SATIS-3/t6/{m,m2}/olcum.jsonl`, özet `ozet.py`. Kanıt görüntüleri (Read ile görüldü): `ham/TUVAL-KARSILASTIRMA/SATIS-3-t6-*.png` (24 dosya: 8 sekme + Diğer'in 6 formu açılış 1440, dolu karma BASİT/PRO + PRO karanlık + BASİT 375, Peşin kimlik bandı).
Kalan görsel not (DÜŞÜK): BASİT 375 karmada eski altın satırının sil düğmesi aracın altında ayrı satırda (Kart satırında tutarın yanında) — taşma/üst üste binme yok, işlev aynı.

### 18.4 Testler ve mutasyonlar
- Yeni: 9 metot / 11 vaka (`SATIS3EkranTestleri.T6_*` ×8 metot, `SATIS3BilesenTestleri.T6_Diger_formlari…`). Güncellenen: `KarmaOdemeEkranTestleri` ×2, `S25YazdirParaUstuEkranTestleri.S25_E7`.
- İlgili kümeler (SATIS3/SATIS2/TS1/TS2/Hurda/Islem/Fis/L1/KarmaOdeme/S24/S25/B4/AltinBacagi/Kasa/BasitMod): **1384/1384** (`t6/test-kume2.log`).
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3/<kod>.orj` → perl → filtreli test → geri kopya → cmp):
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-1s | `DigerAyrintiAcik => true \|\| …` (gizli alan açılışta) | SATIS3.Aciklama_TUM_turlerde_ARTI_AYRINTI_altinda_KAPALI ×4 | BİREBİR |
| M-S3-2s | `bacaklar.Take(1)` (eklenen ödeme gövdeden düşer) | KarmaOdeme.Karma_fis_TEK_POST…, SATIS3.PRO_dan_BASIT_e…, Tek_kasada…, Otomatik_KALAN… | BİREBİR |
| M-S3-3s | hurda adres kapısı düştü | S21FisSaf.HurdaGirisi_satici_kimligi…, SATIS3.Hurda_ADRESSIZ… | BİREBİR |
| M-S3-4s | `IadeGonderilebilir => true \|\| …` | SATIS3.Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
| M-S3-T6-1 | varsayılan kasada şube süzgeci kaldırıldı | T6_Diger…, T6_VarsayilanKasa ×2, T6_Subede_kasa_YOKSA…, T6_Varsayilan_kasa… | BİREBİR |
| M-S3-T6-2b | Diğer formunda ilk kasa (şubesiz) | T6_Diger_formlari… | BİREBİR |
| M-S3-T6-3 | kaynak bayrağı bileşen yereline geri | T3_Guncel_fiyati_KULLAN…, T6_Kurdan_geldi… | BİREBİR |
| M-S3-T6-4 | para üstü katlanması kaldırıldı | S25_E7, T6_Karmada_para_ustu… | BİREBİR |
| M-S3-T6-5 | başlık hep "Satış" | T6_Sayfa_basligi ×2 | BİREBİR |
| M-S3-T6-6b | nakit kilidi Kaydet'ten düştü | KarmaOdeme.Had_asilinca_NAKIT… | BİREBİR |
| M-S3-T6-7 | `NakitOdemede => true` | T6_Nakit_kurali_bandi… | BİREBİR |
(İlk denemede M-S3-T6-2 başka şeridin eşzamanlı derlemesi (apphost kilidi) ve M-S3-T6-6 yorum satırına düşen `;` yüzünden DERLENMEDİ — sonuçları geçersiz sayıldı, `-2b`/`-6b` olarak yeniden koşuldu.) Son mutasyondan sonra temiz derleme: **0 Hata**, 1 Uyarı (başka şeridin `EtiketEkranTestleri.cs(145)` xUnit1031). Bir ara derlemede `blazor.boot.json` başka süreç kilidinden MSB4018 alındı; tekrar derleme temiz.

### 18.5 Tam süit ve denetim
- 1. koşum (19:55, t6 düzeltmelerinin çoğu): **6743 · 6742 geçti · 1 kırmızı** — `OrtakGorunumSaflik.O8` (ORTAK-1).
- Son koşum (20:10, son ağaç, `trx-pSATIS-3/satis3-t6-son.trx`): **6744 · 6736 geçti · 8 kırmızı**, hiçbiri bu paketin:
  - `OrtakGorunumSaflik.O8` → **ORTAK-1** (baz listede de var).
  - `OlcekTekTestleri.OL9`, `OL24_v11…` → **OLCEK-1** (`olcek.css` 20:0x'te o şeritçe değişiyordu; OL9 baz listede).
  - `EmanetSadeTestleri.E23…`, `E33…`, `Dogrulayici.EMANET1SinirTestleri.V1…`, `W3…` → **EMANET-1** (Emanet.razor/EMANET1SinirTestleri 19:24–19:25 değişti; tek başına koşumda yeşil → eşzamanlı düzenleme).
  - `P20bEkranTestleri.Defterde_ham_tur_kodu_BASILMAZ…` → **ZO-ISLEM-A** (Hurda): tek başına da kırmızı (`#hrd-defter` bulunamadı). `Hurda.razor` bu paketin dosyalarından hiçbirini kullanmaz (grep: FisHesap/FisParaUstu/AltinBacagi/Satis3/Contracts.Islemler.KasaVeri yok — kendi `Contracts.Sayfalar.Kasalar.KasaVeri`'si); 1. koşumda yeşildi ve iki koşum arasında bu paket yalnız `Islemler.razor`/`Islemler.Odeme.cs`/`SATIS3EkranTestleri.cs`'e dokundu; aynı aralıkta `MasakFormu.razor` (20:01), `olcek.css`, `zo-islem.css` başka şeritlerce değişti.
  - **Bu paketin yeni kırmızısı: 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (Pages ModalPencere 48/55; `Islemler.razor` 1393 satır < 1400).

### 18.6 Canlı doğrulama ve temizlik
- Uçlar (yanıt okundu): `GET /kasalar` 200 (Id sırası, `subeId` dolu) · `POST /islemler/satis` (Peşin, KART, 94.944,46) **400** "Peşin Müşteri ile bu tutarda işlem yapılamaz… tevsik (459 s. VUK GT) eşiğine (30.000,00 TL)…" (yasal kapı — beklenen, DEĞİŞMEDİ) · aynı fiş gerçek müşteriyle **200** → SQL `hareketler` SATIS 94944.46 KART cari=Anadolu Holding; `kasa_hareketleri` Ana Kasa 94944.46 KART.
- Durdurma: API (dotnet run + Kuyumcu.Api), vekil, sürücü Chrome'u kapatıldı; `lsof` 5162/5163/9911–9916 **boş**; t6 node/chrome süreci 0.
- Silme: `kiraci-temizle.sh 01M3M5CVCRQ2PGRYPHDWMV8X3T --uygula` rc 0. Sonrasında `kur_snapshots`'ta bu kiracıya ait **1 satır** kaldı (id 01M3MFXGFB…, silme sırasında başka bir sürecin kur işi yazmış olabilir — betik o an satırı görmemiş) → `DELETE` (ROW_COUNT 1). Kanıt: 70 tenant_id kolonlu tabloda bu kiracıya **0 satır**; `tenants` PAKETSATIS% **0**. `tenants` 21 (t6 başı, t5 kiracısı dahil) → **19** (bu şeridin −1; diğer −1 başka şeridin kendi temizliği).
- Kurucuya not: `kiraci-temizle.sh` silme sırasında çalışan bir API'nin kur işi yeni `kur_snapshots` satırı yazabiliyor; betik sonunda tenant_id taramasıyla ikinci geçiş önerilir (betik bu şeridin dosyası değil — değiştirilmedi).

### 18.7 Değişen dosyalar (t6)
`Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs` (KasaVeri.SubeId) · `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` (Listele OrderBy — alan dışı tek satır, doğrulayıcının açık isteği; eşzamanlı şeritlerin hiçbirinin alanında değil) · `Kuyumcu.Web/Bilesenler/Fis/{FisModelleri.cs, FisDigerModelleri.cs, FisDigerKasa.razor, FisTurleri.cs, FisIslemIzgarasi.razor, FisParaUstu.razor, FisOdemeBacaklari.razor, FisOdemeKasa.razor}` · `Kuyumcu.Web/Bilesenler/AltinBacagiPaneli.razor` · `Kuyumcu.Web/Servisler/AltinBacagiGirisi.cs` (FiyatKurdan) · `Kuyumcu.Web/Pages/{Islemler.razor, Islemler.Diger.cs, Islemler.Odeme.cs}` · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (ParaUstuHesapla TR+EN) · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` ((r),(s),(t)) · testler `KarmaOdemeEkranTestleri.cs`, `S25YazdirParaUstuTestleri.cs`, `SATIS3EkranTestleri.cs`, `SATIS3BilesenTestleri.cs`. Yeni DOM id: `islem-para-ustu-ac`; mevcut id değişmedi. POST gövdeleri değişmedi. Migration: gerekmedi. Kanıt görüntüleri: `ham/TUVAL-KARSILASTIRMA/SATIS-3-t6-*.png`.

### 18.8 DoD sözleşmesi (t6)
Yeni varlık/kayıt türü yok. Yeni düğme: `#islem-para-ustu-ac` (tıklanınca alanı açar — istemci hesabı, istek atmaz; DEFTERE GİRMEZ). Kasa seçimi PRO'da her zaman değiştirilebilir; BASİT'te varsayılan yoksa seçici görünür.

### 18.9 Paket kapısı (7 madde, t6)
| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | build 0 hata | ✓ | § 18.4 |
| 2 | bu paketin testleri 0 başarısız | ✓ | 1384/1384 |
| 3 | sayaçlar | ✓ | denetim TEMİZ |
| 4 | mutasyon | ✓ | 11/11 kırmızı, cmp BİREBİR |
| 5 | canlı doğrulama | ✓ | § 18.3 (396 ölçüm) + § 18.6 |
| 6 | test kiracısı silindi | ✓ | § 18.6 |
| 7 | yeni kırmızı yok | ✓ | § 18.5 |

### 18.10 Açık kalanlar (sahibiyle)
1. **Layout/UstBar (KATMAN/ORTAK):** görünen üst bar başlığı rota adıdır ("Satış"); seçili türü göstermesi için sayfanın başlığı devralması gerekir (`RotaBasliklari`). Bu şerit sayfa başı/`<title>`/form başlığını düzeltti.
2. **Api/Urun + Api/Fiyat:** öneri listesinde stok ve (dinamik fiyatlı ürünlerde) fiyat — toplu uç gerekir.
3. **KATMAN:** öneri listesi bağlı açılır katman (K9).
4. **Güvenlik (Mehmet'e):** sunucu `IslemDogrulayici.KasaDogrula` kasanın oturum şubesine ait olduğunu denetlemiyor; ekran artık doğru varsayılanı seçiyor ama sunucu kapısı ayrı paket (fail-closed: şube dışı kasaya yazma reddi).
5. **ORTAK-1** O8 · **OLCEK-1** OL9/OL24 · **EMANET-1** E23/E33/V1/W3 · **ZO-ISLEM-A** P20b.Defterde_ham_tur_kodu — § 18.5.
6. DÜŞÜK: BASİT 375 karmada eski altın sil düğmesi ayrı satırda (§ 18.3).

## 19. 28.09 t7 koşumu (20:50–21:30) — t6 doğrulamasının kalan bulguları, güncel ağaçta
Ortam: API **5162** (`--no-build`, Development, sistem anahtarı bu koşum için üretildi, dosyası sonra boşaltıldı), vekil **5163**, başsız Chrome CDP **9913/9914** (matris) + **9917/9918** (sürücü), tarayıcı bölmesi KULLANILMADI, `Network.setCacheDisabled`. Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3MJP7FYG0BWBMCZNMFZHRY0) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py` (6+1 cari, 12 ürün, 12 satış, 2 kasa). Geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t7/`. **COMMIT YOK.**

### 19.1 Doğrulama bulguları → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| KRİTİK (hakem çelişkisi) + ORTA | Görünen başlık Değişim/Hurda/Takas'ta "Satış"; rapor "KARŞILANDI" diyordu | **Yeniden ölçüldü, rapor düzeltildi:** koşul **KARŞILANMADI (görünen ekranda)**. § 18.2 #11 satırı "AÇIK" olarak düzeltildi. Kod değişmedi: görünen tek h1 `Layout/UstBar.razor` → `RotaBasliklari` (rota bazlı); Layout bu şeridin dosya alanında değil ve eşzamanlı şeritlerin hiçbirine atanmamış → "alanın dışına çıkman gerekirse DUR ve rapora yaz" | Canlı (1280, BASİT+PRO): HURDA_ALIS/TAKAS/SATIS_IADE'de görünür h1 = `["Satış"]`; `#sayfa-basi-islemler-baslik` = "Alış · eski altın"/"Değişim"/"İade" (BASİT), "Hurda Alış"/"Takas"/"Satış İadesi" (PRO) — hepsi **1×1** görsel-gizli; `<title>` = seçili tür. Görüntü `SATIS-3-t7-takas-fark-BASIT-1440.png` (üst bar "Satış", karo "Değişim"). T6_Sayfa_basligi testleri görünmez öğeyi ölçtüğü için görünen ekranın kanıtı DEĞİLDİR |
| ORTA | Fiyatı hesaplanamayan üründe her durumda gümüşe özgü metin; kalem eklenince bant kalıyor | (1) `Islemler.Arama.cs` `FiyatHesaplanamadiMetni(Teklif?)`: sunucunun sebep kodu okunur — `LISTE_YOK` → "Fiyat listesi tanımlı değil. Birim fiyatı elle girip Ekle deyin.", `KOTASYON_YOK`/`GUMUS` → mevcut gümüş metni, `FIYAT_YOK` → "Bu ürünün fiyatı hesaplanamadı…", istemci `HATA` → "Fiyat alınamadı…", kur yok → "Güncel kur henüz gelmedi…" (TR+EN `Sozlukler.Satis3.cs`, 4 anahtar). Hem liste seçimi (`OneriSec`) hem barkod yolu (`Islemler.razor` BarkodOkut) aynı yardımcıyı kullanır. (2) `KalemEkle` başarıyla bitince `giris.BarkodHata = null`. (3) Bant sebebi söylüyorsa altındaki sarı `#islem-teklif-yok` satırı ikinci kez yazmaz (aynı bilgi iki kez — hakem #8 ilkesi). Bant `#islem-barkod-hata` id'si aldı (yeni id) | Canlı: liste pasifken `GET /fiyat/teklif` → 200 `{"teklifVar":false,"sebep":"LISTE_YOK",…}`; ekranda bant "Fiyat listesi tanımlı değil. Birim fiyatı elle girip Ekle deyin." (`SATIS-3-t7-fiyat-sebep-BASIT-1440-once-ekle.png`); 50.000 girilip Ekle → kalem "22 Ayar Burma Bilezik 50.000 ₺", bant **yok** (`…-sonra-ekle.png`). Testler T7_Fiyati_cikmayan_oneride…, T7_Fiyat_sebebi_metni… (×5) |
| DÜŞÜK | PRO→BASİT canlı geçişte nakit uyarı kutusu 0 ₺ fişte sayısız metinle açık | Kök neden: PRO'da (karma kapalı) önizleme `ProYasalTazele` yolundan gidiyor ve sunucunun `had` değerini yalnız `proTevsik`'e yazıyordu; `tevsikHaddi` null kalınca BASİT kutusu "eşik bilinmiyor → tutucu: göster" çiziyordu. `ProYasalTazele` artık sunucunun haddini `tevsikHaddi`'ye de yazar (sayı yine sunucudan, istemciye gömülmez) | Canlı 1280: PRO'da yenile → "Basit" tıkla → 50/300/1500/3000 ms'de `#islem-nakit-uyari` **hidden=true, data-yakin=0**, metin "Nakit 30.000 ₺'yi geçerse…" (`SATIS-3-t7-pro-basit-gecis-nakit-uyari-1280.png`); `GET /tevsik/gunluk` 200 `had:30000.0`. Test T7_PRO_dan_BASIT_e_geciste… (ikinci önizleme isteği 500 dönse bile) |
| DÜŞÜK | Tahsilat/Ödeme bu ekranda form değil | Değişmedi — **açıkça devredildi** (§ 19.7 madde 2): Tahsilat `#islem-git-tahsilat` → Müşteriler sayfası; Ödeme `#islem-git-odeme` pasif + gerekçe (DoD EK 1). Sadeleştirmesi Müşteriler (Cariler) sayfasının paketine aittir | — |
| DÜŞÜK | Öneri listesinde stok yok, fiyat "—" | Değişmedi — alan dışı (Api/Urun + Api/Fiyat toplu uç), § 19.7 madde 3 | — |
| DÜŞÜK | Sunucu varsayılan kasanın şubesini denetlemiyor | Değişmedi — güvenlik paketi (Mehmet), § 19.7 madde 4 | — |
| DÜŞÜK | `FisTakas.razor` satır içi style ×2 | `style="padding:.75rem 1rem"` → sınıf `takas-fark-govde`, `style="margin-top:.25rem"` → `takas-fark-toplam`; kurallar `tuval-satis.css` sonunda (aynı değerler, belirteçsiz sabit renk yok) | Canlı: `.takas-fark-govde` padding **12px 16px 12px 16px**, `.takas-fark-toplam` margin-top **4px** (önceki satır içi değerlerle aynı), `.islem-sayfa [style]` = **0**; denetim satır içi style 74 → **72** |

**Ölçüm sırasında bulunan ek kusur (düzeltildi):** fiyatı çıkmayan ürün seçilince açılan elle satırında BASİT 1280 `span.secici-metin "22 Ayar Burma …"` üç noktayla kesiliyordu (sikisma K10=1, K10b=1). Sebep: app.css `.fis-kalem-satiri` sütunları `minmax(10rem,1fr) auto auto auto` — Adet ve Birim fiyat kutuları doğal genişlikte (204 px) kalıp ürün seçicisini 163 px'e sıkıştırıyordu. `tuval-satis.css` (≥769 px, daha özgül seçici; app.css'e dokunulmadı): `minmax(12rem,2fr) minmax(5rem,.8fr) minmax(8rem,1.2fr) auto`, kutular sütuna sığar → ürün sütunu 286 px, ad `154/154` (kesik yok).

### 19.2 Gerçek tarayıcı — ölçüm tablosu
**(a) Fiyat sebebi + elle satırı** (liste pasif, "burma" → ok+Enter; sikisma K1..K13 + yatay):
| Mod | Tema | 1440 | 1280 | 1024 | 960 | 768 | 375 |
|---|---|---|---|---|---|---|---|
| BASİT | aydınlık | 0 | 0 (düzeltme öncesi K10=1) | 0 | 0 | 0 | 0 |
| BASİT | karanlık | 0 | 0 (düzeltme öncesi K10=1) | — | — | — | 0 |
| PRO | aydınlık | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | karanlık | 0 | 0 | — | — | — | 0 |
Her ölçümde: bant var, sarı tekrar satırı yok, yatay kaydırma yok, ürün adı kesilmez. Görüntüler (Read ile görüldü): `ham/TUVAL-KARSILASTIRMA/SATIS-3-t7-fiyat-sebep-{BASIT-aydinlik-1280, PRO-karanlik-1280, BASIT-aydinlik-375, PRO-aydinlik-1024, BASIT-karanlik-1440}.png`.

**(b) Senaryo matrisi (güncel ağaç, t6 betiği aynen):** 31 durum×mod × {1440,1280,1024,960,768,375 aydınlık + 1440,1280,375 karanlık} = **279 ölçüm** (`t7/m/olcum.jsonl`) + CSS düzeltmesinden sonra etkilenen 10 durum×mod tekrarı = **90 ölçüm** (`t7/m2`). Sonuç t6 ile birebir: tüm durumlar **0 kusur**; yalnız bilinen iki kalem: açık öneri listesinin alttaki başlıkla K9'u (bağlı açılır katman — KATMAN) ve 375 px karmada ödeme aracı/tutarın kasıtlı alt alta yığını (`bacakHiza` 78/157, K7 = 0). 1440 açılış boyu: tüm türler 1,0 (dolu karma 1,65–2,15; hurda/takas PRO 1,13/1,12).

### 19.3 Testler
- Yeni: 3 metot / 7 vaka (`SATIS3EkranTestleri.T7_Fiyati_cikmayan_oneride_SUNUCU_SEBEBI_yazar_gumus_metni_DEGIL_kalem_eklenince_bant_KALKAR`, `T7_Fiyat_sebebi_metni_sebep_koduna_gore` ×5, `T7_PRO_dan_BASIT_e_geciste_nakit_uyari_kutusu_0_TL_fiste_GIZLI_ve_sunucu_haddi_KORUNUR`). Sahte API'ye `tevsikHata` bayrağı eklendi.
- İlgili kümeler (SATIS3/SATIS2/TS1/TS2/Hurda/Islem/Fis/L1/KarmaOdeme/S24/S25/B4/AltinBacagi/Kasa/BasitMod/Takas/PosBarkod/PosTeklif/S21): **1445/1445** (`t7/kume2.log`).
- İlk tam süitte `PosBarkodTestleri.GUMUS_urunde_sessizce_gecilmez_fiyat_ISTENIR` kırmızıydı (bu paketin: yeni metin "…— birim fiyatı" küçük harfle başlıyordu, test "Birim fiyatı elle girip" arıyor). Metinler ". Birim fiyatı elle girip Ekle deyin." biçimine çevrildi (başkasının testi değiştirilmedi) → yeşil.

### 19.4 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t7/<kod>.orj` → perl tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T7-1 | `OneriSec`'te gümüş metnine geri dön | T7_Fiyati_cikmayan_oneride… | BİREBİR |
| M-S3-T7-2 | `KalemEkle` sonundaki `giris.BarkodHata = null` silindi | T7_Fiyati_cikmayan_oneride… (Assert.Empty — bant kaldı) | BİREBİR |
| M-S3-T7-3 | `ProYasalTazele`'de `tevsikHaddi = ph.Had` silindi | T7_PRO_dan_BASIT_e_geciste… | BİREBİR |
| M-S3-T7-4 | `LISTE_YOK` eşlemesi bozuldu | T7_Fiyat_sebebi_metni(LISTE_YOK), T7_Fiyati_cikmayan_oneride… | BİREBİR |
| M-S3-T7-5 | sarı tekrar satırı koşulu `true` | T7_Fiyati_cikmayan_oneride… | BİREBİR |
Son mutasyondan sonra temiz derleme: **0 Hata, 0 Uyarı** (`t7/build5.log`).

### 19.5 Tam süit ve denetim
- Son koşum (`trx-pSATIS-3/satis3-t7-son.trx`): **6784 · 6782 geçti · 2 kırmızı**, hiçbiri bu paketin:
  - `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` → `["Banka.razor@7783"]` → **ZO-ISLEM-A** (Banka.razor 20:37'de o şeritçe değişti).
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` → `OrtakGorunumMetinleri` 15 beklenen / 16 gerçek → **ORTAK-1** (t6 baz listesinde de var).
  - **Bu paketin yeni kırmızısı: 0.**
- `bash denetim.sh` → tek ✗ `P-39: TABLO_SARMALAYICISIZ 1` = aynı Banka.razor tablosu (K5 ile aynı sayaç) → **ZO-ISLEM-A**. Bu paketin sayaçları ✓: satır içi style 72 (74'ten düştü), SABIT_RENK 0, Pages ModalPencere 48/55, `Islemler.razor` 1393 satır < 1400.

### 19.6 Canlı doğrulama ve temizlik
- Uçlar (yanıt okundu): `GET /fiyat/teklif` (liste aktif) 200 `teklifVar:true, satisFiyati:95131.40, kaynak:"LISTE"` · (liste pasif, yalnız test kiracısında `fiyat_listeleri.aktif=0` ile) 200 `teklifVar:false, sebep:"LISTE_YOK", maden:"ALTIN"` · `GET /tevsik/gunluk?fisToplamTl=0` 200 `had:30000.0`. Sunucu kodu değişmedi; POST gövdeleri değişmedi.
- Durdurma: sürücü `/kapat`, vekil, API (yalnız 5162 süreci), matris Chrome'ları → `lsof` 5162/5163/9913/9914/9917/9918 **boş**. 5199/5200'e dokunulmadı.
- Silme: `kiraci-temizle.sh 01M3MJP7FYG0BWBMCZNMFZHRY0 --uygula` rc 0 → `tenant_id` kolonlu **70 tabloda 0 satır**, `tenants` bu id **0**. `tenants`: koşum başı 20 → (kiracım açıldığında başka bir şeridin silmesiyle yine 20) → son **18** (bu şeridin net etkisi 0; −2 başka şeritlerin kendi temizliği).

### 19.7 Açık kalanlar (sahibiyle)
1. **Layout/UstBar — sahipsiz (KATMAN ya da ORTAK'a atanmalı):** görünen üst bar h1 rota adıdır ("Satış"); seçili türü göstermesi için sayfanın başlığı devralması gerekir (`UstBarSekmeYuvasi` benzeri bir başlık yuvası ya da `RotaBasliklari`'na sayfa bildirimi). Bu şerit sayfa başı / form başlığı / `<title>`'ı düzeltti; **görünen ekranda koşul KARŞILANMADI** — tasarım hakemi #11 AÇIK.
2. **Müşteriler (Cariler) sayfasının paketi:** Tahsilat/Ödeme sadeleştirmesi (form orada; bu ekranda bağlantı + pasif gerekçeli düğme).
3. **Api/Urun + Api/Fiyat:** öneri listesinde stok ve dinamik fiyat (toplu teklif/stok ucu).
4. **Güvenlik (Mehmet):** `IslemDogrulayici.KasaDogrula` şube dışı kasaya yazmayı reddetmiyor (fail-closed kapı ayrı paket).
5. **KATMAN:** öneri listesi bağlı açılır katman (K9).
6. **ZO-ISLEM-A:** Banka.razor tablo sarmalayıcısı (K5 testi + denetim P-39). **ORTAK-1:** O8.

### 19.8 Değişen dosyalar (t7)
`Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (TR+EN 4 anahtar) · `Kuyumcu.Web/Pages/Islemler.Arama.cs` (`FiyatHesaplanamadiMetni`) · `Kuyumcu.Web/Pages/Islemler.razor` (barkod yolu aynı yardımcı; `KalemEkle` bandı temizler — 1393 satır) · `Kuyumcu.Web/Pages/Islemler.Pro.cs` (had saklanır) · `Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` (bant id'si, tekrar satırı) · `Kuyumcu.Web/Bilesenler/Fis/FisTakas.razor` (satır içi style → sınıf) · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (takas sınıfları, elle satırı sütunları) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs`. Yeni DOM id: `islem-barkod-hata`; mevcut id değişmedi. Migration: gerekmedi. `git status --porcelain | grep -v -E "/(bin|obj)/"` (paket dosyaları): ` M Pages/Islemler.razor`, `?? Sozlukler.Satis3.cs`, `?? Bilesenler/Fis/`, `?? Pages/Islemler.{Arama,Diger,Odeme,Pro,Sade,Yazdir}.cs`, `?? css/tuval-satis.css` (Kuyumcu.Tests dizini izlenmiyor).

### 19.9 DoD sözleşmesi (t7)
Yeni varlık/kayıt türü, uç ya da düğme yok. Değişen yalnız metin/görünüm ve istemci durumu; yasal kapılar, eşik sayıları (sunucudan), POST gövdeleri, id'ler DEĞİŞMEDİ. Mevzuat hükmü yazılmadı.

### 19.10 Paket kapısı (7 madde, t7)
| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | build 0 hata | ✓ | 0 Hata 0 Uyarı |
| 2 | bu paketin testleri 0 başarısız | ✓ | 1445/1445 |
| 3 | sayaçlar | ✓ (bu paketinkiler) | § 19.5 — tek ✗ ZO-ISLEM-A'nın |
| 4 | mutasyon | ✓ | 5/5 kırmızı, cmp BİREBİR |
| 5 | canlı doğrulama | ✓ | § 19.2 (279+90+18 ölçüm, görüntüler) + § 19.6 |
| 6 | test kiracısı silindi | ✓ | 70 tabloda 0 satır |
| 7 | yeni kırmızı yok | ✓ | § 19.5 |
Görünen başlık koşulu (#11) bu şeridin alanı dışında olduğu için AÇIK — paket kapısı maddesi değil, alan dışı açık kalan olarak yazıldı.

## 20. 28.09 t8 kapanış kontrolü (21:48–22:05) — DEVİR NOTU 4 gereği KOD YAZILMADI
Görev: ağacı raporla karşılaştır, derleme + SATIS-3 kümesi, raporu "doğrulamaya hazır" diye kapat. Tarayıcı ölçümü yapılmadı (t7'nin 387 ölçümü geçerli; bu koşumda ekrana dokunan değişiklik yok).

### 20.1 Ağaç ↔ rapor
- `git status --porcelain | grep -v -E "/(bin|obj)/"` paket dosyaları § 19.8 listesiyle aynı (` M Islemler.razor`, ` M KasaModulu.cs`, `?? Islemler.{Arama,Diger,Odeme,Pro,Sade,Yazdir}.cs`, `?? Bilesenler/Fis/`, `?? Katlanir.razor`, `?? AltinBacagi*`, `?? Sozlukler.Satis3.cs`, `?? css/{tuval-satis,katlanir}.css`).
- Yarım mutasyon YOK: `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t7/M-S3-T7-{1..5}.orj` hepsi güncel dosyayla `cmp` BİREBİR (Islemler.Arama.cs, Islemler.razor, Islemler.Pro.cs, FisKalemGirisi.razor).
- Proje ağacında `.mutorj/.tmp/.bak` artığı 0. 5162/5163 kapalı. 5198'de başka bir sürecin API'si (PID 46613) dinliyor — bu şeridin değil, dokunulmadı.

### 20.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`/private/tmp/kuyumcu-serit/SATIS-3/t8/build.log`).
- İlgili küme (t7 ile aynı filtre, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **1386 · 1383 geçti · 3 kırmızı** (küme 1445 → 1386: t7'den sonra başka şeritlerin test dosyaları değişti, eşleşme sayısı kaydı):
  1. `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` — bUnit `WaitForFailedException` (zaman aşımı); tek başına yeniden koşumda **YEŞİL** → yük kaynaklı kararsızlık.
  2. `Dogrulayici.ZOISLEMASinirPosTestleri.ZOS_pos_ayni_tahsilat_iki_satirla_net_tutari_asamaz` — MySQL duplicate key `pos_yatma_eslesmeleri` → **ZO-ISLEM-A** (dosya 21:40'ta o şeritçe değişti).
  3. **`Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` — BU PAKETİN, tek başına da KIRMIZI:** "u2'nin fiyatı çıkmadı ama kutuda önceki ürünün fiyatı duruyor: '10.000'". Test dosyası 21:35'te (t7 bitişinden SONRA) hasım doğrulayıcı tur 7 tarafından eklendi. Kök neden (okundu, düzeltilmedi): `Kuyumcu.Web/Pages/Islemler.Arama.cs:137` `if (giris.BirimFiyat is > 0m) takas.YeniFiyat = giris.BirimFiyat;` — fiyatı çıkmayan ikinci ürün seçilince önceki ürünün `YeniFiyat`'ı silinmez; Takas farkı yanlış ürün fiyatıyla hesaplanır ve Kaydet açık kalır (finansal hata). Tek satırlık düzeltme: fiyat yoksa `takas.YeniFiyat = null`. DEVİR NOTU 4 "KOD YAZMA" dediği için uygulanmadı → sonraki uygulayıcı turuna.
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 20.3 Durum
**YARIM (doğrulamaya hazır DEĞİL tek maddeyle):** paket kapısı madde 2 (bu paketin testleri 0 başarısız) hasım doğrulayıcının yeni Tur7 takas testiyle karşılanmıyor. Diğer maddeler t7 kanıtıyla geçerli (build ✓, denetim ✓, mutasyon 5/5, 387 tarayıcı ölçümü, test kiracısı silindi, yeni kırmızı yok — Tur7 hariç). Alan dışı açıklar § 19.7'deki gibi (Layout/UstBar başlığı, stok sütunu Api/Urun, K9 KATMAN, Tahsilat/Ödeme Müşteriler paketi, KasaDogrula güvenlik).

## 21. 28.09 t9 koşumu (22:00–22:20) — son doğrulamanın KRİTİK Takas bulgusu kapatıldı
Görev istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" dediği için (DEVİR NOTU 4'ün "kod yazma" hükmü § 20'deki t8 kapanışına aitti; § 20.3 bu tek maddeyi bir sonraki uygulayıcı turuna bırakmıştı) yalnız iki satırlık düzeltme yapıldı. Başka kod değişmedi. POST gövdeleri değişmedi, Kaydet() yolu değişmedi.

### 21.1 Değişiklikler
| Dosya | Değişiklik | Bulgu |
|---|---|---|
| `Kuyumcu.Web/Pages/Islemler.Arama.cs:138-139` (`TakasUrunAta`) | `if (giris.BirimFiyat is > 0m) takas.YeniFiyat = giris.BirimFiyat;` → `takas.YeniFiyat = giris.BirimFiyat is > 0m ? giris.BirimFiyat : null;` Fiyatı çıkmayan ürün seçilince önceki ürünün fiyatı taşınmıyor. Kutu boş kalıyor, fark hesaplanmıyor, Kaydet kilitli. | [KRİTİK] Tur7 takas fiyat taşıma |
| `Kuyumcu.Web/Bilesenler/Fis/FisModelleri.cs:260` (`TakasGirisi.Tamam`) | `&& Ayarlar.Bul(EskiAyar) is not null` eklendi. Bilinmeyen ayarda has 0 olur, bu durumda takas TAMAM sayılmıyor. | [DÜŞÜK] bilinmeyen EskiAyar |
| `Kuyumcu.Tests/SATIS3BilesenTestleri.cs` (sona) | Yeni test: `SATIS3_T9_takas_bilinmeyen_eski_ayar_TAMAM_degil` | DÜŞÜK bulgunun testi |

### 21.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Hata, 1 Uyarı**. Uyarı `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031`: başka şeridin dosyası, bu paketin değil. İlk deneme paralel şeridin `obj/.../Kuyumcu.Web.pdb` dosya kilidi (MSB4018) yüzünden rc 1 verdi. Yeniden denemede rc 0 alındı. Mutasyonlardan sonra temiz derleme rc 0 (`t9/build2.log`).
- `FullyQualifiedName~SATIS3` (`KUYUMCU_TEST_DB=kuyumcu_test_t2`): **183 / 183 geçti**. Bunlara `Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` (artık YEŞİL) ve yeni T9 testi dahil.
- Geniş küme (`SATIS3|Takas|Islem|Fis`): 698 test, 695 geçti, 3 kırmızı. Üçü de `ZOISLEMA4Testleri.ZO28e_esik_ustunde_ISO_DISI_uyruk_400` (ZO-ISLEM-A, dosya 20:43'te o şeritçe değişti). "Islem" alt dizesiyle kümeye girdi.
- **Tam süit** (`trx-pSATIS-3-t9`): **6756 · 6753 geçti · 3 kırmızı**. Hiçbiri bu paketin değil:
  1. `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` → **ORTAK-1**
  2. `ZOISLEMASubeTestleri.ZO_subesiz_mudur_IKI_aktif_subede_400_kalir_fail_closed` → **ZO-ISLEM-A**
  3. `ZOISLEMASubeTestleri.ZO_subesiz_mudur_HURDA_ALISI_tek_subede_o_subeye_iki_subede_400` → **ZO-ISLEM-A**

  Bu paketten gelen yeni kırmızı: **0**. Tur7 kırmızısı kapandı.
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 21.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t9/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | Geri alma |
|---|---|---|---|
| M-S3-T9-1 | `Islemler.Arama.cs`: üçlü ifade eski koşullu atamaya döndürüldü | `Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` (13 testte 12 geçti, 1 kırmızı) | cp + `cmp` BİREBİR |
| M-S3-T9-2 | `FisModelleri.cs`: `Ayarlar.Bul(EskiAyar) is not null` satırı silindi | `SATIS3BilesenTestleri.SATIS3_T9_takas_bilinmeyen_eski_ayar_TAMAM_degil` (13 testte 12 geçti, 1 kırmızı) | cp + `cmp` BİREBİR |

### 21.4 Gerçek tarayıcı — Takas ekranı yeniden ölçüldü (başsız Chrome + CDP 9913/9914, tarayıcı bölmesi KULLANILMADI)
Ortam: API 5162 (`--no-build`, sistem anahtarı bu koşum için üretildi, dosyası sonra boşaltıldı), vekil 5163. Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3MPNW2N0ZD6VEYFAQAEDMKB) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py` ile kuruldu (6+1 cari, 12 ürün, 12 satış, 2 kasa). Betik t7'nin `senaryo.mjs`'i, çıktı yolu t9. 2 durum × {BASİT, PRO} × {1440, 1280, 1024, 960, 768, 375 aydınlık + 1440, 1280, 375 karanlık} = **36 ölçüm** (`t9/m/olcum.jsonl`).

| Durum | Mod | Genişlik × tema | Yatay kaydırma | K1..K13 kusur | K4 (bilgi: boy kat) |
|---|---|---|---|---|---|
| acilis-TAKAS | BASİT | 1440/1280/1024/960/768/375 aydınlık · 1440/1280/375 karanlık | 0 | 0 | 1,0 (1440/1280) · 1,1–1,2 · 1,4 (375) |
| takas-iki-tarafli | BASİT | aynı 9 | 0 | 0 | 1,0 / 1,1 · 1,5–1,6 · 1,9 (375) |
| acilis-TAKAS | PRO | aynı 9 | 0 | 0 | 1,0 · 1,1 · 1,4 (375) |
| takas-iki-tarafli | PRO | aynı 9 | 0 | 0 | 1,1 / 1,3 · 1,4 · 1,9 (375) |

Görüntüler Read ile GÖRÜLDÜ:
- `t9/m/foto/SATIS-3-senaryo-takas-iki-tarafli-BASIT-aydinlik-1280.png`: 6 g 22 ayar, has fiyatı "Kurdan geldi". Yeni ürün 22 Ayar Burma Bilezik, birim fiyat 95.061,75 (sunucu teklifi). Fark "58.980,62 ₺ — Müşteri öder". Kaydet kilitli, sebep "Müşteri seçin".
- `t9/m/foto/SATIS-3-senaryo-takas-iki-tarafli-PRO-karanlik-375.png`: tek sütun yığın, yatay kaydırma yok. Fiyat 95.086,80, fark doğru, "Takas Kaydet" kilitli ("Cari seçin").

Fiyatı çıkan ürünün fiyatı canlıda hâlâ doğru doluyor (düzeltme olumlu yolu bozmadı). Fiyatı ÇIKMAYAN ürün canlıda ayrıca kurulmadı (liste pasifleştirme gerekir). Bu yol bUnit Tur7 testi ve M-S3-T9-1 mutasyonuyla kanıtlı.

### 21.5 Temizlik
- API (yalnız 5162 PID'i) ve vekil durduruldu. `lsof` 5162/5163/9913/9914 **boş**. 5199/5200/5198'e dokunulmadı.
- `kiraci-temizle.sh 01M3MPNW2N0ZD6VEYFAQAEDMKB --uygula` rc 0 döndü. `tenants` içinde bu id **0**, `PAKETSATIS-3%` **0**.
- `tenants`: koşum başı **20** → son **19**. Aradaki fark OLCEK-1 şeridinin kendi kiracısını (`PAKETOLCEK-1-TEST Dogrulama`) silmesinden geliyor. Bu şeridin net etkisi 0.
- **Kurucuya:** başsız Chrome profilleri `/private/tmp/kuyumcu-serit/SATIS-3/t9/profil-9913` ve `profil-9914` (113 MB + 113 MB) silinemedi. TurkReport hook `rm` komutunu blokluyor. Elle silinmeli. Proje ağacında `.mutorj/.orj/.bak/.tmp` artığı 0.

### 21.6 Durum
Paket kapısının 7 maddesi: (1) derleme 0 hata ✓ · (2) SATIS3 kümesi 183/183 ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon: t7'nin 5/5'i + t9'un 2/2'si ✓ · (5) canlı: Takas 36 ölçüm 0 kusur, önceki türler için t7'nin 387 ölçümü (bu koşumda yalnız Takas davranışı değişti) ✓ · (6) test kiracısı silindi ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

Alan dışı açıklar § 19.7'de sahipleriyle yazılı olduğu gibi duruyor: Layout/UstBar'daki görünen h1 hep "Satış" (KATMAN) · stok sütunu (Api/Urun) · K9 (KATMAN) · Tahsilat/Ödeme formları (Müşteriler paketi).

## 22. 29.09 t10 koşumu (28.09 23:00 – 29.09 00:10) — tasarım hakeminin yeni KRİTİK bulguları + doğrulayıcının DÜŞÜK bulguları
Görev istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" dediği ve listede 7 KRİTİK tasarım bulgusu olduğu için kod yazıldı (DEVİR NOTU 4'ün "kod yazma" hükmü t8'e aitti, § 20). POST gövdeleri, uç adları, mevcut DOM id'leri DEĞİŞMEDİ; migration yok; yeni uç/varlık yok. Eşik SAYISI koda gömülmedi.

### 22.1 Bulgu → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt (test · mutasyon · canlı) |
|---|---|---|---|
| K1 | Kayıttan sonra ödeme satırları + eski altın satıcı kimliği yeni fişe taşınıyor | Başarı yolunda Vazgeç'teki gibi `odemeTuru = "NAKIT"` + karma kapanır, satırlar silinir (`Islemler.razor` kayıt sonrası) | `T10_karma_KAYITTAN_SONRA_…`, `T10_tek_KART_odemeli_kayittan_sonra_…` · M-S3-T10-1, -13 · canlı `kayit-sonrasi-karma` BASİT ve PRO: `bacak=0 satici=false` |
| K2 | İade MASAK eşiğinde çıkmaz sokak, ham anahtar `masak.aciklama` | Sunucu MASAK reddi (alan `masak.*` ya da metinde MASAK) → `masakSunucudanGerekli` → MASAK formu açılır; gereksinim en az kimlik eşiğiyle sorulur (kümülatif red); iade tutarı `MasakTutar`'a girdi; `(masak.xxx)` ham anahtarı iki modda silinir; BASİT'te "Kimlik bilgileri gerekiyor — açılan alanları doldurun." | `T10_Iade_MASAK_reddi_…`, `T10_PRO_MASAK_reddi_…` · M-S3-T10-5, -11, -12 · canlı PRO kümülatif MASAK (23:43, `SATIS-3-t10-masak-kumulatif-PRO-aydinlik-1280.png`): form açıldı, Kaydet pasif + sebep, "185.000 ₺" |
| K3 | Kaydet istemcinin bildiği yasal engelde etkin; hurda/iade'de "Nakit — sistem belirledi" | `YasalKilitSebebi`: sunucunun bildirdiği had (`/tevsik/gunluk`) BİLİNİRKEN Peşin + tutar ≥ had → "Bu tutarda Peşin Müşteri olmaz — müşteri seçin."; tek ödeme Nakit + tutar ≥ had → "{had} ve üstünde nakit kullanılamaz — ödeme türünü kart ya da havale yapın." Tevsik tutarı türün kendi tutarı (hurda has×fiyat, iade orantılı tutar; sunucu kapısıyla aynı) → hurda/iade'de ödeme seçicisi BASİT'te de açılır. Had bilinmiyorsa istemci kilitlemez (sayı gömülmez, son söz sunucuda) | `T10_Pesin_…(BASIT/PRO)`, `T10_gercek_musteride_NAKIT_…`, `T10_sunucu_haddi_BILINMIYORSA_…`, `T10_Hurda_had_ustu_…` · M-S3-T10-3, -10 · canlı `satis-nakit` (Peşin, 94.7 bin) ve `hurda-dolu` (61 bin): Kaydet pasif + tek kısa sebep |
| K4 | Önceki müşterinin hata bandı müşteri değişince kalıyor | `MusteriDegisti`, kalem ekle/sil/toplam değişimi, ödeme türü ve ödeme satırı değişimi `hata = null` | `T10_onceki_musterinin_sunucu_reddi_…` · M-S3-T10-4 |
| K5 | BASİT'te teknik/yasal metin, büyük harf, "TL" | Sunucu mesajı BASİT'te tek kısa cümle (tevsik → had cümlesi, MASAK → kimlik cümlesi, md.234/gider pusulası → satıcı kimliği cümlesi, diğerlerinde parantez içi dayanak silinir); ham metin katlı "Bilgi" (`#hata-ayrinti`) içinde (Anayasa md.1: yutulmaz); PRO'da aynen. Sunucu metnindeki "127.937,79 TL"/"185.000 TL" → `Bicim.Tl`. Satıcı tipi BASİT'te "Vatandaş / Esnaf (muaf) / Vergi mükellefi (fatura keser)" (stopaj yok). Hurda kilit sebebi BASİT'te "(VUK md.234)"sız. BASİT hurda/iade'de "tevsik" jargonlu kırmızı satır görsel gizli | `T10_BASIT_sunucu_reddi_…`, `T10_PRO_sunucu_reddi_…`, `SATIS3_T10_sunucu_metnindeki_TL_…` · M-S3-T10-9 · canlı `hurda-dolu` BASİT 375 karanlık görüntüsü |
| K6 | Karma: yeni satır "Nakit · 0", 0 ₺ nakitte hata, fark üç yerde, '+ …' tam genişlik çubuk | Yeni satır mevcutta olmayan araçla (Nakit → Kart → Havale) + POS tekse otomatik; otomatik kalan 0'a inen satır düşer; 0 ₺ nakit kilitlemez; eski altının "Fark 0,00 TL" eksiği görsel gizli (fişin tek fark cümlesi söyler); "Kalanı ata", "+ Para üstü hesapla", "+ Taksit" küçük bağlantı | `T10_baska_odeme_…`, `SATIS3_T10_yeni_odeme_satiri_…`, `SATIS3_T10_otomatik_0_TL_…`, S21 nakit kilidi · M-S3-T10-2, -6, -7 |
| K7 | PRO karmada Kart satırı iki satıra dağılıyor (±46) | "+ Taksit" satırın altına (order 2, tam satır), tek POS açıklaması görsel gizli (id korunur) | canlı `karma-nakit-kart-altin` PRO 1440: Nakit/Kart satırı tek satır, sil aynı hizada (görüntü); `bacakHiza` 1440/1280 = 0 |
| K8 | Diğer formları farklı kalıpta, 'Henüz kayıt seçilmedi.', açıklama satırı, karışık etiket boyu | Yeni ortak `FisDigerEylem.razor`: sağda tek büyük birincil, eksik alanda PASİF + "Eksik: Ürün · Müşteri"; Rezerv/Konsinye/Kapora/Değişim/Evrak/Sipariş; seçici yer tutucusu "Ürün seçin"/"Satış seçin"; Değişim açıklaması katlı "Bilgi"de; form içi etiket tek ölçü | `SATIS3_T10_Rezerv_eksik_alanda_…` · M-S3-T10-8 · canlı `diger-rezerv/konsinye/degisim` + `kapora-dolu` PRO 0 kusur |
| D1 | Takas fark 0'a yuvarlanınca "— İade edilir" | Yuvarlanmış fark 0 ise yön metni yazılmaz | (görüntüde değişiklik yok; kod FisTakas) |
| D2 | Kullanılmış davet satırları | `kiraci_davetleri` `PAKETSATIS-3%` 9 satır silindi (7 eski + t9 + t10) → 0 | SQL § 22.6 |

Kapanmayan/alan dışı: görünen üst bar h1 hep "Satış" (Layout/UstBar — KATMAN/ORTAK); "Ödeme" türü (tedarikçiye ödeme ucu yok — Müşteriler/Cari paketi); öneri listesinde stok sütunu (Api/Urun); `TevsikHaddi => tevsikHaddi ?? 30_000m` istemci yedeği (paketten önce vardı; yeni kilit bu yedeği KULLANMAZ, yalnız sunucu değeriyle çalışır — yedeğin kaldırılması ayrı paket); BASİT "Değişim" karosu = Takas (tuval adı) ile Diğer'deki "Ürün değişimi" ad benzerliği — tuval karo metni birebir kuralı gereği dokunulmadı, menü adı KATMAN'ın `FisDigerIslemler`'inde.

### 22.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Hata** (son: 0 Uyarı; ara derlemelerde tek uyarı `EtiketEkranTestleri.cs(145) xUnit1031` başka şeridin).
- Yeni testler **16** (SATIS3EkranTestleri 12 · SATIS3BilesenTestleri 3 · S26DigerIslemlerTestleri 1). `FullyQualifiedName~T10` **29/29** (16 yöntem, teori satırlarıyla; filtre S26/S21 T10 adlarını da kapsar).
- Geniş küme (SATIS3|S26|P10|B4|S24|TS1|TS2|S21|Islem|Fis|UX4|Takas|S2|L1|T10|OL18): **1309 · 1308 geçti · 1 kırmızı** = `ZOISLEMAEkranTestleri.ZO28n_pos_eslesmis_satirda_GERI_AL_…` — ZO-ISLEM-A (dosyaları 23:58/00:02'de o şeritçe değişiyor, tek başına da kırmızı; bu paketin dosyası değil).
- Mevcut testlerde tasarım gereği değişen beklentiler (gerekçeli): L1Tek/L1b fikstür haddi 30.000 → 50.000 (40.000 ₺'lik fiş artık istemcide kilitlenir; testin amacı sunucu reddi yolu — korunur); S21 nakit kilidi testine TutarTl eklendi + "0 ₺ nakit kilitlemez"; S22 ikinci satır aracı NAKIT → KART; S24 fark kilidi cümlesi "Eski altın: Fark 0,00 TL" → `Satis3.FarkFazla`; SATIS3 hurda adres testi had üstünde havaleye geçer.
- **Tam süit** (`trx-pSATIS-3-t10`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **6813 · 6810 geçti · 3 kırmızı**: (1) `OlcekTekTestleri.OL18_…` — OLCEK-1 (olcek.css ve test dosyası koşum sırasında o şeritçe değişti; yeniden koşumda YEŞİL); (2) `OrtakGorunumSaflik.O8_…` — ORTAK-1 (bilinen); (3) `SATIS3EkranTestleri.T10_tek_KART_…` — BU PAKETİN, yük altında yarış (POST görülünce sıfırlama bitmeden devam ediyordu) → test başarı bandını ve boş fişi bekleyecek şekilde düzeltildi, tekrar koşumda YEŞİL (geniş küme içinde). **Bu paketten kalan yeni kırmızı: 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 22.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t10/<kod>.orj` → tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T10-1 | kayıt sonrası `TekOdemeyeDon` silindi | T10_karma_KAYITTAN_SONRA_odeme_satirlari_ve_eski_altin_saticisi_SIFIRLANIR | BİREBİR |
| M-S3-T10-2 | yeni satır aracı hep NAKIT | T10_baska_odeme_ikinci_satir_KART_acilir_… | BİREBİR |
| M-S3-T10-3 | `Gonderilebilir`'den yasal kilit çıkarıldı | T10_Pesin_…(BASIT), T10_Pesin_…(PROFESYONEL), T10_gercek_musteride_NAKIT_… | BİREBİR |
| M-S3-T10-4 | müşteri değişince `hata = null` silindi | T10_onceki_musterinin_sunucu_reddi_MUSTERI_DEGISINCE_silinir | BİREBİR |
| M-S3-T10-5 | MASAK reddi bayrağı silindi | T10_Iade_MASAK_reddi_MASAK_alanlarini_ACAR_ham_anahtar_BASILMAZ | BİREBİR |
| M-S3-T10-6 | 0 ₺ satır düşürme çağrısı silindi | T10_baska_odeme_ikinci_satir_… | BİREBİR |
| M-S3-T10-7 | nakit kilidinde `TutarTl > 0` silindi | S21FisSafTestleri.FisHesap_nakit_kilidi_yalniz_NAKIT_bacakta_ve_sunucu_bayragiyla | BİREBİR |
| M-S3-T10-8 | Diğer eylem düğmesi eksikte pasif değil | S26DigerIslemlerTestleri.SATIS3_T10_Rezerv_eksik_alanda_… | BİREBİR |
| M-S3-T10-9 | BASİT sade mesaj dalı kapatıldı | T10_BASIT_sunucu_reddi_KISA_cumle_… | BİREBİR |
| M-S3-T10-10 | hurda tevsik tutarı kalem toplamı | T10_Hurda_had_ustu_NAKITTE_kilit_sebebi_haddi_soyler | BİREBİR |
| M-S3-T10-11 | MASAK gereksinim sorgusu tek işlem tutarıyla | T10_PRO_MASAK_reddi_ham_anahtar_BASILMAZ_alanlar_ACILIR | BİREBİR |
| M-S3-T10-12 | ham `(masak.x)` anahtar sökme silindi | T10_PRO_MASAK_reddi_… | BİREBİR |
| M-S3-T10-13 | kayıt sonrası `odemeTuru = "NAKIT"` silindi | T10_tek_KART_odemeli_kayittan_sonra_odeme_NAKIT_varsayilanina_doner | BİREBİR |
13/13 kırmızı; son mutasyondan sonra temiz derleme 0 hata.

### 22.4 Gerçek tarayıcı (başsız Chrome + CDP 9913/9914, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3MV9E40HF3BP5R1GAH17P82) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py` (6+1 cari, 12 ürün, 12 satış, 2 kasa). İlk matris koşumu BASİT ve PRO aynı kullanıcıyla EŞZAMANLI koştuğu için mod tercihi sunucuda karıştı (BASİT dosyaları PRO yüz gösterdi — görüntüde görüldü) → atıldı, ardışık yeniden koşuldu. Genişlik×tema: 1440 açık · 1280 açık · 375 açık · 1280 koyu · 375 koyu. **110 ölçüm** (`t10/m/olcum.jsonl`, her durum için son ölçüm):

| Durum | Mod | n | yatay kaydırma | K1..K13 | boy 1440 |
|---|---|---|---|---|---|
| acilis-SATIS · satis-nakit · hurda-dolu · iade-kaynak-secili · takas-iki-tarafli · acilis-HURDA_ALIS | BASİT | 6×5 | 0 | 0 | 1,0 · 1,0 · 1,02 · 1,0 · 1,0 · 1,0 |
| aynı + kapora-dolu · diger-rezerv · diger-konsinye · diger-degisim | PRO | 10×5 | 0 | 0 | 1,0 … 1,21 (hurda) |
| karma-kart-altin · karma-nakit-kart-altin | BASİT/PRO | 4×5 | 0 | 0 (1440/1280 satır hizası 0) | 1,68–2,15 (eski altın paneli açık) |
| kayit-sonrasi-karma | BASİT/PRO | 2×5 | 0 | K9 5'er — kayıttan sonra odak arama kutusuna döner ve boş odak listesi açık; liste alttaki boş-fiş metninin üstüne biner (bilinen bağlı açılır katman kusuru, KATMAN) | 1,0 |
375 px karma satırlarında `bacakHiza` 78/157 px: ≤600 px'te araç · tutar · kasa bilerek alt alta (t3 (q) kuralı) — aynı görsel satır değil, K7 = 0.
Görüntüler Read ile GÖRÜLDÜ ve `ham/TUVAL-KARSILASTIRMA/SATIS-3-t10-*.png` (15 dosya) olarak kondu: PRO 1440 karma (Nakit/Kart satırı tek satır, sil hizalı, eski altın satıcısı "Vatandaş" değil PRO etiketi), BASİT 1280/375 hurda (ödeme türü seçicisi açık, "Vatandaş", Kaydet pasif + "30.000 ₺ ve üstünde nakit kullanılamaz — …", tevsik jargon satırı yok), Diğer→Ürün ayır PRO 1280 ("Ürün seçin", sağda pasif "Ayır" + "Eksik: Ürün · Müşteri"), kayıt sonrası (bacak 0, satıcı alanı yok), PRO kümülatif MASAK (form açık, Kaydet pasif). Not: kümülatif MASAK görüntüsü ham anahtar sökme düzeltmesinden ÖNCEKİ bundle'la alındı ("(masak.aciklama)" görünüyor); düzeltme sonrası aynı durum canlıda yeniden üretilemedi (test ürününün stoku bitti) — bUnit + M-S3-T10-12 ile kanıtlı.

### 22.5 DoD sözleşmesi (t10)
Yeni varlık/uç yok. Yeni bileşen `FisDigerEylem` yalnız görünüm (istek atmaz, formun yöntemini çağırır). Tüm düğmeler ya istek atar ya pasif + sebep (UX-S30). Mevzuat hükmü yazılmadı: kilitler sunucunun yapılandırmasından gelen had/eşikle çalışır, bilinmiyorsa istemci kilitlemez (sunucu fail-closed kapısı değişmedi).

### 22.6 Temizlik
API/vekil/Chrome durduruldu; `lsof` 5162/5163/9913/9914 **boş**; 5198/5199/5200'e dokunulmadı. Sistem anahtarı dosyası boşaltıldı. `kiraci-temizle.sh 01M3MV9E40HF3BP5R1GAH17P82 --uygula` rc 0 → tenant_id kolonlu tüm tablolarda **0 satır**; `DELETE FROM kiraci_davetleri WHERE isletme_adi LIKE 'PAKETSATIS-3%'` → 9 satır, kalan 0. `tenants` **17 → 17**. Kurucuya: `t10/profil-9913`, `profil-9914` Chrome profilleri hook `rm`'i blokladığı için silinemedi (elle silinmeli). Proje ağacında `.mutorj/.orj/.bak/.tmp` artığı 0.

### 22.7 Değişen dosyalar (t10)
`Kuyumcu.Web/Pages/Islemler.razor` (1398 satır) · `Islemler.Odeme.cs` · `Bilesenler/Fis/{FisFark.cs, FisModelleri.cs, FisOdemeBacaklari.razor, FisParaUstu.razor, FisAltinBacagi.cs, FisOdemeKasa.razor, FisHurdaAlis.razor, FisTakas.razor, FisDigerRezerv/Konsinye/Avans/Degisim/Evrak/Siparis.razor, FisDigerEylem.razor (YENİ)}` · `Bilesenler/AltinBacagiPaneli.razor` · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (TR+EN 13 anahtar) · `wwwroot/css/tuval-satis.css` · testler `SATIS3EkranTestleri.cs`, `SATIS3BilesenTestleri.cs`, `S26DigerIslemlerTestleri.cs`, `L1TekFisEkranTestleri.cs`, `L1bAlisFisiEkranTestleri.cs`, `S21FisBilesenTestleri.cs`, `S22TezgahFisiTestleri.cs`, `S24EskiAltinEkranTestleri.cs`. Yeni DOM id: `hata-ayrinti`, `degisim-bilgi`, `<düğme>-kilit` (rezerv-kaydet-kilit vb.); mevcut id değişmedi. Migration: gerekmedi.

### 22.8 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) bu paketin testleri 0 başarısız ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon 13/13 ✓ · (5) canlı 110 ölçüm + görüntüler ✓ · (6) test kiracısı ve davet satırları silindi ✓ · (7) tam süitte bu paketten kalan yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.** Alan dışı açıklar § 22.1 sonunda sahipleriyle.

## 23. 29.09 t11 koşumu (00:50 – 01:35) — tasarım hakeminin 4 YÜKSEK bulgusu (kapanış koşumu wf_d5097c52-3e6)
Görev istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" dediği için kod yazıldı (DEVİR NOTU 4'ün "kod yazma" hükmü t8'e aitti, § 20). POST gövdeleri, uçlar, mevcut DOM id'leri DEĞİŞMEDİ; migration yok; yeni varlık/uç yok; eşik SAYISI koda gömülmedi (had yine sunucunun `/tevsik/gunluk` değeri).

### 23.1 Bulgu → yapılan → kanıt
| # | Bulgu (hakem) | Yapılan | Kanıt |
|---|---|---|---|
| Y1 | Nakit kuralında çelişkili yönlendirme ("program uyarır; kalanı kartla alırsın" ↔ kilit "nakit alınamaz") | Not metni sunucunun işlem-tutarı kuralıyla AYNI cümle: "{had} ve üstündeki fişte nakit alınamaz — tamamını kart ya da havaleyle alın." (sayısız sürüm + EN). Bant: "Bu tutarda nakit alınamaz — tamamını kart ya da havaleyle alın." "program uyarır" hiçbir metinde yok. `FisNakitUyari.BantVar`: kural bu fişte tetiklendi ve ödemede nakit varken not GİZLİ (ekranda tek bant); not artık fiş toplamına (TevsikTutar) bakar, nakit payına değil. Sunucu reddi de aynı kuralı söylüyor (canlı: "ödemenin BİR KISMININ … " 400) — mevzuat hükmü yazılmadı, yalnız metin sunucu kuralına hizalandı | `T11_nakit_notu_kuralla_AYNI_cumle_…` · M-S3-T11-1 · canlı BASİT+PRO `karma-kart-nakit-esik` (Kart 100.000 + Nakit 89.744,70, fiş 189.744,70): `not: GIZLI`, tek bant, Kaydet PASİF aynı cümle, `program uyarır` = false |
| Y2 | "+ Taksit" kırık (PRO tam genişlik şerit, BASİT havada hap; K7 ±36/±42; ✕ 60 px kayık) | "+ Taksit" satırın İÇİNDEN çıktı → satırın altında kendi `.bacak-ek-satiri`'nde, ödeme aracı kutusuyla aynı sol hiza, kenarlıklı ikincil düğme (`btn-ikincil`); eski `order`/tam satır kuralları silindi | `T11_TAKSIT_baglantisi_satirin_ALTINDA_…` · M-S3-T11-2 · canlı `kart-taksit` BASİT/PRO: `satirIci:false, solFark:0, silXfark:0` (✕ iki satırda aynı sütun), 60 ölçümde K7 yalnız MASAK formu (aşağı) |
| Y3 | "Ödeme" karosu/sekmesi hep pasif, gerekçe yalnız fareyle | Tedarikçiye ödeme ucu sunucuda YOK (Cari modülünde yalnız `/{id}/tahsilat`; `CariModulu.cs` HttpPost listesi) → form açılamaz; hakemin seçeneğiyle karo (BASİT) ve sekme (PRO) KALDIRILDI (`BasitPos.Karolar`/`Kisayollar`, `FisTurleri.ProSekmeler`). 7 karoda son karo (İndirim) iki sütunu kaplar. GEREKÇELİ TUVAL FARKI; ödeme ucu gelince geri eklenir (Müşteriler/Cari paketi) | `T11_ODEME_karosu_ve_sekmesi_YOK_…` (BASİT+PRO) · M-S3-T11-3 · güncellenen S22/TS1/TS2/SATIS2 testleri · görüntü `SATIS-3-t11-acilis-SATIS-*`, `…kart-taksit-PRO-karanlik-1280` |
| Y4 | İade bandı ters yön ("… alın"), müşteri ayrıca soruluyor, "Kalan iade hakkı" değeri etiketten kopuk | `NakitBantMetni`: SATIS_IADE → "Bu tutarda nakit iade edilemez — kart ya da havaleyle geri ödeyin."; HURDA_ALIS/ALIS → "… nakit ödenemez — kart ya da havaleyle ödeyin."; diğerleri "alın" (sunucu sade mesajı da aynı kaynaktan). Kaynak fişin müşterisi varsa müşteri seçicisi ÇİZİLMEZ; FisIade içinde salt okunur "Müşteri — İade edilen fişten" (`#islem-iade-musteri`); kaynak seçilmeden Kaydet sebebi "İade edilecek işlemi seçin." Kalan hakkı ve Orantılı tutar `SaltOkunur` alan kabuğunda (`#iade-kalan`, `#iade-orantili` yeni id) | `T11_Iade_nakit_bandi_GERI_ODEME_yonunde_…`, `Iade_KAYNAK_secilmeden_Kaydet_KILITLI` · M-S3-T11-4/5/6 · canlı `iade-esik` (94.890,45 ₺ kart satışı iadesi) BASİT: bant "nakit iade edilemez — … geri ödeyin", `alinYonu:false`, `cariSecici:false`; `iade-detay` BASİT/PRO: `kalanSolEtiketSol:0`, `ustFark:[0,0]` |
| ek | Canlı ölçümde yeni: 375 px uzun müşteri adı bakiye rozetinin altında (K9) | ≤600 px'te rozet kutunun altına iner (tuval-satis.css) | yeniden ölçüm 375 açık/koyu K9 = 0, görüntü Read ile görüldü |

### 23.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (son mutasyondan sonra temiz derleme).
- Yeni testler **4 yöntem / 5 koşum** (SATIS3EkranTestleri T11_*); gerekçeli güncellenen: S22 (2), TS1 (2), TS2 (2), SATIS2 (1), TS1Sinir (1, sabit metin yerine sözlük+Bicim), S25_E9 (müşteri kaynaktan), SATIS3 `Iade_KAYNAK_secilmeden…` (sebep + kilitli müşteri).
- Geniş küme (SATIS3|SATIS2|S22|TS1|TS2|P10|KarmaOdeme|Iade|S21|S26|L1|B4|S25|Islem|Fis|UX4|Takas|S24|T10): **1428/1428**.
- **Tam süit** (`trx-pSATIS-3-t11`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **6853 · 6852 geçti · 1 kırmızı** = `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1, devir notunda bilinen; bu paketten yeni kırmızı **0**).
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0). `Islemler.razor` 1399 satır (≤1400 kuralı).

### 23.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t11/<kod>.orj` → tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T11-1 | `Gorunur => Yakin` (BantVar yok sayıldı) | T11_nakit_notu_kuralla_AYNI_cumle_… | BİREBİR |
| M-S3-T11-2 | taksit sarmalayıcısı `bacak-satiri` | T11_TAKSIT_baglantisi_satirin_ALTINDA_… | BİREBİR |
| M-S3-T11-3 | `("ODEME", null)` kısayolu geri | S22 Basit_izgara…, S22 BasitPos_izgara…, T11_ODEME_…(BASIT) | BİREBİR |
| M-S3-T11-4 | iade metni `NakitAlinamaz` | T11_Iade_nakit_bandi_GERI_ODEME_… | BİREBİR |
| M-S3-T11-5 | iadede müşteri seçicisi hep çizilir | T11_Iade_…, Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
| M-S3-T11-6 | kaynak-seçin kilit sebebi silindi | Iade_KAYNAK_secilmeden_Kaydet_KILITLI | BİREBİR |
6/6 kırmızı.

### 23.4 Gerçek tarayıcı (başsız Chrome CDP 9913, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3N1AGHYVAE6Y9AE4DBBEWP9): `demo-tohumla.js --prova-kiraci` + `ek-tohum.py` (+ "Anadolu Holding A.Ş.", 2. kasa, 1 POS cihazı) + ölçüm için 1 kartlı 94.890,45 ₺ satış. Durumlar × {BASİT, PRO} × {1440 açık, 1280 açık, 375 açık, 1280 koyu, 375 koyu} = **70 ölçüm**, yatay kaydırma **0** (hepsi).
| Durum | BASİT K-liste | PRO K-liste |
|---|---|---|
| acilis-SATIS | 0 | 0 |
| karma-kart-nakit-esik | 1440/1280: K7 1 (MASAK formu "Doğum Tarihi" ±8) · 375: K3 2 (ürün adı iki satır, kelime bölünmez), K9 0 (düzeltme sonrası) | 1440/1280: K7 1 (aynı MASAK), 1440: K10 1 (uzun müşteri adı PRO kutuda üç nokta) |
| kart-taksit · iade-detay · iade-esik · karma-nakit-kart-altin | 0 | 0 |
(K2/K4 bilgi alanları — kart sağ yüzde / ekran boyu — kusur sayılmaz.) Görüntüler Read ile GÖRÜLDÜ (BASİT 1280 karma, BASİT 1440 iade-detay, BASİT 1440 iade-eşik, BASİT 375 karma, PRO koyu 1280 kart-taksit) ve 50 dosya `ham/TUVAL-KARSILASTIRMA/SATIS-3-t11-*.png`.

### 23.5 Temizlik
API/vekil/Chrome durduruldu: `lsof` 5162/5163/9913/9914 **boş**; 5198/5199/5200'e dokunulmadı. Anahtar ve fikstür dosyaları `printf ''` ile boşaltıldı (0 bayt). `kiraci-temizle.sh 01M3N1AGHYVAE6Y9AE4DBBEWP9 --uygula` rc 0 → tenant_id kolonlu tüm tablolarda **0 satır**; `kiraci_davetleri` `PAKETSATIS-3%` 2 satır silindi. `tenants` başlangıç **17** → son **16**: bizim kiracımız silindi; eksilen bir kiracı `01M3MY5ATK2W6XRG1ENHWA1RZ0 PAKETZO-BELGE-TEST` — ZO-BELGE şeridinin kendi temizliği (bu şerit ona dokunmadı). `t11/profil-9913` ve `t11/m` (ölçüm/görüntü) `/private/tmp/kuyumcu-serit/SATIS-3/t11` altında; hook `rm`'yi engellediği için silme saatlik `serit-temizlik.sh`'a / kurucuya kalır.

### 23.6 Açık kalanlar (alan dışı, sahibiyle)
- MASAK kimlik formunda "Doğum Tarihi" kutusu yan kutulardan 8 px aşağıda (K7) — `Bilesenler/MasakFormu.razor` (bu şeridin alanı dışında; ortak bileşen) → ORTAK/MASAK sahibi.
- PRO 1440'ta çok uzun müşteri adı seçici kutusunda üç noktayla kısalıyor (K10; tam ad title'da) — bilinçli PRO kuralı (rozet %46), gerekirse KATMAN/ORTAK.
- Tedarikçiye ödeme ucu yok → "Ödeme" türü kaldırıldı; uç eklenince (Müşteriler/Cari paketi) karo/sekme geri gelir.
- Önceki açıklar (görünen h1 "Satış" — KATMAN; öneri listesinde stok sütunu — Api/Urun; K9 bağlı açılır katman — KATMAN; tam süitte O8 — ORTAK-1) değişmedi.

### 23.7 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) bu paketin testleri 0 başarısız ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon 6/6 ✓ · (5) canlı 70 ölçüm + görüntüler ✓ · (6) test kiracısı silindi ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 24. 29.09 t12 koşumu (01:40 – 02:05) — doğrulayıcının 2 KRİTİK tevsik bulgusu (eşitlik sınırı + "ve üstü" metni)
Görev istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" dediği için kod yazıldı (t11 ile aynı yorum; DEVİR NOTU 4'ün "kod yazma" hükmü t8'e aitti). POST gövdeleri, uçlar, DOM id'leri DEĞİŞMEDİ; migration yok; eşik SAYISI koda gömülmedi (had sunucunun `/tevsik/gunluk` değeri).

### 24.1 Bulgu → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| K1 (KRİTİK) | İstemci fiş TAM hadde iken Kaydet'i kilitliyordu (`>=`); sunucu `TevsikKurali`/`TevsikKapisi.cs:55` `olcut > Had`, `MasakKapisi.cs:189` `tutar > tevsik.Had` | `TevsikOneri.Gerekli`: `toplamTl >= had` → `toplamTl > had` (öneri, nakit bandı, `OdemeKapisiAcik` ve kilit AYNI yüklemden beslenir — sunucuyla tek kural); `Islemler.Odeme.cs YasalKilitSebebi`: `PesinSecili && TevsikTutar >= h` → `> h` | Doğrulayıcının `SATIS3TevsikEsitlikSinirTestleri` 3/3 YEŞİL (eşitlikte gerçek müşteri+NAKIT ve Peşin KİLİTLENMEZ; had=9.999,99'da kilitli). `UX4dSafTestleri.M5_Nakit_tevsik_haddini_asinca…` girdisi 30.000,01'e çekildi (aşan), yeni `M5_TAM_hadde_nakit_SERBEST_asan_degil` |
| K2 (KRİTİK) | Metin mevzuata aykırı: "{0} ve üstünde / ve üstündeki" (EN "or more") | `Sozlukler.Satis3.cs` NakitHaddiKilit TR "{0} üstündeki fişte nakit kullanılamaz — …", EN "Cash cannot be used above {0} — …"; `Sozlukler.SatisTuval.cs` NakitUyari TR "{0} üstündeki fişte nakit alınamaz — …", EN "No cash on a receipt above {0} — …"; `FisNakitUyari.razor` açıklama yorumu aynı | Yeni `SATIS3BilesenTestleri.T12_nakit_hadd_metinleri_ASAN_anlaminda_ve_ustu_DEMEZ_iki_dil` (TR'de "ve üstü" YOK + "{0} üstündeki" VAR; EN'de "or more" YOK + "above {0}" VAR). Canlı: `veUstu:false, ustundeki:true` |
| D1 (DÜŞÜK) | ODEME karosu/sekmesi kaldırıldı, `#islem-git-odeme` DOM'da yok | Kod değişmedi (t11 kararı). GEREKÇELİ TUVAL FARKI: sunucuda tedarikçiye ödeme ucu yok (`CariModulu.cs` HttpPost listesinde yalnız `/{id}/tahsilat`), karo hep pasifti. **Orkestratöre iş kuyruğu maddesi:** "Tedarikçiye ödeme ucu (Cari/Api) gelince BASİT karo + PRO sekme `ODEME` ve `#islem-git-odeme` geri eklenir." | § 23.1 Y3 |
| D2 (DÜŞÜK) | `TevsikHaddi => tevsikHaddi ?? 30_000m` istemci yedeği (`Islemler.razor:503`) | Paketten ÖNCE vardı; doğrulayıcının önerisiyle AYRI PAKET (sayı istemciye gömülmez kuralı). Bu koşumda dokunulmadı; `YasalKilitSebebi` yalnız sunucu değeriyle çalışır | — |
| D3 (DÜŞÜK) | Sunucu reddi dize eşleştirmesiyle sınıflanıyor | AYRI PAKET (Api hata gövdesine makine okunur `kod` alanı). Bu şeridin alanı dışı | — |

Birincil kaynak (doğrulayıcının verdiği, rapora aynen): Resmî Gazete 24.12.2015/29572, 459 sıra no. VUK Genel Tebliği §4.1 — "…TL'yi aşan tutardaki her türlü tahsilat ve ödemelerini…". Metin ve istemci kilidi artık bu hükümle ve sunucu kuralıyla (`> Had`) aynı; yeni mevzuat hükmü YAZILMADI.

### 24.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (son mutasyondan sonra temiz derleme; mutasyondan önceki ilk derlemedeki 1 uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası, temiz derlemede görünmedi).
- Filtreli: `SATIS3TevsikEsitlik|M5_|T12_` **9/9**; `SATIS3TevsikEsitlik|UX4dSaf|SATIS3Bilesen` **53/53**.
- **Tam süit** (`trx-pSATIS-3-t12`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **6869 · 6867 geçti · 2 kırmızı**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1, devir notunda bilinen) ve `S34RaporBugunTestleri.S34_10_Gun_sonu_FARK_VAR_ise_…` (beklenen "FARK = kasaya giren para − satış faturala…", gelen "Kasaya giren para ile kesilen faturalar t…" — `Raporlar.razor`/`RaporModulu.cs` metni, ZO-BELGE şeridi; bu koşum o dosyalara dokunmadı). **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 24.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t12/<kod>.orj` → perl tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T12-1 | `TevsikOneri`: `toplamTl > had` → `>=` | M5_TAM_hadde_nakit_SERBEST_asan_degil · Esitlik_had_10000_gercek_musteri_NAKIT_… · Esitlik_had_10000_Pesin_Musteri_… | BİREBİR |
| M-S3-T12-2 | `YasalKilitSebebi`: `TevsikTutar > h` → `>=` | Esitlik_had_10000_Pesin_Musteri_fis_TAM_10000_KILITLENMEZ_… | BİREBİR |
| M-S3-T12-3 | NakitHaddiKilit TR metni "ve üstünde"ye geri | T12_nakit_hadd_metinleri_ASAN_anlaminda_ve_ustu_DEMEZ_iki_dil | BİREBİR |
3/3 kırmızı.

### 24.4 Gerçek tarayıcı (başsız Chrome CDP 9913, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3N3CH8XXB1EZX5V1F52ZYSJ) `demo-tohumla.js --prova-kiraci --ad …` + `ek-tohum.py`. (İlk tohum denemesi kur 40 sn'de gelmediği için yarıda kaldı ve betiğin varsayılan adıyla `PAKET-P-30-TEST Kuyumculuk` (01M3N384E48D79ESFNRNVGHH2V, oluşturma 22:47:56Z = bu koşum) bıraktı; `kiraci-temizle.sh --uygula` ile silindi, adı doğru verilerek yeniden kuruldu.)
3 durum × {BASİT, PRO} × {1440 açık, 1280 açık, 375 açık, 1280 koyu, 375 koyu} = **30 ölçüm**, yatay kaydırma **0** (hepsi).
| Durum | BASİT | PRO | Metin/kilit |
|---|---|---|---|
| nakit-esik-ustu (2× burma ≈190.000 ₺, Anadolu, Nakit) | 1440/1280: K7 1 (MASAK formu "Doğum Tarihi" ±8 — § 23.6 alan dışı) · 375: K3 2 (ürün adı iki satır, kelime bölünmez) | 1440/1280: K7 1 (aynı MASAK) · 1440: K10 1 (uzun müşteri adı, § 23.6) | Kaydet PASİF, sebep "30.000 ₺ üstündeki fişte nakit kullanılamaz — ödeme türünü kart ya da havale yapın." · sayfada "ve üstü" **yok** |
| karma-kart-nakit-esik | aynı profil | aynı profil | tek bant "Bu tutarda nakit alınamaz — …", not GİZLİ, "program uyarır" yok |
| hurda-dolu | 0 (K2/K4 bilgi) | 0 | — |
(K2/K4 bilgi alanlarıdır.) Görüntüler Read ile GÖRÜLDÜ (BASİT açık 1280 nakit-eşik-üstü: alt satırda kilit sebebi "30.000 ₺ üstündeki fişte…"; PRO koyu 1280: aynı sebep sağ altta, Yasal kapılar "Aşıldı"). 12 dosya `ham/TUVAL-KARSILASTIRMA/SATIS-3-t12-*.png`. Eşitlik (fiş = had TAM) canlıda üretilemez (fiyat kurdan gelir, tam tutar elde edilemez) → bUnit `SATIS3TevsikEsitlikSinirTestleri` ile kanıtlı.

### 24.5 Temizlik
API/vekil/Chrome durduruldu: `lsof` 5162/5163/9913/9914 **boş**; 5198/5199/5200'e dokunulmadı. Sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı (0 bayt). `kiraci-temizle.sh 01M3N3CH8XXB1EZX5V1F52ZYSJ --uygula` → 361 satır + tenants 1 satır silindi; `kiraci_davetleri` bu koşumun davetleri silindi. `tenants` başlangıç **17** → son **17**. Proje ağacında `.mutorj/.orj/.bak/.tmp` artığı 0. `t12/profil-9913` ve `t12/m` `/private/tmp/kuyumcu-serit/SATIS-3/t12` altında (hook `rm`'yi engeller → saatlik temizlik/kurucu).

### 24.6 Değişen dosyalar (t12)
`Kuyumcu.Web/Servisler/TevsikOneri.cs` · `Kuyumcu.Web/Pages/Islemler.Odeme.cs` · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` · `Kuyumcu.Core/I18n/Sozlukler.SatisTuval.cs` · `Kuyumcu.Web/Bilesenler/Fis/FisNakitUyari.razor` (yalnız yorum) · testler `Kuyumcu.Tests/UX4dSafTestleri.cs` (1 girdi + 1 yeni test), `Kuyumcu.Tests/SATIS3BilesenTestleri.cs` (1 yeni test). Dokunulan sayfalar: yok (Islemler.razor açılmadı).

### 24.7 DoD sözleşmesi (t12)
Yeni varlık/kayıt türü/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı.

### 24.8 Açık kalanlar (sahibiyle)
- `S34RaporBugunTestleri.S34_10_…` kırmızı — ZO-BELGE (Raporlar metni değişmiş, test güncellenmemiş).
- `OrtakGorunumSaflik.O8_…` kırmızı — ORTAK-1.
- İstemci `tevsikHaddi ?? 30_000m` yedeği — ayrı paket (Web, sayı gömme kuralı).
- Sunucu reddi için makine okunur hata kodu — ayrı paket (Api).
- Tedarikçiye ödeme ucu → ODEME türü geri eklenir (Cari/Api; orkestratör kuyruğu).
- § 23.6'daki alan dışı açıklar (MASAK "Doğum Tarihi" K7, PRO uzun ad K10, h1 "Satış", stok sütunu, K9) değişmedi.

### 24.9 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) bu paketin testleri 0 başarısız ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon 3/3 ✓ · (5) canlı 30 ölçüm + görüntüler ✓ · (6) test kiracısı silindi, tenants 17→17 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 25. 29.09 t13 koşumu (02:45 – 03:25) — tasarım hakeminin 2 YÜKSEK bulgusu (liste kendiliğinden açılıyor · "elle ekle" aynı listeyi açıyor)
Görev istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" dediği için kod yazıldı (t11/t12 ile aynı yorum). POST gövdeleri, uçlar, mevcut DOM id'leri DEĞİŞMEDİ; migration yok; yeni varlık/uç yok; mevzuat hükmü yazılmadı.

### 25.1 Bulgu → yapılan → kanıt
| # | Bulgu (hakem) | Yapılan | Kanıt |
|---|---|---|---|
| Y1 | Ürün arama listesi Basit↔Profesyonel geçişinde ve sekme dönüşünde kendiliğinden açılıp kalem satırı/toplamların üstüne biniyor (K9 6) | `FisKalemGirisi.razor`: liste durumu `listeAcik` YALNIZ kullanıcı eylemiyle açılır — kutuya tıklama, yazma, ↓, kutudayken F2; odak olayı (`OdakGeldi`) artık yalnız klavye seçimini sıfırlar (programın `OdakJeton` odağı, mod geçişi, sekme dönüşü, Tab ile gelme listeyi AÇMAZ). Sayfanın F2 kısayolu (odak başka yerdeyken) `AcJeton` parametresiyle açar; bileşen yeniden kurulduğunda sayfanın o anki jetonu AÇMAZ (ilk değer yalnız kaydedilir). Boş odak listesi isteği de yalnız açılışta gider | bUnit `T13_ODAK_tek_basina_listeyi_ACMAZ_F2_ve_kisayol_F2_ACAR`, `T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ`; güncellenen `Bos_odakta_EN_COK_SATANLAR_…` (odak yerine tıklama). Canlı: `basit2pro` (kalem + Profesyonel), `pro2basit`, `pro-sekme-donus` (Hurda→Satış), `basit-sekme-donus`, `acilis-*` → 5 genişlik/tema × 6 durum = 30 ölçümde `listeAcik:false`, `aria-expanded=false`, odak kutuda, **K9 = 0**, yatay 0. Kasıtlı açılışlar çalışıyor: `tik-bos-odak-PRO` ve `f2-PRO` → 8 seçenek |
| Y2 | "Listede olmayan ürünü elle ekle" aynı ürün seçicisini açıyor, yazılan ad kullanılmıyor | Sunucu kalemde ürün kimliğini ZORUNLU tutar (`SatisModulu.cs:48/122/186/382/406` `[Required] UrunId`) → serbest adlı kalem ancak Api değişikliğiyle mümkün (alan dışı). Hakemin ikinci seçeneği uygulandı: satır adı yaptığı işi söyler — TR "Ürünü seçip fiyatı elle yaz" / EN "Pick the product and type the price"; aranan metin forma taşınır: elle satırı bu satırdan açılınca ürün seçicisinin boş hâli `«zzqqxx» için ürün seçin` (yeni anahtar `Satis3Metinleri.ElleAranan`, TR+EN; seçici `YerTutucu` parametresi — ortak `Secici.razor`'a dokunulmadı) | bUnit `T13_ELLE_satiri_yaptigi_isi_soyler_ve_ARANAN_metni_seciciye_tasir`; mevcut `Olmayan_metin_BULUNAMADI…` sözlükten okuduğu için yeşil. Canlı `elle-liste-*`: `elleMetin:"Ürünü seçip fiyatı elle yaz"`; `elle-form-*`: `seciciMetin:"«zzqqxx» için ürün seçin"`, 375'te de K10 = 0 (ilk sürüm "Aranan: … — satılacak ürünü seçin…" 1440'ta üç noktaya düştü → kısaltıldı) |
| ek | Canlı ölçümde yeni: PRO'da 94.959 ₺'lik fiş → Hurda Alış → Satış; kalemler boşalmış (0 ₺) ama Yasal kapılar kartı "30.000 ₺ nakit kuralı: Aşıldı" | `Islemler.razor` `TurSec`: `proTevsik` sıfırlanır ve PRO satışa dönüşte önizleme yeniden sorulur (tek satır; dosya 1399 satır ≤1400) | bUnit `T13_PRO_sekme_donusunde_yasal_kapi_ONCEKI_fisin_ASILDI_durumunu_TASIMAZ`; canlı `pro-sekme-donus` sonrası "Yasal kapılar uygun" (görüntü Read ile görüldü) |

### 25.2 Derleme · testler · denetim
- `dotnet build Kuyumcu.sln` → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası; bu koşumda dokunulmadı). Not: koşum ortasında (02:50–02:53) derleme `PosModulu.cs` `PosYatmasiKapisi/CihazHesapKapisi` hatasıyla KIRMIZIYDI — ZO-ISLEM-A şeridinin o an yazdığı dosya; dokunulmadı, şerit tamamlayınca yeşile döndü.
- SATIS3 kümesi: **261/261** (yeni 4 test: T13_ODAK, T13_MOD, T13_ELLE, T13_PRO_sekme). Geniş küme (SATIS3|SATIS2|S22|TS1|TS2|P10|KarmaOdeme|Iade|S21|S26|L1|B4|S25|Islem|Fis|UX4|Takas|S24|T10|PosBarkod|OlcekTek|S1iGorsel): 1523 · 1517 geçti · 6 kırmızı — hepsi `ZOISLEMAEkranTestleri.ZO28*` (POS eşleştirme, ZO-ISLEM-A yarım işi; Islemler/Fis'e dokunmaz).
- **Tam süit** (`trx-pSATIS-3-t13b`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **6928 · 6926 geçti · 2 kırmızı**: `OrtakGorunumSaflik.O8_…` (ORTAK-1, bilinen: beklenen 15 gelen 16) ve `ZoBelgeDenetimSistemTestleri.ZBH01_…` (ZO-BELGE, `DenetimModulu.cs` koşum sırasında o şeritçe değiştirildi). **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0). (Aradaki bir koşumda `Sınırsız ToListAsync (Modules · B-8) 149/145` görüldü — Api/Modules'ta yalnız `PosModulu.cs` ve `DenetimModulu.cs` değişmişti, ikisi de başka şeritlerin; son koşumda TEMİZ.)

### 25.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t13/<kod>.orj` → perl tek eşleşme → `SATIS3EkranTestleri` → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T13-1 | `OdakGeldi` listeyi yeniden açar (`listeAcik = true`) | T13_ODAK_…, T13_MOD_… | BİREBİR |
| M-S3-T13-2 | `AcJeton` ilk değeri 0 sayılır (yeniden kurulan bileşen sayfanın jetonuyla açılır) | T13_MOD_… | BİREBİR |
| M-S3-T13-3 | sayfa F2 kısayolu jetonu artırmaz | T13_ODAK_…, T13_MOD_… | BİREBİR |
| M-S3-T13-4 | `ElleYerTutucu` → null (aranan metin taşınmaz) | T13_ELLE_… | BİREBİR |
| M-S3-T13-5 | satır adı eski "Listede olmayan ürünü elle ekle" | T13_ELLE_… | BİREBİR |
| M-S3-T13-6 | `TurSec` PRO önizleme tazelemesi silinir | T13_PRO_sekme_… | BİREBİR |
6/6 kırmızı. Dürüstlük notu: M-1 ve M-2'nin İLK koşumunda (T13_MOD'a F2 ön adımı eklenmeden hemen sonra) sırasıyla 1 ve 0 kırmızı görüldü; aynı mutasyonlar tekrarlandığında (M-1b, M-2b, yukarıdaki tablo) T13_MOD kırmızıya döndü. İlk koşumun farkı ayrıca bir hata ayıklama koşumuyla doğrulandı (M-1 altında T13_MOD satır 319'da kırmızı). Son mutasyondan sonra temiz derleme 0 hata.

### 25.4 Gerçek tarayıcı (başsız Chrome CDP 9913, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3N7PBSPFTG52PFJ1Y0M2PYH) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py`. (İlk tohum denemesi kur 40 sn'de gelmediği için yarıda kaldı ve aynı adla 01M3N7KW1M7KF0JP49FAHN2CPG bıraktı → `kiraci-temizle.sh --uygula` ile silindi, yeniden kuruldu.) İlk ölçüm iki tarayıcıda paralel koşuldu; iki oturum aynı kullanıcının mod tercihini değiştirdiği için (sunucuda saklı) sonuçlar karıştı → ATILDI, tümü tek tarayıcıda sıralı yeniden koşuldu.
12 durum × {1440 açık, 1280 açık, 375 açık, 1280 koyu, 375 koyu} = **60 ölçüm** (+ TurSec düzeltmesinden sonra `pro-sekme-donus` ve `basit2pro` 10 ölçüm yeniden), yatay kaydırma **0** (hepsi).
| Durum | Mod | Liste | K-liste (K2/K4 bilgi hariç) |
|---|---|---|---|
| acilis-BASIT / acilis-PRO | B / P | kapalı | 0 |
| basit2pro (kalem + "Profesyonel") | B→P | **kapalı** (önce: 6 seçenek açık, K9 6) | 0 |
| pro2basit (kalem + "Basit") | P→B | kapalı | 0 |
| pro-sekme-donus (Hurda→Satış) | P | **kapalı** (önce: 6 seçenek) | 0 |
| basit-sekme-donus | B | kapalı | 0 |
| tik-bos-odak-PRO (kutuya tıklama) | P | açık, 8 seçenek | K9 6 — kullanıcının açtığı bağlı liste (bilinen KATMAN açığı) |
| f2-PRO (odak sekmede, F2) | P | açık, 8 seçenek | K9 6 — aynı |
| elle-liste-BASIT / -PRO ("zzqqxx") | B / P | açık: "Bulunamadı" + "Ürünü seçip fiyatı elle yaz" | K9 2 — aynı |
| elle-form-BASIT / -PRO | B / P | kapalı; seçici "«zzqqxx» için ürün seçin" | 0 (375 dahil K10 0) |
Görüntüler Read ile GÖRÜLDÜ: `basit2pro` açık 1440 (liste kapalı, kalem ve toplam görünür), `elle-form-PRO` açık 1440, `pro2basit` koyu 1280, `elle-liste-BASIT` açık 1440, `elle-form-BASIT` koyu 375, `pro-sekme-donus` açık 1440 (düzeltme öncesi "Aşıldı", sonrası "Yasal kapılar uygun"). 40 dosya `ham/TUVAL-KARSILASTIRMA/SATIS-3-t13-*.png`; ham ölçüm `/private/tmp/kuyumcu-serit/SATIS-3/t13/m/olcum*.jsonl`.

### 25.5 Temizlik
API/vekil/Chrome durduruldu: `lsof` 5162/5163/9913/9915 **boş**; 5198/5199/5200'e dokunulmadı. Sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı (0 bayt). `kiraci-temizle.sh 01M3N7PBSPFTG52PFJ1Y0M2PYH --uygula` rc 0 (urunler/hareketler 0 satır). `tenants` başlangıç **14** → son **14**. Proje ağacında `.mutorj/.orj/.bak/.tmp` artığı 0. Ölçüm profili ve görüntüler `/private/tmp/kuyumcu-serit/SATIS-3/t13` altında (hook `rm`'yi engeller → saatlik temizlik/kurucu).

### 25.6 Değişen dosyalar (t13)
`Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` (liste açılış kuralı, `AcJeton`, `ElleYerTutucu`) · `Kuyumcu.Web/Pages/Islemler.Pro.cs` (`aramaAcJeton`, F2) · `Kuyumcu.Web/Pages/Islemler.razor` (2 satır içi değişiklik: `AcJeton` parametresi, `TurSec` önizleme tazeleme; 1399 satır) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (`ElleEkleSatiri` metni, yeni `ElleAranan`, TR+EN) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs` (4 yeni test, 1 test odak→tıklama). Dokunulan sayfalar: `Pages/Islemler.razor` (bu şeridin kendi sayfası; gerekçe: hakem bulgusu Y1 + canlıda bulunan yasal kart kusuru).

### 25.7 DoD sözleşmesi (t13)
Yeni varlık/kayıt türü/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı. Yeni düğme yok; "Ürünü seçip fiyatı elle yaz" satırı gerçek iş yapar (elle satırını açar).

### 25.8 Açık kalanlar (sahibiyle)
- Serbest adlı kalem (katalogda olmayan ürünü adıyla satmak): sunucu kalemde `UrunId` zorunlu → Api/Satis + Ürün paketi (orkestratör kuyruğu); ekran o gün satır adını değiştirir.
- `OrtakGorunumSaflik.O8_…` kırmızı — ORTAK-1. `ZoBelgeDenetimSistemTestleri.ZBH01_…` kırmızı — ZO-BELGE. `ZOISLEMAEkranTestleri.ZO28*` (geniş kümede 6) — ZO-ISLEM-A yarım işi.
- Kullanıcının açtığı bağlı listenin altındaki içerikle K9 örtüşmesi — KATMAN (bilinen).
- § 24.8'deki diğer açıklar değişmedi.

### 25.9 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) bu paketin testleri 0 başarısız (SATIS3 261/261) ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon 6/6 ✓ · (5) canlı 70 ölçüm + görüntüler ✓ · (6) test kiracısı silindi, tenants 14→14 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 26. 29.09 t14 koşumu (03:40 – 04:10) — doğrulayıcının KRİTİK bulgusu (PRO arama listesi kalem kartında kırpılıyor)
DEVİR NOTU 4 "kod yazma" diyor, ama o not 28.09 21:50 tarihli ve t9–t13 koşumlarından ÖNCE; bu koşumun istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" diyor ve bulgu KRİTİK → t11–t13 ile aynı yorumla **yalnız bu bulgu için** kod yazıldı (tek CSS kuralı + tek test). POST gövdeleri, uçlar, DOM id'leri, Razor DEĞİŞMEDİ; migration yok; mevzuat hükmü yok.

### 26.1 Bulgu → yapılan → kanıt
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| K1 (KRİTİK) | PRO'da `#islem-pro-kalem-kart` `overflow:hidden` (tuval-satis.css:490) bağlı ürün listesini kesiyor: 8 öneriden 4'ü, "Ürünü seçip fiyatı elle yaz" satırı erişilemez | `css/tuval-satis.css` sonuna: `.tuval-pro .fis-pro-kart { overflow: visible; }`; köşe yuvarlatması iç öğelere taşındı (`> .fis-pro-giris:first-child` üst 11 px, `> .fis-pro-toplam:last-child` alt 11 px — kenarlık 1 px içinde); kart içindeki listeye `z-index:60` | Canlı (aşağıda) 24 ölçümde `gorunmez=[]` (her öneri ve elle satırı `elementFromPoint` ile kendisi), `kartOverflow=visible`; yeni test `SATIS3EkranTestleri.T14_PRO_kalem_karti_arama_listesini_KIRPMAZ` (kartın SON `overflow` bildirimi visible + iki köşe kuralı) |
| ORTA | Görünen üst başlık hep "Satış" | DOKUNULMADI — üst bar h1'i `Layout/UstBar` (KATMAN şeridi); gerekçe DEVİR NOTU 4 ile aynı | açık kalanlarda |
| DÜŞÜK | "Ödeme" türü yok | DOKUNULMADI — sunucuda tedarikçiye ödeme ucu yok (§ 23 Y3, § 24 D1), kuyrukta | açık kalanlarda |
| DÜŞÜK | Arama satırında fiyat "—", stok sütunu yok | DOKUNULMADI — `/urunler` yanıtı stok/fiyat taşımıyor (Api/Urun, alan dışı) | açık kalanlarda |
| DÜŞÜK | CDP 9963 çakışması (doğrulayıcı süreç notu) | Bu koşum CDP **9914** kullandı (atanan 9900–9920), iş sonunda süreç öldürüldü | `lsof :9914` boş |

### 26.2 Gerçek tarayıcı (başsız Chrome CDP 9914, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3NADVCTT64KDRJFCV03B4MX) `demo-tohumla.js --prova-kiraci` + `ek-tohum.py` (12 ürün, 6+1 cari "Anadolu Holding A.Ş.", 12 satış, 2 kasa, 1 POS). Betik `/private/tmp/kuyumcu-serit/SATIS-3/t14/liste.mjs`: kutuya tıklama (boş odak) ve "Ayar" harf harf yazma → listenin her satırı `scrollIntoView` + `elementFromPoint`.
| Mod | Tema | Gen. | Durum | Seçenek | Elle satırı | Görünmeyen | Liste top/bottom | Kart bottom | K1/K3/K5/K7/K10/K12/K13 | K9 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık/koyu | 1440 | boş odak | 8 | — | **0** | 360/760 | 569 | 0 | 6* | 0 |
| PRO | açık/koyu | 1440 | "Ayar" | 11 | var | **0** | 360/776 (iç kaydırma 492/414) | 569 | 0 | 6* | 0 |
| PRO | açık/koyu | 1280 | boş odak | 8 | — | **0** (önce 4) | 426/826 | 635 | 0 | 6* | 0 |
| PRO | açık/koyu | 1280 | "Ayar" | 11 | var | **0** (önce elle satırı erişilemez) | 426/842 | 635 | 0 | 6* | 0 |
| PRO | açık/koyu | 375 | boş odak / "Ayar" | 8 / 11 | — / var | **0** | 432/848 | 649 | 0 | 6* | 0 |
| BASİT | açık/koyu | 1440·1280·375 | boş odak / "Ayar" | 8 / 11 | — / var | **0** | 239/641–655 · 432/848 | — | 0 | 6* | 0 |
\* K9 = kullanıcının açtığı bağlı listenin altındaki içerikle örtüşme — liste KASITLI üstte (KATMAN kalıbı, § 25.4 ile aynı; kırpma değil). Kapalı kart (PRO 1280 açık/koyu): `overflow: visible`, giriş alanı radius `11px 11px 0 0`, toplam şeridi `0 0 11px 11px`, yatay 0.
Görüntüler Read ile GÖRÜLDÜ: `t14-ayar-PRO-aydinlik-1280` (liste kartın dışına taşıyor, 9 öneri görünür, kalanı iç kaydırmada), `t14-bos-odak-PRO-karanlik-1440` (8 önerinin 8'i tam), `t14-ayar-PRO-karanlik-375`, `t14-kapali-PRO-karanlik-1280` (kart köşeleri yuvarlak, toplam şeridi taşmıyor). 26 dosya `/private/tmp/kuyumcu-serit/SATIS-3/t14/foto/`; ham ölçüm `…/t14/olcum.jsonl`.

### 26.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (temiz derleme, mutasyon sonrası).
- SATIS3 kümesi (`FullyQualifiedName~SATIS3`, t2 DB): önce **273/273**; yeni test `T14_PRO_kalem_karti_arama_listesini_KIRPMAZ` 1/1.
- Mutasyon **M-S3-T14-1**: yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t14/css.orj` → `perl` ile `.tuval-pro .fis-pro-kart { overflow: visible; }` satırı silindi (tek eşleşme, sonra 0) → `T14_PRO_kalem_karti_arama_listesini_KIRPMAZ` **KIRMIZI** → geri kopya → `cmp` **BİREBİR**.
- Tam süit (`trx-pSATIS-3-t14`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): **6948 · 6947 geçti · 1 kırmızı** = `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1, § 25.2'de zaten listede). **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 26.4 Temizlik
API (5162), vekil (5163), Chrome (9914) durduruldu; `lsof` üçü de **boş**; 5198/5199/5200'e dokunulmadı. `kiraci-temizle.sh 01M3NADVCTT64KDRJFCV03B4MX --uygula` + `kiraci_davetleri` (PAKETSATIS-3%) silindi; `tenants` başlangıç **16** → son **16**, `PAKETSATIS-3%` = 0. Sistem anahtarı ve fikstür `printf ''` ile boşaltıldı. Proje ağacında `.mutorj/.orj/.bak/.tmp` 0. Profil/görüntüler `/private/tmp/kuyumcu-serit/SATIS-3/t14` altında (hook `rm`'yi engeller → saatlik temizlik/kurucu).

### 26.5 Değişen dosyalar (t14) · dokunulan sayfalar · DoD
`Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona 4 kural) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs` (1 yeni test). Razor sayfası açılmadı. Yeni varlık/uç/düğme yok → PATCH/DELETE/denetim yükümlülüğü doğmadı.

### 26.6 Açık kalanlar (sahibiyle)
- Görünen üst başlık h1 = "Satış" (tür adı yalnız görsel gizli `#sayfa-basi-islemler-baslik` ve `document.title`'da) — **KATMAN** (Layout/UstBar).
- Arama satırında stok sütunu / sunucu fiyatı — **Api/Urun** paketi (`/urunler` yanıtına stok+fiyat).
- "Ödeme" türü — **Cari/Api** tedarikçiye ödeme ucu (kuyrukta).
- `OrtakGorunumSaflik.O8_…` kırmızı — **ORTAK-1**.
- § 25.8'deki diğerleri değişmedi.

### 26.7 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) SATIS3 testleri 0 başarısız ✓ · (3) denetim TEMİZ ✓ · (4) mutasyon M-S3-T14-1 kırmızı + cmp ✓ · (5) canlı 24+2 ölçüm + görüntüler ✓ · (6) test kiracısı silindi, tenants 16→16 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 27. 29.09 k2 koşumu (04:45 – 05:45, kapanış 2 wf_7de4c77e-bca) — tasarım hakemi k3'ün iki bulgusu (K19 karma toplam · BASİT eski altın 2 ekran)
DEVİR NOTU 4 ("kod yazma") 28.09 21:50 tarihli; bu koşumun istemi "önceki doğrulamada kalan bulgular — önce bunları kapat" diyor ve iki bulgu KRİTİK → t9–t14 ile aynı yorumla **yalnız bu iki bulgu için** kod yazıldı. POST gövdelerinin ŞEKLİ değişmedi (yalnız tutarı 0 olan boş ödeme satırı artık gönderilmiyor — aşağıda). Migration yok. Mevzuat hükmü eklenmedi (nakit yasağı yine sunucunun bildirdiği hadden; eşik sayısı istemciye gömülmedi).

### 27.1 Bulgu → kök neden → yapılan → kanıt
| # | Bulgu (hakem k3) | Kök neden (koda bakıldı) | Yapılan | Kanıt |
|---|---|---|---|---|
| K19-a | "+ Başka ödeme ekle" yasak Nakit 0 satırı açıyor | `FisFark.YeniBacakAraci` fişte olmayan ilk aracı seçiyordu (Nakit); nakit yasağını bilmiyordu | `YeniBacakAraci(mevcut, nakitsiz)`: nakit kullanılamıyorsa Nakit atlanır (Kart → Havale). `Islemler.YeniOdemeNakitsiz` = `tevsikNakitKapali` ya da **sunucudan bilinen** had aşıldı (`TevsikOneri.Gerekli`); had bilinmiyorsa false. `FisOdemeBacaklari.Nakitsiz` parametresi, `BacakEkle` aynı kuralı kullanır | Canlı: Anadolu Holding 95.1xx ₺ Kart → yeni satır **Havale** (BASİT ve PRO, 10/10 ölçüm); test `K19_nakit_kullanilamayan_fiste_yeni_odeme_satiri_NAKIT_acilmaz` |
| K19-b | "Fark 0 ✓" iken Kaydet "Ödeme tutarını girin" diye kilitli | `KarmaGonderilebilir` her satırda `TutarTl > 0` istiyordu | `FisFark.Gonderilecek()`: tutarı 0 olan TL satırı (altın hariç) **kilitlemez ve gövdeye girmez** (kullanıcı silmiş gibi). Kilit sebebi yalnız hiç tutarlı satır yoksa | Canlı: Kart + Havale 0 → Kaydet **ETKİN**, fark 0 (BASİT+PRO); test `Baska_odeme_ekle_ILK_tikta_IKI_satir_acar_bos_satir_Kaydeti_KILITLEMEZ_govdeye_girmez` (gövdede 1 ödeme, 10.000) |
| K19-c | Kart + boş satır + Eski altın → Kart kalanda kalıyor, fark −63.101 | `FisHesap.OtoDengele` hedefi "elle yazılmamış SON TL satırı" = boş (0) satırdı; kalan oraya max(0,…)=0 yazılıyordu | Hedef artık **tutarı olan** son elle-yazılmamış TL satırı (yoksa son aday) | Canlı `k19-kart-bos-altin`: Kart **31.939,12** + Havale 0 + Eski altın 63.180,26 = fiş, fark **0**, Kaydet ETKİN (BASİT+PRO ×5 genişlik/tema); testler `K19_OtoDengele_hedef_TUTARLI_satir_bos_satir_kalani_calmaz`, `K19_Baska_odeme_sonra_Eski_altin_KART_KALANA_duser_bos_satir_hedef_olmaz` (BASİT+PRO) |
| K19-d | İki kapı iki sonuç: ayrı "Eski altın" düğmesi | `FisVerdimAldim` #islem-odeme-altin ayrı yol (EskiAltinEkle) | **Ayrı düğme kaldırıldı** — tek kapı "+ Başka ödeme ekle", eski altın açılan satırın ödeme türü listesinden (taslaktaki adıyla). Özellik silinmedi. BASİT ipucu (#islem-odeme-altin-ipucu) yeni yolu söyler (`Satis3.EskiAltinYolu`, TR+EN) | Canlı `eskiAltinDugme=false` (40/40); S24/CumaS testleri tek kapıya uyarlandı |
| EA-1 | BASİT eski altın 1,8–2,0 ekran | iç içe iki çerçeve + satıcı 4 alan + 6 maddelik liste + iki büyük salt okunur kutu | İç `fieldset#altin-satici` → düz `div` (ince ayraç + küçük başlık, id aynı); BASİT'te dış küme çerçevesiz (`.islem-sayfa #islem-aldim #altin-bacagi`); has + tutar BASİT'te **tek satır** (`#altin-hesap-ozet`; kutular hidden, id/data-deger aynı) | BASİT 1440 eski altın (müşterili): **1,34** ekran (önce 1,8–2,0); `#altin-bacagi fieldset` = 0 |
| EA-2 | Müşteri seçili ama satıcı ad/TCKN/adres boş | Panel müşteriyi hiç bilmiyordu | `AltinBacagiGirisi.MusteridenDoldur`: seçili GERÇEK müşterinin (Peşin/sistem carisi değil) adı, TCKN'si (yalnız kayıttaki numara TCKN ise), adresi BOŞ alanlara dolar, rozet "Müşteriden geldi"; elle yazılana dokunmaz; müşteri kaldırılınca müşteriden gelenler temizlenir. `CariVeri` istemci kaydına `/cari` yanıtının ZATEN taşıdığı `vergiNo, adres, kimlikTuru` eklendi | Canlı: satıcı özeti "Satıcı: Vatandaş · Anadolu Holding A.Ş. · Müşteriden geldi", alan kutusu **0**, Kaydet ETKİN; POST gövdesinde satıcı = müşteri (test `K2_BASIT_…govdede_musteri`) |
| EA-3 | "Yalnız eksik olan alanı gösterin" | — | BASİT: müşteriden dolan alanlar + satıcı türü tek özet satırında, **"Değiştir"** (#altin-satici-degistir) hepsini açar; eksik alan kutusu görünür. PRO: tüm alanlar, değerler dolu + rozet | Canlı Peşin: yalnız ad/TCKN/adres kutuları; müşterili: 0 kutu |
| EA-4 | "Kaydetmek için" 3–6 maddelik turuncu liste | `#altin-eksikler` bant olarak görünüyordu | Liste **hidden** (sayfanın kilit sebebi ve eski testler aynı kaynağı okur); eksik alan **kendi kutusunun altında** (`Hata` — ayar/gram/fiyat üçünden biri yazılınca, satıcı alanları değer hesaplanınca; açılışta kırmızı duvar yok) | Canlı Peşin: 3 alan altı mesaj, liste görünmez, K9 0; test `K2_Pesin_…EKSIK_alan_altinda_mesaj_liste_yok` |
| Yan | PRO 375 "Anadolu Holding A.Ş." bakiye rozeti yüzünden 2 satır (K3) — canlı ölçümde bulundu | PRO rozet kutu içinde, tetik `padding-right: 46%+36px` | ≤600 px'te rozet kutunun altına (BASİT t11 ile aynı); tetik dolgusu yüz sınıfsız seçiciyle (`#islem-ust-satir.fis-ust-ozetli`, OLCEK-1 OL26 envanterine dokunmadan) | K3 1 → **0** |
| Yan | BASİT <1400 eski altın satırında ✕ panelin altına düşüyordu (canlı görüntü) | flex sırası | `.tuval-tezgah .bacak-satiri > #altin-bacagi { order:3; flex:1 1 100% }` (PRO kuralının aynısı) | 1280 koyu görüntü: ✕ ödeme türünün yanında |

### 27.2 Gerçek tarayıcı (başsız Chrome CDP **9916**, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` — `demo-tohumla.js --prova-kiraci` + `k2/ek-tohum.py` ("Anadolu Holding A.Ş." TCKN+adresli, 12 satış, 2 kasa, 1 POS). Rastgele tek kullanımlık `Sistem__Anahtari` (kurucu 20:12 onayı), değeri hiçbir çıktıya yazılmadı. Betik `/private/tmp/kuyumcu-serit/SATIS-3/k2/senaryo.mjs` (4 durum × {BASİT, PRO} × {1440 açık, 1280 açık, 1280 koyu, 375 açık, 375 koyu}); son ağaçta 40 + 30 yeniden ölçüm (CSS düzeltmesinden sonra).
| Durum | Mod | 1440 | 1280 a/k | 375 a/k | Satırlar (son) | Fark | Kaydet | Satıcı kutusu | K1..K13 |
|---|---|---|---|---|---|---|---|---|---|
| k19-kart-baska (Kart → + Başka ödeme) | BASİT | 1,08 | 1,22 | 2,11 | Kart 95.1xx · **Havale 0** | 0 | ETKİN | — | 0 |
| k19-kart-baska | PRO | 1,29 | 1,80 | 2,79 | Kart · **Havale 0** | 0 | ETKİN | — | 0 (önce K3=1) |
| k19-kart-bos-altin (hakemin yolu) | BASİT | 1,41 | 1,62 | 2,93 | **Kart 31.939** · Havale 0 · Altın 63.180 | 0 | ETKİN | 0 (özet) | 0 |
| k19-kart-bos-altin | PRO | 2,01 | 2,61 | 4,10 | Kart 31.933 · Havale 0 · Altın 63.169 | 0 | ETKİN | 4 (dolu, rozetli) | 0 |
| altin-musteri (temiz yol) | BASİT | **1,34** | 1,51 | 2,73 | Kart 31.947 · Altın 63.196 | 0 | ETKİN | 0 (özet) | 0 |
| altin-musteri | PRO | 1,92 | 2,50 | 3,80 | Kart · Altın | 0 | ETKİN | 4 (dolu) | 0 |
| altin-pesin (Peşin, 0,3 g) | BASİT | 1,62 | 1,91 | 3,03 | Nakit 1.394,83 · Altın 1.805,17 | 0 | PASİF + "Eski altın: Satıcı ad soyad zorunludur." | 3 (eksik, alan altı mesaj) | 0 |
| altin-pesin | PRO | 2,04 | 2,42 | 3,65 | Nakit · Altın | 0 | PASİF + sebep | 4 | 0 |
Yatay kaydırma 70/70 ölçümde 0; `#islem-odeme-altin` 70/70'te yok; `#altin-bacagi fieldset` (iç içe) 70/70'te 0.
Görüntüler Read ile GÖRÜLDÜ (10): BASİT 1440 müşterili eski altın (tek çerçeve, "Has karşılığı 9,6180 has · Fişe sayılan tutar 63.1xx ₺" tek satır, "Satıcı: Vatandaş · Anadolu Holding A.Ş. [Müşteriden geldi] [Değiştir]", Kaydet etkin) · BASİT 1440 Kart+Havale 0 (Fark 0 ✓, Kaydet etkin) · BASİT 1280 koyu hakem yolu (önce ✕ panelin altında → sonra yanında) · BASİT 375 koyu · BASİT 1440 Peşin (3 alan, alan altı kırmızı mesaj, liste yok) · PRO 1440 müşterili (tek dış çerçeve, satıcı alanları dolu "Cariden geldi") · PRO 375 koyu (önce ad 2 satır → sonra tek satır, rozet altta). Kanıt kopyaları: `ham/TUVAL-KARSILASTIRMA/SATIS-3-senaryo-k2-*.png` (8 dosya); tüm 40 görüntü `/private/tmp/kuyumcu-serit/SATIS-3/k2/m/foto/`, ham ölçüm `…/k2/m/olcum*.jsonl`.

### 27.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (son mutasyondan sonra temiz derleme).
- İlgili küme (SATIS3|S24|B4Altin|CumaS|Karma|S22|S21|TezgahFis|L1|TS1|TS2|SATIS2|Islem|Fis, t2 DB): **1206/1206**. Yeni test **7** (6 yöntem; K19 ekran testi BASİT+PRO). Değişen eski testler (davranış bilinçli değişti, gerekçe hakem k3): `S22…Odemeyi_bol_…` (boş satır artık kilitlemez), `SATIS3…Baska_odeme_ekle_…` (ad: `…_KILITLEMEZ_govdeye_girmez`, gövde 1 ödeme), S24 E-1/E-2/E-3/E-7 + yardımcı (tek kapı), `CumaS_01` (ayrı düğme yok).
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-k2/<kod>.orj` → perl tek eşleşme → filtreli test → geri kopya → `cmp`):
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-K2-1 | OtoDengele hedefi "tutarlı satır" tercihi silindi | K19_Baska_odeme_sonra_Eski_altin_… (BASİT, PRO), K19_OtoDengele_hedef_TUTARLI_… | BİREBİR |
| M-S3-K2-2 | YeniBacakAraci nakit yasağını yok sayar | K19_nakit_kullanilamayan_fiste_yeni_odeme_satiri_NAKIT_acilmaz | BİREBİR |
| M-S3-K2-3 | Gonderilecek boş satırları da gönderir | S22 Odemeyi_bol_…, Baska_odeme_ekle_…_KILITLEMEZ_govdeye_girmez (BASİT, PRO), K19_OtoDengele_… | BİREBİR |
| M-S3-K2-4 | MusteridenDoldur müşteriyle hiç doldurmaz | K2_BASIT_satici_kimligi_SECILI_MUSTERIDEN_…, K2_PRO_satici_alanlari_… | BİREBİR |
| M-S3-K2-5 | BASİT'te müşteriden dolan kutular da gösterilir | K2_BASIT_satici_kimligi_… | BİREBİR |
| M-S3-K2-6 | "Kaydetmek için" listesi yeniden görünür | K2_BASIT_…, K2_Pesin_…_liste_yok | BİREBİR |
- Tam süit (`trx-pSATIS-3-k2/k2b.trx`, t2): **6995 · 6994 geçti · 1 kırmızı** = `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (**ORTAK-1**, önceki koşumlarda da listede). İlk tam koşumda bu paketin CSS'i `OlcekTekTestleri.OL26_v13_…` kırmızısı üretti (mod yüzü sınıfında kontrol ölçüsü) → seçiciler yüz sınıfsız yazıldı (OLCEK-1 envanterine dokunulmadı), yeniden tarayıcıda ölçüldü, OL26 yeşil. **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0; OLCEK-1 YUZ_OLCU_ENVANTER_DISI 0).

### 27.4 Temizlik
API 5162, vekil 5163, Chrome 9916 durduruldu; `lsof` üçü **boş**; 5198/5199/5200'e dokunulmadı. İki test kiracısı (01M3NE42WY3V13FW6PTZYZHMSD, 01M3NFNJHP28AZ1ZGK9EZTWA1D) `kiraci-temizle.sh --uygula` (rc 0; 70 tablo) + `kiraci_davetleri` silindi; `PAKETSATIS-3%` = **0**. `tenants`: başlangıç **14** → son **16** — fark bu paketin değil: koşum sırasında başka şeritlerin açtığı `PAKET-OLCEK-1-HAKEM5-TEST…` ve `PAKETZO-ISLEM-A-HAKEM-TEST…` (dokunulmadı). Anahtar ve fikstür dosyaları `printf ''` ile 0 bayt. Proje ağacında `.mutorj/.orj/.bak/.tmp` 0.

### 27.5 Değişen dosyalar (k2) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Bilesenler/Fis/{FisFark.cs, FisModelleri.cs, FisOdemeBacaklari.razor, FisVerdimAldim.razor, FisOdemeKasa.razor}` · `Kuyumcu.Web/Bilesenler/AltinBacagiPaneli.razor` (alan dışı ama KAYNAK 2'nin konusu — § 2'deki gerekçe) · `Kuyumcu.Web/Servisler/AltinBacagiGirisi.cs` (ön-dolgu + alan hataları; gövde `Bacak()` değişmedi) · `Kuyumcu.Web/Pages/{Islemler.razor (1398 satır), Islemler.Odeme.cs}` · `Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs` (`CariVeri` + 3 isteğe bağlı alan — sunucunun zaten döndürdüğü) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (TR+EN 3 anahtar) · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona kurallar; sabit renk yok, belirteç) · testler `SATIS3EkranTestleri.cs`, `S24EskiAltinEkranTestleri.cs`, `S22TezgahFisiTestleri.cs`, `CumaSAldimSadeTestleri.cs`. Yeni DOM id: `altin-satici-baslik`, `altin-satici-ozet`, `altin-satici-kaynak`, `altin-satici-degistir`, `altin-hesap-ozet`, `altin-*-hata`; mevcut id'ler aynı (yalnız `#islem-odeme-altin` ve `#islem-odeme-altin-not` KALKTI — hakemin "tek kapı" isteği). Yeni varlık/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı; "Değiştir" düğmesi gerçek iş yapar (alanları açar). Razor sayfası: yalnız `Pages/Islemler.razor` (+ partial) — § 12 ile aynı.

### 27.6 Açık kalanlar (sahibiyle)
- `OrtakGorunumSaflik.O8_…` kırmızı — **ORTAK-1**.
- PRO eski altın 1440'ta 1,9–2,0 ekran (satıcı 4 alanı açık kalır — PRO tam form kuralı); hakemin KRİTİK'i BASİT içindi. PRO'da da özet satırı istenirse ayrı karar.
- Görünen h1 "Satış" (KATMAN) · arama satırı stok/fiyat (Api/Urun) · "Ödeme" türü ucu (Cari/Api) — § 26.6 ile aynı.
- Kurucuya not: müşteri tüzel kişiyse (VKN) TCKN kutusu boş kalır ve sorulur; satıcı türünü müşteri kaydından otomatik "mükellef" yapmak bir vergi sınıflandırması olduğundan **yapılmadı** (mali müşavir teyidi olmadan koda yazılmaz).

### 27.7 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) ilgili testler 1206/1206 ✓ · (3) denetim TEMİZ ✓ · (4) 6 mutasyon kırmızı + cmp ✓ · (5) canlı 70 ölçüm + 10 görüntü ✓ · (6) test kiracıları silindi (PAKETSATIS-3% = 0) ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 28. 29.09 t15 koşumu (06:10 – 07:05) — istemdeki "önceki doğrulamada kalan" 4 tasarım hakemi bulgusu (1 KRİTİK + 3 YÜKSEK)
DEVİR NOTU 4 ("kod yazma") ile aynı istemdeki "önce bunları kapat" listesi çelişiyor; t9–k2 ile aynı yorumla **yalnız bu dört bulgu için** kod yazıldı (ağaçtaki iş korundu, başka kod değişmedi). Önce ağaç ↔ rapor karşılaştırıldı: KRİTİK Takas bulgusu kodda KAPANMAMIŞTI (takas POST gövdesinde ödeme türü yok, `TevsikTutar` takas için 0 → istemci kilidi hiç çalışmıyordu). Migration yok · yeni uç/varlık yok · eşik SAYISI koda gömülmedi (had yine sunucunun `/tevsik/gunluk` değeri) · mevcut DOM id'leri değişmedi · Islemler.razor 1398 satır.

### 28.1 Bulgu → kök neden → yapılan → kanıt
| # | Bulgu | Kök neden (koda bakıldı) | Yapılan | Kanıt |
|---|---|---|---|---|
| KRİTİK | Takas/Değişim farkı 30.000 ₺'yi aşınca sunucu "ödeme türünü kart ya da havale yapın" diyor ama ekranda alan yok; Kaydet etkin | Sunucu `TakasDto.OdemeTuru` (NAKIT/KART/HAVALE, varsayılan NAKIT) ZATEN kabul ediyor ve yalnız NAKİT farkı `tevsik.Dogrula`'ya veriyor (SatisModulu.cs ~2312); istemci alanı hiç göndermiyordu, `TevsikTutar` takasta fiş toplamı (kalem yok → 0) | (1) `Islemler.Odeme.cs` `TevsikTutar`: `"TAKAS" => |takas.Fark|` (sunucuyla aynı ölçüt); `TevsikOnerisiGerekli` takası satış gibi denetler; (2) `FisOdemeKasa.razor`: TAKAS'ta ödeme türü seçicisi PRO'da her zaman, BASİT'te yalnız kapı açılınca (fark had üstü) ya da nakit dışı seçilmişken — seçilen kart/havale BASİT'te "Nakit · sistem belirledi" diye gizlenmez (`ZorunluGorunur` = kapı ∨ tür≠NAKIT); (3) POST `/islemler/takas` gövdesine `odemeTuru` (NAKİT'te sunucunun varsayılanıyla aynı değer); (4) kilit sebebi bantla AYNI cümle ("Bu tutarda nakit alınamaz — tamamını kart ya da havaleyle alın."; fark müşteriye ödeniyorsa "ödenemez — ödeyin"); Peşin kilidi takasta yalnız NAKİT'te (sunucu kartlı takasta kapıyı çağırmaz) | Testler `T15_Takas_farki_HADDI_asinca_odeme_turu_sorulur_nakitte_KILITLI_kartla_govdeye_girer` (BASİT+PRO), `T15_BASIT_Takas_farki_HAD_altinda_odeme_sorulmaz_govdede_NAKIT` · canlı: BASİT/PRO fark 89.176,68 ₺ → seçici görünür, Kaydet PASİF + cümle; KART → ETKİN (24 ölçüm) · API: aynı gövde NAKIT → **400** "Tevsik zorunluluğu: 89.327,99 TL nakit ödeme, 30.000,00 TL haddini … aşıyor", KART → **200** `farkTl 89327.99`; SQL `hareketler`: `TAKAS · KART · 89327.99 · -13.0072` |
| YÜKSEK | PRO 1024'te birincil "Kaydet ve yazdır" alt satırın SOLUNA düşüyor (x292), gerekçe başka satırda | `.tuval-pro .islem-alt-satir` flex-wrap, `justify-content` başlangıç; sığmayan Kaydet yeni satırın başına iniyordu | `tuval-satis.css` (c): `.islem-sayfa.tuval-pro .islem-alt-satir { justify-content: flex-end; }` — "+ Ayrıntı ekle" solda (margin-right:auto) kalır, sarmalanan birincil satırın SAĞINDA, kilit sebebi hemen altında sağda | canlı PRO 1024 karma-bol: Kaydet önce `[292,1279]` → sonra `[823,1279,177]` (sağ kenar 1000 = kap sağı), kilit altında sağa yaslı; satis-kart/satis-nakit-peşin 1024 aynı; 1280/1440 değişmedi (tek satır) · test `T15_css_…` |
| YÜKSEK | K7 MASAK kutusunda Doğum Tarihi/Adres Uyruk'tan 8 px aşağıda; "Kurdan geldi" rozeti fiyat kutusuna 7,5 px biniyor | Uyruk hücresi (`#masak-uyruk-alan`, seçici + ipucu) esnek kutu → ızgara raylarına (subgrid) oturmuyordu; rozet hücrenin dibine `bottom:0` yaslı, alt ray boş → kutunun üstüne biniyordu | `tuval-satis.css` (a)(b), yalnız `.islem-sayfa` kapsamında (MasakFormu bileşenine dokunulmadı): Uyruk hücresi ve seçicisi rayları devralır, ipucu alt raya; fiyat hücrelerinin (`#altin/#hurda/#takas-eski-fiyat-sar`) altında rozet boyu kadar yer | canlı ölçüm (enjeksiyonla önce, sonra dosyadan): Doğum/Uyruk/Adres kutu üstü **913/905/913 → 913/913/913** (BASİT 1440), PRO 1280 1066/1066/1066; rozet üst − kutu alt **−8 px → +18 px** (bindirme yok); hurda-masak 12 ölçümde K7 = 0 |
| YÜKSEK | Nakit uyarı kutusu nakit yokken de kalıcı; PRO "Aşıldı" nakitsiz fişte; İade'de aynı engel iki ayrı metinle | `FisNakitUyari.Gorunur` yalnız eşiğe yakınlığa bakıyordu; `ProNakitAsildi` ödeme aracına bakmıyordu; iade kilit sebebi `NakitHaddiKilit`, bant `NakitBantMetni` | `FisNakitUyari`: `NakitVar` parametresi (sayfa `NakitOdemede` verir) → nakit yoksa `[hidden]`; `ProNakitAsildi`: yalnız ödemede nakit varken ya da Peşin Müşteri'de (sunucu Peşin'de kartla da kapıyı işletir) "Aşıldı"; iade ve takasta kilit sebebi = bant cümlesi (tek metin) | Testler `T15_nakit_notu_ODEMEDE_NAKIT_yokken_gizli_nakitte_gorunur`, `T15_PRO_yasal_kapi_gercek_musteride_KARTLA_odenen_fiste_ASILDI_demez_nakitte_der`; canlı BASİT satis-kart (Anadolu, 95.244,64 ₺ Kart) `nakitNotu=false` 6/6; PRO satis-kart "Uygun" 6/6, Peşin+Nakit "Aşıldı" (doğru) |

Bilinçli değişen eski test: `T11_nakit_notu_…_karta_gecince_gorunur` → `…_karta_gecince_de_GIZLI` (t11'in "karta geçince not görünür" davranışı hakemin bu bulgusuyla ters; metin aynı kalır, not gizli).

### 28.2 Gerçek tarayıcı (başsız Chrome CDP 9908/9909, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3NJY1MX6S9SH50Z6DX2VEZY) — `demo-tohumla.js --prova-kiraci` (ilk koşum 40 sn'de kur alamadı → `--eposta` ile sürdürüldü, 12 ürün, 9 satış) + `t15/ek-tohum.py` (Anadolu Holding A.Ş., 2 kasa, 1 POS). Tek kullanımlık rastgele `Sistem__Anahtari`, değeri hiçbir çıktıya yazılmadı. Betik `/private/tmp/kuyumcu-serit/SATIS-3/t15/t15.mjs`: 6 durum × {BASİT, PRO} × {1440 a, 1280 a, 1024 a, 1280 k, 375 a, 375 k} = **72 ölçüm** + PRO alt şerit düzeltmesinden sonra 24 yeniden ölçüm (`m/olcum.jsonl`, `m/olcum-t15a.jsonl`).
| Durum | Mod | Kaydet | Kilit sebebi | Ödeme seçici | Nakit notu | PRO nakit kuralı | K1..K13 kusur | Yatay kaydırma |
|---|---|---|---|---|---|---|---|---|
| takas-ustu (fark 89.176 ₺, Nakit) | BASİT | PASİF | "Bu tutarda nakit alınamaz — …" | görünür | — | — | 0 (6/6) | 0 |
| takas-ustu | PRO | PASİF | aynı | görünür | — | — | 0 | 0 |
| takas-kart | BASİT/PRO | ETKİN | — | görünür (Kart) | — | — | 0 | 0 |
| satis-kart (Anadolu, Kart) | BASİT | ETKİN | — | — | **gizli** | — | 0 | 0 |
| satis-kart | PRO | ETKİN | — | — | — | **Uygun** | 0 | 0 |
| satis-nakit-pesin (95 bin) | BASİT/PRO | PASİF | "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." | — | — | Aşıldı (doğru) | 0 | 0 |
| karma-bol (+ Başka ödeme ekle) | PRO 1024 | PASİF | aynı, Kaydet'in altında SAĞDA | — | — | Aşıldı (Peşin) | 0 | 0 |
| hurda-masak (42,5 g, 256 bin) | BASİT/PRO | PASİF | "30.000 ₺ üstündeki fişte nakit kullanılamaz — ödeme türünü kart ya da havale yapın." | görünür | — | — | 0 (K7 0) | 0 |
(K4 = sayfa boyu ve K2_kartSagYuzde = sütun oranı bilgi amaçlıdır, kusur sayılmaz.) Görüntüler Read ile GÖRÜLDÜ: takas-ustu BASİT 1440 (Ödeme Türü + Kasa satırı, Fark 89.176,68 ₺, Kaydet pasif + cümle), takas-ustu PRO karanlık 1280, PRO 1024 karma-bol (önce Kaydet solda → sonra sağda, sebep altında), hurda-masak BASİT 1440 (MASAK üç kutu aynı hizada, rozet kutunun altında), satis-kart BASİT+PRO 1440 yan yana (not yok · "Uygun"). Kopyalar: `ham/TUVAL-KARSILASTIRMA/SATIS-3-senaryo-t15-*.png` (8 dosya); tüm 72 görüntü `/private/tmp/kuyumcu-serit/SATIS-3/t15/m/foto/`.

### 28.3 Testler · mutasyon · derleme · denetim
- `dotnet build Kuyumcu.sln` (son mutasyondan sonra temiz) → **0 Uyarı 0 Hata**.
- Yeni test **6** (5 yöntem; takas testi BASİT+PRO). İlgili küme (SATIS3|SATIS2|S22|TS1|TS2|S24|S21|Takas|Iade|Masak|Islem|Fis|CumaS|OlcekTek, t2 DB): **1403 → T11 uyarlamasından sonra yeşil**; T11+T15 11/11.
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t15/<kod>.orj` → perl tek eşleşme → filtreli test → geri kopya → `cmp`), betik `t15/mut.sh`:
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T15-1 | takas tevsik tutarı 0 | T15_Takas_farki_HADDI_… (BASİT, PRO) | BİREBİR |
| M-S3-T15-2 | takas gövdesinden `odemeTuru` düşer | T15_Takas_farki_HADDI_… (BASİT, PRO), T15_BASIT_Takas_farki_HAD_altinda_… | BİREBİR |
| M-S3-T15-3 | nakit notu nakit yokken de görünür | T15_nakit_notu_…, T11_nakit_notu_…_de_GIZLI | BİREBİR |
| M-S3-T15-4 | PRO "Aşıldı" ödeme aracına bakmaz | T15_PRO_yasal_kapi_… | BİREBİR |
| M-S3-T15-5 | TAKAS'ta ödeme seçicisi çizilmez | T15_Takas_farki_HADDI_… (BASİT, PRO) | BİREBİR |
| M-S3-T15-6 | takasta kilit sebebi eski (ikinci) metin | T15_Takas_farki_HADDI_… (BASİT, PRO) | BİREBİR |
| M-S3-T15-7 | PRO alt şerit sona yaslı kuralı silinir | T15_css_MASAK_uyruk_rayda_rozet_kutuya_binmez_PRO_birincil_sagda | BİREBİR |
**7/7 kırmızı.**
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pSATIS-3-t15/t15.trx`, t2): **7037 · 7035 geçti · 2 kırmızı** = `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (**ORTAK-1**, önceki koşumlarda da listede) ve `Dogrulayici.ZOISLEMASinir12Testleri.ZOS12_yatma_adaylari_kalan_0_01_…` (**ZO-ISLEM-A**, POS yatma adayları; Islemler/Fis'e dokunmaz). **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 28.4 Temizlik
API 5162, vekil 5163, Chrome 9908/9909 durduruldu; `lsof` dördü **boş**; 5198/5199/5200'e dokunulmadı. Kiracı 01M3NJY1MX6S9SH50Z6DX2VEZY `kiraci-temizle.sh --uygula` (rc 0) + `kiraci_davetleri` satırı (1) silindi; `PAKETSATIS-3%` = **0**. `tenants`: başlangıç **16** → son **14** (azalma başka şeritlerin kendi kiracılarını silmesinden; bu paketin kiracısı yok). Anahtar/fikstür dosyaları `printf ''` ile boşaltıldı. Proje ağacında bu paketin `.mutorj/.orj/.bak/.tmp` artığı 0 (ağaçtaki `Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak` bu paketin değil).

### 28.5 Değişen dosyalar (t15) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Pages/{Islemler.razor (1398 satır — 2 satır içi değişiklik), Islemler.Odeme.cs, Islemler.Pro.cs}` · `Kuyumcu.Web/Bilesenler/Fis/{FisOdemeKasa.razor, FisNakitUyari.razor}` · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona (a)(b)(c); sabit renk yok) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs`. Yeni DOM id yok; mevcut id'ler aynı. Yeni varlık/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı. Takas POST gövdesine sunucunun zaten tanımladığı `odemeTuru` alanı eklendi (NAKİT'te sunucu varsayılanıyla aynı sonuç — hakemin istediği "ödeme türü seçimi"; sözleşme izin veriyor). Razor sayfası: yalnız `Pages/Islemler.razor` (+ partial) — § 12 ile aynı. Mevzuat: yeni hüküm yok; nakit kuralı ve had sunucudan (takas için sunucunun kendi yorumu — "işlem tutarı kuralı takasa uygulanmaz, yalnız nakit fark", SatisModulu.cs yorumu: mali müşavir teyidi bekliyor — değişmedi).

### 28.6 Açık kalanlar (sahibiyle)
- `OrtakGorunumSaflik.O8_…` — **ORTAK-1**; `ZOS12_yatma_adaylari_…` — **ZO-ISLEM-A**.
- Takasta KART seçilip şubede birden çok POS cihazı varsa sunucu cihaz ister (TakasDto.PosCihazId); takas formunda POS seçicisi yok — tek POS'ta sunucu otomatik seçer (canlıda tek POS, 200). Çok POS'lu şubede ayrı iş.
- Önceki açıklar değişmedi: görünen h1 "Satış" (KATMAN) · arama satırı stok (Api/Urun) · "Ödeme" türü ucu (Cari/Api).
- PRO'da takas/hurda ödeme türü altındaki ayrıntılı tevsik ipucu satırı (PRO tam bilgi kuralı, t10'dan beri) duruyor; BASİT'te görsel gizli.

### 28.7 Durum
Paket kapısı: (1) derleme 0 hata 0 uyarı ✓ · (2) ilgili testler yeşil, T11+T15 11/11 ✓ · (3) denetim TEMİZ ✓ · (4) 7 mutasyon kırmızı + cmp ✓ · (5) canlı 96 tarayıcı ölçümü + API 400/200 + SQL ✓ · (6) test kiracısı silindi (PAKETSATIS-3% = 0) ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 29. 29.09 t16 koşumu (10:55 – 12:05) — önceki doğrulamanın (KALDI) 1 KRİTİK + 2 ORTA + 4 DÜŞÜK bulgusu
Ağaçtaki iş korundu; yalnız aşağıdaki bulgular için kod yazıldı. Migration yok · yeni uç/varlık yok · mevzuat hükmü eklenmedi (had/nakit kuralı yine sunucudan) · mevcut DOM id'leri değişmedi · yeni id `islem-pos` (`<önek>-<alan>`) · `Islemler.razor` **1399** satır (S21 sınırı ≤ 1.400).

### 29.1 Bulgu → kök neden → yapılan → kanıt
| # | Bulgu | Kök neden | Yapılan | Kanıt |
|---|---|---|---|---|
| KRİTİK | Takas has gram fiyatına belirsiz "1.500" → alan hatası görünürken Kaydet ETKİN; ~6 sn sonra kur alanın üzerine yazıyor, kullanıcının görmediği fiyat POST ediliyor | Doğrulayıcının teşhisi doğru: `SayisalAlan.Degisti` geçersiz/belirsiz metinde `DegerChanged` ateşlemiyordu → `TakasGirisi/HurdaGirisi.FiyatElle=false` kalıyor, `KurYansit` (Islemler.razor) eski/kur değerini yazıyor, `OnParametersSet` ham metni ve iç hatayı siliyordu; Kaydet kapısı alanın geçersiz olduğunu bilmiyordu | (a) `Bilesenler/Form/SayisalAlan.cs`: **isteğe bağlı** `GecersizdeBosalt` parametresi (varsayılan KAPALI — öbür tüm Sayi/Para/Gram kullanıcılarının davranışı değişmez): açıksa geçersiz/belirsiz metinde `DegerChanged(null)` bildirilir; ham metin ve iç hata korunur (Deger null ⇒ `OnParametersSet` kutuyu ezmez). (b) `FisTakas.razor` `#takas-eski-fiyat` ve `FisHurdaAlis.razor` `#hurda-fiyat` bunu açar → ilk tuşta `FiyatGirildi(null)` → `FiyatElle=true`, kur artık üzerine YAZMAZ, `takas.EskiFiyat/hurda.Fiyat=null` ⇒ `Gonderilebilir=false`. (c) Kilit sebebi (UX-S30) alanı gösterir: `Satis3Sozlugu.FiyatAlaniDuzeltin` TR "Has gram fiyatını düzeltin — kutunun altındaki uyarıya bakın." / EN | bUnit: `H15_Takas_belirsiz_1_500_hata_gorunurken_Kaydet_POST_atmaz` ve `T15S_Turkce_giris_belirsiz_1_500_POST_YOK` **yeşil**; yeni `T16_Takas_belirsiz_…_kur_UZERINE_YAZMAZ` (BASİT+PRO — `KurYansit` yansımayla çağrılır, değer "1.500" kalır, POST 0; "1500" yazınca gövdede **1500**) ve **hurda için** `T16_Hurda_belirsiz_…` (BASİT+PRO). Canlı (5162/5163, başsız Chrome CDP 9910; `t16/belirsiz.mjs`, doğrulayıcının `belirsiz2.mjs`'inin kopyası): takas BASİT/PRO ve hurda BASİT/PRO, dördünde de yazınca · blur · **6 sn sonra** · Kaydet'e basınca `{deger:"1.500", ariaInvalid:"true", alan altı "Ondalık için virgül kullanın: 1,5 ya da 1500", kaydet:"PASIF", title:"Has gram fiyatını düzeltin — …"}`, **POST 0**. Düzeltme yolu: "1500" → Kaydet ETKİN, gövde `eskiUrunBirimFiyatTlHasGram:1500`, yanıt 200; SQL `hareketler`: `TAKAS · KART · 93611.30` (= yeni ürün − 0,916 × **1.500**; kurla 6.560 olsaydı ≈ 88.985) |
| ORTA | "Bu paketten yeni kırmızı 0" tutmuyordu (T15S kırmızı) | Yukarıdaki kök neden + testin son satırı | Kök neden düzeltildi. T15S'nin SON iddiası `Assert.Contains("belirsiz", …)` kanonik mesajla çelişiyordu: belirsizlik mesajı `SayiGirisiSozlugu.Tr.Belirsiz` = "Ondalık için virgül kullanın: 1,5 ya da 1500" ("belirsiz" kelimesi yok; `P78SayiGirisiTestleri` ve `EMANET1SinirTestleri` bu metni BİREBİR sabitliyor — değiştirmek başka şeritleri kırar). Testin amacı (alan altında mesaj) korunarak iddia kanonik metne çevrildi + `aria-invalid=true` ve kutu değeri "1.500" iddiaları EKLENDİ (daha sıkı). POST 0 iddiası aynen duruyor | Tam süit (aşağıda) bu paketten 0 kırmızı |
| ORTA | Önceki doğrulama koşumunun kiracı/davet artıkları | — | Doğrulayıcı kendi kiracısını silmişti; bu koşumda başlangıç `tenants` = **16**, `PAKETSATIS-3%` = 0. Eski iki davet satırı (01M3NGJ0ZCD61FB3N2Z3R1XDVB, 01M3NHFRWQCTX0BVXZ3C6CA162) başka koşumların — DOKUNULMADI (orkestratör/kurucu) | § 29.4 |
| DÜŞÜK | BASİT'te kilit sebebi aynı cümleyle iki kez (üst bant + Kaydet altı) | `BasitSatisAkisi` bandı `NakitBantMetni`ni, kilit satırı aynı cümleyi basıyordu | `Islemler.Odeme.cs` `KilitBantlaAyni(kilit)`: BASİT + bant açık + MASAK dışı + nakit bandı var + metin AYNI ⇒ Kaydet altındaki satır `gorunmez` (DOM'da ve düğmenin `title`'ında kalır — ekran okuyucu/UX-S30) | `T16_kilit_sebebi_bantla_ayniysa_BASITte_tek_kez_gorunur` (BASİT gizli / PRO görünür). Canlı takas-üstü: BASİT 1440/1280/375 görünen aynı metin **1** (yalnız `#islem-yasal-kapi`), PRO 1440 **1** (yalnız `#islem-kaydet-kilit`, PRO'da bant yok) |
| DÜŞÜK | PRO 1440 satış-kart'ta müşteri adı seçicide kesiliyor (K10) | `.tuval-pro #islem-ust-satir .secici-tetik` `white-space:nowrap; text-overflow:ellipsis`; "Anadolu Döküm Toptan Kuyum" 227 px, kutu içi 214 px | `tuval-satis.css` sonuna: `.tuval-pro #islem-ust-satir > .alan:first-child .secici-metin { white-space: normal; overflow-wrap: anywhere; text-overflow: clip; line-height: 1.15; }` — kontrol ölçüsü (46 px) DEĞİŞMEDİ (ilk denemede `height:auto` OLCEK-1 envanter testi `OL26`'yı kırdı → geri alındı; metin kutu içinde iki satıra iner: 2 × 16 × 1,15 ≈ 37 px < 44 px) | Canlı: PRO 1440 aydınlık/karanlık `K10_elipsis 0, K10b_kesik 0` (önce **1/1**), metin `{w:214, h:37, sw=cw}`; PRO 1280/1024/960/375 ve BASİT 1280 K1..K13 = 0. Test `T16_css_PRO_musteri_adi_kutuda_sarar_kesilmez` |
| DÜŞÜK | `Islemler.Odeme.cs`'te yetim `<summary>` | `KarmaKilitSebebi`nin belgesi `NakitOdemede`'nin önünde kalmıştı | Belge `KarmaKilitSebebi()`nin önüne taşındı | derleme 0 uyarı |
| DÜŞÜK | Takasta KART seçilince POS cihazı seçilemiyor | Takas gövdesinde `posCihazId` yoktu; `TakasDto.PosCihazId` ZATEN var ve sunucu çok cihazlı şubede cihazsız isteği REDDEDİYOR (SatisModulu.cs TakasDto yorumu: "birden çok varsa istek REDDEDİLİR — fail-closed") | `FisOdemeKasa.razor`: TAKAS + KART + aktif POS > 1 ⇒ `#islem-pos` seçicisi (ödeme türüyle aynı `.alan` sarmalayıcısında; tek cihazda seçici YOK — UX-S22, sunucu seçer). `Islemler.Odeme.cs` `takasPosId/TakasPosEksik/TakasPosId`; Kaydet kilidi + sebep `Satis3Sozlugu.PosCihaziSecin` "Kartın geçtiği POS cihazını seçin."; gövdeye `posCihazId` (nakitte/tek cihazda null). `tuval-satis.css`: `#islem-pos-sar { row-gap: 4px; }` (K7 — ilk ölçümde kutu 4 px aşağıdaydı) | `T16_Takas_KART_cok_POSta_cihaz_secilmeden_KILITLI_secince_govdede` (BASİT+PRO), `T16_Takas_KART_tek_POSta_secici_YOK_govdede_cihaz_null`. Canlı (2 POS: Ziraat + Garanti): KART → seçici görünür, Kaydet PASİF + cümle; Garanti seçilince ETKİN; POST gövdesi `posCihazId` = Garanti, yanıt **200** `farkTl 88961.42`; SQL `pos_tahsilatlari ⋈ hareketler`: `TAKAS · KART · 88961.42 · Garanti POS` (tek POS'lu ilk takas `Ziraat POS` — sunucu seçti). Hiza: ödeme/POS etiket 221/221, kutu 244/244 (BASİT 1440); PRO 1280 319/319 · 342/342; PRO 1024 341/341 · 364/364 |

Not (gözlem, bu paketin kapsamı dışı — sahibi sunucu MASAK kümülatifi): aynı müşteriyle aynı gün ikinci takasta sunucu 400 "MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur (bugün kümülatif 192.437,17 TL…)" döndü; ekran kimlik formunu önceden açmadı (istemci `/masak/gereksinim`'e yalnız bu işlemin tutarını soruyor). Hata bandı sunucu mesajını gösterdi (sessiz değil). Ayrı iş olarak orkestratöre.

### 29.2 Gerçek tarayıcı (başsız Chrome CDP 9910, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3P3579B5B5F6M2291FPE1B9) — `demo-tohumla.js --prova-kiraci` (24 sn; kur geldi: 12 ürün, 6 cari, 9 satış) + `t16/ek-tohum.py` (Anadolu Holding A.Ş., 2. kasa, 1 POS) + `t16/pos2.py` (2. POS). Tek kullanımlık rastgele `Sistem__Anahtari` (48 hane), değeri hiçbir çıktıya yazılmadı; dosyalar sonra `printf ''` ile boşaltıldı.
- Hedefli ölçümler: `belirsiz.mjs` (takas/hurda × BASİT/PRO, 4 an × 4), `k10.mjs` (PRO 1440 a/k, 1280 a, 1024 k, 960 a, 375 k; BASİT 1280 a), `dup.mjs` (takas-üstü BASİT 1440/1280/375 k, PRO 1440), `pos.mjs` (BASİT 1440 a, 375 k; PRO 1440 a, 1280 k, 1024 a).
- Genel tekrar (t15 betiği, güncel ağaç): 6 durum × {BASİT, PRO} × {1440 a, 1280 a, 1024 a, 1280 k, 375 a, 375 k} = **72 ölçüm** (`t16/m/olcum.jsonl`): K1, K3, K5, K7, K8, K9, K9b, K10, K10b, K11, K12, K13 **toplam 0**; yatay kaydırma en çok **0**. (K2 sütun oranı ve K4 sayfa boyu bilgi amaçlı.) takas-üstü BASİT kilit satırı `[1×1]` (görsel gizli, bant söylüyor); takas-kart artık 2 POS'ta "Kartın geçtiği POS cihazını seçin." ile PASİF (doğru — cihaz seçilmedi).

| Durum | Mod | 1440 a | 1280 a | 1024 a | 1280 k | 375 a | 375 k |
|---|---|---|---|---|---|---|---|
| takas-ustu · takas-kart · satis-kart · satis-nakit-pesin · karma-bol · hurda-masak | BASİT | 0 | 0 | 0 | 0 | 0 | 0 |
| aynı 6 durum | PRO | 0 | 0 | 0 | 0 | 0 | 0 |

Görüntüler Read ile GÖRÜLDÜ: takas belirsiz BASİT (kutu kırmızı "1.500", alan altı mesaj, "Elle girildi", Kaydet pasif + alanı gösteren cümle), hurda belirsiz PRO karanlık (aynı; "Tutar: —"), PRO karanlık 1440 müşteri adı kutu içinde iki satır (kutu 46 px, özet kutusuyla aynı boy), BASİT karanlık 375 takas-üstü (tek turuncu bant, Kaydet altında tekrar yok), takas POS BASİT 1440 ve PRO karanlık 1280 (Ödeme Türü | POS cihazı aynı hizada), hurda-masak BASİT 1440 (bant ile kilit farklı cümle — tekrar değil). Kopyalar: `ham/TUVAL-KARSILASTIRMA/SATIS-3-senaryo-t16-*.png` (6 dosya); tümü `/private/tmp/kuyumcu-serit/SATIS-3/t16/foto/` ve `t16/m/foto/`.

### 29.3 Testler · mutasyon · derleme · denetim
- `dotnet build Kuyumcu.sln` (son mutasyondan ve son düzeltmeden sonra temiz) → **0 Uyarı 0 Hata**.
- Yeni test **10 durum** (6 yöntem): `T16_Takas_belirsiz_…` ×2, `T16_Hurda_belirsiz_…` ×2, `T16_kilit_sebebi_bantla_ayniysa_…` ×2, `T16_Takas_KART_cok_POSta_…` ×2, `T16_Takas_KART_tek_POSta_…`, `T16_css_PRO_musteri_adi_…`. Değişen test: `T15S_Turkce_giris_belirsiz_1_500_POST_YOK` son iddiası (gerekçe § 29.1 ORTA).
- İlgili küme (SATIS3|SATIS2|S22|TS1|TS2|S24|S21|Takas|Iade|Masak|Islem|Fis|CumaS|OlcekTek|P78|Hurda|Bicim|Form, t2 DB): **1861/1861 yeşil** (son koşumdan önce S21 1.400 satır sınırı ve OL26 envanter kırmızısı yakalandı → düzeltildi).
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t16/<kod>.orj` → perl tek eşleşme → build → filtreli test → geri kopya → `cmp`), betik `t16/mut.sh`, günlük `t16/mut-ozet.log`:

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T16-1 | SayisalAlan: `if (GecersizdeBosalt)` → `if (false)` | H15_Takas_belirsiz_1_500_…, T15S_Turkce_giris_belirsiz_1_500_POST_YOK, T16_Takas_belirsiz_… ×2, T16_Hurda_belirsiz_… ×2 | BİREBİR |
| M-S3-T16-2 | FisTakas: `GecersizdeBosalt="true"` silinir | H15_Takas_belirsiz_…, T15S_…, T16_Takas_belirsiz_… ×2 | BİREBİR |
| M-S3-T16-3 | FisHurdaAlis: `GecersizdeBosalt="true"` silinir | T16_Hurda_belirsiz_… ×2 | BİREBİR |
| M-S3-T16-4 | kilit sebebi: hurda fiyat koşulu `false &&` | T16_Hurda_belirsiz_… ×2 | BİREBİR |
| M-S3-T16-5 | `KilitBantlaAyni` → hep false | T16_kilit_sebebi_…(BASIT) | BİREBİR |
| M-S3-T16-6 | `TakasPosEksik` → hep false | T16_Takas_KART_cok_POSta_… ×2 | BİREBİR |
| M-S3-T16-7 | takas gövdesinde `posCihazId = null` | T16_Takas_KART_cok_POSta_… ×2 | BİREBİR |
| M-S3-T16-8 | `KurYansit`: `!takas.FiyatElle` koşulu düşer (kur hep yazar) | T16_Takas_belirsiz_… ×2 | BİREBİR |
| M-S3-T16-9 | css: müşteri adı `white-space: normal` silinir | T16_css_PRO_musteri_adi_… | BİREBİR |
**9/9 kırmızı**, hepsi geri alındı (`cmp` birebir); son temiz build 0/0. (`#islem-pos-sar` hiza kuralı mutasyonlardan SONRA eklendi; `T16_css_…` testi onu da iddia eder.)
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pSATIS-3-t16/t16.trx`, t2): **7101 · 7100 geçti · 1 kırmızı** = `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (**ORTAK-1** — `OrtakGorunumMetinleri` 15 yerine 16 alan; önceki koşumlarda da listede). Doğrulayıcının 4 kırmızısından T15S (bu paket) **kapandı**; K5 IslemDefteri ve SATISTAN_ALISA bu koşumda yeşil. **Bu paketten yeni kırmızı 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0; `OLCEK-1: YUZ_OLCU_ENVANTER_DISI 0`).

### 29.4 Temizlik
API 5162 (PID dosyası), vekil 5163, Chrome 9910 durduruldu; `lsof` üçü **boş**; 5198/5199/5200'e dokunulmadı. Kiracı 01M3P3579B5B5F6M2291FPE1B9 `kiraci-temizle.sh --uygula` (rc 0) + kendi `kiraci_davetleri` satırı (1; `kullanan_tenant_id` ile) silindi; `PAKETSATIS-3%` = **0**; `tenants` başlangıç **16** → son **16**. Anahtar/fikstür dosyaları `printf ''` ile boşaltıldı. Chrome profili `/private/tmp/kuyumcu-serit/SATIS-3/t16/profil-9910` (193 MB) — hook `rm`'i engellediği için silme kurucuya/orkestratöre (disk %96 dolu). Proje ağacında bu koşumun `.mutorj/.orj/.bak/.tmp` artığı 0 (yedekler yalnız `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t16/`).

### 29.5 Değişen dosyalar (t16) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Pages/{Islemler.razor (1399 satır), Islemler.Odeme.cs}` · `Kuyumcu.Web/Bilesenler/Fis/{FisTakas.razor, FisHurdaAlis.razor, FisOdemeKasa.razor}` · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona 2 kural; sabit renk yok) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (2 anahtar TR+EN) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs` · `Kuyumcu.Tests/Dogrulayici/SATIS3SinirTestleri.cs` (T15S son iddiası).
**Alan dışı dokunuş (gerekçeli):** `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` — doğrulayıcının istediği düzeltme (a) "Para/Gram geçersiz ya da belirsiz durumu sayfaya bildirsin" yalnız bu tabanda yapılabilir. EKLEMELİ ve varsayılan KAPALI bir parametre (`GecersizdeBosalt`); açmayan tüm bileşenlerin davranışı birebir aynı (P78/EMANET1/Siparis/P12 belirsizlik testleri yeşil). Dosya şu an hiçbir eşzamanlı şeridin listesinde değil (OLCEK-1'in yalnız `Form/Secim.razor`'ı var).
Razor sayfası: yalnız `Pages/Islemler.razor` (+ partial) — § 12 ile aynı. Yeni varlık/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı. Takas POST gövdesine sunucunun ZATEN tanımladığı `posCihazId` eklendi (nakitte/tek cihazda null = eski gövdeyle aynı sonuç). Mevzuat: yeni hüküm yok.

### 29.6 Açık kalanlar (sahibiyle)
- `OrtakGorunumSaflik.O8_…` — **ORTAK-1**.
- Aynı müşteride gün içi kümülatif MASAK kimlik gereksinimini ekran önceden bilmiyor (sunucu 400 ile söylüyor) — **sunucu/istemci MASAK sözleşmesi**, ayrı iş.
- Eski davet satırları 01M3NGJ0ZCD61FB3N2Z3R1XDVB, 01M3NHFRWQCTX0BVXZ3C6CA162 ve `/private/tmp/kuyumcu-serit/dogrulama-pSATIS-3` (1,0 GB) + `SATIS-3/t16/profil-9910` (193 MB) — silme **kurucu/orkestratör** (hook).
- Önceki açıklar değişmedi: görünen h1 "Satış" (KATMAN) · arama satırı stok (Api/Urun) · K9 (KATMAN).

### 29.7 Durum
Paket kapısı: (1) derleme 0 hata 0 uyarı ✓ · (2) ilgili testler 1861/1861, T16 10/10, H15/T15S yeşil ✓ · (3) denetim TEMİZ ✓ · (4) 9 mutasyon kırmızı + cmp ✓ · (5) canlı: belirsiz takas/hurda × BASİT/PRO POST 0, düzeltince 200 + SQL; POS 200 + SQL; 72 genel + hedefli ölçümler K1..K13 = 0 ✓ · (6) test kiracısı silindi, tenants 16 → 16 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 30. 29.09 t17 koşumu (12:15 – 13:45) — "ekranda görünen ≠ gönderilen" HATA SINIFI (orkestratör 12:10 + hasım doğrulayıcının 2 KRİTİK'i)
Ağaçtaki iş korundu. Migration yok · yeni uç/varlık yok · mevzuat hükmü eklenmedi · mevcut DOM id'leri değişmedi · `Islemler.razor` **1399** satır (S21 ≤ 1.400; değişiklikler mevcut satırlarda). Kaydet()/POST gövdeleri DEĞİŞMEDİ (yalnız kapı).

### 30.1 Kök neden ve çözüm (tek kapı — kutu kutu yama DEĞİL)
- **Kök neden (doğrulayıcının teşhisi doğru):** `SayisalAlan.Degisti` çözülemeyen/belirsiz metinde (`"1.500"`; tarayıcı her tuşta input atar → `"1.5"`/`"1.50"` 1,5 diye çözülür, `"1.500"` belirsiz) yalnız t16'da bayrak açılan iki kutuda `DegerChanged(null)` bildiriyordu. Öbür kutularda sayfa ESKİ değeri (teklif fiyatı 10.000 / ara değer 1,5 g) tutuyor, Kaydet açık kalıyor, 6 sn'lik yeniden çizimde `OnParametersSet` kullanıcının metnini siliyordu.
- **Çözüm — `GecersizAlanKapisi` (yeni, `Bilesenler/Form/GecersizAlanKapisi.cs`):** `Islemler.razor` sayfayı `<CascadingValue Value="gecersizAlanlar" IsFixed="true">` ile sarar (satır 32/222, mevcut satırlara eklendi). Sayfanın ve `Bilesenler/Fis/*` + `AltinBacagiPaneli` içindeki HER `Sayi/Para/Gram` kutusu bu kapıyı basamaklı parametre olarak alır ve:
  (a) alan hatasını gösterir — kapıda ayrıştırma hatası çağıranın "zorunlu/sıfırdan büyük" mesajının ÖNÜNE geçer (kullanıcı yazdığının neden anlaşılmadığını görür);
  (b) kendini kapıya "geçersiz" yazar → `Gonderilebilir` başında `!gecersizAlanlar.Var` → Kaydet PASİF (F9 kısayolu da, `Kaydet()` iç koruması da aynı özelliği okur); `KaydetKilitSebebi` kur kontrolünden hemen sonra **alanın etiketiyle** tam cümle: `Satis3Sozlugu.AlanDuzeltin` TR "«{0}» kutusundaki sayıyı düzeltin — kutunun altındaki uyarıya bakın." / EN (has gram fiyatı kutuları t16 cümlesini korur);
  (c) çağırana `DegerChanged(null)` bildirir → gövdeye eski/varsayılan/kur/teklif/ara değer GİTMEZ;
  (d) geçersizken `OnParametersSet` dışarıdan gelen değeri (null→0 dönüşümü, kur, teklif, otomatik kalan) kutunun ÜSTÜNE YAZMAZ; önizleme "eski sayı"yı altına yazmaz.
  Alan DOM'dan kalkınca (satır silindi, katlanır kapandı, tür değişti) `Dispose` kaydı siler (bayat kilit yok). Kapı değişince sayfa yeniden çizilir (çağıranı yalnız alt bileşen olan "Alınan" kutusu için de kilit tazelenir).
- **Diğer işlem formları:** `FisDigerEylem` aynı kapıyı formun ön ekiyle (`avans-`, `siparis-`, `degisim-` …) okur → birincil düğme PASİF, kilit satırı aynı cümle, programatik tık da istek atmaz.
- **Fark satırı dürüstlüğü (canlı görüntüde bulundu):** ödeme satırı "1.500" derken alt çubuk "Fark 0 ₺ ✓" diyordu (satır hesaba 0 giriyordu). `FisVerdimAldim` (BASİT) fark "—" + kırmızı + aynı cümle; `FisOdemeBacaklari` (PRO) fark metni aynı cümle.
- **Canlıda bulunan iki ek kusur (düzeltildi):** (1) silinen ödeme kutusuna "0" basılıyor, ardından yazılan "1,5" **"01,5"** görünüyordu → kapıda odaktaki boş kutu boş kalır; (2) uyarı satırı uzatınca ödeme satırında kutular ±25/±78 px kayıyordu (K7) → `tuval-satis.css` sona 2 kural (uyarılı satır `align-items: start`; BASİT <1400 px sil düğmesi etiket payı kadar aşağıda — PRO'daki `--bacak-etiket` yöntemi). Sabit renk yok.
- **Geriye uyum (diğer şeritler):** kapıyı VERMEYEN her form (Hurda*, Emanet*, Pos* …) BİREBİR eski davranışta — `T17_Kapisiz_formda_SayisalAlan_davranisi_DEGISMEZ` + P78/EMANET1 belirsizlik testleri yeşil; `M-S3-T17-12` (kapısız formda da null bildir) P78 ve EMANET1 testlerini kırdı = sınır testle korunuyor. t16'nın `GecersizdeBosalt` parametresi korunur (eklemeli).

### 30.2 Envanter — Islemler + Fis/* içindeki her sayısal kutu (hepsi kapının altında)
| Kutu | Bileşen | Geçersizken çağırana | Kilit/sebep | Test |
|---|---|---|---|---|
| `takas-eski-gram` | FisTakas | null → Tamam=false | Kaydet · "«Gram Ağırlık (g)»…" | T16D_takas_ESKI…, T17_Takas_ESKI_gram ×2 |
| `takas-eski-fiyat` | FisTakas | null (t16) | Kaydet · FiyatAlaniDuzeltin | H15, T15S, T16_Takas ×2 |
| `takas-yeni-adet`, `takas-yeni-fiyat` | FisTakas | 0 / null | Kaydet · "«Birim Fiyat (₺)»…" | T16D_takas_YENI…, T17_Takas_YENI_fiyat ×2 |
| `hurda-gram`, `hurda-fiyat` | FisHurdaAlis | null | Kaydet · etiket | T17_Hurda_gram ×2, T16_Hurda ×2 |
| `islem-adet`, `islem-birimfiyat` | FisKalemGirisi | 0 / null → kalem EKLENMEZ | Kaydet · etiket; alan altı "Ondalık…" ezilmez | T17_Satis_giris_satiri ×2 |
| `islem-bacak-{i}-tutar`, `islem-bacak-{i}-doviz` | FisOdemeBacaklari | 0 (satır düşer) | Kaydet · "«Tutar»…"; Fark "—" | T17_Karma_odeme_tutari…, T17_Karma_…FARK_tamam_DEMEZ ×2 |
| `altin-gram`, `altin-fiyat` | AltinBacagiPaneli (Fis dışı, sayfanın içinde) | null | Kaydet · etiket | (aynı kapı; K-KUR-4 FiyatElle korunur) |
| `islem-alinan` | FisParaUstu | null | Kaydet · "«Alınan»…" | T17_Para_ustu_alinan |
| `islem-fis-iskonto` | FisIskontoSatiri | null | Kaydet · etiket | (aynı kapı) |
| `islem-vade` | FisOdemeKasa | null | Kaydet · etiket | (aynı kapı) |
| `iade-adet` | FisIade | null | Kaydet · "«İade Adedi»…" | T17_Iade_adedi |
| `iscilik-tutar` | Islemler.razor | null | Kaydet · etiket | (aynı kapı) |
| `transfer-adet` | FisTransfer | null | Kaydet · etiket | (aynı kapı) |
| `avans-tutar`, `avans-mahsup-tutar`, `avans-iade-tutar` | FisDigerAvans | null | `avans-al` PASİF · kilit satırı | T17_Kapora_tutari (canlı: AVANS_ALINDI 1,50) |
| `degisim-geri-adet`, `degisim-yeni-adet`, `degisim-yeni-fiyat` | FisDigerDegisim | null | `degisim-kaydet` PASİF | T17_Degisim_yeni_fiyat |
| `konsinye-adet`, `konsinye-geri-adet-{id}` | FisDigerKonsinye | null → GeriAl reddeder | `konsinye-gonder` PASİF | (aynı kapı) |
| `siparis-fiyat-{id}`, `siparis-kapora` | FisDigerSiparis | null | `siparis-cevir` PASİF, onay AÇILMAZ | T17_Siparis_kapora |
| `rezerv-adet`, `rezerv-duz-adet` | FisDigerRezerv | null → Hatalar reddeder | `rezerv-kaydet` PASİF | (aynı kapı) |
Ham `<input>` sayı kutusu: **0** (grep `inputmode|type="number"|<input` Pages/Islemler* + Fis/* → boş).

### 30.3 Canlı kanıt — GERÇEK tuş olayları (CDP `Input.dispatchKeyEvent`, her karakter ayrı input), başsız Chrome CDP 9911/9913, tarayıcı bölmesi KULLANILMADI; API 5162 (`--no-build`), vekil 5163
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3P8KE6YS50T895GCB7VRQBZ) — `demo-tohumla.js --prova-kiraci` + `t17/ek-tohum.py`. Betik `t17/tustus.mjs`, kayıt `t17/tustus.jsonl`: her kutu için yazınca · blur + **6,5 sn** · Kaydet'e basınca · "1,5" yazınca.

| Kutu | Mod | "1.500" (yazınca / +6 sn / tık) | "1,5" |
|---|---|---|---|
| takas-yeni-fiyat | BASİT, PRO | değer "1.500", aria-invalid=true, alan altı "Ondalık için virgül kullanın: 1,5 ya da 1500", Kaydet PASİF, title "«Birim Fiyat (₺)» kutusundaki sayıyı düzeltin…", **POST 0** (üç anda) | aria-invalid=false, Kaydet ETKİN |
| takas-eski-gram | BASİT (KART), PRO | aynı; title "«Gram Ağırlık (g)»…", **POST 0** | BASİT: Kaydet → gövde `eskiUrunGram: 1.5`, `yeniUrunBirimFiyatTl: 95185.99` → 200; **SQL** `hareketler`: `TAKAS · KART · 86156.45` (= 95.185,99 − 1,5 × 0,916 × 6.571,72) — kiracıda TAKAS satırı **1** (hiçbir "1.500" denemesi deftere yazmadı) |
| hurda-gram | BASİT, PRO | aynı; **POST 0** | Kaydet kilidi sıradaki gerçek eksik: satıcı kimliği |
| islem-birimfiyat | PRO | aynı + **Ekle tıklandı → kalem sayısı 1 → 1** (eklenmedi); **POST 0** | Kaydet kilidi sıradaki gerçek eksik: müşteri |
| islem-bacak-1-tutar | BASİT, PRO | aynı; title "«Tutar»…"; **POST 0** | değer "1,5" (önceki koşumda "01,5" → düzeltildi) |
| avans-tutar (Diğer · Kapora) | PRO | `avans-al` PASİF, kilit "«Tutar» kutusundaki sayıyı düzeltin…", tık → **POST 0** | ETKİN → POST `{"tutar":1.5,…}`; **SQL** `AVANS_ALINDI · NAKIT · 1.50` |

### 30.4 Gerçek tarayıcı ölçümü (sikisma-olc K1..K11 + ekstra K12/K13; betik `t17/olc.mjs`, kayıt `t17/olc.jsonl`) — geçersiz durum ekranı
| Durum | Mod | 1280 açık | 1280 koyu | 375 açık | 375 koyu | 1440 |
|---|---|---|---|---|---|---|
| takas yeni fiyat "1.500" | BASİT | 0 | 0 | 0 | 0 | 0 (açık) |
| takas yeni fiyat "1.500" | PRO | 0 | 0 | 0 | 0 | 0 (koyu) |
| karma ödeme tutarı "1.500" | BASİT | 0 | 0 | 0 | 0 | 0 (açık) |
| karma ödeme tutarı "1.500" | PRO | 0 | 0 | 0 | 0 | 0 (koyu) |
| kapora tutarı "1.500" (Diğer) | PRO | 0 | 0 | 0 | 0 | — |
Toplam **25 ölçüm** (son yayında; karma satırları CSS düzeltmesinden SONRA yeniden ölçüldü — önce K7 ±25/±78 bulundu), kusur **0**, yatay kaydırma **0**, her ölçümde kutu "1.500" + aria-invalid + düğme PASİF + kilit metni görünür. K4 (sayfa boyu) bilgi amaçlı.
Görüntüler Read ile GÖRÜLDÜ: takas BASİT 1440 açık (kırmızı "1.500", alan altı uyarı, Kaydet pasif, altında alan adıyla cümle), takas PRO 1440 koyu ("Takas Kaydet" pasif + cümle), karma BASİT 1280 açık (Kart satırı "1.500" kırmızı; **Fark "—"** kırmızı + cümle; kutular hizalı), karma BASİT 375 koyu, kapora PRO 375 koyu. Kopyalar `ham/TUVAL-KARSILASTIRMA/SATIS-3-senaryo-t17-*.png` (6 dosya); tümü `/private/tmp/kuyumcu-serit/SATIS-3/t17/foto/`.

### 30.5 Testler · mutasyon · derleme · denetim
- `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (son mutasyondan sonra, kaynaklar yeniden derlenerek). Not: ara yapılarda görülen tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 — ORTAK-1'in dosyası.
- **⚠ Yöntem düzeltmesi (dürüst not):** mutasyon betiği yedeği `shutil.copy2` ile geri koyuyordu → dosyanın ESKİ zaman damgası geri geliyor, MSBuild artımlı derlemede kaynağı "değişmemiş" sayıp **son mutasyonun ikili dosyasını** bırakıyordu. İlk canlı koşumda Kaydet sebebinin görünmemesi buradan çıktı (yayın M-S3-T17-13'lü idi); ilk tam süit (7148 · 5 kırmızı) de o ikiliyle koştu. Düzeltme: geri kopyadan sonra `os.utime` (dosya "dokunuldu") + kaynakları `touch` edip temiz yeniden derleme; canlı ve tam süit SONRA yeniden koşuldu. Önceki koşumların (t16 ve öncesi) mutasyon betikleri de `cp -p`/`copy2` kullanıyorsa aynı risk vardır — orkestratöre not.
- Yeni test **18 durum / 13 yöntem**: `SATIS3EkranTestleri` T17_Takas_YENI_fiyat ×2, T17_Takas_ESKI_gram ×2, T17_Hurda_gram ×2, T17_Satis_giris_satiri ×2 (adet/fiyat), T17_Karma_odeme_tutari…, T17_Karma_…FARK_tamam_DEMEZ ×2, T17_Para_ustu_alinan, T17_Iade_adedi, T17_Kapisiz_formda…, T17_css_… ; `S26DigerIslemlerTestleri` T17_Kapora_tutari, T17_Siparis_kapora, T17_Degisim_yeni_fiyat. Doğrulayıcının `T16D_takas_YENI_fiyat…` ve `T16D_takas_ESKI_gram_tus_tus…` **yeşil**.
- İlgili küme (SATIS3|SATIS2|S22|TS1|TS2|S24|S21|Takas|Iade|Masak|Islem|Fis|CumaS|OlcekTek|P78|Hurda|Bicim|Form|S26|EMANET|Emanet|L1|BasitMod|Altin): **2178/2178** (ara); son ağaçta T17|T16|SATIS3|S26|P78|EMANET **593/593**, T17_ 18/18 + S26 T17 3/3.
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t17/<kod>.orj` → tek eşleşme → derleyerek filtreli test → geri kopya → `cmp`), betik `t17/mut.py`, günlükler `t17/M-S3-T17-*.log`:

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T17-1 | SayisalAlan `NullBildir` → yalnız bayrak (kapı null bildirmez) | T16D ×2 + T17 9 yöntem (Takas_YENI, Takas_ESKI, Hurda, Satis, Karma_odeme, Iade, Kapora, Siparis, Degisim) | BİREBİR |
| M-S3-T17-2 | `KapiyaYaz` hep "geçerli" yazar | T17 10 yöntem (14 durum) | BİREBİR |
| M-S3-T17-3 | geçersizken `OnParametersSet` koruması silinir | T17_Iade, T17_Karma_odeme, T17_Satis_giris_satiri | BİREBİR |
| M-S3-T17-4 | `Dispose` kaydı silmez | T17_Karma_odeme_tutari (satır silinince kilit kalkar) | BİREBİR |
| M-S3-T17-5 | `Gonderilebilir`'den `!gecersizAlanlar.Var` düşer | T17_Karma_odeme, T17_Para_ustu, T17_Satis_giris_satiri | BİREBİR |
| M-S3-T17-7 | kapıda ayrıştırma hatası öne alınmaz | T17_Satis_giris_satiri | BİREBİR |
| M-S3-T17-8 | FisDigerEylem `Kilitli` kapıyı okumaz | T17_Siparis_kapora | BİREBİR |
| M-S3-T17-9 | FisDigerEylem tık koruması kalkar | T17_Siparis_kapora | BİREBİR |
| M-S3-T17-10 | `GecersizAlanSebebi` hep null | T17 7 yöntem | BİREBİR |
| M-S3-T17-11 | kapı değişince yeniden çizim yok | T17_Para_ustu_alinan | BİREBİR |
| M-S3-T17-12 | kapısız formda da null bildir (geriye uyum kırılır) | P78 `B2_B6_kullanici_1_500…`, `Gram_kutusu_uc_haneli…`, T17_Kapisiz | BİREBİR |
| M-S3-T17-13 | `KaydetKilitSebebi`'nden kapı dalı düşer | T17 7 yöntem | BİREBİR |
| M-S3-T17-14 | boş odak kutusu 0 ile ezilir ("01,5") | T17_Karma_odeme_tutari | BİREBİR |
| M-S3-T17-15 | css: uyarılı satır `align-items: start` silinir | T17_css_… | BİREBİR |
| M-S3-T17-16 | css: BASİT sil düğmesi etiket payı silinir | T17_css_… | BİREBİR |
| M-S3-T17-17 | FisVerdimAldim `OdemeBelirsiz` → false | T17_Karma_…FARK_tamam_DEMEZ(BASIT) | BİREBİR |
| M-S3-T17-18 | FisOdemeBacaklari `OdemeBelirsiz` → false | T17_Karma_…FARK_tamam_DEMEZ(PRO) | BİREBİR |
**17/17 kırmızı** (M-S3-T17-1/2/3/5 kapının ilk sürümünde koşuldu; hedef satırları sonradan DEĞİŞMEDİ. M-S3-T17-6 — `KalemEkle` ek koruması — kırmızı üretmedi: değer null/0'a düştüğü için mevcut "sıfırdan büyük" kuralı kalemi zaten eklemiyor; koruma GEREKSİZ bulundu ve **kaldırıldı**, satır özgün hâline döndü). Son temiz derleme 0/0.
- **Tam süit (son ağaç, `/private/tmp/kuyumcu-serit/trx-pSATIS-3-t17/t17d.trx`, t2 DB): 7170 · 7167 geçti · 3 kırmızı** — `OrtakGorunumSaflik.O8_…` (**ORTAK-1**, t16'da da vardı), `ZOISLEMAHurdaTahsilTestleri.ZO29y_alici_kabul_7_hane…` (**ZO-ISLEM-A** — tek başına 3/3 yeşil: yük altında kararsız; sayfası Hurda, kapı yok), `ZOISLEMAHurdaTahsilTestleri.ZZDEBUG` (**ZO-ISLEM-A**'nın geçici hata ayıklama testi; dosya 13:30'da değişti, test artık dosyada yok). Önceki ara koşumlarda görülen `PosBarkodTestleri` ×2 (tek başına 7/7 yeşil) ve `ZOBELGESinirTestleriTur4` ×4 (**ZO-BELGE**, Evrak portföy metni) son koşumda yeşil. **Bu paketten yeni kırmızı: 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 30.6 Temizlik
API 5162, vekil 5163, Chrome 9911/9912/9913 durduruldu; `lsof` beşi de **boş**; 5198/5199/5200'e dokunulmadı. Kiracı 01M3P8KE6YS50T895GCB7VRQBZ `kiraci-temizle.sh --uygula` (rc 0) + kendi `kiraci_davetleri` satırı (1) silindi; `tenant_id` kolonlu **70 tabloda 0 satır**; `PAKETSATIS-3%` = 0; `tenants` başlangıç **15** → son **15**. Anahtar/fikstür/indirilen wasm dosyaları `printf ''` ile boşaltıldı. Chrome profilleri `/private/tmp/kuyumcu-serit/SATIS-3/t17/profil-9911|9912|9913` (~342 MB) — hook `rm`'i engellediği için silme **kurucu/orkestratör**. Proje ağacında `.orj/.bak/.tmp/.mutorj` artığı **0**.

### 30.7 Değişen dosyalar (t17) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Pages/{Islemler.razor (1399 satır; satır 32/222 kapı sarmalayıcısı, KaydetKilitSebebi ve Gonderilebilir mevcut satırlarda), Islemler.Gecerlilik.cs (YENİ)}` · `Kuyumcu.Web/Bilesenler/Fis/{FisDigerEylem.razor, FisVerdimAldim.razor, FisOdemeBacaklari.razor}` · `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona 2 kural bloğu; sabit renk yok) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (`AlanDuzeltin` TR+EN) · `Kuyumcu.Tests/{SATIS3EkranTestleri.cs, S26DigerIslemlerTestleri.cs}`.
**Alan dışı dokunuş (gerekçeli, orkestratörün açık talimatı "SayisalAlan ortak bileşendir — eklemeli ve geriye uyumlu"):** `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` (basamaklı `GecersizKapi` parametresi + `IDisposable`; kapı yoksa davranış birebir — tam süit ve P78/EMANET1 testleri yeşil) ve `Kuyumcu.Web/Bilesenler/Form/GecersizAlanKapisi.cs` (YENİ). Bu iki dosya hiçbir eşzamanlı şeridin listesinde yok (OLCEK-1'in yalnız `Form/Secim.razor`'ı var). `AltinBacagiPaneli.razor` DEĞİŞMEDİ (kapıyı basamaklı parametreyle kendiliğinden alır).
Razor sayfası: yalnız `Pages/Islemler.razor` (+ partial) — § 12 ile aynı. Yeni varlık/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı. Her düğme: Kaydet ve Diğer birincil düğmeleri geçersiz kutuda `disabled` + sebep (UX-S30). Mevzuat: yeni hüküm yok.

### 30.8 Açık kalanlar (sahibiyle)
- `OrtakGorunumSaflik.O8_…` — **ORTAK-1**. `ZO29y…`/`ZZDEBUG` — **ZO-ISLEM-A** (kararsız / geçici test). `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı — **ORTAK-1**.
- Mutasyon yöntemi: yedeği zaman damgasıyla geri koyan betikler artımlı derlemede mutantı ikilide bırakır (§ 30.5) — **orkestratör** (ortak yöntem notu; bu koşumda düzeltildi).
- Kapısız formlar (Hurda*, Emanet*, Pos*, Gider …) aynı hata sınıfını taşıyabilir: kapı ortak bileşende hazır; o sayfalar `<CascadingValue Value="…GecersizAlanKapisi…">` ile sarılıp Kaydet kapısına bağlanmalı — **ilgili şeritler** (ZO-ISLEM-A, EMANET-1, ORTAK-1).
- Chrome profilleri `t17/profil-99xx` (~342 MB) ve önceki koşumların profilleri — silme **kurucu/orkestratör** (hook).
- Önceki açıklar değişmedi: görünen h1 "Satış" (KATMAN) · arama satırı stok (Api/Urun) · K9 (KATMAN) · gün içi kümülatif MASAK ön bilgisi (sunucu/istemci sözleşmesi).

### 30.9 Durum
Paket kapısı: (1) derleme 0 hata 0 uyarı ✓ · (2) T16D ×2 + yeni 18 test + ilgili küme 593/593 yeşil ✓ · (3) denetim TEMİZ ✓ · (4) 17 mutasyon kırmızı + cmp BİREBİR ✓ · (5) canlı: 6 kutu × BASİT/PRO gerçek tuşlarla "1.500" → POST 0 (yazınca / +6 sn / tık), "1,5" → gövde 1.5 + SQL (TAKAS·KART 86.156,45; AVANS_ALINDI 1,50); 25 ölçüm K1..K13 = 0, yatay 0 ✓ · (6) test kiracısı silindi, tenants 15 → 15 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 31. 29.09 t18 koşumu (14:24 – 15:20) — doğrulayıcının YÜKSEK bulgusu: PRO 1024/960 karma ödemede "Kaydet ve yazdır" ayrı satıra düşüyor

### 31.1 Bulgu → kök neden → yapılan → kanıt
| Önem | Bulgu | Kök neden (ölçüldü) | Yapılan | Kanıt |
|---|---|---|---|---|
| YÜKSEK | PRO 1024 karma: "+ Ayrıntı ekle · Tek ödemeye dön · Vazgeç" bir satırda, "Kaydet ve yazdır" altta tek başına, gerekçe daha aşağıda; "Tek ödemeye dön" mavi (birincil gibi) | ÖNCE ölçümü (başsız Chrome, PRO 1024 açık, `t18/serit.jsonl` ET=once): şerit 708 px; karmada içi `[hidden]` olan özet sarmalayıcısı `.fis-pro-odeme-ozet-sar` `flex: 1 1 200px` ile **344 px** boş yer kaplıyordu → Kaydet y1279 (Vazgeç y1207), kilit y1355 (Kaydet altından 24 px). "Tek ödemeye dön" rengi `rgb(36,87,214)` = `--vurgu` | `css/tuval-satis.css` sonuna (yalnız PRO, belirteçle, sabit renk yok): (a) içi boş özet sarmalayıcısı `display:none` (`:not(:has(> :not([hidden])))`); (b) şerit `justify-content:flex-start`, "+ Ayrıntı ekle" `margin-right:4px` → ikinciller solda; (c) `#islem-kaydet { margin-left:auto }` → birincil HER satırda en sağda, sığmazsa ikinciller solda kalır birincil sağa sarar; (d) `#islem-pro-tek` nötr (`--metin`, `--kenarlik`, `--yuzey`; Vazgeç ile aynı dil); (e) kilit gerekçesi `margin-top:-18px` → Kaydet'in hemen altında (24 → 10 px); (f) tek ödemede özet düğmesi `flex:1 1 0; min-width:7.5rem` (flex-basis 200 px tek ödemede de Kaydet'i 1024/768'de alta itiyordu — ölçümde bulundu), özet metni sözcük sınırından sarar, kesilmez | SONRA: PRO 1024 açık Kaydet y1207 = Vazgeç satırı, x823–1000 (şerit sağı), kilit Kaydet altından 10 px; görüntü `t18/foto/T18-sonra-karma-PRO-acik-1024.png` Read ile GÖRÜLDÜ |

Not: İlk sürümde `#islem-pro-tek`'e `font-weight:400` yazılmıştı → denetim `OLCEK-1: YUZ_OLCU_ENVANTER_DISI = 1` ve tam süitte `OlcekTekTestleri.OL26_…` kırmızı verdi (bu paketin ürünü); yazı ağırlığı kaldırıldı (OLCEK-1 envanteri korunur), OL26 yeniden koşumda YEŞİL, denetim TEMİZ.

### 31.2 Gerçek tarayıcı (başsız Chrome CDP 9915, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETSATIS-3-TEST Kuyumculuk` (01M3PEMWG2HC441D545SCTN81P) — `demo-tohumla.js --prova-kiraci` (15 sn; 6 cari, 12 ürün, 9 satış) + `t18/ek-tohum.py` (Anadolu Holding A.Ş., 2. kasa, 1 POS). Tek kullanımlık rastgele `Sistem__Anahtari`, değeri hiçbir çıktıya yazılmadı. Betik `/private/tmp/kuyumcu-serit/SATIS-3/t18/serit.mjs` (burma → ok+Enter ile kalem; karmada "+ Başka ödeme ekle"; sikisma.js K1..K11 + ekstra.js K12/K13 + şerit geometrisi), kayıt `t18/serit.jsonl`, özet `t18/ozet.py`.

Son CSS ile (ET=son, 18 ölçüm) + BASİT gerileme denetimi (ET=mat, 18 ölçüm; BASİT kuralları değişmedi):

| Durum | Mod | Tema | 1440 | 1280 | 1024 | 960 | 768 | 375 |
|---|---|---|---|---|---|---|---|---|
| karma | PRO | açık | tek satır ✓ | tek satır ✓ | tek satır ✓ | tek satır ✓ | tek satır ✓ | sarar, Kaydet sağda ✓ |
| karma | PRO | koyu | tek satır ✓ | — | tek satır ✓ | — | — | sarar, Kaydet sağda ✓ |
| tek ödeme | PRO | açık | tek satır ✓ | tek satır ✓ | tek satır ✓ | sarar, Kaydet sağda ✓ | tek satır ✓ | sarar, Kaydet sağda ✓ |
| tek ödeme | PRO | koyu | tek satır ✓ | — | tek satır ✓ | — | — | sarar, Kaydet sağda ✓ |
| karma / tek | BASİT | açık+koyu | tek satır ✓ | ✓ | ✓ | ✓ | ✓ | sarar, Kaydet sağda ✓ |

36/36 ölçümde K1, K3, K5, K7, K8, K9, K9b, K10, K10b, K11, K12, K13 = **0**; yatay kaydırma **0**; Kaydet sağ kenarı = şerit sağ kenarı (±1 px) her ölçümde; kilit gerekçesi Kaydet'in altından PRO 10 px / BASİT 8 px. (K2 sütun oranı, K4 sayfa boyu bilgi amaçlı.) PRO tek ödeme 960'ta şerit içeriği sığmıyor (özet + "+ Başka ödeme ekle" + Vazgeç + Kaydet) → kural gereği birincil yeni satırda sağda, gerekçe hemen altında. Görüntüler Read ile GÖRÜLDÜ: `T18-once-karma-PRO-acik-1024.png` (ÖNCE), `T18-sonra-karma-PRO-acik-1024.png`, `T18-son-karma-PRO-koyu-1024.png`, `T18-son-karma-PRO-acik-375.png`, `T18-son-tek-PRO-acik-960.png` (hepsi `/private/tmp/kuyumcu-serit/SATIS-3/t18/foto/`).

### 31.3 Testler · mutasyon · derleme · denetim
- Yeni test: `SATIS3EkranTestleri.T18_css_PRO_alt_eylem_seridi_birincil_sagda_tek_satir` (CSS sözleşmesi; yerleşim gerçek tarayıcıda ölçüldü).
- `SATIS3` süzgeci: **372/372** (test eklenmeden önce, kod sonrası) → ilgili küme `SATIS3|TS1Tuval|TS2Tuval|OlcekTek`: **483/483** yeşil.
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t18/<kod>.orj` → perl tek eşleşme → `SATIS3EkranTestleri.T18` → geri kopya → `cmp` BİREBİR; betik `t18/mut.zsh`):

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-S3-T18-1 | Kaydet `margin-left:auto` → `0` | T18_css_PRO_alt_eylem_seridi_birincil_sagda_tek_satir | BİREBİR |
| M-S3-T18-2 | boş özet sarmalayıcısı `display:none` → `flex` | aynı | BİREBİR |
| M-S3-T18-3 | şerit `flex-start` → `flex-end` | aynı | BİREBİR |
| M-S3-T18-4 | "Tek ödemeye dön" `--metin` → `--vurgu` | aynı | BİREBİR |
| M-S3-T18-5 | özet `flex:1 1 0; min-width:7.5rem` → `1 1 200px; 0` | aynı | BİREBİR |
(M-S3-T18-2'nin ilk denemesi düzenli ifadede fazla parantez yüzünden "eşleşme 0" ile hiçbir şey değiştirmeden durdu, düzeltilip yeniden koşuldu.) 5/5 kırmızı.
- `dotnet build Kuyumcu.sln` → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — **ORTAK-1**, önceki koşumlarda da vardı).
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pSATIS-3-t18/t18son.trx`, t2 DB): **7198 · 7196 geçti · 2 kırmızı** — `OrtakGorunumSaflik.O8_…` (**ORTAK-1**, t16/t17'de de vardı), `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (tek başına yeniden koşumda **YEŞİL** — yük altında kararsız, t8'de de görüldü). **Bu paketten yeni kırmızı: 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 31.4 Temizlik
API 5162 (PID 40318), vekil 5163, Chrome 9915 durduruldu; `lsof` üçü de boş; 5198/5199/5200'e dokunulmadı. Kiracı 01M3PEMWG2HC441D545SCTN81P `kiraci-temizle.sh --uygula` (rc 0; 70 tabloda 713 satır + tenants 1) + kendi `kiraci_davetleri` satırı (1) silindi; `PAKETSATIS-3%` = 0. `tenants` başlangıç **16** → son **15**: silme betiği yalnız bu paketin kiracısını sildi (günlükte tek "SİLİNDİ" satırı); aradaki 1 fark koşum sırasında başka bir şeridin kendi kiracısını silmesinden (paralel şeritler) — bu koşum başka kiracıya dokunmadı. Anahtar/fikstür dosyaları `printf ''` ile boşaltıldı. Chrome profili `/private/tmp/kuyumcu-serit/SATIS-3/t18/profil-9915` (~119 MB) — hook `rm`'i engellediği için silme **kurucu/orkestratör**. Proje ağacında `.mutorj/.bak/.tmp/.orj` artığı 0.

### 31.5 Değişen dosyalar (t18) · dokunulan sayfalar · DoD
`Kuyumcu.Web/wwwroot/css/tuval-satis.css` (sona iki kural bloğu, yalnız `.islem-sayfa.tuval-pro`) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs` (1 test). Razor sayfası açılmadı. Yeni varlık/uç yok → PATCH/DELETE/denetim yükümlülüğü doğmadı. Her düğme davranışı değişmedi (yalnız yerleşim/renk). Mevzuat: yeni hüküm yok.

### 31.6 Açık kalanlar (sahibiyle)
- `OrtakGorunumSaflik.O8_…`, `EtiketEkranTestleri.cs(145)` xUnit1031 — **ORTAK-1**. `PosBarkodTestleri.Fiyat_SUNUCU…` yük altında kararsız — **ZO-ISLEM-A / POS sahibi**.
- Chrome profilleri `t17/profil-99xx`, `t18/profil-9915` — silme **kurucu/orkestratör** (hook).
- Önceki açıklar değişmedi: görünen h1 "Satış" (KATMAN) · arama satırı stok (Api/Urun) · K9 (KATMAN) · kapısız formlar (ilgili şeritler).

### 31.7 Durum
Paket kapısı: (1) derleme 0 hata ✓ · (2) T18 + SATIS3/TS1/TS2/OlcekTek 483/483 ✓ · (3) denetim TEMİZ ✓ · (4) 5/5 mutasyon kırmızı + cmp BİREBİR ✓ · (5) canlı gerçek tarayıcı 36 ölçüm K1..K13 = 0, yatay 0, birincil her genişlikte sağda ✓ · (6) test kiracısı silindi ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**


## 32. 29.09 t19 koşumu (15:45 – 16:50) — orkestratör 15:40 (MASAK tek kaynak) + tasarım hakeminin 2 KRİTİK bulgusu

> DEVİR NOTU 4 ("kod yazma") bu koşumdan ÖNCE yazılmıştı; istemdeki "önceki doğrulamada kalan bulgular" (orkestratör 15:40 + 2 KRİTİK) ağaçta AÇIKTI (kod okundu: `Islemler.razor:775` SATIS için `kalemler.Max(...)`, `Islemler.Odeme.cs:130` `Max(MasakTutar, KimlikEsigi)`), bu yüzden kapatıldı.

### 32.1 Bulgu → kök neden → yapılan → kanıt
| Önem | Bulgu | Kök neden (koddan / ölçümden) | Yapılan | Kanıt |
|---|---|---|---|---|
| KRİTİK | Çok kalemli satışta MASAK kapısı "Altında", Kaydet açık, sunucu 400, amaç/açıklama alanları hiç açılmıyor | SATIS için `MasakTutar = en büyük kalem`; sunucu `SatisFisi`/`/islemler/satis` fiş TOPLAMINI (`fisToplam`) `DogrulaKumulatif`'e veriyor (SatisModulu.cs ~1629, 756). Red sonrası gereksinim `Max(tutar, kimlikEşiği)` ile soruluyordu → AMAÇ kademesi hiç gelmiyordu | (1) `MasakTutar` SATIS = `ToplamTl` (fiş toplamı, sunucunun tutarı). (2) Kademe TEK KAYNAKTAN: `/masak/gereksinim?tutar=<fiş toplamı>&cariId=<gerçek müşteri>` (ZO-ISLEM-A'nın 29.09'da açtığı parametre — sunucu aynı gün aynı müşterinin işlemlerini ekler; Peşin Müşteri gönderilmez, sunucu onu kümülatife katmaz). Gerçek müşteride eşik ALTINDA da sorulur; cevap kimlik isterse alanlar kayıttan ÖNCE açılır, Kaydet sebebiyle kilitlenir. (3) Sunucu reddi sonrası cevap kimlik istemiyorsa güvenli yön: en az kimlik eşiğiyle yeniden sorulur (çıkmaz yok). (4) `MasakGerekli` = eşik ∨ red ∨ **sunucunun `kimlikGerekli` cevabı**. İstemci eşik sayısı UYDURMAZ (eşik yine `/masak/esikler`'den). | bUnit `T19_iki_kalem_her_biri_esik_ALTI_toplam_USTU_MASAK_amac_ACILIR_Kaydet_KILITLI` (2×150.000, eşik 200.000 → `masak-amac` görünür, Kaydet kilitli, sebep `MasakKimlikEksik`); `T19_tek_kalem_esik_ALTI_ama_musterinin_gunluk_KUMULATIFI_ustu_alanlar_ONCEDEN_acilir`; `T19_PESIN_musteride_gereksinim_MUSTERISIZ_sorulur_esik_altinda_hic_sorulmaz`. Canlı: § 32.3 |
| (canlıda bulundu) | Kayıt başarılı olduktan sonra boş fişte MASAK kartı açık, Kaydet "Kimlik tespiti eksik" diye kilitli | `OnAfterRenderAsync` soru kalkınca (tutar 0) eski `kimlikGerekli=true` cevabını temizlemiyordu | Soru yoksa eski cevap düşer (`MasakSorulur ? anahtar eşit : masakGereksinim is null`) | 1. canlı deneme `sonra: masakKart true, kaydet PASIF` → düzeltme sonrası `masakKart false, sebep "Önce ürün seçin"`; bUnit `T19_kumulatif_formu_doldurulup_KAYDEDILINCE_bos_fiste_MASAK_karti_KALKAR` |
| (bUnit'te bulundu) | MASAK formu doldurulunca Kaydet kilidi yalnız klavye olayı sayfaya kabarcıklanınca açılıyordu (fareyle yapıştırma/otomatik doldurmada kilitli kalır) | `<MasakFormu>`'ya `Degisti` bağlanmamıştı; sayfa yeniden çizilmiyordu | `Degisti="MasakDegisti"` (boş sayfa yöntemi → EventCallback sayfayı çizer) | aynı test (`Input` olayı yalnız bileşene gider) |
| KRİTİK | PRO 960 ve 375'te kalem satırının TUTAR'ı okunmuyor (tablo 700/860 px, kart 642 px, kaydırma) | `min-width: 700px` (+ ≤900 px'te `width: 860px`) — kap sorgusu yalnız Milyem'i gizliyordu | `css/tuval-satis.css` sonu: kap ≤ 760 px → tablo `min-width:0; width:100%`, Milyem·Has·İşçilik gizli (Ayar·Gram·Adet·Tutar·Düzenle kalır; 768'de kart 742 px ölçüldü, 720 eşiği yetmiyordu); kap ≤ 480 px → satır iki katlı (üstte ad tam genişlik + "22 · 15,20 g" satırı — BASİT'in `data-ayrinti` biçimiyle aynı; altta adet · tutar · düzenle), başlık satırı bu dizilişte gösterilmez. `FisKalemTablosu.razor`: ad hücresine `span.fis-pro-ad-ozet` (geniş kartta gizli). Tutar sütunu HİÇBİR kuralda gizlenmez (test sözleşmesi) | § 32.2 tablo; `T19_css_PRO_dar_kartta_tablo_SIGAR_ikincil_sutunlar_duser_tutar_kalir`, `T19_PRO_ad_hucresinde_ayar_gram_OZETI_var_dar_kartta_gosterilir` |
| (canlıda bulundu, bu paketin kusuru) | PRO'da "Düzenle" tıklanınca hiçbir şey görünmüyordu (aria-expanded=true, satır yüksekliği 0) — birim fiyat / satır iskontosu / hediye / Sil erişilemez | SATIS-3 KAYNAK 5'in barkod sütununu gizleyen `:is(th,td):nth-child(1){display:none}` kuralı ayrıntı satırının TEK hücresini de gizliyordu; hücre görünür yapılınca `colspan=10` hayalet sütun açıp adı daraltıyordu (K10b) | Ayrıntı hücresi `display: table-cell`; `colspan` 6 (içerik) + 3 (`td.fis-pro-ayrinti-dolgu`, dar kartta gizli) = geniş kartta 9 / dar kartta 6 görünür sütunla birebir; 375'te blok | CDP ölçümü önce `{"trDisp":"table-row","tdDisp":"none","h":0}` → sonra `tdDisp table-cell, h 69`; § 32.2 "Düzenle açık" satırları |

### 32.2 Gerçek tarayıcı — PRO kalem tablosu (başsız Chrome CDP 9917, tarayıcı bölmesi KULLANILMADI; API 5162 `--no-build`, vekil 5163; betik `/private/tmp/kuyumcu-serit/SATIS-3/t19/tablo.mjs`, kayıt `tablo.jsonl`)
Her ölçümde 2 kalem ("22 Ayar Burma Bilezik" + "925 Gümüş Kolye", arama kutusuna harf harf + ↓ + Enter). K = sikisma K1..K11 + ekstra K12/K13 (K2/K4 bilgi amaçlı hariç). "Tutar kapta" = her satırın tutar hücresi tablo kabının sınırları içinde; "kaydırma" = kabın `scrollWidth − clientWidth`.

**ÖNCE** (hakem): PRO 960 kap 642 / tablo 700, kaydırma 58, tutar "128.30…" · 375 yalnız Ürün sütunu. İlk düzeltmede ölçülen ara durum: 768'de kap 742 → kaydırma **118**, tutar kap DIŞINDA (720 eşiği yetmedi → 760); 375'te dört sütunda ad üç satır (K3 2 / K10b 2) → iki katlı satır.

| Durum (SON) | Mod | Tema | 1440 | 1280 | 1024 | 960 | 768 | 375 |
|---|---|---|---|---|---|---|---|---|
| 2 kalem | PRO | açık | kap 784 · 9 sütun · kaydırma 0 · tutar kapta · K 0 | 962 · 9 · 0 · ✓ · 0 | 706 · 6 · 0 · ✓ · 0 | 642 · 6 · 0 · ✓ · 0 | 742 · 6 · 0 · ✓ · 0 | 349 · iki kat · 0 · ✓ · 0 |
| 2 kalem | PRO | koyu | 784 · 9 · 0 · ✓ · 0 | — | — | 642 · 6 · 0 · ✓ · 0 | — | 349 · iki kat · 0 · ✓ · 0 |
| 2 kalem + Düzenle açık | PRO | açık | 784 · 0 · ✓ · 0 | 962 · 0 · ✓ · 0 | 706 · 0 · ✓ · 0 | 642 · 0 · ✓ · 0 | 742 · 0 · ✓ · 0 | 349 · 0 · ✓ · 0 |
| 2 kalem + Düzenle açık | PRO | koyu | 784 · 0 · ✓ · 0 | — | — | — | — | 349 · 0 · ✓ · 0 |
| 2 kalem (gerileme) | BASİT | açık / koyu | — | açık: 3 sütun · 0 · ✓ · 0 | — | açık 0 · ✓ · 0 | — | açık+koyu 0 · ✓ · 0 |

Toplam son ölçüm: 29 (son7 15 + son9 14); hepsinde yatay kaydırma 0, kap kaydırması 0, tutar taşması 0, K1..K13 = 0. Görüntüler Read ile GÖRÜLDÜ ve `ham/TUVAL-KARSILASTIRMA/SATIS-3-T19-*.png`'ye kopyalandı: `son-tablo-PRO-acik-960` (tutar tam, 6 sütun), `son7-tablo-PRO-koyu-375` (iki kat), `son9duz-tablo-PRO-acik-1440` (Düzenle açık, ad tek satır), `son9duz-tablo-PRO-koyu-375`, `son9-tablo-PRO-acik-960/375`, `masak-cok-PRO-alanlar`, `masak-kum-PRO-alanlar`.

### 32.3 Canlı MASAK doğrulaması (kiracı `PAKETSATIS-3-TEST Kuyumculuk` 01M3PKM2PDS96WSW544164DKM9; `demo-tohumla.js --prova-kiraci` + `ek-tohum.py`; eşikler sunucudan: kimlik 185.000, amaç 200.000)
Ham API (`canli-gereksinim.txt`, yanıtlar okundu): `tutar=173969.38` müşterisiz → `YOK`; aynı tutar `&cariId=<Anadolu Holding>` → `YALNIZ_KIMLIK` (müşterinin günlük işlemi eklendi); `301852.57&cariId` → `AMAC_BEYANI, amacGerekli true, asgari 20`.

| Senaryo (PRO 1440, ödeme Kart, betik `masak.mjs`) | İstemcinin sorduğu (ağ kaydı) | Ekran önce | Doldurunca | POST | SQL |
|---|---|---|---|---|---|
| Çok kalem: Anadolu Holding, 95.151,81 + 110.000 (her biri 185 bin ALTI, toplam 205.151,81) | `gereksinim?tutar=95151.81&cariId=…` → `205151.81&cariId=…` | MASAK kartı + `masak-amac` + `masak-aciklama` AÇIK, Kaydet PASİF "Kimlik tespiti eksik…" | ETKİN | `/islemler/satis-fisi` **200** | `masak_kimlik_tespitleri` islem_tutari_tl **205151.81** (fiş TOPLAMI), hareket SATIS 95151.81 (fişin ilk hareketi) |
| Tek kalem eşik ALTI ama müşterinin günlük kümülatifi üstü: Anadolu Holding, 95.159,92 | `tutar=95159.92&cariId=…` | alanlar kayıttan ÖNCE açık (red beklenmedi), Kaydet PASİF | ETKİN | `/islemler/satis` **200** | tespit 95159.92 |
| Tek kalem eşik üstü: Kemal Ardıç, 190.000 | `tutar=190000&cariId=…` | alanlar açık, Kaydet PASİF | ETKİN | `/islemler/satis` **200** | tespit 190000.00 |
| Tek kalem eşik altı, temiz müşteri: Derya Yıldızlı, 95.247,32 | `tutar=95247.32&cariId=…` → YOK | MASAK kartı YOK, Kaydet ETKİN | — | `/islemler/satis` **200** | satış 1 (95247.32), tespit **0** |
Kayıttan sonra her senaryoda MASAK kartı kapandı (`masakKart false`, sebep "Önce ürün seçin"). `masak_beyanlari` 3 satır (amaç kademesi). Kayıt `sql-masak.txt`.

### 32.4 Testler · mutasyon · derleme · denetim
- Yeni test 6 (`SATIS3EkranTestleri.T19_*`): iki kalem toplam, kümülatif önceden açılır, Peşin'de müşterisiz/eşik altı soru yok, kayıttan sonra kart kalkar, CSS sözleşmesi, ad hücresi özeti.
- Uyarlanan 2 test (gerekçeli, sahte API gerçekçileştirildi — davranış sözleşmesi korunur): `BasitModPosTestleri` sahtesi HER tutara `kimlikGerekli:true` dönüyordu; artık gerçek sunucu gibi eşik altına `YOK` (5 test bu yüzden kırmızıya dönmüştü: ekran artık gerçek müşteride eşik altında da soruyor). `L1bAlisFisiEkranTestleri.MASAK_formu_FIS_toplamiyla_acilir…`: "eşik altında hiç soru yok" yerine "form açılmaz" + soru fiş toplamıyla (`tutar=200000`, müşteri parametresiyle) — `EndsWith` → `Contains`.
- Filtreli: `SATIS3|TS1Tuval|TS2Tuval|OlcekTek|L1|BasitModPos|SATIS2|Pesin|S2` **916/916**; geniş Islemler kümesi (`Ekran|Tuval|L1|Pesin|BasitMod|CumaS|S22|S25|Masak`) **2294/2294**; T19 3 ardışık koşum 6/6.
- Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t19/<kod>.orj` → tek eşleşme → `SATIS3EkranTestleri.T19` → geri kopya → `cmp`; betik `t19/mut.py`):

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T19-1 | SATIS MASAK tutarı = en büyük kalem (eski) | T19_iki_kalem_her_biri_esik_ALTI_toplam_USTU_MASAK_amac_ACILIR_Kaydet_KILITLI | BİREBİR |
| M-S3-T19-2 | `MasakGerekli`'den sunucunun `kimlikGerekli` cevabı çıkarıldı | T19_tek_kalem_…KUMULATIFI…, T19_kumulatif_…KAYDEDILINCE… | BİREBİR |
| M-S3-T19-3 | gereksinim sorgusundan müşteri düşürüldü | T19_iki_kalem…, T19_tek_kalem…, T19_kumulatif… | BİREBİR |
| M-S3-T19-4 | soru kalkınca eski cevap temizlenmez | T19_kumulatif_formu_doldurulup_KAYDEDILINCE_bos_fiste_MASAK_karti_KALKAR | BİREBİR |
| M-S3-T19-5 | `MasakFormu Degisti` bağı kaldırıldı | T19_kumulatif_…KAYDEDILINCE… | BİREBİR |
| M-S3-T19-6 | dar kartta tablo `min-width: 700px` | T19_css_PRO_dar_kartta_tablo_SIGAR_ikincil_sutunlar_duser_tutar_kalir | BİREBİR |
| M-S3-T19-7 | ayrıntı hücresi `display: none` (eski kusur) | T19_css_PRO_dar_kartta… | BİREBİR |
| M-S3-T19-8 | ad hücresi özet sınıfı değişti | T19_PRO_ad_hucresinde_ayar_gram_OZETI_var_dar_kartta_gosterilir | BİREBİR |
8/8 kırmızı. ⚠ Yöntem notu (dürüst): ilk turda geri kopya `copy2` eski mtime'ı taşıdığı için M5'ten sonra Web derlemesi tetiklenmedi; M6/M7'de ek olarak `T19_kumulatif` kırmızısı çıktı (M5'in derlenmiş artığı) ve M8 sonrası `--no-build` koşumu M8'li derlemeyle kırmızı verdi. Kaynaklar `cmp` BİREBİR idi; dosyalar `touch` edilip temiz derlendi → T19 6/6 (3 koşum); M6/M7 düzeltilmiş betikle (geri kopyadan sonra `os.utime`) yeniden koşuldu → yalnız CSS testi kırmızı. Tablo düzeltilmiş sonuçtur.
- `dotnet build Kuyumcu.sln` → **0 Hata, 0 Uyarı** (son derleme `t19/b7.log`, mutasyon sonrası temiz).
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pSATIS-3-t19/t19.trx`, t2 DB): **7256 · 7255 geçti · 1 kırmızı** — `Dogrulayici.ZOBELGEAramaSinirTestleri.DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz` (API `/arama` alış fişi işareti; test dosyası `Dogrulayici/ZOBELGESinirTestleri.cs` 16:24'te yazıldı — **ZO-BELGE doğrulayıcısının** açık bulgusu; bu şerit `/arama`'ya dokunmadı). t18'deki `OrtakGorunumSaflik.O8` ve `PosBarkod` kararsızlığı bu koşumda yeşil. **Bu paketten yeni kırmızı: 0.**
- `bash denetim.sh` → **DENETİM TEMİZ** (rc 0). `Islemler.razor` 1399 satır (≤ 1.400, değişmedi).

### 32.5 Temizlik
API 5162, vekil 5163 durduruldu, CDP 9917 kapalı (`lsof` üçü boş); 5198/5199/5200'e dokunulmadı. Kiracı 01M3PKM2PDS96WSW544164DKM9 `kiraci-temizle.sh --uygula` (rc 0; 70 tablo) + kendi `kiraci_davetleri` satırı (1) silindi; `PAKETSATIS-3%` = 0; `tenants` başlangıç **15** → son **15**. Sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı (değer hiçbir çıktıya yazılmadı). Chrome profili `t19/profil-9917` (t19 dizini ~186 MB) — hook `rm`'i engellediği için silme **kurucu/orkestratör**. Proje ağacında bu şeridin `.mutorj/.bak/.tmp/.orj` artığı 0 (ağaçtaki `Kuyumcu.Tests/{AramaHttp,MadenDefteri,P21CiroRucu}Testleri.cs.bak` bu şeridin değil).

### 32.6 Değişen dosyalar (t19) · dokunulan sayfalar · DoD
`Pages/Islemler.razor` (satır sayısı sabit: MASAK tutarı/gerekli/tazele/anahtar, `Degisti`) · `Pages/Islemler.Odeme.cs` (MasakSorguCari · MasakSorulur · MasakSorguAnahtari · MasakGereksinimSor · MasakDegisti) · `Bilesenler/Fis/FisKalemTablosu.razor` (ad özeti span, ayrıntı satırı 6+3) · `wwwroot/css/tuval-satis.css` (sona t19 blokları, belirteçle, sabit renk yok) · testler `SATIS3EkranTestleri.cs` (+6), `BasitModPosTestleri.cs` (sahte gereksinim), `L1bAlisFisiEkranTestleri.cs` (1 test). Dokunulan sayfa: `/islemler` (aynı şerit, aynı paket — gerekçe: doğrulama bulguları). Yeni varlık/uç/migration YOK → PATCH/DELETE/denetim yükümlülüğü doğmadı. POST gövdeleri DEĞİŞMEDİ (masak bloğu yalnız `MasakGerekli` iken gider — önceki kural). Mevzuat: yeni hüküm yazılmadı; eşik ve kademe sunucudan (`/masak/esikler`, `/masak/gereksinim`), istemcide sayı yok.

### 32.7 Açık kalanlar (sahibiyle)
- `DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz` — **ZO-BELGE** (API arama).
- `MasakFormu` başlık metni "Bu tutar yasal eşiği aştığı için…" kümülatif durumda (tek işlem eşik altı) tam doğru değil — bileşen `Bilesenler/MasakFormu.razor` bu şeridin alanı dışında (**ZO-ISLEM-A**): sunucunun `kademe` gerekçesi (kümülatif) metne yansıtılmalı.
- Önceki açıklar değişmedi: görünen h1 "Satış" (**KATMAN**) · arama satırı stok (**Api/Urun**) · K9 (**KATMAN**) · Chrome profilleri silme (**kurucu/orkestratör**).

### 32.8 Durum
Paket kapısı: (1) derleme 0 hata 0 uyarı ✓ · (2) T19 6/6 + ilgili küme 916/916 + geniş küme 2294/2294 ✓ · (3) denetim TEMİZ ✓ · (4) 8/8 mutasyon kırmızı + cmp BİREBİR ✓ · (5) canlı: 4 MASAK senaryosu HTTP 200 + SQL, 29 tarayıcı ölçümü K1..K13 = 0, kaydırma 0, tutar her genişlikte kapta ✓ · (6) test kiracısı silindi, tenants 15 → 15 ✓ · (7) tam süitte bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**

## 33. 29.09 t20 koşumu — doğrulayıcının KRİTİK MASAK istek fırtınası bulgusu + DÜŞÜK bulgular

### 33.1 Bulgu → yapılan
| Bulgu | Durum | Yapılan / kanıt |
|---|---|---|
| **KRİTİK** — `/masak/gereksinim` hata verince bitmeyen istek + çizim döngüsü, hata ekranda yok | **KAPANDI** | `GereksinimTazele` (artık `Pages/Islemler.Odeme.cs`): sorgu anahtarı İSTEKTEN ÖNCE "soruldu" işaretlenir (`gereksinimAnahtari = MasakSorguAnahtari`), hata durumunda `masakHata = true`; `OnAfterRenderAsync` aynı anahtarı bir daha sormaz. Yeniden sorma yalnız anahtar değişince (tutar/müşteri/red) ya da **"Yeniden dene"** düğmesiyle (`#islem-masak-tekrar`, `MasakYenidenDene`: anahtarı sıfırlar, TEK istek). Eşik üstünde MASAK kartı AÇIK kalır (`#islem-masak-hata-kart`, `.kart--masak`) ve içinde Türkçe hata bandı (`#islem-masak-hata`, `HataBandi`); cevap gelmediği için `MasakTamam=false` → Kaydet KİLİTLİ (fail-closed). Eşik altında (yalnız kümülatif sorusu) hata sessiz kalmaz: kartın dışında aynı bant + düğme (`#islem-masak-hata-bilgi`); Kaydet açık, sunucu kayıtta son sözü söyler (kümülatif kapı sunucuda). `T19d_gereksinim_ucu_DUSERSE_istek_FIRTINASI_yok_Kaydet_KILITLI` YEŞİL. |
| DÜŞÜK — MASAK kartı metni kümülatif durumda yanlış yönlendiriyor | AÇIK (alan dışı) | `Bilesenler/MasakFormu.razor` + sunucunun kademe gerekçesi → **ZO-ISLEM-A**. Bu şerit bileşene dokunmadı. |
| DÜŞÜK — `/masak/gereksinim` `[HerkeseAcik]` + `cariId` ile günlük toplam kademesi sızabilir | AÇIK (alan dışı) | `Api/Modules/Masak/MasakModulu.cs:361-364` → **ZO-ISLEM-A** (gerekçe metni ya da `cariId`'li sorguyu yetkiye bağlama). Bu paket yalnız çağırır. |
| DÜŞÜK — tarayıcı ölçümü zorunlu araçla yapılmamış | **KAPANDI** | t20 ölçümleri `betikler/sikisma-olc.js` ile (kopyası `t20/sikisma-olc.js`, `cmp` → birebir aynı) + `betikler/tur/ekstra.js`. |
| DÜŞÜK — XML belge yorumu yanlış metotta | **KAPANDI** | `MasakGereksinimSor`'un `<summary>` bloğu kendi metodunun üstüne taşındı; `MasakDegisti` tek `<summary>` taşır. |

Metinler `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (TR+EN): `MasakGereksinimHata` ("Kimlik kuralı alınamadı — bağlantıyı kontrol edip yeniden deneyin."), `MasakTekrarDene` ("Yeniden dene"). CSS `wwwroot/css/tuval-satis.css` sonu (t20 bloğu; belirteç `--bosluk-2/3`, sabit renk yok, satır içi style yok). `Islemler.razor` 1399 satır (≤ 1.400 — ilk denemede 1425'e çıkıp `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` ve `S1i_06` kırmızı verdi; `GereksinimTazele`/`MasakYenidenDene`/`masakHata` `Islemler.Odeme.cs`'e taşındı → yeşil).

### 33.2 Testler
- Yeni: `Dogrulayici.SATIS3SinirTestleriT19.T20_gereksinim_ucu_DUSERSE_Turkce_HATA_BANDI_ve_Yeniden_dene_TEK_istek_atar` (bant metni sözlükten, Kaydet kilitli, düğme tam 1 istek, sonra döngü yok).
- Filtreli `SATIS3|Satis3|S21FisYapi|S1iGorselSigma2` → **421/421**; `T19d|T20_` → 10/10.
- Tam süit (t2 DB, `/private/tmp/kuyumcu-serit/trx-pSATIS-3-t20c`): **7281/7281 geçti, 0 kırmızı.** (Ara koşum `trx-pSATIS-3-t20b`'de `SATIS3EkranTestleri.T17_Takas_ESKI_gram_tus_tus…(PROFESYONEL)` başsız Chrome matrisiyle AYNI ANDA koşulurken WaitFor zaman aşımıyla 1 kez kırmızı verdi; tek başına 3/3 yeşil, yüksüz tam süitte yeşil → yük kaynaklı zamanlama, kod değişikliği yok.)
- `dotnet build Kuyumcu.sln` → 0 Hata 0 Uyarı · `denetim.sh` → **DENETİM TEMİZ** (rc 0).

### 33.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t20/Islemler.razor` → perl tek eşleşme → filtreli test → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T20-1 | anahtar yalnız başarıda yazılır (eski davranış) | `T19d_gereksinim_ucu_DUSERSE_istek_FIRTINASI_yok_Kaydet_KILITLI` ("31 istek art arda"), `T20_…Yeniden_dene_TEK_istek_atar` | BİREBİR |
| M-S3-T20-2 | "Yeniden dene" anahtarı sıfırlamaz | `T20_…Yeniden_dene_TEK_istek_atar` | BİREBİR |
Not (dürüst): önce denenen "düğme `GereksinimTazele`'yi çağırmaz" mutasyonu testi kırmadı — anahtar sıfırlanınca `OnAfterRenderAsync` zaten tek soru sorar; davranış bozulmadığı için eşdeğer mutasyon sayıldı ve anlamlı olan M-S3-T20-2 ile değiştirildi. Son mutasyondan sonra temiz derleme 0/0.

### 33.4 Gerçek tarayıcı (başsız Chrome + ham CDP, port 9905; tarayıcı bölmesi KULLANILMADI)
API 5162 (kendi, `Sistem__Anahtari` rastgele üretildi, değer yazılmadı) · vekil 5163 · kiracı `PAKETSATIS-3-TEST t20 Kuyumculuk` (01M3PSK272TTBXPZNY8WQAC4JD, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış). `/masak/gereksinim` CDP `Fetch.failRequest` ile düşürüldü (ağ hatası). Betik `t20/hata.mjs`, ham sonuç `t20/hata.jsonl`, görüntüler `t20/foto/T20-<durum>-<mod>-<tema>-<w>.png`.

| durum | mod | tema | w | görünen | gereksinim isteği ilk → 3 sn sonra | Yeniden dene | Kaydet | K1..K13 + yatay |
|---|---|---|---|---|---|---|---|---|
| eşik üstü 250.000 | BASİT | açık/koyu | 1280 · 375 | hata kartı (.kart--masak) | 1 → 1 | 1 → 2 | PASİF | 0 |
| eşik üstü 250.000 | PRO | açık/koyu | 1280 · 375 | hata kartı | 1 → 1 | 1 → 2 | PASİF | 0 |
| eşik altı 5.000 (gerçek müşteri) | BASİT | açık/koyu | 1280 · 375 | kart dışı bant + düğme | 1 → 1 | 1 → 2 | ETKİN | 0 |
| eşik altı 5.000 | PRO | açık/koyu | 1280 · 375 | kart dışı bant + düğme | 1 → 1 | 1 → 2 | ETKİN | 0 |
16/16 ölçüm: istek fırtınası yok, düğme tek istek, K1..K13 = 0, yatay kaydırma 0. Read ile GÖRÜLEN: `T20-ustu-BASIT-aydinlik-1280.png`, `T20-ustu-BASIT-karanlik-1280.png`, `T20-ustu-PRO-karanlik-375.png`, `T20-alti-PRO-karanlik-1280.png`, `T20-alti-BASIT-aydinlik-375.png`. İlk turda düğme kart kenarına yapışıyordu (`foto-ilk/`) → kart iç boşluğu + dikey düzen eklendi, matris yeniden koşuldu (tablo ikinci turdur).

### 33.5 Temizlik
API 5162, vekil 5163, CDP 9905 kapalı (`lsof` boş); 5198/5199/5200'e dokunulmadı. Kiracı 01M3PSK272TTBXPZNY8WQAC4JD `kiraci-temizle.sh --uygula` (rc 0, 70 tablo) + kendi `kiraci_davetleri` satırı (1) silindi; `tenants` başlangıç **14** → son **14**. Anahtar ve fikstür dosyaları `printf ''` ile boşaltıldı. Chrome profili `t20/profil-9905` (115 MB) — hook `rm`'i engellediği için silme **kurucu/orkestratör**. Proje ağacında bu koşumun `.mutorj/.bak/.tmp/.orj` artığı 0.

### 33.6 Değişen dosyalar (t20) · DoD
`Pages/Islemler.razor` (hata kartı + kart dışı bant; tazele kodu çıkarıldı) · `Pages/Islemler.Odeme.cs` (`masakHata`, `GereksinimTazele`, `MasakYenidenDene`, belge yorumu) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (2 anahtar TR+EN) · `wwwroot/css/tuval-satis.css` (t20 bloğu) · `Kuyumcu.Tests/Dogrulayici/SATIS3SinirTestleri.cs` (+1 test). Yeni varlık/uç/migration YOK. POST gövdeleri DEĞİŞMEDİ. Yeni düğme gerçek istek atar (test + canlı). Mevzuat hükmü yazılmadı.

### 33.7 Durum
Paket kapısı: (1) derleme 0/0 ✓ · (2) ilgili küme 421/421 ✓ · (3) denetim TEMİZ ✓ · (4) 2/2 mutasyon kırmızı + cmp ✓ · (5) canlı 16 ölçüm ✓ · (6) kiracı silindi, 14 → 14 ✓ · (7) tam süit 7281/7281, bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.** Açık (alan dışı): MasakFormu kümülatif metni + gereksinim ucu yetkisi (**ZO-ISLEM-A**), görünen h1 / K9 (**KATMAN**), arama satırı stok (**Api/Urun**), Chrome profilleri silme (**kurucu/orkestratör**).

## 34. 29.09 t21 koşumu (22:05 – 23:40) — hasım doğrulayıcının KRİTİK döviz bulgusu + ORTA MASAK + DÜŞÜK bulgular

Ortam: API **5162** (`--no-build`, Development; sistem anahtarı bu koşum için üretildi, dosyası sonra `printf ''` ile boşaltıldı), vekil **5163**, başsız Chrome CDP **9906–9911** (tarayıcı bölmesi KULLANILMADI, `Network.setCacheDisabled`). Kiracı `PAKETSATIS-3-TEST t21 Kuyumculuk` (01M3QAW76FY7AFG0THKN9RKAPZ) `demo-tohumla.js --prova-kiraci` ile (6 cari, 12 ürün, 9 satış) + API ile bir "Sterlin Kasa" (GBP). Test DB `kuyumcu_test_t2`. Geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t21/`. **COMMIT YOK.**

### 34.1 Bulgu → yapılan
| Bulgu | Durum | Yapılan / kanıt |
|---|---|---|
| **KRİTİK** — kur yokken DÖVİZ ödeme satırı gövdeden sessizce düşüyor (fiş tam nakit/kart kaydediliyordu) | **KAPANDI** | `FisFark.Gonderilecek`: DÖVİZ satırı **döviz miktarı > 0** iken TL'si 0 olsa da DÜŞMEZ (miktar 0 ise boş satırdır, düşer). Yeni `FisFark.DovizKuruEksik`. Satır gövdede kaldığı ve `gb.All(TutarTl > 0)` tutmadığı için Kaydet KİLİTLİ; sebep `KarmaKilitSebebi` başında **"Döviz kuru alınamadı — kur gelince kaydedebilirsiniz."** Döviz satırının altında aynı bant + **"Kuru yeniden al"** (`#islem-bacak-{i}-kur-tekrar` → `CanliKurServisi.TazeleAsync`, TEK `/kur/guncel` isteği). Kur sonradan gelince (düğme ya da push/yoklama) `FisOdemeBacaklari.OnParametersSetAsync` → `DovizleriYenile` TL'yi kur kaydından yeniden türetir ve otomatik kalan yeniden dengelenir. Fark çubukları (PRO `#islem-bacak-fark-metin`, BASİT `#islem-fark-tutar`/`#islem-fark-durum`) kur yokken **"0 ₺ ✓" demez** ("—" + sebep). İstemci kur UYDURMAZ. `FisOdemeBacaklari.DovizTl` yorumu gerçeğe göre düzeltildi. `K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ` **YEŞİL**. |
| (canlı ölçümde bulundu) kasası olmayan döviz satırında Kaydet "Ödemeyi tamamlayın" diye belirsiz kilitli | **KAPANDI** | `KasasizSatirSebebi`: gönderilecek satırın kasası boşsa — uyan kasa yoksa **"GBP kasası tanımlı değil — bu ödeme için önce Kasalar ekranından GBP kasası açın."**, varsa "Kasa seçin" (UX-S30). |
| (canlı/bUnit'te bulundu) döviz BİRİMİ değişince eski birimin kasası satırda kalıyordu (USD kasası EUR satırında) | **KAPANDI** | Birim seçicisi `BirimDegisti`: `FisHesap.BacakKasaSabitle` + TL yeniden türetilir (araç değişiminin K6 kuralının aynısı). |
| **ORTA** — MASAK gereksinim ucu düşünce ekran "gereken alanlar açıldı / kimlik bilgilerini girin" diyor ama alan yok | **KAPANDI** | `KaydetKilitSebebi`: `!MasakTamam` iken `masakHata` ise sebep **`MasakGereksinimHata`** ("Kimlik kuralı alınamadı — bağlantıyı kontrol edip yeniden deneyin."), değilse eski `MasakKimlikEksik`. `YasalBantAcik`: `masakHata && MasakGerekli` iken BASİT üst bandı (`#islem-yasal-kapi`, "gereken alanlar açıldı") ÇIKMAZ — gerçek engel hata kartında (Yeniden dene) ve Kaydet sebebinde. |
| DÜŞÜK — `KarmaGonderilebilir` VERESİYE koşulları gönderilmeyecek 0 satırı da sayıyor | **KAPANDI** | İki koşul da `gb` (gönderilecek satırlar) üzerinden: `gb.Count(VERESIYE) <= 1`, `!(gb.Any(VERESIYE) && cariId boş)`. |
| DÜŞÜK — kesilen önceki doğrulamanın kiracısı DB'de kalmıştı | Doğrulayıcı sildi; bu koşumun kiracısı silindi (§ 34.6) | Kesilen koşum sonrası kiracı süpürmesinin kapı adımı olması **orkestratöre** öneri. `kiraci_davetleri`'deki 4 eski `PAKETSATIS-3…` davet satırı (doğrulayıcı/hakem koşumlarının; kiracıları yok) başka ajanların — bu koşum yalnız KENDİ davetini (1) sildi. |
| DÜŞÜK — PRO 960 tek ödemede "Kaydet ve yazdır" ayrı satır + MasakFormu 960'ta yıldız alt satırda | AÇIK (alan dışı, raporda açıklanmış) | Alt satır sarması KATMAN ile, `MasakFormu.razor` **ZO-ISLEM-A** bileşeni. Bu koşumda dokunulmadı. |

### 34.2 Testler
- Yeni dosya `Kuyumcu.Tests/SATIS3DovizKurTestleri.cs` (7 metot / 19 koşum): `T21_Gonderilecek_kur_yokken_DOVIZ_satirini_DUSURMEZ_miktar_0_ise_dusurur`, `T21_odeme_turleri_tablosu_girilen_satir_SESSIZCE_DUSMEZ` (12 satırlık tablo: NAKIT/KART/HAVALE/VERESIYE/DOVIZ/ALTIN × dolu/boş), `T21_kur_yokken_doviz_girilince_Kaydet_PASIF_sebep_yazili_fark_TAMAM_demez_POST_yok`, `T21_Kuru_yeniden_al_TEK_istek_kur_gelince_doviz_GOVDEDE_ve_kaydedilir` (gövdede DOVIZ · USD · 250 · 10.000), `T21_bos_ikinci_VERESIYE_satiri_gonderilmez_Kaydet_KILITLEMEZ`, `T21_kasasi_olmayan_doviz_satiri_sebebi_EKSIK_KASAYI_soyler`, `T21_BASIT_fark_cubugu_kur_yokken_TAMAM_demez`.
- `Dogrulayici/SATIS3SinirTestleri.cs` · `SATIS3SinirTestleriT19.T21_gereksinim_ucu_DUSERSE_kilit_sebebi_KURAL_ALINAMADI_bant_ALANLAR_ACILDI_demez` (PRO + BASİT).
- Filtreli: `SATIS3DovizKurTestleri|T21_|DovizBacagi` → **25/25**; geniş küme `SATIS3|Satis3|S21Fis|Karma|Doviz|Islem|Fis|SATIS2|Masak` → **1465/1465** (kasa düzeltmesinden önce) · `SATIS3DovizKurTestleri|T21_|DovizBacagi|Karma` → **75/75** (sonra).
- Tam süit: § 34.5.

### 34.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t21/<kod>.orj` → `perl -0777` tek eşleşme → filtreli test → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T21-1 | `Gonderilecek`'ten döviz koşulu çıkarıldı (eski davranış) | `K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ`, `T21_Gonderilecek_…`, `T21_odeme_turleri_tablosu…(DOVIZ,0,100)`, `T21_kur_yokken_doviz_girilince_Kaydet_PASIF…` | BİREBİR |
| M-S3-T21-2 | kilit sebebinde döviz kuru satırı `null` | `T21_kur_yokken_doviz_girilince_Kaydet_PASIF…` | BİREBİR |
| M-S3-T21-3 | MASAK kilit sebebi yine `MasakKimlikEksik` | `T21_gereksinim_ucu_DUSERSE…` (PRO + BASİT) | BİREBİR (ilk denemede başka şeridin `ZOBELGESinirTestleri.cs` derleme hatası → yeniden koşuldu) |
| M-S3-T21-4 | `YasalBantAcik`'tan `!(masakHata && MasakGerekli)` çıkarıldı | `T21_gereksinim_ucu_DUSERSE…(BASIT)` | BİREBİR |
| M-S3-T21-5 | VERESİYE sayımı yine `bacaklar` | `T21_bos_ikinci_VERESIYE_satiri…` | BİREBİR |
| M-S3-T21-6 | `DovizleriYenile` hiçbir satırı güncellemez | `T21_Kuru_yeniden_al_TEK_istek…` | BİREBİR |
| M-S3-T21-7 | PRO fark metninden döviz satırı çıkarıldı | `T21_kur_yokken_…fark_TAMAM_demez…` | BİREBİR |
| M-S3-T21-8 | `KasasizSatirSebebi` çağrısı çıkarıldı | `T21_kasasi_olmayan_doviz_satiri…` | BİREBİR |
| M-S3-T21-9 | `BirimDegisti` kasayı sabitlemez | `T21_kasasi_olmayan_doviz_satiri…` | BİREBİR |
| M-S3-T21-10 | BASİT fark çubuğu `KurEksik`'i bilmez | `T21_BASIT_fark_cubugu_kur_yokken_TAMAM_demez` | BİREBİR |
**10/10 kırmızı**, hepsi `cmp` BİREBİR; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata 0 Uyarı**. Proje ağacında `.mutorj/.bak/.tmp/.orj` artığı 0.

### 34.4 Canlı doğrulama + gerçek tarayıcı (başsız Chrome + ham CDP)
**Kur yok benzetimi:** CDP `Fetch` ile `/kur/guncel` yanıtından `gbpTry` çıkarıldı (canlı kurda GBP var: 65,22), SignalR `/hubs/kur` düşürüldü (push GBP'yi geri getirmesin). Betik `t21/doviz.mjs`: müşteri seç → "bileklik" ara, ↓+Enter → "+ Başka ödeme ekle" → 2. satır Döviz · GBP · **gerçek tuşlarla 10** → 1. satır Kart. Sonra GBP silme kapatılır, "Kuru yeniden al" tıklanır. Ölçüm `betikler/sikisma-olc.js` + `betikler/tur/ekstra.js` (kopyaları `cmp` → birebir).

| mod | tema | w | kur yok: Kaydet · sebep | POST (kur yok, Kaydet'e basıldı) | "Kuru yeniden al" `/kur/guncel` isteği | kur geldi: TL · Kaydet | K1..K13 + yatay (kur yok / kur geldi) |
|---|---|---|---|---|---|---|---|
| BASİT | açık | 1280 | PASİF · "Döviz kuru alınamadı — …" | 0 | 1 | 651,94 ₺ · ETKİN | 0 / 0 |
| BASİT | açık | 375 | PASİF · aynı | 0 | 1 | 651,99 ₺ · ETKİN | 0 / 0 |
| BASİT | koyu | 1280 | PASİF · aynı | 0 | 1 | 651,99 ₺ · ETKİN | 0 / 0 |
| BASİT | koyu | 375 | PASİF · aynı | 0 | 1 | 651,99 ₺ · ETKİN | 0 / 0 |
| PRO | açık | 1280 | PASİF · aynı | 0 | 1 | 651,99 ₺ · ETKİN | 0 / 0 |
| PRO | açık | 375 | PASİF · aynı | 0 | 1 | 651,99 ₺ · ETKİN | 0 / 0 |
| PRO | koyu | 1280 | PASİF · aynı | 0 | 1 | 651,94 ₺ · ETKİN | 0 / 0 |
| PRO | koyu | 375 | PASİF · aynı | 0 | 1 | 651,89 ₺ · ETKİN | 0 / 0 |
(TL farkları canlı kurun dakikalık değişimidir.) Her hücrede fark çubuğu kur yokken sebep metni (BASİT "—"), "0 ₺ ✓" YOK.
**İlk turda bulunup düzeltilen (ölçüm):** 375 BASİT döviz satırında Döviz/miktar/TL yan yana sıkışıyordu (K3 kırık 2, K7 1, K12 miktar kutusu 26 px) ve 1280'de GBP seçicisi 73 px (K12); PRO 375'te "Kurdan geldi" rozeti kutunun üstüne biniyordu (K9); PRO 1280'de kur-yok bandı sil düğmesini alt satıra itiyordu → `tuval-satis.css` t21 blokları (dar ekranda döviz alanları tam genişlik, birim en az 6,5 rem, bant `order: 2`, rozetli etiket yüksekliği içeriğe göre). Tablo son turdur.

**Kaydetme + SQL (BASİT açık 1280, kur geldikten sonra Kaydet):** POST `/islemler/satis-fisi` 1 kez, başarı. 
```
SELECT o.fis_id,o.sira,o.arac,o.tutar_tl,o.para_birimi,o.doviz_tutar,k.ad FROM odeme_kalemleri o LEFT JOIN kasalar k ON k.id=o.kasa_id WHERE … fis_id=01M3QBQ97MSCNKT4B20NA2KD15
01M3QBQ97MSCNKT4B20NA2KD15  0  KART   2547.92  TRY  NULL   Ana Kasa
01M3QBQ97MSCNKT4B20NA2KD15  1  DOVIZ   652.08  GBP  10.00  Sterlin Kasa
```
Ekranda 652,08 ₺ = SQL 652.08; KART 2.547,92 + DÖVİZ 652,08 = fiş 3.200,00. (İlk denemede 95.636 ₺'lik fişte sunucu "30.000 ₺ üstündeki fişte nakit kullanılamaz" dedi — efektif döviz nakit sayılıyor; sunucunun yasal kapısı, istemci değiştirmedi.)

**MASAK (`t21/masak.mjs`, t20 betiğinin kopyası + kilit/bant alanları; `/masak/gereksinim` CDP `Fetch.failRequest` ile düşürüldü, eşik üstü 250.000):**
| mod | tema | w | Kaydet | kilit sebebi | `#islem-yasal-kapi` | istek ilk → 3 sn | Yeniden dene | K1..K13 |
|---|---|---|---|---|---|---|---|---|
| BASİT | açık/koyu | 1280 · 375 | PASİF | "Kimlik kuralı alınamadı — bağlantıyı kontrol edip yeniden deneyin." | YOK | 1 → 1 | 1 → 2 | 0 |
| PRO | açık/koyu | 1280 · 375 | PASİF | aynı | YOK | 1 → 1 | 1 → 2 | 0 |
(8/8 hücre.) 

**Gerileme ölçümü (CSS değiştiği için):** t7 `senaryo.mjs` (çıktı `t21/m/olcum.jsonl`) — `satis-kart`, `karma-kart-altin`, `karma-nakit-kart-altin` × {BASİT, PRO} × {1440, 1280, 1024, 960, 768, 375 açık + 1440, 1280, 375 koyu} = **54 ölçüm, K1..K13 + yatay = 0**.

Toplam bu koşumda son turda **78 tarayıcı ölçümü** (8 döviz hücresi × 2 durum = 16 + 8 MASAK + 54 gerileme; ilk turlar hariç). Read ile GÖRÜLEN görüntüler (kopyaları `ham/TUVAL-KARSILASTIRMA/SATIS-3-T21-*.png`): `T21-doviz-kuryok-BASIT-aydinlik-1280`, `T21-doviz-kuryok-BASIT-karanlik-375`, `T21-doviz-kuryok-PRO-aydinlik-1280`, `T21-doviz-kuryok-PRO-karanlik-1280`, `T21-doviz-kuryok-PRO-aydinlik-375`, `T21-doviz-kurgeldi-BASIT-aydinlik-1280`, `T21-masak-ustu-BASIT-aydinlik-1280`, `T21-masak-ustu-PRO-karanlik-375`.

### 34.5 Derleme · denetim · tam süit
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata 0 Uyarı** (iki ara denemede başka şeridin eşzamanlı derlemesi `obj/ref` dosyalarını sildi / çalışan API apphost'u kilitledi → yeniden koşuldu, kod kaynaklı değil).
- `bash dotnet/denetim.sh` → **DENETİM TEMİZ** (rc 0).
- Tam süit (t2 DB, `/private/tmp/kuyumcu-serit/trx-pSATIS-3-t21`, API ve Chrome kapalıyken): **7365/7365 geçti, 0 kırmızı** (günlükteki `DbUpdateException` satırları beklenen-hata testlerinin kaydı). Bu paketin yeni kırmızısı 0; başka şeridin kırmızısı da yok.
- `Islemler.razor` 1399 satır (≤ 1400).

### 34.6 Temizlik
API 5162, vekil 5163, CDP 9906–9911 kapalı (`lsof` boş); 5198/5199/5200'e dokunulmadı. Kiracı 01M3QAW76FY7AFG0THKN9RKAPZ `kiraci-temizle.sh --uygula` (rc 0) + kendi `kiraci_davetleri` satırı (1) silindi; `tenants` başlangıç **14** → son **14** (bu kiracıdan 0 satır). Anahtar ve fikstür dosyaları `printf ''` ile boşaltıldı. Chrome profilleri `t21/profil-99xx` — hook `rm`'i engellediği için silme **kurucu/orkestratör** (DEVİR NOTU 4 gereği not edildi).

### 34.7 Değişen dosyalar (t21) · DoD
`Bilesenler/Fis/FisFark.cs` (`Gonderilecek`, `DovizKuruEksik`) · `Bilesenler/Fis/FisOdemeBacaklari.razor` (kur-yok bandı + "Kuru yeniden al", `DovizleriYenile`, `KurYenidenAl`, `BirimDegisti`, fark metni/rengi, `DovizTl` yorumu) · `Bilesenler/Fis/FisVerdimAldim.razor` (`KurEksik`) · `Pages/Islemler.razor` (VERESİYE `gb`, MASAK kilit sebebi — satır sayısı değişmedi) · `Pages/Islemler.Odeme.cs` (`YasalBantAcik`, `KasasizSatirSebebi`, `KarmaKilitSebebi`) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (3 anahtar TR+EN: `DovizKuruYok`, `DovizKuruTekrar`, `OdemeKasasiYok`) · `wwwroot/css/tuval-satis.css` (t21 blokları; belirteç, sabit renk yok, satır içi style yok) · testler (`SATIS3DovizKurTestleri.cs` yeni, `SATIS3SinirTestleri.cs` +1 teori). Yeni varlık/uç/migration YOK. POST gövdesi ŞEMASI değişmedi (kur yokken gövde hiç gitmez; kur gelince döviz satırı mevcut `odemeler[]` sözleşmesiyle gider). Yeni düğme ("Kuru yeniden al") gerçek istek atar (bUnit + canlı: 1 istek). Mevzuat hükmü yazılmadı; döviz kuru istemcide üretilmez (fail-closed).

### 34.8 Durum
Paket kapısı: (1) derleme 0/0 ✓ · (2) kendi testleri 25/25 + ilgili küme ✓ · (3) denetim TEMİZ ✓ · (4) 10/10 mutasyon kırmızı + cmp ✓ · (5) canlı: kur yokken POST 0, kur gelince SQL'de DÖVİZ satırı; 16 hücre + 54 gerileme ölçümü K = 0 ✓ · (6) kiracı silindi, 14 → 14 ✓ · (7) tam süit 7365/7365, bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır**. Açık (alan dışı): PRO 960 Kaydet satırı / MasakFormu yıldızı (**KATMAN / ZO-ISLEM-A**), MasakFormu kümülatif metni + gereksinim ucu yetkisi (**ZO-ISLEM-A**), görünen h1 / K9 (**KATMAN**), arama satırı stok (**Api/Urun**), eski 4 davet satırı + Chrome profilleri silme (**orkestratör/kurucu**).

## 35. 30.09 t22 koşumu (00:10 – 01:20) — tasarım hakeminin 2 YÜKSEK bulgusu (Kapora nakit eşiği · BASİT tek nakitte açık "Alınan / Para üstü")

Ortam: API **5162** (`--no-build`, Development; sistem anahtarı bu koşum için üretildi, dosyası sonra `printf ''` ile boşaltıldı), vekil **5163**, başsız Chrome + ham CDP **9912** (tarayıcı bölmesi KULLANILMADI). Kiracı `PAKETSATIS-3-TEST t22 Kuyumculuk` (01M3QHVJ38GJVZX5V545ASC7GR) `demo-tohumla.js --prova-kiraci` ile (6 cari, 12 ürün, 9 satış). Test DB `kuyumcu_test_t2`. Geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t22/`. **COMMIT YOK.**
Not: DEVİR NOTU 4 (28.09 21:50, "kod yazma") t9–t21 koşumlarıyla aşılmıştır; bu istemin "önceki doğrulamada kalan" iki bulgusu güncel ve açıktı (koda bakıldı: `FisOdemeKasa` → `<FisParaUstu Nakit=… />` Katli'siz; `FisDigerAvans` nakit eşiği denetimi yok) → kapatıldı.

### 35.1 Bulgu → kök neden → yapılan
| Bulgu | Kök neden | Yapılan | Durum |
|---|---|---|---|
| **YÜKSEK** — Kapora nakit eşiğini önceden denetlemiyor; tıklanınca sayfa dibinde büyük harfli ham VUK paragrafı | `FisDigerAvans` yalnız zorunlu alanları denetliyordu; sunucu reddi `diger.Hata` → `FisDigerIslemler` hata bandı (sayfa dibi) | (1) `DigerBaglam.NakitHad` (sunucunun `/tevsik/gunluk` haddi — sayfanın zaten aldığı `tevsikHaddi`; Kapora/Değişim açılırken bilinmiyorsa bir kez `TevsikGetir`). (2) `FisDigerAvans.NakitHaddiSebebi`: NAKİT ve tutar **> had** (sunucu `olcut > Had` ile aynı; tam hadde nakit serbest) → `FisDigerEylem.EkSebep` → **"Kapora al" PASİF**, yanında **"Nakit 30.000 ₺'yi geçemez — kart ya da havaleyle alın."** (sayı sunucudan, `Bicim.Tl`), tık istek atmaz. (3) Sunucunun tevsik reddi (`400` + "Tevsik…", Hurda.razor `TevsikReddi` ile aynı tanıma) artık `diger.Red` → düğmenin YANINDA kısa **"Bu tutarda nakit alınamaz — kart ya da havaleyle alın."** (`#avans-al-red`, Değişim'de `#degisim-kaydet-red`); ham paragraf hiçbir yerde basılmaz. Eşik sayısı istemciye GÖMÜLMEDİ; had bilinmezse ön denetim yok, kapı sunucuda (fail-closed sunucu). | **KAPANDI** |
| **YÜKSEK** — BASİT Satış tek nakit ödemede isteğe bağlı "Alınan / Para üstü" ve 3 satırlık açıklama açık | `FisOdemeKasa` `FisParaUstu`'yu `Katli` vermeden çiziyordu (t6 yalnız karmayı katlamıştı); `Para` bileşenine `Ipucu` verilmişti | Tek ödemede de `Katli="true"` → açılışta yalnız **"+ Para üstü hesapla"** ("+ Başka ödeme ekle" ile aynı kalıp: `btn-ikincil btn-kucuk`, içerik genişliği); açıklama satırı kaldırıldı, metin düğmenin `title` ipucunda; açılınca "Alınanı yazın" değer gibi büyük değil küçük soluk metin. Alan hâlâ gövdeye girmez (D7). | **KAPANDI** |

### 35.2 Testler
- Yeni `Kuyumcu.Tests/SATIS3T22KaporaNakitTestleri.cs` (4): `T22_Kapora_nakit_haddi_asinca_PASIF_kisa_sebep_POST_YOK_karta_gecince_acilir`, `T22_Kapora_tam_hadde_nakit_SERBEST_sunucu_gibi_buyuktur_kiyasi`, `T22_Had_bilinmezken_sunucu_reddi_dugme_yaninda_KISA_metin_hata_bandinda_ham_paragraf_YOK`, `T22_Kisa_metinlerde_buyuk_harf_vurgusu_YOK_TR_EN_dolu`.
- Güncellenen: `S25YazdirParaUstuEkranTestleri.S25_E6` (açılışta alan/açıklama YOK, "+ Para üstü hesapla" title'ı, açınca `#islem-alinan-ipucu` YOK), `S25_E8` (yeni fişte yine kapalı), `CumaSAldimSadeTestleri.CUMA_S_03` (`#islem-para-ustu-ac` bekler).
- Filtreli: `SATIS3T22|S25YazdirParaUstu|CumaSAldimSade|S26DigerIslemler` → **52/52**; geniş küme `SATIS3|Satis3|Fis|Islem|Karma|SATIS2|Diger|ParaUstu|CumaS|Avans` → **1309/1309**.

### 35.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t22/<kod>.orj` → `perl -0777` tek eşleşme → filtreli test → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T22-1 | `FisDigerAvans`: `EkSebep="@NakitHaddiSebebi"` kaldırıldı | `T22_Kapora_nakit_haddi_asinca_PASIF…`, `T22_Kapora_tam_hadde_nakit_SERBEST…` | BİREBİR |
| M-S3-T22-2 | `FisOdemeKasa`: `Katli="true"` kaldırıldı (tek ödemede alan açık) | `S25_E6`, `S25_E8`, `CUMA_S_03` | BİREBİR |
| M-S3-T22-3 | `Islemler.Diger.cs`: tevsik reddi yakalama dalı silindi (ham metin hata bandına) | `T22_Had_bilinmezken_sunucu_reddi…` | BİREBİR |
| M-S3-T22-4 | kıyas `t > h` → `t >= h` | `T22_Kapora_tam_hadde_nakit_SERBEST…` | BİREBİR |
| M-S3-T22-5 | `FisParaUstu`: `Ipucu="@M.AlinanIpucu"` geri kondu | `S25_E6` | BİREBİR |
**5/5 kırmızı**, hepsi `cmp` BİREBİR; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata 0 Uyarı**. Proje ağacında `.mutorj/.bak/.tmp/.orj` artığı 0.

### 35.4 Gerçek tarayıcı (başsız Chrome + ham CDP 9912; betik `t22/senaryo.mjs`, kayıt `t22/olcum.jsonl`; ölçüm `betikler/sikisma-olc.js` + `betikler/tur/ekstra.js`)
Senaryolar: **satis-tek** ("çeyrek" harf harf arandı, ↓+Enter; tek Nakit) kapalı + "+ Para üstü hesapla" açılmış; **kapora-esik** (Diğer → Kapora, Anadolu…, tutar gerçek tuşlarla 1.234.567,50, Nakit → sonra Kart); **kapora-red** (CDP `Fetch` ile `/tevsik/gunluk` yanıtında `had=0` → istemci ön denetimsiz; 50.000 Nakit → sunucu reddi). Diğer menüsü yalnız PRO'da var (S2-6 kuralı), Kapora satırları PRO.

| senaryo · durum | mod | tema × genişlik | sonuç | K1..K13 + yatay |
|---|---|---|---|---|
| satis-tek · kapalı | BASİT, PRO | aydınlık/karanlık × 1440/1280/375 (12) | `#islem-alinan` YOK, `#islem-para-ustu-sar` YOK, açıklama satırı YOK, "+ Para üstü hesapla" (title = açıklama) | 0 |
| satis-tek · açık | BASİT, PRO | aynı (12) | alan açık, "Alınanı yazın" küçük soluk (`metin-kucuk metin-soluk`), ipucu satırı YOK | 0 |
| kapora-esik · nakit eşik üstü | PRO | aydınlık/karanlık × 1440/1280/375 (6) | "Kapora al" **PASİF**, yanında "Nakit 30.000 ₺'yi geçemez — kart ya da havaleyle alın."; tık → **POST 0**; ham paragraf YOK | 0 |
| kapora-esik · Kart | PRO | aynı (6) | ETKİN, kilit metni YOK | 0 |
| kapora-red · sunucu reddi | PRO | aynı (6) | POST 1 → `#avans-al-red` "Bu tutarda nakit alınamaz — kart ya da havaleyle alın."; hata bandı YOK; sayfada "Tevsik zorunluluğu/BİR KISMI" YOK | 0 |
**42 ölçüm, kusur 0.** Açılış boyu: 1440/1280 ≤ 1,1 ekran; 375'te 1,5–1,7 (dikey yığın, yatay kaydırma 0).
**SQL (kiracı silinmeden önce):** `SELECT tur,odeme_turu,COUNT(*) FROM hareketler WHERE tenant_id=… AND tur LIKE 'AVANS%'` → **0 satır** (6 reddedilen nakit kapora deftere hiçbir şey yazmadı; kiracının 24 hareketi tohumdan).
Read ile GÖRÜLEN görüntüler (kopyaları `ham/TUVAL-KARSILASTIRMA/SATIS-3-T22-*.png`): `satis-tek-kapali-BASIT-aydinlik-1440` (ilk turda düğme tam genişlik gri çubuk çıktı → `btn-ikincil` + `tuval-satis.css` t22 bloğu; son turda "+ Başka ödeme ekle" ile aynı), `satis-tek-acik-BASIT-aydinlik-1440`, `satis-tek-kapali-BASIT-karanlik-1440`, `kapora-esik-nakit-esik-ustu-PRO-aydinlik-1440`, `kapora-red-sunucu-red-PRO-aydinlik-1440`, `kapora-esik-nakit-esik-ustu-PRO-karanlik-375`.

### 35.5 Derleme · denetim · tam süit
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata 0 Uyarı**.
- `bash dotnet/denetim.sh` → tek ✗: **P-23 ODEME_KALEMSIZ_YOL = 1** (CANLI kuyumcu_net ölçümü). Kaynak satır: hareket `01M3QG1P6EVK9HM5CVRTCBKWXJ` · kiracı **TEST-KESIF Kuyumculuk** · SATIS · KART, ödeme kalemi yok — bu şeridin kiracısı/verisi DEĞİL (bu koşum o sırada DB'ye hiç yazmamıştı). Bu paketin sayaçlarının hepsi ✓. Sahibi: TEST-KESIF kiracısını açan ajan / orkestratör.
- Tam süit (t2 DB) iki kez: (a) 7411 → **7410 geçti, 1 kırmızı** `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (bUnit zaman aşımı, API açılışıyla eşzamanlı; tek başına 3/3 yeşil); (b) 7413 → **7412 geçti, 1 kırmızı** `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (WaitForFailed zaman aşımı; tek başına 3/3 yeşil, (a)'da yeşildi). İkisi de farklı koşumlarda farklı test — yük altı kararsızlık; bu paketin değiştirdiği koda (Diğer/Kapora, tek ödeme para üstü) bağlı değil. **Bu paketten yeni kalıcı kırmızı 0.**

### 35.6 Temizlik
API 5162, vekil 5163, CDP 9912 kapalı (`lsof` boş); 5198/5199/5200'e dokunulmadı. Kiracı 01M3QHVJ38GJVZX5V545ASC7GR `kiraci-temizle.sh --uygula` (rc 0) + kendi `kiraci_davetleri` satırı (1) silindi. `tenants`: başlangıç **15** → son **14**; fark bu koşumdan DEĞİL (bu arada başka şeridin `PAKETKATMAN1b-TEST` kiracısı silinmiş; bu koşumun kiracısı 0). Anahtar ve fikstür `printf ''` ile boşaltıldı. Chrome profili `t22/profil-9912` — hook `rm`'i engellediği için silme **kurucu/orkestratör**.

### 35.7 Değişen dosyalar (t22) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Bilesenler/Fis/{FisDigerAvans.razor, FisDigerDegisim.razor, FisDigerEylem.razor (EkSebep, Red), FisDigerModelleri.cs (DigerBaglam.NakitHad, DigerIslemDurumu.Red), FisOdemeKasa.razor, FisParaUstu.razor}` · `Kuyumcu.Web/Pages/Islemler.Diger.cs` · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (2 anahtar TR+EN: `DigerNakitHaddi`, `DigerNakitRed`) · `wwwroot/css/tuval-satis.css` (t22 bloğu; belirteç, sabit renk yok, satır içi style yok) · testler (`SATIS3T22KaporaNakitTestleri.cs` yeni; `S25YazdirParaUstuTestleri.cs`, `CumaSAldimSadeTestleri.cs`). Dokunulan sayfa: `Islemler` (bu paketin sayfası — tek dokunuş kuralı içinde). `FisDigerIslemler.razor` (KATMAN) DEĞİŞMEDİ. Yeni varlık/uç/migration YOK; POST gövdeleri DEĞİŞMEDİ. Yeni düğme yok ("+ Para üstü hesapla" eskiden vardı, istek atmaz — yalnız alanı açar, D7). Mevzuat: yeni hüküm yazılmadı; had sunucudan, kıyas sunucunun `olcut > Had` kuralıyla aynı (t12'de kaynaklı).

### 35.8 Durum
Paket kapısı: (1) derleme 0/0 ✓ · (2) kendi testleri 52/52 + ilgili küme 1309/1309 ✓ · (3) paketin denetim sayaçları ✓ (tek ✗ başka kiracının canlı verisi) · (4) 5/5 mutasyon kırmızı + cmp ✓ · (5) canlı: 42 ölçüm K = 0, reddedilen kapora POST'ları SQL'de 0 satır ✓ · (6) kiracı silindi ✓ · (7) tam süitte bu paketten yeni kalıcı kırmızı 0 (iki farklı yük kararsızlığı, tek başına yeşil) ✓ → **BİTTİ, doğrulamaya hazır**.
Açık (alan dışı, sahibiyle): P-23 canlı sayacı TEST-KESIF kiracısının kalemsiz KART satışı (**o kiracıyı açan ajan/orkestratör**); `PosBarkodTestleri` / `AlisOdemeAraciEkranTestleri` yük altı zaman aşımı (**ZO-ISLEM-A / orkestratör — bUnit bekleme süresi**); görünen h1 / K9 (**KATMAN**), stok sütunu (**Api/Urun**); Değişim nakit farkının istemci ön denetimi yok (fark sunucuda hesaplanır; red kısa metinle düğme yanında) — gerekirse fark önizleme ucu ayrı paket; Chrome profili ve /private/tmp artıkları (hook) **kurucu**.

## 36. 30.09 t23 koşumu — hasım doğrulayıcının KRİTİK t22 gerilemesi (kapora iadesi / siparişten satış tevsik reddi SESSİZ) + 2 DÜŞÜK metin bulgusu

Ortam: API **5162** (`--no-build`, Development; sistem anahtarı bu koşum için rastgele üretildi, değer yazılmadı, dosyası sonra `printf ''`), vekil **5163**, başsız Chrome + ham CDP **9913** (tarayıcı bölmesi KULLANILMADI). Kiracı `PAKETSATIS-3-TEST t23 Kuyumculuk` (01M3QM62T7P6M207XJ92H0BJK3, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış). Test DB `kuyumcu_test_t2`. Geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t23/`. **COMMIT YOK.**

### 36.1 Bulgu → kök neden → yapılan
| Bulgu | Kök neden | Yapılan | Durum |
|---|---|---|---|
| **KRİTİK** — kapora İADESİ ve siparişten satışta sunucunun tevsik reddi ekranda hiç görünmüyor | `Islemler.Diger.cs` t22 `catch … when (400 && "Tevsik…")` HER Diğer eylemini yakalayıp `diger.Red` yapıyordu; `Red`'i yalnız `FisDigerAvans` (AvansAl) ve `FisDigerDegisim` (Degisim) çiziyor | Yakalama koşuluna `&& istek.Eylem is DigerEylemler.AvansAl or DigerEylemler.Degisim` eklendi; kalan her eylem (AvansIade, SiparisCevir, …) eski yolla `diger.Hata` → `#islem-diger-hata` bandı. Kapora iadesi her zaman kasadan (nakit) çıktığı için "kart ya da havale" kısa metni orada YANLIŞ olurdu → iadede sunucunun tam gerekçesi bantta gösterilir | KAPANDI |
| **DÜŞÜK** — `DigerNakitRed` tek yönlü ("alınamaz") ve kümülatif reddi "bu tutarda" diye yanlış anlatıyor | metin | TR: "Bu işlem nakitle yapılamaz — kart ya da havale kullanın." · EN: "This transaction cannot be made in cash — use card or bank transfer." (yön ve kümülatif durum için nötr) | KAPANDI |
| **DÜŞÜK** — "30.000 ₺'yi" ek uyumsuzluğu | metin | TR: "Nakit en çok {0} olabilir — kart ya da havale kullanın." · EN: "Cash can be at most {0} — use card or bank transfer." | KAPANDI |
| **DÜŞÜK** — denetim.sh P-23 canlı sayacı | başka kiracıların verisi (aşağıda) | dokunulmadı | ALAN DIŞI |

### 36.2 Testler
- `SATIS3T22KaporaNakitTestleri.cs` +2: `T23_Kapora_iadesi_tevsik_reddi_SESSIZ_DEGIL_hata_bandinda_gorunur`, `T23_Siparisten_satis_tevsik_reddi_SESSIZ_DEGIL_hata_bandinda_gorunur` (sahte API: `/islemler/avans/iade` ve `/satisa-cevir` 400 "Tevsik…"; `AcikAvans` ayarlanabilir; sipariş fikstürü S26 ile aynı).
- Doğrulayıcının `Dogrulayici/SATIS3SinirTestleri.cs` `D22_avans_iadesi_tevsik_reddi_SESSIZ_kalmamali` artık **YEŞİL**.
- Filtreli `SATIS3T22|D22_|S26DigerIslemler` → **43/43**; geniş küme `SATIS3|Satis3|SATIS2|Islem|Fis|S26Diger` → **1246/1246**.
- **Tam süit** (t2 DB, trx `/private/tmp/kuyumcu-serit/trx-pSATIS-3`) → **7458/7458 geçti, 0 kırmızı**.

### 36.3 Mutasyon
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T23-1 | `Islemler.Diger.cs`: `&& istek.Eylem is DigerEylemler.AvansAl or DigerEylemler.Degisim` silindi (t22 davranışı) | `T23_Kapora_iadesi_tevsik_reddi_SESSIZ_DEGIL…`, `T23_Siparisten_satis_tevsik_reddi_SESSIZ_DEGIL…`, `Dogrulayici.SATIS3SinirTestleriT22Hasim.D22_avans_iadesi_tevsik_reddi_SESSIZ_kalmamali` (3 kırmızı / 18) | BİREBİR (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t23/`) |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata 0 Uyarı**.

### 36.4 Gerçek tarayıcı (başsız Chrome + ham CDP 9913; betik `t23/senaryo.mjs`, kayıt `t23/olcum.jsonl`; `betikler/sikisma-olc.js` + `betikler/tur/ekstra.js`)
Diğer menüsü yalnız PRO'da (S2-6). Senaryolar: **kapora-iade** (Anadolu…, 40.000 ₺ KART kapora alındı → "Kaporayı geri öde" 40.000 + gerekçe → onay → sunucu tevsik reddi); **kapora-esik** (1.234.567,50 Nakit → Kart); **kapora-red** (`/tevsik/gunluk` had=0 → 50.000 Nakit → sunucu reddi).

| senaryo · durum | tema × genişlik | sonuç | K1..K13 + yatay |
|---|---|---|---|
| kapora-iade · iade-red | aydınlık/karanlık × 1440/1280/375 (6) + ilk deneme 1 | POST `/islemler/avans/iade` gitti, 400 → `#islem-diger-hata` bandında sunucunun tam tevsik gerekçesi GÖRÜNÜR; başarı bandı YOK | 0 |
| kapora-esik · nakit eşik üstü | aynı (6) | "Kapora al" PASİF, yanında **"Nakit en çok 30.000 ₺ olabilir — kart ya da havale kullanın."**; POST 0 | 0 |
| kapora-esik · Kart | aynı (6) | ETKİN, kilit metni yok | 0 |
| kapora-red · sunucu reddi | aynı (6) | `#avans-al-red` **"Bu işlem nakitle yapılamaz — kart ya da havale kullanın."**; hata bandı yok, ham paragraf yok | 0 |
**25 ölçüm, kusur 0.** Read ile GÖRÜLEN: `T23-kapora-iade-iade-red-PRO-aydinlik-1440`, `T23-kapora-iade-iade-red-PRO-karanlik-375`, `T23-kapora-esik-nakit-esik-ustu-PRO-aydinlik-1280` (kopyaları `ham/TUVAL-KARSILASTIRMA/T23-*.png`, + `T23-kapora-red-sunucu-red-PRO-karanlik-1440`).
**SQL (kiracı silinmeden önce):** `SELECT tur,odeme_turu,COUNT(*),SUM(tl_tutari) FROM hareketler WHERE tenant_id='01M3QM62…' AND tur LIKE '%AVANS%' GROUP BY 1,2` → yalnız `AVANS_ALINDI KART 7 280000.00`; **AVANS_IADE satırı 0** (7 reddedilen nakit iade deftere hiçbir şey yazmadı).
Siparişten satış canlıda denenemedi: tohum kiracısında HAZIR sipariş yok; yol bUnit `T23_Siparisten_satis…` + mutasyonla kanıtlı (aynı `catch` dalı).

### 36.5 Derleme · denetim
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata 0 Uyarı**.
- `bash dotnet/denetim.sh` → tek ✗ **P-23 ODEME_KALEMSIZ_YOL = 3** (CANLI kuyumcu_net): `01M3QG1P6EVK9HM5CVRTCBKWXJ` **TEST-KESIF Kuyumculuk** SATIS/KART; `01M3QM6PTN7JWMXJYZ4KVAS9RY`, `01M3QM6PTPMAMGFEHKP2PBR498` **PAKETZO-BELGE-TEST Kuyumculuk** SATIS/KARMA. Bu şeridin verisi DEĞİL (bu kiracıda 0 satır). Paketin diğer sayaçları ✓.

### 36.6 Temizlik
API 5162, vekil 5163, CDP 9913 kapalı (`lsof` boş); 5198/5199/5200'e dokunulmadı. Kiracı 01M3QM62T7P6M207XJ92H0BJK3 `kiraci-temizle.sh --uygula` (rc 0) + kendi `kiraci_davetleri` satırı (1) silindi. `tenants`: başlangıç **15** → son **15**. Anahtar ve fikstür `printf ''`. Chrome profili `t23/profil-9913` (114 MB) — hook `rm`'i engellediği için silme **kurucu/orkestratör**. Proje ağacında `.mutorj/.bak/.tmp/.orj` artığı 0.

### 36.7 Değişen dosyalar (t23)
`Kuyumcu.Web/Pages/Islemler.Diger.cs` (catch koşulu + yorum) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (`DigerNakitHaddi`, `DigerNakitRed` TR+EN metni) · `Kuyumcu.Tests/SATIS3T22KaporaNakitTestleri.cs` (+2 test, sahte API iade/sipariş yolları, `Panel(tur)`). Dokunulan sayfa: `Islemler` (bu paketin sayfası). Yeni varlık/uç/migration YOK; POST gövdeleri DEĞİŞMEDİ; yeni düğme yok. Mevzuat: yeni hüküm yazılmadı (had ve ret sunucudan).

### 36.8 Durum
(1) derleme 0/0 ✓ · (2) kendi testleri 43/43 + küme 1246/1246 ✓ · (3) paket sayaçları ✓ (tek ✗ başka kiracıların canlı verisi) · (4) mutasyon 1/1 kırmızı (3 test) + cmp ✓ · (5) canlı 25 ölçüm K=0 + SQL ✓ · (6) kiracı silindi, tenants 15→15 ✓ · (7) tam süit 7458/7458, bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**
Açık (alan dışı, sahibiyle): P-23 canlı sayacı — TEST-KESIF ve PAKETZO-BELGE-TEST kiracılarının kalemsiz satışları (**o kiracıları açan ajanlar / orkestratör**); görünen h1 / K9 (**KATMAN**); stok sütunu (**Api/Urun**); kapora iadesi ret bandındaki sunucu metni uzun ve "BİR KISMININ" büyük harfli — metin sunucunun (`TevsikKurali`) — kısaltmak Api/Satis alanında ayrı iş; Chrome profilleri (hook) **kurucu**.

## 37. 30.09 t24 koşumu (02:35 – 03:35) — orkestratör 02:30 (2 tur kuralı istisnası): yasal kapı hakkında TEK, tutarlı mesaj (tasarım hakemi KRİTİK)

Ortam: API **5162** (`--no-build`, Development; sistem anahtarı bu koşum için rastgele üretildi, değer yazılmadı, dosyası sonra `printf ''`), vekil **5163**, başsız Chrome + ham CDP **9914** (tarayıcı bölmesi KULLANILMADI). Test DB `kuyumcu_test_t2`. Geçici dosyalar `/private/tmp/kuyumcu-serit/SATIS-3/t24/`. **COMMIT YOK.** Kapsam yalnız bu KRİTİK (orkestratör: "başka iş YOK").

### 37.1 Bulgu → kök neden → yapılan
| Bulgu | Kök neden | Yapılan |
|---|---|---|
| Peşin + 304.872 ₺: "Peşin satış: müşteri adı gerekmez." (sarı şerit) ile "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." AYNI ANDA; ayrıca "Müşteri seçin · Neden?", "yasal bir kapıya takıldı" bandı, MASAK kimlik kutusu | `FisMusteriSatiri`: `EsikYakin` (=MasakGerekli) gelince `#islem-pesin-notu` gizli nottan **uyarı bandına** dönüşüyor ama metni hâlâ "müşteri adı gerekmez"; üst bant, Kaydet sebebi ve ödeme sütunu durumu birbirinden habersiz; MASAK kutusu Peşin'de de açılıyordu (sunucu K2 Peşin'i zaten reddeder) | (1) `#islem-pesin-notu` eşik aşılınca **hiç çizilmez** (`PesinSecili && !EsikYakin`; `EsikYakin = MasakGerekli || PesinEsikUstu`). (2) Tek kaynak `Islemler.Odeme.cs`: `PesinEsikUstu` (sunucu K2 ile aynı ölçü: fiş > tevsik haddi — takasta nakit farkı — **ya da** ≥ MASAK kimlik eşiği; eşikler `/tevsik/gunluk`, `/masak/esikler`'den, bilinmeyen eşik kilitlemez) ve `YasalEngelMetni` (engeller TEK cümlede, sırayla). (3) BASİT üst bant (`BasitBantAcik`/`BasitBantMesaji`) = aynı cümle; Kaydet altındaki satır aynı cümleyse görsel gizli (`KilitBantlaAyni`, title'da kalır); ödeme sütunu kısa durumu görsel gizli (`FisOdemeKasa.DurumGizli` ← `OdemeDurumuGizli`; "Neden?" katlı ayrıntısı yerinde — özellik silinmedi). (4) MASAK kimlik kutusu Peşin eşik üstündeyken açılmaz (`MasakGerekli && !PesinEsikUstu`); gerçek müşteri seçilince açılır. |
| 129 bin ₺ nakitte önce "Müşteri seçin", müşteri seçilince "Nakit alınmaz" (engeller tek tek) | Kaydet sebebi zinciri ilk engelde duruyordu | Peşin + nakit kuralı → **"Müşteri seçin ve ödemeyi kart ya da havaleyle alın."** (para müşteriye ödenen yönde "…yapın."); gerçek müşteri + MASAK kimliği eksik + nakit → **"Kimlik bilgilerini girin ve ödemeyi kart ya da havaleyle alın."**; yalnız Peşin (kart) → "Bu tutarda Peşin Müşteri olmaz — müşteri seçin."; yalnız nakit → eski cümleler (değişmedi). MASAK tamam + nakit sürüyorsa BASİT bandı "gereken alanlar açıldı" demez, Kaydet sebebiyle aynı nakit cümlesini söyler. |

Sözlük: `Sozlukler.Satis3.cs` +4 anahtar TR+EN (`PesinVeNakitAl/Ode`, `KimlikVeNakitAl/Ode`; büyük harf vurgusu yok, eşik sayısı yok). POST gövdeleri, sunucu kuralları, id'ler DEĞİŞMEDİ; Api'ye dokunulmadı; yeni mevzuat hükmü yazılmadı (eşikler ve kıyas sunucudaki `TevsikKapisi.Dogrula` `olcut > Had` / `MasakKapisi.DogrulaKumulatif` kademe ≠ Yok ile aynı).

### 37.2 Mesaj envanteri (her tutar aralığı, BASİT ve PRO) — bUnit
Yeni `Kuyumcu.Tests/SATIS3T24YasalMesajTestleri.cs` (`SATIS3EkranTestleri` partial; fikstür ortak, `masakEsikJson` ayarlanır): görünen yasal cümle envanteri (`#islem-yasal-kapi`, `#islem-pesin-notu`, `#islem-tevsik-durum-metin`, `#islem-kaydet-kilit`, `#islem-pesin-esik`, `#islem-pesin-hata`, MASAK kutusu) + **çelişen çift sayacı** ("müşteri adı gerekmez" türü ipucu × Peşin engeli; Peşin engeli × MASAK kutusu).
| Test (BASİT + PRO) | Durum | Beklenen |
|---|---|---|
| `T24_had_ALTI_…` | fiş 10.000 < had 50.000 | engel yok, Kaydet açık, ipucu DOM'da, çelişen 0 |
| `T24_hadde_ESIT_…` | fiş = had | sunucu gibi `>` kıyası: engel yok, çelişen 0 |
| `T24_had_USTU_NAKIT_…` | fiş > had, Nakit | TEK cümle `PesinVeNakitAl`, `#islem-pesin-notu` DOM'da YOK, durum görünmez, POST 0 |
| `T24_had_USTU_KART_…` | fiş > had, Kart | TEK cümle `PesinTutarKilit` (BASİT bantta, PRO Kaydet altında) |
| `T24_MASAK_kimlik_esigi_USTU_…` | nakit haddi altı, ≥ kimlik eşiği | MASAK kutusu YOK, TEK cümle; gerçek müşteri seçilince kutu açılır, Peşin cümleleri kalkar |
| `T24_gercek_musteri_secilince_…` | fiş > had, gerçek müşteri | Peşin cümleleri 0, yalnız nakit kuralı |
| `T24_gercek_musteride_MASAK_kimligi_eksik_VE_nakit_…` | gerçek müşteri, kimlik eksik + nakit | TEK cümle `KimlikVeNakitAl`; karta geçince yalnız `MasakKimlikEksik` |
| `T24_Sozluk_TR_EN_…` | 4 yeni anahtar | dolu, büyük harf vurgusu ve rakam yok |
Güncellenen: `T10_Pesin_Musteri_sunucu_haddinde_KAYDET_KILITLI…` (nakit ödemede artık iki engel tek cümle → `PesinVeNakitAl`). Filtreli `SATIS3EkranTestleri.T24|T10_Pesin|CumaSAldimSade` → **21/21**; geniş küme (SATIS3/Satis3/SATIS2/Islem/Fis/Pesin/Masak/S23/S21/S1i/Tevsik/CumaS/Yasal/Basit/Hurda/Iade/Takas/Alis) → **1942/1943**, tek kırmızı `ZOISLEMAOdemeTutarKapisiHttpTestleri.ZOP23h_…` (ZO-ISLEM-A şeridinin o sırada yazılmakta olan sunucu kapısı; Api'ye bu koşum dokunmadı; aşağıdaki tam süitte YEŞİL).

### 37.3 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/SATIS-3-t24/<kod>.orj` → `perl -0777` tek eşleşme → filtreli test → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-S3-T24-1 | `FisMusteriSatiri`: `@if (PesinSecili && !EsikYakin)` → `@if (PesinSecili)` (ipucu eşik üstünde de çizilir) | `T24_had_USTU_NAKIT…`×2, `T24_had_USTU_KART…`×2, `T24_MASAK_kimlik…`×2 | BİREBİR |
| M-S3-T24-2 | `YasalEngelMetni`: nakit birleşimi kaldırıldı (hep `PesinTutarKilit`) | `T24_had_USTU_NAKIT…`×2, `T10_Pesin…`×2 | BİREBİR |
| M-S3-T24-3 | MASAK kutusu koşulu `MasakGerekli && !PesinEsikUstu` → `MasakGerekli` | `T24_MASAK_kimlik…`×2 | BİREBİR |
| M-S3-T24-4 | BASİT bant mesajı eski ifadeye (`!MasakGerekli && NakitBandiVar ? NakitBantMetni : null`) | `T24_had_USTU_KART…(BASIT)`, `T24_had_USTU_NAKIT…(BASIT)` | BİREBİR |
| M-S3-T24-5 | `TevsikTutar > h` → `>= h` (sunucudan sıkı) | `T24_hadde_ESIT…`×2 | BİREBİR |
| M-S3-T24-6 | sayfadan `DurumGizli` kaldırıldı | `T24_had_USTU_NAKIT…`×2 | BİREBİR |
| M-S3-T24-7 | `BasitSatisAkisi KapiAcik="@BasitBantAcik"` → `@YasalBantAcik` | `T24_had_USTU_KART…(BASIT)`, `T24_had_USTU_NAKIT…(BASIT)` | BİREBİR |
| M-S3-T24-8 | Kimlik+nakit birleşik dalı `if (false)` | `T24_gercek_musteride_MASAK…`×2 | BİREBİR |
| M-S3-T24-9 | `OdemeDurumuGizli => false` | `T24_gercek_musteride_MASAK…(PRO)`, `T24_had_USTU_NAKIT…(PRO)` | BİREBİR |
**9/9 kırmızı**, hepsi `cmp` BİREBİR; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata 0 Uyarı**. Proje ağacında `.mutorj/.orj/.bak/.tmp` artığı 0.

### 37.4 Gerçek tarayıcı — mesaj envanteri (başsız Chrome + ham CDP 9914; betik `t24/senaryo.mjs`, kayıt `t24/olcum.jsonl`; ölçüm `betikler/sikisma-olc.js` + `betikler/tur/ekstra.js`)
Kiracı `PAKETSATIS-3-TEST t24 Kuyumculuk` (`demo-tohumla.js --prova-kiraci`, 6 cari · 12 ürün · 9 satış; sunucu eşikleri: nakit 30.000, MASAK kimlik 185.000). Ürün CDP ile harf harf aranıp ↓+Enter ile eklendi. Akış (tek sayfada): a1 çeyrek (≈11 bin, had altı) → a2 + burma bilezik (≈299 bin / PRO'da ≈107 bin, had üstü, Nakit) → a3 Kart → a4 (MASAK üstü, Kart) → a5 Nakit → a6 gerçek müşteri. "Hadde eşit": aynı kurla ölçülen fiş toplamı `/tevsik/gunluk` yanıtında had olarak verildi (CDP Fetch; kur oynadığı için önce yoklama, ardından hemen ölçüm; `toplam = had` kayıtta doğrulandı).
| Durum | BASİT (aydınlık/karanlık × 1440/1280/375) | PRO (aynı) | Görünen yasal cümle sayısı | Çelişen çift | Kaydet · POST |
|---|---|---|---|---|---|
| a1 had altı | 6 ölçüm | 6 | 0 | 0 | ETKİN · 0 |
| hadde eşit (1440) | 2 | 2 | 1 bilgi notu ("X ₺ üstündeki fişte nakit alınamaz…", BASİT sağ sütun) / PRO yalnız "Yasal kapılar: Uygun" kartı | 0 | ETKİN · 0 |
| a2 had üstü Nakit | 6 | 6 | **1**: "Müşteri seçin ve ödemeyi kart ya da havaleyle alın." | 0 | PASİF · 0 |
| a3 had üstü Kart | 6 | 6 | **1**: "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." | 0 | PASİF · 0 |
| a4 MASAK üstü Kart | 6 | 6 | **1**: aynı; MASAK kutusu YOK | 0 | PASİF · 0 |
| a5 MASAK üstü Nakit | 6 | 6 | **1**: "Müşteri seçin ve ödemeyi kart ya da havaleyle alın."; MASAK kutusu YOK; `#islem-pesin-notu` YOK | 0 | PASİF · 0 |
| a6 gerçek müşteri, Nakit | 6 | 6 | **1**: "Kimlik bilgilerini girin ve ödemeyi kart ya da havaleyle alın." + MASAK kutusu (doldurulacak alan) | 0 | PASİF · 0 |
Toplam **86 ölçüm** (akış 72 + eşitlik yoklama/ölçüm 14), **çelişen çift 0**, satış POST'u 0. sikisma-olc K1..K13 + yatay: **83/86 temiz**; 3 ölçümde K3 (BASİT aydınlık 375, a4/a5/a6): "22 Ayar Burma Bilezik" adı kalem tablosunda iki satıra kırılıyor — kalem tablosu sütun dengesi, bu düzeltmenin dokunmadığı alan (tablo/CSS değişmedi; aynı durum karanlık 375'te 0) → § 37.7 açık.
Read ile GÖRÜLEN (kopyaları `ham/TUVAL-KARSILASTIRMA/SATIS-3-T24-*.png`): `a5-masak-ustu-nakit-BASIT-aydinlik-1440` (tek sarı bant, Peşin notu yok, MASAK kutusu yok, "Neden?" katlı), `a5-…-BASIT-aydinlik-375` ve `-karanlik-375`, `a6-gercek-musteri-nakit-BASIT-karanlik-1440` (tek bant + MASAK kutusu), `a2-had-ustu-nakit-PRO-aydinlik-1440` (Kaydet altında tek cümle, sağda durum kartı), `a3-had-ustu-kart-PRO-karanlik-1280`, `esit-BASIT-aydinlik-1440` (Kaydet ETKİN).
Not: PRO "Yasal kapılar" kartı durum tablosudur ("30.000 ₺ nakit kuralı · Aşıldı", "185.000 ₺ kimlik eşiği · Altında"); cümle değil, hiçbir mesajla çelişmiyor — dokunulmadı.

### 37.5 Derleme · tam süit · denetim
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata 0 Uyarı** (bir ara derlemede eşzamanlı başka derlemeden `NETSDK1177 apphost is already signed` + `MSB3026` kopyalama uyarısı çıktı; tekrarında temiz — geçici dosya kilidi).
- **Tam süit** (t2 DB, trx `/private/tmp/kuyumcu-serit/trx-pSATIS-3-t24/_Mac_2026-09-30_03_25_41.trx`) → **7581/7581 geçti, 0 kırmızı**.
- `bash dotnet/denetim.sh` → tek ✗ **P-23 ODEME_KALEMSIZ_YOL = 2** (CANLI kuyumcu_net): `01M3QG1P6EVK9HM5CVRTCBKWXJ` SATIS/KART ve `01M3QQEA86CZ0ADP59MXZTBVEX` SATIS/NAKIT — ikisi de **TEST-KESIF Kuyumculuk** kiracısının; bu şeridin verisi DEĞİL. Paketin diğer sayaçları ✓.

### 37.6 Temizlik
API 5162, vekil 5163, CDP 9914 kapalı (`lsof` boş); 5198/5199/5200'e dokunulmadı. Kiracılar: `01M3QS8229YM20F26K49SFRGRV` (ölçüm kiracısı) ve `01M3QS9XZWJKW9RVYSNPQA2PBX` (tohum betiği ilk denemede kur 40 sn'de gelmeyince yeniden koşuldu; `--eposta` boş geçtiği için ikinci bir prova kiracısı açtı — ikisi de benim) → `kiraci-temizle.sh --uygula` (rc 0) + `kiraci_davetleri` 2 satır + betiğin bıraktığı `denetim_kayitlari` 6 + `is_calismalari` 3 satır silindi; tenant_id taşıyan 70 tabloda bu iki kiracı için **0 satır**. `tenants`: başlangıç **13** → son **16**; fark bu koşumdan DEĞİL (bu arada başka şeritlerin `PAKET-ORTAK-1-TEST … 30b`, `PAKETKATMAN-1b-TEST Dogr` vb. kiracıları açıldı; bu koşumun kiracısı 0). Anahtar ve fikstür `printf ''`. Chrome profili `t24/profil-9914` — hook `rm`'i engellediği için silme **kurucu/orkestratör**.
Not (kiraci-temizle.sh için, sahibi orkestratör): betik `denetim_kayitlari` ve `is_calismalari` satırlarını silmedi; önceki koşumlarda da aynı artık kalmış olabilir.

### 37.7 Değişen dosyalar (t24) · dokunulan sayfalar · DoD
`Kuyumcu.Web/Pages/Islemler.Odeme.cs` (`PesinEsikUstu`, `YasalEngelMetni`, `BasitBantAcik`, `BasitBantMesaji`, `OdemeDurumuGizli`, `KilitBantlaAyni`, `YasalKilitSebebi` ilk dalı) · `Kuyumcu.Web/Pages/Islemler.razor` (BASİT bant parametreleri, `EsikYakin`, MASAK kutusu koşulu, `DurumGizli`) · `Kuyumcu.Web/Bilesenler/Fis/FisMusteriSatiri.razor` (not eşik üstünde çizilmez, ölü bant sınıfı kaldırıldı) · `Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor` (`DurumGizli` parametresi; kısa durum `gorsel-gizli`) · `Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` (+4 anahtar TR+EN) · testler (`SATIS3T24YasalMesajTestleri.cs` yeni; `SATIS3EkranTestleri.cs` partial + `masakEsikJson` + T10 beklentisi). Dokunulan sayfa: `Islemler` (bu paketin sayfası). Yeni varlık/uç/migration YOK; POST gövdeleri, id'ler, yasal kapıların sunucu tarafı DEĞİŞMEDİ; yeni CSS yok; satır içi style yok.

### 37.8 Açık kalanlar (sahibiyle)
- BASİT aydınlık 375'te "22 Ayar Burma Bilezik" kalem adı 2 satır (K3, 3 ölçüm) — kalem tablosu sütun dengesi; orkestratörün "yalnız bu KRİTİK" sınırı gereği dokunulmadı (**SATIS-3 sonraki tur / orkestratör kararı**).
- Hadde eşit durumda BASİT sağ sütundaki "X ₺ üstündeki fişte nakit alınamaz…" bilgi notu Peşin eşiğinden söz etmiyor (had aşılınca yerine tek birleşik cümle geliyor; çelişki değil) — gerekirse metin ayrı iş.
- P-23 canlı sayacı — TEST-KESIF kiracısının kalemsiz satışları (**o kiracıyı açan ajan / orkestratör**); görünen h1 / K9 (**KATMAN**); stok sütunu (**Api/Urun**); Chrome profilleri ve `/private/tmp` artıkları (hook) **kurucu**; `kiraci-temizle.sh` denetim/iş çalışması satırları (**orkestratör**).

### 37.9 Paket kapısı (7 madde, t24)
(1) derleme 0/0 ✓ · (2) kendi testleri 21/21 + geniş küme (başka şeridin o anki tek kırmızısı hariç) ✓ · (3) paket sayaçları ✓ (tek ✗ başka kiracının canlı verisi) · (4) 9/9 mutasyon kırmızı + cmp BİREBİR ✓ · (5) canlı: 86 tarayıcı ölçümü, çelişen çift 0, her engel durumunda görünen yasal cümle 1, POST 0 ✓ · (6) iki test kiracısı silindi, 70 tabloda 0 satır ✓ · (7) tam süit 7581/7581, bu paketten yeni kırmızı 0 ✓ → **BİTTİ, doğrulamaya hazır.**
