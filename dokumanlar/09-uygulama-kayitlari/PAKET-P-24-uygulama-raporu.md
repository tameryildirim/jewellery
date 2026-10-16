# PAKET P-24 — MUTABAKAT SÖZLEŞMESİ + KANIT REJİMİ · UYGULAMA RAPORU

**Tarih:** 2026-09-22 · **Dalga:** DALGA 6 · **Roller:** Taşkın (backend) + Ertuğ (frontend) · **Sürüm:** 1.0
**Kimlikler:** B-7 · B-28 · B-62 (P-00 triyajı) · **Migration:** HAYIR (plan böyle diyor, gerekmedi)
**Durum:** BİTTİ — derleme 0 uyarı/0 hata · tam süit 3812/3812 yeşil · `dotnet/denetim.sh` **DENETİM TEMİZ** · canlı 5198 doğrulaması yapıldı · test kiracısı silindi (tenants 10 → 9)

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (varsayım yok, diskte ölçüldü)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| P-04 | VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-04-uygulama-raporu.md` · `dotnet/sube-suzgec-siniflandirma.txt` mühürlü, `denetim.sh` P-04 bloğu koşuyor |
| P-19 | VAR | `PAKET-P-19-uygulama-raporu.md` |
| P-23 | VAR (3 faz) | `PAKET-P-23-uygulama-raporu.md`, `-FAZ2-`, `-P-23c-` · `denetim.sh` P-23 bloğu koşuyor |

Üçü de diskte olduğu için **kapsam daraltılmadı**; planın "P-04/P-23'ten önce koşarsa testler iki kere yazılır" riski gerçekleşmedi (mevcut testlerde `Mutabakat_` önekli tek bir test yoktu, ölçüldü).

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu bölüm)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-7** — Türetilmiş özet ↔ liste toplamı mutabakatı | **KAPANDI** | `Modules/Denetim/DenetimModulu.cs:29-71` (`DenetimSuzgeci`, `MUTABAKAT-SUZGEC`) — liste ve özet TEK süzgeç nesnesinden türer; `Modules/Rapor/Mutabakat.cs` (sözleşme + `Denk`); çalışma zamanı denklik kapısı `DenetimModulu.cs` içinde 3 çağrı (`eylemler`/`turler`/`subeler` kırılımı ↔ `toplam`). Testler: `Mutabakat_Denetim_ozet_toplami_SUZGECLI_liste_toplamina_ESITTIR`, `Mutabakat_Denetim_ozet_KIRILIM_toplami_toplamla_DENKTIR`, `Mutabakat_Denetim_HASSAS_suzgeci_ozete_de_DUSER`, `Mutabakat_Masak_…`, `Mutabakat_Risk_…`, `Mutabakat_Yedek_…`. Canlı: § 6 tablosu, 6 süzgeç kombinasyonunda liste toplamı = özet toplamı. Sayaç: `OZET_AYRI_SORGU = 0`, `MUTABAKAT_TESTI_OLMAYAN_OZET = 0`. |
| **B-28** — DOM gerektiren kanıtlar (pasta `getBBox`) | **KAPANDI (mekanizma)** | `betikler/qa-turu.js`: BULGU şemasına `grafik_bbox` alanı eklendi; tur istemine (g) adımı — her `<svg>` için `getBBox().width` / kapsayıcı genişlik ölçülür, `parca>0 && en_genis_px===0` → KRİTİK `GRAFIK_CIZILMIYOR: …`, `oran_yuzde<50` → YÜKSEK. Sayım **betikte** (`grafik_cizilmeyen`, `grafik_dar_cizilen`), ajanda değil. Sayaç: `GRAFIK_KANITI_GETBBOX = 4 (≥2)`. ⚠ **Ölçümün kendisi bir sonraki QA turunda koşar** — bu paket tarayıcı turu koşturmaz (kapsam kilidi); "pasta gerçekten %100 çiziliyor" iddiası QA turunda kanıtlanacaktır (Anayasa md.1: burada "oldu" denmiyor, **ölçüm yolu kuruldu** deniyor). |
| **B-62** — /denetim-izi doğruluğu (bitiş off-by-one, kur anlık görüntüsü kirliliği, çift getirme, sayfalama) | **KISMEN** | **Off-by-one KAPANDI:** `Modules/Rapor/Mutabakat.cs` `UstSinirKapsayici` + `DenetimSuzgeci` (tek yer). Testler: `Mutabakat_Denetim_BITIS_GUNU_kapsama_DAHILDIR`, `Mutabakat_Denetim_bitis_gununden_SONRAKI_kayit_KAPSAM_DISIDIR`, `Mutabakat_UstSinirKapsayici_*` (3 saf test). Canlı: `baslangic=bitis=2026-09-22` → 26 kayıt (eskiden 0 dönerdi), `bitis=2026-09-22T00:05:00` → 0 (kural saat bileşenine bağlı, § 6). Sayaç: `BITIS_GUNU_OFF_BY_ONE = 0`. **Çift getirme ve sayfalama ZATEN KAPALIYDI** (kodda ölçüldü: `DenetimIzi.razor` `sonSorgu` koruması — UX-4d md.22; `DahaFazla`/`Getir(offset)` sayfalaması mevcut) — bu pakette yeniden yazılmadı. **"Kur anlık görüntüsü kirliliği" DEVREDİLDİ: P-25** — /denetim-izi ekranında kur anlık görüntüsü ile ilgili bir alan yoktur; bulgunun yüzeyi rapor/kur tarafıdır ve P-24'ün dosya ayak izi dışındadır (kapsam kilidi: kapsamı büyütmemek için yapılmadı, gizlenmedi). |

---

## § 2 — DoD SÖZLEŞMESİ (Bertan koşulu — hangi varlık nasıl düzeltilir)

Bu paket **yeni varlık / yeni kayıt türü ÜRETMEZ**; migration yoktur. Dolayısıyla PATCH/DELETE/pasifleştirme ucu doğurması gereken yeni bir varlık yoktur. Dokunulan yüzeyin düzeltilebilirlik durumu:

| Varlık | Düzenlenebilir mi | Uç / gerekçe |
|---|---|---|
| `DenetimKaydi` | **HAYIR — bilinçli, gerekçesi ekranda yazılı** | Append-only. Ekranda `DegismezIpucu Varlik="DenetimKaydi"` (mevcut, korundu) + `Dil.T.Denetim.DegistirilemezNot`. Altyapı da reddeder (`DenetimYazmaDinleyicisi → DenetimIziDegistirilemez`). Silme/güncelleme ucu **yoktur ve açılmayacaktır** (controller yorumu). Bu, UX-S25'in "neden düzeltilemediği yazar" kolundan karşılanır. |
| Süzgeç durumu (`baslangic/bitis/eylem/varlikTuru/subeId/hassas`) | **EVET** | Her süzgeç ekrandan değiştirilebilir; değişiklik hem listeye hem üst kartlara **aynı anda** düşer (tek `SuzgecParcalari()` kaynağı). |

**Finansal varlıkta hard DELETE:** bu pakette hiçbir DELETE yolu eklenmedi. `HareketFabrikasi`, `StornoServisi`, migration, şema — **hiçbirine dokunulmadı**.

---

## § 3 — DoD EK / EK 2 KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| Her düğme ya istek atar ya disabled + gerekçe | KARŞILANDI | Yeni düğme eklenmedi; mevcut `DahaFazla` düğmesi gerçek istek atar (`Getir(offset)`). |
| Tek düzenleme kalıbı (yeni `duzenlenenId` açılmaz) | KARŞILANDI | Düzenleme kalıbı eklenmedi; `Pages/` altında yeni `ModalPencere` düzenleme kullanımı **0 artış** (denetim.sh P-36c/P-37 sayaçları yeşil). |
| Form: geçersiz gönderimde alan altı Türkçe mesaj | KARŞILANDI | `f-aralik-hata` (ters aralık) mevcut ve korundu; sunucu da 400 + Türkçe mesaj döner (§ 6). |
| Yıkıcı eylem onay modalı | UYGULANMAZ | Yıkıcı eylem eklenmedi. |
| **TEK DOKUNUŞ — dokunulan sayfalar** | — | **`Kuyumcu.Web/Pages/DenetimIzi.razor`** (bu dalgada bu paketle ilk kez açıldı). Başka razor sayfasına dokunulmadı. |
| Storno taklidi yok | KARŞILANDI | Storno kodu hiç açılmadı. |
| Ücretsiz pilot / fiyat metni yok | KARŞILANDI | Fiyat/teklif/tahsilat metni üretilmedi. |

---

## § 4 — YAPILANLAR (dosya dosya)

### 4.1 Yeni — `dotnet/Kuyumcu.Api/Modules/Rapor/Mutabakat.cs` (3.397 bayt)
- `Mutabakat.UstSinirKapsayici(DateTime?)` — **B-62 off-by-one**. Kural: *saat bileşeni 00:00:00 ise değer bir GÜN'dür, üst sınır ertesi günün başıdır; saat bileşeni varsa değer aynen kalır.*
- `Mutabakat.Denk(alan, ozet, listeToplami)` — ayrışma hâlinde `MutabakatIhlali` atar; **yuvarlamaz, gizlemez**.
- `MutabakatIhlali` — alan adı + iki sayıyı taşıyan istisna.

### 4.2 `dotnet/Kuyumcu.Api/Modules/Denetim/DenetimModulu.cs`
- **Yeni `internal sealed record DenetimSuzgeci`** — `MUTABAKAT-SUZGEC` işaretli tek süzgeç kaynağı; `Uygula(db)` tüm süzgeçleri (tarih + eylem + tür + varlıkId + hassas + kullanıcı + şube + kasa) uygular.
- `Listele` ve `Ozet` **aynı nesneyi** kurar. `Ozet` yeni (varsayılanlı, sona eklenmiş) süzgeç parametreleri aldı → **mevcut konumsal çağrılar kırılmadı** (`UX4dServisTestleri.M23_…` değişmeden yeşil).
- `Ozet` dönüşünde **3 çalışma zamanı denklik kapısı**: `eylemler`/`turler`/`subeler` kırılım toplamı = `toplam`.
- Controller `GET /api/v1/denetim/ozet` süzgeç parametrelerini kabul eder (liste ucuyla **aynı küme**).
- ⚠ `subeId`/`kasaId` için **yerel değişken kullanıldı**: satır metni P-04 sınıflandırma tablosunda (`sube-suzgec-siniflandirma.txt:25`) birebir mühürlü; büyük harfli özellik adı `GUVENLIK_ELLE_IZOLASYON` sayacını kırıyordu (ölçüldü, § 7'de anlatıldı).

### 4.3 `dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor`
- `SuzgecParcalari()` **tek kaynak**; `OzetSorgu()` ve `Sorgu(offset)` ondan üretilir → özet ucu artık listenin TÜM süzgeçlerini taşır (eskiden yalnız tarihi taşıyordu → B-7'nin ekran ayağı).
- `SuzgecAltMetni` — üst kartların hangi kümeyi saydığı **yazılı** (UX-S29 / UX-S33).
- `f-bitis-ipucu` — "Bitiş günü kapsama dâhildir." (UX-S30: görünmez kural olmaz).

### 4.4 `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- **TR + EN**: `Denetim.SuzgeceGore`, `Denetim.BitisGunuDahil`. Çıplak metin yok.

