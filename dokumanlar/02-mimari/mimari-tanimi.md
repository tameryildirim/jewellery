# Mimari Tanımı (Architecture Description)

| | |
|---|---|
| **Belge kimliği** | KYM-AD-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Bertan (02 Baş Mimar) |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC/IEEE 42010:2022** §6 · **arc42** (CC BY-SA 4.0) · **C4 model** |

> ⚠ Bu belge **IEEE 1016-2009**'a dayanmaz. O standart 5 Mart 2020'de **Inactive-Reserved** durumuna alınmıştır ve aktif değildir.

---

## 1. Giriş ve hedefler *(arc42 §1)*

**Entity of Interest (42010:2022 terimi):** Kuyumcu ERP — çok kiracılı kuyumculuk işletme yönetim sistemi.

**Üç yönlendirici hedef:**

1. **Kiracı izolasyonu mutlaktır.** Her kuyumcu ayrı bir işletmedir ve rakip olabilir. Bir kiracının verisinin diğerine sızması, ürünün varlık sebebini ortadan kaldırır.
2. **Yasal uyum üründe taşınır, kullanıcıya bırakılmaz.** MASAK, VUK ve KVKK yükümlülükleri kuyumcunun hatırlamasına bağlı olamaz; sistem fail-closed davranır.
3. **Mevzuat değişince kod değişmez.** Eşikler ve oranlar yapılandırmadadır.

---

## 2. Kısıtlar *(arc42 §2)*

| Kısıt | Kaynak |
|---|---|
| .NET 8, EF Core, MySQL 8.4, Blazor WebAssembly | Devralınan kod tabanı |
| Tek MySQL örneği, sharding yok | Ölçek ve maliyet |
| Türkçe arayüz (+ İngilizce) | Hedef pazar |
| MASAK'a gönderim **elle** yapılır | MASAK'ın API'si ve test sunucusu **yok** |
| e-Fatura entegratör sözleşmesine bağlı | Dış bağımlılık (K1) |
| Europe/Istanbul gün sınırı | Mevzuat |
| **Commit atılmaz** | Kurucu kuralı |

---

## 3. Bağlam ve kapsam *(arc42 §3 · C4 Seviye 1)*

→ [`diyagramlar/c4-1-sistem-baglami.svg`](../diyagramlar/c4-1-sistem-baglami.svg)

| Dış varlık | İlişki |
|---|---|
| Kuyumcu / Sahip · Müdür · Muhasebe · Tezgâhtar | Kullanıcı (rol bazlı) |
| Kur sağlayıcıları (TCMB, Altınkaynak, Truncgil, Ankara Altın Borsası) | Giden HTTPS — **yalnız okuma**, veri gönderilmez |
| MASAK Online | ŞİB hazırlanır, **gönderim elle** (API yok) |
| GİB / özel entegratör | **Henüz bağlı değil** (K1) |

**Dışa giden tüm ağ hedefleri bu dört kur sağlayıcısıyla sınırlıdır.** Ölçüldü: başka hiçbir dış çağrı yok, hiçbir AI servisine veri gönderilmiyor.

---

## 4. Çözüm stratejisi *(arc42 §4)*

| Hedef | Strateji | Karar |
|---|---|---|
| Kiracı izolasyonu | **İki katman**: okuma filtresi + yazma denetçisi; fail-closed | [ADR-0001](adr/0001-cok-kiracili-izolasyon-iki-katman.md) |
| Denetlenebilirlik | Tek kaçış yolu (`SistemKapsami`), grep'lenebilir | [ADR-0002](adr/0002-sistem-kapsami-tek-kacis-yolu.md) |
| Kişisel veri | AES-256-GCM + aranabilirlik için HMAC | [ADR-0003](adr/0003-alan-bazli-sifreleme.md) |
| İnkâr edilemezlik | Append-only denetim izi, kodla zorlanan | [ADR-0004](adr/0004-denetim-izi-append-only.md) |
| Mevzuat esnekliği | Eşikler yapılandırmadan | [ADR-0005](adr/0005-mevzuat-esikleri-yapilandirmadan.md) |
| Belge üretimi | İstemcide render, sunucuda PDF bağımlılığı yok | [ADR-0007](adr/0007-belge-uretimi-istemcide.md) |
| Saklama ↔ KVKK gerilimi | Silme değil maskeleme, elle onay | [ADR-0008](adr/0008-saklama-suresi-maskeleme.md) |
| Referans bütünlüğü | Kompozit `(id, tenant_id)` FK, RESTRICT, aşamalı | [ADR-0009](adr/0009-veritabani-yabanci-anahtar-yok.md) |

---

## 5. Yapı taşları görünümü *(arc42 §5 · C4 Seviye 2-3)*

