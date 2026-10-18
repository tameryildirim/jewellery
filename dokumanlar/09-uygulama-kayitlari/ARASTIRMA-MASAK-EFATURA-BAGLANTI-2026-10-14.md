# MASAK ve e-Fatura bağlantısı — araştırma

**Tarih:** 14–15 Ekim 2026 (15.10 düzeltme: yaptırım kontrolü kodda var) · **Hazırlayan:** Tamer Bey (araştırma: üç paralel ajan) · **Kapsam:** yalnız okuma. Kod değiştirilmedi, çalışan sunuculara dokunulmadı.
**Kaynak kuralı:** Mevzuat yalnız birincil kaynakla (Resmî Gazete, mevzuat.gov.tr, gib.gov.tr, ebelge.gib.gov.tr, masak.hmb.gov.tr ve MASAK'ın resmî PDF'leri). Rakip siteleri yalnız § 6'da **"rakip beyanı"** olarak geçer, mevzuat kaynağı sayılmaz.

| İşaret | Anlamı |
|---|---|
| ✅ | Birincil metin okundu, doğrulandı |
| 🟡 TEYİTSİZ | Kısmen bulundu ya da yoruma dayanıyor; teyit gerekiyor |
| ❌ | Birincil kaynakta bulunamadı |

---

## 1. Bir sayfalık özet

### MASAK

