# MİHENK — Kullanım Kılavuzu

Kuyumcu işletmenizin satışını, alışını, müşteri hesaplarını, stoğunu, kasasını, bankasını ve faturasını tek programda tutan MİHENK'in adım adım rehberi. Programdaki her ekranın bu kılavuzda kendi bölümü vardır; ekrandayken üst çubuktaki **Yardım** bağlantısı sizi doğrudan o bölüme getirir.

<!--
  ═══════════════════════════════════════════════════════════════════════════
  YAZARA NOT (kılavuza basılmaz) — PAKET P-33 (B-11)

  BU DOSYA KILAVUZUN TEK KAYNAĞIDIR. Kuyumcu.Web/wwwroot/kilavuz.html bu
  dosyadan ÜRETİLİR (dotnet/kilavuz-uret.sh); HTML'i elle düzenlemeyin —
  KilavuzUretimTestleri ve denetim.sh KILAVUZ_URETILMIS farkı yakalar.

  KURALLAR:
  • Programdaki HER rota (Kuyumcu.Web/Layout/RotaBasliklari.cs) burada TAM BİR
    kez "ekran: /rota" işaretiyle geçer ve işaretin bağlı olduğu başlık
    ekranın menüdeki adını taşır (denetim.sh KILAVUZ_EKSIK_EKRAN = 0).
  • SABİT ADRES / PORT YAZILMAZ: adres kurulumda sorulur ve kısayola yazılır
    (K-26). Eski başlatıcı dosyaların adı da geçmez (ESKI_PORT_REFERANSI = 0).
  • Mevzuat hükmü (eşik tutarı, madde numarası, oran) YAZILMAZ: eşikler
    programın yapılandırmasından gelir; kural sorusu mali müşavire gider.
  • "Bu sürümde olmayanlar" elle yazılmaz; HARIC-LISTESI.md'den GÖMÜLÜR
    (BK-26, HARIC_BEYAN_KOPYA).
  • Oturumsuz erişim kararı: kılavuz statik dosyadır, işletme verisi taşımaz
    ve giriş yapamayan kullanıcı da okuyabilsin diye oturum istemez.
  ═══════════════════════════════════════════════════════════════════════════
-->

<!-- grup: Başlangıç -->

## Tanıtım ve temel kavramlar {#tanitim}

MİHENK her işlemi **hem Türk Lirası hem de has (saf altın) gram** cinsinden kaydeder. Buna *paralel defter* denir: bir müşterinin hem TL bakiyesini hem de size olan gram altın borcunu ya da alacağını aynı anda görürsünüz.

- **Has gram:** ürünün içindeki saf altın miktarıdır. Program gram, taş ağırlığı ve ayardan kendisi hesaplar; siz yalnız ürünün gramını ve ayarını girersiniz.
- **Kur anlık görüntüsü:** her işlem, yapıldığı andaki kura bağlanır. Kur sonradan değişse de eski işlemin değeri değişmez.
- **Cari:** para ya da altın alıp verdiğiniz her kişi veya firma (müşteri, tedarikçi, atölye).
- **Basit ve profesyonel mod:** basit mod günlük işte gereken ekranları gösterir; profesyonel mod bütün ekranları açar. Yeni açılan işletme basit modda başlar. Mod, üst çubuktaki anahtarla değiştirilir.

