# PAKET P-67 — Vitrin pano kalem kataloğu: gerekçenin ekranda gösterilmesi — UYGULAMA RAPORU

- **Şerit:** SERİT D (izole VT `kuyumcu_test_m`, canlı port 5185) · Rol: Taşkın (backend) + Ertuğ (frontend) · 2026-09-24
- **Kaynak:** QA turu 8 triyajı (`TRIYAJ-TUR8-2026-09-23.md`, P-67, ORTA, tahmin 1,5 saat)
- **Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı. Tam süitteki 2 kırmızı ve denetimdeki 1 ihlal bu paketten değil; sahipleri § 9'da.

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia | Koddaki durum | Sonuç |
|---|---|---|
| Kalem kümesi kur kaynağının parite kod sözlüğünden gelir (`PanoModulu.cs:94`) | `PanoAyarServisi.Kalemler => [.. MKur.PariteKodlari.Adlar.Keys]` satır 94'te duruyor. `PariteKodlari.Adlar` (`KurModulu.cs:117-141`) 22 kod içeriyor. | Doğru |
| Ekranda kalemin kaynağını anlatan metin yok | `Vitrin.razor` ayar kartında yalnız `AyarAciklama` (marj ticari sırdır) vardı | Doğru, eklendi |
| "Varsayılana dön" satırdan çağrılamıyor | Düğme yalnız çekmecede vardı (`pano-varsayilana-don`) | Doğru, satıra eklendi |
| Atlama sebebi tablosu boşken anlamlı metin yok | `@if (p.Atlananlar.Count > 0)`: atlanan yokken bölüm hiç çizilmiyordu | Doğru, düzeltildi |
| Ekranda "yeni kalem ekle" izlenimi veren devre dışı düğme var | Sayfada böyle bir düğme **yoktu**. Kalan iş, olmadığını test ile kilitlemekti. | Kısmen doğru. Test eklendi. |
| Dosya ayak izi: `Kuyumcu.Tests/VitrinEkranTestleri.cs` | **Yoktu** (`test -e` → YOK). Vitrin testleri `P17EkranTestleri.cs` (P17VitrinEkranTestleri) ve `S35KurVitrinEmanetTestleri.cs` içinde. | Spesifikasyondaki adla **yeni dosya** açıldı. Mevcut dosyalara dokunulmadı. |
| Dosya ayak izi: `Sozlukler.cs` (HUB) | Şerit koşulu bu dosyayı yasaklıyor | HUB'a dokunulmadı. Metinler yeni `Sozlukler.VitrinKalem.cs` dosyasında. |

**Sapma 1: "Kur kaynağı ayarları" yönlendirmesi.** Kalem kümesi, kaynağa göre değişen bir liste değil. Ürün kodundaki sabit bir sözlükten geliyor (`PariteKodlari.Adlar`). Bu yüzden "kaynağı değiştirirseniz yeni kalem gelir" sözü verilmedi. Metin şöyle: "Önce kur kaynağına bakın; kalem ancak kaynak o fiyatı verirse eklenebilir." Bağlantı `/kur#kur-kaynak-kart` adresine gidiyor (Kur ekranındaki kaynak seçici kartı, S-3e). Sistemde `/destek` rotası olmadığı için destek bağlantısı eklenmedi.

**Sapma 2: satırdan dönüş satır içi onayla çalışıyor.** Varsayılana dönmek kiracının ayar satırını siliyor, yani yıkıcıya yakın bir eylem. `ModalPencere` kullanmak denetimdeki "Pages: ModalPencere kullanımı" tavanını değiştirmeyi gerektirirdi (denetim.sh şerit sınırı dışında). Onun yerine satır içi iki adım kullanıldı: önce "Varsayılana dön", sonra soru ile "Evet, dön" ve "Vazgeç". Çekmecedeki mevcut düğmenin P-17 davranışı (onaysız) **korundu**, çünkü P-17 testi bu davranışı sınıyor.

