# PAKET YASAL-4 — Uygulama raporu (Y4-A → Y4-C → Y4-B; Y4-D 30.09'da bitti, dokunulmadı)

> Tarih: 01.10.2026 · Şerit: t1 (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9700-9715)
> Rol: Taşkın (backend) + Ertuğ (frontend) · Opus · Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-YASAL-4-GIDER-PUSULASI-23E-KMTS-2026-09-30.md`
> COMMIT YOK. Dosya silme YOK.

## 0. Devir notu — üç tur (1. tur kesinti, 2. tur tamamlama, 3. tur hasım bulguları)

İlk uygulayıcı 05:52–07:11 arasında çalışıp internet kesintisiyle düştü; rapor yazamadı. Ağaçtaki iş **yeniden yazılmadı**, olduğu yerden tamamlandı. O oturumun bıraktıkları (`/private/tmp/kuyumcu-serit/YASAL-4/`):

- Kodun tamamı yazılmış, migration uygulanmış, canlı doğrulama (`canli/canli.log`) ve 13 mutasyon (`mut/`) yapılmıştı.
- 07:00'deki tam süitte 5 kırmızı vardı. 2'si bu paketindi: `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` (yeni `StopajOdemesi` varlığı haritada yoktu) ve `S31PanelSadeTestleri.Y4A_K1`. 07:01'de ikisi de kaynakta düzeltilmişti, ama ikili (dll) eski kalmıştı.
- Tarayıcı turu (07:01–07:11) koşulmuş, ölçümleri okunmamıştı.
- Yarım kalmış mutasyon yok: `yedek/` dizininde paket öncesi kopyalar var; ağaçtaki dosyalar hepsinden farklı, yani iş yerinde duruyor. `mut/*.yedek` dosyaları geri yüklenmiş durumda (o turun sonuçlarında hepsi `cmp AYNI`).
- Test kiracısı `01M3TSP90RE8J16HBZD63Y1QE0` "PAKETYASAL4-TEST-T1" duruyordu. API (PID 29752) ve vekil (PID 40446) hâlâ çalışıyordu.

Bu oturumda (2. tur) yapılanlar:

1. `DuzeltmePolitikasi.cs` dokunularak yeniden derlendi (MSBuild, dll'yi düzenlemeden 7 sn sonra yazdığı için güncel saymıştı). `Her_varlik_haritada_VARDIR` yeşile döndü.
2. **Tarayıcı bulgusu düzeltildi:** Panel 375 px PRO'da "Gider pusulaları →" bağlantısı iki satıra kırılıyordu (K3_kirik=1; açık ve koyu temada). `Panel.razor`'daki 4 Y4 bağlantısına mevcut `hucre-tek-satir` sınıfı verildi. Yeni CSS yazılmadı; satır içi stil yok. Yeniden ölçümde 0.
3. Bütün mutasyonlar güncel kodla **baştan koşuldu**. M5 (denetim sayacı) ilk turda kayıtlı değildi, eklendi.
4. Tam süit son kodla koşuldu. Ardından `denetim.sh`, temiz build ile canlı yeniden kontrol, tam tarayıcı turu ve hedefli ölçüm (fatura has satırı, ürün KMTS alanı, İşletme KMTS ayarı) yapıldı. API ve vekil durduruldu, test kiracısı silindi.

## 0b. 3. tur — hasım doğrulayıcının 7 bulgusu (01.10, 10:30–11:55)

Doğrulayıcının (dogY4) bulguları sırayla kapatıldı. Ağaçtaki iş korundu, yalnız bulgunun gösterdiği yerlere dokunuldu.

| # | Önem | Bulgu | Ne yapıldı | Kanıt |
|---|---|---|---|---|
| 1 | **KRİTİK** | Stopaj ödemesinde eşzamanlılık açığı. Kalan kilitsiz okunuyordu, iki ödeme kesilenin üstüne çıkabiliyordu. `Math.Max(0,…)` fazla ödemeyi gizliyordu. | **Veritabanı kilidi:** `stopaj_odemeleri.sira_no` + UNIQUE `(tenant_id, donem, sira_no)`, migration `20261001073758_Yasal4StopajSiraKilidi`. Sıra kalan hesabından ÖNCE okunur (dönemdeki ödeme sayısı + 1). Bu okumadan sonra işlenen her ödeme o numarayı almış olur; çakışan yazma 1062 ile reddedilir. Hata 409'a çevrilir ("aynı anda başka bir stopaj ödemesi kaydedildi; bu ödeme YAZILMADI"). Kasa satırı ve ödeme AYNI SaveChanges/tx içinde, ikisi birlikte geri alınır. `Math.Max` kaldırıldı: eksi kalan görünür ve ekranda "Fazla ödendi" rozeti çıkar. **Canlıda bulunan ikinci yarış da kapandı:** kiracının ilk ödemesinde "Vergi — stopaj" sistem kategorisi aynı anda iki kez açılmaya çalışılıyordu. Eskiden kullanıcı genel "Bu kayıt zaten var" 409'u görüyordu. Artık çakışan istek diğer isteğin açtığı kategoriyi kullanıyor (`GiderServisi.SistemKategorisi`). | 5 yeni HTTP testi + doğrulayıcının `SINIR_esZamanli…` testi **8/8 + 3/3 tekrar yeşil**. Canlıda 6 eşzamanlı ödeme `[409,200,409,409,409,409]`, `stopaj_odemeleri` 1 satır (sira 1), 1 kasa gider satırı. Mutasyon M13-1/2/3/4 ve M19-1 kırmızı. |
| 2 | ORTA | Gün sonu mutabakatı eski yanlış "7 günlük süre dolmuş" alarmını sürdürüyordu. İptal edilmiş pusula belge sayılıyordu. | `GiderPusulasiSuresi.Durum(...)` = **tek türetme**: Geçerli / YenidenDuzenlenecek / Belgesiz. Hem `SureTakibi` hem `RaporServisi` (mutabakat) bunu kullanır. Geçerli pusulada süre uyarısı yok. İptal edilmiş ve yenisi olmayan pusula: son gün geçtiyse belgesiz, geçmediyse "yeniden düzenlenmeli". `gecikmisPusulaSayisi` alan adı sözleşme için korundu; anlamı yeni tanım. | HTTP `Mutabakat_10_gun_once_GECERLI…`, `Mutabakat_bugun_iptal…`. Canlıda mutabakat (10 gün önce): belgesiz 1, gecikmisPusula 0, "süresi dolmuş" yok. M14-1/M14-2 kırmızı. |
| 3 | ORTA | § 4.1 md.2 (MÜKELLEF, fatura no boş) uygulanmamıştı. | `SureTakibi` yanıtına `faturaBekleyenSayisi` + `faturaBekleyen[]` eklendi. Kapsam: fatura no'su boş, pusulası olmayan, stornolanmamış HURDA_ALIS. **Geri sayım YOK**: fatura süresi teyitsiz (§ 3.2), yalnız bilgi. Gider pusulaları ekranında `#gp-fatura-bekleyen` bilgi satırı (sayı 0 ise satır yok). | HTTP `Sure_takibi_mukellef…`, bUnit `Y4A_mukellef_fatura_bekleyen…`/`…_0_iken_satir_YOK`. Canlı `faturaBekleyenSayisi=1`. M15-1/M18-2 kırmızı. |
| 4 | ORTA | Rapor K-4 "Uyum'da belgesiz alış kaydı" iddiası koda karşılık gelmiyordu. | **Rapor düzeltildi** (§ 1 Y4-A, § 2 S-6). Uyum.razor KESIF-P6 şeridinin dosyası olduğu için (izolasyon notu) Uyum ekranına satır eklenmedi. Belgesiz alış bugün şuralarda görünür: Gider pusulaları (kart + uyarı satırı), Panel (`#panel-pusula-belgesiz` / `#panel-uyum-pusula`), gün sonu mutabakatı (belgesiz sayısı). "Kapatma gerekçeli" ayrı kayıt **uygulanmadı** (açık kalan). | — |
| 5 | DÜŞÜK | KMTS: mevcut kayıt taraması yoktu; içe aktarım tek doğrulayıcıyı atlıyordu. | `KmtsKodu.Tara(...)` (aynı `Uyar` yardımcısı) + `GET /api/v1/kmts/uymayanlar` ([RolGerekli] SAHİP, MÜDÜR; liste en çok 200, tarama sınırı 5000, `sinirdaMi`). `GET /kmts/ayar` yanıtına `uymayanSayisi` eklendi. İşletme → Ayarlar'da `#i-kmts-uymayan` satırı Ürünler'e bağlanır. Desen boşken (bugün) tarama KAPALI. **İçe aktarım:** satır `Uygula` adımında `UrunServisi.Olustur` üzerinden geçer; o da `KmtsKodu.Dogrula(dto.KmtsKodu, yasal)` (UrunModulu.cs:454) çağırır, yani yazma yolu tek doğrulayıcıdan geçiyor. Önizleme (`Dogrula`) adımı DI dışında statik çalıştığı için desene bakmıyor: uymayan satır önizlemede geçerli görünür, uygulamada 400 ile o satır reddedilir (açık kalan). | HTTP `KMTS_mevcut_kayit_taramasi…` (TEZGÂHTAR 403 dahil), bUnit `Y4C_uymayan_KMTS…`. M18-1 kırmızı. |
| 6 | DÜŞÜK | Panel stopaj satırında "Ödendi olarak kaydet" düğmesi yoktu; CSV dışa aktarım jetonu deseni yoktu. | Panel'in iki yerleşiminde de stopaj satırı **tek dokunuş**: `#panel-uyum-stopaj-*-ode` / `#panel-stopaj-gecti-*` bağlantısı `/gider-pusulalari?sekme=stopaj&ode=YYYY-AA` adresine gider ve o dönemin ödeme çekmecesi **açık** gelir. Panel kendisi POST etmez. Kalanı olmayan dönemde çekmece açılmaz. 375 px'te düğme yazısı taşıyordu; `yasal.css`'e `.yasal-eylem { flex: 0 0 auto; white-space: nowrap; }` eklendi (renk yok, belirteç gerekmedi). **CSV jetonu: uygulanmadı** (S-7). | bUnit `Y4B_Panel_tek_dokunus…`, `Y4B_ode_parametresi_kalani_olmayan…`, S31 `Y4B_K8` (href + metin). M16-1/M16-2 kırmızı. Tarayıcı tablosu § 8-C. |
| 7 | DÜŞÜK | Fatura has satırı "Has altın bedeli" gümüş KUYUM kalemlerini de topluyordu. | Altın satırı (`#fd-has-kdv-disi`, kimlik değişmedi) artık yalnız gümüş DIŞI KUYUM kalemlerini toplar. Gümüş kalemlerin has bedeli > 0 ise ayrı satır çıkar: `#fd-gumus-kdv-disi` "Külçe gümüş bedeli (KDV'ye tabi değil)". 0 ise satır yok: gümüş fiyatı yokken fail-closed, KDV dışı iddiası yazılmaz. Baskı şablonu hâlâ yok (S-1). | bUnit `Y4C_K10_gumus_kalem…`, `…gumus_has_bedeli_0…`, `…yalniz_altin…`. Canlıda altın+gümüş fatura: altın satırı 30.177,57 ₺ (gümüş hasDeger 0 eklenmedi). M17-1/M17-2 kırmızı. |

**Ek düzeltme (denetim):** Yeni süre takibi sorgusu `hareketler.fatura_no` okuyordu ve `P-08: FATURA_NO_SERBEST_METIN` 3 > 2 oldu. Mutabakattaki eşdeğer kontrol `Hurda.TedarikciFaturasi.YokMu(...)` yardımcısına alındı (bellek tarafı); SQL tarafı HurdaModulu'nda tek satır. Sayaç 2'ye döndü. Not: denetim.sh yorumundaki "kalan 2 satır" adlarından biri artık RaporModulu değil, HurdaModulu SureTakibi satırı. Yorum orkestratörün dosyasıdır, değiştirilmedi.

## 0c. 4. tur — orkestratör bulgusu (12:35) + doğrulayıcının yeni sınır testi (01.10, 12:33–13:20)

### 0c-1. Panel yasal satırları tam sayfa yüklemede görünmüyordu (orkestratör, 12:35)
**Kök neden (koddan doğrulandı):** `Panel.razor` `OnInitialized` içinde `YukleUyum()` çağrılıyor ve rol kontrolü (`UyumGorebilir` = SAHİP/MÜDÜR/MUHASEBE) o anda yapılıyordu. Tam sayfa yüklemede (F5, adres çubuğundan doğrudan `/`) profil `/auth/ben` → `Oturum.KullaniciAta` ile İLK çizimden SONRA gelir. Rol o an `null` olduğu için MASAK sayaçları, pusula süre satırları, stopaj KRİTİK satırı ve "bu ay kesilen stopaj" HİÇ istenmiyordu. BASIT yüzde yerine "Bekleyen iş yok"/"Başka iş yok" çıkıyordu. Panel `OturumServisi.Degisti`'ye abone değildi. YASAL-1 bandındaki sorunla aynı sınıf. `YasalDegisiklikBandi` bunu abonelikle çözmüştü; aynı desen uygulandı.

**Düzeltme (yalnız `Panel.razor` kod bölümü; işaretleme ve element kimlikleri değişmedi):**
- `Oturum.Degisti += OturumDegisti` (Dispose'da `-=`).
- `uyumBasladi` bayrağı: uyum ve stopaj yüklemesi rol yetkili olduğunda **bir kez** yapılır.
- `ProfilBekleniyor` = oturum jetonu var ama profil yok. Bu durumda iskelet kalır, "iş yok" ya da "rolünüz göremez" yazılmaz.
- Profil TEZGÂHTAR gelirse iskelet kalkar ve rol notu çıkar. Stopaj isteği atılmaz.
- Oturum sonradan yeniden değişirse (jeton yenileme, mod) yükleme tekrarlanmaz ve iskelete dönülmez.

**bUnit (S31PanelSadeTestleri, 3 yeni):**
- `Y4_Tur4_oturum_basta_null_sonra_SAHIP_BASIT_stopaj_KRITIK_satiri_gelir_is_yok_DENMEZ`: orkestratörün istediği "oturum başta null sonra SAHİP" senaryosu. KRİTİK nokta, kasa stopaj satırı, "Bekleyen iş yok" ve "Başka iş yok" metinleri YOK. `/stopaj/ozet` tam 1 kez çağrılır, ikinci `Degisti`'de de 1 kalır.
- `Y4_Tur4_oturum_var_profil_GELMEDEN_iskelet_kalir_MUHASEBE_gelince_PRO_stopaj_satiri`: jeton var, profil yok → iskelet, istek 0. MUHASEBE (PRO) gelince `#panel-stopaj-gecti-2026-08` ve `&ode=2026-08` bağlantısı çıkar.
- `Y4_Tur4_profil_TEZGAHTAR_gelirse_iskelet_kalkar_stopaj_istegi_YOK`.

### 0c-2. Doğrulayıcının yeni testi kırmızıydı: eşzamanlı yeniden düzenleme aynı alış için 2 geçerli pusula yazabiliyordu
3. tur raporundan (11:57) sonra, 12:04'te doğrulayıcı `Dogrulayici/YASAL4SinirTestleri.cs` dosyasına `YASAL4Tur2SinirTestleri.SINIR2_eszamanli_yeniden_duzenle_ayni_alis_icin_EN_COK_bir_gecerli_pusula` testini ekledi. Bu turun ilk filtreli koşusunda test kırmızıydı: `aynı alış için 2 geçerli pusula (stopaj 2× sayılır); durumlar: 409,409,409,409,409,200,200,409`. Doğrulayıcının kendi koşusunda (sinir2.log) yeşil geçmişti; yarışa bağlı, aralıklı bir hata.

**Kök neden:** `HurdaModulu.YenidenDuzenle`, "geçerli pusula var mı" diye okuyup sonra yazıyordu. Bu okuma kilitsiz olduğu için yarışa açıktı.

**Düzeltme:** veritabanı kilidi, migration `20261001093846_Yasal4GecerliPusulaKilidi`.
- Saklanan üretilmiş kolon `gider_pusulalari.gecerli_hareket_id` = `case when iptal_edildi = 0 then hareket_id end`.
- UNIQUE `UQ_gider_pusulalari_tenant_gecerli_hareket (tenant_id, gecerli_hareket_id)`. İptal edilmiş belgede değer NULL olduğu için iptal edilen belge engellemez. Değişmez "bir alış = en çok bir geçerli pusula" artık bütün yazma yolları için veritabanında.
- `YenidenDuzenle` 1062 hatasını yakalar. İki durumda da Türkçe 409 döner ve "bu istek YAZILMADI" der: geçerli pusula çakışması ve aynı anda numara alınması (`pusula_no`). Pusula ve denetim kaydı aynı SaveChanges'tadır; çakışan istekten hiçbir satır kalmaz.
- Migration prosedürü:
  - build YEŞİL → `dotnet ef migrations add` (build'li).
  - Up() okundu: yalnız AddColumn (computed, stored) + CreateIndex(unique).
  - `.cs` 1398 B, `.Designer.cs` 258964 B.
  - `database update` kuyumcu_net'e uygulandı.
  - SQL ile doğrulandı:
    - `__EFMigrationsHistory` son satır `20261001093846_Yasal4GecerliPusulaKilidi`;
    - `show index` UQ 2 kolon;
    - mevcut 5 satırda çift geçerli pusula 0 (migration öncesi kontrol).

**HTTP/DB testleri (Yasal4HttpTestleri, 2 yeni):**
- `Y4A_tur4_ayni_alis_icin_ikinci_GECERLI_pusula_YAZILAMAZ_iptal_edilmis_engellemez`
- `Y4A_tur4_eszamanli_12_yeniden_duzenle_TEK_gecerli_pusula_digerleri_409_Turkce`: tam 1×200; geri kalanların hepsi 409 ve özgül Türkçe mesaj taşır; geçerli 1, toplam 2.

`SINIR2` + 2 yeni test + 3 Panel testi **5 kez art arda koşuldu: 5/5 × 6/6 yeşil**.

### 0c-3. 4. tur mutasyonları (`/private/tmp/kuyumcu-serit/YASAL-4/mut-r4/`; her birinde geri yükleme sonrası `cmp AYNI`)
| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M20-1 | `Panel.razor`: `Oturum.Degisti += OturumDegisti;` silindi | `Y4_Tur4_oturum_basta_null_sonra_SAHIP…`, `Y4_Tur4_oturum_var_profil_GELMEDEN…`, `Y4_Tur4_profil_TEZGAHTAR…` (3/53) |
| M20-2 | `uyumYukleniyor = ProfilBekleniyor;` → `= false;` (profil beklenirken "iş yok" denir) | `Y4_Tur4_oturum_var_profil_GELMEDEN…`, `Y4_Tur4_profil_TEZGAHTAR…` (2/53) |
| M20-3 | tek seferlik koruma (`if (uyumBasladi) return; uyumBasladi = true;`) silindi | İlk denemede **0 kırmızı = TEST AÇIĞI**. Test güçlendirildi (ikinci `Degisti` sonrası istek sayısı 1 kalmalı ve iskelete dönmemeli). Tekrar (M20-3b): `Y4_Tur4_oturum_basta_null_sonra_SAHIP…` kırmızı. |
| M21-1 | migration `unique: true` → `false` | `Y4A_tur4_ayni_alis_icin_ikinci_GECERLI_pusula…`, `Dogrulayici.YASAL4Tur2SinirTestleri.SINIR2_eszamanli…` |
| M21-2 | 409 çevirisinin süzgeci `"gecerli_hareket"` → `"yok_boyle_indeks"` (genel "Bu kayıt zaten var" yoluna düşer) | `Y4A_tur4_eszamanli_12_yeniden_duzenle…` |

Son mutasyondan sonra kalıntı taraması yapıldı (`yok_boyle_indeks` / `unique: false` 0). Ardından TEMİZ `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

### 0c-4. 4. tur canlı doğrulama (API 5160 son kod, `Sistem__Anahtari` ortam değişkeni dosyadan; kiracı `PAKETYASAL4-TEST-T1-R4`, `01M3VDVXGMHEYPQCHBN106ZQCB`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| ESNAF_MUAF hurda alış | 200, stopaj > 0 | 200, GP-2026-000001, stopaj 54,96 |
| Pusula teslimini Ağustos'a taşı (SQL, yalnız test kiracısı) | dönem 2026-08 gecikmiş | `GET /stopaj/ozet` 200 → gecikmis `[["2026-08",54.96,"2026-09-26",true]]` |
| Bu ay ikinci ESNAF alış | buAy > 0 | buAy 54,96 |
| MUHASEBE kullanıcısı (`POST /kullanicilar`) | 201 | 201 rol=MUHASEBE (parola rastgele, dosyada 600; yazılmadı) |
| GP-2026-000002 iptal → **8 eşzamanlı** `POST /yeniden-duzenle` | 1×200, geri kalan 409 | `[409,200,409,409,409,409,409,409]`. Üç mesaj türü: "aynı anda başka bir gider pusulası düzenlendi; bu istek YAZILMADI", "…numara aldı; bu istek YAZILMADI", "geçerli bir gider pusulası zaten var" |
| SQL | geçerli pusula/alış = 1 | GP-…002 `iptal=1, gecerli_hareket_id NULL`; GP-…003 `iptal=0, duzeltilen dolu, gecerli_hareket_id dolu`; ikisi aynı hareket_id; 3. satır yok |

### 0c-5. Gerçek tarayıcı (başsız Chrome, CDP 9716/9717, vekil 5161; tarayıcı bölmesi KULLANILMADI)
Betik `/private/tmp/kuyumcu-serit/YASAL-4/canli-r4/panel-r4.mjs`. Her koşu temiz profille başlar ve sırayla şunları yapar:
1. **Gerçek giriş formu** (`#giris-eposta`/`#giris-sifre`, değerler dosyadan; çıktıya yazılmadı) → ilk Panel.
2. Her mod × tema × genişlik için **F5** (`Page.reload`, önbelleksiz) → ölçüm (`sikisma-olc.js` + `tur/sikisma.js` + `ekstra.js`) + tam sayfa ekran görüntüsü.
3. **Doğrudan `/`** (`Page.navigate`, yeni belge) ×3.

Bekleme süresi 20 sn'dir; bu sürede KRİTİK stopaj satırı aranır.

| Rol | Giriş yolu | BASIT 1280 aç/koyu | BASIT 375 aç/koyu | PRO 1280 aç/koyu | PRO 375 aç/koyu |
|---|---|---|---|---|---|
| SAHİP | form → ilk Panel | satır VAR (PRO yüzü) | — | — | — |
| SAHİP | F5 | VAR / VAR | VAR / VAR | VAR / VAR | VAR / VAR |
| SAHİP | doğrudan `/` ×3 | 3/3 · 3/3 | 2/3* · 3/3 | 3/3 · 3/3 | 3/3 · 3/3 |
| MUHASEBE | form → ilk Panel | VAR (BASIT ve PRO koşularında) | — | — | — |
| MUHASEBE | F5 | VAR / VAR** | VAR / VAR | VAR / VAR | VAR / VAR |
| MUHASEBE | doğrudan `/` ×3 | 3/3 · 3/3 | 3/3 · 3/3 | 3/3 · 3/3 | 3/3 · 3/3 |

Her başarılı hücrede:
- kasa kartında "Bu ay kesilen stopaj (vergi dairesine ait): 54,96 ₺" VAR;
- "Bekleyen iş yok" / "Başka iş yok" **0**;
- `Ödendi olarak kaydet` → `/gider-pusulalari?sekme=stopaj&ode=2026-08`.

\* ve \*\*: o yüklemede sunucu **429 (istek sınırı)** döndü. Sebebi betiğin saniyeler içinde yaptığı onlarca yenilemedir. Ekran durumu dürüstçe gösterdi: "Biraz yoğunluk var…" bandı ve kartların kendi hata bantları (UX-S28); "iş yok" demedi. Görüntüler: `HATA-dogrudan-SAHIP_BASIT_aydinlik_375_3.png`, ilk `panel-F5__MUHASEBE_BASIT_karanlik_1280.png`. Hücre 90 sn sakin bekleyişten sonra yeniden ölçüldü (olcum4): F5 + doğrudan ×3, hepsi VAR, kusur sayaçları 0.

**Ölçüm sayaçları (F5 hücrelerinin hepsi):**
- Şunların hepsi **0**: K1 gizli-görünen, K3 taşan, K3 kırık, K5 TRY, K7 hizasız, K8 jargon, K9 binen, K9b etiket binmesi, K10 üç nokta, K10b kesik, K11 hata bandı, K12 ezik, K13 kırpılan, yatay kaydırma.
- K2 kartSağ %97–98, K4 boyKat 1,0–2,8 (bilgi; kusur sayacı değil).
- Tablolar: `canli-r4/tarayici/olcum2-SAHIP.jsonl`, `olcum2/3/4/6-MUHASEBE.jsonl`.
- Görüntüler: `canli-r4/tarayici/foto/panel-F5__{SAHIP,MUHASEBE}_{BASIT,PRO}_{aydinlik,karanlik}_{1280,375}.png` (16).
- Read ile bakıldı: SAHIP BASIT koyu 375, SAHIP PRO açık 1280, MUHASEBE PRO koyu 375, MUHASEBE BASIT açık 1280, iki 429 görüntüsü. Hepsinde KRİTİK stopaj satırı ve "Ödendi olarak kaydet" görünüyor; taşma yok.

**Not (betik, ürün dışı gözlem):** MUHASEBE koşularında mod anahtarına art arda tıklama bir kez "Profesyonel"e geçmedi (`aria-pressed=false`). API ile aynı geçiş 200 döndü (`PATCH /kullanicilar/ben/arayuz-modu`). Ardından mod sunucuda ayarlanarak PRO hücreleri ölçüldü (olcum6). Sebep büyük olasılıkla aynı 429 dalgası; ama doğrulanmadı. Açık kalanlara yazıldı.

### 0c-6. Tam süit (4. tur, SON kod) ve denetim
- `trx-pYASAL-4-t5`: **8778 toplam, 8772 başarılı, 4 başarısız, 2 atlanan**. 4 kırmızının HİÇBİRİ bu paketin değil:
  - `OrtakGorunumSaflik.O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz`: `ortak.css` içinde `.secici-metin--tam` için `line-clamp: 2; overflow: hidden`. Dosyayı 12:47'de başka şerit değiştirdi (TOPLU-1 Secici bölgesi). Bu şerit ortak.css'e dokunmadı.
  - `Goc47BTestleri` (3 test: `Acilis_bakiyesi_TEK_YAZICIDAN…`, `Ayni_dosya_IKINCI_kez…`, `Geri_al_TERS_KAYITLA…`): test dosyası 12:43'te başka şeritçe (GÖÇ-47B) değiştirildi; o şeridin kiracısı `PAKETGOC-47B-TEST` 13:02'de açıldı. Tek başına koşuda kırmızılar değişken (bir koşuda yalnız `Has_4_ondaligi_asan_satir…`). Testler gider pusulası, hurda ya da Panel kullanmıyor (`grep` 0).
  - FARK (bu paketin ürettiği yeni kırmızı) = **0**.
- `denetim.sh`: tek ✗ yine `P-30 DEMO_INSAN_SURESI`, 3. turla birebir aynı ve orkestratörün/kurucunun. Bu paketin sayaçları ✓:
  - IgnoreQueryFilters 0;
  - ExecuteUpdate beyaz liste 5;
  - Hareketler.Add 1;
  - Pages ModalPencere 48/55;
  - P-08 FATURA_NO_SERBEST_METIN 2/2.

### 0c-7. Temizlik
- API (5160), vekil (5161) ve Chrome (CDP 9716/9717) durduruldu; `lsof` 5160/5161 boş.
- `zsh kiraci-temizle.sh 01M3VDVXGMHEYPQCHBN106ZQCB --uygula` → çıkış 0.
- `tenants where id` 0; `gider_pusulalari` 0; `ad like 'PAKETYASAL4%'` 0.
- Toplam `tenants` = 14. Tur başında 13 idi; aradaki fark başka şeritlerin açtığı `PAKETTOPLU-1-TEST Tur4` (12:52) ve `PAKETGOC-47B-TEST` (13:02) kiracıları. QA-TEST kiracılarına dokunulmadı.
- Yedekler `r4-yedek/`, mutasyon kopyaları `mut-r4/` (geçici dizin).

### 0c-8. Dokunulan dosyalar (4. tur)
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`: yalnız @code. Oturum aboneliği ve tek seferlik uyum yüklemesi eklendi. 2. turda açılan sayfa; gerekçe: orkestratör bulgusu 12:35.
- `dotnet/Kuyumcu.Api/Data/GiderPusulasi.cs`: `GecerliHareketId` üretilmiş kolon.
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`: computed SQL ve UQ indeks.
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`: `YenidenDuzenle` 1062 → 409 ve `BenzersizlikIhlali` yardımcısı.
- `dotnet/Kuyumcu.Api/Data/Migrations/20261001093846_Yasal4GecerliPusulaKilidi.cs` + `.Designer.cs` + `KuyumcuDbContextModelSnapshot.cs`.
- `dotnet/Kuyumcu.Tests/S31PanelSadeTestleri.cs`: 3 test.
- `dotnet/Kuyumcu.Tests/Yasal4HttpTestleri.cs`: 2 test.

## 1. Yapılanlar (şartname § 4)

### Y4-A — Gider pusulası süre anlamı + yeniden düzenleme (§ 4.1)
- `HurdaModulu.cs` → `GiderPusulasiServisi.SureTakibi` yalnız iki küme döndürür:
  - (1) iptal edilmiş ve yerine yenisi düzenlenmemiş pusula. Son gün = **orijinal teslim günü** + `GIDER_PUSULASI_GUN` (teslim gününün değeriyle).
  - (2) son günü geçmiş olan = "belgesiz alış" (gecikmiş).
  - İptal edilmemiş pusula hiçbir zaman gecikmiş sayılmaz. `gecikti/kalanGun` yalnız takip kümesinde döner.
- **Yeni uç `POST /api/v1/gider-pusulalari/{id}/yeniden-duzenle`** ([RolGerekli] hurda yetkili roller).
  - Aynı `hareket_id` ile yeni pusula; `duzeltilen_id` = eski.
  - Tutar, gram, has, ayar ve teslim tarihi eski belgeden gelir.
  - **Yeni hurda hareketi yazılmaz.**
  - Ret durumları: iptal edilmemiş pusula → 409; alış için geçerli pusula zaten varsa → 409; son gün geçtiyse → 409 "7 gün geçti (son gün …); bu alış için artık gider pusulası düzenlenemez. Mali müşavirinize danışın."
- `GiderPusulasiKapisi.cs` → yeniden düzenleme yolu (`HareketId = eski.HareketId`, `DuzeltilenId`).
- `Iptal` yanıtı: `duzeltmeYolu` "Yeniden düzenle" yolunu ve son günü söyler. "YENİ bir hurda alışı girin" cümlesi kalktı.
- `UyumServisi.cs`: süre takibi ve stopaj istemci çağrıları, `SureTakip.FaturaBekleyenSayisi`. **Düzeltme (3. tur, hasım bulgusu 4):** önceki sürüm "Uyum'da belgesiz alış kaydı (K-4)" diyordu, ama kodda böyle bir kayıt yok. Belgesiz alış Gider pusulaları, Panel ve gün sonu mutabakatında görünür. Kapatma gerekçeli ayrı kayıt yok (S-6).
- `Data/GiderPusulasi.cs`: kaynak yorumları birincil URL'lere çevrildi (mevzuat.gov.tr VUK 234, GVK; RG 2009/14592). Migration yok, `duzeltilen_id` zaten vardı.
- `GiderPusulalari.razor`:
  - Kırmızı "gecikmiş" kartının yerine iki kart geldi: "Yeniden düzenlenecek pusula" ve "Pusulası kalmayan alış".
  - İptal edilmiş ve yenisiz satırda birincil eylem "Yeniden düzenle" (`gp-yeniden-{id}`), çekmece formuyla açılır.
  - Belgesiz kalan alış için uyarı bandı.
- `Panel.razor`: yanlış "7 günlük süre DOLDU" KRİTİK satırı kalktı. Yalnız şunlar görünür:
  - `#panel-pusula-yeniden` / `#panel-uyum-pusula-yeniden` (son günle);
  - `#panel-pusula-belgesiz`.

### Y4-C — KDV 23/e görünürlüğü + KMTS (§ 4.3, § 4.4)
- `Faturalar.razor` — **yalnız özel matrah bölümüne** dokunuldu (TOPLU-1 taslak çekmece bölgesine dokunulmadı):
  - Sayfa sonundaki kapalı `Katlanir #fk-ozel-matrah-beyani` kalktı.
  - Kod kapalıyken ve kiracıda KUYUM fatura varken başlık altında tek satır rozet `#fk-ozel-matrah-rozet`: "Özel matrah kodu: mali müşavir onayı bekleniyor". Rozet Yasal sınırlar satırına bağlanır.
  - Hata durumundaki üst kart korundu.
- Fatura kalem penceresinde (faturanın kendisi) KUYUM kalem varsa:
  - `#fd-has-kdv-disi` "Has altın bedeli (KDV'ye tabi değil)";
  - `#fd-kdv-matrahi` "KDV matrahı".
  - Değerler sunucunun kalem satırlarından gelir (OzelMatrahMotoru'nun yazdığı `has_deger` / `kdv_matrah`); yeni formül yok.
- `KdvAyarlari.cs`: kapalıyken `KaynakBeyani` ekrana basılmaz (yalnız sistem ucu/günlük).
- `OzelMatrahMotoru.cs`: yalnız yorum değişti — KDVK 23/e birincil metin URL'si + DOĞRULANDI notu.
- `Sozlukler.Yasal4.cs`: Basit ve Pro aynı sade cümleyi kullanır. K-12 taramasıyla "Kdv:OzelMatrahKodu", "K-10", "805" ve "yapılandırma" ekranda 0.
- KMTS:
  - Kiracı ayarı `kmts_kullanimi` (`kiraci_ayarlari` deposu; yeni tablo yok). Uçlar `GET/PUT /api/v1/kmts/ayar`, PUT yalnız SAHİP. İşletme → Ayarlar sekmesinde `#i-kmts-kullanimi`.
  - Ayar kapalıyken `Urunler.razor` `#u-kmts` ve `#d-kmts` çizilmez. Açıkken yalnız PRO'da "Ek bilgiler" katlanırında (`#u-ek-bilgiler`/`#d-ek-bilgiler`); Basit'te hiç yok. Veri silinmez.
- YENİ `Common/KmtsKodu.cs`: tek doğrulayıcı. `KMTS_KOD_DESENI` (YASAL-1 tablosu, işlem gününün değeri) doluysa uymayan kod 400 döner; boşsa (bugün KAPALI) yalnız uzunluk ve yazdırılabilir karakter denetlenir (bugünkü davranış). `UrunModulu` ve `SatisModulu` aynı yardımcıyı çağırır.
- İpucu jargonsuz: "Rafineriden aldığınız külçe ya da sikkede yazan takip kodu. Yoksa boş bırakın."
- `adr/0006-kmts-kapsam-disi.md`: "Yeniden doğrulandı 2026-10-01" bölümü eklendi (RG 29.07.2026/33324 + KMTS 12.08.2026 duyurusu). Karar değişmedi: perakende kapsam dışı.

### Y4-B — Stopaj borcu (§ 4.2)
- YENİ tablo `stopaj_odemeleri`:
  - `IKiraciVarlik`, append-only.
  - Kolonlar: donem, tutar, odeme_tarihi, hareket_id, kasa_id, tahakkuk_fis_no, kullanici_id, olusturuldu.
  - İndeksler `(tenant_id, donem)` ve UNIQUE `(tenant_id, hareket_id)`.
- YENİ `Modules/Hurda/StopajModulu.cs` (SAHİP, MÜDÜR, MUHASEBE):
  - `GET /stopaj/donemler?yil=` → kesilen, ödenen, kalan, son gün (`STOPAJ_BEYAN_GUNU`, teyit=TEYITSIZ), geçti mi, pusula sayısı. Kesilen = `SUM(stopaj_tutar)`, iptal edilmemiş pusulalar, teslim yerel ayı.
  - `GET /stopaj/ozet` → Panel/kasa için: bu ay kesilen, gecikmiş/yaklaşan dönemler.
  - `POST /stopaj/odemeler` → **mevcut `GiderServisi.Kaydet`** yolunu çağırır (sistem kategorisi "Vergi — stopaj"); ikinci kasa yazma yolu yok. Kalanı aşan tutar 400; kalan 0 iken 409; TL olmayan kasa 400.
  - `POST /stopaj/odemeler/{id}/geri-al` → kasa satırının **stornosu** (48-A motoru); satır silinmez.
  - `GET /stopaj/donemler/{donem}/liste.csv` → pusula no, tarih, satıcı, tam TCKN, brüt, oran, stopaj. **Okuma izi** (`denetim.OkumaKaydet`, hassas) yazılır; TEZGÂHTAR 403.
  - Yazmaların hepsi `DenetimServisi.YazmaKaydet` ile denetime girer.
- YENİ `StopajHatirlatmaIsi` (`STOPAJ_HATIRLATMA`, PAKET-06 çatısı): son güne 5 gün kala ve son gün, kalan > 0 ise SAHİP/MUHASEBE bildirimi.
- `GiderModulu.cs`: sistem kategorisi + çağrılabilir yazma yardımcısı (yeni kasa yolu yok).
- `DuzeltmePolitikasi.cs`: `StopajOdemesi` = STORNO (geri-al ucu).
- `GiderPusulalari.razor`: "Stopaj" sekmesi.
  - Dönem kartı: Kesilen, Ödenen, Kalan, Son gün + "teyit bekliyor", Pusula.
  - Düğmeler "Ödendi" (çekmece) ve "Mali müşavire liste" (CSV).
  - "Ödenmiş aylar" ve "Ödemeler" kapalı akordeon; geri alma ödemeler listesinden.
- `Panel.razor`:
  - Kalanı olan ve son günü geçmiş dönem → KRİTİK `#panel-stopaj-gecti-{donem}`; yaklaşan → uyarı.
  - Kasa kartında "Bu ay kesilen stopaj (vergi dairesine ait): X ₺" (`#panel-soru-kasa-stopaj`). Kasa bakiyesi değişmez.
- `denetim.sh`: `BEKLENEN_VARLIK` +1.

## 2. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | **Fatura baskı şablonu yok** — K-10 "baskı HTML'i" maddesi kalem penceresinde karşılandı | Kod tarandı: `Kuyumcu.Web/Belgeler/` dizini yok. `Bilesenler/Yazdirma/A4Belge.razor` fatura için kullanılmıyor. Faturalar.razor'da yazdır düğmesi ya da fatura belge rotası yok (yalnız `/gider-pusulalari/{Id}/belge`). Uydurma şablon yazılmadı. Has satırı faturanın ekrandaki kalem penceresinde (`#fd-has-kdv-disi`). Fatura baskısı açıldığında aynı satır taşınmalı (açık kalan). |
| S-2 | KMTS ayarı `kiraci_ayarlari` deposunda | Şartname "uygulayıcı mevcut işletme ayarı deposunu seçer" diyor. Yeni tablo/migration açılmadı. |
| S-3 | Stopaj ödemesinin düzeltmesi "geri al" (storno) ucuyla | Şartname "düzeltme = ters kayıt" diyor. Finansal varlıkta hard DELETE yasak; 48-A motoru kullanıldı, storno taklidi yok. |
| S-4 | Panel stopaj satırı "ayın 20'si" yerine "son güne 5 gün kala" (`uyariGunu=5`) | § 4.2 hatırlatma iş kuralıyla aynı tek eşik. İki farklı eşik açılmadı. |
| S-5 | Spesifikasyon mutasyon 9 ve 10 (maden süzgeci, kapanış günü) koşulmadı | Y4-D kapsamı, 30.09'da bitti. Bu şerit Y4-D'ye dokunmaz. |
| S-6 | § 4.1 "Uyum ekranında belgesiz alış kaydı (kapatma gerekçeli)" uygulanmadı | `Uyum.razor` eşzamanlı KESIF-P6 şeridinin dosyası (izolasyon kuralı: başka şeridin bölgesine dokunma). Kapatma gerekçesi yeni bir kalıcı kayıt ister: alan/tablo + PATCH + denetim, yani ayrı DoD. Belgesiz alış bugün Gider pusulaları + Panel + mutabakatta görünür; kapatma akışı açık kalan. |
| S-7 | Stopaj CSV'si "dışa aktarım jetonu (P-14 deseni)" ile bağlanmadı | Bu ağaçta dışa aktarım jetonu yalnız `DisaAktarimModulu`'nde var: SAHİP-only, gerekçe zorunlu, şifreli ZIP, tek kullanımlık jeton. Şartname listeyi MUHASEBE/MÜDÜR'e de açıyor. O akışa bağlamak ya bu rolleri dışarıda bırakır ya da dışa aktarım modülünün güvenlik şartlarını (Mehmet VETO-1) gevşetir. CSV bugün rol korumalı (TEZGÂHTAR 403) ve her indirmede okuma izi yazıyor (KVKK md.12, satır sayısıyla). Jeton akışı ayrı güvenlik kararı: açık kalan. |
| S-8 | Gümüş has satırı yalnız has bedeli > 0 iken çizilir | Gümüş fiyatı yokken motor has bedelini 0 yazar (fail-closed). "KDV'ye tabi değil: 0,00" satırı yanlış bir güvence verir. |

## 3. Migration

- **3. tur:** `20261001073758_Yasal4StopajSiraKilidi`. Build yeşilken `dotnet ef migrations add` (build'li) çalıştırıldı.
  - Up(): eski `IX_stopaj_odemeleri_tenant_id_donem` düşer; `sira_no int NOT NULL DEFAULT 0` eklenir; varsa eski satırlar `ROW_NUMBER() OVER (PARTITION BY tenant_id, donem ORDER BY olusturuldu, id)` ile numaralanır (elle eklenen SQL); UNIQUE `IX_stopaj_odemeleri_tenant_id_donem_sira_no` kurulur.
  - Yalnız stopaj tablosuna dokunur: başka şeridin model değişikliği sızmadı (dosya okundu).
  - `kuyumcu_net`'e uygulandı ("Applying migration '20261001073758_Yasal4StopajSiraKilidi'. Done."). `SHOW INDEX FROM stopaj_odemeleri` → UNIQUE (tenant_id, donem, sira_no) + UNIQUE (tenant_id, hareket_id).

- `20261001030025_Yasal4StopajOdemeleri` (build'li `dotnet ef migrations add`; `--no-build` kullanılmadı).
  - `.cs` ve `.Designer.cs` dolu; Up() `stopaj_odemeleri` tablosunu ve iki indeksi kurar.
  - `kuyumcu_net`'e uygulandı (`ef2.log`: "Applying migration '20261001030025_Yasal4StopajOdemeleri'. Done.").
  - `__EFMigrationsHistory`'de en son satır bu.
  - `SHOW CREATE TABLE stopaj_odemeleri` doğrulandı: `IX_stopaj_odemeleri_tenant_id_hareket_id` UNIQUE, `IX_stopaj_odemeleri_tenant_id_donem`.

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| 1. tur tam süit (07:00, eski oturum) | 8547 | 8542 | 5 | — |
| 2. tur filtreli (Yasal4, S31, Fatura, DuzeltmePolitikasi, AcikKalanlar, P10, S33, S3b, ZoBelge, Gider, Hurda, GiderPusulasi, OzelMatrah, Kmts) | 533 | 533 | 0 | 0 |
| 2. tur Panel filtreli (Panel düzeltmesi sonrası) | 138 | 138 | 0 | 0 |
| 2. tur tam süit (son kod) — `trx-pYASAL-4-t2` | 8557 | 8556 | 0 | 1 |
| 3. tur filtreli (Yasal4, YASAL4 doğrulayıcı, S31, FaturaEkran, Gider, Rapor, Hurda, Q103, DuzeltmePolitikasi…) | 848 | 846 | 2* | 0 |
| 3. tur ara tam süit (`trx-pYASAL-4-t3`, son düzeltmelerden önce) | 8730 | 8726 | 3 (başkasının) | 1 |
| **3. tur tam süit (SON kod)** — `trx-pYASAL-4-t4` | **8740** | **8739** | **0** | 1 |
| 3. tur son filtreli (Panel + Yasal4 + YASAL4, `.yasal-eylem` sınıfından sonra) | 204 | 204 | 0 | 0 |

\* 848'lik filtreli koşudaki 2 kırmızı: (a) `OrtakGorunumKiyasSaflik.KY8_…`, Kullanicilar.razor'a ait (KESIF-P6 şeridi); (b) bizim `Stopaj_eszamanli_12…`. (b) yeni güçlendirilmiş iddia sayesinde kategori yarışını yakaladı, düzeltildi; sonra 3/3 yeşil. Ara tam süitteki 3 kırmızı başka şeritlerin: `FaturaFistenTestleri.Ayni_fisten_ikinci_fatura…` ve `FaturaKalemTestleri.Ayni_fisten_ikinci_kalemli…` (FaturaModulu.cs 11:23'te başka şerit değiştirdi; bu şerit FaturaModulu'na dokunmadı), `AuthSifirlaEkranTestleri.Jeton_reddedilirse…` (AUTH-SIFIRLA şeridi). Son tam süitte üçü de yeşil.

- **Not (dürüst sınır):** Son tam süitten SONRA yalnız şu değişti: Panel düğmesinin sınıfı (`hucre-tek-satir` → `yasal-eylem`) ve `yasal.css`'e tek kural. C# mantığı değişmedi. Panel + Yasal4 filtreli koşu (204/204) bundan sonra yeniden koşuldu.

- Atlanan test bu paketin değil: `SATIS3EkranTestleri.DOG_T24_SONDA_…`.
- **3. turda yeni testler: 19.**
  - HTTP +9: `Stopaj_ardisik…sira_1_ve_2`, `Stopaj_ayni_donemde_ayni_sira…veritabani_kilidi`, `Stopaj_sira_cakismasi_409…`, `Stopaj_eszamanli_12…`, `Stopaj_fazla_odeme_kalani_EKSI…`, `Mutabakat_10_gun_once…`, `Mutabakat_bugun_iptal…`, `Sure_takibi_mukellef…`, `KMTS_mevcut_kayit_taramasi…`.
  - bUnit +10: `Yasal4PusulaEkranTestleri` +5 (ode parametresi ×2, fazla ödeme, fatura bekleyen ×2); `Yasal4IsletmeKmtsEkranTestleri` +2; `FaturaEkranTestleri` +3 (gümüş satırı, gümüş 0, yalnız altın).
  - `S31PanelSadeTestleri.Y4B_K8` iddia eklendi (ödeme bağlantısı href + metin).
- **1.–2. turda yeni testler: 35.**
  - `Yasal4HttpTestleri` 13: K1, K2, K3, K4, K5, K6/K7, K9, K14 …
  - `Yasal4SafTestleri` 4.
  - `Yasal4EkranTestleri` 9 (`Yasal4PusulaEkranTestleri` + `Yasal4UrunKmtsEkranTestleri`).
  - `S31PanelSadeTestleri` +5 (Y4A_K1, Y4A uyarı, Y4B_K8 ×2, …).
  - `FaturaEkranTestleri` +4 (Y4C_K10, Y4C_K11, Y4C_K12 Basit/Pro).
- Mevcut test dosyalarında yeni yerleşime uyarlama yapıldı: AcikKalanlar, P10, S33, S3b, ZoBelge (test sayıları değişmedi).

## 5. Mutasyon tablosu (2. ve 3. tur; her biri geri alındı, `cmp` AYNI)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | Süre takibinde "yenisi düzenlenmiş" süzgeci kaldırıldı | Yasal4HttpTestleri.K3_yeniden_duzenle_ayni_hareket_… | AYNI |
| M1-2 (şartname 1) | Her pusula takipte sayıldı (eski anlam) | Yasal4HttpTestleri.K1_on_gun_once_duzenlenmis_iptal_edilmemis_… | AYNI |
| M2-1 (şartname 2) | Yeniden düzenleme yeni hareket kimliği yazdı | Yasal4HttpTestleri.K3_… (ilk koşumda başka şeridin build'i dll'yi sildi; tekrar koşuldu → KIRMIZI) | AYNI |
| M3-1 (şartname 3) | Son gün kontrolü silindi | Yasal4HttpTestleri.K4_teslimden_9_gun_sonra_409_… | AYNI |
| M3-2 (şartname 3) | Son gün bugünden hesaplandı | Yasal4SafTestleri.Gider_pusulasi_son_gunu_TESLIM_GUNUNUN_…, Yasal4HttpTestleri.K2_… | AYNI |
| M4-1 (şartname 4) | Stopaj toplamına iptal edilenler katıldı | Yasal4HttpTestleri.K6_K7_stopaj_donemi_… | AYNI |
| M5-1 (şartname 5) | StopajModulu'na `db.Hareketler.Add(...)` eklendi | `denetim.sh`: "✗ Hareketler.Add (yalnız HareketFabrikasi) 2 (beklenen 1)" | AYNI |
| M6-1 (şartname 6) | Fatura has satırı kaldırıldı | FaturaEkranTestleri.Y4C_K10_KUYUM_faturada_has_bedeli_… | AYNI |
| M7-1 (şartname 7) | Katlanır geri eklendi | FaturaEkranTestleri.Y4C_K11_…, FaturaEkranTestleri.Y40_teyit_yokken_… | AYNI |
| M8-1 (şartname 8) | KMTS alanı ayara bakılmadan çizildi | Yasal4UrunKmtsEkranTestleri.Y4C_K13_ayar_kapali_PRO_formda_KMTS_alani_YOK | AYNI |
| M9-1 | Satış yolunda KMTS doğrulaması atlandı | Yasal4HttpTestleri.K14_KMTS_deseni_doluyken_… | AYNI |
| M10-1 | CSV okuma izi kaldırıldı | Yasal4HttpTestleri.K9_csv_satir_sayisi_… | AYNI |
| M11-1 | Panel pusula satırı sayı 0 iken de çizildi | S31PanelSadeTestleri.Y4A_K1_iptal_edilmemis_eski_pusula_… | AYNI |
| M12-1 | Panel stopaj KRİTİK satırı hiç çizilmedi | S31PanelSadeTestleri.Y4B_K8_kalan_var_son_gun_gecti_KRITIK_… | AYNI |

| **3. tur** (`/private/tmp/kuyumcu-serit/YASAL-4/mut-r3/sonuc.txt`) | | | |
| M13-1 | Ödeme sıra numarası atanmadı (hep 0) | Yasal4HttpTestleri.Stopaj_ardisik_iki_kismi_odeme_sira_1_ve_2…, Stopaj_sira_cakismasi_409… | AYNI |
| M13-2 | Migration'da `(tenant, donem, sira_no)` UNIQUE değil | Stopaj_ayni_donemde_ayni_sira…veritabani_kilidi, Stopaj_eszamanli_12…, Stopaj_sira_cakismasi_409… | AYNI |
| M13-3 | `Math.Max(0, …)` geri kondu | Stopaj_fazla_odeme_kalani_EKSI_gosterir_sifira_kirpilmaz | AYNI |
| M13-4 | Sıra çakışması yakalanmadı (genel 409'a düştü) | Stopaj_sira_cakismasi_409_Turkce_mesaj… | AYNI |
| M19-1 | Sistem kategorisi yarış yakalaması kapatıldı | Stopaj_eszamanli_12_tam_odeme… (genel "kayıt zaten var" mesajı) | AYNI |
| M14-1 | Mutabakat eski `DuzenlemeSonTarihi < şimdi` alarmına döndü | Mutabakat_bugun_iptal_edilen_yenisiz… | AYNI |
| M14-2 | `Durum`: iptal edilmiş pusula da belge sayıldı | K4_teslimden_9_gun…, Mutabakat_10_gun_once…, Mutabakat_bugun_iptal… | AYNI |
| M15-1 | Fatura bekleyen sorgusundan fatura no süzgeci kaldırıldı | Sure_takibi_mukellef_alisi_fatura_no_bossa… | AYNI |
| M16-1 | Panel ödeme bağlantısı yalnız sekmeye gitti (`ode=` yok) | S31PanelSadeTestleri.Y4B_K8_kalan_var_son_gun_gecti… | AYNI |
| M16-2 | `?ode=` parametresi çekmeceyi açmadı | Yasal4PusulaEkranTestleri.Y4B_Panel_tek_dokunus… | AYNI |
| M17-1 | Altın has satırı gümüşü de topladı | FaturaEkranTestleri.Y4C_K10_gumus_kalem_HAS_ALTIN_satirina_girmez… | AYNI |
| M17-2 | Gümüş satırı has 0 iken de çizildi | Y4C_K10_gumus_has_bedeli_0…, Y4C_K10_yalniz_altin_faturada_gumus_satiri_YOK | AYNI |
| M18-1 | KMTS taraması uyanları listeledi (ters koşul) | Yasal4HttpTestleri.KMTS_mevcut_kayit_taramasi… | AYNI |
| M18-2 | Fatura bekleyen satırı hiç çizilmedi | Yasal4PusulaEkranTestleri.Y4A_mukellef_fatura_bekleyen… | AYNI |

- 1.–2. tur: 14/14; 3. tur: 14/14 mutasyon kırmızı (M13-2 ve M13-4'ün ilk koşumu başka şeridin eşzamanlı derlemesinin `Kuyumcu.Web.wasm` dosya kilidiyle derlenemedi, test koşmadı; aynı mutasyon yeniden koşuldu → KIRMIZI). M13-4 ilk biçimiyle hiçbir testi kırmıyordu (genel 409'a da düşse durum 409 kalıyordu): **test açığıydı**. Deterministik çakışma testi eklendi (`Stopaj_sira_cakismasi_409…`), sıra "en büyük + 1"den "sayı + 1"e alındı (numaralar kesintisiz doğduğu için eşdeğer). Tekrarında kırmızı.
- Son mutasyondan sonra temiz build: 0 uyarı, 0 hata.
- Kanıt dosyaları: `mut/log-r2.txt`, `mut/sonuc-hepsi-r2.json`, `mut/sonuc-M2-1.json`, `denetim-M5.log`.

## 6. Denetim (`dotnet/denetim.sh`, son kod)
- Bu paketin sayaçları:
  - ✓ `Hareketler.Add (yalnız HareketFabrikasi)` 1.
  - ✓ `IKiraciVarlik uygulayan varlık` 75.
  - ✓ `Pages: ModalPencere kullanımı` 48 (tavan 55; artış yok).
  - ✓ `P-26: OZEL_MATRAH_BEYANSIZ_EKRAN` 0 (nüfus 1).
  - ✓ `P-26: OZEL_MATRAH_KODU_GOMULU` 0.
  - ✓ `P-26: OZEL_MATRAH_TEYITSIZ_ACIK` 0.
  - ✓ `P-04: SUBE_VARLIK_KAPSAM`.
- **3. tur son koşum** (`/private/tmp/kuyumcu-serit/YASAL-4/denetim-r3.log`): ✓ Hareketler.Add 1 · ✓ IKiraciVarlik 76 · ✓ ExecuteUpdate/Delete 5 · ✓ ModalPencere 48/55 · ✓ P-26 ×3 · ✓ **P-08 FATURA_NO_SERBEST_METIN 2** (ara koşumda 3 > 2 idi, § 0b ek düzeltme). Tek ✗ yine P-30 (aşağıda).
- Tek ✗ bu paketin değil: **`P-30: DEMO_INSAN_SURESI (BK-45)`**. Tarih tabanlı bir kural: demo prova dosyalarında kronometreli insan süresi yok, son tarih 2026-09-30 geçti. Kod değişikliğiyle ilgisi yok; sahibi orkestratör/kurucu (demo provası). Sonuç satırı "DENETİM BAŞARISIZ".

## 7. Canlı doğrulama (API 5160, kiracı PAKETYASAL4-TEST-T1)

1. tur betiği `canli/canli.mjs` (çıktı `canli/canli.log`). 2. turda temiz build ile API yeniden başlatıldı ve `canli/r2.mjs` ile yeniden kontrol edildi.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Hurda alış NIHAI_TUKETICI | 200 + pusula | 200, GP-2026-000001, stopaj 0 |
| sure-takibi (yeni pusula) | gecikmiş 0, yaklaşan 0 (K-1) | 0 / 0 |
| yeniden-duzenle (iptal edilmemiş) | 409 (K-5) | 409 "Bu gider pusulası geçerli; yeniden düzenlenemez…" |
| iptal | 200 + "Yeniden düzenle" yolu | 200, duzeltmeYolu "Aynı alış için 'Yeniden düzenle' … Yeni hurda alışı GİRMEYİN…", sonGun 2026-10-08 |
| sure-takibi (iptal sonrası) | yaklaşan 1, son gün teslim+7 (K-2) | yaklaşan 1, 2026-10-08 |
| yeniden-duzenle (iptalli) | 200, aynı hareket, duzeltilenId = eski (K-3) | 200, GP-2026-000002, duzeltilenId=true, hareketAyni=true |
| yeniden-duzenle (2. kez) | 409 | 409 "…geçerli bir gider pusulası zaten var…" |
| yeniden-duzenle P2 (teslim 9 gün önce, SQL ile geri alındı) | 409, belgesiz 1 (K-4) | 409 "7 gün geçti (son gün 29.09.2026)…", sure-takibi gecikmiş(belgesiz)=1 |
| KMTS ayar GET / PUT | kapalı → açık | 200 `{"acik":false}` → 200 `{"acik":true}` |
| Ürün (KMTS serbest, desen boş) | 201 | 201 kmts=RAF-2026-0001 |
| KUYUM fatura | 200 + has satırı | 200; kalem hasDeger 30257.5, kdvMatrah 9785.42, kdvTutar 1957.08, satır 42000 |
| ozel-matrah-ayari | kuyumFaturaVar true, kod null | aynen (rozet koşulu) |
| stopaj/donemler | Ekim kesilen 54,96; Ağustos geçti | `2026-10` kesilen 54.96, son gün 2026-11-26 TEYITSIZ; `2026-08` gecti=true |
| Fazla tutar ödeme | 400 | 400 "…en çok 54,96 TL ödenebilir." |
| Ödeme 2026-08 | 200, kalan 0 (K-7) | 200, tutar 54.96, kalan 0, kasa "Ana kasa" |
| Tekrar ödeme | 409 | 409 "…ödenecek stopaj yok (kalan 0,00 TL)." |
| Geri al → yeniden ödeme | 200 / 200 | geriAlindi=true; yeniden ödeme 200 kalan 0 |
| liste.csv | 200 text/csv, satır = pusula sayısı (K-9) | 200 `text/csv; charset=utf-8`, 1 satır, başlık `pusula_no;tarih;satici_ad_soyad;tckn;brut_tl;stopaj_oran;stopaj_tl` |
| 2. tur — sure-takibi / donemler / ozet / ozel-matrah-ayari | 200 | 200 / 200 / 200 / 200 (aynı değerler; Ağustos ödenen 54.96 kalan 0) |

**SQL kanıtı** (kuyumcu_net, kiracı silinmeden önce):
```
gider_pusulalari:
  GP-2026-000001 NIHAI_TUKETICI hareket …GFG2F duzeltilen=0 iptal=1
  GP-2026-000002 NIHAI_TUKETICI hareket …GFG2F duzeltilen=1 iptal=0     ← aynı hareket (K-3)
  GP-2026-000003 iptal=1 teslim 2026-09-22                              ← süresi geçmiş, belgesiz
  GP-2026-000004 ESNAF_MUAF stopaj 54.96 teslim 2026-08-15
  GP-2026-000005 ESNAF_MUAF stopaj 54.96 teslim 2026-10-01
hareketler: HURDA_ALIS 4 satır (5 pusula / 4 alış → yeniden düzenleme hareket YAZMADI)
stopaj_odemeleri: 2026-08 54.96 (geri alınan) · 2026-08 54.96 (geçerli) — append-only
kasa_hareketleri: -54.96 "Stopaj ödemesi … tahakkuk TF-CANLI-1" [Vergi — stopaj]
                  +54.96 "STORNO: …" storno_kaynak_id dolu
                  -54.96 "Stopaj ödemesi … dönem 2026-08" [Vergi — stopaj]
denetim_kayitlari: EKLEME StopajOdemesi 5 · OKUMA GiderPusulasi (stopaj CSV) 1 · EKLEME KiraciAyari (kmts) 1
```

### 7-C. 3. tur canlı doğrulama (API 5160 son kodla; kiracı PAKETYASAL4-TEST-T1-R3, `01M3V8G45MVC87BVG89MV6X9P7`)

Betikler `/private/tmp/kuyumcu-serit/YASAL-4/canli-r3/{a,a2,b,c}.mjs`, çıktılar `a.log, a2.log, b.log, c.log, sql.log`. Senaryo tarihleri SQL ile kaydırıldı: Temmuz ve Ağustos ESNAF pusulaları; iki NIHAI alış 10 gün önceye; MÜKELLEF alışın fatura no'su NULL.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Dönemler | 2026-07 ve 2026-08 kesilen 54,96 | `2026-07 kesilen=54.96 kalan=54.96 · 2026-08 kesilen=54.96 kalan=54.96 gecti=true` |
| **6 eşzamanlı tam ödeme (2026-07)** | 1×200, gerisi 409; tek ödeme | `[409,200,409,409,409,409]`; sonra `odenen=54.96 kalan=0 odeme sayısı=1` |
| 409 mesajları | Türkçe, sebebini söyler | 1 adet "…aynı anda başka bir stopaj ödemesi kaydedildi; bu ödeme YAZILMADI…". 4 adet genel "Bu kayıt zaten var" (günlük: `IX_gider_kategorileri_tenant_id_ad`, ilk ödemede kategori yarışı) → **düzeltildi** (`SistemKategorisi`), testle kilitlendi (M19-1) |
| Ödeme sonrası Temmuz pusulası iptal | kalan EKSİ görünür | `kesilen=0 odenen=54.96 kalan=-54.96` |
| stopaj/ozet | Ağustos gecikmiş | `gecikmis=[["2026-08",54.96]]` |
| sure-takibi | faturaBekleyen 1, son gün YOK | `faturaBekleyenSayisi=1 faturaBekleyen=[{"hareketId":…,"tarih":…,"tutarTl":2748}]` (sonGun alanı yok) |
| Mutabakat (10 gün önce: 1 geçerli + 1 iptal-yenisiz pusula) | belgesiz 1, "süresi dolmuş" yok | `belgesiz=1 gecikmisPusula=0 uyarilar=["1 hurda alımının belgesi yok (ne geçerli gider pusulası ne tedarikçi faturası)"]` (son kodla tekrar: aynı) |
| kmts/uymayanlar, kmts/ayar | desen boş → tarama kapalı | `{"bicimDenetimi":false,"sayi":0,…,"sinir":5000}` / `{"acik":false,"bicimDenetimi":false,"uymayanSayisi":0}` |
| Altın + gümüş KUYUM fatura | altın satırı yalnız altın | kalemler `ALTIN hasDeger 30177.57` · `GUMUS hasDeger 0`; ekranda "Has altın bedeli 30.177,57 ₺", gümüş satırı yok (S-8) |
| Kalanı eksi dönemde yeni ödeme | 409 | 409 "2026-07 döneminde ödenecek stopaj yok (kalan 0,00 TL)." (metin eksi kalanı 0,00 yazıyor: açık kalan 6) |

**SQL kanıtı** (kiracı silinmeden önce, `sql.log`):
```
stopaj_odemeleri   2026-07  sira_no=1  54.96          ← 6 eşzamanlı istekten TEK satır
kasa_gider         -54.96  "Stopaj ödemesi · dönem 2026-07"  [Vergi — stopaj]   ← tek kasa çıkışı
kategori           "Vergi — stopaj"  1                ← yarışa rağmen tek kategori
pusula GP-2026-000001 ESNAF  iptal=1 2026-07-15 54.96 (ödeme sonrası iptal → fazla ödeme)
pusula GP-2026-000002 ESNAF  iptal=0 2026-08-15 54.96
pusula GP-2026-000003 NIHAI  iptal=0 2026-09-21        ← geçerli, alarm YOK
pusula GP-2026-000004 NIHAI  iptal=1 2026-09-21        ← belgesiz (son gün 28.09 geçti)
denetim  Gider EKLEME 1 · GiderKategorisi EKLEME 2 · StopajOdemesi EKLEME 2
         (ikişer satır: servis YazmaKaydet + yazma denetçisi; varlık kimlikleri tek — başarısız isteklerden iz YOK)
```

## 8. Gerçek tarayıcı (başsız Chrome, CDP; vekil 5161; tarayıcı bölmesi KULLANILMADI)

**A) Tam tur** — `tur.mjs`, 4 işçi; rotalar `/`, `/gider-pusulalari`, `/gider-pusulalari?sekme=stopaj`, `/faturalar`, `/urunler`, `/isletme` × 1280/375 × BASIT/PRO × açık/koyu = 48 sayfa ölçümü + açıcılar. Ölçüm `tur-r2/olcum-*.jsonl`, özet `olcum-r2-ozet.txt`, 328 görüntü `tur-r2/foto/`. Her sayfada `modGercek` = istenen mod.

| Rota | 1280 B açık | 1280 B koyu | 1280 P açık | 1280 P koyu | 375 B açık | 375 B koyu | 375 P açık | 375 P koyu |
|---|---|---|---|---|---|---|---|---|
| / (Panel) | 0 | 0 | 0 | 0 | 0 | 0 | 0 (1. turda kirik 1 → düzeltildi) | 0 (aynı) |
| /gider-pusulalari | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /gider-pusulalari?sekme=stopaj | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /faturalar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /urunler | ezik 1* | ezik 1* | ezik 1* | ezik 1* | 0 | 0 | 0 | 0 |
| /isletme | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- Sayılan ölçütler: taşan, kırık kelime, üst üste binen, yatay kaydırma, `[hidden]` görünen, TRY kalıbı, elipsis, kırpılan, etiket binmesi. Tümü 0. K2 (ana alan genişliği) %97–98.
- \* `#urun-durum-filtre "Hepsi" w=84` paket öncesinden var (`yedek/Urunler.razor`'da da aynı öğe), KMTS değişikliğiyle ilgisi yok.

**B) Hedefli ölçüm** — `hedef/yasal4-hedef.mjs` ile `__olc`, `__ek` ölçülüp görüntü alındı (`hedef/olcum.jsonl`, 24 görüntü).

| Senaryo | BASIT (açık/koyu, 1280/375) | PRO (açık/koyu, 1280/375) |
|---|---|---|
| Fatura kalem penceresi `#fd-has-kdv-disi` | VAR — "Has altın bedeli (KDV'ye tabi değil) 30.257,50 ₺" (motor değeri, fark 0,00) | VAR |
| Ürün düzenle `#d-kmts` (ayar AÇIK) | **YOK** (K-13: Basit'te 0) | VAR, "Ek bilgiler" katlanırında |
| İşletme → Ayarlar `#i-kmts-kullanimi` | VAR, taşma 0 | VAR, taşma 0 |

- Pencere/çekmece açıkken ölçer `binen` 5–6, `etiketBin` 1, `ezik` 1–2 verdi. Sayılan çiftlerin **hepsi arka plan sayfa öğesi ⟂ üstteki pencere öğesi** (ör. `#fk-yeni "+ Yeni Fatura" ⟂ button.modal-kapat`, `label "Maden" (liste süzgeci) ⟂ "KMTS kodu" (çekmece)`). Bu bindirme katmanının ölçer yan etkisi; pencere içinde iki öğenin çakıştığı tek bir çift yok.
- `#belge-sec w=1` gizli dosya girdisi.
- Görüntüler Read ile tek tek bakılarak kontrol edildi: `kok__PRO_karanlik_375.png` (bağlantı tek satır), `gider_pusulalari__PRO_aydinlik_1280.png`, `gider_pusulalari_sekme_stopaj__BASIT_aydinlik_1280.png`, `faturalar__PRO_karanlik_1280.png` (rozet), `fatura-has__PRO_aydinlik_375.png`, `fatura-has__BASIT_karanlik_1280.png`, `urun-kmts__PRO_aydinlik_375.png`, `isletme-kmts__BASIT_karanlik_375.png`.

**C) 3. tur hedefli ölçüm.** Betik `hedef-r3/hedef.mjs`, CDP 9715–9718, çıktı `hedef-r3/olcum.jsonl` ve 48 görüntü. Her senaryo bütün sayfayı `__olc` + `__ek` ile ölçtü: 6 senaryo × 1280/375 × BASIT/PRO × açık/koyu = 48 ölçüm.

| Senaryo (rota) | Hedef öğe | 1280 B açık/koyu | 1280 P açık/koyu | 375 B açık/koyu | 375 P açık/koyu |
|---|---|---|---|---|---|
| Panel `/` | `#panel-uyum-stopaj-gecti-2026-08-ode` (B) / `#panel-stopaj-gecti-2026-08` (P) | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 |
| `/gider-pusulalari` | `#gp-fatura-bekleyen` | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 |
| `?sekme=stopaj` | `#sp-fazla-2026-07` "Fazla ödendi" | VAR · kirik 1† | VAR · kirik 1† | VAR · kirik 1† | VAR · kirik 1† |
| `?sekme=stopaj&ode=2026-08` | `#sp-od-form` (çekmece açık geldi) | VAR · kirik 1† binen 4‡ | VAR · kirik 1† binen 4‡ | VAR · kirik 1† binen 2‡ | VAR · kirik 1† binen 1‡ |
| `/faturalar` → kalem penceresi | `#fd-has-kdv-disi` "Has altın bedeli 30.177,57 ₺" | VAR · binen 6‡ | VAR · binen 6‡ | VAR · binen 6‡ | VAR · binen 6‡ |
| `/isletme` → Ayarlar | `#i-kmts-uymayan` (3 ürün) § | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 | VAR · 0/0 |

- Bütün ölçümlerde taşan 0, yatay kaydırma 0, `[hidden]` görünen 0, TRY kalıbı 0.
- **Bu turda bulunan ve düzeltilen:**
  - 375 px Basit Panel'de "Ödendi olarak kaydet" önce iki satıra kırıldı (kirik 1). `hucre-tek-satir` verilince bu kez düğme kutusundan taştı (tasan 1). Düğme esnek satırda büzülüyordu: `.yasal-eylem { flex: 0 0 auto; white-space: nowrap }` → 0/0.
  - 1280'de Panel satırı ilk ölçümde YOK çıktı: senaryo girişin hemen ardından koşuyordu ve özet daha yüklenmemişti (ekranda "Başka iş yok"). Senaryo sona alındı ve öğe beklendi; 1280 B/P açık/koyu dört ölçümün dördünde de VAR.
- † `#gp-sekme-stopaj "Stopaj1"`: ortak `Sekmeler.razor`'ın sayı rozeti. Ölçer, rozet hapının yüksekliği yüzünden düğmeyi "iki satır" sayıyor. Görüntüde etiket ve rozet tek satırda (`stopaj-fazla__PRO_karanlik_375.png`). Rozet kalanı olan geciken dönem varken çıkar; 2. turda Ağustos ödenmiş olduğu için görünmemişti. Bileşen ortak (bu şeridin değil); Uyum sekmelerinde de aynı.
- ‡ Pencere ya da çekmece açıkken arka plan öğesi ⟂ katman öğesi çiftleri (ör. `#gp-hurda-alimi "Hurda alımı gir" ⟂ #sp-od-kalan`, `#fk-yeni ⟂ #fd-kalem-ayar-…`). Pencere içinde çakışan çift yok. 2. turdaki ölçer yan etkisiyle aynı.
- § Ortak `kuyumcu_net`'te yasal desen (sistem geneli) AÇILMADI; 5199 inceleme sunucusu da aynı tabloyu okuyor. Satırın düzenini ölçmek için yalnız bu ölçüm sekmesinde `/kmts/ayar` yanıtı tarayıcıda `uymayanSayisi=3` olarak değiştirildi (fetch sarmalı). Sunucu tarafı HTTP testinde gerçek desenle sınandı.
- Read ile bakılan görüntüler: `panel-stopaj-ode__BASIT_karanlik_375.png` (düğme tek satır, taşma yok), `panel-stopaj-ode__PRO_aydinlik_1280.png`, `stopaj-ode-cekmece__BASIT_aydinlik_375.png` (Ağustos, kalan 54,96 ₺ dolu, tek kasa satırı), `stopaj-fazla__PRO_karanlik_375.png` ("−54,96 ₺ Fazla ödendi" rozeti, Ödendi düğmesi yok), `pusula-fatura-bekleyen__PRO_aydinlik_1280.png`, `isletme-kmts-uymayan__PRO_karanlik_375.png`, `fatura-has-altin__PRO_aydinlik_375.png` (30.177,57 ₺, gümüş satırı yok).

## 9. DoD sözleşmesi

| Varlık / kayıt | Uç | Ekran |
|---|---|---|
| `StopajOdemesi` (YENİ, finansal, append-only) | POST oluştur · POST `{id}/geri-al` = kasa stornosu (48-A). PATCH yok: kasa çıkışının belgesi, yerinde düzenlenmez; hard DELETE yasak | "Ödendi" çekmecesi; "Ödemeler" akordeonunda "Geri al" (onay modalı). DuzeltmePolitikasi = STORNO |
| `GiderPusulasi` (mevcut) | Mevcut PATCH/iptal + YENİ `yeniden-duzenle` | "Yeniden düzenle" birincil eylemi; iptal onayı mevcut |
| KMTS ayarı (`kiraci_ayarlari`) | GET/PUT `/kmts/ayar` (PUT yalnız SAHİP; açılıp kapatılabilir, denetimli) | İşletme → Ayarlar onay kutusu; SAHİP değilse disabled + sebep |
| Stopaj dönem satırı | Türetilmiş (yeni kayıt değil) — salt okunur | Panel'den tek dokunuşla o dönemin ödeme çekmecesi |
| `stopaj_odemeleri.sira_no` (3. tur) | Sunucu atar, düzenlenmez (eşzamanlılık kilidi); geri alma = storno, sıra korunur | — |
| KMTS uymayan kod taraması (3. tur) | `GET /kmts/uymayanlar` (SAHİP, MÜDÜR), salt okunur; düzeltme ürünün kendi PATCH'i | İşletme → Ayarlar sayısı → Ürünler |
| Borsa kapanışı (Y4-D) | Bu şeridin kapsamı değil | — |

## 10. Değişen dosyalar (bu paket; `git status --porcelain | grep -v -E "/(bin|obj)/"` içinden)

- **YENİ:**
  - `Kuyumcu.Api/Data/StopajOdemesi.cs`
  - `Kuyumcu.Api/Modules/Hurda/StopajModulu.cs`
  - `Kuyumcu.Api/Modules/Isler/Isler/StopajHatirlatmaIsi.cs`
  - `Kuyumcu.Api/Common/KmtsKodu.cs`
  - `Kuyumcu.Contracts/Sayfalar/StopajSozlesmeleri.cs`
  - `Kuyumcu.Core/I18n/Sozlukler.Yasal4.cs`
  - `Kuyumcu.Api/Data/Migrations/20261001030025_Yasal4StopajOdemeleri{,.Designer}.cs`
  - `Kuyumcu.Tests/Yasal4HttpTestleri.cs`, `Yasal4SafTestleri.cs`, `Yasal4EkranTestleri.cs`
- **DEĞİŞEN:**
  - Api: `Modules/Hurda/HurdaModulu.cs`, `Modules/Hurda/GiderPusulasiKapisi.cs`, `Modules/Gider/GiderModulu.cs`, `Modules/Fatura/KdvAyarlari.cs`, `Modules/Fatura/OzelMatrahMotoru.cs` (yorum), `Modules/Urun/UrunModulu.cs` (KMTS satırları), `Modules/Satis/SatisModulu.cs` (KMTS satırları), `Modules/Meta/DuzeltmePolitikasi.cs`, `Modules/ModulKayit.cs`, `Data/KuyumcuDbContext.cs`, `Data/GiderPusulasi.cs` (yorum), `Data/YasalParametre.cs`, `Data/VezneFisi.cs`, `Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
  - Web: `Servisler/UyumServisi.cs`, `Pages/GiderPusulalari.razor`, `Pages/Panel.razor`, `Pages/Faturalar.razor` (yalnız özel matrah bölümü + kalem penceresi has satırı), `Pages/Urunler.razor` (KMTS alanları), `Pages/Isletme.razor` (KMTS ayarı)
  - Testler: `S31PanelSadeTestleri.cs`, `FaturaEkranTestleri.cs`, `AcikKalanlarTestleri.cs`, `P10EkranTestleri.cs`, `S33UrunlerEtiketlerEkranTestleri.cs`, `S3bDuzeltilebilirlikHttpTestleri.cs`, `ZoBelgeTestleri.cs`
  - Diğer: `dotnet/denetim.sh`, `dokumanlar/02-mimari/adr/0006-kmts-kapsam-disi.md`
- **3. turda değişen/eklenen:**
  - YENİ: `Kuyumcu.Api/Data/Migrations/20261001073758_Yasal4StopajSiraKilidi{,.Designer}.cs`.
  - DEĞİŞEN: `Data/StopajOdemesi.cs` (SiraNo), `Data/KuyumcuDbContext.cs` (indeks), `Data/Migrations/KuyumcuDbContextModelSnapshot.cs`, `Modules/Hurda/StopajModulu.cs` (sıra kilidi, kırpma kaldırıldı, 409), `Modules/Gider/GiderModulu.cs` (yalnız `SistemKategorisi` yarış yakalaması), `Modules/Hurda/HurdaModulu.cs` (`GiderPusulasiSuresi.Durum`, `PusulaBelgeDurumu`, `TedarikciFaturasi`, faturaBekleyen), `Modules/Rapor/RaporModulu.cs` (yalnız mutabakatın pusula/belgesiz bölümü + kurucu parametreleri; bulgu 2 gereği, paket listesinde olmayan dosya), `Common/KmtsKodu.cs` (Tara + uç), `Core/I18n/Sozlukler.Yasal4.cs` (+5 anahtar, TR+EN; PanelStopajGit metni "Ödendi olarak kaydet"), `Web/Servisler/UyumServisi.cs` (SureTakip.FaturaBekleyenSayisi), `Web/Pages/GiderPusulalari.razor`, `Web/Pages/Panel.razor`, `Web/Pages/Faturalar.razor` (yalnız kalem penceresi has satırı), `Web/Pages/Isletme.razor` (KMTS satırı), `Web/wwwroot/css/yasal.css` (+1 kural `.yasal-eylem`).
  - Testler: `Yasal4HttpTestleri.cs`, `Yasal4EkranTestleri.cs`, `FaturaEkranTestleri.cs`, `S31PanelSadeTestleri.cs`.
  - `Iceaktarim/Kaynaklar.cs` **değiştirilmedi** (bulgu 5'in içe aktarım kısmı için yazma yolu zaten tek doğrulayıcıdan geçiyor; § 0b).
- **Dokunulan sayfalar (tek dokunuş):** GiderPusulalari.razor, Panel.razor, Faturalar.razor, Urunler.razor, Isletme.razor. 3. turdaki ikinci dokunuşun gerekçesi: hasım doğrulayıcının bulguları (2. tur düzeltme turu, aynı paket). Yeni sayfa açılmadı.
- ⚠ **Proje ağacında kalan boş dosya:** `dokumanlar/09-uygulama-kayitlari/betikler/tur/private/yasal4-hedef.mjs` (0 bayt). Ölçüm betiği önce yanlışlıkla buraya yazıldı; içerik `/private/tmp/kuyumcu-serit/YASAL-4/hedef/`e taşındı ve dosya `printf '' >` ile boşaltıldı. Silme kancası yüzünden dosya duruyor; **silinmesi kurucuya bırakıldı.**

## 11. Koşul karşılama (şartname § 6, Y4-D hariç)

| # | Durum | Kanıt |
|---|---|---|
| K-1 | KARŞILANDI | HTTP K1 + bUnit Y4A_K1 + canlı (gecikmiş 0) + M1-2/M11-1 |
| K-2 | KARŞILANDI | HTTP K2 + canlı (son gün 08.10) + M3-2 |
| K-3 | KARŞILANDI | HTTP K3 + SQL (HURDA_ALIS 4 / pusula 5, aynı hareket_id) + M1-1/M2-1 |
| K-4 | KARŞILANDI (409 + belgesiz sayımı); "Uyum'da kapatma gerekçeli kayıt" KISMİ (S-6) | HTTP K4 (9 gün 409, 6 gün 200, belgesiz 1) + canlı + M3-1; 3. tur: mutabakat da aynı türetme (M14-1/M14-2) |
| K-5 | KARŞILANDI | HTTP + canlı 409 |
| K-6 | KARŞILANDI | HTTP K6_K7 + M4-1 |
| K-7 | KARŞILANDI | HTTP + SQL (1 GİDER kasa satırı, stopaj_odemeleri) + denetim sayacı 1 + M5-1; 3. tur: eşzamanlı 6/12 ödemede tek satır (canlı + HTTP, M13-1..4, M19-1) |
| K-8 | KARŞILANDI | bUnit Y4B_K8 ×2 + M12-1 |
| K-9 | KARŞILANDI (rol + okuma izi); dışa aktarım jetonu deseni UYGULANMADI (S-7) | HTTP K9 (satır sayısı, okuma izi, TEZGÂHTAR 403) + canlı CSV + SQL OKUMA + M10-1 |
| K-10 | KISMİ — ekran karşılandı, baskı şablonu yok (S-1) | bUnit Y4C_K10 + tarayıcı (30.257,50 = motor) + M6-1 |
| K-11 | KARŞILANDI | bUnit Y4C_K11 + M7-1 |
| K-12 | KARŞILANDI | bUnit Y4C_K12 (Basit + Pro) |
| K-13 | KARŞILANDI | bUnit + tarayıcı (Basit YOK, Pro VAR) + M8-1 |
| K-14 | KARŞILANDI | HTTP K14 (ürün + satış) + M9-1; 3. tur: mevcut kayıt taraması (HTTP + M18-1) |
| K-15 | KARŞILANDI | § 8 (48 sayfa + 24 hedefli ölçüm, 1280/375 · B/P · açık/koyu) |
| K-16 | Paket kapısında KARŞILANDI | Tam süit 0 başarısız; denetim'de bu paketin sayaçları ✓. Tek ✗ P-30 başka sahipte (§ 6) |

**Paralel şerit kapısı (7 madde):**
**3. tur, 7 madde:**
1. `dotnet build Kuyumcu.sln` 0 hata, 0 uyarı (`build-r3-son7.log`). Ara derlemelerde görülen uyarıların hepsi başka şeritlerin test dosyalarındaydı (AuthSifirla, SUBEKAPSAMA, KesifP2, Toplu1, Giris.razor). Ara `MSB4018 Kuyumcu.Web.wasm` hatası başka şeridin eşzamanlı derlemesinin dosya kilidiydi; yeniden derlendi.
2. Filtreli testler: 848 (2 kırmızı açıklandı, biri düzeltildi) → son 611/611, 154/154, 204/204.
3. Sayaçlar ✓ (P-08 dahil).
4. Mutasyon 14/14 (3. tur) kırmızı, hepsi `cmp AYNI`.
5. Canlı doğrulama 7-C.
6. Test kiracısı silindi (§ 12).
7. Son tam süit 8740 / 8739 geçti / **0 başarısız** / 1 atlandı (başkasının) → yeni kırmızı 0.

**2. tur, 7 madde:**
1. Build 0 hata, 0 uyarı.
2. Filtreli testler 533/533 + 138/138.
3. Sayaçlar ✓.
4. Mutasyon 14/14.
5. Canlı doğrulama yapıldı.
6. Test kiracısı silindi.
7. Tam süit kırmızı 0; yeni kırmızı yok (1. turdaki 5 kırmızının 2'si bu paketindi ve kapandı; diğer 3'ü — Toplu1 ×3 — artık yeşil).

## 12. Temizlik
- API PID 49498 (5160) ve vekil PID 50632 (5161) durduruldu; iki port da `lsof` ile boş.
- 1. turun API (29752) ve vekili (40446) de durduruldu.
- `zsh kiraci-temizle.sh 01M3TSP90RE8J16HBZD63Y1QE0 --uygula` → 947 satır + tenants 1 silindi. Doğrulama:
  - `tenants where id=…` 0, `gider_pusulalari` 0;
  - QA-TEST kiracıları (4) dokunulmadan duruyor;
  - toplam `tenants` = 14. Bu şeridin açtığı 1 kiracı yok; aradaki fark başka şeritlerin kendi açıp sildiği kiracılardan geliyor.
- 5199/5200 portlarına dokunulmadı.
- **3. tur:**
  - API (5160) ve vekil (5161) durduruldu; `lsof` ikisinde de boş.
  - `zsh kiraci-temizle.sh 01M3V8G45MVC87BVG89MV6X9P7 --uygula` → "Toplam 632 satır + tenants 1 satır … SİLİNDİ … kalan kiracı sayısı: 13".
  - Doğrulama: `tenants where id` 0, `stopaj_odemeleri` 0, `gider_pusulalari` 0. Toplam `tenants` = 13, 3. tur başındaki değerle aynı. QA-TEST kiracıları (4) dokunulmadan duruyor.
  - Geçici dosyalar yalnız `/private/tmp/kuyumcu-serit/YASAL-4/` altında (`r3-yedek/`, `mut-r3/`, `canli-r3/`, `hedef-r3/`). Mutasyon yedekleri geri yüklendi, her birinde `cmp AYNI`.

## 13. Açık kalanlar

**4. turdan:**
13. MUHASEBE oturumunda başsız Chrome'da mod anahtarına art arda tıklama bir kez PRO'ya geçmedi (API yolu 200). Büyük olasılıkla 429 dalgası; doğrulanmadı. UstBar sahibi bir tekrar ölçümle kapatmalı.
14. PRO yüzünde "Bekleyen işler" kutusunun bütün satırları (KRİTİK stopaj ve MASAK süresi geçti dahil) sarı `uyari-kutu` içinde. Paket öncesi desen; BASIT yüzde KRİTİK kırmızı noktayla ayrışıyor. PRO'da önem rengi ayrımı ayrı küçük UX işi.
15. Tam süitte başkasına ait 4 kırmızı: `OrtakGorunumSaflik.O7_…` (ortak.css, TOPLU-1/Secici, 12:47) ve `Goc47BTestleri` ×3 (GÖÇ-47B şeridi, 12:43).

**3. turdan:**
6. Kalanı eksi (fazla ödenmiş) dönemde yeni ödeme 409 metni "kalan 0,00 TL" diyor; gerçek kalan −54,96. Para hareketi yok, yalnız metin. Küçük metin düzeltmesi: `StopajModulu.OdemeKaydet` mesajı kalanı biçimlesin.
7. **S-6:** Uyum ekranında "belgesiz alış" listesi ve kapatma gerekçesi. Uyum.razor KESIF-P6'nın dosyası; kapatma kalıcı alan + PATCH + denetim gerektiriyor (ayrı paket).
8. **S-7:** Stopaj CSV'sinin dışa aktarım jetonu akışına bağlanması (güvenlik kararı: Mehmet).
9. İçe aktarım önizlemesi KMTS desenini görmüyor (satır uygulamada 400 ile reddedilir). Desen bugün KAPALI, etkisiz. Desen açıldığında önizleme adımına `YasalParametreServisi` geçirilmeli.
10. Ortak `Sekmeler.razor` rozeti ölçerde "kırık" sayılıyor (görselde tek satır). Bileşen sahibi ya da ölçer düzeltmesi.
11. Mutabakat `gecikmisPusulaSayisi` alanının adı yeni anlamıyla ("yeniden düzenlenmeli") uyuşmuyor; istemci sözleşmesi bozulmasın diye korundu. Gün sonu ekranının (TOPLU-1 / KasaRaporu) etiketi bu anlama göre gözden geçirilmeli.
12. denetim.sh P-08 yorumundaki adlı satır listesi güncellenmeli: "RaporModulu belgesiz hurda" → "HurdaModulu SureTakibi". Dosya orkestratörün.

**1.–2. turdan:**
1. **Fatura baskı şablonu** (S-1): ürün fatura basmıyor. Fatura belgesi/baskısı açıldığında has satırı oraya da eklenmeli (ayrı paket, P-5 belge motoru).
2. Gider pusulası listesinde iptal edilmemiş satırlar hâlâ "Son düzenleme tarihi" sütununu gösteriyor (alarm yok, yalnız tarih). Sade görünüm için bu sütunu takipteki satırlara indirmek ayrı bir küçük UX işi.
3. Has satırı 375 px'te etiket ve değer alt alta geliyor (taşma yok, ölçüm 0; görsel tercih).
4. Başka sahipte duran: `denetim.sh` P-30 DEMO_INSAN_SURESI (orkestratör/kurucu — kronometreli demo ölçümü).
5. Ürünler `#urun-durum-filtre` ezik ölçümü 1280'de (paket öncesinden; Urunler süzgeç sahibi).

## 14. Kurucuya / mali müşavire (şartname § 11; hepsi TEYİTSİZ ve fail-closed tutuldu)
- **S-4 stopaj oranları:** ESNAF_MUAF %2 birincil ilk metinle doğrulandı (RG 03.02.2009/27130, 2009/14592 BKK 13. bent). 2009 sonrası değişiklik doğrulanamadı. NIHAI_TÜKETİCİ %0 TEYİTSİZ. Oran değiştirilmedi, yalnız görünür kılındı.
- **Beyan/ödeme son günü:** `STOPAJ_BEYAN_GUNU` = 26, ekranda "teyit bekliyor". Aylık mı üç aylık mı muhtasar — kiracı ayarı olarak sorulsun mu?
- **Yeniden düzenleme usulü (T-4):** VUK 234'te iptal + yeniden düzenleme hükmü yok. Akış stok/kasa çift yazımını kapatıyor, ama usulün mali müşavirce onaylanması gerekiyor.
- **805 kodu (K-10):** GİB kod listesinde doğrulandı. Kod KAPALI duruyor; açılış mali müşavir imzasıyla sistem ucundan.
- **KMTS 12.08.2026 Usul-Esaslar değişikliği:** tam metin okunamadı. Perakendeye yükümlülük çıkarsa ayrı entegrasyon paketi açılmalı.
- **Has bedeli fiyatı (Y4-D'nin açığı, bilgi):** Borsa İstanbul kapanış verisinin kaynağı/lisansı ve "son işgünü" yorumu (T-5, T-6).
