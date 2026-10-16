# Paket 25 — Reyon/vitrin bazlı stok bölümlemesi

> Katman: Katman 3 — Stok · Tahmin: Faz A 2–3 gün, Faz B 1–1,5 gün (PARITE tahmini 2–3 gün yalnız Faz A'yı karşılar; sayımın yerleşime taşınması PARITE'de hesaplanmamış ek iştir) gün · Önkoşullar: 23, 24 · Kaynak: katman mimarı K3-25 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Bugün sistem 'bu ÜRÜN şu vitrinde durur' diyebiliyor; 'bu üründen 3'ü vitrinde, 2'si kasada' diyemiyor. Perakende kuyumcuda aynı model yüzük hem vitrinde hem kasada durur; sayım farkı çıkınca hangi vitrinin sorumlusuna sorulacağı belirsiz kalır. Bu paket stok adedini şube içinde reyonlara böler, reyonlar arası taşımayı kayıt altına alır ve reyon bazlı stok/sorumluluk raporu verir. Kasa/depo/vitrin/çanta ayrımı fiziksel sayımı ve kayıp tespitini mümkün kılar.

## Kodda bugünkü durum
Reyon varlığı var: Data/UrunKategorisi.cs:58-74 (`reyonlar`: sube_id, ad, tip VITRIN|KASA|DEPO|CANTA, sorumlu_kullanici_id, aktif). Kapsam sınırı dosyanın kendi yorumunda açıkça yazılı (UrunKategorisi.cs:44-52: 'reyon ÜRÜN KARTINA bağlanır, stok satırına DEĞİL'). Bağ ürün kartında: Entities.cs:239 `urunler.reyon_id`. Stok satırı reyon taşımıyor: Entities.cs:263-273 `stoklar` = (id, tenant_id, sube_id, urun_id, adet, toplam_has_gram, guncellendi). Uçlar yalnız GET/POST: KategoriModulu.cs:282-287 (`api/v1/reyonlar`, Listele+Olustur; PATCH/DELETE YOK). ReyonServisi.Listele urün sayısını ÜRÜN KARTINDAN sayıyor (KategoriModulu.cs:194-199 `db.Urunler...Where(u.ReyonId != null).GroupBy`). Sayım kapsamı da ürün kartından: SayimModulu.cs:60-64 `urunler.Where(u => u.ReyonId == dto.KapsamId)`. Ekranda sınır dürüstçe yazılı: Sozlukler.cs:1938 KapsamUyarisi. Stok yazan iki yol var: SatisModulu.cs:1594 `StokDegistir` (satış/alış/takas/transfer) ve UrunModulu.cs:606 `StokSayimGuncelle` (sayım). Şubeler arası transfer: SatisModulu.cs:757-815. Maliyet partisi ayrı ve sube+urun anahtarlı: StokPartisi.cs:82-83.

## Kapsam
**İçinde:**
- Yeni `stok_yerlesimleri` tablosu ve değişmez I1 (Σ yerleşim ≤ stok adedi, fark = ATANMAMIŞ)
- Yerleşim okuma uçları: reyon içeriği, ürünün şube×reyon dağılımı
- Atama ucu (atanmamış → reyon) ve reyon içi taşıma ucu (aynı şube, reyon→reyon)
- Reyon güncelleme (PATCH: ad/tip/sorumlu/aktif) ve pasife alma kapısı
- Stok azaltan/artıran mevcut akışlara opsiyonel `reyonId` bacağı (satış, alış, hurda alış, takas, iade, şubeler arası transfer, sayım düzeltmesi)
- Reyon stok değeri raporu (sunucuda toplama, TB-19)
- Faz B: sayım oturumu kapsamının reyondan (yerleşimden) okunması + reyon sorumlusu fark raporu
- Sozlukler TR+EN, UrunAgaci/Urunler/Islemler/Raporlar ekran değişiklikleri

