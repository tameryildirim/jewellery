# Paket 27 — Çoklu birim cari bakiye (TL + has gram + USD/EUR/GBP)

> Katman: Katman 4 — Cari ve finans (paket 27) · Tahmin: 3–4 gün (Faz A 2–3 · Faz B 1–2) — PARITE tahminiyle aynı gün · Önkoşullar: yok · Kaynak: katman mimarı K4-27 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 4

## Hedef
Perakende kuyumcuda müşteri ya da tedarikçiyle hesap her zaman TL veya has gram üzerinden kapanmaz: turist/gurbetçi müşteriye dövizle satış yapılıp bakiye dövizle bırakılır, tedarikçiyle USD üzerinden hesap tutulur, eski programdan devreden USD/EUR bakiyeler vardır. Bugün bunlar ya TL'ye çevrilip kaydediliyor (kur değişince hesap tutmuyor) ya da hiç kaydedilmiyor. Bu paket cari hesabı üçüncü bir paralel defterle (döviz) tamamlar: borç hangi para biriminde doğduysa o para biriminde durur, aynı para biriminde kapanır, ekranda ayrı satır olarak görünür.

## Kodda bugünkü durum
cari_bakiyeler tablosunda YALNIZ iki bakiye kolonu var: tl_bakiye decimal(15,2) ve has_gram_bakiye decimal(15,6) (Entities.cs:150-151). Bakiyeye yazan üç yer var: CariModulu.BakiyeGuncelle (CariModulu.cs:648-675), SatisModulu.CariBakiyeGuncelle (SatisModulu.cs:1633-1665), EmanetModulu.BakiyeGuncelle (EmanetModulu.cs:221-247) — üçü de yalnız (tlDelta, hasGramDelta) alır. Okuyanlar: CariModulu.Listele/Getir/BakiyeGetir (CariModulu.cs:281,332,400), BakiyeFormat (CariModulu.cs:676-682), VadeKapisi.KrediLimitiDogrula (VadeKapisi.cs:56), VadeKapisi.AcikBorclar (VadeKapisi.cs:130), RaporModulu.CariYaslandirma (RaporModulu.cs:290) ve RaporModulu.VadeTakibi (RaporModulu.cs:361), CariKaynagi CSV (Kaynaklar.cs:73). Döviz altyapısı BAŞKA yerlerde zaten var: KurSnapshot.UsdTry/EurTry/GbpTry (Entities.cs:112-114), Kasa.ParaBirimi (Entities.cs:425), KasaHareket.ParaBirimi (Entities.cs:439), BankaHesap para birimi regex TRY|USD|EUR|GBP (BankaModulu.cs:52), fişte DOVIZ ödeme bacağı (OdemeSepeti.cs:17,32-42,176-200 + SatisModulu.cs:894-910,1004-1008). Hareket.ParaBirimi kolonu VAR (Entities.cs:285) ama hiçbir modül yazmıyor; tek okuyucu CSV dışa aktarımı (Kaynaklar.cs:154) — yani bugün her defter satırı fiilen TRY. Sonuç: döviz kasaya girebiliyor, bankada tutulabiliyor, fişte ödeme aracı olabiliyor; ama CARİYE DÖVİZ BORCU YAZILAMIYOR.

## Kapsam
**İçinde:**
- cari_bakiyeler'e usd_bakiye / eur_bakiye / gbp_bakiye kolonları (decimal(15,2))
- hareketler'e doviz_tutar kolonu (decimal(15,2), nullable) + mevcut para_birimi kolonunun ilk kez gerçek kullanımı
- POST /api/v1/cari/{id}/doviz-veresiye — döviz cinsinden borç doğurma
- POST /api/v1/cari/{id}/doviz-tahsilat — efektif dövizle tahsilat (döviz kasasına girer)
- Mevcut POST /api/v1/cari/{id}/duzeltme ucuna döviz deltası (açılış/devir bakiyesi için)
- Bakiye okuma uçlarının (Getir, Listele, BakiyeGetir, Ekstre) döviz bakiyelerini döndürmesi
- Cariler.razor: döviz bakiye sütunu/rozeti + döviz veresiye/tahsilat formu
- VadeKapisi.KrediLimitiDogrula'nın döviz borcunu TL karşılığıyla hesaba katması
- CariKaynagi CSV dışa aktarımına üç döviz kolonu
- Faz B: döviz açık borç/vade türetimi, raporlarda döviz kırılımı

