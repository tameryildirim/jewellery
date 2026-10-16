# PAKET P-52 — Sipariş düzenleme formunda ürün seçici + tahmini tutar (yalnız arayüz)

> Kaynak: QA turu 8 triyajı (`TRIYAJ-TUR8-2026-09-23.md`, satır 83) · Ciddiyet **YÜKSEK** · Triyaj tahmini 2,0 sa (TAHMİN)
> Şerit: **SERİT A — SİPARİŞ + FATURA** · izole test veritabanı `kuyumcu_test_a` · canlı doğrulama portu **5197**
> Zincir: **P-51 → P-52** (aynı dosya). P-51 çalışma ağacında BULUNDU ve üzerine yazıldı.
> **DURUM (2. tur, 24.09.2026 00:20): BİTTİ** — paralel şerit kapısının 7 maddesi sağlandı (§ 12).
> 1. turdaki (23.09 16:58) engel — eksik `sema` migration'ı — kapanmıştı (`20260923135612_B1KullaniciGorunumTercihi`);
> canlı doğrulama bu turda 5197'de koşuldu, tam süit yeniden koşuldu. § 1–§ 11 1. turun kaydıdır
> (tarihsel, değiştirilmedi); **güncel ölçüm § 12'dedir ve § 7/§ 8/§ 10'un ❌ satırlarının yerini alır.**

---

## 1. Spesifikasyonun koda karşı doğrulanması

| Spesifikasyon iddiası | Kodda ölçüldü | Sonuç |
|---|---|---|
| "Kalem birleştirme `urunId`'yi korur (SiparisModulu.cs:559-596)" | `KalemBirlestir` + `KalemKur` — gerçek satırlar **`SiparisModulu.cs:918-980`** (P-51 sonrası kaydı) | **DOĞRU**, satır no kaymış |
| "`tahminiTutarTl` PATCH'te kabul edilir (:540-544)" | `TahminDogrula` çağrısı **`:634-641`**, gövdesi **`:816-825`** | **DOĞRU**, satır no kaymış |
| "Eksik yalnızca ekranda" | Düzenleme formunda ürün **salt okunur rozet**ti; ayar **serbest metin**ti; tahmini tutar **düz metin**ti (form alanı yoktu) | **DOĞRU** |
| "Kalem PATCH gövdesi `id` alanını korumalı" | Ekran `id`'yi **zaten gönderiyordu** (S-3a paketi). P-52 bunu **testle kilitledi** (mutasyon M52-1) | Sapma yok |

**Triyajın doğrulayamadıkları — P-52 kapsamındaki tek kalem ölçüldü:**
- Sunucu `ayarId`'yi zaten `Ayarlar.Bul(...)` ile doğruluyor (`SiparisModulu.cs:959-963`) →
  ekrandaki serbest metin kullanıcıyı 400'e sürüyordu. Ayar seçicisinin kaynağı bu yüzden
  **`Kuyumcu.Core.Domain.Ayarlar.Tumu`** seçildi: ekranda seçilebilen her ayar sunucuda da geçerli (UX-S31).
- `Siparis.SubeId` sütunu, fatura taslak DELETE ucu, `/yedekler` gerekçe ucu, `/kur/guncel` rota dağılımı,
  6648 KurSnapshot kaydı → **P-52 kapsamı dışı**, ölçülmedi, tahmin de edilmedi.
- **Mevzuat/yasal hüküm yok** → koda sabit yazılan bir hüküm yok.

---

## 2. Yapılanlar

**`Kuyumcu.Web/Pages/SiparisDetay.razor` (tek dokunulan ekran)**

1. **Kalem başına ürün seçici.** Salt okunur rozet → ortak `Secici` (arama kutusu ve klavye erişimi
   bileşenin kendisinde; P-36c gereği bu ekranda ham seçim öğesi açılamaz).
   - **Eleman kimliği DEĞİŞMEDİ:** `sipd-d-kalem-urun-{sıra}`.
   - Mevcut ürün **ön seçili** gelir (UX-S21).
   - Kalemin kendi ürünü sunucunun ilk 100'ünde olmasa bile seçenekten **düşmez**
     (`UrunSecenekleri(sira)` — aksi hâlde kaydette ürün bağı sessizce kaybolurdu).
   - Ürün seçilince **ayar ve gram ürün kartından türetilir** (oluşturma formuyla aynı — UX-S26).
   - Ürün bağı kaldırılınca yalnız ad düşer; kullanıcının girdiği ayar/gram **sessizce silinmez**.
2. **Ayar sözlükten seçim.** `Metin` (serbest, 10 karakter) → `Secici`, kaynak `Ayarlar.Tumu`
   (altın/gümüş/platin/paladyum/çelik, maden → milyem sırasıyla). Kimlik `sipd-d-kalem-ayar-{sıra}` aynı.
