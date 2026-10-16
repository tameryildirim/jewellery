# Nasıl bakarsın — 10 dakikalık tur

> **Önce şunu bil:** 30 ekran var ama **günlük kullanılan 7 tanesi**. Gerisi ayda ya da yılda bir açılır. Programı büyük gösteren şey ekran sayısı değil, kaç tanesini her gün açtığın.

---

## 1. Adres ve giriş

| | |
|---|---|
| **Adres** | http://localhost:5199 — **şu an açık** (kontrol ettim: 200 döndü) |
| **Hangi hâli?** | 16 Eylül 22:42 yayını = **dalga 2b** sonu. Şu an koşan dalga 2c'nin yenilikleri bu yayında **yok**; 2c bitince yayını yenileyip haber vereceğim. |
| **Kullanıcı** | `qa-test-3@bygatech.test` |
| **Parola** | Ekrana basmıyorum (kural). Dosyada duruyor: `scratchpad/qa-fikstur.json` → `sifre` alanı. |
| **Hangi dükkân?** | QA fikstür dükkânı (test verisi). Kendi demo verine dokunmaz. |

---

## 2. İlk hamle: Basit moda al (30 saniye)

Sol menünün altından **Ayarlar → Arayüz modu → Basit**. Menü şu 7 satıra iner:

**Panel · Satış (İşlemler) · Müşteriler · Ürünler · Etiketler · Kasa · Raporlar**

İstediğin an Profesyonel'e dönersin; hiçbir veri değişmez, yalnız menü kısalır.

---

## 3. 10 dakikalık tur — sırayla tıkla

| Dakika | Nereye | Ne yap | Ne göreceksin |
|---|---|---|---|
| 0-1 | **Panel** | — | Gün özeti, kasa durumu, has pozisyonu |
| 1-3 | **Satış (İşlemler)** | Barkod/ürün seç → ödemeyi **karma** yap | Kasa **tek kez** soruluyor — senin ekran görüntüsüyle bildirdiğin hata burada düzeldi |
| 3-4 | **Ürünler** | Bir ürünün **Stok** düğmesi | Şube stokları penceresi açılıyor (dün her açılışta çöküyordu) |
| 4-5 | **Fiyatlandırma** (profesyonel modda) | Kayıtlı listeyi aç, marjı değiştir | Kaydedilmiş liste ve kampanya artık **düzenlenebiliyor** |
| 5-7 | **Müşteriler** | Yeni cari aç, sonra **Düzenle** | İki form artık aynı alanları istiyor; hata mesajları Türkçe |
| 7-8 | **Kasa** | Gün kapanışı | Kapanış özeti + iptal onayı ekranda |
| 8-10 | **Raporlar** | Kâr/zarar, sonra Vade takibi | "TL'de kâr, HAS'ta zarar" uyarısı — rakiplerde olmayan şey |

---

## 4. Göreceğin kusurlar — biliyoruz, ölçtük

QA turu 2'de (27 sayfa) ölçülen hâl:

- **11 kritik bulgu**, 149 bulgu toplam.
- **30 yerde** "kaydettiğin veriyi ekrandan düzeltemiyorsun".
- Bazı formlar kaydedince sessiz kalıyor; bazı ekranlarda ikon yerine emoji var.
- **Telefonda açarsan taşar** — dar ekran kabuğu henüz yayında değil.

Şu an koşan **dalga 2c tam olarak bu dört işi** kapatıyor. Yani bugün gördüğün eksikler zaten sıradaki işin içeriği.

---

## 5. "Kullanmayı bilmiyorum" — bunun için ne yapıyoruz

1. **Bugün, hemen:** Basit mod (yukarıdaki adım 2) → 7 ekran.
2. **Kılavuz ekranı artık dolu** (eskiden bomboştu): sol menünün altında **Kılavuz**.
3. **Dalga 3'te:** ilk girişte "ne tür dükkân?" sihirbazı (UX-3-B) — menü kendiliğinden sadeleşecek; ayrıca kuruluş sihirbazı (T-2) seni adım adım kuracak.

Üçü de zaten planda. Bunun için yeni iş açmıyorum.

---

## 6. Ekran haritası — hangisi ne sıklıkta açılır

| Sıklık | Ekranlar |
|---|---|
| **Her gün (7)** | Panel · Satış/İşlemler · Müşteriler · Ürünler · Etiketler · Kasa · Raporlar |
| **Haftada bir (9)** | Faturalar · Emanet · Siparişler · Sayım · Banka · Fiyatlandırma · Bildirimler · Kur · Vade takibi |
| **Ayda/yılda bir (14)** | Uyum (MASAK) · Gider pusulaları · Denetim izi · Kâr-zarar · Kullanıcılar · Yetkiler · İşletme · Ayarlar · Ürün ağacı · Veri dışa aktarım · Kılavuz · Giriş · sipariş detayı · belge ekranı |

---

## 7. Kapsam kararı — talimatın işlendi

"Artık başka iş almayalım" dedin:

- Dün çıkardığım **§ B listesindeki 13 yeni madde** (sadakat, personel primi, vitrin panosu, fiyat kilitleme, ölü stok raporu…) **oya sunulmadı, donduruldu**.
- Mühürlü kuyruk dışında **hiçbir yeni paket açılmayacak**.
- Kalan yol: **dalga 2c (şu an koşuyor)** → kapı → QA turu 4 → dalga 3 → dalga 4 → dalga 5 → 1.1.
