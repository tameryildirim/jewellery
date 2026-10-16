# MASAK Entegrasyonu — Araştırma Raporu ve Yol Haritası

**Tarih:** 2026-09-11 · **İsteyen:** Kurucu (*"MASAK'ı araştır, test sunucusunu bul, orada test edelim, gerçek ortamını da oluştur"*)
**Yöntem:** birincil kaynaklar — MASAK'ın kendi kılavuzları (PDF, tam metin), Tedbirler Yönetmeliği (mevzuat.gov.tr), portalın kendisi (HTTP). Rakip blogları yalnız yön göstermek için kullanıldı, kaynak sayılmadı.
**Kod değişikliği:** yalnız §6'daki düzeltme. Commit yok.

---

## 0. KISA CEVAP

| İstek | Cevap |
|---|---|
| Test sunucusu bul | **Yok.** MASAK herkese açık test/sandbox ortamı sunmuyor. Kılavuzlarda "test", "deneme" 0 kez geçiyor; `test.onlineislemler.masak.gov.tr` gibi adlar çözülmüyor. |
| API / web servis | **Yok.** Portal WAF arkasında tek sayfalık uygulama; `/api`, `/swagger`, `/xsd` 200 dönüyor ama hepsi WAF'ın "Güvenlik Merkezi" HTML'i — gerçek uç değil. |
| Entegrasyon mümkün mü | **Evet, tek yolla: ŞİB XML dosyası.** Bildirim XML + ekler `.zip` olarak portala **elle yüklenir**, portal doğrular, uyum görevlisi onaylar, gönderir. |
| "Test" nerede yapılır | Portalın **"ŞİB XML Doğrulama"** ekranı — göndermeden yalnız doğrular. Fiilen test ortamı budur. **Ama kayıtlı hesap gerektirir.** |
| Gerçek ortamı kur | **Kod değil, kayıt işi.** Yükümlü kaydı + Taahhüt Formu + noter belgeleri + MASAK'ın hesap açması. Kurucu/pilot kuyumcu işi, ben yapamam. |
| XML şeması (XSD) | **Herkese açık yayınlanmıyor.** Portal içinden ya da MASAK'tan alınacak. **Şema olmadan XML üretici yazılamaz** — uydurmam. |

---

## 1. MASAK ONLINE — ne var, ne yok (ölçüldü)

- Adres: **https://onlineislemler.masak.gov.tr** · giriş: e-Devlet ya da TCKN + şifre + SMS doğrulama.
- Kılavuzlar: *MASAK Online Başvuru Kılavuzu* (2025/03, 340 satır) · *ŞİB Kullanım Kılavuzu* (2025/05, 269 satır) — ikisi de indirildi, tam metin tarandı.
- **API/web servis/entegrasyon** kelimeleri: iki kılavuzda **0**. "XML": kullanım kılavuzunda **14** — hepsi dosya yükleme bağlamında.
- Portal yoklaması: robots.txt bile WAF sayfası döndürüyor (title: *"Güvenlik Merkezi - WAF Bilgilendirme"*). Bilinen hiçbir yolda şema veya swagger yok.

### Bildirim akışı (kullanım kılavuzundan, aynen)
1. **Yeni ŞİB** — formu elle doldur, **ya da**
2. **ŞİB XML Doğrulama** — en fazla 10 XML, yalnız doğrular; hata varsa *"Dosya Yükleme Hata Mesajını Göster"*; temizse *"XML Doğrulama hatası yoktur."*
3. **ŞİB XML Doğrulama ve Yükleme** — XML + ekler (ekstre, Excel, Word, pdf, jpeg) `.zip`, **toplam ≤ 250 MB**, sefer başına ≤ 10 zip → doğrulanır, **evrak kayıt referans numarası** alır, PDF görüntülenir → *"Bildirimi Kaydet ve Onaya Gönder"*.
4. **Bekleyen bildirimler** — taslak; **72 saat içinde onaya gönderilmezse sistem otomatik SİLER.**
5. **Uyum Görevlisi İşlem Hareketleri** — UG *"Taslağa Çek"* / *"Onayla"*. Onay sonrası değiştirilemez.
6. **Gönderilecekler → ŞİB GÖNDER** — nihai PDF iner.
7. **Gönderilen bildirimler** — yalnız evrak no, tarih, giren kullanıcı görünür; **detaya MASAK güvenlik politikası gereği erişilemez.**
8. **Saklama:** 5549 md.8 — **8 yıl**, ibraz edilmezse adli ceza (md.14).

