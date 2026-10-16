# ADR-0002 — `IgnoreQueryFilters` yerine tek ve denetlenebilir kaçış yolu

**Durum:** Kabul edildi · **Tarih:** 2026-09-11

## Bağlam

Bazı işlemler kiracı filtresinin dışında çalışmak zorundadır: kullanıcı girişi (hangi kiracı olduğu henüz bilinmez), arka plan kur zamanlayıcısı, yönetim uçları.

EF Core'un doğal çözümü sorgu başına `IgnoreQueryFilters()`'tır. Ancak bu çağrı kod tabanına yayıldığında **her biri ayrı bir denetim yüzeyi** olur; bir tanesinin yanlış yerde kullanılması sessizce tüm kiracıların verisini açar.

## Karar

`IgnoreQueryFilters()` kullanımı **yasaklandı** (izin verilen sayı: 0, `denetim.sh` ile zorlanır).

Yerine tek bir kaçış yolu kondu: **`SistemKapsami`** — açıkça kurulan, adı konmuş, grep'lenebilir bir kapsam. Filtre yalnızca bu kapsamda kurulmaz.

## Gerekçe

Denetlenebilirlik. `grep -rn "SistemKapsami"` komutu, sistem genelinde filtresiz çalışan **tüm** yerleri tek seferde listeler. Dağınık `IgnoreQueryFilters()` çağrılarıyla bu mümkün değildir.

## Sonuçlar

**Olumlu** — Tek denetim noktası; CI betiği ihlali dosya ve satır numarasıyla yakalar.

**Olumsuz** — Kapsam yönetimi elle yapılır; yanlış yerde açılan bir kapsam hâlâ risktir.

## Canlı testte yakalanan hata

İlk uygulamada `KiraciBaglami`, `HttpContext`'e öncelik veriyordu. `SistemKapsami` bir HTTP isteğinin **içinde** de yeni kapsam açtığı için (ör. `AuthServisi.Giris`), o çocuk kapsamda `IHttpContextAccessor` hâlâ isteği görüyor ve sistem bağlamını eziyordu. Sonuç: **giriş ucu HTTP 500** veriyordu — ve bu, 137 testin tamamı yeşilken oluyordu.

Düzeltme: **açıkça kurulan kapsam önceliklidir.**

```csharp
public string? KiraciId =>
    Kapsam is { } kapsam
        ? kapsam.KiraciId
        : erisim.HttpContext?.User.FindFirst("tenantId")?.Value is { Length: > 0 } t ? t : null;
```

## Ek not — 2026-09-30 · PAKET ŞUBE-KAPSAM-A: izinli kullanım 10 (yasal kümülatif okuyucu)

**Bağlam.** P-04 global şube süzgeci şubeye kilitli rolde (TEZGÂHTAR) `db.Hareketler`'i oturum şubesine daraltır. Tevsik (`TevsikKapisi.GunlukNakitToplam`) ve MASAK (`MasakKapisi.GunlukIslemToplam`) kümülatifleri istek bağlamındaki `DbContext` ile okuduğu için, A şubesindeki tezgâhtarın kapısı aynı müşterinin aynı gün B şubesindeki işlemlerini görmüyordu. HTTP testiyle ölçüldü (paket öncesi ağaç): B'de 20.000 TL nakit + A'da 15.000 TL nakit → **200** (tevsik delindi); B'de 100.000 + A'da 90.000 kimliksiz → **200** (MASAK delindi).

**Mevzuat.** Tevsik "aynı gün aynı kişi" mükellef geneli (459 s. VUK GT §4.1.1); MASAK "bağlantılı işlemler toplamı" yükümlü geneli (Tedbirler Yön. md.5/1-b); şube yükümlünün birimidir (MASAK GT 34 md.3/1-c). Şube ayrımını öngören birincil hüküm bulunmadı — tutucu yorum (mali müşavir teyidi ayrıca istenir).

**Karar.** `SistemKapsami.YasalKumulatifOku(db, tenantId, oku)`:
- şube süzgeci KAPALI bağlamda sorgu verilen `db` ile aynen koşar (değişiklik yok);
- şube süzgeci AÇIK bağlamda `SistemKapsami.Kiraci(tenantId)` kapsamı açılır (kiracı süzgeci VAR, şube süzgeci YOK) ve sorgu orada **salt okuma** koşar.

**Güvenlik koşulları (fail-closed).** (1) yalnız oturumun KENDİ kiracısı (`AktifKiraciId == tenantId`, değilse istisna); (2) `SistemKapsami` çözülemezse istisna; (3) açılan kapsamda şube süzgeci ya da sistem modu açıksa istisna; (4) izleme kapalı (`NoTracking`) ve okuyucu değişiklik izlerse istisna. Kümülatif hiçbir koşulda sessizce şube toplamına düşmez. `IgnoreQueryFilters` sayısı 0 kalır.

**Ortam transaction'ı (tur 2, doğrulayıcı bulgusu).** Çağıranın `DbContext`'i bir transaction içindeyse (ör. `SiparisiSatisaCevir` → `SatisFisi`, `CurrentTransaction is null ? Begin : null` deseni) açılan kapsamın bağlamı **aynı bağlantıyı** (`SetDbConnection`, sahiplik devredilmez) ve **aynı transaction'ı** (`UseTransactionAsync`) kullanır. Ayrı bağlantıda, ortam transaction'ında kapıdan önce yazılmış ama commit edilmemiş nakit satırları görünmezdi (ölçüldü: 20.000 commit + 7.000 aynı tx'te → paylaşımsız okuyucu 20.000 döndürdü). Değişmez: yasal kümülatif okuyucu çağıranın transaction görünürlüğünü birebir paylaşır; test `Ortam_transactioni_icinde_commit_edilmemis_nakit_kilitli_rolde_kumulatife_sayilir`, mutasyon M4-4.

**Onay.** Güvenlik yüzeyi: Mehmet (06 Güvenlik) onayı orkestratör kapısında alınacaktır. Alternatif (ayrı `IKiraciGeneliOkuma` DbContext'i) yeni mekanizma açtığı için seçilmedi.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
