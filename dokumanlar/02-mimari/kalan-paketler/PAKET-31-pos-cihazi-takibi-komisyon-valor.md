# Paket 31 — POS cihazı takibi + komisyon + valör

> Katman: Katman 4 — Cari ve finans (bu görevde yalnız Paket 31: POS cihazı + komisyon + valör) · Tahmin: 4 gün (Faz A 2 gün: cihaz + tahsilat defteri + valör takvimi + ekranlar; Faz B 2 gün: eşleştirme + taksit oranları + mutabakat bloğu). PARITE tahmini 3–4 gün ile uyumlu; üst sınır seçildi çünkü satış yolunun 6 ayrı KART noktasına dokunuluyor ve mevcut kasa komisyon kusurunun düzeltilmesi de kapsamda. gün · Önkoşullar: yok · Kaynak: katman mimarı K4-31 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 4

## Hedef
Perakende kuyumcuda kart tahsilatı bugün kasaya tam tutarla giriyor; bankanın kestiği komisyon ve 1–30 günlük valör hiçbir yerde tutulmuyor. Bu paket her POS cihazını (banka, üye işyeri no, komisyon oranı, valör günü) tanımlı hale getirir, her kart tahsilatı için brüt/komisyon/net ve valör tarihi taşıyan bir POS alacak defteri yazar, 'bankadan ne zaman ne kadar yatacak' sorusunu yanıtlayan bir valör takvimi verir ve (Faz B) gelen banka hareketiyle tahsilatları eşleştirip kapatır. Kuyumcunun bugün elle Excel'de tuttuğu POS mutabakatı sistemin içine girer.

## Kodda bugünkü durum
Cihaz/valör/yatma eşleşmesi YOK. Var olan tek iz: KasaHareket.KomisyonOran (Entities.cs:441) ve KasaHareket.BlokajBitis (Entities.cs:442). BlokajBitis hiçbir yerde YAZILMIYOR — tek kullanımı dışa aktarım CSV'si (Modules/Disaaktarim/Kaynaklar.cs:214). KomisyonOran yalnız elle kasa hareketinde set ediliyor (KasaModulu.cs:66 DTO, :139 okuma, :159 yazma) ve orada kanıtlı kusurlu: bakiyeye net (:140), harekete brüt (:156) yazılıyor. Satış yolunda komisyon/valör HİÇ set edilmiyor: KasaGuncelle (SatisModulu.cs:1670-1693) KomisyonOran ve BlokajBitis alanlarına dokunmuyor; karma ödemede her KART bacağı brüt tutarla kasaya yazılıyor (SatisModulu.cs:1005-1008). Raporda pos-fatura-mutabakati var (RaporModulu.cs:156, uç :542) ama bu POS değil FATURA mutabakatıdır: kasa hareketlerini OdemeTuru'na göre gruplar (RaporModulu.cs:176-180), komisyon/valör/banka yatması kavramı yoktur. BankaHareket (Entities.cs:464-481) Durum alanı BEKLIYOR|ESLESTI|FATURALANDI taşır, cari eşleştirmesi elle yapılır (BankaModulu.cs:184-189, :214).

## Kapsam
**İçinde:**
- POS cihazı tanımı (banka, üye işyeri/terminal no, şube, bağlı kasa, bağlı banka hesabı, peşin komisyon oranı, valör günü)
- Her KART tahsilatı için pos_tahsilatlari satırı: brüt / komisyon oranı / komisyon tutarı / net / taksit adedi / işlem tarihi / valör tarihi / durum
- Valör takvimi ekranı ve ucu: hangi gün hangi bankadan ne kadar net yatacak (sunucuda toplanmış)
- Bekleyen POS alacağı toplamı (cihaz ve banka kırılımlı)
- KART iadesinde ters kayıt (chargeback / iade)
- Faz B: gelen banka hareketi ile POS tahsilatlarının elle ve önerili (oto) eşleştirilmesi, kısmi kapanış, eşleşme geri alma (gerekçeli)
- Faz B: taksit adedine göre komisyon oranı tablosu
- Faz B: pos-fatura-mutabakati raporuna POS bloğu (brüt tahsilat, beklenen net, yatan, fark)
- KasaModulu.HareketGir komisyon kusurunun düzeltilmesi (Karar E)

