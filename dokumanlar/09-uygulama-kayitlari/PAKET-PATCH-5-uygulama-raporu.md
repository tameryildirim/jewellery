# PAKET PATCH-5 — uygulama raporu (03.10, dalga PATCH-5 · t2 · 5162/5163)

Rol: Taşkın (backend). Ertuğ (frontend) işi bu pakette doğmadı: hiçbir razor, css, sözlük ya da bileşen dosyasına dokunulmadı (§ 9).
Kaynak: `PAKET-PATCH-SOZLESME-uygulama-raporu.md` (T3.2 "SONRAKİ PAKET" satırı, § 12, T3.11), PATCH-2/3/4 kalıpları (KolonTavani, genel JSON Strict, "önce yuvarla sonra ≤ 0", Verildi/null = koru, MySQL 1264 → 400 ağı) ve PARA-DEMO-2 hasımının DÜŞÜK bulgusu (Gün Kapat sayılan nakit).
Kanıtlar: `ham/PATCH-5-olcum/` (canlı betik + 3 koşum çıktısı, mutasyon sürücüsü `mut5.py`, 4 mutasyon koşumunun sonuçları, her mutasyonun özet günlüğü `mut-log/`, denetim çıktısı, tam süit özeti). Mutasyon yedekleri `/private/tmp/kuyumcu-serit/PATCH-5/mut-yedek/`; TRX `/private/tmp/kuyumcu-serit/trx-pPATCH-5-son2/`. Proje ağacına geçici dosya bırakılmadı.

## 1. Spesifikasyon ile kodun karşılaştırılması

| İddia / madde | Kodda bulunan | Sonuç |
|---|---|---|
| Gün Kapat'ta "sayılan nakit" sunucuda zorunlu değil | Doğru. `GunKapanisiDto.SayilanNakit` `decimal` (null olamaz) idi. Gövdede alan hiç yoksa 0 okunuyor, sayılmamış kasa "0 TL sayıldı" diye kapanıyordu. Ekran boş gönderimi engelliyor, API'yi doğrudan çağıranı engellemiyordu | Düzeltildi: `decimal?` + servis içinde 400 alan:sayilanNakit |
| Kasa/Cari PATCH taranmadı (PATCH-SOZLESME T3.2) | `PATCH /kasalar/{id}`: `""` para birimi **yazılıyordu** (RegularExpression boş dizeyi geçerli sayar). `PATCH /cari/{id}`: `{"ad":""}` ve `{"tur":""}` **yazılıyordu**, iskonto 3 ondalık sessizce yuvarlanıyordu | Düzeltildi (§ 2) |
| Virman ve Çıktı modüllerinde PATCH ucu yok | Doğru. `CiktiModulu`'nde **hiç yazma ucu yok** (tek uç `GET {tur}/{id}`). Virman'da POST + storno var | Çıktı: değişiklik yok (tarama tablosunda gerekçesi var) |
| Genel Strict (PATCH-2) bu modüllerin DTO'larını kapsıyor | Doğru: `Program.cs` `AddJsonOptions(Strict)` tüm `[FromBody]` DTO'larında tırnaklı sayıyı reddediyor. Bu modüllerde JsonElement gövdeli uç yok | Değişiklik gerekmedi |
| MySQL 1264 ağı (PATCH-3) son savunma hattı | Var. Ama alanı **kolondan** türetiyor: kasa hareketi `tutar:9999999999999` → 400 alan:**bakiye** (M1-5 günlüğünde ölçüldü). 1406 (metin taşması) ağda **yok** → 500 | Uç kapıları eklendi (birincil savunma); 1406 için ağ değil uç kapısı (§ 2) |

Sapmalar ve gerekçeleri:
- **Çıktı modülü:** yazma ucu olmadığı için değişmedi. Spesifikasyonun hedefi (yazma uçlarında girdi doğrulaması) burada boş küme.
- **Açılış (T-2) istisnası:** Cari düzeltme ile açılış aynı gövdeyi (`DuzeltmeYaz`) kullanıyor. "Önce yuvarla, sonra sıfır" kuralı açılışa da uygulanınca başka paketin sınır testi kırmızı oldu: `RAPORYASLANDIRMASinirTestleri.Kurus_yuvarlamasi_0_015_iki_kurus_0_004_listede_yok`. Bu test, eski sistemden gelen 0,004 TL'lik artığın açılışta kabul edilip 0,00'a yuvarlandığını ve listede görünmediğini sabitliyor. Açılış toplu ve tek transaction'da çalıştığı için, yuvarlanınca sıfırlanan tek satır yüzünden bütün açılış geri alınırdı. **Karar:** açılışta sıfır kontrolü ham değere yapılır (eski davranış). Yuvarlama ve kolon tavanı açılışta da uygulanır. Elle düzeltme fişinde (`CARI_DUZELTME`) sıfır kontrolü yuvarlanmış değere yapılır. Mutasyon M4-8 bu istisnayı kanıtlıyor.
- **Banka karşı taraf adı:** HTTP yolunda `StringLength(150)` zaten vardı. Bu yüzden canlıdaki 400 iletisi DTO'dan geliyor ve "eskiden 500" iddiası HTTP için **yanlış** olurdu (M5-3 mutasyonu hayatta kaldı, teşhis koşumunda gövde okundu). Servis kapısı ekstre onayı yolu için eklendi: o yol DataAnnotations'a uğramadan `HareketEkleIc`'e girer. 1406 hatası `ApiException` olmadığı için satır bazlı yakalanamaz. Bunu ayrı bir servis testi kanıtlıyor (M5-5).

## 2. Tarama tablosu — yazma uçları (uç · dosya:satır · PATCH null · kolon tavanı · Strict · yuvarlama · karar)

Satır numaraları son kod içindir. "Strict" sütununda **G**, genel `AddJsonOptions(Strict)` ayarı demektir (PATCH-2). Bu tabloda JsonElement gövdeli uç yok.

