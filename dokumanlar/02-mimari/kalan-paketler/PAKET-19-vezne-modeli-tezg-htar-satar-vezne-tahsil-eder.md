# Paket 19 — Vezne modeli — tezgâhtar satar, vezne tahsil eder

> Katman: Katman 2 — Satış/POS · Tahmin: Faz A 2 gün + Faz B 1-1,5 gün (PARITE tahmini 2-3 gün ile uyumlu) gün · Önkoşullar: 14, 16 · Kaynak: katman mimarı K2-19 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Perakende kuyumcuda tezgâhtar müşteriyle ilgilenir, para vezneden tahsil edilir. Bugün ürün bunu hiç bilmiyor: satışı yapan kişi aynı anda kasaya parayı da yazıyor, yani görev ayrılığı (dört göz) yok. Bu paket, tezgâhtarın hazırladığı ve veznenin tahsil ettiği bir 'bekleyen fiş' kuyruğu kurar; para el değiştirdiği an defter, kasa, tevsik ve MASAK kapıları bugünkü tek yoldan çalışır. Ayrıca kasa farkının kime sorulacağını netleştirir: tahsil eden kullanıcı hareketin sahibidir, hazırlayan fişte kayıtlı kalır.

## Kodda bugünkü durum
Kod tabanında 'vezne' geçen 0 satır var (grep -rn vezne, proje kökü — eşleşme yok). Satış uçları SatisModulu.cs:1716-1772'deki SatisController'dadır: sınıfta [Yetki.ModulYetkisi("islem")] (:1716) var, POST satis (:1722) ve POST satis-fisi (:1745) uçlarında [RolGerekli] YOKTUR — yani TEZGAHTAR rolü bugün tek başına satış yapıp kasaya para yazabiliyor. Alış/hurda/transfer uçlarında rol kapısı var (:1727, :1732, :1737). Kasa tarafı hazır: Kasa/KasaHareket varlıkları Entities.cs:419 ve :432; kasa yazımı SatisModulu.cs:1669-1691 KasaGuncelle ile işaretli yapılır; kasa doğrulama + gün kilidi IslemDogrulayici.KasaDogrula (Common/IslemDogrulayici.cs:67, 'an' parametresi verilince GunKapanisiKapisi çalışır); gün kapanışı KasaModulu.cs:177 ve kasa erişim kapatma (bakiyeli kasa kapatılamaz) Kuyumcu.Tests/ErisimKapatmaTestleri.cs:19-20'de sınanıyor. Çok bacaklı ödeme altyapısı hazır: hareketler.fis_id (Entities.cs:275 sınıfı içinde 'fis_id' kolonu), OdemeSepeti (Modules/Satis/OdemeSepeti.cs) ve SatisFisi ucu (SatisModulu.cs:1745). Kullanıcı rolleri KullaniciModulu.cs:37 regex'inde SAHIP|MUDUR|TEZGAHTAR|MUHASEBE; modül yetkileri YetkiModulu.cs:31-33 (TEZGAHTAR: cari, urun, islem, kasa, banka, fatura). Kiracı ayarı deseni Tenant varlığında var (Entities.cs: kur_kaynagi, maliyet_yontemi, ai_etkin) ve PATCH /api/v1/admin/ai (AdminModulu.cs:87, sınıf düzeyinde [RolGerekli("SAHIP")] :71) üzerinden ekranda yönetiliyor (Kuyumcu.Web/Pages/Isletme.razor:255).

## Kapsam
**İçinde:**
- Faz A: vezne_fisleri + vezne_fis_kalemleri tabloları, fiş açma/güncelleme/vezneleme/iptal/tahsil uçları, /vezne ekranı (kuyruk + tahsil paneli), tenants.vezne_zorunlu ayarı (varsayılan KAPALI) ve İşletme ekranında anahtarı
- Faz A: tahsil ucunun mevcut SatisServisi.SatisFisi yolunu çağırması; hareketlerin fis_id'sinin vezne fişine geri bağlanması
- Faz B: vezne_zorunlu=true iken doğrudan satış kapısı (R1) ve dört-göz kuralı (R2); Islemler.razor'da 'Kaydet' yerine 'Vezneye gönder' akışı
- Faz A+B: TR+EN sözlük, kenar çubuğu girişi, rota başlığı, bUnit ekran testleri, DB/HTTP testleri, mutasyon kanıtları, canlı doğrulama

