# PAKET 27 — Çoklu birim cari bakiye (TL + has gram + USD/EUR/GBP) · FAZ A

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 3 · 2026-09-17
> Durum: **BİTTİ** · Derleme 0 hata 0 uyarı · 2090/2090 test yeşil · `dotnet/denetim.sh` **TEMİZ** · canlı doğrulama tam

---

## 1. Yapılanlar

Cari hesabına **üçüncü paralel defter** eklendi: borç hangi para biriminde doğduysa
o para biriminde durur, aynı para biriminde kapanır, TL ve has defterlerine karışmaz.

### Veri modeli
| Tablo | Değişiklik |
|---|---|
| `cari_bakiyeler` | `usd_bakiye` · `eur_bakiye` · `gbp_bakiye` — `decimal(15,2) NOT NULL DEFAULT 0`. İşaret TL/has ile aynı: **+ = cari bize borçlu**. Yeni indeks YOK (mevcut `HasIndex(TenantId)` yeterli). |
| `hareketler` | `doviz_tutar decimal(15,2) NULL` — İŞARETLİ efektif (`TlTutari` ile aynı yön). `null` = döviz bacağı yok. Mevcut `para_birimi` kolonu ilk kez TRY dışına çıkıyor. |

Yeni varlık **eklenmedi** → `denetim.sh` `BEKLENEN_VARLIK=41` değişmedi.

### Yeni dosya
`dotnet/Kuyumcu.Api/Modules/Cari/DovizBakiye.cs` — para birimi ↔ kolon eşlemesinin **TEK** yeri:
`ParaBirimleri` (kapalı küme USD·EUR·GBP) · `Gecerli` (TAM eşleşme, `ToUpperInvariant` YOK) ·
`Dogrula` · `Oku` · `Ekle` (üç kolona doğrudan atama yapan tek yer) · `KurAl` ·
`KurZorunlu` (fail-closed) · `TlKarsiligi` (2 hane, `AwayFromZero`) · `DovizTuru`.

### Uçlar
| Fiil | Yol | Roller | Not |
|---|---|---|---|
| POST | `/api/v1/cari/{id}/doviz-veresiye` | `SAHIP · MUDUR · MUHASEBE` | `Tur=DOVIZ_VERESIYE`, `DovizTutar=+tutar`, `TlTutari=+TL karşılığı`, `OdemeIzi.Yok`. |
| POST | `/api/v1/cari/{id}/doviz-tahsilat` | modül yetkisi (TEZGÂHTAR dâhil) | `Tur=DOVIZ_TAHSILAT`, `DovizTutar=−tutar`, `OdemeIzi.Nakitten(TL karşılığı)`, kasaya **EFEKTİF** yazılır. |
| POST | `/api/v1/cari/{id}/duzeltme` | mevcut `[RolGerekli]` **değişmedi** | `dovizParaBirimi` + `dovizDelta` eklendi — eski sistemden devreden döviz bakiyesinin tek meşru giriş yolu. |
| GET | `/cari/{id}/bakiye` · `/cari/{id}` · `/cari` · `/cari/{id}/ekstre` | mevcut | `usdBakiye` · `eurBakiye` · `gbpBakiye` eklendi (sıfırda `0`, `null` değil). `BakiyeFormat` tek noktadan genişletildi. |