| Uç | dosya:satır (servis) | PATCH null davranışı | Kolon tavanı | Strict | Yuvarlama / ≤0 | Karar |
|---|---|---|---|---|---|---|
| POST /kasalar | Kasa/KasaModulu.cs:153 | — | ad varchar(100): **101 → 500 (1406)** idi | G | — | **DÜZELTİLDİ**: ad kırpılır, boş → 400, >100 → 400 alan:ad; para birimi listesi (`""` → TRY, `XYZ` → 400 alan:paraBirimi; eskiden doğrudan yazılıyordu) |
| PATCH /kasalar/{id} | Kasa/KasaModulu.cs:206 | zorunlu alanlar (ad, paraBirimi, subeId, aktif): null = KORU ✓ | ad 100 | G | — | **DÜZELTİLDİ**: `paraBirimi:""` / liste dışı → 400 (geçmişi boş kasada `""` YAZILIYORDU); ad hatalarına alan:ad |
| POST /kasalar/{id}/hareket | Kasa/KasaModulu.cs:326 | — | tutar + bakiye (15,2): sonuç sığmazsa ağ **alan:bakiye** veriyordu; açıklama varchar(500): **501 → 500** idi | G | net = tutar×(1−komisyon) 0,00 kalabiliyordu (komisyon 1 ya da 0,01×0,4) → **sıfır tutarlı satır**; komisyon 0,12345 → kolon (5,4) sessizce yuvarlıyor ama net ham oranla hesaplanıyordu | **DÜZELTİLDİ**: `PozitifTutar` (önce yuvarla → ≤0 → tavan), komisyon 0..1 + en çok 4 ondalık, net 0,00 → 400 alan:komisyonOran, `BakiyeSigar` alan:tutar, açıklama `[MaxLength(500)]` + servis |
| PATCH /kasalar/{id}/hareket/{h} | Kasa/KasaModulu.cs:621 | Verildi deseni ✓; tarih null → 400 (P-12 kararı korundu) | açıklama 500 | G | — | **İYİLEŞTİRİLDİ**: tarih null 400'üne alan:tarih; açıklama servis içinde de 500 |
| POST /kasalar/{id}/gun-kapanisi | Kasa/KasaModulu.cs:405 | — | sayılan nakit (15,2) | G | sayılan ham değerle fark hesaplanıyordu | **DÜZELTİLDİ (ek madde)**: sayılan nakit **zorunlu** (null/yok → 400 alan:sayilanNakit), önce 2 haneye yuvarlanır, tavan, eksi → 400 alanlı; Range iletisi Türkçe ve sınır yazılı |
| POST /kasalar/{id}/gun-kapanisi/{tarih}/ac | Kasa/KasaModulu.cs:535 | — | — | G | — | **DÜZELTİLDİ**: gerekçe ≥20 kuralı **kırpılmış** metne uygulanıyor (`"a"+19 boşluk` geçiyor, kayda "a" yazılıyordu) → 400 alan:gerekce |
| POST /kasalar/{id}/hareket/{h}/storno · DELETE /kasalar/{id} | Satis/StornoServisi.cs:371 · KasaModulu.cs:724 | — | — | G | sayı yok | uygun (dokunulmadı; storno motoru değişmedi) |
| KasaBakiyeKapisi (giriş) | Kasa/KasaBakiyeKapisi.cs | — | — | — | çağıranlar yuvarlanmış delta geçiriyor (kasa hareketi net, gider, virman, tahsilat) | **değişiklik yok** (girdi zaten yuvarlanmış; para kuralı değişmedi) |
| POST /virman | Gider/VirmanModulu.cs:110 | — | tutar (15,2) + iki bakiye; **kasa bacağı açıklaması varchar(500): etiket "VİRMAN: kaynak → hedef · açıklama" 500'ü aşınca 500 (1406)** | G | `≤0` yuvarlamadan ÖNCE idi: 0,004 → 0,00 bacak | **DÜZELTİLDİ**: `PozitifTutar`; etiket >500 → 400 alan:aciklama (kalan karakter iletide yazılı, metin kırpılmaz); iki taraf `BakiyeSigar` alan:tutar |
| POST /virman/{bacakId}/storno | Gider/VirmanModulu.cs | — | — | G | sayı yok | uygun |
| POST /giderler | Gider/GiderModulu.cs:314 | — | tutar (15,2); açıklama 500 | G | `≤0` yuvarlamadan önce: 0,004 → 0,00 gider satırı | **DÜZELTİLDİ**: `PozitifTutar`, açıklama servis içi 500 |
| PATCH /gider-kategorileri/{id} | Gider/GiderModulu.cs:187 | zorunlu ad/aktif **null → 400** idi (PATCH-3 sözleşmesine aykırı) | ad 100 | G | — | **DÜZELTİLDİ**: ad null = KORU, aktif null = KORU; `""`/boşluk ad → 400 alan:ad |
| POST/DELETE /gider-kategorileri | Gider/GiderModulu.cs:126/251 | — | ad 2..100 (DTO + servis) | G | — | uygun |
| POST /cari | Cari/CariModulu.cs:455 | — | kredi limiti (15,2); iskonto (5,2) | G | iskonto 3 ondalık sessiz yuvarlanıyordu | **DÜZELTİLDİ**: kredi limiti 2 haneye + tavan (alan:krediLimitiTl), iskonto en çok 2 ondalık (alan:iskontoYuzde) |
| PATCH /cari/{id} | Cari/CariModulu.cs:716 | Verildi deseni ✓; **`ad:""` / `tur:""` YAZILIYORDU** | ad 200; kredi limiti; iskonto | G | iskonto sessiz yuvarlama | **DÜZELTİLDİ**: zorunlu ad/tür null = KORU, `""` → 400 alanlı; ad kırpılır; kredi limiti null = SİL korunarak değer 2 hane + tavan; iskonto 2 ondalık |
| PATCH /cari/{id}/pep | Cari/CariModulu.cs:833 | Verildi deseni ✓ | — | G | sayı yok | uygun |
| POST /cari/{id}/veresiye | Cari/CariModulu.cs:952 | — | TL (15,2), has (15,6) | G | TL yuvarlanmadan hareket ve bakiyeye gidiyordu (eksi bakiyede yarım kuruş ters yöne kayabilir) | **DÜZELTİLDİ**: TL `PozitifTutar`, has `IsaretliHas` (6 hane AwayFromZero), eksi has → 400 |
| POST /cari/{id}/tahsilat | Cari/CariModulu.cs:1034 | — | TL, has, kasa bakiyesi | G | `tl==0 && has==0` kontrolü **ham** değerle: `tlTutari:0,001` geçip **0,00 tahsilat satırı** yazıyordu | **DÜZELTİLDİ**: önce yuvarla → sıfır/eksi kontrolü (alan:tlTutari/hasGrami), kasa bakiyesi tavanı HİÇBİR Add'den önce |
| POST /cari/{id}/duzeltme (+ açılış T-2) | Cari/CariModulu.cs:1283 | — | TL/döviz (15,2), has (15,6) | G | `0,001` "en az bir fark" kapısından geçip 0,00 fiş yazıyordu | **DÜZELTİLDİ**: önce yuvarla → sıfır kontrolü (düzeltme fişi); açılışta ham değerle (§ 1 sapma); tavanlar alanlı |
| POST /cari/{id}/doviz-veresiye · /doviz-tahsilat | Cari/CariModulu.cs:1415 / 1496 | — | döviz (15,2), TL karşılığı (15,2), kasa bakiyesi | G | `≤0` ham değerle | **DÜZELTİLDİ**: `PozitifTutar`, TL karşılığı tavanı alan:tutar, kasa bakiyesi tavanı Add'den önce |
| DELETE /cari/{id} | Cari/CariModulu.cs:879 | — | — | G | — | uygun |
| POST /bankalar | Banka/BankaModulu.cs:185 | — | — | G | — | **DÜZELTİLDİ**: `paraBirimi:""` hesaba **yazılıyordu** → artık TRY varsayılanı; liste dışı → 400 |
| PATCH /bankalar/{id} | Banka/BankaModulu.cs:216 | zorunlu alanlarda null = KORU ✓; **`paraBirimi:""` yazılıyordu, `iban:""` sessizce yok sayılıyordu, `"   "` banka adı yazılıyordu** | ad 100, sahip 150 | G | — | **DÜZELTİLDİ**: `""`/boşluk → 400 alanlı (paraBirimi, iban, bankaAdi, hesapAdi), kırpma, servis içi uzunluk |
| POST /bankalar/{id}/hareket (+ ekstre onayı) | Banka/BankaModulu.cs:359 | — | tutar + bakiye (15,2); karşı taraf 150 | G | tutar yuvarlanmadan satıra ve bakiyeye gidiyordu; servis yolunda ≤0 kontrolü YOKTU; **`tarih:"dün"` → FormatException → 500** | **DÜZELTİLDİ**: `PozitifTutar`, `BakiyeSigar`, `TryParse` → 400 alan:tarih, karşı taraf adı servis kapısı (ekstre yolu) |
| PATCH /bankalar/{id}/hareketler/{h} · storno · eşleştir | Banka/BankaModulu.cs:610 / … | Verildi deseni ✓ | — | G | sayı yok | uygun |
| POST /bankalar/hareketler/{h}/faturaya-donustur | Banka/BankaModulu.cs:503 | — | kdv_oran decimal(5,4) | G | ara toplam/KDV **ham** oranla hesaplanıp oran 4 haneye yuvarlanarak yazılıyordu (hesap ile kayıt farklı oran) | **DÜZELTİLDİ**: servis içi 0..1 + en çok 4 ondalık → 400 alan:kdvOran |
| DELETE /bankalar/{id} | Banka/BankaModulu.cs:277 | — | — | G | — | uygun |
| POST /pos/cihazlar · taksit-oranlari | Pos/PosModulu.cs:182 / 561 | — | oran (5,4) `OranDogrula` (4 ondalık reddi zaten var), valör int Range | G | — | uygun |
| PATCH /pos/cihazlar/{id} | Pos/PosModulu.cs:302 | null = KORU; **banka adı `""` sessizce yok sayılıyordu; üye işyeri/terminal `""` kolona `""` yazılıyordu** | MaxLength DTO | G | — | **DÜZELTİLDİ**: banka adı `""` → 400 alan:bankaAdi; isteğe bağlı no'lar `""` = SİL (NULL); ad hatasına alan:ad |
| PATCH /pos/cihazlar/{id}/taksit-oranlari/{o} | Pos/PosModulu.cs:596 | null = KORU | (5,4) | G | — | uygun |
| POST /pos/eslestir | Pos/PosModulu.cs:744 | — | eşleşen tutar (15,2) | G | `≤0` ve aşım kapıları **ham**, satır yuvarlanmış: 0,004 → **0,00 eşleşme satırı** | **DÜZELTİLDİ**: her satır önce yuvarlanır (`PozitifTutar`, alan `satirlar[i].tutar`), kapılar yuvarlanmış değerle |
| POST /pos/cihazlar/{id}/kasa-goc · pasife-al · tahsilat iptal · oto-öneri · eşleşme geri al | Pos/PosModulu.cs | — | — | G | sayı yok (gerekçe) | uygun |
| GET /cikti/{tur}/{id} | Cikti/CiktiModulu.cs:462 | — | — | — | — | **yazma ucu yok** — kapsam boş |

