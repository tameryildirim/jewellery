# ADR-0015 — Barkod Code 128'dir, EAN-13 değildir

**Durum:** Kabul edildi · **Tarih:** 2026-09-14 · **Dayanak:** PLAN-2 Adım 5

## Bağlam

Etiket basımı için her ürünün makine okunur bir kodu olmalı. İki aday vardı:

- **EAN-13** — perakendenin yaygın biçimi, her kasa okuyucusunda çalışır.
- **Code 128** — değişken uzunlukta, alfanümerik, kayıt otoritesi istemez.

## Karar

**Code 128-B** seçildi. Barkod verilmediğinde ürün oluşturulurken **otomatik üretilir**: `K` + ULID'in son 10 karakteri.

## Gerekçe

**EAN-13 uydurulamaz.** Geçerli bir EAN-13, GS1'den alınan bir **firma önekiyle** başlar. Elimizde böyle bir önek yok. Rastgele bir önekle numara üretmek, **başka bir firmanın numara alanına yazmak** demektir; o ürün bir gün başka bir sistemde okutulduğunda yanlış firmayı gösterir. "2" ile başlayan mağaza içi (restricted circulation) aralığı da yalnız kapalı devrede anlamlıdır — ürün dükkândan çıktığı anda hükmü kalmaz.

Bu, Anayasa md.1'in ("bilinmeyen tahmin edilmez") doğrudan uygulanmasıdır: elimizde olmayan bir tescili varmış gibi göstermeyiz.

**Code 128** kayıt otoritesi istemez, kuyumcunun iç stok etiketi için tasarlanmış gibidir ve bugün her el terminali okur. GS1 üyeliği alınırsa EAN-13 ayrı bir sınıf olarak eklenir; `Code128` yerinde kalır.

**Neden ULID'den türetiliyor:** ULID'in kendisi 26 karakterdir — 50×30 mm etikete sığmaz ve personel elle giremez. Son 10 karakter ULID'in rastgele bölümüdür. ULID Crockford baz32 kullandığı için `I`, `L`, `O`, `U` harfleri zaten yoktur; elle okurken 1/I ve 0/O karışması olmaz. Bu varsayım bir testle tutuluyor (`Karistirilabilir_harfler_uretilen_barkodda_BULUNMAZ`) — ULID kütüphanesi değişirse haber verir.

## Doğrulama — "çizilmiş görünüyor" yeterli değildir

Yanlış sağlama (checksum) üreten bir barkod ekranda kusursuz görünür ama okuyucu **hiçbir şey göstermez**. Bu yüzden üç katmanlı doğrulandı:

1. **Sembol tablosu bağımsız üretildi.** Code 128'in "bar-boşluk genişlikleri" (6 rakamlı) gösterimi modül dizisine çevrilip koddaki tabloyla karşılaştırıldı — **107/107 eşleşti**.
2. **Sağlama elle hesaplandı.** `PJJ123C` → 104 + 48 + 84 + 126 + 68 + 90 + 114 + 245 = 879; 879 mod 103 = **55**. Test bu değeri bekliyor.
3. **Canlı çizim geri çözüldü.** Tarayıcıdaki gerçek etiketin `<rect>` koordinatları okundu, bağımsız bir çözücüye verildi: START-B (104) → `K0N9AERGV3T` → sağlama 36 (hesaplanan 36) → STOP (106). **Ekrandaki barlar gerçekten o metni taşıyor.**

## Sonuçlar

**Olumlu**
- Etiket, Kuyum Ticareti Yönetmeliği md.11/(e) ibaresini (ADR-0013) ve barkodu **birlikte** taşır: iki yasal yük tek kâğıtta.
- Çizim istemcide (ADR-0007) — sunucuya PDF/barkod kütüphanesi eklenmedi. `Kuyumcu.Core`'daki algoritma hem WASM'da hem sunucuda çalışır.
- Etiket ölçüsü 50×30 mm; A4'e 4 sütun basar, kesim sırasında etiket **sayfa ortasından bölünmez**.

**Olumsuz**
- EAN-13 bekleyen bir zincir mağaza müşterisi bugün kapsam dışıdır. GS1 üyeliği kurucu kalemidir.
- Barkod alanı **kiracıya özeldir**: iki ayrı dükkân aynı tedarikçi barkodunu kullanabilir. Kiracı içinde tekilliği zorunlu kılınır, sistem genelinde değil.

## Bu adımda yakalanan iki gerçek hata

**1. SVG sayıları Türkçe virgülle yazılıyordu.** `{1.6:0.##}` tr-TR kültüründe `"1,6"` üretir; `viewBox="0 0 184,8 34"` ayrıştırılamaz ve **barkod tarayıcıda hiç çizilmez** — ne hata, ne uyarı, sadece boş bir etiket. Birim testleri invariant kültürde koştuğu için **yeşildi**; hata yalnız canlı Türkçe arayüzde göründü. Artık `CultureInfo.InvariantCulture` ile yazılıyor ve `BarkodKulturTestleri` tr-TR kültürünü **zorlayarak** koşuyor.

**2. `ToUpperInvariant("sentetik")` → `"SENTETIK"`** (noktasız I). Yönetmeliğin ibaresi **"SENTETİK"**tir; etikete yasal metin yanlış basılıyordu. Aynı hatanın diğer yönü ürün kaydında da vardı: `OrdinalIgnoreCase`, kuyumcunun yazdığı "SENTETİK"i "sentetik" ile eşleştirmiyor ve kaydı **reddediyordu**. İkisi de `Kuyumcu.Core.Domain.TurkceMetin` ile tek noktaya toplandı ve testlendi.

Her ikisi de Aşkın'ın çizgisini doğruluyor: *"yeşil artık bu projede kanıt sayılmıyor, yalnız ön koşul."*

## Yan bulgu — CSS sınıf adı çakışması

`.etiket` sınıfı kur bandında, belgelerde ve son-sorgu bileşeninde **"küçük başlık"** anlamında zaten kullanılıyordu. Ürün etiketi kuralları (50×30 mm, kesikli çerçeve) o öğelere de uygulanacaktı. Ürün etiketi sınıfları `urun-etiket-*` önekiyle ayrıldı.