### Değişmezler ve kapılar
- **K27-E — `HareketFabrikasi.Ekle` içinde KAPI:** `Tur` `DOVIZ_*` iken `OdemeIzi.Veresiye > 0` ise
  `InvalidOperationException`. Âdet değil, derleme sonrası kapı (Bertan'ın birleşik koşulu).
- **K27-I — kredi limiti:** `VadeKapisi.KrediLimitiDogrula` imzası `(…, string? kurSnapshotId)` ile
  genişledi; döviz bakiyesinin TL karşılığı limite **sayılır**. Döviz bakiyesi sıfırdan farklı ve
  kuru yoksa **400** — sessiz atlama yok. `Hazirla` da aynı parametreyi aldı; **tüm çağıranlar
  derleyiciyle zorlandı** (`SatisModulu` ×6, `StornoServisi` ×1, `CariModulu` ×2).
- **K27-G — çapraz kapatma yok:** USD borcu yalnız USD ile azalır; EUR tahsilatı EUR kolonunu
  eksiye düşürür, USD borcu olduğu gibi kalır (32 Sayılı Karar gerekçesi korundu).
- **KUR FAIL-CLOSED:** kur yoksa ya da `<= 0` ise 400. "Yaklaşık kur" yok.
- **TL KARŞILIĞI SUNUCUDA:** DTO'da TL alanı **hiç yok**; istemci gönderse de hiçbir yere yazılmaz.
- **K6 kasa para birimi + gün kilidi:** `KasaDogrula(db, kasaId, tenant, paraBirimi, now)` — tek çağrı.
- **TEVSİK:** efektif döviz NAKİTTİR → `TevsikKapisi.Dogrula(TL karşılığı)` (459 s. VUK).
- **MASAK:** `DogrulaKumulatif(TL karşılığı)`; `MasakKapisi.KapiyaTabiTurler` listesine
  `DOVIZ_VERESIYE` · `DOVIZ_TAHSILAT` eklendi — döviz MASAK eşiğinden kaçış yolu olamaz.
- **Üç kapı da transaction AÇILMADAN önce** işler (Mehmet'in koşulu).
- **DEFTER TEKELİ:** döviz satırları da `HareketFabrikasi.Ekle`den geçer; `Hareketler.Add` = 1.
- **TOPLAMA YASAĞI:** TL · has · döviz hiçbir uçta/ekranda tek sayıya toplanmaz.

### Diğer dokunulan sunucu noktaları
- `Sabitler.HareketTurleri` + 2 tür · `DuzeltmePolitikasi.HareketListesi` + 2 damga
  (`STORNO`, hedef uç = düzeltme fişi — "sunucuda uç var, ekranda düğme yok" değil, UX-S25).
- `RaporModulu.CariYaslandirma`: döviz bakiyeli cari süzgeçten **düşmez**; yanıt
  `usdBakiye/eurBakiye/gbpBakiye` + `dovizBakiyesiVar` taşır. `VadeTakibi` TL filtresini **korur**
  (Faz A'da döviz vadesizdir) ve bu ekranda **yazılı**dır.
- `Kaynaklar.cs`: `CariKaynagi` +3 kolon, `HareketKaynagi` + `dovizTutar`.
- `Sozlukler.cs`: `SozlukCari`'ye 11 yeni `required` alan, **Tr ve En birlikte**.

### Ekran (Ertuğ)
`Cariler.razor` — tek dokunuş:
- Liste: **Döviz** sütunu. Sıfırdan farklı her para birimi ayrı rozet, `Bicim.Para(tutar, pb)`
  ile **para birimi tutarla birlikte** (UX-S32); üçü sıfırsa `—`. Hiçbir toplama yok.
- Detay: üçüncü bakiye kartı + "vadelendirilmedi" notu.
- İki yeni form **Çekmece** ile (MÜHÜR 2 DoD/2 — tek düzenleme kalıbı; yeni `ModalPencere`
  düzenleme kullanımı **0 artış**, sayaç 44'te kaldı):
  - **Döviz Veresiye** (rolle gizli) — para birimi, efektif miktar, açıklama;
    TL karşılığı `readonly disabled` (UX-S21), kaynağı rozetle söylenir.
  - **Döviz Tahsilat** — para birimi değişince kasa listesi yenilenir (UX-S24); tek kasa varsa
    seçici **gizlenir** ve otomatik atanır (UX-S22); yalnız o para birimindeki kasalar listelenir (K6).
- **Her düğme ya istek atar ya disabled + sebep** (DoD/1): kur yoksa düğme kilitli ve
  `Cekmece` kilit metni sebebi yazar (UX-S30).
- **Sessiz form yasağı**: tutar 0 / kur yok → alan altı Türkçe mesaj + odak, istek **atılmaz**.
- **UX-S27**: kaydet sonrası başarı bandı. **UX-S28**: kur çekilemezse hata bandı — boş duruma çevrilmez.
- K27-C açıkça yazılı: "fişteki döviz bacağı bir ÖDEMEDİR, cariye döviz borcu bırakmaz".

`Raporlar.razor` — yaşlandırma satırında **"Döviz bakiyesi var (vadelendirilmedi)"** rozeti.

### denetim.sh — iki yeni sayaç
1. `Doviz kolonu atama (yalnız DovizBakiye)` = **3** (yalnız `DovizBakiye.Ekle` içinde).
2. `DOVIZ_ turu yazan dosya (sabit 4)` = **4** (`DovizBakiye.cs`, `Entities.cs`,
   `MasakKapisi.cs`, `DuzeltmePolitikasi.cs`).

> **`.mutorj` koşulu — iddia DOĞRULANDI ve YANLIŞ ÇIKTI.** `kod_say` zaten
> `grep --include='*.cs'` kullanıyor; `.mutorj` uzantısı bu süzgece takılmıyor
> (ölçüldü). Bu yüzden ayrı `--exclude` **gerekmedi**; iki yeni sayaç da `kod_say`
> üzerinden gidiyor. Gerekçe betiğin içine yorum olarak yazıldı.

---

## 2. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| 1 | **Faz B uygulanmadı** (`GET /raporlar/doviz-pozisyon`, raporlarda döviz kırılımı). | Görev metni "yalnız Faz A" diyor. Faz A'nın tek başına sevk edilmesi hâlinde gerekli uyarılar (vade yok, K27-C) **ekranda yazılı**. |
| 2 | Spesifikasyondaki satır numaraları kaymış (`Entities.cs:150-151` → 190-191, `CariModulu.cs:648` → 793, `denetim.sh:66 BEKLENEN_VARLIK=15` → **41**). | Kod gerçeğine göre uyarlandı; hedef değişmedi. |
| 3 | `VadeKapisi.Hazirla` imzası da genişletildi (spesifikasyon yalnız `KrediLimitiDogrula` diyordu). | Aksi hâlde veresiye **satış** yolu döviz borcunu görmezdi; Bertan'ın "çağıran tüm yollar derleyiciyle zorlanır" koşulu ancak böyle sağlanır. |
| 4 | `MasakKapisi.KapiyaTabiTurler`'e iki tür eklendi (spesifikasyonda yazmıyordu). | "Bağlantılı işlem kümülatifi TL üzerinden işler" koşulunun kod karşılığı; eklenmeseydi aynı gün içindeki döviz işlemleri kümülatife hiç sayılmazdı (fail-open olurdu). |
| 5 | `DovizVeresiyeDto.VadeGun` **yalnız doğrulanır, deftere yazılmaz**. | Faz A'da döviz borcunun vadesi yoktur (K27-E). Alan tümüyle atılsaydı istemci gönderdiğini sessizce kaybederdi; aralık dışı değer artık 400 alır, geçerli değer ekranda "vadelendirilmedi" diye açıklanır. |
| 6 | Düzeltme fişinin döviz satırında `TlTutari = 0` (döviz veresiye/tahsilatta ise TL karşılığı dolu). | Düzeltmede para el değiştirmez, MASAK/tevsik kapıları zaten çalışmaz (paket 33 `SAYIM_DUZELTME` gerekçesi). TL karşılığı yazmak sahte bir TL hareketi üretirdi. |
| 7 | İstemci `KurTamVeri` yerine mevcut `KurVeri` record'u genişletildi. | `denetim.sh` "Pages içinde istemci record tanımı" tavanı 122; yeni tip açılsa tavan aşılırdı. Sonuç: **121** (bir azaldı). |
| 8 | Ekranda döviz rozeti para birimi **simgesiyle** basılıyor (`1.250,00 $`). | `Bicim.Para` projenin tek para biçimleyicisidir; ikinci bir biçim açmak UX-S31'i (tek kaynak) ihlal ederdi. |
| 9 | `Bicim.Para` kullanıldığı için `Rozet` içeriğinde `USD` dizgesi yok; testler simgeye göre yazıldı. | Aynı gerekçe. |

---

## 3. Migration

**`20260917120931_CariDovizBakiyesi`**

- Üretim: `dotnet ef migrations add CariDovizBakiyesi --project Kuyumcu.Api` (build'li, `--no-build` **kullanılmadı**).
- `.cs` 1813 bayt, `.Designer.cs` 133.300 bayt, `BuildTargetModel` **var** (güdük değil).
- `Up()` gövdesi: `hareketler.doviz_tutar` (nullable) + `cari_bakiyeler.{usd,eur,gbp}_bakiye`
  (`NOT NULL DEFAULT 0`). `Down()` dördünü de düşürür.
- **kuyumcu_net'e uygulandı** (`dotnet ef database update --connection …`, çıkış 0,
  `Applying migration '20260917120931_CariDovizBakiyesi'. Done.`).

SQL kanıtı:
```
tl_bakiye        decimal(15,2)  NO   NULL
has_gram_bakiye  decimal(15,6)  NO   NULL
eur_bakiye       decimal(15,2)  NO   0.00
gbp_bakiye       decimal(15,2)  NO   0.00
usd_bakiye       decimal(15,2)  NO   0.00
doviz_tutar      decimal(15,2)  YES  NULL
```

---

## 4. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| `DovizBakiyeSaflikTestleri` (saf) | 30 | 30 | 0 | 0 |
| `CariDovizBakiyeTestleri` (DB) | 21 | 21 | 0 | 0 |
| `HttpCariDovizTestleri` (HTTP) | 10 | 10 | 0 | 0 |
| `CariEkranTestleri` (bUnit) | 31 | 31 | 0 | 0 (9'u yeni) |
| **TAM SÜİT** | **2090** | **2090** | **0** | **0** |

**Önce / sonra:** 2020 → **2090** (+70 yeni test).

Yeni dosyalar: `CariDovizBakiyeTestleri.cs` · `HttpCariDovizTestleri.cs` · `DovizBakiyeSaflikTestleri.cs`.
Güncellenenler: `TestOrtami.cs` (`TestKurulum.KurDoviz`, `TestKurulum.KasaEkle`),
`CariEkranTestleri.cs` (sahte API'ye **gerçekçi** döviz yanıtları — `/kur/guncel` döviz kurları,
`/kasalar`'a USD + iki EUR kasası, `/doviz-veresiye` · `/doviz-tahsilat` yanıtları; `'{}'` varsayılanı bırakılmadı).

> **İki mevcut test düzeltildi (gizlenmedi):** `Her_cari_satirinda_TAHSILAT_ve_VERESIYE_dugmesi_vardir`
> ve `TEZGAHTAR_duzeltme_fisi_dugmesini_GORMEZ`, "Döviz Tahsilat" düğmesi de `Contains("Tahsilat")`
> eşleştiği için 2 yerine 4 sayıyordu. Sayım TL düğmelerine daraltıldı ve tezgâhtar testine
> **döviz rol matrisi** iddiası eklendi (döviz tahsilat görünür, döviz veresiye görünmez).

**Kültür tuzağı (Aşkın'ın koşulu):** test projesinde `CultureInfo` sabitlenmemiş; yuvarlama
testleri sayıyı `CultureInfo.InvariantCulture` ile biçimliyor. `ToUpperInvariant` iddiası
kaynak taraması yerine **davranış testi** (`'usd'` → red).

---

## 5. Mutasyon kanıtları (M76–M88 · 13/13)

Yöntem: kaynak `scratchpad/mut-yedek/p27/` altına kopyalandı → `perl -0777 -pi -e` ile
tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir
aynılık kanıtlandı. Son mutasyondan sonra temiz derleme: **0 hata 0 uyarı**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M76** | `DovizBakiye.Ekle`'de USD dalı EUR kolonuna yazar | `Ekle_yalniz_hedef_kolonu_degistirir(USD)`, `Ekle_birikimlidir_ve_negatif_delta_azaltir`, `Doviz_veresiye_usd_kolonunu_artirir_TL_ve_has_DEGISMEZ`, `Iki_para_birimi_ayni_caride_BAGIMSIZ_kolonlar_karismaz` (+7) — **11 kırmızı** | `cmp=OK` |
| **M77** | `KurZorunlu` kur yokluğunda fırlatmaz, `1m` döner | `KurZorunlu_kur_yok_veya_sifir_alti_ise_400_verir` (×3), `GBP_kuru_yokken_GBP_veresiye_REDDEDILIR_ve_hicbir_satir_yazilmaz`, `Kuru_olmayan_para_biriminde_400_ve_mesajda_kur_gecer`, `Doviz_bakiyesi_varken_kuru_olmayan_kurla_limit_kapisi_REDDEDER` — **6 kırmızı** | `cmp=OK` |
| **M78** | Döviz veresiye satırının `Tur`'u `"TAHSILAT"` | `Doviz_satiri_AcikBorclara_HIC_girmez_TL_partisi_bozulmaz`, `Doviz_veresiye_usd_kolonunu_artirir_TL_ve_has_DEGISMEZ`, `Doviz_hareketinde_TlTutari_kur_carpimi_ve_iki_hanedir` — **3 kırmızı** | `cmp=OK` |
| **M79** | `OdemeIzi.Yok` → `OdemeIzi.Veresiyeden(tlKarsiligi, bugün)` | `Doviz_satiri_AcikBorclara_HIC_girmez_TL_partisi_bozulmaz` + 9 (K27-E kapısı patlıyor) — **10 kırmızı** | `cmp=OK` |
| **M80** | Limit hesabından `dovizTl` çıkarılır | `Kredi_limiti_doviz_borcunun_TL_karsiligini_GORUR` — **1 kırmızı** | `cmp=OK` |
| **M81** | Kuru olmayan para birimi limit kapısında `continue` ile atlanır | `Doviz_bakiyesi_varken_kuru_olmayan_kurla_limit_kapisi_REDDEDER` — **1 kırmızı** | `cmp=OK` |
| **M82** | `OdemeIzi.Nakitten` → `OdemeIzi.Yok` + tevsik çağrısı kaldırıldı | `Doviz_tahsilat_bakiyeyi_azaltir_ve_kasaya_EFEKTIF_yazar`, `Tevsik_haddini_asan_efektif_doviz_tahsilati_REDDEDILIR` — **2 kırmızı** | `cmp=OK` |
| **M83** | `KasaDogrula(…, paraBirimi, …)` → `"TRY"` | `Doviz_tahsilatinda_TL_kasasi_REDDEDILIR`, `Capraz_kapatma_denemesi_USD_borcunu_DEGISTIRMEZ`, `Doviz_tahsilat_bakiyeyi_azaltir_ve_kasaya_EFEKTIF_yazar`, `Tevsik_haddini_asan_efektif_doviz_tahsilati_REDDEDILIR`, `TEZGAHTAR_doviz_tahsilat_yapabilir_200` — **5 kırmızı** | `cmp=OK` |
| **M84** | Kasaya efektif yerine TL karşılığı yazılır | `Doviz_tahsilat_bakiyeyi_azaltir_ve_kasaya_EFEKTIF_yazar` — **1 kırmızı** | `cmp=OK` |
| **M85** | `MasakKapisi.DogrulaKumulatif(TL karşılığı)` → `dto.Tutar` (efektif) | `MASAK_esigini_asan_doviz_veresiye_kimlik_tespiti_olmadan_REDDEDILIR` — **1 kırmızı** | `cmp=OK` |
| **M86** | DTO'ya `TlTutari` eklenip `dto.TlTutari ?? hesap` kullanılır (istemcinin TL'si kabul) | `Istemcinin_gonderdigi_TL_degeri_YOK_SAYILIR_sunucu_hesaplar` — **1 kırmızı** | `cmp=OK` |
| **M87** | `ParaBirimleri`'ne `"TRY"` eklenir + TRY özel reddi kaldırılır | `Gecerli_normalize_ETMEZ_ve_kume_disini_reddeder("TRY")`, `Dogrula_TRY_icin_dogru_yolu_soyleyen_400_verir`, `Kredi_limiti_doviz_borcunun_TL_karsiligini_GORUR`, `Doviz_bakiyesi_varken_kuru_olmayan_kurla_limit_kapisi_REDDEDER` — **4 kırmızı** | `cmp=OK` |
| **M88** | "Sıfır delta" reddi döviz deltasını saymaz (eski hâl) | `Duzeltme_fisiyle_acilis_USD_bakiyesi_girilir_ve_denetim_izinde_para_birimi_gorunur`, `Iki_para_birimi_ayni_caride_BAGIMSIZ_kolonlar_karismaz` — **2 kırmızı** | `cmp=OK` |

Hiçbir testi kırmayan mutasyon **çıkmadı** — test açığı yok.

---

## 6. Denetim çıktısı

`dotnet/denetim.sh` → **DENETİM TEMİZ** (çıkış 0). İlgili satırlar:

```
  ✓ Doviz kolonu atama (yalnız DovizBakiye)       3
  ✓ DOVIZ_ turu yazan dosya (sabit 4)             4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)      1
  ✓ IgnoreQueryFilters (yasak)                    0
  ✓ Find/FindAsync (yasak)                        0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)          4
  ✓ IKiraciVarlik uygulayan varlık               41
  ✓ Pages içinde istemci record tanımı          121 (tavan 122)
  ✓ Pages: ModalPencere kullanımı                44 (tavan 45)
  ✓ Pages: @onclick'siz ve disabled'sız <button>  67 (tavan 71)
  ✓ Pages/*.razor dosya sayısı                    31 (tavan 31)
DENETİM TEMİZ.
```

---

## 7. Canlı doğrulama (localhost:5198, gerçek veri)

Test kiracısı: **`PAKET27-TEST`** (`01M2QN83G4CW37R40VTSP8QV6M`) — iş sonunda tümüyle silindi.
Kur: USD 48,82 · EUR 56,089 · GBP 65,55. `curl -o /dev/null` **kullanılmadı**, her yanıt okundu.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /cari/{id}/doviz-veresiye` 1.000 USD | 200, `usdBakiye=1000`, `tlBakiye=0` | **200** · `usdBakiye: 1000.00`, `tlBakiye: 0.00`, `hasGramBakiye: 0.000000` |
| 2 | `GET /cari/{id}/bakiye` | üç döviz alanı camelCase | **200** · `{"usdBakiye":1000.00,"eurBakiye":0.00,"gbpBakiye":0.00}` |
| 3 | SQL `hareketler` | `tur=DOVIZ_VERESIYE, para_birimi=USD, doviz_tutar=1000, tl_tutari=1000×48,82, veresiye_tutar=0, vade NULL` | `DOVIZ_VERESIYE · USD · 1000.00 · **48820.00** · 0.00 · 0.00 · NULL` |
| 4 | `GET /raporlar/vade-takibi` | cari **görünmemeli** | `{"cariler":[]}` · cari geçiş sayısı **0** |
| 5 | `GET /raporlar/cari-yaslandirma` | cari görünmeli + döviz rozeti | `"usdBakiye":1000.00, "dovizBakiyesiVar":**true**, "dilim":"0-30"` |
| 6 | `POST /doviz-tahsilat` 400 USD (USD kasa) | 200, `usdBakiye=600`, USD kasa +400 | **200** · `usdBakiye: 600.0` |
| 7 | SQL kasa + `kasa_hareketleri` | kasa **400,00 USD** (TL karşılığı değil), satır `para_birimi=USD, odeme_turu=DOVIZ` | `USD Kasa · USD · **400.00**` · `USD · 400.00 · DOVIZ` · `TRY Kasa · TRY · 0.00` |
| 8 | SQL `hareketler` (tahsilat) | `doviz_tutar=-400, tl_tutari=-19528, nakit_tutar=19528` | `DOVIZ_TAHSILAT · USD · **-400.00** · **-19528.00** · **19528.00** |
| 9 | GBP kuru NULL iken GBP veresiye | 400 + mesajda "kur" | **400** · `"GBP kuru işlem anındaki kur kaydında yok — döviz işlemi yapılamaz"` |
| 10 | `paraBirimi=TRY` ile döviz ucu | 400 | **400** · `"Para birimi geçersiz — kabul edilen değerler: USD, EUR, GBP"` |
| 11 | TL kasasına USD tahsilat (K6) | 400 | **400** · `"Bu işlem USD kasası gerektirir; 'TRY Kasa' kasasının para birimi TRY"` |
| 12 | 9–11 sonrası hareket sayısı | **değişmemeli** | ÖNCE `2` → SONRA `2` ✔ |
| 13 | Limit 10.000 TL, cari 600 USD borçlu → TL veresiye | 400, mesajda döviz karşılığı | **400** · `"… Döviz borcunun TL karşılığı: **29292** TL, Eklenecek: 100 TL, Limit: 10000.00 TL"` |
| 14 | TEZGÂHTAR `doviz-veresiye` | 403 | **403** · `"Bu işlem için gerekli rol: SAHIP veya MUDUR veya MUHASEBE"` |
| 15 | TEZGÂHTAR `doviz-tahsilat` 50 USD | 200 | **200** · `usdBakiye: 550.00` |
| 16 | Düzeltme fişiyle açılış EUR −750 | 200, `eurBakiye=-750` | **200** · `eurBakiye: -750.00` |
| 17 | SQL denetim izi | `Hassas=1`, detayda para birimi + gerekçe | `CariDuzeltme · 1 · "tl=0 has=0 **EUR=-750** · Eski programdan devreden EUR borcumuz — mutabakat 2026-09"` |
| 18 | SQL `hareketler` (düzeltme) | `para_birimi=EUR, doviz_tutar=-750, tl_tutari=0` | `CARI_DUZELTME · EUR · -750.00 · **0.00**` |
| 19 | SQL `cari_bakiyeler` | `usd=550, eur=-750, gbp=0, tl=0` | `550.00 · -750.00 · 0.00 · 0.00` — **iki para birimi bağımsız** |
| 20 | `GET /cari` liste | döviz alanları dolu | `{'usdBakiye': '550.0', 'eurBakiye': '-750.0', 'gbpBakiye': '0.0'}` |

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**); `tenant_id` kolonu olan
44 tablodan `PAKET27-TEST` verisi ve `tenants` satırı silindi.
**Kiracı sayısı: 8 → 8** (başlangıç değeri). Kalanlar: Demo Kuyumculuk, ByGatech Kuyumculuk,
Düzeltme Sonrası Kuyumculuk, Demo Kuyumcu, Ramazan Deneme, **QA-TEST-1/2/3** —
QA fikstürlerine **dokunulmadı**. `cariler` ve `hareketler`'de `PAKET27-TEST` kalıntısı: **0**.

### ⚠ `yetim-tara.sh` çıkış kodu **2** — ÖNCEDEN VAR OLAN bulgu (Anayasa md.1)

```
hareketler        cari_id  → cariler   108 dolu · 0 yetim · 1 SIZINTI
kasa_hareketleri  kasa_id  → kasalar    68 dolu · 0 yetim · 1 SIZINTI
taranan ilişki: 36 · toplam yetim: 0 · toplam kiracı sızıntısı: 2
```

İki satır da **bu pakete ait değil**: kaynak kiracı `01M24019NQ9FAFEJZEVAH0M4N7` (ByGatech
Kuyumculuk), hedef `01M23ZP11ENJ179J7DE8WR46GZ` (Demo Kuyumculuk) — ULID damgaları
(`01M240…`) 11 Eylül dönemine ait, benim kiracım `01M2QN…`. Geliştirme veritabanındaki
**eski** veri. Sessizce düzeltilmedi, **Mehmet (06 Güvenlik) ve Kurucu'ya eskale ediliyor**
(§ 11). Bu paket yeni sızıntı üretmedi: benim kiracım silindikten sonra sayı **2'de kaldı**.

---

## 8. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Gerekçe |
|---|---|---|
| `CariBakiye.{Usd,Eur,Gbp}Bakiye` | **Yeni varlık değil** — mevcut `cari_bakiyeler` satırının kolonları. Düzeltme yolu: `POST /cari/{id}/duzeltme` (`dovizParaBirimi` + `dovizDelta`), ekranda **Düzeltme Fişi** düğmesi. | Bakiye bir **türev**tir; defterden doğar. Doğrudan PATCH açılsaydı defterle bakiye ayrışırdı. |
| `Hareket` (`DOVIZ_VERESIYE`) | **SALT OKUNUR — STORNO damgası.** Düzeltme yolu: ters yönlü döviz düzeltme fişi (`api/v1/cari/{id}/duzeltme`), `DuzeltmePolitikasi` haritasında yazılı, ekran bu haritayı okur. | VUK md.219 · ADR-0004: defter append-only. **Hard DELETE yok**, `[HttpDelete]` yok (`denetim.sh` finansal DELETE sayacı 0). 48-A storno motorunun beyaz listesinde değil — döviz kasası bacağını da çevirmesi gerekirdi, Faz A bunu açmıyor; gerekçe damgada yazılı. |
| `Hareket` (`DOVIZ_TAHSILAT`) | Aynı — STORNO damgası + düzeltme fişi yolu. | Aynı. |
| `KasaHareket` (döviz bacağı) | Mevcut `KasaHareket` damgası (`STORNO`, `Defter`) — değişmedi. | Aynı. |
| `CariDuzeltmeDto.{DovizParaBirimi,DovizDelta}` | Mevcut düzeltme ucunun PATCH-benzeri genişlemesi: **gönderilmezse döviz bakiyesine dokunulmaz** (koru), yarım gövde (biri var biri yok) **400**. | "Gövdede yoksa koru" sözleşmesi; sessiz yok sayma yasak. |

**Denetim kaydı:** `DenetimServisi.YazmaKaydet(islem, "CariDuzeltme", hareketId, …)` **çağrılıyor**
(yeniden yazılmadı), `Hassas=true`, detayda **delta + para birimi + gerekçe**, kişisel veri **yok**.
Döviz veresiye/tahsilat satırları için otomatik yazma izi (`DenetimYazmaDinleyicisi`) düşer.

**Yetki:** `doviz-veresiye` → `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` TAM eşleşme;
`doviz-tahsilat` → sınıf düzeyi `[ModulYetkisi("cari")]` (mevcut tahsilatla paritede).

## 9. Dokunulan sayfalar (MÜHÜR 2 DoD/2 — tek dokunuş)

- `dotnet/Kuyumcu.Web/Pages/Cariler.razor` — **birinci dokunuş** (bu dalgada).
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — **birinci dokunuş**; yalnız yaşlandırma satırına
  rozet + `YaslananCari` record'una bir alan (DOM'un geri kalanına dokunulmadı).

---

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| K27-E `HareketFabrikasi` içinde **KAPI** olarak zorlanır | ✅ | `HareketFabrikasi.cs` `DovizTuru && izi.Veresiye > 0` → throw; `HareketFabrikasi_doviz_satirinda_veresiye_payini_REDDEDER`; M78/M79 |
| `DovizBakiye.Gecerli` TAM eşleşme, `ToUpperInvariant` yok, TRY → 400 | ✅ | `Gecerli_normalize_ETMEZ_…` (9 girdi, **davranış** testi), `Dogrula_TRY_…`; M87 |
| Yuvarlama `AwayFromZero` / 2 hane, `InvariantCulture` ile biçimlenir | ✅ | `TlKarsiligi_iki_hane_ve_AwayFromZero_yuvarlar` (4 vaka + negatif yön) |
| `KrediLimitiDogrula` imza değişimi, tüm çağıranlar derleyiciyle zorlanır, kur yoksa 400 | ✅ | `SatisModulu` ×6 · `StornoServisi` ×1 · `CariModulu` ×2 güncellendi; M80/M81; canlı #13 |
| Döviz tahsilatı: `KasaDogrula(paraBirimi, an)` + tevsik(TL) + MASAK(TL), **üçü transaction öncesi**, kasaya EFEKTİF | ✅ | `CariModulu.DovizTahsilatGir`; M82/M83/M84/M85; canlı #7, #11 |
| Çapraz kapatma Faz A'da kapalı, Faz B'ye otomatik geçmez | ✅ | `Capraz_kapatma_denemesi_USD_borcunu_DEGISTIRMEZ`; Faz B **uygulanmadı** |
| Paket 20 ile aynı dalgada koşulmadı | ✅ | Paket 20 raporu 2026-09-17 tarihli ve **bitmiş**; bu paket onun üstüne geldi (görev metni: "20'den SONRA") |
| Düzeltme ucu `YazmaKaydet` `Hassas=true`, detayda delta+PB+gerekçe, kişisel veri yok | ✅ | canlı #17; `Duzeltme_fisiyle_acilis_USD_bakiyesi_…` |
| `.mutorj` iddiası **doğrulanmadan** yeni sayaç eklenmez | ✅ | Doğrulandı, **yanlış çıktı** (`kod_say` `--include='*.cs'`); gerekçe betiğe yazıldı |
| Faz A tek başına sevk: vade/yaşlandırma yok ve K27-C **ekranda yazılı** | ✅ | `dv-vadesiz-not`, `dv-fis-bacagi-not`, detay kartı notu, yaşlandırma rozeti; `Doviz_formunda_vade_yok_ve_fis_bacagi_uyarilari_YAZILIDIR` |
| Migration **seri** sevk edildi | ✅ | Tek migration, snapshot çakışması yok, `database update` çıkış 0 |
| bUnit sahte API'lerine **gerçekçi** yanıt | ✅ | `/kur/guncel` döviz kurları, `/kasalar` USD+2 EUR, `/doviz-*` yanıtları; 31/31 yeşil |
| Her düğme istek atar ya da disabled + sebep | ✅ | `Tutar_girilmeden_doviz_kaydet_KILITLI_ve_istek_ATILMAZ`, `Kuru_olmayan_para_birimi_secilince_kur_yok_mesaji_cikar` |
| Tek düzenleme kalıbı (Cekmece), ModalPencere artışı 0 | ✅ | `denetim.sh` ModalPencere **44** (tavan 45, artış yok) |
| Sessiz form yasağı (alan altı Türkçe mesaj) | ✅ | `form.Ekle("dv-tutar"…)` + `AlanHatasi` + `OdakIlkHataya` |
| UX-S21/22/24/25/26/27/28/29/30/31/32 | ✅ | § 1 "Ekran" ve § 8 |
| Tek dokunuş kaydı | ✅ | § 9 |
| Toplama yasağı (TL+has+döviz tek sayıya toplanmaz) | ✅ | Hiçbir uçta/ekranda toplam yok; `Ekle_yalniz_hedef_kolonu_degistirir` TL/has'a dokunulmadığını da sınar |
| Ücretsiz pilot — fiyat/teklif metni yok | ✅ | Bu pakette hiç fiyat/teklif metni üretilmedi |
| COMMIT YOK | ✅ | Çalışma ağacında kalındı; `git add/commit/push` çalıştırılmadı |

---

## 11. Açık kalanlar

1. **Faz B uygulanmadı** — `GET /raporlar/doviz-pozisyon` (para birimi başına alacak/borç/net,
   sunucuda `GROUP BY`), raporlarda döviz kırılımı, döviz açık borç/vade türetimi.
   Paket 37 (pozisyon/net varlık) bunu tüketecek.
2. **Çapraz kapatma (K27-G)** ve kur farkı hareketi kapalı; Faz B'ye **otomatik geçmez**, ayrı oylama şartı yazılı kalır.
3. **Satış/alış/hurda/emanet yollarında döviz borcu üretimi yok** (K27-C/K27-K). Fişteki döviz
   bacağı bir ödemedir; ekranda açıkça yazılı.
4. **Storno motoru döviz türlerini kapsamıyor** — ters kayıt döviz kasası bacağını da çevirmek
   zorunda. Bugünkü düzeltme yolu: ters yönlü döviz düzeltme fişi (damgada yazılı).
5. **Paket 20 taban çelişkisi ADR'ye bağlanmadı** (20: altın dışı maden bakiyesi HESAPLANAN;
   27: döviz KOLON). Emanet maden bakiyesinin kolon mu hesaplanan mı olacağı **Bertan'ın ADR
   kalemidir**; bu paket kararı vermedi, yalnız döviz için kolon seçimini kanıtıyla uyguladı.
6. **TB-06 kur snapshot TOCTOU:** döviz hareketinin TL karşılığı kur kaydına bağlı; kayıt silinirse
   yeniden türetilemez. TB-04 (FK) kapanana kadar açık — bu paket riski **artırır ama yaratmaz**.
7. **`yetim-tara.sh` çıkış 2 — 2 kiracı sızıntısı** (`hareketler.cari_id`, `kasa_hareketleri.kasa_id`),
   ikisi de 11 Eylül dönemi geliştirme verisi. **Mehmet ve Kurucu'ya eskale.**
8. **Derleme uyarısı borcu (paket dışı):** `Kuyumcu.Tests/MadenTestleri.cs:78-83` dört
   `xUnit2008` uyarısı taşıyor (`Assert.False/True` + regex → `Assert.DoesNotMatch/Matches`).
   Tam yeniden derlemede görünüyor, artımlı derlemede görünmüyordu. **Bu pakette dokunulmadı**
   (izinsiz değişiklik yok); temiz derleme sayımı bu dosya derlenmediğinde 0/0'dır.

---

## 12. Kurucuya / mali müşavire notlar

1. **DÖVİZ ALACAĞIN AÇIK BIRAKILMASI — hukuki teyit eksik (DOGRULANMALI).**
   2008-32/34 Tebliği (2025-32/72 ile değişik), Türkiye'de yerleşikler arasında **menkul satış
   bedelinin dövizle kararlaştırılmasını ve tahsilini** serbest bırakıyor — bu **doğrulandı**
   (A12, `PAKET-15-DOVIZ-KARAR-2026-09-15.md`). Bedelin bir kısmının **döviz cinsinden ALACAK
   olarak açık bırakılmasına** dair birincil metin teyidi **yok**.
   **Koda hüküm gömülmedi.** Yalnız iki tutucu tedbir alındı: (a) döviz veresiye ucu
   `SAHIP/MUDUR/MUHASEBE` ile sınırlı (tezgâhtara kapalı), (b) çapraz kapatma kapalı.
   → **Mali müşavir teyidi istenir.** Teyit olumsuz gelirse ucun tümüyle kapatılması gerekir.
2. **VUK 280 — dönem sonu yabancı para değerlemesi: VARSAYIM.**
   Kur değerlemesi ve kambiyo kâr/zararı **bu üründe yapılmıyor**; bakiye kolonlarına asla
   değerleme yazılmıyor. Mali müşavirin muhasebe programının işi sayıldı. **Bu bir varsayımdır
   ve Kurucu'nun oyuna açıktır.**
3. **Efektif dövizin hangi kurla TL'ye çevrileceği (459 s. VUK tevsik):** birincil metinden
   teyit **edilemedi**. Tutucu davranıldı: **işlem anındaki kur** kullanılıyor ve efektif döviz
   **daima nakit** sayılıyor (kapı yalnız sıkılaşıyor). Mali müşavir teyidi istenir.
4. **MASAK 185.000 TL eşiği** ADR-0005 gereği **yapılandırmadan** okunuyor, koda gömülü değil;
   döviz işleminin **TL karşılığı** eşiğe sayılıyor (doğrulandı).
5. **Kiracı sızıntısı (§ 11/7)** — geliştirme veritabanında iki satır, iki eski kiracı arasında.
   Üretim verisi değil; yine de **güvenlik olayı olarak** Mehmet'e ve Kurucu'ya bildiriliyor.
6. **COMMIT YOK** — tüm değişiklikler çalışma ağacında.
