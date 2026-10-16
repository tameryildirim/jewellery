# Vitrin Panosu — Faz A adım 1-3 · Canlı Test Raporu

**Tarih:** 2026-09-11 · **Koşum saati:** 14:46–15:0x (Europe/Istanbul)
**Kapsam:** PANO-KARAR-2026-09-11.md §4 maddeleri 2 ve 3.
**Commit atılmadı** (Kurucu kuralı).
**Veri:** canlı. Bu rapordaki bütün kotasyonlar gerçek kaynaklardan o an çekildi; uydurulmadı.

---

## 1. Yazılanlar

| Dosya | Durum | Ne |
|---|---|---|
| `dotnet/Kuyumcu.Api/Modules/Pano/PanoFiyatServisi.cs` | **yeni** | Marj + yuvarlama hesabı. Saf fonksiyon: veritabanı, ağ, saat okuma yok. |
| `dotnet/Kuyumcu.Tests/PanoFiyatTestleri.cs` | **yeni** | 43 test. Veritabanı gerektirmez. |
| `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs` | **değişti** (+3 satır alan, +1 metot) | `KurKaynakDefteri.SonCekim(kaynak)` — bayat tespitinin ön şartı. |

Mevcut hiçbir modül (Satış, Cari, Kasa, Ürün, Fatura, Rapor…) **açılmadı bile**. Pano ERP'ye eklenti; ERP'nin amacı bükülmedi. Faz 0 §11 mührü duruyor: `urunler.parite_kodu` ne okundu ne yazıldı.

**Test sonucu:** `dotnet test` → **124/124 başarılı** (81 eski + 43 yeni). Eski testlerin hiçbiri kırılmadı.

---

## 2. Canlı kaynak güvenilirliği (8 ardışık tur)

| Kaynak | Başarı | Süre |
|---|---|---|
| ALTINKAYNAK | **8/8** | 378–1033 ms |
| ANKARA | **8/8** | 154–722 ms |
| TRUNCGIL | **6/8** | 108–336 ms |

Truncgil iki turda `HttpRequestException` verdi; aynı anda `curl` ile HTTP 200 dönüyordu, yani kaynak ayakta, bağlantı ara ara kopuyor. **Geçici, ama pano bunu tolere etmek zorunda** — kararın "bayat veri ekrandan kaldırılır" maddesinin gerekçesi tam olarak budur.

---

## 3. Marj hesabı — canlı doğrulama

Altınkaynak, 11.09.2026 ~14:5x. Kuyumcu ayarı: gram −40/+60, çeyrek −150/+250, yarım −300/+500, tam −600/+900 (adım 50), Ata −600/+900 (adım 50, etiket "Cumhuriyet"), bilezik −50/+120, gümüş −2/+4 (yuvarlama kapalı).

| Kalem | Ham alış | Ham satış | **Panoda alış** | **Panoda satış** |
|---|---|---|---|---|
| Gram Altın | 6.675,98 | 6.785,49 | **6.630,00** | **6.850,00** |
| Yeni Çeyrek | 10.740,80 | 11.400,00 | **10.590,00** | **11.650,00** |
| Yeni Yarım | 21.488,05 | 22.800,00 | **21.180,00** | **23.300,00** |
| Yeni Tam | 42.937,34 | 45.595,00 | **42.300,00** | **46.500,00** |
| Cumhuriyet | 44.101,67 | 47.710,00 | **43.450,00** | **48.600,00** |
| 22 Ayar Bilezik | 6.118,89 | 6.590,00 | **6.060,00** | **6.710,00** |
| Gümüş (gram) | 90,07 | 100,43 | **88,07** | **104,43** |

Doğrulandı:
- **Marj sabit TL.** Her satırda (pano fiyatı − ham kotasyon) ayarlanan marja eşit, sapma yalnız yuvarlama adımı kadar.
- **Yön doğru.** Satış yukarı, alış aşağı — her satırda kuyumcunun lehine.
- **Yuvarlama adımı işliyor.** Adım 50 olan Tam/Cumhuriyet 50'nin katına oturdu; adım 0 olan gümüş kuruşunu korudu (88,07 / 104,43).
- **Gizlilik.** `PanoSatir` kaydında marj ve ham kotasyon **alanı yok** — testle de sabitlendi.

---

## 4. BULGU 1 — Kaynakların makası çok farklı, aynı marj aynı anlama gelmiyor

Makas = (satış − alış) / alış, aynı anda ölçüldü:

| Kalem | ALTINKAYNAK | ANKARA |
|---|---|---|
| HAS | 0,78% | 0,76% |
| GRAM | 1,64% | 1,56% |
| **CEYREK_YENI** | **6,13%** | **1,64%** |
| **YARIM_YENI** | **6,10%** | **1,46%** |
| **TAM_YENI** | **6,19%** | **1,30%** |
| **ATA_YENI** | **8,18%** | **1,97%** |
| **BILEZIK_22** | **7,66%** | **0,76%** |
| **AYAR_14** | **13,57%** | **0,74%** |
| **AYAR_18** | **16,94%** | **0,76%** |
| GUMUS | 11,52% | 10,11% |

