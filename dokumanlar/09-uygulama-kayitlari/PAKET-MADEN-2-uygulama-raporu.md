# PAKET MADEN-2 — uygulama raporu (03.10, t1 · API 5160 / vekil 5161 / CDP 9709)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-MADEN-AYRIMI-uygulama-raporu.md` + hasım t1/t2 + tasarım t1 bulguları.
> COMMIT YOK. Migration YOK (şema değişmedi; yalnız türetilmiş rapor alanları). Ham kanıt: `ham/MADEN-2-olcum/`.

## 1. Yapılanlar (bulgu → çözüm)

| Bulgu | Çözüm | Dosya |
|---|---|---|
| **[KRİTİK] MAS20** — has maliyeti bilinmeyen altın satışı "Altın kârı (has)"a SIFIR maliyetle giriyordu (karHas = tüm has hasılatı) | Has bacağı artık KENDİ "bilinen" kümesiyle ölçülür. Yeni `KarZararServisi.HasMaliyetiBilinenler` TL kuralının AYNI yardımcısını (`MaliyetiBilinenler`) çağırır, birim olarak satırın has maliyetini verir (TL maliyeti yoksa has da bilinmiyor sayılır) → iki bacak ayrı kural üretmez. Bir altın satışı ancak TÜM parti satırlarında has maliyeti doluysa has kârına girer. `bilinenHasilatHas`, `maliyetHas`, `karHas` yalnız bu kümeden; eksik has hasılatı ayrı alanda (`maliyetiEksikHasilatHas`); hiçbiri bilinmiyorsa `karHas`/`maliyetHas` = null. Yeni yanıt alanları: `hasKarDurumu` (TAM/KISMI/HESAPLANAMADI/VERI_YOK), `hasMaliyetiEksikSatisAdedi`, `hasMaliyetiEksikIadeAdedi`, `yalnizHasMaliyetiEksikAdedi` (TL'si bilinen ama has'ı bilinmeyen altın hareketi). **Ürün satırı** (`UrunBazli`) aynı kural: `hasKarDurumu`, `hasMaliyetiEksikSatisAdedi`; has HESAPLANAMADI ise karHas/maliyetHas null | KarZararServisi.cs |
| (ekran) has bacağının kısmiliği | KarZarar.razor has kartları TL'nin `KismiMi`'si yerine yeni `HasKismiMi` (sunucunun `hasKarDurumu`'su) ile "Maliyeti bilinen satış / toplam · maliyeti eksik" yazar; TL'si bilinip has'ı bilinmeyen altın satışı için ayrı uyarı `#kz-has-maliyet-eksik` (TR+EN sözlük `HasYalnizEksikNot`) — TL eksiği zaten üst kutuda, aynı satış iki kez anlatılmaz | KarZarar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] xUnit2029 MadenAyrimiTestleri.cs(623) | `Assert.DoesNotContain("uyari-kutu", bant.ClassList)` | MadenAyrimiTestleri.cs |
| [DÜŞÜK] Stok değerlemesi konsinye/uyarı metni yalnız Türkçe, EN'de ham | Ekran sunucunun `uyari` metnini artık basmaz; `konsinyeUrunSayisi` (sunucuda zaten sayı) + sözlük `StokKonsinyeNot` (TR+EN), `#kz-stok-konsinye` | KarZarar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] Kâr-zarar Özet PRO 375 K4 "tren" | Ölçer kaynaklı (ⓘ summary bölüm sayılıyor) → TOPLU kuyruğu (açık kalanlar §11) | — |
| [DÜŞÜK] Bicim.Has yorumları bayat | Has yorumu tarihçeyle düzeltildi (Has N4 binlik; Gram F3 binliksiz); `HasGirdi` özeti "BİLEREK binliksiz (giriş kutusu)" diye yeniden yazıldı | Bicim.cs |
| [ORTA] Stok değeri / Maden dağılımı: başlık, gümüş biçimi, altın toplamı, bağlantı rengi | Başlıklar MADEN-AYRIMI t2'de "Miktar (madenine göre)" olmuştu (doğrulandı). Bu pakette: değer hücrelerinden `metin-amber` (= `--vurgu`, Platin'de bağlantı mavisi) kaldırıldı (stok değeri, maden dağılımı, reyon, atanmamış); stok değeri tablosuna **altın toplam satırı** (`tfoot #rapor-stok-altin-toplam`, sunucunun `toplamHas`'ı, `Bicim.Has`); reyon/atanmamış satırları "fiziksel (saf)" biçiminde — sunucu `reyon-stok` madenler satırına `fizikselGram` (adet × ürün gramı) ekler | Raporlar.razor, RaporModulu.cs (ReyonStok), YerlesimSozlesmeleri.cs |
| [DÜŞÜK] Kart altı rozet/katlanır ve stok yaşı ızgaraları yapışık | Yeni CSS yazılmadı; mevcut `.mt-2` yardımcı sınıfı: `#kz-stok-guven` (PRO), `Katlanir #kz-stok-nereden` (BASİT, `Sinif`), stok yaşı ikinci `stat-izgara` | KarZarar.razor, Raporlar.razor |
| [DÜŞÜK] Ölü stok boş metni "Bu dönemde kayıt yok" | `#sy-olu-bos`: "Ölü stok yok — {eşik} günden eski mal bulunmuyor." (`OluStokYok`, TR+EN); ölü stok tablosu başlığı "Has gram" → "Miktar (madenine göre)" (gümüş satırı da var) | Raporlar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] Rapor merkezi Özet bandı eksik iadeyi anmıyor | `aylik` yanıtına `maliyetiEksikIadeAdedi` (KarZararServisi'nden); bant koşulu satış+iade; iade varsa "Maliyeti eksik 2 satış ve 1 iade — kâra katılmadı" (`MaliyetiEksikGenel` + `KarRaporSozlugu.EksikSayi`, tutar yalnız iadesizken — iade varken net tutar eksiye dönebilir); kâr kartı "Hesaplanamadı" ve "kısmi" koşulları da iadeyi sayar | RaporModulu.cs (Aylik), RaporlarSozlesmeleri.cs, Raporlar.razor |
| [ORTA] Panel.razor eksik-iade dalı | `karEksikIade` (PanelKarOzeti.MaliyetiEksikIadeAdedi); hesaplanamadı/kısmi dalları satış+iade; iade varsa "Kâr X · maliyeti eksik: 1 iade" (`PanelKarKismiGenel`); iadesizken eski metin aynen | Panel.razor, PanelSozlesmeleri.cs |
| [DÜŞÜK] Gümüş için "has" taşıyan hub etiketleri | Ekranlarda kalan son kullanım: ölü stok başlığı → değiştirildi. Hub anahtarları (`RaporHasSozlugu.*HasBaslik`, `KarZarar.MadenToplanmaz`…) artık bu ekranlarda okunmuyor; değerleri değiştirilmedi (başka ekran/test bağı riski) → hub temizliği TOPLU kuyruğu | Raporlar.razor |
| [DÜŞÜK] "Altın pozisyonum 0,0 g has" ayrı has biçimi | `#pozisyon-net` artık işaretli `Bicim.Has` ("+2,3400 has", "+1.312,8000 has"); kırılım satırları (Vitrin/Alacak "g") değişmedi | AltinPozisyonuKarti.razor |
| [DÜŞÜK] pos-fatura-mutabakati `hurda.hasGrami` tüm madenleri topluyor | DEĞİŞİM-GÜNSONU şeridinin alanı — **devir** (dokunulmadı) | — |

## 2. Sapmalar ve gerekçeleri
- **MAS20 ön koşulu değişti** (`Dogrulayici/MADENAYRIMISinirTestleri.cs:179`): test `bilinenHasilatHas > 0` ön koşulunu okuyordu; doğru kuralda has maliyeti bilinmeyen satış `bilinenHasilatHas`'a GİRMEZ (ayrı alan). Ön koşul toplam `hasilatHas`'tan okunur; iddia (karHas null ya da ≠ tüm hasılat) AYNEN. Yerinde `MADEN-2` yorumu.
- **KR50 fikstürü** (`KarRaporTestleri.cs` `MaliyetliParti`): partiler has fiyatı OLMADAN açılıyordu ve test has bacağının toplanabilirliğini MAS20 hatasının üzerine kuruyordu (karHas = hasılat − 0). Fikstüre kur fiyatı (6.827,20) verildi; niyet (toplanabilirlik) korunur.
- **Altın pozisyonu mühürlü testleri**: `AltinPozisyonuKartiTestleri.B2K_01`, `S31PanelSadeTestleri` (`+312,8 g has` → `+312,8000 has`), `OrtakGorunumTestleri.PN1` (`+155,2000 has` 13 karakter → kademe `uzun`). Gerekçe: ilke "Altın pozisyonum has biçimi Bicim.Has ile aynı".
- **Alan dışı zorunlu dokunuşlar** (sözleşme/derleme bağı): `Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor` (yalnız `#pozisyon-net` satırı + `NetHas` yardımcısı — "altın pozisyonu biçimi" maddesi bu bileşende yaşıyor), `Kuyumcu.Contracts/Sayfalar/{RaporlarSozlesmeleri,PanelSozlesmeleri}.cs` (+`MaliyetiEksikIadeAdedi = 0`), `Kuyumcu.Core/Sozlesmeler/YerlesimSozlesmeleri.cs` (+`FizikselGram = null`), `RaporModulu.ReyonStok` (Stok değeri sekmesinin reyon kartı).
- **Canlı KISMI/HESAPLANAMADI tetikleyicisi**: üretimdeki tetikleyici (alış kurunda `AltinHasFiyati` yok) canlıda API ile üretilemez (manuel kur `AltinHasFiyati`'yı zorunlu kılar, otomatik kur hep dolu). Test kiracısında iki partinin `birim_maliyet_has`'ı SQL ile NULL'a çekildi (aynı durumun benzetimi; yalnız kendi test kiracım, kayıt §8'de).
- Konsinye notu eski davranışla aynı koşulda çıkar (`else if`, kısmi değilken).

## 3. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Tüm değişiklikler türetilmiş salt okunur rapor alanları (`hasKarDurumu`, `hasMaliyetiEksik*`, `yalnizHasMaliyetiEksikAdedi`, aylık `maliyetiEksikIadeAdedi`, reyon `fizikselGram`). Rapor rakamları düzenlenmez: düzeltme yolu kaynak işlemin ekranında ters kayıt (DegismezIpucu PRO'da görünür — görüntüde "Bu kayıt değiştirilemez"). Yeni düğme yok. [RolGerekli] uçlar değişmedi. Finansal tabloya yazma yok.

## 4. Testler
- Yeni dosya `Kuyumcu.Tests/MadenIkiTestleri.cs` — **21 vaka**: DB MI01 (TAM 0,2197 − 0,1465 = 0,0732), MI02 (KISMI: TL TAM, has KISMI, karHas 0,0732 — eski hata 0,2929), MI03 (HESAPLANAMADI: karHas/maliyetHas null, eksik has 0,2197), MI04 ×2 (ürün satırı KISMI/HİÇBİRİ), MI05 (aylık `maliyetiEksikIadeAdedi`); saf MI06 (tek yardımcı: TL+has / has yok / yarım adet / TL yok / bir satırı eksik), MI07 (EksikSayi TR+EN); bUnit Kâr-zarar MI10 ×2, MI11, MI12 ×2 (TR+EN konsinye), MI13 ×2; bUnit Raporlar MI20–MI23; bUnit Panel MI30, MI31 ("+1.312,8000 has").
- Filtreli: `MadenIki|MADENAYRIMI` **77/77**; geniş (Kar|Has|Maliyet|Rapor|Maden|Iade|Pozisyon|Panel|Sinir|Ortak) **3629 · 3627 geçti · 0 başarısız · 2 atlanan**; mutasyon sonrası temiz derlemeyle `MadenIki|MADENAYRIMI|AltinPozisyonuKarti|S31PanelSade|OrtakGorunum` **219/219**.
- **Tam süit** `trx-pMADEN-2/_Mac_2026-10-03_02_20_52.trx`: **9925 · geçti 9922 · başarısız 1 · atlanan 2**. Tek kırmızı `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — devir notunda zaten listeli (PATCH-3 / Gün sonu sahibi; bu paket o uca dokunmadı). MadenIki 21/21 trx'te Passed. **Yeni kırmızı 0.**
- Build: son temiz derlemem (`b6.log`, mutasyonlardan sonra `touch` + `dotnet build Kuyumcu.sln`) **0 hata, 0 uyarı**. Tam süit öncesi ikinci derleme (`b7.log`) **7 hata** verdi — hepsi `Dogrulayici/DEGISIMGUNSONUSinirTestleri.cs` (`Islemler.TakasOdemeVarsayilani` yok): DEĞİŞİM-GÜNSONU şeridinin o an yarım işi; bu paketin dosyası değil. Bu paketin dosyalarında uyarı 0 (xUnit2029 giderildi); ağaçta kalan uyarılar başka şeritlerin: SUBEKAPSAMASinirTestleri.cs:49 CS8601, KesifP2Testleri.cs:455 xUnit2029.

## 5. Mutasyon tablosu (betik `/private/tmp/kuyumcu-serit/MADEN-2/mut.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/MADEN-2/`, ham `ham/MADEN-2-olcum/mut-sonuc.txt`)

| Kod | Değişiklik | Kırmızı test (adıyla) | cmp |
|---|---|---|---|
| M1 | `HasMaliyetiBilinenler` birim = TL (has yokluğu görülmez) | MAS20; MI02; MI03; MI04(kismi:True/False); MI06 | AYNI |
| M2 | Olc bilinen has hasılatı tekrar TL kümesinden | MI02; MI03 | AYNI |
| M3 | UrunBazli `hasDurum == HESAPLANAMADI` koşulu kaldırıldı | MI04(kismi: False) | AYNI |
| M4 | `HasKismiMi` KISMI'yi görmez | MI10 ×2 | AYNI |
| M5 | `#kz-has-maliyet-eksik` hiç çizilmez | MI10 ×2 | AYNI |
| M6 | konsinye notu tekrar sunucu `uyari` metni | MI12 ×2 | AYNI |
| M7 | Rapor bandı yalnız satışa bakar | MI21 | AYNI |
| M8 | Rapor bandı iadeyi metne katmaz | MI20; MI21 | AYNI |
| M9 | altın toplam satırı çizilmez | MI22 | AYNI |
| M10 | reyon fiziksel gram yok sayılır | MI22 | AYNI |
| M11 | ölü stok boş metni eski | MI23 | AYNI |
| M12 | Panel kısmi dalı iadeyi görmez | MI30 | AYNI |
| M13 | `#pozisyon-net` eski "N1 g has" | MI31; B2K_01; PN1 (OrtakGorunumPanel); S31 | AYNI |
| M14 | aylık `maliyetiEksikIadeAdedi` = 0 | MI05 | AYNI |
| M15 | `#kz-stok-guven` boşluk sınıfı kaldırıldı | MI13(basit: False) | AYNI |
| M16 | stok yaşı ikinci ızgara boşluğu kaldırıldı | MI23 | AYNI |
| M17 | stok değeri hücresi tekrar `metin-amber` | MI22 | AYNI |

17/17 kırmızı, 17/17 cmp AYNI. **Not (süreç):** betik `shutil.copy2` ile geri kopyaladığı için dosyanın ESKİ mtime'ı korundu ve MSBuild bir sonraki koşumda bazı derlemeleri güncel saydı (M3'ün Api dll'i M4–M8'e, M13/M14'ünkü sonrakilere taşındı — ham listede MI04/B2K_01/MI05'in "fazladan" kırmızısı bu yüzden). Her mutasyonun KENDİ hedef testi kırmızı olduğundan sonuç geçerli; ardından değişen 6 kaynak `touch` + temiz `dotnet build Kuyumcu.sln` (0/0) + filtreli koşum 219/219 ile ağaç doğrulandı.

