# MİHENK — MALİ MÜŞAVİRE SORULAR

**Firma:** ByGatech A.Ş. · **Ürün:** Mihenk (kuyumcu dükkânı yazılımı)
**Sürüm:** 1 · **Tarih:** 23 Eylül 2026
**Bu belge canlıdır** — yeni soru çıktıkça numaralanarak eklenir, sürüm numarası artar.

---

## Bu belge neden var

Yazılımı geliştirirken mevzuata dokunan her noktayı **önce internetten birincil kaynaktan**
(Gelir İdaresi Başkanlığı, Resmî Gazete, MASAK rehberleri, TÜRMOB yayınları) doğrulamaya
çalışıyoruz. Doğrulayamadığımız hiçbir hükmü yazılıma sabit yazmıyoruz; o değer
**yapılandırmadan okunur, varsayılanı tutucu (fail-closed) kalır** ve buraya soru olarak düşer.

Aşağıdaki soruların **hepsinin yanında ekibin bugünkü cevabı ve gerekçesi** yazılıdır.
Sizden beklediğimiz, çoğu için **tek cümlelik "doğru" ya da "yanlış, doğrusu şu"** teyididir.

**Önem sırası:** **[ÖNCELİK 1]** ürünün davranışını belirler · **[ÖNCELİK 2]** ürünü etkiler ama bekleyebilir · **[BİLGİ]** bilgi.

---

## **[ÖNCELİK 1]** S-1 · Eski altınla kısmi ödeme: fatura brüt mü, net mi?

**Soru.** Müşteri satış bedelinin bir kısmını **eski altınıyla** öderse (örn. 50.000 TL'lik bilezik;
müşteri 20.000 TL nakit + 30.000 TL değerinde eski altın veriyor):

- **(a)** Eski altın, **gider pusulalı ayrı bir hurda alımı** sayılıp satış faturası **brüt** (50.000 TL)
  bedelle mi düzenlenmelidir?
- **(b)** Yoksa satış bedelinden mahsup edilip fatura **net** (20.000 TL) tutar üzerinden mi?

**Bizim cevabımız: (a).** Gerekçe: KDVK md. 2/5 *"trampa iki ayrı teslim hükmündedir"* → eski altın
bir ödeme aracı değil, ayrı bir alıştır. Bu alım VUK md. 234 gereği gider pusulasıyla belgelenir.
Satışın KDV matrahı ise KDVK md. 23/e özel matrahıyla, eski altın mahsubundan **bağımsız** hesaplanır.

**Neden kritik.** Sıra ters olursa (satışı net tutardan yazmak) **KDV matrahı eksik çıkar.**
Ayrıca kuyumcunun kârı ve altın pozisyonu yanlış hesaplanır.

**Dikkat:** bu üç hüküm **tek bir maddede birlikte yazılı değildir**; sonucu biz birleştirerek çıkardık.
Karma ödemeye dair **doğrudan bir GİB özelgesi bulamadık.**

> **Cevabınız:**
>
> ☐ (a) doğru ☐ (b) doğru ☐ Başka: ..................................................

---

## **[ÖNCELİK 1]** S-2 · MASAK kimlik tespiti eşiği bugün kaç TL?

**Soru.** Kıymetli maden/mücevher ticaretinde **kimlik tespiti zorunluluğunun parasal eşiği**
bugün itibarıyla nedir?

**Bizim değerimiz: 185.000 TL.** Ama bu rakamı yalnız **ikincil kaynaklardan** okuduk. Birincil kaynak
olan MASAK Kuyumcular Rehberi'nin PDF'ini indirdik, **metnini otomatik çıkaramadık.**

**Neden kritik.** Yazılım bu eşiğin üstündeki işlemde kimlik tespiti olmadan kaydı **reddediyor**
(fail-closed). Eşik yanlışsa ya gereksiz engel çıkar ya da yasal yükümlülük atlanır.

**Şu an ne yapıyoruz.** Değeri yapılandırmada tutuyoruz, **siz teyit edene kadar değiştirmiyoruz.**

> **Cevabınız:** eşik ............... TL · dayanak: ................................................

---

## **[ÖNCELİK 1]** S-3 · KDV özel matrah neden kodu 805 kullanılsın mı?

**Soru.** Ziynet eşyası tesliminde KDVK md. 23/e özel matrah uygulanırken e-Arşiv/e-Fatura'da
**özel matrah neden kodu "805"** yazılmalı mıdır?

**Bizim durumumuz.** Kod alanı **bugün BOŞ ve kapalı.** Yazılım bu kodu hiçbir hesaba katmıyor;
yalnız e-belge etiketidir. Sizin teyidiniz gelmeden **açmıyoruz** ve ürün içinde
*"bu kurulumda hariç"* beyanını gösteriyoruz.

**Neden önemli.** İlk gerçek faturadan önce doğru olmalı; sonradan düzeltmek fatura serisini yakar.

> **Cevabınız:** ☐ 805 doğru ☐ Başka kod: .......... ☐ Özel matrah uygulanmamalı

