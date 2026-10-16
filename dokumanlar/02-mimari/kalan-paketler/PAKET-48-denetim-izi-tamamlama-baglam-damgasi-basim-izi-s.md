# Paket 48 — Denetim izi tamamlama — bağlam damgası, basım izi, storno motoru

> Katman: Katman 7 — Platform · Tahmin: 3-4 (Faz A ~1 · Faz B ~0,5-1 · Faz C ~1,5-2) gün · Önkoşullar: 33 · Kaynak: katman mimarı K7-48 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 7

## Hedef
Perakende kuyumcuda üç soru bugün cevapsız: (1) bu kaydı HANGİ ŞUBEDE/HANGİ KASADA yapıldı, (2) müşterinin TCKN'sini taşıyan belgeyi KİM BASTI, (3) yanlış girilen bir işçilik/takas/tahsilat fişini defteri bozmadan NASIL geri alırım. Bu paket üçünü de kapatır: denetim satırına şube/kasa damgası, gerçek basım izi ve ters kayıt üreten fail-closed bir storno motoru. Sahip, üç şubeli bir dükkânda 'kim, nerede, neyi' sorusunu tek ekrandan cevaplayabilir hâle gelir.

## Kodda bugünkü durum
Yazma izi otomatik ve unutulamaz (DenetimYazmaDinleyicisi.cs:37-93); append-only kod tarafından zorlanıyor (aynı dosya :51-55 + DenetimIziDegistirilemez :108). Okuma izi açık çağrıyla düşüyor (DenetimIzi.cs:90-106), gerekçeli yazma izi paket 33 ile geldi (DenetimIzi.cs:120-131, çağrısı CariModulu.cs:606). Okuma ucu ve ekran var (DenetimModulu.cs:30-155, Pages/DenetimIzi.razor:1). EKSİKLER kanıtlı: (a) DenetimKaydi'nda sube_id/kasa_id KOLONU YOK (DenetimIzi.cs:28-50) — üç şubeli dükkânda iz 'nerede' sorusunu cevaplamıyor; KasaHareket'te de kullanici_id/sube_id yok (Entities.cs:432-445). (b) BASIM eylemi yok (DenetimEylemi.cs karşılığı DenetimIzi.cs:53-61: yalnız OKUMA/EKLEME/GUNCELLEME/SILME/REDDEDILDI); belge sayfası yazdırırken sunucuya HİÇBİR ŞEY söylemiyor (PusulaBelgesi.razor:159-161 doğrudan window.print). (c) HurdaModulu.cs:67 belge verisi çekilince yanlış metinle 'yazdırıldı' yazıyor. (d) storno kavramı kodda YOK: grep 'storno' üretim kodunda 0 sonuç; tek geri alma yolu iade ve o da yalnız SATIS/ALIS için (SatisModulu.cs:1041, 1150). ISCILIK (SatisModulu.cs:1511), TAKAS (:1431), TRANSFER (:788), HURDA_ALIS (:705), VERESIYE/TAHSILAT (CariModulu.cs:441/503), EMANET_GIRIS/CIKIS (EmanetModulu.cs:97/163) için düzeltme yolu HİÇ YOK.

## Kapsam
**İçinde:**
- Faz A — denetim_kayitlari'na sube_id + kasa_id kolonları, dinleyicide ve DenetimServisi'nde doldurulması, okuma ucunda filtre + yanıt alanı, ekranda sütun ve şube süzgeci
- Faz B — DenetimEylemi.Basim, POST /api/v1/denetim/basim (beyaz listeli belge türü), PusulaBelgesi.razor'un yazdırmadan önce iz düşürmesi, HurdaModulu.cs:67 metninin düzeltilmesi
- Faz C — hareketler.storno_kaynak_id, POST /api/v1/islemler/{hareketId}/storno, fail-closed tür beyaz listesi, yan etkilerin (kasa, cari bakiye, stok, maliyet partisi) ters alınması, gerekçe zorunluluğu, denetim izine gerekçeli yazma
- Storno satırlarının tevsik günlük nakit toplamı ve MASAK kümülatifi dışında bırakılması (çift sayma düzeltmesi)
- Sozlukler.cs TR+EN metinleri, DenetimIzi ekranının BASIM/STORNO süzgeçleri
- Testler: DB + HTTP + bUnit, her kural için mutasyon kanıtı

