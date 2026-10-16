# Paket E-BELGE — e-Fatura / e-Arşiv entegrasyonu (paket 38 Faz B)

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: 9-13 gün (Faz A 5-7, Faz B 4-6). Dış takvim: entegratör sözleşmesi + mali mühür 3-6 hafta, Faz A ile PARALEL yürür ve onu engellemez. gün · Önkoşullar: 38-A (OzelMatrahMotoru + KdvAyarlari + özel matrah kodu) — ZORUNLU; bugün Modules/Fatura altında yalnız OzelMatrahMotoru.cs var, KdvAyarlari.cs YOK (ölçüldü)., 1-A (fatura_kalemleri) — BİTTİ; UBL satır kalemleri bu tablodan gelir (FaturaKalemi.cs:25)., 4-A (belgeler tablosu + BelgeKapisi) — BİTTİ; UBL XML orada saklanır (UrunBelgesi.cs:27-52)., UX-3-A (basit/pro mod, ProAlan) — BİTTİ; ayar kartı ProAlan içine girer., UX-1-A (DuzeltmePolitikasi + GerekceAlani + DegismezIpucu) — BİTTİ; 3 yeni varlık haritaya eklenir., DIŞ ÖNKOŞUL (Kurucu imzası, teknik işi engellemez): entegratör sözleşmesi + mali mühür — 3-6 hafta dış takvim. Faz A bu imza olmadan sevk edilir., 42 (sevk irsaliyesi / ortak BelgeYerlesimi) — ÖNKOŞUL DEĞİL; yazdırma yerleşimi bu pakette kapsam dışı. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 5

## Hedef
Kesilen faturayı GİB'in kabul ettiği UBL-TR 2.1 belgesine çevirmek, e-Fatura/e-Arşiv ayrımını VKN'den seçmek, entegratöre göndermek ve GİB durumunu sorgulamak. Kuyum özel matrahı (805/808, istisna 230) belgeye doğru yansır. Sözleşme/mali mühür yoksa sistem fail-closed durur, sahte "gönderildi" yazmaz.

