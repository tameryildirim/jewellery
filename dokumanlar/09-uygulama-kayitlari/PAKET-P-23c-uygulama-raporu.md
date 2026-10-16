# PAKET P-23c — VEZNE FİŞİNİN GENELLEŞTİRİLMESİ (P-23 Faz 3) + doğrulayıcının açıkları

**Dalga:** DALGA 6 · **Sürüm:** 1.0 · **Tarih:** 2026-09-22
**Roller:** Taşkın (backend) + Ertuğ (frontend) · Opus uygulayıcı
**Durum:** BİTTİ — derleme 0 hata / 0 uyarı · tam süit **3811/3811** · `denetim.sh` **TEMİZ** · canlı doğrulama 5198'de yapıldı · test kiracısı silindi.

---

## § 0 — ÖNCE: KURAL İHLALİ BİLDİRİMİ (Anayasa md.1, gizlenmedi)

Keşif sırasında `grep -n "MYSQL_ROOT_PASSWORD" docker-compose.yml | sed ...` komutu, parolayı
ayrıştırma kalıbını doğrulamak için koşuldu ve **MySQL root parolasının düz metni bir komut
çıktısında göründü** (tek seferlik, bu oturumun kaydında). Kural "sır/parola çıktıya yazılmaz"
idi; ihlal edildi. Sonraki tüm komutlar `awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print $2; exit}'`
ile değişkene alındı ve bir daha basılmadı. **Kurucuya not:** bu oturum kaydı parolayı içerdiği
için `docker-compose.yml` içindeki root parolasının değiştirilmesi önerilir (karar Kurucu'nundur;
ekip parolayı değiştirmedi — yapılandırma dosyasına izinsiz dokunulmadı).

---

## § 1 — BU PAKET NE YAPTI

P-23 Faz 1 ödeme kalemi tablosunu doğurmuş, Faz 2 geçmişi geri doldurmuştu; **paket adının
ikinci yarısı — "vezne fişinin genelleştirilmesi" — üç turda da yapılmamıştı.** Bu paket onu
ve doğrulayıcının açık bıraktığı 4 kalemi kapattı.

### 1.1 Asıl iş — ödeme kaleminin TÜM yollara yayılması (R-18 "tümü" dalı)

Faz 1/2 sonunda kalemi **yalnız satış sepeti** yazıyordu. Tahsilat · takas · işçilik · alış ·
evrak · tek bacaklı satış yolları parayı deftere yazıp kırılımı **kayıtsız** bırakıyordu.

Çözüm iki yapı taşıyla kuruldu:

| Yapı | Dosya | Ne yapar |
|---|---|---|
| `OdemeKalemFabrikasi` | `dotnet/Kuyumcu.Api/Data/OdemeKalemFabrikasi.cs` (YENİ) | `odeme_kalemleri` tablosuna yazmanın **TEK** kapısı. `Turet()` saf işlevi kalemi hareketten ve ödeme izinden türetir; `Yaz()` doğrular ve ekler. |
| `HareketFabrikasi.Ekle` kalem kapısı | `Data/HareketFabrikasi.cs` | Deftere yazan tek yol, **ödeme taşıyan her harekete kalemi kendisi yazar**. |

`OdemeIzi` kaydına dördüncü alan geldi: `KalemYazari Kalem = KalemYazari.Fabrika`.
Varsayılan **fabrika**dır → yeni bir yazma yolu eklendiğinde kalem **kendiliğinden** doğar,
çağıranın hatırlaması gerekmez (fail-closed). Fabrikayı atlayan **yalnız iki** beyan vardır ve
ikisinin de gerekçesi çağrı yerinde yazılıdır:

1. **Satış sepeti** (`SatisModulu.SatisFisi`) — çok bacaklı fişin kalemlerini kendi yazar
   (fabrika tek bacak türetemez).
2. **Storno** (`StornoServisi`) — ters kayıt **yeni kalem doğurmaz**; kaynağın kalemleri
   pasifleşir. Ters satıra da kalem yazılsaydı aktif kalem toplamı fiş toplamını aşar ve
   araç kırılımlı rapor hükümsüz parayı iki kez sayardı.

**Türetme sırası (tahmin YOK, yalnız yazılı değerler):**
1. `hareketler.odeme_turu` kapalı listede bir araçsa (NAKIT/KART/HAVALE/VERESIYE/DOVIZ) → TEK
   kalem, tutar = `|tl_tutari|`. `"KARMA"` bir araç değil özet etikettir, bu dalı tetiklemez.
2. Etiket yoksa / "KARMA" / "ALTIN" / "DIGER" ise kırılım `OdemeIzi` paylarından okunur:
   nakit payı → NAKIT kalemi, veresiye payı → VERESIYE kalemi. (Alışta kısmi ödeme böylece
   **iki** kalem doğurur ve toplamları fiş toplamına eşitlenir.)
3. Ne etiket ne pay varsa (transfer, emanet, sayım düzeltme, açılış) kalem **yazılmaz**.

**Kasa bağı uydurulmadı:** tek araçlı yolda kasa satırı hareketten SONRA doğduğu için
`kasa_id`/`kasa_hareket_id` NULL bırakıldı. Bağ kaybolmaz — `kasa_hareketleri.hareket_id`
aynı hareketi gösterir. Çok bacaklı satış fişinde bu alanlar eskisi gibi DOLU yazılır.

