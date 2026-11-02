# ByGatech — DEMO GÜNÜ PROSEDÜRÜ (Cumartesi 26 Eylül 2026)

> Senaryonun kendisi: `DEMO-SENARYOSU-2026-09-26.md` (12 adım, 14,8 dk).
> Bu belge **demonun çalışır durumda olması için** yapılacakları sırasıyla yazar.
> Her madde bir ölçümden doğdu — tahmin yok.

---

## Üç ölçülmüş tuzak (neden bu prosedür var)

**T1 · Tohum tarihi.** Demo kiracısı (`DEMO-Vitrin Kuyumculuk`, tenant `01M37H5Z2NZA72V8GY8K4NHDCW`)
**23.09.2026** tarihli verilerle kuruldu. Panel'in "Bugün ne sattım?", "Kasada ne var?" kartları
**bugünü** gösterir → **cumartesi sabahı sıfır görünür.** Demo sabahı tohum yenilenmeli.

**T2 · Yeniden koşum çift kayıt üretir.** `demo-tohumla.js --eposta <mevcut>` kipi mevcut kiracının
**içine ek içerik** yazar (her koşumda yeni sonekli ürün/müşteri). Cumartesi bu kiple koşulursa
ekranda 24 ürün, 12 müşteri, çift adlar olur. **Doğru yol: taze kiracı** (`--sahip-eposta` ile
hatırlanabilir adres) + eski DEMO kiracısının satırlarını veritabanından silmek (K-1 izni var).

**T3 · 5199 eski yayını sunuyor.** İnceleme sunucusu **22.09 06:05** yayınını sunuyor. O yayında
yeni görsel dil (S-1), ByGatech adı (P-D2), 8 şema (B-1), altın pozisyonu (B-2) **YOK.**
Demo 5199'da yapılacaksa **cuma akşamı yeni yayın alınmalı** — şeritler birleşip dalga kapısı
(tam süit 0 kırmızı + denetim TEMİZ) geçtikten sonra.

---

## Sıra

### Cuma akşamı (orkestratör)
1. Şeritler birleşti mi → **dalga kapısı**: `dotnet build` 0/0 · tam süit **0 kırmızı** · `bash dotnet/denetim.sh` TEMİZ.
   Kapı geçmeden yayın ALINMAZ.
2. **Yeni yayın** → 5199 (`betikler/qa-sunucu.sh`; P-41 yayın bütünlük kapısı SHA-256 envanterini
   doğrular, P-43 duman testi 6 rotayı + `/kilavuz`'u açar). `ham/YAYIN-ETIKET` güncellenir.
3. Duman: `/giris` 200 · `/kilavuz` 200 (tur 8'de 404'tü) · `/_framework/*` hepsi 200.

### Cumartesi 06:30 (orkestratör)
4. **Taze demo kiracısı**, hatırlanabilir adresle:
   `node betikler/demo-tohumla.js --sahip-eposta <adres> --sema platin --olcek 18`
   (ortam: `Sistem__Anahtari` + çalışan API; betik 17 maddeyi sunucudan geri okur, biri tutmazsa çıkış 1).
5. Eski `DEMO-Vitrin Kuyumculuk` kiracısının satırları silinir (veritabanı satırı — kurucu izni K-1).
6. Ölçüm: Panel'in günlük kartları **dolu** (satış > 0, kasa > 0, net ≥ 0), kasa **eksi değil**.

### Cumartesi 07:30 (kurucu + orkestratör)
7. **Giriş kurucuda.** Demo hesabının parolası `betikler/demo-tohumla.js` içinde sabit
   (satır ~729). Ekip parolayı **hiçbir çıktıya yazmaz ve bir forma yazmaz** — güvenlik kuralı.
   Kurucu tarayıcı bölmesinde **bir kez giriş yapar**; ardından orkestratör aynı oturumda
   senaryonun 12 adımını tıklayıp ekran görüntüleriyle doğrular.

---

## Bilinen zayıflıklar (demoda söylenmeyecek, bilinmesi gereken)

* **7 günlük grafik tek gün dolu.** Satış uçlarında `islemTarihi` alanı yok; geçmiş tarihli satış
  API'den girilemez. Veritabanına elle geçmiş yazmak defterin tek yazma yolunu (HareketFabrikasi)
  atlar → **yapılmaz.** Demoda grafik vurgulanmaz.
* **"Bekleyen işler" boş.** Demo kiracısında MASAK/süre/risk kaydı yok.
* **Girişten sonraki ekranlar tarayıcıda gezilmedi** (P-D3 ajanı parola giremez); o ekranlar
  çağırdıkları uçların yanıtları okunarak doğrulandı (18 uç, hepsi 200). Gerçek tarayıcı turu 7. adımda.

## Güvenlik kaydı (demo sonrası kapatılacak)

`demo-tohumla.js` demo hesabının parolasını **kaynakta sabit** taşıyor (satır ~706, ~729).
Demo hesabı yerel ve atılabilir olduğu için cumartesiye kadar tolere edildi. Demo sonrası:
parola yalnız ortam değişkeninden gelecek, varsayılanı olmayacak (Mehmet kuralı —
`yetim-tara.sh:30` ve `docker-compose.yml:3` ile aynı sınıf bulgu).
