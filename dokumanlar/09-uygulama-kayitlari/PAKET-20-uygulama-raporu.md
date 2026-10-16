# Paket 20 — Metal tipi genişletme (platin · paladyum · rodyum · çelik) + saflık tabloları — **FAZ A** uygulama raporu

> Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-17
> Durum: **BİTTİ (Faz A)** · Faz B (kotasyon kolonları + maden kuru) KAPSAM DIŞI — § Açık kalanlar.

---

## 1. Bir cümlede

Sistem artık "has gram = **altın** has gramı" değişmezini kodun tamamında zorluyor: platin/paladyum ürünler altın kuruyla fiyatlanmıyor (fail-closed `KOTASYON_YOK`), çelik ürünün sahte has gramı üretilmiyor, gümüş emaneti altın bakiyesine yazılmıyor, gümüş yatıran müşteri altın çekemiyor ve raporlar farklı madenlerin gramlarını tek sayıda toplamıyor.

**Kapı sayıları:** derleme 0 hata / 0 uyarı · `dotnet test` **2020/2020 yeşil** (önce 1964) · `dotnet/denetim.sh` **DENETİM TEMİZ** · 36 migration · test kiracısı silindi, `tenants` 8 → 8.

---

## 2. Yapılanlar

### 2.1 Maden modeli (Kuyumcu.Core)
- `Maden` sınıfına **`Celik`** (kıymetsiz) ve **`Rodyum`** sabitleri eklendi. `Maden.Hepsi` = ALTIN · GUMUS · PLATIN · PALADYUM · **CELIK**. **Rodyum `Hepsi` dizisinde BİLEREK YOK** (K20-7): kaplamadır, stok madeni değildir — dizide olsaydı "rodyum stoğu" diye var olmayan bir bakiye doğardı. `Maden.RodyumKaplanabilir` = ALTIN · PLATIN · PALADYUM.
- `Ayar` record'una **`Kiymetli`** ve **`HasHesaplanir`** özellikleri eklendi. Çelik ayarı: `CELIK`, milyem 0, `Kiymetli=false`, `HasHesaplanir=false`.
- `HasHesap.ToHasGram` **kıymetsiz ayarda 0 döner** — elle girilen "gerçek milyem" bile sahte has üretemez (test: `CelikteGercekMilyemBileHasUretmez`).
- **`Ayarlar.Desen` artık ÜRETİLİYOR** (`Tumu.Keys`'ten, uzun anahtar önce sıralı) + `DesenTam` (çıpalı) + `Ayarlar.Gecerli()`.
- **YENİ `AyarIdAttribute`** (Kuyumcu.Core): DTO doğrulaması artık düzenli ifade sabitine değil **sözlüğe** bağlı. Sapma: `[RegularExpression(Ayarlar.Desen)]` ile bağlamak MÜMKÜN DEĞİLDİ (öznitelik argümanı `const` olmak zorunda, desen ise artık üretiliyor) → § 3.1.
- **YENİ `UrunTipleri`** (Kuyumcu.Core): ürün grubu listesi tek kaynak; `Sabitler.UrunTipleri` artık buna bağlı.

### 2.2 Fail-closed maden kapısı
- **YENİ `Kuyumcu.Api/Modules/Urun/MadenKapisi.cs`**
  - `UrunDogrula(tip, ayarId, rodyumKaplama, satisFiyati)`: ayar geçerli mi · tip↔maden tutarlı mı (GUMUS→gümüş, PLATIN→platin, PALADYUM→paladyum, CELIK→çelik, ZIYNET/KULCE/HURDA/SIKKE→altın; PIRLANTA/SAAT/DEGERLI_TAS serbest) · kıymetsiz ayarda satış fiyatı **zorunlu** · rodyum yalnız altın/platin/paladyumda. Her ihlal Türkçe, sebebi yazan `BadRequestException`.
  - `HareketDogrula(hareket)`: has bacağı ≠ 0 ise maden dolu ve geçerli olmak ZORUNDA. **Yalnız `HareketFabrikasi.Ekle` içinde çağrılır** (denetim.sh sayar).

### 2.3 Veri modeli + migration
- `hareketler.maden` VARCHAR(10) NOT NULL DEFAULT `'ALTIN'` + indeks `(tenant_id, maden, tarih)`.
- `urunler.rodyum_kaplama` TINYINT(1) NOT NULL DEFAULT 0.
- `Urun.Maden` / `Urun.MadenAdi` **`[NotMapped]` türetilmiş** özellikler — ayar ile maden iki yerde tutulmuyor (K20-8).
- Migration: `20260917112823_HareketMaden` — geri doldurma SQL'i + varsayım + sevk öncesi ölçüm yorumda yazılı (§ 4).

### 2.4 Defter, emanet, satış, sayım, hurda
- `HareketFabrikasi.Ekle` → `MadenKapisi.HareketDogrula` (K20-1).
- **SatisModulu**: 8 hareket yazma noktasına ürünün/ayarın madeni damgalandı (SATIS · ALIS · HURDA_ALIS · TRANSFER · karma fiş SATIS · SATIS_IADE · ALIS_IADE · TAKAS). ISCILIK satırında has = 0 → maden bacağı yok.
- **TAKASTA MADEN KARIŞTIRMA YASAĞI (yeni, K20-3):** TAKAS tek bir NET has bacağı yazar (`eskiHas − yeniHas`). Gümüş verip altın almak bu çıkarmada iki madeni tek sayıda birleştirirdi → 400 ile reddediliyor, işlem iki ayrı kalem olarak girilmesi isteniyor.
- **EmanetModulu**: giriş hareketi ayarın madenini yazıyor; `cari_bakiyeler.has_gram_bakiye`ye **yalnız ALTIN** dokunuyor (K20-2); `EmanetNet` maden parametresi alıyor; çıkış `maden` taşıyor ve o madenin netiyle karşılaştırılıyor (K20-9); çelik emaneti reddediliyor (has bacağı yok); yanıt `madenBakiyeleri` + `mevcutHasGramBakiyeMadeni` + açık not taşıyor.
- **StornoServisi**: ters satır kaynağın madenini birebir tersliyor.
- **SayimModulu**: `netFarkHas` yalnız altın, `madenDagilimi` eklendi; sayım düzeltme hareketi ürünün madenini taşıyor.
- **HurdaModulu / gider pusulası**: belgedeki mal cinsi artık **ayarın madeninden** üretiliyor ("Hurda gümüş (G925 — 925/1000)"); eskiden gümüş hurdada belgeye "Hurda altın" yazıyordu (VUK md.234 malın cinsini ister).

### 2.5 Fiyat
- **`FiyatMotoru` kural 2 yeniden yazıldı:** `urun.AyarId.StartsWith('G')` KALDIRILDI → `Ayarlar.MadenBul(urun.AyarId)`; ALTIN dışı maden has dalına HİÇ girmiyor. Sabit fiyat yolu açık (altın dışı ürün elle girilen fiyatla satılır). Yuvarlama, kampanya, indirim, geçersiz kılma kuralları DEĞİŞMEDİ.
- `FiyatModulu` teklif ucu: yeni sebep kodu **`KOTASYON_YOK`** + `maden`/`madenAdi` (UX-S30 — "fiyat yok" demek yetmez, sebebi söylenir).

### 2.6 Raporlar
- `RaporServisi`'ne üç yardımcı: `MadenDagilimi` · `AltinHas` · `MadenUyarisi` (stok raporundaki desenin aynısı).
- Gün sonu (`satis` · `alis` · `hurda` · dönem geneli) ve aylık özet: `toplamHas`/`netHasGramHareketi` **YALNIZ ALTIN**, yanına `madenDagilimi` + `madenUyarisi`. Gruplama mevcut listeler üzerinde yapılıyor — **yeni sınırsız `ToListAsync` AÇILMADI** (TB-19).

### 2.7 Dışa aktarım
- Ürün CSV'sine `maden` + `rodyumKaplama`, hareket CSV'sine `maden` kolonu (ADR-0014 biçimi korundu).

### 2.8 Sözlük (TR + EN)
`Urun` bölümüne 15 anahtar: Maden · Madenler · TumMadenler · Saflik · SaflikTablosu · RodyumKaplama(+İpucu) · HasHesaplanmaz(+İpucu) · KotasyonYok(+İpucu) · MadenBakiyeleri · MadenDagilimi · MadenUyarisi; `Rapor` bölümüne HareketSayisi. Ekranda çıplak metin yok.

### 2.9 Ekranlar — **Dokunulan sayfalar** (MÜHÜR 2 tek dokunuş)
| Sayfa | Ne yapıldı |
|---|---|
| `Pages/Urunler.razor` | Ürün grubu ve maden listeleri **tek kaynaktan** (Core); form akışı **önce maden → o madenin saflık tablosu** (`PT950 — 950 Platin (950‰)`); rodyum onay kutusu (kaplanamayan madende disabled + sebep); çelikte "has hesaplanmaz" notu + fiyatsız Kaydet'te **alan altı Türkçe mesaj**; ürün grubu maden dayatıyorsa maden seçici kilitli + `#u-maden-kilit` sebebi; maden filtresi (`#urun-maden-filtre`); düzenleme formu **aynı alan kümesini** taşıyor (UX-S26); konsinye kutusuna `#u-konsinye` kimliği verildi |
| `Pages/Emanet.razor` | "Elimizdeki has" tek rakam yerine **maden bazlı satırlar**; çıkışta maden seçimi (tek maden varsa seçici gizli — UX-S22); iade ayarı seçilince maden **ayardan türetiliyor** (UX-S24); fazla iade kontrolü maden bazında |
| `Pages/Raporlar.razor` | **Maden dağılımı tablosu** + "toplam has yalnız altındır" uyarısı ekranda görünür |
| `Pages/UrunEtiketleri.razor` | Etikette **maden + milyem** ("Platin 950"); rodyum ibaresi **yapılandırmadan**, varsayılan KAPALI |
| `wwwroot/css/app.css` | `.urun-etiket-maden` (sabit renk yok) |

İstemci yanıt sözleşmeleri Pages'e değil **`Kuyumcu.Core/Sozlesmeler/MadenSozlesmeleri.cs`**'e yazıldı (S-2 / MÜHÜR 5 KOŞUL 3 — denetim.sh "Pages içinde istemci record" tavanı 122'de kaldı).

### 2.10 denetim.sh — üç yeni sayaç
1. `Elle yazılmış ayar deseni (yasak)` = **0** (`"K8|K14` dizgesi kodda yok)
2. `MadenKapisi.HareketDogrula (yalnız fabrika)` = **1** ve yalnız `Data/HareketFabrikasi.cs`
3. `HasHesap.ToHasGram (beyaz liste 4)` = **4**

---

## 3. Sapmalar ve gerekçeleri

### 3.1 `Ayarlar.Desen` öznitelik argümanı olamaz → `AyarIdAttribute`
Spesifikasyon "Desen `Tumu.Keys`'ten ÜRETİLİR" diyor ve DTO'ların ona bağlanmasını istiyor. İkisi birlikte **derlenmiyor**: `[RegularExpression(...)]` argümanı `const` olmak zorunda, üretilen desen `static readonly`. Spesifikasyonun HEDEFİ (tek kaynak, yeni ayar tek dosyada eklenir) korunarak doğrulama `AyarIdAttribute` ile doğrudan `Ayarlar.Tumu` sözlüğüne bağlandı. Desen ve `DesenTam` mesaj/test için duruyor. Dört DTO'daki elle yazılı desenin **hepsi** kaldırıldı (denetim.sh sayacı 0).

### 3.2 Ekran listeleri `GET /urunler/madenler` yerine Core sabitinden
Spesifikasyon "sabit liste arayüze KOPYALANMAZ" diyor ve ucu öneriyor. **Uç açıldı ve çalışıyor** (`[Authorize]`, Mehmet koşulu), ancak Blazor ekranı listeyi **aynı Kuyumcu.Core derlemesinden** okuyor: bu bir kopya DEĞİL, aynı tek kaynağın kendisidir ve derleme zamanında bağlıdır (HTTP kopyasından daha güçlü). UX-4c mührü de üç ekranın `Ayarlar.EkranSecenekleri`'ne bağlı kalmasını zorunlu kılıyor (`UX4cEkranTestleri.M10`). Uç, POS/harici istemciler ve etiket ibare politikası için kullanılıyor (UrunEtiketleri onu çağırıyor).

### 3.3 Rota adı: `/api/v1/urunler/madenler`
Spesifikasyon `/api/v1/urun/madenler` demiş; koddaki denetleyici rotası `api/v1/urunler`. Mevcut rota ailesine uyuldu.

### 3.4 Çelik ayar kodu: tek `CELIK`
316L/904L gibi kalite kodlarının perakende kuyumcuda "ayar" karşılığı olup olmadığı **doğrulanmadı** → tahmin edilmedi (Anayasa md.1). Tek kod açıldı; kalite kodu gerekirse `Ayarlar.Celik` sözlüğüne eklenir, desen/ekran/filtre **kendiliğinden** genişler. → § 8 kurucuya.

### 3.5 Takasta maden değiştirme yasağı (spesifikasyonda yok, eklendi)
TAKAS tek net has bacağı yazıyor; iki maden arası takas K20-3'ü doğrudan ihlal ederdi. Fail-closed reddedildi, mesajda doğru yol yazıyor.

### 3.6 Emanet: çelik/kıymetsiz maden reddediliyor
Emanet bakiyesi has gram üzerinden izleniyor; çeliğin has gramı yok → sessizce 0 gramlık emanet yazmak yerine açık hata veriliyor.

### 3.7 `mevcutHasGramBakiye` sözleşme daralması — Web+API tek sevk
Alan adı korundu, anlamı **yalnız ALTIN**'a daraldı. Yanıta `mevcutHasGramBakiyeMadeni` + `mevcutHasGramBakiyeNotu` bayrakları eklendi; Emanet ekranı aynı sevkte güncellendi; `madenBakiyeleri` boş gelirse ekran ALTIN için bu alana düşüyor (eski/boş yanıtta düğme sebepsiz kilitlenmesin). Mevcut emanet HTTP/ekran testleri güncellendi ve yeşil.

### 3.8 Faz B kapsam dışı
Kotasyon kolonları (`gumus/platin/paladyum_gram_fiyati`), KurModulu ve `MadenKotasyonTestleri` **yapılmadı** (görev: "yalnız Faz A"). Faz A'nın davranışı: altın dışı maden **sabit fiyatla** satılır, kotasyon yoksa `KOTASYON_YOK`. → § 7.

---

## 4. Migration ve geri doldurma (Bertan + Mehmet koşulu)

**Migration:** `20260917112823_HareketMaden` — `.cs` 4.322 bayt, `.Designer.cs` 132.631 bayt (ikisi de > 0).
`Up()`: `urunler.rodyum_kaplama` (default 0) + `hareketler.maden` (**default `'ALTIN'`**) + indeks `(tenant_id, maden, tarih)` + geri doldurma `UPDATE`.

**SEVK ÖNCESİ ÖLÇÜM (`kuyumcu_net`, `Up` çalıştırılmadan ÖNCE — açık soru 6 kapatıldı):**

| Ölçü | Değer |
|---|---|
| hareketler toplam | 128 |
| ürünsüz satır | 34 (bunların **21**'inin has bacağı ≠ 0) |
| — kırılım | HURDA_ALIS 7 · EMANET_GIRIS 11 · EMANET_CIKIS 3 |
| altın dışı ayarlı ürün | **4** (hepsi G925) |
| altın dışı ayarlı üründen doğan hareket | **3** → GUMUS etiketlendi |
| ayarı bir satıştan SONRA güncellenmiş ürün | **7 — ÜST SINIR** |

⚠ **7 rakamı üst sınırdır ve tahmin değildir:** `urunler.guncellendi` damgası ayar dışı bir alan değiştiğinde de ilerler; kaçının **ayarının** fiilen değiştiği veri modelinde saklanmıyor. Gerçek sayı bilinmiyor, uydurulmadı.

**Varsayım (K20-10):** ürünü olan satır ürünün **bugünkü** ayarından, ürünü olmayan satır `'ALTIN'` etiketlenir. Ürünsüz 21 satırın tamamı altın akışıydı (hurda + emanet); hurda ve emanetin ayarı harekette saklanmadığı için bu satırlar başka türlü etiketlenemezdi. Varsayım ve ölçüm **migration yorumunda** ve bu raporda yazılı. Migration `DenetimKaydi` düşürmez (SaveChanges dışıdır) — bilinçli.

**Uygulama sonrası kanıt (`kuyumcu_net`):**
```
maden dağılımı:  ALTIN 125 · GUMUS 3
gümüş ürünlü satır ALTIN kalmış mı: 0
__EFMigrationsHistory: 36 (35 → 36)
urunler.rodyum_kaplama: tinyint(1) NOT NULL DEFAULT 0
```

---

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli (Maden* + FiyatMotoru) | 59 | 59 | 0 | 0 |
| Filtreli (MadenUcTestleri, HTTP) | 10 | 10 | 0 | 0 |
| Filtreli (UrunEkranTestleri, bUnit) | 33 | 33 | 0 | 0 |
| **TAM SÜİT** | **2020** | **2020** | **0** | **0** |

**Yeni test sayısı: 56** (önce 1964 → sonra 2020).

- `Kuyumcu.Tests/MadenTestleri.cs` (YENİ, 21) — saf: ayar→maden eşlemesi · çelikte ToHasGram 0 · gerçek milyem bile has üretmez · rodyum stok madeni değil · desen tüm ayarları kapsar (davranışla: `IsMatch("K23")=false`) · uzun anahtar önce · `AyarIdAttribute` · MadenKapisi'nin 6 kuralı · HareketDogrula'nın 3 hâli.
- `Kuyumcu.Tests/MadenDefteriTestleri.cs` (YENİ, 12) — DB: madensiz hareket yazılamaz · platin satışında maden PLATIN · gümüş emaneti altın bakiyesini değiştirmez · altın emaneti değiştirir · gümüş yatıran altın çekemez · madensiz çıkış ALTIN sayılır · emanet yanıtı maden bakiyeleri · çelik emaneti reddedilir · dönem raporu madenleri toplamaz · tek madende uyarı yok · maden değiştiren takas reddedilir · **migration SQL'i dizge düzeyinde** (Aşkın koşulu: geri doldurma taze şemada ölçülemez).
- `Kuyumcu.Tests/MadenUcTestleri.cs` (YENİ, 10) — HTTP: `/urunler/madenler` anonime 401 · yetkiliye 200 (saflık tablosu + etiket politikası) · tip↔ayar 400 · çelik fiyatsız 400 / fiyatlı 201 (has=0) · platin 201 (has=4,75) · rodyum yaz/koru/kaldır · rodyum gümüşte 400 · geçersiz ayar 400 (ürün **ve** emanet ucunda) · maden değişiminde denetim izi · maden filtresi.
- `FiyatMotoruTestleri.cs` (+5) · `UrunEkranTestleri.cs` (+8, bUnit).
- Kültür tuzağı: Türkçe hata mesajları **JSON ayrıştırılarak** sınanıyor (`System.Text.Json` `ü` kaçışı yazar; ham gövdede "gümüş" ASLA bulunmaz).
- Sahte API'ler: `/urunler/madenler` üç fikstürde de **gerçekçi zarf** döndürüyor (genel `/urunler` dalından ÖNCE — aksi hâlde dizi dönüp JsonException'a yol açardı).

---

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/paket20/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli koşum → yedek geri kopyalandı → **`cmp` ile birebir aynı** kanıtlandı. Son mutasyondan sonra temiz `dotnet build`: **0 hata / 0 uyarı**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M20-1 | `MadenKapisi.UrunDogrula` tip↔maden kontrolü devre dışı | `MadenTestleri.TipVeAyarMadeniTutarsizsaRed`, `MadenUcTestleri.Gumus_tipinde_ALTIN_ayari_400_doner` | AYNI |
| M20-2 | `FiyatMotoru` maden kontrolü → `AyarId.StartsWith('G')` | `FiyatMotoruTestleri.PlatinUrunAltinKuruylaFiyatlanmaz`, `.PaladyumUrunAltinKuruylaFiyatlanmaz`, `.CelikUrunYalnizSabitFiyatlaFiyatlanir` | AYNI |
| M20-3 | `HareketFabrikasi.Ekle` içindeki `MadenKapisi.HareketDogrula` silindi | `MadenDefteriTestleri.MadensizHareketYazilamaz` | AYNI |
| M20-4 | `EmanetModulu` `BakiyeGuncelle`'nin `maden == ALTIN` koşulu kaldırıldı | `MadenDefteriTestleri.GumusEmanetiAltinBakiyesiniDegistirmez`, `.GumusYatiranAltinCekemez` | AYNI |
| M20-5 | Rapor `AltinHas` maden süzgeci kaldırıldı (tek toplam) | `MadenDefteriTestleri.DonemRaporuMadenleriToplamaz` | AYNI |
| M20-6 | `Ayarlar.Desen` elle yazılı sabite döndürüldü, ÇELİK eksik bırakıldı | `MadenTestleri.DesenTumAyarlariKapsar` | AYNI |
| M20-7 | `EmanetNet`'in maden süzgeci etkisizleştirildi | `MadenDefteriTestleri.GumusYatiranAltinCekemez`, `.MadensizCikisAltinSayilir`, `.EmanetYanitiMadenBakiyeleriniTasir` | AYNI |
| M20-8 | `HasHesap.ToHasGram` kıymetsiz kontrolü kaldırıldı | `MadenTestleri.CelikteGercekMilyemBileHasUretmez` | AYNI |
| M20-9 | Çelikte satış fiyatı zorunluluğu kaldırıldı | `MadenTestleri.CelikUrunFiyatsizKaydedilemez`, `MadenUcTestleri.Celik_urun_sabit_fiyatsiz_400_fiyatlı_201_doner` | AYNI |
| M20-10 | Rodyum maden kısıtı kaldırıldı | `MadenTestleri.RodyumKaplamaCelikUrundeRed`, `MadenUcTestleri.Rodyum_kaplama_gumus_urunde_400_doner` | AYNI |
| M20-11 | Takas maden karıştırma yasağı kaldırıldı | `MadenDefteriTestleri.MadenDegistirenTakasReddedilir` | AYNI |
| M20-12 | Satış hareketinin maden damgası `ALTIN`'a sabitlendi | `MadenDefteriTestleri.PlatinSatisindaHareketMadeniPLATIN`, `.DonemRaporuMadenleriToplamaz` | AYNI |

**Hiçbir testi kırmayan mutasyon çıkmadı** — 12/12 kırmızı üretti.

---

## 7. `dotnet/denetim.sh` çıktısı

```
✓ Elle yazılmış ayar deseni (yasak)             0
✓ MadenKapisi.HareketDogrula (yalnız fabrika)   1
✓ HasHesap.ToHasGram (beyaz liste 4)            4
✓ Hareketler.Add (yalnız HareketFabrikasi)      1
✓ IgnoreQueryFilters (yasak)                    0
✓ Pages içinde istemci record tanımı          122 (tavan 122)
DENETİM TEMİZ.
```

---

## 8. Canlı doğrulama (localhost:5198, `kuyumcu_net`, kiracı `PAKET20-TEST Kuyumculuk`)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /urunler/madenler` **anonim** | 401 | **401** | curl |
| 2 | `GET /urunler/madenler` yetkili | 200, 5 maden + saflık tablosu | **200** · `ALTIN GUMUS PLATIN PALADYUM CELIK` · `CELIK (milyem 0, kiymetli=False, hasHesaplanir=False)` · `PT850/900/950/999` · `etiket.rodyumIbaresiGoster=False` · RODYUM listede YOK | curl |
| 3 | Platin alyans PT950 5 g | 201, maden PLATIN | **201** · `maden=PLATIN` · `hasGrami=4.75` | curl |
| 4 | Gümüş kolye G925 12 g | 201, maden GUMUS | **201** · `maden=GUMUS` · `hasGrami=11.1` | curl |
| 5 | Çelik saat **fiyatsız** | 400 | **400** "Çelik ürününde satış fiyatı zorunludur…" | curl |
| 6 | Çelik saat 2.500 TL | 201, **has 0** | **201** · `maden=CELIK` · `hasGrami=0.0` | curl + SQL |
| 7 | Rodyumlu beyaz altın K18 | 201, `rodyumKaplama=true` | **201** | curl + SQL |
| 8 | Tip GUMUS + ayar K22 | 400 | **400** "Ürün grubu 'GUMUS' gümüş bekler, seçilen ayar (K22 — 22 Ayar) altın ayarıdır…" | curl |
| 9 | **Kotasyonsuz platin teklifi** | TL fiyat ÜRETİLMEZ | **200** `{"teklifVar":false,"sebep":"KOTASYON_YOK","maden":"PLATIN"}` — eski kodda 4,75 × 6.787,01 × 1,05 = **≈33.845 TL fahiş fiyat** üretiliyordu | curl |
| 10 | Kotasyonsuz gümüş teklifi | üretilmez | **`KOTASYON_YOK`, maden GUMUS** | curl |
| 11 | Çelik teklifi (sabit fiyat) | 2.500 | **`teklifVar:true, listeFiyati:2500.00, kaynak:URUN_SABIT_FIYAT`** | curl |
| 12 | **REGRESYON** altın K18 3 g | 2,25 × 6.787,01 + %5 | **16.034,31** (`hasDegeri 15.270,77 + marj 763,54`) — elle hesapla birebir | curl |
| 13 | Üç madenden satış + gün sonu | `toplamHas` yalnız altın, `madenDagilimi` 3 satır | **`satis.toplamHas=2.25`** · `madenDagilimi = [GUMUS 11.1, PLATIN 4.75, ALTIN 2.25]` · `netHasGramHareketi=9.0` (yalnız altın) · `madenUyarisi` dolu | curl |
| 14 | 50 g G925 emanet | 200, maden GUMUS | **200** `hasGrami=46.25, maden=GUMUS` | curl |
| 15 | **10 g ALTIN emanet çıkışı** | REDDEDİLİR | **400** "Yetersiz **altın** emaneti. Mevcut: 0.0000 has gram, Talep: 10" | curl |
| 16 | `GET /emanet/{cariId}` | altın 0 + gümüş ayrı | `mevcutHasGramBakiye=0.0` · `madeni=ALTIN` · not: "…YALNIZ altın emanetidir…" · `madenBakiyeleri=[{GUMUS, 46.25}]` | curl |

**SQL kanıtları (test kiracısı, silinmeden önce):**
```sql
-- 1) hareket maden dağılımı
EMANET_GIRIS   GUMUS   1   46.2500
SATIS          ALTIN   1   -2.2500
SATIS          GUMUS   1  -11.1000
SATIS          PLATIN  1   -4.7500
SAYIM_DUZELTME ALTIN   1   11.2500
SAYIM_DUZELTME GUMUS   1   55.5000
SAYIM_DUZELTME PLATIN  1   23.7500
-- 2) gümüş ürünlü satır ALTIN kalmış mı → 0
-- 3) cari_bakiyeler.has_gram_bakiye (gümüş emaneti sonrası) → 0.000000  ✅ K20-2
-- 4) rodyum_kaplama=1 → P20-R (K18)
-- 5) çelik ürün → P20-C2, CELIK, has_grami=0.000000, satis_fiyati=2500.00
```

**Temizlik:** API PID 49966 `kill` edildi → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır**. 5199 (kurucunun inceleme sunucusu) **çalışmaya devam ediyor, dokunulmadı**. Test kiracısı `tenant_id` kolonu olan 43 tablodan ve `tenants`'tan silindi:
```
tenants = 8 (başlangıç 8)  ·  hareketler = 128 (başlangıç 128)  ·  urunler = 51 (başlangıç 51)
maden dağılımı = ALTIN 125 · GUMUS 3 (değişmedi)
```
`QA-TEST-1/2/3` kiracıları **dokunulmadı**.

---

## 9. Değişen dosyalar

**Yeni (7):**
```
dotnet/Kuyumcu.Core/Domain/AyarIdAttribute.cs
dotnet/Kuyumcu.Core/Domain/UrunTipleri.cs
dotnet/Kuyumcu.Core/Sozlesmeler/MadenSozlesmeleri.cs
dotnet/Kuyumcu.Api/Modules/Urun/MadenKapisi.cs
dotnet/Kuyumcu.Api/Modules/Urun/EtiketAyarlari.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260917112823_HareketMaden.cs (+ .Designer.cs)
dotnet/Kuyumcu.Tests/MadenTestleri.cs · MadenDefteriTestleri.cs · MadenUcTestleri.cs
```
**Değiştirilen (24):**
```
dotnet/Kuyumcu.Core/Domain/Ayarlar.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Api/Data/Entities.cs · KuyumcuDbContext.cs · HareketFabrikasi.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Program.cs · appsettings.json
dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs
dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs · StornoServisi.cs
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs · FiyatModulu.cs
dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs
dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs
dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs
dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs
dotnet/Kuyumcu.Web/Pages/Urunler.razor · Emanet.razor · Raporlar.razor · UrunEtiketleri.razor
dotnet/Kuyumcu.Web/wwwroot/css/app.css
dotnet/denetim.sh
dotnet/Kuyumcu.Tests/FiyatMotoruTestleri.cs · UrunEkranTestleri.cs · EkranTestleri.cs · UX1C1EkranTestleri.cs
```
(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısındaki diğer kalemler bu paketten ÖNCE de değişik/izlenmemiş durumdaydı.) **COMMIT YAPILMADI.**

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Faz A ikiye bölünür (A1 model/kapı/ekran, A2 defter/emanet/rapor) | **KARŞILANDI** | Tek migration'da uygulandı ama A1 ve A2 ayrı ayrı test edildi: A1 = `MadenTestleri` + `MadenUcTestleri` (deftere dokunmaz), A2 = `MadenDefteriTestleri`. Tam süit 2020/2020 yeşil |
| Geri doldurma ölçülmeden `Up` çalıştırılmaz (açık soru 6) | **KARŞILANDI** | § 4 — 6 ölçü `Up`'tan ÖNCE alındı, migration yorumuna ve rapora yazıldı |
| Varsayım + sayılar migration yorumunda ve raporda | **KARŞILANDI** | `20260917112823_HareketMaden.cs` XML yorumu + § 4 |
| Migration `DenetimKaydi` düşürmez | **KARŞILANDI** | Ham `migrationBuilder.Sql`, SaveChanges dışı |
| `mevcutHasGramBakiye` daralması: Web+API tek sevk, açık bayrak, testler güncel | **KARŞILANDI** | § 3.7; `EmanetYanitiMadenBakiyeleriniTasir`; Emanet ekran/HTTP testleri yeşil |
| `MadenKapisi.HareketDogrula` yalnız `HareketFabrikasi`, denetim.sh sayacı | **KARŞILANDI** | denetim.sh sayacı = 1, yalnız `Data/HareketFabrikasi.cs` |
| `GET /urunler/madenler` `[Authorize]`, anonim yok | **KARŞILANDI** | Canlı adım 1 = 401 |
| Kotasyon yoksa fail-closed, sessiz sıfır fiyat yok | **KARŞILANDI** | `KOTASYON_YOK` + maden adı; canlı 9-10 |
| K20-2 ADR'ye yazılmadan kod yazılmaz | **KISMEN** | ADR **dosyası yazılmadı** (§ 11); değişmez `Hareket.Maden` XML belgesinde, `MadenKapisi` başlığında ve migration yorumunda yazılı. ADR numarası çakışması tek elden çözülmeli → orkestratöre |
| Regresyon kapısı: FiyatMotoru/Maliyet/SatisFisi/KarmaOdeme 0 başarısız | **KARŞILANDI** | Tam süit 2020/2020 |
| Geri doldurma testi ölçülebilir (SQL kanıtı ya da dizge doğrulaması) | **KARŞILANDI** | Her ikisi: `GeriDoldurmaMigrationiUrunAyarindanTuretir` + canlı SQL |
| Desen mutasyonu davranışla ölçülür (`IsMatch("K23")=false`) | **KARŞILANDI** | `DesenTumAyarlariKapsar`; M20-6 |
| Paket 25/27 ile aynı dalgada değil; `StokDegeri` TB-19 borcu KOPYALANMAZ | **KARŞILANDI** | Yeni sınırsız `ToListAsync` yok; gruplamalar mevcut listeler/`GroupBy` üzerinde |
| Gümüş kuralı (Q3) korunur | **KARŞILANDI** | Sabit fiyat ve geçersiz kılma yolları değişmedi; `Gumus_urun_SABIT_fiyatli_gecersiz_kilmayla_FIYATLANIR` yeşil |
| DoD: yeni varlık PATCH + pasife alma + yetki + denetim kaydı + ekran yolu | **KARŞILANDI** | § 12 DoD sözleşmesi |
| Her düğme istek atar ya da disabled + sebep | **KARŞILANDI** | Rodyum kutusu disabled + sebep metni; maden seçici disabled + `#u-maden-kilit`; `Rodyum_kutusu_kaplanamayan_madende_KILITLI…` |
| Tek düzenleme kalıbı (yeni `duzenlenenId` yok) | **KARŞILANDI** | denetim.sh `duzenlenenId` = 1 (tavan 1) |
| Sessiz form yasağı | **KARŞILANDI** | Çelikte fiyatsız Kaydet → alan altı mesaj; `Celik_secilince…_SESSIZ_KALMAZ` |
| Tek dokunuş (Dokunulan sayfalar listesi) | **KARŞILANDI** | § 2.9 |
| Ürün ücretsiz pilot — fiyat/teklif metni üretilmedi | **KARŞILANDI** | Raporda ticari metin yok |
| UX-S21/22/24/25/26/30/31 | **KARŞILANDI** | Maden ayardan türetilir (S21); tek maden varsa seçici gizli (S22); maden↔ayar↔tip bağı ekranda (S24); rodyum/maden düzenlenebilir (S25); düzenleme = oluşturma alan kümesi (S26); kilitli kutunun sebebi yazılı (S30); ham kod yerine sözlük (S31) |
| UX-S23 / S27 / S28 / S29 / S32 / S33 | **DEĞİŞMEDİ** | Bu paket bu yüzeylere dokunmadı; mevcut davranış korundu |
| COMMIT YOK | **KARŞILANDI** | `git log` tek commit (`bbfdc95`), çalışma ağacı kirli |

---

## 11. Açık kalanlar

1. **Faz B (kotasyon)** — `kur_snapshots`'a `gumus/platin/paladyum_gram_fiyati`, `POST /kur/manuel` alanları, `SnapshotZenginlestir`, `MadenKotasyonTestleri`. Bugün altın dışı ürün **yalnız sabit fiyatla** satılıyor. Platin/paladyum kotasyonu üç kur kaynağının **hiçbirinde yok** (yalnız manuel giriş mümkün olacak).
2. **ADR dosyası yazılmadı** — "has = altın; maden işareti zorunlu" değişmezi kod içi belgelerde yazılı ama `dokumanlar/02-mimari/adr/` altına ADR açılmadı (numara çakışması tek elden çözülmeli — orkestratör kararı).
3. **Gümüş kotasyonu birimi** — kaynaklardan gelen gümüş paritesinin "has gümüş (999)" mi yoksa 925 gram fiyatı mı olduğu doğrulanmadı; formül (kotasyon × has gram mı, × net gram mı) buna bağlı. Faz B öncesi üç kaynağın canlı yanıtı incelenmeli.
4. **Altın dışı emanetin cari ekstresinde görünümü** — bu pakette **hesaplanan görünüm** verildi (hareketlerden); kalıcı kolon paket 27.
5. **Ons/USD üzerinden platin türetme** (XPTUSD × USD/TRY) — kaynak ve marj doğrulanmadı, açılmadı.
6. **Vitrin panosu** platin/paladyum satırı — PANO-KARAR mühürlü, dokunulmadı.
7. **Silinecek artık dosya:** `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` — `sed -i` ürünü; içeriği `printf '' >` ile **boşaltıldı** (0 bayt), derlemeye girmiyor. TurkReport hook `rm` engellediği için **silme kurucuya bırakıldı**.

---

## 12. DoD sözleşmesi — hangi kayıt hangi uçla düzenlenebilir

| Kayıt / alan | Düzenleme yolu | Pasife alma / silme | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|
| `urunler.rodyum_kaplama` | `PATCH /urunler/{id}` (gönderilmeyen alan **korunur**, `false` kaldırır) | Ürün `DELETE` ile pasife alınır (mevcut) | `[ModulYetkisi("urun")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | Otomatik yazma izi (`DenetimYazmaDinleyicisi`) | Ürünler → Düzenle → "Rodyum kaplama" kutusu (`#d-rodyum`) |
| `urunler.ayar_id` (→ **maden**) | `PATCH /urunler/{id}` — maden ayardan türer, ayrı kolon yok | — | aynı | **Ek gerekçeli iz:** `DenetimServisi.YazmaKaydet` çağrılır → "Ürün madeni Altın → Platin (ayar PT950). Geçmiş hareket satırları yazıldıkları madenle etiketli KALIR" | Ürünler → Düzenle → Maden + Ayar seçicileri |
| `hareketler.maden` | **SALT OKUNUR — DEĞİŞMEZ.** Defter append-only'dir; satır yazıldığı madenle kalır. Düzeltme yolu: **storno (48-A motoru)** + doğru madenle yeniden giriş. Hard DELETE **YASAK** (finansal varlık) | — | — | Storno kendi izini yazar | Ekranda düzenleme düğmesi YOK; gerekçe defterin append-only sözleşmesidir (`Hareket.Maden` XML belgesinde yazılı) |
| `Ayarlar` / maden tablosu | **Kod sabiti** (Kuyumcu.Core). Kullanıcı kaydı değildir; yeni ayar tek dosyada eklenir, desen/ekran/filtre kendiliğinden genişler | — | — | — | `GET /urunler/madenler` ile **okunur**; ekranda değiştirilemez (kayıt türü değil) |
| Etiket rodyum ibaresi | **Yapılandırma** (`appsettings.json` → `Etiket:RodyumIbaresiGoster`), varsayılan **KAPALI** | — | Sunucu yapılandırması | — | Ekran politikayı sunucudan okur; mevzuat kararını ekran vermez |

**Yeni finansal varlık AÇILMADI**; bu paket mevcut `hareketler` satırına bir **işaret kolonu** ekledi. Hard DELETE eklenmedi.

---

## 13. Kurucuya / mali müşavire notlar

1. **Çelik ayar kodu (kurucu/pilot dükkân):** perakende kuyumcuda çelik saat/bileklik için **316L / 904L gibi kalite kodu ayrımı** yapılıyor mu? Şu an tek `CELIK` kodu var. Yapılıyorsa `Ayarlar.Celik` sözlüğüne eklenir — başka hiçbir yer değişmez.
2. **Rodyum kaplama ibaresi (mali müşavir):** ibarenin **etikette/faturada gösterilmesi ZORUNLU mu?** Doğrulanamadı → ADR-0005 gereği koda gömülmedi, **varsayılan KAPALI**. Zorunluysa `appsettings.json` → `"Etiket": { "RodyumIbaresiGoster": true }` yeterli. (⚠ Kuyum Ticareti Yönetmeliği md.11/(e) **kıymetli taş kökeni** ibaresidir — rodyumla karıştırılmamalıdır; o hüküm doğrulanmış ve fail-closed uygulanıyor.)
3. **Çeliğin KDV/belge rejimi (mali müşavir):** çeliğin kıymetli maden sayılmadığı teknik bir gerçektir; **farklı bir KDV/belge rejimine** tabi olup olmadığı doğrulanmadı. Bu pakette yalnız "has hesaplanmaz" teknik kuralı uygulandı; KDV yolu **değiştirilmedi**. Paket 1/38 ile birlikte araştırılmalı.
4. **Geçmiş verideki etiket riski (kurucu):** 128 hareket satırının 21'inin ürünü yok (hurda + emanet) ve `'ALTIN'` etiketlendi; ayarı bir satıştan sonra güncellenmiş **en fazla 7** ürün var (gerçek sayı veri modelinde saklanmıyor). Bu satırlar geçmiş raporlarda yanlış madende görünebilir — **bilinen ve ölçülmüş** bir varsayımdır, gizlenmedi.
5. **Milyem standartları (mali müşavir):** platin 950/900, gümüş 925, altın 585/750/916 değerleri **ticari teamül + mevcut kod** kaynaklıdır; hesaplama sabiti olarak kullanılıyor, **yasal hüküm olarak sunulmuyor**.
6. **Tevsik (30.000 TL) ve MASAK (185.000 TL) kapıları DEĞİŞMEDİ** — ikisi de TL bacağı üzerinden çalışır, maden değişikliği bunları etkilemez. Mevcut kapı çağrıları aynen korundu, yeni akış açılmadı.
