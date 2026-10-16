# Mihenk — Kurucudan ve Mali Müşavirden Beklenenler

**Tarih:** 1 Ekim 2026 · **Hazırlayan:** Tamer Bey (orkestratör) · **Durum:** kod tarafı sürüyor; aşağıdakiler bizim çözemeyeceğimiz, sizin kararınızı ya da mali müşavirin teyidini bekleyen maddeler.

> **Nasıl okunur:** Her maddede *ne istiyoruz*, *neden*, *önerimiz* ve *siz karar verene kadar program ne yapıyor* yazıyor. Teyit gelmeyen yasal maddelerde program **tutucu** davranır (yasağı uygular, uyarır); hiçbir şey sessizce gevşetilmez.

---

## BÖLÜM A — KURUCU (Ramazan Bey)

### A1. Bugün yayını bekleten tek madde

| # | Karar | Neden | Seçenekler | Önerimiz |
|---|---|---|---|---|
| A1 | **Demo süresinin kronometreyle ölçülmesi** (denetim P-30) | Ara kapı 01.10 05:40'ta geçti (derleme 0/0, 8444/8445 test, denetimde 302 ✓). Tek ✗ bu: 15 dk demo süresi 23.09'da yalnız *tahmin* yazılmıştı; kural tahmini 30.09'a kadar kabul ediyordu. Kuralı biz gevşetmedik, bu yüzden **5199'a yayın yapılamıyor**. | **(a)** `docs/RUNBOOK-DEMO.md`'deki 10 adımlık demoyu bir kez kronometreyle oynatırsınız, her adımın başlama/bitiş saatini söylersiniz, biz kayda işleriz. **(b)** Yazılı olarak yeni bir son tarih verirsiniz (ör. 07.10); kapı uyarıyla geçer. | (a) — 15-20 dk sürer, kalıcı çözüm. |

### A2. Satışı doğrudan etkileyen kararlar (kurul 5'in bulduğu)

| # | Karar | Neden | Önerimiz |
|---|---|---|---|
| A2 | **Fiyat (K-16)** | Rakamlı fiyat ilan edilemiyor; kurulda satış ve maliyet puanını düşüren ana sebep. | İlk 10 dükkâna 12 ay sabit "kurucu pilot" fiyatı: **600–750 TL/ay + KDV** (kurulum ve taşıma dahil). |
| A3 | **Destek kanalı ve yanıt süresi (K-19)** | Müşteri "arayınca kim açar?" diye soruyor; sözleşmede yanıt süresi yok. | Mesai içi telefon/WhatsApp hattı + yazılı ilk yanıt süresi (ör. 4 iş saati). |
| A4 | **Kurulum yeri (K-6 / K-9)** | Gerçek bir Linux sunucuya ve Windows tezgâh bilgisayarına hiç kurulum yapılmadı; kurulum denemesi planda ama hangi sunucu/alan adı/bilgisayar kullanılacak sizin kararınız. | 1 küçük Linux VPS + 1 Windows 10/11 bilgisayar (deneme için); alan adı. |
| A5 | **e-Fatura / e-Arşiv konumu** | Entegrasyon yok; e-belge mükellefi dükkân bugün bizi seçemez. | 1.0 sözleşmesinde açıkça "e-belge hariç"; paralelde **3 entegratörden** (ör. Paraşüt, QNB eFinans, Logo) teklif istensin. |
| A6 | **Yedek anahtarını kim saklar (K-18)** | Şifreli yedeklerin anahtarı kaybolursa yedek açılamaz. | Anahtar sizde + kasada kâğıt kopya. |

### A3. Veri ve izinler