**Dışında:**
- Storno EKRANI / düğmesi — UX-1 Faz D'nin işidir (K-UX-3); bu paket yalnız ucu verir
- SATIS/ALIS/SATIS_IADE/ALIS_IADE stornosu — iade yolu vardır, ikinci mekanizma açılmaz
- CARI_DUZELTME stornosu — paket 33'ün düzeltme fişi ters delta ile aynı işi yapar
- SAYIM_DUZELTME stornosu — düzeltmesi yeni bir sayım oturumudur
- Denetim izinin dışa aktarımı / imzalı arşiv — ADR-0014 kapsamı, ayrı iş
- Veritabanı düzeyinde değişmezlik (DB kullanıcı yetkisi, WORM) — ADR-0004'ün bilinen sınırı, uygulama katmanı işi değil
- Eski denetim satırlarının şube/kasa ile geriye doldurulması

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| denetim_kayitlari | İki yeni nullable kolon (bağlam damgası). Varlık sınıfı DenetimIzi.cs:26-51. | sube_id VARCHAR(36) NULL — işlemin yapıldığı şube; kasa_id VARCHAR(36) NULL — işlem bir kasaya dokunuyorsa o kasa. NULL = bilinmiyor (eski satır ya da şubesiz sistem işi); sıfır/varsayılan şube UYDURULMAZ. | IX_denetim_kayitlari_tenant_id_sube_id_tarih (tenant_id, sube_id, tarih). Mevcut üç indeks (KuyumcuDbContext.cs:148-150) korunur. | 20260917HHMMSS_DenetimBaglamDamgasi |
| hareketler | Storno bağı. Varlık sınıfı Entities.cs:275. | storno_kaynak_id VARCHAR(36) NULL — bu satırın hangi hareketi hükümsüz kıldığı. iade_kaynak_hareket_id'den (Entities.cs:346) AYRI kolondur ve ikisi aynı satırda asla birlikte dolmaz. | IX_hareketler_tenant_id_storno_kaynak_id (tenant_id, storno_kaynak_id) — 'bu hareket zaten stornolandı mı' sorgusu her storno isteğinde çalışır. | 20260917HHMMSS_HareketStornoKaynagi |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/denetim` | SAHIP | MEVCUT UÇ GENİŞLER (DenetimModulu.cs:143-150): yeni [FromQuery] subeId ve kasaId filtreleri; yanıt satırına subeId, subeAd, kasaId, kasaAd alanları eklenir. Şube/kasa adları tek sorguda çözülür (kullanıcı adlarının çözüldüğü desen, DenetimModulu.cs:57-65). | Sınıf düzeyi [RolGerekli("SAHIP")] korunur; listeleme kendi okuma izini düşürmeye devam eder (:67) |
| GET | `/api/v1/denetim/ozet` | SAHIP | MEVCUT UÇ GENİŞLER: eylem ve varlık kırılımının yanına subeler kırılımı (sube_id → adet) eklenir. BASIM eylemi kırılımda kendiliğinden görünür. | — |
| POST | `/api/v1/denetim/basim` | kimliği doğrulanmış tüm roller | YENİ — basım izi. Gövde: { belgeTuru (beyaz liste), belgeId, kopya (1-10, varsayılan 1) }. 204 döner. Beyaz liste dışı tür → 400 (fail-closed). Kayıt: eylem=BASIM, varlik_turu=belgeTuru, varlik_id=belgeId, detay='basım · kopya N', hassas = belge türü kişisel veri taşıyorsa true. | AYRI controller (DenetimBasimController) — DenetimController'ın sınıf düzeyi SAHİP kapısı altına konmaz; kiracı damgası AktifKullanici'dan, şube damgası islem.SubeId'den |
| POST | `/api/v1/islemler/{hareketId}/storno` | SAHIP, MUDUR | YENİ — ters kayıt üretir. Gövde: { gerekce (≥20, ≤1000), kurSnapshotId (zorunlu), kasaId (kaynak kasadan geçtiyse zorunlu), masak? }. Yanıt: { stornoHareketId, kaynakHareketId, tlTutari, hasGrami, kasaBakiyesi?, cariBakiyesi? }. 409: zaten stornolanmış · kaynağın kendisi storno · tür beyaz listede değil · maliyet partisi güvenle geri alınamıyor. 404: yabancı kiracı ya da yok. | KurDogrula · KasaDogrula (para birimi + gün kilidi, an=now) · TevsikKapisi.Dogrula (nakit çıkış/giriş doğuyorsa) · MasakKapisi.DogrulaKumulatif · VadeKapisi.KrediLimitiDogrula (yalnız borcu ARTIRAN yönde) · DenetimServisi.YazmaKaydet aynı transaction'da |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/DenetimIzi.cs` — DenetimKaydi'na SubeId/KasaId özellikleri (sube_id, kasa_id). DenetimEylemi'ne Basim = "BASIM" sabiti. DenetimServisi.Olustur'a SONDA opsiyonel subeId/kasaId parametreleri — mevcut çağrılar (DenetimYazmaDinleyicisi.cs:88, DenetimIzi.cs:93/102/123) DERLEMEYİ BOZMADAN çalışmaya devam eder. OkumaKaydet/ListeOkumaKaydet/YazmaKaydet islem.SubeId'yi geçirir. YENİ: BasimKaydet(AktifKullanici, string belgeTuru, string belgeId, int kopya) — SaveChanges ÇAĞIRIR (basım izi bağımsız bir işlemdir, çağıranın transaction'ı yoktur). YENİ: BasilabilirBelgeler beyaz listesi (HassasVarliklar.cs desenindeki HashSet, DenetimIzi.cs:67-81) — GiderPusulasi, Fatura, MasakBeyanFormu, SentetikTasSertifikasi, UrunEtiketi, GunKapanisiTutanagi, CariEkstre; hassas olanlar ayrı küme (GiderPusulasi, MasakBeyanFormu, CariEkstre).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs` — Kaydet() içinde tenantId belirlemesinin (:63-68) yanına AÇIK switch ile bağlam çıkarımı: Hareket h => (h.SubeId, null) · Kasa k => (k.SubeId, k.Id) · KasaHareket kh => (null, kh.KasaId) · GunKapanisi g => (g.SubeId, g.KasaId) · Sube s => (s.Id, null) · _ => (null, null). Varlıktan şube çıkmazsa HttpContext'teki 'sube' claim'ine düşülür (AktifKullanici.SubeId ile aynı kaynak). Refleksiyon KULLANILMAZ — tip güvenli switch okunabilir ve yeni varlık eklendiğinde derleyici değil insan karar verir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Denetim/DenetimModulu.cs` — Listele'ye subeId/kasaId parametreleri ve Where'leri; yanıt projeksiyonuna subeId/subeAd/kasaId/kasaAd (adlar tek sorguda Distinct+ToDictionaryAsync ile, :57-65 deseni). Ozet'e subeler kırılımı. YENİ sınıf DenetimBasimController: [ApiController][Authorize][Route("api/v1/denetim/basim")], tek POST, DenetimServisi.BasimKaydet çağırır, 204 döner. SİLME/GÜNCELLEME ucu yoktur notu (:157-158) korunur.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` — :67 gerekçe metni 'gider pusulası belgesi yazdırıldı' → 'gider pusulası belgesi görüntülendi'. Tek satır; yapılmamış eylemin izde yazılı olmasını bitirir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Hareket'e StornoKaynakId (storno_kaynak_id) özelliği + IadeKaynakHareketId (:346) ile farkını anlatan XML yorumu (iade ticari olgu, storno hata düzeltmesi; aynı satırda ikisi birden dolmaz).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki yeni indeks: DenetimKaydi (TenantId, SubeId, Tarih) ve Hareket (TenantId, StornoKaynakId) — mevcut indeks bloğu :148-150 deseni.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs` — Storno motoru. (1) Kaynağı FirstOrDefaultAsync + açık TenantId koşuluyla bulur (Find/FindAsync YASAK, denetim.sh:49). (2) StornolanabilirTurler beyaz listesi: ISCILIK, TAKAS, TRANSFER, HURDA_ALIS, VERESIYE, TAHSILAT, EMANET_GIRIS, EMANET_CIKIS — liste dışı 409, gerekçe mesajı doğru yolu söyler ('satış için iade, cari bakiye için düzeltme fişi'). (3) Çift storno koruması: storno_kaynak_id = kaynak.Id olan satır varsa 409; kaynağın kendi storno_kaynak_id'si doluysa 409. (4) Kapılar sırayla: KurDogrula → (kasa varsa) KasaDogrula(an=now, gün kilidi bugüne bakar) → TevsikKapisi.Dogrula (nakit payı doğuyorsa) → MasakKapisi.DogrulaKumulatif → VadeKapisi.KrediLimitiDogrula (yalnız borcu artıran yön). (5) Ters kayıt HareketFabrikasi.Ekle ile: Tur = kaynak.Tur, TlTutari = -kaynak.TlTutari, HasGrami = -kaynak.HasGrami, StornoKaynakId = kaynak.Id, Aciklama = 'STORNO: ' + gerekçe, KurSnapshotId = dto'dan (yeni kur), SubeId = kaynak.SubeId, KullaniciId = islem.Id. OdemeIzi kaynağın NakitTutar/VeresiyeTutar paylarından ters yönde kurulur; borç azaltan yönde OdemeIzi.VeresiyeKapanisi, borç doğuran yönde Veresiyeden(tutar, VadeHesapla). (6) Yan etkiler ters alınır: kasa (KasaGuncelle -tutar), cari bakiye (CariBakiyeGuncelle ters), stok (StokDegistir ters), maliyet partisi (MaliyetMotoru.PartiIade / PartiTuket ters yön) — geri alınamıyorsa 409, kısmi geri alma YAPILMAZ. (7) DenetimServisi.YazmaKaydet(islem, "Storno", stornoHareketId, gerekçe özeti + kaynak türü, hassas:true) aynı transaction'da, SaveChanges'tan ÖNCE. Hepsi tek BeginTransactionAsync içinde (SatisModulu.cs:1041 deseni).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — Yalnız controller'a tek uç eklenir: [HttpPost("{hareketId}/storno")][RolGerekli("SAHIP","MUDUR")] → StornoServisi'ne delege. İş mantığı bu dosyaya YAZILMAZ (dosya bugün 1500+ satır).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Satis/StornoSuzgeci.cs` — Saf yardımcı: bir IQueryable<Hareket> üzerinden storno çiftlerini (ters satır + hükümsüz kalan kaynak) dışlayan tek Where ifadesi. Tevsik günlük nakit toplamı, MASAK kümülatifi ve adet sayan raporlar bunu çağırır ki bir hata düzeltmesi müşterinin nakit haddini ya da MASAK eşiğini İKİNCİ KEZ doldurmasın.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs` — GunlukNakitToplam sorgusuna StornoSuzgeci uygulanır. ⚠ Bu dosya bu görevde OKUNMADI — uygulayıcı önce okuyup sorgunun gerçek şeklini doğrular, sonra değiştirir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` — DogrulaKumulatif (:114) kümülatif sorgusuna StornoSuzgeci uygulanır. ⚠ Dosya OKUNMADI; aynı doğrulama şartı.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukDenetim'e (:695, TR :1644, EN :2578) yeni required üyeler: Sube, Kasa, Basim, BasimKapsamNotu ('Basım izi yalnız uygulamadaki Yazdır düğmesinden geçen basımları kapsar; tarayıcının kendi yazdırma kısayolu izlenemez'), Storno, StornoGerekce, StornoOnay, StornoYapildi, StornoTurDesteklenmiyor. TR ve EN BLOKLARININ İKİSİ BİRDEN doldurulur — biri eksikse required üye yüzünden DERLENMEZ (güvenlik ağı).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs` — Yalnız gerekiyorsa: basım izi POST'u için mevcut PostAsync kullanılır, yeni metot eklenmez.

