# PAKET ŞUBE-KAPSAM-A — Seçili şube görünümü, SUNUCU tarafı — Uygulama raporu

> Tarih: 2026-09-30 · Şerit: SUBE-YASAL23 / t2 (API 5162 · vekil 5163 · CDP 9905-9906 · test DB `kuyumcu_test_t2`)
> Rol: Taşkın (backend) + Ertuğ (frontend regresyon ölçümü) · Uygulayıcı: Opus · COMMIT YOK
> Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-SUBE-KAPSAM-2026-09-30.md` (yalnız ŞK-A: § 4.1, § 4.2, § 2.6)

## 1. Özet

- **§ 2.6 YASAL BULGU DOĞRULANDI ve KAPATILDI (en yüksek öncelik).** Paket öncesi ağaçta yazılan HTTP testleri KIRMIZI verdi:
  B şubesinde aynı müşteriye 20.000 TL nakit varken A şubesindeki TEZGÂHTAR 15.000 TL nakit satışı **200** aldı (tevsik delindi);
  B'de 100.000 TL + A'da 90.000 TL kimliksiz satış **200** aldı (MASAK delindi); `/tevsik/gunluk` önizlemesi A'daki tezgâhtara 0 gösterdi.
  Düzeltmeden sonra aynı testler yeşil; canlıda (5162) iki şube + iki ayrı TEZGÂHTAR ile ölçüldü → ikinci işlem **400**, `hareketler`'e yeni satır **0**.
- **Görünüm şubesi tek yardımcı:** `Common/GorunumSubesi.cs` (`Coz` + `Daralt`). 15 okuma ucu bu yardımcıya bağlandı; 13 elle yazılmış sınıf-b `SubeId ==` satırı yardımcıya taşındı (sınıf b: **23 → 11**).
- **Hurda birleşik görünüm:** şubesiz SAHİP/MÜDÜR `/hurda/bakiye` artık **200** (eskiden çok şubeli kiracıda 400) + `subeler[]` kırılımı; kırılım toplamı = birleşik toplam (tek türetme).
- **Tercih:** `gorunum-subesi` anahtarı (migration YOK); yazarken ve okurken doğrulanır; geçersiz tercih "Tüm şubeler"e değil **oturum şubesine** düşer (`gecersizTercih: true`).
- Build 0 hata / 0 uyarı (derleyici); denetim.sh **TEMİZ**; 30 yeni test yeşil; 8 mutasyon ailesi (10 koşum) kırmızı → geri alındı → `cmp` AYNI.
- **TUR 2 (doğrulayıcı bulguları, § 16):** M2-1 sayısı yeniden ölçüldü — iki rapor aynı gerçeği yazar (sınıf filtresinde **8/24**, K1|K3 filtresinde **7/9**; fark filtre kapsamı). DÜŞÜK bulgu "ortam transaction'ı" not yerine **kodla kapatıldı**: okuyucu çağıranın bağlantı + transaction'ına katılır; yeni test + M4-4 mutasyonu (paylaşım kapatılınca 27.000 beklenirken **20.000** → KIRMIZI). Tam süit 7890 · 7888 geçti · 1 başarısız (başkasına ait) · 1 atlandı; canlı K-10/K-11/K-3/K-4/K-5 yeniden ölçüldü.

## 2. Yapılanlar (sunucu)

### 2.1 Yasal kümülatif (§ 2.6 / § 4.2.5)
| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Api/Data/SistemKapsami.cs` | İzinli kullanım listesine **madde 10** (yasal kümülatif okuyucu) + `YasalKumulatifOku(db, tenantId, oku)`: şube süzgeci KAPALI bağlamda sorgu aynen `db` ile; AÇIK bağlamda `Kiraci(tenantId)` kapsamı (kiracı süzgeci var, şube süzgeci yok) + `NoTracking`. Fail-closed: kiracı ≠ oturum kiracısı → istisna; `SistemKapsami` çözülemez → istisna; açılan kapsamda şube süzgeci/sistem modu → istisna; değişiklik izlenirse → istisna. |
| `Modules/Tevsik/TevsikKapisi.cs` (yalnız `GunlukNakitToplam`) | Şube süzgeci açıksa `YasalKumulatifOku` üzerinden kiracı geneli okuma. |
| `Modules/Masak/MasakKapisi.cs` (yalnız `GunlukIslemToplam`) | Aynı düzeltme (defter + banka dekontu + storno süzgeci aynı kapsamda). |
| `dokumanlar/02-mimari/adr/0002-sistem-kapsami-tek-kacis-yolu.md` | "Ek not — 2026-09-30" bölümü (bağlam, mevzuat, karar, güvenlik koşulları, Mehmet onayı notu). |

`IgnoreQueryFilters` sayısı 0 kaldı (denetim `P-04: IGNORE_QUERY_FILTERS 0`). Önizleme ucu `/tevsik/gunluk` ve MASAK önizlemesi aynı fonksiyonları çağırdığı için onlar da düzeldi.

### 2.2 Görünüm şubesi yardımcısı (§ 4.1 / § 4.2.1)
`Kuyumcu.Api/Common/GorunumSubesi.cs` (YENİ):
- `Coz(db, islem, istenen)`: kilitli rol → boş = oturum şubesi, başka şube ya da `TUMU` = **403** "Bu şubeyi görme yetkiniz yok."; kilitli rolde oturum şubesi yoksa **400** (fail-closed, birleşiğe düşmez). Tüm-şube rolü → boş/`TUMU` = `null` (birleşik); dolu → kiracıda **aktif** şube değilse **404**.
- `Daralt<T>(subeId) where T : ISubeVarlik` — tek `SubeId ==` satırı.
- `Kapsam(subeId)` → `"SUBE"` / `"TUM_SUBELER"`; `KiraciGeneli = "KIRACI_GENELI"`.

