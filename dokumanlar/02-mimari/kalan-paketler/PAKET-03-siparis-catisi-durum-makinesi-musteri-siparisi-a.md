# Paket 3 — Sipariş çatısı (durum makinesi) — müşteri siparişi · atölye siparişi · tamir-servis takibi

> Katman: Katman 0 — Altyapı · yalnız paket 3 (Sipariş / iş emri çatısı — perakende bacağı) · Tahmin: 5-7 gün (Faz A 3-4 gün, Faz B 2-3 gün) — tek geliştirici, ±%40 gün · Önkoşullar: 4 · Kaynak: katman mimarı K0-3 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 2

## Hedef
Perakende kuyumcunun bugün deftere hiç girmeyen üç sözünü kayda alır: vitrinde olmayan malı müşteriye söz vermek, atölyeye mal ısmarlamak ve tamire alınan eşyayı takip etmek. Bugün bunlar not defterinde tutuluyor; 'söz verilen gün' geçtiğinde kimse görmüyor ve tamire alınan müşteri altını kaybolduğunda kanıt kalmıyor. Paket, para hareketi doğurmadan bir durum makinesi ve append-only geçiş kaydı kurar; ödeme ve teslim hâlâ mevcut defter uçlarından geçer.

## Kodda bugünkü durum
Sipariş kavramı kodda YOK: Modules/ altında Siparis klasörü yok (ls çıktısı), Data/ altında sipariş varlığı yok. Tek iz paket 4'ün kapalı listesidir: Data/UrunBelgesi.cs:68 BelgeSahipTuru.Siparis = "SIPARIS" tanımlı, Modules/Urun/BelgeModulu.cs:93-96 bu dalı AÇIKÇA reddediyor ("Sipariş modülü henüz yok") ve bu ret dotnet/Kuyumcu.Tests/BelgeDeposuTestleri.cs:137 testiyle kilitli. Ayrıca BelgeGeriDoldurmaTestleri.cs:92-98 SIPARIS sahip türünü fikstürde kullanıyor. Atölye zaten cari türüdür (Entities.cs:17 CariTurleri içinde ATOLYE) ve ondan gelen mal POST /islemler/alis ile giriliyor (SatisModulu.cs:1726). Fiyat teklifi ucu hazır (Modules/Fiyat/FiyatModulu.cs:244 GET /fiyat/teklif). Yetki matrisinde sipariş anahtarı yok (YetkiModulu.cs:22-23 sekiz modül sayıyor).

## Kapsam
**İçinde:**
- Faz A: siparisler + siparis_kalemleri + siparis_gecisleri tabloları, tek migration
- Faz A: SiparisDurumMakinesi (saf sınıf) — tür bazında izinli geçiş tablosu, fail-closed
- Faz A: sipariş oluştur / listele / detay / taslak düzenle / durum geçişi / geçiş dökümü uçları
- Faz A: /siparisler ekranı (liste + süzgeç + yeni sipariş + geçiş) ve /siparisler/{id} detay ekranı
- Faz A: yeni yetki modülü anahtarı 'siparis' (Moduller.Tumu + Varsayilan), [RolGerekli] + [ModulYetkisi]
- Faz B: paket 4 belge bağlama — BelgeSahipTuru.Siparis dalının açılması (tamir fotoğrafı, ürün görseli)
- Faz B: hareket referansları — kapora, teslim ve tamirde alınan emanet hareketinin siparişe iliştirilmesi (yalnız referans, yazma yok)
- Faz B: geciken/yaklaşan sipariş sorgu ucu ve durum özeti (patron paneli ve paket 6 için veri kaynağı)