**Dışında:**
- Maliyet partisinin (StokPartisi) reyon bazına indirilmesi — FIFO/LIFO ve PartiTasi değişmez
- Reyon bazlı kâr/zarar ve reyon bazlı fiyat listesi
- Reyon barkodu/QR ile fiziksel raf etiketi basımı
- RFID ile otomatik reyon tespiti
- Şubeler arası reyon eşleme (hedef şubede otomatik aynı adlı reyon yaratma)
- Reyon başına devir/kapanış bakiyesi (gün kapanışı paketiyle ilişkilendirilmez)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| stok_yerlesimleri | YENİ. Şube toplam stoğunun reyon kırılımı. `stoklar` tablosu DEĞİŞMEZ. | id varchar(36) PK · tenant_id varchar(36) NOT NULL · sube_id varchar(36) NOT NULL · urun_id varchar(36) NOT NULL · reyon_id varchar(36) NOT NULL · adet decimal(12,3) NOT NULL (stoklar.adet ile aynı ölçek; gramlı üründe 1 adet = 1 gram, A15 sözleşmesi) · guncellendi datetime NOT NULL | UNIQUE (tenant_id, sube_id, urun_id, reyon_id) · (tenant_id, reyon_id) [reyon içeriği raporu] · (tenant_id, sube_id, urun_id) [düşüm yolu] | 20260916HHMMSS_StokYerlesimi |
| reyonlar | Kolon eklenmez. Yalnız PATCH ucu açılır (aktif/ad/tip/sorumlu). Mevcut `aktif` kolonu (UrunKategorisi.cs:71) pasife almada kullanılır. | — | EKLE: (tenant_id, sube_id, ad) UNIQUE — bugün çakışma yalnız kodda kontrol ediliyor (KategoriModulu.cs:220-222), eşzamanlı iki POST iki aynı adlı reyon üretebilir | aynı migration içinde |
| urunler | Kolon değişmez; `reyon_id`nin ANLAMI belgelenerek daraltılır (varsayılan/ana reyon). XML yorum Entities.cs:239'da güncellenir. | — | — | yok (şema değişmez) |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/reyonlar/{id}/stok` | tüm roller | Reyonun içeriği: ürün kodu/adı/birimi, adet, toplam has (adet × urun.HasGrami). Sunucuda GroupBy, sayfalı (varsayılan 200, `limit`/`ofset`). | ModulYetkisi("urun") |
| GET | `/api/v1/urunler/{id}/yerlesim` | tüm roller | Ürünün şube×reyon dağılımı + her şube için `atanmamis` = stoklar.adet − Σ yerleşim. Şube toplamı `stoklar`dan okunur, yerleşimden TÜRETİLMEZ. | ModulYetkisi("urun") |
| POST | `/api/v1/reyonlar/yerlesim` | SAHIP, MUDUR, TEZGAHTAR | Atanmamış maldan reyona yerleştirme. Gövde: {subeId, urunId, reyonId, adet}. adet > atanmamis → 400. | ModulYetkisi("urun") · SubeDogrula · ReyonDogrula (reyon.SubeId == subeId ve aktif) |
| POST | `/api/v1/reyonlar/tasima` | SAHIP, MUDUR, TEZGAHTAR | Aynı şube içinde reyon→reyon taşıma. Gövde: {subeId, urunId, kaynakReyonId, hedefReyonId, adet, aciklama}. Kaynak = hedef → 400. Kaynakta yetersiz → 400. Toplam stok DEĞİŞMEZ. | ModulYetkisi("urun") · iki reyon da aynı şube ve aktif · DenetimKaydi yazılır |
| POST | `/api/v1/reyonlar/geri-al` | SAHIP, MUDUR | Reyondan atanmamışa geri çekme (yanlış yerleştirmeyi düzeltmek için). Gövde: {subeId, urunId, reyonId, adet}. | ModulYetkisi("urun") |
| PATCH | `/api/v1/reyonlar/{id}` | SAHIP, MUDUR | Ad/tip/sorumluKullaniciId/aktif güncelleme. Bugün YOK (KategoriModulu.cs:282-287'de yalnız GET+POST). aktif=false yapılırken reyonda yerleşim varsa 400 ('önce malı boşaltın'). | ModulYetkisi("urun") · sorumlu kullanıcı kiracı doğrulaması (KategoriModulu.cs:212-217 deseni) |
| GET | `/api/v1/raporlar/reyon-stok` | SAHIP, MUDUR, MUHASEBE | Sorgu: subeId?, reyonId?, maden?. Reyon bazında adet, toplam has (madene göre AYRI toplanır — RaporModulu.cs:470'teki 'farklı madenler toplanmaz' kuralı), atanmamış satırı ayrı gösterilir. | ModulYetkisi("rapor") · TB-19: sınırsız ToListAsync yok, GroupBy sunucuda |
| POST | `/api/v1/islemler/* (mevcut uçlar)` | mevcut roller aynen | SatisDto/AlisDto/TransferDto/TakasDto'ya OPSİYONEL `reyonId` (transferde ayrıca `hedefReyonId`) eklenir. Yol ve mevcut sözleşme değişmez; alan yoksa KARAR 3 sırası uygulanır. | mevcut kapılar aynen (tevsik/MASAK/vade/gün kilidi dokunulmaz) |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/StokYerlesimi.cs` — `StokYerlesimi : IKiraciVarlik` varlığı + sınıf başında değişmez I1'i ve 'reyon alt-bölümlemedir, stok gerçeği stoklar tablosudur' kararını anlatan yorum bloğu (UrunKategorisi.cs:44-52 deseninde, dürüst kapsam notu).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — `DbSet<StokYerlesimi> StokYerlesimleri` (satır 89 civarı, Reyonlar'ın yanına) + OnModelCreating'e üç indeks (UNIQUE dörtlü, (tenant,reyon), (tenant,sube,urun)) + reyonlar için (tenant,sube,ad) UNIQUE. Kiracı filtresi ELLE EKLENMEZ: IKiraciVarlik uygulayan her varlığa otomatik bağlanıyor (KuyumcuDbContext.cs:207-208).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Stok/YerlesimServisi.cs` — Tek merkez. Metotlar: `Task Yerlestir(tenant, sube, urun, reyon, adet, now)` · `Task Tasi(...)` · `Task GeriAl(...)` · `Task<string?> CikisDus(tenant, sube, urun, adet, string? reyonId, now)` — KARAR 3 sırasını uygular, uyarı metni döner · `Task GirisEkle(tenant, sube, urun, adet, string? reyonId, string? varsayilanReyonId, now)` · `Task<decimal> Atanmamis(...)`. Tüm sorgularda elle `TenantId ==` şartı (ADR-0010). `db.StokYerlesimleri.Local` önce bakılır (SatisModulu.cs:1600 deseni — aynı transaction içindeki kaydedilmemiş satır).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Stok/YerlesimModulu.cs` — DTO'lar (YerlestirDto, TasimaDto, GeriAlDto) + `YerlesimController` (Route `api/v1/reyonlar`, ModulYetkisi("urun"), RolGerekli TAM eşleşme). Taşıma/atama/geri-al her biri transaction içinde ve DenetimKaydi yazar.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs` — ReyonServisi'ne `Guncelle(id, ReyonGuncelleDto, islem)` eklenir (pasife alma kapısı dahil); `Listele` çıktısına `yerlesikAdet` (Σ stok_yerlesimleri.adet) eklenir — mevcut `urunSayisi` (kart bazlı) KALDIRILMAZ, iki sayı farklı şeydir ve ikisi de gösterilir. Controller'a PATCH eklenir (satır 287 sonrası).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — `StokDegistir` (satır 1594) imzasına `string? reyonId, string? varsayilanReyonId` eklenir; gövdesinin SONUNDA YerlesimServisi.CikisDus/GirisEkle çağrılır (mevcut aritmetik DEĞİŞMEZ). `Transfer` (satır 757): kaynak şubede CikisDus(dto.ReyonId), hedef şubede GirisEkle(dto.HedefReyonId). DTO'lara opsiyonel alanlar. Maliyet çağrıları (PartiTasi/PartiTuket) DOKUNULMAZ.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — `StokSayimGuncelle` (satır 606): fark > 0 → GirisEkle, fark < 0 → CikisDus (sayımda reyonId sayım satırından gelir, Faz B). `ReyonDogrula` (satır 373) yeniden kullanılır — yeni doğrulayıcı yazılmaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — `ReyonStok(islem, subeId, reyonId, maden)` eklenir; GroupBy sunucuda (TB-19). Mevcut `StokDegeri` (satır 428) DOKUNULMAZ.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — `YerlesimServisi` DI kaydı (scoped).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs` — FAZ B: `Ac` içinde KapsamTur=="REYON" dalı (satır 60-64) ürün kartı yerine `stok_yerlesimleri`den okur — kapsam = o reyonda yerleşimi olan ürünler; sistem adedi olarak YERLEŞİM adedi dondurulur (şube toplamı değil). `Detay` fark raporuna `reyonSorumlusu` eklenir. Kapanışta düzeltme yine TB-18 yolundan geçer, fark yerleşime de yansır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — Yeni `Yerlesim` bölümü TR+EN (Atanmamis/Yerlestir/Tasi/GeriAl/KaynakReyon/HedefReyon/YetersizYerlesim/ReyonBosaltilmali/YerlesimUyarisi). UrunAgaci.KapsamUyarisi (satır 1938 TR, EN karşılığı ~2852) FAZ B'de YENİ GERÇEĞE göre güncellenir — eski uyarı olduğu gibi kalırsa yalan söyler.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — `BEKLENEN_VARLIK` (satır 67) 31→32.

## Ekranlar
- `/urunler/agac` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor` — Reyon tablosuna (satır 78-95) 'yerleşik adet' sütunu ve satır tıklayınca açılan reyon içeriği paneli; reyon düzenle (PATCH) ve pasife al düğmeleri; 'Taşı' modalı (kaynak reyon sabit, hedef reyon + ürün + adet). (menü/yetki: mevcut menü girişi (KenarCubugu.razor:22) değişmez; düzenleme düğmeleri SAHIP/MUDUR'da görünür)
- `/urunler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Urunler.razor` — Ürün detayında 'Yerleşim' sekmesi: şube×reyon dağılımı + ATANMAMIŞ satırı (vurgulu); 'Reyona yerleştir' düğmesi. Mevcut reyon süzgeci (kart bazlı) korunur, başlığı 'Varsayılan reyon' olur. (menü/yetki: mevcut)
- `/islemler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Islemler.razor` — Satış/alış/transfer formlarına opsiyonel reyon seçici (seçilen şubenin aktif reyonları). Boş bırakılırsa yanıttaki `yerlesimUyarisi` bir bilgi bandında gösterilir — sessizce yutulmaz. (menü/yetki: mevcut)
- `/raporlar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — 'Reyon stok' raporu sekmesi: şube/reyon süzgeci, maden bazında ayrı toplam, atanmamış satırı ayrı. (menü/yetki: SAHIP/MUDUR/MUHASEBE)
- `/sayim` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Sayim.razor` — FAZ B: REYON kapsamı seçilince 'sistem adedi reyon yerleşiminden alınır' notu ve fark raporunda sorumlu kullanıcı sütunu (Sayim.razor:27-31 kapsam seçicisi korunur). (menü/yetki: mevcut)

## İş kuralları (fail-closed)
- I1 (DEĞİŞMEZ): Σ stok_yerlesimleri.adet ≤ stoklar.adet — her yerleştirme/taşıma/giriş sonrası kontrol edilir, ihlal 400.
- Yerleşim adedi negatif olamaz; sıfıra düşen yerleşim satırı SİLİNİR (çöp satır bırakılmaz) — ancak taşıma izi DenetimKaydi'nda kalır.
- Reyon reyonun şubesine kilitlidir: reyon.SubeId != istenen subeId → 400. Şubeler arası taşıma 'reyon taşıma' ucuyla YAPILAMAZ, TRANSFER ucuyla yapılır.
- Pasif reyona yerleştirme/taşıma 400. İçinde yerleşimi olan reyon pasife alınamaz (400).
- Çıkışta reyonId verilmişse ve o reyonda yetersizse 400 — başka reyondan sessizce alınmaz (fail-closed).
- reyonId verilmemişse: önce ATANMAMIŞ, sonra reyonlar Ad artan sırada tüketilir; yanıtta uyarı döner.
- Girişte reyonId verilmemişse ve ürünün varsayılan reyonu (urunler.reyon_id) o şubeye ait AKTİF bir reyonsa oraya, değilse ATANMAMIŞ'a yazılır. Varsayılan reyon başka şubeye aitse SESSİZCE kullanılmaz.
- Reyon içi taşıma stoklar tablosuna, hareketler defterine, kasaya, maliyet partisine DOKUNMAZ; yalnız stok_yerlesimleri + denetim_kayitlari.
- Tevsik/MASAK/vade/gün kilidi kapıları bu pakette ÇAĞRILMAZ: para el değiştirmez, borç doğmaz (SAYIM_DUZELTME ile aynı gerekçe, A14/A17 hattı).
- Gramlı üründe (A15) yerleşim adedi gram sayısıdır; 3 ondalık, ayrı bir has yolu açılmaz.
- Yerleşim toplamı ile stok toplamı ASLA birbirinden türetilmez; rapor her ikisini de gösterir ve tutarsızlık varsa 'atanmamış' olarak görünür (gizlenmez).
- Tüm yerleşim sorgularında elle TenantId şartı yazılır (ADR-0010); IgnoreQueryFilters/Find/FindAsync kullanılmaz.

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/StokYerlesimiTestleri.cs` [DB]
  - 10 adet stok, 4'ü vitrine yerleştirilir → vitrin 4, atanmamış 6
  - atanmamıştan fazlası yerleştirilemez (11 → 400)
  - vitrin→kasa 2 taşıma: stoklar.adet DEĞİŞMEZ, vitrin 2 kasa 2
  - kaynak reyonda yetersiz taşıma → 400
  - kaynak = hedef reyon → 400
  - başka şubenin reyonuna yerleştirme → 400
  - pasif reyona yerleştirme → 400
  - içinde yerleşim olan reyon pasife alınamaz → 400
  - yerleşim 0'a düşünce satır silinir
  - yabancı kiracının reyonuna yerleştirme → 404/400 (kiracı sızıntısı yok)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YerlesimCikisTestleri.cs` [DB]
  - reyonId ile satış: yalnız o reyondan düşer
  - reyonId ile satış, o reyonda yetersiz → 400 (stok yeterli olsa bile)
  - reyonId'siz satış: önce atanmamış tükenir
  - atanmamış bitince Ad sırasıyla reyon tüketilir + uyarı döner
  - şubeler arası transfer: kaynak reyondan düşer, hedefte hedefReyonId'ye girer
  - hedefReyonId yoksa hedef şubede ATANMAMIŞ'a girer
  - alışta varsayılan reyon (urunler.reyon_id) doğru şubede ise oraya girer
  - varsayılan reyon başka şubedeyse atanmamışa girer
  - satış sonrası I1 korunur (Σ yerleşim ≤ stok)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/ReyonYonetimTestleri.cs` [HTTP]
  - PATCH ile ad/tip/sorumlu güncellenir
  - aynı şubede aynı adla ikinci reyon → 409
  - TEZGAHTAR PATCH → 403, MUDUR → 200
  - reyon içi taşıma DenetimKaydi yazar (append-only)
  - taşıma hareketler defterine SATIR YAZMAZ (hareket sayısı değişmez)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/ReyonRaporTestleri.cs` [DB]
  - reyon stok raporu madenleri ayrı toplar
  - atanmamış ayrı satır olarak görünür
  - subeId süzgeci uygulanır
  - rapor sınırsız ToListAsync kullanmaz (GroupBy sunucuda — sorgu sayısı/plan kanıtı)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YerlesimEkranTestleri.cs` [bUnit]
  - UrunAgaci reyon satırında yerleşik adet gösterilir
  - Taşı modalı hedef reyon seçilmeden Kaydet'i kapalı tutar
  - adet 0 veya kaynaktan büyükse Kaydet kapalı
  - ürün detayı Yerleşim sekmesi ATANMAMIŞ satırını gösterir
  - reyonsuz satışta dönen yerlesimUyarisi ekranda bir bandda gösterilir (yutulmaz)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/SayimReyonKapsamTestleri.cs` [DB]
  - FAZ B: REYON kapsamlı oturum kapsamı yerleşimden okur (ürün kartından değil)
  - FAZ B: sistem adedi olarak reyon yerleşimi dondurulur, şube toplamı değil
  - FAZ B: kapanışta fark yerleşime de yansır ve I1 korunur
  - FAZ B: fark raporunda reyon sorumlusu görünür

## Mutasyon kanıtları
- I1 kontrolü kaldırılırsa (Σ yerleşim ≤ stok) → StokYerlesimiTestleri 'atanmamıştan fazlası yerleştirilemez' KIRMIZI
- Çıkışta reyon yetersizliğinde 400 yerine başka reyondan düşülürse → YerlesimCikisTestleri 'reyonId ile satış, o reyonda yetersiz' KIRMIZI
- Çıkış sırası 'atanmamış önce' yerine reyon önce yapılırsa → 'reyonId'siz satış: önce atanmamış tükenir' KIRMIZI
- Reyonun şube kilidi kaldırılırsa → 'başka şubenin reyonuna yerleştirme' KIRMIZI
- Pasife alma kapısı kaldırılırsa → 'içinde yerleşim olan reyon pasife alınamaz' KIRMIZI
- Reyon içi taşıma HareketFabrikasi ile deftere yazılırsa → ReyonYonetimTestleri 'taşıma hareketler defterine satır yazmaz' KIRMIZI
- Taşımada DenetimKaydi yazımı kaldırılırsa → 'taşıma DenetimKaydi yazar' KIRMIZI
- Varsayılan reyonun şube kontrolü kaldırılırsa → 'varsayılan reyon başka şubedeyse atanmamışa girer' KIRMIZI
- FAZ B: sayım kapsamı tekrar ürün kartından okunursa → SayimReyonKapsamTestleri KIRMIZI
- Raporda madenler tek toplamda birleştirilirse → ReyonRaporTestleri 'madenleri ayrı toplar' KIRMIZI

## Canlı doğrulama
- Test kiracısı REYON-TEST açılır; 1 şube, 3 reyon (VITRIN-A, KASA, DEPO), 2 ürün (biri ADET biri GRAM).
- 10 adet alış → ATANMAMIŞ 10 doğrulanır (SQL: stok_yerlesimleri boş, stoklar.adet=10).
- 4 adet VITRIN-A'ya yerleştir → SQL'de tek satır 4, atanmamış 6.
- VITRIN-A → KASA 2 taşı → stoklar.adet HÂLÂ 10 (SQL), yerleşim 2/2, hareketler tablosunda YENİ SATIR YOK (taşıma öncesi/sonrası COUNT eşit), denetim_kayitlari'nda 1 yeni satır.
- reyonId=KASA ile 3 adet satış → 400 (KASA'da 2 var); reyonId=KASA ile 2 adet satış → 200, KASA satırı silinmiş.
- reyonId'siz 5 adet satış → atanmamış 6'dan düşer, uyarı alanı yanıtta görünür.
- Şube-2'ye 3 adet transfer (hedefReyonId boş) → şube-2'de atanmamış 3.
- GRAM ürün: 7,5 g stok, 2,5 g VITRIN-A'ya yerleştir, 0,3 g taşı → ondalık 3 hanede bozulma yok.
- Reyon PATCH ile pasife alma denemesi → 400; malı boşalt → 200.
- /api/v1/raporlar/reyon-stok çıktısı SQL toplamıyla birebir karşılaştırılır.
- Yabancı kiracı jetonuyla /reyonlar/{id}/stok → 404.
- Doğrulama bitince REYON-TEST kiracısı silinir; dotnet test 0 başarısız + ./denetim.sh TEMİZ kanıtı rapora yazılır.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/UrunKategorisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Migrations/ (yeni migration + Designer + Snapshot)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Sayim.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/UrunAgaciEkranTestleri.cs (Reyonun_KART_BAZLI_oldugu_ekranda_yazar testi FAZ B'de güncellenmeli — satır 208)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/SayimOturumuTestleri.cs (FAZ B kapsam davranışı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/SayimEkranTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs`
- `DOKUNULMAZ (kanıtlı karar): Kuyumcu.Api/Modules/Maliyet/MaliyetMotoru.cs, Data/StokPartisi.cs, Data/HareketFabrikasi.cs, Program.cs`

## Mevzuat dayanağı
- VUK md.186 (envanter) — paket 23 A14 notunda aynı dayanak kullanıldı — **DOGRULANMALI** — Envanterin fiziki yerleşim birimine (vitrin/kasa) göre bölümlenmesini ZORUNLU kılan bir hüküm bu oturumda doğrulanmadı. Reyon bölümlemesi işletme içi kontrol amacıdır; mevzuat gerekçesiyle sunulmamalı.
- VUK md.253 (saklama) — sayım oturumunun silinmemesi kararının dayanağı — **DOGRULANMALI** — Reyon taşıma kaydının saklama süresine tabi bir 'belge' sayılıp sayılmadığı doğrulanmadı. Bu yüzden iz DenetimKaydi'na yazılır (ADR-0004 append-only) — silinemez, ama resmî belge iddiası da yapılmaz.
- Kuyum Ticareti Yönetmeliği (RG 24.06.2026/33290) — **DOGRULANMALI** — Reyon/vitrin bazlı stok tutma yükümlülüğü aranmadı/doğrulanmadı. Bu pakette hiçbir mevzuat kapısı (tevsik/MASAK) çağrılmaz; para el değiştirmez.
- Tevsik 30.000 TL nakit haddi · MASAK 185.000 TL eşiği (devir belgesi A7, doğrulanmış sayılır) — **DOGRULANDI** — Bu pakette UYGULANMAZ: reyon içi taşımada para ve borç doğmaz. Mevcut satış/alış akışlarındaki kapılar aynen korunur, yerleşim bacağı onların SONRASINA eklenir.

## Riskler
- EN BÜYÜK RİSK — SatisModulu.StokDegistir imzası değişiyor (satır 1594): POS, iade, takas, transfer hepsi bu fonksiyondan geçer. Aynı fonksiyona dokunan başka bir paket paralelde koşarsa çakışır. Seri yazma (şirket üretim yasası) zorunlu.
- I1 eşzamanlılık: iki satış aynı anda aynı yerleşim satırını düşürürse aşırı-düşüm olabilir. Mevcut kod `stoklar` için de aynı riski taşıyor (SatisModulu.cs:1600 desenli okuma-yazma); bu pakette transaction içinde SELECT ... FOR UPDATE ya da adet kolonuna CHECK yerine iyimser kontrol tercih edilmeli — hangisi seçilirse seçilsin MEVCUT davranışla tutarlı olmalı, yeni bir kilit felsefesi getirilmemeli.
- Geçiş verisi: yürürlükteki stokların HEPSİ ATANMAMIŞ olarak başlar. Kullanıcı 'reyonlarım boş göründü' der. Migration'da urunler.reyon_id'den otomatik yerleştirme YAPILMAMALI (ürün kartı şubeden bağımsız; hangi şubedeki hangi adet oraya gider bilinmiyor — uydurma olur). Ekranda 'atanmamış' açıkça gösterilerek dürüst kalınır.
- Sozlukler.cs:1938 kapsam uyarısı Faz A sonrasında yarı-doğru hâle gelir (yerleşim var ama sayım hâlâ kart bazlı). Faz A'da uyarı metni 'sayım kapsamı hâlâ ürün kartından okunur' diye DARALTILMALI, tamamen silinmemeli.
- Reyon içi taşımanın deftere yazılmaması bilinçli; denetçi 'stok hareketi neden defterde yok' diye sorabilir. Karar ve gerekçesi bir ADR'ye (ADR-0016 önerisi: 'Reyon yerleşimi alt-bölümlemedir, defter kaydı değildir') yazılmalı.
- RaporModulu.StokDegeri (satır 432) hâlâ sınırsız ToListAsync yapıyor (mevcut TB-19 borcu). Yeni rapor bu deseni KOPYALAMAMALI.

## Açık sorular
- (yok)

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — İki tablo kararı (stoklar gerçek kaynak + stok_yerlesimleri alt bölümleme) veri modeli açısından DOĞRU: stok anahtarını dörtlüye çıkarmak StokPartisi/FIFO-LIFO maliyet defterini de yeniden yazmayı gerektirirdi. 'Atanmamış saklanmaz, hesaplanır' kararı çift gerçek üretmiyor. Reyon içi taşımanın deftere yazılmaması doğru (para/has/adet değişmiyor) ve iz DenetimKaydi'na gidiyor.
  - Koşullar: denetim.sh iddiası YANLIŞ: dosyada BEKLENEN_VARLIK=15 (satır 66, spec 67 diyor) ve karşılaştırma '-ge' (satır 70). '31→32' diye mutlak sayı yazmak eşiği kırılgan yapar. Kural: paket kendi eklediği varlık kadar artırır, mevcut değeri okuyarak. · I1 EŞZAMANLILIK ÇÖZÜLMEDEN Faz A sevk edilemez: spec 'iyimser mi kilit mi' sorusunu açık bırakıyor ama Σ yerleşim ≤ stok değişmezi eşzamanlı iki satışta sessizce bozulur. Karar zorunlu ve MEVCUT stok davranışıyla aynı olmalı (yeni kilit felsefesi getirilmeyecek) — bu spec'in kendi kuralı, koşula çevriliyor. · SatisModulu.StokDegistir imza değişikliği (satır 1594) paket 19/20/31/46/48 ile çakışır; paket 25 bu dosyaya dokunan TEK paket olarak sıraya alınmalı. · Faz A sonrası Sozlukler.cs:1938 kapsam uyarısı SİLİNMEYECEK, DARALTILACAK ('yerleşim var, sayım kapsamı hâlâ ürün kartından'); mevcut UrunAgaciEkranTestleri.cs:208 testi de o yeni gerçeğe göre güncellenecek, silinmeyecek. · ADR zorunlu: 'reyon yerleşimi alt-bölümlemedir, defter kaydı değildir' kararı yazılı olmalı (spec öneriyor; numara için bkz. çapraz bulgu 1).
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Deftere yazmıyor, para el değiştirmiyor, tevsik/MASAK/vade çağrılmaması doğru. Kiracı izolasyonu IKiraciVarlik + elle TenantId ile kurulmuş. Yalnız yanlış denetim sayacı ve kilit disiplini koşul.
  - Koşullar: denetim.sh iddiası yanlış: BEKLENEN_VARLIK gerçekte 15 (denetim.sh:66), '31→32' değil. Sayı sevk sırasına göre TEK ELDEN artırılsın (bkz. çapraz bulgu). · I1 eşzamanlılık: yerleşim düşümü transaction içinde iyimser kilitle (guncellendi damgası) ya da SELECT ... FOR UPDATE — hangisi seçilirse ExecuteUpdate/ExecuteDelete KULLANILMAZ (beyaz liste 4'te kalır); mevcut stoklar deseniyle tutarlı. · PATCH /reyonlar/{id} sorumlu_kullanici_id kiracı doğrulaması (KategoriModulu.cs:212-217 deseni) + yabancı kiracının reyonu 404 (400 değil). · Reyon içi taşıma/atama/geri-al DenetimKaydi'na (adet, kaynak, hedef, kullanıcı) — spec böyle; mutasyon kanıtı korunsun. · Geri-al ucu SAHIP/MUDUR (spec böyle), TEZGAHTAR 403 HTTP testi.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Tasarım kararları kanıtlı ve mutasyonlar test adlarına bağlı; ancak denetim.sh sayısı yanlış, iki kabul ölçütü belirsiz (404/400, 'plan kanıtı') ve eşzamanlılık testi yok.
  - Koşullar: KARAR 8 'BEKLENEN_VARLIK 31→32' kod gerçeğiyle uyuşmuyor: denetim.sh:66 değeri 15. Düzeltilsin (15→16; ancak seri sıralamada önceki paketlerin artışı kümülatif sayılsın). · StokYerlesimiTestleri 'yabancı kiracının reyonuna yerleştirme → 404/400' tek kod olsun (öneri 404, varlık sızdırılmaz); HTTP testi olarak da eklensin. · ReyonRaporTestleri 'rapor sınırsız ToListAsync kullanmaz (sorgu sayısı/plan kanıtı)' ölçülebilir değil. Somut: kaynak tarama testi (RaporModulu.ReyonStok metni içinde 'ToListAsync' öncesi GroupBy) VEYA EF komut yakalayıcı ile üretilen SQL'de 'GROUP BY' aranması; biri seçilsin. · I1 eşzamanlılık: 'iki satış aynı anda aynı yerleşimi düşürür' senaryosu için en az bir DB testi (iki paralel transaction, ikincisi 400 ya da yeniden deneme) yazılsın; risk bölümünde kabul ediliyor ama test listesinde yok. · StokDegistir imzası değişince mevcut satış/iade/takas/transfer testlerinin (IadeTestleri, SatisFisiTestleri, TransferTestleri) 0 başarısızla geçmesi kabul ölçütü olarak yazılsın; 'mevcut aritmetik değişmez' iddiası ancak böyle kanıtlanır. · Faz A'da Sozlukler.cs:1938 KapsamUyarisi daraltma metni için bUnit testi (UrunAgaciEkranTestleri satır 208) güncellensin — spesifikasyon Faz B'de diyor ama Faz A'da metin yarı-doğru kalıyor.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- denetim.sh iddiası YANLIŞ (iki mercek birleşik): dosyada BEKLENEN_VARLIK=15 (denetim.sh:66, karşılaştırma '-ge' satır 70), '31→32' değil. Kural: mutlak sayı yazılmaz; paket mevcut değeri okuyarak kendi eklediği varlık kadar artırır (seri sırada kümülatif; paket 29'un 2→3 artışı da devralınır).
- I1 eşzamanlılık çözülmeden Faz A sevk edilemez: yerleşim düşümü transaction içinde iyimser kilit (guncellendi damgası) ya da SELECT ... FOR UPDATE ile; ExecuteUpdate/ExecuteDelete KULLANILMAZ (beyaz liste 4'te kalır) ve mevcut stoklar deseniyle (SatisModulu.cs:1600) tutarlı olur — yeni kilit felsefesi getirilmez.
- Eşzamanlılık testi zorunlu: 'iki paralel transaction aynı yerleşimi düşürür, ikincisi reddedilir/yeniden dener' en az bir DB testi (risk kabul edilmiş ama test listesinde yok).
- SatisModulu.StokDegistir imza değişikliği (satır 1594) POS/iade/takas/transferin tümünü etkiler; 25 bu dosyaya dokunan TEK paket olarak seri sıraya alınır (19/20/31/46/48 ile aynı anda değil).
- Regresyon kabul ölçütü: IadeTestleri, SatisFisiTestleri, TransferTestleri 0 başarısız — 'mevcut aritmetik değişmez' iddiası ancak böyle kanıtlanır.
- Geçiş verisi dürüstlüğü: tüm stoklar ATANMAMIŞ başlar; migration'da urunler.reyon_id'den otomatik yerleştirme YAPILMAZ (uydurma olur), ekranda 'atanmamış' açıkça gösterilir.
- Sozlukler.cs:1938 kapsam uyarısı Faz A'da SİLİNMEZ, DARALTILIR ('yerleşim var, sayım kapsamı hâlâ ürün kartından'); UrunAgaciEkranTestleri.cs:208 testi Faz A'da güncellenir (silinmez).
- Yetki/izolasyon: PATCH /reyonlar/{id} sorumlu_kullanici_id kiracı doğrulaması (KategoriModulu.cs:212-217 deseni); yabancı kiracının reyonu 404 (tek kod, 400 değil), HTTP testi de eklenir; geri-al ucu SAHIP/MUDUR, TEZGAHTAR 403 HTTP testi.
- Reyon içi taşıma/atama/geri-al DenetimKaydi'na (adet, kaynak, hedef, kullanıcı) yazılır; deftere yazılmaz ve bu karar ADR'ye bağlanır ('Reyon yerleşimi alt-bölümlemedir, defter kaydı değildir').
- 'Rapor sınırsız ToListAsync kullanmaz' ölçütü somutlaştırılır: ya kaynak tarama testi (GroupBy, ToListAsync öncesi) ya da EF komut yakalayıcıyla üretilen SQL'de GROUP BY aranması — biri seçilir; TB-19 deseni kopyalanmaz.
