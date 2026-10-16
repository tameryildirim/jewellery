# PAKET EKSIK-KUCUK — uygulama raporu (B2-10 · B2-11 · B2-12 + kılavuz)

**Tarih:** 10.10.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** ISLEM-BANT-2 + EKSIK-KUCUK + SATINALMA-EKRAN (t5 · 5166/5167 · `kuyumcu_test_t5`)
**Kaynak:** `dokumanlar/09-uygulama-kayitlari/EKSIK-OZELLIK-DENETIMI-2026-10-02.md` § B2 satır 10, 11, 12.
**COMMIT YOK.** Dosya silinmedi. Sır/parola değeri hiçbir çıktıya yazılmadı (sistem anahtarı rastgele üretildi, dosyalar chmod 600, iş sonunda `printf '' >` ile boşaltıldı).

---

## 1. Ne yapıldı

### B2-10 — Vitrin panosu: tam ekran + "işçilik dahildir / hariçtir" ibaresi (yasal)
- **Sunucu (Taşkın):** `kiraci_ayarlari` içinde yeni anahtar `pano.iscilik_ibaresi` = `DAHIL` | `HARIC` (migration YOK; P-17'nin kiracı ayarı deseni). `pano_kalem.` önekini taşımadığı için kalem okuyucusuna karışmaz (testle kanıtlı).
  - `GET /api/v1/pano/guncel` yanıtına `iscilikIbaresi` eklendi (geriye dönük uyumlu, varsayılan `null`).
  - YENİ `PATCH /api/v1/pano/iscilik-ibaresi` — `[RolGerekli("SAHIP","MUDUR")]`, modül kapısı `kur` (sınıf). Sözleşme: alan yoksa **400**, geçersiz değer **400** (alanlı), `null` = satır silinir (pano yeniden kilitlenir), küçük harf normalize edilir. `DenetimServisi.YazmaKaydet` çağrılır. Yanıt: güncel pano (UX-S23).
  - Sunucu iletisi `SunucuIletiSozlugu.Veri.cs`'ye TR/EN çifti olarak eklendi (DIL-SUNUCU sayacı).
- **Ekran (Ertuğ):**
  - `/vitrin`: fiyat tablosunun altında ibare bloğu. Seçilmişse cümle kalın yazılır; seçilmemişse sarı bant ("seçmeden dükkân ekranı açılmaz"; tezgâhtara "müdürünüz seçmeli"). MUDUR+ için **İşçilik dahil / İşçilik hariç** düğmeleri (seçili olan `aria-pressed` + vurgu; birincil düğme kuralı bozulmadı) ve yasal not. Sayfa başında **Dükkân ekranını aç**: ibare yoksa `disabled` + `title` sebep + `aria-describedby` (UX-S30), varsa `/vitrin/ekran` bağlantısı.
  - YENİ `/vitrin/ekran` (`Pages/VitrinEkrani.razor`): **kabuksuz** (kenar çubuğu/üst bar/Yıldız üst menüsü yok — `AnaYerlesim.VitrinEkranRotasi`, her şemada), yetki kapısı ve hata sınırı aynen sarar. Büyük yazılı kalem/alış/satış listesi, altında ibare, son güncelleme + kaynak; 60 sn'de bir kendini yeniler; **Tam ekran** (tarayıcı `requestFullscreen`) ve **Panoya dön**. **Fail-closed:** ibare seçilmemişse fiyat GÖSTERİLMEZ, kilit bandı çıkar. İbare `yazdirma-gizle` taşımaz → kâğıtta da çıkar (kâğıt = ekran).
  - `MenuTanimi.MenusuzRotalar` (gerekçe: Vitrin.razor), `RotaModulleri` (`kur`), `RotaBasliklari` ("Dükkân ekranı" / "Shop screen") kayıtları.
- **Sözlük:** `Kuyumcu.Core/I18n/Sozlukler.EksikKucuk.cs` (TR+EN, boş anahtar testle yasak).
- **CSS:** `wwwroot/css/eksik-kucuk.css` (yalnız `var()` belirteç; satır içi style yok), `index.html`'de `girdi.css`'ten sonra, `olcek.css`'ten önce.

### B2-11 — Yeni işletme BASİT modda açılır (D1)
- `AuthServisi` kayıt yolu (`POST /api/v1/auth/kayit`, yeni kiracının TEK doğum yolu) `VarsayilanArayuzModu = ArayuzModu.Basit` yazar.
- **Sapma (gerekçeli):** spesifikasyon `Data/Entities.cs:148` başlangıç değerini gösteriyor. O değer (ve `KuyumcuDbContext`'teki `HasDefaultValue("PROFESYONEL")`) DEĞİŞTİRİLMEDİ, çünkü (a) değiştirmek migration/varsayılan değişikliği demektir, (b) test fikstürü `TestKurulum.KiraciKur` aynı başlangıç değerini kullanır ve "mevcut davranış korunur" testleri (UX-3) kırılırdı. Hedef (yalnız YENİ işletme BASİT, eskiler aynen) kayıt yolunda sağlandı; `Entities.cs`'e yalnız açıklama eklendi. Migration yok, mevcut satır yok.

### B2-12 — Müşteri değişince "fiyat listesi değişti — güncelle" (UX-S24)
- `Pages/Islemler.ListeGuncelle.cs` (yeni partial) + `Bilesenler/Fis/FisListeDegisti.razor` (yeni bant bileşeni; Islemler.razor 1.400 satır sınırı için).
- Kalem teklif fiyatıyla eklenince izi tutulur: (teklif fiyatı, kur kaydı). Müşteri değişince (`MusteriDegisti`) yalnız SATIŞ'ta, hediye olmayan ve fiyatı ELLE değiştirilmemiş kalemler için **aynı kur kaydıyla** yeni müşterinin teklifi istenir (`GET /fiyat/teklif?…&cariId=…&kurSnapshotId=<kalemin kaydı>`; liste çözümü sunucuda — `ListeCoz`). Fark varsa bant: "Müşteri değişti — bu müşterinin fiyat listesine göre N kalemin fiyatı değişiyor." + **Fiyatları güncelle** / **Eski fiyatlar kalsın**.
- Basılmazsa ya da "Eski fiyatlar kalsın" denirse sepet AYNEN kalır ve gövdeye o fiyat gider. "Güncelle" basılınca kalem fiyatı/liste fiyatı/işçilik yeni tekliften yazılır, başarı bandı + tevsik yeniden hesap. Teklif alınamazsa sepet değişmez, hata bandı (UX-S28). Arada müşteri yeniden değişirse eski sorgunun sonucu basılmaz (sıra numarası).
- Yıldız dalına dokunulmadı (bant `KalemParcasi` içinde; ortak parça).
- **Not:** ekran liste KİMLİĞİ karşılaştırmaz; fiyat farkına bakar (sunucu teklif ucu liste kimliği döndürmüyor, yeni uç açılmadı). Aynı kur kaydı kullanıldığı için fark yalnız müşteriden (liste/cari koşulu) doğar.

### Kılavuz
- `docs/KULLANIM_KILAVUZU.md`: "Yeni açılan işletme basit modda başlar" (tanıtım), fiyat listesi maddesi davranışla birebir (iki düğme, basılmazsa eski fiyat, elle fiyat ve hediye hariç), Vitrin bölümüne işçilik ibaresi maddesi, YENİ bölüm **"Dükkân ekranı {#vitrin-ekran}"** + `<!-- ekran: /vitrin/ekran -->` işareti. `KILAVUZ_YAZ=1 ./kilavuz-uret.sh` ile `kilavuz.html` üretildi (71.319 bayt).

## 2. Mevzuat (BAĞLAYICI kural) — B2-10

- **Birincil kaynak okundu:** Fiyat Etiketi Yönetmeliği, Resmî Gazete 28.06.2014 / 29044 — https://www.resmigazete.gov.tr/eskiler/2014/06/20140628-2.htm (RG günlük fihristi https://www.resmigazete.gov.tr/eskiler/2014/06/20140628.htm üzerinden bulundu). Madde 6 ("Etiket zorunluluğu olmayan mallar"), kıymetli taş/maden fıkrası, alıntı: *"Bu satış fiyatlarına işçilik bedelinin dahil olup olmadığı ayrıca belirtilir."*
- **Koda giren hüküm:** yalnız "dahil olup olmadığı belirtilir". Hangisinin (dahil/hariç) doğru olduğu dükkânın fiyat politikasıdır → **koda sabit yazılmadı**, kiracı ayarıdır; seçilmemişse müşteriye dönük pano **fiyat göstermez** (fail-closed). Eşik/oran yok.
- **TEYİTSİZ kalan:** fıkra NUMARASI. 2014 asıl metninde bu hüküm md.6'nın 2. fıkrasıdır; denetim belgesi ve iç notlar "md.6/3" diyor (güncel konsolide metin, ikincil kaynak lexpera). Güncel konsolide birincil metin (mevzuat.gov.tr) bu oturumda açılamadı (denenen iki adres 404). Bu yüzden ekranda ve kılavuzda fıkra numarası YAZILMADI: ekran "Fiyat Etiketi Yönetmeliği md.6", kılavuz yalnız yönetmelik adı (kılavuzun "mevzuat madde numarası taşımaz" testi gereği).

## 3. Test sayıları

| Koşum | Toplam | Geçti | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket filtresi `EksikKucuk` (son kod) | 23 | 23 | 0 | 0 |
| İlgili geniş filtre (Vitrin, P17, Menü, RotaBaslik, S1d, Kılavuz, ArayüzModu, GocD, PeşinMüşteri, İkon, S1f, Yıldız2, TK4, MenüKatlama, DilSunucu, Toplu4/6, OnayKipi, P38, S35) | 602 | 599 → düzeltme sonrası tümü yeşil | 3 → 0 | 0 |
| Tam süit tur 1 (ara kod) | 12.274 (vstest sayımı) | — | 14 | — |
| **Tam süit tur 2 (SON KOD, trx `/private/tmp/kuyumcu-serit/trx-pEKSIK-KUCUK/tur2.trx`)** | **12.277** | **12.268** | **6** | **3** |

- Yeni test: **23** (`Kuyumcu.Tests/EksikKucukTestleri.cs`: HTTP 6 · saf 7 · bUnit vitrin 4 · bUnit satış 6).
- Tur 1'de bu paketin ürettiği kırmızılar ve düzeltmesi: `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz`, `Toplu5KaynakTestleri.T5_Islemler_razor_1400…`, `S1iGorselSigma2Testleri.S1i_06…` (Islemler.razor 1.413 satır) → bant `FisListeDegisti.razor` bileşenine taşındı, 1.399 satır. `P37cPanelVeKapsamTestleri.Her_ekran…` ve `RotaBaslikTestleri.Yansima_TAM_42_rota_bulur` (sayfa/rota sayısı 51 → 52) → emsal paketler gibi sayı + gerekçe satırı güncellendi. `KilavuzUretimTestleri` (ekran işareti başlığı + madde numarası) → kılavuz bölümü düzeltildi.
- **Tam süit tur 2'deki 6 kırmızı — hiçbiri bu paketin değil (fark = 0):**
  - `SonKalanlarTahsilatEkranTestleri.SKC01…(BASIT/PROFESYONEL)` ×2 — Cariler tahsilat çekmecesi (`Pages/Cariler.razor` bugün 18:47'de başka şeritçe değişti; YASAL-KAPI-2 alanı). Tek başına da kırmızı.
  - `RaporDortTestleri.RD_KH01` / `RD_KH02` — "Rapor dönemi en çok 400 gün" (GOC-D kuralı × RAPOR-4 testi); devir notlarında önceki turlarda da var.
  - `FaturaEkranTestleri.Y4C_K12…(basit: False)` — "805" yasak alt dizgesi rastgele GUID'e çarpıyor (bilinen kararsız; tek başına 3/3 yeşil).
  - `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter…` — yük altında kararsız (bilinen; tek başına 3/3 yeşil; bu paket arama isteğine dokunmaz).

## 4. Mutasyon kanıtları (mut.sh yok → yedek + `perl -0777 -pi` + filtreli test + geri kopya + `cmp`)

Yedekler: `/private/tmp/kuyumcu-serit/mut-yedek/EKSIK-KUCUK/`. Her mutasyondan sonra `cmp` AYNI; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` 0 hata.

| Kod | Değişiklik | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M-EK-1 | `PanoModulu.Gorunum`: `IscilikIbaresi: ibare` → `null` | EK_H02 | cmp AYNI |
| M-EK-2 | PATCH "verildi" kapısı `if (!dto.IscilikIbaresiVerildi)` → `if (false)` | EK_H03 | cmp AYNI |
| M-EK-3 | Kayıt yolu `ArayuzModu.Basit` → `Profesyonel` | EK_H06 | cmp AYNI |
| M-EK-4 | Dükkân ekranı kilidi `@if (p.IscilikIbaresi is null)` → `@if (false)` | EK_B03 | cmp AYNI |
| M-EK-5 | Liste farkı SESSİZCE uygulanır (`ListeFiyatlariniUygula` otomatik) | EK_S01, EK_S02, EK_S03, EK_S05 | cmp AYNI |
| M-EK-6 | "Güncelle" fiyat yazmaz (`k.BirimFiyat = k.BirimFiyat`) | EK_S02, EK_S05 | cmp AYNI |
| M-EK-7 | Vitrin düğmesi kilidi `@if (pano?.IscilikIbaresi is not null)` → `@if (true)` | EK_B01 | cmp AYNI |
| M-EK-8 | Aday süzgeci elle fiyatlı kalemi de sorar (`.Where(k => !k.Hediye)`) | ilk denemede **0 kırmızı (TEST AÇIĞI)** → EK_S06 yazıldı → EK_S06 | cmp AYNI |

## 5. denetim.sh

Son koşum (`/private/tmp/kuyumcu-serit/EKSIK-KUCUK/denetim3.txt`): bu paketin dokunduğu sayaçların hepsi ✓ —
`Pages/*.razor dosya sayısı 52 (tavan 52)` · `P-02: ROTA_KAPSAM 52` · `P-02: OLCUM_TABANI_KOPYA 0 (varlık 76 · rota 52 · record 3)` · `P-37c: SAYFA_BASI_KAPSAM 49/49 (+3 yazılı muafiyet)` · `P-33: KILAVUZ_EKSIK_EKRAN 0 (52 rota)` · `DIL-SUNUCU: SUNUCU_CIPLAK_TURKCE_ILETI 0` · `GRID-1 ham tablo 8 (tavan 8)`.
Kalan 2 ✗ başkasının: `P-30: DEMO_INSAN_SURESI` (demo prova kronometresi — kurucu/orkestratör) ve `GIRDI-2: TEST_GECERSIZ_FIKSTUR 1` (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` geçersiz IBAN — 09.10 tarihli doğrulayıcı dosyası). → Ağaç ölçüsünde "DENETİM BAŞARISIZ"; paket ölçüsünde sayaçlarım temiz.

**Sayaç tavanı artışı (gerekçeli, emsal: P-17, S1-d, YASAL-1, AUTH-SIFIRLA, EKSIK-B1-SATINALMA):** yeni sayfa `VitrinEkrani.razor` → `denetim.sh` Pages 51→52 ve `P02_ROTA_BEKLENEN` 51→52 (gerekçe yorumları yazıldı), `ham/olcum-2026-09-18.json` ROTA_KAPSAM 51→52 (not satırı), `betikler/qa-turu.js` `ROTA_KAPSAM_BEKLENEN` 51→52. Gerekçe: dükkân ekranı kabuksuz müşteri/TV sayfasıdır, `/vitrin` ise MUDUR+ yönetim ekranı; aynı sayfada yaşayamazlar (P-17'nin kendi gerekçesi).

## 6. Canlı doğrulama (API 5166, vekil 5167, başsız Chrome CDP 9501–9515)

Başlangıç: `tenants` = **23** (PROFESYONEL 21 · BASIT 2; tam id→mod listesi `tenants-once.txt`).
Test kiracısı: davet `POST /sistem/davetler` (rastgele sistem anahtarı, değer basılmadı) → `POST /auth/kayit` **201**.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | Yeni kayıt kiracısı modu | BASIT | `tenants.varsayilan_arayuz_modu = BASIT`; `/auth/ben` → `arayuzModu=null, etkinArayuzModu=BASIT, rol=SAHIP` | SQL + HTTP 200 |
| 2 | Eski kiracılar | değişmez | diğer 23 satırın id+modu kayıt öncesi listeyle `diff` → AYNI | SQL |
| 3 | `GET /pano/guncel` (ibare yok) | 200, `iscilikIbaresi=null` | 200 · 7 satır · null | HTTP |
| 4 | `PATCH /pano/iscilik-ibaresi {}` | 400 alanlı | 400 `alanlar.iscilikIbaresi=["Güncellenecek alan gönderilmedi"]` | HTTP |
| 5 | `PATCH … {"iscilikIbaresi":"BELKI"}` | 400 | 400 "İşçilik ibaresi DAHIL ya da HARIC olmalıdır"; `Accept-Language: en` → "The workmanship notice must be…" | HTTP |
| 6 | `PATCH … {"iscilikIbaresi":"DAHIL"}` | 200, panoda DAHIL | 200 · `iscilikIbaresi=DAHIL` | HTTP |
| 7 | SQL | ayar satırı + denetim | `kiraci_ayarlari: pano.iscilik_ibaresi=DAHIL`; `denetim_kayitlari: PanoKalemAyari / pano.iscilik_ibaresi / "Vitrin işçilik ibaresi: DAHIL"` | SQL |
| 8 | `PATCH … null` | 200, ibare silinir | 200 · null | HTTP |
| 9 | Tarayıcı, ibare yokken `/vitrin` | düğme kilitli + sebep | `BUTTON / disabled / title "Önce işçilik ibaresini seçin"` | CDP |
| 10 | Tarayıcıda **İşçilik dahil**'e GERÇEK tıklama | ibare yazılır, bant, düğme bağlantıya döner | "Satış fiyatlarına işçilik bedeli dahildir." · bant "İşçilik ibaresi kaydedildi: …" · `A /vitrin/ekran` | CDP |
| 11 | Bağlantıya tıklama | kabuksuz dükkân ekranı | `location=/vitrin/ekran`, kenar çubuğu/üst bar YOK, 7 satır + ibare | CDP · foto `vtam-tikla__PRO_aydinlik_1280.png` |
| 12 | Satış: sepette ürün (Peşin, 13.107,33 ₺) → müşteri "Toptancı" (Toptan liste) | bant | "Müşteri değişti — … 1 kalemin fiyatı değişiyor." | CDP |
| 13 | **Basmadan** kaydet | eski fiyat SQL'e | `hareketler SATIS tl_tutari = 13107.33` (cari = Toptancı) = ekrandaki | SQL |
| 14 | **Fiyatları güncelle** → kaydet | yeni liste fiyatı SQL'e | ekran 12.469,43 ₺ · bant "1 kalemin fiyatı yeni listeye göre güncellendi." · `SATIS tl_tutari = 12469.43` | SQL |
| 15 | Sunucu teklifi (aynı kur kaydı) | 13107.33 / 12469.43 | varsayılan 13107.33 · cari 12469.43 | HTTP |

(12–15 iki kez koşuldu: ilk turda ve bant bileşene taşındıktan sonra SON KODLA yeni bir test kiracısında — aynı sonuç.)

### Gerçek tarayıcı ölçümü (sikisma-olc.js `__olcTam`, başsız Chrome, tam sayfa)

| Rota | Genişlik | Mod | Tema | K1 gizli-görünen | K3 taşan/kırık | K3c | K5 TRY | K9 binen | K11 hata | yatay | K2 % | Not |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /vitrin | 1280 · 375 · 1920 | PRO | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 98 · 97 · 99 | ibare seçili |
| /vitrin | 1280 · 375 | BASIT | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 98 · 97 | |
| /vitrin/ekran | 1280 · 375 · 1920 | PRO | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | — | kabuksuz |
| /vitrin/ekran | 1280 · 375 | BASIT | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | — | |
| /vitrin, /vitrin/ekran (ibare YOK) | 1280 · 375 · 1920 | (BASIT kiracı) | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | — | kilit bandı, fiyat yok |
| /islemler (bant açık) | 1280 · 375 | PRO | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 98 · 97 | son kod, 2 kez |
| /islemler (bant açık) | 1280 · 375 | BASIT | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | **69** · 97 | 69 = BASİT tezgâh fişinin mevcut iki sütunlu yerleşimi (sepet + ödeme), bu paket değiştirmedi |

Ham ölçüm: `/private/tmp/kuyumcu-serit/EKSIK-KUCUK/olcum.jsonl`, `olcum2.jsonl` (son kod), ekran görüntüleri `foto/`, `foto2/`. Göz ile bakılan 4 görüntü: `vtam__PRO_aydinlik_1920.png` (dükkân ekranı, ibare altta), `vitrin__BASIT_karanlik_375.png`, `islem-bant__PRO_aydinlik_1280.png`, `islem-bant__BASIT_karanlik_375.png`.

### Temizlik
- API (5166) ve vekil (5167) yalnız kendi PID'leriyle durduruldu; `lsof` boş. Kendi Chrome profillerim kapalı.
- İki test kiracısı `zsh kiraci-temizle.sh <id> --uygula` (rc 0) ile silindi; davet satırları SQL ile silindi (`row_count=1` ×2). Kiracıya ait `tenants/hareketler/kiraci_ayarlari` satırı 0.
- **tenants: başta 23, sonda 23.** (Liste karşılaştırmasında başka şeritlerin bir kiracısı silinmiş, bir başkası eklenmiş; onlara dokunulmadı.)

## 7. DoD sözleşmesi

| Varlık / kayıt | Oluşturma | Düzenleme | Silme / pasif | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|---|
| Vitrin işçilik ibaresi (`kiraci_ayarlari: pano.iscilik_ibaresi`) | `PATCH /pano/iscilik-ibaresi` | aynı uç (DAHIL↔HARIC); alan yoksa 400 | `null` → satır silinir (tercih satırı, finansal değil) | `[ModulYetkisi("kur")]` + `[RolGerekli("SAHIP","MUDUR")]` | `YazmaKaydet` | İki düğme (seçili `aria-pressed`); tezgâhtar seçemez, sebebi yazar. Ekranda "kaldır" düğmesi bilinçli YOK: yasa bir ibare ister, kaldırmak yalnız dükkân ekranını kilitler. |
| Tenant `varsayilan_arayuz_modu` (yeni kayıtta BASIT) | `POST /auth/kayit` | mevcut `PATCH /admin/tenant` (değişmedi) | — | mevcut | mevcut | Ayarlar › Görünüm (mevcut) |
| Satış kalemi fiyatı (liste güncelleme) | ekranda, kaydedilmemiş | "Fiyatları güncelle" / satır içi düzeltme (mevcut) | kalem sil (mevcut) | — | satış kaydı mevcut yolla | bant |

Yeni finansal varlık YOK; deftere yazan yol değişmedi (HareketFabrikasi).

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| B2-10 ibare metni birincil kaynaktan | Karşılandı (fıkra no TEYİTSİZ) | § 2 RG 29044 |
| Vitrin tam ekran kabuksuz (/ekranlar kalıbı) | Karşılandı | `AnaYerlesim.VitrinEkranRotasi`, CDP `kabukVar=false` |
| Kâğıt/ekran kuralı | Karşılandı | EK_B04 (ibare `yazdirma-gizle` taşımaz) |
| Gerçek Chrome 1920 + 1280 görüntü, ibare görünür | Karşılandı | `vtam__PRO_*_1920/1280.png`, ölçüm 0 |
| B2-11 yeni kiracı BASIT (SQL), eskiler değişmez (önce/sonra) | Karşılandı | § 6 #1–2, EK_H06 |
| B2-12 güncelle düğmesi + iki yol canlı SQL | Karşılandı | § 6 #12–15 |
| SQL'e giden fiyat = ekrandaki | Karşılandı | 13107.33 / 12469.43 |
| Mutasyon ≥ 3 kırmızı, cmp AYNI | Karşılandı (8 mutasyon) | § 4 |
| Chrome 1280 + 375 · BASIT + PRO · açık + koyu | Karşılandı | § 6 tablo |
| Build 0 hata | Karşılandı (6 uyarı başka şeritlerin test dosyalarında: SUBEKAPSAMA, KesifP2, SonKalanlar, Yıldız2) | `build5.txt` |
| denetim TEMİZ | Paket sayaçları ✓; ağaçta 2 ✗ başkasının | § 5 |
| Tam süit 0 yeni kırmızı | Karşılandı (fark 0) | § 3 |
| Kılavuz birebir + kilavuz.html | Karşılandı | § 1, KilavuzUretim testleri yeşil |

## 9. Değişen dosyalar (bu paket)

- `dotnet/Kuyumcu.Contracts/Pano/PanoSozlesmeleri.cs` (+`IscilikIbaresi`, `IscilikIbaresiDegerleri`)
- `dotnet/Kuyumcu.Api/Modules/Pano/PanoModulu.cs` (ibare oku/yaz, PATCH ucu)
- `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` (kayıtta BASIT)
- `dotnet/Kuyumcu.Api/Data/Entities.cs` (yalnız açıklama)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.EksikKucuk.cs` (YENİ) · `SunucuIletiSozlugu.Veri.cs` (+1 çift)
- `dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs` · `dotnet/Kuyumcu.Web/Layout/{AnaYerlesim.razor, MenuTanimi.cs, RotaBasliklari.cs}`
- `dotnet/Kuyumcu.Web/Pages/Vitrin.razor` · `Pages/VitrinEkrani.razor` (YENİ) · `Pages/Islemler.razor` · `Pages/Islemler.ListeGuncelle.cs` (YENİ) · `Bilesenler/Fis/FisListeDegisti.razor` (YENİ)
- `dotnet/Kuyumcu.Web/wwwroot/css/eksik-kucuk.css` (YENİ) · `wwwroot/index.html` · `wwwroot/kilavuz.html` (üretildi)
- `docs/KULLANIM_KILAVUZU.md`
- `dotnet/Kuyumcu.Tests/EksikKucukTestleri.cs` (YENİ) · `RotaBaslikTestleri.cs` (51→52) · `P37cPanelVeKapsamTestleri.cs` (51→52, 48→49)
- `dotnet/denetim.sh` (Pages ve ROTA tavanı 51→52, gerekçeli) · `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json` · `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js`

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): `Vitrin.razor`, `VitrinEkrani.razor` (yeni), `Islemler.razor` (yalnız MusteriDegisti, TeklifAl satırı, KalemEkle izi, KalemParcasi bant satırı).
Not: `git status` bu deponun çoğu dosyasını izlenmeyen (`??`) gösteriyor (tek "first commit" var); liste yukarıda elle doğrulandı. `dotnet/denetim.sh.mutbak` (0 bayt, 18.09 tarihli) bu paketin DEĞİL.

## 10. Açık kalanlar (sahipleriyle)

1. `SonKalanlarTahsilatEkranTestleri.SKC01` ×2 — Cariler tahsilat çekmecesi; `Pages/Cariler.razor` 18:47'de değişti (YASAL-KAPI-2 şeridi).
2. `RaporDortTestleri.RD_KH01/RD_KH02` — "Rapor dönemi en çok 400 gün" (GOC-D) × RAPOR-4 test aralıkları.
3. `FaturaEkranTestleri.Y4C_K12(basit: False)` — "805" rastgele GUID'e çarpıyor (Fatura/Y4-C test sahibi; öneri `\b805\b` ya da `TextContent`).
4. `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter…` — yük altında kararsız (SATIS-3).
5. denetim: `P-30 DEMO_INSAN_SURESI` (kurucu/orkestratör: kronometreli demo provası), `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`).
6. Paralel şerit riski: sayfa/rota sayısı (52) başka bir şerit de sayfa eklerse orkestratör birleştirmede yeniden ölçmeli.

## 11. Kurucu / mali müşavir

1. **Mali müşavir:** Fiyat Etiketi Yönetmeliği md.6'daki hüküm güncel konsolide metinde kaçıncı fıkra (6/2 mi 6/3 mü)? Ekranda yalnız "md.6" yazıyor.
2. **Mali müşavir / kurucu:** Kotasyon panosundaki (gram, çeyrek, yarım, tam, ata, 22 ayar bilezik) satış fiyatlarında işçilik "dahil" mi sayılmalı? Program seçimi dükkâna bırakıyor ve seçmeden dükkân ekranını açmıyor; varsayılan bilerek YOK.
3. **Mali müşavir:** Ekrandaki cümleler ("Satış fiyatlarına işçilik bedeli dahildir." / "…dahil değildir.") yönetmeliğin "ayrıca belirtilir" şartını karşılıyor mu, yoksa vitrinde ayrıca kâğıt ilan da gerekir mi?
