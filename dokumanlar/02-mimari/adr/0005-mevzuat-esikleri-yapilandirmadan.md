# ADR-0005 — Mevzuat eşikleri koda gömülmez, yapılandırmadan okunur

**Durum:** Kabul edildi · **Tarih:** 2026-09-11

## Bağlam

MASAK kimlik tespiti eşiği **185.000 TL**'dir (Tedbirler Yönetmeliği md.5/1-b; bu rakam 14.01.2023 tarih 32073 sayılı RG, 6702 sayılı CK ile belirlendi). Hurda alımında stopaj oranları GVK md.94'e bağlıdır. VUK md.459 tevsik haddi 2024 sonunda 7.000 TL'den **30.000 TL**'ye çıkarıldı.

Bu rakamların **hepsi değişkendir** ve mevzuat değişikliğiyle güncellenir.

## Karar

Tüm mevzuat rakamları `appsettings` altındaki bölümlerden okunur: `Masak`, `Hurda`, `Takvim`. Kodda sabit rakam bulunmaz.

Dinî bayramlar ayrıca **yapılandırmadan** gelir ve girilmemiş yıl **sessizce geçilmez**, bildirilir.

## Gerekçe

Bir eşik değiştiğinde kod değişmemeli, yeniden derleme ve yayın gerekmemelidir. Kuyumcu bir tebliğ yayımlandığı gün uyumlu kalabilmelidir.

Dinî bayramlar özel bir durumdur: hicri takvim miladi takvime göre kayar ve tarihler Diyanet ilanıyla kesinleşir. Bunları koda gömmek veya hesaplamaya çalışmak **tahmin** olurdu — Anayasa md.1'e aykırıdır.

## Sonuçlar

**Olumlu**
- 36 test tüm eşik **sınırlarını** kapsar: 184.999,99 / 185.000 / 199.999,99 / 2.000.000 / 2.000.000,01 / 20.000.000 / 20.000.000,01 — negatif tutarlar dahil.
- Mevzuat değişince kod değişmez.

**Olumsuz**
- Yanlış yapılandırma sessizce yanlış davranışa yol açabilir. Bu risk, eşiklerin sınır testleriyle ve `/masak/esikler` ucunun eşikleri istemciye **sunucudan** vermesiyle azaltılmıştır.

## Güncelleme — PAKET YASAL-1 (2026-09-30): tarih boyutu

**Değişen karar:** mevzuat rakamları artık `appsettings`'ten değil, **tarihli `yasal_parametreler` tablosundan** okunur (`YasalParametreServisi`). Her satır geçerlilik aralığı (`[başlangıç, bitiş)`, yerel gün), birincil kaynak URL'si ve teyit damgası (`DOGRULANDI` / `TEYITSIZ` / `KAPALI`) taşır.

- Bugünkü işlem bugünkü rakamla, **geçmiş işlem kendi günündeki** rakamla değerlendirilir (risk penceresi, geç değerlendirme, raporlar).
- İleri tarihli değer yalnız sistem anahtarlı uçtan (`POST /api/v1/sistem/yasal-parametreler`) eklenir; gününde kendiliğinden devreye girer, **yeniden başlatma gerekmez** (önbellek gece ve yazımda tazelenir). Geçmiş/bugün başlayan değer 409.
- Tabloda o gün için değer yoksa **fail-closed**: 503 "Yasal sınır tanımlı değil" — kod varsayılanına düşülmez.
- `appsettings` bölümleri yalnız tohum ve **açılış eşitlik denetimi** içindir: yapılandırma değeri tablonun bugünkü değerinden farklıysa sunucu açılmaz (`YASAL_PARAM_AYRISMA`).
- Politika anahtarları (kural açık/kapalı, açıklama uzunlukları, risk puanları) tabloya girmez, `appsettings`'te kalır.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*

## İşletim runbook'u — ileri tarihli yasal değer (YASAL-1 tur 2, hasım bulgusu ORTA-3)

**Kim yapar:** yalnız ByGatech operatörü (sistem anahtarı; kuyumcu ekranından yazma yok).

1. **Hangi anahtar yazılabilir?** Sistem ucu (`POST /api/v1/sistem/yasal-parametreler`)
   YALNIZ programın tablodan, işlem gününe göre okuduğu anahtarları kabul eder
   (`YasalAnahtar.TablodanOkunan`: `TEVSIK_HADDI_TL` ve `MASAK_*`). Diğerlerine
   (`GIDER_PUSULASI_GUN`, `STOPAJ_*`, `KDV_*`, `FATURA_HADDI_TL`, `RESMI_TATIL`) ileri tarihli
   yazma **409** döner: kod onları hâlâ yapılandırmadan okur, satır gününde uygulanmazdı.
   Bu anahtarlar YASAL-3/YASAL-4 ile tarihli okumaya geçince listeye eklenir.
2. **Yeni değer yazıldığında appsettings ne olacak?** K-7 açılış eşitlik denetimi, yapılandırma
   değerini tablonun **o günkü** değeriyle karşılaştırır. İleri tarihli değer yürürlüğe girdiği
   gün (ör. 1 Ocak) yapılandırmadaki eski rakam tabloyla ayrışır → o günden sonraki **ilk
   yeniden başlatmada sunucu açılmaz** (günlükte `YASAL_PARAM_AYRISMA` satırı).
   **Zorunlu adım:** sistem ucuyla ileri tarihli değer yazan operatör, aynı değişikliği
   (`Tevsik:Had`, `Masak:*`) yürürlük gününden SONRA dağıtılacak yapılandırmaya işler ve
   yürürlük günü sabahı yayın/yeniden başlatma takvimini buna göre planlar. Çalışan sunucu
   etkilenmez (değer gününde tablodan kendiliğinden devreye girer); tuzak yalnız yeniden
   başlatmadadır.
3. **Açık soru (Bertan'a):** K-7'nin, tablodan okunan anahtarlarda yalnız "bugünkü değer
   tabloda YOKSA" kırılacak biçimde yumuşatılması (yapılandırma bu anahtarlarda artık okunmuyor).
   Karar gelene kadar şartname K-7 aynen uygulanır.
4. **Bayram günleri:** MASAK süre hesabı YASAL-3'e kadar `Takvim:ResmiTatiller`
   yapılandırmasını okur; tablo satırı hesabı ve uyarıyı ETKİLEMEZ. Yıllık bakım (Diyanet
   ilanı → yapılandırma) ByGatech destek görevidir (şartname § 11/3 önerisi, kurucu teyidi
   bekliyor). Eksiklik `GET /api/v1/sistem/yasal-parametreler/saglik` ve "Yasal sınırlar →
   Bu yılın bayram tatilleri" katlanırında görünür; Panel'de bant yoktur.
