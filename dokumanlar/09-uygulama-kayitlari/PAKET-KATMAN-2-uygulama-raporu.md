# PAKET KATMAN-2 — Kenar menüsü "Grubun açık/kapalı durumu kaydedilemedi, geri alındı" kalıcı kırmızı bandı

**Tarih:** 2026-09-30 (03:58–05:10; düzeltme turu 2: 05:25–05:50) · **Şerit:** KATMAN-2 (t5: API 5166, vekil 5167, `kuyumcu_test_t5`, CDP 9500–9519) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Commit:** YOK.
Derleme/test yalıtık kopyada: `dotnet build <gerçek sln> --artifacts-path /private/tmp/kuyumcu-serit/KATMAN-2/j/dotnet/art` (proje ağacının `bin/`'ine yazılmadı; `j/dotnet` = kaynak dizinlerine sembolik bağ, testler `Kuyumcu.sln`'i oradan bulur).
Not: `j/dotnet/Kuyumcu.sln` (sembolik bağ yolu) üzerinden derleme 134 Razor ad alanı hatası verdi (bileşen ad alanı gerçek yoldan hesaplanıyor); gerçek `.sln` yolu + `--artifacts-path` ile 0 uyarı 0 hata. Kod kusuru değil, yöntem notu.

## 0. DÜZELTME TURU 2 (30.09 05:25–05:50) — hasım doğrulayıcı bulguları
| Bulgu | Durum | Kanıt |
|---|---|---|
| **[KRİTİK]** Tek yazıcı alanı senkron tamamlanan göreve takılıyor (`return katlamaYazici ??= KatlamayiYazAsync();` + finally `null`) → fırtına/oturum hatasından sonra katlama bir daha HİÇ yazılmıyordu | **KAPANDI** | `GrupKatlamaDegistirAsync` artık alanı yalnız tamamlanmamış görevle doldurur: `if (katlamaYazici is { IsCompleted: false } suren) return suren; var yazici = KatlamayiYazAsync(); if (!yazici.IsCompleted) katlamaYazici = yazici; return yazici;`. Doğrulayıcının iki kırmızı testi (`Dogrulayici.KATMAN2SinirTestleri.Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_sunucuya_yazilir`, `…Kalici_hatadan_ve_bant_kapatildiktan_sonra_yeni_tiklama_yeniden_yazilir`) **yeşil**; yeni `Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` + `Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_yazilir`. Gerçek Chrome (doğrulayıcının firtina senaryosu birebir): pencere sonrası tık **1 PATCH** (önce 0), ikinci tık 1 PATCH, bant 0, yenileme sonrası **kalıcı: true** (SAHIP PRO açık + TEZGAHTAR BASİT koyu). |
| **[DÜŞÜK]** Uçuştaki PATCH ile "Grupları sıfırla" DELETE yarışı | **KAPANDI** | `KatlamayiSifirlaAsync` önce süren yazıcıyı bekler (yazıcı `acikGruplar == null` görüp yeni PATCH açmaz), yazıcı hata verip geri aldıysa geri dönüş hedefi o olur, sonra DELETE. bUnit `Grup_sifirla_ucustaki_PATCH_bitmeden_DELETE_atmaz` (sıra `[PATCH, DELETE]`). Canlı: PATCH CDP ile 1500 ms bekletildi, sıfırla bu sırada tıklandı → istek sırası `PATCH, DELETE`, DELETE PATCH bırakıldıktan **447 ms sonra**; SQL: SAHIP için `menu-acik-gruplar` satırı **yok**; yenileme sonrası yalnız `gunluk` açık. |
| **[DÜŞÜK]** Karşılama ekranı (Panel) katlama hatasını ham "Internal server error" ile basıyor | **AÇIK — sahibi ORTAK-1 (Panel.razor)** | Dosya bu şeridin alanında değil (EŞZAMANLI ŞERİTLER: Panel = ORTAK-1). Bkz. §10 madde 1. |

## 1. Kök neden — ÖLÇÜLDÜ (tahmin yok)
Canlı API 5166 (kuyumcu_net), kendi kiracım `PAKETKATMAN-2-TEST Menu` (davet satırı + `/auth/kayit` 201; MUDUR/TEZGAHTAR/MUHASEBE kullanıcıları `/kullanicilar` 201×3). Betik `/private/tmp/kuyumcu-serit/KATMAN-2/t1/kok.py`, çıktı `kok.log` (önce) / `kok-sonra.log` (sonra).