### 2.3 Uçlar (§ 4.2.2)
| Uç | Durum |
|---|---|
| `GET /raporlar/gunluk`, `/raporlar/aylik` | YENİ `?subeId=`; hareket + kasa (aylıkta kasa girişleri şubenin kasalarından) daraltılır; aylıktaki kâr-zarar özetine şube geçer; yanıtta `gorunumSubesi`, `kapsam`. |
| `GET /raporlar/pos-fatura-mutabakati` | YENİ `?subeId=`; hareket, kasa girişi (kasa→şube), gider pusulası, POS daraltılır. **Fatura kiracı genelidir** (şube taşımaz — mimari red korunur): şube görünümünde FARK yalnız şubenin fişlerine bağlı faturalarla hesaplanır; bağsız faturalar `subeyeAtanamayanFaturaAdet` + uyarı metniyle SÖYLENİR; `faturaKapsami: "KIRACI_GENELI"`. |
| `GET /hurda/bakiye`, `/hurda/defter` | YENİ `?subeId=`; boş + tüm-şube rolü = **birleşik** (400 kalktı). Bakiye türetmesi yine TEK yerde (`HurdaStokKapisi.Bakiyeler`): birleşik = şube başına aynı türetmenin maden bazında toplamı → K-4 yapısal. `OturumSubesiCoz` yalnız YAZMA yolunda kaldı. |
| `GET /pos/tahsilatlar`, `/pos/valor-takvimi` | YENİ `?subeId=` (tahsilat yanıtında `kapsam`). |
| `GET /emanet/{cariId}` | YENİ `?subeId=` (satırın şubesi = hareketin şubesi; `mevcutHasGramBakiye` da daraltılır). Yazma yolundaki emanet neti değişmedi. |
| `GET /urunler/stok-adetleri` | YENİ `?subeId=`. |
| Mevcut `subeId` alan uçlar: `/kasalar`, `/raporlar/stok-degeri`, `/reyon-stok`, `/stok-yaslandirma`, `/kar-zarar`, `/kar-zarar/urun`, `/stok-degerlemesi`, `/pos/cihazlar`, `/raporlar/islem-defteri`, `/odeme-araci`, `/personel` | Hepsi `Coz`'a bağlandı (pasif/yabancı şube 404, kilitli rol başka şube 403 — tek davranış). |
| `/raporlar/cari-yaslandirma`, `/raporlar/vade-takibi` | `subeId` yok sayılır; yanıta `kapsam: "KIRACI_GENELI"` eklendi. |

### 2.4 Tercih (§ 4.2.4)
`Modules/Tercih/TercihModulu.cs`: beyaz listeye `gorunum-subesi`. PATCH'te değer yazılmadan önce doğrulanır (kilitli rol `TUMU`/başka şube → 403; pasif/yabancı → 404; boş → 400; `tumu` → `TUMU`). GET ve PATCH yanıtına `gorunumSubesi: {kayitli, subeId, tumSubeler, gecersizTercih, deger}` eklendi. `tumSubeler` sunucudaki tek kaynaktan (`SubeYetkisi`) gelir — § 4.3.5'teki istemci rol kopyasının kaldırılması için SK-B bu alanı okuyacak. Denetim kaydı mevcut `YazmaKaydet` çağrısıyla düşer (değişmedi). PATCH sözleşmesi: gönderilmeyen anahtar korunur, `null` siler (test: `Tercih_gecersiz_degerler_yazilmaz`).

## 3. Sapmalar ve gerekçeleri

1. **`Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs` (şerit dosya listesinde YOK) — ek alan.** § 4.2.2 ve K-4 birleşik yanıtta `subeler[]` kırılımı ister; `HurdaBakiyeYaniti` bir Contracts tipidir (P-03: anonim yanıt yasak). Kayda **sona, varsayılanlı iki isteğe bağlı parametre** (`Kapsam`, `Subeler`) ve `HurdaSubeKirilimi` kaydı eklendi — geriye uyumlu, istemci derlemesi ve mevcut ekran etkilenmez (tam derleme 0 hata). Kırılım şeması `{subeId, ad, satirlar[maden]}`: şartnamedeki tek `hasBakiye` yerine maden bazlı satırlar, çünkü kod tabanı madenleri toplamayı yasaklar (Paket 20 — "100 g altın + 500 g gümüş = 600 g" yanlışı).
2. **`Kuyumcu.Tests/ZOISLEMASubeTestleri.cs` — iki testin OKUMA beklentisi güncellendi.** Bu testler şubesiz MÜDÜR'ün çok şubeli kiracıda `/hurda/bakiye` okumasında **400** bekliyordu; şartname § 4.2.2 / K-4 bu davranışı açıkça kaldırır. YAZMA yolu beklentileri (hurda satışında 400, tek şubede o şubeye yazma) **aynen korundu**. Kilitli rolde şubesiz okuma yine 400 (Coz fail-closed) — `ZO_subesiz_TEZGAHTAR_tek_subede_bile_COZULMEZ_400` değişmeden yeşil.
3. **Kasa-hareketi şube kırılımı:** `kasa_hareketleri`'nde `sube_id` yok; şube, `Kasalar.Daralt(sube)` kimlik listesiyle türetildi (mevcut `SatisRaporlari.OdemeAraci` deseni). Liste `Take(RaporFiltresi.EnFazlaLimit)` ile sınırlı (TB-19 sayacı artmadı).
4. **§ 4.2.3 "kiracı geneli uçlar `kapsam` alanı":** yalnız şerit dosya alanımdaki `/raporlar/cari-yaslandirma` ve `/raporlar/vade-takibi`'ye eklendi. `/cariler*`, `/faturalar*`, `/bankalar*`, `/masak/*`, `/tevsik/*` dosyaları şerit alanında DEĞİL (Cari/Fatura/Banka/Masak/Tevsik modülleri); `subeId` zaten yok sayılıyor (ASP.NET bilinmeyen sorgu parametresini bağlamaz) — K-11 testi `/tevsik/gunluk?subeId=` için bunu kanıtlar. `kapsam` alanı SK-B rozetinin ihtiyacı olunca ayrı kalem (açık kalanlar).
5. **Kilitli rol `Coz` sonucu:** boş istekte oturum şubesiyle `Daralt` uygulanır. Etkilenen tüm varlıklarda (`Hareket`, `Kasa`, `Stok`, `PosCihaz`, `PosTahsilat`) `sube_id` NOT NULL olduğu için P-04 süzgecinin "`sube_id IS NULL` = kiracı geneli" dalı bu uçlarda satır üretmez; davranış farkı yok (SubeIzolasyon 13 test yeşil).

