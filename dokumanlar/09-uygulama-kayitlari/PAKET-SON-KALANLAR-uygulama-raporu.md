# PAKET SON-KALANLAR — uygulama raporu (03.10 · t2 · 5162/5163)

Roller: Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-PATCH-5-uygulama-raporu.md` ve istemdeki PATCH-5 hasım/hakem bulguları (tamamı, 5199 hariç).
Kanıtlar: `ham/SON-KALANLAR-olcum/`. Bu dizinde canlı betik ve çıktısı (`canli-sk.mjs`, `canli-sk-kosum.txt`), mutasyon sürücüsü ile sonuçları (`mut.py`, `sonuc-hepsi.json`, `mut-tablo.txt`), ekran ölçüm sürücüsü (`olc-sk.mjs`), `olcum.jsonl`, `olcum-tablo.md`, 84 ekran görüntüsü (`foto/`) ve denetim çıktısı (`denetim.txt`) var.
Geçici dosyalar `/private/tmp/kuyumcu-serit/SON-KALANLAR/` altında; TRX dosyaları `/private/tmp/kuyumcu-serit/trx-pSON-KALANLAR/` altında. Proje ağacına geçici dosya bırakılmadı. COMMIT yapılmadı.

## 1. Spesifikasyon ile kodun karşılaştırılması (iddialar hâlâ doğru mu)

| Bulgu | Kodda bulunan (iş öncesi) | Sonuç |
|---|---|---|
| D-P5-1 banka para birimi | `BankaModulu.HesapGuncelle`: `if (dto.ParaBirimi is not null) hesap.ParaBirimi = dto.ParaBirimi;` — kilit yoktu | DOĞRU → düzeltildi |
| Gün Kapat gerekçe 400 alansız | `GunKapat` içindeki BadRequest'te `.AlanIle` yoktu; ileti "TL" ve "ZORUNLUDUR" içeriyordu | DOĞRU → düzeltildi |
| Yarım kuruş eksi sayım | `IsaretliTutar` önce yuvarlıyor, `< 0` kontrolü sonra yapılıyordu | DOĞRU → düzeltildi |
| 0,004 / 0,001 iletisi çelişiyor | DTO `Range(0.01…)` iletisi "Tutar sıfırdan büyük olmalıdır"; `PozitifTutar` iletisi "… sıfırdan büyük …"; tahsilatta "en az bir tutar girilmelidir (TL veya has gram)" | DOĞRU → düzeltildi |
| Tavan için iki cümle | `GunKapanisiDto` `[Range(0, UstTutar, "… 0 ile 9.999.999.999.999,99 arasında …")]`; diğer uçlar `KolonTavani.Denetle` iletisini kullanıyor | DOĞRU → düzeltildi |
| Strict iletisi ASCII ve teknik | `DogrulamaSozlugu`'nda `SayilanNakit` yoktu; PascalCase çözümü "Sayilan nakit" üretiyordu; ileti "tırnaksız sayı gönderin (ondalık ayırıcı nokta)…" | DOĞRU → düzeltildi |
| Çift hata gösterimi | `Kasalar.razor` hareket catch bloğu `SunucuMesaji` (bant) ve `Sunucudan` (alan) çağrılarını birlikte yapıyordu | DOĞRU → düzeltildi (ortak kalıp) |
| Kapanmış güne yazma iletisi | `GunKapanisiKapisi.cs:35` iletisi "KAPATILMIŞTIR … TL … SAHİP yetkisiyle" içeriyordu; kartta işaret yoktu, düğmeler açıktı | DOĞRU → düzeltildi |
| İki çekmece düzeni / tahsilat ModalPencere | Hareket çekmecesinde `KaydetGoster="false"` ve gövde içinde Kaydet vardı; tahsilat `ModalPencere` ile açılıyordu | DOĞRU → düzeltildi |
| Gider geçmişi açık | Giderler ve para aktarımları listeleri açık kart olarak geliyordu; açıklama kutusu da açıktı | DOĞRU → düzeltildi |
| Tahsilat modalı | `form-grid-2` içinde dar tutar kutusu vardı; "Kasa seçin" üç yerde yazıyordu (yer tutucu, `#th-kasa-gerekli`, kilit satırı); `tlTutari` hatası yalnız bantta görünüyordu | DOĞRU → düzeltildi |
| Fark yönü | `#gun-fark` işaretsiz kırmızı tutar gösteriyordu | DOĞRU → düzeltildi |

