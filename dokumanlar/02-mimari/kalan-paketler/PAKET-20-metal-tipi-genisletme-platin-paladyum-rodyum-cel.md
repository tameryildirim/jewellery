# Paket 20 — Metal tipi genişletme (platin, paladyum, rodyum, çelik) + saflık tabloları

> Katman: Katman 3 — Stok / ürün kartı · Tahmin: Faz A 2-3 gün · Faz B 1-2 gün · toplam 3-5 gün (PARITE tahmini 2-3 gün; fark, maden karıştırma hatalarının defter ve rapor yollarında da düzeltilmesi gerektiğinin kodda kanıtlanmasından kaynaklanıyor) gün · Önkoşullar: yok · Kaynak: katman mimarı K3-20 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Perakende kuyumcu bugün vitrininde altın ve gümüşün yanında platin alyans, paladyum yüzük, çelik saat/bileklik ve rodyum kaplamalı beyaz altın satıyor. Sistem bu ürünleri kaydedebiliyor ama FİYATLAYAMIYOR ve RAPORLAYAMIYOR: platin ürün altın has kuruyla çarpılıp fahiş fiyat üretiyor, gümüş ürün hiç fiyat üretmiyor, gümüş emaneti altın emanetiyle aynı bakiyeye yazılıyor. Bu paket maden kavramını kodun tamamında tutarlı hale getirir, altın dışı madenler için kotasyon yolunu açar ve maden karıştırmayı fail-closed kapıyla imkânsızlaştırır.

## Kodda bugünkü durum
VAR: Maden sabitleri ALTIN/GUMUS/PLATIN/PALADYUM (Ayarlar.cs:15-32); 18 ayar tanımı K8..K995, G800..G999, PT850..PT999, PD500..PD999 (Ayarlar.cs:45-81); MadenBul/MadeninAyarlari yardımcıları (Ayarlar.cs:92-97); ürün tipi listesinde PLATIN/PALADYUM/CELIK/DEGERLI_TAS (Entities.cs:21-23); stok raporu maden kırılımı ve 'farklı madenler toplanmaz' uyarısı (RaporModulu.cs:475-500). YOK/BOZUK: (1) RODYUM hiçbir yerde yok — grep 0 sonuç. (2) ÇELİK ürün tipi var ama çeliğin ayarı yok; çelik ürün K18 gibi bir altın ayarıyla kaydedilmek zorunda, has gramı sahte üretiliyor (UrunModulu.cs:249 → HesaplaHasGrami her ayarda çalışır). (3) FiyatMotoru yalnız `urun.AyarId.StartsWith('G')` bakıyor (FiyatMotoru.cs:49); PT950/PD999 ürün bu kontrolden GEÇİYOR ve altın has kuruyla fiyatlanıyor — 5 g platin alyans altın kuruyla çarpılıyor. (4) Gümüş/platin/paladyum kotasyonu HİÇBİR YERDE saklanmıyor: kur_snapshots yalnız altin_has_fiyati, gram_altin_fiyati, usd/eur/gbp taşıyor (Entities.cs:110-114); GUMUS paritesi üç kaynaktan da geliyor (KurModulu.cs:313 'gumus', :386 'AG_T', :477 'HAS-GUMUS') ama yalnız bellekte tutuluyor (KurModulu.cs:577 sonPariteler) → kur_snapshot_id'ye bağlanamıyor. (5) Emanet gümüş/platin ayarı kabul ediyor ve has gramı ALTIN bakiyesiyle aynı kolona yazıyor (EmanetModulu.cs:31 regex, :86 ToHasGram, :101 HasGrami, :231 HasGramBakiye) → 100 g gümüş emaneti 100 g altın emaneti gibi görünüyor. (6) HURDA_ALIS aynı şekilde G/PT/PD ayarı kabul ediyor (SatisModulu.cs:145). (7) Gün/dönem raporları has gramları maden ayırmadan topluyor (RaporModulu.cs:62-75, :132-133, :296-320). (8) Ürünler ekranı ayar listesi yalnız K8..K24 + G800/G925/G950 (Urunler.razor:551) ve tip listesi PLATIN/PALADYUM/CELIK/DEGERLI_TAS içermiyor (Urunler.razor:550) → API'de var olan ayarlar arayüzden seçilemiyor. (9) Ayar regex'i dört ayrı dosyada elle kopyalanmış (UrunModulu.cs:36, :116, EmanetModulu.cs:31, SatisModulu.cs:145) hâlbuki Ayarlar.Desen sabiti var (Ayarlar.cs:104) — yeni ayar eklenince dört yerden biri unutulur.

## Kapsam
**İçinde:**
- Faz A — Maden bütünlüğü: RODYUM (kaplama) ve CELIK madeninin modellenmesi, çelik ayarları, Ayarlar.Desen'in tek kaynak yapılması, hareketler.maden kolonu, MadenKapisi fail-closed kapısı, emanet/hurda/sayım/rapor yollarında maden ayrımı, ürün ve emanet ekranlarının tam ayar/tip listesine açılması.
- Faz B — Kotasyon ve fiyat: kur_snapshots'a gümüş/platin/paladyum gram fiyatı kolonları, otomatik kaynaklardan gümüş kotasyonunun snapshot'a yazılması, platin/paladyum için manuel kotasyon girişi, FiyatMotoru'nun maden bazlı kur seçmesi, kotasyon yoksa fail-closed teklif yokluğu.
- Saflık (milyem) tablolarının ekranda gösterilmesi: seçilen madenin ayarları ve milyemleri ürün formunda listelenir (Ayarlar.MadeninAyarlari zaten var, kullanılmıyor).
- Rodyum kaplama işareti (ürün kartı özniteliği) ve etikette görünmesi.

