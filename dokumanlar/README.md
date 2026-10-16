# Kuyumcu ERP — Proje Dokümantasyonu

**Ürün:** Kuyumcu ERP (çok kiracılı kuyumculuk işletme yönetim sistemi)
**Geliştirici:** ByGatech A.Ş.
**Belge seti sürümü:** 1.1 · **Tarih:** 2026-09-14
**Dil:** Türkçe (standart adları özgün hâliyle İngilizce bırakılmıştır)

---

## 0. Bu belge seti nasıl okunmalı

Bu set üç ilkeye göre yazılmıştır:

**(1) Her rakam ölçülmüştür.** Bu belgelerde "yaklaşık", "civarında", "tahminen" geçmez. Bir sayı varsa, onu üreten komut da yanında yazılıdır ve tekrar çalıştırılabilir. Ölçülemeyen bir şey "ölçülmedi" diye yazılır.

**(2) Diyagramlar üretilir, çizilmez.** ER diyagramları canlı veritabanı şemasından `diyagramlar/er-uret.py` ile üretilir. Elle çizilen diyagram ilk şema değişikliğinde yalana döner.

**(3) Zayıflıklar gizlenmez.** `08-degerlendirme/` altındaki öz-değerlendirme, bu projenin bir teknik incelemede alacağı eleştirileri **bizim kendimizin** bulup yazdığı bölümdür. Bir alıcının bulacağı şeyi ondan önce biz yazarız.

> Bu üç ilke ByGatech A.Ş. Anayasası Madde 1'in ("Ne atlama, ne varsayım") belge karşılığıdır.

---

## 1. Standart temeli

Bu set aşağıdaki **güncel** standartlara dayanır. Her standardın durumu 2026-09-13'te doğrulanmıştır.

| Alan | Standart | Sürüm | Durum |
|---|---|---|---|
| Gereksinimler | **ISO/IEC/IEEE 29148** — Requirements engineering | 2018 (2. baskı) | Active |
| Mimari tanımı | **ISO/IEC/IEEE 42010** — Architecture description | 2022 (2. baskı) | Active |
| Kalite modeli | **ISO/IEC 25010** — SQuaRE, Product quality model | **2023** (2. baskı) | Active |
| Test belgeleri | **ISO/IEC/IEEE 29119-3** — Test documentation | 2021 (2. baskı) | Active |
| Yaşam döngüsü | **ISO/IEC/IEEE 12207** — Software life cycle processes | 2017 | Active |
| Uygulama güvenliği | **OWASP ASVS** | **5.0.0** (30 Mayıs 2025) | Güncel |
| Bilgi güvenliği | **ISO/IEC 27001** / **27002** | 2022 (+27001 Amd 1:2024) | Active |
| Proje yönetimi | **PMBOK Guide** | **8. baskı** (ANSI/PMI 99-001-2025) | Güncel |
| Mimari gösterim | **C4 model** (Simon Brown) · **arc42** (CC BY-SA 4.0) | — | Güncel |
| Karar kaydı | **ADR** (Michael Nygard, 15 Kasım 2011) | — | Güncel |

### ⚠ BİLİNÇLİ OLARAK KULLANILMAYAN STANDARTLAR

Aşağıdakiler internetteki şablonların çoğunda hâlâ görülür ve **artık geçerli değildir.** Bunlara dayanan bir belge seti, profesyonellik iddiasını güçlendirmek yerine çürütür:

| Standart | Durum | Yerine |
|---|---|---|
| **IEEE 830-1998** (SRS) | **Geri çekildi** | ISO/IEC/IEEE 29148:2018 |
| **IEEE 1016-2009** (SDD) | **Inactive-Reserved** (2020-03-05) | ISO/IEC/IEEE 42010:2022 + arc42 |
| **IEEE 829-2008** (Test dok.) | **Superseded** | ISO/IEC/IEEE 29119 serisi |
| ISO/IEC 25010:**2011** | 2023 ile iptal edildi | ISO/IEC 25010:**2023** (9 karakteristik) |
| ISO/IEC 12207:**2008** | 2017 ile değiştirildi | ISO/IEC/IEEE 12207:**2017** (4 süreç grubu) |

