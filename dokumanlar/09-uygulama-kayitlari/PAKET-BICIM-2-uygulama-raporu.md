# PAKET BICIM-2 — Sayı/para girdi biçimi tek kalıp · uygulama raporu

**Tarih:** 25.09.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** BICIM-2 (test VT `kuyumcu_test_t9`, canlı API 5188, vekil 5189, CDP 9981–9984)
**Kaynak:** Kurucu 25.09 ~12:10, Satış karma ödeme ekranı: solda kalem tablosu `113.363,16 ₺`, sağda ödeme bacağı kutularında `50000` ve `63363,16`. İstek: "gibi tüm formatları gözden geçir".
**Durum:** BİTTİ. Paket kapısının yedi maddesi aşağıda kanıtlarıyla var.

---

## 1. Kök neden ve yapılan iş

**Kök neden:** `SayisalAlan` ailesi (Para, Gram, Sayı) kutuya her zaman `Bicim.Girdi(n)` basıyordu. Bu biçim `0.##########`, tr-TR ve gruplamasız. P-78 kararıyla böyle kalmıştı, çünkü `FormAlani.SayiCoz` `"1.500"` gibi belirsiz bir metni bilerek reddediyor. Kutu kendi değerini geri okuyabilsin diye gruplama hiç konmamıştı.

**Çözüm (CUMA-M `TelefonBicimli` kalıbı):**

| Durum | Kutunun `value`'su | Kaynak |
|---|---|---|
| Odak dışında | Biçimli: Para `50.000` / `63.363,16`, Gram `12,500`, Sayı `12.000` | `Bicim.ParaGirdi` / `GramGirdi` / `SayiGirdi` |
| Odakta (yazarken) | Ham, düzenlenebilir: `50000` / `63363,16`. Yazarken değer **yeniden yazılmaz**, imleç atlamaz. | `Bicim.Girdi` (P-78, değişmedi) |
| Odaktan çıkınca | Metin `SayiCoz` ile yeniden çözülür, ham normal yazıma getirilir (`50.000` → `50000`), sonra kutu biçimlenir | `SayisalAlan.OdakGitti` |
| Metin çözülemiyorsa (hata) | Kullanıcının yazdığı **aynen** kalır, alanın altında hata yazar | `KutuDegeri` |

- **P-78 ayrımı:** Odak dışındaki biçimli metin `SayiCoz`'a hiç gitmez. Değer bileşende `decimal` olarak tutulur. Örneğin kutu `1.500` gösterir ve hata çıkmaz. Kullanıcı kendisi `1.500` yazarsa bugünkü belirsizlik hatası aynen çıkar. Bu ayrımı B2_B5 ve B2_B6 testleri kilitliyor.
- **`input` olayı odak demektir:** `Degisti` içinde `odakta = true` yapılıyor. Böylece focus olayı kaçsa bile (tarayıcının otomatik doldurması, olay sırası) yazılan metin biçimle ezilmez. Bunu B2_B12 kilitliyor.
- **`data-deger` özniteliği:** Kutunun düzenlenebilir hâli (`Bicim.Girdi`, eski `value`) `data-deger`de duruyor. Bu, `Secici` ile aynı sözleşme: `SeciciYardim.SeciliDeger` önce `data-deger`i okur. Kutuların ekranda nasıl görüneceği değişti ama makine değerini okuyan testler ve betikler bozulmadı.
- **POST gövdesi değişmedi:** Gövdeye `decimal` gidiyor. B2_P1 testi `/gider` sayfasında kutuya `63.363,16` yazıyor ve gövdede `tutar` alanının JSON **sayı** olarak 63363.16 gittiğini doğruluyor. Canlıda da SQL `12345.67` döndü (§ 7).
- **Tablo ve rapor gösterim kuralları (T-K1) değişmedi.** `Bicim.cs`'e yalnız dört yeni metot eklendi: `ParaGirdi`, `GramGirdi`, `HasGirdi`, `SayiGirdi`. Mevcut imzalara dokunulmadı; B2_S5 bunu sınıyor.

