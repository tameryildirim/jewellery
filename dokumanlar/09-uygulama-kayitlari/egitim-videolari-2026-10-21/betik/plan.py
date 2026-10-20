#!/usr/bin/env python3
# Y-01 eğitim videoları — ekran başına 1–2 dk; her video: plan-<id>.json. Yazım (ses modeli): şapka yok (tezgah, kagit),
# kesme işareti yok, rakam yok, cümle ≥ 4 kelime. Ton: S sakin · H heyecanlı · V vurgulu. Kaynak: Hasfiyat/Pusula ekran
# başına video modeli (§ 2.17 / 2.2) + KuyumSmart bölüm zaman damgaları (§ 2.18); adım sayısı + kısayol sesle söylenir.
import json, os
H = os.path.dirname(os.path.abspath(__file__))
BARKOD = "K6ED1Z3NA00"   # Altın Çarşı · ZYN-CYR-ESKI · 22 Ayar Eski Çeyrek (stok 5)
V = []
def video(vid, baslik, alt, adimlar): V.append(dict(id=vid, baslik=baslik, alt=alt, adimlar=[dict(id=a[0], baslik=a[1], sorun=a[2], eylemler=a[3], cumleler=a[4]) for a in adimlar]))

video("satis", "Satış", "Barkoddan fişe, dört adım",
[("tezgah", "Tezgah ekranı", "Satış için ayrı program, ayrı defter yok; tek ekran.", [{"t":"git","rota":"/islemler"},{"t":"imlec","x":300,"y":130}],
  [("S","Satış tezgah ekranından yapılır."),("S","Üstte satış, alış, iade, tahsilat ve diğer işler yan yana durur."),("V","Satışa başlamak için barkodu okutmak yeter.")]),
 ("barkod", "Barkodu okut", "Fiyat elle hesaplanmaz; o anki kurdan gelir.", [{"t":"etiket","metin":"Barkod okutuluyor"},{"t":"tik","sel":"#islem-barkod"},{"t":"yaz","metin":BARKOD},{"t":"tus","key":"Enter"},{"t":"bekle","ms":1800},{"t":"etiket","metin":"Ürün ve fiyat satıra geldi"},{"t":"imlec","x":520,"y":360}],
  [("S","Barkodu okutun ya da ürün adını yazın."),("H","Ürün satıra gelir, fiyat o anki kurdan hesaplanır!"),("S","Gram, has ve tutar altta birlikte yazar.")]),
 ("odeme", "Ödeme", "Nakit hazır seçili; kart, havale, indirim bir dokunuş uzakta.", [{"t":"etiket","metin":"Ödeme: Nakit · Ana Kasa"},{"t":"imlec","x":1100,"y":205},{"t":"bekle","ms":1500},{"t":"imlec","x":1070,"y":357},{"t":"bekle","ms":1200},{"t":"imlec","x":1045,"y":409}],
  [("S","Ödeme nakit ve ana kasa hazır seçilidir."),("S","Başka ödeme ile kart ya da havale eklenir; para üstü ve indirim ayrı düğmelerde."),("V","Fark sıfır olunca yasal kurallar satırı uygun der.")]),
 ("kaydet", "Kaydet ve yazdır", "Fiş elle yazılmaz; stok düşer, kasa artar.", [{"t":"etiket","metin":"Kaydet ve yazdır"},{"t":"tik","sel":"#yildiz-tezgah-kaydet"},{"t":"bekle","ms":1500},{"t":"tikVarsa","sel":"#yildiz-belge-hayir"},{"t":"bekle","ms":2500},{"t":"etiket","metin":"Kaydedildi"}],
  [("V","Kaydet ve yazdır: stok düşer, kasa artar, fiş hazır."),("S","Yanlış fiş silinmez; iade ya da geri alma ile düzeltilir.")]),
 ("kisayol", "Kısayollar", "Fare olmadan da satış biter.", [{"t":"etiket","metin":"F2 ürün · F4 müşteri · F8 ödeme · F9 kaydet"},{"t":"imlec","x":700,"y":500}],
  [("S","Klavyeden F iki ürün, F dört müşteri, F sekiz ödeme, F dokuz kaydet."),("S","Kontrol K arama kutusunu açar."),("V","Dört adım: barkod, ödeme, kaydet, fiş.")])])

