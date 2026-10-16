# PAKET P-23 — ÖDEME KALEMİ TABLOSU + VEZNE FİŞİNİN GENELLEŞTİRİLMESİ

**Tarih:** 2026-09-22 · **Dalga:** 6 · **Rol:** Taşkın (backend) + Ertuğ (frontend)
**Durum:** **YARIM** — Faz 1 (ileriye dönük yazım) BİTTİ ve kanıtlı; geri doldurma,
R-18 mahsup ve vezne fişi başlığının genelleştirilmesi **DEVREDİLDİ** (gerekçeleri
§ 9'da, hepsi planın kendi koşullarından doğuyor).

> **Anayasa md.1 beyanı:** bu raporda "oldu" denen her şeyin altında komut çıktısı
> vardır. Yapılmayan üç kalem § 9'da tek tek sayılmıştır; gizlenmemiştir.

---

## 0. ÖNCEKİ DOĞRULAMANIN KRİTİK BULGUSU — KAPANDI

Bir önceki turda doğrulayıcı şunu yazmıştı: *"PAKET P-23 HİÇ UYGULANMADI —
`grep -rln 'OdemeKalemi|odeme_kalemleri' dotnet/` → 0 dosya, `odeme_kalemleri`
tablosu = 0, `__EFMigrationsHistory` = 61, rapor dosyası yok."*

Bu tur o boşluk kapatıldı. Bugünkü ölçüm:

| Ölçüm | Önce | Şimdi |
|---|---|---|
| `OdemeKalemi`/`odeme_kalemleri` geçen dosya | 0 | 8 |
| `information_schema` → `odeme_kalemleri` | 0 | 1 |
| `__EFMigrationsHistory` | 61 | **62** |
| `PAKET-P-23-uygulama-raporu.md` | yok | bu dosya |
| denetim.sh P-23 sayaçları | yok | 2 sayaç, ikisi de 0 |

### 0.1 Kurucunun canlı sesi ("adet kısmında ne yazarsam altta da o görünüyor")
Doğrulayıcının notuna göre bir önceki tur bu paket yerine kurucunun bildirdiği
hatayı işlemişti. **O iş bu oturumda DEPODA BULUNDU ve kapanmış durumda** —
uydurma değil, kod ve testle:

- `dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` → `BicimBilgiEkliyor`
  ölçütü artık rakam dizisi değil **sayısal değer** karşılaştırması.
- `dotnet/Kuyumcu.Tests/P39TasarimKokleriTestleri.cs:446-490` → kurucunun
  gördüğü dört ölçümün birebir kendisi (`"1,0"→"1"`, `"15,20"→"15.2"`,
  `"2,50"→"2.5"`, `"007"→"7"`) + yazarken hiçbir ara adımda önizlemenin
  doğmadığını sınayan test. Tam süitte **yeşil**.

Bu turda o dosyalara **dokunulmadı**; yalnız doğrulandı ve raporlandı.

---

## 1. ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayımsız)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| **P-21** | VAR | `Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` (ciro/rücu + değişim akışı), `Kuyumcu.Tests/P21CiroRucuTestleri.cs` |
| **P-01** | VAR | `SatisServisi.FisOku` ucu çalışır durumda (`api/v1/islemler/fis/{fisId}`), canlı 200 döndü (§ 7) |

Kapsam daraltması önkoşul eksikliğinden DEĞİL, planın kendi BK-03 koşulundan
doğdu (§ 9.1).

---

## 2. SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI

Plan bloğu `PLAN-TEK-SEFER-2026-09-18.md:525-542`. Satır numaraları kaymıştı;
gerçek durum koddan okundu:

| Plan iddiası | Koddaki gerçek | Sonuç |
|---|---|---|
| `Data/VezneFisi.cs:14-17` — vezne deftere YAZMAZ | Doğru, dosyanın 14-18. satırlarındaki uyarı yerinde | KURAL KORUNDU (tek satır bile değişmedi) |
| `Modules/Vade/VadeKapisi.cs:172` | Vade kapısı fiş yolunda `VadeKapisi.Hazirla` ile çağrılıyor (`SatisModulu.cs:1392`) | dokunulmadı (kapsam dışı kaldı, § 9.2) |
| Ödeme kırılımı bugün nerede | ÜÇ yere dağılmış: `hareketler.odeme_turu` (karma fişte yalnız `"KARMA"` özeti), `nakit_tutar`/`veresiye_tutar` (yalnız iki araç), `kasa_hareketleri` (VERESIYE bacağının satırı YOK) | Doğrulandı; tablonun gerekçesi budur |
| `OdemeBacagiDto.Arac` listesi | `NAKIT\|KART\|HAVALE\|VERESIYE\|DOVIZ` (ALTIN bilerek yok) | `OdemeAraci.Tumu` bu listeden sapmasın diye **yansımayla** sınandı |

**Mevzuat işaretleri:** bu pakette hiçbir mevzuat hükmü koda SABİT YAZILMADI.
459 s. VUK tevsik haddi ve MASAK kümülatifi **eski kaynaklarından okumaya devam
ediyor** (§ 9.1); yeni tabloya taşınmadı.

---

## 3. YAPILANLAR

### 3.1 Veri modeli
`dotnet/Kuyumcu.Api/Data/OdemeKalemi.cs` (YENİ, 158 satır)
- `OdemeAraci` kapalı liste sınıfı (NAKIT · KART · HAVALE · VERESIYE · DOVIZ) +
  `KasayaGirer()` ölçütü.
- `OdemeKalemi : IKiraciVarlik` → `odeme_kalemleri`
  (`fis_id`, `hareket_id`, `sira`, `arac`, `tutar_tl`, `para_birimi`,
  `doviz_tutar`, `kasa_id`, `kasa_hareket_id`, `pos_tahsilat_id`,
  `taksit_adedi`, `evrak_id`, `kur_snapshot_id`, `bilinmiyor`, `aktif`,
  `storno_hareket_id`, `olusturuldu`).
- `ISubeVarlik` DEĞİL (şube zaten hareket satırında). Global kiracı süzgeci
  `OnModelCreating`'de otomatik kuruluyor; kapsam denetimi (`KuyumcuDbContext`
  IKiraciVarlik kapısı) uygulamayı açtı → kanıt: 3774 test + canlı koşum.
