#!/usr/bin/env python3
# Ses kaydından yapılanlar — 16.10.2026 kuyumcu görüşmesinin (35 dk) kayıt sırasıyla 12 konusu. Her konu iki adım:
# (1) alıntı kartı: "Sizinle konuştuğumuz konu şöyleydi: …" (kayıttaki cümlelerden, ses tanıma hataları düzeltilmiş),
# (2) ekranda yapılan: o konu için kodlanan değişiklik canlı gösterilir. Yalnız ses kaydı yüzünden yapılan işler (M1–M9, M6b,
# kur kaynakları, banka bağlayıcıları, e-Fatura arka plan gönderimi). Yazım: şapka yok, kesme yok, rakam yok, cümle ≥ 4 kelime.
import json, os, re
H = os.path.dirname(os.path.abspath(__file__))
BILEZIK = "#yildiz-urun-ad-01M4Z655102026XVZ182YXA1DR"; CEYREK = "K6ED1Z3NA00"
def _tc(d9):
    d = [int(c) for c in d9]; d.append(((d[0]+d[2]+d[4]+d[6]+d[8])*7 - (d[1]+d[3]+d[5]+d[7])) % 10); d.append(sum(d) % 10); return "".join(map(str, d))
import time
TC = os.environ.get("YENI_TC") or _tc(str(int(time.time()) % 900000000 + 100000000))   # her kayıtta yeni, kontrol haneleri geçerli test TC
BANKA_TC = "34567891238"   # provada açılan "Ayşe Demir" (eğitim şeması) — havaleyi program bu numarayla tanır
A = []
def kart(no, dk, baslik, alinti): return {"t": "kart", "ust": f"Konu {no} / 12 · ses kaydı {dk}", "b": baslik, "s": alinti}
def konu(no, kid, dk, baslik, alinti_ekran, alinti_ses, yapilan, eylemler, cumleler):
    A.append(dict(id=f"{kid}-a", baslik=baslik, sorun=f"Ses kaydı {dk}: {alinti_ekran}", eylemler=[kart(no, dk, baslik, alinti_ekran), {"t": "bekle", "ms": 400}],
                  cumleler=[("S", "Sizinle konuştuğumuz konu şöyleydi."), ("V", alinti_ses)]))
    A.append(dict(id=f"{kid}-b", baslik=baslik, sorun=f"Yapılan: {yapilan}", eylemler=[{"t": "kartKapat"}] + eylemler, cumleler=cumleler))
urun_ac = [{"t": "git", "rota": "/urunler"}, {"t": "bekle", "ms": 600}, {"t": "etiket", "metin": "22 Ayar Burma Bilezik"}, {"t": "tik", "sel": BILEZIK}, {"t": "bekle", "ms": 1200}, {"t": "tik", "sel": "#d-daha-fazla"}, {"t": "bekle", "ms": 900}]
urun_kapat = [{"t": "tus", "key": "Escape"}, {"t": "bekle", "ms": 600}, {"t": "tikVarsa", "sel": "#cekmece-kapat"}, {"t": "bekle", "ms": 500}]

A.append(dict(id="acilis", baslik="Ses kaydından yapılanlar", sorun="On altı Ekim görüşmesi · otuz beş dakika · on iki konu",
  eylemler=[{"t": "git", "rota": "/"}, {"t": "imlec", "x": 700, "y": 400}],
  cumleler=[("S", "On altı Ekimde dükkanınızda yaptığımız görüşmeyi baştan sona yeniden dinledik."),
            ("S", "Kayıtta söylediğiniz her konuyu programa işledik."),
            ("V", "Şimdi kayıttaki sırayla, on iki konuyu tek tek gösteriyoruz.")]))

konu(1, "kaynak", "00:00", "Fiyat ürün başına kendi kaynağından",
  "“Tek bir noktadan çekmeyecek… ya da sabit bir fiyat.”",
  "Tek bir noktadan çekmeyecek; bunu buradan, şunu oradan çekecek, ya da sabit bir fiyat olacak dediniz.",
  "ürün kartında altın cinsi ve fiyat farkı",
  urun_ac + [{"t": "etiket", "metin": "Altın cinsi: fiyat hangi kalemden gelecek"}, {"t": "imlecSel", "sel": "#d-parite"}, {"t": "bekle", "ms": 2200},
             {"t": "etiket", "metin": "Fiyat farkı (₺): sabit ekleme ya da indirim"}, {"t": "imlecSel", "sel": "#d-fiyat-ofset"}, {"t": "bekle", "ms": 2200}] + urun_kapat,
  [("S", "Artık her ürün fiyatını kendi kaleminden alır."), ("S", "Ürün kartında altın cinsi seçilir; bilezik bilezik fiyatından, çeyrek çeyrek fiyatından gelir."),
   ("V", "İsterseniz fiyat farkı alanına sabit bir tutar yazar, fiyatı kendinize göre ayarlarsınız.")])

