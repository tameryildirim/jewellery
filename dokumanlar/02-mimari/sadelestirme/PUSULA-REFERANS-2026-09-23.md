# PUSULA REFERANS KARTI — "ezber" (23 Eylül 2026)

> Amaç: Pusula'yı bir daha araştırmamak. Bu kart `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 2'nin dayanağıdır ve `PUSULA-KIYAS-RAPORU-2026-09-21.md`'nin üstüne yazılmıştır (o rapordaki firma/fiyat/menü bulguları burada tekrarlanmaz, yalnız yeni görülenler eklenir).
> Kaynaklar (23 Eylül, uygulama içi tarayıcıyla bakıldı): pusulax.com (metin + 6 görsel: `hero-dashboard.png`, `sales-screen.png`, `production-screen.png`, `app-start-screen.png`, `app-price-screen.png`, logo) · pusulanet.net (ana sayfa, Perakende ürün sayfası, ders sayfası, online mağaza) · YouTube kanalı @pusulayazilim (36 video; **Pusulax Demo 10:51 (7 ay önce)** kare şeridiyle, **Perakende Satış İşlemi Genel 3:53 (2016)** kare şeridi + otomatik altyazı parçalarıyla).
> **Ölçülemeyenler (dürüst kayıt):** video kareleri tarayıcı bölmesinde siyah geldi; kare şeritleri 320×180 olduğundan **yazılar okunmuyor, yalnız düzen görüldü**. Tık sayısı hâlâ sayılamadı. Pusulax Demo'nun altyazısı yok. Klasik satış videosunun tam transkripti alınamadı (timedtext boş döndü); yalnız 6 altyazı parçası okundu.

---

## 1. İki Pusula var — müşteri hangisini gördü, sorulacak

