# Risk Kaydı

| | |
|---|---|
| **Belge kimliği** | KYM-RK-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **PMBOK Guide 8. baskı** (ANSI/PMI 99-001-2025) — Risk performance domain |

---

## 0. Değerlendirme ölçeği

**Olasılık:** Yüksek (bekleniyor) · Orta (olabilir) · Düşük (beklenmiyor ama mümkün)
**Etki:** Kritik (proje durur veya ürün satılamaz) · Yüksek (önemli gecikme/maliyet) · Orta · Düşük

Olasılık değerleri **tahmindir** ve öyle işaretlenmiştir. Etki değerleri ölçülebilir sonuçlara dayanır.

---

## 1. Aktif riskler

### R-01 🔴 e-Fatura entegratör sözleşmesi yapılmazsa
**Olasılık:** Orta · **Etki:** Kritik
e-Fatura mükellefi olan her müşteri segmenti kapsam dışı kalır. Satış değerlendirmesinde *"listedeki en büyük ticari risk"* olarak işaretlendi.
**Karşılık:** `IEFaturaSaglayici` soyutlaması hazır; eşik yapılandırmadan okunacak biçimde tasarlandı. Mock ile "tamamlandı" denmeyecek ([ADR-0012](../02-mimari/adr/0012-efatura-sahte-entegrasyonla-yapilmaz.md)).
**Sahibi:** Kurucu (K1) · **Durum:** Açık

### R-02 🔴 Sürüm kontrolündeki sırlar ifşa olursa
**Olasılık:** Düşük-orta · **Etki:** Kritik
JWT sırrı, veritabanı parolası ve **alan şifreleme anahtarı** ilk commit'te. Anahtarın ifşası diskteki AES-256-GCM korumasını **tamamen** geçersiz kılar.
**Karşılık:** Rotasyon + git geçmişi temizliği. `sifre_surumu` alanı anahtar rotasyonu için mevcut.
**Sahibi:** Kurucu · **Durum:** Açık — **en öncelikli**

### R-03 🔴 Pilot doğrulaması olmadan yanlış ürün geliştirmek
**Olasılık:** Orta · **Etki:** Yüksek
StRS'deki **8 paydaş gereksinimi** hiçbir gerçek kuyumcuyla doğrulanmadı. Kaynakları: devralınan kodun kapsamı, rakip özellik listeleri, **iki** şikâyet kaydı.
**Karşılık:** Doğrulanmamış gereksinimler belgede açıkça işaretli; mevzuat tarafı birincil kaynaklarla doğrulanmış durumda (yanlış olma riski düşük).
**Sahibi:** Kurucu (K4) · **Durum:** Açık

### R-04 🟠 Mali müşavir teyidi gelmezse
**Olasılık:** Düşük · **Etki:** Yüksek
Üç soru açık: fatura/e-fatura haddi (dört çelişen rakam), hurda stopaj oranları (**para hareketini etkiler**), MASAK beyan kademelerinin kuyumcuyu bağlayıp bağlamadığı.
**Karşılık:** Hiçbiri tahminle kodlanmadı; eşikler yapılandırmadan okunuyor.
**Sahibi:** Kurucu (K2) · **Durum:** Açık

### R-05 🟠 Bus factor 1
**Olasılık:** Düşük · **Etki:** Yüksek
`git shortlog` tek katkı veren gösteriyor.
**Karşılık:** 12 ADR, kod içi gerekçe yorumları, bu belge seti. **Kalan boşluk: operasyonel bilgi (runbook).**
**Sahibi:** ByGatech · **Durum:** Kısmen azaltıldı

### R-06 🟠 Rakiplerin uyum tarafında hızla kapatması
**Olasılık:** **Yüksek** · **Etki:** Yüksek
MASAK artık farklılaşma değil **giriş bileti**. Altı rakip açıkça satıyor; biri *"Kimlik girilmeden işlem kapatılamaz"* diyor.
**Karşılık:** Farklılaşma dar ve kanıtlanabilir alanlara kaydırıldı: 10 iş günü ŞİB sayacı, 8 yıl muhafaza yönetimi, gider pusulası + **stopaj** ("tevkifat" 18 firmanın hiçbirinde geçmiyor), sentetik taş etiketleme.
⚠ Rakip iddiaları **yayımlanmış pazarlama metnidir**; hiçbir ürünün içi görülmedi.
**Sahibi:** Satış · **Durum:** Açık

### R-07 🟠 Ürün olgunluğu satış için yetersiz
**Olasılık:** **Yüksek** · **Etki:** Yüksek
Barkod/RFID, toptan, imalat, personel/prim, mobil yok. Toptancı, imalatçı ve e-Fatura mükellefi segmentleri **bugün kapsam dışı**.
**Karşılık:** Hedef segment daraltıldı: tek dükkân ve 2-3 şubeli perakende kuyumcu (19.905 firmalık havuzun çoğunluğu).
**Sahibi:** Kurucu + Satış · **Durum:** Açık