### Sapmalar ve gerekçeleri
1. **Para birimi kilidi HTTP kodu: 409 alan:paraBirimi (VERI satırında "400" yazıyor).** Hasım bulgusu (D-P5-1) açıkça "Kasa.GecmisiBosMu kuralının eşi … değilse 409" diyor. Kasadaki mevcut eşi 409 (ConflictException) döndürüyor. Bunu 400'e çevirmek kasanın mevcut sözleşmesini ve testlerini değiştirirdi. Durum kaynaklı bir red için anlamca doğru kod 409'dur. VERI maddesinin özü korundu: alan `paraBirimi` gövdede var, SQL değişmiyor. Ayrıca kasanın 409'una da artık alan ekleniyor (`paraBirimi`, şube değişiminde `subeId`). Ekranın eşleme sözlüğü (`["ParaBirimi"]="d-doviz"`) bu alanı yakalıyor.
2. **Gün Kapat DTO'sundaki `[Range]` kaldırıldı.** Aynı tavan kuralının iki cümlesi olmasının kaynağı bu öznitelikti. Eksi ve tavan kontrolü artık TEK yerde, serviste: HTTP ve servis yolu aynı iletiyi veriyor. Değişmez kural ("DTO DataAnnotations yalnız controller yolunda → servis içi elle kontrol") zaten servisi asıl kapı yapıyordu.
3. **Kapanmış gün iletisinde tarih korundu.** Spesifikasyonun örnek iletisi "bugünkü günü" diyordu. Storno gibi geçmiş güne yazan yollarda gün "bugün" olmayabilir, bu yüzden ileti tarihi taşıyor: "Bu kasanın 03.10.2026 günü kapandı (sayılan 1.300,00 ₺). Kapanmış güne kayıt yazılamaz; yeniden açmak için işletme sahibine başvurun." Kasa kartındaki rozet metni spesifikasyondaki örnekle birebir aynı.
4. **Gün Kapat gerekçe iletisinde kart/havale ayrımı korundu.** PARA-DEMO madde 1 kuralı bunu istiyor ve `ParaDemoTestleri.PD1` bunu sabitliyor. Biçim sade hâle getirildi: ₺, yön, büyük harf yok.
5. **Gider geçmiş akordeonlarının başlıkları yeni ad değil, mevcut liste adı** ("Giderler", BASİT'te "Para aktarımları"). UX-S31 aynı kavram için tek ad istiyor. Ortak `Bilesenler/Katlanir.razor` hazırdı ve kullanıldı; geçici yerel katlanır yazılmadı.
6. **Tahsilat altlığında "Kasa seçin" iki kez görünüyor:** biri seçicinin yer tutucusu (uyarı değil, kutunun içi), diğeri altlıktaki tek kilit sebebi. Gövdedeki `#th-kasa-gerekli` satırı kaldırıldı. Bu satıra dayanan `UX4bEkranTestleri.Kasasiz_tahsilat_KAYDEDILEMEZ_ve_sebebi_yazar` testi altlığı sınayacak şekilde güncellendi.

## 2. Yapılanlar

### Veri / sunucu (Taşkın)
- **BankaModulu.HesapGuncelle:** para birimi gerçekten değişiyorsa `ParaBirimiKilidi` çalışır. Bakiye ≠ 0 ise 409 alan:paraBirimi döner. Bakiye 0 ama `banka_hareketleri` doluysa da 409 alan:paraBirimi döner. Kapı hiçbir yazmadan önce çalışır. Aynı değeri göndermek kilide takılmaz.
- **KasaModulu.Guncelle → GecmisiBosMu:** 409'lara alan eklendi.
- **KasaModulu.GunKapat:** eksi kontrolü ham değere yapılıyor (−0,004 artık 0 sayılmıyor). Fark gerekçesi reddi `.AlanIle("aciklama")` taşıyor; ileti yönlü ve ₺'li. Örnek: "Sayılan nakit beklenenden 200,00 ₺ fazla (beklenen nakit 1.000,00 ₺). Farkın nedenini yazın." DTO'daki `[Range]` kaldırıldı (bkz. sapma 2).
- **GunKapanisiKapisi:** ileti sadeleştirildi. Kasanın para simgesi DB'den okunuyor. Ortak `Para(tutar, paraBirimi)` biçimleyicisi eklendi (tr-TR, ₺/$/€/£).
- **KasaModulu.NakitAyrimlari:** `KasaNakitAyrimiOzeti` kaydına `BugunKapali` eklendi. İsteğe bağlı bir sonuç parametresidir, geriye uyumludur. Ölçüt `GunKapanisiKapisi` ile aynı: bugünün yerel günü ve `acilis_zamani IS NULL`. Sorgu `.Take(idler.Count)` ile sınırlı (B-8).
- **KolonTavani:** `EnAzTutarMesaji(ad)` / `EnAzTutar` eklendi. İleti: "Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez". Kullanan yerler: `PozitifTutar`, ayrıca Kasa, Banka, Gider ve Virman DTO'larındaki `Range(0.01…)` iletileri.
- **CariModulu.TahsilatGir:** üç ayrı ileti var. Kullanıcı TL yazdıysa "en az 0,01" (alan tlTutari). Altın yazdıysa "Altın miktarı en az 0,000001 gram…" (alan hasGrami). Hiçbir şey yazmadıysa "Tutar ya da altın girin." "has gram" ifadesi çıkarıldı.
- **Program.cs JsonHataMesaji:** ileti "{Alan} geçerli bir sayı değil — sayıyı rakamla yazın, ör. 1500,50". Teknik çözücü metni `JSON_SAYI_COZULEMEDI` olarak günlüğe Information düzeyinde yazılıyor. **DogrulamaSozlugu:** `SayilanNakit`, `KomisyonOran` ve `IskontoYuzde` Türkçe yazımla eklendi.

### Ekran (Ertuğ)
- **Ortak alan hatası kalıbı:** `FormDurumu.SunucuHatasi(ex, dil, esleme)` eklendi. Alan hataları deftere yazılır; bant yalnız eşlenemeyen hatayı gösterir. Eşleme yalnız ÇİZİLİ alanları içerir; çizili olmayan alanın mesajı bantta kalır, yani yutulmaz (UX-S28).
- **Kasalar.razor:**
  - Kasa hareketi çekmecesinde Kaydet altlığa taşındı (İptal → Kaydet, `#h-kaydet` kimliği aynen). Tutar ve açıklama alanı hataları yalnız alan altında görünür. Kasa kapısı reddi bantta kalır.
  - Gün Kapat'ta sayılan nakit ve gerekçe hataları alan altında görünür. Fark yönlü yazılır ("196.050 ₺ fazla" / "… eksik").
  - Bugünkü günü kapanmış kasanın kartında "Bugün kapandı" rozeti ve sebep satırı var. "+ Hareket" ve "Günü Kapat" düğmeleri disabled; title ve `aria-describedby` sebebi gösteriyor.
- **Cariler.razor:** tahsilat `ModalPencere` → `Cekmece` (Varlik `tahsilat`). `#th-kaydet` ve `#th-kaydet-kilit` kimlikleri aynen. Tutar kutusu tam genişlik, ızgaradan çıkarıldı. Kilit sebebi tek yerde (altlık). Sunucunun `tlTutari`/`hasGrami` reddi kutunun altında görünür ve kutuyu kırmızıya çevirir; kullanıcı yazınca silinir. Alan dışı ret bantta kalır.
- **Gider.razor:** "Giderler" ve "Para aktarımları" geçmişleri ortak `Katlanir` ile KAPALI açılıyor; sayı rozeti gerçek. Açıklama kutusu "+ Not ekle" arkasında; yazılmışsa açık kalıyor, kayıttan sonra kapanıyor.
- **Sozlukler.SonKalanlar.cs (TR+EN, konumsal record):** FarkFazla, FarkEksik, GunKapandiRozet ve GunKapandiSebep metinleri bu dosyada. `Sozlukler.cs` HUB'ına dokunulmadı. "+ Not ekle" için mevcut `ParaDemo2Sozlugu.NotEkle` kullanıldı.

### Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
Bu dalgada açılan razor sayfaları:
- `Pages/Kasalar.razor`: PATCH-5 hakem bulguları gereği (hareket çekmecesi, kapalı gün, fark yönü).
- `Pages/Cariler.razor`: yalnız tahsilat çekmecesi.
- `Pages/Gider.razor`: istem bu dosyayı paketin dosya alanına aldı (TOPLU-6 devri).

**Gerekçe (ikinci dokunuş):** üç sayfa da önceki dalgalarda açılmıştı. Bu açılışlar istemin KAPSAM maddesine ("bulguların tamamı") ve DOSYA ALANI maddesine dayanıyor. Kapı kaydına bu gerekçe yazılmalı.

## 3. DoD sözleşmesi
Bu pakette yeni varlık ya da kayıt türü doğmadı.
- **Banka hesabı:** PATCH (para birimi yalnız geçmişsiz hesapta değişir; geçmişli hesapta 409 ve "yeni hesap açın" yolu yazılı) + DELETE/pasif + aktife al. Ekranda Düzenle var; değişmedi.
- **Kasa:** aynı kural mevcuttu; 409'a alan eklendi.
- **Gün kapanışı:** değişmez kayıttır. Yeniden açma yalnız işletme sahibi tarafından, gerekçeyle yapılır (`POST …/gun-kapanisi/{tarih}/ac`, SAHİP rolü — değişmedi). Kasa kartı kapalı günü söylüyor (UX-S30).
- **Kasa hareketi, tahsilat, gider, virman:** finansal kayıtlardır; hard DELETE yok, storno yolu değişmedi. Gider geçmişinde `DegismezIpucu` korunuyor (akordeon açılınca görünür).
- `DenetimServisi.YazmaKaydet` çağrıları ve `[RolGerekli]` değişmedi. Yeni red yolları hiçbir Add'den önce çalışıyor.

## 4. Migration
Gerekmedi; şema değişmedi. `BugunKapali` mevcut `gun_kapanislari` tablosundan türetiliyor.

## 5. Testler
Yeni dosya: `Kuyumcu.Tests/SonKalanlarTestleri.cs`. Toplam **27 metot / 37 vaka**:
- `SonKalanlarHttpTestleri` SK01–SK11 (HTTP + servis + SQL)
- `SonKalanlarSafTestleri` SK20–SK22
- `SonKalanlarKasaEkranTestleri` SKE01–SKE06 (bUnit)
- `SonKalanlarTahsilatEkranTestleri` SKC01–SKC04
- `SonKalanlarGiderEkranTestleri` SKG01–SKG03
- Ortak yardımcı `GiderAkordeon.Ac`.

`Dogrulayici/PATCH5SinirTestleri.D_P5_banka_bakiyeli_hesapta_para_birimi_DEGISMEZ_409` testinin **Skip'i kaldırıldı**; test yeşil.

Beklentisi değişen mevcut testler (davranış bilerek değişti; her birinde `SON-KALANLAR` notu var):
- `GunKapanisiTestleri` (2): ileti sade ve yönlü; "KAPATILMIŞTIR" yerine "günü kapandı".
- `StornoHttpTestleri` (1): kapanmış gün iletisi.
- `ParaDemoTestleri.PD1` ve `PARADEMOSinirTestleri.PDS_kurus`: "TL fark / ZORUNLUDUR" yerine "₺ fazla" ve alan:aciklama.
- `ParaDemo2EkranTestleri.PD2E_2`: fark yönlü.
- `PatchBesTestleri` (1 InlineData): altın yazanın reddi alan:hasGrami.
- `UX4bEkranTestleri` (1): kilit sebebi altlıkta.
- `Toplu6Testleri` (1 assert + yardımcı), `Toplu5Testleri`, `Q101GiderToplamEkranTestleri`, `QA10KritikTestleri`, `P19EkranTestleri`: geçmiş akordeonu kullanıcı gibi açılıyor (`GiderAkordeon.Ac`).

| Koşum | Sonuç |
|---|---|
| Filtreli (SonKalanlar, Gider, Toplu6Gider) | 314/314 |
| Filtreli ilgili kümeler (Kasa, Cari, Banka, ParaDemo, PatchBes, PatchIki, PatchUc, GunKapanis, StornoHttp, Tevsik, UX4b, ORTAK1, ErisimKapatma, Gider ailesi) — ara koşum | 1874 geçti, 11 kırmızı (yalnız yeni testlerin fikstür/beklenti hataları); düzeltildikten sonra son koşum 314/314 |
| **Tam süit (son kod, bir kez)** | **Toplam 10235 · Geçti 10233 · Başarısız 0 · Atlandı 2** |

Atlanan 2 test önceden Skip'li: `TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli` ve `SATIS3EkranTestleri.DOG_T24_…`.
**Bu paketin yeni kırmızısı: 0.** PATCH-5 devrindeki 9 başkasına ait kırmızı (ZOBELGE ×8, TOPLU-6 ×1) bu koşumda da yeşil.
Test sayısı: önce 10198 (10235 − 37), sonra **10235**.

## 6. Mutasyon tablosu
**Yöntem:** kaynak `/private/tmp/kuyumcu-serit/SON-KALANLAR/mut-yedek/` altına kopyalandı. Tam 1 eşleşmeli değişiklik yapıldı (eşleşme 1 değilse sürücü durur). Ardından `dotnet test --filter` koşuldu. Yedek geri kopyalandı ve **mtime güncellendi** (PATCH-5 notu: `os.utime`). Son olarak `cmp` (filecmp, shallow=False) çalıştırıldı.
Sonuç: **23 mutasyonun 23'ü en az bir testi kırmızıya çevirdi; hepsinde cmp AYNI.** Ham kayıt: `ham/SON-KALANLAR-olcum/mut-tablo.txt` ve `sonuc-hepsi.json`.

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M1-1 | Banka bakiye kilidi `if (false)` | SK01 (ileti "Bakiyesi sıfır olmayan…" değil "Hareket görmüş…" oldu) |
| M1-2 | Banka hareket kilidi `&& false` | SK02 |
| M1-3 | `ParaBirimiKilidi` çağrısı silindi | SK01, SK02, D_P5_banka_bakiyeli_hesapta_para_birimi_DEGISMEZ_409 |
| M2-1 | Kasa 409'unda `.AlanIle(alan)` yok | SK04 |
| M3-1 | Gün Kapat gerekçe `.AlanIle("aciklama")` yok | SK05, PDS_kurus_0_005_fark_bir_kurusa_yuvarlanir_gerekce_ister, Fark_varsa_GEREKCE_ZORUNLUDUR |
| M4-1 | Ham eksi kontrolü `< -0.005` | SK06(−0,004), SK06(−0,0049) |
| M5-1 | Kapanmış gün iletisi eski "SAHİP yetkisiyle" | SK09, Kapanmis_gune_SATIS_yazilamaz |
| M6-1 | Strict iletisi eski teknik metin | SK08, SK21 |
| M7-1 | Sözlükte `SayilanNakit` yok (ASCII yazım) | SK08, SK21 |
| M8-1 | DTO `EnAzTutar` → "sıfırdan büyük" | SK10 |
| M8-2 | `EnAzTutarMesaji` → eski ileti | SK20 |
| M9-1 | Tahsilat "kullanıcı TL yazdı" dalı `if (false)` | SK11 |
| M10-1 | `GunKapanisiDto` `[Range]` geri kondu | SK07 |
| M11-1 | `BugunKapali: false` | SK09 |
| M12-1 | Ekranda `BugunKapali(k) => false` | SKE01 (BASİT, PRO) |
| M13-1 | Hareket bandı yine `SunucuMesaji` | SKE03 |
| M14-1 | `FormDurumu.SunucuHatasi` bant yerine `SunucuMesaji` | SKE03, SKE06 |
| M15-1 | Fark yönü ters | SKE05(1200) |
| M16-1 | Tahsilat bandı yine `ex.Message` | SKC03 |
| M16-2 | `tlTutari` alan eşlemesi yok | SKC03 |
| M17-1 | Gider akordeonu `BaslangictaAcik="true"` | SKG01 (BASİT, PRO) |
| M18-1 | Açıklama hep açık | SKG02 (BASİT, PRO) |
| M19-1 | Tahsilat çekmecesinde altlık Kaydet yok | SKC01, SKC03, SKC04, UX4b (2), … (8 test) |

Son mutasyondan sonra `dotnet build Kuyumcu.sln --no-incremental` çalıştırıldı: **0 Hata**. 2 uyarı başka şeridin dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601 ve `KesifP2Testleri.cs:455` xUnit2029. Son artımlı derleme 0/0.

## 7. Denetim
`bash dotnet/denetim.sh` → **303 ✓**. Tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ). Ham çıktı: `ham/SON-KALANLAR-olcum/denetim.txt`.

