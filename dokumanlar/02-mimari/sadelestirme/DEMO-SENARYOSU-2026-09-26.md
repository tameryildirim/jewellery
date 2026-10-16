# MİHENK — CUMARTESİ DEMO SENARYOSU (26 Eylül 2026, sabah)

> **Kime:** Kurucu (Ramazan Akkocan). Elinde tutup uygulayacağı belge.
> **Yazan:** Köprü (07 Teslimat) + Eray (08 Satış) + Aşkın (04 QA) merceği, Tamer Bey koordinasyonu.
> **Ürün adı:** **MİHENK** (kurucu kararı, 23 Eylül). "Kuyumcu ERP" adı ölüdür.
> **Firma:** ByGatech A.Ş.
> **Müşteri:** Bizi daha önce görmüş ve üç şey söylemiş kuyumcu: *"çok karışık abi — menüler karışık, ekranlar karışık, renkleri iyi değil; Pusula çok sade ve güzel."*
> **Bütçe:** 14 dakika 50 saniye ekran + söz. 15 dakikayı aşmaz.
>
> **ANAYASA MADDE 1 UYARISI — bu belgenin en önemli satırı:**
> Sadeleştirme planı (`PLAN-SADELESTIRME-PUSULA-2026-09-23.md` satır 6) bugün hâlâ
> **"PLAN — kod yazılmadı"** durumundadır. Cumartesi gösterilecek olan **bugünkü ürün**tür.
> Müşteriye "yeni sade arayüzü yaptık" **denmez**. Bugün dürüstçe gösterilebilecek sadelik
> şudur: **BASİT MOD** (gerçekten var, gerçekten çalışıyor) + **renk/yazı seçimi** (gerçekten var).
> Bunun dışındaki her sadelik sözü yalan olur.

---

## 0. ÖNCE ŞUNU OKU — demo öncesi 6 ZORUNLU iş

Bunlar yapılmadan demo yapılmaz. Hepsinin kanıtı bölüm 7'de.