### 4.5 `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js` (B-28)
- BULGU şemasına `grafik_bbox` (secici, parca, en_genis_px, svg_genislik_px, oran_yuzde).
- Tur istemine (g) adımı: `getBBox().width` ölçümü, path dizgesine bakmak **açıkça yasaklandı**.
- Betik tarafı sayım: `grafik_olculen_sayfa`, `grafik_svg_sayisi`, `grafik_cizilmeyen`, `grafik_dar_cizilen` → `OLCUM` nesnesine ve log satırına girer (sayıyı ajan değil betik sayar).
- `node --check` ile sözdizimi doğrulandı (exit 0).

### 4.6 `dotnet/denetim.sh` — işaretli blok `# >>> P-24 … # <<< P-24`
Beş sayaç, **tabanlar taze ölçüldü** (belgeden kopyalanmadı), her birinin ölçüm komutu betikte yorum olarak yazılı:

| Sayaç | Değer | Anlamı |
|---|---|---|
| `OZET_AYRI_SORGU` | **0** | Süzgeç parametresi alan hiçbir özet ucu ikinci bir `.Where(` zinciri yazmıyor |
| `MUTABAKAT_SUZGEC_KAPISI` | süzgeç 2 dosya · denklik 3 | İşaretli süzgeç + çalışma zamanı denklik çağrıları sökülmemiş |
| `MUTABAKAT_TESTI_OLMAYAN_OZET` | **0** (nüfus **4**) | Özet ucu olan 4 modülün (Denetim · Masak · Risk · Yedek) hepsinde `Mutabakat_<Modul>_` testi var |
| `BITIS_GUNU_OFF_BY_ONE` | **0** | Ham `d.Tarih < bitis` deseni yok; kapsayıcı üst sınır tek yerde |
| `GRAFIK_KANITI_GETBBOX` | **4** (≥2) | qa-turu.js şema + istem |

