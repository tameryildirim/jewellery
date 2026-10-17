# Mevcut proje — eksik / fazla incelemesi (17.10.2026)

> Ölçüt: `KARAR-MERKEZI-MIMARI-2026-10-17.md` + bağımsız tasarım `MIMARI-BAGIMSIZ-FABLE-2026-10-17.md`
> (Fable 5.1) + araştırma `../01-gereksinimler/arastirma/MUADIL-ARASTIRMASI-2026-10-17.md`.
> Yöntem: dört paralel salt-okuma incelemesi (para/banka/cari · belge/fatura/KDV · stok/kur/MASAK/emanet/hurda ·
> ekran/fazlalık); her bulgu dosya:satır kanıtlı. Bu belge birleşik öncelik listesidir; ham raporlar oturum
> kaydında. Kodda yalnız § 0'daki düzeltme yapıldı; geri kalanı karar bekler.

## 0. İnceleme sırasında bulunan ve HEMEN düzeltilen

| Bulgu | Düzeltme |
|---|---|
| Çek/senet banka tahsilinin satırı (borç evrak girişinde zaten düşülmüş) M7a "borcundan düşülsün mü?" listesine düşüyordu → aynı para ikinci kez düşülebilirdi (`EvrakModulu.cs:599-610`) | **M7b** (`01caf1b`): satır `CariKarari=KAPALI`, karar ucu 409, geçiş geçmiş satırları işaretler. Canlıda etkilenen satır: 0. Tam test 12.708 geçti; 1 kararsız test (PosBarkod, ayrı koşuda 3/3 geçti) |

## 1. P0 — canlıda yanlış sonuç üretebilecekler (Karar Merkezi'nden ÖNCE)

| # | Bulgu | Kanıt | Önerilen düzeltme |
|---|---|---|---|
| 1 | **Havaleden "Faturaya dönüştür" yolu:** altın havalesine tüm tutardan %20 KDV'li STANDART fatura (23/e yok); "borcundan düşülmüş" ya da zaten faturalı satışın parası yeniden faturalanabilir; `FaturaServisi` dışından `Faturalar.Add`; belge tarihi dekont tarihi (geçmiş tarihli KESILDI); fatura iptali dekont bağını çözmüyor → dekont kalıcı stornolanamaz | `BankaModulu.cs:579-667`, `:584`, `:620-652`; `FaturaModulu.cs:1769-1797`; `StornoServisi.cs:571-574` | **O-0:** yol kilitlenir (ya da bekçilerle `FaturaServisi`'ne taşınır); iptal bağları çözer |
| 2 | **Mevzuat kapıları yapılandırma bayrağıyla kapatılabiliyor:** `Tevsik:Etkin`, `IslemTutariKuraliEtkin`, `Isler:Masak:Etkin` (ŞİB 10 iş günü sayacı) | `appsettings.json:98`; `TevsikKapisi.cs:77,260`; `MasakSibSuresiIsi.cs:32` | Bayrak kapalıyken sunucu açılmaz (açılış eşitlik denetimine eklenir) |
| 3 | **Kur devre kesici yok:** dış kaynaktaki hatalı sıçrama hem teklif fiyatına hem işlem bandının referansına kendiliğinden geçer; sunucuda bayat kur kapısı yok | `KurModulu.cs:880-893, 1051-1067`; `IslemDogrulayici.cs:22` | Otomatik çekimde bant; sıçramada "onay bekliyor"; satış/alışta kur yaşı kapısı |
| 4 | **Kuyumcu fatura haddi yalnız ekranda:** API'den 36.000 TL üstü satış FİŞ olarak geçer; had üstünde geçerli TCKN/VKN zorunlu değil; fatura oluşmazsa satış faturasız kalır | `Islemler.Belge.cs:26`; `SatisModulu.cs:3084, 3093, 3108-3112` | Sunucu kilidi; başarısız/taslak fatura Bekleyenler'e |
| 5 | **Has gramsız özel matrah:** API elle `HasDeger` kabul ediyor; "binde" yöntemi has gram olmadan matrah düşürüyor (mevzuatta yok — araştırma § 6.3) | `FaturaModulu.cs:291-307`; `OzelMatrahMotoru.cs:95-126` | Sunucuda reddet; "binde" mali müşavir onay bayrağı olmadan kilitli |
| 6 | **BORC kararı tahsilat kapısını atlıyor:** gün sonu kilidi ve kur doğrulaması yok, "son kur" kendiliğinden seçiliyor | `GelenOdemeModulu.cs:274-292` vs `CariModulu.cs:1080` | Ortak tahsilat çekirdeğine kasasız banka bacağı eklenir, karar oradan yazar |
| 7 | **Konsinye sahibine borç defter satırı olmadan yazılıyor;** kur kayıtları `ExecuteDelete` ile kalıcı siliniyor (yazma denetçisini atlıyor) | `SatisModulu.cs:890`; `KurModulu.cs:1339-1353` | Konsinye borcu hareket satırıyla; kur silme saklama süresi mali müşavirle, silme yerine arşiv |
| 8 | **Alıcı bilgisi faturaya dondurulmuyor:** cari kartı düzeltilince kesilmiş faturanın alıcısı geriye dönük değişir | `Entities.cs:1296` | Kesimde unvan/TCKN-VKN/adres belgeye kopyalanır |
| 9 | **Aynı para iki kez sayılabilir:** havale tahsilatı Cariler ekranından kasaya, aynı para bankaya dekont olarak da girilebiliyor | `CariModulu.cs:1116` | Havale tahsilatı banka bacağına yönlenir (Karar Merkezi A2) |

