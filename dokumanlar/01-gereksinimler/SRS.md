# Yazılım Gereksinimleri Belirtimi (SRS)

| | |
|---|---|
| **Belge kimliği** | KYM-SRS-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC/IEEE 29148:2018** §9.6 |

> ⚠ **IEEE 830-1998 geri çekilmiştir.** Bu belge onun yerine geçen ISO/IEC/IEEE 29148:2018 yapısını izler.

---

## 1. Giriş

### 1.1 Amaç
Kuyumcu ERP'nin yazılım gereksinimlerini tanımlar. Paydaş gereksinimleri [StRS](StRS.md)'de, mevzuat izlenebilirliği [mevzuat-gereksinimleri.md](mevzuat-gereksinimleri.md)'de, kalite hedefleri [kalite-gereksinimleri.md](../03-kalite/kalite-gereksinimleri.md)'de yer alır.

### 1.2 Kapsam
Çok kiracılı bir kuyumculuk işletme yönetim sistemi: stok, satış, cari, kasa, banka, fatura, emanet, raporlama ve **yasal uyum** (MASAK, VUK, KVKK).

**Kapsam dışı (bu sürümde):** barkod/RFID, toptan modülü, imalat/atölye, tamirat, personel/prim, masraf yönetimi, sertifika/katalog, mobil uygulama, e-ticaret entegrasyonu.

### 1.3 Ürün bakış açısı
Devralınan bir kod tabanının geliştirilmesidir, sıfırdan geliştirme değildir. Devralındığında **0 otomatik test** ve kanıtlanmış 6 kritik hata vardı.

### 1.4 Ürün işlevleri (özet)
17 modül, 100 API ucu, 14 ekran, 21 tablo.

### 1.5 Kullanıcı özellikleri

| Rol | Teknik yeterlilik | Kullanım sıklığı |
|---|---|---|
| Sahip | Düşük-orta | Günlük |
| Müdür | Düşük-orta | Günlük |
| Muhasebe | Orta (muhasebe bilgisi yüksek) | Günlük |
| Tezgâhtar | **Düşük** | Sürekli — müşteri karşısında |

> Tezgâhtar profili tasarımı belirler: yasal bir zorunluluk çıktığında ekran **ne yapılacağını yazmalıdır**, hata kodu göstermemelidir.

### 1.6 Sınırlamalar
.NET 8 · MySQL 8.4 tek örnek · tek süreç · Türkçe birincil dil · Europe/Istanbul gün sınırı · MASAK'a gönderim elle · commit yasağı.

### 1.7 Varsayımlar ve bağımlılıklar

| Varsayım | Risk |
|---|---|
| Kur sağlayıcıları erişilebilir | 4 kaynak tanımlı; devretme **ölçülmedi** |
| İşletmenin MASAK Online kaydı var | Kurucu kalemi K5 |
| e-Fatura entegratör sözleşmesi yapılacak | **K1** — yapılmazsa o segment kapsam dışı |
| Mevzuat rakamları değişebilir | Yapılandırmadan okunur |

---

## 2. Gereksinimler

### 2.1 Dış arayüzler

| # | Gereksinim |
|---|---|
| **G-001** | Sistem REST API sunar (`/api/v1`), JSON, **camelCase** |
| **G-002** | Hata gövdesi `{ message, error, statusCode }`; doğrulama hatasında `message` **dizidir** |
| **G-003** | Kimlik doğrulama JWT Bearer; access 15 dk, refresh 7 gün |
| **G-004** | Canlı kur SignalR ile yayınlanır (`/hubs/kur`) |
| **G-005** | Kur en az 2 bağımsız kaynaktan çekilir *(4 tanımlı)* |
| **G-006** | `/saglik` ucu kimlik doğrulaması istemez |
| **G-007** | Ters vekil arkasında gerçek istemci IP'si **yalnız güvenilen vekilden** kabul edilir |

### 2.2 İşlevsel gereksinimler

#### Kiracılık ve kimlik

| # | Gereksinim | Kabul ölçütü |
|---|---|---|
| **F-101** | Her veri kaydı bir kiracıya aittir | 20/21 tablo `tenant_id` taşır |
| **F-102** | Bir kiracı diğerinin verisini **göremez** | Filtre kapatılınca ≥10 test kırmızı |
| **F-103** | Bir kiracı diğerinin verisine **yazamaz** | Yazma denetçisi reddeder ve **ize yazar** |
| **F-104** | Kiracı bağlamı yoksa `DbContext` **oluşturulamaz** | Ctor istisna |
| **F-105** | `IKiraciVarlik` uygulamayan varlık → **uygulama açılmaz** | Model doğrulaması |
| **F-106** | Kullanıcı rolleri: SAHİP / MÜDÜR / MUHASEBE / TEZGÂHTAR | Rol matrisi |
| **F-107** | Rol yetkisi **sunucuda** zorlanır; ekran gizlemesi tek kapı değildir | HTTP 403 testi |
| **F-108** | Refresh token rotasyonu; **yeniden kullanımda tüm oturumlar kapanır** | Test |
| **F-109** | Şifreler BCrypt ile saklanır | Test — düz metin yok |

