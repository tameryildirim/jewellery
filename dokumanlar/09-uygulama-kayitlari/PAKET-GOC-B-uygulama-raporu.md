# PAKET GOC-B — Kimlik ve uyum ekranlarının ortak girdi + ortak ızgara göçü · uygulama raporu (08.10.2026)

> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t1 · canlı API 5160 · vekil 5161 · CDP 9701–9715 · `KUYUMCU_TEST_DB=kuyumcu_test_t1`.
> Kaynak (tek doğruluk): `GIRDI-KURALLARI-ENVANTER-2026-10-08.md` — (B) K1–K34, (C) bu ekranların satırları, (E) Faz 2 GOC-B, (F) F2/F3, (G) G1 + G2'nin 13 satırı.
> COMMIT YOK. Dosya silinmedi/taşınmadı. 5199/5200'e dokunulmadı. Sır/parola değeri çıktıya yazılmadı (fikstürler chmod 600).
> Migration: **gerekmedi** (şema değişikliği yok).
> **TUR 2 (08.10 22:10–23:00):** doğrulayıcının 2 KRİTİK + 1 ORTA + 2 DÜŞÜK bulgusu kapatıldı — ayrıntı **§ 0**. Aşağıdaki tur 1 bölümlerinde tur 2'nin geçersiz kıldığı cümleler yerinde düzeltildi ("⟶ tur 2" işaretli).

## 0. TUR 2 — doğrulayıcı bulgularının kapanışı

| # | Bulgu (doğrulayıcı) | Kök neden | Düzeltme | Kanıt |
|---|---|---|---|---|
| K1 | **KRİTİK — evrak düzenlemesi kayıtlı borçlu kimlik no'yu sessizce SİLİYORDU** (`borcluKimlikNo:null`; canlıda kimlik + hash silindi) | Kimlik şifreli, listede DÖNMEZ → form değeri bilemez; tur 1 boş alanı `null` gönderiyordu, sunucu `null = sil` sayıyordu | **İki kat koruma, "yalnız-yazılır alan" modeli:** (1) EKRAN: kimlik YALNIZ kullanıcı yeni değer yazdıysa gövdeye girer; dokunulmadıysa alan gövdede HİÇ YOK. Silme yalnız AÇIK onay kutusu "Kimlik no'yu kaldır" (`#evrak-form-borclu-kimlik-kaldir`) → `borcluKimlikSil:true`; işaretliyken kutu kilitli + "Kaydet'e basınca kayıtlı kimlik no silinecek." (2) SUNUCU (`EvrakDuzeltDto`): `borcluKimlikNo` null/boş = DEĞİŞMEZ (genel "null ise sil" sözleşmesinin **gerekçeli istisnası**: istemciye hiç dönmeyen şifreli alan geri gönderilemez); silme yalnız `borcluKimlikSil:true`; ikisi birlikte → 400 alan `borcluKimlikNo`. (3) LİSTE: `EvrakSatiri.BorcluKimlikKayitli` (yalnız var/yok, değer DÖNMEZ) → düzenlemede "Kayıtlı kimlik no gizli tutulur ve değişmez. Değiştirmek için yenisini yazın." ipucu ve kaldır kutusu yalnız kayıtlıysa görünür | Doğrulayıcı testleri **GOCB_E01 ve GOCB_H03 YEŞİL**; yeni GBH10, GBE05; canlı: eski ekran gövdesi (null) sonrası kimlik + hash AYNI, alan yokken AYNI, yaz+sil 400, açık sil sonrası `kimlik-YOK hash-YOK`; liste yanıtında numara yok |
| K2 | **KRİTİK — evrak tutarı 999.999.999,99 ekranda geçiyor, sunucu "sıfırdan büyük olmalı" diye reddediyordu** | `EvrakGirisDto.Tutar` üzerinde eski `[Range(0.01, 999_999_999)]` controller yolunda K9'dan önce çalışıyordu | Range kaldırıldı → `[GD.Tutar]` (Core `TutarKurali`, tek tavan = decimal(15,2) = 9.999.999.999.999,99 = ekranın tavanı; kolon `kiymetli_evraklar.tutar decimal(15,2)` SQL'de doğrulandı). Servis yolu `GirdiKapisi.Tutar` zaten aynı kural | **GOCB_H01 YEŞİL**; GBH11 + GBS03; canlı 999.999.999,99 → 201 (SQL `999999999.99`), 10^13 → 400 "Değer çok büyük (en çok 9.999.999.999.999,99).", 0 → 400 "Değer 0'dan büyük olmalı." |
| O1 | **ORTA — 18 yaş altı doğum tarihi koda sabit ENGEL** (cari POST/PATCH + MASAK kimlik tespiti) | TMK md.11 erginliği TANIMLAR, küçüğün müşteri/MASAK kimlik kaydını yasaklamaz; tur 1 yasak gibi uygulamıştı (mevzuat kuralı ihlali: birincil kaynak yasağı desteklemiyor) | Core'a yeni tür **`TarihTuru.KisiDogum` (K8f)**: gelecek yok + en çok 120 yıl; **18 altı ENGEL DEĞİL** (paket öncesi davranış birebir). Ekranda (Cariler yeni/düzenle, MasakFormu) yalnız **uyarı** ipucu: "Kişi 18 yaşından küçük. Tarihi kontrol edin." — Kaydet kilitlenmez. `TarihTuru.Dogum` (18 engelli) Core'da AYNEN duruyor (GIRDI-AILESI-2 testleri), hiçbir uçta kullanılmıyor; engele çevirmek **kurucu/hukuk kararı** (§ 12) | GBH01 (17 yaş POST 201 + SQL doğum), GBS04/GBS06/GBS07 (MASAK kapısı + MasakFormu kilidi 17 yaşta geçer), GBE07 (uyarı var, hata yok); canlı 17 yaş → 201, SQL `2009-10-08 · 17 yaş`; 3000/1899 hâlâ 400 |
| D1 | **DÜŞÜK — evrak borçlu kimlik yalnız 10/11 rakam; yabancı borçlu girilemiyordu; DTO 10-20 diyordu** | Tur 1 `KimlikKurali.TcknVeyaVkn` ile daraltmış, sapma yazılmamıştı | Core'a **`KimlikKurali.TcknVknVeyaPasaport`**: yalnız rakamsa K4/K5 (KimlikNoDogrulayici — yeniden yazılmadı), harf içeriyorsa K7 pasaport (5–20 harf/rakam, büyük harfe normalleşir). Öznitelik `TcknVknVeyaPasaportAttribute`; DTO `StringLength(20)` (MinimumLength kalktı → kuralla eşit). Ekranda **"Kimlik türü"** radyosu (TC kimlik / vergi no · Yabancı (pasaport no)) → `TcknVeyaVkn` ya da `Pasaport` bileşeni, aynı id; yabancıda etiket "Borçlu pasaport no" | GBH11 (u1234567x → 201, saklanan `U1234567X`; AB-12 → 400 PasaportBicim; 11111111111 → 400), GBE06 (alan altı + kilit + PATCH `U1234567X`); M2-5 kırmızı. ⚠ Yalnız-rakam yabancı pasaport (ör. 9 hane) TCKN/VKN kuralına düşer — § 11 |
| D2 | **DÜŞÜK — ölçer: Uyum sekme rozeti K3, Cariler tür süzgeci K12** | K3: `sikisma-olc.js` sekmenin sayı rozetini ayrı kırık satır sayıyor (ölçer betiği orkestratörün; görüntüde tek satır). K12: tur 2 ölçümünde yeniden üretilemedi | K3 ölçer düzeltmesi bu paketin alanı DIŞI (betikler/sikisma-olc.js — orkestratör) → § 11. **K12 `#cari-tur-filtre`: kendi ölçümümde 8/8 kombinasyonda K12 = 0** (`olcum-t2c/olcum.jsonl`, /cariler 1280+375 × BASİT+PRO × açık+koyu); yeniden üretilemedi, ortak `Secici` bileşenine dokunulmadı | § 0.3 tablo |

