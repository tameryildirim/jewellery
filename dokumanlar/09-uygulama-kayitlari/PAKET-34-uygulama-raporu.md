# Paket 34 — Rapor seti tamamlama (hedef 19 rapor) · **FAZ A** uygulama raporu

> Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-17
> Durum: **BİTTİ (Faz A)** · Derleme 0 hata / 0 uyarı · 2161/2161 test yeşil · `dotnet/denetim.sh` **TEMİZ**
> Kapsam: yalnız **Faz A** (RaporOrtak + katalog + R10–R14) + **MÜHÜR 5 / 34-Ö** kabul ölçütü + **QA turu 3** iki bulgusu.

---

## 1. Yapılanlar

### 1.1 Ortak altyapı (YENİ)
`dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs` (126 satır)
- `RaporAraligi.Coz(bas, bit)` → `RaporDonemi(Bas, Bit, YerelBas, YerelBit)`. K5 kuralı **tek yerde**: yerel (Europe/Istanbul) gün sınırı UTC'ye çevrilir. Bugüne kadar aynı dönüşüm `GunlukOzet`, `AylikOzet`, `PosFaturaMutabakati`, `KarZararServisi.Donem` içinde **dört kez** tekrarlanıyordu.
  - `bitis < baslangic` → 400 · dönem > 400 gün → 400 · ikisi de boşsa **bugün**.
- `RaporFiltresi` + `Dogrula()`: limit 1–500, ofset ≥ 0. **Kapı SERVİSTEDİR**, `[Range]`'de değil — Aşkın'ın koşulu: DataAnnotations yalnız controller model bağlamada çalışır, servisi doğrudan çağıran yol limitsiz döküm yapabilirdi.
- `SayfaliSonuc<T>(ToplamSatir, Ofset, Limit, Satirlar)` + `DahaFazlasiVar`.
- `RaporGuvenilirligi.Hesapla(bilinen, bilinmeyen, ekKusur)` → VERI_YOK / TAM / YUKSEK / ORTA / DUSUK. `KarZararServisi`'nin özel yardımcısı buraya taşındı; artık bütün raporlar aynı ölçeği kullanır.

### 1.2 Rapor kataloğu (YENİ)
`dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs` (69 satır) — **Faz A'da sevk edilmiş 15 rapor**, `GET /api/v1/raporlar/katalog`.
"Kaç rapor var?" sorusu artık koddan cevaplanıyor. Katalog bir iddia değil ölçümdür: her satırın `Uc` alanının **gerçekten kayıtlı bir `[HttpGet]` ucuna** karşılık geldiği yansımayla sınanıyor.

| Grup | Raporlar |
|---|---|
| Gün Sonu | R01 gunluk · R02 aylik · R05 pos-fatura-mutabakati · **R11 kasa-defteri (YENİ)** |
| Satış Analizi | **R10 islem-defteri · R12 odeme-araci · R13 personel · R14 sube-karsilastirma (hepsi YENİ)** |
| Stok | R06 stok-degeri · R20 reyon-stok · R09 stok-degerlemesi |
| Mali | R03 cari-yaslandirma · R04 vade-takibi · R07 kar-zarar · R08 kar-zarar/urun |

### 1.3 Yeni raporlar (Faz A — 5 uç)
`dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs` (470) · `FinansRaporlari.cs` (167)