## Ekranlar
- `/denetim-izi` · `dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor` — Mevcut ekran genişler: tabloya Şube ve Kasa sütunları (boşsa '—'), filtre satırına #f-sube (şube seçici, /isletme şube listesinden) ve #f-eylem seçeneklerine BASIM. Özet kartlarına şube kırılımı. Mevcut id'ler (#f-baslangic :37, #f-bitis :41, #f-eylem :45, #f-varlik :55) DEĞİŞTİRİLMEZ (UX-S4). (menü/yetki: KenarCubugu.razor:72 mevcut giriş; yalnız SAHİP)
- `/gider-pusulalari/{Id}/belge` · `dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor` — Yazdir() (:159-161) önce POST /denetim/basim { belgeTuru='GiderPusulasi', belgeId=Id, kopya=1 } çağırır; başarılıysa window.print() çalışır, başarısızsa HataBandi ile sunucu mesajı gösterilir ve YAZDIRILMAZ (fail-closed: izsiz basım olmaz). Araç çubuğuna yazdirma-gizle sınıfı içinde BasimKapsamNotu metni. Belge zaten sunucudan veri çekerek açılıyor (Api.Belge, :155) — çevrimdışı basım zaten mümkün değildi, fail-closed yeni bir kısıt getirmiyor. (menü/yetki: Belgeyi görebilen her rol)
- `—` · `dotnet/Kuyumcu.Web/Bilesenler/DuzeltAkisi.razor` — Bu pakette DOKUNULMAZ. UX-1 Faz D, bu paketin storno ucuna bağlanır; ekran sözleşmesi ve id'ler değişmez (K-UX-3). (menü/yetki: —)