**Dışında:**
- İş emri, fire, usta ataması, atölye kasası, BOM (Katman 1 — kurucu kapsam düzeltmesi 2026-09-14)
- Stok rezervasyonu / ayırma (gerekçesi ortak kararlarda)
- Sipariş üzerinden doğrudan satış/tahsilat yazma — deftere yazma tekeli korunur
- Sipariş makbuzunun yazdırılabilir belgesi (paket 5 / BelgeYerlesimi bileşenine bırakıldı)
- Müşteriye SMS/WhatsApp/e-posta ile 'siparişiniz hazır' bildirimi (C sınıfı, sözleşme gerekir)
- CRM teklifi / fiyat teklifi belgesi (PARITE'de paket 3'ün 'açtıkları' arasında sayılıyor ama ayrı iştir)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| siparisler | YENİ | id (varchar36, ULID) · tenant_id · sube_id (zorunlu) · tur (MUSTERI_SIPARISI|ATOLYE_SIPARISI|TAMIR, kapalı liste) · cari_id (zorunlu; MUSTERI_SIPARISI ve TAMIR için MUSTERI/HER_IKISI, ATOLYE_SIPARISI için ATOLYE/TEDARIKCI/HER_IKISI) · no (kiracı içinde tekil, insan okur: S-2026-000123) · durum (TASLAK|ONAYLANDI|ATOLYEDE|HAZIR|TESLIM_EDILDI|IPTAL) · soz_verilen_gun (date, null olabilir) · aciklama (varchar1000) · tahmini_tutar_tl decimal(15,2) null · kur_snapshot_id null (tahmini tutar doluysa ZORUNLU) · kapora_hareket_id null (Faz B) · teslim_hareket_id null (Faz B) · emanet_hareket_id null (Faz B, TAMIR) · olusturan_kullanici_id · olusturuldu · guncellendi | (tenant_id, durum, soz_verilen_gun) · (tenant_id, cari_id) · (tenant_id, sube_id, durum) · (tenant_id, no) UNIQUE | YYYYMMDDHHMMSS_SiparisCatisi |
| siparis_kalemleri | YENİ | id · tenant_id · siparis_id · sira (int) · urun_id null (kartı olan mal) · tanim (varchar300, urun_id boşsa ZORUNLU) · ayar_id null (Ayarlar.Bul ile doğrulanır) · adet decimal(12,3) · gram decimal(15,6) null · tahmini_iscilik_tl decimal(15,2) null · tahmini_tutar_tl decimal(15,2) null · not (varchar500) | (tenant_id, siparis_id, sira) | aynı migration |
| siparis_gecisleri | YENİ (append-only) | id · tenant_id · siparis_id · onceki_durum null (ilk kayıtta null) · yeni_durum · gerekce (varchar500; IPTAL ve geri geçişte ZORUNLU, ≥10 karakter) · hareket_id null · kullanici_id · tarih (utc) | (tenant_id, siparis_id, tarih) | aynı migration |
| KuyumcuDbContext | DbSet<Siparis>/<SiparisKalemi>/<SiparisGecisi> + yukarıdaki indeksler OnModelCreating içine; global kiracı filtresi üç varlığa otomatik uygulanır (IKiraciVarlik zorunlu, kapsam denetimi KuyumcuDbContext.cs:194-201) | - | yukarıda | aynı migration |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/siparisler` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Sipariş oluşturur; en az bir kalem zorunlu, durum TASLAK doğar ve ilk geçiş kaydı (onceki=null) yazılır. Sipariş no sunucuda üretilir. | kiracı filtresi · cari aynı kiracıda ve tür uyumlu · şube doğrulaması · kur_snapshot_id doğrulaması (tahmini tutar varsa) · para kapısı YOK (para hareketi doğmaz) |
| GET | `/api/v1/siparisler?tur=&durum=&cariId=&subeId=&geciken=&limit=` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Liste; varsayılan limit 50, azami 200, sunucuda Take; toplam sayı ve durum kırılımı sunucuda GroupBy ile hesaplanır (TB-19). | sınırsız ToListAsync YASAK · geciken süzgeci Zaman.YerelBugun() ile |
| GET | `/api/v1/siparisler/{id}` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Tam kart: başlık + kalemler + geçiş geçmişi + (Faz B) bağlı belge özetleri. | başka kiracının kimliği verilirse 404 (varlık sızdırılmaz) |
| PATCH | `/api/v1/siparisler/{id}` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | YALNIZ TASLAK durumundaki siparişin kalemleri, söz verilen günü ve açıklaması değiştirilir. TASLAK dışında 400. | durum kapısı fail-closed · kalem tümüyle değiştirilir (sil+yaz), kalem id'si dışarıdan gelmez |
| POST | `/api/v1/siparisler/{id}/gecis` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR — IPTAL yalnız SAHIP, MUDUR | Gövde: {yeniDurum, gerekce?, hareketId?}. SiparisDurumMakinesi izinli demezse 400; izinliyse durum güncellenir ve siparis_gecisleri'ne satır yazılır (aynı transaction). | durum makinesi · gerekçe zorunluluğu · TESLIM_EDILDI için hareketId zorunluluğu (Faz B) · hareketId aynı kiracıda olmalı |
| GET | `/api/v1/siparisler/{id}/gecisler` | SAHIP, MUDUR, MUHASEBE | Append-only geçiş dökümü (kim, ne zaman, hangi gerekçe, hangi hareket). | salt okuma |
| GET | `/api/v1/siparisler/ozet` | SAHIP, MUDUR, MUHASEBE | Durum bazlı sayılar + geciken sayısı (patron paneli/paket 6 kaynağı). Faz B. | sunucuda GroupBy, satır dönmez |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Data/Siparis.cs` — Siparis, SiparisKalemi, SiparisGecisi varlıkları (üçü de IKiraciVarlik) + SiparisTuru, SiparisDurumu kapalı listeleri. Desen: Data/UrunBelgesi.cs (aynı dosyada varlık + statik kapalı liste sınıfları). Entities.cs'e DOKUNULMAZ.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisDurumMakinesi.cs` — Saf statik sınıf. Izinli(tur, mevcut, yeni) -> bool ve GerekceGerekli(mevcut, yeni) -> bool. Tablo: MUSTERI_SIPARISI: TASLAK->ONAYLANDI|IPTAL; ONAYLANDI->ATOLYEDE|HAZIR|IPTAL; ATOLYEDE->HAZIR|IPTAL; HAZIR->TESLIM_EDILDI|IPTAL; TESLIM_EDILDI ve IPTAL TERMİNAL (hiçbir çıkış yok). ATOLYE_SIPARISI: TASLAK->ONAYLANDI|IPTAL; ONAYLANDI->ATOLYEDE|IPTAL; ATOLYEDE->HAZIR|IPTAL; HAZIR->TESLIM_EDILDI|IPTAL. TAMIR: TASLAK->ONAYLANDI|IPTAL; ONAYLANDI->ATOLYEDE|HAZIR|IPTAL; ATOLYEDE->HAZIR|IPTAL; HAZIR->TESLIM_EDILDI|IPTAL. Bilinmeyen tür/durum -> false (fail-closed).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` — Tek dosya: DTO'lar + SiparisServisi + SiparisController. Desen: Modules/Emanet/EmanetModulu.cs (DTO + servis + controller aynı dosyada, servis içinde elle doğrulama). Controller: [Authorize] [Route("api/v1/siparisler")] [Yetki.ModulYetkisi("siparis")], yazma uçlarında [RolGerekli(...)] TAM eşleşme (desen: BelgeModulu.cs:218-219). Servis: IslemDogrulayici.CariDogrula + KurDogrula kullanır, kimlikleri Kimlik.Ulid() ile üretir, geçişi db.Database.BeginTransactionAsync içinde yazar.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisNo.cs` — Kiracı içinde tekil, insan okunur numara üretimi (S-YYYY-NNNNNN). Eşzamanlılık: (tenant_id, no) UNIQUE indeks + çakışmada tek yeniden deneme; sayaç tablosu AÇILMAZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Siparis.SiparisServisi>(); satırı eklenir (dosyanın tek dokunma noktası, Program.cs değişmez).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` — Moduller.Tumu'ya "siparis" (satır 22-23) ve Moduller.Varsayilan'daki üç role de "siparis" (satır 29-34) eklenir. Gerekçe: tezgâhtar sipariş alır; sipariş kişisel veri taşımaz (cari_id referansıdır), bu yüzden MASAK/gider pusulası kısıtı uygulanmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — Üç DbSet + veri modelindeki indeksler. Kapsam denetimi (satır 194-201) sayesinde IKiraciVarlik unutulursa uygulama açılmaz.
- **DEGISTIR (Faz B)** `dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs` — SahipDogrula içindeki case BelgeSahipTuru.Siparis dalı (satır 93-96) açılır: db.Siparisler.AnyAsync(s => s.Id == sahipId && s.TenantId == islem.TenantId) değilse NotFoundException("Sipariş bulunamadı"). Ret mesajı ve onun testi birlikte güncellenir.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukNav'a Siparisler (satır 50 bloğu) + yeni SozlukSiparis sınıfı; Tr (1049/1120 bloğu) ve En (1942/2054 bloğu) BİRLİKTE doldurulur, yoksa 'required' alan eksikliğinden derlenmez. Tür ve durum etiketleri sözlükten okunur, ekranda sabit metin yazılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — @if (Yetki.Erisebilir("siparis")) bloğu içinde <NavLink href="/siparisler">. Ekran gizlemesi tek kapı sayılmaz (satır 54-56 yorumu); sunucu [ModulYetkisi] ile ayrıca kapalıdır.
- **DEGISTIR** `dotnet/denetim.sh` — BEKLENEN_VARLIK alt sınırı üç yeni varlık kadar yükseltilir (satır 66). Hareketler.Add sayacı 1, IgnoreQueryFilters 0, Find/FindAsync 0, ExecuteUpdate 4 DEĞİŞMEZ — değişiyorsa paket kendi ortak kararını ihlal etmiştir.

