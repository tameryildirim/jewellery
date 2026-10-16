# ADR-0009 — Kritik ilişkilerde kompozit yabancı anahtar kullanılır

**Durum:** Kabul edildi (oybirliği) · **Tarih:** 2026-09-13 · **Mühür:** Tamer Bey
**Oylar:** Bertan (c) · Mehmet (c, koşullu veto) · Aşkın (c, koşullu)

> **Bu kayıt bir önceki sürümün yerine geçer.** 2026-09-13 sabahı bu ADR
> "Gözden geçirilmeli — karar bekliyor" durumundaydı. Araştırma ve oylama
> sonrası karara bağlandı.

## Bağlam

2026-09-13 ölçümü:

| Ölçüt | Değer |
|---|---|
| Birincil anahtar | 22 |
| Tekil kısıt (UNIQUE) | 4 |
| **Yabancı anahtar (FOREIGN KEY)** | **0** |
| EF'te `HasOne`/`WithMany`/`HasForeignKey` | 0 |
| Gezinme özelliği (navigation property) | 0 |

36 mantıksal `*_id` ilişkisinin bütünlüğü tamamen uygulama katmanındaydı. Bu, devralınan kod tabanından gelen bir durumdur; bilinçli bir tasarım tercihi olduğu **doğrulanamamıştır**.

## Araştırma bulguları

**Akademik konum.** Elmasri & Navathe (*Fundamentals of Database Systems*, Böl. 5) referential integrity'yi **şema-tabanlı (explicit) kısıt** sınıfına koyar; "uygulama tarafından zorlanan" sınıf, *modelin ifade gücünün ötesindeki* kurallar içindir. FK'yı uygulamaya taşımak bu sınıflandırmada bir kategori hatasıdır. **Literatürde FK'sızlığı savunan kanonik bir kaynak bulunamadı.**

**FK'sızlığın iki meşru gerekçesi var — ve ikisi de bizde yok.** Bulunan tüm birincil kaynaklar (GitHub gh-ost, PlanetScale/Vitess) gerekçelerini **sharding** ve **gölge-tablo online DDL** üzerine kurar. Bu proje tek MySQL 8.4 örneği kullanır, sharding yoktur, gh-ost kullanılmaz. Üstelik Vitess 18'den (Aralık 2023) beri tek-shard'da FK **desteklenmektedir**.

**Uygulama katmanı bütünlüğü eşzamanlılıkta yetersiz.** Franck Pachot'nun gösterdiği "önce kontrol et, sonra yaz" deseni iki eşzamanlı transaction arasında yetim kayıt üretir: *"This corrupts data, breaking the C in ACID"* ve *"This works only on a single-user database."* Bu kod tabanında desen mevcuttur (`IslemDogrulayici`).

**Basit FK kiracı sınırını KORUMAZ.** Tek sütunlu FK yalnız "bu id hedef tabloda var mı" diye sorar, **hangi kiracıya ait olduğunu sormaz**. Global sorgu filtresi de engellemez: filtre okumada çalışır, INSERT'te yazılan ham kimlik değerini denetlemez.

**Performans itirazı kanıtsız.** InnoDB için FK yazma maliyetini ölçen güvenilir bir kaynak **bulunamadı**. Bulunan tek rakam (%18) farklı bir motora aittir (NDB Cluster, 2013) ve bu senaryoya uygulanamaz. Anayasa md.1 gereği "FK yavaşlatır" bir gerekçe olarak kullanılmamıştır.

## Karar

**(c) Kritik ilişkilerde kısmi yabancı anahtar — aşamalı, kompozit.**