3. **Tahmini tutar düzenlenebilir alan.** `Para` bileşeni (`sipd-d-tahmini`, ParaBirimi `TRY`, EnAz 0).
   - Kur anlık görüntüsü kullanıcıdan **istenmez**: `/kur/guncel` sayfa açılışında çekilir, gövdeye
     ekran koyar (`EtkinKurId` = canlı kur, yoksa kaydın kendi kuru).
   - **Kur yoksa istek HİÇ GİTMEZ:** alan altına `Dil.T.Siparis.KurYok` yazılır (DoD EK md.3 + UX-S30).
     Ayrıca kur yokken alanın altında kalıcı sebep satırı (`sipd-d-tahmini-kur-yok`) durur.
   - Tutar silinirse `kurSnapshotId` de `null` gider — "hangi kurdan söz verildi" tutarsız kalmaz.
4. **UX-S29:** ürün listesinin sunucu sınırı (100) ve daraltma yolu ekranda yazar (`sipd-d-urun-sinir`).
5. `/urunler?aktif=true&limit=100` ve `/kur/guncel` **sayfa açılışında** yüklenir (P-51'de alınan ders:
   düzenleme açılınca boş seçici çıkmaz). İkisinin de `catch`i **UX-S28 uyumlu** — liste boşaltılmaz,
   hata bandına yazılır.

**Dokunulmayanlar:** `Kuyumcu.Core/I18n/Sozlukler.cs` (ortak hub — **hiç açılmadı**, yeni anahtar
gerekmedi; mevcut `Siparis.Urun · Ayar · TahminiTutar · KurYok · TeklifKaynak · UrundenGeldi ·
SinirUyarisi` yetti), `Kuyumcu.Contracts` (ürün/kur satırı `Sayfalar.Siparisler`den **yeniden kullanıldı** —
ikinci tip türetilmedi), `MIMARI-KALAN-PAKETLER-*.md`, `TRIYAJ-TUR8-*.md`, `SONRAKI-OTURUM-PROMPT.md`.

**Dokunulan sayfalar (DoD EK 2 · tek dokunuş):** `Pages/SiparisDetay.razor` — bu dalgada P-51 ile
birlikte **zincirli tek şerit**; ikinci bağımsız dokunuş değildir.

---

## 3. Migration

**GEREKMEDİ.** P-52 yalnız arayüzdür: yeni varlık, yeni sütun, şema değişikliği yok.
`dotnet ef` hiç çalıştırılmadı. Mevcut migration sayısı değişmedi.

---

## 4. DoD sözleşmesi (Bertan koşulu)

P-52 **yeni varlık/kayıt türü doğurmaz** — mevcut `Siparis` + `SiparisKalemi` üzerinde çalışır.

| Varlık | Düzenleme yolu | Silme / pasifleştirme | Yetki | Denetim izi |
|---|---|---|---|---|
| `Siparis.TahminiTutarTl` + `KurSnapshotId` | **YENİ:** `PATCH /siparisler/{id}` — ekranda `sipd-d-tahmini` alanı | Tutar `null` gönderilerek silinir (kur da birlikte düşer) | Mevcut `[RolGerekli]`/`[ModulYetkisi]` kapısı — **değişmedi** | `DenetimServisi.YazmaKaydet` dinleyicisi üzerinden kendiliğinden (yeniden yazılmadı) |
| `SiparisKalemi.UrunId` | **YENİ:** kalem başına `sipd-d-kalem-urun-{n}` seçicisi, gövdede `id` ile → kalem **güncellenir**, yenisi doğmaz | Ürün bağı `null` → yalnız bağ kalkar; kalem `sipd-d-kalem-sil-{n}` ile (son kalem kilitli) | aynı | aynı |
| `SiparisKalemi.AyarId` | **YENİ:** sözlük seçicisi | Boş seçenek (`—`) ile silinir | aynı | aynı |

**Salt okunur kalanlar / gerekçe:** TASLAK dışı sipariş düzenlenmez — düzenle düğmesi çizilmez, yerine
`DegismezIpucu` çıkar (mevcut davranış, P-52 değiştirmedi). **Finansal hard DELETE yok** — sipariş modülü
deftere yazmaz; kalem silme yalnız TASLAK'ta ve mali kayıt üretmez.

**Her düğme gerçek istek atar ya da kilitli + sebebi yazılıdır:** `sipd-d-kaydet` (PATCH),
`sipd-d-vazgec` (form kapatır), `sipd-d-kalem-ekle` (satır ekler), `sipd-d-kalem-sil-{n}`
(tek kalemde `disabled`). Yeni `ModalPencere` düzenleme kullanımı **0** (tek düzenleme kalıbı korundu).

---

## 5. Derleme ve testler