### 1.2 Vezne fişi başlığının genelleştirilmesi

`vezne_fisleri` iki kolon kazandı: **`fis_turu`** (kapalı liste `VezneFisTuru`: SATIS ·
TAHSILAT · ISCILIK; varsayılan SATIS) ve **`talep_tutar_tl`** (kalemsiz türlerde tezgâhtarın
girdiği tutar).

| Tür | Gövde | Tahsilde çağrılan servis | Deftere düşen satır |
|---|---|---|---|
| `SATIS` (varsayılan) | ürün kalemleri | `SatisServisi.SatisFisi` (paket 19 davranışı **birebir**) | `SATIS` (çok bacak) |
| `TAHSILAT` | tek `tutarTl` + cari | `CariServisi.TahsilatGirHareket` | `TAHSILAT` |
| `ISCILIK` | tek `tutarTl` + cari | `SatisServisi.Iscilik` | `ISCILIK` |

**R10 KORUNDU:** vezne modülü hâlâ deftere YAZMAZ. `VezneModulu.cs` içinde
`HareketFabrikasi.Ekle` / `Hareketler.Add` / `KasaHareketleri.Add` **yoktur**; her dalda
mevcut servis çağrılır, tevsik (459 s. VUK) · MASAK · gün kilidi · K6 kapıları o servislerin
içinde bugünkü tek yoldan koşar. Canlı doğrulamada TAHSILAT fişinin tahsilinden sonra
kiracıda **tek** hareket satırı bulunduğu SQL ile gösterildi.

### 1.3 Kapsam kararı — TAKAS ve EMANET vezne fişi TAŞIMAZ (gerekçeli)

Gerekçe kodda (`Data/VezneFisi.cs` başlığı), sunucu hata mesajında ve ekranda (`#vz-f-tur-kapsam`)
**yazılıdır**; tahmine bırakılmadı:

- **EMANET** — emanet giriş/çıkış **para hareketi doğurmaz** (`EmanetModulu` `OdemeIzi.Yok` ile
  yazar). Tahsil edilecek parası olmayan işin veznesi de olmaz; fiş açmak kullanıcıya
  "burada para alınacak" demek ve yanlış olurdu.
- **TAKAS** — takas satırı eski malın **ayarını, gramını ve has birim fiyatını** ister
  (`SatisModulu.Takas`). `vezne_fis_kalemleri` bugün yalnız (ürün, adet, birim fiyat, işçilik)
  taşır; takası desteklemek kalem tablosuna dört kolon daha eklemek demektir. Bu paket veri
  modelini o kadar genişletmedi. **Uydurma bir eşleme yapılmadı** (Anayasa md.1).

### 1.4 Kalemsiz fiş TEK bacaklıdır (fail-closed)

`TahsilatGir` ve `Iscilik` bugün **tek** ödeme aracı kabul eder; çok bacaklı sepet yalnız
`SatisFisi`'ndedir. İki bacağı "toplayıp" tek araç saymak kırılımı **uydurmak** olurdu.
İkinci bacak 400 ile reddedilir, sessizce yutulmaz. VERESIYE ve DOVIZ bacakları da bu iki
türde gerekçeli olarak reddedilir ("borç veresiyeyle ödenmiş olmaz").

---

## § 2 — DOĞRULAYICININ AÇIK BIRAKTIĞI 4 KALEM

### (1) Geri doldurma kapsam açığı — ÖLÇÜLDÜ, GEREKÇE YANLIŞ ÇIKTI, KAPATILDI

Doğrulayıcı "ödeme tutarı yazılı olan **13 hareket** kalem almadı; gerekçe: araç hiçbir yerde
yazılı değil" demişti. **Ölçüldü — bu gerekçe yanlıştır:**

| Grup | Adet | Aracın YAZILI olduğu yer |
|---|---|---|
| SATIS ×5 · HURDA_ALIS ×2 · ISCILIK ×1 · TAKAS ×1 | 9 | `kasa_hareketleri.odeme_turu = 'NAKIT'` |
| EVRAK_GIRIS | 4 | `hareketler.veresiye_tutar` (evrakla kapanan cari borç; kasa satırı yok) |

Gerçek sebep Faz 2 deyimindeki **`h.odeme_turu IS NOT NULL` süzgeciydi**: bu satırlarda
`hareketler.odeme_turu` NULL'dur, dolayısıyla üç kaynağın hiçbiri okunmadan satırlar elendi.

