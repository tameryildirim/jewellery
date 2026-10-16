# PAKET MASAK-370 — Uygulama raporu

**Konu:** MASAK kimlik eşiği 185.000 → 370.000 TL (07.10.2026'dan itibaren)
**Tarih:** 7 Ekim 2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · şerit t2 (API 5162, vekil 5163, CDP 9905/9906, test DB `kuyumcu_test_t2`)
**Kaynak (tek doğruluk):** `dokumanlar/09-uygulama-kayitlari/MEVZUAT-KUYUMCU-BELGE-2026-10-07.md`
**Durum:** BİTTİ (tur 3 — doğrulayıcının KRİTİK /banka dekont çıkmazı ve ORTA Peşin/PRO tutarsızlığı kapatıldı; ayrıntı § 14). COMMIT YOK. Silinen dosya yok.

## 1. Mevzuat dayanağı (yalnız rapordaki birincil kaynak)

- Tedbirler Yönetmeliği md.5/1-b: "yüzseksenbeşbin TL" yerine "üçyüzyetmişbin TL". Kaynak 11845 sayılı CK md.1, RG 07.10.2026 sayı 33393. Yürürlük CK md.6: yayımı tarihinde.
- Birincil kaynaklar: https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf ve mevzuat.gov.tr 21.5.200713012 (dipnot 9). Kaynak raporda iki metin de okunmuş.
- Yeni eşik, oran ya da tarih uydurulmadı. 30.000 TL elektronik transfer sınırı ve md.16/38 kuyumcuya uygulanmadığı için koda dokunulmadı.
- 200.000 TL "işlem amacı beyanı" kademesi (TEYİTSİZ, `yasal1-tohum-08`) **değiştirilmedi**. Gerekçe kurucu kararı md.15 (a): mali müşavir cevabına kadar olduğu gibi kalır.

## 2. Yapılanlar

| # | Kalem | Ne yapıldı |
|---|---|---|
| 1 | Migration `20261007140228_MasakKimlikEsigi370` | `yasal1-tohum-07` (185.000) satırının `gecerlilik_bitis` alanı `2026-10-07` oldu. Aralık [başlangıç, bitiş) olduğundan 185.000'in son günü 06.10.2026'dır. Bu satırın değeri, başlangıcı ve damgası yeniden yazılmadı. Yeni satır `masak370-01` eklendi: 370.000 · başlangıç 2026-10-07 · bitiş açık · **DOGRULANDI** · dayanak "Tedbirler Yönetmeliği md.5/1-b (11845 sayılı CK, RG 07.10.2026/33393)" · kaynak RG PDF'i. Kaynak künyesi (RG 07.10.2026/33393 · CK 11845 · Yönetmelik md.5/1-b, yürürlük md.6) `ic_not` alanında. `denetim_kayitlari` tablosuna sistem kiracısıyla iz yazıldı. İleri tarihli bir satır varsa yeni satır onun başlangıcında kapanır. Aynı gün başlayan bir satır varsa migration sessizce geçmez, UNIQUE kısıtında durur. Down adımı önce yeni satırı siler, sonra eski satırın bitişini geri açar. |
| 2 | Aynı sürümde varsayılanlar | `appsettings.json` Masak:KimlikEsigi 370000.0 oldu. `MasakEsikleri.KimlikEsigi` varsayılanı `370_000m` oldu. Açılış karşılaştırması geçti: 5162'de API `HAZIR`, günlükte `YASAL_PARAM` satırı yok. |
| 3 | Yorum metinleri | `SatisModulu.cs` (5 yer), `MasakVarliklari.cs`, `GiderPusulasi.cs`, `YasalParametre.cs`, `EkstreModulu.cs`, `BankaModulu.cs`, `MasakKapisi.cs` ve `MasakModulu.cs` güncellendi. Kural anlatan yerler dönemden bağımsız ya da 370.000 oldu. Tarihsel örnekler "o günkü eşik (185.000)" olarak bırakıldı. Ekranlar eşiği sunucudan alıyor; `Kuyumcu.Web` içinde `185` geçen satır yok. Bu grep ile doğrulandı. |
| 4 | Testler | 13 yeni test eklendi (`Masak370Testleri.cs`). Varsayılan eşiği ya da bugünün eşiğini okuyan 17 test dosyasının sabit verisi güncellendi (bkz. § 6). |
| 5 | 200.000 kademesi | Dokunulmadı. Davranış testle ve canlıda belgelendi: 07.10.2026'dan itibaren 200.000–369.999,99 TL arasında kimlik, md.5/1-b eşiği yüzünden değil, amaç beyanı kademesi yüzünden istenmeye devam ediyor. |
| 6 | /yasal-sinirlar | `YasalParametreServisi.cs:317` (bugün başlayan değeri reddetme) değiştirilmedi. Yol migration oldu. Ekranda yeni satır "Doğrulandı", 370.000 ₺ ve üzeri, "Ne zamandan beri: 7 Ekim 2026" ve dayanak + "Resmî kaynağı aç" ile görünüyor. Geçmiş listesinde "14 Ocak 2023 – 6 Ekim 2026: 185.000 ₺" satırı var. Başsız Chrome görüntüleri § 9'da. |

### 2a. Sapmalar ve gerekçeleri (spesifikasyonun hedefine sadık uyarlamalar)

1. **Mesajda gerçek tetikleyici eşik (MasakKuralMotoru + MasakKapisi).**
   - Sorun: 07.10.2026'dan sonra kimlik eşiği (370.000) amaç beyanı eşiğinin (200.000) üstüne çıktı. Eski kod 250.000 TL'lik işlemi "370.000 TL ve üstü işlemde kimlik tespiti zorunludur" diye reddederdi. Kuyumcu için anlaşılmaz ve yanlış bir mesajdı (UX-S30).
   - Yapılan: `MasakEsikleri.KimlikIstenenEnDusukTutar = Math.Min(KimlikEsigi, AmacBeyaniEsigi)` hesaplanan özelliği eklendi. Bu özellik yapılandırmadan okunmuyor, tabloya yazılmıyor.
   - Mesaj artık tutar kimlik eşiğinin altındaysa şunu söylüyor: "200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur".
   - Kümülatif notu ("eşik X TL") ve Peşin Müşteri red mesajı da bu tutarı kullanıyor.
   - 07.10.2026 öncesinde 185.000 < 200.000 olduğundan davranış ve mesaj birebir aynı. Mesaj eski hâlinden yalnız 200.000–369.999,99 bandında ayrılıyor.
2. **Peşin Müşteri art arda fiş penceresi.**
   - `PesinPencereKapisi` daha önce `KimlikEsigi`ni alıyordu. Şimdi `KimlikIstenenEnDusukTutar`ı alıyor.
   - Aksi hâlde pencere sorusu 200.000'de kimlik istenirken 370.000'e kadar susardı. Bu, fail-closed davranışı gevşetirdi.
   - Mevcut `PesinArtArdaTestleri.Pencere_MASAK_kimlik_esigine_ulasinca_soru` testi bu davranışı koruyor (M1-6).
3. **Program.cs açılış karşılaştırması.**
   - Tam süit tur 1'de `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` kırmızıydı.
   - Sebep: şema eskiyken (bu migration bekliyorken) yeni appsettings (370.000) eski tablo (185.000) ile karşılaştırılıyordu. Sunucu "SEMA_ESKI 503" yerine `YASAL_PARAM_AYRISMA` ile hiç açılmıyordu.
   - Yapılan: bekleyen migration varsa eşitlik denetimi **erteleniyor** ve günlüğe `YASAL_PARAM_DENETIM_ERTELENDI` yazılıyor.
   - Hazır olmama `/saglik/hazir` 503 SEMA_ESKI ile, üretimde de T-1 kapısının çıkış kodu 3 ile söyleniyor. Kapılar işlem anında tabloyu (eski, daha sıkı değeri) okuyor; gevşeme yok.
   - Bu değişiklik görevin "Program.cs YALNIZ açılış karşılaştırması gerekirse" maddesi kapsamında. Kanıt mutasyon M1-9.

## 3. DoD sözleşmesi (Bertan koşulu)

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| `yasal_parametreler` satırı `masak370-01` | Yeni API ucu yok. İleri tarihli değer için mevcut `POST /api/v1/sistem/yasal-parametreler` (sistem anahtarlı, denetimli) var. | Geçmiş ve bugün başlayan değer append-only. Düzeltme yalnız migration ve denetim kaydıyla yapılır (ADR-0005, `YasalParametreServisi.Ekle`). Ekranda "Devletin rakamları — değiştirilemez" bölümü bu gerekçeyi taşıyor (DegismezIpucu deseni, mevcut). |
| Yeni varlık / kayıt türü | Yok | Şema değişmedi. Yeni form, düğme ya da uç yok. Finansal tabloya hard DELETE yok. |

Dokunulan sayfalar (razor): tur 2'de **yok**; **tur 3'te** `Pages/Banka.razor`, `Pages/Hurda.razor`, `Pages/Islemler.razor` (+ `Islemler.Odeme.cs`), `Bilesenler/Fis/FisProYanSutun.razor` (yalnız belge yorumu). TEK DOKUNUŞ gerekçesi: bu sayfalar önceki dalgalarda açılmıştı; ikinci dokunuş bu paketin KENDİ veri değişikliğinin ürettiği regresyonu (doğrulayıcı KRİTİK, /banka dekont çıkmazı) kapatmak içindir ve yalnız formu açan eşik karşılaştırması değişti (§ 14). Yeni form, düğme, uç, id yok.

## 4. Migration

- Ad: `20261007140228_MasakKimlikEsigi370`. Önce `dotnet build Kuyumcu.sln` YEŞİL oldu (0 hata), sonra build'li `dotnet ef migrations add` koşuldu. `--no-build` kullanılmadı.
- Üretilen dosyalar: `.cs` boş iskeletti (model değişmedi; snapshot farkı yok). İskelet SQL ile dolduruldu. `.Designer.cs` 258.952 bayt.
- `kuyumcu_net` öncesi yedeği: `/private/tmp/kuyumcu-serit/MASAK-370/yedek-yasal_parametreler-oncesi.sql` (16.343 bayt; yasal_parametreler + __EFMigrationsHistory).
- **İleri** (`dotnet ef database update`): `Applying migration '20261007140228_MasakKimlikEsigi370'. Done.`
  - SQL: `yasal1-tohum-07 185000 2023-01-14 2026-10-07 DOGRULANDI` · `masak370-01 370000 2026-10-07 NULL DOGRULANDI`.
- **Geri, ilk deneme BAŞARISIZ oldu ve gizlenmedi.**
  - Down adımı önce eski satırı açıyordu. `Duplicate entry 'MASAK_KIMLIK_ESIGI_TL-1' for key 'yasal_parametreler.ux_yasal_param_acik_uc'` hatası verdi.
  - İşlem geri alındı; geçmiş tablosu ve satırlar değişmedi (SQL ile doğrulandı).
  - Düzeltme: Down sırası tersine çevrildi (önce sil, sonra aç).
- **Geri, ikinci deneme** (`database update Yasal4GecerliPusulaKilidi`): `Reverting migration ... Done.`
  - SQL: `yasal1-tohum-07 185000 2023-01-14 NULL` · geçmişte 0 kayıt.
- **İleri, tekrar:** `Applying ... Done.` → iki satır yeniden kuruldu.
- Denetim izi: 3 satır (eklendi · GERİ ALINDI · eklendi).
- Kanıt dosyaları: `ham/MASAK-370-olcum/migration-ileri-1.txt`, `migration-geri.txt`, `migration-ileri-2.txt`.

## 5. Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo` (son, mutasyonlardan sonra temiz): **0 Uyarı · 0 Hata**.
  - Artımlı olmayan derlemede 1 uyarı görüldü: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`. Bu uyarı bu paketten önce de vardı (ilk derleme `build1.log`, test dosyalarına dokunmadan önce). 49. satıra dokunulmadı.
- `bash dotnet/denetim.sh`: 306 ✓, 1 ✗ — **yalnız P-30 DEMO_INSAN_SURESI** (kabulde hariç tutulmuş, bu paketle ilgisiz).
  - Bu paketle ilgili sayaçların hepsi ✓: IgnoreQueryFilters 0, Migrations 0-bayt 0, BuildTargetModel'siz Designer 0, B-3 KAPI_EKSIK 0, S2-3 0, YASAL-1 YASAL_PARAM_TARIHSIZ/YAZMA/OKUMA_DISI 0, YASAL-3 0.
  - Özet: `ham/MASAK-370-olcum/denetim-ozet.txt`.
  - Not: betik `zsh` ile koşulunca "bu betik BASH içindir" diyerek koşmadı; bash ile koşuldu.

## 6. Testler

| Koşum | Toplam | Geçti | Kaldı | Atlandı |
|---|---|---|---|---|
| Filtreli ilk koşum (Masak/Yasal/Pesin/Risk, değişiklikten hemen sonra) | 625 | 595 | 29 | 1 |
| Filtreli, sabitler güncellendikten sonra | 698 | 697 | 1 (K11 tohum sayısı) | 0 → düzeltildi |
| **Tam süit tur 1** (trx `trx-pMASAK-370`) | 10354 | 10346 | **6** | 2 |
| Filtreli, 6 kırmızı düzeltildikten sonra | 174 | 174 | 0 | 0 |
| **Tam süit tur 2, son kod** (trx `trx-pMASAK-370-t2`) | **10354** | **10352** | **0** | 2 |

- Tur 1'deki 6 kırmızının **hepsi bu paketin ürünüydü** ve tur 2'de kapandı:
  - `ZOS4_uyruk_esik_sinirinda…`
  - `ZOSR19_…` (3 test)
  - `UX4d M4_Kumulatif_mesaj…`
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` (Program.cs düzeltmesi).
- Tur 2 sonucu: **bu paketin ürettiği yeni kırmızı = 0**, tam süitte başkasına ait kırmızı da yok.
- Atlanan 2 test bu paketten önce de atlanıyordu: `TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli`, `SATIS3EkranTestleri.DOG_T24_SONDA_…`.
- Yeni test: **13** (`Masak370Testleri` 11 + `Masak370HttpTestleri` 2). Paket öncesi toplam 10354 − 13 = 10341 olmalı. Paket öncesinde tam süit koşulmadığı için bu sayı ölçülmüş değil, hesaplanmış bir değerdir.

**Güncellenen test sabitleri.** İki ilke uygulandı:

- Kademe merdivenini (YALNIZ_KIMLIK) sınayan saf ya da hasım testlerinde 185.000, **06.10.2026 dönem eşiği** olarak açıkça verildi (`new MasakEsikleri { KimlikEsigi = 185_000m }`).
- Gerçek saatle koşan DB/HTTP testlerinde eşik, bugün kimliği fiilen tetikleyen 200.000'e çekildi. Yorumlarda gerekçe yazıyor.

Dosyalar: `MasakKuralTestleri`, `MasakServisTestleri`, `MasakBankaTestleri` (Sinir testi `Sinir_199999_99_gecer_200000_00_reddedilir` adını aldı), `MasakBaglantiliIslemTestleri`, `MasakEntegrasyonTestleri`, `RiskServisiTestleri`, `YasalParametreHttpTestleri`, `YasalParametreTestleri` (K11 testi 22 satır / 10 DOGRULANDI oldu), `B4AltinBacagiTestleri`, `Yasal3MasakTestleri`, `KumulatifKapiSubedenEtkilenmezTestleri` (K10 adı değişti), `Dogrulayici/SUBEKAPSAMASinirTestleri`, `Dogrulayici/YASAL3SinirTestleri` (iki test adı 200000/199999_99 oldu), `Dogrulayici/ZOISLEMASinirTestleri` (ZOS4 adı değişti, R19 için dönem eşiği), `UX4dServisTestleri`.

**Yeni testler (`Masak370Testleri.cs`).** Testler gerçek migration tablosundan okuyor:

- Migration_185000_satiri_06_10_sonunda_kapanir_370000_07_10_DOGRULANDI_acilir
- Migration_denetim_izi_birakir_sistem_kiracisi
- Donem_06_10_200000_kimlik_ZORUNLU_ve_190000_eski_esikle_YALNIZ_KIMLIK
- Donem_07_10_190000_artik_kimlik_ISTEMEZ_200000_amac_beyani_kademesi_ister
- Donem_07_10_369999_99_esik_kapisi_istemez_ama_200000_beyan_kademesi_kimlik_ISTER
- Donem_07_10_370000_tam_esikte_kimlik_ZORUNLU_mesaj_370000_soyler
- Gun_siniri_yerel_saatle_06_10_23_59_eski_07_10_00_00_yeni_esik
- Kimlik_istenen_en_dusuk_tutar_eski_donemde_185000_yeni_donemde_200000
- Acilis_karsilastirmasi_varsayilan_370000_ile_GECER_eski_185000_ile_AYRISIR
- Baglantili_ayni_gun_185000_kimliksiz_gecer_ikinci_185000_toplam_370000_kimlik_ZORUNLU
- Tek_islem_369999_99_kimliksiz_REDDEDILIR_cunku_200000_beyan_kademesi_aynen
- Masak370HttpTestleri.Esikler_ucu_bugun_370000_06_10_tarihiyle_185000_doner
- Masak370HttpTestleri.Gereksinim_ucu_bugun_190000_YOK_200000_AMAC_BEYANI_370000_AMAC_BEYANI_kimlikli

## 7. Mutasyon kanıtları

Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/MASAK-370/` altına kopyalandı. Her mutasyon `perl -0777 -pi -e` ile tek eşleşmeli bir değişiklikti. Ardından derleme ve filtreli test koşuldu, yedek geri kopyalandı ve `cmp` ile karşılaştırıldı. Son mutasyondan sonra temiz derleme 0/0.

| Kod | Değişiklik | Kırmızıya dönen testler (örnek) | Geri alındı (cmp) |
|---|---|---|---|
| M1-1 | Migration INSERT 370000 → 185000 | Migration_…_DOGRULANDI_acilir, Donem_07_10_370000…, Baglantili_…370000…, Acilis_karsilastirmasi…, YasalParametreTestleri.K1_…, Masak370HttpTestleri.Esikler_ucu… (12 kırmızı) | AYNI |
| M1-2 | Geçiş günü 2026-10-07 → 2026-10-08 | Donem_07_10_190000…, Gun_siniri_yerel_saatle…, Migration_…, Masak370HttpTestleri (11 kırmızı) | AYNI |
| M1-3 | `MasakEsikleri.KimlikEsigi` varsayılanı 370_000 → 185_000 | MasakKuralTestleri.VarsayilanEsiklerMevzuatlaAyni, Masak370Testleri.Acilis_karsilastirmasi…, MasakServisTestleri.Esikler_KisiselVeriIcermez…, YasalParametreTestleri.K1_… | AYNI |
| M1-4 | `KimlikIstenenEnDusukTutar` → yalnız `KimlikEsigi` | Kimlik_istenen_en_dusuk_tutar…, Tek_islem_369999_99_kimliksiz_REDDEDILIR… | AYNI |
| M1-5 | Kimlik mesajı her zaman kimlik eşiğini söyler | Donem_07_10_369999_99_esik_kapisi_istemez…, Tek_islem_369999_99… | AYNI |
| M1-6 | Peşin pencere eşiği yalnız `KimlikEsigi` | PesinArtArdaTestleri.Pencere_MASAK_kimlik_esigine_ulasinca_soru | AYNI |
| M1-7 | 200.000 kademesi kimlik eşiği altında atlanır (`&& t >= KimlikEsigi`) | Donem_07_10_190000…_200000_amac…, Donem_07_10_369999_99…, Tek_islem_369999_99…, Masak370HttpTestleri.Gereksinim_ucu… | AYNI |
| M1-8 | appsettings KimlikEsigi 370000 → 185000 | Masak370HttpTestleri (2/2). Sunucu açılmadı: `YASAL_PARAM_AYRISMA … MASAK_KIMLIK_ESIGI_TL: yapılandırma 185000.0 ≠ tablo 370000.000000 (2026-10-07)` | AYNI |
| M1-9 | Program.cs şema-eski ertelemesi kaldırıldı (`if (false)`) | YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner | AYNI |

Her kritik kural en az bir testi kırmızıya döndürdü. Test açığı kalmadı.

## 8. Canlı doğrulama (API 5162, son derleme, `kuyumcu_net`)

- Açılış: `/saglik/hazir` → `{"hazir":true,"kod":"HAZIR"}`. Günlükte `YASAL_PARAM` satırı 0. Açılış karşılaştırması geçti (`api-5162-son.log`).
- Test kiracısı `PAKETMASAK370-TEST` (`01M4BCCQ6MVYH9VH63F3SJW2XQ`) sistem davetiyle API üzerinden kuruldu. Parola rastgele üretildi ve 600 izinli dosyaya yazıldı; hiçbir çıktıya basılmadı.
- Sistem anahtarı yalnız bu sürecin ortam değişkeniydi (rastgele, 600 izinli dosya).

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /masak/esikler | 200, kimlik 370.000 | 200 · `kimlikEsigi 370000, amacBeyaniEsigi 200000` |
| GET /masak/esikler?tarih=2026-10-06 | 185.000 (eski dönem) | 200 · `185000 / 200000` |
| GET /masak/esikler?tarih=2026-10-07 | 370.000 | 200 · `370000 / 200000` |
| GET /masak/gereksinim 184999.99 / 190000 / 199999.99 | YOK | YOK · kimlikGerekli false (üçü de) |
| GET /masak/gereksinim 200000 | AMAC_BEYANI, kimlik ister | AMAC_BEYANI · kimlikGerekli true |
| GET /masak/gereksinim 369999.99 | Eşik kapısı istemez ama 200.000 kademesi kimlik ister | AMAC_BEYANI · kimlikGerekli true |
| GET /masak/gereksinim 370000 | Kimlik zorunlu | AMAC_BEYANI · kimlikGerekli true |
| Dekont C1 185.000 kimliksiz | 200 (07.10'dan itibaren eşik altı; eski kodda 400) | **200** |
| Dekont C1 ikinci 185.000 kimliksiz (bağlantılı toplam 370.000) | 400 | **400** "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur · … (Bu cariyle bugün kümülatif 370.000,00 TL — eşik 200.000 TL; bu işlem 185.000,00 TL)" |
| Dekont C2 369.999,99 kimliksiz | 400 (200.000 beyan kademesi) | **400** "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · …" |
| Dekont C2 199.999,99 kimliksiz | 200 | **200** |
| Dekont C3 370.000 kimliksiz | 400 | **400** "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur · …" |
| Dekont C3 370.000 kimlikli + amaç | 200 + tespit kaydı | **200** |

**06.10.2026 tarihli işlem (canlı):**
- Kapı her zaman işlem günü = sunucunun bugünü ile değerlendiriyor. Dekontun `tarih` alanı kullanıcı girdisi; zaman ekseni `olusturuldu` (K41-1). Bu yüzden 06.10 senaryosu canlıda yalnız tarihli eşik ucuyla gösterilebiliyor (`?tarih=2026-10-06` → 185.000).
- Kapının 06.10'daki davranışı (200.000 → kimlik zorunlu, 190.000 → YALNIZ_KİMLİK) sahte saatli testlerle kanıtlandı: `Donem_06_10_…` ve `Gun_siniri_yerel_saatle_06_10_23_59…`.
- RiskServisi geçmiş dönemleri `Donemler()` ile her hareketin kendi gününün eşiğiyle değerlendirmeye devam ediyor (kod değişmedi).

**Bağımsız SQL** (`ham/MASAK-370-olcum/canli-sql.txt`):

```
cari                    tutar      yon    yerel_gun
MASAK370 Bağlantılı     185000.00  GELEN  2026-10-07     ← ikinci 185.000 YAZILMADI
MASAK370 Tek 369999     199999.99  GELEN  2026-10-07     ← 369.999,99 YAZILMADI
MASAK370 Tek 370000     370000.00  GELEN  2026-10-07     ← yalnız kimlikli olan
masak_kimlik_tespitleri: 370000.00 BANKA_HAREKET TC_KIMLIK (tek satır)
yasal_parametreler: yasal1-tohum-07 185000 2023-01-14 → 2026-10-07 DOGRULANDI · masak370-01 370000 2026-10-07 → NULL DOGRULANDI
```

Tam yanıtlar: `ham/MASAK-370-olcum/canli-api.json`.

Kapanış:
- API (PID 34371 ve üst süreci), vekil ve başsız Chrome durduruldu. 5162, 5163, 9905 ve 9906 portlarında LISTEN yok.
- 5199/5200'e dokunulmadı.
- Test kiracısı `zsh betikler/kiraci-temizle.sh 01M4BCCQ6MVYH9VH63F3SJW2XQ --uygula` ile silindi. 75 tablodan 279 satır ve 1 tenants satırı silindi. Silme sonrası SQL: kiracı 0, banka_hareketleri 0, masak_kimlik_tespitleri 0.
- **tenants sayısı:** başlangıçta 16 idi. Kiracı kurulduktan sonra okunan değer 16 çıktı, yani arada başka bir şerit bir kiracı silmiş. Silme sonrası 15. Bu paketin kiracısı +1 / −1 olarak döndü. QA-TEST-* kiracılarına dokunulmadı.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/tur.mjs` + `sikisma-olc.js`, vekil 5163)

