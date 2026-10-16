# PAKET YASAL-2 — Taksitte nakit sınırı (TB-15) · Geç fark edilen aşım ve bildirim (TB-16): uygulama raporu

> Tarih: 30.09.2026 · Şerit t2 (SUBE-YASAL23) · Rol: Taşkın (backend) + Ertuğ (frontend) · Tur 1 · COMMIT YOK.
> Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-YASAL-2-TAKSIT-BILDIRIM-2026-09-30.md`.

## 1. Sonuç

Durum: **BİTTİ** — tur 3 sonrası (orkestratör 22:20 bulguları § 1B'de kapatıldı; paket kapısının yedi maddesi son kodla § 1B'de yeniden kanıtlı; tur 1–2 kanıtları aşağıda korunuyor).

- Build: `dotnet build Kuyumcu.sln` **0 hata**. Tek uyarı `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65)` CS8601. Bu dosya bu paketin değil, SUBE-KAPSAM doğrulayıcısının dosyası.
- Migration: `20260930142603_TevsikIhlalleri` kuyumcu_net veritabanına uygulandı.
- Yeni test: **54**, hepsi yeşil. Ayrıca 3 mevcut test tersine çevrildi ya da uyarlandı (§ 7).
- Mutasyon: **14 mutasyonun 14'ü** en az bir adı belli testi kırmızıya çevirdi. Her birinden sonra `cmp` ile dosyanın **birebir aynı** geri geldiği görüldü.
- `denetim.sh`: **DENETİM TEMİZ** (son kodla).
- Canlı doğrulama (5162/5163): K-1, K-2, K-3, K-6, K-8, K-14 ve TB-16 uçları sınandı; iş iki kez koşturuldu. SQL kanıtı alındı.
- Gerçek başsız Chrome: 3 ekran durumu × 1280/375 × BASİT/PRO × açık/koyu = **24 ölçüm**, ayrıca 2 dilekçe ölçümü. Görüntüler Read ile incelendi.
- Test kiracısı silindi. `tenants` sayısı 15 → 15.

## 1A. Tur 2 (30.09 19:26–20:45) — doğrulayıcı ve tasarım hakemi bulgularının kapatılması

> Tur 1 aşağıda olduğu gibi duruyor. Bu bölüm tur 2'nin yaptıklarını, kanıtlarını ve kalanları özetler. Tur 2 çalışması 19:26'da başlayıp yarım kalan bir oturumun (yedek: `/private/tmp/kuyumcu-serit/YASAL-2/tur2-yedek/`) üzerine kuruldu; yarım değişiklikler tek tek okunup doğrulandı, eksikleri tamamlandı.

### Bulgu → yapılan
| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| Nakit sınırı aşımında alan altında hata yok, sebep modal dibinde "Eksik olan" önekiyle | KRİTİK | `#th-tutar` altına kırmızı `Hata` ("Nakit en çok 10.000 ₺ olabilir; kalanı kartla ya da havaleyle alın."); kutu `uyari-kirmizi`; `TahsilatKilidi` bu durumda "Eksik olan" yazmıyor | bUnit `Tahsilat_kismen_izinli_…_KAYDET_kilitli_sebebi_yazili`; T2M8-1; tarayıcı A3 ×8 (`kutuKirmizi:true`, `eksikOlan:false`, `kaydetPasif:true`) |
| Uyum nakit sekmesinde MASAK kutuları "0" ve ilgisiz "+ Yeni bildirim" birincili | KRİTİK | Nakit sekmesinde MASAK özet kutuları ve "+ Yeni bildirim" çizilmez; sayfa başı eylemsiz çizilir (boş satır 40 px boşluk bırakıyordu — tarayıcıda görüldü, düzeltildi). Açık kayıtlar en acil önce (ÖDEME/son gün); yalnız ilk satırın "Dilekçeyi yazdır"ı birincil | bUnit `Uyum_nakit_sekmesinde_MASAK_…`, `Ekran_basina_TEK_birincil_…`; T2M6-1, T2M10-1, T2M14-1; tarayıcı B1 ×8 (`yeniBildirim:false`, `ozetKutu:false`, tek birincil) |
| BASİT'te "kısmen nakit" satırı gerekçesiz | KRİTİK | Liste satırına iki modda aynı tek satır gerekçe: GÜNLÜK "Aynı gün bu kişiyle toplam X nakit (sınır Y)"; İŞLEM "İşlem toplamı 41.250,50 ₺; bunun 11.250,50 ₺ kadarı nakit (sınır 30.000 ₺)"; TAKSİT "{gün} tarihli, sınırı aşan işlemin borcuna X nakit alındı". Sunucu `islemToplamTl` ve `kaynakGunu` döndürür (defterden, tahmin yok) | HTTP `Liste_ISLEM_TUTARI_satirinda_fis_toplami_…`; bUnit `Satir_gerekcesi_BASIT_ve_PRO_modda_AYNI_…`; T2M3-1, T2M11-1; canlı `ISLEM_TUTARI … 11250.5 \| 41250.5` |
| "Kartla al" düğmesi bir şey yapmıyor; Nakit gizleniyor | KRİTİK | İki düğme kaldırıldı. Nakit seçicide GÖRÜNÜR ama PASİF, alt satırında "Bu borçta nakit alınamaz" | bUnit `Tahsilat_buyuk_islem_borcunda_NAKIT_PASIF_…`; T2M7-1; tarayıcı A1/A2 (`Nakit Bu borçta nakit alınamaz[PASIF] \| Kart \| Havale / EFT`, `kart-dugme=false`) |
| Çekmecede "(isteğe bağlı) (isteğe bağlı)" ve üçlü sayaç | KRİTİK | Sözlükte `EvrakNo` = "Evrak no"; `CokSatir SayacGoster="false"`; alan altı sayaç kaldırıldı; tek sayaç Kaydet kilit sebebinde ("açıklama en az 20 karakter olmalı (4/20)"); tarih boşsa kilit sebebi "bildirim tarihini seçin" | bUnit `Bildirildi_cekmecesi_…`; T2M9-1; tarayıcı C1 (1280 PRO açık, 375 BASİT koyu): `tekrar:false`, sayaç yalnız `["4/20"]` |
| K-A kaynaksız nakit Uyum'da görünmüyor | ORTA | Uygulayıcı kararı (Taşkın + Ertuğ rolü; ayrı bir oylama yapılmadı, orkestratör isterse kurula sunar): ihlal satırı AÇILMAZ (yanlış son gün/bildirim doğardı) ama Uyum → Nakit sınırı sekmesinde **salt okunur, KAPALI akordeon** "Kaynağı bilinmeyen borçtan alınan nakit" listesi. Yeni uç `GET /api/v1/tevsik/ihlaller/dogrulanamayanlar` (SAHIP·MUDUR·MUHASEBE, son 90 gün, sınır 100 + `dahaVar`, liste okuma izi). Kaynak: `TEVSIK_TAKSIT_DOGRULANAMADI` denetim izi | HTTP `Dogrulanamayanlar_ACILIS_…_TEZGAHTAR_403_okuma_izi`; bUnit `Kaynagi_bilinmeyen_nakit_KAPALI_akordeonda_…`; T2M4-1; canlı 200 `Kemal Devirli 20000/20000`, SQL `TevsikDogrulanamayan "liste: 1 kayıt"`; tarayıcı B2 |
| Aynı nakit hem GÜNLÜK hem TAKSİT | DÜŞÜK | Aynı cari × gün için GÜNLÜK satırı varsa TAKSİT satırı açılmaz; tahsilat ve büyük işlem kimlikleri GÜNLÜK satırının `hareket_idleri`'ne eklenir (tek olay, tek bildirim) | DB `Ayni_cari_gun_GUNLUK_varsa_TAKSIT_ikinci_satir_ACMAZ_kanit_birlesir`; T2M1-1; canlı: BUYUK için tek satır `GUNLUK_KUMULATIF 40000`, `JSON_LENGTH=4` (2 TAHSILAT + 2 SATIS), TAKSİT satırı yok |
| K-16 şube süzgeçli bağlam testsiz | DÜŞÜK | Tarayıcı fail-closed: `db.SubeFiltresiAcik` ise `InvalidOperationException` (eksik tarama yapılmaz) + `OlusturSubeli` ile ikinci test | DB `K16_sube_suzgecli_baglamda_tarama_SESSIZCE_eksik_saymaz_DURUR`; T2M2-1 |
| Red/önizleme metni elle veresiyeyi "satıştan" diyor | DÜŞÜK | Kaynak türü uydurulmaz: "…tarihli işlemden kalan borç"; sözlükte "Büyük işlemin taksiti nakit" | HTTP/DB testleri (`DoesNotContain("satıştan")`); T2M5-1; canlı önizleme `sebep:"…tarihli işlemden kalan borç"` |
| ODEME bildirimi "vergi dairesine" diyor (kanal teyitsiz, T-1) | DÜŞÜK | "idareye (vergi dairesi — bildirim yolunu mali müşavirinizle teyit edin) … ödeyen adına ceza kesilmez. Dilekçe taslağı …"; Uyum açıklaması aynı tutucu kalıpta | saf `Bildirim_metni_yone_gore_…`; canlı bildirim gövdesi SQL |
| (ek, canlıda görüldü) Bildirim gövdesi İŞLEM_TUTARI'nda "bir kişiyle 11.250,50 TL nakit işlem var (sınır 30.000)" diye çelişkili | — | Gövde türe göre: İŞLEM "sınırı (30.000,00 TL) aşan bir işlemin 11.250,50 TL'si nakit yapıldı"; TAKSİT "…aşan bir işlemin borcuna X nakit alındı"; GÜNLÜK "toplam X nakit işlem var (sınır Y)" | saf `Bildirim_govdesi_ISLEM_TUTARI_ve_TAKSIT_nedeni_soyler_GUNLUK_toplam_der`; T2M12-1 |
| (ek, gerçek tarayıcıda görüldü) 375 koyu: Mehmet Ali'nin tahsilat penceresinde önceki carinin "en çok 10.000 ₺" kutusu | — | `TahsilatAc` ilk çizimden önce `thTaksit`'i temizler; geç gelen önizleme başka carinin penceresine yazılmaz (`tahsilatCari.Id` kontrolü) | bUnit `Tahsilat_baska_cariye_gecince_ONCEKI_carinin_nakit_kutusu_gorunmez` (kasa önbelleği bayat + kasa isteği bekletilerek); T2M13-2 (T2M13-1 önbellek yüzünden yakalanmadı → test güçlendirildi → kırmızı); tarayıcı A1 375 koyu tekrar ölçümü: "Bu borç 30.000 ₺ tutarı aşan bir işlemden…", ödeme Kart |
| Fiş onayı / yazdırılan fişte "kalan borç yalnız kartla/havaleyle" satırı | ORTA | **Bu şeridin dosya alanı dışında** (`Islemler.razor`, fiş şablonu) → SATIŞ şeridine devredildi (§ 13/1). Veri hazır: `GET /tevsik/gunluk?cariId=&fisToplamTl=` → `taksit.nakitAlinabilir` | — |
| K-7 tediye yönünün üretim çağıranı yok | DÜŞÜK | Değişmedi: kodda tedarikçiye sonradan nakit ödeme ucu yok; kapı yöne duyarlı ve DB testiyle kanıtlı (§ 13/2) | — |
| Ortam: 5199 kapalı, tenants sayısı oynuyor | DÜŞÜK | Bu paketin ürünü değil; 5199'a dokunulmadı | — |

