# Paket 46 — Şube bazlı görünürlük + yetki matrisi tamamlama

> Katman: Katman 7 — Platform · Tahmin: Faz A 2–2,5 gün · Faz B 1,5–2 gün · toplam 3,5–4,5 gün (±%40) gün · Önkoşullar: yok · Kaynak: katman mimarı K7-46 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 7

## Hedef
Perakende kuyumcunun somut üç korkusunu kapatır: çırak maliyeti/kârı görmesin, tezgâhtar kafasına göre iskonto yapmasın ve zararına satmasın, şube çalışanı diğer şubenin defterini görmesin. Bugün sistemde yalnız dört rol dizgesi ve rol×modül (ekran) matrisi var; kullanıcı başına ince yetki ve şube görünürlüğü sıfır. Rakip çıtası (Milyem: kullanıcı başına azami iskonto oranı, zararına işlem yetkisi, şube yetkisi) satış görüşmesinde birebir sorulmaktadır.

## Kodda bugünkü durum
Rol×modül matrisi VAR ve çalışıyor: Modules/Yetki/YetkiModulu.cs:20-35 (8 modül, 3 yapılandırılabilir rol), tenant başına tohumlama YetkiModulu.cs:99-118, IMemoryCache 30 dk YetkiModulu.cs:58, ModulYetkisiAttribute YetkiModulu.cs:126-138, ekran Kuyumcu.Web/Pages/Yetkiler.razor:1-60, menü gizleme Kuyumcu.Web/Layout/KenarCubugu.razor:15-74. Roller yalnız dört dizge: Data/Entities.cs:17 `Roller = [SAHIP, MUDUR, TEZGAHTAR, MUHASEBE]`, seviye tablosu Auth/AktifKullanici.cs:9-12, DTO regex KullaniciModulu.cs:37 ve :84. ŞUBE: varlık Data/Entities.cs:61-73 (subeler), kullanıcıda sube_id Entities.cs:79, CRUD Modules/Sube/SubeModulu.cs:106-133 ve GET /subeler HERKESE açık (SubeModulu.cs:116-118, RolGerekli yok), JWT claim'i Auth/AktifKullanici.cs:16, yazma yolunda kullanımı SatisModulu.cs:408/544/636 (SubeBelirle) ve transferde SatisModulu.cs:764-765. OKUMA yolunda şube filtresi neredeyse YOK: yalnız iki uçta İSTEĞE BAĞLI subeId parametresi var — RaporModulu.cs:428-431 (stok-degeri) ve KasaModulu.cs:102-105; KarZararServisi.cs:35 ve :205 de subeId alır. GunlukOzet (RaporModulu.cs:29), AylikOzet (:89), PosFaturaMutabakati (:156), CariYaslandirma (:286), VadeTakibi (:352) hiç şube bilmez. İskonto tavanı/zararına satış/maliyet gizleme: kodda HİÇBİR karşılığı yok (grep: Iskonto → yalnız kampanya indirimi, SatisModulu.cs:466-469). Satışta liste fiyatı ile uygulanan fiyat farkı ZATEN hesaplanıyor: SatisModulu.cs:465-467 `indirim = (teklif.ListeFiyati − dto.BirimFiyatTl) × dto.Adet`.

## Kapsam
**İçinde:**
- FAZ A — `kullanici_yetkileri` tablosu: kullanıcı başına azami iskonto yüzdesi, zararına satış izni, maliyet görme, kâr görme, şube kapsamı
- FAZ A — Rol seti KASIYER ve CIRAK ile genişletilir; seviye tablosu yeniden numaralanır; rol×modül matrisine iki yeni satır
- FAZ A — Satışta İskonto Tavanı Kapısı (fiş yolunda da) ve Zararına Satış Kapısı
- FAZ A — Maliyet/kâr görme kapısı: kâr-zarar uçları ve maliyet taşıyan alanlar yetkisiz kullanıcıya KAPALI (alan gizleme değil, uç reddi + alan düşürme)
- FAZ A — /yetkiler ekranına 'Kullanıcı bazlı ince yetkiler' sekmesi; /kullanicilar ekranına yeni roller
- FAZ B — `SubeKapsami` yardımcısı ve okuma uçlarına açık şube süzgeci (rapor, kasa, stok, sayım, işlem listesi)
- FAZ B — GET /subeler kapsam dışındaki şubeleri döndürmez; üst barda şube seçici (kapsamı TUMU olanlar için)
- FAZ B — Transferde kaynak şube kapsam kontrolü
- Her iki fazda reddedilen işlemin denetim izine REDDEDILDI olarak yazılması