## İş kuralları (fail-closed)
- FAIL-CLOSED TÜR: storno yalnız beyaz listedeki türlerde çalışır; bilinmeyen/yeni bir tür eklendiğinde varsayılan REDDETMEKTİR.
- ÇİFT STORNO YASAK: bir hareket en fazla bir kez stornolanır; storno satırının kendisi stornolanamaz.
- DEFTER YERİNDE DEĞİŞMEZ: kaynak satır DOKUNULMAZ (K-UX-2, ADR-0004); hükümsüzlük yalnız ters satırın varlığından okunur. hareketler üzerinde PATCH/DELETE ucu AÇILMAZ.
- GEREKÇE ZORUNLU VE SUNUCUDA ZORLANIR: [Required, MinLength(20), MaxLength(1000)] — istemci kilidi tek kapı sayılmaz (CariDuzeltmeDto deseni, CariModulu.cs:178).
- GÜN KİLİDİ BUGÜNE BAKAR: storno bugünün hareketidir; kaynak günü kapalı olsa bile storno yazılabilir, çünkü kapalı güne satır EKLENMİYOR. Storno anının günü kapalıysa 409 (KasaDogrula an=now).
- KUR UYDURULMAZ: storno yeni bir kurSnapshotId ister; kaynağın kuru KOPYALANMAZ (kaynak kuru bugünün değeri değildir).
- İŞARET SÖZLEŞMESİ: ters satır TlTutari = -kaynak.TlTutari, HasGrami = -kaynak.HasGrami. Toplamı sıfır olmayan bir storno çifti hatadır.
- ÖDEME İZİ SIFIRLANMAZ, TERSLENİR: kaynağın NakitTutar/VeresiyeTutar payları ters yönde OdemeIzi olarak kurulur; HareketFabrikasi'nın işaretsizlik ve 'iz TL bacağını aşamaz' değişmezleri (HareketFabrikasi.cs:81-103) aynen geçerlidir.
- KÜMÜLATİF ÇİFT SAYMAZ: tevsik günlük nakit toplamı ve MASAK kümülatifi storno çiftini dışlar; aksi hâlde bir hata düzeltmesi müşterinin haddini ikinci kez doldurur.
- BASIM TÜRÜ BEYAZ LİSTELİDİR: istemciden gelen serbest metin varlik_turu alanını kirletir ve filtreyi işe yaramaz kılar; liste dışı 400.
- BASIM İZİ YAZILAMAZSA BASILMAZ (istemci tarafı fail-closed) ve bu kapsam sınırı ekranda yazılıdır.
- DAMGA UYDURULMAZ: şube çıkarılamıyorsa NULL yazılır; 'varsayılan şube' atanmaz.
- APPEND-ONLY KORUNUR: yeni kolonlar ve yeni eylem türü DenetimKaydi'nın Modified/Deleted yasağını (DenetimYazmaDinleyicisi.cs:51-55) gevşetmez; ExecuteUpdate/ExecuteDelete beyaz listesi 4'te kalır (denetim.sh:53).
- HAREKETLER.ADD TEK KAPI: storno da HareketFabrikasi.Ekle'den geçer; denetim.sh:58 sayacı 1'de kalır.

## Testler
- `dotnet/Kuyumcu.Tests/DenetimDamgaTestleri.cs` [DB]
  - Hareket yazılınca denetim satırına hareketin şubesi damgalanır
  - Kasa hareketi yazılınca kasa_id damgalanır
  - Gün kapanışı yazılınca hem şube hem kasa damgalanır
  - Varlıkta şube yoksa oturumun şube claim'i kullanılır
  - Şube hiç belirlenemiyorsa NULL yazılır, varsayılan şube ATANMAZ
  - Damga eklenmesi append-only korumasını bozmaz: DenetimKaydi.Modified hâlâ DenetimIziDegistirilemez atar
- `dotnet/Kuyumcu.Tests/DenetimBasimTestleri.cs` [HTTP]
  - POST /denetim/basim geçerli türle 204 döner ve BASIM eylemli satır yazılır
  - Beyaz liste dışı belgeTuru 400 alır ve HİÇBİR satır yazılmaz
  - TEZGAHTAR basım izi yazabilir (204) ama GET /denetim'i okuyamaz (403)
  - Hassas belge türünde (GiderPusulasi) hassas=true yazılır
  - GET /denetim?eylem=BASIM yalnız basım satırlarını döndürür
  - Yabancı kiracının belgeId'si için yazılan satır kendi kiracısına yazılır, sızıntı olmaz
  - Belge ucu (GET /gider-pusulalari/{id}/belge) izinde artık 'görüntülendi' yazar, 'yazdırıldı' YAZMAZ