**Dışında:**
- Cari bakiyenin kalıcı çoklu-maden kolonları — paket 27 (çoklu birim cari bakiye). Bu paket cari_bakiyeler tablosuna DOKUNMAZ.
- Kâr-zarar/maliyet motorunun maden bazında ayrıştırılması — paket 35.
- Rodyum kaplama işçiliği/hizmet satışı (kaplama servisi bir işçilik hizmetidir, imalat değil) — bu pakette yalnız ÜRÜN ÖZNİTELİĞİ olarak girer, hizmet satışı açılmaz.
- Ons/USD üzerinden platin fiyat türetme (XPTUSD × kur) — kaynak doğrulanmadı, açık soru.
- Vitrin panosuna platin/paladyum satırı eklenmesi — pano kalem kümesi PANO-KARAR ile mühürlü.

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| hareketler | YENİ KOLON `maden` VARCHAR(10) NOT NULL DEFAULT 'ALTIN' — bu satırın has/gram bacağının hangi madene ait olduğu. Mevcut satırlar için geri doldurma: ürünü olan satırlarda ürünün ayarından, olmayanlarda 'ALTIN'. | maden VARCHAR(10) NOT NULL DEFAULT 'ALTIN' | (tenant_id, maden, tarih) — maden bazlı dönem raporları bu indeksi kullanır | YYYYMMDDHHMMSS_HareketMaden |
| urunler | YENİ KOLON `rodyum_kaplama` TINYINT(1) NOT NULL DEFAULT 0 — ürün rodyum kaplamalı mı (etiket/vitrin ibaresi için). | rodyum_kaplama tinyint(1) NOT NULL DEFAULT 0 | yeni indeks yok (filtreleme ölçütü değil) | YYYYMMDDHHMMSS_HareketMaden (aynı migration) |
| kur_snapshots | Faz B — YENİ KOLONLAR: gumus_gram_fiyati, platin_gram_fiyati, paladyum_gram_fiyati DECIMAL(15,4) NULL. Hepsi NULL olabilir; NULL = kotasyon yok = o madende fiyat teklifi üretilmez. | gumus_gram_fiyati decimal(15,4) NULL, platin_gram_fiyati decimal(15,4) NULL, paladyum_gram_fiyati decimal(15,4) NULL | yeni indeks yok (snapshot zaten tenant+olusturuldu üzerinden okunuyor) | YYYYMMDDHHMMSS_MadenKotasyonlari |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/urun/madenler` | tüm roller (okuma) | YENİ (Faz A). Maden listesi + her madenin ayarları (id, ad, milyem, kıymetli mi, has hesaplanır mı). Ürün formu seçim listesini buradan kurar; sabit liste arayüze KOPYALANMAZ (bugünkü Urunler.razor:551 kopyası bu yüzden geride kaldı). | [Authorize]; kiracıya bağlı veri yok |
| POST | `/api/v1/urun` | SAHIP, MUDUR, TEZGAHTAR (mevcut haliyle) | DEĞİŞİR. AyarId regex'i Ayarlar.Desen'e bağlanır (çelik ayarları dahil); yeni alan rodyumKaplama; MadenKapisi.UrunDogrula çağrılır (tip↔maden tutarlılığı, çelikte has=0). | TasKapisi, PirlantaKapisi, KonsinyeKapisi (mevcut) + MadenKapisi (yeni) |
| PATCH | `/api/v1/urun/{id}` | mevcut haliyle | DEĞİŞİR. Aynı kapılar; ayar değişince maden de değişebilir — stok has gramı yeniden hesaplanırken (UrunModulu.cs:542-550) maden değişimi denetim izine yazılır. | MadenKapisi + mevcutlar |
| POST | `/api/v1/emanet/giris` | mevcut haliyle | DEĞİŞİR. Hareket satırına maden yazılır; altın dışı emanet reddedilmez, maden bazında ayrı bakiye olarak izlenir. | MadenKapisi.HareketDogrula |
| GET | `/api/v1/emanet/{cariId}` | mevcut haliyle | DEĞİŞİR. Yanıt `mevcutHasGramBakiye` (yalnız ALTIN) + yeni `madenBakiyeleri: [{maden, madenAdi, gram}]`. Eski alanın anlamı DARALIR — istemci güncellenmeden sevk edilmez. | - |
| POST | `/api/v1/emanet/cikis` | mevcut haliyle | DEĞİŞİR. Çıkış talebi hangi madenden olduğunu taşır; o madenin net bakiyesinden fazlası verilemez (bugün EmanetModulu.cs:146 tek havuzdan kontrol ediyor — gümüş yatıran müşteri altın çekebiliyor). | MadenKapisi + mevcut yetersiz bakiye kontrolü |
| POST | `/api/v1/kur/manuel` | mevcut haliyle (MUDUR+) | DEĞİŞİR (Faz B). ManuelKurDto'ya gumusGramFiyati, platinGramFiyati, paladyumGramFiyati opsiyonel alanları eklenir. | mevcut doğrulama + Range(0,..) |
| GET | `/api/v1/kur/guncel` | tüm roller | DEĞİŞİR (Faz B). Yanıta madenKotasyonlari eklenir; ayarFiyatlari bugün yalnız ALTIN ayarlarını üretiyor (KurModulu.cs:723, :884) — kotasyonu olan her maden için üretilir. | - |
| GET | `/api/v1/fiyat/teklif` | mevcut haliyle | DEĞİŞİR (Faz B). Kur snapshot'undan ürünün MADENİNE karşılık gelen kotasyon okunur; yoksa {teklifVar:false, sebep:'KOTASYON_YOK'} döner (bugün FiyatModulu.cs:246 her ürün için AltinHasFiyati okuyor). | - |
| GET | `/api/v1/rapor/gun-sonu, /donem, /cari-bakiye` | mevcut haliyle | DEĞİŞİR. Has toplamları maden bazında kırılır; `toplamHas` yalnız ALTIN kalır, `madenDagilimi` eklenir (stok raporundaki desen, RaporModulu.cs:475-500, diğer raporlara taşınır). | TB-19: sunucuda GroupBy, sınırsız ToListAsync açılmaz |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Core/Domain/Ayarlar.cs` — Maden sınıfına Celik ve Rodyum sabitleri; Hepsi dizisi genişler. Ayar record'una iki yeni özellik: Kiymetli (bool) ve HasHesaplanir (bool). Çelik ayarları eklenir (paslanmaz çelik kalite kodları, milyem 0, HasHesaplanir=false). Rodyum bir AYAR olarak eklenmez — kaplamadır; Maden.Rodyum sabiti yalnız etiket/ibare metni için vardır. Desen sabiti Tumu.Keys'ten ÜRETİLİR (bugün elle yazılı string, Ayarlar.cs:104) ki yeni ayar eklendiğinde desen kendiliğinden genişlesin. HasHesap.ToHasGram, HasHesaplanir=false ayarda 0 döner.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Urun/MadenKapisi.cs` — Fail-closed maden kapısı. UrunDogrula(tip, ayarId, rodyumKaplama): (a) ayar geçerli mi; (b) tip↔maden tutarlı mı — tip GUMUS ise ayar gümüş ayarı olmalı, PLATIN ise platin, PALADYUM ise paladyum, CELIK ise çelik ayarı; ZIYNET/KULCE/HURDA/SIKKE altın ayarı bekler; PIRLANTA/SAAT/DEGERLI_TAS serbesttir; (c) çelik/kıymetsiz ayarda satış fiyatı ZORUNLUDUR (has×kur ile fiyat üretilemez, aksi halde ürün hiç fiyatlanamaz); (d) rodyum kaplama yalnız altın/platin/paladyum madeninde işaretlenebilir. HareketDogrula(hareket): has bacağı sıfırdan farklıysa maden dolu ve geçerli olmalı. Her ihlal BadRequestException ile Türkçe, sebebi yazan mesaj döndürür.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Hareket'e Maden kolonu (varsayılan 'ALTIN', XML yorumunda 'has_grami bu madenin gramıdır; ALTIN dışı gramlar altın toplamına KATILMAZ' notu). Urun'a RodyumKaplama. Faz B'de KurSnapshot'a üç kotasyon kolonu. Sabitler.UrunTipleri değişmez (zaten tam).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — hareketler için (tenant_id, maden, tarih) indeksi. Global kiracı filtresi ve yazma denetçisi davranışı değişmez.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs` — Ekle içinde MadenKapisi.HareketDogrula çağrısı — deftere yazan TEK yol burası olduğu için maden işaretinin boş kalması burada imkânsızlaşır (Hareketler.Add başka yerde yasak, denetim.sh zorluyor).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — İki RegularExpression (satır 36, 116) Ayarlar.Desen'e bağlanır; DTO'lara RodyumKaplama; Olustur/Guncelle içinde MadenKapisi.UrunDogrula; HesaplaHasGrami kıymetsiz ayarda 0 üretir; GET /madenler ucu; ürün yanıtına maden ve madenAdi eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs` — Regex → Ayarlar.Desen; giriş hareketine maden yazılır; EmanetNet (satır 219) maden parametresi alır; çıkış talebi maden taşır ve o madenin netiyle karşılaştırılır; BakiyeGuncelle YALNIZ maden ALTIN iken cari_bakiyeler.has_gram_bakiye'ye dokunur (paket 27'ye kadar kalıcı kolon açılmaz), altın dışı bakiye hareketlerden hesaplanır; yanıt madenBakiyeleri taşır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — HURDA_ALIS regex'i (satır 145) Ayarlar.Desen'e bağlanır; satış/alış/iade/karma satışta yazılan her hareket satırına ürünün madeni yazılır (satır 489, 590, 709, 792, 964, 1108, 1217); satış adedini has gramından geri hesaplayan yollar (satır 1291, 1353) maden bazında güvenli kalır (bölme aynı ürünün birim has gramına yapılıyor, değişmez).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — Gün sonu (satır 62-75), dönem (132-133), hurda (257) ve cari bakiye (296-320) raporlarında has toplamları hareketler.maden ile gruplanır; toplamHas alanı YALNIZ ALTIN kalır ve madenDagilimi + uyarı alanı stok raporundaki (475-500) biçimle birebir aynı olur. Gruplama sunucuda GroupBy ile yapılır, satır listesi çekilmez (TB-19).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs` — Faz B: ManuelKurDto üç yeni opsiyonel kotasyon alanı; KaydetVeyaMevcut değişiklik karşılaştırmasına (satır 813) yeni kolonlar girer; adaptörlerden gelen GUMUS paritesi snapshot'a yazılır (Truncgil 'gumus', Altınkaynak 'AG_T', Ankara 'HAS-GUMUS' eşlemeleri ZATEN VAR); platin/paladyum hiçbir kaynakta yok → yalnız manuel; SnapshotZenginlestir ayar fiyatlarını kotasyonu olan HER maden için üretir (bugün AltinAyarlari sabitine bağlı, satır 723/884).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs` — Hesapla imzası hasKuru yerine MadenKuru(maden→kotasyon) alır. Kural 2 yeniden yazılır: 'AyarId.StartsWith(G)' (satır 49) kaldırılır; ürünün madeni Ayarlar.MadenBul ile bulunur, o madenin kotasyonu yoksa (ve sabit fiyat da yoksa) null döner. Kıymetsiz maden (çelik) yalnız sabit fiyatla satılır. Yuvarlama, kampanya ve indirim kuralları DEĞİŞMEZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` — Teklif ucu (satır 240-252) snapshot'tan yalnız AltinHasFiyati değil üç kotasyonu da okur; sebep kodlarına KOTASYON_YOK eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs` — farkHas (satır 147) maden etiketiyle döner; sayım düzeltme hareketine maden yazılır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs + GiderPusulasiKapisi.cs` — Pusula satırlarında maden gösterilir; hurda hareketine maden yazılır. Gider pusulası mevzuat kapıları DEĞİŞMEZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs` — Ürün ve hareket CSV'lerine maden kolonu (satır 121, 154). ADR-0014 dışa aktarım biçimi değişmez.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — TR+EN: Maden, Madenler, Saflik/Milyem, RodyumKaplama, Celik, Platin, Paladyum, KotasyonYok, MadenBakiyeleri, MadenDagilimi anahtarları (Urun ve Kur bölümlerine).
- **DEGISTIR** `dotnet/denetim.sh` — İki yeni denetim: (1) ayar regex'i elle yazılmış kalmasın — 'K8|K14' deseninin kod içinde bulunma sayısı 0 (tek kaynak Ayarlar.Desen); (2) HasHesap.ToHasGram doğrudan çağrısı beyaz listeyle sınırlı kalsın (maden kapısını atlayan has hesabı üretilmesin).

## Ekranlar
- `/urunler` · `dotnet/Kuyumcu.Web/Pages/Urunler.razor` — Tipler ve Ayarlar sabit dizileri (satır 550-551) kaldırılır; liste GET /api/v1/urun/madenler ucundan gelir. Form akışı: önce MADEN seç → o madenin ayarları milyemiyle listelenir (saflık tablosu görünür hale gelir). Rodyum kaplama onay kutusu. Çelik seçilince has gramı alanı 'hesaplanmaz' der ve satış fiyatı zorunlu olur. Ayar filtresi maden filtresine dönüşür. (menü/yetki: mevcut menü ve rol kısıtları değişmez)
- `/emanet` · `dotnet/Kuyumcu.Web/Pages/Emanet.razor` — Sabit ayar listesi (satır 182-183) uca bağlanır; 'elimizdeki has' tek rakam yerine maden bazlı satırlar; çıkışta maden seçimi. (menü/yetki: değişmez)
- `/kur` · `dotnet/Kuyumcu.Web/Pages/Kur.razor` — Faz B: manuel kur formuna gümüş/platin/paladyum gram fiyatı alanları; ayar fiyatları tablosu maden sekmeli; kotasyonu olmayan maden 'kotasyon girilmedi' rozetiyle gösterilir (sessizce boş bırakılmaz). (menü/yetki: manuel giriş mevcut rol kısıtıyla (MUDUR+))
- `/raporlar` · `dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — Has toplamlarının yanında maden dağılımı tablosu; 'toplam has yalnız altındır' uyarısı ekranda görünür. (menü/yetki: değişmez)
- `/urun-etiketleri` · `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor` — Etikette maden + milyem (ör. 'Platin 950') ve rodyum kaplama ibaresi. Belge üretimi istemcide @media print kalır (ADR-0007). (menü/yetki: değişmez)