Ölçülen rotalar: `/yasal-sinirlar` ve `/uyum`. Görevde "/masak" yazıyordu, ancak bu rota uygulamada **yok** ("Sayfa bulunamadı" görüntüsüyle doğrulandı). MASAK ekranının gerçek rotası `@page "/uyum"`, yani "Uyum (MASAK)". Ek olarak `/yasal-sinirlar` kimlik kartının "Dayanak ve geçmiş" bölümü açık hâlde (`aria-expanded=true` doğrulanarak) ayrıca ölçüldü.

| rota | genişlik | mod | tema | geçerli | taşan K3 | binen K9 | kırık K3 | yatay | gizli-görünen K1 | TRY K5 | K13 kırp | hata bandı |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /yasal-sinirlar | 1280 / 375 | BASIT | açık | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 1280 / 375 | BASIT | koyu | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 1280 / 375 | PRO | açık | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 1280 / 375 | PRO | koyu | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar (kimlik ayrıntısı AÇIK) | 1280 / 375 | BASIT + PRO | açık + koyu | ✓ (8/8) | 0 | 0 | 0 | 0 | 0 | 0 | – | – |
| /uyum | 1280 / 375 | BASIT | açık + koyu | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /uyum | 1280 / 375 | PRO | açık + koyu | ✓ | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- Toplam 16 sayfa ve 104 katman kaydı ölçüldü, ölçüm hatası 0.
- Tam tablo: `ham/MASAK-370-olcum/olcum-tablo.txt`; ham veriler: `tur-*.jsonl` ve `yasal-kimlik-acik-olcum.json`.
- Açık kartın metni 8 kombinasyonun hepsinde "Dayanak Tedbirler Yönetmeliği md.5/1-b (11845 sayılı CK, RG 07.10.2026/33393)" ve "370.000 ₺ ve üzeri" içeriyor.
- **K4 "tren"**: /yasal-sinirlar 375 px'te sayfa boyu ekranın 2,1 katı ve sayfada 9 bölüm var. Bu, paket öncesinden kalan bir sayfa yapısı; bu pakette razor ya da CSS değişmedi. Bilgi olarak kaydedildi, bu paketin ürünü değil.
- Dar sütun (K2): 1280'de kartlar 3 sütunlu ızgara. Ekran tasarımı değişmediği için gerekçe gerekmiyor.