#### Ürün, stok, işlem

| # | Gereksinim | Kabul ölçütü |
|---|---|---|
| **F-201** | Ayar bazlı stok (14/18/22/24) | Mevcut |
| **F-202** | Has altın: `(gram − taş) × milyem / 1000` | %100 kapsam |
| **F-203** | İşlem türleri: SATIŞ, ALIŞ, HURDA_ALIŞ, İŞÇİLİK, TAKAS, TRANSFER | Mevcut |
| **F-204** | Her işlem **kur snapshot'ına** bağlanır | Zorunlu alan |
| **F-205** | Kasa hareketi **işaretli** yazılır: + giriş, − çıkış | Özdeşlik testi (bu bir kez kırıldı) |
| **F-206** | Çok şube; şubeler arası transfer | Mevcut |

#### Cari ve finans

| # | Gereksinim | Kabul ölçütü |
|---|---|---|
| **F-301** | Paralel defter: TL **ve** has gram | Özdeşlik testi |
| **F-302** | Kredi limiti aşılırsa veresiye **reddedilir** | Test |
| **F-303** | Kuyum KDV özel matrahı | Mevcut |
| **F-304** | Gün sonu POS–fatura mutabakatı; fark ve faturasız işlemler listelenir | Kasıtlı uyumsuz veriyle test |
| **F-305** | Cari kartı okunduğunda **denetim izi düşer** | Canlı doğrulama |

#### Yasal uyum

| # | Gereksinim | Kabul ölçütü |
|---|---|---|
| **F-401** | 185.000 TL ve üstünde kimlik tespiti **zorunlu**; eksikse işlem **tamamlanamaz** | Canlı 400 |
| **F-402** | Eşik kademelerine göre amaç beyanı / beyan formu / ek belge | Tam sınır testleri |
| **F-403** | Eşikler **yapılandırmadan** okunur | ADR-0005 |
| **F-404** | ŞİB için **10 iş günü** sayacı; hafta sonu **ve resmî tatiller** hariç | Test |
| **F-405** | Dinî bayramlar yapılandırmadan; girilmemiş yıl **sessizce geçilmez** | `DiniBayramEksik()` |
| **F-406** | Kimlik verisi **şifreli** saklanır; aranabilirlik hash ile | Diskte düz metin 0 |
| **F-407** | Listede TCKN **maskeli**; tam değer yalnız tekil görüntülemede + iz | Test |
| **F-408** | Uyum kayıtları **append-only** | ADR-0004 |
| **F-409** | 8 yıl sonunda kimlik alanları **maskelenir**, kayıt silinmez | ADR-0008 |
| **F-410** | Maskeleme **elle tetiklenir**; önce önizleme | Otomatik zamanlayıcı bilinçli yok |
| **F-411** | Hurda alımında kimlik zorunlu; **TCKN checksum** doğrulanır | Canlı 400 |
| **F-412** | Gider pusulası VUK md.234 alanlarıyla, **tutar yazıyla** | Canlı belge |
| **F-413** | Stopaj satıcı tipine göre; **kasadan net tutar çıkar** | Özdeşlik testi (bu da bir kez kırıldı) |
| **F-414** | Gider pusulası **7 gün** süre takibi | Test |
| **F-415** | Kişisel veri okumaları **kaydedilir** (KVKK md.12) | Canlı |
| **F-416** | Denetim izi **değiştirilemez ve silinemez** | Mutasyon kanıtı |
| **F-417** | Denetim izi **yalnız SAHİP** tarafından okunabilir; ibraz edilebilir | Canlı 403/200 |
| **F-418** | Gövdeden gelen **her** `*_id` kiracıya karşı doğrulanır | Mutasyon kanıtı |

#### Yapılacak — kaynağı kesin

| # | Gereksinim | Dayanak | Durum |
|---|---|---|---|
| **F-419** | 30.000 TL nakit tevsik kümülatif kontrolü, **fail-closed** | VUK md.459 / 575 s. Tebliğ | ⬜ PLAN-2 Adım 2 |
| **F-420** | Sentetik taş: ürün kartında zorunlu köken alanı; etiket, fatura ve kategoride ayrım | RG 24.06.2026/33290 | ⬜ PLAN-2 Adım 3 |
| **F-421** | Veri dışa aktarımı — varsayılan maskeli, tam veri ayrı onayla | StR-04 | ⬜ PLAN-2 Adım 4 |

