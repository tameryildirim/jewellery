# Parite İş Listesi — MÜHÜRLÜ

**Tarih:** 2026-09-14 · **Soru:** *"Piyasadaki en güncel programlara göre, dış izin/sözleşme gerektirenler hariç, tamamen bizim elimizde olan ne kadar iş var? Rakiplerden en ufak bir fark olmayacak."*

---

## 0. Bu liste nasıl çıktı

| Adım | Sonuç |
|---|---|
| Rakip araştırması | 10 ayrı açı (M&L, Pusula/Dezy/Kuvvem, Milyem Plus/Kuyumsoft/Bilsoft, Logo/Mikro/Nebim/Akınsoft/DİA, KuyumHub/ZarifSoft/CetaSoft, vitrin panosu ürünleri, mobil uygulama mağazaları, donanım entegrasyonları, kullanıcı şikâyetleri, sektörel iş akışları) |
| Ham yetenek kalemi | **663** |
| Tekilleştirilmiş harita | **17 alan · 141 kalem** |
| Bağımsız doğrulama | 17 alanın **17'sinde** düzeltme çıktı — 108 düzeltme, 150 atlanmış kalem |
| Birleştirme | Elle. Doğrulayıcılar iki sistematik hata buldu: *(a)* aynı altyapı üç ayrı kalemde üç kez "BÜYÜK" sayılmış, *(b)* bazı kalemlerde rakip tarafı için kanıt yok |

**Toplam ajan: 45 · 4,7M token · 1.404 araç çağrısı.**

### Mühür kuralı
Bu liste **sabittir**. Bundan sonra bulunan her yeni şey varsayılan olarak **backlog**'a gider ve **engelleyici değildir**. Bir maddenin bu listeye girmesi için rakipte olduğunun kanıtlanması gerekir.

---

## 1. CEVAP

### Tamamen bize bağlı: **48 iş paketi · 149–220 iş günü**

Tek geliştirici, insan hızı varsayımıyla **≈ 7–11 ay**.

> **Ölçek notu:** bu tahminler *tek geliştirici* içindir. Bu oturumda PLAN-2'nin 5 adımı + K4 denetimi + yük testi + devir ölçümü bir oturumda bitti. Kendi hızımızı bu tabloya sen ölçekle — ben "AI ile 10 kat hızlı" gibi bir rakam uydurmam.

### Bize bağlı **olmayan**: 17 paket

| Sınıf | Paket | Ne gerekiyor |
|---|---|---|
| **B — donanım/veri** | 6 | Kodu yazarız, çalıştığını kanıtlamak için cihaz gerekir (RFID okuyucu, terazi, el terminali, TV/LED panel, ÖKC, gerçek cihaz matrisi) |
| **C — izin/sözleşme** | 11 | e-Fatura/e-Arşiv, e-İrsaliye, e-Defter, sanal POS, banka API, pazaryeri API, SMS/WhatsApp gönderim, GS1 üyeliği, PEP/yaptırım veri beslemesi, native mobil mağaza yayını, kargo API |

---

## 2. A SINIFI — tamamen bizim elimizde

### Katman 0 — Altyapı (6 paket · 24–37 gün)
*Bunlar bir kez yazılır, aşağıdaki onlarca kalemi açar. Sırayı bozmak işi büyütür.*

| # | Paket | Gün | Neyi açar |
|---|---|---|---|
| 1 | **Fiş / işlem grubu + kalem modeli** (fatura satır kalemi dâhil — TB-17) | 5–8 | Karma ödeme, takoz/karışık işlem, fatura satırı, KDV satır bazlı hesabı, milyem/işçilik ayrımı, e-Fatura ön şartı |
| 2 | **Stok maliyet partisi (lot) + maliyet motoru** | 5–8 | FIFO/LIFO/ortalama, kâr-zarar analizi, ürün kârlılığı, fire maliyeti |
| 3 | **Sipariş / iş emri çatısı** (durum makinesi) | 5–8 | Sipariş döngüsü, tamir-servis, fason atölye, üretim emri, CRM teklifi |
| 4 | **Dosya yükleme ve belge deposu** | 2–3 | Sertifika PDF'i, ürün görseli, pusula fotoğrafı, katalog |
| 5 | **Belge / etiket şablon motoru** | 4–6 | İrsaliye, atölye teslim fişi, kasa kapanış fişi, etiket tasarımı |
| 6 | **Bildirim kanalı + zamanlanmış iş çatısı** | 3–4 | Vade hatırlatma, uyarı kuralları, otomatik raporlar |

### Katman 1 — İmalat / atölye (7 paket · 24–36 gün)
*Rakiplerin en güçlü, bizim en zayıf olduğumuz alan. Bugün fire kavramı bile yok.*