---

## **[ÖNCELİK 1]** S-4 · Hurda alımında stopaj oranları doğru mu?

**Soru.** Nihai tüketiciden ve esnaf muaflığı kapsamındaki satıcıdan eski altın alımında
uygulanacak **gelir vergisi stopaj oranları** nedir?

**Bizim değerlerimiz:**
- Esnaf muaflığı kapsamındaki satıcı: **%2**
- Nihai tüketici (zati eşyasını satan): **%0**

**Neden önemli.** Stopaj, müşteriye ödenecek net tutarı doğrudan değiştiriyor.

**Ayrıca:** kesilen stopajın **vergi dairesine borç olarak muhasebeleştirilmesi yazılımda YOK.**
Vergi yükümlülüğü hesabı modellenmemiştir — bunun sizde mi yazılımda mı durması gerektiğini de
söylerseniz seviniriz.

> **Cevabınız:** esnaf muaf %....... · nihai tüketici %....... · stopaj borcu: ☐ bizde ☐ yazılımda

---

## **[ÖNCELİK 2]** S-5 · Gider pusulası 7 gün kuralı ve VUK md. 234'ün güncel metni

**İki soru.**

1. Gider pusulasının **düzenlenme süresi 7 gün** olarak tuttuğumuz doğru mu?
2. VUK md. 234'ün **7338 sayılı Kanun (2021) sonrası güncel metninde**
   *"zati eşyalarını satan kimselerden satın alınan altın, mücevher"* ibaresi **aynen duruyor mu?**

3. *(24.09 eklendi)* Düzenlenmiş bir gider pusulasında **satıcı adresi** ve **mal açıklaması**
   sonradan **düzeltilebilir mi?** Düzeltilebilirse bunun bir **süresi** var mı (ör. 7 günlük
   düzenleme süresi geçtikten sonra da düzeltme yapılabilir mi)?

**Neden soruyoruz.** Elimizdeki alıntı **2017/2018 tarihli** bir SMMM yayınından; mevzuat.gov.tr'ye
erişimde sertifika hatası aldık, güncel tam metni birincil kaynaktan teyit edemedik.
24 Eylül'de 3. soru için yeniden denedik: GİB sayfası boş döndü, mevzuat.gov.tr yine sertifika hatası verdi.

**Yazılımın bugünkü davranışı (3. soru için).** Satıcı adı, TCKN, tutar ve tarih **kilitli**; bunlarda
hata varsa tek yol pusulayı **iptal edip yeni hurda alışı** girmek. Adres ve mal açıklaması
**düzeltilebiliyor**, her düzeltme denetim izine eski ve yeni değerle yazılıyor. Süre geçtiğinde
**kilitlemiyoruz**, yalnız ekranda "mali müşavirinize sorun" uyarısı çıkıyor. Cevabınıza göre süre
sonrası düzeltmeyi kilitleyebiliriz.

> **Cevabınız:** süre ....... gün · ibare: ☐ duruyor ☐ değişti, yenisi: ........................
> 3. soru: adres/mal açıklaması ☐ düzeltilebilir ☐ düzeltilemez (iptal + yeni pusula) · süre sonrası: ☐ serbest ☐ kilitlensin

---

## **[ÖNCELİK 2]** S-6 · Tevsik zorunluluğu takas/trampada işler mi?

**Soru.** 459 sayılı VUK Genel Tebliği'ndeki **tevsik haddi (bugün 30.000 TL)**, bedelin bir kısmının
**ayni olarak (eski altınla)** ödendiği işlemlerde nasıl uygulanır? Had, işlemin **brüt** tutarına mı
bakar, yoksa yalnız **nakit** kısmına mı?

**Bizim cevabımız: brüt tutara bakar.** Yazılım böyle davranıyor — yani *"yarısını altın verelim,
had delinsin"* mümkün değil. Tebliğ metninde **"takas/trampa/ayni" kelimeleri geçmiyor**, bu yüzden
kapıyı **sıkı** tarafta bıraktık.

**Not.** Tevsik haddinin kendisi doğrulanmıştır: 575 s. Tebliğ, **RG 30.11.2024 / 32738** ile
7.000 TL → 30.000 TL. Sorumuz haddin değeri değil, **ayni ödemede nasıl uygulandığı.**

> **Cevabınız:** ☐ brüt doğru ☐ yalnız nakde bakılır ☐ Başka: ..........................

---

## **[ÖNCELİK 2]** S-7 · Eski altın alımında hangi kur kullanılmalı?

**Soru.** Dükkân müşteriden eski altın alırken TL karşılığını hangi fiyatla hesaplamalı?
**İstanbul Kuyumcular Odası kuru** mu, **Borsa İstanbul Kıymetli Madenler Piyasası rayici** mi,
yoksa **dükkânın kendi alış fiyatı** serbest mi?

