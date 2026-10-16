# K4 — Referans Kapısı Denetimi

**Soran:** Mehmet (06 Güvenlik) · **Tarih:** 2026-09-14 · **Kapsam:** 36 ilişki

## Sorunun kendisi

> *"Veritabanında yabancı anahtar kısıtı yok ([ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md)). O hâlde her `*_id` alanı için **uygulama katmanında** bir kapı var mı? Tek tek gösterin."*

FK olmadığında bir kimlik alanını yanlış doldurmak veritabanı tarafından **engellenmez**. Tek savunma, yazma yolundaki doğrulamadır. Bu denetim o savunmanın tam envanteridir.

## Yöntem

1. `yetim-tara.sh` ile şemadaki 36 ilişki listelendi (3 ilişki FK kurulamaz kapsam dışı: polimorfik `varlik_id`, hedef tablosu olmayan iki `ayar_id`).
2. Her ilişki için **yazma yolu** okundu: değer nereden geliyor?
   - **Güvenilir kaynak** — `islem.TenantId`, `islem.Id`, `islem.SubeId` (JWT'den, imzalı) veya aynı transaction'da üretilen kimlik → kapı gerekmez.
   - **İstemciden gelen** (`dto.*Id`) → **kapı ZORUNLU**.
3. İstemciden gelen her alan için doğrulamanın varlığı ve kiracı şartı ayrıca okundu.

## Sonuç — üç boşluk bulundu, üçü de kapatıldı

| # | İlişki | Bulgu | Ağırlık | Durum |
|---|---|---|---|---|
| K4-1 | `kasalar.sube_id` | `KasaServisi.Olustur`, `dto.SubeId`'yi **doğrulamadan** yazıyordu. Başka kiracının ya da hiç var olmayan bir şubenin kimliğine bağlı kasa açılabiliyordu. | Orta — kasa satırı kendi kiracısına yazılıyor, veri sızmıyor; ama referans askıda kalıyor ve şube bazlı görünümlerden kayboluyor. | ✅ `IslemDogrulayici.SubeDogrula` eklendi |
| K4-2 | `kullanicilar.sube_id` | `Olustur`, `Guncelle` ve `KendinGuncelle` `dto.SubeId`'yi **doğrulamadan** yazıyordu. | **Ağır** — `subeId` JWT'ye girer; satış/alış `islem.SubeId`'yi doğrudan `hareketler.sube_id`'ye yazar. Yabancı bir şube kimliği bu yolla **defterlere** sızardı. | ✅ Üç yolda da doğrulanıyor; açık `null` (şubesiz merkez personeli) bilinçli olarak serbest |
| K4-3 | `faturalar` okuması (`BankaModulu`) | Fatura, commit sonrası **elle kiracı şartı olmadan** okunuyordu. Global sorgu filtresi koruyordu ama [ADR-0010](../02-mimari/adr/0010-elle-tenant-sartlari-korunur.md) tek katman bırakmayı yasaklar. | Düşük — global filtre tutuyordu | ✅ Elle `TenantId` şartı eklendi |

**Mutasyon kanıtı:** üç kapı da devre dışı bırakıldığında `K4ReferansKapisiTestleri`nden **4 test kırmızıya döndü** (`Kasa_BASKA_kiracinin_subesine_acilamaz`, `Kasa_OLMAYAN_subeye_acilamaz`, `Kullanici_BASKA_kiracinin_subesine_atanamaz`, `Kullanici_guncellemesinde_de_sube_dogrulanir`); kapılar geri konunca 591/591 yeşil.

## Kapı envanteri — istemciden gelen kimlikler

| Alan | Nereden | Kapı | Yer |
|---|---|---|---|
| `kur_snapshot_id` | `dto` | `KurDogrula` | Satış ×4, Emanet ×2, Cari ×2, Takas, Transfer (10 çağrı) |
| `cari_id` | `dto` | `CariDogrula` | Satış ×4, Emanet ×2, Banka, Fatura (8 çağrı) |
| `kasa_id` | `dto` | `KasaDogrula` | Satış akışları (4 çağrı) |
| `sube_id` / `karsi_sube_id` | `dto` | `SubeDogrula` | Transfer (kaynak + hedef), StokSayimGuncelle, **+ Kasa ve Kullanıcı (bu denetimle eklendi)** |
| `urun_id` / `yeni_urun_id` | `dto` | `UrunBul` — kiracı şartlı `FirstOrDefault`, bulunamazsa 404 | Satış, Alış, Takas, Transfer |
| `hareket_id` | `dto` | Kiracı şartlı okuma | MASAK, Fatura |
| `kimlik_tespiti_id` | `dto` | `KimlikTespitiDogrula` | MASAK — *önceki turda bulunan boşluk* |
| `banka_hesap_id` | yol parametresi | `TenantHesabiBul` | Banka |
| `fatura_id` | aynı transaction'da üretildi | — | Banka |
| `ayar_id` | `dto` | `Ayarlar.Bul` — kodda sabit liste, tabloya bakmaz | Ürün, Takas, Hurda |

## Güvenilir kaynaklar — kapı gerekmeyenler

`tenant_id`, `kullanici_id`, `tespit_eden_kullanici_id`, `bildiren_kullanici_id`, `alan_kullanici_id` ve `islem.SubeId` üzerinden yazılan `sube_id` alanları **JWT'den** gelir. JWT imzalıdır ve kiracı bağlamı `IKiraciBaglami` ile zorlanır; istemci bunları seçemez.

`duzeltilen_id` alanları (`gider_pusulalari`, `masak_beyanlari`, `masak_kimlik_tespitleri`) düzeltme akışında **aynı kiracının** kaydından okunarak konur.

## Açık kalan — koda değil VERİYE ait

`kuyumcu_net` içinde **2 satırlık kiracı sızıntısı** duruyor (`hareketler.cari_id` 1, `kasa_hareketleri.kasa_id` 1). Faz 0 sızma testimizin kalıntısıdır, [teknik borç envanterinde VB-02](teknik-borc-envanteri.md) olarak kayıtlıdır ve **sessizce silinmemiştir** (Mehmet'in K3 şartı). FK Dalga 1 başlamadan önce Kurucu kararı gerekir.

## Bu denetimin FK planına etkisi

ADR-0009'un (c) seçeneği — bileşik `(id, tenant_id)` FK, yalnız RESTRICT, 4 dalga — **artık başlatılabilir durumdadır**: yetim sayısı 0, kapı envanteri tam, üç boşluk kapalı. Kalan tek engel VB-02 kararıdır.