| # | Karar | Durum | Önerimiz |
|---|---|---|---|
| A7 | **Demo dükkânındaki 4 bozuk müşteri kaydı** (Fatma Şahin, Ali Veli Çelik, Zeynep Koç + "Demo Kuyumculuk / Kurban Musteri") | Kök neden bulundu: 9 Eylül'de **eski kodla** yazılmış veri; bugünkü kodla oluşmuyor (46 adımlık sıfırdan müşteri testi temiz). Silme talimatınızı uygulamak istedik ama **Claude Code'un otomatik izin denetçisi veritabanı silme işlemini durdurdu**. | (a) Komutları biz hazırlayalım, siz terminalde çalıştırın (önce yedek alınır); ya da (b) Claude Code ayarlarına bu tür veritabanı işlemleri için izin kuralı ekleyin. |
| A8 | **Binlog saklama süresi (2 gün)** | Disk dolmasın diye çalışma anında ayarlandı; Docker yeniden başlarsa eski değere döner. | docker-compose'a kalıcı ayar olarak yazılsın (onayınızla). |
| A9 | **Boş (0 bayt) artık dosyaların silinmesi** | Silme yetkimiz yok. | Siz silin: `dotnet/mb-MB1.log`, `mb-MB2.log`, `mb-MB3.log`, `dotnet/denetim.sh.mutbak`, `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/` altındaki 8 adet 0 baytlık `.png`, `/tmp/k2fix.cs`. |
| A10 | **Kodun GitHub'a gitmesi / commit yasağı** | Bulut oturumlarıyla şerit sayısı Mac işlemcisinden bağımsız artardı (süre yaklaşık yarıya iner); ama kod özel bir depoya gönderilmeli. Şu an "commit yok" kuralı geçerli. | İsteğe bağlı. Karar sizin. |

### A4. İş kuralı kararları (ürün)

| # | Soru | Bugün program | Önerimiz |
|---|---|---|---|
| A11 | Önceki borç (devir) bakiyesinden nakit tahsilat serbest mi? | Tutucu | Geçsin, iz bıraksın. |
| A12 | MASAK otomatik maskeleme (saklama süresi dolan kimlik verisi) | Önizleme | Önce 90 gün yalnız önizleme, sonra açılsın. |
| A13 | Yaptırım listesi kaynağı | Resmî ücretsiz liste | Başlangıçta yeterli; ücretli sağlayıcı sonra. |
| A14 | Borsa İstanbul kapanış verisi lisansı (KDV 23/e has bedeli için tebliğ şartı) | Piyasa kuru kullanılıyor, 805 kodu kapalı | Kur verisi lisansıyla birlikte değerlendirilsin. |
| A15 | Yasal parametre tablosunun (eşikler, bayramlar) yıllık bakımı kimin işi? | — | ByGatech (sistem ucu yalnız bizde). |
| A16 | Peşin Müşteri art arda fiş penceresi | 60 dk | 60 dk. |
| A17 | Mehmet (güvenlik) onayı: şubeye kilitli tezgâhtarda yasal nakit/MASAK toplamının **tüm şubeleri** görmesi | Uygulandı (yasa gereği kişi bazında toplam) | Onaylansın. |
| A18 | **Tamamen altın cinsinden veresiye** (TL tutarı 0) açılamıyor; program en az 0,01 TL istiyor | Engelli | Açılsın mı? (Kuyumcuda yaygın; öneri: açılsın.) |
| A19 | **Yanlış girilen hurda alışı ve takas geri alınamıyor** | Geri alma yolu yok | Geri alma (ters kayıt) eklensin mi? Öneri: evet, gider pusulası iptal kuralıyla birlikte. |
| A20 | **E-posta gönderme kanalı** ("Şifremi unuttum" için) | Özellik hazır ama üretimde SMTP/e-posta servisi yok; kanal yokken bağlantı gösterilmiyor, sahibine/desteğe yönlendiriyor | Bir e-posta servisi seçilsin (ör. kurumsal SMTP ya da Amazon SES/Brevo); kurulumda tanımlanır. |

---

## BÖLÜM B — MALİ MÜŞAVİR

> Program bu maddelerde **tutucu** çalışıyor ve ekranda "mali müşavirinizle teyit edin" diyor. Cevaplar geldiğinde yalnız yasal parametre tablosu/sözlük güncellenir, kod değişmez.

### B1. Önceki listeden (MALI-MUSAVIR-SORULARI.md, S-1…S-10) — henüz cevap kaydı yok

