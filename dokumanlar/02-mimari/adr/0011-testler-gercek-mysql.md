# ADR-0011 — Testler gerçek MySQL'e karşı çalışır; InMemory veya SQLite kullanılmaz

**Durum:** Kabul edildi · **Tarih:** 2026-09-10

## Bağlam

EF Core testleri için üç seçenek vardır: InMemory sağlayıcı, SQLite (bellekte), gerçek veritabanı.

## Karar

Testler **gerçek MySQL 8.4**'e karşı çalışır (`kuyumcu_test` veritabanı). Şema **migration ile** kurulur, `EnsureCreated` ile değil.

## Gerekçe

- **EF InMemory transaction desteklemez.** Bu üründe para ve has altın hareketleri transaction içinde yürür; desteklenmeyen bir transaction sessizce yok sayılırsa testler sahte güven verir.
- **SQLite decimal hassasiyetini uygulamaz.** Has gram hesabı `(gram − taş) × milyem / 1000` ondalık hassasiyete bağlıdır; SQLite'ta doğru görünen bir test MySQL'de yanlış sonuç verebilir.
- **Migration ile kurulum**, test şemasının üretim şemasından sapmasını engeller.

## Sonuçlar

**Olumlu** — Testler üretimdeki boru hattının aynısını kullanır: aynı interceptor'lar, aynı model önbelleği, aynı şema.

**Olumsuz**
- Test koşumu çalışan bir MySQL kabı gerektirir.
- Testler paylaşımlı bir şema kullanır; koleksiyonlar arası izolasyon dikkat ister.

## Yol boyunca yakalanan hata

HTTP test altyapısı (2026-09-13) ikinci bir `TestVeritabani` örneği yarattı; onun yapıcısındaki `EnsureDeleted()` **diğer koleksiyonun koşan testlerinin altından** şemayı sildi → **132 test birden kırmızı**.

Düzeltme: şema kurulumu süreç başına **bir kez** (`SemayiBirKezKur`).

## Koruma

`TestVeritabani`, `kuyumcu` veya `kuyumcu_net` veritabanına bağlanmayı **reddeder**:

```csharp
if (vt is "kuyumcu" or "kuyumcu_net")
    throw new InvalidOperationException("Testler geliştirme veritabanında çalıştırılamaz.");
```

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
