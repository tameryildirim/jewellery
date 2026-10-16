# Paket 38 — KDV özel matrah otomatiği (kuyum KDV'si) + özel matrah neden kodu

> Katman: Katman 6 — Uyum (paket 38–42) · Tahmin: 2–3 (Faz A 2, Faz B 1) gün · Önkoşullar: yok · Kaynak: katman mimarı K6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 2

## Hedef
Kuyum KDV'li faturada matrahtan düşülen has altın bedeli bugün kasiyerin elle yazdığı bir rakamdır; yanlış yazılırsa KDV eksik ya da fazla beyan edilir ve düzeltmenin dayanağı hiçbir yerde durmaz. Bu paket o rakamı faturalandırılan HAREKETTEN türetir: has gram × o işlemin kur kaydındaki has fiyatı. Ayrıca özel matrah neden kodunu (yapılandırmadan, varsayılan 805) faturaya yazar ve belgeye basar. Türetimin girdisi olan fatura↔hareket bağı bugün hiçbir ekrandan gönderilmiyor — paket onu da bağlar.

## Kodda bugünkü durum
HasDeger DTO alanı opsiyoneldir ve elle gelir (FaturaModulu.cs:40-42); servis onu koşulsuz kabul eder: 'hasDeger = dto.HasDeger ?? 0m' (FaturaModulu.cs:98). KDV hesabı türe göre dallanır (FaturaModulu.cs:96-107), tek doğrulama 'hasDeger >= toplam' (FaturaModulu.cs:99-100). Fatura varlığı has_deger/kdv_turu kolonlarını taşır (Entities.cs:485, Entities.cs:487). Türetimin girdileri KODDA HAZIR: hareketler.has_grami (Entities.cs:281) ve kur_snapshots.altin_has_fiyati (Entities.cs:104); SatisModulu satışta bu kuru zaten okuyor (SatisModulu.cs:460-463). ⚠ ÖLÇÜM: FaturaOlusturDto.HareketId (FaturaModulu.cs:57) ve fişin tüm kalemlerini damgalayan blok (FaturaModulu.cs:121-144) vardır, ama 'hareketId' Kuyumcu.Web altında 0 KEZ geçer (grep, .razor+.cs) ve Faturalar.razor'un POST gövdesinde yoktur (Faturalar.razor:297). Yani bugün ekrandan kesilen hiçbir fatura deftere bağlanmıyor; gün sonu mutabakatı (RaporModulu.cs:156) her satışı 'faturasız' sayıyor. ⚠ KDV oranı iki yerde koda gömülü: 'dto.KdvOran ?? 0.20m' (FaturaModulu.cs:87) ve (BankaModulu.cs:268) — ADR-0005 ihlali. ⚠ BankaServisi.FaturayaDonustur KdvTuru'nu HİÇ yazmıyor (BankaModulu.cs:278-296), varsayılan 'STANDART' devreye giriyor (Entities.cs:485). Kod tabanında '805' / 'özel matrah' 0 isabet.

## Kapsam
**İçinde:**
- faturalar tablosuna özel matrah kanıt alanları (has gram, has birim fiyat, kaynak, neden kodu)
- POST /faturalar: HareketId verilmişse has bedelinin SUNUCUDA türetilmesi ve istemci değeriyle kuruş-kuruş tutarlılık kapısı
- GET /faturalar/ozel-matrah-onizleme: Kaydet'e basmadan önce türetilen matrah kırılımı
- Faturalar.razor: KUYUM seçilince 'faturalandırılacak işlem' seçici + hareketId gönderimi + salt okunur has bedeli
- Kdv:VarsayilanOran ve Kdv:OzelMatrahKodu yapılandırması (ADR-0005)
- BankaServisi.FaturayaDonustur'da KdvTuru'nun AÇIKÇA yazılması (sessiz varsayılan kaldırılır)
- Yazdırılabilir fatura çıktısında özel matrah kırılımı (Faz B)