**Bizim cevabımız: dükkânın kendi alış fiyatı serbesttir.** Gerekçe: VUK md. 274/A "borsa rayici"
der ama bu **yalnız dönem sonu değerlemesi** içindir; günlük alım fiyatı mevzuatta belirlenmemiştir.
Yazılım fiyatı **işlem anında donduruyor**, fişi sonradan yeniden değerlemiyor.

**Neden soruyoruz.** Kaynak seçimi bir yapılandırma kalemi; yanlış kaynak seçilirse dönem sonu
değerlemesiyle tutarsızlık çıkabilir.

> **Cevabınız:** ................................................................................

---

## **[ÖNCELİK 2]** S-8 · MASAK risk temelli yaklaşım: hangi skorda hangi tedbir?

**Soru.** MASAK'ın **risk temelli yaklaşımının kuyumcuyu BAĞLAYAN biçimi** nedir — yani
"şu risk skorunun üstünde şu tedbir zorunludur" diyen bir hüküm var mı?

**Bizim durumumuz.** Risk skorlama modülü yazılımda **var ama KAPALI.** Açıkken skor hesaplanıp
gösteriliyor, **hiçbir kapı kurmuyor**; satış/MASAK/tevsik kapılarına bağlanmıyor. Puanlar
**mevzuat değil politika** — "doğru" oldukları iddia edilmiyor.

**Ek soru.** "Yüksek riskli ülkeler" listesi yazılımda **boş** — bu listeyi biz uyduramayız.
**FATF / Hazine ve Maliye Bakanlığı listesini** uyum görevlisi mi girmeli, kaynağı nedir?

> **Cevabınız:** ................................................................................

---

## **[ÖNCELİK 2]** S-9 · Has/ayar bazlı takip — muhasebede yeri

**Soru.** Kuyumcunun **has (1000 ayar karşılığı) bazlı** stok ve cari takibi, muhasebede
**Tek Düzen Hesap Planı'nda 153 Ticari Mallar'ın alt hesaplarında miktar takibi** olarak mı
yürütülmelidir?

**Bizim cevabımız: evet.** "Has defteri" yasal bir defter değil, işletme içi envanter/pozisyon
takibidir (VUK md. 186: envanter *saymak, ölçmek, tartmak ve değerlemektir*). Yazılım hem gram hem
TL tutuyor; **mali hakikat TL bacağında**, pozisyon has bacağında okunuyor.

**Çürüttüğümüz bir iddia.** Bazı yayınlarda geçen *"VUK Geçici md. 35 ile kuyumcular Altın/Gümüş
Esaslı Muhasebe Sistemine geçebilir"* ifadesi **yürürlükteki bir hüküm değil**; kaynak makale
sistemin **getirilmesini talep ediyor**. Buna dayanmadık. Teyit eder misiniz?

> **Cevabınız:** ................................................................................

---

## **[BİLGİ]** S-10 · Dönem sonu değerleme kancası

**Soru.** VUK md. 274/A (7524 s. Kanun, RG 02.08.2024) kıymetli madenlerin **borsa rayici ile
değerlenmesini** ve bu hükmün **kıymetli maden cinsinden alacak ve borçlar** için de geçerli
olmasını öngörüyor. Yazılımın dönem sonunda bu yeniden değerlemeyi **kendi yapması** mı beklenir,
yoksa bu tamamen **sizin tarafınızda** kalan bir işlem midir?

**Bizim durumumuz.** Yazılım gramı **saklıyor** (değerlemenin girdisi olduğu için) ama
**yeniden değerleme yapmıyor.** Kanca bırakıldı, hesap kurulmadı.

> **Cevabınız:** ☐ yazılım yapsın ☐ bizde kalsın ☐ Başka: ..............................

---

## Cevaplarınız yazılıma nasıl girecek

Bu eşiklerin ve oranların **hiçbiri yazılımın içine sabit yazılmıyor.** Hepsi ayrı bir
**program parametreleri** tablosunda, **geçerlilik tarihiyle birlikte** duracak — yani
30 Kasım 2024'ten önceki bir işlem eski tevsik haddiyle (7.000 TL), sonraki yenisiyle (30.000 TL)
değerlendirilir. Bir parametreyi değiştirmek geçmişi bozmaz.

Her parametrenin yanında **kaynağı** da yazılı olacak (Resmî Gazete tarih/sayı, tebliğ no) —
böylece bir denetimde "bu rakam nereden geldi" sorusunun cevabı ekranda durur.

---

## Sürüm geçmişi

| Sürüm | Tarih | Değişiklik |
|---|---|---|
| 1 | 23.09.2026 | İlk sürüm — 10 soru (has defteri kararı, MASAK eşiği, KDV özel matrah, stopaj, gider pusulası, tevsik/trampa, kur kaynağı, risk skorlama, has takibi, dönem sonu değerleme) |
| 2 | 24.09.2026 | S-5'e 3. soru: düzenlenmiş gider pusulasında adres/mal açıklaması düzeltmesi ve süre sonrası düzeltme (P-55 paketinde birincil kaynak açılamadı) |