| # | İş | Neden (kanıt) |
|---|---|---|
| Z1 | **Yayın anlık görüntüsünü yeniden üret**, `blazor.boot.json`'daki 66 varlığın her birini HTTP 200 doğrula | QA turu 8'de 2 dosya (`Microsoft.Extensions.Logging.Abstractions.wasm`, `System.ComponentModel.Annotations.wasm`) 404 verdi, **uygulama hiç açılmadı**, 6 rota hiç ölçülemedi. Aynı arıza demo sabahı olursa demo biter. (`ham/qa-turu-8.json`, grup 1) |
| Z2 | **Panel'deki ölü bağlantıyı düzelt** | `Pages/Panel.razor:273` → `href="/islem-defteri"`. Böyle bir sayfa **yok** (`@page "/raporlar/islem-defteri"`). Ölçüldü: uygulamadaki **tek** ölü iç bağlantı bu, ve **demonun 2. adımındaki ekranda**. Tıklanırsa "Sayfa bulunamadı" çizer. |
| Z3 | **Ekranda "Kuyumcu ERP" yazısını MİHENK yap** — en az `wwwroot/index.html` (başlık + "yükleniyor" metni) ve `App.razor` `<PageTitle>` | Bugün **43 dosyada** "Kuyumcu ERP" yazıyor, **0 dosyada** "Mihenk" (ölçüldü, 23 Eylül). Müşteri ürünün eski adını tarayıcı sekmesinde ve açılış ekranında görür. |
| Z4 | **Temiz demo kiracısı kur** (bölüm 3'teki listeyle) | QA kiracısında `QA-TEST G1 T8 Kasa DUZ`, `QA-TEST G2T8 Musteri DUZENLENDI` gibi artıklar var. Ekranda "QA-TEST" yazan tek satır demoyu laboratuvara çevirir. |
| Z5 | **BASİT MOD açık olsun**, profesyonel mod demoda hiç açılmasın | Profesyonel menüde **40 bağlantı** aynı anda açık duruyor (ölçüldü: `Layout/MenuTanimi.cs`, `new(MenuGrubu…` = 40; Günlük 5 · Müşteri-Stok 6 · Para-Banka 10 · Belge-Uyum 3 · Rapor 6 · Yönetim 10). Müşterinin 1. eleştirisi tam budur. |
| Z6 | **Renk şemasını önceden seç ve provayı o şemada koş** | Bugün kodda **3 şema** var: varsayılan · kurumsal · zümrüt (`Servisler/GorunumServisi.cs:31`, `belirtecler.css` `data-sema`). Plandaki 8 şema **henüz yazılmadı** — demoda "8 renk" sözü verilmez. |

---

## 1. EKRAN SIRASI — 12 adım, 14 dk 50 sn

**Sıranın gerekçesi (üç kaynağın kesişimi):**

1. **Pusula'nın kendi demosunun sırası** (`PUSULA-REFERANS-2026-09-23.md` § 6, 10:51'lik PusulaX Demo'nun kare şeridinden okundu): giriş → ana ekran → *tanımlamalar* → satış fişi → çıktı önizleme → barkodlama → fiyat sorgulama → raporlar → kapanış. Müşteri Pusula'yı beğendiğini söylediği için **tanıdık sırayla** ilerliyoruz. Tek sapma: Pusula'nın 1:40–3:00 arası **Tanımlamalar** bloğunu atlıyoruz — demonun en sıkıcı yeri orası, bizde veriler hazır geliyor.
2. **Kuyumcu dükkânının günü**: sabah kur → gün içi satış → stok/etiket → vitrin → akşam özet. Bu sıra bizim **basit menümüzün sırasına** birebir oturuyor (`Servisler/BasitMenu.cs`: `/` · `/islemler` · `/cariler` · `/urunler` · `/urunler/etiketler` · `/kasalar` · `/raporlar` · `/bildirimler`), yani demo yukarıdan aşağı akıyor, menüde gezinmiyoruz.
3. **Yazılım demosu yöntemi** (Great Demo! / Peter Cohan): kurumsal tanıtımla başlama, uzun doğrusal tur yapma, başta süreyi teyit et, **sonda özetle**. Bu yüzden adım 0 ve adım 11 ekransızdır ve zorunludur.

**Ekran sayısı uyarısı (dürüst kayıt):** yöntem araştırması 15 dakika için **en çok 7 ekran** türetti. Bu senaryoda **9 ayrı ekran** var. Gerekçe: kurucu "sık kullanılan tüm ekranları" göstermek istiyor ve 9 ekranın 3'ü aynı senaryonun parçası (satış iki kez aynı ekranda; etiket ürünün devamı; vitrin kurun devamı). **Sıkışırsan kesme sırası: önce adım 5 (/urunler), sonra adım 9 (/raporlar).** İkisi de senaryo kırmaz.

---

### ADIM 0 — Açılış sözü · 30 sn · ekran yok (masaüstü, program kapalı)

- **Kurucu ne yapar:** Bilgisayarı müşteriye döndürür, ekrana henüz bakmaz.
- **Ne söyler:** *"On beş dakikanı alacağım. Üç şey göstereceğim: bir satış nasıl yapılıyor, malın nerede olduğunu nasıl görüyorsun, akşam kasan nasıl kapanıyor. Geçen sefer üç şey söylemiştin — karışık menü, karışık satış ekranı, renkler. Üçüne de bugün cevap vereceğim."*
- **Neden bu sırada:** Great Demo!'nun iki kuralı: **başta süreyi teyit et** ve **kurumsal tanıtımla başlama**. Ayrıca müşterinin kendi cümlelerini geri vermek, "seni dinledim" demenin en ucuz yoludur.
- **Bugün hazır mı:** Evet — ekran yok.

---

### ADIM 1 — `/giris` · 40 sn

- **Kurucu ne yapar:** Programı açar. Giriş ekranında **durur**, hemen girmez. Sağdaki/alttaki **"bu kurulumda olmayanlar" listesini** parmakla gösterir. Sonra e-posta + şifreyi yazar, **Giriş Yap**'a basar. *(2 tık + yazı)*
- **Ne söyler:** *"Programı açtım. Şurada bir liste var: bu kurulumda **olmayan** şeyler. Ne olmadığını sana ben söylüyorum, sen sonradan bulmuyorsun."*
- **Müşteri ne görür:** Tek kart, iki kutu, tek düğme. Ve satıcının kendi eksiklerini önceden yazdığı bir liste.
- **Neden bu sırada:** Pusula demosu da 0:10'da giriş ekranıyla başlıyor. Ama biz buraya **güven hamlesi** koyuyoruz: kısıt beyanı sunucudan gelir (`Pages/Giris.razor` → `GET /genel/kisitlar`), ekrana elle yazılmaz. Rakipte böyle bir şey yok — ama bunu **söylemeyeceğiz** (bkz. bölüm 6, yasak cümleler).
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel 3 / kullanıcı dostu 3, **0 kritik**. Gerçek ekrandan giriş yapıldı, öykünme kullanılmadı. *Bilinen eksik: "şifremi unuttum" akışı yok (ORTA) — demoda konu açılmaz.*

---

### ADIM 2 — `/` Panel (sabah) · 90 sn

- **Kurucu ne yapar:** Hiçbir yere tıklamadan **sol menüyü parmakla gösterir** (8 satır), sonra sırayla üç kartı gösterir: **Günlük satış** → **Kur** → **Kasa Özeti**. Sonra durur. *(0 tık)*
- **Ne söyler:** *"Sol tarafta sekiz satır var, hepsi bu. Sabah açtığında gördüğün şey: bugün ne sattın, altının kuru kaç, kasanda ne var. Başka bir şeye bakmana gerek yok."*
- **Müşteri ne görür:** Solda 8 satırlık menü (40 değil). Ortada büyük rakamlar: günlük satış, has, kur, kasa.
- **Neden bu sırada:** Pusula demosu 0:20–1:30 arasını Ana Ekran'da geçiriyor ve **hiçbir menü dalı açmıyor**. Biz de ilk 2 dakikada menü açmıyoruz. **Müşterinin 1. eleştirisinin cevabı tam burada ve konuşarak değil, ekranda 8 satır durarak veriliyor.**
- **Bugün hazır mı:** **EVET — en yüksek puanlı ekranımız.** QA turu 8: görsel **4** / kullanıcı dostu **4**, **0 kritik**. Canlı doğrulandı: Günlük satış ₺48.200,00, net has −13,9232, Kur ₺6.760,53, Kasa Özeti, Bekleyen işler, Son işlemler, 7 günlük satış grafiği yüklendi.
- ⛔ **BU EKRANDA ASLA TIKLANMAYACAK 4 BAĞLANTI:**
  | Bağlantı | Neden |
  |---|---|
  | **"Son işlemler →"** (`panel-defter-git`) | **Ölü bağlantı** — `/islem-defteri` diye sayfa yok. Z2 yapılmadıysa "Sayfa bulunamadı" çizer. |
  | **"Kasa Özeti →"** (`panel-kasa-git`) | `/kasalar`'a gider; o rota QA turu 8'de **ENGELLENDİ**, hiç açılamadı. Durumu **bilinmiyor**. |
  | **"Hurda Alış"** düğmesi (`panel-hurda`) | `/hurda`: 1 kritik ("hurda alış satırının hiçbir eylemi yok") + 4 yüksek, biri **tema değişince yazı okunmaz oluyor (kontrast 1,17)**. |
  | **"Tahsilat"** düğmesi (`panel-tahsilat`) | `/vezne`: 0 kritik ama görsel 3 / dostu 3 ve fiş türü sonradan değiştirilemiyor. Senaryoda yeri yok. |
- ⚠ **Hava durumu kutucuğu:** Panel'de selamın yanında hava var (`panel-hava`, `Pages/Panel.razor:61`). Veri gelmezse sessizce yok. **Gelirse** ve müşteri takılırsa tek cümle: *"Onu kaldırabiliyoruz, ayardan kapanıyor."* Tartışma açma.
- ⚠ **"Açık pozisyonum nerede?" sorusu gelirse:** Kodda "Pozisyon" diye bir şey **yok** (arandı, 0 sonuç). Dürüst cevap: *"O kutucuk yolda. Şu an borcunu-alacağını müşteri kartından ve rapordan görüyorsun."* Uydurma.

---

### ADIM 3 — `/islemler` · SATIŞ 1: normal satış · 120 sn

- **Kurucu ne yapar:**
  1. Menüden **Satış (POS)**'a basar.
  2. Ürünü seçer (barkod okuyucu varsa **okutur**), **Ekle**'ye basar.
  3. Sepet satırında **adedi 1'den 2'ye** çıkarır (satır içi, ekranda), sonra 1'e döndürür.
  4. Müşteriyi seçer — **listeye gitmeden, satış ekranının içinden**.
  5. Kasayı seçer, ödeme **NAKİT**, **Kaydet**. *(toplam 5–6 tık)*
- **Ne söyler:** *"Malı okuttum, satır geldi. Adedi yanlış yazdıysam satırın üstünde düzeltiyorum, baştan girmiyorum. Müşteriyi buradan seçiyorum, ayrı ekrana gitmiyorum. Kaydet. Bitti — tek ekranda."*
- **Müşteri ne görür:** Tek ekran, tek liste, tek Kaydet. Basit modda **3 işlem türü** var (Satış · Alış · Satış İadesi — `Servisler/BasitPos.cs`), 8 sekme değil.
- **Neden bu sırada:** Pusula'nın klasik satış dersi **3 dk 53 sn** ve tek ekranda bitiyor; PusulaX fişi de tek gridde bitiyor (**3 alan + 13 düğme + FARK 0,00 → Kaydet**). Müşterinin 2. eleştirisinin cevabı burada. Ayrıca Great Demo!: **en değerli şeyi sona saklama** — satış demonun kalbi, 3. adımda geliyor.
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel **4** / kullanıcı dostu 3, **0 kritik**. Canlı doğrulandı: 22 Ayar Burma Bilezik 45.000 ₺ sepete eklendi, **satır içi adet düzenleme 1→2→1 çalıştı** (toplam ₺90.000'e, gram 15.200→30.400 g'a döndü), ikinci satış (925 Gümüş Kolye, 3.200 ₺) kaydedildi ve başarı bandı çıktı.
- ⚠ **TUTARI 30.000 ₺'NİN ALTINDA TUT.** Nakit ve eşik üstü olursa sunucu kaydı reddeder. Eşik sunucudan gelir; istemci varsayılanı 30.000 ₺ (`Pages/Islemler.razor:1523`). Demo satışı **3.000–15.000 ₺** aralığında olmalı.
- ⚠ **Başarı bandı ~5 saniyede kayboluyor ve fiş numarası hiçbir yerde kalmıyor** (QA turu 8, YÜKSEK bulgu). Müşteri fiş numarasını sorarsa: `/raporlar/islem-defteri`'ne gidip satırı göster. **Panel'deki "Son işlemler" bağlantısını kullanma** (ölü).

---

### ADIM 4 — `/islemler` · SATIŞ 2: kanun kapısı · 100 sn

- **Kurucu ne yapar:**
  1. Aynı ekranda yeni satış açar, **pahalı** bir kalem ekler (ör. 22 ayar bilezik, 45.000 ₺).
  2. Ödemeyi **NAKİT** bırakır ve **durur** — ekranda beliren uyarıyı parmakla gösterir.
  3. Ödemeyi **HAVALE**'ye çevirir, **Kaydet**. *(4–5 tık)*
- **Ne söyler:** *"Kırk beş bin liralık mal, nakit. Bak program ne diyor: bu tutarda nakit almak yasak, banka veya kart olacak. Ben unutsam da program unutmuyor. Havaleye çevirdim, kaydetti."*
- **Müşteri ne görür:** Kaydet'e basmadan önce çıkan Türkçe uyarı: *"Bu tutar tevsik haddini (30.000 ₺) aşıyor — nakit ödeme reddedilir. HAVALE/EFT veya kart seçin."* Sonra sorunsuz kayıt.
- **Neden bu sırada:** Bu, MİHENK'in en güçlü tek anı ve **satıştan hemen sonra** gelmeli — müşteri satışın nasıl yapıldığını daha yeni gördü, şimdi neden bu programı seçeceğini görüyor. Yöntem: **önce söyle → göster → ne anlama geldiğini söyle.**
- **Bugün hazır mı:** **EVET, ama tek şartla.** Uyarı metni kodda doğrulandı (`Kuyumcu.Core/I18n/Sozlukler.cs:4017`) ve **Kaydet'e basmadan önce** çıkıyor. QA turu 8 ayrıca kanıtladı: nakitle Kaydet denendi → sunucu 400 verdi (doğru davranış), HAVALE'ye geçilince 200.
  ⛔ **KAYDET'E NAKİTLE ASLA BASMA.** Basarsan ekranda kırmızı hata bandı çıkar; anlatı "program beni korudu"dan "program hata verdi"ye döner. Uyarıyı göster, ödemeyi değiştir, öyle kaydet.
- ⚠ Ekranda **"tevsik haddi"** jargonu yazıyor. Kurucu bunu sesli çevirir: *"tevsik dedikleri şu: 30 bin üstü nakit alınmaz."* Kelimeyi okuma, anlamını söyle.

---

### ADIM 5 — `/urunler` · 70 sn  *(sıkışırsan ilk kesilecek adım)*

- **Kurucu ne yapar:** Menüden **Ürünler**. Listeyi gösterir, bir ürünün **Stok** penceresini açar, kapatır. *(3 tık)*
- **Ne söyler:** *"Bütün mal burada. Her satırda ayarı, gramı ve **has karşılığı** yazıyor. Bir malın hangi şubede kaç tane olduğunu buradan görüyorsun."*
- **Müşteri ne görür:** Ayar · gram · has karşılığı sütunlu temiz bir liste.
- **Neden bu sırada:** Pusula demosunda 5:30–7:30 arası **Ürün Barkodlama**; sektörün her tanıtımında satıştan sonra stok geliyor. Ayrıca bir sonraki adımın (etiket) girişidir.
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel **4** / dostu 3, **0 kritik**. Uçtan uca çalıştı. Ayrıca doğrulandı: **pasife alınan ürün `/islemler` ürün seçicisinde artık görünmüyor** (10 seçenek, pasif yok) — eski "pasif ürün satılabiliyor" kuşkusu bu turda kapandı.
- ⚠ Bilinen kusurlar (ORTA, demoda değinme): ürün listesinde **stok adedi sütunu yok**; stok penceresinde şube adı yerine **ham kimlik numarası** yazıyor. Müşteri ham kodu görürse: *"O iç numara, ekrandan kalkıyor."*

---

### ADIM 6 — `/urunler/etiketler` · 80 sn

- **Kurucu ne yapar:** **Etiketler**. **Tümünü seç** → "her üründen kaç adet" = **3** yazar. Ekranda etiket önizlemesi çizilir. **Yazdır** ön izlemesini açar, **gerçek çıktı göndermeden** kapatır. *(4 tık)*
- **Ne söyler:** *"Yeni mal geldi, etiketini buradan basıyorsun. Üzerinde barkod, ayar, has gram ve stok kodu var. Bas, yapıştır, tezgâha koy."*
- **Müşteri ne görür:** **Gerçekten çizilmiş barkodlar** — 30 etiketlik sayfa. Kâğıda dökülen ilk somut çıktı.
- **Neden bu sırada:** Pusula demosunun 5:00'teki en güçlü anı **"kaydettim, işte çıktısı"**. Bizde satış fişi çıktısı bugün **bağlı değil** (aşağıya bak), ama **etiket çıktısı çalışıyor ve kanıtlı**. Yani "kâğıt" anını kaybetmiyoruz, sadece yerini değiştiriyoruz.
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel **4** / dostu 3, **0 kritik**. Canlı: 10 ürün listelendi, "Tümünü seç" + 3 adet → "🖨 Etiketleri Yazdır (30)" ve **30 etiket önizlemesi; barkod SVG'leri gerçekten çizildi**.
- ⛔ **SATIŞ FİŞİ ÇIKTISI SÖZÜ VERİLMEZ.** İki sebep, ikisi de ölçülü: (1) satış ekranında `/cikti` sayfasına giden **hiçbir bağlantı yok** (`Pages/Islemler.razor` tarandı, 0 sonuç); (2) `/cikti/{Tur}/{Id}` rotası QA turu 8'de **ENGELLENDİ**, hiç açılamadı. Müşteri fiş çıktısını sorarsa cümle bölüm 6'da.

---

### ADIM 7 — `/kur` → `/vitrin` · 80 sn

- **Kurucu ne yapar:** Menüde yoksa üst bardan/adres yerine **Kur** ekranını açar, kaynağı gösterir. Kur ekranındaki **"Vitrin"** düğmesine basar. Vitrin panosunu gösterir, geri döner. *(3 tık)*
- **Ne söyler:** *"Kur şuradan geliyor, kaynağı yazıyor; istersen elle de girebiliyorsun. Bu düğme de vitrine bakan ekranı açıyor — dükkânın camına koyacağın ikinci ekran. Müşteri fiyatı kendi okuyor, sen tezgâhta uğraşmıyorsun."*
- **Müşteri ne görür:** Canlı kur + kaynağı; sonra müşteriye dönük büyük rakamlı pano.
- **Neden bu sırada:** PusulaX ana ekranının üstünde sabit **HAS/USD/EUR/CHF/GBP** şeridi var — kuyumcunun gözü oraya alışkın. Vitrin ekranı ise tanıtım değil, **dükkânda işe yarayan bir donanım fikri**; demonun ortasında nefes aldırır ve müşteri "bunu camıma koyarım" der.
- **Bugün hazır mı:** **EVET, ikisi de.** `/kur`: görsel **4** / dostu 3, **0 kritik** (canlı: kaynak Altınkaynak, has ₺6.758,45; geçersiz manuel kur reddedildi, geçerli kaydedildi, gerekçeli iptal edildi). `/vitrin`: görsel **4** / dostu **4**, **0 kritik**, uçtan uca test edildi. Vitrin'e giden düğme kodda doğrulandı (`Pages/Kur.razor:36`, `kur-vitrin-ac`) — adres çubuğuna yazmaya gerek yok.
- ⚠ **Vitrin için tam ekran / kiosk kipi yok** (ORTA bulgu). "Camına koyarsın" derken tarayıcıyı F11 ile tam ekran yapmayı provada dene. Ayrıca kur geçmişindeki **100 "İptal et" düğmesinin hepsi devre dışı** ve sebebi yazmıyor — kur geçmişini aşağı kaydırma.

---

### ADIM 8 — `/emanet` · 90 sn  *(demonun "fark" anı)*

- **Kurucu ne yapar:**
  1. **Emanet** ekranını açar.
  2. Bakiyesi sıfır bir müşteri seçer, **K22 · 6,4 gram · taş 0,4** girer, açıklama yazar, **Kaydet**.
  3. Bakiyenin **5,4960 has** olduğunu gösterir.
  4. **Geri al (storno)** düğmesine basar, gerekçe yazar, bakiyenin **sıfıra döndüğünü** gösterir. *(6–7 tık)*
- **Ne söyler:** *"Müşteri altınını emanete bıraktı. Gramını yazdım, program hasını kendi hesapladı — altı buçuk gram 22 ayar, beş buçuk has. Yanlış yazdıysam? Sildirmiyorum, **geri alıyorum**; eski kayıt duruyor, üstüne düzeltme kaydı düşüyor. Defter böyle tutulur."*
- **Müşteri ne görür:** Has hesabının kendiliğinden yapılması ve **silme yerine geri alma**. Kuyumcunun defter mantığı.
- **Neden bu sırada:** Demonun sonuna doğru, müşterinin aklında "bu da öteki gibi" yerine **"bu benim işimi biliyor"** kalsın diye. Emanet, taranan 27 Pusula dersinin **hiçbirinde geçmiyor** (`PUSULA-KIYAS-RAPORU-2026-09-21.md` § 5) — ama bunu **söylemeyeceğiz** (bölüm 6).
- **Bugün hazır mı:** **EVET — ve tam bu senaryo canlı doğrulandı.** QA turu 8: görsel **4** / dostu 3, **0 kritik** ("KRİTİK BULGU KAPANDI"). Canlı kayıt: K22, 6,4 g, taş 0,4 → bakiye **5,4960 has** (net 6,0 g × 0,916 = 5,496, hesap doğru); satır içi açıklama düzenlendi; storno gerekçeyle yazıldı, bakiye 0,0000'a döndü, kaynak satır "Stornolandı" oldu.
- ⚠ Storno ters kaydı TÜR sütununda **"Giriş"** (yeşil rozet) görünüyor (ORTA bulgu). Müşteri takılırsa: *"O yeşil etiket düzeltilecek."*

---

### ADIM 9 — `/raporlar` · 70 sn  *(sıkışırsan ikinci kesilecek adım)*

- **Kurucu ne yapar:** **Raporlar**. Aylık özet + günlük satış grafiği + gün sonu mutabakatı bölümlerini **kaydırarak** gösterir. Hiçbir alt rapora **girmez**. *(1 tık + kaydırma)*
- **Ne söyler:** *"Akşam kepengi indirdiğinde bakacağın yer burası. Bugün ne girdi ne çıktı, kasan tutuyor mu. Başka defter tutmana gerek yok."*
- **Müşteri ne görür:** Dolu bir rapor sayfası: aylık özet, maden dağılımı, günlük satış/alış grafiği, stok değeri, gün sonu mutabakatı.
- **Neden bu sırada:** Pusula demosu da 8:30–9:30 arası raporlarla bitiriyor; sektörün tamamı günü "gün sonu / günlük icmal" ile kapatıyor. **Kapanıştan hemen önce** gelmeli ki son görüntü "her şey yerli yerinde" olsun.
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel **4** / dostu 3, **0 kritik**. Canlı: bütün bölümler doldu.
- ⛔ **`/raporlar/kar-zarar`'a GİRME** (2 yüksek bulgu) ve **Cari Yaşlandırma** bölümünde oyalanma — o rapor **has gram bakiyesini hiç göstermiyor** (YÜKSEK bulgu); kuyumcunun asıl sorduğu sayı orada yok.
- ⛔ **`/raporlar/vade-takibi` demoya konmadı.** Sebebi dürüst: demo kiracısı **geçmişe tarihli kayıt üretemiyor** (`docs/RUNBOOK-DEMO.md` § 1: tohumlanan geçmiş `TEK_GUN`, "tarihi sonradan SQL ile kaydırmak yasaktır"), bu yüzden "GECİKEN" ve "YAKLAŞAN" sütunları **₺0,00** kalıyor (QA turu 8'de de öyleydi). Ekranın adı "Gecikmiş Alacaklar" ama içi boş görünür.

---

### ADIM 10 — `/ayarlar` › Görünüm · 50 sn  *(renk eleştirisinin cevabı)*

- **Kurucu ne yapar:** **Ayarlar** → **Görünüm** kartı. Renk şemasını değiştirir (3 seçenek), **yazı boyutunu büyütür**, aydınlık/karanlık arasında geçiş yapar. Beğenilen ayarda bırakır. *(4 tık)*
- **Ne söyler:** *"Renkler hoşuna gitmemişti. Renk senin seçimin — bak, değişiyor. Yazıyı da büyütebiliyoruz; tezgâhtarın gözü rahat etsin diye. Her kullanıcı kendi ayarını seçiyor, sen dükkânın varsayılanını belirliyorsun."*
- **Müşteri ne görür:** Ekranın rengi ve yazı boyutu **10 saniyede** değişiyor.
- **Neden bu sırada ve neden SONDA:** Bu, üç eleştiriden birinin **yerinde ve anında** kapandığı tek an. Ama **başa konamaz**: canlı tema değişiminin en az bir ekranda ölçülmüş kusuru var — `/hurda`'da tema sayfa açıkken değişince düğme zeminleri eski temada kalıyor ve **yazı okunmaz oluyor (kontrast 1,17)**. Diğer ekranların temiz olduğu kanıtlanmadı. Bu yüzden renk gösterisi **demonun sonuna** konuyor: bozulursa demo zaten bitmiş olur.
- **Bugün hazır mı:** **EVET.** QA turu 8: görsel **4** / dostu **4**, **0 kritik**; profil düzenleme, dil değişimi, tema değişimi canlı doğrulandı.
- ⚠ **"8 renk" SÖZÜ VERİLMEZ.** Bugün kodda **3 şema** var: varsayılan · kurumsal · zümrüt (`GorunumServisi.cs:31`). Plandaki 8 şema yazılmadı. Söylenecek: *"şu an üç renk, daha fazlası geliyor"* — ya da sayı hiç söylenmez.
- ⚠ Şema/yazı boyutu seçimi **yalnız SAHİP** rolünde açıktır (`GorunumKilitli => !SahipMi`). Demoda SAHİP hesabıyla girilmiş olmalı.

---

### ADIM 11 — Kapanış · 70 sn · ekran yok (Panel'e dön, oradan konuş)

- **Kurucu ne yapar:** Panel'e döner, ekrandan gözünü kaldırır, müşteriye bakar.
- **Ne söyler:** *"Üç şey gösterdim: satış tek ekranda bitti, mal ve etiketi yerinde, akşam özeti hazır. Senin üç itirazına da cevap verdim: menü sekiz satıra indi, satış tek ekran oldu, rengi kendin seçiyorsun. Şimdi tek soru: bir hafta kendi malınla, kendi müşterinle deneyelim mi? Kurulumu ben yaparım, sen sadece kullan."*
- **Neden bu sırada:** Great Demo!'nun iki maddesi doğrudan buraya bakıyor: **özetlemeden bitirme** ve **tek bir sonraki adım iste**. Demo "beğendin mi?" ile değil, **evet/hayır diye cevaplanabilir tek bir istekle** biter.
- **Bugün hazır mı:** Evet — ekran yok.
- ⚠ **Fiyat konuşulmaz.** `docs/RUNBOOK-DEMO.md` "Gösterimde söylenmeyecekler": fiyat, indirim oranı, "şu kadar tasarruf" cümlesi yasak (R-8 · BK-49). Müşteri fiyat sorarsa: *"Rakamı sana yazılı vereceğim, ağızdan söylemiyorum."*

---

### Süre tablosu (kronometre için)

| Adım | Ekran | Saniye | Kümülatif |
|---|---|---:|---:|
| 0 | Açılış sözü | 30 | 0:30 |
| 1 | `/giris` | 40 | 1:10 |
| 2 | `/` Panel | 90 | 2:40 |
| 3 | `/islemler` — satış | 120 | 4:40 |
| 4 | `/islemler` — kanun kapısı | 100 | 6:20 |
| 5 | `/urunler` | 70 | 7:30 |
| 6 | `/urunler/etiketler` | 80 | 8:50 |
| 7 | `/kur` → `/vitrin` | 80 | 10:10 |
| 8 | `/emanet` | 90 | 11:40 |
| 9 | `/raporlar` | 70 | 12:50 |
| 10 | `/ayarlar` › Görünüm | 50 | 13:40 |
| 11 | Kapanış | 70 | **14:50** |

**9 ayrı ekran · 3 senaryo · 14 dk 50 sn.** Provada 15:30'u aşarsa adım 5 çıkarılır; hâlâ aşarsa adım 9 çıkarılır.

---

## 2. MÜŞTERİNİN ÜÇ ELEŞTİRİSİNE CEVAP — demonun hangi anı neyi çürütüyor

### ELEŞTİRİ 1 — "Menüler karışık"

| | |
|---|---|
| **Ölçülmüş gerçek** | Profesyonel modda **40 bağlantı** aynı anda açık duruyor, gruplar katlanmıyor (`Layout/MenuTanimi.cs`, sayıldı: 40). Müşteri geçen sefer bunu gördü. |
| **Demoda cevap veren an** | **ADIM 2, ilk 10 saniye.** Menü **8 satır** (`Servisler/BasitMenu.cs`: `/`, `/islemler`, `/cariler`, `/urunler`, `/urunler/etiketler`, `/kasalar`, `/raporlar`, `/bildirimler`) + altta Kılavuz/Ayarlar. |
| **Nasıl cevap veriliyor** | **Konuşarak değil.** Kurucu "menüyü sadeleştirdik" demez; parmağıyla gösterir: *"sol tarafta sekiz satır var, hepsi bu."* Demo boyunca **hiçbir menü grubu açılmaz.** |
| **Kanıt gücü** | **GÜÇLÜ.** Basit mod gerçekten var, sunucuda çözülüyor, bugün çalışıyor. |
| **Dikkat** | Basit menüde ekranda ~10 satır görünüyor (8 + Kılavuz + Ayarlar); plan hedefi ≤7. Sayı söyleme, göster. **Ve "Kılavuz"a asla tıklama** (bölüm 5). |

### ELEŞTİRİ 2 — "Satış ekranı karışık"

| | |
|---|---|
| **Ölçülmüş gerçek** | Profesyonel POS'ta 8 sekme + form yığını var; `Islemler.razor` 2.576 satır. **Basit modda 3 işlem türü** kalıyor (`BasitPos.Turler` = SATIS · ALIS · SATIS_IADE). |
| **Demoda cevap veren an** | **ADIM 3, tamamı.** Tek ekran · tek liste · tek Kaydet · 5–6 tık. Ayrıca **müşteri satış ekranının içinden seçiliyor**, ayrı müşteri ekranına gidilmiyor. |
| **Nasıl cevap veriliyor** | Kurucu tıkları **sesli sayar**: *"okuttum — ekledim — müşteri — kaydet."* Sayı somutlaşınca "karışık" iddiası düşer. |
| **Kanıt gücü** | **ORTA-GÜÇLÜ.** Ekran bugün çalışıyor (0 kritik, canlı satış doğrulandı) ama **Pusula formülü (tek grid + FARK 0,00) henüz uygulanmadı** — o iş S-2 dalgasında ve yazılmadı. Yani "yeniden tasarladık" denemez; "basit modda böyle" denir. |
| **Dikkat — SÖYLENEMEZ** | *"Pusula'da 5 tık, bizde 3"* gibi bir cümle **kurulamaz**. Pusula'nın tık sayısı **ölçülemedi** (`PUSULA-REFERANS-2026-09-23.md`: video kareleri siyah geldi, 320×180 şeritlerde yazı okunmuyor). Yalnız **kendi** tık sayımızı sayabiliriz. |
| **Dikkat — GÖSTERİLEMEZ** | **Nakit + eski altın + kart karma ödeme.** Kodda doğrulandı: karma ödeme bacaklarında **ALTIN yoktur** (`Pages/Islemler.razor:1437` — "Sunucunun kabul ettiği bacak araçları — ALTIN YOKTUR"). Kuyumcudaki en sık ödeme şekli bugün karma olarak girilemiyor. Demoda **hiç açılmaz**, "yolda" da denmez. *(Tek ödeme aracı olarak ALTIN seçilebiliyor — ama bölme yapılamıyor.)* |

### ELEŞTİRİ 3 — "Renkleri iyi değil"

| | |
|---|---|
| **Ölçülmüş gerçek** | Palet WCAG AA'yı geçiyor (27 çift ölçüldü); sorun renk **değeri** değil **disiplin**: 31 farklı ham dolgu, 27 farklı yazı boyutu, 3 düğme sistemi. Görsel tek tipleştirme **yapılmadı** (S-1 dalgası, yazılmadı). |
| **Demoda cevap veren an — 1** | **ADIM 1'den itibaren, sessizce.** Kurucu demodan **önce** şemayı seçip uygular; müşteri ilk saniyeden **yeni rengi** görür, "değiştirdik" demeye gerek kalmaz. |
| **Demoda cevap veren an — 2** | **ADIM 10, 10 saniye.** Renk ve yazı boyutu canlı değişir. *"Renk senin seçimin."* |
| **Kanıt gücü** | **ORTA.** Seçim mekanizması gerçek ve çalışıyor (`/ayarlar` 4/4, 0 kritik). Ama görsel disiplin işi yapılmadı — demoda **görsel dili düzelttik** denemez. |
| **Dikkat** | 3 şema var, 8 değil. **Sayı söyleme.** Ve tema değişimini **demonun sonunda** yap (gerekçesi Adım 10'da). |
| **Ek kalkan** | Demoya alınan 9 ekranın **6'sı görsel puan 4** (Panel, İşlemler, Ürünler, Etiketler, Kur, Vitrin, Emanet, Raporlar, Ayarlar). En düşük puanlı ekranlarımız (`/cariler` 2, `/kilavuz` 2, `/faturalar` 3/2) güzergâhın **dışında**. Renk eleştirisine verilen sessiz cevap budur: **kötü ekranı hiç açmıyoruz.** |

---

## 3. GEREKEN VERİ — temiz demo kiracısı (P-30'a doğrudan girdi)

**Kiracı adı:** `DEMO-<Müşterinin Dükkân Adı> Kuyumculuk` — `DEMO-` öneki zorunlu (`docs/RUNBOOK-DEMO.md` § 0 kural 2: toplu temizlik betiği `DEMO-` adını bilerek reddeder).
**Tohumlama yalnız ürünün kendi uçlarından** yapılır; doğrudan veritabanı yazımı yasak (§ 0 kural 3).
**Müşteri/cari adları kurgusal** olacak (§ 0 kural 4, KVKK). Dükkân adı müşterinin kendi firmasıdır, kullanılabilir.

| # | Kalem | Bugün tohum ne veriyor | **Demo için GEREKEN** | Neden (ölçülü gerekçe) |
|---|---|---|---|---|
| V1 | **Şube** | 1 | **1** (değişiklik yok) | Çok şubeli kiracıda üst barda şube seçici çıkar; demoda fazladan kontrol istemiyoruz |
| V2 | **Kullanıcı** | 1 SAHİP | **1 SAHİP** (kurucu bununla girer) | Renk/yazı şeması **yalnız SAHİP**'te açık (`GorunumKilitli => !SahipMi`) — Adım 10 aksi halde çalışmaz |
| V3 | **TL kasa** | 1 ("Demo Ana Kasa") | **1 TL kasa, açılış bakiyesi ≥ 50.000 ₺ POZİTİF** | **ASLA EKSİ OLMAYACAK.** Kanıt: QA kiracısında `/gider` ekranı "nakit kasa eksi bakiyede olduğu için **hiçbir gider kaydedilemiyor**" (qa-turu-8, KRİTİK). Eksi kasa ürünü kilitliyor, üstelik Panel'in Kasa Özeti kartında görünüyor. |
| V4 | **Cari (müşteri)** | 2 müşteri + 1 tedarikçi | **5 müşteri + 1 toptancı = 6** | Satış ekranındaki müşteri açılır listesi 2 seçenekle boş görünüyor; ayrıca veresiye/alacak için birden fazla müşteri gerek. Adlar kurgusal, telefonlar `0532 000 00 xx` kalıbında. |
| V5 | **Ürün** | 3 | **≥ 12 ürün** | `/urunler/etiketler` QA'da **10 ürünle** gerçekçi göründü ("Tümünü seç × 3 = 30 etiket"). 3 üründe etiket sayfası 9 etikete düşer, tezgâh gibi durmaz. |
| V6 | **Ürün karması** | 2× K22 ziynet + 1× K14 | **4× 22 ayar ziynet** (çeyrek · yarım · tam · ata) · **3× 22 ayar takı** (burma bilezik · zincir · künye) · **2× 14 ayar taşlı** (yüzük · kolye) · **2× 925 gümüş** (kolye · bileklik) · **1× 24 ayar gram altın** | Etiket sayfasında farklı ayarlar yan yana görünmeli; QA turu 8 etiket ekranında tam bu karma vardı (14/22/24 ayar + gümüş) |
| V7 | **Barkod / stok kodu** | var (`DEMO-…`) | **Her üründe dolu**, önek `DEMO-` değil, **dükkân kodu gibi** (ör. `BLZ-22-015`) | Etiket önizlemesinde stok kodu basılıyor; "DEMO-" yazısı ekranda görünmemeli |
| V8 | **Stok adedi** | 1 alış/ürün | **Her üründe ≥ 3 adet** | Adım 3'te satır içi adet 1→2 düzenlemesi yapılacak; stokta 1 adet varsa sunucu reddedebilir |
| V9 | **Geçmiş satış** | 3 (2 nakit + 1 veresiye) | **≥ 8 satış**, tutarlar 2.000–25.000 ₺ arası, **hiçbiri 30.000 ₺ üstü nakit değil** | Panel'in "Günlük satış" ve "Son işlemler" kartları dolu görünsün. 30.000 ₺ üstü nakit tohumlanamaz (tevsik kapısı reddeder). |
| V10 | **Veresiye** | 1 | **2 açık veresiye** | Müşteri kartında bakiye görünsün; "kimden alacağım var" sorusunun cevabı olsun |
| V11 | **Emanet kaydı** | yok | **1 mevcut emanet kaydı** (farklı müşteri) | Adım 8'de ekran **boş açılmasın**; ikinci kaydı kurucu canlı girecek |
| V12 | **Emanet için boş müşteri** | — | **Emanet bakiyesi 0,0000 olan 1 müşteri** ayrılacak | Adım 8'de bakiyenin 0 → 5,4960 → 0 yolculuğu net görünsün (QA aynen böyle doğruladı) |
| V13 | **Kur** | tohum kuru bekler | **DOLU OLACAK.** Demo sabahı kur kaynağından gelmezse **elle kur girilecek** | `/kur` QA'da canlı doğrulandı: manuel kur 6.900,50 kaydedildi ve güncel kur ona döndü. Kursuz demo olmaz: Panel'in Kur kartı ve Vitrin panosu kura bağlı. |
| V14 | **Vitrin panosu** | 22 sabit kalem | **7–10 kalem görünür** bırakılacak, etiketler Türkçe | QA'da pano 7 kalem gösteriyordu; 22 kalemin hepsi açıksa vitrin kalabalık görünür |
| V15 | **Renk şeması + yazı boyutu** | varsayılan | **Seçilmiş ve uygulanmış** olacak (3 şemadan biri) + yazı boyutu orta/büyük | Müşteri ilk saniyeden yeni rengi görsün (bölüm 2, eleştiri 3) |
| V16 | **"QA-TEST" artığı** | — | **SIFIR.** Hiçbir kayıtta "QA", "TEST", "DUZ", "DUZENLENDI" geçmeyecek | QA kiracısında `QA-TEST G1 T8 Kasa DUZ`, `QA-TEST G2T8 Musteri DUZENLENDI`, `QA-TEST G3 T9 Liste` gibi artıklar var; demo kiracısı bunlardan **türetilmez** (RUNBOOK § 0 kural 1) |
| V17 | **Ürün adı** | "Kuyumcu ERP" | **MİHENK** (en az sekme başlığı + açılış yazısı) | 43 dosyada "Kuyumcu ERP", 0 dosyada "Mihenk" (ölçüldü) |

### ⚠ V9 hakkında dürüst uyarı — demoda söylenemeyecek cümle
Demo tohumlayıcısı **geçmişe tarihli hareket üretemez**: `docs/RUNBOOK-DEMO.md` § 1 → *"Tohumlanan geçmişin takvimi: TEK_GUN … bu sürüm 3 aylık geçmiş üretmez … Tarihi sonradan SQL ile kaydırmak yasaktır."*
Sonuç: **Panel'deki "7 günlük satış grafiği" tek güne sıkışık görünecek**, vade takibinde "geciken" satırı olmayacak, kâr-zarar karşılaştırması boş kalacak.
**Kurucu "son bir haftanın satışları" DEMEZ.** Doğru cümle: *"Bunlar bugün girilen hareketler; dükkânda kullandıkça geçmiş kendi kendine oluşuyor."*

---

## 4. S-3 ÖNCELİK SIRASI — sadeleştirme hangi sırayla yapılacak

**Plandaki sıra (§ 6, S-3):** Panel → Cariler → Ürünler → Kasalar → Raporlar → Etiketler → Bildirimler.
**Demo senaryosu bu sırayı iki yerde değiştiriyor.** Değişiklik ve gerekçesi:

### Önce: **S-2.5 "DEMO" mini dalgası — 1 gün, cumartesiden ÖNCE**
S-3'ün önüne, yalnız demoyu bloke eden kalemler. Hiçbiri tasarım işi değil, hepsi kablo/metin:

| # | İş | Gerekçe |
|---|---|---|
| D-a | Yayın anlık görüntüsü yeniden üretilir; `blazor.boot.json`'daki her varlığın HTTP 200 olduğunu doğrulayan bir kapı adımı eklenir | QA turu 8'i tümüyle boşa çıkaran arıza; demo sabahı tekrarlarsa demo biter |
| D-b | `Pages/Panel.razor:273` → `/islem-defteri` **→** `/raporlar/islem-defteri` | Uygulamadaki tek ölü iç bağlantı, demonun 2. ekranında |
| D-c | `betikler/demo-tohumla.js:131` prova tablosundaki `rota: '/vade-takibi'` → `/raporlar/vade-takibi` | Aynı sınıf hata; RUNBOOK § 3.1 bunu 23 Eylül'de yakalamış ama betikte hâlâ duruyor |
| D-d | `index.html` + `App.razor`: **Kuyumcu ERP → MİHENK** | Müşteri ürünün adını ekranda görür |
| D-e | Demo kiracısı bölüm 3'teki listeyle kurulur | P-30 |

### Sonra: **S-3 revize sırası**

| Sıra | Ekran | Plandaki yeri | Değişiklik gerekçesi |
|---|---|---|---|
| **1** | **Panel** (`/`) | 1 | Değişmedi. Demonun 2. adımı ve her sabah açılan ilk ekran. |
| **2** | **Cariler** (`/cariler`) | 2 | **Değişmedi — ama sebebi değişti.** Cariler demoda **gösterilmiyor**; yine de 2. sırada kalıyor çünkü müşterinin "ekranlar karışık" cümlesinin **en güçlü canlı kanıtı** odur: satırda **8 eylem düğmesi**, satır yüksekliği **305 px**, ekranda **yalnız 2 müşteri** görünüyor (qa-turu-8, YÜKSEK). Basit menüde duruyor, yani müşteri her an tıklayabilir. Hedef: satırda ≤3 eylem + "⋯", satır ≤64 px. |
| **3** | **Ürünler** (`/urunler`) | 3 | Değişmedi. Demonun 5. adımı. Ek iş: **stok adedi sütunu eklenmeli** ve stok penceresindeki **ham kimlik numarası** şube adına çevrilmeli (ikisi de ORTA bulgu, demoda gizlenmesi gerekti). |
| **4** | **Etiketler** (`/urunler/etiketler`) | **6 → 4 (YÜKSELDİ)** | Demonun **kâğıt çıktı anı** bu ekrana taşındı, çünkü satış fişi çıktısı (`/cikti`) bugün ne bağlı ne ölçülü. Müşteriye somut çıktı gösteren tek doğrulanmış ekran bu. Ek iş: sonuçsuz aramada boş durum mesajı yok, eski seçim ekranda kalıyor (**yanlış etiket basma riski**, ORTA). |
| **5** | **Raporlar** (`/raporlar`) | 5 | Değişmedi. Demonun 9. adımı, kapanıştan önceki son ekran. Ek iş: **Cari Yaşlandırma has gram göstermiyor** (YÜKSEK) — kuyumcunun asıl sorduğu sayı; Gün Sonu Mutabakatı "FARK VAR" diyor ama düzeltmeye giden bağlantı yok (ORTA). |
| **6** | **Kasalar** (`/kasalar`) | **4 → 6 (DÜŞTÜ)** | Gerekçe: bu ekran QA turu 8'de **hiç açılamadı** (ENGELLENDİ). Ölçülmemiş ekran sadeleştirilemez — önce D-a ile yayın düzelip yeniden ölçülmeli. Ayrıca demoda kasa bilgisi **Panel'in Kasa Özeti kartından** veriliyor, ayrı ekrana gerek kalmadı. |
| **7** | **Bildirimler** (`/bildirimler`) | 7 | Değişmedi. Demo dışı, basit menüde, 0 kritik. En düşük risk, en sona. |

### **S-3b — demoya giren ama basit menüde olmayan 5 ekran** *(yeni, S-3'ten hemen sonra)*
`/giris` · `/kur` · `/vitrin` · `/emanet` · `/ayarlar`
**Gerekçe:** Plan S-3'ü "basit modun ekranları" diye tanımlamış; demo ise bu beşini de kullanıyor ve hepsi 0 kritik. Müşteriye gösterdiğimiz ekran, her gün açılan ekran kadar önemlidir. `/vitrin`'in **hiçbir menüde olmadığı** da burada çözülür (`MenuTanimi.MenusuzRotalar` — yalnız `/kur` ekranındaki düğmeden açılıyor) ve **tam ekran/kiosk kipi** eklenir.

### **S-4'e düşenler — demoda gösterilmeyecek 13 ekran**
`/faturalar` · `/gider` · `/hurda` · `/kilavuz` · `/kurulum` · `/yedekler` · `/gider-pusulalari` · `/siparisler` + `/siparisler/{Id}` · `/banka` + `/banka/ekstre/{id}` · `/kullanicilar` · `/yetkiler` · `/sayim` · `/pos` + `/pos/tahsilatlar` · `/vezne` · `/evraklar` + `/evraklar/takvim` · `/raporlar/kar-zarar` · `/raporlar/kasa` · `/raporlar/satis-analizi` · `/raporlar/islem-defteri` · `/veri-ice-aktarim` · `/veri-disa-aktarim` · `/uyum` · `/denetim-izi` · `/urunler/agac` · `/fiyatlandirma` · `/isletme` · `/cikti/{Tur}/{Id}`
**İstisna — S-4'ün en başına alınacak 3 ekran** (çünkü açık KRİTİK bulguları var ve kritikli ekran hiçbir dalgada "sonraya" bırakılmaz):
1. **`/faturalar`** — 2 kritik: kuyum özel matrah taslağı "Düzenle" ile hiç kaydedilemiyor (iki PATCH de 400); düzenleme kipinde Cari açılır listesi **0 seçenek**.
2. **`/gider`** — 1 kritik: nakit kasa eksi bakiyede olduğu için **hiçbir gider kaydedilemiyor** ve kullanıcıya çıkış yolu gösterilmiyor.
3. **`/kilavuz`** — 1 kritik: `kilavuz.html` sunucuda 404, düğme + **14 bağlantının hepsi ölü**. **Ve bu bağlantı kenar çubuğunun alt bloğunda her modda görünüyor** — yani demoda müşterinin bir tık uzağında.

---

## 5. GÖSTERİLMEYECEKLER — demoda açılmayacak ekranlar ve nedeni

Hepsinin kanıtı `dokumanlar/09-uygulama-kayitlari/ham/qa-turu-8.json` (46 rota · 225 bulgu · 17 kritik · ortalama görsel 3,17 / kullanıcı dostu 2,74).

### A) Açık KRİTİK bulgusu olanlar — **kesinlikle açılmayacak**

| Rota | Puan (G/K) | Kritik bulgu (birebir) |
|---|---|---|
| `/faturalar` | 3 / 2 | "Kuyum (özel matrah) taslak faturası 'Düzenle' ile hiç kaydedilemiyor — düğme var, işlev yok" · "Taslak fatura düzenleme kipinde 'Cari' açılır listesi boş (0 seçenek)" |
| `/gider` | 3 / 2 | "Nakit kasa eksi bakiyede olduğu için HİÇBİR gider kaydedilemiyor; kullanıcıya çıkış yolu gösterilmiyor" |
| `/hurda` | 4 / 3 | "Hurda alış satırının hiçbir eylemi yok — düzenleme de storno da yok" · ayrıca YÜKSEK: "Tema sayfa açıkken değişince düğme zeminleri eski temada kalıyor: metin okunmaz (kontrast 1,17)" ve "Geri alınamayan hurda satışı tek tıkla, onay diyaloğu olmadan yazılıyor" |
| `/kilavuz` | **2 / 1** | "Kullanım kılavuzunun tamamı 404 — sayfadaki düğme ve 14 bağlantının hepsi ölü" — **turun en düşük puanlı ekranı** |
| `/kurulum` | 3 / 2 | "Geri dönülemez 'Kurulumu mühürle', açılış provası için yazılan aynı gerekçe alanıyla tek tıkla açılıyor" |
| `/yedekler` | 3 / 3 | "Yedek talebi gerekçesi sonradan düzenlenemiyor (Düzenle yok)" |
| `/gider-pusulalari` | 4 / 3 | "Listede 'Düzenle' yok; düzenleme yalnız 'Detay' kipinin içinde gizli" |
| `/siparisler/{Id}` | 4 / 3 | "Sipariş detayında cari, tür, şube ve tahmini tutar taslak durumunda bile düzenlenemiyor" |
| `/banka/ekstre/{id}` | 4 / 4 | "Ekstre satırının hiçbir alanı düzenlenemiyor — onaydan ÖNCE bile" |
| `/kullanicilar` | 4 / 3 | "Yayın anlık görüntüsünde iki çerçeve derlemesi eksik — uygulama hiç açılmıyor" |

### B) Ölçülemeyenler — **"çalışıyor" da "bozuk" da denemez, demoya konmaz**

`/cikti/{Tur}/{Id}` · `/kasalar` · `/sayim` · `/banka` · `/pos/tahsilatlar` · `/yetkiler`
Altısı da `durum: ENGELLENDI`, görsel 1 / dostu 1. Tek bulgu, birebir: *"UYGULAMA ACILMIYOR: 5199 yayin anlik goruntusunde 2 _framework varligi eksik (404) — hicbir rota test edilemiyor."* Kanıt: `Microsoft.Extensions.Logging.Abstractions.wasm` 404, `System.ComponentModel.Annotations.wasm` 404; 66 varlığın 64'ü 200.
**`/cikti` en acı kayıp:** "Kaydet → fiş çıktısı" Pusula'nın en güçlü anı, bizde hem rota ölçülmemiş hem de satış ekranından **bağlantısı yok** (`Islemler.razor` tarandı, 0 sonuç). Demoda yeri etiket sayfası aldı (Adım 6).

### C) Boş görünenler — **ekran çalışıyor ama içi boş, demoyu zayıflatır**

| Rota | Neden |
|---|---|
| `/evraklar/takvim` | Bu kiracıda portföy boş: "ADET 0 / TUTAR ₺0,00 / Portföyde evrak yok" |
| `/raporlar/vade-takibi` | GECİKEN ve YAKLAŞAN **₺0,00** — demo tohumu geçmişe tarihli kayıt üretemediği için (RUNBOOK § 1, `TEK_GUN`) her kiracıda böyle görünür |

### D) 0 kritik ama görsel olarak riskli — **açılmayacak**

| Rota | Puan | Neden |
|---|---|---|
| `/cariler` | **2 / 2** | "Her satırda 8 eylem düğmesi; satır yüksekliği 305 px, ekranda yalnız 2 cari görünüyor" (YÜKSEK). Bu ekran müşterinin "ekranlar karışık" cümlesini **kanıtlar**. Müşteri bilgisi satış ekranının içinden gösterilecek. |
| `/raporlar/kar-zarar` | 3 / 3 | 2 YÜKSEK bulgu; ayrıca sayfa başlığı diğer sayfaların kalıbını kullanmıyor |
| `/vezne` | 3 / 3 | Fiş türü (Satış/Tahsilat/İşçilik) sonradan değiştirilemiyor; senaryoda yeri yok |
| `/isletme`, `/pos`, `/yetkiler`, `/kullanicilar` | — | **Ayar/kurulum ekranları demoda zaman israfıdır** — satıcı kurulumunda bir kez yapılan işler. (Great Demo! "Don'ts") |

---

## 6. ARIZA PLANI — bir şey ters giderse

**Altın kural:** Demo bir yazılım denetimi değil. Ters giden şeyi **tamir etmeye çalışma**, **konuşmaya devam et ve bir sonraki ekrana geç.** Müşteri senin ne yaptığını değil, nasıl davrandığını hatırlar.

| Ne olursa | Kurucu ne yapar | Ne söyler (ezberle) |
|---|---|---|
| **Program hiç açılmıyor / "Beklenmeyen bir hata oluştu"** | Ekrandaki **Yenile**'ye bas. Olmazsa **Ctrl+F5**. Olmazsa **önceden açık bıraktığın ikinci sekmeye** geç. Üçüncü denemede vazgeç, kâğıda geç. | *"Bir saniye, sayfayı tazeliyorum."* — Üç kez olursa: *"Bugün buradaki kurulumda bir aksama var; sana ekranları elimdeki çıktılardan anlatayım, kurulumu bu hafta senin dükkânda yapalım, kendi malınla göreceksin."* |
| **Bir ekranda kırmızı hata bandı çıktı** | Bandı okuma, üstüne gitme. Panel'e dön (logo/Panel), senaryonun bir sonraki adımına geç. | *"Bunu not aldım, düzelteceğiz. Devam edelim."* — **"Bu olmamalıydı" deme.** |
| **"Sayfa bulunamadı" çıktı** | Tarayıcının **geri** düğmesini kullanma (oturum karışabilir). Menüden Panel'e dön. | *"Yanlış yere bastım."* — Kısa kes, açıklama yapma. |
| **Satış kaydedilemedi (kırmızı)** | Tutarı düşür ya da ödemeyi HAVALE/KART yap, tekrar kaydet. | *"Bu tutarda nakit yasak, program bırakmıyor. Bankadan geçiriyorum."* — **Hatayı satış argümanına çevir.** |
| **Fiş numarası bandı gözden kaçtı** | `/raporlar/islem-defteri`'ni aç, satırı göster. **Panel'deki "Son işlemler" bağlantısını kullanma.** | *"Kaydettiğim fiş burada duruyor, numarasıyla."* |
| **Kur gelmedi / boş** | Kur ekranından **elle kur gir** (doğrulanmış yol). | *"Kuru kaynaktan alıyoruz; istersen elle de giriyorsun — bak böyle."* |
| **Tema değiştirince yazılar okunmaz oldu** | Aynı düğmeden **eski temaya dön**. Israr etme. | *"Bu renkte biraz oynayacağız, notuma yazdım."* |
| **Müşteri kara listedeki bir ekranı istedi** (fatura, hurda, kılavuz, kasa…) | **Ekranı açma.** Deftere not al, gözünün önünde yaz. | *"Var, ama bugün üç şeye söz verdim. Onu ayrı bir randevuda tek tek göstereyim — yarım göstermek istemiyorum."* |
| **Müşteri "Pusula'da şu var" dedi** | Rakip üzerinden iddia kurma (RUNBOOK yasağı). Soruyu **kendi ekranına** çevir. | *"Sende o iş nasıl yürüyor, bir anlatsana?"* — dinle, not al. Kıyaslama yapma. |
| **Müşteri "kaç tıkla oluyor, Pusula'da kaç?" dedi** | **Kendi sayını söyle, rakibinkini asla.** | *"Bizde beş tık; okuttum, ekledim, müşteri, kasa, kaydet. Pusula'nınkini ben saymadım, sallamayayım."* |
| **Müşteri fiyat sordu** | Rakam telaffuz etme (R-8 · BK-49). | *"Rakamı ağızdan söylemiyorum, sana yazılı geleceğim."* |
| **Müşteri "fiş basıyor mu?" dedi** | Yazdır düğmesi arama. Etiket çıktısına dön. | *"Belge motoru var, satış ekranından düğmesini bu hafta bağlıyoruz. Bugün sana etiket çıktısını göstereyim — barkodlu, ayarlı, has gramlı."* |
| **Müşteri "açık pozisyonum nerede?" dedi** | Uydurma. Panel ve müşteri bakiyesini göster. | *"O tek kutucuk yolda. Şu an borcu-alacağı müşteri kartından ve rapordan görüyorsun."* |
| **Süre 12. dakikaya geldi ama 3 adım kaldı** | **Adım 5 ve 9'u atla**, doğrudan Adım 10 + 11'e geç. | *"Sana söz verdiğim on beş dakikayı aşmayayım; toparlıyorum."* |

**Demo öncesi hazır bulunacak iki kaçış yolu:**
1. **İkinci tarayıcı sekmesi**, girişi yapılmış, Panel açık, bekliyor.
2. **Basılmış etiket sayfası** (30 etiketlik) ve **basılmış bir gider pusulası belgesi** — program hiç açılmazsa elle gösterilecek tek somut kanıt. *(Gider pusulası belge ekranı QA turu 8'de çalıştı: GP-2026-000001 tam basıldı.)*

---

## 7. PROVA LİSTESİ — cuma akşamı, orkestratör baştan sona tıklayacak

**Kural (`docs/RUNBOOK-DEMO.md` § 3):** API provası **iki AYRI kişi/ajan** tarafından koşulur (BK-45); tek koşum kanıt sayılmaz. **Ve API provası yetmez** — § 3.1: *"betik gösterimin veri yolunu ölçer, insanın ekranda geçireceği süreyi değil."* Tarayıcı turu ayrı ve zorunludur. 23 Eylül'de tam bu fark bir kusuru sakladı.

### Bölüm A — Ortam (bunlar yeşil değilse devam etme)

| ✅ | Adım | Kabul ölçütü |
|---|---|---|
| ☐ | A1 | Yayın anlık görüntüsü yeniden üretildi |
| ☐ | A2 | `blazor.boot.json`'daki **66 varlığın hepsi HTTP 200** (curl ile tek tek) | Tek 404 varsa demo yok |
| ☐ | A3 | 6 ENGELLENDİ rota tek tek tarayıcıda açıldı: `/cikti/{Tur}/{Id}` · `/kasalar` · `/sayim` · `/banka` · `/pos/tahsilatlar` · `/yetkiler` | Her biri **çiziliyor**; çizmiyorsa demoya **konmaz** ve Panel'deki "Kasa Özeti →" bağlantısı demoda tıklanmaz |
| ☐ | A4 | `Pages/Panel.razor` ölü bağlantısı düzeltildi (`/islem-defteri` → `/raporlar/islem-defteri`) **ya da** demo makinesinde asla tıklanmayacağı kurucuya yazılı bildirildi |
| ☐ | A5 | Tarayıcı sekmesinde ve açılış ekranında **MİHENK** yazıyor, "Kuyumcu ERP" yazmıyor |
| ☐ | A6 | `dotnet/denetim.sh` TEMİZ; `DEMO_ROTA_KRITIK = 0` (son **iki** prova, `kosan` alanları **dolu ve farklı**) |

### Bölüm B — Veri (bölüm 3'teki listeyle tek tek sayılacak)

| ✅ | Adım | Kabul ölçütü |
|---|---|---|
| ☐ | B1 | Kiracı adı `DEMO-…` ile başlıyor |
| ☐ | B2 | **Kasa bakiyesi POZİTİF** ve Panel'in Kasa Özeti kartında eksi rakam yok |
| ☐ | B3 | Cari sayısı ≥ 6, ürün sayısı ≥ 12, stok her üründe ≥ 3 adet |
| ☐ | B4 | Geçmiş satış ≥ 8, hiçbiri 30.000 ₺ üstü nakit değil |
| ☐ | B5 | Emanet defterinde **1 mevcut kayıt** var; ayrıca bakiyesi **0,0000 has** olan 1 müşteri ayrıldı |
| ☐ | B6 | **Kur dolu** (kaynaktan ya da elle) |
| ☐ | B7 | Renk şeması ve yazı boyutu seçildi, uygulandı |
| ☐ | B8 | **Tüm ekranlarda "QA", "TEST", "DUZ", "DUZENLENDI" araması 0 sonuç** |
| ☐ | B9 | Ürün kodlarında "DEMO-" öneki görünmüyor |

### Bölüm C — Ekran turu (kronometreyle, seçilen renk şemasında, BASİT MODDA)

| ✅ | Adım | Kabul ölçütü |
|---|---|---|
| ☐ | C1 | `/giris` açıldı, **kısıt (hariç) listesi çizildi**, giriş yapıldı |
| ☐ | C2 | Panel çizildi: menüde **8 satır**, Günlük satış / Kur / Kasa Özeti kartları **dolu** |
| ☐ | C3 | **Satış 1 provası:** ürün ekle → adet 1→2→1 → müşteri → kasa → NAKİT → Kaydet → **POST 200**, başarı bandı çıktı. Tık sayısı **sayıldı ve yazıldı**: ____ |
| ☐ | C4 | **Satış 2 provası:** 45.000 ₺ nakit → **Kaydet'e basmadan** uyarı metni ekranda göründü → HAVALE → Kaydet → **POST 200** |
| ☐ | C5 | `/urunler` doldu; bir ürünün Stok penceresi açıldı ve kapandı |
| ☐ | C6 | `/urunler/etiketler`: Tümünü seç + 3 adet → **etiket önizlemesi çizildi, barkodlar göründü**; yazdır ön izlemesi açıldı ve **gerçek çıktı gönderilmeden** kapatıldı |
| ☐ | C7 | `/kur` doldu; **Vitrin düğmesi** tıklandı, pano açıldı, **F11 tam ekran denendi**, geri dönüldü |
| ☐ | C8 | `/emanet`: K22 · 6,4 g girişi → **5,4960 has**; storno → **0,0000**; ekranda kalıcı |
| ☐ | C9 | `/raporlar` bütün bölümleriyle doldu (kâr-zarar'a **girilmedi**) |
| ☐ | C10 | `/ayarlar` › Görünüm: şema değişti, yazı boyutu değişti, **hiçbir ekranda yazı okunmaz olmadı** |
| ☐ | C11 | **Toplam süre ölçüldü: ____ dk ____ sn.** 15:30'u aşarsa adım 5 çıkarılır; hâlâ aşarsa adım 9. |

### Bölüm D — Kara liste kontrolü

| ✅ | Adım | Kabul ölçütü |
|---|---|---|
| ☐ | D1 | Demo güzergâhındaki **hiçbir ekranda** bölüm 5'teki rotalara giden bir düğmeye basılmadığı doğrulandı |
| ☐ | D2 | Kurucuya **4 yasak bağlantı** yazılı verildi: Panel'de "Son işlemler →" · "Kasa Özeti →" · "Hurda Alış" · "Tahsilat" |
| ☐ | D3 | Kenar çubuğunun alt bloğundaki **"Kılavuz"** bağlantısının **tamamen kırık** olduğu (14 bağlantı 404) kurucuya bildirildi |
| ☐ | D4 | Kurucuya **5 yasak cümle** yazılı verildi: fiyat · rakip kıyası · "Pusula'da X tık" · "8 renk şeması" · "son bir haftanın satışları" |

### Bölüm E — İkinci tur (farklı kişi/ajan)

| ✅ | Adım | Kabul ölçütü |
|---|---|---|
| ☐ | E1 | `demo-tohumla.js --prova --kosan "<ikinci kişi>"` koşuldu; **son iki provanın `kosan` alanı farklı** |
| ☐ | E2 | Tarayıcı turu (bölüm C) **ikinci kişi** tarafından da koşuldu, kritik = 0 |
| ☐ | E3 | İkinci tarayıcı sekmesi girişli ve Panel açık bırakıldı (kaçış yolu 1) |
| ☐ | E4 | 30 etiketlik sayfa + 1 gider pusulası belgesi **kâğıda basıldı** (kaçış yolu 2) |

---

## 8. DOĞRULANAMAYANLAR — bu belgenin dürüst kaydı (Anayasa md. 1)

Aşağıdakiler **bilinmiyor**. Demoda bunlara dayanan hiçbir cümle kurulmayacak.

1. **Pusula'da bir satışın kaç tıkla bittiği.** `PUSULA-REFERANS-2026-09-23.md` kendi kaydıyla: *"Tık sayısı hâlâ sayılamadı"* — video kareleri siyah geldi, 320×180 şeritlerde yazı okunmuyor. Bizim 5–6 tık rakamımız ölçülü, rakibinki **değil**. → *"bizde daha az tık"* cümlesi **kurulamaz**.
2. **Müşterinin hangi Pusula'yı gördüğü:** klasik karo-ızgaralı masaüstü mü, mor menülü PusulaX mi. `PUSULA-REFERANS` § 1 bunu kurucunun soracağı açık soru olarak bırakmış, cevap repoda yok. → Demo biçimi bu cevaba göre değişirdi; **cumartesi sorulacak ilk soru budur**, ama demo yine de bu senaryoyla yapılır.
3. **Müşterinin Pusula'da tam olarak neyi beğendiği.** Elimizde yalnız *"Pusula çok sade ve güzel"* genel cümlesi var; ekran/işlem düzeyinde kayıt yok (`sadelestirme/MUSTERI-SESI.md` henüz yok). → *"Pusula'da şunu beğenmiştin"* cümlesi **kurulamaz**.
4. **6 rotanın (`/cikti` · `/kasalar` · `/sayim` · `/banka` · `/pos/tahsilatlar` · `/yetkiler`) bugünkü gerçek durumu.** QA turu 8'de yayın arızası yüzünden **hiç açılmadılar**. "Hazır" da "bozuk" da denemez. Prova A3 bunu kapatacak.
5. **Satış fişi çıktısının (`/cikti`) çalışıp çalışmadığı.** Rota ölçülmedi; ayrıca satış ekranından bağlantısı **yok** (kodda arandı, 0 sonuç). → Demoda fiş çıktısı **vaadi verilmez**.
6. **Basit modda bir ekranın kaç kontrol gösterdiği** (plan hedefi ≤3 alan + ≤12 düğme). Plan bu satırda "ölçülecek (S-0)" diyor; ölçüm defteri boş. → Demoda "şu kadar alan, şu kadar düğme" **söylenmez**.
7. **Menü anlatım süresi ve uçtan uca satış süresi.** Plan § 4: ikisi de "ölçülmedi". Prova C3 ve C11 bunları ilk kez ölçecek.
8. **QA turu 8 verisinin kendi bütünlüğü.** Dosyanın `uretim.not` alanı: *"qa-turu.js tur 8 için ham/qa-turu-8.json YAZMADI — ham veri koşum çıktısından geri yüklendi."* Veri ikinci elden kurtarılmış; betiğin dosyayı yazmaması ayrı bir kusur olarak kayıtlı.
9. **Tur 8 ortalamalarının (görsel 3,17 / dostu 2,74) gerçek değeri.** İçinde 1/1 puanlı 6 ENGELLENDİ rota var; bunlar çıkarılınca ortalamanın ne olduğu **hesaplanmadı** ve burada uydurulmadı.
10. **Demo kiracısında ekranların nasıl göründüğü.** QA turu 8 ölçümleri `QA-TEST` fikstürüyle yapıldı. Demo tohumunun verisiyle ekranların dolu/boş nasıl göründüğü **ölçülmedi** — prova bölüm B ve C bunu ilk kez ölçecek.
11. **Diğer rakiplerin (Dezy, Gramino, ZarifSoft, Terminal Gold, Milyem Plus) gerçek tanıtım videoları izlenemedi**; yalnız web sitelerinin metin ve ekran-fotoğrafı sırası okundu. → *"videoda önce şu ekranı gösteriyorlar"* bu firmalar için **doğrulanmadı**.
12. **"15 dakikada en çok kaç ekran" için tek bir ölçülmüş kaynak bulunamadı.** Yöntem araştırmasındaki 7 (ideal 5) sayısı üç ayrı kaynaktan **türetildi**. Bu belge 9 ekran kullanıyor ve sapmanın gerekçesini bölüm 1'de yazıyor.

---

## 9. Tek sayfalık özet — kurucunun cebine koyacağı kart

```
MİHENK — CUMARTESİ DEMOSU · 14 dk 50 sn · BASİT MOD

 0  0:30  Açılış sözü        "15 dakika. Üç şey: satış, mal, akşam özeti."
 1  0:40  /giris             Kısıt listesini göster, gir
 2  1:30  / Panel            8 SATIR MENÜ · satış · kur · kasa   [TIKLAMA: 4 bağlantı]
 3  2:00  /islemler          SATIŞ (< 30.000 ₺ nakit) · tıkları say
 4  1:40  /islemler          45.000 ₺ NAKİT → UYARI → HAVALE → Kaydet  [NAKİTLE KAYDETME]
 5  1:10  /urunler           Ayar · gram · has                    [kesilebilir]
 6  1:20  /urunler/etiketler 30 ETİKET — kâğıt çıktı anı
 7  1:20  /kur → /vitrin     Kur kaynağı · vitrin panosu
 8  1:30  /emanet            6,4 g → 5,4960 has → GERİ AL → 0
 9  1:10  /raporlar          Gün sonu                             [kesilebilir]
10  0:50  /ayarlar Görünüm   RENK + YAZI BOYUTU değişiyor
11  1:10  Kapanış            Üç maddeyi say + "bir hafta deneyelim mi?"

ASLA AÇMA:  /faturalar /gider /hurda /kilavuz /kurulum /yedekler
            /gider-pusulalari /siparisler /banka /cariler /kar-zarar
            /kasalar /sayim /pos /yetkiler /cikti   (ölçülmedi ya da kritikli)

ASLA SÖYLEME: fiyat · rakip adıyla kıyas · "Pusula'da X tık" ·
              "8 renk" · "son bir haftanın satışları" · "yeni arayüz"

TERS GİDERSE: Yenile → Ctrl+F5 → ikinci sekme → kâğıt.
              "Not aldım, devam edelim." Tamir etmeye çalışma.
```

---

*Hazırlayan: ByGatech A.Ş. — Köprü (Teslimat) · Eray (Satış) · Aşkın (QA), Tamer Bey mührüyle.*
*Kaynaklar: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` · `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md` · `PUSULA-KIYAS-RAPORU-2026-09-21.md` · `docs/RUNBOOK-DEMO.md` · `dokumanlar/09-uygulama-kayitlari/ham/qa-turu-8.json` · kod: `Layout/MenuTanimi.cs`, `Servisler/BasitMenu.cs`, `Servisler/BasitPos.cs`, `Servisler/GorunumServisi.cs`, `Pages/Panel.razor`, `Pages/Islemler.razor`, `Pages/Kur.razor`, `Kuyumcu.Core/I18n/Sozlukler.cs`.*