konu(2, "banka", "00:28", "Para düşer düşmez ekranda",
  "“Düştü mü, hemen şak ekranda görüyoruz… şimdilik simülasyon da olur.”",
  "Bankaya evrak götürdük, entegrasyon sağlandı; para düştü mü hemen ekranda görüyoruz, şimdilik simülasyon da olur dediniz.",
  "sekiz banka bağlantısı, demo bankası ve gelen ödeme kararı",
  [{"t": "git", "rota": "/banka"}, {"t": "bekle", "ms": 800}, {"t": "etiket", "metin": "Havale gönder (demo)"}, {"t": "tik", "sel": "#yildiz-banka-havale"}, {"t": "bekle", "ms": 900},
   {"t": "yazSel", "sel": "#sh-tutar", "metin": "2500"}, {"t": "yazSel", "sel": "#sh-ad", "metin": "Ayşe Demir"}, {"t": "yazSel", "sel": "#sh-kimlik", "metin": BANKA_TC}, {"t": "bekle", "ms": 500},
   {"t": "etiket", "metin": "Kaydet: para demo bankasına düşer"}, {"t": "tikMetin", "metin": "Kaydet", "kap": "aside button, footer button, [role=dialog] button"}, {"t": "bekle", "ms": 3500},
   {"t": "etiket", "metin": "Ayşe Demir 2.500 ₺ gönderdi"}, {"t": "imlecMetin", "metin": "Ayşe Demir"}, {"t": "bekle", "ms": 2200}, {"t": "etiket", "metin": "Borçlu müşteride: borcundan düşülsün mü?"}, {"t": "imlecMetin", "metin": "Bu ödeme borcundan düşülsün mü?"}, {"t": "bekle", "ms": 2200}],
  [("S", "Sekiz banka için bağlantı hazır; Kuveyt Türk ve VakıfBank da aralarında."), ("S", "Şimdilik demo bankasıyla deniyoruz; havale gönderildi."),
   ("H", "Para düştüğü an ekranda... Program göndereni tanır ve borcundan düşülsün mü diye sorar!")])

konu(3, "esik", "02:33", "Sınırı geçen satışta isim",
  "“Otuz bin lirayı geçtiğinde isim koymak gerekiyor.”",
  "Otuz bin lirayı geçtiğinde isim koymak gerekiyor, bunların seçilmesi lazım dediniz.",
  "kuyumcu fatura sınırı programın içinde",
  [{"t": "git", "rota": "/yasal-sinirlar"}, {"t": "bekle", "ms": 800}, {"t": "etiket", "metin": "Kuyumcu satışında fatura sınırı"}, {"t": "imlec", "x": 555, "y": 660}, {"t": "bekle", "ms": 2500},
   {"t": "etiket", "metin": "Kimlik eşiği"}, {"t": "imlec", "x": 555, "y": 390}, {"t": "bekle", "ms": 1800}],
  [("S", "Bu sınırlar artık programın içinde yazılı."), ("S", "Kuyumcu satışında fatura sınırı bu yıl otuz altı bin lira; sınırı geçen satışta müşteri adı istenir."),
   ("V", "Mevzuat değişince rakam tek yerden güncellenir, siz aklınızda tutmazsınız.")])

konu(4, "maliyet", "04:38", "Maliyet milyemi ve sahte kâr",
  "“Kiminin 920’dir, 925’tir… Şimdi benim maliyetim bu.”",
  "Bileziğin maliyeti kiminde dokuz yüz yirmi, kiminde dokuz yüz yirmi beş; sistem bana kar etmediğim parayı kar gösterirse üstüne vergi öderim dediniz.",
  "alış milyemi ve kâr raporunda işçilik ayrımı",
  urun_ac + [{"t": "etiket", "metin": "Alış milyemi (maliyet)"}, {"t": "imlecSel", "sel": "#d-alis-milyemi"}, {"t": "bekle", "ms": 2500}] + urun_kapat +
  [{"t": "git", "rota": "/raporlar/kar-zarar"}, {"t": "bekle", "ms": 900}, {"t": "kaydir", "top": 520}, {"t": "bekle", "ms": 900},
   {"t": "etiket", "metin": "İşçilik (makas) kârı · altın fiyat farkı"}, {"t": "imlec", "x": 300, "y": 360}, {"t": "bekle", "ms": 2000}],
  [("S", "Ürün kartına alış milyemi geldi; maliyetiniz dokuz yüz yirmi beşse dokuz yüz yirmi beş yazarsınız."),
   ("V", "Kar zarar raporu maliyeti bu milyemden bilir; kar etmediğiniz para kar görünmez."),
   ("S", "İşçilik karı ile altın fiyat farkı ayrı satırlarda durur.")])