## İş kuralları (fail-closed)
- K20-1 (fail-closed): has/gram bacağı sıfırdan farklı her hareket satırı geçerli bir maden taşır; maden boş veya bilinmeyen ise HareketFabrikasi.Ekle patlar — deftere yazılmaz.
- K20-2 (değişmez): hareketler.has_grami ve cari_bakiyeler.has_gram_bakiye 'altın has gramı' anlamındadır. Altın dışı maden bu bakiyeyi DEĞİŞTİREMEZ.
- K20-3: hiçbir toplama farklı madenlerin gramlarını tek sayıda birleştirmez; her toplam ya tek madene aittir ya da maden kırılımı olarak döner.
- K20-4 (fail-closed): ürünün madenine ait kotasyon yoksa ve sabit satış fiyatı da yoksa fiyat teklifi ÜRETİLMEZ (sebep KOTASYON_YOK). Başka bir madenin kuru ikame edilmez.
- K20-5: kıymetsiz maden (çelik) ürününde has gramı 0'dır ve satış fiyatı zorunludur.
- K20-6: ürün tipi ile ayarın madeni tutarsızsa ürün kaydedilemez (tip GUMUS + ayar K22 reddedilir).
- K20-7: rodyum bir stok madeni değildir; yalnız kaplama işareti olarak bulunur ve has hesabına girmez.
- K20-8: ayar listesi tek kaynaktan (Ayarlar.Tumu) türetilir; DTO deseni, arayüz listesi ve ekran filtresi bu kaynaktan beslenir.
- K20-9: maden emaneti çıkışı yalnız aynı madenin net bakiyesi kadar yapılabilir.
- K20-10: mevcut kayıtların geri doldurulması ürünün AYARINDAN yapılır; ürünü olmayan satır 'ALTIN' kalır ve bu varsayım migration yorumunda açıkça yazılır.