- `dotnet/Kuyumcu.Tests/StornoTestleri.cs` [DB]
  - İşçilik stornosu: ters satır yazılır, kasa bakiyesi işlem öncesine döner, kaynak satır DEĞİŞMEZ
  - Nakit tahsilat stornosu cari bakiyeyi eski hâline getirir
  - Takas stornosu stok adedini ve maliyet partisini geri koyar
  - Aynı hareket ikinci kez stornolanamaz → 409
  - Storno satırının kendisi stornolanamaz → 409
  - SATIS türü storno isteği 409 alır ve mesaj iade yolunu söyler
  - CARI_DUZELTME storno isteği 409 alır ve düzeltme fişi yolunu söyler
  - Gerekçe 19 karakter → 400; 20 karakter → 200
  - Storno satırı için denetim izinde gerekçeli YAZMA kaydı düşer (varlik_turu=Storno, hassas=true)
  - Storno çifti tevsik günlük nakit toplamına NET 0 katkı yapar (had ikinci kez dolmaz)
  - Storno çifti MASAK kümülatifinde çift sayılmaz
  - Maliyet partisi geri alınamıyorsa 409 ve HİÇBİR yan etki uygulanmaz (transaction geri alınır)
- `dotnet/Kuyumcu.Tests/StornoHttpTestleri.cs` [HTTP]
  - TEZGAHTAR POST storno → 403, defterde satır oluşmaz
  - Yabancı kiracının hareketi → 404
  - kurSnapshotId gönderilmeyince → 400
  - Gün kilidi bugüne kapalıysa → 409
  - Kaynağın günü kapalı ama bugün açıksa storno BAŞARILI (200)
- `dotnet/Kuyumcu.Tests/EkranTestleri.cs (DenetimIzi + PusulaBelgesi bölümleri)` [bUnit]
  - Denetim izi tablosunda Şube sütunu vardır ve boş damga '—' gösterir
  - #f-sube seçilince istek sorgusuna subeId eklenir
  - #f-eylem listesinde BASIM seçeneği vardır
  - Yazdır tıklanınca önce /denetim/basim POST'u gider, sonra window.print çağrılır (sıra doğrulanır)
  - Basım izi POST'u hata dönerse window.print ÇAĞRILMAZ ve HataBandi sunucu mesajını gösterir
  - Basım kapsam notu ekranda görünür ve yazdirma-gizle sınıfındadır

## Mutasyon kanıtları
- Dinleyicideki şube çıkarımı kaldırılır (damga hep null) → DenetimDamgaTestleri.Hareket_yazilinca_sube_damgalanir KIRMIZI
- 'Şube belirlenemezse varsayılan ilk şube yazılır' mutasyonu eklenir → DenetimDamgaTestleri.Sube_belirlenemezse_NULL_yazilir KIRMIZI
- Basım beyaz listesi kaldırılır (her tür kabul) → DenetimBasimTestleri.Beyaz_liste_disi_belge_turu_400 KIRMIZI
- PusulaBelgesi.Yazdir'daki POST kaldırılır → BasimEkranTestleri.Yazdir_once_basim_izi_dusurur KIRMIZI
- Basım POST hatası yutulup print yine çağrılır → BasimEkranTestleri.Basim_izi_yazilamazsa_print_cagrilmaz KIRMIZI
- Çift storno koruması kaldırılır → StornoTestleri.Ayni_hareket_iki_kez_stornolanamaz KIRMIZI
- Tür beyaz listesi kaldırılır (her tür stornolanır) → StornoTestleri.SATIS_storno_istegi_409 KIRMIZI
- Ters satırın işareti pozitif bırakılır → StornoTestleri.Iscilik_stornosu_kasa_bakiyesini_geri_alir KIRMIZI
- StornoSuzgeci tevsik toplamından kaldırılır → StornoTestleri.Storno_cifti_tevsik_haddini_ikinci_kez_doldurmaz KIRMIZI
- Gerekçe DTO'sundaki MinLength(20) kaldırılır → StornoTestleri.Gerekce_19_karakter_400 KIRMIZI (istemci kilidinden BAĞIMSIZ, HTTP yolunda)
- YazmaKaydet çağrısı silinir → StornoTestleri.Storno_denetim_izine_gerekceli_yazilir KIRMIZI
- Kaynak hareket ters satır yerine yerinde güncellenir (TlTutari=0) → StornoTestleri.Kaynak_satir_degismez KIRMIZI + DenetimAppendOnly/KiraciYazma testleri
- Storno yan etkisi kısmi uygulanır (kasa geri alınır, parti alınmaz) → StornoTestleri.Parti_geri_alinamiyorsa_hicbir_yan_etki_uygulanmaz KIRMIZI