| Ölçüm | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo -t:Rebuild` | **0 Hata** · 4 Uyarı — **hiçbiri P-52'nin değil** (2× `MSB3026` dosya kilidi: başka şeridin API süreci; 2× `CS8604` `Kuyumcu.Tests/OlcumKapisiTestleri.cs:525` → **DALGA 7c şeridinin dosyası**, sınırım dışı, dokunmadım) |
| P-52 dosyalarının uyarısı | **0** |

**Test koşumları (hepsi `KUYUMCU_TEST_DB=kuyumcu_test_a`):**

| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `SiparisEkranTestleri` (öncesi) | 17 | 17 | 0 |
| `SiparisEkranTestleri` (sonrası) | **21** | **21** | 0 |
| `SiparisEkranTestleri` + `S3aSiparisDuzenlemeEkranTestleri` | **24** | **24** | 0 |
| **Tam süit** | 4009 | 2319 | **1690** — § 7 |

**Yeni test sayısı: 4** (+ 1 mevcut testin yeni kalıba uyarlanması)

| Test | Ne ölçer |
|---|---|
| `DuzenlemeFormundaUrunSeciciGorunur` | Ürün seçicisi çizilir, mevcut ürün ön seçili (`data-deger="u-1"`), ayar da seçici (`K22`), sınır uyarısı var |
| `UrunDegisiminceKalemKimligiKorunur` | Ürün değişip kaydedilince gövdede kalem `id="k1"` **korunur**, `urunId="u-2"`, ayar ürün kartından `K14`, kalem **sayısı 1** (çoğaltma yok) |
| `TahminiTutarEkrandanKaydedilir` | `31500` girilip kaydedilince gövdede `tahminiTutarTl=31500` **ve** `kurSnapshotId="kur-1"` |
| `TahminiTutarKursuzIseIstekGitmez` | Kur yokken: sebep satırı çizilir, tutar girilip kaydet basılınca **alan altı hata** var ve **hiç PATCH gitmez** |

**Uyarlanan test:** `S3aEkranTestleri.cs` → `Duzenleme_formu_urun_ayar_ve_grami_GOSTERIR`.
Ürün ve ayar artık `Secici` taşıdığı için okunan öznitelik `value` → `data-deger` oldu; ürün adının
görünürlüğü (UX-S21) **aynı iddiayla korundu**, üstüne `data-deger="u-1"` iddiası eklendi.
**Eleman kimlikleri değişmedi.**

**Sahte API güncellemesi:** `/urunler` iki ürün döndürür (`u-1 Bilezik K22 10,0g` · `u-2 Künye K14 22,5g`);
`/kur/guncel` yanıtı artık test alanından gelir (`kurYaniti`) ki "kur yok" hâli **uydurulmadan**, sahte
sunucunun gerçek yanıtı değiştirilerek ölçülsün. Yeni `KursuzTaslakDetay` fikstürü eklendi.

---

## 6. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M52-1** | Kalem gövdesinden `id` çıkarıldı (`id = string.IsNullOrWhiteSpace(k.Id) ? null : k.Id,` → `id = (string?)null,`) | **`UrunDegisiminceKalemKimligiKorunur`** → `Başarısız` (Geçti 20 / Başarısız 1) | `cmp` → **birebir aynı** |
| **M52-2** | Kur zorunluluğu kapısı etkisizleştirildi (`if (dTahminiTutar is not null && !KurVar)` → `if (false)`) | **`TahminiTutarKursuzIseIstekGitmez`** → `Başarısız` (Geçti 20 / Başarısız 1) | `cmp` → **birebir aynı** |

Test açığı bulunmadı — her mutasyon **tam olarak hedeflediği testi** kırdı, başkasını kırmadı.
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln -t:Rebuild` → 0 hata.

---

## 7. ⚠ TAM SÜİT KIRMIZI — SEBEBİ P-52 DEĞİL (Anayasa md.1: gizlenmedi)

`dotnet test` (tam süit, `kuyumcu_test_a`): **4009 test · 2319 geçti · 1690 başarısız.**
TRX: `…/scratchpad/trx-pP-52/_Ramazan-MacBook-Air_2026-09-23_16_52_08.trx`