**Read ile bakılan görüntüler:**
- `yasal_sinirlar__BASIT_aydinlik_1280.png`: kimlik kartında "Doğrulandı · 370.000 ₺ ve üzeri · Ne zamandan beri: 7 Ekim 2026"; amaç kartında "Mali müşavir teyidi bekliyor · 200.000 ₺ ve üzeri".
- `yasal_kimlik_acik__BASIT_aydinlik_1280.png` ve `…__PRO_karanlik_375.png`: dayanak, "Resmî kaynağı aç" bağlantısı ve geçmiş listesi görünüyor ("7 Ekim 2026 – sürüyor: 370.000 ₺" / "14 Ocak 2023 – 6 Ekim 2026: 185.000 ₺").
- `uyum__PRO_aydinlik_1280.png`: Kimlik Tespitleri listesinde 370.000,00 ₺ satırı maskeli belge no ile görünüyor.
- `uyum__BASIT_karanlik_375.png`: kartlar tek sütun; tablo kendi kabında kayıyor, sayfada yatay kaydırma 0.

Hepsi `ham/MASAK-370-olcum/foto/` altında (24 görüntü).

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` bu dosyaları ayırt etmiyor: depoda yalnız "first commit" var ve dosyaların çoğu `??` ya da `M` görünüyor. Bu yüzden liste elle ve `diff` ile tutuldu. Öncesi kopyaları `/private/tmp/kuyumcu-serit/MASAK-370/onceki/` altında.

Üretim kodu:
- `dotnet/Kuyumcu.Api/Data/Migrations/20261007140228_MasakKimlikEsigi370.cs` (yeni)
- `dotnet/Kuyumcu.Api/Data/Migrations/20261007140228_MasakKimlikEsigi370.Designer.cs` (yeni, ef üretti)
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs` (ef yeniden yazdı; model farkı yok, Up boş üretildi)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKuralMotoru.cs` (varsayılan, `KimlikIstenenEnDusukTutar`, mesaj)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (kümülatif notu, Peşin eşiği, yorum)
- `dotnet/Kuyumcu.Api/appsettings.json` (Masak:KimlikEsigi 370000.0)
- `dotnet/Kuyumcu.Api/Program.cs` (açılış karşılaştırmasının şema-eski ertelemesi)
- Yalnız yorum değişen dosyalar: `Modules/Satis/SatisModulu.cs`, `Modules/Masak/MasakModulu.cs`, `Modules/Banka/BankaModulu.cs`, `Modules/Banka/EkstreModulu.cs`, `Data/MasakVarliklari.cs`, `Data/GiderPusulasi.cs`, `Data/YasalParametre.cs`

Testler:
- Yeni: `Kuyumcu.Tests/Masak370Testleri.cs`
- Güncellenen: `MasakKuralTestleri.cs`, `MasakServisTestleri.cs`, `MasakBankaTestleri.cs`, `MasakBaglantiliIslemTestleri.cs`, `MasakEntegrasyonTestleri.cs`, `RiskServisiTestleri.cs`, `YasalParametreHttpTestleri.cs`, `YasalParametreTestleri.cs`, `B4AltinBacagiTestleri.cs`, `Yasal3MasakTestleri.cs`, `KumulatifKapiSubedenEtkilenmezTestleri.cs`, `UX4dServisTestleri.cs`, `Dogrulayici/SUBEKAPSAMASinirTestleri.cs`, `Dogrulayici/YASAL3SinirTestleri.cs`, `Dogrulayici/ZOISLEMASinirTestleri.cs`

Belge ve kanıt:
- Bu rapor
- `dokumanlar/09-uygulama-kayitlari/ham/MASAK-370-olcum/` (yeni)

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Migration: 185.000 06.10 sonunda kapanır, 370.000 07.10'dan DOGRULANDI ve kaynakla; geçmiş yeniden yazılmaz | ✓ | § 4 SQL; test Migration_… |
| (2) appsettings ve `MasakKuralMotoru` varsayılanı 370.000; açılış karşılaştırması geçer | ✓ | 5162 HAZIR, günlükte YASAL_PARAM yok; M1-3, M1-8 |
| (3) Koddaki "185.000" yorumları; ekranlar değeri sunucudan alıyor | ✓ | § 2 madde 3; Web'de grep 0 |
| (4) Testler: sabitler ve dönem geçişi sınır testleri (06.10 200.000 zorunlu · 07.10 369.999,99 eşik kapısı istemez · 370.000 zorunlu · 2×185.000 bağlantılı) | ✓ | § 6, 13 yeni test |
| (5) 200.000 kademesi değişmedi; testle ve raporla belgelendi | ✓ | Tek_islem_369999_99…, Donem_07_10_369999_99…; canlı C2 400 |
| (6) /yasal-sinirlar bugün-başlayan reddine dokunulmadı; ekranda DOGRULANDI ve kaynak görünüyor (başsız Chrome) | ✓ | § 9 görüntüler |
| Migration ileri/geri (önce build, --no-build yok) | ✓ | § 4 (ilk Down hatası raporlandı ve düzeltildi) |
| Canlı API ve bağımsız SQL | ✓ | § 8 (port 5162) |
| /yasal-sinirlar ve MASAK ekranı · 1280 + 375 · BASIT + PRO · açık + koyu · Read | ✓ | § 9 ("/masak" rotası yok; gerçek rota /uyum) |
| Mutasyon (≥ 1 kırmızı, cmp AYNI) | ✓ | § 7, 9 mutasyon |
| Build 0/0 · denetim TEMİZ (P-30 hariç) · tam süitte yeni kırmızı 0 | ✓ | § 5, § 6 tur 2: 0 kırmızı |
| Test kiracısı silindi | ✓ | § 8 |
| COMMIT yok · dosya silme yok · sır basılmadı | ✓ | — |

## 12. Açık kalanlar (sahibiyle)

1. **ACİL — Batuhan (DevOps) / orkestratör: 5199/5200 yeniden başlatılırsa açılmaz.**
   - Migration, kurucunun inceleme sunucularının da kullandığı `kuyumcu_net`'e uygulandı (görevin migration prosedürü bunu gerektiriyor).
   - Çalışan 5199/5200 süreçleri etkilenmedi. Önbellekleri gece yarısı gün değişince tabloyu yeniden okur ve 370.000'e geçer; bu doğru yön.
   - Ancak `~/.kuyumcu-yayin/appsettings.json` hâlâ `KimlikEsigi: 185000.0`. Eski yayın yeniden başlatılırsa `YASAL_PARAM_AYRISMA` ile **açılmaz**: o sürüm bu migration'ı bilmiyor, bekleyen migration görmüyor ve ayrışmayı doğrudan sayıyor. Nöbetçi döngüye girebilir.
   - Çözüm: kapıdan sonra `yayinla.sh` ile yeniden yayın. Kaynak raporun kurucu kararı 3'e göre yayın ayrı onayla yapılır ve önce yedek alınır.
   - Ayrıca `dotnet/yayin/linux-x64/appsettings.json` da 185000.0. Bu yayın çıktısı bu paketin dosya alanı dışında olduğu için dokunulmadı.
2. **[KAPANDI — tur 3] Ertuğ (frontend): ekranın form açma eşiği.**
   - `Islemler.razor:802`, `Islemler.Odeme.cs:190`, `Banka.razor:1200/1206` ve `Hurda.razor:728/736` MASAK formunu `masakEsik.KimlikEsigi`ye göre önceden açıyor.
   - 07.10.2026'dan sonra kimlik 200.000'de (amaç kademesi) isteniyor, `KimlikEsigi` ise 370.000.
   - **TUR 3'TE KAPANDI (§ 14).** Tur 2'deki bu madde YANLIŞTI ve düzeltildi: "ekran sunucu reddinden sonra formu açıyor" iddiası /banka için doğru değildi — Banka.razor'da `masakZorla`/sunucu-reddi kurtarma yolu yok; müşterisiz 200.000–369.999,99 dekontta form hiç açılmıyor, sunucu her kayıtta 400 dönüyordu (doğrulayıcı KRİTİK, gerçek tarayıcıda gösterildi). Bu bir çıkmazdı ve bu paketin veri değişikliğinin ürünüydü.
   - Tur 3 düzeltmesi: `/masak/esikler` yanıtına `kimlikIstenenEnDusukTutar` eklendi; Banka/Hurda/Islemler/FisProYanSutun bu tutarı kullanıyor. Paket ve düzeltme aynı sürümde.
3. **Kurucu / mali müşavir: RiskServisi "eşiğe yakın" bandı.**
   - Band md.5/1-b eşiğinin %80–100'ü, yani 07.10'dan itibaren 296.000–370.000.
   - Kimliği fiilen tetikleyen 200.000'in hemen altındaki bölme (ör. 160.000–199.999) artık "eşiğe yakın" sayılmıyor.
   - Spesifikasyon "RiskServisi değişiklik gerekmiyor" dediği için koda dokunulmadı. Bu bir politika ya da sinyal kararı.
4. **Aşkın (QA) — bilgi.** `/yasal-sinirlar` 375 px'te K4 "tren" ölçümü (2,1 ekran boyu, 9 bölüm). Paket öncesinden kalan yapı.
5. **Bilgi.** 5162 açılışında `is_calismalari` UNIQUE "Duplicate entry … MASAK_SIB_SURESI-2026-10-07" günlük satırları görüldü. Bunlar günlük iş kilidinin tasarlanmış idempotentliği: başka bir sunucu bugünkü işi zaten koşmuş. Bu paketle ilgisiz.
6. **Bilgi.** Test kiracısı için üretilen sistem daveti (`davetler`, sistem düzeyi, tenant kolonu yok) kayıtla tüketildi ve tabloda kaldı. Kiracı temizlik betiği bu tabloyu kapsamıyor.

## 13. Kurucuya ve mali müşavire notlar

- **M-2 (açık, artık acil).** 200.000 TL "işlem amacı beyanı" kademesi kuyumcuya uygulanıyor mu? Uygulanıyorsa 370.000 altında da kimlik gerektiriyor mu? Yalnız nakitte mi geçerli?
  - Cevap gelene kadar program 200.000 TL'den itibaren kimlik istemeye devam ediyor (kurucu md.15 (a)). Bu yüzden düzenlemenin pratik etkisi bugün yalnız **185.000–199.999,99 TL** bandında görülüyor: bu bantta artık kimlik istenmiyor.
- **S-2 kapanabilir.** Eşik 07.10.2026'dan itibaren 370.000 TL (RG 33393). Tabloda DOGRULANDI olarak işlendi.
- **Önce açılıp sonra kapanan işlemler** (kaparo, vadeli satış): program işlem gününü (kapının çalıştığı yerel günü) esas alıyor. Mali müşavir teyidi bekleniyor; kod değişmedi.

## 14. Tur 3 — doğrulayıcı bulgularının kapatılması (8 Ekim 2026 gece, şerit t2)

### 14.1 Bulgu → karar

| Bulgu (doğrulayıcı) | Önem | Tur 3 kararı |
|---|---|---|
| /banka dekont çıkmazı: müşterisiz 200.000–369.999,99 dekontta MASAK formu açılmıyor, sunucu her kayıtta 400 | KRİTİK | **KAPANDI.** Sunucu `/masak/esikler` yanıtına `kimlikIstenenEnDusukTutar` (= `MasakEsikleri.KimlikIstenenEnDusukTutar` = min(md.5/1-b eşiği, amaç beyanı kademesi)) eklendi. İstemci sözleşmesi `Esikler.KimlikIstenenTutar()`: sunucu değeri varsa o, yoksa (eski sunucu) iki eşiğin küçüğü — fail-closed, sayı istemciye gömülmez. Banka.razor:1200/1206, Hurda.razor:728/736/743, Islemler.razor:224 (PRO yan sütun parametresi) ve 802, Islemler.Odeme.cs:115/190/236 bu tutarı kullanıyor. `MASAK370SinirBankaEkranTestleri.B1` YEŞİL. Gerçek tarayıcı: 250.000 dekontta form ÖNCEDEN açılıyor (8/8), kimlikle kayıt 200 + tespit satırı. |
| PRO yan sütun ve Peşin kapısı istemcide 370.000'e bakıyor | ORTA | **KAPANDI.** `PesinEsikUstu` ve `FisProYanSutun.KimlikEsigi` artık kimliğin fiilen istendiği tutarı alıyor. Gerçek tarayıcı PRO yan sütun: "200.000 ₺ kimlik eşiği" (4/4 PRO kombinasyonu). |
| 5199/5200 yayını yeniden başlarsa açılmaz (`~/.kuyumcu-yayin/appsettings.json` 185000.0) | ORTA | **Açık — sahibi Batuhan/orkestratör, kurucu onayıyla** (§ 12 md.1). Paketin dosya alanı dışında; 5199/5200'e dokunulmadı. Bu tur `kimlikIstenenEnDusukTutar` alanını da ekledi: eski yayın bu alanı göndermez, yeni istemci yine doğru davranır (geri dönüş yolu testli). |
| Program.cs: bekleyen migration varken açılış eşitlik denetimi tümüyle erteleniyor | DÜŞÜK | **Açık, bilinçli.** İsteğe bağlı iyileştirme (erteleme yalnız bekleyen migration'ların anahtarlarıyla sınırlanabilir). Kapılar işlem anında tabloyu okuduğu için gevşeme yok; üretimde T-1 çıkış kodu 3. Kapsamı genişletmemek için dokunulmadı. |
| Dönem geçişinde kapı işlem gününü değil sunucunun bugününü kullanıyor | DÜŞÜK | **Açık — mali müşavir sorusu** (§ 13 "önce açılıp sonra kapanan işlemler"). Spesifikasyon § 5 bunu açık bırakıyor; tahminle kural yazılmadı. |

Mevzuat: yeni eşik/oran yazılmadı. `kimlikIstenenEnDusukTutar` yeni bir yasal değer DEĞİL; tablodaki iki mevcut değerin (370.000 DOĞRULANDI, 200.000 TEYİTSİZ) küçüğü, sunucunun zaten uyguladığı kural. 200.000 kademesi aynen (kurucu md.15 (a)).

### 14.2 Değişen dosyalar (tur 3)

- `dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs` — `EsikYaniti`'ne `kimlikIstenenEnDusukTutar` (3 satır).
- `dotnet/Kuyumcu.Contracts/Bilesenler/MasakFormuSozlesmeleri.cs` — `Esikler` kaydına `KimlikIstenenEnDusukTutar { get; init; }` + `KimlikIstenenTutar()` (konumsal kurucu değişmedi).
- `dotnet/Kuyumcu.Web/Pages/Banka.razor` (2 satır), `Pages/Hurda.razor` (3), `Pages/Islemler.razor` (2), `Pages/Islemler.Odeme.cs` (3), `Bilesenler/Fis/FisProYanSutun.razor` (yalnız belge yorumu).
- Testler: yeni `Kuyumcu.Tests/Masak370EkranTestleri.cs` (9 koşum), yeni `Kuyumcu.Tests/Masak370PesinEkranTestleri.cs` (`SATIS3EkranTestleri` kısmi sınıfı, 4 koşum), güncellenen `Kuyumcu.Tests/Masak370Testleri.cs` (`Esikler_ucu…` testine 4 doğrulama).
- Öncesi kopyalar: `/private/tmp/kuyumcu-serit/MASAK-370/onceki-t3/`. `diff` ile yalnız bu satırlar farklı (başka şeridin aynı dosyaya eşzamanlı değişikliği yok).
- Element id'leri değişmedi; yeni id yok; yeni CSS yok; Sozlukler değişmedi (yeni metin yok).

### 14.3 Testler

| Koşum | Toplam | Geçti | Kaldı | Atlandı |
|---|---|---|---|---|
| Filtre `Masak370 \| MASAK370` (düzeltmeden sonra; doğrulayıcının B1/B2 dahil) | 34 | 34 | 0 | 0 |
| Filtre `Masak370 \| MASAK370 \| T24_` (Peşin testi eklendikten sonra) | 61 | 60 | 0 | 1 (önceden atlanan `SATIS3EkranTestleri.DOG_T24_SONDA_…`) |
| **Tam süit tur 3, son kod** (`/private/tmp/kuyumcu-serit/trx-pMASAK-370-t3/_Mac_2026-10-07_23_49_27.trx`) | **10399** | **10397** | **0** | 2 |

- Tam süitte kırmızı **0** → bu paketin yeni kırmızısı 0, başkasına ait kırmızı da yok. Atlanan 2 test önceden de atlanıyordu.
- Yeni test (tur 3): **13 koşum** (`Masak370EkranTestleri` 9 + `Masak370PesinEkranTestleri` 4). Paketin toplam yeni testi: 13 (tur 2) + 13 (tur 3) = **26**.
- Yeni testler: `Sozlesme_sunucu_alani_varsa_O_kullanilir_yoksa_iki_esigin_KUCUGU_fail_closed` · `Banka_musterisiz_250000_dekontta_MASAK_formu_ONCEDEN_acilir_kaydet_gerekceli_kilitli` (BASIT/PRO) · `Banka_musterisiz_199999_99_dekontta_MASAK_sorulmaz_form_yok` · `Banka_alani_gondermeyen_eski_sunucuda_da_250000_form_acilir` · `Banka_eski_donem_185000_esigi_birebir_190000_form_acilir` · `Hurda_musterisiz_204000_MASAK_alanlari_ONCEDEN_acilir_197200_acilmaz` · `Islemler_PRO_yan_sutun_kimligin_ISTENDIGI_tutari_gosterir_370000_degil` (alanlı/alansız) · `SATIS3EkranTestleri.MASAK370_Pesin_kimlik_esiginin_ALTINDA_ama_amac_kademesi_USTUNDE_engel_TEK_cumle` (BASIT/PRO) · `SATIS3EkranTestleri.MASAK370_gercek_musteride_gereksinim_ucu_DUSERSE_amac_kademesi_ustunde_Kaydet_KILITLI` (BASIT/PRO).
- Sahte API'ler gerçek sunucu yanıtını taklit ediyor (canlı 5162'de okunan `kimlikEsigi 370000 / kimlikIstenenEnDusukTutar 200000 / amacBeyaniEsigi 200000`; gereksinim < 200.000 YOK, ≥ 200.000 AMAC_BEYANI).

### 14.4 Mutasyon kanıtları (tur 3)

Yöntem: `/private/tmp/kuyumcu-serit/MASAK-370/mut-t3.sh` — kaynak `/private/tmp/kuyumcu-serit/mut-yedek/MASAK-370-t3/` altına kopya → `perl -0777 -pi -e` tek eşleşme → derleme → filtreli test (`Masak370|MASAK370`) → yedek geri → `cmp`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı · 0 Hata**.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M3-1 | `Esikler.KimlikIstenenTutar()` → yalnız `KimlikEsigi` | 10 kırmızı: Dogrulayici `MASAK370SinirBankaEkranTestleri.B1…`, `Banka_musterisiz_250000…` (BASIT, PRO), `Banka_alani_gondermeyen…`, `Hurda_musterisiz_204000…`, `Islemler_PRO_yan_sutun…` (2), `Sozlesme_…`, `SATIS3EkranTestleri.MASAK370_Pesin_…` (2) | cmp AYNI |
| M3-2 | Sunucu `kimlikIstenenEnDusukTutar = e.KimlikEsigi` | `Masak370HttpTestleri.Esikler_ucu_bugun_370000_06_10_tarihiyle_185000_doner` | cmp AYNI |
| M3-3 | Banka.razor `MasakSorulmali` eski karşılaştırma (`KimlikEsigi`) | `MASAK370SinirBankaEkranTestleri.B1…`, `Banka_musterisiz_250000…` (BASIT, PRO), `Banka_alani_gondermeyen…` | cmp AYNI |
| M3-4 | Hurda.razor `MasakSorulmali` eski karşılaştırma | `Hurda_musterisiz_204000_MASAK_alanlari_ONCEDEN_acilir_197200_acilmaz` | cmp AYNI |
| M3-5 | Islemler.razor PRO yan sütun parametresi `masakEsik?.KimlikEsigi` | `Islemler_PRO_yan_sutun_kimligin_ISTENDIGI_tutari_gosterir_370000_degil` (2) | cmp AYNI |
| M3-6 | Islemler.Odeme.cs `PesinEsikUstu` eski karşılaştırma | `SATIS3EkranTestleri.MASAK370_Pesin_kimlik_esiginin_ALTINDA_…` (BASIT, PRO) | cmp AYNI |
| M3-7 | Islemler.razor `MasakGerekli` eski karşılaştırma | `SATIS3EkranTestleri.MASAK370_gercek_musteride_gereksinim_ucu_DUSERSE_…` (BASIT, PRO) — ilk denemede test yoktu; uç düşünce fail-closed davranışını sınayan test yazıldı, sonra mutasyon kırmızı | cmp AYNI |

Mutasyonla ayrıca sınanmayan değişiklikler ve gerekçesi (eşdeğer durumlar): Banka.razor:1206 ve Hurda.razor:736 (`MasakGerekli`) — `MasakSorulmali` doğruysa gereksinim sunucuya sorulur ve cevap (`KimlikGerekli=true`) formu zaten açar; istemci karşılaştırması yalnız cevaptan önceki ilk çizimde etkili. Islemler.Odeme.cs:190 (`MasakSorulur`) — gerçek müşteri seçiliyse soru zaten sorulur, Peşin'de `PesinEsikUstu` önce kilitler (M3-6 bunu sınar). Islemler.Odeme.cs:236 ve Hurda.razor:743 — sunucu reddinden sonraki yeniden sorgunun alt sınırı; davranışı mevcut ret testleri (ZO29d, SATIS3 kümülatif red) korur. Hepsi aynı tek kaynağı (`KimlikIstenenTutar()`) çağırıyor ve o kaynak M3-1 ile 10 testte kırmızı.

### 14.5 Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo` (son): **0 Uyarı · 0 Hata** (`/private/tmp/kuyumcu-serit/MASAK-370/build-t3-son.log`). Artımlı olmayan derlemede görülen 4 uyarı başka dosyalarda: `SUBEKAPSAMASinirTestleri.cs(49)` CS8601 (paket öncesi), `KesifP2Testleri.cs(455)` xUnit2029, `SonKalanlarTestleri.cs(584,813)` xUnit2013 — bu paketin dosyaları değil.
- `bash dotnet/denetim.sh`: **306 ✓, 1 ✗ — yalnız P-30 DEMO_INSAN_SURESI** (kabulde hariç; bu paketle ilgisiz) (`denetim-t3.log`). Pages/ altında yeni ModalPencere düzenleme kullanımı yok.