Tam tanımlar için [Kavramlar sözlüğü](#sozluk) bölümüne bakın.

## Programı açma {#baslat}

MİHENK işletmedeki bilgisayarın tarayıcısında açılır. Kurulumu yapan kişi masaüstünüze **MİHENK** adlı bir kısayol bırakır; gün başında yalnız o kısayola çift tıklarsınız.

1. Masaüstündeki **MİHENK** kısayoluna çift tıklayın.
2. Tarayıcı açılır ve **Giriş** ekranı gelir.
3. Size verilen e-posta ve şifreyle giriş yapın (bkz. [Giriş](#giris)).

> [!BILGI] **Adres nereden geliyor?** Programın adresi kurulumda bir kez sorulur ve kurulumda sorulan adres kısayola yazılır. Tek bilgisayarlık kurulumda bu adres o bilgisayarı, işletme ağındaki kurulumda ağdaki sunucuyu gösterir. Bu yüzden kılavuzda sabit bir adres yazmıyoruz; yazsaydık sizinkiyle uyuşmayabilirdi. Adres değişirse kurulumu yapan kişi kısayolu yeni adresle yeniden üretir.

> [!UYARI] Program açılmıyorsa kapatıp yeniden denemeden önce tarayıcıda görünen hata metnini ve hata kodunu not edin, destek hattına iletin. Tarayıcıyı kapatmak hiçbir kaydı silmez: bütün kayıtlar sunucuda tutulur.

## Giriş {#giris}

<!-- ekran: /giris -->

1. E-posta ve şifrenizi girin, **Giriş Yap**'a basın.
2. İki aşamalı giriş açıksa telefonunuzdaki doğrulama uygulamasının ürettiği kodu girin.
3. Sağ üstteki **TR / EN** seçimiyle giriş öncesinde de dili değiştirebilirsiniz.

İşletme kaydı yalnız davet koduyla açılır. İlk kurulumda davet kodunu size satış ekibi verir; **İşletme Kur** sekmesinde işletme adını ve ilk kullanıcıyı (Sahip) oluşturursunuz.

> [!IPUCU] Şifrenizi unuttuysanız giriş ekranında **Parolamı unuttum**'a basın: e-postayla yeni parola alabilirsiniz (aşağıda). İşletmenizin Sahip ya da Müdür rolündeki kullanıcısı da **Kullanıcılar** ekranından şifrenizi sıfırlayabilir.

## Parolamı unuttum {#sifre-sifirla}

<!-- ekran: /sifre-sifirla -->

1. Giriş ekranında **Parolamı unuttum**'a, ardından **E-postayla yeni parola al**'a basın.
2. Kayıtlı e-posta adresinizi yazıp **Bağlantı gönder**'e basın. Adres kayıtlı olsun ya da olmasın ekran aynı cevabı verir; bu, hesabınızın varlığının başkalarınca öğrenilmesini engeller.
3. E-postanızdaki bağlantıyı açın, yeni parolayı iki kez yazıp **Parolayı kaydet**'e basın. Parola en az 8 karakter olmalı, içinde en az bir harf ve bir rakam bulunmalıdır.
4. **Girişe dön** ile yeni parolanızla giriş yapın. İki aşamalı giriş açıksa doğrulama kodu yine sorulur.

Bağlantı 30 dakika geçerlidir ve yalnız bir kez kullanılır. Yeni bağlantı isterseniz eskisi geçersiz olur. Aynı adres için 15 dakikada en çok 3 bağlantı istenebilir. Parola değişince diğer cihazlardaki oturumlar yenilenemez ve kısa süre içinde kapanır.

> [!BILGI] E-postayla sıfırlama her kurulumda açık değildir. Ekran "E-postayla sıfırlama kapalı" diyorsa işletme sahibine ya da destek hattına başvurun; Sahip ya da Müdür **Kullanıcılar** ekranından size yeni parola verebilir.

## Arayüz turu {#arayuz}

Program üç bölümden oluşur: solda **menü**, üstte **başlık ve kur çubuğu**, ortada **çalışma alanı**.

- **Sol menü:** ekranlar gruplar hâlinde durur: Günlük İş, Müşteri ve Stok, Para ve Banka, Belge ve Uyum, Dökümler, Yönetim. Yetkinizin yetmediği ekran menüde görünmez.
- **Üst çubuk:** bulunduğunuz ekranın adı, canlı kur, basit/profesyonel mod anahtarı ve **Yardım** bağlantısı.
- **Her ekranın başı:** ekranın ne işe yaradığını anlatan kısa açıklama ve ekrandaki tek ana düğme.
- **Bantlar:** kaydettiğiniz işin sonucu yeşil başarı bandında (fiş no, fatura no, yeni bakiye), bir sorun kırmızı hata bandında yazar. Hata olduğunda liste "kayıt yok" demez; hata bandı çıkar.
- **Kilitli düğme:** bir düğme basılamıyorsa yanında neyin eksik olduğu yazar (kur yok, kasa seçilmedi, gerekçe çok kısa gibi).

### Tüm ekranlar

<!-- ekran: /ekranlar -->

Menünün en altındaki **Tüm ekranlar** bağlantısı, yetkili olduğunuz bütün ekranları kutucuklar hâlinde gösterir. Üstteki arama kutusuna "fatura", "kasa", "sayım" gibi bir sözcük yazarak ekranı bulabilir, sık kullandığınız ekranı yıldızlayarak favorilerinize ekleyebilirsiniz.

## Bir iş günü {#gunluk-akis}

Programı ilk kez kullanıyorsanız bir iş gününü şu sırayla yürütün; her adımın ayrıntısı ilgili ekranın bölümündedir.

1. **Güne başlarken:** kısayoldan programı açıp giriş yapın. Üst çubuktaki kurun güncel olduğunu kontrol edin (bkz. [Kur](#kur)).
2. **Panele bakın:** gecikmiş alacak, bekleyen sipariş ve kasa bakiyeleri panelde görünür (bkz. [Panel](#panel)).
3. **Satış ve alışları girin:** müşteriyle yaptığınız her alışverişi **İşlemler** ekranından kaydedin; başarı bandındaki fiş numarasını müşteriye verin (bkz. [İşlemler](#islemler)).
4. **Emanet ve siparişleri işleyin:** size bırakılan altını **Emanet**, sipariş ve tamirleri **Siparişler** ekranında tutun.
5. **Gün sonunda:** **Kasa Hareketleri** dökümünü açıp kasadaki parayı sayın ve karşılaştırın. Fark varsa hareketleri tek tek kontrol edin; yanlış kaydı silmeyin, storno ile düzeltin.
6. **Haftada bir:** Sahip rolündeki kullanıcı **Yedekler** ekranında son yedeğin doğrulandığını ve son geri yükleme tatbikatının tarihini kontrol eder.

> [!IPUCU] Basit modda yalnız günlük işte gereken ekranlar görünür. Bir ekranı bulamıyorsanız menünün altındaki **Tüm ekranlar** bağlantısından arayın.

## Kur {#kur}

<!-- ekran: /kur -->

Üst çubuktaki kur alanı gram altın, has altın ve döviz değerlerini gösterir ve her ekranda görünür. Değerler değiştiğinde açık olan bütün ekranlara aynı anda düşer; sayfayı yenilemeniz gerekmez.

**Kur** ekranında güncel kurları, kurun hangi kaynaktan ve ne zaman geldiğini görürsünüz.

- **Otomatik güncelleme:** kurlar yapılandırılmış kaynaktan düzenli aralıklarla çekilir.
- **Elle yenileme:** Sahip ve Müdür rolleri o anki kuru hemen çektirebilir.
- **Elle kur girişi:** kaynağa ulaşılamıyorsa yetkili kullanıcı kuru elle girebilir; elle girilen kur ekranda öyle işaretlenir.

> [!UYARI] Her işlem o andaki kura bağlanır. Kur yanlışsa has hesabı da yanlış olur. Kur çubuğu "kur yok" ya da "eski" diyorsa satış yapmadan önce kuru yenileyin.

### Kur kaynakları {#kur-kaynaklari}

Kurların nereden geldiğini **İşletme → Ayarlar** sekmesinden siz seçersiniz. Üç ayrı seçim vardır; her birinin yanında kısa bir durum yazar (son çekim saati, son kapanış, "anahtar tanımlı değil" gibi). Seçim hemen yazılmaz: **Değişikliği kaydet** düğmesine basıp açılan pencerede onaylarsınız. Bu ayarları işletme sahibi değiştirir.

1. **Kur kaynağı (altın):** has altın ve sarrafiye fiyatlarının geldiği yer — Altınkaynak, Harem Altın, Truncgil ya da Ankara Altın Borsası.
2. **Döviz kaynağı:** dolar, euro ve sterlin kurunun geldiği yer. Altın kaynağından ayrı seçilir. **Merkez Bankası (EVDS)** önerilen kaynaktır; bunun için işletmenize ait EVDS anahtarının sistemde tanımlı olması gerekir. Anahtar yoksa bu seçenek soluk görünür ve seçilemez. İsterseniz **Altın kaynağıyla aynı** seçeneğiyle dövizi altın kaynağından da alabilirsiniz. Merkez Bankası günde bir kez kur yayımladığı için döviz kuru gün içinde değişmez.
3. **Faturada has fiyatı:** kuyum faturasında KDV'nin dışında bırakılan has altın bedelinin hangi fiyatla hesaplanacağı. Mevzuat bu fiyat için Borsa İstanbul'un son işgünü külçe altın kapanışını ister; yeni işletmelerde varsayılan budur.
   - **Borsa İstanbul kapanışı:** her işgünü Borsa İstanbul günlük bülteninden kapanışı siz girersiniz (aşağıya bakın).
   - **Harem Altın** ya da **Altınkaynak:** kapanış o kaynaktan kendiliğinden alınır.
   - **Kendi girdiğim kapanış:** kapanışı kendi belirlediğiniz değerle girersiniz.
   - **Satıştaki kur:** satış anındaki kur kullanılır (önceki davranış).

Faturada **son işgününün** kapanışı kullanılır: bugünün faturası dünün (hafta sonuysa cumanın) kapanışıyla hesaplanır. Son 10 günde kapanış yoksa fatura **kesilmez** ve ekran nedenini söyler; kapanışı girdikten sonra faturayı yeniden kesin. Her faturaya kullanılan kaynağın kodu (BIST, HAREM, ALTINKAYNAK, ELLE, ISLEM_KURU) ile kapanışın günü ve fiyatı yazılır. Ürün kartında "binde" pay tanımlı ürünlerde has fiyatı kullanılmaz; o faturaya BINDE yazılır.

**Kapanışı elle girme:** **İşletme → Ayarlar**'da has fiyatı kaynağı Borsa İstanbul ya da Kendi girdiğim kapanış seçiliyken **+ Kapanış gir** düğmesi görünür. Günü seçin, kapanış değerini yazın ve birimini seçin. Borsa İstanbul bülteni fiyatı **TL / kg** olarak yayımlar; bu birimi seçerseniz değer 1.000'e bölünerek gram fiyatına çevrilir (1000/1000 saflık). Yanlış girdiyseniz aynı günü yeniden girerek düzeltebilirsiniz; ancak bir faturada kullanılmış kapanış artık değiştirilemez.

> [!UYARI] Harem Altın, Altınkaynak, Truncgil ve Ankara Altın Borsası ile veri kullanım sözleşmemiz yoktur. Bu kaynaklar ekranda **İzinsiz kaynak** diye işaretlenir ve yalnız sistem yöneticisi açtığında görünür. Harem Altın için yazılı izin başvurusu satış sonrasında yapılacaktır; o zamana kadar ticari kullanım riski işletmeye aittir.

**Kuru koruyan iki kapı:**

- **Eski kur:** kaynaktan birkaç dakikadır yeni kur gelmiyorsa satış ve alış kaydedilmez; ekran kurun hangi saatten beri güncellenmediğini söyler. Kur ekranından kuru yenileyin ya da elle girin. Elle girilen kur yalnız girildiği gün geçerlidir.
- **Ani sıçrama:** kaynaktan gelen fiyat son kurdan çok farklıysa (yüzde 20'den fazla) yazılmaz; fiyatlar eski kurda kalır ve Sahip ile Müdür'e bildirim gider. Fiyat gerçekten doğruysa Kur ekranından elle girip "Fiyat doğru" onayıyla ve nedenini yazarak kaydedin.

<!-- grup: Günlük İş -->

## Panel {#panel}

<!-- ekran: / -->

Girişten sonra açılan ana ekrandır. Günün satış ve alış toplamları, has gram durumu, kasa bakiyeleri ve son yedi günün satış grafiği burada görünür.

- Kartlardaki her sayı ilgili ekrana götüren bir bağlantıdır; örneğin gecikmiş alacak sayısına basınca gecikmiş alacaklar listesi açılır.
- Seçtiğiniz tarih aralığından etkilenmeyen kartlar bunu alt metinlerinde yazar ("bugün itibarıyla").
- Tutarlar para birimiyle birlikte gösterilir; farklı para birimleri toplanmaz.

## Satış {#islemler}

<!-- ekran: /islemler -->

İşletmedeki alışverişin tamamı bu ekrandan yapılır: satış, alış, hurda alışı, takas, işçilik, iade ve şubeler arası transfer. Her işlem o anki kura bağlanarak kaydedilir.

### Satış ve otomatik fiyat {#satis}

1. **Satış** sekmesini seçin.
2. Müşteriyi (cariyi) seçin. Kayıtlı olmayan, peşin ödeyen müşteri için "Peşin Müşteri" kullanılabilir. Müşteri aramasına **TC kimlik ya da vergi numarası** yazarsanız o numarayla kayıtlı müşteri kendiliğinden seçilir. Kayıt yoksa adını yazıp **Müşteriyi aç ve seç** düğmesine basın; müşteri bu numarayla kaydedilir ve seçilir (tezgâhtar da açabilir). Numara geçersizse ekran uyarır.
3. Ürünü barkodu, kodu ya da adıyla bulun. Birim fiyat kendiliğinden dolar: ürünün sabit satış fiyatı varsa o, yoksa has gram güncel kurla çarpılır ve işçilik eklenir.
4. Adedi girip kalemi ekleyin; birden çok kalem ekleyebilirsiniz. Toplam altta görünür.
5. Ödeme türünü ve kasayı seçin. Ödemeyi birden çok parçaya bölebilirsiniz (bir kısmı nakit, bir kısmı kart gibi).
6. **Kaydet**'e basın. Başarı bandında fiş numarası yazar; stok düşer, kasa ve cari bakiyeleri güncellenir.

**Fatura:** satış fişi her satışta kendiliğinden oluşur; ayrıca seçilmez. Müşteri seçiliyse **Kaydet**'e basınca program **Fatura kesilsin mi?** diye sorar:

- **Evet:** satış ile birlikte fatura kesilir; fatura numarası başarı bandında yazar.
- **Hayır:** yalnız satış fişi oluşur; bantta "Fatura kesilmedi" yazar.
- Satış kuyumcu fatura sınırını aşıyorsa ve **Hayır** derseniz program **sınırı aştınız, emin misiniz?** diye uyarır. Uyarıda **Evet, fatura kes** derseniz fatura kesilir; **Geri dön** ile satışa dönersiniz. Sınırın üstünde faturasız satış kaydedilmez.
- Peşin Müşteri'ye fatura kesilmez, bu yüzden soru sorulmaz. Sınırın üstündeki satışta müşteri seçmeden **Kaydet** açılmaz.
- İşletme Ayarları'ndaki **Satışta fatura önerilsin** açıksa soruda **Evet** öne çıkar; kapalıysa **Hayır** öne çıkar.

> [!IPUCU] Tek kasanız varsa kasa seçici hiç görünmez; kasa kendiliğinden kullanılır. Kart ile ödemede POS cihazı ve komisyon alanı açılır.

**Fişi bekletme:** müşteri karar verirken sıradaki müşteriye geçmek için **Beklet**'e basın; fiş kaydedilmeden kenara alınır, tezgâh boşalır. Bekletme stok, kasa ve cariye hiçbir şey yazmaz. Fiş boşken **Bekleyen fişler (sayı)** düğmesi görünür; listeden **Al** ile fiş kalemleri ve müşterisiyle tezgâha geri gelir (başka bir kasadan da alınabilir), **Sil** ile atılır. Geri alınan fiş normal **Kaydet** ile kaydedilir.

**Fiyat teklifi:** müşteri "düşüneyim" derse **Fiyat teklifi ver**'e basın. Önce kur kilitlenir (teklif, kilit süresi boyunca geçerlidir; tezgâhtar en çok 1 saat, sahip/müdür 30 güne kadar); program numaralı **Fiyat teklifi** dökümünü açar, müşteriye verilir. Teklif satış değildir, stok ayrılmaz. Müşteri dönünce bekleyen fişlerden **Al** ile tezgâha alınır; aynı kilitli kur kullanılır. Süresi dolan teklif tezgâha alınmaz; yeni teklif verilir.

**Fişte bakiye:** kayıtlı müşterinin satış fişinde ve tahsilat makbuzunda **Önceki bakiye → Yeni bakiye** satırı yazar (TL; has varsa has). Peşin Müşteri fişinde yazmaz.

**Kısayol tuşları (Profesyonel mod):** fiş kartının başlığındaki **Kısayollar** satırı açılınca listelenir.

| Tuş | Ne yapar |
|---|---|
| **F2** | Ürün / barkod kutusuna gider ve ürün listesini açar |
| **F4** | Müşteri kutusuna gider |
| **F8** | Ödeme türüne gider (parçalı ödemede "Tek ödemeye dön") |
| **F9** | Kaydet (fiş kilitliyse hiçbir şey göndermez; kilit sebebi düğmenin altında yazar) |
| **Ctrl K** | Üst çubuktaki aramaya gider |

### Alış, hurda alışı ve işçilik {#alis}

- **Alış:** müşteriden eski altın alımı. Ayar ve gram girilir, tutar has fiyatından hesaplanır. Tedarikçiden gelen ürünü stoğa almak için **Satın alma** ekranındaki **Mal geldi** düğmesini kullanın (bkz. [Satın alma](#satinalma)).
- **Hurda alışı:** müşteriden hurda altın alımı. Ayar ve gram girilir; hurda has gram olarak deftere ve hurda stoğuna işlenir. Vergi mükellefi olmayan kişiden alımda gider pusulası kendiliğinden düzenlenir (bkz. [Gider Pusulaları](#giderpusulasi)). Kaydettikten sonra **Fotoğraf ekle** ile hurdanın fotoğrafı eklenir; telefonda kamera açılır.
- **İşçilik:** yalnız işçilik bedeli tahsil edilir, stok hareketi olmaz.

### Takas {#takas}

Müşteriden eski ürünü alıp yeni ürün verdiğiniz işlemdir. Eski ürünün ayarı ve gramı ile yeni ürün girilir; program farkı hesaplar. Fark müşterinin aleyhineyse müşteri öder, lehineyse fark müşteriye ödenir ya da carisine yazılır.

### Transfer {#transfer}

Şubeler arası stok aktarımıdır. Kaynak şube, hedef şube, ürün ve adet seçilir. Kasa ve cari gerekmez; Müdür ve üstü roller yapabilir.

### Havaleden satış

Müşteri ürünün parasını hesabınıza havale ya da EFT ile gönderdiyse satışı bu paradan yaparsınız. Fatura kendiliğinden kesilir, satış fişi de arka planda oluşur.

- **Gelen havaleler:** satış ekranında, ödeme satırının altında **Gelen havaleler (sayı)** düğmesi görünür. Bu düğme yalnız, son 30 gün içinde gelmiş ve henüz kullanılmamış havale varsa çıkar. Düğmeye basınca liste açılır; her satırda gönderen, tutar ve tarih yazar (IBAN görünmez). Bu listeyi tezgâhtar da görür.
- **Havaleyi seçin:** müşteri havaleyi gönderen kişi olur, ödeme **Havale** olarak ayarlanır; havaleden satışta fatura her zaman kesilir, soru sorulmaz. Satılan ürünleri ekleyin.
- **Tutar eşit olmalı:** fişin toplamı gelen paraya eşit olmadan Kaydet açılmaz; eşit olmadığında sebebi yazar. Toplamı fiyat ya da indirimle eşitleyin. Havaleden satışta başka ödeme aracı eklenmez.
- **Kaydet:** satış, fatura ve paranın bu satışa bağlanması tek seferde yapılır. Para bankada olduğu için kasaya yazılmaz. Havale listeden düşer.
- **Havaleden değil:** havaleyi yanlış seçtiyseniz bu düğmeyle normal satışa dönersiniz; eklediğiniz ürünler kalır.
- Aynı yol **Banka › Karar bekleyen gelen ödemeler** kutusundaki **Ürün seç ve sat** düğmesiyle de açılır.
- Havaleden yapılan satış yanlışsa **satış iadesi** yapın. Para iadesi ayrı bir ödeme olarak yazılır; havale satışta kullanılmış sayılır ve dekontu stornolanamaz.

### Yanlış işlemi düzeltme

Kaydedilmiş bir işlem silinmez ve üzerine yazılmaz. Yanlış işlemi **Storno** ile ters kayıtla geri alır, doğrusunu yeniden girersiniz. Böylece hem hata hem düzeltmesi kayıtta görünür kalır. İade için işlemler listesinde ilgili satışı açıp **İade** yolunu kullanın.

## Emanet {#emanet}

<!-- ekran: /emanet -->

Müşterinin tamir, ölçü ya da saklama için size bıraktığı altın burada izlenir. Emanet mal sizin malınız değildir: stok değerine katılmaz, yalnız has gram olarak ayrı izlenir.

1. **Emanet girişi:** müşteriyi seçin, gramı ve ayarı girin. Kayıt has grama çevrilerek tutulur.
2. **Emanet çıkışı:** mal müşteriye geri verildiğinde çıkış kaydı girilir; teslim belgesi basılabilir.

> [!BILGI] Yanlış girilen emanet hareketi silinmez; Storno ile ters kaydı atılır ve doğru kayıt yeniden girilir.

## Sipariş ve tamir {#siparisler}

<!-- ekran: /siparisler -->

Müşteri siparişi, dış atölye siparişi ve tamir kayıtları buradan izlenir. Sipariş deftere yazmaz; satış ya da ödeme ayrıca girilir.

- **Durumlar:** Taslak, Onaylandı, Atölyede, Hazır, Teslim edildi. Her geçiş onay penceresinden geçer.
- **Geri alma:** onaylanmış bir siparişi gerekçe yazarak taslağa geri alabilirsiniz; taslakta sipariş yeniden düzenlenir.
- **İptal:** yalnız Sahip ve Müdür iptal edebilir, gerekçe zorunludur.
- **Geciken sipariş:** söz verilen gün geçtiyse ve sipariş hâlâ açıksa listede işaretlenir.

### Sipariş ve tamir — sipariş ayrıntısı {#siparis-ayrinti}

<!-- ekran: /siparisler/{Id} -->

Listeden bir siparişe basınca ayrıntı ekranı açılır. Siparişin kalemleri, durumu, söz verilen günü ve notları bu ekrandan düzenlenir. Durum değişiklikleri ve kimin ne zaman değiştirdiği aynı ekranda listelenir.

## Bildirimler {#bildirimler}

<!-- ekran: /bildirimler -->

Programın ürettiği uyarı ve hatırlatmalar burada toplanır. Bildirimi okuduktan sonra kapatabilirsiniz; kapatılan bildirim silinmez, kayıtta kalır.

> [!BILGI] Program e-posta, SMS ya da WhatsApp göndermez; bildirimler yalnız bu ekranda görünür (bkz. [Bu sürümde olmayanlar](#haric)).

<!-- grup: Müşteri ve Stok -->

## Müşteriler {#cariler}

<!-- ekran: /cariler -->

Müşteri, tedarikçi ve atölye hesaplarının tutulduğu ekrandır. Her cari için TL bakiyesi ve has gram bakiyesi ayrı görünür.

### Yeni cari ekleme

1. **Yeni cari** düğmesine basın.
2. Adı, türü (müşteri, tedarikçi ya da ikisi), telefonu ve adresi girin. Vergi numarası ya da kimlik bilgisi gereken işlemlerde program sizden ayrıca ister.
3. **Kaydet**'e basın; cari listeye eklenir.

### Cari ayrıntısı, ekstre ve tahsilat

Listedeki bir cariye basınca bütün hareketleri TL ve has gram olarak listelenir. Buradan tahsilat alabilir, cari ekstresini ve **Mutabakat formu**nu yazdırabilirsiniz. Mutabakat formunda bugünkü TL, has ve döviz bakiyesi "bize borçlu / size borçluyuz" diye yazar; müşteri "mutabıkız" ya da "mutabık değiliz" kutusunu işaretleyip imzalar. Kartın **KVKK** bölümünde aydınlatmanın yapıldığı ve pazarlama izni kaydedilir (bkz. [KVKK](#kvkk)). Cari bilgileri satırdaki **Düzenle** ile değiştirilir; kullanılmayan cari silinmez, **Pasife al** ile kapatılır ve istenirse geri açılır.

### IBAN listesi {#cari-iban}

Müşterinin kartında **IBAN'lar** bölümü vardır; bir müşteri birden çok hesaptan para gönderebilir. **+ IBAN ekle** ile yeni IBAN yazılır, **Kaldır** ile bağ kaldırılır (geçmiş ödemeler etkilenmez). Listedeki bir IBAN'dan para gelince ödeme bu müşteriye kendiliğinden bağlanır. Bir IBAN yalnız bir müşteride kayıtlı olabilir; başka müşterideyse program kimde olduğunu söyler. Bankadan öğrenilen IBAN'ların yanında "bankadan" yazar.

## Ürünler {#urunler}

<!-- ekran: /urunler -->

Stoktaki ürün kartları. Her ürünün ayarı, gramı, taş ağırlığı, has gramı ve reyonu görünür. Has gram program tarafından hesaplanır.

1. **Yeni ürün** ile formu açın; ad, kod, ayar, gram ve taş ağırlığını girin.
2. İsterseniz sabit satış fiyatı girin; boş bırakırsanız satışta has gram, güncel kur ve işçilik üzerinden hesaplanır.
3. **Kaydet**'e basın. Ürün kartı satırdaki **Düzenle** ile değiştirilir, satılmayacak ürün **Pasife al** ile kapatılır.

> [!IPUCU] Liste ekranda bir sınırla gösterilir; sınıra ulaşıldığında ekran "daha fazlası var" yazar. Aramayı ya da kategori süzgecini kullanarak listeyi daraltın.

### Ürün Ağacı {#urun-agaci}

<!-- ekran: /urunler/agac -->

Kategori ağacı ürün listesini kırılımlı süzmenizi sağlar (örneğin Bilezik altında Burma, Hasır). **Reyon** ise malın işletmede fiziken nerede durduğunu söyler (vitrin, kasa altı, kasa). Kategori ve reyonlar bu ekrandan eklenir, adları değiştirilir, kullanılmayanlar pasife alınır; her satırın yanında kaç ürünün o kategoride olduğu yazar.

### Ürün Etiketleri {#etiketler}

<!-- ekran: /urunler/etiketler -->

Ürünler için barkodlu etiket basılır. Basılacak ürünleri seçin, etiket sayısını girin ve yazdırın. Laboratuvar üretimi (sentetik) taş taşıyan ürünlerde bu ibare etikete kendiliğinden gelir.

## Fiyat Listeleri {#fiyatlandirma}

<!-- ekran: /fiyatlandirma -->

Fiyat listeleri ve kampanyalar burada tanımlanır. Bir listeyi kaydetmeden önce hangi ürünlerin etkileneceğini ve hesaplanan fiyatı önizlersiniz; kaydettikten sonra etkilenen ürün sayısı aynı ekranda görünür.

- Her liste ve kampanya satırı düzenlenebilir, pasife alınabilir ve geri açılabilir.
- Carinin bağlı olduğu liste satış ekranında fiyatı belirler. Sepette ürün varken müşteriyi değiştirirseniz ve yeni müşterinin listesine göre fiyat farklıysa satış ekranı kaç kalemin fiyatının değiştiğini yazar ve sorar: **Fiyatları güncelle** derseniz sepetteki fiyatlar yeni listeye göre değişir; **Eski fiyatlar kalsın** derseniz ya da hiçbir şeye basmazsanız eski fiyatlar aynen kalır. Fiyat hiçbir zaman sessizce değişmez. Elle yazdığınız fiyatlar ve hediye kalemler bu soruya girmez.

## Sayım {#sayim}

<!-- ekran: /sayim -->

Stok sayımı, kayıttaki adet ile rafta duran adedi karşılaştırır ve farkı sayım düzeltmesi olarak deftere yazar.

1. **Oturum açma:** sayıma bir ad verin, şube ve kapsam seçin.
2. **Sayma:** ürünü barkodu, kodu ya da adıyla arayın, sayılan adedi satırın üzerinde girin ya da düzeltin. Fark sütunu kendiliğinden hesaplanır.
3. **Kapanış:** oturum kapanınca farklar deftere işlenir. Kapanış geri alınamaz; bu yüzden önce onay istenir.

> [!BILGI] Oturumun adı, kapsamı ve şubesi açıldıktan sonra değiştirilemez, çünkü sayılacak satırları belirler. Yanlış açtıysanız oturumu gerekçeyle iptal edip yenisini açın.

## Satın alma {#satinalma}

<!-- ekran: /satin-alma -->

Tedarikçiden gelen malı stoğa almak ve tedarikçiye sipariş vermek buradan yapılır. Ekranı işletme sahibi, müdür ve muhasebe görür; stoğa almayı ve sipariş açmayı yalnız sahip ve müdür yapar.

**Mal geldi (tek adım):** mal elinize ulaştıysa en kısa yol budur.

1. **Mal geldi** düğmesine basın.
2. Tedarikçiyi seçin; her satıra ürünü, adedi ve birim alış fiyatını yazın. Birden çok ürün için **+ Satır ekle** kullanın.
3. **Stoğa al**'a basın. Mal stoğa girer, tutar tedarikçinin hesabına borç yazılır ve irsaliye kendiliğinden açılır; irsaliye numarası bantta yazar.

Tutar MASAK kimlik eşiğini aşıyorsa ilk **Stoğa al**'da tedarikçinin kimlik beyanı formu açılır; doldurup yeniden **Stoğa al**'a basın. Kayıt herhangi bir nedenle reddedilirse stoğa hiçbir şey girmez ve sebep formun altında yazar.

**Siparişle (üç adım):** malı önceden sipariş edip sonra teslim almak isterseniz:

1. **Sipariş:** **+ Yeni sipariş** ile tedarikçiyi ve satırları (ürün, adet, birim fiyat) girin. Sipariş taslak olarak açılır ve düzenlenebilir.
2. **Onay:** tedarikçiye verdiğiniz siparişi **Onayla** ile onaylayın. Onaylı siparişin satırları artık değişmez.
3. **Malı teslim alma:** mal geldiğinde siparişi açıp her satıra gelen adedi yazın ve **Malı teslim al** deyin. Mal stoğa girer, tedarikçiye borç yazılır ve irsaliye kendiliğinden açılır. Mal parça parça gelebilir; hepsi gelince sipariş kapanır.

> [!BILGI] Teslim alınmış mal deftere yazıldığı için sipariş artık iptal edilemez; yanlış gelen mal için alış iadesi yapın. Kapanan ve iptal edilen siparişler ekranın altında kapalı listede durur.

<!-- grup: Para ve Banka -->

## Kasalar {#kasalar}

<!-- ekran: /kasalar -->

Nakit ve döviz kasalarınız. Her kasanın para birimi ve bakiyesi ayrı tutulur; farklı para birimleri tek toplamda birleştirilmez.

- **Yeni kasa:** kasa adı ve para birimi girilerek açılır.
- **Hareket girme:** kasaya elle gelir, gider, tahsilat ya da ödeme kaydı eklenir.
- **Düzenleme:** kasa adı ve bilgileri satırdaki **Düzenle** ile değiştirilir. Kullanılmayan kasa pasife alınır, gerekirse geri açılır.

> [!BILGI] Kaydedilmiş kasa hareketi silinmez; yanlış hareket ters kayıtla düzeltilir.

## Kasa kuyruğu (vezne) {#vezne}

<!-- ekran: /vezne -->

Vezne, tezgâh ile kasanın ayrıldığı işletmeler içindir. Tezgâhtar müşteriyle fişi hazırlar, parayı kasadaki kişi tahsil eder.

- **Hazırlanıyor:** fiş açıktır; kalemler ve cari serbestçe değiştirilebilir.
- **Veznede:** fiş kasaya gönderilmiş ve tutarı damgalanmıştır; kalemler artık yerinde değiştirilmez.
- **Yanlış fiş:** fişi gerekçe yazarak iptal edin ve doğru kalemlerle yeni fiş açın. İptal izi kayıtta kalır.

## POS Cihazları {#pos}

<!-- ekran: /pos -->

Kredi kartıyla tahsil edilen para hemen kasaya girmez: banka komisyonunu keser ve belirli bir gün sonra hesabınıza yatırır. Bu ekran POS cihazlarınızı tanımlar.

1. **Cihaz kurma:** cihaz adı, banka, komisyon oranı, kaç gün sonra yatırılacağı (valör günü) ve bağlı kasa girilir. Şube, seçtiğiniz kasadan kendiliğinden gelir.
2. **Bağlı kasayı değiştirme:** cihazda henüz tahsilat yoksa doğrudan değiştirilir. Tahsilat yazıldıysa değiştirilemez; cihazı pasife alıp doğru kasaya bağlı yeni bir cihaz kurun.

> [!UYARI] Programda resmî tatil takvimi yoktur: valör günü takvim günü olarak sayılır. Bayramda banka parayı geç yatırabilir.

### POS alacakları {#pos-hakedis}

<!-- ekran: /pos/tahsilatlar -->

Kartla yapılan her satıştan doğan ve bankanın size yatıracağı alacaklar burada izlenir. Satırlar satıştan türetilir ve elle değiştirilmez. Banka bir işleme itiraz ederse satır gerekçeyle iptal edilir ya da ilgili satışın iadesi girilir.

## Banka {#banka}

<!-- ekran: /banka -->

Banka hesaplarınızı ve dekont hareketlerini izlediğiniz ekrandır.

- **Hesap ekleme:** banka adı, hesap adı, hesap sahibi, IBAN ve para birimi girilir. Hesap bilgileri **Düzenle** ile değiştirilir; kapanan hesap pasife alınır ve geri açılabilir.
- **Dekont girme:** gelen ya da giden hareket elle girilir. Gelen parada **gönderenin IBAN'ı** ya da **TC / vergi numarası** yazılırsa program müşteriyi kendisi bulur: önce TC / vergi numarasına, sonra kayıtlı IBAN'a bakar. Bulamazsa ve TC ya da IBAN varsa müşteriyi kendisi kaydeder. Yalnız ad yazılmışsa müşteri açılmaz; adı benzeyen müşteri varsa "bu kişi olabilir" diye söylenir, bağlamayı siz yaparsınız (iki "Mehmet Yılmaz" olabilir).
- **Ekstre yükleme:** bankadan aldığınız ekstre dosyası yüklenir ve satırlar eşleştirme ekranına gelir.
- **Gelen parayı faturaya dönüştürme:** gelen bir hareketin satırındaki düğmeyle, faturanın kesileceği cari seçilerek fatura oluşturulur.

### Karar bekleyen gelen ödemeler {#banka-karar}

Müşteriye bağlanmış gelen para ekranın üstünde **Karar bekleyen gelen ödemeler** kutusuna düşer. Program parayı kendiliğinden borçtan düşmez; karar sizindir (müşteri borcunu mu ödüyor, yeni bir şey mi alıyor?).

- **Borcu olan satışlar:** müşterinin borcu kalan satışları listelenir; vadesi en eski olan üstte ve gelen tutar önerilen şekilde işaretli gelir. Müşteri "şu borcumu ödüyorum" dediyse işaretleri ve tutarları değiştirin. Seçtiğiniz satış önce kapanır, artan para en eski borca gider.
- **Borcundan düş:** para müşterinin borcundan düşülür. Kısmi ödemede kalan borç açık kalır. Gelen para borçtan fazlaysa fazlası **müşterinin hesabında** alacak olarak durur.
- **Ürün seç ve sat:** para yeni bir alışverişin ödemesiyse satış ekranı bu havaleyle açılır (bkz. **Havaleden satış**). Fatura kendiliğinden kesilir.
- **Satış zaten girildi:** satışı önceden girdiyseniz para o alışverişin ödemesi sayılır; borca dokunulmaz.
- **Geri al:** verdiğiniz kararı geri alır; borç eski haline döner, ödeme yeniden listeye düşer. Kayıt silinmez, ters kayıtla geri alınır.
- **Faturası yok — 7 gün geçti:** faturası kesilmemiş ve üzerinden 7 günden fazla geçmiş satışta bu uyarı görünür. Borçtan düşmek fatura kesmez; faturayı Faturalar ekranından kesin (fatura satıştan itibaren 7 gün içinde kesilir).
- **Altın borcu:** müşterinin gram cinsinden borcu bilgi olarak yazar; bu kutudan düşülmez.
- Çek ve senet tahsilinden bankaya gelen para bu kutuya düşmez: o borç evrak alınırken zaten kapanmıştır.

### Banka ekstresi eşleştirme {#banka-ekstre}

<!-- ekran: /banka/ekstre/{IthalatId} -->

Yüklenen ekstrenin satırlarını gözden geçirir, her satırı ilgili cariye bağlar ve seçtiklerinizi banka hareketine çevirirsiniz. Çevrilen satır sayısı ve atlanan satırlar sonuç bandında yazar; aynı satır iki kez harekete çevrilmez.

## Çek ve senet {#evraklar}

<!-- ekran: /evraklar -->

Müşteriden alınan çek ve senetler burada tutulur. Evrak girişi müşterinin borcunu kapatır; evrak karşılıksız çıkarsa borç geri doğar. Evrakın durumu (portföyde, tahsil edildi, ciro edildi, karşılıksız) onay penceresiyle değiştirilir.

### Vade Takvimi {#vade-takvimi}

<!-- ekran: /evraklar/takvim -->

Hangi gün ne kadar evrak vadesi olduğunu takvim üzerinde gösterir. Bir güne basınca o günün evrakları listelenir.

## Gider ve Para Aktarımı {#gider}

<!-- ekran: /gider -->

İşletmenin masrafları (kira, elektrik, maaş gibi) ve kasa ile banka arasındaki para aktarımları buradan girilir. (Eski adı: virman.)

- **Gider:** tutar, gider türü ve ödendiği kasa ya da banka seçilir.
- **Virman:** para bir kasadan ya da bankadan ötekine aktarılır. Virman ciroya girmez: para işletmeden çıkmaz, yer değiştirir. İki taraf aynı anda kaydedilir; biri yazılıp öteki eksik kalmaz.

## Hurda ve Ramat {#hurda}

<!-- ekran: /hurda -->

İşletmede biriken hurdanın has bakiyesi ve rafineriye (ramatçıya) çıkışı burada izlenir. Bakiye ayrı bir sayıdan değil, hurda alış ve hurda satış kayıtlarından hesaplanır; bu yüzden elle değiştirilmez. Rafineriden gelen tahlil sonucu beklenenden farklıysa fark ayrıca kaydedilir. Hurda alımı satırındaki **Fotoğraf** ile alımın fotoğrafı eklenir ya da görülür.

<!-- grup: Belge ve Uyum -->

## Faturalar {#faturalar}

<!-- ekran: /faturalar -->

Kesilen ve taslak faturalar burada listelenir.

- **Durum akışı:** taslak fatura düzenlenebilir; kesilen fatura yerinde değiştirilmez. Durum geçişleri (kes, ödendi işaretle, iptal et) onay ister.
- **Kuyum faturalarında KDV:** kuyum ürünlerinde KDV'nin hangi tutar üzerinden hesaplanacağı programın ayarlarında tanımlıdır. Bu kuralın işletmenize uygunluğunu mali müşavirinize teyit ettirin; kılavuz oran ya da kural yazmaz.
- **Yanlış fatura:** kesilmiş fatura silinmez; iptal edilip doğrusu kesilir.
- **Fatura sınırı:** kuyumcu fatura sınırının (2026'da 36.000 TL) üstündeki satış fişle yapılamaz, faturalı kesilir ve müşterinin geçerli TC kimlik ya da vergi numarası gerekir. Fatura kesilemezse satış da kaydedilmez; sebep ekranda yazar.
- **Kuyum faturası:** yalnız satış kaydından ya da has gramı bilinen kalemlerden kesilir; has bedeli elle yazılamaz.
- **Alıcı bilgisi:** fatura kesildiği anda müşterinin adı/unvanı, kimlik numarası ve adresi faturaya yazılır; müşteri kartı sonradan düzeltilse de kesilmiş fatura değişmez.
- **Yazdırma:** satırdaki **Yazdır** faturayı açar; üstte **A4** ya da **80 mm fiş** seçilir (fiş yazıcısı için). Kâğıtta "bilgi amaçlı döküm" uyarısı yazar; taslak ve iptal edilmiş faturada bu durum da basılır.

- **e-Fatura giden kutusu:** üstteki **e-Fatura giden kutusu** düğmesi kesilen faturaların Gelir İdaresi yolculuğunu gösterir (bkz. [e-Fatura giden kutusu](#ebelgeler)).

## e-Fatura giden kutusu {#ebelgeler}

<!-- ekran: /e-belgeler -->

Kesilen faturaların e-Fatura / e-Arşiv gönderim durumu burada izlenir. Gönderim arka planda yapılır; satış ekranında beklenmez.

- **Durumlar:** sırada · gönderildi · onaylandı · gönderilemedi · reddedildi · iptal. Üstteki sayaçlara dokununca liste o duruma süzülür.
- **Gönderilemedi:** sebep satırda yazar; **Yeniden dene** aynı belgeyi (aynı belge kimliğiyle) tekrar gönderir, mükerrer fatura oluşmaz.
- **PDF / XML:** gönderilmiş belgenin entegratördeki resmi kopyası indirilir; kağıt dökümden farklı olarak bu kopya resmî belgedir.
- **Olay izi:** her satırın geçmişi (kim, ne zaman, hangi duruma) açılır; bu iz değiştirilemez ve silinmez.
- **Kurulum:** entegratör anahtarı Ayarlar › E-belge bölümünden girilir; anahtar bir daha ekranda görünmez.

> [!BILGI] Gönderim bir e-belge entegratörü üzerinden yapılır; işletme entegratörle kendisi sözleşme yapar. Hangi faturanın e-Arşiv, hangisinin e-Fatura olacağına alıcının Gelir İdaresi kaydına göre program karar verir.

## İrsaliyeler {#irsaliyeler}

<!-- ekran: /irsaliyeler -->

Gönderdiğiniz malın sevk irsaliyesi ve satın almada teslim aldığınız malın irsaliyesi burada listelenir. Teslim alınan malın irsaliyesi Satın alma ekranından kendiliğinden açılır; sevk irsaliyesini **+ Sevk irsaliyesi** ile siz açarsınız.

- **Fatura bağı:** faturası kesilen irsaliyeyi **Faturaya bağla** ile bağlayın; faturası bekleyenlerin sayısı ekranın üstünde yazar.
- **Yazdırma:** **Yazdır** irsaliyenin kâğıt dökümünü açar. İrsaliyede fiyat yazmaz; yalnız mal ve miktar yazar.
- **Düzeltme:** irsaliye silinmez; gerekçeyle iptal edilir. Malı stoğa girmiş irsaliye iptal edilmez, yanlış mal için alış iadesi yapılır.

> [!BILGI] Kâğıt döküm bilgi amaçlıdır; yasal sevk irsaliyesi ya da e-İrsaliye yerine geçmez. Hangi durumda hangi belgeyi düzenlemeniz gerektiğini mali müşavirinize sorun.

## Gider Pusulaları {#giderpusulasi}

<!-- ekran: /gider-pusulalari -->

Vergi mükellefi olmayan kişiden (vatandaştan) hurda altın aldığınızda düzenlenen belgeler burada listelenir. Program pusulayı hurda alışıyla birlikte kendiliğinden üretir.

- **Düzeltilebilen alanlar:** satıcının adresi ve malın açıklaması. İkisi de boş bırakılamaz.
- **Değişmeyen alanlar:** pusula numarası, satıcının adı ve kimlik numarası, ayar, gram, has, tutarlar ve tarih. Bunlar belgenin mali kimliğidir.
- **Hatalı belge:** gerekçeyle iptal edin ve doğru bilgilerle yeni bir hurda alışı girin; yeni pusula kendiliğinden düzenlenir.

> [!UYARI] Pusuladaki kesinti oranı ve düzenleme süresi gibi kurallar mevzuata bağlıdır ve programın ayarından gelir. Kural sorusu için mali müşavirinize danışın.

### Gider Pusulası Belgesi {#pusula-belgesi}

<!-- ekran: /gider-pusulalari/{Id}/belge -->

Listeden bir pusulanın **Belge** bağlantısına basınca yazdırılabilir belge açılır. Tarayıcının yazdır komutuyla basılır; belgede işletmenizin kimlik bilgileri **İşletme** ekranından gelir.

## Uyum (MASAK) {#uyum}

<!-- ekran: /uyum -->

Yasal yükümlülüklerin izlendiği ekrandır: kimlik tespitleri, şüpheli işlem bildirimleri ve kayıtların saklama süresi.

- **Kimlik tespiti:** yapılandırmadaki eşiği aşan işlemlerde program müşterinin kimlik bilgisini ister ve kaydeder.
- **Belgelendirme uyarısı:** eşiği aşan tahsilat ve ödemelerde program uyarır ve ödemenin banka ya da kart gibi belgeli bir yolla yapılmasını ister.
- **Şüpheli işlem bildirimi:** kaydedildikten sonra tutarı ve gerekçesi değiştirilemez; gönderilmemiş bildirim kapatılabilir.

> [!UYARI] Eşik tutarları ve süreler mevzuata bağlıdır ve programın yapılandırmasından okunur; bu kılavuz rakam yazmaz. Kesin kural için mali müşavirinize danışın.

## KVKK {#kvkk}

<!-- ekran: /kvkk -->

Kişisel verilerle ilgili aydınlatma metni ve müşterilerin verdiği izinler burada izlenir. Ekranı Sahip ve Müdür görür.

- **Aydınlatma metni:** hukukçunuzdan aldığınız metni buraya yapıştırıp **Kaydet**'e basın; program metin yazmaz. Her kayıt yeni bir sürüm olur, eski sürüm silinmez. Paragrafları boş satırla ayırın.
- **Müşteri kartında:** **Aydınlatma yapıldı** ile müşteriye metnin okunduğu (hangi sürüm olduğu da) kaydedilir. **İzin verdi** / **İzni geri aldı** ile kampanya ve bilgilendirme iletileri için izin tutulur; geri alma da kayıt olarak eklenir, geçmiş silinmez. Metin değiştiyse kartta "yeniden aydınlatın" yazar.
- **KVKK formu:** müşteri kartındaki bağlantı, işletmenin metnini ve iki işaret kutusunu (okudum · izin veriyorum) imza satırıyla basar.

> [!BILGI] Peşin Müşteri için KVKK kaydı tutulmaz. Metnin içeriği hukuki bir konudur; uygunluğunu hukukçunuza teyit ettirin.

<!-- grup: Dökümler -->

## Yasal sınırlar {#yasal-sinirlar}

<!-- ekran: /yasal-sinirlar -->

Devletin belirlediği tutar sınırlarını ve süreleri (nakitte günlük sınır, kimlik alma tutarı, bildirim ve saklama süreleri) bugünkü değerleriyle gösterir. Ekran salt okunurdur ve yalnız Sahip, Müdür ve Muhasebe rolleri görür. Menüde "Belge ve uyum" grubundadır, "Tüm ekranlar"dan ve Panel'deki yasal bilgi bandından da açılır.

- **Bugünkü değer ve başlangıç:** her sınırın bugün geçerli rakamı ve ne zamandan beri geçerli olduğu. Başlangıç tarihi resmî kaynaktan doğrulanmamışsa tarih yazılmaz, "doğrulanmadı" yazar.
- **Doğrulandı / Mali müşavir teyidi bekliyor / Henüz kullanılmıyor:** rakamın resmî kaynağı okunmuşsa "Doğrulandı" yazar. Kaynağı bulunamayan rakam bugünkü davranış korunarak tutulur ve teyit bekler; programın henüz denetlemediği rakamlar (ör. fatura sınırı) "Henüz kullanılmayan sınırlar" bölümünde bilgi için durur.
- **Dayanak:** kapalı bölümdedir; açınca resmî metnin adı (ör. 459 sıra no'lu VUK Genel Tebliği tevsik haddi, MASAK Tedbirler Yönetmeliği) ve kaynağa bağlantı çıkar.
- **Geçmiş değerler:** bir sınırın önceki rakamları ve geçerli olduğu tarihler (her aralığın son günü dahil yazılır: "1 Ocak 2016 – 29 Kasım 2024"). Geçmiş bir işlem her zaman kendi günündeki rakamla değerlendirilir; rakam sonradan değişse de eski işlemin sonucu değişmez.

> [!BILGI] Rakamlar bu ekrandan değiştirilemez: yanlış giriş cezaya yol açabilir. Yeni bir rakamı program sağlayıcısı resmî kaynağıyla ileri bir tarihten başlayacak şekilde girer; günü gelince kendiliğinden uygulanır, yeniden başlatma gerekmez. Değişiklikten yedi gün önce Panel'de mavi bir bilgi bandı çıkar. Bayram günlerini de program sağlayıcısı girer; girilmemişse "Bu yılın bayram tatilleri" bölümünde yazar (bildirim süreleri bir iki gün erken hesaplanabilir).

## Rapor merkezi {#raporlar}

<!-- ekran: /raporlar -->

İşletmenin durumunu özetleyen rapor merkezidir: aylık özet, gün sonu mutabakatı, cari yaşlandırma ve stok değeri. Dönem seçicisi üsttedir; seçilen dönemden etkilenmeyen kartlar bunu alt metinlerinde yazar. Her raporun kartına basınca ilgili ayrıntılı döküm açılır.

## Kâr-Zarar {#kar-zarar}

<!-- ekran: /raporlar/kar-zarar -->

TL kârını ve has kârını ayrı ayrı gösterir. Altın yükselirken TL'de kâr yazıp gramda zarar etmek mümkündür; bu ekran ikisini birden gösterdiği için bunu görürsünüz. Maliyeti bilinmeyen ürün varsa sayısı yazar ve o ürünlere giden bağlantı olur.

## Vade takibi {#vade-takibi}

<!-- ekran: /raporlar/vade-takibi -->

Vadesi geçen ve yaklaşan müşteri borçlarını listeler. Her satırdan müşterinin cari hesabına gidilir; tahsilat oradan alınır.

## İşlem defteri {#islem-defteri}

<!-- ekran: /raporlar/islem-defteri -->

Seçilen dönemdeki bütün işlemleri satır satır döker: tarih, işlem türü, cari, TL ve has tutarlar. Süzgeçlerle daraltılır. İptal edilen ya da storno ile düzeltilen işlemler dökümde işaretli görünür.

## Satış Analizi {#satis-analizi}

<!-- ekran: /raporlar/satis-analizi -->

Seçilen dönemdeki satışları özetler ve karşılaştırır; hangi ürünlerin ve hangi günlerin öne çıktığını grafik ve tabloyla gösterir.

## Kasa raporu {#kasa-raporu}

<!-- ekran: /raporlar/kasa -->

Seçilen kasanın seçilen dönemdeki bütün giriş ve çıkışlarını, açılış ve kapanış bakiyesiyle birlikte gösterir. Gün sonu kasa sayımında elinizdeki parayla karşılaştırmak için kullanılır.

## Belge çıktısı {#cikti}

<!-- ekran: /cikti/{Tur}/{Id} -->

Satış fişi, tahsilat makbuzu, emanet teslim belgesi ve cari ekstresi tek bir A4 şablonundan basılır. İlgili ekrandaki **Yazdır** düğmesi bu belge sayfasını açar; tarayıcının yazdır komutuyla basarsınız. Belgedeki işletme bilgileri **İşletme** ekranından gelir.

## Vitrin Panosu {#vitrin}

<!-- ekran: /vitrin -->

Müşteriye dönük alış ve satış fiyatlarını gösteren panodur. Kâr payı yalnız bu ekranın yönetim bölümünde görünür, müşterinin gördüğü panoya inmez.

- **İşçilik ibaresi:** yasa (Fiyat Etiketi Yönetmeliği), altın ve gümüş fiyatlarına işçilik bedelinin dahil olup olmadığının ayrıca yazılmasını ister. Sahip ya da müdür, panonun altındaki **İşçilik dahil** / **İşçilik hariç** düğmelerinden birini seçer; seçilen cümle fiyatların altına yazılır ve sonradan değiştirilebilir. Seçilmemişse tezgâhtar bunu ekranda okur; seçimi müdür yapar.

## İşletme ekranı {#vitrin-ekran}

<!-- ekran: /vitrin/ekran -->

Vitrin panosundaki **İşletme ekranını aç** düğmesi panoyu menüsüz, büyük yazılı tam ekran olarak açar; işletmedeki bir ekrana ya da televizyona açık bırakılır. Fiyatlar dakikada bir kendiliğinden yenilenir ve seçilen işçilik ibaresi fiyatların altında yazar; sayfa yazdırılırsa ibare kâğıtta da çıkar. **Tam ekran** düğmesi tarayıcı çubuklarını da gizler, **Panoya dön** geri getirir. İşçilik ibaresi seçilmeden işletme ekranı fiyat göstermez; düğme kilitli kalır ve sebebi panoda yazar.

<!-- grup: Yönetim -->

## Kullanıcılar {#kullanicilar}

<!-- ekran: /kullanicilar -->

Çalışan hesapları, rolleri ve erişim durumları. Roller yetki sırasıyla: Sahip, Müdür, Muhasebe, Tezgâhtar.

- **Yeni kullanıcı:** ad, e-posta ve rol girilir. Yalnız kendi rolünüzden düşük roller atanabilir.
- **Düzenleme:** rol değiştirme ve şifre sıfırlama her satırdadır.
- **Ayrılan çalışan:** hesabı silinmez, erişimi kapatılır; gerekirse yeniden açılır. Kendi hesabınız üzerinde bu işlemler yapılamaz.

## Yetkiler {#yetkiler}

<!-- ekran: /yetkiler -->

Hangi rolün hangi ekranı görebileceğini belirleyen matristir. Son sözü her zaman sunucu söyler: yetkisi olmayan kullanıcı adresi elle yazsa bile ekranı açamaz.

## İşletme {#isletme}

<!-- ekran: /isletme -->

İşletmenizin kimliği (ünvan, vergi bilgileri, adres), abonelik planı ve şubeleri. Kimlik alanları fiş, fatura ve pusulalarda satıcı bilgisi olarak basılır. Şubeler buradan eklenir, adları değiştirilir, kapanan şube pasife alınır.

**Mali müşavirin hesap kodları:** Ayarlar sekmesindeki **Mali müşavirin hesap kodları** bölümüne satış hesabı (boşsa 600) ve KDV hesabı (boşsa 391) bir kez yazılır; Luca fiş dosyası bu kodlarla çıkar. Müşteri hesap kodu (120.xxx) her müşterinin kartındadır.

## Kurulum {#kurulum}

<!-- ekran: /kurulum -->

İşletmenin ilk gününü buradan kurarsınız: işletme ve şube, kullanıcılar, güncel kur, kasa ve banka açılış bakiyeleri ve müşteri açılış bakiyeleri.

1. Her adımı sırayla doldurun; tamamlanan adım işaretlenir.
2. Açılış bakiyelerini önce **prova** ile deneyin; prova hiçbir şey yazmaz, hatalı satırları gösterir.
3. **Uygula** ile açılış kayıtları deftere "Açılış devri" olarak düşer; para el değiştirmez.
4. Yanlış uygulanan açılış, sihirbazdaki geri alma yoluyla geri alınır.

## Kim ne yaptı {#denetim-izi}

<!-- ekran: /denetim-izi -->

Kim, ne zaman, hangi kaydı gördü ya da değiştirdi — burada listelenir. Bu kayıt silinemez ve değiştirilemez. Kişi, tarih ve kayıt türüne göre süzülür. Yalnız Sahip rolü görür.

## Veri aktarımı {#veriaktarim}

Eski programınızdaki kayıtları içeri almak ve kendi verinizi dışarı almak için iki ekran vardır.

### Veri içe aktar {#veri-ice-aktarim}

<!-- ekran: /veri-ice-aktarim -->

Eski programınızdaki müşteri ve ürün kartlarını şablona yapıştırıp içeri alırsınız.

1. Şablonu indirip doldurun ya da satırları kutuya yapıştırın.
2. **Kuru prova** yapın: hiçbir şey yazılmadan hatalı satırlar tek tek listelenir. Prova temiz değilse uygulamayın.
3. **Uygula** ile kayıtlar oluşur; uygulanan ve atlanan satır sayısı sonuç bandında yazar.
4. Uygulanan aktarımın tamamı tek düğmeyle geri alınabilir.

> [!BILGI] Aktarım mevcut bir kartın üzerine yazmaz; mevcut kartı kendi ekranından düzenleyin. Geçmiş satış ve kasa hareketleri aktarılmaz.

### Veri dışa aktar {#veri-disa-aktarim}

<!-- ekran: /veri-disa-aktarim -->

Verinizin tamamını şifreli bir arşiv olarak indirirsiniz. Her aktarım için gerekçe yazılır ve gerekçe geçmişe işlenir; geçmiş satırı sonradan değiştirilemez, çünkü bir kanıt kaydıdır.

## Yedekler {#yedekler}

<!-- ekran: /yedekler -->

Verinizin düzenli kopyası (yedek) alınır ve kopyanın gerçekten geri yüklenebildiği bir tatbikatla denenir. Bu ekran ikisini de gösterir. Ekran yalnız Sahip rolüne ve profesyonel modda görünür.

- **Son yedek:** zamanı, boyutu ve şifreli olup olmadığı.
- **Doğrulama:** son yedeğin geri yüklenip karşılaştırılarak doğrulanıp doğrulanmadığı. Doğrulanmamış yedek kırmızı görünür.
- **Dış kopya:** yedeğin sunucu dışında ikinci bir kopyasının alınıp alınmadığı. Tek kopya yedek, yedek sayılmaz.
- **Son geri yükleme tatbikatı:** yedeğin ayrı bir veritabanına geri yüklenip her tablonun sağlama toplamıyla karşılaştırıldığı son başarılı denemenin tarihi. Tatbikat en geç otuz günde bir tekrarlanır; son başarılı tatbikat bundan eskiyse ya da hiç yapılmadıysa ekran kırmızı uyarı verir.
- **Elle yedek talebi:** gerekçe yazarak ek bir yedek isteyebilirsiniz. Talep silinmez; vazgeçerseniz gerekçeyle iptal olarak işaretlenir.

> [!UYARI] Yedek anahtarı kaybolursa yedek açılamaz. Anahtar kurulumda üretilir ve sunucu dışında güvenli bir yerde saklanmalıdır; bu iş kurulumu yapan kişinin sorumluluğundadır.

## Ayarlar {#ayarlar}

<!-- ekran: /ayarlar -->

Kendi hesabınızın dili, teması (aydınlık ya da karanlık), arayüz modu (basit ya da profesyonel), şifresi ve iki aşamalı girişi.

- **Dil:** Türkçe ya da İngilizce; seçim kaydedilir.
- **Şifre değiştirme:** mevcut ve yeni şifreyle.
- **İki aşamalı giriş:** telefonunuzdaki doğrulama uygulamasıyla açılır. Telefonunuzu kaybederseniz Sahip rolündeki kullanıcı kurtarma yolunu başlatır.

İşletme yöneticisi için ayrıca işletme ayarları bölümü vardır: bu ayarlar yalnız bu işletme için geçerlidir; boş bırakılan alan kurulum varsayılanına döner.

## Kılavuz {#kilavuz}

<!-- ekran: /kilavuz -->

Menüdeki **Kılavuz** ekranı bu kılavuzu yeni sekmede açar ve programdaki her ekranın bölümüne doğrudan bağlantı verir. Kılavuz ayrı bir sekmede açılır; böylece tezgâhta yarım kalmış bir işlem kılavuza baktınız diye kaybolmaz.

<!-- grup: Ek -->

## Bu sürümde olmayanlar {#haric}

Aşağıdaki liste ürünün kısıt beyanının tek kaynağından (HARİÇ listesi) alınır; kılavuz kendi cümlesini yazmaz.

<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->

## Kavramlar sözlüğü {#sozluk}

| Kavram | Anlamı |
|---|---|
| Paralel defter | Her hareketin TL ve has gram olarak iki ayrı bakiyede tutulması. |
| Has gram | Ürünün içindeki saf altın miktarı: (gram − taş) × milyem / 1000. |
| Milyem | Binde saflık. 22 ayar yaklaşık 916, 14 ayar yaklaşık 585, 24 ayar 1000'dir. |
| Gerçek milyem | Ürünün fiili saflığı; ayarın standart değerinden farklı olabilir. |
| Kur anlık görüntüsü | İşlem anındaki kurun kaydı; her işleme kalıcı bağlanır. |
| Cari | Para ya da altın alıp verdiğiniz kişi veya firma. |
| Storno | Yanlış kaydı silmeden, ters kayıtla geri almak. |
| Pasife alma | Kaydı silmeden kullanımdan kaldırmak; istenince geri açılır. |
| Tatbikat | Yedeğin gerçekten geri yüklenebildiğinin denenmesi. |

## Sık sorulanlar {#sss}

### Kur çubuğundaki rakamlar eski görünüyor, ne yapmalıyım?

Sahip ya da Müdür rolündeyseniz kur çubuğundaki yenileme düğmesiyle kuru hemen çektirin. Kaynağa ulaşılamıyorsa yetkili kullanıcı kuru elle girebilir.

### Satışta birim fiyat neden kendiliğinden doldu?

Ürünün sabit satış fiyatı varsa o kullanılır; yoksa fiyat has gram, güncel kur ve işçilikten hesaplanır. İsterseniz alanı elle değiştirebilirsiniz.

### Bir menüyü göremiyorum.

Menü yalnız rolünüzün yetkili olduğu ekranları gösterir; basit modda bazı ekranlar gizlidir. Profesyonel moda geçin ya da Sahip rolündeki kullanıcıdan yetki isteyin.

### Kaydettiğim bir işlemi nasıl silerim?

Kaydedilmiş işlem ve para hareketi silinmez. Storno ile ters kaydını atın ve doğrusunu girin; kartlar (cari, ürün, kasa) ise pasife alınır. Böylece hiçbir düzeltme iz bırakmadan kaybolmaz.

### Verilerim nerede saklanıyor?

Kurulumda seçilen sunucudaki veritabanında. Yedekleri ve tatbikat sonuçlarını **Yedekler** ekranında görürsünüz.