**Dışında:**
- Stok rezervasyonu (bekleyen fiş stok tutmaz) — Faz C adayı, açık soruda
- Müşteri çağrı numarası / sıra ekranı / donanım (fiş yazıcısı, müşteri ekranı)
- Yeni rol (KASIYER/VEZNE) tanımlanması
- Alış, hurda alış, takas, işçilik, iade akışlarının vezneye taşınması — yalnız SATIŞ vezneye alınır
- Vezne kapanışı (paket 30 gün kapanışı zaten var, tekrarlanmaz)
- e-Fatura/e-Arşiv (ADR-0012), döviz alım-satımı (PAKET-15)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| tenants | Kiracı düzeyinde vezne ayrımı anahtarı — mevcut kur_kaynagi/maliyet_yontemi/ai_etkin deseniyle aynı | vezne_zorunlu BOOLEAN NOT NULL DEFAULT 0 | yok | YYYYMMDDHHMMSS_VezneModeli |
| vezne_fisleri | YENİ — tezgâhtarın hazırladığı, henüz deftere girmemiş satış fişi başlığı | id (36, PK, ULID) · tenant_id (36) · sube_id (36) · durum (10: ACIK|VEZNEDE|TAHSIL|IPTAL) · hazirlayan_kullanici_id (36) · tahsil_eden_kullanici_id (36, null) · cari_id (36, null) · aciklama (text, null) · kur_snapshot_id (36) [referans kur] · toplam_tl decimal(15,2) NOT NULL DEFAULT 0 [yalnız VEZNEDE'ye geçerken damgalanır] · hareket_fis_id (36, null) [tahsil sonucu] · iptal_gerekce (500, null) · olusturuldu · veznelendi (null) · tahsil_edildi (null) · iptal_edildi (null) | (tenant_id, sube_id, durum, olusturuldu) — vezne kuyruğu tek indeksten okunur; (tenant_id, hareket_fis_id) BENZERSİZ (null'lar hariç MySQL davranışı) — aynı fişin iki kez deftere yazılması veritabanı düzeyinde imkânsız | YYYYMMDDHHMMSS_VezneModeli |
| vezne_fis_kalemleri | YENİ — fişin ürün satırları | id (36, PK) · tenant_id (36) · fis_id (36) · urun_id (36) · adet decimal(15,3) [UrunBirim paketi sonrası adet ondalıklı olabilir — A15 kontrol edilmeli] · birim_fiyat_tl decimal(15,2) · iscilik_tl decimal(15,2) NOT NULL DEFAULT 0 · sira int · olusturuldu | (tenant_id, fis_id, sira) | YYYYMMDDHHMMSS_VezneModeli |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/vezne/fisler` | ModulYetkisi("islem") — TEZGAHTAR dahil | Tezgâhtar fiş açar: cariId?, kurSnapshotId, aciklama?, kalemler[] (urunId, adet, birimFiyatTl, iscilikTl?). Durum ACIK. Şube kullanıcının SubeId'sidir (SatisModulu.cs:1655 SubeBelirle deseni); gövdeden şube ALINMAZ. | K1 kur + cari doğrulaması (IslemDogrulayici.KurDogrula/CariDogrula); ürünlerin kiracıya aitliği; biçimsel kalem doğrulaması |
| PATCH | `/api/v1/vezne/fisler/{id}` | hazırlayan kullanıcı ya da MUDUR+ | Kalemleri/açıklamayı/cariyi değiştirir. YALNIZ durum=ACIK. Kalemler toptan değiştirilir (sil+yaz), kısmi yama yoktur. | R4: VEZNEDE/TAHSIL/IPTAL fişte 409 |
| POST | `/api/v1/vezne/fisler/{id}/veznele` | hazırlayan ya da MUDUR+ | ACIK → VEZNEDE. toplam_tl damgalanır (Σ adet×birimFiyat + işçilik), veznelendi=now. Bundan sonra fiş tezgâhtar tarafından değiştirilemez. | R7: en az 1 kalem; boş fiş vezneye gönderilemez |
| GET | `/api/v1/vezne/kuyruk` | ModulYetkisi("islem") | durum (varsayılan VEZNEDE), limit (1-200). Yalnız kullanıcının şubesi. Dönen alanlar: id, durum, toplamTl, cariAd, hazirlayanAd, olusturuldu, veznelendi, kalemSayisi. Sınırsız ToListAsync YOK (TB-19): limit zorunlu, sayım sunucuda. | R6: şube kapsamı sunucuda zorlanır |
| GET | `/api/v1/vezne/fisler/{id}` | ModulYetkisi("islem") | Fiş başlığı + kalemleri + (varsa) bağlı hareket fis_id'si. | kiracı + şube |
| POST | `/api/v1/vezne/fisler/{id}/tahsil` | ModulYetkisi("kasa") + (vezne_zorunlu ise hazırlayan olmayan kullanıcı) | Gövde: kurSnapshotId (TAHSİL anı kuru — zorunlu), bacaklar[] (OdemeSepeti şekli: tur NAKIT|KART|HAVALE|ALTIN|VERESIYE, tutar, kasaId?, cariId?), masak?, vadeGun?. Fiş kalemleri + bacaklar SatisFisiDto'ya çevrilir ve SatisServisi.SatisFisi(dto, islem) çağrılır. Dönen fisId, vezne fişine hareket_fis_id olarak yazılır; durum TAHSIL, tahsil_eden = çağıran. | R2 dört göz · R3 yalnız VEZNEDE · R6 şube · devamı SatisFisi içinde: tevsik, MASAK, K6, gün kilidi, vade/limit, taş kökeni, konsinye, stok |
| POST | `/api/v1/vezne/fisler/{id}/iptal` | hazırlayan ya da MUDUR+ | Gövde: gerekce (≥10 karakter). ACIK|VEZNEDE → IPTAL. Kayıt SİLİNMEZ (A9 yeniden açma kararıyla aynı gerekçe). | R5: TAHSIL fiş iptal edilemez — iade yolu paket 16'dadır |
| PATCH | `/api/v1/admin/vezne` | SAHIP (AdminController sınıf düzeyi [RolGerekli("SAHIP")], AdminModulu.cs:71) | Gövde: { zorunlu: bool }. tenants.vezne_zorunlu yazar. AdminModulu.cs:87 (PATCH /admin/ai) deseninin birebir ikizi. | — |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Tenant'a VezneZorunlu ([Column("vezne_zorunlu")] bool) eklenir. VezneFisi ve VezneFisKalemi sınıfları : IKiraciVarlik olarak eklenir; kolon adları snake_case, MaxLength'ler mevcut varlıklarla aynı (id/tenant_id/sube_id 36). Durum değerleri için ayrı sınıfta sabitler (VezneDurum.Acik/Veznede/Tahsil/Iptal) — sihirli dize yazılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — DbSet<VezneFisi> VezneFisleri ve DbSet<VezneFisKalemi> VezneFisKalemleri (:56-89 bloğuna); HasIndex satırları (:102-122 bloğuna) — (TenantId, SubeId, Durum, Olusturuldu), (TenantId, HareketFisId) IsUnique, (TenantId, FisId, Sira). Global kiracı filtresi yansıma döngüsüyle (:207) otomatik uygulanır; elle HasQueryFilter satırı eklenmez.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs` — DTO'lar (VezneFisOlusturDto, VezneKalemDto, VezneTahsilDto, VezneIptalDto) + VezneServisi + [Route("api/v1/vezne")][Authorize][Yetki.ModulYetkisi("islem")] VezneController. VezneServisi ctor: (KuyumcuDbContext db, ILogger<VezneServisi> logger, Satis.SatisServisi satis). Tahsil metodu: fişi oku → R2/R3/R6 → SatisFisiDto kur → satis.SatisFisi(dto, islem) → dönen fisId'yi fişe yaz, durum TAHSIL → SaveChanges. DİKKAT: SatisFisi kendi transaction'ını açar; vezne fişinin durum güncellemesi o çağrıdan SONRA ayrı SaveChanges ile yapılır ve (tenant_id, hareket_fis_id) benzersiz indeksi ile çift yazma engellenir (bkz. riskler).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Vezne/VezneKapisi.cs` — Saf sınıf, tek karar noktası: DogrudanSatisaIzinVar(string rol, bool vezneZorunlu) → vezneZorunlu && rol == "TEZGAHTAR" ise ForbiddenException; DortGozDogrula(string hazirlayanId, string tahsilEdenId, bool vezneZorunlu) → eşitse BadRequestException. Kapı ayrı sınıftadır ki mutasyonla tek satır bozulduğunda hangi testin kırılacağı belirli olsun (TevsikKapisi/GunKapanisiKapisi deseni).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — Faz B: Satis() (:407) ve SatisFisi() servis metotlarının başına VezneKapisi.DogrudanSatisaIzinVar(islem.Rol, await VezneZorunluMu(islem.TenantId)) çağrısı. Ayar okuması Maliyet.MaliyetMotoru.Yontem (MaliyetMotoru.cs:354) desenindeki tek kolonluk projeksiyonla yapılır. Vezne'den gelen çağrıda kapı ÇALIŞMAZ — vezne tahsili tanım gereği vezne yoludur; bunun için SatisFisi'ye (bool vezneUzerinden = false) parametresi eklenir, controller varsayılanı kullanır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs` — VezneToggleDto + PATCH /admin/vezne ucu + AdminServisi.VezneToggle — :87'deki AiToggle'ın birebir ikizi.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Vezne.VezneServisi>(); (:17 SatisServisi kaydının yanına).
- **YENI** `dotnet/Kuyumcu.Api/Data/Migrations/YYYYMMDDHHMMSS_VezneModeli.cs` — İki yeni tablo + tenants.vezne_zorunlu + üç indeks. Down: tabloları ve kolonu düşürür. Geri-doldurma YOK (yeni kavram, geçmiş veri yok). Migration ÖNCE dotnet build yeşilken üretilir; --no-build YASAK (§6 tuzağı).
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukVezne sınıfı (TR+EN) + Sozluk (:1024) içinde Vezne özelliği + SozlukNav'a (:50) Vezne girişi. Metinler: Kuyruk, Fişi aç, Vezneye gönder, Tahsil et, İptal, Gerekçe, Hazırlayan, Bekleyen fiş yok, 'Kendi hazırladığın fişi tahsil edemezsin', 'Vezne ayrımı zorunlu'.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Kasa grubuna (:34 civarı) <NavLink href="/vezne">💵 @Dil.T.Nav.Vezne</NavLink>.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs` — /vezne rotası için başlık kaydı — RotaBaslikTestleri.cs bunu zorlar.
- **YENI** `dotnet/Kuyumcu.Web/Pages/Vezne.razor` — Sol: kuyruk listesi (10 sn'de bir yenile ya da elle yenile düğmesi — SignalR BEKLENMEZ, A2'deki 'Yükleniyor'da kalma hatası); sağ: seçili fişin kalemleri + ödeme bacağı paneli. Bacak paneli KarmaOdemeEkranTestleri.cs'nin sınadığı Islemler.razor bacak bileşeninin aynı deseni (Kalan göstergesi + 'Kalanı ata'). Tahsil TEK POST ile gider. Razor özniteliğinde bloklu lambda YOK, adlandırılmış metot (§6 tuzağı).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Islemler.razor` — Faz B: vezne zorunluysa (GET /admin/tenant'tan okunan bayrak) satış gövdesinde 'Kaydet' yerine 'Vezneye gönder' düğmesi; POST /vezne/fisler + /veznele çağrılır ve kullanıcıya fişin kuyruğa düştüğü söylenir. Bayrak kapalıysa ekran bugünkü hâliyle kalır.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Isletme.razor` — :255'teki AI anahtarının yanına 'Vezne ayrımı zorunlu' anahtarı → PATCH /admin/vezne. Yalnız SAHİP görür.
- **DEGISTIR** `dotnet/denetim.sh` — BEKLENEN_VARLIK (:66) 15'ten 17'ye çıkarılır (eşik '-ge' olduğu için zorunlu değil ama eşik tutmayan tabloyu yakalamaz hâle gelir). Ayrıca yeni bir kural eklenir: Modules/Vezne altında 'HareketFabrikasi.Ekle' ve 'KasaHareketleri.Add' çağrısı SIFIR olmalı — vezne kendi defterini yazmaya kalkarsa CI kırılır.

## Ekranlar
- `/vezne` · `dotnet/Kuyumcu.Web/Pages/Vezne.razor` — Bekleyen fiş kuyruğu + seçili fişin kalemleri + çok bacaklı tahsil paneli + iptal (gerekçeli) (menü/yetki: Kenar çubuğu 'Kasa' grubunda; ModulYetkisi("kasa") olmayan kullanıcıda tahsil düğmesi pasif)
- `/islemler` · `dotnet/Kuyumcu.Web/Pages/Islemler.razor` — Faz B: vezne zorunluyken satış 'Vezneye gönder' ile biter, kasa seçimi gizlenir (menü/yetki: değişmez)
- `/isletme` · `dotnet/Kuyumcu.Web/Pages/Isletme.razor` — 'Vezne ayrımı zorunlu' anahtarı (menü/yetki: SAHIP)

## İş kuralları (fail-closed)
- R1 (Faz B, fail-closed): vezne_zorunlu=true iken TEZGAHTAR rolü POST /islemler/satis ve /islemler/satis-fisi çağıramaz → 403. İstisna yoktur; istisna bırakılsaydı ayrım ilk yoğun günde delinirdi.
- R2 (dört göz): vezne_zorunlu=true iken tahsil eden ≠ hazırlayan; eşitse 400. Ayar kapalıyken kural uygulanmaz (tek kişilik dükkân kilitlenmesin).
- R3 (tek yönlü durum): tahsil yalnız VEZNEDE'den yapılır; ACIK fiş tahsil edilemez, TAHSIL fiş ikinci kez tahsil edilemez. Yarış durumunda ikinci isteği (tenant_id, hareket_fis_id) benzersiz indeksi düşürür — uygulama kontrolü tek başına yeterli sayılmaz.
- R4 (fiyat kilidi): VEZNEDE fişin kalemleri ve fiyatları değiştirilemez → 409. Değiştirilebilseydi tezgâhtar, müşteri vezneye yürürken fiyat kırabilirdi ve kuyrukta görünen tutar ile tahsil edilen tutar ayrışırdı.
- R5 (iptal): gerekçe ≥10 karakter, zorunlu; TAHSİL edilmiş fiş iptal edilemez (iade paket 16 yoludur). İptal kaydı SİLİNMEZ.
- R6 (şube): kuyruk ve tahsil yalnız kullanıcının SubeId'si içindir; başka şubenin fişi 404 döner (varlığı sızdırmamak için 403 değil 404).
- R7 (biçim): fiş ≥1 kalem; adet>0; birimFiyatTl>0; iscilikTl≥0. Sıfır fiyatlı satış hediye değil hatadır (hediye yolu paket 17'de kampanyadadır).
- R8 (kur): fişteki kur_snapshot_id referanstır; deftere yazılan kur tahsil gövdesinde yeniden gönderilen kurdur. Fişin kuru kullanılsaydı sabah açılan fiş akşam eski kurla kapanırdı.
- R9 (rezervasyon yok): bekleyen fiş stok tutmaz. Tahsilde stok yetmezse SatisFisi 400 verir, fiş VEZNEDE kalır ve mesaj 'mal tükendi' der. Sessiz kısmi satış YOKTUR.
- R10 (tek yazma yolu): Vezne modülü hareketler ya da kasa_hareketleri tablosuna DOĞRUDAN yazmaz; yalnız SatisServisi.SatisFisi çağırır. denetim.sh bunu sayar.
- R11 (kullanıcı izi): deftere yazılan hareketin KullaniciId'si TAHSİL EDEN'dir (kasa sorumluluğu ondadır); hazırlayan yalnız vezne fişinde saklanır. İkisi de gerekli: kasa farkı tahsil edene, fiyat/indirim hazırlayana sorulur.

## Testler
- `dotnet/Kuyumcu.Tests/VezneTestleri.cs` [DB]
  - Fiş aç → veznele → tahsil et: hareketler tablosunda fis_id dolu kalemler oluşur, kasa bakiyesi bacak toplamı kadar artar, vezne fişi TAHSIL ve hareket_fis_id dolu
  - ACIK fiş tahsil edilemez (R3)
  - TAHSIL fiş ikinci kez tahsil edilemez (R3) — ikinci istek hata, defterde yeni satır YOK
  - VEZNEDE fişte kalem güncellenemez (R4)
  - vezne_zorunlu=true iken hazırlayan = tahsil eden → hata (R2); farklı kullanıcı → başarı
  - vezne_zorunlu=false iken aynı kullanıcı tahsil edebilir
  - Boş fiş veznelenemez (R7); gerekçesiz iptal reddedilir (R5); TAHSIL fiş iptal edilemez
  - Başka şubenin fişi kuyrukta görünmez ve tahsil edilemez (R6)
  - Kapanmış kasa-gününe tahsil → gün kilidi 400 (A9 kapısının vezne yolunda da çalıştığı kanıtı)
  - 30.000 TL üstü nakit bacaklı tahsil → tevsik 400 (kapı paritesi kanıtı)
  - Stok tükenmişse tahsil 400 ve fiş VEZNEDE kalır (R9)
  - Başka kiracının fiş id'siyle tahsil → 404 (kiracı izolasyonu)
- `dotnet/Kuyumcu.Tests/HttpVezneTestleri.cs` [HTTP]
  - TEZGAHTAR jetonuyla POST /vezne/fisler → 200; POST /vezne/fisler/{id}/tahsil → 403 (kasa yetkisi yoksa)
  - vezne_zorunlu=true iken TEZGAHTAR POST /islemler/satis → 403 (R1); MUDUR → 200
  - vezne_zorunlu=false iken TEZGAHTAR POST /islemler/satis → 200 (geriye uyumluluk kanıtı)
  - PATCH /admin/vezne yalnız SAHİP (MUDUR → 403)
- `dotnet/Kuyumcu.Tests/VezneEkranTestleri.cs` [bUnit]
  - Kuyruk boşken 'Bekleyen fiş yok' yazar, tahsil paneli çizilmez
  - Fiş seçilince kalemler ve toplam görünür; bacak toplamı fiş toplamına eşit değilken Tahsil düğmesi pasif
  - Tahsil TEK POST /vezne/fisler/{id}/tahsil isteği üretir (kalem başına istek YOK)
  - İptalde gerekçe boşken düğme pasif
  - Sahte API JSON'u camelCase; anonim yanıt alanları OrdinalIgnoreCase aranır (§6 tuzağı)
- `dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs` [bUnit]
  - /vezne rotasının başlığı tanımlı

## Mutasyon kanıtları
- M-V1: VezneKapisi.DogrudanSatisaIzinVar gövdesi boşaltılınca (kapı hiç çalışmaz) → HttpVezneTestleri 'TEZGAHTAR doğrudan satış 403' kırmızı
- M-V2: DortGozDogrula'daki eşitlik kontrolü kaldırılınca → VezneTestleri 'hazırlayan tahsil edemez' kırmızı
- M-V3: Tahsil metodunda durum kontrolü (durum==VEZNEDE) kaldırılınca → 'ACIK fiş tahsil edilemez' kırmızı
- M-V4: (tenant_id, hareket_fis_id) benzersiz indeksi IsUnique'ten çıkarılınca → 'ikinci tahsil defterde satır üretmez' kırmızı
- M-V5: PATCH ucundaki 'yalnız ACIK' kontrolü kaldırılınca → R4 testi kırmızı
- M-V6: Tahsilde SatisServisi.SatisFisi yerine doğrudan HareketFabrikasi.Ekle yazılınca → denetim.sh yeni kuralı ihlal (ve Hareketler.Add sayısı korunsa bile vezne kuralı) kırmızı
- M-V7: Kuyruk sorgusundaki şube filtresi kaldırılınca → R6 testi kırmızı
- M-V8: Tahsilde kurSnapshotId fişten alınınca (gövdeden değil) → 'tahsil anı kuru ile yazılır' testi kırmızı
- M-V9: hareketin KullaniciId'si hazırlayan yazılınca → R11 testi kırmızı
- M-V10: toplam_tl vezneleme yerine tahsilde damgalanınca → 'kuyrukta görünen tutar tahsil edilenle aynıdır' testi kırmızı

## Canlı doğrulama
- API'yi --urls http://localhost:5199 ile başlat; test kiracısı aç; kur kaydı oluşana kadar GET /kur/guncel'i bekle (yeni kiracıda 10 sn, §6 tuzağı) — id boşsa DUR
- İki kullanıcı oluştur: TEZGAHTAR (t1) ve MUDUR (m1); iki jeton al
- t1 ile POST /vezne/fisler (1 ürün) → 200, id camelCase 'id' alanından perl ile çekilir (sed [0-9.] tuzağı yok)
- t1 ile /veznele → 200, durum VEZNEDE, toplamTl damgalı
- t1 ile /tahsil → 403 (kasa yetkisi yok) — beklenen
- m1 ile /tahsil (nakit+kart karma bacak) → 200; GET /kasalar/{id}/hareketler ile kasaya giren tutar doğrulanır; GET /islemler ile fis_id'li iki hareket satırı görülür
- Aynı fişe ikinci /tahsil → 409/400 ve defterde YENİ SATIR YOK (sayarak doğrula)
- PATCH /admin/vezne {zorunlu:true} → t1 ile POST /islemler/satis → 403; m1 ile → 200
- Kapanmış kasa gününe tahsil dene → 400 (gün kilidi vezne yolunda da çalışıyor)
- 30.001 TL nakit bacaklı tahsil → 400 (tevsik); aynı tutar HAVALE → 200
- Doğrulama sonrası API'yi DURDUR (kur zamanlayıcısı yazmaya devam eder), sonra test kiracısını sil (${=LISTE} ile)

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/ (yeni migration + güncellenen KuyumcuDbContextModelSnapshot.cs)`
- `NOT: UstBar.razor ve Program.cs'ye dokunulmaz (controller keşfi otomatik, üst barda yeni öge yok) — okundu/doğrulandı: KenarCubugu.razor menü tek yerdir`