### 0.1 Tur 2 testleri
- Yeni: **GBH10** (kimlik yalnız-yazılır: alan yok/null korur, liste yalnız bayrak, yaz+sil 400, açık sil siler + hash null), **GBH11** (tutar tek tavan, doğru ileti, yabancı pasaport), **GBE05** (dokunulmayan kimlik gövdede YOK; kaldır → `borcluKimlikSil`), **GBE06** (yabancı pasaport ekranı), **GBE07** (17 yaş uyarı, hata yok).
- Güncellenen (gerekçe: tur 2 kararı, hiçbiri gevşetilmedi — yerine ters yön sınanıyor): GBS03 (KisiDogum, TcknVknVeyaPasaport, Tutar özniteliği + Range YOK), GBS04/GBS06/GBS07/GBH01 (17 yaş artık GEÇER; gelecek/120 yıl hâlâ red).
- Doğrulayıcının `Dogrulayici/GOCBSinirTestleri.cs` dosyasına DOKUNULMADI; 8/8 yeşil (GOCB_S01–S04, H01–H03, E01).
- Filtre `~GocB|~GOCB_`: **39/39**. Geniş filtre (Evrak|Cari|Masak|Girdi|Tckn|Uyum|GocB|ZoBelge, 11285 test): ara koşumda 2 kırmızı → **ZB60** (benim: Razor öznitelikte gövdeli lambda — yöntemlere çevrildi, yeşil) ve **DilEkranTestleri Bildirimler IsAdi ≥5** (bu turda Bildirimler'e dokunulmadı; test ve sayfa 21:49–21:50'de PAKET DIL-EKRAN şeridince değiştirilmişti — son tam süitte yeşil).
- **Tam süit (son kod, tur başına bir kez):** `Başarılı! - Başarısız: 0, Başarılı: 11283, Atlanan: 2, Toplam: 11285` (`/private/tmp/kuyumcu-serit/trx-pGOC-B/tam-t2.trx`). Tur 1 sonu: 11262 test / 0 kırmızı → tur 2: 11285 / 0 kırmızı. **Yeni kırmızı 0.**
- Derleme `dotnet build Kuyumcu.sln`: **0 Hata**; 7 uyarı hepsi başka paketlerin test dosyalarında (Yildiz2 ×2, SonKalanlar ×2, KesifP2, DilEkran, Dogrulayici/SUBEKAPSAMA) — bu paketin dosyalarında 0 (GocBTestleri'nde çıkan xUnit2029 düzeltildi).

- **denetim.sh (tur 2):** mutasyonlardan sonra `DENETİM BAŞARISIZ` yalnız P-30 (kurucu kalemi). Son koddan sonraki ikinci koşumda ek olarak **OLCEK-1 YUZ_OLCU_ENVANTER_DISI = 3** çıktı: kural yalnız `wwwroot/css` dosyalarını sayar; bu paket hiçbir CSS'e dokunmadı, o arada `tema-yildiz.css` 22:46'da **YILDIZ (t5) şeridince** değiştirilmişti → sahibi YILDIZ. Bu paketin sayaçları: HAM_TABLO 58, GIRDI_TUR_GENEL 9, DTO_DECIMAL_RANGESIZ 17, UZUNLUK_FARKI 86, HAM_TARIH 6, HAM_INPUT 23, HAM_SELECT 17 — hepsi ✓ (artış 0).

### 0.2 Tur 2 mutasyonları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-B-t2/` → TEK eşleşme → `~GocB|~GOCB_` → geri kopya + mtime ileri → `filecmp` birebir; betik `GOC-B/betik/mut2.py`, sonuç `GOC-B/mut2-sonuc.jsonl`)
| Kod | Kural kapatıldı | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M2-1 | Sunucu: PATCH'te null kimlik yine siler | GOCB_H03, GBH10 | AYNI |
| M2-2 | Ekran: dokunulmayan kimlik de gövdeye konur | GOCB_E01, GBE05 | AYNI |
| M2-3 | Evrak tutarı K9 yerine eski Range | GOCB_H01, GBS03, GBH11 | AYNI |
| M2-4 | KisiDogum'a 18 yaş engeli geri | GBE07, GBS04, GBS06, GBS07, GBH01 | AYNI |
| M2-5 | Harfli kimlik de TCKN/VKN kuralına düşer | GBH11 | AYNI |
| M2-6 | Kimlik yaz + sil birlikte 400 kapalı | GBH10 | AYNI |
| M2-7 | Liste `borcluKimlikKayitli` hep false | GBH10 | AYNI |
7/7 kırmızı, 7/7 cmp AYNI. ⚠ Dürüstlük notu: ilk koşumda geri kopya `copy2` eski mtime'ı koruduğu için MSBuild geri alınan kaynağı yeniden derlemedi (M2-4..M2-7 sonuçları M2-3 kalıntısıyla kirlendi; `mut2-sonuc-tur-a.jsonl`); M2-2'nin ilk deseni derleme hatası verdi, M2-4 bir kez başka şeridin eşzamanlı derlemesinin dosya kilidine takıldı. Betik düzeltildi (mtime ileri), 7'si temiz yeniden koşuldu; tablo SON koşumdur. Son mutasyondan sonra temiz derleme 0 hata, `~GocB|~GOCB_` 39/39.

### 0.3 Tur 2 canlı doğrulama (API 5160, Development, `kuyumcu_net`; kiracı `PAKETGOC-B-TEST …` = 01M4EFPEDC9XZE6WR9PKAT5GW4; betik `GOC-B/canli_gocb2.py`, sonuç `GOC-B/canli-sonuc2.json`) — **UYUMSUZ 0 / 53**
Tur 1'in 40 adımı aynen yeniden koşuldu (değişen tek beklenti: "Cari doğum 17 yaş" 400 → **201**), ek 13 adım:
| Adım | Beklenen | Gerçek | İleti / SQL |
|---|---|---|---|
| Cari doğum 17 yaş | 201 | 201 | SQL: `2009-10-08 · 17 yaş` |
| SQL önce (GB-5 PATCH ile kimlik yazıldıktan sonra) | — | — | `kimlik-VAR hash VAR` |
| Evrak PATCH eski ekran gövdesi (`borcluKimlikNo:null`, yalnız not) | 200 | 200 | SQL: kimlik + hash AYNI |
| Evrak PATCH kimlik alanı yok | 200 | 200 | SQL: kimlik + hash AYNI |
| Liste: `borcluKimlikKayitli=true`, numara yanıtta YOK | ✓ | ✓ | — |
| PATCH kimlik yaz + sil birlikte | 400 | 400 | "Kimlik no hem yazıldı hem kaldırılmak istendi; birini seçin." · borcluKimlikNo |
| PATCH AÇIK kimlik sil | 200 | 200 | SQL: `kimlik-YOK hash-YOK ikinci not` |
| Evrak tutar 999.999.999,99 | 201 | 201 | SQL `999999999.99` |
| Evrak tutar 10^13 | 400 | 400 | "Değer çok büyük (en çok 9.999.999.999.999,99)." · tutar |
| Evrak tutar 0 | 400 | 400 | "Değer 0'dan büyük olmalı." · tutar |
| Yabancı borçlu `u1234567x` | 201 | 201 | SQL GB-Y1: kimlik VAR, hash VAR (şifreli; düz metin `U1234567X` HTTP testinde GBH11) |
| Bozuk pasaport `AB-12` | 400 | 400 | "Pasaport no 5–20 harf ya da rakam olmalı, boşluksuz." · borcluKimlikNo |
SQL son: evrak 3 (`GB-5,GB-T1,GB-Y1`; tutar `10000.00, 999999999.99, 10000.00`); yaptırım `BILDIRILDI 2026-10-08`; şüpheli `1500.25 ACIK`; tercih `grid:uyum:uy-supheli` 110.

### 0.4 Tur 2 gerçek başsız Chrome (vekil 5161, CDP 9705–9716; `sikisma-olc.js`; `GOC-B/olc2t.mjs`; ham `olcum-t2/` → `olcum-t2b/` → SON `olcum-t2c/`)
Görünümler: `/evraklar`, `/evraklar` düzenleme çekmecesi (kayıtlı kimlikli GB-Y1, "Ek bilgiler" açık, **Yabancı** seçili, kimlik alanına kaydırılmış), `/cariler`, `/cariler` yeni müşteri (17 yaş doğum tarihi yazılmış, uyarı görünür). SON ölçüm (`olcum-t2c`, son kodla), 4 görünüm × 8 kombinasyon = 32 ölçüm, modGercek = istenen 32/32:
| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K | K9 |
|---|---|---|---|---|---|---|---|---|---|
| /evraklar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /evraklar düzenle (yabancı) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 (¹) |
| /cariler (K12 tür süzgeci dahil) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /cariler yeni (17 yaş uyarı) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 (¹) |
Hücre = K1 + K3 taşan/kırık + K5 TRY + K7 + K12 + yatay kaydırma. Ek DOM kanıtı (`olcum-t2c/ek.jsonl`): PRO'da kimlik türü radyosu + kaldır kutusu VAR, maske `pasaport`; BASİT'te ProAlan gereği YOK; 17 yaş uyarı metni 8/8 görünür.
(¹) K9 = açık modal/çekmecenin altında kalan sayfa yazısı (tur 1 § 7.1 (¹) ile aynı ölçer sınırı).
**Ölçümün bulduğu gerçek kusurlar (düzeltildi):** (a) `olcum-t2`: PRO 1280'de kimlik türü radyo etiketleri çekmecenin iki sütununda iki satıra kırılıyordu (K3 kırık 2) → radyo tam satıra alındı, `olcum-t2b/t2c` K3 = 0; (b) `olcum-t2b` görüntüsünde Yabancı seçiliyken kutunun etiketi hâlâ "Borçlu TCKN / VKN" idi → "Borçlu pasaport no" (GBE06'ya eklendi), `olcum-t2c` yeniden ölçüldü.
**Read ile görülen görüntüler (4/4):** `olcum-t2/foto/evraklar-duzenle__PRO_aydinlik_1280.png` (çekmece üstü), `olcum-t2b/foto/evraklar-duzenle__PRO_aydinlik_1280.png` (radyo tam satır, kaldır kutusu, gizli kimlik ipucu), `olcum-t2b/foto/evraklar-duzenle__PRO_karanlik_375.png` (koyu 375 kart düzeni), `olcum-t2b/foto/cariler-yeni-17yas__BASIT_aydinlik_375.png` (08.10.2009 + "Kişi 18 yaşından küçük. Tarihi kontrol edin.", hata yok).