| Adet | Hata | Kaynak |
|---|---|---|
| **1664** | `DbUpdateException` → `MySqlException: Unknown column 'sema' in 'field list'` | **P-52 DIŞI.** `Kuyumcu.Api/Data/Entities.cs:308` → `Kullanici` varlığına `[Column("sema")] Sema` eklenmiş ama **karşılık gelen migration YOK** (`grep -rln '"sema"' Kuyumcu.Api/Data/Migrations` → **0 dosya**). Her kullanıcı INSERT'i patlıyor. |
| **21** | `Cannot provide a value for property 'BenimGorunum' … no registered service 'KullaniciGorunumServisi'` | **P-52 DIŞI.** `Pages/Ayarlar.razor`a yeni servis enjekte edilmiş, test kayıtları güncellenmemiş. |
| **3** | `Kayit_kiraci_…` → `Expected Created, Actual InternalServerError` | Aynı `sema` kökü |
| **1** | `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | **P-52 DIŞI** — DALGA 7c şeridinin dosyası |
| **1** | `S3aSiparisDuzenlemeEkranTestleri.Duzenleme_formu_urun_ayar_ve_grami_GOSTERIR` | **BENİMDİ — DÜZELTİLDİ** (§ 5), şimdi yeşil |

`Sema` · `KullaniciGorunumServisi` · `GorunumSecenekleri.Semalar` aynı eşzamanlı "Görünüm" paketinden
geliyor. **P-52 hiçbir varlığa, migration'a, DI kaydına dokunmadı.** Kendi kapsamımdaki 24 testin
tamamı yeşil. **Orkestratöre:** bu üç kalem kapatılmadan tam süit hiçbir şeritte yeşil olamaz.

---

## 8. Denetim ve canlı doğrulama

### `dotnet/denetim.sh` → **TEMİZ DEĞİL** (sebebi P-52 değil)

```
✗ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       48 > tavan 47
DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli.
```
*(ayrıca 4 adet ⚠ ÜST SINIR/BAYAT damgası: `YAYIN_TAZE`, `ICLOUD_ESITLEME_ACIK`, `QA_TUR_SURESI_OLCUM`,
`DEMO_INSAN_SURESI` — hepsi P-52 öncesinden.)*

**P-52'nin katkısı ölçüldü = 0.** Denetimin kendi ölçüm komutu `SiparisDetay.razor` üzerinde koşuldu:
```
perl -0777 -ne 'while(/catch\s*\([^)]*\)[^{;]*\{(.*?)\}/gs){$b=$1;
  while($b=~/(\w+)\s*=\s*(\[\]|null)\s*;/g){print "x\n"}}' Kuyumcu.Web/Pages/SiparisDetay.razor
→ 0 satır
```
Eklediğim iki `catch` UX-S28 uyumludur (liste boşaltmaz, hata bandına yazar). 48'inci vuruş bugün
başka şeritlerin dokunduğu `.razor` dosyalarından geliyor (bugün değişen: `Ayarlar` 3, `UstBar` 3,
`KarZarar` 1, `Yedekler` 0, `Giris` 0, `Panel` 0 — hepsi **sınırım dışı**, dokunmadım).

### Canlı doğrulama (5197) → **KOŞULAMADI**

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | API `--urls http://localhost:5197` | ayağa kalkar | **KALKTI** | `Now listening on: http://localhost:5197` (log satır 36) |
| 2 | `POST /auth/kayit` (davetsiz) | 403 | **403** | `Geçerli bir davet jetonu gerekiyor…` |
| 3 | `kiraci_davetleri` satırı + `POST /auth/kayit` | **201** | **500** | API logu: `Unknown column 'sema'` → test kiracısı **doğmadı** |
| 4-8 | Ürün/cari/sipariş kur, kalemin ürününü PATCH ile değiştir, GET'te kalem `id` sabit mi, tahmini tutar görünür mü, kursuz PATCH 400 mü | — | **KOŞULAMADI** (kiracı yok) | — |

`kuyumcu_net` şemasında da `sema` sütunu **yok** (`information_schema` sorgusu → `0`), yani engel
test veritabanına özgü değil: **eksik migration canlıda da duruyor.** Mevcut bir kiracıya yazmak veri
kirliliği olurdu, `QA-TEST-` fikstürlerine dokunmak yasak, eksik sütunu elle eklemek o şeridin
migration'ını kıracağı için yapılmadı. **Tahmin edilmedi, uydurulmadı — ölçüldü ve raporlandı.**

**Temizlik (yapıldı):**
- Eklediğim `kiraci_davetleri` satırı silindi → `SELECT COUNT(*) … id='p52-davet-0001'` = **0**
- `SELECT COUNT(*) FROM tenants` → **9 → 9** (başlangıç = son) · `… WHERE ad LIKE 'PAKET52%'` = **0**
- `SELECT COUNT(*) FROM kullanicilar WHERE email='p52@ornek.local'` = **0**
- API süreci durduruldu · `lsof -nP -iTCP:5197 -sTCP:LISTEN` → **0 satır**
- **5199 (kurucunun inceleme sunucusu) DOKUNULMADI** ve hâlâ ayakta (2 LISTEN satırı). 5198/5200 kullanılmadı.
- `QA-TEST-` önekli kalıcı QA fikstürlerine dokunulmadı. Proje ağacına geçici dosya bırakılmadı.

---

## 9. Değişen dosyalar (P-52)

