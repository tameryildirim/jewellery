# Mimari Karar Kayıtları (ADR)

**Format:** Michael Nygard, *"Documenting Architecture Decisions"*, 15 Kasım 2011.
**Standart karşılığı:** ISO/IEC/IEEE 42010:2022 §6.10 — *Recording of architecture decisions and rationale*.

Her kayıt beş alandan oluşur: **Başlık · Bağlam · Karar · Gerekçe · Sonuçlar**. Kayıtlar **değiştirilmez**; bir karar geçersizleşirse durumu `Geçersiz kılındı` yapılır ve yerine yeni bir ADR yazılır.

## Durum etiketleri

| Etiket | Anlamı |
|---|---|
| `Kabul edildi` | Karar yürürlükte |
| `Gözden geçirilmeli` | Devralınan veya gerekçesi doğrulanmamış durum; karar bekliyor |
| `Geçersiz kılındı` | Yerine başka bir ADR geçti (numarası belirtilir) |

## Kayıtlar

| # | Başlık | Durum | Tarih |
|---|---|---|---|
| [0001](0001-cok-kiracili-izolasyon-iki-katman.md) | Çok kiracılı izolasyon iki katmanla sağlanır | Kabul edildi | 2026-09-11 |
| [0002](0002-sistem-kapsami-tek-kacis-yolu.md) | `IgnoreQueryFilters` yerine tek ve denetlenebilir kaçış yolu | Kabul edildi | 2026-09-11 |
| [0003](0003-alan-bazli-sifreleme.md) | Kimlik alanları AES-256-GCM, aranabilirlik HMAC ile | Kabul edildi | 2026-09-11 |
| [0004](0004-denetim-izi-append-only.md) | Denetim izi append-only'dir ve kod tarafından zorlanır | Kabul edildi | 2026-09-13 |
| [0005](0005-mevzuat-esikleri-yapilandirmadan.md) | Mevzuat eşikleri koda gömülmez | Kabul edildi | 2026-09-11 |
| [0006](0006-kmts-kapsam-disi.md) | KMTS perakende kuyumcu için kapsam dışıdır | Kabul edildi | 2026-09-11 |
| [0007](0007-belge-uretimi-istemcide.md) | Resmî belgeler istemcide render edilir | Kabul edildi | 2026-09-13 |
| [0008](0008-saklama-suresi-maskeleme.md) | Saklama süresi dolunca silme değil maskeleme | Kabul edildi | 2026-09-12 |
| [0009](0009-veritabani-yabanci-anahtar-yok.md) | Veritabanında enforce edilen FK kısıtı yoktur | ⚠ **Gözden geçirilmeli** | 2026-09-13 |
| [0010](0010-elle-tenant-sartlari-korunur.md) | 98 adet elle `TenantId` şartı kaldırılmaz | Kabul edildi | 2026-09-12 |
| [0011](0011-testler-gercek-mysql.md) | Testler gerçek MySQL'e karşı çalışır | Kabul edildi | 2026-09-10 |
| [0012](0012-efatura-sahte-entegrasyonla-yapilmaz.md) | e-Fatura, gerçek entegratör ortamı olmadan tamamlandı sayılmaz | Kabul edildi | 2026-09-11 |
| [0013](0013-tas-kokeni-fail-closed.md) | Taş kökeni bilinmiyorsa ürün satılamaz | Kabul edildi | 2026-09-14 |
| [0014](0014-veri-disa-aktarimi.md) | Dışa aktarım: şifreli arşiv, tek kullanımlık bağlantı, ham döküm yok | Kabul edildi | 2026-09-14 |
| [0015](0015-barkod-code128.md) | Barkod Code 128'dir; EAN-13 uydurulmaz | Kabul edildi | 2026-09-14 |
| [0016](0016-reyon-yerlesimi-defter-kaydi-degildir.md) | Reyon yerleşimi alt-bölümlemedir, defter kaydı değildir | Kabul edildi | 2026-09-17 |
| [0019](0019-acilis-gocu-geri-alma-ters-kayit.md) | Açılış göçü storno ile değil ters kayıtla geri alınır (storno istisnası) | Kabul edildi | 2026-10-01 |

## Açık karar

**ADR-0009** bir karar kaydı değil, bir **açık madde kaydıdır.** Veritabanında yabancı anahtar kısıtı bulunmaması devralınan bir durumdur; bilinçli bir tasarım tercihi mi yoksa ihmal mi olduğu doğrulanamamıştır. Üç seçenek arasında seçim yapılmadan kayıt `Kabul edildi` durumuna geçmeyecektir.
