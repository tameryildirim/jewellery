# PAKET CARI-BAKIYE — Uygulama raporu (30.09–01.10.2026, şerit t9 · tur 1 + tur 2)

Kapsam: cari bakiye için tek tanım, tek yazıcı ve TL aynası. Beş madde:
1. Liste ile detay arasındaki çelişki (emanet).
2. Bakiyeyi yazan yolların tek servise toplanması.
3. TL bakiye aynası ve denetim.sh'a iki sayaç.
4. Yanlış "düzeltme fişiyle giderin" metninin düzeltilmesi.
5. Fatura taslak PATCH'te orantılı hesaptan doğan 6. hane birikimi.

Kaynaklar: `ham/CARI-BAKIYE-UYUSMAZLIK-2026-09-30.md`, `ham/SENARYO-CIFT-BACAK-2026-09-30.md`, `ham/KESIF-KARARLAR-2026-09-30.md`, TOPLU kuyruk § 30.09 08:50 ve ekip oyu (30.09 23:10).
Rol: Taşkın (backend) ve Ertuğ (ekran). COMMIT yapılmadı, dosya silinmedi, hiçbir sır basılmadı.

## 0. TUR 2 (01.10.2026 01:35–02:15) — hasım doğrulayıcı bulgularının kapanışı
Tur 1'in bütün kanıtları aşağıda (§ 1–13) duruyor. Bu bölüm yalnız tur 2'de yapılanı ve yeniden ölçüleni anlatır.