### 0.5 Tur 2 temizlik
API 5160 (PID + alt süreç) ve vekil 5161 durduruldu, `lsof` boş; ölçüm Chrome'ları kapandı. `zsh kiraci-temizle.sh 01M4EFPEDC9XZE6WR9PKAT5GW4 --uygula` + davet satırı silindi; kontrol: tenants satırı 0, cari 0, evrak 0, davet 0. **tenants 18 = başlangıç 18.** QA-TEST-* kiracılarına dokunulmadı.

### 0.6 Tur 2 değişen dosyalar
Core: `Domain/Girdi/TarihPenceresi.cs` (K8f KisiDogum + `ResitDegil`), `Domain/Girdi/MetinKurallari.cs` (`KimlikKurali.TcknVknVeyaPasaport`), `I18n/Sozlukler.GocB.cs` (YENİ, TR+EN), `Sozlesmeler/EvrakSozlesmeleri.cs` (`BorcluKimlikKayitli = false` varsayılanlı — geriye uyumlu).
Api: `Common/Dogrulama/GirdiOznitelikleri.cs` (+`TcknVknVeyaPasaportAttribute`, en küçük ekleme), `Modules/Evrak/EvrakModulu.cs`, `Modules/Cari/CariModulu.cs`, `Modules/Masak/MasakKapisi.cs`.
Web: `Pages/Evraklar.razor`, `Pages/Cariler.razor`, `Bilesenler/MasakFormu.razor`. Test: `GocBTestleri.cs`.
Gerekçe (alan dışı dokunuşlar): `Sozlesmeler/EvrakSozlesmeleri.cs` ve `GirdiOznitelikleri.cs` — KRİTİK veri kaybının ekranda "kayıtlı mı" bilgisi ve yabancı borçlu kuralının sunucu özniteliği için zorunlu, ikisi de geriye uyumlu ekleme. Tek dokunuş kaydı: Evraklar, Cariler, MasakFormu tur 1'de de açılmıştı — **ikinci dokunuş gerekçesi = doğrulayıcı KRİTİK/ORTA bulguları** (aynı paketin 2. turu).

