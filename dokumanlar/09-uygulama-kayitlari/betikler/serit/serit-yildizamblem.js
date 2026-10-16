export const meta = {
  name: 'kuyumcu-emanet',
  description: 'Bir uygulama dalgasinin paketlerini SERI olarak Opus uygulayici + hasim dogrulayici + duzeltme donguleriyle uygular; kapi gecmeyen paket dalgayi durdurur',
  phases: [
    { title: 'Uygulama', detail: 'Opus uygulayici: spesifikasyon + kosullar + UX sozlesmesi; test, mutasyon, denetim, canli', model: 'opus' },
    { title: 'Dogrulama', detail: 'Hasim dogrulayici: once KOR inceleme (yalniz spesifikasyon + degisen dosyalar), sonra rapor; finansal sinir testleri' },
    { title: 'Tasarim', detail: 'Tasarim hakemi (Opus): raporu gormeden ekrani kendisi acar, doldurur, olcer; katalog K1-K23', model: 'opus' },
    { title: 'Hakem', detail: 'Capraz hakem (Sonnet): iki raporun sayisal celiskilerini avlar; KRITIK celiski paketi o tur gecirmez', model: 'sonnet' },
  ],
}
const GOMULU = {"dalga": "YILDIZ-AMBLEM (09.10 · t9 · 5188/5189)", "ux_sozlesme": "### Ek UX sözleşmesi — mantık ve tutarlılık taraması (2026-09-16)\n- UX-S21 — Sistemin bildiği alan kullanıcıdan istenmez: sunucuda hesaplanabilen veya kayıtlı olan hiçbir değer (canlı has fiyatı, has gram = ayar+gram, para birimi = kasadan, vergi no = cariden, şube = oturumdan, KDV türü = ürün tipinden) boş giriş alanı olarak konmaz; türetilir, doldurulur ve nereden geldiği rozetle söylenir.\n- UX-S22 — Tek seçenek varsa seçici gizlenir: tek kasa, tek şube, tek liste durumunda seçici gösterilmez; değer salt okunur satır olarak yazılır ve gövdeye otomatik konur. Aynı ekranda aynı sorunun iki farklı cevabı olamaz (POS'ta bacak kasası otomatik seçilirken ana kasanın seçilmemesi gibi).\n- UX-S23 — Soyut kayıt kaydedilince etkisi aynı ekranda gösterilir: fiyat listesi, kampanya, kategori, reyon, yetki gibi dolaylı kayıtların sonucu (etkilenen ürünler, hesaplanan fiyat, etkilenen adet) kaydetmeden önce önizlenir ve kaydettikten sonra aynı ekranda görünür. Sayı gösteren her yer (ürün sayısı, gecikmiş adet, maliyeti bilinmeyen adet) o kayıtlara giden bağlantı olur.\n- UX-S24 — Bağımlı alanın bağını ekran kurar: cari değişince fiyat teklifi yenilenir (sessizce değil, 'liste değişti, güncelle' düğmesiyle), iade kaynağı seçilince cari dolar ve kilitlenir, ödeme türü KART olunca komisyon alanı açılır, KDV türü KUYUM olunca oran sabitlenir.\n- UX-S25 — Oluşturulabilen her kayıt düzeltilebilir ya da neden düzeltilemediği yazar: ya düzenle/pasife al/aktifleştir yolu vardır, ya da DegismezIpucu ile değişmezlik gerekçesi ve doğru düzeltme yolu (ters hareket, düzeltme fişi, storno) anlatılır. 'Sunucuda uç var, ekranda düğme yok' kabul edilmez.\n- UX-S26 — Oluşturma formu düzenleme formuyla aynı alan kümesini taşır: sunucunun oluşturmada kabul ettiği alan 'sonra düzenlemeden gir' diye ertelenmez; seçenek listeleri iki formda tek diziden üretilir.\n- UX-S27 — Kaydet sonrası sonuç görünür: her yazma işlemi başarı bandıyla biter ve sunucunun döndürdüğü sonuç (fiş no, fatura no, uygulanan/atlanan satır, yeni bakiye) banda yazılır; POST yanıtı okunmadan atılmaz. Geri alınamaz işlem (iptal, maskeleme, düzeltme fişi, sayım iptali) önce onay ister.\n- UX-S28 — Hata 'kayıt yok'a çevrilmez: catch içinde listeyi boşaltıp boş durum göstermek yasaktır (Anayasa md.1). Boş durum yalnız istek başarılı ve sonuç gerçekten boşken çıkar; hata bandı ayrıdır ve kullanıcının göreceği yerdedir.\n- UX-S29 — Sunucu sınırı ekranda söylenir: limitli her liste (ilk 500 ürün, 50 cari, 100 fatura) sınırını ve daraltma yolunu yazar, sınıra değdiyse 'daha fazlası var' uyarısı basar. Sessiz kırpma yok.\n- UX-S30 — Kilitli düğme sebebini söyler: disabled bir eylemin yanında hangi koşulun eksik olduğu yazılır (kur yok, kasa seçilmedi, MASAK formu eksik, gerekçe n/20 karakter). Görünmez kural olmaz.\n- UX-S31 — Aynı kavram tek ad, tek dil, tek kaynak: ham veritabanı kodu (ACIK, TEZGAHTAR, A22, MUSTERI) ekrana basılmaz, sözlükten çevrilir; aynı kavram iki ekranda iki ad taşımaz ('Kategori' hem ürün cinsi hem ağaç kategorisi olamaz); ayar/rol/durum listeleri tek kaynaktan beslenir ve eksik anahtar derleme hatası verir.\n- UX-S32 — Para birimi tutarla birlikte taşınır: hiçbir tutar 'varsayılan TL' sayılıp ₺ ile basılmaz; Bicim.Para(tutar, paraBirimi) kullanılır, toplam kartları hangi para birimini topladığını etiketinde söyler.\n- UX-S33 — Dönemden bağımsız kart bunu yazar: üstteki tarih/dönem seçicisinin etkilemediği kartlar '(bugün itibarıyla — dönemden bağımsız)' alt metnini taşır.\n\n", "paketler": [{"no": "YILDIZ-AMBLEM", "ad": "Yildiz amblemi (§ 2a, kurucu karari 9 Ekim): sol ust halkasiz elmas + MIHENK yazisi, titanyum gradyan parilti, 11 sn dongu, koyu+acik, hareket azaltma — tuvaldeki gibi AYNEN ve EKSIKSIZ", "belge": "/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md", "kosullar": ["HASIMSIZ PAKET (kurucu 30.09 21:15): bu paket yalniz gorunum; hasim dogrulayici KOSMAYACAK. Bu yuzden KENDI kanitin baglayicidir: tam suit (tur basina bir kez) 0 yeni kirmizi, denetim.sh TEMIZ, her kabul maddesi icin mutasyon, gercek basliksiz Chrome olcumu + goruntuler Read ile. Para/veri/yasal davranisina DOKUNMA; dokunman gerekirse DUR ve rapora 'hasimli pakete devir' yaz.", "KURUCU (09.10, verbatim): 'tamam bunu uygula eksiksiz ayni sekilde'.", "KURUCU (09.10, BAGLAYICI, verbatim): 'ben senden bunu istedim sen farkli yapmissin kurus kismi normal tutardan daha kucuk gibi detaylara bak birebir tasarim neyse aynisini istisnasiz olarak istiyorum'. HEDEF = TUVALIN KENDISI. Paket notundaki okunurluk karari (200 yerine >=300) ve diger 'uyarlama'lar IPTAL: tuvaldeki agirlik, olcu, harf araligi, renk, bosluk, hizalama, metin AYNEN. Tuvalde olmayan oge Yildiz'da ekrana konmaz (gerekiyorsa tuvaldeki kaliplarla: '+ ekle', b2 alti cizili, 'Tum ekranlar').", "KURUCU (09.10, ikinci mesaj, verbatim): 'uyarlama varsayim vs istemiyorum bu benim icin cok onemli ayrica ben bunun icin o kadar tasarim icin calistim farkli olamamali birebir aynisini istiyorum'. KARAR KURALI: tuvalden her sapma KALDI'dir; bir ogenin tuvalde karsiligi belirsizse VARSAYMA — tuvaldeki en yakin kalibi kullan ve rapora 'belirsiz eslesme' tablosuna yaz (orkestrator kurucuya gosterir).", "IZOLASYON: Yildiz farki tema-yildiz.css + sema kosullu razor dali (YILDIZ-2/3 deseni); veri/para davranisi DEGISMEZ (ekrandaki sayilar mevcut servislerden).", "MODEL: bu paket Opus (kurucu kalite talebi); olcum 1440 + 375 · BASIT + PRO · koyu + acik; Read en cok 6 (yan yana goruntuler); tam suit BIR kez.", "KAYNAK (TEK DOGRULUK): /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md § 2a (09.10 17:28 surumu — notun TAMAMINI oku; § 2 Menu satiri da guncellendi) + tuval /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/yildiz-tuval/kaynak/Amblem_sol.dc.html (koyu) ve Amblem_sol_acik.dc.html (acik). Amblem_orta UYGULANMAZ.", "UYGULAMA: tuvaldeki SVG (gradyan url(#ti), SMIL animateTransform degerleri, keyTimes, dur=11s), CSS keyframes (opaklik/bulaniklik/hale zamanlari, .h1..h5 harf gecikmeleri 0,14 sn) ve olculer (isaret 40 px, 14 px bosluk, MIHENK 13 px / 400 / harf araligi 4,4 px / buyuk harf) BIREBIR kopyalanir; renkler koyu govde #6A6A6A→#9A9A9A parilti #FFFFFF hale beyaz %16, acik govde #3A3A3A→#5E5E5E parilti #C9C6BE hale #141414 %10. Tek <a> → Bugun. Ek dosya/gorsel/JavaScript YOK (SVG + CSS). prefers-reduced-motion: reduce → tum animasyon kapali, isaret+yazi tam opak, hale gizli. Yalniz amblem hareket eder.", "OLCUM: tuval Amblem_sol(.acik) ve uygulama ust seridi AYNI viewport'ta basliksiz Chrome'da acilir; animasyon zamani esitlenir (document.getAnimations() currentTime + SVG setCurrentTime ile ayni ana sabitlenir) ve 0,5 / 2,0 / 6,0 / 9,5 sn karelerinde amblem bolgesi piksel karsilastirmasi (fark orani raporda, hedef ~0) + yan yana goruntu; tuval-kiyas.mjs ile olcu/renk tablosu; reduced-motion karesi ayri.", "TOKEN: Read en cok 6 (4 zaman karesi yan yana + acik + reduced-motion); tam suit BIR kez.", "KAYNAK (TEK DOGRULUK — ONCE TAMAMINI OKU): /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md", "KAPSAM: Not § 2a tamami; YildizUstMenu (kabuk) icinde.", "KABUL: 4 zaman karesi x koyu+acik: tuvalle yan yana goruntu + piksel fark orani ~0 (rapor tablosu); olcu/renk tablosu tolerans icinde; reduced-motion karesi sabit+opak; diger 8 semada kabuk piksel FARK 0 (betikler/tur/sema-fark.mjs); ek dosya/JS yok (bUnit/kaynak testi); mutasyon >=3 kirmizi cmp AYNI (dur 11s→10s, harf gecikmesi, reduced-motion kurali); build 0/0, denetim TEMIZ (P-30 haric), tam suit 0 yeni kirmizi.", "DOSYA ALANI: Bilesenler/Kabuk/YildizUstMenu.razor (amblem), tema-yildiz.css (amblem keyframes), Kuyumcu.Tests YildizAmblem*.cs.", "HIZ VE TUR (kurucu 29.09 23:10): paket EN COK 2 tur alir. Tam suit tur basina BIR kez (son kodla); ara adimlarda filtreli test. Kalite kapilari korunur: build 0/0, denetim.sh TEMIZ, mutasyon (her kritik kural en az bir testi kirmizi yapar, geri alininca cmp AYNI), canli API + SQL kaniti, ekrana dokunan her is gercek (basliksiz) Chrome'da 1280 + 375 · BASIT + PRO · acik + koyu olculur (sayisal olcum tum kombinasyonlarda); TOKEN TASARRUFU (kurucu 08.10): Read ile goruntu EN COK 4 (en riskli kombinasyonlar). Test kiracisi is bitince `zsh kiraci-temizle.sh <id> --uygula` ile silinir (bash degil ZSH). COMMIT YOK. Dosya silme YOK (rm kancasi). Sir/parola degeri ASLA basilmaz. 'dotnet ef' --no-build YASAK (once build).", "MEVZUAT (kurucu, BAGLAYICI): yasal kural/esik/oran yalniz BIRINCIL kaynakla (Resmi Gazete, mevzuat.gov.tr, GIB, MASAK) yazilir; dogrulanamayan deger TEYITSIZ damgasiyla BUGUNKU davranisi birebir koruyarak tutulur ve ilgili kapi fail-closed kalir; mali musavir sorusu raporun 'Kurucu/mali musavir' bolumune yazilir. Tahmin etme.", "KURUCU TEMEL KURALI (25.09 ~11:35, BAGLAYICI): 'ne islem yapacaksa yapsin en basit haliyle, zorunlu olanlar haric ilk etapta kullanici gormesin … isterse ekler ama eklemese bir kayip yoksa acilir kapanir bir secenek olarak kalsin … Pusula'ya da bakarak'. Gecmis kayit listeleri KAPALI akordeon (ortak Bilesenler/Katlanir.razor SATIS-2 seridinde yaziliyor — hazirsa kullan, degilse ayni API ile gecici yerel katlanir, rapora not).", "OLCUM ARACLARI (BAGLAYICI): /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/ — BENIOKU.md (tur.mjs + analiz.py) ve tutarlilik.mjs (kullanim: TUR_HEDEF=http://localhost:5189 node tutarlilik.mjs '<rota,rota,...>' 1440 → tutarlilik-fark-1440.txt BASIT↔PRO eleman farki, tutarlilik-dugme-1440.txt dugme sinifi boy/yazi dagilimi). Orkestrator tabani: ham/MOD-TUTARLILIK-FARK-1440.txt (198 fark, 37 ekran) ve ham/DUGME-OLCEK-1440.txt.", "KURUCU IZNI (23.09): bu projenin veritabanindaki SATIRLAR komutla silinebilir; DOSYA silme YASAK.", "⚠ IZOLASYON (30.09): KUYUMCU_TEST_DB=kuyumcu_test_t9 · canli API 5188 · vekil 5189; CDP portu 9980-9995. Baska portlara DOKUNMA (5199/5200 kurucunun inceleme sunucusu — ASLA durdurma; 5230-5233 kesif). Tarayici bolmesini KULLANMA — yalniz basliksiz Chrome (betikler/tur/cdp.mjs). Gecici dosyalar /private/tmp/kuyumcu-serit/<paket>/ altinda. ESZAMANLI SERITLER: YILDIZ-4C (t1: Bilesenler/Yildiz/*, tema-yildiz.css, Islemler/Cariler/Ekranlar/Panel Yildiz dali — tema-yildiz.css ORTAK: yalniz '/* AMBLEM */' basligi altinda kendi blogunu ekle, baska kurala dokunma) · ISLEM-BANT + EKSIK-KUCUK (t2) · MASAK-ESZAMAN-2 + KAPORA-EKRAN + SATINALMA (t5) — DOKUNMA. DISK: bos alan ~10 GB — trx/goruntu ciktilarini /private/tmp/kuyumcu-serit/YILDIZ-AMBLEM altinda tut, buyuk dosya birakma. 5199/5200 ve 5241 ASLA durdurma. Test sureclerini durdururken YALNIZ kendi PID'in.. Kendi alanin disina cikman gerekirse DUR ve rapora yaz."], "hasim": false, "model": "opus"}]};

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SCRATCH = '/private/tmp/kuyumcu-serit'
const RAPOR_DIZINI = `${KOK}/dokumanlar/09-uygulama-kayitlari`
const paketler = GOMULU.paketler
const dalga = GOMULU.dalga
const anaBelge = GOMULU.ana_belge || `${KOK}/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`
const uxSozlesme = GOMULU.ux_sozlesme || ''