### 14.6 Canlı doğrulama (API 5162, `kuyumcu_net`, son derleme)

- Açılış: `/saglik/hazir` → `{"hazir":true,"kod":"HAZIR"}`; günlükte `YASAL_PARAM` satırı 0 (açılış karşılaştırması geçti).
- Test kiracısı `PAKETMASAK370-TEST Tur3` (`01M4C29CJ1GDDDQ5NSJS1ETB8Y`) sistem davetiyle (201) ve kayıtla (201) kuruldu. Sistem anahtarı ve parola rastgele, 600 izinli dosyada; çıktıya basılmadı; iş sonunda dosyalar boşaltıldı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /masak/esikler | 370.000 / istenen 200.000 | 200 · `kimlikEsigi 370000, kimlikIstenenEnDusukTutar 200000, amacBeyaniEsigi 200000` |
| GET /masak/esikler?tarih=2026-10-06 | 185.000 / istenen 185.000 (davranış birebir) | 200 · `185000 / 185000 / 200000` |
| GET /masak/esikler?tarih=2026-10-07 | 370.000 / istenen 200.000 | 200 · `370000 / 200000 / 200000` |
| GET /masak/gereksinim 199.999,99 | YOK | YOK · kimlikGerekli false |
| GET /masak/gereksinim 200.000 · 250.000 · 369.999,99 · 370.000 | AMAC_BEYANI, kimlik ister | dördü de AMAC_BEYANI · kimlikGerekli true |
| Dekont müşterisiz 250.000 kimliksiz | 400 (beyan kademesi) | **400** "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · …" |
| Dekont müşterisiz 199.999,99 kimliksiz | 200 | **200** |
| Dekont müşterisiz 250.000 kimlikli + amaç | 200 + tespit | **200** |
| Dekont C1 185.000 kimliksiz | 200 | **200** |
| Dekont C1 ikinci 185.000 (bağlantılı toplam 370.000) | 400 | **400** "… (Bu cariyle bugün kümülatif 370.000,00 TL — eşik 200.000 TL; bu işlem 185.000,00 TL)" |
| **Gerçek tarayıcı:** /banka dekont 260.000 müşterisiz, form ÖNCEDEN açıldı, kimlik + amaç dolduruldu, Kaydet | kayıt + tespit | Kaydet önce kilitli, doldurunca açıldı; pencere kapandı; bakiye 634.999,99 → 894.999,99 ₺ (görüntü `t3/foto/banka-dekont-260000__BASIT_aydinlik_1280-kayit-sonra.png`) |

