# Paket T-4 — Destek ve işletim görünürlüğü — sürüm/derleme/migration damgası + korelasyonlu hata kimliği (T-4a) ve maskeli destek paketi (T-4b)

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: Faz A (T-4a) 0,5-1 gün · Faz B (T-4b) 1-1,5 gün · toplam 1,5-2,5 gün. Mühür takvimi T-4a için 0,5 gün diyor (MIMARI-KALAN-PAKETLER-2026-09-15.md:707); fark, bUnit + HTTP test yükü ve CSP uyumlu kopyalama yolundan gelir. ±%40. gün · Önkoşullar: T-1 + F-6 (birleşik yayın paketi) — sürüm/derleme damgasının gerçek değeri publish hattından gelir; T-4a yayın betiği yazılmadan damgayı sahte bir sabitle doldurmaz., TB-M1 (8 sıfır baytlık migration + .mutorj temizliği; silme Kurucu'da) — kirli migration dizini sayımı yanıltır: dotnet/Kuyumcu.Api/Data/Migrations altında 9 adet 0-2 baytlık FiyatListesiVeKampanya/VadeTakibi dosyası duruyor., Faz B için Faz A (sürüm künyesi paketin ilk dosyasıdır)., Faz B için UX bileşen kümesi (GerekceAlani, BosDurum, DegismezIpucu, HataBandi) — hepsi mevcut; yeni bileşen kurulmaz. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Kullanıcı hata gördüğünde tek bir kopyalanabilir kimlik söyleyebilsin; destek o kimlikle sunucu günlüğündeki isteği, çalışan sürümü/derlemeyi ve uygulanmış migration kümesini kesin eşleştirsin. Faz B'de kişisel veri içermeyen, gerekçeli ve denetim kaydı düşen maskeli destek paketi indirilebilsin.

## Kodda bugünkü durum
Korelasyon yok: HataYakalayici gövdeye yalnız message/error/statusCode yazar (dotnet/Kuyumcu.Api/Common/ApiException.cs:33-85); 500 dalında TraceIdentifier ne loglanır ne döner (a.g.e. :69-71). İstemci ApiHata yalnız StatusCode+mesaj taşır (dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs:10-13,129-142). Sürüm ucu yok; tek işletim ucu app.MapHealthChecks("/saglik") (Program.cs:313).

## Kapsam
**İçinde:**
- T-4a: her isteğe kimlik üreten korelasyon ara katmanı — yanıt başlığı X-Istek-Kimligi + hata gövdesinde istekKimligi alanı + ILogger kapsamında aynı değer.
- T-4a: HataYakalayici'nin dört dalının (ApiException, BadHttpRequest, 500, yeni) tamamında aynı kimliğin gövdeye yazılması (ApiException.cs:33-85).
- T-4a: GET /api/v1/meta/surum — ürün sürümü, InformationalVersion, derleme zamanı (UTC), ortam adı, uygulanmış migration sayısı ve son migration adı.
- T-4a: SAHIP'e özel GET /api/v1/meta/surum/ayrinti — uygulanmış migration listesi + bekleyen migration listesi (fail-closed uyarı).
- T-4a ekran: Ayarlar sayfasında sürüm/derleme/migration damgası kartı, tek tıkla kopyalanabilir künye.
- T-4a ekran: HataBandi bileşeninde hata kimliği rozeti + kopyala; ApiIstemci hata kimliğini ApiHata'ya taşır.
- T-4b: POST /api/v1/destek/paket (gerekçe zorunlu) + POST /api/v1/destek/paket/{jeton}/indir + GET /api/v1/destek/paket/gecmis — zip, SAHIP.
- T-4b: paket içeriği maskeli — sürüm künyesi, migration listesi, sağlık çıktısı, kapı/ayar özeti, son 200 denetim kaydının maskelenmiş özeti, kiracı kimliği kısaltılmış.
- T-4b ekran: /destek sayfası (SAHIP) — paket oluştur, gerekçe alanı, geçmiş tablosu, indir.
- Dalga sonu kapısında belge damgalama girdisi: /api/v1/meta/surum çıktısındaki migration/test sayıları rapor hattına verilir (MÜHÜR 2 / MADDE 1, MIMARI...2026-09-15.md:706).

**Dışında:**
- Telemetri/APM, OpenTelemetry, metrik ihracı, dış günlük toplayıcı (Seq/ELK) — kurul satırındaki 'telemetri yok' kalemi T-4'te açılmaz, ayrı pakettir.
- Sıfır baytlık 8 migration dosyasının ve .mutorj artıklarının SİLİNMESİ — TB-M1 paketidir, silme Kurucu'dadır (bkz. dotnet/Kuyumcu.Api/Data/Migrations/*FiyatListesiVeKampanya*).
- Kişisel veri (ad, TCKN, telefon, adres, cari unvanı, tutar/bakiye) içeren hiçbir alanın destek paketine konması — MÜHÜR 2: kişisel veri yok.
- Veritabanı yedeği / dump üretimi (T-3 yedek otomasyonu paketinin işi).
- Hata kimliğinin kullanıcıya e-posta/ticket olarak otomatik gönderilmesi.
- Sunucu günlüklerinin ham dosya olarak indirilmesi (sır sızma riski — Mehmet kapısı).

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| destek_paketleri | YENİ (yalnız Faz B). IKiraciVarlik; oluşturulan her maskeli paketin kaydı. Tek yönlü: PATCH/DELETE ucu AÇILMAZ (UX-S2 kapsamında değil ama log niteliğinde), DuzeltmePolitikasi haritasına SALT_OKUNUR + gerekçe anahtarı DENETIM olarak yazılır ve ekranda DegismezIpucu gösterilir. | id (ULID, 36), tenant_id (36), kullanici_id (36), gerekce (500, ≥20 krktr), jeton (64, tek kullanımlık), boyut_bayt (bigint), indirildi_mi (bool), surum_kunyesi (300), olusturma (datetime), indirme (datetime?) | (tenant_id, olusturma DESC); (tenant_id, jeton) UNIQUE | 20260916_DestekPaketi |
| — | Faz A (T-4a) için şema değişikliği YOKTUR. Uygulanmış migration bilgisi EF'in __EFMigrationsHistory tablosundan db.Database.GetAppliedMigrationsAsync() ile okunur; yeni tablo açılmaz. | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/meta/surum` | Kimliği doğrulanmış her rol (SAHIP, MUDUR, TEZGAHTAR) | Sürüm + derleme zamanı + ortam + uygulanmış migration sayısı/son ad. Kiracıya özel veri yok; hızlı ve önbelleklenebilir (60 sn bellek önbelleği). | Kapı yok; yalnız [Authorize] |
| GET | `/api/v1/meta/surum/ayrinti` | [RolGerekli("SAHIP")] | Uygulanmış migration listesi + bekleyen migration listesi + kapı durumları özeti. Bekleyen varsa 'uyari' alanı doldurulur. | DenetimServisi.OkumaKaydet (hassas değil) |
| POST | `/api/v1/destek/paket` | [RolGerekli("SAHIP")] | Faz B. Gerekçe (≥20 karakter) ister, maskeli paketi hazırlar, tek kullanımlık jeton döner. 400 gerekçe kısa; 429 oran sınırı (saatte 3). | DenetimServisi.YazmaKaydet (varlik_turu=destek_paketi); KiraciYazmaDenetcisi |
| POST | `/api/v1/destek/paket/{jeton}/indir` | [RolGerekli("SAHIP")] | Faz B. application/zip döner; jeton tek kullanımlıktır, ikinci çağrı 404/409. Dışa aktarım deseninin birebir kopyası (DisaAktarimModulu.cs:364). | DenetimServisi.OkumaKaydet (hassas=false, kişisel veri yok) |
| GET | `/api/v1/destek/paket/gecmis` | [RolGerekli("SAHIP")] | Faz B. Son 50 paket: tarih, kullanıcı, gerekçe, boyut, indirildi mi. Sınır UX-S29 gereği ekranda yazılır. | DenetimServisi.ListeOkumaKaydet |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/IstekKimligi.cs` — IstekKimligiAraci (middleware): NUlid ile kimlik üretir, HttpContext.Items["istek_kimligi"] ve ctx.TraceIdentifier'a yazar, X-Istek-Kimligi yanıt başlığını basar, ILogger.BeginScope ile günlüğe taşır. İkinci bir kimlik üretici açılmaz; okuma tek yardımcı ile (HttpContext.IstekKimligi()).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/ApiException.cs` — HataYakalayici'nin üç yazma dalına (satır 40-52, 57-70, 71-85) ortak bir Yaz(ctx, kod, mesaj, ad) yardımcısı; gövdeye istekKimligi alanı eklenir. Mevcut message/error/statusCode alan adları KORUNUR (Nest sözleşmesi + var olan testler).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs` — app.UseMiddleware<IstekKimligiAraci>() satırı UseForwardedHeaders'tan (Program.cs:260) SONRA, UseMiddleware<HataYakalayici>()'dan (satır 301) ÖNCE eklenir; böylece hata yakalayıcı kimliği hazır bulur ve oran sınırı reddi de kimlikli olur.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/SurumModulu.cs` — SurumServisi + SurumController tek dosya (modül kalıbı). Sürüm: Assembly.GetEntryAssembly() InformationalVersion; derleme zamanı: gömülü BuildTimestamp özniteliği (csproj'dan); migration: db.Database.GetAppliedMigrations/GetPendingMigrations. 60 sn IMemoryCache.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj` — Version/InformationalVersion + derleme damgası: <SourceRevisionId> yerine yayın betiğinden geçirilen -p:InformationalVersion ve gömülü AssemblyMetadata("DerlemeZamani"). Yayın hattı T-1+F-6'nın publish adımına tek satır ekler, ikinci yayın yolu açılmaz.
- **YENI (Faz B)** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Destek/DestekModulu.cs` — DestekPaketiServisi: maskeleme kuralları + zip üretimi ICSharpCode.SharpZipLib ile (DisaAktarimModulu.cs:4 ile aynı kütüphane, aynı jeton/indirme deseni). Maskeleme allow-list'tir: yalnız izin verilen alanlar yazılır, kara liste yoktur.
- **DEGISTIR (Faz B)** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` — destek_paketi girdisi SALT_OKUNUR + GerekceAnahtarlari.Denetim ile haritaya eklenir (HedefUc null). Eklenmezse DuzeltmePolitikasiTestleri kırmızıdır (UX-S1).
- **DEGISTIR (Faz B)** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — DbSet<DestekPaketi> + HasQueryFilter (kiracı) + (tenant_id, olusturma) ve (tenant_id, jeton) indeksleri. denetim.sh BEKLENEN_VARLIK sayacı 15'ten 16'ya çıkar (dotnet/denetim.sh:66).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs` — ApiHata'ya IstekKimligi alanı; HataFirlat (satır 129-142) yanıt başlığından X-Istek-Kimligi'ni ve gövdedeki istekKimligi'ni okur. Mevcut kurucu imzası korunur (opsiyonel parametre) — çağıran hiçbir yer kırılmaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/HataBandi.razor` — Yeni opsiyonel Kimlik parametresi: doluysa 'Destek kodu: <kimlik>' rozeti + kopyala düğmesi. Mesaj olduğu gibi gösterilmeye devam eder (UX-S9). Mevcut çağrı yerleri parametresiz çalışır.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Servisler/SurumServisi.cs` — /api/v1/meta/surum'u bir kez çeker, bellekte tutar; Ayarlar kartı ve /destek sayfası aynı kaynaktan okur (iki kopya künye doğmaz).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — TR+EN anahtarlar: Surum, Derleme, MigrationSayisi, BekleyenMigration, DestekKodu, Kopyala, DestekPaketi, PaketOlustur, PaketGecmisi, MaskeliUyari, DegismezDestekPaketi (UX-S12, UX-S31).

