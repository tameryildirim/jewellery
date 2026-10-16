# Paket 39 — Has altın defteri otomatiği (defter dökümü + belge–defter mutabakatı)

> Katman: Katman 6 — Uyum (paket 38–42) · Tahmin: 3–4 (Faz A 2–3, Faz B 1) gün · Önkoşullar: 38, 42 · Kaynak: katman mimarı K6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 5
> ⚠ **VETO:** Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KISMİ VETO — canlı doğrulama adımındaki 'kapanış = stok has toplamı (GET /raporlar/stok-degeri) TUTMALI; tutmuyorsa DUR ve farkı raporla' kabul ölçütüne VETO. İki rakam YAPISAL OLARAK tutmaz: (a) defter EMANET_GIRIS/EMANET_CIKIS satırlarını taşır (Sabitler.HareketTurleri, Entities.cs:24-26) ama stoklar tablosu emaneti taşımaz; (b) StokDegeri yalnız ALTIN has'ı topluyor (RaporModulu.cs:466-489 mühürlü kararı: 'has bir ALTIN kavramıdır') oysa has_grami'nin ham toplamı gümüş/platin satırlarını da içerir. Bu ölçüt bugünkü hâliyle her koşuda sahte alarm üretir ve 'DUR' adımını tetikleyerek paketi kilitler. Veto kalkma şartı: beklenti ÖNCE yazılı olarak tanımlansın (kapanış = stok altın has + emanet net ± listelenmiş bilinen sapmalar) ve has defterinin maden ayrımı (yalnız altın mı, maden kırılımlı mı) ayrıca oylansın — bu karar spesifikasyonda HİÇ geçmiyor.

## Hedef
Kuyumcunun varlığı TL değil HAS GRAMdır; bugün sistem anlık has stoğunu gösteriyor ama 'şu tarihten şu tarihe has nereden girdi, nereye çıktı, açılış neydi, kapanış ne' sorusuna yazılı bir cevap veremiyor. Bu paket has bacağını append-only defterden türeterek tarih sıralı, yürüyen bakiyeli, belge numarası sütunlu bir döküm üretir. Ayrıca iki yönlü mutabakat verir: belgesi olmayan has hareketi ve defterde karşılığı olmayan belge.

## Kodda bugünkü durum
Has bacağı hareketler.has_grami'dir (Entities.cs:281) ve deftere yazmanın tek kapısı HareketFabrikasi.Ekle'dir (HareketFabrikasi.cs:79-115; denetim.sh:58 tek çağrı olduğunu sayar). Yani has defterinin HAM VERİSİ eksiksiz ve tek kaynaklıdır. Eksik olan ÇIKTIDIR: RaporModulu'nda 5 rapor var (GunlukOzet:29, AylikOzet:89, PosFaturaMutabakati:156, CariYaslandirma:286, VadeTakibi:352, StokDegeri:428) ve hiçbiri defter dökümü değil — StokDegeri anlık FOTOĞRAFTIR (stoklar tablosundan), GunlukOzet tek günün NET has hareketini verir; açılış devri, yürüyen bakiye ve satır satır döküm yoktur. Belge bağı yalnız hareketler.fatura_no'dur (Entities.cs:382) ve o bağı kuran TEK yol FaturaServisi.Olustur'un HareketId dalıdır (FaturaModulu.cs:122-144) — ⚠ ÖLÇÜM: 'hareketId' Kuyumcu.Web altında 0 kez geçiyor, yani bugün bağ HİÇ kurulmuyor ve PosFaturaMutabakati her satışı faturasız sayıyor. Ayrıca BankaServisi.FaturayaDonustur fatura üretir ama hareketler'e satır YAZMAZ (BankaModulu.cs:278-303) → o fatura defterde yoktur. ⚠ ÖNEMLİ SINIR: hareketler tablosunda ADET KOLONU YOKTUR (Entities.cs'te 'adet' yalnız stoklar:263'te) → FİZİKİ GRAM defterden türetilemez.