**Sapma 3: bilgi bandı sınıfı.** `app.css` içinde nötr bir `bant-bilgi` sınıfı yok. Ortak CSS de değiştirilemiyor. Bu yüzden mevcut `.bant` sınıfı (belirteçli kenarlık) kullanıldı. Yeni CSS yazılmadı, sabit renk ve satır içi stil de yok.

**Mevzuat:** Bu pakette mevzuat hükmü yok (yalnız ekran açıklaması var). Web araştırması gerekmedi.

## 2. Yapılanlar

### Backend (Taşkın)
- **Kod değişikliği gerekmedi.** Gerekli uçların hepsi zaten vardı: `GET /pano/ayarlar`, `PATCH /pano/ayarlar/{kod}`, `POST /pano/ayarlar/{kod}/varsayilana-don` (`[RolGerekli("SAHIP","MUDUR")]` + `[ModulYetkisi("kur")]`). Canlıda doğrulandı (§ 6).
- Katalog dışı bir kod ile kalem eklenemiyor, sunucu 400 döndürüyor. Canlı kanıt: § 6 adım 6 ve 7.

### Frontend (Ertuğ) — `Kuyumcu.Web/Pages/Vitrin.razor`
1. **Kaynak açıklama bandı** (`#vitrin-kalem-kaynak`) yönetim tablosunun üstünde duruyor. Kalemlerin kur kaynağının fiyat kodlarından geldiğini, elle eklenip silinmediğini ve istenmeyen kalemin "Panoda göster" ile gizlendiğini söylüyor. Altında yeni kalem yönlendirmesi var (`#vitrin-yeni-kalem-yolu`) ve kur kaynağı ayarlarına bağlantı veriyor (`#vitrin-kur-kaynagi` → `/kur#kur-kaynak-kart`).
2. **Düzenlenebilir alanlar işaretli:** alış farkı, satış farkı ve yuvarlama sütun başlıklarında "değiştirilebilir" rozeti var (`#vitrin-baslik-alis-marj`, `-satis-marj`, `-yuvarlama`). Değiştirilebilen alanların tam listesi `#vitrin-duzenlenebilir` satırında yazıyor.
3. **Satırdan "Varsayılana dön":** yeni sütun "Varsayılan".
   - Özel ayarlı kalemde `#vitrin-varsayilana-don-{Kod}` düğmesi çıkıyor. Tıklanınca satır içinde soru (`#vitrin-varsayilana-don-soru-{Kod}`) ile `#vitrin-varsayilana-don-evet-{Kod}` ve `#vitrin-varsayilana-don-vazgec-{Kod}` düğmeleri açılıyor. Onay verilince POST gidiyor, başarı bandı çıkıyor (UX-S27) ve pano yeniden çekiliyor (UX-S23).
   - Varsayılandaki kalemde düğme yok, yerine "Varsayılan" durumu yazıyor (`#vitrin-varsayilan-{Kod}`). Böylece ekranda tıklanınca hiçbir şey olmayan düğme kalmıyor.
   - Hata, çekmece kapalıyken sayfa bandına (`#vitrin-hata`), çekmece açıkken çekmece bandına yazılıyor (UX-S28).
4. **Atlananlar bölümü artık her zaman çiziliyor.** Atlanan yoksa `#vitrin-atlanan-yok` "Atlanan kalem yok — açık kalemlerin hepsi panoda." yazıyor. Bu metin yalnız istek başarılıyken çıkıyor, hata durumu ayrı bantta kalıyor.
5. Mevcut element id'lerinin hiçbiri değişmedi. S3-5 sadeleştirmesine (`vitrin-kur-don`, kaynak adı) dokunulmadı.

### Metinler — `Kuyumcu.Core/I18n/Sozlukler.VitrinKalem.cs` (YENİ, partial değil, ayrı record)
- `VitrinKalemMetinleri` konumsal record + `VitrinKalemSozlugu.Tr/En`. KurVitrinEmanetSozlugu deseni kullanıldı: bir dilde alan eksik kalırsa derleme kırılıyor.
- Metinler jargonsuz ve ham kod içermiyor. EN karşılıkları mevcut "Show on the board" metniyle tutarlı.