| Kod | Uç | Değişmez kural |
|---|---|---|
| R10 | `GET /raporlar/islem-defteri` | **İşaret korunur** — `tlTutari` HAM işaretiyle döner, `yon` ayrı alandır. Sayfalı (limit/ofset/toplamSatir). Cari adı taşıdığı için **okuma izi** yazar. |
| R11 | `GET /raporlar/kasa-defteri` | Gün gün devir/giriş/çıkış/kapanış. **Kapanmamış gün "ACIK", farkı `null`** (sıfır DEĞİL). Devir kasanın gerçek bakiyesinden geriye çözülür (açılış bakiyesi `kasa_hareketleri`'nde satır değildir). |
| R12 | `GET /raporlar/odeme-araci` | Kırılım **`kasa_hareketleri` bacaklarından** okunur, `hareketler.odeme_turu`'ndan DEĞİL (karma fişte o kolon yalnız "KARMA" özet etiketidir). `nakit_tutar` NULL = **BİLİNMİYOR**, ayrı sütun, sıfır sayılmaz. |
| R13 | `GET /raporlar/personel` | Alan adı `kaydedenAd`; `olcut: "KAYDEDEN_KULLANICI"`, `uyari: "SATAN_DEGIL_KAYDEDEN"`. "Satan/tezgâhtar" ne yanıtta ne ekranda geçer. |
| R14 | `GET /raporlar/sube-karsilastirma` | **TRANSFER ciroya KATILMAZ** (`transferCiroyaKatilir: false`), ama ayrı sütunda görünür. |

**Kapılar:** üç yeni controller da sınıf düzeyinde `[Authorize][RolGerekli("SAHIP","MUDUR","MUHASEBE")][Route("api/v1/raporlar")][ModulYetkisi("rapor")]` taşır — nitelik miras alınmaz (K34-3).
**Kiracı sınırı:** `cariId`/`kullaniciId`/`subeId` süzgeçleri doğrulanır; yabancı kiracının kimliğiyle süzme **boş sonuç değil 404** döner (K34-4).
**Storno devri (48-A):** rapor sorguları `StornoSuzgeci.StornoCiftiniDisla` **kullanır**, ikinci süzgeç yazılmadı. Storno çifti ciro/adet toplamlarından dışlanır; işlem defterinde üç satır da durur (append-only) ve `storno: {adet, tutarTl}` **ayrı blokta** sayılır.

### 1.4 Ekranlar
| Rota | Dosya | Not |
|---|---|---|
| `/raporlar/islem-defteri` | `IslemDefteri.razor` (313) | Tarih + tür + ödeme aracı + şube + kullanıcı süzgeçleri, sayfalı tablo, özet kartları, Yazdır (`@media print` / `yazdirma-gizle`). |
| `/raporlar/satis-analizi` | `SatisAnalizi.razor` (288) | R12/R13/R14 üç sekme; sekme değişince **yalnız o sekmenin** ucuna gidilir. |
| `/raporlar/kasa` | `KasaRaporu.razor` (166) | Kasa seçici (tek kasada gizli), gün gün tablo, AÇIK gün amber satır. |
| `/raporlar` | `Raporlar.razor` (DEĞİŞTİR) | Üstte **Rapor Merkezi** kart ızgarası, katalogtan beslenir. Mevcut bloklar korundu. |

**Menü:** `KenarCubugu.razor`'a **yalnız üç** NavLink eklendi (spesifikasyondaki "altı yeni bağlantı" reddi korundu); kalan raporlar Rapor Merkezi'nden açılır.
**İstemci sözleşmesi:** `Kuyumcu.Web/Servisler/RaporServisi.cs` (129) — tipli record'lar + tek `Sorgu()` yardımcısı; sahte API deseni tek yerden kurulabiliyor.
**Sözlük:** `SozlukRaporSeti` (**86 anahtar** × TR/EN) + `Sozluk.RaporSeti` + `Ad(kod)` / `Grup(kod)` çevirici; `SozlukNav`'a `IslemDefteri`, `SatisAnalizi`, `KasaRaporu`. Eksik anahtar **derleme hatası** verir (`required init`).
**Rota başlıkları:** `RotaBasliklari.Harita`'ya üç yeni rota (üst bar "Sayfa bulunamadı" demesin).

### 1.5 MÜHÜR 5 / 34-Ö — kabul ölçütü (TB-19)
- **`CariYaslandirma`**: sınırsız `ToListAsync` → `limit`/`ofset`/`toplamSatir`. `ozetTl` **sunucuda 5 ayrı toplama sorgusuyla**, TÜM cari kümesi üzerinden hesaplanır (sayfadan türetilmez). Cari adları artık yalnız **sayfadaki** kimlikler için okunur (eski hâl tüm carileri sözlüğe alıyordu — ikinci sınırsız okuma).
- **`StokDegeri`**: `ToListAsync`+bellek gruplaması → **SQL `GroupBy`** + `limit`/`ofset`/`toplamSatir`; maden dağılımı ve `toplamHas` tüm ürünlerden, ürün kartı yalnız sayfa için.
- **Yanıt şekli DEĞİŞMEDİ** (`yaslananCariler`+`ozetTl`, `urunler`+`toplamHas`+`madenDagilimi`+`uyari`); yalnız sayfalama alanları eklendi. `Raporlar.razor` kayıtları kırılmadı (regresyon testi + tam süit yeşil).
- **`denetim.sh` sayacı eklendi:** "Rapor materyalizasyonu (TB-19)" — `Modules/Rapor` + `Modules/Maliyet` altındaki `ToListAsync`/`ToDictionaryAsync` sayısı, **üst sınır 43**. Artarsa denetim kırmızıya döner.
- **Ölçüm (50.001 ürünlü kiracıda, canlı 5198):** aşağıda § 7.4.

### 1.6 QA turu 3 bulguları (ikisi de KAPANDI)
| Bulgu | Kök neden | Düzeltme | Kanıt |
|---|---|---|---|
| `/raporlar` "Stok Dağılımı" pastası **hiç çizilmiyor** | `PastaGrafik.razor` dilim yolu `$"M {cx:F1} …"` ile üretiliyordu; tr-TR'de ondalık **virgül** → `<path d>` ayrıştırılamıyor | `string.Format(CultureInfo.InvariantCulture, …)` | Yeni test `GrafikKulturTestleri.PastaGrafik_dilim_yolu_tr_TR_kulturunde_de_SAYI_uretir` · mutasyon **M-34-QA3a** |
| Aylık özet **veresiye ve tahsilat 0**, gün sonuyla çelişiyor | (1) veresiye yalnız `Tur=="VERESIYE"` sayılıyordu; veresiye **satışın** payı `SATIS` satırının `veresiye_tutar`'ındadır. (2) tahsilat yalnız `Tur=="TAHSILAT"`; gün sonu ise **kasaya giren parayı** sayıyor → aynı ekranda iki cevap (UX-S22) | veresiye = R12 ile **aynı tanım** (borç doğuran yönde `veresiye_tutar` toplamı) · tahsilat = **gün sonuyla aynı kaynak** (`kasa_hareketleri` TL girişleri), `kaynak` alanı yanıtta yazılı; cari tahsilatı `cariTahsilatiTl`'de saklanıyor | Canlı: aylık tahsilat **₺42.000** = gün sonu tahsilat **₺42.000**; aylık veresiye **₺3.000** = karma fişin veresiye payı · mutasyon **M-34-QA3b** |

---

## 2. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | `Sabitler.HareketTurleri`'ne SATIS_IADE/ALIS_IADE/CARI_DUZELTME **eklenmedi**; `Entities.cs`'e **dokunulmadı** | Birleşik koşul: üçü `Entities.cs`'te zaten var (doğrulandı). Bayat okuma. |
| S2 | Katalog testi "**tam 19**" değil, "**≥ N + kodlar tekil + her rota gerçek bir `[HttpGet]`**" | Bertan/Aşkın koşulu: `==19` testi tautolojidir ve paket 39/42'yi kırar. |
| S3 | Katalog **15** rapor sayıyor, 19 değil | Faz A'da sevk edilen rapor sayısı budur. Faz B'nin 5 raporu (R15–R19) **katalogda YOK** — DoD gereği "tıklanınca 404 veren kart" açılmaz. Faz B ile 20'ye çıkar. |
| S4 | Faz B raporları (R15 kategori-satış, R16 stok-devir, R17 hurda-özet, R18 kdv-özet, R19 iade-özet) **yazılmadı**; ekranlar `StokAnalizi.razor` / `MaliRaporlar.razor` **açılmadı** | Görev "yalnız Faz A" + Aşkın koşulu: 19'luk küme **mimarın kendi önerisidir**, kabul ölçütü oylanmadan Faz B yazılmaz. M113/M118/M119/M120 mutasyonları da bu yüzden Faz B'ye kaldı. |
| S5 | `RaporGuvenilirligi` `KarZararServisi.Guvenilirlik`'in yerine **taşındı ama eski metot da duruyor** | Eski metot `private static` ve yalnız kendi içinde kullanılıyor; imzası (maliyetli/maliyetsiz/kursuz) alan adlarıyla anlamlı. Ortak yardımcı yeni raporlarda kullanılıyor. Eski çağrının bağlanması KarZarar paketinin (35) işidir — **açık kalan**, § 9/A3. |
| S6 | `RaporServisi` ctor'una `DenetimServisi` **zorunlu** parametre olarak eklendi; 18 test çağrı yeri güncellendi | Birleşik koşul: opsiyonel `null` parametre YASAK (null'da iz düşmez = sessiz KVKK açığı). `TestKurulum.RaporServisi(db)` fabrikası eklendi, hepsi gerçek `DenetimServisi` alıyor. |
| S7 | Kasa defteri gün kovası SQL'de `Tarih.AddHours(ofset)` ile kuruluyor; ofset `Zaman.Isletme`'den okunuyor, **sabit yazılmıyor** | TB-19: satırları belleğe alıp gruplamak sınırsız okumadır. Yaz saati riski varsa yanıt `yazSaatiRiski: true` ile **beyan eder** (saklanmaz). |
| S8 | `GunlukOzet` / `AylikOzet` / `PosFaturaMutabakati` TB-19 kapsamına **alınmadı** | Bertan koşulu "ya hepsi ya açık borç": bunlar dönem (gün/ay) ile sınırlı ama satır sayısıyla değil. Tam sunucu-tarafı toplamaya çevirmek maden dağılımı + günlük kırılım + kâr bloğunu yeniden yazmayı gerektirir ve regresyon riski Faz A'nın değerinden büyüktür. **AÇIK BORÇ olarak yazıldı** — `denetim.sh` sayaç bloğunun yorumunda ve § 9/A1'de. |

