# ADR-0016 — Reyon yerleşimi alt-bölümlemedir, defter kaydı değildir

**Durum:** Kabul edildi · **Tarih:** 2026-09-17 · **Dayanak:** Paket 25 Faz A (reyon/vitrin bazlı stok bölümlemesi), Bertan + Mehmet koşulları

## Bağlam

Paket 25, şube stoğunu şube içindeki fiziksel yerleşim birimlerine (vitrin · kasa · depo · çanta) bölen `stok_yerlesimleri` tablosunu getirdi. Ortaya iki soru çıktı:

1. `stoklar` tablosu ile yeni tablo arasında **hangisi gerçektir**?
2. Reyondan reyona taşıma **deftere (hareketler) yazılmalı mıdır**?

İkinci soru önemsiz değildir: bir denetçi "mal yer değiştirmiş ama defterde satır yok" diyebilir.

## Karar

1. **Tek gerçek kaynak `stoklar` tablosudur.** `stok_yerlesimleri` onun ŞUBE İÇİ KIRILIMIDIR. Değişmez I1 geçerlidir: `Σ stok_yerlesimleri.adet ≤ stoklar.adet`. Aradaki fark **saklanmaz, hesaplanır** ve adı **ATANMAMIŞ**'tır.
2. **Aynı şube içindeki reyon taşıması deftere YAZILMAZ.** `HareketFabrikasi` çağrılmaz, `hareketler` tablosuna satır eklenmez. İz `denetim_kayitlari`na yazılır (ADR-0004, append-only): kim, ne zaman, hangi reyondan hangi reyona, kaç adet.
3. Yerleşim satırı **PATCH ile düzeltilmez**; düzeltme **ters hareketle** yapılır (taşıma ya da atanmamışa geri alma). Düzeltme politikası haritasında damgası `STORNO`dur.

## Gerekçe

**Defter bir DEĞER ve MÜLKİYET kaydıdır, konum kaydı değil.** Reyon içi taşımada:

- toplam adet değişmez,
- has gram değişmez,
- para el değiştirmez, borç doğmaz,
- mülkiyet değişmez, karşı taraf yoktur.

Böyle bir olayı deftere yazmak, `hareketler` tablosunu "her şeyin günlüğü" hâline getirir ve defterin kendi anlamını (VUK md.219 hattındaki kayıt disiplini) sulandırır. Aynı gerekçe `SAYIM_DUZELTME` kararında tersine işlemişti: sayım **adedi değiştirdiği** için deftere yazılır (TB-18), taşıma **değiştirmediği** için yazılmaz.

**İzsiz de bırakılmıyor.** "Defter kaydı değildir" demek "iz yoktur" demek değildir. Denetim izi append-only'dir, silinemez ve sorumluluk sorusunu ("bu 3 gram vitrinden kasaya kim taşıdı") cevaplar. Resmî belge iddiası ise **yapılmaz**: reyon taşıma kaydının VUK md.253 saklama yükümlülüğüne tabi bir "belge" sayılıp sayılmadığı bu oturumda **doğrulanmadı**.

**İki tablo, tek gerçek.** Stok anahtarını (kiracı + şube + ürün) dörtlüye çıkarmak `StokPartisi` (FIFO/LIFO maliyet defteri), transfer ve sayımın tamamını yeniden yazmayı gerektirirdi. Alt bölümleme, maliyet defterine hiç dokunmadan aynı iş sorusunu ("bu üründen kaçı vitrinde") cevaplar.

**ATANMAMIŞ neden saklanmıyor.** Saklansaydı iki tablo da "toplam" iddia ederdi; ilk sapmada hangisinin doğru olduğunun cevabı olmazdı. Hesaplanan bir fark sapma üretemez — en kötü ihtimalle "atanmamış" büyür ve bu **doğru** bilgidir: fiziksel yeri bildirilmemiş mal.

## Sonuçlar

- Reyon içi taşıma `hareketler` sayısını değiştirmez; bu bir testle tutuluyor (`ReyonYonetimTestleri.Tasima_hareketler_defterine_satir_yazmaz`) ve mutasyonla kanıtlandı (M25-6).
- Kâr-zarar, has defteri, kasa ve maliyet partileri bu paketten **etkilenmez**.
- Geçiş verisinde tüm stoklar ATANMAMIŞ başlar. Migration'da `urunler.reyon_id`den otomatik yerleştirme **yapılmaz**: ürün kartı şubeden bağımsızdır, hangi şubedeki hangi adedin oraya gideceği bilinmez — uydurma olurdu (Anayasa md.1).
- Reyon bazlı sayım (Faz B) bu karara dayanacaktır: sayım kapsamı yerleşimden okunacak, fark yine TB-18 yolundan deftere yazılacaktır.
