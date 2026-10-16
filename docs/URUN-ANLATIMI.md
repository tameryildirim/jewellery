# ÜRÜN ANLATIMI — dükkânın 8 acı noktası, ürünün karşılığı

<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->

> **TASLAK — ilan edilmemiştir.** Bu metin satış materyalinin **taslağıdır**;
> hiçbir kanalda rakam, fiyat, tasarruf ya da "silme oranı" iddiası içermez.
> Rakamlı fiyat ilanı **K-16 kurucu imzasına** bağlıdır. Kapsam dışı kalemler tek
> kaynaktan okunur: **`docs/HARIC-LISTESI.md`**.
>
> **Paket:** P-29 (S-1) · **Kimlik:** D-8 · **Tarih:** 2026-09-23

---

## 0. Anlatım kuralı (Anayasa md.1)

Aşağıdaki her satırın sağında **bugün depoda duran** bir karşılık vardır. Karşılığı
olmayan acı noktası bu listeye **yazılmaz**; "yakında" denmez. Acı noktaları
`PAZAR-ARASTIRMASI-2026-09-16.md` § "perakende kuyumcunun günlük acı noktaları"
taramasından (Hasfiyat'ın beş maddesi, Ariyen blogu, kuyumcuprogrami.com müşteri
yorumları, rakip özellik sayfaları) alınmıştır.

---

## 1. Sekiz acı nokta → karşılığı