| Ölçüm | DÜZELTMEDEN ÖNCE | SONRA |
|---|---|---|
| (a) 4 rol (SAHIP/MUDUR/TEZGAHTAR/MUHASEBE) × sıralı aç-kapa PATCH + GET | 200/200/200 ×4 — **yetki sorunu YOK** | 200/200/200 ×4 |
| (b) aynı kullanıcı, aynı anahtar, satır YOKKEN 2 eşanlı PATCH (10 tur) | **10/10 tur başarısız**: 409 "Bu kayıt zaten var" (MySQL 1062) ya da **500** (MySQL 1213 "Deadlock found") | **0/10** |
| (b2) aynı kullanıcı, FARKLI anahtar, satır yokken eşanlı | 0/10 | 0/10 |
| (c) satır VARKEN 10 eşanlı PATCH | 10×200 | 10×200 |
| (c2) satır YOKKEN 10 eşanlı PATCH | **5×409**, 5×200 | **10×200** |
| (d) 300/dk genel sınır: tek kullanıcı art arda GET | 297 başarılı sonra 429, `Retry-After: 10`; 11 sn sonra PATCH yine 429 (kayan pencere 60 sn) | — (sınır gevşetilmedi) |

**Sonuç:** Kalıcı bandın kaynağı **eşanlı yazma yarışı**: `TercihServisi.Yama` "önce SELECT, satır yoksa INSERT" yapıyordu; satır yokken aynı anahtara iki eşanlı PATCH ikisi de INSERT'e gidip benzersiz indekse `(tenant_id, kullanici_id, anahtar)` çarpıyor → 409/500. İstemci (`MenuTercihServisi`) her tıklamada AYRI ve EŞANLI PATCH açtığı için hızlı çift tıklama ya da aynı kullanıcının iki sekmesi (25.09 turu: 5 paralel tarayıcı, aynı MUDUR) ilk katlamada bu yarışa giriyordu; ekran da "kaydedilemedi, geri alındı" bandını çiziyordu. Sunucu günlüğündeki "DB hatası yoktu" gözlemi: EF bu hatayı `DbUpdateException` olarak günlüğe yazıyor, 25.09'da aranmamıştı (bugün 5166 günlüğünde 1062 ve 1213 kayıtları görüldü). Oran sınırı (d) ikincil etken: 25.09'da 100/dk'ydı; bugün 300/dk'da tek kullanıcı normal kullanımda buna değmez, ama değerse ApiIstemci'nin tek seferlik beklemesi (10 sn) yetmiyor ve bant çıkıyordu → istemcide katlama için sessiz yeniden deneme eklendi.

## 2. Yapılanlar
**Taşkın (backend) — `Kuyumcu.Api/Modules/Tercih/TercihModulu.cs`:** `Yama` yazma gövdesi `YamayiUygula`'ya alındı; yarışı kaybeden yazma (`DbUpdateException` zinciri: 1062 benzersizlik, 1213 kilitlenme — EF'in "transient failure" sarmalı dahil) `ChangeTracker.Clear()` + baştan okuma ile **en çok `AzamiDeneme = 4`** kez yeniden denenir (ikinci denemede satırı görür → UPDATE). Sınır aşılırsa hata eskisi gibi 409/500 döner (md.1, gizleme yok). Doğrulama hataları (beyaz liste/uzunluk → 400) döngüye girmez. Denetim kaydı her denemede yeniden kurulur (`DenetimServisi.YazmaKaydet` çağrılır, yeniden yazılmaz), aynı SaveChanges ile yazılır. PATCH sözleşmesi (gönderilmeyen = koru, null = sil) değişmedi. Yetki değişmedi (kimlik jetondan; `[HerkeseAcik]` gerekçeli, 4 rol ölçüldü). Migration YOK.