Tavanların tamamı `information_schema.columns` (kuyumcu_net) kolon tiplerinden alındı: kasalar.bakiye, kasa_hareketleri.tutar, gun_kapanislari.*, banka_hareketleri.tutar, banka_hesaplari.bakiye, cari_bakiyeler.tl/döviz, hareketler.tl_tutari/doviz_tutar → decimal(15,2); has → decimal(15,6); komisyon/kdv oranı → decimal(5,4); iskonto → decimal(5,2); kasa_hareketleri.aciklama varchar(500); kasalar.ad varchar(100); banka_hareketleri.karsi_taraf_ad varchar(150). İş eşiği uydurulmadı.

**Yeni ortak yardımcılar (`Kuyumcu.Api/Modules/KolonTavani.cs`):** `Yuvarla2`, `PozitifTutar` (önce 2 haneye yuvarla, sonra ≤ 0, kolon tavanı), `IsaretliTutar`, `IsaretliHas` (6 hane AwayFromZero), `BakiyeSigar`, `OndalikSiniri` (kolon ölçeğinden fazla ondalık → 400; sessiz yuvarlama yok, Pos `OranDogrula` ve PATCH-4 gerçek milyem kuralıyla aynı), `ParaBirimi` (TRY/USD/EUR/GBP; `""` → 400), `MetinSiniri` (varchar tavanı, 1406 → 500 yerine 400).