### 1) "Dükkânda değilken ne olduğunu bilmiyorum"
Sektörün kendi dilinde ilk dert bu (*Hasfiyat: "dükkânın dışındayken kontrol
eksikliği"*). Rakiplerin yeni açtığı cephe de burası.
**Bizde:** ürün tarayıcıdan çalışır; **Panel** günün satışını, kasa ve has
bakiyesini tek ekranda verir.
→ `Kuyumcu.Web/Pages/Panel.razor`

### 2) "Kasa tutmuyor"
Müşteri yorumlarında en çok geçen dert **kasa-bakiye uyuşmazlığı**.
**Bizde:** kasa hareketleri deftere **tek yoldan** yazılır (`HareketFabrikasi`),
kasa ekranında bakiye ve hareket dökümü vardır, **gün sonu kapanışı** sayım farkını
gerekçesiz geçirmez.
→ `Kuyumcu.Api/Data/HareketFabrikasi.cs` · `Pages/Kasalar.razor` · `POST /api/v1/kasalar/{id}/gun-kapanisi`

### 3) "Milyem/has hesabı kafa karıştırıyor"
Sarrafiye hesapları sektörün "hata kabul etmeyen" işi.
**Bizde:** her işlemin **iki bacağı** tutulur — TL ve **has gram**. Canlı koşumda
10,5 g 22 ayar ürün satışı `hasGrami = −9,618` olarak deftere düştü; ayar→milyem
dönüşümü tek kaynaktan gelir.
→ `Kuyumcu.Core/Domain/Ayarlar.cs` · canlı kanıt: `docs/HIZLI-BASLANGIC.md` § 1

### 4) "Veresiye defteri ve emanet takibi elden kaçıyor"
*Hasfiyat: "hata riski taşıyan veresiye ve emanet takibi."*
**Bizde:** vade takibi (geciken / yaklaşan / vadesiz kırılımı, kredi limiti,
FIFO açık borç kapatma) ve emanet ekranı vardır.
→ `Modules/Vade/VadeKapisi.cs` · `Pages/VadeTakibi.razor` · `Modules/Emanet/EmanetModulu.cs` · `Pages/Emanet.razor`
*(Emanet **parça kimliği** 1.1'dedir — `HARIC-LISTESI.md` H-7.)*

### 5) "Stokta ne var, gerçekten bilmiyorum"
**Bizde:** sayım **oturumu** vardır — sayım başlar, fark çıkarsa `SAYIM_DUZELTME`
hareketi deftere yazılır; fark sessizce yutulmaz.
→ `Modules/Sayim/SayimModulu.cs` · `Pages/Sayim.razor`
*(RFID okuyucu **yoktur** — `HARIC-LISTESI.md` H-6.)*

### 6) "Kâr mı ettim, zarar mı — ay sonunda anlıyorum"
Rakip standardı hareketli ağırlıklı ortalama; tarama hiçbir rakipte **"TL'de kâr
ama has'ta zarar"** ayrımı bulamadı.
**Bizde:** maliyet motoru FIFO / LIFO / ağırlıklı ortalama yöntemlerini tutar,
kâr-zarar **iki bacaklı** hesaplanır ve **maliyeti bilinmeyen parti sıfır
sayılmaz** (güvenilirlik göstergesiyle işaretlenir).
→ `Modules/Maliyet/MaliyetMotoru.cs:106-110,349` · `Modules/Maliyet/KarZararServisi.cs:18-19` · `Pages/KarZarar.razor`

### 7) "Kayıt dışılıktan gerçek usule geçtim; belge ve denetim yükü arttı"
1/1/2026'dan itibaren büyükşehirlerde emtia alım-satımı yapanlar **gerçek usule**
geçti (10380 sayılı Karar, RG 09.09.2025; GVK-100/2025-1 sirküleri).
**Bizde:** fatura durum makinesi (TASLAK→KESİLDİ→ÖDENDİ/İPTAL), **kuyum matrahı**
KDV ayrımı, tevsik haddi kapısı (canlı koşumda 30.000 TL üstü nakit işlem **400**
ile durdu), MASAK/kimlik tespiti ekranı ve **denetim izi** vardır.
→ `Modules/Fatura/FaturaModulu.cs:16-18` · `Modules/Tevsik/` · `Modules/Masak/MasakModulu.cs` · `Pages/Uyum.razor` · `Pages/DenetimIzi.razor`
*(GİB'e **elektronik iletim** yoktur — `HARIC-LISTESI.md` H-1. Mevzuat yorumu mali müşavire aittir.)*

### 8) "Yanlış girdim, düzeltemiyorum"
Rakip sayfalarında "işlem geçmişi düzenleme / ters ibraz" ayrı bir satış maddesi.
**Bizde:** finansal kayıt **silinmez**; geri alma **tek motordan** (storno) geçer,
düzeltme fişi ve pasife alma yolları ekranda gösterilir. Düzeltilemeyen kaydın
**neden** düzeltilemediği ve doğru yolu ekranda yazılıdır.
→ `Modules/Satis/StornoServisi.cs` · `Modules/Satis/StornoSuzgeci.cs` · `Modules/Meta/DuzeltmePolitikasi.cs`

---

## 2. Konumlandırma (R-8 — bağlayıcı)

**Tek rakamlı "silme / tasarruf oranı" hiçbir kanalda ilan edilmez.** Sebebi
F-8 birim ekonomi belgesinde yazılıdır ve dürüsttür:

- **Dezy**'nin 500 ₺/ay tabanına karşı **fiyat üstünlüğümüz YOKTUR** — orada
  tasarruf iddia edilemez.
- **M&L** için dolaşan **%85** iddiası doğrulanamadı; aynı varsayımlarla
  aritmetik **≈%54** veriyor. **%85 kullanılmaz.**
- Rakam bazlı her kıyas, F-8'de **2 satır `OLCULMEDI`** olduğu sürece
  **FAIL-CLOSED 1** gereği kilitlidir.

**Bunun yerine konumlandırma toplam sahip olma maliyetine dayanır** (hepsi F-8'de
kanıtlı): kullanıcı başına ücret yok · modül başına ücret yok · kurulum ücreti yok ·
yazılım lisans maliyeti 0 TL (izinli açık kaynak bağımlılıklar) · **KDV durumu açık
yazılır** (rakiplerin çoğu belirtmiyor).

---

## 3. Ne söylemiyoruz

- Fiyat, indirim, tasarruf oranı, geri ödeme süresi — **yok** (K-16).
- Çalışma süresi (uptime), destek yanıt süresi, kapasite — **yok** (`docs/SLA.md` § 3).
- e-Fatura / ÖKC / SMS / banka API / imalat / RFID — **yok**
  (`docs/HARIC-LISTESI.md`; ikinci bir cümleyle burada tekrar tarif edilmez).
- "X dakikada ilk satış" — **yok**; ölçüm henüz bir koşumda (`docs/HIZLI-BASLANGIC.md` § 0).
