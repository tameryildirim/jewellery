# Paket 38 — KDV özel matrah otomatiği (kuyum KDV'si) · Faz A · Uygulama raporu

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Model: Opus 5 · Dalga 2
> Tarih: 2026-09-16, gece (DEVAM KOŞUMU — 3. koşum; 1. koşum yarım kaldı, 2. koşum hasım doğrulamadan 6 bulguyla döndü)
> Kapsam: **yalnız Faz A**. Faz B (e-Arşiv/entegratör, yazdırılabilir çıktı) YAPILMADI — entegratör sözleşmesi Kurucu'da.

## 0. Devralma notu (Anayasa md.1 — ne atlama, ne varsayım)
Bu koşum sıfırdan yazmadı; önce **ölçtü**. Ölçülen devralma durumu:
- `Modules/Fatura/KdvAyarlari.cs` ve `Modules/Fatura/OzelMatrahMotoru.cs` ağaçta (önceki koşumda yazıldı, bu koşumda okundu ve doğrulandı).
- Migration `20260916150104_FaturaOzelMatrah` ağaçta **ve** `kuyumcu_net`'e uygulanmış (`__EFMigrationsHistory` son satır = bu migration; toplam 28 migration). **İkinci migration ÜRETİLMEDİ.**
- Yeni test dosyaları ağaçta: `FaturaOzelMatrahTestleri.cs` (18 Fact), `OzelMatrahMotoruTestleri.cs` (9 Fact); `FaturaEkranTestleri.cs` (+5 Fact) ve `HttpBoruHattiTestleri.cs` (+1) güncellenmiş.
- Bu koşumda **yeni kod yalnız iki yerde** yazıldı (hasım bulgusunu kapatmak için): `app.css` (+`.satir--aralik-sm`) ve `Faturalar.razor:22` (satır içi style → sınıf). Geri kalan her madde kodda **doğrulandı**, tekrar yazılmadı.

### Migration kolon sayısı — spesifikasyondan sapma ve gerekçesi
Spesifikasyon "faturalar'a 4 yeni kolon" diyor; bu migration **3 kolon** ekliyor (`has_gram`, `has_birim_fiyat`, `ozel_matrah_kodu`) + `(tenant_id, kdv_turu, tarih)` indeksi. Dördüncü kolon `has_deger_kaynagi` **paket 1-A'nın** `20260916081339_FaturaKalemleri` migration'ında zaten eklenmiş (kanıt: `grep has_deger_kaynagi Data/Migrations/*.cs` → `20260916081339_FaturaKalemleri.cs:15`). Canlı şema doğrulaması: `information_schema` → `has_deger decimal(15,2)`, `has_gram decimal(15,6)`, `has_birim_fiyat decimal(15,4)`, `has_deger_kaynagi varchar(10)`, `ozel_matrah_kodu varchar(5)` — **beşi de mevcut**. Eksik kolon YOK, bu yüzden "DUR ve raporla" koşulu tetiklenmedi.

