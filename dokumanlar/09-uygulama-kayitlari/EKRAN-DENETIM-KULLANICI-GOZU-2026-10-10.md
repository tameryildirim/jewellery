# Ekran denetimi — kullanıcı gözüyle (satış öncesi) · 10.10.2026

> Konum notu: Bu dosya denetim oturumunun çalışma ağacında yazıldı (kanca ana checkout'a doğrudan yazmayı engelledi). Ana checkout'ta hedef yol: `dokumanlar/09-uygulama-kayitlari/EKRAN-DENETIM-KULLANICI-GOZU-2026-10-10.md`. Ekran görüntüleri **ana checkout'ta**: `/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/ekran-denetim/` (27 dosya; aşağıda `[nn]` = dosya adının başındaki numara).

**Neden:** Kurucu 10.10'da Yıldız temasında Tezgâh'ı kullandı: *"sağ tarafta ek ödeme indirim gibi kısımları kullanırken neyin ne olduğunu anlamadım … scroll vs çıkmadığından aşağıda kaldığından nasıl haberi olur"*.
**Yöntem:** Başsız Chrome (CDP 9751, tek profil), API 5230, kendi prova kiracısı `PAKET-EKRAN-DENETIM` (Yıldız şeması, tohumlu: 6 müşteri, 12 ürün, 9 satış). Her ekranda asıl iş bir tezgâhtar/dükkân sahibi gibi **gerçekten yapıldı** (satış kaydedildi, emanet alındı/iade edildi, satın alma→onay→mal kabul, sayım, tahsilat, sipariş, ürün ekleme…) ve her adımda `getBoundingClientRect / scrollHeight / clientHeight` ile görünürlük ölçüldü. Ayrıca bütün menü ekranları (45 rota) otomatik ölçerle tarandı: **gizli iç kaydırma kutusu**, **ekran altında kalan eylem**, **kesik metin**, **nedensiz pasif düğme**, **etiketsiz alan**, **yatay gizli sütun**.

**Kapsam (ölçülen kombinasyonlar):**

| Tema · mod | Genişlik × yükseklik | Ne yapıldı |
|---|---|---|
| Yıldız koyu · BASİT | 1024×768, 1280×800, 1366×768, 1440×900, 375×812 | 45 rota statik tarama + 17 "aç" eylemi taraması (1366) + elle iş akışları |
| Yıldız koyu · PRO | 1366×768, 375×812 | 45 rota tarama + Tezgâh işlem türleri (Alış) |
| Yıldız açık · BASİT | 1366×768, 375×812 | 45 rota tarama + Giriş |
| Platin açık · BASİT | 1366×768, 375×812 | 45 rota tarama + Tezgâh (para üstü) karşılaştırması |
| Tezgâh ek | 1440×900, 1920×1080 | para üstü görünürlük ölçümü |

**Dürüstlük notları (Madde 1):** Gün sonu "Günü Kapat" **basılmadı** (gün kilidi sonraki satış testlerini engellerdi; akış fark gerekçesine kadar yürütüldü). Platin PRO ayrıca taranmadı (Platin BASİT + Yıldız PRO ile yetinildi). Mevzuat yorumu içeren iki bulgu (Y15, Y16) "yanlış" diye değil **"teyit edilmeli"** diye işaretlendi. Kiracı iş bitince `kiraci-temizle.sh --uygula` ile silindi ("Kiracı YOK" doğrulandı); 5230 API'ye, 5199/5200/5241'e ve şeritlere dokunulmadı.

---

## Özet tablo — ekran × ciddiyet

| Ekran | KRİTİK | YÜKSEK | ORTA | DÜŞÜK |
|---|---:|---:|---:|---:|
| **Tezgâh (satış)** | 4 | 5 | 11 | 1 |
| Sayım | 1 | 1 | – | – |
| Ürünler (ürün ekle) | 1 | 1 | 1 | – |
| Müşteriler | – | 2 | – | – |
| Kasalar / Panel / Raporlar | – | 2 | 2 | – |
| Gider ve virman | – | 1 | – | – |
| Sipariş ve tamir | – | 1 | – | – |
| Yetkiler | – | 1 | – | – |
| Giriş | – | 1 | – | 1 |
| Yasal sınırlar | – | 1 | – | – |
| Faturalar | – | 1 | – | 1 |
| Emanet | – | 1 | 1 | – |
| Satın alma / İrsaliye | – | (Y17'de) | 1 | – |
| Eski tasarımda kalan ekranlar (çoklu) | – | 1 | – | – |
| Etiketler, Kur, Kılavuz, Kurulum, Veri aktar, başlıklar, genel dil | – | – | 6 | 2 |
| **Toplam** | **6** | **19** | **22** | **4** |

Temiz çıkanlar (asıl iş sorunsuz, ölçer bulgusu yok/önemsiz): Bildirimler, Kasa kuyruğu, POS cihazları, POS alacakları, Banka, Çek-senet, Vade takvimi, Hurda, Gider pusulaları, Fiyat listeleri, Ürün ağacı, Kasa raporu, Kullanıcılar, Yedekler, Vitrin, Tüm ekranlar. **Uyum (MASAK)** 256.244 ₺'lik kimlikli satışı doğru listeledi; **Emanet** al/iade, **tahsilat**, **satın alma→onay→mal kabul→irsaliye** uçtan uca çalıştı.

## Kurucunun ekran görüntüsünün karşılığı (tezgah-odeme-2026-10-10.webp)

| Kurucunun gördüğü | Yeniden üretildi mi | Bulgu |
|---|---|---|
| Sağ ödeme bölümünde neyin ne olduğu anlaşılmıyor, aşağıda kalan görünmüyor | Evet — 1280×800 ve 1366×768'de ölçüldü | K1, K2 |
| Tek ödemede tutar 0, FARK tam tutar, "Açık hesap olarak kaydet" birincil gibi | Evet — tek tıkla 11.099,68 ₺ müşteriye **borç yazıldı** | K3 |
| "+ Para üstü" soluk | Evet — kart/boş sepette soluk, nedeni yazmıyor | Y1, O8 |
| Büyük toplam ve Kaydet aşağıda | Evet — işlem türü açılınca Kaydet ekran dışı; telefonda ödeme bölümü görünmüyor | Y4, Y5 |
| Tutar "0 ₺" sağdan kesik | Kurucunun görüntüsü pencerenin sağından kırpılmış (yer imi çubuğu da kesik); bizim ölçümde tutar kesilmedi. Ama tutar girişi **kenarsız** — yazılabilir olduğu belli değil | O2 |

---

## İlk 10 — en kritik

1. **K3 · Tezgâh — tek tıkla, onaysız veresiye.** Ödeme 0 ₺ kalınca görünen tek eylem "Açık hesap olarak kaydet"; basınca sorusuz kaydediyor, tüm tutar müşteriye borç. Çözüm düğmesi "Kalanı ata" görünmez kutuda.
2. **K1 · Tezgâh — ödeme düzenleyicileri görünmez kaydırma kutusunda.** Ödeme türü listesi, "Tamam", açıklamalar, 3. ve sonraki ödeme satırları 47–198 px'lik `overflow:auto` kutuda; Mac'te kaydırma çubuğu yok. **Ödeme türü değiştirilemiyor** (seçenekler hiç görünmüyor).
3. **K2 · Tezgâh — para üstü sonucu görünmüyor** (1366×768'de 455,30 ₺ hesaplanıyor, ekranda yok).
4. **K4 · Tezgâh — 200.000 ₺ üstü satışta gizli zorunlu alan.** "Beyan açıklaması" zorunlu ama işareti yok; Kaydet nedenini söylemeden kilitli; "Tamam" düğmesi kod adında "iptal".
5. **K5 · Sayım — barkod okuyucuyla sayılmıyor.** Okut+Enter → "ADET (İSTEĞE BAĞLI)" alanı "Bu alan zorunludur" hatası.
6. **K6 · Ürün ekle — ayar varsayılanı 18.** "22 Ayar Halka Küpe" 18 ayar (750) fiyatıyla kaydedildi (17.657 ₺) → eksik fiyatla satış.
7. **Y1 · Tezgâh — nakit sessizce Karta döndü** (30.000 ₺ aşınca); açıklaması görünmez kutuda.
8. **Y3 · Tezgâh — indirim tutarı hiçbir yerde görünmüyor;** kalem 11.099,68, toplam 10.544,70; düğme "İndirim", alan "Fiş iskontosu (%)".
9. **Y4 · Tezgâh — işlem türü (Alış, Tahsilat, İade, Takas) gizli;** küçük "SATIŞ" yazısı düğmeymiş; açılınca Kaydet ekran dışı, Alış formu bambaşka tasarım.
10. **Y7 · Borç/alacak dili ters.** Tezgâh "Anadolu · alacağı 3.840 ₺" ↔ Müşteriler "biz borçluyuz"; Müşteriler başlığı "3 ALACAK" ise tam ters anlamda.

---

## Bulgular

Biçim: **ekran · tema/mod/genişlik · adım** → ne oldu · neden şaşırtır · görüntü · önerilen çözüm.

### KRİTİK

**K1 · Tezgâh · Yıldız koyu+açık, BASİT+PRO, 1280×800 / 1366×768 (1024×768 dahil) · ödeme bölümünde herhangi bir düzenleyici açınca**
- Ne oldu: Sağ sütun ekran yüksekliğine sabit (`.yz-tezgah-ana` overflow:hidden, h=728); FARK+Kaydet alta çivili, aradaki `#yildiz-tezgah-odeme-duzen` **198 px** (bir düzenleyici açıkken **96 px**, tek ödeme satırında **47 px**) yükseklikte `overflow:auto`. Ölçüm: içerik 554–702 px, kutu 198 px. Kart satırına tıklayınca açılan ödeme türü listesi (Nakit/Kart/Havale/Veresiye/Döviz/Eski altın) y=367–683'te çiziliyor ama kutu 367'de bitiyor → **hiçbir seçenek görünmüyor**, yalnız "Listede ara" kutusu. "+ Başka ödeme"ye iki kez basınca 3. ve 4. satır (Havale, ikinci Nakit) y=723/797'de, görünürde **hiçbir şey değişmiyor**. Tek ödeme modunda "Tutar 30.000 ₺ nakit sınırını aştığı için ödeme Kart olarak seçildi…" açıklaması, "KASA" ve "Tamam" da kutunun dışında.
- Neden şaşırtır: Mac/telefonda kaydırma çubuğu yok; kullanıcı tıkladığına bir şey olmadığını sanır, ödeme türünü değiştiremez. Kurucunun şikâyetinin birebir karşılığı.
- Görüntü: [01] seçici kırpık · [02] 4 satır eklendi, görünen 2 · [03] kutu içi kaydırınca gizli satırlar · [07] açıklama+Tamam gizli · [08] tek ödemede tür listesi görünmüyor
- Çözüm: Sağ sütunu doğal akışa bırakıp FARK+Kaydet'i yapışkan alt çubuk yap; ödeme türü seçimini satır içi görünür seçenekler (Nakit · Kart · Havale · Veresiye) ya da üstte açılan katman olarak çiz.

**K2 · Tezgâh · Yıldız koyu, 1366×768 (1280×800, 1024×768) · ürün ekle → + Para üstü → alınan 11000**
- Ne oldu: "PARA ÜSTÜ 455,30 ₺" DOM'da hesaplanıyor, `#yildiz-tezgah-para-ustu-alan` kutusu 20 px → ekranda yok; "ALINAN" etiketi de kayboluyor, alttaki "FİŞ İSKONTOSU" etiketi yarıdan kesik. 1440×900 ve 1920×1080'de tek düzenleyici açıkken görünüyor (ölçüldü); iki düzenleyici (indirim+para üstü) açıkken sıkışıyor.
- Neden şaşırtır: Kasiyerin müşteriye vereceği tek rakam görünmüyor.
- Görüntü: [10]
- Çözüm: Para üstü sonucunu FARK bloğunun yanına/altına (yapışkan alt çubuğa) taşı; Platin'deki "Alınan · Para üstü" yan yana düzeni örnek al.

**K3 · Tezgâh · Yıldız koyu 1366×768 · müşteri Selin → çeyrek → + Başka ödeme → ilk satırı Çıkar**
- Ne oldu: Tek satır "Kart · Ana Kasa 0 ₺", FARK **11.099,68 ₺**, "Ödeme tutarını girin", altında çerçeveli **"Açık hesap olarak kaydet"**. Tutar girişi kenarsız; düzeltme düğmesi **"Kalanı ata"** 47 px'lik kutuda gizli. "Açık hesap olarak kaydet"e basınca **onay sormadan** kaydetti; Müşteriler'de "Selin Ergüven · borcu 11.099,68 ₺" doğrulandı. Önceki satışın "Son fiş: … 256.244,17 ₺" satırı da bu satışa aitmiş gibi duruyordu.
- Neden şaşırtır: Kaydet griyken görünen tek "kaydet" bu düğme; tezgâhtar parayı almış olsa bile satış borç olarak işlenir — para hatası.
- Görüntü: [14]
- Çözüm: Tek ödemede tutarı otomatik toplamla doldur ("Kalanı ata" görünür); "Açık hesap" yalnız seçilmiş müşteride, ikincil ve "11.099,68 ₺ Selin'in hesabına borç yazılsın mı?" onayıyla.

**K4 · Tezgâh · Yıldız koyu 1366×768 · 3 kalem = 256.244 ₺ → müşteri Kemal → + Kimlik bilgisi**
- Ne oldu: Peşin müşteride doğru uyarı çıktı ("200.000 ₺ ve üstünde kimlik gerekir"). Kimlik çekmecesinde TCKN + işlem amacı dolduruldu, "Tamam" → Kaydet **hâlâ kilitli**, mesaj yalnız "Kaydetmek için kimlik bilgilerini doldurun." Hangi alanın eksik olduğu yazmıyor; alanlarda `*` yok. Gizli zorunlu alan "BEYAN AÇIKLAMASI" çıktı; doldurunca mesaj "Bu işlem yasal bir kapıya takıldı; gereken alanlar açıldı." oldu ve Kaydet açıldı. Çekmecenin tek düğmesi "Tamam"ın kimliği `cekmece-yildiz-kimlik-iptal`. Satış sonunda kaydedildi, Uyum ekranına doğru düştü.
- Neden şaşırtır: Tezgâhtar yüksek tutarlı satışta takılır, müşteri bekler.
- Görüntü: [25]
- Çözüm: Zorunlu alanlara `*` koy, Kaydet'in yanında "Eksik: beyan açıklaması" gibi alan adı yaz; "Tamam"ı kaydeden birincil düğme yap, "yasal kapı" dilini kaldır.

**K5 · Sayım · Yıldız koyu 1366×768 · Sayım başlat → ad ver → Oturumu Aç → barkod 8690000000074 + Enter**
- Ne oldu: "Form eksik ya da hatalı" bandı + "ADET (İSTEĞE BAĞLI)" alanının altında **"Bu alan zorunludur."** Sayılmadı (0/13). Adet 1 yazıp "Say"a basınca sayıldı.
- Neden şaşırtır: Barkod okuyucu her okumada Enter gönderir; sayım tabanca ile yapılamaz, alan "isteğe bağlı" derken hata verir.
- Görüntü: [18]
- Çözüm: Adet boşsa 1 say; okut+Enter tek hamlede saysın, odak barkod kutusuna dönsün.

**K6 · Ürünler · Yıldız koyu 1366×768 · Yeni ürün → ad "22 Ayar Halka Küpe", kod, 3,40 g, stok 4 → Kaydet**
- Ne oldu: Ürün grubu "Ziynet" varsayılanında AYAR alanı **"18 Ayar"** dolu geliyor. Kaydedilen ürün listede ayar **750**, fiyat **17.657 ₺** (18 ayar fiyatı).
- Neden şaşırtır: Kullanıcı ayarı değiştirmeyi unutursa ürün eksik fiyatla satılır.
- Görüntü: [16] (form)
- Çözüm: Ayar varsayılanı boş olsun (seçmeden kaydetme yok) ya da gruptan gelsin; ad ile ayar çelişirse uyar.

### YÜKSEK

**Y1 · Tezgâh · Yıldız koyu 1280×800 · müşteri seç → 96.408 ₺ bilezik ekle** — Ödeme "Nakit"ten **sessizce "Kart · Ana Kasa"**ya döndü; nedeni yalnız gizli kutuda (K1). "+ Para üstü" soluk, nedeni yok. [06] · Çözüm: Değişikliği ödeme satırının altında tek satırla söyle ("30.000 ₺ üstü nakit alınamaz — Kart seçildi").

**Y2 · Tezgâh · Yıldız koyu 1280×800 · Değiştir → müşteri seç** — Müşteri seçince liste **kapanmıyor** ("Kapat" gerekiyor); liste 287 px iç kutu, 3. satır ve "Seç" düğmesi yarıdan kesik; ardından eklenen kalem y≈630'da yarıdan kesik görünüyor. [04] [05] [06] · Çözüm: Seçimle listeyi kapat; listeyi iç kutu yerine üstte açılan katman yap.

**Y3 · Tezgâh · Yıldız koyu 1366×768 · + İndirim → %5** — Toplam 11.099,68 → 10.544,70 oldu ama **indirim tutarı (554,98 ₺) hiçbir yerde görünmüyor** (açıklama satırı 64 px kutuda gizli); kalem satırı 11.099,68 kalıyor. Düğme "İndirim", alan "FİŞ İSKONTOSU (%)"; yalnız yüzde (TL indirim yok); alan kenarsız. [09] · Çözüm: Toplamın üstünde "İndirim −554,98 ₺" satırı; terim tek ("İndirim"), % / ₺ seçimi.

**Y4 · Tezgâh · Yıldız koyu PRO 1366×768 · işlem türü** — Alış/Hurda alış/Tahsilat/Takas/İadeler küçük harf aralıklı **"SATIŞ"** yazısının arkasında (`#yildiz-tezgah-tur`, 12 px, alt çizgisiz, yalnız `title`). Açılınca sekmeler + "Diğer ▾ ürün ayır · kapora · konsinye · siparişten satış · çek/senet devri" kutusu ekleniyor, **Kaydet y=809'a (ekran dışı)** iniyor. "Alış" sekmesi bambaşka eski tasarımda form açıyor (gri özet bandı, "Cari seçin...", "Alış Kaydet" ekran altında). [12] [21] · Çözüm: İşlem türünü başlık satırında görünür seçici yap; Alış/İade formlarını Satış terazisiyle aynı düzende çiz.

**Y5 · Tezgâh · Yıldız koyu 375×812 / Platin açık 375×812** — Yıldız telefonda yapışkan alt çubuk (FARK + Kaydet) ekranın ~%27'sini kaplıyor; ödeme türü, para üstü, indirim **kaydırmadan görünmüyor** — kullanıcı "Nakit" mi "Kart" mı olduğunu görmeden kaydeder (kaydırınca görünür ve okunur). Platin telefonda tersi: "Kaydet ve yazdır" y=1032'de ekran dışı. [11] [26] · Çözüm: Telefonda alt çubuğa ödeme türünü tek satır ekle ("Nakit · 10.544,70"); Platin'e yapışkan Kaydet.
*Not:* Platin 1366×768 Tezgâh aynı işi sorunsuz yapıyor (görünür ödeme seçici, "Alınan · Para üstü 400,32 ₺" yan yana, işlem türleri sağda düğme, Kaydet görünür) — Yıldız düzeltmesi için iyi örnek.

**Y6 · Müşteriler · Yıldız koyu 1366×768 · müşteri satırına dokun** — Kart açılınca sayfa yeniden diziliyor: "7 MÜŞTERİ · 3 ALACAK · 1 BORÇ" dört satıra kırılıyor, arama kutusu büyük "Müşteriler" başlığının **üstüne biniyor**. Kart borcu değil **düzenleme formunu** (Ad, Telefon, Not) gösteriyor; 6 eylem ("Tahsilat, Detay, Veresiye, Düzeltme Fişi, Döviz Tahsilat, Döviz Veresiye") sekme gibi görünüyor, hiçbiri seçili değil. [15] · Çözüm: Kart açılınca liste daralmasın (katman üstte); kart borç + "Tahsilat al" birincil ile açılsın.

**Y7 · Tezgâh ↔ Müşteriler ↔ Panel · dil** — Aynı tedarikçi: Tezgâh "alacağı 3.840 ₺", Müşteriler "−56.340 ₺ biz borçluyuz", Panel "borcun 56.340 ₺". Müşteriler başlığı "3 ALACAK · 1 BORÇ" — buradaki "alacak" Tezgâh'takinin **tersi**. · Çözüm: Her yerde iki sabit kalıp: "bize borçlu X ₺" / "biz borçluyuz X ₺".

**Y8 · Kasalar / Panel / Raporlar · Yıldız koyu 1366** — Kasalar büyük rakam "Ana Kasa 312.999,49 ₺" kart (22.846) ve havaleyi (96.408) içeriyor; çekmecedeki nakit 193.744. Tezgâh'ta Kart ve Havale de "· Ana Kasa" yazıyor. Panel'deki en büyük rakam (463.034,40 ₺) **etiketsiz** (bugünkü satış olduğu ancak Raporlar'dan anlaşılıyor). · Çözüm: Büyük rakam "Çekmecedeki nakit" olsun, kart/havale ayrı; Panel rakamına "Bugünkü satış" etiketi.

**Y9 · Gider ve virman · Yıldız koyu 1024–1440 / 375** — Yeni kiracıda kategori yok; Kaydet pasif, nedeni yalnız `title`'da; "Önce bir kategori açın" yazıyor ama **nerede açılacağı yok** (kategoriler "Ayrıntı ekle" içinde). Kaydet 1366×768'de y=867 (ekran dışı). Başlıkta jargon "virman". [19] · Çözüm: "Kategori" alanına "+ Yeni kategori" (kira, elektrik, personel hazır öneri); Kaydet görünür alanda.

**Y10 · Sayım · Yıldız koyu 1366×768** — Sayım ekranında **kenar boşluğu sıfır**: üst etiket ve "Sayım" başlığı x=0'da, "Sayıma dön" sağdan kesik. "Sayım başlat" doğrudan saymıyor; "Oturum" adı zorunlu, altında uzun "Bu kayıt değiştirilemez" metni; tablo sütunları "SİSTEM / GÜNCEL / OTURUMDA HAREKET VAR". [17] · Çözüm: Diğer Yıldız ekranlarıyla aynı kenar; "Sayım başlat" → adı tarihle otomatik ver, doğrudan okutma ekranına geç.

**Y11 · Yetkiler · tüm genişlikler** — 25 sütunluk matris; 1366'da 1238/2140 px görünür, KURULUŞ, POS, RAPORLAR, SAYIM… **sağda görünmez**, kaydırma işareti yok. Sütun adları menüyle farklı ("Cariler", "İşlemler" ↔ "Müşteriler", "Tezgâh"); Sahip satırında 13 kez "Tam yetkili". Kaydet pasif, gerekçe "Eksik olan: değiştirilmiş bir yetki yok". [22] · Çözüm: Rol başına dikey liste (ekran adı = menü adı), aç/kapa anahtarı.

**Y12 · Sipariş ve tamir · Yıldız koyu 1366×768 · Yeni iş** — Yeni sipariş formu liste süzgeçlerinin ("Tüm durumlar / Yalnız gecikenler / Yenile") **altında**; "Siparişi oluştur" y=904 ekran dışı. **Kapora alanı yok** (kapora yalnız Tezgâh → gizli "SATIŞ" → Diğer içinde). Tamir işi de "Müşteri siparişi" türüyle açılıyor. [24] · Çözüm: Yeni sipariş çekmecede açılsın; "Kapora al" sipariş formunda görünür satır.

**Y13 · Giriş · Yıldız açık 375×812** — Parola alanının yer tutucusu **"••••••••"**: alan boşken (value.length=0) dolu görünüyor. "YENİ DÜKKÂN KUR" bağlantı olduğu belli değil. [20] · Çözüm: Yer tutucuyu kaldır ya da "Parolanız" yaz.

**Y14 · Kâr-zarar / Panel · Yıldız koyu 1366** — "Ne kazandım?" ekranında MALİYET, KÂR, DÖNEM KÂRI **"Hesaplanamadı"** ("Satılan malların alış maliyeti girilmemiş"); satın alma ile mal kabul yapıldıktan sonra da. Panel "KÂR maliyet eksik". [23] · Çözüm: Maliyeti bilinmeyen kalemleri ayırıp kalan kârı göster; eksik maliyeti tamamlamak için doğrudan bağlantı ver.

**Y15 · Yasal sınırlar · teyit edilmeli** — İki satır çelişkili görünüyor: "Bu tutar ve üstünde müşterinin kimliği alınır **370.000 ₺**" ve "Bu tutar ve üstünde işlemin amacı yazılır ve kimlik alınır **200.000 ₺**" (Tezgâh 200.000'de kimlik istedi). İki farklı tarihe (7 ve 8 Ekim 2026) **aynı RG sayısı 33393** yazılmış. · Çözüm: Mevzuat ekibi kaynakları teyit etsin; kullanıcıya tek kural cümlesi.

**Y16 · Faturalar · Yeni fatura · teyit edilmeli** — KDV oranı "Fatura ayrıntıları: Satış · Standart · %20" katlanır satırda gizli; "TOPLAM (KDV DAHİL)" girilince %20 toplamdan ayrılıyor gibi. Tezgâh Alış formu ise "KDV (yalnız işçilik+kâr)" diyor. [27] · Çözüm: Mali müşavirle teyit; KDV matrahı ve oranı formda açık yazsın.

**Y17 · Eski tasarımda kalan ekranlar (Yıldız koyu, tüm genişlikler)** — Satın alma, İrsaliyeler, Faturalar, Uyum, Yasal sınırlar, Raporlar alt ekranları (Kâr-zarar, Vade takibi, İşlem defteri, Satış analizi), Kullanıcılar, İşletme, Yetkiler, Kim ne yaptı, Kurulum, Veri aktar, Yedekler, Ayarlar, Kılavuz: başlık "Ad SS:DD · Yardım" küçük satır, çekmece perdesi lacivert, eski form öğeleri — Yıldız ekranlarındaki büyük başlık + sade düzenle tutarsız. Satın alma ve İrsaliye için tuval **yok** (`ham/yildiz-tuval/kaynak/` içinde Y_Satinalma / Y_Irsaliye bulunmuyor). [13] · Çözüm: Bu ekranlara Yıldız sayfa başlığı ve perde rengini ortak bileşenden ver; Satın alma/İrsaliye için tuval çizilsin.

**Y18 · Emanet · Yıldız koyu 1366 · al 12,50 g 22 ayar → iade** — Ekran "Elimizdeki emanet **11,4500 has**" gösteriyor; iade formu **"İADE EDİLECEK HAS GRAM"** istiyor. · Neden: Müşteri "12,5 gram bilezik bıraktım" der; "has" dükkân içi hesap dilidir. · Çözüm: Emaneti bırakıldığı gibi göster ("22 ayar · 12,50 g"), has yalnız ikincil.

**Y19 · Ürünler · Yeni ürün · Yıldız koyu 1366×768** — Modal üst menünün üstüne biniyor; GRAM ve AÇILIŞ STOĞU modal içi kaydırmada; boş Kaydet'te **gram hatası görünmeyen bölgede** kalıyor (yalnız genel bant görünüyor). Arkadaki sayfada ikinci dolu "Yeni ürün" düğmesi. [16] · Çözüm: Zorunlu alanlar (ad, ayar, gram) ilk ekranda; hata olunca ilk hatalı alana kaydır.

### ORTA

- **O1 · Tezgâh** — Açık ödeme seçicisini Escape kapatmıyor; 4. "+ Başka ödeme" ikinci bir "Nakit · Ana Kasa" satırı açıyor. · Çözüm: Escape kapatsın; aynı tür ikinci kez eklenmesin.
- **O2 · Tezgâh** — Ödeme tutarı girişleri kenarsız (yazılabilir olduğu belli değil); "ÖDEME ARACI" ve "TUTAR" etiketleri görsel olarak gizli (1 px kırpık); biçim tutarsız: "0 ₺" / "0,00 ₺" / "11.099,68 ₺". · Çözüm: Alt çizgi + tek biçim (0,00 ₺).
- **O3 · Tezgâh** — "Bu müşterinin bugünkü kalan nakit hakkı: 30.000 ₺" ile alttaki "nakit 30.000 ₺ sınırının altında" aynı bilgi iki kez; ilki satıra yapışık. · Çözüm: Tek yerde.
- **O4 · Tezgâh** — Kalem satırındaki büyük "22" (ayar) adet/sıra no sanılıyor; "2 adet · 15,20 g" birim gram mı toplam mı belirsiz (alt toplam 40,40 g). · Çözüm: "22 ayar" yaz; "2 × 15,20 g".
- **O5 · Tezgâh** — Kayıt sonrası tek geri bildirim soluk "Son fiş: Satış · 22:19 · 96.408,70 ₺"; yeni satışa başlanınca da duruyor. "Kaydet ve yazdır" sonrası "Fiş yazdır" — yazdı mı belli değil. · Çözüm: Kısa başarı bandı ("Satış kaydedildi · fiş yazdırıldı"); yeni kalem eklenince "Son fiş" gizlensin.
- **O6 · Tezgâh** — Kayıt sonrası içi boş "ÖDEME ŞEKLİ" düzenleyicisi açık kalıyor, "Ş" çengeli kırpık; aynı şeye üç ad: "Ödeme türü", "Ödeme şekli", "Ödeme aracı". · Çözüm: Kayıtta düzenleyicileri kapat; tek ad.
- **O7 · Tezgâh** — İndirim kutusundan Tab ile odak doğrudan "Vazgeç"e gidiyor (Enter = sepeti sil riski). · Çözüm: Odak sırası Kaydet'e.
- **O8 · Tezgâh** — "+ Başka ödeme / + Para üstü / + İndirim" ve Kaydet boş sepette soluk; neden yazmıyor. · Çözüm: "Önce ürün ekleyin" tek satır.
- **O9 · Tezgâh** — Müşteri listesi iç kutusu 287 px (Y2 ile birlikte çözülür).
- **O10 · Tezgâh** — "Bu işlem yasal bir kapıya takıldı; gereken alanlar açıldı", "Ürünü seçip fiyatı elle yaz" gibi iç dil. · Çözüm: Düz Türkçe ("Kimlik bilgisi gerekli").
- **O11 · Emanet** — Müşteri seçilmeden "Emanet al" dolu gri, "Emanet iade et" çizgili; ikisi de pasif ama farklı görünüyor; sekme mi düğme mi belli değil. · Çözüm: İki eşit düğme, pasif nedeni altta (var: "Önce müşteri seçin").
- **O12 · Satın alma / İrsaliye** — Tedarikçi listesi 2 harf yazmadan boş; perakende müşteriler (Tuna Akarsu) de tedarikçi olarak çıkıyor; "Aramak için en az 2 karakter yazın." sonuç gelince de duruyor; "Taslak → Onayla" ek adımı; mal kabul mesajı stok +5 ve tedarikçi borcu 52.500 ₺'yi söylemiyor; irsaliye "Faturası bekliyor" — ne yapılacağı yazmıyor; başlıkta "Kuru güncelle — 22:42" düğme. · Çözüm: Tedarikçi listesi açılışta dolu ve yalnız toptancılar; mal kabul sonucu "Stok +5 · Anadolu'ya borç 52.500 ₺".
- **O13 · Ürünler** — Liste AYAR sütunu 916/750 (milyem), form "22 Ayar"/"18 Ayar". · Çözüm: Listede de "22 / 18".
- **O14 · Kasalar · Gün sonu** — Sayılan nakit yazılırken binlik ayıracı yok (193700); "Defter bakiyesi" jargon; çekmece perdesi lacivert. Akış anlaşılır: kart/havale "sayıma girmez", fark "44 ₺ eksik", gerekçe zorunlu.
- **O15 · Etiketler** — "Yazdır" 1024–1366'da y≈841, 375'te y=834 ekran dışı.
- **O16 · Başlıklar** — Yıldız sayfa başlığı 64 px, ama Sayım, Kasalar, Kur, Hurda, Kasa raporu 40 px.
- **O17 · Raporlar / Satış analizi** — Satış analizi açıklaması "Sevk edilmiş raporların tam listesi — kartına tıklayın." (yanlış metin); ödeme aracı kırılımında "DİĞER 150.000 ₺" (açılış devri satış gibi); rapor tarih alanları "(İSTEĞE BAĞLI)"; Kasa raporu "AÇILIŞ 0,00 ₺" (kiracı 150.000 açılışla kuruldu).
- **O18 · Kurulum / Veri aktar** — Kurulum'da adsız pasif düğme, etiketsiz iki gerekçe alanı (`#kur-gerekce`, `#kur-muhur-gerekce`); "Hazırla ve İndir", "Önce kontrol et" pasif, nedeni ayrı satırda.
- **O19 · Kılavuz** — Ekran listesi 307 px iç kutuda (1366), 195 px (375); çoğu başlık görünmüyor.
- **O20 · Kur** — "22 ayar gram" alış "—", "GÜN FARKI —".
- **O21 · Genel dil** — virman, ramat, konsinye, çek/senet devri, altın pozisyonu, Düzeltme Fişi, Döviz Veresiye, Oturum, "sahte fark" (POS). · Çözüm: BASİT modda düz karşılıklar.
- **O22 · Platin Tezgâh 1366/375** — Arama ipucu "Ürünü arayın ya da barkodunu okutun." kırpık.

### DÜŞÜK

- **D1 · Faturalar** — "Fişten Fatura Kes" (her kelime büyük) ↔ "+ Yeni fatura"; ikisinin farkı yazmıyor.
- **D2 · Giriş** — Giriş ekranında "Bu kurulumda HARİÇ olanlar (8)"; Platin girişte "3 tık nakit satış / 30 dk öğrenme" tanıtım metni.
- **D3 · Tezgâh 1024×768** — Üst çubukta saat düşüyor; ürün adı ve "1,75 g · 1,6030 g has" iki satıra kırılıyor; sol sütun 436 px.
- **D4 · Tüm ekranlar** — Platin menüsü "Tüm ekranlar (41)", Yıldız /ekranlar 44 kare.

---

## Paketleme önerisi (Tamer Bey için)

1. **P-TEZGAH-ODEME (K1, K2, K3, Y1, Y3, O1–O3, O5–O8):** sağ sütunu yeniden düzenle (doğal akış + yapışkan FARK/Kaydet), görünür ödeme türü seçimi, tek ödemede otomatik tutar, açık hesap onayı, indirim satırı, para üstü alt çubukta. Platin Tezgâh düzeni referans.
2. **P-TEZGAH-TUR-KIMLIK (K4, Y4, Y2, O10):** görünür işlem türü seçici, Alış/İade formlarını Yıldız terazisine taşı, kimlik çekmecesinde zorunlu işaret + birincil Tamam, müşteri seçici katman.
3. **P-SAYIM (K5, Y10):** okut+Enter=1, kenar boşluğu, otomatik oturum adı.
4. **P-URUN (K6, Y19, O13):** ayar varsayılanı, zorunlular ilk ekranda, hata kaydırma.
5. **P-DIL-BORC-KASA (Y7, Y8, Y18, O21):** borç/alacak tek kalıp, kasa = çekmecedeki nakit, emanet gram/ayar dili.
6. **P-FORM-GORUNURLUK (Y5, Y9, Y11, Y12, O15):** ekran dışı Kaydet'ler, gider kategorisi, sipariş kaporası, yetki matrisi, telefon alt çubuğu.
7. **P-YILDIZ-KALAN (Y17, O16):** eski başlıklı ekranlara Yıldız başlık/perde; Satın alma/İrsaliye tuvali.
8. **Teyit (mevzuat):** Y15, Y16 — mali müşavir/hukuk.
9. **Küçükler:** Y13, Y14, O4, O9, O11, O12, O14, O17–O20, O22, D1–D4.

**Ölçerlere eklenmesi önerilen kural (bu turun dersi):** "Etkileşim sonrası görünürlük" — her açıcıya bas; açılan içeriğin en yakın `overflow:auto|hidden` atasının `clientHeight`'ı dışında kalan metin/düğme varsa KIRMIZI. Mevcut ölçerler (sikisma-olc, tuval-kiyas, yildiz-tipo-tara) yalnız açılış durumunu ölçtüğü için K1–K3'ü göremedi; bu turdaki statik taramada da açılış durumları neredeyse temiz çıktı — sorunların tamamı etkileşimde.