## Testler
- `dotnet/Kuyumcu.Tests/MadenTestleri.cs` [saf]
  - Her ayarın madeni doğru çözülür (K22→ALTIN, G925→GUMUS, PT950→PLATIN, PD999→PALADYUM, çelik ayarı→CELIK)
  - Çelik ayarında ToHasGram 0 döner
  - Ayarlar.Desen, Tumu.Keys ile birebir aynı kümeyi kapsar (elle yazılmış desen sapması yakalanır)
  - MadenKapisi: tip GUMUS + ayar K22 reddedilir; tip ZIYNET + ayar PT950 reddedilir
  - MadenKapisi: çelik üründe satış fiyatı yoksa reddedilir
  - MadenKapisi: rodyum kaplama çelik üründe reddedilir
- `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs` [DB]
  - Platin ürün satışında hareket satırının madeni PLATIN yazılır
  - Gümüş emaneti cari_bakiyeler.has_gram_bakiye'yi DEĞİŞTİRMEZ; altın emaneti değiştirir
  - Gümüş yatıran müşteri altın çekemez (emanet çıkışı maden bazında reddedilir)
  - Maden boş bırakılmış hareket HareketFabrikasi.Ekle ile yazılamaz
  - Dönem raporunda 100 g altın + 500 g gümüş 'toplamHas=100' ve iki satırlık madenDagilimi üretir
  - Geri doldurma: migration sonrası eski satırların madeni ürün ayarıyla tutarlıdır