## 2. Gösterim kuralları — tek kaynak `Servisler/Bicim.cs`

| Tür | Tablo/rapor (ekran) | Girdi kutusu — odak dışı | Girdi kutusu — odakta | Kâğıt belge |
|---|---|---|---|---|
| Para (TRY) | `Tl`: `50.000 ₺`, `63.363,16 ₺`. Kuruş sıfırsa yazılmaz. ₺ sonda, araya NBSP. Eksi işareti U+2212. | `ParaGirdi`: `50.000`, `1.500,50`. ₺ kutunun sağındaki son ekte. | `Girdi`: `50000`, `63363,16` | `TlKurus` / `BelgePara`: her zaman 2 hane |
| Para (döviz) | `Para(n,"USD")`: `1.250,00 $` | `ParaGirdi` + son ek kodu | `Girdi` | `BelgePara` |
| Gram | `Gram`: `12,500 g`. P-57: 3 hane, **binlik yok** | `GramGirdi`: `12,500`, `1234,500` (3 haneden fazlası yuvarlanmaz) | `Girdi`: `12,5` | `BelgeSayisi(n, locale, 3)` |
| Has | `Has`: `77,6014 has` (4 hane, binlik yok) | `HasGirdi`: `77,6014`. Has için ayrı bir girdi bileşeni yok; has sunucuda türetilir (UX-S21). | — | — |
| Milyem | Tam sayı, binliksiz (`916`, `1000`) | Sayı kutusu (1000 altında gruplama devreye girmez) | — | — |
| Yüzde | `Yuzde`: `%12,5`, `%25` | Sayı kutusu, son ek `%` | `Girdi` | — |
| Adet/miktar | `Miktar`: `4,25 adet`, `1,000 g` | `SayiGirdi`: `12.000` | `Girdi` | — |
| Kur şeridi | `TlTam` (HAS/GRAM22: `6.877 ₺`) · `TlKurus` (USD/EUR: `41,95 ₺`) | — | — | — |
| Tarih | `Tarih(t, locale)`: `25.09.2026` · makine: `Makine(DateOnly)` → `2026-09-25` | — | — | — |
| Saat | `Saat`: `09:02` (her dilde iki nokta) | — | — | — |
| Telefon | `TelefonBicimi.Girdi`: `0532 411 22 33` (CUMA-M; kutudan çıkınca) | aynı | ham | — |
| IBAN | Olduğu gibi (`TR…`). Sayı kuralına girmez; `bicim-olc` bunu hariç tutar. | — | — | — |
| Makine (SVG / sorgu / JSON) | `Makine`: nokta ondalık, gruplamasız, Invariant | — | — | — |

## 3. Spesifikasyon doğrulaması ve sapmalar

- **Spesifikasyon dosyası:** İstem `Servisler/Bicim.cs`'i "spesifikasyon" olarak gösteriyor. O dosyada "Birleşik koşullar" başlıklı bir bölüm **yok**. Bağlayıcı koşullar istemdeki "EK KOŞULLAR"dan alındı.
- **İddialar doğrulandı:** `Bicim.Girdi` = `0.##########` (satır 170 civarı). Para, Gram ve Sayı `value="@ham"` basıyordu. Satış karma ödemede bacak tutarı `FisOdemeBacaklari.razor:67` bir **`<Para>` bileşeni**, ham `<input>` değil. Bu yüzden düzeltme doğrudan Satış ekranına yansıdı ve Satış kanıtı için yedek ekranlara (`/kasalar` vb.) gerek kalmadı; yine de `/kasalar` ve `/hurda` da gösterildi.
- **Gram kuralı:** Kabul maddesi "Gram: Bicim.cs'teki gram gösterim kuralı ile AYNI" diyor. `Bicim.Gram` P-57 gereği 3 hane ve **binliksiz**. Girdi de böyle yapıldı (`1234,500`). `bicim-olc.js` gramda binliği ihlal saymıyor; gram kutusunda en az 3 hane arıyor. (Kapsam metnindeki "gruplanmamış ≥4 haneli tutar/gram" ifadesi P-57 ile çelişiyor. Tek kaynak kuralı gereği P-57 esas alındı.)
- **Kuruş kuralı:** Kuruş 2 haneden fazlaysa (ör. `12,345`) yuvarlanmıyor. Kutu kullanıcının değerini saklamaz, gösterir.
- **Test beklentisi güncellemeleri (davranış bilerek değişti):** Odak dışındaki kutu değerini ham olarak bekleyen 13 doğrulama satırı yeni biçime getirildi, her birinin yanında `// BICIM-2` notu var. Dosyalar: `S3aEkranTestleri.cs:99`, `P16EkranTestleri.cs:152,227`, `UX4bEkranTestleri.cs:129,140,152,355`, `B4AltinBacagiEkranTestleri.cs:162`, `EmanetKurEkranTestleri.cs:321,323`, `S21FisBilesenTestleri.cs:474`, `P20EkranTestleri.cs:163,171`, `PosTeklifEkranTestleri.cs:92,126,129`. `SeciliDeger` kullanan yaklaşık 90 test `data-deger` sayesinde **değişmeden** yeşil kaldı. Bu dosyaların bir kısmı başka şeritlerin test dosyası; dokunulan satır yalnızca beklenen değer.
- **Mevzuat:** Bu pakette mevzuat hükmü yok. Yalnız biçim var; vergi, KDV ya da eşik kuralına dokunulmadı.
- **Migration:** Gerekmedi, şema değişmedi.