---

## 2. KAYIT — gerçek ortam nasıl açılır (Kurucu / pilot kuyumcu işi)

Başvuru kılavuzundan:
1. Yükümlü ya da **kanuni temsilci** e-Devlet ile `onlineislemler.masak.gov.tr/login` → 2 aşamalı doğrulama (e-Devlet mobil onay ya da SMS).
2. **"Şüpheli İşlem Bildirimi Taahhüt Formu"** girişi — yükümlü bilgileri, uyum görevlisi + yardımcısı.
3. Belgeler: kanuni temsilciye ait **noter onaylı yetki belgesi** + **noter onaylı imza sirküleri** (pdf/png/jpg/tif, belge başına ≤ 50 MB).
4. Kaydet → taahhüt formu otomatik iner → MASAK inceler → uyum görevlisi ve yardımcısı için hesap açar (aktivasyon e-posta/SMS). Beyan Takip ekranından kalan süre izlenir.
5. Yetkili kullanıcı kaydını **yalnız MASAK açar**; kapatmak için resmî yazı gerekir.

**Kuyumcular yükümlü listesinde:** *"Kıymetli Maden, Taş veya Mücevher Alım Satımı Yapanlar ile Bu İşlemlere Aracılık Edenler."* Bir kuyumcu tek başına yükümlüdür; **yazılım firması yükümlü değildir** — hesap kuyumcunun (uyum görevlisinin) adına açılır, XML'i o yükler.

---

## 3. FORMUN İSTEDİĞİ — Kuyumcu ŞİB Rehberi 2.0 (MSK-RHB-ŞİB-008-2.0, 16.06.2025'ten zorunlu)

| Bölüm | Alanlar | Bizde |
|---|---|---|
| (A) Yükümlü | ad/unvan, TCKN/VKN, temsilci ad-soyad + TCKN | Tenant'ta **yok** (VKN/temsilci eklenmeli) |
| (B) Bildirim | **yükümlü referans no** (tekrarsız), yeni/ek + önceki referans, **kategori** (aklama / terörizmin finansmanı [+örgüt] / KİS), **aciliyet** (Normal / İvedi / Çok İvedi / İşlemin Ertelenmesi Talepli) | referans = `Id` var; kategori, aciliyet, ek-bağı **yok** |
| (C) Gerçek kişi | **bağlantı türü** (kendisi adına / başkası adına / adına işlem yapılan / gerçek faydalanıcı / dolaylı), **kimliği sahte mi**, ad, soyad, **belge türleri** (nüfus cüzdanı, TC kimlik kartı seri/geçerlilik, sürücü, yeni tip sürücü, pasaport, ikamet izni, diğer — birden çoksa hepsi), TCKN 11 hane, doğum, uyruk, adres… | `MasakKimlikTespiti` var; bağlantı türü, sahte bayrağı, çoklu belge, seri/geçerlilik **yok** |
| (D) Tüzel kişi | unvan, VKN, MERSİS… | **yok** |
| (E) İşlem | **durum** (teşebbüs/gerçekleşti), **çeşit** (tek/çoklu), tarih(ler), para birimi, tutar, **TP tutar** (TCMB gişe alış), **yer: ülke/il/ilçe** (şube adı yetmez), işlem açıklaması | hareket bağı var; yer, durum, çeşit, TP çevrimi **yok** |
| (F) Şüphe kategorisi | **SK-01…SK-41**'den **1–3** adet; TF için SK-23/SK-35, KİS için SK-36 (kaynak: SK-Kodlari.pdf) | **yok** |
| (G) Açıklama | **asgari 300, azami 50.000 karakter**; ekte değil doğrudan; **özgün metin** — otomatik yazılımla şablon tekrarı yasak; kişiler/tutarlar serbest metinde değil yapısal alanlarda | 300/50.000 **düzeltildi** (§6) |
| Tipoloji | T-008-1.x müşteri profili (16), **T-008-2.x kuyumcu (31+)** — örn. 2.5 bölme, 2.7 kimliksiz işlem, 2.20 yalnız nakit yüksek mücevher, 2.26 hızlı geri satış, 2.28 kripto ödeme, **2.29 yüksek hurda/çalıntı şüphesi** —, 3.x terör/riskli ülke, 5.x KİS | **yok** (referans tablo olarak eklenmeli) |