## 1. Yapılanlar (ölçülen durum)
| # | Spesifikasyon maddesi | Durum | Kanıt |
|---|---|---|---|
| 1 | `OzelMatrahMotoru` — SAF, bacak bacak, tek motor (ADR-0016) | önceki koşumda yapıldı, **bu koşumda doğrulandı** | `Modules/Fatura/OzelMatrahMotoru.cs` okundu: DB/ağ/saat yok; `HasBedeli` + `Hesapla`; ikinci motor/`FaturaKdvHesabi.cs` YOK |
| 2 | POST /faturalar — KUYUM+hareketId'de has bedeli SUNUCUDA türetilir | yapıldı, doğrulandı | `FaturaModulu.cs:203-231`; canlı adım 5 |
| 3 | Tutarlılık kapısı — tolerans **SIFIR** | yapıldı, doğrulandı | `FaturaModulu.cs:213-221` (`Yuvarla2(gonderilen) != sonuc.HasDeger` → 400); canlı adım 7; mutasyon M38-2 |
| 4 | `has_deger_kaynagi` üç değerli: HAREKET / KALEM / ELLE | yapıldı, doğrulandı | `FaturaModulu.cs:278` (HAREKET/ELLE), `:716` kalemli yolda `KALEM`; canlı SQL `HAREKET` |
| 5 | GET `/faturalar/ozel-matrah-onizleme` + rol kapıları | yapıldı, doğrulandı | `FaturaModulu.cs:1120-1126` `[HttpGet]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`, sınıfta `[ModulYetkisi("fatura")]` (`:1093`) |
| 6 | Ekran `hareketId` gönderiyor | yapıldı, doğrulandı | `Faturalar.razor:897` POST gövdesi `hareketId`; bUnit `KUYUM_da_POST_govdesi_hareketId_ve_TURETILEN_hasDeger_tasir` |
| 7 | `Kdv:OzelMatrahKodu` yapılandırması, varsayılan **BOŞ** | yapıldı, doğrulandı | `KdvAyarlari.cs` (`OzelMatrahKodu` init'siz = null), `appsettings.json:54-58` `"OzelMatrahKodu": ""`; test `Ozel_matrah_kodu_VARSAYILANDA_NULL_kalir`; canlı SQL `ozel_matrah_kodu NULL` |
| 8 | KDV oranı yapılandırmaya taşındı, **davranış değişmedi** | yapıldı, doğrulandı | `VarsayilanOran = 0.20m` varsayılan; `FaturaModulu.cs:177,554`; `BankaModulu.cs:108-116` aynı ayardan; ctor'lar **opsiyonel** (`new FaturaServisi(db)` kırılmadı) |
| 9 | `BankaServisi.FaturayaDonustur` KdvTuru'nu AÇIKÇA yazar | yapıldı, doğrulandı | `BankaModulu.cs:356-358` `KdvTuru="STANDART", HasDeger=0m, OzelMatrahKodu=null` |
| 10 | `RaporFaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir` dokunulmadı | doğrulandı | Test adı/gövdesi aynı; ELLE yolu `FaturaModulu.cs:234-242` korunmuş (aşağıda § açık kalanlar) |
| 11 | Satır içi style (bu dalgada eklenen) sınıfa taşındı | **bu koşumda yapıldı** | `app.css:350 .satir--aralik-sm { gap:.5rem }` + `Faturalar.razor:22 class="satir satir--aralik-sm"` |

## 2. Derleme (bu koşumun kendi çıktısı)
```
dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata
Geçen Süre 00:00:04.68
```
Mutasyon turundan sonra tekrar: `0 Uyarı / 0 Hata` (Geçen Süre 00:00:03.86).

## 3. Migration
- Ad: **`20260916150104_FaturaOzelMatrah`** (önceki koşumda üretildi; bu koşumda yalnız doğrulandı).
- `.cs` Up(): `has_birim_fiyat decimal(15,4) NULL`, `has_gram decimal(15,6) NULL`, `ozel_matrah_kodu varchar(5) NULL`, `IX_faturalar_tenant_id_kdv_turu_tarih`. Down() üçünü de geri alıyor.
- `.Designer.cs`: 103.346 bayt, `BuildTargetModel` **var** (1 isabet).
- `kuyumcu_net`'e uygulanmış: `__EFMigrationsHistory` → `20260916150104_FaturaOzelMatrah` (28 satırın sonuncusu).
- **İkinci migration üretilmedi.**

## 4. Testler (bu koşumun kendi ölçümü — İKİ ARDIŞIK TAM KOŞUM)
| Koşum | Toplam | Geçti | Kaldı | Kanıt |
|---|---|---|---|---|
| Tam süit #1 | **1473** | **1473** | **0** | `trx-p38/_Mac_2026-09-16_20_12_35.trx`, `Test Çalıştırması Başarılı.` |
| Tam süit #2 (ardışık) | **1473** | **1473** | **0** | `test2.txt`: `Toplam test sayısı: 1473 / Geçti: 1473`, `grep -c "^  Başarısız " = 0` |

Hasım doğrulayıcının bildirdiği **kırılgan (flaky)** `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR` bu koşumun **iki tam koşumunda da kırmızıya dönmedi**. Kırılganlık **çürütülmedi, yalnız tekrar etmedi** — paket 38 kapsamı dışıdır ve ayrı iş kalemi olarak açık bırakılıyor (§ 12).

Bu pakette doğan test sayısı: **33** — `OzelMatrahMotoruTestleri.cs` 9 · `FaturaOzelMatrahTestleri.cs` 18 · `FaturaEkranTestleri.cs` +5 · `HttpBoruHattiTestleri.cs` +1.

## 5. Mutasyon kanıtları (HEPSİ BU KOŞUMDA ÜRETİLDİ)
Yöntem: kaynak `scratchpad/mut-yedek/paket38/` altına kopyalandı → `perl -0777 -pi -e` ile TEK eşleşmeli değişiklik (her mutasyonda `diff` ile "değişen satır sayısı = 1" ölçüldü) → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` **birebir aynı**.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| M38-1 | `hasDeger = sonuc.HasDeger;` → `dto.HasDeger ?? 0m` (türetim kaldırıldı) | `FaturaOzelMatrahTestleri.HareketId_verilen_KUYUM_faturada_has_deger_DEFTERDEN_turetilir` | birebir aynı |
| M38-2 | tutarlılık kapısı koşulu → `&& false` | `FaturaOzelMatrahTestleri.Istemci_FARKLI_hasDeger_gonderirse_400_ve_HICBIR_satir_yazilmaz` | birebir aynı |
| M38-3 | `hasliSatirlar` kümesine `.Take(1)` (matrah tek hareketten) | `FaturaOzelMatrahTestleri.Cok_kalemli_fiste_matrah_FISIN_TAMAMINDAN_cikar_ve_damga_kumesiyle_AYNIDIR` | birebir aynı |
| M38-4 | fail-closed gevşetildi: `kur is null \|\| kur.AltinHasFiyati <= 0m` → `kur is null` | `FaturaOzelMatrahTestleri.Kur_kaydi_has_fiyati_SIFIR_olan_hareket_400_verir_ve_fatura_yazilmaz` | birebir aynı |
| M38-5 | motorda her bacak `bacaklar[0].HasBirimFiyat` ile çarpıldı (tek kur varsayımı) | `OzelMatrahMotoruTestleri.Iki_bacak_FARKLI_kurla_carpilir_ortalama_kur_KULLANILMAZ` | birebir aynı |
| M38-6 | `Math.Abs(h.HasGrami)` → `h.HasGrami` (satışın negatif has'ı) | `FaturaOzelMatrahTestleri.HareketId_verilen_KUYUM_faturada_has_deger_DEFTERDEN_turetilir` | birebir aynı |
| M38-7 | `OzelMatrahKodu = kdvTuru=="KUYUM" ? ayar.KodYaDaNull : null` → koşulsuz (satır 275) | `FaturaOzelMatrahTestleri.Yapilandirmada_kod_ACIKSA_YALNIZ_KUYUM_faturaya_yazilir` | birebir aynı |
| M38-8 | `KuyumEngeli => Kuyum && string.IsNullOrEmpty(formHareketId)` → `=> false` | `FaturaEkranTestleri.KUYUM_da_islem_SECILMEDEN_Kaydet_KAPALI_ve_sebebi_YAZILI` | birebir aynı |

**Test açığı bulunmadı** — sekiz mutasyonun sekizi de kırmızı. Son mutasyondan sonra temiz `dotnet build`: 0 uyarı / 0 hata.

## 6. denetim.sh (bu koşumun çıktısı — mutasyonlardan SONRA tekrarlandı)
```
✓ IgnoreQueryFilters (yasak) 0 · new KuyumcuDbContext 1 · Find/FindAsync 0
✓ ExecuteUpdate/Delete (beyaz liste 4) 4 · Hareketler.Add 1 · VeresiyeKapanisi 2
✓ IKiraciVarlik 33 · HasQueryFilter 2
✓ oran sınırı/HTTPS/sıkıştırma/sağlık ucu var · ArayuzModu 6 · kapı yüzeyinde 0
✓ Finansal varlıkta [HttpDelete] 0 · CORS beyaz liste
DENETİM TEMİZ.
```

## 7. Canlı doğrulama — BU KOŞUM (localhost:5198; 5199'a DOKUNULMADI)
Kiracı: **PAKET38-TEST DOGRULAMA** (`01M2NKF5YW1SKNQ7V7N03SWKV7`) · canlı kur `altin_has_fiyati = 6814.3800` (`ALTINKAYNAK`).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `/health` | 200 | **200** (1. denemede) | API PID 28585 |
| 2 | `POST /auth/kayit` | 201 | **201** | `tenantId 01M2NKF5YW…` |
| 3 | `GET /kur/guncel` (10-20 sn tuzağı) | id + altinHasFiyati | **200**, `id 01M2NKFD35…`, `altinHasFiyati 6814.3800` | yanıt okundu |
| 4 | Ürün (ZIYNET K22, 10 g) + 10 adet alış + 2 adet satış (HAVALE) | 200 | **201 / 200 / 200** | satışta `hasGrami = -18.320000`, `tlTutari = 130000.00`, `hareketId 01M2NKGG0D…` |
| 5 | Bağımsız hesap (perl) vs `GET /faturalar/ozel-matrah-onizleme?hareketId=…&kdvOran=0.20&toplam=130000` | birebir aynı | perl: `hasDeger 124839.44 · matrah 4300.47 · kdv 860.09` — uç: **200**, `hasGram 18.320000 · hasDeger 124839.44 · matrah 4300.47 · kdvTutar 860.09 · araToplam 129139.91 · kullanilanKurSayisi 1 · hasDegerKaynagi HAREKET` → **BİREBİR AYNI** | yanıt gövdesi okundu |
| 6 | `POST /faturalar` KUYUM + hareketId, **hasDeger GÖNDERİLMEDİ** | 200, sunucu doldurur | **200** `FTR-2026-000001`, `hasDeger 124839.44 · hasDegerKaynagi HAREKET · ozelMatrahKodu null` | SQL: `has_gram 18.320000 · has_birim_fiyat 6814.3799 · has_deger_kaynagi HAREKET · ozel_matrah_kodu NULL` |
| 7 | **Has bedeli matrah DIŞI mı** (piyasa koşulu) | KDV yalnız işçilik/marj | SQL: `toplam 130000.00 · has_deger 124839.44 · kdv_tutar 860.09` — has bedeli düşülmeseydi KDV **21666.67** olurdu | `SELECT … ROUND(toplam-toplam/1.2,2)` |
| 8 | Aynı hareketten ikinci fatura | 400 | **400** "Bu hareket zaten FTR-2026-000001 ile faturalandırılmış" | mevcut davranış korunuyor |
| 9 | `hasDeger` 1 TL kaydırılmış POST | 400 + satır yazılmaz | **400** "…defterden 124839.44 TL olarak türetildi, gönderilen 124840.44 TL — fark 1.00 TL. Hareket bağlı kuyum faturasında tolerans yoktur." · SQL fatura sayısı **ÖNCE 1 → SONRA 1** | sessiz yutma yok |
| 10 | Var olmayan hareketId ile önizleme | 404 | **404** "Hareket bulunamadı" | kiracı/varlık sınırı |
| 11 | API durduruldu | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** | PID 28585 `kill` |
| 12 | Test kiracıları silindi | tenants gerçek başlangıca döner | `tenant_id` kolonu olan **33 tablodan** silindi; `faturalar/hareketler/kur_snapshots/urunler/kullanicilar/cariler/kasalar` kiracı satırı **0**; `SELECT COUNT(*) FROM tenants` → **7** | `QA-TEST-1/2` kiracılarına DOKUNULMADI |

**5199 notu:** koşum sonunda `lsof -nP -iTCP:5199 -sTCP:LISTEN` → **3 satır** (Kurucu'nun inceleme sunucusu ayakta). Porta hiç dokunulmadı.

### Önceki koşumun canlı kanıtları (2026-09-16 20:07 raporundan devralındı, ÜZERİNE YAZILMADI)
Önceki koşum 17 adımlık daha geniş bir canlı tur koştu: çok kalemli fiş (iki kalem tek fatura_no, `has_gram` iki kalemin toplamı), `altin_has_fiyati = 0` ile **400 fail-closed**, banka dekontundan `STANDART`/`hasDeger 0`/kod NULL, `Kdv__OzelMatrahKodu=805` ile yeniden başlatıp **yalnız KUYUM** faturaya kodun yazılması. Bu koşum o adımları **tekrarlamadı** (adım ekonomisi); ilgili davranışların hepsi DB/HTTP testleriyle ayrıca sabitli (§ 4, § 5).

## 8. DoD sözleşmesi (Bertan koşulu + Mühür 2)
| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| `faturalar` (finansal) | **hard DELETE YOK**; durum değişimi (TASLAK→KESILDI→IPTAL) mevcut uçlardan; düzeltme yolu storno/iptal | Mühür 2: finansal varlıkta silme yasak; `denetim.sh` "Finansal varlıkta [HttpDelete] = 0" |
| Yeni 4 kanıt alanı (`has_gram`, `has_birim_fiyat`, `has_deger_kaynagi`, `ozel_matrah_kodu`) | **salt okunur / türetilmiş** — kullanıcıdan istenmez (UX-S21), `DegismezIpucu` ile gerekçelenir | Bu paket yeni bir *kayıt türü* doğurmaz; mevcut fatura kaydına kanıt alanı ekler → yeni PATCH/DELETE ucu gerekmez |
| Yeni uç `GET ozel-matrah-onizleme` | **salt okunur** (yazma yok) → denetim kaydı gerekmez | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` + sınıf düzeyinde `[ModulYetkisi("fatura")]` |

DoD EK (kurul tur 1): ekranda **tıklanınca hiçbir şey olmayan düğme yok** — `fom-yenile` istek atar, `disabled` durumunda `fom-kilit` sebebi yazar (UX-S30); KUYUM'da işlem seçilmeden Kaydet kapalı ve sebebi yazılı (bUnit ile sabit); yeni `ModalPencere` düzenleme kalıbı açılmadı (denetim sayaçları değişmedi); hata bandı `HataBandi` ile ayrı (UX-S28).

**Tek dokunuş — bu dalgada dokunulan sayfalar:** `Kuyumcu.Web/Pages/Faturalar.razor` (paket 38'in kendi sayfası) ve `wwwroot/css/app.css` (yeni yardımcı sınıf). Başka razor sayfası açılmadı.

## 9. Koşul karşılama tablosu
| Koşul | Durum | Kanıt |
|---|---|---|
| Yalnız Faz A; Faz B yapılmadı | ✅ | e-Arşiv/entegratör/yazdırılabilir çıktı kodu YOK |
| Tek motor (ADR-0016), ikinci motor/`FaturaKdvHesabi.cs` yok | ✅ | `ls Modules/Fatura/` → yalnız `FaturaModulu.cs`, `KdvAyarlari.cs`, `OzelMatrahMotoru.cs` |
| `has_deger_kaynagi` HAREKET \| KALEM \| ELLE | ✅ | `FaturaModulu.cs:278` / `:716` / `:237`; canlı SQL `HAREKET` |
| Önizleme ucu + rol/modül kapısı | ✅ | `:1120-1126` + `:1093` |
| Ekran `hareketId` gönderiyor | ✅ | `Faturalar.razor:897`; bUnit testi |
| `Kdv:OzelMatrahKodu` varsayılanı **BOŞ** ('805' değil) | ✅ | `KdvAyarlari.cs` + `appsettings.json` `""` + test |
| KDV oranı yapılandırmadan, **davranış değişmedi** (0.20) | ✅ | `VarsayilanOran = 0.20m`; `Kdv_orani_yapilandirmadan_okunur_varsayilan_020_davranisi_AYNI` |
| `new FaturaServisi(db)` / `BankaServisi` ctor kırılmadı | ✅ | ctor parametresi `KdvAyarlari? = null`; `RaporFaturaTestleri.cs` derleniyor ve yeşil |
| `RaporFaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir` dokunulmadı | ✅ | ad/gövde aynı, yeni assert yok |
| Kur kayıtları tek sorguda `ids.Contains` + `TenantId` | ✅ | `FaturaModulu.cs:317-323` |
| `AltinHasFiyati <= 0` → 400 fail-closed | ✅ | `:330-332`; M38-4 |
| KUYUM+HareketId'de tolerans **SIFIR** | ✅ | `:213-221`; canlı adım 9 |
| Tek migration, `dotnet ef` build'li, `--no-build` yok | ✅ | tek yeni migration; bu koşumda `dotnet ef` hiç çalıştırılmadı (gerekmedi) |
| `dotnet build` 0 hata (0 uyarı) | ✅ | § 2 |
| Tam süit 0 başarısız (2 ardışık koşum) | ✅ | § 4 |
| `denetim.sh` TEMİZ | ✅ | § 6 |
| Test kiracısı silindi, tenants başlangıca döndü | ✅ | § 7 adım 12 → **7** |
| COMMIT YOK | ✅ | `git add/commit/push` çalıştırılmadı; çalışma ağacındayız |
| Yeni CSS'te sabit renk yok / satır içi style eklenmedi | ✅ | `.satir--aralik-sm { gap:.5rem }` — renk yok; bu dalgada eklenen tek inline style kaldırıldı |
| UX-S21 (sistemin bildiği alan istenmez) | ✅ | has bedeli girdisi yok; türetilir + kaynak rozetiyle gösterilir |
| UX-S28 / S29 / S30 | ✅ | `HataBandi` ayrı; `fom-sinir` sınır uyarısı; `fom-kilit` kilit sebebi |

## 10. Hasım doğrulamanın 6 bulgusu — bu koşumda ne yapıldı
| Bulgu | Durum | Yapılan |
|---|---|---|
| [KRITIK] `PAKET38-TEST` kiracısı canlı DB'de duruyordu | **KAPANDI** | Hem eski `PAKET38-TEST` (`01M2NCQH9GK50NEZT5BDCVEHA1`) hem bu koşumun kiracısı 33 tablodan silindi; `tenants` **7**. Önceki koşumun "başlangıç 8" ölçümü **yanlıştı**: 8 rakamı, o koşumdan önce silinmeden kalmış `PAKET38-TEST` kiracısını İÇERİYORDU; gerçek başlangıç **7**'dir. Bu, önceki raporun "tenants başlangıç değerine döndü" beyanını geçersiz kılar — düzeltildi, gizlenmedi. |
| [ORTA] `KarmaOdemeEkranTestleri` kırılgan | **AÇIK (kapsam dışı)** | Bu koşumun iki ardışık tam süitinde de kırmızıya dönmedi; kırılganlık çürütülmedi. Ayrı iş kalemi (§ 12). |
| [ORTA] `fatura-hasdeger` id'si kaldırıldı, gerekçe yazılmamıştı | **KAPANDI (kayıt düşüldü)** | **Kaldırılan id:** `fatura-hasdeger` (elle has bedeli `<input>`'u). **Gerekçe:** UX-S21 — sunucuda türetilebilen değer kullanıcıdan istenmez; alan girdi olmaktan çıkıp salt okunur oldu. **Yerine gelen id:** `fom-hasdeger` (türetilen has bedelinin salt okunur gösterimi); ayrıca `fom-hasgram`, `fom-haskur`, `fom-kapsam`, `fom-defter`, `fom-matrah`, `fom-kdv`, `fom-aratoplam`. Sabitleyen test: `FaturaEkranTestleri.cs` `Assert.Empty(e.FindAll("#fatura-hasdeger"))`. **"Mevcut element id'leri DEĞİŞMEZ" değişmezine karşı İSTİSNA talebi** — orkestratör kapı kaydında mühürlenmeli; dış otomasyon bu seçiciyi kullanıyorsa `#fom-hasdeger`'e taşınmalı. |
| [DUSUK] `Faturalar.razor`'da satır içi style | **KISMEN KAPANDI** | Bu dalgada eklenen tek kullanım (`:22`) `.satir--aralik-sm` sınıfına taşındı. Kalan 4 kullanım (`:79, :85, :98, :262` — hepsi `var(--…)` belirteçli, sabit renk yok) **bu paketten önce vardı**, izinsiz değiştirilmedi → ayrı temizlik kalemi. |
| [DUSUK] 5199 dinlemiyordu | **KENDİLİĞİNDEN KAPANDI** | Koşum sonunda 5199'da **3 dinleyici** var; porta dokunulmadı. |
| [ORTA] ELLE yolunda sessiz 0 açığı | **AÇIK — karar kalemi** | Bilinçli olarak değiştirilmedi (§ 12/1). |

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, paket 38 kesiti)
```
 M dotnet/Kuyumcu.Api/Data/Entities.cs                      (faturalar: 4 kanıt alanı)
 M dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs        (türetim + kapı + önizleme + faturalanabilir liste)
 M dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs          (KdvTuru/HasDeger/kod açıkça + KdvAyarlari)
 M dotnet/Kuyumcu.Api/Program.cs                            (Kdv bölümü bağlandı)
 M dotnet/Kuyumcu.Api/appsettings.json                      (Kdv: VarsayilanOran 0.20, OzelMatrahKodu "")
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                    (TR+EN yeni anahtarlar)
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor                 (işlem seçici, önizleme, salt okunur has bedeli)
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                   (.satir--aralik-sm — BU KOŞUM)
?? dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs
?? dotnet/Kuyumcu.Api/Modules/Fatura/OzelMatrahMotoru.cs
?? dotnet/Kuyumcu.Api/Data/Migrations/20260916150104_FaturaOzelMatrah.cs (+ .Designer.cs)
?? dotnet/Kuyumcu.Tests/FaturaOzelMatrahTestleri.cs
?? dotnet/Kuyumcu.Tests/OzelMatrahMotoruTestleri.cs
   (Kuyumcu.Tests/ dizininin tamamı git'te izlenmiyor → FaturaEkranTestleri.cs ve
    HttpBoruHattiTestleri.cs güncellemeleri "??" altında görünür.)
```
COMMIT YAPILMADI.