## Ekranlar
- `/siparisler` · `dotnet/Kuyumcu.Web/Pages/Siparisler.razor (YENİ)` — Tür sekmeleri (müşteri siparişi / atölye siparişi / tamir) + durum süzgeci + 'geciken' anahtarı; liste satırında no, cari, söz verilen gün, durum rozeti. Yeni sipariş formu: cari seçimi, kalem satırları (ürün seç ya da serbest tanım + ayar + gram + adet), söz verilen gün, tahmini tutar için GET /fiyat/teklif çağrısı (fiyat İSTEMCİDE hesaplanmaz — PosBarkodTestleri.cs:156-169 kararı). Durum geçişi düğmeleri yalnız durum makinesinin izin verdiklerini gösterir; iptalde gerekçe kutusu zorunlu. (menü/yetki: Yetki.Erisebilir("siparis") · sunucuda [ModulYetkisi("siparis")])
- `/siparisler/{id}` · `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor (YENİ)` — Tam kart: başlık, kalem tablosu, append-only geçiş zaman çizelgesi (kim/ne zaman/gerekçe), bağlı hareket referansları ve (Faz B) belge yükleyici bileşeni. Yazdırma yok — sipariş makbuzu paket 5'e bırakıldı. (menü/yetki: aynı; doğrudan menüde görünmez, listeden açılır)