## Kapsam
**İçinde:**
- GET /raporlar/has-defteri — tarih aralıklı, sayfalı, açılış bakiyesi + satır satır giren/çıkan + yürüyen bakiye + kapanış
- Belge sütunu: fatura_no (Entities.cs:382), gider pusulası no (gider_pusulalari.hareket_id üzerinden), irsaliye no (paket 42 geldiyse satır tablosundan)
- İki yönlü mutabakat: (a) belgesi olmayan has hareketi, (b) defterde karşılığı olmayan belge (banka yolundan kesilen faturalar)
- /raporlar/has-defteri ekranı + yazdırılabilir döküm (ADR-0007, ortak BelgeYerlesimi)
- Faz B: has_defteri_donemleri — açılış devri ve dönem kilidi

**Dışında:**
- Fiziki gram sütunu — hareketler.adet olmadığı için TÜRETİLEMEZ; has/urun.HasGrami ile bölerek tahmin ETMEK YASAK (ürün kartı sonradan düzenlenmiş olabilir). Ayrı paket önerisi: 'hareketler.adet' (yeni TB adayı)
- Resmî defter tasdiki, noter, e-Defter (K1/K2)
- Ayar bazlı alt defterler (K22/K18/K14 ayrı defter) — Faz B'nin sonrası, veri hazır (urun.AyarId) ama hareket ayar taşımıyor
- MASAK/vergi denetimine 'resmî defter' olarak sunulma iddiası — belgede böyle bir ibare YER ALMAZ

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| — | Faz A'da HİÇBİR ŞEMA DEĞİŞİKLİĞİ YOK. Defter hareketler'den türetilir; ayrı defter tablosu bilinçli olarak AÇILMAZ (bkz. ortak_kararlar/2). | — | Mevcut (tenant_id, tarih) indeksi yeterlidir (KuyumcuDbContext.cs:109). Aralık sorgusu bu indeksten okur. | — |
| has_defteri_donemleri | YENİ — YALNIZ FAZ B | id VARCHAR(36) PK · tenant_id VARCHAR(36) · yerel_baslangic DATE · yerel_bitis DATE · acilis_has DECIMAL(15,6) · kapanis_has DECIMAL(15,6) · kilitli BOOL · gerekce VARCHAR(500) · kapatan_kullanici_id VARCHAR(36) · olusturuldu DATETIME. Açılış devri defterden TÜRETİLEMEZ (devir öncesi stok, göç) — bir KARARdır, bu yüzden tablo gerekir. Gün kapanışındaki (GunKapanisi) 'yeniden açma kaydı silinmez' ilkesi aynen geçerlidir. | (tenant_id, yerel_baslangic) UNIQUE | YYYYMMDDHHMMSS_HasDefteriDonemleri |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/raporlar/has-defteri?baslangic=YYYY-MM-DD&bitis=YYYY-MM-DD&subeId=&limit=200&offset=0` | SAHIP, MUDUR, MUHASEBE | Tarih aralığı ZORUNLU (TB-19). Yanıt: { acilisHas, satirlar:[{tarih, tur, aciklama, cariAd, urunAd, girenHas, cikanHas, yuruyenBakiye, belgeTuru, belgeNo, hareketId}], donemGirenHas, donemCikanHas, kapanisHas, toplamSatir, eksikBelgeSayisi }. acilisHas TEK SumAsync ile aralıktan ÖNCEKİ tüm satırlardan alınır; defterin tamamı belleğe ALINMAZ. yuruyenBakiye acilisHas'tan başlayıp sayfa içinde yürütülür. | [ModulYetkisi("rapor")] (RaporModulu.cs:512) · kiracı filtresi + elle TenantId şartı |
| GET | `/api/v1/raporlar/has-defteri/mutabakat?baslangic=&bitis=` | SAHIP, MUDUR, MUHASEBE | İki yönlü. (a) belgesizHasHareketleri: has bacağı sıfırdan farklı olup fatura_no'su, gider pusulası ve irsaliyesi olmayan hareketler. (b) defterdeYokBelgeler: faturalar tablosunda olup hiçbir hareketi damgalamamış faturalar (banka yolundan kesilenler buraya düşer). İkisi de SAYI + sayfalı liste döndürür; SUÇLAMA değil, gün sonunda bakılacak listedir (PosFaturaMutabakati:150-152 ile aynı dil). | aynı |
| GET | `/api/v1/raporlar/has-defteri/belge?baslangic=&bitis=&subeId=` | SAHIP, MUDUR, MUHASEBE | Yazdırılabilir döküm verisi (ADR-0007: sunucu VERİ döndürür, render istemcide). İşletme unvanı, şube, dönem, açılış, satırlar, kapanış ve 'bu bir yardımcı dökümdür' ibaresi. Okuma izi düşürür (HurdaModulu.cs:67 deseni). | aynı + DenetimServisi.ListeOkumaKaydet |
| POST | `/api/v1/raporlar/has-defteri/donem (FAZ B)` | SAHIP | Dönem kapatma: açılış devri girilir/onaylanır, kapanış hesaplanır, dönem KİLİTLENİR. Yeniden açma yalnız SAHİP + gerekçe ≥ 20 karakter (GunKapanisi deseni, A9). | kilitli dönem ikinci kez kapatılamaz (benzersiz indeks) |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Rapor/HasDefteriServisi.cs` — RaporModulu.cs deseniyle ayrı servis (RaporServisi 551 satır, büyütülmez — TB-10 karmaşıklık borcu). Defter(baslangic, bitis, subeId, limit, offset), Mutabakat(...), BelgeVerisi(...). Tarih aralıkları Zaman.YerelGunAraligiUtc ile (Zaman.cs:31) — K5 tuzağı. Belge numaraları TEK sorguda toplanır: fatura_no doğrudan hareket satırında; gider pusulası GiderPusulasi.HareketId indeksinden (KuyumcuDbContext.cs:167); irsaliye paket 42 varsa satır tablosundan. N+1 açılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — RaporController'a 3 (Faz B ile 4) uç eklenir (satır 513-551 bölgesi). Servis gövdesi buraya YAZILMAZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Rapor.HasDefteriServisi>(); (satır 22 civarı)
- **YENİ** `dotnet/Kuyumcu.Api/Data/HasDefteriDonemi.cs` — FAZ B. IKiraciVarlik + [Table("has_defteri_donemleri")]. GunKapanisi.cs deseni.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — FAZ B: DbSet + (tenant_id, yerel_baslangic) benzersiz indeksi (satır 126-127 GunKapanisi deseni).