konu(5, "binde", "04:59", "KDV payı fiyatın üstüne",
  "“Bilezik için binde bir gir… bunu bunun üstüne ekle.”",
  "Bilezik için binde bir gir, KDV sini bunun üstüne ekle, fiyatın içine koy dediniz.",
  "ürün kartında KDV ye giren pay (binde)",
  urun_ac + [{"t": "etiket", "metin": "KDV’ye giren pay (binde)"}, {"t": "imlecSel", "sel": "#d-kdv-binde"}, {"t": "bekle", "ms": 3000}] + urun_kapat,
  [("S", "Ürün kartında KDV ye giren pay binde olarak yazılır; bu bilezikte binde bir."),
   ("V", "Bu pay satış fiyatının üstüne eklenir; her satışta elle hesaplamazsınız.")])

konu(6, "gram", "07:09", "Gram ve tutar yan yana",
  "“Ben hem gramını görmem hem değerini görmem lazım.”",
  "Hepsini has grama çeviremezsin; bileziği bilezik olarak keseceğim, hem gramını hem değerini görmem lazım dediniz.",
  "tezgahta gram ve tutar birlikte",
  [{"t": "git", "rota": "/islemler"}, {"t": "bekle", "ms": 700}, {"t": "etiket", "metin": "Barkod: 22 ayar eski çeyrek"}, {"t": "tik", "sel": "#islem-barkod"}, {"t": "yaz", "metin": CEYREK},
   {"t": "tus", "key": "Enter"}, {"t": "bekle", "ms": 1800}, {"t": "etiket", "metin": "Adet · gram · has · tutar aynı satırda"}, {"t": "imlec", "x": 300, "y": 375}, {"t": "bekle", "ms": 1800}, {"t": "imlec", "x": 850, "y": 362}, {"t": "bekle", "ms": 1500},
   {"t": "tikVarsa", "sel": "#yildiz-tezgah-vazgec"}, {"t": "bekle", "ms": 800}, {"t": "tikVarsa", "sel": "#yildiz-tezgah-onay"}, {"t": "bekle", "ms": 600}],
  [("S", "Tezgahta her satırda adet, gram ve tutar yan yana durur."), ("V", "Has karşılığı yalnız ek bilgi olarak yazar; ana rakam gram ve lira.")])

konu(7, "sarrafiye", "07:46", "Eski çeyrek, Reşat, Hamit ayrı ürün",
  "“Eski çeyrek de var, yeni çeyrek de var… hepsini tek tek tanımlamak lazım.”",
  "Eski çeyrek de var, yeni çeyrek de var; eski çeyrek satış fiyatımız farklı, bunların hepsini tek tek tanımlamak lazım dediniz.",
  "sarrafiye ürünleri ayrı ayrı",
  [{"t": "git", "rota": "/urunler"}, {"t": "bekle", "ms": 700}, {"t": "etiket", "metin": "Sarrafiye"}, {"t": "tik", "sel": "#yildiz-urun-sekme-01M536W2MMMV9ZAJ0FKMG1Q8MX"}, {"t": "bekle", "ms": 1500},
   {"t": "imlec", "x": 200, "y": 400}, {"t": "bekle", "ms": 1500}, {"t": "imlec", "x": 200, "y": 600}, {"t": "bekle", "ms": 1500}, {"t": "tik", "sel": "#yildiz-urun-sekme-hepsi"}],
  [("S", "Sarrafiye sekmesinde eski ve yeni çeyrek, yarım, tam, Ata, Hamit ve Reşat ayrı ürün olarak hazır."),
   ("V", "Her birinin fiyatı ayrı gelir; eski çeyreği yeni çeyrek fiyatından satmazsınız.")])