## 3. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Pano kalem ayarı (`kiraci_ayarlari`, anahtar `pano_kalem.<KOD>`) | PATCH `/pano/ayarlar/{kod}` (çekmece) · POST `/varsayilana-don` (çekmece ve **artık satır**) · "Panoda göster" ile pasife alma | P-17'den beri var. Denetim kaydı canlıda görüldü (EKLEME PanoKalemAyari, SILME KiraciAyari). |
| Pano kalem **kataloğu** (hangi kalemlerin var olduğu) | **Salt okunur, gerekçesi ekranda:** katalog kur kaynağının parite kod sözlüğünden geliyor. Kalem eklemek bir veri girişi değil, kur kaynağı değişikliği. | UX-1 "gerekçeli istisna". Katalog dışı kod API'de 400 dönüyor (canlı). |

Yeni varlık yok, migration yok.

## 4. Derleme ve testler

- `dotnet build Kuyumcu.sln --nologo` → **0 uyarı, 0 hata** (son temiz derleme mutasyonlardan sonra alındı).
  - Not: ilk denemede başka bir şeridin eşzamanlı derlemesi `Kuyumcu.Web.pdb` dosyasını kilitledi (`MSB` "being used by another process"). Tekrar denemede yeşil geçti. Bir ara derlemede görülen `EtiketEkranTestleri.cs(145)` xUnit1031 ve `KarZarar.razor(528)` CS8604 uyarıları başka şeritlerin dosyalarında; son derlemede 0 uyarı ölçüldü.
- **Filtreli koşum** (`KUYUMCU_TEST_DB=kuyumcu_test_m`, `~VitrinEkranTestleri|~P17VitrinEkranTestleri|~S35KurVitrinEmanet`): **21/21 geçti**. P-67'nin 10 yeni testi, P-17 vitrin testleri ve S3-5 testleri birlikte koşuldu.
- **Yeni testler (10)** — `Kuyumcu.Tests/VitrinEkranTestleri.cs`:
  - `PanoKalemKaynakAciklamasiGorunur` *(spesifikasyon)*
  - `SatirdanVarsayilanaDonCalisir` *(spesifikasyon)*
  - `AtlamaSebebiBosDurumMetniVar` *(spesifikasyon)*
  - `Duzenlenebilir_sutunlar_ACIKCA_isaretli`
  - `Satir_onayinda_Vazgec_istek_ATMAZ_ve_dugmeyi_geri_getirir`
  - `Satirdan_donus_hatasi_SAYFA_bandinda_gorunur`
  - `Varsayilandaki_kalemde_satirda_DUGME_yok_durum_YAZILI`
  - `Atlanan_varsa_tablo_cizilir_bos_durum_metni_CIKMAZ`
  - `Yeni_kalem_ekle_izlenimi_veren_DUGME_yok_ve_her_dugme_istek_atar_ya_disabled`
  - `Ingilizce_metinler_de_DOLU`