## 6. denetim.sh
303 ✓, tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde muaf) → **TEMİZ (P-30 hariç)** (`/private/tmp/kuyumcu-serit/MADEN-2/denetim1.log`). Yeni CSS yok, satır içi style yok, Pages altında yeni ModalPencere yok.

## 7. Canlı doğrulama (yayın anlık görüntüsü `/private/tmp/kuyumcu-serit/MADEN-2/yayin`, API 5160)
Kiracı **PAKETMADEN-2-TEST** (01M3ZE3BFAMH3JXMNSHAE4AJJK). Ürünler: M2-T / M2-K / M2-H (22 ayar 3,5 g), M2-G (925 gümüş 30 g), M2-A (14 ayar, açılış 3 maliyetsiz). Alışlar 15.000 ₺ (kur has fiyatı → birim has maliyeti 2,265077); **H partisi ve K'nın ilk partisi (2 adet) `birim_maliyet_has = NULL`** (SQL, row_count 2).

| Adım | HTTP | API (has bacağı) | Bağımsız SQL |
|---|---|---|---|
| kur/ürün/cari/alış | 201×7, 200×5 | — | stok_partileri: T/K2/H/G birim_maliyet_has 2,265077 / 0,135905; A NULL |
| 1 — yalnız H satışı (21.000) → **HESAPLANAMADI** | 200 | `karDurumu TAM karTl 6000` · `hasKarDurumu HESAPLANAMADI`, `hasilatHas 3,1711`, `bilinenHasilatHas 0`, `maliyetiEksikHasilatHas 3,1711`, `maliyetHas null`, **`karHas null`** (eski hesap 3,1711 has "kâr" derdi); ürün M2-H aynı | H satışı has_karsilik 3,171108, tüketim has_null 1 |
| 2 — T + K×3 + G + A×2 + A iadesi → **KISMI** | 200×7 | dönem `hasKarDurumu KISMI`: hasilatHas 16,4596 · **bilinen 6,3422 − maliyet 4,5302 = karHas 1,8120** · eksik has 10,1174 · hasEksikSatis 5 · yalnizHasEksik 3; TL KISMI karTl 30.600, eksik 2 satış + 1 iade. Ürün: **M2-T TAM karHas 0,9060**, **M2-K KISMI 0,9060** (hasEksik 2), **M2-H HESAPLANAMADI null**, M2-G gümüş has alanları null, M2-A HESAPLANAMADI | `BILINEN_HAS_ALTIN 6,3422 / 4,5302 / 1,8120` (kuruşu kuruşuna aynı); eski hatalı formül `ESKI_HATALI_KAR_HAS 11,3253` (`sql-bagimsiz.txt`) |
| aylık | 200 | `karDurumu KISMI`, `maliyetiEksikSatisAdedi 2`, **`maliyetiEksikIadeAdedi 1`** | hareketler: A SATIS×2 + SATIS_IADE×1, tüketim tl_null 1 |
| reyon-stok / stok-degeri / pozisyon | 200×3 | atanmamış GUMUS 83,25 saf / **90,0 fiziksel**, ALTIN 2,34 / 4,0; stok-degeri toplamHas 2,34; pozisyon altın netGram 2,34 | — |