video("hurda", "Hurda alışı", "Eski altını al, hurda stoğunu izle",
[("alis", "Alış sekmesi", "Eski altın alışı da tezgahta; ayrı ekran aranmaz.", [{"t":"git","rota":"/islemler"},{"t":"etiket","metin":"Alış sekmesi"},{"t":"tik","sel":"#yildiz-tur-HURDA_ALIS"},{"t":"bekle","ms":1500},{"t":"imlec","x":400,"y":300}],
  [("S","Müşteriden eski altın alışı tezgahtaki alış sekmesinden yapılır."),("S","Maden, ayar ve gram yazılır; has karşılığı kendiliğinden hesaplanır."),("V","Alış fiyatı o günkü kurdan gelir, pazarlık payı ayarlardan sınırlanır.")]),
 ("belge", "Belge ve kimlik", "Gider pusulası ve kimlik tespiti unutulmaz.", [{"t":"etiket","metin":"Gider pusulası kendiliğinden"},{"t":"imlec","x":1100,"y":400},{"t":"bekle","ms":1500}],
  [("S","Vatandaştan alışta gider pusulası program tarafından kesilir."),("S","Tutar yasal sınırı aşarsa kimlik tespiti istenir; eksik belgeyle kayıt kapanmaz."),("V","Belge sonradan değil, alış anında hazırdır.")]),
 ("stok", "Hurda ve ramat", "Biriken hurdanın has miktarı her an belli.", [{"t":"git","rota":"/hurda"},{"t":"imlec","x":500,"y":330},{"t":"bekle","ms":1500},{"t":"etiket","metin":"Rafineriye gönder"},{"t":"imlec","x":140,"y":820}],
  [("S","Alınan eski altın hurda stoğunda birikir."),("S","Hurda ekranı toplam has gramı ve fotoğraflı kayıtları gösterir."),("V","Rafineriye gönder düğmesi ramat çıkışını kaydeder; fire hesabı burada tutulur.")])])

video("emanet", "Emanet", "Müşterinin bıraktığı altın, kayıt altında",
[("liste", "Emanet listesi", "Kimin ne bıraktığı kagit pusulada kaybolmaz.", [{"t":"git","rota":"/emanet"},{"t":"imlec","x":500,"y":330},{"t":"bekle","ms":1200},{"t":"etiket","metin":"Sekmeler: Hepsi · Emanette · Geri verildi"},{"t":"tik","sel":"#yildiz-emanet-sekme-emanette"},{"t":"bekle","ms":1200}],
  [("S","Emanet ekranında müşterinin bırakıp sonra alacağı altın kayıtlıdır."),("S","Emanette sekmesi şu an dükkanda duran emanetleri gösterir."),("V","Her satırda müşteri, parça ve has gram yazar.")]),
 ("al", "Emanet al", "Bırakılan parça has gramıyla müşterinin hesabına yazılır.", [{"t":"etiket","metin":"Emanet al"},{"t":"tik","sel":"#yildiz-emanet-al"},{"t":"bekle","ms":1800},{"t":"imlec","x":500,"y":400},{"t":"bekle","ms":1500},{"t":"tus","key":"Escape"}],
  [("S","Emanet al düğmesi müşteri ve parça bilgisini sorar."),("H","Kaydedince emanet müşterinin has hesabına yazılır, makbuzu çıkar!"),("S","Dükkan altını ile müşteri altını hiç karışmaz.")]),
 ("geri", "Geri ver", "İade anında hesap kapanır.", [{"t":"git","rota":"/emanet"},{"t":"bekle","ms":1200},{"t":"etiket","metin":"Geri ver"},{"t":"imlec","x":1250,"y":330},{"t":"bekle","ms":1500}],
  [("S","Müşteri geldiğinde satırdaki geri ver düğmesine basılır."),("V","Emanet kapanır, geçmişte iz olarak kalır; silinmez."),("S","Üç adım: emanet al, listede izle, geri ver.")])])