**PARA KURALLARI DEĞİŞMEDİ:** nakit kapısı, kart/havale payı, araç payı kapısı, çift bacak eşlemesi ve storno motoru aynen duruyor. Bu modüllerde yalnız girdi doğrulamasına dokunuldu. `KasaBakiyeKapisi.cs` dosyası değişmedi.

## 3. DoD sözleşmesi

Bu pakette yeni varlık ya da yeni kayıt türü doğmadı. Düzeltilen her uç, mevcut bir varlığın oluşturma, düzenleme ya da işlem yoludur.

- **Kasa:** PATCH + DELETE (pasif) + PATCH `aktif:true` ile geri dönüş. Ekranda Düzenle/Pasife al/Aktife al var (Kasalar.razor, değişmedi).
- **Kasa hareketi:** finansal varlıktır, hard DELETE yok. PATCH yalnız açıklama ve tarih alanlarını değiştirir; finansal düzeltme storno ile yapılır (değişmedi).
- **Gün kapanışı:** yeniden açma yalnız SAHİP rolüyle ve gerekçeyle yapılır.
- **Gider:** kasa hareketi olarak tutulur, düzeltmesi storno ile yapılır. Gider kategorisinde PATCH + pasife alma var.
- **Virman:** iki bacak birlikte stornolanır.
- **Cari:** PATCH + PEP + pasif. Cari hareketleri (veresiye, tahsilat, düzeltme) değişmezdir; düzeltme fişi/storno yolu değişmedi.
- **Banka hesabı:** PATCH + pasif + aktife al. Banka hareketinde PATCH açıklama/karşı taraf/tarih alanlarını düzeltir, finansal düzeltme storno iledir.
- **POS cihazı ve oranları:** PATCH + pasife al. Eşleşme geri al yolu var.
- **Denetim kaydı:** `DenetimServisi.YazmaKaydet` çağrıları değişmedi. Yeni 400'ler hiçbir Add'den önce fırlar; satır yazılmaz, iz düşmez.
- **[RolGerekli]:** hiçbir uçta değişmedi.

## 4. Testler

Yeni dosya `Kuyumcu.Tests/PatchBesTestleri.cs`, iki sınıf:
- `PatchBesHttpTestleri` (HTTP + servis yolu; servis yolunda kiracı bağlamı kurulmuş DI kapsamı kullanıldı, DataAnnotations atlanır).
- `PatchBesKolonTavaniTestleri` (saf sınır tablosu).

Toplam **27 test metodu / 53 vaka**. Her HTTP vakası hem durum kodunu hem alanı sınar (`alanlar` anahtarı, gövde ayrıştırılarak), ayrıca SQL'de satır sayısını ve bakiyeyi önce/sonra karşılaştırır.