⚠ **Tavan artırılmadı**; beş sayaç da yeni ve hepsi en sert değerinde (0 / ≥ eşik) kuruldu.

---

## § 5 — TESTLER VE MUTASYON KANITI

### 5.1 Sayılar
| Ölçü | Önce | Sonra |
|---|---|---|
| Tam süit | 3.799 | **3.812** (0 başarısız, 0 atlanan) |
| Yeni test | — | **13** (`Kuyumcu.Tests/MutabakatTestleri.cs`) |
| Derleme | 0 uyarı / 0 hata | **0 uyarı / 0 hata** |
| `denetim.sh` | TEMİZ | **TEMİZ** (243 ✓, 0 ✗) |

Tam süit komutu: `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --no-build --logger trx --results-directory <scratchpad>/trx-pP-24`
→ `Başarılı! - Başarısız: 0, Başarılı: 3812, Atlanan: 0, Toplam: 3812, Süre: 1 m 4 s`

### 5.2 Mutasyon tablosu (7 mutasyon — hepsi kırmızı, hepsi `cmp` ile geri alındı)

| Kod | Dosya | Değişiklik | KIRMIZI olan test(ler) | Geri alındı (`cmp`) |
|---|---|---|---|---|
| M24-1 | `Modules/Rapor/Mutabakat.cs` | `UstSinirKapsayici`: `b.AddDays(1)` → `b` (off-by-one geri geliyor) | `Mutabakat_Denetim_BITIS_GUNU_kapsama_DAHILDIR`, `Mutabakat_UstSinirKapsayici_GUN_verildiginde_ERTESI_GUNE_tasir` | CMP-AYNI |
| M24-2 | `Modules/Rapor/Mutabakat.cs` | `Denk`: `if (ozet != listeToplami)` → `if (false)` | `Mutabakat_Denk_ayrisinca_MutabakatIhlali_ATAR` | CMP-AYNI |
| M24-3 | `Modules/Denetim/DenetimModulu.cs` | `Ozet` süzgeç nesnesine `eylem` yerine `null` verir (**B-7 gerilemesinin birebir kendisi**) | `Mutabakat_Denetim_ozet_toplami_SUZGECLI_liste_toplamina_ESITTIR` | CMP-AYNI |
| M24-4 | `Modules/Denetim/DenetimModulu.cs` | `DenetimSuzgeci.Uygula`: hassas süzgeci satırı silindi | `Mutabakat_Denetim_HASSAS_suzgeci_ozete_de_DUSER` | CMP-AYNI |
| M24-5 | `Modules/Masak/MasakModulu.cs` | `Ozet.suresiGecmis`: `now` → `now.AddYears(-50)` | `Mutabakat_Masak_ozet_ACIK_bildirim_sayisi_kayitlarla_ESITTIR` | CMP-AYNI |
| M24-6 | `Modules/Masak/RiskServisi.cs` | `Ozet.dusuk` → sabit `0` | `Mutabakat_Risk_ozet_seviye_dagilimi_PROFIL_sayisina_ESITTIR` | CMP-AYNI |
| M24-7 | `Modules/Yedek/YedekModulu.cs` | `Ozet.saklananKosu` → sabit `0` | `Mutabakat_Yedek_ozet_saklanan_kosu_KAYITLARLA_ESITTIR` | CMP-AYNI |

