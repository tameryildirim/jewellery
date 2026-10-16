# Paket 42 — Kâğıt sevk irsaliyesi (belge modeli + yazdırma + ortak belge yerleşimi)

> Katman: Katman 6 — Uyum (paket 38–42) · Tahmin: 3–4 (Faz A 2–3, Faz B 1) gün · Önkoşullar: yok · Kaynak: katman mimarı K6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 5

## Hedef
Mal dükkândan çıktığında (şubeye, müşteriye, atölyeye, konsinye alıcısına) onu belgeleyen hiçbir kayıt yok: kod tabanında 'irsaliye' 0 isabet. Bugün yalnız tek ürünlü TRANSFER hareketi var ve o bir defter satırıdır, belge değildir. Bu paket kâğıt sevk irsaliyesini belge olarak modeller, numaralar, durum makinesine bağlar ve yazdırılabilir çıktısını üretir. Aynı zamanda ADR-0007'nin 'dördüncü belge öncesi ortak yerleşim' borcunu öder.

## Kodda bugünkü durum
'irsaliye' kelimesi dotnet/ altında (obj/bin hariç) 0 KEZ geçiyor. Şubeler arası hareket SatisServisi.Transfer'dir (SatisModulu.cs:757-816): TEK ÜRÜN, TEK ADET alanı, TlTutari=0, has bacağı negatif, KarsiSubeId dolu (SatisModulu.cs:781-798); belge üretmez. Belge deseni hazır: GiderPusulasi varlığı (GiderPusulasi.cs:54-108), numara üretimi (FaturaNoUretici.cs:41-59, MAX+1), belge veri ucu (HurdaModulu.cs:61-77, okuma izi düşüyor), yazdırma sayfası (PusulaBelgesi.razor, 166 satır, @media print, ADR-0007). Durum makinesi deseni Fatura'da (FaturaModulu.cs:71-77, 222-224). ⚠ SINIR: hareketler tablosunda ADET KOLONU YOK (Entities.cs'te 'adet' yalnız stoklar:263'te) → irsaliye satırının miktarı hareketten TÜRETİLEMEZ, satırda kendi alanı olmak zorundadır. Taş ibaresi ürün kartında (Entities.cs:193 tas_kokeni, 201 tas_ibaresi) ve ADR-0013 satışta fail-closed.

## Kapsam
**İçinde:**
- sevk_irsaliyeleri + sevk_irsaliye_satirlari varlıkları, SI-{yıl}-{sıra:6} numara
- Durum makinesi TASLAK→SEVK_EDILDI→TESLIM_EDILDI, her durumdan IPTAL (gerekçeli, MUDUR+)
- Yazdırılabilir belge ucu + sayfası + ORTAK BelgeYerlesimi bileşeni ve print CSS standardı (ADR-0007 Not maddesi)
- Faz B: transfer/satış akışından 'irsaliye oluştur' kısayolu, irsaliyesiz sevk raporu, fatura bağı ve 'süresinde faturalanmamış irsaliye' uyarısı

