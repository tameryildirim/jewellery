# ADR-0004 — Denetim izi append-only'dir ve bu kod tarafından zorlanır

**Durum:** Kabul edildi · **Tarih:** 2026-09-13

## Bağlam

KVKK md.12, kişisel veriye erişimin kayıt altına alınmasını gerektirir. `denetim_kayitlari` tablosu bu amaçla 2026-09-11'de kuruldu.

Tablonun başlık yorumunda şu yazıyordu:

> *"APPEND-ONLY: bu tablo güncellenmez, silinmez. Denetim izi değiştirilebiliyorsa denetim izi değildir."*

2026-09-13 ölçümünde ortaya çıktı ki **bunu uygulayan hiçbir kod yoktu.** `DenetimKaydi` için `Modified`/`Deleted` durumunu engelleyen tek bir kısıt bulunmuyordu. Yorum iddia ediyor, kod tutmuyordu.

## Karar

`DenetimYazmaDinleyicisi` içinde `DenetimKaydi` üzerinde `Modified` veya `Deleted` durumu tespit edilirse `DenetimIziDegistirilemez` istisnası atılır ve **tüm `SaveChanges` düşer**.

Okuma ucu (`GET /api/v1/denetim`) eklendi; **silme veya güncelleme ucu yoktur ve eklenmeyecektir.**

## Gerekçe

Bir yorum kural değildir. Denetim izinin değeri, değiştirilemezliğinin **kanıtlanabilir** olmasından gelir. Bir denetçiye "bu tabloya dokunulamaz" demek, ancak dokunma denemesinin reddedildiğini gösterebiliyorsak anlamlıdır.

## Sonuçlar

**Olumlu**
- Mutasyon kanıtı: koruma kaldırıldığında **5 testin 4'ü kırmızı**; yeşil kalan tek test "kayıt EKLENEBİLİR" — o zaten kırmızıya dönmemeli.
- Meşru bir yazmanın arasına saklanan denetim değişikliği de reddedilir (tüm `SaveChanges` düşer).
- Canlı: `DELETE /api/v1/denetim/{id}` → **405**.

**Olumsuz / bilinen sınır**
- `ExecuteUpdate`/`ExecuteDelete`, `SaveChanges`'ı atlar; bu koruma onları **görmez**. `denetim.sh` bu çağrıları beyaz listeyle sayar (izin verilen: **5** — P-30b, 2026-09-23 ölçümü); listeye `denetim_kayitlari`'na dokunan bir çağrı eklenirse orada yakalanır.
- Veritabanına doğrudan SQL erişimi olan biri yine de değiştirebilir. Bu, uygulama katmanı korumasının doğal sınırıdır ve veritabanı yetkilendirmesiyle ele alınmalıdır.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