Ara koşumda bu paketin iki ihlali görüldü ve düzeltildi:
- **B-8 sınırsız ToListAsync 146/145:** `NakitAyrimlari`'ndaki kapalı gün sorgusuna `.Take(idler.Count)` eklendi.
- **B-3 ESIK_ISTEMCIDE 5/4:** Kasalar.razor'daki bir yorumda örnek tutar olarak "185.000" geçiyordu; "X ₺" yapıldı.

Bu iki değişiklik canlı yayın anlık görüntüsünden sonra yapıldı. Davranışa etkisi yok: biri yorum, diğeri en çok kasa sayısı kadar satır dönen bir sorguya eşdeğer sınır. Tam süit son kodla koşuldu.

## 8. Canlı doğrulama (API 5162 · Release anlık görüntüsü · vekil 5163 · bağımsız SQL kuyumcu_net)
**Kurulum:** test kiracısı `PAKETSON-KALANLAR-TEST …` (01M3ZZFVJ5GJ0MJ6ETERCNHY6R) sistem daveti ile açıldı. Sırlar yalnız süreç ortamına verildi; sistem anahtarı rastgele üretildi ve chmod 600 ile korundu.
**5xx sayısı: 0.** Çıktı: `canli-sk-kosum.txt`.

| İstek | Beklenen | Gerçek | SQL önce → sonra |
|---|---|---|---|
| USD hesaba GELEN 100 → PATCH `{paraBirimi:"TRY"}` | 409 alan:paraBirimi | 409 [paraBirimi] "Bakiyesi sıfır olmayan banka hesabının para birimi değiştirilemez — yeni hesap açın" | `USD 100.00 1` → `USD 100.00 1` |
| +GIDEN 100 (bakiye 0, 2 hareket) → PATCH `{paraBirimi:"EUR"}` | 409 | 409 [paraBirimi] "Hareket görmüş …" | `USD 0.00 2` → aynı |
| OLUMLU: geçmişsiz hesap PATCH EUR | 200 | 200 | `EUR 0.00 0` |
| PATCH kasa (geçmişli) `{paraBirimi:"USD"}` | 409 alanlı | 409 [paraBirimi] | `TRY 1000.00 1 0` → aynı |
| Gün Kapat `{sayilanNakit:0}` (fark, gerekçe yok) | 400 alan:aciklama | 400 [aciklama] "Sayılan nakit beklenenden 1.000,00 ₺ eksik (beklenen nakit 1.000,00 ₺). Farkın nedenini yazın." | aynı |
| Gün Kapat `{sayilanNakit:1200}` | 400, yön "fazla" | 400 [aciklama] "… 200,00 ₺ fazla …" | aynı |
| Gün Kapat `{sayilanNakit:"1000"}` (Strict) | 400 Türkçe ileti | 400 [sayilanNakit] "Sayılan nakit geçerli bir sayı değil — sayıyı rakamla yazın, ör. 1500,50" | aynı |
| Gün Kapat −0,01 / −0,004 | 400 (sıfır sayılmaz) | 400 [sayilanNakit] "Sayılan nakit eksi olamaz." ×2 | aynı |
| Gün Kapat 1e14 | tek tavan cümlesi | 400 [sayilanNakit] "Sayılan nakit çok büyük — kaydedilebilecek en büyük değeri aşıyor" | aynı |
| Kasa hareketi 0,004 / 1e14 | çelişmeyen ileti / aynı tavan cümlesi | 400 [tutar] "Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez" / "Tutar çok büyük — kaydedilebilecek en büyük değeri aşıyor" | `TRY 1000.00 1 0` → aynı |
| Tahsilat 0,001 / altın 0,0000004 / boş | sade, "has gram" yok | [tlTutari] "Tutar en az 0,01…" / [hasGrami] "Altın miktarı en az 0,000001 gram…" / [tlTutari] "Tutar ya da altın girin." | `1500.00 1` → aynı |
| Gün kapatıldı (sayılan 1300 = defter) → kasa hareketi / tahsilat | 400 sade ileti | 400 "Bu kasanın 03.10.2026 günü kapandı (sayılan 1.300,00 ₺). Kapanmış güne kayıt yazılamaz; yeniden açmak için işletme sahibine başvurun." (×2) | `TRY 1300.00 2 1` → aynı |
| GET /kasalar/nakit-ayrimi | bugunKapali | önce `[kasa1 false, kasa2 false]` → sonra `[kasa1 true, kasa2 false]` | gun_kapanislari `1300.00 0.00 açık=null` |

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9911, vekil 5163 → API 5162)
**Matris:** 1440 / 1280 / 375 × BASİT / PRO × açık / koyu = 12 kombinasyon. Her kombinasyonda 7 ekran ölçüldü, toplam **84 ölçüm + 84 ekran görüntüsü**. Tablo `ham/SON-KALANLAR-olcum/olcum-tablo.md`, ham veri `olcum.jsonl`. Ölçer: `betikler/sikisma-olc.js`.

