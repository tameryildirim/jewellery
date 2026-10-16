# ADR-0003 — Kimlik alanları AES-256-GCM ile şifrelenir, aranabilirlik HMAC ile sağlanır

**Durum:** Kabul edildi · **Tarih:** 2026-09-11

## Bağlam

MASAK Tedbirler Yönetmeliği md.46 gereği kimlik tespiti kayıtları **8 yıl** saklanır. Bu kayıtlar TCKN, pasaport numarası, doğum tarihi ve adres içerir — KVKK kapsamında kişisel veridir.

Veritabanı yedeği ele geçtiğinde bu verinin düz metin olarak okunabilir olması kabul edilemez.

Ancak şifreli bir sütun üzerinde `WHERE tckn = ?` sorgusu çalışmaz: aynı değer her şifrelemede farklı şifreli metin üretir (GCM rastgele nonce kullanır).

## Karar

- Hassas alanlar **AES-256-GCM** ile, EF Core value converter üzerinden şifrelenir.
- Aranabilirlik için **ayrı bir HMAC-SHA256 hash sütunu** tutulur (`satici_tckn_hash`, `belge_no_hash`).
- Anahtar **açılışta, ilk istekten önce** yüklenir; yoksa uygulama **hiç ayağa kalkmaz**.
- Her kayıtta `sifre_surumu` alanı bulunur (anahtar rotasyonu için).

## Gerekçe

GCM, şifrelemeye ek olarak **bütünlük** de sağlar (kurcalanan şifreli metin çözülmez). HMAC hash'i deterministiktir, bu yüzden aranabilir; ama tek yönlüdür, yani hash'ten TCKN geri üretilemez.

## Sonuçlar

**Olumlu**
- Diskte TCKN düz metin sayısı: **0** (ham SQL ile doğrulandı).
- Aynı TCKN hash ile bulunabilir, şifreli sütunları farklıdır.

**Olumsuz**
- Şifreli sütunda `LIKE`, sıralama ve aralık sorgusu yapılamaz.
- Anahtar kaybı = veri kaybı. Anahtar yönetimi operasyonel bir sorumluluktur.

## Canlı testte yakalanan hata

Anahtar ilk sürümde `AddSingleton(_ => Yukle(...))` ile **tembel** kaydedilmişti. Bu, anahtarın ilk `DbContext` çözülene kadar yüklenmediği anlamına geliyordu — ve **anahtarsız uygulama `/saglik` ucuna 200 döndürüyordu.**

Düzeltme: fabrika değil, `builder.Build()` öncesinde **hemen yüklenen örnek** kaydedilir. Anahtar yoksa `Build()`'e bile gelinmez.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
