# ÜRÜN KARARLARI — kurucu yetkisi devredildi (21 Eylül 2026)

> **KURUCU TALİMATI 16 (21 Eylül 2026, aynen):** *"program hakkında benim yapabileceğim tek şey
> ekranın rengi yazının tipi vs görsel olur; geri kalan ne varsa Türkiye'de ne kadar program varsa
> araştırmıştın oradan karar vermen lazım, orada yoksa nerede takıldıysan onu ayrıyeten araman."*
>
> Yani: **ürünün ne yapacağına ekip karar verir**, kararın dayanağı önce kendi pazar
> araştırmamız, orada yoksa o konuya özel yeni araştırma olur. Kurucuya yalnız **görsel tercih**
> (renk, yazı tipi, düzen) ile **imza/para/sözleşme/kendi makinesi** kalır.
>
> Kural (Anayasa md.1): her kararın **kaynağı yazılır**. Kaynaksız karar yoktur.

---

## K-13 — İMALAT/FİRE SINIRI · hurda satışında fark kaydı

**Soru:** Hurda altın satılınca/rafineriye gidince çıkan fark deftere yazılsın mı?

**Araştırma**
| Kaynak | Bulgu |
|---|---|
| Kendi pazar raporumuz (`arastirmalar/KUYUMCU-YAZILIM-FIRSAT-2026-09-07.md`) | "atölye **fire** takibi has bazlıdır" — ve **Faz 2** başlığında: "atölye fire/ramat, B2B bayi kataloğu, RFID sayım". Yani fire, araştırmanın kendisinde **sonraki faz** sayılmış. |
| CetaSoft (sektörün yerleşik masaüstü programı) özellik listesi | Hurda **stok kalemi** olarak izleniyor, "hareketli ağırlıklı ortalama ile maliyet takibi" ve "kalafat / hurdalaştırma" var. Perakende özellik listesinde **ayrı bir fire modülü yok**. |
| Sektör kaynakları (Dezy rehberi, Ankara Ü. açık ders) | **Eritme firesi %0,2–0,6** ve açıkça **üretim/eritme** sürecine ait. Perakendecinin hurdayı ya rafineriye gönderdiği ya da atölyenin erittiği belirtiliyor. |

**KARAR (Tamer Bey mührü) — ikiye ayrıldı, biri girer biri girmez**

1. **ERİTME FİRESİ → 1.0'a GİRMEZ.** İmalat/atölye kapsam dışıdır (yerleşik kapsam kararı:
   müşteri perakende dükkân). Ekranda gerekçesi görünür: *"Bu sürümde eritme firesi
   kaydedilmez; imalat kapsam dışıdır."*
2. **TAHLİL / MUTABAKAT FARKI → 1.0'A GİRER, ZORUNLUDUR.** Perakendeci hurdayı sattığında
   alıcının (rafineri/toptancı) kabul ettiği has, dükkânın kaydettiği hastan **farklı çıkar**.
   Bu fark yazılmazsa **has-cari denk gelmez** — ve has-cari bu ürünün çekirdek vaadidir
   (pazar raporu: "kuyumcunun vazgeçilmez programı has-cari"). Denk gelmeyen bir has hesabı,
   programın varlık sebebini ortadan kaldırır.

**Uygulama şartları:** Hurda satışı belgesinde iki has alanı ayrı durur — **bizim kaydımız** ve
**alıcının kabul ettiği**. Fark sıfır değilse ayrı bir **"tahlil farkı" bacağı** yazılır
(gerekçe alanı zorunlu), has fark hesabına gider ve çift bacaklı atomik desene uyar
(P-19'un `CiftBacakFabrikasi`'ı). Fark eritme firesi diye **adlandırılmaz** — ayrı kavramdır.
→ **Paket: P-20b** (P-20 bittikten sonra, dalga 5 kapısıyla birlikte).

---

## K-23 — SİPARİŞTE ATÖLYEDE/HAZIR DURUMUNDAN GERİ DÖNÜŞ

**Soru:** Sipariş "ATÖLYEDE" veya "HAZIR" olduktan sonra geri alınabilsin mi?