Ölçülen ekranlar: `kasalar`, `kasa-hareket-hata` (1e14 gönderildi), `gun-kapat-fark` (sayılan 200000), `tahsilat` (tutar 123456789012,50), `gider-kapali`, `gider-acik`, `gider-virman`.

**84/84 ölçümde sonuç:**
- K1 gizli görünen 0
- K3 taşan/kırık 0/0
- K5 TRY 0
- K7 hizasız 0
- K8 jargon 0
- K10 elips 0
- K11 hata bandı 0
- K12 dikey hizasız düğme 0
- **yatay kaydırma 0**
- K2 kart sağ yüzdesi 98 (dar sütun yok)

**Özel kontrollerin hepsi geçti (84/84):**
- Hareket hatası çekmecede **1 kez** ve alanın altında; Kaydet altlıkta.
- Fark "… ₺ fazla".
- Tahsilat: çekmece, altlıkta Kaydet, `#th-kasa-gerekli` yok, kilit tek satır ("Kasa seçin..."), dev tutar kutuya sığıyor (430 px @1440).
- Gider: geçmiş `aria-expanded=false`, rozet "1", tablo çizili değil, açıklama kutusu yok, "+ Not ekle" var. Açılınca 1 satır ve "Toplam gider: 750 ₺" görünüyor.
- Virman geçmişi kapalı, rozet "1".
- Kapalı kasa kartı: rozet + sebep; düğmeler kilitli; açık kasa düğmeleri açık.