**Test açığı bulunmadı** — yedi mutasyonun her biri en az bir testi adıyla kırdı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 uyarı, 0 hata**.

### 5.3 Yeni testlerin listesi (13)
`Mutabakat_UstSinirKapsayici_GUN_verildiginde_ERTESI_GUNE_tasir` · `…_SAATLI_deger_DEGISMEZ` · `…_NULL_kalir` · `Mutabakat_Denk_ayrisinca_MutabakatIhlali_ATAR` · `Mutabakat_Denk_esitse_SESSIZ_gecer` · `Mutabakat_Denetim_ozet_toplami_SUZGECLI_liste_toplamina_ESITTIR` · `Mutabakat_Denetim_ozet_KIRILIM_toplami_toplamla_DENKTIR` · `Mutabakat_Denetim_BITIS_GUNU_kapsama_DAHILDIR` · `Mutabakat_Denetim_bitis_gununden_SONRAKI_kayit_KAPSAM_DISIDIR` · `Mutabakat_Denetim_HASSAS_suzgeci_ozete_de_DUSER` · `Mutabakat_Masak_ozet_ACIK_bildirim_sayisi_kayitlarla_ESITTIR` · `Mutabakat_Risk_ozet_seviye_dagilimi_PROFIL_sayisina_ESITTIR` · `Mutabakat_Yedek_ozet_saklanan_kosu_KAYITLARLA_ESITTIR`

