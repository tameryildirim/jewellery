# F-8 — Birim ekonomi ve fiyatlandırma dosyası (2026-09-16, Faz A; ÖNERİ — fiyat ilanı DEĞİL)

> Mustafa (09 FinOps) + Eray (08 Satış) araştırması; tek gerçek kaynak `09-uygulama-kayitlari/ham/f8-birim-ekonomi.json` (bu tablo ondan türer). MÜHÜR 2: bu dosya dolmadan hiçbir kanalda rakamlı fiyat açıklanmaz; kurucu onayı olmadan ilan yok. Sözleşme numarası/temsilci bilgisi yazılmaz. TAHMİN satırları toplamdan ayrı gösterilir.

## Özet

F-8 Faz A sentezi. Kanıtlı tablo: yazılım lisansı 0 TL (csproj:10-17 sekiz paket izinli açık kaynak) — rakiplerin kullanıcı/modül başı ücretine karşı tek yapısal üstünlüğümüz. Platform sabit tabanı ~554 TL/ay KDV hariç (Hetzner CX33 476 + Backblaze 100 GB 29 + alan adı 49). Kiracı başı çekirdek toplam: 10 kiracıda 89 TL/ay, 50'de 45 TL, 100'de 40 TL — destek HARİÇ, kapasite ÖLÇÜLMEDİ. Uyum+ kademesinde yaptırım taraması (OpenSanctions 5,61 TL/sorgu × 50 = 281 TL) kiracı başı maliyeti ~321 TL'ye çıkarır; tek ciddi değişken kalem budur. e-Fatura kontörü ve mali mühür MÜKELLEFE aittir, bizim maliyetimiz değildir. Kontörde 3,5 kat fiyat farkı var (Paraşüt 3,30 TL vs eLogo 11,40 TL) → üç yazılı teklif zorunlu. İKİ SATIR ÖLÇÜLMEDİ: destek saati ve kiracı başı kaynak tüketimi. Bu yüzden FAIL-CLOSED 1 gereği FİYAT İLANI KİLİTLİ kalır; aşağıdaki kademeler ÖNERİDİR, ilan değildir. Kurucunun 'memleketten silme' hedefi dürüstçe: Mikro Jump'a karşı ~%86, Kuyumcum.net 3 kullanıcıya karşı ~%73 tasarruf çıkıyor; ama Dezy'nin 500 TL/ay tabanına karşı önerimiz DAHA PAHALI — orada tasarruf iddiası edilemez.

## Maliyet tablosu — kaynaklı satırlar (19)