**Gerekçeli tek sayaç — K9 "binen" (yalnız çekmece açıkken, 36 ölçümde).** Örnek çiftler:
- `#kasa-yeni "+ Yeni Kasa" ⟂ label "Hareket Türü"`
- `#cari-segment-toptanci ⟂ #th-guncel`

Bunlar arka plandaki sayfa öğesi ile çekmece öğesi çiftleridir. Çekmece perdeli bir üst katmandır; ekran görüntülerinde arka plan perdenin altında kalıyor (ör. `foto/tahsilat__Basit_aydinlik_1440.png`). Aynı sayaç değişmeyen Gün Kapat çekmecesinde de çıkıyor (`gun-kapat-fark__*`). Bu bir ölçüm aracı yapıntısıdır; yazı yazıyla görünür biçimde üst üste binmiyor. Sayfa ölçümlerinde (kasalar, gider-*) K9 = 0.

**Görülen (Read) ekran görüntüleri:**
- `kasalar__Basit_aydinlik_1440`
- `tahsilat__Basit_aydinlik_1440`
- `kasa-hareket-hata__Basit_karanlik_375`
- `gun-kapat-fark__Basit_karanlik_375`
- `kasalar__Basit_karanlik_375`
- `gider-kapali__Profesyonel_karanlik_1280`
- `tahsilat__Profesyonel_aydinlik_375`
- `gider-acik__Basit_aydinlik_375`

## 10. Temizlik
- API (PID dosyasından) ve vekil durduruldu. Chrome sürücü sonunda kapandı. `lsof` 5162/5163/9911 LISTEN → **boş**. 5199/5200'e dokunulmadı.
- Test kiracısı `zsh kiraci-temizle.sh 01M3ZZFVJ5GJ0MJ6ETERCNHY6R --uygula` ile silindi (rc 0). tenant_id kolonu olan **75 tabloda satır toplamı 0**; tenants'ta bu kiracı 0.
- **tenants sayısı:** başlangıçta 15, sonda 16. Fark **bu paketin değil**: `PAKET-RAPORGORUNUM-HAKEM-TEST` kiracısı 04:16 UTC'de RAPOR-GORUNUM hakem şeridi tarafından, koşum sırasında açıldı. QA-TEST- kiracılarına dokunulmadı.
- `fikstur.json` ve `sistem.anahtar` `printf ''` ile boşaltıldı.