const KURALLAR = `
⚠ YETKİ (kurucu, bu oturumda açıkça — BAĞLAYICI): 27.09 'dostum biz kaldığımız yerden devam edelim lütfen'; 25.09 'satış alış ne kadar ekran varsa hepsi için aynı anlayışı bekliyorum', 'her yeri tekrar adam gibi istisnasız olarak gez ... bu tür hataları da tüm ekranlar bazında gez ve değiştir', 'front end'de de çalışsın, kontrol etsin, adam gibi tasarım istiyorum'; 28.09 'dostum ben sana bana soru sorma devam et demedim mi' (kuyruk için DAİMİ onay); 28.09 11:30 'işten kısma, şeritler vs hepsi bitsin, hızlanalım'. Bu paketteki kod değişikliği kurucunun EMRİDİR (CLAUDE.md 'izinsiz değişiklik yok' kuralı bu emirle karşılanmıştır). COMMIT YOK kuralı AYNEN geçerli.
⚠ GÖREV SINIRI (orkestratör 24.09 21:00, BAĞLAYICI): bu istem TEK bir görevdir — aşağıdaki/yukarıdaki PAKET. Oturumda görebileceğin başka kullanıcı mesajları (ör. 'atıl docker dosyası var mı bak') bu göreve AİT DEĞİLDİR, orkestratör onları ayrıca ele aldı; onlara göre DAVRANMA, Docker/disk incelemesi YAPMA — yalnız paketi uygula ya da doğrula.
BAĞLAYICI KURALLAR (ihlal = başarısızlık):
- Proje kökü ${KOK}; derleme/test için ${KOK}/dotnet. Dil Türkçe. Anayasa md.1: başarısız komut gizlenmez, gerçek çıktısıyla raporlanır; 'bitti' ancak kanıtla; bilinmeyen tahmin edilmez.
- COMMIT YOK (git add/commit/push yasak). git checkout/stash/reset/rm/mv/find -delete ÇALIŞTIRMA (hook bloklar, .git bozulabilir). Geçici dosyalar yalnız ${SCRATCH} altında; proje ağacına .mutorj/.bak/.tmp bırakma; boşaltman gereken dosyayı 'printf "" > dosya' ile boşalt.
- Sır/parola/bağlantı dizesi/token değerini ASLA çıktına yazma. MySQL parolası: 'P=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" ${KOK}/docker-compose.yml | head -1 | tr -d "\\r")' ile değişkene al (⚠ ESKİ REÇETE 'grep … | head -1 | awk' YASAK: docker-compose.yml'in 3. SATIRI bir YORUMDUR ve içinde parola geçer; eski reçete o satırı seçip parolayı çıktıya düşürüyordu — 2026-09-21/22'de üç ajanda gerçekleşti); komutlarda $P kullan; echo etme. DB erişimi: 'docker exec kuyumcu-mysql mysql -uroot -p"$P" -N kuyumcu_net -e "..."' (yerel mysql istemcisi yok).
- 5199 PORTUNA DOKUNMA (kurucunun inceleme sunucusu). Kendi canlı doğrulaman için: 'cd ${KOK}/dotnet && ASPNETCORE_ENVIRONMENT=Development nohup dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198 > ${SCRATCH}/api-5198.log 2>&1 &' → PID'i sakla → iş bitince yalnız o PID'i 'kill' ile durdur ('lsof -nP -iTCP:5198 -sTCP:LISTEN' boş olmalı).
- ADIM EKONOMİSİ: her araç çağrısını KISA tut (3 dakikadan uzun süren tek bir düşünme/komut adımı 'durdu' sayılır ve iş İPTAL olur): büyük dosyaları bütünüyle cat'leme, grep/sed -n aralıklarıyla oku; testleri filtreli koş, tam süiti yalnız sonda; uzun komutlara 'timeout' ver; API'yi nohup ile arka planda başlat.
- ÇIKTI EKONOMİSİ: 'dotnet build' ve 'dotnet test' çıktısını dosyaya yaz ve yalnız süz ('grep -E "error|Hata|Uyarı|Başarısız|Passed!|Failed!|Toplam|Geçti|Kaldı"'); tam çıktıyı bağlama okuma.
- MİGRATION PROSEDÜRÜ (yalnız spesifikasyon şema değişikliği istiyorsa): (1) 'dotnet build Kuyumcu.sln' YEŞİL olmadan 'dotnet ef' ÇALIŞTIRMA; '--no-build' ASLA. (2) 'cd ${KOK}/dotnet && dotnet ef migrations add <YYYYMMDDHHMMSS'siz Ad> --project Kuyumcu.Api' (build'li). (3) Üretilen .cs ve .Designer.cs dosyalarının 0 bayttan büyük ve Up() gövdesinin beklenen değişikliği içerdiğini OKU. (4) 'dotnet ef database update --project Kuyumcu.Api --connection "server=localhost;port=3306;database=kuyumcu_net;user=root;password=$P"' (parola değişkenden). (5) kuyumcu_test'i testler kendisi kurar (süreç başına EnsureDeleted+Migrate). Yeni her varlık IKiraciVarlik + (tenant_id, …) indeks; HasQueryFilter kapsama denetimi uygulamayı açmaz sa bunu düzelt.
- DEĞİŞMEZLER: deftere yazan tek yol HareketFabrikasi.Ekle (Hareketler.Add yasak); IgnoreQueryFilters/Find/FindAsync yasak; ExecuteUpdate/Delete beyaz listesi 4; [RolGerekli] TAM eşleşme; DTO DataAnnotations yalnız controller yolunda → servis içi elle kontrol; PATCH sözleşmesi 'gövdede yoksa koru, null ise sil' + testi; Sozlukler.cs TR+EN (çıplak metin yok); her yeni form için düzenleme yolu (UX-S1); mevcut element id'leri DEĞİŞMEZ; yeni id'ler '<önek>-<alan>'; Razor öznitelikte '{ }' lambda yok; 'disabled="false"' dizgedir → '@(false)'; bUnit sahte API '{}' varsayılanı → yeni uçlara gerçekçi yanıt; SVG/CSV sayıları InvariantCulture; Türkçe JSON gövdesi ayrıştırılarak sınanır; yeni CSS'te sabit renk yok, belirteç var(); satır içi style yok; boş durum + hata/başarı bandı zorunlu; ateşle-unut yan kanallar beklenmez.
- MUTASYON YÖNTEMİ (mut.sh projede yok): kaynağı ${SCRATCH}/mut-yedek/<paket>/ altına kopyala → 'perl -0777 -pi -e' ile TEK eşleşmeli değişiklik → 'dotnet test --filter <ilgili> -v n' → yedeği geri kopyala → 'cmp' ile birebir aynı olduğunu KANITLA. Hiçbir testi kırmayan mutasyon = TEST AÇIĞI: testi düzelt, mutasyonu tekrarla. Son mutasyondan sonra TEMİZ 'dotnet build'.
- Kabuk zsh: 'for t in \${=LISTE}'. API yanıtları camelCase; boş kimlikle devam etme; yeni kiracıda /kur/guncel ilk 10-20 sn boş dönebilir → bekle. Test kiracısı adı 'PAKET<no>-TEST' ile başlasın ve iş sonunda tüm tablolardan silinsin (information_schema'dan tenant_id kolonu olan tablolar), tenants sayısı başlangıçtaki değere dönsün.
- SÖZLEŞME-DoD (Bertan koşulu, 2026-09-16 12:12 mührü, dalga 2b'den itibaren her pakette bağlayıcı): her YENİ varlık/kayıt türü PATCH (gönderilmeyen alan = koru, null = sil) + DELETE ya da pasife alma ucu + [ModulYetkisi]/[RolGerekli] + denetim kaydı (DenetimServisi.YazmaKaydet ÇAĞRILIR, yeniden yazılmaz) + ekranda Düzenle/Pasife al eylemi ya da DegismezIpucu gerekçesi ile DOĞAR. Finansal varlıklarda (hareketler, kasa_hareketleri, banka_hareketleri, faturalar) hard DELETE YASAK — yalnız pasifleştirme/storno. Raporda "DoD sözleşmesi" bölümü zorunlu: hangi varlık hangi uçla düzenlenebilir, hangisi neden salt okunur. Eksikse hasım doğrulayıcı paketi GEÇİRMEZ.

- DoD EK (kurul tur 1, 2026-09-16 13:35, bağlayıcı): (1) HER DÜĞME ya gerçek istek atar ya da disabled + DegismezIpucu/açıklama taşır — "tıklanınca hiçbir şey olmayan" düğme = paket geçmez (bUnit: her button için tıklama → istek ya da disabled); (2) TEK DÜZENLEME KALIBI: çok alanlı düzenleme Cekmece.razor (2c sonrası), tek alanlı SatirIciDuzenle; ModalPencere yalnız yıkıcı onay; ikinci "duzenlenenId" kalıbı açılmaz — dotnet/denetim.sh sayaçları (Pages/ altında yeni ModalPencere düzenleme kullanımı = 0 artış); (3) her form: boş/geçersiz gönderimde alan altı Türkçe mesaj, sessiz etkisizlik YASAK; (4) yıkıcı eylem (pasife al, iptal, kes, storno) ortak onay modalı + geri dönüş yolu (aktife al).

- DoD EK 2 (mühür 2, 13:38): TEK DOKUNUŞ — bir razor sayfası bu dalgada açıldıysa raporun "Dokunulan sayfalar" listesine yazılır; aynı sayfa başka bir dalgada tekrar açılırsa kapı kaydına yazılı gerekçe zorunlu (gerekçesiz ikinci dokunuş = engel). Storno taklidi ("ters iade") YASAK; storno yalnız 48-A motoruyla. Ürün, dalga 2b+2c ve QA turu 2 bitene dek yalnız ÜCRETSİZ PİLOT: fiyat/teklif/tahsilat içeren hiçbir metin üretilmez.

- ⚠ GERÇEK TARAYICI KURALI (kurucu talimatı 2026-09-24, BAĞLAYICI — MEVZUAT KURALI ile aynı ağırlıkta): **Ekrana dokunan hiçbir iş (razor, css, bileşen, biçim, sözlük, dil verisi) gerçek tarayıcıda ölçülüp ekran görüntüsüyle kanıtlanmadan BİTTİ/GEÇTİ sayılmaz.** bUnit/API/SQL kanıtı görünürlük, sığma, taşma ve biçim için YETMEZ (24.09'da üç kusur testten yeşil geçip kurucunun gözüne takıldı: menüde gizli 33 satır görünüyordu, Ayarlar kartı dar sütuna sıkışmıştı, İngilizce tarayıcıda kur 'TRY6,754.91' çıkıyordu). Zorunlu ölçüm, değişen HER ekran için: 1280×800 ve 375×812 · BASİT ve PROFESYONEL mod · açık ve koyu tema. Ölçüm aracı: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js (javascript_tool ile çalıştır) → taşan yazı 0 · üst üste binen 0 · tek kelimesi kırılan kısa etiket 0 · yatay kaydırma 0 · '[hidden]' olup görünen 0 · para/sayı tr-TR ('TRY' kalıbı 0) · dar sütun (en geniş blok ana alanın %60'ından azsa gerekçe). Oturum parola YAZMADAN açılır: API'ni kendi portunda aç, test kiracısını API ile kur, {email,sifre} JSON'u chmod 600 yaz, 'python3 .../betikler/inceleme-vekili.py <fikstur.json> <api_port> <vekil_port>' çalıştır, başsız Chrome ile (betikler/tur/cdp.mjs, BENIOKU.md; kendi CDP portun, --user-data-dir kendi ${SCRATCH} alt dizinin) http://localhost:<vekil_port>/__giris adresine git ve ölç; ekran görüntüsünü CDP ile al, Read ile GÖR. TARAYICI BÖLMESİNİ (mcp__Claude_Browser, tabs_create) KULLANMA — alt ajanda görünmez, ekran görüntüsü alınamaz (28.09: 3 paket bu yüzden 'kanıt yok' turu yaktı). Başka sekmelere/profillere dokunma. Rapora: rota × genişlik × mod ölçüm tablosu + ekran görüntüsü. DOĞRULAYICI: ölçüm tablosu ve görüntü yoksa ya da ölçümü kendi sekmesinde tekrar ettiğinde sonuç tutmuyorsa KALDI verir.\n- MEVZUAT KURALI (kurucu talimatı 2026-09-23, BAĞLAYICI): **Mevzuatla ilgili hiçbir işlem, internetten araştırıp BİRİNCİL KAYNAK bulmadan yapılmaz.** Vergi/VUK, KDV (özel matrah 23/e dahil), MASAK, KMTS, tevsik haddi, gider pusulası, e-Fatura/e-Arşiv, defter tutma, kıymetli maden ve has hesabı mevzuatı — **takıldığın HER noktada DUR**: ToolSearch ile WebSearch+WebFetch yükle ("select:WebSearch,WebFetch"), GİB / Resmî Gazete / TÜRMOB / ilgili kurum yayını gibi BİRİNCİL kaynaktan doğrula, bulduğunu URL ve alıntıyla rapora yaz. Forum/blog cevabı kaynak değildir. **Bulamazsan hükmü koda SABİT YAZMA**: yapılandırmadan okunur, varsayılanı TUTUCU/fail-closed olur, rapora "DOĞRULANAMADI — mali müşavir teyidi gerekir" notu düşülür ve eşik sayısı istemciye gömülmez. Tahminle mevzuat kuralı yazmak = paket BAŞARISIZ.
- ⚠ PARALEL ŞERİT KAPISI (orkestratör düzeltmesi, 2026-09-23 19:40 — BAĞLAYICI, ESKİ DoD'U BU MADDE YORUMLAR):
  Aynı ağaçta BİRDEN ÇOK şerit koşuyor. Eski DoD "tam süit 0 başarısız + denetim.sh TEMİZ" diyor; bu AĞAÇ
  ÖLÇÜSÜDÜR, PAKET ölçüsü değildir. Paralel koşumda her paket bunu tek başına sağlayamaz — başka şeridin
  yarım işi tam süiti kırmış olabilir ve devir notu ona dokunmayı YASAKLAR. Bu, paketi sonsuza dek "YARIM"da
  kilitler (23.09'da S1-a'da gerçekleşti: kendi kapsamı yeşil, 7/7 mutasyon kırmızı, 28/28 test geçti, yine de
  BITTI diyemedi). DOĞRU AYRIM:
  · SENİN KAPINCA (paket): (1) dotnet build 0 hata · (2) SENİN testlerin 0 başarısız (filtreli koşum, adlarıyla)
    · (3) SENİN sayaçların denetim.sh'ta ✓ · (4) mutasyon kanıtı (kuralı kır → ADI yazılı test kırmızı → geri al
    → cmp birebir) · (5) canlı doğrulama · (6) test kiracısı silindi · (7) **BU PAKETİN YENİ KIRMIZI TEST
    ÜRETMEDİĞİ**: tam süiti koş, kırmızı listesini devir notundaki listeyle karşılaştır, FARK = senin ürünün.
    Fark 0 ise (7) sağlanmıştır.
  · ORKESTRATÖRÜN KAPISI (dalga): tam süit 0 başarısız + denetim.sh TEMİZ. Bunu şeritler birleşince
    orkestratör ölçer. SEN bunu sağlamakla YÜKÜMLÜ DEĞİLSİN.
  → Yedi maddeyi sağladıysan durum **BITTI** beyan et ve tam süitteki başkasına ait kırmızıları raporun
    "açık kalanlar" bölümüne SAHİBİYLE yaz. Sağlayamadığın bir madde varsa YARIM de. Başkasının kırmızısı
    yüzünden YARIM demek ARTIK YANLIŞTIR.
- Belge güncelleme: ${anaBelge} ve paket spesifikasyonuna DOKUNMA; raporunu ${RAPOR_DIZINI}/PAKET-<no>-uygulama-raporu.md dosyasına yaz (dizini gerekirse oluştur). SONRAKI-OTURUM-PROMPT.md'ye dokunma (orkestratör yazacak).
`

