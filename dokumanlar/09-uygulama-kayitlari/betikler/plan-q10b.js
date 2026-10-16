export const meta = {
  name: 'kuyumcu-demo-q10b',
  description: 'Bir uygulama dalgasinin paketlerini SERI olarak Opus uygulayici + hasim dogrulayici + duzeltme donguleriyle uygular; kapi gecmeyen paket dalgayi durdurur',
  phases: [
    { title: 'Uygulama', detail: 'Opus uygulayici: spesifikasyon + kosullar + UX sozlesmesi; test, mutasyon, denetim, canli', model: 'opus' },
    { title: 'Dogrulama', detail: 'Hasim dogrulayici: once KOR inceleme (yalniz spesifikasyon + degisen dosyalar), sonra rapor; finansal sinir testleri' },
    { title: 'Hakem', detail: 'Capraz hakem (Sonnet): iki raporun sayisal celiskilerini avlar; KRITIK celiski paketi o tur gecirmez', model: 'sonnet' },
  ],
}
const GOMULU = {"dalga": "DEMO DUZELTME — Q10-B (satis/panel/arama + ortak form/jargon/tablo/banka ekstre; v2 · vt _i · 5178/5177)", "ux_sozlesme": "### Ek UX sözleşmesi — mantık ve tutarlılık taraması (2026-09-16)\n- UX-S21 — Sistemin bildiği alan kullanıcıdan istenmez: sunucuda hesaplanabilen veya kayıtlı olan hiçbir değer (canlı has fiyatı, has gram = ayar+gram, para birimi = kasadan, vergi no = cariden, şube = oturumdan, KDV türü = ürün tipinden) boş giriş alanı olarak konmaz; türetilir, doldurulur ve nereden geldiği rozetle söylenir.\n- UX-S22 — Tek seçenek varsa seçici gizlenir: tek kasa, tek şube, tek liste durumunda seçici gösterilmez; değer salt okunur satır olarak yazılır ve gövdeye otomatik konur. Aynı ekranda aynı sorunun iki farklı cevabı olamaz (POS'ta bacak kasası otomatik seçilirken ana kasanın seçilmemesi gibi).\n- UX-S23 — Soyut kayıt kaydedilince etkisi aynı ekranda gösterilir: fiyat listesi, kampanya, kategori, reyon, yetki gibi dolaylı kayıtların sonucu (etkilenen ürünler, hesaplanan fiyat, etkilenen adet) kaydetmeden önce önizlenir ve kaydettikten sonra aynı ekranda görünür. Sayı gösteren her yer (ürün sayısı, gecikmiş adet, maliyeti bilinmeyen adet) o kayıtlara giden bağlantı olur.\n- UX-S24 — Bağımlı alanın bağını ekran kurar: cari değişince fiyat teklifi yenilenir (sessizce değil, 'liste değişti, güncelle' düğmesiyle), iade kaynağı seçilince cari dolar ve kilitlenir, ödeme türü KART olunca komisyon alanı açılır, KDV türü KUYUM olunca oran sabitlenir.\n- UX-S25 — Oluşturulabilen her kayıt düzeltilebilir ya da neden düzeltilemediği yazar: ya düzenle/pasife al/aktifleştir yolu vardır, ya da DegismezIpucu ile değişmezlik gerekçesi ve doğru düzeltme yolu (ters hareket, düzeltme fişi, storno) anlatılır. 'Sunucuda uç var, ekranda düğme yok' kabul edilmez.\n- UX-S26 — Oluşturma formu düzenleme formuyla aynı alan kümesini taşır: sunucunun oluşturmada kabul ettiği alan 'sonra düzenlemeden gir' diye ertelenmez; seçenek listeleri iki formda tek diziden üretilir.\n- UX-S27 — Kaydet sonrası sonuç görünür: her yazma işlemi başarı bandıyla biter ve sunucunun döndürdüğü sonuç (fiş no, fatura no, uygulanan/atlanan satır, yeni bakiye) banda yazılır; POST yanıtı okunmadan atılmaz. Geri alınamaz işlem (iptal, maskeleme, düzeltme fişi, sayım iptali) önce onay ister.\n- UX-S28 — Hata 'kayıt yok'a çevrilmez: catch içinde listeyi boşaltıp boş durum göstermek yasaktır (Anayasa md.1). Boş durum yalnız istek başarılı ve sonuç gerçekten boşken çıkar; hata bandı ayrıdır ve kullanıcının göreceği yerdedir.\n- UX-S29 — Sunucu sınırı ekranda söylenir: limitli her liste (ilk 500 ürün, 50 cari, 100 fatura) sınırını ve daraltma yolunu yazar, sınıra değdiyse 'daha fazlası var' uyarısı basar. Sessiz kırpma yok.\n- UX-S30 — Kilitli düğme sebebini söyler: disabled bir eylemin yanında hangi koşulun eksik olduğu yazılır (kur yok, kasa seçilmedi, MASAK formu eksik, gerekçe n/20 karakter). Görünmez kural olmaz.\n- UX-S31 — Aynı kavram tek ad, tek dil, tek kaynak: ham veritabanı kodu (ACIK, TEZGAHTAR, A22, MUSTERI) ekrana basılmaz, sözlükten çevrilir; aynı kavram iki ekranda iki ad taşımaz ('Kategori' hem ürün cinsi hem ağaç kategorisi olamaz); ayar/rol/durum listeleri tek kaynaktan beslenir ve eksik anahtar derleme hatası verir.\n- UX-S32 — Para birimi tutarla birlikte taşınır: hiçbir tutar 'varsayılan TL' sayılıp ₺ ile basılmaz; Bicim.Para(tutar, paraBirimi) kullanılır, toplam kartları hangi para birimini topladığını etiketinde söyler.\n- UX-S33 — Dönemden bağımsız kart bunu yazar: üstteki tarih/dönem seçicisinin etkilemediği kartlar '(bugün itibarıyla — dönemden bağımsız)' alt metnini taşır.\n\n", "paketler": [{"no": "Q10-4", "ad": "SATIS + PANEL + ARAMA KALANLARI: fis no kalici degil (S1-i madde 9 etkisiz), fiste VERDIM sutunu dibe hizali, Panel altin karti tuvalden kalabalik, Ctrl+K kayit bulunca 'Eslesen kayit yok' diyor, S1-j DUSUK'leri", "belge": "/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-10-bulgular.json", "kosullar": ["KAYNAK: QA turu 10 (24.09, gercek tarayici, 47/47 rota, gorsel 4,06 / KD 3,62) — bulgular + kanit + oneri: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-10-bulgular.json (rota alanina gore suz, ILGILI KAYITLARI OKU). Katalog: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/KURUCU-SIKAYET-KATALOGU.md — K1..K11 bu pakette de gecerli ('sikisma-olc.js' K8 artik buyuk/kucuk harf duyarsiz + kebab-case kod adlari). ONCE bulguyu KENDI olcumunle yeniden uret (ekran + API + SQL); uretilemiyorsa kanitla 'KAPANMIS' yaz, kod YAZMA. NOT: QA 10 hedefi KATALOG sonrasi/S1-j ONCESI kopyaydi — hava CSP ihlali ve menu aramasi S1-j'de kapandi; /hubs/kur SignalR 404 inceleme vekilinin WebSocket tasimamasindan (urun hatasi degil) — bunlari yeniden acma.", "KAPSAM: (1) /islemler: kaydedilen fisin numarasi + 'Fis yazdir' baglantisi bir sonraki Kaydet'e kadar sayfada KALIR (S1-i (9) neden etkisiz — OLC; S2-5b #islem-yazdir ile birlestir). (2) Fis govdesinde VERDIM ve ALDIM sutunlari USTTEN hizali (tuval /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/02-mimari/sadelestirme/tuval/project/Tezgah.dc.html); bos VERDIM kutusu gereksiz yukseklik birakmaz. (3) Panel BASIT 'Altin pozisyonum' karti tuvale BIREBIR: buyuk sayi '+N g has' + tek satir 'Vitrin X g · Alacak Y g' (tuval Panel.dc.html); hurda, emanet notu, diger madenler, 'bugun itibariyla' notu ve DegismezIpucu kutusu BASIT yuzde gorunmez — PRO yuzde ya da kartin 'Ayrinti' baglantisinda (ozellik silinmez). Uc kart esit ve kisa. (4) Ctrl+K paleti kayit bulundugunda 'Eslesen kayit ya da ekran yok' YAZMAZ (bos durum yalniz gercekten bos). (5) S1-j DUSUK: hava hatasi da onbelleklenir; Ctrl+K belge dinleyicisi Dispose'da kaldirilir; 375 BASIT Panel'de hava parcasi '· ' ile yeni satira dusmez; olu Hava:KaynakOrigin ayari kalkar; Form/Arama CaptureUnmatchedValues yazim hatasini sessizlestirmez (bilinen parametre listesi ya da derleme zamani).", "DOSYA ALANI: Pages/Islemler.razor (yalniz fis no), Bilesenler/Fis/*, Pages/Panel.razor, Bilesenler/AltinPozisyonuKarti.razor, Bilesenler/GlobalArama.razor, Bilesenler/Form/Arama.razor, Kuyumcu.Web/Servisler/HavaServisi.cs, Kuyumcu.Api hava onbellegi, appsettings (yalniz Hava:KaynakOrigin), wwwroot/css/app.css (yalniz SONA Q10-B blogu) ve bunlarin testleri. ESZAMANLI SERIT Q10-A (DOKUNMA): Pages/Gider.razor, Pages/Hurda.razor, Pages/Isletme.razor ve Modules/Gider, Modules/Hurda, Modules/Isletme (ya da tenant/isletme uclari). app.css bu seridin (yalniz dosya SONUNA Q10-B blogu).", "KABUL (GERCEK TARAYICI, 1280+375, BASIT+PRO, acik+koyu; ekran goruntusu once/sonra): tarayicida: Kaydet sonrasi fis no + yazdir baglantisi 10 sn sonra da gorunur; VERDIM/ALDIM baslik top farki ≤2 px; Panel altin karti BASIT'te 2 satir (tuval) ve uc kart yuksekligi ±8 px; Ctrl+K 'Anadolu' → kayit listelenir, bos durum metni YOK; 375 Panel selamlama tek satir. sikisma-olc K1..K11 = 0.", "KURALLAR: ozellik silinmez; DOM id ve rota degismez; yetki != mod; sabit renk/inline style yok; Sozlukler HUB'ina dokunulmaz (Sozlukler.<Alan>.cs); tek yazma yolu (HareketFabrikasi/OdemeKalemFabrikasi) atlanmaz; mevzuat gerektiren kilit acilmaz — MEVZUAT KURALI.", "KURUCU IZNI (23.09): bu projenin veritabanindaki SATIRLAR komutla silinebilir; DOSYA silme YASAK.", "⚠ IZOLASYON: KUYUMCU_TEST_DB=kuyumcu_test_i · canli API 5178 · vekil 5177. 5173/5174 (Q10-A), 5179/5181, 5199/5200 portlarina DOKUNMA; tarayicida KENDI sekmen."]}, {"no": "Q10-5", "ad": "ORTAK FORM HATASI + KALAN JARGON + TABLO/MOBIL + BANKA EKSTRE: alan hatasi duzeltilince kalkmiyor (5 ekran), Kilavuz 'Cariler', veri disa aktarimda kebab kod adlari, tablo basliklari kirik, /cariler ve /sayim mobil tablo tasmasi, /denetim-izi tarih hizasi, banka ekstre binme ve yanlis 'Eslesti' rozeti", "belge": "/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-10-bulgular.json", "kosullar": ["KAYNAK: QA turu 10 (24.09, gercek tarayici, 47/47 rota, gorsel 4,06 / KD 3,62) — bulgular + kanit + oneri: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-10-bulgular.json (rota alanina gore suz, ILGILI KAYITLARI OKU). Katalog: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/ham/KURUCU-SIKAYET-KATALOGU.md — K1..K11 bu pakette de gecerli ('sikisma-olc.js' K8 artik buyuk/kucuk harf duyarsiz + kebab-case kod adlari). ONCE bulguyu KENDI olcumunle yeniden uret (ekran + API + SQL); uretilemiyorsa kanitla 'KAPANMIS' yaz, kod YAZMA. NOT: QA 10 hedefi KATALOG sonrasi/S1-j ONCESI kopyaydi — hava CSP ihlali ve menu aramasi S1-j'de kapandi; /hubs/kur SignalR 404 inceleme vekilinin WebSocket tasimamasindan (urun hatasi degil) — bunlari yeniden acma.", "KAPSAM: (1) ORTAK: alan hatasi, alanin degeri degisince (input) o alan icin temizlenir — ortak form durumu (Bilesenler/Form/*, FormDurumu) tek yerden; /fiyatlandirma, /urunler, /vezne, /islemler'de dogrula (Hurda Q10-A'da ayni cozumu kullanir); /banka IBAN 400 sonrasi alan isaretlenir. (2) JARGON (BASIT): /kilavuz 'Cariler' → 'Musteriler'; /veri-disa-aktarim onay kutularinda kebab-case kod adlari sozluk adlariyla; vezne/faturalar/kurulum/pos/kar-zarar govdesindeki kalan jargon (yeni K8 olcumuyle bul). (3) TABLO BASLIKLARI: 1280'de tek kelime kirilmaz; iki kelimelik th okunur (kisaltma + title ya da nowrap + sutun genisligi) — /kullanicilar 'IKI ASAMALI', /urunler/agac 'REYON TIPI', 'YERLESIK ADET' ve bulgu dosyasindaki diger 7 rota. (4) MOBIL: /cariler (975 px) ve /sayim (513 px) tablo tasmasi — 375'te kart/yigin gorunumu ya da kaydirilabilir sarmal + yatay sayfa kaydirmasi 0. (5) /denetim-izi tarih kutusu 15 px hizasiz → hizali. (6) /banka/ekstre: karsi taraf adi ile IBAN etiketi binmez; 'Atla' sonrasi rozet 'Atlandi' (Eslesti degil); 'Yeni' durumdaki sahne satirinda tarih/tutar/yon/aciklama/karsi taraf duzenlenebilir ya da gerekceli DegismezIpucu.", "DOSYA ALANI: Bilesenler/Form/*, Servisler/FormDurumu.cs, Pages/Kilavuz.razor, Pages/VeriDisaAktarim.razor, Pages/Vezne.razor, Pages/Faturalar.razor, Pages/Kurulum.razor, Pages/Pos.razor, Pages/KarZarar.razor, Pages/Kullanicilar.razor, Pages/UrunAgaci.razor, Pages/Cariler.razor, Pages/Sayim.razor, Pages/DenetimIzi.razor, Pages/Banka*.razor + Modules/Banka (yalniz ekstre sahne satiri duzenleme gerekirse), Pages/Fiyatlandirma.razor, Pages/Urunler.razor, wwwroot/css/app.css (yalniz SONA Q10-B blogu) ve bunlarin testleri. ESZAMANLI SERIT Q10-A (DOKUNMA): Pages/Gider.razor, Pages/Hurda.razor, Pages/Isletme.razor ve Modules/Gider, Modules/Hurda, Modules/Isletme (ya da tenant/isletme uclari). app.css bu seridin (yalniz dosya SONUNA Q10-B blogu).", "KABUL (GERCEK TARAYICI, 1280+375, BASIT+PRO, acik+koyu; ekran goruntusu once/sonra): tarayicida bulgu dosyasindaki her Q10-5 bulgusu yeniden olculur ve 0; alan hatasi 5 ekranda duzeltmeyle kalkar; K8 (yeni regex) BASIT modda tum rotalarda 0; tablo basliklarinda tek kelime kirilmasi 0; 375'te yatay kaydirma 0 (/cariler, /sayim); binen 0.", "KURALLAR: ozellik silinmez; DOM id ve rota degismez; yetki != mod; sabit renk/inline style yok; Sozlukler HUB'ina dokunulmaz (Sozlukler.<Alan>.cs); tek yazma yolu (HareketFabrikasi/OdemeKalemFabrikasi) atlanmaz; mevzuat gerektiren kilit acilmaz — MEVZUAT KURALI.", "KURUCU IZNI (23.09): bu projenin veritabanindaki SATIRLAR komutla silinebilir; DOSYA silme YASAK.", "⚠ IZOLASYON: KUYUMCU_TEST_DB=kuyumcu_test_i · canli API 5178 · vekil 5177. 5173/5174 (Q10-A), 5179/5181, 5199/5200 portlarina DOKUNMA; tarayicida KENDI sekmen."]}]};

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SCRATCH = '/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad'
const RAPOR_DIZINI = `${KOK}/dokumanlar/09-uygulama-kayitlari`
const paketler = GOMULU.paketler
const dalga = GOMULU.dalga
const anaBelge = GOMULU.ana_belge || `${KOK}/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`
const uxSozlesme = GOMULU.ux_sozlesme || ''