## 11. Değişen dosyalar (bu paket)
Ağaçtaki diğer değişiklikler başka şeritlerin.
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/GunKapanisiKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/KolonTavani.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Gider/GiderModulu.cs`, `VirmanModulu.cs` (yalnız Range iletisi)
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Core/I18n/DogrulamaSozlugu.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SonKalanlar.cs` (YENİ)
- `dotnet/Kuyumcu.Contracts/Sayfalar/KasalarSozlesmeleri.cs`
- `dotnet/Kuyumcu.Web/Servisler/FormDurumu.cs`
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Gider.razor`
- Testler:
  - `dotnet/Kuyumcu.Tests/SonKalanlarTestleri.cs` (YENİ)
  - `Dogrulayici/PATCH5SinirTestleri.cs`, `Dogrulayici/PARADEMOSinirTestleri.cs`
  - `GunKapanisiTestleri.cs`, `StornoHttpTestleri.cs`, `ParaDemoTestleri.cs`, `ParaDemo2EkranTestleri.cs`
  - `PatchBesTestleri.cs`, `UX4bEkranTestleri.cs`
  - `Toplu5Testleri.cs`, `Toplu6Testleri.cs`, `Q101GiderToplamEkranTestleri.cs`, `QA10KritikTestleri.cs`, `P19EkranTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-SON-KALANLAR-uygulama-raporu.md` ve `ham/SON-KALANLAR-olcum/` (YENİ)

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| D-P5-1 banka para birimi (bakiye/hareket varken değişmez, SQL değişmez) + Skip kaldırıldı | KARŞILANDI (409, sapma 1) | SK01–03, D_P5 yeşil, M1-1..3, canlı |
| Kasa para birimi kilidi alanlı | KARŞILANDI | SK04, M2-1, canlı |
| Gün Kapat gerekçe → 400 alan:aciklama | KARŞILANDI | SK05, M3-1, canlı, SKE06 |
| Yarım kuruş eksi sayım sıfır sayılmaz | KARŞILANDI | SK06 (3 vaka), M4-1, canlı −0,004 → 400 |
| Kapanmış gün iletisi sade, ₺, rol kodu yok; kart rozeti + kilitli düğmeler | KARŞILANDI | SK09, SKE01, M5-1/M11-1/M12-1, canlı, ekran 84/84 |
| Strict iletisi Türkçe | KARŞILANDI | SK08, SK21, M6-1/M7-1, canlı |
| Tek tavan cümlesi | KARŞILANDI | SK07, M10-1, canlı |
| "≤0" iletisi çelişmez, "has gram" yok | KARŞILANDI | SK10, SK11, SK20, M8-1/M8-2/M9-1 |
| Çift hata gösterimi yok (ortak kalıp) | KARŞILANDI | SKE03/SKE04/SKE06, SKC03/SKC04, M13/M14/M16, tarayıcı "kez:1" |
| Tek çekmece düzeni (hareket + tahsilat altlıkta Kaydet; tahsilat Cekmece) | KARŞILANDI | SKE03, SKC01, M19-1, tarayıcı |
| Gider geçmişi kapalı akordeon + "+ Not ekle" | KARŞILANDI | SKG01–03, M17/M18, tarayıcı |
| Tahsilat tutar tam genişlik, "Kasa seçin" tek kilit, `tlTutari` → `th-tutar` | KARŞILANDI | SKC01–03, tarayıcı (sığıyor, `#th-kasa-gerekli` yok) |
| Fark yönü | KARŞILANDI | SKE05, M15-1, PD2E_2, tarayıcı |
| build 0/0 | KARŞILANDI | son artımlı 0/0; `--no-incremental`'de 2 uyarı başka şeritte |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | 303 ✓, tek ✗ P-30 |
| Tam süit yeni kırmızı 0 (bir kez) | KARŞILANDI | 10233/10235, 0 başarısız |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | 23/23 |
| Gerçek tarayıcı 1440/1280/375 · BASİT/PRO · açık/koyu, Read | KARŞILANDI | § 9 |
| Bellek: 1 API + 1 Chrome, tam süit bir kez | KARŞILANDI | aynı anda tek API ve tek Chrome; tam süit 1 kez |
| Test kiracısı silindi | KARŞILANDI | 75 tablo 0 satır (tenants farkı başka şeridin, § 10) |
| COMMIT yok, dosya silme yok, sır basılmadı | KARŞILANDI | — |
| Mevzuat | Uygulanmadı | yasal eşik/oran yazılmadı |

## 13. Açık kalanlar
- **Tahsilat altlığındaki kilit satırı** "Kaydet kilitli:" ön ekini taşımıyor (Cekmece `KaydetMetni` verildiği için, Gün Kapat ile aynı). Eski modalde "Kilit sebebi: Kasa seçin" yazıyordu. Hakem isterse tek satırlık sözlük değişikliğidir.
- **`ModalPencere` ile açılan diğer çok alanlı formlar** (Cariler veresiye, düzeltme fişi) bu paketin bulgusu değildi ve dokunulmadı. DoD EK md.2 için ayrı bir pakete önerilir.
- **Ölçer K9:** çekmece açıkken arka plan öğelerini sayıyor (§ 9). `sikisma-olc.js`'in perde altındaki öğeleri dışlaması orkestratöre önerilir.

## 14. Kurucu / mali müşavir
Bu pakette mevzuat hükmü yazılmadı ve değiştirilmedi.

---

# TUR 2 (03.10 · 08:10–08:40) — önceki doğrulamada kalan bulguların kapatılması

Kanıtlar `ham/SON-KALANLAR-olcum/tur2/` altında: canlı betik ve çıktısı, mutasyon sürücüsü ve sonucu, 48 ölçüm, 48 ekran görüntüsü, denetim çıktısı, eşleme denetim betiği ve tam süit özeti. COMMIT yapılmadı.

## T2.1 Bulgu → yapılan