Standart durum doğrulamaları: `standards.ieee.org`, ISO resmî önizleme PDF'leri.

---

## 2. Belge dizini

### 01 — Gereksinimler *(ISO/IEC/IEEE 29148:2018)*
| Belge | İçerik |
|---|---|
| [`01-gereksinimler/SRS.md`](01-gereksinimler/SRS.md) | Software Requirements Specification — §9.6 yapısı |
| [`01-gereksinimler/StRS.md`](01-gereksinimler/StRS.md) | Paydaş gereksinimleri, kullanım senaryoları |
| [`01-gereksinimler/mevzuat-gereksinimleri.md`](01-gereksinimler/mevzuat-gereksinimleri.md) | Yasal gereksinimlerin birincil kaynak izlenebilirliği |

### 02 — Mimari *(ISO/IEC/IEEE 42010:2022 + arc42 + C4)*
| Belge | İçerik |
|---|---|
| [`02-mimari/mimari-tanimi.md`](02-mimari/mimari-tanimi.md) | Architecture Description — 42010 §6 zorunlu maddeleri |
| [`02-mimari/adr/`](02-mimari/adr/) | Mimari Karar Kayıtları (Nygard formatı) |

### 03 — Kalite *(ISO/IEC 25010:2023)*
| Belge | İçerik |
|---|---|
| [`03-kalite/kalite-gereksinimleri.md`](03-kalite/kalite-gereksinimleri.md) | 9 karakteristik × ölçülebilir hedef × ölçüm yöntemi |

### 04 — Test *(ISO/IEC/IEEE 29119-3:2021)*
| Belge | İçerik |
|---|---|
| [`04-test/test-politikasi.md`](04-test/test-politikasi.md) | Test Policy (§6) |
| [`04-test/test-plani.md`](04-test/test-plani.md) | Test Plan (§7) |
| [`04-test/test-tamamlama-raporu.md`](04-test/test-tamamlama-raporu.md) | Test Completion Report (§7) — gerçek koşum çıktılarıyla |

### 05 — Güvenlik *(OWASP ASVS 5.0.0 + ISO/IEC 27002:2022)*
| Belge | İçerik |
|---|---|
| [`05-guvenlik/asvs-uygunluk-matrisi.md`](05-guvenlik/asvs-uygunluk-matrisi.md) | V1–V17 bölümlerine karşı durum |
| [`05-guvenlik/tehdit-modeli.md`](05-guvenlik/tehdit-modeli.md) | Tehdit modeli ve karşı önlemler |

### 06 — Süreç *(ISO/IEC/IEEE 12207:2017)*
| Belge | İçerik |
|---|---|
| [`06-surec/yasam-dongusu.md`](06-surec/yasam-dongusu.md) | 4 süreç grubu · uygulanan/uygulanmayan süreçler (tailoring) |
| [`06-surec/runbook.md`](06-surec/runbook.md) | **İşletme kılavuzu** — kurulum, ölçüm, yedek, olay müdahalesi |

### 07 — Proje yönetimi *(PMBOK 8)*
| Belge | İçerik |
|---|---|
| [`07-proje-yonetimi/proje-plani.md`](07-proje-yonetimi/proje-plani.md) | Kapsam, iş paketleri, kilometre taşları |
| [`07-proje-yonetimi/risk-kaydi.md`](07-proje-yonetimi/risk-kaydi.md) | Risk kaydı |

### 08 — Değerlendirme
| Belge | İçerik |
|---|---|
| [`08-degerlendirme/teknik-oz-degerlendirme.md`](08-degerlendirme/teknik-oz-degerlendirme.md) | **Teknik due diligence öz-değerlendirmesi** — bulunacak zayıflıkları önce biz yazdık |
| [`08-degerlendirme/teknik-borc-envanteri.md`](08-degerlendirme/teknik-borc-envanteri.md) | Teknik borç envanteri (TB-01…TB-17 + veri borcu VB-01/VB-02) |
| [`08-degerlendirme/k4-referans-kapisi-denetimi.md`](08-degerlendirme/k4-referans-kapisi-denetimi.md) | **K4 — kimlik alanı kapı denetimi**: 36 ilişki tek tek, 3 açık bulundu ve kapatıldı |