Gram ve has'ta iki kaynak neredeyse aynı. **Sarrafiyede ve işçilikli kalemlerde uçurum var.** Okuması şu: Altınkaynak'ın "satış"ı zaten perakendeye yakın bir fiyat, Ankara'nınki toptancı makası.

**Sonucu doğrudan ürünü ilgilendiriyor:** kuyumcu Altınkaynak'tayken çeyreğe +250 TL marj koyarsa, kaynağın içindeki %6'lık makasın **üstüne** koymuş olur. Aynı marjı Ankara'dayken koyarsa toptancı fiyatının üstüne koyar. Yani **kaynak değiştirince marj sabit kalsa bile vitrindeki fiyat zıplar.**

Bu bir hata değil, bir tasarım kararı gerektiriyor ve **kararı henüz alınmadı.** Seçenekler: (a) marjı kaynak başına ayrı tutmak, (b) marjı hep alış tarafına uygulayıp satışı alış+marj olarak türetmek, (c) kaynak değişince kuyumcuyu uyarmak. Bu Kurucu'ya/oya gidecek bir maddedir — **tahminle kapatmadım.**

---

## 5. BULGU 2 — Kaynakta bozuk veri var, koruma eklendi

Altınkaynak, **Hamit Altın (kod PH)**, kaynağın kendi ham çıktısı:

```
{'Alis': '44.052,56', 'Satis': '43.250,00', 'Kod': 'PH',
 'Aciklama': 'Hamit Altın', 'GuncellenmeZamani': '11.09.2026 14:48:41'}
```

**Alış, satıştan 802,56 TL yüksek.** Üç ayrı turda da aynı geldi — geçici dalgalanma değil. Doğrudan `curl` ile kaynağın kendisinden de teyit edildi; bizim adaptörümüzün hatası **değil**.

Bu vitrine çıksaydı kuyumcu aldığından ucuza satıyor görünürdü — müşteri dükkâna satıp aynı anda geri alarak kuyumcuyu zarara sokabilirdi.

**Eklenen koruma:** son fiyatlarda alış ≥ satış ise satır **ekrana çıkmaz**, sebebiyle birlikte (`TERS_MAKAS`) yönetim ekranına düşer. Sessizce yutulmaz (Madde 1).

Canlı kanıt — kuyumcu Hamit'i marjsız seçtiğinde:

```
  PANODA GÖRÜNECEK (7 satır):
    ...
    Gümüş (gram)                     88,05          104,42   16,37

  PANOYA ÇIKMAYANLAR (yalnız yönetim ekranında görünür):
    ! HAMIT          TERS_MAKAS
```

Aynı kuyumcu Hamit'e −600/+900 marj verdiğinde satır **geri geliyor** (43.450 / 44.150), çünkü marj makası düzeltiyor. Koruma ekrandaki değişmezi korur, kaynağın verisini düzeltmez.

**Açık kalan:** marj bozuk kotasyonu maskeleyebiliyor. Kaynak seviyesinde ters makas, satır ekranda kalsa bile yöneticiye ayrı bir uyarı olarak gösterilmeli. Bunu **yapmadım** — ek tasarım kararı, senin onayını bekliyor.

---

## 6. Atlama sebepleri

Bir kalem şu hâllerde panoya çıkmaz ve sebebi yönetim ekranına bildirilir:

| Sebep | Ne zaman |
|---|---|
| `KOTASYON_YOK` | Kaynak o kalemi hiç vermedi (ör. Ankara'da `HURDA_22` yok) |
| `FIYAT_YOK` | Kalem geldi ama iki fiyatı da boş |
| `TERS_MAKAS` | Alış ≥ satış |
| `TAVAN_ASILDI` | 16 satır doldu |

Kuyumcunun kasten kapattığı kalem bu listeye **girmez** — o bir sorun değil.

---

## 7. Sırada ne var (henüz yapılmadı)

§4'ün kalan maddeleri: migration (`pano_ayarlari`, `pano_kalemleri`, `pano_cihazlar`), güvenlik filtresi + oran sınırı, SSE uçları, sade HTML pano sayfası, yönetim ekranı, sıkıştırma, kurulum runbook'u.

**Mehmet'in yayın vetosu duruyor.** Faz 0'ın global kiracı filtresi vetosu da duruyor.

**§4/2 tamamlandı ama tam değil:** `SonCekim` artık var; bayat eşiğini (sunucu 60 sn / pano 20 sn) uygulayan kod henüz yazılmadı — o, veri uçlarıyla birlikte gelecek.