**Ayrıca doğrulayıcının 13'ü eksikti.** "Ödeme yazan yol" ölçütü yalnız `nakit+veresiye > 0`
ile tanımlanırsa 13 çıkar; ölçüte **araç etiketi** de eklenince (KART/HAVALE bacaklarında
nakit ve veresiye payı 0'dır) gerçek sayı **17**'dir: 4 SATIS satırı daha (KART ×2, HAVALE ×2)
kalemsizdi. Hepsi kapatıldı.

Migration: `20260921234547_P23cKalemsizYolGeriDoldurma` — **17 kalem**, toplam
**727.600,22 TL**, `bilinmiyor = 0` (tek bir değer bile tahmin edilmedi), `evrak_id` ilk kez
doldu (4 satır, bağ `kiymetli_evraklar.giris_hareket_id`'de YAZILIYDI).

⚠ Ölçülmüş tuzak, kodda yazılı: hariç tutma **fiş** bazındadır, hareket bazında değil. Çok
kalemli satış fişinde kalemler yalnız ilk hareketin kimliğine bağlıdır; hareket bazında
bakılsaydı aynı fişin diğer satırları "kalemsiz" görünür ve **fiş toplamı ikiye katlanırdı**.

### (2) MASAK + 459 s. VUK — gerileme testi YAZILDI

`P23cGenellestirmeTestleri.MASAK_ve_459_VUK_kumulatifi_kalem_tablosundan_ETKILENMEZ`:
kalem yazılıyorken 25.000 TL nakit işçilik geçer (had 30.000 aşılmadı), aynı gün 6.000 TL daha
**400 "459 s. VUK"** ile durur. Reddedilen işlemin kalemi de yazılmamıştır (transaction
bütünlüğü). Ayrıca eski alanın (`hareketler.nakit_tutar`) aynen yazıldığı doğrulanır —
kapıların kaynağı değişmedi.

### (3) `FIS_TOPLAM_ODEME_KALEMI_FARKI` — artık GERÇEKTEN FARKI ÖLÇÜYOR

Eski sayaç kapının **varlığını** sayıyordu. Yeni sayaç **canlı veritabanını** sorguluyor:

```sql
SELECT COUNT(*) FROM (
  SELECT COALESCE(h.fis_id,h.id) fk, ROUND(SUM(ABS(h.tl_tutari)),2) ft
    FROM hareketler h
   WHERE ((COALESCE(h.nakit_tutar,0)+COALESCE(h.veresiye_tutar,0)) > 0
          OR h.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ'))
     AND h.storno_kaynak_id IS NULL
     AND NOT EXISTS (SELECT 1 FROM hareketler s WHERE s.storno_kaynak_id = h.id)
   GROUP BY 1) a
LEFT JOIN (SELECT fis_id, ROUND(SUM(tutar_tl),2) kt
             FROM odeme_kalemleri WHERE aktif = 1 GROUP BY fis_id) b ON b.fis_id = a.fk
WHERE ABS(a.ft - COALESCE(b.kt,0)) > 0.005;     --  → 0
```

Çalışma zamanı kapısı ayrı bir sayaca taşındı (`FIS_TOPLAM_CALISMA_ZAMANI_KAPISI`): canlı
sayaç **geçmişi**, kapı **geleceği** ölçer. Docker yoksa sayaç **sessiz yeşil vermez**,
"ÖLÇÜLEMEDİ" uyarısı basar.

### (4) `§ DoD sözleşmesi` bölümü — bu raporda § 9'da.

### (5) Kararsız test — orkestratörün kapattığı kalem tekrar aranmadı.
Bu turda **başka** bir kararsız test görüldü; § 6'da adıyla ve izole koşum sonucuyla yazılıdır.

---

## § 3 — MEKANİZMA KURALI (CIRCIR) — TAZE ÖLÇÜLDÜ

| Sayaç | Taban (öncesi) | Sonrası | Ölçüm |
|---|---|---|---|
| `HAREKETLERDE_ODEME_YONTEMI_YENI_YOL` | 0 (beyaz liste: `SatisModulu.cs`) | **0** (beyaz liste **daraldı**: `Data/OdemeKalemFabrikasi.cs`) | statik grep |
| `ODEME_KALEMI_FABRIKA_KAPISI` | — (yeni) | **1** · atlayan beyan **2 ≤ 2** | statik grep |
| `ODEME_KALEMSIZ_YOL` | **17** | **0** | **canlı SQL** |
| `FIS_TOPLAM_ODEME_KALEMI_FARKI` | **17** (eski sayaç bunu göremiyordu) | **0** | **canlı SQL** |
| `FIS_TOPLAM_CALISMA_ZAMANI_KAPISI` | 1 | **1** | statik grep |

> Beyaz liste **genişlemedi, daraldı.** Paket koşulu "kalem yazmayan yol beyaz listeye
> giremez" idi; tek kapı olduğunda girebilecek bir yol kalmaz — kalem, ödeme taşıyan her
> harekette fabrika tarafından doğar.

---

## § 4 — MIGRATION

| Ad | Boyut (.cs / .Designer.cs) | İçerik | Uygulandı |
|---|---|---|---|
| `20260921234502_P23cVezneFisTuru` | 2.550 B / 214.298 B | `vezne_fisleri` + `fis_turu varchar(10) NOT NULL DEFAULT 'SATIS'`, `talep_tutar_tl decimal(15,2) NULL`, mevcut satırlara açık `UPDATE` | EVET |
| `20260921234547_P23cKalemsizYolGeriDoldurma` | 6.988 B / 214.320 B | veri: kalemsiz 17 hareketin kalemi (kasa hareketi + veresiye payı kaynaklı) | EVET |

`__EFMigrationsHistory`: **63 → 65**. `vezne_fisleri` mevcut 5 satırın hepsi `fis_turu = 'SATIS'`
(bu bir tahmin değil: paket 19'dan beri tek tür satıştı).

EF'in ürettiği `defaultValue: ""` **elle `"SATIS"` yapıldı** — boş bırakılsaydı mevcut satırlar
kapalı listenin dışında bir türle kalır ve tür kapısı onları reddederdi.

### Down() TATBİKATI — GERÇEKTEN KOŞTURULDU (BK-21)

```
Öncesi tam yedek : yedek-tatbikat-oncesi.sql · MD5 90ab1372132ecdfd545b09f9083226bb
Reverting migration '20260921234547_P23cKalemsizYolGeriDoldurma'.
Reverting migration '20260921234502_P23cVezneFisTuru'.
Done.
```

Geri alma sonrası ölçüm: `G3%` kalem **0** · Faz 2'nin `geri_dolduruldu=1` satırları
**90 — DOKUNULMADI** · `odeme_kalemleri` toplam **90** · `fis_turu`/`talep_tutar_tl` kolonları
**düşürüldü (0)** · migration **65 → 63**.

Yeniden uygulama: `Applying …P23cVezneFisTuru` + `…P23cKalemsizYolGeriDoldurma` → **Done** ·
`G3%` **17 / 727.600,22 TL** · kalem **107** · migration **65** ·
sonrası tam yedek `yedek-sonrasi.sql` · MD5 **b8dcf76eb1ab179faa7e83fe8f1bfc3d**.

İlk (tatbikat öncesi) tam yedek: `yedek-oncesi.sql` · MD5 **e160aae15288f75799b5f1828ab56034**
(41.014.672 B). Üç yedek de oturum scratchpad'indedir.

---

## § 5 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Paket öncesi taban (tam süit) | 3.785 | 3.785 | 0 |
| P23c hedefli (`~P23c`) | 16 | 16 | 0 |
| Vezne ekranı (bUnit) | 25 | 25 | 0 |
| **Tam süit (son, trx)** | **3.811** | **3.811** | **0** |

**Yeni test sayısı: 26** (21 sunucu/saf + 5 bUnit).

Yeni dosya: `dotnet/Kuyumcu.Tests/P23cGenellestirmeTestleri.cs`
(5 saf + 16 veritabanı testi). Genişletilen: `P23OdemeKalemiTestleri.cs`,
`VezneEkranTestleri.cs`.

`P23OdemeKalemiTestleri.Storno_kalemi_PASIFE_ALIR_SILMEZ` **bilinçli olarak değiştirildi**:
test kalemi **elle** ekliyordu (çünkü işçilik yolu kalem yazmıyordu). Artık yol kalemi kendisi
yazdığı için elle ekleme iki kalem üretiyordu; test kırıldı ve **kırılması doğruydu**. Elle
ekleme kaldırıldı, yerine "yolun kalemi tek ve doğru yazdığı" iddiası kondu.

---

## § 6 — KARARSIZ TEST (gizlenmedi)

Tam süitin ara koşumlarından **birinde** `P37RevizyonTestleri.Menu_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir`
kırmızıya döndü (3810/3811). **İzole koşuldu: 5/5 YEŞİL.** Ardından koşulan tam süit yine
**3811/3811 yeşil**. Bilinen bUnit + paralel koleksiyon yarışı kalıbıdır (orkestratörün
`P07DayaniklilikTestleri.B10…` için kayda geçirdiği kalıpla aynı). Bu pakette dokunulan hiçbir
dosya `P37RevizyonTestleri`'ni ya da menü aramasını ilgilendirmez. Sessizce "yeşil" denmedi.

---

## § 7 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P23c/` altına kopyalandı → `perl -0777 -pi -e` ile tek
eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir
aynılık kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| **M23c-1** | `HareketFabrikasi`: kalem kapısı `if (false && izi.Kalem == …)` | `Tahsilat_yolu_odeme_kalemi_YAZAR_ve_fis_toplamina_esittir`, `Iscilik_yolu_odeme_kalemi_YAZAR`, `Iscilik_veresiye_kalirsa_VERESIYE_kalemi_yazilir`, `Takas_farki_odeme_kalemi_YAZAR`, `Alista_kismi_odeme_IKI_kalem_yazar_toplami_fis_toplamidir`, `Storno_YENI_kalem_yazmaz_kaynagi_pasiflestirir`, `MASAK_ve_459_VUK_kumulatifi_kalem_tablosundan_ETKILENMEZ`, `Vezne_TAHSILAT_fisi_acilir_veznelenir_tahsil_edilir_ve_kalem_yazilir`, `Vezne_ISCILIK_fisi_tahsil_edilince_defterde_ISCILIK_satiri_dogar` (**9 kırmızı**) | `cmp` birebir aynı |
| **M23c-2** | `OdemeKalemFabrikasi.Turet`: VERESIYE pay kalemi satırı silindi | `OdemeKalemFabrikasiSafTestleri.KARMA_etiketi_paylardan_IKI_kalem_uretir_toplami_korunur`, `Alista_kismi_odeme_IKI_kalem_yazar_toplami_fis_toplamidir` | `cmp` birebir aynı |
| **M23c-3** | `StornoServisi`: `KalemYazari.Cagiran` kaldırıldı (storno kalem yazar) | `Storno_YENI_kalem_yazmaz_kaynagi_pasiflestirir` | `cmp` birebir aynı |
| **M23c-4** | `VezneServisi.TurDogrula`: tür kapısı `if (false && …)` | `TAKAS_turu_vezne_fisi_olarak_REDDEDILIR_ve_gerekce_yazilir` | `cmp` birebir aynı |
| **M23c-5** | `VezneServisi.Tahsil`: tek-bacak kapısı `if (false && …)` | `Kalemsiz_fis_TEK_bacakla_tahsil_edilir_ikinci_bacak_REDDEDILIR` | `cmp` birebir aynı |
| **M23c-6** | `SatisModulu.SatisFisi`: `KalemYazari.Cagiran` kaldırıldı (çift kalem yazımı) | `Eski_SATIS_fisi_davranisi_BIREBIR_korunur`, `P23OdemeKalemiTestleri.Kalemler_fisin_ILK_hareketine_baglanir`, `…Kalem_toplami_fis_toplamina_ESITTIR`, `…FisOku_kalemleri_ve_KALEM_TOPLAMINI_dondurur`, `…VERESIYE_kaleminde_kasa_bagi_YOKTUR_digerlerinde_VARDIR`, `…Her_bacak_VERESIYE_dahil_KALEM_yazar` (**6 kırmızı**) | `cmp` birebir aynı |
| **M23c-7** | `OdemeKalemFabrikasi.Kalem`: `Math.Abs(hareket.TlTutari)` → işaretli | `Tahsilat_yolu_odeme_kalemi_YAZAR_ve_fis_toplamina_esittir`, `Vezne_TAHSILAT_fisi_acilir_veznelenir_tahsil_edilir_ve_kalem_yazilir` | `cmp` birebir aynı |

Hiçbir mutasyon "testi kırmadan" geçmedi → **test açığı bulunmadı**.
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln --nologo` → **0 hata, 0 uyarı**.

---

## § 8 — DENETİM

```
✓ P-23: HAREKETLERDE_ODEME_YONTEMI_YENI_YOL      0
✓ P-23: ODEME_KALEMI_FABRIKA_KAPISI              1 (atlayan beyan: 2 ≤ 2)
✓ P-23: ODEME_KALEMSIZ_YOL (canlı)               0
✓ P-23: FIS_TOPLAM_ODEME_KALEMI_FARKI (canlı)    0 (gerçek toplam farkı ölçüldü)
✓ P-23: FIS_TOPLAM_CALISMA_ZAMANI_KAPISI         1 (kapı + test yerinde)
DENETİM TEMİZ.
```

(Betiğin geri kalanındaki `⚠` satırları bu paketten ÖNCE de vardı: P-02 QA rota kapsamı,
yayın tazeliği, iCloud eşitleme. Onlara dokunulmadı.)

---

## § 9 — DoD SÖZLEŞMESİ (Bertan koşulu · zorunlu bölüm)

### 9.1 Bu paketin doğurduğu/ dokunduğu kayıt türleri

| Varlık | Oluşturma | Düzenleme (PATCH) | Silme / pasife alma | Yetki | Denetim kaydı | Ekrandaki eylem |
|---|---|---|---|---|---|---|
| **`vezne_fisleri.fis_turu`** | `POST /vezne/fisler` (`fisTuru`) | **YOK — DEĞİŞMEZ** | — | `[Authorize]` (vezne controller) | `DenetimServisi.YazmaKaydet` (tahsil/iptal) | `DegismezIpucu` (`#degismez-ipucu-VezneFisiTuru-{id}`) + gerekçe + doğru yol: gerekçeli iptal → yeni fiş |
| **`vezne_fisleri.talep_tutar_tl`** | `POST /vezne/fisler` (`tutarTl`) | `PATCH /vezne/fisler/{id}` (`tutarTl`) — **gönderilmezse KORUNUR** | — (fiş IPTAL edilir, satır silinmez) | aynı | aynı | Çekmecede `#vz-d-tutar` alanı |
| **`odeme_kalemleri`** (bu pakette yeni yol) | `HareketFabrikasi.Ekle` → `OdemeKalemFabrikasi` (kullanıcı doğrudan yazamaz) | **YOK — türetilmiş kayıt** | **hard DELETE YOK** · storno `aktif=false` + `storno_hareket_id` | yazan hareketin yetkisi | hareketin denetim izi | Kalemi düzeltmenin yolu **hareketi stornolamaktır** (`POST /islemler/{id}/storno`) |

### 9.2 Hangisi neden salt okunur

- **`fis_turu`** — tür fişin **anlamıdır**: TAHSILAT fişini SATIS'a çevirmek kalemsiz bir fişi
  kalemli saymak ve damgalanmış tutarı anlamsızlaştırmak olurdu. Doğru düzeltme yolu
  gerekçeli iptaldir (kayıt **silinmez**, gerekçesiyle durur).
- **`odeme_kalemleri`** — finansal varlıktır ve **türetilmiş** kayıttır; kaynağı defter
  satırıdır. Elle düzenlenseydi fiş toplamı ile kalem toplamı ayrışır ve
  `FIS_TOPLAM_ODEME_KALEMI_FARKI` sessizce bozulurdu. Düzeltme yolu **storno**dur (tek motor:
  `StornoServisi`), ters iade taklidi yasaktır.

### 9.3 Her düğme ya istek atar ya disabled + gerekçe

| Ekran öğesi | Davranış |
|---|---|
| `#vz-f-tur` seçici | Değişince formun şekli değişir (kalem satırları ↔ tutar alanı) ve karşı şeklin girdisi **temizlenir** — ekranda görünmeyen değer sessizce gönderilmez |
| `#vz-f-kaydet` | Kalemsiz türde tutar yoksa **istek atılmaz**, `#vz-f-kalem-hata` alan altı mesajı çıkar (`TutarGerekli`); cari yoksa `CariGerekli` |
| Çekmece `Kaydet` | `DuzenlemeGecerli` kalemsiz türde **tutara** bakar; kilitliyken `KilitSebebi` yazılıdır |
| `#vz-f-tur-kapsam` | Takas/emanet kapsam dışılığını **yazar** (görünmez kural yok — UX-S30) |

### 9.4 Karşılanan UX sözleşmesi maddeleri

UX-S21 (satış fişinin tutarı kullanıcıdan istenmez, kalemlerden türer) · UX-S22 (tek şeklin
alanı çizilir, ikinci cevap yok) · UX-S25 (değişmez alan gerekçesini ve doğru yolu söyler) ·
UX-S26 (oluşturma ve düzenleme formu aynı alan kümesini taşır; tür listesi **tek diziden**) ·
UX-S27 (POST/PATCH yanıtı okunur, başarı bandına yazılır) · UX-S30 (kilitli düğme sebebini
söyler) · UX-S31 (tür listesi tek kaynaktan, ham kod ekrana basılmaz — `TurMetni` sözlükten
çevirir) · UX-S32 (kalem tutarı `para_birimi` ile birlikte taşınır).

---

## § 10 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198`
(nohup, **PID 60523**). Kiracı: `kiraci_davetleri`ne SHA-256 özetli satır + `POST /auth/kayit`
→ **201**, **`PAKETP-23C-TEST`**.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C1 | `POST /cari/{id}/tahsilat` HAVALE 4.500 | 200 | **200** · `tlBakiye -4500.00` |
| C2 | `POST /islemler/iscilik` NAKIT 750 | 200 + `hareketId` döner | **200** · `{"cariId":…,"iscilikTl":750,"hareketId":"01M336JV6TED5P0PH1CA5JZW6S"}` |
| C3 | `POST /vezne/fisler` `fisTuru=TAHSILAT`, `tutarTl=1250.75` | 200, `kalemler: []`, `veznelenebilir: true` | **200** · `kalemler:[]` · `veznelenebilir:true` |
| C4 | `PATCH /vezne/fisler/{id}` `tutarTl=1500.00` | 200, tutar güncellenir | **200** · `tutarTl 1500.0` · `toplamTl 1500.0` · `fisTuru TAHSILAT` |
| C5 | `PATCH` `fisTuru=SATIS` | **400** | **400** "…değiştirilemez. Yanlış türde açıldıysa fiş iptal edilir (gerekçesiyle kayıtta kalır) ve doğru türde yeni fiş açılır." |
| C6 | `POST …/veznele` | 200, damga | **200** · `durum VEZNEDE` · `toplamTl 1500.0` |
| C7 | `POST …/tahsil` **iki** bacakla | **400** | **400** "…tek ödeme aracıyla tahsil edilir; 2 bacak gönderildi. Karma ödeme bugün yalnız satış fişinde desteklenir." |
| C8 | `POST …/tahsil` tek KART bacağıyla | 200, defter satırı doğar ve bağlanır | **200** · `durum TAHSIL` · `hareketFisId 01M336K50PS93A39XKD5DKFQ9J` · `satis:{tahsilatTl:1500,arac:"KART",hareketId:…}` |
| C9 | `POST /vezne/fisler` `fisTuru=TAKAS` | **400** + gerekçe | **400** "Takas ve emanet vezne fişi taşımaz; takas doğrudan işlem ekranından girilir, emanet giriş/çıkışında para el değiştirmez." |

### SQL kanıtı (kiracı silinmeden ÖNCE)

```
+----------+------------+-----------+--------+----------+----------------+-----------------+-------+
| tur      | odeme_turu | tl_tutari | arac   | tutar_tl | fis_harekettir | geri_dolduruldu | aktif |
+----------+------------+-----------+--------+----------+----------------+-----------------+-------+
| TAHSILAT | HAVALE     |  -4500.00 | HAVALE |  4500.00 |              1 |               0 |     1 |
| ISCILIK  | NAKIT      |    750.00 | NAKIT  |   750.00 |              1 |               0 |     1 |
| TAHSILAT | KART       |  -1500.00 | KART   |  1500.00 |              1 |               0 |     1 |
+----------+------------+-----------+--------+----------+----------------+-----------------+-------+
kalemsiz_yol_kiraci  0
vezne_fis   TAHSILAT | TAHSIL | talep 1500.00 | toplam 1500.00 | hareket_fis_id 01M336K50PS93A39XKD5DKFQ9J
```

Üç yolun üçünde de fiş kesildi ve kalem yazıldı; kalemsiz yol **0**.
Vezne tahsilinden sonra kiracıda **tek** hareket satırı var → R10 (vezne deftere yazmaz) korundu.

### Temizlik

| Kontrol | Sonuç |
|---|---|
| API durduruldu (`kill 60523`) | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş** |
| 5199 (kurucunun sunucusu) | **dokunulmadı** — hâlâ dinliyor (2 satır) |
| Test kiracısı | `tenant_id` kolonu olan **63 tablodan** DELETE + `tenants` satırı + davet satırı |
| `tenants` sayısı | **9** (başlangıçtaki değer) — kalan: Demo/ByGatech/Düzeltme/Demo Kuyumcu/Ramazan Deneme + **QA-TEST-1/2/3/FRONTEND (DOKUNULMADI)** |
| Kalan satır | hareketler 0 · odeme_kalemleri 0 · vezne_fisleri 0 |

---

## § 11 — DEĞİŞEN DOSYALAR

Depo bütünüyle commit'siz olduğu için `git status --porcelain` binlerce önceden var olan satır
döner; **bu paketin dokunduğu dosyalar** şunlardır:

```
YENİ  dotnet/Kuyumcu.Api/Data/OdemeKalemFabrikasi.cs
YENİ  dotnet/Kuyumcu.Api/Data/Migrations/20260921234502_P23cVezneFisTuru.cs (+ .Designer.cs)
YENİ  dotnet/Kuyumcu.Api/Data/Migrations/20260921234547_P23cKalemsizYolGeriDoldurma.cs (+ .Designer.cs)
YENİ  dotnet/Kuyumcu.Tests/P23cGenellestirmeTestleri.cs
 M    dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs
 M    dotnet/Kuyumcu.Api/Data/VezneFisi.cs
 M    dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
 M    dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs
 M    dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs
 M    dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs
 M    dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs
 M    dotnet/Kuyumcu.Core/I18n/Sozlukler.cs            (TR + EN, 12 yeni anahtar)
 M    dotnet/Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs
 M    dotnet/Kuyumcu.Web/Pages/Vezne.razor
 M    dotnet/denetim.sh
 M    dotnet/Kuyumcu.Tests/P23OdemeKalemiTestleri.cs
 M    dotnet/Kuyumcu.Tests/VezneEkranTestleri.cs
```

**Dokunulan sayfalar (MÜHÜR 2 · TEK DOKUNUŞ):** `Kuyumcu.Web/Pages/Vezne.razor` — bu dalgada
ilk dokunuş. Yeni `ModalPencere` düzenleme kullanımı **açılmadı** (mevcut ModalPencere yalnız
yıkıcı iptal onayıdır); düzenleme tek kalıpta (`Cekmece`) kaldı. Mevcut element kimlikleri
**değişmedi**; yeni kimlikler `vz-` önekiyle: `vz-f-tur`, `vz-f-tur-aciklama`,
`vz-f-tur-kapsam`, `vz-f-tutar`, `vz-d-tur`, `vz-d-tutar`, `vz-tur-{id}`.

---

## § 12 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Vezne fişi genelleştirildi (asıl iş) | **KARŞILANDI** (kapsam: SATIS·TAHSILAT·ISCILIK; TAKAS/EMANET gerekçeli dışarıda) | § 1.2, § 1.3, canlı C3–C9 |
| Beyaz liste genişletilebilir AMA kalem yazımı zorunlu (fail-closed) | **KARŞILANDI** — liste **daraldı**, kapı `HareketFabrikasi`'na taşındı | § 3, `denetim.sh` |
| (1) Geri doldurma kapsam açığı ölçüldü/kapatıldı | **KARŞILANDI** — 13 değil **17**; hepsi yazılı kaynaklardan, `bilinmiyor=0` | § 2(1) |
| (2) MASAK + 459 s. VUK gerileme testi | **KARŞILANDI** | `MASAK_ve_459_VUK_kumulatifi_kalem_tablosundan_ETKILENMEZ` |
| (3) `FIS_TOPLAM_ODEME_KALEMI_FARKI` gerçek farkı ölçüyor | **KARŞILANDI** — canlı SQL, kapı ayrı sayaca taşındı | § 2(3), § 8 |
| (4) Raporda `§ DoD sözleşmesi` | **KARŞILANDI** | § 9 |
| (a) Her kural için hatayı yeniden üreten test | KARŞILANDI | § 5, § 7 |
| (b) Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | KARŞILANDI — 7 mutasyon | § 7 |
| (c) 5198'de canlı doğrulama + SQL kanıtı | KARŞILANDI | § 10 |
| (d) `denetim.sh` TEMİZ | KARŞILANDI | § 8 |
| (e) Tam süit 0 başarısız (+ kararsız test adıyla) | KARŞILANDI — 3811/3811; kararsız test § 6'da | § 5, § 6 |
| (f) Test kiracısı silindi | KARŞILANDI — `tenants` 9'a döndü, QA-TEST-* dokunulmadı | § 10 |
| (g) Rapor + Kimlik kapanış + DoD sözleşmesi | KARŞILANDI | bu belge |
| Tek yazma yolu `HareketFabrikasi` | KORUNDU | `Hareketler.Add` sayacı 0 |
| Tek storno motoru `StornoServisi` | KORUNDU | § 1.1 |
| Finansal varlıkta hard DELETE yok | KORUNDU | kalem `aktif=false` + `storno_hareket_id` |
| Kiracı izolasyonu (IKiraciVarlik + filtre + yazma denetçisi) | KORUNDU | `OdemeKalemi`/`VezneFisi` zaten `IKiraciVarlik`; yeni varlık eklenmedi |
| `dotnet ef` asla `--no-build`, boş migration yok | KARŞILANDI | § 4 |
| COMMIT YOK / git komutu yok / rm-mv yok | KARŞILANDI | yalnız `git status --porcelain` (salt okunur) |
| Pages altında yeni istemci record açılmadı | KARŞILANDI | şekiller `Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs`'te |
| İstemci alanı sunucuyla ad VE tip olarak eşleşir | KARŞILANDI | `FisTuru string?`, `TutarTl decimal?`, `Kalemli bool` |
| Sozlukler TR + EN | KARŞILANDI | 12 anahtar × 2 dil |

---

## § 13 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Konu | Bu paketteki durum |
|---|---|---|
| **R-17** | Ödeme kalemi tablosu | **KAPANDI** — tablo Faz 1'de doğdu, Faz 2 geçmişi doldurdu, **Faz 3 tüm yollara yaydı**; `ODEME_KALEMSIZ_YOL = 0` |
| **R-18** | Mahsup: tümü ya da hiç | **KAPANDI (tümü dalı)** — Faz 2'de "hiç" dalı gerekçeyle seçilmişti (MAHSUP bir ödeme aracı değildir, satıştan sonra yazılan ayrı defter hareketidir; `OdemeAraci.Tumu` onu içermez, `R18_Mahsup_Odeme_Araci_Olarak_Girmez` testi kilitler). Faz 3, o kararın gerekçesinde geçen **"tümü dalı tahsilat·vezne·takas·emanet·işçilik ve P-21 değişiminin HEPSİNDE kalem üretmeyi gerektirir"** şartını **fiilen yerine getirdi**: artık ödeme yazan her yol kalem yazar. Mahsup kararı **değişmedi** (hâlâ araç değildir) — değişen, diğer yolların kalemsiz olmasıdır. |
| **KRİTİK-3** (§ 12) | Vezne fişi başlığı genelleştirilmedi | **KAPANDI** — § 1.2 |
| Y-22 | Emanet parça kimliği | **AÇILMADI** (K-12 hukuki görüşü beklemede); vezne EMANET türü de bu yüzden yok |
| Y-24 | Taksit planı (Faz B) | **YER KORUNDU** — `taksit_adedi` + `pos_tahsilat_id` yerinde; kolon eklenmedi |

---

## § 14 — AÇIK KALANLAR / SONRAKİ PAKETE

1. **Vezne TAKAS fişi** — `vezne_fis_kalemleri`ne (eski ayar, eski gram, eski has birim fiyat,
   eski maden) dört kolon eklenmesini gerektirir. Bu paket veri modelini genişletmedi; ekranda
   ve sunucu mesajında kapsam dışılığı **yazılı**. Sonraki paket için ölçülmüş iş kalemi.
2. **Kalemsiz vezne fişinde karma ödeme** — `TahsilatGir`/`Iscilik` tek araç kabul ettiği için
   kapalı. Açılması `OdemeSepeti`'nin o iki servise taşınması demektir (SatisFisi ile aynı
   sepet motoru); bu paket kapsamında değildi, fail-closed reddedildi.
3. **Tek araçlı yolun kalem `kasa_id`/`kasa_hareket_id` alanları NULL** — bağ
   `kasa_hareketleri.hareket_id` üzerinden tamdır, ama isteyen paket bu iki alanı da
   doldurabilir (kasa satırı hareketten sonra doğduğu için ikinci bir yazma adımı gerekir).
   Uydurulmadı.
4. **Kararsız bUnit testleri** — `P37RevizyonTestleri.Menu_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir`
   bu turda bir kez kırmızı verdi (izole 5/5 yeşil). Paralel koleksiyon yarışının kökü
   kapatılmadı; orkestratörün kararsızlık kütüğüne eklenmesi önerilir.

---

## § 15 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **PAROLA (acil, Kurucu kararı):** § 0. MySQL root parolası bu oturumun kaydında düz metin
   göründü. Ekip yapılandırmaya izinsiz dokunmadı; parolayı değiştirme kararı Kurucu'nundur.
2. **Mali müşavire:** geri doldurulan 17 satırın araç bilgisi **ölçülmüş** kayıtlardan geldi
   (`kasa_hareketleri.odeme_turu` ve `hareketler.veresiye_tutar`); hiçbiri tahmin değildir ve
   `bilinmiyor` damgalı satır **yoktur**. Toplam 727.600,22 TL'lik kırılım, ilgili fişlerin
   **mevcut** toplamlarıyla kuruşu kuruşuna denkleşir (`FIS_TOPLAM_ODEME_KALEMI_FARKI = 0`).
   Bu kayıt **yeni bir vergisel olay yaratmaz**: `odeme_turu`, `nakit_tutar`, `veresiye_tutar`
   alanlarına dokunulmadı, tevsik (459 s. VUK) ve MASAK kümülatifleri **eski kaynaklarından**
   okumaya devam ediyor (gerileme testi § 2(2)).
3. **Mali müşavire:** EVRAK_GIRIS satırlarının ödeme kalemi `VERESIYE` aracıyla yazıldı ve
   `evrak_id` ile ilgili çek/senede bağlandı. Gerekçe: evrakla kapanan cari borç bir **hesaba
   mahsup**tur, kasaya para girmez. Kapalı araç listesine ayrı bir "EVRAK" aracı **eklenmedi**
   çünkü liste `OdemeBacagiDto` ile birebir aynı olmak zorundadır (ayrışırsa fiş toplamı ≠
   kalem toplamı çıkar). Farklı bir muhasebe yorumu varsa bildirilmesi hâlinde ayrı araç
   açılabilir.
4. **Ürün durumu:** dalga 2b+2c ve QA turu 2 kuralı gereği bu pakette hiçbir fiyat/teklif/
   tahsilat metni üretilmedi.