**Dışında:**
- Sanal POS / online tahsilat entegrasyonu (PARITE satır 157: banka veya ödeme kuruluşu sözleşmesi gerekir — C grubu)
- Banka API'siyle otomatik ekstre çekme (paket 32'nin konusu; bu paket yalnız var olan BankaHareket kayıtlarıyla eşleşir)
- Fiziksel POS cihazıyla haberleşme / slip okuma
- Kredi kartı taksitlendirmesinin müşteri cari bakiyesine yansıtılması (tahsilat peşindir; taksit yalnız bankanın komisyon ve valörünü etkiler)
- Resmî tatil takvimi (yalnız haftasonu atlama seçeneği sunulur)
- Kâr-zarar raporunda komisyon giderinin gösterimi (paket 35 bunun üstüne biner)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| pos_cihazlari | YENİ (PosCihaz : IKiraciVarlik) | id(36) PK · tenant_id(36) · sube_id(36) · ad(100) · banka_adi(100) · uye_isyeri_no(50, null) · terminal_no(50, null) · kasa_id(36) — kart bacağının yazıldığı kasa · banka_hesap_id(36, null) — yatmanın beklendiği hesap · para_birimi(5)='TRY' · pesin_komisyon_oran decimal(5,4) · valor_gun int · aktif bool · olusturuldu · guncellendi | (tenant_id) · (tenant_id, sube_id) · (tenant_id, ad) UNIQUE · (tenant_id, aktif) | 20260916HHMMSS_PosCihazVeTahsilat |
| pos_tahsilatlari | YENİ (PosTahsilat : IKiraciVarlik) | id(36) PK · tenant_id(36) · sube_id(36) · pos_cihaz_id(36) · hareket_id(36, null) · kasa_hareket_id(36, null) · kur_snapshot_id(36) · cari_id(36, null) · brut_tutar decimal(15,2) · komisyon_oran decimal(5,4) · komisyon_tutar decimal(15,2) · net_tutar decimal(15,2) · yatan_tutar decimal(15,2) default 0 · taksit_adedi int default 1 · para_birimi(5) · islem_tarihi datetime(UTC) · valor_tarihi datetime(UTC) · durum(15) BEKLIYOR|KISMI|YATTI|IPTAL · iptal_gerekcesi text null · olusturuldu | (tenant_id, valor_tarihi) · (tenant_id, durum, valor_tarihi) · (tenant_id, pos_cihaz_id, islem_tarihi) · (tenant_id, hareket_id) · (tenant_id, kasa_hareket_id) | 20260916HHMMSS_PosCihazVeTahsilat |
| pos_taksit_oranlari | YENİ — Faz B (PosTaksitOran : IKiraciVarlik) | id(36) PK · tenant_id(36) · pos_cihaz_id(36) · taksit_adedi int (2..12) · komisyon_oran decimal(5,4) · valor_gun int null (null=cihaz varsayılanı) · gecerli_baslangic datetime(UTC) · aktif bool · olusturuldu | (tenant_id, pos_cihaz_id, taksit_adedi, gecerli_baslangic) · (tenant_id, pos_cihaz_id, aktif) | 20260917HHMMSS_PosEslestirmeVeTaksit |
| pos_yatma_eslesmeleri | YENİ — Faz B (PosYatmaEslesme : IKiraciVarlik) | id(36) PK · tenant_id(36) · pos_tahsilat_id(36) · banka_hareket_id(36) · eslesen_tutar decimal(15,2) · otomatik bool · eslestiren_kullanici_id(36) · geri_alindi bool default false · geri_alma_gerekcesi text null · olusturuldu | (tenant_id, pos_tahsilat_id) · (tenant_id, banka_hareket_id) · (tenant_id, pos_tahsilat_id, banka_hareket_id) UNIQUE | 20260917HHMMSS_PosEslestirmeVeTaksit |
| kasa_hareketleri | DEĞİŞİKLİK YOK (şema). BlokajBitis (Entities.cs:442) mevcut hâliyle bırakılır; POS valörü pos_tahsilatlari.valor_tarihi'nde tutulur. Gerekçe: BlokajBitis yazılmayan ölü alandır ve tek tahsilat–tek kasa hareketi varsayımı Faz B kısmi kapanışını taşıyamaz. | - | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/pos/cihazlar` | SAHIP, MUDUR | POS cihazı tanımla. sube_id IslemDogrulayici.SubeDogrula ile, kasa_id kiracı+para birimi eşleşmesiyle, banka_hesap_id varsa kiracıyla doğrulanır. Aynı adda aktif cihaz varsa Conflict. | ModulYetkisi("kasa") |
| GET | `/api/v1/pos/cihazlar?subeId=&aktif=` | tümü (modül yetkisi olan) | Cihaz listesi; her satırda bekleyen alacak toplamı (sunucuda toplanmış). | ModulYetkisi("kasa") |
| GET | `/api/v1/pos/cihazlar/{id}` | tümü | Tek cihaz + (Faz B) taksit oran tablosu. | ModulYetkisi("kasa") |
| PATCH | `/api/v1/pos/cihazlar/{id}` | SAHIP, MUDUR | Ad, komisyon oranı, valör günü, banka hesabı güncelle. Oran/valör değişikliği GEÇMİŞE İŞLEMEZ — yazılmış pos_tahsilatlari satırları değişmez. | ModulYetkisi("kasa") |
| POST | `/api/v1/pos/cihazlar/{id}/pasife-al` | SAHIP, MUDUR | Cihazı kapatır. BEKLIYOR/KISMI tahsilatı varsa BadRequest (fail-closed: açık alacağı olan cihaz kapatılamaz). | ModulYetkisi("kasa") |
| GET | `/api/v1/pos/tahsilatlar?durum=&bas=&bit=&cihazId=&limit=&offset=` | tümü | POS alacak defteri, sayfalı. bas/bit Zaman.YerelGunAraligiUtc ile UTC'ye çevrilir (K5). | ModulYetkisi("kasa") |
| GET | `/api/v1/pos/valor-takvimi?bas=&bit=` | tümü | Valör gününe ve cihaza göre GroupBy + Sum, veritabanında toplanır (TB-19: satır listesi dönmez). Yanıt: [{valorTarihi, cihazId, bankaAdi, brut, komisyon, net, yatan, kalan}]. | ModulYetkisi("kasa") |
| POST | `/api/v1/pos/tahsilatlar/{id}/iptal` | SAHIP, MUDUR | Chargeback/iptal. Gerekçe zorunlu (>=20 karakter). Eşleşmiş tahsilat iptal edilemez (önce eşleşme geri alınır). | ModulYetkisi("kasa") |
| POST | `/api/v1/pos/eslestir` | SAHIP, MUDUR, MUHASEBE | Faz B. Gövde {bankaHareketId, satirlar:[{posTahsilatId, tutar}]}. Tek transaction; banka hareketi GELEN olmalı, toplam eşleşen <= banka hareketi tutarı ve satır bazında <= (net_tutar - yatan_tutar). Banka hareketi ESLESTI'ye geçer. | ModulYetkisi("kasa") |
| POST | `/api/v1/pos/oto-eslestir-onerisi` | SAHIP, MUDUR, MUHASEBE | Faz B. Gövde {bankaHareketId}. Aynı cihazın aynı valör gününe düşen BEKLIYOR tahsilatlarından tutar toplamı hareket tutarına eşitlenen kümeyi ÖNERİR; hiçbir şey yazmaz. Tek aday yoksa boş döner (varsayım yok). | ModulYetkisi("kasa") |
| POST | `/api/v1/pos/eslesmeler/{id}/geri-al` | SAHIP, MUDUR | Faz B. Satır silinmez, geri_alindi=true + gerekçe (append-only, ADR-0004 ruhu). Özet (yatan_tutar, durum) yeniden hesaplanır. | ModulYetkisi("kasa") |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` — Modül deseni (KasaModulu.cs:13-21 örnek modül başlığı): DTO'lar + PosServisi + PosController tek dosyada. DTO'lar: PosCihazOlusturDto, PosCihazGuncelleDto, PosTahsilatIptalDto, PosEslestirDto, PosTaksitOranDto. Servis metotları: CihazOlustur, CihazListele, CihazGetir, CihazGuncelle, CihazPasifeAl, TahsilatListele, ValorTakvimi, TahsilatIptal, Eslestir, OtoEslestirOnerisi, EslesmeGeriAl. DataAnnotations yalnız controller yolunda çalışır — servis içinde elle kontrol tekrarlanır.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Pos/PosDefteri.cs` — POS tahsilat satırı yazan TEK yol. `PosDefteri.Ekle(db, ayarlar, cihaz, brut, taksit, hareketId, kasaHareketId, kurSnapshotId, cariId, now)` — komisyonu ve valör tarihini hesaplar, satırı db.PosTahsilatlari'na ekler (SaveChanges çağırmaz; çağıran transaction'ın içindedir). Ayrıca `Ozetle(db, tenantId, tahsilatId)` eşleşme toplamından yatan_tutar+durum türetir (Karar C). HareketFabrikasi.cs deseni: statik, db'yi parametre alır.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Pos/PosAyarlari.cs` — IOptions sınıfı: CihazZorunlu(false), ValorIsGunu(false), IadeKomisyonIadeEdilir(false), AzamiValorGun(90), OtoEslestirToleransKurus(0). ADR-0005: sayı koda gömülmez.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — PosCihaz, PosTahsilat, PosTaksitOran, PosYatmaEslesme sınıfları eklenir; hepsi IKiraciVarlik, hepsinde tenant_id. Kasa/KasaHareket (satır 419-445) DEĞİŞMEZ.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — 4 DbSet (satır 66-75 bloğunun yanına) + global kiracı HasQueryFilter (mevcut IKiraciVarlik mekanizması) + veri_modeli'ndeki indeksler (satır 118-194 bloğuna).
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — (a) OdemeBacakDto'ya `PosCihazId` (string?) eklenir; tek ödemeli SatisOlusturDto'ya da. (b) Karma yolda SatisModulu.cs:1005-1008 foreach'inde Arac=="KART" olan bacak için CihazCoz + PosDefteri.Ekle. (c) Tek ödemeli yolda SatisModulu.cs:522 KasaGuncelle çağrısından sonra aynısı. (d) SatisIade (:1124) ve AlisIade (:1234) KART ise ters kayıt. (e) KasaGuncelle imzası kasa hareketinin Id'sini döndürecek şekilde genişletilir ki pos_tahsilatlari.kasa_hareket_id dolsun. (f) HİÇBİR kasa bakiyesi hesabı değişmez.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — HareketGir (satır 134-167) komisyon kusuru (Karar E): brüt tutarlı giriş satırı + komisyon>0 ise ayrı CIKIS satırı; kasa.Bakiye += brüt - komisyonTutar. Hareket satırları toplamı = bakiye deltası. Gün kilidi çağrısı (:145) korunur.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — Faz B: PosFaturaMutabakati (satır 156) yanıtına `pos` bloğu — o günün brüt kart tahsilatı, beklenen net, valörü gelmiş ama yatmamış tutar, uyarı: 'N POS tahsilatının valörü geçti, banka yatması görünmüyor'. Mevcut tahsilat kırılımı (:176-182) DEĞİŞMEZ.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` — Faz B: HareketListele (satır 227) filtresine `posEslesmemis=true` seçeneği (GELEN + durum BEKLIYOR). HareketEkle (:180) davranışı değişmez.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — `s.AddScoped<Pos.PosServisi>();` (satır 18 Kasa.KasaServisi'nin yanına).
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Program.cs` — `builder.Services.Configure<PosAyarlari>(builder.Configuration.GetSection("Pos"));` (Masak/Tevsik bağlamalarının yanına).
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/appsettings.json + appsettings.Development.json` — "Pos" bölümü: _not (bu eşikler mevzuat değil, banka üye işyeri sözleşmesidir), CihazZorunlu:false, ValorIsGunu:false, IadeKomisyonIadeEdilir:false, AzamiValorGun:90.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs` — ADR-0014 dışa aktarım kapsamına pos_cihazlari + pos_tahsilatlari + pos_yatma_eslesmeleri CSV kaynakları (satır 214 KasaHareket deseni). Kişisel veri yok; maskeleme gerekmez.
- **DEĞİŞTİR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukPos` sınıfı (SozlukKasa deseni, satır 439) + `Sozluk.Pos` özelliği (satır 1046) + `SozlukNav`a (satır 50) Pos/PosTahsilat girdileri. TR ve EN sözlüklerin İKİSİ de doldurulur.
- **YENİ** `dotnet/Kuyumcu.Web/Servisler/PosServisi.cs` — İstemci API sarmalayıcısı (Servisler/UyumServisi.cs deseni, satır 100 çağrı biçimi).
- **DEĞİŞTİR** `dotnet/denetim.sh` — Yeni denetim: `PosTahsilatlari\.Add\(` yalnız 1 (PosDefteri.cs içinde) — kasa/hareket fabrikası deseninin aynısı. Mevcut sayaçlar (IgnoreQueryFilters 0, Find 0, Hareketler.Add 1) DEĞİŞMEZ; ExecuteUpdate/Delete beyaz listesi 4'te kalır.

## Ekranlar
- `/pos` · `dotnet/Kuyumcu.Web/Pages/Pos.razor` — POS cihazları: StatKart'larda toplam bekleyen alacak / bugün yatacak net / cihaz sayısı. Tablo: cihaz, banka, şube, komisyon %, valör günü, bekleyen alacak, aksiyonlar. ModalPencere ile ekle/düzenle/pasife al (Kasalar.razor:89-190 deseni). Faz B'de taksit oran tablosu bu ekranda düzenlenir. (menü/yetki: KenarCubugu.razor:34 (/kasalar) hemen altına: <NavLink href="/pos">💳 @Dil.T.Nav.Pos</NavLink>; kasa modülü yetkisi olanlara görünür)
- `/pos/tahsilatlar` · `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` — Valör takvimi (gün × cihaz matrisi, net beklenen) + POS alacak defteri listesi (durum/cihaz/tarih filtreli, sayfalı) + iptal modalı. Faz B: gelen banka hareketi seçilip eşleştirme modalı (öneri butonu, satır satır tutar girişi, toplam göstergesi). (menü/yetki: KenarCubugu.razor'da /pos'un alt bağlantısı; aynı yetki)
- `/kasalar` · `dotnet/Kuyumcu.Web/Pages/Kasalar.razor` — Faz A: hareket modalında (satır 130-156) komisyon oranı alanına 'komisyon ayrı gider satırı olarak yazılır' açıklaması; girilen orandan hesaplanan net tutarın canlı gösterimi. Razor özniteliğinde { } lambda YASAK — adlandırılmış metot (§6 tuzak). (menü/yetki: mevcut)
- `/uyum` · `dotnet/Kuyumcu.Web/Pages/Uyum.razor` — Faz B: mutabakat kartına POS bloğu (brüt kart / beklenen net / yatan / valörü geçmiş bekleyen). (menü/yetki: mevcut)

## İş kuralları (fail-closed)
- Cihaz çözümü FAIL-CLOSED: kart bacağında PosCihazId verilmemişse, o şubede tam bir aktif cihaz varsa o seçilir; birden çok varsa BadRequest ('POS cihazı belirtilmeli'); hiç yoksa Pos.CihazZorunlu=true ise BadRequest, false ise POS satırı yazılmaz (bugünkü davranış aynen sürer).
- Cihazın kasa_id'si ile bacağın KasaId'si AYNI olmak zorundadır; değilse BadRequest. Cihazın para_birimi ile kasanın para_birimi eşleşmeli (K6 kasa para birimi kapısının POS karşılığı).
- komisyon_tutar = Math.Round(brut * oran, 2, MidpointRounding.AwayFromZero); net_tutar = brut - komisyon_tutar. Bankacı yuvarlaması KULLANILMAZ (SatisModulu Yuvarla2 deseni).
- valor_tarihi = Zaman.YerelGunBasiUtc(yerel işlem günü + valor_gun). Pos.ValorIsGunu=true ise Cumartesi/Pazar atlanır; resmî tatil takvimi YOKTUR ve uydurulmaz.
- Oran ve valör günü tahsilat satırına DONDURULUR (snapshot). Cihaz ayarı sonradan değişse de geçmiş satırlar değişmez — kur_snapshot mantığının aynısı.
- POS tahsilatı kasa bakiyesini ETKİLEMEZ; kasa yazımı yalnız mevcut KasaGuncelle yolundadır. pos_tahsilatlari bir yan defterdir.
- Eşleşme toplamı (geri_alindi=false olanlar) hiçbir zaman net_tutar'ı aşamaz; bir banka hareketine eşleşen toplam da o hareketin tutarını aşamaz. İhlal → BadRequest, transaction geri alınır.
- durum türetilir: yatan=0 → BEKLIYOR; 0<yatan<net → KISMI; yatan>=net → YATTI. Elle atanamaz.
- İptal (chargeback) gerekçesi >=20 karakter (GunAcmaDto deseni, KasaModulu.cs:52). Eşleşmiş tahsilat iptal edilemez.
- Eşleşme geri alma SİLMEZ; geri_alindi bayrağı + gerekçe (ADR-0004 append-only).
- İadede: brut_tutar negatif, komisyon Pos.IadeKomisyonIadeEdilir=false ise 0 (banka komisyonu iade etmez varsayılanı), true ise oransal negatif.
- Tevsik kapısı kart bacağına UYGULANMAZ — bugünkü kod da yalnız NakitToplam için çağırıyor (SatisModulu.cs:915-917). Bu paket o davranışı değiştirmez.
- MASAK kümülatifi işlem toplamı üzerindendir, ödeme aracından bağımsızdır; POS bu kapıyı DEĞİŞTİRMEZ.
- Tüm POS sorguları tenant_id şartını ELLE de taşır (ADR-0010: 98 elle şart korunuyor); global filtre tek başına dayanak sayılmaz.
- IgnoreQueryFilters / Find / FindAsync kullanılmaz; PK erişimi FirstOrDefaultAsync + TenantId ile.

## Testler
- `dotnet/Kuyumcu.Tests/PosCihazTestleri.cs` [DB]
  - Cihaz_Olusturulur_VeListelenir
  - Cihaz_BaskaKiracininSubesiyle_Reddedilir
  - Cihaz_KasaParaBirimiUyusmazsa_Reddedilir
  - Cihaz_AyniAd_Conflict
  - Cihaz_AcikAlacakVarken_PasifeAlinamaz
  - Cihaz_OranDegisikligi_GecmisTahsilatiDegistirmez
- `dotnet/Kuyumcu.Tests/PosTahsilatTestleri.cs` [DB]
  - KartSatisi_PosTahsilatiYazar_BrutKomisyonNet
  - KartSatisi_ValorTarihiHesaplanir_TakvimGunu
  - ValorIsGunu_Acikken_HaftasonuAtlanir
  - KartBacagi_BirdenCokAktifCihaz_Reddedilir
  - KartBacagi_CihazYok_CihazZorunluKapali_TahsilatYazilmaz
  - KartBacagi_CihazYok_CihazZorunluAcik_Reddedilir
  - KasaBakiyesi_PosTahsilatindanEtkilenmez
  - KarmaOdeme_YalnizKartBacagi_PosaYazilir
  - KartIadesi_TersKayitYazar_KomisyonIadeEdilmez
  - Tahsilat_Iptal_GerekcesizReddedilir
  - ValorTakvimi_SunucudaToplanir_SatirDonmez
  - PosTahsilati_BaskaKiraciyaSizmaz
- `dotnet/Kuyumcu.Tests/PosEslestirmeTestleri.cs` [DB]
  - BankaYatmasi_UcTahsilatiKapatir_DurumYATTI
  - KismiEslesme_DurumKISMI
  - Eslesme_NetTutariAsamaz
  - Eslesme_BankaHareketiTutariniAsamaz
  - Eslesme_GidenHareketle_Reddedilir
  - EslesmeGeriAl_SatirSilmez_OzetiDuzeltir
  - OtoOneri_TekAdayYoksa_BosDoner
  - TaksitOrani_PesinOranaGoreUstunGelir
- `dotnet/Kuyumcu.Tests/PosEkranTestleri.cs` [bUnit]
  - PosEkrani_CihazListesiniGosterir
  - PosEkrani_YeniCihazModali_ZorunluAlanlar
  - TahsilatEkrani_ValorTakviminiGosterir
  - TahsilatEkrani_EslestirmeModali_ToplamAsiminda_ButonKapali
- `dotnet/Kuyumcu.Tests/KasaKomisyonTestleri.cs` [DB]
  - ElleHareket_KomisyonluGiris_IkiSatirYazar
  - ElleHareket_SatirlarToplami_BakiyeDeltasinaEsit
- `dotnet/Kuyumcu.Tests/HttpPosTestleri.cs` [HTTP]
  - PosUclari_YetkisizRolde_403
  - PosUclari_KasaModuluKapaliKiracida_403
  - CihazOlustur_MUHASEBE_403

## Mutasyon kanıtları
- PosDefteri komisyon hesabını `komisyon_tutar = 0` yap → PosTahsilatTestleri.KartSatisi_PosTahsilatiYazar_BrutKomisyonNet KIRMIZI
- valor_gun toplamasını kaldır (valor_tarihi = islem_tarihi) → KartSatisi_ValorTarihiHesaplanir_TakvimGunu KIRMIZI
- Cihaz belirsizliğinde ilk cihazı seç (BadRequest yerine) → KartBacagi_BirdenCokAktifCihaz_Reddedilir KIRMIZI
- Eşleşme aşım kontrolünü kaldır → Eslesme_NetTutariAsamaz ve Eslesme_BankaHareketiTutariniAsamaz KIRMIZI
- durum'u eşleşmeden türetmek yerine sabit YATTI yaz → KismiEslesme_DurumKISMI KIRMIZI
- PosServisi sorgusundan `TenantId ==` şartını kaldır → PosTahsilati_BaskaKiraciyaSizmaz ve Cihaz_BaskaKiracininSubesiyle_Reddedilir KIRMIZI
- Cihaz–kasa para birimi kontrolünü kaldır → Cihaz_KasaParaBirimiUyusmazsa_Reddedilir KIRMIZI
- KasaModulu düzeltmesini geri al (net bakiye + brüt satır) → ElleHareket_SatirlarToplami_BakiyeDeltasinaEsit KIRMIZI
- ValorTakvimi'nde ToListAsync + bellekte GroupBy'a dön → ValorTakvimi_SunucudaToplanir_SatirDonmez KIRMIZI

## Canlı doğrulama
- API'yi `--urls http://localhost:5199` ile başlat, giriş yap (port 5000 macOS ControlCenter'da).
- Test kiracısı aç; `/kur/guncel` id gelene kadar bekle (yeni kiracıda kur 10 sn sonra oluşur, yoksa her istek 400).
- POST /pos/cihazlar ile 'Garanti POS' (oran 0.0185, valör 1) tanımla; yanıttaki `id` camelCase okunur, boşsa DUR (SPA fallback tuzağı).
- Aynı şubede ikinci cihaz tanımla; kart satışını PosCihazId'siz dene → 400 'POS cihazı belirtilmeli' beklenir.
- PosCihazId ile 10.000 TL kart satışı yap; GET /pos/tahsilatlar ile brüt 10000 / komisyon 185,00 / net 9815,00 / valör = bugün+1 doğrula.
- Kasa bakiyesini GET /kasalar ile karşılaştır: satıştan ÖNCEki bakiye + 10.000 olmalı (POS defteri bakiyeyi etkilemez).
- GET /pos/valor-takvimi?bas=..&bit=.. → ilgili günde net 9815,00.
- Faz B: /bankalar/{id}/hareket ile 9.815 TL GELEN hareket gir; POST /pos/eslestir → tahsilat YATTI, banka hareketi ESLESTI.
- Kısmi: 5.000 TL eşleştir → KISMI; geri al → tekrar BEKLIYOR, eşleşme satırı SİLİNMEMİŞ (geri_alindi=true).
- GET /raporlar/pos-fatura-mutabakati?tarih=bugün → pos bloğu doğru.
- curl'de `-o /dev/null` KULLANMA, yanıtı oku. JSON alanı çekerken perl -CSD (zsh + sed [0-9.] Türkçe JSON'da patlar).
- API'yi durdur, test kiracısını sil (kur zamanlayıcısı yazmaya devam eder). kuyumcu_net 5 kiracıya dönmeli. Tablo silme SQL'inde `${=LISTE}` kullan.

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs`
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor`
- `dotnet/Kuyumcu.Web/Pages/Uyum.razor`
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`
- `dotnet/Kuyumcu.Tests/TestOrtami.cs`
- `dotnet/Kuyumcu.Tests/SatisFisiTestleri.cs`
- `dotnet/Kuyumcu.Tests/MutabakatTestleri.cs`
- `dotnet/Kuyumcu.Tests/GunKapanisiTestleri.cs`
- `dotnet/Kuyumcu.Tests/IadeTestleri.cs`
- `dotnet/Kuyumcu.Tests/KarmaOdemeEkranTestleri.cs`
- `dotnet/denetim.sh`
- `dokumanlar/02-mimari/mimari-tanimi.md`
- `dokumanlar/02-mimari/adr/README.md`
- `dokumanlar/08-degerlendirme/teknik-borc-envanteri.md`
- `YENİ: dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs`
- `YENİ: dotnet/Kuyumcu.Api/Modules/Pos/PosDefteri.cs`
- `YENİ: dotnet/Kuyumcu.Api/Modules/Pos/PosAyarlari.cs`
- `YENİ: dotnet/Kuyumcu.Web/Pages/Pos.razor`
- `YENİ: dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor`
- `YENİ: dotnet/Kuyumcu.Web/Servisler/PosServisi.cs`
- `YENİ: dotnet/Kuyumcu.Api/Data/Migrations/20260916HHMMSS_PosCihazVeTahsilat.cs (+Designer)`
- `YENİ: dotnet/Kuyumcu.Api/Data/Migrations/20260917HHMMSS_PosEslestirmeVeTaksit.cs (+Designer)`
- `YENİ: dokumanlar/02-mimari/adr/0016-pos-alacak-defteri.md`
- `YENİ: 6 test dosyası (testler bölümünde listeli)`

## Mevzuat dayanağı
- Banka üye işyeri sözleşmesi (POS komisyon oranı ve valör günü) — **VARSAYIM** — Komisyon oranı ve valör süresi MEVZUAT DEĞİLDİR; bankayla yapılan ticari sözleşmeden gelir ve kuyumcudan kuyumcuya değişir. Bu yüzden koda hiçbir sayı gömülmez; oran ve valör günü cihaz tanımında kullanıcıdan alınır.
- 459 sıra no.lu VUK Genel Tebliği (tevsik zorunluluğu), 575 sıra no.lu Tebliğ ile had 30.000 TL — RG 30.11.2024/32738 — **DOGRULANDI** — A7 araştırmasında doğrulanmış. Kart/POS zaten tevsik edici ödeme aracıdır; tevsik kapısı yalnız nakit bacağa uygulanır. Kod bugün de böyle: SatisModulu.cs:915-917 yalnız sepet.NakitToplam için TevsikKapisi.Dogrula çağırıyor. Bu paket davranışı değiştirmez.
- MASAK 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — A7'de doğrulanmış; appsettings 'Masak'ta yapılandırılı (KimlikEsigi 185000). Eşik işlem TOPLAMI üzerindendir, ödeme aracından bağımsızdır — POS bu kapıyı değiştirmez, dokunulmaz.
- 5464 sayılı Banka Kartları ve Kredi Kartları Kanunu — POS'tan nakit kullandırma yasağı — **DOGRULANMALI** — Kuyumcu sektöründe 'kartla çekip nakit verme' yaygın bir ihlal iddiasıdır; hükmün tam maddesi ve idari yaptırımı bu oturumda DOĞRULANMADI. Koda hiçbir kural, ekrana hiçbir uyarı metni KONMAZ. Doğrulanırsa ayrı bir uyum paketine (Katman 6) girer.
- Döviz alım-satımı yetkisiz kuyumcuya yasak (PAKET-15 kararı) — **DOGRULANDI** — POS cihazı yalnız TRY için tanımlanır (para_birimi varsayılan TRY). Döviz POS'u ucu AÇILMAZ; cihaz para birimi TRY dışına ayarlanmak istenirse reddedilir.

## Riskler
- EN BÜYÜK RİSK — kasa semantiği: kart tahsilatı bugün kasa bakiyesine giriyor (SatisModulu.cs:1005-1008). Gün kapanışı ise sayılan NAKİT ile kasa bakiyesini karşılaştırıyor (KasaModulu.cs:191). Kart satışı olan bir günde kuyumcu aynı kasayı kullanıyorsa kaçınılmaz sayım farkı çıkar. Bu paket bu davranışı DEĞİŞTİRMİYOR (Karar A) ama kusur gerçekse paket 30'u da ilgilendirir. Doğrulanması ve ayrı oylanması gerekir.
- SatisModulu.cs 1772 satırlık ve 6 ayrı yerde KasaGuncelle çağırıyor; KART işleme noktalarından birinin atlanması POS defterini sessizce eksik bırakır. Karşı önlem: PosDefteri.Ekle çağrısını KasaGuncelle'nin İÇİNE koymak yerine çağrı noktalarına koymak zorunlu (kasa hareketi Id'si gerekiyor) — bu yüzden 'KART bacağı var ama PosTahsilat yok' durumunu yakalayan bir denetim testi şart (Pos.CihazZorunlu=true kiracısında).
- Geriye dönük veri: mevcut kart kasa hareketleri için POS tahsilatı üretilmez. Kuyumcu 'geçmiş POS alacağım nerede' diye sorar. Karşı önlem: açılış bakiyesi mantığında elle POS tahsilatı girişi (ADR-0014 içe aktarım yasağıyla çelişmez, tek tek elle girilir) — kapsam dışı bırakıldı, açık soru.
- bUnit sahte API deseni: yeni /pos uçları eklenince mevcut ekran testlerinin sahte API'leri `_ => "{}"` döndürüp JsonException atabilir (§6 tuzak: 5 CariEkran testi bundan kırıldı). Pos.razor/PosTahsilatlar.razor çağrılarına gerçekçi sahte yanıt eklenmeli.
- Migration riski: `dotnet ef migrations add --no-build` derleme kırıkken BOŞ migration üretir ve `remove --force` başka migration siler (§6, VadeTakibi dosyaları bu şekilde silinmişti). Önce `dotnet build` yeşil.
- decimal(5,4) komisyon oranı en fazla 9,9999 tutar — oran 0..1 aralığında olduğu için yeterli, ama %100'ü aşan bir giriş sessizce kırpılmaz, Range(0,1) ile reddedilir. DTO Range'i servis içinde de tekrarlanmalı (DataAnnotations yalnız controller yolunda çalışır).
- Kültüre bağlı biçimleme: valör takvimi ve CSV'de sayılar CultureInfo.InvariantCulture ile yazılmalı (§6: tr-TR'de '1,6' üretip SVG viewBox'ı bozmuştu).

## Açık sorular
- Kart tahsilatı kasaya yazılmaya devam etmeli mi? Bugünkü tasarım 'her ödeme aracı kendi kasası' varsayıyor olabilir (her bacak kendi KasaId'sini taşıyor, SatisModulu.cs:905-908) ama bu hiçbir belgede doğrulanmadı. Alternatif: Kasa'ya tur(NAKIT|POS|BANKA) kolonu ekleyip gün kapanışını yalnız NAKIT kasalara uygulamak. Karar oylamaya ait — bu paket mevcut davranışı korudu.
- Valör iş günü mü takvim günü mü? Bankalar iş günü sayar ama resmî tatil takvimi sistemde YOK. Pos.ValorIsGunu yalnız haftasonunu atlayabilir; bayramlar yanlış hesaplanır. Resmî tatil tablosu ayrı bir iş kalemi mi?
- Chargeback (kart itirazı) muhasebesi: iptal edilen tahsilat cari bakiyeyi ve satış hareketini etkilemeli mi, yoksa yalnız POS defterinde mi kalmalı? Bu bir iade değil banka itirazıdır; paket 33 manuel düzeltme fişiyle mi bağlanmalı?
- Geçmiş (paket 31 öncesi) kart kasa hareketleri için açılış POS alacağı girişi verilecek mi? ADR-0014 içe aktarım yasağıyla çelişmeyen tek yol elle giriş.
- PosTahsilat.kur_snapshot_id zorunlu tutuldu (mimari değişmez gereği) ama satır her zaman TRY'dir; gereksiz olabilir. Bertan onayı isteniyor.
- Taksitli satışta müşteriye tek çekim gibi mi davranılacak (tahsilat peşin, banka valörü uzun) — kuyumcu tarafında taksitin cari etkisi yok varsayımı doğrulanmalı.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — POS'un 'alacak defteri' olarak modellenmesi ve üç tablo (cihaz / tahsilat / N:M yatma eşleşmesi) kararı doğru: bir banka yatması N tahsilatı kapatır, kasa_hareketleri'ne kolon eklemek bunu modelleyemez. yatan_tutar+durum'un türetilmiş özet olduğunun açıkça yazılması ve tek servis metodundan güncellenmesi çift gerçek riskini kapatıyor. Komisyonun kasadan düşülmemesi (brüt kasa, append-only defterden türetilebilirlik) doğru karar.
  - Koşullar: KARAR E (KasaModulu.HareketGir'in komisyon kusurunun düzeltilmesi) BU PAKETTEN ÇIKARILSIN ve ayrı, tek konulu bir düzeltme paketi olarak oylansın. Gerekçe: mevcut ve canlıda kullanılan bir bakiye hesabını değiştiriyor, gün kapanışı (paket 30) ile kesişiyor ve paket 31'in kendi konusuyla (POS alacak defteri) hiçbir zorunlu bağı yok. 'İzinsiz değişiklik yok' kuralı burada bağlayıcıdır. · Kasa semantiği açık sorusu (kart tahsilatının nakit kasasına girmesi ve gün kapanışı sayımıyla kaçınılmaz fark üretmesi) paket 30'u ilgilendirir ve DOĞRULANMADAN 'mevcut davranış korunur' denerek geçilemez: sevk öncesi 'kart satışı olan bir günde gün kapanışı farkı' senaryosu canlıda ölçülüp raporlanmalı (Madde 1). · PosTahsilat.kur_snapshot_id ZORUNLU KALSIN (Bertan onayı istenmişti): satır bugün TRY olsa da mimari değişmez 'para hareketi doğuran her satır kur_snapshot_id taşır' istisnasızdır; istisna açmak bir sonraki pakette kuralı tartışmaya açar. · PosDefteri.Ekle çağrısı KasaGuncelle'nin dışında 6 ayrı noktaya konuyor — 'KART bacağı var ama PosTahsilat yok' durumunu yakalayan bir DB testi (CihazZorunlu=true kiracıda) Faz A'nın kabul ölçütü olmalı; spec bunu risk diye yazmış, koşula çevriliyor. · Faz B (eşleştirme + taksit + mutabakat) ayrı sevk; Faz A tek başına sevk edilebilir kalmalı.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Yan defter yaklaşımı kasa/defter değişmezlerini korur; kişisel veri yok; mevzuat kapılarına dokunmuyor. Tek pürüz mevcut kod değişikliği ve kiracı doğrulama sayaçları.
  - Koşullar: KARAR E (KasaModulu.HareketGir düzeltmesi) MEVCUT KODU DEĞİŞTİRİR — Anayasa 'izinsiz değişiklik yok': ayrı oy maddesi olarak Tamer Bey mühürlemeden uygulanmaz. Güvenlik açısından düzeltme DOĞRU yöndedir (bakiye defterden türetilebilir olmalı), oyum EVET. · PosServisi tüm sorgularda elle TenantId (spec böyle); PosCihazId gövdeden gelince cihaz kiracı+şube doğrulaması, yabancı cihaz 404; mutasyon 'TenantId şartı kaldırılınca sızma testi kırmızı' zorunlu. · Eşleşme geri alma SİLMEZ (geri_alindi + gerekçe ≥20) — ADR-0004 ruhu; chargeback/iptal gerekçesi servis içinde elle kontrol. · Taksit oran tablosu ve cihaz PATCH SAHIP/MUDUR; MUHASEBE eşleştirme yapabilir ama cihaz oluşturamaz (HttpPosTestleri CihazOlustur_MUHASEBE_403 zorunlu). · POS uçlarında ExecuteUpdate yok; PosTahsilatlari.Add=1 denetim sayacı *.mutorj hariç. · Tevsik kart bacağına uygulanmaz (mevcut davranış) — DEĞİŞTİRİLMEZ; MASAK işlem toplamı üzerinden — dokunulmaz. Kasa semantiği (kart kasaya girer) açık sorusu güvenlik değil muhasebe kararıdır, oylamaya.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — POS yan defteri tasarımı ve mutasyon listesi sağlam; ancak mevcut kodu değiştiren Karar E'nin regresyon etkisi ölçülmemiş ve en büyük risk (6 çağrı noktasından biri atlanır) için test yok.
  - Koşullar: Karar E (KasaModulu.HareketGir komisyon düzeltmesi) MEVCUT DAVRANIŞI değiştirir ve mevcut GunKapanisiTestleri/MutabakatTestleri kasa bakiyesi beklentilerini kırabilir; ayrı oy maddesi olarak sunulsun ve etkilenen mevcut testlerin listesi + güncellenme kararı spesifikasyona yazılsın. Bu karar onaylanmadan Faz A sevk edilmez. · 'KART bacağı var ama PosTahsilat yok' denetim testi risk bölümünde şart deniyor ama test listesinde yok: PosTahsilatTestleri'ne 'CihazZorunlu=true kiracısında her KART kasa hareketi için tam bir pos_tahsilatlari satırı vardır (6 çağrı noktası: satis, satis-fisi, satis-iade, alis-iade, takas, iscilik)' testi eklensin. · ValorTakvimi_SunucudaToplanir_SatirDonmez ölçüm yöntemi belirsiz; EF komut yakalayıcıyla SQL'de GROUP BY aranması ya da kaynak taraması seçilsin. · Cihaz çözümü 'tam bir aktif cihaz varsa o seçilir' kuralı fail-open eğilimli; PosAyarlari.CihazZorunlu=false ve tek cihaz varken POS satırı yazıldığı test edilsin (KartBacagi_TekCihaz_OtomatikSecilir). · Kültür: valör takvimi/CSV testleri CultureInfo sabitlenmeden koşuyor (test projesinde ayar yok); InvariantCulture ile biçimleme test içinde açıkça doğrulansın. · kur_snapshot_id'nin TRY-only satırda zorunlu tutulması (açık soru) Bertan kararına bağlı; test bu alanın zorunluluğunu ölçmesin, aksi hâlde karar değişince test kırılır.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Karar E (KasaModulu.HareketGir komisyon düzeltmesi) BU PAKETTEN ÇIKARILSIN, ayrı tek konulu paket olarak oylanıp Tamer Bey mühürlemeden uygulanmasın (Bertan+Mehmet+Aşkın ortak; Anayasa 'izinsiz değişiklik yok'). Mehmet yönü doğru bulur, Aşkın etkilenen mevcut test listesini şart koşar.
- Kasa semantiği açık sorusu doğrulanmadan geçilemez: kart tahsilatı kasaya girerken (SatisModulu.cs:1005-1008) gün kapanışı NAKİT sayımıyla karşılaştırıyor (KasaModulu.cs:191) — 'kart satışı olan günde kapanış farkı' senaryosu canlıda ölçülüp raporlansın (Madde 1); paket 30 ile ilgili, muhasebe kararı, ayrı oylama.
- ÇELİŞKİ ÇÖZÜMÜ — kur_snapshot_id: Bertan ZORUNLU kalsın der (mimari değişmez istisnasız), Aşkın 'test bu zorunluluğu ölçmesin' der. Çözüm: alan zorunlu kalır (Bertan kararı), ilgili testler alanın varlığını değil POS defteri davranışını ölçer.
- 'KART bacağı var ama PosTahsilat yok' DB denetim testi Faz A kabul ölçütü: CihazZorunlu=true kiracıda 6 çağrı noktası (satis, satis-fisi, satis-iade, alis-iade, takas, iscilik) tek tek kanıtlansın.
- Kiracı sertliği: PosServisi tüm sorgularda elle TenantId; PosCihazId gövdeden gelince kiracı+şube doğrulaması, yabancı cihaz 404; 'TenantId şartı kaldırılınca sızma testi kırmızı' mutasyon kanıtı zorunlu.
- Yetki: cihaz PATCH ve taksit oran tablosu SAHIP/MUDUR; MUHASEBE eşleştirir ama cihaz oluşturamaz (HttpPosTestleri CihazOlustur_MUHASEBE_403). Eşleşme geri alma SİLMEZ (geri_alindi + gerekçe ≥20, ADR-0004 ruhu).
- POS uçlarında ExecuteUpdate yok; PosTahsilatlari.Add=1 denetim sayacı (*.mutorj hariç). Tevsik kart bacağına uygulanmaz, MASAK işlem toplamı üzerinden — mevcut davranış DEĞİŞTİRİLMEZ.
- Fail-open eğilimi testlensin: PosAyarlari.CihazZorunlu=false + tek aktif cihaz varken POS satırı yazıldığı (KartBacagi_TekCihaz_OtomatikSecilir).
- Kültür sabitleme: valör takvimi ve CSV InvariantCulture ile biçimlensin ve bu test içinde açıkça doğrulansın (test projesinde kültür ayarı yok).
- ValorTakvimi_SunucudaToplanir_SatirDonmez ölçüm yöntemi netleşsin (EF komut yakalayıcı ile SQL'de GROUP BY veya kaynak taraması). Faz B (eşleştirme+taksit+mutabakat) ayrı sevk; Faz A tek başına sevk edilebilir kalsın.