| Koşum | Sonuç |
|---|---|
| `FullyQualifiedName~PatchBes` (son kod) | **53/53 geçti** |
| İlgili kümeler (Kasa, GunKapanis, Cari, Virman, Gider, Banka, Pos, ParaDemo, PARADEMO, Tahsilat, Ekstre, Duzeltme, Doviz, Acilis, Stopaj, Vezne, Masak) — ilk kod | **2403/2404 geçti**, 1 atlandı (önceden Skip'li) |
| PatchBes, RAPORYASLANDIRMA, Acilis ve Duzeltme kümeleri (açılış istisnasından sonra) | **328/328 geçti** |

Test sayısı: önce 10088 (10141 − 53), sonra **10141**. Yeni test vakası **53**.

## 5. Mutasyon tablosu

Yöntem: kaynak dosya `mut-yedek/<dosya>.orijinal-<kod>` altına kopyalandı. Ardından metinde **tam 1 kez** geçen literal değiştirildi (sürücü eşleşme 1 değilse durur), `dotnet test --filter "PatchBes|RAPORYASLANDIRMA"` koşuldu, yedek geri kopyalandı ve `cmp` çalıştırıldı. Sürücü: `ham/PATCH-5-olcum/mut5.py`. Sonuçlar `mut-run-1..4.txt` ve `mut-sonuc-*.json` dosyalarında.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | Gün kapat: sayılan nakit zorunluluğu kaldırıldı (yoksa 0) | `P5_gun_kapat_sayilanNakit_GONDERILMEZSE_400_alan_sayilanNakit_kapanis_YAZILMAZ` (3 vaka), `P5_gun_kapat_SERVIS_yolunda_da_sayilanNakit_zorunlu_alan_sayilanNakit` | AYNI |
| M1-2 | Kasa hareketi: `PozitifTutar` kaldırıldı | `P5_kasa_elle_hareket_SERVIS_yolunda_0_004_tutar_400_alan_tutar_satir_YOK` | AYNI |
| M1-3 | Kasa hareketi: net 0,00 kapısı `if (false)` | `P5_kasa_elle_hareket_girdi_kapisi_400_ALANLI_hareket_YAZILMAZ_bakiye_DEGISMEZ` (komisyon 1 · 0,01×0,4) | AYNI |
| M1-4 | Komisyon 4 ondalık → 9 | aynı test (0,12345 vakası) | AYNI |
| M1-5 | Kasa bakiye tavanı kaldırıldı | aynı test (9.999.999.999.999 vakası; ağ 400 **alan:bakiye** verdi) | AYNI |
| M1-6 | Kasa PATCH para birimi listesi kaldırıldı | `P5_kasa_PATCH_paraBirimi_BOS_ve_GECERSIZ_400_alan_SQL_DEGISMEZ_null_KORUR`, `P5_kasa_PATCH_servis_yolunda_paraBirimi_BOS_400_gecmisi_bos_kasada_da_YAZILMAZ` | AYNI (1. koşumda literal yorumun yarısını bıraktığı için derleme hatası verdi; literal düzeltilip 2. koşumda tekrarlandı) |
| M1-7 | Kasa POST ad sınırı 100 → 1000 | `P5_kasa_OLUSTUR_ad_101_…` (eski davranış ölçüldü: **500**, "Data too long for column 'ad'") | AYNI |
| M1-8 | Gün açma kırpılmış gerekçe kapısı `if (false)` | `P5_gun_ac_gerekce_BOSLUKLA_doldurulmus_400_alan_gerekce_gun_KAPALI_kalir` | AYNI |
| M2-1 | Virman `PozitifTutar` → `Math.Round` | `P5_virman_SERVIS_yolunda_0_004_tutar_400_alan_tutar_bacak_YOK` | AYNI |
| M2-2 | Virman etiket tavanı 500 → 5000 | `P5_virman_kasa_bacagi_aciklama_ADLARLA_500_asarsa_…` (eski davranış: **500**, "Data too long for column 'aciklama'") | AYNI |
| M3-1 | Gider `PozitifTutar` → `Math.Round` | `P5_gider_SERVIS_yolunda_0_004_tutar_400_alan_tutar_satir_YOK` | AYNI |
| M3-2 | Gider kategori ad null = KORU kaldırıldı | `P5_gider_kategori_PATCH_ad_null_ve_aktif_null_KORUR_bos_ad_400_alan_ad` | AYNI |
| M3-3 | Gider kategori aktif null → eski 400 | aynı test | AYNI |
| M4-1 | Tahsilat TL önce yuvarlanmıyor | `P5_cari_tahsilat_ONCE_yuvarla_sonra_sifir_400_ALANLI_hareket_YAZILMAZ` (2 vaka) | AYNI |
| M4-2 | Düzeltme TL yuvarlama + tavan kaldırıldı | `P5_cari_duzeltme_ONCE_yuvarla_sonra_sifir_ve_tavan_400_ALANLI_fis_YAZILMAZ` (3 vaka; açılış istisnasından sonra tekrar koşuldu) | AYNI |
| M4-3 | Veresiye `PozitifTutar` kaldırıldı | `P5_cari_veresiye_SERVIS_yolunda_0_004_TL_400_alan_tlTutari_hareket_YOK` | AYNI |
| M4-4 | Döviz veresiye `PozitifTutar` kaldırıldı | `P5_cari_doviz_veresiye_SERVIS_yolunda_0_004_400_alan_tutar_hareket_YOK` | AYNI |
| M4-5 | Cari PATCH boş ad 400 `if (false)` | `P5_cari_PATCH_bos_ad_ve_bos_tur_400_alan_null_KORUR_iskonto_3_ondalik_400_SQL_DEGISMEZ` | AYNI |
| M4-6 | Cari PATCH iskonto 2 ondalık → 9 | aynı test | AYNI |
| M4-7 | Tahsilat has yuvarlama + tavan kaldırıldı | `P5_cari_tahsilat_…` (2 vaka) | AYNI |
| M4-8 | Açılış istisnası kaldırıldı (`= true`) | `RAPORYASLANDIRMASinirTestleri.Kurus_yuvarlamasi_0_015_iki_kurus_0_004_listede_yok` | AYNI |
| M5-1 | Banka hareket `PozitifTutar` kaldırıldı | `P5_banka_hareket_SERVIS_yolunda_0_004_tutar_400_alan_tutar_dekont_YOK` | AYNI |
| M5-2 | Banka tarih eski `DateTime.Parse` | `P5_banka_hareket_girdi_kapisi_400_ALANLI_500_DEGIL_dekont_YAZILMAZ` (eski davranış: **500**, FormatException 'dün') | AYNI |
| M5-3 | Karşı taraf adı 150 → 1500 | **HAYATTA** — HTTP testinde DTO `StringLength(150)` aynı 400'ü veriyor (teşhis koşumunda gövde okundu). TEST AÇIĞI → servis yolu testi yazıldı (M5-5) | AYNI |
| M5-4 | Banka PATCH para birimi listesi kaldırıldı | `P5_banka_PATCH_paraBirimi_BOS_iban_BOS_banka_adi_BOSLUK_400_alan_SQL_DEGISMEZ` | AYNI |
| M5-5 | (M5-3 tekrarı, yeni testle) | `P5_banka_hareket_SERVIS_ekstre_yolu_karsiTarafAd_151_400_alan_dekont_YOK` | AYNI |
| M5-6 | Faturaya dönüştür KDV oranı 4 → 9 ondalık | `P5_banka_faturaya_donustur_kdvOran_4ten_fazla_ondalik_400_alan_kdvOran_fatura_YOK` | AYNI |
| M6-1 | POS eşleştir ön yuvarlama kaldırıldı | `P5_pos_eslestir_ONCE_yuvarla_0_004_400_alan_satir_tutari_esleme_YAZILMAZ` | AYNI |
| M7-1 | `PozitifTutar`: `y <= 0` → `y < 0` | 8 test (7 servis yolu 0,004 testi + saf `P5_PozitifTutar_once_yuvarla_sonra_sifir_ve_tavan`) | AYNI |

**28 mutasyon:** 27'si en az bir testi kırmızıya çevirdi. Hayatta kalan tek mutasyon M5-3; açığı kapatıldı ve aynı mutasyon M5-5 kodu altında tekrarlandığında kırmızı verdi. Her mutasyondan sonra `cmp` sonucu AYNI.

**⚠ TASARIM NOTU (Anayasa md.1, ölçülen olay):** Sürücü yedeği `shutil.copy2` ile geri kopyalıyor; bu komut dosyanın **eski mtime değerini koruyor**. Son mutasyon M4-8'den sonra geri yüklenen kaynak, mutasyonlu derlemeden daha eski zaman damgası taşıdığı için MSBuild Api derlemesini atladı. İkinci tam süit, mutasyonlu ikiliyle koştu ve `RAPORYASLANDIRMA` kırmızı çıktı (çıktı `tam-suit-2-kirli-ikili.txt`). Kaynaklar `touch` ile güncellendi, Api yeniden derlendi (dll 05:33:29) ve tam süit temiz ikiliyle üçüncü kez koşuldu (§ 6). Canlı yayın (Release) her seferinde güncel kaynaktan derlendi; bu olaydan etkilenmedi. Sonraki sürücülerde geri yüklemeden sonra `touch` zorunlu olmalı.

## 6. Tam süit ve denetim

**Son kodla, temiz ikiliyle tam süit:** `KUYUMCU_TEST_DB=kuyumcu_test_t2 dotnet test … -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-5-son2`. Sonuç: **Toplam 10141 · Geçti 10130 · Başarısız 9 · Atlandı 2**. Atlananlar önceden Skip'li: `TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli` ve `SATIS3EkranTestleri.DOG_T24_…`.

| Kırmızı | Sahibi | Bu paketle ilgisi |
|---|---|---|
| `ZOBELGESinirTestleri.ZOBS12_…`, `ZOBS13_…`, `ZOBELGESinirTestleriTur3.ZOBT3_01…06` (8 test) | **RAPOR-GORUNUM (t1)**: testler `Pages/Raporlar.razor` içindeki `BugunHas` metodunu yansımayla çağırıyor ve NullReferenceException alıyor. Raporlar.razor o şeritte değişiyor (04:31) | yok — razor dosyasına dokunulmadı |
| `Toplu6GiderTestleri.T6_Para_aktarimi_geri_alma_onayi_ve_sonucu_moda_gore_tek_ad(mod: "BASIT")` | **TOPLU-6 (t9)**: `Toplu6Testleri.cs` 05:27'de değişti. Ekran metni beklentisi farklı ("Bu para aktarımının…" ↔ "Virmanın İKİ BACAĞI…"). bUnit, sahte API ile | yok. Tek başına koşulduğunda **2/2 yeşil** (yük altında ve şeridin yarım işi) |

**Bu paketin yeni kırmızısı: 0.**
- Birinci tam süitte görülen `RAPORYASLANDIRMA…0_004` kırmızısı bu paketin ürünüydü. Açılış istisnasıyla kapatıldı ve son süitte yeşil.
- `RaporGorunumDbTestleri.RG03` birinci süitte kırmızıydı, son süitte yeşil (RAPOR-GORUNUM şeridinin işi).

Devir notunda karşılaştırılacak bir kırmızı listesi verilmediği için bu fark, kırmızıların sahiplerine göre ayrıştırılarak ölçüldü.

**Derleme:** `dotnet build Kuyumcu.sln` → **0 Uyarı · 0 Hata** (son, zorla yeniden derleme). Ara derlemelerde görülen 2 uyarı başka şeritlerin dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601 ve `KesifP2Testleri.cs:455` xUnit2029. Bu paketin ara uyarısı `KasaModulu.cs:558` CS8602 idi; giderildi.

**Denetim:** `bash dotnet/denetim.sh` → rc=1, **303 ✓**. Tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ). Ara koşumda görülen `P-36b GIRDI_AILESI_DISI_BICIM 3` ihlali `Pages/Raporlar.razor:1814/1824` satırlarındaydı (RAPOR-GORUNUM); son koşumda o şerit tarafından kapanmıştı. Ham çıktı: `ham/PATCH-5-olcum/denetim.txt`.