- **Y-24 hazırlığı:** `taksit_adedi` ve `pos_tahsilat_id` şimdiden kolon; taksit
  planı tablosu şema değişikliği olmadan bağlanabilir (plan DoD maddesi).

`dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `DbSet<OdemeKalemi> OdemeKalemleri` + üç indeks:
  `(tenant, fis_id)`, `(tenant, hareket_id)`, `(tenant, arac, olusturuldu)`.

### 3.2 Yazma yolu
`dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- Fiş yazımındaki bacak döngüsü **TÜM** bacakları geziyor (VERESIYE dâhil);
  kasa/POS işleri eskisi gibi yalnız `OdemeAraci.KasayaGirer(...)` olanlarda.
- `PosDefterineYaz` dönüşü `Task` → `Task<string?>` oldu; yazılan POS tahsilatı
  kalemin `pos_tahsilat_id` alanına konabiliyor. Altı çağrı noktası dönüşü
  kullanmıyor, davranış değişmedi.
- `FisOku` yanıtı **genişledi** (şekli korundu): `odemeKalemleri[]` +
  `odemeKalemToplam`. `bacaklar` ve `veresiyeToplam` aynen duruyor.

**Defter değişmezi korundu:** kalem yazmak hareket yazmak DEĞİLDİR.
`HareketFabrikasi.Ekle` tek yazma yolu olmaya devam ediyor; `Hareketler.Add`
kullanılmadı. Vezne hâlâ deftere yazmıyor.

### 3.3 Storno
`dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`
- Storno, kaynağın **aktif** ödeme kalemlerini bulur, `aktif=false` yapar ve
  `storno_hareket_id`'ye ters satırın kimliğini yazar. **Hard DELETE yok**
  (finansal varlık, ADR-0004).
- ⚠ Gerçek sınır: `StornolanabilirTurler` bugün `ISCILIK · VERESIYE · TAHSILAT ·
  EMANET_GIRIS · EMANET_CIKIS` (`StornoServisi.cs:71-72`) — **SATIS listede
  DEĞİL**, yani satış fişi bugün stornolanamıyor. Kalem davranışı bu yüzden
  stornolanabilir bir tür üzerinden sınandı; SATIS listeye eklendiği gün
  davranış zaten yerinde olacak.

