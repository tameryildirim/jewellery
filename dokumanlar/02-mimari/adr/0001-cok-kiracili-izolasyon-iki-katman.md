# ADR-0001 — Çok kiracılı izolasyon iki katmanla sağlanır

**Durum:** Kabul edildi · **Tarih:** 2026-09-11 · **Karar:** Tamer Bey (Mehmet vetosu kapsamında)

## Bağlam

Devralınan kod tabanında kiracı izolasyonu **her sorguda elle `TenantId` yazılmasına** bağlıydı. Ölçüm: `HasQueryFilter` sayısı **0**.

Bu yaklaşımın yetersizliği varsayım değil, **kanıtlanmış bir olaydı**: Faz 0 denetiminde başka bir kiracının kasasına 99.999 TL yazıldı ve sistem **HTTP 200** döndürdü. Bir geliştirici tek bir `Where` şartını unuttuğunda veri sızıyordu.

MASAK kimlik verisi (TCKN, adres, doğum tarihi) bu zeminin üstüne binecekti.

## Karar

İzolasyon **iki bağımsız katmanla** sağlanacak:

1. **Okuma tarafı** — EF Core global query filter (`HasQueryFilter`), `IKiraciBaglami` soyutlamasıyla istek kiracısına bağlanır.
2. **Yazma tarafı** — `KiraciYazmaDenetcisi` (`SaveChangesInterceptor`). Query filter **yalnızca okumayı** korur; `Add()` ile başka kiracıya satır yazmayı hiç engellemez.

Ek olarak **fail-closed** kurulur: bağlamsız `DbContext` yapıcıda patlar; `IKiraciVarlik` uygulamayan bir varlık eklendiğinde uygulama **açılmaz**.

Kiracıya duyarlı model için ayrı model önbelleği (`IModelCacheKeyFactory`) kullanılır — tek modelde OR yüklemi indeks kullanımını bozardı.

## Gerekçe

Tek katman yetmez, çünkü okuma ve yazma **farklı yollardan** geçer. Query filter'ın yazmayı koruduğunu varsaymak, bu projede zaten gerçekleşmiş bir veri sızıntısının tekrarına davetiye olurdu.

## Sonuçlar

**Olumlu**
- Filtre unutulsa bile başka kiracının verisi görünmez.
- Mutasyonla kanıtlandı: filtre bilerek kapatıldığında **13 testin 11'i kırmızıya döndü**, geri konunca tümü yeşile döndü.
- Canlı doğrulama: veritabanında 19 cari varken yeni kiracı **0** görüyor.

**Olumsuz / bedeli**
- Kiracıya duyarlı model önbelleği bellek maliyeti getirir (kiracı başına model).
- Sistem kapsamında filtre **kurulmaz** — orada elle şart tek korumadır (bkz. ADR-0010).
- `Find`/`FindAsync` yasaklandı: birincil anahtarla erişim query filter'ı atlar. `denetim.sh` bunu sayar (izin verilen: 0).

## Doğrulama

```
./denetim.sh   → HasQueryFilter 2 · IgnoreQueryFilters 0 · Find/FindAsync 0 · IKiraciVarlik 20
dotnet test    → KiraciFiltreTestleri + KiraciIzolasyonTestleri
```

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