```
dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor
dotnet/Kuyumcu.Tests/SiparisEkranTestleri.cs
dotnet/Kuyumcu.Tests/S3aEkranTestleri.cs
dokumanlar/09-uygulama-kayitlari/PAKET-P-52-uygulama-raporu.md   (bu dosya)
```
*(`git status --porcelain` çalışma ağacının tamamında kirli — depo "first commit"ten beri
commit'lenmiyor (kurucu yasağı); yukarıdaki liste P-52'nin **gerçekten** dokunduğu dosyalardır.)*
**COMMIT YAPILMADI.**

---

## 10. Kabul koşulları karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Düzenleme formunda kalem başına ürün seçici görünür, mevcut ürün ön seçili | ✅ | `DuzenlemeFormundaUrunSeciciGorunur` · `S3a…GOSTERIR` |
| Ürün değiştirilip kaydedilince kalem `id` DEĞİŞMEZ, `urunId` yeni ürünü gösterir | ✅ *(gövde düzeyinde)* | `UrunDegisiminceKalemKimligiKorunur` + mutasyon **M52-1**. **GET yanıtında** doğrulama canlı koşulamadı (§ 8) |
| Tahmini tutara değer girilip kaydedilince detayda aynı değer görünür | ✅ *(gövde düzeyinde)* | `TahminiTutarEkrandanKaydedilir`; detay tazelemesi `await Yukle()` ile. Canlı GET koşulamadı (§ 8) |
| Tahmini tutar girilip kur seçilmemişse alan düzeyinde hata, ağ isteği GİTMEZ | ✅ | `TahminiTutarKursuzIseIstekGitmez` + mutasyon **M52-2** |
| Kaydet sonrası kalem sayısı değişmez (çoğaltma yok) | ✅ | `UrunDegisiminceKalemKimligiKorunur` (`kalemler.GetArrayLength() == 1`) |
| Ayar serbest metin yerine sözlükten seçim | ✅ | `Secici` + `Ayarlar.Tumu`; `S3a…GOSTERIR` `data-deger="K22"` |
| `Sozlukler.cs`e dokunulmadı | ✅ | Dosya açılmadı; mevcut anahtarlar yetti |
| Mevcut element kimlikleri değişmedi | ✅ | `sipd-d-kalem-urun-{n}` · `sipd-d-kalem-ayar-{n}` aynı |
| P-51'den SONRA koştu | ✅ | P-51 kodu ağaçta bulundu, üzerine yazıldı |
| `denetim.sh` TEMİZ | ❌ | § 8 — P-52 katkısı **0**, ihlal başka şeritten |
| Tam süit 0 başarısız | ❌ | § 7 — P-52 katkısı **0** (tek kendi kırmızım düzeltildi) |
| Canlı doğrulama | ❌ | § 8 — eksik `sema` migration'ı kiracı açmayı engelliyor |

---

## 11. Açık kalanlar / orkestratöre

1. **ENGEL (P-52 dışı, öncelikli):** `Kuyumcu.Api/Data/Entities.cs:308` → `Kullanici.Sema` sütununun
   migration'ı **yok**. Etki: hem `kuyumcu_test_a`da 1664 test, hem `kuyumcu_net`te **her kiracı/kullanıcı
   oluşturma** kırık. Bu kapatılmadan hiçbir şerit canlı doğrulama yapamaz. Sahibi: "Görünüm" paketi şeridi.
2. **ENGEL (P-52 dışı):** `KullaniciGorunumServisi` bUnit servis kaydı eksik → 21 ekran testi kırmızı.
3. **ENGEL (P-52 dışı):** `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM` 47 → 48. Bugün dokunulan
   `Ayarlar.razor` / `UstBar.razor` / `KarZarar.razor` içinde `catch` bloğunda `X = null;`/`X = [];`
   yapan bir satır eklenmiş olmalı; sahibi şerit düşürmeli (tavan ARTIRILMAZ).
4. **P-52 için kalan tek iş:** engel 1 kapandıktan sonra § 8'deki 5 adımlık canlı doğrulama listesi
   (kiracı aç → 2 ürün + 1 cari + 1 TASLAK sipariş → kalem ürününü PATCH'le → GET'te `kalem.id` sabit
   ve `urunId` yeni mi → tahmini tutar + kur 200, kursuz 400 → kiracıyı sil) koşulmalı.
5. **`OlcumKapisiTestleri.cs:525` CS8604 uyarıları** (DALGA 7c) — 0 uyarı hedefini bozuyor, sınırım dışı.

**Kurucuya / mali müşavire not:** yok. P-52 mevzuat hükmü içermez, para/defter yazmaz, fiyat-teklif-tahsilat
metni üretmez (ücretsiz pilot kuralı korundu).

---

## 12. İKİNCİ TUR (24.09.2026, 00:00–00:20) — tamamlama ve paralel şerit kapısı