---

## § 6 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

Kiracı: **PAKET-P-24-TEST Kuyumculuk** (`01M337ZBC43A6C8KH5H5YB0K15`), davetle açıldı (kayıt ucu davet jetonu ister — ölçüldü, varsayılmadı; sistem anahtarı yalnız bu koşum için ortam değişkeninden verildi, değeri hiçbir yere yazılmadı).

### 6.1 Özet ↔ liste mutabakatı (B-7)
⚠ **Ölçüm sırası önemlidir ve bu raporda açıkça yazılıdır:** `GET /api/v1/denetim` kendi okumasını denetim izine yazar (`ListeOkumaKaydet`). Liste önce çağrılırsa özet 1 fazla sayar — bu bir mutabakat kusuru **değil**, ölçümün kendi yan etkisidir. İlk koşumda tam bunu gördük (liste 11 ↔ özet 12) ve **gizlemedik**; ölçüm özet-önce sırasına çevrildi.

| Süzgeç | Özet HTTP / toplam | Liste HTTP / toplam | Mutabık |
|---|---|---|---|
| (süzgeçsiz) | 200 / 16 | 200 / 16 | ✔ |
| `eylem=EKLEME` | 200 / 6 | 200 / 6 | ✔ |
| `varlikTuru=Kullanici` | 200 / 1 | 200 / 1 | ✔ |
| `bitis=2026-09-22` | 200 / 19 | 200 / 19 | ✔ |
| `baslangic=2026-09-22&bitis=2026-09-22` | 200 / 20 | 200 / 20 | ✔ |
| `baslangic=2026-09-21&bitis=2026-09-21` | 200 / 0 | 200 / 0 | ✔ |

### 6.2 Bitiş günü off-by-one (B-62)
| İstek | HTTP | toplam | Yorum |
|---|---|---|---|
| `ozet?baslangic=2026-09-22&bitis=2026-09-22` | 200 | **26** | Bugünün tamamı kapsanıyor (**eski davranışta 0 dönerdi**) |
| `ozet?baslangic=2026-09-22&bitis=2026-09-22T00:05:00` | 200 | **0** | Saat bileşeni verilince kural kaydırmaz — sınır gerçekten o an |
| `ozet?baslangic=2026-09-22&bitis=2026-09-22T23:00:00` | 200 | **26** | |
| `ozet?baslangic=2026-09-22&bitis=2026-09-01` (ters) | **400** | — | `"Bitiş tarihi başlangıçtan önce olamaz (başlangıç 2026-09-22, bitiş 2026-09-01)"` — boş liste değil |

### 6.3 Kırılım denkliği (çalışma zamanı kapısı)
`GET /api/v1/denetim/ozet` → `eylemler` 24 = `turler` 24 = `subeler` 24 = `toplam` 24 · `hassasAdet` 0.

### 6.4 SQL kanıtı
```
SELECT COUNT(*) FROM denetim_kayitlari WHERE tenant_id='01M337ZBC43A6C8KH5H5YB0K15';  → 26
SELECT eylem, COUNT(*) … GROUP BY eylem;                                              → OKUMA 17 · EKLEME 9
```
API'nin `bitis=2026-09-22` için döndürdüğü 26 ile SQL'in 26'sı **birebir**.