| Kalem | Sınıf | Tutar | Dönem | Kiracı başı aylık TL | Belirsizlik (alt/olası/üst) | Kaynak | Tarih | Güven |
|---|---|---|---|---|---|---|---|---|
| VPS — Hetzner CX33 (4 vCPU / 8 GB / 80 GB NVMe) | ALTYAPI | 8.49 EUR | AYLIK | 10 kiracı: 47,6 · 50: 9,5 · 100: 4,8 (kapasite ÖLÇÜLMEDİ, payda varsayım) | alt 308 TL (CX23) / olası 476 TL (CX33) / üst 897 TL (CX43) | https://docs.hetzner.com/general/infrastructure-and-availability/price-adjustment/ | 2026-06-15 | KAYNAKLI |
| VPS alternatifi — DigitalOcean Basic Droplet (2 vCPU / 4 GB / 80 GB) | ALTYAPI | 24 USD | AYLIK | 100 kiracı: 11,7 TL (Hetzner'in ~2,5 katı; 2026'da zam görülmedi) | alt 1.168 / olası 1.168 / üst 1.168 TL | https://www.digitalocean.com/pricing/droplets | 2026-09-16 | KAYNAKLI |
| Yedek depolama — Backblaze B2 (100 GB, S3 uyumlu) | ALTYAPI | 0.6 USD | AYLIK | 10 kiracı: 2,9 · 50: 0,6 · 100: 0,3 | alt 29 TL (B2 100 GB) / olası 29 TL / üst 364 TL (Hetzner Object Storage taban) | https://www.backblaze.com/cloud-storage/pricing | 2026-09-16 | KAYNAKLI |
| Alan adı .com (yıllık) | ALTYAPI | 12 USD | YILLIK | 100 kiracı: 0,49 TL/ay (48,7 TL/ay platform gideri) | alt 72 TL (.com.tr) / olası 584 TL (.com) / üst 600 TL | https://www.inetmar.com/domain/domain-fiyatlari/ | 2026-09-16 | KAYNAKLI |
| TLS sertifikası — Let's Encrypt (ACME/certbot) | ALTYAPI | 0 TRY | AYLIK | 0 | alt 0 / olası 0 / üst 0 TL | https://letsencrypt.org/ | 2026-09-16 | KAYNAKLI |
| Veritabanı — MySQL 8.4 Community, kendi VPS'imizde (docker-compose) | LISANS | 0 TRY | AYLIK | 0 (yönetilen MySQL'e geçilirse 100 kiracıda 7,3-29,6 TL) | alt 0 TL (kendi sunucu) / olası 0 TL / üst 2.963 TL (DO Managed 2 vCPU/4 GiB) | https://www.mysql.com/about/legal/licensing/oem/ | 2026-09-16 | KAYNAKLI |
| Yazılım lisansı — Kuyumcu.Api.csproj 8 NuGet paketi (tamamı izinli açık kaynak) | LISANS | 0 TRY | AYLIK | 0 — rakiplerin kullanıcı/modül başı ücretine karşı yapısal üstünlük | alt 0 / olası 0 / üst 0 TL | dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj:10-17 (depo içi kanıt) | 2026-09-16 | KAYNAKLI |
| SMS — Netgsm toplu SMS (10.000'lik paket, standart) | DIS_TEDARIKCI | 0.17 TRY | AYLIK | 200 SMS/ay varsayımı → 34 TL (KDV+ÖİV DAHİL fiyat) | alt 0,077 TL/SMS (İleti Merkezi 100k) / olası 0,17 TL/SMS / üst 0,37 TL/SMS (1.000'lik paket) | https://www.netgsm.com.tr/fiyatlar/toplu-sms | 2026-09-16 | KAYNAKLI |
| Kur verisi — bugünkü üç ücretsiz kaynak (Truncgil/Altınkaynak/AAB) | DIS_TEDARIKCI | 0 TRY | AYLIK | 0 bugün; ticari geçişte 100 kiracıda ~9,2 TL/ay | alt 0 TL (bugün) / olası 0 TL / üst 924 TL/ay (altinapi.com ~19 $, TEYİT EDİLMEDİ) | https://finans.truncgil.com/ · KurModulu.cs:292/362/452 | 2026-09-16 | KAYNAKLI |
| Kur verisi — TCMB EVDS resmî referans API | DIS_TEDARIKCI | 0 TRY | AYLIK | 0 — gün içi serbest piyasa altını vermez, yalnız resmî gösterge | alt 0 / olası 0 / üst 0 TL | https://evds2.tcmb.gov.tr/index.php?%2Fevds%2FuserDocs= | 2026-09-16 | KAYNAKLI |
| Yaptırım tarama — OpenSanctions SaaS API (sorgu başı) | DIS_TEDARIKCI | 0.1 EUR | AYLIK | 50 sorgu/ay varsayımı → 280,5 TL (yalnız Uyum+ kademesi) | alt 0 TL (resmî ücretsiz listeler + kendi eşleme) / olası 5,61 TL/sorgu / üst teklif bazlı (Dow Jones/LexisNexis) | https://www.opensanctions.org/api/ | 2026-09-16 | KAYNAKLI |
| Yaptırım listeleri — OFAC SDN / BM Konsolide / AB Konsolide (resmî, ücretsiz) | DIS_TEDARIKCI | 0 TRY | AYLIK | 0 (veri); indirme/ayrıştırma/eşleme geliştirme maliyeti ÖLÇÜLMEDİ | alt 0 / olası 0 / üst 0 TL veri bedeli; geliştirme+işletme saati AYRI | https://sanctionslist.ofac.treas.gov/ | 2026-09-16 | KAYNAKLI |
| e-Fatura kontörü — Paraşüt (200'lük paket, en ucuz birincil ilan) | DIS_TEDARIKCI | 3.3 TRY | AYLIK | 50 fatura/ay → 165 TL. MÜKELLEFE aittir; pakete alınırsa maliyetimize eklenir. | alt 3,30 TL/kontör (Paraşüt 200'lük) / olası 4,00 TL / üst 15,00 TL (eLogo 150'lik) | https://www.parasut.com/on-muhasebe-fiyatlari | 2026-09-16 | KAYNAKLI |
| e-Fatura kontörü — eLogo (500'lük paket, kontör SÜRESİZ) | DIS_TEDARIKCI | 11.4 TRY | AYLIK | 50 fatura/ay → 570 TL. TUZAK: ALINAN e-Fatura da kontör düşer, gerçek tüketim daha yüksek. | alt 5,48 TL/kontör (25.000'lik) / olası 11,40 TL (500'lük) / üst 15,00 TL (150'lik) | https://cdn.logo.com.tr/files/logocomtr/Uploads/Documents/elogo-ecozumler-fiyat-listesi.pdf | 2026-09-09 | KAYNAKLI |
| Paraşüt e-Portal giriş bedeli (tek seferlik, 120 hediye kontör) | DIS_TEDARIKCI | 1800 TRY | TEK_SEFER | 12 aya yayılırsa 150 TL/ay; yıl-2'de düşer. Taahhüt ve kurulum ücreti yok. | alt 1.800 / olası 1.800 / üst 1.800 TL + KDV | https://www.parasut.com/on-muhasebe-fiyatlari | 2026-09-16 | KAYNAKLI |
| TÜRMOB mükellef sorgulama kontörü (eLogo) — KYC/cari açılışı | DIS_TEDARIKCI | 0.44 TRY | TEK_SEFER | VKN 2, TCKN 3 kontör; 50 sorgu/ay → ~0,5 TL/ay. İhmal edilebilir. | alt 0,44 / olası 0,44 / üst 0,44 TL/kontör + KDV | https://cdn.logo.com.tr/files/logocomtr/Uploads/Documents/elogo-ecozumler-fiyat-listesi.pdf | 2026-09-09 | KAYNAKLI |
| Mali mühür sertifikası (MÜS) 3 yıl + kart okuyucu — Kamu SM | DIS_TEDARIKCI | 1700 TRY | TEK_SEFER | 47,2 TL/ay eşdeğeri (1.700/36). MÜKELLEFE aittir, bizde değil. | alt 1.400 TL+KDV (NES 1 yıl) / olası 1.700 TL+KDV (MÜS 3 yıl) / üst 5.250 TL+KDV (HSM'e yüklü) | https://mm.kamusm.gov.tr/fiyatlandirma.jsp | 2026-08-01 | KAYNAKLI |
| e-İmza (NES) — gerçek kişi mükellef alternatifi, Kamu SM | DIS_TEDARIKCI | 1950 TRY | TEK_SEFER | 54,2 TL/ay eşdeğeri (1.950/36). Mükellefe aittir. | alt 1.150 TL+KDV (Mobil NES 1 yıl) / olası 1.950 TL+KDV (3 yıl) / üst 2.340 TL KDV dahil | https://kamusm.bilgem.tubitak.gov.tr/urunler/nitelikli_elektronik_sertifika/fiyatlandirma.jsp | 2026-08-01 | KAYNAKLI |
| Referans kur — TCMB döviz satış (tüm TL çevrimlerinin dayanağı) | ALTYAPI | 48.646 TRY | AYLIK | (çevrim parametresi — maliyet kalemi değil) | USD 48,646 · EUR 56,1216 — kur riski AYRI satırdır, TL karşılıkları kurla birlikte değişir | https://www.tcmb.gov.tr/kurlar/today.xml | 2026-09-15 | KAYNAKLI |

## Maliyet tablosu — TAHMİN / ÖLÇÜLMEDİ satırları (4; toplamdan ayrı)

| Kalem | Sınıf | Tutar | Dönem | Kiracı başı aylık TL | Belirsizlik | Dayanak | Güven |
|---|---|---|---|---|---|---|---|
| E-posta bildirimi — Amazon SES | DIS_TEDARIKCI | 0.1 USD | AYLIK | <1 TL | alt 0 (ilk 12 ay 3.000/ay ücretsiz) / olası 4,86 TL/1.000 / üst 4,86 TL | https://smtpedia.com/amazon-aws-ses-pricing/ (ikincil; resmî sayfa çekilmedi) | TAHMIN |
| Destek — kiracı başı aylık destek saati × saatlik maliyet | DESTEK | 0 TRY | AYLIK | ÖLÇÜLMEDİ — toplamın DIŞINDA. Gerçek kiracı başı maliyet bu satırla yükselecektir. | ÖLÇÜLMEDİ — alt/olası/üst yazılamaz (Madde 1: uydurulmaz) | ÖLÇÜM HATTI KURULDU (P-28, 2026-09-23): `destek_dakikalari` tablosu + `GET /api/v1/sistem/kiracilar/{id}/destek-dakikasi`; rakam PİLOT (P-32) verisi gelince doldurulur. Bugün kayıt yok → sayı YAZILMAZ. | OLCULMEDI |
| Kiracı başı CPU/RAM/disk tüketimi (kapasite paydası) | ALTYAPI | 0 TRY | AYLIK | ÖLÇÜLMEDİ — altyapı satırlarının payda sayısı bu ölçümle kesinleşir | ÖLÇÜLMEDİ — 100 kiracı/tek CX33 VARSAYIMDIR (FAIL-CLOSED 5) | ÖLÇÜM HATTI KURULDU (P-28, 2026-09-23): kiracı etiketli saatlik sayaç `telemetri_olcumleri` (istek · hata · işlem ms · yanıt bayt) + depolama vekili (satır sayısı); `GET /api/v1/sistem/telemetri`. Payda rakamı k6 yük testi (P-25) + pilot (P-32) verisiyle doldurulur; bugün üretim yükü YOK → sayı YAZILMAZ. | OLCULMEDI |
| On-prem (müşteride) barındırma senaryosu — bulut gideri sıfırlanır | ALTYAPI | 0 TRY | AYLIK | 0 (altyapı) — ama MySQL'i ürünle BİRLİKTE dağıtırsak GPL/ticari lisans sorusu doğar | alt 0 TL (donanım müşteride) / olası 0 TL bulut gideri / üst kurulum+bakım saati ÖLÇÜLMEDİ | docker-compose.yml + deploy/nginx-kuyumcu.conf (depo içi profil) | TAHMIN |


## F-8 FAZ B — ÖLÇÜM HATTI (PAKET P-28, 2026-09-23)

> **Bu bölüm rakam İÇERMEZ ve içeremez.** P-28 iki ölçülmemiş satırın **ölçüm
> altyapısını** kurar; rakamı ölçüm üretir, bu belgeye elle sayı yazılmaz
> (FAIL-CLOSED 1 AKTİF kalır).

| Faz B satırı | Ölçüm kaynağı (bugün canlı) | Rakamı getirecek paket | Durum |
|---|---|---|---|
| Kiracı başı kaynak tüketimi (kapasite paydası) | `telemetri_olcumleri` — kiracı + UTC saat başına istek, 5xx hata, toplam işlem ms, yanıt bayt. Uç: `GET /api/v1/sistem/telemetri` (kapasite özeti), `GET /api/v1/sistem/kiracilar/{id}/telemetri`. Depolama **satır sayısıyla** ölçülür (bayt ölçümü `information_schema` gerektirir → iddia edilmez). | P-25 (k6 yük testi) + P-32 (pilot) | ÖLÇÜLMEDİ — altyapı hazır, veri yok |
| Kiracı başı destek dakikası | `destek_dakikalari` — tarih, dakika, kanal (TELEFON/UZAKTAN/YERINDE/YAZILI), konu (KURULUM/EGITIM/HATA/VERI/DIGER), kaynak (MANUEL/TANILAMA), aktif. Uçlar: liste / POST / PATCH / DELETE (**pasife alır**). | P-32 (pilot) — ayrıca P-31 tanılama kanalı aynı kayda yazar | ÖLÇÜLMEDİ — altyapı hazır, veri yok |

**Ölçümün dürüstlük kuralı (Anayasa md.1):** servis, veri yokken 0 döndürmez;
`olcumVarMi=false` döndürür. "0 dakika destek" ile "hiç ölçülmedi" aynı şey
değildir ve bu ayrım kodda kilitlidir (`TelemetriOzetDto.OlcumVarMi`).

**Mahremiyet sınırı (BK-12 · Mehmet koşulu):** otomatik telemetri **yalnız
sayaç** taşır. Yol (path), sorgu dizesi, başlık, gövde, IP, kullanıcı kimliği
hiçbir alanda tutulmaz; kiracı kimliği **etikettir**, içerik değildir. Bu iddia
`dotnet/denetim.sh` → `TELEMETRIDE_KISISEL_VERI = 0` sayacıyla ve
`P28DenetimKapisiTestleri` ile **makineyle** kanıtlanır.

**Sayaç kilidi:** `F8_OLCULMEMIS_SATIR` bugün **2**'dir ve tavanı 2'de
mühürlüdür — **yalnız azalır**. Yeni bir ölçülmemiş satır açmak kapıyı kırar;
iki satır dolunca tavan 0'a indirilir ve fiyat kilidi ancak o zaman tartışılır.

## GELİŞTİRME MALİYETİ (BK-41) — YÖNTEM YAZILI, RAKAM ÖLÇÜLMEDİ

BK-41 "ajan-saat → token → ₺" dönüşümünü ister. **Bugün bu dönüşüm
yapılamaz ve tahmin edilmez** (Madde 1):

- **Ajan-saat** tarafı ölçülebilir durumdadır: paket tahminleri ve gerçekleşen
  süreler `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` ve paket
  uygulama raporlarında satır satır tutulur (plan § 9 formülü: 362,1 ajan-saat).
- **Token** tarafı bu depoda **kayıtlı değildir**: oturum token sayaçları
  Claude hesabı kullanım kayıtlarındadır, ürün ağacında değil.
- **₺** tarafı bir **kurucu girdisidir**: abonelik/kullanım faturası tutarı
  yalnız kurucuda vardır (K ailesi — para kalemi).

**Sonuç:** bu bölüm bugün bir **yöntem beyanıdır**, rakam değildir. Kurucudan
iki girdi gelince (dönem token tüketimi + dönem faturası) hesap tek satırda
kapanır: `₺/ajan-saat = dönem faturası ÷ dönem ajan-saati`, sapma %25'i aşarsa
takvim ve kapsam kurula döner. **Bu bölüme rakam yazılmadan hiçbir kanalda
geliştirme maliyeti telaffuz edilmez.**

## Kiracı başı maliyet (10 / 50 / 100 kiracı)

"Sabit altyapı tabanı (Hetzner CX33 476 + Backblaze 100 GB 29 + alan adı 49/ay) = ~554 TL/ay, KDV hariç. Paydaya bölünmüş hali: 10 kiracı → 55,4 TL/ay · 50 kiracı → 11,1 TL/ay · 100 kiracı → 5,5 TL/ay. Üzerine kiracı başı DEĞİŞKEN kalemler eklenir: SMS 200 adet/ay ≈ 34 TL (KDV+ÖİV dahil, Netgsm standart), e-posta <1 TL, kur verisi bugün 0 TL (ticari geçişte ~9 TL/kiracı, 100 kiracıda). Sonuç — çekirdek kiracı başı toplam: 10 kiracı 89 TL/ay · 50 kiracı 45 TL/ay · 100 kiracı 40 TL/ay (KDV hariç, destek HARİÇ). Uyum+ kademesinde yaptırım taraması eklenince (50 sorgu/ay × 5,61 TL = 281 TL) kiracı başı toplam 100 kiracıda ~321 TL/ay'a çıkar. e-Fatura kontörü (165-570 TL/ay) ve mali mühür (47 TL/ay) MÜKELLEFE aittir, bizim maliyetimiz değildir — paket içine alınırsa kiracı başı maliyete eklenir. DESTEK satırı OLCULMEDI olduğu için bu toplamların DIŞINDADIR; gerçek kiracı başı maliyet destek dahil edilince yükselecektir. 100 kiracının tek CX33'e sığdığı ÖLÇÜLMEMİŞ bir VARSAYIMDIR (FAIL-CLOSED 5) — F-4 telemetrisi ve dotnet/yuk-testi çıktısı gelmeden 50/100 kiracı sütunları TAHMİN'dir."

## Fiyat kademeleri ÖNERİSİ (ilan değil; kurucu onayı gerekir)

| Kademe | Kapsam | Aylık TL önerisi | Gerekçe |
|---|---|---|---|
| Tek Şube | 1 şube, 2-3 kullanıcı, has-cari çekirdek, stok/satış/kasa, e-Arşiv/e-Fatura entegrasyonu (kontör HARİÇ), 200 SMS/ay, günlük yedek, ücretsiz kur verisi. | 1.200-1.500 TL/ay + KDV (yıllık peşinde ~%15 indirim önerisi). ÖNERİDİR, ilan değildir. | Kiracı başı doğrudan maliyet ~39-89 TL/ay (altyapı 5,5-55 + SMS 34) + ölçülmemiş destek. 1.200 TL'de brüt marj destek hariç ~%93. FAIL-CLOSED 4 aşıldı: negatif marj yok. Alt sınır Dezy'nin 500 TL/ay tabanına göre pahalı — konumlandırma fiyat değil kapsam üzerinden yapılmalı. |
| Çok Şube | Sınırsız kullanıcı, 2-5 şube, şubeler arası transfer, konsolide rapor, 1.000 SMS/ay, API erişimi, öncelikli destek. | 2.500-3.000 TL/ay + KDV; ek şube başına 400-600 TL/ay + KDV. | Ek maliyet esasen SMS hacmi ve destek saati; altyapı marjinal maliyeti şube başına ~5 TL/ay. Kuyumcum.net'in 30 $/kullanıcı/ay (≈1.459 TL) modeline karşı 5 kullanıcıda yapısal üstünlük: bizde kullanıcı başı ücret yok (lisans gideri 0, csproj:10-17). Marj pozitif. |
| Uyum+ | Çok Şube + MASAK/KYC modülü, yaptırım listesi taraması (OFAC/BM/AB), KMTS, denetim izi raporları, TÜRMOB mükellef sorgulama, SLA. | 4.500-6.000 TL/ay + KDV (yaptırım taraması kontenjanlı: 50 sorgu/ay dahil, aşımı sorgu başı yansıtılır). | Tek gerçek değişken maliyet kalemi yaptırım taraması: OpenSanctions SaaS 0,10 €/sorgu = 5,61 TL → 50 sorgu/ay ≈ 281 TL/kiracı/ay. Toplam kiracı başı maliyet ~370 TL. 4.500 TL'de marj ~%92, ama sorgu kotası AÇIK UÇLU bırakılırsa marj negatife dönebilir (FAIL-CLOSED 4) → kota zorunlu. Ücretsiz resmî listelerle (OFAC/BM/AB, 0 TL veri) kendi eşlememizi yazarsak bu 281 TL sıfırlanır; karar ekip oyuna gitmeli. |

## Rakip kıyası (yalnız birincil kaynak)

| Rakip | Fiyat | Kanıt | Güven |
|---|---|---|---|
| Dezy | Başlangıç 500 TL/ay · Profesyonel 1.000 TL/ay · Kurumsal 2.500 TL/ay. KDV durumu sayfada BELİRTİLMEMİŞ. İlk 3 ay ücretsiz, kurulum/eğitim/veri taşıma ücretsiz. | https://dezyapp.com/sss/ (erişim 2026-09-16) | ORTA — birincil site (SSS), KDV belirsiz |
| Kuyumcum.net | Standart 30 $/kullanıcı/ay · Barkodlu 35 $ · RFID'li 40 $ — KDV HARİÇ. 30 $ ≈ 1.459 TL (TCMB 15.09.2026). Ayrıca tek seferlik kurulum ücreti var, tutarı sayfada yok. | https://kuyumcum.net/TR/Fiyatlar/ (erişim 2026-09-16) | KAYNAKLI — birincil site, KDV durumu açık |
| Mikro Jump | Ana paket 52.650 TL (yenileme 21.950 TL/yıl) + e-Fatura/e-Arşiv modülü 51.900 TL (yenileme 21.600 TL) + Perakende modülü 15.200 TL. Yıl-1 ≥ 104.550 TL, sonraki yıllar ≥ 43.550 TL. KDV durumu çekilen metinde belirtilmemiş. | https://www.mikro.com.tr/fiyat-listesi/ (erişim 2026-09-16) | ORTA — birincil fiyat listesi, KDV belirsiz, kuyumcu paketi ayrı listelenmiyor |
| M&L | DOĞRULANMALI — bu sentezde birincil URL + erişim tarihi elimde yok. PAZAR-ARASTIRMASI-2026-09-16.md'de 94.686,48 TL peşin rakamı geçiyor ancak FAIL-CLOSED 3 gereği birincil kaynak teyidi olmadan kıyasta kullanılmadı. | (birincil kaynak yok — Eray/Kurucu doğrulamalı) | DOĞRULANMALI |
| CetaSoft | DOĞRULANMALI — 40.000 TL + KDV rakamı PAZAR-ARASTIRMASI'nda geçiyor, birincil sayfa teyidi bu turda yapılmadı. | (birincil kaynak yok) | DOĞRULANMALI |
| KuyumHub | BULUNAMADI — arama sonuçlarında birincil site/fiyat çıkmadı. Rakam YAZILMADI (Madde 1). | (yok) | YOK — DOĞRULANMALI |
| Terminal Gold | DOĞRULANMALI — 900-9.000 $ bandı PAZAR-ARASTIRMASI'nda geçiyor; birincil sayfa ve kapsam (tek seferlik mi abonelik mi) teyit edilmedi. | (birincil kaynak yok) | DOĞRULANMALI |

## Kurucunun tasarruf hedefi — dürüst okuma

"Tek bir 'silme oranı' YOKTUR; rakip bazlı okuma (varsayım: Tek Şube kademesi 1.200 TL/ay + KDV, 36 ay ufku, KDV hariç kıyas): (1) Mikro Jump'a karşı — yıl-1 ≥104.550 TL vs bizde 14.400 TL → ~%86 tasarruf; 36 ayda ≥191.650 TL vs 43.200 TL → ~%77. En güçlü iddiamız bu, çünkü Mikro'da e-Fatura modülü ayrı satılıyor. (2) Kuyumcum.net'e karşı — 3 kullanıcı × 30 $/ay ≈ 4.378 TL/ay vs 1.200 TL → ~%73; kullanıcı sayısı arttıkça fark büyür (bizde kullanıcı başı ücret yok). 1 kullanıcıda fark ~%18'e iner. (3) Dezy'ye karşı — TASARRUF YOK, TERSİ: Dezy Başlangıç 500 TL/ay, önerimiz 1.200 TL/ay → %140 DAHA PAHALI. Dezy Profesyonel 1.000 TL/ay'a karşı da pahalıyız. Dürüst cümle: Dezy'yi fiyatla silemeyiz. (4) M&L'in 94.686,48 TL peşin bedeline karşı iddia edilen ~%85 oranı bu sentezde DOĞRULANAMADI: birincil kaynak URL'i yok (FAIL-CLOSED 3) ve aritmetiği de tutmuyor — 94.686 TL'ye karşı 36 ayda 43.200 TL ödeme ~%54 tasarruf verir, %85 için aylık fiyatın ~400 TL olması gerekir ki o seviye Dezy'nin de altıdır. %85 rakamı ilan edilmemeli. SONUÇ: 'rakip programları memleketten silecek fiyat' hedefi bugünkü kanıtla ancak yüksek bedelli kurumsal ERP'lere (Mikro, muhtemelen M&L) karşı savunulabilir; ucuz bulut rakiplere (Dezy) karşı savunulamaz. Kurucuya dürüst öneri: silme iddiasını fiyattan değil, TOPLAM SAHİP OLMA MALİYETİnden kur — kullanıcı başı ücret yok (lisans 0 TL, csproj kanıtlı), modül başı ücret yok, kurulum ücreti yok, KDV durumu açık ilan edilir (rakiplerin çoğu belirtmiyor)."

## Entegratör önerisi

Üçünden YAZILI teklif alınsın, sıra: (1) Paraşüt — tek birincil ilanlı ucuz kontör (3,30 TL/kontör, 200'lük paket), giriş 1.800 TL+KDV tek sefer, taahhüt yok, 12 ay geçerlilik; kuyumcu ölçeğinde (50 fatura/ay) yıl-1 ≈ 3.780 TL+KDV ile en düşük. (2) QNB eFinans — ikincil verilerde en ucuz bant iddiası (200 kontör 75-170 TL) ama fiyat yayımlamıyor; teklifle doğrulanırsa Paraşüt'ü de altına indirebilir, bu yüzden mutlaka teklif alınmalı. (3) eLogo/Logo — pazarın kurumsal referansı ve kontörü SÜRESİZ (tek tedarikçide bu var), ama 11,40 TL/kontör ile ~3,5 kat pahalı; yalnız yüksek hacimli/kurumsal kiracı için ikinci kanal. Teklifte sorulacaklar: kontör birim bedeli, aylık taban ücret, e-Arşiv/e-İrsaliye farkı, ALINAN faturanın kontör düşüp düşmediği (eLogo ve QNB'de düşüyor — kuyumcunun toptancı faturaları da ücretli), kontör devri/geçerlilik, iptal şartı, kurulum ve API/Connect lisans bedeli, TÜRMOB sorgu kontörü. Sözleşme ve ödeme Kurucu'ya aittir.

## Kurucuya imza/para kalemleri

- Üç entegratörden YAZILI kontör teklifi (Paraşüt, QNB eFinans, eLogo) — sözleşme ve ödeme imzası Kurucu'da; ajan teklif talebi taslağını hazırlar, imzalamaz.
- Mali mühür / e-imza satın alımı: Kamu SM MÜS 1.700 TL+KDV (3 yıl) veya NES 1.400-1.950 TL+KDV. Karar: kiracı kendi alır (önerilen, mükellefe ait) mı, biz aracılık mı edeceğiz — imza Kurucu'da.
- OpenSanctions ticari lisans/SaaS sözleşmesi (0,10 €/sorgu veya yayımlanmamış self-host veri lisansı) — teklif talebi ve imza Kurucu'da; alternatifi ücretsiz resmî listeler + kendi geliştirmemiz.
- Kur verisi ticari sözleşmesi: bugün Truncgil/Harem/AAB sözleşmesiz ve izinsiz kullanılıyor. altinapi.com (~19 $/ay) veya doğrudan sağlayıcıyla yazılı izin — ticari SaaS için hukuki zorunluluk.
- Bulut hesabı açılışı ve ödeme yöntemi: Hetzner/DigitalOcean + Backblaze + alan adı (.com ~584 TL/yıl, .com.tr ~72 TL/yıl) — kart/kurumsal fatura Kurucu'da; KDV-2 yükümlülüğü teyidi.
- SMS sağlayıcı hesabı ve başlık (sender ID) başvurusu: Netgsm veya İleti Merkezi — başlık onayı kurumsal evrak ister, Kurucu imzası gerekir.
- Rakip fiyat doğrulaması: Dezy, M&L, CetaSoft, KuyumHub, Terminal Gold birincil sayfalarından KDV durumuyla teyit — 'silme oranı' iddiasının tek meşru dayanağı.
- Fiyat ilanı kararı: F-8 Faz B kapanmadan hiçbir kanalda rakamlı fiyat açıklanmaz (MÜHÜR 2). Kademe rakamlarının ilanı Kurucu onayı + Tamer Bey mührü ister.

## DOĞRULANMALI

- QNB eFinans kontör birim fiyatı: iki ikincil kaynak çelişiyor (200 kontör 75 TL vs 50-200 kontör 170 TL KDV dahil). QNB birim fiyat yayımlamıyor → yazılı teklif şart.
- Turkcell (5.250-21.000 TL / 1.000-5.000 kontör) ve Logo İşbaşı (463/738 TL/ay) rakamları ikincil (eticaretradari.com); birincil sayfadan teyit edilmeli, KDV durumu belirsiz.
- M&L 94.686,48 TL peşin, CetaSoft 40.000 TL+KDV, Terminal Gold 900-9.000 $ rakamları PAZAR-ARASTIRMASI-2026-09-16.md'den; bu sentezde birincil URL+erişim tarihi elimde değil → FAIL-CLOSED 3 gereği kıyasta kullanılmadı.
- KuyumHub fiyatı bulunamadı; birincil site doğrulanmalı. Dezy 500/1.000/2.500 TL/ay ve Mikro fiyatlarında KDV durumu sayfada yazmıyor → yazılı teyit.
- Yerli VPS fiyatları ALINAMADI: Turhost sayfası kesik, Hostinger TR 403, Natro ayrıştırma hatalı (242.744,99 TL anlamsız). Rakam yazılmadı; tarayıcıdan okunmalı.
- 100 kiracının tek Hetzner CX33'e sığdığı ÖLÇÜLMEDİ (FAIL-CLOSED 5). dotnet/yuk-testi çıktısı ve F-4 kiracı etiketli telemetri gelmeden kapasite iddiası yazılamaz.
- Canlı SQL ölçümleri (tenants COUNT, information_schema disk, hareketler/tenant_id) bu sentezde koşulmadı — görev salt okunur ve derleme/test yasak. Faz A yazımında koşulacak.
- Hetzner Object Storage 6,49 €/ay rakamı ikincil kaynaktan (european-alternatives.eu); resmî sayfa çekilmedi. Amazon SES 0,10 $/1.000 rakamı da ikincil (smtpedia.com).
- altinapi.com 19 $/ay rakamı arama özetinden; /pricing sayfası çekilemedi. Truncgil/Harem/AAB için yazılı lisans, oran sınırı ve ticari kullanım izni YOK → F-9.
- Mali mühür teslim süresi hiçbir birincil Kamu SM sayfasında rakamla verilmemiş; müşteri kabul takvimi için sorulmalı.
- Yurt dışı bulut faturalarında KDV-2 (sorumlu sıfatıyla %20) yükümlülüğü — Mustafa/FinOps teyit etsin; TL rakamlarına eklenmedi.
- eLogo Connect lisansı kontör listesinin dışında ve tutarı PDF'de yok; entegratörlerin kurulum/API erişim ücreti hiçbir birincil listede ayrı kalem değil → teklifte sorulacak.
- Destek saati ve saatlik maliyet OLCULMEDI: kiracı başı aylık destek dakikası hiç ölçülmedi, ilk 3 ay yoğunluğu bilinmiyor.
- OpenSanctions self-host (yente) veri lisans bedeli yayımlanmıyor → satışa sorulmalı; SaaS 0,10 €/sorgu ile kıyaslanacak.

## Riskler

- FAIL-CLOSED 1 AKTİF: destek ve kiracı başı kaynak tüketimi satırları ÖLÇÜLMEDİ → hiçbir kanalda rakamlı fiyat ilan edilemez. Bu belge kilidi açmaz, kilidi kayda geçirir.
- Kur verisi hukuki risk: Truncgil/Harem/AAB sözleşmesiz, oran sınırı ve ticari kullanım izni yayımlanmamış. Topluluk 'Harem API' çözümleri izinsiz kazımadır. Ticari SaaS'ta dava/kesinti riski — F-9 önceliklendirilmeli.
- Kontör tuzağı: eLogo ve QNB'de ALINAN e-Fatura da kontör düşer. Kuyumcu toptancıdan aldığı faturalar için de öder. Pakete 'e-Fatura dahil' yazılırsa maliyet tahmin edilenin 2 katına çıkabilir.
- Yaptırım taraması marj yiyicisi: Uyum+ kademesinde sorgu kotası açık uçlu bırakılırsa 5,61 TL/sorgu ile marj negatife dönebilir (FAIL-CLOSED 4). Kota zorunlu ya da kendi eşlememizi yazmalıyız.
- Hetzner 2026'da iki zam yaptı (1 Nisan tümü; 15 Haziran paylaşımlı %30-38, adanmış %113-175). Altyapı satırına yıllık zam bandı konmalı; CX23/CX33 stok yokluğu da raporlandı.
- Kapasite iddiası çürükse tüm birim ekonomi çöker: 100 kiracı/tek sunucu varsayımı yanlışsa kiracı başı altyapı 5,5 TL'den 55 TL'ye (10 kiracı) çıkar — yine de marj pozitif kalır, bu riskin şiddeti düşük.
- Dezy 500 TL/ay tabanı fiyat rekabetini kapatıyor: 1.200 TL/ay önerisi Dezy'nin altına inemez. Farklılaşma has-cari doğruluğu, MASAK/KMTS uyumu ve KDV şeffaflığı üzerinden yapılmalı — salt fiyatla 'silme' mümkün değil.
- KDV-2 (sorumlu sıfatıyla %20) yurt dışı bulut faturalarında maliyeti %20 artırabilir; TL rakamlarına eklenmedi.
- On-prem dağıtımda MySQL'i ürünle paketlersek GPL/ticari lisans sorusu doğar; MariaDB da GPLv2 olduğu için çözüm değil. Müşteri kendi kurmalı ya da yalnız bulut sunulmalı.
- Yerli VPS fiyatları alınamadı; veri yerelleşmesi (KVKK/MASAK) gerekçesiyle TR barındırma zorunlu çıkarsa altyapı satırı yeniden hesaplanmalı ve muhtemelen pahalılaşır.

## Araştırma kaynakları

- [eLogo e-Çözümler tavsiye edilen fiyat listesi (PDF, 9 Eylül 2026)](https://cdn.logo.com.tr/files/logocomtr/Uploads/Documents/elogo-ecozumler-fiyat-listesi.pdf) — güven: YÜKSEK — birincil
- [Paraşüt ön muhasebe / e-fatura fiyatları](https://www.parasut.com/on-muhasebe-fiyatlari) — güven: YÜKSEK — birincil
- [Kamu SM Mali Mühür fiyatlandırma](https://mm.kamusm.gov.tr/fiyatlandirma.jsp) — güven: YÜKSEK — birincil
- [Kamu SM NES (e-imza) fiyatlandırma](https://kamusm.bilgem.tubitak.gov.tr/urunler/nitelikli_elektronik_sertifika/fiyatlandirma.jsp) — güven: YÜKSEK — birincil
- [Dezy SSS (paket fiyatları)](https://dezyapp.com/sss/) — güven: ORTA — birincil, KDV belirsiz
- [Kuyumcum.net fiyatlar](https://kuyumcum.net/TR/Fiyatlar/) — güven: YÜKSEK — birincil
- [Mikro Yazılım fiyat listesi (Mikro Jump)](https://www.mikro.com.tr/fiyat-listesi/) — güven: ORTA — birincil, KDV belirsiz
- [Yengeç — e-Fatura kontör paketleri (eLogo/QNB/Trendyol)](https://yengec.co/e-fatura-kontor-paketleri-fiyatlari/) — güven: DÜŞÜK — ikincil
- [EticaretRadarı — kontör fiyatları sağlayıcı listesi 2026](https://eticaretradari.com/e-fatura-kontor-fiyatlari/) — güven: DÜŞÜK — ikincil
- [Logo özel entegratör kontör kampanyası (bu turda çekilmedi)](https://www.logo.com.tr/logo-kampanyalar/logo-ozel-entegrator-kontor-kampanyasi) — güven: DOĞRULANMALI
- [TCMB günlük kurlar (today.xml) — 15.09.2026](https://www.tcmb.gov.tr/kurlar/today.xml) — güven: yüksek
- [Hetzner Docs — 15 Haziran 2026 fiyat ayarı (CX23/CX33/CX43/CCX13)](https://docs.hetzner.com/general/infrastructure-and-availability/price-adjustment/) — güven: yüksek
- [DigitalOcean Droplet fiyatları](https://www.digitalocean.com/pricing/droplets) — güven: yüksek
- [DigitalOcean Managed MySQL fiyat dokümanı](https://docs.digitalocean.com/products/databases/mysql/details/pricing/) — güven: yüksek
- [Bytebase — MySQL hosting fiyat karşılaştırması 2026 (DO 4 GiB plan 60,90 $)](https://www.bytebase.com/blog/mysql-hosting-options-pricing-comparison/) — güven: orta
- [Hetzner Object Storage (resmî ürün sayfası; rakam ikincil kaynaktan)](https://www.hetzner.com/storage/object-storage/) — güven: orta
- [Backblaze B2 fiyatlandırma](https://www.backblaze.com/cloud-storage/pricing) — güven: yüksek
- [İnetmar domain fiyatları 2026 (.com 12 $, .com.tr 1,49 $)](https://www.inetmar.com/domain/domain-fiyatlari/) — güven: yüksek
- [Natro .com.tr kayıt](https://www.natro.com/domain-sorgulama/com-tr-domain-kayit) — güven: yüksek
- [Karekod — Domain fiyatları 2026 (.com 500-600 TL bandı)](https://www.karekod.org/blog/domain-fiyatlari/) — güven: orta
- [Truncgil Finans API ana sayfa (ücretsiz; lisans/oran sınırı yok)](https://finans.truncgil.com/) — güven: orta
- [TCMB EVDS kullanıcı dokümanı](https://evds2.tcmb.gov.tr/index.php?%2Fevds%2FuserDocs=) — güven: yüksek
- [altinapi.com — planlar ve oran sınırları](https://altinapi.com/) — güven: orta
- [Harem Altın canlı piyasalar (resmî API yok)](https://canlipiyasalar.haremaltin.com/) — güven: orta
- [OpenSanctions API — 0,10 €/sorgu](https://www.opensanctions.org/api/) — güven: yüksek
- [OpenSanctions veri lisansı (CC BY-NC / ticari)](https://www.opensanctions.org/licensing/) — güven: yüksek
- [OpenSanctions — SaaS API mi yente self-host mu](https://www.opensanctions.org/faq/api/license-cost/) — güven: yüksek
- [Sphinx — 2026 yaptırım tarama yazılımları (Dow Jones/LexisNexis kurumsal maliyet)](https://sphinxhq.com/blog-posts/best-sanctions-screening-software-2026) — güven: düşük
- [Netgsm toplu SMS fiyatları](https://www.netgsm.com.tr/fiyatlar/toplu-sms) — güven: yüksek
- [İleti Merkezi toplu SMS fiyatları](https://www.iletimerkezi.com/toplu-sms-fiyatlari) — güven: yüksek
- [Amazon SES fiyatı (ikincil özet; resmî sayfa fetch edilmedi)](https://smtpedia.com/amazon-aws-ses-pricing/) — güven: orta
- [MySQL ticari lisans (OEM/ISV) — GPL sınırı](https://www.mysql.com/about/legal/licensing/oem/) — güven: yüksek
- [MariaDB lisans SSS (Server GPLv2, bağlayıcılar LGPL)](https://mariadb.com/docs/general-resources/community/community/faq/licensing-questions/licensing-faq) — güven: yüksek
- [Natro sunucu kiralama (fiyat ayrıştırması hatalı — doğrulanamadı)](https://www.natro.com/sunucu-kiralama) — güven: düşük
- [Turhost VPS TR (sayfa kesik döndü — fiyat alınamadı)](https://www.turhost.com/sunucu/vps-tr-sunucu/) — güven: yok
- [HostAdvice — Hostinger VPS fiyatları (KVM1 5,99 $/ay, üçüncül)](https://tr.hostadvice.com/hosting-company/hostinger-reviews/vps-pricing/) — güven: düşük
- [Northflank — Hetzner 2026 zam dökümü (bağlam)](https://northflank.com/blog/hetzner-cloud-server-price-increases) — güven: orta