Temizlik: API (PID dosyası) ve vekil `kill`; 5160/5161/9709 dinleyen yok. `zsh kiraci-temizle.sh 01M3ZE3BFAMH3JXMNSHAE4AJJK --uygula` → `tenants where id=…` 0, `hareketler` 0, `stok_partileri` 0. tenants: başlangıç **17** → son **15** (benimki silindi; −1 fark eşzamanlı şeridin kendi kiracısını silmesi — betik yalnız verilen kimliği siler). QA-TEST-* dokunulmadı.

## 8. Gerçek tarayıcı (başsız Chrome `betikler/tur/tur.mjs` + sikisma-olc, CDP 9709, vekil 5161; ham `ham/MADEN-2-olcum/olcum-1.jsonl`, `analiz.txt`)
266 kayıt (36 sayfa + 78 sekme + 152 katman), **hata 0, geçersiz 0**, 12 kombinasyonun 12'si (1440/1280/375 × BASİT/PRO × açık/koyu; her kombinasyonda modGercek = istenen).

| Rota | Sonuç |
|---|---|
| `/` (Panel) | **temiz** |
| `/raporlar/kar-zarar` Grafik / Köprü / Ürünler / Stok | **temiz** (taşan 0 · kırık 0 · binen 0 · yatay 0 · gizli-görünen 0 · TRY 0) |
| `/raporlar/kar-zarar` Özet | yalnız K4 "tren" 3.6x4 PRO 375 açık+koyu (önceden 3.4x4; ölçer ⓘ summary sayar + yeni has uyarı satırı) — TOPLU kuyruğu |
| `/raporlar` tüm sekmeler | yalnız önceden var olanlar: K12 `#rp-yil/#rp-ay` ezik (PRO 1280/1440), K4 tren 375 (ana, Stok yaşı, Gün sonu) |