video("banka", "Banka", "Gelen para, açık hesaba ve satışa bağlanır",
[("hesaplar", "Banka hesapları", "Bakiye ve hareketler tek ekranda.", [{"t":"git","rota":"/banka"},{"t":"imlec","x":500,"y":300},{"t":"bekle","ms":1500}],
  [("S","Banka ekranında hesaplar ve bakiyeleri görünür."),("S","Hesap ekle ile yeni banka hesabı açılır; bağlantı kurulunca hareketler kendiliğinden gelir.")]),
 ("gelen", "Gelen havale kararı", "Gelen para boşta kalmaz; borca ya da satışa yazılır.", [{"t":"etiket","metin":"Gelen havale: Borcundan düş · Ürün seç ve sat"},{"t":"imlec","x":700,"y":420},{"t":"bekle","ms":1800},{"t":"imlec","x":900,"y":420}],
  [("S","Gelen havale listede karar bekler."),("H","Borcundan düş ile açık hesap kapanır; ürün seç ve sat ile havaleden satış yapılır!"),("V","Karar verilen havale bir daha sorulmaz.")]),
 ("hareket", "Hareketler ve ekstre", "Banka ekstresi elle yazılmaz; dosyadan eşleşir.", [{"t":"etiket","metin":"Hareketler"},{"t":"tik","sel":"[id^=yildiz-banka-hareket-]"},{"t":"bekle","ms":1800},{"t":"imlec","x":600,"y":450}],
  [("S","Hareketler düğmesi hesabın dökümünü açar."),("S","Bankadan indirilen ekstre dosyası yüklenir; satırlar müşterilerle eşleştirilir."),("V","Eşleşmeyen satır kırmızı kalır, unutulmaz.")])])

video("fatura", "Faturalar", "Fişten faturaya tek adım, e-belge arkada",
[("liste", "Fatura listesi", "Hangi fatura ödendi, hangisi açık; tek listede.", [{"t":"git","rota":"/faturalar"},{"t":"imlec","x":600,"y":400},{"t":"bekle","ms":1500}],
  [("S","Faturalar ekranında kesilen faturalar durumlarıyla listelenir."),("S","Ödendi işaretle düğmesi tahsilatı faturaya bağlar.")]),
 ("fisten", "Fişten fatura kes", "Satış fişi yeniden yazılmaz; faturaya dönüşür.", [{"t":"etiket","metin":"Fişten Fatura Kes"},{"t":"tik","sel":"#ff-ac"},{"t":"bekle","ms":1800},{"t":"imlec","x":700,"y":400},{"t":"bekle","ms":1500}],
  [("S","Fişten fatura kes, satış fişini seçtirir."),("H","Kalemler, KDV ve müşteri fişten gelir; yeniden yazmak yok!"),("V","Üründe KDV oranı varsa ürünün oranı, yoksa faturada seçilen oran uygulanır.")]),
 ("ebelge", "e-Fatura giden kutusu", "GİB yolculuğu görünür; gönderilemeyen fatura kesildi sayılmaz.", [{"t":"tus","key":"Escape"},{"t":"bekle","ms":800},{"t":"etiket","metin":"e-Fatura giden kutusu"},{"t":"tik","sel":"#fk-ebelgeler"},{"t":"bekle","ms":2200},{"t":"imlec","x":600,"y":350}],
  [("S","e-Fatura giden kutusu her faturanın gönderim durumunu gösterir."),("S","Sırada, gönderildi, onaylandı ya da hata; hata varsa yeniden dene düğmesi."),("V","Entegratör bağlantısı işletme ayarlarından bir kez kurulur.")])])

