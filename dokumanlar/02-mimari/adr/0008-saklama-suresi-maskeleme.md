# ADR-0008 — Saklama süresi dolunca kayıt silinmez, kimlik alanları maskelenir

**Durum:** Kabul edildi · **Tarih:** 2026-09-12

## Bağlam

İki yükümlülük çatışıyor:

- **MASAK md.46/1** — kimlik tespitine ilişkin belge ve kayıtlar son işlem tarihinden itibaren **sekiz yıl** muhafaza edilir.
- **KVKK** — kişisel veri, işlendiği amaç için gerekli olan süreden **uzun tutulamaz** (amaçla sınırlılık ve ölçülülük).

## Karar

Süre dolduğunda kayıt **silinmez**. Yalnızca **kimlik alanları maskelenir** (`[SAKLAMA SÜRESİ DOLDU]`); tutar, tarih ve beyan kademesi **korunur**.

İş **elle tetiklenir.** Otomatik zamanlayıcı bilinçli olarak **eklenmemiştir**. Önce `Onizleme()` kaç kaydın etkileneceğini gösterir, sonra `Uygula()` çalıştırılır.

## Gerekçe

Silme, denetim izini bozar ve satır sayısını değiştirir — append-only ilkesine aykırıdır. Maskeleme, saklama yükümlülüğünün **sona ermesinin gereğidir**, verinin tahrifi değildir.

Zamanlayıcı eklenmemesi kasıtlıdır: **kişisel veriyi geri döndürülemez biçimde değiştiren bir iş, işletmenin haberi olmadan kendi kendine çalışmamalıdır.**

## Sonuçlar

**Olumlu** — Denetim izi bozulmaz; kişisel veri gereğinden uzun tutulmaz; işlem denetim izine yazılır (kim, ne zaman, kaç kayıt).

**Olumsuz / açık kalem**
- Kuyumcu işi **hiç tetiklemezse**, süresi dolmuş kişisel veri sistemde kalır. Bu bilinçli bir tercihtir ama **kapanmış bir madde değildir**; otomatik tetikleme kararı ekip oylamasına bırakılmıştır.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*

## Güncelleme — PAKET YASAL-3 (2026-10-01): kendiliğinden, 30 gün önceden haber + gerekçeli bekletme

**Karar değişti:** "Elle tetiklenir" → **kendiliğinden** (`SAKLAMA_UYGULA` işi, `Modules/Isler/Isler/SaklamaUygulaIsi.cs`, günlük).

- **Birincil dayanak (şartname § 3.1):** KVKK Silme/Yok Etme/Anonimleştirme Yönetmeliği md.7 — işleme şartları kalkınca **resen** silme/anonimleştirme; md.11/2 — periyodik imha aralığı **en çok 6 ay** (iş günlük koştuğu için sınır her zaman sağlanır). Tedbirler Yönetmeliği md.46 — 8 yıl **son işlem tarihinden** itibaren.
- **Süre son işlemden:** aday kayıt, (a) aynı `belge_no_hash`'e sahip ve süresi dolmamış daha yeni bir tespit varsa, (b) kaydın carisiyle son 8 yıl içinde herhangi bir hareket varsa **atlanır**. `saklama_bitis` yeniden yazılmaz (append-only korunur).
- **Mehmet'in şartı ("haberi olmadan çalışmamalı")** 30 gün önceden SAHİP bildirimiyle karşılanır ("N kaydın kimlik bilgileri 30 gün sonra kendiliğinden gizlenecek").
- **Gerekçeli bekletme:** `saklama_bekletmeleri` (SAHİP; tümü ya da tek kayıt; en çok 1 yıl, yenilenebilir; gerekçe ≥ 20 karakter; silinmez, "kaldır" damgası). Bekletme bitince iş kendiliğinden devam eder.
- **Tek motor:** elle uç (`POST /masak/saklama-uygula`) ve iş aynı `SaklamaSuresiIsi.AdaylariBul/Uygula` motorunu kullanır; otomatik koşum denetim izine "(otomatik)" etiketiyle, kullanıcısız yazılır.
- **Kuru koşum (varsayılan):** `Isler:SaklamaUygula:Kuru=true` — iş yalnız bildirir, maskelemez. Açma kararı kurucudadır (K-2 önerisi: 90 gün kuru koşumdan sonra `false`).
- `denetim.sh`: `Isler/` altında `Uygula(` sayacı 0 → **1** (yalnız `SaklamaUygulaIsi.cs`); ikizi `IsCatisiSafTestleri` M6-6.