**Dışında:**
- e-İrsaliye (ADR-0012 ile aynı gerekçe: entegratör sözleşmesi olmadan sahte entegrasyonla yapılmaz; PARITE §4 C sınıfı). Belgede 'e-İrsaliye' ibaresi geçmez, XML üretilmez.
- İrsaliyeli fatura (tek belge) — ayrı karar
- Stok hareketi üretme: irsaliye BELGEDİR, stoğu satış/transfer hareketi düşürür
- Taşıyıcı/kargo entegrasyonu (C sınıfı)
- Fiziki gram sütununun hareketten türetilmesi (hareketler.adet yok)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| sevk_irsaliyeleri | YENİ | id VARCHAR(36) PK · tenant_id VARCHAR(36) · irsaliye_no VARCHAR(30) · sube_id VARCHAR(36) (çıkış şubesi) · tur VARCHAR(20) ('SUBE_TRANSFER'|'MUSTERI_TESLIM'|'TEDARIKCI_IADE'|'ATOLYE_GONDERIM'|'KONSINYE_GONDERIM') · cari_id VARCHAR(36) NULL · karsi_sube_id VARCHAR(36) NULL · teslim_alan_ad VARCHAR(200) · teslim_adresi VARCHAR(500) NULL · tasiyan_ad VARCHAR(200) NULL · plaka VARCHAR(20) NULL · fiili_sevk_tarihi DATETIME · duzenleme_tarihi DATETIME · durum VARCHAR(20) · iptal_gerekcesi VARCHAR(1000) NULL · iptal_kullanici_id VARCHAR(36) NULL · fatura_id VARCHAR(36) NULL · aciklama TEXT NULL · olusturan_kullanici_id VARCHAR(36) · olusturuldu DATETIME · guncellendi DATETIME | (tenant_id, irsaliye_no) UNIQUE · (tenant_id, fiili_sevk_tarihi) · (tenant_id, durum) | YYYYMMDDHHMMSS_SevkIrsaliyesi |
| sevk_irsaliye_satirlari | YENİ | id VARCHAR(36) PK · tenant_id VARCHAR(36) · irsaliye_id VARCHAR(36) · sira INT · urun_id VARCHAR(36) NULL · hareket_id VARCHAR(36) NULL · cins VARCHAR(300) (ürün kartı yoksa serbest metin) · adet DECIMAL(12,3) · birim VARCHAR(10) ('ADET'|'GRAM') · ayar_id VARCHAR(10) NULL · gram_agirlik DECIMAL(12,4) NULL · has_grami DECIMAL(15,6) NULL · tas_ibaresi VARCHAR(60) NULL (sevk anında DONDURULUR) · aciklama VARCHAR(500) NULL | (tenant_id, irsaliye_id) · (tenant_id, hareket_id) — 'bu hareket sevk edildi mi' anti-join'i buradan okur | aynı migration |
| hareketler | DEĞİŞMEZ — bilinçli | İrsaliye numarası hareketler'e KOLON OLARAK EKLENMEZ. 'Bu hareket sevk edildi mi' sorusu satır tablosundaki (tenant_id, hareket_id) indeksinden anti-join ile cevaplanır. Gerekçe: 22 migration'lık sıcak tabloyu bir belge kavramı için genişletmemek. fatura_no'nun hareketler'de olması DEVRALINAN bir durumdur, tekrar edilmez. ⚠ KARŞI GÖRÜŞ VE BEDELİ: fatura deseniyle asimetri doğar ve 'irsaliyesiz sevk' sorgusu anti-join gerektirir (indeksle karşılanır). | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/irsaliyeler` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | İrsaliye oluştur (TASLAK). Gövde: tur, cariId?, karsiSubeId?, teslimAlanAd, teslimAdresi?, tasiyanAd?, plaka?, fiiliSevkTarihi, aciklama?, satirlar[] (urunId? | cins, adet, birim, ayarId?, gramAgirlik?, hareketId?). Numara oluşturma anında verilir. | şube/cari kiracı doğrulaması (IslemDogrulayici.SubeDogrula) · en az 1 satır · adet > 0 · fiili sevk tarihi geleceğe verilemez · taş kökeni (türe göre) |
| GET | `/api/v1/irsaliyeler?durum=&baslangic=&bitis=&limit=&offset=` | tümü (modül yetkisi 'islem') | Sayfalı liste; tarih aralığı verilmezse son 30 gün (TB-19). Satırlar liste yanıtında GELMEZ (N+1 ve şişme). | kiracı filtresi |
| GET | `/api/v1/irsaliyeler/{id}` | tümü | Başlık + satırlar. | kiracı filtresi |
| PATCH | `/api/v1/irsaliyeler/{id}/durum` | SEVK/TESLIM: TEZGAHTAR+ · IPTAL: SAHIP, MUDUR | Durum geçişi. TASLAK→SEVK_EDILDI→TESLIM_EDILDI. IPTAL için gerekçe ≥ 20 karakter ve MUDUR+. | geçiş matrisi (FaturaModulu.cs:71-77 deseni) · IPTAL'den çıkış yok · iptal gerekçesi · DenetimServisi.YazmaKaydet |
| PATCH | `/api/v1/irsaliyeler/{id}` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Yalnız TASLAK durumunda başlık/satır düzenleme. SEVK_EDILDI sonrası 400. | durum kapısı |
| GET | `/api/v1/irsaliyeler/{id}/belge` | tümü | Yazdırılabilir belge verisi (ADR-0007). İşletme unvanı/şube, taraflar, satırlar (cins, miktar, birim, ayar, gram, taş ibaresi), fiili sevk tarihi/saati, taşıyan/plaka, durum. Okuma izi düşer (HurdaModulu.cs:67 deseni) — paket 48'in 'basım izi' kalemiyle uyumlu. | kiracı filtresi + DenetimServisi.OkumaKaydet |
| GET | `/api/v1/raporlar/irsaliyesiz-sevk?baslangic=&bitis= (FAZ B)` | SAHIP, MUDUR, MUHASEBE | Belirtilen aralıkta mal çıkışı doğuran hareketler (SATIS, TRANSFER, konsinye gönderimi) içinde hiçbir irsaliye satırına bağlı OLMAYANLAR. Anti-join, sunucuda sayılır ve sayfalanır. | [ModulYetkisi("rapor")] |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Data/SevkIrsaliyesi.cs` — SevkIrsaliyesi ve SevkIrsaliyeSatiri varlıkları, ikisi de IKiraciVarlik. Başlıkta neden iki tablo olduğu, neden hareketler'e kolon eklenmediği ve neden stok hareketi doğurmadığı yazılır (GiderPusulasi.cs:6-32 başlık deseni).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs` — IrsaliyeOlusturDto / SatirDto / DurumGuncelleDto + IrsaliyeServisi + IrsaliyeController. Controller [ApiController][Authorize][Route("api/v1/irsaliyeler")][Yetki.ModulYetkisi("islem")]. Durum geçiş sözlüğü FaturaModulu.cs:71-77 ile aynı biçimde static readonly Dictionary.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Common/FaturaNoUretici.cs` — SiradakiIrsaliye(db, tenantId, yil) eklenir — 'SI-{yıl}-' öneki, MAX+1 (COUNT+1 DEĞİL; satır 11-13'teki gerekçe: defterde boşluk varsa mevcut numarayı yeniden üretir ve benzersizlik kısıtını ihlal eder).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeKapisi.cs` — Taş kökeni kapısı: satırda urunId varsa ve ürünün tas_agirlik'i doluysa tas_kokeni kontrol edilir. MUSTERI_TESLIM ve KONSINYE_GONDERIM türlerinde kökensiz mal → 400 (FAIL-CLOSED, ADR-0013 ile aynı gerekçe: mal dükkândan müşteriye çıkıyor). SUBE_TRANSFER, ATOLYE_GONDERIM, TEDARIKCI_IADE türlerinde yalnız UYARI döner, durdurmaz — mal işletmenin içinde kalıyor ya da geri gidiyor. Bu ayrım ADR-0013'ün GENİŞLETİLMESİDİR ve oylamaya açıktır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — FAZ B: irsaliyesiz-sevk ucu (controller satır 513-551 bölgesi), servis gövdesi HasDefteriServisi ya da yeni bir servise.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Irsaliye.IrsaliyeServisi>(); + s.AddScoped<Irsaliye.IrsaliyeKapisi>();
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki DbSet + 5 indeks (satır 100-190 bölgesi). Kapsama denetimi (satır 194-201) yeni varlıkları otomatik zorlar.
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — "Irsaliye": { "_not": "Faturalama süresi DOĞRULANMALI; kapı DEĞİL uyarıdır.", "FaturaGunu": 7, "FaturaUyarisiEtkin": false }

## Ekranlar
- `/irsaliyeler` · `dotnet/Kuyumcu.Web/Pages/Irsaliyeler.razor` — Liste (durum süzgeci, tarih aralığı, sayfalı) + 'Yeni irsaliye' modalı: tür seçici, tür'e göre değişen alanlar (SUBE_TRANSFER'de karşı şube, diğerlerinde cari + teslim adresi), satır ekleme (ürün seç ya da serbest cins), taşıyan/plaka, fiili sevk tarihi (geleceğe seçilemez). Satır yokken ya da adet 0 iken Kaydet KAPALI. Detayda durum düğmeleri; İptal'de gerekçe < 20 karakter → Kaydet KAPALI. 'Yazdır' → belge sayfası. (menü/yetki: KenarCubugu.razor:26-30 bloğuna '🚚 İrsaliyeler' eklenir; modül 'islem' (TEZGAHTAR dâhil — mal sevkini tezgâhtar yapar).)
- `/irsaliyeler/{Id}/belge` · `dotnet/Kuyumcu.Web/Pages/IrsaliyeBelgesi.razor` — Yazdırılabilir sevk irsaliyesi. Ortak BelgeYerlesimi bileşenini kullanır. İşletme unvanı/şube, gönderen–alan blokları, satır tablosu (sıra, cins, ayar, miktar+birim, gram, has, taş ibaresi), fiili sevk tarihi VE SAATİ, taşıyan/plaka, imza alanları. İPTAL durumunda belge üzerine 'İPTAL EDİLDİ' filigranı basılır (belge silinmez). (menü/yetki: Menüde yok; listeden açılır.)
- `—` · `dotnet/Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor + wwwroot/css/belge.css` — ADR-0007 Not maddesinin karşılığı: ortak belge kabuğu (üst başlık, dayanak satırı, taraf blokları, tablo, alt not, yazdır/geri araç çubuğu, yazdirma-gizle sınıfı). PusulaBelgesi.razor TAŞINMAZ (izinsiz değişiklik yok); yeni belgeler bunu kullanır. CSS sınıf adları önce grep'lenir (kayıtlı tuzak: '.etiket' çakışması) — önek 'belge-'. (menü/yetki: —)

## İş kuralları (fail-closed)
- İRSALİYE STOK HAREKETİ DOĞURMAZ. Stoğu satış/transfer hareketi düşürür. İrsaliye de düşseydi transferle ÇİFT düşerdi. Bağ satirlar.hareket_id ile kurulur, opsiyoneldir.
- Numara SI-{yıl}-{sıra:6}, MAX+1. COUNT+1 kullanılsaydı iptal/boşluk hâlinde mevcut numara yeniden üretilir ve benzersizlik kısıtı ihlal edilirdi (FaturaNoUretici.cs:11-13'te kayıtlı hata).
- Fiili sevk tarihi ZORUNLU ve GELECEĞE verilemez. Sevk irsaliyesi malın fiilen çıktığı anı belgeler; ileri tarihli bir belge o anı belgelemez.
- En az 1 satır; her satırda adet > 0. urunId verilmişse ürün kiracıya ait olmalı ve cins ürün adından doldurulur.
- TAŞ İBARESİ SEVK ANINDA DONDURULUR (satıra kopyalanır). Ürün kartı sonra değişse bile BASILMIŞ belge değişmez — basılan kâğıtla ekran ayrışmamalı.
- TAŞ KÖKENİ: MUSTERI_TESLIM ve KONSINYE_GONDERIM'de kökensiz mal → 400 (fail-closed). SUBE_TRANSFER/ATOLYE_GONDERIM/TEDARIKCI_IADE'de yalnız uyarı. Gerekçe: ADR-0013 malın müşteriye çıkışını hedefler; işletme içi hareketi durdurmak orantısız olurdu.
- DURUM MAKİNESİ: TASLAK→{SEVK_EDILDI, IPTAL}, SEVK_EDILDI→{TESLIM_EDILDI, IPTAL}, TESLIM_EDILDI→{IPTAL}, IPTAL→{} (FaturaModulu.cs:71-77 ile birebir aynı biçim). İPTAL'den çıkış yoktur.
- SEVK_EDILDI'den sonra satır ve başlık DEĞİŞMEZ. Belge basılmıştır; düzeltme = iptal + yeni irsaliye (append-only ilkesi, MasakVarliklari.cs:18-19).
- İPTAL KAYIT SİLMEZ. Gerekçe (≥ 20 karakter), iptal eden ve zamanı satırda saklanır; belge 'İPTAL EDİLDİ' filigranıyla basılabilir kalır.
- Belge ucu OKUMA İZİ düşürür (KVKK md.12 deseni, HurdaModulu.cs:67). Durum değişikliği ve iptal YAZMA izi düşürür (paket 40'ın eklediği DenetimServisi.YazmaKaydet).
- e-İrsaliye ibaresi belgede YER ALMAZ ve XML üretilmez (ADR-0012 gerekçesi).
- Faturalama süresi uyarısı KAPI DEĞİLDİR ve varsayılan KAPALIDIR (Irsaliye:FaturaUyarisiEtkin=false); süre mevzuat dayanağı doğrulanana kadar iş durdurmaz.

## Testler
- `dotnet/Kuyumcu.Tests/IrsaliyeTestleri.cs` [DB]
  - Oluşturma: numara SI-2026-000001, ikinci irsaliye 000002
  - İptal edilen irsaliyeden sonra yeni irsaliye 000003 (MAX+1; COUNT+1 olsaydı 000002 tekrarlanırdı)
  - Satırsız gövde → 400; adet 0 olan satır → 400
  - Fiili sevk tarihi yarın → 400
  - SEVK_EDILDI durumunda PATCH ile satır değiştirme → 400
  - Durum geçişleri: TASLAK→TESLIM_EDILDI doğrudan → 400; IPTAL→TASLAK → 400
  - İptal gerekçesiz → 400; gerekçeli → 200 ve KAYIT DURUYOR (silinmedi), gerekçe+kullanıcı+zaman dolu
  - Taş ağırlığı olup kökeni olmayan ürün MUSTERI_TESLIM'de → 400; SUBE_TRANSFER'de → 200 + uyari alanı dolu
  - Taş ibaresi satıra KOPYALANIR; ürün kartındaki ibare sonradan değiştirilince satır DEĞİŞMEZ
  - İRSALİYE STOK DÜŞÜRMEZ: oluşturma öncesi/sonrası stoklar.adet aynı
  - Yabancı kiracının ürünüyle satır → 404; yabancı kiracının irsaliyesi → 404
  - Belge ucu denetim_kayitlari'na okuma kaydı düşürür
  - FAZ B: irsaliyesiz-sevk raporu, irsaliyeye bağlanan satışı listeden ÇIKARIR
- `dotnet/Kuyumcu.Tests/IrsaliyeEkranTestleri.cs` [bUnit]
  - Satır eklenmeden Kaydet KAPALI (disabled="@(false)" tuzağı)
  - Tür SUBE_TRANSFER seçilince karşı şube alanı çıkar, cari alanı GİZLENİR
  - Fiili sevk tarihi geleceğe seçilince Kaydet KAPALI
  - İptal modalında gerekçe 19 karakterde KAPALI, 20'de AÇIK
  - Gövdede satirlar dizisi doğru alan adlarıyla gider (adet, birim, urunId)
  - Liste sayfalaması sunucuya offset taşır
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - TEZGAHTAR irsaliye OLUŞTURABİLİR (200) ama İPTAL EDEMEZ (403)
  - Belge ucu kimliksiz → 401

## Mutasyon kanıtları
- Numara üretimi COUNT+1'e çevrilirse → 'iptal sonrası numara tekrarlanmaz' KIRMIZI
- Fiili sevk tarihi gelecek kontrolü kaldırılırsa → 'yarın tarihli → 400' KIRMIZI
- SEVK_EDILDI sonrası düzenleme kapısı kaldırılırsa → 'sevk sonrası satır değişmez' KIRMIZI
- Durum geçiş matrisi yerine serbest geçiş yazılırsa → 'TASLAK→TESLIM_EDILDI 400' ve 'IPTAL→TASLAK 400' KIRMIZI
- İptal gerekçe kontrolü kaldırılırsa → 'gerekçesiz iptal 400' KIRMIZI
- İptal kaydı silerse → 'iptal sonrası kayıt duruyor' KIRMIZI
- Taş ibaresi satıra kopyalanmayıp ürün kartından okunursa → 'ürün değişince satır değişmez' KIRMIZI
- Taş kökeni kapısı TÜM türlerde fail-closed yapılırsa → 'SUBE_TRANSFER'de 200' KIRMIZI (orantılılık kaybı yakalanır)
- Taş kökeni kapısı tamamen kaldırılırsa → 'MUSTERI_TESLIM'de 400' KIRMIZI
- İrsaliye stok düşürürse → 'stok değişmez' KIRMIZI
- Belge ucundan okuma izi kaldırılırsa → 'denetim kaydı düşer' KIRMIZI
- Ekranda satırsız Kaydet açık bırakılırsa → ekran testi KIRMIZI

## Canlı doğrulama
- Yeni kiracı (IRSALIYE-TEST). İki şube, bir cari, taşlı (kökenli) ve taşlı (kökensiz) iki ürün aç.
- SUBE_TRANSFER irsaliyesi, 2 satır → 200, numara SI-2026-000001. SQL: başlık + 2 satır, stoklar DEĞİŞMEDİ.
- Kökensiz taşlı ürünle MUSTERI_TESLIM irsaliyesi → 400. SQL: yeni satır yazılmadı.
- Aynı ürünle SUBE_TRANSFER → 200 + yanıtta uyarı alanı dolu.
- Fiili sevk tarihini yarına ver → 400.
- Satırsız gövde → 400.
- TASLAK→TESLIM_EDILDI doğrudan → 400. TASLAK→SEVK_EDILDI → 200. SEVK sonrası PATCH ile satır değiştir → 400.
- Gerekçesiz iptal → 400; 25 karakterlik gerekçeyle → 200. SQL: durum='IPTAL', gerekçe/kullanıcı/zaman dolu, satırlar DURUYOR.
- Yeni irsaliye aç → numara SI-2026-000003 (000002 iptal edilmişti, tekrarlanmadı).
- Belge sayfasını aç: tarayıcı yazdırma önizlemesinde menü/kenar çubuğu GÖRÜNMEMELİ; taş ibaresi satırda basılmalı; iptal edilen belgede filigran görünmeli.
- Ürün kartındaki taş ibaresini değiştir, basılmış irsaliyenin belgesini tekrar aç → İBARE DEĞİŞMEMELİ.
- SQL: denetim_kayitlari'nda belge okuma kaydı ve iptal yazma kaydı var.
- Migration ileri/geri: Down iki tabloyu da düşürmeli, Up geri kurmalı.
- Test kiracısını sil (API'yi önce durdur).

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/SevkIrsaliyesi.cs`
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Common/FaturaNoUretici.cs`
- `dotnet/Kuyumcu.Api/Data/DenetimIzi.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Irsaliyeler.razor`
- `dotnet/Kuyumcu.Web/Pages/IrsaliyeBelgesi.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/belge.css`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/denetim.sh`