## İş kuralları (fail-closed)
- Durum geçişi yalnız SiparisDurumMakinesi.Izinli() true dönerse yapılır; bilinmeyen tür/durum false döner (fail-closed).
- TESLIM_EDILDI ve IPTAL TERMİNAL durumlardır; bu durumdaki siparişte hiçbir alan değişmez, yalnız okunur.
- IPTAL ve her geri yönlü geçiş için gerekçe ZORUNLU (≥10 karakter); gerekçesiz iptal 400.
- Her durum değişikliği siparis_gecisleri'ne bir satır yazar ve aynı transaction'dadır; geçiş satırı UPDATE/DELETE edilmez (ADR-0004 ruhu).
- Sipariş modülü deftere yazmaz: HareketFabrikasi.Ekle çağrısı ve Hareketler.Add YOKTUR (denetim.sh:58 sayacı 1 kalır).
- Faz B: TESLIM_EDILDI geçişi hareketId ister; verilen hareket aynı kiracıda olmalı ve türü MUSTERI_SIPARISI/TAMIR için SATIS|ISCILIK|TAKAS, ATOLYE_SIPARISI için ALIS|TAKAS olmalıdır. 'Teslim edildi' denen ama defterde karşılığı olmayan sipariş kabul edilmez.
- Faz B: kapora hareket referansı yalnız OdemeTuru NAKIT|KART|HAVALE olan ve aynı cariye ait bir hareket olabilir; sipariş kapora TUTARI tutmaz — tek gerçek defterdir.
- TAMIR türünde müşteri eşyası deftere alınacaksa mevcut EMANET_GIRIS ucundan geçer (EmanetModulu.cs) ve emanet_hareket_id ile iliştirilir; sipariş modülü has bacağına yazmaz.
- Kalem: urun_id boşsa tanim zorunlu; ikisi de boşsa 400. ayar_id verilirse Ayarlar.Bul ile doğrulanır (EmanetModulu.cs:32 deseninin aynısı).
- tahmini_tutar_tl doluysa kur_snapshot_id zorunludur ve IslemDogrulayici.KurDogrula'dan geçer — hangi kurdan söz verildiği sonradan sorulur.
- Sipariş stok gerçeğini değiştirmez: stoklar tablosuna, stok partilerine ve kâr-zarara hiçbir etkisi yoktur.
- Liste uçları sınırsız ToListAsync yapmaz; limit azami 200, toplamlar sunucuda (TB-19).
- Sipariş no kiracı içinde tekildir ve veritabanı UNIQUE indeksiyle zorlanır; iki eşzamanlı istek aynı numarayı alamaz.

## Testler
- `dotnet/Kuyumcu.Tests/SiparisDurumMakinesiTestleri.cs` [saf]
  - İzinli geçiş listesi tür bazında birebir doğrulanır (üç tür × tüm durum çiftleri)
  - TESLIM_EDILDI'den hiçbir çıkış yok
  - IPTAL'den hiçbir çıkış yok
  - Bilinmeyen tür/durum false döner (fail-closed)
  - IPTAL ve geri geçişte GerekceGerekli true
- `dotnet/Kuyumcu.Tests/SiparisTestleri.cs` [DB]
  - Sipariş oluşturunca TASLAK doğar ve ilk geçiş satırı (onceki=null) yazılır
  - Kalemsiz sipariş reddedilir
  - urun_id ve tanim ikisi de boşsa reddedilir
  - tahmini tutar var ama kur yoksa reddedilir
  - Yasak geçiş (TASLAK -> TESLIM_EDILDI) 400
  - IPTAL gerekçesiz reddedilir, gerekçeliyse geçiş satırı gerekçeyle yazılır
  - TESLIM_EDILDI sonrası PATCH reddedilir
  - Başka kiracının siparişi 404 (kiracı izolasyonu)
  - Sipariş yazımı hiçbir hareket/kasa_hareketi satırı üretmez (defter sayımı önce=sonra)
  - Faz B: teslim geçişi hareketId olmadan reddedilir; yabancı kiracının hareketiyle de reddedilir
  - Faz B: SIPARIS sahip türüyle belge bağlanabilir, olmayan sipariş kimliğinde 404