### 3.4 Düzeltme politikası damgası
`dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs`
- `T("OdemeKalemi", GerekceAnahtarlari.Defter)` → **STORNO** politikası.
  Bu damga olmadan `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR`
  kırmızıydı (bkz. § 6: kapı gerçekten çalıştı, uydurulmadı).

---

## 4. MIGRATION

| Ölçü | Değer |
|---|---|
| Ad | `20260921224711_P23OdemeKalemleri` |
| `.cs` | **4.217 bayt** (boş değil; `Up()` tabloyu + 3 indeksi kuruyor) |
| `.Designer.cs` | **213.723 bayt** |
| Snapshot | `KuyumcuDbContextModelSnapshot.cs` içinde `odeme_kalemleri` var |
| `--no-build` | KULLANILMADI (build yeşilken üretildi: `Build succeeded.`) |
| `kuyumcu_net`'e uygulama | `Applying migration '20260921224711_P23OdemeKalemleri'. Done.` |
| `__EFMigrationsHistory` | 61 → **62** |

`Down()` **tabloyu düşürür, eski alanlara dokunmaz** — plan şartı birebir.

### 4.1 Geri dönüş provası (plan DoD ii + iii) — GERÇEKTEN KOŞTURULDU

```
1) mysqldump --single-transaction kuyumcu_net  → 40.353.612 bayt (yedek-once.sql)
   Başlangıç: __EFMigrationsHistory=62 · odeme_kalemleri=0
2) dotnet ef database update 20260921084754_P37KullaniciTercihi
   → "Reverting migration '20260921224711_P23OdemeKalemleri'. Done."
   Sonrası: __EFMigrationsHistory=61 · odeme_kalemleri tablosu=0 (düştü)
            hareketler=172 · kasa_hareketleri=95  ← DEĞİŞMEDİ
3) dotnet ef database update
   → "Applying migration '20260921224711_P23OdemeKalemleri'. Done."
   Sonrası: __EFMigrationsHistory=62 · odeme_kalemleri=0 · hareketler=172 · tenants=9
```

Yani: **yedek alındı, Down() koşturuldu, geri dönüldü ve hiçbir eski satır
kaybolmadı.** (Bu prova BOŞ tablo üzerinde yapıldı; geri doldurma migration'ı
için gereken prova AYRI ve henüz koşmadı — § 9.1.)

---

## 5. SALT OKUNUR ETKİ ÖLÇÜMÜ (plan DoD i)

`kuyumcu_net`, 2026-09-22, geri doldurmadan ÖNCE:

| Ölçüm | Değer |
|---|---|
| `hareketler` toplam | 172 |
| `odeme_turu` NULL | **85** |
| `odeme_turu = 'KARMA'` | **6** |
| `fis_id` dolu satır | 4 |
| farklı `fis_id` | 3 |
| `nakit_tutar` NULL | 39 |
| `veresiye_tutar` NULL | 39 |
| `kasa_hareketleri` | 95 |
| `banka_hareketleri` | 24 |

Araç kırılımı: `NULL 85 · VERESIYE 26 · HAVALE 24 · NAKIT 18 · KART 13 · KARMA 6`.

**Geri doldurmanın gerçek yükü:** 6 KARMA satır + 85 `odeme_turu` NULL satır.
Bunların **en az 85'i "BİLİNMİYOR" işaretlenmek zorunda** — alan eklenmeden önce
yazıldıkları için payı geri kazanılamaz. Tahmin edilmeyecek (md.1). Bu sayı
raporda açıktır ve geri doldurma fazının kabul ölçütüdür.

---

## 6. TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| P-23 filtreli (ilk) | 18 | 17 | 1 | 0 |
| P-23 filtreli (düzeltme sonrası) | 18 | **18** | 0 | 0 |
| **Tam süit (ara)** | 3774 | 3773 | **1** | 0 |
| **Tam süit (son)** | **3774** | **3774** | **0** | 0 |