| # | Soru (özet) |
|---|---|
| S-1 | Eski altınla kısmi ödemede satış faturası **brüt mü net mi**? |
| S-2 | MASAK kimlik tespiti eşiği bugün kaç TL? (Programda 185.000 TL — Tedbirler Yönetmeliği md.5/1-b okundu, doğrulandı.) |
| S-3 | KDV özel matrah için **805 kodu** kullanılsın mı? |
| S-4 | Hurda alımında **stopaj oranları** (esnaf muaflığı %2, nihai tüketici %0) doğru mu? |
| S-5 | Gider pusulası **7 gün** kuralı ve VUK md. 234'ün güncel metni; satıcı adresi/mal açıklaması sonradan düzeltilebilir mi? |
| S-6 | **Tevsik** zorunluluğu takas/trampada (ayni ödemede) nasıl uygulanır? |
| S-7 | Eski altın alımında hangi kur: Kuyumcular Odası mı, Borsa İstanbul mu, dükkânın serbest fiyatı mı? |
| S-8 | MASAK risk temelli yaklaşımda bağlayıcı skor/tedbir hükmü var mı? |
| S-9 | Has/ayar bazlı takibin muhasebedeki yeri (153 alt hesapta miktar takibi mi?). |
| S-10 | Dönem sonu değerleme (bilgi). |

### B2. Yeni sorular (30.09–01.10, yasal paketlerden)

| # | Soru | Program bugün |
|---|---|---|
| M-1 | **Nakit İşlem Beyan Formu alt sınırı:** tebliğ metni "ikimilyonbir TL" (2.000.001) diyor; program 2.000.000,01 TL'den başlıyor. Hangisi uygulanmalı ve bu yükümlülük kuyumcuya uygulanır mı? | 2.000.000,01 (daha tutucu), TEYİTSİZ |
| M-2 | MASAK **200.000 TL amaç beyanı** ve **2 M / 20 M** kademeleri kuyumcuya uygulanır mı? Dayanak metin hangisi? | Zorunlu (tutucu), TEYİTSİZ |
| M-3 | MASAK **"bağlantılı işlem"** yorumu: aynı gün + aynı kişi yeterli mi; şubeler ayrı mı sayılır? | Aynı yerel gün + aynı kişi, tüm şubeler birlikte |
| M-4 | Taksitli satışta tevsik haddi: **işlem (sözleşme) günü** mü, **ödeme günü** mü? | İşlem günü |
| M-5 | Tevsik ihlalinin **kendiliğinden bildiriminin** biçimi ve kanalı (vergi dairesi?); tahsilat yönündeki ihlalde kurtarma yolu var mı? | Panelde son gün uyarısı + "mali müşavirinize danışın"; dilekçe TASLAK damgalı |
| M-6 | Takasta tevsik için **Max(eski, yeni)** tutucu yorumu doğru mu? | Max(eski, yeni) |
| M-7 | Yaptırım eşleşmesinde "işlemi ertele + MASAK'a ivedilikle bildir": **bildirim biçimi** (MASAK Online / yazı)? | Kilit + tarih/evrak no ile kapatma kaydı |
| M-8 | **805 kodunun açılması** ve has bedelinde "son işgünü kapanışı"nın yorumu (aynı gün mü önceki gün mü?). | 805 kapalı |
| M-9 | **Gümüşte (23/g) özel matrah** uygulanır mı? Platin/paladyumda has bedeli düşülür mü? | Yalnız altında özel matrah; gümüş/platin/paladyumda KDV tam (tutucu) |
| M-10 | İptal edilen gider pusulasının yerine **yeniden düzenleme** usulü. | Yeniden düzenleme akışı YASAL-4'te yazılıyor, tutucu |
| M-11 | Hurda stopajının **beyan dönemi**: aylık mı üç aylık mı (muhtasar 23'ü/26'sı)? Kiracı ayarı olarak mı sorulsun? | Ayar olarak sorulacak |
| M-12 | **STOPAJ_ESNAF_MUAF_HURDA_ORAN** (2009/14592 BKK 13. bent) sonradan değişti mi? | TEYİTSİZ, bugünkü değer korunuyor |
| M-13 | **KMTS 12.08.2026 değişikliği** perakende kuyumcuya yükümlülük getiriyor mu? | İzleniyor; entegrasyon yok |

---

## Cevaplar nasıl işlenecek

1. Bölüm A kararlarını sohbete yazmanız yeterli (ör. "A1: a; A2: 650 TL; A7: komutları ver").
2. Mali müşavir cevapları geldiğinde metni olduğu gibi iletin; biz yasal parametre tablosuna **kaynak + tarih + DOĞRULANDI** damgasıyla işleriz. Geçmiş işlemler eski kuralla kalır (program tarihli kural tutuyor).
3. Hiçbir yasal madde teyit gelmeden gevşetilmez.