## 7. Canlı doğrulama (API 5162 · Release anlık görüntüsü · bağımsız SQL, kuyumcu_net)

API, `/private/tmp/kuyumcu-serit/PATCH-5/yayin` dizininden `--urls http://localhost:5162` ile başlatıldı. Sırlar yalnız süreç ortamına verildi; sistem anahtarı rastgele üretildi ve chmod 600 ile korundu. Betik `ham/PATCH-5-olcum/canli-p5.mjs`, son çıktı `canli-p5-kosum.txt`. SQL hatası yalnız çıkış koduyla yazılıyor.
- 1. koşum geçersiz sayıldı: yeni kiracıda kasa yoktu, uçlar 404 döndü (`canli-p5-kosum-1.txt`).
- 2. koşum geçerli (`canli-p5-kosum-2.txt`).
- 3. koşum, Range iletisi Türkçeleştirildikten sonra yapıldı (son kanıt).

**5xx sayısı: 0.** SQL sütunları: `para_birimi bakiye kasaHareket gunKapanis kasaSayisi` / `ad tur iskonto tl has hareket` / `para_birimi banka_adi bakiye hareket`.

| İstek | Beklenen | Gerçek | SQL önce → sonra |
|---|---|---|---|
| POST gun-kapanisi `{}` | 400 alan:sayilanNakit | 400 [sayilanNakit] "Sayılan nakit zorunludur…" | `TRY 0.00 0 0 1` → aynı |
| POST gun-kapanisi `{sayilanNakit:null}` | 400 | 400 [sayilanNakit] | aynı |
| POST gun-kapanisi `{sayilanNakit:1e14}` | 400 | 400 [sayilanNakit] "Sayılan nakit 0 ile 9.999.999.999.999,99 arasında olmalıdır" | aynı |
| Kasa hareketi komisyon 1 (net 0,00) | 400 alan:komisyonOran | 400 [komisyonOran] | `TRY 0.00 0 0 1` → aynı |
| Kasa hareketi 0,01 × (1−0,6) | 400 | 400 [komisyonOran] | aynı |
| Kasa hareketi komisyon 0,12345 | 400 | 400 [komisyonOran] "en çok 4 ondalık" | aynı |
| Kasa hareketi tutar 1e14 | 400 alan:tutar | 400 [tutar] | aynı |
| Kasa hareketi açıklama 501 | 400 alan:aciklama | 400 [aciklama] | aynı |
| OLUMLU: GIRIS 1000 NAKIT | 200 | 200 | `TRY 1000.00 1 0 1` |
| PATCH kasa `{paraBirimi:""}` / `{ad:"  "}` | 400 alanlı | 400 [paraBirimi] / 400 [ad] | `TRY 1000.00 1 0 1` → aynı |
| POST kasa ad 101 / paraBirimi XYZ | 400 alanlı | 400 [ad] / 400 [paraBirimi] | kasa sayısı 1 → 1 |
| OLUMLU: POST kasa ad 100, `paraBirimi:""` | 200, TRY | 200 · SQL `TRY` | — |
| POST virman açıklama 500 (adlarla > 500) | 400 alan:aciklama | 400 [aciklama] "…açıklamaya en fazla 375 karakter kalıyor" | `TRY 1000.00 1 0 2` → aynı · hedef 0.00 |
| PATCH gider kategori `{ad:null,aktif:null,aciklama:"not"}` | 200, ad/aktif korunur | 200 · SQL `P5 Kira 1 not` | — |
| PATCH gider kategori `{ad:" "}` | 400 alan:ad | 400 [ad] | değişmedi |
| Tahsilat tlTutari 0,001 HAVALE | 400 alan:tlTutari | 400 [tlTutari] | `P5 Müşteri MUSTERI NULL 0.00 0.000000 0` → aynı |
| Tahsilat hasGrami 0,0000004 | 400 | 400 [tlTutari] ("en az bir tutar") | aynı |
| Düzeltme tlDelta 0,001 / hasDelta 1e10 | 400 alanlı | 400 [tlDelta] / 400 [hasDelta] | aynı |
| PATCH cari `{ad:""}` / `{tur:""}` / `{iskontoYuzde:12.345}` | 400 alanlı | 400 [ad] / [tur] / [iskontoYuzde] | aynı |
| OLUMLU: PATCH cari `{ad:null,tur:null,iskontoYuzde:12.34}` | 200, ad/tür korunur | 200 · SQL `P5 Müşteri MUSTERI 12.34` | — |
| POST bankalar `paraBirimi:""` | 200, TRY (`""` yazılmaz) | 200 · SQL `TRY P5 Banka 0.00 0` | — |
| Banka hareketi tarih "dün" | 400 alan:tarih | 400 [tarih] | `TRY P5 Banka 0.00 0` → aynı |
| Banka hareketi karşı taraf 151 | 400 | 400 [karsiTarafAd] (DTO iletisi; § 1) | aynı |
| Banka hareketi tutar 1e14 | 400 alan:tutar | 400 [tutar] | aynı |
| PATCH banka `{paraBirimi:""}` / `{iban:""}` / `{bankaAdi:"   "}` | 400 alanlı | 400 [paraBirimi] / [iban] / [bankaAdi] | aynı |
| OLUMLU: gün kapat `{sayilanNakit:1000}` | 200 | 200 · SQL `1000.00 1000.00 0.00` | — |