- `dotnet/Kuyumcu.Tests/HttpSiparisTestleri.cs` [HTTP]
  - TEZGAHTAR sipariş oluşturabilir, iptal edemez (403)
  - Yetki matrisinden 'siparis' kaldırılmış rol 403 alır
  - limit=1000 istense de en çok 200 satır döner
  - JSON sözleşmesi camelCase, durum ve tür kapalı liste değerleriyle döner
- `dotnet/Kuyumcu.Tests/SiparisEkranTestleri.cs` [bUnit]
  - Liste POST /siparisler gövdesini doğru kurar (kalem dizisi, söz verilen gün)
  - Fiyat ekranda hesaplanmaz; /fiyat/teklif çağrısı kurSnapshotId ile yapılır
  - Durum makinesinin izin vermediği geçiş düğmesi hiç render edilmez
  - İptal düğmesi gerekçe boşken isteği GÖNDERMEZ
  - Geciken sipariş rozeti Zaman.YerelBugun ile karşılaştırılır (gece yarısı tuzağı)
- `dotnet/Kuyumcu.Tests/BelgeDeposuTestleri.cs` [DB]
  - Faz B: satır 137'deki 'SIPARIS dalı açıkça reddeder' testi, 'gerçek sipariş kabul edilir + olmayan sipariş 404' testiyle DEĞİŞTİRİLİR (mevcut testin silinmesi değil, davranış değişikliğinin kayda geçirilmesi)

## Mutasyon kanıtları
- M143 — SiparisDurumMakinesi.Izinli her zaman true dönsün: SiparisDurumMakinesiTestleri yasak geçiş testleri + SiparisTestleri 'TASLAK->TESLIM_EDILDI 400' kırmızı
- M144 — TESLIM_EDILDI terminalliği kaldırılsın: 'terminal durumda PATCH reddedilir' kırmızı
- M145 — İptalde gerekçe zorunluluğu kaldırılsın: 'gerekçesiz iptal reddedilir' kırmızı
- M146 — Geçiş satırı yazılmasın (yalnız durum güncellensin): 'geçiş dökümü satır sayısı' testi kırmızı
- M147 — Geçiş satırı transaction dışına alınsın / hata sonrası kalıcı olsun: 'başarısız geçişte ne durum ne log değişir' testi kırmızı
- M148 — tenant_id şartı sipariş sorgusundan çıkarılsın: kiracı izolasyonu testi kırmızı
- M149 — tahmini tutar varken kur zorunluluğu kaldırılsın: ilgili DB testi kırmızı
- M150 — Faz B: teslim geçişinde hareketId zorunluluğu kaldırılsın: 'defter karşılığı olmayan teslim' testi kırmızı
- M151 — Liste ucundaki Take kaldırılsın: HTTP 'en çok 200 satır' testi kırmızı
- M152 — Ekranda fiyat istemcide hesaplansın: bUnit '/fiyat/teklif çağrılır' testi kırmızı

