# Paket 32 — Banka ekstresi içe aktarma + IBAN/ünvandan cari eşleştirme

> Katman: Katman 4 — Cari ve finans (bu görevde yalnız paket 32) · Tahmin: 5-7 gün (Faz A: 3-4 · Faz B: 2-3) — mühürlü 4-6 günün üst sınırına yakın; fark, sahne tablosu + öğrenen eşleştirme tablosunun mühürlü tahminde ayrıca sayılmamasından geliyor gün · Önkoşullar: 41 · Kaynak: katman mimarı K4-32 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 4
> ⚠ **VETO:** Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOŞULLU VETO: paket 41 (MASAK kapısının BankaServisi.HareketEkle'ye bağlanması) sevk edilmeden 32'nin toplu onay ucu canlıya alınamaz — banka hareketi toplu üretimi MASAK kümülatifini görmeden çalışır. Ayrıca IBAN/ünvan şifreleme (AS-2 kararım) Faz A'nın parçasıdır.

## Hedef
Perakende kuyumcu bugün her havaleyi Banka ekranında TEK TEK elle giriyor (BankaModulu.cs:180-223 manuel dekont). Ayın sonunda 150-400 satırlık banka ekstresi ile programdaki dekontları göz kararı karşılaştırıyor; hem saat kaybı hem de 'girilmemiş havale' yüzünden müşteri bakiyesi yanlış duruyor. Bu paket müşterinin kendi bankasından aldığı MT940/MT942 ya da CSV ekstre dosyasını yükleyip, satırları mükerrer koruması ve IBAN/ünvan eşleştirmesiyle banka hareketine çevirmesini sağlar. Banka sözleşmesi GEREKMEZ: dosyayı müşteri kendi bankasından alır (Ziraat MT940/942 teknik dokümanı kamuya açık — PAZAR-ARASTIRMASI-2026-09-16.md:493).

## Kodda bugünkü durum
Ekstre kelimesi kodda yalnız CARİ ekstresi anlamında geçiyor; banka ekstresi içe aktarımı YOK. banka_hareketleri satırları tek tek elle giriliyor (BankaModulu.cs:180-223 HareketEkle; ekranda Banka.razor:207 'DekontGir' modalı). Mükerrer koruması YOK: aynı dekont iki kez girilirse iki satır oluşur ve hesap bakiyesi iki kez artar (BankaModulu.cs:200 `hesap.Bakiye += dto.Tutar * yonKatsayi`, hiçbir benzersizlik kontrolü yok). Cari eşleştirme tamamen elle: dto.CariId verilirse 'ESLESTI', verilmezse 'BEKLIYOR' (BankaModulu.cs:215). Cari kartında IBAN alanı YOK (Entities.cs:120-143). banka_hareketleri'nde banka referans numarası kolonu YOK (Entities.cs:464-480). İndeks yalnız (tenant_id, tarih) (KuyumcuDbContext.cs:132). Dosya yükleme altyapısı VAR ve çalışıyor (paket 4: UrunBelgesi.cs Belge varlığı, BelgeModulu.cs:27 base64 DTO, :221 [RequestSizeLimit(8_000_000)], OranSiniri.cs:40 'belge' politikası) — ama içerik kapısı yalnız PDF/JPEG/PNG tanır (BelgeKapisi.cs:56-60). Kodda IFormFile hiç geçmiyor (grep 0).

## Kapsam
**İçinde:**
- Faz A — CSV ekstre içe aktarma: yükleme (base64), ayrıştırma, sahne tablosuna yazma, mükerrer tespiti, elle cari eşleştirme, seçili satırları onaylayıp banka hareketine çevirme
- Faz A — ekstre_ithalatlari + ekstre_satirlari sahne tabloları, banka_hareketleri'ne ekstre_satir_id ve banka_referans kolonları + mükerrer benzersizlik indeksi
- Faz A — /banka/ekstre eşleştirme ekranı (sayfalı, TB-19 uyumlu), Banka.razor'a 'Ekstre Yükle' düğmesi
- Faz B — MT940/MT942 ayrıştırıcı (:20:, :25:, :28C:, :60F:, :61:, :86:, :62F: etiketleri)
- Faz B — cari_ibanlari öğrenen eşleştirme tablosu + IBAN otomatik eşleşmesi + ünvan benzerlik ÖNERİSİ
- Faz B — açılış/kapanış bakiyesi kontrolü (MT940 :60F:/:62F: ile hesap bakiyesinin tutarlılık uyarısı — engelleyici değil, uyarı)
- Denetim izi: yükleme, prova ve onay ayrı ayrı satır sayısıyla kaydedilir (ADR-0004)

**Dışında:**
- Banka API / açık bankacılık ile otomatik hareket çekme — C sınıfı; ÖHVPS'ye erişim ödeme hizmeti sağlayıcısı lisansı ister (TCMB DUY2026-13, PAZAR-ARASTIRMASI-2026-09-16.md:491)
- Hareketler defterine yazma, cari bakiyesi güncelleme, veresiye kapatma (K4-OK-1)
- Faturaya otomatik dönüştürme zinciri — mevcut FaturayaDonustur ucu (BankaModulu.cs:250) elle kalır; toplu faturalama ayrı karardır
- Ürün/cari/stok verisinin içe aktarımı (paket 47) — bu paket YALNIZ banka hareketi satırı üretir
- Döviz hesabı ekstresinin TL karşılığının hesaplanması (kur_snapshot_id gerektirir; açık soru AS-3)
- Gün kilidi (GunKapanisiKapisi) banka hareketine bağlanması — paket 41 ile aynı gerekçeyle ayrı karar
- e-Fatura XML üretimi (ADR-0012)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| ekstre_ithalatlari | YENİ tablo (IKiraciVarlik) | id VARCHAR(36) PK · tenant_id VARCHAR(36) · banka_hesap_id VARCHAR(36) · dosya_adi VARCHAR(255) · format VARCHAR(10) ('CSV'|'MT940') · sha256 VARCHAR(64) — aynı dosyanın ikinci kez yüklendiği görülsün · satir_sayisi INT · yeni_sayisi INT · mukerrer_sayisi INT · onaylanan_sayisi INT · durum VARCHAR(15) ('PROVA'|'ONAYLANDI'|'IPTAL') · acilis_bakiye DECIMAL(15,2) NULL · kapanis_bakiye DECIMAL(15,2) NULL (MT940 :60F:/:62F:; CSV'de null) · yukleyen_kullanici_id VARCHAR(36) · olusturuldu DATETIME · guncellendi DATETIME. ⚠ Ham dosya SAKLANMAZ: sha256 mükerrer dosya tespiti için yeter, ham satır zaten ekstre_satirlari'nda durur — 8 yıl boyunca blob biriktirmenin gerekçesi yok. | (tenant_id, olusturuldu) · (tenant_id, banka_hesap_id, sha256) | YYYYMMDDHHMMSS_BankaEkstresiIceAktarma |
| ekstre_satirlari | YENİ tablo (IKiraciVarlik) — SAHNE tablosu, K4-OK-3 | id VARCHAR(36) PK · tenant_id · ithalat_id VARCHAR(36) · satir_no INT (dosyadaki sıra, denetim için) · ham_satir VARCHAR(500) (kırpılır) · tarih DATETIME (UTC) · yon VARCHAR(10) ('GELEN'|'GIDEN') · tutar DECIMAL(15,2) · para_birimi VARCHAR(5) · karsi_taraf_ad VARCHAR(150) NULL · karsi_iban VARCHAR(34) NULL · banka_referans VARCHAR(50) NULL · aciklama TEXT NULL · dedupe_anahtar VARCHAR(64) NOT NULL (SHA-256) · durum VARCHAR(15) ('YENI'|'MUKERRER'|'ONAYLANDI'|'ATLANDI'|'HATALI') · hata_mesaji VARCHAR(200) NULL · eslesen_cari_id VARCHAR(36) NULL · eslesme_kaynagi VARCHAR(15) NULL ('IBAN'|'ELLE') · banka_hareket_id VARCHAR(36) NULL (onay sonrası üretilen satır) | (tenant_id, ithalat_id, satir_no) BENZERSİZ · (tenant_id, ithalat_id, durum) — ekranın sayfalı listesi bu indeksten okur (TB-19) | aynı migration |
| cari_ibanlari | YENİ tablo (IKiraciVarlik) — öğrenen eşleştirme, K4-OK-6 | id VARCHAR(36) PK · tenant_id · cari_id VARCHAR(36) · iban VARCHAR(34) · unvan VARCHAR(150) NULL (ekstrede görülen ad — denetim için) · kaynak VARCHAR(15) ('ELLE'|'OGRENILDI') · olusturuldu DATETIME. ⚠ NEDEN CARİ'YE KOLON DEĞİL: bir müşterinin birden çok hesabı olabilir ve eşi/şirketi adına havale gelebilir; tek kolon ikinci IBAN görüldüğünde ya birincisini EZER ya da eşleşmeyi kaybeder. | (tenant_id, iban) BENZERSİZ — bir IBAN iki cariye bağlanamaz (belirsiz eşleşme yapısal olarak imkânsız) · (tenant_id, cari_id) | aynı migration |
| banka_hareketleri | 2 yeni kolon + 1 benzersiz indeks | ekstre_satir_id VARCHAR(36) NULL — elle girilen dekontlarda null kalır, ekstreden geleni işaret eder · banka_referans VARCHAR(50) NULL · dedupe_anahtar VARCHAR(64) NULL — elle girişte de hesaplanır ki ekstre yüklendiğinde 'bunu zaten elle girmiştim' görülsün | (tenant_id, banka_hesap_id, dedupe_anahtar) BENZERSİZ — ⚠ MySQL'de NULL değerler benzersizlik kısıtını ihlal etmez; mevcut satırların dedupe_anahtar'ı NULL kalır ve migration onları geriye doldurmaz (Down'ın geri dönülemez veri üretmemesi için). Yeni yazılan HER satır anahtarı doldurur. | aynı migration |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/bankalar/{hesapId}/ekstre/yukle` | SAHIP, MUDUR, MUHASEBE (BankaModulu.cs:362 ile aynı) | Faz A. Gövde: { dosyaAdi, format ('CSV'|'MT940'), icerikBase64 }. Dosyayı ayrıştırır, ekstre_ithalatlari + ekstre_satirlari yazar, HİÇBİR banka hareketi üretmez. Yanıt: ithalatId, satır sayısı, yeni/mükerrer/hatalı dağılımı, ilk 50 satırın önizlemesi. Aynı sha256 aynı hesaba ikinci kez yüklenirse 409 ('Bu dosya zaten yüklendi') — zorlamak isteyen ?tekrar=true ile geçer ve satırlar yine mükerrer işaretlenir. | [RequestSizeLimit(3_000_000)] · [EnableRateLimiting(OranSiniri.Ekstre)] · boyut ≤ 2 MB ham · satır ≤ 10.000 · [ModulYetkisi("banka")] (BankaModulu.cs:328) |
| GET | `/api/v1/bankalar/ekstre/{ithalatId}` | SAHIP, MUDUR, MUHASEBE | Faz A. İthalat başlığı + SAYFALI satır listesi (?durum=&limit=100&offset=0). Sınırsız ToListAsync YOK (TB-19); toplamlar sunucuda SumAsync/CountAsync ile hesaplanır. | tenant şartı elle + global filtre (ADR-0010) |
| PATCH | `/api/v1/bankalar/ekstre/satir/{satirId}` | SAHIP, MUDUR, MUHASEBE | Faz A. Gövde: { cariId?, durum? ('ATLANDI') }. Tek satırın cariye elle bağlanması ya da atlanması. cariId verilirse ve satırda karsi_iban varsa cari_ibanlari'na ÖĞRENİLMİŞ bağ yazılır (K4-OK-6); IBAN başka bir cariye bağlıysa 409 ve mevcut sahibinin adı döner. | durum ONAYLANDI ise 409 (onaylanmış satır değiştirilemez) |
| POST | `/api/v1/bankalar/ekstre/{ithalatId}/onayla` | SAHIP, MUDUR, MUHASEBE | Faz A. Gövde: { satirIdler: [] } — BOŞ GÖNDERİLEMEZ (400). Seçili YENI satırların her biri için BankaServisi.HareketEkleIc çağrılır (K4-OK-2), banka_hareket_id sahne satırına yazılır, durum ONAYLANDI olur. Satır bazında hata olursa O SATIR HATALI işaretlenir, diğerleri devam eder; yanıt başarılı/başarısız sayısını ve hata mesajlarını döner. | HareketEkleIc içindeki tüm mevcut kapılar + (paket 41 sevk edilince) MASAK kapısı otomatik · benzersiz dedupe indeksi (yarış durumunda ikinci yazma DB'den döner) |
| GET | `/api/v1/bankalar/ekstre/satir/{satirId}/oneriler` | SAHIP, MUDUR, MUHASEBE | Faz B. Ünvan benzerliğine göre en fazla 5 cari önerisi + skor. ASLA otomatik uygulanmaz (K4-OK-5). | — |
| GET | `/api/v1/bankalar/ekstre/sablon.csv` | SAHIP, MUDUR, MUHASEBE | Faz A. Beklenen CSV sütun başlıklarını içeren örnek dosya (tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan). Müşterinin bankası farklı başlık kullanıyorsa kolon eşleme ekranı DEĞİL, otomatik başlık tanıma devreye girer (bkz. iş kuralları). | — |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Data/BankaEkstresi.cs` — EkstreIthalati, EkstreSatiri, CariIban varlıkları (üçü de IKiraciVarlik) + EkstreSatirDurumu/EkstreFormati kapalı liste sabitleri. Neden ayrı dosya: Entities.cs zaten büyük ve MasakVarliklari.cs / UrunBelgesi.cs deseni ayrı dosyayı destekliyor.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — BankaHareket sınıfına (satır 464-480) EkstreSatirId, BankaReferans, DedupeAnahtar kolonları + neden elle girişte de anahtar hesaplandığını anlatan XML yorumu.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — DbSet<EkstreIthalati>/<EkstreSatiri>/<CariIban> + OnModelCreating'e (satır 132 BankaHareket indeksinin yanına) veri modeli tablosundaki 6 indeks. Global kiracı filtresi üç yeni varlığa da otomatik uygulanır (HasQueryFilter, KuyumcuDbContext.cs:194-208 bölgesi — IKiraciVarlik üzerinden).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Banka/EkstreAyristirici.cs` — SAF sınıf (DbContext görmez, test edilmesi kolay). CsvAyristir(string metin) ve Mt940Ayristir(string metin) → List<AyristirilmisSatir>. Kodlama: UTF-8 dene, geçersiz bayt varsa ISO-8859-9 (Türkçe) ile yeniden dene — bankaların text ekstresi genelde Latin-5'tir. Ayraç otomatik: ilk satırda ';' sayısı > ',' sayısı ise ';'. Ondalık: son ayraçtan sonrası 2 hane ise o ondalık ayracıdır (TR '1.234,56' ve EN '1,234.56' ikisi de çözülür); çözülemeyen satır HATALI işaretlenir, ATLANMAZ.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Banka/CariEslestirici.cs` — SAF sınıf. UnvanNormalle(string): TR büyük harf (tr-TR kültürüyle — 'i'→'İ' tuzağı), noktalama/çoklu boşluk temizliği, sondaki LTD/ŞTİ/A.Ş./SAN/TİC ekleri atılır. Skor(a,b): token kesişimi / birleşim (Jaccard). Eşik yapılandırmadan okunur, varsayılan 0,60 ve YALNIZ öneri sıralaması için kullanılır (K4-OK-5).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Banka/EkstreModulu.cs` — EkstreServisi (Yukle, Getir, SatirGuncelle, Onayla, Oneriler) + EkstreController. Dedupe anahtarı: SHA-256( bankaHesapId | tarih(yyyy-MM-dd) | tutar(F2, invariant) | yon | bankaReferans ?? '' | karsiIban ?? '' | aciklamaNormal ). ⚠ Banka referansı VARSA tek başına yeter; yoksa bileşik alanlar kullanılır — iki özdeş havale aynı gün meşru olarak yapılabilir, bu yüzden referanssız dosyada aynı anahtarlı ikinci satır MUKERRER işaretlenir ama kullanıcı 'yine de al' ile tek tek onaylayabilir (fail-closed ama çıkmaz sokak değil).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` — (a) HareketEkle gövdesi (satır 180-223) `HareketEkleIc(hesapId, dto, islem, ekstreSatirId, bankaReferans)` internal metoduna çıkarılır; genel uç bu metodu ekstreSatirId=null ile çağırır. Davranış DEĞİŞMEZ. (b) Yeni BankaHareket satırına DedupeAnahtar yazılır. (c) BankaHareketleri.Add'in TEK YER olduğu K4-OK-2 gerekçesiyle sınıf başına yorum. (d) EkstreServisi bu metodu çağırır; kendi Add'ini YAZMAZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Banka.EkstreServisi>(); (satır 20 BankaServisi'nin yanına)
- **DEGISTIR** `dotnet/Kuyumcu.Api/Common/OranSiniri.cs` — public const string Ekstre = "ekstre"; + AddPolicy — saatte 20 yükleme (Belge politikasının deseni, OranSiniri.cs:40,68). Gerekçe: ayrıştırma CPU işidir ve 10.000 satırlık dosya ucuz değildir.
- **DEGISTIR** `dotnet/denetim.sh` — Yeni kural: denetle "BankaHareketleri.Add (yalnız BankaServisi)" "BankaHareketleri\\.Add\\(" 1 — K4-OK-2'yi CI'da zorlar. Gerekçe yorumu Hareketler.Add kuralının (denetim.sh içindeki mevcut kural) diliyle yazılır.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukBanka'ya (satır 508) ~22 yeni anahtar: EkstreYukle, EkstreDosyasi, Format, Prova, YeniSatir, Mukerrer, Hatali, Atla, Onayla, SeciliOnayla, Eslestir, OneriListesi, CariBagla, IbanOgrenildi, AcilisBakiye, KapanisBakiye, BakiyeUyusmuyor, DosyaZatenYuklendi, SatirBulunamadi, SablonIndir, KodlamaHatasi, TumunuSec. TR + EN ikisi de doldurulur (satır 1467 TR bloğu, 2401 EN bloğu).
- **YENI** `dotnet/Kuyumcu.Web/Pages/BankaEkstre.razor` — @page "/banka/ekstre/{IthalatId}" — eşleştirme ekranı. Sayfalı tablo (100 satır/sayfa), durum süzgeci, satır başına cari seçici + 'Atla', üstte 4 StatKart (toplam/yeni/mükerrer/hatalı), altta 'Seçilileri Onayla'. Mükerrer satırlar varsayılan olarak SEÇİLİ DEĞİL. Onay sonrası satır sayısıyla özet gösterilir.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Banka.razor` — Hareketler başlığının yanına (satır 63-65 düğme grubu) '+ Ekstre Yükle' düğmesi + yükleme modalı (InputFile ile dosya okunur, base64'e çevrilip POST edilir — BelgeYukleyici bileşeninin deseni). Yanıttaki ithalatId ile /banka/ekstre/{id}'ye yönlendirilir.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs` — Değişiklik gerekmeyebilir (GetAsync/PostAsync/PatchAsync jenerik, satır 24-33). Yalnız sablon.csv indirmesi için mevcut GetBaytAsync (satır 56) kullanılır. Ayak izine çakışma analizi için yazıldı.

## Ekranlar
- `/banka` · `dotnet/Kuyumcu.Web/Pages/Banka.razor` — Hesap seçiliyken '+ Ekstre Yükle' düğmesi ve yükleme modalı (dosya seç, format seç, yükle). Mevcut dekont akışı hiç değişmez. (menü/yetki: Menü DEĞİŞMEZ (KenarCubugu.razor:36-38 'banka'). Modül yetkisi 'banka' (BankaModulu.cs:328).)
- `/banka/ekstre/{ithalatId}` · `dotnet/Kuyumcu.Web/Pages/BankaEkstre.razor` — YENİ. Prova/eşleştirme ekranı: sayfalı satır listesi, durum süzgeci, satır başına cari bağlama ve atlama, toplu onay. Ekran menüden değil Banka ekranından değişkenle çağrılır (mevcut 'ekransız uç' sayımında bu kabul edilen desendir). (menü/yetki: Menüye YENİ SATIR EKLENMEZ. Rol: SAHIP/MUDUR/MUHASEBE; TEZGAHTAR 403.)

## İş kuralları (fail-closed)
- FAIL-CLOSED AYRIŞTIRMA: çözülemeyen satır ATLANMAZ, HATALI olarak yazılır ve ekranda hata mesajıyla görünür. 'Sessizce eksik içe aktarım' bu projede en pahalı hata sınıfıdır (banka bakiyesi tutmaz, kimse nedenini bulamaz).
- YÜKLEME HİÇBİR ŞEY YAZMAZ (deftere): /ekstre/yukle çağrısı banka_hareketleri'ne SIFIR satır yazar. Kanıt testle sabitlenir.
- ONAY OLMADAN HAREKET YOK: yalnız POST /onayla banka hareketi üretir ve yalnız gövdede kimliği AÇIKÇA verilen satırlar için. 'Tümünü onayla' ucu YOKTUR; ekran seçim yapar, sunucu listeyi ister.
- TEK YAZMA YOLU: EkstreServisi.Onayla, BankaServisi.HareketEkleIc'i çağırır. denetim.sh 'BankaHareketleri.Add = 1' kuralıyla zorlanır (K4-OK-2).
- MÜKERRER FAIL-CLOSED: dedupe anahtarı banka_hareketleri'nde zaten varsa satır MUKERRER işaretlenir ve varsayılan olarak SEÇİLMEZ. Benzersiz indeks (tenant_id, banka_hesap_id, dedupe_anahtar) son savunmadır: iki eşzamanlı onay isteği aynı satırı iki kez yazamaz.
- OTOMATİK EŞLEŞME YALNIZ IBAN: karsi_iban cari_ibanlari'nda TAM eşleşiyorsa eslesme_kaynagi='IBAN' ile otomatik bağlanır. Ünvan benzerliği hiçbir koşulda otomatik bağlamaz (K4-OK-5).
- BİR IBAN TEK CARİYE: (tenant_id, iban) benzersizdir. Başka bir cariye bağlama denemesi 409 döner ve mevcut sahibi gösterilir — sessizce taşınmaz.
- YÖN SÖZLEŞMESİ: CSV'de borç sütunu dolu ise GIDEN, alacak sütunu dolu ise GELEN. MT940 :61:'de 'C'→GELEN, 'D'→GIDEN; 'RC'/'RD' ters kayıtlardır ve ters yönle alınır. Tutar HER ZAMAN pozitif yazılır (BankaModulu.cs:191 yön katsayısı sözleşmesi korunur).
- PARA BİRİMİ HESAPTAN GELİR: satırın para birimi hesabın para birimidir (BankaModulu.cs:210 ile aynı). MT940 :60F:'teki para birimi hesabınkiyle uyuşmuyorsa DOSYA TÜMDEN REDDEDİLİR (400) — yanlış hesaba yüklenen ekstre sessizce kabul edilmez.
- GELECEK TARİH REDDEDİLİR: satır tarihi sunucu saatine göre gelecekteyse HATALI. Gerekçe paket 41'in şartlı vetosuyla aynıdır (PAKET-41 §VETO: kullanıcı tarihli kayıt MASAK kümülatifini bölmek için kullanılabilir).
- TARİH ARALIĞI VE GÜN SINIRI: ekran süzgeçleri ve raporlama Zaman.YerelGunAraligiUtc ile kurulur (Zaman.cs:31). Dosyadaki tarih (saat taşımaz) yerel gün başı UTC'ye çevrilir — Zaman.YerelGunBasiUtc (Zaman.cs:26).
- TEVSİK KAPISI ÇAĞRILMAZ (K4-OK-9); MASAK kapısı HareketEkleIc üzerinden paket 41 ile birlikte otomatik devreye girer ve bu paket onu ATLAMAZ.
- BOYUT VE SATIR TAVANI: ham dosya ≤ 2 MB, satır ≤ 10.000. Aşılırsa 400, kısmi içe aktarım YOK.
- DENETİM İZİ: yükleme (dosya adı + sha256 + satır sayısı) ve onay (onaylanan satır sayısı + üretilen hareket kimlikleri sayısı) ayrı kayıtlarla yazılır — ADR-0004 append-only, DenetimIzi.YazmaKaydet (DenetimIzi.cs:120) deseni.
- ONAYLANMIŞ SATIR DEĞİŞMEZ: durum ONAYLANDI olan satırda PATCH 409 döner. Yanlış onay, üretilen banka hareketi üzerinden düzeltilir (paket 33 düzeltme fişi yolu) — sahne tablosu geçmişi bozulmaz.

## Testler
- `dotnet/Kuyumcu.Tests/EkstreAyristiriciTestleri.cs` [saf]
  - CSV: ';' ayraçlı, TR ondalıklı (1.234,56) dosya doğru tutarla ayrışır
  - CSV: ',' ayraçlı, EN ondalıklı (1,234.56) dosya doğru tutarla ayrışır
  - CSV: borç sütunu dolu satır GIDEN, alacak dolu satır GELEN olur
  - CSV: bozuk tarihli satır HATALI işaretlenir, DİĞER satırlar ayrışmaya devam eder (atlanmaz)
  - ISO-8859-9 kodlu dosyada 'ŞİRKET' doğru okunur
  - MT940: :61: satırından tarih/yön/tutar/referans, :86: satırından açıklama ve karşı IBAN çıkar
  - MT940: çok satırlı :86: bloğu tek açıklamada birleşir
  - MT940: 'RC'/'RD' ters kayıt yönü tersine çevirir
  - MT940: :60F:/:62F: açılış-kapanış bakiyesi okunur
  - Ayrıştırıcı hiçbir testte DbContext kullanmaz (saf sınıf)
- `dotnet/Kuyumcu.Tests/CariEslestirmeTestleri.cs` [saf]
  - 'AYŞE DEMİR LTD. ŞTİ.' ile 'Ayşe Demir' normalleşince yüksek skor verir
  - 'Ayşe Demir' ile 'Ayşe Demirci' eşiğin ALTINDA kalır ya da kalsa bile ASLA otomatik uygulanmaz
  - tr-TR kültürü: 'ilker' → 'İLKER' (invariant kültürde 'ILKER' olurdu — tuzak sabitlenir)
- `dotnet/Kuyumcu.Tests/EkstreIceAktarmaTestleri.cs` [DB]
  - Yükleme sonrası banka_hareketleri'nde SIFIR yeni satır; ekstre_satirlari'nda N satır
  - Aynı dosya ikinci kez yüklenince 409 ve ikinci ithalat kaydı OLUŞMAZ
  - Ekstrede daha önce ELLE girilmiş bir dekontun aynısı varsa satır MUKERRER işaretlenir
  - Onayla: 3 satır seçilir → 3 banka hareketi oluşur, hesap bakiyesi tam 3 satırın net toplamı kadar değişir
  - Onayla: aynı satır ikinci kez onaylanmaya çalışılırsa 409 ve İKİNCİ hareket oluşmaz
  - Onayla: boş satırIdler listesi → 400
  - Karşı IBAN cari_ibanlari'nda kayıtlıysa satır otomatik ESLESTI gelir (eslesme_kaynagi='IBAN')
  - Elle cari bağlanınca IBAN öğrenilir; AYNI ekstre yeniden yüklenince satır otomatik eşleşir
  - Aynı IBAN'ı ikinci bir cariye bağlama denemesi 409
  - Hesabın para birimi TRY iken MT940 :60F: USD → dosya 400, hiçbir satır yazılmaz
  - Gelecek tarihli satır HATALI; onaya seçilemez
  - 10.001 satırlık dosya → 400, hiçbir tablo yazılmaz
  - Yabancı kiracının hesabına ekstre yüklemesi → 404
  - Yabancı kiracının satırını PATCH → 404
  - Onaylanmış satırda PATCH → 409
  - Denetim izinde yükleme ve onay için ayrı kayıt, satır sayısıyla
- `dotnet/Kuyumcu.Tests/EkstreEkranTestleri.cs` [bUnit]
  - Banka ekranındaki 'Ekstre Yükle' POST /bankalar/{id}/ekstre/yukle'ye base64 gövdeyle gider
  - Eşleştirme ekranı ilk açılışta GET /bankalar/ekstre/{id}?limit=100&offset=0 çağırır (sayfalama isteniyor — TB-19)
  - MUKERRER satırların onay kutusu varsayılan olarak İŞARETSİZ
  - Hiç satır seçilmemişken 'Seçilileri Onayla' düğmesi KAPALI
  - Cari seçilince PATCH /bankalar/ekstre/satir/{id} doğru gövdeyle gider
  - HATALI satırın hata mesajı ekranda görünür (sessizce gizlenmez)
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - POST /api/v1/bankalar/{id}/ekstre/yukle TEZGAHTAR ile 403
  - 3 MB'ı aşan gövde 413 ve ApiException'ın mevcut gövde sınırı çevirisi (ApiException.cs:53 bölgesi) devrede
  - Kimliksiz istek 401
- `dotnet/Kuyumcu.Tests/KiraciIzolasyonTestleri.cs` [DB]
  - Üç yeni tablo IKiraciVarlik uygular ve global filtre altında yabancı kiracıya görünmez (mevcut dosyanın desenine ek)

## Mutasyon kanıtları
- EkstreServisi.Yukle içinde satırlar doğrudan banka_hareketleri'ne yazılırsa → 'Yükleme sonrası SIFIR banka hareketi' KIRMIZI
- Dedupe anahtarı kontrolü kaldırılırsa → 'elle girilmiş dekontun aynısı MUKERRER' KIRMIZI
- (tenant_id, banka_hesap_id, dedupe_anahtar) benzersiz indeksi kaldırılırsa → 'aynı satır ikinci kez onaylanamaz' KIRMIZI
- Onayla, BankaServisi.HareketEkleIc yerine kendi BankaHareketleri.Add'ini yazarsa → denetim.sh 'BankaHareketleri.Add = 1' İHLAL (çıkış kodu 1)
- Ünvan benzerliği eşik üstünde otomatik uygulanırsa → 'Ayşe Demir / Ayşe Demirci otomatik bağlanmaz' KIRMIZI
- cari_ibanlari (tenant_id, iban) benzersizliği kaldırılırsa → 'aynı IBAN ikinci cariye 409' KIRMIZI
- Ayrıştırıcı bozuk satırı atlarsa (HATALI yazmak yerine) → 'bozuk satır HATALI, diğerleri ayrışır' KIRMIZI
- Para birimi kontrolü kaldırılırsa → 'TRY hesaba USD ekstre 400' KIRMIZI
- Gelecek tarih kontrolü kaldırılırsa → 'gelecek tarihli satır HATALI' KIRMIZI
- Satır tavanı (10.000) kaldırılırsa → '10.001 satır 400' KIRMIZI
- Ekran sayfalama yerine tüm satırları isterse (limit parametresi kaldırılırsa) → EkstreEkranTestleri sayfalama testi KIRMIZI
- Onayla boş liste kabul ederse → 'boş satırIdler 400' KIRMIZI
- UnvanNormalle invariant kültürle yazılırsa → 'ilker → İLKER' KIRMIZI

## Canlı doğrulama
- Yeni kiracı aç (EKSTRE-TEST). Kur kaydını bekle. TRY banka hesabı + 2 cari aç.
- Elle 1 dekont gir (5.000 TL GELEN, referanslı). SQL: banka_hareketleri 1 satır, dedupe_anahtar DOLU.
- 10 satırlık CSV yükle (içinde o dekontun aynısı + 1 bozuk tarihli satır olsun). Yanıt: 10 satır / 8 yeni / 1 mükerrer / 1 hatalı. SQL: banka_hareketleri HÂLÂ 1 satır.
- Aynı dosyayı tekrar yükle → 409. SQL: ekstre_ithalatlari 1 satır.
- Bir satırı elle cariye bağla → 200. SQL: cari_ibanlari'nda kaynak='OGRENILDI' satırı var.
- 5 satır seç, onayla → 200. SQL: banka_hareketleri 6 satır; hesap bakiyesi tam 5 satırın net toplamı kadar arttı/azaldı; ekstre_satirlari'nda 5 satır ONAYLANDI ve banka_hareket_id dolu.
- Aynı 5 satırı yeniden onayla → 409, SQL: satır sayısı 6'da kaldı.
- Aynı dosyayı ?tekrar=true ile yükle → tüm satırlar MUKERRER; hiçbirini onaylamadan çık. SQL: banka_hareketleri 6'da kaldı.
- İlk satırın IBAN'ını taşıyan ikinci bir ekstre yükle → o satır otomatik ESLESTI geldi (öğrenme kanıtı).
- MT940 örnek dosyası (Ziraat kamuya açık dokümanındaki örnek yapıya göre elle hazırlanmış) yükle → :61:/:86: satırları doğru tarih/tutar/yönle geldi; :60F:/:62F: ekranda gösterildi.
- USD hesap aç, TRY ekstresi yükle → 400, hiçbir satır yazılmadı.
- TEZGAHTAR kullanıcısıyla yükleme → 403.
- 3 MB'lık dosya → 413/400 (gövde sınırı).
- Migration ileri/geri: Down üç tabloyu ve banka_hareketleri'ndeki üç kolonu + benzersiz indeksi düşürmeli; Up geri koymalı. Down sonrası mevcut dekontlar OKUNABİLİR kalmalı.
- dotnet test 0 başarısız + ./denetim.sh TEMİZ (yeni BankaHareketleri.Add kuralı dâhil).
- Test kiracısını sil (API'yi önce durdur).

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Common/OranSiniri.cs`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Banka.razor`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs`
- `dotnet/Kuyumcu.Tests/KiraciIzolasyonTestleri.cs`

## Mevzuat dayanağı
- TCMB Basın Duyurusu 2026-13 (17.03.2026) — ÖHVPS 2.0.0 — **DOGRULANDI** — Açık bankacılık altyapısına erişim ödeme hizmeti sağlayıcısı lisansı gerektirir; kuyumcu ERP'si doğrudan bağlanamaz. Bu paketin dosya tabanlı yolu seçmesinin gerekçesidir (PAZAR-ARASTIRMASI-2026-09-16.md:491).
- Ziraat Bankası MT940/MT942 muhasebe entegrasyonu teknik dokümanı (kamuya açık) — **DOGRULANDI** — Ekstre dosyasını MÜŞTERİ kendi bankasından alır (e-posta/FTP/SFTP/web servis); bizim bankayla sözleşmemiz gerekmez. Paketi A sınıfına sabitler (PAZAR-ARASTIRMASI-2026-09-16.md:493).
- 459 sıra no.lu VUK Genel Tebliği — tevsik zorunluluğu — **DOGRULANDI** — Banka hareketi tanımı gereği tevsik edilmiş kanaldır; tevsik kapısı bu akışta çağrılmaz (paket 41 ile aynı gerekçe). Bu bir GEVŞETME değil, kapsamın doğru çizilmesidir.
- MASAK 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — Bu paket kendi başına MASAK kuralı KOYMAZ; kapıyı paket 41 kurar. Paket 32'nin tek sorumluluğu o kapıyı ATLAYAN ikinci bir yazma yolu açmamaktır (K4-OK-2).
- KVKK — banka ekstresindeki karşı taraf ad/ünvan ve IBAN'ı kişisel veridir — **DOGRULANMALI** — IBAN ve ünvan ADR-0003'ün şifreli kimlik alanları listesinde DEĞİL (bugün BankaHareket.KarsiIban düz metin — Entities.cs:473). Ekstre bunu ölçek olarak büyütür (ayda yüzlerce satır). Alan şifrelemesi gerekip gerekmediği güvenlik (Mehmet) kararıdır — AS-2.

## Riskler
- ÇAKIŞMA: paket 41 ile aynı üç dosyayı (BankaModulu.cs, Entities.cs, KuyumcuDbContext.cs) ve aynı migration alanını değiştirir. Aynı dalgada koşturulmamalı (K4-OK-10).
- MT940 lehçe farkı: her banka :86: alanını farklı doldurur; karşı IBAN/ünvanın çıkarılması bankaya göre değişir. Faz B'de 'çıkarılamadı' hâli NORMAL kabul edilir (satır HATALI değil, yalnız eşleşmemiş) — aksi hâlde ayrıştırıcı bankadan bankaya kırılır.
- Gerçek banka dosyası elimizde YOK; Faz B yalnız kamuya açık doküman yapısına göre yazılmış örnek dosyalarla doğrulanabilir. Bu bir kanıt boşluğudur ve raporda açıkça yazılmalıdır (Anayasa md.1). Faz A (CSV) bu riski taşımaz çünkü şablonu biz yayımlıyoruz.
- Mükerrer yanlış pozitifi: aynı gün aynı müşteriden iki özdeş 1.000 TL havale meşrudur ve referanssız CSV'de ayırt edilemez. Tasarım bunu MUKERRER işaretleyip kullanıcıya bırakır; 'sessizce iki kez yazmak' ile 'sessizce birini yutmak' arasında ikincisi seçilmiştir — çünkü görünür ve düzeltilebilir.
- Ölçek: 10.000 satırlık dosyanın onayında satır başına bir HareketEkleIc çağrısı ve bir SaveChanges pahalıdır. Faz A'da onay TEK SEFERDE en fazla 500 satırla sınırlanmalıdır (ekran zaten 100'er sayfa gösteriyor).

## Açık sorular
- AS-1 — ADR-0014 ÇELİŞKİSİ: görev talimatı 'veri içe aktarımı ADR-0014 ile yasak' diyor; ancak dosyayı okudum (dokumanlar/02-mimari/adr/0014-veri-disa-aktarimi.md) ve ADR yalnız DIŞA aktarımı düzenliyor, içe aktarım hakkında tek cümle yok. Bu paketin içe aktarımı ürün/cari verisi değil, yalnız banka hareketi satırıdır ve her satır mevcut yazma yolundan geçer. Yine de kararı oylama vermelidir: (a) mevcut ADR yeterli sayılır, paket sevk edilir; (b) yeni ADR-0016 'Dosyadan içe aktarım kapılıdır: deftere yazan tek yol korunur, insan onayı zorunludur' yazılır ve paket 47 de bu ADR'ye bağlanır. ÖNERİM: (b).
- AS-2 — KarsiIban/KarsiTarafAd şifrelenecek mi? Bugün düz metin (Entities.cs:472-473). Ekstre bunu ölçekli hâle getiriyor. Mehmet'in (06 Güvenlik) kararı gerekir; ADR-0003 kapsamı genişletilecekse bu paket ondan ÖNCE değil SONRA sevk edilmeli.
- AS-3 — Döviz hesabı ekstresi: banka_hareketleri'nde kur_snapshot_id YOK (Entities.cs:464-480). USD/EUR hesaba ekstre yüklenince TL karşılığı hiç hesaplanmıyor; paket 37 (pozisyon/net varlık) bunu isteyecek mi? Şu anki tasarım hesabın para biriminde saklar ve çevirmez.
- AS-4 — Onaylanan GELEN hareket cari borcunu KAPATSIN mı? Bugün elle dekont da kapatmıyor (BankaModulu.cs:203-218 hareketler'e hiç yazmıyor) — yani bu bir gerileme değil, devralınan bir boşluk. Kapatılacaksa HareketFabrikasi üzerinden ayrı bir paket gerekir (K4-OK-1).
- AS-5 — Gün kilidi (GunKapanisiKapisi) banka hareketine uygulanacak mı? Paket 41 bunu 'ayrı karar' diye bıraktı. Uygulanırsa kapanmış güne ait ekstre satırı onaylanamaz; bu MUHTEMELEN doğru ama kapanmış aya ait ekstre yüklemeyi imkânsız kılar. Oylama kararı.
- AS-6 — CSV kolon eşleme sihirbazı gerekli mi? Tasarım otomatik başlık tanıma + yayımlanan şablonla ilerliyor (sihirbaz yok). Müşteri bankası tanınmayan başlık kullanırsa kullanıcı dosyayı şablona göre düzenlemek zorunda kalır. Sihirbaz +1-2 gündür; paket 47 için de yeniden kullanılabilir.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Sahne (staging) tablosu kararı kanıta dayalı ve doğru: onaysız satırlar banka_hareketleri'ne girmediği için paket 41'in MASAK kümülatifi ekrandaki provadan şişmez. 'Tek yazma yolu = BankaServisi.HareketEkleIc' kararı deftere yazma tekeli felsefesinin banka tarafındaki karşılığı ve denetim.sh'la zorlanması doğru. IBAN'dan öğrenen eşleştirme + ünvanın asla otomatik eşleştirmemesi isabetli (yanlış eşleşme geri alınması pahalı bir hatadır).
  - Koşullar: AS-1 (ADR çelişkisi) oylama öncesi kapanmalı: ADR-0014 yalnız DIŞA aktarımı düzenliyor, içe aktarım hakkında hüküm yok. Yeni bir ADR ('dosyadan içe aktarım kapılıdır: deftere yazan tek yol korunur, insan onayı zorunludur') YAZILMADAN paket başlamaz — ve bu ADR paket 47 ile ORTAK yazılmalı, iki ayrı ADR açılmamalı (bkz. çapraz bulgu 1). · Önkoşul 41 (MASAK kapısının banka hareketlerine yayılması) bu oylamanın kapsamında DEĞİL. Paket 32, 41'den önce sevk edilirse K4-OK-2 (tek yazma yolu) yalnız bir niyet beyanı kalır; bu yüzden 'BankaHareketleri.Add = 1' denetim kuralı Faz A'nın İLK işi olmalı, 41 beklenmeden. · Mükerrer tespitinde (tenant, banka_hesap_id, dedupe_anahtar) UNIQUE indeksi MySQL'de NULL'lu satırları engellemez; mevcut satırların anahtarı NULL kalacağı için indeks geçmişe karşı koruma SAĞLAMAZ. Bu sınır ekranda ve belgede açıkça yazılmalı, 'mükerrer imkânsız' denmemeli. · AS-2 (KarsiIban/KarsiTarafAd şifrelenecek mi) Mehmet'in kararı olmadan Faz A sevk edilmez: ekstre bu alanları ölçekli hâle getiriyor (ayda yüzlerce satır) ve bugün düz metin. · MT940 (Faz B) için elimizde GERÇEK banka dosyası yok; sevk raporunda 'kamuya açık doküman yapısına göre yazıldı, gerçek banka çıktısıyla doğrulanmadı' AÇIKÇA yazılacak (Madde 1). Faz A (CSV, şablonu biz yayımlıyoruz) bu riski taşımıyor ve önce sevk edilmeli.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Tasarım kapıyı atlamıyor (tek yazma yolu, sahne tablosu, prova zorunlu, IBAN-only otomatik eşleşme) — güvenlik mimarisi doğru. Eksik olan kişisel veri koruması; ADR-0003 kapsamı bu pakette genişletilmeden sevk edilmez.
  - Koşullar: AS-2 KARARIM: karşı taraf IBAN + ünvan KİŞİSEL VERİDİR ve ekstre bunu ölçekli hâle getirir. Faz A'da: cari_ibanlari.iban AES (IAlanSifreleyici) + iban_hash HMAC; UNIQUE ve eşleştirme HASH üzerinden (MasakHashDinleyicisi deseni). ekstre_satirlari.karsi_iban aynı şekilde şifreli+hash; ham_satir (500) IBAN/ad içerdiği için de şifreli saklanır ya da IBAN maskelenerek (TR** **** 1234) yazılır. Mevcut BankaHareket.KarsiIban düz kalır (ayrı TB kaydı açılır, bu pakette geriye dönük şifreleme yok). · Paket 41 ÖNCE sevk (K4-OK-10); 41 gelmeden 32'nin onay ucu MASAK kapısını fiilen atlar — 41 sevk edilmeden 32 Faz A CANLIYA ALINMAZ. · BankaHareketleri.Add=1 denetim sayacı (K4-OK-2) *.mutorj hariç; Onayla YALNIZ HareketEkleIc çağırır (mutasyon kanıtı zorunlu). · Onay gövdesi ≤500 satır (riskte yazılı; uç kuralına taşınsın, aşımı 400); yükleme 2 MB ham/10.000 satır tavanı; [RequestSizeLimit(3_000_000)]; OranSiniri.Ekstre 20/sa. · Ayrıştırıcı DoS: satır uzunluğu tavanı (ör. 2.000 karakter) ve alan sayısı tavanı — 'CSV bomb' ile bellek şişmesin; saf test. · ham_satir ekranda YALNIZ metin olarak basılır (MarkupString yok); CSV enjeksiyon tırnağı girişte sökülür — ama dışa aktarımda (Kaynaklar.cs) yeniden kaçışlanır. · Gelecek tarihli satır HATALI (spec) + kapalı güne onay kararı (AS-5) verilene kadar: kapanmış güne düşen satır onaylanabilir AMA denetim izine 'kapalı gün' işaretiyle — sessiz olmaz. · ADR-0014 çelişkisi: (b) yeni ADR yazılsın; içe aktarım kapı ilkesi ('deftere yazan tek yol korunur, insan onayı zorunlu, prova yazmaz') 47 ile ortak.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Sahne tablosu kararı MASAK kümülatifi için kanıtlı ve doğru; mutasyon listesi geniş. Fakat kültür tuzağı testte sabitlenmemiş, çift katmanlı koruma tek mutasyonla ölçülüyor ve önkoşul paket bu turda yok.
  - Koşullar: Önkoşul 41 bu oylama setinde yok; paket 32'nin HareketEkleIc refaktörü 41'den önce sevk edilirse MASAK kapısı bağlanma noktası 41'de yeniden yazılır. Kabul ölçütü: 41 sevk edilmeden 32 Faz A BAŞLAMAZ, ya da 32 'MASAK kapısı burada çağrılacak' diye işaretli tek satırlık kanca bırakır ve 41 onu doldurur — ikisinden biri yazılsın. · CariEslestirmeTestleri 'ilker → İLKER' testi test kültürüne bağlı: TestOrtami/HttpOrtami CultureInfo ayarlamıyor (grep 0). UnvanNormalle içinde açıkça CultureInfo('tr-TR') kullanılsın ve test, iş parçacığı kültürünü InvariantCulture'a çekip yine 'İLKER' beklesin (aksi hâlde makine kültürü testi yeşil/kırmızı yapar). · EkstreIceAktarmaTestleri 'aynı satır ikinci kez onaylanmaya çalışılırsa 409' iki katmanlı koruma (durum=ONAYLANDI kontrolü + benzersiz indeks); mutasyon 'benzersiz indeks kaldırılırsa kırmızı' ancak durum kontrolü de kaldırılırsa ölçülebilir — iki mutasyon ayrı ayrı test edilsin ya da indeks testi db üzerinden doğrudan ikinci BankaHareket eklemeyle kurulsun. · Onay 500 satır sınırı riskte yazılmış ama uç/test listesinde yok; 'satirIdler > 500 → 400' testi eklensin. · MT940 gerçek dosya yok (kanıt boşluğu) — Faz B kabul raporunda 'gerçek banka dosyasıyla test edilmedi' cümlesi zorunlu; Faz B'nin sevk kararı Faz A'dan ayrı verilsin. · AS-1 (ADR-0014 içe aktarım) paket 47 ile aynı ADR-0016'yı talep ediyor — tek ADR yazılsın (çapraz bulgu).

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO: Mehmet — paket 41 (MASAK kapısının BankaServisi.HareketEkle'ye bağlanması) sevk edilmeden 32'nin toplu onay ucu CANLIYA ALINAMAZ (toplu üretim MASAK kümülatifini görmeden çalışır); ayrıca AS-2 IBAN/ünvan şifrelemesi Faz A'nın parçasıdır. Dalga planı 41'i 32'den önce koyarak vetoyu karşılar.
- AS-2 çözümü (Mehmet kararı, Bertan da Mehmet'in kararını şart koşmuştu): cari_ibanlari.iban AES (IAlanSifreleyici) + iban_hash HMAC; eşleştirme ve UNIQUE hash üzerinden (MasakHashDinleyicisi deseni); ekstre_satirlari.karsi_iban şifreli+hash; ham_satir (500) şifreli ya da IBAN maskeli (TR** **** 1234). Mevcut BankaHareket.KarsiIban düz kalır, ayrı TB kaydı açılır.
- AS-1 çözümü (üç mercek ortak): ADR-0014 yalnız DIŞA aktarımı düzenler; yeni TEK ADR yazılsın — 'dosyadan içe aktarım kapılıdır: deftere yazan tek yol korunur, insan onayı zorunlu, prova yazmaz' — paket 47 ile ORTAK, iki ayrı ADR açılmasın (ADR numara çakışması çapraz bulgusu).
- Tek yazma yolu: 'BankaHareketleri.Add = 1' denetim kuralı (K4-OK-2, *.mutorj hariç) Faz A'nın İLK işi, 41 beklenmeden; Onayla YALNIZ HareketEkleIc çağırır, mutasyon kanıtı zorunlu. 41 henüz gelmemişse 'MASAK kapısı burada çağrılacak' tek satırlık kanca bırakılır.
- Sınırlar uç kuralına taşınsın: onay gövdesi ≤500 satır (aşım 400, testi eklensin), yükleme 2 MB / 10.000 satır tavanı, [RequestSizeLimit(3_000_000)], OranSiniri.Ekstre 20/sa; ayrıştırıcı DoS için satır uzunluğu (~2.000 karakter) ve alan sayısı tavanı (saf test).
- Çıktı güvenliği: ham_satir ekranda YALNIZ metin (MarkupString yok); CSV enjeksiyon tırnağı girişte sökülür, dışa aktarımda (Kaynaklar.cs) yeniden kaçışlanır.
- Mükerrer sınırı dürüstçe yazılsın: (tenant, banka_hesap_id, dedupe_anahtar) UNIQUE indeksi MySQL'de NULL'lu eski satırları engellemez — geçmişe karşı koruma YOK; ekranda ve belgede 'mükerrer imkânsız' denmez. 409 koruması iki katmanlı (durum kontrolü + indeks) ve iki mutasyon AYRI ölçülsün.
- AS-5 ara karar: kapanmış güne düşen satır onaylanabilir AMA denetim izine 'kapalı gün' işaretiyle (sessiz olmaz); gelecek tarihli satır HATALI.
- Kültür tuzağı: UnvanNormalle içinde açıkça CultureInfo('tr-TR'); test iş parçacığı kültürünü InvariantCulture'a çekip yine 'İLKER' beklesin (TestOrtami kültür ayarlamıyor).
- Faz A (CSV, şablonu biz yayımlıyoruz) önce sevk; MT940 Faz B'nin sevk kararı ayrı verilsin ve raporda 'gerçek banka dosyasıyla doğrulanmadı, kamuya açık doküman yapısına göre yazıldı' AÇIKÇA yazılsın (Madde 1).