Açılış istisnası (§ 1) canlı yayından sonra eklendi. Bu değişiklik yalnız `AcilisYaz` yolunu etkiler. Canlıda koşulan `/duzeltme` (`CARI_DUZELTME`) davranışı aynı kaldı. Açılış davranışı DB testiyle (RAPORYASLANDIRMA) ve M4-8 mutasyonuyla kanıtlandı.

**Temizlik:**
- API (PID dosyasından) durduruldu. `lsof -iTCP:5162 -sTCP:LISTEN` → **0**.
- Vekil (5163) ve CDP Chrome açılmadı (ekran işi yok).
- 5199/5200'e dokunulmadı.
- Üç test kiracısı (`01M3ZRBENQ7WBSKZRRQB9KNS76`, `01M3ZRC3GMHKWA5JSJPM4Z5G5M`, `01M3ZRFBMSXKQGG9B5PNVVX8AF`, adları `PAKETPATCH-5-TEST …`) `zsh betikler/kiraci-temizle.sh <id> --uygula` ile silindi; üçünde de rc 0.
- tenant_id kolonu olan **75** tabloda bu üç kimlik için satır toplamı **0**. `tenants` içinde `PAKETPATCH-5%` **0**.
- `tenants` başlangıçta **16**, sonda **16**. Arada başka şeritlerin kiracıları açılıp kapandı (ör. `PAKET-TOPLU-6-TEST`); QA-TEST- kiracılarına dokunulmadı.
- `fikstur.json` ve `sistem.anahtar` `printf ''` ile boşaltıldı. Ham çıktılarda MySQL parolası `grep -F` ile arandı: **0** dosya.

## 8. Değişen dosyalar (bu paket)