### Yük testi
| Dosya | İçerik |
|---|---|
| [`../dotnet/yuk-testi/k6-senaryo.js`](../dotnet/yuk-testi/k6-senaryo.js) | k6 senaryosu — POS deseni, çok kullanıcılı, oran sınırı **gevşetilmeden** |

### Diyagramlar
| Dosya | İçerik |
|---|---|
| [`diyagramlar/er-uret.py`](diyagramlar/er-uret.py) | ER diyagramı üreteci (şemadan) |
| [`diyagramlar/er-ciz.sh`](diyagramlar/er-ciz.sh) | `.mmd` → `.svg` çizici |
| `diyagramlar/er-*.mmd` / `.svg` | Genel + 4 konu alanı ER diyagramı |
| `diyagramlar/c4-*.mmd` / `.svg` | C4: System Context · Container · Component |
| `diyagramlar/uml-1-kullanim-senaryolari` | UML Use Case — roller ve yetki sınırları |
| `diyagramlar/uml-2-sira-esik-ustu-satis` | UML Sequence — eşik üstü satış, iki fazlı MASAK kapısı |
| `diyagramlar/uml-3-aktivite-hurda-alimi` | UML Activity — hurda alımı, MASAK + VUK birlikte |

---

## 3. Diyagramları yeniden üretme

```bash
cd dokumanlar/diyagramlar
python3 er-uret.py      # şemayı okur, .mmd üretir
./er-ciz.sh             # tüm .mmd -> .svg (ER + C4 + UML)
```

Gereksinim: çalışan MySQL kabı (`kuyumcu-mysql`) ve `npx`. Bağlantı `KUYUMCU_ER_VT`, `KUYUMCU_ER_KAP`, `KUYUMCU_ER_SIFRE` ortam değişkenleriyle değiştirilebilir.

---

## 4. Ürünün ölçülen durumu

Aşağıdaki rakamlar 2026-09-13'te ölçülmüştür. Üreten komutlar ilgili belgelerde yazılıdır.

| Ölçüt | Değer |
|---|---|
| C# kaynak satırı (migration hariç, test dahil) | 16.451 |
| Test kapsamı | **%72,4 satır** (Core %100 · Api %80,9) |
| Ortalama siklomatik karmaşıklık | **3,0** (409 fonksiyon) |
| Blazor ilk yükleme (Release, Brotli) | **2,42 MB** |
| Bağımlılık | 128 paket · **0 zafiyet** · **0 copyleft** |
| Blazor `.razor` satırı | 5.072 (14 sayfa) |
| API ucu (`[HttpX]`) | 100 |
| Modül | 18 klasör, 17'si uç içerir |
| Veri tablosu | 21 (262 sütun, 54 indeks) |
| Otomatik test | **392 — tamamı başarılı** |
| Veritabanı göçü (migration) | 9 |
| Zafiyetli bağımlılık | 0 (`dotnet list package --vulnerable`) |
| CI denetim betiği | `dotnet/denetim.sh` — TEMİZ |

---

## 5. Belge durumu

Bu set **canlı belgedir**. Her belgenin başında sürüm, tarih ve durum alanı vardır (ISO/IEC/IEEE 29119-3:2021 §5 ortak zorunlu alanları: *unique identifier, issuing organization, approval authority, change history, status*).

| Durum etiketi | Anlamı |
|---|---|
| `TASLAK` | Yazıldı, gözden geçirilmedi |
| `GÖZDEN GEÇİRİLDİ` | İç inceleme tamamlandı |
| `ONAYLANDI` | Kurucu onayı alındı |
| `KURUCU KALEMİ` | İçeriği için dış girdi bekleniyor (sözleşme, mali müşavir, hukuk danışmanı) |