| Bulgu (önceki doğrulama) | Yapılan | Kanıt |
|---|---|---|
| **[KRİTİK] Tasarım hakemi:** banka hesabında para birimi değişimi ekranda sessiz başarısız oluyordu. 409 `paraBirimi` d-doviz'e eşleniyordu ama Secim alan hatası çizmiyordu, bant da boş kalıyordu. | `Banka.razor` Hesabı Düzenle: kilit kaydetmeden ÖNCE okunuyor. Bakiye ≠ 0 ise sebep BAKIYE. Bakiye 0 ise mevcut okuma ucu `GET /bankalar/{id}/hareketler?limit=1` soruluyor; hareket varsa sebep HAREKET. Okunamazsa sebep BILINMIYOR ve kilit fail-closed. Kilitliyken Döviz radyoları disabled ve altında `DegismezIpucu` (`#degismez-ipucu-banka-hesap-doviz`) yazıyor: "Hareketi olan hesabın para birimi değişmez — yeni hesap açın." (bakiye için de eşi var). Ek olarak `d-doviz` artık `Durum="duzenleForm" HataAlani="d-doviz"` taşıyor; sunucu yine reddederse mesaj alanın ALTINDA görünüyor. Aynı açık `d-sahibi`, `hesap-doviz` ve `hesap-sahibi` alanlarında da vardı (eşleniyor ama çizilmiyordu); hepsi kapatıldı. Yeni uç açılmadı. Sunucu kuralı (`ParaBirimiKilidi`) değişmedi. | SKB01–SKB05, M20-1..5, canlı (§ T2.5), tarayıcıda 36/36 banka ölçümü |
| BantMesaji kullanan diğer 6 sayfada "eşlenen ama çizilmeyen alan" denetimi | Statik denetim (`tur2/esleme-denetim.py` ve elle okuma) yapıldı. **Kasalar:** gün kapat `gun-sayilan`/`gun-aciklama` ve hareket `h-tutar`/`h-aciklama` çizili. Düzenle eşlemesi `SunucuMesaji` kullanıyor, bant hatayı yutmuyor. **Cariler:** tahsilat `th-tutar` her zaman çizili; `th-has` yalnız çiziliyse eşleniyor. Yeni/düzenle formları `SunucuMesaji` kullanıyor. **SifreSifirla:** 3/3 çizili. **Faturalar:** `BantMesaji` kullanmıyor; 4 eşlemenin 4'ü de `AlanHatasi` ile çizili. **Islemler / Islemler.Odeme:** `FormDurumu.BantMesaji` kullanmıyor (`BasitBantMesaji` farklı bir özellik, grep'te yanlış pozitif çıktı). Sonuç: açık yalnız Banka'daydı ve kapatıldı. Banka için SKB05 kaynak denetim testi bağlandı. | SKB05, M20-4 |
| **[DÜŞÜK]** 0,005 HTTP'de reddediliyor, servis yolu ve tahsilat 0,01 kabul ediyordu (uçlar arası tutarsızlık) | **Tek kural seçildi:** alt sınır yalnız `KolonTavani.PozitifTutar`'da (önce 2 haneye AwayFromZero yuvarla, sonra en az 0,01). Kasa, Banka, Gider ve Virman DTO'larındaki `[Range(0.01, …, EnAzTutar)]` kaldırıldı; Kasa ve Banka'da `[Required]` kaldı. **Gerekçe:** PATCH-5'in "önce yuvarla" sözleşmesi zaten servis yolunda, tahsilatta ve saf testlerde sabitti (`P5_PozitifTutar_once_yuvarla…` 0,005 → 0,01, CARIBAKIYE, DEGISIMGUNSONU). Ters yön (servis 0,005'i reddetsin) bu sözleşmeyi bozardı. Para etkisi: 0,005 artık 0,01 olarak yazılıyor. 0,0049 ve altı ile eksi tutarlar aynı literal iletiyle 400 alıyor. | SK12, DSK10 (yeniden yazıldı), M22-1/M22-2, canlı: kasa, banka, gider, virman ve tahsilat 0,005 → 200 (SQL 0,01); 0,0049 → 400 |
| **[DÜŞÜK]** Tahsilatta "Kasa seçin" iki kez görünüyordu (yer tutucu + altlık) | Altlıktaki kilit sebebi `SonKalanlarSozlugu.TahsilatKasaKilit` oldu: "Kaydetmek için yukarıdan kasayı seçin" (EN: "Choose the cash box above to save"). Bu metin "kasa seçin" dizisini, büyük/küçük harf ayrımı olmadan da İÇERMİYOR. | SKC02 (çekmece metninde "kasa seçin" harf duyarsız tam 1 kez), UX4b, M21-1, tarayıcı 12/12 `kasaSecinGorunurKez=1` |
| **[DÜŞÜK]** SK10/SK11 beklenen iletiyi üretim sabitinden okuyordu | SK10, SK11 ve SK20 artık literal metinle karşılaştırıyor. | M23-1: ileti değişince SK10, SK11 ve SK12 kırmızı |
| **[DÜŞÜK]** 409 ↔ 400 sapması | Kod değişikliği yok (bulgunun önerisi: kurul kayda geçirsin). Sapma 1 geçerli. | — |

## T2.2 Değişen dosyalar (tur 2)
- `Kuyumcu.Web/Pages/Banka.razor`: kilit, DegismezIpucu ve 4 alanın alan-hatası bağı. Bu sayfa bu pakette ilk kez açıldı. Gerekçe: hakemin KRİTİK bulgusu. Dosya alanına "ortak HataBandi/alan hatası kalıbı" kapsamında girdi.
- `Kuyumcu.Web/Pages/Cariler.razor`: yalnız `TahsilatKilidi`.
- `Kuyumcu.Core/I18n/Sozlukler.SonKalanlar.cs`: BankaDovizKilit{Bakiye,Hareket,Bilinmiyor} ve TahsilatKasaKilit anahtarları, TR+EN.
- `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`, `Banka/BankaModulu.cs`, `Gider/GiderModulu.cs`, `Gider/VirmanModulu.cs`: DTO `Range(0.01)` kaldırıldı.
- Testler:
  - `SonKalanlarTestleri.cs`: SK12 ve SKB01–05 yeni; SK10, SK11, SK20 ve SKC02 literal oldu.
  - `UX4bEkranTestleri.cs`: kilit metni.
  - `Dogrulayici/SONKALANLARSinirTestleri.cs`: DSK10 tek kurala göre yeniden yazıldı.
  - `Dogrulayici/PATCH5SinirTestleri.cs`: red listesinde 0,005 yerine 0,0049.
  - `Dogrulayici/Q101SinirTestleri.cs`: DQ101 red vakasında 0,005 yerine 0,0049.
  - Beklentisi bilerek değişen bu üç doğrulayıcı testinin her birinde gerekçe yorumu var.