## 12. Açık kalanlar
1. **[ORTA · karar kalemi] ELLE yolunda sessiz 0.** `FaturaModulu.cs:237` `hasDeger = dto.HasDeger ?? 0m;` → `kdvTuru=KUYUM` + `hareketId` YOK + `hasDeger` YOK olan POST'ta `has_deger = 0` yazılır ve KDV **tüm tutar** üzerinden hesaplanır. Ekran bu yolu kapatıyor (`Faturalar.razor:721 KuyumEngeli`), **API doğrudan çağrılırsa açık**. Bu pakette bilerek değiştirilmedi: spesifikasyonun regresyon sözleşmesi `RaporFaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir` bu davranışı koruyor. **Ekip oyuna sunulacak öneri:** "KUYUM + hareketId yok + hasDeger yok → 400"; kabul edilirse regresyon testi AYNI anda güncellenmeli, yoksa sözleşme çelişir.
2. **[ORTA] `KarmaOdemeEkranTestleri` kırılganlığı** (paket 38 kapsamı dışı): bUnit render sırası / statik durum sızıntısı incelenmeli; kapı ölçümü en az iki ardışık tam koşumla alınmalı (bu koşumda öyle yapıldı).
3. **[DUSUK] `Faturalar.razor` kalan 4 satır içi style** (`:79, :85, :98, :262`) — ayrı temizlik kalemi.
4. **[DUSUK] `fatura-hasdeger` id istisnası** orkestratör kapı kaydında mühürlenmeli (§ 10).
5. **Faz B yapılmadı**: e-Arşiv/entegratör alan eşlemesi ve yazdırılabilir fatura çıktısında özel matrah kırılımı. Entegratör sözleşmesi Kurucu'da.
6. Proje ağacında önceki paketlerden kalma `*.mutorj` artıkları var (`HareketFabrikasi.cs.mutorj` vb.). Hook `rm`'i bloklar; silinmesi **Kurucu/orkestratör** kararı.

## 13. Kurucuya / mali müşavire notlar
1. **`805` kodu DOĞRULANMADI.** `Kdv:OzelMatrahKodu` varsayılanı **boştur**; boş olduğu sürece `ozel_matrah_kodu` NULL kalır, faturada ve hiçbir belgede basılmaz, hiçbir hesaba girmez. Kodun hangi GİB listesine ait olduğu ve karşılığı **mali müşavir teyidi** ister; teyitten sonra yapılandırmadan açılır (canlı testte `Kdv__OzelMatrahKodu=805` ile açıldığında yalnız KUYUM faturaya yazıldığı önceki koşumda kanıtlandı).
2. **KDVK md.23/e yorumu** (has bedeli matrah dışı, KDV yalnız işçilik/marj üzerinden) kodda tek motorda toplandı; birincil mevzuat metni ekip tarafından okunmadı — **doğrulama mali müşavirde**.
3. **KDV oranı** artık `Kdv:VarsayilanOran`'dan okunuyor (bugünkü davranış: 0.20). Oran değişirse kod değişmeden yapılandırmadan güncellenir.
4. **e-Arşiv (Faz B)** için entegratör sözleşmesi gerekiyor — dış taahhüt olduğu için Kurucu'ya kalıyor.