## Ekranlar
- `/ayarlar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — DEGISTIR: 'Sistem künyesi' kartı — sürüm, derleme zamanı, ortam, migration sayısı/son ad; #ayar-kunye-kopyala düğmesi tek satır künyeyi panoya yazar. Basit modda tek satır özet, pro modda ProAlan içinde migration ayrıntısı (UX-S5). Kart '(bugün itibarıyla — dönemden bağımsız)' değil, sürüm bilgisi olduğu için UX-S33 dışıdır. (menü/yetki: Mevcut sayfa; menüde değişiklik yok (KenarCubugu.razor:131 /ayarlar bağlantısı). Ayrıntı satırı yalnız Oturum.Kullanici.Rol == SAHIP iken gösterilir ve sunucu kapısı da SAHIP'tir.)
- `/destek` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Destek.razor` — YENI (Faz B): künye kartı + 'Maskeli destek paketi oluştur' (GerekceAlani ≥20, UX-S10) + onay modalı (UX-S27) + geçmiş tablosu (BosDurum UX-S8, sınır metni UX-S29) + DegismezIpucu 'paket kaydı silinmez' (UX-S25). İçerik listesi ekranda yazılıdır: ne konur, ne konmaz. (menü/yetki: KenarCubugu.razor'da RolluGorunur("/destek", "SAHIP") — /isletme ve /denetim-izi kalıbıyla aynı (satır 104,112). RotaBasliklari.cs'e '/destek' satırı eklenir yoksa RotaBaslikTestleri kırmızıdır (UX-S13).)
- `(tüm sayfalar)` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/HataBandi.razor` — DEGISTIR: hata kimliği rozeti + #hata-kimlik-kopyala. Kopyalama JS interop gerektirir; CSP script-src 'self' olduğu için kod wwwroot/js altındaki dosyada durur, satır içi script yazılmaz (Program.cs:283). Interop yoksa kimlik salt okunur input olarak seçilebilir kalır — düğme sessizce ölmez. (menü/yetki: Yetki yok; her rolde görünür (kimlik kişisel veri değildir).)

## İş kuralları (fail-closed)
- Hata kimliği HER yanıtta üretilir, yalnız hata yanıtında gövdeye yazılır; istemciden gelen X-Istek-Kimligi başlığı GÜVENİLMEZ — okunmaz, her zaman sunucu üretir (log zehirlenmesi).
- Kimlik biçimi ULID (26 karakter, NUlid zaten bağımlı, csproj:14); tahmin edilebilir sayaç kullanılmaz, kiracı/kullanıcı kimliği kimliğe gömülmez.
- Fail-closed: /api/v1/meta/surum sürümü okuyamazsa 'bilinmiyor' dönmez — 500 döner ve günlüğe yazar (Anayasa md.1, sessiz varsayım yok).
- Bekleyen migration varsa /surum/ayrinti 'uyari' alanını doldurur ve ekran kırmızı rozet basar; sessiz geçilmez.
- Sürüm ucu hiçbir sır, bağlantı dizesi, dosya yolu, makine adı veya IP döndürmez; ortam yalnız 'Production'/'Development' dizgesidir.
- Destek paketi ALLOW-LIST ile üretilir: yazılacak alanlar kodda tek tek sayılır; yeni tablo eklenince pakete kendiliğinden girmez.
- Destek paketinde tutar, bakiye, has gram, cari unvanı, TCKN, telefon, adres, e-posta, kullanıcı adı YOKTUR; kullanıcı yalnız rol + kısaltılmış kimlik olarak görünür (MÜHÜR 2: kişisel veri yok).
- Paket üretimi gerekçesiz yapılamaz (≥20 karakter) ve DenetimServisi.YazmaKaydet çağrılmadan dosya üretilmez; denetim yazımı başarısızsa işlem iptal edilir.
- İndirme jetonu tek kullanımlıktır ve 15 dakikada zaman aşımına uğrar; süresi geçmiş jeton 404 döner (DisaAktarim jeton deseniyle aynı).
- Paket boyutu 5 MB'ı aşarsa denetim özeti kırpılır ve pakete 'kırpıldı' notu konur — sessiz kırpma yasak (UX-S29).
- Destek paketi kaydı silinemez/düzenlenemez; DuzeltmePolitikasi'nda SALT_OKUNUR ve ekranda DegismezIpucu ile gerekçesi yazılıdır (DoD).
- Her düğme ya istek atar ya disabled'dır ve sebebi yazar: gerekçe 12/20 iken 'Oluştur' kilitli ve sayaç görünür (UX-S30).

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/IstekKimligiTestleri.cs` [HTTP (WebApplicationFactory, HttpOrtami.cs)]
  - Basarili_istek_X_Istek_Kimligi_basligi_dondurur
  - Bulunamayan_kayit_404_govdesinde_istekKimligi_alani_var
  - Govde_siniri_413_yanitinda_da_kimlik_var
  - Istemcinin_gonderdigi_X_Istek_Kimligi_yok_sayilir_sunucu_kendi_uretir
  - Iki_ayri_istek_iki_ayri_kimlik_uretir
  - Basliktaki_kimlik_ile_govdedeki_kimlik_ayni
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/SurumUcuTestleri.cs` [HTTP + DB (gerçek MySQL)]
  - Surum_ucu_200_doner_ve_migration_sayisi_EFMigrationsHistory_ile_ayni
  - Surum_ucu_sir_veya_baglanti_dizesi_icermez
  - Ayrinti_ucu_TEZGAHTAR_icin_403
  - Ayrinti_ucu_SAHIP_icin_uygulanmis_ve_bekleyen_listeleri_doner
  - Kimlik_dogrulanmamis_istek_401
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DestekPaketiTestleri.cs` [DB + HTTP (Faz B)]
  - Gerekce_20_karakterden_kisa_ise_400
  - Paket_olusturulunca_denetim_kaydi_dusuyor_varlik_turu_destek_paketi
  - Zip_icerigi_TCKN_telefon_unvan_tutar_dizgelerini_icermez
  - Baska_kiracinin_jetonu_ile_indirme_404
  - Jeton_ikinci_kez_kullanilamaz
  - MUDUR_rolu_403
  - Paket_kaydi_icin_PATCH_ve_DELETE_ucu_yok_405_veya_404
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/SurumEkranTestleri.cs` [bUnit]
  - Ayarlar_kunye_karti_surum_ve_migration_sayisini_basar
  - Bekleyen_migration_varsa_uyari_rozeti_gorunur
  - Kunye_kopyala_dugmesi_id_ile_bulunur_ve_etkin
  - TEZGAHTAR_rolunde_migration_ayrinti_satiri_gorunmez
  - HataBandi_kimlik_verilince_destek_kodu_rozetini_gosterir
  - HataBandi_kimlik_yokken_rozet_hic_cizilmez
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DestekEkranTestleri.cs` [bUnit (Faz B)]
  - Gerekce_kisa_iken_Olustur_dugmesi_disabled_ve_sebep_yazili
  - Gerekce_20_karakteri_gecince_dugme_acilir
  - Gecmis_bos_iken_BosDurum_mesaji_cikar
  - Sunucu_hatasinda_liste_bosaltilmaz_hata_bandi_cikar_UX_S28
  - DegismezIpucu_paket_kaydi_icin_gorunur
  - Rota_basligi_RotaBasliklari_haritasinda_var
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MaskelemeTestleri.cs` [saf birim (Faz B)]
  - Allow_list_disindaki_alan_pakete_yazilmaz
  - Kullanici_kimligi_kisaltilarak_yazilir
  - 5_MB_asilinca_kirpildi_notu_eklenir_ve_sessiz_kirpma_olmaz
  - Denetim_ozetinde_detay_alani_maskelenir

## Mutasyon kanıtları
- IstekKimligiAraci'nde başlık yazma satırı silinir → Basarili_istek_X_Istek_Kimligi_basligi_dondurur KIRMIZI.
- HataYakalayici 500 dalından istekKimligi alanı çıkarılır → Bulunamayan_kayit_404_govdesinde_istekKimligi_alani_var yeşil kalır, o yüzden 500 için ayrı bir test şarttır: Beklenmeyen_hata_500_govdesinde_kimlik_var KIRMIZI (izole mutasyon, §A10 tuzağı).
- Ara katman sunucu üretimi yerine istemci başlığını kabul eder → Istemcinin_gonderdigi_X_Istek_Kimligi_yok_sayilir_sunucu_kendi_uretir KIRMIZI.
- SurumController'dan [RolGerekli("SAHIP")] kaldırılır → Ayrinti_ucu_TEZGAHTAR_icin_403 KIRMIZI.
- Migration sayısı sabit (22) döndürülür → Surum_ucu_migration_sayisi_EFMigrationsHistory_ile_ayni KIRMIZI (belge kayması bu hatla yakalanır).
- Maskeleme allow-list'i kara listeye çevrilip cari unvanı eklenir → Zip_icerigi_TCKN_telefon_unvan_tutar_dizgelerini_icermez KIRMIZI.
- Jeton tek kullanımlık işareti kaldırılır → Jeton_ikinci_kez_kullanilamaz KIRMIZI.
- HasQueryFilter yerine elle Where(tenant) yazılır ve kiracı parametresi sabitlenir → Baska_kiracinin_jetonu_ile_indirme_404 KIRMIZI.
- Destek.razor'da disabled="@(false)" ile kilit kaldırılır (dizge 'false' tuzağına düşmeden) → Gerekce_kisa_iken_Olustur_dugmesi_disabled_ve_sebep_yazili KIRMIZI.
- DuzeltmePolitikasi'ndan destek_paketi girdisi silinir → DuzeltmePolitikasiTestleri KIRMIZI (UX-S1 zorlaması).

## Canlı doğrulama
- curl -i http://localhost:5198/api/v1/meta/surum -H 'Authorization: Bearer <jeton>' → 200 + 'X-Istek-Kimligi:' başlığı dolu; gövdede migrationSayisi.
- curl -i http://localhost:5198/api/v1/cari/YOK-123 -H 'Authorization: ...' → 404 ve gövdede istekKimligi; aynı değer yanıt başlığında.
- curl -i http://localhost:5198/api/v1/meta/surum/ayrinti (TEZGAHTAR jetonu) → 403; SAHIP jetonu → 200.
- SQL: SELECT COUNT(*) FROM __EFMigrationsHistory; → ucun dondurdugu migrationSayisi ile birebir esit olmali (esit degilse Madde 1 ile raporla).
- curl -X POST .../api/v1/destek/paket -d '{"gerekce":"kisa"}' → 400; ≥20 karakterle → 200 + jeton.
- curl -X POST .../api/v1/destek/paket/<jeton>/indir -o /tmp/paket.zip -w '%{http_code}' → 200; ikinci çağrı → 404.
- unzip -p /tmp/paket.zip '*' | grep -Ei '[0-9]{11}|tckn|telefon|iban' → çıktı BOŞ olmalı.
- SQL: SELECT eylem, varlik_turu, LENGTH(detay) FROM denetim_kayitlari WHERE varlik_turu='destek_paketi' ORDER BY tarih DESC LIMIT 3; → EKLEME + OKUMA satırları.
- SQL: SELECT COUNT(*) FROM destek_paketleri WHERE tenant_id='<test-kiraci>'; doğrulama sonunda test kiracısı silinir.
- Tarayıcıda /ayarlar → künye kartı; kasıtlı 500 üretilip HataBandi'nda destek kodu rozeti görülür ve panoya kopyalanır (CSP ihlali konsolda YOK).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/IstekKimligi.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/ApiException.cs (DEGISTIR, 33-85)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs (DEGISTIR, 260-301 arası + DI kaydı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/SurumModulu.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Destek/DestekModulu.cs (YENI, Faz B)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs (DEGISTIR, Faz B: DestekPaketi)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DEGISTIR, Faz B)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj (DEGISTIR: sürüm + derleme damgası)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs (DEGISTIR, 10-13 ve 129-142)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Destek.razor (YENI, Faz B) + Pages/Ayarlar.razor (DEGISTIR) + Layout/KenarCubugu.razor, Layout/RotaBasliklari.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/HataBandi.razor + wwwroot/js/pano.js (YENI, CSP uyumlu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/{IstekKimligiTestleri,SurumUcuTestleri,SurumEkranTestleri,DestekPaketiTestleri,DestekEkranTestleri,MaskelemeTestleri}.cs (YENI) + dotnet/denetim.sh (DEGISTIR: BEKLENEN_VARLIK 15→16, satır 66)`