**Dışında:**
- Fatura satır kalemi modeli (TB-17) — PARITE paket 1'in işi; bu paket TEK fiş/tek hareket kümesi düzeyinde çalışır ve paket 1 geldiğinde satır bazına iner
- e-Fatura/e-Arşiv UBL üretimi ve GİB kod listesi eşlemesi (ADR-0012, K1 sözleşmesi)
- KDV beyannamesi üretimi
- Eski faturaların geri-doldurulması (has bedelinin dayanağı veride yok)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| faturalar | 4 yeni kolon (hepsi NULL kabul eder; eski satırlar dokunulmaz) | ozel_matrah_kodu VARCHAR(5) NULL — yalnız KdvTuru='KUYUM' satırlarda dolar, HESABA GİRMEZ, etikettir · has_gram DECIMAL(15,6) NULL — matrahtan düşülen has GRAM (bedelin dayanağı; gram olmadan denetimde 'bu 41.000 TL nereden çıktı' sorusu cevapsız kalır) · has_birim_fiyat DECIMAL(15,4) NULL — kullanılan has kuru · has_deger_kaynagi VARCHAR(10) NULL — 'HAREKET' | 'ELLE' | (tenant_id, kdv_turu, tarih) — dönem bazlı 'özel matrah uygulanan faturalar' dökümü bu indeksten okur | YYYYMMDDHHMMSS_FaturaOzelMatrah |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/faturalar/ozel-matrah-onizleme?hareketId={id}&kdvOran={oran}` | SAHIP, MUDUR, MUHASEBE | Salt okunur. Verilen hareketin (fiş kalemiyse fişin tamamının) has gramını, kullanılan kur(lar)ı, türetilen has bedelini, matrahı ve KDV'yi döndürür. Kapsanan hareket kimliklerini de listeler ki kasiyer neyin faturalandığını Kaydet'ten ÖNCE görsün. Tevsik önizleme ucuyla (B-5) aynı gerekçe. | kiracı filtresi + elle TenantId şartı (ADR-0010) |
| POST | `/api/v1/faturalar` | SAHIP, MUDUR, MUHASEBE (FaturaModulu.cs:277 aynen) | DEĞİŞTİ. HareketId verilmiş ve KdvTuru='KUYUM' ise has bedeli sunucuda türetilir. dto.HasDeger null ise sunucu doldurur; doluysa ve türetilenden 0,01 TL'den fazla sapıyorsa 400 ('türetilen X, gönderilen Y'). HareketId yoksa elle değer kabul edilir ve has_deger_kaynagi='ELLE' yazılır. Mevcut sözleşme kırılmaz: HasDeger alanı DTO'dan kaldırılmaz. | cari doğrulama (FaturaModulu.cs:83-85) · fiş damgası tekilliği (FaturaModulu.cs:134-140) · yeni: kur kaydı fail-closed |
| POST | `/api/v1/bankalar/hareketler/{hareketId}/faturaya-donustur` | SAHIP, MUDUR, MUHASEBE (BankaModulu.cs:362 aynen) | DEĞİŞTİ (tek satır): KdvTuru='STANDART' ve HasDeger=0 AÇIKÇA yazılır. Davranış değişmez; sessiz varsayılana güvenilmez. Banka hareketi hareketler defterinde olmadığı için bu yoldan KUYUM faturası kesilemez ve bu kod yorumuna yazılır. | mevcutlar |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Fatura/OzelMatrahMotoru.cs` — SAF sınıf (DB/ağ/saat yok — MasakKuralMotoru.cs:9-11 deseni). Girdi: (hasGrami, hasBirimFiyat) çiftlerinin listesi + toplam + kdvOran. Çıktı: record OzelMatrahSonuc(decimal HasGram, decimal HasDeger, decimal Matrah, decimal KdvTutar, decimal AraToplam, string? Uyari). Her kalem KENDİ kur fiyatıyla çarpılır ve toplanır; tek kur VARSAYILMAZ (fişin kalemleri farklı KurSnapshotId taşıyabilir — Entities.cs:377). Yuvarlama FaturaModulu.cs:154-158 ile birebir aynı: Math.Round(…, 2, MidpointRounding.AwayFromZero).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` — (a) FaturaOlusturDto'ya [RegularExpression("^[0-9]{3}$")] string? OzelMatrahKodu eklenmez — kod istemciden GELMEZ, yapılandırmadan gelir (beyan edilen kodun istemciden gelmesi yanlış beyan kapısı olurdu). (b) FaturaServisi ctor'a KdvAyarlari eklenir. (c) Olustur içinde damgalanacak kümesi BUGÜNKÜ hâliyle (satır 121-141) hesaplandıktan SONRA, KUYUM ise o kümenin hareketleri ve kur kayıtları TEK sorguda çekilip OzelMatrahMotoru'na verilir. (d) kdvOran varsayılanı ayar.VarsayilanOran olur (satır 87). (e) Yeni 4 kolon Faturalar.Add bloğuna yazılır (satır 146-166). (f) CariliYanit projeksiyonuna 4 alan eklenir (satır 241-262).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs` — public sealed class KdvAyarlari { public decimal VarsayilanOran { get; init; } = 0.20m; public string? OzelMatrahKodu { get; init; } = "805"; } — appsettings 'Kdv' bölümünden. OzelMatrahKodu boş/null bırakılırsa alan hiç yazılmaz ve hiçbir yerde basılmaz (kodun doğruluğu DOGRULANMADI; kapatılabilir olması şarttır).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` — FaturayaDonustur içindeki Faturalar.Add bloğuna (satır 278-296) KdvTuru = "STANDART", HasDeger = 0m, OzelMatrahKodu = null eklenir + gerekçe yorumu. kdvOran varsayılanı (satır 268) KdvAyarlari'ndan okunur.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Masak/Tevsik/Hurda bağlamalarının yanına (satır 57-87 bölgesi) Kdv bölümü bağlanır: AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection("Kdv").Get<KdvAyarlari>() ?? new KdvAyarlari()).
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — "Kdv": { "_not": "Özel matrah neden kodu DOĞRULANMADI — yalnız etikettir, hesaba girmez; boşaltılırsa hiç basılmaz.", "VarsayilanOran": 0.20, "OzelMatrahKodu": "805" }

## Ekranlar
- `/faturalar` · `dotnet/Kuyumcu.Web/Pages/Faturalar.razor` — KUYUM seçilince (Faturalar.razor:148-163) has bedeli girdisi SALT OKUNUR hâle gelir ve yerine 'Faturalandırılacak işlem' seçicisi çıkar: son 30 günün faturasız SATIS/ALIS hareketleri (yeni GET /islemler/faturasiz ucu değil — mevcut /islemler listesi hareketId taşıyorsa o kullanılır; taşımıyorsa küçük bir uç eklenir, bkz. açık sorular). Seçim yapılınca ozel-matrah-onizleme çağrılır ve kırılım gösterilir. İstemcideki ikizlenmiş KDV formülü (Faturalar.razor:251-256) KUYUM dalında SİLİNİR ve sunucu önizlemesi gösterilir (TB-11 deseni: 'ikizlenmiş kural' büyütülmez). STANDART dalı dokunulmadan kalır. Seçim yoksa Kaydet KAPALI (KUYUM'da). (menü/yetki: Menü değişmez (KenarCubugu.razor:42). Modül yetkisi 'fatura' (FaturaModulu.cs:270).)

## İş kuralları (fail-closed)
- KUYUM + HareketId verilmişse has bedeli SUNUCUDA türetilir. İstemciden gelen HasDeger null değilse ve |türetilen − gönderilen| > 0,01 ise 400. Gerekçe: DOVIZ bacağında kuruş-kuruş eşitlik kapısının (A12) aynısı — kasiyer matrahı seçemez; seçebilseydi KDV beyanı fiilen elle yazılırdı.
- Matrahın kümesi, FATURA DAMGASININ kümesiyle AYNIDIR (FaturaModulu.cs:128-132). Ayrışırsa fatura ile defter çelişir: damgalanmış bir kalem matraha girmemiş olur.
- Her kalem KENDİ KurSnapshotId'sinin AltinHasFiyati'yla çarpılır. Tek kur varsayılmaz.
- FAIL-CLOSED: kalemlerden birinin kur kaydı bulunamazsa ya da AltinHasFiyati <= 0 ise 400. Sıfır kabul edilseydi has bedeli 0 çıkar, TÜM bedel matraha girer ve KDV FAZLA hesaplanırdı — mükellef aleyhine sessiz hata.
- has_grami işareti önemsizdir; Math.Abs alınır (satışta negatif — SatisModulu.cs:490). İşaret korunsaydı satış faturasında matrah toplamdan BÜYÜK çıkardı.
- 'hasDeger >= toplam → 400' kuralı BUGÜNKÜ hâliyle korunur (FaturaModulu.cs:99-100); yalnız hata mesajına türetim kırılımı eklenir. Kuralın kendisi bu pakette DEĞİŞTİRİLMEZ (izinsiz değişiklik yok).
- ozel_matrah_kodu YALNIZ KdvTuru='KUYUM' satırlarda yazılır. STANDART faturaya yazılsaydı yanlış beyan kaynağı olurdu.
- ozel_matrah_kodu hiçbir hesaba girmez. Yapılandırmadan okunur; boşaltılırsa alan null kalır ve belgede basılmaz.
- has_deger_kaynagi her KUYUM faturada doludur: 'HAREKET' (türetildi) ya da 'ELLE' (hareket bağı yok). Boş bırakılan bir KUYUM faturası olamaz — denetimde 'bu rakam nereden geldi' sorusunun cevabı budur.
- KDV oranı koda gömülmez; Kdv:VarsayilanOran'dan okunur (ADR-0005).

## Testler
- `dotnet/Kuyumcu.Tests/OzelMatrahMotoruTestleri.cs` [saf]
  - Tek kalem: 10 g has × 6.800 = 68.000; toplam 75.000 → matrah 7.000, KDV %20 → 1.166,67, araToplam 73.833,33 (bağımsız hesaplanmış sabitlerle)
  - İki kalem FARKLI kurla: 5 g × 6.800 + 5 g × 7.000 = 69.000 — tek kurla (ortalama) hesaplanan 69.000 değil ise test kırmızı
  - Has bedeli toplamı AŞIYOR → sonuç Uyari taşır, hesap yapılmaz
  - Negatif has_grami (satış satırı) mutlak değere çevrilir
  - Yuvarlama: 0,005 sınırında AwayFromZero, FaturaModulu.cs:154-158 ile aynı
- `dotnet/Kuyumcu.Tests/FaturaOzelMatrahTestleri.cs` [DB]
  - hareketId verilen KUYUM faturada has_deger/has_gram/has_birim_fiyat/kaynak='HAREKET' yazılır
  - Çok kalemli fişin hareketId'si verilince MATRAH FİŞİN TAMAMINDAN çıkar (tek kalemden değil) ve damgalanan küme ile birebir aynıdır
  - İstemci farklı HasDeger gönderirse 400 ve HİÇBİR satır yazılmaz (SQL ile sayılır)
  - Kur kaydı silinmiş/bulunamayan hareket → 400 (fail-closed), fatura yazılmaz
  - hareketId YOKSA elle değer kabul edilir ve kaynak='ELLE'
  - STANDART faturada ozel_matrah_kodu null kalır
  - OzelMatrahKodu yapılandırmada boşsa KUYUM faturada da null kalır
  - Yabancı kiracının hareketId'si → 404 (kiracı sınırı)
- `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs` [bUnit]
  - KUYUM seçilince has bedeli girdisi salt okunur olur ve işlem seçici çıkar
  - İşlem seçilince POST gövdesinde hareketId gider (CariEkranTestleri.cs:46-72 sahte API deseni)
  - İşlem seçilmeden Kaydet KAPALI (disabled="@(false)" tuzağına dikkat — SONRAKI-OTURUM-PROMPT.md §4/A)
  - STANDART seçiliyken gövdede hareketId YOK ve eski davranış aynen sürer
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - GET /api/v1/faturalar/ozel-matrah-onizleme TEZGAHTAR ile 403

## Mutasyon kanıtları
- Türetim kaldırılıp dto.HasDeger'e geri dönülürse → FaturaOzelMatrahTestleri 'hareketId verilen KUYUM faturada has_deger türetilir' KIRMIZI
- İstemci/sunucu tutarlılık kapısı kaldırılırsa (fark sessizce yutulursa) → 'farklı HasDeger gönderilince 400' KIRMIZI
- Matrah tek hareketten hesaplanırsa (fiş kümesi yerine) → 'çok kalemli fişte matrah fişin tamamından' KIRMIZI
- Kur kaydı yokken AltinHasFiyati 0 varsayılırsa → 'kur kaydı bulunamayan hareket 400' KIRMIZI
- Kalemler tek (ilk) kurla çarpılırsa → motorun 'iki kalem farklı kurla' testi KIRMIZI
- Math.Abs kaldırılırsa (satışın negatif has'ı) → 'negatif has_grami mutlak değere çevrilir' KIRMIZI
- ozel_matrah_kodu STANDART faturaya da yazılırsa → 'STANDART'ta kod null kalır' KIRMIZI
- Ekranda işlem seçilmeden Kaydet açık bırakılırsa → FaturaEkranTestleri KIRMIZI

## Canlı doğrulama
- Yeni kiracı aç (KDV-TEST). Kur kaydı 10 sn sonra oluşur — /kur/guncel id gelene kadar BEKLE (tuzak).
- Ürün aç (K22, 10 g), 10 adet alış gir → stok ve maliyet partisi doğsun.
- 2 adet satış yap (odemeTuru HAVALE — tevsik haddine takılmasın). Dönen hareketId'yi al (camelCase 'id' tuzağı: perl ile çek, boşsa DUR).
- GET /faturalar/ozel-matrah-onizleme?hareketId=… → hasGram ve hasDeger'i perl ile bağımsız hesapla, birebir karşılaştır.
- POST /faturalar (kdvTuru=KUYUM, hareketId, hasDeger GÖNDERME) → 200; SQL ile faturalar satırında has_gram/has_birim_fiyat/has_deger_kaynagi='HAREKET'/ozel_matrah_kodu='805' doğrula.
- Aynı fişten ikinci fatura → 400 ('zaten faturalandırılmış' — mevcut davranış korunuyor mu).
- hasDeger'i 1 TL kaydırarak POST → 400; SQL ile fatura satırı sayısı ARTMADI (yeni satır yazılmadı) doğrula.
- Karma fiş (2 kalem, aynı fis_id) aç, bir kaleminin hareketId'siyle fatura kes → SQL: iki kalem de aynı fatura_no ile damgalı VE has_gram iki kalemin toplamı.
- Kur kaydını sildirmeden test etmek için: hareketin kur_snapshot_id'sini geçici olarak bilinmeyen bir değere çeken bir SENARYO KURMA (defter append-only) — bunun yerine kur_snapshots'ta altin_has_fiyati=0 olan bir kayıtla yeni satış yap → fatura 400.
- Banka dekontundan faturaya dönüştür → SQL: kdv_turu='STANDART', has_deger=0, ozel_matrah_kodu NULL.
- appsettings'te OzelMatrahKodu'nu boşalt, API'yi yeniden başlat, yeni KUYUM faturası kes → kolon NULL.
- Test kiracısını SİL (API'yi önce durdur — kur zamanlayıcısı yazmaya devam eder). Kiracı sayısı başlangıç değerine dönsün.

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/RaporFaturaTestleri.cs`