const UYGULAMA = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    durum: { type: 'string', enum: ['BITTI', 'YARIM', 'BASARISIZ'], description: 'BITTI = paketin TAMAMI yapıldı ve DoD karşılandı · YARIM = bir kısmı yapılmadı '+ '(neyin yapılmadığını ozet içinde tek tek say) · BASARISIZ = yapılamadı. ⚠ BU BEYAN BAĞLAYICIDIR: BITTI demezsen doğrulayıcı GECTI dese bile paket GEÇMEZ.' },
    ozet: { type: 'string' },
    derleme: { type: 'string' },
    migration: { type: 'string', description: 'Adı ve kuyumcu_net uygulama kanıtı; yoksa "gerekmedi"' },
    test_kosumlari: { type: 'array', items: { type: 'object', properties: { kapsam: { type: 'string' }, toplam: { type: 'integer' }, basarili: { type: 'integer' }, basarisiz: { type: 'integer' }, atlanan: { type: 'integer' }, kirmizilar: { type: 'array', items: { type: 'string' } } }, required: ['kapsam', 'toplam', 'basarili', 'basarisiz'] } },
    yeni_test_sayisi: { type: 'integer' },
    mutasyonlar: { type: 'array', items: { type: 'object', properties: { kod: { type: 'string' }, degisiklik: { type: 'string' }, kirmizi_testler: { type: 'array', items: { type: 'string' } }, geri_alindi_cmp: { type: 'string' } }, required: ['kod', 'degisiklik', 'kirmizi_testler', 'geri_alindi_cmp'] } },
    denetim_sh: { type: 'string' },
    canli: { type: 'array', items: { type: 'object', properties: { adim: { type: 'string' }, beklenen: { type: 'string' }, gercek: { type: 'string' }, kanit: { type: 'string' } }, required: ['adim', 'beklenen', 'gercek'] } },
    kiraci_sayisi_son: { type: 'integer' },
    degisen_dosyalar: { type: 'array', items: { type: 'string' } },
    kosul_karsilama: { type: 'array', items: { type: 'object', properties: { kosul: { type: 'string' }, durum: { type: 'string' }, kanit: { type: 'string' } }, required: ['kosul', 'durum'] } },
    acik_kalanlar: { type: 'array', items: { type: 'string' } },
    rapor_dosyasi: { type: 'string' },
  },
  required: ['paket_no', 'durum', 'ozet', 'derleme', 'migration', 'test_kosumlari', 'yeni_test_sayisi', 'mutasyonlar', 'denetim_sh', 'canli', 'kiraci_sayisi_son', 'degisen_dosyalar', 'kosul_karsilama', 'acik_kalanlar', 'rapor_dosyasi'],
}
const DOGRULAMA = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    karar: { type: 'string', description: 'GECTI | KALDI' },
    kontroller: { type: 'array', items: { type: 'object', properties: { kontrol: { type: 'string' }, sonuc: { type: 'string', description: 'OK | HATA | YAPILAMADI' }, kanit: { type: 'string' } }, required: ['kontrol', 'sonuc', 'kanit'] } },
    bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, ciddiyet: { type: 'string', description: 'KRITIK | ORTA | DUSUK' }, kanit: { type: 'string' }, oneri: { type: 'string' } }, required: ['baslik', 'ciddiyet', 'kanit'] } },
    test_toplam: { type: 'integer' },
    kor_inceleme: { type: 'string', description: 'ASAMA A — uygulayicinin raporunu ACMADAN, yalniz spesifikasyon + degisen dosyalar + git diff ile yazilan bagimsiz degerlendirme (ne bekledin, ne buldun)' },
    sinir_testleri: { type: 'array', description: 'FINANSAL SINIR TESTLERI — dogrulayicinin KENDI yazip kostugu durumlar; paket para/gram/has/kur/miktar islemiyorsa tek satir: {durum:"KAPSAM_DISI", gerekce}', items: { type: 'object', properties: { durum: { type: 'string' }, girdi: { type: 'string' }, beklenen: { type: 'string' }, gercek: { type: 'string' }, sonuc: { type: 'string', description: 'OK | HATA | KAPSAM_DISI' }, test_adi: { type: 'string' } }, required: ['durum', 'sonuc'] } },
    sahte_yesil: { type: 'string', description: 'Uygulayicinin yeni testlerinden en az 2 tanesinin iddiayi GERCEKTEN olctugu (bos/trivial assert yok, beklenen deger sabit ve dogru) kaniti ya da bulunan sahte yesil' },
  },
  required: ['paket_no', 'karar', 'kontroller', 'bulgular', 'test_toplam', 'kor_inceleme', 'sinir_testleri', 'sahte_yesil'],
}

