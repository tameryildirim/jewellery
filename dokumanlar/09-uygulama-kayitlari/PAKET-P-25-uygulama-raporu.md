# PAKET P-25 — ÖLÇEK + KÜLTÜR + RAPOR TAMAMLAMA + YÜK TESTİ

**Dalga:** DALGA 6 · **Sürüm:** 1.0 · **Tarih:** 2026-09-22
**Roller:** Taşkın (backend) + Ertuğ (frontend) · **Kimlikler:** B-8 · Y-16 · Y-31 · R-6 · B-64
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md:556-570`

---

## § 0 — DEVRALMA BEYANI (Anayasa md.1, gizlenmedi)

Bu oturum P-25'i **sıfırdan başlatmadı.** Çalışma ağacında, aynı paketin
**yarım kalmış bir koşumundan** gelen ve raporu yazılmamış iş bulundu
(dosya damgaları 2026-09-22 03:29–03:51):

| Diskte bulunan | Durum |
|---|---|
| `Modules/Rapor/StokYaslandirmaRaporu.cs` (16.972 B) | vardı |
| `Modules/Rapor/RaporKatalogu.cs` içinde `R15` + `FazAAsgari = 16` | vardı |
| `Modules/Rapor/RaporOrtak.cs` `HareketToplami` (sunucuda toplama) | vardı |
| `denetim.sh` `# >>> P-25` sayaç bloğu (3 sayaç) | vardı |
| `Kuyumcu.Tests/P25OlcekTestleri.cs` (18 test) | vardı |
| `Pages/Raporlar.razor` B-64 geçersizleştirme | vardı |
| **Mutasyon kanıtı** | **YOKTU** |
| **k6 ölçek koşumu + `ham/yuk-testi-<tarih>.json`** | **YOKTU** |
| **Canlı doğrulama (5198)** | **YOKTU** |
| **Uygulama raporu** | **YOKTU** |

Bu oturumun işi: **var olanı DoD'a karşı doğrulamak, eksik kanıtları üretmek,
mutasyonun açtığı test açıklarını kapatmak ve paketi kapatmak.** "Bitti" beyanı
aşağıdaki kanıtlara dayanır; devralınan kısım da yeniden ölçülmüştür.

