# Paket 6 — Bildirim kanalı + zamanlanmış iş çatısı

> Katman: Katman 0 — Altyapı (paket 6: Bildirim kanalı + zamanlanmış iş çatısı) · Tahmin: 3–4 gün (Faz A 2–2,5 gün: iki tablo + çatı + üç iş + uçlar + /bildirimler ekranı, tek başına sevk edilebilir · Faz B 1–1,5 gün: TEVSIK_KURTARMA (kapalı), iş çalışmaları ekranı, WhatsApp derin bağlantısı) gün · Önkoşullar: yok · Kaynak: katman mimarı K0-6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 2

## Hedef
Perakende kuyumcunun kaçırdığı süreleri ürün hatırlatsın: geciken veresiye müşterisi, MASAK şüpheli işlem bildiriminin 10 iş günlük son tarihi ve saklama süresi dolmuş kişisel veri, bugün hiçbir yerde kendiliğinden görünmüyor — kullanıcı ilgili raporu AÇMADIKÇA öğrenemiyor. Bu paket iki tablo (is_calismalari + bildirimler), bir zamanlanmış iş çatısı ve bir bildirim ekranı kurar; üstüne binecek işler (paket 28 vade, TB-16 tevsik kurtarma, ADR-0008 saklama önizlemesi, ileride paket 3 sipariş termini) tek arayüzü uygulayarak eklenir. Dış kanal (e-posta/SMS/WhatsApp gönderimi) sözleşme gerektirdiği için yalnız arayüz olarak bırakılır.

## Kodda bugünkü durum
Tek zamanlanmış iş var: Program.cs:205 AddHostedService<KurZamanlayici>; KurModulu.cs:955-1075 BackgroundService olarak kur çeker, günde bir kez eski kurları temizler ve kiracılar arasında SistemKapsami ile gezer (Data/SistemKapsami.cs:17-23 bu üç kullanımı 'izin verilen tam liste' olarak sayar). Genel bir iş çatısı, iş çalışma kaydı ve kullanıcıya görünen bildirim kavramı YOK: Data klasöründe bildirim/is_calismasi varlığı yok (KuyumcuDbContext.cs:60-89 DbSet listesi), Kuyumcu.Web/Layout/KenarCubugu.razor'da bildirim menüsü yok, UstBar.razor'da yalnız canlı kur bandı var. Hatırlatmanın veri kaynakları HAZIR: VadeKapisi.AcikBorclar (Vade/VadeKapisi.cs) ve RaporModulu.cs:375 vade-takibi raporu geciken/yaklaşan ayrımını zaten yapıyor; MasakSupheliBildirim.SonBildirimTarihi (MasakVarliklari.cs:186-189) '10 iş günü, resmî tatiller hariç' hesaplanmış olarak duruyor ve (TenantId, Durum, SonBildirimTarihi) indeksi var (KuyumcuDbContext.cs:186); SaklamaSuresiIsi.Onizleme (Masak/SaklamaSuresiIsi.cs:35-60) kaç kaydın süresi dolduğunu döndürüyor. Hiçbirini okuyan bir iş yok. TB-16 (tevsik 5 iş günü kendiliğinden bildirim) için kodda 0 isabet (teknik-borc-envanteri.md:277-283). ⚠ Test tuzağı: HttpOrtami.cs:78 s.RemoveAll<IHostedService>() — HTTP testlerinde hiçbir zamanlayıcı koşmaz.

