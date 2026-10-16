> **ARŞİV — TEK KAYNAK DEĞİLDİR (P-01 mührü, 2026-09-18).** Sürümün tek kaynağı
> `dokumanlar/09-uygulama-kayitlari/ham/surum.json`'dur; o dosya `PLAN-TEK-SEFER-2026-09-18`
> § 2 (paket blokları) ve § 5 (kararlar) ile beslenir ve `dotnet/denetim.sh` içindeki
> `SURUM_KARAR_BEKLEYEN = 0` cırcır sayacıyla korunur. Bu belge 2026-09-16 tarihli TASLAK
> hâlini saklar; aşağıdaki sürüm satırları ile `surum.json` çelişirse **`surum.json` geçerlidir**
> (ör. F-1 / F-3 / F-4 / S-1 / SÖZLEŞME-1 / T-4b burada 1.1 yazıyorsa da plan v2 ile **1.0**'dır;
> 5 · 39 · 43 · 40-B · UX-2-C **1.1**, E-BELGE **KAPSAM-DIŞI** olarak mühürlendi). İkinci kaynak açılmaz.

# V-1 Sürüm mührü — 1.0 kapsamı (2026-09-16; TASLAK — Mehmet uyum kontrolü sonrası Tamer Bey mühürler)

> Paket V-1 (MÜHÜR 5 KOŞUL 8): 0,5 gün, **kod yok**, 3-P belge/betik hattında koşar.
> Kaynaklar: `ham/muhur5.md` (nihai dalga sıraları, satışa hazırlık asgarisi, 12 koşul) · `HAZIRLIK-KURULU-2.md` (§ Satışa hazırlık asgarisi, § Mehmet) · `MIMARI-KALAN-PAKETLER-2026-09-15.md` (§ Karar tablosu, § MÜHÜR 2) · `ORKESTRATOR-DURUM.md`.
> **İmza kapısı (MÜHÜR 5 KOŞUL 8, Mehmet VETO):** bu belge, aşağıdaki 4 uyum maddesi belgelenmeden **imzalanamaz**. Bu hâliyle TASLAK'tır.
> **Madde 1 notu:** bu belgedeki tek sayısal iddia, `ORKESTRATOR-DURUM.md`nin son kapı satırından alınmıştır (2026-09-16 15:53, dalga 2a orkestratör kapısı GEÇTİ: build 0 uyarı 0 hata · denetim.sh TEMİZ · tam süit 1312/1312 · 26 migration). Bundan sonraki paketlerin test/migration sayıları burada tekrarlanmaz; her kapı kendi çıktısını yazar.

---

## 1. Neden bu belge var

HAZIRLIK-KURULU-2 § Ramazan: *"1.0 kapsamı hâlâ yazılı değil — sürümleme yok, 'bitti' tanımı yok"* (KRİTİK). Kapsam yazılı olmadığı için "satışa hazır" tanımı her kurul turunda kaydı. V-1 bu kaymayı durdurur:

1. **1.0 = MÜHÜR 5 satışa hazırlık asgarisi** (dalga 2b + 2c + 3 + 4 paketleri) + Mehmet'in 4 uyum maddesi + kanıt asgarisi.
2. **1.1 = sıradaki sürüm**, iptal değil. Kurucu talimatı ("kapsam dışı Katman 1 hariç hepsini bitir") ile çelişmez: 1.1 paketleri bitirilecektir, yalnız **1.0 kapanmadan başlatılmaz**.
3. Karar tablosuna **Sürüm** sütunu eklenir (mimari-render.js bu belgeden beslenir); dalga kapısı **sürüm dışı işi reddeder**.

---

## 2. 1.0 kapsamı — paket paket (MÜHÜR 5 satışa hazırlık asgarisi)