## Mevzuat dayanağı
- 3065 s. KDV Kanunu md.23/(e) — özel matrah şekilleri (altından mamul veya altın ihtiva eden ziynet eşyası ile sikke altın tesliminde matrah, külçe altın bedeli düşüldükten sonra kalan miktar) — **DOGRULANMALI** — Kod tabanında bugün yalnız 'KUYUM' etiketiyle uygulanıyor (FaturaModulu.cs:91-107); madde metni bu oturumda birincil kaynaktan (Resmî Gazete / GİB) doğrulanmadı. Uygulayıcı önce metni teyit etsin. Teyit edilemezse HESAP DAVRANIŞI BUGÜNKÜ HÂLİYLE KORUNUR; bu paket yalnız rakamın KAYNAĞINI elle girişten türetime çevirir, kuralın kendisini değiştirmez. Bu yüzden teyit gecikmesi paketi bloke etmez.
- GİB e-Fatura/e-Arşiv özel matrah neden/kod listesi — '805' — **VARSAYIM** — Kod tabanında '805' 0 isabet. Kodun hangi listeye ait olduğu ve tam karşılığı DOĞRULANMADI. Bu yüzden kod hiçbir hesaba girmez; yapılandırmadan okunan bir ETİKET olarak faturaya yazılır, belgede basılır ve boşaltılarak tamamen kapatılabilir. Doğrulanana kadar 'bu kodun doğru olduğunu iddia eden' hiçbir metin arayüze yazılmaz.
- KDV genel oranı %20 — **DOGRULANMALI** — Bugün iki yerde koda gömülü (FaturaModulu.cs:87, BankaModulu.cs:268) ve bu ADR-0005'in açık ihlali. Paket rakamı DEĞİŞTİRMEZ, yalnız Kdv:VarsayilanOran yapılandırmasına taşır.
- Kuyum Ticareti Yönetmeliği md.11/(e) — taş ibaresinin faturada yer alması (TB-17) — **DOGRULANMALI** — Teknik borç envanterinde kayıtlı (teknik-borc-envanteri.md:287-297). Fatura satır kalemi olmadan otomatik basılamaz; PARITE paket 1'in işidir ve bu paketin KAPSAMI DIŞINDADIR. Paket 38'in veri modeli (has_gram/has_birim_fiyat) satır bazına inmeye engel değildir.

