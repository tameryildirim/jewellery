# PAKET P-17 — ORTAK ÇIKTI ŞABLONU + PANO/VİTRİN · UYGULAMA RAPORU

**Tarih:** 2026-09-21 · **Dalga:** DALGA 4 · **Kimlikler:** Y-6, Y-7 · **Sürüm:** 1.0
**Uygulayıcı:** Taşkın (backend) + Ertuğ (frontend) rolü, tek el
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 428-440

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayım yok)

| Önkoşul | Diskte var mı | Ölçüm |
|---|---|---|
| **P-16** | VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-16-uygulama-raporu.md` · denetim.sh `# >>> P-16` bloğu (satır 2907-2978) canlı |
| **P-01** (R-11 kararı) | VAR | `PAKET-P-01-uygulama-raporu.md` satır 31 ve 106: **R-11 KAPANDI**, "paket 5 etiket/belge şablon motoru" → **1.1**; 1.0 karşılığı P-17'nin **SABİT** şablonu |

**Sonuç:** kapsam daraltılmadı. R-11 kararı "tasarımcı/şablon motoru" demediği için risk R-11 gerçekleşmedi; yalnız **sabit** A4 şablonu yazıldı (plan § 2 P-17 Risk satırının gereği).

### Spesifikasyonun kodla doğrulanması (ADIM 1)

Plan "dosya ayak izi"ndeki her iddia bugün koda karşı ölçüldü:

| Plan iddiası | Bugünkü gerçek | Durum |
|---|---|---|
| `Pages/PusulaBelgesi.razor:213` window.print | satır **213** — doğru | ✅ |
| `Pages/IslemDefteri.razor:307` window.print | satır **307** — doğru | ✅ |
| `Pages/UrunEtiketleri.razor:263` window.print | satır **256** (7 satır kaymış) | ⚠ kayma, hedef aynı |
| `Modules/Pano/PanoFiyatServisi.cs:75` | dosya 205 satır; `PanoFiyatServisi` sınıfı satır **75**'te başlıyor | ✅ |
| `Modules/ModulKayit.cs` | var, 91 satır | ✅ |
| `Modules/Cari/CariModulu.cs:864` | satır 864 civarı **TahsilatGir** — tahsilat makbuzunun kaynağı | ✅ |
| `Bilesenler/Yazdirma/` | **YOKTU** (plan "YENİ" diyor) | ✅ oluşturuldu |

**Ek ölçüm (planda yoktu, Anayasa md.1):** `PanoFiyatServisi` saf hesap olarak vardı ve `PanoFiyatTestleri.cs`'te **20 testi geçiyordu**, ama **hiçbir uçtan çağrılmıyor, hiçbir ekranda görünmüyordu**. Y-7 bu yüzden "uç + ekran yok" kalemidir, "hesap yok" kalemi değil. Hesap **yeniden yazılmadı**, uca ve ekrana bağlandı.