**Dışında:**
- Meta (ürün grubu) bazlı ve cari kart grubu bazlı yetki — rakipte var, bu pakette YOK (paket sonrası ayrı iş olarak önerilir)
- Kasa/şube damgasının denetim izine eklenmesi — paket 48
- İki aşamalı giriş — paket 45
- Şubeler arası ayrı fiyat listesi / ayrı kasa politikası
- Kullanıcı bazlı 'borç-alacak işlemi yapabilme' yetkisi — paket 33 ile çakışır, oraya bırakılır
- Cari bakiyelerinin şubeye bölünmesi (mimari olarak reddedildi, bkz. ortak kararlar)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| kullanici_yetkileri | YENİ. Kullanıcı başına en fazla bir satır. Tüm ince yetki alanları NULL yapılabilir; NULL = rol varsayılanından devral. | id CHAR(36) PK · tenant_id CHAR(36) · kullanici_id CHAR(36) · azami_iskonto_yuzde DECIMAL(5,2) NULL (0-100) · zararina_satis TINYINT(1) NULL · maliyet_gorur TINYINT(1) NULL · kar_gorur TINYINT(1) NULL · sube_kapsami VARCHAR(10) NULL ('KENDI' | 'TUMU') · olusturuldu DATETIME · guncellendi DATETIME | UNIQUE (tenant_id, kullanici_id); IKiraciVarlik gereği tenant_id öncü | 20260917090000_KullaniciYetkileri |
| — | Yeni tablo IKiraciVarlik uygular → denetim.sh'daki BEKLENEN_VARLIK 15 → 16 yükseltilir (denetim.sh:76). | - | - | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/yetkiler/benim` | tüm kimlikli kullanıcılar | MEVCUT UÇ GENİŞLETİLİR (YetkiModulu.cs:159-165): moduller[] yanına inceYetkiler nesnesi (azamiIskontoYuzde, zararinaSatis, maliyetGorur, karGorur, subeKapsami, subeId) eklenir. Arayüz bunu menü/alan gizleme için kullanır; KARAR MERCİİ SUNUCUDUR. | — |
| GET | `/api/v1/yetkiler/kullanicilar` | SAHIP, MUDUR | YENİ. Tenant'ın tüm kullanıcılarının etkin ince yetkileri (miras çözülmüş + ham override birlikte) — yapılandırma ekranı için. | ModulYetkisi("kullanici") |
| PUT | `/api/v1/yetkiler/kullanicilar/{id}` | SAHIP, MUDUR | YENİ. Bir kullanıcının ince yetkilerini yazar. Alan gönderilmezse dokunulmaz, açık null gönderilirse rol varsayılanına döner (KullaniciGuncelleDto.SubeIdGonderildi deseni, KullaniciModulu.cs:58-65). | Hedef kullanıcı üzerinde HedefUzerindekiYetkiKontrol; kendi yetkisini yükseltemez; MUDUR azami iskontoyu kendi tavanının üstüne çıkaramaz |
| GET | `/api/v1/subeler` | tüm kimlikli kullanıcılar | MEVCUT UÇ DARALTILIR (SubeModulu.cs:116-118): kapsamı KENDI olan kullanıcıya yalnız kendi şubesi döner. | SubeKapsami |
| GET | `/api/v1/raporlar/* , /api/v1/raporlar/kar-zarar* , /api/v1/kasalar` | mevcut roller korunur | MEVCUT UÇLAR: opsiyonel subeId parametresi kapsamla DOĞRULANIR; parametre boşsa kapsam KENDI ise kullanıcının şubesi zorlanır, TUMU ise değişmez. | SubeKapsami + (kar-zarar için) KarGorur |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — `KullaniciYetkisi : IKiraciVarlik` varlığı (yukarıdaki kolonlar, [Table("kullanici_yetkileri")]). Satır 17'deki Roller dizisine "KASIYER", "CIRAK" eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — `public DbSet<KullaniciYetkisi> KullaniciYetkileri` (satır 57-80 bloğuna); OnModelCreating'e `HasIndex(y => new { y.TenantId, y.KullaniciId }).IsUnique()` (satır 104 civarındaki benzersizlik bloğuna komşu).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Auth/AktifKullanici.cs` — Seviye tablosu 6/5/4/3/2/1 (SAHIP..CIRAK) olarak yeniden numaralanır; XML özeti güncellenir. Kayıt tipine YENİ ALAN EKLENMEZ (ince yetkiler JWT'ye girmez).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Yetki/IncelikliYetki.cs` — `EtkinYetki` record'u (AzamiIskontoYuzde, ZararinaSatis, MaliyetGorur, KarGorur, SubeKapsami); `RolVarsayilanlari` sabit tablosu — SAHIP: 100/izinli/görür/görür/TUMU · MUDUR: 100/izinli/görür/görür/TUMU · MUHASEBE: 0/yasak/görür/görür/TUMU · TEZGAHTAR: 10/yasak/görmez/görmez/KENDI · KASIYER: 0/yasak/görmez/görmez/KENDI · CIRAK: 0/yasak/görmez/görmez/KENDI. Bilinmeyen rol → en kısıtlı (fail-closed).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Yetki/KullaniciYetkiServisi.cs` — `Task<EtkinYetki> Etkin(string tenantId, string kullaniciId, string rol)` — DB satırını okur, NULL alanları RolVarsayilanlari ile doldurur, IMemoryCache'te `inceyetki:{tenantId}:{kullaniciId}` anahtarıyla 30 dk tutar (YetkiModulu.cs:40-62 deseni birebir). `Kaydet(...)` yazınca anahtar silinir. SAHIP için DB'ye BAKILMADAN tam yetki döner (YetkiModulu.cs:66-67 deseni).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Yetki/IskontoKapisi.cs` — `static void Dogrula(decimal? listeFiyati, decimal birimFiyat, decimal adet, EtkinYetki y)` — listeFiyati null ise (kampanya/fiyat listesi yok) kapı GEÇER (iskonto oranı tanımsızdır); aksi hâlde oran = (liste − birim)/liste × 100; oran > tavan ise ForbiddenException("İskonto tavanınız aşıldı: %X (tavan %Y)"). Negatif oran (liste üstü satış) engellenmez.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Yetki/ZararinaSatisKapisi.cs` — `static void Dogrula(MaliyetSonucu maliyet, decimal netTutar, EtkinYetki y)` — y.ZararinaSatis true ise geçer. maliyet.TamBilinir false ise (devir stoku / maliyetsiz parti) kapı GEÇER ama çağıran 'ZARARINA_DOGRULANAMADI' denetim kaydı yazar. Aksi hâlde netTutar < maliyet.ToplamMaliyet ise ForbiddenException. MaliyetSonucu sözleşmesi MaliyetMotoru.cs:24-37'de tanımlıdır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — Satis (satır ~408-501) ve fiş yolu (satır ~951-968): (a) transaction AÇILMADAN önce iskonto kapısı — teklif satır 464'te zaten hesaplanıyor, ama teklif transaction İÇİNDE alınıyor; kapı için Teklif çağrısı transaction öncesine TAŞINIR (aynı veriyi okur, yan etkisi yoktur — doğrulanmalı, bkz. açık sorular); (b) PartiTuket'ten (satır 501) DÖNEN MaliyetSonucu ile zararına kapısı, commit'ten ÖNCE — throw transaction'ı geri alır; (c) SubeBelirle (satır 1542) değişmez.
- **YENI** `dotnet/Kuyumcu.Api/Common/SubeKapsami.cs` — FAZ B. `static string? Coz(EtkinYetki y, AktifKullanici islem, string? istenenSubeId)` — kapsam TUMU: istenen aynen döner (null = tüm şubeler). Kapsam KENDI: islem.SubeId null ise ForbiddenException("Şubeniz tanımlı değil"); istenen doluysa ve islem.SubeId'den farklıysa ForbiddenException; aksi hâlde islem.SubeId döner. Ayrıca `IQueryable<T> Uygula<T>(IQueryable<T> q, string? subeId)` — T : ISubeliVarlik.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — FAZ B. GunlukOzet(:29), AylikOzet(:89), PosFaturaMutabakati(:156), StokDegeri(:428) imzalarına `string? subeId` eklenir/kapsamla çözülür; hareket sorgularına `h.SubeId == subeId` koşulu (subeId null değilse). CariYaslandirma(:286) ve VadeTakibi(:352) DEĞİŞMEZ — cari bakiyesi kiracı genelidir (ortak karar).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — FAZ B. Listele(:102-105) kapsamla çözülmüş subeId kullanır; tekil Getir ve kasa hareketleri kapsam dışı kasada 404 yerine 403 döner.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` — Donem(:35), UrunBazli(:128), StokDegerlemesi(:205): controller uçlarına (satır 285, 294, 304) KarGorur/MaliyetGorur kapısı; subeId kapsamla çözülür.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Sube/SubeModulu.cs` — FAZ B. Listele(:63-66) kapsam süzgeci; Olustur/Guncelle/DeaktifEt rolleri değişmez.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs` — Satır 37 ve 84'teki RegularExpression'a KASIYER|CIRAK eklenir; satır 15 ve 285-297'deki hiyerarşi yorumları güncellenir. Kullanıcı silinince/deaktif olunca ince yetki satırı KALIR (denetim izi için), önbellek düşürülür.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` — Moduller.Roller (:26) → MUDUR, MUHASEBE, TEZGAHTAR, KASIYER, CIRAK. Varsayilan (:29-34) KASIYER = [islem, kasa], CIRAK = [urun]. YENİ ROL EKLENİNCE MEVCUT TENANT'LARDA TOHUMLAMA TEKRAR ÇALIŞMAZ (marker satırı :38 nedeniyle) → yeni roller o tenant'larda BOŞ matrisle başlar, yani hiçbir ekranı görmez (fail-closed, kabul). Matris ekranında bu durum açıkça yazılır. Controller'a iki yeni uç eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — KullaniciYetkiServisi scoped olarak eklenir (mevcut YetkiServisi kaydının yanına).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs · BelgeModulu.cs · KategoriModulu.cs · Cari/CariModulu.cs · Fatura/FaturaModulu.cs · Banka/BankaModulu.cs · Kur/KurModulu.cs · Masak/MasakModulu.cs · Hurda/HurdaModulu.cs` — Yalnız [RolGerekli(...)] listeleri: KASIYER satış/tahsilat ve ürün OKUMA uçlarına eklenir; CIRAK yalnız ürün okuma uçlarına. MASAK (MasakModulu.cs) ve hurda/gider pusulası (HurdaModulu.cs:20, :183) uçlarına KASIYER/CIRAK ASLA eklenmez — TCKN taşırlar.
- **YENI** `dotnet/Kuyumcu.Api/Data/Migrations/20260917090000_KullaniciYetkileri.cs` — Tablo + benzersiz indeks. Veri taşıma YOK (boş tablo = herkes rol varsayılanında).