## Kodda bugünkü durum
Sıfır: kod tabanında efatura/earsiv/UBL/GİB 0 isabet (grep, --include=*.cs/*.razor). Fatura yalnız iç kayıt: Entities.cs:505-543 (faturalar; kdv_turu, has_deger, has_deger_kaynagi, kalem_kaynagi), FaturaKalemi.cs:25-80 (fatura_kalemleri, 1-A ile geldi), OzelMatrahMotoru.cs:45-115 (saf KDV motoru). tenants'ta VKN/unvan/vergi dairesi/adres YOK (Entities.cs:51-75) — UBL zorunlu alanları eksik.

## Kapsam
**İçinde:**
- UBL-TR 2.1 XML üretimi (Invoice) — saf UblUretici sınıfı, fatura + fatura_kalemleri'nden; ikinci KDV hesabı YOK, OzelMatrahMotoru sonucu yazılır
- OZELMATRAH senaryosu: özel matrah kodu (805 altın / 808 gümüş) ve KDV istisna kodu 230, 38-A'daki KdvAyarlari'ndan; kod boşsa belge üretilmez (fail-closed)
- XSD/şematron şema doğrulaması — geçmeyen XML kaydedilmez, 422 döner
- e-Fatura ↔ e-Arşiv ayrımı: cari VKN mükellef sorgusu; sonuç ve zamanı belgeye damgalanır
- Mükellef kimliği: tenants'a vkn/unvan/vergi_dairesi/adres/sube_kodu kolonları + Isletme ekranı
- e_belgeler + e_belge_olaylari tabloları (append-only durum izi), ETTN (UUID), SHA-256
- Üretilen XML mevcut belgeler tablosunda saklanır (4-A BelgeKapisi) — ikinci dosya deposu açılmaz
- IEFaturaSaglayici soyutlaması + KapaliSaglayici (sözleşmesiz varsayılan, 503) + tek gerçek entegratör adaptörü (IHttpClientFactory, KurAdaptoru deseni)
- Entegratör kimlik bilgileri tenant başına AES-GCM şifreli (Common/AlanSifreleme.cs), ekrana asla basılmaz (var/yok)
- Gönderim + GİB durum sorgu + iptal/itiraz talebi (Faz B)
- /e-belgeler ekranı + Faturalar.razor'da e-Belge durumu ve XML indirme
- Yükümlülük uyarısı: kâğıt fatura kesilirken e-Arşiv zorunluluğu hatırlatması (bilgi bandı, engel değil)

**Dışında:**
- e-İrsaliye, e-Müstahsil, e-SMM, e-Defter (ayrı paketler; e-Gider Pusulası 509 s.n. IV.6.4 uyarınca genel zorunluluk değil)
- KDV beyannamesi / BA-BS üretimi
- Gelen (alış) e-Faturaların entegratörden çekilmesi ve otomatik alış kaydı — Faz C, ayrı paket
- GİB portal/entegrasyon (doğrudan) yöntemi — yalnız özel entegratör modeli
- Mali mühür/e-imza sertifikasının kendisinde imza atma: imzayı entegratör atar; HSM/USB token yönetimi kapsam dışı
- Eski (geçmiş) faturaların toplu e-belgeleştirilmesi — dayanak veri yok
- Kâğıt fatura tasarımı/yazdırma yerleşimi (paket 42 BelgeYerlesimi)
- 805/808 dışındaki sektörel özel matrah kodları
- Entegratör sözleşmesi, kontör satın alma, iş ortaklığı ticari kurgusu (Kurucu kalemi)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| e_belgeler | YENİ tablo (IKiraciVarlik + HasQueryFilter) | id, tenant_id, fatura_id (tekil), belge_tipi EFATURA|EARSIV, senaryo TEMELFATURA|TICARIFATURA|EARSIVFATURA, ettn CHAR(36), belge_no VARCHAR(16), durum HAZIR|GONDERILDI|GIB_ONAY|GIB_RED|IPTAL, ubl_belge_id (belgeler FK), ubl_sha256, entegrator, gib_durum_kodu, gib_mesaj, son_sorgu, olusturuldu, guncellendi | UNIQUE (tenant_id, fatura_id) · UNIQUE (tenant_id, ettn) · (tenant_id, durum, olusturuldu) | YYYYMMDDHHMMSS_EBelge |
| e_belge_olaylari | YENİ tablo — append-only durum geçmişi (PATCH/DELETE ucu YOK, UX-S2) | id, tenant_id, e_belge_id, eski_durum, yeni_durum, kaynak ISTEK|SORGU|ENTEGRATOR, http_kod, gib_kod, mesaj (kişisel veri yok), kullanici_id, olusturuldu | (tenant_id, e_belge_id, olusturuldu) | YYYYMMDDHHMMSS_EBelge (aynı migration) |
| e_belge_ayarlari | YENİ tablo — kiracı başına entegratör kimliği (tek satır) | tenant_id (PK), saglayici, ortam TEST|CANLI, kullanici_adi_sifreli VARBINARY, parola_sifreli VARBINARY, sifre_surum TINYINT, sozlesme_var BOOL, mali_muhur_var BOOL, etkin BOOL, guncellendi | PK tenant_id | YYYYMMDDHHMMSS_EBelge (aynı migration) |
| tenants | 5 yeni NULL kolon — UBL zorunlu mükellef kimliği (bugün hiç yok, Entities.cs:51-75) | vkn VARCHAR(11), unvan VARCHAR(200), vergi_dairesi VARCHAR(100), adres VARCHAR(500), sube_kodu VARCHAR(10) | yok (tek satır okunur) | YYYYMMDDHHMMSS_EBelge (aynı migration) |
| faturalar | Kolon EKLENMEZ. e-belge durumu e_belgeler'den JOIN ile okunur; faturaya durum kolonu konsaydı iki gerçek doğardı (paket 38 'yeni tablo yok' gerekçesinin tersi: burada belge yaşam döngüsü ayrı varlıktır) | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/faturalar/{id}/e-belge/hazirla` | SAHIP, MUDUR, MUHASEBE | UBL-TR 2.1 üretir, şema doğrular, belgeler'e yazar, e_belgeler satırını HAZIR durumda açar. Tekrar çağrılırsa mevcut HAZIR satırı YENİLER; GONDERILDI/GIB_ONAY ise 409. | ModulYetkisi("fatura") · EBelgeKapisi (mükellef kimliği tam, fatura KESILDI, KUYUM'da özel matrah kodu dolu) · kiracı filtresi + elle TenantId |
| GET | `/api/v1/faturalar/{id}/e-belge` | SAHIP, MUDUR, MUHASEBE | Belge durumu, ETTN, tip/senaryo, GİB kodu, son sorgu zamanı ve olay geçmişi (son 20). | ModulYetkisi("fatura") · salt okunur |
| GET | `/api/v1/faturalar/{id}/e-belge/xml` | SAHIP, MUDUR, MUHASEBE | Üretilen UBL XML'i indirir (application/xml). DenetimServisi.OkumaKaydet ile okuma izi düşer. | ModulYetkisi("fatura") · BelgeKapisi içerik okuma |
| POST | `/api/v1/faturalar/{id}/e-belge/gonder` | SAHIP, MUDUR | Faz B. HAZIR belgeyi entegratöre gönderir. Sözleşme/mühür yoksa KapaliSaglayici 503 döner ve durum DEĞİŞMEZ. Idempotent: ETTN aynı kalır, çift gönderim 409. | ModulYetkisi("fatura") · EBelgeKapisi · e_belge_ayarlari.sozlesme_var && mali_muhur_var && etkin |
| POST | `/api/v1/e-belgeler/{id}/durum-sorgu` | SAHIP, MUDUR, MUHASEBE | Faz B. GİB/entegratör durumunu sorgular, e_belge_olaylari'na satır yazar. Sonuç bilinmiyorsa durum BİLİNMİYOR bırakılır, ONAY varsayılmaz. | ModulYetkisi("fatura") · sağlayıcı etkin değilse 503 |
| POST | `/api/v1/e-belgeler/{id}/iptal` | SAHIP, MUDUR | Faz B. e-Arşiv iptal / e-Fatura itiraz talebi. GerekceAlani (≥20 karakter) zorunlu; satır silinmez, durum IPTAL + olay kaydı. | ModulYetkisi("fatura") · gerekçe zorunlu · GIB_ONAY sonrası yalnız itiraz yolu |
| GET | `/api/v1/e-belgeler?durum=&baslangic=&bitis=&limit=100` | SAHIP, MUDUR, MUHASEBE | Liste + sayaçlar (hazır/gönderildi/onay/red). Sınır 100, UX-S29 uyarınca sınıra değdiği ekranda yazılır. | ModulYetkisi("fatura") · ListeOkumaKaydet |
| GET | `/api/v1/e-belge/mukellef-sorgu?vkn={vkn}` | SAHIP, MUDUR, MUHASEBE | VKN e-Fatura kayıtlı kullanıcı mı? EFATURA|EARSIV|BILINMIYOR döner. BILINMIYOR'da belge hazırlanmaz (fail-closed), kullanıcıya sebep yazılır. | ModulYetkisi("fatura") · sağlayıcı kapalıysa 503 |
| GET | `/api/v1/e-belge/ayarlar` | SAHIP | Sağlayıcı, ortam, sözleşme/mühür bayrakları ve kimlik bilgisinin VAR/YOK bilgisi. Şifre/kullanıcı adı ASLA dönmez. | ModulYetkisi("fatura") · SAHIP tam eşleşme |
| PATCH | `/api/v1/e-belge/ayarlar` | SAHIP | UX-S3: gövdede olmayan alan korunur, açık null siler. Kimlik bilgileri AES-GCM ile şifrelenip yazılır; DenetimServisi.YazmaKaydet (hassas=true), değer detaya yazılmaz. | ModulYetkisi("fatura") · SAHIP · denetim kaydı zorunlu |
| PATCH | `/api/v1/admin/tenant (DEĞİŞTİ)` | SAHIP | Mevcut uca vkn/unvan/vergi_dairesi/adres/sube_kodu alanları eklenir (UX-3-A'da açılan uç). Yeni uç açılmaz. | mevcut Admin kapıları + VKN 10/11 hane ve mod-11 doğrulaması |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/UblUretici.cs` — SAF sınıf (DB/ağ/saat yok — OzelMatrahMotoru.cs:45 deseni). Girdi: MukellefKimligi + Fatura + FaturaKalemi listesi + OzelMatrahSonuc. Çıktı: UBL-TR 2.1 XDocument. KDV'yi YENİDEN HESAPLAMAZ; kalemlerdeki kdv_matrah/kdv_tutar'ı yazar (tek motor kararı, ADR-0016).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/UblDogrulayici.cs` — XSD + iş kuralı doğrulaması (zorunlu alan, kod listesi üyeliği, toplam tutarlılık: Σ satır = fatura toplamı, kuruş sapması yasak). Geçmezse BadRequest/422, XML kaydedilmez.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/EBelgeKapisi.cs` — Fail-closed kapı (MasakKapisi/TevsikKapisi deseni): mükellef kimliği eksik, fatura KESILDI değil, KUYUM'da özel matrah kodu boş, mükellef sorgusu BILINMIYOR, sözleşme/mühür yok → red + sebep metni. Sebep ekrana olduğu gibi çıkar (UX-S9).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/EBelgeModulu.cs` — DTO + EBelgeServisi + EBelgeController tek dosya (modül deseni). [ModulYetkisi("fatura")] + [RolGerekli] tam eşleşme. XML'i BelgeKapisi ile belgeler tablosuna yazar (SahipTuru="EBelge"); ikinci dosya deposu açmaz.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/IEFaturaSaglayici.cs` — Soyutlama: MukellefSorgula, Gonder, DurumSorgula, IptalTalep. Yanıtlar record; BILINMIYOR değeri açıkça vardır. KapaliSaglayici (varsayılan kayıt) her çağrıda 503 + 'entegratör sözleşmesi tanımlı değil' döner.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/EntegratorSaglayici.cs` — Faz B. IHttpClientFactory ile REST+JWT adaptörü (KurModulu.cs:292/362 adaptör deseni). Zaman aşımı, tekrar denemesiz idempotent gönderim (ETTN anahtar), her çağrı e_belge_olaylari'na yazılır. Ortam TEST|CANLI ayarından.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs` — 38-A'da doğan sınıfa GumusOzelMatrahKodu ("808") ve KulceIstisnaKodu ("230") eklenir. Kod boş bırakılırsa ilgili belge ÜRETİLMEZ — yanlış kodla beyan etmektense durmak. (38-A henüz yazılmamıştır: klasörde dosya yok, ölçüldü.)
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — EBelge modülü kaydı; yetki anahtarı YENİ DEĞİL — mevcut "fatura" modülü kullanılır (ikinci yetki adı açılmaz).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs` — EBelgeAyarlari bağlama + IEFaturaSaglayici kaydı: yapılandırmada sağlayıcı yoksa KapaliSaglayici (fail-closed varsayılan). AddHttpClient zaten var (Program.cs:206).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` — 3 yeni varlık: EBelge=SALT_OKUNUR (düzeltme yolu: iptal/itiraz), EBelgeOlayi=SALT_OKUNUR, EBelgeAyari=DUZENLENEBILIR (PATCH + ekran aynı pakette). Kayıt yoksa DuzeltmePolitikasiTestleri kırmızı (UX-S1).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — 3 DbSet + HasQueryFilter + indeksler (FaturaKalemi deseni, KuyumcuDbContext.cs:78/148). KiraciYazmaDenetcisi yeni varlıkları otomatik kapsar.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — Yeni sayaçlar: 'UBL üretimi = 1 yer' (ikinci üretici yasak), 'KDV yeniden hesabı = 0' (UblUretici içinde çarpma/oran yok), 'entegratör HttpClient = 1 adaptör', 'şifresiz kimlik alanı = 0'.

## Ekranlar
- `/e-belgeler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/EBelgeler.razor` — Liste: tarih, fatura no, cari, tip/senaryo, ETTN, durum rozeti (renk+metin, UX-S7), GİB kodu. Hazırla/Gönder/Durum sorgu/XML indir düğmeleri; her biri istek atar ya da disabled ve sebebi yanında yazar (UX-S30). BosDurum + HataBandi + BasariBandi + Yukleniyor zorunlu. Sınır 100 ve daraltma yolu yazılır (UX-S29). (menü/yetki: KenarCubugu.razor'a 'Faturalar'ın altına eklenir; ModulYetkisi "fatura" (yeni modül anahtarı yok). UstBar rota başlığı haritasına eklenir (UX-S13).)
- `/faturalar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Faturalar.razor` — DEĞİŞTİR: satıra e-Belge durum rozeti + 'e-Belge hazırla' eylemi. KUYUM'da uygulanan özel matrah kodu (805/808) salt okunur rozetle gösterilir (UX-S21 'nereden geldi'). Mevcut id'ler değiştirilmez (UX-S4); yeni önek 'eb-'. (menü/yetki: Menü değişmez (KenarCubugu.razor:60); ModulYetkisi "fatura".)
- `/isletme` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor` — DEĞİŞTİR: mükellef kimliği bölümü (VKN, unvan, vergi dairesi, adres, şube kodu) — UBL zorunlu alanları. Eksikse 'e-Belge kesilemez' uyarısı ve eksik alanların listesi. Oluşturma/düzenleme aynı alan kümesi (UX-S26). (menü/yetki: Mevcut menü; SAHIP (PATCH /admin/tenant kapısı).)
- `/ayarlar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — DEĞİŞTİR: e-Belge sağlayıcı kartı — sağlayıcı, ortam (TEST|CANLI), sözleşme/mühür bayrakları, kimlik VAR/YOK (değer asla basılmaz). ProAlan içinde (UX-S5); basit modda gizli, yasal kapı tetiklenirse ZorunluGorunur. (menü/yetki: SAHIP; ModulYetkisi "fatura".)

## İş kuralları (fail-closed)
- FAIL-CLOSED KİMLİK: tenants.vkn/unvan/vergi_dairesi/adres'ten biri boşsa hazırla ucu 422 ve hangi alanın eksik olduğunu yazar. Eksik alan boş etiketle UBL'e yazılmaz.
- FAIL-CLOSED SÖZLEŞME: sozlesme_var veya mali_muhur_var false ise gönder ucu 503 ve e_belgeler durumu HAZIR kalır. 'Gönderildi' yalnız entegratörün 2xx yanıtı + belge numarası ile yazılır (Anayasa md.1).
- TEK KDV MOTORU: UblUretici çarpma/oran uygulamaz; kalem ve başlık tutarlarını okur. Σ kalem ≠ başlık toplamı ise belge üretilmez (ikizlenmiş kural yasağı, ADR-0016).
- ÖZEL MATRAH KODU ZORUNLU: KdvTuru='KUYUM' faturada 805 (altın) / 808 (gümüş) kodu yoksa belge üretilmez. Kod ürünün metal cinsinden türetilir; ikisi de yoksa 422, STANDART'a sessizce düşülmez.
- KÜLÇE İSTİSNASI: külçe altın/gümüş kalemi 230 istisna koduyla yazılır; istisna ile özel matrah AYNI kalemde birlikte kullanılmaz.
- TİP SEÇİMİ SORGUYLA: EFATURA/EARSIV kararı mükellef sorgusundan gelir; BILINMIYOR ise belge hazırlanmaz. Yanlış tip belgeyi GİB reddeder, kullanıcı sebebini bilmez.
- ETTN DEĞİŞMEZ: bir fatura için ETTN bir kez üretilir; yeniden hazırlama XML'i yeniler ama ETTN'i korur. Gönderim idempotenttir; çift gönderim 409.
- BELGE DEĞİŞMEZ: e_belgeler ve e_belge_olaylari için PATCH/DELETE ucu yoktur (UX-S2). Düzeltme iptal/itiraz + yeni belge ile yapılır; ekranda DegismezIpucu bunu anlatır (UX-S25).
- GİB ONAYLI BELGE İPTAL EDİLMEZ: onaylı e-Fatura için yalnız itiraz talebi kaydı açılır; durum GIB_ONAY olarak kalır.
- DURUM BİLİNMİYORSA ONAY VARSAYILMAZ: sorgu zaman aşımına uğrarsa durum değişmez, olay kaydına http_kod ve hata yazılır.
- SIR SIZDIRMA YOK: entegratör kullanıcı adı/parolası şifreli saklanır, hiçbir uçta/logda/denetim detayında düz metin geçmez; ekranda yalnız VAR/YOK.
- KİRACI İZOLASYONU: üç yeni tablo IKiraciVarlik; sorgu filtresi + elle TenantId şartı (ADR-0010). XML üretimi başka kiracının faturasını okuyamaz.

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/UblUreticiTestleri.cs` [SAF]
  - KuyumFaturasi_HasBedeli_MatrahDisinda_Yazilir
  - OzelMatrahKodu805_AltinKaleminde_Yazilir
  - GumusZiynet_808_Kodu_Yazilir
  - KulceKalemi_230_IstisnaKodu_Yazilir_OzelMatrahla_Birlikte_Degil
  - SatirToplamlari_BaslikToplamina_Kurusu_Kurusuna_Esit
  - UblUretici_KdvyiYenidenHesaplamaz_KalemDegerleriniYazar
  - ZorunluAlanEksik_BelgeUretilmez_422
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/EBelgeKapisiTestleri.cs` [SAF]
  - MukellefKimligiEksik_Kapi_Reddeder
  - FaturaTaslak_Kapi_Reddeder
  - KuyumFaturasi_OzelMatrahKodsuz_Kapi_Reddeder
  - MukellefSorgusu_Bilinmiyor_Kapi_Reddeder
  - SozlesmeYok_GonderimKapisi_503
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/EBelgeTestleri.cs` [DB (gerçek MySQL)]
  - Hazirla_EBelgeSatiriVeOlayKaydiYazar
  - Hazirla_IkinciKez_EttnKorunur_XmlYenilenir
  - BaskaKiracininFaturasi_EBelgeGoremez_Sizinti_Yok
  - Ayarlar_Patch_KimlikBilgisiSifreliYazilir_DuzMetinYok
  - Gonderildi_Belge_PatchDelete_UcuYok
  - TenantVkn_ModUlDogrulamasi_Gecersizde_400
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/EBelgeHttpTestleri.cs` [HTTP (WebApplicationFactory)]
  - Hazirla_Yetkisiz_Rol_403
  - Hazirla_Eksik_Kimlik_422
  - Gonder_SozlesmeYok_503_DurumDegismez
  - DurumSorgu_ZamanAsimi_DurumDegismez_OlayYazilir
  - Xml_Indirme_OkumaIzi_Duser
  - Ayarlar_Get_ParolaDondurmez
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/EBelgeEkranTestleri.cs` [bUnit]
  - EBelgeler_BosDurum_Gosterilir
  - Hazirla_Dugmesi_KimlikEksikse_Kapali_Ve_Sebep_Yazili
  - Gonder_Dugmesi_SozlesmeYoksa_Kapali
  - Durum_Rozeti_Renk_Yaninda_Metin_Tasir
  - Ayarlar_Parola_Ekranda_Basilmaz_VarYok_Gosterilir
  - EBelge_DegismezIpucu_IptalYolunu_Anlatir

## Mutasyon kanıtları
- KapaliSaglayici'yi 503 yerine 200 döndür → EBelgeKapisiTestleri.SozlesmeYok_GonderimKapisi_503 ve EBelgeHttpTestleri.Gonder_SozlesmeYok_503_DurumDegismez kırmızı.
- UblUretici'de 805 kodunu sabit yaz (gümüşte de 805) → UblUreticiTestleri.GumusZiynet_808_Kodu_Yazilir kırmızı.
- Σ kalem = başlık kontrolünü kaldır → UblUreticiTestleri.SatirToplamlari_BaslikToplamina_Kurusu_Kurusuna_Esit kırmızı.
- Mükellef sorgusu BILINMIYOR'u EARSIV'e düşür → EBelgeKapisiTestleri.MukellefSorgusu_Bilinmiyor_Kapi_Reddeder kırmızı.
- Zaman aşımında durumu GIB_ONAY yaz → EBelgeHttpTestleri.DurumSorgu_ZamanAsimi_DurumDegismez_OlayYazilir kırmızı.
- Ayarlar GET'ine parolayı ekle → EBelgeHttpTestleri.Ayarlar_Get_ParolaDondurmez ve EBelgeEkranTestleri.Ayarlar_Parola_Ekranda_Basilmaz_VarYok_Gosterilir kırmızı.
- Kimlik bilgisini şifresiz yaz → EBelgeTestleri.Ayarlar_Patch_KimlikBilgisiSifreliYazilir_DuzMetinYok kırmızı + denetim.sh 'şifresiz kimlik alanı = 0' sayacı bozulur.
- e_belgeler sorgusundan elle TenantId şartını sil → EBelgeTestleri.BaskaKiracininFaturasi_EBelgeGoremez_Sizinti_Yok kırmızı.
- Hazırla düğmesini disabled="@(false)" ile aç (UX-S16 tuzağı) → EBelgeEkranTestleri.Hazirla_Dugmesi_KimlikEksikse_Kapali_Ve_Sebep_Yazili kırmızı.
- Yeniden hazırlamada ETTN'i yeniden üret → EBelgeTestleri.Hazirla_IkinciKez_EttnKorunur_XmlYenilenir kırmızı.

## Canlı doğrulama
- Test kiracısı aç; PATCH /api/v1/admin/tenant ile VKN/unvan/vergi dairesi/adres yaz → 200. Eksik bırakılan kiracıda POST .../e-belge/hazirla → 422 ve gövdede eksik alan adları.
- KUYUM faturası kes (kalemli, 1-A yolu) → POST /api/v1/faturalar 201. Ardından POST /api/v1/faturalar/{id}/e-belge/hazirla → 201. SQL: SELECT belge_tipi, senaryo, ettn, durum, ubl_sha256 FROM e_belgeler WHERE tenant_id='<test>' → 1 satır, durum='HAZIR'.
- GET /api/v1/faturalar/{id}/e-belge/xml → 200, content-type application/xml. XML'de cbc:TaxExemptionReasonCode=805 ve kuyum kaleminde matrah = toplam − has bedeli olduğu gözle doğrulanır; SHA-256 e_belgeler.ubl_sha256 ile birebir eşleşir.
- Gümüş ziynet kalemli fatura → XML'de 808; külçe kalemli fatura → 230. İkisi aynı kalemde çıkarsa HATA (beklenen: çıkmaz).
- Sözleşme kapalıyken POST .../e-belge/gonder → 503 ve SQL: SELECT durum FROM e_belgeler WHERE id='<id>' → hâlâ 'HAZIR'; SELECT count(*) FROM e_belge_olaylari WHERE e_belge_id='<id>' ≥ 1 (http_kod=503).
- Başka kiracının oturumuyla GET /api/v1/faturalar/{id}/e-belge → 404 (sızıntı yok). SQL: SELECT count(*) FROM e_belgeler WHERE tenant_id<>'<test>' AND fatura_id='<id>' → 0.
- PATCH /api/v1/e-belge/ayarlar ile kimlik yaz → 204. SQL: SELECT kullanici_adi_sifreli IS NOT NULL, LOCATE('<kullanıcı adı>', CAST(kullanici_adi_sifreli AS CHAR)) FROM e_belge_ayarlari → (1, 0). GET /ayarlar yanıtında parola alanı yok.
- Faz B (sözleşme geldiğinde): entegratör TEST ortamına gerçek gönderim → 200 + belge numarası; SQL durum='GONDERILDI'. Sonra POST /durum-sorgu → GİB kodu ve mesajı e_belge_olaylari'na yazılır. Sahte/mock ile 'oldu' denmez.
- Bitince: dotnet test 0 başarısız · dotnet/denetim.sh TEMİZ · test kiracısı silinir; yetim-tara.sh çıkış 0.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/UblUretici.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/UblDogrulayici.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/EBelge/EBelgeKapisi.cs · EBelgeModulu.cs · IEFaturaSaglayici.cs · EntegratorSaglayici.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/EBelge.cs (YENİ: EBelge, EBelgeOlayi, EBelgeAyari varlıkları)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs (DEĞİŞTİR: tenants 5 kolon)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs · Data/Migrations/ (YENİ migration + snapshot)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs · Program.cs · Modules/Admin/AdminModulu.cs · Modules/Meta/DuzeltmePolitikasi.cs (DEĞİŞTİR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs (DEĞİŞTİR — 38-A ürünü) · Modules/Fatura/FaturaModulu.cs (DEĞİŞTİR: yanıta e-belge özeti)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json + appsettings.Development.json (DEĞİŞTİR: EBelge bölümü, sır YOK)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/EBelgeler.razor (YENİ) · Pages/Faturalar.razor · Pages/Isletme.razor · Pages/Ayarlar.razor · Layout/KenarCubugu.razor · Layout/UstBar.razor (DEĞİŞTİR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs (DEĞİŞTİR: TR+EN metinler, UX-S12)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh + Kuyumcu.Tests/{UblUreticiTestleri,EBelgeKapisiTestleri,EBelgeTestleri,EBelgeHttpTestleri,EBelgeEkranTestleri}.cs`

## Fazlar
- **Faz A — Sözleşmeden bağımsız e-Belge hazırlık katmanı (SEVK EDİLEBİLİR ASGARİ)** (38-A biter bitmez; entegratör sözleşmesi BEKLENMEZ): tenants mükellef kimliği kolonları + Isletme ekranı; UblUretici + UblDogrulayici (805/808/230); e_belgeler/e_belge_olaylari tabloları; hazırla + XML indir + liste uçları; /e-belgeler ekranı; EBelgeKapisi; KapaliSaglayici (gönderim 503). Çıktı: GİB şemasına uyan, indirilebilir, denetlenebilir UBL. Dış bağımlılık sıfır.
- **Faz B — Entegratör gönderimi ve GİB durum sorgusu** (Kurucu sözleşme + mali mühür imzası geldikten sonra (3-6 hafta dış takvim); test ortamı erişimi daha erken gelirse TEST ortamıyla başlanır): EntegratorSaglayici (REST+JWT), mükellef sorgu, gönder, durum sorgu, iptal/itiraz; e_belge_ayarlari şifreli kimlik + Ayarlar kartı; canlı doğrulama entegratörün TEST ortamına GERÇEK gönderimle yapılır, mock ile 'oldu' denmez.
- **Faz C (bu paketin dışı, kuyruğa)** (Faz B kararlı çalıştıktan sonra): Gelen e-Fatura çekme ve alış kaydına bağlama; e-İrsaliye (paket 42 üstüne); toplu gönderim ve kontör/iş ortaklığı raporlaması.
## Mevzuat dayanağı
- VUK 509 s.n. Genel Tebliğ IV.2.4.3 (GİB konsolide metin) — 1/1/2026'dan itibaren e-Arşiv Fatura tutarına bakılmaksızın zorunlu; kâğıt düzenlenirse her bir fatura için ayrı ayrı VUK 353 cezası. — **YUKSEK** — PAZAR-ARASTIRMASI-2026-09-16.md:61'de GİB birincil metin bağlantısıyla kayıtlı. Bu paketin varlık sebebi: e-Fatura artık farklılaştırıcı değil eleme kriteri.
- VUK 589 s.n. Tebliğ (RG 31.12.2025) — basit usul ve işletme hesabı esasında 3.000 TL kuralı 31/12/2026'ya uzatıldı; onlarda tutarsız zorunluluk 1/1/2027. — **YUKSEK** — Mükellef grubu ayrımı ürün içinde MODELLENMELİ; tek eşik varsaymak yanlış uyarı üretir. Eşikler tarihli yürürlüklü parametre tablosuna taşınmalı (pazar araştırması öneri 7).
- GİB UBL-TR Kod Listeleri v1.26 — özel matrah kodları 805 (altın ziynet/sikke) ve 808 (gümüş ziynet/sikke); fatura tipi OZELMATRAH; KDV istisna kodu 230 (17/4-g külçe altın/gümüş). — **YUKSEK** — PAZAR-ARASTIRMASI-2026-09-16.md:63. Mühürlü paket 38 yalnız 805'ten söz ediyor; 808 ve 230 bu pakette kapsama alınır.
- KDVK md.23/(e) — kuyumda özel matrah (has altın bedeli matrah dışı). — **DOGRULANMALI** — OzelMatrahMotoru.cs:16-17 yorumunda da DOGRULANMALI işaretli: birincil metin okunmadı. Bu paket kuralı DEĞİŞTİRMEZ, yalnız belgeye yansıtır.
- GİB e-Belge duyurusu 16.03.2026 — 1 Nisan 2026'dan itibaren faaliyet kodu ↔ KDV oranı kontrolü; yeni kod 555 (KDV oran kontrolüne tabi olmayan satışlar). — **ORTA** — Duyuru sirküler kopyasından okunmuş (PAZAR-ARASTIRMASI-2026-09-16.md:76). Demirbaş/vitrin satışı ve yansıtma faturası için 555 desteklenmezse entegratör reddedebilir — Faz B'de ele alınmalı.
- VUK md.253 — belge saklama süresi; basılmış belge içeriği sabit kalmalı. — **ORTA** — FaturaKalemi.cs:12-13 aynı gerekçeyi taşıyor. e_belgeler ve XML değişmezdir; düzeltme iptal/itiraz + yeni belge yoludur.
- Kuyum Ticareti Yönetmeliği md.11/(e) — taş ibaresi belge üzerinde. — **ORTA** — FaturaKalemi.tas_ibaresi (FaturaKalemi.cs:56) UBL kalem açıklamasına kopyalanır; referans değil metin.

## Riskler
- Entegratör sözleşmesi gecikirse Faz B kilitlenir. Azaltma: Faz A tamamen bağımsız tasarlandı ve tek başına sevk edilebilir; gecikme müşteriye 'XML üretiyoruz, gönderimi entegratör açınca' diye dürüstçe söylenir.
- Entegratörün gerçek API sözleşmesi görülmeden yazılan adaptör yanlış çıkabilir. Azaltma: IEFaturaSaglayici dar tutulur (4 metot), adaptör Faz B'de TEST ortamına karşı yazılır; şimdiden ayrıntılı uygulama yazılmaz.
- XSD şema dosyalarının dağıtımı/lisansı ve depoya konması DOĞRULANMALI. Azaltma: şema dosyaları ayrı klasörde, yoksa doğrulayıcı fail-closed davranır (belge üretilmez), 'doğrulandı' varsayılmaz.
- 805/808 kod seçimi ürünün metal cinsine bağlı; ürün kartında metal cinsi ayrımı net değilse yanlış kod yazılır. Azaltma: cins belirsizse 422, tahmin yok. DOĞRULANMALI: Urun varlığında metal cinsi kolonu ölçülmedi.
- Mali mühür/e-imza entegratörde tutulacak; sır yönetimi yanlış kurulursa kiracı kimliği sızar. Azaltma: AES-GCM alan şifreleme, ekranda VAR/YOK, denetim detayına değer yazılmaz; Mehmet (06 Güvenlik) kapısı.
- Çift gönderim riski (kullanıcı iki kez basar) → GİB'de mükerrer belge. Azaltma: ETTN idempotenti + UNIQUE (tenant_id, fatura_id) + 409.
- Faturalar.razor ve Entities.cs/KuyumcuDbContext.cs çok paketli ortak dosyalar; aynı dalgada paralel çalışma çakışır. Azaltma: yaz-seri yasası, dalga 5 içinde seri uygulama.
- Test ortamında gerçek gönderim yapılamazsa 'çalışıyor' kanıtı üretilemez. Azaltma: kabul ölçütü açıkça 'entegratör TEST ortamına gerçek gönderim'; mock ile yeşil sayılmaz (Anayasa md.1).

## Açık sorular
- Hangi entegratör? (NES/İzibiz/…) — REST+JWT mi SOAP mı, test ortamı erişimi sözleşme öncesi veriliyor mu? Kurucu kalemi; teknik seçim Faz B'yi doğrudan belirler.
- Ticari model: toplu kontör / sabit paket / komisyon — FinOps (Mustafa) ile tasarruf hesabına girer mi, ürün içinde kontör sayacı gösterilecek mi?
- Urun varlığında metal cinsi (altın/gümüş/platin) ayrımı hangi kolonda? 808 kodu bu ayrıma bağlı — ÖLÇÜLMEDİ, uygulamadan önce doğrulanmalı.
- Mükellef VKN sorgusu için GİB kullanıcı listesi entegratörden mi çekilecek, yerel önbellek tutulacak mı (tutulursa tazeleme süresi ve KVKK etkisi)?
- Şube kodu ve fatura seri/sıra numarası politikası: mevcut FTR-{yıl}-{sıra} numaralandırması e-belge belge numarasıyla nasıl ilişkilendirilecek (aynı mı, ayrı mı)?
- UBL-TR sürümü: 1.2.1 mi 2.1 mi — kaynaklarda iki adlandırma karışık geçiyor (belge UBL 2.1 üzerine UBL-TR 1.2.x özelleştirmesi). Birincil kılavuzdan DOĞRULANMALI.
- Mükellef grubu (bilanço / işletme hesabı / basit usul) kiracı ayarı olarak tutulacak mı? 3.000 TL eşiğinin 2026'da kime uygulanacağı buna bağlı.
- 555 kodu (KDV oran kontrolü dışı satışlar) Faz A'ya mı Faz B'ye mi girsin — demirbaş/yansıtma faturası kuyumcuda ne sıklıkta?

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — Tam metin okundu; tasarım mimari açıdan sağlam: tek KDV motoru kararı korunuyor (UblUretici KDV'yi yeniden hesaplamıyor, ADR-0016 ile tutarlı), mevcut mekanizmalar yeniden kullanılıyor (4-A BelgeKapisi ile ikinci dosya deposu açılmıyor, 'fatura' yetki anahtarı korunuyor, KurAdaptoru/IHttpClientFactory deseni, MasakKapisi/GiderPusulasiKapisi kapı deseni, IKiraciVarlik+HasQueryFilter), veri modeli doğru (faturalar'a durum kolonu koymayıp e_belgeler'i ayrı yaşam döngüsü varlığı yapmak ikinci gerçeği önlüyor; UNIQUE(tenant_id,fatura_id) + ETTN idempotenti çift gönderimi kapatıyor; append-only olay tablosu VUK md.253 ile uyumlu). Faz A'nın dış bağımlılığı sıfır olması önkoşul sırasını doğru çözüyor. KOŞULLU olmasının sebebi üç ölçülmüş boşluk: (a) zorunlu önkoşul 38-A ürünü KdvAyarlari.cs kodda YOK, (b) 805/808 ayrımının dayandığı metal cinsi Urun varlığında YOK, (c) UBL-TR sürüm adlandırması belgenin kendi içinde çelişik. Bunlar kapatılmadan uygulama başlarsa 'varsayım' üretir (Anayasa md.1).
  - Koşullar: 38-A ÖNKOŞULU TEYİT: Modules/Fatura/ altında bugün yalnız FaturaModulu.cs + OzelMatrahMotoru.cs var, KdvAyarlari.cs YOK (ls ile ölçüldü). E-BELGE bu dosyayı YARATMAZ; 38-A mühürlenip KdvAyarlari.cs doğduktan sonra yalnız 808/230 alanı eklenir. · METAL CİNSİ BOŞLUĞU (açık soru kapatılmalı): Urun varlığında metal cinsi kolonu YOK — Entities.cs:187-189 yalnız ayar_id + milyem taşıyor. 805/808 seçimi ayar_id→metal eşlemesi olarak TEK yerde (KdvAyarlari) tanımlanır; eşleme yoksa 422, tahmin yasak. · UBL SÜRÜM ÇELİŞKİSİ: belge başlıkta 'UBL-TR 2.1' diyor, açık sorular bölümü (satır 199) 'UBL 2.1 üzerine UBL-TR 1.2.x' diyor. Faz A kodu yazılmadan birincil GİB kılavuzundan doğrulanıp doküman tek adlandırmaya çekilmeli. · PROGRAM.CS TEK-EL: AddHttpClient Program.cs:206'da zaten kayıtlı; ikinci AddHttpClient eklenmez, yalnız IEFaturaSaglayici/KapaliSaglayici kaydı yapılır. Program.cs dokunuşu dalga 5 içinde T-1 sır turuyla aynı seri elde birleştirilir. · SIR DİSİPLİNİ — MEVCUT MEKANİZMA: Common/AlanSifreleme.cs mevcut (ölçüldü); e_belge_ayarlari şifrelemesi bu sınıfı kullanır, ikinci şifreleme yardımcısı yazılmaz. appsettings'e kimlik/parola düz yazılmaz (T-1 ortam değişkeni disiplini). · XSD ŞEMA DEPOSU: doküman 'ayrı klasör' diyor ama yol vermiyor (risk md.186). Şema dosyalarının yeri Modules/EBelge/Semalar/ olarak sabitlenir ve T-3 dizin tekliği kuralına yazılır; şema yoksa doğrulayıcı fail-closed (belge üretilmez). · DENETİM İZİ YENİDEN KULLANIM: XML indirme/liste izi için yeni mekanizma açılmaz — mevcut DenetimServisi.OkumaKaydet / ListeOkumaKaydet kullanılır (HurdaModulu.cs:37,56 deseni). DuzeltmePolitikasi.cs mevcut (Modules/Meta/), 3 varlık oraya EKLENİR, yeni harita açılmaz. · PAKET BOYUTU / SEVK AYRIMI: 9-13 gün tek dalga için büyük. Dalga 5'e YALNIZ Faz A alınır (UblUretici + UblDogrulayici + EBelgeKapisi + 2 tablo + hazırla/XML/liste + KapaliSaglayici 503). Faz B (EntegratorSaglayici + e_belge_ayarlari) sözleşme gelene dek kuyrukta bekler.
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: KOSULLU** — Önceki turda kesik ulaşan metin şimdi tam okundu: mutasyon kanıtları isimlendirilmiş testlerle eşleşiyor (10 madde), SAF/DB/HTTP/bUnit dengesi kurulmuş, canlı doğrulama çoğunlukla HTTP kodu + SQL ile ölçülebilir, test kiracısı temizliği ve yetim-tara.sh satır 154'te var. Ancak Faz B'nin en kritik yolu (gerçek gönderim başarı senaryosu) otomatik test setinde sahte adaptörle karşılığı yok; yalnız 'gerçek entegratöre karşı canlı doğrulama' ile kapatılmış — bu, CI'da tekrarlanamayan, insan onayına bağlı tek nokta yaratır ve regresyonu yakalayamaz. Zaman aşımı testinin determinizmi de belirsiz. Bu boşluklar kapatılırsa (koşul 1-2 öncelikli) paket kabul edilebilir; Faz A kısmı (SAF UblUretici/UblDogrulayici/EBelgeKapisi testleri) kendi içinde sağlam ve sevk edilebilir durumda.
  - Koşullar: Faz B EBelgeHttpTestleri listesinde (satır 118-124) başarılı gönderim yolu (Gonder_Basarili_Durum_GONDERILDI_Yazar, ETTN korunur) testi YOK; yalnız 503 negatif test var. Sahte HttpMessageHandler ile deterministic pozitif yol testi eklenmeli. · DurumSorgu_ZamanAsimi_DurumDegismez_OlayYazilir testinin (satır 122) zaman aşımını nasıl ürettiği (gerçek ağ beklemesi mi, enjekte sahte sağlayıcı mı) belirtilmemiş; gerçek timeout ile kararsız (flaky) test riski — CancellationToken/sahte adaptör zorunlu kılınsın. · UblDogrulayici.cs (satır 64) için ayrı test/mutasyon kaydı yok; şema/XSD doğrulamasını devre dışı bırakma mutasyonu Mutasyon Kanıtları listesinde (satır 133-143) eksik — hangi test kırmızı olacağı yazılmalı. · EntegratorSaglayici (satır 68) IHttpClientFactory adaptörü için otomatik testte gerçek ağ çağrısı yasak olmalı; testte kullanılacak sahte HttpMessageHandler / test double açıkça isimlendirilmeli (KurAdaptoru desenindeki test şablonu referans gösterilsin). · 555 kodu (satır 179, 201) hangi faza (A/B) gireceği net değil; test kapsamı da bu kararsızlığı yansıtıyor — karar netleşene kadar testler bu kodu kapsam dışı bırakmalı, aksi halde kapsam kayması riski. · Canlı doğrulama Faz B adımı (satır 153) 'gerçek gönderim... mock ile oldu denmez' diyor — doğru fail-closed disiplin, ancak bu CI'da tekrarlanamaz; regresyon güvencesi yalnız koşul 1'deki sahte-adaptör testine kalır, o test eksikse Faz B'de otomatik kapı yok demektir.
- **YENİ: Güvenlik merceğinden (Mehmet) üç tasarımı inceledim: kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek/TLS, içe aktarım riski ve MFA koşulu açısından. Program.cs:283 canlı kodda script-src 'self' 'wasm-unsafe-eval' olarak doğrulandı (unsafe-inline yok) — PWA-1'in CSP taahhüdünün mevcut temeli sağlam. docker-compose.yml:7'de MySQL root parolası düz metin olarak canlıda doğrulandı; hiçbir pakette bu ele alınmıyor (T-1/F-6/F-7'ye bırakılmış), ayrı takip gerekir.: KOSULLU** — Tasarım fail-closed ilkesine büyük ölçüde uyuyor: KapaliSaglayici varsayılan 503, EBelgeKapisi mükellef/kod/sözleşme eksikliğinde red, ETTN idempotent, e_belgeler/e_belge_olaylari append-only (PATCH/DELETE yok), kiracı filtresi + elle TenantId (IKiraciVarlik), kimlik bilgileri AES-GCM şifreli ve ekranda yalnız VAR/YOK, denetim kaydı (DenetimServisi) hem okuma hem yazma için var, roller en az yetki ilkesine uygun (ayarlar sadece SAHIP). Veto gerektirecek bir mimari açık yok; ancak MFA-45 bağımlılığının bu pakete açıkça bağlanmaması, PII sanitizasyonunun ve anahtar rotasyonunun belgede boşluk bırakması ile KVKK önbellek sorusunun cevapsız kalması EVET için yeterli değil — KOŞULLU.
  - Koşullar: PATCH /e-belge/ayarlar (satır 59) SAHIP kimlik-yazma ucu; 45-API MFA kapısı canlıya alınmadan bu uç prod'da etkinleşmemeli — dalga sırası 2b'de 45-API E-BELGE'den (Dalga 5) önce bitirilmeli, bağımlılık belgeye açıkça yazılsın. · e_belge_olaylari.mesaj (satır 42) 'kişisel veri yok' notu var ama GİB/entegratör serbest metin hata mesajları için sanitize/maskeleme kuralı tanımlı değil; kayıttan önce PII filtresi eklensin. · AlanSifreleme.cs (satır 22,66) ile şifreleme anahtarının kaynağı/rotasyonu bu belgede tanımsız; T-1 ortam-değişkeni + fail-closed sır disiplinine tabi olduğu ve appsettings'e sır yazılmayacağı (satır 165) açıkça teyit edilsin. · sifre_surum kolonu (satır 43) var ama anahtar sızıntısı/rotasyon senaryosunda yeniden şifreleme prosedürü yok; en az bir mutasyon/test ile rotasyon davranışı kanıtlanmalı. · XSD/şematron şema dosyalarının lisans/dağıtım durumu DOĞRULANMALI (satır 186) kapanmadan Faz A sevk edilmesin; doğrulayıcının fail-closed kaldığı (belge üretilmez) ayrıca test ile kanıtlanmalı. · Mükellef VKN önbellekleme açık sorusu (satır 197) KVKK etkisi netleşmeden önbellek AÇILMASIN; sorgu taze yapılmaya devam etsin ya da DPO/Mehmet onayı ayrıca alınsın. · Faz B TEST ortamı gerçek gönderimi (satır 153) yalnız izole test kiracısı verisiyle yapılmalı; gerçek müşteri VKN/fatura içeriği GİB TEST ortamına sızmamalı, bu canlı doğrulama adımına açıkça yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Dalga 5'e YALNIZ Faz A alınır (UblUretici + UblDogrulayici + EBelgeKapisi + 2 tablo + hazırla/XML/liste + KapaliSaglayici 503); Faz B (EntegratorSaglayici + e_belge_ayarlari) entegratör sözleşmesi gelene dek kuyrukta bekler.
- 38-A mühürlenip KdvAyarlari.cs doğmadan E-BELGE bu dosyayı YARATMAZ; yalnız 808/230 alanı eklenir ve bu kapsam genişlemesi 38-A oylamasına geri bildirilir (38 mührünün '3 yeni tablo' istisnası da işlenir).
- 805/808 seçimi ayar_id→metal eşlemesi olarak TEK yerde (KdvAyarlari) tanımlanır; Urun'da metal cinsi kolonu olmadığı için eşleme yoksa 422 döner, tahmin yasaktır.
- UBL-TR sürüm adlandırma çelişkisi (2.1 vs UBL 2.1 + UBL-TR 1.2.x) Faz A kodu yazılmadan GİB birincil kılavuzundan doğrulanıp tek adlandırmaya çekilir.
- Program.cs tek-el: ikinci AddHttpClient eklenmez (Program.cs:206 mevcut), yalnız IEFaturaSaglayici/KapaliSaglayici kaydı yapılır; Entities.cs + KuyumcuDbContext.cs + migration turu dalga içinde SERİ yürütülür.
- Sır: yalnız mevcut Common/AlanSifreleme.cs kullanılır, ikinci şifreleme yardımcısı yazılmaz; anahtar kaynağı/rotasyonu T-1 ortam-değişkeni hattına tabidir, sifre_surum ile rotasyon davranışı en az bir testle kanıtlanır; appsettings'e sır yazılmaz.
- XSD/şematron şemaları Modules/EBelge/Semalar/ altında sabitlenir (T-3 dizin tekliği); lisans/dağıtım sorusu kapanmadan Faz A sevk edilmez; şema yoksa doğrulayıcı fail-closed (belge üretilmez) testle kanıtlanır.
- Yeni denetim mekanizması açılmaz: mevcut DenetimServisi.OkumaKaydet/ListeOkumaKaydet kullanılır; 3 varlık mevcut Modules/Meta/DuzeltmePolitikasi haritasına eklenir; Faturalar.razor'da yalnız 'eb-' önekli yeni id'ler, mevcut id'lere dokunulmaz (UX-S4 bağlayıcı).
- Testler: sahte HttpMessageHandler ile deterministik POZİTİF gönderim yolu (durum GONDERILDI, ETTN korunur) eklenir; zaman aşımı enjekte adaptör/CancellationToken ile üretilir (gerçek ağ beklemesi yasak); UblDogrulayici için mutasyon kaydı yazılır; otomatik testte gerçek ağ çağrısı yasak (KurAdaptoru test şablonu referans); 555 kodu karar netleşene dek kapsam dışı.
- PATCH /e-belge/ayarlar 45-API MFA kapısına bağlı olduğu paket metnine açıkça yazılır; e_belge_olaylari.mesaj için PII sanitize filtresi eklenir; mükellef VKN önbelleği KVKK onayı olmadan AÇILMAZ; Faz B GİB TEST gönderimi yalnız izole test kiracısı verisiyle yapılır ve 'mock ile oldu' denmez.