## 4. DoD sözleşmesi

Yeni bir varlık ya da kayıt türü yok, yeni uç yok. PATCH, DELETE, yetki ve denetim kaydı maddeleri **uygulanmaz**. Ekran sözleşmesi korundu:
- Mevcut element id'leri değişmedi. Yeni id yok; yalnız `data-deger` özniteliği eklendi.
- Yeni CSS yok, satır içi style yok. Denetimde satır içi style sayısı 105, tavan 196.
- Hata bandı ve alan altı mesaj akışı aynı: çözülemeyen metin alanın altında Türkçe hata verir ve kutu yazılanı gösterir (UX-S28, DoD EK md.3).

## 5. Testler

**Yeni: `Kuyumcu.Tests/Bicim2GirdiBicimiTestleri.cs` — 31 test**
- Saf testler, 18: `B2_S1` ParaGirdi (8 durum), `B2_S2` GramGirdi = Bicim.Gram (3), `B2_S3` HasGirdi = Bicim.Has (2), `B2_S4` SayiGirdi (3), `B2_S5` T-K1 ve Girdi değişmedi, `B2_S6` "1.500" belirsiz kalır.
- bUnit, 12: `B2_B1` odak dışı gruplu, odakta ham, `data-deger`. `B2_B2` `63363,16` → blur'da `63.363,16`. `B2_B3` `1500,5` → `1.500,50`. `B2_B4` yazarken değer yeniden yazılmaz. `B2_B5` odak dışı `1.500` SayiCoz'a gitmez. `B2_B6` P-78 belirsizliği korunur. `B2_B7` blur'da SayiCoz normalleştirir. `B2_B8` çözülemeyen metin aynen kalır. `B2_B9` Gram. `B2_B10` Sayı. `B2_B11` dışarıdan gelen değer. `B2_B12` focus kaçsa da yazılan ezilmez.
- Sayfa, 1: `B2_P1` `/gider` kutusu `63.363,16`, POST gövdesinde `tutar` JSON sayısı 63363.16.

**Filtreli koşum (yerinde, son derleme sonrası):** Bicim2 + GirdiAilesi + P78 + SayisalAlanGidisDonus + beklentisi güncellenen ya da etkilenen 26 sınıf → **482/482 başarılı, 0 başarısız.**

**Tam süit, önce ve sonra (yalıtılmış kopya):** Aynı ağaçta birden çok şerit aynı `bin/` klasörünü yeniden derliyor. Bu yüzden yerinde koşulan tam süit bozuk çıkıyor (ilk yerinde koşumda 1.068 kırmızı: `FileNotFoundException` ve `Kuyumcu.Api.deps.json bulunamadı`, yani başka şeridin derlemesi koşunun ortasında). Karşılaştırma için ağacın **aynı anlık görüntüsü** iki kopyaya alındı: `scratchpad/b2/izole/{taban,sonra}`. `taban` kopyasında BICIM-2'nin beş kaynak dosyası orijinal hâline döndürüldü.