## Canlı doğrulama
- API'yi --urls 5199 ile ayağa kaldır (python3 yok; kabuk değişkeni zsh'de ${=VAR})
- Yeni test kiracısı aç, TEZGAHTAR rolüyle gir, /siparisler ekranından bir müşteri siparişi oluştur: TASLAK göründüğünü ve geçiş dökümünde tek satır olduğunu gör
- ONAYLANDI -> HAZIR -> TESLIM_EDILDI zincirini yürüt; teslimde gerçek bir satış hareketi id'si ver ve reddedilmediğini gör; id vermeden dene ve 400 aldığını gör (Faz B)
- Aynı siparişte TASLAK'a geri dönmeyi dene: 400 beklenir
- Bir tamir siparişi aç, EMANET_GIRIS ile müşteri altınını deftere al ve emanet_hareket_id'yi iliştir; cari has bakiyesinin YALNIZ emanet ucundan değiştiğini doğrula
- Sipariş yaz/geç işlemlerinden önce ve sonra hareketler ile kasa_hareketleri satır sayısını karşılaştır: DEĞİŞMEMELİ (deftere yazma tekeli kanıtı)
- Söz verilen günü dün olan bir sipariş aç, geciken süzgecinde göründüğünü doğrula (gece 00:00-03:00 penceresinde de dene — K5)
- Yetkiler ekranından TEZGAHTAR rolünden 'siparis' modülünü kaldır, menünün kaybolduğunu VE doğrudan /api/v1/siparisler çağrısının 403 döndüğünü gör
- dotnet test (0 başarısız) + ./denetim.sh (TEMİZ) + açılan test kiracısını sil (kuyumcu_net 5 kiracıya dönmeli)

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DbSet + indeks)`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs (tek satır DI)`
- `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs (Moduller.Tumu satır 22-23, Varsayilan satır 29-34)`
- `dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs (Faz B, satır 93-96 SIPARIS dalı)`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs (SozlukNav satır 50 bloğu + Tr satır 1120 bloğu + En satır 2054 bloğu)`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor (yeni NavLink)`
- `dotnet/denetim.sh (BEKLENEN_VARLIK satır 66)`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs (migration üretimiyle otomatik — çakışma kaynağı, yaz-seri zorunlu)`
- `dotnet/Kuyumcu.Tests/BelgeDeposuTestleri.cs (Faz B, satır 132-143 testinin davranış değişikliği)`
- `DOKUNULMAYACAK (bilinçli): dotnet/Kuyumcu.Api/Data/Entities.cs · Program.cs · HareketFabrikasi.cs · SatisModulu.cs · KasaModulu.cs · Kuyumcu.Web/wwwroot/css/app.css (yeni sınıf gerekirse UX-2 paketiyle çakışır, mevcut sınıflar kullanılır)`

## Mevzuat dayanağı
- 459 sıra no.lu VUK Genel Tebliği — tevsik zorunluluğu (30.000 TL nakit haddi) — **DOGRULANDI** — Sipariş kaporası nakit alınırsa had işler; ancak kapora sipariş modülünden değil mevcut tahsilat/satış ucundan geçtiği için TevsikKapisi zaten devrededir. Sipariş modülüne YENİ bir mevzuat kapısı KONULMAZ.
- MASAK 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — Aynı gerekçe: sipariş para el değiştirmediği için eşik sipariş anında değil ödeme anında ölçülür. Riski: bir sipariş birden çok kısmi ödemeye bölünürse kümülatif MasakKapisi.DogrulaKumulatif üzerinden zaten sayılır — sipariş kimliği kümülatif ölçüme SOKULMAZ (ikinci bir gerçek doğurur).
- 6502 sayılı TKHK ve Satış Sonrası Hizmetler Yönetmeliği — servis fişi / tamir belgesi yükümlülüğü — **DOGRULANMALI** — Perakende kuyumcunun tamire aldığı müşteri eşyası için belge verme yükümlülüğünün kapsamı (yetkili servis mi, her tamir mi) doğrulanmadı. Bu yüzden kodda hiçbir zorunluluk YOK; tamir makbuzu paket 5'e bırakıldı ve o paket geldiğinde yapılandırmadan okunan, varsayılan KAPALI bir özellik olarak tasarlanmalıdır (ADR-0005).
- Emanet altının işletme malı sayılmaması (mevcut proje kararı, EmanetModulu.cs başlığı) — **DOGRULANDI** — Kod içi yerleşik karar: emanet yalnız has bacağında izlenir, stok değerlemesine ve kâr-zarara girmez. Tamir siparişi bu kuralı değiştirmez.

## Riskler
- ÇAKIŞMA: Sozlukler.cs, KenarCubugu.razor, KuyumcuDbContext.cs ve ModelSnapshot.cs'e Katman 0/5/6'nın birçok paketi dokunuyor (MIMARI-KALAN-PAKETLER satır 194). İki ajan aynı anda migration üretirse 'boş migration' tuzağı tekrarlanır — yazma kesinlikle seri olmalı.
- PAKET 4 BAĞI: Faz B, BelgeDeposuTestleri.cs:137'deki mevcut yeşil testin davranışını DEĞİŞTİRİR. Uygulayıcının refleksi testi silmek olabilir; doğrusu testi yeni davranışa çevirip mutasyon kanıtını korumaktır.
- KAPSAM KAYMASI: 'iş emri' adı Katman 1'i çağırır. Kalem tablosuna fire/usta/ayar düşürme alanı eklenmesi kapsam ihlalidir; gözden geçirende bu özellikle aranmalı.
- DURUM ENFLASYONU: müşteri 'kumaş geldi/cila bitti' gibi ara durumlar isteyebilir. Kapalı liste altı durumda tutulmazsa durum makinesi test edilemez hâle gelir; ihtiyaç çıkarsa ekip oyuyla ve tek seferde genişletilmelidir.
- EŞZAMANLILIK: iki kullanıcı aynı siparişte aynı anda geçiş yaparsa son yazan kazanır ve geçiş logunda iki satır oluşur. Faz A'da satır kilidi (guncellendi damgası ile iyimser kilit) önerilir; kurulmazsa log tutarsız görünür ama veri kaybı olmaz.
- ÖLÇEK: siparisler tablosu zamanla büyür ve 'geciken' sorgusu (tenant_id, durum, soz_verilen_gun) indeksine bağlıdır; indeks unutulursa tam tarama olur.

## Açık sorular
- Defter karşılığı olmayan teslim: 'TESLIM_EDILDI için hareketId zorunlu' kuralı fail-closed önerildi. Kuyumcu, faturasız/deftersiz teslimi işaretlemek isterse ne olacak? Seçenekler: (a) kural fail-closed kalsın, (b) gerekçeli 'defter dışı teslim' bayrağı + raporda ayrı gösterim. Karar oylamaya bırakıldı.
- TAMIR türünde müşteri eşyasının EMANET_GIRIS ile deftere alınması ZORUNLU mu, opsiyonel mi? Zorunlu yapılırsa küçük tamirlerde (zincir lehimi) sürtünme doğar; opsiyonel bırakılırsa müşteri altını defterde görünmez.
- Sipariş no biçimi (S-YYYY-NNNNNN) kiracı bazında mı, şube bazında mı artmalı? Şube bazlı numara talebi rakiplerde görüldü iddiası DOĞRULANMADI — piyasa araştırması dosyalarında birebir kanıt aranmadı.
- Kapora birden çok olabilir mi? Tek kapora_hareket_id alanı önerildi; çoklu kapora gerekirse geçiş logundaki hareket_id yeterli olur mu, yoksa ayrı bir bağ tablosu mu gerekir?
- Paket 6 (bildirim/zamanlanmış iş) henüz yok. 'Söz verilen gün yaklaştı' hatırlatması Faz B'de yalnız sorgu ucu olarak mı kalacak, yoksa paket 6 sevk edilene kadar ekranda pasif rozet mi yeterli?
- Sipariş ekranının UX-3 (basit/profesyonel mod) ile ilişkisi tanımlanmadı; basit modda tamir takibi gizlenmeli mi?

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Kapsam düzeltmesi doğru (Katman 1 dışarıda, iş emri yok). Üç tablo + saf durum makinesi + append-only geçiş logu kararı tutarlı ve mutasyon kanıtını ucuzlatıyor. 'Para hareketi doğurmaz, deftere yazmaz, yalnız referans tutar' kararı deftere yazma tekelini koruyor. Entities.cs'e dokunmayıp ayrı dosyaya yazma kararı çakışma azaltma açısından isabetli ve mevcut desene (GunKapanisi.cs, UrunKategorisi.cs) uygun. Stok rezervasyonunun gerekçeli reddi doğru — rezerv kolonu ikinci bir stok gerçeği yaratırdı.
  - Koşullar: Yeni 'siparis' modül anahtarı paket 29'un modül-başı tohumlama mekanizmasından SONRA eklensin; yoksa mevcut 5 kiracıda sessiz 403 doğar (YetkiModulu marker satırı tohumlamayı bir kez çalıştırıyor). · Faz B 'TESLIM_EDILDI için hareketId zorunlu' kuralı fail-closed olarak KABUL; ancak defter dışı teslim ihtiyacı sahada çıkarsa ikinci mekanizma açılmayacak, gerekçeli bir bayrak + raporda ayrı gösterim olarak aynı uçtan çözülecek. · Sipariş no eşzamanlılığı: (tenant_id, no) UNIQUE + tek yeniden deneme yeterli değil; çakışmada yeniden deneme sayısı sınırlı bir döngü olmalı ve tükenirse 409 dönmeli (sessiz ikinci ULID üretilmemeli). · Geçiş yazımında iyimser kilit (guncellendi damgası) Faz A kapsamına alınsın — iki kullanıcı aynı anda geçiş yaparsa logda iki satır, durumda son yazan kazanır; veri kaybı yok ama iz tutarsız görünür. · TAMIR türünde müşteri eşyasının EMANET_GIRIS zorunluluğu karar bekliyor; karar verilene kadar opsiyonel kalsın ama ekranda 'defterde görünmez' uyarısı çıksın.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Para hareketi yok, defter tekeli korunuyor, kapılar mevcut uçlarda kalıyor. Kişisel veri yalnız cari_id referansı. Tek eksik geçiş logunun kod tarafında zorlanmış append-only'si.
  - Koşullar: siparis_gecisleri append-only: DenetimYazmaDinleyicisi'ndeki Modified/Deleted yasağı deseni bu varlığa da uygulansın (SiparisGecisi için EntityState.Modified/Deleted → istisna) + test; 'ADR-0004 ruhu' demek zorlama değildir. · IPTAL yalnız SAHIP/MUDUR; PATCH yalnız TASLAK; TEZGAHTAR iptal → 403 HTTP testi (spec böyle). · Faz B hareketId bağı: hareket aynı kiracıda VE aynı cariye ait olmalı (spec yalnız kiracı diyor) — başka müşterinin satışı bu siparişe 'teslim' diye bağlanmasın. · Yabancı kiracı 404; liste ≤200; ekranda fiyat sunucudan (spec böyle). · Sipariş no üretiminde UNIQUE çakışmasında tek yeniden deneme — sayaç tablosu yok (kabul).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Deftere yazmayan, saf durum makineli tasarım QA açısından ideal; mutasyonlar (M143–M152) test adlarına bağlı. Ancak yeni modül anahtarının tohumlama borcu görülmemiş (29 görmüş) ve iki test ölçüm yöntemi eksik.
  - Koşullar: Yeni modül anahtarı 'siparis' Moduller.Tumu'ya eklenirken TohumlaGerekiyorsa (YetkiModulu.cs:99-118) tek marker ile bir kez çalıştığından MEVCUT kiracılarda MUDUR/MUHASEBE/TEZGAHTAR 'siparis' modülünü sessizce 403 görür. Paket 29'un K4-F modül-başı tohumlaması bu paketten önce sevk edilmeli ya da aynı mekanizma burada kurulmalı; HttpSiparisTestleri'ne 'eski marker'lı kiracıda MUDUR → 200' regresyon testi eklensin. · M146/M147 (geçiş satırı transaction içinde) testi 'başarısız geçişte ne durum ne log değişir' için hata enjeksiyonu yöntemi yazılsın (ör. gerekçesiz IPTAL 400 sonrası SQL sayımı). · SiparisNo eşzamanlılık: '(tenant_id,no) UNIQUE + tek yeniden deneme' için iki paralel oluşturma testi yazılsın; aksi hâlde yeniden deneme kodu ölçülmez. · Faz B BelgeDeposuTestleri.cs:137 testinin davranış değişikliği spesifikasyonda doğru; kabul ölçütüne 'test silinmez, güncellenir ve M-BELGE mutasyonu korunur' yazılsın. · Ekran testi 'geciken rozeti Zaman.YerelBugun ile karşılaştırılır (gece yarısı tuzağı)' bUnit'te sahte saat gerektirir; Zaman soyutlaması yoksa test tanımlanamaz — nasıl ölçüleceği yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- TOHUMLAMA: yeni 'siparis' modül anahtarı paket 29'un modül-başı tohumlama mekanizmasından SONRA eklensin; YetkiModulu.cs:99-118 tek marker ile bir kez çalıştığı için mevcut 5 kiracıda MUDUR/MUHASEBE/TEZGAHTAR sessiz 403 görür (Bertan + Aşkın aynı bulgu).
- APPEND-ONLY: siparis_gecisleri için DenetimYazmaDinleyicisi'ndeki Modified/Deleted yasağı deseni SiparisGecisi'ne de uygulansın (EntityState.Modified/Deleted → istisna) + test; 'ADR-0004 ruhu' demek zorlama değildir.
- YETKİ: IPTAL yalnız SAHIP/MUDUR; PATCH yalnız TASLAK; TEZGAHTAR iptal → 403 HTTP testi; yabancı kiracı 404; liste ≤200; ekranda fiyat sunucudan.
- SİPARİŞ NO: (tenant_id,no) UNIQUE + yeniden deneme SINIRLI DÖNGÜ olsun, tükenirse 409 (sessiz ikinci ULID YOK). Aşkın: iki paralel oluşturma testi yazılsın, yoksa yeniden deneme kodu ölçülmez.
- İYİMSER KİLİT: geçiş yazımında guncellendi damgası ile satır kilidi Faz A kapsamına alınsın (aksi hâlde log tutarsız görünür).
- FAZ B hareketId bağı: hareket aynı kiracıda VE aynı cariye ait olmalı (spec yalnız kiracı diyor) — başka müşterinin satışı 'teslim' diye bağlanmasın. TESLIM_EDILDI için hareketId fail-closed KABUL.
- TAMIR'de EMANET_GIRIS zorunluluğu karar bekliyor; karar çıkana dek OPSİYONEL kalsın ama ekranda 'defterde görünmez' uyarısı çıksın. Defter dışı teslim ihtiyacı çıkarsa ikinci mekanizma değil, gerekçeli bayrak + raporda ayrı gösterim.
- PAKET 4 BAĞI: Faz B, BelgeDeposuTestleri.cs:137'deki yeşil testin davranışını değiştirir — kabul ölçütüne 'test SİLİNMEZ, güncellenir ve M-BELGE mutasyonu korunur' yazılsın.
- TEST ÖLÇÜLEBİLİRLİĞİ: M146/M147 için hata enjeksiyonu yöntemi yazılsın (ör. gerekçesiz IPTAL 400 sonrası SQL sayımı). 'Geciken rozeti Zaman.YerelBugun' bUnit testi sahte saat ister; Zaman soyutlaması yoksa nasıl ölçüleceği önce yazılır.
- KAPSAM: Katman 1 sızıntısı yasak — kalem tablosuna fire/usta/ayar düşürme alanı EKLENMEZ. Durum listesi kapalı altı durumda kalır; genişletme yalnız ekip oyuyla ve tek seferde. (tenant_id, durum, soz_verilen_gun) indeksi unutulmaz.