### 0.7 DoD sözleşmesi (tur 2)
Yeni varlık yok. `kiymetli_evraklar.borclu_kimlik_no`: PATCH ile değiştirilebilir (dolu değer), silme yalnız AÇIK `borcluKimlikSil` (ekranda onay kutusu + uyarı metni, geri dönüş = kutuyu kaldırmak/Kaydet'e basmamak); null = korunur — "null ise sil" genel kuralından **gerekçeli istisna** (şifreli, istemciye dönmeyen alan) DTO belgesinde ve bu raporda yazılı. Finansal kayıtlarda yeni silme yolu yok.

## 1. Yapılanlar

### 1.1 Sunucu — "önce kapanacak açıklar" (E · GOC-B satırı), hepsi Core kuralı + GirdiSozlugu metni
| Açık | Önce | Şimdi | Dosya |
|---|---|---|---|
| **Doğum tarihi #2** (cari + MASAK) | gelecek / 0001 / 150 yaş geçiyordu | ⟶ tur 2: K8f `[TarihPenceresi(KisiDogum)]` (18 altı engel DEĞİL, uyarı; tur 1'deki K8b/18 yaş engeli geri alındı) (Cari POST/PATCH DTO + MasakBilgiDto) + servis yolu `CariServisi.DogumDenetle` (oluştur + PATCH) ve `MasakKapisi.KimlikBilgisiDenetle` | Cari/CariModulu.cs, Masak/MasakKapisi.cs |
| **MASAK uzunlukları #3** | ekran 60/200, sunucu 50/100 → tezgâhta 400 | ekran `EnFazla` = DTO `MaxLength` = `AlanSinirlari.MasakBelgeNo/MasakIslemAmaci/MasakAdSoyad/MasakAciklama/MasakFonKaynagi` (tek sabit); servis yolunda da uzunluk + K19 ad soyad ("1" geçiyordu) + K7 pasaport biçimi (yalnız belge türü PASAPORT). Biçim denetimi motorun "eksikler" iletisinden SONRA (eksikler tek seferde söylenir) | MasakKapisi.cs |
| **MASAK bildirim ileri tarihi** (yaptırım eşleşmesi) | ileri tarih kabul | PATCH'te `TarihPenceresi(Serbest, enGec: bugün)` → "Tarih bugünden ileri olamaz." alan `masakBildirimTarihi`; geri pencere bilinçli açık | Masak/YaptirimServisi.cs |
| **Evrak borçlu kimlik PATCH kaybı** | ekran alanı yazılıyordu, PATCH gövdesinde YOKTU | ⟶ tur 2 (tur 1 çözümü kayıtlı kimliği SİLİYORDU): yalnız-yazılır model — dokunulmadıysa gövdede YOK, silme açık `borcluKimlikSil`, sunucu null'u korur; kural `TcknVknVeyaPasaport` (§ 0) | Pages/Evraklar.razor, Evrak/EvrakModulu.cs |
| **Cari telefon / e-posta** | `[EmailAddress]` "a@b"yi geçiyordu | `[Eposta]` (K3, RFC + alan adında nokta), `MaxLength(AlanSinirlari.Eposta)`; telefon `[Telefon]` (GIRDI-2) canlıda doğrulandı | CariModulu.cs |
| **Keşide ≤ vade** | iki tarafta da yoktu; vade 9999 yılı geçiyordu | `EvrakServisi.TarihleriDenetle`: vade ≤ 10 yıl (K8c), keşide yıl penceresi (K8) + keşide ≤ vade → "Tarih {vade} tarihinden sonra olamaz."; PATCH'te kayıtlı + yeni değer BİRLİKTE sınanır | EvrakModulu.cs |
| Cari ad / ünvan | "12345", "A" geçiyordu; ekran 300 / sunucu 200 | `[Ad(CariAd=200)]`, ünvan `[Ad(CariUnvan=300)]` | CariModulu.cs |
| Cari tür süzgeci (C 87) · Evrak tür süzgeci (C 121) · vade aralığı ters (C 33) | sessizce BOŞ liste (UX-S28) | 400 + alan | CariModulu.cs, EvrakModulu.cs |
| Evrak tutarı (C 87) | eksi → "zorunlu alan"; 3. ondalık sessizce yuvarlanıyordu | `GirdiKapisi.Tutar` (K9) ⟶ tur 2: DTO'daki eski `Range(…, 999_999_999)` de `[Tutar]` oldu (§ 0 K2) | EvrakModulu.cs |
| Evrak borçlu adı (C 42) · evrak no kırpılmıyordu (C 122) | yalnız uzunluk | K19 `[Ad]` + servis; evrak no `Trim` | EvrakModulu.cs |
| Evrak takvimi (C 31/32) | yıl penceresi yok | K8e Rapor penceresi (2000..bugün+1) → 400 alan `bas`/`bit` | EvrakModulu.cs |
| Şüpheli bildirim tutarı (#10, C 161) | `Range(0,…)` 0 ve 10^15 geçiyordu | `[Tutar]` + servis `GirdiKapisi.Tutar` | Masak/MasakModulu.cs |
| Kapatma gerekçesi (C 325) | ekran 2000 / sunucu 1000; en az 20 KIRPILMADAN | `[Gerekce]` + servis `GirdiKapisi.Gerekce(…, 1000)` (kırpılmış) | MasakModulu.cs |
| Elle risk gerekçesi (C 324) | ekran 1000 / kolon 500 → 500 riski | `MaxLength(RiskElleGerekce=500)` + servis 400 | Masak/RiskServisi.cs |
| İş kodu (C 19) | uydurma iş kodu yazılabiliyordu | beyaz liste = kayıtlı `IZamanlanmisIs` kodları (DI) | Isler/IsAyarServisi.cs |
| `CariDuzeltmeDto` decimal (DTO_DECIMAL_RANGESIZ) | aralıksız | `[Range]` (değişmez kültürle — tr-TR'de "." binlik ayracı 500 doğuruyordu, bulundu ve düzeltildi); servis PATCH-5 yuvarlama sözleşmesi AYNEN | CariModulu.cs |

Sınır sabitleri: `AlanSinirlari` (Core/Domain/Girdi/GirdiHatasi.cs) += 25 GOC-B sabiti (CariAd, CariUnvan, CariNot, HesapKodu, UyrukKod, MasakAdSoyad, MasakAciklama, MasakFonKaynagi, EvrakNo, EvrakBanka, EvrakBorcluAd, EvrakBorcluKimlik, EvrakNot, SupheliAciklama, SupheliKapatma, GonderimReferansi, RiskElleGerekce, YaptirimEvrakNo, YaptirimNot, NakitSiniriEvrakNo, NakitSiniriGerekce, BildirimNotu …).

### 1.2 Ekran — 6 sayfa + 4 bileşen (Cariler, Uyum + Uyum/{YasalListePaneli, SaklamaBekletmePaneli}, Evraklar, EvrakTakvimi, VadeTakibi, Bildirimler, MasakFormu, NakitSiniriAsimlari)
- **Ham `<input>/<select>/<table>` = 0** (NakitSiniri dilekçe tablosu hariç — § 2.2). Sayısal `EnFazla` 0 (hepsi `AlanSinirlari`).
- **Anlamsal bileşenler:** cari ad/ünvan → `Unvan` (EnFazla kolon); vergi no ×2 → `TcknVeyaVkn`; doğum ×2 + MASAK doğum → `Tarih Tur=Dogum`; e-posta `EnFazla=254`; iskonto ×2 → `Yuzde`; kredi limiti → `Para Tur=SifirOlabilir`; adres ×2 → `CokSatir`; tahsilat/veresiye has → ortak `Has` (6 ondalık; Gram 3 ondalıktı); MASAK belge no → türe göre `TcKimlikNo` / `Pasaport` / `Metin` (id aynı), ad soyad → `AdSoyad`; evrak tutar → `Para Tur=Pozitif`, vade `Tarih Tur=Vade`, keşide `Tarih Tur=Keside EnGec=vade`, borçlu ad → `AdSoyad`, borçlu kimlik → `TcknVeyaVkn`, notlar → `CokSatir` (tam satır); şüpheli tutar → `Para Tur=Pozitif` (eski çift `#sb-tutar-hata` kaldırıldı); takvim tarihleri `Tur=Rapor` + aralık hatası bitişin altında (istek YOK); evrak vade süzgeci aralık hatası (istek YOK); Bildirimler iş ayarları → `GunSayisi` (boş kutu 0 olarak SESSİZCE kaydediliyordu → boş = alan altı aralık hatası).
- **Kaydet kilidi:** Evraklar'a `GecersizAlanKapisi` (CascadingValue) — ortak bileşen kural hatası `cekmece-evrak-kaydet` kilidine ve sebebine (alan altı metniyle AYNI) düşer; MasakFormu `Eksiksiz` ortak kuralın aynası (`KimlikKuralHatasi`).
- **Ortak ızgara (12 tablo → `Izgara`)**, her biri `grid:<ekran>:<grid-id>` tercihiyle sütun sırası, göster/gizle, sıralama, kullanıcıya+ekrana kalıcı, "Varsayılana dön": `uyum:uy-tespit` · `uyum:uy-supheli` · `uyum:uy-risk-tarihce` · `uyum:uy-yuksek-risk` · `uyum:ylp-eslesme` · `uyum:skb-bekletme` · `evraklar:evrak-liste` · `evrak-takvimi:evrak-takvim-gun` · `vade-takibi:vade-cari` · `vade-takibi:vade-kalem` (cari başına `KimlikOneki`) · `bildirimler:is-liste` · `bildirimler:is-calisma`. Mevcut id/sınıflar korundu (`#evrak-tablo` q10-yigin zo-yigin-dar data-zo, `#evrak-th-*`, satır/hücre id'leri). Sunucu sınırına değen listede sıralama kapalı + GOC-A'nın `SiralamaSinirNotu` (UX-S29: tespit 100, şüpheli 500, tarihçe 10, yüksek risk `ListeSiniri`, eşleşme 100, bekletme 100, koşum 50, evrak `dahaFazlasiVar`). Uyum şüpheli durumu ham kod yerine sözlük adı (UX-S31); takvim günü gg.aa.yyyy (makine biçimi yalnız bağlantıda); satır içi `style` kalktı.

### 1.3 Ortak bileşenlere en küçük geriye uyumlu eklemeler (gerekçeli)
| Dosya | Ekleme | Gerekçe |
|---|---|---|
| `Bilesenler/Grid/IzgaraKolonu.cs` + `Izgara.razor` | `BaslikId`, `BaslikIpucu` (null = eski davranış birebir) | evrak tablosunun başlık id sözleşmesi (`#evrak-th-tur/-veren/-vade`, kısa ad + tam ad title) korunmalıydı (EvrakEkranTestleri CJ_03b) |
| `Core/Domain/IzgaraTercihi.cs` | `BilinenEkranlar` += uyum, evraklar, evrak-takvimi, vade-takibi, bildirimler | beyaz liste fail-closed |
| `Core/Domain/Girdi/GirdiHatasi.cs` | `AlanSinirlari` GOC-B sabitleri | ekran = sunucu tek sabit |

## 2. Sapmalar ve gerekçeleri
1. **Keşide ≤ vade için yeni GirdiKodu açılmadı:** `TarihPenceresi.Denetle(keside, Keside, bugün, enGec: vade)` → mevcut `TarihGec` ("Tarih {vade} tarihinden sonra olamaz.") — ekran ve sunucu aynı anahtar; ortak sözlük dosyasına dokunulmadı. Sozlukler.GocB.cs GEREKMEDİ (sınır notu GOC-A anahtarı aynen kullanıldı).
2. **NakitSiniriAsimlari dilekçe tablosu (`#nsa-dilekce-islemler`) ızgaraya taşınmadı:** yazdırılan dilekçe (article.belge) — kâğıt düzeni, sütun tercihi anlamsız. `belge-tablo` istisnasına almak belge tavanını (4) gevşetirdi → ham sayılmaya devam eder (58'in içinde). Diğer 3 nakit listesi `<ul>` (tablo değil).
3. **Vade takibi ufuk kutusu `Sayi` kaldı:** `GunSayisi` kesirli değeri bileşen içinde null'a çevirip sayfaya "boş bırakıldı" dedirtiyordu (alan altı "tam sayı" ile çelişen iki metin; VadeEkranTestleri kırmızı). Sayi'nin EnAz/EnCok'u artık uygulanıyor. Sunucu üst sınırı (`ufukGun=100000`) **Rapor modülünde** — paket alanı dışı → § 11.
4. **K21 adres ≥ 10 karakter uygulanmadı** (TEYİTSİZ iş kuralı; GOC-A ile aynı karar). Adresler CokSatir'e taşındı, uzunluk tek sabit.
5. **Cari düzeltme / tahsilat / veresiye ondalık yuvarlaması (PATCH-5 sözleşmesi) değiştirilmedi** — ilk denemede düzeltme fişine "fazla ondalık 400" eklendi, PATCH-5'in açılış istisnası ve sınır testleriyle çeliştiği için geri alındı; GOC-A açık #6 açık kalır (§ 11).
6. **MASAK pasaport biçimi (K7) yalnız belge türü PASAPORT'ta** — sürücü belgesi/mavi kart/vergi levhası biçimi TEYİTSİZ, uygulanmadı.
7. ~~Doğum 18 yaş alt sınırı engel olarak uygulandı~~ ⟶ **tur 2: geri alındı** — TMK md.11 yasak koymaz; 18 altı yalnız uyarı (K8f, § 0 O1).
8. Yeni sözleşmeye güncellenen mevcut testler (gerekçe satırı her birinde, hiçbiri gevşetilmedi): `CariEkranTestleri.Duzenle_formu…` (adres textarea), `ZoBelgeReyonTestleri.ZB29` (veren hücresi kolon tanımında), `Toplu2Tur2…T2_Bildirimler` ve `OrtakGorunumKiyas…KY10` (iş/koşum hücre sınıfı kolon tanımında), `BildirimEkranTestleri.Her_dugme…` (ızgaranın "Sütunlar" düğmesi istek atmaz, paneli açar — etkisi ölçülür), `P05UcTestleri` ×2 (fikstür adı "A/B/C" → "Cari A/B/C": K19'a uymayan olumlu fikstür).

## 3. Migration
Gerekmedi.

## 4. Testler
### 4.1 Yeni — `Kuyumcu.Tests/GocBTestleri.cs` (21 yöntem)
- **GocBSafTestleri** GBS01 ızgara beyaz listesi · GBS02 ekran sınırı = DTO sınırı (10 alan) · GBS03 DTO'lar Core özniteliklerini taşır (13) · GBS04 MASAK kimlik kapısı (gelecek/17 yaş/1899 doğum, "1"/"12345" ad, pasaport "AB 12", amaç 101; tam sınır geçer) · GBS04b MASAK kapısı servis yolunda (DTO'suz) · GBS05 vade 10 yıl tam sınır/+1 gün, keşide = vade geçer / +1 red, 0001 yılı · GBS06 cari doğum kapısı + tür listesi · GBS07 MasakFormu kilidi.
- **GocBSunucuTestleri** (gerçek MySQL) GBH01 cari POST 3000/1899/17 yaş/"12345"/"A"/harfli tel/a@b/201 karakter → 400 + satır YOK, geçerli kayıt + SQL doğum · GBH02 PATCH gelecek doğum/a@b → 400 kayıt değişmez, tür=XYZ 400 · GBH03 evrak keşide>vade, vade+11y, 10,005, borçlu ad "1", 14 haneli kimlik → 400 satır YOK; geçerli + evrak no kırpılır · GBH04 PATCH borçlu kimlik SQL'e yazılır, keşide>vade 400, vade keşideden öne 400 (kayıt değişmez), liste tür/aralık/takvim 1999 400 · GBH05 satış MASAK "1"/3000/amaç 101 → 400 hareket YOK · GBH06 yaptırım ileri tarih / 3000 → 400 kayıt değişmez, bugün 200 · GBH07 şüpheli 0/−5/10,005 → 400, geçerli 201, boşluklu kapatma gerekçesi 400 (durum ACIK kalır), risk gerekçe 501 → 400 · GBH08 uydurma iş kodu 400 · GBH09 ızgara tercihi A kalıcı / B yok / bilinmeyen ekran 400.
- **GocBEkranTestleri** (bUnit) GBE01 evrak düzenle PATCH gövdesi `borcluKimlikNo` taşır + ızgara başlık id'leri + Sütunlar düğmesi · GBE02 keşide>vade alan altı sözlük metni + Kaydet kilitli, PATCH YOK · GBE03 cari "12345" alan altı AdHarf + 3000 doğum alan altı + Kaydet kilitli, POST YOK · GBE04 Uyum şüpheli ızgarası, ham "ACIK" yok.

### 4.2 Koşumlar
- GocB filtreli: **26/26 yeşil** (filtre `~GocB`).
- Mutasyon sonrası etkilenen geniş filtre (Evrak|GocB|ZoBelge): **360/360**.
- **Tam süit (son sunucu/test kodu, temiz derleme sonrası):** 11262 · **Geçti 11260 · Başarısız 0 · Atlandı 2** (`/private/tmp/kuyumcu-serit/trx-pGOC-B/tam.trx`). Tam süit sonrasında yalnız Evraklar.razor'da notlar kutusu tam satıra alındı (K7 ölçüm düzeltmesi) → Evrak|GocB|ZoBelge 360/360 yeniden yeşil.
- Önce/sonra: paket öncesi ilk geniş koşum 11211 test, 18 kırmızı (hepsi bu paketin değişikliğinden: Range kültür hatası 8, test sözleşmesi 8, Y4C_K12 GUID'de "805" kararsızlığı 1 — sonraki koşumda yeşil, P05 fikstür 2) → son: 0 kırmızı.
- Derleme: `dotnet build Kuyumcu.sln` → **0 Hata**; tek farklı uyarı `SUBEKAPSAMASinirTestleri.cs(49)` CS8601 (paket öncesinden, dokunulmadı).

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-B/` → TEK eşleşme → `~GocB` testleri → geri kopya → `filecmp` birebir; betik `GOC-B/betik/mut.py`, sonuç `GOC-B/mut-sonuc.jsonl`)
| Kod | Kural kapatıldı | Kırmızı test | cmp |
|---|---|---|---|
| M1-1 | Cari servis doğum K8b (Dogum→Serbest) | GBS06 | AYNI |
| M1-2 | Evrak keşide ≤ vade (`enGec: vade` silindi) | GBS05, GBH03, GBH04 | AYNI |
| M1-3 | MASAK bildirim ileri tarih yasağı | GBH06 | AYNI |
| M1-4 | MASAK kapısı servis yolu biçim denetimi | GBS04b | AYNI |
| M1-5 | Ekran: evrak PATCH gövdesinde borçlu kimlik | GBE01 | AYNI |
| M1-6 | Evrak liste tür beyaz listesi | GBH04 | AYNI |
| M1-7 | İş kodu beyaz listesi | GBH08 | AYNI |
| M1-8 | Izgara beyaz listesinden "uyum" | GBS01, GBH09 | AYNI |
| M1-9 | Kapatma gerekçesi DTO K30 özniteliği | GBS03 | AYNI |
| M1-10 | Ekran: keşide `EnGec=vade` | GBE02 | AYNI |
| M1-11 | Ekran: MasakFormu ad soyad kilidi | GBS07 | AYNI |
| M1-12 | Şüpheli tutar DTO `[Tutar]` | GBS03 | AYNI |
12/12 kırmızı, 12/12 cmp AYNI; son mutasyondan sonra temiz derleme (0 hata).

## 6. denetim.sh (son kod)
`DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, görev "P-30 hariç"). Bu paketin sayaçları (tavanlar indirildi, yalnız azalır):
| Sayaç | Önce | Sonra |
|---|---|---|
| GRID-1 HAM_TABLO_ORTAK_GRID_DISI | 70 | **58** |
| GIRDI-2 GIRDI_TUR_GENEL_BILESEN | 11 | **9** |
| GIRDI-2 DTO_DECIMAL_RANGESIZ | 20 | **17** (Cari/Masak/Evrak/Isler'de 0) |
| GIRDI-2 EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) | 129 | **86** (paket ekranlarında 0) |
| GIRDI-2 HAM_TARIH_GIRDISI | 6 | 6 (paket ekranlarında zaten 0) |
| P-18 MOBIL_TABLO_SARMALAYICISIZ | ✓ | ✓ (0 elle sarmalayıcı · 42 KartListe/ızgara) |
| P-36 HAM_INPUT / HAM_SELECT | 23 / 17 | 23 / 17 (paket ekranlarında 0) |

## 7. Canlı doğrulama (API 5160, Development, `kuyumcu_net`; test kiracısı `PAKETGOC-B-TEST …` = 01M4EBRD16547JQJGP27EESC0V; A=SAHİP, B=MÜDÜR; betik `GOC-B/canli_gocb.py`, sonuç `GOC-B/canli-sonuc.json`)
| Adım | Beklenen | Gerçek | İleti · alan |
|---|---|---|---|
| Cari vergi no 11111111111 | 400 | 400 | "TC kimlik numarası geçerli değil" · vergiNo |
| Cari doğum 3000 / 1899 / 17 yaş | 400 ×3 | 400 ×3 | DogumGelecek · DogumCokEski(120) · DogumResit(18) · dogumTarihi ⟶ tur 2: 17 yaş artık 201 (§ 0.3) |
| Cari harfli telefon 0532abc1234 | 400 | 400 | "Telefona yalnız rakam yazılır." · telefon |
| Cari e-posta a@b / ayse@ | 400 ×2 | 400 ×2 | "E-posta ad@alanadi.com biçiminde olmalı." · email |
| Cari ad "12345" / 201 karakter | 400 ×2 | 400 ×2 | "En az bir harf olmalı." · "En çok 200 karakter yazılır." · ad |
| SQL: 400'lerde cari yazılmadı | 0 satır | 0 | — |
| Cari geçerli (doğum 1985-04-12, tel, e-posta) | 201 | 201 | SQL: 1 cari, dogum_tarihi 1985-04-12, e-posta dolu |
| Cari PATCH doğum gelecek / x@y | 400 ×2 | 400 ×2 | dogumTarihi · email |
| Cari liste tür=XYZ | 400 | 400 | tur |
| Evrak keşide > vade | 400 | 400 | "Tarih 28.10.2026 tarihinden sonra olamaz." · kesideTarihi |
| Evrak vade +11 yıl | 400 | 400 | "Tarih 08.10.2036 tarihinden sonra olamaz." · vadeGunu |
| Evrak tutar 10,005 | 400 | 400 | "Virgülden sonra en çok 2 hane yazılır." · tutar |
| Evrak borçlu TCKN 11111111111 | 400 | 400 | borcluKimlikNo |
| Evrak geçerli (keşide = vade) | 201 | 201 | SQL: 1 evrak GB-5, keşide 2026-10-28 |
| Evrak PATCH borçlu kimlik no | 200 | 200 | SQL: borclu_kimlik_no DOLU (şifreli). ⟶ tur 2 notu: bu adım yalnız yazmayı sınıyordu; ekranın null gövdesi kimliği SİLİYORDU (doğrulayıcı KRİTİK) — § 0.3 |
| Evrak PATCH keşide > vade | 400 | 400 | kesideTarihi |
| Evrak liste ters vade aralığı / takvim 1999 | 400 ×2 | 400 ×2 | vadeBit · bas |
| MASAK ad soyad "1" / doğum 3000 / amaç 101 / belge no 51 | 400 ×4 | 400 ×4 | masak.adSoyad · masak.dogumTarihi · masak.islemAmaci · masak.belgeNo |
| SQL: MASAK 400'lerinde hareket yazılmadı | 0 | 0 | — |
| MASAK bildirim tarihi yarın | 400 | 400 | "Tarih bugünden ileri olamaz." · masakBildirimTarihi |
| MASAK bildirim bugün | 200 | 200 | SQL: BILDIRILDI · 2026-10-08 |
| Şüpheli tutar 0 / −5 | 400 ×2 | 400 ×2 | SayiSifir · SayiEksi · islemTutariTl |
| Şüpheli geçerli 1.500,25 | 201 | 201 | SQL: 1500.25 · ACIK |
| Kapatma boşluklu gerekçe | 400 | 400 | "Gerekçe en az 20 karakter olmalı (şu an 4)." |
| Elle risk gerekçe 501 | 400 | 400 | gerekce |
| İş ayarı uydurma kod | 400 | 400 | kapaliIsler |
| Izgara tercihi A yaz/oku · B yok · bilinmeyen ekran | 200/200/200/400 | aynı | SQL: `grid:uyum:uy-supheli` 110 karakter |
**UYUMSUZ 0 / 40.**

### 7.1 Gerçek başsız Chrome (vekil 5161, CDP 9701–9715; `sikisma-olc.js`; `GOC-B/olc.mjs`; ham `GOC-B/olcum/olcum.jsonl` 96 ölçüm + `olcum2/` 16 yeniden ölçüm, her ölçümün ekran görüntüsü)
12 görünüm (formlar açılarak: cari yeni modalı "Daha fazla" açık, evrak çekmecesi "Ek bilgiler" açık; geçmiş akordeonlar açık) × {1280×800, 375×812} × {BASİT, PRO} × {açık, koyu}; modGercek = istenen mod 96/96. Hücre = K1 + K3 taşan/kırık + K5 TRY + K7 + K12 + yatay; K9 ayrıca yazıldı.
| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K | K9 (binen) |
|---|---|---|---|---|---|---|---|---|---|
| /cariler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /cariler + yeni modalı | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2–6 (¹) |
| /uyum (tespit) | 1 (²) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0 |
| /uyum şüpheli | 1 (²) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0 |
| /uyum saklama | 1 (²) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0 |
| /uyum risk | 1 (²) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0 |
| /uyum nakit | 1 (²) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0 |
| /evraklar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /evraklar + yeni çekmecesi | 0 (³) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 (¹) |
| /evraklar/takvim | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /raporlar/vade-takibi | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /bildirimler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
K5 (TRY kalıbı) 0/112, yatay kaydırma 0/112, K1 0/112, K11 hata bandı 0/112.
(¹) K9: açık modal/çekmecenin ALTINDA kalan sayfa yazısı ("TELEFON" sütun başlığı ⟂ modal etiketi "Tür"; "Vade takvimi" ⟂ "Evrak no") — ölçerin örtü kontrolü yok (GOC-A açık #12 ile aynı); görüntüde örtüşme yok.
(²) K3 kırık: `#uy-sekme-supheli "Şüpheli İşlemler1"` — sekmenin sayı rozeti ayrı öğe olarak sayılıyor; görüntüde sekme tek satır ("Şüpheli İşlemler ①"). Sekmeler bileşeni bu paketin değil, değiştirilmedi.
(³) İlk ölçümde 1280 BASİT'te K7 = 1 (evrak "Ek bilgiler"de iki satırlık not kutusu yanındaki kutuyla 22 px kayık) → not kutusu tam satıra alındı, 16 yeniden ölçümde K7 = 0.
Ayrıca ölçer K8 "jargon" BASİT cari formunda "E-posta" etiketini işaretledi — etiket paket öncesinden (Dil.T.Auth.Eposta), değiştirilmedi.
**Görüntüler (Read ile görülen 3):** `olcum/foto/uyum-supheli__BASIT_aydinlik_1280.png` (A'nın kayıtlı düzeni: "1 sütun gizli", İŞLEM TUTARI önde ↓ azalan, durum "Açık" sözlükten), `olcum2/foto/evraklar-yeni__PRO_karanlik_375.png` (çekmece + 375 kart düzeni, Sütunlar düğmesi, ₺ biçimi), `olcum/foto/cariler-yeni__PRO_aydinlik_1280.png` (ortak bileşenli yeni cari formu).
**Izgara tercihi tarayıcıda (`izgara_ab.mjs`):** A → `/uyum?sekme=supheli` sütunlar `tutar, tespit, son-bildirim, durum, eylem` + "1 sütun gizli" + `tutar:descending`; B aynı rota → varsayılan `tespit, tutar, son-bildirim, durum, gerekce, eylem`, gizli yok, sıralama yok.

### 7.2 Temizlik
API (5160) ve vekil (5161) durduruldu, `lsof` boş; Chrome ölçüm süreçleri 0. `zsh kiraci-temizle.sh 01M4EBRD16547JQJGP27EESC0V --uygula` → kiracı silindi; kontrol: cariler/yaptırım/evrak/tercih/şüpheli/kullanıcı satırı 0; davet satırı (`GOCBDAVET…`) silindi (0). **tenants 18 = başlangıç 18.** QA-TEST-* kiracılarına dokunulmadı.

## 8. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Izgara düzeni GRID-1 sözleşmesiyle (`kullanici_tercihleri`: PATCH koru/null sil, DELETE "Varsayılana dön", denetim izi). Evrak düzeltme PATCH'i (gönderilmeyen = koru, null = sil) artık borçlu kimlik no'yu taşır. Finansal kayıtlarda yeni silme yolu açılmadı.

## 9. Değişen dosyalar (bu paket)
Sunucu: `Modules/Cari/CariModulu.cs` · `Modules/Masak/MasakKapisi.cs` · `Modules/Masak/MasakModulu.cs` · `Modules/Masak/RiskServisi.cs` · `Modules/Masak/YaptirimServisi.cs` · `Modules/Evrak/EvrakModulu.cs` · `Modules/Isler/IsAyarServisi.cs`.
Core: `Domain/Girdi/GirdiHatasi.cs` (AlanSinirlari) · `Domain/IzgaraTercihi.cs` (BilinenEkranlar).
Web: `Pages/{Cariler,Uyum,Evraklar,EvrakTakvimi,VadeTakibi,Bildirimler}.razor` · `Bilesenler/{MasakFormu,NakitSiniriAsimlari}.razor` · `Bilesenler/Uyum/{YasalListePaneli,SaklamaBekletmePaneli}.razor` · `Bilesenler/Grid/{Izgara.razor,IzgaraKolonu.cs}`.
Denetim: `dotnet/denetim.sh` (4 tavan indirildi).
Testler: `GocBTestleri.cs` (YENİ) + `CariEkranTestleri.cs`, `ZoBelgeReyonTestleri.cs`, `Toplu2Testleri.cs`, `OrtakGorunumKiyasTestleri.cs`, `BildirimEkranTestleri.cs`, `P05BenzersizlikTestleri.cs`.
Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Cariler, Uyum, Evraklar, EvrakTakvimi, VadeTakibi, Bildirimler (+ bileşenler MasakFormu, NakitSiniriAsimlari, YasalListePaneli, SaklamaBekletmePaneli). MasakFormu; Islemler/Banka/Hurda sayfalarında da kullanılır (yalnız bileşen değişti, o sayfalar açılmadı).

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K1 ham input/table kalmaz, sayaçlar azalır | KARŞILANDI (1 gerekçeli istisna) | paket ekranlarında input/select 0, table 0 (dilekçe hariç § 2.2); HAM_TABLO 70→58, GIRDI_TUR 11→9, DTO 20→17, UZUNLUK 129→86 |
| K2 her açık için sunucu 400 testi + ekran alan altı, aynı sözlük anahtarı | KARŞILANDI | GBH01–GBH09, GBE01–GBE03, GBS02–GBS07; canlı § 7 iletileri |
| K3 mutasyon ≥3 kural, cmp AYNI | KARŞILANDI | 12/12 kırmızı, cmp AYNI |
| K4 canlı: 11111111111 TCKN, 3000/1899 doğum, ileri MASAK bildirimi, keşide>vade, harfli telefon, bozuk e-posta, aşırı uzun MASAK alanı red; geçerli kayıt; ızgara A kalıcı B varsayılan | KARŞILANDI | § 7 40/40 + SQL; § 7.1 tarayıcı A/B |
| K5 başsız Chrome 1280+375 · BASİT+PRO · açık+koyu, Read ≤ 4 | KARŞILANDI | 96 + 16 ölçüm; 1 gerçek kusur (K7) bulundu/düzeltildi; kalan K3(²)/K9(¹) ölçer kalemi gerekçeli; Read 3 |
| K6 build 0/0, denetim TEMİZ (P-30 hariç), tam süit 0 yeni kırmızı | KARŞILANDI (uyarı notuyla) | 0 hata; tek uyarı paket öncesinden (SUBEKAPSAMA); denetim yalnız P-30; tam süit 11260/11262 geçti, 0 kırmızı |
| TCKN/YKN/VKN tek kaynak (KimlikNoDogrulayici) | KARŞILANDI | yeni kural yazılmadı; `KimlikKurali.TcknVeyaVkn` / ortak bileşenler |
| Ortak Form/Grid: yalnız kullan, eksik özellikte en küçük geriye uyumlu ekleme | KARŞILANDI | § 1.3 (BaslikId/BaslikIpucu, null = eski davranış) |

## 11. Açık kalanlar
0. **(tur 2)** Yalnız rakamdan oluşan yabancı pasaport/kimlik no (ör. 9 haneli) evrak borçlusunda TCKN/VKN kuralına düşer ve reddedilir (paket öncesinde de DTO 10 karakter altını reddediyordu); ayrı "kimlik türü" kolonu şema değişikliği ister — ürün kararı.
0b. **(tur 2)** `sikisma-olc.js` K3 sekme sayı rozeti (Uyum `#uy-sekme-supheli`) — sahibi orkestratör betikleri; K12 `#cari-tur-filtre` bu turda 8/8 kombinasyonda 0, yeniden üretilemedi.
0d. **(tur 2)** denetim.sh OLCEK-1 = 3 (tema-yildiz.css, YILDIZ şeridi) ve `Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` (0 bayt, 08.10 03:36 — bu paketin değil) ağaçta duruyor; dokunulmadı.
0c. **(tur 2)** Evrak borçlu kimlik araması (`borclu_kimlik_no_hash`) pasaportu büyük harfe normalleşmiş haliyle hashler; arama ucunun girdiyi aynı biçimde normalleştirip normalleştirmediği bu turda sınanmadı (küçük harfle aranan pasaport bulunamayabilir).
1. **Vade takibi sunucu üst sınırı** (`/raporlar/vade-takibi?ufukGun=100000` kabul) — uç `Modules/Rapor/RaporModulu.cs`'te, paket alanı dışı (sahibi: GOC-D · Rapor). Ekran 0–365'i uyguluyor.
2. **Cari düzeltme/tahsilat/veresiye fazla ondalık** hâlâ PATCH-5 sözleşmesiyle yuvarlanıyor (GOC-A açık #6) — sözleşme değişikliği PATCH-5 sınır testlerini ve açılış istisnasını etkilediği için ayrı karar gerekir.
3. **Uyum maskeleme onayı** (C satır 80, YALNIZ İSTEMCİ: sunucu onay istemiyor, rol yeterli) — dokunulmadı (DÜŞÜK; geri alınamaz işlemin sunucu onay bayrağı ayrı tasarım ister).
4. **Cari muhasebe hesap kodu biçimi** (C 88/90) ve **fiyat listesi id'sinin başka kiracıya ait olması** (C 89/91) — dokunulmadı (biçim kuralı TEYİTSİZ; liste kiracı kontrolü Fiyat modülünde).
5. K21 adres ≥ 10 (TEYİTSİZ) uygulanmadı.
6. sikisma-olc.js: K9 açık modal/çekmece altındaki sayfa yazısını, K3 sekme sayı rozetini kırık sayıyor (ölçer sahibine — orkestratör betikleri; GOC-A #12 ile aynı başlık).
7. Paket öncesinden kalan tek derleme uyarısı: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601 (dokunulmadı).

## 12. Kurucuya / mali müşavire
- **18 yaş altı müşteri (tur 2):** engel KALDIRILDI — TMK md.11 yalnız erginliği tanımlar, küçüğün müşteri/MASAK kimlik kaydını yasaklamaz; birincil kaynakta yasak bulunmadığı için mevzuat kuralı gereği paket öncesi davranış korundu. Ekran "Kişi 18 yaşından küçük. Tarihi kontrol edin." UYARISI verir. Küçükle işlemde yasal temsilci bilgisi/izni gerekip gerekmediği **DOĞRULANAMADI — kurucu/hukuk teyidi gerekir**; karar "engel" olursa tek satır: Cari/MASAK'ta `TarihTuru.KisiDogum` → `TarihTuru.Dogum`.
- **İleri tarihli çek (keşide)** — 5941 s. Çek Kanunu birincil metni bu pakette okunmadı → keşide ileri tarihe serbest (bugünkü davranış), yalnız "keşide ≤ vade" ve yıl penceresi uygulandı. **DOĞRULANAMADI — mali müşavir/hukuk teyidi gerekir.**
- **Evrak vadesi en çok 10 yıl** ve **MASAK bildirim tarihi geri penceresi açık** İŞ KURALIdır (envanter K8c/K8a); mevzuat hükmü değildir.
- **Pasaport biçimi** yalnız ICAO 9 karakter MRZ alanını aşmayan harf/rakam (5–20) — TR pasaport biçimi TEYİTSİZ.