| Koşum | Toplam | Başarılı | Başarısız | trx |
|---|---|---|---|---|
| TABAN (BICIM-2 öncesi, aynı anlık görüntü) | 6150 | 6150 | 0 | `scratchpad/trx-pBICIM-2/taban/` |
| SONRA-1 (ilk hâl) | 6180 | 6076 | 104 | `…/sonra/`: 104'ünün **hepsi** eski ham değeri bekliyordu (`"10000"` → `"10.000"` gibi) |
| SONRA-2 (`data-deger` + `input` = odak + 9 beklenti) | 6181 | 6178 | 3 | `…/sonra2/`: iki satır daha (`PosTeklif:129`, `EmanetKur:323`), bir de `UyumEkranTestleri.Bildirim_formunda_300…`. Sonuncusu tek başına üç kez koşuldu, üçünde de yeşil. |
| **SONRA-3 (son hâl)** | **6181** | **6181** | **0** | `…/sonra3/` |

→ **Bu paketin ürettiği yeni kırmızı: 0** (6181 − 6150 = 31 yeni BICIM-2 testi).

Yerinde tam süit (adım 6 komutu, `…/yerinde/`): sonucu § 9'da.

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/BICIM-2/` altına kopyalandı. `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı. Filtreli test koşuldu. Yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu doğrulandı. Günlükler `scratchpad/b2/MBICIM2-*.log`.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı (cmp) |
|---|---|---|---|
| MBICIM2-1 | **Odak dışı biçimi kapat**: `KutuDegeri`'nde `? OdakDisiBicim(d)` → `? ham` | B2_B1, B2_B2, B2_B3, B2_B5, B2_B7, B2_B9, B2_B10, B2_B11, B2_B12, B2_P1 (10) | AYNI |
| MBICIM2-2 | **Blur'da SayiCoz'u atla**: `OdakGitti` içinde `var cozum = SayiCoz(ham)` → `new SayiCozumu(SayiSonucu.Bos, null)` | B2_B7 | AYNI |
| MBICIM2-3 | Odak gelince ham'a dönme: `OdakGeldi() => odakta = false` | B2_B1, B2_B2, B2_B5, B2_B7, B2_B9, B2_B10 | AYNI |
| MBICIM2-4 | Kuruş kuralı: `"#,##0.00########"` → `"#,##0.##########"` | B2_B3, B2_S1("0.5"), B2_S1("1500.5") | AYNI |
| MBICIM2-5 | `Degisti` içinde `odakta = true` → `odakta = odakta` | B2_B12 | AYNI |