## 2. EKSİK — Karar Merkezi paketlerine düşenler

| Paket | Eksik |
|---|---|
| **A1 / O-1 altyapı** | `kararlar` + sürümlü `otomasyon_politikalari` yok; Pano sayacı yok (12 ayrı yerde bekleyen iş; Pano 3'ünü sayıyor); "İşlenenler" + Geri al yalnız son kararda; toplu onay, "Bunu kural yap" yok. **Carisiz / benzer adlı gelen ödeme hiçbir kuyruğa düşmüyor** (`GelenOdemeModulu.cs:209`) — "eksik olay yok" ölçütüne aykırı. Otomatik IBAN bağında neden etiketi ve Geri al yok; ekstreden yanlış bağlanan satır düzeltilemiyor |
| **A2 para geldi** | Ekstre yolu M7a çözücüsünü kullanmıyor (iki çözücü, üç ad eşiği: 0,5 / 0,60 / >0). TCKN ile IBAN farklı cari gösterirse SOR yok. Seçenekler 2 (BORC/SATIS): Avans, Yeni satış, Satış dışı eksik (**Emanet seçeneği olmayacak** — emanet gram defteridir, Fable § 5). SATIS kararı hiçbir satışa bağlanmıyor; sipariş/kapora ve fişin HAVALE bacağı eşleşmesi yok. Gram veresiyeye TL düşme (kur önerili, kilitli SOR) yok. Fazla ödeme ayrılmıyor. SOR'da "Borcundan düş" birincil düğme (ön seçim yasağına aykırı). Bankadan açılan kart için yapısal `kaynak`, kimlik tespiti=HAYIR, KVKK aydınlatma bayrağı yok. MASAK'a yaklaşınca uyarı yok |
| **A3 banka** | `IBankaSaglayici`, deneme bağlayıcısı, çekim hizmeti yok (günlük anahtarlı zamanlayıcı dakikalık çekime uymaz). Ekstre yalnız CSV; MT940/Excel ve TCKN sütunu yok. Banka bakiye mutabakatı yok (ekstre açılış/kapanış alanı hiç dolmuyor) |
| **A4 belge** | e-Fatura/e-Arşiv belge tipi, ETTN, mükellef sorgusu, `IEFaturaSaglayici`, UBL-TR yok. Toplu (gün sonu) e-Arşiv onay listesi yok. Fatura çıktısı (PDF/yazdır) yok. İade faturası / faturalı satış stornosunda fatura denetimi yok. GİB numara biçimi yok. Gelen e-fatura ve işçilik belgesi (M9) yok. e-Gider pusulası, 234/5 "banka ödemesi pusula yerine geçer" notu yok |
| **A5** | Cari mutabakat mektubu yok; borç hatırlatma yalnız uygulama içi (müşteriye plan yok; SMS/WhatsApp 6563/İYS açık soru); POS komisyonu gidere yazılmıyor; gram borç kredi limitine katılmıyor |
| **Diğer (yeni)** | 459 yabancı istisnası (pasaport) yok; NFC kimlik okuma yok; RFID sayım yok (H-6 kapsam dışı beyanı); yaptırım listesi yaş uyarısı yok; Luca/Zirve şablonu yok; doğrudan stok PATCH'i sayım oturumunu atlıyor (`UrunModulu.cs:1542`) |

## 3. FAZLA — sadeliği bozan / ölü

| Ne | Ölçüm | Öneri |
|---|---|---|
| Eski temalar (yalnız Yıldız açık/koyu kalacak — kurucu kararı) | ≈2.700 satır: 8 şema belirteci ≈1.500 CSS, eski sol menü kabuğu ≈930, eski şema razor dalları ≈270; `TemaSecici` (çağıran 0). **`YildizOzgunCiz` dalları canlı — silinmez** ("Tüm ayrıntılar" görünümü) | Tema temizliği paketi: önce şemayı `yildiz`e sabitle + veri göçü (`platin`→`yildiz`), sonra sil; 21 test dosyası yeniden yazılır |
| Menü | 45 öğe → ≈27: raporlar −5 (Rapor Merkezi), yönetim −5, Kasa+Vezne+Kasa raporu tek ekran −2, POS −1, Evrak+Vade takvimi −1, Yasal sınırlar→Uyum −1; Hurda/Gider pusulası grup ve ad karışıklığı | Ekran birleştirme paketi |
| Ölü bileşen / dosya | `DuzenlenebilirListe` 126, `Form/TarihAralik` 116, `Form/Anahtar` 49, `Form/Vkn` 28, `Form/Adres` 21 (web'de çağıran 0); 0 baytlık `Modules/Stok/Yerlesim*.cs`, `Islemler.YasalParam.cs.tmp` | Testleriyle birlikte sil |
| Çağıranı olmayan uçlar | `evraklar/cari/{id}` (müşteri kartında evrak kutusu — aslında EKSİK), `ekstre/satir/{id}/oneriler` (A1'e bağlanmalı), `evraklar/ara`, `cari/kasasiz-tahsilatlar`, `pos/cihazlar/{id}/kasa-gocleri`, `gider-pusulalari/{id}/yazdirildi`, `kmts/uymayanlar` | İlk ikisi bağlanır, gerisi değerlendirilir |
| Çift yol | İki müşteri çözücü; banka→fatura (FaturaServisi dışı); doğrudan stok PATCH; görünüm tercihi 4 katmanda; "binde" ikinci matrah formülü; Fatura `ODENDI` durumu (ödeme ≠ belge); `FaturaNo` alanının iki anlamı | İlgili paketlerde teke indirilir |
| Ekran sınırı | Müşteri seçici 200 kayıtla sınırlı (`BankaEkstre.razor:322`, `Banka.razor:1502`) — 200'den sonraki müşteri görünmüyor | Arama tabanlı seçici (A1) |

## 4. Mimari birleşimi (Fable bağımsız tasarımı ile)

Fable 5.1'in bağımsız tasarımı taslağımızla iskelette örtüşüyor; § 12.3'teki 10 eleştiri kanıtla kontrol edildi ve
**kabul edilmesi önerilir**: tekillik/eşzamanlılık, sürümlü ve kapsamlı politika (CARİ > HESAP > KİRACI, kademe yalnız
düşer), kilidin üç katmanlı zorlanması (400 + uygulayıcı + denetim.sh sayacı), otomatik akışın önce sahneye
(`ekstre_satirlari`) düşmesi, "Emanet" TL seçeneğinin çıkarılması, BORC kararında fatura adımının hiç açılmaması,
çoklu borçta mahsup tablosu sorusu, banka web servisinde çift anahtar (sistem izni + kiracı beyanı) ve kodla salt-okur
kanıtı, M7a göçünün A1'e alınması, **O-0 hotfix'inin her şeyden önce gelmesi**. Fable'ın tek doğrulanamayan kalemi
(MASAK tohum değeri) orkestratörce doğrulandı: 370.000 TL, geçiş `MasakKimlikEsigi370`.

## 5. Önerilen sıra

1. **O-0 / P0** (§ 1, madde 1–9) — canlı doğruluk; kurucu onayıyla.
2. **A1 = O-1** Karar Merkezi altyapısı + M7a göçü + carisiz ödeme kuyruğu + arama tabanlı seçici.
3. **A2 = O-3…O-7** para geldi akışının tamamı.
4. **A3 = O-2** sahne + bağlayıcı + deneme; **O-10** gerçek banka yalnız avukat görüşü + kiracı beyanıyla.
5. **A4 = O-8** belge (entegratör seçimine bağlı kısım hariç taslak).
6. **Tema temizliği** ve **ekran birleştirme** — A1 ile paralel yürüyebilir (dosya çakışması az).
7. **A5** ve "diğer" kalemleri.

## 6. Karar bekleyenler

- **Kurucu:** O-0'ın "kilitle" mi "bekçiyle düzelt" mi olacağı; mahsup tablosu (çoklu borçta kuyumcunun seçtiği borcu kapatma) açılsın mı; K-13 fire; H-6 RFID; ilk banka; entegratör; avukat görüşü.
- **Mali müşavir:** 36.000 TL kuyumcu haddi (514 md.4 yürürlüğü), "binde" yöntemi, BIST kapanış kaynağı, kur kaydı saklama süresi, "satış dışı" paranın ciro/MASAK dışı sayılması, avans faturası zamanı, stopaj özelgesi, MASAK "bağlantılı işlem" penceresi.
- **Müşteri:** Faturalog'da fatura otomatik mi onaylı mı; gram veresiyeye TL hangi kurla düşülüyor.

## 7. P0 uygulama durumu (17.10, akşam)

| # | Durum | Not |
|---|---|---|
| 1 | Bekliyor → "Havaleden satış" paketi | Kurucu kararı 17.10-1 (listeden seç → fatura + arka planda fiş). Eski yol o pakette değişir |
| 2 | **Bitti** (`13e4bda`) | Tevsik / ŞİB / iş turu bayrağı `false` ise üretim açılmaz |
| 3 | **Bitti** | Devre kesici (bant dışı dış kur yazılmaz, günde bir kritik bildirim); satış/alış/takas/hurda kur tazeliği kapısı (iptal kur, dünkü elle kur, son çekimi 3 yoklama aralığından eski dış kur → durur); son çekim her başarılı çekimde yazılır; `/kur/kaynaklar` son çekimi döner. Canlıda ölçüldü: son çekim 26 sn önce, eşik 180 sn |
| 4 | **Bitti** | Had üstü FİŞ 400 `belgeTuru`; had üstü faturada geçerli TCKN/VKN zorunlu; satış + fatura TEK işlemde (fatura kesilemezse satış yazılmaz). Kapı tüm yasal kapılardan SONRA çalışır |
| 5 | **Kısmen bitti** | Elle has bedeli yolu API'de kapandı. **"Binde" yöntemi kurucu kararına bırakıldı** (müşterinin açık isteği; mevzuatta karşılığı yok) |
| 6 | İncelendi — açık yok | Gün sonu kilidi kasaya bağlıdır; banka kararının kasa bacağı yoktur. Kur yalnız referans; iptal kur dışlanır |
| 7 | **Bitti** | Kur temizliği: günün son kuru + 6 tabloya bağlı kur korunur (eskiden yalnız hareketler korunuyordu). Konsinye borcunun defter satırı → ayrı paket (tasarım kararı) |
| 8 | **Bitti** | Kesim anında alıcı unvan/TCKN-VKN/adres faturaya dondurulur (geçiş `P0FaturaAliciDondur`) |
| 9 | 1 ile birleşti | Havale bugün tasarım gereği kasaya yazılıyor (satış, vezne, takas, tahsilat); doğru çözüm havaleden satış akışının banka bacağı |

Test 12.721 / 0 · denetim yalnız bilinen P-23/P-30 · canlı yedek `kuyumcu_net-1710-p0-oncesi.sql.gz` → geçiş → yayın.