## 4. Migration
**Gerekmedi.** Tercih anahtar→metin deposu (`kullanici_tercihleri`) mevcut; yeni tablo/kolon/indeks yok (şartname § 4.2.4 ile aynı; TB-19: ölçülmeden indeks eklenmedi).

## 5. Mevzuat
Bu paket yeni eşik/oran/had YAZMADI; kodda hiçbir rakam sabitlenmedi (eşikler mevcut `YasalParametreServisi`/yapılandırmadan gelir). Kümülatifin **şubeye bölünmemesi** şartname § 3'teki birincil kaynaklara dayanır:
- 459 s. VUK GT §4.1.1 — https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm — "Aynı günde aynı kişi veya kurumlarla yapılan işlemlerin toplam tutarının"
- MASAK Tedbirler Yön. md.5/1-b — https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf — "birbiriyle bağlantılı birden fazla işlemin toplam tutarı"
- MASAK GT 34 md.3/1-c — https://www.mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=46302&mevzuatTur=Teblig&mevzuatTertip=5 — şube yükümlünün birimidir.

Yön **tutucudur** (yükümlü aleyhine hata yapmaz: kapı yalnız SIKILAŞTI).

## 6. DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| `kullanici_tercihleri` · `gorunum-subesi` (yeni anahtar, yeni varlık DEĞİL) | `PATCH /tercihler/benim` (gönderilmeyen = koru, `null` = sil) · `DELETE /tercihler/benim/gorunum-subesi` | `[HerkeseAcik]` (kendi tercihi; kimlik jetondan). Denetim: mevcut `DenetimServisi.YazmaKaydet`. Finansal değil. |
| Yeni varlık | YOK | Paket yalnız okuma daraltması + yasal okuma düzeltmesi. Finansal tabloya yazma yolu eklenmedi. |
Ekran (Düzenle/Pasife al) SK-B paketinde; bu pakette ekrana dokunulmadı (Web/razor/css/Sozlukler değişmedi).

## 7. Testler

### 7.1 Yeni testler (30 koşum — 23 `[Fact]` + 1 `[Theory]` × 8 satır − birleşik)
`Kuyumcu.Tests/KumulatifKapiSubedenEtkilenmezTestleri.cs` (6): K10 tevsik 400 + satır 0 · K10 önizleme 20.000 · K10 kontrol eşik altı geçer · K10 MASAK 400 + satır 0 · K11 SAHİP `?subeId=Merkez` yine kiracı geneli (önizleme + yazma kapısı) · kiracı-geneli okuma başka kiracının satırını saymaz.
`Kuyumcu.Tests/GorunumSubesiTestleri.cs` (24): K1/K2 günlük + SQL · K3 aylık · K3 hurda bakiye · K3 hurda defter · K3 POS tahsilat/valör/cihaz · K3 kasalar · K3 stok adetleri · K3 emanet · K3 mutabakat · K4 şubesiz SAHİP hurda 200 + kırılım = birleşik · K5 tablo güdümlü 8 uç (403/403/200/200) · K5 kilitli rol kendi şubesi · K6 pasif/yabancı şube 404 (4 uç) · kiracı geneli uçlar `KIRACI_GENELI` · tercih yaz/oku/TUMU · tercih pasif şube → oturum şubesi · tercih geçersiz değerler.

**Paket öncesi (düzeltmesiz) koşum — § 2.6 kanıtı:** `KumulatifKapiSubedenEtkilenmez` 6 testten 3'ü KIRMIZI (tevsik 400 beklenirken 200 · MASAK 400 beklenirken 200 · önizleme 20.000 beklenirken 0). Günlük: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-A/t-k10-once.log`.