| # | Paket | Gün |
|---|---|---|
| 7 | Atölye bölümü (tezgâh/döküm/ocak/cila) + **fire hareket türü** | 3–5 |
| 8 | İş emri: has giriş-çıkış bacakları + **otomatik fire = giren − çıkan** + usta ataması | 4–6 |
| 9 | **Ramat** takibi (rafineriye gönderim fişi → dönüş fişi → açık bakiye) | 3–4 |
| 10 | **Ayar düşürme/yükseltme** + katkı (bakır/gümüş) stoğu ve sarfı | 3–4 |
| 11 | **Üretim reçetesi (BOM)** + üretim emri + termin takvimi | 5–8 |
| 12 | **Atölye kasası**: nakit + has + hurda + taş + mamul çok varlıklı, bölümler arası transfer | 4–6 |
| 13 | **Mamul bozma/dönüş** (satılan mamul → ham has + taş) | 2–3 |

### Katman 2 — Satış / POS (6 paket · 14–21 gün)

| # | Paket | Gün |
|---|---|---|
| 14 | **Karma ödeme tek fişte** (nakit + kart + havale + altın + hurda) | 2–3 |
| 15 | **Döviz alış / satış** (sarraflık) ayrı işlem tipi | 2–3 |
| 16 | **Gelen iade / çıkan iade** ayrı işlem tipleri | 2–3 |
| 17 | **Çoklu fiyat listesi** + kampanya/hediye | 4–6 |
| 18 | POS ekranında **barkod okutma + canlı fiyat** (uç var, arayüz kullanmıyor) | 2–3 |
| 19 | **Vezne modeli** — tezgâhtar satar, vezne tahsil eder | 2–3 |

### Katman 3 — Stok / ürün kartı (7 paket · 20–29 gün)

| # | Paket | Gün |
|---|---|---|
| 20 | Metal tipi genişletme (**platin, paladyum, rodyum, çelik**) + saflık tabloları | 2–3 |
| 21 | **Çift birim stok**: adetli + gramlı aynı üründe | 3–4 |
| 22 | **Pırlanta 4C** (karat/renk/berraklık/kesim) + sertifika no + PDF arşivi | 4–6 |
| 23 | **Sayım oturumu** + vitrin/sistem fark raporu + deftere yazma (**TB-18**) | 4–6 |
| 24 | Çok seviyeli **ürün ağacı** (bölüm/cins/tür/grup) | 2–3 |
| 25 | **Reyon / vitrin** bazlı stok bölümlemesi | 2–3 |
| 26 | **Konsinye** ürünün kendi stoktan ayrılması | 3–4 |

### Katman 4 — Cari ve finans (7 paket · 22–31 gün)

| # | Paket | Gün |
|---|---|---|
| 27 | **Çoklu birim cari bakiye**: TL + has gram + USD/EUR | 3–4 |
| 28 | **Vade takibi** + geciken müşteri listesi + hatırlatma | 3–4 |
| 29 | **Çek / senet portföyü** (giriş, ciro, tahsile verme, vade takvimi, karşılıksız) | 5–7 |
| 30 | **Gün sonu kapanış** (sayılan nakit, fark, devreden bakiye, gün kilidi) | 3–4 |
| 31 | **POS cihazı** takibi + komisyon oranı + valör | 3–4 |
| 32 | **Banka ekstresi içe aktarma** + IBAN/ünvandan cari eşleştirme | 4–6 |
| 33 | Manuel borç/alacak fişi (gerekçe zorunlu) | 1–2 |

### Katman 5 — Raporlama (4 paket · 13–19 gün)