→ [`c4-2-konteyner.svg`](../diyagramlar/c4-2-konteyner.svg) · [`c4-3-bilesen-api.svg`](../diyagramlar/c4-3-bilesen-api.svg)

**Üç derleme birimi, tek süreç:**

| Birim | İçerik |
|---|---|
| `Kuyumcu.Api` | 17 modül, 100 uç, veri katmanı, kesişen ilgiler |
| `Kuyumcu.Web` | Blazor WASM SPA — 14 sayfa, 5.072 satır `.razor` |
| `Kuyumcu.Core` | Etki alanı hesapları (has altın, ayarlar) + i18n sözlükleri |

Üretimde **tek origin**: Blazor ve API aynı süreçten servis edilir. CORS beyaz listesinin boş olması bu yüzden doğru varsayılandır.

### Modül dağılımı (uç sayısıyla)

| Uyum | İş | Kimlik |
|---|---|---|
| Masak 12 · Hurda 4 · Denetim 2 | Kullanici 10 · Cari 9 · Urun 8 · Kur 7 · Kasa 7 · Banka 7 · Satis 6 · Sube 5 · Rapor 5 · Fatura 4 · Emanet 3 | Auth 5 · Yetki 3 · Admin 3 |

### Kesişen ilgiler *(arc42 §8)*

| İlgi | Bileşen | Zorlama noktası |
|---|---|---|
| Kiracı izolasyonu | `KiraciBaglami` + global filtre + `KiraciYazmaDenetcisi` | Sorgu ve `SaveChanges` |
| Denetim izi | `DenetimYazmaDinleyicisi` (otomatik) + `DenetimServisi` (okuma, açık çağrı) | `SaveChanges` / servis |
| Alan şifreleme | `AlanSifreleme` + EF değer dönüştürücü | Model |
| Oran sınırı | `OranSiniri` — `GlobalLimiter` + uç politikaları | Boru hattı |
| Güvenlik başlıkları | Program.cs ara katmanı | Boru hattının başı |

---

## 6. Çalışma zamanı görünümü *(arc42 §6)*

### Eşik üstü satış akışı

```
1. İstemci   → POST /api/v1/islemler/satis   (masak bloğu ile)
2. Middleware: ForwardedHeaders → güvenlik başlıkları → auth → oran sınırı
3. Yetki     : RolGerekli + ModulYetkisi
4. Kiracı    : IstekKiraciBaglami ← JWT tenantId
5. MasakKapisi.Dogrula(tutar, masak)        ← FAZ 1: eksikse 400, işlem BİTMEZ
6. Transaction başlar
     ├─ Hareket yazılır            → denetim izi otomatik
     ├─ Stok düşülür
     ├─ Kasa güncellenir           → İŞARETLİ (+ giriş / − çıkış)
     └─ MasakKapisi.Kaydet(...)    ← FAZ 2: kimlik tespiti + beyan
7. Commit
```

**İki fazlı kapı bilinçlidir:** doğrulama transaction'dan **önce** yapılır ki eksik kimlikle hiçbir yazma başlamasın.

### Denetim izi okuma akışı

```
GET /api/v1/denetim  →  RolGerekli("SAHIP")  →  kiracı filtresi
                     →  liste  →  KENDİ OKUMASINI da ize yazar
```
Özyineleme yoktur: okuma bir **yazma** üretir, o yazma yeni bir okuma üretmez.

---

## 7. Dağıtım görünümü *(arc42 §7)*

```
[ İstemci tarayıcı ]
        │ HTTPS
[ nginx ters vekil ]  X-Forwarded-For, X-Forwarded-Proto
        │ 127.0.0.1:4000
[ Kuyumcu.Api (tek süreç: API + Blazor + SignalR) ]
        │ TCP 3306
[ MySQL 8.4 ]
```

**İlk yükleme:** 2,42 MB (Brotli, Release, IL trimming açık, 52 assembly).

**Bilinen tekil arıza noktaları:** tek MySQL örneği (replika yok) · tek uygulama süreci (yatay ölçekleme denenmedi) · kur sağlayıcısı kesintisi (4 kaynak tanımlı, devretme davranışı **ölçülmedi**).

---

## 8. Veri modeli

→ [`er-genel.svg`](../diyagramlar/er-genel.svg) + 4 konu alanı diyagramı

**21 tablo · 262 sütun · 54 indeks.** Diyagramlar `er-uret.py` ile **şemadan üretilir**, elle çizilmez.

| Konu alanı | Tablolar |
|---|---|
| Kiracı ve kimlik | `tenants`, `subeler`, `kullanicilar`, `refresh_tokenler`, `rol_yetkileri` |
| Ürün, stok, işlem | `urunler`, `stoklar`, `hareketler`, `kur_snapshots` |
| Finans | `cariler`, `cari_bakiyeler`, `kasalar`, `kasa_hareketleri`, `banka_hesaplari`, `banka_hareketleri`, `faturalar` |
| Uyum | `masak_kimlik_tespitleri`, `masak_beyanlari`, `masak_supheli_bildirimler`, `gider_pusulalari`, `denetim_kayitlari` |