**Dışında:**
- Döviz alış/satış (sarraflık) ucu — PAKET-15 kararıyla kapalı, açılmaz
- Çapraz kapatma (USD borcunu TL ile ödeme) ve kur farkı hareketi — K27-G, ayrı oylama
- Dönem sonu kur değerlemesi / TDHP muhasebe fişi — mali müşavirin işi, ADR kapsamı dışı
- Satış fişi, alış, hurda, emanet yollarında döviz bakiyesi üretimi (K27-C, K27-K)
- Banka ekstresinden döviz hareketi eşleştirme — paket 32
- Döviz cinsinden fiyat listesi / etiket — paket 17 kapsamı, açılmaz

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| cari_bakiyeler | Üç yeni kolon eklenir (Faz A) | usd_bakiye decimal(15,2) NOT NULL DEFAULT 0 · eur_bakiye decimal(15,2) NOT NULL DEFAULT 0 · gbp_bakiye decimal(15,2) NOT NULL DEFAULT 0. İşaret: + = cari bize borçlu (K27-B). | Yeni indeks YOK. Mevcut HasIndex(TenantId) (KuyumcuDbContext.cs:138) yeterlidir; döviz bakiyesi olanları bulma sorgusu kiracı içinde tarama yapar ve perakende cari sayısında kabul edilebilir. IKiraciVarlik + tenant_id zaten var (Entities.cs:146-149), yeni varlık eklenmediği için denetim.sh:66 sayacı değişmez. | YYYYMMDDHHMMSS_CariDovizBakiyesi |
| hareketler | Bir yeni kolon; mevcut para_birimi ilk kez TRY dışına çıkar | doviz_tutar decimal(15,2) NULL — efektif döviz miktarı, İŞARETLİ (TlTutari ile aynı yön: + alacak doğuran, - borç kapatan). null = TL/has satırı (bugünkü tüm satırlar). para_birimi mevcut kolon, USD|EUR|GBP yazılır. | Yeni indeks yok; cari ekstresi zaten cari_id üzerinden filtreleniyor (CariModulu.cs:598-601). | Aynı migration içinde (YYYYMMDDHHMMSS_CariDovizBakiyesi) |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/cari/{id}/doviz-veresiye` | SAHIP, MUDUR, MUHASEBE | Döviz cinsinden borç doğurur. Gövde: { paraBirimi: USD|EUR|GBP, tutar: >0, vadeGun?, aciklama?, kurSnapshotId, masak? }. Hareket: Tur=DOVIZ_VERESIYE, ParaBirimi=seçilen, DovizTutar=+tutar, TlTutari=+TL karşılığı (round 2, AwayFromZero), OdemeIzi.Yok (K27-E). Bakiye: ilgili döviz kolonu += tutar. Yanıt: güncel cari + bakiye. | KurDogrula + para biriminin kuru var mı (fail-closed) · MasakKapisi.DogrulaKumulatif(TL karşılığı) · VadeKapisi.KrediLimitiDogrula (döviz dahil, K27-I) · şube zorunlu. Tevsik ÇALIŞMAZ (nakit el değiştirmiyor). |
| POST | `/api/v1/cari/{id}/doviz-tahsilat` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR (mevcut tahsilat ucuyla aynı) | Efektif dövizle tahsilat. Gövde: { paraBirimi, tutar: >0, kasaId, aciklama?, kurSnapshotId, masak? }. Hareket: Tur=DOVIZ_TAHSILAT, DovizTutar=-tutar, TlTutari=-TL karşılığı, OdemeIzi.Nakitten(TL karşılığı) — A12 gereği efektif döviz NAKİTTİR. Bakiye: döviz kolonu -= tutar. Kasa: seçilen kasa EFEKTİF miktarla artar (TL değil — SatisModulu.cs:1006 deseni), kasa_hareketleri satırı ParaBirimi=seçilen, OdemeTuru=DOVIZ. | KurDogrula + kur varlığı · IslemDogrulayici.KasaDogrula(db, kasaId, tenant, beklenenParaBirimi=paraBirimi, an) — K6 para birimi kapısı + gün kilidi · TevsikKapisi.Dogrula(TL karşılığı) · MasakKapisi.DogrulaKumulatif(TL karşılığı) |
| POST | `/api/v1/cari/{id}/duzeltme` | SAHIP, MUDUR, MUHASEBE (mevcut [RolGerekli] değişmez) | MEVCUT uç genişletilir (paket 33, CariModulu.cs:583-645). CariDuzeltmeDto'ya opsiyonel { dovizParaBirimi?, dovizDelta? } eklenir. Verilirse Tur=CARI_DUZELTME satırına ParaBirimi + DovizTutar=dovizDelta yazılır ve ilgili döviz kolonu güncellenir. Gerekçe (>=20 karakter) ve OdemeIzi.Yok kuralları AYNEN korunur. Eski sistemden devreden döviz bakiyesinin TEK meşru giriş yoludur (ADR-0014 içe aktarımı yasaklıyor). | Gerekçe uzunluğu · sıfır delta reddi (artık üç deltadan en az biri dolu olmalı) · KurDogrula · KrediLimitiDogrula (borcu artıran yönde) |
| GET | `/api/v1/cari/{id}/bakiye` | modül yetkisi (mevcut) | MEVCUT uç; yanıta usdBakiye, eurBakiye, gbpBakiye eklenir. BakiyeFormat (CariModulu.cs:676-682) tek noktadan genişletilir, böylece Getir/BakiyeGetir/Ekstre üçü birden aynı alanları döndürür. | — |
| GET | `/api/v1/cari` | modül yetkisi (mevcut) | MEVCUT liste; satırlara usdBakiye/eurBakiye/gbpBakiye eklenir (CariModulu.cs:302-319 projeksiyonu). Yeni sorgu YOK — bakiyeler zaten tek sorguda çekiliyor (CariModulu.cs:280-286). | Okuma izi zaten düşüyor (CariModulu.cs:271) |
| GET | `/api/v1/raporlar/doviz-pozisyon` | SAHIP, MUDUR, MUHASEBE | FAZ B. Kiracının döviz alacak/borç pozisyonu: para birimi başına toplam alacak, toplam borç, net; sunucuda SUM ile (TB-19). Ayrıca güncel kur kaydıyla TL karşılığı — 'bilgi amaçlı' etiketiyle. Paket 37 (pozisyon/net varlık) bunu tüketir. | Sınırsız ToListAsync YOK; GROUP BY sunucuda |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/DovizBakiye.cs` — Para birimi <-> kolon eşlemesinin TEK yeri. static class DovizBakiye: Gecerli(string pb) -> USD|EUR|GBP kontrolü (fail-closed, TRY reddedilir çünkü TRY tl_bakiye'dir); Oku(CariBakiye b, string pb) -> decimal; Ekle(CariBakiye b, string pb, decimal delta) -> void; KurAl(KurSnapshot s, string pb) -> decimal? (OdemeSepeti.DovizKurlari.Kur ile AYNI switch mantığı, tek fark kaynağın snapshot olması); TlKarsiligi(decimal miktar, decimal kur) -> Math.Round(miktar*kur, 2, AwayFromZero). NEDEN AYRI DOSYA: üç kolona doğrudan atama yapılan her yeni nokta, bir sonraki para biriminde unutulacak bir yer demektir; denetim.sh bu dosyanın dışında 'UsdBakiye +=' benzeri atama saymaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs` — CariBakiye'ye (satır 146-154 arası) UsdBakiye/EurBakiye/GbpBakiye özellikleri; Hareket'e (satır 275-335 bloğu) DovizTutar özelliği + ParaBirimi'nin yorumunun güncellenmesi ('bugüne kadar hep TRY idi; DOVIZ_* türlerinde efektifin para birimidir').
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — Yeni DbSet YOK, yeni HasQueryFilter YOK (CariBakiye ve Hareket zaten kapsamda). Yalnız mevcut CariBakiye yapılandırmasının (satır 138) yorumu güncellenir. Migration snapshot bu dosyayı takip ettiği için ayak izine dahil.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` — DTO'lar: DovizVeresiyeDto, DovizTahsilatDto (yeni); CariDuzeltmeDto'ya dovizParaBirimi/dovizDelta. Servis: DovizVeresiyeGir, DovizTahsilatGir (yeni metotlar, VeresiyeGir/TahsilatGir desenini birebir izler); BakiyeGuncelle'ye opsiyonel (string? paraBirimi, decimal dovizDelta) parametreleri — TL/has imzası bozulmadan; BakiyeFormat + Listele projeksiyonu + Olustur'daki sıfır bakiye satırı. Controller: iki yeni uç, [RolGerekli] TAM eşleşmeyle. Duzeltme'nin 'en az bir fark' kontrolü üç deltayı kapsayacak şekilde genişler.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Vade/VadeKapisi.cs` — KrediLimitiDogrula (satır 50-66): imzaya opsiyonel KurSnapshot (veya kurSnapshotId) eklenir. Mevcut TL bakiyesine, döviz bakiyelerinin o kurdaki TL karşılığı eklenir. Döviz bakiyesi 0'dan farklı ve kuru yoksa BadRequestException — sessiz atlama YOK. Çağıran tüm yollar (CariModulu.cs:410,610; SatisModulu üzerinden Hazirla) derleyici tarafından zorlanır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — FAZ A: CariYaslandirma'nın 'bakiyesi olan cari' filtresi (satır 295-298) döviz kolonlarını da sayar, ama yaşlandırma dilimleri TL kalır ve döviz satırı 'döviz bakiyesi var, vadelendirilmedi' notuyla işaretlenir — sessizce gizlenmez. VadeTakibi (satır 361) TlBakiye>0 filtresini KORUR (döviz vadesi Faz B). FAZ B: DovizPozisyon uç metodu.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs` — CariKaynagi.Basliklar (satır 65-67) ve Satirlar (satır 78-92) üç döviz kolonu alır. HareketKaynagi (satır ~154) doviz_tutar kolonunu ekler — para_birimi zaten yazılıyor. ADR-0014 gereği çıktı yine şifreli arşiv yolundan geçer, yeni ham döküm yolu açılmaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukCari'ye (satır 165-189) yeni required alanlar: DovizBakiye, DovizVeresiye, DovizTahsilat, ParaBirimi, EfektifMiktar, DovizBorcuVar, TlKarsiligiBilgiAmacli, KurYokDovizYapilamaz. Tr bloğu (satır ~1238) ve En bloğu (satır ~2172) BİRLİKTE güncellenir — En eksik kalırsa 'required' yüzünden DERLENMEZ, bu zorlama kasıtlıdır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Cariler.razor` — CariVeri/BakiyeVeri record'larına (satır 387-388) üç döviz alanı; liste tablosuna 'Döviz' sütunu (yalnız sıfırdan farklı olanlar rozet olarak: '1.250,00 USD'); detay panelinde (satır ~321) döviz bakiye kartı; iki yeni form (döviz veresiye / döviz tahsilat) — para birimi seçimi, efektif miktar, kasa seçimi (yalnız seçilen para birimindeki kasalar listelenir), TL karşılığı SUNUCU hesabıdır, kullanıcı yazamaz (A12 M73 mutasyonuyla aynı gerekçe). Roller ekranda da gizlenir ama kapı sunucudadır. Razor özniteliğinde bloklu lambda YASAK — adlandırılmış metot (tuzak listesi).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — FAZ B: döviz pozisyon bölümü; YaslananCari record'una döviz alanları. Faz A'da yalnız 'bu carinin döviz bakiyesi var' uyarı rozeti.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — İki yeni sayaç: (1) 'Doviz kolonuna dogrudan atama (yalniz DovizBakiye.cs)' — desen 'UsdBakiye[ ]*[+-]?=|EurBakiye[ ]*[+-]?=|GbpBakiye[ ]*[+-]?=', beklenen 3 (yalnız DovizBakiye.Ekle içinde). (2) 'DOVIZ_ hareket turu yazan yer (yalniz CariModulu)' — desen 'DOVIZ_VERESIYE|DOVIZ_TAHSILAT', beklenen sabit sayı. BEKLENEN_VARLIK=15 değişmez (yeni varlık yok).

## Ekranlar
- `/cariler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Cariler.razor` — Liste: TL ve Has sütunlarının yanına 'Döviz' sütunu — sıfırdan farklı her para birimi ayrı rozet, hiçbir toplama yok. Detay: üç bakiye kartı (TL / Has / Döviz) + ekstre. İki yeni işlem formu: Döviz Veresiye (para birimi, miktar, vade gün, gerekçe) ve Döviz Tahsilat (para birimi, efektif miktar, kasa). Her ikisinde TL karşılığı canlı gösterilir ama SALT OKUNUR ve sunucu yeniden hesaplar; uyuşmazlıkta 400. Kur kaydında o para biriminin kuru yoksa form açılmaz, 'kur yok' mesajı gösterilir. (menü/yetki: Mevcut 'cari' modül yetkisi. Döviz veresiye düğmesi TEZGAHTAR'a gizli (kapı sunucuda [RolGerekli] ile); döviz tahsilat TEZGAHTAR'a açık.)
- `/raporlar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — Faz A: cari yaşlandırma satırında 'döviz bakiyesi var (vadelendirilmedi)' rozeti — bilgi saklanmaz. Faz B: para birimi başına alacak/borç/net pozisyon tablosu. (menü/yetki: Mevcut 'raporlar' modül yetkisi)

## İş kuralları (fail-closed)
- İŞARET: döviz bakiyesi + ise cari BİZE borçlu, - ise BİZ cariye borçluyuz (tl_bakiye ve has_gram_bakiye ile aynı perspektif).
- PARA BİRİMİ KÜMESİ KAPALI: USD, EUR, GBP. TRY döviz olarak kabul EDİLMEZ (TRY zaten tl_bakiye'dir) — gönderilirse 400.
- KUR FAIL-CLOSED: seçilen para biriminin kuru işlem anındaki kur kaydında yoksa veya <= 0 ise istek reddedilir. 'Yaklaşık kur' yoktur.
- TL KARŞILIĞI SUNUCUDA: istemci TL değeri gönderemez; gönderirse yok sayılmaz, 400 döner (OdemeSepeti.cs:194-199 deseniyle aynı sertlik).
- AYNI PARA BİRİMİNDE KAPANMA: döviz borcu yalnız aynı para biriminde azalır. Çapraz kapatma Faz A'da 400 ile reddedilir (K27-G).
- DEFTER TEKELİ: döviz hareketleri de HareketFabrikasi.Ekle'den geçer; Hareketler.Add sayısı 1'de kalır.
- TL TÜRETİMİNİ KİRLETMEME: döviz satırlarında VeresiyeTutar daima 0 ve Tur asla 'TAHSILAT' değildir; böylece VadeKapisi.AcikBorclar hiçbir döviz satırını TL borcu saymaz (K27-E).
- MASAK: döviz işleminin TL karşılığı MasakKapisi.DogrulaKumulatif'e verilir; eşik ve bağlantılı işlem kümülatifi TL üzerinden işler (185.000 TL eşiği — DOGRULANDI).
- TEVSİK: efektif döviz tahsilatı NAKİT sayılır; NakitTutar = TL karşılığı yazılır ve TevsikKapisi.Dogrula çağrılır (30.000 TL haddi — DOGRULANDI, A12 ile aynı gerekçe: kapı yalnız sıkılaşır).
- KASA PARA BİRİMİ KAPISI (K6): döviz tahsilatında kasa, tahsil edilen para biriminde olmak ZORUNDA; KasaDogrula beklenen para birimiyle çağrılır. Kasaya EFEKTİF miktar yazılır, TL değil.
- GÜN KİLİDİ: döviz tahsilatı KasaDogrula(..., an) üzerinden gün kapanışı kapısına tabidir; kapanmış güne yazılamaz.
- KREDİ LİMİTİ: döviz borcunun TL karşılığı da limite sayılır; kur yoksa limit kontrolü REDDEDER (K27-I).
- VADE: Faz A'da döviz borcunun vadesi YOKTUR ve bu ekranda açıkça yazılır ('vadelendirilmedi'). Gizlenmez, bugüne vade yazılmaz (paket 33'ün SAYIM_DUZELTME gerekçesiyle aynı).
- TOPLAMA YASAĞI: TL, has ve döviz bakiyeleri hiçbir uçta/ekranda tek sayıya toplanmaz. TL karşılığı yalnız 'bilgi amaçlı' etiketiyle, güncel kurla, gösterimde hesaplanır.
- DENETİM İZİ: döviz düzeltmesi paket 33'ün iki katmanlı izini aynen kullanır (otomatik yazma izi + DenetimServisi.YazmaKaydet, Hassas=true, Detay'da delta + para birimi + gerekçe).
- ROL: döviz veresiye ve döviz düzeltme SAHIP/MUDUR/MUHASEBE; döviz tahsilat mevcut tahsilatla aynı (TEZGAHTAR dahil). Ekran gizlemesi kapı sayılmaz.

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/CariDovizBakiyeTestleri.cs` [DB]
  - Döviz veresiye 1.000 USD → usd_bakiye = 1000, tl_bakiye DEĞİŞMEZ
  - Döviz tahsilat 400 USD → usd_bakiye = 600, USD kasa bakiyesi +400 (efektif), TL kasa dokunulmaz
  - Döviz hareketinde TlTutari = miktar × kur (2 hane, AwayFromZero) ve DovizTutar işaretli
  - Döviz satırı VadeKapisi.AcikBorclar'a HİÇ girmez: TL borcu 0 kalır, 'vadesiz bakiye' de üretilmez
  - İki farklı para birimi aynı caride bağımsız: USD +1000, EUR -300 → kolonlar karışmaz
  - Kur kaydında GbpTry null iken GBP veresiye → BadRequestException, hiçbir satır yazılmaz
  - TRY para birimiyle döviz ucu → BadRequestException
  - Çapraz kapatma denemesi (USD borcuna EUR tahsilat) bakiyeleri karıştırmaz; USD borcu aynen kalır
  - Kredi limiti 10.000 TL, cari 500 USD borçlu (kur 40 → 20.000 TL) → yeni TL veresiye limitten reddedilir
  - Döviz bakiyesi varken kur kaydı o para birimini taşımıyorsa limit kapısı REDDEDER (sessiz geçmez)
  - Düzeltme fişiyle açılış USD bakiyesi girilir; gerekçe < 20 karakter → red; denetim izinde para birimi görünür
  - Kiracı izolasyonu: A kiracısının döviz bakiyesi B kiracısından okunamaz/yazılamaz
  - MASAK: 185.000 TL karşılığını aşan döviz veresiye kimlik tespiti olmadan reddedilir
  - Tevsik: 30.000 TL karşılığını aşan efektif döviz tahsilatı reddedilir
  - Gün kapanmış şubede döviz tahsilatı reddedilir
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/HttpCariDovizTestleri.cs` [HTTP]
  - TEZGAHTAR rolüyle POST /cari/{id}/doviz-veresiye → 403
  - TEZGAHTAR rolüyle POST /cari/{id}/doviz-tahsilat → 200
  - Yanıt camelCase: usdBakiye/eurBakiye/gbpBakiye alanları GET /cari/{id}/bakiye içinde var
  - GET /cari listesinde döviz alanları dönüyor ve sıfır bakiyede 0 geliyor (null değil)
  - Geçersiz para birimi (CHF) → 400, model bağlama hatası biçimi korunuyor
  - Kimliksiz istek → 401 (middleware sırası korunmuş)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/CariEkranTestleri.cs` [bUnit]
  - Sahte API döviz alanlarını dönünce liste satırında 'USD' rozeti görünür
  - Döviz bakiyesi 0 olan caride rozet HİÇ görünmez (gürültü yok)
  - Döviz tahsilat formunda kasa listesi yalnız seçilen para birimindeki kasaları gösterir
  - TL karşılığı alanı salt okunurdur (readonly/disabled) — kullanıcı yazamaz
  - Kur yoksa form yerine 'kur yok' mesajı çıkar
  - DİKKAT: mevcut CariEkran testlerinin sahte API'sine yeni uçların gerçekçi yanıtı eklenmezse JsonException ile kırılırlar (tuzak listesi)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DovizBakiyeSaflikTestleri.cs` [saf]
  - DovizBakiye.TlKarsiligi yuvarlaması AwayFromZero ve 2 hane (0,005 sınırı)
  - DovizBakiye.Gecerli: TRY, boş, küçük harf 'usd', CHF reddedilir
  - DovizBakiye.Ekle yalnız hedef kolonu değiştirir, diğer ikisini değiştirmez
  - Türkçe harf tuzağı: para birimi karşılaştırmasında ToUpperInvariant kullanılmaz, tam eşleşme aranır

## Mutasyon kanıtları
- M76 — DovizBakiye.Ekle'de USD dalını EUR kolonuna yaz → 'iki farklı para birimi bağımsız' ve 'döviz veresiye USD' testleri kırmızı
- M77 — Kur yokluğunda fırlatılan istisnayı kaldırıp kur=1 varsay → 'GbpTry null iken red' testi kırmızı
- M78 — Döviz hareketinde Tur'u 'TAHSILAT' yap → 'döviz satırı AcikBorclar'a girmez' testi kırmızı (TL borcu sahte şekilde azalır)
- M79 — Döviz hareketinde VeresiyeTutar'ı doldur (OdemeIzi.Veresiyeden kullan) → aynı test kırmızı (borç hem TL hem döviz sayılır)
- M80 — KrediLimitiDogrula'dan döviz TL karşılığı toplamını çıkar → 'limit döviz borcunu görür' testi kırmızı
- M81 — Döviz bakiyesi varken kuru olmayan durumda limit kapısını sessizce atla → ilgili fail-closed testi kırmızı
- M82 — Döviz tahsilatında OdemeIzi.Nakitten yerine OdemeIzi.Yok kullan → tevsik 30.000 TL testi kırmızı (efektif döviz nakit sayılmıyor)
- M83 — KasaDogrula'ya beklenen para birimi yerine 'TRY' geç → 'USD kasa zorunlu' testi kırmızı
- M84 — Kasaya efektif yerine TL karşılığını yaz → 'USD kasa bakiyesi +400' testi kırmızı
- M85 — MasakKapisi çağrısını TL karşılığı yerine efektif miktarla yap → MASAK eşik testi kırmızı
- M86 — İstemcinin gönderdiği TL değerini kabul et (sunucu hesabını atla) → ekran/HTTP uyuşmazlık testi kırmızı
- M87 — TRY'yi geçerli döviz say → 'TRY reddedilir' testi kırmızı
- M88 — Duzeltme'de üç deltadan biri doluyken 'sıfır delta' reddini eski hâline bırak → düzeltmeyle açılış USD bakiyesi testi kırmızı

## Canlı doğrulama
- Test kiracısı aç (ör. DOVIZ-CARI-TEST), kur kaydı oluşana kadar bekle (yeni kiracıda ~10 sn; /kur/guncel id gelene kadar bekle — yoksa her istek kurSnapshotId required 400 verir)
- USD kasası oluştur (POST /kasalar, paraBirimi USD) ve TRY kasasının ayrı durduğunu doğrula
- POST /cari/{id}/doviz-veresiye ile 1.000 USD borç yaz; GET /cari/{id}/bakiye yanıtında usdBakiye=1000 ve tlBakiye=0 olduğunu OKU (curl -o /dev/null KULLANMA)
- hareketler tablosunda satırı kontrol et: tur=DOVIZ_VERESIYE, para_birimi=USD, doviz_tutar=1000, tl_tutari = 1000 × o günkü usdTry, veresiye_tutar=0, vade_tarihi NULL
- GET /raporlar/vade-takibi: bu cari geciken listesinde GÖRÜNMEMELİ (TL borcu yok); cari yaşlandırmada 'döviz bakiyesi var' rozetiyle görünmeli
- POST /cari/{id}/doviz-tahsilat ile 400 USD tahsil et; usdBakiye=600, USD kasa bakiyesi +400,00 USD, kasa_hareketleri satırı para_birimi=USD
- Kur kaydında GBP yoksa GBP veresiye dene → 400 ve gövdede 'kur' geçen mesaj; hiçbir satır yazılmamış olmalı (hareket sayısı değişmemeli)
- Kredi limiti 10.000 TL olan bir caride 300 USD borç varken TL veresiye dene → limit mesajında döviz karşılığının dahil edildiğini gör
- TEZGAHTAR kullanıcısıyla doviz-veresiye çağır → 403; doviz-tahsilat → 200
- Cariler ekranında döviz rozetini ve iki formu gerçek veriyle kullan; TL karşılığı alanının yazılamadığını doğrula
- API'yi durdur, test kiracısını sil (kur zamanlayıcısı 10 sn'de bir kur_snapshots yazar; ${=LISTE} ile zsh kelime bölme tuzağına dikkat), kiracı sayısının 5'e döndüğünü doğrula
- Kapılar: dotnet test 0 başarısız · dotnet/denetim.sh TEMİZ · dotnet/yetim-tara.sh çıkış 0

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Vade/VadeKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TestOrtami.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/CariEkranTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/CariDuzeltmeTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/HttpCariDuzeltmeTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DisaAktarimTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/VadeTakibiTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/DovizBakiye.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/YYYYMMDDHHMMSS_CariDovizBakiyesi.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/CariDovizBakiyeTestleri.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/HttpCariDovizTestleri.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DovizBakiyeSaflikTestleri.cs (YENI)`

## Mevzuat dayanağı
- Türk Parası Kıymetini Koruma Hakkında 32 Sayılı Karar md.4/g + Tebliğ 2008-32/34 md.8 (2025-32/72, RG 06.03.2025 ile değişik) — **DOGRULANDI** — Türkiye'de yerleşikler arasında MENKUL satış bedelinin döviz cinsinden kararlaştırılması serbesttir; altın bedelinin efektif dövizle TAHSİLİ bu kapıdan A12'de kodlandı (PAKET-15-DOVIZ-KARAR-2026-09-15.md). Paket 27'nin döviz tahsilat ucu bu doğrulanmış kapının aynısını kullanır.
- Aynı tebliğ — bedelin dövizle kararlaştırıldığı satışta ALACAĞIN döviz cinsinden AÇIK bırakılması (veresiye) — **DOGRULANMALI** — A12 araştırması 'bedelin dövizle kararlaştırılması ve tahsili'ni doğruladı; bedelin bir kısmının döviz cinsinden ALACAK olarak açık kalmasına dair birincil metin teyidi YOK. Bu yüzden döviz veresiye ucu SAHIP/MUDUR/MUHASEBE ile sınırlıdır ve çapraz kapatma kapalıdır (K27-G) — kapı yalnız sıkılaşır. Uygulayıcı bu hükmü koda gömmez; yalnız yetki kısıtı olarak uygular.
- 32 Sayılı Karar — yetki belgesiz döviz alım-satımı yasağı; Bakanlık 22.02.2026 açıklaması — **DOGRULANDI** — Döviz alış/satış ucu AÇILMAZ (PAKET-15 kararı). Çapraz kapatmanın Faz A'da yasak olmasının gerekçesi budur: USD borcunun TL ile kapatılması kur farkıyla birlikte fiilen döviz satışıdır.
- 459 sıra no.lu VUK Genel Tebliği — tevsik zorunluluğu, 30.000 TL nakit haddi — **DOGRULANDI** — Efektif döviz elden alınır, hiçbir finansal kurum kanalından geçmez → NAKİT sayılır (A12 kararı). TL karşılığı kümülatif nakit toplamına yazılır. Yabancı paranın hangi kurla çevrileceğine dair fıkra birincil metinden teyit EDİLEMEDİ; işlem anındaki kur kullanılır ve döviz daima nakit sayılır.
- MASAK — 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — Döviz işleminin TL karşılığı eşiğe sayılır; MasakKapisi.DogrulaKumulatif TL üzerinden çalışmaya devam eder. Eşik değeri ADR-0005 gereği yapılandırmadan okunur, koda gömülmez.
- VUK 280 — dönem sonu yabancı para değerlemesi — **VARSAYIM** — Kur değerlemesi ve kambiyo kâr/zararı kaydı bu üründe YAPILMAZ; mali müşavirin muhasebe programının işidir. Bakiye kolonlarına asla değerleme yazılmaz. Bu bir varsayımdır ve kurucuya/oylamaya açıktır.

## Riskler
- ÇİFTE SAYMA: döviz satırının TlTutari'si dolu olduğu için, TL toplayan YENİ bir sorgu yazan biri döviz borcunu TL borcu sanabilir. Karşı önlem: Tur ayrımı + VeresiyeTutar=0 değişmezi + M78/M79 mutasyonları + Entities.cs'te açık yorum. Mevcut kod tarandı; TlTutari'yi cari bakiyesi anlamında toplayan tek yer VadeKapisi.AcikBorclar'ın TAHSILAT dalıdır ve o Tur eşitliğiyle korunur.
- KUR SNAPSHOT TOCTOU (TB-06): döviz hareketi kur kaydına bağlıdır; kayıt silinirse TL karşılığı yeniden türetilemez. TB-04 (FK) kapanana kadar açık; bu paket riski artırır ama yaratmaz.
- SATIŞ/ALIŞ YOLUNDA BEKLENTİ: kullanıcılar 'fişte dövizle satış yaptım, cariye USD borç kalsın' isteyebilir. Faz A bunu vermez (K27-C). Ekranda açık mesaj gerekir, yoksa 'yapmıyor' şikâyeti gelir.
- MIGRATION ÇAKIŞMASI: Katman 0/4'teki diğer paketler aynı anda migration üretirse KuyumcuDbContextModelSnapshot.cs çakışır (tuzak: --no-build ile boş migration, remove --force ile dosya silinmesi). Yaz-seri zorunlu.
- EKRAN TESTİ KIRILMASI: yeni uçlar eklendiğinde mevcut bUnit sahte API'leri JsonException verir (5 CariEkran testi daha önce bundan kırıldı).
- VERİ YOKLUĞU: Faz A'da döviz bakiyesi için vade/yaşlandırma yok; müşteri 'geciken döviz alacağım görünmüyor' diyebilir. Faz B bunu kapatır, ama Faz A tek başına sevk edilirse ekranda açıkça yazılmalıdır.
- ARTIK DOSYALAR: depoda .mutorj uzantılı mutasyon yedekleri duruyor (ör. Modules/Satis/OdemeSepeti.cs.mutorj, Modules/Satis/SatisModulu.cs.mutorj, Kuyumcu.Web/Pages/*.razor.mutorj). Derlenmiyorlar ama grep tabanlı denetim sayaçlarını YANILTIYORLAR (aynı deseni iki kez sayıyorlar). denetim.sh'e yeni sayaç eklenirken *.mutorj elenmelidir; dosyaların silinmesi kurucuya bırakılır (hook rm'yi blokluyor).

## Açık sorular
- (yok)

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Kolon kararı (ayrı tablo değil) kanıta dayanıyor ve doğru: para birimi kümesi zaten üç yerde kapalı (KurSnapshot.UsdTry/EurTry/GbpTry, OdemeSepeti regex, Banka regex) ve cari_bakiyeler bugün TEK SATIR/TEK CARİ olarak okunuyor — satır tablosu her okuyucuya join ve 'satır yok' dalı ekler, bu depoda kanıtlı en büyük hata sınıfı olan çifte kayıt yüzeyini büyütür. K27-C (fişteki DOVIZ bacağı bir ödemedir, cariye borç bırakmaz) ayrımı isabetli. İşaret sözleşmesi ve 'toplama yasağı' has/TL çift defter felsefesiyle tutarlı.
  - Koşullar: K27-E (döviz satırında VeresiyeTutar=0 ve Tur asla TAHSILAT değil) bir DEĞİŞMEZDİR ve yalnız testle değil, HareketFabrikasi.Ekle içinde kapı olarak da zorlanmalı: Tur DOVIZ_* ise VeresiyeTutar>0 gelirse patlasın. Aksi hâlde sonraki paket bu kuralı bilmeden borcu iki kez sayar. · VadeKapisi.KrediLimitiDogrula imzasının değişmesi TÜM çağıranları etkiler (CariModulu, SatisModulu üzerinden Hazirla). Faz A'da derleyicinin zorladığı bu değişiklik ayrı bir adım olarak, kendi testleriyle sevk edilmeli. · Paket 20 ile TABAN ÇELİŞKİSİ çözülmeli: 20 altın dışı maden bakiyesini HESAPLANAN bırakıyor, 27 dövizi KOLON yapıyor. Aynı tabloda iki genişleme stratejisi kalıcılaşmadan, emanet maden bakiyesinin de ileride kolon mu hesaplanan mı olacağı ADR'ye bağlanmalı (bkz. çapraz bulgu 6). · Çapraz kapatmanın Faz A'da 400 ile reddedilmesi KORUNSUN (32 Sayılı Karar gerekçesi doğru); Faz B'ye de otomatik geçmesin, ayrı oylanma şartı yazılı kalsın. · *.mutorj artık dosyalarının denetim sayaçlarını yanılttığı iddiası DOĞRULANMADAN denetim.sh'a yeni sayaç eklenmesin; eklenecekse desenler bu uzantıyı elemeli.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Tüm mevzuat kapıları TL karşılığı üzerinden fail-closed çalışıyor; çapraz kapatma yasağı 32 Sayılı Karar riskini kapatıyor; defter tekeli korunuyor. Rol matrisi (döviz veresiye SAHIP/MUDUR/MUHASEBE, tahsilat TEZGAHTAR dahil) mevcut tahsilat paritesiyle tutarlı.
  - Koşullar: DovizBakiye.Gecerli TAM eşleşme (USD|EUR|GBP), ToUpperInvariant yok, TRY 400 — spec böyle, saf test M87 zorunlu. · VadeKapisi.KrediLimitiDogrula imza değişimi: çağıran TÜM yollar derleyiciyle zorlanır; kur yoksa BadRequest (sessiz atlama yok) — M81 zorunlu. · Döviz tahsilatı: KasaDogrula(beklenenParaBirimi=paraBirimi, an) + TevsikKapisi.Dogrula(TL karşılığı) + MasakKapisi.DogrulaKumulatif(TL karşılığı) ÜÇÜ de transaction öncesi; kasaya EFEKTİF yazılır — M82/M83/M84/M85 zorunlu. · Yeni denetim.sh sayaçları *.mutorj dosyalarını elesin (spec riskte yazıyor; sayaç eklenirken --exclude='*.mutorj' zorunlu). · Döviz düzeltme (duzeltme ucu) DenetimServisi.YazmaKaydet Hassas=true, Detay'da yalnız delta+para birimi+gerekçe — kişi verisi yok.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: EVET** — Mutasyon listesi (M76–M88) her biri adlandırılmış bir teste bağlı ve tek katman ölçüyor; K27-E değişmezi için iki ayrı mutasyon (M78/M79) doğru kurgulanmış; HTTP/DB/bUnit/saf dengesi tam; canlı doğrulama her adımda HTTP kodu + SQL kolonu istiyor ve kiracı silme adımı var. .mutorj artık dosyası uyarısı denetim sayaçları için değerli.
  - Koşullar: Öneri (bağlayıcı değil): DovizBakiyeSaflikTestleri 'ToUpperInvariant kullanılmaz' kaynak taraması yerine davranış testi olsun ('usd' → red). Test projesinde CultureInfo sabitlenmediği için (TestOrtami.cs/HttpOrtami.cs grep: 0) AwayFromZero/2 hane testi CultureInfo.InvariantCulture ile sayıyı biçimlemeli.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- K27-E değişmezi (döviz satırında VeresiyeTutar=0, Tur asla TAHSILAT değil) yalnız testle değil HareketFabrikasi.Ekle içinde KAPI olarak zorlanır: Tur DOVIZ_* iken VeresiyeTutar>0 gelirse patlar (M78/M79 korunur).
- DovizBakiye.Gecerli TAM eşleşme (USD|EUR|GBP), ToUpperInvariant YOK, TRY → 400; saflık testi M87 zorunlu. QA önerisi birleştirildi: kaynak taraması yerine davranış testi ('usd' → red) ve CultureInfo.InvariantCulture ile biçimlenen AwayFromZero/2 hane testi (test projesinde kültür sabitlenmemiş).
- VadeKapisi.KrediLimitiDogrula imza değişimi ayrı bir adım olarak, kendi testleriyle sevk edilir; tüm çağıranlar (CariModulu, SatisModulu→Hazirla) derleyiciyle zorlanır; kur yoksa BadRequest, sessiz atlama yok (M81).
- Döviz tahsilatı: KasaDogrula(beklenenParaBirimi=paraBirimi, an) + TevsikKapisi.Dogrula(TL karşılığı) + MasakKapisi.DogrulaKumulatif(TL karşılığı) ÜÇÜ de transaction öncesi; kasaya EFEKTİF yazılır (M82-M85 zorunlu).
- Çapraz kapatmanın Faz A'da 400 ile reddi KORUNUR (32 Sayılı Karar); Faz B'ye otomatik geçmez, ayrı oylama şartı yazılı kalır.
- Paket 20 ile taban çelişkisi çözülür: 20 altın dışı maden bakiyesini HESAPLANAN bırakıyor, 27 dövizi KOLON yapıyor — emanet maden bakiyesinin ileride kolon mu hesaplanan mı olacağı ADR'ye bağlanır; 20 ile aynı dalgada koşulmaz (Entities.cs/RaporModulu.cs ortak).
- Döviz düzeltme ucu DenetimServisi.YazmaKaydet Hassas=true; Detay'da yalnız delta + para birimi + gerekçe, kişisel veri yok.
- *.mutorj artık dosyalarının sayaçları yanılttığı iddiası DOĞRULANMADAN yeni denetim.sh sayacı eklenmez; eklenecekse desenler --exclude='*.mutorj' ile bu uzantıyı eler (dosyaların silinmesi kurucuya bırakılır).
- Faz A tek başına sevk edilirse 'döviz bakiyesinde vade/yaşlandırma yok' ve 'fişteki DOVIZ bacağı ödemedir, cariye borç bırakmaz (K27-C)' ekranda açıkça yazılır.
- Migration seri sevk edilir (KuyumcuDbContextModelSnapshot.cs çakışması); yeni uçlar için mevcut bUnit sahte API'lerine gerçekçi yanıt eklenir (5 CariEkran testi daha önce JsonException ile kırıldı).