**Mevzuat işareti:** bu pakette DOĞRULANMALI/VARSAYIM işaretli hiçbir mevzuat hükmü yoktur (çıktı biçimi ve pano fiyatı yasal zorunluluk taşımaz; VUK md.234 belgesi P-17 kapsamı dışında, mevcut `PusulaBelgesi.razor`'dadır ve **düzeni değişmedi**).

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-6** — Ortak yazdırma / PDF şablonu | **KAPANDI** | Sözleşme: `Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs` · Sunucu: `Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs` (`CiktiServisi` + `CiktiController`, GET `/api/v1/cikti/{tur}/{id}`) · Kâğıt: `Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor` + `YazdirDugmesi.razor` · Ekran: `Kuyumcu.Web/Pages/Cikti.razor` · Test: `P17HttpTestleri.Dort_belge_turu_de_AYNI_sozlesmeyi_doldurur`, `P17CiktiEkranTestleri.Belge_ORTAK_sablondan_cizilir_ve_ucu_cagirir` · Canlı: § 6 adım 13-17 (HTTP 200/400/404) · Sayaç: `ORTAK_SABLON_DISI_PRINT = 0` |
| **Y-7** — Pano fiyat ucu + vitrin/kur panosu | **KAPANDI** | Sunucu: `Kuyumcu.Api/Modules/Pano/PanoModulu.cs` (`PanoAyarServisi` + `PanoController`; GET `/pano/guncel`, GET/PATCH `/pano/ayarlar[/{kod}]`, POST `/pano/ayarlar/{kod}/varsayilana-don`) · Modül kaydı: `ModulKayit.cs:94` · Ekran: `Kuyumcu.Web/Pages/Vitrin.razor` (`@page "/vitrin"`) · Test: `P17HttpTestleri` 12 test + `P17VitrinEkranTestleri` 8 test · Canlı: § 6 adım 1-9 · Sayaç: `PANO_AYAR_KIRACIDAN`, `PANO_MARJ_SIZINTISI`, `PANO_IKINCI_HESAP_YOLU` |

Devredilen kimlik **yoktur**.

---

## § 2 — YAPILANLAR

### Y-6 — Ortak A4 çıktı şablonu

**Neden gerekliydi (bugün ölçüldü):** satış fişi, tahsilat makbuzu, emanet teslim belgesi ve cari ekstresi için **hiçbir çıktı yoktu** — kuyumcu müşterisine verecek kâğıdı üretemiyordu. `window.print` üç ayrı dosyada **üç ayrı davranışla** çağrılıyordu (biri hatayı yutuyor, biri banda yazıyor, biri hiç bakmıyordu).

1. **Tek sözleşme** — `Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs`: `OrtakBelge` (+ `CiktiKimlik`, `CiktiTaraf`, `CiktiBolum`, `CiktiSatir`, `CiktiTutar`) ve kapalı liste `BelgeTurleri` (SATIS_FISI · TAHSILAT_MAKBUZU · EMANET_TESLIM · CARI_EKSTRE). Contracts'a konması **derleyici kilidi** verir (denetim.sh `SOZLESME_KILITSIZ_TIP` ölçütü (a)); razor'da açılan yeni istemci record'u **yoktur**.
2. **Tek uç** — `GET /api/v1/cikti/{tur}/{id}`. Salt okur; deftere yazmaz. Tek yazdığı `DenetimServisi.OkumaKaydet` (KVKK md.12) — denetim servisi **yeniden yazılmadı, çağrıldı**.
3. **Tek kâğıt düzeni** — `Bilesenler/Yazdirma/A4Belge.razor`. Dört tür de bu bileşenden çıkar.
4. **Tek yazdırma çağrısı** — `Bilesenler/Yazdirma/YazdirDugmesi.razor`. `PusulaBelgesi.razor` ve `IslemDefteri.razor` kendi `window.print` çağrılarını **bıraktı** ve bu bileşeni kullanıyor.
5. **Erişim yolu** — `IslemDefteri.razor`'a satır başına "Belge" bağlantısı eklendi (`/cikti/{tur}/{id}`); karşılığı olmayan hareket türünde düğme **çizilmiyor**, hücrede "—" duruyor (UX-S30).

### Y-7 — Vitrin / kur panosu

1. **Uç** — `PanoController`: `guncel` (tüm roller), `ayarlar` GET/PATCH ve `varsayilana-don` (**MUDUR+**, marj ticari sırdır).
2. **Kalıcılık** — **mevcut** `kiraci_ayarlari` tablosu, kalem başına bir satır: `anahtar = "pano_kalem.<PARITE_KODU>"`, `deger = "<sira>|<alisMarj>|<satisMarj>|<yuvarlama>|<aktif>|<etiket>"` (InvariantCulture). `deger` 200 karakter olduğu için tek JSON kullanılamazdı.
3. **Ekran** — `Pages/Vitrin.razor`: üstte müşteriye dönük pano (**marj yok**), altında atlanan kalemler + sebepleri, en altta MUDUR+ marj/yuvarlama tablosu ve `Cekmece` ile düzenleme + "Varsayılana dön".
4. **Erişim yolu** — `Kur.razor`'a `#kur-vitrin-ac` bağlantısı; `MenuTanimi.MenusuzRotalar`'a gerekçeli satır.

---

## § 3 — DoD SÖZLEŞMESİ (Bertan koşulu — zorunlu bölüm)

| Varlık / kayıt türü | Düzenleme ucu | Silme / pasife alma | Yetki | Denetim | Ekran eylemi |
|---|---|---|---|---|---|
| **PanoKalemAyari** (`kiraci_ayarlari` satırı) | `PATCH /pano/ayarlar/{kod}` — gönderilmeyen alan korunur, `null` varsayılana döner | `POST /pano/ayarlar/{kod}/varsayilana-don` (ayar satırı silinir; **finansal varlık değildir**, bir tercih satırıdır) | `[ModulYetkisi("kur")]` + `[RolGerekli("SAHIP","MUDUR")]` | `DenetimServisi.YazmaKaydet` çağrılır (varlık `PanoKalemAyari`) | `#vitrin-duzenle-{kod}` → `Cekmece` · `#pano-varsayilana-don` |
| **OrtakBelge** (çıktı) | **YOK — salt okunur** | — | `[RolGerekli]` 4 rol + serviste **tür başına** modül kapısı | `DenetimServisi.OkumaKaydet` (varlık `CiktiBelgesi`) | `DegismezIpucu Varlik="CiktiBelgesi"` — gerekçe: "deftere yazılmış bir kayıttan türetilir"; doğru yol: kaynağındaki düzeltme |

**Salt okunur olanın gerekçesi:** belge bir *görünüm*tür, bir kayıt değildir. Belgeyi düzenlemek defteri değiştirmez, yalnız kâğıdı defterden ayırır — bu Anayasa md.1'e aykırı olurdu. Düzeltme kaynağında (işlem defteri / cari kartı) yapılır ve belge kendiliğinden düzelir.

**Finansal varlıkta hard DELETE:** bu pakette finansal varlık **oluşturulmadı ve silinmedi**. Tek DELETE `kiraci_ayarlari` tercih satırıdır.

### DoD EK (kurul tur 1) karşılığı
1. **Her düğme ya istek atar ya disabled + sebep** — `P17CiktiEkranTestleri.Her_dugme_ya_istek_atar_ya_disabled_tasir` ve `P17VitrinEkranTestleri.Her_dugme_ya_istek_atar_ya_disabled_tasir` her `<button>` için zorluyor.
2. **Tek düzenleme kalıbı** — çok alanlı düzenleme `Cekmece`; `ModalPencere` **kullanılmadı**; ikinci "duzenlenenId" kalıbı açılmadı (denetim.sh TEMİZ).
3. **Boş/geçersiz gönderim** — `#pano-sira` aralık dışıysa alan altı mesaj, istek gitmez (`Gecersiz_sira_ALAN_ALTI_mesaj_verir_ve_istek_GITMEZ`); sunucuda boş gövde 400.
4. **Yıkıcı eylem** — bu pakette yıkıcı eylem yok; "Varsayılana dön" geri dönülebilir (yeniden PATCH) ve zaten varsayılandaysa **kapalı + sebep yazılı**.

### DoD EK 2 (mühür 2) — Dokunulan sayfalar
`Pages/Cikti.razor` (YENİ) · `Pages/Vitrin.razor` (YENİ) · `Pages/IslemDefteri.razor` · `Pages/PusulaBelgesi.razor` · `Pages/Kur.razor`.
İlk dört dosya **P-17'nin kendi plan listesindedir**. `Pages/Kur.razor` planda yoktur: gerekçe — `/vitrin` rotasının erişim yolu gerekiyordu, menü (P-14 mührü, 36 bağlantı) genişletilmedi, pano kurun türevi olduğu için kur ekranından açılır. Değişiklik **tek bir `<NavLink>`**tir. Storno taklidi yapılmadı; fiyat/teklif/tahsilat metni üretilmedi.

---

## § 4 — SAPMALAR VE GEREKÇELERİ

| # | Sapma | Gerekçe |
|---|---|---|
| 1 | **Dört belge için dört sayfa değil, TEK sayfa** (`/cikti/{Tur}/{Id}`) | Plan "tek A4 şablonundan üretilir" diyor. Dört sayfa dört kâğıt düzeni demektir ve paketin var oluş sebebini ilk günde bozardı. `Pages/*.razor` sayacı 42 → **44** (45 değil). |
| 2 | **Migration ÜRETİLMEDİ** | Plan "ŞARTLI — `PanoKalemAyari` kalıcı ayar tablosu **gerekiyorsa**" diyor ve DoD "marj/yuvarlama kaydı **kiracı ayarından** gelir" diyor. `kiraci_ayarlari` mevcut, `IKiraciVarlik`, global filtre + yazma denetçisi + denetim izi hazır. Yeni tablo aynı üç mekanizmayı sıfırdan kurardı. **Şart gerçekleşmedi.** SQL kanıtı § 6. |
| 3 | `CiktiController`'da sınıf düzeyinde `[ModulYetkisi]` **yok** | Kapı belge türüne göre değişir (fiş=islem · makbuz/ekstre=cari · emanet=emanet). Tek nitelik ya çok geniş (fail-open) ya çok dar olurdu. Kapı `CiktiServisi.Belge` içinde **tür başına** sorulur (P-15 `AramaServisi` deseni) ve uçta yine `[RolGerekli]` durur — P-09 `OZNITELIKSIZ_UC = 0` korunuyor. |
| 4 | `/cikti/{Tur}/{Id}` rotası `RotaModulleri`'nde **null** | Yukarıdaki (3)'ün rota karşılığı; haritada gerekçesi yazılı. `ROTA_YETKI_ESLESMEZ = 0` korunuyor. |
| 5 | `Pages/Kur.razor`'a dokunuldu (planda yok) | § 3 "Dokunulan sayfalar"da gerekçelendirildi. |
| 6 | Dört sayaç tabanı yükseltildi: `P02_ROTA_BEKLENEN` 42→44, `Pages/*.razor dosya sayısı` 42→44, `qa-turu.js ROTA_KAPSAM_BEKLENEN` 42→44, `olcum-2026-09-18.json ROTA_KAPSAM` 42→44 | Bunlar **tavan** değil **gerçek sayıya eşitlik** sayaçlarıdır (`-eq`); plan DoD'u iki yeni ekranı zorunlu kılıyor. Yükseltme **taze ölçümle** yapıldı (`grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' \| wc -l` → 44), belgeden kopyalanmadı. Cırcır (yalnız-azalan) sayaçların hiçbiri yükseltilmedi. |
| 7 | `IslemDefteri.razor`'daki `#id-yazdir-kilit` satırı kaldırıldı | Kilit sebebini artık `YazdirDugmesi` kendi basıyor (`#id-yazdir-kilit` id'si **korundu**: bileşen `{Id}-kilit` üretiyor). Mevcut element id'leri değişmedi. |
| 8 | `UrunEtiketleri.razor`'a **dokunulmadı** | Plan DoD'unun açık hükmü: termal/barkod ailesi ortak A4 şablonuna **girmez**, gerekçeli beyaz listede kalır ("3 → 0 hedefi yanlıştı" — hasım bulgusu). |

**Plan ile koşullar arasında çelişki:** "KAPSAM KİLİDİ: yeni ekran/modül/özellik açma" koşulu ile plan bloğunun DoD'u (dört yeni belge çıktısı + pano ekranı) çelişir. **PLAN üstündür** (koşulun kendi hükmü). Çelişki bu satırla rapora yazıldı; açılan ekran sayısı DoD'un gerektirdiği asgari sayıda (2) tutuldu.

---

## § 5 — MIGRATION

**ÜRETİLMEDİ — gerekmedi.** Gerekçe § 4 sapma 2.

Kanıtlar (canlı `kuyumcu_net`):
- `SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='kuyumcu_net' AND table_name LIKE 'pano%';` → **0** (yeni tablo açılmadı)
- `SELECT COUNT(*) FROM __EFMigrationsHistory;` → **56** (paket öncesi ve sonrası aynı; P-17 migration eklemedi)
- Ayar gerçekten yazılıyor: `SELECT anahtar, deger FROM kiraci_ayarlari WHERE tenant_id='<test>';` → `pano_kalem.GRAM  0|-30|45|50|1|Gram (has)`

---

## § 6 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

Test kiracısı **PAKETP-17-TEST Kuyumculuk** açıldı (sistem daveti ile), iş sonunda silindi.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /api/v1/pano/guncel` | 200, gerçek kotasyonlu satırlar | **200** | `kaynak=ALTINKAYNAK`, 7 satır (GRAM 6770/6870, CEYREK_YENI 10890/11530, …) |
| 2 | `GET /api/v1/pano/ayarlar` | 200, 22 kalem / 7 aktif | **200** | `kalem:22 aktif:7 yuvarlama:[0,1,5,10,50,100] tavan:16` |
| 3 | `PATCH /pano/ayarlar/GRAM` (marj −30/+45, yuvarlama 50, etiket) | 200, `ozel=true` | **200** | `{'alisMarjTl': -30, 'satisMarjTl': 45, 'yuvarlamaAdimi': 50, 'etiket': 'Gram (has)', 'ozel': True}` |
| 4 | `PATCH` **yalnız** `{"sira":0}` | Diğer alanlar **DEĞİŞMEZ** | **200** | marj −30/+45, yuvarlama 50, etiket "Gram (has)" **korundu** |
| 5 | `PATCH` boş gövde `{}` | 400 | **400** | `"Güncellenecek alan gönderilmedi"` |
| 6 | `PATCH yuvarlamaAdimi=7` | 400 | **400** | `"Geçersiz yuvarlama adımı: 7. Seçilebilir: 0, 1, 5, 10, 50, 100"` |
| 7 | Ayarın **panoya etkisi** | alış aşağı, satış yukarı, 50'ye yuvarlı | **200** | GRAM: 6770−30=6740 → **6700** (aşağı) · 6870+45=6915 → **6950** (yukarı) |
| 8 | `POST /pano/ayarlar/GRAM/varsayilana-don` | 200, `ozel=false`, marj 0 | **200** | `{'alisMarjTl': 0, 'satisMarjTl': 0, 'yuvarlamaAdimi': 10, 'etiket': None, 'ozel': False}` |
| 9 | Aynı çağrı ikinci kez | 404 | **404** | `"Bu kalemin kiracı ayarı yok: GRAM"` |
| 10-12 | Cari + kasa + veresiye 5.000 TL + tahsilat 1.500 TL | 200/201 | **201/200/200/200** | SQL: `hareketler` → `VERESIYE 5000.00`, `TAHSILAT -1500.00` |
| 13 | `GET /cikti/TAHSILAT_MAKBUZU/{hareketId}` | 200, ortak sözleşme dolu | **200** | `TAHSİLAT MAKBUZU` · imza `['Tahsil eden','Ödeyen']` · düzenleyen + karşı taraf dolu · tutar `-1500.00 TRY` · `uyari=None` |
| 14 | `GET /cikti/CARI_EKSTRE/{cariId}` | 200, 2 satır + sınır notu | **200** | `CARİ EKSTRESİ` · 2 satır · not: *"Sunucu sınırı: en fazla 200 hareket (sınıra değilmedi)."* · `TL bakiye 3500.00 TRY` + `Has gram bakiye 0 HAS_GRAM` |
| 15 | `GET /cikti/EMANET_TESLIM/{tahsilatId}` | 400 | **400** | `"Bu hareket 'EMANET_TESLIM' belgesi üretmez (hareket türü: TAHSILAT). Beklenen: EMANET_GIRIS, EMANET_CIKIS"` |
| 16 | `GET /cikti/SACMA/{id}` | 400 | **400** | `"Bilinmeyen belge türü: SACMA. Kabul edilenler: SATIS_FISI, TAHSILAT_MAKBUZU, EMANET_TESLIM, CARI_EKSTRE"` |
| 17 | `GET /cikti/TAHSILAT_MAKBUZU/<olmayan>` | 404 | **404** | `"Hareket bulunamadı"` |
| 18 | Denetim izi | belge okuma + ayar yazma | ✅ | `CiktiBelgesi OKUMA 2` · `PanoKalemAyari EKLEME 3` |

**Temizlik:**
- API PID durduruldu · `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** · 5199 de **0 satır** (dokunulmadı).
- Test kiracısı `information_schema`'dan `tenant_id` kolonu olan **tüm** tablolardan silindi + davet satırı silindi.
- Doğrulama: `hareketler=0 · cariler=0 · denetim_kayitlari=0` (test kiracısı için) · **`tenants` = 8 (başlangıçtaki değer)** · `QA-TEST-%` kiracı sayısı **3** (dokunulmadı).
- Geçici sır dosyaları (`tok.txt`, `sifre.txt`, `siskey.txt`, `kayit.json`, `davet.json`) `printf '' >` ile boşaltıldı.

---

## § 7 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket öncesi (bu oturumda ölçülmedi; türetilmiş) | 3309 | 3309 | 0 | 0 |
| **Yalnız P-17 filtresi** | **48** | **48** | **0** | 0 |
| **Tam süit (sonra)** | **3357** | **3357** | **0** | 0 |

Tam süit komutu ve TRX: `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory <scratchpad>/trx-pP-17` → `Test Çalıştırması Başarılı.` · `Geçti: 3357`.

**Yeni test sayısı: 48** — `Kuyumcu.Tests/P17CiktiPanoTestleri.cs` (saf 6 + HTTP 22) ve `Kuyumcu.Tests/P17EkranTestleri.cs` (bUnit 20).

Süit sırasında kırılan ve **düzeltilen** mevcut testler (kod değişikliğiyle değil, taban güncellemesiyle): `RotaBaslikTestleri.Yansima_TAM_42_rota_bulur`, `OlcumKapisiTestleri` ×3 — hepsi rota sayısı 42→44 sabitleri; § 4 sapma 6.

---

## § 8 — MUTASYON KANITI

Yöntem: kaynak `<scratchpad>/mut-yedek/P-17/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| **M17-1** | `CiktiServisi`: tür başına yetki kapısı `if (false)` yapıldı | `P17HttpTestleri.Emanet_modulu_kapali_rolde_emanet_belgesi_403_doner` | ✅ birebir |
| **M17-2** | `CiktiServisi`: `if (!beklenen.Contains(h.Tur))` → `if (false)` | `P17HttpTestleri.Tahsilat_hareketinden_SATIS_FISI_istenirse_400_doner` | ✅ birebir |
| **M17-3** | `CiktiServisi`: storno uyarısı koşulu `false` | `P17HttpTestleri.Storno_edilmis_fis_belgesinde_UYARI_vardir` | ✅ birebir |
| **M17-4** | `CiktiServisi`: ekstre "sınıra değilmedi" notu `null` | `P17HttpTestleri.Cari_ekstresi_sunucu_sinirini_YAZAR` | ✅ birebir |
| **M17-5** | `PanoAyarServisi`: PATCH `AlisMarjTlVerildi` bayrağı atlandı (tam gövde ezmesi) | `P17HttpTestleri.PATCH_gonderilmeyen_alani_DEGISTIRMEZ` | ✅ birebir |
| **M17-6** | `PanoAyarServisi`: `if (!verildi) throw` → `if (false)` | `P17HttpTestleri.Bos_PATCH_govdesi_400_doner` | ✅ birebir |
| **M17-7** | `PanoAyarServisi.Diz`: `InvariantCulture` → `tr-TR` | `P17SafTestleri.Ayar_CSV_gidis_donus_ayni_degeri_verir` | ✅ birebir |
| **M17-8** | `PanoController`: `[RolGerekli("SAHIP","MUDUR")]` silindi | `P17HttpTestleri.Marj_ayarlari_TEZGAHTARA_kapali_pano_ACIK` | ✅ birebir |
| **M17-9** | `A4Belge`: storno uyarısına `yazdirma-gizle` eklendi | `P17CiktiEkranTestleri.Storno_uyarisi_KAGIDA_basilir_ekranda_gizlenmez` | ✅ birebir |
| **M17-10** | `YazdirDugmesi`: `disabled="@(...)"` satırı silindi | `P17CiktiEkranTestleri.Yazdir_dugmesi_belge_yokken_KAPALI_ve_sebebi_YAZILI` | ✅ birebir |
| **M17-11** | `A4Belge.KimlikEksik` → `false` | `P17CiktiEkranTestleri.Isletme_kimligi_eksikse_DUZELTME_YOLU_gosterilir` | ✅ birebir |

**Dürüstlük notu (Anayasa md.1):** M17-11'in **ilk denemesi** (`false && ...` önekiyle) hiçbir testi kırmadı. Sebep test açığı değil, **mutasyonun etkisizliğiydi**: `&&`, `||`'dan önce bağlandığı için OR zincirinin geri kalanı hâlâ `true` dönüyordu. Mutasyon ifadenin tamamını `false` yapacak biçimde **yeniden uygulandı** ve test kırmızıya döndü. İlk deneme raporda gizlenmedi.

Son mutasyondan sonra: dört dosya `cmp` ile birebir aynı · `dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı**.

---

## § 9 — DERLEME VE DENETİM

**Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (tüm projeler).

**`dotnet/denetim.sh` → `DENETİM TEMİZ.`** (çıkış kodu 0). P-17 bloğunun çıktısı:

```
── P-17: ORTAK ÇIKTI ŞABLONU + VİTRİN PANOSU ──────────────────────────
  ✓ P-17: ORTAK_SABLON_DISI_PRINT                  0 (beyaz liste: 2 — ortak düğme + termal etiket)
  ✓ P-17: PRINT_BEYAZ_LISTE_BAYAT                  0
  ✓ P-17: A4_KAGIT_DUZENI_SAYISI                   2/2 (tavan YALNIZ azalır)
  ✓ P-17: ORTAK_BELGE_TURU                         4 (fiş · makbuz · emanet · ekstre)
  ✓ P-17: PANO_AYAR_KIRACIDAN                      kiraci_ayarlari 5 · modül kaydı 1
  ✓ P-17: PANO_IKINCI_HESAP_YOLU                   0 (hesap YALNIZ PanoFiyatServisi'nde)
  ✓ P-17: PANO_MARJ_SIZINTISI                      0 (marj ticari sırdır, panoya inmez)
```

Tek `⚠` satırı P-17'nin değil: `P-02: QA_TUR_SURESI_OLCUM (BK-39)` — devri 2026-09-30'a dek açık.

### Sayaç: `ORTAK_SABLON_DISI_PRINT` (plan § 2 P-17 "Sayaçlar")

**Ölçüm komutu (betiğe yorum olarak yazıldı, taban TAZE ölçüldü):**
```
grep -rlE 'InvokeVoidAsync\("window\.print"\)' --include='*.razor' \
  Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout | sort
```
| | Dosyalar | Sayı |
|---|---|---|
| **ÖNCE** | `Pages/PusulaBelgesi.razor` · `Pages/IslemDefteri.razor` · `Pages/UrunEtiketleri.razor` | **3** |
| **SONRA** | `Bilesenler/Yazdirma/YazdirDugmesi.razor` · `Pages/UrunEtiketleri.razor` | **2** |
| **Beyaz liste dışı** | — | **0** |

Gerekçeli beyaz liste: (a) `YazdirDugmesi.razor` — ortak şablonun kendi çağrısı; (b) `UrunEtiketleri.razor` — termal/barkod ailesi (plan DoD'unun açık istisnası). İkinci sayaç `PRINT_BEYAZ_LISTE_BAYAT = 0` muafiyetin gerçeği yansıttığını zorlar: listeye yazılıp sonra print'i kaldırılan dosya denetimi kırar.

---

## § 10 — DEĞİŞEN DOSYALAR

> `git status --porcelain | grep -v -E "/(bin|obj)/"` bu depoda **tüm ağacı** listeliyor (depo tabanı büyük ölçüde izlenmiyor). P-17'nin gerçekten dokunduğu dosyalar aşağıdadır.

**YENİ (8):**
```
dotnet/Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs
dotnet/Kuyumcu.Contracts/Pano/PanoSozlesmeleri.cs
dotnet/Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs
dotnet/Kuyumcu.Api/Modules/Pano/PanoModulu.cs
dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor
dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/YazdirDugmesi.razor
dotnet/Kuyumcu.Web/Pages/Cikti.razor
dotnet/Kuyumcu.Web/Pages/Vitrin.razor
```
**YENİ TEST (2):**
```
dotnet/Kuyumcu.Tests/P17CiktiPanoTestleri.cs
dotnet/Kuyumcu.Tests/P17EkranTestleri.cs
```
**DEĞİŞEN (11):**
```
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs            (CiktiServisi + PanoAyarServisi kaydı)
dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs         (/vitrin · /cikti/{Tur}/{Id})
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs               (SozlukCikti + SozlukPano, TR + EN)
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs         (iki yeni rota başlığı)
dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs             (MenusuzRotalar: iki gerekçeli satır)
dotnet/Kuyumcu.Web/Pages/IslemDefteri.razor         (ortak yazdır düğmesi + satır başına Belge bağlantısı)
dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor        (ortak yazdır düğmesi)
dotnet/Kuyumcu.Web/Pages/Kur.razor                  (vitrin bağlantısı — tek NavLink)
dotnet/Kuyumcu.Web/wwwroot/css/app.css              (.belge-uyari/.belge-satir-vurgu/.belge-sag + .yazdir-kutu/.pano-tablo)
dotnet/denetim.sh                                   (# >>> P-17 bloğu + rota tabanı 42→44)
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs          (rota sayısı 42→44)
```
**DEĞİŞEN (proje dışı ölçüm dosyaları, 2):**
```
dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js          (ROTA_KAPSAM_BEKLENEN 42→44)
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json    (ROTA_KAPSAM 42→44)
```
`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve `SONRAKI-OTURUM-PROMPT.md` dosyalarına **dokunulmadı**. **Commit yapılmadı**, hiçbir git komutu çalıştırılmadı.

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her iş kuralı için hatayı yeniden üreten test | ✅ | 48 yeni test; § 7 · dosya başlıklarındaki kural→test eşlemesi |
| (b) Mutasyon kanıtı (kır → kırmızı → geri al → cmp) | ✅ | § 8, 11 mutasyon, hepsi adıyla kırmızı, hepsi cmp birebir |
| (c) 5198'de canlı doğrulama (HTTP + SQL); 5199'a dokunma | ✅ | § 6, 18 adım · 5199 `lsof` 0 satır |
| (d) `dotnet/denetim.sh` TEMİZ | ✅ | § 9 |
| (e) Tam süit 0 başarısız | ✅ | § 7 — 3357/3357 |
| (f) Kendi test kiracını sil, QA-TEST-* dokunma | ✅ | § 6 temizlik · `tenants=8` · `QA-TEST-%`=3 |
| (g) Uygulama raporu | ✅ | bu dosya |
| Sayaç eklendi (mekanizma kuralı), taban taze ölçüldü | ✅ | § 9 `ORTAK_SABLON_DISI_PRINT` + 6 destek sayacı |
| Tavan/eşik ARTIRILMADI (cırcır) | ✅ | Yükseltilenlerin hepsi `-eq` eşitlik sayacı (§ 4 sapma 6); cırcır tavanlarının hiçbirine dokunulmadı |
| Pages içinde yeni istemci record AÇILMADI | ✅ | Yeni tipler `Kuyumcu.Contracts/Cikti` ve `/Pano` altında |
| İstemci alanı sunucuyla ad VE tip olarak eşleşir | ✅ | Aynı tip iki tarafta (derleyici kilidi); `SOZLESME_KILITSIZ_TIP` tavanı aşılmadı |
| PATCH: gönderilmeyen alan = değişmedi | ✅ | § 6 adım 4 · M17-5 |
| Finansal varlıkta hard DELETE yok | ✅ | Bu paket finansal varlık yazmıyor/silmiyor |
| Tek yazma yolu HareketFabrikasi · tek storno motoru | ✅ | Her iki servis de **salt okur** |
| Kiracı izolasyonu | ✅ | `Bir_kiracinin_pano_ayari_digerine_SIZMAZ`, `Baska_kiracinin_hareketinin_belgesi_404_doner` |
| `dotnet ef --no-build` yok, boş migration yok | ✅ | Migration üretilmedi (§ 5) |
| Sozlukler TR + EN, çıplak metin yok | ✅ | `SozlukCikti` + `SozlukPano` · `Cikti_ve_Pano_sozlukleri_TR_ve_EN_dolu` |
| Mevcut element id'leri DEĞİŞMEDİ; yeni id'ler `<önek>-<alan>` | ✅ | `#gpb-yazdir`, `#id-yazdir`, `#id-yazdir-kilit` korundu; yeni: `cikti-*`, `vitrin-*`, `pano-*` |
| Razor özniteliğinde `{ }` lambda yok; `disabled="false"` dizgesi yok | ✅ | `disabled="@(...)"` bool ifadesi |
| bUnit sahte API `{}` varsayılanıyla bırakılmadı | ✅ | Her iki sahte API sunucu sözleşmesiyle birebir gerçekçi yanıt veriyor |
| Sayılar InvariantCulture (makine yolu) | ✅ | `Diz/Coz` + `Bicim.Makine` · M17-7 |
| Türkçe JSON gövdesi ayrıştırılarak sınanır | ✅ | Boş PATCH ve yuvarlama testleri `Json().GetProperty("message")` kullanıyor (ham gövde `\uXXXX` kaçışlı) |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✅ | `.yazdir-kutu`/`.pano-tablo` belirteçli; `.belge-*` kuralları mevcut **yazılı kâğıt muafiyeti** içinde ve **ham hex içermiyor** (`currentColor` + tipografi) |
| Boş durum + hata/başarı bandı zorunlu | ✅ | `BosDurum`, `HataBandi`, `BasariBandi` her iki ekranda |
| Ateşle-unut yan kanal beklenmiyor | ✅ | Tüm yazmalar aynı `SaveChanges` içinde |
| COMMIT YOK / git komutu yok / rm-mv yok | ✅ | Yalnız `git status` okundu |
| Sır/parola/jeton çıktıya yazılmadı | ✅ | Parolalar değişkenden; geçici dosyalar boşaltıldı |

---

## § 12 — AÇIK KALANLAR

1. **Satış fişi ve emanet teslim belgesi canlıda ÖRNEKLENMEDİ.** Test kiracısında ürün/stok tohumu kurulmadığı için `SATIS` ve `EMANET_*` hareketi canlıda üretilmedi; iki tür **HTTP testlerinde** gerçek veritabanıyla doğrulandı (`Dort_belge_turu_de_AYNI_sozlesmeyi_doldurur`). Canlı tabloda yalnız TAHSILAT ve CARI_EKSTRE var. Bu bir eksiktir, gizlenmiyor — QA turunda zengin fikstür üzerinde dört tür de ekrandan açılmalı.
2. **Tarayıcı QA'sı yapılmadı.** Ekran davranışı 20 bUnit testiyle kapatıldı; gerçek tarayıcıda kâğıt ön izlemesi (A4 taşması, sayfa kırılması) QA turunda ölçülmeli. `@media print` kuralları mevcut `.belge*` bloğundan geliyor ve değiştirilmedi.
3. **Pano okunabilirlik eşiği (12) hâlâ ÖLÇÜLMEMİŞ bir tahmindir** (`PanoFiyatServisi.OkunabilirlikUyariEsigi` sınıf notu, PANO-KARAR §7). Bu paket eşiği değiştirmedi, yalnız ekranda görünür kıldı. Pilotta gerçek TV'de doğrulanmalı.
4. **`A4_KAGIT_DUZENI_SAYISI = 2`**: `PusulaBelgesi.razor` kendi VUK md.234 düzenini koruyor (yasal zorunlu alan yerleşimi ortak şablona sığmıyor). Tavan yalnız azalır; bir sonraki pakette 1'e indirilebilir ama bu **kapsam genişletmesi** olurdu, yapılmadı.
5. **Cari ekstresi tarih aralığı süzgeci yok** — en son 200 hareket basılıyor. Aralık seçimi P-17 DoD'unda yok; istenirse ayrı kalem olarak kimliklenmeli (Y-42 triyaj kuralı).

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire:** bu paketin ürettiği dört belge **resmî belge değildir** — satış fişi, tahsilat makbuzu, emanet teslim belgesi ve cari ekstresi *işletme içi / müşteriye bilgi* çıktılarıdır. Resmî belge yalnız fatura (`Faturalar`) ve gider pusulası (VUK md.234, `PusulaBelgesi`) tarafındadır ve düzenleri **değişmedi**. Bu dört çıktının bir mevzuat zorunluluğu içerip içermediği (ör. emanet teslim belgesinin ispat değeri) teyit edilirse şablon buna göre genişletilir; şu hâliyle koda hiçbir yasal hüküm sabit yazılmadı.
- **Kurucuya:** vitrin panosu `/vitrin` adresinden açılıyor ve **menüde yok** — kenar çubuğu 36 bağlantıyla P-14'te mühürlendiği için pano, kaynağı olan **Kur** ekranındaki "Vitrin panosunu aç" düğmesinden açılıyor. Panoyu dükkândaki TV'ye tam ekran vermek isterseniz menüye ayrı bir satır eklenmesi bir **karar** kalemidir (menü sayacı ve P-14 mührü etkilenir); ekip oyuna sunulabilir.
- **Kurucuya:** marj ticari sır sayıldığı için pano ayarları **yalnız Sahip ve Müdür** rollerine açıktır; tezgâhtar panoyu görür, marjı göremez. Bu bir güvenlik kararıdır, isterseniz gevşetilebilir.

---

# § 14 — REVİZYON 1.1 (2026-09-21, hasım doğrulama bulgularının kapatılması)

Hasım doğrulayıcı turu **1 KRİTİK + 2 ORTA/DÜŞÜK** bulgu bıraktı. Bu bölüm her
bulguyu gerçek çıktısıyla kapatır. **Anayasa md.1:** revizyon 1.0'daki
"denetim.sh TEMİZ / ORTAK_SABLON_DISI_PRINT 0" kanıtının **HÜKÜMSÜZ** olduğu
kabul edilir — sayaç ölçmüyordu, `0` yalancıydı. Aşağıdaki kanıt yenisidir.

## § 14.1 — [KRİTİK] Ölü sayaç: bash betiğinde zsh `${=VAR}` sözdizimi

**Teşhis doğrulandı.** `dotnet/denetim.sh` `#!/usr/bin/env bash` ile başlar;
`${=VAR}` zsh'e özgüdür, bash'te `bad substitution` verip döngüyü **sessizce**
boş bırakır. P-17'nin **zorunlu mekanizma koşulu** olan iki sayaç bu yüzden
her koşulda `0` diyordu — beyaz liste dışı üç yeni `window.print` noktası olsa
bile "TEMİZ" basardı.

### Düzeltme (bash dizisi)

| Satır (eski) | Eski (ölü) | Yeni (çalışan) |
|---|---|---|
| `denetim.sh:3008` | `P17_BEYAZ="… …"` (dizge) | `P17_BEYAZ=( "$WEB/Bilesenler/Yazdirma/YazdirDugmesi.razor" "$WEB/Pages/UrunEtiketleri.razor" )` |
| `denetim.sh:3009` | `P17_PRINT=$(grep …)` (dizge) | `P17_PRINT=()` + `while IFS= read -r … done < <(grep …)` |
| `denetim.sh:3012` | `for f in ${=P17_PRINT}; do` | `for f in "${P17_PRINT[@]}"; do` + açık beyaz-liste karşılaştırma döngüsü |
| `denetim.sh:3028` | `for f in ${=P17_BEYAZ}; do` | `for f in "${P17_BEYAZ[@]}"; do` |
| `denetim.sh:2446` | `P12_FINANSAL="…"` (dizge) | `P12_FINANSAL=( Tutar Yon … CariId )` — **mevcut borç, birlikte kapatıldı** |
| `denetim.sh:2453` | `for y in ${=P12_FINANSAL}; do` | `for y in "${P12_FINANSAL[@]}"; do` |

`case`/glob eşleştirmesi de bırakıldı: dosya adı glob karakteri içerirse
sessizce yanlış eşleşirdi. Yerine birebir `[ "$f" = "$b" ]` karşılaştırması.

**Kalan `${=` geçişleri yalnız YORUM satırlarıdır** (1595, 2446, 2533, 2648,
2808, 3008 — tuzağı belgeleyen uyarılar), kod değil:

```
$ grep -n '\${=' dotnet/denetim.sh     # hepsi '#' ile başlayan uyarı satırı
$ bash -n dotnet/denetim.sh && echo SOZDIZIMI OK
SOZDIZIMI OK
```

### Negatif kontrol — sayaç GERÇEKTEN kırmızıya dönüyor mu?

Kanıt olmadan "düzelttim" denmez. Üç mutasyon, üçü de kaynak scratchpad'e
yedeklenip `cmp` ile birebir geri alındı (proje ağacında `.bak` bırakılmadı).

| Kod | Mutasyon | Beklenen | **Gerçek çıktı** | Geri alım |
|---|---|---|---|---|
| **NK-1** | `Pages/IslemDefteri.razor`'a beyaz liste dışı `InvokeVoidAsync("window.print")` eklendi | sayaç ≥1, denetim KIRMIZI | `↳ beyaz liste dışı: Kuyumcu.Web/Pages/IslemDefteri.razor` · `✗ P-17: ORTAK_SABLON_DISI_PRINT 1` · `DENETİM BAŞARISIZ` · exit 1 | `cmp` AYNI |
| **NK-2** | `Bilesenler/Yazdirma/YazdirDugmesi.razor` içinde `window.print` → `window.printMut` (muafiyet bayatlatıldı) | bayat sayacı ≥1 | `↳ bayat muafiyet: Kuyumcu.Web/Bilesenler/Yazdirma/YazdirDugmesi.razor` · `✗ P-17: PRINT_BEYAZ_LISTE_BAYAT 1` · `DENETİM BAŞARISIZ` · exit 1 | `cmp` AYNI |
| **NK-3** | `KasaHareketDuzeltDto`'ya `public decimal? Tutar` eklendi (P-12 finansal sızıntı) | P-12 sayacı ≥1 | `✗ P-12: STORNO_DISI_FINANSAL_PATCH 1 (-eq 0 olmalı — KasaHareketDuzeltDto.Tutar)` · `DENETİM BAŞARISIZ` · exit 1 | `cmp` AYNI |

NK-1 ve NK-2'den önce bu iki sayaç, aynı ihlallerde bile `0`/`✓` basıyordu.
**Sayaç artık ölçüyor.** NK-3, ölü olduğu bilinmeyen P-12 sayacının da
canlandığını kanıtlar.

**Bugünkü gerçek değer yine 0'dır** ve bu kez ölçülmüştür: `window.print`
çağıran tek iki dosya `Bilesenler/Yazdirma/YazdirDugmesi.razor` (ortak şablonun
kendi çağrısı) ve `Pages/UrunEtiketleri.razor` (termal/barkod ailesi, plan DoD'sinde
gerekçeli istisna). Beyaz liste **2**, tavan artırılmadı.

## § 14.2 — [ORTA] Tam süit kararsızlığı (`BildirimEkranTestleri`)

**Kök neden bulundu — paylaşılan durum DEĞİL, ölçüm anı.**
`Pages/Bildirimler.razor:522` `IsiCalistir` **asenkrondur** ve gövdesi
`if (calisanIs is not null) return;` bekçisiyle başlar:

```csharp
private async Task IsiCalistir(string kod)
{
    if (calisanIs is not null) return;   // ← önceki tıklama HÂLÂ uçuyorsa no-op
    calisanIs = kod;
    …  await Api.PostAsync(…);  await Yukle();  await IsleriYukle();
    calisanIs = null;
}
```

`Her_dugme_ya_istek_atar_ya_da_disabled_dir` düğmeleri **döngüyle sırayla**
tıklıyor ve etkiyi **senkron** ölçüyordu. Tam süit paralel koşarken sürücü iş
parçacığı gecikince önceki yinelemenin isteği hâlâ uçuyor oluyor, `calisanIs`
dolu kalıyor, sıradaki tıklama **gerçekten** no-op oluyor →
`is-calistir-VADE_HATIRLATMA: tıklanınca hiçbir şey olmadı`. İzole koşumda
gecikme olmadığı için 18/18 yeşildi. Kararsızlığın tarifi budur.

**Düzeltme** (`Kuyumcu.Tests/BildirimEkranTestleri.cs`): kural gevşetilmedi,
yalnız ölçüm anı düzeltildi — her tıklamanın etkisi `WaitForAssertion` ile
beklenir ve sıradaki düğmeye geçmeden bileşenin dinlenmesi (uçuşan eylemin
bitmesi) beklenir. Gerekçe koda yorum olarak yazıldı.

**Test hâlâ ölü düğme yakalıyor mu? — mutasyon kanıtı:**

| Kod | Mutasyon | Kırmızıya dönen test (ADIYLA) | Geri alım |
|---|---|---|---|
| **M17-4** | `Bildirimler.razor:524` `if (calisanIs is not null) return;` → `if (calisanIs is null) return;` (düğme ölür) | `Kuyumcu.Tests.BildirimEkranTestleri.Her_dugme_ya_istek_atar_ya_da_disabled_dir` → `is-calistir-VADE_HATIRLATMA: tıklanınca hiçbir şey olmadı` **ve** `Kuyumcu.Tests.BildirimEkranTestleri.Simdi_calistir_POST_calistir_ucuna_gider` → `Başarısız: 2, Başarılı: 16` | `cmp` AYNI · sonrası `dotnet build` **0 Uyarı 0 Hata** |

Yani gevşetme **yalancı yeşil üretmiyor**: DoD EK md.1 ("tıklanınca hiçbir şey
olmayan düğme = paket geçmez") hâlâ zorlanıyor, test 1 sn bekleyip **kırmızı**
oluyor.

**Tekrarlanabilirlik — tam süit ÜÇ ARDIŞIK koşum:**

| Koşum | Komut | Sonuç |
|---|---|---|
| 1 | `dotnet test … --logger trx --results-directory …/trx-pP-17` | `Toplam test sayısı: 3357` · **`Geçti: 3357`** · 0 başarısız |
| 2 | aynı (`…/trx-pP-17b`) | `Toplam test sayısı: 3357` · **`Geçti: 3357`** · 0 başarısız |
| 3 | aynı + `--no-build` (`…/trx-pP-17c`) | `Toplam test sayısı: 3357` · **`Geçti: 3357`** · 0 başarısız |

Kapı kanıtı olan "tam süit 0 başarısız" cümlesi artık **tekrar üretilebilir**.
Not: test sayısı 3357'dir (CLAUDE.md'deki 3032 eski bir kapı kaydıdır).

## § 14.3 — [DÜŞÜK] `SATIS_FISI` ve `EMANET_TESLIM` canlıda örneklenmedi

Bu tur **kapatıldı**: zengin fikstürlü geçici kiracı (`PAKET17-TEST Kuyumculuk`)
açıldı, ürün + stok + satış + emanet tohumlandı, **dört belge türü de** 5198'de
gerçek veriyle çekildi. (5199'a **dokunulmadı**; zaten kapalıydı — § 14.4.)

| # | Adım | Beklenen | **Gerçek** | Kanıt |
|---|---|---|---|---|
| 1 | Sistem daveti + `POST /auth/kayit` | 201 | **201** | kiracı açıldı (jeton/parola çıktıya **yazılmadı**) |
| 2 | `POST /kasalar`, `POST /cari`, `POST /urunler` | 200/201 | **200 / 201 / 201** | kasa, `P17 Musteri`, `P17-BILEZIK` |
| 3 | `POST /islemler/alis` (2 × 5.000 TL) | 200 | **200** | `ALIS -10000.00 TL · 18.32 has` |
| 4 | `POST /islemler/alis` (2 × 30.000 TL) | tevsik reddi | **400** | *"Tevsik zorunluluğu: … 60.000,00 TL, 30.000,00 TL haddini aşıyor"* (kural canlı çalışıyor) |
| 5 | `POST /islemler/satis` (1 ad. 9.000 + 500 işçilik) | 200 | **200** | `SATIS 9500.00 TL · -9.16 has` |
| 6 | `POST /emanet/giris` (K22, 12,5 g) | 200 | **200** | `hasGrami 11.45` → `EMANET_GIRIS 0.00 TL · 11.45 has` |
| 7 | `POST /cari/{id}/tahsilat` (2.000 TL) | 200 | **200** | `TAHSILAT -2000.00 TL` |
| 8 | **`GET /cikti/SATIS_FISI/{hareketId}`** | 200, ortak sözleşme dolu | **200** | `SATIŞ FİŞİ` · imza `['Satıcı','Alıcı']` · bölüm `Kalemler` → `["Satış","P17 Test Bilezik","-9.160","9500.00"]` · `Toplam tutar 9500.00 TRY` + `Toplam has gram -9.16 HAS_GRAM` · `uyari=null` |
| 9 | **`GET /cikti/EMANET_TESLIM/{hareketId}`** | 200, ortak sözleşme dolu | **200** | `EMANET TESLİM BELGESİ` · imza `['Teslim eden','Teslim alan']` · bölüm **`Emanet Kalemleri`** → `["Emanet girişi","P17 canli dogrulama emaneti","11.450","0.00"]` · `Toplam has gram 11.45 HAS_GRAM` |
| 10 | `GET /cikti/TAHSILAT_MAKBUZU/{hareketId}` | 200 | **200** | `TAHSİLAT MAKBUZU` · imza `['Tahsil eden','Ödeyen']` · `-2000.00 TRY` |
| 11 | `GET /cikti/CARI_EKSTRE/{cariId}` | 200 | **200** | `CARİ EKSTRESİ` · imza `['Düzenleyen']` · `TL bakiye -2000.00 TRY` |
| 12 | **Dört türün alan kümesi karşılaştırması** | tek sözleşme | **farklı alan kümesi sayısı = 1** | ortak: `tur, belgeAdi, belgeNo, dayanak, duzenlemeTarihi, duzenleyen, karsi, bolumler, tutarlar, imzaBasliklari, not, uyari` |
| 13 | `GET /cikti/SACMA/{id}` | 400 | **400** | *"Bilinmeyen belge türü: SACMA. Kabul edilenler: SATIS_FISI, TAHSILAT_MAKBUZU, EMANET_TESLIM, CARI_EKSTRE"* |
| 14 | `GET /cikti/SATIS_FISI/<olmayan>` | 404 | **404** | *"Hareket bulunamadı"* |
| 15 | `GET /cikti/EMANET_TESLIM/{satisHareketId}` | 400 | **400** | *"Bu hareket 'EMANET_TESLIM' belgesi üretmez (hareket türü: SATIS). Beklenen: EMANET_GIRIS, EMANET_CIKIS"* |
| 16 | Denetim izi (SQL) | belge okuması kayıtlı | ✅ | `CiktiBelgesi OKUMA 13` · `Hareket EKLEME 4` · `Cari EKLEME 1` · `Urun EKLEME 1` |

**Revizyon 1.0'ın § 7 md.1'deki açık kalemi böylece KAPANDI.** Dört belge
türünün de **tek şablondan** doğduğu artık canlı veriyle kanıtlı (adım 12).

### Temizlik (kanıtlı)

```
API PID durduruldu · lsof -nP -iTCP:5198 -sTCP:LISTEN → 0 satır
                     lsof -nP -iTCP:5199 -sTCP:LISTEN → 0 satır (DOKUNULMADI)
tenants            = 8   (başlangıçtaki değere döndü)
artik_hareket      = 0 · artik_cari = 0 · artik_denetim = 0
qa_fikstur (QA-TEST-%) = 3   ← DOKUNULMADI
paket17_kalan      = 0   (kiraci_davetleri satırı dahil silindi)
migrations         = 56  (değişmedi — § 14.5)
```

Silme, `information_schema`'dan `tenant_id` kolonu olan **58 tablonun** hepsinde
koşuldu. Geçici sır dosyaları (`siskey.txt`, `sifre.txt`, `jeton.txt`,
`davet.json`, `kayit.json`, `ctx.json`, `sil.sql`) `printf '' >` ile boşaltıldı;
**hiçbir sır/parola/jeton değeri çıktıya yazılmadı**.

## § 14.4 — [DÜŞÜK] Kurucunun 5199 inceleme sunucusu kapalı

**Doğrulandı ve giderilmedi (kapsam dışı, kurucu/orkestratör kalemi).**
Bu turun başında ve sonunda `lsof -nP -iTCP:5199 -sTCP:LISTEN` → **0 satır**.
Denetim betiği bunu zaten söylüyor: `P-02: YAYIN_TAZE (5199 anlık görüntüsü) BAYAT`.
Uygulayıcı 5199'a **hiç dokunmadı** (kırmızı çizgi). → **Kurucu/orkestratör
`yayinla.sh` + `kuyumcu-qa` profilini yeniden başlatsın; kapı kaydına "5199 bu
turda kapalıydı" yazılsın.**

## § 14.5 — [DÜŞÜK] Proje ağacındaki `.bak` / `.mutorj` artıkları

**Doğrulandı, atıf yapılmadı (tahmin yok).** Bugün ağaçta duran artıklar:

```
dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak      (0 bayt)
dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak
dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj
dotnet/Kuyumcu.Web/Pages/{Fiyatlandirma,Islemler,Urunler,VadeTakibi,Sayim}.razor.mutorj
dotnet/Kuyumcu.Api/Modules/{Rapor/RaporModulu,Sayim/SayimModulu}.cs.mutorj
```

Hiçbiri P-17 dosya listesinde değildir; **bu turda oluşturulmadı** ve
dokunulmadı. Bu turun **tüm** yedekleri scratchpad altındadır
(`scratchpad/mut-yedek/P-17/`); proje ağacına `.bak`/`.mutorj`/`.tmp`
**bırakılmadı**. Silme hook nedeniyle **kurucuya bırakılıyor.**

## § 14.6 — Migration

**GEREKMEDİ.** Bu revizyon yalnız bir kabuk betiğine (`denetim.sh`) ve bir test
dosyasına dokundu; şema değişikliği yoktur. Kanıt: `__EFMigrationsHistory` = **56**
(tur başında da 56). Revizyon 1.0'da da `PanoKalemAyari` için yeni tablo
gerekmemişti (plan "ŞARTLI" diyordu).

## § 14.7 — Derleme ve denetim (son hâl)

```
$ dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata

$ ./denetim.sh                                    # exit 0
  ✓ P-17: ORTAK_SABLON_DISI_PRINT   0 (beyaz liste: 2 — ortak düğme + termal etiket)
  ✓ P-17: PRINT_BEYAZ_LISTE_BAYAT   0
  ✓ P-17: A4_KAGIT_DUZENI_SAYISI    2/2 (tavan YALNIZ azalır)
  ✓ P-17: ORTAK_BELGE_TURU          4 (fiş · makbuz · emanet · ekstre)
  ✓ P-17: PANO_AYAR_KIRACIDAN       kiraci_ayarlari 5 · modül kaydı 1
  ✓ P-17: PANO_IKINCI_HESAP_YOLU    0 (hesap YALNIZ PanoFiyatServisi'nde)
  ✓ P-17: PANO_MARJ_SIZINTISI       0 (marj ticari sırdır, panoya inmez)
  ✓ P-12: STORNO_DISI_FINANSAL_PATCH 0 (-eq 0: 3 düzeltme DTO'su)
DENETİM TEMİZ.
```

Tek `⚠` (hata değil, devir açık): `P-02: QA_TUR_SURESI_OLCUM (BK-39) ÖLÇÜM YOK
— devir açık (tur >6 ya da 2026-09-30'e dek; sonrasında DENETİM KIRILIR)`.
**Bu bir zaman bombasıdır: 2026-09-30'dan sonra denetim kırılır.** Orkestratöre not.

## § 14.8 — Bu revizyonda değişen dosyalar

```
dotnet/denetim.sh                             ← sayaç onarımı (P-17 ×2, P-12 ×1)
dotnet/Kuyumcu.Tests/BildirimEkranTestleri.cs ← kararsızlık onarımı (ölçüm anı)
```

Başka hiçbir kaynak dosya değiştirilmedi; NK-1/2/3 ve M17-4 mutasyonlarının
tümü `cmp` ile birebir geri alındı. **COMMIT YOK** — çalışma ağacında kalındı,
hiçbir `git` yazma komutu çalıştırılmadı.

## § 14.9 — Kimlik kapanış tablosu (revizyon 1.1)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-6** — Ortak A4 çıktı şablonu | **KAPANDI** | Dört belge türü canlıda **tek sözleşmeden** (§ 14.3 adım 8-12, "farklı alan kümesi sayısı = 1") · sayaç `ORTAK_SABLON_DISI_PRINT` artık **gerçekten ölçüyor** (§ 14.1 NK-1/NK-2) · `denetim.sh:3008-3040` |
| **Y-7** — Vitrin / kur panosu | **KAPANDI** | Rev 1.0 § 6 adım 1-9 (canlı) + `PANO_AYAR_KIRACIDAN`, `PANO_IKINCI_HESAP_YOLU`, `PANO_MARJ_SIZINTISI` sayaçları ✓ (§ 14.7) |
| *(kapsam dışı, mevcut borç)* P-12 `STORNO_DISI_FINANSAL_PATCH` sayacı | **ONARILDI** | Ölü döngü canlandırıldı, NK-3 ile kanıtlandı (§ 14.1) — P-12'nin kendi kapsamına dokunulmadı, yalnız **sayacı** çalışır hâle getirildi |

## § 14.10 — Kurucuya / orkestratöre notlar

1. **`${=VAR}` tuzağı dört yerde yazılı uyarıyla belgeliydi ve yine açıldı.**
   Yorum uyarısı yetmiyor. Öneri: `denetim.sh`'ın kendisine bir öz-denetim
   satırı — `grep -n '\${=' denetim.sh | grep -v '^\s*[0-9]*:#'` boş değilse
   **denetim kırılsın**. Bu bir kapsam genişletmesidir, **karar orkestratörde**;
   bu turda **uygulanmadı** (kapsam kilidi).
2. **5199 kapalı** — kurucu/orkestratör `yayinla.sh` + `kuyumcu-qa`'yı ayağa
   kaldırsın (§ 14.4).
3. **`.bak` / `.mutorj` artıkları** silme hook'u nedeniyle kurucuda (§ 14.5).
4. **`P-02: QA_TUR_SURESI_OLCUM` 2026-09-30'da denetimi kıracak** (§ 14.7).
5. **Görev metnindeki parola komutu hatalıdır** (mali değil, teknik not):
   `grep MYSQL_ROOT_PASSWORD docker-compose.yml | head -1` **yorum satırını**
   (`docker-compose.yml:3`) yakalar ve yanlış parola üretir →
   `ERROR 1045 Access denied`. Doğrusu araya `grep -v '^\s*#'` koymaktır.
   Sonraki paket talimatlarında düzeltilmeli.
6. Mali/mevzuat hükmü bu revizyonda **koda sabitlenmedi**; tevsik haddi zaten
   yapılandırmadan gelmektedir (§ 14.3 adım 4 canlı kanıtı).