- `dotnet/Kuyumcu.Tests/FiyatMotoruTestleri.cs` [saf]
  - Platin ürün altın has kuruyla fiyatlanmaz (kotasyon yoksa null)
  - Platin kotasyonu verilince fiyat platin kotasyonundan hesaplanır
  - Gümüş ürün gümüş kotasyonuyla fiyatlanır (bugünkü 'her zaman null' davranışı değişir)
  - Çelik ürün yalnız sabit fiyatla fiyatlanır
  - Mevcut altın senaryolarının sonuçları DEĞİŞMEZ (regresyon)
- `dotnet/Kuyumcu.Tests/MadenKotasyonTestleri.cs` [HTTP]
  - POST /api/v1/kur/manuel ile girilen gümüş kotasyonu snapshot'a yazılır ve teklifte kullanılır
  - Kotasyon yoksa GET /api/v1/fiyat/teklif {teklifVar:false, sebep:'KOTASYON_YOK'} döner
  - Kotasyon değişmediyse yeni snapshot satırı yazılmaz (KaydetVeyaMevcut davranışı korunur)
- `dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs` [bUnit]
  - Ürün formunda maden seçilince yalnız o madenin ayarları ve milyemleri listelenir
  - Çelik seçilince satış fiyatı alanı zorunlu işaretlenir ve kaydet düğmesi fiyatsız etkin olmaz
  - Ayar listesi sabit diziden değil uçtan gelir (sahte API deseni, CariEkranTestleri.cs)

## Mutasyon kanıtları
- MadenKapisi.UrunDogrula içindeki tip↔maden kontrolü kaldırılırsa MadenTestleri.TipVeAyarMadeniTutarsizsaRed KIRMIZI olmalı.
- FiyatMotoru'nda maden kontrolü tekrar 'AyarId.StartsWith(G)' hâline döndürülürse FiyatMotoruTestleri.PlatinUrunAltinKuruylaFiyatlanmaz KIRMIZI olmalı.
- HareketFabrikasi.Ekle'deki MadenKapisi.HareketDogrula çağrısı silinirse MadenDefteriTestleri.MadensizHareketYazilamaz KIRMIZI olmalı.
- EmanetModulu'nda BakiyeGuncelle'nin maden koşulu kaldırılırsa MadenDefteriTestleri.GumusEmanetiAltinBakiyesiniDegistirmez KIRMIZI olmalı.
- Rapor gruplamasından maden kırılımı çıkarılıp tek toplam döndürülürse MadenDefteriTestleri.DonemRaporuMadenleriToplamaz KIRMIZI olmalı.
- Ayarlar.Desen tekrar elle yazılmış sabite döndürülüp bir ayar eksik bırakılırsa MadenTestleri.DesenTumAyarlariKapsar KIRMIZI olmalı.
- Emanet çıkışındaki maden koşulu kaldırılırsa MadenDefteriTestleri.GumusYatiranAltinCekemez KIRMIZI olmalı.