## Mevzuat dayanağı
- VUK md.230/5 — malın taşınması/sevki hâlinde sevk irsaliyesi düzenlenmesi ve irsaliyede bulunması gereken bilgiler — **DOGRULANMALI** — Belgenin ZORUNLU ALAN LİSTESİ (gönderen, alıcı, tarih, saat, taşıyan, adres, malın cinsi ve miktarı) birincil kaynaktan teyit edilmeden belge şablonu kesinleşmemeli. Tasarım bu alanların tümünü TAŞIR; teyit sonrası eksik çıkarsa alan eklenir, fazla çıkarsa kaldırılmaz (fazla bilgi belgeyi geçersiz kılmaz). Bu yüzden teyit gecikmesi paketi bloke etmez.
- VUK md.231/5 — faturanın malın tesliminden itibaren belirli süre içinde düzenlenmesi — **DOGRULANMALI** — Süre (7 gün) doğrulanmadı. Bu yüzden 'süresinde faturalanmamış irsaliye' UYARIDIR, kapı değildir; Irsaliye:FaturaGunu yapılandırmadan okunur ve Irsaliye:FaturaUyarisiEtkin VARSAYILAN FALSE'tur.
- Kuyum Ticareti Yönetmeliği md.11/(e) — sentetik/laboratuvar üretimi/yapay üretim ibaresi — **DOGRULANMALI** — ADR-0013 ile satışta fail-closed olarak zaten kabul edilmiş ve kodda uygulanıyor (Entities.cs:193-201). İbarenin İRSALİYEDE de yer alması gerektiği DOĞRULANMADI; tasarım ibareyi taşır ama kapıyı yalnız malın müşteriye çıktığı türlerde fail-closed kurar. Bu, ADR-0013'ün genişletilmesidir ve OYLAMA gerektirir.
- e-İrsaliye — **DOGRULANMALI** — ADR-0012 gerekçesiyle kapsam dışı: entegratör sözleşmesi (K1) olmadan sahte entegrasyonla yapılmaz. PARITE §4'te C sınıfı. Bu paket e-İrsaliye'nin ÖN ŞARTIDIR (PARITE paket 42 tanımı), kendisi değildir.