## Mevzuat dayanağı
- 459 s. VUK Genel Tebliği — tevsik zorunluluğu (30.000 TL nakit haddi ve işlem tutarı kuralı) — **DOGRULANDI** — Devir belgesi A7 ile doğrulanmış sayılıyor. Vezne paketi yeni bir mevzuat kuralı GETİRMEZ; yalnız mevcut TevsikKapisi'nin tahsil anında çalışmasını sağlar. Nakit haddi fişin nakit bacakları toplamı üzerinden, SatisFisi içinde TEK KEZ uygulanır (B-3).
- MASAK — 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — A7 araştırması. Vezne yolunda MasakKapisi.DogrulaKumulatif fiş toplamı üzerinden çalışır; kimlik tespiti VEZNEDE değil TAHSİL anında istenir — para el değiştiren an odur.
- Görev ayrılığı / dört göz ilkesi — **VARSAYIM** — Perakende kuyumcu için görev ayrılığını ZORUNLU kılan bir Türk mevzuat hükmü bu oturumda ARAŞTIRILMADI. Bu yüzden R1/R2 yasal zorunluluk olarak değil, kiracı ayarı (varsayılan KAPALI) olarak tasarlandı; koda 'yasa gereği' diye bir kural KONMUYOR.

## Riskler
- TAHSİL İŞLEMİNİN ATOMLUĞU: SatisServisi.SatisFisi kendi transaction'ını açıp commit eder (SatisModulu.cs:457 deseni). Vezne fişinin TAHSIL'e çevrilmesi o commit'ten sonra ayrı bir SaveChanges olur. Arada süreç düşerse defterde satır vardır, fiş VEZNEDE kalır ve ikinci tahsil DENENEBİLİR. Azaltma: (a) (tenant_id, hareket_fis_id) benzersiz indeksi ikinci yazmayı düşürür, (b) tahsil öncesi 'bu fiş için fis_id'li hareket var mı' kontrolü, (c) kuyrukta 'defterde karşılığı var, mutabakat gerekli' uyarısı. Tam atomluk için SatisFisi'nin dış transaction kabul edecek şekilde genişletilmesi gerekir — ayrı ve daha büyük bir iştir, açık soruda.
- PERFORMANS/YARIŞ: aynı fişe iki vezne aynı anda basarsa (b) kontrolü TOCTOU'dur; asıl koruma benzersiz indekstir. Test M-V4 bunu kanıtlar.
- STOK REZERVE EDİLMEDİĞİ İÇİN müşteri veznede beklerken aynı mal başka tezgâhtar tarafından satılabilir. Perakendede tek parça mal yaygındır; bu gerçek bir kullanıcı şikâyeti üretir. Bilinçli kabul: rezervasyon stok defterini kirletir ve iptal edilmeyen rezervasyonlar malı görünmez kılar. Faz C'ye bırakıldı.
- KULLANICI DENEYİMİ RİSKİ: kuyruk yoklaması (polling) ile çalışır; SignalR'a bağlanmak A2'deki 'Yükleniyor'da kalma hatasını geri getirebilir — push kullanılacaksa ateşle-unut olmalı.
- SatisModulu.cs paralelde başka paketlerce de değiştirilen büyük dosyadır (1.772 satır); birleştirme çakışması riski yüksek — yaz-seri kuralı burada özellikle geçerli.