## Canlı doğrulama
- Test kiracısı aç; platin alyans (PT950, 5 g), gümüş kolye (G925, 12 g), çelik saat (çelik ayarı, sabit fiyat 2.500 TL) ve rodyum kaplamalı beyaz altın yüzük (K18 + rodyum işareti) kaydet.
- Kotasyon girilmeden POS'ta platin alyansın fiyatını iste → 'kotasyon yok' mesajı görülmeli, TL fiyat ÜRETİLMEMELİ (bugün altın kuruyla fahiş fiyat üretiyor — önce/sonra ekran görüntüsü kanıt olarak alınır).
- Manuel kurdan platin ve gümüş gram fiyatı gir → aynı ürünlerin teklifleri doğru kotasyondan hesaplanmalı; elle hesapla karşılaştır.
- Gümüş kolyeyi sat, platin alyansı sat, altın yüzüğü sat; gün sonu raporunda toplamHas'ın yalnız altını saydığını ve madenDagilimi'nın üç satır verdiğini doğrula.
- Aynı cariye 50 g gümüş emanet al, sonra 10 g ALTIN emanet çıkışı dene → reddedilmeli.
- Çelik saati sabit fiyatsız kaydetmeyi dene → reddedilmeli.
- Doğrulama bitince test kiracısını sil (kuyumcu_net başlangıç 5 kiracıya dönmeli).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/Domain/Ayarlar.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Hurda/GiderPusulasiKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/MadenKapisi.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/ (2 yeni migration + ModelSnapshot)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Emanet.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Kur.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MadenTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MadenKotasyonTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/FiyatMotoruTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MaliyetTestleri.cs (MadenAyrimiTestleri:784 genişler)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/adr/ (yeni ADR: has = altın kavramı, maden işareti zorunlu)`

## Mevzuat dayanağı
- Kuyum Ticareti Yönetmeliği md.11/(e), RG 24.06.2026/33290 — kıymetli taş kökeni ibaresi — **DOGRULANDI** — Kodda zaten uygulanıyor (Entities.cs:190-207, TasKapisi). Bu paket bu kurala DOKUNMAZ; rodyum kaplama ibaresi bu maddeyle karıştırılmamalıdır.
- Platin mücevherde 950/900, gümüşte 925, altında 585/750/916 milyem standartları (ticari teamül + mevcut kod) — **DOGRULANMALI** — Kodda 2026-09-14'te eklenmiş (Ayarlar.cs:70-81) ancak dayanağı belgelenmemiş. Milyem değerleri hesaplama sabitidir, yasal hüküm olarak sunulmaz.
- Kıymetli madenlerin damgalanması / ayar denetimi mevzuatı (Darphane ayar evi uygulamaları) — **VARSAYIM** — Rodyum kaplamanın etikette gösterilmesinin ZORUNLU olup olmadığı doğrulanmadı. Bu yüzden rodyum ibaresi yapılandırmadan okunur ve VARSAYILAN KAPALI gelir; zorunluluk iddiası koda gömülmez (ADR-0005).
- Çelik ve kıymetsiz metal ürünlerin kıymetli maden mevzuatı kapsamı — **VARSAYIM** — Çeliğin kıymetli maden sayılmadığı teknik bir gerçektir; ancak KDV/belge açısından farklı bir rejime tabi olup olmadığı doğrulanmadı — paket 1 (fatura satır kalemi) ile birlikte araştırılmalı. Bu pakette yalnız 'has hesaplanmaz' teknik kuralı uygulanır.
- Tevsik (30.000 TL nakit haddi) ve MASAK (185.000 TL) kapıları — **DOGRULANDI** — Maden değişikliği bu kapıları ETKİLEMEZ — ikisi de TL bacağı üzerinden çalışır. Mevcut kapı çağrıları aynen korunur, yeni akış açılmaz.

## Riskler
- GERİ DOLDURMA RİSKİ: mevcut hareket satırlarının madeni ürünün BUGÜNKÜ ayarından türetilir; ürün ayarı satıştan sonra değiştiyse geçmiş satır yanlış etiketlenir. Azaltma: migration yalnız ürünü olan satırları doldurur, kalanı 'ALTIN' bırakır ve bu varsayım migration yorumunda + denetim izinde açıkça yazılır. Tek dükkânda ayar değişimi nadirdir ama 'yok' denemez.
- SÖZLEŞME KIRAN DEĞİŞİKLİK: /api/v1/emanet/{cariId} yanıtındaki mevcutHasGramBakiye'nin anlamı daralır (artık yalnız altın). Web ile API aynı sürümde sevk edilmezse ekran yanlış rakam gösterir. Azaltma: Faz A tek seferde sevk edilir, alan adı değişmez ama XML/dokümanda anlamı yazılır.
- KOTASYON KAYNAĞI YOK: platin ve paladyum için üç kur kaynağının hiçbiri kotasyon vermiyor (KurModulu.cs:300-313, 371-386, 466-478 eşleme listelerinde yok). Manuel giriş unutulursa platin ürünler hiç fiyatlanmaz. Azaltma: fail-closed davranış ekranda açık mesajla gösterilir, sessiz sıfır fiyat üretilmez.
- TEST YÜZEYİ: SatisModulu.cs'de 7 ayrı hareket yazma noktası var; birinde maden atlanırsa o akış çalışmaz hale gelir (fabrika patlar). Bu bilinçli bir fail-closed tercihidir; sevk öncesi bütün satış/iade/karma ödeme testlerinin yeşil olması şarttır.
- ÇELİK AYAR KODLARI: paslanmaz çelik kalite kodlarının (316L vb.) kuyumcu ERP'lerinde standart bir 'ayar' karşılığı olup olmadığı doğrulanmadı — açık soru.

## Açık sorular
- Çelik ürünler için ayar kodu ne olmalı: tek bir 'CELIK' kodu mu, yoksa 316L/904L gibi kalite kodları mı? Perakende kuyumcuda çelik saat/bileklik ayar ayrımı yapılıyor mu? (Kurucu/pilot dükkân doğrulaması gerekir.)
- Rodyum kaplama etikette/faturada gösterilmek zorunda mı? (Mevzuat dayanağı doğrulanmadı — varsayılan KAPALI tasarlandı.)
- Platin/paladyum kotasyonu için ücretsiz ve anahtarsız bir Türkçe kaynak var mı (XPTUSD × USD/TRY türetmesi kabul edilebilir mi)? Türetme yapılacaksa hangi marj ile?
- Gümüş kotasyonu kaynaklardan 'has gümüş' (999) olarak mı geliyor, yoksa 925 gram fiyatı mı? Fiyatlama formülü buna bağlı (kotasyon×has gram mı, kotasyon×net gram mı) — üç kaynağın canlı yanıtı incelenmeden karar verilmemeli.
- Altın dışı maden emanetinin cari ekstresinde nasıl görüneceği paket 27 ile mi birleştirilsin, yoksa bu pakette geçici bir hesaplanan görünüm mü verilsin? (Öneri: hesaplanan görünüm; kalıcı kolon paket 27.)
- Mevcut veride altın dışı ayarla kaydedilmiş ürün/hareket var mı? Canlı veritabanında sayılmalı (salt okunur görev olduğu için bu oturumda sorgu çalıştırılmadı — okunmadı).

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Tespit edilen kusurlar gerçek ve kanıtlı: FiyatMotoru.cs:48 yalnız 'G' önekine bakıyor, yani PT950/PD999 ürün altın has kuruyla fiyatlanıyor (fahiş fiyat); kur_snapshots'ta gümüş/platin/paladyum kotasyonu yok; emanet altın dışı madeni aynı has bakiyesine yazıyor. 'Maden tek kaynak = Ayarlar.MadenBul, tek istisna defter satırı' kararı veri modeli açısından DOĞRU ve has/TL çift defter sözleşmesini korur. Tek sorun kapsam büyüklüğü.
  - Koşullar: FAZ A İKİYE BÖLÜNMELİ (sevk edilebilirlik): A1 = Ayarlar.Desen tek kaynak + çelik/rodyum modeli + MadenKapisi + ürün/ekran (hiçbir defter satırına dokunmaz, tek başına sevk edilir). A2 = hareketler.maden kolonu + geri doldurma + HareketFabrikasi kapısı + emanet maden ayrımı + rapor kırılımı. Tek migration'da 7 satış yazma noktası + emanet + sayım + hurda + rapor değiştirmek 957 testin geniş bölümünü aynı anda riske atar. · Geri doldurma kuralı migration YORUMUNDA değil, DENETİM İZİNDE de kayıtlı olmalı: 'ürünü olan satır bugünkü ayardan, kalanı ALTIN' bir VARSAYIMDIR ve Madde 1 gereği geçmişte ayar değişmiş ürünlerde yanlış etiket üretir. Zorunlu ek: migration öncesi 'kaç satır ürünsüz / kaç ürünün ayarı satış tarihinden sonra değişmiş' SAYIMI raporlanmadan Up çalıştırılmaz. · /api/v1/emanet/{cariId} yanıtındaki mevcutHasGramBakiye'nin anlamı daraltılıyor — bu SÖZLEŞME KIRAN değişikliktir. Alan adı korunacaksa yanıta 'mevcutHasGramBakiye = yalnız ALTIN' açık bir bayrak/notu eklenmeli ve Web ile API aynı sürümde sevk edilmeli (spec bunu risk olarak yazmış, koşula çevriliyor). · K20-2 (has = altın) ADR'ye yazılmadan kod yazılamaz — spec zaten yeni ADR öneriyor, ADR numarası çakışması için bkz. çapraz bulgu 1. · Paket 25 ve paket 27 ile aynı dalgada olamaz: üçü de Entities.cs + RaporModulu.cs + (20/25) SatisModulu.cs paylaşıyor. · RaporModulu'na maden kırılımı eklenirken StokDegeri'ndeki mevcut sınırsız ToListAsync (TB-19) KOPYALANMAYACAK; yeni GroupBy'lar sunucuda kalacak — bu, paket 25'in de aynı dosyada tespit ettiği borçtur.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Para el değiştiren yeni akış yok; tevsik/MASAK TL bacağı üzerinden değişmiyor. Fail-closed fiyat (KOTASYON_YOK) ve has=altın değişmezi güvenlik açısından doğru. Tek gerçek risk migration'ın denetimsiz veri dönüşümü.
  - Koşullar: Geri doldurma migration'ı hareketler.maden'i ürün ayarından yazarken DenetimKaydi düşürmez (SaveChanges dışı). Migration Up içinde yazılan satır sayısı ve varsayım ('ürünsüz satır ALTIN') migration yorumuna VE sevk raporuna sayıyla yazılsın; canlı DB'de altın dışı ayarlı ürün/hareket sayısı sevk ÖNCESİ ölçülsün (acik_soru 6 kapatılmadan Faz A sevk edilmez). · GET /api/v1/urun/madenler [Authorize] zorunlu (spec böyle); kiracıya bağlı veri olmasa da anonim uç açılmasın. · /api/v1/emanet/{cariId} yanıt sözleşmesi daralıyor: Web+API tek sevk; eski istemciyle karşılaşma testi (mevcutHasGramBakiye artık yalnız altın) ekransal olarak kanıtlansın. · MadenKapisi.HareketDogrula HareketFabrikasi.Ekle içinde çağrılsın (spec böyle) — denetim.sh'e 'MadenKapisi.HareketDogrula çağrısı = 1 ve yalnız HareketFabrikasi.cs' sayacı eklensin. · Manuel kotasyon (POST /kur/manuel) MUDUR+ ve Range(0,∞) servis içinde de elle; kotasyon değişikliği denetim izine (mevcut kur manuel deseni neyse aynısı).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Kapsam büyük ama fazlar ayrılmış, mutasyonlar test adlarıyla eşleşiyor. Geri doldurma testi ölçülemez biçimde tanımlı; regresyon kapısı açıkça yazılmamış.
  - Koşullar: MadenDefteriTestleri 'Geri doldurma: migration sonrası eski satırların madeni ürün ayarıyla tutarlıdır' kuyumcu_test'te ölçülemez (taze migration, eski satır yok). Test, migration Up öncesi ham SQL ile maden=NULL/olmayan satır ekleyip Up'ı çalıştıramaz (EF testi tek yönlü). Ya bu senaryo canlı doğrulamaya taşınıp SQL kanıtı (SELECT COUNT(*) WHERE maden='ALTIN' AND urun_id IN (gümüş ürünler)=0) istensin, ya da migration SQL'i saf test ile dize düzeyinde doğrulansın. · FiyatMotoruTestleri 'Mevcut altın senaryolarının sonuçları DEĞİŞMEZ' için altın kalemli mevcut testlerin (MaliyetTestleri, SatisFisiTestleri, KarmaOdeme) hepsinin 0 başarısızla geçmesi paket kabul ölçütü olarak yazılsın; 7 hareket yazma noktasından birinde maden atlanırsa HareketFabrikasi patlar — bu fail-closed tercih ancak tam regresyonla güvenli. · Faz B 'Kotasyon değişmediyse yeni snapshot yazılmaz' testi HTTP yerine DB olsun (KaydetVeyaMevcut servis davranışı); HTTP testinde kur zamanlayıcısı HttpOrtami.cs:78 ile kaldırıldığından yalnız manuel giriş yolu ölçülür — test adı bunu belirtsin. · Emanet yanıt sözleşmesi daralması (mevcutHasGramBakiye yalnız altın) için mevcut Emanet ekran/HTTP testlerinin GÜNCELLENMESİ ayak izine yazılsın; aksi hâlde sahte API JsonException ile kırılır. · Mutasyon 'Ayarlar.Desen elle sabite döndürülürse' testi, Desen'in Tumu.Keys'ten üretildiği yeni tasarımda kaynak taramasıyla değil davranışla ölçülmeli: Regex(Desen).IsMatch(her ayar) ve IsMatch('K23')=false.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Faz A ikiye bölünür: A1 = Ayarlar.Desen tek kaynak + çelik/rodyum modeli + MadenKapisi + ürün/ekran (deftere dokunmaz, tek başına sevk edilir); A2 = hareketler.maden kolonu + geri doldurma + HareketFabrikasi kapısı + emanet maden ayrımı + rapor kırılımı.
- Geri doldurma bir VARSAYIMDIR: sevk ÖNCESİ canlıda 'kaç satır ürünsüz / kaç ürünün ayarı satış sonrası değişmiş / altın dışı ayarlı ürün-hareket sayısı' ölçülmeden Up çalıştırılmaz (açık soru 6 kapanmadan Faz A sevk yok); varsayım + yazılan satır sayısı migration yorumuna VE sevk raporuna yazılır (migration DenetimKaydi düşürmez).
- /api/v1/emanet/{cariId} sözleşmesi daralıyor (mevcutHasGramBakiye = yalnız ALTIN): Web+API tek sevk, yanıtta açık bayrak/not, eski istemci senaryosu ekransal kanıtlanır; mevcut Emanet ekran/HTTP testlerinin güncellenmesi ayak izine yazılır (sahte API JsonException riski).
- MadenKapisi.HareketDogrula yalnız HareketFabrikasi.Ekle içinde çağrılır; denetim.sh'a 'çağrı = 1 ve yalnız HareketFabrikasi.cs' sayacı eklenir. GET /urun/madenler [Authorize] zorunlu, anonim uç açılmaz.
- Manuel kotasyon (POST /kur/manuel) MUDUR+ ve Range(0,∞) servis içinde elle doğrulanır; kotasyon değişikliği mevcut kur-manuel desenindeki denetim izine düşer. Kotasyon yoksa fail-closed KOTASYON_YOK, sessiz sıfır fiyat yok (KurModulu.cs:300-313/371-386/466-478'de platin/paladyum eşlemesi yok).
- K20-2 (has = altın) ADR'ye yazılmadan kod yazılmaz; ADR numarası çakışması tek elden çözülür.
- Regresyon kapısı kabul ölçütüdür: FiyatMotoru/Maliyet/SatisFisi/KarmaOdeme dâhil mevcut altın senaryolarının tamamı 0 başarısız (7 hareket yazma noktasından biri madeni atlarsa fabrika patlar — bilinçli fail-closed).
- Geri doldurma testi ölçülebilir kılınır: kuyumcu_test'te eski satır yok — ya canlı doğrulamaya taşınıp SQL kanıtı (COUNT WHERE maden='ALTIN' AND urun_id IN (gümüş ürünler) = 0) istenir ya da migration SQL'i saf testle dize düzeyinde doğrulanır.
- Faz B 'kotasyon değişmediyse yeni snapshot yazılmaz' testi HTTP değil DB olsun (kur zamanlayıcısı HttpOrtami.cs:78'de kaldırılmış); 'Ayarlar.Desen elle sabite döndürülürse' mutasyonu kaynak taramasıyla değil davranışla ölçülür (IsMatch('K23')=false).
- Paket 25 ve 27 ile aynı dalgada olamaz (Entities.cs, RaporModulu.cs, SatisModulu.cs ortak); RaporModulu.StokDegeri:432'deki sınırsız ToListAsync (TB-19) KOPYALANMAZ, GroupBy sunucuda kalır.