Görüntüler Read ile GÖRÜLDÜ (`ham/MADEN-2-olcum/foto/`):
- `raporlar_kar_zarar__PRO_aydinlik_1440.png`: Altın kârı "MALİYETİ BİLİNEN SATIŞ 6,3422 has · Toplam satış 16,4596 has · maliyeti eksik 10,1174 has", "MALİYET 4,5302 has", "KÂR 1,8120 has" (SQL ile aynı); uyarı "3 altın satışının alış günündeki altın fiyatı bilinmiyor; bu satışlar altın kârına (has) katılmadı."; gümüş kartı "DEFTERDEKİ GÜMÜŞ (SAF GRAM) saf 27,75 g".
- `raporlar_kar_zarar__BASIT_karanlik_375.png`: aynı rakamlar, taşma yok, has uyarısı görünür, "Bu rakamlar nereden geliyor?" kapalı.
- `raporlar_kar_zarar__PRO_aydinlik_1440__sekme4.png`: "Güvenilirlik: ORTA" rozeti kart sırasından ayrık (~16 px).
- `raporlar__PRO_karanlik_1440__sekme3.png`: Stok değeri değerleri düz metin (mavi değil), gümüş "90,00 g (saf 83,25 g)", altın "2,3400 has", **"ALTIN (HAS) 2,3400 has" toplam satırı**; reyon atanmamış "Gümüş: 90,00 g (saf 83,25 g) · Altın: 2,3400 has".
- `raporlar__PRO_aydinlik_1440.png`: bant "Maliyeti eksik 2 satış ve 1 iade — kâra katılmadı · Ayrıntı →".
- `raporlar__BASIT_aydinlik_1280.png`: "Altın pozisyonum +2,3400 has" (Bicim.Has).
- `raporlar__PRO_karanlik_375__sekme4.png`: kova ve devir ızgaraları arasında boşluk; "Ölü stok yok — 180 günden eski mal bulunmuyor."; gümüş "90,00 g (saf 83,25 g)".