## Riskler
- Dördüncü yazdırılabilir belge olduğu için ADR-0007'nin ortak yerleşim borcu bu pakette ödenmek zorunda. Bu, pakete yarım gün ekler ama 39'un ve sonraki belgelerin maliyetini düşürür.
- CSS sınıf adı çakışması gerçek bir tuzak ('.etiket' daha önce ısırdı). Yeni sınıflar 'belge-' önekiyle ve önce grep'lenerek eklenir.
- Tür bazlı taş kökeni kapısı (fail-closed / uyarı ayrımı) ADR-0013'ü genişletiyor. Oylanmadan sevk EDİLMEZ; oylama olumsuzsa kapı tamamen uyarıya indirilir ve tek satır değişir.
- hareketler'e kolon eklememe kararı fatura desenine asimetriktir ve sonraki bir oturum 'neden fatura_no orada, irsaliye_no burada' diye sorabilir. Gerekçe hem varlık başlığına hem bu belgeye yazıldı.
- İrsaliye ile mevcut TRANSFER akışı arasındaki bağ Faz B'ye bırakıldı. Faz A'da kullanıcı irsaliyeyi elle açar; bu, çift veri girişi demektir ve Faz B'ye kadar bilinen bir sürtünmedir.
- hareketler.adet olmadığı için Faz B'deki 'irsaliye oluştur' kısayolu miktarı hareketten dolduramaz; kullanıcıya sorar. Bu da 'hareketler.adet' paketini işaret eder.