const KURALLAR = `
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

- ⚠ GERÇEK TARAYICI KURALI (kurucu talimatı 2026-09-24, BAĞLAYICI — MEVZUAT KURALI ile aynı ağırlıkta): **Ekrana dokunan hiçbir iş (razor, css, bileşen, biçim, sözlük, dil verisi) gerçek tarayıcıda ölçülüp ekran görüntüsüyle kanıtlanmadan BİTTİ/GEÇTİ sayılmaz.** bUnit/API/SQL kanıtı görünürlük, sığma, taşma ve biçim için YETMEZ (24.09'da üç kusur testten yeşil geçip kurucunun gözüne takıldı: menüde gizli 33 satır görünüyordu, Ayarlar kartı dar sütuna sıkışmıştı, İngilizce tarayıcıda kur 'TRY6,754.91' çıkıyordu). Zorunlu ölçüm, değişen HER ekran için: 1280×800 ve 375×812 · BASİT ve PROFESYONEL mod · açık ve koyu tema. Ölçüm aracı: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js (javascript_tool ile çalıştır) → taşan yazı 0 · üst üste binen 0 · tek kelimesi kırılan kısa etiket 0 · yatay kaydırma 0 · '[hidden]' olup görünen 0 · para/sayı tr-TR ('TRY' kalıbı 0) · dar sütun (en geniş blok ana alanın %60'ından azsa gerekçe). Oturum parola YAZMADAN açılır: API'ni kendi portunda aç, test kiracısını API ile kur, {email,sifre} JSON'u chmod 600 yaz, 'python3 .../betikler/inceleme-vekili.py <fikstur.json> <api_port> <vekil_port>' çalıştır, tarayıcı bölmesinde tabs_create ile KENDİ sekmeni aç ve http://localhost:<vekil_port>/__giris adresine git. Başka sekmelere dokunma. Rapora: rota × genişlik × mod ölçüm tablosu + ekran görüntüsü. DOĞRULAYICI: ölçüm tablosu ve görüntü yoksa ya da ölçümü kendi sekmesinde tekrar ettiğinde sonuç tutmuyorsa KALDI verir.\n- MEVZUAT KURALI (kurucu talimatı 2026-09-23, BAĞLAYICI): **Mevzuatla ilgili hiçbir işlem, internetten araştırıp BİRİNCİL KAYNAK bulmadan yapılmaz.** Vergi/VUK, KDV (özel matrah 23/e dahil), MASAK, KMTS, tevsik haddi, gider pusulası, e-Fatura/e-Arşiv, defter tutma, kıymetli maden ve has hesabı mevzuatı — **takıldığın HER noktada DUR**: ToolSearch ile WebSearch+WebFetch yükle ("select:WebSearch,WebFetch"), GİB / Resmî Gazete / TÜRMOB / ilgili kurum yayını gibi BİRİNCİL kaynaktan doğrula, bulduğunu URL ve alıntıyla rapora yaz. Forum/blog cevabı kaynak değildir. **Bulamazsan hükmü koda SABİT YAZMA**: yapılandırmadan okunur, varsayılanı TUTUCU/fail-closed olur, rapora "DOĞRULANAMADI — mali müşavir teyidi gerekir" notu düşülür ve eşik sayısı istemciye gömülmez. Tahminle mevzuat kuralı yazmak = paket BAŞARISIZ.
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
Ciddiyet: karar değiştirebilecek (geçti sayılan bir şey aslında ölçülmemiş/tutmuyor) = KRITIK; açıklama farkı = DUSUK. ⚠ GÜRÜLTÜ AYARI (geriye dönük deneme 24.09): iki raporun tam süit sayıları/kırmızı listeleri BAŞKA şeridin (paralel koşum, 'sahibi X şeridi' yazan) testlerinden dolayı farklıysa = ORTA; fark BU PAKETİN kendi testinden ya da kendi dosyasından geliyorsa = KRITIK. Tarayıcıda ölçülmemiş görsel iddia her zaman KRITIK. Tahmin etme — yalnız iki metinde yazanı karşılaştır.
Ayrıca 'ozet_tamer' alanına orkestratör için EN FAZLA 12 satırlık budanmış özet yaz: değişen dosyalar, mutasyon kanıtı, kabul metrikleri (sayısal), açık kalanlar.
UYGULAYICI:
${JSON.stringify(uyg, null, 1).slice(0, 60000)}
DOĞRULAYICI:
${JSON.stringify(dog, null, 1).slice(0, 40000)}`

