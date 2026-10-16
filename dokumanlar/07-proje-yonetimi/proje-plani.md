# Proje Yönetim Planı

| | |
|---|---|
| **Belge kimliği** | KYM-PP-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **PMBOK Guide 8. baskı** (ANSI/PMI 99-001-2025) |

> ⚠ PMBOK **7. baskı artık güncel değildir.** 8. baskı 6 ilke, 7 performans alanı ve 5 odak alanı (Initiating · Planning · Executing · Monitoring and Controlling · Closing) tanımlar.

---

## 1. Proje tanımı

**Ürün:** Kuyumcu ERP — çok kiracılı kuyumculuk işletme yönetim sistemi
**Durum:** Devralınan kod tabanının geliştirilmesi; **yayında değil**, pilot öncesi
**Geliştirme modeli:** AI ile yönetilen ekip; kararlar çok ajanlı oylamayla, Tamer Bey mührüyle

---

## 2. Kapsam *(Scope domain)*

### 2.1 Kapsam içi

| Alan | Durum |
|---|---|
| Stok, satış, cari, kasa, banka, fatura, emanet, raporlama | ✅ Mevcut |
| Çok kiracılık ve rol bazlı yetki | ✅ Mevcut |
| MASAK uyumu (kimlik, beyan, ŞİB, saklama) | ✅ Mevcut |
| Gider pusulası + stopaj (VUK md.234, GVK md.94) | ✅ Mevcut |
| POS–fatura mutabakatı | ✅ Mevcut |
| Denetim izi (KVKK md.12) | ✅ Mevcut |
| Uyum ekranları | ⬜ PLAN-2 Adım 1 |
| 30.000 TL tevsik kümülasyonu | ⬜ PLAN-2 Adım 2 |
| Sentetik taş etiketleme | ⬜ PLAN-2 Adım 3 |
| Veri dışa aktarımı | ⬜ PLAN-2 Adım 4 |
| Barkod / etiket | ⬜ PLAN-2 Adım 5 |
| e-Fatura | 🔒 K1 |

### 2.2 Kapsam dışı — bilinçli