## Açık sorular
- VUK md.230/5'in istediği zorunlu alan listesi nedir? (Belge şablonu teyit sonrası kesinleşir.)
- Taş ibaresi irsaliyede de zorunlu mu? ADR-0013'ün genişletilmesi oylanacak mı?
- İrsaliye numarası kiracı+yıl bazında mı yoksa kiracı+şube+yıl bazında mı olmalı? (Tasarım kiracı+yıl seçti; çok şubede kâğıt koçan ayrımı isteniyorsa değişir.)
- Faturalama süresi kaç gün ve uyarı varsayılan açık mı olmalı?
- İrsaliyeli fatura (tek belge) isteniyor mu? Bugün kapsam dışı.
- 'hareketler.adet' kolonu ayrı bir paket olarak açılsın mı? (Paket 39 ve 42 ikisi de buna çarpıyor.)
- Belge ucunun okuma izi düşürmesi, gün içinde defalarca yazdırılan bir irsaliyede denetim izini gürültüye boğar mı? (Gider pusulasında aynı desen zaten var.)

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Sıfırdan doğru tespit edilmiş gerçek bir boşluk (0 isabet). İki tablo (başlık + satır) kararı doğru ve TB-17'nin faturada yaptığı hatanın tekrarını önlüyor. İrsaliyenin stok hareketi DOĞURMAMASI (stoğu satış/transfer düşürür) çift düşme riskini kapatıyor — mimari olarak en kritik doğru karar. Taş ibaresinin sevk anında satıra DONDURULMASI, basılan kâğıtla ekranın ayrışmaması ilkesiyle tutarlı. İptalin kayıt silmemesi append-only disipliniyle uyumlu. Koşullarım sahiplik çakışması ve bayat ölçümler üzerine.
  - Koşullar: BELGEYERLESIMI SAHİPLİĞİ — BAĞLAYICI ÇAPRAZ KARAR: Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor + wwwroot/css/belge.css BU PAKETTE doğar. K0'ın paket 5'i (belge/etiket şablon motoru) aynı bileşeni kurduğunu yazıyor; paket 5 onu GENİŞLETİR, YENİDEN YAZMAZ. İki paket aynı bileşeni ayrı ayrı kurarsa iki print standardı doğar ve ADR-0007'nin notu ödenmek yerine ikiye bölünür. · MEVCUT DURUM METNİ DÜZELTİLECEK: yazdırılabilir yüzey sayısı 1 değil 3'tür (PusulaBelgesi.razor, GiderPusulalari.razor, UrunEtiketleri.razor). İrsaliye dördüncüdür. Yanlış sayı, ADR-0007 borcunun neden TAM BURADA ödendiğini zayıflatıyor — düzeltilince gerekçe güçleniyor. · 'DenetimServisi.YazmaKaydet paket 40'ın eklediği' İFADESİ DÜZELTİLECEK: metot DenetimIzi.cs:120'de ZATEN VAR. Dolayısıyla paket 42'nin 40'ı beklemesi için bir sebep yok ve K6'nın 42→39→40 sıra gerekçesinin bu ayağı düşer. · TAŞ KÖKENİ TÜR BAZLI AYRIMI (MUSTERI_TESLIM/KONSINYE_GONDERIM fail-closed, diğerleri uyarı) ADR-0013'ü GENİŞLETİYOR → yeni bir ADR kaydı olarak yazılıp OYLANACAK, oylanmadan kod yazılmayacak. Spesifikasyon bunu kendisi söylüyor; kabul ölçütüne bağlansın. · OKUNMADI — ZORUNLU ÖLÇÜM: FaturaNoUretici.cs:11-13 ve 41-59'daki 'MAX+1, COUNT+1 DEĞİL' gerekçesi bu oturumda açılmadı. Uygulayıcı önce dosyayı açıp mevcut deseni ölçsün, sonra SiradakiIrsaliye'yi aynı desende yazsın. · Faz A / Faz B AYRILACAK ve BelgeYerlesimi Faz A'ya dâhil edilecek (3-4 gün tahmini o bileşeni içermiyor gibi duruyor; +0,5 gün açıkça eklensin). · hareketler'e irsaliye_no kolonu EKLENMEMESİ kararı KORUNSUN ve gerekçesi (fatura_no devralınan bir durumdur, tekrarlanmaz) varlık başlığına yazılsın. Asimetri bilinçlidir; yazılmazsa sonraki oturum 'tutarsızlık' diye düzeltmeye kalkar. · İrsaliye numarası KİRACI+YIL bazında kalsın (açık soru kapatılıyor): şube bazlı koçan ayrımı bugün talep edilmedi ve benzersizlik kısıtını karmaşıklaştırır. İstenirse ayrı paket.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Belge modeli, numara MAX+1, durum makinesi, append-only iptal, okuma izi — mühürlü desenlerle uyumlu; stok hareketi doğurmuyor (Hareketler.Add=1 korunur). Eksikler: rol kapısının attribute düzeyinde kurulamayacak biçimde tasarlanması ve irsaliyedeki adres/plaka kişisel verisinin hassas varlık listesine alınmaması.
  - Koşullar: K42-1 (rol kapısı attribute düzeyinde): tek PATCH /durum ucunda gövdeye göre rol değişemez (RolGerekli attribute'tür, RolFiltresi.cs:10-19). IPTAL için AYRI uç `PATCH /irsaliyeler/{id}/iptal` [RolGerekli('SAHIP','MUDUR')] açılır; /durum ucu yalnız SEVK_EDILDI/TESLIM_EDILDI geçişlerini alır ve IPTAL değerini 400 ile reddeder. HTTP testi 'TEZGAHTAR /iptal 403' bu uca yazılır. Servis içi rol kontrolü YAZILMAZ (mimari değişmez). · K42-2: iptal ve durum geçişleri DenetimServisi.YazmaKaydet (MEVCUT, DenetimIzi.cs:120; yeniden yazılmaz) ile çağıranın tx'i içinde, tek SaveChanges. · K42-3: teslim_alan_ad/teslim_adresi/tasiyan_ad/plaka kişisel veridir → 'SevkIrsaliyesi' HassasVarliklar'a eklenir; /belge ucu OkumaKaydet (yazılı), GET /{id} detay ucu da OkumaKaydet; liste ucu adres/plaka DÖNMEZ (yalnız no/tarih/tür/durum/karşı taraf adı) ve ListeOkumaKaydet düşürür. · K42-4: cariId/karsiSubeId/urunId kiracı sınırında doğrulanır (IslemDogrulayici.SubeDogrula, Cariler/Urunler AnyAsync TenantId ile); satırdaki hareketId verilirse `h.TenantId == islem.TenantId` ile var olduğu doğrulanır, yabancı → 404. · K42-5: Taş kökeni kapısının tür bazlı fail-closed/uyarı ayrımı ADR-0013 genişletmesidir — güvenlik lensinden KABUL (müşteriye çıkışta fail-closed korunuyor, işletme içi hareket durdurulmuyor); oylamaya bırakılması doğru, ben EVET. · K42-6: 'İPTAL EDİLDİ' filigranı ve sevk sonrası değişmezlik KABUL; SEVK_EDILDI sonrası PATCH 400 kuralı KiraciYazmaDenetcisi düzeyinde değil servis düzeyinde — yeterli, çünkü ExecuteUpdate beyaz listesi (4) değişmiyor.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Belge modeli (başlık+satır, adet satırda — hareketler'de adet olmadığı doğru tespit), durum makinesi (FaturaModulu.cs:71-77 deseni), MAX+1 numara ve stok düşürmeme kuralı test edilebilir ve mutasyonlar test adlarıyla eşleşiyor. Taş kökeni genişletmesi oylama bekliyor; YazmaKaydet iddiası bayat; sevk tarihi kuralı K5 tuzağına açık.
  - Koşullar: IrsaliyeKapisi'nin tür bazlı fail-closed/uyarı ayrımı ADR-0013 genişletmesidir; oylanmadan 'MUSTERI_TESLIM 400 / SUBE_TRANSFER 200+uyarı' testleri yazılmaz (kabul ölçütü karara bağlı). Karar 'hepsi uyarı' çıkarsa mutasyon listesinden iki madde düşer. · 'DenetimServisi.YazmaKaydet (paket 40'ın eklediği)' BAYAT: zaten var (DenetimIzi.cs), SaveChanges çağırmaz → iptal/durum geçişi izi aynı SaveChanges'te yazılır; onkosul 40 kalkar. Test: iptal sonrası denetim_kayitlari'nda eylem=EKLEME varlik_turu=SevkIrsaliyesi satırı +1. · 'Fiili sevk tarihi geleceğe verilemez' YEREL güne göre kurulur (Zaman.YerelBugun / YerelGunAraligiUtc, K5): test İstanbul 23:30 (20:30 UTC) bugünkü tarihle 200, yerel yarın 00:30 ile 400. UtcNow ile ham karşılaştırma İstanbul gecesinde bugünü 'yarın' sayar. · 'İRSALİYE STOK DÜŞÜRMEZ' testine 'hareketler satır sayısı değişmez' assert'i eklenir (denetim.sh:58 Hareketler.Add=1 korunur); stoklar.adet öncesi/sonrası SQL. · Numara yarışı: iki eşzamanlı POST aynı MAX+1'i üretip (tenant_id, irsaliye_no) UNIQUE ihlaline düşebilir; FaturaNoUretici davranışı (Siradaki) bu oturumda okunmadı — uygulayıcı ihlalde 409/yeniden deneme davranışını Fatura yolundakiyle AYNI yapar ve tek testle sınar (Parallel iki oluşturma → biri 200, diğeri 409 ya da ikisi ardışık numara). · 'Yabancı kiracının ürünüyle satır → 404' çok katmanlı koruma → regresyon etiketi ya da filtresiz bağlam. · IrsaliyeEkranTestleri: tür seçimi/alan gizleme/iptal gerekçe 19→20 karakter testleri WaitForAssertion; 'Kaydet KAPALI' disabled varlığı; sayfalama offset SahteApi yolundan okunur. · belge.css / BelgeYerlesimi sınıf adları 'belge-' önekiyle ve önce grep ('.etiket' çakışması §6); yazdırma kabuk gizlemesi bUnit ile sınanamaz → canlı doğrulama adımı 'tarayıcı yazdırma önizlemesi' tek kanıttır, ekran görüntüsü kaydedilir. · TEZGAHTAR 'oluşturabilir 200 / iptal 403' HTTP testi: ModulYetkisi('islem') varsayılan matriste TEZGAHTAR'a açık (YetkiModulu.cs:29-34) ✓; iptal [RolGerekli('SAHIP','MUDUR')] TAM eşleşme.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Veto YOK. Üç mercek de KOSULLU; taş kökeni tür bazlı ayrımı Mehmet'ten (K42-5) güvenlik onayı aldı, ama ADR genişletmesi olarak ayrıca oylanmadan kod yazılmaz (Bertan + Aşkın ortak koşulu).
- BelgeYerlesimi.razor + wwwroot/css/belge.css BU PAKETTE doğar (Bertan bağlayıcı çapraz karar). UX-2 Faz C ve paket 5 aynı bileşeni yeniden yaratmaz; Faz A'ya dâhil edilir ve tahmine +0,5 gün açıkça eklenir.
- BAYAT MADDE (Bertan + Aşkın, aynı bulgu): 'YazmaKaydet paket 40'ın eklediği' YANLIŞ — DenetimIzi.cs:120'de ZATEN VAR. Dolayısıyla 42'nin 40'ı beklemesi gereksiz; 42→39→40 sıralama zorunluluğu kalkar.
- MEVCUT DURUM DÜZELTMESİ: yazdırılabilir yüzey 1 değil 3'tür (PusulaBelgesi.razor, GiderPusulalari.razor, UrunEtiketleri.razor); irsaliye DÖRDÜNCÜdür — ADR-0007'nin ortak yerleşim borcu bu pakette ödenir.
- K42-1 rol kapısı attribute düzeyinde: tek PATCH /durum ucunda gövdeye göre rol değişemez (RolFiltresi.cs:10-19). IPTAL için AYRI uç PATCH /irsaliyeler/{id}/iptal. TEZGAHTAR 'oluşturur 200 / iptal 403' HTTP testiyle kanıtlanır.
- K42-3 kişisel veri: teslim_alan_ad/teslim_adresi/tasiyan_ad/plaka → SevkIrsaliyesi HassasVarliklar'a eklenir; /belge ve GET /{id} uçları OkumaKaydet düşer. K42-2: iptal/durum geçişi izi çağıranın tx'inde tek SaveChanges ile yazılır.
- K42-4 kiracı sınırı: cariId/karsiSubeId/urunId IslemDogrulayici.SubeDogrula + AnyAsync TenantId ile doğrulanır; satır hareketId verilirse h.TenantId == islem.TenantId şartı. Yabancı kiracı ürünüyle satır → 404 regresyon etiketi.
- 'Fiili sevk tarihi geleceğe verilemez' YEREL güne göre (Zaman.YerelBugun / YerelGunAraligiUtc): test İstanbul 23:30 (20:30 UTC) 200, yerel yarın 00:30 400. 'İrsaliye stok düşürmez' testine 'hareketler satır sayısı değişmez' assert'i (denetim.sh:58).
- OKUNMADI — ZORUNLU ÖLÇÜM: FaturaNoUretici.cs:11-13 ve 41-59 'MAX+1, COUNT+1 DEĞİL' deseni bu oturumda açılmadı; uygulayıcı önce dosyayı okur, SiradakiIrsaliyeNo'yu ona göre kurar ve UNIQUE yarışında 409/yeniden dene davranışını belirler.
- İrsaliye no KİRACI+YIL bazında kalır (açık soru kapandı). hareketler'e irsaliye_no kolonu EKLENMEZ, asimetri gerekçesi varlık başlığına yazılır. belge.css sınıfları 'belge-' önekli ve önce grep'li ('.etiket' çakışması); yazdırma canlı önizlemeyle doğrulanır.
