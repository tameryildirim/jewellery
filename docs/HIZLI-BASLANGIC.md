# HIZLI BAŞLANGIÇ — kurulumdan ilk satışa

<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->

> **TASLAK — ilan edilmemiştir.** Bu belge bir süre **vaadi** değil, bir **yol
> haritası + ölçüm kaydıdır**. Ürünün 1.0'da neyi kapsamadığı tek kaynaktan
> okunur: **`docs/HARIC-LISTESI.md`**.
>
> **Paket:** P-29 (S-1) · **Tarih:** 2026-09-23

---

## 0. Kural: süre TAHMİN EDİLMEZ, ÖLÇÜLÜR — hem de iki kez

Köprü koşulu (**BK-29**): "kurulumdan ilk satışa geçen süre" **iki ayrı koşumla**
ölçülür — biri **ekip üyesi**, biri **ürünü hiç görmemiş kişi**. Tek ölçümle
rakam ilan edilmez.

**Bugünkü dürüst durum:**

| Koşum | Kim | Durum | Sonuç |
|---|---|---|---|
| Ö-1 | **Makine tabanı** (API seri yolu, betikle) | **YAPILDI** — 2026-09-23 12:43:45→12:43:50, localhost:5198 | **5 saniye** (8 istek) — bu bir *insan süresi değildir*, yolun **alt sınırıdır** |
| Ö-2 | **Ekip üyesi, ekrandan** (tarayıcı) | **YAPILMADI** | — |
| Ö-3 | **Ürünü hiç görmemiş kişi** | **YAPILMADI** — insan gerektirir (kurucu kalemi) | — |

→ **Bu yüzden bu belgede "X dakikada ilk satış" cümlesi YOKTUR** ve hiçbir kanalda
kullanılamaz. Ö-2 ve Ö-3 tamamlandığında bu tablo doldurulur ve rakam ancak o
zaman konuşulur.

---

## 1. Ölçülen yol — 8 adım (canlı koşum, 2026-09-23)

Aşağıdaki her adım gerçekten koşturuldu; yanıt kodları gerçek çıktıdandır.

| # | Adım | Uç | Sonuç |
|---|---|---|---|
| 1 | Davet üret (sistem yöneticisi) | `POST /api/v1/sistem/davetler` | **201** — tek kullanımlık jeton |
| 2 | İşletme kaydı (sahip hesabı) | `POST /api/v1/auth/kayit` | **201** — kiracı + **"Merkez Şube"** otomatik açıldı |
| 3 | Canlı kur bekle | `GET /api/v1/kur/guncel` | **~5 sn sonra** kur kaydı oluştu |
| 4 | **Kasa aç** | `POST /api/v1/kasalar` | **200** — *yeni kiracıda kasa listesi **boş** gelir; bu adım atlanamaz* |
| 5 | Ürün kartı | `POST /api/v1/urunler` | **201** (22 ayar, 10,5 g) |
| 6 | Alış = stok girişi | `POST /api/v1/islemler/alis` | **200** — has gram **9,618 g** hesaplandı |
| 7 | Müşteri kartı | `POST /api/v1/cari` | **201** |
| 8 | **İLK SATIŞ** | `POST /api/v1/islemler/satis` | **200** — TL **26.000,00**, has **−9,618 g** |

**SQL kanıtı (aynı kiracı):** `hareketler=2`, `kasa_hareketleri=2`, `urunler=1`,
`cariler=1`, `denetim_kayitlari=23`.

---

## 2. Koşumda ÇIKAN GERÇEK TUZAKLAR (kurulum yapan kişi bunları bilmeli)

Bunlar tahmin değil, bu koşumda **ısıran** durumlardır:

1. **Yeni kiracıda kasa YOKTUR.** `GET /api/v1/kasalar` → `[]`. Şube ("Merkez Şube")
   otomatik açılır, **kasa açılmaz**. Kasa açılmadan nakit satış yapılamaz.
2. **Kur kaydı hemen gelmez.** Kayıttan sonra ilk kur anlık görüntüsü **~5 saniye**
   sonra oluştu. Erken gönderilen işlem `kurSnapshotId` olmadığı için reddedilir.
3. **Tevsik haddi işlemi durdurur.** 20.000 TL'lik alış geçti; **40.000 TL**'lik
   nakit alış **400** ile reddedildi: *"toplam tutarı 40.000,00 TL, 30.000,00 TL
   haddini aşıyor… tutarın tamamı banka/yetkili kurum üzerinden"*. Bu bir hata
   değil, **kasıtlı yasal kapıdır**.
4. **Stoksuz satış yapılamaz.** Ürün kartı açmak stok yaratmaz; alış (ya da açılış
   stoku — bkz. `HARIC-LISTESI.md` § 2) yapılmadan satış **400** verir:
   *"Bu şubede ürün stoğu bulunamadı"*.
5. **Kayıt ucu saatte 3 istekle sınırlıdır** (IP başına). Arka arkaya deneme
   yapan kurulumcu **429** alır. (`Kuyumcu.Api/Common/OranSiniri.cs:120-122`)
6. **Kayıt daveti olmadan açılmaz.** Davetsiz `POST /auth/kayit` **403** döner.

---

## 3. Ekrandan yapılacak sıra (Ö-2 koşumu bunu ölçecek)

1. **Giriş** → davet bağlantısıyla işletme kaydı (`Pages/Giris.razor`)
2. **İşletme / Kurulum** → şube bilgisi, açılış bakiyeleri sihirbazı (`Pages/Isletme.razor`, `Pages/Kurulum.razor`)
3. **Kasalar** → en az bir kasa aç (`Pages/Kasalar.razor`)
4. **Ürünler** → ürün kartı ya da **CSV ile toplu ürün aktarımı** (`Pages/Urunler.razor`, `Pages/VeriIceAktarim.razor`)
5. **Cariler** → müşteri kartı ya da **CSV ile toplu cari aktarımı** (`Pages/Cariler.razor`)
6. **İşlemler / Tezgâh** → alış ile stok girişi, sonra satış (`Pages/Islemler.razor`, `Pages/Pos.razor`)
7. **Panel** → günün özeti, kasa ve has bakiyesi (`Pages/Panel.razor`)

> Devir stoğu ve açılış bakiyeleri için sınırlar: **`docs/HARIC-LISTESI.md` § 2**.

---

## 4. Ölçüm nasıl tekrarlanır (Ö-2 / Ö-3 için)

- Koşum **gerçek saatle** damgalanır: başlangıç = davet üretimi, bitiş = ilk
  satışın **200** yanıtı.
- Koşan kişi, adım adım **nerede takıldığını** yazar; takılma süresi ölçümden
  düşülmez (gerçek kullanıcı da takılır).
- İki koşumun **ikisi de** yazılmadan hiçbir kanalda süre rakamı kullanılmaz.
