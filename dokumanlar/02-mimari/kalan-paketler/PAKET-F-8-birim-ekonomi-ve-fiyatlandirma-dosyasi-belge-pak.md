# Paket F-8 — Birim ekonomi ve fiyatlandırma dosyası (BELGE paketi)

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: Faz A 1 gün (belge yazımı + canlı SQL ölçümleri) · Faz B 0,5 gün yazım, ancak Kurucu'nun teklif dönüşüne bağlı (takvim garanti edilemez) · Toplam ajan işi ~1,5 gün; kod hattını bloklamaz, dalga 3'e paraleldir. gün · Önkoşullar: Kurucu tedariki (BLOKLAYICI, kod değil): üç entegratörden yazılı kontör teklifi, mali mühür/e-imza bedeli, yaptırım listesi sağlayıcı bedeli — MIMARI-KALAN-PAKETLER-2026-09-15.md:458-467 'Kurucuya kalanlar'., F-4 (kiracı etiketli ölçüm) — altyapı satırlarındaki TAHMİN etiketlerini kanıta çevirir. F-4 gelmeden Faz A yazılabilir, Faz B tam kapanamaz., dotnet/yuk-testi dizini sonuçları — kapasite (tek VPS'te kaç kiracı) iddiasının tek meşru kaynağı; bu turda OKUNMADI (HAZIRLIK-KURULU-1.md:250)., PAZAR-ARASTIRMASI-2026-09-16.md (BİTTİ) — rakip fiyat satırlarının birincil kaynağı; yalnız yayımlı rakamlar (Dezy 500/1.000/2.500 TL/ay, M&L 94.686,48 TL, CetaSoft 40.000 TL+KDV, Kuyumcum 30-40 $/kullanıcı/ay, Terminal Gold 900-9.000 $) kullanılır., T-1/F-6 (üretim profili) ve F-7 (TLS) — barındırma satırının gerçek kaynak/sertifika maliyetini netleştirir; gelmeden önce bu satır 'bugünkü docker-compose profili' varsayımıyla yazılır ve varsayım açıkça etiketlenir., F-9 (kur kaynak dayanıklılığı) — kur verisinin ücretli sürüme geçme riski maliyet satırı olarak F-8'de durur, teknik çözüm F-9'dadır., S-1/SATIS-1 (dalga 6) — F-8'in ARDILI, önkoşulu değil: fiyat kademesini yayımlayan pakettir ve F-8 mührü olmadan yayımlanamaz. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Kiracı başı gerçek maliyeti (altyapı + dış tedarikçi + destek) tek bir kanıtlı tabloda toplayıp brüt marjı hesaplanabilir kılmak; üç kademeli fiyat önerisini bu tabloya dayandırmak; tablo dolmadan fiyat ilanını kurumsal olarak kilitlemek (Madde 1). Kod üretilmez, belge üretilir.

## Kodda bugünkü durum
Bugün ağaçta birim ekonomiyi taşıyan hiçbir mekanizma yok. Tenant.Plan alanı var ama karar vermiyor: Entities.cs:52-56 `Plan = \"BASLANGIC\"` varsayılanı; planı okuyan tek yer ekran (Kuyumcu.Web/Pages/Isletme.razor:47-51 seçim kutusu, :205 TenantVeri kaydı). Kayıt ucu açık ve süresiz: AuthModulu.cs:87 `Kayit`, :323-326 yalnız [EnableRateLimiting(OranSiniri.Kayit)] — deneme bitişi, kota, ödeme yok. Ölçüm yok: Program.cs:219 `AddHealthChecks()` ve :313 `MapHealthChecks(\"/saglik\")` dışında telemetri yok (OpenTelemetry/Prometheus eşleşmesi 0) → kiracı başı CPU/disk/istek BİLİNMİYOR. Dış kalemlerin kodda karşılığı sıfır: 'Entegrator|Kontor|MaliMuhur|Yaptirim' grep'i dotnet/ altında 0 eşleşme. Kur verisi üç ücretsiz, anahtarsız, sözleşmesiz uca bağlı: KurModulu.cs:292 TruncgilAdaptoru, :362 AltinkaynakAdaptoru, :452 AnkaraAltinBorsasiAdaptoru. Lisans gideri kanıtlı 0: Kuyumcu.Api.csproj:10-17 sekiz paketin tamamı izinli açık kaynak. Barındırma tek profil: docker-compose.yml (mysql:8.4, tek servis, parola düz metin satır 7, 3306 dışa açık). Yedek betiği var ama zamanlanmamış: dotnet/yedek-al-ve-dogrula.sh. Depoda fiyat/teklif/sözleşme belgesi yok (dokumanlar/09-uygulama-kayitlari listesi: yalnız paket raporları + QA çizelgesi + kurul).

## Kapsam
**İçinde:**
- Kiracı başı maliyet tablosu: altyapı (VPS/CPU/RAM/disk/yedek/trafik) + dış tedarikçi (e-Fatura kontörü, mali mühür, yaptırım listesi, kur verisi, alan adı/TLS) + destek (saat × saatlik maliyet) kalemlerinin tek tabloda toplanması.
- Üç e-Fatura/e-Arşiv entegratöründen YAZILI kontör teklifi talebi taslağı (paket fiyatı, kontör birim bedeli, aylık taban ücret, e-Arşiv/e-İrsaliye farkı, kontör devri, iptal şartı) — teklif toplama Kurucu'da.
- Mali mühür / e-imza sertifikası yıllık bedeli ve yenileme periyodunun kayda geçirilmesi (kiracı başına mı, bizde mi sorusunun cevabıyla birlikte).
- Yaptırım listesi (OFAC/BM/AB) veri sağlayıcı aboneliğinin yıllık bedeli ve ücretsiz resmî kaynak alternatifinin maliyet karşılaştırması.
- Kur verisi maliyet satırı: bugün üç ücretsiz/sözleşmesiz kaynak kullanılıyor (KurModulu.cs:292/362/452); ticari sözleşmeye geçilirse doğacak kiracı başı ek gider aralığı.
- Barındırma maliyeti profili: docker-compose.yml tek MySQL 8.4 + API; bulut (bizde) ve on-prem (müşteride) iki senaryonun maliyet ayrımı; deploy/nginx-kuyumcu.conf ve ecosystem.config.js işletim varsayımları.
- Lisans maliyeti kanıtı: Kuyumcu.Api.csproj:10-17 sekiz paketin tamamı izinli açık kaynak → yazılım lisans gideri 0 TL (rakiplerin kullanıcı/modül başı ek ücretine karşı yapısal üstünlük).
- Üç kademeli fiyat ÖNERİSİ (Tek Şube / Çok Şube / Uyum+) ve her kademenin brüt marj hesabı — yalnız ÖNERİ, ilan değil.
- Kurucunun tasarruf hedefinin dürüst okuması: M&L peşin bedeline karşı ~%85 (36 ay), Dezy 500 TL/ay tabanına karşı ~%20 — tek bir 'silme oranı' iddiası yerine rakip bazlı tablo.
- Fiyat ilanı kilidinin yazılı kuralı: tablo dolmadan hiçbir kanalda rakamlı fiyat açıklanmaz (MÜHÜR 2).
- Belirsizlik bandı: her satır için {alt, olası, üst} tahmin + güven derecesi + kaynak; TAHMİN satırları toplamdan ayrı gösterilir.
- Kurucu'ya tek sayfalık tedarik talebi: hangi kalemde kimden ne isteneceği, imza/para gerektiren kalemlerin listesi.

**Dışında:**
- Abonelik/faturalama tabloları, deneme süresi, plan kapısı, kota uygulaması — F-1/F-2/F-3 işidir; F-8 bunlara yalnız fiyat girdisi verir.
- Kiracı başı telemetri toplayan kod (OpenTelemetry/Prometheus sayaçları) — F-4. F-8 bu ölçüm gelmeden altyapı satırını TAHMİN olarak işaretler.
- Entegratör/mühür/yaptırım listesi sözleşmelerinin İMZASI ve ödemesi — Kurucu'ya aittir (MIMARI-KALAN-PAKETLER-2026-09-15.md:458-467); ajan sözleşme imzalamaz, teklif talebi taslağı hazırlar.
- Fiyatın kamuya İLAN EDİLMESİ, web sayfası, teklif şablonu, sözleşme metni — S-1/SATIS-1 (dalga 6) işidir ve F-8 kapanmadan yayımlanamaz.
- Kur veri sağlayıcısıyla ticari sözleşme ve teknik yedeğe düşme — F-9.
- Üretim profili, TLS, yedek otomasyonu maliyetlerinin UYGULANMASI — T-1/F-6, F-7, T-3/F-5; F-8 yalnız bunların bedelini satır olarak alır.
- Rakip ürünlerin içinin görülmesi / demo alınması — bu paket rakip fiyatını yalnız yayımlı birincil kaynaktan alıntılar.
- Vergi/muhasebe danışmanlığı, şirketin genel gider ve personel maliyeti dağıtımı — FinOps'un şirket düzeyi işidir, ürün birim ekonomisine girmez.

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| (yok) | YOK — F-8 belge paketidir: yeni tablo, yeni kolon, yeni indeks ve migration ÜRETMEZ. Abonelik/plan veri modeli F-1 (abonelik + hareket tablosu) ve F-2 (Tenant.DenemeBitis + Durum) paketlerinin işidir; F-8 yalnız onların fiyat girdisini hazırlar. | (yok) | (yok) | (yok) |
| tenants (mevcut, salt okunur) | OKUMA amaçlı kullanılır (yazma yok): kiracı sayısı, kiracı başı satır/disk hacmi ölçümü için canlı SQL sorguları koşulur. Tenant.Plan bugün yalnız etiket olduğu için maliyet dağıtımında ayırt edici değildir (Entities.cs:52-56). | id, ad, plan, aktif (mevcut) | mevcut kiracı indeksleri (KuyumcuDbContext) — değişmez | (yok) |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| (yok) | `(yok — belge paketi)` | (yok) | F-8 hiçbir HTTP ucu açmaz, değiştirmez veya kapatmaz. Kod üretmeyen bir belge paketidir; ModulKayit.cs'e kayıt yapılmaz, yeni [RolGerekli]/[ModulYetkisi] eklenmez. | (yok) |
| GET | `/saglik (mevcut, değişmez)` | yok | Yalnız OKUMA amaçlı kullanılır: mevcut /saglik ucu (Program.cs:313) barındırma profili doğrulamasında 200 beklenir. Yeni uç değildir. | yok (anonim sağlık ucu) |

## Servis ve dosyalar
- **YENI** `dokumanlar/09-uygulama-kayitlari/BIRIM-EKONOMI-F8-2026-09-16.md` — Ana belge. Bölümler: §1 Kapsam ve kilit kuralı · §2 Kiracı başı maliyet tablosu (altyapı/dış kalem/destek, her satırda tutar+kaynak+tarih+güven) · §3 Dış tedarikçi kalemleri ve teklif durumu · §4 Kademe önerisi ve brüt marj · §5 Tasarruf hedefinin dürüst okuması · §6 Bilinmeyenler listesi.
- **YENI** `dokumanlar/09-uygulama-kayitlari/ham/f8-birim-ekonomi.json` — Makine okunur ham veri (mevcut ham/ klasörü deseniyle, hazirlik-kurulu-1.json ile aynı yerleşim): kalemler[] = {ad, sinif: SABIT|KIRACI_BASI|DEGISKEN, tutar, para_birimi, donem, kaynak, tarih, guven: YUKSEK|ORTA|DUSUK|OLCULMEDI}. Tabloyu bu dosya besler; iki yerde elle rakam tutulmaz.
- **YENI** `dokumanlar/09-uygulama-kayitlari/KURUCUYA-F8-TEDARIK-TALEBI.md` — Tek sayfa: Kurucu'nun toplayacağı 5 kalem (üç entegratör kontör teklifi, mali mühür bedeli, yaptırım listesi aboneliği, kur verisi ticari teklifi, alan adı+TLS), her biri için sorulacak somut sorular (kontör birim fiyatı, taban ücret, iptal şartı, kontör devri) ve dönüş formatı.
- **DEGISTIR (yalnız ek satır)** `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-1.md` — Engel 14 satırına (dosya satır 36) ve Mustafa merceğine (satır 242) F-8 belgesinin yolu referans olarak eklenir; engel kapanış durumu 'Faz A yazıldı / Faz B teklif bekliyor' diye işaretlenir. Mevcut metin SİLİNMEZ, ek satır yazılır.
- **DEGISTIR (yalnız ek işaret)** `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md` — § 'Kurucuya kalanlar' (satır 457-467) kalemlerinin yanına 'bedel: ÖLÇÜLMEDİ → F-8' işareti; karar tablosunda F-8 satırının dalga 3 paralel hattı olarak damgalanması.
- **DEGISTIR (yalnız ek satır)** `SONRAKI-OTURUM-PROMPT.md` — A18 bölümüne F-8 durumu: 'fiyat ilanı KİLİTLİ; kilit F-8 Faz B + Tamer Bey mührü ile açılır' satırı — sonraki oturumun yanlışlıkla fiyat yazmasını engeller.
- **DEGISMEZ** `dotnet/ altındaki tüm kaynak dosyalar` — DOKUNULMAZ. F-8 kod üretmez; mevcut mekanizmalar (KiraciYazmaDenetcisi, HareketFabrikasi, kapılar) değişmez, ikinci yol açılmaz.

## Ekranlar
- `(yok — belge paketi)` · `(yok)` — F-8 ekran üretmez. Fiyat/plan yüzeyi zaten var ve YANILTICIDIR: Kuyumcu.Web/Pages/Isletme.razor:47-51 BASLANGIC/PRO/KURUMSAL seçtiriyor ama plan hiçbir yerde karar vermiyor. Kademeleri ekrana bağlamak F-3 (PlanKapisi) işidir, F-8 yalnız kademelerin fiyatını ve maliyetini yazar. (menü/yetki: (yok))
- `/isletme (mevcut)` · `(gelecek paket girdisi) Kuyumcu.Web/Pages/Isletme.razor` — F-8'in çıktısı S-1 ve F-3'e GİRDİ olarak devredilir: hangi kademe hangi kotayı alır, hangi fiyattan. UX sözleşmesi bu pakette tetiklenmez (yeni düğme yok → 'her düğme istek atar ya da disabled' kuralı ihlal edilmez). (menü/yetki: F-3/F-1 uygulanınca: [RolGerekli("YONETICI")] tam eşleşme)

## İş kuralları (fail-closed)
- FAIL-CLOSED 1 — Fiyat kilidi: tablodaki 'dış kalem' satırlarından herhangi biri ÖLÇÜLMEDİ/TAHMİN iken hiçbir kanalda (site, teklif, demo, sözleşme) rakamlı fiyat ilan edilmez. Kilit yalnız Faz B kapanışı + Tamer Bey mührü ile açılır (MÜHÜR 2).
- FAIL-CLOSED 2 — Kaynak zorunluluğu: her maliyet satırı {tutar, para birimi, dönem (aylık/yıllık/kontör başı), kaynak URL veya teklif dosyası, tarih, güven} altı alanını taşımak zorundadır; eksik alan varsa satır TAHMİN sayılır ve toplamda ayrı gösterilir.
- FAIL-CLOSED 3 — Rakip fiyatı: birincil kaynaktan (firmanın kendi sayfası, erişim tarihiyle) doğrulanmamış hiçbir rakip fiyatı karşılaştırmada kullanılmaz; DOĞRULANMALI etiketiyle ayrı listede kalır (MÜHÜR 2).
- FAIL-CLOSED 4 — Negatif marj: bir kademede (fiyat − kiracı başı toplam maliyet) ≤ 0 çıkıyorsa o kademe önerilmez; 'zararına satış' uyarısıyla Kurucu'ya çıkar (HAZIRLIK-KURULU-1.md:249).
- FAIL-CLOSED 5 — Ölçüm yoksa iddia yok: 'tek VPS'te onlarca dükkân' gibi kapasite cümleleri F-4 ölçümü ve dotnet/yuk-testi çıktısı olmadan tabloya yazılmaz; bugün mimari çıkarımdır (HAZIRLIK-KURULU-1.md:250).
- Tasarruf yüzdesi dürüstlüğü: %85 yalnız M&L'in 94.686,48 TL peşin bedeline karşı ve yalnız 36 ay üzerinden geçerlidir; Dezy'nin 500 TL/ay tabanına karşı fark ~%20'dir. Tek bir 'tasarruf oranı' rakamı ilan edilmez; karşılaştırma her rakip için ayrı yazılır.
- Değişken/sabit ayrımı: kontör, mühür ve yaptırım listesi KİRACI BAŞINA tekrarlayan gider sınıfına yazılır (HAZIRLIK-KURULU-1.md:242); VPS ve alan adı sabit gidere, kiracı sayısına bölünerek gösterilir ve bölen sayı tarihiyle yazılır.
- Para birimi kuralı: USD/EUR fiyatlı kalemler (ör. sertifika, yurt dışı sağlayıcı) tabloya hem döviz hem TL karşılığıyla, kur kaynağı ve tarihiyle yazılır; kur riski ayrı satırdır.
- Belge kapsamı kilidi: F-8 hiçbir .cs/.razor dosyasını değiştirmez, migration üretmez, yeni uç açmaz. Abonelik/kota mekanizması F-1/F-2/F-3'ün, ölçüm F-4'ün işidir; ikinci yol açılmaz.
- KDV ve stopaj: tüm kalemler KDV hariç yazılır, KDV ayrı satırdır; ilan fiyatının KDV dâhil/hariç olduğu açıkça belirtilir (rakiplerin çoğunun belirtmediği nokta — farklılaşma).
- Sır yasağı: teklif belgelerindeki sözleşme numarası, müşteri temsilcisi iletişimi gibi veriler depoya yazılmaz; ham/f8-birim-ekonomi.json yalnız tutar + tarih + firma adı taşır.
- Sürüm damgası: tablo her güncellemede tarih + hangi ölçümle güncellendiği satırıyla damgalanır; eski satır silinmez, üstü çizilir (denetim izi mantığının belge karşılığı).

## Testler
- `dokumanlar/09-uygulama-kayitlari/BIRIM-EKONOMI-F8-2026-09-16.md (belge kapısı — kod testi YOK)` [belge kapısı (kontrol listesi)]
  - Tablodaki her satır altı zorunlu alanı taşıyor mu (tutar, para birimi, dönem, kaynak, tarih, güven)?
  - Kaynağı olmayan tek bir rakam var mı? Varsa satır TAHMİN işaretli mi ve toplamın dışında mı?
  - Rakip fiyat satırlarının hepsi birincil kaynak URL + erişim tarihi taşıyor mu (MÜHÜR 2)?
  - Belgede rakamlı bir 'ilan fiyatı' cümlesi geçiyor mu? Geçiyorsa kapı KAPALI (öneri ile ilan ayrımı).
  - Üç kademe için brüt marj hesabı gösterilmiş ve negatif marj kontrolü yapılmış mı?
  - Tasarruf yüzdeleri rakip bazlı mı verilmiş, tek bir genel oran iddiası var mı?
- `dokumanlar/09-uygulama-kayitlari/ham/f8-birim-ekonomi.json` [saf (veri tutarlılığı, elle veya küçük betikle)]
  - JSON geçerli mi ve her kalem {ad, sinif, tutar, para_birimi, donem, kaynak, tarih, guven} şemasına uyuyor mu?
  - guven='OLCULMEDI' olan kalem sayısı belgedeki TAHMİN satır sayısıyla birebir eşleşiyor mu (iki yerde farklı gerçek olmasın)?
  - sinif='KIRACI_BASI' kalemlerin toplamı belgedeki kiracı başı toplamla aritmetik olarak tutuyor mu?
- `dotnet/ (DB/HTTP/bUnit)` [uygulanamaz (kod yok)]
  - YOK — bu paket kod üretmediği için yeni xUnit/bUnit/HTTP testi yazılmaz; mevcut 971 testin sayısı ve denetim.sh sayaçları DEĞİŞMEMELİDİR. Test sayısının değişmesi kapsam kaymasının kanıtıdır.

## Mutasyon kanıtları
- YOK — mutasyon kanıtı kod testi kavramıdır; F-8 kod üretmez, dolayısıyla mut.sh koşulmaz ve mutasyon kanıtı ÜRETİLEMEZ (Madde 1: uydurulmaz).
- Belge karşılığı 'kanıt mutasyonu' şudur: tablodaki herhangi bir kaynaklı satırın tutarı elle değiştirilirse, o satırın kaynak URL/teklif dosyasıyla çeliştiği tek bakışta görülmelidir — yani her satır kendi kanıtını taşır.
- Fiyat kilidi kuralının canlı sınaması: S-1 taslağına rakamlı fiyat konulmaya çalışıldığında F-8 tablosundaki TAHMİN satırları nedeniyle mühür verilmemelidir; bu kapı dalga 6'da Tamer Bey tarafından fiilen sınanır.
- F-8'in ölçüm satırlarının mutasyon kanıtı F-4 paketine devredilir (kiracı etiketli sayaçlar yazıldığında sayaç mutasyonu testle yakalanmalıdır).

## Canlı doğrulama
- Kiracı sayısı paydası: `SELECT COUNT(*) FROM tenants;` — canlı `kuyumcu_net` şemasında beklenen 6 (HAZIRLIK-KURULU-1.md:230 iddiası; F-8 yazımında bu sayım yeniden koşulup tabloya tarihli yazılır).
- Kiracı başı disk: `SELECT table_schema, ROUND(SUM(data_length+index_length)/1048576,1) AS mb FROM information_schema.tables WHERE table_schema='kuyumcu_net' GROUP BY table_schema;` → tablodaki 'depolama' satırının kanıtı.
- Kiracı başı satır hacmi: `SELECT tenant_id, COUNT(*) FROM hareketler GROUP BY tenant_id;` — en büyük/en küçük kiracı oranı, kademe eşiklerinin gerekçesi.
- Yedek boyutu ve süresi: `dotnet/yedek-al-ve-dogrula.sh` bir kez koşturulup çıktı boyutu (MB) ve süre (sn) tabloya yazılır; betik fail-closed olduğu için çıkış kodu 0 değilse satır 'ÖLÇÜLEMEDİ' kalır.
- Servis ayakta kanıtı: `curl -s -o /dev/null -w '%{http_code}' http://localhost:5198/saglik` → 200 (Program.cs:313). Bu paket kod eklemediği için tek beklenen HTTP kodu budur; yeni uç yoktur.
- Dış kalem ölçümü YAPILAMAZ: e-Fatura kontörü, mali mühür ve yaptırım listesi için kodda tek satır yok (grep 'Entegrator|Kontor|MaliMuhur|Yaptirim' → 0 eşleşme), dolayısıyla bu satırlar yalnız yazılı teklifle dolar.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/BIRIM-EKONOMI-F8-2026-09-16.md (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/ham/f8-birim-ekonomi.json (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/KURUCUYA-F8-TEDARIK-TALEBI.md (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-1.md (DEGISTIR — engel 14 satırına F-8 bağlantısı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md (DEGISTIR — 'Kurucuya kalanlar' §457-467 kalemlerine bedel sütunu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/SONRAKI-OTURUM-PROMPT.md (DEGISTIR — A18 altına F-8 durumu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/PAZAR-ARASTIRMASI-2026-09-16.md (SALT OKUNUR kaynak — rakip fiyatları buradan alıntılanır, değiştirilmez)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docker-compose.yml (SALT OKUNUR — barındırma profili girdisi)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/nginx-kuyumcu.conf, deploy/ecosystem.config.js (SALT OKUNUR — işletim profili girdisi)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj (SALT OKUNUR — lisans bedeli kanıtı, satır 10-17)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/yedek-al-ve-dogrula.sh (SALT OKUNUR — yedek süresi/boyutu ölçümü)`
- `KOD DOSYASI DEĞİŞTİRİLMEZ: dotnet/ altında hiçbir .cs/.razor/.csproj dosyasına yazılmaz; migration üretilmez.`

## Fazlar
- **Faz A — Sevk edilebilir asgari: maliyet iskeleti + fiyat kilidi** (Dalga 3'e PARALEL, dalga 3 başlar başlamaz (kod hattını bloklamaz)): BIRIM-EKONOMI-F8 belgesi yazılır: (1) bugün KANITLI sıfır/düşük kalemler (lisans 0 — csproj:10-17; kur verisi 0 TL ama sözleşmesiz — KurModulu.cs:292/362/452; on-prem barındırma 0 — docker-compose.yml); (2) canlı SQL ölçümleriyle depolama/kiracı; (3) dış kalemler 'ÖLÇÜLMEDİ' satırı olarak açık bırakılır; (4) fiyat ilanı kilidi resmî karar olarak yazılır ve SONRAKI-OTURUM-PROMPT'a işlenir; (5) Kurucu'ya tek sayfalık tedarik talebi çıkar.
- **Faz B — Teklifler döndükten sonra: tablo kapanışı ve kademe önerisi** (Dalga 4 öncesi; teklif dönüşüne bağlı (Kurucu bağımlılığı — takvim garanti edilemez)): Kurucu'nun getirdiği üç yazılı kontör teklifi + mühür + yaptırım listesi bedelleri tabloya işlenir; F-4 ölçümü ve dotnet/yuk-testi sonuçları geldiyse altyapı satırındaki TAHMİN etiketleri kanıta çevrilir; kiracı başı toplam maliyet ve brüt marj üç kademe için hesaplanır; S-1'e imzalı girdi verilir. Ancak bu fazdan sonra fiyat İLAN EDİLEBİLİR.
## Mevzuat dayanağı
- e-Fatura / e-Arşiv zorunluluğu (VUK 509 sıra no.lu Genel Tebliğ ve sonraki değişiklikler) — **ORTA — genel çerçeve bilinir, ancak 2026 ciro hadleri ve kuyumculuğa özel zorunluluk eşiği birincil kaynaktan (GİB tebliğ metni) DOĞRULANMALI** — Kontör maliyetinin kiracı başına mı yoksa bize mi yazılacağını bu belirler. Tabloya yazılmadan önce tebliğin güncel hâli okunmalı.
- Mali mühür sertifikası (tüzel kişi) / e-imza — e-Arşiv ve e-İrsaliye belgelerinin yasal geçerliliği — **DÜŞÜK — bedel ve tedarik yolu bu turda hiç doğrulanmadı** — MIMARI-KALAN-PAKETLER-2026-09-15.md:459 'Kurucuya kalanlar' listesinde imzasız duruyor. Yıllık bedel ve yenileme periyodu DOĞRULANMALI; tahmini rakam yazılmayacak.
- 6415 sayılı Kanun kapsamı ve MASAK yükümlülükleri — yaptırım/taraması ve kimlik tespiti — **ORTA — yükümlülüğün varlığı kurul belgelerinde sabit; veri sağlayıcı bedeli DOĞRULANMALI** — Resmî listeler ücretsiz yayımlanıyor olabilir; ticari OFAC/BM/AB sağlayıcısı ayrı bir abonelik gideridir. İkisi arasındaki maliyet farkı F-8'in en büyük tek belirsizidir (MIMARI:461).
- MASAK 185.000 TL kimlik tespiti eşiği ve 8 yıl saklama yükümlülüğü — **ORTA — kapsamı MASAK-ENTEGRASYON-ARASTIRMASI-2026-09-11.md'de işlenmiş, güncel hâli DOĞRULANMALI** — Saklama süresi doğrudan depolama maliyeti demektir: 8 yıllık veri + yedek, kiracı başı disk satırının çarpanıdır.
- KVKK yurt dışına aktarım rejimi — **ORTA — Kurul 'hiçbir ülke için yeterlilik kararı verilmemiştir' ifadesi PAZAR-ARASTIRMASI-2026-09-16.md öneri 21'de kayıtlı** — AI veya yurt dışı sağlayıcı kullanımı ek uyum maliyeti (standart sözleşme, bildirim) doğurur; F-8 tablosunda 'uyum yükü' satırı olarak yer alır, bugün 0 TL değil 'ÖLÇÜLMEDİ'.
- Fiyat Etiketi Yönetmeliği md.6/3 (vitrin birim fiyat listesi, 'işçilik dâhildir/hariçtir') — **YÜKSEK — konsolide metin PAZAR-ARASTIRMASI-2026-09-16.md öneri 18'de birebir alıntılanmış** — Doğrudan maliyet kalemi değil; Uyum+ kademesinin değer gerekçesidir (rakiplerde görülmeyen çıktı).
- KMTS (Kıymetli Madenler Takip Sistemi) beyan yükümlülüğü — **DÜŞÜK — kmts.gov.tr kapsamı iki ayrı turda rafineri odaklı bulundu, perakende kuyumcuya uygulanabilirliği DOĞRULANMALI** — Rakiplerin 'tam uyumluluk' iddiası çürük çıktı. Uyum+ kademesinin fiyatını KMTS'ye dayandırmak bugün Madde 1'e aykırı olur.

## Riskler
- Takvim riski (en büyük): Faz B tamamen Kurucu'nun teklif toplamasına bağlıdır. Teklifler gecikirse F-8 açık kalır ve S-1 (dalga 6) fiyat ilanı bloke olur — yani ticari hattın tamamı dış bağımlılıkla duruyor.
- Marj riski: kontör + mühür + yaptırım listesi kiracı başına tekrarlayan giderdir. Üçünün toplamı 399 TL/ay önerisinin anlamlı bir kısmını yiyebilir; bugün bunu bilmediğimiz için önerilen kademe rakamları YALNIZ ÖN ÖNERİDİR.
- Ölçüm riski: F-4 gelmeden altyapı satırı tahmindir; gerçek kiracı başı disk/CPU beklenenden yüksek çıkarsa en ucuz kademe zararına satışa dönüşebilir (HAZIRLIK-KURULU-1.md:250).
- Rakip fiyat riski: Dezy tabanı 500 TL/ay ve 3 ay ücretsiz. Altına girip 3 ay bedava vermek, tahsilat mekanizması olmadığı için (AuthModulu.cs:87 kayıt ucu açık) kontrolsüz maliyet demektir; F-1/F-2 gelmeden agresif fiyat ilan etmek çifte risk.
- İtibar riski: erken ilan edilip sonra yukarı revize edilen fiyat, pazar araştırmasının 'fiyat şeffaflığı tek başına farklılaşmadır' avantajını tersine çevirir.
- Tasarruf hedefi riski: Kurucu'nun 'rakipleri memleketten silecek' hedefi salt fiyat kırmayla karşılanamaz (Dezy'ye karşı fark ~%20). Fiyatı zorlamak yerine uyum kalemlerine dayanmak gerektiği yazılı söylenmeli — bu kötü haberi yumuşatmak Madde 1 ihlalidir.
- Kur riski: dış kalemlerin bir kısmı dövizli olabilir; TL fiyat sabitken maliyetin dövize bağlı olması marjı erozyona uğratır. Tabloya kur duyarlılığı satırı konmazsa bu risk görünmez kalır.
- Kapsam kayması riski: F-8 yazarken 'madem buradayız' diyip abonelik tablosu veya plan kapısı kodlanırsa F-1/F-3 ile ikiz mekanizma doğar; bu paket bilinçli olarak kod üretmez.
- On-prem/bulut ikiliği: iki modelin maliyet yapısı taban tabana zıttır (on-prem barındırma 0 ama destek yüksek). Model kararı verilmeden tek bir birim maliyet rakamı yazmak yanıltıcı olur — tablo iki sütunlu tutulmalı.
- Destek maliyeti körlüğü: rakip çıtası 7/24 WhatsApp destek. İnsan destek saati hiç ölçülmedi ve kiracı başı maliyetin en oynak kalemidir; ilk üç pilotta saat kaydı tutulmazsa tablo kalıcı olarak eksik kalır.

## Açık sorular
- Kurucu e-Fatura entegratörü için hangi üç firmadan teklif isteyecek (ör. üç farklı GİB özel entegratörü)? İsim listesi Kurucu'dan gelmeli; ajan firma seçemez (dış taahhüt).
- Mali mühür kime ait olacak: her kiracı kendi mührünü mü alacak (maliyet müşteride), yoksa ByGatech mi tedarik edecek (maliyet bizde)? Fiyat kademesinin en büyük değişkeni budur.
- Yaptırım taraması için resmî ücretsiz MASAK/BKDK listeleri yeterli sayılacak mı, yoksa ticari OFAC/BM/AB sağlayıcısı mı alınacak? İkisinin kiracı başı maliyeti mertebe farkındadır.
- Barındırma modeli hangisi ana hat: bizim VPS'imizde çok kiracılı bulut mu, yoksa docker-compose ile müşteri sunucusunda on-prem mi (barındırma maliyeti 0, destek maliyeti yüksek)?
- Fiyatlar KDV dâhil mi ilan edilecek? Rakiplerin çoğu belirtmiyor (PAZAR-ARASTIRMASI-2026-09-16.md rakip matrisi, Dezy satırı: 'KDV durumu belirtilmemiş').
- Destek kanalı (WhatsApp 7/24 rakip çıtası) kim tarafından karşılanacak? İnsan destek saati kiracı başı maliyetin en büyük belirsiz kalemidir ve bugün ölçülmemiştir.
- Ücretsiz pilot kiracıları maliyet tablosunda hangi başlığa yazılacak (pazarlama gideri mi, negatif marj mı)?

## Oylama
- **YENİ: BAŞ MİMAR (Bertan) merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilirlik), dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı bizzat doğrulandı (Entities.cs Tenant bloğu, Program.cs CSP + statik boru hattı, denetim.sh yüzeyi, wwwroot/index.html, AdminModulu plan yazma yolu, AuthServisi.Kayit). NOT (Madde 1): görevde "4 paket" denmesine rağmen bana ulaşan tasarım metni 3 pakettir (F-8, S-1, PWA-1); PWA-1 'mevzuat' bölümünün ortasında kesilmiş, 4. paket nesnesi hiç başlamamıştır. Olmayan tasarıma oy uydurulmadı.: KOSULLU** — Mimari açıdan en temiz paket: kod üretmiyor, veri modeline dokunmuyor, migration yok, ikinci mekanizma doğurmuyor, kapsam kilidi (abonelik=F-1/F-2/F-3, ölçüm=F-4) açıkça yazılmış; dalga 3'e paralel yürümesi kod hattını bloklamıyor. İtirazım kapsamda değil, kanıt metninin doğruluğunda: Plan alanının yazma yolu yanlış anlatılmış. Fiyat kilidinin bugün kod karşılığı olmaması kabul edilebilir, çünkü kilidi taşıyacak test S-1'de tanımlı — ama iki paket bu kapıyı birbirine atmamalı.
  - Koşullar: Düzelt: 'planı okuyan/yazan tek yer ekran' yanlış. Gerçek yazma yolu AdminModulu.cs:63-68 (tenant.Plan = plan) + :122-125 PATCH /api/v1/admin/plan; Isletme.razor:258 yalnız bu ucu çağırır. · Düzelt: 'Entities.cs:52-56' Tenant'ın tamamı değil. Doğrulanan aralık Entities.cs:52-74 (Plan :56, VarsayilanArayuzModu :69-70, Aktif :72). · Tek gerçek kaynağı: MD tablosundaki her rakam ham/f8-birim-ekonomi.json'dan türemeli; elle yazılan ikinci tablo yasak. 'guven=OLCULMEDI sayısı = TAHMİN satır sayısı' kapısı zorunlu olsun. · Fiyat kilidinin kod karşılığı F-8'de değil S-1'dedir (TicariBelgeSozlesmesiTestleri.FiyatBelgesi_F8Referanssiz_Rakam_Icermez). F-8 bu testin adını referans versin; yoksa kilit sahipsiz kalır. · Kod dokunulmazlığı ölçülsün: rapora test sayısının 971'de kaldığı ve denetim.sh sayaçlarının (IgnoreQueryFilters=0, ExecuteUpdate=4, Hareketler.Add=1) değişmediği çıktı olarak eklensin. · Faz B'nin Kurucu teklifine bağlılığı S-1 Faz A'yı bloklamasın: 'F-8 Faz A yeterlidir, ücretsiz pilot fiyatsız sevk edilir' cümlesi belgeye yazılsın.
- **YENİ: Güvenlik merceğinden (Mehmet) üç tasarımı inceledim: kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek/TLS, içe aktarım riski ve MFA koşulu açısından. Program.cs:283 canlı kodda script-src 'self' 'wasm-unsafe-eval' olarak doğrulandı (unsafe-inline yok) — PWA-1'in CSP taahhüdünün mevcut temeli sağlam. docker-compose.yml:7'de MySQL root parolası düz metin olarak canlıda doğrulandı; hiçbir pakette bu ele alınmıyor (T-1/F-6/F-7'ye bırakılmış), ayrı takip gerekir.: EVET** — F-8 kod üretmiyor, yalnız belge/JSON yazıyor; kiracı izolasyonu, rol/yetki veya fail-closed kapı yüzeyine dokunmuyor. Tek güvenlik riski dış tedarikçi teklif belgelerinin (sözleşme no, temsilci bilgisi gibi) yanlışlıkla depoya sızması — tasarım bunu zaten yasaklamış (Sır yasağı maddesi). Veto gerektiren bir açık yok.
  - Koşullar: ham/f8-birim-ekonomi.json şeması yalnız {ad,sinif,tutar,para_birimi,donem,kaynak,tarih,guven} taşımalı; sözleşme numarası/temsilci iletişim bilgisi gibi sır ASLA bu dosyaya yazılmamalı (tasarımda zaten belirtilmiş, bağlayıcı koşul olarak tekrar mühürleniyor — servis_ve_dosyalar: ham/f8-birim-ekonomi.json). · Belgeye eklenecek üç entegratör/mühür/yaptırım teklif dosyaları (Faz B) depo içine PDF/e-posta olarak COMMIT EDİLMEMELİ; yalnız özet rakam+tarih+kaynak metne girer (is_kurallari: Sır yasağı satırıyla uyumlu, ihlal riskine karşı ek uyarı). · Kod dosyası değiştirilmediği teyit edilmeli: publish öncesi `grep` ile dotnet/ altında .cs/.razor/.csproj/migration diff'i sıfır olmalı (servis_ve_dosyalar: 'KOD DOSYASI DEĞİŞTİRİLMEZ' maddesi — denetim adımına bağlanmalı).
- **YENİ: Aşkın — QA/fail-closed kalite kapısı. Mercek: (a) her iş kuralının mutasyon kanıtlı bir testi var mı, (b) DB/HTTP/bUnit dengesi kurulu mu, (c) kararsızlık (flaky) riski, (d) canlı doğrulama ölçülebilir/somut mu, (e) kurulum/yedek paketlerinde "kanıt" gerçek publish+geri yükleme mi, (f) test kiracısı temizliği taahhüt edilmiş mi. Doğrulama: Program.cs:271-330 (CSP script-src 'self' 'wasm-unsafe-eval', unsafe-inline YOK — PWA-1'in iddiasıyla örtüşüyor), Kuyumcu.Api.csproj:9-17 (8 paket, F-8'in lisans-0 iddiasıyla örtüşüyor), Entities.cs:16,56 (Planlar dizisi ve Plan varsayılanı doğrulandı), AuthModulu.cs:87,323-326 (Kayit yolu doğrulandı, kota/deneme bitişi yok iddiası tutarlı), denetim.sh:1-60 (kalıp denetimi mekanizması gerçek ve betikte "ExecuteUpdate|ExecuteDelete beyaz liste 4" gibi sayaçlar var — S-1/PWA-1'in "denetim.sh'a blok eklenir" planı bu mekanizmaya uygun).: KOSULLU** — Kod üretmeyen bir belge paketi olduğu için xUnit/DB/HTTP/bUnit dengesi kavramı uygulanamaz — bu dürüstçe 'uygulanamaz' diye işaretlenmiş, iyi. Fail-closed kilit kuralları (5 madde) somut ve ölçülebilir yazılmış, TAHMİN/ÖLÇÜLMEDİ ayrımı QA açısından doğru bir tasarım. Ancak 'belge testi'nin CI'da fiilen koşan bir mekanizması yok; bu QA'nın 'her iddia doğrulanabilir olmalı' ilkesine kısmen aykırı.
  - Koşullar: Belge kapısı için otomatik doğrulama yok: 'testler' bölümü yalnız kontrol listesi/elle bakım; en azından f8-birim-ekonomi.json şema+aritmetik kontrolü (guven=OLCULMEDI sayısı = TAHMİN satır sayısı, KIRACI_BASI toplamı) küçük bir betikle (perl/python3 uygun) CI'a bağlanmalı, yoksa 'fail-closed fiyat kilidi' iddiası denetlenemez kalır. · testler bölümünde '971 testin sayısı DEĞİŞMEMELİDİR' deniyor; ama S-1/PWA-1 paketleri sırayla uygulanınca taban sayı kayacak — bu satır 'F-8 kendi turunda mevcut test sayısını artırmaz' diye netleştirilmeli, yoksa sonraki oturumda yanlış-kırmızı okunur. · canli_dogrulama'daki 6 SQL/curl adımının çıktısı f8-birim-ekonomi.json'a otomatik yazılmıyor, elle taşınacak → transkripsiyon hatası riski; en azından bir betik çıktıyı JSON'a dökmeli.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Kanıt düzeltmesi: Plan'ı yazan tek yer ekran DEĞİL, AdminModulu.cs:63-68 (tenant.Plan = plan) + :122-125 PATCH /api/v1/admin/plan; Isletme.razor:258 yalnız bu ucu çağırır. Tenant aralığı Entities.cs:52-74.
- Tek gerçek kaynak: MD tablosundaki her rakam ham/f8-birim-ekonomi.json'dan türer; elle yazılan ikinci tablo yasak. 'guven=OLCULMEDI sayısı = TAHMİN satır sayısı' kapısı zorunlu.
- JSON şeması yalnız {ad,sinif,tutar,para_birimi,donem,kaynak,tarih,guven} taşır; sözleşme numarası/temsilci iletişimi asla yazılmaz. Teklif PDF/e-postaları depoya commit edilmez, yalnız özet rakam+tarih+kaynak.
- Belge kapısı otomatik doğrulanır: JSON şema + aritmetik kontrolü (perl/python3) küçük bir betikle CI'a bağlanır; canlı SQL/curl çıktıları JSON'a betikle dökülür, elle taşınmaz.
- Kod dokunulmazlığı ölçülür: dotnet/ altında .cs/.razor/.csproj/migration diff'i sıfır; denetim.sh sayaçlarının (IgnoreQueryFilters=0, ExecuteUpdate=4, Hareketler.Add=1) değişmediği rapora çıktı olarak eklenir.
- 'Test sayısı değişmez' kuralı 'F-8 KENDİ turunda test sayısını artırmaz' diye netleştirilir; taban sayı her paket sonunda SONRAKI-OTURUM-PROMPT'ta güncellenir.
- Fiyat kilidinin kod karşılığı S-1'dedir (TicariBelgeSozlesmesiTestleri); F-8 bu testin adını referans verir, test sahibi S-1-BELGE'ye bağlanır.
- F-8 Faz A ölçümleri S-1 tanıtım tohumlamasından ÖNCE alınır ya da tanıtım ayrı DB'de yaşar (ölçüm kirlenmesi).
- 'F-8 Faz A yeterlidir, ücretsiz pilot fiyatsız sevk edilir' cümlesi belgeye yazılır; Faz B'nin Kurucu teklifine bağlılığı S-1 Faz A'yı bloklamaz.