**Araştırma**
| Kaynak | Bulgu |
|---|---|
| Pazar taraması (Meadify, KuyumERP, CetaSoft, kuyumcuprogrami.com) | Hepsi sipariş durumu takibi ve atölye/tamir izleme sunuyor; **hiçbiri "geri dönüş yasaktır" diye bir kural ilan etmiyor.** Yani sektörde yerleşik bir yasak YOK — bu bir tasarım kararı. |
| **Kendi QA ölçümümüz (tur 7, 21 Eylül)** — en güçlü kanıt | İki **KRİTİK** bulgu aynı hastalığın belirtisi: taslak faturada düzeltmenin tek yolu *"iptal + fatura numarası serisini yakmak"*; taslak siparişte cari/tür/şube/tutar formda hiç yok, *"yanlış müşteriye açılan sipariş ancak iptal + yeniden açma ile düzeliyor"*. Geri dönüşü yasaklamak bu hastalığı **üçüncü ekrana** taşırdı. |
| Kendi kuralımız | **UX-1: her veri girişi düzenlenebilir** (kurucunun sabit talimatı). |

**KARAR (Tamer Bey mührü): GERİ DÖNÜŞ SERBEST — ama sessiz değil, denetimli.**
Dört şart birlikte uygulanır:
1. **Yetki anahtarı** ile korunur (herkes yapamaz).
2. **Gerekçe zorunlu** (boş geçilemez).
3. **Denetim izine olay** yazılır: kim, ne zaman, hangi durumdan hangi duruma, neden.
4. Geri alınan geçiş **belgede ve sipariş geçmişinde görünür** — silinmez, üzeri örtülmez.

Gerekçe: veriyi düzeltilebilir yapmak ile izlenebilir yapmak çelişmez; çelişen şey
"düzeltmek için belgeyi iptal etmek"tir — o da numara serisi yakar ve deftere yalan yazdırır.
→ **Paket: P-22** (kendi dalgasında, bu karar geçerli).

---

## K-10 (birinci dilim) — KDV 23/e ÖZEL MATRAH

**Soru:** Altın ziynet satışında KDV matrahı nasıl hesaplanır, e-belgede nasıl kodlanır?
(Plan bu alanı *"OzelMatrahKodu VARSAYILAN OLARAK BOŞTUR — doğrulanmamış beyan gömülmez"*
diye bilerek boş bırakmıştı.)

**Araştırma — birbirinden bağımsız üç kaynak aynı şeyi söylüyor**
| Kaynak | Bulgu |
|---|---|
| KDV Kanunu 23/e yorumları (muhasebetr, Denet KDV-6 dosyası) | Altından mamul/altın içeren ziynet tesliminde matrah, **külçe (has) altın bedeli düşüldükten sonra kalan** tutardır. Külçe/has altın teslimi KDV'den istisnadır. |
| GİB e-Fatura forumu (0084450) + e-arşiv kod listesi | Özel matrah nedenleri **801–812** aralığında kodlanır; **805 = "Altından mamul veya altın içeren ziynet eşyaları ile sikke altınların teslimi"**. |
| Sektör uygulama rehberi (Dezy) | Aynı formül ve aynı kod; fatura satırında brüt gram, milyem, has altın bedeli (özel matrah tutarı), işçilik matrahı ayrı ayrı gösterilir. |

**KARAR (Tamer Bey mührü):**
* Formül: **KDV matrahı = satış tutarı − (has gram × anlık has altın gram fiyatı)**;
  has gram = (brüt gram × milyem) / 1000.
* **`OzelMatrahKodu` varsayılanı artık boş değil: `805`** — ve değerin **kaynağı ekranda/belgede
  gösterilir**, tek tıkla değiştirilebilir kalır (farklı yorum gelirse kilitlenmiş olmayalım).
* Fatura satırında brüt gram · milyem · has altın bedeli · işçilik matrahı · KDV oranı **ayrı
  alanlar** olarak basılır.