Bağımsız SQL (`ham/MASAK-370-olcum/canli-sql-t3.txt`):

```
banka_hareketleri        199999.99  GELEN  -                           2026-10-07
banka_hareketleri        250000.00  GELEN  -                           2026-10-07   ← kimlikli API kaydı
banka_hareketleri        185000.00  GELEN  01M4C29ZEZH0G33CQJX7CXAZPA  2026-10-07   ← ikinci 185.000 YAZILMADI
banka_hareketleri        260000.00  GELEN  -                           2026-10-07   ← tarayıcıdan, ekranın açtığı formla
masak_kimlik_tespitleri  250000.00  BANKA_HAREKET  TC_KIMLIK
masak_kimlik_tespitleri  260000.00  BANKA_HAREKET  TC_KIMLIK
```

Tam yanıtlar: `ham/MASAK-370-olcum/canli-api-t3.json`.

Kapanış: API (PID ve alt süreci), vekil ve başsız Chrome durduruldu; 5162, 5163, 9905, 9906 LISTEN yok. 5199/5200'e dokunulmadı. Kiracı `zsh betikler/kiraci-temizle.sh 01M4C29CJ1GDDDQ5NSJS1ETB8Y --uygula` ile silindi; sonra kiracı 0, banka_hareketleri 0, masak_kimlik_tespitleri 0. **tenants:** başlangıç 17 → son 16. Fark bu paketin değil: eşzamanlı OLCEK-YUK şeridinin kiracısı `PAKETOLCEK-TEST Altınbaşak…` (`01M4C1BHCM9RE0K2P4KSP7A34Q`) bu süre içinde silindi; bu paketin kiracısı +1/−1. QA-TEST-* kiracılarına dokunulmadı.

