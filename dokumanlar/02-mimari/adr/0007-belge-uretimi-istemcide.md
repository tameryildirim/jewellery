# ADR-0007 — Resmî belgeler istemcide render edilir; sunucuya PDF kütüphanesi eklenmez

**Durum:** Kabul edildi · **Tarih:** 2026-09-13

## Bağlam

VUK md.234 gereği hurda alımında **gider pusulası** düzenlenir ve yazdırılır. İleride e-Fatura, MASAK beyan formu ve sentetik taş sertifikası da yazdırılabilir belge olacaktır.

Ölçüm: kod tabanında hiçbir PDF üretim kütüphanesi yok (QuestPDF, iText, PdfSharp, DinkToPdf = 0).

## Karar

Sunucu, belgenin **verisini** döndürür (`GET /api/v1/hurda/{id}/belge`). Görsel render ve yazdırma **istemcide** yapılır: ayrı bir Blazor yolu, sade yerleşim, `@media print` CSS ve `window.print()`.

Sunucu tarafına PDF kütüphanesi **eklenmez**.

## Gerekçe

- Yeni bir sunucu bağımlılığı = yeni saldırı yüzeyi + lisans yükümlülüğü (bazı PDF kütüphaneleri ticari kullanımda ücretlidir).
- Tarayıcının yazdırma motoru zaten PDF üretebilir ("PDF olarak kaydet").
- Belge verisi ile sunumunun ayrılması, aynı veriyi farklı şablonlarla basmayı kolaylaştırır.

## Sonuçlar

**Olumlu** — Sıfır ek bağımlılık; belge verisi test edilebilir (367 testin içinde).

**Olumsuz**
- Sunucu tarafında toplu belge üretimi (ör. gece işiyle 500 pusula arşivleme) bu tasarımla **yapılamaz**. Böyle bir ihtiyaç doğarsa bu karar yeniden ele alınmalıdır.
- Çıktının birebir görünümü tarayıcıya bağlıdır.

## Not

Dördüncü yazdırılabilir belge yazılmadan önce ortak bir `BelgeYerlesimi` bileşeni ve print CSS standardı kurulmalıdır (Bertan, 2026-09-13).

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