const sonuclar = []
for (const p of paketler) {
  phase('Uygulama')
  log(`Dalga ${dalga} · Paket ${p.no} ${p.ad} — uygulama başlıyor`)
  let uyg = await agent(uygulamaPrompt(p, null), { label: `uygula:p${p.no}`, phase: 'Uygulama', model: 'opus', effort: 'medium', schema: UYGULAMA })
  let dogrulama = null
  let tur = 0
  while (uyg && tur < 3) {
    tur++
    phase('Dogrulama')
    dogrulama = await agent(dogrulamaPrompt(p, uyg), { label: `dogrula:p${p.no}:t${tur}`, phase: 'Dogrulama', effort: 'medium', schema: DOGRULAMA })
    if (!dogrulama) { log(`Paket ${p.no}: doğrulayıcı dönmedi`); break }
    log(`Paket ${p.no} doğrulama turu ${tur}: ${dogrulama.karar} · bulgu ${dogrulama.bulgular.length} · test toplam ${dogrulama.test_toplam}`)
    // ═══ ÇAPRAZ HAKEM (2026-09-24, kurucu onayı) — yalnız GECTI'yi sınar; paketi DAHA SIKI yapabilir, gevşetemez ═══
    if (dogrulama.karar === 'GECTI') {
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
    if (dogrulama.karar === 'GECTI' || tur === 3) break
    const kritik = dogrulama.bulgular.map(b => `- [${b.ciddiyet}] ${b.baslik}: ${b.kanit}${b.oneri ? ' → ' + b.oneri : ''}`).join('\n')
    phase('Uygulama')
    uyg = await agent(uygulamaPrompt(p, kritik), { label: `duzelt:p${p.no}:t${tur}`, phase: 'Uygulama', model: 'opus', effort: 'medium', schema: UYGULAMA })
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