Bu turda **kod değişikliği yapılmadı.** `SiparisDetay.razor`, `SiparisEkranTestleri.cs`, `S3aEkranTestleri.cs`
1. turdaki hâliyle yerinde bulundu (`grep`: `sipd-d-tahmini`, `sipd-d-kalem-urun-{n}`, `KurVar`, kalem `id` satırı
mevcut). Tur, 1. turun koşulamayan maddelerini (canlı doğrulama, tam süit) ve kanıtların tazelenmesini kapsar.

### 12.1 Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (son koşum, `build4.txt`).
Ara koşumlarda görülen ve P-52'ye ait OLMAYAN iki anlık durum (Anayasa md.1 gereği kayıtta):
- 00:05 civarı `Pages/Cariler.razor` 12× `CS0103` (`TurDurumSutunu`, `DovizSutunu`, `M` yok) — başka şerit dosyayı
  yazarken; sonraki koşumda kendiliğinden kapandı. **Dokunmadım.**
- `MSB4018 ConvertDllsToWebCil` — eşzamanlı başka bir derlemeyle çakışma; tekrarında 0/0.
- İlk koşumda 1× `CS0162` `Pages/Raporlar.razor(776)` (PARA şeridi) — son koşumda yok.

### 12.2 Kendi testlerim (filtreli, `KUYUMCU_TEST_DB=kuyumcu_test_a`)
`--filter "FullyQualifiedName~SiparisEkranTestleri|FullyQualifiedName~S3aSiparisDuzenlemeEkranTestleri"` →
**Toplam 24 · Geçti 24 · Başarısız 0.** P-52'nin 4 testi: `DuzenlemeFormundaUrunSeciciGorunur` ·
`UrunDegisiminceKalemKimligiKorunur` · `TahminiTutarEkrandanKaydedilir` · `TahminiTutarKursuzIseIstekGitmez`.

### 12.3 Mutasyonlar (bu turda YENİDEN koşuldu)
Yedek: `scratchpad/mut-yedek/P-52/SiparisDetay.razor`; değişiklik `perl -0777 -pi` ile, eşleşme sayısı 1 değilse `die`.

| Kod | Değişiklik | Kırmızıya dönen test | Sonuç | Geri alma |
|---|---|---|---|---|
| M52-1 | `id = string.IsNullOrWhiteSpace(k.Id) ? null : k.Id,` → `id = (string?)null,` | **`SiparisEkranTestleri.UrunDegisiminceKalemKimligiKorunur`** | Toplam 21 · Geçti 20 · Başarısız 1 | `cmp` → birebir aynı |
| M52-2 | `if (dTahminiTutar is not null && !KurVar)` → `if (false)` | **`SiparisEkranTestleri.TahminiTutarKursuzIseIstekGitmez`** | Toplam 21 · Geçti 20 · Başarısız 1 | `cmp` → birebir aynı |

Son mutasyondan sonra temiz derleme 0/0 (§ 12.1).

