# Paket 5 — Belge / etiket şablon motoru

> Katman: Katman 0 — Altyapı · Tahmin: 5–7 gün (Faz A 2,5 · Faz B 1,5 · Faz C 1,5; BelgeYerlesimi bileşeni önkoşul olarak HAZIR kabul edilerek — hazır değilse +0,5 gün paket 42/UX-2 Faz C'ye yazılır) gün · Önkoşullar: 4, 42 · Kaynak: katman mimarı K0-5 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 5

## Hedef
Perakende kuyumcunun bugün elle yazdığı ya da hiç veremediği kâğıtları sistemden, kendi unvanı–adresi–VKN'si–logosuyla basılabilir hâle getirir. Bugün tek düzgün belge gider pusulasıdır; sayım tutanağı, kasa gün kapanış fişi ve satış fişi yoktur, ürün etiketinin yerleşimi sabittir ve hiçbir çıktıda dükkânın künyesi görünmez (unvan yerine yalnız kiracı adı basılır). Bu paket ortak bir belge kabuğu, kiracı başına künye/logo ayarı ve belge türü başına seçilebilir sabit yerleşim getirir; sonraki her yazdırılabilir belge (irsaliye, has defteri dökümü, raporlar) bunun üstüne biner. Kullanıcı-yazılı şablon dili YOKTUR: seçim listesi vardır.

## Kodda bugünkü durum
Yazdırılabilir yüzey sayısı ÜÇTÜR, bir değil: (1) /Users/…/dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor — gider pusulası, ADR-0007 deseninin referansı, kendi yorumunda 'BELGE YERLEŞİM STANDARDIDIR' diyor (PusulaBelgesi.razor:20); (2) Pages/GiderPusulalari.razor:14 — liste yazdırma; (3) Pages/UrunEtiketleri.razor:105-140 — 50×30 mm etiket ızgarası + Code128 SVG. Print CSS tek blokta: wwwroot/css/app.css:432-455 (`.kenar-cubugu/.ust-bar/.yazdirma-gizle` gizlenir, @page A4 15mm, etiket için @page 8mm, `.urun-etiket { page-break-inside: avoid }`). Belge sınıfları app.css:346-385 (`.belge-sayfa/-arac/-ust/-no/-dayanak/-taraflar/-tablo/-toplam/-yazi/-tarihler/-not/-imza`). ORTAK BİLEŞEN YOK: Bilesenler/ altında BelgeYerlesimi.razor bulunmuyor (dizin listesi: BelgeYukleyici, CubukGrafik, AlanGrafik, PastaGrafik, StatKart). İŞLETME KÜNYESİ YOK: Entities.cs:43-60 `Tenant` yalnız Ad/Plan/KurKaynagi/MaliyetYontemi/AiEtkin tutuyor — unvan, VKN, adres, telefon, logo kolonu YOK; `grep -rn 'Unvan|logo' Kuyumcu.Api` yalnız Cari.VergiNo'yu (Entities.cs:128) buluyor. Belgedeki düzenleyen bloğu bu yüzden `duzenleyen = new { unvan = tenant?.Ad, sube = sube?.Ad }` (HurdaModulu.cs:85) ile sınırlı. ŞABLON AYARI YOK: belge_sablon_ayarlari benzeri tablo yok. VERİSİ VAR BELGESİ YOK olan iki kalem: SayimOturumu/SayimSatiri (Data/SayimOturumu.cs:15-50; uçlar SayimModulu.cs:236-241, tutanak ucu YOK) ve GunKapanisi (Data/GunKapanisi.cs:19-57; uçlar KasaModulu.cs:364-379, fiş ucu YOK). Çok bacaklı satış fişi verisi var (SatisModulu.cs:1744 POST /islemler/satis-fisi, SatisFisiDto:230), yazdırılabilir çıktısı yok. Dosya deposu hazır: Data/UrunBelgesi.cs `Belge` sınıfı, BelgeSahipTuru.Isletme ve BelgeTuru.Gorsel kapalı listede TANIMLI (UrunBelgesi.cs:70-84), EnFazlaGorselBayt = 2 MB (satır 63); yükleme bileşeni Bilesenler/BelgeYukleyici.razor mevcut.

## Kapsam
**İçinde:**
- FAZ A — `isletme_kunyesi` tablosu (unvan, VKN/TCKN, vergi dairesi, adres, telefon, e-posta, web, ticaret sicil no, MERSİS, logo belge kimliği, belge alt notu) + GET/PUT ucu + /isletme ekranına 'Belge künyesi' kartı ve logo yükleme (paket 4 Faz B'nin ISLETME sahibini tüketir)
- FAZ A — `belge_sablon_ayarlari` tablosu: belge türü başına bir satır (yerleşim kodu, künye göster, logo göster, alt not, kopya sayısı, imza bloğu göster)
- FAZ A — `BelgeYerlesimleri` kapalı listesi (Kuyumcu.Core, saf sınıf): A4_RESMI · A4_SADE · TERMAL_80 · ETIKET_50X30. Her belge türü için İZİNLİ yerleşim alt kümesi tanımlı — fail-closed
- FAZ A — mevcut BelgeYerlesimi.razor bileşeninin künye/logo/filigran parametreleriyle GENİŞLETİLMESİ (kurulması değil — K0-1) + belge.css'e yerleşim sınıfları
- FAZ A — /ayarlar/belge-sablonlari ekranı: belge türü listesi, yerleşim seçimi, canlı önizleme (sahte veriyle), kaydet
- FAZ A — gider pusulası belgesinin künyeyi kullanması (HurdaModulu.cs:85 düzenleyen bloğu künyeyle genişletilir; razor tarafı yeni alanları basar, mevcut alanlar BİREBİR korunur)
- FAZ B — Sayım tutanağı: GET /sayim/{id}/tutanak + /sayim/{Id}/tutanak ekranı (A4_RESMI). Kapsam, sistem/sayılan/fark sütunları, sayan ve kapatan kullanıcı, imza bloğu; İPTAL oturumda 'İPTAL EDİLDİ' filigranı
- FAZ B — Kasa gün kapanış fişi: GET /kasalar/{id}/gun-kapanislari/{kapanisId}/fis + ekran (TERMAL_80 varsayılan, A4_SADE seçilebilir). Yerel gün, sayılan nakit, defter bakiyesi, fark, gerekçe, kapatan; gün yeniden açıldıysa 'YENİDEN AÇILDI' filigranı
- FAZ C — İşlem/satış fişi: GET /islemler/{id}/fis + ekran (TERMAL_80). Ödeme bacakları, has karşılığı, kur snapshot kimliği, iade ise 'İADE' ibaresi
- FAZ C — Ürün etiketinde yerleşim seçimi (ETIKET_50X30 varsayılan) + künye/logo satırı seçeneği + 'işçilik dahil/hariç' ibaresi seçeneği (varsayılan KAPALI, mevzuat DOGRULANMALI)
- FAZ C — Sozlukler.cs'e SozlukBelge (TR+EN) ve yeni rotaların RotaBasliklari.cs'e eklenmesi (her fazda kendi rotası eklenir; UX-S13 kuralı)

**Dışında:**
- Kullanıcı-yazılı şablon dili / sürükle-bırak tasarımcı / serbest CSS (K0-2)
- Sunucuda PDF üretimi, toplu gece basımı, görüntü ölçekleme (ADR-0007 + PAKET-04)
- Sevk irsaliyesi belgesi — paket 42'nin kendi kapsamı; bu motoru TÜKETİR
- Fatura çıktısının satır kalemleriyle basılması — paket 1 (TB-17) fatura satır kalemi modelini vermeden yapılamaz; yalnız yerleşim kodu bugünden ayrılır
- Vitrin fiyat listesi / pano çıktısı — paket 49 ve pano işi
- Şube bazlı ayrı künye ve şube bazlı şablon geçersiz kılma — paket 46 (şube görünürlüğü) ile birlikte kararlaştırılmalı
- Termal yazıcıya doğrudan sürücü/ESC-POS gönderimi — B sınıfı (donanım); tarayıcı yazdırma kutusundan 80 mm rulo seçilir
- Belge numarası üretimi (irsaliye/fatura no) — bu paket numara DOĞURMAZ, var olanı basar
- e-Fatura/e-Arşiv XML görselleştirmesi (ADR-0012)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| isletme_kunyesi | YENİ. IKiraciVarlik. Kiracı başına TEK satır (tenant_id UNIQUE). Satır yoksa belge Tenant.Ad'a düşer (geri uyum, K0-4). Down: tablo düşer, veri kaybı bilinçlidir ve Down yorumunda yazılır. | id varchar(36) PK · tenant_id varchar(36) NOT NULL · unvan varchar(200) NOT NULL · vergi_kimlik varchar(20) NULL · vergi_dairesi varchar(100) NULL · adres varchar(500) NULL · telefon varchar(30) NULL · eposta varchar(200) NULL · web varchar(200) NULL · ticaret_sicil_no varchar(50) NULL · mersis_no varchar(20) NULL · logo_belge_id varchar(36) NULL · belge_alt_notu varchar(500) NULL · olusturuldu datetime · guncellendi datetime | UNIQUE (tenant_id) | YYYYMMDDHHMMSS_IsletmeKunyesiVeBelgeSablonu |
| belge_sablon_ayarlari | YENİ. IKiraciVarlik. Belge türü başına en fazla bir satır. Satır yoksa tür için KOD İÇİ VARSAYILAN yerleşim kullanılır — belge her hâlükârda basılır (fail-safe; fail-closed olan yerleşim KODUNUN doğrulanmasıdır, satırın varlığı değil). | id varchar(36) PK · tenant_id varchar(36) NOT NULL · belge_turu varchar(30) NOT NULL · yerlesim_kodu varchar(20) NOT NULL · kunye_goster bool NOT NULL default 1 · logo_goster bool NOT NULL default 1 · imza_goster bool NOT NULL default 1 · alt_not varchar(300) NULL · kopya_sayisi int NOT NULL default 1 (1..3) · guncelleyen_kullanici_id varchar(36) · olusturuldu datetime · guncellendi datetime | UNIQUE (tenant_id, belge_turu) | YYYYMMDDHHMMSS_IsletmeKunyesiVeBelgeSablonu |
| belgeler | DEĞİŞİKLİK YOK. Logo, paket 4'ün kapalı listesindeki sahip_turu='ISLETME' + tur='GORSEL' satırı olarak saklanır (UrunBelgesi.cs:73 ve :81'de sabitler ZATEN tanımlı). isletme_kunyesi.logo_belge_id yalnız o satıra işaret eder; FK yoktur (ADR-0009). | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/isletme/kunye` | SAHIP, MUDUR | Künye kaydı. Satır yoksa 200 + { unvan = Tenant.Ad, kalan alanlar null, varsayilan = true } döner (404 DEĞİL — ekran boş formu açabilmeli). | [Authorize] + [RolGerekli("SAHIP","MUDUR")] TAM eşleşme |
| PUT | `/api/v1/isletme/kunye` | SAHIP, MUDUR | Künyeyi oluşturur/günceller (upsert). logo_belge_id verilirse belgenin AYNI kiracıda, sahip_turu='ISLETME', tur='GORSEL' ve aktif olduğu doğrulanır. | [Authorize] + [RolGerekli]; unvan boşsa 400; VKN 10 ya da 11 hane değilse 400 (algoritma doğrulaması YOK — bkz. açık sorular); logo kiracıda yoksa/yanlış türdeyse 404/400; SaveChanges üzerinden KiraciYazmaDenetcisi + denetim izi |
| GET | `/api/v1/belge-sablonlari` | giriş yapmış tüm roller | Tüm belge türleri + kiracının ayarı + o tür için izinli yerleşim kodları listesi. Ayarı olmayan tür varsayılanıyla döner. | [Authorize] |
| PUT | `/api/v1/belge-sablonlari/{belgeTuru}` | SAHIP, MUDUR | Tek türün ayarını yazar (upsert). | [Authorize] + [RolGerekli]; belgeTuru kapalı listede değilse 400; yerlesim_kodu o tür için izinli alt kümede değilse 400; kopya_sayisi 1..3 dışıysa 400; denetim izi yazılır |
| GET | `/api/v1/sayim/{id}/tutanak` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | FAZ B. Yazdırılabilir sayım tutanağı verisi: künye, oturum başlığı/kapsamı, satırlar (ürün, sistem, sayılan, fark), toplamlar, açan/kapatan, durum. SAYILMAMIŞ satır (SayilanAdet null, SayimOturumu.cs:45) 'sayılmadı' olarak işaretlenir, SIFIR sayılmaz. | [Authorize] + [ModulYetkisi("sayim")]; kiracı dışı 404; okuma izi düşer; satır sayısı sunucuda sayfalanır (TB-19: sınırsız ToListAsync yok — azami 2000 satır, aşarsa 400 + 'kapsamı daraltın') |
| GET | `/api/v1/kasalar/{id}/gun-kapanislari/{kapanisId}/fis` | SAHIP, MUDUR, MUHASEBE | FAZ B. Gün kapanış fişi verisi: künye, kasa adı/para birimi, yerel gün, sayılan nakit, defter bakiye, fark, gerekçe, kapatan; GunKapanisi.Kilitli false ise yeniden açılma bilgisi (acan, zaman, gerekçe). | [Authorize] + [RolGerekli]; kapanış bu kasaya ve kiracıya ait değilse 404; okuma izi |
| GET | `/api/v1/islemler/{id}/fis` | giriş yapmış tüm roller | FAZ C. İşlem fişi verisi: künye, işlem türü, kalemler, ödeme bacakları, has karşılığı, kur_snapshot_id, cari (ad + maskeli kimlik), iade ise ters ibare. | [Authorize] + [ModulYetkisi("islem")]; kimlik alanları ADR-0008 maskelemesiyle döner; okuma izi |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/BelgeSablonu.cs` — `IsletmeKunyesi` ve `BelgeSablonAyari` varlıkları (ikisi de IKiraciVarlik). Aynı dosyada `BelgeSablonTuru` kapalı listesi: GIDER_PUSULASI · SAYIM_TUTANAGI · KASA_KAPANIS_FISI · ISLEM_FISI · ETIKET_URUN · SEVK_IRSALIYESI (paket 42 için yer ayrılır, ekranı o pakette gelir) — `Tumu` string[].
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/Domain/BelgeYerlesimleri.cs` — SAF sınıf, veritabanı görmez, WASM'da da çalışır (Code128 ile aynı desen). Kodlar: A4_RESMI, A4_SADE, TERMAL_80, ETIKET_50X30. `Izinli(string belgeTuru)` → o türün alt kümesi; `Varsayilan(string belgeTuru)`; bilinmeyen tür ArgumentException. Tür→izinli eşlemesi TEK YERDE: sunucu doğrulaması ve istemci açılır listesi aynı kaynaktan okur (TB-11 ikizlenme tuzağı tekrarlanmaz).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Belge/BelgeSablonModulu.cs` — `BelgeSablonServisi` (Kunye/KunyeYaz/Sablonlar/SablonYaz) + iki denetleyici: `IsletmeKunyeModulu` [Route("api/v1/isletme")] ve `BelgeSablonModulu` [Route("api/v1/belge-sablonlari")]. NEDEN AdminModulu'na EKLENMİYOR: AdminModulu.cs:71'de SINIF DÜZEYİNDE [RolGerekli("SAHIP")] var ve TAM eşleşmedir; künyeyi MUDUR'un da düzenlemesi isteniyor, mevcut denetleyiciye eklemek SAHIP kilidini ya deler ya MUDUR'u dışarıda bırakır.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Belge/BelgeKunyesi.cs` — `Task<object> Olustur(KuyumcuDbContext db, string tenantId)` — belge uçlarının HEPSİNİN çağırdığı tek yardımcı. Künye satırını okur, yoksa Tenant.Ad'a düşer, logo belge kimliğini yalnız aktif+ISLETME+GORSEL ise döndürür. Tek nokta olması, sonraki her belgenin künyeyi yeniden yazmasını önler.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki DbSet (IsletmeKunyeleri, BelgeSablonAyarlari); OnModelCreating'e iki HasQueryFilter (mevcut desen) ve iki indeks: UNIQUE(TenantId), UNIQUE(TenantId, BelgeTuru).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Belge.BelgeSablonServisi>() — tek satır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` — Yalnız satır 85: `duzenleyen = new { unvan = tenant?.Ad, sube = sube?.Ad }` → `duzenleyen = await BelgeKunyesi.Olustur(db, islem.TenantId)` ile şube eklenmiş hâli. MEVCUT ALANLAR (unvan, sube) ADLARINI KORUR — PusulaBelgesi.razor:52-55 onları okuyor, GiderPusulasiTestleri onları doğruluyor; yalnız YENİ alanlar eklenir.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs` — FAZ B. `Tutanak(string id, AktifKullanici)` servisi + [HttpGet("{id}/tutanak")]. Satır okuması AsNoTracking + Take(2000); sayılmamış satır ayrı işaretlenir.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — FAZ B. `KapanisFisi(string kasaId, string kapanisId, AktifKullanici)` + [HttpGet("{id}/gun-kapanislari/{kapanisId}/fis")]. Mevcut gün kapanışı uçlarına (satır 364-379) DOKUNULMAZ.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — FAZ C. `IslemFisi(string id, AktifKullanici)` + [HttpGet("{id}/fis")]. Yazan hiçbir metoda dokunulmaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor` — GENİŞLETİLİR (K0-1; dosya paket 42 / UX-2 Faz C'de doğar). Yeni parametreler: `Kunye` (unvan/VKN/adres/telefon/sicil/alt not), `LogoUrl`, `YerlesimKodu`, `Filigran` (null değilse `.belge-filigran` katmanı), `ImzaGoster`. Kök öğeye `belge-yerlesim-@YerlesimKodu.ToLowerInvariant()` sınıfı eklenir. Künye alanları METİN olarak basılır; MarkupString YOKTUR (K0-2).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/belge.css` — GENİŞLETİLİR. Yeni sınıflar `belge-` önekiyle ve eklemeden ÖNCE grep'lenir (kayıtlı tuzak: `.etiket` çakışması, ADR-0015 yan bulgu): .belge-kunye, .belge-logo, .belge-filigran, .belge-yerlesim-a4-resmi, .belge-yerlesim-a4-sade, .belge-yerlesim-termal-80 (@page { size: 80mm auto; margin: 3mm }), .belge-yerlesim-etiket-50x30. SABİT renk kullanılır, `var(--…)` semantik belirteci KULLANILMAZ (K0-6).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/app.css` — @media print bloğuna (satır 432) yalnız yeni sınıfların gizleme/bölünme kuralları eklenir: `.belge-kunye, .belge-filigran { page-break-inside: avoid }`. Mevcut hiçbir seçicinin adı ya da özgüllüğü değişmez (UX-2 KOŞUL 4).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukBelge` sınıfı (yerleşim adları, künye alan etiketleri, filigran metinleri, önizleme) TR+EN; `SozlukNav`'a BelgeSablonlari, SayimTutanagi, KasaKapanisFisi, IslemFisi. Mevcut SozlukPusula (satır 786-826) DEĞİŞMEZ.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs` — Her yeni @page için bir satır (UX-S13). Eklenmezse RotaBaslikTestleri KIRMIZI — bu paketin dört rotası var, hepsi eklenir.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Yalnız /ayarlar/belge-sablonlari bağlantısı (Ayarlar grubunun altına). Belge sayfaları menüye GİRMEZ; ilgili listeden açılır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json` — `Belge` bölümüne (paket 4'ün açtığı bölüm) `"TutanakAzamiSatir": 2000` ve `"KopyaAzami": 3` eklenir; sınıfta yalnız AYNI değerli varsayılanlar durur (ADR-0005).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — İki yeni denetim: (1) `MarkupString` sayımı Kuyumcu.Web içinde beklenen 1 (yalnız UrunEtiketleri.razor Code128) — artarsa ihlal; (2) IKiraciVarlik beklenen varlık sayısı 15 → 17. Mevcut denetimlere DOKUNULMAZ.

## Ekranlar
- `/isletme` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor` — FAZ A. Mevcut 'İşletme bilgisi' kartının ALTINA yeni 'Belge künyesi' kartı: unvan, VKN, vergi dairesi, adres, telefon, e-posta, web, sicil, MERSİS, belge alt notu + logo için BelgeYukleyici (SahipTuru="ISLETME", IzinliTurler=["GORSEL"]). Mevcut kartlar ve uçları değişmez. (menü/yetki: Menüde var; yazma SAHIP/MUDUR — sunucuda kapalı, ekranda gizleme yalnız kolaylıktır)
- `/ayarlar/belge-sablonlari` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/BelgeSablonlari.razor` — FAZ A. Belge türü başına satır: yerleşim açılır listesi (yalnız o tür için izinli kodlar), künye/logo/imza anahtarları, alt not, kopya sayısı, 'Önizle' düğmesi. Önizleme SAHTE veriyle BelgeYerlesimi bileşenini çizer — gerçek kayıt okumaz, okuma izi düşmez. Test kancaları: #sablon-satiri-{tur}, #sablon-yerlesim-{tur}, #sablon-kaydet. (menü/yetki: Menüde Ayarlar altında; düzenleme SAHIP/MUDUR)
- `/sayim/{Id}/tutanak` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/SayimTutanagi.razor` — FAZ B. BelgeYerlesimi ile A4 tutanak: künye, oturum kapsamı, satır tablosu (ürün, sistem, sayılan, fark), fark toplamları, sayılmamış satır sayısı, açan/kapatan, imza bloğu. IPTAL durumunda filigran. Yazdır/Geri araç çubuğu .yazdirma-gizle. (menü/yetki: Menüde yok; /sayim listesinden açılır. ModulYetkisi("sayim"))
- `/kasalar/{Id}/kapanis/{KapanisId}/fis` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/KasaKapanisFisi.razor` — FAZ B. TERMAL_80 varsayılan fiş: künye (kısa), kasa, yerel gün, sayılan/defter/fark, gerekçe, kapatan, imza satırı. Yeniden açılmışsa 'YENİDEN AÇILDI' filigranı — imzalanmış bir fişin altının sonradan doldurulduğu gizlenmez (GunKapanisi.cs:14-16 gerekçesi). (menü/yetki: Menüde yok; /kasalar kapanış listesinden. SAHIP/MUDUR/MUHASEBE)
- `/islemler/{Id}/fis` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/IslemFisi.razor` — FAZ C. TERMAL_80 satış/işlem fişi: künye, kalemler, ödeme bacakları, has karşılığı, kur snapshot kimliği, maskeli cari kimliği, iade ibaresi. (menü/yetki: Menüde yok; /islemler listesinden. ModulYetkisi("islem"))
- `/urunler/etiketler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor` — FAZ C. Mevcut ekran KORUNUR; üstüne yerleşim seçimi (ETIKET_URUN ayarından), künye/logo satırı seçeneği, 'işçilik dahil/hariç' ibaresi seçeneği eklenir. md.11/(e) ibaresi ve Code128 çizimi BİREBİR dokunulmaz (ADR-0013/0015 kanıtları geçersizleşmesin). (menü/yetki: Menüde var; mevcut yetki korunur)

## İş kuralları (fail-closed)
- FAIL-CLOSED YERLEŞİM: `yerlesim_kodu` BelgeYerlesimleri.Izinli(belgeTuru) kümesinde değilse 400. Bilinmeyen kod sessizce varsayılana DÜŞÜRÜLMEZ — kullanıcı seçtiğini sandığı yerleşimi basmamış olur.
- FAIL-SAFE AYAR SATIRI: şablon satırı yoksa kod içi varsayılan kullanılır ve belge BASILIR. Ayarın yokluğu belgeyi engellemez; kuyumcu ayar ekranına hiç girmeden gider pusulasını basabilmelidir.
- KÜNYE METİNDİR: künye ve alt not alanları hiçbir yolda MarkupString/RenderFragment olarak basılmaz; denetim.sh sayımı Kuyumcu.Web'de MarkupString = 1 (Code128) bekler.
- LOGO KAYNAĞI KAPALIDIR: logo yalnız `belgeler` tablosundan, sahip_turu='ISLETME' + tur='GORSEL' + aktif + AYNI kiracı. Dış URL alanı YOKTUR (SSRF ve kiracı sızıntısı yüzeyi açılmaz).
- KÂĞIT TEMASIZDIR: belge.css ve yeni yerleşim sınıfları semantik belirteç (`var(--yuzey…)`, `var(--metin…)`) kullanmaz; TasarimBelirtecTestleri'nin muafiyet listesi belge.css'i de kapsar.
- FİLİGRAN GİZLENEMEZ: iptal edilmiş sayım oturumu, yeniden açılmış gün kapanışı ve iade işlemi için filigran/ibare ZORUNLUDUR ve şablon ayarıyla kapatılamaz — 'imza_goster' gibi bir anahtarı YOKTUR.
- SAYILMAMIŞ ≠ SIFIR: tutanakta SayilanAdet null satır 'sayılmadı' basılır, fark hesabına GİRMEZ (SayimOturumu.cs:45 sözleşmesi kâğıda taşınır).
- KİRACI SINIRI: belge uçları id'yi her zaman tenant_id ile birlikte arar; yabancı kiracı 404 (403 değil — varlık sızdırılmaz, paket 4'ün canlı kanıtıyla aynı davranış).
- OKUMA İZİ: her belge ucu DenetimServisi.OkumaKaydet çağırır (HurdaModulu.cs:66 deseni). Önizleme ekranı gerçek kayıt okumadığı için iz düşmez.
- ÖLÇEKLENME (TB-19): tutanak satırı sınırsız ToListAsync ile çekilmez; yapılandırmadan okunan tavan (2000) aşılırsa 400 + kapsam daraltma mesajı.
- TARİH: belgedeki gün/tarih aralıkları Zaman.YerelGunAraligiUtc / YerelBugun ile üretilir; GunKapanisi.YerelGun zaten DateOnly'dir ve UTC'ye çevrilmeden basılır (K5 tuzağı).
- YAZMA YOK: bu paketin hiçbir ucu hareket/stok/kasa kaydı doğurmaz; denetim.sh'deki Hareketler.Add=1 ve VeresiyeKapanisi=2 sayıları değişmez.

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/BelgeSablonTestleri.cs` [DB]
  - Künye satırı yokken Olustur() Tenant.Ad'a düşer ve unvan doludur
  - Künye yazıldıktan sonra Olustur() unvan/VKN/adres döndürür
  - Aynı kiracıya ikinci künye satırı yazılamaz (UNIQUE tenant_id)
  - Aynı (tenant, belge_turu) için ikinci şablon satırı yazılamaz
  - Başka kiracının künyesi global filtreyle GÖRÜNMEZ
  - Logo belge kimliği başka kiracının belgesini gösteriyorsa yazma reddedilir
  - BelgeYerlesimleri.Izinli(ETIKET_URUN) A4_RESMI içermez
  - BelgeYerlesimleri.Varsayilan(bilinmeyen) ArgumentException
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/BelgeSablonHttpTestleri.cs` [HTTP]
  - GET /isletme/kunye künye yokken 200 + Tenant.Ad
  - PUT /isletme/kunye SAHIP 200, MUDUR 200, TEZGAHTAR 403
  - PUT unvan boş → 400
  - PUT VKN 9 hane → 400
  - PUT /belge-sablonlari/ETIKET_URUN yerlesimKodu=A4_RESMI → 400
  - PUT /belge-sablonlari/BILINMEYEN → 400
  - PUT kopyaSayisi=4 → 400
  - GET /belge-sablonlari tüm türleri döndürür ve ayarsız tür varsayılanla gelir
  - GET /sayim/{id}/tutanak yabancı kiracı → 404
  - GET /sayim/{id}/tutanak sonrası denetim_kayitlari'nda OKUMA satırı +1
  - GET /kasalar/{id}/gun-kapanislari/{kapanisId}/fis yanlış kasa kimliğiyle → 404
  - Yeniden açılmış kapanışta fiş yanıtı yenidenAcildi=true taşır
  - GET /gider-pusulalari/{id}/belge yanıtında duzenleyen.unvan ve duzenleyen.sube ALANLARI HÂLÂ VAR (geri uyum)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/BelgeSablonEkranTestleri.cs` [bUnit]
  - /ayarlar/belge-sablonlari yerleşim açılır listesi ETIKET_URUN satırında yalnız etiket kodlarını gösterir
  - Kaydet sonrası PUT çağrısı doğru gövdeyle gider (SahteApi deseni, CariEkranTestleri.cs)
  - Önizleme düğmesi hiçbir gerçek API çağrısı YAPMAZ
  - /isletme künye kartı unvan boşken Kaydet KAPALI (disabled)
  - /sayim/{id}/tutanak iptal oturumda filigran öğesi vardır
  - Künye alanına `<b>x</b>` yazıldığında ekranda kalın değil ham metin görünür (XSS regresyonu)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs` [saf]
  - belge.css hiçbir semantik belirtece (--yuzey/--metin/--kenarlik) referans vermez
  - @media print bloğu belge.css sınıflarını gizlemez (kâğıt boş çıkmaz)
  - Kuyumcu.Web genelinde MarkupString kullanımı tam 1 (UrunEtiketleri.razor)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs` [saf]
  - Yeni dört rota haritada var (mevcut iki yönlü test kendiliğinden zorlar — ek senaryo yazılmaz, sayı 27→31 olur)

## Mutasyon kanıtları
- M-05-01 BelgeYerlesimleri.Izinli() her tür için TÜM kodları döndürürse → 'ETIKET_URUN + A4_RESMI → 400' KIRMIZI
- M-05-02 Yerleşim doğrulaması 400 yerine sessizce varsayılana düşerse → aynı test KIRMIZI
- M-05-03 Künye yokken Olustur() null unvan döndürürse → 'künye yokken Tenant.Ad' KIRMIZI
- M-05-04 Logo kiracı kontrolü kaldırılırsa → 'yabancı kiracının logosu' testi KIRMIZI
- M-05-05 [RolGerekli("SAHIP","MUDUR")] kaldırılırsa → 'TEZGAHTAR 403' KIRMIZI
- M-05-06 Künye MarkupString ile basılırsa → XSS regresyon testi ve denetim.sh MarkupString sayımı KIRMIZI
- M-05-07 Sayılmamış satır sıfır sayılırsa → 'sayılmadı fark hesabına girmez' KIRMIZI
- M-05-08 Yeniden açılmış kapanışta filigran bayrağı yazılmazsa → 'yenidenAcildi=true' KIRMIZI
- M-05-09 Tutanak Take(tavan) kaldırılırsa → 2001 satırlık fikstürde '400 bekleniyor' KIRMIZI
- M-05-10 duzenleyen bloğundaki 'unvan'/'sube' alan adları değişirse → geri uyum testi + mevcut GiderPusulasiTestleri KIRMIZI
- M-05-11 belge.css'e var(--yuzey) eklenirse → TasarimBelirtec testi KIRMIZI

## Canlı doğrulama
- Test kiracısı açılır; /isletme künyesi gerçek bilgilerle doldurulur, PNG logo yüklenir; PUT 200 ekran görüntüsüyle kaydedilir
- /gider-pusulalari/{id}/belge tarayıcıda açılır, Ctrl+P önizlemesi alınır: künye ve logo üstte, kenar çubuğu/menü KÂĞITTA YOK, zemin BEYAZ (karanlık tema açıkken de) — öncesi/sonrası ekran görüntüsü
- /sayim/{id}/tutanak gerçek bir kapanmış oturumla açılır, yazdırma önizlemesinde tablo sayfa ortasından bölünmüyor doğrulanır; iptal edilmiş bir oturumda filigranın önizlemede göründüğü kanıtlanır
- /kasalar/… /fis TERMAL_80 ile açılır; tarayıcı yazdırma kutusunda kâğıt boyutu 80 mm seçilip taşma olmadığı görülür (fiziksel termal yazıcı B sınıfıdır, bu adım yazıcı çıktısı DEĞİL önizleme kanıtıdır — abartılmaz)
- /ayarlar/belge-sablonlari üzerinden ETIKET_URUN yerleşimi değiştirilir, /urunler/etiketler önizlemesinin değiştiği görülür
- Yabancı kiracı belirteciyle /sayim/{id}/tutanak çağrılır → 404 (gövde boş) kanıtı
- dotnet test 0 başarısız + ./denetim.sh TEMİZ çıktısı
- Test kiracısı SİLİNİR; silme kanıtı raporlanır

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/belge.css`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/GiderPusulasiTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `YENİ: /Users/…/dotnet/Kuyumcu.Api/Data/BelgeSablonu.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Api/Data/Migrations/*_IsletmeKunyesiVeBelgeSablonu.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Api/Modules/Belge/BelgeSablonModulu.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Api/Modules/Belge/BelgeKunyesi.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Core/Domain/BelgeYerlesimleri.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Web/Pages/BelgeSablonlari.razor`
- `YENİ: /Users/…/dotnet/Kuyumcu.Web/Pages/SayimTutanagi.razor`
- `YENİ: /Users/…/dotnet/Kuyumcu.Web/Pages/KasaKapanisFisi.razor`
- `YENİ: /Users/…/dotnet/Kuyumcu.Web/Pages/IslemFisi.razor`
- `YENİ: /Users/…/dotnet/Kuyumcu.Tests/BelgeSablonTestleri.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Tests/BelgeSablonHttpTestleri.cs`
- `YENİ: /Users/…/dotnet/Kuyumcu.Tests/BelgeSablonEkranTestleri.cs`
- `ÇAKIŞMA UYARISI: BelgeYerlesimi.razor ve belge.css paket 42 + UX-2 Faz C ile ORTAK; app.css @media print bloğu UX-2 ile ORTAK; Sozlukler.cs, RotaBasliklari.cs, KenarCubugu.razor, ModulKayit.cs, KuyumcuDbContext.cs, denetim.sh neredeyse tüm paketlerle ortak — paket 5 aynı dalgada 42/UX-2 Faz C ile KOŞTURULMAMALI.`

## Mevzuat dayanağı
- 213 sayılı VUK md.234 (gider pusulası) — **DOGRULANDI** — Mevcut gider pusulası belgesinin dayanağı; kodda ve ADR-0007'de yazılı, PLAN-2 Adım 1'de doğrulanmış. Bu paket yalnız görünümünü künyeyle zenginleştirir, alanlarını değiştirmez.
- Kuyum Ticareti Yönetmeliği md.11/(e) — sentetik/laboratuvar üretimi taş ibaresi — **DOGRULANDI** — ADR-0013 ve ADR-0015'te doğrulanmış; etiket üzerindeki ibare bu pakette DEĞİŞTİRİLMEZ, küçültülmez, şablonla kapatılamaz.
- 213 sayılı VUK md.186 (envanter) ve md.253 (saklama) — sayım tutanağının dayanağı — **DOGRULANMALI** — Kodda SayimOturumu.cs:10-13 yorumunda dayanak olarak yazılı ama bu oturumda bağımsız doğrulanmadı. Tutanağın 'dayanak' satırına madde numarası, doğrulanana kadar YAZILMAZ; tutanak dayanaksız da geçerli bir iç belgedir.
- Fiyat Etiketi Yönetmeliği (Ticaret Bakanlığı) md.6 — etikette bulunması zorunlu bilgiler; parite listesindeki 'işçilik dahil mi' ibaresi md.6/3'e atfediliyor — **DOGRULANMALI** — A7 araştırmasının doğrulanmış kalemleri arasında DEĞİL. Bu yüzden ibare koda KURAL olarak gömülmez: 'işçilik dahil/hariç' yalnız şablon ayarından açılan bir seçenektir ve VARSAYILANI KAPALIDIR (ADR-0005 deseni). Madde numarası doğrulanmadan etikete basılmaz.
- Vitrin fiyat listesi zorunluluğu (parite notu) — **VARSAYIM** — Kaynağı gösterilmemiş; bu pakette KAPSAM DIŞI. Pano/fiyat listesi işine (paket 49) bırakılır.
- Tevsik zorunluluğu 30.000 TL ve MASAK 185.000 TL eşikleri — **DOGRULANDI** — Bu pakette UYGULANMAZ: hiçbir uç para el değiştirmesi doğurmuyor (K0-5). Fişte gösterilen ödeme bacakları, işlemi doğuran yolda zaten kapılardan geçmiş veridir.

## Riskler
- ÇAPRAZ SAHİPLİK: BelgeYerlesimi.razor üç spesifikasyonda geçiyor. Paket 5 önce sevk edilirse bileşeni kurmak zorunda kalır ve K0-1 tersine döner. Azaltma: uygulayıcı işe başlarken `ls Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor` sonucunu rapora yazar; dosya yoksa paket DURUR ve sıraya itiraz eder.
- TEMA SIZINTISI: yeni belge sınıflarına yanlışlıkla `var(--…)` girerse karanlık temada kâğıt siyah basılır — geri dönülemez müşteri hatası. Azaltma: TasarimBelirtecTestleri genişletmesi Faz A'nın İLK işi olur.
- TERMAL 80 mm DOĞRULANAMAZ: fiziksel yazıcı yok (B sınıfı). Azaltma: kanıt 'tarayıcı yazdırma önizlemesinde 80 mm'dir ve raporda 'gerçek termal yazıcıda test edilmedi' açıkça yazılır (Madde 1).
- KÜNYE KİŞİSEL VERİ DEĞİL AMA İŞLETME VERİSİDİR: VKN ve MERSİS dışa aktarım kaynaklarına (ADR-0014) sızdırılmamalı; Disaaktarim/Kaynaklar.cs'e YENİ kaynak eklenmez.
- ETİKET REGRESYONU: UrunEtiketleri.razor'a dokunmak ADR-0015'in canlı barkod kanıtını (SVG geri çözümü) geçersizleştirebilir. Azaltma: Faz C'de Code128 çağrısı ve ibare bloğu satır satır korunur, BarkodTestleri + BarkodKulturTestleri yeşil kalmadan sevk yok.
- TB-19: tutanak 2000 satır tavanı büyük kiracıda 'basamıyorum' şikâyetine döner. Azaltma: tavan yapılandırmadan okunur ve hata mesajı kapsam daraltmayı (reyon/kategori) önerir.

## Açık sorular
- VKN/TCKN için algoritmik sağlama (mod-11 / TCKN algoritması) yapılsın mı? Bugün Cari.VergiNo'da (Entities.cs:128) hiçbir doğrulama yok; künyeye algoritma koymak asimetri yaratır. Öneri: yalnız hane sayısı doğrulanır, algoritma ayrı ve her iki yeri kapsayan bir pakette ele alınır. KARAR OYLAMAYA.
- Şube bazlı künye (şubenin kendi adresi/telefonu belgeye bassın mı)? Bugün Sube.Adres ve Sube.Telefon VAR (Entities.cs:68-69) ama belgeye girmiyor. Paket 46 (şube görünürlüğü) ile birlikte karara bağlanmalı; bu pakette şube yalnız AD olarak basılıyor.
- Fiyat etiketine SATIŞ FİYATI basılacak mı? Fiyat motoru ve fiyat listesi var (FiyatListesi.cs, FiyatMotoru), ama fiyatın etikete basımı paket 49 ile çakışır. Bu pakette etiket fiyatsız kalıyor.
- Kopya sayısı (1..3) gerçekten isteniyor mu, yoksa tarayıcının kendi kopya kutusu yeterli mi? Ekran alanı ve bir kolon maliyeti var; kullanıcı talebi doğrulanmadı.
- Fatura çıktısı (FATURA belge türü) bu listeye şimdi eklensin mi? Paket 1 (TB-17) satır kalemi modelini vermeden anlamlı bir fatura basılamaz; şimdilik kapalı listeye KONMADI — sonradan eklemek migration gerektirmez (yalnız kod içi liste), bu yüzden ertelemek ucuzdur.
- Sayım tutanağının 'dayanak' satırında VUK md.186 yazılacak mı — mevzuat doğrulaması sonrası netleşir.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Tek tablo kararı (etiket de bir belge türü) ve ölçünün yerleşim KODUNUN içinde olması doğru — ölçü serbest bırakılırsa CSS mm değerleri veriye bağlanır ve şablon dili yasağı delinir. Künyenin ayrı varlık olması denetim izi ve kiracı yazma denetçisi açısından gerekli ve kanıtlı. Fail-closed yerleşim + fail-safe ayar satırı ayrımı isabetli: ayarın yokluğu belgeyi engellememeli, ama seçilen kod doğrulanmalı. Gider pusulası yanıtındaki mevcut alan adlarının birebir korunması geri uyumu koruyor.
  - Koşullar: SIRA KİLİDİ BAĞLAYICI: paket 5 başlamadan önce `ls Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor` sonucu raporlanacak. Dosya yoksa paket DURUR — bileşeni paket 42 Faz A ya da UX-2 Faz C kurar. Üç paketin aynı dosyayı kurması üç print standardı doğurur ve bu, paketin en büyük riski. · Önkoşullardan 42 bu oylamanın kapsamında DEĞİL; oylama paket 5'i onaylasa bile sevk sırası 42/UX-2 kararı verilmeden sabitlenemez. · isletme_kunyesi'nin ayrı IKiraciVarlik tablo olması (Tenant'a kolon eklenmemesi) DOĞRU ve BAĞLAYICI — aynı gerekçeyle paket 19'un tenants.vezne_zorunlu kolonu reddediliyor (çapraz bulgu 4). · K0-2 (kullanıcı-yazılı şablon dili yok) bağlayıcı; 'belge yollarında MarkupString yasak' kuralı denetim.sh'ta beklenen 1 (yalnız Code128 SVG) olarak zorlanacak. · Kâğıt temadan muafiyeti (UX-2 KOŞUL 5) yeni belge.css'i de kapsayacak şekilde TasarimBelirtecTestleri'ne EKLENMEDEN Faz A sevk edilmez — karanlık temada siyah zemin basmak geri dönülemez müşteri hatasıdır. · Termal 80 mm iddiası yalnız 'tarayıcı yazdırma önizlemesi' kanıtıdır; raporda 'gerçek termal yazıcıda test edilmedi' açıkça yazılacak.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Kullanıcı DSL'sinin reddi ve kapalı yerleşim listesi XSS yüzeyini kapatıyor; para/defter yok. Denetim sayacının yanlış kurulması CI'ı ya kırar ya gevşetir — düzeltmesi koşul.
  - Koşullar: denetim.sh 'MarkupString = 1' sayacı YANLIŞ: gerçek kullanım UrunEtiketleri.razor:128 + AlanGrafik.razor:37/49 (+ yorum satırları). Sayaç gerçek değerle (yorumlar elenmiş, *.mutorj hariç) kurulsun; kural 'belge yollarında (Pages/*Belgesi*, *Fisi*, *Tutanagi*, Bilesenler/BelgeYerlesimi) MarkupString = 0' olarak daraltılsın. · Logo yalnız PNG/JPEG (BelgeKapisi magic byte); SVG logo YASAK (SVG içinde script/XSS). Logo `<img src>` ile basılır; CSP img-src'nin data: ya da 'self' iznini Program.cs'ten DOĞRULA — okunmadı. · Künye alanları düz metin (K0-2) — XSS regresyon bUnit testi zorunlu (spec'te var). · Künye PUT SAHIP/MUDUR ayrı controller (AdminController sınıf SAHIP kilidi delinmez — doğrulandı AdminModulu.cs:71); denetim izi SaveChanges üzerinden. · VKN/MERSİS dışa aktarım kaynaklarına eklenmez (Kaynaklar.cs dokunulmaz). · Belge uçları (tutanak/fiş) OkumaKaydet + yabancı kiracı 404 + tutanak satır tavanı (TB-19). · Sıralama kilidi K0-1: BelgeYerlesimi.razor yoksa paket DURUR.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Test ve mutasyon listesi (M-05-01..11) tutarlı, kâğıt muafiyeti testle bekçileniyor; ama paketin başlangıç şartı kodda yok ve bu bizzat spesifikasyonun 'DURUR' dediği durum.
  - Koşullar: Önkoşul FİİLEN KARŞILANMIYOR: Kuyumcu.Web/Bilesenler/ altında BelgeYerlesimi.razor YOK ve wwwroot/css/ altında belge.css YOK (ls doğrulandı: yalnız app.css, belirtecler.css). Paket 42 ve UX-2 Faz C bu turda oylanmıyor. Spesifikasyonun kendi kuralı gereği paket 5 DURUR; sevk sırası 42/UX-2 Faz C sonrası. · BelgeSablonEkranTestleri 'künye <b>x</b> ham metin görünür' XSS regresyonu EVET; ancak denetim.sh 'MarkupString = 1' sayacı Kuyumcu.Web'i taradığında paket 42/UX-2'nin kendi MarkupString kullanımı olabilir — sayı 42 sevk edildikten sonra ölçülüp yazılsın. · Sayım tutanağı 'Take(2000) kaldırılırsa 2001 satırlık fikstürde 400' testi gerçek MySQL'de 2001 satır ekler — süre riski; tavan yapılandırmadan okunuyorsa testte tavan=5 verilip 6 satırla ölçülsün. · Termal 80 mm kanıtı yalnız tarayıcı önizlemesi — raporda 'fiziksel yazıcıda test edilmedi' zorunlu cümle. · denetim.sh VeresiyeKapanisi=2 'değişmez' iddiası paket 29 sonrası 3 olur (çapraz bulgu); sayı devralınsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- SIRA KİLİDİ K0-1 BAĞLAYICI: paket 5 başlamadan `ls Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor` sonucu rapora yazılır; dosya yoksa paket DURUR. Aşkın ÖLÇTÜ: BelgeYerlesimi.razor YOK, wwwroot/css/belge.css YOK (yalnız app.css, belirtecler.css) → bileşeni paket 42 Faz A / UX-2 Faz C kurar.
- Önkoşul 42 bu oylamanın kapsamında değil; paket 5 onaylansa bile sevk sırası 42/UX-2 kararı verilmeden sabitlenemez — dalga 5 ancak 42-A aynı dalgada ve ÖNCE koşarsa geçerlidir.
- TEMA MUAFİYETİ: kâğıt temadan muafiyet (UX-2 KOŞUL 5) yeni belge.css'i kapsayacak biçimde TasarimBelirtecTestleri'ne EKLENMEDEN Faz A sevk edilmez; karanlık temada siyah kâğıt geri dönülemez müşteri hatasıdır. Bu, Faz A'nın İLK işidir.
- ÇELİŞKİ ÇÖZÜMÜ (MarkupString sayacı): Bertan 'beklenen 1 (yalnız Code128 SVG)' dedi; Mehmet ölçtü — gerçek kullanım UrunEtiketleri.razor:128 + AlanGrafik.razor:37/49. Çözüm: sayaç GERÇEK değerle (yorumlar elenmiş, *.mutorj hariç) kurulur; kural 'belge yollarında MarkupString yasak' olarak yazılır. Aşkın: 42/UX-2 sevk edilince sayı devralınır.
- isletme_kunyesi AYRI IKiraciVarlik tablo olacak (Tenant'a kolon EKLENMEZ) — bağlayıcı; aynı gerekçeyle paket 19'un tenants.vezne_zorunlu kolonu reddedildi. K0-2 (kullanıcı-yazılı şablon dili yok) bağlayıcı.
- LOGO: yalnız PNG/JPEG (BelgeKapisi magic byte); SVG logo YASAK (XSS). <img src> ile basılır; CSP img-src'nin data:/'self' izni Program.cs'ten DOĞRULANACAK — okunmadı, varsayılmayacak.
- KÜNYE: alanlar düz metin + XSS regresyon bUnit testi ('<b>x</b> ham metin görünür') zorunlu. Künye PUT SAHIP/MUDUR ayrı controller'da (AdminController sınıf SAHIP kilidi delinmez — AdminModulu.cs:71 doğrulandı); denetim izi SaveChanges üzerinden.
- GİZLİLİK: VKN/MERSİS dışa aktarım kaynaklarına EKLENMEZ (Disaaktarim/Kaynaklar.cs dokunulmaz, ADR-0014). Belge uçları OkumaKaydet + yabancı kiracı 404.
- TAVAN/TEST: tutanak 2000 satır tavanı yapılandırmadan okunur; test gerçek MySQL'de 2001 satır EKLEMEZ — tavan=5 verilip 6 satırla ölçülür (süre riski). Hata mesajı kapsam daraltmayı (reyon/kategori) önerir.
- REGRESYON + DÜRÜSTLÜK: UrunEtiketleri.razor'da Code128 çağrısı ve ibare bloğu satır satır korunur; BarkodTestleri + BarkodKulturTestleri yeşil kalmadan sevk yok. Termal 80 mm kanıtı yalnız tarayıcı önizlemesidir — raporda 'fiziksel termal yazıcıda test edilmedi' cümlesi zorunlu. denetim.sh VeresiyeKapanisi sayacı paket 29 sonrası 3 olur, devralınır.
