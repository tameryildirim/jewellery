# PAKET Q9-1 — BANKA: dekont tarihi + "Bekliyor"dan çıkış + storno sonrası bakiye — UYGULAMA RAPORU

- **Tarih:** 2026-09-24 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** DEMO DÜZELTME (QA turu 9)
- **Kaynak:** `ham/qa-turu-9-bulgular.json` — `/banka` rotası: 2 KRİTİK (DUZENLENEBILIRLIK) + 1 YÜKSEK (HATA)
- **Durum:** BİTTİ (paket kapısının yedi maddesi aşağıda kanıtlı; tam süitteki 2 kırmızı başka şeridin, sahipleriyle § 10'da)

---

## 1. Bulguların yeniden üretimi (kod yazılmadan ÖNCE, kendi ölçümüm)

Ortam: API `dotnet run --no-build` → `http://localhost:5174`, vekil `inceleme-vekili.py` → 5173, kendi sekmem (tab-22), test kiracısı `PAKETQ9-1-TEST` (API `/auth/kayit` + tek kullanımlık davet).

| Bulgu | Ekran (tarayıcı, değişiklikten önce) | API / SQL | Sonuç |
|---|---|---|---|
| K1 tarih girilemiyor/düzeltilemiyor | Dekont Gir penceresindeki girdiler: `dekont-tutar`, `dekont-karsi`, `dekont-aciklama` (+ yön/cari seçici) — tarih YOK. Tarih hücresi 3 satırda da düz metin (`button` yok). | Sunucu `HareketEkleDto.Tarih` (P-12'den beri) ve `PATCH …/hareketler/{id}` `tarih` alanını zaten kabul ediyor. | **ÜRETİLDİ** — boşluk yalnız ekrandaydı |
| K2 "Bekliyor"dan çıkış yok | Durum hücresinde 0 düğme/seçici (3 satır). | `BEKLIYOR → ESLESTI` geçişini yapan uç YOK (yalnız oluşturmada `cariId` ya da POS eşleşmesi). | **ÜRETİLDİ** — uç eklendi |
| Y1 storno sonrası bakiye bayat | 1.250 + 300 dekont → ₺1.550,00; 300'lük dekont storno → bant "Stornolandı: 01M39007…", **TOPLAM BAKİYE ve sekme hâlâ ₺1.550,00**. | `GET /bankalar` → `bakiye: 1250.0`; SQL `banka_hesaplari.bakiye = 1250.00`. Kök neden: `StornoUygula` yalnız `HareketleriYukle()` çağırıyordu. | **ÜRETİLDİ** |

## 2. Yapılanlar

### 2.1 Backend (Taşkın) — `Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- **Yeni DTO `DekontEslestirDto { CariId }`** (Required + StringLength). ⚠ `HareketDuzeltDto`'ya EKLENMEDİ: `CariId`, denetim.sh `STORNO_DISI_FINANSAL_PATCH` listesindedir (sayaç 0 kaldı).
- **Yeni servis `BankaServisi.DekontEslestir` — durum makinesi:**
  - `BEKLIYOR → ESLESTI` yalnız bu uçla (müşteri seçilir). `ESLESTI → BEKLIYOR` YOK (bilinçli, § 5).
  - Reddedilenler (409): STORNO satırı, stornolanmış dekont, virman bacağı, `BEKLIYOR` dışı durum. Başka kiracının/olmayan cari → 404. Boş cari → 400 (servis içi elle kontrol + `AlanIle("cariId")`).
  - **MASAK (fail-closed, YENİ HÜKÜM YOK):** mevcut `MasakKapisi.DogrulaKumulatif` dekontun `Olusturuldu` gününe göre yeniden koşulur; kimlik tespiti gerekiyorsa eşleştirme 409 ile reddedilir ve doğru yol söylenir (storno + kimlik bloğuyla yeni dekont). Eşik sayısı koda/istemciye gömülmedi.
  - Denetim izi: `DenetimServisi.YazmaKaydet(islem, "BankaHareketiEslestirme", id, "… durum: BEKLIYOR → ESLESTI · cari: <id>")` (kişisel veri değil, kimlik yazılır).
- **Yeni uç:** `POST /api/v1/bankalar/{id}/hareketler/{hareketId}/eslestir` — `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` (PATCH/storno ile aynı), sınıfta `[ModulYetkisi("banka")]`.
- Tarih için backend değişikliği GEREKMEDİ (oluşturma `Tarih` ve PATCH `tarih` P-12'den beri var; ileri tarih kapıları mevcut).

### 2.2 Sözleşme — `Kuyumcu.Contracts/Sayfalar/BankaSozlesmeleri.cs`
- `HareketVeri`'ye `string? CariId = null` (sona, varsayılanlı; mevcut çağrılar kırılmaz). "Eşleşti" rozetinin müşteri bağından mı (değişmez) yoksa POS/virman bağından mı geldiğini ekran tahmin etmesin diye.

### 2.3 Sözlük — `Kuyumcu.Core/I18n/Sozlukler.BankaDekont.cs` (YENİ, hub'a dokunulmadı)
- `BankaDekontMetinleri` konumsal record, TR + EN (eksik anahtar = derleme hatası). Jargon yok: "cari" yerine "müşteri", "MASAK eşiği" yerine "yasal bildirim".

### 2.4 Ekran (Ertuğ) — `Kuyumcu.Web/Pages/Banka.razor`
- **Dekont Gir:** `<Tarih ElemanId="dekont-tarih">` zorunlu, varsayılan **bugün**, `max = bugün`, ipucu metni. Boş → "Dekont tarihini seçin.", ileri → "Dekont tarihi bugünden ileri olamaz." (alan altı, `alan-hata-dekont-tarih`). Bugün seçiliyse gövdeye `tarih: null` gider (sunucu saati — eski davranış birebir); geçmiş gün `yyyy-MM-ddT12:00:00Z` (öğle UTC: gece yarısı gönderilseydi batı saat dilimlerinde bir gün önce görünürdü). Sunucu alan hatası `Tarih → dekont-tarih` eşlendi.
- **Satırda tarih düzeltme:** tarih hücresi `button.si-tetik#bh-tarih-{id}` (SatirIciDuzenle görünümüyle aynı) → **Cekmece `banka-dekont-tarih`** (`#bh-tarih-girdi`). PATCH gövdesinde YALNIZ `tarih` (üç durumlu sözleşme — diğer alanlar korunur). Yanıttaki `tarih` başarı bandına yazılır ("Dekont tarihi düzeltildi: 19.09.2026"). Aynı gün → istek atılmaz, "değişiklik yapılmadı" bandı. Ekstreden gelen / storno / stornolanmış satırda düz metin + DegismezIpucu (`BankaHareketiEkstreTarih`).
- **Müşteriyle eşleştir:** `BEKLIYOR` + hükümsüz değil + virman değil satırda `#bh-eslestir-{id}` → **Cekmece `banka-dekont-eslestir`** (özet satırı `#bh-eslestir-ozet`, `Secici #bh-eslestir-cari` zorunlu). Boş kaydet → alan altı "Eşleştirmek için müşteri seçin." + form bandı, istek YOK. Başarı bandı: "Dekont eşleşti: <müşteri> · Eşleşti". Müşterisi olan `ESLESTI` satırlar için DegismezIpucu (`BankaHareketiEslesme`: gerekçe + gerçek yol = storno + yeni dekont). Bekleyen satır varsa `#bh-bekliyor-not` "'Bekliyor' ne demek" açıklaması.
- **Storno sonrası bakiye:** `StornoUygula` → `HareketleriYukle()` + **`HesaplariTazele()`** (sessiz: tam sayfa "Yükleniyor" çizmez, hata listeyi boşaltmaz — UX-S28).
- Yeni ModalPencere YOK (sayaç 48, değişmedi); iki yeni Cekmece (tek düzenleme kalıbı). Mevcut id'ler değişmedi; yeni id'ler `bh-`/`cekmece-banka-dekont-*` önekli. Satır içi style / sabit renk yok; app.css'e dokunulmadı.

## 3. Sapmalar ve gerekçeleri
1. **Tarih düzeltme SatirIciDuzenle yerine Cekmece:** SatirIciDuzenle yalnız metin `<input>` taşır; tarih seçici eklemek ortak bileşene (dosya alanım dışında) dokunmak demekti. DoD EK md.2 "çok alanlı → Cekmece, tek alanlı → SatirIciDuzenle" der; tek alanlı Cekmece yasak değil, ikinci `duzenlenenId` kalıbı açılmadı (sayaç 1/1). Tetik görünümü SatirIciDuzenle'yle aynı (`si-tetik`, `data-duzenlenebilir`).
2. **"Bekliyor → Onaylandı (mutabakat)" değil "Bekliyor → Eşleşti":** kodda dekont durumları `BEKLIYOR · ESLESTI · FATURALANDI` (Entities.BankaDurumlari); "Bekliyor" = müşteriyle eşleşmemiş demektir. Ekstre mutabakatı (`EkstreSatirDurumu.Onaylandi`) ayrı bir tablo/akıştır ve elle girilen dekontu kapsamaz. Mevcut ekstre/eşleştirme uçları ölçüldü: `PATCH ekstre/satir/{id}` yalnız sahne satırı içindir, `POS eslestir` yalnız POS yatmasıdır → yeni uç eklendi.
3. **Geri yol (Eşleşti → Bekliyor) açılmadı:** cari bağını kaldırmak/değiştirmek dekontu o müşterinin bağlantılı işlem toplamından DÜŞÜRÜR → eşik bölme yolu. Doğru düzeltme: storno + yeni dekont (ekranda DegismezIpucu söylüyor). DoD EK md.4'ün "geri dönüş yolu" şartı yıkıcı eylemler (pasife al/iptal) içindir; eşleştirme yıkıcı değil.

## 4. Migration
**Gerekmedi** — şema değişikliği yok (`banka_hareketleri.durum`, `cari_id` mevcut kolonlar).

## 5. Mevzuat notu (MEVZUAT KURALI)
- Bu pakette **yeni mevzuat hükmü YAZILMADI**. Eşleştirmedeki MASAK kontrolü mevcut `MasakKapisi.DogrulaKumulatif`'in (eşikler `MasakKuralMotoru` ayarından) aynen yeniden kullanımıdır; yön tutucu (fail-closed): gerekirse reddeder, asla gevşetmez.
- Dekont tarihi düzeltmesi P-12'de açılmış yoldur; MASAK kümülatifi `olusturuldu` eksenini okuduğu için tarih düzeltmesi eşik hesabını kaydıramaz (Mehmet K41-1, kodda yorumlu).
- **Kurucuya/mali müşavire not (DOĞRULANAMADI, koda sabit yazılmadı):** "Elle girilmiş bir banka dekontunun müşterisinin SONRADAN belirlenmesi hâlinde MASAK kimlik tespiti anı" konusunda birincil kaynak aramadım; bu yüzden ürün en tutucu yolu seçti (eşiğe değiyorsa sonradan eşleştirme yok, storno + kimlikle yeni kayıt). Gevşetilmesi istenirse mali müşavir teyidi gerekir.
- Mevcut davranış (değiştirilmedi, bilgi): döviz hesabındaki dekont tutarı MASAK kapısına hesap para birimindeki sayı olarak gider (HareketEkleIc ile aynı); eşleştirme de aynı sayıyı kullanır.

## 6. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Filtre `Q91` (yeni 22 test) | 22 | 22 | 0 |
| İlgili süitler (`Banka|P12|Ekstre|Duzeltme|P19|PosEslestirme|MobilParite|Ikon|Sozluk|Masak|Q91`) | 494 | 494 | 0 |
| **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_l`, trx: `scratchpad/trx-pQ9-1`) | **5054** | 5052 | **2 (başka şerit, § 10)** |

Önce/sonra: tam süit kapıda 3869 idi (22.09 kapısı); bu ağaçta şimdi 5054 (diğer şeritlerle birlikte). Bu paket **+22 test** ekledi.

**Yeni testler**
- `Kuyumcu.Tests/Q91BankaDekontTestleri.cs` [HTTP + gerçek MySQL, 11]: `Q91_dekont_GECMIS_tarihle_girilir_ve_oyle_saklanir` · `Q91_dekont_tarihi_PATCH_ile_DUZELTILIR_diger_alanlar_KORUNUR_iz_yazilir` · `Q91_ILERI_tarihe_duzeltme_REDDEDILIR` · `Q91_bekleyen_dekont_musteriyle_ESLESIR_durum_ve_iz_yazilir` · `Q91_eslesmis_dekont_IKINCI_KEZ_eslestirilemez` · `Q91_stornolanmis_dekont_ve_STORNO_satiri_eslestirilemez` · `Q91_bos_musteriyle_eslestirme_400_ve_satir_DEGISMEZ` · `Q91_BASKA_kiracinin_musterisiyle_eslestirilemez` · `Q91_eslestirme_ROL_kapisi_TEZGAHTAR_403` · `Q91_MASAK_esigini_asan_sonradan_eslestirme_REDDEDILIR` · `Q91_storno_sonrasi_hesap_listesi_YENI_bakiyeyi_doner`
- `Kuyumcu.Tests/Q91BankaEkranTestleri.cs` [bUnit, 11; durumlu sahte API, yeni uçlara gerçekçi yanıt]: `K1_dekont_formunda_TARIH_alani_VAR_ve_varsayilan_BUGUN` · `K1_gecmis_tarih_POST_govdesine_OGLE_UTC_olarak_gider` · `K1_bugun_secili_kalirsa_tarih_GONDERILMEZ_sunucu_saati_kullanilir` · `K1_ileri_tarih_ALAN_ALTINDA_soylenir_ve_POST_GITMEZ` · `K1_satirdaki_tarih_DUZENLENEBILIR_ve_PATCH_YALNIZ_tarih_tasir` · `K1_ekstreden_gelen_satirda_tarih_tetigi_YOK_ve_sebebi_YAZILI` · `K1_bos_tarihle_kaydet_ALAN_ALTI_mesaj_verir_PATCH_GITMEZ` · `K2_bekleyen_dekontta_ESLESTIR_dugmesi_VAR_eslesmiste_YOK` · `K2_musteri_secmeden_kaydet_ALAN_ALTI_mesaj_verir_istek_GITMEZ` · `K2_musteri_secilince_ESLESTIR_ucu_cagrilir_ve_sonuc_BANDA_yazilir` · `Y1_storno_sonrasi_TOPLAM_BAKIYE_ve_hesap_sekmesi_TAZELENIR`

## 7. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/Q9-1/` altına kopya → LİTERAL tek eşleşme (eşleşme sayısı 1 değilse uygulanmaz) → filtreli `dotnet test -v n` → yedek geri → `cmp` birebir. Betik `scratchpad/q91-mut.sh`, desenler `scratchpad/mut-desen/`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata 0 uyarı.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| MQ91-1 | `if (h.Durum != "BEKLIYOR")` → `== "__MUTANT__"` | Q91_eslesmis_dekont_IKINCI_KEZ_eslestirilemez | AYNI |
| MQ91-2 | MASAK `DogrulaKumulatif` çağrısı → `Task.CompletedTask` | Q91_MASAK_esigini_asan_sonradan_eslestirme_REDDEDILIR | AYNI |
| MQ91-3 | stornolanmış kontrolü `if (false && stornolandi)` | Q91_stornolanmis_dekont_ve_STORNO_satiri_eslestirilemez | AYNI |
| MQ91-4 | `h.Durum = "ESLESTI"` → `"BEKLIYOR"` | Q91_bekleyen_dekont_musteriyle_ESLESIR_durum_ve_iz_yazilir | AYNI |
| MQ91-5 | denetim `YazmaKaydet` → `if (false)` | Q91_bekleyen_dekont_musteriyle_ESLESIR_durum_ve_iz_yazilir | AYNI |
| MQ91-6 | storno sonrası `await HesaplariTazele();` silindi | Y1_storno_sonrasi_TOPLAM_BAKIYE_ve_hesap_sekmesi_TAZELENIR | AYNI |
| MQ91-7 | dekont gövdesi `tarih = (string?)null` | K1_gecmis_tarih_POST_govdesine_OGLE_UTC_olarak_gider | AYNI |
| MQ91-8 | ileri tarih kontrolü `> Bugun.AddDays(999)` | K1_ileri_tarih_ALAN_ALTINDA_soylenir_ve_POST_GITMEZ | AYNI |
| MQ91-9 | `TarihDuzeltilebilir` ekstre şartı silindi | K1_ekstreden_gelen_satirda_tarih_tetigi_YOK_ve_sebebi_YAZILI | AYNI |
| MQ91-10 | `EslestirAcik` durum şartı silindi | K2_bekleyen_dekontta_ESLESTIR_dugmesi_VAR_eslesmiste_YOK | AYNI |
| MQ91-11 | boş müşteri kontrolü `if (false && …)` | K2_musteri_secmeden_kaydet_ALAN_ALTI_mesaj_verir_istek_GITMEZ | AYNI |
| MQ91-12 | PATCH gövdesine `aciklama` eklendi | K1_satirdaki_tarih_DUZENLENEBILIR_ve_PATCH_YALNIZ_tarih_tasir | AYNI |
| MQ91-13 | uç rolüne `"TEZGAHTAR"` eklendi | Q91_eslestirme_ROL_kapisi_TEZGAHTAR_403 | AYNI |

13/13 kırmızı; test açığı çıkmadı. (Not: MQ91-4'ün ilk koşumu başka şeridin eşzamanlı derlemesi yüzünden `ConvertDllsToWebCil` MSB4018 ile derlenemedi — test sonucu sayılmadı, aynı mutasyon yeniden koşuldu ve kırmızı döndü.)

## 8. denetim.sh
Çıkış: **DENETİM BAŞARISIZ (3 ✗) — üçü de bu paketin dışında**, aynı tek kökten:
- `Pages+Bilesenler+Layout record tanımı 4 (tavan 3)`, `P-10: BU_PAKETTE_EKLENEN_RECORD +1`, `P-02: OLCUM_TABANI_KOPYA (record=4 vs 3)` → dördüncü record `Kuyumcu.Web/Bilesenler/Sekmeler.razor:51 public sealed record SekmeTanimi` (S1-h/S1-i şeridinin dosyası; bu şeride YASAK liste). Bu paket razor'a record EKLEMEDİ (yeni tip Contracts/Core'da).

Bu paketin sayaçları ✓: `BankaHareketleri.Add 5` (değişmedi) · `Hareketler.Add 1` · `IgnoreQueryFilters 0` · `Find/FindAsync 0` · `ExecuteUpdate/Delete 5` · `ModalPencere 48 (tavan 55, artış 0)` · `duzenlenenId 1/1` · `P-12 STORNO_DISI_FINANSAL_PATCH 0` · `P-12 STORNO_GORUNMEYEN_EKRAN 0` · `P-18 SABIT_RENK 0` · `satır içi style 109 (tavan 196)`.

## 9. Canlı doğrulama (API 5174 + vekil 5173, kendi sekmem, kuyumcu_net)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| Dekont Gir'de tarih alanı | type=date, varsayılan bugün, max bugün | `{"type":"date","value":"2026-09-24","max":"2026-09-24"}` | DOM ölçümü |
| 450 TL dekont, tarih 21.09.2026 (tarayıcıdan) | POST 200, listede 21.09.2026 | bant "Hareket kaydedildi!", satır `21.09.2026 · Q91 canli gecmis tarihli · +₺450,00 · Bekliyor` | SQL `tarih = 2026-09-21 12:00:00`, `olusturuldu = 2026-09-24 06:17:00` |
| Satırda tarih düzelt → 19.09.2026 | PATCH 200, SQL değişir, iz yazılır | ağ: `PATCH …/hareketler/01M390YSZQ… → 200 OK`; bant "Dekont tarihi düzeltildi: 19.09.2026" | SQL `tarih = 2026-09-19 12:00:00`; `denetim_kayitlari BankaHareketiDuzeltme: tarih: 2026-09-21 → 2026-09-19` |
| Eşleştir, müşteri seçmeden Kaydet | alan altı mesaj, istek yok | `alan hata: "Eşleştirmek için müşteri seçin."` | DOM + ekran görüntüsü |
| Eşleştir, müşteri seçili | POST 200, Bekliyor→Eşleşti, iz | bant "Dekont eşleşti: Q91 Test Musteri · Eşleşti", satır rozeti "Eşleşti", eşleştir düğmesi kalktı, DegismezIpucu çıktı | SQL `durum=ESLESTI, cari_id=01M390XRDX…`; `denetim_kayitlari BankaHareketiEslestirme: durum: BEKLIYOR → ESLESTI · cari: 01M390XRDX…` |
| 1.250 TL dekont storno (tarayıcı) | TOPLAM BAKİYE sayfa yenilenmeden doğru | ÖNCE ₺1.700,00 → SONRA **₺450,00** (kart + sekme), `navigation.type = navigate` (yeniden yükleme yok) | SQL `banka_hesaplari.bakiye = 450.00` |

(mysql istemcisinin konsol karakter kümesi Türkçe harfleri `?` bastı; veri UTF-8 ve API/ekranda doğru.)

### 9.1 Gerçek tarayıcı ölçüm tablosu (sikisma-olc.js + ek ölçüm)

Ölçülen durumlar: sayfa (/banka, bekleyen + eşleşmiş + stornolu satırlarla), Dekont Gir penceresi, Tarih düzelt çekmecesi, Eşleştir çekmecesi. Pencere/çekmece ölçümü kendi kökünde (`.modal-panel`, `#cekmece-banka-dekont-*`) yapıldı — ham betik bütün `main`'i ölçtüğü için açık pencerenin arka planla "üst üste binmesini" sayıyordu (katman, kusur değil).

| Genişlik | Mod | Tema | Durum | taşan | binen | gerçek kırık | yatay kaydırma | [hidden] görünen | 'TRY' kalıbı | en geniş blok |
|---|---|---|---|---|---|---|---|---|---|---|
| 1280×800 | Profesyonel | koyu | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | Basit | koyu | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | Basit | açık | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | Profesyonel | açık | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %95 |
| 375×812 | Profesyonel | açık | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %94 |
| 375×812 | Basit | açık | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %94 |
| 375×812 | Basit | koyu | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %94 |
| 375×812 | Profesyonel | koyu | sayfa / modal / tarih ç. / eşleştir ç. | 0/0/0/0 | 0/0/0/0 | 0/0/0/0 | 0 | 0 | 0 | %94 |

Ham `sikisma-olc.js` "kirik" sayacının yanlış pozitifleri (ölçüyle ayrıştırıldı):
- 1280: `si-banka-dekont-aciklama-… "Q91 canli gecmis tarihli"` — dört kelimelik VERİ metni dar sütunda 2 satıra sarıyor; tek kelime kırılmıyor (P-12'nin mevcut hücresi, bu pakette dokunulmadı).
- 1280 (çekmece açıkken): `cekmece-*-iptal/kaydet` — ortak Cekmece düğmesi `min-height: 32px`, `line-height: normal` → betik yüksekliği "1,7 satır" sanıyor; `Range.getClientRects` metin satırı = **1**.
- 375: `bh-tarih-… "22.09.2026"`, `si-banka-dekont-karsitaraf-… "—"` — mobil dokunma hedefi `min-height: 44px`; metin satırı = **1** (tek karakterlik "—" bile işaretleniyor: kanıt yanlış pozitif).
- 375 sayfada "kök dışına taşan 83" öğenin TAMAMI `#banka-hesap-liste-sar` (`overflow-x: auto`) içindeki tablo satırlarıdır — tasarlanmış yatay kaydırmalı tablo; sayfa yatay kaydırması 0.

Ekran görüntüleri (bu oturumda, tab-22): (a) değişiklikten önce storno sonrası ₺1.550,00 bayat kart; (b) 1280 koyu Dekont Gir penceresi + "Dekont tarihi 21.09.2026"; (c) 1280 koyu "Dekont tarihini düzelt" çekmecesi; (d) 1280 koyu/açık "Dekontu müşteriyle eşleştir" çekmecesi, boş kaydet alan altı mesaj; (e) 1280 koyu storno sonrası ₺450,00; (f) 375 koyu Dekont Gir (tarih alanı taşmadan); (g) 375 koyu Eşleştir çekmecesi; (h) 1280×1500 koyu tam tablo: tarih tetikleri, "Müşteriyle eşleştir", "Eşleşti" rozeti, üç DegismezIpucu ve "Bekliyor" notu. Araç notu: tarayıcı bölmesinin ekran yakalaması öykünülmüş 1280×800'de kaydırılmış sayfayı boş boyuyor; bu yüzden tablo görüntüsü 1280×1500'de alındı (ölçüm 1280×800'de).

### 9.2 Temizlik
- API (PID 29655) ve vekil durduruldu: `lsof :5174` = 0, `:5173` = 0. Sekme tab-22 kapatıldı, görüntü alanı `desktop`'a döndü.
- Test kiracısı `01M38ZY59862DG8P3AEWA9VWMY` (`PAKETQ9-1-TEST`) `kiraci-temizle.sh --uygula` ile silindi (154 satır + tenants 1); `tenant_id` kolonlu 70 tablonun hepsinde kalan satır 0; davet satırı `Q91DAVET…` silindi (0). **tenants: başlangıç 11 → son 11.** QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Fikstür/jeton/davet dosyaları `printf '' >` ile boşaltıldı (silme kurucuya).

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzülmüş, bu paketinkiler)
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` (M)
- `dotnet/Kuyumcu.Web/Pages/Banka.razor` (M)
- `dotnet/Kuyumcu.Contracts/Sayfalar/BankaSozlesmeleri.cs` (depoda izlenmiyor, değiştirildi)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.BankaDekont.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/Q91BankaDekontTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/Q91BankaEkranTestleri.cs` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/PAKET-Q9-1-uygulama-raporu.md` (bu rapor)

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Banka.razor`. `Pages/BankaEkstre.razor` açılmadı.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Girişte tarih alanı (varsayılan bugün) | ✓ | bUnit K1_* + canlı 21.09 |
| Satırda tarih düzeltme, kilitli satırda DegismezIpucu | ✓ | K1_satirdaki…, K1_ekstreden…, canlı PATCH 200 + SQL |
| Sayfa "doğrudan düzeltilebilir" metni artık doğru | ✓ | açıklama/karşı taraf (P-12) + tarih (Q9-1) üçü de ekranda düzeltilebilir |
| Bekliyor → durum değişir (uç + durum makinesi + denetim izi) | ✓ | Q91_bekleyen…, canlı SQL + denetim_kayitlari |
| Storno sonrası TOPLAM BAKİYE/hesap bakiyesi aynı ekranda | ✓ | Y1_…, canlı 1.700 → 450 yenilemesiz |
| Sözleşme-DoD: yeni kayıt türü yok; yeni geçiş için uç + [RolGerekli] + [ModulYetkisi] + denetim + ekranda eylem | ✓ | § 2.1, § 2.4 |
| Tek yazma yolu atlanmadı | ✓ | denetim.sh BankaHareketleri.Add 5, Hareketler.Add 1 |
| PATCH sözleşmesi (yoksa koru) testi | ✓ | Q91_dekont_tarihi_PATCH…KORUNUR, K1_satirdaki…YALNIZ_tarih |
| Her yeni düğme istek atar ya da alan altı mesajla durur | ✓ | K2_musteri_secmeden…, K1_bos_tarihle… |
| Sözlük TR+EN ayrı dosya, hub'a dokunulmadı | ✓ | Sozlukler.BankaDekont.cs |
| app.css / yasak listedeki dosyalar | ✓ dokunulmadı | § 10 |
| DOM id / rota değişmedi | ✓ | yalnız yeni id eklendi |
| Mevzuat: yeni hüküm yok, fail-closed | ✓ | § 5 |
| Gerçek tarayıcı ölçümü (2 genişlik × 2 mod × 2 tema) | ✓ | § 9.1 |
| Mutasyon | ✓ 13/13 | § 7 |
| Test kiracısı silindi, tenants sayısı döndü | ✓ 11→11 | § 9.2 |
| Bu paket yeni kırmızı üretmedi | ✓ | tam süitteki 2 kırmızı S1-h şeridinin dosyalarına ait (aşağıda) |

## 12. Açık kalanlar
1. **Tam süitte başkasına ait 2 kırmızı** (bu paketin dosyalarına dokunmuyor; iki dosya da koşum sırasında 09:17/09:18'de başka şerit tarafından değiştirildi):
   - `S1hGorselSigmaTestleri.S1h_03_Sekmeler_tiklama_URLye_sekme_yazar_ve_TARIH_KAYDI_acar` → sahibi **S1-h/S1-i görsel sığma şeridi** (`Bilesenler/Sekmeler.razor`).
   - `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` → sahibi **S1-h/S1-i şeridi** (`wwwroot/css/app.css`).
2. **denetim.sh 3 ✗** — `Sekmeler.razor` içindeki yeni `record SekmeTanimi` (S1-h/S1-i şeridi).
3. **Derleme uyarısı 1 (başka dosya, ara derlemede):** `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` — son temiz derlemede 0 uyarı.
4. **Bu paketin kapsamı dışında gözlenen (dokunulmadı):** stornolanmış GELEN dekontta "Faturaya Dönüştür" düğmesi hâlâ görünüyor (sunucu reddediyor mu doğrulanmadı — ayrı bulgu adayı); hesap sekmesine tıklamak (`HesapSec`) başka yerden eklenen dekontun bakiyesini tazelemiyor (yalnız hareketleri çeker); QA turu 9'un /banka ORTA/DÜŞÜK bulguları (IBAN istemci doğrulaması, "kasa" diyen pasife al ipucu, yüklenirken ₺0,00, "kümülatif kurulamaz" jargonu) bu paketin kapsamında değildi.
5. Mali müşavir teyidi: § 5 (sonradan müşteri eşleştirmesinde MASAK anı).
