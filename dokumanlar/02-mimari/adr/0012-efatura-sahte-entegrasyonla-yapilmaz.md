# ADR-0012 — e-Fatura, gerçek entegratör test ortamı olmadan "yapıldı" sayılmaz

**Durum:** Kabul edildi · **Tarih:** 2026-09-11 · **Engel:** Kurucu kalemi K1

## Bağlam

e-Fatura/e-Arşiv, GİB'e özel entegratör üzerinden gönderilir. Entegratörle **sözleşme** yapılmadan test ortamına erişim alınamaz.

Yazılım tarafında `IEFaturaSaglayici` soyutlaması yazılıp sahte (mock) bir uygulama ile "e-Fatura modülü tamamlandı" denebilirdi.

## Karar

**Denmeyecektir.** Adım, sözleşme gelene kadar **başlatılmaz**.

Kabul ölçütü değiştirilmedi: *entegratörün test ortamına gerçek fatura gönderilir, dönen belge numarası ve durum raporlanır.* Kuyum KDV'li bir faturada has altın bedelinin matrah dışı kaldığı **UBL çıktısında gösterilir**.

## Gerekçe

Sahte bir sağlayıcıyla geçen test, entegrasyonun çalıştığını **kanıtlamaz**. UBL-TR şema uyumu, GİB senaryoları (TEMELFATURA / TİCARİFATURA / e-Arşiv), imza ve zarflama — hiçbiri mock ile doğrulanamaz.

"Modül hazır, sadece bağlantı kalacak" demek, işin **en riskli kısmını** ölçülmemiş bırakıp tamamlandı ilan etmektir. Anayasa md.1: "Oldu/bitti ancak kanıtla."

## Sonuçlar

**Olumlu** — Yanlış bir tamamlanma iddiası kayda geçmez.

**Olumsuz / ticari maliyet**
- e-Fatura mükellefi olan her müşteri segmenti bugün **kapsam dışıdır.** Satış tarafının değerlendirmesiyle bu, ürünün **en büyük ticari riski**dir.
- Fatura/e-fatura haddi için dört farklı ve çelişen rakam bulundu (12.000 / 36.000 / 3.000.000 / 5.000.000 TL). Hangisinin geçerli olduğu **mali müşavir teyidine** bağlıdır (K2). Eşik, yapılandırmadan okunacak biçimde tasarlanmıştır (ADR-0005).

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