1. **Tüm kiracılı FK'lar KOMPOZİTTİR:** `(hedef_id, tenant_id) → hedef(id, tenant_id)`. Tek sütunlu FK, kiracı koruması iddiasıyla eklenmez (Mehmet, mutlak veto).
2. **Referans davranışı istisnasız `ON DELETE RESTRICT` / `ON UPDATE RESTRICT`.** Sistemde ana veri zaten **hard-delete edilmez** (hepsi `Aktif` bayrağıyla soft-delete) — RESTRICT sıfır davranış değişikliği demektir.
3. **Append-only tablolarda CASCADE ve SET NULL YASAK** (`denetim_kayitlari`, `masak_*`, `gider_pusulalari`). Bu teknik değil **hukuki** bir kısıttır: CASCADE, bir üst kaydın silinmesiyle MASAK kanıtını ve denetim izini yok eder.
4. **Gezinme özelliği (navigation property) EKLENMEZ.** FK, `HasOne<T>().WithMany()` ile nav property olmadan tanımlanır. Gerekçe (Bertan): `Include` kazanmak hedef değildir; iki ayrı model önbelleği ve OR'suz SQL tasarımı bilinçli bir performans kararıdır ve nav property bu sorgu şeklini değiştirme riski taşır. **FK ile navigation ayrı kararlardır.**
5. **Kapsam dışı — FK kurulamaz:**
   - `denetim_kayitlari.varlik_id` → **polimorfik**; hedef tablo `varlik_turu` ile belirlenir, tek hedef yoktur.
   - `urunler.ayar_id`, `gider_pusulalari.ayar_id` → **hedef tablo yoktur**; ayar (K14/K18/K22/K24) kodda sabittir.
   → Bu yüzden **"tüm ilişkilere FK" sözü verilemez.** Taranabilir ilişki sayısı: **36**.
6. **Uygulanmaz:** `faturalar.banka_hareket_id` ↔ `banka_hareketleri.fatura_id` **karşılıklı döngüsü**. Ekleme sırasını ve `mysqldump` geri yükleme sırasını kırar; kazanç maliyeti karşılamaz (Bertan).

### Sıralama (dalgalar)

| Dalga | Kapsam | Not |
|---|---|---|
| **0** | Yetim + kiracı sızıntısı taraması | ✅ **YAPILDI** — aşağıya bakınız |
| 1 | `tenant_id → tenants(id)` (20 tablo, basit FK) | Yeni indeks gerekmez; `tenant_id` mevcut indekslerde öncü sütun |
| 2 | Ana veri: `stoklar`, `cari_bakiyeler`, `kasalar`, `refresh_tokenler`, `kullanicilar.sube_id`, `kasa_hareketleri.kasa_id`, `banka_hareketleri.banka_hesap_id` | kompozit |
| 3 | Defter: `hareketler`'in 6 FK'sı | en sıcak tablo, ayrı ölçüm |
| 4 | Uyum: `masak_*`, `gider_pusulalari`, `denetim_kayitlari.kullanici_id` | RESTRICT zorunlu |

**UNIQUE `(id, tenant_id)` eklenmesi gereken 11 hedef tablo** (Bertan): `subeler`, `kullanicilar`, `cariler`, `urunler`, `kur_snapshots`, `kasalar`, `banka_hesaplari`, `faturalar`, `hareketler`, `masak_kimlik_tespitleri`, `masak_beyanlari` (+ `gider_pusulalari` self-ref için).

---

## ⛔ DALGA 0 SONUCU — KİRACI SIZINTISI BULUNDU

`dotnet/yetim-tara.sh` çalıştırıldı (2026-09-13 22:46, `kuyumcu_net`):

```
taranan ilişki: 36 · toplam yetim: 0 · toplam kiracı sızıntısı: 2
```

| İlişki | Bulgu |
|---|---|
| `hareketler.cari_id` | **1 satır** başka kiracının carisine bağlı |
| `kasa_hareketleri.kasa_id` | **1 satır** başka kiracının kasasına bağlı |

### Bulgunun incelemesi

```
kasa_hareketleri: tutar 99.999,00 · tarih 2026-09-09 21:19:12.699
                  kasa adı "Kurban Kasa"
hareketler      : SATIS 77.777,00 · tarih 2026-09-09 21:19:12.725
                  cari adı "Kurban Musteri"

yazan kiracı  : ByGatech Kuyumculuk   (oluşturma 2026-09-09 21:08)
kurban kiracı : Demo Kuyumculuk       (oluşturma 2026-09-09 21:02)
üçüncü kiracı : "Düzeltme Sonrası Kuyumculuk" (oluşturma 2026-09-09 22:04)
```

**Hüküm: bu satırlar AKTİF bir açığın değil, kendi sızma testimizin kalıntısıdır.**