- Sahte API '{}' ile bırakılmadı: `/pano/guncel`, `/pano/ayarlar` ve `/varsayilana-don` sunucu sözleşmesindeki alan adlarıyla yanıt veriyor. Dönüşten sonra GRAM `ozel=false` geliyor ve ekran bu yanıtı okuyor.
- Kendi test hatam, düzeltildi: ilk koşumda "yeni" kelimesini arayan kontrol "Yeni**le**" düğmesini yakaladı. Kontrol "ekle", "yeni kalem" ve "+" ile başlayan düğmeleri arayacak şekilde daraltıldı.
- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_m`, trx: `scratchpad/trx-pP-67`): **4854 test · 4852 geçti · 2 başarısız · 0 atlandı**. Kırmızılar § 9'da; ikisi de bu paketin dosyalarında değil.
  - Test sayısı önce/sonra: önce **ölçülmedi** (ağaç 5 şeritle eşzamanlı değişiyor). Sonra 4854 ölçüldü. Bu paketin katkısı +10 test.

## 5. Mutasyon kanıtları

Yöntem: `Vitrin.razor` dosyası `scratchpad/mut-yedek/P-67/` altına kopyalandı. Her mutasyonda `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı ve filtreli test koşuldu. Ardından yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu gösterildi. Betik: `scratchpad/mut67.sh`, günlükler `scratchpad/mut-M67-*.txt`.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M67-1 | Kaynak açıklama bandı (`#vitrin-kalem-kaynak` div'i) silindi | `PanoKalemKaynakAciklamasiGorunur` | birebir aynı |
| M67-2 | Boş durum metni (`#vitrin-atlanan-yok`) silindi | `AtlamaSebebiBosDurumMetniVar` | birebir aynı |
| M67-3 | Satır içi onay dalı kapatıldı (`else if (false && onayKod == a.Kod)`) | `SatirdanVarsayilanaDonCalisir`, `Satir_onayinda_Vazgec_istek_ATMAZ_ve_dugmeyi_geri_getirir`, `Satirdan_donus_hatasi_SAYFA_bandinda_gorunur` | birebir aynı |
| M67-4 | Yuvarlama başlığındaki "değiştirilebilir" rozeti silindi | `Duzenlenebilir_sutunlar_ACIKCA_isaretli` | birebir aynı |

Spesifikasyon listesindeki iki mutasyon (M67-1, M67-2) ve iki ek mutasyon yazılı adlı testleri kırdı. Test açığı bulunmadı. Son mutasyondan sonra alınan temiz derleme 0 uyarı, 0 hata verdi.

## 6. Canlı doğrulama (port 5185, `kuyumcu_net`)

API şu komutla başlatıldı: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5185`. Başlatan süreç 84823, dinleyen süreç 84854. Kiracı tek kullanımlık bir davetle açıldı (`kiraci_davetleri`, SHA-256 özetiyle). Jeton ve parola ekrana basılmadı. Kiracı: **PAKETP67-TEST**. Betikler `scratchpad/pP-67/canli.sh` ve `canli.py`, çıktılar `canli-cikti.txt`, `canli.json` ve `canli-sql.txt`. Yanıtların hepsi okundu.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit (davetli) | 201 | 201 | jeton alındı |
| 2 | GET /pano/ayarlar | 200, 22 kalem (= `PariteKodlari.Adlar`), hepsi ozel=false | 200 | kalem=22, ozel=0, kodlar GRAM…ONS |
| 3 | GET /pano/guncel | 200 | 200 | satır=7, **atlanan=0** → bu kiracıda ekran yeni boş durum metnini basar |
| 4 | PATCH /pano/ayarlar/GRAM {satisMarjTl:25, yuvarlamaAdimi:5} | 200, ozel=true | 200 | ozel=True, satisMarj=25, yuvarlama=5, alisMarj=0 (gönderilmeyen alan korundu) |
| 5 | POST /pano/ayarlar/GRAM/varsayilana-don (satırdaki düğmenin ucu) | 200, ozel=false | 200 | ozel=False, satisMarj=0, yuvarlama=10 |
| 6 | POST /pano/ayarlar/YENI_KALEM/varsayilana-don | 400/404 | 400 | "Geçersiz parite kodu: YENI_KALEM" |
| 7 | PATCH /pano/ayarlar/YENI_KALEM (API'den kalem eklenemez) | 400/404 | 400 | "Geçersiz parite kodu: YENI_KALEM" |
| 8 | GET /pano/ayarlar (sonra) | 200, 22 kalem, ozel=0 | 200 | kalem=22, ozel=0 → katalog değişmedi |
| 9 | GET /pano/ayarlar jetonsuz | 401 | 401 | |

**SQL kanıtı** (`canli-sql.txt`):
```
kiraci_ayarlari pano_kalem.* (varsayılana dönüşten sonra): 0
denetim_kayitlari (eylem, varlik_turu, varlik_id — tarih sırasıyla):
  EKLEME  PanoKalemAyari  GRAM         ← PATCH
  EKLEME  KiraciAyari     01M38DK55J…  ← ayar satırı yazıldı
  EKLEME  PanoKalemAyari  GRAM         ← "varsayılana döndürüldü" iz kaydı
  SILME   KiraciAyari     01M38DK55J…  ← ayar satırı silindi
```

**Ekranın çalışan derlemede olduğu:** `Kuyumcu.Web/bin/Debug/net8.0/Kuyumcu.Web.dll` içinde `vitrin-atlanan-yok`, `vitrin-kalem-kaynak` ve `VitrinKalemSozlugu` dizeleri var. `Kuyumcu.Api/bin/.../Kuyumcu.Core.dll` içinde `VitrinKalemSozlugu` var.

**Tarayıcıda görsel doğrulama YAPILMADI.** Ekrana girmek için giriş formuna parola yazmak gerekiyor ve bu ajan bunu yapmıyor. Ekran davranışı 10 bUnit testi (id'ler üzerinden) ve uçların canlı yanıtlarıyla doğrulandı. 375/768/1440 px taşma ölçümü orkestratörün yayınına ve QA turuna bırakıldı.

**Yan gözlem (bu paketten değil):** API günlüğünde `is_calismalari` benzersiz anahtar ihlali var (`MASAK_SIB_SURESI`/`SAKLAMA_ONIZLEME` için 2026-09-24 tekrar kaydı). Aynı veritabanında birden çok şeridin API'si zamanlanmış işleri aynı anda koşuyor. Bu durum üretimde tek süreçle oluşmaz, yine de orkestratöre not düşüldü.

**Kapanış:**
- API durduruldu (`kill 84823 84854`). `lsof -nP -iTCP:5185 -sTCP:LISTEN` **0 satır** döndü.
- PAKETP67-TEST kiracısının satırları `tenant_id` kolonu olan **70 tablonun hepsinden** ve `tenants` tablosundan silindi. Davet satırı da silindi. İlk silmeden sonra başka şeritlerin kur zamanlayıcısı 1 `kur_snapshots` satırı ve 1 `denetim_kayitlari` satırı yazmıştı. Bunlar ikinci geçişte silindi ve 15 saniye sonra yeniden ölçüldü: **kalan satır 0**.
- `QA-TEST-*` kiracılarına dokunulmadı (4 adet duruyor).
- `tenants` sayısı: başlangıçta **10**, sonda **10**.

## 7. denetim.sh

`bash dotnet/denetim.sh` → **rc=1 · 285 ✓ · 1 ✗**:
```
✗ Sınırsız ToListAsync (Modules · B-8)       146 (tavan 145 — ARTIŞ)
```
Bu ihlal `Kuyumcu.Api/Modules` altındaki bir dosyadan geliyor. P-67 hiçbir `Modules` dosyasına dokunmadı (backend değişikliği yok), yani ihlal bu paketin ürünü değil. Adaylar § 9'da.

Bu paketin ilgili sayaçlarının hepsi ✓:
- Pages: `@onclick`'siz ve `disabled`'sız `<button>` 58 (tavan 71)
- Pages: ModalPencere kullanımı 48 (tavan 55, artış yok)
- Satır içi `style=` 110 (tavan 196)
- P-17 PANO_MARJ_SIZINTISI 0
- P-17 PANO_IKINCI_HESAP_YOLU 0
- P-17 PANO_AYAR_KIRACIDAN ✓

## 8. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` (ağaç, ilk commit'ten beri çoğunlukla izlenmiyor). Bu paketin dokunduğu dosyalar:
```
?? dotnet/Kuyumcu.Web/Pages/Vitrin.razor                 (değişti; dosya ilk commit'ten sonra doğduğu için ?? görünür)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.VitrinKalem.cs     (YENİ)
?? dotnet/Kuyumcu.Tests/VitrinEkranTestleri.cs           (YENİ; Kuyumcu.Tests/ dizini bütünüyle ?? görünür)
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-67-uygulama-raporu.md (bu rapor)
```
Dokunulan sayfalar (DoD EK 2, TEK DOKUNUŞ): **`Pages/Vitrin.razor`**. Çakışma haritasındaki sıra P-59 → **P-67** → P-65. Bu sayfaya bu dalgada başka dokunuş yapılmadı.

Dokunulmayanlar: `Sozlukler.cs` (HUB), `app.css`, ortak bileşenler, `denetim.sh`, diğer şeritlerin dosyaları.

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Pano ayarları ekranında kalem kaynağını açıklayan metin görünür | KARŞILANDI | `#vitrin-kalem-kaynak` · `PanoKalemKaynakAciklamasiGorunur` · M67-1 |
| Her kalem satırında marj ve yuvarlama düzenlenebilir, "varsayılana dön" satırdan çağrılabilir | KARŞILANDI | Başlık rozetleri + `Duzenle` çekmecesi (P-17) · `#vitrin-varsayilana-don-{Kod}` · `SatirdanVarsayilanaDonCalisir` · M67-3/M67-4 · canlı adım 4-5 |
| Atlama sebebi tablosu boşken "atlanan kalem yok" metni basar | KARŞILANDI | `#vitrin-atlanan-yok` · `AtlamaSebebiBosDurumMetniVar` · M67-2 · canlıda atlanan=0 |
| Ekranda "yeni kalem ekle" izlenimi veren devre dışı düğme kalmaz | KARŞILANDI | Böyle bir düğme yok, test ile kilitlendi: `Yeni_kalem_ekle_izlenimi_veren_DUGME_yok_…` |
| Yeni kalem talebi için yönlendirme metni (kur kaynağı ayarları) | KARŞILANDI | `#vitrin-yeni-kalem-yolu` + `/kur#kur-kaynak-kart` bağlantısı |
| Paket kapısı (1) build 0 hata | KARŞILANDI | 0 uyarı, 0 hata |
| (2) kendi testlerim 0 başarısız | KARŞILANDI | 21/21 (10 yeni) |
| (3) kendi sayaçlarım ✓ | KARŞILANDI | § 7 |
| (4) mutasyon kanıtı | KARŞILANDI | 4/4 kırmızı, cmp birebir |
| (5) canlı doğrulama | KARŞILANDI | 9/9 beklenen kod + SQL |
| (6) test kiracısı silindi | KARŞILANDI | kalan 0, tenants 10 → 10 |
| (7) yeni kırmızı üretilmedi | KARŞILANDI | Tam süitteki 2 kırmızının ikisi de başka şeritlerin test ve dosyalarında. P-67 bu dosyalara dokunmadı. |

## 10. Açık kalanlar (sahipleriyle)

1. **Tam süit kırmızısı** `S25YazdirParaUstuEkranTestleri.S25_E4_Ham_ULID_banda_basilmaz_adreste_durur` → sahibi **S-2 (S2-5 Tezgâh fişi / Kaydet→Yazdır)**.
2. **Tam süit kırmızısı** `KarZararEkranTestleri.P64_02_Degisim_null_iken_tire_basilir` → sahibi **SERİT C (P-64 rapor grafikleri)**.
3. **denetim ✗** `Sınırsız ToListAsync (Modules · B-8) 146 > 145`. P-67 Modules'a dokunmadı. En yeni değişen Modules dosyaları: `Satis/SatisModulu.Satici.cs` (03:39, `:81 .ToListAsync()`, yeni dosya → **S-2** adayı) ve `Kurulum/*` (**SERİT C / P-68** adayı). Kesin sahip ölçülmedi; orkestratör kapısında belirlenmeli.
4. Tarayıcıda görsel doğrulama (parola girişi gerektiriyor) orkestratörün 5199 yayınına ve QA turuna bırakıldı.
5. Satırdaki "varsayılana dön" iki adımlı satır içi onay kullanıyor. Çekmecedeki P-17 düğmesi hâlâ onaysız. İkisinin tek kalıba (ortak onay) toplanması, `ModalPencere` tavanı ya da ortak onay bileşeni kararıyla birlikte ayrı bir işte ele alınabilir.

## 11. Kurucuya / mali müşavire not

Mevzuat içeriği yok. Kurucuya not: "yeni pano kalemi" isteği ürün (kod) değişikliğidir. Kur kaynağı o fiyatı veriyorsa `PariteKodlari` sözlüğüne kod eklenir. Ekran bunu kullanıcıya bu dille söylüyor.