const HAKEM = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    celiski_var: { type: 'boolean' },
    celiskiler: { type: 'array', items: { type: 'object', properties: { alan: { type: 'string' }, uygulayici: { type: 'string' }, dogrulayici: { type: 'string' }, ciddiyet: { type: 'string', description: 'KRITIK | ORTA | DUSUK' }, aciklama: { type: 'string' } }, required: ['alan', 'ciddiyet', 'aciklama'] } },
    ozet_tamer: { type: 'string', description: 'Orkestratore en fazla 12 satir: degisen dosyalar (git diff ozeti), mutasyon kaniti (sayi + kirmiziya donen test adlari), kabul metrikleri (sayisal), acik kalanlar' },
  },
  required: ['paket_no', 'celiski_var', 'celiskiler', 'ozet_tamer'],
}

const uygulamaPrompt = (p, ekBulgular) => `${KURALLAR}
GÖREV — PAKET ${p.no} "${p.ad}" UYGULAMASI (Taşkın backend + Ertuğ frontend rolü; Opus). Dalga ${dalga}.
ÖNCE OKU (sırayla): (1) ${anaBelge} — özellikle 'Kesişen kararlar', 'Dosya çakışma haritası', Katman 8 UX sözleşmesi; (2) paket spesifikasyonu ${p.belge} — 'Birleşik koşullar' bölümü BAĞLAYICIDIR; (3) spesifikasyonun 'dosya ayak izi'ndeki mevcut dosyalar (gerçek satır numaraları spesifikasyondakinden kaymış olabilir — koda bak); (4) ${KOK}/dotnet/Kuyumcu.Tests/TestOrtami.cs, HttpOrtami.cs ve bir ekran testi (desen); (5) ${KOK}/SONRAKI-OTURUM-PROMPT.md §6 Tuzaklar.
${uxSozlesme ? 'UX SÖZLEŞMESİ (Katman 8, bağlayıcı):\n' + uxSozlesme + '\n' : ''}
${p.kosullar && p.kosullar.length ? 'EK KOŞULLAR (oylamadan):\n' + p.kosullar.map(k => '- ' + k).join('\n') + '\n' : ''}
${ekBulgular ? 'ÖNCEKİ DOĞRULAMADA KALAN BULGULAR (önce bunları kapat, sonra kalan adımları yeniden koş):\n' + ekBulgular + '\n' : ''}
ADIMLAR:
1. Spesifikasyonu koda karşı doğrula: 'mevcut durum' iddiaları hâlâ doğru mu; sapma varsa spesifikasyonun HEDEFİNE sadık kalarak uyarlama yap ve raporda gerekçele. Spesifikasyon bir yasal/mevzuat hükmünü 'DOGRULANMALI/VARSAYIM' işaretlemişse o hükmü koda sabit yazma: yapılandırmadan okunur, varsayılan KAPALI/tutucu; raporda 'kurucuya/mali müşavire' notu düş.
2. Kodu yaz: veri modeli (+ migration prosedürü), servis/kapılar, uçlar (+[RolGerekli]), Sozlukler TR+EN, ekran (+ düzenleme yolu, ProAlan/boş durum/bandlar), sahte API güncellemesi.
3. 'dotnet build Kuyumcu.sln --nologo' → 0 hata (0 uyarı hedefi). Kırmızıysa düzelt; düzelemiyorsan DUR ve raporla.
4. Testler: spesifikasyondaki test listesi (DB/HTTP/bUnit/saf) → filtreli koşumlar yeşil.
5. Mutasyon kanıtları: spesifikasyondaki listenin HER maddesi (kod: M<no>-<sıra>) → kırmızıya dönen testin ADI. Açık varsa testi güçlendir.
6. Tam süit: 'dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory ${SCRATCH}/trx-p${p.no}' → 0 başarısız; kırmızı varsa adını trx'ten al, çöz ya da kararsızlık kanıtıyla raporla. ${KOK}/dotnet/denetim.sh → DENETİM TEMİZ.
7. Canlı doğrulama (5198): spesifikasyonun 'canlı doğrulama' listesi birebir: beklenen HTTP kodları + SQL kanıtı; yanıtları OKU (-o /dev/null yasak). Sonra API'yi durdur, test kiracısını sil (yalnız KENDİ açtığın test kiracısını; 'QA-TEST-' önekli kiracılar kalıcı QA fikstürüdür — ASLA silme/değiştirme, verisine dokunma), tenants sayısını doğrula.
8. Raporu ${RAPOR_DIZINI}/PAKET-${p.no}-uygulama-raporu.md dosyasına yaz: yapılanlar, sapmalar ve gerekçeleri, migration, test sayıları (önce/sonra), mutasyon tablosu, denetim çıktısı, canlı tablo + SQL, değişen dosyalar ('git status --porcelain | grep -v -E "/(bin|obj)/"'), koşul karşılama tablosu, açık kalanlar, kurucuya/mali müşavire notlar.
Yapılandırılmış çıktıyı gerçek değerlerle doldur.`