### R-08 🟠 CI/CD yokluğunda kalite kapısının atlanması
**Olasılık:** Orta · **Etki:** Orta
Kapılar elle çalıştırılıyor.
**Karşılık:** Kapılar hazır ve tekrarlanabilir (`denetim.sh`, `dotnet test`, `yetim-tara.sh`, `yedek-al-ve-dogrula.sh`, lisans kapısı). Bağlamak küçük bir iş.
**Sahibi:** ByGatech · **Durum:** Açık

### R-09 🟡 FK eklenmesinin regresyon üretmesi
**Olasılık:** Orta · **Etki:** Orta
EF'te ilişki metadatası olmadığından ekleme sırası garantisiz; bugün sessizce yetim üreten bir yol FK'dan sonra `errno 1452` ile patlayacaktır.
**Karşılık:** Aşamalı (4 dalga); Dalga 0 taraması tamamlandı; her dalgada tam test koşumu + mutasyon kanıtı; tek tablolu spike önce.
**Sahibi:** Mimari · **Durum:** Karara bağlandı ([ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md))

### R-10 🟡 Kur sağlayıcısı kesintisi
**Olasılık:** Orta · **Etki:** Orta
Dört kaynak tanımlı, **devretme davranışı ölçülmedi**.
**Sahibi:** ByGatech · **Durum:** Açık

### R-11 🟡 Ölçek davranışının bilinmemesi
**Olasılık:** Orta · **Etki:** Orta
10× yükte ilk kırılan nokta bilinmiyor. Tek MySQL örneği, replika yok. `hareketler.kur_snapshot_id` indekssiz — 1M satırda gece işini kilitler.
**Karşılık:** FK Dalga 3 indeksi bedavaya getirir; k6 ile sentetik yük testi planlandı.
**Sahibi:** Mimari · **Durum:** Açık

### R-12 🟡 KVKK aydınlatma metninin olmaması
**Olasılık:** Yüksek (pilot başlarsa) · **Etki:** Orta
Mekanizma hazır, **metin yok**.
**Sahibi:** Kurucu (hukuk danışmanı) · **Durum:** Açık

### R-13 🟡 MASAK'a gerçek gönderimin doğrulanamaması
**Olasılık:** Yüksek · **Etki:** Orta
MASAK'ın **API'si ve test sunucusu yok**; XSD kamuya açık değil. Sistem bildirimi hazırlar ve süresini takip eder, **gönderim elle** yapılır.
**Karşılık:** Bu bir ürün kusuru değil, dış kısıttır ve müşteriye açıkça söylenir.
**Sahibi:** Kurucu (K5, K6) · **Durum:** Açık

---

## 2. Gerçekleşmiş riskler (ders çıkarılanlar)

| # | Risk | Nasıl gerçekleşti | Alınan yapısal önlem |
|---|---|---|---|
| G-01 | Kiracı sızıntısı | Faz 0'da kanıtlandı: başka kiracının kasasına 99.999 TL yazıldı, HTTP 200 | İki katmanlı izolasyon + mutasyon kanıtı. **Kalıntı veri bugün hâlâ `kuyumcu_net`'te** ve tarayıcı tarafından bulundu |
| G-02 | Yeşil testin yanıltması | **4 hata** ~300 yeşil testten geçti, canlıda çıktı | HTTP boru hattı testleri + özdeşlik testleri + "canlı doğrulama zorunlu" kuralı |
| G-03 | Blogun birincil kaynak sanılması | KMTS yanlışlıkla "olmazsa olmaz" sayıldı | Birincil kaynak zorunluluğu; blog kaynak sayılmaz |
| G-04 | Yanlış ölçüm yöntemi | WASM boyutu **Debug** derlemesinden ölçüldü (7,53 MB); gerçek rakam **2,42 MB** | Ölçüm yöntemi belgeye yazıldı; yaygın hatalar listelendi |
| G-05 | Testin kendi altyapısını bozması | İkinci fikstür `EnsureDeleted` ile **132 testi** düşürdü | Şema kurulumu süreç başına bir kez |
| G-06 | Kendi eklediğimiz bağımlılığın zafiyet getirmesi | bUnit 1.34.0 → `AngleSharp 1.1.2` (mXSS) | Aynı gün tarandı ve kapatıldı; tarama kalıcı kapı oldu |
| G-07 | Doğrulanmayan alan | `KimlikTespitiId` kiracı doğrulaması olmadan yazılıyordu | Kapatıldı + mutasyon kanıtı; **gövdeden gelen her `*_id` doğrulanır** kuralı |
| G-08 | Yapılandırmanın koddan sapması | nginx `X-Forwarded-For` gönderiyor, uygulama okumuyordu | `UseForwardedHeaders` + güvenilir vekil listesi + 7 bekçi testi |

---

## 3. Risk dağılımı

| Etki | Açık risk |
|---|---|
| 🔴 Kritik | 3 (R-01, R-02, R-03) |
| 🟠 Yüksek | 5 (R-04…R-08) |
| 🟡 Orta | 5 (R-09…R-13) |

**Üç kritik riskin ikisi doğrudan Kurucu kalemidir** (sözleşme, sır rotasyonu, pilot erişimi). Bu, projenin teknik değil **ticari ve hukuki** tarafta tıkandığını gösterir.