## Riskler
- Türetim, hareketin has bacağının DOĞRU olduğunu varsayar. Ürün kartının HasGrami'si sonradan değiştirilirse geçmiş hareketler güncellenmez (UrunModulu.cs:540-553 yalnız stok satırlarını günceller) — ama hareket kendi has'ını taşıdığı için geçmiş fatura etkilenmez. Bu bir güvenlik, risk değil; yine de test edilmeli.
- Faturalar.razor'da KUYUM dalı değiştiriliyor; STANDART dalına dokunulmuyor. Regresyon riski düşük ama FaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir (RaporFaturaTestleri.cs:113) bu paketle SÖZLEŞME DEĞİŞTİRİR — testin yeni hâli 'elle değer hareketsiz kabul edilir' senaryosunu korumalı, silinmemeli.
- 'Faturalandırılacak işlem' seçicisi için uygun bir liste ucu yoksa yeni uç açmak gerekir; bu paket büyür. Faz A'da mevcut /islemler listesi yeterliyse yeni uç açılmaz.
- Kod alanı yanlış doğrulanırsa fatura üzerinde yanlış bir beyan kodu basılır. Bu yüzden varsayılanı boşaltılabilir ve hesaba girmez.

## Açık sorular
- KDVK md.23/(e) metni ve 2026 yürürlükteki hâli birincil kaynaktan teyit edilecek mi, yoksa mali müşavire mi sorulacak (K2)?
- '805' hangi GİB kod listesine aittir ve karşılığı nedir? Teyit edilemezse varsayılan boş bırakılsın mı?
- hasDeger == toplam (işçilik/kâr sıfır) hâli bugün 400 veriyor. Bu doğru mu, yoksa matrah 0 olan bir fatura meşru mu? Bugünkü davranış korundu; karar oylamaya.
- 'Faturalandırılacak işlem' seçicisi için yeni bir GET /islemler/faturasiz ucu açılsın mı, yoksa mevcut /islemler listesi mi süzülsün?
- Alış faturasında (Tur='ALIS') kuyum KDV'si uygulanır mı? Bugün tür ayrımı yok (FaturaModulu.cs:96). Türetim alış hareketleri için de çalışır; mevzuat yönü doğrulanmadı.

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Paketin ölçümü bu oylamadaki en sağlam ölçüm: beş ayrı iddianın beşini de dosya:satır düzeyinde doğruladım. Kuralın kendisini değiştirmeyip yalnız rakağın KAYNAĞINI elle girişten türetime çevirmesi, doğrulanmamış bir mevzuat hükmüne rağmen paketi sevk edilebilir kılıyor — bu doğru mimari tercihtir. Fail-closed kur kuralı (kur yoksa 400, has bedeli 0 varsayılmaz) ve Math.Abs kararı doğru. Tek ciddi itiraz paket 1 ile çift motor.
  - Koşullar: ÇİFT MEKANİZMA — BAĞLAYICI: has bedeli türetimi TEK saf sınıfta (OzelMatrahMotoru) doğacak ve paket 1 onu ÇAĞIRACAK. Paket 1'in FaturaKdvHesabi'si ayrı bir KDV/has motoru olarak YAZILMAYACAK. İki motor iki gerçek demektir ve bu projede 'ikizlenmiş kural' zaten TB-11 olarak kayıtlı bir borç. · SIRA: 38 paket 1'den ÖNCE. faturalar tablosuna kolon ekleyen migration bu pakette açılır; paket 1 aynı migration'a kalem_kaynagi'nı ekler ya da hemen ardından gelir. İki paralel migration KuyumcuDbContextModelSnapshot.cs'i çakıştırır. · has_deger_kaynagi ÜÇ DEĞERLİ tanımlansın: HAREKET | KALEM | ELLE. İki değerle açılırsa paket 1 üçüncüyü sonradan eklemek için kolonu yeniden migrate etmek zorunda kalır. · MEVCUT DURUM METNİ DÜZELTİLECEK: "'hareketId' Kuyumcu.Web altında 0 KEZ geçer" İDDİASI YANLIŞ — 15 isabet var (UyumServisi.cs:117-190, Uyum.razor:284-406, VadeTakibi.razor:117, Islemler.razor:1203, Faturalar.razor:73/223). Doğru ve yeterli iddia şudur: Faturalar.razor:293-303 POST gövdesinde hareketId YOKTUR. Kanıt gücü aynı, ölçüm dürüst olmalı. · KUYUM + HareketId yolunda dto.HasDeger ile türetilen arasındaki tolerans 0,01 TL DEĞİL SIFIR olsun ya da 0,01'in neden seçildiği kuruş yuvarlama zinciriyle gösterilsin. OdemeSepeti'nin 'TOLERANS YOK' kuralıyla asimetri gerekçesiz kalmamalı. · KDV oranının yapılandırmaya taşınması DAVRANIŞ DEĞİŞTİRMEYECEK: varsayılan 0.20 kalacak ve iki çağrı noktası (FaturaModulu.cs:87, BankaModulu.cs:268) AYNI ayardan okuyacak. Biri taşınıp diğeri bırakılırsa ihlal yarım kapanır. · Faz A'nın ürünü şu olacak: türetim + tutarlılık kapısı + ekranın hareketId göndermesi. Yazdırılabilir fatura çıktısı (Faz B) BU PAKETE GİRMEYECEK — o iş paket 42'nin kurduğu BelgeYerlesimi'ne bağlıdır ve onu beklemelidir. · '805' kodu hesaba girmeyecek, yapılandırmadan gelecek, boşaltılabilir olacak ve arayüzde 'bu kod doğrudur' iddiası taşıyan hiçbir metin bulunmayacak. Spesifikasyon bunu zaten diyor — kabul ölçütüne yazılsın.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Elle girilen has bedelinin hareketten türetilmesi, tutarlılık kapısı (±0,01), fail-closed kur, KDV oranının yapılandırmaya taşınması (ADR-0005 ihlalinin kapanması) — hepsi güvenlik/uyum yönünde sıkılaşma. Tek ciddi sorun: DOGRULANMAMIŞ '805' kodunun varsayılan olarak yasal belgeye basılması; bu, 'mevzuat uydurma' yasağının ihlali olur.
  - Koşullar: K38-1 (VARSAYIM koda girmez): `Kdv:OzelMatrahKodu` VARSAYILANI BOŞ/null olur, '805' DEĞİL. Spesifikasyon kendi içinde çelişiyor: mevzuat bloğu 'doğrulanana kadar bu kodun doğru olduğunu iddia eden hiçbir metin arayüze yazılmaz' derken appsettings varsayılanı '805' basıyor. Doğrulanmamış bir kodun her KUYUM faturasına yazılıp belgede basılması = yanlış beyan riski. Kod ancak K2 teyidi sonrası yapılandırmadan AÇILIR. Test 'varsayılanda ozel_matrah_kodu null' olarak ters çevrilir. · K38-2: ozel-matrah-onizleme ucu [RolGerekli('SAHIP','MUDUR','MUHASEBE')] + [ModulYetkisi('fatura')] (controller sınıf düzeyi zaten var, FaturaModulu.cs:267-270) ve hareket kümesi `TenantId == islem.TenantId` şartıyla (ADR-0010) — yabancı hareketId 404; HTTP testi TEZGAHTAR 403 yazılı, korunur. · K38-3: Kur kaydı `db.KurSnapshots.Where(k => ids.Contains(k.Id) && k.TenantId == islem.TenantId)` ile tek sorguda; AltinHasFiyati<=0 → 400 (fail-closed, yazılı) korunur. · K38-4 (paket 1 ile tek motor): bkz. K1-4 — OzelMatrahMotoru ile FaturaKdvHesabi'ndan yalnız biri yazılır.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Kusur tespiti doğru (FaturaModulu.cs:98 'dto.HasDeger ?? 0m', :87 ve BankaModulu.cs:268 sabit 0.20m, Faturalar.razor:297-306 hareketId yok, BankaModulu.cs:278-296 KdvTuru yazılmıyor). Saf OzelMatrahMotoru ve fail-closed kur kuralı test edilebilir. Ancak paket 1 ile çift mekanizma ve ctor değişikliğinin mevcut testleri kırması çözülmeden sevk edilemez.
  - Koşullar: Paket 1 ile ÇAKIŞMA çözülmeden başlanmaz (çapraz bulgu 1). Öneri: paket 38'in hareket-düzeyi türetimi (has_gram/has_birim_fiyat/has_deger_kaynagi/ozel_matrah_kodu) ÖNCE gider; paket 1 kalem modeli bunun ÜSTÜNE gelir ve kalem has_deger'i aynı OzelMatrahMotoru'ndan üretir; iki tolerans kuralı tek kurala indirilir (has bedeli için 0,01 TL, toplam için 0 — ya da ikisi de 0; oylama). · FaturaServisi ctor'una KdvAyarlari eklenmesi mevcut 'new MFatura.FaturaServisi(db)' çağrısını (RaporFaturaTestleri.cs:119, FaturaNo testi dahil) KIRAR → 'KdvAyarlari? ayar = null' opsiyonel, null'da new KdvAyarlari(). Aynı kural BankaServisi için (MutabakatTestleri vb.). · 'OzelMatrahKodu yapılandırmada boşsa null' testi appsettings'i değil ctor'a verilen KdvAyarlari{OzelMatrahKodu=null} nesnesini kullanır (HttpOrtami ortam değişkenleri süreç genelidir, HttpOrtami.cs:41-56; test içinde değiştirilmez). · FaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir (RaporFaturaTestleri.cs:113-130) hareketId'siz olduğu için 'ELLE' yolunu sınar; SİLİNMEZ, adı değiştirilmez; yeni assert eklenmez (regresyon sözleşmesi). · 'Yabancı kiracının hareketId'si → 404' çok katmanlı koruma: mevcut FaturaModulu.cs:124-126 elle TenantId şartı var; mutasyon kanıtı için filtresiz bağlam testi, yoksa 'regresyon' etiketi. · 'Kur kaydı silinmiş hareket → 400' fikstürü: KurSnapshot satırı vt.OlusturSistem() ile silinir (append-only koruması KurSnapshots'ta var mı — DenetimAppendOnlyTestleri.cs kapsamı okunmalı; varsa altin_has_fiyati=0 kaydıyla yeni satış yolu kullanılır, canlı doğrulama zaten bu yolu seçmiş). · FaturaEkranTestleri.cs paket 1 ile aynı dosya adı → tek dosya; 'Kaydet KAPALI' assert'i disabled niteliğinin varlığına bakar (§6:159-160); işlem seçici için SahteApi'ye /islemler yanıtı tanımlanır; WaitForAssertion. · 'mevcut_durum'daki "'hareketId' Kuyumcu.Web altında 0 KEZ geçer" ifadesi literal olarak yanlış (15 isabet: UyumServisi.cs, Uyum.razor, VadeTakibi.razor, Islemler.razor:1203 kaynakHareketId); doğru ifade: Faturalar.razor POST gövdesinde (satır 297-306) hareketId YOK. Belge düzeltilir — uygulayıcı grep'te 15 görünce belgeye güvenini yitirmesin. · Canlı doğrulama ölçülebilir; 'hasDeger 1 TL kaydırılınca fatura satırı ARTMADI' SQL sayımı ve 'appsettings OzelMatrahKodu boşaltılıp API yeniden başlatılır' adımı için API'nin --urls 5199 ile yeniden başlatılması ve kiracı silmeden önce durdurulması yazılır.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- TEK MOTOR (bağlayıcı, Bertan + Mehmet K38-4/K1-4): has bedeli türetimi tek saf sınıfta — OzelMatrahMotoru; paket 1 onu ÇAĞIRIR, ayrı FaturaKdvHesabi YAZILMAZ. İki motor iki gerçek demektir.