## Fazlar
- **Faz A — T-4a: korelasyon + sürüm/derleme/migration damgası** (Dalga 3 (mühür 2 sırası: T-3'ten sonra, B@3 yenilemesinden önce)): SEVK EDİLEBİLİR ASGARİ. Şema değişikliği YOK, migration YOK. IstekKimligiAraci + HataYakalayici kimlik alanı + /api/v1/meta/surum(+/ayrinti) + Ayarlar künye kartı + HataBandi destek kodu rozeti + ApiIstemci taşıması. Tek başına destek çağrısını çözer: 'hangi sürüm, hangi istek'.
- **Faz B — T-4b: maskeli destek paketi** (Dalga 6 (S-1 → 37 → 36 → T-4b sırası)): destek_paketleri tablosu + migration, DestekModulu (allow-list maskeleme, zip, tek kullanımlık jeton), /destek ekranı, DuzeltmePolitikasi SALT_OKUNUR girdisi, denetim.sh varlık sayacı güncellemesi. Faz A künyesi paketin ilk dosyasıdır.
## Mevzuat dayanağı
- 6698 sayılı KVKK md.4 (veri minimizasyonu) ve md.12 (veri güvenliği) — **YÜKSEK (ilke düzeyinde) / uygulama ayrıntısı DOĞRULANMALI** — Destek paketinin kişisel veri taşımaması bu maddelerin doğrudan gereğidir; allow-list maskeleme ve 'kişisel veri yok' kuralı mühür 2 ile örtüşür. Paketin müşteri dışına çıkması hâlinde aktarım hukuki dayanağı gerekir — bu yüzden paket yalnız SAHIP tarafından indirilir, otomatik gönderim yoktur.
- VUK 219/mükerrer 242 ve elektronik defter/kayıt saklama yükümlülükleri — **ORTA — DOĞRULANMALI** — Destek paketi mali kayıt değildir, defter yerine geçmez; bu nedenle saklama süresi yükümlülüğü doğurmaz. Yine de paketin denetim kaydı append-only izde kalır (DenetimServisi), böylece 'kim ne zaman sistem künyesi dışa aldı' sorusu cevaplanabilir.
- OWASP ASVS 5.0.0 V7 (Logging & Error Handling) — V7.1 hata kimliği, V7.3 hassas veri sızdırmama — **ORTA-YÜKSEK** — Kullanıcıya kimlik verip ayrıntıyı sunucuda tutmak ASVS'in önerdiği kalıptır. Projede ASVS öz-değerlendirmesi zaten yapılmış (Program.cs:264 yorumu), bu paket o hattın devamıdır. Madde numaraları sürüm bazında DOĞRULANMALI.

## Riskler
- Kimlik gövdeye eklenince mevcut hata testleri tam JSON eşitliği yapıyorsa kırılır — DOĞRULANMALI: Kuyumcu.Tests içinde hata gövdesini birebir karşılaştıran testler paket başında taranmalı (HttpBoruHattiTestleri.cs, GovdeSiniriTestleri.cs ilk adaylar).
- Panoya kopyalama JS interop ister; CSP script-src 'self' (Program.cs:283) satır içi script'i keser. Kod ayrı .js dosyasında olmalı; olmazsa düğme sessizce ölür (DoD ihlali) — yedek yol: salt okunur seçilebilir alan.
- Sürüm damgası yayın hattından gelmezse geliştirme derlemesinde '1.0.0' sabiti görünür ve yanlış güven verir; T-1 önkoşulu bu yüzden bağlayıcıdır.
- Migration dizinindeki 9 adet 0-2 baytlık dosya (…/Data/Migrations/20260915132645_FiyatListesiVeKampanya.cs vb.) sayımı ve 'son migration' adını yanıltabilir; sayım __EFMigrationsHistory'den okunmalı, dosya sayımından DEĞİL.
- Maskeleme kara listeye kayarsa yeni bir tablo eklendiğinde kişisel veri sessizce pakete girer — allow-list şart, MaskelemeTestleri bunu zorlar.
- Destek paketi ucu kaynak yoğun olabilir (zip + denetim sorgusu); oran sınırı konmazsa kötüye kullanım riski — mevcut UseRateLimiter (Program.cs:323) politikasına yeni kova eklenir.
- denetim.sh BEKLENEN_VARLIK sayacı (dotnet/denetim.sh:66) Faz B'de güncellenmezse denetim BAŞARISIZ verir ve kapı yanlış yere kapanır.
- Hata kimliğinin günlüğe düşmesi için ILogger kapsamı kullanılmalı; yalnız gövdeye yazılırsa destek kimliği alır ama sunucuda arayamaz — paketin asıl faydası kaybolur.

## Açık sorular
- Sunucu günlükleri bugün nereye yazılıyor (yalnız konsol mu, dosya mı)? Kimliğin aranabilir olması için kalıcı günlük gerekir — deploy/ ve docker-compose.yml içindeki logging yapılandırması DOĞRULANMALI; yoksa T-1 kapsamına bir satır eklenmeli.
- Destek paketi kaç gün saklanacak ve dosya diskte mi tutulacak yoksa her indirmede yeniden mi üretilecek? Öneri: diskte tutulmaz, jeton anında üretir — Kurucu/Tamer Bey onayı.
- /api/v1/meta/surum kimlik doğrulaması olmadan da açılsın mı (yük dengeleyici/izleme için)? Öneri: HAYIR — /saglik bu işi görür.
- Sürüm numarası kim artıracak (elle csproj mu, yayın betiği mi)? T-1 ile tek elden kararlaştırılmalı.
- Destek paketine kiracı kimliği tam mı, kısaltılmış mı yazılacak — destek tarafında eşleştirme gereksinimi nedir?

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — Tasarım mimari olarak sağlam: Faz A şemasız ve sevk edilebilir asgari, Faz B tek yeni tablo + tek migration ile sınırlı; mevcut mekanizmalar (SharpZipLib jeton/indirme deseni, RolGerekli, DenetimServisi, HasQueryFilter, GerekceAlani/BosDurum/DegismezIpucu/HataBandi, IMemoryCache, UseRateLimiter) yeniden kullanılıyor, yeni bileşen/ikinci kimlik üretici/ikinci yayın yolu açılmıyor. Paket boyutu 1,5-2,5 gün ve iki faza bölünmüş; dalga 3 / dalga 6 ayrımı doğru. Koşullu olmasının sebebi dört kanıtlı belge sapması (denetim.sh -ge semantiği, 500 dalında error alanının olmaması, 0-bayt dosya sayısı, DisaAktarim parola farkı) ve beş açık sorunun kod öncesi kapatılması gereğidir.
  - Koşullar: denetim.sh karşılaştırması `-ge` (dotnet/denetim.sh:66-69): sayaç 15'te kalsa da 16 varlıkla TEMİZ verir. Belgedeki 'güncellenmezse denetim BAŞARISIZ' ifadesi düzeltilir; sayaç yine 16'ya çekilir, dalga 6'da aynı satıra dokunan paketlerle tek elden sıralanır. · 500 dalı gövdesinde `error` alanı YOK; yalnız message+statusCode var (ApiException.cs:71-85). Ortak Yaz() yardımcısı 500'e `error` EKLEMEZ (sözleşme kayar), sadece istekKimligi eklenir. 413/ApiException dallarının alan kümesi aynen korunur. · Migration sayımı yalnız __EFMigrationsHistory + assembly'den okunur, dosya sayımından asla. Belgedeki '9 adet 0-2 baytlık' yanlış: 8 adet 0-bayt FiyatListesiVeKampanya dosyası var; ayrıca 20260915115025_VadeTakibi.Designer.cs 336 bayt (ayrı kırıklık, TB-M1'e not). · CSP gerçek dizge "script-src 'self' 'wasm-unsafe-eval'" (Program.cs:281-283). Kopyalama kodu wwwroot/js/pano.js'te durur, 'unsafe-inline' eklenmez; interop yoksa salt okunur seçilebilir alan yedeği zorunlu, düğme sessizce ölmez. · İndirme ucu DisaAktarim'ın 'birebir kopyası' değildir: DisaAktarim gövdede parola ister ve şifreli zip üretir (DisaAktarimModulu.cs:364-372). Parolasız tasarım belgeye gerekçesiyle yazılır; jeton tüketimi + kiracı kapanı deseni aynen kopyalanır. · Oran sınırı yeni kovası mevcut OranSiniri sabitleri ve UseRateLimiter (Program.cs:323) üzerinden eklenir; ikinci limiter yolu açılmaz. IstekKimligiAraci tek yere kaydedilir: UseForwardedHeaders (Program.cs:260) sonrası, UseMiddleware<HataYakalayici> (Program.cs:302) öncesi. · Açık sorular Faz A kodu yazılmadan kapatılır: /meta/surum [Authorize] kalır (anonim açılmaz, /saglik Program.cs:314 bu işi görür); destek paketi diske YAZILMAZ, jetonla bellekte üretilir (T-3 yedek dizini ile karışmaz); sürüm numarasını yalnız T-1 publish artırır. · Faz A hata gövdesine alan eklediği için tam-JSON eşitliği yapan mevcut testler paket başında taranır (HttpBoruHattiTestleri, GovdeSiniriTestleri); kırılan gizlenmez, aynı pakette düzeltilir. Sozlukler.cs ve ApiIstemci.cs dalga içinde tek elden yazılır.
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: KOSULLU** — Test seti HTTP (WebApplicationFactory)/gerçek MySQL DB/bUnit dengesini doğru kurmuş (IstekKimligiTestleri:HTTP, SurumUcuTestleri:HTTP+DB, SurumEkranTestleri/DestekEkranTestleri:bUnit, MaskelemeTestleri:saf birim), mutasyon kanıtları çoğunlukla test adıyla isabetli (satır 124-134) ve canlı doğrulama HTTP kodu + SQL karşılaştırmasıyla gerçekten ölçülebilir (satır 136-145); test kiracısı temizliği açıkça yazılı (satır 144). Ancak mutasyon kanıtı-test adı uyuşmazlığı fail-closed kapıyı zayıflatır, 429 ve büyük-paket senaryoları kanıtsız kalmış, kiracı kimliği maskeleme kuralı iç çelişkili — bunlar giderilmeden Faz A/B kodu 'sınandı' sayılmamalı.
  - Koşullar: Mutasyon kanıtı satır 128'deki test adı 'Surum_ucu_migration_sayisi_EFMigrationsHistory_ile_ayni' ile Testler bölümündeki gerçek ad 'Surum_ucu_200_doner_ve_migration_sayisi_EFMigrationsHistory_ile_ayni' (satır 90) uyuşmuyor; kanıt bağlayıcı olsun diye ad eşitlensin. · POST /api/v1/destek/paket için 429 oran sınırı (saatte 3) iş kuralı var (Uçlar tablosu satır 44) ama DestekPaketiTestleri listesinde (satır 96-102) 429 senaryosu yok; en az bir test eklensin. · Kiracı kimliğinin pakette tam mı kısaltılmış mı yazılacağı Kapsam'da kesin ('kısaltılmış', satır 21) ama Açık sorular'da (satır 184) hâlâ soru olarak duruyor; çelişki giderilip tek karar MaskelemeTestleri'ne yansıtılsın. · Faz B'de rate limiter'a yeni kova eklenmesi (Riskler, satır 175, Program.cs) Dosya ayak izi listesinde (satır 147-159) Faz B değişikliği olarak görünmüyor; eklensin, yoksa çakışma planı eksik kalır. · 5 MB kırpma kuralı (satır 77, 120) yalnız MaskelemeTestleri birim testiyle kanıtlanıyor; Canlı doğrulama listesinde (135-145) büyük paket üretip gerçek zip boyutu/kırpma notunu doğrulayan bir HTTP adımı yok, eklensin. · /api/v1/meta/surum 60 sn IMemoryCache (satır 52) ile Surum_ucu_...migration_sayisi testi (satır 90) aynı süreçte migration uygulanırsa çakışabilir; cache-migration etkileşimi test notunda netleştirilsin.
- **YENİ: Güvenlik merceğinden (Mehmet) üç tasarımı inceledim: kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek/TLS, içe aktarım riski ve MFA koşulu açısından. Program.cs:283 canlı kodda script-src 'self' 'wasm-unsafe-eval' olarak doğrulandı (unsafe-inline yok) — PWA-1'in CSP taahhüdünün mevcut temeli sağlam. docker-compose.yml:7'de MySQL root parolası düz metin olarak canlıda doğrulandı; hiçbir pakette bu ele alınmıyor (T-1/F-6/F-7'ye bırakılmış), ayrı takip gerekir.: KOSULLU** — Tasarım fail-closed ilkeleri (sürüm ucu 500 döner, sessiz varsayım yok — satır 70), rol/yetki ayrımı (SAHIP-only ayrıntı/destek uçları — satır 42-46, 403 testleri satır 92,101), kiracı izolasyonu (HasQueryFilter + tenant_id indeks — satır 56, mutasyon kanıtı satır 131) ve kişisel veri dışlama (allow-list maskeleme, mutasyon kanıtlı — satır 73-74,129) sağlam kurgulanmış. Ancak destek paketinin diskte kalıcılığı, log kalıcılığı ve 'ayar özeti' alan sınırı gibi üç açık soru (satır 179-182) çözülmeden Faz A/B sevk edilirse güvenlik varsayımı doğrulanmamış olur (Anayasa Md.1 ihlali riski).
  - Koşullar: Açık soru §179 satır 181 bağlayıcı koşula çevrilsin: destek paketi DİSKTE TUTULMAZ, her indirmede bellekte üretilip response sonrası imha edilir; aksi halde şifrelenmemiş PII-yakın zip disk üzerinde kalır (KVKK md.12). · MaskelemeTestleri.cs (satır 117-121) kapsamına 'Zip içeriği API anahtarı/bağlantı dizesi/JWT secret içermez' testi eklensin; mevcut testler yalnız TCKN/telefon/unvan/tutar kontrol ediyor, sır sızıntısı ayrı doğrulanmalı. · 'kapı/ayar özeti' (satır 21,54) içeriği kod düzeyinde somut alan listesiyle sınırlansın (yalnız bool kapı durumları); ortam değişkeni adı/değeri veya feature-flag değeri pakete girmesin. · POST /api/v1/destek/paket için tanımlı 429 oran sınırı (satır 20,44: saatte 3) test listesine eklenmemiş; Testler §82-121 bu senaryoyu kapsamıyor — DestekPaketiTestleri'ne 'Saatte_3_paketten_sonra_429' eklensin. · Açık soru §179-180 (kalıcı günlük var mı) Faz A sevkinden ÖNCE T-1 kapsamında netleşsin; kalıcı log yoksa korelasyon kimliğinin destek faydası fiilen sıfırdır (Riskler §177 zaten bunu doğruluyor). · Sürüm ucu 60 sn IMemoryCache (satır 52) çok kiracılı ortamda tek süreç genelinde paylaşılıyor mu netleştirilsin; migration sayısı kiracıdan bağımsız olsa da önbellek anahtarı kiracıya göre ayrışmıyorsa yanlış veri sızmaz ama tutarsızlık riskine karşı doğrulama testine bir satır eklensin.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Destek paketi DİSKE YAZILMAZ: jetonla bellekte üretilir, yanıt sonrası imha edilir (T-3 yedek dizini/saklama politikasıyla ikinci dosya hattı açılmaz).
- 500 dalına `error` alanı EKLENMEZ (ApiException.cs:71-85 yalnız message+statusCode); ortak Yaz() sadece istekKimligi ekler, 413/ApiException alan kümesi aynen korunur.
- Migration sayısı yalnız __EFMigrationsHistory + assembly'den okunur. Belgedeki '9 adet 0-bayt' düzeltilir: 8 adet 0-bayt + 336 baytlık VadeTakibi.Designer.cs. TB-M1 bağlayıcı değil, bilgilendirici önkoşuldur.
- denetim.sh:66-69 `-ge` semantiği belgeye doğru yazılır ('güncellenmezse BAŞARISIZ' ifadesi kaldırılır); BEKLENEN_VARLIK 16'ya çekilmesi dalga 6 sonunda tek elden yapılır (S-1/37/36 ile aynı satır).
- CSP'ye 'unsafe-inline' EKLENMEZ (script-src 'self' 'wasm-unsafe-eval'); kopyalama kodu wwwroot/js/pano.js'te durur, interop yoksa seçilebilir salt-okunur yedek zorunlu, düğme sessizce ölmez.
- İndirme ucu parolasızdır (DisaAktarim şifreli zip üretir); fark gerekçesiyle belgeye yazılır, yalnız jeton tüketimi + kiracı kapanı deseni kopyalanır.
- Oran sınırı mevcut OranSiniri sabitleri + UseRateLimiter üzerinden eklenir (ikinci limiter yolu yok); IstekKimligiAraci UseForwardedHeaders sonrası, HataYakalayici öncesi; Faz B limiter değişikliği dosya ayak izine eklenir.
- Test boşlukları kapatılır: POST /destek/paket 429 (saatte 3), 5 MB kırpma için gerçek zip boyutu canlı doğrulaması, 'zip sır (API anahtarı/bağlantı dizesi/JWT) içermez' testi, 60sn önbellek–migration etkileşim notu.
- Çelişkiler giderilir: mutasyon kanıtı test adı satır 90 ile eşitlenir; kiracı kimliği tek karar 'kısaltılmış' olarak MaskelemeTestleri'ne yansır; 'ayar özeti' yalnız bool kapı durumlarıyla sınırlıdır (ortam değişkeni adı/değeri, flag değeri girmez).
- /meta/surum [Authorize] kalır ve sır/bağlantı dizesi/IP/makine/yol döndürmez (ortam yalnız Production/Development); sürümü yalnız T-1 publish artırır; yaz-seri sıra T-1 → T-3 → T-4a; Sozlukler.cs, ApiIstemci.cs, HataBandi.razor tek elden yazılır.