## Açık sorular
- Fişe insan-okur SIRA/ÇAĞRI NUMARASI konacak mı? (tenant+şube+gün bazlı sayaç eşzamanlılık kilidi ister; ULID öneki zaman damgasıdır ve aynı ms'te çakışır — §6 tuzağı.) Şimdilik yok.
- Tahsil işleminin tam atomluğu için SatisServisi.SatisFisi dış transaction kabul edecek şekilde genişletilsin mi? (Mevcut kodu değiştirmek gerekir — izin gerektirir.)
- Stok rezervasyonu Faz C olarak açılsın mı; açılırsa rezervasyonun ömrü (ör. 30 dk) ve otomatik düşürme zamanlanmış iş gerektirir (paket 6 çatısıyla ilişkili).
- adet alanının ondalık hassasiyeti: A15 'çift birim stok' paketinden sonra Stok/SatisOlusturDto'da adetin tipi (int mi decimal mi) OKUNMADI; vezne_fis_kalemleri.adet tipi o tiple BİREBİR aynı olmalı.
- Vezne yetkisi 'kasa' modül yetkisiyle mi (önerilen) yoksa kasa bazlı atama ile mi (hangi kullanıcı hangi kasadan tahsil eder) verilmeli? İkincisi çok vezneli büyük dükkân içindir, tek dükkân + 2-3 şube kapsamında gereksiz görüldü.
- vezne_zorunlu ayarı kiracı düzeyinde mi şube düzeyinde mi olmalı? Merkez şubede vezne varken küçük şubede olmayabilir. Kiracı düzeyi seçildi (paket 46 şube görünürlüğü geldiğinde şubeye indirilebilir).

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Mimari çekirdek doğru: vezne yeni bir ödeme yolu değil, mevcut SatisServisi.SatisFisi'nin önüne konan kuyruk; deftere yazma tekeli ve tüm kapılar tek yerde kalıyor. İki tablo + IKiraciVarlik + bileşik indeks kararı değişmezlere uygun. Sevk edilebilir boyutta (Faz A/B ayrımı gerçek).
  - Koşullar: vezne_fis_kalemleri.adet tipi decimal(15,3) DEĞİL decimal(12,3) olmalı — stoklar.adet Entities.cs:269'da decimal(12,3); iki ölçek ayrışırsa tahsilde sessiz yuvarlama olur. Spec'in 'A15 kontrol edilmeli' açık sorusu bu kanıtla KAPANIR. · Tahsil atomluğu Faz A'da çözülmeli: (tenant_id, hareket_fis_id) UNIQUE indeksi MySQL'de NULL'ları engellemez, yani çift yazmayı TEK BAŞINA durdurmaz. Zorunlu ek: tahsil öncesi 'bu fiş için fis_id'li hareket var mı' kontrolü + tahsil sırasında vezne fişi satırının SELECT ... FOR UPDATE ile kilitlenmesi. M-V4 mutasyonu bu hâliyle yanlış güven verir. · tenants.vezne_zorunlu kolonu REDDEDİLDİ: Tenant IKiraciVarlik DEĞİL, global filtre ve KiraciYazmaDenetcisi ona işlemez, denetim izi düşmez (paket 5 aynı gerekçeyle Tenant'a kolon eklemeyi reddetmiş — iki paket aynı tabloya zıt karar veremez). Ayar ya paket 5'in isletme_kunyesi deseninde ayrı IKiraciVarlik tabloya ya da paket 46'nın kullanici_yetkileri/kiracı ayar tablosuna konur. · R1 kapısı (VezneKapisi.DogrudanSatisaIzinVar) SatisServisi içinde çağrılmalı, controller'da değil: satis ve satis-fisi uçlarında [RolGerekli] yok (SatisModulu.cs:1722/1745 doğrulandı), servis dışı her kapı atlanır. · denetim.sh'a mutlak sayı (15→17) yazılmasın; bkz. çapraz bulgu 3. · Paket 46 ile AYNI DALGADA sevk edilemez: 46 KASIYER rolünü ekliyor, 19 'rol eklenmez, kiracı ayarı + dört göz' kararını onun yokluğuna dayandırıyor.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Deftere yazma tekelini korumak, kapıları SatisServisi.SatisFisi'den geçirmek ve varsayılan-kapalı ayar doğru tasarım. Kod gerçeği (SatisModulu.cs:1722/1744 RolGerekli yok) paketin gerekçesini doğruluyor. Kalan riskler yetki niteliği katmanlaması, yarış kilidi ve bayrağın dışarıdan sızması — hepsi koşulla kapanır.
  - Koşullar: Tahsil ucu için 'ModulYetkisi("kasa")' metot düzeyinde, sınıf düzeyi ModulYetkisi("islem") ile ÜST ÜSTE bindirilmesin — ModulYetkisiAttribute'un metot düzeyinde geçersiz kıldığı DOĞRULANMADI. Tahsil/İptal/PATCH uçları ayrı bir controller'da (VezneTahsilController, [ModulYetkisi("kasa")]) ya da servis içinde elle YetkiServisi.Erisebilir("kasa") kontrolü + HTTP testi 'kasa yetkisi olmayan TEZGAHTAR tahsil → 403'. · VezneKapisi.DogrudanSatisaIzinVar fail-closed yazılsın: 'rol == TEZGAHTAR ise yasak' DEĞİL, 'rol ∉ {SAHIP, MUDUR} ise yasak'. Paket 46 KASIYER/CIRAK ekleyince mevcut yazım sessizce delinir. · 'vezneUzerinden' bayrağı DTO'da/HTTP gövdesinde OLMASIN; yalnız servis-servis çağrısında iç parametre. Test: gövdede vezneUzerinden=true gönderen TEZGAHTAR yine 403. · R3 yarışı için (tenant_id, hareket_fis_id) UNIQUE yeterli değil: tahsil isteği fişi transaction içinde okuyup durumu VEZNEDE→TAHSIL'e iyimser kilitle (guncellendi damgası WHERE koşulu, ExecuteUpdate KULLANMADAN — SaveChanges + DbUpdateConcurrencyException) çevirsin; ikinci istek 409. M-V4 bu kilidi de kapsasın. · Vezne fişi PATCH/veznele/iptal uçlarında 'hazırlayan ya da MUDUR+' kontrolü SUNUCUDA (servis içinde islem.Id == fis.HazirlayanKullaniciId || Seviye>=MUDUR) — ekran gizlemesi kapı değildir; test 'başka tezgâhtarın fişini PATCH → 403'. · İptal ve tahsil, DenetimServisi.YazmaKaydet ile gerekçeli iz düşürsün (fiş kimliği + hazırlayan + tahsil eden); iptal edilen fiş SİLİNMEZ (spec böyle, testle zorlansın). · Kuyruk ucu yanıtında cariAd dışında kişisel veri (telefon/TCKN) dönmesin; yabancı kiracı/şube fişi 404 (R6 aynen).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Test kapsamı geniş ve mutasyon listesi test adlarıyla büyük ölçüde eşleşiyor; ancak bir HTTP senaryosu kod gerçeğiyle çelişiyor (TEZGAHTAR 'kasa' yetkisine sahip) ve M-V4 çok katmanlı korumanın arkasında ölçülemez. Atomluk riski spesifikasyonda dürüstçe yazılmış ama testi yok.
  - Koşullar: HttpVezneTestleri 'TEZGAHTAR /tahsil → 403 (kasa yetkisi yoksa)' senaryosu ve canlı doğrulama 5. adımı düzeltilsin: YetkiModulu.cs:33'te TEZGAHTAR varsayılanı 'kasa' İÇERİR, dolayısıyla varsayılan kiracıda 200 döner. Test, önce PUT /yetkiler ile TEZGAHTAR'dan 'kasa' modülünü kaldırıp SONRA 403 beklemeli; ayrıca 'kasa yetkisi olan TEZGAHTAR, vezne_zorunlu=false iken tahsil edebilir → 200' senaryosu eklenmeli. · M-V4 (benzersiz indeks) tek başına ölçülemez: uygulama R3 durum kontrolü aynı isteği zaten 409 ile keser, indeks kaldırılsa test yeşil kalır. Ya M-V3+M-V4 birlikte uygulanıp 'defterde ikinci fis_id'li satır yok' SQL sayımıyla doğrulansın, ya da test doğrudan db üzerinden ikinci VezneFisi.HareketFisId ataması yapıp DbUpdateException beklesin — hangi katmanın ölçüldüğü test adında yazsın. · Tahsil atomluğu riski (SatisFisi commit sonrası ayrı SaveChanges) için test şart: 'SatisFisi başarılı, fiş güncellemesi patlarsa (sahte hata enjeksiyonu) ikinci tahsil denemesi 409 verir ve defterde tek fiş kalır'. Bu test yoksa 'çift yazma engellenir' iddiası kanıtsız. · Vezne.razor kuyruk yoklaması (10 sn) bUnit testinde zamanlayıcıyla değil 'elle yenile' düğmesiyle tetiklenmeli; yoklama zamanlayıcısı testte devre dışı bırakılmalı (kararsızlık). · VezneTestleri 'Başka kiracının fiş id'siyle tahsil → 404' HttpVezneTestleri'ne de eklensin; DB testi kiracı filtresini, HTTP testi uç davranışını ölçer. · Canlı doğrulama 7. adımı 'ikinci tahsil → 409/400' belirsiz; tek kod (409) seçilsin ve SQL: SELECT COUNT(*) FROM hareketler WHERE fis_id=@f eşitliği yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Yetki katmanı: tahsil/iptal/PATCH uçları ayrı controller ([ModulYetkisi("kasa")]) ya da servis içinde YetkiServisi.Erisebilir("kasa"); metot düzeyi geçersiz kılma DOĞRULANMADI. R1 kapısı (VezneKapisi.DogrudanSatisaIzinVar) SatisServisi içinde çağrılır — SatisModulu.cs:1722/1745'te [RolGerekli] yok.
- Kapı fail-closed yazılır: 'rol ∉ {SAHIP, MUDUR} ise yasak' (TEZGAHTAR negatifi değil); paket 46 KASIYER/CIRAK eklerse delinmesin. 19 ile 46 aynı dalgada sevk edilemez.
- Yarış/atomluk: (tenant_id, hareket_fis_id) UNIQUE tek başına yetmez (MySQL NULL). Zorunlu: tahsil öncesi fis_id'li hareket kontrolü + fiş satırında iyimser kilit (guncellendi damgası, SaveChanges + DbUpdateConcurrencyException; ExecuteUpdate YOK); ikinci istek 409.
- vezne_fis_kalemleri.adet = decimal(12,3) (stoklar.adet Entities.cs:269 ile birebir); A15 açık sorusu bu kanıtla kapanır.
- tenants.vezne_zorunlu kolonu RED (Tenant IKiraciVarlik değil, global filtre/KiraciYazmaDenetcisi işlemez; paket 5 aynı kararı verdi). Ayar ayrı IKiraciVarlik kiracı-ayar tablosunda tutulur.
- 'vezneUzerinden' bayrağı DTO/HTTP gövdesinde OLMAZ, yalnız servis-içi parametre; test: gövdede true gönderen TEZGAHTAR yine 403.
- Sahiplik kontrolü sunucuda: fis.HazirlayanKullaniciId == islem.Id || Seviye>=MUDUR; 'başka tezgâhtarın fişini PATCH → 403' testi. Tahsil/iptal DenetimServisi.YazmaKaydet ile gerekçeli iz, fiş SİLİNMEZ.
- QA düzeltmesi: 'TEZGAHTAR /tahsil → 403' senaryosu kod gerçeğiyle çelişiyor — YetkiModulu.cs:33 TEZGAHTAR varsayılanı 'kasa' İÇERİR; test önce PUT /yetkiler ile 'kasa'yı kaldırır, ayrıca 'kasa yetkili TEZGAHTAR + vezne_zorunlu=false → 200' eklenir.
- M-V4 ölçülebilir olmalı: M-V3+M-V4 birlikte ya da doğrudan DB'den ikinci HareketFisId ataması → DbUpdateException; ayrıca 'SatisFisi başarılı + fiş güncellemesi hata enjeksiyonu → ikinci tahsil 409, defterde tek fiş' testi. Canlı adım 7 tek kod: 409 + SELECT COUNT(*) FROM hareketler WHERE fis_id=@f.
- Kuyruk yanıtında cariAd dışında kişisel veri yok; yabancı kiracı/şube fişi 404 (hem DB hem HTTP testi). Vezne.razor yoklaması bUnit'te zamanlayıcısız, 'elle yenile' ile tetiklenir. denetim.sh'a mutlak sayı yazılmaz, mevcut değer okunarak artırılır.