- SIRA: 38, paket 1'den ÖNCE (dalga 2). faturalar migration'ı burada açılır; paket 1 aynı/hemen sonraki migration'a kalem_kaynagi'nı ekler — iki paralel migration ModelSnapshot'ı çakıştırır (Bertan+Aşkın çapraz bulgu 1).
- has_deger_kaynagi ÜÇ DEĞERLİ: HAREKET | KALEM | ELLE (paket 1 üçüncüyü sonradan migrate etmesin).
- VARSAYIM KODA GİRMEZ: Kdv:OzelMatrahKodu varsayılanı BOŞ/null, '805' değil; hesaba girmez, yapılandırmadan gelir, arayüzde 'bu kod doğrudur' iddiası taşıyan metin yok (Mehmet K38-1 + Bertan).
- KUYUM + HareketId yolunda dto.HasDeger ile türetilen arasındaki tolerans SIFIR olsun ya da 0,01 TL kuruş yuvarlama zinciriyle gerekçelendirilsin (OdemeSepeti 'TOLERANS YOK' ile asimetri gerekçesiz kalmasın).
- KDV oranı yapılandırmaya taşınırken DAVRANIŞ DEĞİŞMEZ: varsayılan 0.20 ve iki çağrı noktası (FaturaModulu.cs:87, BankaModulu.cs:268) AYNI ayardan okur.
- FaturaServisi ctor'una KdvAyarlari OPSİYONEL eklenir ('KdvAyarlari? ayar = null', null'da new KdvAyarlari()) — aksi hâlde 'new MFatura.FaturaServisi(db)' (RaporFaturaTestleri.cs:119) kırılır; aynı kural BankaServisi için (Aşkın).
- RaporFaturaTestleri.cs:113-130 KuyumKdv_HasBedeliMatrahDisiKalir SİLİNMEZ/adı değişmez/yeni assert almaz: 'ELLE' yolunun regresyon sözleşmesidir. 'Kod boşsa null' testi appsettings'i değil ctor'a verilen KdvAyarlari nesnesini kullanır (HttpOrtami.cs:41-56 süreç geneli).
- ozel-matrah-onizleme ucu [RolGerekli(SAHIP,MUDUR,MUHASEBE)] + [ModulYetkisi("fatura")]; hareket kümesi TenantId==islem.TenantId (ADR-0010); kur kayıtları tek sorguda ids.Contains+TenantId; AltinHasFiyati<=0 → 400 fail-closed korunur (Mehmet K38-2/K38-3).
- MEVCUT DURUM METNİ DÜZELTİLECEK: "'hareketId' Kuyumcu.Web altında 0 KEZ geçer" YANLIŞ — 15 isabet (UyumServisi.cs:117-190, Uyum.razor:284-406, VadeTakibi.razor:117, Islemler.razor:1203, Faturalar.razor:73/223). Faz A kapsamı: türetim + tutarlılık kapısı + ekranın hareketId göndermesi; yazdırılabilir çıktı Faz B'de, paket 42'nin BelgeYerlesimi'ni bekler.