const dogrulamaPrompt = (p, uyg) => `${KURALLAR}
GÖREV — HASIM DOĞRULAYICI (Aşkın + Mehmet). Paket ${p.no} "${p.ad}". Görevin uygulayıcıyı ÇÜRÜTMEK.
⚠ BAĞLAM YALITIMI (orkestratör 2026-09-24, BAĞLAYICI): uygulayıcının iddialarını bu istemde GÖRMÜYORSUN — bilerek. Yazanın anlatımına kapılmaman için iki aşama:
 AŞAMA A — KÖR İNCELEME: rapor dosyasını AÇMADAN yalnız (1) spesifikasyon ve ek koşullar, (2) değişen dosyalar: ${JSON.stringify((uyg && uyg.degisen_dosyalar) || [])}, (3) 'git diff' ve 'git status --porcelain' (bin/obj hariç) ile kendi değerlendirmeni yaz: spesifikasyon ne istiyor, kod gerçekte ne yapıyor, hangi durum eksik/yanlış olabilir. Bunu 'kor_inceleme' alanına yaz. Build/test/tarayıcı ölçümünü bu aşamada KENDİN koş.
 AŞAMA B — ancak A yazıldıktan sonra uygulayıcının raporunu oku: ${(uyg && uyg.rapor_dosyasi) || '(rapor yolu yok — bu kendi başına bulgu)'} — iddialarını A'daki bulgularınla ve kendi ölçümlerinle karşılaştır; rapordaki her sayı (test sayısı, mutasyon sayısı, kiracı sayısı, ölçüm değeri) yeniden üretilebilir olmalı.
FİNANSAL SINIR TESTLERİ (paket para/tutar/gram/has/milyem/kur/stok miktarı/kasa bakiyesi hesaplayan ya da gösteren bir yola DOKUNDUYSA ZORUNLU): Kuyumcu.Tests/Dogrulayici/${p.no.replace(/[^A-Za-z0-9]/g, '')}SinirTestleri.cs dosyasına KENDİ testlerini yaz ve koş (kalıcı kalır — gerileme testi olur): sıfır · eksi (reddedilmeli ya da işaret doğru) · kuruş yuvarlaması (projede kullanılan MidpointRounding kuralı neyse onunla; 0,005 sınırı) · has/gram 4 hane (0,0001) · çok büyük tutar (999.999.999,99) · boş/null · Türkçe giriş ('45.000', '1,5', belirsiz '1.500' reddi — FormAlani.SayiCoz). Her birini 'sinir_testleri' alanına {durum, girdi, beklenen, gercek, sonuc, test_adi} yaz. Paket bunlara dokunmadıysa tek satır KAPSAM_DISI + gerekçe. HATA çıkan sınır testi = KRITIK bulgu.
SAHTE YEŞİL AVI: uygulayıcının yeni testlerinden en az 2'sini oku: assert gerçekten iddiayı mı ölçüyor (Assert.True(true), yalnız NotNull, beklenen değeri üretim kodundan hesaplayan 'kendini doğrulayan' test = sahte yeşil = KRITIK). Sonucu 'sahte_yesil' alanına yaz.
Spesifikasyon: ${p.belge} (Birleşik koşullar bağlayıcı). Rapor: ${uyg.rapor_dosyasi}
KONTROLLER (komutu koş, gerçek çıktıyı 'kanit'e yaz; çıktı ekonomisi kuralına uy):
1. 'cd ${KOK}/dotnet && dotnet build Kuyumcu.sln --nologo' → 0 hata; uyarı sayısı.
2. Spesifikasyon uyumu: her uç (grep [Http…] + rol), her yeni tablo/kolon (Entities/DbContext/Migration Up gövdesi), her ekran/rota (+UstBar rota başlığı, KenarCubugu yetki), her test dosyası VAR mı; 'Birleşik koşullar'ın her maddesi için kanıt; UX sözleşmesi: yeni form için düzenleme yolu, Sozlukler TR+EN, mevcut id'ler değişmemiş ('git diff' ile kaldırılan id="..." satırı var mı), sabit renk/inline style yok.
3. Güvenlik: yeni varlıklar IKiraciVarlik + tenant indeksi; para el değiştiren yeni akış tevsik+MASAK+gün kilidi+kasa para birimi kapılarından geçiyor mu; 'grep -rn "Hareketler\\.Add(" Kuyumcu.Api --include=*.cs' yalnız HareketFabrikasi.cs; IgnoreQueryFilters/Find 0; ExecuteUpdate/Delete 4; yeni yazma uçlarında [RolGerekli].
4. '${KOK}/dotnet/denetim.sh' → DENETİM TEMİZ.
5. Tam süit BİR koşu (trx → ${SCRATCH}/trx-dogrulama-p${p.no}) → 0 başarısız; toplam sayı; kırmızı varsa adı.
6. Mutasyon örneklemesi: uygulayıcının tablosundan EN KRİTİK 2 mutasyonu KENDİN yeniden uygula (yedek ${SCRATCH}/dogrulama-yedek/p${p.no}/, perl, filtreli test, geri al, cmp) → aynı testler kırmızıya dönmeli; sonra TEMİZ dotnet build.
7. Artık/ayak izi: 'find ${KOK}/dotnet -name "*.mutorj" -newer ${KOK}/dotnet/Kuyumcu.sln', '*.bak', '*.tmp' boş; 'git status --porcelain | grep -v -E "/(bin|obj)/"' beklenmeyen dosya yok; sıfır baytlık yeni migration yok; Data/Migrations/KuyumcuDbContextModelSnapshot.cs son migration ile tutarlı.
8. DB: 'SELECT COUNT(*) FROM tenants' = uygulayıcının beyan ettiği başlangıç sayısı; 'PAKET${p.no}-TEST' kiracısı yok; migration varsa __EFMigrationsHistory'de kayıtlı; 5199 dinliyor, 5198 kapalı.
9. Rapor dosyası var ve canlı tablo + SQL kanıtı içeriyor; yasal hüküm 'DOGRULANMALI' ise koda sabit yazılmamış.
Karar: KRITIK bulgu yoksa GECTI; aksi hâlde KALDI (bulgular dosya:satır / komut çıktısıyla).`