- **Yazılımın MASAK'a bağlanma yolu yok.** Kuyumcu için tek yol, MASAK Online 2.0'daki web formu. Kılavuzlarda kuyumcuya açık bir XML, XSD, web servis ya da toplu yükleme yok. ✅
- **Giriş e-Devlet ile yapılıyor ve iki aşamalı doğrulama zorunlu** (e-Devlet mobil onay ya da SMS kodu). Bir "kurum şifresi" yok; giriş kişinin kendi e-Devlet hesabıyla yapılıyor. ✅
- **Şifreyi başkasına vermek yönetmelikle yasak.** Bildirimin yapıldığını üçüncü kişiye açıklamak hapis cezası gerektiren bir suç. ✅
- **Sonuç: "Şifreyi AES-256 ile saklayalım" önerisi yapılamaz ve yapılmamalı** (gerekçesi § 3'te). Bugünkü yöntemimiz doğru: bildirimi hazırla, süreyi say, kuyumcu kendisi yüklesin, referansı sakla.
- **Rakipler de bildirimi otomatik göndermiyor.** Pusula'nın ortağı Kivi bunu kendi sitesinde açıkça yazıyor (§ 6).
- **Kuyumcunun uyum görevlisi atama, uyum programı kurma ve iç denetim yükümlülüğü yok.** Bunlar yalnız bankalar ve büyük kuruluşlar için geçerli. ✅
- **Yeni yükümlülük:** 23.09.2026 tarihli 34 Sıra No'lu Genel Tebliğ'e göre BM Güvenlik Konseyi listelerindeki kişilerle yapılan işlem hemen bildirilecek ve işlem ertelenecek. Kuyumcu da bu kapsamda. ✅ **Programda bu kontrol var** (`Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs`, Tebliğ 34'e atıflı; eşleşen kişiyle işlem durur). Ancak liste ByGatech tarafından elle yükleniyor ve demo veritabanında boş.
- **200.000 TL "işlem amacı beyanı" kademesi birincil kaynakta bulunamadı.** 2.000.000 / 20.000.000 TL kademeleri de bulunamadı. ❌ Programda hâlâ 200.000 TL'den itibaren kimlik isteniyor.

### e-Fatura / e-Arşiv

- **En önemli bulgu:** 1 Ocak 2026'dan bu yana e-Fatura'ya kayıtlı olmayan mükellefler de faturalarını **tutarına bakılmaksızın e-Arşiv olarak** düzenlemek zorunda. Basit usul ve işletme hesabı esasına göre defter tutanlar için tarih 1 Ocak 2027. ✅ (metin) / 🟡 (kuyumcuya uygulanışı mali müşavirle teyit edilmeli)
  - **Okumamız doğruysa, bilanço esasına göre defter tutan bir kuyumcunun Mihenk'te kestiği kâğıt/PDF fatura bugün geçerli bir fatura değil.** Bu durumda e-Fatura bağlantısı "sonra bakarız" değil, **satış için ön koşul** olur.
- **e-Fatura'ya geçiş haddi:** yıllık satış 3 milyon TL ve üzeri; internetten satış yapan için 500 bin TL. Kuyuma özel bir geçiş hükmü yok. ✅
- **Gönderimin üç yolu var:** GİB portalı (elle), özel entegratör, doğrudan entegrasyon. Bizim gibi bir yazılım için gerçekçi yol **özel entegratör**. GİB'in listesinde ~105 firma var. ✅
- **Kuyum faturasının kendi kodları var:** fatura tipi "ÖZELMATRAH"; 805 = altın ziynet ve sikke, 808 = gümüş, 230 = külçe altın istisnası. ✅
- **e-İrsaliye** kuyumcuya yalnız yıllık satış 10 milyon TL ve üzeriyse zorunlu. ✅
- **V-04'teki dört rakam ayrıştı:**

  | Rakam | Ne olduğu | Durum |
  |---|---|---|
  | 12.000 TL | 2026 fatura düzenleme haddi | ✅ |
  | 3.000.000 TL | e-Fatura'ya geçiş haddi | ✅ |
  | 5.000.000 TL | 2018–2020 dönemine ait eski e-Fatura haddi, artık geçersiz | ✅ |
  | 36.000 TL | birincil kaynakta yok | ❌ |

---

## 2. Maddeler ve kaynakları

### A) MASAK

| # | Bulgu | Kaynak | Durum |
|---|---|---|---|
| A1.1 | Sistem adresi `onlineislemler.masak.gov.tr` (MASAK Online 2.0). Kuyumcu ŞİB Rehberi 2.0, 16.06.2025'ten beri tek geçerli usul | [Kuyumcu ŞİB Rehberi 2.0 (MSK-RHB-ŞİB-008-2.0)](https://ms.hmb.gov.tr/uploads/sites/12/2025/06/SIB-REHBER-KUYUMCULAR-2.0-19936ffcf75ce5f9.pdf) §1.4, §3.1 · masak.hmb.gov.tr duyurusu 13.06.2025 | ✅ |
| A1.2 | Giriş e-Devlet ile; *"2 aşamalı doğrulamanın e-Devlet üzerinden aktif hale getirilmiş olması gerekmektedir"*. Kullanıcı adı/şifre ya da e-imza ile giriş kılavuzda geçmiyor | [MASAK Online Başvuru Kılavuzu](https://ms.hmb.gov.tr/uploads/sites/12/2025/03/Masak-Online-Basvuru-Kilavuzu-201485e62f36b80c.pdf) s.2–3 | ✅ |
| A1.3 | Kullanıcı kaydını yükümlü ya da kanuni temsilcisi bir kez yapar. Gerçek kişi: faaliyet belgesi ve kimlik. Tüzel kişi: tescil belgesi, noter onaylı temsil belgesi ve imza sirküleri. Taahhüt formu sistemde oluşur. **Kayıttan sonra ŞİB ancak 72 saat geçince gönderilebilir** | Başvuru Kılavuzu s.5–10 · Kuyumcu Rehberi §3.1 | ✅ |
| A1.4 | Bir kişide olabilecek en fazla kullanıcı sayısı | — | 🟡 TEYİTSİZ |
| A1.5 | ŞİB web formu adımları: gerçek kişi → tüzel kişi → işlem → şüphe kategorisi (en az 1, en çok 3) → açıklama (**en az 300, en çok 50.000 karakter**) → ekler (her biri ≤50 MB, toplam ≤250 MB) → önizleme → "Onaya Gönder" | [ŞİB Kılavuzu](https://ms.hmb.gov.tr/uploads/sites/12/2025/03/Supheli-Islem-Bildirimi-SIB-Kilavuzu-f8f8657a492cafc4.pdf) s.2–9 | ✅ |
| A2.1 | Kuyumcu rehberinde ve kılavuzlarda XML, XSD, web servis ya da toplu yükleme yok. Kuyumcu için tek yol web formu | Yukarıdaki üç belge | ✅ (yokluk) |
| A2.2 | Bankalar için eski bir dayanak var: Genel Tebliğ Sıra No 6'da "elektronik kasa" ile bildirim. Bankacılık dışı yükümlülere açık bir arayüz bulunamadı | [masak.hmb.gov.tr — Genel Tebliğ 6](https://masak.hmb.gov.tr/masak-genel-tebligi-sira-no-6) | ❌ arayüz yok |
| A3.1 | **Tedbirler Yönetmeliği md.29/2:** uyum görevlileri sisteme erişimi sağlayan *"kart, şifre … hiç kimseye veremez"*. Gizlilik, bildirimden haberi olan diğer personeli de kapsar | [mevzuat.gov.tr — Tedbirler Yönetmeliği](https://www.mevzuat.gov.tr/mevzuat?MevzuatNo=200713012&MevzuatTur=21&MevzuatTertip=5) md.29/1-2 | ✅ |
| A3.2 | **5549 s. Kanun md.4/2:** bildirimin yapıldığı, denetim elemanları ve mahkemeler dışında *"işleme taraf olanlar dahil hiç kimseye"* açıklanamaz. **md.14/1:** aykırılığa 1–3 yıl hapis ve 5.000 güne kadar adlî para cezası | [mevzuat.gov.tr — 5549](https://www.mevzuat.gov.tr/mevzuat?MevzuatNo=5549&MevzuatTur=1&MevzuatTertip=5) md.4/2, md.14/1 | ✅ |
| A3.3 | Taahhüt formu: *"şifremi herhangi bir üçüncü kişi ile paylaşmayacağımı"* ve kullanıcı kodu/şifreyle yapılan tüm işlemlerden sorumluluk | [MSK-FRM-THT-002](https://ms.hmb.gov.tr/uploads/sites/12/2025/05/MSK-FRM-THT-002-1_0_24-2960a0e99d8f490b.pdf) (başka sektör formu; kuyumcunun 2.0 metni sistem içinde, görülemedi) | ✅ / 🟡 kuyumcu metni |
| A4.1 | Uyum programı ve uyum görevlisi **kuyumcuya zorunlu değil**. Uyum Programı Yönetmeliği md.4/1 ve md.29/1 listelerinde perakende kuyumcu yok; yalnız "kıymetli madenler aracı kuruluşları" var | [mevzuat.gov.tr — Uyum Programı Yönetmeliği](https://www.mevzuat.gov.tr/mevzuat?MevzuatNo=12426&MevzuatTur=7&MevzuatTertip=5) (RG 22.08.2025/32994 değişikliği işlenmiş) | ✅ |
| A4.2 | Kuyumcunun yükümlü olduğu madde **Tedbirler Yönetmeliği md.4/1-k**. 7 Ekim belgesinde yazan "3/1-k" yanlış | aynı yönetmelik | ✅ düzeltme |
| A4.3 | Kimlik tespiti eşiği **370.000 TL** (md.5/1-b); yayım tarihinde yürürlükte | [RG 7.10.2026 / 33393](https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf), CK 11845 md.1 | ✅ |
| A4.4 | ŞİB süresi en geç 10 iş günü; gecikmesinde sakınca olan hallerde derhal. Parasal alt sınır yok | Tedbirler md.28/2 · Genel Tebliğ 13 md.4/2, md.5/1 | ✅ |
| A4.5 | Bilgi ve belge verme (md.31); belgeler 8 yıl saklanır ve istenince ibraz edilir (md.46/1-2). Dahili bildirimler ve ŞİB belgeleri de kapsamda | Tedbirler Yönetmeliği | ✅ |
| A4.6 | Devamlı bilgi verme (md.32): kuyumcuyu kapsayan bir düzenleme bulunamadı | — | 🟡 TEYİTSİZ |
| A4.7 | **Genel Tebliğ Sıra No 34:** BM Güvenlik Konseyi listelerindeki kişilerin işlemleri için tüm yükümlüler **hemen bildirir ve işlemi erteler** | [RG 23.09.2026 / 33379](https://www.resmigazete.gov.tr/eskiler/2026/09/20260923-2.htm) | ✅ |
| A4.8 | Nakit ödeme ya da elektronik ödeme zorunluluğu MASAK mevzuatında bulunamadı (nakit sınırı VUK tevsik kuralında, V-03) | — | ❌ |
| A5.1 | **200.000 TL "işlem amacı beyanı" kademesi** Tedbirler Yönetmeliği'nde ve Tebliğ 5'in güncel metninde yok. Tek benzer hüküm md.5/3: sürekli iş ilişkisinde "iş ilişkisinin amacı ve mahiyeti" sorulur | Tedbirler md.5/3 | ❌ |
| A5.2 | 2.000.000 / 20.000.000 TL beyan kademeleri yalnız ikincil sitelerde "taslak" olarak geçiyor; Resmî Gazete'de karşılığı yok. 2026'da yayımlanan 31–34 Sıra No'lu Tebliğler bu konuyu içermiyor | — | ❌ |

> **Bir düzeltme daha:** Mevzuat gereksinimleri belgesindeki M-05 maddesi, 300 karakter kuralını "işlem amacı beyanı" diye yazıyor. Birincil kaynakta 300 karakter, **ŞİB açıklama alanının** alt sınırı (A1.5). Belgeyi kendim düzeltmedim (izinsiz değişiklik yok); düzeltme önerisi § 4'te.

### B) e-Fatura / e-Arşiv / e-İrsaliye

Ana kaynak, 509 Sıra No'lu VUK Genel Tebliği'nin güncel metni: [mevzuat.gov.tr](https://mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=33905&mevzuatTur=Teblig&mevzuatTertip=5) ve [GİB 2026 güncel hali](https://cdn.gib.gov.tr/api/gibportal-file/file/getFile?objectKey=MEVZUAT_TEBLIGLER%2FUNIVERSAL%2F2026%2FMEVZUAT_TEBLIGLER_2026_VukTeb509_Guncel.pdf). Tebliği değiştirenler: 515, 526, 535, 550, 573 ve 589 (RG 31.12.2025/33124). 2026'da yayımlanmış yeni bir değişiklik bulunamadı. Hadleri düşüren bir 2026 taslağından söz ediliyor, ancak taslak ❌ TEYİTSİZ.

| # | Bulgu | Kaynak | Durum |
|---|---|---|---|
| B1.1 | e-Fatura'ya geçiş haddi: brüt satış hasılatı 2022 ve sonrası için **3 milyon TL**. Haddi geçen, izleyen yılın 7. ayının başından itibaren geçer (2025 hasılatı haddi aşan → 1.7.2026) | 509 IV.1.4/a-1 · IV.1.5/a | ✅ |
| B1.2 | İnternetten (kendi sitesi ya da pazaryeri) satış yapanlar için had **500 bin TL**; aşılırsa e-Fatura ve e-Arşiv birlikte zorunlu | 509 IV.1.4/a-4 · IV.2.4.2 | ✅ |
| B1.3 | e-Fatura'ya kayıtlı olmayanlar: 2025'te vergiler dahil 3.000 TL üstü fatura e-Arşiv olacaktı. **1.1.2026'dan itibaren "tutarına bakılmaksızın"** e-Arşiv zorunlu. Basit usul ve işletme hesabı esasına göre defter tutanlar için tarih 1.1.2027 | 509 IV.2.4.3 (573 ve 589 md.1 ile değişik) | ✅ metin · 🟡 kuyumcuya uygulanışı |
| B1.4 | Perakende satışta, fatura haddine kadarki e-Arşiv faturada müşteri bilgisi yerine "NİHAİ TÜKETİCİ" yazılabilir. Bu imkân yalnız ÖKC muafiyeti olanlara ve Güvenli Mobil Ödeme sistemi kullananlara açık | 509 V.2 (550 ile değişik) | ✅ |
| B1.5 | Kuyuma özel bir geçiş hükmü **yok**. "Kıymetli maden" ifadesi yalnız döviz büfelerinin e-Döviz belgesinde geçiyor. GİB, yazılı bildirimle sektöre ya da cirosuna bakmadan zorunluluk getirebilir | 509 IV.10 · IV.1.4/ğ · IV.2.4.4 | ✅ |
| B2.1 | Gönderimin üç yolu: **GİB Portal** (elle; GİB, düzenlenebilecek belgeleri sınırlayabilir) · **Özel Entegratör** (GİB izinli; belgeyi mali mühürle onaylar, sistemi Türkiye'de olmak zorunda) · **Doğrudan Entegrasyon** (GİB'in belirlediği ölçek şartları, test süreci, başvurudan itibaren 1 yıl içinde tamamlama) | 509 V.1.1 · V.1.2 · V.1.3 | ✅ |
| B2.2 | Doğrudan entegrasyonun sayısal şartları (başvuru kılavuzu okunmadı) | — | 🟡 TEYİTSİZ |
| B2.3 | Özel entegratör listesi resmî olarak burada yayımlanır (çekildiği anda ~105 firma vardı) | [ebelge.gib.gov.tr — özel entegratör listesi](https://ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html) · 509 V.1.2 son fıkra | ✅ |
| B2.4 | "GİB portalı kapanıyor" iddiası için bir duyuru bulunamadı | [e-Belge duyuruları](https://ebelge.gib.gov.tr/duyurular.html) | 🟡 TEYİTSİZ |
| B3.1 | Fatura dosya biçimi **UBL-TR 1.2.1**; her faturada zorunlu **ETTN** (GUID); senaryolar TEMELFATURA, TICARIFATURA, EARSIVFATURA | [UBL-TR 1.2.1 kılavuz paketi](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/UBLTR_1.2.1_Kilavuzlar.zip) — Fatura kılavuzu §2.3.7, Kod Listeleri §2.2 | ✅ |
| B3.2 | Gönderici Birim (GB) ve Posta Kutusu (PK); fatura mali mühür ya da e-imzayla onaylanır; ticari faturaya 8 gün içinde yanıt verilir; iletim web servisiyle (HTTPS) yapılır | [e-Fatura Entegrasyon Kılavuzu v1.10](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-FaturaUygulamasiEntegrasyonKilavuzu-v1.10.pdf) §3.1–3.2 | ✅ |
| B3.3 | Kayıtlı kullanıcı listesi kılavuzu güncellendi; yeni sürüm **16.11.2026'da zorunlu** (duyuru 30.09.2026). Entegratör bunu üstlenir, ama tarih takvime yazılmalı | e-Belge duyuruları | ✅ |
| B3.4 | Mali mühür: tüzel kişi kendi mali mührünü kullanır; gerçek kişi mali mühür ya da nitelikli elektronik sertifika (NES) kullanır. Entegratör üzerinden çalışıldığında entegratörün mührü de kullanılabilir | 509 V.2 | ✅ |
| B3.5 | Yazılımın entegratöre bağlanması için gerekenler (API kullanıcı/şifre ya da anahtar, varsayılan GB/PK etiketi, test ortamı) **entegratöre göre değişiyor**. Entegratör belgesi okunmadı | — | 🟡 TEYİTSİZ (entegratör seçilince netleşir) |
| B3.6 | e-Fatura numarasının biçimi: 16 karakter = 3 harf ön ek + yıl + 9 hane sıra. **Bu turda kılavuzdan okunmadı.** Bizim numaramız `FTR-{yıl}-{sıra:6}` (`Kuyumcu.Api/Data/Entities.cs:1261`) buna uymuyor | — | 🟡 TEYİTSİZ |
| B4.1 | **Fatura düzenleme haddi 2026: 12.000 TL** (VUK md.232); yeniden değerleme oranı %25,49 | [588 Sıra No'lu VUK Genel Tebliği](https://www.resmigazete.gov.tr/eskiler/2025/12/20251231M5-31.pdf), RG 31.12.2025/33124 (5. Mük.), ekli liste md.232 | ✅ |
| B4.2 | 2025 haddi 9.900 TL (577 Sıra No'lu Tebliğ; Resmî Gazete metni açılmadı) | — | 🟡 TEYİTSİZ |
| B4.3 | 36.000 TL: 509'da ve 588 listesinin okunan sayfalarında yok. 588 listesindeki 30.000 TL, md.104 ilan haddi | — | ❌ |
| B4.4 | **Kuyum KDV'si:** ziynet eşyası ve sikke altının tesliminde *"matrah, külçe altın bedeli düşüldükten sonra"* bulunur. Külçe altın teslimi ayrıca KDV'den istisna | [mevzuat.gov.tr — 3065 KDV Kanunu](https://www.mevzuat.gov.tr/mevzuat?MevzuatNo=3065&MevzuatTur=1&MevzuatTertip=5) md.23/e · md.17/4-g | ✅ |
| B4.5 | UBL-TR'de fatura tipi **ÖZELMATRAH**; **805** = altından ziynet ve sikke altın, **808** = gümüş ziynet ve sikke gümüş; **230** = külçe altın/gümüş/kıymetli taş istisnası (17/4-g) | UBL-TR Kod Listeleri **V1.43** (27.07.2026), aynı paket | ✅ |
| B4.6 | KDV Genel Uygulama Tebliği'nin kuyum bölümü okunmadı | — | 🟡 TEYİTSİZ |
| B5.1 | e-İrsaliye: zorunlu sektörler arasında kuyum yok. Kuyumcu için geçerli kural: 2021 ve sonrası hasılatı **10 milyon TL** ve üzeri olan e-Fatura mükellefleri; izleyen yılın 7. ayında geçer | 509 IV.3.5 (bent 7) · IV.3.6 | ✅ |

---

## 3. "MASAK şifresini AES-256 ile saklasak, biz de göremesek olmaz mı?"

**Kısa cevap: Hayır. Teknik olarak da yasal olarak da olmaz.** Sebepler kuvvetliden zayıfa doğru:

1. **Saklanacak bir şifre yok.** MASAK Online'a girişte kurum şifresi kullanılmıyor. Kişi kendi **e-Devlet** hesabıyla giriyor ve **iki aşamalı doğrulama** zorunlu: telefonuna gelen SMS kodu ya da e-Devlet mobil onayı (A1.2). Şifreyi saklasak bile o anki telefon onayı olmadan giriş yapılamaz. Otomatik giriş için kuyumcunun telefonunu da ele geçirmek gerekir. Bu, MASAK'ın güvenlik önlemini aşmak demektir.
2. **Yönetmelik açıkça yasaklıyor.** md.29/2'ye göre kart ve şifre "hiç kimseye" verilemez. Kuyumcu, taahhüt formunda da şifreyi üçüncü kişiyle paylaşmayacağını ve şifreyle yapılan her işlemden kendisinin sorumlu olduğunu imzalıyor (A3.1, A3.3). Ayrıca kullanılan şifre kişinin **e-Devlet şifresi**; o şifre vergi, tapu, banka ve sağlık kayıtlarına da açılıyor. Bunu bir yazılım firmasında tutmak kabul edilemez bir risk.
3. **"Biz de göremeyiz" sözü teknik olarak tutmuyor.** Program şifreyi kullanacaksa çözmesi gerekir. Çözme anahtarı sunucuda olur; sunucuyu da biz işletiyoruz. Bugünkü alan şifrelememiz de böyle çalışıyor: anahtar `KUYUMCU_SIFRELEME_ANAHTARI` ortam değişkeninde (`Kuyumcu.Api/Common/AlanSifreleme.cs:19`). AES-256 şifreyi diskte ya da yedekte çalınmaya karşı korur, **sunucuyu işletenden korumaz**.
4. **Asıl tehlike ceza hukuku.** Program bildirimi kendisi gönderirse ByGatech, bildirimin yapıldığını bilen taraf olur. 5549 md.4/2'deki istisnalar yalnız denetim elemanları ve mahkemeler; yazılım firması ya da hizmet sağlayıcı için istisna yok. Aykırılığın cezası 1–3 yıl hapis (A3.2). *Bu sonuç birincil metne dayalı bizim yorumumuzdur; doğrudan "yazılım firması" diyen bir hüküm yok. Bkz. § 4 hukukçu sorusu.*
5. **Pazarda kimse yapmıyor.** En agresif rakip Kivi bile "hiçbir bildirim otomatik gönderilmez" diyor (§ 6).

**Doğru yol (bugünkü yöntemin geliştirilmişi):** Mihenk bildirimi MASAK formunun alan sırasıyla hazırlar, kuyumcu kendi e-Devlet'iyle girip **kopyala-yapıştır** ile 5 dakikada gönderir, program süreyi sayar ve referansı saklar. Şifre programa hiç girmez.

> **Not — farklı bir durum:** e-Fatura entegratörünün API anahtarı bundan farklı. O anahtar kuyumcunun entegratörle yaptığı sözleşmeye bağlı bir **uygulama kimliği**; amacı zaten yazılımın kullanmasıdır. Bu anahtarı AES-256 ile şifreleyip saklamak doğru ve olağan bir uygulama (§ 5, E-2).

---

## 4. Kurucunun vereceği kararlar ve mali müşavire sorular

### Kurucu kararları

| # | Karar | Öneri |
|---|---|---|
| **K1** | e-Fatura entegratörü seçilsin mi, hangisi? | **Evet, şimdi.** § 2-B1.3 doğruysa bilanço esasına göre defter tutan kuyumcuya e-Arşiv olmadan satış yapılamaz. Liste: ebelge.gib.gov.tr. Seçim ölçütleri: belgeli API, test ortamı, ÖZELMATRAH desteği, kontör fiyatı, bayi/aracı modeli (bizim adımıza mı, kuyumcunun kendi sözleşmesiyle mi) |
| **K1a** | Sözleşmeyi kim yapacak: ByGatech mi (aracı) yoksa her kuyumcu kendisi mi? | **Kuyumcu kendisi**, Mihenk yalnız bağlanır. Mali mühür ve sorumluluk mükellefte kalır; bize kontör riski düşmez |
| **K3** | ŞİB kayıtları sunucuda tutulmaya devam etsin mi? | Tutulsun, ama **ByGatech personeli göremesin**: yalnız işletme sahibi ve kendisinin yetkilendirdiği kişiler görsün. Destek erişiminde ŞİB içeriği gizlensin. 5549 md.4/2 gerekçesi (§ 5, M-3) |
| **K4** | Satış dili | **"MASAK entegrasyonu" denmesin.** Önerilen: *"MASAK uyumu: kimlik tespiti, şüpheli işlem hazırlığı, süre takibi"*. Rakiplerin kullandığı ifade gerçeği yansıtmıyor (§ 6) |
| **K5** | 200.000 TL kademesi ne olsun? | Birincil kaynakta yok (A5.1). **Öneri:** mali müşavir/hukukçu "yok" derse kademe kaldırılsın; cevap gelene kadar olduğu gibi kalsın (tutucu) |
| **K6** | Uyum görevlisi | Kuyumcuya zorunlu değil (A4.1). Programdaki "uyum görevlisi" adı "bildirim yetkilisi (işletme sahibi ya da kanuni temsilci)" olarak sadeleştirilebilir |

### Mali müşavire sorular

1. **Kuyumcunun defter türü** (bilanço esası / işletme hesabı / basit usul) ve **2025 brüt satış hasılatı** nedir? (3 milyon TL'yi geçiyor mu?)
2. 509 IV.2.4.3'e göre **1.1.2026'dan itibaren bütün faturaların e-Arşiv olması** gerekiyor mu? İşletme hesabı esasında bu tarih 1.1.2027 mi? Kâğıt fatura şu an tamamen kapandı mı?
3. Perakende satışta 12.000 TL altında **yazarkasa (ÖKC) fişi** yeterli mi? Müşteri fatura isterse fatura e-Arşiv mi olmalı? ("NİHAİ TÜKETİCİ" ibaresinin kullanılabilmesi için ÖKC muafiyeti şartı var mı?)
4. **İnternetten satış** (Instagram, web sitesi, pazaryeri) yapılıyor mu? Yapılıyorsa 500 bin TL haddi uygulanır mı?
5. Kuyum faturasında 805/808 ile 230 kodlarının ayrımı nasıl yapılmalı: işçilikli ziynet = 805, külçe/has = 230 mu? Hurda alımında hangi belge düzenlenmeli (gider pusulası mı, e-Gider Pusulası mı)?
6. **36.000 TL** rakamı nereden çıktı, hangi kurala karşılık geliyor?
7. Hurda alımındaki **stopaj oranları** (eski K2 sorusu, hâlâ açık).
8. Kuyumcunun e-Fatura'ya geçiş tarihi (kayıtlıysa) ve hâlihazırda bir entegratör sözleşmesi var mı?

### Hukukçuya (ya da MASAK'a yazılı) sorular

1. Bulutta çalışan bir yazılımın sunucusunda ŞİB taslağı ve referansının tutulması 5549 md.4/2 bakımından "açıklama" sayılır mı? Hangi teknik önlemlerle (yalnız işletme sahibinin görmesi, uçtan uca şifreleme) risk ortadan kalkar?
2. 200.000 TL'lik "işlem amacı beyanı" diye bir kuyumcu yükümlülüğü var mı?

### Belgelerde düzeltme önerisi (izin bekliyor, yapılmadı)

- `mevzuat-gereksinimleri.md` dosyasında:
  - M-01: 185.000 → 370.000 TL (RG 33393).
  - M-05: 300 karakter kuralı "ŞİB açıklaması"na ait (A1.5).
  - M-06 ve V-04: bu belgedeki ayrıştırmaya göre güncellenmeli.
  - M-07: "XML yükleme" değil, "web formu" (A2.1).
- `MEVZUAT-KUYUMCU-BELGE-2026-10-07.md` dosyasında: "md.3/1-k" → **md.4/1-k** (A4.2).

---

## 5. Ürüne yansıması — iş paketleri

### MASAK

| Paket | İş | Büyüklük | Bağımlılık |
|---|---|---|---|
| **M-1** | Kimlik eşiğini 370.000 TL'ye çekmek (7 Ekim belgesindeki paket). Hâlâ açıksa önce bu yapılmalı | S | yok |
| **M-2** | **ŞİB gönderim yardımcısı:** Uyum ekranında bildirimi MASAK Online formunun adım sırasıyla göstermek (gerçek kişi → tüzel kişi → işlem → şüphe kategorisi 1–3 → açıklama). Her alana "kopyala" düğmesi; açıklamada 300–50.000 karakter sayacı; kuyumcu şüphe tipleri (T-008 listesi) seçenek olarak; ek belge sınırı uyarısı (50/250 MB) | M | yok |
| **M-3** | **ŞİB gizliliği:** ŞİB içeriği yalnız işletme sahibi ve yetkilendirdiği kişilere açılsın. Destek ve yönetici hesaplarında maske; erişim izine "ŞİB görüntülendi" kaydı | M | K3 kararı, hukukçu sorusu 1 |
| **M-4** | Sözlük ve ekran metni: "bağlantı arayüzü bulunmuyor" doğru, kalsın. Eklenecekler: *"MASAK Online'a kendi e-Devlet hesabınızla girersiniz; şifrenizi kimseyle, bizimle de paylaşmayın."* İlk kayıt için *"kayıttan sonra 72 saat beklemeniz gerekir"* uyarısı | S | yok |
| **M-5** | **Yaptırım listesinin düzenli beslenmesi:** kontrol kodda VAR (`YaptirimServisi.cs`); eksik olan, listenin resmî kaynaktan düzenli ve otomatik çekilmesi (bugün `POST /sistem/yaptirim-listesi` ile ByGatech operatörü elle yüklüyor) | M | listenin resmî yayın biçimi (TEYİTSİZ) |
| **M-6** | 200.000 TL kademesi: ayardaki satırı kaldırmak ya da yalnız nakde daraltmak | S | K5 kararı |

### e-Fatura

| Paket | İş | Büyüklük | Bağımlılık |
|---|---|---|---|
| **E-1** | Fatura haddi 12.000 TL'yi `yasal_parametreler` tablosuna tarihli ve DOĞRULANDI damgalı girmek (V-04'ün kesin kısmı) | S | yok |
| **E-2** | **Entegratör ayar ekranı:** API kullanıcı/anahtar (AES-GCM ile şifreli; ekranda maskeli; değiştirme izi tutulur), test/canlı ortam seçimi, gönderici birim etiketi, e-Fatura/e-Arşiv seri ön eki | S | K1 |
| **E-3** | **Fatura modelini e-belgeye uydurmak:** ETTN alanı; 16 karakterlik GİB numara biçimi (bugünkü `FTR-yıl-sıra` yerine); senaryo (TEMEL/TİCARİ/E-ARŞİV); alıcının e-Fatura kayıtlı olup olmadığına göre yönlendirme; satıra göre 805/808/230 kodu; gram ve işçiliğin ayrı satırda gösterilmesi | M | B3.6 teyidi, mali müşavir sorusu 5 |
| **E-4** | **UBL-TR üretimi ve gönderim:** UBL-TR XML üretmek, entegratöre göndermek, durum takibi (kuyrukta / iletildi / hata / ticari faturada kabul-ret), hata sebebinin ekranda Türkçe gösterilmesi, PDF görseli | L | K1, entegratör API belgesi |
| **E-5** | **Gelen fatura** (posta kutusu): tedarikçi faturalarını çekmek, alışa bağlamak, 8 gün içinde kabul/ret | M | E-4 |
| **E-6** | İptal ve iade akışı: e-Arşiv iptali, ticari faturada ret ya da iade faturası; bugünkü iptal akışının buna bağlanması | M | E-4 |
| **E-7** | e-İrsaliye | L | Yalnız 10 milyon TL üstü müşteri çıkarsa. **Şimdilik kapsam dışı** |

**Kritik yol:** K1 (entegratör) → E-2 → E-3 → E-4 → E-6 (E-5 paralel yürüyebilir). E-1, M-1 ve M-4 bağımsız ve küçük; hemen yapılabilir.

**Kıyaslamadan çıkan isteğe bağlı fikirler** (§ 6):
- Kimliği **NFC ile çipten okuma**: M–L; Android/iOS donanımına bağlı.
- **Banka hareketinden otomatik kuyum faturası**: L; açık bankacılık kapsam dışı (H-4).

Yasal zorunluluk değiller; sırada M-2 ve E paketlerinin arkasında gelirler.

---

## 6. Rakip kıyası — Pusula / PusulaX / Kivi (rakip beyanı, mevzuat kaynağı değil)

- **Pusula'nın kendi ürünlerinde MASAK ve e-Fatura yok.** Bu işler ortak firma **Kivi** (Kivi Bilgi Sistemleri A.Ş., Teknopark İstanbul) üzerinden geliyor. PusulaX sitesindeki bölümün adı "KiviERP Entegrasyon". Yıllık 18.900 TL+KDV Kivi'nin fiyatı ("KİVİ JEWELLERY" paketi). [pusulax.com](https://pusulax.com) · [kivierp.com/fiyatlar](https://www.kivierp.com/fiyatlar) · [kivierp.com/kuyum-superapp](https://www.kivierp.com/kuyum-superapp)
- **Kivi'nin "MASAK entegrasyonu" dediği şey:**
  - kimliği kamerayla ve çipten (NFC) okuma,
  - imzalı kimlik tespit belgesi,
  - mal varlığı dondurulanlar listesi kontrolü,
  - eşik uyarısı.
- **Şüpheli işlem bildirimini otomatik göndermiyorlar.** Kendi sitelerindeki ifade: *"hiçbir bildirim otomatik gönderilmez"* ([kivierp.com/uyum-ve-risk](https://www.kivierp.com/uyum-ve-risk)).
  - MASAK şifresi isteyip sakladıklarına dair iz yok.
  - Kendi blog yazılarında eşik hâlâ 185.000 TL yazıyor.
- **e-Fatura:**
  - "Faturalar otomatik GİB'e gider" diyorlar.
  - Kivi, GİB'in özel entegratör listesinde görünmüyor. Arkada hangi entegratörün çalıştığı yazılmıyor: TEYİTSİZ.
  - Kuyum kodlarını (805/808/230) otomatik koyuyorlar.
- **Banka:** açık bankacılıkla 25'ten fazla bankadan hareket çekiyorlar; banka açıklamasında "22 ayar" geçerse kuyum faturası otomatik oluşuyor.
- **Pusula'nın 21.09'dan sonraki durumu:** PusulaX iOS 1.6.0 (destek talebi ve kur ekranı); MASAK ya da e-Fatura yeniliği yok.
- **Başka kuyumcu programları** (Milyem Plus, SarrafPro, hasfiyat): MASAK tarafında kayıt tutma, risk taraması ve yaptırım listesi var; **MASAK Online'a otomatik bildirim gönderen bir kuyumcu programı bulunamadı.**

**Sonuç:** "Pusula aynı işi yapıyor" doğru değil. MASAK tarafında Mihenk'in yöntemi sektörün en ilerisiyle aynı yerde; yaptırım kontrolü bizde de var (liste beslemesi elle). Eksiğimiz NFC ile kimlik okuma ve imzalı kimlik tespit belgesi. e-Fatura tarafında ise rakip önde: bizde gönderimin tek satırı bile yok.

---

## 7. Okunamayan kaynaklar ve açık kalanlar

- `onlineislemler.masak.gov.tr`: e-Devlet girişi gerektiği için içine girilemedi. Kuyumcunun 2.0 taahhüt metni ve kullanıcı sayısı üst sınırı görülemedi.
- masak.hmb.gov.tr sayfaları tarayıcı uygulaması olarak açılıyor; içerik sitenin resmî içerik servisinden alındı. 13–15.10.2026 arasında yayımlanmış bir tebliğ olup olmadığı kontrol edilemedi.
- RG 7.10.2026 PDF'i taranmış görüntü; sayfa görüntüsünden okundu. mevzuat.gov.tr'deki işlenmiş metinle aynı.
- Okunmayanlar:
  - doğrudan entegrasyon başvuru kılavuzu,
  - KDV Genel Uygulama Tebliği'nin kuyum bölümü,
  - 577 Sıra No'lu Tebliğ'in Resmî Gazete metni,
  - herhangi bir entegratörün API belgesi,
  - yaptırım listelerinin resmî yayın biçimi.
- Kaynak bulunamayanlar: GİB'in 2026 taslak sayfası boş geldi; gib.gov.tr/node/138117 ve RG 20241112-3 bağlantıları 404 verdi.