| | **Klasik Pusula Perakende** (2016 videoları, hâlâ satılan masaüstü) | **PusulaX** (yeni nesil, 2025–26; web/masaüstü + PusulaApp) |
|---|---|---|
| Ana ekran | **Karo ızgarası**: solda dikey liste, ortada ~30 mavi büyük karo (6 sütun × 4–5 satır), her karo bir işlem. Menü yok, "ana menü" = karo sayfası (1:52'lik video bunu anlatıyor) | Sol kenar çubuğu (mor gradyan) + **sekmeli belge alanı** (Ana Ekran · Cari Hesap İşlemleri … sekme gibi açılır) + üstte kur şeridi |
| Satış ekranı | Tek pencere (turuncu çerçeve): üstte cari/tarih, ortada ürün grid'i, altında ödeme satırı (Nakit · Kredi Kartı · … küçük düğmeler), sağ altta toplamlar, solda dikey büyük düğme sütunu | § 3'teki fiş ekranı |
| Görsel dil | Windows Forms, mavi/gri, gradyan zeminler, 2010 estetiği | Mor (#5b2fd6 civarı) marka, beyaz kartlar, açık gri zemin, mor çerçeveli düğmeler, 12 px yoğun grid |
| Öğrenme | 36 video, medyan 74 sn | Tek 10:51'lik demo |

**Karar için soru (kurucu müşteriye sorar):** "Gördüğün Pusula karo karo büyük düğmeli olan mı, mor menülü olan mı?" — cevap S-2'nin biçimini etkiler (karo ızgarası ↔ fiş + düğme ızgarası; ikisi de planda var, ağırlık değişir).

---

## 2. PusulaX Ana Ekran (hero-dashboard.png) — birebir envanter

Üst şerit: **HAS · USD · EUR · CHF · GBP** (her biri alış / satış, bayrak ikonu) + "Güncelleme Saati" + zil + Admin.
Sol menü: "Menü Ara" kutusu → **Tanımlamalar ▾ · Bilgi Girişleri ▾ · Stok Raporları ▾ · Cari Raporlar ▾ · Transfer ▾ · Fatura ▾ · Ayarlar ▾** (7 dal, **kapalı**), altta "Destek Talebi Gönder", en altta kullanıcı bloğu (PARS GENEL USER · Database · Firma ID · Versiyon).
Kutucuklar (her biri başlık + dışa aktar/büyüt ikonları):
1. **Açık Pozisyon** — Para Birimi · Borcumuz · Alacağımız · Fark · HAS (satırlar: EUR, HAS, TL, USD)
2. **Barkodlu Stoklar** — Malzeme Adı · Miktar (BİLEZİK, ÇEYREK ZİYNET, ATA ALTIN, 14 HURDASI, 24 HURDASI, 24 GRAM ALTIN, 22 HURDASI, 22 GRAM ALTIN …)
3. **Barkodsuz Stoklar** — Ayar · Miktar · İşçilik (14 K, 22 K, 21 K …)
4. **Kasa** — Para Birimi · Bakiye (TL 1.400 · USD −10.300)
5. **Banka Bakiyeleri** — banka listesi + Grand Total
6. Tarih süzgeci: Today · Last 7 Days · **Last 30 Days** · Last 90 Days · This Year · Set Filter
7. **Satış Tablosu** — iki pasta (Miktar / Adet, kategoriye göre)
8. **Toptancı Satışları** — pasta
Alt: "Son güncelleme zamanı" + Yenile.

**Ders:** patronun sabah baktığı tek sayı **Açık Pozisyon**; rapor değil kutucuk. Bizde karşılığı yok (planda D7).

---

## 3. PusulaX Satış ekranı (sales-screen.png) — birebir envanter

```
[Ana Ekran] [Cari Hesap İşlemleri ×]                       ← sekmeli belge
┌ Hesap Adı [PEŞİN MÜŞTERİ ▾]  Son Durum   [Fişler] ┐ [Satış İşlemi] ┌ Risk Durumu ┐ ┌ Döviz | Borç | Alacak ┐ 🖨
│ Tarih [04.04.2025] Para Birimi [TL ▾] [İşlem Listesi] │ [Cari İşlem ] │  4.51  −64% │ │ TL   | 19.600,00 |     │
└──────────────────────────────────────────────────────┘ (mor/gri geçiş) └─(kırmızı)──┘ └───────────────────────┘
┌ Ürün Kodu | Ayar | Miktar | Adet | Tutar | Döviz | Ödenecek Tutar | Güncelle | Sil ┐  ┌ İşlem Adı [▾]  İşlem Tipi [GİRİŞLER|ÇIKIŞLAR] ┐
│ ▸ ÇIKIŞLAR          4.31                              22,500.00                     │  │ [AÇIK HESAP]  [BARKODLU SATIŞ] [MAHSUP]       │
│   KLY0010129  14K   2.30         12,878.02 TL         12,500.00   ✎  🗑             │  │ [NAKİT ÖDEME] [NAKİT TAHSİLAT] [BARKODSUZ SATIŞ]│
│   YZK0009987  14K   2.01         10,926.80 TL         10,000.00   ✎  🗑             │  │ [BARKODLU İADE][BARKODSUZ ALIŞ][BANKA]         │
│ ▸ GİRİŞLER          2.55    1                         22,500.00                     │  │ [GİDEN HAVALE] [AÇIKLAMA]      [İSKONTO]       │
│   KASA                                       TL       10,000.00   ✎  🗑             │  │ [KOMİSYON]                                     │
│   AKBANK                    1                TL        5,000.00   ✎  🗑             │  └────────────────────────────────────────────────┘
│   14 HURDASI        2.55          2,156.8627 TL        5,500.00   ✎  🗑             │
│   AÇIK HESAP                                 TL        2,000.00   ✎  🗑             │
│                                                   FARK:  0.00 (kırmızı)             │
└─────────────────────────────────────────────────────────────────────────────────────┘
[ resim ] Açıklama [__________________________]            [💾 Kaydet] [🗑 Sil] [↶ Vazgeç]
```

Okunan kurallar:
- **Ödeme bacakları da fiş satırıdır**: KASA (nakit), AKBANK (kart/banka), **14 HURDASI** (eski altın — gram 2,55, birim 2.156,86 TL/g), **AÇIK HESAP** (veresiye) aynı grid'de GİRİŞLER altında. Bizim "karma ödemeye altın bacağı" (D3) birebir bu.
- ÇIKIŞLAR toplamı = GİRİŞLER toplamı → FARK 0,00 → Kaydet. Grid gruplu (▸ katlanır).
- "Satış İşlemi / Cari İşlem" iki büyük düğme = kip anahtarı (ürünlü fiş ↔ yalnız para fişi).
- Müşteri adının yanında **Risk Durumu** kartı (kırmızı: 4.51, −64 %) ve **Borç/Alacak** tablosu.
- 13 işlem düğmesi sağda, ızgara; seçili olan dolu mor (AÇIK HESAP), diğerleri mor çerçeveli beyaz.
- Ürün satırı **7 kolon**; ürün adı yok, **ürün kodu** var (barkod kültürü).
- Altta tek serbest metin: Açıklama. Üç düğme: Kaydet · Sil · Vazgeç.

---

## 4. PusulaX Üretim/Barkodlama ekranı (production-screen.png)

Grup Adı [YÜZÜK ▾] · Alt Grup [BEŞTAŞ ▾] · Stok Kartı [PIRLANTA ▾]. Üç grid alt alta: **Maden Bilgileri** (Stok Adı · Miktar · Ayar · Maden · Birim İşçilik · Toplam İşçilik · Döviz · Üretim Firma · Toplam · Döviz), **Taş Bilgileri** (Taş Kodu · Miktar · Birim · Adet · Fiyat · Döviz · Cins · Renk · Saflık · Kesim · Toplam), **Ürün Hareketleri**. Sağ panel: Sade Gram · Şimşi Gram · Rayon · Model · Boy · Barkod Kodu; **Tutar Bilgileri**: Toplam Maliyet → Satış Fiyatı (çarpan 1,40) → Etiket Fiyatı (çarpan 2) — ürün fotoğrafı yanında. Düğmeler: Kaydet · Sil · Vazgeç · Liste · Üretimi Tamamla.
**Ders:** bizde kapsam dışı (imalat). Alınacak tek şey: **çarpanla fiyat** (maliyet × k) ve ürün fotoğrafının fiyatın yanında durması.

---

## 5. PusulaApp (mobil) — görsel dil bambaşka

- Başlangıç: "Merhaba, Roberto 👋", "Tekrar hoş geldiniz", kullanıcı seçici, **Favoriler** listesi (Hesap Tanımlama · Fiyat Okuma · Barkod Stokları · Bakiye Listesi — her biri kart + ok), altta 3 ikonlu sekme çubuğu. Açık/koyu iki tema.
- Fiyat Okuma: barkod kutusu → **büyük fiyat (754.50)** + para birimi + % → yeşil "Ürün Stokta" rozeti → ürün fotoğrafı → stok satırları → Toplam Fiyat → Temizle.
- Hesap Tanımlama: Müşteri/Reyon seçici, katlanır bölümler (Kişisel Bilgiler · Adres Bilgileri · Fatura Bilgileri), tek Kaydet.
- Palet: **çam yeşili + soluk yeşil zemin**, büyük yuvarlatılmış kartlar, tek birincil düğme. Masaüstünün moruyla ilgisi yok — Pusula'nın kendi görsel dili tutarsız; "Roberto" yer tutucu adları mockup olduğunu gösteriyor (21 Eylül raporuyla tutarlı).
**Ders (bizim için asıl olan):** mobil ekranın "tek iş, büyük sayı, tek düğme, katlanır bölümler" dili **tam olarak ilkokul/lise mezunu tezgâhtar için** olan dil. Masaüstü yüzümüzde de bunu isteriz.

---

## 6. Pusulax Demo (10:51) — kare şeridinden okunan akış

0:00 marka → 0:10 **giriş ekranı**: sol küçük form (kullanıcı/parola), sağ büyük pazarlama görseli ("PusulaX APP ile İşiniz Artık Yanınızda"; başka karelerde "Tüm İşlemler Tek Ekranda", "Güvenli Altyapı · Esnek Kurulum", "Pusula RFID") → 0:20 boş Ana Ekran → 0:30–1:30 Ana Ekran pastalarla → 1:40–3:00 **Tanımlamalar**: üç sütunlu kart formlar (aynı kalıp her tanımda) + liste grid'leri + küçük seçim penceresi → 3:30–5:00 **Cari Hesap İşlemleri** fişi (§ 3), sağ üstte Satış/Cari kip düğmeleri → 5:00 **fiş çıktısı önizleme** (dar termal fiş ve A4 iki biçim) → 5:30–7:30 **Ürün Barkodlama** (ürün fotoğrafı ile) → 7:30–8:30 **Fiyat Sorgulama** (ürün fotoğrafı + büyük TL/USD rakamları: 22.978,00 / 600,00) → 8:30–9:30 raporlar (print-preview tarzı, sekmeli) → 9:30 Ana Ekran'da açılır pencere (ayar) → 10:00 giriş ekranı varyantları (kapanış).
**Ders:** her ekran **aynı kabuk + aynı üç düğme (Kaydet/Sil/Vazgeç)**; kayıt → çıktı önizleme refleksi; fiyat sorgulamada fotoğraf + büyük rakam.

---

## 7. Klasik "Satış İşlemi Genel" (3:53, 2016) — altyazıdan okunan senaryo

8 s "cari işlemler ana menüsü altında satış işlemleri" → 40 s "herhangi bir ürün seçiyorum, alyans 4 gramını" → 75 s "tutar bu" → 115 s "ziynet, kredi kartı, iskonto…" → 160 s "325 lira yazdım, şimdi görüyorsunuz 920 TL" → 210 s "675'ini kredi kartından çekiyorum".
Yani anlatılan satış: **ürün seç → tutar otomatik → iskonto yaz → kalan tutarı kart+nakit böl → kaydet**; 3 dk 53 sn'de karma ödemeli satış anlatılıyor. (Kare şeridinde: tek pencere, ürün grid'i üstte, ödeme satırı altta, toplamlar sağ altta.)

---

## 8. Kanal envanteri (23 Eylül) — 21 Eylül listesine ek

36 video (21 Eylül raporunda 27 perakende dersi listelenmişti). Yeni görülenler: **"Pusulax Demo" 10:51 (7 ay önce, 1,4 B izlenme)** ve **"Kredi kartı ve havale ile tahsilat yapma" 2:01 (3 yıl önce, 2 B)**. Kanal 644 abone. Klasik satış videosunun altındaki tek yorum (7 ay önce) "çantada sayım yapınca gramlar tutmuyor" — sayım/mutabakat sorunu, Pusula müşterisinde de var.

---

## 9. Pusula'dan ALINACAKLAR / ALINMAYACAKLAR (mühür için)

**Alınacak (kablo/yüz işi, yeni modül değil):**
1. Fişte ödeme bacaklarının satır olması (nakit · kart · **eski altın** · açık hesap) + FARK 0,00 kuralı
2. PEŞİN MÜŞTERİ ön seçimi + müşteri yanında bakiye/risk rozeti
3. İşlem düğmeleri ızgarası (13'e karşı bizde ≤ 12, basit modda 6–8)
4. Kapalı başlayan menü dalları + "Menü Ara"
5. Kaydet → çıktı önizleme refleksi
6. Açık Pozisyon kutucuğu (D7)
7. Fiyat sorgulama: fotoğraf + büyük rakam (ürün kartında zaten fotoğraf var, `UrunGorselGalerisi`)
8. Her ekranda aynı üç düğme sırası (Kaydet · Sil/Pasife al · Vazgeç) — bizde § 5 düğme şartnamesiyle
9. Mobil dilin (tek iş, büyük sayı, katlanır bölüm) masaüstü basit moda taşınması

**Alınmayacak:**
- Sekmeli belge alanı (MDI) — karmaşıklık kaynağı; bizde çekmece + tek sayfa
- 12 px yoğun grid ve mor gradyan — hedef kitle 40+ ve okunaklılık (Ö2 16 px)
- Pasta grafik enflasyonu — Panel'de en fazla 1 grafik, gerisi sayı
- Üretim ekranı — kapsam dışı
- USB dongle / tek seferlik lisans modeli — ticari model farklı
- Kendi tutarsız görsel dili (mor masaüstü ↔ yeşil mobil) — bizde tek belirteç seti

---

*Bu kartın güncellenmesi: müşteri hangi Pusula'yı gördü (§ 1 sorusu) cevabı gelince § 1'e; kurucu Pusula'yı 10 dk izletebilirse tık sayıları § 3'e eklenir.*