## T2.3 Testler
- Filtreli koşumlar:
  - SonKalanlar + SONKALANLAR + UX4b: 85/85.
  - İlgili aileler (Banka, Kasa, Gider, Virman, Cari, PatchBes, PATCH5, UX1C2, ZOISLEMA, Q91, P12, Ekstre, UX4*, P19, DegisimGunSonu, Tahsilat, ParaDemo, Toplu, MASAK, Pos): 2703/2705, 1 kırmızı (PATCH5 0,005 InlineData, düzeltildi), 1 atlandı (önceden Skip'li).
  - Düzeltme sonrası PATCH5Sinir + SonKalanlar: 101/101.
- **Tam süit (tur 2, BİR kez, son mutasyondan sonraki temiz derlemeyle):** Toplam 10280 · Geçti 10277 · **Başarısız 1** · Atlandı 2.
  - Kırmızı test: `Dogrulayici.Q101SinirTestleri.DQ101_kurus_yuvarlamasi_0_005_yukari_ve_storno_net_sifir` ("Expected BadRequest, Actual OK"). **Bu paketin ürünüdür**: Gider DTO Range kaldırılınca 0,005 artık kabul ediliyor.
  - Testin red vakası tek kurala göre 0,0049 yapıldı. Testin asıl amacı (sıfır tutarlı gider doğmaz, AwayFromZero, storno net sıfır) korundu.
  - Filtreli yeniden koşum: Q101 ailesi **41/41 yeşil**. Tam süit tur kuralı gereği ikinci kez koşulmadı.
  - Başka kırmızı yok. Önceki turun başkasına ait kırmızıları da yeşil.
- Test sayısı: tur 1 sonu 10235 → tur 2: 10280 (fark: bu paketin 7 yeni vakası + başka şeritlerin eklemeleri).
- Derleme: `dotnet build Kuyumcu.sln` 0 uyarı 0 hata. Test projesinin artımlı derlemesindeki tek uyarı CS8601 başka şeridin dosyasında (`SUBEKAPSAMASinirTestleri.cs:49`).

## T2.4 Mutasyon (tur 2) — 9/9 kırmızı, hepsinde cmp AYNI
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M20-1 | Bakiye kilidi `if (false)` | SKB01 (BASİT, PRO) |
| M20-2 | Hareket kilidi `? null : null` | SKB02 |
| M20-3 | Okunamayınca serbest (`null`) | SKB04 |
| M20-4 | d-doviz alan-hatası bağı silindi | SKB03, SKB05 |
| M20-5 | `Devredisi` silindi | SKB01 ×2, SKB02, SKB04 |
| M21-1 | Altlık sebebi yine `KasaSecin` | SKC02, UX4b `Kasasiz_tahsilat_KAYDEDILEMEZ_ve_sebebi_yazar` |
| M22-1 | Kasa DTO `Range(0.01)` geri kondu | DSK10, SK12 |
| M22-2 | Banka DTO `Range(0.01)` geri kondu | SK12 |
| M23-1 | `EnAzTutarMesaji` "sıfırdan büyük" | SK10, SK11, SK12 |

Son mutasyondan sonra `dotnet build Kuyumcu.sln`: 0/0. Kaynakta mutasyon artığı yok (grep 0).

## T2.5 Canlı (API 5162 · Release anlık görüntüsü · bağımsız SQL kuyumcu_net · 5xx = 0)
| İstek | Sonuç | SQL |
|---|---|---|
| PATCH SK Dolar (USD, bakiye 100) `{paraBirimi:"TRY"}` | 409 [paraBirimi] "Bakiyesi sıfır olmayan…" | `USD 100.00 1` → aynı |
| PATCH SK Hareketli (bakiye 0, 2 hareket) | 409 [paraBirimi] "Hareket görmüş…" | `USD 0.00 2` → aynı |
| GET `hareketler?limit=1` (ekranın kilit sorusu) | Dolar 1 · Hareketli 1 · Bos 0 | Bos `USD 0.00 0` |
| Kasa hareketi 0,005 / 0,0049 | 200 / 400 [tutar] literal | `1000.00 1` → `1000.01 2` (satır 0.01) → 0,0049'da aynı |
| Banka hareketi 0,005 / 0,0049 | 200 / 400 [tutar] | `100.00 1` → `100.01 2` → aynı |
| Gider 0,005 / 0,0049 | 200 (satır −0,01) / 400 [tutar] "Gider tutarı en az 0,01…" | — |
| Virman 0,005 / 0,0049 | 200 / 400 [tutar] "Virman tutarı en az 0,01…" | — |
| Tahsilat 0,005 / 0,0049 | 200 / 400 [tlTutari] | `1500.00 1` → `1499.99 2` → aynı |

## T2.6 Gerçek tarayıcı (başsız Chrome CDP 9912 → vekil 5163 → API 5162)
- **Matris:** 1440 / 1280 / 375 × BASİT / PRO × açık / koyu × 4 ekran: banka-doviz-bakiye, banka-doviz-hareket, banka-doviz-serbest, tahsilat-kasa. Toplam **48 ölçüm + 48 görüntü**. Tablo: `tur2/olcum-tablo-t2.md`.
- **48/48 ölçümde sıfır olan sayaçlar:** K1, K3 (taşan ve kırık), K5 TRY, K7, K8, K10, K11, K12 ve yatay kaydırma.
- **Ekrana özel kontroller:**
  - Bakiyeli ve hareketli hesap: 4 radyonun 4'ü disabled, DegismezIpucu görünür ve metni doğru.
  - Geçmişsiz hesap: radyolar açık, ipucu yok.
  - Tahsilat: "kasa seçin" (harf duyarsız) 12/12'de 1 kez görünüyor. Kilit metni "Kaydetmek için yukarıdan kasayı seçin", Kaydet altlıkta ve kilitli.
- **K9 (2–6):** yalnız çekmece açıkken çıkıyor. Arka plandaki sayfa öğesi çekmece öğesiyle çift olarak sayılıyor (ör. `p.stat-etiket "HESAP SAYISI" ⟂ "Hesap Adı"`). Bu, tur 1 § 9'daki ölçer yapıntısıyla aynı; görüntülerde perdenin altında kalıyor.
- **Read ile görülen görüntüler:**
  - `banka-doviz-bakiye__Basit_aydinlik_1440`
  - `banka-doviz-hareket__Profesyonel_karanlik_375`
  - `banka-doviz-serbest__Basit_aydinlik_375`
  - `tahsilat-kasa__Profesyonel_karanlik_1280`

## T2.7 Denetim ve temizlik
- **Denetim:** `denetim.sh` 303 ✓; tek ✗ P-30 DEMO_INSAN_SURESI (istemde hariç).
- **Durdurulan süreçler:** API (PID 66622) ve vekil `kill` ile durduruldu; Chrome sürücünün sonunda kapandı. `lsof` 5162/5163/9912 boş. 5199/5200'e dokunulmadı.
- **Test kiracısı:** `PAKETSON-KALANLAR-TEST t2 …` (01M403N6CJP37FV948CK3N61JE) `zsh kiraci-temizle.sh … --uygula` ile silindi (rc 0). tenants başta **15**, sonda **15**. QA-TEST- kiracılarına dokunulmadı.
- **Sır dosyaları:** `fikstur.json` ve `sistem.anahtar` `printf ''` ile boşaltıldı.

## T2.8 Açık kalanlar
- **Kurul kaydı:** 409/400 sapması (Sapma 1).
- **Kurul kaydı:** 0,005 için seçilen tek kural "önce yuvarla". Böylece 0,005 → 0,01 artık HTTP'de de kabul ediliyor.
- **Ölçer K9 yapıntısı:** `sikisma-olc.js` perde altını dışlamalı (orkestratöre).
- **DTO alt sınırları:** Diğer 16 DTO'daki `Range(0.01…)` bu bulgunun kapsamında değildi ve dokunulmadı (Satış, POS vb.). Aynı tek kural için ayrı bir pakete önerilir.

## T2.9 Kurucu / mali müşavir
Mevzuat hükmü yazılmadı ve değiştirilmedi.