RFID · toptan modülü · imalat/atölye · tamirat · personel/prim · masraf yönetimi · sertifika/katalog · mobil uygulama · e-ticaret entegrasyonu · TV pano (Faz A'nın kalanı).

**Gerekçe:** Yasal zorunluluk değil. Hedef segment (tek dükkân + 2-3 şubeli perakende) bunları istemiyor; isteyen segmente zaten gidilmiyor.

**Bedeli açıkça kabul edilmiştir:** Toptancı, imalatçı ve barkod/RFID isteyen mağazalar **bugün kapsam dışıdır**.

---

## 3. Kilometre taşları ve durum *(Schedule domain)*

### Tamamlanan

| # | Kilometre taşı | Kanıt |
|---|---|---|
| M1 | Faz 0 — 6 kritik hata kapatıldı | `FAZ0-DUZELTME-RAPORU` |
| M2 | Kiracı izolasyonu iki katman | Mutasyon: 13 testin 11'i |
| M3 | Oran sınırı + taşıma güvenliği | Gerçek 429 + Retry-After |
| M4 | MASAK veri modeli + kural motoru + akış | 36 sınır testi |
| M5 | Gider pusulası + stopaj | Canlı GP-2026-000001 |
| M6 | POS–fatura mutabakatı | Kasıtlı uyumsuz veride fark yakalandı |
| M7 | Test kapısı (HTTP + bUnit) + denetim izi okuma | 392 test |
| M8 | Ölçüm boşluklarının kapatılması | Kapsam, WASM, karmaşıklık, yedek, SBOM, lisans |
| M9 | **Belge seti** (standartlara bağlı) | Bu set |

### Bekleyen

| # | Kilometre taşı | Engel |
|---|---|---|
| M10 | Uyum ekranları | — |
| M11 | Tevsik + sentetik taş | — |
| M12 | FK dalgaları | Sızıntı kalıntısının temizlenmesi |
| M13 | CI/CD | — |
| M14 | e-Fatura | **K1** |
| M15 | Pilot kabul testleri | **K4** |
| M16 | Yayın | Güvenlik vetosu + K1/K4 |

### Süre tahmini — bilinçli olarak verilmiyor

Geçmiş hızdan çarpan uydurmak Anayasa md.1'e aykırı sayılmıştır. **Ölçülen tek süre:** test koşumu 392 test ≈ 4 saniye.

---

## 4. Kaynaklar *(Resources domain)*

| Rol | Sorumluluk |
|---|---|
| **Tamer Bey** | Orkestrasyon, karar mührü |
| **Bertan** (02) | Mimari; ADR sahipliği |
| **Taşkın** (03) / **Ertuğ** (03) | Backend / Frontend |
| **Aşkın** (04) | Kalite kapısı — **fail-closed** |
| **Mehmet** (06) | Güvenlik — **veto yetkisi** |
| **Batuhan** (05) | DevOps, yayın |
| **Eray** (08) | Satış, konumlandırma |
| **Mustafa** (09) | FinOps — fiyat/marj |
| **Kurucu** (insan) | Sözleşme, para, imza, dış taahhüt |

**Model çeşitliliği:** QA (Aşkın) ve Güvenlik (Mehmet) farklı model ailesinde çalışır — aynı kör noktayı paylaşmamaları için.

---

## 5. Yönetişim *(Governance domain)*

### Karar alma
1. Araştırma (**birincil kaynak zorunlu**)
2. Çok ajanlı oylama (EVET / HAYIR / KOŞULLU)
3. **İddia denetimi** — oy gerekçeleri ölçülür
4. Tamer Bey mührü
5. ADR

### Kalite kapıları — fail-closed
Aşkın'ın kapısından geçmeyen sevk edilmez. Mehmet'in vetosu **ayrıdır**; QA kapısından geçmek yayın izni değildir.

### Kurucuya çıkan kararlar
Sözleşme · para/harcama · dışa yayın · geçmişi yeniden yazan git işlemleri · hukuki metinler.

### Yürürlükteki kısıtlar
- **Commit atılmaz** (Kurucu kuralı)
- **Yayın vetosu duruyor** (Mehmet)
- Kaynak dilinde ve belgelerde **Türkçe**

---

## 6. Paydaşlar *(Stakeholders domain)*
→ [StRS §1](../01-gereksinimler/StRS.md)

**Kritik uyarı:** 8 paydaş gereksinimi hiçbir gerçek kullanıcıyla doğrulanmamıştır (K4).

---

## 7. Finans *(Finance domain)*

**Fiyat kararı Kurucu'ya aittir** (K7). Satış tarafının önerisi: 1.250 TL/ay/şube, yıllık peşin 15.000 TL.

⚠ **Birim maliyetimiz ölçülmemiştir.** Marj onayı FinOps'a (Mustafa) aittir. Bu bir öneridir, karar değildir.

Doğrulanmış rakip fiyat aralığı (3 yıllık toplam sahip olma maliyeti karşılaştırması için):

| Rakip | Fiyat |
|---|---|
| Kuyumcum | $30–40 / kullanıcı / ay |
| Ariyen | 5.000–15.000 ₺/ay |
| CetaSoft | 40.000 TL + KDV (**cihaz başı**) |
| Kuyumcu Programı | 69.000 TL tek seferlik |
| Terminal Gold | $4.000–9.000 lisans + yıllık bakım |
| M&L Perakende / Diamond / Gold ERP | 94.686 / 135.266 / **236.716** TL |
| Logo Tiger 3 (1 kullanıcı) | **350.100 TL** + KDV + yıllık LEM |

**19 üründen yalnız 6'sı fiyatını yayımlıyor.** Fiyat şeffaflığı tek başına bir konumlandırma seçeneğidir.

---

## 8. Risk *(Risk domain)*
→ [Risk kaydı](risk-kaydi.md) — 13 aktif risk (3 kritik), 8 gerçekleşmiş risk ve dersleri.

---

## 9. Kurucu kalemleri — kilit noktalar

| # | İş | Neyi kilitliyor |
|---|---|---|
| **K1** | e-Fatura entegratör sözleşmesi | e-Fatura adımının tamamı · en büyük ticari risk |
| **K2** | Mali müşavir: fatura haddi + stopaj oranları + MASAK kademeleri | Vergi hesaplarının kesinleşmesi |
| **K3** | Rakip demoları | ✅ Kısmen kapandı — araştırma yapıldı, **ürün içi görülmedi** |
| **K4** | Pilot kuyumcu erişimi | **Validation süreci** · 8 doğrulanmamış gereksinim |
| **K5** | Pilotun MASAK Online kaydı | Gerçek ŞİB gönderimi |
| **K6** | ŞİB XML şeması (XSD) | XML üretimi |
| **K7** | Fiyat kararı | Satış başlangıcı |
| **K8** | Konumlandırma metni onayı | "MASAK uyumluyuz" artık savunulamaz |
| **K9** | **Sır rotasyonu + git geçmişi temizliği** | Güvenlik — **en öncelikli** |
| **K10** | LICENSE + IP devir belgesi | Her ciddi görüşmede sorulur |

---

## 10. Kapanış ölçütleri *(Closing)*

Bu sürümün "tamamlandı" sayılması için:

1. PLAN-2 Adım 1–5 kabul ölçütleriyle kapanmış
2. CI hattı çalışıyor, kapılar otomatik
3. Sırlar döndürülmüş, geçmiş temizlenmiş
4. FK dalgaları uygulanmış, yetim taraması çıkış kodu 0
5. Pilot kabul testleri **gerçek kullanıcıyla** geçmiş (K4)
6. Mehmet'in yayın vetosu **yazılı olarak** kalkmış
7. Belge seti güncel

**Bugün 1, 2, 3, 4 ve 5 açıktır.** Madde 5 olmadan ürün "doğrulanmış" sayılamaz.