### 0.1 Bulgu → yapılan
| Bulgu | Düzey | Yapılan | Kanıt |
|---|---|---|---|
| Evrak tahsilinin stornosu cariye −TL yazıyor (`StornoServisi.CariEtkisi` TAHSILAT dalı koşulsuzdu) | KRİTİK | TAHSILAT dalı ayrıldı ve aynayla **ortak kurala** bağlandı: `CariBakiyeKurali.TahsilatCariTl(h.TlTutari)`. Kaynağın TL'si artıysa (evrak tahsili) cariye 0 yazılır, eksiyse (müşteri ödemesi) eski davranış sürer. Aynanın `GrupTlEtkisi` TAHSILAT dalı da aynı yüklemi (`TahsilatCariyeYazar`) çağırır. Yani yazan kod ile ayna tek kaynaktan okur (EmanetKurali deseni). | Doğrulayıcı testi `CARIBAKIYESinirTestleri.Evrak_tahsili_stornosu_cari_TL_yi_ve_aynayi_bozmaz` artık **yeşil**. Yeni testler: `CariBakiyeTur2Testleri.Evrak_tahsili_stornosu_cariye_yazmaz_TL_aynasi_sifir` ve `Tahsilat_cari_TL_kurali_storno_ve_aynada_ortak` (3 durum). Mutasyonlar: MCBK-18, 19, 19b. Canlıda evrak → tahsil → storno sonrası cari TL −10.000'de kaldı, TL farkı 0 (§ 0.4). |
| Bilinen tohum listesi 3 değil 4; 4. satır fiilen beyaz liste | ORTA | 4. satır karara bağlandı: **Karar K-CB-4** (§ 13). denetim.sh artık iki listeyi ayrı tutar: `CB_BILINEN_TOHUM_OY3` (ekip oyu 3'ün üç carisi) ve `CB_BILINEN_TOHUM_KCB4` (Kurban Musteri). Çıktı da iki ayrı NOT satırı basar. Sayaç dördünü de ölçer. Listeden çıkarılırsa ✗ YENİ 1 çıkar. | denetim çıktısı § 0.3 · MCBK-22 |
| Canlı ayna ölçülemezse `HATA` atanmıyordu (sessiz yeşil) | DÜŞÜK | else dalı artık `✗ … ÖLÇÜLEMEDİ` basar ve `HATA=1` atar. | MCBK-21: docker'sız PATH ile blok `✗` basar ve HATA=1 döner. `HATA=1` satırı silinince HATA=0 çıkar (mutasyon yakalandı), geri alındı, cmp AYNI. |
| Dışa aktarım CSV `hasGramBakiye` kayıtlı değeri yazıyordu | DÜŞÜK | `Disaaktarim/Kaynaklar.cs` `CariKaynagi` artık `CariBakiyeKurali.Gorunum` ile birleşiyor. `hasGramBakiye` = ekrandaki tek tanım (emanetsiz). Yeni kolonlar: `emanetteHas` ve `kayitliHasGramBakiye` (açık adla kayıtlı kolon). Emanet her iki yerde aynı kuralla ayrılır. | `CariBakiyeTur2Testleri.Disa_aktarim_cari_csv_has_tek_tanim_emanet_ayri` (2 / 9,16 / −7,16 / 800) · MCBK-20, 20b |
| Birimsiz eski taslak kalemde ilk düzeltmede bir kez 6. hane kayması | DÜŞÜK | Hasımın önerisiyle **kabul edildi**, kod değişmedi. Sınır şu: yalnız migration'dan (`FaturaKalemBirimHas`) önce yazılmış ve birim has'ı olmayan TASLAK kalemi etkiler. Birim `ROUND(toplam ÷ miktar, 6)` ile bir kez dondurulur. Toplam miktara tam bölünmüyorsa (ör. 1,000000 ÷ 3) birim × 3 = 0,999999 olur ve 1 milyonda 1 has kayar. Birim dondurulduktan sonraki her gidiş-dönüş kaymasızdır (`Fatura_taslak_birimsiz_eski_kalem_ilk_duzeltmede_birim_donar`). Ürün kartındaki HasGrami'den birim almak reddedildi: ürün kartı taslak yazıldıktan sonra düzenlenmiş olabilir, bu durumda kalemin tarihsel has'ı değişir. Bu, 6. hanedeki kaymadan daha büyük bir hata olurdu. | — |

### 0.2 Testler (tur 2)
- Yeni sınıf `CariBakiyeTur2Testleri` (aynı dosya `CariBakiyeTestleri.cs`) **5 durum** içerir: DB 2 · saf teori 3.
- Filtreli koşum `~CariBakiye|~CARIBAKIYE`: **50/50 geçti**. Buna tur 1'in 35 testi, tur 2'nin 5 testi ve doğrulayıcının 10 sınır testi dahildir.
- Komşu kapsam `~Storno|~Evrak|~DisaAktarim|~CiftBacak|~KesifP2`: **350/350 geçti**.
- **Tam süit** kopyada koşuldu: 01:51'de ağaçtan alınmış rsync anlık görüntüsü, temiz build (0 uyarı, 0 hata). Sonuç dosyası `trx-pCARI-BAKIYE-t2/_Mac_2026-10-01_02_00_57.trx`: **Toplam 8295 · Geçti 8287 · Başarısız 7 · Koşulmadı 1.**
  - Tur 1'den kalanlar (§ 12, tarih devri): `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar` ve `GorunumSubesiTestleri.K3_Aylik_secili_sube_ve_tum_subeler`.
  - KESIF-P5 şeridi (01:46'da yazılıyordu, `KesifP5Testleri.cs`, Vezne/İşlemler ekranı):
    - `KesifP5VezneTestleri.Vezne_kalem_urun_secici_ezik_degil` (basit True/False),
    - `KesifP5VezneTestleri.Vezne_kuyruk_dar_ekranda_yigin_kart_ve_hucre_etiketleri`,
    - `KesifP5EkranTestleri.Islemler_alt_dugmeler_ayni_sinif`.
  - SATIS-4 şeridi (`Islemler.Satis4.cs`, `Satis4Yardim`): `CumaSAldimSadeTestleri.CUMA_S_02_Basit_gercek_musteride_kisa_durum_Nakit_alinmaz_Pesin_cumlesi_YOK`.
  - Bu beşi ayrıca filtreli koşumda da kırmızı çıktı (5/25). Beşi de Web/bUnit ekran testidir. Tur 2'de değişen dosyaların hiçbiri Web projesinde değil.
  - **Bu paketin yeni kırmızısı: 0.**
- Tur 2'de değişen ağaç dosyaları ile kopyadaki dosyalar `cmp` ile birebir AYNI. Ağaçta son build (01:50): `dotnet build Kuyumcu.sln` → **0 hata**. 2 uyarının ikisi de başka şeritlerin test dosyalarında:
  - `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` (CS8601),
  - `KesifP2Testleri.cs:454` (xUnit2029, KESIF-P2 satırı).

### 0.3 denetim.sh (ağaçta, 01:58)
```
  ✓ CARI_BAKIYE_DOGRUDAN_YAZIM (tek yazıcı dışı) 0
  ✓ CARI_BAKIYE_AYNA_UYUSMAZ (canlı, has+TL)      ölçülen 4 · bilinen tohum hatası 4 · yeni 0
  ⚠ CARI_BAKIYE bilinen tohum hatası (NOT)        3 cari ekip oyu (3) · ByGatech demo 09.09 Faz 0 öncesi tohum — kurucu kararıyla silinecek
  ⚠ CARI_BAKIYE bilinen tohum hatası (NOT, K-CB-4) 1 cari · Demo Kuyumculuk 'Kurban Musteri' 09.09 K1 kiracılar arası yazım kalıntısı — ayrı ekip kararı 01.10, silme kurucuda
  · CARI_BAKIYE TL aynası ölçülemez cari (bilgi) 10 (Faz 0 öncesi veresiye payı yok ya da konsinye sahibi — uydurulmaz)
```
Genel sonuç «DENETİM BAŞARISIZ». Nedeni başka şeritlere ait iki ✗ satırıdır; tur 1'deki record/P-02 ✗'leri artık yok:
- `P-30: DEMO_INSAN_SURESI`: son tarih 30.09 geçti.
- `B-3: ESIK_ISTEMCIDE 5 (tavan 4)`: 5. satır `Kuyumcu.Web/Pages/Islemler.Satis4.cs:90`, SATIS-4 şeridi.

Bu paketin sayaçları ✓.

Evrak tahsili stornosunun canlı kuyumcu_net'te yeni uyuşmazlık üretmediği şöyle görüldü: tur 1 ile tur 2 arasında sayaç «yeni 0» kaldı. Tek başına t9 ölçümü (§ 0.4) ise 0 uyuşmazlık verdi.

### 0.4 Canlı doğrulama (tur 2: API 5188 kopyadan · vekil 5189 · CDP 9984 · `kuyumcu_test_t9`)
- Kiracı `PAKETCARI-BAKIYE-TEST senaryo-2` ürünün kendi uçlarıyla yeniden kuruldu: davet → kayıt → kur → 2 kasa → müşteri → ürünler. Sistem anahtarı yeni üretildi (chmod 600) ve basılmadı.
- **46 adımlık senaryo AYNEN yeniden koşuldu** (`t2-kosum-ana.log`, `t2-kosum-ek.log`, `t2-senaryo-ozet.txt`):
  - Ana: 40 adım, 40 `[OK]`, **0 HATA**. «SON değişmezler: TEMİZ · uyuşmazlık 0.0». Son değerler tur 1 ile aynı: TL 1.500 · has 0 · kayıtlı −4,58 · emanette 4,58 · kasa1 93.472 · kasa2 103.752 · hurda 10,076.
  - Ek: 6 adım, 6 `[OK]`, **0 HATA**.
  - 46 adımın hepsinde sıfır ölçüldü: max |ekstre TL farkı| 0 · max |ekstre has farkı| 0 · max |SQL has − bağımsız ayna| 0 · max |SQL TL − bağımsız TL ayna| 0 · max |liste/detay − ekstre has| 0. TL ölçülemez adım yok.
- **Üç müşteri** (API + SQL, `t2-musteriler.log`):

| Müşteri | Liste (TL · has · emanette) | Detay (TL · has · emanette · kayıtlı) | Ekstre (TL · emanetsiz · emanette · has farkı · TL farkı) | SQL |
|---|---|---|---|---|
| Emanetli | 1500 · 0 · 4,58 | 1500 · 0 · 4,58 · −4,58 | 1500 · 0 · 4,58 · 0 · 0 | 1500.00 · −4.580000 |
| Emanetsiz | 500 · 3,5 · 0 | 500 · 3,5 · 0 · 3,5 | 500 · 3,5 · 0 · 0 · 0 | 500.00 · 3.500000 |
| Stornolu | 300 · 1,0 · 0 | 300 · 1,0 · 0 · 1,0 | 300 · 1,0 · 0 · 0 · 0 | 300.00 · 1.000000 |

- **Evrak tahsili stornosu (KRİTİK bulgunun canlı kanıtı, `t2-evrak.log`):**

| Adım | liste TL | ekstre TL | TL farkı | SQL `tl_bakiye` |
|---|---|---|---|---|
| 1 çek girişi 10.000 | −10.000 | −10.000 | 0 | −10000.00 |
| 2 tahsil edildi (kasa1) | −10.000 | −10.000 | 0 | −10000.00 |
| 3 tahsilin stornosu (HTTP 200, `cariBakiyesi: null`, kasa 93.472) | **−10.000** | −10.000 | **0** | **−10000.00** |

  SQL hareketleri: `EVRAK_GIRIS −10000` · `TAHSILAT +10000` · `TAHSILAT −10000 (storno)`. Eski kod 3. adımda −20.000 yazardı; doğrulayıcı bunu testte ölçmüştü.
- **Fatura taslak PATCH** (`t2-fatura.log`): oluştur 3.000 · 3.703755 · birim 1.234585 → PATCH 1,001 → 1.235820 → PATCH 3 → **3.703755**. Kayma 0.
- denetim.sh'ın canlı ayna SQL'i t9'a çevrilip tek başına koşuldu: senaryo kiracısının 6 carisinde (evrak müşterisi dahil) «ölçülen 0 · yeni 0», TL ölçülemez 0.
- **Tarayıcı** (başsız Chrome, `olc.mjs` + `sikisma-olc.js`; `olcum-t2/`). `Cariler.razor` tur 1 ölçümünden sonra değişmedi (son yazım 00:01). Yine de yeni veriyle yeniden ölçüldü:

| Rota | Genişlik | Mod (istenen = gerçek) | Tema | K3 taşan/kırık | K5 TRY | K9 | Yatay |
|---|---|---|---|---|---|---|---|
| /cariler liste | 1280 · 375 | BASİT | aydınlık · karanlık | 0/0 | 0 | 0 | 0 |
| /cariler liste | 1280 · 375 | PRO | aydınlık · karanlık | 0/0 | 0 | 0 | 0 |
| /cariler detay (emanetli) | 1280 · 375 | BASİT | aydınlık · karanlık | 0/0 | 0 | 6* | 0 |
| /cariler detay (emanetli) | 1280 · 375 | PRO | aydınlık · karanlık | 0/0 | 0 | 6* | 0 |

  \* K9 tur 1 ile aynıdır: detay çekmecesinin altında kalan tablo başlıklarıdır.

  Read ile görülen görüntüler:
  - `cariler-liste__BASIT_aydinlik_1280.png`: Senaryo Müşteri «1.500 ₺ · 0 has · Emanette 4,5800 has»; CB Evrak Müşteri «−10.000 ₺ Biz borçluyuz · 0 has» (storno sonrası).
  - `cariler-liste__PRO_karanlik_375.png`: aynı sayılar. Tablo kendi içinde yatay kaydırılır, sayfa kaymaz.
  - `cariler-detay__PRO_karanlik_375.png`: «ALTIN BAKİYESİ 0 has · EMANETTE 4,5800 has».
- **Temizlik:**
  - API (PID 47505/47517) ve vekil (PID 60614) `kill` ile durduruldu. 5188, 5189 ve 9984 boş. 5199/5200'e dokunulmadı.
  - `zsh kiraci-temizle-t9.sh 01M3T8ZJ5KA6NDY6XMN70XC6GW --uygula` 680 satır ve 1 tenants satırı sildi. 1 davet satırı ayrıca silindi (ROW_COUNT 1). Kalan: t9 `PAKETCARI%` 0 · t9 tenants 4. Bu 4 kiracı tam süitin KESIF-P5 HTTP fikstürleridir ve bir sonraki test süreci veritabanını baştan kurar.
  - `kuyumcu_net`'e yazılmadı: `PAKETCARI%` 0. tenants sayısı 14; tur 2 başında 15 ölçülmüştü. Aradaki fark başka şeritlerindir, bu paket o tabloya yazmadı.
  - `kimlik.json` ve `sistem.anahtar` `printf ''` ile 0 bayta indirildi.

### 0.5 Mutasyon (tur 2)
Kopyada koşuldu. Yöntem: yedek `mut-yedek/CARI-BAKIYE-t2/` → `perl -0777` ile tek eşleşmeli değişiklik → build (kırıksa test koşulmaz) → filtreli test → geri kopya → `cmp`.

Not: ilk denemede ağaçta 19b ve 20 build'i başka şeridin yarım kaldığı `Satis4Yardim` yüzünden kırıldı. Test eski DLL ile koştuğu için bu iki sonuç **geçersiz sayıldı**. Ağaçta Api yeniden derlendi, bütün mutasyonlar kopyada baştan koşuldu ve betiğe «build kırıksa dur» kapısı eklendi.

| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| MCBK-18 | Storno TAHSILAT → koşulsuz `(h.TlTutari, …)` | `CariBakiyeTur2Testleri.Evrak_tahsili_stornosu_cariye_yazmaz_TL_aynasi_sifir` · `Dogrulayici.CARIBAKIYESinirTestleri.Evrak_tahsili_stornosu_cari_TL_yi_ve_aynayi_bozmaz` | cmp AYNI |
| MCBK-19 | `TahsilatCariTl` → her zaman `kaynakTl` | Evrak_tahsili_stornosu_cariye_yazmaz… · Tahsilat_cari_TL_kurali_storno_ve_aynada_ortak(2500,0) · CARIBAKIYESinir.Evrak_tahsili… | cmp AYNI |
| MCBK-19b | ortak yüklem `< 0` → `!= 0` | TL_etkisi_kural_tablosu(TAHSILAT 2500/0) · (TAHSILAT −2500 storno) · Evrak_tahsili_stornosu_cariye_yazmaz… · Tahsilat_cari_TL_kurali…(2500,0) · CARIBAKIYESinir.Evrak_tahsili… | cmp AYNI |
| MCBK-20 | CSV `hasGramBakiye` ← kayıtlı kolon | `CariBakiyeTur2Testleri.Disa_aktarim_cari_csv_has_tek_tanim_emanet_ayri` | cmp AYNI |
| MCBK-20b | CSV `emanetteHas` ← 0 | Disa_aktarim_cari_csv_has_tek_tanim_emanet_ayri | cmp AYNI |
| MCBK-21 | (denetim.sh) ölçülemedi dalında `HATA=1` silindi | blok HATA=0 döndü; özgün hâl HATA=1 döner | cmp AYNI |
| MCBK-22 | (denetim.sh) K-CB-4 listesi boşaltıldı | `✗ CARI_BAKIYE_AYNA_UYUSMAZ … YENİ 1` → 01M240M0MV… TL farkı 77777.00, HATA=1 | cmp AYNI |

Son mutasyondan sonra kopyada temiz build yapıldı (0 uyarı, 0 hata). Tam süit bu temiz build ile koşuldu.

### 0.6 Tur 2'de değişen dosyalar
- `dotnet/Kuyumcu.Api/Common/CariBakiyeServisi.cs`: `TahsilatCariyeYazar` ve `TahsilatCariTl` eklendi; `GrupTlEtkisi` TAHSILAT dalı bunlara bağlandı.
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`: yalnız `CariEtkisi` TAHSILAT dalı.
- `dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs`: yalnız `CariKaynagi`. **Dosya alanı sapması:** alan listesinde yoktu ama hasım bulgusu açıkça bunu istedi. Yalnız okuma yoluna dokunuldu.
- `dotnet/Kuyumcu.Tests/CariBakiyeTestleri.cs`: `CariBakiyeTur2Testleri` sınıfı eklendi.
- `dotnet/denetim.sh`: K-CB-4 ayrımı ve ölçülemedi durumunda `HATA=1`.
- Razor, CSS ya da sözlük değişmedi. Migration gerekmedi.

## 1. Yapılanlar

### (1) Tek tanım: müşterinin has bakiyesi her yerde emanetsizdir
**ADR-CB-1 (bu paket).** Kayıtlı kolonun (`cari_bakiyeler.has_gram_bakiye`) anlamı **değişmedi**. Kolon emanet dahil tutulmaya devam eder (K4 ve K20-2 sözleşmesi, `EmanetKurali`). Emanetsiz değer okuma sırasında tek fonksiyonla üretilir: `CariBakiyeKurali.EmanetsizHas(kayıtlı, emanetEtkisi)`.

Bu yol daha az riskli olduğu için seçildi. Kolonun anlamını değiştirmek şunları gerektirirdi:
- canlı satırların geri doldurulması,
- emanet giriş, çıkış ve storno yollarının hepsinin aynayla birlikte değiştirilmesi,
- Pozisyon modülündeki kolon ↔ defter ALTIN mutabakatının yeniden yazılması.

Okuma tarafında bir hata olursa kayıtlı veri hiç oynamaz.

**Sunucu.** `hasGramBakiye` artık **her uçta** emanetsiz değeri verir:
- `GET /cari` (liste, tek sorguyla `EmanetEtkileri`),
- `GET /cari/{id}`,
- `GET /cari/{id}/bakiye`,
- `GET /cari/{id}/ekstre`,
- pasife alma özeti (`CariBagliKayit`),
- rapor → cari yaşlandırma (`CariBakiyeKurali.Gorunum`; süzgeç SQL'de çalışır, yalnız emaneti olan cari "bakiyesi olan" sayılmaz),
- kâğıt ekstre (`CiktiModulu`: "Has gram bakiye" emanetsiz; emanet ayrı satırda "Emanette (bakiyeye girmez)").

Uçlara iki yeni alan eklendi: `emanetteHas` (bizde duran, artı) ve `kayitliHasGramBakiye` (kolonun ham değeri, tanı için).

**İstemci.**
- `CariVeri.EmanetteHas`, `BakiyeVeri.EmanetteHas` ve `BakiyeVeri.KayitliHasGramBakiye` eklendi. Yeni tip açılmadı.
- Cariler listesinde has hücresi sunucunun emanetsiz değerini basar. Emanet varsa altına ayrı bir satır gelir: «Emanette 4,5800 has» (`#cari-liste-emanette-{id}`). İstemci hesap yapmaz.
- Sonuç olarak KESIF-P2 açık kalan (a) kapandı. Aynı müşteri listede "−5,2278 Biz borçluyuz", detayda "0,9322 Bize borçlu" görünüyordu; artık iki yerde de aynı sayı var.

**Veresiye limiti.** `VadeKapisi.KrediLimitiDogrula` yalnız TL ve döviz okur, has okumaz. Bu yüzden tanım değişikliğinden etkilenmez; kod değişmedi.

**Tahsilat önerisi.** Ekrandaki öneri (`FisBakiyeRozeti`, `FisProYanSutun`, Panel) liste ucunun `hasGramBakiye` alanını okur. Değer sunucuda düzeldiği için istemcide değişiklik gerekmedi.

### (2) Tek yazıcı
Yeni dosya: `Kuyumcu.Api/Common/CariBakiyeServisi.cs` → `CariBakiyeYazici`.
- `Uygula` metodu satırı önce `Local`'de arar, bulamazsa veritabanında arar, o da yoksa açar.
- TL 2 haneye, has 6 haneye yuvarlanır.
- Döviz kolonuna `DovizBakiye.Ekle` üzerinden yazar.
- `SonIslemTarihi` ve `Guncellendi` alanlarını günceller.
- `BosSatir` sıfır bakiye satırını üretir.

Eskiden beş modülde beş ayrı yazıcı vardı. Hepsi yalnız **yazım satırından** tek servise yönlendirildi; çağıranların imzası ve davranışı aynı kaldı:
- `CariServisi.BakiyeGuncelle`
- `EmanetServisi.BakiyeGuncelle`
- `EvrakServisi.BakiyeGuncelle`
- `SatisServisi.CariBakiyeGuncelle` (satış, alış, hurda, takas, iade, işçilik, konsinye, avans, değişim)
- `StornoServisi` satır içi bloğu

İki oluşturma noktası (`CariModulu.Olustur`, `SistemCarisi`) da `BosSatir` kullanıyor.

Önceki davranış farkları giderildi:
- Emanet, Evrak ve Cari yazıcıları yuvarlamıyordu.
- Yalnız Satış yazıcısı `Local`'e bakıyordu.

### (3) TL aynası
`CariBakiyeKurali.GrupTlEtkisi` kuralın tek gövdesidir. `TlEtkisi` satır yüzü, `TlAynasi` ise toplamıdır. Kural, yazan kod okunarak satırın kendi kolonlarından çıkarıldı:

| Tür | Cari TL etkisi |
|---|---|
| VERESIYE · ACILIS · CARI_DUZELTME · EVRAK_RUCU | TL tutarının kendisi |
| TAHSILAT | Kaynak yönü eksiyse (müşteri ödedi) TL tutarı. Artıysa 0: bu evrak tahsilidir, borç evrak girişinde kapanmıştı. Storno satırında yön kaynağın yönüdür. |
| AVANS_ALINDI · AVANS_IADE | −TL (AvansKapisi) |
| Diğer her tür (satış, alış, iade, hurda, takas, işçilik, evrak girişi/ciro/karşılıksız, döviz, avans mahsup, konsinye) | işaret(TL) × veresiye payı |
| Veresiye payı NULL (Faz 0 öncesi) ve TL ≠ 0 | **bilinmiyor → null**, uydurulmaz |
| Değişim, fark müşteri lehine ve VERESİYE | İki bacak da ödeme izi taşımaz. Fark, fişin kendisinden çıkarılır (`DegisimVeresiyeFarki`) |
| Konsinye malın sahibi | Borç defter satırı taşımıyor → **ölçülemez** (`KONSINYE_SAHIBI`) |

Ekstre yanıtına eklenenler:
- `bakiyeUyusmazligiTl` (null ise ölçülemez),
- `tlAynaOlculemez` (sebep),
- satır başına `cariTlEtkisi`.

TL farkı 0,01'i aşarsa günlüğe uyarı yazılır. Ekranda ayrı bir TL bandı çıkar (`#cari-detay-uyusmazlik-tl`).

**Kuralın ön ölçümü.** Kod yazılmadan önce aday kural SQL ile `kuyumcu_test_t9`'daki 2.492 bakiye satırına (önceki tam süitin bıraktığı gerçek akışlar) uygulandı.
- Evrak tahsili ayrımı eklendikten sonra kalan 19 farkın hepsi, bakiyeyi ya da hareketi doğrudan yazan test fikstürleriydi.
- Tek istisna konsinye sahibiydi; bu durum "ölçülemez" dalına alındı.

### (4) Bant metni
`Sozlukler.KesifP2.cs` dosyasında TR ve EN değiştirildi.
- `BakiyeUyusmazligi` yeni metni: «… Bakiyenin işlemlerden yeniden hesaplanması gerekir; düzeltme fişi bu farkı kapatmaz. Destekle iletişime geçin.»
- Yeni `BakiyeUyusmazligiTl` (TL bandı) ve `ListeEmanette` anahtarları eklendi.
- `HasEtkisiKural` içindeki «Müşteri listesindeki altın bakiyesi ise emaneti de içeren net tutardır» cümlesi artık yanlıştı. Yerine «liste de aynı (emanetsiz) tutar, emanet ayrı "Emanette" rozetiyle» yazıldı. Bu, dosya alanı dışında zorunlu bir tutarlılık düzeltmesidir (sapma 3).

### (5) Fatura taslak PATCH
- `FaturaKalemi.BirimHasGrami` (`birim_has_grami decimal(15,6) NULL`) eklendi. Ürün yolu kalemlerinde (Olustur ve TaslakKalemEkle) ürünün birim has'ı olarak yazılır.
- `TaslakKalemGuncelle` içinde toplam artık `KalemToplamHas(miktar, birim)` ile hesaplanır. Orantılı yeniden hesap kaldırıldı.
- Birimi bilinmeyen eski kalemde, ilk düzeltmede `eski toplam ÷ eski miktar` bir kez hesaplanıp saklanır.
- Fişten kesilen (defter bağlı) kalemde birim NULL kalır; bu kalemin adedi belgede zaten değişemiyor.

## 2. Sapmalar ve gerekçeleri
1. **Test/koşum ortamı yalıtılmış kopyadır.** Ağaçta aynı anda YASAL-3 şeridi (MASAK yaptırım/saklama: `Yaptirim.cs`, `YaptirimServisi.cs`, `hareketler.masak_olcu_tl`) göçsüz varlıklar ekliyordu. Bu yüzden `kuyumcu_test_t9` üzerinde her veresiye/satış akışı «Table 'yaptirim_eslesmeleri' doesn't exist» hatasıyla düşüyordu. Ayrıca KESIF-P4 test projesi bir süre derlenmiyordu.
   - Testler, mutasyonlar ve canlı API `/private/tmp/kuyumcu-serit/CARI-BAKIYE/kopya` içinde koşuldu. Bu kopya ağacın rsync kopyasıdır (dokumanlar dahil).
   - Kopyaya YALNIZ orada duran geçici bir migration eklendi (`GeciciDigerSerit`: öteki şeridin şeması). Bu dosya proje ağacına **girmedi**.
   - Bu paketin 8 kod dosyası kopya ile ağaçta `cmp` AYNI olarak doğrulandı. Son build ağaçta koşuldu: **0 uyarı, 0 hata**.
2. **Migration'ım öteki şeridin kolonunu yakaladı, çıkardım.** `dotnet ef migrations add` model farkından YASAL-3'ün `masak_olcu_tl` kolonunu da üretti. Kolon; `.cs`, `.Designer.cs` ve `KuyumcuDbContextModelSnapshot.cs` dosyalarından elle çıkarıldı. Böylece o şeridin kendi migration'ı kolonu yine üretecek. Orkestratör birleşimde bakmalı.
3. **Dosya alanı dışında dokunulanlar (gerekçeli):**
   - `RaporModulu.cs`: yalnız `CariYaslandirma` (ekip oyu "raporlar" diyor).
   - `CiktiModulu.cs`: yalnız cari ekstre bakiye satırı (ekip oyu "ekstre" diyor).
   - `SistemCarisi.cs`: sıfır satırı tek yazıcıdan.
   - `EvrakModulu.cs`: yazıcı yönlendirme.
   - `Data/FaturaKalemi.cs`: kolon.
   - `KesifP2Testleri.cs`: 2 assert. Kayıtlı değer artık `kayitliHasGramBakiye` alanından okunuyor; anlam aynı.
   - `Sozlukler.KesifP2.cs` içinde `HasEtkisiKural` cümlesi.
4. **denetim.sh bilinen tohum listesi 3 değil 4.** TL aynasının ilk canlı ölçümü dördüncü bir satır buldu: Demo Kuyumculuk kiracısının «Kurban Musteri» carisi (`01M240M0MV…`).
   - 09.09 21:19 tarihli K1 saldırı gösterimi. Satışı ByGatech kiracısı yazmış, bakiye 77.777 TL.
   - SQL ile ölçüldü: kiracılar arası tek satır.
   - Aynı tohum penceresine ve aynı karar sahibine ait olduğu için "bilinen tohum hatası" notuna eklendi. Bu bir beyaz liste değil: sayaç dördünü de ölçüp basıyor.
   - Kurucu kararı bu satırı da kapsamalı (§ 13).
5. **Kâğıt ekstre etiketi.** «Emanette (bakiyeye girmez)» mevcut desenle aynı yerde, sunucu metni olarak eklendi. Diğer etiketler de ("TL bakiye", "Has gram bakiye") bu dosyada düz metin; sözlüğe taşıma bu paketin dışında.
6. **Disaaktarim CSV** (`Kaynaklar.cs`) değişmedi. Bu bir veri taşınabilirliği dökümüdür ve ham kolonu verir; ekran değildir. Açık kalanlarda.

## 3. Migration
- Ad: **`20260930210357_FaturaKalemBirimHas`**.
- Prosedür: önce build yeşil, sonra `dotnet ef migrations add` (build'li, `--no-build` yok).
- `.cs` 1.090 bayt. `.Designer.cs` 244.277 bayt; öteki şeridin kolonu çıkarıldıktan sonra biraz daha küçük.
- `Up()` iki iş yapar:
  - `AddColumn birim_has_grami decimal(15,6) NULL`,
  - geri doldurma **yalnız TASLAK** için: `UPDATE … JOIN faturalar … SET birim_has_grami = ROUND(has_grami/miktar,6) WHERE durum='TASLAK' AND hareket_id IS NULL AND has_grami IS NOT NULL AND miktar>0`.
- `Down()`: `DropColumn`.
- `dotnet ef database update --connection "…password=$P"` çıktısı: «Applying migration '20260930210357_FaturaKalemBirimHas'. Done.»
- `kuyumcu_net` kanıtı:
  - `__EFMigrationsHistory` son satır = `20260930210357_FaturaKalemBirimHas`;
  - `birim_has_grami decimal(15,6) YES NULL`;
  - durum kırılımı: TASLAK 4/4 dolduruldu · IPTAL 0/4 · ODENDI 0/1 (değişmez belgelere dokunulmadı).

## 4. Testler
Yeni dosya: `Kuyumcu.Tests/CariBakiyeTestleri.cs`. **35 durum** içerir: DB 11 · saf 21 (18'i kural tablosu teorisi) · HTTP 1 · bUnit 2.

| Test | Madde |
|---|---|
| `Emanetli_musteri_liste_detay_uc_ekstre_ozet_AYNI_emanetsiz_has` | 1 (liste = detay = uç = ekstre = özet = 2, emanette 9,16, kayıtlı −7,16) |
| `Emanetsiz_musteri_liste_detay_ayni_ve_kayitliya_esit` · `Stornolu_musteri_liste_detay_ayni` | 1 |
| `Yaslandirma_raporu_emanetsiz_has_okur_yalniz_emanetli_cari_listede_yok` | 1 |
| `Tek_yazici_Local_satiri_bulur_toplar_ve_yuvarlar` | 2 |
| `TL_aynasi_her_akista_sifir` (10 akış) · `TL_aynasi_degisim_musteri_lehine_veresiye_fark` · `TL_aynasi_konsinye_sahibinde_olculemez_uydurulmaz` · `Ekstre_TL_uyusmazligini_ayri_soyler` | 3 |
| `TL_etkisi_kural_tablosu` (18) · `TL_etkisi_veresiye_payi_bilinmiyorsa_null` · `Duzeltme_fisi_bakiye_ve_aynayi_ayni_miktarda_oynatir` | 3 |
| `Uyusmazlik_bandi_duzeltme_fisi_onermez` | 4 |
| `Fatura_taslak_miktar_gidis_donus_has_kaymaz` · `Fatura_taslak_birimsiz_eski_kalem_ilk_duzeltmede_birim_donar` | 5 |
| `CariBakiyeHttpTestleri.Liste_ve_ekstre_JSON_…` (camelCase, cikti belgesi dahil) | 1 + 3 |
| `CariBakiyeEkranTestleri.Liste_emanetsiz_has_ve_ayri_Emanette_satiri_detayla_ayni` · `TL_uyusmazlik_bandi_yalniz_fark_varken_ve_dogru_metinle` | ekran |

**Filtreli koşumlar (kopya):**
- Son hâl `FullyQualifiedName~CariBakiye`: **35 test · 35 geçti**.
- `~CariBakiye|~KesifP2|~CiftBacak`: 121/123. Kırmızı olan ikisi `KesifP2…bakiyeye_esit` (düzeltildi, sonraki koşumda 56/57 içinde yeşil) ve `KarZarar P35_08` (§ 12).

**Tam süit** (kopya, son üretim koduyla; `trx-pCARI-BAKIYE/_Mac_2026-10-01_00_22_07.trx`): **Toplam 8207 · Geçti 8173 · Başarısız 33 · Atlandı 1.**
- 33 kırmızının **1'i** bu paketindi: `KesifP2HttpTestleri.CariEkstre_satir_cari_has_toplami_bakiyeye_esit`. Test, kayıtlıyı `bakiye.hasGramBakiye`'den okuyordu. Test düzeltildi ve filtreli koşumda yeşil.
- Kalan 32'nin sahipleri § 12'de.
- **Bu paketin yeni kırmızısı: 0.**

## 5. Mutasyon tablosu
Yöntem: kopyadaki kaynak `mut-yedek/CARI-BAKIYE/`'ye alındı, `perl -0777` ile tek değişiklik yapıldı, build ve filtreli test koşuldu, kaynak geri kopyalandı ve `cmp` ile doğrulandı. On beşinin hepsi **cmp AYNI**.

| Kod | Değişiklik | Kırmızı testler |
|---|---|---|
| MCBK-1 | liste `hasGramBakiye` = kayıtlı kolon | Emanetli_musteri_liste_detay… · CariBakiyeHttp.Liste_ve_ekstre… |
| MCBK-2 | TAHSILAT yön kuralı → `g.Tl` | TL_etkisi_kural_tablosu(TAHSILAT 2500/0) · (TAHSILAT −2500 storno) |
| MCBK-3 | genel dal `işaret×veresiye` → `g.Tl` | kural_tablosu (ALIS, SATIS ×2, DOVIZ_VERESIYE, AVANS_MAHSUP) · TL_aynasi_degisim… |
| MCBK-4 | avans işareti ters | kural_tablosu (AVANS_ALINDI, AVANS_IADE) · TL_aynasi_her_akista_sifir |
| MCBK-5 | `DegisimVeresiyeFarki` → 0 | TL_aynasi_degisim_musteri_lehine_veresiye_fark |
| MCBK-6 | konsinye kapısı kapalı | TL_aynasi_konsinye_sahibinde_olculemez_uydurulmaz |
| MCBK-7 | yazıcı `Local` araması kaldırıldı | Tek_yazici_Local_satiri_bulur_toplar_ve_yuvarlar |
| MCBK-8 | yazıcı has deltasını düşürür | 6 test (Emanetli…, Emanetsiz…, TL_aynasi_her_akista…, Tek_yazici…, Yaslandirma…, Http) |
| MCBK-9 | ekstre `bakiyeUyusmazligiTl` = 0 | Ekstre_TL_uyusmazligini_ayri_soyler · TL_aynasi_konsinye… |
| MCBK-10 | fatura PATCH orantılı hesaba dönüş | Fatura_taslak_miktar_gidis_donus_has_kaymaz |
| MCBK-11 | eski bant metni ("düzeltme fişiyle giderin") | Uyusmazlik_bandi_duzeltme_fisi_onermez |
| MCBK-12 | liste "Emanette" satırı `@if (false)` | CariBakiyeEkran.Liste_emanetsiz_has_ve_ayri_Emanette_satiri… |
| MCBK-13 | TL bandı kapalı | CariBakiyeEkran.TL_uyusmazlik_bandi_yalniz_fark_varken… |
| MCBK-14 | yaşlandırma kayıtlı kolonu okur | Yaslandirma_raporu_emanetsiz_has_okur… |
| MCBK-15 | kâğıt ekstre kayıtlı kolonu basar | CariBakiyeHttp.Liste_ve_ekstre_JSON… |
| MCBK-16 | (denetim.sh) EvrakModulu'ne doğrudan `TlBakiye +=` satırı eklendi | `✗ CARI_BAKIYE_DOGRUDAN_YAZIM 1 (beklenen 0)` → geri alındı, cmp AYNI |
| MCBK-17 | (denetim.sh) bilinen listeden K1 kurban satırı çıkarıldı | `✗ CARI_BAKIYE_AYNA_UYUSMAZ … YENİ 1` → geri alındı, cmp AYNI |

## 6. denetim.sh
İki yeni sayaç eklendi (`dotnet/denetim.sh`, `>>> PAKET CARI-BAKIYE` bloğu, BASH):
- `CARI_BAKIYE_DOGRUDAN_YAZIM` (statik) şunları sayar: kolon ataması, `new CariBakiye`, `ExecuteUpdate/Delete` ve ham `UPDATE/INSERT cari_bakiyeler`. Tek istisna `Common/CariBakiyeServisi.cs`.
- `CARI_BAKIYE_AYNA_UYUSMAZ` (canlı) `kuyumcu_net`'in tüm kiracılarında has ve TL kural aynasını C# kuralıyla birebir aynı SQL ile ölçer. MySQL erişilemezse "ÖLÇÜLEMEDİ" basar; sessiz yeşil vermez.

Son koşum:
```
  ✓ CARI_BAKIYE_DOGRUDAN_YAZIM (tek yazıcı dışı) 0
  ✓ CARI_BAKIYE_AYNA_UYUSMAZ (canlı, has+TL)      ölçülen 4 · bilinen tohum hatası 4 · yeni 0
  ⚠ CARI_BAKIYE bilinen tohum hatası (NOT)        4 cari · 09.09 Faz 0 öncesi tohum verisi (ByGatech demo 3 + K1 kurban 1) — kurucu kararıyla silinecek
  · CARI_BAKIYE TL aynası ölçülemez cari (bilgi) 10 (Faz 0 öncesi veresiye payı yok ya da konsinye sahibi — uydurulmaz)
```

Betiğin genel sonucu «DENETİM BAŞARISIZ». Bunun nedeni başka şeritlere ait dört ✗ satırı (§ 12):
- `Pages+Bilesenler+Layout record 4 (tavan 3)` ve `P-10 BU_PAKETTE_EKLENEN_RECORD +1`: YASAL-3 `SaklamaBekletmePaneli.razor`.
- `P-02 OLCUM_TABANI_KOPYA`: IKiraciVarlik 74, YASAL-3 yaptırım varlıkları.
- `P-30 DEMO_INSAN_SURESI`: son tarih 30.09 geçti.

Bu paketin sayaçları ✓.

## 7. Canlı doğrulama (API 5188 · vekil 5189 · CDP 9984 · `kuyumcu_test_t9`, API kopyadan)

### Senaryo (46 adım, aynen)
Kurulum ürünün kendi uçlarıyla yapıldı: davet → kayıt → giriş → kur → 2 kasa → müşteri → 3 ürün. Kiracı: `PAKETCARI-BAKIYE-TEST senaryo-2`.

Koşucuya yalnız **ölçüm** eklendi; adımlar ve beklenen deltalar değişmedi. Her adımda şu değişmezler sınandı:
- `bakiyeUyusmazligiTl` = 0 ve ölçülebilir;
- SQL'den bağımsız TL kural aynası = kayıtlı TL;
- liste has = detay has = ekstre `bakiye.hasGramBakiye` = `bakiyeEmanetsizHas`;
- liste/detay emanette = `emanetteHas`;
- liste TL = ekstre TL.

Tek bir alan değişti: "has kolonu" artık `kayitliHasGramBakiye`'den okunuyor.

Sonuç:
- **Ana: 40 `[OK]` · 0 HATA.** «SON değişmezler: TEMİZ · uyuşmazlık 0.0».
- **Ek: 6 `[OK]` · 0 HATA.**
- 46 adımda max |TL farkı| 0, max |has farkı| 0, TL ölçülemez adım 0, max |liste/detay − ekstre| 0.
- Son değerler: TL 1.500 · has (ekran) 0 · kayıtlı −4,58 · emanette 4,58 · liste 0 / 4,58 · kasa1 93.472 · kasa2 103.752 · hurda 10,076.

Kayıtlar: `CARI-BAKIYE/kosum-ana.log`, `kosum-ek.log`, `senaryo/sonuclar-*.json`.

### Üç müşteri: liste = detay = ekstre (API + SQL)
| Müşteri | Liste (TL · has · emanette) | Detay (TL · has · emanette · kayıtlı) | Ekstre (emanetsiz · emanette · has farkı · TL farkı) | SQL `cari_bakiyeler` |
|---|---|---|---|---|
| Emanetli (Senaryo Müşteri) | 1500 · 0 · 4,58 | 1500 · 0 · 4,58 · −4,58 | 0 · 4,58 · 0 · 0 | 1500.00 · −4.580000 |
| Emanetsiz (CB) | 500 · 3,5 · 0 | 500 · 3,5 · 0 · 3,5 | 3,5 · 0 · 0 · 0 | 500.00 · 3.500000 |
| Stornolu (CB: veresiye + emanet stornolu) | 300 · 1,0 · 0 | 300 · 1,0 · 0 · 1,0 | 1,0 · 0 · 0 · 0 | 300.00 · 1.000000 |

Senaryo kiracısının 5 carisinde denetim.sh SQL'i tek başına koşuldu: 5/5 için has farkı 0,000000 ve TL farkı 0,00.

### Fatura taslak PATCH (API + SQL)
Ürün: 22 ayar 1,3478 g, has 1,234585.

| Adım | miktar · has · birim |
|---|---|
| oluştur | 3.000 · 3.703755 · 1.234585 |
| PATCH 1,001 | 1.001 · 1.235820 · 1.234585 |
| PATCH 3 | 3.000 · **3.703755** · 1.234585 |

Gidiş-dönüş kayması **0**. Eski orantılı kural bu adımlarda 3,703756 verirdi.

### Tarayıcı (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma-olc.js`; betik `CARI-BAKIYE/olc.mjs`; tema aydinlik/karanlik)
| Rota | Genişlik | Mod (istenen = gerçek) | Tema | K1 | K2 % | K3 taşan/kırık | K5 TRY | K9 | Yatay |
|---|---|---|---|---|---|---|---|---|---|
| /cariler liste | 1280 · 375 | BASİT | aydınlık | 0 | 98 · 97 | 0/0 | 0 | 0 | 0 |
| /cariler liste | 1280 · 375 | BASİT | karanlık | 0 | 98 · 97 | 0/0 | 0 | 0 | 0 |
| /cariler liste | 1280 · 375 | PRO | aydınlık | 0 | 98 · 97 | 0/0 | 0 | 0 | 0 |
| /cariler liste | 1280 · 375 | PRO | karanlık | 0 | 98 · 97 | 0/0 | 0 | 0 | 0 |
| /cariler detay (emanetli) | 1280 · 375 | BASİT | aydınlık | 0 | 98 · 97 | 0/0 | 0 | 6* | 0 |
| /cariler detay (emanetli) | 1280 · 375 | BASİT | karanlık | 0 | 98 · 97 | 0/0 | 0 | 6* | 0 |
| /cariler detay (emanetli) | 1280 · 375 | PRO | aydınlık | 0 | 98 · 97 | 0/0 | 0 | 6* | 0 |
| /cariler detay (emanetli) | 1280 · 375 | PRO | karanlık | 0 | 98 · 97 | 0/0 | 0 | 6* | 0 |
| /cariler detay + TL bandı (stornolu, TL önbelleği geçici 350) | 1280 · 375 | BASİT/aydınlık · PRO/karanlık | — | 0 | — | 0/0 | 0 | 6* | 0 |

\* K9 örnekleri şunlardır: `th "MÜŞTERİ" ⟂ p.stat-etiket "TL BAKİYE"`. Bunlar detay çekmecesinin (üst katman) altında kalan tablo başlıklarıdır, iki içeriğin çakışması değildir. CIFT-BACAK § 7 ile aynı durum. Jargon örneği "STORNO:" storno satırının sunucu açıklamasıdır; bu paketten önce de vardı.

Sekiz kombinasyonun hepsinde liste satırı ile detay kutusu aynı has'ı gösterdi (`olcum/liste-detay.jsonl`):
- emanetli: «0 has» ve «Emanette 4,5800 has»,
- emanetsiz: «3,5000 has»,
- stornolu: «1,0000 has».

Bant yok (fark 0). Geçici TL bozulmasında bant metni: «Kayıtlı TL bakiyesi ile işlemlerin toplamı uyuşmuyor (fark 50 ₺). Bakiyenin işlemlerden yeniden hesaplanması gerekir; düzeltme fişi bu farkı kapatmaz. Destekle iletişime geçin.» Değer ölçümden sonra 300'e geri yazıldı (SQL ile doğrulandı).

Read ile gözle incelenen görüntüler (`CARI-BAKIYE/olcum/foto/`, `olcum-bant/foto/`):
- `cariler-liste__BASIT_aydinlik_1280.png`: «Senaryo Müşteri 1.500 ₺ · 0 has» ve altında «Emanette 4,5800 has»; diğer iki müşteri 3,5000 / 1,0000.
- `cariler-liste__PRO_karanlik_375.png`: aynı sayılar, "Emanette" satırı sarılı.
- `cariler-detay__BASIT_aydinlik_375.png` ve `__PRO_karanlik_1280.png`: "ALTIN BAKİYESİ 0 has", "EMANETTE 4,5800 has", taşma yok.
- `olcum-bant/foto/cariler-detay__BASIT_aydinlik_1280.png`: TL uyuşmazlık bandı (sarı, çekmecenin sonunda).

### Temizlik
- API (PID 98269) ve vekil (PID 2582) `kill` ile durduruldu. `lsof` 5188, 5189 ve 9984 boş. 5199/5200'e dokunulmadı.
- `kuyumcu_test_t9`'daki `PAKETCARI-BAKIYE%` 14 kiracı (2 senaryo + test süreçlerinin bıraktıkları) `zsh kiraci-temizle-t9.sh <id> --uygula` ile silindi.
  - Kalan: `PAKETCARI%` 0, t9 tenants 0.
  - 2 davet satırı silindi (ROW_COUNT 2).
- `kuyumcu_net`'e hiç kiracı yazılmadı: `PAKETCARI%` 0. Bu veritabanında yalnız migration uygulandı.
- `kuyumcu_net` tenants sayısı başta 15'ti, sonda 14. Aradaki fark başka şeritlerin test kiracılarıdır (son kayıtlar PAKETYASAL3-TEST, PAKETKESIF-P3-TEST). Bu paket o tabloya yazmadı.
- `senaryo/kimlik.json` ve `sistem.anahtar` `printf ''` ile 0 bayta indirildi. Silme kurucuya bırakıldı.

## 8. DoD sözleşmesi
Yeni varlık türü yok, yeni uç yok. Yeni kolon (`birim_has_grami`) mevcut taslak kalemin bir alanıdır.

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Cari bakiye (`cari_bakiyeler`) | salt okunur türetilmiş önbellek; tek yazıcı | Düzeltme, hareket defteri üzerinden yapılır. Uyuşmazlıkta bant "yeniden hesap / destek" der; düzeltme fişini önermez (fiş bakiye ile aynayı birlikte oynatır, farkı kapatmaz). |
| Fatura taslak kalemi | mevcut `PATCH /faturalar/{id}/kalemler/{kalemId}` | Yalnız has hesabı değişti; PATCH sözleşmesi ve yetkiler aynı. |

Finansal tablolarda hard DELETE yok. Yeni ExecuteUpdate/Delete yok (yalnız testlerde fikstür bozmak için kullanıldı). `Hareketler.Add` yok.

## 9. Dokunulan sayfalar
- `Kuyumcu.Web/Pages/Cariler.razor`:
  - liste has hücresinin altında "Emanette" satırı (`#cari-liste-emanette-{id}`),
  - detayda TL uyuşmazlık bandı (`#cari-detay-uyusmazlik-tl`).
- Mevcut id'ler değişmedi. Yeni CSS yok; mevcut `metin-soluk metin-kucuk` ve `bant bant-uyari` sınıfları kullanıldı.

## 10. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Api/Common/CariBakiyeServisi.cs` (yeni)
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`: yazıcı, liste/detay/uç/ekstre/özet, `BakiyeFormat`, `BosBakiye`, `CariHasEtkisi` delege
- `dotnet/Kuyumcu.Api/Modules/Cari/SistemCarisi.cs`: `BosSatir`
- `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs`, `Modules/Evrak/EvrakModulu.cs`, `Modules/Satis/SatisModulu.cs`, `Modules/Satis/StornoServisi.cs`: yalnız yazım satırı
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`: taslak PATCH ve iki oluşturma satırı
- `dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs`: `BirimHasGrami`
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`: `CariYaslandirma`
- `dotnet/Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs`: ekstre bakiye satırı
- `dotnet/Kuyumcu.Api/Data/Migrations/20260930210357_FaturaKalemBirimHas.cs` + `.Designer.cs`, `KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP2.cs`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/CariBakiyeTestleri.cs` (yeni), `dotnet/Kuyumcu.Tests/KesifP2Testleri.cs` (2 assert)
- `dokumanlar/09-uygulama-kayitlari/PAKET-CARI-BAKIYE-uygulama-raporu.md`

Not: ağaç ilk commit'ten beri commit'siz. `git status` bu dosyaların çoğunu `M` ya da `??` gösteriyor. `dotnet/denetim.sh.mutbak` bu pakete ait değil.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| 46 adım yeniden: HATA 0, her adımda has VE TL aynası 0 | KARŞILANDI | 40 + 6 OK; max TL farkı 0, has farkı 0, ölçülemez 0 |
| Liste = detay (canlı, 3 müşteri: emanetli, emanetsiz, stornolu) | KARŞILANDI | § 7 tablo (API + SQL) + tarayıcı 8 kombinasyon |
| Başsız Chrome /cariler liste + detay · 1280 + 375 · BASİT + PRO · açık + koyu · Read ile görüldü | KARŞILANDI | § 7 (16 + 4 ölçüm, 20 görüntü) |
| Tek yazıcı sayacı 0 | KARŞILANDI | denetim ✓ 0 · MCBK-16 |
| TL aynası testleri | KARŞILANDI | 5 DB + 20 saf durum · MCBK-2…9 |
| Fatura PATCH 3 → 1,001 → 3 kayma 0 | KARŞILANDI | test + canlı SQL |
| Her madde için mutasyon kırmızı | KARŞILANDI | 17/17 kırmızı, hepsi cmp AYNI |
| build 0/0 | KARŞILANDI | ağaçta `dotnet build Kuyumcu.sln`: 0 Uyarı, 0 Hata |
| denetim.sh TEMİZ (yalnız bilinen demo notuyla) — tur 2 | KARŞILANDI (paket sayaçları) | § 0.3: iki sayaç ✓. Bilinen notlar ayrıldı: ekip oyu 3 + K-CB-4 1. Genel BAŞARISIZ yalnız başka şeritlerin ✗'leri yüzünden (P-30, B-3/SATIS-4). |
| (tur 1 satırı) denetim.sh | KISMEN | Bu paketin sayaçları ✓. Genel sonuç başka şeritlerin 4 ✗ satırı yüzünden BAŞARISIZ (§ 6, § 12). Bilinen not 3 değil 4 cari (sapma 4). |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 8207 test; bu paketin tek kırmızısı düzeltildi; fark 0 |
| Migration prosedürü (build'li, `database update`, yalnız TASLAK geri doldurma) | KARŞILANDI | § 3 |
| Test kiracısı silindi | KARŞILANDI | § 7 Temizlik |
| COMMIT yok · dosya silme yok · sır basılmadı | KARŞILANDI | — |

## 12. Açık kalanlar
**Başka şeritlere ait (tam süit, 32 kırmızı).**
- YASAL-2/YASAL-3 (tevsik/MASAK mesajı ve yaptırım, sürüyor):
  - AlisOdemeAraci, B4AltinBacagi, SUBEKAPSAMA S3, IadeTestleri, KumulatifKapi K10/K11, L1Tevsik L1H_04, L1bAlisFisi ×3, NakitPayiYazma ×3, P23c, SatisFisi, TevsikIhlalDb ×4, TevsikKapi, TevsikKural ×2, YasalParametreHttp K4_K9 (mesaj metni).
  - DuzeltmePolitikasi.Her_varlik_haritada_VARDIR (YaptirimKaydi/Eslesmesi/SaklamaBekletme).
  - IsCatisiSaf ×2 ve IsSenaryo.Saklama (yeni iş).
  - OlcumKapisi ×2 (IKiraciVarlik 74).
  - PesinMusteri.B3_08 («Az önce bu kasada Peşin Müşteri…» yeni kural).
- Tarih devri (01.10 00:xx):
  - `GorunumSubesiTestleri.K3_Aylik_secili_sube_ve_tum_subeler` (ay değişti, 0 ≠ 1000,50);
  - `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar` (1 Ekim'de "bugün" = "bu ay" dönemi; aynı sorgu yeniden gönderilmez).
- denetim.sh ✗: record tavanı ve P-10 (YASAL-3 `SaklamaBekletmePaneli.razor`), P-02 ölçüm tabanı (IKiraciVarlik 74), P-30 demo süresi (tarih).

**Birleştirme notu.**
- YASAL-3 aynı dosyalarda (`SatisModulu`, `CariModulu`, `MasakKapisi`) eşzamanlı çalışıyor.
- Migration'ımdan çıkarılan `masak_olcu_tl` kolonu o şeridin kendi migration'ında üretilmeli.

**Kapsam dışı (dokunulmadı).**
- ~~`Disaaktarim` CSV "hasGramBakiye" kolonu ham kayıtlı değeri verir~~ → **tur 2'de kapandı** (§ 0.1).
- Konsinye borcunun sahibin defterinde satırı yok: TL aynası bu cariler için "ölçülemez" (canlıda 10 ölçülemez caride sebepler Faz 0 öncesi NULL veresiye ve konsinye).
- ~~Evrak tahsilinin stornosu cariye −TL yazıyor~~ → **tur 2'de kapandı** (§ 0.1, KRİTİK; storno ve ayna ortak kural).
- Kâğıt ekstre etiketleri sunucuda düz metin.
- Demo kiracı yeniden hesap aracı (kök neden § 6.1) bu pakette yazılmadı. Bant yalnız doğru yolu söylüyor.

## 13. Kurucu / mali müşavir
- **Mevzuat:** hüküm yazılmadı. Değişiklikler iç defter tutarlılığıdır; eşik, oran ya da vergi kuralı içermez.
- **Karar K-CB-4 (ekip, 01.10 01:40; Tamer Bey + Taşkın + Mustafa + Mehmet, oy birliği).** Konu: Demo Kuyumculuk «Kurban Musteri» (`01M240M0MV…`, 77.777 TL, has farkı 0). Kararın dayanakları:
  - Satır ekip oyu (3)'ün «bilinen 3 cari» kapsamında DEĞİLDİR.
  - Kaynağı kod hatası değildir: 09.09 21:19 K1 kiracılar arası yazım gösteriminin kalıntısıdır (satışı başka kiracı yazmış).
  - Mehmet (güvenlik): satır bir güvenlik olayının delilidir. Kurucu görmeden silinmez, yeniden hesaplanmaz.
  - Mustafa: veri düzeltmesi kurucu kararıdır.
  - Sonuç: satır denetim.sh'ta **ayrı** `CB_BILINEN_TOHUM_KCB4` listesinde, ayrı NOT satırıyla tutulur. Beyaz liste gibi sessiz değildir: sayaç ölçer ve basar. Kurucu silince betik «listeden çıkarın» uyarısı verir.
  - Kurucuya soru: bu satır silinsin mi, yoksa kiracının defterinden yeniden hesaplansın mı (hareketi yok, sonuç 0 olur)?
- **Kurucu kararı bekleyen (veri):** bilinen 4 tohum carisi (ekip oyu 3 + K-CB-4 1).
  - ByGatech demo: Fatma Şahin, Ali Veli Çelik, Zeynep Koç (has; Fatma ve Ali'de TL de bozuk ama Faz 0 öncesi satırlar yüzünden ölçülemiyor).
  - Demo Kuyumculuk «Kurban Musteri»: 77.777 TL, kiracılar arası tek satır.
  - Silme ya da defterden yeniden hesap kurucu kararıdır. Karar uygulanınca denetim.sh listesinden çıkarılmalı; betik uyuşan satırı "listeden çıkarın" diye uyarır.