M1–M4 son koddan sonra (`data-deger` ve `input` = odak eklendikten sonra) yeniden koşuldu (`MBICIM2-*r.log`); sonuç aynı. Testleri kırmayan mutasyon kalmadı, test açığı yok. Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln` → **0 uyarı, 0 hata.**

## 7. Canlı doğrulama (API 5188, kiracı `PAKETBICIM-2-TEST Kuyumculuk`)

Kurulum: sistem anahtarı rastgele üretildi (chmod 600, çıktıya yazılmadı). `POST /sistem/davetler` 201, `POST /auth/kayit` 201, `POST /auth/giris` 200. Ardından: `POST /kasalar` 200 ×2, `POST /kasalar/{id}/hareket` 200 (açılış 1.250.000), `POST /cari` 201, `POST /urunler` 201 ×2, `POST /islemler/alis` 200 ×2, `POST /islemler/satis` 200. İlk denemelerde `POST /islemler/alis` iki kez **400** döndü: biri MASAK 185.000 TL kimlik eşiği, biri tevsik 30.000 TL haddi. İkisi de beklenen kapı; tutarlar eşik altına indirilerek geçildi. Oturum `inceleme-vekili.py` (5189) ile parola yazılmadan açıldı; başsız Chrome ve ham CDP kullanıldı (`betikler/tur/cdp.mjs`, tarayıcı bölmesi kullanılmadı).

**Satış karma ödeme (kurucunun ekranı):** Kalem `113.363,16 ₺`. Bacak 1 Havale: kutuya odaklanınca `113363,16` (ham), `50000` yazılınca `50000` (gruplanmadı). Blur sonrası **`50.000`**. Bacak 2 Kart: kalan otomatik **`63.363,16`**.

| Mod | Tema | Genişlik | Bacak 1 | Bacak 2 | Yatay kaydırma | K1 gizli | K3 taşan/kırık | K5 TRY | K9 binen | bicim-olc ortak |
|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1440 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| PRO | açık | 1280 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| PRO | açık | 375 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| PRO | koyu | 1280 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| PRO | koyu | 375 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| BASİT | açık | 1440 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| BASİT | açık | 1280 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| BASİT | açık | 375 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| BASİT | koyu | 1280 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |
| BASİT | koyu | 375 | 50.000 | 63.363,16 | yok | 0 | 0/0 | 0 | 0 | 0 |

Ham ölçüm: `scratchpad/b2/satis-olcum.jsonl`. Ölçümde BICIM-2 dışından gelen üç bulgu var, hiçbiri Para kutusu değil:
- K12 ezik: `#islem-bacak-1-taksit` 48–52 px (Secici; SATIS/KATMAN).
- K10b kesik: barkod hücresi `K8R4R04WKFC` ve `Kasa seçin...` (Secici metni).
- K7 hizasız: BASİT'te 1.

Ekran görüntüleri Read ile incelendi. Klasör: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/`
- `BICIM-2-satis-karma-PRO-aydinlik-1440.png`: kutular `50.000` ve `63.363,16`, tablo `113.363,16 ₺`.
- `BICIM-2-satis-karma-{PRO,BASIT}-aydinlik-1440-ODAKTA.png`: odaktaki kutu `50000`.
- `BICIM-2-satis-karma-{BASIT,PRO}-{aydinlik,karanlik}-{1280,375}.png` ve `…-BASIT-aydinlik-1440.png`
- `BICIM-2-hurda-PRO-aydinlik-1440.png`: Gram `1234,500`, kurdan gelen fiyat `6.766,04`.
- `BICIM-2-kasa-hareket-1440.png`: `/kasalar` hareket çekmecesinde Tutar `12.345,67`.

**Uçtan uca gövde ve SQL:** `/kasalar` → "+ Hareket" → Tutar kutusuna `12345,67` yazıldı. Blur sonrası kutu `12.345,67` gösterdi, sonra Kaydet'e basıldı. Bant: "Gelir 12.345,67 ₺ kaydedildi. Yeni bakiye: 1.217.440,84 ₺".
```
SELECT tutar, para_birimi, odeme_turu, aciklama FROM kasa_hareketleri WHERE tenant_id=<kiracı> ORDER BY olusturuldu;
… 12345.67  TRY  NAKIT  BICIM2 canli
```
Biçimli gösterim gövdeye sızmadı; veritabanına makine değeri yazıldı.

**Temizlik:** API (5188) ve vekil (5189) yalnız kendi PID'leriyle durduruldu; `lsof` ile 5188 ve 5189'un boş olduğu doğrulandı. Kiracı, `tenant_id` kolonu olan 70 tablonun hepsinden silindi, ardından `tenants` satırı ve `kiraci_davetleri` satırı da silindi. Son sayımda `kiracı=0`, `kasa_hareketleri=0`, davet=0, kullanıcı=0. `tenants` başlangıçta **16**, sonda **17**. Farkın hepsi başka şeritlerden: PAKETSATIS-2-TEST'in eski kiracısı silindi, PAKETSADE-1-TEST ve PAKETISTEK-1-TEST eklendi. BICIM-2'nin net katkısı 0. QA-TEST-* kiracılarına dokunulmadı.

## 8. Biçim denetimi — `betikler/tur/bicim-olc.js` (yeni) · 42 rota × BASİT/PRO × 1440

Ölçer görünür metinde ve odak dışındaki `input` değerlerinde şunları arıyor: GRUPSUZ (≥4 haneli binliksiz), NOKTA_ONDALIK, TL_ONDE, TRY, HAS_HANE, GRAM_HANE. Hariç tutulanlar: tarih, saat, yıl, telefon, TCKN/VKN, IBAN, ≥9 haneli barkod, harf ya da tireyle bitişik fiş/belge no, "WCAG 2.1" gibi kısaltma+sürüm. Sürücü her sayfadaki boş ve görünür ortak `SayisalAlan` kutularını gerçek klavye olayıyla dolduruyor (odak → `Input.insertText` → blur), kayıt yapmıyor. Taban ölçümü MBICIM2-1'e eşdeğer derlemeyle alındı (odak dışı biçim kapalı, eski davranış), sonra ölçümü son kodla. Ham veri: `scratchpad/b2/{taban,sonra}-{BASIT,PRO}.jsonl`.

| Koşum | Rota | Ortak bileşen kaynaklı | Sayfa kaynaklı | Toplam |
|---|---|---|---|---|
| TABAN BASİT | 42 | **5** | 10 | 15 |
| TABAN PRO | 42 | **5** | 7 | 12 |
| SONRA BASİT | 42 | **0** | 10 | 10 |
| SONRA PRO | 42 (41 PRO + `/kurulum` mod düğmesi yok) | **0** | 7 | 7 |

Tabanda ortak bileşenden gelen ihlaller: `/gider #gd-tutar 63363,16`, `/hurda #hrd-fiyat 6743,46` (kurdan dolan fiyat), `/hurda #hrd-tahsil 63363,16`, `/hurda #hrd-has`/`#hrd-kabul-has 1234,5`. Sonrasında **0**.