**Mevzuat:** Bu pakette **hiçbir mevzuat hükmü koda yazılmadı**. VUK md.253/256 rapor için saklama yükümlülüğü doğurmaz; KDV md.23/(e) özel matrah Faz B'nin R18'idir ve **salt okur** olacaktır (matrah paket 38'in işi). KVKK md.12 okuma izi zaten uygulanan ilkedir, iki uca daha yayıldı.

---

## 3. Migration

**`20260917125715_RaporIndeksleri`** — yalnız indeks, kolon/tablo değişikliği YOK.

| Tablo | İndeks | Hangi rapor kullanıyor |
|---|---|---|
| hareketler | `(tenant_id, sube_id, tarih)` | R14 şube karşılaştırma · R10 şube süzgeci |
| hareketler | `(tenant_id, kullanici_id, tarih)` | R13 kaydeden kullanıcı · R10 kullanıcı süzgeci |
| hareketler | `(tenant_id, tur, tarih)` | R10 tür süzgeci · R19 (Faz B) |
| hareketler | `(tenant_id, urun_id, tarih)` | R16 stok devir (Faz B) · ürün bazlı döküm |
| kasa_hareketleri | `(tenant_id, kasa_id, tarih)` | R11 kasa defteri |

**`kuyumcu_net`'e uygulama kanıtı:**
```
Applying migration '20260917125715_RaporIndeksleri'.  Done.   (exit=0)

SHOW INDEX FROM hareketler WHERE Key_name LIKE 'IX_hareketler_tenant_id_%tarih':
  IX_hareketler_tenant_id_cari_id_tarih
  IX_hareketler_tenant_id_kullanici_id_tarih   ← YENİ
  IX_hareketler_tenant_id_maden_tarih
  IX_hareketler_tenant_id_sube_id_tarih        ← YENİ
  IX_hareketler_tenant_id_tarih
  IX_hareketler_tenant_id_tur_tarih            ← YENİ
  IX_hareketler_tenant_id_urun_id_tarih        ← YENİ
```
Toplam migration sayısı: **42**. Üretilen `.cs` ve `.Designer.cs` okundu, `Up()` gövdesi beş `CreateIndex` içeriyor, `Down()` beşini de düşürüyor.

---

## 4. Test sayıları

| | Önce | Sonra |
|---|---|---|
| Toplam test | **2106** | **2161** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | 0 |

**Yeni test: 55**
- `RaporOrtakTestleri.cs` — **14** [saf]: aralık çözümü, K5 yerel gün sınırı, limit/ofset kapıları, güvenilirlik ölçeği, katalog sayısı + kod tekilliği + **yansımayla rota doğrulama** + ham kod basılmama.
- `RaporSetiTestleri.cs` — **17** [DB]: işaret koruma, limit kapısı, tür süzgeci, sayfalama+toplamSatir, yabancı kimlik 404, kiracı izolasyonu (regresyon), karma fiş bacak kırılımı, bilinmeyen nakit payı, kaydeden kullanıcı ayrımı, "KAYDEDEN" metni, transfer ciro dışı, storno çifti, kasa defteri AÇIK/KAPALI + K5 gece yarısı kovası, TB-19 yaşlandırma ve stok değeri, KVKK okuma izi, QA3 aylık özet.
- `RaporYetkiTestleri.cs` — **9** [HTTP]: katalog üzerinden döngüyle TEZGAHTAR 403 (15 uç), MUHASEBE 200 (15 uç), **modül açıkken rol kapısı 403**, 401, modül kapatılınca MUDUR 403, camelCase sözleşmesi, limit 400, ters tarih 400, katalog.
- `RaporEkranTestleri.cs` — **14** [bUnit]: ilk yükleme limitli istek, süzgeç → yeni istek (yol+query), sayfa ileri/geri ofset, sınır uyarısı, bilinmeyen nakit payı, TL işareti, **her düğme ya istek atar ya kilitlidir**, sekme → ilgili uç, KAYDEDEN başlığı, bilinmeyen beyanı, transfer notu, kasa AÇIK günü, kasa seçici, Rapor Merkezi kart çizimi.
- `GrafikKulturTestleri.cs` — **+1**: pasta dilim yolu tr-TR kültüründe sayı üretir (QA3 bulgusu).

Tüm assert'lerde bUnit için `WaitForAssertion` kullanıldı; istek yolu/query sahte API kayıt listesinden okunuyor.

---

## 5. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M109** | `RaporAraligi` K5 dönüşümü yerine ham `DateOnly→DateTime` | `RaporOrtakTestleri.Yerel_gun_siniri_isletme_saatine_gore` | cmp AYNI |
| **M110** | `RaporFiltresi.Dogrula()` limit kapısı kaldırıldı | `RaporSetiTestleri.Islem_defteri_limitsiz_cagrilamaz` | cmp AYNI |
| **M111** | Ödeme aracı kırılımı `hareketler.odeme_turu`'ndan okundu | `RaporSetiTestleri.Karma_fis_odeme_aracina_bacaklara_bolunur` | cmp AYNI |
| **M112** | TRANSFER şube cirosuna katıldı | `RaporSetiTestleri.Transfer_sube_cirosuna_KATILMAZ` | cmp AYNI |
| **M114** | Sözlükte `KaydedenKullanici` → "Satan Tezgâhtar" | `RaporSetiTestleri.Personel_tablosu_KAYDEDEN_der` **+** `RaporEkranTestleri.Personel_tablosu_KAYDEDEN_der_SATAN_ICERMEZ` | cmp AYNI |
| **M115** | `CariYaslandirma`'dan `ListeOkumaKaydet` kaldırıldı | `RaporSetiTestleri.Kisisel_veri_donen_rapor_okuma_izi_yazar` | cmp AYNI |
| **M116** | Katalogtan R14 satırı silindi | `RaporOrtakTestleri.Katalog_asgari_sayiyi_tasir_ve_kodlar_tekildir` | cmp AYNI |
| **M117** | NULL nakit payı 0 sayıldı | `RaporSetiTestleri.Bilinmeyen_nakit_payi_sifira_yazilmaz` | cmp AYNI |
| **M121** | `toplamSatir = min(count, limit)` (sayfadan türetildi) | `RaporSetiTestleri.Yaslandirma_toplamSatir_gercek_sayidir` | cmp AYNI |
| **M122** | İşlem defteri `tlTutari`'ye `Math.Abs` | `RaporSetiTestleri.Islem_defteri_TL_isaretini_korur` | cmp AYNI |
| **M-34-S** | `StornoCiftiniDisla` şube karşılaştırmadan kaldırıldı | `RaporSetiTestleri.Storno_cifti_ciroyu_sismez_ama_AYRI_gorunur` | cmp AYNI |
| **M-34-K5** | Kasa defteri gün kovası UTC gününe göre kuruldu | `RaporSetiTestleri.Kasa_defteri_gun_kapanisi_farkini_gosterir_kapanmamis_gunu_ACIK_isaretler` | cmp AYNI |
| **M-34-Y** | `SatisRaporController`'dan `[RolGerekli]` silindi | `RaporYetkiTestleri.TEZGAHTAR_rapor_modulu_ACILSA_BILE_rol_kapisina_takilir_403` | cmp AYNI |
| **M-34-QA3a** | Pasta dilim yolu `InvariantCulture`'sız | `GrafikKulturTestleri.PastaGrafik_dilim_yolu_tr_TR_kulturunde_de_SAYI_uretir` | cmp AYNI |
| **M-34-QA3b** | Aylık veresiye eski (hatalı) tanıma döndürüldü | `RaporSetiTestleri.Aylik_veresiye_ve_tahsilat_GUN_SONU_ile_ayni_kaynaktan_gelir` | cmp AYNI |

### 5.1 Bulunan ÜÇ test açığı (hepsi kapatıldı — Anayasa md.1)
1. **M-34-S ilk denemede hiçbir testi kırmadı.** Sebep: storno çifti TL'de **net sıfırdır** — süzgeç kaldırılsa bile `toplamCiroTl` 10.000 kalıyordu. Test yalnız tutara bakıyordu. → `satisAdet == 1` assert'i eklendi (süzgeçsiz hâlde 3 sayılır); mutasyon tekrarlandı, **kırmızı**.
2. **M-34-K5 ilk denemede hiçbir testi kırmadı.** Sebep: fikstürdeki tek kasa hareketi `DateTime.UtcNow` ile yazılıyordu ve o an UTC günü ile yerel gün aynıydı. → Fikstüre **yerel 00:30** (UTC'de dünün 21:30'u) bir kasa bacağı eklendi; bugünün girişi 2.500, dünün girişi 0 assert'lendi. Mutasyon tekrarlandı, **kırmızı**.
3. **M-34-Y ilk denemede hiçbir testi kırmadı** — Aşkın'ın uyardığı **çok katmanlı koruma tuzağı**: TEZGAHTAR varsayılan matriste "rapor" modülüne sahip olmadığı için 403'ü `[ModulYetkisi]` üretiyordu, `[RolGerekli]` hiç sınanmıyordu. → Yeni test `TEZGAHTAR_rapor_modulu_ACILSA_BILE_rol_kapisina_takilir_403`: SAHİP eliyle modül **açılıyor**, kalan tek kapı rol oluyor. Mutasyon tekrarlandı, **kırmızı**.

`Yabanci_kiracinin_hareketi_hicbir_raporda_gorunmez` bilinçli olarak **mutasyon kanıtı değil, regresyon** etiketlidir (global `HasQueryFilter` zaten koruyor; elle TenantId şartını silmek testi kırmaz — aynı tuzak).

**Son mutasyondan sonra temiz derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı**.

---

## 6. Denetim çıktısı

```
./dotnet/denetim.sh
  ✓ Rapor materyalizasyonu (TB-19)                 43 (üst sınır 43)     ← YENİ SAYAÇ
  ✓ Hareketler.Add (yalnız HareketFabrikasi)        1                    ← rapor deftere YAZMAZ
  ✓ IgnoreQueryFilters (yasak)                      0
  ✓ Find/FindAsync (yasak)                          0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)            4
  ✓ Pages: @onclick'siz ve disabled'sız <button>   71 (tavan 71)
  ✓ Pages: ModalPencere kullanımı                  44 (tavan 45)         ← ikinci düzenleme kalıbı açılmadı
  ✓ Pages: duzenlenenId kalıbı                      1 (tavan 1)
  ✓ Pages/*.razor dosya sayısı                     34 (tavan 34)         ← +3 (gerekçeli)
  ✓ Pages içinde istemci record tanımı            122 (tavan 122)
  ...
DENETİM TEMİZ.
```
İki tavan gerekçeyle yükseltildi: sayfa sayısı 31→34 (üç yeni rapor ekranı, gerekçe betiğe yorum olarak yazıldı). `@onclick'siz düğme` tavanı **yükseltilmedi** — yeni düğmelerin `@onclick`/`disabled` nitelikleri `<button` satırına taşındı. Yeni iki istemci record'u sayfadan `RaporServisi.cs`'e taşındı.

---

## 7. Canlı doğrulama (localhost:5198 — kurucunun 5199'una DOKUNULMADI)

Test kiracısı: **`PAKET34-TEST canli`** (`01M2QRJM4XR7WHZVWX65W4W3YB`).
Kur kaydı için beklendi (ilk 2 denemede `/kur/guncel` "Henüz…" döndü, 3.'de id geldi).

### 7.1 Kurulan fikstür
| # | İşlem | HTTP |
|---|---|---|
| 1 | ALIS · HAVALE · ₺50.000 | 200 |
| 2 | SATIS · NAKIT · ₺5.000 | 200 |
| 3 | SATIS · KART · ₺20.000 | 200 |
| 4 | **Karma fiş** · ₺12.000 nakit + ₺5.000 kart + ₺3.000 veresiye | 200 |
| 5 | SATIS_IADE (kaynak = 2) · ₺5.000 | 200 |
| 6 | TRANSFER şube1 → şube2 | 200 |
| 7 | HURDA_ALIS (gider pusulalı, NIHAI_TUKETICI) · ₺29.770 | 200 |
| 8 | Gün kapanışı (kasa sayımı) | 200 |

> ⚠ 7. adım **ilk denemede 400 döndü** (`MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur`) — 20 g hurda aynı carinin kümülatif eşiğini aşıyordu; sonra `NIHAI_TUKETICI` satıcı bilgisiyle ve 5 g ile geçti. Ayrıca satıcı tipi olmadan `400 — Hurda alışında satıcı kimliği zorunludur (VUK md.234)` alındı. **İkisi de fail-closed kapıların doğru çalıştığının kanıtıdır**, gizlenmedi.

### 7.2 Uç doğrulamaları

| Uç | HTTP | Sonuç |
|---|---|---|
| `GET /raporlar/katalog` | 200 | **15** rapor, hepsinin adı sözlükten çevrilmiş (ham kod basılmıyor) |
| Katalogtaki **15 rotanın hepsi** | **15/15 = 200** | — |
| `GET /raporlar/islem-defteri?limit=50` | 200 | 7 satır, `ozet.tlGiris=45000`, `tlCikis=84770` |
| `GET /raporlar/islem-defteri?limit=5000` | **400** | `Limit 1 ile 500 arasında olmalıdır` |
| `GET /raporlar/islem-defteri?limit=0` | **400** | aynı mesaj |
| `?baslangic=2026-09-17&bitis=2026-09-01` | **400** | `Bitiş tarihi başlangıçtan önce olamaz` |
| `?cariId=<yabancı>` | **404** | `Cari bulunamadı` (boş sonuç DEĞİL) |
| `GET /raporlar/odeme-araci` | 200 | KART ₺25.000/2 · NAKIT ₺17.000/4 · HAVALE giriş ₺0 (çıkış ₺50.000) · veresiye doğan ₺3.000 · bilinmeyen 0/7 · güvenilirlik TAM |
| `GET /raporlar/sube-karsilastirma` | 200 | `transferCiroyaKatilir: false`, ciro **₺45.000**, `transferAdet: 1` |
| `GET /raporlar/personel` | 200 | `kaydedenAd="Test Kullanici"`, işlem 3, ciro ₺45.000, ort. sepet ₺15.000, iade 1, oran 0,3333 · **`satanAd` alanı yanıtta YOK** |
| `GET /raporlar/kasa-defteri` | 200 | `saatDilimi: Europe/Istanbul`, `utcOfsetSaat: 3`, iki gün **ACIK + fark `null`**, bugün giriş ₺42.000 / çıkış ₺84.770 |
| kapanış sonrası aynı uç | 200 | bugün **KAPALI**, `sayilan=0`, `defter=-42770`, **`fark=42770`** |
| TEZGAHTAR jetonu · 6 uç | **6/6 = 403** | islem-defteri, odeme-araci, personel, sube-karsilastirma, kasa-defteri, katalog |

### 7.3 SQL karşılaştırması (birebir)

```sql
SELECT tur, tl_tutari, nakit_tutar, veresiye_tutar FROM hareketler WHERE tenant_id=…;
  ALIS        -50000.00      0.00     0.00
  SATIS         5000.00   5000.00     0.00
  SATIS        20000.00      0.00     0.00
  SATIS        20000.00  12000.00  3000.00     ← karma fiş
  SATIS_IADE   -5000.00   5000.00     0.00
  TRANSFER         0.00      0.00     0.00
  HURDA_ALIS  -29770.00  29770.00     0.00

SELECT COUNT(*), SUM(pozitif), SUM(-negatif) …  →  7 | 45000.00 | 84770.00
```
**Rapor `toplamSatir`=7, `ozet.tlGiris`=45000, `ozet.tlCikis`=84770 → SQL ile BİREBİR.** İşaretler korunmuş (ALIS/SATIS_IADE/HURDA_ALIS negatif, `yon=CIKIS`).

```sql
SELECT odeme_turu, para_birimi, SUM(CASE WHEN tutar>0 THEN tutar ELSE 0 END), COUNT(*)
  FROM kasa_hareketleri WHERE tenant_id=… GROUP BY 1,2;
  KART   TRY  25000.00  2
  NAKIT  TRY  17000.00  4
  HAVALE TRY      0.00  1
```
**R12 çıktısıyla BİREBİR.** Karma fişin nakit (₺12.000) ve kart (₺5.000) bacakları **ayrı** görünüyor; "KARMA" diye bir arac satırı **yok**.

```sql
SELECT eylem, varlik_turu, detay FROM denetim_kayitlari WHERE tenant_id=… AND eylem='OKUMA';
  OKUMA  Cari  liste: 7 kayıt · islem-defteri
  OKUMA  Cari  liste: 1 kayıt · cari-yaslandirma
  OKUMA  Cari  liste: 1 kayıt · vade-takibi
  (her çağrı için bir satır, hassas=1)
```
**KVKK md.12 okuma izi kanıtlandı** (3 uç × 2 çağrı = 6 satır).

### 7.4 MÜHÜR 5 ölçümü — 50.001 ürünlü kiracı

SQL ile 50.000 ürün + 50.000 stok satırı eklendi (`SELECT COUNT(*) FROM urunler → 50001`).

| Uç | HTTP | Süre | Yanıt boyu | Not |
|---|---|---|---|---|
| `/raporlar/stok-degeri?limit=500` (3 deneme) | 200 | **0,416 s · 0,374 s · 0,364 s** | **74.492 bayt** | `toplamSatir=50001`, dönen 500, `dahaFazlasiVar=true`, `toplamHas=458027,48` (**tüm** ürünlerden) |
| `/raporlar/islem-defteri?limit=500` | 200 | 0,035 s | 3.159 B | |
| `/raporlar/cari-yaslandirma?limit=500` | 200 | 0,021 s | 398 B | |
| `/raporlar/odeme-araci` | 200 | 0,018 s | 678 B | |
| `/raporlar/sube-karsilastirma` | 200 | 0,010 s | 341 B | |
| `/raporlar/personel` | 200 | 0,011 s | 321 B | |
| `/raporlar/kasa-defteri` | 200 | 0,013 s | 499 B | |

Eski davranışta `stok-degeri` 50.001 ürünün **tamamını** hem belleğe hem yanıta koyuyordu. Yeni hâlde sunucu 500 satır + sunucu-tarafı toplamlar döndürüyor ve `toplamSatir` ile "daha fazlası var" **beyan ediliyor** (UX-S29). *Not: eski hâlin süresi/boyutu ölçülmedi — kıyas rakamı uydurulmadı.*

### 7.5 Temizlik
- API PID ile durduruldu: `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**. `5199` (kurucunun sunucusu) → 3 satır, **dokunulmadı**.
- `tenant_id` kolonu olan **44 tablonun hepsinden** test kiracısı silindi; 44 tabloda kalan satır **0**.
  - ⚠ İlk silmeden sonra `denetim_kayitlari`'nda **1 satır kalmıştı** (`EKLEME · KurSnapshot`) — API kapanırken kur zamanlayıcısı silme ile yarışmıştı. İkinci turda temizlendi, doğrulandı.
- `tenants` sayısı: **9 → 8** (başlangıç değeri).
- **`QA-TEST-` önekli 3 kalıcı QA kiracısı korundu** (silme öncesi 3, sonrası 3) — verisine dokunulmadı.
- Proje ağacında `.mutorj`/`.bak`/`.tmp` artığı **yok** (mutasyon yedekleri scratchpad'de).

---

## 8. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Faz A kapsamı, Faz B yazılmadı | ✅ | § 2/S4 |
| Rapor mutabakatı: her rapor "tanım + kaynak sorgu + regresyon testi" | ✅ | § 1.3 + `RaporSetiTestleri` (fikstür verisiyle) |
| Günlük/aylık tanımları tek yerde | ✅ | `RaporAraligi.Coz` (M109) |
| Sayı biçimi tr-TR tek yardımcı | ✅ | `Bicim.Tl/Para/Gram`; SVG/InvariantCulture kapanı (M-34-QA3a) |
| **BAYAT OKUMA**: `Entities.cs`'e dokunulmadı | ✅ | § 2/S1 |
| Önkoşul [33] kaldırıldı | ✅ | uygulanmadı |
| Katalog testi "≥N + tekil + gerçek `[HttpGet]`" | ✅ | `Katalogtaki_her_rota_gercek_bir_HttpGet_ucudur` |
| Okuma izi **Faz A'ya** (islem-defteri) | ✅ | M115 + SQL § 7.3 |
| Opsiyonel `null` denetim parametresi YASAK | ✅ | § 2/S6 |
| Yeni controller'lar kapıları AÇIKÇA taşır | ✅ | M-34-Y + § 7.2 (15 uç × TEZGAHTAR 403) |
| Süzgeç kimliği yabancı kiracıda **404** | ✅ | `Yabanci_kiracinin_kimligiyle_suzme_404_doner` + canlı |
| Rapor Merkezi: rozetli gösterim değil **gizleme** | ✅ | Katalog sunucudan gelir; yetkisiz rol katalog ucuna 403 alır, kart hiç çizilmez |
| Limit testi doğru katmanda | ✅ | Servis: `Dogrula()` (DB testi) + HTTP: 400 gövdesi okunuyor |
| TB-19 kapsamı **dürüst** yazıldı | ✅ | § 1.5 + § 2/S7 + `denetim.sh` yorumu + § 9/A1 |
| `denetim.sh:66 BEKLENEN_VARLIK` yanlış cümlesi | ✅ | Bu pakette yeni varlık yok; sayaç iddiası kullanılmadı |
| **MÜHÜR 5 / 34-Ö**: sunucu toplama + limit/ofset/toplamSatir + denetim sayacı + 50k ölçüm | ✅ | § 1.5 + § 7.4 |
| **48-A devri**: `StornoSuzgeci` KULLANILIR, ikinci süzgeç yok, storno ayrı görünür | ✅ | M-34-S + § 7.2 |
| **QA turu 3** iki bulgusu kapandı | ✅ | § 1.6 |
| DoD: her düğme istek atar ya da disabled | ✅ | `IslemDefteri_her_dugme_ya_istek_atar_ya_KILITLIDIR` + denetim.sh tavanı korundu |
| DoD: tek düzenleme kalıbı, yeni `ModalPencere` düzenlemesi yok | ✅ | denetim.sh 44 (tavan 45) |
| DoD: sessiz form yasağı / boş durum / hata bandı | ✅ | Üç ekranda da `HataBandi` + `BosDurum` + `bant-uyari`; `catch` hiçbir yerde listeyi boşaltmıyor (UX-S28) |
| **Tek dokunuş** (dokunulan sayfalar listelendi) | ✅ | § 10 |
| Sozlukler TR+EN, çıplak metin yok | ✅ | 86 anahtar × 2 dil + `SozlukNav` × 3 |
| Sahte API `{}` varsayılanı bırakılmadı | ✅ | `RaporEkranTestleri` 13 uca gerçekçi yanıt (mutabakat zarfı dâhil) |
| Rapor deftere YAZMAZ | ✅ | `Hareketler.Add` sayacı 1 (değişmedi) |
| Kültür: en az bir tr-TR koşan test | ✅ | `GrafikKulturTestleri` +1 |
| `dotnet test` 0 başarısız | ✅ | 2161/2161 |
| `denetim.sh` TEMİZ | ✅ | § 6 |
| Test kiracısı silindi, tenants eski değerde | ✅ | § 7.5 |

### DoD sözleşmesi — hangi varlık nasıl düzenlenir

**Bu paket HİÇBİR YENİ VARLIK/TABLO EKLEMEZ.** Yalnız beş indeks + salt-okur uçlar + ekranlar.
Dolayısıyla PATCH/DELETE/pasife alma yükümlülüğü **doğmaz**; onun yerine:

| Yüzey | Düzenlenebilir mi | Gerekçe / yol |
|---|---|---|
| R10–R14 rapor çıktıları | **HAYIR — salt okur** | Üç ekranda da `<DegismezIpucu Varlik="Rapor">` var: rapor **türetilmiş** veridir, kaynak kayıt düzeltilir. Doğru düzeltme yolu ekranda yazılı (`SayfaDuzeltme.RaporDuzeltmeYolu`). |
| Rapor kataloğu | **HAYIR — koddan** | `RaporKatalogu.Hepsi` derleme zamanı sabitidir; kiracı başına düzenlenebilir olsaydı "sunucuda uç var, katalogda yok" ikinci gerçeği doğardı. Yeni rapor eklemenin tek yolu kod + test. |
| Kaynak kayıtlar (hareket, kasa hareketi, fatura) | Kendi paketlerindeki yollarla | **Finansal varlıkta hard DELETE YOK** — storno 48-A motoruyla; rapor stornoyu ayrı gösterir ve toplamlardan düşer. |
| Gün kapanışı | Var olan "günü yeniden aç" yolu | R11 `durum: YENIDEN_ACILDI` ile bunu **gösterir**. |

---

## 9. Açık kalanlar

- **A1 — TB-19 açık borcu (Bertan koşulu gereği yazılı):** `GunlukOzet`, `AylikOzet`, `PosFaturaMutabakati` hâlâ dönemin TÜM hareketlerini belleğe alır. Dönemle sınırlıdır, satır sayısıyla değil. `denetim.sh` sayacı yeni sınırsız döküm eklenmesini engelliyor ama mevcut üçünü kapatmıyor. **Faz B ya da paket 35 işi.**
- **A2 — Faz B (R15–R19) yazılmadı:** kategori/reyon satış, stok devir + ölü stok, hurda özet, KDV-fatura özeti, iade özeti. Kabul ölçütü olan 19'luk küme **oylanmadı** (mimarın önerisi). Bunlarla birlikte `StokAnalizi.razor` ve `MaliRaporlar.razor` ekranları ve M113/M118/M119/M120 mutasyonları da Faz B'ye devredildi. Ayrıca Mehmet'in K34-2 koşulunun ikinci yarısı — **`GiderPusulasi` türünün `HassasVarliklar`'a eklenmesi** — R17 ile birlikte yapılacak (şimdi eklemek kullanıcısı olmayan bir süzgeç anahtarı doğururdu).
- **A3 — `KarZararServisi.Guvenilirlik`** hâlâ kendi özel kopyasını kullanıyor; `RaporGuvenilirligi`'ne bağlanması paket 35'in işidir (§ 2/S5).
- **A4 — Kararsız test ADIYLA yakalandı:** `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir`. Üç tam süit koşusunun **birinde** kırmızı oldu (`#alan-hata-islem-adet` bulunamadı), izole 3/3 yeşil, sonraki iki tam koşuda yeşil. `SONRAKI-OTURUM-PROMPT.md §6:847`'deki "adı yakalanamayan kararsız test" **bu**. Benim paketimin yüzeyinde değil; POS ekranı sahibine devredildi. **Kanıt: `scratchpad/tfin.log` ve `tamson.log`.**
- **A5 — 37/36 önkoşul zincirine etki (Aşkın için):** Paket **37 (pozisyon/net varlık)** ve **36 (patron paneli)** artık şu hazır parçaları devralıyor: `RaporAraligi` (dönem çözümü), `RaporFiltresi`/`SayfaliSonuc` (sayfalama sözleşmesi), `RaporGuvenilirligi`, `RaporKatalogu` (panel kartları katalogdan beslenebilir), `Web/Servisler/RaporServisi.cs` (tipli istemci). **Yeni önkoşul doğmadı**; 37 ve 36 kendi uçlarını katalogda **açmak zorundadır**, aksi hâlde `Katalogtaki_her_rota_gercek_bir_HttpGet_ucudur` değil ama "sunucuda uç var, ekranda yok" (UX-S25) kapısı işler. `denetim.sh` materyalizasyon üst sınırı **43**'tür: 37/36 rapor sorgusu eklerken ya sunucu-tarafı toplama yazmalı ya sınırı gerekçeyle büyütmelidir.
- **A6 — k6 yük testi koşulmadı:** beş yeni indeks yazma yolunu bir miktar yavaşlatır (her satışta 4 ek indeks bakımı). Spesifikasyonun riskler bölümü p95 karşılaştırması istiyordu; bu pakette **ölçülmedi** (k6 betiği ve taban ölçümü elde yok). **Batuhan'a (DevOps) devredildi.**
- **A7 — "Yazdır" tarayıcı önizlemesi ekranda gözle doğrulanmadı:** `@media print` kuralları mevcut `app.css`'ten (`yazdirma-gizle`) geliyor ve menü/süzgeç/sayfalama blokları o sınıfla işaretlendi; ama **tarayıcı önizlemesi açılıp bakılmadı** (canlı doğrulama API üzerinden yapıldı). QA turu 4'te gözle doğrulanmalı.

---

## 10. Dokunulan sayfalar (MÜHÜR 2 — tek dokunuş)

**Bu dalgada bu paketin açtığı `Pages/*.razor` dosyaları:**
1. `Kuyumcu.Web/Pages/IslemDefteri.razor` — **YENİ**
2. `Kuyumcu.Web/Pages/SatisAnalizi.razor` — **YENİ**
3. `Kuyumcu.Web/Pages/KasaRaporu.razor` — **YENİ**
4. `Kuyumcu.Web/Pages/Raporlar.razor` — **DEĞİŞTİR** (Rapor Merkezi ızgarası + `RaporServisi` enjeksiyonu; mevcut bloklar korundu)

Bileşen/yerleşim: `Bilesenler/PastaGrafik.razor` (QA3 kültür düzeltmesi) · `Layout/KenarCubugu.razor` (3 NavLink) · `Layout/RotaBasliklari.cs` (3 rota).

---

## 11. Değişen dosyalar

**YENİ (12)**
```
dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs                    126
dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs                  69
dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs                470
dotnet/Kuyumcu.Api/Modules/Rapor/FinansRaporlari.cs               167
dotnet/Kuyumcu.Api/Data/Migrations/20260917125715_RaporIndeksleri.cs (+ .Designer.cs)
dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs                      129
dotnet/Kuyumcu.Web/Pages/IslemDefteri.razor                       313
dotnet/Kuyumcu.Web/Pages/SatisAnalizi.razor                       288
dotnet/Kuyumcu.Web/Pages/KasaRaporu.razor                         166
dotnet/Kuyumcu.Tests/RaporOrtakTestleri.cs                        153
dotnet/Kuyumcu.Tests/RaporSetiTestleri.cs                         529
dotnet/Kuyumcu.Tests/RaporYetkiTestleri.cs                        181
dotnet/Kuyumcu.Tests/RaporEkranTestleri.cs                        364
```

**DEĞİŞTİRİLEN**
```
dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs      RaporServisi ctor + CariYaslandirma/StokDegeri TB-19
                                                      + AylikOzet QA3 + VadeTakibi okuma izi + controller imzaları
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs          5 rapor indeksi
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs             SatisRaporServisi + FinansRaporServisi
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                SozlukRaporSeti (86×2) + SozlukNav ×3
dotnet/Kuyumcu.Web/Program.cs                        RaporServisi DI
dotnet/Kuyumcu.Web/Pages/Raporlar.razor              Rapor Merkezi
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor          3 NavLink
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs          3 rota başlığı
dotnet/Kuyumcu.Web/Bilesenler/PastaGrafik.razor      QA3: InvariantCulture
dotnet/denetim.sh                                    TB-19 sayacı + sayfa tavanı 31→34
dotnet/Kuyumcu.Tests/TestOrtami.cs                   TestKurulum.Denetim / .RaporServisi fabrikaları
dotnet/Kuyumcu.Tests/GrafikKulturTestleri.cs         +1 pasta kültür testi
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs           31→34 rota
dotnet/Kuyumcu.Tests/IkonTestleri.cs                 3 yeni menü rotası
dotnet/Kuyumcu.Tests/UX1C3EkranTestleri.cs           RaporServisi DI kaydı
dotnet/Kuyumcu.Tests/{RaporFatura,MadenDefteri,UX4dServis,Maliyet,CariDovizBakiye,
                      Mutabakat,ReyonRapor,VadeTakibi}Testleri.cs   → 18 çağrı yeri fabrikaya bağlandı
```
**DOKUNULMADI:** `HareketFabrikasi.cs`, `Program.cs` (API), `Entities.cs`, `MIMARI-KALAN-PAKETLER-2026-09-15.md`, paket spesifikasyonu, `SONRAKI-OTURUM-PROMPT.md`, 5199 sunucusu, `QA-TEST-` kiracıları.
**COMMIT YAPILMADI** — çalışma ağacında.

---

## 12. Kurucuya / mali müşavire notlar

1. **Personel raporu prim kararına dayanak DEĞİLDİR.** `hareketler.kullanici_id` işlemi **kaydeden** kişidir. Vezne modeli (paket 19) satan ile tahsil edeni ayrıştırana kadar "kim ne sattı" sorusunun cevabı **yoktur**. Rapor başlığı, sütun adı ve ekrandaki uyarı bunu açıkça yazar; sayıyı prim/ceza için kullanmak yanlış karar doğurur.
2. **Nakit payı bilinmeyen eski satırlar.** `nakit_tutar` kolonu eklenmeden önce yazılmış hareketlerin nakit payı **geri kazanılamaz**. Ödeme aracı raporu bunları sıfır saymaz; ayrı "bilinmiyor" sütununda **sayısıyla** gösterir ve raporun güvenilirliğini düşürür. 459 s. VUK kümülatif nakit toplamı bu satırları göremez — bu bir kusur değil, kayıt tarihçesinin sonucudur.
3. **Faz B raporlarının listesi onay bekliyor.** PARITE belgesi "19 rapor" der ama **listeyi hiçbir yerde vermez**; 19'luk küme katman mimarının önerisidir, rakip kanıtı değildir. Faz A **15 rapor** sevk etti. Kalan 5 rapor (kategori satış, stok devir/ölü stok, hurda özet, KDV-fatura özeti, iade özeti) yazılmadan önce kümenin **ekip oyuyla mühürlenmesi** gerekiyor.
4. **KDV özeti (R18, Faz B) SALT OKUR olacaktır** — `faturalar` tablosundaki hazır `kdv_turu`/`has_deger`/`kdv_tutar` alanlarını toplar, **matrah hesaplamaz** (o paket 38'dir). `has_deger` bugün **elle giriliyor**; rapor bunu "elle girilmiş" diye işaretleyecek. **Mali müşavire sorulacak:** KDV md.23/(e) özel matrah beyanında bu rapor tek başına dayanak sayılabilir mi, yoksa paket 38'in otomatiği beklenmeli mi?
5. **Yeni beş indeks yazma yolunu bir miktar yavaşlatır** (her satışta dört ek indeks bakımı). Ölçülmedi (§ 9/A6). Yoğun kiracıya geçmeden k6 yük testi koşulmalı.
6. **Ücretsiz pilot kuralı korundu:** bu pakette hiçbir fiyat/teklif/tahsilat metni üretilmedi.