const hakemPrompt = (p, uyg, dog) => `GÖREV — ÇAPRAZ HAKEM (Sonnet, mekanik karşılaştırma; kod YAZMA, komut koşma — yalnız iki raporu karşılaştır). Paket ${p.no} "${p.ad}".
İki ajanın yapılandırılmış çıktıları aşağıda. Aralarındaki SAYISAL ve OLGUSAL çelişkileri avla:
- test sayıları (filtreli / tam süit toplam, başarısız sayısı; biri '0 kırmızı' diyor diğeri kırmızı listeliyor mu)
- mutasyon sayısı ve kırmızıya dönen test adları; doğrulayıcının yeniden ürettiği mutasyon uygulayıcınınkiyle aynı sonucu verdi mi
- para/gram/has değerleri (ör. 10,00 TL ↔ 10,0001; 13,923 ↔ 13,9232), kiracı sayısı (başlangıç/son), port, migration adı/sayısı
- 'değişen dosyalar' listesi ile doğrulayıcının git diff bulgusu; tarayıcı ölçümü (K1..K10) iddiaları ile doğrulayıcının ölçümü
- uygulayıcı 'BITTI' ama koşul tablosunda 'KARŞILANMADI/KISMEN' var mı
- görsel/tarayıcı iddiası: rapor 'tarayıcıda görülmedi / ekran görüntüsü yok / parola girilemedi' diyorsa ama ekran davranışı GEÇTİ sayıldıysa KRITIK (GERÇEK TARAYICI KURALI)
Ciddiyet: karar değiştirebilecek (geçti sayılan bir şey aslında ölçülmemiş/tutmuyor) = KRITIK; açıklama farkı = DUSUK. ⚠ GÜRÜLTÜ AYARI (geriye dönük deneme 24.09; genişletme 30.09): denetim.sh ✗ sayısı farkı BAŞKA şeridin dosyasından geliyorsa (ör. yarım dugme.css, başka sayfadaki record) ve kiracı (tenant) başlangıç/son sayısı farkı paralel şeritlerin kiracılarından geliyorsa = ORTA, KRITIK DEĞİL. iki raporun tam süit sayıları/kırmızı listeleri BAŞKA şeridin (paralel koşum, 'sahibi X şeridi' yazan) testlerinden dolayı farklıysa = ORTA; fark BU PAKETİN kendi testinden ya da kendi dosyasından geliyorsa = KRITIK. Tarayıcıda ölçülmemiş görsel iddia her zaman KRITIK. Tahmin etme — yalnız iki metinde yazanı karşılaştır.
Ayrıca 'ozet_tamer' alanına orkestratör için EN FAZLA 12 satırlık budanmış özet yaz: değişen dosyalar, mutasyon kanıtı, kabul metrikleri (sayısal), açık kalanlar.
UYGULAYICI:
${JSON.stringify(uyg, null, 1).slice(0, 60000)}
DOĞRULAYICI:
${JSON.stringify(dog, null, 1).slice(0, 40000)}`