`git status --porcelain | grep -v -E "/(bin|obj)/"` (bu paketin dosyaları; ağaçtaki diğer değişiklikler başka şeritlerin):
- ` M dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — GunKapanisiDto `decimal?` + Range iletisi; Gün Kapat zorunlu/yuvarlama; GunAc kırpılmış gerekçe; HareketGir girdi kapısı; Olustur/Guncelle ad + para birimi; HareketDuzelt alanlar; KasaHareketDto açıklama MaxLength(500).
- ` M dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` — Olustur/Guncelle (ad/tür `""`, kredi limiti, iskonto); veresiye, tahsilat, düzeltme (açılış istisnası), döviz veresiye/tahsilat.
- ` M dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` — HesapOlustur/HesapGuncelle; HareketEkleIc (tutar, tarih, karşı taraf, bakiye); FaturayaDonustur KDV oranı.
- `?? dotnet/Kuyumcu.Api/Modules/KolonTavani.cs` (dosya izlenmiyor; bu pakette 8 yardımcı eklendi).
- `?? dotnet/Kuyumcu.Api/Modules/Gider/` (`VirmanModulu.cs`, `GiderModulu.cs` değişti; klasör izlenmiyor).
- `?? dotnet/Kuyumcu.Api/Modules/Pos/` (`PosModulu.cs` değişti; klasör izlenmiyor).
- `dotnet/Kuyumcu.Tests/PatchBesTestleri.cs` (YENİ).
- `dokumanlar/09-uygulama-kayitlari/PAKET-PATCH-5-uygulama-raporu.md` (YENİ) ve `ham/PATCH-5-olcum/` (YENİ).

**Migration:** gerekmedi (şema değişmedi). `KasaBakiyeKapisi.cs`, `CiktiModulu.cs`, ekran, css, sözlük ve Contracts dosyalarına dokunulmadı.

## 9. Ekran / gerçek tarayıcı kuralı

Ekrana dokunan iş yok. Razor, css, bileşen, sözlük ve dil verisi değişmedi; tarayıcı ölçümü kapsam dışı. Sunucunun yeni 400'lerinin ekranla ilişkisi kodda okunarak kontrol edildi:
- **Gün Kapat:** Kasalar.razor ve KasaRaporu.razor boş gönderimi zaten istemcide engelliyor (`sayiKapisi` / alan altı `ZB.KasaSayimZorunlu`). Sunucu kuralı yalnız API'yi doğrudan çağıranı etkiler.
- **Kasa/Banka/Cari düzenle gövdeleri** (Kasalar.razor:928, Banka.razor:1355, Cariler.razor:2349) `""` zorunlu alan göndermiyor; istemci tarafında `Zorunlu` kapıları var.
- Yeni 400'lerin hepsi `alanlar` taşır. Ekranın `FormDurumu.SunucuMesaji/BantMesaji` yolu, sessiz etkisizlik doğurmadan bandı ya da alan altını gösterir.
- Bir sınır durumu: istemcide 3 ondalık iskonto yazılırsa artık 400 döner (eskiden sessizce yuvarlanıyordu). Bu § 11'e yazıldı.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tarama tablosu (uç · dosya:satır · PATCH null · kolon tavanı · Strict · yuvarlama · karar) | KARŞILANDI | § 2 |
| Bulunan tüm 500 / sessiz yanlış okuma / yuvarlama sıfırı / PATCH ezme durumları kapatıldı | KARŞILANDI | § 2: 500'ler (kasa adı, virman etiketi, banka tarihi, ekstre yolu karşı taraf); sıfır satırlar (kasa net, virman, gider, tahsilat, düzeltme, veresiye, döviz, banka, POS eşleşme); sessiz okuma (komisyon, iskonto, KDV oranı, para birimi `""`, gün açma gerekçesi); PATCH ezme (cari ad/tür `""`, kasa/banka para birimi `""`, banka iban/ad, gider kategori null, POS banka adı) |
| Gün Kapat sayılan nakit sunucuda zorunlu (400 alan:sayilanNakit) | KARŞILANDI | M1-1 · 4 vaka · canlı 3 istek 400 + SQL değişmedi |
| Her düzeltilen uç için sınır testi | KARŞILANDI | § 4 (27 metot / 53 vaka) |
| Canlı API (5162) + bağımsız SQL (satır yazılmaz/değişmez) | KARŞILANDI | § 7 (5xx 0) |
| Mutasyon (≥1 kırmızı, cmp AYNI) | KARŞILANDI | § 5 (28 mutasyon; hayatta kalan M5-3 test açığıyla kapatıldı) |
| build 0/0 | KARŞILANDI | § 6 |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | § 6 (303 ✓, tek ✗ P-30) |
| Tam süitte bu paketin yeni kırmızısı 0 | KARŞILANDI | § 6 (9 kırmızı: 8 RAPOR-GORUNUM, 1 TOPLU-6) |
| PARA KURALLARI değişmez | KARŞILANDI | nakit/araç payı kapısı, çift bacak, storno kodu değişmedi; ilgili kümeler 2403/2404 |
| Sır basılmadı (canlı betikte hata gövdesi değil çıkış kodu) | KARŞILANDI | betik SQL hatasında yalnız `status`; parola taraması 0 |
| Bellek: en çok 1 API + 1 başsız Chrome; tam süit bir kez | KISMEN | API 1 (aynı anda tek); Chrome açılmadı. Tam süit **3 kez** koşuldu: 1. açılış kırmızısını gösterdi ve düzeltildi; 2. mutasyonlu ikiliyle koştuğu için geçersiz (§ 5 notu); 3. temiz ikiliyle koşan son geçerli koşum |
| Dosya alanı | KARŞILANDI | yalnız listelenen dosyalar ve PatchBesTestleri.cs |
| Test kiracısı silindi, tenants sayısı geri döndü | KARŞILANDI | 75 tablo 0 satır; 16 → 16 |
| COMMIT YOK, dosya silme yok | KARŞILANDI | git add/commit yok; rm yok |
| Mevzuat | Uygulanmadı | yasal eşik/oran yazılmadı; KDV oranı yalnız biçim (ondalık) olarak sınandı, değer `KdvAyarlari`'ndan gelmeye devam ediyor |

## 11. Açık kalanlar

- **Cari bakiye yazıcısı (`Common/CariBakiyeServisi.cs`, dosya alanı dışı):** birikmiş bakiye kolonu (`cari_bakiyeler.tl_bakiye` 15,2) taşarsa hâlâ ağ (1264 → 400) devreye giriyor ve alanı **kolondan** veriyor (tlBakiye). Uç kapısı tek işlemin tutarını sınıyor. Bakiye toplamı için kapı o dosyanın sahibine öneriliyor.
- **POS `OranDogrula` ve `ValorDogrula` 400'leri alansız.** Dört uç ortak fonksiyonu kullanıyor; ekran bandı gösteriyor. Alan adı (pesinKomisyonOran/komisyonOran) için fonksiyona parametre eklemek ayrı bir küçük iş.
- **PATCH /pos/cihazlar ve PATCH /bankalar/{id}'de isteğe bağlı alan `null = SİL`** Verildi bayrağı gerektiriyor (DTO'larda yok; şu an null = KORU, `""` = SİL). PATCH-3 sözleşmesinin "bir sürüm boyunca `""` = sil" geriye uyum yolu çalışıyor; Verildi'li DTO ayrı paket.
- **Banka hesabı para birimi değişimi:** kasadaki "geçmişi olan kasada değişmez" kuralı (Mehmet K4) banka hesabında yok. Bu bir iş kuralı; girdi doğrulaması değil, bu paketin dışında. Ekibe soru olarak bırakıldı.
- **Kasa adı POST'ta en az 2 karakter kuralı yok** (PATCH'te var; UX-S26 tutarsızlığı). Mevcut testlerin fikstürlerini etkileyebileceği için bu pakette yalnız üst sınır eklendi.
- **Ekran:** iskonto alanı 3 ondalık kabul ediyorsa artık sunucu 400 veriyor. Cariler.razor'da istemci kapısı yok; dokunulmadı (TOPLU-6 / ekran şeridi dosyası). Hata bandı görünüyor, sessiz değil.
- **Mutasyon sürücüleri:** geri yüklemeden sonra `touch` (mtime) zorunlu olmalı (§ 5 tasarım notu). Orkestratörün `mut*.py` şablonlarına eklenmesi önerilir.
- **Başka şeritlerin kırmızıları (sahipleriyle):**
  - RAPOR-GORUNUM (t1): `ZOBELGESinirTestleri.ZOBS12/ZOBS13` ve `ZOBELGESinirTestleriTur3.ZOBT3_01…06`, sebebi `Raporlar.razor` `BugunHas` yansıma çağrısının null dönmesi.
  - TOPLU-6 (t9): `Toplu6GiderTestleri.T6_Para_aktarimi_geri_alma_onayi_ve_sonucu_moda_gore_tek_ad(BASIT)`; tek başına 2/2 yeşil.

## 12. Kurucu / mali müşavir

Bu pakette mevzuat hükmü yazılmadı ve değiştirilmedi. Açılış (devir) satırlarında 0,005 TL altındaki artıkların 0,00'a yuvarlanıp kayda geçmesi (sıfır tutarlı ACILIS satırı) eski davranış olarak korundu. Devirde bu artıkların hiç yazılmaması mı yoksa toplu bir yuvarlama farkı satırına mı aktarılması gerektiği bir muhasebe kararıdır. Karar mali müşavire bırakıldı; kod tahmin yürütmedi.