### 2.3 Kullanılabilirlik gereksinimleri

| # | Gereksinim |
|---|---|
| **F-501** | Arayüz Türkçe ve İngilizce |
| **F-502** | Yasal alanlar **kademeli** açılır — eşik altında hiçbir ek alan istenmez |
| **F-503** | Yetkisiz kullanıcı ekranı **görmez** |
| **F-504** | Para ve gram değerleri yerel biçimde gösterilir |

### 2.4 Başarım gereksinimleri

| # | Gereksinim | Durum |
|---|---|---|
| **F-601** | İlk yükleme ≤ 3 MB (sıkıştırılmış) | ✅ **2,42 MB** |
| **F-602** | Kiracı sorguları indeks kullanır | ✅ 54 indeks |
| **F-603** | p95 yanıt süresi hedefi | ⬜ **Hedef konmadı, ölçülmedi** |

### 2.5 Mantıksal veritabanı gereksinimleri

| # | Gereksinim |
|---|---|
| **F-701** | Şema **migration ile** yönetilir; elle değiştirilmez |
| **F-702** | Uyum tabloları append-only |
| **F-703** | Kimlik alanları şifreli + arama için hash sütunu |
| **F-704** | Referans bütünlüğü kompozit `(id, tenant_id)` FK ile sağlanacak | ADR-0009, uygulanmadı |

### 2.6 Tasarım kısıtları

| # | Kısıt |
|---|---|
| **F-801** | `IgnoreQueryFilters` **yasak**; tek kaçış yolu `SistemKapsami` |
| **F-802** | `Find`/`FindAsync` **yasak** — birincil anahtar erişimi filtreyi atlar |
| **F-803** | `ExecuteUpdate`/`ExecuteDelete` **beyaz listede** (**5** kullanım: Auth ×4 — davet tüketimi + son giriş damgası + iki token iptali — ve Kur ×1 yetim snapshot bakımı; sayı 2026-09-23'te P-30b ile koda bakılarak ölçüldü, eski "4" beyanı çürüktü); yeni ekleme CI'yı kırar |
| **F-804** | Sunucuya PDF kütüphanesi **eklenmez** |
| **F-805** | Navigation property eklenmez |

### 2.7 Yazılım sistem nitelikleri
→ [kalite-gereksinimleri.md](../03-kalite/kalite-gereksinimleri.md) — ISO/IEC 25010:2023, 9 karakteristik.

### 2.8 Doğrulama

| Gereksinim sınıfı | Doğrulama yöntemi |
|---|---|
| Kiracı izolasyonu | **Mutasyon testi** + HTTP testi + canlı |
| Yasal eşikler | **Tam sınır** değer testleri |
| Para / has | **Özdeşlik** testleri |
| Güvenlik | HTTP testi + mutasyon + ASVS öz-değerlendirmesi |
| Arayüz | bUnit |
| Veri bütünlüğü | `yetim-tara.sh` |
| Yedek | `yedek-al-ve-dogrula.sh` (sağlama toplamı) |

---

## 3. İzlenebilirlik

| Kaynak | → Gereksinim |
|---|---|
| MASAK md.5/1-b | F-401, F-403 |
| MASAK md.5/2 | F-401 (fail-closed, iki fazlı kapı) |
| MASAK md.28/2 | F-404 |
| MASAK md.46/1 | F-409 |
| VUK md.234 | F-411, F-412, F-414 |
| VUK md.459 / 575 s.T. | **F-419** |
| GVK md.94 | F-413 |
| RG 24.06.2026/33290 | **F-420** |
| KVKK md.12 | F-406, F-415, F-416 |
| KVKK md.4 | F-407, F-409 |
| StR-04 | **F-421** |
| StR-26 | F-101…F-105, F-418 |

---

## 4. Karşılanmayan gereksinimler

| # | Neden | Sahibi |
|---|---|---|
| F-419 | Planlandı, yapılmadı | Geliştirme |
| F-420 | Planlandı, yapılmadı | Geliştirme |
| F-421 | Planlandı, yapılmadı | Geliştirme |
| F-603 | Hedef konmadı, ölçülmedi | Mimari |
| F-704 | Karara bağlandı, uygulanmadı | Geliştirme |
| e-Fatura | Entegratör sözleşmesi yok | **Kurucu (K1)** |
| KVKK aydınlatma metni | Hukuki metin | **Kurucu (hukuk)** |
| MASAK gönderimi | **API yok** | Dış |