### Tur 2 ölçüleri
- **Build:** `dotnet build Kuyumcu.sln` → **0 hata, 0 uyarı** (b5.log).
- **Filtreli testler:** `Tevsik|YASAL2|UX1C3Ekran|Evrak|IsSenaryo|IsCatisi` → **331/331**; son koddan sonra `Tevsik` → **162/162**.
- **Yeni test (tur 2):** 11 — DB 2 (`K16_sube_suzgecli…`, `Ayni_cari_gun_GUNLUK…`), HTTP 2 (`Liste_ISLEM_TUTARI…`, `Dogrulanamayanlar…`), saf 1 (`Bildirim_govdesi_…`), bUnit 6 (`Satir_gerekcesi_…` ×2 teori, `Ekran_basina_TEK_birincil…`, `Uyum_nakit_sekmesinde…`, `Kaynagi_bilinmeyen_nakit…`, `Tahsilat_baska_cariye_gecince…`). Mevcut 5 bUnit testi yeni davranışa uyarlandı (kart/havale düğmesi yok, Nakit pasif, tek sayaç, EvrakNo tekrarsız).
- **denetim.sh:** **DENETİM TEMİZ** (çıkış 0; `t2/denetim2.log`, son kod).
- **Tam süit (son kod, tur başına bir kez):** `dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pYASAL-2-t2` (trx `…2026-09-30_20_34_20.trx`) → **Toplam 8101 · Geçti 8099 · Başarısız 1 · Atlanan 1**. Tek kırmızı `SATIS2EkranTestleri.PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet_kutular_KATLI_tiklaninca_acilir` (`:255 Assert.False`): tek başına sınıfı koşulunca **25/25 yeşil** → yük altında kararsız; bu paketin dokunduğu hiçbir dosyayı (`Islemler.razor`, SATIS-2 testi) kullanmıyor, sahibi SATIŞ-2. Tur 1'deki iki başkasına ait kırmızı (KATMAN-2, KESİF-P1) artık yeşil. **Bu paketin ürettiği yeni kırmızı: 0.**
- (Tur 2'nin ortasında ara kodla başlatılan bir tam süit, son kod değişince bilerek durduruldu; sonuç sayılmadı.)

### Tur 2 mutasyon tablosu
Betik `/private/tmp/kuyumcu-serit/YASAL-2/t2/mut-t2.zsh`, `mut-t2b.zsh`; sonuç `t2/mut-sonuc.txt`. Her satır: tek eşleşme → derleme 0 → filtreli test → yedeği geri kopyala → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| T2M1-1 | GÜNLÜK varsa TAKSİT'i atlama koşulu `false &&` | `TevsikIhlalDbTestleri.Ayni_cari_gun_GUNLUK_varsa_TAKSIT_ikinci_satir_ACMAZ_kanit_birlesir` | AYNI |
| T2M2-1 | Şube süzgeci fail-closed kapısı `false &&` | `TevsikIhlalDbTestleri.K16_sube_suzgecli_baglamda_tarama_SESSIZCE_eksik_saymaz_DURUR` | AYNI |
| T2M3-1 | İşlem toplamı gerekçesi `null` | `TevsikIhlalHttpTestleri.Liste_ISLEM_TUTARI_satirinda_fis_toplami_TAKSIT_satirinda_kaynak_gunu_doner` | AYNI |
| T2M4-1 | Doğrulanamayan listesi yanlış iz türünden | `TevsikIhlalHttpTestleri.Dogrulanamayanlar_ACILIS_bakiyesinden_nakit_satiri_listede_TEZGAHTAR_403_okuma_izi` | AYNI |
| T2M5-1 | Red metni tekrar "satıştan" | `TevsikTaksitDbTestleri.K7_40000_veresiye_alis_iki_kez_10000_nakit_odeme_400` | AYNI |
| T2M6-1 | MASAK özet kutuları nakit sekmesinde de | `TevsikEkranTestleri.Uyum_nakit_sekmesinde_MASAK_ozet_kutulari_ve_Yeni_bildirim_YOK_diger_sekmede_VAR` | AYNI |
| T2M7-1 | Nakit seçeneği pasif değil | `TevsikEkranTestleri.Tahsilat_buyuk_islem_borcunda_NAKIT_PASIF_ve_nedenli_olu_dugme_yok_havale_govdeye_gider` (mutasyon anındaki adı `…NAKIT_secenegi_YOK_…`; sonra adı düzeltildi) | AYNI |
| T2M8-1 | Tutar alanı altındaki hata kaldırıldı | `TevsikEkranTestleri.Tahsilat_kismen_izinli_nakitte_sinir_cumlesi_ve_asiminda_KAYDET_kilitli_sebebi_yazili` | AYNI |
| T2M9-1 | Gerekçe alanı sayacı geri açıldı | `TevsikEkranTestleri.Bildirildi_cekmecesi_gerekce_20_karakterden_kisaysa_kilitli_gecerli_govde_POST_edilir_basari_bandi` | AYNI |
| T2M10-1 | Tüm satırların dilekçesi birincil | `…Uyum_nakit_sekmesinde_…`, `…Ekran_basina_TEK_birincil_EN_ACIL_odeme_satirinin_dilekcesi` | AYNI |
| T2M11-1 | GÜNLÜK gerekçe cümlesi düştü | `…Satir_gerekcesi_BASIT_ve_PRO_modda_AYNI_ve_fis_toplami_yazili(False)` ve `(True)` | AYNI |
| T2M12-1 | Bildirim gövdesinin İŞLEM dalı kapatıldı | `TevsikIhlalSafTestleri.Bildirim_govdesi_ISLEM_TUTARI_ve_TAKSIT_nedeni_soyler_GUNLUK_toplam_der` | AYNI |
| T2M13-1 | `TahsilatAc` içindeki eski kutu temizliği silindi | **hiçbiri** (kasa listesi önbellekten eşzamanlı geldiği için bUnit'te yarış oluşmuyordu) → TEST AÇIĞI, test kasa önbelleğini bayatlatıp kasa isteğini bekletecek şekilde güçlendirildi | AYNI |
| T2M13-2 | aynı mutasyon, güçlendirilmiş testle | `TevsikEkranTestleri.Tahsilat_baska_cariye_gecince_ONCEKI_carinin_nakit_kutusu_gorunmez` | AYNI |
| T2M14-1 | Nakit sekmesinde de eylemli sayfa başı | `…Uyum_nakit_sekmesinde_MASAK_ozet_kutulari_ve_Yeni_bildirim_YOK_diger_sekmede_VAR` | AYNI |

Son mutasyondan sonra temiz derleme: 0 hata 0 uyarı.

### Tur 2 canlı doğrulama (API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETYASAL2-TEST-TUR2 Altın Sarayı Kuyumculuk` (`01M3SMNMCZWWMF5M9E1EKDSZQ2`), rastgele süreç içi sistem anahtarı → davet → kayıt (sır değerleri yazılmadı). Faz 1 `Tevsik__Etkin=false` (kapıyı kaçan geçmiş fikstürü), faz 2 normal yapılandırma.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Faz 1: BUYUK 62.000 veresiye + aynı gün 2 × 20.000 NAKİT tahsilat, KUCUK 41.250,50 (11.250,50 nakit + 30.000 kart), NAKITCI 20.000 + 15.000 nakit, HURDACI 38.256 nakit alış, KARMA 10.000 + 50.000 veresiye | 200 | hepsi OK |
| Faz 2: BUYUK önizleme | kapalı, "işlemden" | 200 `kuralDevrede:true, nakitAlinabilir:0, sebep:"30.000,00 TL'yi aşan 30.09.2026 tarihli işlemden kalan borç"` |
| KARMA önizleme | 10.000 | 200 `nakitAlinabilir:10000.00` |
| BUYUK 5.000 NAKİT (kural açık) | 400 | 400 (günlük toplam 45.000 > 30.000) |
| DEVIR düzeltme +40.000 → 20.000 NAKİT (K-A) | 200 + iz | 200 · SQL `TEVSIK_TAKSIT_DOGRULANAMADI "nakit 20000.00 TL; kaynağı bilinmeyen bakiyeden 20000.00 TL"` |
| İş `TEVSIK_KURTARMA/calistir` | BUYUK için TEK satır | 200 `BASARILI · uretilenBildirim 4 · incelenen 10` |
| `GET /tevsik/ihlaller?durum=ACIK` | 4 satır, TAKSİT yok, İŞLEM satırında fiş toplamı | 200: `GUNLUK TAHSILAT 40000` · `GUNLUK ODEME 35042.73 (son 2026-10-07, 6 iş günü)` · `GUNLUK TAHSILAT 35000` · `ISLEM_TUTARI TAHSILAT 11250.5 \| islemToplamTl 41250.5` |
| SQL `tevsik_ihlalleri` | aynı | 4 satır; BUYUK satırı `JSON_LENGTH(hareket_idleri)=4` → `SATIS 20000`, `SATIS 21000`, `TAHSILAT -20000` ×2 (taksit kanıtı birleşti) |
| SQL `bildirimler` | 4 × KRİTİK; ODEME "idareye (vergi dairesi — … teyit edin)" | 4 × KRITIK; ODEME gövdesi "Son güne (07.10.2026) kadar idareye (vergi dairesi — bildirim yolunu mali müşavirinizle tey…"; (İŞLEM gövdesinin eski kalıbı canlıda görüldü → yukarıdaki ek düzeltme; yeni kalıp saf testle kanıtlı) |
| `GET /tevsik/ihlaller/dogrulanamayanlar` | 200, 1 satır | 200 `{satirlar:[{kaynak:"TAHSILAT", cariAd:"Kemal Devirli", nakitTl:20000, dogrulanamayanTl:20000}], sinir:100, dahaVar:false, gunSayisi:90}` · SQL okuma izi `TevsikDogrulanamayan "liste: 1 kayıt"` |

Temizlik: API ve vekil `kill` (5162/5163 LISTEN 0); `zsh kiraci-temizle.sh 01M3SMNMCZWWMF5M9E1EKDSZQ2 --uygula` → ikinci koşum "Kiracı YOK"; SQL `tenants WHERE id` 0, `tevsik_ihlalleri` 0, `hareketler` 0; davet satırı 1 → 0 (`DELETE … ROW_COUNT 1`). `tenants`: başlangıç **14**, son **15** — fark başka şeritlerin kiracıları (bu paketin önekiyle kiracı 0; QA-TEST-* kiracılarına dokunulmadı). Sır dosyaları `printf ''` ile boşaltıldı.

### Tur 2 gerçek başsız Chrome ölçümü
Araç: `betikler/tur/cdp.mjs` + `sikisma.js`/`ekstra.js` (tasarım hakemiyle aynı `ortak.mjs`), CDP 9910–9914, vekil 5163. Betikler ve çıktılar: `/private/tmp/kuyumcu-serit/YASAL-2/t2/tar/` (`a-tahsilat.mjs`, `b-uyum.mjs`, `c-cekmece.mjs`, `d-panel.mjs`, `olcum.jsonl` = son kod, `olcum-tur2a.jsonl` = düzeltmeler öncesi, `foto/*.png`).

| Ekran × durum | 1280 BASİT açık | 1280 PRO açık | 1280 BASİT koyu | 1280 PRO koyu | 375 BASİT açık | 375 PRO açık | 375 BASİT koyu | 375 PRO koyu |
|---|---|---|---|---|---|---|---|---|
| Tahsilat — nakit kapalı (A1) | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* |
| Tahsilat — 15.000 > 10.000 (A3) | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* |
| Uyum → Nakit sınırı (B1) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Panel (D1) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

"0" = yatay kaydırma 0, taşan yazı (K3) 0, kırılan etiket 0, üst üste binen (K9) 0, görünen `[hidden]` (K1) 0, `TRY` kalıbı (K5) 0, modal içi taşma 0. \* K9=6: modalın arkasındaki sayfa öğeleri (tur 1'de taban ölçümüyle aynı; taksit bloğu olmayan caride de 6) — bu paketten değil. Ek ölçümler: A2 (seçici açık) modal dışı 8 = açılır liste katmanı (beklenen); B2 (kaynaksız akordeon açık) 0; C1 çekmece (1280 PRO açık, 375 BASİT koyu) 0; C2 dilekçe (aynı iki kombinasyon) 0 ve tek birincil ("Dilekçeyi yazdır").

Read ile görülen görüntüler: `A3-karma-asim-PRO-1280-aydinlik` (alan altında kırmızı hata, kutu kırmızı, "Eksik olan" yok) · `A3-karma-asim-BASIT-375-aydinlik` · `A2-buyuk-secici-acik-PRO-1280` (Nakit gri + "Bu borçta nakit alınamaz") · `A1-buyuk-BASIT-375-karanlik` (düzeltme öncesi yanlış carinin kutusu → düzeltme sonrası doğru kutu, ödeme Kart) · `B1-uyum-nakit-BASIT-1280-aydinlik` (düzeltme öncesi sekme üstü boşluk) · `B1-uyum-nakit-PRO-1280-aydinlik` (boşluk yok, MASAK kutusu yok, tek birincil, satır gerekçeleri) · `B1-uyum-nakit-BASIT-375-aydinlik` (ilk ekranda liste görünür) · `B1-uyum-nakit-PRO-1280-karanlik` · `B2-uyum-dogrulanamayan-acik-BASIT-1280` · `C1-bildirildi-kisa-BASIT-375-karanlik` ("Evrak no (isteğe bağlı)" tek, tek sayaç) · `C2-dilekce-BASIT-375-karanlik` · `D1-panel-BASIT-375-aydinlik`.

### Tur 2 değişen dosyalar
- Api: `Modules/Tevsik/TevsikIhlalModulu.cs` (tekilleştirme, fail-closed, gerekçe alanları, doğrulanamayan listesi + uç, bildirim gövdesi), `Modules/Tevsik/TevsikKapisi.cs` ("işlemden").
- Contracts: `Sayfalar/NakitSiniriSozlesmeleri.cs` (`IslemToplamTl`, `KaynakGunu`, `TevsikDogrulanamayan*`).
- Core: `I18n/Sozlukler.TevsikTaksit.cs` (TR+EN, dosya sonuna yeni anahtarlar; EvrakNo, metin düzeltmeleri).
- Web: `Pages/Cariler.razor` (yalnız tahsilat penceresi), `Pages/Uyum.razor` (sayfa başı + özet koşulu), `Bilesenler/NakitSiniriAsimlari.razor`.
- Testler: `TevsikIhlalTestleri.cs`, `TevsikEkranTestleri.cs`, `TevsikTaksitTestleri.cs`.
- Tek dokunuş gerekçesi: `Cariler.razor` ve `Uyum.razor` bu paketin tur 1'de açtığı sayfalar; tur 2 dokunuşu aynı paketin hakem düzeltmesidir (aynı dalga).
- Dokunulmayanlar: `YasalSinirlar.razor`, `YasalDegisiklikBandi.razor`, `Data/YasalParametre.cs`, `Islemler.razor`, MIMARI/şartname belgeleri, SONRAKI-OTURUM-PROMPT.md.

## 1B. Tur 3 (30.09 22:13–23:10) — orkestratör 22:20 bulgularının kapatılması

> Tur 1 ve tur 2 aşağıda aynen duruyor. Bu bölüm yalnız tur 3'ü anlatır. Yeni migration yok. COMMIT yok.

### Bulgu → yapılan
| # | Bulgu (orkestratör 22:20) | Yapılan | Kanıt |
|---|---|---|---|
| 1 | Red metninde ham mevzuat kodu var, "en çok 0,00 TL" çıkıyor, döviz tahsilatında birim yanlış | `TevsikKapisi.TaksitMesaji` yeniden yazıldı. Artık **tek cümle + ne yapmalı**. Metinde "459", "VUK", "§" ya da "Tebliğ" yok. Nakit hiç alınamıyorsa tutar yazılmaz: "Bu borç 30.09.2026 tarihli, 30.000,00 TL'yi aşan bir işlemden kalıyor; nakit alınamaz, kartla ya da havaleyle alın." Nakit kısmen alınabiliyorsa: "Bu ödemede en çok 10.000,00 TL nakit alınabilir; kalanı kartla ya da havaleyle alın." Döviz tahsilatında (`TaksitDogrula(..., dovizMi: true)`) metin "TL **karşılığı**" der. Dayanak ekranda yalnız ipucunda durur: tahsilat kutusundaki "Neden?" için `title` = "Dayanak: 459 sıra no.lu VUK Genel Tebliği, 4.1.2". `NedenMetin` içinden kod çıkarıldı | saf `Tur3_red_metni_*` (6 satır) · HTTP `K1_…`, `K3_…`, `Doviz_nakit_…`, **yeni** `Doviz_kismen_izinli_red_TL_KARSILIGI_der` · DB `K5_…`, `K7_…` (`TaksitMesajDenetimi.KodsuzTekCumle`) · bUnit `Tur3_Tahsilat_kutusu_dayanak_YALNIZ_title_ipucunda…` · canlı R2/R3 · tarayıcı A1 ×8, A4 ×2 |
| 2 | Satış fişi onayında "kalan borç yalnız kart/havale" satırı yok | Yeni `Pages/Islemler.Taksit.cs` (partial). Satışta veresiye bacağı ve gerçek müşteri varsa kayıttan **sonra** `GET /tevsik/gunluk?cariId=&fisToplamTl=` sorulur. Sunucu `kuralEtkin && islemTutariKuraliDevrede` derse başarı bandının altında tek satır çıkar: "Bu fişin kalan borcu yalnız kartla ya da havaleyle alınabilir." (`#islem-taksit-uyari`). İstemcide eşik sayısı yok. Sorgu düşerse ayrı hata bandı çıkar (`#islem-taksit-hata`, UX-S28). `Islemler.razor` satır sayısı **1399'da kaldı** (≤1400 tavanı): yalnız mevcut satırlara çağrılar eklendi | bUnit `YASAL2FisTaksitEkranTestleri` (4) · canlı G1 `fis 41000 → islemTutariKuraliDevrede=true`, G2 `20000 → false` · tarayıcı E1 ×8 (8 gerçek VERESİYE satışı; SQL `SATIS VERESIYE 41000.00` ×8) |
| 3 | Panel: şerit ile "Bekleyen iş" farklı yerden sayıyor | Tek kaynak üstteki kırmızı şerittir (`GET /tevsik/ihlaller`). (a) `ilgiliVarlikTuru=TevsikIhlali` bildirimleri BASİT "Bekleyen iş" listesine **girmez** (ikinci sayım yok). (b) Şerit, yüklediği sayıyı `SayiDegisti` ile Panel'e bildirir. İki yüzdeki "Bekleyen iş" kutusu, aşım varken "iş yok / bugün temiz" **demez**: "Başka iş yok. Nakit sınırı aşımları üstteki kırmızı kutuda." | bUnit `S31PanelSadeTestleri.YASAL2_Tur3_*` (3) · tarayıcı D1 ×8 (`bekleyenNakitSatiri:0`, şerit 1 + 3) · görüntü D1-PRO-1280-karanlik |
| 4 | Uyum → Nakit sınırı aşımları: açıklama satırı ve teknik kutu açıkta | Açılışta listeden önce metin **0**. Açıklama, bayram notu ve `DegismezIpucu`, en altta KAPALI `Katlanir` "Bu liste nasıl oluşur?" (`#nsa-nasil`) içinde | bUnit `Tur3_Uyum_nakit_acilista_aciklama_satiri_YOK…`, `Sekme_acik_kayitlar…` (uyarlandı) · tarayıcı B1 ×8 (`listedenOnceMetin:[]`, `nasilAcik:false`, `kod459:false`, tek birincil), B2 ×2 (açık hâli) |

Yeni metinlerin hepsi yalnız YASAL-2 sözlüğüne, dosyanın sonuna eklendi (`Sozlukler.TevsikTaksit.cs`, TR+EN): `Dayanak`, `NasilCalisir`, `FisKalanBorcBanka`, `FisKalanBorcHata`, `PanelSeritte`.

### Tur 3 ölçüleri
- **Build:** `dotnet build Kuyumcu.sln` → **0 hata, 0 uyarı** (`t3/b5.log`).
- **Filtreli testler:** `Tevsik|YASAL2|S31PanelSade|KarmaOdemeEkran|Panel` → **317/317** (`t3/t4.log`).
- **Yeni test (tur 3): 17.** Saf 6 (iki teori × 3), HTTP 1, bUnit 10 (`YASAL2FisTaksit` 4, `TevsikEkran` 2, `S31PanelSade` 3, ayrıca 1 mevcut test uyarlandı). Mevcut 5 test yeni metne uyarlandı: K1, K3, K5, K7, `Doviz_nakit_…`, `Sekme_acik_kayitlar…`.
- **Tam süit (son kod, tur başına 1 kez):** trx `/private/tmp/kuyumcu-serit/trx-pYASAL-2-t3` → **Toplam 8152 · Geçti 8149 · Başarısız 2 · Atlandı 1**. İki kırmızı:
  - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` — hata `Unknown database 'kuyumcu_t1_eski'`.
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner`.

  Bu iki test, t1 şeridiyle paylaşılan geçici veritabanında yarışa düşüyor. İkisi ayrı koşulunca **15/15 yeşil** (`t3/mig.log`). Bu paketin dokunduğu hiçbir dosyayı kullanmıyorlar. **Bu paketin ürettiği yeni kırmızı: 0.**
- **denetim.sh:** **DENETİM TEMİZ**, çıkış 0 (`t3/denetim.log`).

### Tur 3 mutasyon tablosu
Betik `/private/tmp/kuyumcu-serit/YASAL-2/t3/mut.zsh`, liste `t3/mut/liste.txt`, sonuç `t3/mut/sonuc.txt`. Her mutasyon için sıra aynı: tek eşleşme → derleme (0 hata) → filtreli test → yedeği geri kopyala → `cmp`. **13 mutasyonun 13'ü kırmızı; 13'ünde `cmp` AYNI.** Son mutasyondan sonra temiz derleme: 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test (seçme) | cmp |
|---|---|---|---|
| T3M1-1 | Red metnine "(459 s. VUK Genel Tebliği §4.1.2)" geri eklendi | `TevsikTaksitSafTestleri.Tur3_red_metni_kismen_izinli…` ×3, `TevsikTaksitHttpTestleri.K3_FIFO…`, `Doviz_kismen_izinli_red_TL_KARSILIGI_der` | AYNI |
| T3M1-2 | "Nakit alınamaz" dalı kapatıldı (→ "en çok 0,00 TL") | `TevsikTaksitDbTestleri.K5_20_11_2024…`, `K7_40000…`, `Tur3_red_metni_nakit_SIFIRSA…`, `K1_62000…` | AYNI |
| T3M1-3 | Döviz tahsilatında `dovizMi: true` kaldırıldı | `TevsikTaksitHttpTestleri.Doviz_kismen_izinli_red_TL_KARSILIGI_der` | AYNI |
| T3M2-1 | Fiş satırı `islemTutariKuraliDevrede` koşulunu bıraktı | `YASAL2FisTaksitEkranTestleri.Veresiye_satis_sunucu_asilmiyor_derse_satir_YOK` | AYNI |
| T3M2-2 | Veresiye koşulu ters çevrildi | `…Kayit_sonrasi_sorgu_duserse_HATA_bandi…`, `…Pesin_KART_satista…`, `…HAD_ASILIYOR…` | AYNI |
| T3M2-3 | Sorgu hatası yutuldu | `…Kayit_sonrasi_sorgu_duserse_HATA_bandi_sessiz_degil` | AYNI |
| T3M2-4 | Kayıt sonrası çağrı razor'dan silindi | `…HAD_ASILIYOR_derse_tek_satir…`, `…HATA_bandi…` | AYNI |
| T3M3-1 | Panel bildirim süzgeci kaldırıldı | `S31PanelSadeTestleri.YASAL2_Tur3_nakit_siniri_asimi_TEK_kaynak…` | AYNI |
| T3M3-2 | Şerit sayıyı Panel'e bildirmiyor | `S31PanelSadeTestleri.YASAL2_Tur3_PRO_bekleyen_kutucugu_serit_sayisini_okur_temiz_demez` | AYNI |
| T3M3-3 | BASİT boş durum yine "Bekleyen iş yok" | `S31PanelSadeTestleri.YASAL2_Tur3_yalniz_nakit_asimi_varken…` | AYNI |
| T3M4-1 | "Bu liste nasıl oluşur?" açık başlıyor | `TevsikEkranTestleri.Tur3_Uyum_nakit_acilista…`, `Sekme_acik_kayitlar…` | AYNI |
| T3M5-1 | Tahsilat "Neden?" `title` dayanağı silindi | `TevsikEkranTestleri.Tur3_Tahsilat_kutusu_dayanak…` | AYNI |
| T3M5-2 | `NedenMetin` içine kod geri kondu | `TevsikEkranTestleri.Tur3_Tahsilat_kutusu_dayanak…` | AYNI |

### Tur 3 canlı doğrulama (API 5162 `--no-build`, vekil 5163)
Kiracı `PAKETYASAL2-TEST-TUR3 Altın Sarayı Kuyumculuk` (`01M3SWNAB465CM7Z2BE136XJ8N`). Kurulum yolu: süreç içi rastgele sistem anahtarı → davet → kayıt. Sır değerleri hiçbir çıktıya yazılmadı. Faz 1'de `Tevsik__Etkin=false` ile geçmiş fikstür kuruldu, faz 2 normal yapılandırmayla koştu. DEVİR (kaynaksız borç) adımı bu turda kur alanı eksik diye 400 aldı ve atlandı; K-A tur 2'de kanıtlı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| R2 KARMA 15.000 NAKİT tahsilat | 400, kodsuz, 10.000 | 400 "Bu ödemede en çok 10.000,00 TL nakit alınabilir; kalanı kartla ya da havaleyle alın." |
| R3 KARMA 250 USD döviz tahsilat | 400, "TL karşılığı" | 400 "Bu ödemede en çok 10.000,00 TL karşılığı nakit alınabilir; kalanı kartla ya da havaleyle alın." |
| R1/R4 BUYUK 5.000 TL / 100 USD NAKİT | 400 | 400. Metni **günlük kapı** verdi (TevsikKurali, bu paketin değil); o metinde kod hâlâ var → § 1B açık kalan 1 |
| G1 `/tevsik/gunluk?fisToplamTl=41000` | devrede | 200 `islemTutariKuraliDevrede=true, kuralEtkin=true` |
| G2 `…fisToplamTl=20000` | değil | 200 `islemTutariKuraliDevrede=false` |
| İş `TEVSIK_KURTARMA` | 4 ihlal | 200 `BASARILI, uretilenBildirim 4` · SQL `tevsik_ihlalleri` 4 satır (GÜNLÜK 40.000 / GÜNLÜK ÖDEME 35.042,73 / GÜNLÜK 35.000 / İŞLEM 11.250,50) · açık `TevsikIhlali` bildirimi 4 |
| E1 arayüzden 8 veresiye satış (41.000 TL) | 200 + satır | SQL `SATIS VERESIYE 41000.00` ×8 · ekranda `#islem-taksit-uyari` 8/8 |

Temizlik: API ve vekil `kill` ile durduruldu (5162/5163 LISTEN 0). `zsh kiraci-temizle.sh 01M3SWNAB465CM7Z2BE136XJ8N --uygula` çalıştı; ikinci koşum "Kiracı YOK" dedi. `kiraci_davetleri` satırı 1 silindi. `tenants`: başlangıç **14**, son **14**. QA-TEST-* kiracılarına dokunulmadı. `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı.

### Tur 3 gerçek başsız Chrome ölçümü
Araç `betikler/tur/cdp.mjs` + `sikisma.js`/`ekstra.js`. CDP portları 9910, 9911, 9913, 9914 ve 9915. Betikler, `olcum.jsonl` ve `foto/*.png` şurada: `/private/tmp/kuyumcu-serit/YASAL-2/t3/tar/`.

| Ekran × durum | 1280 BASİT açık | 1280 PRO açık | 1280 BASİT koyu | 1280 PRO koyu | 375 BASİT açık | 375 PRO açık | 375 BASİT koyu | 375 PRO koyu |
|---|---|---|---|---|---|---|---|---|
| Panel (D1) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Uyum → Nakit (B1) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Tahsilat — nakit kapalı (A1) | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* |
| Tahsilat — 15.000 > 10.000 (A3) | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* |
| Satış — veresiye fiş onayı (E1) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Tablodaki "0" hücresi şu ölçülerin hepsinin sıfır olduğu anlamına gelir: yatay kaydırma, taşan yazı (K3), kırılan etiket, üst üste binen (K9), görünen `[hidden]` (K1), `TRY` kalıbı (K5) ve modal içi taşma.

\* K9=6: bunlar modalın arkasındaki sayfa öğeleri (tur 1 ve tur 2 taban ölçümüyle aynı). Modal içi taşma 0.

Ek ölçümler de 0 çıktı: A4 döviz red (1280 BASİT açık, 375 PRO koyu) ve B2 "nasıl oluşur" açık (1280 PRO açık, 375 BASİT koyu).

Read ile görülen görüntüler:
- `D1-panel-BASIT-1280-aydinlik`: düzeltme öncesi "Bekleyen iş yok" yazıyordu, şeritle çelişiyordu. Tarayıcıda görüldü ve düzeltildi.
- `D1-panel-PRO-1280-aydinlik`: düzeltme öncesi "Bekleyen iş yok — bugün temiz" yazıyordu. Düzeltildi.
- `D1-panel-PRO-1280-karanlik`: son hâl, "Başka iş yok. Nakit sınırı aşımları üstteki kırmızı kutuda."
- `B1-uyum-nakit-BASIT-375-aydinlik`: liste doğrudan başlıyor, açıklama satırı yok.
- `B2-uyum-nasil-acik-PRO-1280-aydinlik`: kutu açık; açıklama, bayram notu ve değişmezlik kutusu içeride.
- `A3-karma-asim-BASIT-1280-aydinlik`.
- `A4-doviz-red-PRO-375-karanlik`: kırmızı bant tek cümle, "TL karşılığı", kod yok.
- `E1-satis-veresiye-BASIT-1280-aydinlik` ve `E1-satis-veresiye-PRO-375-karanlik`: fiş onayı altında tek satır.

### Tur 3 değişen dosyalar
- Api:
  - `Modules/Tevsik/TevsikKapisi.cs` (`TaksitMesaji`, `dovizMi`)
  - `Modules/Cari/CariModulu.cs` (yalnız `DovizTahsilatGir` kapı satırı: `dovizMi: true`)
- Core: `I18n/Sozlukler.TevsikTaksit.cs` (sona 5 anahtar; `NedenMetin` kodsuz)
- Web:
  - `Pages/Islemler.Taksit.cs` (YENİ)
  - `Pages/Islemler.razor`: 4 mevcut satıra çağrı eklendi, satır sayısı değişmedi (1399)
  - `Pages/Panel.razor`: 106, 242, 635, 730 ve 890. satırlar
  - `Pages/Cariler.razor`: yalnız tahsilat penceresindeki `title`
  - `Bilesenler/NakitSiniriAsimlari.razor`
  - `Bilesenler/NakitSiniriPanelSatiri.razor`
- Testler:
  - `TevsikTaksitTestleri.cs`
  - `TevsikEkranTestleri.cs`
  - `YASAL2FisTaksitEkranTestleri.cs` (YENİ)
  - `S31PanelSadeTestleri.cs` (sona 3 test)
- Tek dokunuş gerekçesi:
  - `Cariler.razor`, `Uyum` bileşeni ve `Panel.razor`: bu paketin tur 1'de açtığı yerlerdir; tur 3 dokunuşu aynı paketin orkestratör düzeltmesidir.
  - `Islemler.razor`: şerit dosya alanının **dışında**. Orkestratör 22:20 madde 2 açıkça istedi. Dokunuş en küçük tutuldu: yeni satır 0, mantık ayrı partial dosyada. SATIŞ şeridine bilgi.
- Dokunulmayanlar: `YasalSinirlar.razor`, `YasalDegisiklikBandi.razor`, `TevsikKurali.cs`, MIMARI/şartname belgeleri, SONRAKI-OTURUM-PROMPT.md.

### Tur 3 açık kalanlar (sahibiyle)
1. **Günlük kapının red metni** (`TevsikKurali.Degerlendir`: "Tevsik zorunluluğu: … 459 s. VUK Genel Tebliği … (dayanak: VUK mükerrer md.257)") hâlâ ham kodlu. Aynı cariye aynı gün 30.000 TL üstü nakitte tahsilat ve döviz tahsilat pencerelerinde görünüyor (canlı R1, R4). Dosya bu şeridin alanında değil ("TevsikKurali.cs yalnız TaksitDegerlendir"). Satış akışının testleri de bu metne bağlı. **Sahibi: ŞK-A / YASAL-1 şeridi.** Öneri: aynı kalıp — tek cümle, dayanak yalnız `title`'da.
2. Satış ekranı: kayıttan sonra form sıfırlanınca üstteki "Bu tutarda nakit alınamaz — tamamını kart ya da havaleyle alın" bandı, toplam 0 iken de kalıyor. Görüntü: E1-BASIT-1280. Bu paketin kodu değil (`FisNakitUyari`). **Sahibi: SATIŞ şeridi.**
3. Tam süitteki 2 migration testi kırmızısı, t1 ile paylaşılan `kuyumcu_t1_eski` veritabanı yarışından geliyor. Tek başına 15/15 yeşil. **Sahibi: orkestratör / t1 şeridi.**

## 2. Yapılanlar

### 2.1 TB-15 — Büyük işlemin taksiti nakit alınamaz
| Dosya | Değişiklik |
|---|---|
| `Modules/Tevsik/TevsikKurali.cs` | Yeni saf kural `TaksitKurali` eklendi. İçinde `FifoDagit`, `Buyuk`, `TaksitDegerlendir`, `NakitAlinabilir` ve (TB-16 için) `BildirimSonGunu` var. Tipler: `TaksitKaynak`, `TaksitParti`, `TaksitKaynagi`, `TaksitSonuc`. |
| `Modules/Vade/VadeKapisi.cs` | Defter yürüyüşü tek bir yardımcıya taşındı: `DefteriYuru`. `AcikBorclar` bu yardımcıyı davranışı değiştirmeden kullanıyor (`duzeltmeDahil:false`). Yeni `AcikPartiler`: açık partileri kaynak hareket, tür ve fiş ile verir; kaynağı bilinmeyen tutarı ayrıca döner. Yeni `TuketimleriOynat`: TB-16 için geçmişi yeniden oynatır. |
| `Modules/Tevsik/TevsikKapisi.cs` | Yeni metotlar: `TaksitKaynaklari`, `KaynakBilgisi`, `TaksitHesapla`, `TaksitOnizle`, `TaksitDogrula`, `TaksitMesaji`. `GunlukNakitToplam`'a dokunulmadı (ŞK-A'nın). |
| `Modules/Cari/CariModulu.cs` | `TahsilatGirHareket` (NAKİT) ve `DovizTahsilatGir` içinde kapı çağrısı eklendi. Yeni `TaksitDogrulanamadiIzi` yardımcısı `TEVSIK_TAKSIT_DOGRULANAMADI` denetim izini yazar. |
| `Modules/Evrak/EvrakModulu.cs` | Nakit tahsil yolunda (`:490-492`) kapı çağrısı ve iz eklendi. |
| `Modules/Tevsik/TevsikController.cs` | `GET /tevsik/gunluk` artık `taksit {kuralDevrede, nakitAlinabilir, buyukIslemTutari, buyukIslemGunu, buyukIslemHaddi, sebep}` alanını da döner. Yanıt tipi artık sözleşme tipi `TevsikGunlukVeri`; alan adları eskisiyle aynı. |
| `Web/Pages/Cariler.razor` | Yalnız tahsilat penceresi değişti. Nakit tamamen kapalıysa "Nakit" seçeneği listeden çıkar, varsayılan Kart olur, ekranda tek cümle ve "Kartla al" / "Havaleyle al" düğmeleri çıkar. Nakit kısmen serbestse sınır cümlesi yazılır; tutar sınırı aşarsa Kaydet kilitlenir ve sebebi yazılır (UX-S30). Açılır "Neden?" bölümü eklendi. Önizleme hatası ayrı bantta gösterilir (UX-S28). |

Kural özeti:
- Ödeme FIFO ile dağılır: önce kaynağı bilinmeyen tutar (en eski sayılır), sonra partiler eskiden yeniye, artan tutar "fazla" sayılır.
- Kaynak işlem: parti bir fişe aitse fişin tamamıdır (storno çifti dışarıda). Fiş yoksa hareketin kendisidir.
- Had, **kaynak günün** haddidir (YASAL-1 tarihli tablo).
- Nakit ancak büyük olmayan partilere düşen toplam kadar olabilir. Nakit bu tutardan büyükse **400** döner.
- `ACILIS` / `CARI_DUZELTME` kaynaklı parti ve defterle açıklanamayan fark kaynaksızdır. Bu tutar **geçer** ama iz bırakır (K-A önerisi).
- `TAKAS` kaynaklı parti kural dışıdır (T-4).
- Kural kapalıyken (`Tevsik:Etkin=false` ya da `IslemTutariKuraliEtkin=false`) karar hesaplanır ama işlem durdurulmaz.

### 2.2 TB-16 — Geç fark edilen aşım: bul, göster, dilekçe, kapat
| Dosya | Değişiklik |
|---|---|
| `Data/TevsikIhlali.cs` (YENİ) | `tevsik_ihlalleri` tablosu. Kolonlar: tür, yön, cari, gün, nakit, had, hareket kimlikleri (JSON), son gün, durum, kapatma alanları, `bildirim_tarihi`, `gec`. `IKiraciVarlik`; şube taşımaz. |
| `Data/KuyumcuDbContext.cs` | DbSet eklendi. UNIQUE `(tenant_id, cari_id, ihlal_gunu, tur)` ve `(tenant_id, durum, bildirim_son_gunu)` indeksleri eklendi. |
| `Modules/Tevsik/TevsikIhlalModulu.cs` (YENİ) | `TevsikIhlalTarayici.Tara` son 10 takvim gününü tarar. Günlük kümülatif **`TevsikKapisi.GunlukNakitToplam` ile** (tek motor). `ISLEM_TUTARI` taraması kısmen nakit ödenmiş fişleri bulur. `TAKSIT` taraması FIFO'yu yeniden oynatır. `SURE_DOLDU` geçişi burada. Aynı dosyada servis ve `TevsikIhlalController` (4 uç) var. |
| `Modules/Isler/Isler/TevsikKurtarmaIsi.cs` (YENİ) | İş kodu `TEVSIK_KURTARMA`, varsayılan **AÇIK**. Her yeni kayıt için KRİTİK bildirim üretir: roller `SAHIP,MUHASEBE`, tekillik `TVS:{id}`, gövdede ad/TCKN yok. `YasalParametreServisi` yoksa iş HATALI biter (fail-closed). |
| `Modules/ModulKayit.cs` | 1 satır: iş ve servis kaydı. |
| `appsettings.json` | `Isler:TevsikKurtarma:Etkin=true` yapıldı ve not güncellendi. |
| `Modules/Meta/DuzeltmePolitikasi.cs` | 1 kayıt eklendi: `S("TevsikIhlali", Turetilmis, …/bildirildi)`. Gerekçe § 3/5. |
| `Contracts/Sayfalar/NakitSiniriSozlesmeleri.cs` (YENİ) | Sözleşme tipleri: `TaksitOnizlemeVeri`, `TevsikGunlukVeri`, `TevsikIhlalListesi/Satiri`, `TevsikDilekce*`, `TevsikKapatmaYaniti`. Sunucu `Task<object>` yerine bu tipleri döner (sözleşme kilidi). |
| `Core/I18n/Sozlukler.TevsikTaksit.cs` (YENİ) | TR+EN metinler, konumsal kayıt (eksik anahtar derlemeyi kırar). Ekranda "tevsik/459/355" geçmez; bu ifadeler yalnız açılır "Neden?" ve dayanak satırında var. |
| `Web/Bilesenler/NakitSiniriAsimlari.razor` (YENİ) | Uyum sekmesi. Açık ve süresi dolmuş kayıtlar listede; kapanmışlar kapalı `Katlanir` içinde. Satırda "Dilekçeyi yazdır", "Bildirildi olarak işaretle" ve yalnız SAHİP için "Yanlış kayıt". Bildirildi için Cekmece (tarih, evrak no, gerekçe ≥20 ve alan altı hata). "Yanlış kayıt" için Cekmece ve ardından OnayKipi. Dilekçe belge görünümünde, `YazdirDugmesi` ile yazdırılır. `DegismezIpucu` eklendi. |
| `Web/Bilesenler/NakitSiniriPanelSatiri.razor` (YENİ) | Panel'de kırmızı uyarı kutusu. ODEME yönü: "n nakit sınırı aşımı — bildirim için son gün 07.10 (k iş günü)". TAHSILAT yönü: "… mali müşavirinize danışın" (geri sayım yok). Yalnız SAHİP ve MUHASEBE; oturum hazır olunca yükler. |
| `Web/Pages/Uyum.razor` | Yeni sekme `nakit` eklendi (enum, sekme listesi, iki eşleme, bir dal). `?sekme=` artık `[SupplyParameterFromQuery]` ile okunuyor (P-15). |
| `Web/Pages/Panel.razor` | 1 satır: `<NakitSiniriPanelSatiri />`. |
| `Web/Servisler/NakitSiniriVeri.cs` (YENİ) | Yalnız gün biçimi yardımcıları. |
| `Web/wwwroot/css/nakit-siniri.css` (YENİ) + `index.html` 1 satır | Stiller yalnız `var()` belirteçleriyle yazıldı. Yazdırmada `@media print` yalnız dilekçeyi kâğıda çıkarır. |
| `denetim.sh` + `ham/olcum-2026-09-18.json` | `BEKLENEN_VARLIK` 71 → 72 (yeni varlık), gerekçe yorumu eklendi. |

Uçlar:

| Uç | Rol | Not |
|---|---|---|
| `GET /api/v1/tevsik/ihlaller?durum=ACIK\|KAPANMAMIS\|BILDIRILDI\|SURE_DOLDU\|GECERSIZ` | SAHIP, MUDUR, MUHASEBE | Liste okuma izi yazılır. Sınır 200 ve "daha var" bilgisi döner (UX-S29). Bayram eksikse bilgi verir. |
| `GET …/{id}/dilekce` | aynı roller | Karşı tarafın tam kimliği yalnız bu uçta döner. **Okuma izi** yazılır. Belge "TASLAK — mali müşavirinizle kontrol edin" damgalıdır. |
| `POST …/{id}/bildirildi` | aynı roller | Gerekçe ≥20 karakter zorunlu. Bildirim tarihi bugünden sonra ya da aşım gününden önce olamaz (400). Son günden sonra bildirildiyse `gec=true` işaretlenir. Kapalı kayda 409. Denetim izi yazılır. |
| `POST …/{id}/gecersiz` | **yalnız SAHIP** | Gerekçe ≥20 karakter. Kapalı kayda 409. Denetim izi yazılır. |
| `POST /api/v1/isler/TEVSIK_KURTARMA/calistir` | mevcut uç | Elle tetikleme. |

## 3. Sapmalar ve gerekçeleri
1. **Tediye yolu yok (K-7).** Kod tabanında tedarikçiye sonradan nakit ödeme yapan bir uç bulunmuyor. `TAHSILAT` türünü üreten yalnız cari tahsilatı, döviz tahsilatı ve evrak tahsili var (grep ile doğrulandı). Kapı yöne duyarlı yazıldı (`alacakYonu:false`). K-7, gerçek ALIŞ veresiye borcuna karşı **DB testiyle** kanıtlandı. Tediye ucu eklendiğinde aynı kapı çağrılmalı; § 10'a yazıldı.
2. **Satış fişi onayı ve yazdırılan fiş bilgi satırı yapılmadı** (şartname § 4.1 ve § 4.3). `Islemler.razor` ve fiş şablonu bu şeridin dosya alanında değil. § 10'a yazıldı, sahibi SATIŞ şeridi.
3. **Dilekçe için ayrı rota açılmadı.** Ayrı bir `@page`, ROTA_KAPSAM sayacını, RotaBasliklari'nı ve qa-turu tabanını kırardı. Dilekçe Uyum sekmesinde belge görünümü olarak çiziliyor; yazdırırken `@media print` yalnız belgeyi basıyor. Ayrıca şartnamede anılan `Web/Belgeler/` dizini ağaçta yok.
4. **Doğrulanamayan geçişler ihlal satırı açmıyor.** Açılış/devir bakiyesinden alınan nakit (K-A) `tevsik_ihlalleri`'ne yazılmıyor. Bunun yerine `denetim_kayitlari`'na `TEVSIK_TAKSIT_DOGRULANAMADI` izi yazılıyor. Gerekçe: bu durum ihlal değil; ihlal satırı açılsaydı yanlış bir "son gün" geri sayımı ve bildirim doğardı. Şartnamedeki "Uyum listesinde görünür satır" isteği bu nedenle yalnız iz olarak karşılandı. § 10'a yazıldı.
5. **"Bildirildi/Yanlış kayıt" geri alınamıyor ve PATCH ucu yok.** Şartname "kapatma bir kez yazılır, silme yok (ADR-0004)" diyor. Bu, DoD EK-4'teki "geri dönüş yolu" isteğine üstün tutuldu. Ekranda `DegismezIpucu` hem gerekçeyi hem düzeltme yolunu yazar (kaynakta ters kayıt, sonra "Yanlış kayıt"). Politika kütüğüne `Turetilmis` olarak işlendi.
6. **İş veritabanına yazıyor.** Çatı sözleşmesine göre işler yalnız taslak döndürür. Bu iş ise ihlal kaydını yazar (defter kaydı değil; `Isler/` altında defter yazımı yine 0). Tekillik UNIQUE indeksle sağlanıyor; bildirim yazımı yine `IsCalistirici`'de. Gerekçesi dosya başlığında yazılı.
7. **Satır düğmeleri ikincil.** Şartname "Dilekçeyi yazdır"ı birincil düğme istiyordu. Kurucunun "ekran başına tek birincil düğme" kuralı gereği satır düğmeleri ikincil yapıldı; dilekçe görünümündeki "Dilekçeyi yazdır" ise birincil.
8. **Evrak kapısının etkisi:** 50.000 TL'lik tek veresiyeye karşılık alınan senet kasaya nakit tahsil edilirse artık 400 dönüyor. Bu, şartnamenin istediği davranış. Mevcut `EvrakTestleri.Nakit_tahsil_kasa_bakiyesini_artirir_ve_borcu_IKINCI_KEZ_kapatmaz` testinin fikstürü iki had-altı veresiyeye bölündü (konusu kasa ve çift sayma; assert'ler değişmedi). 400 davranışı yeni `Evrak_buyuk_veresiyeden_senedin_NAKIT_tahsili_400` testiyle kanıtlandı.
9. **`TEVSIK_BILDIRIM_IS_GUNU` tarihli okunuyor ama `YasalAnahtar.TablodanOkunan` listesine eklenmedi.** İş bu değeri günün tablosundan okuyor. Liste `Data/YasalParametre.cs` içinde; bu dosya YASAL-1'in ve t1 şeridi paralel kapanıyor. Dosyaya dokunulmadı; § 10'a yazıldı.
10. **Panel'deki "Bekleyen işler" kartı** kırmızı kutu varken "bugün temiz" diyebiliyor. Kart `BekleyenYok` mantığı Panel'in kendi kodunda; şartname burada yalnız 1 satır izni veriyor. § 10'a yazıldı.

## 4. Migration
`20260930142603_TevsikIhlalleri` — build yeşilken `dotnet ef migrations add` ile üretildi.
- `.cs` dosyası 3.855 bayt: `CreateTable tevsik_ihlalleri` ve iki indeks, başka tablo değişikliği yok. `Down` tabloyu siler.
- `.Designer.cs` dosyası 243.742 bayt.
- `dotnet ef database update` çıktısı: "Applying migration '20260930142603_TevsikIhlalleri'. Done."
- SQL: `__EFMigrationsHistory` son satırı `20260930142603_TevsikIhlalleri`. `SHOW INDEX` çıktısında UNIQUE 4 kolon ve durum indeksi görünüyor.

## 5. Mevzuat
Birincil kaynaklar şartname § 3.1'de URL ve alıntılarıyla doğrulanmıştı. Bu pakette ayrıca VUK mük. md.355'in 30.09.2026'da indirilen konsolide metni (`https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf`) okundu. İlgili ifade: "takip eden beş iş günü içerisinde kendiliğinden idareye bildirmesi halinde, ödemede bulunan adına". Bu ifade muafiyetin **ödeyene** ait olduğunu ve `yon` kolonunun gerekliliğini doğruluyor.

Koda sabit rakam yazılmadı:
- Had `TEVSIK_HADDI_TL` ve süre `TEVSIK_BILDIRIM_IS_GUNU` YASAL-1 tarihli tablosundan okunuyor.
- Tatiller `ResmiTatiller` + `TakvimAyarlari` ile geliyor. Bayram girilmemişse süre kısa hesaplanır (güvenli yön) ve ekranda not çıkar.
- Doğrulanamayan maddeler (T-1…T-5) şartnamedeki tutucu varsayımlarla kodlandı:
  - T-1: dilekçe TASLAK damgalı.
  - T-2: arife iş günü sayılıyor.
  - T-3: kaynak günün haddi kullanılıyor.
  - T-4: takas kural dışı.
  - T-5: ceza rakamı ekrana hiç basılmıyor.

## 6. DoD sözleşmesi
| Varlık | Düzenleme / kapatma | Yetki | Denetim | Ekran |
|---|---|---|---|---|
| `TevsikIhlali` | Tutar ve gün **değişmez** (defterden türetilir). Kapatma iki uçla yapılır: `/bildirildi` ve `/gecersiz`. DELETE yok (ADR-0004). | `[RolGerekli]` (liste/dilekçe/bildirildi: SAHIP·MUDUR·MUHASEBE; gecersiz: SAHIP) | `YazmaKaydet` (gerekçeli), `OkumaKaydet` (dilekçe), `ListeOkumaKaydet` (liste) | "Bildirildi olarak işaretle" (Cekmece), "Yanlış kayıt" (Cekmece ve OnayKipi), `DegismezIpucu` |
| Kaynaksız nakit listesi (tur 2) | Kayıt değil, denetim izinden türetilen salt okunur görünüm; düzeltme yolu kaynak kayıtta (açılış/düzeltme fişi) | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | `ListeOkumaKaydet("TevsikDogrulanamayan")` | Kapalı akordeon, açıklama cümlesi (salt okunur gerekçesi) |
| Tahsilat kararı | Kayıt değil, kapı kararı | mevcut tahsilat yetkisi | Kaynaksız geçişte `TEVSIK_TAKSIT_DOGRULANAMADI` izi | Kilit sebebi ekranda yazılı |

## 7. Testler
Yeni **54** test, hepsi yeşil. Sınıflar ve sayıları:
- `TevsikTaksitHttpTestleri` 11
- `TevsikTaksitSafTestleri` 5
- `TevsikTaksitDbTestleri` 5
- `TevsikIhlalSafTestleri` 3
- `TevsikIhlalDbTestleri` 12
- `TevsikIhlalHttpTestleri` 6
- `TevsikEkranTestleri` (bUnit) 12

Değişen mevcut testler:
- `IsSenaryoTestleri.Tevsik_kurtarma_isi_KAYITLI_ve_varsayilani_ACIK`: `:287`/`:292` tersine çevrildi.
- `IsCatisiSafTestleri.DI_de_uc_Faz_A_isi_ve_tevsik_kurtarma_kayitlidir`: dört iş.
- `EvrakTestleri` fikstürü (§ 3/8).

Filtreli koşumlar yeşil:
- Taksit: 20/20
- İhlal + iş çatısı: 39/39
- bUnit: 12/12
- Denetim ve sözleşme grupları: 280/281. Tek kırmızı KATMAN-2 testi (§ 9).

### Tam süit
| Koşum | Toplam | Geçti | Kaldı | Kırmızılar |
|---|---|---|---|---|
| 18:10 (trx `…18_10_01.trx`) | 8019 | 8017 | 1 | `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_…` |
| Son kodla geniş filtre (8027 test) | 8027 | 8025 | 1 | aynı KATMAN-2 testi |
| 18:28 (trx `…18_28_09.trx`, son kod) | 8027 | 7999 | 27 | Aşağıda açıklandı |

18:28 koşumundaki 27 kırmızı:
- **Geçici olanlar:**
  - `P31DestekPaketiTestleri` ×11 ve `YayinAcilis`: hata "Kuyumcu.Api.deps.json bulunamadı". Koşum sırasında başka bir şerit `bin` dizinini yeniden derledi.
  - `Dogrulayici.*` ×12 ve `SATIS3…Barkod`: aynı sebep.
  - Bu testler ayrı koşumda yeşil: P31 + SATIS3 barkod 21/21, `Dogrulayici` + `YayinAcilis` 1199/1199.
- **Başka şeritlere ait kalıcı kırmızılar:**
  - KATMAN-2 testi: tek başına koşulunca da kırmızı. Bu paketin dokunduğu hiçbir dosyayı kullanmıyor.
  - `KesifP1IsletmeTestleri.Isletme_ham_rol_kodu_ve_jargon_yok`: `Pages/Isletme.razor` 18:30'da KESİF-P1 şeridi tarafından değiştirildi.

**Bu paketin ürettiği yeni kırmızı: 0.**

## 8. Mutasyon tablosu
Yöntem: yedek kopya → tek eşleşmeli `perl` değişikliği → derleme → filtreli test → yedeği geri kopyala → `cmp`. Betik ve kayıtlar `/private/tmp/kuyumcu-serit/YASAL-2/mut.zsh` ve `mut-sonuc.txt` içinde. Son mutasyondan sonra temiz derleme 0 hata verdi.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | `KaynakTutar > had` → `>=` | `TevsikTaksitSafTestleri.K4_saf_…(30000,False)`, `TevsikTaksitHttpTestleri.K4_kaynak_tam_had_…` | AYNI |
| M2-1 | Kaynak tutar = fiş toplamı yerine tek hareket (`p.Kalan`) | `K1_cok_kalemli_fis_kalemleri_had_altinda_FIS_haddi_asiyor_nakit_taksit_400` | AYNI |
| M3-1 | FIFO yerine en yeni parti (`partiler.Reverse()`) | `FIFO_once_kaynaksiz_sonra_eskiden_yeniye_artan_FAZLA`, `K3_FIFO_…` | AYNI |
| M4-1 | `TahsilatGir` kapı çağrısı → `null` | `K1_62000_…`, `K1_cok_kalemli_…`, `K3_FIFO_…`, `K4_…`, `K6_acilis_…`, `K6_defterde_…` (6) | AYNI |
| M5-1 | Had kaynak gün yerine bugün | `K5_20_11_2024_…_400` | AYNI |
| M6-1 | Tarama kümülatifi şube kırılımlı (şube başına en büyük) | `K16_A_subesinde_20000_B_subesinde_15000_…_BULUR` | AYNI |
| M7-1 | Migration `unique: true` → `false` | `K10_ayni_cari_gun_tur_ikinci_satir_VERITABANINDA_reddedilir` | AYNI |
| M8-1 | `BildirimSonGunu` tatil atlamıyor | `K11_Cuma_23_10_2026_…` (saf), `K11_is_Cuma_…` (DB) | AYNI |
| M9-1 | Bildirim gövdesine cari adı eklendi | `K13_bildirim_basligi_ve_govdesinde_ad_TCKN_belge_no_GECMEZ` | AYNI |
| M10-1 | Evrak nakit tahsil kapısı → `null` | `Evrak_buyuk_veresiyeden_senedin_NAKIT_tahsili_400` | AYNI |
| M11-1 | `SURE_DOLDU` geçişi silindi | `K12_son_gun_gecince_ACIK_SURE_DOLDU_…` | AYNI |
| M12-1 | Dilekçe okuma izi silindi | `K14_dilekce_ucu_tam_kimlik_doner_ve_OKUMA_izi_yazar_…` | AYNI |
| M13-1 | Ekranda nakit kapalıyken NAKİT seçeneği kaldı | `TevsikEkranTestleri.Tahsilat_buyuk_satis_borcunda_NAKIT_secenegi_YOK_…` | AYNI |
| M14-1 | `/gecersiz` MUDUR'a açıldı | `Gecersiz_yalniz_SAHIP_MUDUR_403_…` | AYNI |

## 9. denetim.sh
- Başlangıçta (paket öncesi ağaç) tek ✗ vardı: `DUGME-2: DUGME2_KURAL_EKSIK 2`. Bu başka bir şeridin; sonradan kapandı.
- İlk koşumda 4 ✗ çıktı ve hepsi bu pakette kapatıldı:

| ✗ | Düzeltme |
|---|---|
| Pages'te `@onclick`'siz düğme 72 > 71 | `@onclick` ve `<button` aynı satıra alındı |
| Sınırsız `ToListAsync` 147 > 145 | Kimlik listesi kadar `Take` eklendi (veri kaybı yok) |
| P-11 veri düşüren yol 48 > 47 | `catch` içindeki `veri = null` kaldırıldı |
| P-15 `/uyum` sorgu süzgecini okumuyor | `[SupplyParameterFromQuery] sekme` eklendi |

- Son kodla: **DENETİM TEMİZ** (`denetim3.log`, çıkış kodu 0).

## 10. Canlı doğrulama (API 5162, vekil 5163, `--no-build`)
Test kiracısı: `PAKETYASAL2-TEST-canli` (id `01M3SDXBC6981CCPEN4H2DGCGS`). Kuruluş: rastgele sistem anahtarı (yalnız bu süreç için) → davet → kayıt. Sır değerleri hiçbir çıktıya yazılmadı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| 62.000 veresiye → önizleme | `kuralDevrede`, alınabilir 0 | 200 · `{kuralDevrede:True, nakitAlinabilir:0, buyukIslemTutari:62000, sebep:"30.000,00 TL'yi aşan 30.09.2026 tarihli satıştan kalan borç"}` |
| K-1: 3 × 20.000 NAKİT | 3 × 400, yeni TAHSILAT 0 | 400 ×3 ("…en çok 0,00 TL nakit…") · SQL `COUNT(TAHSILAT)=0` |
| K-2: 20.000 KART | 200 | 200 · SQL `TAHSILAT KART -20000.00` |
| K-3/K-8: 10.000 + 50.000 veresiye | önizleme 10.000 · 15.000 → 400 · 10.000 → 200 | `True 10000.0 50000.0` · 400 "…en çok 10.000,00 TL nakit…" · 200 · SQL `VERESIYE 10000 / VERESIYE 50000 / TAHSILAT NAKIT -10000` |
| K-6: düzeltme +40.000 → 20.000 NAKİT | 200 + iz | 200 · SQL `TEVSIK_TAKSIT_DOGRULANAMADI · "nakit 20000.00 TL; kaynağı bilinmeyen bakiyeden 20000.00 TL"` |
| TB-16 fikstürü (`Tevsik__Etkin=false` ile yeniden başlatıldı): taksit 20.000 nakit, cari2 +25.000 nakit (günlük 35.000), tedarikçiden 36.000 NAKİT alış | 200 | 200 ×3 (kural kapalı → kapı geçirdi) |
| İş: `POST /isler/TEVSIK_KURTARMA/calistir` ×2 | 1. koşum 4 bildirim, 2. koşum 0 | `BASARILI · uretilen 4 · incelenen 10`; ikinci koşum `BASARILI 0 0` |
| SQL `tevsik_ihlalleri` | 4 satır | `GUNLUK_KUMULATIF ODEME 36000 son 2026-10-07 ACIK` · `GUNLUK_KUMULATIF TAHSILAT 35000 ACIK` · `TAKSIT TAHSILAT 20000 ACIK` · `TAKSIT TAHSILAT 25000 ACIK` |
| SQL `bildirimler` | KRİTİK, `SAHIP,MUHASEBE` | 4 × KRITIK. ODEME: "…bildirim için son gün 07.10"; TAHSILAT: "…mali müşavirinize danışın" |
| K-14: TEZGÂHTAR liste / dilekçe | 403 / 403 | 403 / 403 |
| SAHİP liste / dilekçe | 200 · okuma izi | 200 (4 kayıt, sınır 200, `bayramEksik` True) · 200 (damga TASLAK, yön ODEME, karşı taraf adı, 1 hareket) · SQL `OKUMA TevsikIhlali "liste: 4 kayıt"` ve `"nakit sınırı bildirim dilekçesi"` |
| Bildirildi: gerekçe kısa / geçerli / tekrar | 400 / 200 / 409 | 400 "Gerekçe en az 20 karakter olmalı (4/20)." / 200 `BILDIRILDI gec=false` / 409 |
| Yanlış kayıt (SAHİP) | 200 | 200 `GECERSIZ` · SQL kapatma alanları dolu, `bildirim_evrak_no E-2026/77`, denetim `GECERSIZ · …` / `BILDIRILDI · …` |

Temizlik:
- API ve vekil süreçleri `kill` ile durduruldu. `lsof :5162` ve `:5163` boş.
- `zsh kiraci-temizle.sh 01M3SDXBC6981CCPEN4H2DGCGS --uygula` → 371 satır ve tenant silindi. `tenants WHERE id` = 0, `tevsik_ihlalleri` = 0.
- Davet satırı (`kiraci_davetleri`, kiracısız) 1 → 0.
- `tenants`: başlangıç **15**, son **15**. QA-TEST-* kiracılarına dokunulmadı.
- Geçici sır dosyaları `printf ''` ile boşaltıldı.

### 10.1 Gerçek başsız Chrome ölçümü
Araç: `sikisma-olc.js __olcTam` ve `cdp.mjs`. CDP portları 9907–9909, vekil 5163. Betik: `/private/tmp/kuyumcu-serit/YASAL-2/olc.mjs`. Çıktılar: `tarayici3/olcum.jsonl` ve `tarayici3/foto/*.png`.

| Durum | 1280 PRO açık | 1280 PRO koyu | 375 PRO açık | 375 PRO koyu | 1280 BASİT açık | 1280 BASİT koyu | 375 BASİT açık | 375 BASİT koyu |
|---|---|---|---|---|---|---|---|---|
| Panel `#nsp` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Uyum → Nakit sınırı `#nsa` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cari → Tahsilat `#th-taksit` | K9 6* | K9 6* | K9 6*, K3 kırık 1* | aynı* | K9 6* | K9 6* | K9 6*, K3 1* | aynı* |
| Dilekçe `#nsa-dilekce` | 0 (açık) | — | — | 0 (koyu) | | | | |

"0" şu sayaçların hepsinin sıfır olduğu anlamına gelir: yatay kaydırma, taşan yazı (K3), kırılan etiket, üst üste binen (K9), görünen `[hidden]` (K1), `TRY` kalıbı (K5).

\* Tahsilat penceresindeki K9=6 ve K3=1 bu paketten gelmiyor. Aynı pencere taksit bloğu **olmayan** bir caride açılıp ölçüldüğünde de sonuç aynı (`tarayici-taban/olcum.jsonl`: 1280 K9 6 · 375 K9 6, K3 1). Örnekler şunlar:
- K9: modalın altında kalan sayfa yazıları (ör. "Cariler" başlığı ⟂ modal başlığı).
- K3: 375 px'te modal başlığı "Tahsilat — Ayşe Taksit" iki satıra kırılıyor. Bu `ModalPencere` ile ilgili ve önceden var.

Taksit bloğu ekstra ölçüm bulgusu üretmiyor.

Read ile incelenen görüntüler:
- `tarayici/foto/cari-tahsilat__PRO_aydinlik_1280.png`: Nakit yok, Kart seçili, cümle ve iki düğme görünüyor.
- `cari-tahsilat__BASIT_karanlik_375.png`
- `tarayici2/foto/uyum-nakit__BASIT_karanlik_375.png`
- `tarayici3/foto/uyum-nakit__PRO_karanlik_1280.png`: iki açık kayıt ve kapalı "Kapanmış kayıtlar 2" akordeonu.
- `tarayici3/foto/panel__PRO_aydinlik_1280.png`: kırmızı kutu, "1 nakit sınırı aşımı — bildirim için son gün 07.10 (6 iş günü)" ve "…mali müşavirinize danışın".
- `panel__BASIT_karanlik_375.png`
- `dilekce__aydinlik_1280.png`: TASLAK damgası, muhatap, konu, gövde, taraflar, işlem tablosu, dayanak ve imza.

İlk turda görülen iki tasarım kusuru düzeltildi ve yeniden ölçüldü:
1. Panel satırı Panel'in kapsamlı (scoped) stillerine bağlı olduğu için madde işaretli, dağınık çiziliyordu. Mevcut `.uyari-kutu.uyari-kirmizi` kutusuna alındı.
2. Katlanır başlıkta sayı iki kez yazılıyordu. Tekrar kaldırıldı. `DegismezIpucu`'ya boşluk verildi.

## 11. Değişen dosyalar (bu paket)
Api:
- `Data/TevsikIhlali.cs` (Y)
- `Data/KuyumcuDbContext.cs`
- `Data/Migrations/20260930142603_TevsikIhlalleri.cs` + `.Designer.cs` (Y)
- `KuyumcuDbContextModelSnapshot.cs` (ef üretti)
- `Modules/Tevsik/TevsikKurali.cs`, `TevsikKapisi.cs`, `TevsikController.cs`
- `Modules/Tevsik/TevsikIhlalModulu.cs` (Y)
- `Modules/Vade/VadeKapisi.cs`
- `Modules/Cari/CariModulu.cs`
- `Modules/Evrak/EvrakModulu.cs`
- `Modules/Isler/Isler/TevsikKurtarmaIsi.cs` (Y)
- `Modules/ModulKayit.cs`
- `Modules/Meta/DuzeltmePolitikasi.cs`
- `appsettings.json`

Contracts: `Sayfalar/NakitSiniriSozlesmeleri.cs` (Y).

Core: `I18n/Sozlukler.TevsikTaksit.cs` (Y).

Web:
- `Pages/Cariler.razor`, `Pages/Uyum.razor`, `Pages/Panel.razor`
- `Bilesenler/NakitSiniriAsimlari.razor` (Y), `Bilesenler/NakitSiniriPanelSatiri.razor` (Y)
- `Servisler/NakitSiniriVeri.cs` (Y)
- `wwwroot/css/nakit-siniri.css` (Y)
- `wwwroot/index.html`

Testler:
- `TevsikTaksitTestleri.cs` (Y), `TevsikIhlalTestleri.cs` (Y), `TevsikEkranTestleri.cs` (Y)
- `IsSenaryoTestleri.cs`, `IsCatisiSafTestleri.cs`, `EvrakTestleri.cs`

Denetim: `dotnet/denetim.sh`, `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json`.

Not: `git status --porcelain` bu ağaçta dizinlerin çoğunu izlenmeyen (`??`) olarak gösterdiği için liste elle derlendi.

Tek dokunuş listesindeki sayfalar: `Cariler.razor` (yalnız tahsilat penceresi), `Uyum.razor` (yeni sekme), `Panel.razor` (1 satır).

## 12. Koşul karşılama
| # | Koşul | Durum | Kanıt |
|---|---|---|---|
| K-1 | 62.000 tek fiş, 3 × 20.000 nakit → 400, satır 0 | KARŞILANDI | HTTP ×2 (tek hareket + gerçek çok kalemli fiş), canlı + SQL |
| K-2 | Kart → 200 | KARŞILANDI | HTTP, canlı |
| K-3 | FIFO 15.000 → 400 "en çok 10.000", 10.000 → 200 | KARŞILANDI | HTTP, canlı |
| K-4 | 30.000,00 serbest / 30.000,01 → 400 | KARŞILANDI | saf teori + HTTP |
| K-5 | Kaynak günün haddi (2024-11-20 → 400, 2024-12-05 → 200) | KARŞILANDI | DB ×2 |
| K-6 | Vadesiz → 200 + iz 1 satır, kişisel veri yok | KARŞILANDI | HTTP ×2, canlı + SQL |
| K-7 | Tediye 2 × 10.000 → 400 | KISMEN | Kapı DB testiyle kanıtlı; tediye ucu kodda yok (§ 3/1) |
| K-8 | Önizleme `nakitAlinabilir = 10000` | KARŞILANDI | HTTP, canlı |
| K-9 | Kural kapalı, 20.000 + 15.000 → 1 satır `GUNLUK_KUMULATIF` 35.000 | KARŞILANDI | DB, canlı |
| K-9b | Alış → ODEME + son gün; satış → TAHSILAT + müşavir metni | KARŞILANDI | DB + bUnit (Panel), canlı |
| K-10 | İş iki kez → 1 satır, 1 bildirim | KARŞILANDI | DB ×2 (iş + UNIQUE), canlı |
| K-11 | 23.10.2026 → 02.11.2026 | KARŞILANDI | saf + DB (iş) |
| K-12 | Son gün geçince SURE_DOLDU, satır kalır | KARŞILANDI | DB |
| K-13 | Bildirimde ad/TCKN/belge no yok | KARŞILANDI | DB (regex) |
| K-14 | TEZGÂHTAR 403, dilekçe okuma izi | KARŞILANDI | HTTP, canlı + SQL |
| K-15 | Ekranlar 8 kombinasyonda ölçülü, taşma 0 | KARŞILANDI | § 10.1 (24 + 2 ölçüm, görüntü) |
| K-16 | Şube A/B ihlali bulunur | KARŞILANDI | DB, M6 |
| K-17 | Test 0 kırmızı (bu paketten) · denetim TEMİZ · kiracı silindi | KARŞILANDI | § 7, § 9, § 10 |
| Mutasyon 1–9 (şartname) + 10–14 (ek) | Hepsi kırmızı + cmp AYNI | KARŞILANDI | § 8 |

## 13. Açık kalanlar (sahibiyle)
1. **SATIŞ şeridi:** Fiş onayına ve yazdırılan fişe "Kalan borç sonradan yalnız kartla ya da havaleyle alınır" satırı eklenmeli (`Islemler.razor`, fiş şablonu). Veri hazır: `GET /tevsik/gunluk?cariId=&fisToplamTl=`.
2. **Tediye ucu:** Tedarikçiye sonradan nakit ödeme ucu yazıldığında `TevsikKapisi.TaksitDogrula(…, alacakYonu:false)` çağrılmalı (K-7).
3. **YASAL-1 / t1:** `YasalAnahtar.TablodanOkunan` listesine `TevsikBildirimIsGunu` eklenmeli. Program artık bu değeri tarihli okuyor.
4. **Panel sahibi:** "Bekleyen işler" kartının `BekleyenYok` hesabı nakit sınırı aşımlarını bilmiyor; kırmızı kutu varken kart "bugün temiz" diyebilir. Ayrıca `PanelTuvalSozlugu.Kaynak` içinde `TEVSIK_KURTARMA` eşlemesi yok; bildirim satırının kaynağı "Bildirim" olarak düşüyor.
5. **Evrak sınırı:** Senedin nakit tahsilinde FIFO carinin **bugünkü** açık partileriyle yapılıyor. Senedin girdiği gün hangi partiyi kapattığı tutulmuyor. Borç tümüyle kapandıysa kapı geçirir; iş bu durumu da yakalamaz.
6. ~~Doğrulanamayan geçişler Uyum listesinde görünmüyor~~ → **tur 2'de kapandı** (§ 1A: salt okunur kapalı akordeon + `GET /tevsik/ihlaller/dogrulanamayanlar`).
7. **Başkasına ait tam süit kırmızıları (tur 2 son koşum: yalnız `SATIS2EkranTestleri.PRO_ust_satir_…` — yük altında kararsız, tek başına yeşil → SATIŞ-2):** tur 1 listesi:
   - `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_…` → KATMAN-2 şeridi (deterministik kırmızı).
   - `KesifP1IsletmeTestleri.Isletme_ham_rol_kodu_ve_jargon_yok` → KESİF-P1 (`Isletme.razor` 18:30'da değişti).
   - Derleme uyarısı `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65)` → SUBE-KAPSAM doğrulayıcısı.

## 14. Kurucu / mali müşavir
- **K-A:** Devir/açılış bakiyesinden nakit tahsilat şimdi geçiyor ve iz bırakıyor. Engellenmesi istenirse yapılandırma değil kod kararı gerekir.
- **K-B:** Taksitte had, işlem gününün haddi olarak uygulanıyor (T-3). Teyit gerekir.
- **K-C:** Bildirimin biçimi ve kanalı birincil kaynakta bulunamadı (T-1). Dilekçe "TASLAK — mali müşavirinizle kontrol edin" damgalı serbest metin. Hangi vergi dairesine, hangi ekle ve hangi kanaldan verileceği teyit edilmeli.
- **K-D:** Md.355'e göre muafiyet yalnız ödeyene ait (bu pakette metinden okundu). Tahsilat yönündeki aşımda kuyumcunun başka bir yolu (ör. pişmanlık) olup olmadığı sorulmalı. Ürün bu yönde yalnız "mali müşavirinize danışın" yazar.
- **T-2:** Arife yarım günü iş günü sayılıyor (kısa hesap). Teyit gerekir.
- **T-4 (S-1):** Takas kaynaklı taksit kural dışı tutuluyor. Teyit gerekir.