## Ekranlar
- `/yetkiler` · `dotnet/Kuyumcu.Web/Pages/Yetkiler.razor` — İki sekme: (1) mevcut rol×modül matrisi — satır sayısı 3'ten 5'e çıkar; (2) YENİ 'Kullanıcı yetkileri' tablosu: kullanıcı · rol · azami iskonto % · zararına satış · maliyet görür · kâr görür · şube kapsamı. Boş bırakılan hücre 'rolden devralındı' rozetiyle gösterilir. (menü/yetki: SAHIP (mevcut, KenarCubugu.razor:71); kullanıcı sekmesi ayrıca MUDUR'a açılır)
- `/kullanicilar` · `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor` — AtanabilirRoller (:320) ve yerel Seviye kopyası (:312) yeni rollerle güncellenir; rol rozet renkleri (:338) ve etiketleri (:330) KASİYER/ÇIRAK için tamamlanır; satır sonuna 'İnce yetkiler' bağlantısı. (menü/yetki: ModulYetkisi("kullanici"))
- `—` · `dotnet/Kuyumcu.Web/Layout/UstBar.razor` — FAZ B: kapsamı TUMU olan kullanıcıya şube seçici (tümü / tek şube); seçim rapor ve kasa ekranlarına subeId olarak geçer. Kapsamı KENDI olanda seçici ÇİZİLMEZ, şube adı salt-okunur yazar. (menü/yetki: —)
- `—` · `dotnet/Kuyumcu.Web/Pages/KarZarar.razor · Raporlar.razor · Panel.razor · Urunler.razor` — KarGorur/MaliyetGorur false ise kâr ve maliyet sütunları/kartları hiç çizilmez (KenarCubugu.razor:15 deseni). Sunucu zaten alanı göndermez; arayüz yalnız boş sütun göstermemek için gizler. (menü/yetki: YetkiServisi üzerinden)

## İş kuralları (fail-closed)
- FAIL-CLOSED MİRAS: kullanici_yetkileri satırı yoksa veya alan NULL ise rol varsayılanı uygulanır; rol tanınmıyorsa EN KISITLI değerler uygulanır (iskonto 0, zararına yasak, maliyet/kâr görmez, kapsam KENDI).
- İskonto tavanı yüzde olarak tutulur, 0-100 aralığında; 100 = sınırsız. Tavan yalnız liste fiyatı BİLİNEN satırda işler; liste fiyatı yoksa iskonto oranı tanımsızdır ve kapı geçer (yanlış pozitif engelleme ürünü kilitler).
- Zararına satış kapısı maliyet TAM BİLİNMEYEN üründe ENGELLEMEZ, 'doğrulanamadı' denetim kaydı yazar. Devir stoklu yeni kurulumda aksi hâlde hiçbir satış yapılamazdı (MaliyetMotoru.cs:33 TamBilinir sözleşmesi).
- SAHIP her iki katmanda da daima tam yetkilidir ve kendi yetkisi DB'den okunmaz (mevcut YetkiModulu.cs:66 kuralının aynısı).
- Kullanıcı KENDİ ince yetkisini değiştiremez; MUDUR kendi tavanının üstünde bir tavan atayamaz ve zararına satış iznini kendinde yoksa başkasına veremez.
- ŞUBE KAPSAMI YALNIZ OKUMADIR. Tevsik kümülatifi, MASAK kümülatifi, cari bakiyesi, vade/limit ve gün kilidi sorguları KİRACI GENELİNDE kalır — bu sorgulara şube koşulu EKLENMESİ YASAKTIR.
- Kapsamı KENDI olan ve sube_id'si NULL olan kullanıcı okuma uçlarında 403 alır (bugün satışta zaten 400 alıyor: SatisModulu.cs:1544).
- Transferde (SatisModulu.cs:759-765) kapsamı KENDI olan kullanıcı KaynakSubeId olarak yalnız kendi şubesini verebilir; HedefSubeId serbesttir.
- Reddedilen her ince yetki kontrolü DenetimKaydi(Eylem="REDDEDILDI", VarlikTuru="IskontoKapisi"|"ZararinaSatisKapisi"|"SubeKapsami") yazar; Detay alanına DEĞER YAZILMAZ, yalnız kural adı (DenetimIzi.cs:45-46 sözleşmesi).

## Testler
- `dotnet/Kuyumcu.Tests/IncelikliYetkiTestleri.cs` [DB]
  - Satır yokken TEZGAHTAR'ın etkin yetkisi rol varsayılanına eşittir
  - Tek alan override edilince diğer alanlar rolden devralınır
  - Bilinmeyen rol en kısıtlı değerleri alır
  - Kaydet sonrası önbellek düşer ve yeni değer aynı istekte görülür
  - Kullanıcı kendi yetkisini değiştiremez (Forbidden)
  - MUDUR kendi tavanının üstünde tavan atayamaz
- `dotnet/Kuyumcu.Tests/IskontoTavaniTestleri.cs` [DB]
  - Tavanı %10 olan kullanıcı %15 iskontolu satış yapamaz (Forbidden) ve HİÇBİR hareket yazılmaz
  - Tavan tam sınırda (%10) satış geçer
  - Liste fiyatı olmayan üründe kapı geçer
  - Tavanı 100 olan SAHIP her fiyattan satar
  - Fiş yolunda (çok kalemli) tavan her kalem için ayrı işler
- `dotnet/Kuyumcu.Tests/ZararinaSatisTestleri.cs` [DB]
  - İzinsiz kullanıcı maliyetin altında satış yapamaz; transaction geri alınır (stok ve parti değişmemiş olmalı)
  - İzinli kullanıcı aynı satışı yapar
  - Maliyeti bilinmeyen devir stokunda satış geçer ve ZARARINA_DOGRULANAMADI denetim kaydı yazılır
  - Maliyete eşit tutarda satış engellenmez
- `dotnet/Kuyumcu.Tests/SubeKapsamiTestleri.cs` [HTTP]
  - Kapsamı KENDI olan kullanıcı başka şubenin subeId'siyle rapor isteyince 403
  - Parametresiz istekte yalnız kendi şubesinin hareketleri döner
  - Kapsamı TUMU olan kullanıcı iki şubenin toplamını görür
  - GET /subeler kapsamı KENDI olana tek şube döndürür
  - Şubesi tanımsız + kapsamı KENDI kullanıcı 403 alır
  - Transferde yabancı kaynak şube 403
  - Kâr-zarar ucu KarGorur=false kullanıcıya 403
- `dotnet/Kuyumcu.Tests/KumulatifKapiSubedenEtkilenmezTestleri.cs` [DB]
  - Aynı carinin iki FARKLI şubede aynı gün yaptığı nakit alışveriş toplamı tevsik haddini aşınca kapı kapanır (şube filtresi kümülatifi bölmemeli)
  - Aynı desen MASAK kümülatifi için tekrarlanır
- `dotnet/Kuyumcu.Tests/YetkilerEkranTestleri.cs` [bUnit]
  - Matriste beş yapılandırılabilir rol satırı çizilir
  - İnce yetki tablosunda override edilmemiş hücre 'rolden devralındı' rozeti gösterir
  - KarGorur=false oturumda KarZarar menü bağlantısı çizilmez
- `dotnet/Kuyumcu.Tests/K4ReferansKapisiTestleri.cs` [DB]
  - MEVCUT DOSYA: seviye tablosu değiştiği için rol hiyerarşisi beklentileri (satır 84, 104, 129) yeni numaralandırmayla doğrulanır

## Mutasyon kanıtları
- IskontoKapisi.Dogrula çağrısı SatisModulu'nden silinirse → IskontoTavaniTestleri 'tavan aşılan satış yazılmaz' KIRMIZI
- IskontoKapisi'ndeki `>` operatörü `>=` yapılırsa → 'tam sınırda satış geçer' KIRMIZI
- ZararinaSatisKapisi çağrısı commit'ten SONRAYA alınırsa → 'transaction geri alınır, stok değişmemiş' KIRMIZI
- RolVarsayilanlari'ndaki bilinmeyen-rol dalı en kısıtlı yerine SAHIP değerleri dönerse → 'bilinmeyen rol en kısıtlı' KIRMIZI
- SubeKapsami.Coz KENDI dalında istenen subeId'yi doğrulamadan geri döndürürse → 'başka şube 403' KIRMIZI
- TevsikKapisi sorgusuna şube koşulu EKLENİRSE → KumulatifKapiSubedenEtkilenmezTestleri KIRMIZI (bu testin tek varlık sebebi budur)
- KullaniciYetkiServisi.Kaydet'te cache.Remove silinirse → 'yeni değer aynı istekte görülür' KIRMIZI

## Canlı doğrulama
- Test kiracısı aç, iki şube (Merkez, Şube-2) ve dört kullanıcı oluştur: SAHIP, MUDUR, TEZGAHTAR(tavan %10, kapsam KENDI), CIRAK
- TEZGAHTAR ile %15 iskontolu satış dene → 403 ve hareketler tablosunda YENİ SATIR YOK (SQL ile say)
- TEZGAHTAR ile %8 iskontolu satış yap → geçer; hareketler.indirim_tutari beklenen değerde
- CIRAK ile /raporlar/kar-zarar dene → 403; menüde bağlantı çizilmiyor (ekran görüntüsü)
- TEZGAHTAR ile Şube-2'nin subeId'si ile /raporlar/gunluk dene → 403; parametresiz istekte yalnız Merkez toplamı
- SAHIP ile iki şubede aynı cariye aynı gün 20.000 + 15.000 TL nakit satış → ikincisinde tevsik kapısı kapanmalı (şube bölmesi kümülatifi bozmuyor)
- denetim_kayitlari tablosunda REDDEDILDI kayıtlarını doğrula; detay alanında DEĞER yok
- Doğrulama sonrası test kiracısını sil

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Auth/AktifKullanici.cs`
- `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Sube/SubeModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260917090000_KullaniciYetkileri.cs`
- `dotnet/Kuyumcu.Web/Pages/Yetkiler.razor`
- `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor`
- `dotnet/Kuyumcu.Web/Pages/KarZarar.razor`
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`
- `dotnet/Kuyumcu.Web/Servisler/YetkiServisi.cs`
- `dotnet/Kuyumcu.Web/Servisler/OturumServisi.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/K4ReferansKapisiTestleri.cs`
- `dotnet/Kuyumcu.Tests/ErisimKapatmaTestleri.cs`
- `dotnet/Kuyumcu.Tests/HttpOrtami.cs`

## Mevzuat dayanağı
- 459 s. VUK Genel Tebliği — tevsik zorunluluğu (30.000 TL nakit haddi, işlem tutarı kuralı) — **DOGRULANDI** — Bu pakette YENİ bir mevzuat kuralı getirilmiyor; aksine korunuyor: şube filtresinin tevsik kümülatifine SIZMAMASI bu paketin en sert değişmezidir (TevsikKapisi kiracı+cari+gün okur).
- 5549 s. Kanun / MASAK tedbirler yönetmeliği — 185.000 TL eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — Aynı gerekçe: MasakKapisi.DogrulaKumulatif şubeye bölünemez. Ayrıca KASIYER/CIRAK rolleri MASAK uçlarına ASLA açılmaz (kimlik verisi).
- 6698 s. KVKK md.12 — erişim yetkisi ve denetim izi — **DOGRULANDI** — İnce yetki reddi ve şube kapsamı ihlali denetim izine yazılır; Detay alanına kişisel veri DEĞERİ yazılmaz (mevcut ADR-0004 ve DenetimIzi.cs:45-46 sözleşmesi).
- 'Zararına satış' ve 'iskonto tavanı' için yasal dayanak — **VARSAYIM** — Bunlar MEVZUAT KURALI DEĞİL, işletme iç kontrolüdür. Hiçbir yasal metne dayandırılmıyor; ticari gerekçe rakip paritesidir (PAZAR-ARASTIRMASI-2026-09-16.md:250). Koda 'yasal' etiketiyle girmemelidir.

## Riskler
- ROL PATLAMASI: RolGerekli tam eşleşme yaptığı için yeni iki rol onlarca uçta tek tek açılmak zorunda; bir uç unutulursa KASİYER işini yapamaz (fail-closed yönde, ama saha şikâyeti üretir). Azaltım: ErisimKapatmaTestleri'ne rol×uç kapsama testi eklenir.
- MEVCUT TENANT'LARDA YENİ ROLLER BOŞ MATRİSLE BAŞLAR (YetkiModulu.cs:38 marker satırı tohumlamayı bir kez çalıştırır). Kurulu müşteride KASİYER hiçbir ekranı görmez; ekranda uyarı şart.
- SEVIYE TABLOSUNUN İKİ KOPYASI VAR (AktifKullanici.cs:11 ve Kuyumcu.Web/Pages/Kullanicilar.razor:312). Biri güncellenip diğeri unutulursa arayüz API'nin reddedeceği rolü atamaya çalışır.
- Teklif çağrısının transaction öncesine taşınması (SatisModulu.cs:464) fiyat motorunun yan etkisiz olduğu varsayımına dayanır — doğrulanmadan yapılmamalı.
- Zararına kapısının transaction İÇİNDE olması, geri alınan işlemin denetim kaydını da geri alır. Denetim kaydı ayrı bağlamda yazılmazsa 'reddedildi' izi kaybolur (ADR-0004 ile gerilim).
- Rapor uçlarına şube koşulu eklemek mevcut indeksleri (KuyumcuDbContext.cs:110-113: tenant+tarih, tenant+cari+tarih) tam karşılamayabilir; (tenant_id, sube_id, tarih) indeksi ölçülmeden eklenmemeli (TB-19 disiplini).

## Açık sorular
- ÇIRAK ve KASİYER'in kesin iş tanımı ne? Önerilen varsayılan (ÇIRAK yalnız ürün okuma, KASİYER satış+kasa) kurucudan/oylamadan onay bekler — rakip metni bu ayrımı tanımlamıyor.
- Şube kapsamı ikili mi (KENDI/TUMU) yoksa 'seçili şubeler listesi' mi olmalı? Liste, ikinci bir tablo (kullanici_sube_yetkileri) demektir; 2–3 şubelik hedef segmentte ikili yeterli görünüyor ama karar oylamanın.
- Maliyeti bilinmeyen üründe zararına kapısı GEÇSİN Mİ (önerilen) yoksa ENGELLESİN Mİ? Engellemek devir stoklu yeni kurulumda satışı kilitler.
- MaliyetMotoru'na yan etkisiz bir `MaliyetOnizle` eklenmeli mi (kapıyı transaction öncesine taşımak için), yoksa mevcut PartiTuket sonucu üzerinden transaction içinde mi kontrol edilsin (önerilen, daha az kod)?
- fiyat.Teklif (SatisModulu.cs:464) transaction dışına taşınabilir mi — FiyatModulu.Teklif'in yazma yan etkisi var mı? OKUNMADI, doğrulanmalı.
- Azami iskonto yüzde mi TL mi olmalı? Kuyumda iskonto çoğu zaman işçilik üzerinden yapılıyor (FiyatModulu.cs:34 ISCILIK_YUZDE); tavanın 'işçilik yüzdesi' olarak tanımlanması sahada daha anlamlı olabilir — sektör doğrulaması yapılmadı.
- MUDUR ince yetki yazabilsin mi, yoksa yalnız SAHIP mi? Mevcut matris ekranı yalnız SAHIP (YetkiModulu.cs:170); tutarlılık için SAHIP'e kısıtlamak da savunulabilir.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Paketin en kritik kararı DOĞRU: şube filtresi global yapılmıyor, açık parametre olarak yalnız okuma yollarına uygulanıyor. Gerekçe kanıtlı — global şube filtresi TevsikKapisi ve MasakKapisi.DogrulaKumulatif'in kiracı+cari+gün kümülatifini bölerdi ve 30.000/185.000 TL eşikleri sessizce delinirdi. İnce yetkilerin JWT'ye değil DB+önbelleğe konması (15 dk eski tavanla satış yapılmasın diye) isabetli. NULL = rolden devral mirası, üçüncü tablo açmadan deterministik.
  - Koşullar: ROL SETİ GENİŞLETMESİ (KASIYER/CIRAK) AYRI FAZ OLSUN ve paket 29'un modül-başı yetki tohumlaması SEVK EDİLDİKTEN SONRA gelsin. Aksi hâlde yeni roller mevcut kiracılarda boş matrisle doğar ve hiçbir ekranı göremez; ayrıca RolGerekli tam eşleşme olduğu için onlarca uçta tek tek açma işi doğuyor — bu, ince yetki çekirdeğiyle aynı pakette taşınamayacak bir ayak izi. · fiyat.Teklif çağrısının transaction dışına taşınabilirliği (FiyatModulu.Teklif'in yan etkisiz olduğu) OKUNMADAN varsayılamaz. Doğrulanana kadar iskonto kapısı transaction İÇİNDE, PartiTuket sonucuyla aynı yerde çalışsın — throw zaten işlemi geri alır. · Zararına satış kapısı maliyet TAM BİLİNMEYEN üründe GEÇSİN (önerildiği gibi) ama 'ZARARINA_DOGRULANAMADI' denetim kaydı transaction'dan BAĞIMSIZ yazılmalı; aynı transaction'da yazılırsa red senaryosunda iz de geri alınır ve ADR-0004 ruhu delinir (paket 48'in de aynı gerilimi var). · Seviye tablosunun İKİ KOPYASI (AktifKullanici.cs ve Kuyumcu.Web/Pages/Kullanicilar.razor) aynı düzenlemede güncellenecek; tek kaynağa indirilemiyorsa bir test ikisinin eşitliğini zorlayacak. · KumulatifKapiSubedenEtkilenmezTestleri (şube filtresinin tevsik/MASAK kümülatifine sızmadığı) paketin KABUL ÖLÇÜTÜDÜR, opsiyonel test değil.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Fail-closed miras, tam eşleşmeli rol genişlemesi, kiracı geneli kümülatif kapıların korunması doğru. Riskler denetim izi kaybı ve önbellek tazeliği; koşullarla kapanır.
  - Koşullar: Şube kapsamı YALNIZ okuma; TevsikKapisi/MasakKapisi/VadeKapisi/CariBakiye sorgularına şube koşulu EKLENMESİ YASAK — KumulatifKapiSubedenEtkilenmezTestleri sevk kapısı (spec böyle, vetomun bekçisi). · KASIYER/CIRAK MASAK, hurda/gider pusulası, dışa aktarım, denetim izi, kullanıcı yönetimi uçlarına ASLA açılmaz; ErisimKapatmaTestleri'ne rol×uç kapsama testi. · Reddedilen kapı denetim kaydı ayrı DbContext scope'unda yazılsın (transaction geri alınınca 'REDDEDILDI' izi kaybolmasın) — ADR-0004 ile gerilim spec'te; çözümü zorunlu. · İnce yetki önbelleği: Kaydet'te VE kullanıcı rol/deaktif değişiminde (KullaniciModulu) invalide — 30 dk eski tavanla satış olmasın; test. · Faz A'da ince yetki YAZMA yalnız SAHIP (mevcut matris ekranı SAHIP, YetkiModulu.cs:170 ile tutarlı); MUDUR'a açılması ayrı oy. · Seviye tablosu iki kopya (AktifKullanici.cs:11, Kullanicilar.razor:312) aynı değişiklikte; K4ReferansKapisiTestleri güncellenir. · Yeni roller mevcut kiracılarda boş matrisle başlar (fail-closed) — KABUL, ancak paket 29'un modül-başı tohumlamasıyla TEK mekanizma (bkz. çapraz bulgu). · fiyat.Teklif'in transaction dışına taşınması yan etkisizliği OKUNMADAN yapılmaz.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — İnce yetki tasarımı ve kümülatif kapı koruması kanıtlı; mutasyon listesi test adlarına bağlı. Fakat is_kurallari 'reddedilen işlem denetim izine yazılır' derken tasarım bunu rollback ile siliyor ve test yok; iki tabloda tutulan seviye için tutarlılık testi yok.
  - Koşullar: Zararına satış kapısı transaction İÇİNDE; reddedilince rollback denetim kaydını da siler (risk bölümünde kabul edilmiş). Test listesinde 'REDDEDILDI denetim kaydı rollback sonrası KALIR' testi yok — ya denetim kaydı ayrı DbContext/bağlamda yazılsın ve bu test eklensin, ya da 'denetim izi düşer' iddiası is_kurallari'ndan çıkarılsın. Şu hâliyle kural ile uygulama çelişiyor. · fiyat.Teklif'in transaction öncesine taşınması yan etkisiz varsayımına dayanıyor ve OKUNMADI deniyor; taşımadan önce FiyatModulu.Teklif'te yazma olmadığı (SaveChanges/Add yok) kaynak taraması testi ya da okuma kanıtı istensin. · Seviye tablosunun iki kopyası (AktifKullanici.cs:11, Kullanicilar.razor:312): ikisinin eşitliğini ölçen bir saf test eklensin (Web tarafındaki tablo Core'a taşınamıyorsa yansımayla karşılaştırma). · 'Rol×uç kapsama testi' risk bölümünde öneriliyor ama test listesinde yok; ErisimKapatmaTestleri'ne 'KASIYER için beklenen uç listesi 200, diğer tüm yazma uçları 403' tablo güdümlü test eklensin (fail-closed rolün saha şikâyetini önler). · KumulatifKapiSubedenEtkilenmezTestleri EVET — ancak iki şubede aynı gün fikstürü GunKapanisi kilidiyle çakışmasın diye kasa/gün kurulumu açıkça yazılsın. · Paket 19 ile çelişki: 19 'rol eklenmez' derken 46 KASIYER/CIRAK ekliyor ve 19'un R1 kapısı yalnız TEZGAHTAR'ı kesiyor; KASIYER (satış+kasa) vezne zorunluyken doğrudan satış yapabilir mi? Sıralama ve R1 rol listesi birlikte kararlaştırılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Şube kapsamı YALNIZ okuma yollarına açık parametre olarak uygulanır; TevsikKapisi/MasakKapisi/VadeKapisi/CariBakiye sorgularına şube koşulu eklenmesi YASAK. KumulatifKapiSubedenEtkilenmezTestleri sevk kapısıdır, opsiyonel test değil.