**Yeni test sayısı: 12** (`Kuyumcu.Tests/P23OdemeKalemiTestleri.cs`).
Süit **3762 → 3774**.

### 6.1 Yolda kırılan iki test — gizlenmedi
1. `P23OdemeKalemiTestleri.FisOku_kalemleri_ve_KALEM_TOPLAMINI_dondurur`
   → `KeyNotFoundException`. Sebep: test varsayılan serileştiriciyle Pascal alan
   adı arıyordu, API camelCase döner. **Test düzeltildi**
   (`JsonSerializerDefaults.Web`), ürün kodu değiştirilmedi.
2. `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR`
   → *"Politika haritasında damgası olmayan varlık(lar): OdemeKalemi"*.
   Bu, UX-1'in çekirdek kapısının **doğru çalıştığının kanıtıdır**: yeni varlık
   damgasız kalamaz. Harita `T("OdemeKalemi", Defter)` ile güncellendi.

---

## 7. MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P23/` altına kopyalandı → `perl -0777 -pi -e`
ile tek eşleşmeli değişiklik → filtreli test → yedek geri kopyalandı → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| **M23-1** | Kalem döngüsüne `if (!OdemeAraci.KasayaGirer(b.Arac)) continue;` — VERESIYE kalemi yazılmaz | `Her_bacak_VERESIYE_dahil_KALEM_yazar` · `Kalem_toplami_fis_toplamina_ESITTIR` · `VERESIYE_kaleminde_kasa_bagi_YOKTUR_digerlerinde_VARDIR` · `FisOku_kalemleri_ve_KALEM_TOPLAMINI_dondurur` (4 kırmızı / 10) | `cmp` **BİREBİR** |
| **M23-2** | `kalem.Aktif = false` → `true` (storno kalemi pasife almaz) | `Storno_kalemi_PASIFE_ALIR_SILMEZ` (1 kırmızı / 10) | `cmp` **BİREBİR** |
| **M23-3** | `KasayaGirer(arac) => arac != Veresiye` → `=> true` | `VERESIYE_kasaya_GIRMEZ_digerleri_girer` · `Her_bacak_VERESIYE_dahil_KALEM_yazar` · `Kalem_toplami_fis_toplamina_ESITTIR` · `ESKI_alanlar_AYNEN_yazilir_tevsik_kaynagi_DEGISMEDI` · `VERESIYE_kaleminde_kasa_bagi_YOKTUR...` · `FisOku_kalemleri...` (6 kırmızı / 18) | `cmp` **BİREBİR** |

Hiçbir mutasyon sessiz geçmedi → test açığı YOK.
Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.
Proje ağacına `.mutorj`/`.bak`/`.mutbak` **bırakılmadı** (yöntem `cp` tabanlı).

---

## 8. DENETİM + SAYAÇLAR

`dotnet/denetim.sh` → **DENETİM TEMİZ.** (exit 0)

Bu pakette eklenen iki CIRCIR sayacı (tabanlar **TAZE ÖLÇÜLDÜ**, belgeden
kopyalanmadı; ölçüm komutları betiğe yorum olarak yazıldı):

| Sayaç | Değer | Ne ölçüyor |
|---|---|---|
| `HAREKETLERDE_ODEME_YONTEMI_YENI_YOL` | **0** | `OdemeKalemleri.Add` yalnız `SatisModulu.cs` içinde. İkinci bir modül kendi kırılımını yazmaya başlarsa kapı kırılır. |
| `FIS_TOPLAM_ODEME_KALEMI_FARKI` | **0** | Sepet döngüsünün DIŞINDA yazılan kalem sayısı. Kalem yalnız `OdemeSepeti.Coz`'dan geçmiş bacaktan doğduğu sürece fark yapısal olarak 0'dır (sepet toplamı fiş toplamına zaten TAM eşitlenir). |

