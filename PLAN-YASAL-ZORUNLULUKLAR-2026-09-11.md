# PLAN — Olmazsa Olmaz Yasal Zorunluluklar

**Tarih:** 2026-09-11 · **Onaylayan:** Kurucu ("olmazsa olmaz zorunlu olanları yapalım, plana sadık gidelim")
**Kural:** Bu plan sıralıdır. Bir adım **kabul ölçütü kanıtlanmadan** bir sonrakine geçilmez.
**Commit atılmaz** (Kurucu kuralı, yürürlükte).

---

## 0. ÖNCE BİR DÜZELTME — KMTS blokaj değilmiş

`SATILABILIRLIK-DEGERLENDIRMESI-2026-09-11.md` KMTS'yi "olmazsa olmaz" saymıştı. **Bu yanlıştı ve kaynağı bir blog yazısıydı.**

Birincil kaynağa gidildi — Hazine ve Maliye Bakanlığı, *KMTS Uygulamasına İlişkin Usul ve Esaslar* (Tebliğ 2023/1'e dayalı, 487 satır). Tam metin tarandı:

- **"kuyumcu" kelimesi: 0 kez geçiyor.**
- **"perakende" kelimesi: 0 kez geçiyor.**
- Kapsam (madde 5): *"rafineriler tarafından üretilen granül halindeki gümüş haricindeki standart işlenmemiş kıymetli madenlerin ve basılı kıymetli madenlerin tamamının KMTS'ye dahil edilmesi zorunluluğu"*.
- Yükümlü taraf baştan sona **rafineriler** ve **Darphane**.
- İşlenmiş takı (22/18 ayar bilezik, yüzük) **kapsam dışı**.

**Sonuç:** KMTS perakende kuyumcunun yazılımına doğrudan yükümlülük getirmiyor. Dükkân yalnız KMTS etiketli ürünü (gram altın, külçe) alıp satıyor.

→ **KMTS bu plandan çıkarıldı.** Yerine ucuz bir hazırlık kondu: Adım 8 (opsiyonel KMTS kod alanı).
→ *Kalan risk:* kmts.gov.tr mevzuat sayfası tam okunamadı ve Nisan 2026'da yürürlüğe giren ek düzenlemeler var. **Mali müşavir teyidi gerekiyor (Kurucu kalemi K2).**

---

## 1. ZEMİN — doğrulanmış kurallar (planın dayanağı)

| Kural | Rakam | Durum |
|---|---|---|
| MASAK kimlik tespiti | **185.000 TL** ve üstü | ✅ **birincil kaynak doğrulandı**: Tedbirler Yönetmeliği md.5(1)(b), 6702 s. CK |
| MASAK işlem amacı beyanı | 200.000–2.000.000 TL, **min 20 karakter** açıklama | ⚠️ **kaynak doğrulanamadı** — Yönetmelikte yok; Ağustos 2025 TASLAK tebliğ, bankalara yönelik, kuyumcu geçmiyor (`MASAK-ENTEGRASYON-ARASTIRMASI §4`). K2-a |
| MASAK nakit işlem beyan formu | 2.000.001–20.000.000 TL; 20 M+ ek belge | ⚠️ aynı — **kaynak doğrulanamadı**, K2-a |
| MASAK şüpheli işlem bildirimi | tutardan bağımsız, **10 iş günü** | ✅ doğrulandı md.27(2) · gönderim: MASAK Online **XML yükleme**, API yok, test sunucusu yok (`MASAK-ENTEGRASYON-ARASTIRMASI`) |
| MASAK belge saklama | **~8 yıl**, denetime hazır | yapılacak |
| Kuyum KDV özel matrah | has altın bedeli matrahtan düşülür | ✅ **ZATEN VAR** (`Fatura.KdvTuru = "KUYUM"`, `HasDeger`) |
| Ayar bazlı stok ayrımı | 14/18/22/24 | ✅ **ZATEN VAR** |
| Hurda alışında kimlik + gider pusulası | müşteri kimliği zorunlu | ❌ `HURDA_ALIS` hareketi var, **kimlik yok** |
| e-Fatura / e-Arşiv | GİB entegrasyonu | ❌ yok |
| Günlük POS–fatura mutabakatı | KDV beyanı için | ❌ yok |

### ⚠️ ÇÖZÜLMEMİŞ ÇELİŞKİ — fatura/e-fatura haddi

Dört farklı rakam bulundu ve **birbirini tutmuyor**:

| Kaynak | Rakam |
|---|---|
| İstanbul Mali Müşavirlik | 12.000 TL üstü e-fatura, fiş yasak |
| baf.av.tr (hukuk) | 5.000.000 TL ciro → e-Fatura zorunlu |
| birfatura / Paraşüt | 3.000.000 TL ciro → 1 Temmuz 2026 |
| İlk araştırmamız | 36.000 TL fatura haddi · 29.700 TL |
| Genel e-Arşiv | 3.000 TL üstü (2025'ten beri) |

**Tahminle kapatmıyorum.** Adım 5 (e-Fatura) bu netleşmeden **başlamaz**. Eşik, yapılandırmadan okunacak biçimde kodlanacak ki değişince kod değişmesin.

---

## 2. SIRALI PLAN

Sıra keyfî değil: önce güvenlik (her şey üstüne biniyor + açık veto), sonra dışa bağımlılığı olmayan MASAK, sonra üçüncü tarafa bağımlı e-Fatura, sonra muhasebe boşlukları.

---

### ADIM 1 — Global kiracı sorgu filtresi `[güvenlik · Mehmet vetosu]`

**Neden ilk:** `HasQueryFilter` sayısı ölçüldü = **0**. İzolasyon şu an her sorguda elle `TenantId` yazılmasına bağlı; bir geliştirici unutunca veri sızıyor. Faz 0'da bu **kanıtlanmıştı** (başka kiracının kasasına 99.999 TL yazıldı, HTTP 200 döndü). MASAK ve e-Fatura verisi bunun üstüne binecek — önce zemin.

**Yapılacak:** `KuyumcuDbContext`'e kiracıya duyarlı global query filter; `IKiraciBaglami` soyutlaması; arka plan işleri (kur zamanlayıcı) ve admin için bilinçli `IgnoreQueryFilters` kaçış yolu.

**Kabul ölçütü:**
- `KiraciIzolasyonTestleri` genişletilir: **filtre kaldırıldığında kırmızı, konduğunda yeşil** olan en az 3 yeni test.
- Elle `TenantId` şartı **kaldırılmış** bir sorgu bile başka kiracının satırını görmez.
- 124 testin tamamı yeşil kalır.
- Mehmet'in Faz 0 vetosu **yazılı olarak kapanır**.

---

### ADIM 2 — Oran sınırı + taşıma güvenliği `[güvenlik]`

**Ölçüldü:** `AddRateLimiter` = 0 · `UseHttpsRedirection` = 0 · `AddResponseCompression` = 0 · `AddHealthChecks` = 0.

**Yapılacak:** `AddRateLimiter` (giriş ucuna sıkı, genel API'ye gevşek), HTTPS yönlendirme, br+gzip sıkıştırma, `/saglik` ucu.

**Kabul ölçütü:** Giriş ucuna ardışık hatalı denemede **429** döndüğü gerçek istekle kanıtlanır. Blazor ilk yükleme boyutu sıkıştırma öncesi/sonrası **ölçülüp raporlanır** (bugün 9,26 MB sıkıştırmasız).

---

### ADIM 3 — MASAK veri modeli `[yasal]`

**Yapılacak:**
- ~~`Cari`ye kimlik alanları~~ → **DEĞİŞTİ (Bertan tespiti, Tamer Bey mührü):** kimlik `Cari`ye DEĞİL, işlem bazlı `MasakKimlikTespiti`'ne yazılır. Gerekçe: MASAK "işlem anında tespit" ister; TCKN'yi her müşteri kartında kalıcı tutmak KVKK veri asgariliğine aykırı. `Cari`'de yalnız `VergiNo` kalır.
- Yeni tablo `masak_kimlik_tespitleri` — işleme bağlı, kim/ne zaman/hangi belge.
- Yeni tablo `masak_beyanlari` — eşik kademesi, işlem amacı, açıklama metni.
- Yeni tablo `masak_supheli_bildirimler` — tespit tarihi, **10 iş günü** son tarih, durum, gönderim kaydı.
- Migration.

**Kabul ölçütü:** Migration temiz kurulur; tablolar kiracıya bağlı ve Adım 1'in filtresine tabi; **8 yıllık saklama** için hiçbir tabloda sert silme yok (append-only).

---

### ADIM 4 — MASAK kural motoru `[yasal · farklılaştırıcı]`

**Yapılacak:** Saf, testlenebilir bir eşik motoru (pano hesabı gibi — I/O yok):
- 185.000 TL → kimlik zorunlu, eksikse **işlem kapanmaz**
- 200.000–2.000.000 TL → amaç + min 20 karakter açıklama zorunlu
- 2.000.001–20.000.000 TL → beyan formu
- 20.000.001+ → beyan formu + ek belge
- Şüpheli işlem: elle işaretleme + **10 iş günü** sayacı (resmî tatil hariç), süre dolmadan uyarı

**Eşikler yapılandırmadan okunur** — rakam değişince kod değişmez.

**Kabul ölçütü:** Her eşiğin **tam sınırında** (184.999 / 185.000 / 200.000 / 2.000.000 / 2.000.001 …) test. Fail-closed: kimlik eksikse satış **tamamlanamaz**, uyarı verip geçmez. En az 25 test.

---

### ADIM 5 — e-Fatura / e-Arşiv `[yasal · DIŞ BAĞIMLILIK]`

**⚠️ Kurucu olmadan başlamaz.** Özel entegratör sözleşmesi gerekiyor (K1).

**Yapılacak:** `IEFaturaSaglayici` soyutlaması + bir gerçek uygulama; UBL-TR belge üretimi; gönderim, durum sorgulama, iptal; GİB senaryoları (TEMELFATURA / TİCARİFATURA / e-Arşiv); mevcut `KUYUM` KDV matrahının UBL'e doğru yansıması.

**Kabul ölçütü:** Entegratörün **test ortamına gerçek fatura gönderilir**, dönen belge no ve durum raporlanır. Kuyum KDV'li bir faturada has altın bedelinin matrah dışı kaldığı UBL çıktısında **gösterilir**. Sahte/mock ile "oldu" denmez.

---

### ADIM 6 — Hurda alışı kimlik + gider pusulası `[yasal]`

**Yapılacak:** `HURDA_ALIS` işlemine zorunlu müşteri kimliği; gider pusulası belgesi üretimi; Adım 3'ün kimlik tablosuna bağlanma.

**Kabul ölçütü:** Kimliksiz hurda alışı **reddedilir** (test). Gider pusulası çıktısı örnekle gösterilir.

---

### ADIM 7 — Günlük POS–fatura mutabakatı `[yasal]`

**Yapılacak:** Gün sonu: POS tahsilatları ile kesilen faturaların karşılaştırılması, fark raporu.

**Kabul ölçütü:** Kasıtlı uyumsuz veriyle farkın **yakalandığı** testle gösterilir.

---

### ADIM 8 — KMTS opsiyonel kod alanı `[ucuz hazırlık, zorunlu değil]`

Ürün/harekete opsiyonel `kmts_kodu`. Kapsam genişlerse hazır oluruz; bugün için yalnız kayıt.

---

### ADIM 9 — Demo engelleri `[cila]`

- Panel aylık grafiği: **doğrulandı** — `Panel.razor:230` `aylik.Gunler` bekliyor, `RaporModulu.AylikOzet` (satır 106-115) `gunler` alanını **hiç döndürmüyor**. Grafik kalıcı boş.
- "Net Kasa" kartının yanıltıcı olduğu tespiti.

**Kabul ölçütü:** Gerçek veriyle dolu grafik ekran görüntüsüyle kanıtlanır.

---

## 3. PLANIN DIŞINDA KALANLAR (bilinçli)

Bunlar **bu planda yok**, çünkü yasal zorunluluk değil: barkod/RFID, toptan modülü, personel/prim, masraf, imalat/atölye, tamirat, sertifika/katalog, mobil uygulama, **pano Faz A'nın kalanı**.

**Pano beklemeye alındı.** Gerekçe: ZarifSoft'ta zaten var, fark yaratmıyor; ayrıca Mehmet'in pano yayın vetosu duruyor. Yazılan `PanoFiyatServisi` + 34 test **yerinde kalıyor**, çürümüyor.

---

## 4. KURUCU'YA DÜŞENLER — bugün başlaması gerekenler

| # | İş | Neyi kilitliyor |
|---|---|---|
| **K1** | e-Fatura özel entegratör seçimi ve sözleşmesi | **Adım 5 bunsuz başlayamaz** |
| **K2** | Mali müşavir teyidi: fatura/e-fatura haddi çelişkisi (§1) + KMTS'nin perakendeyi gerçekten bağlamadığı | Adım 5 ve 8'in kapsamı |
| **K3** | 2-3 rakip demosu (M&L, ZarifSoft, Kuyumcum) | MASAK farklılaşma iddiasının doğrulanması |
| **K4** | Pilot kuyumcu erişimi | Kabul testleri |

K1 ve K2 **uzun sürer**, bu yüzden Adım 1'le aynı anda başlatılmalı.

---

## 5. İLERLEME TABLOSU

| Adım | Konu | Durum | Kanıt |
|---|---|---|---|
| 1 | Global kiracı filtresi | ✅ **BİTTİ** | `ADIM1-2-TAMAMLAMA-RAPORU-2026-09-11.md` · 137/137 test · 11 test mutasyonda kırmızı · canlı: DB'de 19 cari, yeni kiracı 0 görüyor |
| 2 | Oran sınırı + taşıma | ✅ **BİTTİ** | Aynı rapor · gerçek 429 (6. denemede) + `Retry-After: 60` · gzip açıldı |
| 3 | MASAK veri modeli | ✅ **BİTTİ** | 3 MASAK tablosu + denetim izi (ön şart) · migration `DenetimIziVeMasak` uygulandı · canlı: yazma izi düşüyor, `Cari` hassas işaretli, izde kişisel veri sızıntısı **0** |
| 4 | MASAK kural motoru | ✅ **BİTTİ** | `MasakKuralMotoru` saf fonksiyon · **36 test**, tüm eşik sınırları (184.999 / 185.000 / 200.000 / 2.000.000 / 2.000.000,01 / 20.000.000,01) · 10 iş günü hafta sonu+tatil atlıyor · eşikler `appsettings:Masak`'tan |
| A | **MASAK → işlem akışı** (oyla eklendi, `SIRADAKI-ADIM-KARAR`) | ✅ **BİTTİ** | `ADIM-A-MASAK-TAMAMLAMA-RAPORU-2026-09-11.md` · 7 akış kapıda · canlı 400/200 · diskte TCKN yok · TEZGAHTAR 403 · **251/251** |
| 5 | e-Fatura / e-Arşiv | 🔒 K1 bekliyor | |
| 6 | Hurda kimlik + gider pusulası | ✅ **BİTTİ** | `ADIM-6-7-8-TAMAMLAMA-RAPORU-2026-09-12.md` · kimliksiz alış canlıda 400 · uydurma TCKN reddedildi · GP-2026-000001 üretildi · TCKN diskte şifreli (düz metin 0) · esnaf muafta %2 stopaj kesiliyor |
| 7 | POS–fatura mutabakatı | ✅ **BİTTİ** | Aynı rapor · kasıtlı uyumsuz veride fark yakalandı (3 satış 75.000, 1 fatura 40.000 → fark 35.000 + 2 uyarı + faturasız liste) · `FaturaOlusturDto.HareketId` bağı eklendi |
| 8 | KMTS opsiyonel alan | ✅ **BİTTİ** | Aynı rapor · `urunler.kmts_kodu` + `hareketler.kmts_kodu`, opsiyonel, biçim doğrulanmıyor (şema bilinmiyor) |
| 9 | Demo engelleri | ✅ **BİTTİ** (öne alındı, `SIRADAKI-ADIM-KARAR` Ö3) | `AylikOzet.gunler` eklendi — Panel VE Raporlar grafiği (ikisi de aynı alanı bekliyordu) · "Net Kasa" → "Günlük Net Akış" (Bertan: etiket yanlıştı, rakam değil) · testle kanıt: `RaporFaturaTestleri.AylikOzet_GunlukKirilimDoner` |

Her adım bitince bu tablo **kanıtla birlikte** güncellenir. Kanıtsız "bitti" yazılmaz (Anayasa Madde 1).

---

## 6. SÜRE

**Süre tahmini vermiyorum.** Geçmiş hızdan çarpan uydurmak Madde 1'e aykırı olur. Adım 1 ve 2 bitince gerçek hız ölçülür, kalan adımlara o ölçümle tahmin konur.

---

### Kaynaklar
- Hazine ve Maliye Bakanlığı — KMTS Usul ve Esaslar (PDF, tam metin tarandı): https://ms.hmb.gov.tr/uploads/sites/7/2023/12/KMTSUYGULAMASINAILISKINUSULESASLAR.pdf
- https://www.kmts.gov.tr/mevzuat-kilavuzlar (kısmen okunabildi)
- https://sarraf.pro/blog/kuyumcular-hangi-islemleri-masak-a-bildirmek-zorunda-2026-guncel-kurallar
- https://www.istanbulmalimusavirlik.net/2026-kuyumcu-muhasebesi-fis-limiti-ve-enflasyon-duzeltmesi/
- https://baf.av.tr/kuyumcularin-vergi-yukumlulukleri/
- https://birfatura.com/e-fatura-zorunlulugu/ · https://www.parasut.com/blog/e-fatura-ve-e-arsiv-zorunlulugu
