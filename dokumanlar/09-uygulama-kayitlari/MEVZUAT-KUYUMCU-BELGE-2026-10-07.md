# Kuyumcu belge zorunluluğu — mevzuat ve entegrasyon

**Tarih:** 7 Ekim 2026 · **Hazırlayan:** Tamer Bey · **Kapsam:** yalnızca okuma. Kod değiştirilmedi.

## 1. Ne değişti, bizi nasıl etkiliyor

Haberdeki "gösterilmesi zorunlu belge" **kimlik belgesidir**. Yeni bir belge türü getirilmedi. Değişen tek şey, kimlik sorma sınırının yükselmesi: kuyumcu dahil bütün yükümlüler, tek seferde ya da birbiriyle bağlantılı işlemlerin toplamında **370.000 TL ve üzeri** işlemde müşterinin kimliğini alıp doğrulayacak. Eski sınır 185.000 TL idi. Düzenleme **bugün (7 Ekim 2026) yürürlüğe girdi**.

Mihenk bu kuralı zaten uyguluyor, ama sınır hâlâ 185.000 TL. Sonuç olarak program bugünden itibaren 185.000–369.999 TL arasındaki işlemlerde **yasanın artık istemediği kimliği** istiyor. Bu bir ceza riski oluşturmuyor, çünkü program yasadan daha sıkı davranıyor. Ama tezgâhta gereksiz bekleme yaratıyor ve gerekmeyen kişisel veri topluyor. Ekranlardaki "185.000 TL" yazısı da artık yanlış. **Entegre edebiliriz; küçük bir iş (S).**

## 2. Resmî dayanak

- **Düzenleme:** Suç Gelirlerinin Aklanmasının ve Terörün Finansmanının Önlenmesine Dair Tedbirler Hakkında Yönetmelikte Değişiklik Yapılmasına Dair Yönetmelik. Cumhurbaşkanı Kararı **sayı 11845**, karar tarihi 6 Ekim 2026. **Resmî Gazete 7 Ekim 2026, sayı 33393.** Yasal dayanağı 5549 sayılı Kanun'un 27. maddesi.
- **Madde 1:** Değiştirilen yönetmeliğin 5/1-b bendinde *"yüzseksenbeşbin TL" ibaresi "üçyüzyetmişbin TL"* olarak değiştirildi. Elektronik transferlerdeki sınır da 15.000 TL'den 30.000 TL'ye çıktı.
- **Madde 6:** *"Bu Yönetmelik yayımı tarihinde yürürlüğe girer."*
- **Kuyumcuya uygulanır mı?** Evet. Yönetmeliğin 3/1-k bendi kıymetli maden, taş ve mücevher alım satımı yapanları yükümlü sayıyor. 5/1-b bendi tüm yükümlülere uygulanıyor. Kural alışa da satışa da işliyor.
- **Kuyumcuyu ilgilendirmeyenler:**
  - 30.000 TL'lik elektronik transfer sınırı (md.5/1-c, 24, 24/A) iki finansal kuruluş arasındaki para transferine uygulanır (md.3, tanım e).
  - SMS ile imza doğrulama (md.16) finansal kuruluşlara yöneliktir.
  - Denetim raporu (md.38) MASAK'ın kendi işleyişini düzenler.
- **Kaynaklar:** resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf ve mevzuat.gov.tr'deki işlenmiş metin (21.5.200713012, dipnot 9: "11845 s. CK md.1"). İki metin de okundu ve doğrulandı.

## 3. Mihenk'te bugün durum

| Konu | Durum | Kanıt |
|---|---|---|
| Kimlik belgesi türleri (TC kimlik, pasaport, ehliyet, mavi kart, vergi levhası) | Var, değişiklik gerekmiyor | `Kuyumcu.Core/Domain/Sozlesmeler.cs:58` |
| Eşik ≥ sınır olunca kimlik zorunlu; eksik bilgiyle işlem kapanmıyor | Var | `Modules/Masak/MasakKuralMotoru.cs:149` |
| Bağlantılı işlem toplamı (aynı gün, aynı kişi) | Var | `Modules/Masak/MasakKapisi.cs:135` |
| Kapının uygulandığı yerler: satış, alış/hurda, takas, cari, banka dekontu, evrak, iptal | Var | `SatisModulu.cs`, `BankaModulu.cs`, `CariModulu.cs`, `EvrakModulu.cs`, `StornoSuzgeci.cs` |
| **Eşiğin tek kaynağı:** tarihli `yasal_parametreler` tablosu (işlem gününün değeri okunur) | Var, değer **185.000** | `MasakKuralMotoru.cs:118`; tohum `Migrations/20260930054500_YasalParametreler.cs:72` |
| Ayar dosyasında eşleşen değer (açılışta tabloyla karşılaştırılıyor; farklıysa sunucu açılmıyor) | **185.000** | `appsettings.json:53`; `Program.cs:661` |
| Ekranlar eşiği sunucudan alıyor (koda gömülü değil) | Var | `MasakModulu.cs:64`; `Web/Pages/Banka.razor:1200` |
| Geçmiş dönem raporları dönemin eşiğini kullanıyor | Var | `RiskServisi.cs:344` |
| /yasal-sinirlar ekranından **bugün başlayan** değer girilebiliyor mu? | **Hayır** (yalnızca ileri tarih). Bugünkü değişiklik veritabanı güncellemesiyle girilmeli | `Common/YasalParametreServisi.cs:317` |
| **Gizli tuzak:** 200.000 TL "işlem amacı beyanı" kademesi de kimliği zorunlu tutuyor | Sınır 370.000'e çekilse bile 200.000–369.999 TL arasında kimlik istenmeye devam eder | `MasakKuralMotoru.cs:146`; tohum `:73` (TEYİTSİZ) |