**Tavan/eşik artırılmadı.** Tek istisna, tavan değil **envanter** olan kapı:
`BEKLENEN_VARLIK` 63 → **64** (`-eq` kapısı; betiğin kendi yordamı "varlık
eklenince sayı elle güncellenir" diyor). Taze ölçüm: `Entities.cs` 25 +
`Data/` diğerleri 39 = 64. `ham/olcum-2026-09-18.json` de aynı değere
güncellendi (yoksa `OLCUM_TABANI_KOPYA` kapısı kırılıyordu — kırıldı, düzeltildi,
§ 6 ruhuyla raporlandı).

---

## 9. SAPMALAR VE DEVREDİLENLER (gizlenen yok)

### 9.1 Geri doldurma migration'ı — **DEVREDİLDİ: P-23 Faz 2**
Plan § 11 **BK-03** ve paket DoD'u geri doldurmayı üç şarta bağlıyor. Bugün
(i) salt okunur etki raporu ✅ (§ 5) ve (ii) koşturulmuş `Down()` tatbikatı ✅
(§ 4.1) **hazır**; (iii) "öncesi/sonrası tam DB yedeği + geri dönüş provası" ise
yalnız **boş tablo** üzerinde yapılabildi. Veri yazan bir geri doldurmanın
provası, **veri içeren** bir anlık görüntü üzerinde ayrıca koşmalıdır.
Koşulmadan çalıştırmak plana açıkça aykırı olurdu; çalıştırılmadı.
**Sonuç:** geçmiş 172 hareketin hiçbiri değişmedi; `odeme_kalemleri` = 0.

### 9.2 R-18 mahsup — **GİRMEDİ (planın izin verdiği seçenek)**
Plan: *"ya tüm ödeme yollarını kapsar ya hiç girmez — kısmi mahsup bakiyeyi
saptırır, mimari veto."* Mahsubun tüm yolları (satış fişi, işçilik, tahsilat,
takas, emanet, vezne, P21 değişimi) tek seferde kapsaması bu fazın kapsamını
aşıyordu. **Kısmi uygulanmadı → veto tetiklenmedi.** R-18 AÇIK.

### 9.3 Vezne fişi başlığının genelleştirilmesi — **DEVREDİLDİ**
Vezne kuralı (`VezneFisi.cs:14-18` — vezne deftere YAZMAZ) korunmak zorundaydı
ve korundu; ama başlığın genelleştirilmesi vezne fişinin ödeme kalemlerini de
taşıması demektir ve bu, geri doldurma yapılmadan yarım bir model üretir
(vezne fişinin kalemi var, geçmiş satış fişinin yok). Faz 2 ile birlikte
yapılmalıdır.