konu(8, "harem", "09:28", "Elle müdahale ve Harem kaynağı",
  "“Manuel müdahale edebilmesi lazım… Harem Altından çekeceksin.”",
  "Fiyata elle müdahale edebilmem lazım; otomatik gelecekse en uygun yerden, Harem Altından çekeceksin dediniz.",
  "kur ekranında elle giriş ve kaynak seçimi",
  [{"t": "git", "rota": "/kur"}, {"t": "bekle", "ms": 900}, {"t": "etiket", "metin": "Elle gir: kur o an düzeltilir"}, {"t": "imlecSel", "sel": "#yildiz-kur-b2"}, {"t": "bekle", "ms": 2200},
   {"t": "etiket", "metin": "Tüm ayrıntılar: kaynak ve pariteler"}, {"t": "tik", "sel": "#yildiz-kur-ayrinti"}, {"t": "bekle", "ms": 1500}, {"t": "imlec", "x": 500, "y": 400}, {"t": "bekle", "ms": 1500}],
  [("S", "Kur ekranında elle gir düğmesiyle fiyatı istediğiniz an düzeltirsiniz; kim ne zaman değiştirdi kayda geçer."),
   ("S", "Kaynak listesine Harem eklendi; döviz için ayrı kaynak seçilebilir."),
   ("V", "Harem bağlantısı hazır; veri kullanım izni gelince açılacak.")])

konu(9, "milyem", "10:26", "Her ürünün milyemi elle",
  "“Bileziğin milyemi, çarpanı burada olacak… 925 ise 925 yazacağım.”",
  "Bileziğin milyemi, çarpanı burada olacak, elimle müdahale edeceğim; şarnel, el örgüsü, kilitli bilezik, hepsinin milyemi ayrı dediniz.",
  "ürün kartında satış milyemi",
  urun_ac + [{"t": "etiket", "metin": "Satış milyemi: elle yazılır"}, {"t": "yazSel", "sel": "#d-satis-milyemi", "metin": "930"}, {"t": "bekle", "ms": 2500},
             {"t": "etiket", "metin": "Vazgeç: deneme kaydedilmedi"}] + urun_kapat,
  [("S", "Her ürünün satış milyemi kartta elle yazılır."), ("V", "Fiyat gram çarpı milyem çarpı has fiyatından hesaplanır."),
   ("S", "Her bilezik çeşidi kendi milyemini taşır; birini değiştirmek ötekini bozmaz.")])

konu(10, "iscilik", "10:53", "İşçilik faturası ve kâr",
  "“Çalıştığın firmadan işçilik faturasını alman lazım.”",
  "Çalıştığın firmadan işçilik faturasını alman lazım; aradaki masrafı fatura etmezsen o para sana kar gibi görünür dendi.",
  "alış işçilik belgeleri ve kâr raporu",
  [{"t": "git", "rota": "/raporlar/kar-zarar"}, {"t": "bekle", "ms": 900}, {"t": "etiket", "metin": "Alış işçilik belgeleri"}, {"t": "tik", "sel": "#kz-iscilik-ac"}, {"t": "bekle", "ms": 1600},
   {"t": "imlec", "x": 1150, "y": 190}, {"t": "bekle", "ms": 2500}, {"t": "tikVarsa", "sel": "#cekmece-alis-iscilik-iptal"}, {"t": "bekle", "ms": 500}],
  [("S", "Alışta işçilik belgesi kaydedilir; fatura ya da gider pusulası."),
   ("V", "Toptancıya ödenen işçilik ayrı görünür; kar raporu bunu kar saymaz."),
   ("S", "Belgesi girilmemiş işçilik için uyarı çıkar.")])

konu(11, "kural", "25:58", "Ayarlarda kural, üründe KDV oranı",
  "“Tek tek yapma; bilezik ise şu kadar, kolye ise bu kadar.”",
  "Ayarlar bölümünde ben tanımlarım; içinde bilezik geçiyorsa binde bir, kolyeyse şu kadar, tek tek yapma; ürün düzenlede de KDV oranı olsun dediniz.",
  "fiyat kuralları ve ürünün KDV oranı",
  [{"t": "git", "rota": "/urunler"}, {"t": "bekle", "ms": 700}, {"t": "etiket", "metin": "Fiyat kuralları"}, {"t": "tik", "sel": "#urun-kurallar-ac"}, {"t": "bekle", "ms": 1500},
   {"t": "imlec", "x": 1100, "y": 175}, {"t": "bekle", "ms": 2000}, {"t": "etiket", "metin": "Önizle: hangi ürünlere uygulanacak"}, {"t": "tikMetin", "metin": "Önizle", "kap": "aside button"}, {"t": "bekle", "ms": 2500},
   {"t": "tus", "key": "Escape"}, {"t": "tikVarsa", "sel": "#cekmece-kapat"}, {"t": "bekle", "ms": 600}] + urun_ac +
  [{"t": "etiket", "metin": "KDV oranı (%) — ürünün kendi oranı"}, {"t": "imlecSel", "sel": "#d-kdv-orani"}, {"t": "bekle", "ms": 2200}] + urun_kapat,
  [("S", "Fiyat kurallarında bir kez tanımlarsınız; adında bilezik geçen ürüne alış milyemi ve KDV payı kendiliğinden yazılır."),
   ("S", "Önizle ile kuralın hangi ürünlere uygulanacağını görürsünüz."),
   ("V", "Ürünün kendi KDV oranı yazılıysa faturada o kullanılır; boşsa faturada seçilen oran.")])