### 6.5 Temizlik
- API PID öldürüldü; `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**.
- `information_schema`'dan `tenant_id` kolonu olan **63 tablo** + `tenants` + `kiraci_davetleri` temizlendi.
- `SELECT COUNT(*) FROM tenants` → **9** (başlangıç değeri). Kalan kiracılar: Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası Kuyumculuk, Demo Kuyumcu, Ramazan Deneme, **QA-TEST-1/2/3/FRONTEND** — QA fikstürlerine **dokunulmadı**.
- Geçici dosyalardaki jeton/parola/sistem anahtarı `printf '' >` ile boşaltıldı; hiçbir değer rapora yazılmadı.

---

## § 7 — SAPMALAR VE GEREKÇELERİ

1. **Plan "Dosyalar" listesi ile gerçek ayak izi farklı.** Plan `Modules/Rapor/*.cs · Modules/Kasa/KasaModulu.cs, Evrak/EvrakModulu.cs` diyor. Kodu okuduğumda planın saydığı üç çelişkinin **ikisi zaten kapanmıştı**:
   - *vezne ₺0,00 ↔ detay 2.000*: `VezneModulu.Kuyruk` (satır 411-412) ve `Detay` (satır 472-479) ACIK/kalemsiz fişte `TalepTutarTl`'yi gösteriyor — düzeltilmiş, yorumu da yazılı.
   - *evrak portföyü boş ↔ 2 evrak*: `EvrakModulu.Takvim` portföy özetini **aralıktan bağımsız** hesaplıyor (satır ~946, "QA turu 5" yorumu).
   Açık olan mutabakatsızlık **/denetim-izi**'ndeydi (özet ucu listenin süzgeçlerini görmüyordu) ve bu aynı zamanda ek kimlik **B-62**'nin yüzeyi. Bu yüzden ayak izi `Modules/Rapor/Mutabakat.cs` (planın `Modules/Rapor/*.cs` kalemi) + `Modules/Denetim/DenetimModulu.cs` + `Pages/DenetimIzi.razor` oldu; `Kasa/KasaModulu.cs` ve `Evrak/EvrakModulu.cs` **açılmadı** (kapanmış kusuru yeniden yazmak, DoD EK 2'nin "tek dokunuş" kuralına da aykırı olurdu). **Plan üstündür** kuralı gereği bu sapma burada yazılıdır; planın HEDEFİ (özet listeyle aynı sorgudan türesin, geri gelmesin) mekanizmayla karşılandı.
   - *kâr-zarar 11,45 g ↔ 50,38 has*: **DEVREDİLDİ — P-25.** `Modules/Maliyet/KarZararServisi.cs` P-25'in dosya listesindedir ve bu pakette açılırsa tek-el kuralı kırılır. Bu pakette kapatılmadı, **gizlenmedi**.
2. **B-62'nin "kur anlık görüntüsü kirliliği" ayağı** /denetim-izi ekranında karşılığı olmadığı için **P-25'e devredildi** (§ 1).
3. **B-28 ölçümü kuruldu, koşulmadı.** Tarayıcı turu bu paketin işi değil (kapsam kilidi + DoD EK 2). "Pasta %100 çiziliyor" iddiası **yapılmıyor**; ölçüm mekanizması ve fail-closed bulgu kuralı kuruldu.
4. **P-04 sayacı bir kez kırıldı ve düzeltildi.** `DenetimSuzgeci`'ne taşınan şube süzgeci satırının metni değişince `GUVENLIK_ELLE_IZOLASYON` 0 → 1 oldu. Sayaç metin eşleşmesiyle çalıştığı için satır, mühürlü tablo metnine birebir döndürüldü (yerel değişken). Sınıflandırma tablosuna **dokunulmadı**.
5. **Sözlük anahtarı bir kez yanlış sınıfa yazıldı** (`perl -0777` ilk eşleşmeyi değiştirdiği için); ölçülüp doğru sınıfa (Denetim sözlüğü) taşındı, yanlış yerdeki beyanlar kaldırıldı. Derleme bunu yakalamıştı.
6. **Tahmin/gerçekleşen:** tahmin 240 ajan-dk. Gerçekleşen ≈ 150 ajan-dk (mevcut kusurların ikisinin zaten kapalı olması ve migration olmaması sayesinde). Sapma katsayısı ≈ **0,63** (BK-34 eşiğinin altında, takvim yeniden ölçümü gerekmez).

---

## § 8 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her iş kuralı için hatayı yeniden üreten test | KARŞILANDI | 13 test, § 5.3 |
| (b) Mutasyon kanıtı (kır → adıyla kırmızı → geri al → `cmp`) | KARŞILANDI | 7 mutasyon, § 5.2 |
| (c) 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | KARŞILANDI | § 6 |
| (d) `dotnet/denetim.sh` TEMİZ | KARŞILANDI | exit 0, "DENETİM TEMİZ.", 243 ✓ / 0 ✗ |
| (e) Tam süit 0 başarısız | KARŞILANDI | 3812/3812 |
| (f) Test kiracısı silindi, QA-TEST-* korundu | KARŞILANDI | tenants 10 → 9, § 6.5 |
| (g) Uygulama raporu | KARŞILANDI | bu dosya |
| Mekanizma kuralı: sayaçlar `denetim.sh`'a CIRCIR olarak eklendi, taban taze ölçüldü, ölçüm komutu yorumda | KARŞILANDI | § 4.6, `# >>> P-24` bloğu |
| Tip/sözleşme: `Pages` içinde yeni istemci record açılmadı | KARŞILANDI | `DenetimIzi.razor`'a yalnız metot/özellik eklendi; yeni `record` yok |
| PATCH sözleşmesi ihlali yok | KARŞILANDI | PATCH ucu eklenmedi/değiştirilmedi |
| Migration: HAYIR | KARŞILANDI | `Data/` ve `Migrations/` açılmadı; migration sayısı değişmedi |
| Sozlukler TR+EN, çıplak metin yok | KARŞILANDI | § 4.4 |
| COMMIT yok, git komutu yok, rm/mv yok | KARŞILANDI | yalnız `git status --porcelain` okundu |
| Sır/parola/jeton çıktıya yazılmadı | KARŞILANDI | § 6.5 |
| Mevcut element id'leri değişmedi; yeni id `<önek>-<alan>` | KARŞILANDI | yeni id yalnız `f-bitis-ipucu` |
| Yeni CSS'te sabit renk / satır içi style yok | KARŞILANDI | yeni CSS yazılmadı; mevcut `metin-kucuk metin-soluk` sınıfları kullanıldı |

---

## § 9 — AÇIK KALANLAR

1. **B-62 / kur anlık görüntüsü kirliliği → P-25.** /denetim-izi yüzeyinde karşılığı yok; rapor/kur tarafında ölçülmeli.
2. **Kâr-zarar 11,45 g ↔ 50,38 has çelişkisi → P-25.** `Maliyet/KarZararServisi.cs` tek-el zinciri P-25'te açılır; mutabakat sözleşmesi (`Mutabakat.Denk`) orada **hazır** bekliyor.
3. **B-28 ölçümünün ilk gerçek koşumu** bir sonraki QA turunda yapılacak; `grafik_cizilmeyen` ve `grafik_dar_cizilen` sayıları o turun raporuna girmelidir. Sıfırdan farklı çıkarsa düzeltme paketi açılır.
4. **`Mutabakat.Denk` yalnız /denetim-izi özetinde çalışıyor.** Vezne, evrak ve kâr-zarar özetlerine de takılması doğal genişlemedir ama bu paketin dosya ayak izi dışındadır (kapsam kilidi). `OZET_AYRI_SORGU` sayacının nüfusu bugün 4 modüldür; yeni özet ucu açan her paket otomatik olarak kapıya takılır.
5. **`dotnet/denetim.sh.mutbak`** (0 bayt, 18 Eylül, izlenmeyen) ve `Modules/Rapor/RaporModulu.cs.mutorj` dâhil 10 adet 0 baytlık `.mutorj` artığı ağaçta duruyor — **bu paketten değil**, önceki paketlerden kalma. Hook `rm`'i blokladığı için silinmedi; **kurucunun elle silmesi gerekir**.

---

## § 10 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire not yok.** Bu pakette hiçbir mevzuat hükmü (KDV, MASAK, VUK) koda yazılmadı, yorumlanmadı ya da değiştirilmedi.
- **Kurucuya:**
  1. `/denetim-izi` ekranındaki "Toplam kayıt" ve "Kişisel veriye erişim" kartları **artık seçtiğiniz süzgeçlere göre** sayar (kart altında yazıyor). Eskiden kart tüm kayıtları, alttaki liste süzülmüşleri sayıyordu — iki farklı sayı görüyordunuz.
  2. Tarih süzgecinde **bitiş günü artık kapsama dâhil**. Eskiden "1 Eylül – 22 Eylül" derseniz 22 Eylül'ün tamamı listeden düşüyordu.
  3. Silinmesi gereken artık dosyalar § 9/5'te; hook nedeniyle ajanlar silemiyor.