video("masak", "Uyum ve MASAK", "Kimlik tespiti, şüpheli işlem, nakit sınırı",
[("tespit", "Kimlik tespitleri", "Yasal eşiği aşan işlemde kimlik tespiti kayda girer.", [{"t":"git","rota":"/uyum"},{"t":"imlec","x":500,"y":350},{"t":"bekle","ms":1500}],
  [("S","Uyum ekranı MASAK yükümlülüklerini tek yerde toplar."),("S","Kimlik tespitleri sekmesinde eşiği aşan işlemlerin kayıtları durur."),("V","Belge numarasıyla arama kutusundan bulunur.")]),
 ("supheli", "Şüpheli işlemler", "Şüphe notu yalnız sahibe görünür.", [{"t":"etiket","metin":"Şüpheli İşlemler"},{"t":"tik","sel":"#uy-sekme-supheli"},{"t":"bekle","ms":1500},{"t":"imlec","x":600,"y":400}],
  [("S","Şüpheli işlemler sekmesi bildirim hazırlığını tutar."),("S","Yeni bildirim düğmesiyle kayıt açılır; tezgahtar liste adını görmez, yalnız yönetici onayı gerekir yazar.")]),
 ("nakit", "Nakit sınırı", "Nakit sınırı akılda tutulmaz; program sayar.", [{"t":"etiket","metin":"Nakit sınırı aşımları"},{"t":"tik","sel":"#uy-sekme-nakit"},{"t":"bekle","ms":1500},{"t":"imlec","x":600,"y":400}],
  [("S","Nakit sınırı aşımları sekmesi tevsik sınırını aşan tahsilatları listeler."),("V","Sınır ve süreler yasal sınırlar ekranında yazılıdır; değişince tek yerden güncellenir.")])])

video("kasa-kapanis", "Kasa kapanışı", "Çekmeceyi say, farkı yaz, günü kilitle",
[("kasa", "Kasa ekranı", "Çekmecedeki nakit ile kart ve havale ayrı satırda.", [{"t":"git","rota":"/kasalar"},{"t":"imlec","x":300,"y":300},{"t":"bekle","ms":1500},{"t":"imlec","x":1000,"y":600}],
  [("S","Kasalar ekranı çekmecedeki nakdi büyük rakamla gösterir."),("S","Kart ve havale ayrı satırdadır; sayıma yalnız nakit girer."),("V","Geçmiş düğmesi günün hareketlerini açar.")]),
 ("kapat", "Günü kapat", "Sayılan para ile defter karşılaştırılır.", [{"t":"etiket","metin":"Günü kapat"},{"t":"tik","sel":"#yildiz-kasalar-b1"},{"t":"bekle","ms":1800},{"t":"imlec","x":1200,"y":240},{"t":"bekle","ms":1200},{"t":"etiket","metin":"Sayılan nakit yazılıyor"},{"t":"kopyala","kaynak":"#gun-nakit-defter","hedef":"#gun-sayilan"},{"t":"bekle","ms":1200},{"t":"etiket","metin":"Günü kapat"},{"t":"tik","sel":"#gun-kapat-onayla"},{"t":"bekle","ms":2500}],
  [("S","Günü kapat düğmesi sayım penceresini açar."),("S","Çekmecede sayılan nakit yazılır; program defterdeki nakitle karşılaştırır."),("H","Fark varsa gerekçe zorunludur; kör sayım açıksa beklenen tutar gösterilmez!")]),
 ("fis", "Kapanış fişi", "İmzalı tutanak elde kalır.", [{"t":"bekle","ms":600},{"t":"etiket","metin":"Tüm ayrıntılar"},{"t":"tik","sel":"#yildiz-kasalar-ayrinti"},{"t":"bekle","ms":1200},{"t":"etiket","metin":"Yazdır — Gün kapanışı"},{"t":"imlecSel","sel":"#kasa-kapanis-yazdir"},{"t":"bekle","ms":1500}],
  [("S","Kapanış kaydedilince gün kilitlenir; o güne yeni hareket yazılamaz."),("S","Yazdır bağlantısı gün sonu kasa fişini verir; kapatan ve onaylayan imzalar."),("V","Üç adım: say, yaz, kilitle.")])])