Gerekçe:
1. Kayıt adları (`Kurban Kasa`, `Kurban Musteri`) ve tutarlar (99.999 / 77.777) `DENETIM-RAPORU-2026-09-10.md`'de belgelenen Faz 0 sızma testinin birebir aynısıdır.
2. İki kayıt arasında **26 milisaniye** vardır — elle üretilmiş bir istismar dizisi.
3. Aynı gün 22:04'te açılan üçüncü kiracının adı *"Düzeltme Sonrası Kuyumculuk"*tur — düzeltmenin doğrulandığı kiracı.
4. **Karşı kanıt:** Aynı tarama `kuyumcu_test` veritabanında, **385 testin tamamı koştuktan sonra** (içlerinde çapraz kiracı saldırı testleri de var) **TEMİZ** dönmektedir. Yani bugünkü kod sızdırmıyor.

### Alınan tutum

Bu satırlar **silinmemiştir.** Mehmet'in K3 koşulu: *"hedefi başka kiracıda çıkan her satır güvenlik olayı olarak ayrı raporlanır, sessizce silinmez/düzeltilmez."*

Silinmesi Dalga 1'in ön koşuludur (MySQL 8.4: `ADD FOREIGN KEY` mevcut veride kısıtı ihlal eden satır varsa **hata verir**) ve ayrı, bilinçli bir adım olarak yapılacaktır — öncesinde doğrulanmış yedek alınır (`dotnet/yedek-al-ve-dogrula.sh`).

**Bu bulgu tarayıcının değerinin kanıtıdır:** ilk çalıştırmasında, iki yıl boyunca fark edilmeyebilecek bir kalıntıyı bulmuştur.

---

## Yol boyunca kapatılan gerçek açık

Oylama sırasında Mehmet, `MasakModulu.SupheliOlustur` içinde somut bir açık tespit etti ve doğrulandı:

`HareketId` ve `CariId` kiracıya karşı doğrulanıyor, ancak **`KimlikTespitiId` kullanıcı gövdesinden gelip doğrulanmadan yazılıyordu.** Yani bir kiracı, başka bir kiracının MASAK kimlik tespiti kaydını kendi **append-only** şüpheli işlem bildirimine bağlayabiliyordu — ve kayıt append-only olduğu için sonradan düzeltilemezdi.

Düzeltildi (`IslemDogrulayici.KimlikTespitiDogrula`) ve **mutasyonla kanıtlandı**: doğrulama kaldırıldığında 4 testin 2'si kırmızıya döndü, meşru yollar yeşil kaldı.

## Sonuçlar

**Olumlu**
- Kiracı sınırı veritabanı seviyesine iner; interceptor'ı atlayan yollarda (raw SQL, `ExecuteUpdate/Delete`, migration betiği, doğrudan DB erişimi) **tek kalan koruma** budur.
- Eşzamanlılık kaynaklı yetim kayıt sınıfı yapısal olarak kapanır.
- Maliyeti bugün ömrünün **en düşük** noktasındadır: sistem henüz yayında değildir, üretim veri hacmi yoktur.

**Olumsuz / bedeli**
- 11 tabloya `(id, tenant_id)` UNIQUE indeksi eklenir. Hesap (ölçüm değil): 1M satırlık `hareketler` için ≈ 126 MB ek indeks.
- Bugün sessizce yetim üreten herhangi bir kod yolu, FK'dan sonra `errno 1452` ile **patlayacaktır**. Bu istenen davranıştır ama regresyon riski taşır.
- EF'te aynı `tenant_id` sütunu birden çok kompozit FK'ya girer; change-tracker davranışı **doğrulanmamıştır** (Bertan'ın 1 numaralı riski). Dalga 2 öncesi tek tablolu spike şarttır.

## Ne zaman yanlış olur

Sharding'e geçilirse veya gh-ost tipi gölge-tablo DDL zorunlu hâle gelirse bu karar yeniden ele alınır. O durumda FK kapsamı shard-içi kritik ilişkilerle sınırlanır.

## Kalite kapısı (Aşkın)

Her FK için: yetim taraması çıkış kodu 0 · migration ileri **ve geri** koşmuş · `dotnet test` 0 başarısız · `denetim.sh` TEMİZ · `information_schema.referential_constraints` sayımı beklenenle eşleşiyor · **mutasyon kanıtı** (FK'yı kaldır → test kırmızıya dönmeli) · her kompozit FK için "A kiracısının kaydını B'ninkine bağlamayı dene → DB reddetmeli" negatif testi.

**`SET foreign_key_checks=0` ile FK eklemek YASAKTIR** — MySQL 8.4 bunu yeniden açarken geçmiş veriyi taramaz; kısıtı yalan söyleyen bir şema üretir.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