### 2b (koşuyor — BÖLÜNMEZ, yeniden sıralama yok — KOŞUL 2)
- **UX-4d** ✔ (24 madde; hasım tur 1 GEÇTİ)
- **UX-1-B** — PATCH/pasife alma uçları + DoD sözleşmesi
- **38-A** — KDV özel matrah otomatiği Faz A
- **48-A** — storno/düzeltme motoru + DenetimIzi zorunlu kaydı
- **45-API** — MFA sunucu tarafı
- **6-A** — bildirim kanalı + zamanlanmış iş çatısı Faz A
- **3-A** — sipariş/servis durum makinesi Faz A

> KOŞUL 1 (Mehmet vetosu, kapı biçiminde): 38-A'nın getirdiği **hiçbir** fatura/kasa/banka/emanet düzeltme-storno ucu 48-A bitmeden canlıya açılmaz; o ana kadar düğme `disabled` + `DegismezIpucu`. "Ters iade" ile storno taklidi YASAK (Eray vetosu).

### 2c
- **S-2** — acil düzeltme (Urunler.razor:647 StokYaniti NRE + /uyum `{adet,kayitlar}` zarfı) + **"yanıt şekli ↔ record" taraması** (+ record dondurma: denetim.sh record sayacı, yeni istemci record'u yasak — KOŞUL 3)
- **UX-2-A2** — kabuk: ≤768 px çekmece, üst bar, ModalPencere.razor:9,53 odak taşıma/odak hapsi/Esc
- **UX-1-C0** — ortak EditForm/ValidationMessage/Toast/onay modalı (eski UX-4j; ayrı paket değil, C'nin ilk adımı, tek el)
- **UX-1-C1..C4** — 27 sayfa düzenlenebilir + storno düğmeleri, **TEK GEÇİŞ**
- **45-UI** — MFA kurulum ekranı
- **UX-5** — ikon sprite

### 3 (SERİ kod hattı)
**T-5** (migration hijyeni; silme Kurucu'da — KOŞUL 6) → **19** vezne → **25** reyon → **20** metal → **27** çoklu birim cari → **UX-3-B** → **34-A** (34-Ö hız kanıtı kabul ölçütü içinde — KOŞUL 7) → **35-A** → **47-A + T-2** → **T-1 + F-6** → **F-2-min** → **T-3** → **T-4a**

### 4
**29-A** çek/senet → **41** MASAK banka kapısı → **32** toplu onay → **40-A** → **31** → **UX-1-D** → B@4

### 3-P (paralel belge/betik hattı — kod yok, dosya manifesti kapı kaydına yazılmak şartıyla — KOŞUL 4/5)
**V-1** (bu belge) · **F-8** birim ekonomi tablosu · **F-6/F-7 TLS-HSTS taslağı** (belge; uygulama T-1'de) · **T-3 yedek betikleri taslağı** · **RAKIP-KIYAS § 5** güncellemesi · **DEMO-1 / S-1-BELGE** (ayrı DB, F-8 ölçümünden SONRA)

---

## 3. 1.0 uyum asgarisi — Mehmet'in 4 maddesi (bağlayıcı; imza ön şartı)

| # | Madde | 1.0'da nasıl kapanır | Durum |
|---|---|---|---|
| U1 | **MFA** | 45-API (dalga 2b) + 45-UI (dalga 2c) canlıda; **ya da** belgelenmiş telafi edici kontrol (yazılı, kanıtlı, Mehmet onaylı) | AÇIK — 45-API/45-UI henüz başlamadı |
| U2 | **e-Arşiv / e-Fatura durumu** | Durum **"var/yok" olarak KANITLI** netleşir: 38-A (özel matrah + fişten fatura yolu) + E-BELGE Faz A kapsam kaydı + Kurucu entegratör hattı (sözleşme/mali mühür — dış taahhüt, Kurucu kalemi). "Yok" ise sözleşmede **kapsam dışı olduğu açıkça yazılı** olmak zorundadır (HAZIRLIK-KURULU-2 § asgari, e-Belge maddesi) | AÇIK — E-BELGE Faz A dalga 5'te, entegratör Kurucu'da |
| U3 | **Canlı DB'deki 2 sızıntı satırı** | 2026-09-09 tarihli 2 kanıt satırı (hareketler / kasa_hareketleri) kapanır. **Hiçbir paket bu satırlara dokunmaz** (MÜHÜR 2 Mehmet koşulu); silme/arşivleme kararı **Kurucu'nundur**. Kapanmadan pilot/demo yapılmaz (KOŞUL 10) | AÇIK — karar Kurucu'da |
| U4 | **Finansal storno uçlarının tamamı denetim izine bağlı** | 48-A motoru + her PATCH/DELETE ucunda `[ModulYetkisi]` + DenetimIzi kaydı canlı doğrulanır; finansal varlıkta hard DELETE yasak; denetim izi olmayan storno = MASAK/defter bütünlüğü ihlali | AÇIK — 48-A dalga 2b'de |

---

> Kanıt yeri (Mehmet eki): U1 → PAKET-45-uygulama-raporu.md + kapı kaydı (scratchpad/kapi-dalga2b/ ve 2c) · U2 → PAKET-38-uygulama-raporu.md + E-BELGE Faz A kapsam kaydı + KURUCUYA-KARARLAR-2026-09-16.md md. 1 (entegratör/mali mühür durumu) · U3 → KURUCUYA-KARARLAR md. 2 kararı + dotnet/yetim-tara.sh çıkış kodu 0 kanıtı + ORKESTRATOR-DURUM.md kapı satırı · U4 → PAKET-48-uygulama-raporu.md § canlı (storno POST → denetim kaydı SQL) + dotnet/denetim.sh sayaçları. Her biri kapandığında ORKESTRATOR-DURUM.md kapı satırında "U1 KAPANDI (kanıt: …)" biçiminde yazılır.

## 4. 1.0 kanıt asgarisi (kapı geçmeden "bitti" yok — Madde 1)

- Düzenlenemeyen veri girişi sayısı = **0** (turda düzenlenemeyen tek alan bile bulgu = engel)
- Kritik bulgu sayısı = **0**
- Tam test süiti **0 başarısız** (taban: son kapı satırı, 2026-09-16 15:53 — 1312/1312; her kapı kendi güncel sayısını yazar)
- `dotnet/denetim.sh` **TEMİZ**
- **Mutasyon kanıtı** (her paket için kırmızıya dönen mutantlar)
- **27 sayfalık tarayıcı turu 1280 px öykünmesinde tekrarlanmış** (önceki turlar dar bölmede alındı; görsel puan bu tekrar olmadan kesinleşmez)
- **Hazırlık kurulu eşiği:** `hazirlik-kurulu.js` — **herkes HAZIR** + **ortalama ≥ 8/10**
- Test kiracısı silinir (QA-TEST-* kalıcı fikstürler hariç — asla silinmez)

---
- Canlı DB'deki 2 sızıntı satırının (U3) kapanması, 27 sayfalık tur ve demo/pilot doğrulamasının ÖNKOŞULUDUR; kapanmadan bu turlar 1.0 kanıtı sayılmaz (KOŞUL 10). (Mehmet eki)

## 5. Sürüm tablosu — her paket için karar

> `paket_no` yazımı `MIMARI-KALAN-PAKETLER-2026-09-15.md` § Karar tablosu ile birebir aynıdır. Alt fazlar (A/B, -A2, -C0 vb.) gerekçe sütununda belirtilir. **"karar bekliyor"** = MÜHÜR 5 ne 1.0 asgarisine ne 1.1 kuyruğuna yazdı; varsayım yapılmadı (Madde 1).

| paket_no | Paket | Sürüm | Gerekçe |
|---|---|---|---|
| 1 | Fiş / işlem grubu + kalem modeli | 1.0 | 1-A dalga 1'de bitti; 1.0 ağacının çekirdeği (fatura_kalemleri E-BELGE'nin de önkoşulu). Faz B kapsamı 1.0 asgarisinde sayılmadı |
| 3 | Sipariş çatısı (durum makinesi) | 1.0 | 3-A dalga 2b asgarisinde (MÜHÜR 5 § 2b son kalem) |
| 4 | Dosya yükleme ve belge deposu | 1.0 | 4-A dalga 1'de bitti |
| 5 | Belge / etiket şablon motoru | karar bekliyor | Dalga 5; MÜHÜR 5 ne 1.0 asgarisine ne 1.1 kuyruğuna yazdı |
| 6 | Bildirim kanalı + zamanlanmış iş çatısı | 1.0 | 6-A dalga 2b asgarisinde |
| 19 | Vezne modeli | 1.0 | Dalga 3 asgarisi (T-5 sonrası ilk paket) |
| 20 | Metal tipi genişletme + saflık tabloları | 1.0 | Dalga 3 asgarisi; Bertan kısmi vetosu 1.1'e itilmesini reddetti |
| 25 | Reyon/vitrin bazlı stok bölümlemesi | 1.0 | Dalga 3 asgarisi |
| 27 | Çoklu birim cari bakiye | 1.0 | Dalga 3 asgarisi; 34-A/35-A'nın önkoşulu |
| 29 | Çek / senet portföyü | 1.0 | 29-A dalga 4'ün ilk paketi; 41'in önkoşulu |
| 31 | POS cihazı takibi + komisyon + valör | 1.0 | Dalga 4 asgarisinde (MÜHÜR 5 § 4) |
| 32 | Banka ekstresi içe aktarma + eşleştirme | 1.0 | Dalga 4 asgarisi; 41 → 32 sırası Mehmet vetosuyla kapalı |
| 34 | Rapor seti tamamlama | 1.0 | 34-A dalga 3 asgarisi (34-Ö hız kanıtı kabul ölçütü içinde — KOŞUL 7). Faz B 1.0 asgarisinde değil |
| 35 | Kâr-zarar analizi derinleştirme | 1.0 | 35-A dalga 3 asgarisi; rapor mutabakatı satış kapısı şartı |
| 36 | Patron paneli | 1.1 | MÜHÜR 5 § 1.1 kuyruğu, açık |
| 37 | Pozisyon / net varlık ekranı | 1.1 | MÜHÜR 5 § 1.1 kuyruğu, açık |
| 38 | KDV özel matrah otomatiği | 1.0 | 38-A dalga 2b asgarisi. Faz B = E-BELGE, ayrı satır |
| 39 | Has altın defteri otomatiği | karar bekliyor | Dalga 5; MÜHÜR 5 iki listeye de yazmadı |
| 40 | MASAK risk skorlama + PEP + yaptırım | 1.0 | 40-A dalga 4 asgarisinde. Yaptırım tarayıcı **seçimi** MÜHÜR 4 K9 ile dalga 8'e ertelendi (1.0 dışı) |
| 41 | MASAK kapısının banka hareketlerine yayılması | 1.0 | Dalga 4 asgarisi, 29-A'dan hemen sonra (Mehmet'in "en erken nokta" talebi) |
| 42 | Kâğıt sevk irsaliyesi | karar bekliyor | Dalga 5; MÜHÜR 5 iki listeye de yazmadı |
| 43 | Mobil / dar ekran uyarlaması | **karar bekliyor** | **Tek açık kalem** — oylar 2-2 bölündü. Karar dalga 4 sonunda kapasiteye göre (bkz. § 6). UX-2-A2 kabuğu (≤768 px çekmece) 1.0'da; 43 tam uyarlama ayrı |
| 44 | PWA kabuğu | 1.1 | MÜHÜR 5 § 1.1: "44/PWA-1 kalanı" |
| 45 | İki aşamalı giriş (TOTP / MFA) | 1.0 | 45-API (2b) + 45-UI (2c); Mehmet uyum maddesi U1. MFA bitmeden T-1 üretim yayın adımı başlamaz |
| 46 | Şube bazlı görünürlük + yetki matrisi | 1.1 | MÜHÜR 5 § 1.1 kuyruğu |
| 47 | Veri göçü aracı (kapılı, kuru provalı) | 1.0 | 47-A + T-2 birlikte, dalga 3 asgarisi; canlı veride deneme yasak |
| 48 | Denetim izi tamamlama + storno motoru | 1.0 | 48-A dalga 2b asgarisi; Mehmet uyum maddesi U4'ün taşıyıcısı |
| 49 | Fiyat listesi = ürün görünümü | 1.0 | Dalga 2a'da bitti, kapıdan geçti (15:53) |
| T-1 | Yayın ve kurulum paketi (T-1 + F-6 birleşik) | 1.0 | Dalga 3 asgarisi; kurulabilirlik satış kapısı şartı. 45 bitmeden yayın adımı başlamaz |
| T-2 | Açılış bakiyeleri ve işletme kuruluş sihirbazı | 1.0 | 47-A ile birlikte sevk, dalga 3 asgarisi |
| T-3 | Yedek otomasyonu ve doğrulanmış geri yükleme | 1.0 | Dalga 3 asgarisi (betik taslağı 3-P'de, uygulama dalga 3'te) |
| T-4 | Destek ve işletim görünürlüğü | 1.0 (T-4a) | T-4a dalga 3 asgarisinin son kalemi (sürüm damgası + korelasyonlu hata kimliği). **T-4b destek paketi dalga 6 → 1.1** |
| F-2-min | Kayıt ucu kilidi ve deneme asgarisi | 1.0 (Faz A) | Faz A dalga 3 asgarisinde. **Faz B (SistemKiracilar.razor) MÜHÜR 4 K5 ile dalga 6'ya → 1.1** |
| F-8 | Birim ekonomi ve fiyatlandırma dosyası | 1.0 | 3-P hattında; KOŞUL 9 — F-8 dolmadan fiyat ilanı/teklif/para tahsili yasak |
| S-1 | Ticari paket (kademe, demo betiği, SLA, referans) | 1.1 | Dalga 6; MÜHÜR 5 satış asgarisi dışında. 3-P'deki **S-1-BELGE/DEMO-1** yalnız belge/tohumlama taslağıdır, paketin kendisi değil |
| PWA-1 | PWA kabuğu asgarisi | 1.1 | MÜHÜR 5 § 1.1: "44/PWA-1 kalanı"; 43 → 44 → PWA-1 tek elden |
| E-BELGE | e-Fatura / e-Arşiv entegrasyonu (38 Faz B) | karar bekliyor | Dalga 5, Faz B entegratör sözleşmesine bağlı (**Kurucu kalemi, dış taahhüt**). 1.0 kapısı paketin kendisini değil, **durumun var/yok kanıtlı beyanını** şart koşar (U2) |
| UX-1 | Her veri girilen yer düzeltilebilir | 1.0 | UX-1-A ✔ (dalga 1) · UX-1-B (2b) · UX-1-C0..C4 (2c) · UX-1-D (dalga 4). "Düzenlenemeyen veri girişi = 0" 1.0 kanıt asgarisidir |
| UX-2 | Görsel çekicilik — tasarım sistemi | 1.0 (A2 + UX-5) | UX-2-A2 kabuk + UX-5 ikon sprite 2c asgarisinde. **UX-2-C dalga 5 → karar bekliyor · UX-2-D → 1.1** |
| UX-3 | Basit / Profesyonel mod | 1.0 (A + B) | UX-3-A ✔ (dalga 1) · UX-3-B dalga 3 asgarisi. **UX-3-C → 1.1** |
| UX-4 | Mantık ve tutarlılık düzeltmeleri | 1.0 | 4a/4b/4c dalga 2a'da kapıdan geçti; 4d dalga 2b'de GEÇTİ. UX-4j ayrı paket değil → **UX-1-C0** |
| T-5 | Migration hijyeni (yeni; MÜHÜR 5) | 1.0 | Dalga 3'ün BAŞI; kapsam = tespit + CI kuralı ("0 bayt .cs = hata"). Silme Kurucu onayında (KOŞUL 6) |
| S-2 | Acil yanıt-şekli düzeltmesi (yeni; MÜHÜR 5) | 1.0 | 2c'nin İLK işi; kapanmadan demo/canlı doğrulama beyanı ve 2c tarayıcı turu yok |
| SÖZLEŞME-1 | Kuyumcu.Contracts + 101 record taşıma | 1.1 | 4/4 ret: 27 sayfayı aynı anda kırar, 2c kanıt rejimini imkânsız kılar. **1.0 öncesi BAŞLATILAMAZ** (KOŞUL 3); yerine record dondurma |
| F-1 | Abonelik çekirdeği | 1.1 | MÜHÜR 5 § 1.1 kuyruğu (dalga 8) |
| F-3 | Plan kapısı | 1.1 | MÜHÜR 5 § 1.1 kuyruğu (dalga 8) |
| F-4 | Telemetri | 1.1 | MÜHÜR 5 § 1.1 kuyruğu; KVKK aydınlatması olmadan açılamaz (Mehmet kapısı) |
| F-6 | Üretim profili / TLS | 1.0 | T-1 ile TEK PAKET (Bertan mimari kararı); taslağı 3-P'de |
| F-7 | TLS-HSTS | 1.0 (belge) | 3-P'de belge olarak; uygulaması T-1+F-6 içinde |
| sadakat · prim · RFID | Sadakat programı · personel primi · RFID | 1.1 | MÜHÜR 5 § 1.1 kuyruğunun son kalemi |
| 40-B | KVKK aydınlatma/rıza + yaptırım listesi ad alanı şifrelemesi (41 MASAK banka kapısıyla ilişkili; Mehmet yazılı onayı + KVKK aydınlatma metni şart) | karar bekliyor | Mehmet incelemesi 2026-09-16: tabloda eksikti; MÜHÜR 2 dalga 5, yaptırım tarayıcı seçimi MÜHÜR 4 K9 ile dalga 8 — sürümü Mehmet onayı + Kurucu KVKK metniyle netleşir; F-4 KVKK notuyla karıştırılmaz |

---

## 6. Tek açık kalem — 43 (mobil / dar ekran uyarlaması)

**Durum: VARSAYIM YAPILMADI.** MÜHÜR 5 oylamasında bölünme: 1.0 diyenler (Aşkın + 1) / 1.1 diyenler (Bertan + Eray) → **2-2**. Karar **dalga 4 sonunda, o andaki kapasiteye ve QA turu ölçümüne göre** verilir ve karar tablosuna yazılır.

| Seçenek | Sonuç | Riskler / kazanç |
|---|---|---|
| **43 → 1.0** | 1.0 satışa hazırlık asgarisi 25 sayfalık dar ekran uyarlamasını da kapsar; kurul § asgari "375 px'te 27/27 rotada yatay kaydırma yok" maddesi 1.0'da kapanır | (+) Sahada tezgâh/telefon kullanımı ilk sürümde çalışır; rakiplerin hepsi mobil gösteriyor. (−) Dalga 4 sonrası 5-8 gün ek; index.html/app.css 44/PWA-1 ve UX-2 ile paylaşımlı → tek el zorunlu; 1.0 kapanış tarihi belirsizleşir |
| **43 → 1.1** | 1.0, UX-2-A2 kabuğuyla (≤768 px çekmece + üst bar + Esc/odak) yetinir; tam uyarlama 44/PWA-1 ile birlikte 1.1'de tek elden | (+) 1.0 kapanışı öne gelir, PWA ile aynı dosya kümesi tek seferde yazılır (ikinci el yeniden iş yok). (−) 375 px maddesi 1.0'da **kısmen** kapanır; dar ekran demosu kabuk düzeyinde kalır — satış materyalinde "mobil uyumlu" iddiası 1.1'e kadar YASAK (doğrulanmamış rakam/iddia yasağı, KOŞUL 9) |

**Karar kuralı:** dalga 4 kapısında düzenlenemeyen kalem = 0 ve kritik = 0 sağlanmışsa ve kapasite varsa 43 → 1.0; aksi hâlde 43 → 1.1 ve satış materyalinde mobil iddiası kapalı. Karar veren: ekip oyu + Tamer Bey mührü.

---

## 7. Kapı metrikleri ve HAZIR eşiği

`kapi.sh` her kapıda şu üç QA metriğini basar (MÜHÜR 5 KOŞUL 8):

| Metrik | 1.0 eşiği |
|---|---|
| **Kritik bulgu sayısı** | 0 |
| **Düzenlenemeyen veri girişi sayısı** | 0 |
| **Görsel / kullanıcı dostu puanı** | 1280 px öykünmesinde tekrarlanmış turda ölçülür; kurul ortalaması ≥ 8/10 eşiğinin bileşeni |

Bunlara ek olarak her dalga kapısı (KOŞUL 12): `dotnet test` 0 başarısız · `denetim.sh` TEMİZ · mutasyon kanıtı · 27 sayfalık tarayıcı turu · `hazirlik-kurulu.js`. **HAZIR eşiği: 7 merceğin tamamı HAZIR + ortalama ≥ 8/10.** Kanıtsız "bitti" yok (Madde 1).

Not (Madde 1): önceki QA turları dar tarayıcı bölmesinde ölçüldü; görsel/kullanıcı dostu puanları 1280 px turu tekrarlanana dek **kesin değildir** ve hiçbir materyalde kullanılamaz.

---

## 8. "Sürüm dışı işi kapı reddeder" kuralı

1. `dalga-uygula.js` args'ına her paket için **sürüm etiketi** (`surum: "1.0" | "1.1" | "karar-bekliyor"`) yazılır; kapı kaydı bu etiketi taşır.
2. **1.1 etiketli paket, 1.0 kapanmadan başlatılamaz.** Başlatılırsa kapı REDDEDER (sebep: "sürüm dışı iş").
3. `karar-bekliyor` etiketli paket de başlatılamaz; önce ekip oyuyla sürümü belirlenir (43 için § 6 kuralı).
4. Bu kural bir **sıra** kuralıdır, **iptal** değildir: Kurucu'nun "kapsam dışı Katman 1 (imalat/atölye) hariç hepsini bitir" talimatıyla çelişmez — 1.1 paketleri 1.0 mühürlendikten sonra aynı otonomiyle bitirilir.
5. İstisna yok; istisna ancak ekip oyu + Tamer Bey mührü ile açılır ve bu belgeye işlenir.

---

## 9. İmza

- **TASLAK** — Mehmet'in 4 uyum maddesi (§ 3) belgelenmeden **imzalanamaz** (MÜHÜR 5 KOŞUL 8, VETO).
- Uyum maddeleri kapandığında: Mehmet kontrolü → **Tamer Bey mührü** → karar tablosuna Sürüm sütunu işlenir (`mimari-render.js`) → `dalga-uygula.js` sürüm etiketi devreye alınır.

- **Mehmet kontrolü (2026-09-16, tur 1): KOŞULLU — U1-U4 dördü de AÇIK; belge TASLAK statüsünü korur, imza koşulu sağlanmadı.** Eksikler işlendi (§ 3 kanıt yeri, § 4 U3 önkoşulu, § 5 40-B satırı). Vetolar: U1-U4'ten biri açıkken "1.0 mühürlendi" imzası VETO; 40-B sürümü netleşmeden VETO; 38-A storno uçları 48-A bitmeden canlıya açılırsa VETO; "ters iade" ile storno taklidi VETO. Sonraki kontrol: U1-U4 kapı kanıtıyla kapanınca (tahminen dalga 3 kapısı).
