# ADR-0010 — 98 adet elle `TenantId` şartı kaldırılmaz

**Durum:** Kabul edildi · **Tarih:** 2026-09-12

## Bağlam

ADR-0001 ile global query filter kurulduktan sonra, sorgulardaki elle yazılmış `Where(x => x.TenantId == ...)` şartları teorik olarak gereksiz hâle geldi. Bunların temizlenmesi planlanmıştı ("Adım 1b").

İlk sayım 68 idi; yeniden ölçümde **98** çıktı. Fark sayım hatası değil: o günden beri üç yeni modül yazıldı ve her biri kendi şartlarını getirdi.

## Karar

**Temizlik yapılmayacaktır.**

## Gerekçe

Baş Mimar'ın ilk gerekçesi ("iki bağımsız savunma katmanı oluşturur") **denetlendi ve yanlış bulundu**: elle yazılan şart ile query filter **aynı JWT claim'ini** okur. Aynı kaynaktan beslenen iki kontrol bağımsız katman değildir.

Ancak temizlik yine de yapılmamalıdır — **sebebi başkadır**:

> **Sistem kapsamında query filter hiç kurulmaz** (ADR-0002). O kapsamda elle yazılmış `TenantId` şartı **tek korumadır.** Temizlik, o korumayı kaldırmak olurdu.

Ek olarak: 98 sorguyu ölçülmüş bir kazanç olmadan elden geçirmek regresyon riskidir.

## Sonuçlar

**Olumlu** — Sistem kapsamındaki koruma korunur; regresyon riski alınmaz.

**Olumsuz** — Kod tabanında görünüşte gereksiz 98 şart kalır. Bu ADR, gelecekte "bunlar niye duruyor?" sorusunun cevabıdır.

## Ders

Bir gerekçenin yanlış olması, kararın da yanlış olduğu anlamına gelmez. Karar doğruydu, gerekçe yanlıştı. İkisi ayrı ayrı denetlenmelidir.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