konu(12, "fatura", "34:55", "Fiş yerine doğrudan fatura",
  "“Fişe hiç gerek yok… TC’yi yazıp sisteme girdiğim anda fatura kesin.”",
  "Fişe hiç gerek yok; otuzun altında da olsa fark etmez, TC yi yazıp sisteme girdiğim anda fatura kesilsin dediniz.",
  "TC ile müşteri, satışta fatura sorusu, giden kutusu",
  [{"t": "git", "rota": "/islemler"}, {"t": "bekle", "ms": 700}, {"t": "etiket", "metin": "Müşteri: TC yazılır"}, {"t": "tik", "sel": "#yildiz-tezgah-degistir"}, {"t": "bekle", "ms": 900},
   {"t": "tik", "sel": "#yildiz-musteri-ara"}, {"t": "yaz", "metin": TC}, {"t": "bekle", "ms": 2000},
   {"t": "etiket", "metin": "Kayıtlı değil: adı yazılıp açılır"}, {"t": "yazSel", "sel": "#yildiz-musteri-yeni input", "metin": "Ayşe Demir"}, {"t": "bekle", "ms": 600},
   {"t": "tikMetin", "metin": "Müşteriyi aç ve seç", "kap": "main button"}, {"t": "bekle", "ms": 2000},
   {"t": "etiket", "metin": "Barkod okutulur"}, {"t": "tik", "sel": "#islem-barkod"}, {"t": "yaz", "metin": CEYREK}, {"t": "tus", "key": "Enter"}, {"t": "bekle", "ms": 1800},
   {"t": "etiket", "metin": "Kaydet"}, {"t": "tik", "sel": "#yildiz-tezgah-kaydet"}, {"t": "bekle", "ms": 1500},
   {"t": "etiket", "metin": "Fatura kesilsin mi? — Evet"}, {"t": "imlecSel", "sel": "#yildiz-belge-evet"}, {"t": "bekle", "ms": 1500}, {"t": "tik", "sel": "#yildiz-belge-evet"}, {"t": "bekle", "ms": 3000},
   {"t": "git", "rota": "/faturalar"}, {"t": "bekle", "ms": 900}, {"t": "etiket", "metin": "Fatura kesildi"}, {"t": "imlec", "x": 300, "y": 540}, {"t": "bekle", "ms": 1500},
   {"t": "etiket", "metin": "e-Fatura giden kutusu"}, {"t": "tik", "sel": "#fk-ebelgeler"}, {"t": "bekle", "ms": 2000}],
  [("S", "Tezgahta müşteri aramasına TC yazılır; kayıtlıysa bulunur, değilse adı yazılıp tek dokunuşla açılır."),
   ("H", "Kaydete basınca program fatura kesilsin mi diye sorar; evet dediğiniz an fatura kesilir!"),
   ("S", "Entegratör anahtarı girildiğinde fatura arka planda gönderilir; durumu e-Fatura giden kutusunda izlenir.")])

A.append(dict(id="kapanis", baslik="On iki konunun hepsi programda", sorun="Ses kaydından yapılanlar · ByGatech",
  eylemler=[{"t": "git", "rota": "/"}, {"t": "imlec", "x": 700, "y": 400}],
  cumleler=[("S", "Görüşmede söylediğiniz on iki konunun hepsi programda."),
            ("V", "Denemek istediğiniz her şeyi birlikte tezgahta deneyebiliriz.")]))

plan = dict(id="ses-kaydi", baslik="Ses kaydından yapılanlar", alt="16 Ekim görüşmesi · 12 konu, kayıt sırasıyla", acilisUst="BYGATECH · SİZİNLE KONUŞTUKLARIMIZ",
            kapanisAlt="Görüşmedeki 12 konunun hepsi programda", adimlar=A)
for i, a in enumerate(A): a["sira"] = i + 1; a["toplam"] = len(A)
json.dump(plan, open(os.path.join(H, "plan-ses-kaydi.json"), "w"), ensure_ascii=False, indent=1)
sorun = [(a["id"], m) for a in A for _, m in a["cumleler"] if re.search(r"[âîûÂÎÛ'’\d]", m) or len(m.split()) < 4]
print(len(A), "adım ·", sum(len(a["cumleler"]) for a in A), "cümle · yazım sorunu:", sorun)