**20/21 tablo `tenant_id` taşır**; taşımayan tek tablo `tenants` — zaten kiracının kendisidir.

⚠ **Bugün enforce edilen yabancı anahtar yoktur.** ER diyagramlarındaki `FK` işaretleri **mantıksal** yabancı anahtarı gösterir. Bu durum [ADR-0009](adr/0009-veritabani-yabanci-anahtar-yok.md) ile karara bağlanmıştır.

---

## 9. Mimari kararlar *(arc42 §9 · 42010:2022 §6.10)*

12 ADR — [`adr/`](adr/). Her kayıt: bağlam, karar, gerekçe, **olumsuz sonuçlar**, ve mümkünse "ne zaman yanlış olur".

---

## 10. Kalite gereksinimleri *(arc42 §10)*

→ [`03-kalite/kalite-gereksinimleri.md`](../03-kalite/kalite-gereksinimleri.md) — ISO/IEC 25010:**2023**, 9 karakteristik, 54 ölçülebilir gereksinim.

---

## 11. Riskler ve teknik borç *(arc42 §11)*

→ [`08-degerlendirme/teknik-borc-envanteri.md`](../08-degerlendirme/teknik-borc-envanteri.md) — 14 kalem, öncelikli.

---

## 12. Sözlük *(arc42 §12)*

| Terim | Anlam |
|---|---|
| **Has altın** | Saf altın karşılığı: `(gram − taş) × milyem / 1000` |
| **Milyem** | Ayarın binde saflığı (K22 = 916/1000) |
| **Kuyum KDV** | Özel matrah: has altın bedeli KDV matrahından düşülür |
| **Gider pusulası** | VUK md.234 — vergi mükellefi olmayandan alımda düzenlenen belge |
| **Stopaj / tevkifat** | GVK md.94 — kaynakta kesinti |
| **ŞİB** | Şüpheli İşlem Bildirimi (MASAK), 10 iş günü |
| **Kiracı (tenant)** | Sistemdeki bir kuyumcu işletmesi |
| **Sistem kapsamı** | Kiracı filtresinin bilinçli olarak kurulmadığı tek kaçış yolu |
| **Mutasyon kanıtı** | Korumayı bilerek kırıp testin kırmızıya döndüğünü gösterme |
| **KMTS** | Kıymetli Maden Takip Sistemi — kapsamı rafineriler |

---

## 13. Paydaşlar ve ilgileri *(42010:2022 §6.2-6.4)*

| Paydaş | İlgi | Nerede karşılanıyor |
|---|---|---|
| Kuyumcu (sahip) | Verim, yasal güvence, veri sahipliği | §4, §6, ADR-0008 |
| Personel | Hız, hata yapmama | §6, rol matrisi |
| Mali müşavir | Belge doğruluğu, mutabakat | Uyum modülleri |
| Denetçi (MASAK/vergi) | İbraz edilebilirlik, değiştirilemezlik | ADR-0004, ADR-0008 |
| Veri sahibi (müşteri) | KVKK hakları | Şifreleme, okuma izi, maskeleme |
| Geliştirici | Anlaşılabilirlik, güvenli değişim | ADR'ler, `denetim.sh` |
| Yatırımcı / alıcı | Teknik olgunluk | [Öz-değerlendirme](../08-degerlendirme/teknik-oz-degerlendirme.md) |

---

## 14. Bakış açıları *(42010:2022 §6.5-6.6)*

| Bakış açısı | Gösterim | Hangi ilgiyi karşılar |
|---|---|---|
| Bağlam | C4 Seviye 1 | Sistem sınırı, dış bağımlılık |
| Konteyner | C4 Seviye 2 | Dağıtım birimleri, teknoloji |
| Bileşen | C4 Seviye 3 | Modül yapısı, kesişen ilgiler |
| Bilgi | ER (genel + 4 konu alanı) | Veri modeli, kiracı kapsaması |
| Çalışma zamanı | §6 akış anlatımları | Kapıların sırası |
| Dağıtım | §7 | Topoloji, arıza noktaları |
| Karar | 12 ADR | Gerekçe ve bedeller |

---

## 15. Mimari denetimi

Mimari kurallar **yorum değil, koddur**: `dotnet/denetim.sh` her koşumda 11 kuralı zorlar ve ihlali dosya + satır numarasıyla raporlar.

Betik **kendisi mutasyonla sınanmıştır**: `RaporModulu`'na bilerek `IgnoreQueryFilters` sokuldu; yakaladı, çıkış kodu 1 verdi; geri alınca 0'a döndü. **Yakalamayan bir denetim betiği sahte güven üretir.**