// ═══ TASARIM HAKEMİ (kurucu 25.09 14:05: "front end'de de çalışsın, kontrol etsin, adam gibi tasarım istiyorum") ═══
const TASARIM = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    karar: { type: 'string', enum: ['GECTI', 'KALDI'] },
    gorulen_goruntu: { type: 'number', description: 'Read ile GERCEKTEN acilip bakilan ekran goruntusu sayisi' },
    olcumler: { type: 'array', items: { type: 'object', properties: { arac: { type: 'string' }, sonuc: { type: 'string' } }, required: ['arac', 'sonuc'] } },
    katalog: { type: 'array', items: { type: 'object', properties: { kod: { type: 'string' }, durum: { type: 'string', enum: ['UYGUN', 'IHLAL', 'KAPSAM_DISI'] }, kanit: { type: 'string' } }, required: ['kod', 'durum', 'kanit'] } },
    bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, ciddiyet: { type: 'string', enum: ['KRITIK', 'YUKSEK', 'ORTA', 'DUSUK'] }, kanit: { type: 'string' }, oneri: { type: 'string' } }, required: ['baslik', 'ciddiyet', 'kanit'] } },
  },
  required: ['paket_no', 'karar', 'gorulen_goruntu', 'olcumler', 'katalog', 'bulgular'],
}
const tasarimPrompt = (p) => `${KURALLAR}
GÖREV — TASARIM HAKEMİ (kurucunun gözü; Opus). Paket ${p.no} "${p.ad}". Kurucu: "adam gibi tasarım istiyorum … bu kadar basit konular için bile test yapacaksam rezil olurum". Hedef kitle ilkokul/lise mezunu tezgâhtar; müşteri "çok karışık, Pusula sade" dedi.
⚠ Uygulayıcının ve doğrulayıcının raporlarını OKUMA. Yalnız: paketin tanımı ve koşulları (aşağıda), çalışan ürün, tuval, katalog.
ADIMLAR: (1) Paketin izolasyon satırındaki portlarda API'yi kendin başlat (dotnet run --no-build), kendi test kiracını gerçekçi veriyle tohumla (uzun adlar, 6-7 haneli ve eksi tutarlar, dolu tablolar), vekille parolasız aç (betikler/inceleme-vekili.py). Tarayıcı bölmesini KULLANMA — ${KOK}/dokumanlar/09-uygulama-kayitlari/betikler/tur/ araçları (BENIOKU.md; TUR_OUT ve TUR_PROFIL mutlak ${SCRATCH} altı, TUR_CDP kendi aralığın): tur.mjs, tutarlilik.mjs, dugme-olc.mjs, arama-dene.mjs — paketin rotalarında KOŞ. (2) SENARYO: ekranı tezgâhtar gibi KULLAN — CDP (betikler/tur/cdp.mjs) ile formu doldur: ürün ekle, ödeme ekle/çıkar, isteğe bağlı "+ … ekle"leri aç, yasal eşiği tetikle, hata üret; her durumda 1440×900 BASİT ve PRO + 375 ekran görüntüsü al. (3) En az 12 görüntüyü Read ile GÖR; tuval ${KOK}/dokumanlar/09-uygulama-kayitlari/ham/TUVAL-png/*.png ile karşılaştır. (4) Kurucu şikâyet kataloğu (${KOK}/dokumanlar/09-uygulama-kayitlari/ham/KURUCU-SIKAYET-KATALOGU.md — K1..K11 ve 25.09 tablosu K12..K23) paketin ekranlarında TEK TEK → 'katalog'. (5) Kurallar: açılışta yalnız zorunlular, geçmiş kapalı akordeon, BASİT↔PRO aynı ölçü, tek birincil düğme, açıklama/teknik metin satırı yok, para biçimi tabloda ve kutuda aynı, düğme yazısı ortada ve zeminden ayırt edilir, katman butona bağlı, jargon/İngilizce/ham kod yok, tuvaldeki tasarım notu kopyalanmamış.
KARAR: kurucuyu demoda utandıracak HER şey KRITIK. (TOKEN TASARRUFU, kurucu 08.10) YALNIZ KRITIK varsa KALDI; YUKSEK/ORTA/DUSUK bulgulari listele ama paketi DUSURMEZ (orkestrator toplu kuyruga alir). Olcumleri (sikisma-olc sayilari) tum kombinasyonlarda yap; Read ile goruntu en az 4, en cok 6. Kapsam dışı ama aynı ekranda gördüğün kusur "KAPSAM DIŞI:" önekiyle yazılır (paketi düşürmez). gorulen_goruntu < 4 ise KALDI. İş bitince kendi API sürecini ve test kiracını temizle.
PAKET TANIMI VE KOŞULLAR:
${(p.kosullar || []).map(k => '- ' + k).join('\n')}`