### 14.7 Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9905/9906, vekil 5163)

Tam tablo: `ham/MASAK-370-olcum/t3/olcum-tablo-t3.txt`; ham: `t3/banka-olcum.jsonl`, `t3/ekran-olcum.jsonl`, `t3/tur/olcum-*.jsonl`; görüntüler `t3/foto/` (38) ve `t3/tur/foto/`.

**A) /banka dekont penceresi, müşterisiz 250.000 (doğrulayıcının çıkmaz senaryosu):**

| genişlik | mod | tema | MASAK formu | Kaydet | sebep (UX-S30) | taşan | kırık | yatay | gizli-görünen | TRY | kırpılan |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280 / 375 | PRO | koyu | AÇIK | kilitli | "MASAK kimlik/beyan alanları eksik — kaydetmek için yukarıdaki zorunlu alanları doldurun." | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 / 375 | PRO | açık | AÇIK | kilitli | aynı | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 / 375 | BASIT | koyu | AÇIK | kilitli | "Kaydetmek için kimlik bilgilerini doldurun." | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 / 375 | BASIT | açık | AÇIK | kilitli | aynı | 0 | 0 | 0 | 0 | 0 | 0 |

- Doğrulayıcının paket öncesi ölçümü (aynı senaryo): `{masakFormu:false, kaydetDisabled:false}` → kayıt sonrası 400 bandı, form yine yok. Şimdi: form kayıttan ÖNCE açık, Kaydet gerekçeli kilitli, doldurulunca kayıt 200.
- Sayfa düzeyi K9 sayacı pencere açıkken 3 (1280) / 6 (375) veriyor; örnekler arka sayfa öğeleriyle (ör. `p.stat-deger "634.999,99 ₺" ⟂ span.alan-etiket-metin "Tutar"`) — pencerenin örttüğü sayfa, görsel binme değil. Pencere İÇİ ayrıca ölçüldü: 2 geometrik kesişme, ikisi de kaydırılan gövdedeki `#dekont-masak-cari-uyari` ile yapışkan alt çubuk düğmeleri arasında; kaydırma kabı kırpıyor, görüntüde binme yok (Read ile bakıldı).