## 9. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokunduğu)
```
?? dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs        (HasMaliyetiBilinenler, Olc has kümesi, UrunBazli has durumu)
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs              (Aylik maliyetiEksikIadeAdedi, ReyonStok fizikselGram)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                      (Özet bandı, Stok değeri, reyon, Stok yaşı — Gün sonu HARİÇ)
 M dotnet/Kuyumcu.Web/Pages/Panel.razor                         (yalnız kâr/eksik-iade dalı)
?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor      (yalnız #pozisyon-net)
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs                        (yalnız yorumlar)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs               (8 yeni anahtar TR+EN + EksikSayi)
?? dotnet/Kuyumcu.Core/Sozlesmeler/YerlesimSozlesmeleri.cs      (+FizikselGram)
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs    (+MaliyetiEksikIadeAdedi)
?? dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs       (+MaliyetiEksikIadeAdedi)
?? dotnet/Kuyumcu.Tests/MadenIkiTestleri.cs                     (yeni)
?? dotnet/Kuyumcu.Tests/MadenAyrimiTestleri.cs                  (xUnit2029)
?? dotnet/Kuyumcu.Tests/KarRaporTestleri.cs                     (KR50 fikstürü)
?? dotnet/Kuyumcu.Tests/Dogrulayici/MADENAYRIMISinirTestleri.cs (MAS20 ön koşulu)
?? dotnet/Kuyumcu.Tests/AltinPozisyonuKartiTestleri.cs / S31PanelSadeTestleri.cs / OrtakGorunumTestleri.cs (pozisyon biçimi)
```
Dokunulan sayfalar: KarZarar.razor, Raporlar.razor, Panel.razor (gerekçe: MADEN-AYRIMI'nın devam paketi; bulgular bu dosyaları açıkça istiyor; Raporlar Gün sonu sekmesine dokunulmadı).

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK MAS20: has bacağı ayrı bilinen kümesi, TAM/KISMI/HİÇBİRİ | KARŞILANDI | MAS20 yeşil; MI01–MI04, MI06; canlı §7 = SQL (6,3422 / 4,5302 / 1,8120; H null) |
| TL kuralı tek yardımcıya bağlı | KARŞILANDI | `HasMaliyetiBilinenler` → `MaliyetiBilinenler`; MI06; M1 |
| Ürün satırı aynı kural | KARŞILANDI | MI04 ×2; canlı M2-T/K/H |
| Has kısmilik işareti/sayımı | KARŞILANDI | `hasKarDurumu`, `hasMaliyetiEksik*`, `yalnizHasMaliyetiEksikAdedi`; ekran `#kz-has-maliyet-eksik` |
| Gümüş hiçbir ekranda "has" değil | KARŞILANDI (bu paketin ekranları) | görüntüler §8; ölü stok başlığı değişti; hub değerleri TOPLU |
| Altın pozisyonum Bicim.Has | KARŞILANDI | MI31, B2K_01; görüntü "+2,3400 has" |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | §5, 17/17 |
| Başsız Chrome 1440/1280/375 BASİT+PRO açık+koyu, Read | KARŞILANDI | §8, hata 0 |
| build 0/0 | KARŞILANDI (kendi son temiz derlemem b6) | §4; b7'deki 7 hata DEĞİŞİM-GÜNSONU dosyasında |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §6 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | §4 (tek kırmızı devir listesinde) |
| Test kiracısı silindi | KARŞILANDI | §7 |

## 11. Açık kalanlar (sahibiyle)
- `pos-fatura-mutabakati hurda.hasGrami` tüm madenleri topluyor → **DEĞİŞİM-GÜNSONU** (devir; dokunulmadı).
- Rapor merkezi BASİT "Bugün ne sattım 18,3700 has" (gün sonu/bugün kartı) — ayrı has kaynağı; Bugün alanı → TOPLU-5 / Gün sonu sahibi.
- Hub sözlüğünde kullanılmayan eski "has" metinleri (`RaporHasSozlugu.MadenHasBaslik/StokHasBaslik/ReyonHasBaslik`, `KarZarar.MadenToplanmaz`, `HasDefterGramMaden`, `HasTanimNotu`) → hub temizliği TOPLU kuyruğu.
- Ölçer K4 "tren" (ⓘ summary bölüm sayılıyor; Kâr-zarar Özet PRO 375 3.6x4, Stok yaşı PRO 375) → ölçer iyileştirmesi / TOPLU.
- PRO defter kartlarında gümüş kartı altından önce (sunucu sırası) — kozmetik, TOPLU.
- `/raporlar` K12 `#rp-yil/#rp-ay` ezik — önceden var.
- Tam süitte başkasına ait kırmızı: `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` (PATCH-3 / Gün sonu).
- Ağaçta derleme hatası (o an): `Dogrulayici/DEGISIMGUNSONUSinirTestleri.cs` `Islemler.TakasOdemeVarsayilani` → DEĞİŞİM-GÜNSONU şeridi.
- Süreç notu (orkestratöre): mutasyon betiklerinde geri kopyalamadan sonra `touch` şart (copy2 eski mtime'ı korur → bayat dll).

## 12. Kurucu / mali müşavir
Mevzuat hükmü içermez (kâr raporunun gösterim/hesap kuralı, birim). Not: "altın kârı (has)" yalnız has maliyeti bilinen altın satışlarından ölçülür; alış günü altın fiyatı kaydı olmayan malın has kârı "bilinmiyor" sayılır — proje gösterim kuralıdır (KAR-RAPOR fail-closed), defter/beyan kuralı değildir.

---

# TUR 2 (03.10 03:00–03:45 · t1 · API 5160 / vekil 5161 / CDP 9709) — doğrulayıcı + tasarım hakemi bulguları

> Ham kanıt: `ham/MADEN-2-olcum/tur2/` (canlı, SQL, ölçüm, görüntüler). Migration YOK. COMMIT YOK.

## T2-1. Kapatılan bulgular
| Bulgu | Çözüm | Dosya |
|---|---|---|
| **[KRİTİK, tasarım hakemi] Altın pozisyonu kartında has biçimi yarım** ("+192,5316 has" altında "Vitrin 192,5 g · Alacak 0,0 g", Alacak çerçeveli hap) | Kartın TÜM altın alt değerleri ana değerle aynı `Bicim.Has` dilinde: yeni yardımcı `HasMiktar` → `#pozisyon-vitrin`, `#pozisyon-alacak`, `#pozisyon-borc`, `#pozisyon-hurda`, `#pozisyon-konsinye`, `#pozisyon-emanet`, alacak/borç cari listesi hücresi. "N1 g" yalnız diğer madenlerin (gümüş) satırında kaldı (has yalnız altın). Hap görünümü: `app.css` `#pozisyon-kart .pozisyon-kirilim-oge` — kenar/yuvarlak/yatay dolgu 0, kalıcı noktalı alt çizgi (`var(--dugme-kenar)`), üzerine gelince düz çizgi + `var(--metin)`; dugme.css'teki `button:is(.pozisyon-kirilim-oge…)` hap kenarı KİMLİK özgüllüğüyle ezilir → bağlantı Vitrin ile düğme Alacak AYNI görünür, davranış aynı | AltinPozisyonuKarti.razor, app.css |
| [DÜŞÜK] Ürünler sekmesi has kâr hücresi has kısmiliğini söylemiyor | İstemci sözleşmesi `UrunSatiri` +`HasKarDurumu`, +`HasMaliyetiEksikSatisAdedi` (sunucu zaten döndürüyordu). Hücre: KISMI → sarı `kısmi` rozeti + "{n} satış katılmadı"; HESAPLANAMADI → "—" altında "Altın maliyeti bilinmiyor"; TAM ve gümüş not taşımaz. Sözlük TR+EN (`HasKismiRozet`, `HasKismiSatirNot`, `HasHesaplanamadiSatirNot`) | KarZarar.razor, KarZararSozlesmeleri.cs, Sozlukler.KarRapor.cs |
| [DÜŞÜK] pos-fatura-mutabakati `hurda.hasGrami` | DEĞİŞİM-GÜNSONU alanı — **devir** (dokunulmadı) | — |
| [DÜŞÜK] Rapor merkezi/Panel "Bugün ne sattım … has" ayrı has kaynağı/biçimi | Bugün/Gün sonu kartı — **devir TOPLU-5 / Gün sonu** (dosya alanı dışı; görüntüde Panel "18,4 g has", Rapor merkezi "18,3700 has") | — |
| [DÜŞÜK] denetim P-30 | Paket dışı (orkestratör/kurucu kronometreli ölçüm) | — |

**Sapma (gerekçeli):** Kural önce `olcek.css`'e yazıldı; `OlcekTekTestleri.OL6` ("olcek.css yalnız ölçü yazar, renk yazmaz") kırmızı verdi → `olcek.css` yedekten birebir geri alındı (`cmp` AYNI) ve kural `app.css`'e (kırılım öğesinin taban kuralının hemen altına) taşındı. Defter farkı uyarısının "{0} g" metni (PozisyonSozlugu/Katalog) değiştirilmedi — yalnız kayıt uyuşmazlığında çıkar, sözlük bu paketin alanı dışında → açık kalanlar.

## T2-2. DoD sözleşmesi
Yeni varlık/uç/düğme yok. `HasKarDurumu`/`HasMaliyetiEksikSatisAdedi` türetilmiş salt okunur rapor alanı; düzeltme yolu değişmedi (DegismezIpucu PRO'da görünür). Kırılım düğmeleri (Alacak/Borç) yine `GET /pozisyon/cariler` isteği atar (davranış aynı, yalnız görünüm).

## T2-3. Testler
- Yeni: `MI14_Urunler_has_kar_hucresi_kismi_rozeti_ve_hesaplanamadi_notu` (tr, en), `MI32_Pozisyon_kirilim_ogeleri_hap_degil_ayni_gorunum`; güçlendirilen: `MI31` (+"Vitrin 1.312,8000 has", "Alacak 0,0000 has", kırılımda "\d g" yok). `MadenIkiTestleri` **24/24** (21 → 24).
- Güncellenen mühürlü beklentiler (gerekçe: tek has biçimi): `AltinPozisyonuKartiTestleri.B2K_01` ("Vitrin 298,4000 has", "Alacak 36,4000 has", "Borç 22,0000 has"), `S31PanelSadeTestleri.TP1_09` ("Vitrin 298,4000 has", "Alacak 14,4000 has").
- Filtreli geniş koşum (MadenIki|MADENAYRIMI|MadenAyrimi|AltinPozisyonuKarti|S31PanelSade|OrtakGorunum|OlcekTek|DugmeIki|S1hGorselSigma|GorunumSubesiEkran|KarZarar|KarRapor|TasarimBelirtec|Css): **577/577**.
- Build (son temiz, mutasyonlardan sonra `tur2-b3.log`): **0 hata · 2 uyarı** — ikisi de başka şeritlerin dosyasında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(455)` xUnit2029); bu paketin dosyalarında 0 uyarı.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pMADEN-2-tur2/_Mac_2026-10-03_03_24_05.trx`): **toplam 10013 · yürütülen 10011 · geçti 10008 · başarısız 3 · atlanan 2**. Kırmızılar: `ParaDemo2IslemEkranTestleri.PD2E_8_takas_kilit_sebebi_EKSIK_ALANA_gore…` (takas — DEĞİŞİM-GÜNSONU), `S34RaporBugunTestleri.S34_10_Gun_sonu_FARK_VAR…` (`#bugun-gun-sonu-notlar-sayi` — Gün sonu, DEĞİŞİM-GÜNSONU; Raporlar.razor ve S34 test dosyası koşum sırasında 03:18/03:24'te o şerit tarafından değiştirildi, test dll'i 03:23'te yeniden derlendi), `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12_…` (eşzamanlılık — PATCH-3/Gün sonu). Üçü son ağaçta tek başına yeniden koşuldu: **3/3 geçti** (`tur2-kirmizi3.log`) → eşzamanlı şerit derlemesi/zamanlama kaynaklı; hiçbiri bu paketin dosyasına bağlı değil. MadenIki 24/24 trx'te Passed. **Yeni kırmızı 0.**

## T2-4. Mutasyon (betik `/private/tmp/kuyumcu-serit/MADEN-2/mut2.zsh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/MADEN-2-tur2/`; geri kopyalama + `touch` — tur 1'deki bayat dll sorunu giderildi)
| Kod | Değişiklik | Kırmızı test (adıyla) | cmp |
|---|---|---|---|
| M18 | Ürün has hücresi `HasKarDurumu == "KISMI"` koşulu bozuldu | MI14(tr), MI14(en) | AYNI |
| M19 | HESAPLANAMADI notu koşulu bozuldu | MI14(tr), MI14(en) | AYNI |
| M20 | `HasMiktar` → eski "N1 g" | MI31; B2K_01; TP1_09 | AYNI |
| M21 | `#pozisyon-kart .pozisyon-kirilim-oge` `border: 0` → hap kenarı | MI32 | AYNI |
| M22 | yalnız Alacak eski "N1 g" | MI31; B2K_01; TP1_09 | AYNI |
| M1R | (KRİTİK tekrar, son kodla) `HasMaliyetiBilinenler` has birimi = TL | MI06; MI03; MI04(True); MI04(False); MI02; **MAS20** | AYNI |
6/6 kırmızı, 6/6 cmp AYNI; ardından temiz `dotnet build Kuyumcu.sln` 0 hata.

## T2-5. denetim.sh
303 ✓ · tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (muaf) → **TEMİZ (P-30 hariç)** (`/private/tmp/kuyumcu-serit/MADEN-2/tur2-denetim.log`).

## T2-6. Canlı doğrulama (API 5160, `dotnet run --no-build`, Development)
Kiracı **PAKETMADEN-2-TEST-T2** (01M3ZJQYE8S4CZH5CN58HR1QGB). Kurulum: davet 201, kayıt 201, giriş 200, ürün 201×5, cari 201×2, alış 200×5. `stok_partileri` M2-H ve M2-K ilk partisi `birim_maliyet_has = NULL` (row_count 2; üretimdeki "alış kurunda AltinHasFiyati yok" durumunun benzetimi, tur 1 §2 gerekçesi).

| Senaryo | HTTP | API has bacağı | Bağımsız SQL (`tur2/sql-bagimsiz.txt`) |
|---|---|---|---|
| HESAPLANAMADI (yalnız M2-H) | 200 | `hasKarDurumu HESAPLANAMADI`, hasilatHas 3,1711, bilinenHasilatHas 0, eksik 3,1711, **maliyetHas null, karHas null**; TL TAM karTl 6000 | M2-H SATIS has_karsilik 3,171108, has_null 1 |
| KISMI (T + K×3 + G + A×2 + A iade) | 200×8 | dönem `KISMI`: hasilatHas 16,4596 · **bilinen 6,3422 − maliyet 4,5302 = karHas 1,8120** · eksik 10,1174 · hasEksikSatis 5 · yalnizHasEksik 3 | `BILINEN_HAS_ALTIN 6,3422 / 4,5302` (kâr ham farkı 1,8121 = 6,342216 − 4,530154; API tanımı gereği yuvarlanmış bileşenlerden 6,3422 − 4,5302 = **1,8120**, kuruşu kuruşuna aynı); eski hatalı formül 11,3254 |
| TAM (ürün M2-T) | 200 | `TAM` karHas 0,9060 | `URUN M2-T 3,1711 − 2,2651 = 0,9060` |
| Ürün M2-K KISMI | 200 | `KISMI` karHas 0,9060, hasEksikSatis 2 | `URUN M2-K 0,9060` (yalnız has'ı bilinen 1 satış) |
| Gümüş M2-G | 200 | hasKarDurumu/hasilatHas/karHas null | — |
| aylık / reyon / stok-değeri / pozisyon | 200×4 | aylık KISMI eksik 2 satış + 1 iade; reyon atanmamış GUMUS 83,25 saf / 90,0 fiziksel; stok toplamHas 2,34; pozisyon netGram 2,34 | — |

Temizlik: API ve vekil `kill`; 5160/5161/9709 dinleyen yok. `zsh kiraci-temizle.sh 01M3ZJQYE8S4CZH5CN58HR1QGB --uygula` → tenants/hareketler/stok_partileri bu kimlik için 0. tenants: başlangıç **16** → kendi kiracımla 17 → silme sonrası **17** (benimki 0; +1 eşzamanlı şeridin yeni kiracısı — listede `PAKETDEGISIM-2-TEST`; dokunulmadı). QA-TEST-* (4) dokunulmadı.

## T2-7. Gerçek tarayıcı (başsız Chrome `betikler/tur/tur.mjs`, CDP 9709, vekil 5161; `tur2/olcum-1.jsonl`, `tur2/analiz.txt`)
266 kayıt (36 sayfa + 78 sekme + 152 katman), **hata 0 · geçersiz 0**, 12 kombinasyon (1440/1280/375 × BASİT/PRO × açık/koyu).

| Rota | 1440 | 1280 | 375 | Not |
|---|---|---|---|---|
| `/` (Panel, pozisyon kartı) | temiz ×4 | temiz ×4 | temiz ×4 | taşan 0 · kırık 0 · binen 0 · gizli-görünen 0 · K5_TRY 0 |
| `/raporlar/kar-zarar` Ürünler / Grafik / Köprü / Stok | temiz ×4 | temiz ×4 | temiz ×4 | |
| `/raporlar/kar-zarar` Özet | temiz | temiz | K4 tren 3.6x4 (PRO a/k) | önceden var, TOPLU (ölçer ⓘ) |
| `/raporlar` (pozisyon kartı Bugün'de) | K12 `#rp-yil/#rp-ay` ezik (PRO) | aynı | K4 tren (PRO) | önceden var; Gün sonu sekmesi K4 — DEĞİŞİM-GÜNSONU |

Görüntüler Read ile GÖRÜLDÜ (`ham/MADEN-2-olcum/tur2/foto/`):
- `kok__BASIT_aydinlik_1440.png`: "Altın pozisyonum +2,3400 has" · altında "Vitrin 2,3400 has   Alacak 0,0000 has" — ikisi aynı noktalı alt çizgili metin, **hap/çerçeve yok**.
- `kok__BASIT_karanlik_375.png`: aynı, taşma yok.
- `raporlar__BASIT_aydinlik_1280.png`: Rapor merkezi Bugün kartı "+2,3400 has · Vitrin 2,3400 has · Alacak 0,0000 has".
- `raporlar_kar_zarar__PRO_aydinlik_1440__sekme3.png` (Ürünler): Bilezik K "0,9060 has" + sarı **kısmi** rozeti + "2 satış katılmadı"; Bilezik H "—" + "Altın maliyeti bilinmiyor"; Bilezik T "0,9060 has" notsuz; Gümüş Kolye "—" notsuz; Açılış Yüzük "—" + "Altın maliyeti bilinmiyor".
- `raporlar_kar_zarar__BASIT_karanlik_375__sekme3.png`: tablo `tablo-sar` içinde yatay kayar, sayfa yatay kaydırma 0.
- `kok__PRO_karanlik_1280.png`: PRO panelde pozisyon kartı yok (yerleşim değişmedi), "NET HAS GRAM 2,3400 has".

## T2-8. Değişen dosyalar (tur 2)
```
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                       (#pozisyon-kart .pozisyon-kirilim-oge — hap yok)
?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor      (HasMiktar; altın kırılımları Bicim.Has)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor                      (Ürünler has hücresi notu)
?? dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs    (+HasKarDurumu, +HasMaliyetiEksikSatisAdedi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs               (+3 anahtar TR+EN)
?? dotnet/Kuyumcu.Tests/MadenIkiTestleri.cs                     (MI14, MI31+, MI32)
?? dotnet/Kuyumcu.Tests/AltinPozisyonuKartiTestleri.cs / S31PanelSadeTestleri.cs (beklenen has biçimi)
```
`olcek.css` denendi ve birebir geri alındı (cmp AYNI). Dokunulan sayfalar: KarZarar.razor (tur 1'de de açıktı — gerekçe: aynı paketin doğrulayıcı bulgusu, Ürünler sekmesi). Panel.razor ve Raporlar.razor tur 2'de açılmadı.

## T2-9. Koşul karşılama (tur 2)
| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK tasarım: pozisyon kartı ana ve alt değer AYNI biçim | KARŞILANDI | MI31, B2K_01, TP1_09; M20/M22; görüntüler T2-7 |
| Alacak hapı düğme gibi görünmesin | KARŞILANDI | MI32; M21; `kok__BASIT_aydinlik_1440.png` |
| Ürünler has hücresi kısmilik/gerekçe | KARŞILANDI | MI14 tr+en; M18/M19; `…PRO_aydinlik_1440__sekme3.png` |
| KRİTİK MAS20 has-bilinen (tur 1) son kodla hâlâ korunuyor | KARŞILANDI | M1R → MAS20 + MI02/03/04/06 kırmızı; canlı = SQL |
| build 0 hata (paket dosyalarında 0 uyarı) | KARŞILANDI | `tur2-b3.log` |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | T2-5 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | T2-3 (3 kırmızı başka şeritlerin, tek başına 3/3 yeşil) |
| Canlı API + bağımsız SQL TAM/KISMI/HESAPLANAMADI | KARŞILANDI | T2-6 |
| Başsız Chrome 1440/1280/375 × BASİT/PRO × açık/koyu + Read | KARŞILANDI | T2-7 |
| Test kiracısı silindi | KARŞILANDI | T2-6 |

## T2-10. Açık kalanlar (sahibiyle)
- Panel "Bugün ne sattım … 18,4 g has" ve Rapor merkezi "18,3700 has" (Bugün kartı, ayrı has kaynağı/biçimi) → **TOPLU-5 / Gün sonu** (Panel.razor'un bu dalı dosya alanı dışında).
- `pos-fatura-mutabakati hurda.hasGrami` tüm madenleri topluyor → **DEĞİŞİM-GÜNSONU**.
- Pozisyon defter farkı uyarısı metni "{0} g" (`PozisyonSozlugu.DefterFarki`, `Katalog.PozisyonDefterFarkiBasit`) → biçim birliği için hub/TOPLU sözlük temizliği.
- Tam süitte başkasına ait kırmızılar (koşum anında): `PD2E_8_takas_kilit_sebebi…`, `S34_10_Gun_sonu_FARK_VAR…` (DEĞİŞİM-GÜNSONU), `Y4A_tur4_eszamanli_12…` (PATCH-3 / Gün sonu) — tek başına yeşil.
- Ağaçtaki uyarılar: `SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029 (sahipleri ŞUBE-KAPSAM / KEŞİF).
- Tur 1'den devam eden: hub sözlüğündeki kullanılmayan gümüş "has" metinleri, ölçer K4 "tren" (ⓘ summary), `/raporlar` K12 ezik `#rp-yil/#rp-ay`, P-30.

## T2-11. Kurucu / mali müşavir
Mevzuat hükmü yok (gösterim kuralı). Tur 1 notu geçerli.