const sonuclar = []
for (const p of paketler) {
  phase('Uygulama')
  log(`Dalga ${dalga} · Paket ${p.no} ${p.ad} — uygulama başlıyor`)
  let uyg = await agent(uygulamaPrompt(p, null), { label: `uygula:p${p.no}`, phase: 'Uygulama', model: (p.model || (p.hasim === false ? 'sonnet' : 'opus')), effort: 'medium', schema: UYGULAMA })
  let dogrulama = null
  let tur = 0
  while (uyg && tur < 2) { // kurucu 29.09 23:10: en cok 2 tur
    tur++
    phase('Dogrulama')
    // 30.09 kurucu: yalniz gorunum paketlerinde hasim dogrulayici yok (p.hasim === false); para/yasal/guvenlik paketlerinde VAR
    dogrulama = p.hasim === false
      ? { paket_no: p.no, karar: 'GECTI', bulgular: [], test_toplam: 0, hasimsiz: true, not: 'HASIMSIZ PAKET (gorunum) — kalite: uygulayicinin tam suit + denetim + mutasyon + tasarim hakemi' }
      : await agent(dogrulamaPrompt(p, uyg), { label: `dogrula:p${p.no}:t${tur}`, phase: 'Dogrulama', model: 'opus', effort: 'medium', schema: DOGRULAMA })
    if (!dogrulama) { log(`Paket ${p.no}: doğrulayıcı dönmedi`); break }
    log(`Paket ${p.no} doğrulama turu ${tur}: ${dogrulama.karar} · bulgu ${dogrulama.bulgular.length} · test toplam ${dogrulama.test_toplam}`)
    // ═══ ÇAPRAZ HAKEM (2026-09-24, kurucu onayı) — yalnız GECTI'yi sınar; paketi DAHA SIKI yapabilir, gevşetemez ═══
    if (dogrulama.karar === 'GECTI' && false) { // 30.09 kurucu hiz: capraz hakem kaldirildi (gurultu)
      phase('Hakem')
      const hakem = await agent(hakemPrompt(p, uyg, dogrulama), { label: `hakem:p${p.no}:t${tur}`, phase: 'Hakem', model: 'sonnet', effort: 'low', schema: HAKEM })
      dogrulama.hakem = hakem
      const kritikCeliski = hakem && hakem.celiskiler ? hakem.celiskiler.filter(c => String(c.ciddiyet).toUpperCase() === 'KRITIK') : []
      if (hakem) log(`Paket ${p.no} hakem turu ${tur}: çelişki ${hakem.celiskiler.length} (KRITIK ${kritikCeliski.length})`)
      if (kritikCeliski.length) {
        dogrulama.karar = 'KALDI'
        dogrulama.bulgular = dogrulama.bulgular.concat(kritikCeliski.map(c => ({ baslik: `HAKEM ÇELİŞKİSİ — ${c.alan}`, ciddiyet: 'KRITIK', kanit: `uygulayıcı: ${c.uygulayici || '-'} · doğrulayıcı: ${c.dogrulayici || '-'} · ${c.aciklama}`, oneri: 'Sayıyı yeniden ölç, iki rapor aynı gerçeği yazsın' })))
      }
    }
    if (dogrulama.karar === 'GECTI' && p.ekran !== false) {
      phase('Tasarim')
      const th = await agent(tasarimPrompt(p), { label: `tasarim:p${p.no}:t${tur}`, phase: 'Tasarim', model: (p.model || 'sonnet'), effort: 'medium', schema: TASARIM })
      dogrulama.tasarim = th
      const agir = th && th.bulgular ? th.bulgular.filter(b => ['KRITIK'].includes(String(b.ciddiyet).toUpperCase()) && !String(b.baslik).startsWith('KAPSAM DIŞI')) : []
      if (!th || th.karar !== 'GECTI' || (th.gorulen_goruntu || 0) < 4 || agir.length) {
        dogrulama.karar = 'KALDI'
        dogrulama.bulgular = dogrulama.bulgular.concat(th ? agir.map(b => ({ baslik: `TASARIM HAKEMİ — ${b.baslik}`, ciddiyet: 'KRITIK', kanit: b.kanit, oneri: b.oneri || '' })) : [{ baslik: 'TASARIM HAKEMİ dönmedi', ciddiyet: 'KRITIK', kanit: 'ajan sonucu yok', oneri: 'yeniden koş' }])
        if (th && (th.gorulen_goruntu || 0) < 4) dogrulama.bulgular.push({ baslik: 'TASARIM HAKEMİ yeterli görüntüye bakmadı', ciddiyet: 'KRITIK', kanit: `gorulen_goruntu=${th.gorulen_goruntu}`, oneri: 'en az 4 görüntü' })
        log(`Paket ${p.no} tasarım hakemi turu ${tur}: KALDI (ağır ${agir.length}, görüntü ${th ? th.gorulen_goruntu : 0})`)
      } else log(`Paket ${p.no} tasarım hakemi turu ${tur}: GECTI (görüntü ${th.gorulen_goruntu})`)
    }
    if (dogrulama.karar === 'GECTI' || tur === 2) break
    const kritik = dogrulama.bulgular.map(b => `- [${b.ciddiyet}] ${b.baslik}: ${b.kanit}${b.oneri ? ' → ' + b.oneri : ''}`).join('\n')
    phase('Uygulama')
    uyg = await agent(uygulamaPrompt(p, kritik), { label: `duzelt:p${p.no}:t${tur}`, phase: 'Uygulama', model: (p.model || (p.hasim === false ? 'sonnet' : 'opus')), effort: 'medium', schema: UYGULAMA })
  }
  // ═══ UYGULAYICININ KENDİ BEYANI DOĞRULAYICIYI BAĞLAR (2026-09-21) ═══════
  // ARIZA 13: P-36'da uygulayıcı `durum: "YARIM"` dedi, yapmadıklarını tek tek
  // saydı ("176 select'in 174'ü göç etmedi, girdi ailesi yazılmadı, kabul eşiği
  // ÖLÇÜLMEDİ → karşılanmadı sayılır, paket KAPANMADI") — ve hasım doğrulayıcı
  // buna rağmen GECTI dedi. Kurucu ekranda arayıp bulamadı; haklıydı.
  // Kural: uygulayıcı "BITTI" demediyse paket GEÇEMEZ. Doğrulayıcı bir paketi
  // yalnız DAHA SIKI yapabilir, daha gevşek YAPAMAZ.
  const beyan = (uyg && uyg.durum ? String(uyg.durum) : '').toUpperCase()
  const uygulayiciBitti = beyan === 'BITTI' || beyan === 'BİTTİ'
  if (dogrulama && dogrulama.karar === 'GECTI' && !uygulayiciBitti) {
    log(`Paket ${p.no}: doğrulayıcı GECTI dedi ama UYGULAYICI "${beyan || 'beyan yok'}" dedi — ` +
        `BEYAN BAĞLAYICIDIR, paket GEÇMEDİ sayılıyor (orkestratör kuralı).`)
  }
  const gecti = !!(dogrulama && dogrulama.karar === 'GECTI' && uygulayiciBitti)
  sonuclar.push({ paket: p.no, ad: p.ad, gecti, tur, uygulama: uyg, dogrulama })
  if (!gecti) { log(`Paket ${p.no} KAPIDAN GEÇMEDİ — dalga durduruldu (orkestratör denetimi gerekli)`); break }
}
return { dalga, sonuclar, tamamlanan: sonuclar.filter(s => s.gecti).map(s => s.paket), duran: sonuclar.find(s => !s.gecti)?.paket || null }