## Ekranlar
- `/raporlar/has-defteri` · `dotnet/Kuyumcu.Web/Pages/HasDefteri.razor` — Tarih aralığı seçici (varsayılan içinde bulunulan ay), şube süzgeci, açılış/kapanış kartları, sayfalı satır tablosu (giren yeşil, çıkan kırmızı, yürüyen bakiye), 'Belgesiz hareket: N' uyarı bandı (tıklayınca mutabakat sekmesi), 'Yazdır' düğmesi → /raporlar/has-defteri/belge sayfası. Sayfalama SUNUCUDAN (sınırsız liste çekilmez). (menü/yetki: KenarCubugu.razor:46-50 bloğuna '⚖️ Has Defteri' eklenir; modül 'rapor'.)
- `/raporlar/has-defteri/belge` · `dotnet/Kuyumcu.Web/Pages/HasDefteriBelgesi.razor` — Yazdırılabilir döküm. Ortak BelgeYerlesimi bileşenini kullanır (paket 42 kurar; 42'den önce sevk edilirse PusulaBelgesi.razor deseni kopyalanır ve 42 geldiğinde taşınır). Belge başlığında 'HAS ALTIN DEFTERİ DÖKÜMÜ (yardımcı kayıt)' — 'resmî defter' iddiası YOK. (menü/yetki: Menüde yok; defter ekranından açılır.)

## İş kuralları (fail-closed)
- Defter TÜRETİLİR, saklanmaz. Tek kaynak hareketler tablosudur; ikinci bir gerçek doğmaz.
- Açılış bakiyesi = aralıktan ÖNCEKİ tüm hareketlerin has toplamı (tek SumAsync). Faz B'de kilitli dönem varsa onun kapanis_has'ı kullanılır ve o değer açıklamasıyla gösterilir.
- Yürüyen bakiye SUNUCUDA hesaplanır. İstemcide hesaplansaydı sayfalamayla birlikte yanlış çıkardı.
- Sıralama (tarih, olusturuldu, id) üçlüsüyle DETERMİNİSTİKTİR. Aynı ms'te yazılan iki satır sayfalar arasında yer değiştirirse yürüyen bakiye iki farklı sayfada iki farklı değer gösterirdi.
- Tarih aralığı yerel gündür (Zaman.YerelGunAraligiUtc). UTC kullanılsaydı İstanbul'da gece yarısından sonraki hareketler bir önceki döneme düşerdi (K5).
- FİZİKİ GRAM SÜTUNU YOKTUR ve ekranda 'hesaplanamıyor' diye AÇIKÇA yazılır. hareketler.adet olmadığı için has/urun.HasGrami bölmesiyle tahmin edilmez — ürün kartı sonradan düzenlenmiş olabilir (UrunModulu.cs:540-553 geçmiş hareketleri güncellemez).
- Belge sütunu boş olan has hareketi 'belgesiz' sayılır ve mutabakatta listelenir. Bu bir SUÇLAMA değildir: transfer, sayım düzeltmesi ve emanet zaten belgesizdir → mutabakat bu türleri AYRI KALEMDE gösterir, 'eksik belge' saymaz.
- Defterde karşılığı olmayan belge listesi banka yolundan kesilen faturaları yakalar (BankaModulu.cs:278-303 hareketler'e yazmaz). Bu bilinen bir açıktır ve rapor onu GÖRÜNÜR kılar; otomatik hareket ÜRETMEZ (uydurma kayıt yazmak defteri bozar).
- Döküm belgesi 'yardımcı kayıt' ibaresi taşır. Resmî defter olduğu iddia edilmez (mevzuat dayanağı doğrulanmadı).
- FAZ B: kilitli dönemin satırları değişemez; ama hareketler zaten append-only olduğu için kilit yalnız AÇILIŞ DEVRİNİ ve kapanış rakamını dondurur.

## Testler
- `dotnet/Kuyumcu.Tests/HasDefteriTestleri.cs` [DB]
  - Alış +has, satış −has, transfer (±0 net), sayım düzeltme → yürüyen bakiye her satırda doğru, kapanış = açılış + giren − çıkan
  - Aralıktan önceki hareketler AÇILIŞ bakiyesine girer, satırlara GİRMEZ
  - İstanbul gecesinde (yerel 00:30) yapılan hareket AYNI yerel güne düşer (GunlukOzet_IstanbulGecesinde… deseni, RaporFaturaTestleri.cs:30)
  - Sayfalama: limit=2 ile 3 sayfa çekilince yürüyen bakiyeler kesintisiz devam eder
  - Aynı anda yazılmış iki hareket iki ayrı koşuda AYNI sırada döner (deterministik sıralama)
  - Fatura damgalı hareket belge sütununda fatura_no gösterir; gider pusulalı hareket GP no gösterir
  - Mutabakat (a): transfer/sayım/emanet 'eksik belge' SAYILMAZ, ayrı kalemde döner
  - Mutabakat (b): banka yolundan kesilen fatura 'defterde yok' listesine düşer
  - Yabancı kiracının hareketleri defterde GÖRÜNMEZ (kiracı izolasyonu)
  - subeId süzgeci yalnız o şubenin satırlarını döndürür ve açılışı da o şubeden hesaplar
- `dotnet/Kuyumcu.Tests/HasDefteriEkranTestleri.cs` [bUnit]
  - Aralık değişince sunucuya YENİ istek gider (istemcide süzme yok)
  - Belgesiz hareket sayısı > 0 iken uyarı bandı görünür
  - Sonraki sayfa düğmesi offset'i sunucuya taşır
  - Fiziki gram sütunu YOKTUR ve yerine açıklama metni vardır
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - Tarih aralığı verilmeden GET → 400 (zorunlu parametre)
  - TEZGAHTAR rolüyle GET → 403 (modül yetkisi 'rapor')

## Mutasyon kanıtları
- Açılış bakiyesi 0 varsayılırsa → 'aralıktan önceki hareketler açılışa girer' KIRMIZI
- Yürüyen bakiye istemciye bırakılır / sayfa başında sıfırlanırsa → 'sayfalama sürekliliği' KIRMIZI
- Sıralamadan olusturuldu/id düşürülürse → 'deterministik sıralama' testi KIRMIZI (iki koşuda farklı sıra)
- Tarih aralığı UTC ile kurulursa → 'İstanbul gecesi' testi KIRMIZI
- Transfer/sayım/emanet de 'belgesiz' sayılırsa → mutabakat (a) testi KIRMIZI
- Banka faturası 'defterde yok' listesinden çıkarılırsa → mutabakat (b) KIRMIZI
- has/urun.HasGrami ile adet türetilip fiziki gram sütunu doldurulursa → ekran testi KIRMIZI (sütun olmamalı)
- Tarih aralığı zorunluluğu kaldırılıp tüm defter çekilirse → HTTP 400 testi KIRMIZI

## Canlı doğrulama
- Yeni kiracı (HASDEF-TEST). Kur kaydını bekle.
- Ürün aç; 10 adet alış (+has), 3 adet satış (−has), 2 adet şubeler arası transfer, 1 sayım düzeltmesi gir.
- GET /raporlar/has-defteri (ayın tamamı) → açılış 0, satırlar ve yürüyen bakiye perl ile bağımsız hesaplananla birebir; kapanış = stok has toplamıyla (GET /raporlar/stok-degeri) TUTMALI. Tutmuyorsa DUR ve farkı raporla.
- Aralığı ikiye böl: ikinci aralığın açılışı birinci aralığın kapanışına EŞİT olmalı.
- limit=2 ile sayfa sayfa gez; yürüyen bakiye kesintisiz mi.
- Satışa fatura kes (paket 38 ile hareketId göndererek) → o satırın belge sütununda fatura_no görünmeli.
- Banka dekontundan fatura kes → mutabakat (b) listesinde görünmeli; defterde satır OLUŞMAMALI (SQL ile hareketler sayısı değişmedi).
- Hurda alışı yap → belge sütununda GP numarası.
- Yazdırma sayfasını aç, tarayıcı yazdırma önizlemesinde kabuk (menü/kenar çubuğu) GÖRÜNMEMELİ.
- Test kiracısını sil (API'yi önce durdur).

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Rapor/HasDefteriServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/HasDefteriDonemi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/HasDefteri.razor`
- `dotnet/Kuyumcu.Web/Pages/HasDefteriBelgesi.razor`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/BelgeYerlesimi.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/belge.css`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/denetim.sh`

## Mevzuat dayanağı
- 'Has altın defteri' adıyla zorunlu bir resmî defter — **VARSAYIM** — PARITE listesindeki ifade budur ama kod tabanında ve bu oturumda ulaşılan hiçbir belgede böyle adlandırılmış bir zorunlu defter DOĞRULANMADI. Bu yüzden çıktı 'yardımcı kayıt / döküm' olarak konumlandırılır ve belgede 'resmî defter' ibaresi YER ALMAZ. Doğrulanırsa ibare ve biçim o zaman uyarlanır.
- VUK md.182-184 (yevmiye, defteri kebir, envanter defteri) ve md.186 (envanter çıkarma) — **DOGRULANMALI** — Bu paket bu defterlerin YERİNE GEÇMEZ; mali müşavirin tuttuğu kayıtlara kaynak olacak bir dökümdür. Hangi defterin karşılığı olduğu K2 (mali müşavir teyidi) ile netleşmeli.
- VUK md.253 — belge ve kayıtların saklanması — **DOGRULANMALI** — Defter zaten append-only hareketler'den türetildiği için saklama kendiliğinden sağlanır (ADR-0004). Yeni bir saklama yükümlülüğü doğmaz.
- Kuyum Ticareti Yönetmeliği — grama/has'a özgü defter hükmü — **DOGRULANMALI** — Kod tabanında kayıtlı önceki ölçüm: 'Kuyum Ticareti Yönetmeliği'nde grama özgü belge hükmü bulunamadı' (SONRAKI-OTURUM-PROMPT.md §A15). Bu paket o ölçümle çelişmez.

## Riskler
- Defter dökümü ile StokDegeri raporu ayrışırsa hangisinin doğru olduğu sorusu doğar. Defter TEK KAYNAKtır; ayrışma varsa stoklar tablosunda bir sapma var demektir ve bu bir BULGUdur, gizlenmez. Canlı doğrulamada bu karşılaştırma zorunlu adımdır.
- Açılış devri olmayan kiracıda (göç öncesi stok) defter 0'dan başlar ve kapanış gerçek stoktan sapabilir. Faz B'nin tek varlık sebebi budur; Faz A bu farkı 'devir bilinmiyor' diye AÇIKÇA gösterir, sıfır saymaz.
- Fiziki gram sütununun olmaması kullanıcı için beklenmedik olabilir (rakip ekranlarda var). Ekranda gerekçesiyle yazılır; kapatma yolu 'hareketler.adet' paketidir.
- Büyük dönemlerde açılış SumAsync'i (tenant_id, tarih) indeksinden okur; hareket sayısı büyüdükçe maliyeti artar. Faz B dönem kilidi bunu sabitler.

## Açık sorular
- Bu döküm hangi resmî defterin karşılığıdır? K2 (mali müşavir) teyidi alınacak mı?
- Ayar bazlı alt defter (K22/K18/K14 ayrı) isteniyor mu? Hareket ayar taşımıyor; taşıması için ayrı iş gerekir.
- Açılış devri kim girer (SAHİP mi mali müşavir mi) ve gerekçe zorunlu mu?
- 'hareketler.adet' kolonu ayrı bir paket olarak açılsın mı? (Paket 39 ve 42 ikisi de bu eksikliğe çarpıyor.)
- Banka yolundan kesilen faturanın deftere hiç yazılmaması kabul mü, yoksa o yol kapatılıp faturanın bir harekete bağlanması mı isteniyor? (Bu bir DAVRANIŞ değişikliğidir, oylama gerekir.)

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Defteri türetme, ayrı defter tablosu açmama kararı ortak karar 1 ve Entities.cs:322-329'daki fiş gerekçesiyle birebir tutarlı — doğru mimari. Açılışın tek SumAsync ile alınması, yürüyen bakiyenin sunucuda hesaplanması ve deterministik sıralama doğru kurulmuş. Banka yolundan kesilen faturanın defterde olmamasını GÖRÜNÜR kılıp otomatik hareket ÜRETMEMESİ en doğru karar. İtirazım canlı doğrulamanın yanlış beklenti kurması ve maden ayrımının hiç konuşulmaması.
  - Koşullar: CANLI DOĞRULAMADAKİ 'kapanış = stok has toplamı (GET /raporlar/stok-degeri) TUTMALI' BEKLENTİSİ YANLIŞ KURULMUŞ ve DÜZELTİLECEK. Defter EMANET_GIRIS/EMANET_CIKIS satırlarını da taşır (Sabitler.HareketTurleri, Entities.cs:24-26) ama stoklar tablosu emaneti taşımaz; ayrıca StokDegeri yalnız ALTIN has'ı topluyor (RaporModulu.cs:466-489) oysa defter tüm madenlerin has_grami'sini toplar. Beklenti ÖNCE yazılı olarak tanımlansın (kapanış = stok altın has + emanet net ± listelenmiş bilinen sapmalar), sonra ölçülsün. Aksi hâlde paket ilk canlı koşuda sahte alarm üretir ve 'DUR ve farkı raporla' adımı her seferinde tetiklenir. · önkoşul [38, 42] → [38]'e DARALTILACAK. İrsaliye numarası belge sütununda opsiyonel bir kaynaktır; 42 gelmeden sütun boş kalır ve paket sevk edilebilir. Gereksiz önkoşul en uzun zinciri üretiyor (41→38→42→39). · DEFTERİN MADEN AYRIMI TANIMLANACAK: 'has' bir ALTIN kavramıdır (RaporModulu.cs:466-489 mühürlü kararı). Has defteri ya YALNIZ altını sayacak ya da maden kırılımlı olacak; hareketler.has_grami'yi maden ayırmadan toplamak o kararı sessizce geri alır. Bu, oylanması gereken bir tasarım kararıdır ve spesifikasyonda HİÇ geçmiyor. · Derin sayfalamada offset yerine keyset (son satırın tarih+olusturuldu+id üçlüsü) kullanılsın ya da offset'e üst sınır konsun. (TenantId, Tarih) indeksi (tarih, olusturuldu, id) sıralamasını tam karşılamaz; büyük dönemde derin offset tablo taraması doğurur (TB-19'un ruhu). · Faz B (has_defteri_donemleri) AYRI SEVK EDİLECEK ve açılış devrini kimin gireceği (SAHİP) + gerekçe zorunluluğu GunKapanisi deseniyle birebir kurulacak. · Yeni uçlar RaporKatalogu'na EKLENECEK (paket 34 ile koordinasyon) ve katalog testi '==19' değil '>=19' olacak — bkz. paket 34 koşulu. · Belge başlığındaki 'yardımcı kayıt' ibaresi ve 'resmî defter değildir' beyanı hem ekranda hem yanıtta dursun; mevzuat dayanağı VARSAYIM olduğu için bu ibare kaldırılabilir bir süs değil, paketin sevk şartıdır.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Salt okur, defter türetiliyor, yeni tablo Faz B'de ve IKiraciVarlik + benzersiz indeksle. 'Resmî defter' iddiası yok (VARSAYIM doğru işaretlenmiş). Kişisel veri (cari adı) dönen uçlarda okuma izi eksik bırakılmış — Faz A'da kapatılmalı.
  - Koşullar: K39-1: has-defteri ve mutabakat uçları cariAd döner → her ikisinde DenetimServisi.ListeOkumaKaydet(islem, nameof(Cari), adet, 'has-defteri') çağrılır (yalnız /belge ucunda değil). Cari HassasVarliklar'da (DenetimIzi.cs:71). · K39-2: HasDefteriController mevcut RaporController'a eklenmiyorsa (ayrı controller) sınıf düzeyinde [Authorize][RolGerekli('SAHIP','MUDUR','MUHASEBE')][ModulYetkisi('rapor')] taşır — RaporModulu.cs:508-512 birebir. · K39-3 (Faz B): POST /has-defteri/donem [RolGerekli('SAHIP')] attribute düzeyinde; yeniden açma gerekçe ≥20 karakter + DenetimServisi.YazmaKaydet (MEVCUT metot, DenetimIzi.cs:120 — YENİDEN YAZILMAZ; çağıranın tx'i içinde, SaveChanges çağırmaz). · K39-4: subeId süzgeci kiracı sınırında IslemDogrulayici.SubeDogrula ile doğrulanır; yabancı şube 404.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Defterin türetilmesi ve K5/TB-19 kuralları mevcut kodla tutarlı (HareketFabrikasi tek kapı, denetim.sh:58; (tenant_id,tarih) indeksi KuyumcuDbContext.cs:109). Mutasyon kanıtlarından biri (deterministik sıralama) test kurgusuyla güvenilir yakalanmıyor ve 42'ye bağımlılığın geri dönüş yolu ADR-0007'yi deler.
  - Koşullar: Önkoşul 42 BAĞLAYICIDIR; 'PusulaBelgesi deseni kopyalanır, 42 gelince taşınır' geri dönüş yolu KALDIRILIR — ADR-0007'nin yasakladığı dördüncü kopyayı üretir ve 'izinsiz değişiklik yok' nedeniyle taşınma hiç yapılmaz. · 'Deterministik sıralama' mutasyonu (olusturuldu/id sıralamadan düşürülürse iki koşuda farklı sıra) MySQL'in fiilen aynı sırayı dönmesi hâlinde yakalanmaz. Test şöyle yazılır: aynı Tarih'li 3 hareket elle yazılır (RaporFaturaTestleri.cs:19-28 deseni, test fikstüründe Hareketler.Add serbest), sayfa limit=1 ile üç kez çekilir ve dönen id'lerin (olusturuldu,id) artan sırada olduğu assert edilir; sıra tersine çevrilirse kırmızı. · DB testine 'kapanisHas == stoklar.toplam_has_gram toplamı (transfer ±0 dahil)' assert'i eklenir — canlı doğrulamadaki 'tutmuyorsa DUR' kuralının otomatik karşılığı; defter ile StokDegeri ayrışması ilk gün yakalansın. · Mutabakat (b) testi BankaServisi.FaturayaDonustur üzerinden gerçek banka faturası keser (BankaModulu.cs:250-310); fatura satırı elle Faturalar.Add ile uydurulmaz. · 'Yabancı kiracının hareketleri defterde GÖRÜNMEZ' çok katmanlı koruma → mutasyon kanıtı değil, regresyon; ikinci katman filtresiz bağlamda ayrıca. · Okuma izi için HasDefteriServisi'ne DenetimServisi enjekte (testte new DenetimServisi(db, new HttpContextAccessor())); belge ucu testinde denetim_kayitlari eylem=OKUMA sayılır. · HasDefteriEkranTestleri: 'Fiziki gram sütunu YOKTUR' assert'i başlık hücrelerinin metnine bakar (Dil sözlüğü anahtarıyla), sabit Türkçe metne değil; olay sonrası WaitForAssertion. · HTTP 'tarih aralığı yok → 400' testi doğru yerde; ek: 'limit=0 → 400' ve TEZGAHTAR 403 (RaporController [RolGerekli] + ModulYetkisi rapor, RaporModulu.cs:509-512).

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO (Bertan, kısmi): 'kapanış = GET /raporlar/stok-degeri TUTMALI, tutmuyorsa DUR' kabul ölçütü YASAK — defter EMANET_GIRIS/CIKIS taşır (Entities.cs:24-26) ama stoklar taşımaz; StokDegeri yalnız ALTIN has'ı toplar (RaporModulu.cs:466-489). Her koşuda sahte alarm.
- VETO KALKMA ŞARTI: beklenti ÖNCE yazılı tanımlanır — kapanış = stok altın has + emanet net ± listelenmiş bilinen sapmalar; ölçüt bu forma göre yeniden yazılır.
- VETO KALKMA ŞARTI 2: defterin MADEN AYRIMI ayrıca oylanır ve karara bağlanır (yalnız altın mı, maden kırılımlı mı) — spesifikasyonda hiç geçmiyor; has_grami'yi maden ayırmadan toplamak mühürlü 'has=altın' kararını sessizce bozar.
- ÇELİŞKİ ÇÖZÜMÜ (Aşkın 'DB testine kapanisHas==stoklar.toplam_has_gram assert'i eklensin' vs Bertan vetosu): assert KALIR ama yeni formülle — altın has + emanet net ± beyan edilen sapmalar; ham eşitlik assert'i yazılmaz.
- ÖNKOŞUL ÇELİŞKİSİ ÇÖZÜMÜ: Bertan [38]'e daraltmak istiyor, Aşkın 42'yi bağlayıcı sayıyor (PusulaBelgesi kopyası ADR-0007'nin yasakladığı 4. kopyayı üretir). Çözüm: Faz A belge ekranı OLMADAN 38 ile sevk edilebilir; /belge ucu 42'nin BelgeYerlesimi'ni BEKLER — kopya çıkarılmaz.
- Erişim + izler: HasDefteriController sınıf düzeyinde [Authorize][RolGerekli(SAHIP,MUDUR,MUHASEBE)][ModulYetkisi("rapor")] (RaporModulu.cs:508-512 birebir); has-defteri ve mutabakat uçları cariAd döndüğü için ikisinde de ListeOkumaKaydet (yalnız /belge'de değil) — Mehmet K39-1/K39-2.
- subeId süzgeci IslemDogrulayici.SubeDogrula ile kiracı sınırında doğrulanır; yabancı şube 404. 'Tarih aralığı yok → 400' + 'limit=0 → 400' + TEZGAHTAR 403 testleri (Mehmet K39-4 + Aşkın).
- Derin sayfalamada keyset (tarih+olusturuldu+id) ya da offset üst sınırı; (TenantId,Tarih) indeksi bu sıralamayı tam karşılamaz. Deterministik sıralama mutasyonu aynı Tarih'li 3 hareketle kurulur (MySQL fiilen aynı sırayı dönebilir).
- Faz B (has_defteri_donemleri) AYRI sevk: POST /has-defteri/donem [RolGerekli("SAHIP")], yeniden açma gerekçe ≥20 karakter + MEVCUT DenetimServisi.YazmaKaydet (DenetimIzi.cs:120) ÇAĞRILIR, yeniden yazılmaz; GunKapanisi deseni birebir (Mehmet K39-3).
- Açılış devri bilinmiyorsa AÇIKÇA 'devir bilinmiyor' gösterilir, sıfır sayılmaz. 'Yardımcı kayıt / resmî defter değildir' ibaresi ekranda ve yanıtta sevk şartıdır. Yeni uçlar RaporKatalogu'na eklenir, katalog testi '>=19'. Mutabakat testi BankaServisi.FaturayaDonustur ile gerçek fatura keser.