### 9.4 Ekran (Katman 8) — **BU FAZDA EKRAN DEĞİŞİKLİĞİ YOK**
Hiçbir `.razor` dosyası açılmadı → **Dokunulan sayfalar: YOK** (DoD EK 2 "tek
dokunuş" kaydı temiz kalıyor). Ödeme kırılımı sunucudan `FisOku` ile dönüyor;
ekranda gösterimi Faz 2'ye bırakıldı. `Sozlukler.cs`'e anahtar eklenmedi çünkü
ekrana basılan yeni bir metin doğmadı.
⚠ Bu, UX-S25 anlamında "sunucuda uç var, ekranda düğme yok" durumu **değildir**:
kalem kullanıcı tarafından oluşturulan bir kayıt değil, fişin türevidir ve
düzeltme yolu `DuzeltmePolitikasi`'nde **STORNO** olarak damgalanmıştır.

### 9.5 `pos_tahsilat_id` canlıda NULL kaldı
Test kiracısında POS cihazı tanımlı değildi ve `Pos.CihazZorunlu` kapalı;
`PosDefterineYaz` bu durumda bugünkü davranışı koruyup `null` dönüyor. Kod yolu
doğru (cihaz varsa kimlik yazılır), canlı örnekte cihaz yoktu.

### 9.6 Tahmin aşımı
Plan 600 ajan-dk öngörmüştü. Alt-adım kırılımı (Mustafa K-F7):
ölçüm ~25 dk · modelleme+migration ~70 dk · yazma yolu + storno + FisOku ~85 dk ·
test yazımı ~70 dk · mutasyon ~40 dk · geri dönüş provası ~20 dk ·
canlı doğrulama ~55 dk · denetim sayaçları ~35 dk · rapor ~30 dk.
Faz 1 bütçe içinde kaldı; **kalan bütçe Faz 2'ye (geri doldurma + R-18 + vezne)
devredildi.**

---

## 10. CANLI DOĞRULAMA (yalnız 5198 — 5199'a DOKUNULMADI)

Test kiracısı: **`PAKETP-23-TEST`** (`01M3334HNZTYMVG6V5BW09GZ5B`).
⚠ `QA-TEST-*` fikstürlerine dokunulmadı.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` davetsiz | 403 | **403** | *"Geçerli bir davet jetonu gerekiyor"* — kapı fail-closed çalışıyor |
| 2 | `POST /sistem/davetler` (anahtarsız kurulum) | 503 | **503** | *"Sistem:Anahtari yapılandırılmamış"* → API **oturum kapsamlı** `Sistem__Anahtari` ortam değişkeniyle yeniden başlatıldı (dosya DEĞİŞTİRİLMEDİ, değer çıktıya yazılmadı) |
| 3 | `POST /sistem/davetler` gerekçesiz | 400 | **400** | *"Gerekçe zorunludur"* |
| 4 | `POST /sistem/davetler` gerekçeli | 201 | **201** | jeton maskelendi |
| 5 | `POST /auth/kayit` davetli | 201 | **201** | tenant `01M3334HNZ…` |
| 6 | kasa/ürün/stok/cari kurulumu | 200 | **200** | stok 10 adet |
| 7 | `POST /islemler/satis-fisi` (NAKIT 4.000 + KART 3.000 + VERESIYE 3.000 = 10.000) | 200 | **200** | `fisId=01M3335CHT9SC2VYYNDK78W6JF` |
| 8 | SQL: kalem satırları | 3 satır, VERESIYE dâhil | **3** | tablo aşağıda |
| 9 | SQL: fiş toplamı = kalem toplamı | eşit | **10000.00 = 10000.00** | aşağıda |
| 10 | `GET /islemler/fis/{id}` | 200 + `odemeKalemleri` | **200** | `odemeKalemToplam=10000.00`, 3 kalem, `bacaklar` (eski yol) hâlâ 2 satır, `veresiyeToplam=3000` |
| 11 | Temizlik | tenants 9'a döner | **9** | aşağıda |

```
+------+----------+----------+-------------+------+----+-----+--------------+------------+-------+
| sira | arac     | tutar_tl | para_birimi | kasa | kh | pos | taksit_adedi | bilinmiyor | aktif |
+------+----------+----------+-------------+------+----+-----+--------------+------------+-------+
|    0 | NAKIT    |  4000.00 | TRY         |    1 |  1 |   0 |         NULL |          0 |     1 |
|    1 | KART     |  3000.00 | TRY         |    1 |  1 |   0 |            1 |          0 |     1 |
|    2 | VERESIYE |  3000.00 | TRY         |    0 |  0 |   0 |         NULL |          0 |     1 |
+------+----------+----------+-------------+------+----+-----+--------------+------------+-------+
+------------+--------------+
| fis_toplam | kalem_toplam |
+------------+--------------+
|   10000.00 |     10000.00 |
+------------+--------------+
```

**Temizlik kanıtı** (test kiracısı `tenant_id` taşıyan TÜM tablolardan +
`kiraci_davetleri` + `tenants`'tan silindi):

```
tenants              = 9    (başlangıçtaki değer)
odeme_kalemleri      = 0
hareketler           = 172  (dokunulmadı)
__EFMigrationsHistory= 62
lsof -nP -iTCP:5198 -sTCP:LISTEN → BOŞ (API durduruldu)
```

---

## 11. § KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt / gerekçe |
|---|---|---|
| **R-17** — Ödeme kalemi tablosu | **KISMEN** | Tablo VAR ve ileriye dönük DOLUYOR: `Data/OdemeKalemi.cs:76` · migration `20260921224711_P23OdemeKalemleri` (4.217 bayt, uygulandı, 61→62) · `SatisModulu.cs:1516` kalem yazımı · 12 test (`P23OdemeKalemiTestleri`) · canlı 3 kalem/10.000 TL (§ 10 adım 8-9) · sayaç `FIS_TOPLAM_ODEME_KALEMI_FARKI=0`. **Eksik:** geçmişin geri doldurulması → **devredildi: P-23 Faz 2** (§ 9.1). Vezne fişi başlığının genelleştirilmesi → **devredildi: P-23 Faz 2** (§ 9.3). |
| **R-18** — Mahsup | **AÇIK (girmedi)** | Plan "ya tümü ya hiç" diyor; kısmi uygulanmadı, mimari veto tetiklenmedi → **devredildi: P-23 Faz 2** (§ 9.2). |

---

## 12. § DoD SÖZLEŞMESİ

| Varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| `OdemeKalemi` (`odeme_kalemleri`) | **SALT OKUNUR — düzeltme yolu STORNO** | Kalem, doğrulanmış sepetten TÜRETİLİR; elle düzenlenseydi kalem toplamı fiş toplamından ayrılır ve bakiye saptırılırdı. Finansal varlık → **hard DELETE YOK**. Storno kalemi `aktif=false` yapar ve `storno_hareket_id` yazar. Damga: `DuzeltmePolitikasi` → `T("OdemeKalemi", GerekceAnahtarlari.Defter)`; `DuzeltmePolitikasiTestleri` bunu zorunlu kılıyor. |
| `Hareket`, `KasaHareket`, `BankaHareket` | değişmedi | — |

Yeni **uç** açılmadı (`FisOku` yanıtı genişledi, yeni uç doğmadı) → yeni
`[ModulYetkisi]`/`[RolGerekli]` gerekmedi; `FisOku` zaten
`[Yetki.ModulYetkisi("islem")]` altındaki controller'da. Yeni **denetim kaydı**
gerekmedi: kalem kendi başına yazılabilen bir kayıt değildir; onu doğuran fiş
ve onu hükümsüz kılan storno **zaten** `DenetimServisi.YazmaKaydet` çağırır
(`StornoServisi` → `denetim.YazmaKaydet(islem, "Storno", …)`).

---

## 13. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Her iş kuralı için hatayı yeniden üreten test | **KARŞILANDI** | 12 yeni test |
| Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | **KARŞILANDI** | § 7, üçü de BİREBİR |
| 5198'de canlı doğrulama (HTTP + SQL) | **KARŞILANDI** | § 10, 11 adım |
| 5199'a dokunulmadı | **KARŞILANDI** | yalnız 5198 kullanıldı, sonra kapatıldı |
| `dotnet/denetim.sh` TEMİZ | **KARŞILANDI** | exit 0 |
| Tam süit 0 başarısız | **KARŞILANDI** | 3774/3774 |
| Test kiracısı silindi, `QA-TEST-*` korundu | **KARŞILANDI** | tenants 9 |
| Migration `--no-build` KULLANILMADI | **KARŞILANDI** | `Build succeeded.` |
| Sayaçlar denetim.sh'a eklendi, taban taze ölçüldü | **KARŞILANDI** | § 8 |
| Tavan artırılmadı | **KARŞILANDI** | tek değişen `-eq` envanter kapısı (63→64), gerekçeli |
| COMMIT YOK / `git` yazma yok / `rm`-`mv` yok | **KARŞILANDI** | çalışma ağacında kalındı |
| Sır/parola çıktıya yazılmadı | **KARŞILANDI** | tüm sırlar değişkende; jeton maskelendi |
| Defter değişmezleri (tek yazma yolu, storno motoru, kiracı izolasyonu) | **KARŞILANDI** | `Hareketler.Add` yok; kiracı izolasyon testi yeşil |
| Vezne deftere YAZMAZ kuralı | **KARŞILANDI** | `VezneFisi.cs` değiştirilmedi |
| Pages'te yeni istemci record | **YOK** | ekran dosyası açılmadı |
| DoD sözleşmesi bölümü | **VAR** | § 12 |
| Kimlik kapanış tablosu | **VAR** | § 11 |
| R-18 kısmi uygulama YASAĞI | **KARŞILANDI** | hiç girilmedi |
| Geri doldurma BK-03 şartsız koşmadı | **KARŞILANDI** | koşturulmadı (§ 9.1) |
| **Paketin TAMAMI** | **KARŞILANMADI** | § 9.1-9.3: üç kalem devredildi → durum **YARIM** |

---

## 14. DEĞİŞEN DOSYALAR

**Yeni:**
- `dotnet/Kuyumcu.Api/Data/OdemeKalemi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260921224711_P23OdemeKalemleri.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260921224711_P23OdemeKalemleri.Designer.cs`
- `dotnet/Kuyumcu.Tests/P23OdemeKalemiTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-23-uygulama-raporu.md` (bu dosya)

**Değişen:**
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs`
- `dotnet/denetim.sh`
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json`

`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve
`SONRAKI-OTURUM-PROMPT.md` dosyalarına **dokunulmadı**.

---

## 15. AÇIK KALANLAR

1. **P-23 Faz 2 — geri doldurma** (BK-03 (iii): veri içeren anlık görüntü
   üzerinde geri dönüş provası). 6 KARMA + 85 `odeme_turu` NULL satır;
   en az 85'i **"BİLİNMİYOR"** işaretlenecek.
2. **R-18 mahsup** — tüm ödeme yollarını kapsayan tek seferlik uygulama.
3. **Vezne fişi başlığının genelleştirilmesi** (vezne deftere yazmama kuralı
   korunarak).
4. **Ekran** — `FisOku`'nun döndüğü `odemeKalemleri` henüz hiçbir sayfada
   gösterilmiyor.
5. **SATIS stornolanabilir değil** — `StornoServisi:71-72`. Fiş düzeyinde
   storno açılana kadar satış fişinin kalemleri pasifleşemez. Ayrı karar konusu.
6. **Çok kasalı fiş stornosu** desteklenmiyor (mevcut sınır, bu paket
   değiştirmedi).

---

## 16. KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **Hiçbir mevzuat hükmü koda sabitlenmedi.** 459 s. VUK tevsik haddi ve MASAK
   kümülatifi **eski kaynaklarından** (hareket satırının `nakit_tutar`'ı)
   okumaya devam ediyor. Kaynak yeni tabloya **taşınmadı** — taşınsaydı
   geçmişi olmayan tablo kümülatifi sıfırlar ve had **sessizce delinirdi**.
   Kaynak değişimi ancak geri doldurmadan sonra, gerileme testiyle yapılır.
2. **Geri doldurmada en az 85 satır "BİLİNMİYOR" kalacak.** Bu satırların nakit
   payı geriye dönük ölçülemiyor. Mali müşavire sorulacak: bu satırlar tevsik
   kümülatifinde **sıfır** mı sayılsın, yoksa "belirsiz" olarak rapora ayrı mı
   düşsün? Ekip bunu **tahmin etmeyecek**.
3. **Sistem anahtarı bu kurulumda tanımsız** (`appsettings.json → Sistem:Anahtari`
   4 karakterlik yer tutucu). Sistem uçları kapalı; canlı doğrulama için
   anahtar yalnız bu oturumun ortam değişkeninde üretildi, hiçbir dosyaya
   yazılmadı. **K-5 sır rotasyonunun kapanmamış ayağı.**
4. **Runbook borcu (tekrar doğrulandı):** `docker-compose.yml` içindeki
   `MYSQL_ROOT_PASSWORD` çalışan konteynere **uymuyor**
   (`ERROR 1045 … Access denied`). Çalışan kimlik
   `appsettings.Development.json` bağlantı dizesindedir. `RUNBOOKS.md` ve
   `docker-compose.yml` güncellenmeli; yoksa her yeni ajan bu duvara çarpıyor.
   (Sır DEĞERİ hiçbir çıktıya yazılmadı.)
5. **Kurucu silmeli (hook `rm`'i engelliyor):** proje ağacında 17 adet 0 baytlık
   `*.mutorj`/`*.mutbak` artığı ve `Data/Migrations` altında 8 adet 0 baytlık
   terk edilmiş migration iskeleti (`20260915131748/132123/132303/132645_
   FiyatListesiVeKampanya{,.Designer}.cs`) duruyor. Derlemeyi ve denetimi
   etkilemiyor; `dotnet ef` geçmişini ileride yanıltabilir.
6. **Yedek dosyası** `scratchpad/yedek-once.sql` (40,4 MB) oturum dizininde
   duruyor; Faz 2'nin provası için saklanabilir.