| # | Paket | Gün |
|---|---|---|
| 34 | **Rapor seti tamamlama** — M&L'de 19 rapor, bizde 5 uç + 3 döküm | 6–9 |
| 35 | **Kâr-zarar analizi** (paket 2'ye bağlı) | 3–4 |
| 36 | **Patron paneli** — operasyondan ayrı yönetici ekranı | 2–3 |
| 37 | **Pozisyon / net varlık** ekranı (açık pozisyon, has + döviz) | 2–3 |

### Katman 6 — Uyum, bize bağlı kısmı (5 paket · 14–20 gün)

| # | Paket | Gün |
|---|---|---|
| 38 | **KDV 23/e özel matrah otomatiği** + 805 neden kodu (bugün HasDeger elle giriliyor) | 2–3 |
| 39 | **Has altın defteri otomatiği** (kesilen belge deftere işlenmiyor) | 3–4 |
| 40 | **MASAK risk skorlama** + PEP/yaptırım tarama motoru | 4–6 |
| 41 | MASAK kapısının **banka hareketlerine de** yayılması | 2–3 |
| 42 | **Kâğıt sevk irsaliyesi** belge modeli (e-İrsaliye'nin ön şartı) | 3–4 |

### Katman 7 — Platform (6 paket · 18–27 gün)

| # | Paket | Gün |
|---|---|---|
| 43 | **Mobil / dar ekran uyarlaması** — 19 sayfa | 5–8 |
| 44 | **PWA kabuğu** (manifest, service worker, ana ekrana ekleme) | 2–3 |
| 45 | **İki aşamalı giriş (TOTP)** — TB-14 | 2–3 |
| 46 | Şube bazlı görünürlük + **yetki matrisi tamamlama** | 3–4 |
| 47 | **Veri göçü aracı** — rakip programdan içe aktarım | 4–6 |
| 48 | **Denetim izi tamamlama** (kasa/şube damgası, storno, basım izi) | 2–3 |

---

## 3. B SINIFI — kodu bizde, doğrulaması cihazda

| Paket | Neden B |
|---|---|
| RFID toplu okuma / toplu çıkış | "3 dakikada 1000 ürün" iddiası UHF okuyucu + anten + etiket olmadan kanıtlanamaz |
| Teraziden otomatik gram okuma | RS-232/USB protokolü yazılır, terazi olmadan doğrulanamaz |
| El terminali arayüzü + çevrimdışı sayım kuyruğu | Cihaz ergonomisi ve tarama hızı sahada ölçülür |
| TV / LED vitrin panosu cihaz tarafı | Panel + yayın zinciri gerekir (yazılım tarafı PANO-KARAR'da hazır) |
| ÖKC / yazarkasa entegrasyonu | Sertifikalı cihaz ve üretici SDK'sı gerekir |
| Gerçek cihaz / tarayıcı destek matrisi | Ölçüm gerçek cihazlarda yapılır |

**Not:** bu paketlerin **kod kısmı A'dır**. İstersen yazarız, doğrulamayı cihaz geldiğinde yaparız — ama o zaman "çalışıyor" diyemeyiz, "yazıldı, denenmedi" deriz.

---

## 4. C SINIFI — izin/sözleşme olmadan başlanamaz

| Paket | Neye bağlı |
|---|---|
| e-Fatura / e-Arşiv | Özel entegratör sözleşmesi |
| e-İrsaliye | Aynı sözleşme + kâğıt irsaliye modeli (paket 42) |
| e-Defter / e-Müstahsil | Entegratör + mali müşavir |
| Sanal POS ile online tahsilat | Banka/ödeme kuruluşu sözleşmesi |
| Banka API ile otomatik hareket çekme | Banka kurumsal API sözleşmesi |
| Pazaryeri adaptörleri (Trendyol vb.) | Satıcı hesabı + API anahtarı |
| SMS / WhatsApp gönderim | Operatör/BSP sözleşmesi *(WhatsApp derin bağlantı A'dır — izin istemez)* |
| GS1 / EAN-13 barkod | GS1 üyeliği (Code 128 zaten var, izin istemez) |
| PEP / yaptırım listesi beslemesi | Ticari veri aboneliği *(motor A — paket 40)* |
| Native mobil uygulama mağaza yayını | Apple/Google geliştirici hesabı |
| Kargo firması API'si | Kargo firması sözleşmesi |

---

## 5. Önerilen sıra

1. **Katman 0** (altyapı) — bunlar bitmeden üsttekiler 2–3 kat şişer.
2. **Katman 1** (imalat/atölye) — rakiplerin en güçlü, bizim en boş olduğumuz yer. Ticari fark en çok burada.
3. **Katman 2 + 4** (satış tipleri + finans) — günlük kullanımda en çok hissedilen eksikler.
4. **Katman 3 + 5** (stok derinliği + rapor) — satış görüşmesinde sorulan şeyler.
5. **Katman 6 + 7** (uyum tamamlama + platform).

---

## 6. Dürüst kayıt

- **Bu liste "rakip paritesi"dir, "iyi ürün" değil.** İçinde bizim bugün daha iyi yaptığımız şeyler yok (kiracı izolasyonu, denetim izi, mutasyon kanıtlı test, tevsik kapısı, sentetik taş kapısı — rakiplerin hiçbirinde bulunamadı).
- **Rakip ürünlerin İÇİ görülmedi.** Hiçbirinin demosu çalıştırılmadı; iddialar firmaların kendi sitelerinden, mağaza kayıtlarından ve kullanıcı şikâyetlerinden. Bir rakibin "var" dediği şeyin ne kadar çalıştığını bilmiyoruz.
- **Gün tahminleri tek geliştirici içindir** ve ±%40 hata payı taşır. Bir işin gerçek büyüklüğü ancak başlandığında bilinir.
- **Erişilemeyen kaynaklar** önceki pazar araştırmasındakiyle aynı: dia.com.tr (403), kuyumpos.com (403), nebim sektör sayfası (404), barsis/payedar (DNS), dijigold (SSL), e-yaz.com.tr (**ele geçirilmiş, girilmedi**), Logo Netsis has altın dokümanı (301→gövde okunamadı). Bu firmaların yetenekleri **eksik ölçülmüş olabilir**.