## Kapsam
**İçinde:**
- is_calismalari + bildirimler tabloları, varlıkları, indeksleri ve tek migration
- IZamanlanmisIs arayüzü + IsCalistirici (kiracı kapsamında koşan, testlenebilir saf servis) + IsZamanlayici (BackgroundService, yalnız uyandırma/gezinme)
- Faz A işleri: VADE_HATIRLATMA (paket 28), MASAK_SIB_SURESI, SAKLAMA_ONIZLEME (ADR-0008, yalnız Önizleme)
- Bildirim üretiminde tekillik anahtarı ile tekrarsızlık; iş çalışmasında günlük çalışma anahtarı ile tek koşum
- GET/POST bildirim uçları + elle iş tetikleme ucu + iş çalışma geçmişi ucu
- /bildirimler ekranı, UstBar'da açık bildirim rozeti, 'bildirim' modül yetkisi
- IBildirimKanali arayüzü + tek uygulama UygulamaIciKanal (DI'da tek kayıt, testle zorlanır)
- Faz B: TEVSIK_KURTARMA işi (varsayılan KAPALI), /bildirimler/is-calismalari ekranı (SAHIP), bildirim kartında WhatsApp derin bağlantısı (cari telefonu varsa)

**Dışında:**
- E-posta/SMS/WhatsApp GÖNDERİMİ (PARITE C sınıfı — sözleşme gerektirir; yalnız arayüz bırakılır)
- Kullanıcı tanımlı uyarı kuralı motoru / kural DSL'i (paket 5'in DSL yasağıyla aynı gerekçe: mutasyon kanıtı kurulamaz)
- Otomatik rapor e-postalama, zamanlanmış rapor üretimi
- ADR-0008 Uygula()'nın otomatikleştirilmesi (ADR revize edilmez)
- Dağıtık iş kuyruğu / Hangfire-Quartz gibi dış kütüphane (tek örnekli dağıtımda gereksiz bağımlılık)
- Paket 3 (sipariş termini) işi — arayüz hazırdır, iş o pakette yazılır
- Push bildirim / tarayıcı Notification API (paket 44 PWA kapsamı)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| is_calismalari | YENİ. Varlık: Data/ZamanlanmisIs.cs içinde `IsCalismasi : IKiraciVarlik`. | id CHAR(36) PK · tenant_id CHAR(36) · is_kodu VARCHAR(40) · calisma_anahtari VARCHAR(40) (yerel gün ya da 'gg.aa.yyyy-Ehhmm' elle koşum) · baslangic DATETIME(UTC) · bitis DATETIME NULL · durum VARCHAR(20) (BASLADI|BASARILI|HATALI) · uretilen_bildirim INT · incelenen_kayit INT · sure_ms INT NULL · hata_ozeti VARCHAR(500) NULL · tetikleyen_kullanici_id CHAR(36) NULL (elle koşumda dolu) | UNIQUE (tenant_id, is_kodu, calisma_anahtari) — aynı gün ikinci koşumu VERİTABANI reddeder · (tenant_id, baslangic) liste ucu için | YYYYMMDDHHMMSS_BildirimVeIsCatisi (tek migration, iki tablo) |
| bildirimler | YENİ. Varlık: Data/ZamanlanmisIs.cs içinde `Bildirim : IKiraciVarlik`. | id CHAR(36) PK · tenant_id CHAR(36) · tur VARCHAR(40) (iş kodu) · onem VARCHAR(10) (BILGI|UYARI|KRITIK) · baslik VARCHAR(200) · govde VARCHAR(1000) · hedef_rota VARCHAR(200) NULL · gorunur_roller VARCHAR(100) (virgüllü; boşsa modül yetkisi neyse o) · ilgili_varlik_turu VARCHAR(30) NULL · ilgili_varlik_id CHAR(36) NULL · tekillik_anahtari VARCHAR(160) · is_calismasi_id CHAR(36) NULL · olusturuldu DATETIME(UTC) · kapatildi DATETIME NULL · kapatan_kullanici_id CHAR(36) NULL · kapatma_notu VARCHAR(500) NULL | UNIQUE (tenant_id, tekillik_anahtari) — tekrar bildirim üretimini veritabanı engeller · (tenant_id, kapatildi, olusturuldu) liste + rozet sayımı · (tenant_id, ilgili_varlik_turu, ilgili_varlik_id) | aynı migration |
| KuyumcuDbContext | DbSet<IsCalismasi> IsCalismalari ve DbSet<Bildirim> Bildirimler eklenir (dosya:satır 60-89 bloğunun sonuna); OnModelCreating'e yukarıdaki 5 indeks yazılır (mevcut desen: satır 128-131 Belge indeksleri). | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/bildirimler?durum=ACIK|KAPALI|TUMU&limit=100` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Açık bildirimler, önem sırası sonra tarih tersine. limit sunucuda Math.Clamp(limit,1,100) ile sıkıştırılır (DataAnnotations yalnız controller yolunda çalışır — servis içinde elle sıkıştırma da yapılır). | [ModulYetkisi("bildirim")] + gorunur_roller süzmesi sunucuda |
| GET | `/api/v1/bildirimler/sayi` | tümü (modül yetkisi olan) | Açık bildirim sayısı ve en yüksek önem düzeyi (UstBar rozeti). CountAsync — liste çekilmez. | [ModulYetkisi("bildirim")] |
| POST | `/api/v1/bildirimler/{id}/kapat` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Bildirimi kapatır (kapatildi + kapatan_kullanici_id + kapatma_notu). Satır SİLİNMEZ. Zaten kapalıysa 400. | [RolGerekli("SAHIP","MUDUR","MUHASEBE","TEZGAHTAR")] + DenetimServisi.YazmaKaydet |
| GET | `/api/v1/isler` | SAHIP, MUDUR | Tanımlı işler (kod, ad, etkin mi, son çalışma zamanı/durumu) + son 50 çalışma kaydı. | [RolGerekli("SAHIP","MUDUR")] + [ModulYetkisi("bildirim")] |
| POST | `/api/v1/isler/{kod}/calistir` | SAHIP, MUDUR | İşi kendi kiracısı için ŞİMDİ koşturur (elle tetikleme). Çalışma anahtarı 'yyyy-MM-dd-Ehhmm' olduğu için günlük anahtara çakışmaz; yine de bildirim tekillik anahtarı aynı olduğundan tekrar bildirim ÜRETİLMEZ. Bilinmeyen kod → 404. Oran sınırı: dakikada 3. | [RolGerekli("SAHIP","MUDUR")] + DenetimServisi.YazmaKaydet("IS_ELLE_CALISTIRMA") |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Data/ZamanlanmisIs.cs` — IsCalismasi ve Bildirim varlıkları + IsDurum/BildirimOnem sabit sınıfları. Entities.cs'e DOKUNULMAZ (GunKapanisi.cs deseni) — paket 1/38 ile çakışmayı önler.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/IZamanlanmisIs.cs` — public interface IZamanlanmisIs { string Kod; string Ad; bool Etkin(IConfiguration y); string CalismaAnahtari(DateOnly yerelGun); Task<IReadOnlyList<BildirimTaslagi>> Calistir(KuyumcuDbContext db, string tenantId, DateTime anUtc, CancellationToken iptal); } + readonly record struct BildirimTaslagi(string Tur, string Onem, string Baslik, string Govde, string? HedefRota, string? IlgiliVarlikTuru, string? IlgiliVarlikId, string TekillikAnahtari, string? GorunurRoller). İşler DbContext'e YAZMAZ, taslak döndürür — yazma tek yerde (IsCalistirici).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/IsCalistirici.cs` — Scoped servis. (a) UNIQUE anahtarla is_calismalari'na BASLADI satırı ekleyip SaveChanges ile 'hak talep eder'; DbUpdateException (duplicate key) gelirse iş o gün ZATEN koşmuştur → ATLANIR (fail-closed, sessiz). (b) İşi çağırır, dönen taslakları bildirimler'e ekler; tekillik anahtarı çakışanı tek tek yakalar ve sayar (kayıt başına ayrı SaveChanges ya da önce mevcut anahtarların okunup süzülmesi + yine de çakışmayı yutan try/catch). (c) Çalışma satırını BASARILI/HATALI olarak tamamlar. Hata TÜM turu düşürmez; hata_ozeti'ne yazılır (≤500 karakter, sır/parola yazılmaz).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/IsZamanlayici.cs` — BackgroundService. Isler:TurDakika (varsayılan 15, 1-1440 arasına sıkıştırılır) periyodunda uyanır; Isler:Etkin=false ise hiç koşmaz. SistemKapsami.Sistem() ile aktif kiracıları okur (KurZamanlayici.KurGuncelle deseni, KurModulu.cs:1010-1020), her kiracı için SistemKapsami.Kiraci(id) kapsamında IsCalistirici'yi çağırır. İş MANTIĞI BURADA YOKTUR (K0-5).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/Isler/VadeHatirlatmaIsi.cs` — Kod VADE_HATIRLATMA. VadeKapisi.AcikBorclar ile cari başına geciken/yaklaşan açık borcu okur (RaporModulu.cs:361-395 ile aynı kaynak, kod kopyalanmaz — ortak yardımcı RaporModulu'ndan çıkarılmaz, VadeKapisi doğrudan çağrılır). Ufuk: Isler:Vade:UfukGun (varsayılan 7). Geciken varsa KRITIK, yalnız yaklaşan varsa UYARI. Tekillik: 'VADE:{cariId}:{yerelGun}'. HedefRota /raporlar/vade-takibi. Cari sayısı Isler:Vade:MaksCari (varsayılan 500) ile sınırlanır; aşılırsa tek özet bildirim (TB-19).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/Isler/MasakSibSuresiIsi.cs` — Kod MASAK_SIB_SURESI. masak_supheli_bildirimler'de Durum=ACIK ve SonBildirimTarihi ≤ bugün+Isler:Masak:UyariGun (varsayılan 3) olanları okur — tarihi YENİDEN HESAPLAMAZ, veride hazır olanı kullanır (MasakVarliklari.cs:186-189). Süre geçmişse KRITIK. Tekillik: 'SIB:{bildirimId}:{yerelGun}'. GorunurRoller 'SAHIP,MUDUR,MUHASEBE' (kişisel veri; TEZGAHTAR görmez — KenarCubugu.razor:54-56 kuralının sunucu tarafı). Bildirim gövdesinde kişi adı/belge no GEÇMEZ, yalnız kayıt no + kalan gün.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/Isler/SaklamaOnizlemeIsi.cs` — Kod SAKLAMA_ONIZLEME. YALNIZ SaklamaSuresiIsi.Onizleme() çağırır; süresi dolmuş kayıt varsa tek bir UYARI bildirimi üretir, hedef rota /uyum. Uygula() bu dosyada ve Isler/ ağacının tamamında GEÇMEZ (denetim.sh sayacı). Tekillik: 'SAKLAMA:{yerelAy}' (ayda bir hatırlatır, her gün dırdır etmez).
- **YENİ (Faz B)** `dotnet/Kuyumcu.Api/Modules/Isler/Isler/TevsikKurtarmaIsi.cs` — Kod TEVSIK_KURTARMA. VARSAYILAN KAPALI: Etkin(y) => y.GetValue("Isler:TevsikKurtarma:Etkin", false). Yalnız Tevsik:Etkin=false yapılmış kiracılarda, son 5 takvim günü içindeki nakit hareketlerinden TevsikKurali'nın haddi aştığını söylediklerini listeler ve 'kendiliğinden bildirim süresi işliyor' hatırlatması üretir. İŞ GÜNÜ HESABI YAPILMAZ (resmî tatil takvimi yok — varsayım yasak); metin 'beş iş günü' der, sayacı kullanıcı yürütür. Tekillik: 'TEVSIK:{hareketId}'.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/IBildirimKanali.cs` — interface IBildirimKanali { string Kod; Task Gonder(Bildirim b, CancellationToken iptal); } ve tek uygulama UygulamaIciKanal (yalnız veritabanına yazılmış bildirimi 'teslim edildi' sayar). Dosyanın başına ADR-0016 gerekçesi yazılır: sözleşme olmadan ikinci uygulama eklenmez.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Isler/IslerModulu.cs` — BildirimServisi (liste/sayı/kapat) + controller'lar: BildirimController [ApiController, Authorize, Route("api/v1/bildirimler")] ve IslerController [Route("api/v1/isler")] — SayimModulu.cs:231-241 deseni. Her controller kendi [RolGerekli]/[ModulYetkisi] niteliklerini AÇIKÇA taşır (miras alınmaz).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Isler.BildirimServisi>(); s.AddScoped<Isler.IsCalistirici>(); s.AddSingleton<IBildirimKanali, Isler.UygulamaIciKanal>(); ve üç/dört işin AddSingleton<IZamanlanmisIs, …> kaydı (iş sınıfları durumsuz).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Satır 205'in yanına builder.Services.AddHostedService<Modules.Isler.IsZamanlayici>();
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/SistemKapsami.cs` — 17-23 arasındaki 'İZİN VERİLEN KULLANIM — tam liste' yorumuna: '5. IsZamanlayici → aktif kiracı listesi [Sistem] · 6. IsZamanlayici → kiracı başına iş koşumu [Kiraci(id)]'.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — 2 DbSet + 5 indeks.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` — Moduller.Tumu'ya "bildirim"; Varsayilan matrisinde MUDUR/MUHASEBE/TEZGAHTAR üçüne de "bildirim" (bildirim listesi rol süzmesiyle zaten daralır).
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — "Isler": { "Etkin": true, "TurDakika": 15, "Vade": { "UfukGun": 7, "MaksCari": 500 }, "Masak": { "UyariGun": 3 }, "TevsikKurtarma": { "Etkin": false } }
- **YENİ** `dotnet/Kuyumcu.Web/Pages/Bildirimler.razor` — @page "/bildirimler". Açık/kapalı sekmesi, önem rozeti (Rozet.razor), hedef rotaya git bağlantısı, 'Kapat' düğmesi (ModalPencere + not alanı), HataBandi/BasariBandi zorunlu (UX-S9). BosDurum.razor ile 'bekleyen iş yok'.
- **YENİ (Faz B)** `dotnet/Kuyumcu.Web/Pages/IsCalismalari.razor` — @page "/bildirimler/is-calismalari". SAHIP/MUDUR; son 50 çalışma, durum, süre, üretilen bildirim, hata özeti + 'Şimdi çalıştır' düğmesi.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/UstBar.razor` — Kur bandının soluna 🔔 rozeti: /bildirimler/sayi'yı 60 sn'de bir sorar (CanliKurServisi yoklama deseni), 0 ise rozet gösterilmez, tıklayınca /bildirimler'e gider.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Yetki.Erisebilir("bildirim") dalında <NavLink href="/bildirimler">🔔 @Dil.T.Nav.Bildirimler</NavLink>.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs` — ["/bildirimler"] ve ["/bildirimler/is-calismalari"] satırları — yazılmazsa RotaBaslikTestleri KIRMIZI (dosya başlığındaki uyarı).
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukNav'a Bildirimler/IsCalismalari + yeni SozlukBildirim bloğu; Tr (≈1193) ve En (≈2151) bloklarına BİRLİKTE eklenir, yoksa derlenmez.
- **DEGISTIR** `dotnet/denetim.sh` — BEKLENEN_VARLIK 15 → 17 (iki yeni IKiraciVarlik) + yeni sayaç: "Isler/ altında SaklamaSuresiIsi.Uygula çağrısı = 0" (ADR-0008 kenarından dolanma koruması) + "IBildirimKanali DI kaydı = 1".
- **DEGISTIR** `dotnet/Kuyumcu.Web/wwwroot/css/app.css` — .bildirim-rozet, .bildirim-kart önem renkleri — mevcut tasarım belirteçleri kullanılır (TasarimBelirtecTestleri sabit renk kullanımını yakalar).

## Ekranlar
- `/bildirimler` · `dotnet/Kuyumcu.Web/Pages/Bildirimler.razor` — Açık/kapalı bildirim listesi, önem rozeti, hedefe git, kapat (not ile). (menü/yetki: Yetki.Erisebilir("bildirim") — sunucuda [ModulYetkisi("bildirim")] + gorunur_roller süzmesi)
- `/bildirimler/is-calismalari` · `dotnet/Kuyumcu.Web/Pages/IsCalismalari.razor` — Faz B. Son 50 iş çalışması + elle 'Şimdi çalıştır'. (menü/yetki: Yalnız SAHIP/MUDUR (Oturum.Kullanici?.Rol kontrolü + sunucuda [RolGerekli]))
- `(bileşen)` · `dotnet/Kuyumcu.Web/Layout/UstBar.razor` — 🔔 açık bildirim sayısı rozeti, 60 sn yoklama, tıklayınca /bildirimler. (menü/yetki: Modül yetkisi yoksa rozet hiç çizilmez)

## İş kuralları (fail-closed)
- TEK KOŞUM: aynı (tenant_id, is_kodu, calisma_anahtari) ikinci kez EKLENEMEZ; çakışma veritabanı UNIQUE indeksinden gelir, uygulama 'önce SELECT et' ile karar vermez (yarış koşulu kapalı).
- TEKRARSIZ BİLDİRİM: aynı (tenant_id, tekillik_anahtari) ikinci bildirim EKLENEMEZ; iş günde 96 kez koşsa da kullanıcı tek satır görür.
- BİLDİRİM SİLİNMEZ: kapatma yalnız kapatildi/kapatan_kullanici_id/kapatma_notu yazar. Remove/ExecuteDelete yok (ExecuteUpdate/Delete beyaz listesi 4'te kalır).
- İŞ ÇALIŞMASI SİLİNMEZ ve YALNIZ BİR KEZ tamamlanır: satır BASLADI ile eklenir, bitiminde bitis/durum/sayaçlar tek seferde yazılır. ⚠ Dürüst kayıt: bu, 'saf append-only' değil 'ekle + bir kez tamamla'dır; hak talebi (claim) eşzamanlılığı kapatmak için başta yazılmak zorundadır. ADR-0004 denetim izini kapsar, bu tablo denetim izi DEĞİLDİR.
- DEFTERE DOKUNMA YASAĞI: Isler/ ağacında HareketFabrikasi, Hareketler.Add, KasaHareketleri.Add, CariBakiyeler güncellemesi GEÇMEZ (denetim.sh:58 sayısı 1 kalır).
- ADR-0008 KAPISI: Isler/ ağacında SaklamaSuresiIsi.Uygula( GEÇMEZ.
- DIŞ KANAL TEKELİ: DI'da IBildirimKanali uygulaması TAM 1'dir (UygulamaIciKanal). İkincisi eklenirse test kırmızı.
- KİŞİSEL VERİ SIZDIRMAZ: bildirim başlığı/gövdesi kimlik no, belge no, TCKN, şifreli alan TAŞIMAZ; MASAK kaynaklı bildirimler gorunur_roller='SAHIP,MUDUR,MUHASEBE' ile üretilir.
- YEREL GÜN: çalışma anahtarı ve tekillik anahtarı Zaman.YerelBugun/YerelGunAraligiUtc ile kurulur; DateTime.UtcNow.Date kullanılmaz.
- FAIL-SOFT TUR, FAIL-CLOSED KAPI: bir işin hatası turu düşürmez (hata_ozeti'ne yazılır, sonraki kiracıya geçilir) — ama bir kapı (yetki/rol/kiracı) ihlali ASLA yumuşatılmaz.
- LİSTE SINIRI: bildirim ≤100, iş çalışması ≤50, vade işinde cari ≤500 (aşılırsa tek özet bildirim).
- KİRACI SIZINTISI YOK: her iş SistemKapsami.Kiraci(id) kapsamında koşar; iş kodu içinde tenant_id elle filtrelense bile global filtre ikinci katmandır.

## Testler
- `dotnet/Kuyumcu.Tests/IsCatisiTestleri.cs` [DB]
  - Aynı yerel gün içinde IsCalistirici iki kez çağrılır → is_calismalari'nda o (is_kodu, anahtar) için TEK satır, ikinci çağrı ATLANDI döner
  - Bir iş iki tur boyunca aynı taslağı üretir → bildirimler'de TEK satır (tekillik anahtarı)
  - İş istisna atar → çalışma satırı durum=HATALI, hata_ozeti dolu, sonraki kiracının işi yine koşar (tur düşmez)
  - Elle koşum anahtarı (…-Ehhmm) günlük anahtarla çakışmaz → yeni çalışma satırı açılır ama yeni bildirim ÜRETİLMEZ
  - İki kiracıda aynı koşullar → her kiracı kendi bildirimini görür, A kiracısının bildirimi B'nin listesinde YOK
- `dotnet/Kuyumcu.Tests/IsSenaryoTestleri.cs` [DB]
  - Vadesi 3 gün geçmiş açık veresiyesi olan cari → VADE_HATIRLATMA bildirimi KRITIK, hedef rota /raporlar/vade-takibi
  - Vadesi 30 gün sonra olan cari, ufuk 7 → bildirim ÜRETİLMEZ
  - Borç tahsil edilip kapanınca ertesi gün yeni bildirim ÜRETİLMEZ
  - Durum=ACIK ve son_bildirim_tarihi 2 gün sonra olan ŞİB → MASAK_SIB_SURESI bildirimi, gövdede ad/belge no GEÇMEZ, gorunur_roller TEZGAHTAR içermez
  - Son bildirim tarihi geçmiş ŞİB → önem KRITIK
  - Süresi dolmuş MASAK kaydı varken SAKLAMA_ONIZLEME → bildirim üretir ve masak_kimlik_tespitleri.ad_soyad DEĞİŞMEZ (Uygula çağrılmadı kanıtı)
  - TEVSIK_KURTARMA varsayılan yapılandırmada Etkin=false → hiç koşmaz, çalışma satırı bile açılmaz
- `dotnet/Kuyumcu.Tests/BildirimHttpTestleri.cs` [HTTP]
  - GET /api/v1/bildirimler jetonsuz → 401
  - TEZGAHTAR jetonuyla GET → 200 ama MASAK kaynaklı bildirim listede YOK
  - TEZGAHTAR POST /api/v1/isler/{kod}/calistir → 403 ve mesajda gerekli roller
  - limit=100000 → en çok 100 kayıt döner
  - Bilinmeyen iş kodu → 404
  - Kapatılmış bildirimi tekrar kapat → 400
  - A kiracısının bildirim id'si B kiracısının jetonuyla kapatılmak istenir → 404
- `dotnet/Kuyumcu.Tests/BildirimEkranTestleri.cs` [bUnit]
  - /bildirimler açılır → GET /bildirimler?durum=ACIK çağrılır (sahte API kaydı, CariEkranTestleri deseni)
  - Kapat düğmesi → POST /bildirimler/{id}/kapat gövdesiyle gider
  - Boş liste → BosDurum bileşeni çizilir
  - UstBar rozeti: sayi=0 yanıtında rozet DOM'da YOK; sayi=3'te rozet ve '3' var
  - Rota başlıkları: /bildirimler ve /bildirimler/is-calismalari RotaBasliklari.Harita'da (RotaBaslikTestleri zaten iki yönlü doğruluyor)
- `dotnet/Kuyumcu.Tests/IsCatisiSafTestleri.cs` [saf]
  - CalismaAnahtari İstanbul saatiyle 00:30'da ve 23:30'da AYNI yerel günü üretir (K5 tuzağı)
  - Tekillik anahtarı biçimi: cari kimliği ve yerel gün içerir, saat İÇERMEZ
  - DI taraması: IBildirimKanali uygulaması sayısı = 1 (ModulKayit üzerinden ServiceCollection kurulup sayılır)
  - Isler/ klasörünün kaynak metninde 'Uygula(' ve 'HareketFabrikasi' 0 kez geçer (kaynak tarama testi — denetim.sh'in test tarafı ikizi)

## Mutasyon kanıtları
- is_calismalari UNIQUE (tenant_id, is_kodu, calisma_anahtari) indeksi kaldırılır → IsCatisiTestleri 'aynı gün iki kez çağrılır → tek satır' KIRMIZI (global kiracı filtresi bu testi kurtaramaz; ölçülen şey tekillik)
- IsCalistirici'de claim satırı işten SONRA eklenecek şekilde taşınır → 'ikinci çağrı ATLANDI döner' KIRMIZI
- bildirimler UNIQUE (tenant_id, tekillik_anahtari) kaldırılır → 'iki tur, tek bildirim' KIRMIZI
- Tekillik anahtarına saat eklenir (yerel gün yerine yerel an) → hem 'iki tur tek bildirim' hem saf anahtar testi KIRMIZI
- MasakSibSuresiIsi'nin gorunur_roller'ı boş bırakılır → BildirimHttpTestleri 'TEZGAHTAR MASAK bildirimini görmez' KIRMIZI
- SaklamaOnizlemeIsi'nde Onizleme yerine Uygula çağrılır → IsSenaryoTestleri 'ad_soyad DEĞİŞMEZ' KIRMIZI + IsCatisiSafTestleri kaynak taraması KIRMIZI + denetim.sh BAŞARISIZ
- ModulKayit'e ikinci bir IBildirimKanali (örn. SahteEpostaKanali) kaydedilir → DI tarama testi KIRMIZI
- TevsikKurtarmaIsi.Etkin varsayılanı true yapılır → 'varsayılan yapılandırmada hiç koşmaz' KIRMIZI
- IsZamanlayici'de SistemKapsami.Kiraci(id) yerine Sistem() kullanılıp iş tüm kiracılar üzerinde koşturulur → 'A kiracısının bildirimi B'de yok' KIRMIZI
- RotaBasliklari'ndan /bildirimler satırı silinir → RotaBaslikTestleri KIRMIZI (rota listesi yansımayla geldiği için)

## Canlı doğrulama
- API'yi tek başına ayağa kaldır: dotnet run --project dotnet/Kuyumcu.Api --urls http://localhost:5199 (python3 yok; JSON'u perl ile biçimle)
- Test kiracısı aç, SAHIP jetonu al; vadesi geçmiş bir veresiye satış yap (mevcut uçtan) → POST /api/v1/isler/VADE_HATIRLATMA/calistir → 200 ve yanıtta uretilenBildirim=1
- GET /api/v1/bildirimler → 200 ve o bildirim listede; GET /api/v1/bildirimler/sayi → 1
- Aynı ucu ikinci kez çağır → uretilenBildirim=0, sayi hâlâ 1 (tekillik kanıtı)
- SQL: SELECT COUNT(*) FROM bildirimler WHERE tenant_id=@t → 1; SELECT COUNT(*) FROM is_calismalari WHERE tenant_id=@t AND is_kodu='VADE_HATIRLATMA' → 2 (biri günlük, biri elle)
- TEZGAHTAR jetonuyla POST /api/v1/isler/VADE_HATIRLATMA/calistir → 403 (gövde metniyle birlikte raporlanır)
- POST /api/v1/bildirimler/{id}/kapat → 200; SELECT kapatildi FROM bildirimler → dolu; satır sayısı DEĞİŞMEDİ
- Zamanlayıcının kendi turu: Isler:TurDakika=1 ile 2 dakika bekle → is_calismalari'nda günlük anahtarlı satır TEK kalır (ikinci tur atlar), uygulama logunda 'zaten çalışmış' kaydı
- Tarayıcıda /bildirimler ekranı: rozet 1 gösteriyor, kapatınca rozet kayboluyor (ekran görüntüsü/DOM kanıtı)
- Sonunda test kiracısını SİL; kuyumcu_net 5 kiracıya döner (Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası Kuyumculuk, Demo Kuyumcu, Ramazan Deneme)

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/SistemKapsami.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs`
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`
- `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs (yeni rota eklenince kendiliğinden kapsar; dosya değişmez)`

## Mevzuat dayanağı
- VUK mükerrer md.355/4 son cümle (aykırı ödemeyi beş iş günü içinde kendiliğinden bildirene ceza kesilmez) — **DOGRULANMALI** — teknik-borc-envanteri.md:281 ve mevzuat-gereksinimleri.md:57 (V-03d) aynı hükmü aktarıyor ama madde metni bu oturumda birincil kaynaktan doğrulanmadı. Bu yüzden TEVSIK_KURTARMA işi varsayılan KAPALI gelir ve hiçbir davranışı değiştirmez; yalnız metinsel hatırlatma üretir. 'İş günü' hesabı YAPILMAZ — resmî tatil takvimi üründe yok, varsayım yasak.
- MASAK şüpheli işlem bildirimi süresi (10 iş günü) — son_bildirim_tarihi alanı — **DOGRULANDI (kod gerçeği olarak)** — Tarih zaten ürün içinde hesaplanıp saklanıyor (MasakVarliklari.cs:186-189, 'resmî tatiller hariç' notuyla). Paket 6 bu rakamı YENİDEN HESAPLAMAZ, yalnız okur — dolayısıyla bu pakete yeni bir mevzuat rakamı GİRMEZ. Hesabın kendisinin mevzuat doğruluğu MASAK paketinin (adım A) sorumluluğundadır.
- MASAK md.46/1 (sekiz yıl muhafaza) ve KVKK amaçla sınırlılık — ADR-0008 — **DOGRULANDI (proje ADR'si olarak mühürlü)** — ADR-0008 kararı 'iş ELLE tetiklenir' diyor. Paket 6 bu kararı DEĞİŞTİRMEZ; yalnız 'süresi dolmuş kayıt var' hatırlatması üretir. Otomatik maskeleme kararı hâlâ ekip oylamasına açık bir kalemdir (ADR-0008 'Olumsuz / açık kalem').
- Kuyum Ticareti Yönetmeliği — yetki belgesi tadili on gün (RG 29.04.2026/33238) — **DOGRULANMALI** — mevzuat-gereksinimleri.md:73 (K-02) bu hükmü 'yazılım yükümlülüğü değil, işletme süreci' diye işaretlemiş. Üründe yetki belgesi tarihi tutan bir alan YOKTUR (grep: yalnız yorumlarda geçiyor). Bu nedenle paket 6'ya 'yetki belgesi tadil hatırlatması' İŞİ KONULMADI — hatırlatılacak veri yok. İstenirse önce işletme kartına belge tarihi alanı gerekir; açık sorulara yazıldı.
- 459 s. VUK Genel Tebliği tevsik haddi (30.000 TL) ve MASAK 185.000 TL eşiği — **DOGRULANDI (A7 araştırması, devir belgesi)** — Bu pakette KULLANILMAZ; eşikler zaten TevsikEsikleri/MasakKapisi içinde yapılandırmadan okunuyor (ADR-0005). Paket 6 eşik rakamı taşımaz.

## Riskler
- ÇALIŞMA ANAHTARI YARIŞI: iki API örneği aynı anda koşarsa claim satırı ikisinde de denenir; UNIQUE indeks birini reddeder. Bugün dağıtım tek örnek (deploy/nginx-kuyumcu.conf tek upstream) — çok örnekli dağıtıma geçilirse davranış yine doğrudur, ancak DbUpdateException'ın duplicate-key olup olmadığı MySQL hata numarasıyla (1062) ayırt edilmelidir; her istisnayı 'zaten çalıştı' saymak gerçek hatayı gizler (Anayasa md.1).
- TEST EDİLEMEZ ZAMANLAYICI: HttpOrtami.cs:78 tüm IHostedService'leri kaldırır. Mantık IsCalistirici'de olduğu için kapsanır, ama IsZamanlayici'nin kendi döngüsü (uyanma aralığı, kiracı gezme) HTTP testinde koşmaz — canlı doğrulama adımı bu boşluğu kapatır ve 'ölçülmedi' denmez.
- BİLDİRİM GÜRÜLTÜSÜ: vade işi 500 cariye kadar tek tek bildirim üretebilir; kullanıcı 200 bildirimle karşılaşırsa özelliği kapatır. Faz A'da cari başına GÜNDE tek bildirim + MaksCari üstünde tek özet bildirim ile sınırlandı; sahada ölçülmeden daha agresif gruplama yapılmaz.
- BÜYÜME: bildirimler ve is_calismalari zamanla büyür; bu pakette temizlik işi YOKTUR (silme, denetim izi kültürüne aykırı). Kapalı bildirimlerin arşivlenmesi açık sorulara bırakıldı — yedek süresini fark ettirmeden uzatabilir (paket 4'teki blob büyümesi riskiyle aynı tür).
- YAZ-SERİ ÇAKIŞMASI: KuyumcuDbContext.cs, Sozlukler.cs, ModulKayit.cs, KenarCubugu.razor, denetim.sh ve ModelSnapshot paket 1/34/38/39/41/42 ile ORTAK. Paket 6 bu dosyalara dokunduğu sürece başka bir paketle AYNI ANDA yazılamaz.
- TEVSIK_KURTARMA'nın hukuki dayanağı doğrulanmadı; kapalı gelse bile metin yanlış hukuki güven verebilir. Bildirim gövdesi 'mali müşavirinize danışın' uyarısı taşır ve tarih hesabı yapmaz.

## Açık sorular
- Kapalı bildirimler için saklama/arşivleme politikası ne olacak? (silme bu projede kural dışı; 'N günden eski KAPALI bildirimleri listede gösterme' yeterli mi, yoksa ayrı arşiv görünümü mü?) — ekip oyu gerekir.
- İşletme kartına 'yetki belgesi tarihi' alanı eklenip tadil/yenileme hatırlatması üretilsin mi? Bugün böyle bir alan yok; K-02 'işletme süreci' diye işaretli (mevzuat-gereksinimleri.md:73).
- Bildirim rozeti yoklama ile mi (60 sn) yoksa mevcut SignalR altyapısı (Program.cs:326 /hubs/kur) genişletilerek anlık mı beslensin? Yoklama Faz A için seçildi; SignalR hub'ına ikinci kanal eklemek güvenlik yüzeyini (kiracı bazlı grup yönetimi) büyütür.
- TEVSIK_KURTARMA işinin dayanağı (VUK mük. md.355/4) birincil kaynaktan doğrulanacak mı, yoksa iş hiç yazılmayıp TB-16 açık borç olarak mı kalacak? Faz B'nin tek belirsiz kalemi budur.
- IsZamanlayici tur aralığı 15 dk mı, günde tek sabit saat mi (örn. yerel 08:30) olsun? 15 dk elle tetikleme olmadan da gün içi değişimi yakalar; sabit saat daha öngörülebilirdir. Ölçüm yok, tercih ekip oyuna bırakıldı.
- denetim.sh varlık sayacının '>=' mi '=' mi olacağı (QA önerisi, MIMARI-KALAN-PAKETLER:225) bu pakette de karşımıza çıkıyor; paket 6 sayıyı 17'ye yükseltir ama karşılaştırma kararını vermez.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Mühürlü K0 kararlarına sadık ve çekirdek doğru: iş çatısı deftere yazmıyor, kapıları atlamıyor, ADR-0008 Uygula()'yı çağırmıyor ve bunu denetim.sh sayacıyla zorluyor. BackgroundService ile iş mantığının ayrılması (KurZamanlayici'nin test edilemez deseninin tekrar edilmemesi) mimari olarak en değerli karar. Tekillik anahtarı + UNIQUE indeks ile tekrarsızlığı veritabanına bırakması, 'önce SELECT et' yarış koşulunu kapatıyor. Dış kanalın sözleşmesiz açılmaması ve DI'da tek uygulamanın testle zorlanması doğru.
  - Koşullar: DbUpdateException'ın duplicate-key (MySQL 1062) olup olmadığı AYIRT EDİLMEDEN 'zaten çalıştı' sayılamaz. Her istisnayı sessizce atlamak gerçek hatayı gizler — Madde 1 ihlali. Bu, Faz A'nın kabul ölçütüdür. · is_calismalari'nın 'ekle + bir kez tamamla' olduğu, saf append-only OLMADIĞI spec'te dürüstçe yazılmış; aynı not kodda ve ADR'de de yer alsın ki sonraki oturum ADR-0004 ile karıştırmasın. · Yeni 'bildirim' modül anahtarı paket 29'un tohumlama mekanizmasından SONRA eklensin (mevcut kiracılarda sessiz 403). · IsZamanlayici'nin kendi döngüsü HTTP testinde koşmuyor (HttpOrtami tüm IHostedService'leri kaldırıyor); bu boşluk canlı doğrulama adımıyla kapatılacak ve raporda 'birim testle değil canlıyla doğrulandı' diye yazılacak. · TEVSIK_KURTARMA (Faz B) varsayılan KAPALI kalacak ve iş günü hesabı YAPILMAYACAK (resmî tatil takvimi yok); bildirim gövdesinde hukuki güven veren ifade kullanılmayacak. · Kapalı bildirimlerin arşivleme politikası Faz A'da karara bağlanmazsa tablo büyümesi bir sonraki pakette sorun olur — en azından 'N günden eski KAPALI bildirim listede gösterilmez' kuralı konsun.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Deftere ve ADR-0008'e dokunmayan, dış kanal açmayan, kiracı gezinmesini tek meşru kapıdan yapan bir çatı. Riskler istisna sınıflandırması ve kişisel veri sızıntısı; koşullarla kapanır.
  - Koşullar: DbUpdateException yalnız MySQL 1062 (duplicate key) ise 'zaten çalıştı' sayılsın; diğer istisnalar HATALI olarak kayda geçsin (spec riskte yazıyor, kurala taşınsın). · hata_ozeti'ne bağlantı dizesi/sır/kişisel veri yazılmaz (≤500, istisna mesajı değil sınıflandırılmış özet); bildirim başlık/gövdesinde TCKN/belge no/ad yok — IsSenaryoTestleri zorunlu. · gorunur_roller süzmesi SUNUCUDA; MASAK kaynaklı bildirim TEZGAHTAR'a görünmez (HTTP testi). · IsZamanlayici her kiracı için AYRI DI scope (DbContext sızıntısı yok) ve SistemKapsami.Kiraci(id); SistemKapsami.cs 'izinli kullanım' listesi güncellenir; IgnoreQueryFilters 0 kalır. · Isler/ altında SaklamaSuresiIsi.Uygula = 0, HareketFabrikasi = 0, IBildirimKanali DI = 1 — denetim.sh sayaçları *.mutorj hariç. · Elle tetikleme SAHIP/MUDUR + 3/dk oran sınırı + denetim izi; TEVSIK_KURTARMA varsayılan KAPALI ve iş günü hesabı YOK (kabul). · Bildirimler silinmez; kapatma yalnız damga.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Zamanlayıcı/mantık ayrımı HttpOrtami.cs:78 gerçeğine göre doğru kurulmuş; iki UNIQUE anahtar tekrarsızlığı veritabanında zorluyor; mutasyonlar test adlarıyla eşleşiyor. Tohumlama borcu görülmemiş ve iki fikstür yöntemi belirsiz.
  - Koşullar: Yeni modül anahtarı 'bildirim' için paket 3 ile aynı tohumlama borcu: mevcut kiracılarda tek marker yüzünden rozet/ekran sessizce 403. Paket 29 K4-F mekanizması önce gelsin ya da burada kurulsun; BildirimHttpTestleri'ne eski marker'lı kiracı regresyonu eklensin. · IsCatisiTestleri 'aynı gün iki kez çağrılır → tek satır' mutasyonu 'UNIQUE indeks kaldırılır' ile ölçülüyor; ancak IsCalistirici claim'i 'önce SELECT' yapmıyorsa indeks tek katman — EVET; ama DbUpdateException'ın MySQL 1062 ayrımı için 'başka DbUpdateException (ör. kolon uzunluğu) ATLANDI sayılmaz, HATALI yazılır' testi eklensin (Madde 1 riskinde kendileri yazmış, test yok). · UstBar rozeti 60 sn yoklama: bUnit testi zamanlayıcıyı tetiklemesin; ilk yükleme çağrısı + elle yenile ile ölçülsün; zamanlayıcı testte devre dışı (kararsızlık). · IsSenaryoTestleri 'Vadesi 3 gün geçmiş cari' fikstürü VadeKapisi.VadeHesapla ile geçmiş vade üretemez (vade ileri tarih); test doğrudan Hareket.VadeTarihi'ni geçmişe yazmalı — HareketFabrikasi bunu engelliyorsa fikstür yöntemi belirtilsin. · IsCatisiSafTestleri 'Isler/ klasörü kaynak metninde Uygula( geçmez' testi SaklamaSuresiIsi.Uygula (satır 67) adına bağlı; 'Onizleme' çağrısı da 'Uygula' alt dizesi içermez — EVET; ama tarama Kuyumcu.Api derleme dizinini değil kaynak dizinini okumalı, test yolu sabitlenmesin (yansıma ile assembly konumundan türetilsin). · Canlı doğrulama 8. adımı (TurDakika=1 ile 2 dakika bekle) tek adım 3 dk sınırına yakın; kanıt is_calismalari SQL sayımı ile yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- HATA GİZLEME YASAK (Faz A kabul ölçütü): DbUpdateException yalnız MySQL hata 1062 (duplicate key) ise 'zaten çalıştı' sayılır; diğer istisnalar HATALI olarak kayda geçer. Her istisnayı sessizce atlamak Anayasa md.1 ihlalidir (Bertan + Mehmet birebir aynı).
- TOHUMLAMA: yeni 'bildirim' modül anahtarı paket 29'un tohumlama mekanizmasından SONRA eklensin — mevcut kiracılarda tek marker yüzünden rozet/ekran sessizce 403 olur; ya K4-F mekanizması önce gelir ya burada kurulur, BildirimHttpTestleri'ne eski-marker senaryosu yazılır.
- KİRACI İZOLASYONU: IsZamanlayici her kiracı için AYRI DI scope (DbContext sızıntısı yok) + SistemKapsami.Kiraci(id); SistemKapsami.cs 'izinli kullanım' listesi güncellenir; IgnoreQueryFilters 0 kalır.
- SIR/KİŞİSEL VERİ: hata_ozeti ≤500 ve sınıflandırılmış özet olsun (ham istisna mesajı değil); bağlantı dizesi/sır/kişisel veri yazılmaz; bildirim başlık-gövdesinde TCKN/belge no/ad yok — IsSenaryoTestleri zorunlu.
- GÖRÜNÜRLÜK: gorunur_roller süzmesi SUNUCUDA; MASAK kaynaklı bildirim TEZGAHTAR'a görünmez (HTTP testi). Elle tetikleme SAHIP/MUDUR + 3/dk oran sınırı + denetim izi.
- TEVSIK_KURTARMA (Faz B) varsayılan KAPALI; iş günü hesabı YAPILMAZ (resmî tatil takvimi yok); gövde hukuki güven veren ifade taşımaz, 'mali müşavirinize danışın' uyarısı bulunur. Dayanağı (VUK mük. md.355/4) doğrulanmazsa iş yazılmaz, TB-16 açık borç kalır.
- SAKLAMA: bildirimler SİLİNMEZ, kapatma yalnız damgadır; en azından 'N günden eski KAPALI bildirim listede gösterilmez' kuralı Faz A'da konur (aksi hâlde tablo büyümesi sonraki pakette sorun olur).
- DENETİM SAYAÇLARI: Isler/ altında SaklamaSuresiIsi.Uygula = 0, HareketFabrikasi = 0, IBildirimKanali DI = 1 (*.mutorj hariç). IsCatisiSafTestleri taraması derleme dizinini değil KAYNAK metnini okumalı. denetim.sh varlık sayacı 17'ye çıkar; '>=' mi '=' mi kararı bu pakette verilmez.
- TEST BOŞLUĞU DÜRÜSTÇE YAZILIR: HttpOrtami.cs:78 tüm IHostedService'leri kaldırdığı için IsZamanlayici döngüsü HTTP testinde koşmaz; boşluk canlı doğrulamayla kapatılır ve raporda 'birim testle değil canlıyla doğrulandı' denir. Canlı adım TurDakika=1 ile is_calismalari SQL sayımına dayanır.
- EKRAN/FİKSTÜR: UstBar rozeti 60 sn yoklama — bUnit testinde zamanlayıcı devre dışı, ilk yükleme + elle yenile ile ölçülür. 'Vadesi 3 gün geçmiş cari' fikstürü VadeKapisi.VadeHesapla ile üretilemez; Hareket.VadeTarihi doğrudan geçmişe yazılır, yöntem belgelenir. is_calismalari'nın append-only OLMADIĞI koda ve ADR'ye not düşülür (ADR-0004 ile karışmasın). Bildirim gürültüsü: cari başına günde tek + MaksCari üstünde tek özet.