### 12.4 Canlı doğrulama — 5197 (`--no-build`, Development)
Test kiracısı: `PAKETP52-TEST` (tenant `01M381RVY8E8BVEAA35B0TG0XD`), tek kullanımlık davet satırı `p52-davet-0002`
(jeton SHA-256 özeti tabloya, düz jeton yalnız scratchpad'de; ekrana basılmadı). Erişim jetonu çıktıya yazılmadı.
PATCH gövdesi **ekranın gönderdiği gövdeyle aynı kalıptadır** (`SiparisDetay.razor` `Kaydet`: `guncellendi` + `tahminiTutarTl`
+ `kurSnapshotId` + kalem `id`/`urunId`/`tanim`/`ayarId`/`gramAgirlik`/`adet`).

| # | Adım | Beklenen | Gerçek | Kanıt (yanıt okundu) |
|---|---|---|---|---|
| 1 | `POST /api/v1/auth/kayit` (davetli) | 201 | **201** | `{"tenantId":"01M381RV…","accessToken":"<gizli>",…}` |
| 2 | `POST /api/v1/urunler` ×2 (U1 `K22 10g`, U2 `K14 22,5g`) | 201 | **201 / 201** | `"aktif":true` |
| 3 | `POST /api/v1/cari` (MUSTERI) | 201 | **201** | bakiye 0 |
| 4 | `POST /api/v1/siparisler` (1 kalem, U1) | başarı | **200** | `"durum":"TASLAK"`, kalem `id=01M381SYQSDHS5Q5A2EGNG79N8`, `urunId=U1` |
| 5 | `GET /api/v1/kur/guncel` | 200 | **200** | kur `01M381STJAR6AAA9N1KP6QH3RD` (ALTINKAYNAK) |
| 6 | `PATCH /siparisler/{id}` — kalem `id` korunarak ürün U2 + `tahminiTutarTl=31500` + `kurSnapshotId` | 200 | **200** | — |
| 7 | `GET /siparisler/{id}` | kalem id aynı, urunId=U2, 1 kalem, tahmini 31500 | **200** · `kalemler=[{"id":"01M381SYQSDHS5Q5A2EGNG79N8","urunId":"01M381SPTZADTFPTVZYYGC9JPC","urunAd":"P52 Kunye","ayarId":"K14","gramAgirlik":22.500,…}]` · `tahminiTutarTl=31500.00` · `kurSnapshotId="01M381STJAR6AAA9N1KP6QH3RD"` | ✅ |
| 8 | `PATCH` `tahminiTutarTl=40000`, `kurSnapshotId=null` (ekranın ENGELLEDİĞİ istek, sunucu savunması) | 400 | **400** | `"Tahmini tutar verildiyse kur kaydı (kurSnapshotId) zorunludur — …"` |

**SQL kanıtı (`kuyumcu_net`):**
```
siparisler         : 01M381SYPYS9S7PJJMZS39TD25 | tahmini_tutar_tl 31500.00 | kur_snapshot_id 01M381STJAR6AAA9N1KP6QH3RD
siparis_kalemleri  : 01M381SYQSDHS5Q5A2EGNG79N8 | urun_id 01M381SPTZADTFPTVZYYGC9JPC (U2) | ayar_id K14 | gram 22.500
                     (tek satır → çoğaltma yok; 8. adımın 400'ü tutarı 31500'de bıraktı → reddedilen yazma iz bırakmadı)
```

**Not (ölçülen, P-52 dışı):** `POST /siparisler` **201 değil 200** döndü. P-52 bu ucu değiştirmedi; kayıt için not.
**Triyajın doğrulayamadığı madde — ölçüldü:** sipariş yanıtında `"subeId":"01M381RVY8GXX1Z4A3116CJS1A"` var →
`Siparis.SubeId` sütunu **MEVCUT**, migration gerekmez. (Fatura taslak DELETE, `/yedekler` gerekçe ucu, `/kur/guncel`
dağılımı, KurSnapshot kökeni P-52 kapsamı dışı — ölçülmedi, tahmin edilmedi.)
**Arka plan logu (P-52 dışı):** API açılışında `is_calismalari` benzersiz anahtar çakışması (`MASAK_SIB_SURESI-2026-09-24`)
— aynı veritabanında 5199 sunucusunun zamanlayıcısıyla eşzamanlı iş; P-52 akışını etkilemedi.

**Temizlik:** API PID 59310 `kill` → `lsof -nP -iTCP:5197 -sTCP:LISTEN` = **0 satır**. `tenant_id` sütunlu **70 tablodan**
kiracı satırları + `tenants` satırı + davet satırı silindi; sonrasında 70 tablonun hepsinde sayım **0**,
`kiraci_davetleri(p52-davet-0002)=0`, `kullanicilar(p52b@…)=0`, **`tenants` 10 → 10** (başlangıç = son).
`QA-TEST-` kiracılarına ve 5199'a dokunulmadı. Proje ağacına geçici dosya bırakılmadı.

### 12.5 Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_a`, `--no-build`, trx: `scratchpad/trx-pP-52b/_Mac_2026-09-24_00_14_55.trx`)
**Toplam 4587 · Geçti 4580 · Başarısız 7.** Siparis/Fatura testlerinden **hiçbiri** kırmızı değil.

| Kırmızı test | Hata | Sahibi (dosya) |
|---|---|---|
| `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` | `Islemler.razor 1437 satır (≤1400)` | `Pages/Islemler.razor` şeridi (00:15'te değişti) |
| `S22TezgahFisiTestleri.FARK_kapanmadan_KAYDET_kilitli_…_TEK_POSTta_gider` | bUnit `WaitFor` zaman aşımı | `Pages/Islemler.razor` şeridi |
| `AlisOdemeAraciEkranTestleri.SATIS_secicisinde_ALTIN_ve_VERESIYE_HALA_VARDIR` | seçicide `ALTIN` yok | Islemler/POS ödeme şeridi |
| `P10CariNotEkranTestleri.Yeni_cari_formunda_NOT_alani_…` | `ArayuzModuServisi` bUnit kaydı yok | `Pages/Cariler.razor` şeridi (00:11'de değişti) |
| `P10CariNotEkranTestleri.Cari_duzenlemesi_mevcut_NOTU_…` | aynı | `Pages/Cariler.razor` şeridi |
| `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` | 59 ≠ 60 | yeni `AltinBacagiAyarlari` sözleşme tipini ekleyen şerit |
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` | kilitsiz yeni tip `AltinBacagiAyarlari` | aynı |

**Devir notundaki 16 kırmızıyla karşılaştırma:** devir listesindekilerin hiçbiri bu koşumda kırmızı değil (başka şeritler
kapatmış). Yeni 7 kırmızının **hiçbiri P-52 dosyalarına bakmıyor** (SiparisDetay/SiparisEkranTestleri/S3aEkranTestleri) ve
P-52 bu turda kod değiştirmedi → **P-52'nin ürettiği yeni kırmızı = 0.**

### 12.6 `dotnet/denetim.sh` → `DENETİM BAŞARISIZ` (2 ihlal, ikisi de P-52 dışı)
```
✗ Pages: @onclick'siz ve disabled'sız <button>  74 (tavan 71 — ARTIŞ)
✗ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       48 > tavan 47
```
- **P-11 (48):** denetimin kendi `P11_DUSUREN` perl komutu `SiparisDetay.razor` üzerinde → **0**. 48 devir notundaki değerle aynı.
- **Ölü düğme (74 > 71):** denetimin `grep` ölçümüyle dağılım: Cariler 15, Uyum 12, Bildirimler 7, Vezne 6, Kasalar 6,
  Faturalar 5, … SiparisDetay **2**. Bu 2 satır (`sipd-d-kalem-sil-{n}`, `sipd-gecis-IPTAL`) çok satırlı etikettir;
  `disabled` ve `@onclick` bir alt satırdadır — **ölü düğme değildir** (grep yanlış pozitifi). Dosyanın içeriği 23.09 18:02'den
  beri değişmedi (oturum başı `mtime` 18:02; mutasyon yedeğiyle `cmp` birebir) ve 18:55 devir ölçümünde bu sayaç ihlalde
  değildi → **71→74 artışı 18:55 sonrasında değişen başka dosyalardan** (ör. Cariler.razor 00:11). P-52 katkısı **0**.
- P-52'nin sayaçları ✓: `ModalPencere kullanımı 55 (tavan 55)` — yeni düzenleme modalı yok; `DIS_HATADA_VERI_DUSUREN_YOL 0`.

### 12.7 Paralel şerit kapısı (7 madde)
| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | build 0 hata | ✅ | 0 Hata · 0 Uyarı |
| 2 | kendi testlerim 0 başarısız | ✅ | 24/24 (§ 12.2) |
| 3 | kendi sayaçlarım ✓ | ✅ | SiparisDetay P-11 katkısı 0; ModalPencere 55/55; ölü düğme katkısı 0 (§ 12.6) |
| 4 | mutasyon kanıtı | ✅ | M52-1, M52-2 (§ 12.3) |
| 5 | canlı doğrulama | ✅ | § 12.4, 8/8 adım beklenen kodla |
| 6 | test kiracısı silindi | ✅ | 70 tablo 0 satır, tenants 10→10 |
| 7 | yeni kırmızı üretilmedi | ✅ | § 12.5 — fark 0 |

### 12.8 Kabul koşulları (güncel — § 10'un yerini alır)
| Koşul | Durum | Kanıt |
|---|---|---|
| Düzenleme formunda kalem başına ürün seçici, mevcut ürün ön seçili | ✅ | `DuzenlemeFormundaUrunSeciciGorunur` |
| Ürün değiştirilip kaydedilince GET'te kalem id DEĞİŞMEZ, urunId yeni ürün | ✅ | bUnit `UrunDegisiminceKalemKimligiKorunur` + M52-1 + **canlı adım 7 + SQL** |
| Tahmini tutar girilip kaydedilince detayda aynı değer | ✅ | `TahminiTutarEkrandanKaydedilir` + **canlı adım 7 (31500.00) + SQL** |
| Tutar var, kur yoksa alan düzeyinde hata, istek GİTMEZ | ✅ | `TahminiTutarKursuzIseIstekGitmez` + M52-2; sunucu savunması canlı adım 8 → 400 |
| Kaydet sonrası kalem sayısı değişmez | ✅ | bUnit (`GetArrayLength()==1`) + SQL tek satır |
| Ayar sözlükten seçim | ✅ | `Secici` + `Ayarlar.Tumu` |
| `Sozlukler.cs`e dokunulmadı | ✅ | açılmadı |

### 12.9 Açık kalanlar (sahipleriyle — P-52'nin değil)
1. 7 tam süit kırmızısı — § 12.5 tablosundaki sahipler (Islemler/POS şeridi, Cariler şeridi, `AltinBacagiAyarlari` sözleşme şeridi).
2. `denetim.sh` ölü düğme sayacı 74 > 71 — 18:55 sonrası değişen sayfaların şeritleri; ayrıca sayaç çok satırlı `<button>`
   etiketlerini yanlış sayıyor (SiparisDetay'daki 2 satır buna örnek) — sayacın sahibi DALGA 7c'ye not.
3. `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM` 48 > 47 — devir notundaki sahibi P-42/P-46.
4. `POST /siparisler` 201 yerine 200 dönüyor (ölçüm; P-52 kapsamı dışı, Sipariş şeridinin sonraki paketine not).

**Kurucuya / mali müşavire not:** yok — P-52 mevzuat hükmü içermez.