**Önkoşul ölçümü (varsayım yok):** `PAKET-P-24-uygulama-raporu.md` ve
`PAKET-P-16-uygulama-raporu.md` diskte **MEVCUT** → kapsam daraltılmadı.

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-8** — ölçek + kültür borcu | **KAPANDI** | `denetim.sh:850 RAPOR_MAT_BEKLENEN=22` (28→22) · `denetim.sh:915 SINIRSIZ_TOLIST_TAVAN=145` (151→145) · `denetim.sh:883 KULTURSUZ_TAVAN=27` (44→27) · `denetim.sh:950 BEYAZ_LISTE_BOYU_TAVAN=15` küme eşitliği ile · `RaporOrtak.cs:162-210 HareketToplami` sunucuda `GroupBy` · testler `Gunluk_ozet_sunucuda_toplar_ve_madenleri_ayirir`, `Aylik_ozet_gunluk_kirilimi_yerel_gune_duser`, `Mutabakat_satis_toplamini_sunucudan_alir`, `Ozet_uclari_sunucuda_toplar_kaynak_kanibi` · canlı: `/raporlar/gunluk` 200 (9 ms), `/raporlar/aylik` 200 (48 ms), `/raporlar/pos-fatura-mutabakati` 200 (45 ms) — 50.000 ürünlü kiracıda |
| **Y-16** — 16. rapor (stok yaşlandırma / devir hızı) | **KAPANDI** | `Modules/Rapor/StokYaslandirmaRaporu.cs` (yeni, 16.972 B) · `RaporKatalogu.cs:58 new("R15", …, "raporlar/stok-yaslandirma")` · 9 test (`Yaslandirma_*`, `Devir_hizi_*`, `Partisiz_stok_yasi_bilinmeyen_olarak_ayri_durur`, `Olu_stok_sayfalidir_ve_toplam_sayfadan_turetilmez`) · canlı: `/raporlar/stok-yaslandirma` 200, 5 kova dolu, `toplamAdet=249.995`, `oluStok.toplamSatir=41.721`, `dahaFazlasiVar=true` |
| **Y-31** — rapor sayısı tutarlılığı (15 → 16) | **KAPANDI** | `RaporKatalogu.cs:74 FazAAsgari = 16` · `denetim.sh:998-1007` sayacı **katalogdan** sayar (`KATALOG_SATIR` ≥ 16 **ve** `= FazAAsgari`) · test `Rapor_sayisi_katalogdan_okunur_ve_16dir`, `Katalogdaki_her_rapor_gercek_uca_ve_ada_sahiptir` · canlı: `/raporlar/katalog` 200 → `toplam=16`, `R15` listede |
| **R-6** — k6 yük testi (karar: 1.0, B-8'den SONRA) | **KAPANDI** | `yuk-testi/k6-senaryo.js` **ÖLÇEK MODU** (`MOD=olcek`) · 50.000 ürün + 180.000 hareket / 1.095 gün tohumlandı · ham çıktı `ham/yuk-testi-2026-09-22.json` (9.690 B) · 1.056 istek, **0 adet 5xx**, tüm eşikler geçti (k6 exit 0) |
| **B-64** (P-00 triyajı) — ters tarih / geçersiz süzgeçte eski tablonun geçersizleştirilmemesi | **KAPANDI** (P-25 yüzeyi: rapor ekranı) | `Pages/Raporlar.razor:751,759` iki hat + `:524` `@if (syHata is not null)` render kapısı · test `Gecersiz_esikte_eski_tablo_gecersizlesir_ve_hata_bandi_cikar` (M25-22) · mutasyon M25-8c / M25-8d **KIRMIZI** · canlı: `?oluEsikGun=0` → **400** "Ölü stok eşiği 1 ile 3650 gün arasında olmalı" |

> **B-64 kapsam notu:** bu kimlik 6 rotada görüldü (KAPSAM-ENVANTERI.md:289 DP4-12)
> ve P-13 / P-16 / P-24 / P-25'e birer dilim olarak bağlandı. P-25 **yalnız kendi
> yüzeyini** (rapor ekranları) kapatır; `/vezne` dilimi P-24'ün, diğerleri kendi
> paketlerinindir. Kapsam kilidi gereği başka paketin yüzeyine dokunulmadı.

---

## § 2 — DoD SÖZLEŞMESİ (Bertan koşulu, bağlayıcı)

**Bu pakette YENİ VARLIK / YENİ KAYIT TÜRÜ DOĞMADI.** Migration = HAYIR
(plan bloğu da HAYIR diyor). Dolayısıyla PATCH / DELETE / pasife alma
yükümlülüğü doğuran bir varlık yoktur.

| Üretilen şey | Tür | Düzenlenebilir mi? | Gerekçe / uç |
|---|---|---|---|
| **R15 stok yaşlandırma raporu** | **salt okunur türetim** | Hayır — düzenlenecek bir kayıt yok | Rapor, `stok_partileri` + `stoklar` + `hareketler` üzerinden **hesaplanır**; kendi satırını yazmaz. Düzeltme yolu kaynaktadır: parti girişi yanlışsa ürün/parti ekranından, hareket yanlışsa **StornoServisi** ile. Ekranda `DegismezIpucu` yerine raporun kendi `uyarilar` + `guvenilirlik` alanları ve "yaşı bilinmeyen" ayrımı bu gerekçeyi yazar. |
| **Rapor kataloğu satırı (R15)** | kod sabiti | Hayır — kod | Katalog tek kaynaktır (`RaporKatalogu.Hepsi`); yeni rapor eklemenin **tek yolu** bu dizidir ve `denetim.sh` katalogla `FazAAsgari` eşitliğini kapıya bağlar. |
| **Ekran süzgeçleri** (`#sy-esik`, `#sy-pencere`) | kullanıcı girdisi | **Evet** | Ortak girdi ailesi `Bilesenler/Form/Sayi.razor`; her değişiklik sunucuya **yeni istek** atar (test `Esik_degisince_sunucuya_yeni_istek_atilir`, M25-23), geçersiz değerde **alan altı Türkçe mesaj** basar ve **eski tablo düşer** (M25-22). Sessiz etkisizlik yok. |

**Finansal varlıkta hard DELETE:** yapılmadı, yapılamaz — bu paket hiçbir
finansal tabloya yazmaz. **Denetim kaydı:** `StokYaslandirmaServisi` liste
okumasını `DenetimServisi.ListeOkumaKaydet` ile kaydeder (`StokYaslandirmaRaporu.cs:253`);
servis yeniden yazılmadı, **çağrıldı**. **Yetki:** `RaporKatalogu`'nda R15
`MuhasebeUstu` (`SAHIP`/`MUDUR`/`MUHASEBE`) rolüne bağlı; jetonsuz çağrı canlıda
**401** döndü.

---

## § 3 — DoD EK 1/2 KARŞILAMA (kurul tur 1 · mühür 2)

| Madde | Karşılama |
|---|---|
| (1) Her düğme ya istek atar ya disabled + gerekçe | R15 kartında düğme yok; **iki sayı alanı** var ve ikisi de değişince istek atar (M25-23). Geçersizde istek **atılmaz** ve sebebi yazılır (M25-22 `Assert.Equal(oncekiIstek, …)`). |
| (2) Tek düzenleme kalıbı | Yeni `duzenlenenId` kalıbı **açılmadı**; `ModalPencere` düzenleme kullanımı **artmadı** (denetim.sh sayaçları TEMİZ). |
| (3) Her formda alan altı Türkçe mesaj | `HataBandi Id="sy-hata"` + `Dil.T.RaporSeti.SinirUyarisi`. |
| (4) Yıkıcı eylem onayı | Bu pakette yıkıcı eylem yok. |
| **TEK DOKUNUŞ — dokunulan sayfalar** | **`Pages/Raporlar.razor`** (tek sayfa). `KarZarar.razor`, `SatisAnalizi.razor`, `KasaRaporu.razor` plan dosya listesinde geçiyordu ama **AÇILMADI** — kültür borcu bu sayfalarda değil, `Servisler/RaporServisi.cs` + `UyumServisi.cs` sorgu dizesi kurulumundaydı; gereksiz dokunuş yapılmadı (mühür 2). |

---

## § 4 — SPESİFİKASYON ↔ KOD SAPMALARI (ve gerekçeleri)

### 4.1 Plandaki sayılar bayattı; taban **taze ölçüldü**, belgeden kopyalanmadı

| Plan (2026-09-18) | Bugün ölçülen | Neden farklı |
|---|---|---|
| `KULTURSUZ_TAVAN = 44` (sat. 729) | **27** (`denetim.sh:883`) | 14 `ToString("yyyy-MM-dd")` çağrısı `Bicim.Makine(...)`'ye geçti; kalan 3 fark dosya konumu/ölçüm kapsamından (`Pages`+`Bilesenler`+`Servisler`). |
| `RAPOR_MAT_BEKLENEN = 43` (sat. 706) | **22** (`denetim.sh:850`) | Eski ölçüm **yorum satırlarını ve `Take`/`GroupBy`'lı sorguları da** sayıyordu. Yeni ölçüm perl ile yorumu siler, `Take`/`GroupBy`'ı eler: P-25 öncesi **28**, sonrası **22**. |
| "171 sınırsız `ToListAsync`" | **145** (`SINIRSIZ_TOLIST`, P-25 öncesi 151) | Aynı sebep. **Dürüstlük notu:** 171 → 145 bir "26 düşüş" DEĞİLDİR; ölçüm yöntemi değişti. Yöntem sabitken düşüş **151 → 145**'tir ve sayaç artık yalnız azalabilir. |
| Dosya: `denetim.sh:694-731` | gerçek konum **850-1007** | Dosya araya giren paketlerle büyüdü (5.145 satır). Kod okundu, satır numarası düzeltildi. |

Plan ile bu koşullar arasında **çelişki bulunmadı**; plan üstün tutuldu.

### 4.2 "16. rapor katalogdan sayılır" — sayaç kod içi ikiziyle birlikte

`denetim.sh` sayacı belgeye değil `RaporKatalogu.cs`'ye bakar; ayrıca test
(`Rapor_sayisi_katalogdan_okunur_ve_16dir`) `FazAAsgari == Hepsi.Length`
eşitliğini kilitler. İkisi birden olmadan katalog sessizce küçülebilirdi.

### 4.3 Personel raporu — "KAYDEDEN bazlı"

Plan DoD'u karşılandı: `SatisRaporlari.cs:258,292-308` alan adı `kaydedenAd`,
`olcut = "KAYDEDEN_KULLANICI"`, `uyari = "SATAN_DEGIL_KAYDEDEN"`; sözlükte
TR "Kaydeden…", EN "…Record…"; test `Personel_raporu_kaydeden_bazli_adiyla_cikar`
"Satıcı"/"Satan"/"Seller"/"Salesperson" kelimelerini **yasaklar**. Canlı yanıt
`olcut: KAYDEDEN_KULLANICI`. **"Satıcı bazlı" iddiası kullanılmadı** — P-08'in
`personel_id = 'SATAN'` kilidi bu pakette kanıtlanmadı.

### 4.4 k6 ilk koşum GEÇERSİZDİ — gizlenmedi

İlk ölçek koşumunda 10 sanal kullanıcı **tek jetonla** koştu; oran sınırı
isteklerin **%92,17'sini 429**'a düşürdü (1.178/1.278). Ölçülen şey ürünün hızı
değil, oran sınırının hızıydı. Senaryoya çoklu jeton desteği eklendi
(`JETON` virgülle ayrılır — gerçek dükkânda 10 personelin 10 hesabı olur),
9 ek kullanıcı açıldı ve koşum tekrarlandı. **Raporlanan rakamlar ikinci
koşumundur; ilk koşum çöpe atılmadı, burada yazılıdır.**

---

## § 5 — DEĞİŞİKLİKLER

### 5.1 Bu oturumda yazılan

| Dosya | İş |
|---|---|
| `dotnet/yuk-testi/k6-senaryo.js` | **ÖLÇEK MODU** eklendi (`MOD=olcek`): dolu kiracıyı hazır jeton(lar)la ölçer, kiracı açmaz; `rapor_sure` ayrı `Trend`'i; ölçek eşikleri ayrı ve **yazılı** (`okuma p95 < 1500 ms`, `rapor p95 < 3000 ms`); çoklu jeton (oran sınırı kullanıcı başınadır). POS modu **değişmedi** (varsayılan). |
| `dotnet/Kuyumcu.Tests/P25OlcekTestleri.cs` | `Yaslandirma_yabanci_kiracinin_partisini_saymaz` **güçlendirildi** (M25-2 açığı). |
| `dotnet/Kuyumcu.Tests/RaporEkranTestleri.cs` | `Gecersiz_esikte_eski_tablo_gecersizlesir_ve_hata_bandi_cikar` **güçlendirildi** + `KaynakYolu` yardımcısı (M25-8 açığı). |
| `dokumanlar/09-uygulama-kayitlari/ham/yuk-testi-2026-09-22.json` | k6 ham çıktısı (**sürümlendi**). |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-25-uygulama-raporu.md` | bu rapor. |

### 5.2 Devralınan (yarım koşumdan; doğrulandı)

`Modules/Rapor/StokYaslandirmaRaporu.cs` (yeni) · `Modules/Rapor/RaporKatalogu.cs`
(R15 + `FazAAsgari=16`) · `Modules/Rapor/RaporOrtak.cs` (`HareketToplami`) ·
`Modules/Rapor/RaporModulu.cs` · `Modules/Rapor/Mutabakat.cs` ·
`Kuyumcu.Core/I18n/Sozlukler.cs` (R15 + yaş kovası TR/EN) ·
`Kuyumcu.Web/Servisler/RaporServisi.cs` (`Bicim.Makine`) ·
`Kuyumcu.Web/Pages/Raporlar.razor` (R15 kartı + B-64) · `dotnet/denetim.sh`
(`# >>> P-25` bloğu) · `Kuyumcu.Tests/P25OlcekTestleri.cs`.

### 5.3 `git status --porcelain | grep -v -E "/(bin|obj)/"` (P-25 yüzeyi)

```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs
?? dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs
?? dotnet/Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs
?? dotnet/Kuyumcu.Api/Modules/Rapor/Mutabakat.cs
?? dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs
?? dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs
?? dotnet/denetim.sh
?? dotnet/yuk-testi/
```
(`??` = depoda hiç izlenmemiş dosyalar; **COMMIT YAPILMADI**, git komutu
çalıştırılmadı — yalnız `git status` okundu.)

---

## § 6 — MİGRATION

**HAYIR** — plan bloğu da HAYIR diyor; şema değişikliği gerekmedi.
`__EFMigrationsHistory` **65 → 65** (değişmedi, SQL ile doğrulandı).

---

## § 7 — DERLEME VE TESTLER

| Ölçüm | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** (6,56 sn) |
| P-25 filtreli koşum (`FullyQualifiedName~P25Olcek`) | **18/18 yeşil** |
| Rapor ekranı koşumu (`~RaporEkran`) | **19/19 yeşil** |
| **Tam süit** `dotnet test … --logger trx` | **3835 / 3835 Geçti · 0 Başarısız · 0 Atlanan** (57,66 sn) |
| trx | `…/scratchpad/trx-pP-25-son/_Mac_2026-09-22_04_16_41.trx` |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (exit 0) |

**P-25'in getirdiği test sayısı: 23** — `P25OlcekTestleri.cs` 18 (`M25-1…M25-18`)
+ `RaporEkranTestleri.cs` 5 (`M25-19…M25-23`). Bu oturumda **yeni [Fact]
eklenmedi**; iki mevcut test mutasyon açığı yüzünden **güçlendirildi**.

### denetim.sh P-25 sayaçları (CIRCIR — yalnız azalır)

```
  ✓ Sınırsız rapor dökümü (TB-19/B-8)        22 (üst sınır 22)
  ✓ Kültürsüz biçimli ToString (Web)         27 (tavan 27)
  ✓ Sınırsız ToListAsync (Modules · B-8)     145 (tavan 145)
  ✓ Sınırsız okuma beyaz listesi             15 (tavan 15 · 2026-09-22)
  ✓ Rapor kataloğu (Y-31 · katalogdan)       16
```

Beyaz liste **küme eşitliği** ile sınanır (yalnız sayı değil): bir yerde dökümü
kapatıp başka yerde açmak kapıyı **durdurur**; kapanan ad listeden elle çıkarılır
(liste çürümez). Ölçüm komutları betiğe **yorum olarak** yazılıdır.

---

## § 8 — MUTASYON TABLOSU

Yöntem: kaynak `scratchpad/mut-yedek/P-25-son/` altına kopyalandı →
`perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` →
yedek geri kopyalandı → **`cmp` ile birebir** doğrulandı.

| Kod | Dosya · değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M25-1** | `RaporOrtak.cs` · `AltinHas` mutlak dalı kaldırıldı (`mutlak ? HasMutlak : HasToplam` → `HasToplam`) | `Gunluk_ozet_sunucuda_toplar_ve_madenleri_ayirir` | cmp birebir |
| **M25-2** | `StokYaslandirmaRaporu.cs` · açık kiracı süzgeci silindi | ⚠ **HİÇBİRİ — TEST AÇIĞI** | cmp birebir |
| **M25-2b** | (aynı değişiklik, test güçlendirildikten sonra) | `Yaslandirma_yabanci_kiracinin_partisini_saymaz` | cmp birebir |
| **M25-3** | `StokYaslandirmaRaporu.cs` · eşik kapısı etkisizleştirildi (`<1 \|\| >3650` → `< -99999`) | `Yaslandirma_limit_ve_esik_kapisi_serviste_durur` | cmp birebir |
| **M25-4** | `StokYaslandirmaRaporu.cs` · `kalanAdet > 0m` → `>= 0m` (payda sıfır) | `Devir_hizi_rafta_mal_yokken_null_doner` (+ izolasyon testi) | cmp birebir |
| **M25-5** | `RaporKatalogu.cs` · `FazAAsgari = 16` → `15` | `Rapor_sayisi_katalogdan_okunur_ve_16dir` | cmp birebir |
| **M25-6** | `RaporServisi.cs` · `Bicim.Makine(baslangic)` → `baslangic?.ToString(…)` | *(derleme hatası CS0023 — mutasyon geçersiz, tekrarlandı)* | cmp birebir |
| **M25-6b** | `RaporServisi.cs` · `Bicim.Makine(baslangic)` → `baslangic.ToString("yyyy-MM-dd")` | `Rapor_istemcisi_sorgu_dizesini_makine_biciminde_kurar` | cmp birebir |
| **M25-7** | `Sozlukler.cs` · `"R13" => AdPersonel` → `"Satıcı bazlı satış"` | `Personel_raporu_kaydeden_bazli_adiyla_cikar` | cmp birebir |
| **M25-8** | `Raporlar.razor` · geçersiz süzgeçte `yaslandirma = null` silindi | ⚠ **HİÇBİRİ — TEST AÇIĞI** | cmp birebir |
| **M25-8b** | `Raporlar.razor` · `@if (syHata is not null)` render kapısı silindi | ⚠ **HİÇBİRİ — TEST AÇIĞI** | cmp birebir |
| **M25-8c** | M25-8 tekrarı (test güçlendirildikten sonra) | `Gecersiz_esikte_eski_tablo_gecersizlesir_ve_hata_bandi_cikar` | cmp birebir |
| **M25-8d** | M25-8b tekrarı (test güçlendirildikten sonra) | `Gecersiz_esikte_eski_tablo_gecersizlesir_ve_hata_bandi_cikar` | cmp birebir |
| **M25-9** | `RaporServisi.cs` · `Bicim.Makine(bitis)` → `bitis.ToString("yyyy-MM-dd")` | **`denetim.sh` KIRMIZI:** `✗ Kültürsüz biçimli ToString (Web) 28 (tavan 27 — ARTIŞ)` → `DENETİM BAŞARISIZ` | cmp birebir |

Son mutasyondan sonra **TEMİZ derleme** (`0 Hata · 0 Uyarı`) ve **TEMİZ denetim**
koşuldu.

### İki test açığının kök nedeni ve nasıl kapatıldığı (md.1)

**M25-2 (kiracı süzgeci):** üç savunma hattı var — (1) global sorgu süzgeci,
(2) servisteki açık `TenantId` yüklemi, (3) `KiraciYazmaDenetcisi`. Test yalnız
(1)'i sınıyordu, o yüzden (2) silinince hiçbir şey kırılmıyordu. Test artık
**çapraz çağrıyı** (bağlam A, jeton B) koşuyor — `KiraciIhlaliException` ile
**fail-closed durduğu ölçüldü ve yazıldı** — ve (2)'yi **kaynak düzeyinde**
kilitliyor. Mutasyon tekrarlandı: **KIRMIZI**.

**M25-8 (B-64):** iki hat birbirini gölgeliyordu — geçersiz süzgeçte veriyi
düşürmek **ve** hata varken tabloyu hiç çizmemek. Tek tek silmek davranışı
değiştirmiyordu (yedeklilik **bilerek** korunuyor). Test artık her iki hattı
kaynakta adıyla kilitliyor. İki mutasyon da tekrarlandı: **KIRMIZI**.

---

## § 9 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

**Kurulum:** API 5198'de `--no-build` ile arka planda koşturuldu
(`Sistem__Anahtari` yalnız bu süreç için ortam değişkeninden verildi; değeri
çıktıya yazılmadı). Kayıt ucu davet istediği için önce `POST /sistem/davetler`
(**201**) ile tek kullanımlık davet üretildi.

**Test kiracısı:** `PAKET-P-25-TEST olcek` · `tenantId = 01M33APCWAHWS5XC60V42S77R2`

### 9.1 Tohumlama (SQL, yük testi için)

| Tablo | Satır | SQL kanıtı |
|---|---|---|
| `urunler` | **50.000** | `SELECT COUNT(*) … WHERE tenant_id=…` → 50000 |
| `stoklar` | 50.000 | → 50000 |
| `stok_partileri` | 50.000 | → 50000 |
| `hareketler` | **180.000** | → 180000 |
| hareket tarih aralığı | **1.095 gün (3 yıl)** | `DATEDIFF(MAX(tarih),MIN(tarih))` → 1095 |

### 9.2 Uç doğrulaması (dolu kiracıda)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `GET /raporlar/stok-yaslandirma` | 200 + 5 kova | **200** (0,613 sn) | `toplamAdet=249995`, kovalar `G0_30=6890 · G31_90=13809 · G91_180=20700 · G181_365=42495 · G365_USTU=166101`, `oluStok.toplamSatir=41721`, `limit=100`, `dahaFazlasiVar=true` (UX-S29) |
| `GET /raporlar/gunluk` | 200, sunucuda toplanmış | **200** (0,009 sn) | `alis.adet=40`, `alis.toplamTl=201500,00`, `netHasGramHareketi=90,0`, maden dağılımı ALTIN |
| `GET /raporlar/aylik` | 200 | **200** (0,048 sn) | günlük kırılım sunucudan |
| `GET /raporlar/pos-fatura-mutabakati` | 200 | **200** (0,045 sn) | — |
| `GET /raporlar/personel` | 200, KAYDEDEN | **200** (0,014 sn) | `olcut=KAYDEDEN_KULLANICI`, `uyari=SATAN_DEGIL_KAYDEDEN` |
| `GET /raporlar/katalog` | 200, **16** | **200** (0,049 sn) | `toplam=16`, `faz=A`, `R15` listede |
| `…/stok-yaslandirma?oluEsikGun=0` | 400 | **400** | "Ölü stok eşiği 1 ile 3650 gün arasında olmalı" |
| `…/stok-yaslandirma?limit=5000` | 400 | **400** | "Limit 1 ile 500 arasında olmalıdır" |
| `…/stok-yaslandirma` (jetonsuz) | 401 | **401** | — |
| `…/stok-yaslandirma-yok` | 404 | **404** | — |

### 9.3 k6 ölçek koşumu (R-6) — ham çıktı sürümlendi

Komut:
`MOD=olcek TABAN=http://localhost:5198 JETON=<10 jeton> KULLANICI=10 k6 run --summary-export ham/yuk-testi-2026-09-22.json yuk-testi/k6-senaryo.js`

| Ölçü | Değer |
|---|---|
| Süre / sanal kullanıcı | 55,8 sn · 1→10 VU rampalı |
| İstek | 1.056 (19,19 req/s) · 352 tur |
| `checks` | **1.056 / 1.056 başarılı (%100)** |
| **`sunucu_hatasi_5xx`** | **0 / 1.056 — %0,00** (eşik `rate<0.001` ✓) |
| `okuma_sure` (ürün listesi + arama) | avg 11,66 ms · med 10,92 · **p95 21,61 ms** (eşik 1500 ✓) |
| `rapor_sure` (günlük/aylık/yaşlandırma/stok değeri/işlem defteri) | avg 210,2 ms · med 54,24 · p90 683,73 · **p95 705,44 ms** · max 823,9 (eşik 3000 ✓) |
| `oran_siniri_429` | %11,17 (118/1.056) — **tasarlanmış davranış**, hata değil |
| k6 çıkış kodu | **0** (tüm eşikler geçti) |
| Ham çıktı | `dokumanlar/09-uygulama-kayitlari/ham/yuk-testi-2026-09-22.json` (9.690 B) |

> **Rakamların sınırı (md.1):** bu sayılar bir **geliştirme dizüstünün** ve
> **Docker MySQL**'in sayılarıdır; üretim kapasitesi İDDİA EDİLMEZ. Şube indeksi
> eksikliği riski (plan risk satırı) **gerçekleşmedi** — 50.000 ürün + 180.000
> hareketle tek bir 5xx bile çıkmadı.

### 9.4 Temizlik

| Adım | Sonuç |
|---|---|
| API (PID 21748) durduruldu | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** |
| 5199 (kurucunun sunucusu) | **dokunulmadı**, hâlâ dinlemede (2 satır) |
| Test kiracısı silindi | `tenant_id` kolonu olan **63 tablonun** hepsinden `DELETE`; `urunler / hareketler / kullanicilar` kalan **0 / 0 / 0** |
| Davet kaydı silindi | `kiraci_davetleri` kalan **0** |
| **`tenants` sayısı** | **9 → 9** (başlangıçtaki değere döndü) |
| QA fikstürleri | `QA-TEST-%` kiracı sayısı **4** — **dokunulmadı** |
| `__EFMigrationsHistory` | **65 → 65** |

---

## § 10 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan § 1 md.1 MEKANİZMA ÖNCE + sayaç | ✔ | `denetim.sh` `# >>> P-25` bloğu, 3 sayaç + katalog kapısı |
| Plan § 1 md.2 SAYAÇ TABANI ÖLÇÜLÜR | ✔ | Ölçüm komutları betikte yorum; taban taze (§ 4.1) |
| Plan § 1 md.8 tek yazma yolu | ✔ | Bu paket deftere yazmaz |
| Plan § 1 md.9 `Pages` record tavanı | ✔ | Pages'te yeni istemci record açılmadı; denetim TEMİZ |
| Plan § 1 md.10 migration | ✔ | Migration üretilmedi (65 → 65) |
| Plan § 7 TEK EL | ✔ | `denetim.sh` yalnız işaretli `P-25` bloğuna dokunuldu; `betikler/qa-turu.js` açılmadı |
| DoD (a) her iş kuralı için test | ✔ | 23 test (§ 7) |
| DoD (b) mutasyon kanıtı | ✔ | 13 mutasyon, 2 test açığı kapatıldı (§ 8) |
| DoD (c) 5198 canlı doğrulama | ✔ | § 9.2 (HTTP kodları + SQL sayımı) |
| DoD (d) `denetim.sh` TEMİZ | ✔ | exit 0, "DENETİM TEMİZ." |
| DoD (e) tam süit 0 başarısız | ✔ | 3835/3835 |
| DoD (f) test kiracısı silindi | ✔ | tenants 9 → 9; QA-TEST-* korundu |
| DoD (g) uygulama raporu | ✔ | bu dosya |
| Mekanizma kuralı: `SINIRSIZ_TOLIST` · `KULTURSUZ_TAVAN` · `BEYAZ_LISTE_BOYU` | ✔ | üçü de betikte, tavanlar **azaldı**, hiçbiri artırılmadı |
| Plan DoD: `GunlukOzet`/`AylikOzet`/`PosFaturaMutabakati` sunucuda toplar | ✔ | `RaporOrtak.HareketToplami.Coz` (`GroupBy`), 4 test, canlı 200 |
| Plan DoD: 16. rapor + `FazAAsgari` 15 → 16 | ✔ | `RaporKatalogu.cs:58,74`; canlı `toplam=16` |
| Plan DoD: personel raporu "KAYDEDEN bazlı" | ✔ | § 4.3 |
| Plan DoD: k6, 50k ürün + 3 yıl, **B-8'den SONRA**, ham çıktı sürümlü | ✔ | § 9.1 / § 9.3 |
| Beyaz liste tarihli ve azalan | ✔ | `BEYAZ_LISTE_TARIHI="2026-09-22"`, tavan 15, küme eşitliği |
| Kapsam kilidi (yalnız B-8/Y-16/Y-31/R-6/B-64) | ✔ | Yeni ekran/modül açılmadı |
| COMMIT YOK · `rm`/`mv`/git yok | ✔ | Yalnız `git status` okundu |
| Sır/parola çıktıya yazılmadı | ✔ | MySQL parolası ve sistem anahtarı yalnız değişkende |
| 5199'a dokunulmadı | ✔ | § 9.4 |

---

## § 11 — AÇIK KALANLAR

1. **`SINIRSIZ_TOLIST = 145`** hâlâ yüksek. Bu paket **rapor yüzeyini** kapattı;
   kalan 145 döküm diğer modüllerde. Tavan cırcır olduğu için **artamaz**, ama
   sıfırlanması P-26 ve sonrası paketlerin işidir. **Devredildi: sonraki dalgalar.**
2. **`KULTURSUZ = 27`** kalıntı. 14'ü kapandı; kalan 27'nin insan/makine bağlamı
   tek tek incelenmeli. **Devredildi: P-33 (kılavuz/biçim turu).**
3. **Beyaz listedeki 15 sınırsız okuma** bir izin değil **borç kütüğüdür**
   (`KarZararServisi.Olc`, `MaliyetMotoru.AcikPartiler`, `RaporModulu.CariYaslandirma`,
   `RaporModulu.StokDegeri` …). Her satır "burada hâlâ sınırsız okuma var" demektir.
4. **B-64'ün diğer 5 rotası** P-13 / P-16 / P-24 kapsamındadır; P-25 yalnız rapor
   ekranını kapattı (kapsam kilidi).
5. **Artık dosyalar** (hook `rm`'i bloklar, **K-4 kurucudadır**):
   `dotnet/denetim.sh.mutbak` (0 bayt, 18 Eylül) ·
   `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs.mutorj` (0 bayt, 21 Eylül).
   **Bu oturum artık bırakmadı.**
6. **`docker-compose.yml` 3. satırındaki yorumda MySQL parolası açık duruyor**
   (K-5'e ek madde, önceki paketlerden devam). Bu oturum parolayı
   `grep … | tail -1` ile aldı ve hiçbir yere yazmadı.

---

## § 12 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire mevzuat sorusu YOK.** Bu paket ölçek, kültür ve raporlamadır;
  hiçbir vergi/KDV/MASAK hükmü koda sabitlenmedi.
- **Kurucuya:** yük testi rakamları (§ 9.3) **bu dizüstünün** rakamlarıdır.
  Müşteriye/pilota "şu kadar hızlı" denecekse **hedef donanımda tekrar
  koşulmalıdır**; komut ve senaryo hazırdır (`MOD=olcek`). Bu rapordaki hiçbir
  sayı üretim kapasitesi taahhüdü değildir.
- **Kurucuya:** 5199 inceleme sunucusu bu paketin kodunu **henüz içermiyor**;
  yeni yayın alınana dek R15 kartı orada görünmez (yayın kurucunun/Batuhan'ın
  adımıdır).
- **Kurucuya:** § 11.5'teki iki artık dosyanın silinmesi (K-4) hâlâ bekliyor.

---

## § 13 — BÜTÇE

**Tahmin:** 420 ajan-dk. **Gerçekleşen (bu oturum):** ≈ 95 ajan-dk.
**Sebep:** paketin kod ayağı yarım kalmış bir koşumdan devralındı (§ 0);
bu oturum doğrulama + eksik kanıt (mutasyon, k6, canlı, rapor) üretti.
İki koşumun toplamı plan tahminine yakındır ama **ölçülmediği için
toplanmadı** (md.1 — tahmin edilmez).