**B) /hurda müşterisiz 35 has gram (230.195–230.230 ₺, kimlik eşiği 370.000'in altı):** 8/8 kombinasyonda MASAK alanları (`#hrd-masak`, `#masak-belge-no`) ÖNCEDEN açık; taşan/kırık/K9/yatay/gizli-görünen/TRY/kırpılan 0. (Kaydet kilidinin sebebi bu test kiracısında "Bu madende hurda bakiyesi yok" — beklenen; hurda stoku yok.)

**C) /islemler PRO yan sütun:** 4/4 PRO kombinasyonda `#islem-pro-kimlik-etiket` = "200.000 ₺ kimlik eşiği" (eskiden "370.000 ₺"); BASIT'te yan sütun yok (tasarım). Ölçüm 0/0/0.

**D) Sayfa turu** (`tur.mjs`): /banka, /hurda, /islemler, /yasal-sinirlar, /uyum × 1280/375 × BASIT/PRO × açık/koyu = **40 sayfa kaydı, hepsi geçerli**, taşan 0, kırık 0, K9 0, yatay 0, gizli-görünen 0, TRY 0, kırpılan 0, hata bandı 0.

Read ile bakılan görüntüler: `banka-dekont-250000__PRO_karanlik_1280.png` (pencere, sebep cümlesi), `…__PRO_karanlik_375-masak-formu.png` (375'te "MASAK Kimlik Tespiti · Kimlik + işlem amacı beyanı gerekli" formu), `…__BASIT_aydinlik_1280-masak-formu.png` ("Kimlik bilgisi gerekli (tutar yüksek)"), `banka-dekont-260000__BASIT_aydinlik_1280-kayit-sonra.png` (bakiye 894.999,99 ₺, 4 hareket), `hurda-35has__BASIT_aydinlik_375.png` (kimlik formu), `islemler-yasal__PRO_aydinlik_1280.png` ("200.000 ₺ kimlik eşiği · Altında"), `tur/foto/islemler__PRO_karanlik_1280.png`.

Gözlem (bu paketin ürünü değil, bilgi): /banka dekont kaydı başarıyla bitince pencere kapanıyor ama görünür bir başarı bandı yakalanmadı (UX-S27 — bakiye ve hareket sayısı güncelleniyor). Aşkın (QA) turunda incelenmesi önerilir.

### 14.8 Koşul karşılama (tur 3 ek)

| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK /banka çıkmazı kapandı; `MASAK370SinirBankaEkranTestleri.B1` yeşil | ✓ | § 14.3; M3-1/M3-3 B1'i kırmızıya döndürüyor |
| /banka 250.000 gerçek tarayıcı 1280/375 × BASIT/PRO × açık/koyu | ✓ | § 14.7 A (8/8) |
| Hurda, Islemler, FisProYanSutun aynı tutarı kullanıyor | ✓ | § 14.2; M3-4…M3-7; § 14.7 B/C |
| Paket ve düzeltme aynı sürümde | ✓ | aynı çalışma ağacı; yayın orkestratörde |
| Build 0/0 · denetim TEMİZ (P-30 hariç) · tam süit 0 kırmızı | ✓ | § 14.5, § 14.3 |
| Canlı API + SQL, test kiracısı silindi | ✓ | § 14.6 |
| Mevzuat: yeni değer uydurulmadı; 200.000 kademesi aynen | ✓ | § 14.1 |