## 4. Entegre edebilir miyiz?

**Evet. Büyüklük: S** (tek paket, yarım gün iş + kapı testi). Yapılacaklar:

1. **Sunucu kuralı:**
   - Bir veritabanı güncellemesi yazılacak. Bu güncelleme 185.000 satırını 7 Ekim 2026'da kapatacak. Yerine 370.000 TL satırını açacak: başlangıç 7 Ekim 2026, damga DOĞRULANDI, kaynak RG 33393.
   - Aynı sürümde `appsettings` (53. satır) ve koddaki varsayılan değer (`MasakKuralMotoru.cs:24`) 370.000 olacak.
   - Üçü birlikte yayımlanmalı, yoksa sunucu açılmaz.
   - Değişiklik dönem bazlı olduğu için 6 Ekim ve öncesindeki işlemler eski sınırla değerlendirilmeye devam eder. Geçmiş yeniden yazılmaz.
2. **Ekran:** Kod değişikliği gerekmiyor; rakam sunucudan geliyor. Kod içi açıklamalardaki "185.000" yazıları düzeltilecek (`SatisModulu.cs:80` vb.).
3. **Saklama ve denetim izi:** Değişiklik gerekmiyor. Saklama süresi 8 yıl (md.46) aynı kaldı. Veritabanı güncellemesi denetim kaydı olarak iz bırakır.
4. **Fiş ve raporlar:** Rakam basılmıyor; MASAK raporları dönem eşiğini kendisi okuyor.
5. **Testler:** Yaklaşık 20 test dosyası 185.000 değerini sabit veri olarak kullanıyor. Varsayılan değeri okuyan testler güncellenecek; ekran testleri kendi sahte verisini kullandığı için etkilenmez.

**Riskler:**
- **Yasal:** Düşük. Program bugün yasadan sıkı davranıyor; gevşetme hatası yapılmadıkça ceza riski yok.
- **Kullanıcı akışı:** Bugün 185.000–370.000 TL arasındaki müşteriler gereksiz kimlik formuna takılıyor.
- **Veri:** Gerekmeyen kimlik toplanıyor (KVKK'nın "en az veri" ilkesi).
- **Yayın:** Ayar dosyası ile tablo uyumsuz kalırsa sunucu açılmaz. Değişiklik tek sürümde birlikte çıkmalı.

## 5. Sorular ve kararlar

**Mali müşavire:**
- **M-2 (açık soru, artık acil):**
  - 200.000 TL "işlem amacı beyanı" kademesi kuyumcuya uygulanıyor mu?
  - Uygulanıyorsa bu beyan, 370.000 TL altında da kimlik almayı gerektiriyor mu?
  - Yalnızca nakit işlemlerde mi geçerli?
  - Cevap gelene kadar program 200.000 TL'den itibaren kimlik istemeye devam eder.
- **S-2 (kapanabilir):** "Eşik bugün kaç TL?" sorusunun cevabı **370.000 TL** (RG 33393). Teyit yeterli.
- **Bugünden önce açılıp sonra kapanan işlemler:** 7 Ekim öncesi açılıp sonra kapanan işlemlerde (kaparo, vadeli satış) hangi günün sınırı esas alınır? Program işlem gününü esas alıyor.

**Kurucu kararı:**
1. **Paket ne zaman açılsın?**
   - **Öneri: hemen.** Küçük iş; düzenleme bugün yürürlüğe girdi ve ekranlardaki rakam yanlış.
2. **200.000 TL kademesi ne olsun?**
   - **(a) Öneri:** Mali müşavir cevabına kadar olduğu gibi kalsın (tutucu).
   - **(b)** Hemen yalnız nakit işlemlere daraltılsın.
3. **Çalışan sunuculara (5199/5200) yayın** ayrı onayla yapılsın; yayından önce yedek alınsın.