- ÇELİŞKİ ÇÖZÜMÜ (Bertan 'ayrı faz' ↔ Mehmet 'fail-closed kabul'): KASIYER/CIRAK rol genişletmesi Faz B'ye alınır ve paket 29'un modül-başı tohumlamasından SONRA, onunla TEK mekanizma olarak sevk edilir; Faz A yalnız ince yetki çekirdeği.
- KASIYER/CIRAK MASAK, hurda/gider pusulası, dışa aktarım, denetim izi ve kullanıcı yönetimi uçlarına asla açılmaz; ErisimKapatmaTestleri'ne tablo güdümlü rol×uç kapsama testi (beklenen uçlar 200, diğer tüm yazma uçları 403) eklenir.
- Reddedilen kapı (zararına satış / azami iskonto) denetim kaydı AYRI DbContext scope'unda yazılır — transaction geri alınınca 'REDDEDILDI' izi kaybolmasın (ADR-0004). 'Rollback sonrası iz KALIR' testi eklenir, yoksa iddia is_kurallari'ndan çıkarılır.
- Zararına satış kapısı maliyet TAM BİLİNMEYEN üründe GEÇER, ancak 'ZARARINA_DOGRULANAMADI' izi transaction'dan bağımsız yazılır (devir stoklu yeni kurulumda satış kilitlenmesin).
- fiyat.Teklif (SatisModulu.cs:464) transaction dışına TAŞINMAZ: FiyatModulu.Teklif'te SaveChanges/Add olmadığı okunarak/kaynak taramasıyla kanıtlanana dek kapı transaction İÇİNDE, PartiTuket sonucuyla aynı yerde çalışır.
- Seviye tablosunun iki kopyası (AktifKullanici.cs:11 ve Kuyumcu.Web/Pages/Kullanicilar.razor:312) aynı düzenlemede güncellenir; eşitliği yansımayla ölçen saf test + K4ReferansKapisiTestleri güncellemesi zorunlu.
- İnce yetkiler JWT'ye değil DB+önbelleğe konur; önbellek hem Kaydet'te hem KullaniciModulu'ndaki rol/deaktif değişiminde invalide edilir (eski tavanla satış olmasın) — testi yazılır.
- Faz A'da ince yetki YAZMA yalnız SAHIP (YetkiModulu.cs:170 ile tutarlı); MUDUR'a açılması ayrı oy. Şube kapsamı ikili (KENDI/TUMU) başlar, liste modeli ayrı paket.
- Paket 19 ile çelişki önce çözülür: 19'un R1 kapısı yalnız TEZGAHTAR'ı kesiyor; KASİYER vezne zorunluyken doğrudan satış yapabilir mi — R1 rol listesi 46 ile birlikte kararlaştırılır. (tenant_id, sube_id, tarih) indeksi ölçülmeden eklenmez (TB-19).