**Kalan sayfa kaynaklı bulgular (başka şeridin dosyaları, DEVİR — dokunulmadı):**

| Rota | Bulgu | Dosya:satır | Sahibi |
|---|---|---|---|
| `/raporlar/kar-zarar` | Has değeri gram gibi basılıyor, 4 hane: `1,8300 g`, `13,9232 g`. Doğrusu `Bicim.Has` → `1,8300 has`. | `Pages/KarZarar.razor:102,103,105,113,316,335` (`{x:N4} g`) | ORTAK-1 |
| `/` (Panel) | Has 1 hane + "g has": `13,9 g has` | `Pages/Panel.razor:394` → `HasMetni` (`:785`) | ORTAK-1 |
| `/raporlar` | Has 1 hane: `13,9 g has`, `68,1 g has` | `Pages/Raporlar.razor:1094,1127` → `BugunHas` (`:1021`, kendi `CultureInfo`'su) | CUMA-J |
| `/islemler` BASİT | Gram 2 hane: `Toplam · 0,00 g` / `15,20 g` | `Bilesenler/Fis/FisVerdimAldim.razor:39` (`ToString("#,0.00")`) | SATIS-2/3 |
| `/islemler` PRO | Has gram 4 hane + "g": `13,9232 g` | `Bilesenler/Fis/FisProKalemKarti.razor:36` (`FisHesap.HasSayi`) | SATIS-2/3 |
| `/veri-ice-aktarim` | Binliksiz satır sınırı: `(… · 5000)` | `Pages/VeriIceAktarim.razor:93` (`@sablon.MaksSatir`) | CUMA-J |
| `/kur` | `1000`: 24 ayar **milyemi**. Milyem kuralı binliksiz olduğu için ihlal değil; ölçerin bilinen yanlış pozitifi. | `Pages/Kur.razor:169` | SADE-1 (gerekmez) |

## 9. Denetim ve tam süit (yerinde)

- `dotnet/denetim.sh` (son kod): **287 ✓, 1 ✗.** BICIM-2 sayaçlarının hepsi ✓: `GIRDI_AILESI_DISI_BICIM 0`, `GIRDI_AILESI_TAM 15/15`, `GIRDI_AILESI_TEK_KAYNAK 1`, `GIRDI_AILESI_KULLANIM 375`, satır içi style 105/196, `MODAL_GOVDE_DISI_KAYDIRMA 0`.
  - Tek ✗: `P-23: ODEME_KALEMSIZ_YOL (canlı) = 1`. Bu bir **canlı veritabanı** ölçümü. Hareket `01M3C30P6YJWMV5Q9SK89PP42W`, kiracı `PAKETSATIS-2-TEST Kuyumculuk`, SATIS, 10:50:42, nakit 2400. Satırı SATIS-2 şeridinin şu an koşan canlı test kiracısı üretti; BICIM-2 ile ilgisi yok (BICIM-2 sunucu kodu yazmadı). BICIM-2'nin ilk denetim koşumunda (bu satır doğmadan önce) sonuç **DENETİM TEMİZ** idi (`scratchpad/b2/denetim.log`).
- Yerinde tam süit (adım 6 komutu, `trx-pBICIM-2/yerinde/`, son kod): **6189 toplam, 6185 başarılı, 4 başarısız.** Dört kırmızının hiçbiri biçimle ilgili değil ve dördü de aynı ikili ile hemen yeniden koşulduğunda **yeşil** (29/29):
  - `SATIS2EkranTestleri.BASIT_Duzelt_ters_kayit_YOK_PRO_kilitli_ve_sebebi_title` (SATIS-2, düğme koleksiyonu)
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` (sabit VT adı `kuyumcu_t1_hazir`: 'Unknown database'. Paralel şeritler aynı fikstür VT'sini paylaşıyor)
  - `KatmanTekKalipTestleri.K1_15` ve `K1_17` (KATMAN, panel konumlama)

  Bunlar paralel koşumdan kaynaklanan kararsızlıklar; sahipleri parantez içinde. Yalıtılmış karşılaştırmada (§ 5) üçü de yok (6181/6181).

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` → `Bicim.cs` ` M`; `Bilesenler/Form/` ve `Kuyumcu.Tests/` git'te izlenmiyor (`??`). BICIM-2'nin dokunduğu dosyalar:
- `dotnet/Kuyumcu.Web/Servisler/Bicim.cs`: ParaGirdi, GramGirdi, HasGirdi, SayiGirdi eklendi.
- `dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs`: odakta, KutuDegeri, OdakGeldi, OdakGitti, OdakDisiBicim, `input` = odak.
- `dotnet/Kuyumcu.Web/Bilesenler/Form/Para.razor`, `Gram.razor`, `Sayi.razor`: `value="@KutuDegeri"`, `data-deger`, `@onfocus`/`@onblur`, OdakDisiBicim.
- `dotnet/Kuyumcu.Tests/Bicim2GirdiBicimiTestleri.cs`: yeni, 31 test.
- Beklenti satırları güncellenen testler: `S3aEkranTestleri.cs`, `P16EkranTestleri.cs`, `UX4bEkranTestleri.cs`, `B4AltinBacagiEkranTestleri.cs`, `EmanetKurEkranTestleri.cs`, `S21FisBilesenTestleri.cs`, `P20EkranTestleri.cs`, `PosTeklifEkranTestleri.cs`.
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/bicim-olc.js`: yeni. `ekstra.js` ve `tur.mjs`'e dokunulmadı.
- `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/BICIM-2-*.png`: 14 görüntü.
- Bu rapor.

**Dokunulan sayfalar (DoD EK 2):** Yok. Hiçbir `Pages/*.razor` ya da `Bilesenler/Fis/*` dosyası açılmadı; değişiklik ortak Form bileşenlerinde.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Odak dışı biçimli, odakta ham, blur'da SayiCoz + biçim | ✓ | B2_B1–B2_B12, canlı 10 ölçüm |
| `50000` → `50.000`, `63363,16` → `63.363,16`, `1500,5` → `1.500,50` | ✓ | B2_B1, B2_B2, B2_B3, canlı Satış |
| Yazarken değer değişmez | ✓ | B2_B4, B2_B12, canlı "yazarken=50000" |
| SayiCoz belirsizlik (P-78) korunur; biçimli metin SayiCoz'a gitmez | ✓ | B2_B5, B2_B6, B2_S6 |
| POST gövdesindeki decimal değişmedi | ✓ | B2_P1 (JSON sayı 63363.16), SQL `12345.67` |
| Gram = Bicim.Gram kuralı, Has 4 hane, Sayı gruplu | ✓ | B2_S2, B2_S3, B2_S4, B2_B9, B2_B10 |
| T-K1 tablo kuralları ve mevcut Bicim testleri yeşil | ✓ | B2_S5; yalıtılmış tam süit 6181/6181 |
| `bicim-olc.js` yeni; `ekstra.js`'e dokunulmadı | ✓ | § 8 |
| 42 rota × BASİT/PRO, taban ve sonra; ortak kaynaklı ihlal 0 | ✓ | 5 → 0 (her iki mod) |
| Kalanlar sahibiyle, dosya:satır ile listelendi | ✓ | § 8 tablo |
| Gerçek tarayıcı: Satış karma ödeme `50.000` / `63.363,16`, 1280+375 × BASİT/PRO × açık/koyu | ✓ | § 7 tablo ve görüntüler |
| Mutasyon: odak dışı biçim kapalı → kırmızı; blur'da SayiCoz atlanınca → kırmızı | ✓ | MBICIM2-1, MBICIM2-2 (+3, 4, 5) |
| Gösterim kuralları tablosu | ✓ | § 2 |
| Yeni kırmızı yok (paket kapısı md.7) | ✓ | TABAN 6150/0 → SONRA 6181/0 |

## 12. Açık kalanlar

1. **Sayfa kaynaklı biçim ihlalleri** (§ 8; devir, kendi şeritlerinin dosyaları):
   - ORTAK-1: `KarZarar.razor` (has `N4 g`), `Panel.razor:785`.
   - CUMA-J: `Raporlar.razor:1021`, `VeriIceAktarim.razor:93`.
   - SATIS-2/3: `FisVerdimAldim.razor:39`, `FisProKalemKarti.razor:36`.
2. **`denetim.sh` P-23 ODEME_KALEMSIZ_YOL = 1:** SATIS-2 şeridinin canlı test kiracısının satışı. Sahibi SATIS-2; o şerit kiracısını silince sıfırlanır.
3. **Ham `type=number` kutuları** (Sayim, Faturalar, Pos, Siparisler, KasaRaporu) ve Fiş'teki ham `<input>`'lar: kapsam gereği SATIS-3 ve ilgili sayfa şeritlerinde. Form bileşenine geçtiklerinde bu biçimi kendiliğinden alırlar. Bugünkü açık sayfalarda `bicim-olc` bunlarda değerli bir ihlal yakalamadı: kutular boş açılıyor ve sürücü yalnız ortak bileşeni dolduruyor.
4. **`/hurda` "Bizim kaydımız (has gram)" alanı** `Gram` bileşeniyle çiziliyor ve 3 hane gösteriyor. Has 4 hane olduğu için ileride `Has` girdisi (`HasGirdi` hazır) düşünülebilir. Sahibi SADE-1 (Hurda).
5. **Ölçer sınırı:** `/kur` milyem `1000` yanlış pozitif. Kapalı çekmece ve katmanlardaki Para kutuları sayfa açılışında görünmediği için turda ölçülmedi. Aynı bileşen `/kasalar` çekmecesinde elle doğrulandı.

## 13. Kurucuya / mali müşavire notlar

- Mali müşavir teyidi gerektiren bir hüküm yok; yalnız ekran biçimi değişti. Kâğıt belge biçimleri (`TlKurus`/`BelgePara`, VUK gereği kuruş iki hane) **değişmedi**.
- Kurucuya kısaca: kutular artık tablo ile aynı dili konuşuyor, `50.000` ve `63.363,16`. Kutuya tıklayınca rakamlar düz yazılabilir hâle geliyor (`50000`), çıkınca yeniden noktalı görünüyor. Kullanıcı kendisi `1.500` yazarsa sistem "1,5 mi 1500 mü?" diye soruyor (P-78, değişmedi).