video("etiket", "Etiketler", "Barkodlu etiket elle yazılmaz",
[("sec", "Ürün seç", "Etiket listeden seçilir, elle yazılmaz.", [{"t":"git","rota":"/urunler/etiketler"},{"t":"imlec","x":400,"y":300},{"t":"bekle","ms":1500},{"t":"etiket","metin":"Ürün seçin"},{"t":"tik","sel":"#yildiz-etiket-urun-deger"},{"t":"bekle","ms":900},{"t":"tik","sel":"#yildiz-etiket-ara"},{"t":"yaz","metin":"çeyrek"},{"t":"bekle","ms":1500},{"t":"etiket","metin":"Ekle"},{"t":"tik","sel":"[id^=yildiz-etiket-sec-]"},{"t":"bekle","ms":1200}],
  [("S","Etiketler ekranında ürün seçilir, adet yazılır."),("S","Arama kutusuna ürün adı ya da kodu yazmak yeter."),("V","Başka ürün ekle ile tek seferde birkaç ürün basılır.")]),
 ("sablon", "Şablon ve adet", "Etiket boyu bir kez ayarlanır.", [{"t":"etiket","metin":"Adet ve şablon"},{"t":"sec","sel":"#yildiz-etiket-adet"},{"t":"yaz","metin":"2"},{"t":"bekle","ms":1200},{"t":"imlec","x":700,"y":500}],
  [("S","Adet kutusu kaç etiket basılacağını söyler."),("S","Şablonu düzenle ile etiket boyu ve alanları bir kez ayarlanır; her üründe aynı kalır.")]),
 ("yazdir", "Yazdır", "Zebra yazıcıya da normal yazıcıya da uyar.", [{"t":"etiket","metin":"Yazdır · Zebra (ZPL) indir"},{"t":"imlec","x":600,"y":780},{"t":"bekle","ms":1500}],
  [("S","Yazdır düğmesi etiketi yazıcıya gönderir."),("V","Zebra yazıcı için ZPL dosyası indirilir; iki adım: seç, yazdır.")])])

video("rapor", "Raporlar", "Gün sonu için hesap makinesi gerekmez",
[("merkez", "Rapor merkezi", "Günün özeti ve pozisyon tek bakışta.", [{"t":"git","rota":"/raporlar"},{"t":"imlec","x":500,"y":330},{"t":"bekle","ms":1500},{"t":"kaydir","top":500},{"t":"bekle","ms":1500}],
  [("S","Rapor merkezi günün satışını, kasayı ve altın pozisyonunu özetler."),("S","Aşağıdaki kartlar günlük, aylık, stok ve cari raporlarına götürür.")]),
 ("karzarar", "Kar zarar", "Lira karı ile altın karı ayrı görülür.", [{"t":"git","rota":"/raporlar/kar-zarar"},{"t":"imlec","x":600,"y":400},{"t":"bekle","ms":1800}],
  [("S","Kar zarar raporu lira karı ile has altın karını ayrı ayrı gösterir."),("V","Ürün bazlı kırılım hangi ürünün kazandırdığını söyler.")]),
 ("defter", "İşlem defteri", "Günün bütün işlemleri tek listede.", [{"t":"git","rota":"/raporlar/islem-defteri"},{"t":"imlec","x":600,"y":400},{"t":"bekle","ms":1800}],
  [("S","İşlem defteri her satış, alış, tahsilat ve ödemeyi tarih sırasıyla listeler."),("S","Her satırın belge bağlantısı fişi ya da makbuzu açar.")]),
 ("analiz", "Satış analizi", "Kim ne sattı, hangi ödeme aracıyla geldi.", [{"t":"git","rota":"/raporlar/satis-analizi"},{"t":"imlec","x":600,"y":400},{"t":"bekle","ms":1800}],
  [("S","Satış analizi ödeme aracı, personel ve şube kırılımı verir."),("V","Prim ve hedef sekmesi personelin primini ve avansını hesaplar."),("S","Dört rapor: merkez, kar zarar, defter, analiz.")])])

for v in V:
    for i, a in enumerate(v["adimlar"]): a["sira"] = i + 1; a["toplam"] = len(v["adimlar"])
    json.dump(v, open(os.path.join(H, f"plan-{v['id']}.json"), "w"), ensure_ascii=False, indent=1)
import re
sorun = [(v["id"], a["id"], m) for v in V for a in v["adimlar"] for _, m in a["cumleler"] if re.search(r"[âîûÂÎÛ'’\d]", m) or len(m.split()) < 4]
print(len(V), "video ·", sum(len(v["adimlar"]) for v in V), "adım ·", sum(len(a["cumleler"]) for v in V for a in v["adimlar"]), "cümle · yazım sorunu:", sorun)
