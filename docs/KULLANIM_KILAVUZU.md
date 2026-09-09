# Kuyumcu ERP — Kullanım Kılavuzu

Kuyumcu ERP; kuyumcu işletmeleri için satış, alış, stok, cari hesap, kasa, banka,
fatura ve raporlama süreçlerini tek çatı altında toplayan bir yönetim sistemidir.
Web (masaüstü tarayıcı) ve mobil (iOS/Android) uygulamalarıyla kullanılır.

---

## İçindekiler

1. [Temel Kavramlar](#1-temel-kavramlar)
2. [Kurulum ve Başlatma](#2-kurulum-ve-başlatma)
3. [Giriş ve İşletme Kurulumu](#3-giriş-ve-i̇şletme-kurulumu)
4. [Panel (Ana Ekran)](#4-panel-ana-ekran)
5. [Cariler](#5-cariler)
6. [Ürünler](#6-ürünler)
7. [İşlemler (Satış / Alış / Hurda / Takas / İşçilik)](#7-i̇şlemler)
8. [Kasalar](#8-kasalar)
9. [Banka](#9-banka)
10. [Faturalar](#10-faturalar)
11. [Raporlar](#11-raporlar)
12. [Ayarlar](#12-ayarlar)
13. [Mobil Uygulama](#13-mobil-uygulama)
14. [Kullanıcı Rolleri ve Yetkiler](#14-kullanıcı-rolleri-ve-yetkiler)
15. [Sık Sorulan Sorular](#15-sık-sorulan-sorular)

---

## 1. Temel Kavramlar

| Kavram | Açıklama |
|---|---|
| **Has gram** | Ürünün 24 ayar saf altın karşılığı. `(gram ağırlık − taş ağırlık) × ayar milyemi` formülüyle otomatik hesaplanır. Örn. 10 g 22 ayar bilezik ≈ 9,16 has gram. |
| **Paralel defter** | Her işlem hem TL hem has gram olarak eş zamanlı kaydedilir. Altın fiyatı değişse de has gram sabittir; işletmenin gerçek altın varlığını gösterir. |
| **Cari** | Müşteri ve/veya tedarikçi hesabı. TL bakiye ve has gram bakiye ayrı ayrı izlenir. |
| **Ayar** | Altın: K8, K14, K18, K21, K22, K24 — Gümüş: G800, G925, G950 |
| **Veresiye** | Cari bakiyesinin artıya geçmesi: müşteri size borçlu. Tahsilat girilince bakiye düşer. |
| **Emanet** | Müşterinin size bıraktığı altının has gram cinsinden takibi. |

---

## 2. Kurulum ve Başlatma

### Gereksinimler
- Windows 10/11, Node.js 20+, pnpm, Docker Desktop (MySQL için)

### Başlatma
1. Proje klasöründeki **`start.bat`** dosyasına çift tıklayın.
2. Sırasıyla: bağımlılıklar kurulur → MySQL konteyneri başlar → API (port 4000) ve Web (port 3000) pencereleri açılır.
3. Tarayıcı otomatik olarak **http://localhost:3000** adresini açar.

### Durdurma
- **`stop.bat`** dosyasına çift tıklayın. Web ve API kapanır.
  (MySQL konteyneri veri güvenliği için açık kalır; kapatmak isterseniz `docker compose stop mysql`.)

---

## 3. Giriş ve İşletme Kurulumu

### İlk kullanım — İşletme Kur
1. Giriş ekranında **"İşletme Kur"** sekmesine tıklayın.
2. Alanları doldurun:
   - **İşletme Adı** — mağazanızın adı (örn. "Altın Dünyası Kuyumculuk")
   - **Ad / Soyad** — işletme sahibinin adı
   - **E-posta / Şifre** — giriş bilgileriniz (şifre en az 8 karakter)
3. **"İşletme Kur"** düğmesine basın.
4. Sistem otomatik olarak işletmenizi, **Merkez Şube**'yi ve **SAHIP** yetkili
   hesabınızı oluşturur ve sizi panele yönlendirir.

### Sonraki girişler
1. **"Giriş Yap"** sekmesinde e-posta ve şifrenizi girin.
2. Oturumunuz tarayıcıda saklanır; sayfayı kapatıp açsanız da devam eder.
3. Sağ üstteki **🇹🇷 TR / 🇬🇧 EN** düğmeleriyle dili değiştirebilirsiniz.

---

## 4. Panel (Ana Ekran)

Giriş yaptığınızda karşınıza çıkan özet ekranıdır.

| Bölüm | İçerik |
|---|---|
| **Günlük Satış / Günlük Alış** | Bugünkü toplam satış ve alış tutarları (TL) |
| **Net Has Gram** | Bugünkü işlemlerin net has gram etkisi (paralel defter) |
| **Net Kasa** | Bugünkü net nakit akışı |
| **Son 7 Günlük Satış** | Haftalık satış grafiği |
| **Bugün Özet** | Satış, alış, işçilik ve has gram dökümü |
| **Üst çubuk (Header)** | Canlı 24K altın kuru — her dakika güncellenir |

> Sol menüden tüm modüllere erişilir. En altta adınız, rolünüz, Ayarlar ve Çıkış bulunur.

---

## 5. Cariler

Müşteri ve tedarikçi hesaplarınızı yönettiğiniz ekran.

### Yeni cari ekleme
1. Sol menüden **Cariler**'e girin.
2. Sağ üstteki **"+ Yeni Cari"** düğmesine tıklayın.
3. Formu doldurun:
   - **Ad / Firma Adı** (zorunlu)
   - **Tür**: Müşteri / Tedarikçi / Her İkisi
   - Telefon, e-posta, adres (isteğe bağlı)
4. **Kaydet**'e basın.

### Cari arama ve filtreleme
- Üstteki arama kutusuna **ad, telefon veya e-posta** yazın; liste anında süzülür.
- Sağındaki açılır menüden türe göre filtreleyin (Müşteri / Tedarikçi).

### Cari detayı ve hareketler
1. Listede bir carinin satırındaki **"Detay"** bağlantısına tıklayın.
2. Açılan pencerede görürsünüz:
   - **TL Bakiye** — pozitif: size borçlu (veresiye), negatif: siz borçlusunuz
   - **Has Gram** — altın cinsinden bakiye (emanet dahil)
   - **Hareketler** — tarih sıralı işlem geçmişi (satış, tahsilat, veresiye...)

---

## 6. Ürünler

Envanterinizi ürün bazında yönettiğiniz ekran.

### Yeni ürün ekleme
1. Sol menüden **Ürünler**'e girin, **"+ Yeni Ürün"** düğmesine tıklayın.
2. Formu doldurun:
   - **Ürün Adı** (zorunlu) ve **Barkod** (isteğe bağlı)
   - **Kategori**: Bilezik, Kolye, Yüzük, Küpe, Saat, Altın Para, Gümüş Eşya, Diğer
   - **Ayar**: K8–K24 (altın) veya G800–G950 (gümüş)
   - **Gram Ağırlık** ve **Taş Ağırlık**
3. **Kaydet**'e basın. **Has gram otomatik hesaplanır** — elle girilmez.

> Ürünün gram ağırlığı veya ayarı sonradan değiştirilirse tüm şube stoklarının
> has gram toplamları otomatik yeniden hesaplanır.

### Stok görüntüleme
1. Ürün satırındaki **"Stok"** bağlantısına tıklayın.
2. Şube bazında adet ve toplam has gram listelenir.

### Arama ve filtre
- Arama kutusuna ürün adı veya barkod yazın.
- Ayar filtresinden belirli bir ayarı seçin (örn. yalnızca K22).

---

## 7. İşlemler

Günlük ticari işlemlerin tamamı bu ekrandan yapılır. Üstteki düğmelerle işlem
türünü seçin: **Satış · Alış · Hurda Alış · Takas · İşçilik**

### 7.1 Satış
1. **Satış** sekmesini seçin.
2. **Cari** seçin (müşteri).
3. **Ürün** seçin, **adet** ve **birim fiyat** girin, **"Ekle"** deyin.
   Birden fazla ürünü aynı fişe ekleyebilirsiniz; alt tabloda toplam görünür.
4. İsterseniz açıklama yazın, **"Satış Kaydet"** düğmesine basın.

Sistem otomatik olarak: stoktan düşer → cari bakiyesini günceller → kasaya işler
→ paralel deftere hem TL hem has gram yazar.

### 7.2 Alış
Satışla aynı adımlar; yönü terstir. Stok artar, kasadan çıkış olur.
Cari olarak genellikle **tedarikçi** seçilir.

### 7.3 Hurda Alış
1. **Hurda Alış** sekmesini seçin, cari seçin.
2. **Gram ağırlık** girin ve **ayar** seçin (örn. 15 g, K14).
3. Kaydedin — has gram karşılığı otomatik hesaplanır ve stok altın varlığınıza eklenir.

### 7.4 Takas
Müşterinin eski ürünü verip yenisini alması. Eski ve yeni ürün bilgileri girilir;
aradaki fark (**farkTl**) pozitifse müşteri öder, negatifse siz iade edersiniz.

### 7.5 İşçilik
Tamir, ölçü, montaj gibi hizmet gelirleri. Cari seçin, tutarı girin, kaydedin.

> Her başarılı işlemden sonra yeşil **"İşlem başarıyla kaydedildi!"** bandı görünür.

---

## 8. Kasalar

Nakit ve POS hesaplarınızı izlediğiniz ekran.

### Yeni kasa
1. Sol menüden **Kasalar**'a girin, **"+ Yeni Kasa"** deyin.
2. Kasa adı ve döviz türü (TRY/USD/EUR/GBP) seçip kaydedin.

### Kasa hareketi girme
1. Kasa kartındaki **"+ Hareket"** düğmesine tıklayın.
2. Hareket türünü seçin: **GELIR, GIDER, TAHSILAT, ODEME**
3. Tutar ve açıklama girip kaydedin. Kasa bakiyesi anında güncellenir.

### Geçmiş
- Kasa kartındaki **"Geçmiş"** düğmesi son hareketleri tarih sıralı gösterir.

> Üstte üç özet kart bulunur: Toplam TL Bakiye, Kasa Sayısı, Aktif Kasa.
> Bakiyesi sıfır olmayan kasa silinemez (deaktif edilemez).

---

## 9. Banka

Banka hesaplarınızı (IBAN bazlı) ve dekontlarınızı yönettiğiniz ekran.
**Gelen ödemeleri tek tıkla faturaya dönüştürebilirsiniz.**

### Banka hesabı ekleme
1. Sol menüden **Banka**'ya girin, **"+ Yeni Hesap"** düğmesine tıklayın.
2. Doldurun:
   - **Banka Adı** (örn. Ziraat Bankası)
   - **Hesap Adı** (örn. Ana Hesap)
   - **IBAN** — TR ile başlayan 26 haneli numara (otomatik büyük harfe çevrilir)
   - Döviz türü
3. Kaydedin. Hesap üstte bir sekme olarak görünür; birden fazla hesabınız varsa
   sekmeler arasında geçiş yapabilirsiniz.

### Dekont (hareket) girme
1. İlgili hesabın sekmesi seçiliyken **"Dekont Gir"** düğmesine tıklayın.
2. Doldurun:
   - **Yön**: Gelen (para size geldi) / Giden (siz ödeme yaptınız)
   - **Tutar**
   - **Karşı Taraf** — gönderen/alıcı adı (isteğe bağlı)
   - **Açıklama** — dekont açıklaması (isteğe bağlı)
3. Kaydedin. Hesap bakiyesi otomatik güncellenir, hareket **"Bekliyor"**
   durumuyla listeye düşer.

### 💡 Gelen parayı faturaya dönüştürme
1. Hareket listesinde **Gelen** ve henüz faturalanmamış satırın sağındaki
   **"Faturaya Dönüştür"** bağlantısına tıklayın.
2. Açılan pencerede:
   - Hareketin tutarı ve karşı tarafı özet kutusunda görünür.
   - **Cari seçin** — fatura bu cariye kesilir.
   - **KDV Oranı seçin** — %0, %1, %10 veya %20.
     Tutar **KDV dahil** kabul edilir; ara toplam ve KDV tutarı önizlemede anlık görünür.
     (Örn. 12.000 TL, %20 KDV → ara toplam 10.000 TL + KDV 2.000 TL)
3. **"Faturaya Dönüştür"** düğmesine basın.
4. Sistem otomatik olarak:
   - Sıralı numaralı fatura keser (örn. **FTR-2026-000001**), durumu **Kesildi** olur.
   - Banka hareketini **"Faturalandı"** durumuna alır ve faturaya bağlar.
   - Aynı hareket **ikinci kez faturalandırılamaz**.

Kesilen faturayı **Faturalar** ekranında görebilirsiniz.

---

## 10. Faturalar

Tüm faturalarınızı listelediğiniz ve yönettiğiniz ekran.

### Liste ve filtre
- Üstteki açılır menüden duruma göre süzün: **Taslak / Kesildi / Ödendi / İptal**
- **Kaynak** sütunu faturanın nereden geldiğini gösterir:
  - 🏦 **Banka hareketinden** — Banka ekranında dönüştürülen
  - 📄 **Manuel** — elle oluşturulan

### Manuel fatura oluşturma
1. **"+ Yeni Fatura"** düğmesine tıklayın.
2. Cari, tür (Satış/Alış), KDV oranı ve **KDV dahil toplam** tutarı girin.
   Ara toplam ve KDV önizlemesi otomatik hesaplanır.
3. Kaydedin — fatura **Taslak** durumuyla oluşur.

### Fatura durum akışı
```
Taslak ──"Kes"──► Kesildi ──"Ödendi İşaretle"──► Ödendi
   │                  │
   └── "İptal Et" ────┘        (İptal edilen fatura geri alınamaz)
```
Satırdaki bağlantılarla durum değiştirilir:
- **Kes** — taslağı resmileştirir
- **Ödendi İşaretle** — tahsilatı onaylar
- **İptal Et** — faturayı iptal eder

> Banka hareketinden dönüştürülen faturalar doğrudan **Kesildi** durumuyla açılır
> (para zaten geldiği için).

---

## 11. Raporlar

İşletmenizin fotoğrafını çektiğiniz ekran.

### Dönem seçimi
- Üstteki yıl ve ay menüleriyle rapor dönemini değiştirin.

### Bölümler
| Rapor | İçerik |
|---|---|
| **Aylık kartlar** | Aylık Satış, Aylık Alış, Kâr (satış − alış), Net Has Gram |
| **Günlük Satış/Alış grafiği** | Ay içi gün gün karşılaştırma (çubuk grafik) |
| **Stok Dağılımı** | Ayara göre pasta grafik (örn. %60 K22, %30 K14...) |
| **Cari Yaşlandırma** | Alacaklarınız yaş dilimlerine göre: 0-30 / 31-60 / 61-90 / 90+ gün. 90+ sütunundaki tutarlar tahsilat riski taşır. |
| **Stok Değeri** | Ürün bazında toplam adet ve toplam has gram |

---

## 12. Ayarlar

Sol menünün altındaki **Ayarlar** bağlantısından ulaşılır.

### Dil
- 🇹🇷 Türkçe / 🇬🇧 English kartlarından birine tıklayın; arayüz anında değişir.
- Seçim tarayıcıda saklanır; bir daha açtığınızda hatırlanır.
- Ayrıca sol menü üstündeki **TR/EN** düğmeleri her ekrandan hızlı geçiş sağlar.

### Profil bilgileri
- Ad soyad, e-posta ve rolünüz görüntülenir.

### Şifre değiştirme
1. **Mevcut Şifre**'nizi girin.
2. **Yeni Şifre**'yi iki kez girin (eşleşmezse uyarı çıkar).
3. **"Şifreyi Güncelle"** düğmesine basın; yeşil onay bandı görünür.

---

## 13. Mobil Uygulama

Mobil uygulama sahada ve tezgahta hızlı işlem için tasarlanmıştır.
Alt sekme çubuğuyla gezinilir: **📊 Panel · 👥 Cariler · 💰 İşlem · 🏦 Kasalar · 📈 Raporlar**

### Giriş
- Web ile aynı e-posta/şifre kullanılır. "İşletme Kur" da mobilden yapılabilir.
- Sağ üstteki TR/EN düğmesiyle dil değiştirilir.

### Panel
- Hoş geldin başlığı, **canlı 24K altın alış/satış kuru** (koyu kart),
  bugünün özeti (satış, alış, has gram, net kasa) ve hızlı erişim kısayolları.
- Aşağı çekerek yenileyebilirsiniz (pull-to-refresh).

### Cariler
1. Arama kutusuna ad/telefon yazarak cari bulun.
2. Kartta TL bakiye ve has gram görünür; karta dokununca detay ve son hareketler açılır.
3. **"+ Ekle"** ile yeni cari: ad, telefon, tür (Müşteri/Tedarikçi/Her İkisi) seçin.

### İşlem
1. Üstten işlem türü seçin: 💰 Satış · 🛒 Alış · ♻️ Hurda Alış · 🔨 İşçilik
2. **Cari Ara** kutusuna en az 2 harf yazın, listeden seçin.
3. Satış/Alış: ürünü arayıp seçin → adet + birim fiyat → **Ekle**.
   Kalemler ve toplam altta listelenir.
4. Hurda: gram + ayar düğmeleri. İşçilik: tutar.
5. En alttaki **"... Kaydet"** düğmesine basın; yeşil onay bandı görünür.

### Kasalar
- Kasa kartlarında bakiyeler görünür; **"+ Hareket Gir"** ile tür/tutar/açıklama girilir.
- Üstte toplam TL bakiye rozeti bulunur.

### Raporlar
- **Bugün** bölümü: günlük kartlar + işçilik/hurda dökümü.
- **Aylık Rapor**: ‹ › oklarıyla ay değiştirin; satış, alış, net kâr, has gram
  ve kâr marjı çubuğu görünür.

> **Not:** Banka ve Faturalar modülleri şimdilik yalnızca web uygulamasındadır.

---

## 14. Kullanıcı Rolleri ve Yetkiler

| Rol | Yetkiler |
|---|---|
| **SAHIP** | Tüm yetkiler: kullanıcı yönetimi, kasa/cari silme, plan yönetimi |
| **MUDUR** | Kasa/banka hesabı açma, ürün ve cari yönetimi, tüm işlemler |
| **MUHASEBE** | Kasa/banka hareketi girme, fatura kesme, tahsilat |
| **TEZGAHTAR** | Satış/alış işlemleri, cari ve ürün görüntüleme |

Yetkiniz olmayan bir işlem denediğinizde sistem "erişim reddedildi" hatası döner.

---

## 15. Sık Sorulan Sorular

**S: Altın kuru nereden geliyor?**
C: Sistem dış kaynaktan otomatik çeker ve her dakika günceller. Kur çekilemezse
son bilinen değer gösterilir; manuel kur girişi de yapılabilir.

**S: Yanlış işlem girdim, nasıl düzeltirim?**
C: Hareket kayıtları değiştirilemez (denetim izi). Ters yönde düzeltme işlemi
girin — örn. yanlış satışı aynı tutarlı alışla dengeleyin.

**S: Gelen havaleyi faturaya dönüştürdüm ama yanlış cari seçmişim?**
C: Faturayı **İptal Et** ile iptal edin ve Faturalar ekranından doğru cariyle
manuel fatura kesin. (İptal edilen faturanın numarası tekrar kullanılmaz.)

**S: Aynı e-postayla iki işletmem var, nasıl giriş yaparım?**
C: Giriş isteğinde tenantId göndermeniz gerekir; aksi halde sistem hangi
işletmeye gireceğinizi soran bir hata mesajı döner.

**S: Verilerim nerede saklanıyor?**
C: Yerel kurulumda Docker MySQL konteynerindeki kalıcı bir disk alanında
(`kuyumcu_mysql_data`). `stop.bat` MySQL'i kapatmaz; veriler her zaman korunur.

**S: KDV oranını sonradan değiştirebilir miyim?**
C: Kesilen faturada değiştirilemez. Faturayı iptal edip doğru oranla yeniden kesin.

---

*Bu kılavuz Kuyumcu ERP v0.1 içindir. Banka canlı entegrasyonu (açık bankacılık)
ve e-Fatura entegrasyonu yol haritasındadır; şu an dekontlar manuel girilir.*