## Canlı doğrulama
- Test kiracısı aç; iki şube ve iki kasa tanımla. Şube A'da bir işçilik fişi kes → GET /denetim?subeId=A ile satırın şube damgasını gör; şube B filtresinde GÖRÜNMEDİĞİNİ doğrula.
- Gider pusulası belgesini aç, Yazdır'a bas (yazdırma iletişim kutusunu iptal et) → GET /denetim?eylem=BASIM ile BASIM satırını, kullanıcıyı, şubeyi ve kopya sayısını gör.
- Ağı kes / API'yi durdur, Yazdır'a bas → yazdırma penceresi AÇILMAZ, ekranda hata bandı çıkar (fail-closed kanıtı).
- Nakit işçilik fişini stornola → kasa bakiyesinin işlem öncesi değere döndüğünü GET /kasalar ile, kaynak satırın DEĞİŞMEDİĞİNİ cari ekstresinden, ters satırın storno_kaynak_id taşıdığını denetim/hareket listesinden doğrula.
- Aynı hareketi ikinci kez stornola → 409 ve defterde üçüncü satır OLUŞMADIĞINI doğrula.
- 30.000 TL sınırına yakın bir nakit işlemi yap, stornola, sonra aynı müşteriyle yeni nakit işlem dene → tevsik kapısının storno çiftini saymadığını (haddin ikinci kez dolmadığını) gör.
- Bir satışı stornolamayı dene → 409 ve mesajın iade yolunu söylediğini gör.
- dotnet test 0 başarısız · ./denetim.sh TEMİZ (Hareketler.Add=1, ExecuteUpdate/Delete=4, IgnoreQueryFilters=0, Find=0) · test kiracısını sil.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/DenetimIzi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/ (2 yeni migration + Designer + ModelSnapshot)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Denetim/DenetimModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/StornoSuzgeci.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs (StornoServisi DI kaydı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs (yalnız DI/controller kaydı gerekiyorsa)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs (TR + EN)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DenetimDamgaTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DenetimBasimTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/StornoTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/StornoHttpTestleri.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/EkranTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DenetimIziTestleri.cs (yeni alanlar için genişler)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DenetimAppendOnlyTestleri.cs (regresyon, değişmemeli)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh (sayaçlar değişmemeli — doğrulama)`
- `DOKUNULMAZ: KenarCubugu.razor, UstBar.razor, RotaBasliklari.cs (yeni rota yok), Bilesenler/DuzeltAkisi.razor (UX-1'in)`

## Mevzuat dayanağı
- KVKK md.12 (veri güvenliği / erişim kaydı) — **DOGRULANMALI** — Kod tabanında denetim izinin gerekçesi olarak DenetimIzi.cs:12-13 ve ADR-0004'te dayanak gösteriliyor. Bu görevde maddenin metni doğrulanmadı; basım izinin KVKK'nın AÇIKÇA istediği bir kayıt olduğu İDDİA EDİLMEZ — kişisel veri taşıyan belgenin kim tarafından çoğaltıldığının kaydı, mevcut erişim kaydı yaklaşımının doğal uzantısı olarak tasarlanmıştır.
- VUK md.219 / md.253 (defter kayıtlarının değişmezliği ve saklama) — **DOGRULANMALI** — UX-1 tasarımında (K-UX-2) 'defter dokunulmaz' ilkesinin dayanağı olarak gösteriliyor. Bu görevde madde metni doğrulanmadı. Storno tasarımı bu maddeye değil, ADR-0004 + K-UX-2 mimari kararına dayandırılmıştır — koda hiçbir yasal eşik/hüküm gömülmez.
- 459 s. VUK Genel Tebliği — tevsik zorunluluğu (30.000 TL nakit haddi) — **DOGRULANDI** — Devir belgesindeki A7 araştırmasıyla doğrulanmış sayılıyor. Bu paketteki etkisi YENİ BİR KURAL DEĞİL, mevcut kapının çift saymasının düzeltilmesidir: storno çifti günlük nakit toplamına net 0 katkı yapar.
- MASAK 185.000 TL eşiği + bağlantılı işlem kümülatifi — **DOGRULANDI** — A7 araştırması. Aynı düzeltme: hükümsüz kılınmış işlem eşiğe ikinci kez sayılmaz. Eşik değerleri koda gömülmez, yapılandırmadan okunur (ADR-0005).

## Riskler
- ADET SAYAN RAPORLAR ŞİŞER: tutar toplamları storno ile kendiliğinden doğru olur ama 'kaç işlem yapıldı' sayan raporlar kaynak + ters satırı 2 sayar. StornoSuzgeci bu raporlara UYGULANMAZSA sayılar yanıltır. Azaltma: süzgeç yazılır, raporlara uygulanması paket 34-37 ile koordine edilir; bu paket yalnız tevsik ve MASAK kümülatiflerini düzeltir.
- TEVSIKKAPISI VE MASAKKAPISI OKUNMADI: bu iki dosyanın sorgu şekli bu görevde doğrulanmadı. Uygulayıcı önce okumadan değiştirirse kümülatif hesabı bozabilir — bu iki dosyada değişiklik ayrı bir adım olarak, kendi testleriyle sevk edilmelidir.
- MALİYET PARTİSİ GERİ ALMA: bir takas/hurda alışının partisi sonradan başka bir satışta tüketilmişse storno güvenle geri alınamaz. Tasarım 409 ile durur (fail-closed), ama bu kullanıcıya 'düzeltemiyorum' demektir. Alternatif (bugünkü maliyetle geri koyma) kâr-zararı bozar ve REDDEDİLMİŞTİR.
- BASIM İZİ KAPSAM SINIRI: tarayıcı kısayolu ile basım yakalanmaz. window.onbeforeprint ile kapsam genişletilebilirdi ama o kanal eşzamansız POST'u bekleyemez (fail-open) ve düğme yoluyla ÇİFT KAYIT üretir. Kapsam sınırı yazılı olarak kabul edilmiştir; 'her basım izlenir' iddiası ASLA yapılmamalıdır.
- FAZ C SÜRESİ PARITE TAHMİNİNİ AŞAR: mühürlü liste 2-3 gün diyor; storno motorunun yan etki geri alması (kasa + cari + stok + parti) tek başına o bütçeyi doldurur. Dürüst tahmin 3-4 gündür.
- SUBE CLAIM'İ BOŞ OTURUMLAR: şube seçilmeden açılan oturumlarda damga NULL kalır ve 'kim nerede' sorusu yine cevapsızdır. Şube seçimini zorunlu kılmak paket 46'nın (şube görünürlüğü) işidir; bu paket onu VARSAYMAZ.

## Açık sorular
- EMANET stornosu kapsamda mı? Tasarım EMANET_GIRIS/CIKIS'i beyaz listeye aldı (has bacağı tersine döner, para el değiştirmez) ama UX-1 açık sorusu (satır 163) bunu 'karar verilmedi' bırakmıştı. Oylama onaylamazsa iki tür beyaz listeden çıkarılır — kod değişikliği tek satırdır.
- Fatura IPTAL durumu (FaturaModulu.cs:292) bir storno mudur? Bugün ters kayıt üretmiyor. Bu paket faturaya DOKUNMUYOR; muhasebe açısından yeterli olup olmadığı mali müşavir teyidi ister (UX-1 açık sorusu 164 hâlâ açık).
- Basım izi 'kopya sayısı' istemciden geliyor ve doğrulanamaz (kullanıcı 1 der, 5 basar). Alan tutulsun mu, yoksa yanlış güven verdiği için kaldırılsın mı?
- TRANSFER stornosu iki şubeyi birden etkiler (karsi_sube_id). Karşı şubenin günü kapalıysa ne olacak? Tasarım gün kilidini yalnız kasa üzerinden kontrol ediyor, transferde kasa yok — bu senaryo doğrulanmadı.
- Storno satırı için ayrı bir denetim varlık türü ('Storno') mu yoksa 'Hareket' mi yazılmalı? Tasarım 'Storno' seçti (denetim ekranında süzülebilsin diye, paket 33'ün 'CariDuzeltme' kararıyla aynı gerekçe — DenetimIzi.cs:125-128), ama bu türün HassasVarliklar listesine EKLENMEMESİ gerekir (okuma izlemesi istenmiyor).

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Üç karar da doğru: (a) storno_kaynak_id ayrı kolon — iade hakkı sayacına binmemesi kanıtlı gerekçeye dayanıyor (SatisModulu.cs:1046-1052 iade_kaynak_hareket_id üzerinden kalan hak sayıyor); (b) ters kaydın kaynak TÜRÜNÜ koruması — yeni bir STORNO türü o türü tanımayan her eski raporda stornoyu görünmez kılardı; (c) tek tablo + BASIM eylemi — ikinci iz tablosu append-only korumasını, filtreyi ve KVKK ibraz yolunu ikizlerdi (TB-11 tekrarı). Bağlam damgasının geriye doldurulmaması dürüst.
  - Koşullar: TevsikKapisi.cs ve MasakKapisi.cs bu görevde OKUNMADI (spec kendisi söylüyor). StornoSuzgeci'nin bu iki kapıya uygulanması AYRI bir adım olarak, dosyalar okunduktan sonra ve kendi testleriyle sevk edilecek; kümülatif hesabı kör değiştirilmeyecek. · Storno denetim kaydı (DenetimServisi.YazmaKaydet) aynı transaction'da yazılırsa, geri alınan bir stornoda 'reddedildi/denenmiş' izi de kaybolur. ADR-0004 ruhu gereği en azından REDDEDİLEN storno denemesi ayrı bağlamda kaydedilmeli. · Faz C'nin PARITE tahminini (2-3 gün) aştığı kabul edilmeli: yan etki geri alma (kasa + cari + stok + maliyet partisi) tek başına o bütçeyi doldurur. Dürüst tahmin 3-4 gün; 'sığar' denip sıkıştırılmasın. · Adet sayan raporların storno çiftini 2 sayacağı gerçeği paket 34-37 ile koordine edilmeden Faz C kapanmış sayılmaz; bu pakette yalnız tevsik ve MASAK düzeltiliyor, kalanı açık borç olarak envantere yazılacak. · HurdaModulu.cs:67'deki 'yazdırıldı' metninin 'görüntülendi' yapılması Faz A'ya alınsın (tek satır, izdeki yanlış beyanı bitiriyor). · Önkoşul 33 (manuel düzeltme fişi) YARIM; Faz C storno beyaz listesinden CARI_DUZELTME'nin dışlanması 33'ün tamamlanmasına bağlı.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Storno beyaz listesi fail-closed, ikinci mekanizma yok, defter yerinde değişmiyor, iz gerekçeli. Basım izi 'en iyi çaba' sınırı dürüst. Koşullar sahte iz kirletme ve kapalı gün sonrası defter değişimiyle ilgili.
  - Koşullar: Basım ucu: belgeId'nin AYNI kiracıda ve beyaz listedeki türde GERÇEKTEN var olduğu doğrulansın (yoksa 404) — aksi hâlde herhangi bir kullanıcı sahte belge kimliğiyle denetim izini kirletir. Test: 'olmayan belgeId → 404, satır yazılmaz'. · TevsikKapisi.cs ve MasakKapisi.cs OKUNMADI — StornoSuzgeci uygulanmadan önce okunup sorgu şekli rapora yazılır; değişiklik kendi testleriyle ayrı adım. · Kaynak günü KAPALI olan hareketin stornosu: yalnız SAHIP + denetim izinde 'kapalı gün stornosu' işareti + gerekçe; MUDUR'a kapalı. Gün kapanış tutanağı imzalandıktan sonra defterin net toplamı değişiyor — sessiz olmaz. · StornoSuzgeci kümülatiften storno çiftini dışlarken YALNIZ (kaynak, ters) çiftini dışlasın; tek taraflı ('storno_kaynak_id dolu olanı at, kaynağı bırak') yazılırsa kümülatif ŞİŞER değil AZALIR — mutasyon kanıtı iki yönlü. · Storno satırı HareketFabrikasi.Ekle'den (denetim.sh:58 sayacı 1) + OdemeIzi ters; KasaDogrula(an=now) ile K6 + gün kilidi; kur snapshot yeni; gerekçe ≥20 servis içinde elle. · Damga (sube_id/kasa_id) yoksa NULL — varsayılan şube atanmaz; append-only koruması (DenetimYazmaDinleyicisi.cs:51-55) yeni kolonlarla gevşemez, regresyon testi. · DenetimBasimController AYRI sınıf (DenetimController sınıf düzeyi SAHIP — DenetimModulu.cs:137 doğrulandı).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Tek tablo, storno≠iade ayrı kolon ve tür koruma kararları kanıtlı; mutasyon listesi geniş. Ancak iki kapı dosyası okunmadan değişiklik planlanmış, süzgeç testi simetrik olduğu için mutasyonu yakalamayabilir ve TRANSFER senaryosu tanımsız.
  - Koşullar: TevsikKapisi.cs ve MasakKapisi.cs OKUNMADI (spesifikasyon kabul ediyor); StornoSuzgeci bu iki kümülatife uygulanmadan ÖNCE mevcut TevsikTestleri/MasakTestleri'nin listesi çıkarılıp 'süzgeç eklendikten sonra 0 başarısız' kabul ölçütü yazılsın; bu iki dosya değişikliği ayrı adım olarak sevk edilsin. · StornoTestleri 'Storno çifti tevsik günlük nakit toplamına NET 0 katkı yapar' tek başına yetersiz: mutasyon 'StornoSuzgeci kaldırılır' hem kaynak hem ters satır nakit ise toplam zaten sıfırlanabilir (−t + t). Süzgecin ölçülmesi için 'kaynak nakit satış 25.000, storno, sonra 10.000 nakit → kapı AÇIK (kümülatif 10.000) ; süzgeçsiz 35.000 sayılır ve 400' biçiminde asimetrik test yazılsın. · Basım izi bUnit testi 'önce POST sonra window.print (sıra doğrulanır)' JSInterop.Strict ile ve çağrı sırası listesiyle ölçülsün; ayrıca 'POST hata dönerse print çağrılmaz' testinde sahte API 500 döndürsün. · Faz C süresi PARITE'yi aşıyor (3-4 gün dürüst tahmin); Faz A/B tek başına sevk edilebilir, Faz C ayrı oy. · TRANSFER stornosu iki şubeyi etkiler ve gün kilidi kasa üzerinden çalışmıyor (açık soru) — TRANSFER Faz C beyaz listesinden ÇIKARILSIN ya da 'karşı şube günü kapalı → 409' testi eklensin; belirsiz bırakılamaz. · Önkoşul 33 (A17 yarım) — 48 Faz C, 33 sevk edilmeden başlamaz.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- TevsikKapisi.cs ve MasakKapisi.cs bu görevde OKUNMADI (spec kabul ediyor): StornoSuzgeci bu iki kümülatife uygulanmadan önce dosyalar okunup sorgu şekli rapora yazılır; değişiklik AYRI adım olarak kendi testleriyle sevk edilir, mevcut TevsikTestleri/MasakTestleri 0 başarısız kabul ölçütüdür.
- StornoSuzgeci testi ASİMETRİK yazılır: 'nakit satış 25.000 → storno → 10.000 nakit ⇒ kapı AÇIK (kümülatif 10.000); süzgeçsiz 35.000 sayılır ve 400'. Simetrik (−t + t) test mutasyonu yakalamaz. Süzgeç yalnız (kaynak, ters) ÇİFTİNİ dışlar; tek taraflı dışlama kümülatifi azaltır — mutasyon iki yönlü.
- Basım ucu: belgeId'nin AYNI kiracıda ve beyaz listedeki türde gerçekten var olduğu doğrulanır, yoksa 404 — sahte belge kimliğiyle denetim izi kirletilemez. Test: 'olmayan belgeId → 404, satır yazılmaz'. DenetimBasimController AYRI sınıf (DenetimController sınıf düzeyi SAHIP, DenetimModulu.cs:137).
- Kaynak günü KAPALI hareketin stornosu: yalnız SAHIP, gerekçe zorunlu, denetim izinde 'kapalı gün stornosu' işareti; MUDUR'a kapalı. Gün kapanış tutanağından sonra defter net toplamı sessizce değişemez.
- Reddedilen/denenmiş storno kaydı AYRI bağlamda yazılır (aynı transaction'da yazılırsa geri alınan stornoda iz de kaybolur — ADR-0004; paket 46 ile aynı gerilim, aynı çözüm).
- Storno satırı HareketFabrikasi.Ekle üzerinden (denetim.sh:58 sayacı 1) + OdemeIzi ters; KasaDogrula(an=now) ile K6 + gün kilidi; kur snapshot yeni; gerekçe ≥20 karakter servis içinde elle doğrulanır.
- Bağlam damgası yoksa NULL kalır — varsayılan şube ATANMAZ (şube zorunluluğu paket 46'nın işi, VARSAYILMAZ). Append-only koruması (DenetimYazmaDinleyicisi.cs:51-55) yeni kolonlarla gevşemez; regresyon testi zorunlu.
- TRANSFER stornosu iki şubeyi etkiler ve gün kilidi kasa üzerinden çalışmıyor: ya Faz C beyaz listesinden ÇIKARILIR ya da 'karşı şube günü kapalı → 409' testi eklenir; belirsiz bırakılamaz.
- HurdaModulu.cs:67'deki 'yazdırıldı' metni 'görüntülendi' yapılması Faz A'ya alınır (tek satır, izdeki yanlış beyanı bitirir). 'Her basım izlenir' iddiası ASLA yapılmaz; kapsam sınırı yazılı kabul edilmiştir.
- Faz C süresi PARITE'nin 2-3 gününü aşar; dürüst tahmin 3-4 gün, sıkıştırılmaz — Faz A/B tek başına sevk edilebilir, Faz C ayrı oy. Adet sayan raporların storno çiftini 2 sayması paket 34-37 ile koordine edilir, kalanı açık borç envanterine yazılır.