**DÜRÜST RİSK BEYANI (Madde 1):** Bu karar **mali müşavir imzası değildir.** Kaynaklar tutarlı ve
biri GİB'in kendi forumu, ama vergi yorumu bize ait. İlk gerçek faturadan önce bir mali müşavire
**onaylatılması tavsiye edilir**; ürün bunu beklemeden çalışır ve yorum değişirse tek alandan döner.
K-10'un kalan iki dilimi (KMTS karekod takvimi, vade farkı KDV'si) dalga 6'da ayrıca araştırılacak.

---

## K-24 — NEGATİF KASA / KREDİLİ MEVDUAT İSTİSNASI

**Soru:** Kasa ve cari eksiye düşebilsin mi? Düşerse engellensin mi?

**Araştırma**
| Kaynak | Bulgu |
|---|---|
| Muhasebe ilkesi (muhasebetr — "Kasa Hesabının Dönem İçinde Eksi Bakiyeye Düşmesi") | Kasa hesabı **daima borç bakiyesi verir ya da sıfırdır**; alacak (eksi) bakiye "kasada olmayan paranın kaydı" demektir ve vergi incelemesinde **karine** sayılır. |
| Pazar pratiği (Aryosis ön muhasebe ve emsalleri) | Eksi bakiyeyi **engellemiyorlar**; negatife düşen hesabı listede **kırmızıyla işaretliyor**, mutabakatta fark ettiriyorlar. |
| **Kendi QA turumuz 7 (21 Eylül)** | Bugünkü ürünümüzde **ne uyarı ne engel var**: tohum işlemleri sonunda "Ana Kasa" **−176.388,20 TL** oldu ve hiçbir şey söylemedi. |