**Sonuç:** veri modelimiz MASAK formunun **yaklaşık üçte birini** karşılıyor. XML üretilmeden önce model genişletilmeli — ama **şema (XSD) gelmeden alan adları kesinleşmez.**

---

## 4. BİRİNCİL KAYNAK DOĞRULAMASI — eşiklerimiz

| Kuralımız | Kaynak | Sonuç |
|---|---|---|
| 185.000 TL kimlik | Tedbirler Yönetmeliği **md.5(1)(b)**: *"yüzseksenbeşbin TL … veya üzerinde"* — 6702 s. CK (RG 14.1.2023/32073), yürürlük **1.2.2023**. Son değişiklik 9305 s. CK (25.12.2024) kripto istisnası ekledi. | ✅ **DOĞRULANDI** |
| Şüpheli işlem 10 iş günü | **md.27(2)**: *"şüphenin oluştuğu tarihten itibaren en geç on iş günü"* | ✅ **DOĞRULANDI** |
| 8 yıl saklama | md.46 + 5549 md.8; kullanım kılavuzu da aynen | ✅ **DOĞRULANDI** |
| Bildirim açıklaması | Rehber 2.0 §4.2.7: **300 / 50.000** | ✅ düzeltildi (biz 20/2.000 yazmıştık) |
| **200.000 / 2.000.000 / 20.000.000 kademeleri, "20 karakter"** | Yönetmelikte **YOK** (arandı: 0). Kaynak: **Ağustos 2025 tarihli MASAK Genel Tebliği TASLAĞI** ("Sıra No 30" diye anılıyor; ama RG 9.9.2025/33012'de yayımlanan gerçek Tebliğ 30 **uyum görevlisi sınav/lisans** tebliği — konu farklı). Taslak **bankalar, ödeme ve elektronik para kuruluşlarına** yönelik; **kuyumcu geçmiyor.** | ⚠️ **DOĞRULANAMADI** — kuyumcuyu bağladığı kanıtsız; kaynağımız SarrafPro'nun bankacılık kuralını kuyumcuya uyarlamasıydı |

İkinci bağımsız kaynak (PKF İstanbul, Ağustos 2025): taslak **"bankacılık kanalıyla yapılan transferler"** ve finansal kuruluşlar için; kuyumcu/kıymetli maden **geçmiyor**; görüş süresi 18.8.2025; **RG yayımı bu araştırmada doğrulanamadı** (en son kaynaklar taslak diyor, bugün 2026-09-11).

**Bu ne demek:** AMAC_BEYANI / BEYAN_FORMU / EK_BELGE kademelerimiz yasal zorunluluk **olmayabilir.** Eksik uygulamak ceza riski, fazla uygulamak yalnız sürtünme — yani mevcut hâli **güvenli taraf**. Ama "yasa böyle istiyor" diye **satılamaz**. Karar K2'ye (mali müşavir). Eşikler zaten yapılandırmadan okunuyor; kapatmak için kod değişmez.

---

## 5. YOL HARİTASI — ne yapılabilir, ne yapılamaz

### Şimdi yapılabilir (kod, MASAK hesabı gerekmez)
1. **Veri modelini forma göre genişlet** (§3 boşlukları): (B) kategori/aciliyet/ek-bağı, (C) bağlantı türü/sahte/çoklu belge, (E) yer/durum/çeşit/TP tutar, (F) SK kodları, tipoloji referans tablosu, Tenant'a VKN + temsilci.
2. **ŞİB "veri paketi" ekranı**: uyum görevlisinin portala **elle** gireceği tüm alanları eksiksiz, kopyalanabilir biçimde hazırlayan ekran + **rehber formatında PDF**. XML gelmeden bile MASAK'a bildirim süresini kısaltır — bugün rakiplerin "otomatik" dediği şeyin en az bir kısmı muhtemelen budur (doğrulanmadı).
3. **72 saat / 10 iş günü uyarı motoru** — taslak silinme ve yasal süre için panel uyarıları (10 iş günü zaten var; resmî tatil listesi eklenmeli).

### XSD gelince yapılabilir
4. **ŞİB XML üretici** — şemaya birebir; ekleri `.zip`'e koyan paketleyici; yükümlünün referans numarasını `MasakSupheliBildirim.Id`'den türeten eşleme.
5. Portalın **XML Doğrulama** ekranında **test** (kayıtlı hesapla, göndermeden).

### Yapılamaz (bugün)
- Otomatik gönderim: API yok. Portal WAF + e-Devlet + SMS + UG onayı — makine erişimi tasarım gereği kapalı. **"MASAK'a otomatik gönderir" iddiası kurulamaz**; rakip bunu iddia ediyorsa K3'te sorulmalı.

---

## 6. BU ARAŞTIRMANIN DOĞURDUĞU KOD DÜZELTMESİ

`MasakSupheliBildirim.Gerekce` 2.000 karakter sınırıyla ve DTO'da 20 asgari ile yazılmıştı. **Rehber 2.0 §4.2.7: asgari 300, azami 50.000.** Düzeltildi: sütun `text`, DTO 300/50.000, servis katmanında da zorlanıyor (trim sonrası; boşlukla şişirme geçmez). Migration `MasakGerekceText` uygulandı. Yeni test `Supheli_300KarakterAltiAciklama_Reddedilir`. **252/252 yeşil.**

---

## 7. KURUCU'YA

| # | İş | Neden |
|---|---|---|
| **K2-a** | Mali müşavire: 200.000/2M/20M kademeleri kuyumcuyu bağlıyor mu? Taslak tebliğ yayımlandı mı? | Kademelerimizin yasal dayanağı yok (§4) |
| **K2-b** | Resmî tatil takvimi kaynağı | 10 iş günü hesabı bayramları iş günü sayıyor |
| **K5 (yeni)** | Pilot kuyumcunun MASAK Online kaydı: e-Devlet, Taahhüt Formu, noter belgeleri, uyum görevlisi ataması | Test ve gerçek ortam **bunsuz açılmaz** |
| **K6 (yeni)** | MASAK'tan **ŞİB XML şeması (XSD)** talebi — portal içinden ya da resmî yazıyla | XML üretici bunsuz yazılmaz |
| K3 | SarrafPro demosu: "otomatik bildirim" gerçekten ne yapıyor? | §5 — API yokken "otomatik" nasıl? |

### Kaynaklar (birincil)
- Başvuru Kılavuzu: https://ms.hmb.gov.tr/uploads/sites/12/2025/03/Masak-Online-Basvuru-Kilavuzu-201485e62f36b80c.pdf
- Kullanım Kılavuzu: https://ms.hmb.gov.tr/uploads/sites/12/2025/05/ETAHS-SIB-Kullanim-Klavuzu-e2e98d79a11baac3.pdf
- Kuyumcu ŞİB Rehberi 2.0: https://ms.hmb.gov.tr/uploads/sites/12/2025/06/SIB-REHBER-KUYUMCULAR-2.0-19936ffcf75ce5f9.pdf
- Şüphe kategorileri: https://ms.hmb.gov.tr/uploads/sites/12/2024/10/SK-Kodlari.pdf
- Tedbirler Yönetmeliği (konsolide): https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf
- Tebliğ 30 (uyum görevlisi lisans, RG 9.9.2025/33012): https://www.alomaliye.com/2025/09/09/mali-suclari-arastirma-kurulu-genel-tebligi-sira-no-30/
- Taslak tebliğ (ikincil, kapsam teyidi): https://pkf.com.tr/masak-genel-tebligi-taslagi-yayimlandi-para-transferinde-beyan-zorunlulugu/
- Portal: https://onlineislemler.masak.gov.tr · Sektörel rehberler/taahhüt formu: https://masak.hmb.gov.tr/sektorel-sib-rehberleri