**Ertuğ (frontend) — `Kuyumcu.Web/Servisler/MenuTercihServisi.cs`:**
- Katlama için **tek yazıcı + birleştirme**: aynı anda en çok BİR katlama PATCH'i uçuşta; ilk tıklama beklemeden yazılır; yazma sürerken gelen tıklamalar yalnız sürüm artırır; yazma dönünce tıklamalar **`KatlamaSukunetMs = 350` ms** durulana dek beklenir (debounce) ve **yalnız SON durum** yazılır.
- Başarısızlıkta yerel durum **sunucunun doğruladığı son duruma** (`sunucudakiAcikGruplar`) döner (önceden: bir önceki tıklamanın durumu — eşanlı yazmalarda yanlış hedefti).
- `OranSinirHatasi` (ApiIstemci'nin kendi geri çekilmesinden sonra süren 429) kalıcı hata sayılmaz: `KatlamaOranYenidenDeneme = 3` kez sunucunun `Retry-After` süresi beklenip sessizce yeniden yazılır; ancak sonra bant. `IstekFirtinasiHatasi` (kod hatası) beklenmez.
- (Tur 2) Tek yazıcı alanı yalnız TAMAMLANMAMIŞ görevi tutar; "Grupları sıfırla" uçuştaki PATCH'i bekler (§0).
- `YazAsync`/`SilAsync` artık `bool` döner; `HatayiKapat()` eklendi. Sıra/favori/panel yolları davranış olarak değişmedi (sunucu düzeltmesi onların ilk-yazma yarışını da kapatır).

**`Kuyumcu.Web/Layout/KenarCubugu.razor`:** tercih bandına (id `kenar-sira-hatasi` AYNI) **Kapat** düğmesi `#kenar-sira-hatasi-kapat` (metin `Dil.T.Ortak.Kapat`, TR+EN mevcut sözlük; yeni sözlük anahtarı yok, sabit renk/satır içi stil yok). Bant mekanizması (md.1) korundu; yalnız haksız hata kalktı.

**Sapmalar ve gerekçe:**
1. "Debounce 300–500 ms": saf gecikmeli debounce yerine **ön-kenar + birleştirme + durulma** uygulandı. Gerekçe: tek tıklama anında kalıcı olur (sayfadan hemen çıkan kullanıcı tercihini kaybetmez) ve mevcut 30+ ekran testi (tıklama → PATCH bekleyen) gerçek saate bağlanmaz; art arda tıklamalarda istek sayısı yine en çok 2'dir ve yazılan hep son durumdur (bUnit + canlı kanıt).
2. "Bant TEK satır": kenar çubuğu 216 px genişlikte; metin 4 satıra kırılır. "Tek bant" olarak uygulandı (üst üste iki bant oluşmaz, `Assert.Single`), metin kısaltılmadı (MenuSozlugu bu şeridin dosya alanında değil).
3. Kök neden yetki değil, oran sınırı birincil değil → `OranSiniri.cs` DOKUNULMADI (giriş sınırı dahil hiçbir sınır gevşetilmedi).

## 3. DoD sözleşmesi
| Varlık | Uç | Durum |
|---|---|---|
| `kullanici_tercihleri` (arayüz tercihi, finansal değil) | GET/PATCH `/tercihler/benim`, DELETE `/tercihler/benim/{anahtar}` | Yeni varlık yok; mevcut uçlar. PATCH koru/sil sözleşmesi + testleri (`PATCH_gonderilmeyen_anahtari_KORUR`, `Katlama_tercihi_SIFIRLANABILIR_null_ve_DELETE`) yeşil. Denetim kaydı her PATCH'te (canlı: kiracımda `KullaniciTercihi` 555 denetim satırı). Ekranda düzenleme = grup başlığı; sıfırlama = "Grupları sıfırla". |
| Tercih hata bandı | — | Yeni düğme `#kenar-sira-hatasi-kapat` gerçek eylem yapar (bandı kaldırır; bUnit + canlı `kapatSonraBant 0`). |

## 4. Testler
Yeni dosya `Kuyumcu.Tests/KATMAN2MenuTercihTestleri.cs` — **16 yeni test** (HTTP 7: 4 rol teorisi + 3; bUnit 9; tur 2'de +3: `Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_yazilir`, `Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir`, `Grup_sifirla_ucustaki_PATCH_bitmeden_DELETE_atmaz`):
- HTTP: `Her_rol_KENDI_katlamasini_ac_kapa_yazar_200` (SAHIP/MUDUR/TEZGAHTAR/MUHASEBE, SQL: satır o kullanıcıya yazıldı), `Satir_YOKKEN_ayni_anahtara_ESANLI_on_PATCH_hepsi_200_ve_TEK_satir` (3 tur × 10 eşanlı), `Esanli_iki_PATCH_FARKLI_anahtar_ikisi_de_KALICI`, `Yeniden_deneme_DOGRULAMA_hatasini_yutmaz_bilinmeyen_anahtar_yine_400`.
- bUnit: `On_hizli_tiklama_ESANLI_PATCH_acmaz_SON_durum_yazilir_bant_YOK`, `Yazma_surerken_gelen_tiklamalar_DURULMA_beklemesinden_sonra_yazilir`, `Tek_tiklama_TEK_PATCH_bekleme_yok`, `Kalici_hatada_SUNUCUNUN_bildigi_duruma_doner_bant_TEK_ve_KAPATILABILIR`, `Gecici_429_SESSIZCE_yeniden_denenir_bant_YOK_durum_KORUNUR`, `Suren_429_denemeler_BITINCE_bant_cizilir_sessiz_kalinmaz`.

| Koşum (KUYUMCU_TEST_DB=kuyumcu_test_t5) | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `KATMAN2*` (yalnız paket, tur 1) | 13 | 13 | 0 |
| **Tur 2** `FullyQualifiedName~KATMAN2` (16 paket + 3 doğrulayıcı `Dogrulayici.KATMAN2SinirTestleri`) | 19 | 19 | 0 |
| **Tur 2** menü/kabuk ilgili 12 sınıf (MenuKatlama, MenuMimarisi, S1gMenuTuval, S1jMenuArama, BasitModMenu, TK2KabukTuval, KabukAramaEkran, P37bKarsilama, S31PanelSade, P37Revizyon, S1dEkranlar, DuzeltmePolitikasi) | 312 | 312 | 0 |
| Paket + MenuKatlama + TercihKatlamaHttp (kararlılık 3×) | 47→49 | hepsi | 0 |
| Paket + P37* + S1gMenuTuval + MenuMimarisi + TK2 + ApiIstemci + Istek1 (6×) | 219 | 218 | 1 = `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` (bkz. açık kalanlar; bu paketin değil) |
| Tercih/KenarCubugu/MenuTercihServisi'ne dokunan 27 test sınıfının tamamı (4×) | 541 | 540/540/541/541 | 1/1/0/0 — kararsız: `TK3AyarlarTuvalTestleri.TK3_02_…` (bUnit UnknownEventHandlerId, 3 yalıtık koşumda 17/17 yeşil) ve `KabukAramaEkranTestleri.Secilen_sube_…` (sonraki 2 koşumda yeşil); ikisi de katlama yazmasına dokunmuyor |

Tam süit: 29.09 23:00 hız kuralı gereği **uygulayıcı koşmadı** (yalnız hasım doğrulayıcı).
Önce/sonra: paket öncesi bu dosyadaki test sayısı 0 → 13 (tur 1) → 16 (tur 2).

## 5. Mutasyon kanıtı (`/private/tmp/kuyumcu-serit/KATMAN-2/mut.sh`: yedek → perl tek eşleşme → derle → filtreli koşum → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-K2-1 | Sunucu yeniden denemesi kapalı (`when (false && …)`) | `KATMAN2TercihHttpTestleri.Satir_YOKKEN_ayni_anahtara_ESANLI_on_PATCH_hepsi_200_ve_TEK_satir` | AYNI |
| M-K2-2 | Tek yazıcı kaldırıldı (`return KatlamayiYazAsync();`) | `On_hizli_tiklama_…`, `Yazma_surerken_…`, `Kalici_hatada_…` | AYNI |
| M-K2-3 | Durulma beklemesi kaldırıldı | `Yazma_surerken_gelen_tiklamalar_DURULMA_beklemesinden_sonra_yazilir` | AYNI |
| M-K2-4 | Hatada geri alma yok (`acikGruplar = acikGruplar;`) | `Kalici_hatada_…`, `Suren_429_…`, `MenuKatlamaTestleri.Yazma_basarisizsa_katlama_GERI_ALINIR_ve_bant_cizilir` | AYNI |
| M-K2-5 | Kapat düğmesinin `@onclick`'i silindi | `Kalici_hatada_…_KAPATILABILIR` | AYNI |
| M-K2-6 | Birikmiş son durum yazılmıyor (`return;`) | `On_hizli_tiklama_…`, `Yazma_surerken_…` | AYNI |
| M-K2-7 | Hatadan sonra döngü sürüyor (`if (!tamam) return;` silindi) | `Kalici_hatada_…` (ilk denemede kırmızısız kaldı → TEST AÇIĞI; test sunucu durumu dolu olacak şekilde güçlendirildi, tekrar koşumda kırmızı) | AYNI |
| M-K2-8 | 429 sessiz yeniden deneme kapalı | `Gecici_429_SESSIZCE_…`, `Suren_429_…` | AYNI |
| **M-K2-9** (tur 2) | Tur 2 düzeltmesi geri alındı (`return katlamaYazici ??= KatlamayiYazAsync();`) | `KATMAN2MenuTercihEkranTestleri.Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_yazilir`, `…Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir`, `Dogrulayici.KATMAN2SinirTestleri.Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_sunucuya_yazilir`, `Dogrulayici.KATMAN2SinirTestleri.Kalici_hatadan_ve_bant_kapatildiktan_sonra_yeni_tiklama_yeniden_yazilir` (51 geçti / 4 kaldı) | AYNI |
| **M-K2-10** (tur 2) | Sıfırlamada uçuştaki yazıcı beklenmiyor (`if (false && katlamaYazici is …)`) | `KATMAN2MenuTercihEkranTestleri.Grup_sifirla_ucustaki_PATCH_bitmeden_DELETE_atmaz` (54/1) | AYNI |

Son mutasyondan sonra üç kaynakta mutasyon kalıntısı 0 (grep); temiz çözüm derlemesi **0 Uyarı 0 Hata** (`build-temiz.log`). Tur 2: M-K2-9/10 `cmp` AYNI, ardından `dotnet build Kuyumcu.sln` **0 Uyarı 0 Hata** (`build2.log`).

## 6. denetim.sh
**Tur 2 (05:37):** `bash dotnet/denetim.sh` → DENETİM BAŞARISIZ, **✗ 1**: `P-13: GEREKCESIZ_KILITLI_ALAN 1 (Cariler.razor:1)` — Cariler.razor bu paketin dosya alanında değil, dokunulmadı (sahibi: Cariler.razor'u açan şerit / orkestratör). Önceki `P-23 ODEME_KALEMSIZ_YOL` bu koşumda ✓. Bu paketin sayaçları ✓: `P-37: MENU_SESSIZ_YUTMA 0`, `P-37: FAVORI_KULLANICI_DISI 0`, `P-37b: KARSILAMA_TERCIH_KULLANICI_DISI 0`, `IgnoreQueryFilters 0`, `ExecuteUpdate/Delete 5 (beyaz liste 5)`, `Hareketler.Add 1`, `Pages: ModalPencere 47 (tavan 55)`.

**Tur 1:** `bash dotnet/denetim.sh` → **DENETİM BAŞARISIZ, ✗ 1**: `P-23: ODEME_KALEMSIZ_YOL 6` — canlı kuyumcu_net'te ödeme kalemi olmayan 6 hareket; kiracıları: `PAKETZO-BELGE-TEST Kuyumculuk` (4, 30.09 01:40) ve `TEST-KESIF Kuyumculuk` (2, 29.09). Bu paketin değil (satış/hareket yazmadım). Bu paketin sayaçları ✓: `P-37: FAVORI_KULLANICI_DISI 0`, `P-37b: KARSILAMA_TERCIH_KULLANICI_DISI 0`, `IgnoreQueryFilters 0`, `ExecuteUpdate/Delete 5 (beyaz liste 5)` (testte ExecuteDelete kullanılmadı), `Hareketler.Add 1`, `Pages: ModalPencere 47 (tavan 55)`.

## 7. Canlı doğrulama — başsız Chrome (`betikler/tur/cdp.mjs`, betik `/private/tmp/kuyumcu-serit/KATMAN-2/olc.mjs`, ölçüm `olc/olcum.jsonl`, görüntüler `olc/foto/`)
**4 rol × aç-kapa (PRO aydınlık 1440):** 3 grubu tek tek aç + kapa, sonra aynı gruba 11 hızlı tıklama (tek görev turunda), 3,5 sn bekle, sayfayı yenile.
| Rol | Bant | tercih yanıt kodları | PATCH sayısı (6 tek + 11 hızlı) | Son durum yenileme sonrası |
|---|---|---|---|---|
| SAHIP | 0 | hepsi 200 | 8 | `musteri-stok` açık = aynı |
| MUDUR | 0 | hepsi 200 | 8 | aynı |
| TEZGAHTAR | 0 | hepsi 200 | 8 | aynı |
| MUHASEBE | 0 | hepsi 200 | 8 | aynı |
11 hızlı tıklama → **2 PATCH** (ön-kenar + son durum), eskiden 11 eşanlı PATCH.

**3 paralel sekme (aynı MUDUR, satır ÖNCEDEN SİLİNDİ → gerçek INSERT yarışı; `BASLA` ile 1 ms içinde eşzamanlı başlatıldı):** 3 tarayıcı × 8 PATCH = 24 PATCH, **hepsi 200**, bant 0/0/0, yenileme sonrası durum tutarlı. API günlüğünde bu koşum sırasında `kullanici_tercihleri` benzersiz indeks çakışması **4 kez** oluştu ve yeniden denemeyle **200'e** döndü (düzeltmenin canlıda fiilen çalıştığının kanıtı).

**SQL kanıtı (silmeden önce):** kiracımda her rol × anahtar için **tek satır** (`MUDUR menu-acik-gruplar 1 ["musteri-stok"]`, `MUDUR menu-favoriler 1`, `MUHASEBE/SAHIP/TEZGAHTAR menu-acik-gruplar 1`); `denetim_kayitlari` `KullaniciTercihi` 555.

**Ekran ölçümü (sikisma-olc.js) — değişen ekran: kenar çubuğu (rota `/`) + hata bandı; 1440/960/375 × BASİT/PRO × açık/koyu:**
| Durum | MOD | Tema | w | bant | K1 gizli-görünen | K3 taşan | K3 kırık | K5 TRY | K9 binen | yatay | tercih kodları |
|---|---|---|---|---|---|---|---|---|---|---|---|
| normal aç-kapa | PRO | açık | 1440/960/375 | 0/0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 200 |
| normal aç-kapa | PRO | koyu | 1440/960/375 | 0/0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 200 |
| normal aç-kapa | BASİT | açık | 1440/960/375 | 0/0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 200 |
| normal aç-kapa | BASİT | koyu | 1440/960/375 | 0/0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 200 |
| kalıcı hata (CDP Fetch ile PATCH→500) | PRO | açık | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | bant 216×132 (375: 216×142), taşma 0, Kapat 198×34 (375: 198×44); kapat sonrası bant 0 |
| kalıcı hata | PRO | koyu | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
| kalıcı hata | BASİT | açık | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
| kalıcı hata | BASİT | koyu | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
(PRO açık hata koşumunun "geri alındı" alanı ilk sürümde yanlış ölçütle hesaplandı — önceki/sonraki kümeyi değil tek grubu karşılaştırıyordu; ölçüt düzeltildi, kalan 3 kombinasyonda ✓. Görüntüde de grup sunucunun bildiği duruma dönmüş.)
Görülen görüntüler: `tur-SAHIP-PRO-aydinlik-1440.png` (bant yok), `bant-PRO-aydinlik-1440.png`, `bant-PRO-aydinlik-375.png`, `bant-BASIT-karanlik-1440.png`, `matris-BASIT-karanlik-375.png`.

**Temizlik:** API (5166) ve vekil (5167) `kill`; `lsof` 5166/5167 boş; KATMAN-2 başsız Chrome süreci 0. 5199/5200 dokunulmadı (5199 ayakta). `zsh betikler/kiraci-temizle.sh 01M3QXFPGTM0WZRDKBAB6BKEKV --uygula` (rapor modu: 976 satır + tenants 1); davet satırı `PKATMAN2-DAVET-3009` silindi (ROW_COUNT 1); 70 tenant_id tablosunda bu kimlik için kalan **0**; `tenants` içinde **0**. tenants: başta **14**, bitişte **12** — farkın 2'si başka şeritlerin (ör. `PAKETSATIS-3-HAKEM11-TEST`, `TEST-KESIF`) bu arada kendi kiracılarını silmesi; benim açtığım +1 / sildiğim −1. QA-TEST-* dokunulmadı. Parola/jeton dosyaları (`t1/fikstur-*.json`, `kullanicilar.json`, `tok.json`) `printf '' >` ile boşaltıldı.

### 7b. Tur 2 canlı doğrulama (yeni derleme, 5166/5167, betik `olc.mjs` görevleri `firtina` + `sifirla` eklendi, ölçüm `olc2/olcum.jsonl`, görüntüler `olc2/foto/`, koşum `kos3.sh` → `kos-r2.log` tümü rc=0)
Yeni kiracı `PAKETKATMAN-2-TEST Menu` (davet + `/auth/kayit` 201, 3 kullanıcı 201). HTTP (`t1/kok.py` → `t1/kok-tur2.log`): 4 rol × PATCH/PATCH/GET **200/200/200**; aynı anahtar eşanlı 2 PATCH satır yokken **0/10** başarısız; farklı anahtar eşanlı **0/10**; 10 eşanlı PATCH satır varken/yokken **10×200 / 10×200**.

| Senaryo | Rol · MOD · tema | Sonuç |
|---|---|---|
| Fırtına (66 Alt+↓ sıra yazması → pencere doldu, katlama tık → bant; kapat; 65 sn bekle; tık; ikinci tık; yenile) | SAHIP · PRO · açık | firtina-1: bant 1 (metin sözlükten); firtina-2: **pencereSonraTikPatch 1**, ikinciTikPatch 1, bant 0/0, kodlar 200, **kalıcı true** (ekran = yenileme sonrası `gunluk, musteri-stok, para-banka`) |
| Fırtına | TEZGAHTAR · BASİT · koyu | bant 1 → sonra **1 PATCH**, 1 PATCH, bant 0, **kalıcı true** |
| Sıfırla yarışı (PATCH 1500 ms bekletildi) | SAHIP · PRO · koyu | sıra `PATCH, DELETE`, DELETE PATCH bırakıldıktan 447 ms sonra; bant 0; sıfırla düğmesi kayboldu; yenileme sonrası yalnız `gunluk`; SQL satırı yok |
| 4 rol aç-kapa + 11 hızlı tık + yenile | SAHIP/MUDUR/TEZGAHTAR/MUHASEBE · PRO · açık | her rolde bant 0, 8 PATCH, tüm kodlar 200, yenileme öncesi = sonrası |
| 3 paralel sekme (aynı MUDUR, `BASLA` ile 1 ms içinde) | MUDUR · PRO · açık | 3×8 PATCH hepsi 200, bant 0/0/0, yenileme sonrası üç sekme aynı (son yazan) durum |

Ekran ölçümü (sikisma-olc.js), kenar çubuğu `/`, 1440/960/375 × BASİT/PRO × açık/koyu (24 ölçüm):
| Durum | MOD | Tema | w | bant | K1 | K3 taşan | K3 kırık | K5 TRY | K9 binen | yatay | ek |
|---|---|---|---|---|---|---|---|---|---|---|---|
| normal aç-kapa | PRO | açık | 1440/960/375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kodlar 200 |
| normal aç-kapa | PRO | koyu | 1440/960/375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kodlar 200 |
| normal aç-kapa | BASİT | açık | 1440/960/375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kodlar 200 |
| normal aç-kapa | BASİT | koyu | 1440/960/375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kodlar 200 |
| kalıcı hata (CDP PATCH→500) | PRO | açık | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
| kalıcı hata | PRO | koyu | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
| kalıcı hata | BASİT | açık | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
| kalıcı hata | BASİT | koyu | 1440/960/375 | 1 | 0 | 0 | 0 | – | 0 | 0 | geri alındı ✓, kapat sonrası 0 |
Görülen görüntüler (Read ile): `firtina-sonra-SAHIP-PRO-aydinlik.png` (bant yok, üç grup açık), `sifirla-SAHIP-PRO-karanlik.png` (bant yok, yalnız Günlük iş açık, sıfırla düğmesi yok), `matris-BASIT-karanlik-375.png` (çekmece menü, taşma yok).

**Tur 2 temizlik:** API PID `kill` (5166 boş), vekil `kill` (5167 boş), olc.mjs/CDP 95xx süreç 0; 5199 ayakta, dokunulmadı. `kiraci-temizle.sh 01M3R2TNSWDCHADTXSKX607W0V --uygula`; information_schema'daki **70** tenant_id tablosunda kalan **0**; davet satırı silindi (ROW_COUNT 1); tenants başta **13**, sonda **13**. QA-TEST-* dokunulmadı. Parola/jeton dosyaları `printf '' >` ile boşaltıldı (toplam 0 bayt).

## 8. Değişen dosyalar
- `dotnet/Kuyumcu.Api/Modules/Tercih/TercihModulu.cs` (Yama: sınırlı yeniden deneme)
- `dotnet/Kuyumcu.Web/Servisler/MenuTercihServisi.cs` (tek yazıcı + birleştirme + durulma, 429 sessiz yeniden deneme, sunucu durumuna geri alma, HatayiKapat)
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` (yalnız tercih bandına Kapat düğmesi)
- `dotnet/Kuyumcu.Tests/KATMAN2MenuTercihTestleri.cs` (yeni; tur 2: +3 test, sahte API PATCH/DELETE sırasını kaydeder)
- Tur 2 `git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeci: ` M dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`, `?? dotnet/Kuyumcu.Api/Modules/Tercih/`, `?? dotnet/Kuyumcu.Web/Servisler/MenuTercihServisi.cs` (test dizini ağaçta izlenmiyor); proje ağacında .mutorj/.bak/.tmp 0. Doğrulayıcının `Kuyumcu.Tests/Dogrulayici/KATMAN2SinirTestleri.cs` dosyasına dokunulmadı.
(`git status` bu dosyaları ilk commit'e göre M/?? gösteriyor; ağaç genelinde çok sayıda başka şeridin değişikliği var.)

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Önce kök nedeni ölç (a–d) | ✓ | §1 tablo, `t1/kok.log`, `t1/d.log` |
| Yetkiyse her rol kendi kaydına yazsın | ✓ (zaten yazıyordu) | 4 rol 200 (canlı + HTTP testi) |
| Eşanlılıksa anahtar bazlı birleştirme | ✓ | sunucu yeniden deneme + istemci tek yazıcı; farklı anahtar eşanlı → ikisi kalıcı |
| Oran sınırıysa debounce 300–500 ms + 429 sessiz yeniden deneme, bant yalnız kalıcı hatada, tek, kapatılabilir | ✓ (sapma 1–2 gerekçeli) | 350 ms; bUnit 429 testleri; Kapat düğmesi |
| Hata bandı mekanizması korunur | ✓ | kalıcı 500'de bant + geri alma (canlı 12 ölçüm) |
| PATCH sözleşmesi korunur | ✓ | mevcut testler yeşil |
| KABUL HTTP: 4 rol 200; eşanlı iki farklı anahtar kalıcı; 10 hızlı → bant yok, son durum sunucuda | ✓ | §4, §7 |
| bUnit: debounce/birleştirme | ✓ | 6 bUnit testi |
| Canlı: 4 rol + 3 paralel sekme + görüntü, bant yok | ✓ | §7 |
| Mutasyon: düzeltmeyi geri al → yeni test kırmızı | ✓ | 8/8 |
| OranSiniri giriş sınırını gevşetme | ✓ | dosyaya dokunulmadı |
| KATMAN-1b işini koru (katman.js/css/GlobalArama) | ✓ | dokunulmadı |
| Tur 2: hasım KRİTİK (tamamlanmış göreve takılma) | ✓ | §0; doğrulayıcının 2 kırmızısı yeşil; fırtına bUnit + canlı |
| Tur 2: sıfırla/PATCH yarışı | ✓ | §0; bUnit + canlı + SQL |
| Tur 2: Panel ham hata metni | ✗ (alan dışı) | §10.1 — ORTAK-1 |

## 10. Açık kalanlar
1. **[ORTA — ORTAK-1 (Panel) / orkestratör]** Karşılama ekranı (`/`) `MenuTercihServisi.Hata`'yı `HataAnahtari`'na bakmadan içerik alanında ham metinle basıyor: katlama yazması kalıcı hata verince panelde de İngilizce "Internal server error" bandı çıkıyor (görüntü `bant-PRO-aydinlik-1440.png`). Panel yalnız `AnahtarPanel` hatasını göstermeli ve sözlük metni kullanmalı. Dosya bu şeridin alanında değil (Panel = ORTAK-1) → dokunulmadı.
2. **[denetim.sh ✗ — ZO-BELGE / orkestratör]** `P-23 ODEME_KALEMSIZ_YOL 6`: `PAKETZO-BELGE-TEST Kuyumculuk` (4) + `TEST-KESIF Kuyumculuk` (2) kiracılarının canlı hareketleri.
3. **[test ağacı — sahibi belirsiz, orkestratör]** `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` kırmızı: ağaçta `dotnet/denetim.sh.mutbak` (0 bayt, 18.09 tarihli) var. Bu paket üretmedi; dosya silme yasağı nedeniyle dokunulmadı.
4. **[kararsız bUnit — sahipleri: T-K3 / kabuk arama]** `TK3AyarlarTuvalTestleri.TK3_02_…` ve `KabukAramaEkranTestleri.Secilen_sube_…` eşzamanlı şerit yükünde 4 koşumda birer kez kırmızı, yalıtık koşumda yeşil; tercih yazmasına dokunmuyorlar.
5. ~~Katlama PATCH'i uçuştayken "Grupları sıfırla" yarışı~~ — **tur 2'de KAPANDI** (§0).
6. **[denetim.sh ✗ — Cariler.razor sahibi / orkestratör]** tur 2 koşumunda `P-13: GEREKCESIZ_KILITLI_ALAN 1 (Cariler.razor:1)`; bu paket Cariler.razor'a dokunmadı.
7. Tam süit bu turda da uygulayıcı tarafından koşulmadı (29.09 hız kuralı: yalnız hasım doğrulayıcı).

## 11. Kurucuya / mali müşavire
Mevzuat içeriği yok. Kurucuya: kırmızı bandın sebebi yetki değil, aynı anda iki yazma yarışıydı; menü aslında çoğu zaman kaydediliyordu ama ekran "kaydedilemedi" diyordu. Artık yarış sunucuda çözülüyor, hızlı tıklamalar tek yazmada birleşiyor; bant yalnız gerçekten kaydedilemediğinde çıkıyor ve kapatılabiliyor.