**KARAR (Tamer Bey mührü): UYAR VE İŞARETLE — ENGELLEME.** Üç katman:
1. **Kasa:** işlem engellenmez; bakiye eksiye düştüğü anda ekranda ve listede **kırmızı uyarı**,
   metni sebebi söyler *("kasada olmayan para kaydedilmiş görünüyor — sayım ya da geç kayıt
   olabilir")*, ve negatif kasa ayrı bir rapor satırı olur.
2. **Banka:** eksi yalnız **beyan edilmiş KMH limiti** kadar sessizdir; limit aşılınca aynı uyarı.
3. **Cari:** eksi/artı **doğaldır** (borç-alacak) — hiçbir kısıt konmaz.

Engellemek yanlış olurdu: tezgâhta satış sırasında kullanıcıyı kilitler ve geç kaydı **gizlemeye**
iter. Sessiz kalmak da yanlış — bugünkü hâli bu ve karineyi görünmez yapıyor.
→ **Paket: P-23** (vezne/kasa ekranlarının sahibi).

---

## K-25 — GİDER PUSULASI: STOPAJ, 7 GÜN, GEÇMİŞ KAYITLAR

**Araştırma**
| Kaynak | Bulgu |
|---|---|
| VUK md. 234 (Müşavirler Kulübü 2026 rehberi, Aposkal, Nilvera) | Gider pusulası, malın teslimi/hizmetin ifasından itibaren **en geç 7 gün** içinde düzenlenir; süresinde düzenlenmeyen belge **hiç düzenlenmemiş sayılabilir**. |
| GVK md. 94/13-a (muhasebenews) | **Vergiden muaf esnaftan** alımda **%2** gelir vergisi stopajı. |
| Aynı kaynak — kritik ayrım | **Nihai tüketiciden** (vergi mükellefi olmayan sıradan vatandaş) alımda **stopaj YOKTUR**; yalnız gider pusulası düzenlenir. |

**KARAR (Tamer Bey mührü):**
1. **Stopaj satıcı türüne bağlıdır ve ekranda seçilir.** Varsayılan **"nihai tüketici → stopaj 0"**,
   çünkü kuyumcunun hurda alımlarının ezici çoğunluğu tezgâh üstü vatandaştandır. "Vergiden muaf
   esnaf" seçilirse **%2**. Oran alanı elle değiştirilebilir; seçilen türün gerekçesi belgeye basılır.
2. **7 gün ölçülür ve uyarılır, engellenmez.** Teslim tarihi ile düzenleme tarihi arası 7 günü
   aşarsa uyarı çıkar; belge yine kaydedilir (geç belge gizlenmemeli), aşım belgede ve denetim
   izinde görünür.
3. **Geçmiş kayıtlar zorlanmaz.** Adresi boş eski kayıtlar ayrı bir "eksik bilgi" listesinde
   gösterilir, tamamlama **teklif edilir** — kullanıcı zorlanmaz.

**DÜRÜST RİSK BEYANI:** Mali müşavir imzası değildir; oran ve süre alan olarak açık kalır.
→ **Paket: P-26.**

---

## K-26 — TESLİMAT KISAYOLUNDAKİ ADRES

**KARAR:** Adres kısayola **gömülmez, kurulumda sorulur.** Kurulum adresi bir kez sorar
(varsayılan `http://localhost:5199`), masaüstü kısayolunu o adresle üretir, adresi tek bir
yapılandırma satırında tutar; adres değişirse kısayol yeniden üretilir.
**Gerekçe:** aynı paket hem tek makinede hem ağ üstünde kurulacak; gömülü adres ikinci kurulumda
**sessizce yanlış yere** bakar. Kılavuz artık "koşullu" değil, kesin yazılır. → **Paket: P-33.**

---

## 1.1'E ERTELENEN / "HARİÇ" YAZILACAKLAR — karar verildi, iş çıkmıyor

| Kalem | Karar | Gerekçe |
|---|---|---|
| **K-15** SMS sağlayıcı | 1.0'da **HARİÇ** — `HARIC-LISTESI.md`ye yazılır | Sağlayıcı sözleşmesi para işidir; ürün mesaj göndermeden çalışır, bildirim ekranda kalır. |
| **K-21** RFID / PVC donanım | 1.0'da **HARİÇ** — ekranda "donanım kapsam dışı" beyanı | Alan zaten var (P-10 bağladı); donanım satın alma kararı ticari. |
| **K-22** Canlı banka API | 1.0'da **HARİÇ** | Banka entegrasyonu sözleşme + kimlik doğrulama işi; ekstre **içe aktarım** yolu zaten var. |
| **K-12** Emanet/rehin hukuki görüşü | **1.1** | Rehin hukuki bir ürün; 1.0 emaneti "saklama" olarak sınırlar, ekranda yazar. |
| **K-14** İkinci el / bandrol kapsamı | **1.1** — D-1'in seri no/bandrol dilimiyle birlikte | 1.0'da seri no serbest metin alanı olarak durur, bandrol iddiası edilmez. |

Bu beş kalem için **kurucuya çıkılmaz**; kararı ekip verdi, "hariç" beyanları tek kaynakta
(`HARIC-LISTESI.md`) toplanır ve pilot davetinde yazılı kısıt olarak gider.

---

## Kurucuda KALAN kalemler (talimat 16 sonrası)

Bunlar "programın ne yapacağı" değil; **imza, para, sözleşme ya da kurucunun kendi makinesi**:

| Kalem | Neden kurucuda |
|---|---|
| **K-20** iCloud eşitlemesini kapatmak | Kurucunun kendi makinesinin ayarı; dosyalarını taşıyabilir. **30 Eylül'de denetimi kırar.** |
| **K-4** depoyu temizlemek (658 MB + 26 artık) | Silme yetkisi kurucuda (kabuk kancası `rm`'i bloklar). |
| **K-8** commit yasağı | Kurucunun açık talimatı; kaldırılması da onun kararı. **Şu an tek kopya riski var.** |
| **K-19** pilot davetinin imzalanması · **K-16** fiyat ilanı | Dışa taahhüt / ticari ilan. |
| **K-2** e-Fatura entegratör sözleşmesi · **K-3** kur verisi lisansı · **K-6** bulut · **K-9** alan adı/TLS · **K-11** ÖKC · **K-5** gerçek üretim sırları | Para ve sözleşme. |
| **K-1** canlı veritabanındaki satırların silinmesi | Geri dönüşü olmayan veri silme — kararı ekip verir, **onayı kurucu** (tek tık). |

Geri kalan tüm kurucu kalemleri (K-13, K-23, K-10, K-24, K-25, K-12, K-14, K-15, K-21, K-22, K-26)
bu belgedeki yöntemle **ekip tarafından araştırmayla** kapatılır.