### 7.2 Filtreli koşumlar
| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Yeni iki sınıf | 30 | 30 | 0 |
| ZOISLEMASube + GorunumSubesi + Kumulatif + SubeIzolasyon + Tercih | 86 | 85 | 1 (`KATMAN2MenuTercihEkranTestleri…` — bUnit/Web ApiIstemci istek fırtınası; Web'e dokunulmadı) |
| Hurda/Pos/Rapor/Tercih/Tevsik/Masak/Emanet/Kasa/Sube/Stok/KarZarar/Pesin/Storno aileleri | 1949 | 1937 (+1 atlandı) | 11 → ZO×3 bu pakette uyarlandı (§ 3.2); kalan 8 bUnit/Web ekran testi (aşağıda) |

### 7.3 Tam süit
7875 toplam · 7870 geçti · 4 başarısız (başkasına ait Web/bUnit, § 12) · 1 atlandı. Bu paketin ürettiği yeni kırmızı: 0.

## 8. Mutasyon tablosu
Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/SUBE-KAPSAM-A/` altına kopya → `perl -0777 -pi -e` tek eşleşme → derleme (M6-1'den itibaren paralel şerit yarışını önlemek için özel çıktı dizini `-o …/out`) → filtreli test → yedek geri → `cmp` AYNI.

| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M1-1 | `Coz` kilitli dalda doğrulama kaldırıldı (`if (false)`) | `K5_TEZGAHTAR_baska_subeyi_isterse_403_bos_ise_200` ×8 uç, `Tercih_gecersiz_degerler_yazilmaz` | cmp AYNI |
| M2-1 | `Daralt` `==` → `!=` | `K1_Gunluk_…`, `K3_Aylik…`, `K3_Kasalar…`, `K3_Urun_stok_adetleri…`, `K3_Emanet…`, `K3_Pos_fatura_mutabakati…`, `K3_Pos_tahsilatlar_ve_valor_takvimi…` (test güçlendirildi: ilk koşumda POS tahsilat testi sayıyla yetiniyordu → cihaz adı doğrulaması eklendi, tekrar koşumda KIRMIZI), `K5_TEZGAHTAR_bos_subeId_ile_yalniz_kendi_subesini_gorur` | cmp AYNI |
| M3-1 | `/raporlar/gunluk` ucu `subeId`'yi servise geçirmiyor (`null`) | `K1_Gunluk_secili_sube_toplami_SQL_ile_ayni_K2_tum_subeler_toplami`, `K6_Pasif_sube_ve_baska_kiracinin_subesi_404` | cmp AYNI |
| M4-1 | Tevsik kümülatifi yeniden istek bağlamı `db` ile (SistemKapsami dalı kapatıldı) | `K10_Tevsik_B_subesindeki_nakit_A_subesindeki_TEZGAHTAR_kapisina_sayilir_ikinci_satis_400`, `K10_Tevsik_onizleme_ucu_TEZGAHTAR_A_icin_B_subesi_nakdini_toplar` | cmp AYNI |
| M4-2 | MASAK kümülatifi yeniden istek bağlamı `db` ile | `K10_Masak_B_subesi_100bin_arti_A_subesi_90bin_kimliksiz_400` | cmp AYNI |
| M4-3 | `YasalKumulatifOku` her zaman gelen `db` ile okur | Test ana işlemi **Stack overflow** ile düştü (sonsuz özyineleme; K10 testleri geçemedi — koşum durduruldu). Sessiz yeşil DEĞİL; ek mutasyon, şartname maddesi M4-1/M4-2 ile karşılandı. | cmp AYNI |
| M5-1 | `GunlukNakitToplam`'a şube koşulu (`h.SubeId == db.AktifSubeId`) | `K11_SAHIP_gorunum_subesi_Merkez_iken_tevsik_kumulatifi_yine_kiraci_geneli`, `K10_Tevsik_…ikinci_satis_400`, `K10_Tevsik_onizleme…` | cmp AYNI |
| M6-1 | Tercih okumada geçersiz şube → `null` (TUMU) | `Tercih_pasife_alinan_sube_TUMU_ya_degil_oturum_subesine_duser` | cmp AYNI |
| M4-4 (tur 2) | `YasalKumulatifOku` ortam transaction'ına katılmaz (`if (ortamTx is not null && false)`) | `Ortam_transactioni_icinde_commit_edilmemis_nakit_kilitli_rolde_kumulatife_sayilir` (Beklenen 27000 · Gerçek 20000,00) | cmp AYNI |

**M2-1 sayı mutabakatı (tur 2, yeniden ölçüldü — `tur2/m2.zsh`):** aynı mutasyon (`GorunumSubesi.cs:68` `==` → `!=`), aynı derleme:
- `FullyQualifiedName~GorunumSubesiTestleri` (sınıfın tamamı, 24 koşum) → **8 kırmızı**: K1_Gunluk, K3_Aylik, K3_Kasalar, K3_Urun_stok_adetleri, K3_Emanet, K3_Pos_fatura_mutabakati, K3_Pos_tahsilatlar_ve_valor_takvimi, **K5_TEZGAHTAR_bos_subeId_ile_yalniz_kendi_subesini_gorur** (`Başarısız: 8, Başarılı: 16, Toplam: 24`).
- `…GorunumSubesiTestleri.K1_|…GorunumSubesiTestleri.K3_` (doğrulayıcının 9 testlik kümesi) → **7 kırmızı** (`Başarısız: 7, Başarılı: 2, Toplam: 9`; yeşil kalan 2 = K3_Hurda_bakiye / K3_Hurda_defter — hurda `Daralt` değil `HurdaStokKapisi` türetmesini kullanır).
- Sonuç: çelişki YOK; doğrulayıcının filtresi K5'i içermiyordu. Doğru ifade: "M2-1 → sınıfta 8 kırmızı; bunun 7'si K1/K3, 1'i K5". Günlükler: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-A/tur2/m2-sinif.log`, `m2-k1k3.log`; geri alma `cmp AYNI`.

M7/M8 (istemci `SubeliSorgu`, K-9 HataBandi) SK-B'ye aittir. Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln` → 0 hata (MSB3026 dosya kilidi yeniden deneme uyarısı: başka şeridin açık süreci, derleyici uyarısı değil).

## 9. denetim.sh
`DENETİM TEMİZ.` (günlük: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-A/denetim3.log`)
- `P-04: GUVENLIK_ELLE_IZOLASYON 0` · `IGNORE_QUERY_FILTERS 0` · `KULLANICI_SUZGEC 11` (paket öncesi **23**; K-12 "az ya da eşit" ✓)
- `Sınırsız rapor dökümü 22 (üst sınır 22)` · `Sınırsız ToListAsync 145 (tavan 145)` · `Sınırsız okuma beyaz listesi 15` — ilk denetimde yeni kasa/şube kimlik listeleri +3 saydırdı; `Take(RaporFiltresi.EnFazlaLimit)` ile sınırlandı, tavanlar artırılmadı.
- `sube-suzgec-siniflandirma.txt`: 13 bayat b satırı çıkarıldı (kod artık `Daralt`), belge amaçlı tek `b|Common/GorunumSubesi.cs (Daralt)` satırı eklendi.
- İlk denetim koşumunda görülen razor record / P-18 / B-3 ESIK_ISTEMCIDE kırmızıları Web tarafındaydı (başka şerit); son koşumda temizlendi. Bu paket Web'e dokunmadı.

## 10. Canlı doğrulama (5162, `ASPNETCORE_ENVIRONMENT=Development`, `--no-build`)
Test kiracısı `PAKET-SUBE-KAPSAM-A-TEST` API ile kuruldu (yerel sistem anahtarı yalnız bu sürece rastgele üretildi; davet → kayıt). İki şube (Merkez, Şube B) + pasife alınacak üçüncü şube, iki kasa, bir cari, **iki ayrı TEZGÂHTAR** (A=Merkez, B=Şube B). Sır değerleri yazılmadı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| K-10: TEZGÂHTAR A, Merkez, 20.000 TL NAKİT avans | 201 | **201**; `hareketler` 0 → 1 |
| K-10: TEZGÂHTAR B önizleme `/tevsik/gunluk?cariId=` | bugünkü nakit 20.000 | **200 · had 30000 · bugün 20000 · kalan 10000** |
| K-10: TEZGÂHTAR B, Şube B, 15.000 TL NAKİT avans | 400, yeni satır 0 | **400** "…toplamı 35.000,00 TL, 30.000,00 TL haddini 5.000,00 TL aşıyor (bu işlem 15.000,00 TL, günün önceki toplamı 20.000,00 TL)…"; `hareketler` **1 → 1** |
| Kontrol: TEZGÂHTAR B 9.000 TL NAKİT (toplam 29.000) | 201 | **201** |
| K-11: SAHİP `/tevsik/gunluk?cariId=&subeId=Merkez` | kiracı geneli 29.000 | **200 · 29000** |
| K-1/K-2: `/raporlar/gunluk?subeId=Merkez` / `=B` / boş | 1 / 1 / 2 hareket; kasalar 1/1/2 | **SUBE·1·1 / SUBE·1·1 / TUM_SUBELER·2·2** |
| K-3: `/kasalar?subeId=Merkez` / `=B` / boş | Merkez / B / ikisi | **['Merkez Kasa'] / ['B Kasası'] / ikisi** |
| K-4: SAHİP `/hurda/bakiye` (subeId yok) | 200 birleşik | **200 · TUM_SUBELER** (hurda yok → boş kırılım) |
| K-5: TEZGÂHTAR A `?subeId=B` → `/kasalar`, `/hurda/bakiye`, `/pos/tahsilatlar`, `/emanet/{cari}` | 403; boş → 200 | **403 "Bu şubeyi görme yetkiniz yok." ×4 · boş 200 ×4**; `/kasalar` boş → yalnız Merkez Kasa |
| Tercih: SAHİP `gorunum-subesi = C` | 200 | **200**, `subeId=C`, `gecersizTercih=false` |
| C pasife alındı (`DELETE /subeler/{C}`) → SQL `aktif` | 0 | **0** |
| Tercih oku (C pasif) | oturum şubesi, `gecersizTercih=true` | **subeId=<Merkez>, gecersizTercih=True, deger=<Merkez>** |
| K-6: `?subeId=<pasif C>` → gunluk, kasalar, hurda/bakiye, pos/tahsilatlar | 404 | **404 ×4** "Şube bulunamadı ya da pasif." |
| K-6: yabancı/bilinmeyen şube kimliği | 404 | **404** |
| Tercih yaz pasif C / TEZGÂHTAR `TUMU` | 404 / 403 | **404 / 403** |

SQL kanıtı (canlı): K-10 sonrası tek satır `Merkez Şube · AVANS_ALINDI · NAKIT · 20000.00`; kontrol sonrası şube başına `Merkez 1 · 20000.00`, `Şube B 1 · 9000.00`. MASAK (100.000 + 90.000) canlıda denenmedi — TEZGÂHTAR'a açık kimliksiz 90.000 TL'lik nakit-dışı akış (ürün + stok) canlı kiracıda kurulmadı; aynı desen HTTP testi `K10_Masak_…` ile gerçek MySQL üzerinde kanıtlı (paket öncesi 200, sonrası 400 + satır 0).

**Temizlik:** API ve vekil PID'leri `kill` ile durduruldu (`lsof :5162`, `:5163` boş). `zsh kiraci-temizle.sh <id> --uygula` → 296 satır + tenant silindi; `tenants WHERE id=<test>` = **0**. Kiracı-öncesi davet satırı (`kiraci_davetleri`, `tenant_id` yok) ayrıca silindi (1 satır). `tenants` sayısı başlangıçta 14, sonda 15: fark **benim değil** — koşum sırasında başka şerit `PAKETYASAL-1-TEST-DOGR` kiracısını açtı (12:38 UTC). `QA-TEST-*` kiracılarına dokunulmadı.

### 10.1 Regresyon — gerçek başsız Chrome (Panel, Kasalar, Hurda, Uyum)
Bu paket ekrana dokunmaz (Web/razor/css/Sozlukler değişmedi); sunucu yanıtı değişen ekranlar için regresyon turu `betikler/tur/tur.mjs` ile (vekil 5163, CDP 9905/9906), çok şubeli test kiracısında:

| Rota | 1280 PRO açık | 375 PRO açık | 1280 BASİT koyu | 375 BASİT koyu |
|---|---|---|---|---|
| `/` Panel | taşma 0 · yatay kaydırma 0 | 0 · 0 | 0 · 0 | 0 · 0 |
| `/kasalar` | 0 · 0 | 0 · 0 | 0 · 0 | 0 · 0 |
| `/hurda` | 0 · 0 | 0 · 0 | 0 · 0 | 0 · 0 |
| `/uyum` (+3 sekme) | 0 · 0 | 0 · 0 | 0 · 0 | 0 · 0 |

`analiz.py`: hata 0 · geçersiz 0; tek bulgu her rotada 375'te **genel arama katmanı** "SERBEST-YUZEN" (GlobalArama bileşeni — bu paketle ilgisiz, önceden var olan ortak bileşen; SK-B/tasarım turu alanı). Görüntüler Read ile görüldü: `tur-pro/foto/kok__PRO_aydinlik_1280.png` (Panel: Kasa Özeti iki şubenin kasası, Son işlemler 20.000 + 9.000), `tur-pro/foto/hurda__PRO_aydinlik_1280.png`, `tur-basit/foto/kasalar__BASIT_karanlik_375.png` (Merkez Kasa + B Kasası, toplam 229.000 ₺). Toplam 42 + 46 kayıt, 16 sayfa görüntüsü + sekme/açıcı görüntüleri: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-A/tur-pro/`, `tur-basit/`.

## 11. Değişen dosyalar (bu paket)
Not: ağaç tek "first commit" üstünde; `git status --porcelain | grep -v -E "/(bin|obj)/"` 524 satır verir (tüm şeritlerin birikimi). Bu paketin dokunduğu dosyalar:
- `dotnet/Kuyumcu.Api/Common/GorunumSubesi.cs` (YENİ)
- `dotnet/Kuyumcu.Api/Data/SistemKapsami.cs`
- `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs` (yalnız `GunlukNakitToplam`)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (yalnız `GunlukIslemToplam`)
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`, `SatisRaporlari.cs`, `StokYaslandirmaRaporu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` (yalnız `Listele`)
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` (yalnız `HurdaStokServisi` + `HurdaStokController`)
- `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` (yalnız okuma: `CihazListele`, `TahsilatListele`, `ValorTakvimi` + controller imzaları)
- `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs` (yalnız GET: `CariEmanetListesi`, `EmanetNet` isteğe bağlı parametre)
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunStokOzetModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` (yalnız controller imzaları)
- `dotnet/Kuyumcu.Api/Modules/Tercih/TercihModulu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs` (sapma § 3.1)
- `dotnet/sube-suzgec-siniflandirma.txt`
- `dokumanlar/02-mimari/adr/0002-sistem-kapsami-tek-kacis-yolu.md`
- `dotnet/Kuyumcu.Tests/GorunumSubesiTestleri.cs` (YENİ), `KumulatifKapiSubedenEtkilenmezTestleri.cs` (YENİ), `ZOISLEMASubeTestleri.cs` (sapma § 3.2)

Dokunulmayanlar: `YasalSinirlar.razor`, `YasalDegisiklikBandi.razor`, tüm `Kuyumcu.Web` (Pages/Bilesenler/Servisler/Layout), `Sozlukler.cs`, `denetim.sh`.
Dokunulan sayfalar (razor): **YOK**.

## 12. Tam süit ve kırmızı karşılaştırması
Komut: `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-A` (`KUYUMCU_TEST_DB=kuyumcu_test_t2`, son kodla, tur başına bir kez).
Sonuç: **Toplam 7875 · Geçti 7870 · Başarısız 4 · Atlandı 1** (1,9 dk). trx: `/private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-A/_Ramazan-MacBook-Air_2026-09-30_15_42_12.trx`.
Bu paketin eklediği test: **30** (önce 7845 → sonra 7875; ölçüm anında başka şeritlerin eklediği testler bu farka dahil değildir — fark yalnız bu paketin iki yeni dosyasıdır).

Kırmızılar (4) — hepsi `Kuyumcu.Web` sayfa/bileşen çizen bUnit testleri; bu paket Web'e dokunmadı:
| Test | Sahibi (dosya alanı) |
|---|---|
| `FiyatListesiUrunGorunumuTestleri.Liste_secilince_urun_tablosu_ve_KAYNAK_rozetleri_cizilir` | Fiyatlandırma ekranı — KESİF-P şeridi |
| `FiyatlandirmaUX4aEkranTestleri.Liste_secilince_urun_paneli_acilir_ve_ONIZLEME_ucu_listeyle_cagrilir` | Fiyatlandırma ekranı — KESİF-P şeridi |
| `P78.PosOranTurkceTestleri.Pos_komisyon_orani_TR_gosterilir_ve_virgullu_giris_1_5_gibi_OKUNUR` | Pos ekranı — KESİF-P şeridi |
| `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` | Web `ApiIstemci` istek fırtınası sayacı (istemci) — KATMAN/ortak bileşen şeridi |

Bu paketin sunucu kodu bUnit testlerinin sahte API'sine girmez; **bu paketin ürettiği yeni kırmızı: 0** (paketin doğrudan etkilediği `ZOISLEMASube` 3 testi § 3.2 ile yeni sözleşmeye uyarlandı ve yeşil). Filtreli ara koşumda görülen diğer Web kırmızıları (`PosEkranTestleri`, `GorunumEkranTestleri.GE1`, `B1GorunumEkranTestleri.BE1/BE2`, `S1fGorunumKartiEkranTestleri`) tam süitte yeşildi — o sırada başka şeridin yarım Web düzenlemesiydi.

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K-1 | KARŞILANDI | `K1_Gunluk_…` (SQL SUM = yanıt, kuruş farkı 0) + canlı gunluk |
| K-2 | KARŞILANDI | aynı test (birleşik = M + B) + canlı |
| K-3 (7 uç × 2) | KARŞILANDI | K3 testleri: aylık, hurda bakiye, hurda defter, POS tahsilat(+valör, cihaz), kasalar, stok-adetleri, emanet (+mutabakat) |
| K-4 | KARŞILANDI | `K4_Subesiz_SAHIP_hurda_bakiye_200_…` |
| K-5 | KARŞILANDI | Theory 8 uç + canlı 4 uç 403 |
| K-6 | KARŞILANDI | `K6_…` 4 uç × 2 + canlı |
| K-10 | KARŞILANDI | paket öncesi KIRMIZI → sonrası yeşil; canlı 400 + satır 0 (tevsik); MASAK HTTP testi |
| K-11 | KARŞILANDI | `K11_…` + canlı 29.000 |
| K-12 | KARŞILANDI | denetim TEMİZ · ELLE_IZOLASYON 0 · sınıf b 23 → 11 |
| § 7 M1–M6 | KARŞILANDI | § 8 |
| DoD sözleşmesi | KARŞILANDI | § 6 (yeni varlık yok; tercih PATCH/DELETE + denetim) |
| Gerçek tarayıcı (regresyon) | KARŞILANDI (ekrana dokunulmadı) | § 10.1 |

## 14. Açık kalanlar
1. **SK-B (istemci) — ayrı paket:** `KabukServisi.GorunumSubesi`, `SubeliSorgu.Ekle`, `GorunumRozeti`, seçicinin `gorunumSubesi.tumSubeler`'i okuması (§ 4.3.5 — sunucu alanı bu pakette hazır), `ListeOnbellegi` anahtarı `url+subeId`, K-7/K-8/K-9/K-13/K-14. Not: Hurda ekranının boş durum metni "Bu şubede hurda hareketi yok" diyor; birleşik görünümde "Tüm şubelerde…" demeli (SK-B, `kapsam` alanını okuyarak).
2. **§ 4.2.3 `kapsam: "KIRACI_GENELI"`** — `/cariler*`, `/faturalar*`, `/bankalar*`, `/masak/*`, `/tevsik/*` yanıtlarına eklenmedi (dosyalar şerit alanı dışında); rozet ihtiyacı doğunca SK-B ile birlikte.
3. **Mehmet (06 Güvenlik) onayı** (tur 2 ekiyle: okuyucu artık ortam transaction'ında aynı bağlantıyı paylaşır — ADR-0002 ek notu güncellendi) — SistemKapsami madde 10 (ADR-0002 ek notu) orkestratör kapısında.
4. **Tam süitteki 4 Web/bUnit kırmızısı (başkasına ait, § 12):** `FiyatListesiUrunGorunumuTestleri…`, `FiyatlandirmaUX4aEkranTestleri…`, `P78.PosOranTurkceTestleri…` (KESİF-P şeridi: Fiyatlandırma/Pos ekranları), `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi…` (istemci ApiIstemci). Bu paket Web'e dokunmadı.
5. **Paralel şerit yarışı (süreç notu):** aynı `bin/` dizinini paylaşan şeritler bir mutasyon penceresinde birbirinin derlemesini görebiliyor (M6-1 ilk koşumunda M5 mutasyonlu DLL koştu). Mutasyonlar bu yüzden özel çıktı dizinine (`-o`) alındı; önerilen kalıcı çözüm: şerit başına `--output`.

6. **[ORTA · ŞK-B / orkestratör kuyruğu] Hurda satış formu "kalan bakiye" (UX-S22):** `HurdaModulu` şubesiz tüm-şube rolüne parametresiz `/hurda/bakiye`'de birleşik bakiye döner (§ 4.2.2 / K-4 gereği); `Hurda.razor` hâlâ parametresiz çağırıp bu değeri ramat çıkışı formunda "Kalan bakiye" gösteriyor, yazma ise `HurdaStokKapisi.OturumSubesiCoz` ile oturum şubesinden düşüyor. Mali kayıp yok (sunucu doğru sınırı uygular, fazla satış 400), ama aynı ekranda iki cevap. ŞK-B'de: form değeri `?subeId=<yazma şubesi>` ile, kart görünüm şubesinden okunmalı; ya da birleşik görünümde satış formu şube seçtirmeli. Bu şerit `Hurda.razor`'a dokunmaz (şerit alanı dışı; KESİF/ŞK-B).
7. **[ORTA] § 4.2.3 `kapsam: "KIRACI_GENELI"` eksikleri** (madde 2'nin sahipli hâli): `/cariler*`, `/faturalar*`, `/bankalar*`, `/masak/*`, `/tevsik/*` — ilgili modül şeritleri + ŞK-B rozeti ile birlikte. `subeId`'nin bu uçlarda yok sayıldığı K-11 testiyle kanıtlı.
8. **[DÜŞÜK · ŞK-B] Şubesiz tüm-şube kullanıcısında geçersiz tercih:** `TercihModulu` geçersiz tercihte `islem.SubeId`'ye düşer; şubesiz SAHİP'te bu `null` → yanıt `deger='TUMU'`, `gecersizTercih=true`. Veri genişlemesi yok (SAHİP zaten kiracı geneli yetkili). ŞK-B uyarı bandı bu durumda da çıkmalı.
9. **[DÜŞÜK · orkestratör] 5199 inceleme sunucusu** bu tur sonunda da dinlemiyor (`lsof :5199` satır sayısı 0). Bu şerit yalnız 5162/5163'ü açıp kapattı; 5199'a dokunulmadı — orkestratör gece yetkisiyle yeniden başlatmalı.
10. **[DÜŞÜK] Derleme uyarıları (başkasına ait):** `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601` (bu paketin DOĞRULAYICISININ dosyası — hasım testine uygulayıcı dokunmaz) ve `KesifP1Testleri.cs(448,37) CS8604` (KESİF-P1 şeridi). Api/Web/Contracts derleyici uyarısı 0.

## 15. Kurucu / mali müşavir
- **Mali müşavir (T-1, PAKET-YASAL-3 § 3.2 ile birlikte):** "Tevsik ve MASAK günlük kümülatifi şubeler arasında birleştirilir (mükellef/yükümlü geneli)" yorumu hükümlerde şube istisnası bulunmamasından çıkarılmıştır — DOĞRULANAMADI, teyit istenir. Kod tutucu yöndedir (kapı yalnız sıkılaştı).
- Kurucuya: § 2.6 bir **yasal açıktı** ve kapandı — çok şubeli dükkânda tezgâhtarlar aynı müşteriyle aynı gün farklı şubelerde nakit işlem yaparak 30.000 TL tevsik haddini ve 185.000 TL MASAK eşiğini fark edilmeden aşabiliyordu.

## 16. Tur 2 — doğrulayıcı bulgularının kapanışı (2026-09-30 16:05–16:35)

| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| M2-1 kırmızı test sayısı çelişkisi | KRİTİK (hakem) | Yeniden ölçüldü; çelişki filtre kapsamından (§ 8 mutabakat) | sınıf 8/24 · K1\|K3 7/9 · `tur2/m2-*.log` · cmp AYNI |
| `YasalKumulatifOku` ayrı bağlantı → ortam transaction'ındaki commit edilmemiş satırı görmez | DÜŞÜK | **Kodla kapatıldı:** `SistemKapsami.YasalKumulatifOku` çağıranın `CurrentTransaction`'ı varsa açılan bağlamda `SetDbConnection(aynı bağlantı)` + `UseTransactionAsync(aynı tx)`; sahiplik devredilmez. ADR-0002 ek notuna değişmez yazıldı. | Yeni test `KumulatifKapiSubedenEtkilenmezTestleri.Ortam_transactioni_icinde_commit_edilmemis_nakit_kilitli_rolde_kumulatife_sayilir` (B'de 20.000 commit + A'da 7.000 aynı tx → 27.000; rollback → 20.000). M4-4: paylaşım kapatılınca **20.000** → KIRMIZI; geri → cmp AYNI. Bulgu kuramsal değilmiş: paylaşımsız okuyucu satırı gerçekten görmüyordu. |
| Hurda formu kalan bakiye (UX-S22) | ORTA | Şerit alanı dışı (`Hurda.razor`) → § 14.6, ŞK-B/orkestratör kuyruğu | — |
| § 4.2.3 `kapsam` eksik uçlar | ORTA | § 14.7 (sahipli açık kalan) | K-11 testi |
| Şubesiz SAHİP geçersiz tercih → TUMU | DÜŞÜK | Davranış § 14.8'e not edildi | `Tercih_*` testleri yeşil |
| 5199 dinlemiyor | DÜŞÜK | Dokunulmadı; § 14.9 orkestratöre | `lsof :5199` = 0 |

**Derleme (tur 2):** `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 2 Uyarı** (ikisi de başkasının test dosyasında, § 14.10; Api derleyici uyarısı 0).
**Filtreli (tur 2):** GorunumSubesi + KumulatifKapi + ZOISLEMASube + SubeIzolasyon + SUBEKAPSAMA (doğrulayıcı) + Tevsik + Masak + Tercih → 375 · 374 geçti · 1 başarısız (`KATMAN2MenuTercihEkranTestleri.Istek_firtinasi…` — başkasına ait, tur 1'de de vardı).
**denetim.sh (tur 2, `bash`):** `DENETİM TEMİZ.` · `GUVENLIK_ELLE_IZOLASYON 0` · `IGNORE_QUERY_FILTERS 0` · `KULLANICI_SUZGEC 11` (günlük `tur2/denetim.log`).
**Tam süit (tur 2, tek koşum, son kodla):** `Toplam 7890 · Geçti 7888 · Başarısız 1 · Atlandı 1` (2,9 dk) — trx `/private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-A-tur2/_Ramazan-MacBook-Air_2026-09-30_16_18_54.trx`. Tek kırmızı `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` (tur 1 listesinde var; Web ApiIstemci — başkasına ait). Tur 1'in diğer 3 Web kırmızısı (Fiyatlandırma/Pos) sahip şeritlerce kapanmış. **Bu paketin ürettiği yeni kırmızı: 0.** Bu paketin yeni testi: 30 → **31**.

**Canlı (tur 2, 5162, yeni `SistemKapsami` koduyla):**
| Adım | Beklenen | Gerçek |
|---|---|---|
| K-10: TEZGÂHTAR A (Merkez) 20.000 NAKİT avans | 201 · hareket 0→1 | **201** · `acikAvans 20000` · 0→**1** |
| K-10: TEZGÂHTAR B önizleme `/tevsik/gunluk?cariId=` | 20.000 | **200 · bugünkü nakit 20000 · had 30000** |
| K-10: TEZGÂHTAR B (Şube B) 15.000 NAKİT avans | 400 · satır değişmez | **400** "…toplamı 35.000,00 TL, 30.000,00 TL haddini 5.000,00 TL aşıyor…" · 1→**1** |
| Kontrol: TEZGÂHTAR B 9.000 | 201 | **201** · 1→**2** |
| K-11: SAHİP `?subeId=Merkez` önizleme | kiracı geneli 29.000 | **200 · 29000** |
| K-3: `/kasalar?subeId=M` / `=B` / boş | M / B / ikisi | **['Merkez Kasa2'] / ['B Kasası'] / ikisi** |
| K-5: TEZGÂHTAR A `/kasalar?subeId=B` | 403 | **403** "Bu şubeyi görme yetkiniz yok." |
| K-4: SAHİP `/hurda/bakiye` | 200 birleşik | **200 · TUM_SUBELER** |

SQL: `Merkez Şube · AVANS_ALINDI · NAKIT · 20000.00 / 20000.00` ve `Şube B · AVANS_ALINDI · NAKIT · 9000.00 / 9000.00` (15.000'lik red satır yazmadı).
**Temizlik:** API PID `kill` (`lsof :5162` = 0). Kayıt oran sınırı (3/saat) ve eksik kur adımı yüzünden canlı betik 4 kez kiracı açtı; dördü de `zsh kiraci-temizle.sh <id> --uygula` ile silindi (`tenants WHERE ad LIKE 'PAKET-SUBE-KAPSAM-A%'` = **0**), kendi 6 davet satırı silindi (kalan 0). `tenants`: başlangıç **13** → son **13**. `QA-TEST-*` kiracılarına dokunulmadı. Yerel sistem anahtarı dosyası `printf ''` ile boşaltıldı.
**Gerçek tarayıcı:** tur 2 yalnız `SistemKapsami.cs` (sunucu) + test + ADR değiştirdi; ekrana dokunan iş yok → tur 1 regresyon ölçümü (§ 10.1) geçerli.

**Tur 2'de değişen dosyalar:** `dotnet/Kuyumcu.Api/Data/SistemKapsami.cs` · `dotnet/Kuyumcu.Tests/KumulatifKapiSubedenEtkilenmezTestleri.cs` · `dokumanlar/02-mimari/adr/0002-sistem-kapsami-tek-kacis-yolu.md` · bu rapor.

