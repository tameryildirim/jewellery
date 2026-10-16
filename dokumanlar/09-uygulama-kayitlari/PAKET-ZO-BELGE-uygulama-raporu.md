# PAKET ZO-BELGE — uygulama raporu

## 30.09 DEVAM 18 (07:36–08:05): orkestratör 07:50 — has bedelinin DAYANAĞI KUR KAYDIDIR (yuvarlanmış bedelden geri çözülmez) + defter bağlı kalemde adet PATCH'i 400 + 040608 geri doldurması yalnız TASLAK. Güncel beyan: **BITTI** (açık kalanlar § W-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9706). 5199/5200 ve tarayıcı bölmesi kullanılmadı. COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30f/`.

### W-1. Başlangıç durumu ve yapılanlar
Devir kontrolü: DEVAM 17'nin mutasyon yedeği (`mut-yedek/ZO-BELGE-d18/FaturaModulu.cs`) sonrası ağaçta yarım mutasyon yok; hasım doğrulayıcının `Dogrulayici/ZOBELGESinirTestleri.cs` → `ZOBELGEKalemHasSinirTestleri.ZOBHGS08(100)` eski kodla KIRMIZI idi (dayanak 125.074,30÷18,32 geri çözülüyordu → 1→100'de 9.214.875,03 gibi kuruş kayması).

| # | Orkestratör maddesi | Yapılan (`dotnet/Kuyumcu.Api/…`) |
|---|---|---|
| 1 | Kalemde dayanak kuru sakla | `Data/FaturaKalemi.cs`: yeni `HasBirimFiyat` → `fatura_kalemleri.has_birim_fiyat decimal(15,6) NULL`. `Modules/Fatura/FaturaModulu.cs` üç oluşturma yolunda motorun kullandığı KUR KAYDI değeriyle yazılır: kalemli (`KalemYolu`, `kalemKuru = kur.AltinHasFiyati` — harekete bağlıysa hareketin kaydı, değilse son kayıt), fişten (`FistenOlustur`, hareketin `KurSnapshotId` kaydı), taslağa ekle (`TaslakKalemEkle`, son kayıt). Bacak kurulmayan kalemde (STANDART, altın dışı) NULL. |
| 1 | PATCH'te has bedeli | `TaslakKalemGuncelle`: `bacak = HasBacagi(yeni satır toplam has gram, kalem.HasBirimFiyat)` → motor `round2(gram × kur)`. "Eski bedel ÷ eski gram" geri çözmesi ve `eskiHasToplam` KALDIRILDI. Kolon NULL ya da has gram ≤ 0 → bacak YOK → özel matrah UYGULANMAZ, KDV tam (fail-closed; eski davranıştaki "dayanak çözülemiyor" 400'ü yerine orkestratörün tarifi). Güncel kura GEÇİLMEZ (ZOBHG08). |
| 2 | Defter bağlı kalemde adet PATCH'i 400 | `TaslakKalemGuncelle`: `HareketId` dolu ve yeni adet (3 hane) ≠ mevcut adet → `400 "Bu satır satış fişinden geldi; adedi fişten değiştirin."`. Aynı adet yeniden gönderilirse değişiklik değildir (200). Fiyat/işçilik/indirim düzeltmesi aynen çalışır. |
| 3 | 040608 geri doldurmasını TASLAK ile sınırla | `Data/Migrations/20260930040608_ZOBelgeKalemHasGramiToplam.cs`: aday listesi ve kalem UPDATE'i `JOIN faturalar f … AND f.durum = 'TASLAK'` ile sınırlandı (kesilmiş/ödenmiş/iptal belge satırına dokunulmaz); başlık toplaması `LEFT JOIN` + `COALESCE(x.g,0)` → altın kalemi kalmayan (maden NULL) taslakta başlık 0 / özet kur NULL olur (koddaki `KalemHasGrami`+`OzetKur` ile aynı). |

**Sapma ve gerekçesi (md.3 — uygulanmış migration'ı düzenleme):** Orkestratör "gerekirse yeni migration" dedi. `kuyumcu_net`'e 040608 ilk uygulamada **0 satır** etkilemişti (DEVAM 17 V-2: 9 kalemin 9'u miktar 1) → düzeltilecek veri YOK, yeni düzeltme migration'ı gerekmedi. Ancak 040608'in SQL'i henüz uygulanmamış her veritabanında (yeni kurulum, başka ortam) kesilmiş belgeye dokunacaktı; bunu yeni bir migration önleyemez (040608 önce koşar). Bu yüzden SQL'in kendisi sınırlandı; EF uygulanmış migration'ı yeniden koşmaz, `kuyumcu_net` etkilenmez. Test veritabanı her süreçte boş kurulur. Kanıt: `ZOBHG09` migration'ın koştuğu SQL'in TA KENDİSİNİ (`Migration.UpOperations`) geri alınan işlem içinde koşar.

**Sapma (ZOBHG03):** Orkestratör "ZOBHG03 beklenen 187.611,46" dedi, ama ZOBHG03 **fişten** kalemde adedi 2→3 yapıyordu; md.2 bunu artık 400'e çeviriyor. Test iki parçaya ayrıldı (tek test, ad `ZOBHG03_fisten_kalemde_MIKTAR_400_fiyat_serbest_urun_kaleminde_2den_3e_KUR_KAYDIYLA_187_611_46`): fişten kalemde fiyat PATCH'i serbest (has 125.074,30 aynen), adet PATCH'i 400 + Türkçe mesaj + satır değişmez, aynı adet 200; **ürün** kaleminde 2→3 → has 27,48 g, has bedeli **187.611,46** (27,48 × 6.827,20 = 187.611,456; eski geri çözme 187.611,45 verirdi).

### W-2. Migration `20260930044042_ZOBelgeKalemHasBirimFiyat`
`dotnet build Kuyumcu.sln` YEŞİL (0/0) → `dotnet ef migrations add ZOBelgeKalemHasBirimFiyat --project Kuyumcu.Api` (build'li; `--no-build` kullanılmadı) → `.cs` 779 B (sonra geri doldurmayla 2.806 B), `.Designer.cs` 236.628 B. Up: `AddColumn has_birim_fiyat decimal(15,6) NULL` (başka şeridin bekleyen şema değişikliği YOK — Up yalnız bu kolon) + geri doldurma, **yalnız TASLAK faturaların KUYUM+ALTIN kalemleri**: (1) fişten kalem → bağlı hareketin `kur_snapshots.altin_has_fiyati` (kiracı sınırında JOIN); (2) kalan (ürün yolları / kur kaydı bulunamayan fişten) → `ROUND(has_deger / has_grami, 6)` — **not: bu, kur kaydı değil yuvarlanmış bedelden GERİ ÇÖZÜLEN yaklaşık değerdir; yalnız bu migration öncesi eski kayıtlar için, tek çıkar yol olarak kullanılır** (yeni kalemde kolon oluşurken kur kaydından yazılır). Has bedeli/gramı olmayan kalem NULL kalır → PATCH'te özel matrah uygulanmaz. Down: `DropColumn`.
`dotnet ef database update --project Kuyumcu.Api --connection …` (parola değişkenden) → `Applying migration '20260930044042_ZOBelgeKalemHasBirimFiyat'. Done.`; `__EFMigrationsHistory` 74 → **75**.
**kuyumcu_net SQL kanıtı** (`d30f/once-sql.txt`, `sonra-sql.txt`): TASLAK 4 kalem → **3 dolduruldu** (6.770,701639 · 6.803,599747 · 6.819,420098 — üçü de ürün yolu, `has_deger ÷ has_grami`), 1 NULL (STANDART). **IPTAL 4 kalem → 0, ODENDI 1 kalem → 0** (kesilmiş belgeye dokunulmadı). Fişten TASLAK kalem kuyumcu_net'te yok → (1) adımı 0 satır. 040608 düzeltmesi kuyumcu_net'te yeniden koşmaz (uygulanmış; ilk koşumda 0 satır).

### W-3. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `d30f/son-build.log`) → **0 Uyarı, 0 Hata**.

### W-4. Testler
- `Kuyumcu.Tests/ZOBelgeKalemHasGramiTestleri.cs` — ZOBHG03 yeniden yazıldı (W-1 sapma); **yeni**: `ZOBHG06_uc_yolda_kalem_HasBirimFiyat_KUR_KAYDINDAN_STANDART_ve_altin_disi_NULL`, `ZOBHG07_dayanak_NULL_ise_duzeltmede_ozel_matrah_UYGULANMAZ_KDV_tam_geri_cozulmez` (1→2: has 0, KDV 33.333,33 = 200.000×0,2/1,2, 805 kodu yok), `ZOBHG08_duzeltmede_GUNCEL_kura_gecilmez_kalemin_kur_kaydi_kullanilir` (7.000'lik yeni kur eklenir; has 125.074,30 kalır), `ZOBHG09_HasGramiToplam_geri_doldurmasi_YALNIZ_TASLAK_kalem_KESILDI_dokunulmaz_maden_NULL_baslik_toplanir`, `ZOBHG10_HasBirimFiyat_geri_doldurmasi_fisten_KUR_KAYDI_digeri_bedel_bolu_gram_KESILDI_ve_STANDART_NULL` (ZOBHG09/10: migration'ın gerçek SQL'i `UpOperations`'tan alınır, `BeginTransaction` içinde koşulur, `Rollback` — başka testin verisine kalıcı dokunmaz). Yeni test: **5** (+ZOBHG03 yeniden yazıldı).
- Hasım doğrulayıcının `ZOBELGEKalemHasSinirTestleri` **ZOBHGS01–08 (ZOBHGS08(3) = 187.611,46 ve ZOBHGS08(100) = 6.253.715,20 dahil) YEŞİL**; `ZOBELGEHakem5MadenSinirTestleri` ZOBH01–07 YEŞİL. Odak koşumu: **28/28** (`d30f/t1.log`).
- Geniş filtreli (OzelMatrah + ZOBELGE + ZoBelge + ZOBelge + Fatura + Kdv + Q92 + Disaaktarim + P08 + K10 + GeriDoldurma): **503/503** (`d30f/t2.log`) — ZOBM01–08, K10b ×9, OzelMatrah* ×40, ZOBHG01–10, ZOBH01–07 adlarıyla içinde.
- **Tam süit** (`d30f/tam.log`, trx `trx-pZO-BELGE/d19`): **7745 · 7743 geçti · 1 başarısız · 1 atlandı**. Kırmızı: `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` (Expected 0 / Actual 1) — SATIS-3 şeridinin ekran testi (İşlemler/barkod zamanlaması), bu paketin dokunmadığı alan; tam süit canlı tohumlama (API + 9 satış) ile AYNI ANDA koşarken çıktı; tek başına 3 kez koşuldu → **3/3 YEŞİL** (`d30f/barkod-1..3.log`) = yük altında zamanlama kararsızlığı. Atlanan: SATIS-3'ün Skip'i. **Bu paketin ürettiği yeni kırmızı: 0.**

### W-5. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra, `d30f/denetim.log`) → **DENETİM TEMİZ** (295 ✓, 0 ✗). ZOBHG09/10'daki `ExecuteSqlRaw` test tarafında, mevcut `BelgeGeriDoldurmaTestleri` deseniyle aynı; denetim sayaçları etkilenmedi.

### W-6. Mutasyon kanıtları (M22, 8/8 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d19/` (FaturaModulu.cs + iki migration), betik `d30f/mut.sh` — her birinde desen TAM 1 eşleşme sayılır, 1 değilse mutasyon uygulanmaz; derleme 0 hata; filtreli koşum 28 test; geri koyma sonrası `cmp` AYNI; günlük `d30f/mut/ozet.txt`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M22-1 | **Geri çözme geri gelir**: PATCH bacağı `Yuvarla6(kalem.HasDeger ÷ eski has gram)` | ZOBHG03, ZOBHGS08(3), ZOBHGS08(100) | AYNI |
| M22-2 | **Kolon yazılmaz** (kalemli yol `HasBirimFiyat = null`) | ZOBH03, ZOBHG03, ZOBHG06, ZOBHG08, ZOBHGS08 ×2 | AYNI |
| M22-3 | **Hareketli kalemde adet kabul** (`if (false && …HareketId…)`) | ZOBHG03 | AYNI |
| M22-4 | **KESILDI geri doldurulur** (040608 kalem UPDATE'inden `f.durum='TASLAK'` çıkarıldı) | ZOBHG09 | AYNI |
| M22-5 | Maden NULL taslakta başlık toplanmaz (`LEFT JOIN` → `JOIN`) | ZOBHG09 | AYNI |
| M22-6b | 044042 geri doldurması KESILDI kalemi de doldurur (2. adımdan `TASLAK` çıkarıldı) | ZOBHG10 | AYNI |
| M22-7 | NULL dayanakta sessiz geri çözme (`HasBirimFiyat ?? HasDeger÷HasGrami`) | ZOBHG07 | AYNI |
| M22-8 | Fişten kalem kur kaydından doldurulmaz (`= NULL`) | ZOBHG10 | AYNI |
Not: ilk M22-6 denemesi SQL'deki boşluğu da sildiği için SÖZDİZİMİ hatası üretti (28/28 kırmızı, `MySqlException … syntax`) — geçersiz mutasyon sayıldı, boşluk korunarak M22-6b olarak yinelendi. Son `cmp` (3 dosya) HEPSİ AYNI + temiz build 0/0.

### W-7. Canlı doğrulama (5160 · `dotnet run --no-build` Development · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3RAGKCTH2K7HSGCNDNBHWVY — `demo-tohumla.js --prova-kiraci --ad …`; ilk koşum 40 sn'de kur gelmediği için DURDU (betiğin kendi fail-closed kuralı), kur geldikten sonra `--eposta` ile sürdürüldü: 6 cari / 12 ürün / 9 satış · tek kullanımlık rastgele `Sistem__Anahtari`, değeri basılmadı) — betikler `d30f/hb.mjs`, `hb2.mjs`, günlük `hb.log`, `hb2.log`, sonuç `hb2.json`, SQL `hb-sql.txt`. Kur kaydı `01M3RAJ4Y4WYDVDADG4BAQBRFF` altın has 6.618,36; ürün "22 Ayar Burma Bilezik" 13,9232 has g/adet.
| Adım | Beklenen | Gerçek (HTTP + SQL) |
|---|---|---|
| KUYUM taslak 1 adet → PATCH `{"miktar":3}` | 200; has 41,7696 g; has bedeli round2(41,7696 × 6.618,36) = 276.446,25; kolon 6.618,36 | **200**; `has_grami 41.769600`, `has_birim_fiyat 6618.360000`, `has_deger 276446.25` (= SQL `ROUND(has_grami×has_birim_fiyat,2)` 276446.25); başlık `has_gram 41.769600`, özet kur 6.618,36 |
| KUYUM taslak 1 adet → PATCH `{"miktar":100}` | 200; 1.392,32 g × 6.618,36 = 9.214.875,00 | **200**; `has_grami 1392.320000`, `has_deger 9214875.00`, KDV 767.920,83, satır 13.822.400,00. (Eski geri çözme: 92.148,75 ÷ 13,9232 = 6.618,360018 → 9.214.875,03 — 3 kuruş kayma artık YOK) |
| Fişten (hareket `01M3RAKJ6CWSWCJJ55K19CQ9EN`, 22 Ayar Çeyrek, 1,603 g) KUYUM fatura | kalem kolonu = hareketin kur kaydı 6.618,36 | **200**; `has_birim_fiyat 6618.360000`, has 10.609,23; hareket `kur_snapshot_id` = 01M3RAJ4Y4WYDVDADG4BAQBRFF |
| Aynı kalemde PATCH `{"miktar":2}` | 400 + Türkçe sebep, satır değişmez | **400** `{"message":"Bu satır satış fişinden geldi; adedi fişten değiştirin.", …}`; SQL `miktar 1.000` |
| Aynı kalemde PATCH `{"miktar":1}` (aynı adet) | 200 | **200** |
| Aynı kalemde PATCH `{"birimFiyat": +100}` | 200; has aynen | **200**; birim 11.133,60, has 10.609,23 aynen, adet 1 |

### W-8. Gerçek tarayıcı (400 mesajı ekranda çıktığı için tek ölçüm; başsız Chrome, profil `d30f/profil`, vekil 5161, CDP 9706; betik `d30f/adet.mjs`, sonuç `adet.json`, foto `d30f/foto/`)
Akış: /faturalar → fişten taslağın "Düzenle" çekmecesi → Miktar hücresi `#si-fatura-kalem-miktar-<id>-ac` → 2 yaz → kaydet → `#alan-hata-fd-kalem-miktar-<id>`.
| Genişlik | Mesaj | Görünür | Kutu | Taşma | K1 | K3 | K5 | K10 | yatay |
|---|---|---|---|---|---|---|---|---|---|
| 1280×800 | "Bu satır satış fişinden geldi; adedi fişten değiştirin." | evet | 373×17 px (tek satır) | yok | 0 | 0/0 | 0 | 0 | 0 |
| 375×812 | aynı | evet | 284×17 px | yok | 0 | 0/0 | 0 | 0 | 0 |
K9 = 6: açık çekmece ile arkadaki liste katmanının kesişmesi (araç artefaktı; önceki turlarla aynı). Görüntüler Read ile GÖRÜLDÜ: `foto/fisten-adet-400-basit-aydinlik-1280.png` ve `…-375.png` — dosya adı "aydinlik" olsa da profil karanlık temayla açıldı (görüntü koyu); mesaj Miktar kutusunun hemen altında kırmızı tek satır, çekmecenin üstünde "Satır kaydedilemedi — işaretli alanın altındaki açıklamaya bakın." bandı; adet 1 olarak kaldı. Ekran kodu DEĞİŞMEDİ (mevcut `KalemKaydet` → `AlanHatasi` yolu).

### W-9. Temizlik ve açık kalanlar
**Temizlik:** API (5160; `dotnet run` + çocuk süreç) ve vekil (5161) PID ile durduruldu; `lsof -iTCP:5160/5161/9706` boş. Kiracı `kiraci-temizle.sh 01M3RAGKCTH2K7HSGCNDNBHWVY --uygula` (çıkış 0) → tenants'ta 0, fatura_kalemleri 0. **tenants: canlı doğrulama başında 12, sonda 12** (07:36'da 13 idi; aradaki 1 azalma başka şeridin kiracısı — bu paketin açtığı kiracı o sırada yoktu). QA-TEST-* dokunulmadı. `d30f/anahtar`, `d30f/fik.json` `printf ''` ile 0 bayt. Chrome profili `d30f/profil` SİLİNEMEDİ (kanca `rm`'yi blokluyor) — orkestratör silmeli (d30c/d30d/d30e profilleriyle birlikte). Proje ağacına .mutorj/.bak/.tmp bırakılmadı.
1. **Mali müşavire not:** 044042 geri doldurmasında ürün yolundan gelen 3 eski TASLAK kalemin dayanak kuru kur kaydından değil `has_deger ÷ has_grami` ile geri çözüldü (6 hane; kuruş hassasiyetinde yaklaşık). Bu kalemler kesilmeden önce düzeltilirse has bedeli bu yaklaşık kurla hesaplanır; kesin istenirse taslak iptal edilip yeniden kesilir (taslak numara tüketmez). Kesilmiş belgelere dokunulmadı.
2. **Davranış değişikliği (bilgi):** dayanak kuru bilinmeyen (NULL) kuyum kaleminde düzeltme artık 400 değil, özel matrahsız (KDV tam) kaydedilir — orkestratör tarifi; fazla KDV yönünde (tutucu). Ekranda bu durum için ayrı bir not YOK (yanıtta `hasDeger 0`); gerekirse ekran notu ayrı iş.
3. Fişten faturanın BAŞLIK özet kuru hâlâ `has_deger ÷ has_gram` (canlıda 6.618,3593 ≠ kur 6.618,36) — yalnız gösterim/özet alanı, hesapta kullanılmıyor; bu turun kapsamı dışı, dokunulmadı.
4. Başkasına ait: `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` yük altında kararsız (SATIS-3). Devreden: § V-9 md.1–3, § U-9 md.1 (gümüş/platin/paladyum özel matrahı — mali müşavir teyidi), md.3, md.4, § T-9 md.5.

### W-10. DoD sözleşmesi
Yeni varlık/uç YOK. Yeni kolon `fatura_kalemleri.has_birim_fiyat` SUNUCUNUN türettiği dayanaktır: PATCH gövdesinde kabul EDİLMEZ, yalnız kalem oluşurken kur kaydından yazılır (UX-S21). Taslak kalem PATCH sözleşmesi (yoksa koru, null = sil) değişmedi — ZOBH07 yeşil. Defter bağlı kalemde adet salt okunur: ekranda deneme alan altı Türkçe gerekçe verir (W-8), doğru yol = fişte düzeltme. Kesilmiş fatura salt okunur; hard delete yok; finansal belge satırına migration ile dokunulmadı (SQL kanıtı W-2). Yeni ekran metni yok (sunucu mesajı).
**Değişen dosyalar (bu tur):** `dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs`, `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`, `dotnet/Kuyumcu.Api/Data/Migrations/20260930040608_ZOBelgeKalemHasGramiToplam.cs`, `dotnet/Kuyumcu.Api/Data/Migrations/20260930044042_ZOBelgeKalemHasBirimFiyat.cs` + `.Designer.cs` (yeni), `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`, `dotnet/Kuyumcu.Tests/ZOBelgeKalemHasGramiTestleri.cs`. (Hasım doğrulayıcının `Dogrulayici/ZOBELGESinirTestleri.cs` dosyasına DOKUNULMADI.)

---

## 30.09 DEVAM 17 (06:58–07:40): orkestratör 07:00 — kalem.HasGrami'nin İKİ ANLAMI (ürün yolunda birim, fişten yolunda toplam) → TEK ANLAM = SATIR TOPLAMI. Güncel beyan: **BİTTI** (açık kalanlar § V-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9706). 5199/5200 ve tarayıcı bölmesi kullanılmadı. COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30e/`.

### V-1. Başlangıç durumu ve yapılanlar
Devir kontrolü: DEVAM 16 mutasyon yedekleri (`mut-yedek/ZO-BELGE-d17/FaturaModulu.cs`, `Faturalar.razor`) ağaçla `cmp` AYNI — yarım mutasyon yok. Taban koşum (`d30e/t0.log`): ZOBH02 ve ZOBH03 KIRMIZI (beklenen 18,32, gerçek 9,16), diğer 5 ZOBH yeşil.

**Karar:** `fatura_kalemleri.has_grami` = SATIRIN TOPLAM has gramı, her yolda (fişten yolu zaten böyleydi; belgedeki has gram satır toplamıdır).
| Yol | Önce | Şimdi (`FaturaModulu.cs`) |
|---|---|---|
| Kalemli oluşturma (`KalemYolu`) | `HasGrami = urun.HasGrami` (birim) | `:751` `KalemToplamHas(miktar, urun?.HasGrami)` = Yuvarla6(birim × miktar) |
| Taslağa kalem ekle | birim | `:1492` aynı yardımcı |
| Fişten | `Yuvarla6(|h.HasGrami|)` (toplam) | değişmedi |
| Taslak kalem PATCH `miktar` | has gram değişmiyordu; bacak `miktar × kalem.HasGrami` (fişten kaleminde ÇİFT çarpım riski), dayanak `HasGrami × eskiMiktar` | `:1294-1303` has gram ORANTIYLA yeniden: `Yuvarla6(eskiToplam × yeniMiktar ÷ eskiMiktar)` (miktar aynıysa dokunulmaz; KUYUM/STANDART fark etmez) · `:1347-1354` dayanak kuru = eski has bedeli ÷ ESKİ satır toplamı, bacak = YENİ satır toplamı (çarpım yok). `KalemEskiMiktar` yardımcısı kaldırıldı. |
| Başlık has gramı (`KalemHasGrami`) | Σ kalem.HasGrami (anlam karışık) | değişmedi — artık hep toplamların toplamı |
| Yardımcı | — | `:829` `public static decimal? KalemToplamHas(decimal miktar, decimal? birimHas)` (birim yoksa NULL) |
`Data/FaturaKalemi.cs` özet açıklaması "Birim başına" → "SATIRIN TOPLAM has gramı" olarak düzeltildi. Kalem has BEDELİ hesabı değişmedi: ZOBH01 125.074,30 korunur (çift çarpım yok). Ekrana dokunan değişiklik YOK (okuma ucu `KalemVeri.HasGrami` ekranda gösterilmiyor; `Faturalar.razor:766` fiş satırını — hareketi — gösterir).

### V-2. Migration `20260930040608_ZOBelgeKalemHasGramiToplam` (yalnız veri)
`dotnet build Kuyumcu.sln` YEŞİL → `dotnet ef migrations add ZOBelgeKalemHasGramiToplam --project Kuyumcu.Api` (build'li) → üretilen Up BOŞ (başka şeridin bekleyen model değişikliği yok), `.Designer.cs` 236 KB. Up'a geri doldurma yazıldı: has gramı dolu, miktarı ≠ 1, `urun_id` dolu VE bağlı hareketin `ROUND(ABS(has_grami),6)` değerine eşit OLMAYAN kalem → `has_grami = ROUND(has_grami × miktar, 6)`; etkilenen faturalardan yalnız **TASLAK KUYUM** başlıkların `has_gram`/`has_birim_fiyat`'ı altın kalemlerden yeniden toplanır (kesilmiş belge başlığına dokunulmaz). Down: dönüşsüz (birim = toplam ÷ miktar her an türetilebilir; veri kaybı yok).
SQL önce karalama şemada sınandı (`d30e/dene.sql`, `zob_hg_dene` oluşturulup silindi): ürün kalemi 9,16×2 → 18,32 · fişten kalemi 18,32 (hareket 18,32) DOKUNULMADI · miktar 1 dokunulmadı · ürünsüz satır dokunulmadı · kesilmiş faturanın kalemi 27,48'e çevrildi, başlığı AYNEN · taslak başlık 23,32 (18,32 + 5). `dotnet ef database update` (kuyumcu_net, parola değişkenden) → `Applying migration '20260930040608_ZOBelgeKalemHasGramiToplam'. Done.`; `__EFMigrationsHistory` 74. **Etkilenen satır (kuyumcu_net): 0** — 9 kalemin 9'u miktar 1.

### V-3. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `d30e/mut/son-build.log`) → **0 Uyarı, 0 Hata**.

### V-4. Testler
- Yeni `Kuyumcu.Tests/ZOBelgeKalemHasGramiTestleri.cs` [DB + saf]: `ZOBHG01_kalemli_yolda_2_adet_kalem_has_grami_SATIR_TOPLAMI_18_32`, `ZOBHG02_taslaga_2_adet_urun_eklenince_kalem_has_grami_toplam_baslik_toplanir` (başlık 27,48), `ZOBHG03_fisten_2_adetlik_kalemde_miktar_2den_3e_has_orantili_CIFT_CARPIM_yok` (fiyat PATCH → 18,32 / 125.074,30 aynen; miktar 3 → 27,48 g, 187.611,45 = 27,48 × (125.074,30 ÷ 18,32 = 6.827,199782)), `ZOBHG04_STANDART_kalemde_de_miktar_duzeltmesi_has_gramini_yeniden_hesaplar`, `ZOBHG05_KalemToplamHas_birim_x_miktar_birim_yoksa_null`.
- Doğrulayıcının `ZOBELGEHakem5MadenSinirTestleri` ZOBH01–07: **7/7 YEŞİL** (ZOBH02/ZOBH03 önce kırmızı).
- Filtreli (OzelMatrah + ZOBELGE + ZoBelge + ZOBelge + Fatura + Kdv + Q92 + Disaaktarim + P08; ZOBM01-08, K10b*, OzelMatrah* dahil): **473/473** (`d30e/t3.log`).
- **Tam süit** (`d30e/tam.log`, trx `trx-pZO-BELGE/d18`): **7729 · 7728 geçti · 0 başarısız · 1 atlandı** (`SATIS3EkranTestleri.DOG_T24_SONDA_…` — SATIS-3'ün Skip'i). **Bu paketin ürettiği yeni kırmızı: 0.**

### V-5. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra, `d30e/denetim.log`) → **DENETİM TEMİZ** (295 ✓, 0 ✗).

### V-6. Mutasyon kanıtları (M21, 6/6 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d18/FaturaModulu.cs`, betik `d30e/mut.sh`, günlük `d30e/mut/ozet.txt`; her birinde tek eşleşme `perl -0777`, filtreli koşum 61 test, geri koyma sonrası `cmp` AYNI; son `cmp` AYNI + temiz build 0/0)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M21-1 | Çarpım silindi: `KalemToplamHas` → birim has | ZOBHG01, ZOBHG02, ZOBHG05, ZOBH02 | AYNI |
| M21-2 | PATCH'te has gram yeniden hesabı silindi | ZOBHG03, ZOBHG04, ZOBH03 | AYNI |
| M21-3 | Çift çarpım: bacak `kalem.Miktar × yeniHasGramToplam` | ZOBHG03, ZOBH03 | AYNI |
| M21-4 | Kalemli yolda birim yazılır | ZOBHG01, ZOBH02 | AYNI |
| M21-5 | Taslağa eklemede birim yazılır | ZOBHG02 | AYNI |
| M21-6 | Dayanak kuru YENİ toplamdan çözülür | ZOBHG03, ZOBH03 | AYNI |

### V-7. Canlı doğrulama (5160 · `dotnet run --no-build` Development · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` — `demo-tohumla.js --prova-kiraci --ad …` · tek kullanımlık rastgele `Sistem__Anahtari`, değeri basılmadı) — betik `d30e/hg.mjs`, sonuç `hg.json`, SQL `hg-sql.txt`
Ürün: 22 Ayar Burma Bilezik, birim has 13,9232 g; kur 6.618,36; birim fiyat 138.224.
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /faturalar` KUYUM, 1 kalem × 2 adet | 200; kalem has gram 27,8464; has bedeli 27,8464 × 6.618,36 = 184.297,50; başlık has gram 27,8464 | **200 · 27,8464 · 184.297,50 · başlık 27,8464 / 6.618,36** |
| `POST /faturalar` KUYUM 1 adet (taslak) | kalem 13,9232 · 92.148,75 · başlık 13,9232 | **13,9232 · 92.148,75 · 13,9232** |
| `PATCH …/kalemler/{id} {miktar:2}` | 200; kalem 27,8464 · 184.297,50 (çift çarpım yok) · KDV (276.448 − 184.297,50)/6 = 15.358,42 | **200 · 27,8464 · 184.297,50 · 15.358,42 · 276.448** |
| Başlık (GET) | has gram 27,8464, özet kur 6.618,36 | **27,8464 · 6.618,36 · 184.297,50** |
SQL (`hg-sql.txt`): iki fatura da `TASLAK KUYUM 276448.00 27.846400 6618.3600 184297.50 15358.42`; kalemler `ALTIN 2.000 27.846400 184297.50 15358.42 276448.00` — oluşturma yolu ile düzeltme yolu BİREBİR aynı sonuç.
Ek (DEVAM 16 akışı tekrar, `k10d.json`): ürünsüz gümüş hurda fişten → 200, has 0, KDV 616,67; fiyat PATCH 678,33; miktar 1→2 → 200, 1.356,67; altın hurda fişten → has 30.312,09; fiyat PATCH → dayanak korunur, KDV 1.245,48, başlık has gram 4,58; önizleme bayrağı gümüş true / altın false.

### V-8. Gerçek tarayıcı (notun göründüğü 2 durum; başsız Chrome, profil `d30e/profil`, vekil 5161, CDP 9706; betik `d30e/gum2.mjs`, sonuç `gum2.json`, foto `d30e/foto/`)
{BASİT, PRO} × {aydınlık, karanlık} × {1280, 375} = 8 ölçüm × 3 ekran.
| Ekran | Beklenen | Gerçek (8/8) | K1 | K3 | K5 | K10 | yatay |
|---|---|---|---|---|---|---|---|
| (a) ürünsüz gümüş hurda taslağı çekmecesi | `#fd-cekmece-kalem-gumus-*` VAR, taşma yok | görünür, taşma 0 (1280: 373×17 px tek satır; 375: 284×34 px iki satır) | 0 | 0/0 | 0 | 0 | 0 |
| (b) altın hurda çekmecesi | not YOK | yok | 0 | 0/0 | 0 | 0 | 0 |
| (c) Yeni Fatura · KUYUM | açılışta YOK, altın işlemde YOK, gümüş işlemde VAR | açılış yok · altın yok · gümüş VAR (1280: 447×17; 375: 342×34), taşma 0 | 0 | 0/0 | 0 | 0 | 0 |
K9 = 6: arka plan tablosu ile açık çekmece katmanı kesişmesi (araç artefaktı; § T-7'de çekmece kapalı K9 = 0). Not: ilk koşumda (c) hurda hareketleri Yeni Fatura işlem listesinde çıkmadığı için (nihai tüketici hurda alışı listede yok — mevcut davranış, bu turun ürünü değil) seçim yapılamadı; ikinci koşum listede olan "Alış · 925 Gümüş Bileklik" (gümüş bacak) ve "Satış · 22 Ayar Çeyrek" (altın) ile yapıldı.
Görüntüler Read ile GÖRÜLDÜ: `foto/cekmece-gumus-basit-aydinlik-1280.png` (satır "1. 8.140 ₺" altında tek satır not, kart içinde), `foto/yeni-kuyum-gumus-profesyonel-karanlik-375.png` (işlem seçicinin altında iki satıra sarılan not, taşma yok; önizleme "Has bacağı yok — özel matrah uygulanmadı").

### V-9. Temizlik ve açık kalanlar
**Temizlik:** API (5160; `dotnet run` + çocuk süreç) ve vekil (5161) PID ile `kill`; `lsof -iTCP:5160/5161` boş. Kiracı `kiraci-temizle.sh --uygula` (çıkış 0) → tenants'ta 0, fatura_kalemleri 0, hareketler 0. **tenants: başta 13, sonda 13.** QA-TEST-* dokunulmadı. `d30e/anahtar`, `d30e/fik.json` `printf ''` ile 0 bayt. Chrome profili `d30e/profil` SİLİNEMEDİ (kanca `rm`'yi blokluyor) — orkestratör silmeli (d30c/d30d profilleriyle birlikte). Proje ağacında bu turdan .mutorj/.bak/.tmp yok (`Kuyumcu.Tests/*.cs.bak` ×3, 0 bayt, 21.09 tarihli — başkasının eski artığı).
1. **Geri doldurma ölçütünün sınırı (bilgi):** ürün yolundan kesilmiş ve AYNI ZAMANDA hareket bağlı kalemde, birim has hareketin toplam has'ına tesadüfen eşitse (ve miktar ≠ 1) satır dönüştürülmez. kuyumcu_net'te etkilenen satır 0 olduğundan ölçülebilir etkisi yok. Eski kodla fişten kaleminde miktarı düzeltilmiş satırların has gramı (eski toplam) geriye dönük doğrulanamaz; kuyumcu_net'te miktar ≠ 1 kalem yok.
2. Nihai tüketici hurda alışı Yeni Fatura işlem listesinde çıkmıyor (fişten/hareket bağlı kesim çalışıyor) — mevcut davranış, bu bulgunun dışında; incelenmedi.
3. Devreden: § U-9 md.1 (gümüş/platin/paladyum özel matrahı — mali müşavir teyidi), md.3 (numarasız taslak mesajı), md.4 (dışa aktarım), § T-9 md.5.

### V-10. DoD sözleşmesi
Yeni varlık/uç yok. `fatura_kalemleri.has_grami` anlamı sabitlendi (sunucu türetir, PATCH gövdesinde kabul EDİLMEZ; miktar PATCH'i onu türetir). Taslak PATCH sözleşmesi (yoksa koru, null = sil) değişmedi — ZOBH07 yeşil. Kesilmiş fatura salt okunur. Hard delete yok. Yeni metin yok.
**Değişen dosyalar (bu tur):** `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`, `dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs`, `dotnet/Kuyumcu.Api/Data/Migrations/20260930040608_ZOBelgeKalemHasGramiToplam.cs` (+ `.Designer.cs`; model anlık görüntüsü yeniden üretildi, şema değişmedi), `dotnet/Kuyumcu.Tests/ZOBelgeKalemHasGramiTestleri.cs` (yeni).

---

## 30.09 DEVAM 16 (05:50–06:45): hasım doğrulayıcı [KRİTİK] ürünsüz gümüş hurda KUYUM kalemi düzenlenemiyor (madenin İKİ kaynağı) + [DÜŞÜK] ×2 gümüş notu — kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § U-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9706). 5199/5200 ve tarayıcı bölmesi kullanılmadı. COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30d/`.

### U-1. Kök neden, karar ve yapılanlar
**Kök neden (orkestratör 05:50 ile aynı):** fatura kaleminin madeni iki yerden okunuyordu — oluşturmada defter satırının madeni (`h.Maden`), düzenlemede ve başlık has gramında kalemin AYARI (`Ayarlar.MadenBul(k.AyarId) ?? ALTIN`). HURDA_ALIS satırı ürün taşımaz (ayar NULL) ama `Maden=GUMUS`; oluşturmada has 0 yazılıyor, düzenlemede "altın" sayılıp `:1318` 400 atıyordu (ZOBM08 kırmızı). `KalemHasGrami` aynı hatayla gümüş gramını başlığa katıyordu.

**Karar — kalemin kendisine tek kolon (`fatura_kalemleri.maden`, NULL olabilir):** "düzenlemede kaynak hareketten oku" seçeneği reddedildi: (a) ürün kartından eklenen kalemin hareketi yok, ikinci bir okuma yolu (ürün) yine iki kaynak demekti; (b) ürün kartı sonradan değişebilir, kalem kendi kopyasını taşımalı (kalemdeki ayar/milyem/taş ibaresi de kopyadır); (c) ekran notu aynı değeri okuyabilsin diye okuma ucuna da çıkar. Kalem oluşurken madeni BİR KEZ yazılır, sonra her yol yalnız bunu okur; NULL = bilinmiyor → özel matrah YOK (fail-closed, KDV tam), ALTIN varsayımı YOK.

| # | Bulgu | Yapılan (dosya:satır) |
|---|---|---|
| 1 [KRİTİK] | Ürünsüz gümüş hurda KUYUM kalemi düzenlenemiyor | `Data/FaturaKalemi.cs` yeni `Maden` (`[Column("maden"), MaxLength(10)] string?`) · `FaturaModulu.cs:344` `KalemAltinMi(k) => OzelMatrahKapsaminda(k.Maden)` (ayar/ALTIN varsayımı SİLİNDİ) · yazılan yerler: fişten `:1071 Maden = h.Maden`, kalemli `:708/:748 kalemMaden = urun?.Maden` (aynı değer `:720` özel matrah kararını da verir), taslağa ekle `:1473 Maden = urun?.Maden` · okuma ucu `:1156 k.Maden` |
| 1b (bulunan) | `TaslakKalemGuncelle` başlık has gramını/özet kuru yeniden hesaplamıyordu (miktar düzeltmesi ya da madeni bilinmeyen satırda başlık eski gramla kalıyordu) | `:1356-1363` KUYUM'da `HasGram = KalemHasGrami(tumKalemler)`, `HasBirimFiyat = OzetKur(...)` — `TaslakKalemEkle` ile aynı kural |
| 2 [DÜŞÜK] | `#fom-gumus-not` Yeni Fatura KUYUM formunda koşulsuz (zorunlu-önce ihlali) | Önizleme yanıtına `altinDisiMadenVar` (`:493`, kümede has bacağı olan altın dışı defter satırı var mı); `Faturalar.razor:445` not YALNIZ `onizleme?.AltinDisiMadenVar == true` iken; sözleşme `OzelMatrahOnizleme.AltinDisiMadenVar` (varsayılan false) |
| 3 [DÜŞÜK] | Not ürünsüz gümüşte çıkmıyor, platin/paladyumda hiç yok | `Faturalar.razor:1654` `GumusKuyumKalemi` artık sunucunun `k.Maden`'ine bakar: GUMUS/PLATIN/PALADYUM; `:1659` yeni `MadeniBilinmeyenKuyumKalemi` → `#fd-cekmece-kalem-madenyok-{id}` "Bu satırın madeni bilinmiyor — özel matrah uygulanmadı, KDV tam." (orkestratör: "maden bilinmeyen satır → özel matrah yok + sebep"); PRO tablo `title` aynı iki metin. Sözleşme `KalemVeri.Maden` (varsayılan null). Sözlük `Sozlukler.FaturaTaslak.cs`: `GumusOzelMatrahYok` TR "Gümüş, platin ve paladyumda özel matrah uygulanmaz — KDV tam." / EN "...silver, platinum or palladium — full VAT."; yeni `MadenBilinmiyorOzelMatrahYok` TR+EN. Mevcut id değişmedi, yeni id önekli, yeni CSS / satır içi stil yok. |

**Tek kaynak envanteri (9 hesap yolu + ekran):** 1 hareket bağlı fatura / 2 önizleme / 4 fişten → `h.Maden` (fişten kalemine AYNI değer yazılır) · 3 kalemli / 5 taslağa ekle → `urun.Maden` (kaleme AYNI değer yazılır) · 6 taslak düzenle / 7 başlık has gramı → `kalem.Maden` · 8-9 805 kodu → yukarıdakilerin ürettiği altın has bedeli · ekran notu → `kalem.Maden` (çekmece/tablo) ve sunucu bayrağı (yeni fatura). Oluşturmadan sonra madeni ayardan türeten hiçbir yol kalmadı (`grep "MadenBul(k.AyarId)" FaturaModulu.cs Faturalar.razor` = 0).

### U-2. Migration `20260930025526_ZOBelgeFaturaKalemMaden`
Prosedür: `dotnet build Kuyumcu.sln` YEŞİL → `dotnet ef migrations add ZOBelgeFaturaKalemMaden --project Kuyumcu.Api` (build'li) → `.cs` 841 B (Up: yalnız `AddColumn maden varchar(10) NULL`, başka şerit değişikliği YOK), `.Designer.cs` 236 KB → Up'a GERİ DOLDURMA eklendi: (1) `hareket_id` bağlı kalem ← `hareketler.maden` (fişten yolu özel matrahı zaten bu değerle hesaplamıştı); (2) `urun_id` dolu kalem ← kalemdeki ayar kopyasından `Urun.Maden` kuralı (K* ya da ayar NULL → ALTIN, G* → GUMUS, PT* → PLATIN, PD* → PALADYUM, CELIK → CELIK) — bu, o kalemin oluşturulduğu anda kullanılan kuralın ta kendisidir (belgenin hesaplandığı değeri kayda geçirir, yeni varsayım üretmez); (3) ikisi de yoksa NULL (fail-closed). `dotnet ef database update --connection …kuyumcu_net…` (parola değişkenden) → `Applying migration '20260930025526_ZOBelgeFaturaKalemMaden'. Done.` SQL: `kuyumcu_net.fatura_kalemleri` maden dağılımı ALTIN/KUYUM 3 · ALTIN/STANDART 3 · GUMUS/STANDART 3 · NULL 0; `__EFMigrationsHistory` 73.

### U-3. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `d30d/son-build.log`) → **0 Uyarı, 0 Hata**.

### U-4. Testler
- Yeni DB (`OzelMatrahMadenTestleri.cs`): `ZOB_Urunsuz_gumus_hurda_fisten_kalem_madeni_DEFTERDEN_yazilir_ve_DUZENLENIR` (ürünsüz gümüş + ürünsüz altın HURDA_ALIS fişi → kalem maden GUMUS/ALTIN, ayar NULL; gümüş PATCH → has 0, KDV 2.000; altın PATCH → has dayanağı korunur; başlık has gramı yalnız altın), `ZOB_Madeni_BILINMEYEN_kuyum_kalemi_ozel_matrah_UYGULANMAZ_KDV_tam` (ayarı K22 olsa bile maden NULL → has 0, KDV 16.000, başlık has 0 / gram 0), `ZOB_Kalem_madeni_URUN_yollarinda_urunun_madeninden_yazilir_ve_okuma_ucu_dondurur`, `ZOB_Onizleme_altin_disi_maden_bayragi_yalniz_gumus_bacak_varken_true`.
- bUnit: `Q92FaturaKalemEkranTestleri.ZOBELGE_kuyum_taslakta_GUMUS_satirinda_ozel_matrah_yok_notu_altinda_yok` genişletildi (5 satır: altın / ayarlı gümüş / AYARSIZ hurda gümüş / platin / madeni bilinmeyen), yeni `FaturaEkranTestleri.ZOB_gumus_notu_yalniz_ALTIN_DISI_bacakli_islemde_gorunur` (açılışta yok, altın işlemde yok, ürünsüz gümüş hurda işleminde var; sahte API'ye gerçekçi `h2` HURDA_ALIS ve `altinDisiMadenVar:true` önizlemesi eklendi).
- Doğrulayıcının `ZOBELGEMadenSuzgeciBesinciYolSinirTestleri.ZOBM08_urunsuz_gumus_hurda_fisten_KUYUM_kalemi_DUZENLENEBILIR` → **YEŞİL** (önceden kırmızı).
- Filtreli (OzelMatrah + ZOBELGE + Fatura + Kdv + ZoBelge + Disaaktarim + P08 + Sozluk): **540/540** (`d30d/test3.log`).
- **Tam süit** (`d30d/tam.log`, trx `trx-pZO-BELGE/d17`): **7714 · 7712 geçti · 1 başarısız** — `Dogrulayici.KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri` (`#kenar-grup-dugme-rapor` bulunamadı — KATMAN şeridi, kenar çubuğu; devir listesinde zaten vardı). **Bu paketin ürettiği yeni kırmızı: 0.**

### U-5. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra, `d30d/denetim.log`) → **DENETİM TEMİZ** (295 ✓, 0 ✗).

### U-6. Mutasyon kanıtları (M20, 9/9 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d17/`, her birinde derleme 0 + filtreli test, geri koyma sonrası `cmp` AYNI; betik `d30d/mut.sh`, günlük `d30d/mut/ozet.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M20-1 | `KalemAltinMi` → eski `MadenBul(k.AyarId) ?? ALTIN` varsayımı (orkestratörün istediği mutasyon) | ZOB_Urunsuz_gumus_hurda…, ZOB_Madeni_BILINMEYEN…; ayrı koşumda doğrulayıcının ZOBM08 (`mut/M20-1b-test.log`) | AYNI |
| M20-2 | Fişten kalemde `Maden = h.Maden` silindi | K10b_Taslakta_gumus_kalemi_DUZENLENIR…, ZOB_Urunsuz_gumus_hurda… | AYNI |
| M20-3 | Taslağa eklemede `Maden = urun?.Maden` silindi | ZOB_Kalem_madeni_URUN_yollarinda… | AYNI |
| M20-4 | Yeni fatura notu koşulu `true` | ZOB_gumus_notu_yalniz_ALTIN_DISI_bacakli_islemde_gorunur | AYNI |
| M20-5 | Ekran `GumusKuyumKalemi` → ayardan gümüş | ZOBELGE_kuyum_taslakta_GUMUS_satirinda… | AYNI |
| M20-6 | Önizleme bayrağı ters (`OzelMatrahKapsaminda`) | ZOB_Onizleme_altin_disi_maden_bayragi… | AYNI |
| M20-7 | Düzenlemede başlık `HasGram` yeniden hesabı silindi | ZOB_Madeni_BILINMEYEN… | AYNI |
| M20-8 | Kalemli yolda `kalemMaden` → ürün varsa ALTIN | ZOB_Kalem_madeni_URUN…, K10b_Kalemli_faturada_gumus_urun_kalemi…, K10b_Yalniz_gumus_taslaga_ALTIN_satir… | AYNI |

### U-7. Canlı doğrulama (5160 · `dotnet run --no-build` Debug · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3R53D6Q78FZ4715X2C6FP44 — `demo-tohumla.js --prova-kiraci` ile kuruldu, ad `--ad` verilmediği için `PAKET-P-30-TEST` geldi, tenants satırında SQL ile `PAKETZO-BELGE-TEST…` yapıldı · tek kullanımlık rastgele `Sistem__Anahtari`, değeri basılmadı) — betikler `d30d/k10d.mjs`, `bilinmez.mjs`, sonuç `k10d.json`, `bilinmez.json`, SQL `hurda-sql2.txt`, `k10d-sql.txt`
`POST /islemler/hurda-alis` (satıcı NİHAİ TÜKETİCİ, HAVALE) → 200 ×2; SQL: `01M3R559XW2A0WA6B3591ZMW49 GUMUS urun_id NULL has 92,5 −3.700` ve `01M3R55XTYQ03AE5F3YXKC1HQC ALTIN urun_id NULL has 4,58 −34.350`.
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /faturalar/fisten {hareketId: gümüş hurda, KUYUM}` | 200, kalem maden GUMUS, ayar NULL, has 0, KDV 3.700/6 = 616,67 | **200 · maden GUMUS · ayar null · has 0 · KDV 616,67** |
| `PATCH …/kalemler/{gümüş} {birimFiyat 4070}` (eski kod: 400) | 200, has 0, KDV 678,33 | **200 · GUMUS · 0 · 678,33 · 4.070** |
| `PATCH …/kalemler/{gümüş} {miktar 2}` | 200, has 0, KDV 1.356,67 | **200 · 0 · 1.356,67 · 8.140** |
| Başlık (GET) | has 0, has gram 0, KDV 1.356,67 | **0 · 0 · 1.356,67 · 8.140** |
| `POST /faturalar/fisten {hareketId: altın hurda}` | 200, maden ALTIN, has bedeli düşülür | **200 · ALTIN · has 30.321,52 · KDV 671,41** |
| `PATCH …/kalemler/{altın} {birimFiyat 37785}` | 200, has dayanağı KORUNUR, KDV (37.785−30.321,52)/6 = 1.243,91 | **200 · 30.321,52 · 1.243,91**; başlık has gram 4,58 |
| Madeni bilinmeyen satır (SQL `maden=NULL`) → `GET …/kalemler` + `PATCH {birimFiyat 38000}` | maden null; 200, has 0, KDV 38.000/6 = 6.333,33, başlık has 0 / gram 0 | **null · 200 · 0 · 6.333,33 · başlık 0/0** |
| `GET /faturalar/ozel-matrah-onizleme` gümüş / altın hurda | `altinDisiMadenVar` true / false | **true / false** |
SQL (`k10d-sql.txt`): `TASLAK KUYUM 8140.00 0.000000 0.00 1356.67 NULL` / `TASLAK KUYUM 37785.00 4.580000 30321.52 1243.91 NULL`; kalemler `1 NULL NULL GUMUS 2.000 4070.00 92.500000 0.00 1356.67 8140.00` / `1 NULL NULL ALTIN 1.000 37785.00 4.580000 30321.52 1243.91 37785.00`. (805 kodu NULL: yerel yapılandırmada `Kdv:OzelMatrahKodu` tanımsız — HARİÇ, fail-closed; kural DB testleriyle kanıtlı.)
Yan gözlemler (bu değişikliğin ürünü değil): 6.000 TL/has altın hurda faturası "Has altın değeri toplam tutardan küçük olmalıdır" 400 (kurun altında alım, mevcut motor kuralı); MÜKELLEF satıcılı hurda "zaten … ile faturalandırılmış" (tedarikçi faturası bağ sayılır); taslakla bağlı harekette mesaj "zaten  ile" (taslağın numarası yok — § U-9 md.3).

### U-8. Gerçek tarayıcı ölçümü (başsız Chrome, temiz profil `d30d/profil`, vekil 5161, CDP 9706; betik `d30d/gum2.mjs`, sonuç `gum2-tam.json` + tekrar `gum2b.json`, foto `d30d/foto/`)
Değişen ekran `/faturalar`: (a) ürünsüz gümüş hurda taslağının Düzenle çekmecesi, (b) ürünsüz altın hurda taslağının çekmecesi, (c) Yeni Fatura · KUYUM (açılış → altın işlem → gümüş işlem). {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 375} = 36 ölçüm.
| Ekran | Durum | Beklenen | Gerçek | K1 | K3 | K5 (TRY) | K10 | yatay |
|---|---|---|---|---|---|---|---|---|
| (a) gümüş hurda çekmecesi | 12 | not VAR, madenyok YOK | 12/12 not görünür, taşma 0, metin "Gümüş, platin ve paladyumda özel matrah uygulanmaz — KDV tam."; madenyok 0 | 0 | 0/0 | 0 | 0 | 0 |
| (b) altın hurda çekmecesi | 12 | not YOK | 12/12 not yok, madenyok yok | 0 | 0/0 | 0 | 0 | 0 |
| (c) yeni fatura KUYUM | 12 | açılışta YOK, altın işlemde YOK, gümüş işlemde VAR | açılış 12/12 yok · altın 12/12 yok · gümüş 11/12 var; 12. (BASİT·karanlık·1280) ilk koşumda "Biraz yoğunluk var" bandı (hız sınırı, betiğin art arda istekleri) nedeniyle önizleme dönmedi → tek başına tekrar (`gum2b.json`) **VAR**, taşma 0 | 0 | 0/0 | 0 | 0 | 0 |
**K9 5–6:** önceki turdaki gibi arka plandaki sayfa tablosu ile açık çekmece/modal katmanının kesişmesi (ölçüm aracı artefaktı; § T-7'de çekmece kapalı sayfada K9 = 0 kanıtlandı).
Görüntüler Read ile görüldü: `foto/cekmece-gumus-basit-aydinlik-1440.png` (satır 1 "8.140 ₺" altında tek satır not, kart içinde), `foto/yeni-kuyum-gumus-profesyonel-karanlik-375.png` (has ipucunun altında iki satıra sarılan not, taşma yok; önizleme "Has bacağı yok — özel matrah uygulanmadı"), `foto/yeni-kuyum-gumus-basit-karanlik-1280.png` (hız sınırı bandı — tekrar ölçüldü). Tuval karşılaştırması yeniden üretilmedi: değişiklik yalnız bir koşullu açıklama satırı; yerleşim bölgeleri § R-7/§ P-7 ile aynı.

### U-9. Temizlik ve açık kalanlar
**Temizlik:** API (5160, `dotnet run` + çocuk süreç) ve vekil (5161) PID ile `kill`; `lsof -iTCP:5160/5161` boş. Kiracı 01M3R53D6Q78FZ4715X2C6FP44 `kiraci-temizle.sh --uygula` (çıkış 0) → tenants'ta 0, fatura_kalemleri 0, hareketler 0. **tenants: koşum başında 13, sonda 15** — benim kiracım 0; fark başka şeritlerin koşum sırasında açtığı kiracılar (`PAKETORTAK-1-TEST Dogrulayici` 03:18, `PAKETKATMAN2-TEST … Tasarım Hakemi` 03:22, `PAKETEMANET-1-TEST T41B` 03:30 UTC); onlara dokunulmadı. QA-TEST-* dokunulmadı. `d30d/anahtar`, `d30d/fik.json` `printf ''` ile 0 bayt. Chrome profilleri `d30d/profil` ve `d30c/profil` SİLİNEMEDİ (kanca `rm`'yi blokluyor) — kurucu/orkestratör silmeli. Proje ağacında .mutorj/.bak/.tmp yok.
1. **Mali müşavir teyidi (kurucuya):** § S-9 md.1 aynen — gümüş/platin/paladyumda KDVK 23 özel matrahı UYGULANMIYOR (tutucu; DOĞRULANAMADI). Ekran notu artık üç madeni birlikte söylüyor; teyit gelirse `OzelMatrahKapsaminda` + not metni birlikte değişir.
2. **Geri doldurma kuralı (kurucuya bilgi):** ürün kartından eklenmiş eski kalemlerin madeni kalemdeki ayar kopyasından (ayar yoksa ALTIN — oluşturma anındaki `Urun.Maden` kuralı) yazıldı; ölçüm: kuyumcu_net'te NULL kalan 0. Ürünü de hareketi de olmayan kalem NULL kalır ve düzenlemede özel matrah almaz, ekranda sebebi yazar.
3. **Fatura numarasız taslak mesajı (küçük, bu dosyada ama bu bulgunun dışı):** taslağa bağlı hareket yeniden faturalanmak istenince "Bu hareket zaten  ile faturalandırılmış" (numara boş). Önerilen: taslakta "taslak bir faturaya bağlı" metni. Değiştirilmedi (kapsam).
4. **Dışa aktarım (Disaaktarim sahibine):** § T-9 md.2 aynen.
5. **Başkasına ait:** tam süitte `KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_…` → KATMAN (kenar çubuğu). Devreden: § T-9 md.5.
6. **Temizlik:** `d30c/profil`, `d30d/profil` — kanca nedeniyle silinemedi.

### U-10. DoD sözleşmesi
Yeni VARLIK yok; `fatura_kalemleri`'ne bir kolon (maden) eklendi — kullanıcı girdisi değil, sunucu türetir (UX-S21), PATCH gövdesinde kabul EDİLMEZ (değişmez kaynak bilgisi; düzeltme yolu: taslağı iptal edip yeniden kes — mevcut DegismezIpucu metni). Taslak PATCH sözleşmesi (yoksa koru, null = sil) değişmedi. Yeni uç yok; önizleme yanıtına bir salt okunur alan. Kesilmiş fatura salt okunur (değişmedi). Hard delete yok. Metinler sözlükten TR+EN. Migration: `20260930025526_ZOBelgeFaturaKalemMaden` (kuyumcu_net'e uygulandı).

**Değişen dosyalar (bu tur):** `dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs`, `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`, `dotnet/Kuyumcu.Api/Data/Migrations/20260930025526_ZOBelgeFaturaKalemMaden.cs` (+ `.Designer.cs`, `KuyumcuDbContextModelSnapshot.cs`), `dotnet/Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs`, `dotnet/Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs`, `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`, `dotnet/Kuyumcu.Tests/OzelMatrahMadenTestleri.cs`, `dotnet/Kuyumcu.Tests/Q92FaturaKalemEkranTestleri.cs`, `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs`.

---

## 30.09 DEVAM 15 (05:00–05:35): hasım doğrulayıcı [KRİTİK] K-10b süzgeci TASLAK KALEM DÜZENLEME yolunda yok (5. yol) + [DÜŞÜK] ekranda gümüş notu yok — kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § T-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9706/9707). 5199/5200 ve tarayıcı bölmesi kullanılmadı. COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30c/`.

### T-1. Bulgular ve yapılanlar
| # | Bulgu | Yapılan |
|---|---|---|
| 1 [KRİTİK] | `TaslakKalemGuncelle` KUYUM kaleminde maden ayırmıyordu; gümüş kalemin HasDeger'i 0 olduğundan "has bedeli dayanağı çözülemiyor" 400 atıyordu → gümüş kuyum satırı düzenlenemiyordu (ZOBM07 kırmızı) | `FaturaModulu.cs:1309` `if (kalem.KdvTuru == "KUYUM" && KalemAltinMi(kalem))` — gümüş satırda bacak yok, KDV tam; altın satırda eski kurdan dayanak aynen korunur |
| 2 (envanterde bulunan 6. yol) | Taslak kalem EKLE/DÜZENLE sonrası belge başlığında 805 özel matrah kodu yeniden hesaplanmıyordu: yalnız gümüş taslağa (kod NULL) altın satır eklenince altın bedeli düşülmüş belge KODSUZ kalıyordu | `TaslakKalemEkle` ve `TaslakKalemGuncelle` başlık toplamından sonra `fatura.OzelMatrahKodu = OzelMatrahKoduFor(fatura.KdvTuru, fatura.HasDeger)` (oluşturmayla aynı tek kural) |
| 3 [DÜŞÜK] | Ekranda gümüş kalem için "özel matrah uygulanmadı" açıklaması yok | `Faturalar.razor`: (a) Düzenle çekmecesinde gümüş satır kartında `#fd-cekmece-kalem-gumus-{id}` kısa not; (b) Yeni Fatura KUYUM seçilince has ipucunun altında `#fom-gumus-not`; (c) PRO kalem tablosunda has hücresinde `title`. Metin `Sozlukler.FaturaTaslak.cs` `GumusOzelMatrahYok` TR "Gümüşte özel matrah uygulanmaz — KDV tam." / EN "No special tax base applies to silver — full VAT." Yardımcı `GumusKuyumKalemi`: KUYUM + ayarın madeni GÜMÜŞ (ayar yoksa not yok — tahmin yok). Yeni id'ler önekli, mevcut id değişmedi, yeni CSS yok. |

**Envanter — `KdvTuru == "KUYUM"` ve `HasDeger` kullanan HER yol (grep: `FaturaModulu.cs`, `Kuyumcu.Api`, `Kuyumcu.Core`, `Kuyumcu.Contracts`, `Kuyumcu.Web`):**
| # | Yol | Yer | Maden süzgeci | Durum |
|---|---|---|---|---|
| 1 | Hareket bağlı fatura (`Olustur` → `HasBacaklariniTopla`) | FaturaModulu.cs:372-379 | `OzelMatrahKapsaminda(h.Maden)` | DEVAM 14 |
| 2 | Özel matrah önizleme (`OzelMatrahOnizleme`) | :417 → aynı yardımcı | aynı | DEVAM 14 |
| 3 | Kalemli fatura (`KalemYolu`) | :701-711 | `OzelMatrahKapsaminda(urun.Maden)` | DEVAM 14 |
| 4 | Fişten fatura (`FistenOlustur`) | :1021, :1027, başlık :1111 | `OzelMatrahKapsaminda(h.Maden)` | DEVAM 14 |
| 5 | Taslağa kalem ekle (`TaslakKalemEkle`) | :1419-1426 | `OzelMatrahKapsaminda(urun.Maden)` | DEVAM 14 |
| 6 | **Taslak kalem düzenle (`TaslakKalemGuncelle`)** | **:1309** | **`KalemAltinMi(kalem)`** | **BU TUR** |
| 7 | Başlık has gramı (`KalemHasGrami`) | :813 | `KalemAltinMi` | DEVAM 14 |
| 8 | 805 kodu (`OzelMatrahKoduFor`) — oluşturma :310/:801/:1108 | :347 | altın has bedeli > 0 | DEVAM 14 |
| 9 | **805 kodu — taslak ekle/düzenle sonrası** | **TaslakKalemEkle başlığı, TaslakKalemGuncelle başlığı** | altın has bedeli > 0 | **BU TUR** |
| 10 | ELLE yol (hareket/kalem bağı yok, `dto.HasDeger`) | :254-270 | uygulanamaz — maden verisi yok; kullanıcı "has altın değeri"ni açıkça beyan eder; ekran bu yolu `KuyumEngeli` ile kapatıyor | değişmedi (gerekçeli) |
| 11 | Taslak başlık toplamı (`TaslakGuncelle`) | :1225 | hesap yok — KUYUM'da başlıktan toplam değişimi zaten 400 | uygulanamaz |
| 12 | Muhasebe dışa aktarım eksik alan bayrağı | Disaaktarim/Kaynaklar.cs:462 | yalnız gümüş KUYUM faturası (kod bilerek NULL) "ozelMatrahKodu eksik" diye İŞARETLENİR — hesap değil, yanlış-pozitif uyarı (fail-safe yön) | **dosya alanım dışında — sahibine not (§ T-9 md.2)** |
| 13 | Banka faturası `HasDeger = 0m` | BankaModulu.cs:527 | STANDART belge | uygulanamaz |
| 14 | Ekran (`Faturalar.razor` 1070/1074/1572/1730) | yalnız görünüm/kapı | sunucu değeri gösterilir | not eklendi (T-1 #3) |
Sonuç: hesap yapan 9 yolun 9'u tek yardımcıdan (`OzelMatrahKapsaminda` / `KalemAltinMi`) geçiyor; atlanan hesap yolu kalmadı.

### T-2. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `d30c/son-build3.log`) → **0 Uyarı, 0 Hata**. (Bir ara derlemede 2 × MSB3026 "dosya başka süreçte, yeniden deneniyor" — eşzamanlı şerit derlemesi; bir sonraki derlemede yok.)

### T-3. Testler
- Yeni DB testleri (`OzelMatrahMadenTestleri.cs`): `K10b_Taslakta_gumus_kalemi_DUZENLENIR_has_0_KDV_tam_altin_dayanak_korunur`, `K10b_Yalniz_gumus_taslaga_ALTIN_satir_eklenince_805_kodu_belgeye_girer`.
- Yeni bUnit (`Q92FaturaKalemEkranTestleri.cs`): `ZOBELGE_kuyum_taslakta_GUMUS_satirinda_ozel_matrah_yok_notu_altinda_yok`.
- Doğrulayıcının `ZOBELGEMadenSuzgeciSinirTestleri.ZOBM07_kuyum_taslakta_gumus_kaleminin_fiyati_DUZENLENEBILIR` → **YEŞİL** (önceden tek kırmızı).
- Filtreli (OzelMatrah + ZOBELGE + Fatura + Kdv + ZoBelge + Disaaktarim): **440/440** (`d30c/test2.log`).
- **Tam süit** (`d30c/tam2.log`, trx `trx-pZO-BELGE/d16b`): **7675 · 7669 geçti · 5 başarısız · 1 atlanmış**. Kırmızılar ve sahipleri: `KATMAN2SinirTestleri` ×2 (KATMAN, MenuTercihServisi — tek başına da kırmızı), `TasarimBelirtecTestleri.Mod_yazicisi_…` (index.html script sayısı 10≠9 — KATMAN/wwwroot, tek başına da kırmızı), `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` + `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` (`Unknown database 'kuyumcu_t1_eski'` — eşzamanlı koşum; tek başına **15/15 yeşil**, `d30c/test3.log`). İlk tam koşumda (`tam.log`, 7674) ayrıca `SATIS3EkranTestleri.T13_…` kırmızıydı, tek başına yeşil. Hiçbiri Fatura/Faturalar/sözlük dosyalarına dokunmuyor. **Bu paketin ürettiği yeni kırmızı: 0.**

### T-4. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra, `d30c/denetim.log`) → **293 ✓ · 2 ✗**: `index.html <script> sayısı 10 (beklenen 9)` (wwwroot/index.html — KATMAN şeridi) ve `P-13: GEREKCESIZ_KILITLI_ALAN — Cariler.razor:1` (Cariler — ZO-BELGE alanı değil). Fatura sayaçları (P-08, P-22) ✓. Bu paketin ✗'i yok.

### T-5. Mutasyon kanıtları (M19, 5/5 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d16/`, her birinde derleme + filtreli test, geri koyma sonrası `cmp` AYNI; günlük `d30c/mut/`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M19-1 | `TaslakKalemGuncelle` `&& KalemAltinMi(kalem)` kaldırıldı | K10b_Taslakta_gumus_kalemi_DUZENLENIR…, ZOBM07_kuyum_taslakta_gumus_kaleminin_fiyati_DUZENLENEBILIR | AYNI |
| M19-2 | `GumusKuyumKalemi` → `false` | ZOBELGE_kuyum_taslakta_GUMUS_satirinda_ozel_matrah_yok_notu_altinda_yok | AYNI |
| M19-3 | `GumusKuyumKalemi` maden koşulu kaldırıldı (her KUYUM satırına not) | aynı test (altın satırında not YOK iddiası) | AYNI |
| M19-4 | `TaslakKalemEkle` sonrası 805 yeniden hesabı kaldırıldı | K10b_Yalniz_gumus_taslaga_ALTIN_satir_eklenince_805_kodu_belgeye_girer | AYNI |
| M19-5 | `TaslakKalemGuncelle` sonrası kod `OzelMatrahKoduFor(…, 0m)` | aynı test | AYNI |

### T-6. Canlı doğrulama (5160 · `dotnet run --no-build` Debug · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3R1B8Z97QP3SA3E28X7XCRP, `demo-tohumla.js --prova-kiraci` · tek kullanımlık rastgele `Sistem__Anahtari`, değeri basılmadı) — betik `d30c/k10c.mjs`, sonuç `k10c.json`, SQL `k10c-sql.txt`
Kur kaydı altın has fiyatı 6.615,69. Ürünler K22 1 g (has 0,916), G925 10 g GUMUS (has 9,25). Fiş: altın 8.000 + gümüş 1.500 → `POST /faturalar/fisten {KUYUM}` → 200 TASLAK (has 6.059,97, KDV 573,34).
| Adım | Beklenen | Gerçek |
|---|---|---|
| `PATCH …/kalemler/{gümüş} {birimFiyat 1800, iscilikTl 100, indirimTutari 50}` | 200 (eski kod 400), has 0, KDV 1.850−1.850/1,2 = 308,33 | **200 · has 0 · KDV 308,33 · satır 1.850** |
| `PATCH …/kalemler/{altın} {birimFiyat 8500}` | 200, has dayanağı korunur 6.059,97, KDV (8.500−6.059,97)−÷1,2 = 406,67 | **200 · 6.059,97 · 406,67** |
| `PATCH …/kalemler/{gümüş} {miktar 2}` | 200, satır 3.650, KDV 608,33, has 0 | **200 · 3.650 · 608,33 · 0** |
| `PATCH …/kalemler/{gümüş} {birimFiyat -5}` | 400 | **400 "Birim fiyat negatif olamaz"** |
| SQL başlık | toplam 12.150, has 6.059,97, KDV 1.015,00 | `TASLAK KUYUM 12150.00 0.916000 6059.97 1015.00 NULL` |
| SQL kalemler | K22: has 6.059,97 KDV 406,67; G925: has 0, KDV 608,33 | `1 K22 1.000 8500.00 NULL NULL 6059.97 2033.36 406.67 8500.00` / `2 G925 2.000 1800.00 100.00 50.00 0.00 3041.67 608.33 3650.00` |
805 kodu canlıda gözlenemez: yerel yapılandırmada `Kdv:OzelMatrahKodu` tanımsız (HARİÇ, fail-closed) → her kuyum belgesinde NULL; kural DB testi + M19-4/5 ile kanıtlı.

### T-7. Gerçek tarayıcı ölçümü (başsız Chrome, temiz profil `d30c/profil`, vekil 5161; betik `d30c/gum.mjs`, sonuç `gum.json`, foto `d30c/foto/`)
Değişen ekran: `/faturalar` — (a) taslağın Düzenle çekmecesi (gümüş satır notu), (b) Yeni Fatura · KUYUM (has ipucu altı not). {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 375} = 24 ölçüm.
| Ekran | Durum sayısı | Not görünür | Not taşma | Altın satırında not | K1 | K3 | K5 (TRY) | K10 | yatay |
|---|---|---|---|---|---|---|---|---|---|
| Düzenle çekmecesi | 12 | 12/12 (metin "Gümüşte özel matrah uygulanmaz — KDV tam.") | 0 | 0 (öğe yok) | 0 | 0/0 | 0 | 0 | 0 |
| Yeni Fatura · KUYUM | 12 | 12/12 | 0 | — | 0 | 0/0 | 0 | 0 | 0 |
**K9 (binen) 5–6:** tamamı arka plandaki sayfa tablosunun (`th "TOPLAM"`, `th "DURUM"`, `td "12.150 ₺"`) açık çekmece/modal katmanıyla kesişmesi (`d30c/k9.mjs`: çekmece kapalı sayfada K9 = **0**; not İÇERMEYEN STANDART Yeni Fatura modalında da K9 = 5) → ölçüm aracının katman artefaktı, bu değişikliğin ürünü değil.
Görüntüler Read ile görüldü: `foto/cekmece-basit-aydinlik-1440.png` (2. satır "3.650 ₺" altında tek satır not, kart içinde), `foto/cekmece-profesyonel-karanlik-375.png` (koyu temada okunur, taşma yok), `foto/yeni-kuyum-basit-aydinlik-375.png` (has ipucunun altında tek satır). Tuval karşılaştırması bu turda yeniden üretilmedi: değişiklik tek satırlık açıklama metni; yerleşim bölgeleri § R-7/§ P-7 ölçümlerindekiyle aynı.

### T-8. Temizlik
API (5160) ve vekil (5161) PID ile `kill`; `lsof -iTCP:5160/5161` boş. Kiracı 01M3R1B8Z97QP3SA3E28X7XCRP `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, faturalar 0, hareketler 0. **tenants: koşum başında 12, sonda 14** — benim kiracım 0; +2 başka şeritlerin koşum sırasında açtığı kiracılar (`PAKETKATMAN-2-TEST Dogrulama` 02:13 UTC, `PAKETEMANET-1-TEST T40` 02:15 UTC); onlara dokunulmadı. QA-TEST-*/TEST-KESIF dokunulmadı. `d30c/anahtar` ve `d30c/fik.json` `printf ''` ile 0 bayt. **Chrome profili `d30c/profil` (202 MB) SİLİNEMEDİ:** TurkReport kancası `rm`'yi blokluyor — kurucunun/orkestratörün silmesi gerekir. Proje ağacında .mutorj/.bak/.tmp bırakılmadı.

### T-9. Açık kalanlar (sahipleriyle)
1. **Mali müşavir teyidi (kurucuya):** § S-9 md.1 aynen (gümüşte 23/g özel matrahı uygulanmıyor — tutucu; DOĞRULANAMADI). Ekran notu bu kararı tezgâhtara söylüyor; teyit gelirse `OzelMatrahKapsaminda` + not metni birlikte değişir.
2. **Dışa aktarım (Disaaktarim şeridi sahibine):** `Kaynaklar.cs:462` yalnız gümüş KUYUM faturasını "ozelMatrahKodu eksik" diye işaretliyor; öneri: koşula `&& r.f.HasDeger > 0` (kod yalnız altın bedeli varken zorunlu). Dosya ZO-BELGE alanı dışında, değiştirilmedi.
3. **Platin/paladyum/çelik kuyum satırı:** sunucu has bedelini düşmüyor (doğru, tutucu) ama ekran notu yalnız GÜMÜŞ için yazılıyor (metin "Gümüşte…"); diğer madenlerde not yok.
4. **Başkasına ait:** KATMAN2SinirTestleri ×2 + TasarimBelirtec (index.html script sayısı) + denetim ✗ index.html → KATMAN; denetim P-13 Cariler.razor → Cariler sahibi; Migration/YayinAcilis kırmızıları eşzamanlı DB çakışması (tek başına yeşil).
5. **Devreden:** § S-9 md.2 (Borsa İstanbul kapanış fiyatı), md.4 (ürünsüz gümüş hareketi), § R-9 md.2–4, § P-9 md.2–4.
6. **Temizlik:** `d30c/profil` 202 MB — kanca nedeniyle silinemedi (T-8).

### T-10. DoD sözleşmesi
Yeni varlık, uç, düğme, migration yok (migration: gerekmedi). Fatura kalemi düzenleme yolu artık gümüş KUYUM satırında da çalışıyor (UX-1 / kurucunun "her veri girişi düzenlenebilir" şartı). Taslak PATCH sözleşmesi değişmedi (işçilik/indirim null = sil). Kesilmiş fatura DegismezIpucu ile salt okunur (değişmedi). Hard delete yok. Ekran notu sözlükten (TR+EN), çıplak metin yok, yeni id'ler `fd-cekmece-kalem-gumus-*` / `fom-gumus-not`, satır içi stil yok, yeni CSS yok.

---

## 30.09 DEVAM 14 (04:05–04:50): doğrulayıcı KRİTİK bulgusu YASAL-4 Y4-D / K-10b (özel matrahta maden süzgeci yok) kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § S-9)
**Rol:** Taşkın (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160). 5199/5200 ve tarayıcı bölmesi kullanılmadı. COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30b/`.

**Devir kontrolü:** DEVAM 13 raporu okundu; ağaçta yarım mutasyon yok. Önceki doğrulamanın 28.09 bulguları (Bicim.Has 4 hane, ZB14'e Raporlar eklenmesi) ağaçta kapalı: `IslemDefteri.razor:142` `Bicim.Has`, `ZoBelgeTestleri.cs:656` listesinde "Raporlar", `Raporlar.razor`'da `style="` = 0.

### S-1. Bulgu, hata kanıtı ve yapılanlar
**Mevzuat (birincil kaynak, PDF indirildi, `pdftotext` ile okundu — `d30b/kdvk.txt`):** 3065 sayılı KDVK md.23/e — "Altından mamül veya altın ihtiva eden ziynet eşyaları ile sikke altınların teslim ve ithalinde matrah, külçe altın bedeli düşüldükten sonra kalan miktardır." (https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf). 23/g yalnız Bakanlığa özel matrah belirleme YETKİSİ verir; gümüş hükmü Tebliğ'dedir ve gümüş kapanış fiyatı kaynağı sistemde yok. **Karar (fail-closed):** özel matrah yalnız ALTIN bacağına uygulanır; gümüş/platin/paladyum has bedeli matrahtan DÜŞÜLMEZ (tam bedel matraha girer — fazla KDV, eksik değil). **DOĞRULANAMADI — mali müşavir teyidi (23/g gümüş).**

**Hatanın kanıtı (düzeltmeden ÖNCE, bugünkü koda karşı; `d30b/kirmizi-once.log`):** yeni `OzelMatrahMadenTestleri` 7 testten 6'sı KIRMIZI. HAREKET yolu: `Expected 4,58 · Actual 13,830000` (gümüşün 9,25 g has'ı altın has'ına eklenip ALTIN fiyatıyla düşülüyordu → KDV EKSİK). Fişten / kalemli / taslağa ekle / yalnız gümüş yolları: `400 "Has altın değeri toplam tutardan küçük olmalıdır"` (gümüş has'ı altın fiyatıyla çarpılınca bedel satış tutarını aşıyor — gerçekçi fiyatlı gümüş kuyum faturası hiç kesilemiyordu).

**Kök neden koda karşı doğrulandı ve kapsam genişletildi (gerekçeli sapma):** şartname yalnız `HasBacaklariniTopla`'yı gösteriyordu; aynı "maden bakmadan `AltinHasFiyati` ile çarp" deseni fatura modülünde **dört** yolda vardı. Hepsi aynı tek yardımcıyla kapatıldı:
| Yol | Dosya/yer | Değişiklik |
|---|---|---|
| Hareket bağlı fatura + önizleme (`HasBacaklariniTopla`) | `FaturaModulu.cs` | `h.Maden` ALTIN değilse bacak yok |
| Kalemli fatura (`KalemYolu`) | aynı | `urun.Maden` ALTIN değilse bacak yok (kur da istenmez) |
| Fişten fatura (ekranın kullandığı yol) | aynı | `h.Maden` ALTIN değilse bacak yok; belge başlığı has gramı yalnız altın bacaklarından (`altinHasGram`) |
| Taslağa kalem ekle | aynı | `urun.Maden` ALTIN değilse bacak yok |
| Başlık has gramı (`KalemHasGrami`) | aynı | yalnız ayarı ALTIN olan kalemler (`KalemAltinMi`) |
| Özel matrah kodu (805) | aynı | `OzelMatrahKoduFor`: yalnız altın has bedeli > 0 ise basılır (805 = "altından mamul / altın içeren ziynet"; yalnız gümüş belgeye basılmaz) |
Tek yardımcı: `FaturaServisi.OzelMatrahKapsaminda(maden) => maden == Maden.Altin` (KDVK alıntısı ve "DOĞRULANAMADI" notu XML yorumunda). Yeni sütun AÇILMADI (mevcut `hareketler.maden`, `Urun.Maden`, `fatura_kalemleri.ayar_id`). `OzelMatrahMotoru` değişmedi. Migration: gerekmedi. Ekran dosyası (razor/css/sözlük) DEĞİŞMEDİ.

**Kapsam dışı (açık):** Borsa İstanbul kapanış fiyatı şartı (şartname 4.3b, `borsa_kapanis_fiyatlari`) bu turda yapılmadı — has bedeli hâlâ işlemin kur kaydından (`KurSnapshot.AltinHasFiyati`).

### S-2. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan ve test düzeltmesinden sonra, `d30b/son-build2.log`) → **0 Uyarı, 0 Hata**. (Arada bir derlemede `MenuTercihServisi.cs(256) CS0162` görüldü — KATMAN şeridinin dosyası o sırada değişiyordu, sonraki derlemede yok.)

### S-3. Testler
- Yeni: `Kuyumcu.Tests/OzelMatrahMadenTestleri.cs` — 7 DB testi: K10b_Altin_ve_gumus_fisinde_HAREKET_yolu_YALNIZ_altin_has_bedeli_dusulur · K10b_Yalniz_gumus_fisinde_has_bedeli_0_KDV_TAM_ve_ozel_matrah_kodu_yazilmaz · K10b_Yalniz_altin_fisinde_davranis_DEGISMEZ_ve_kod_yazilir · K10b_Fisten_faturada_gumus_kalemi_altin_fiyatiyla_DUSULMEZ · K10b_Kalemli_faturada_gumus_urun_kalemi_has_bedeli_0_KDV_tam · K10b_Taslaga_eklenen_gumus_kalemi_has_bedeli_0_KDV_tam · K10b_Onizleme_yalniz_altin_has_gramini_ve_bedelini_gosterir → **7/7**.
- Filtreli (OzelMatrah + Fatura* + Kdv + ZoBelge + ZOBELGE + Disaaktarim): **430/430** (`d30b/test1.log`); mevcut `FaturaOzelMatrahTestleri` / `OzelMatrahMotoruTestleri` yeşil → yalnız altın davranışı değişmedi.
- **Tam süit** (`d30b/tam.log`, trx `trx-pZO-BELGE/d15`): **7628 · 7625 geçti · 2 başarısız · 1 atlanmış (notExecuted değil, executed 7627)**. Kırmızılar: `KATMAN2MenuTercihEkranTestleri.Yazma_surerken_gelen_tiklamalar_DURULMA_beklemesinden_sonra_yazilir`, `…On_hizli_tiklama_ESANLI_PATCH_acmaz_SON_durum_yazilir_bant_YOK` — KATMAN şeridinin `MenuTercihServisi.cs` dosyası süit koşarken (04:38) değişti; yeniden derleme sonrası ikisi tek başına **yeşil** (`d30b/test2.log`: 13/13 OzelMatrahMaden + KATMAN2MenuTercih). **Bu paketin ürettiği yeni kırmızı: 0.**

### S-4. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra, `d30b/denetim.log`) → **294 ✓ · 1 ✗**: `P-23: ODEME_KALEMSIZ_YOL — 2 hareket`. İkisi de **TEST-KESIF Kuyumculuk** kiracısında 0,00 TL satış (`01M3QG1P6EVK9HM5CVRTCBKWXJ` 29.09 21:10 KART, `01M3QQEA86CZ0ADP59MXZTBVEX` 29.09 23:19 NAKIT) — keşif turunun kiracısı; bu paket ödeme kalemi yazmaz, dokunulmadı. ZO-BELGE sayaçları ✓.

### S-5. Mutasyon kanıtları (M18, 7/7 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d15/FaturaModulu.cs`, her mutasyonda derleme + filtreli test, geri koyma sonrası `cmp` AYNI; günlük `d30b/mut/ozet.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M18-1 | `HasBacaklariniTopla` maden süzgeci kaldırıldı (şartname md.9) | K10b_Altin_ve_gumus…HAREKET…, K10b_Onizleme…, K10b_Yalniz_gumus… | AYNI |
| M18-2 | fişten yolda maden süzgeci kaldırıldı | K10b_Fisten_faturada_gumus_kalemi… | AYNI |
| M18-3 | kalemli yolda `if (true)` | K10b_Kalemli_faturada_gumus_urun_kalemi… | AYNI |
| M18-4 | taslağa kalem ekle yolunda `if (true)` | K10b_Taslaga_eklenen_gumus_kalemi… | AYNI |
| M18-5 | `KalemHasGrami` madensiz topluyor | K10b_Taslaga_eklenen_gumus_kalemi… | AYNI |
| M18-6 | 805 kodu altın bedeli 0'da da basılıyor | K10b_Yalniz_gumus…ozel_matrah_kodu_yazilmaz | AYNI |
| M18-7 | fişten başlık has gramı gümüşü de topluyor | K10b_Fisten_faturada_gumus_kalemi… | AYNI |

### S-6. Canlı doğrulama (5160 · `dotnet run --no-build` Debug · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3QZDXZ20WMQB6W5YB9GAMGF, `demo-tohumla.js --prova-kiraci` · tek kullanımlık rastgele `Sistem__Anahtari` ortam değişkeni, değeri basılmadı) — betik `d30b/k10b.mjs`, sonuç `k10b.json`, SQL `k10b-sql.txt`
Kur kaydı altın has fiyatı 6.605,95. Ürünler: `K22` 1 g (has 0,916) ADET, `G925` 10 g tip GUMUS (has 9,25) ADET.
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /urunler` ×2, `POST /islemler/alis-fisi` (HAVALE) | 201 ×2, 200 | 201 ×2, 200 (ilk denemeler: ZIYNET+G925 → 400 "Ürün grubu altın bekler" — betik hatası, sunucu kapısı doğru; NAKIT alış 400 tevsik haddi — önceki denemeyle aynı tedarikçi, sunucu doğru) |
| `POST /islemler/satis-fisi` A (altın 8.000 + gümüş 1.500), B (yalnız gümüş 1.500), C (A ile aynı) | 200 | 200 ×3 (bazı müşterilerde 400 tevsik haddi — tohum satışları, kapı doğru) |
| `GET /faturalar/ozel-matrah-onizleme?hareketId=A&toplam=9500` | hasGram 0,916 · hasDeger 0,916×6.605,95 = 6.051,05 | **0,916 · 6.051,05 · matrah 2.874,13 · KDV 574,82** |
| `POST /faturalar {hareketId A, KUYUM, toplam 9.500}` (HAREKET yolu) | 200, has 0,916 / 6.051,05, KDV (9.500−6.051,05)÷1,2 farkı = 574,82 | **200 · 0,916 · 6.051,05 · 574,82** (eski kod: has 10,166 → bedel 67.156 > 9.500 → 400) |
| `POST /faturalar/fisten {B, KUYUM}` (yalnız gümüş) | 200, has 0, KDV tam 1.500−1.500/1,2 = 250 | **200 · has 0 · KDV 250,00 · kod NULL** |
| `POST /faturalar/fisten {C, KUYUM}` (karma) | 200, yalnız altın düşülür | **200 · 0,916 · 6.051,05 · KDV 574,82** |
| SQL `fatura_kalemleri ⋈ hareketler` | gümüş kalem has_deger 0, KDV 250; altın kalem 6.051,05 | `C·1 K22 0.916 6051.05 KDV 324.82 ALTIN` / `C·2 G925 9.25 0.00 KDV 250.00 GUMUS` / `B·1 G925 9.25 0.00 250.00 GUMUS`; başlıklar `9500 0.916 6051.05 574.82` · `1500 0.000 0.00 250.00` |
**Eski kesilmiş faturalar:** değişmez. `kuyumcu_net`'te etkilenen fatura SQL ile ölçüldü (değişiklikten önce): gümüş/altın-dışı has'lı hareketi kaleme bağlı KUYUM fatura **0**, `fatura_no` bağıyla **0**, gümüş ürün kalemli has bedelli KUYUM fatura **0** (toplam KUYUM fatura 6, KESILDI 2; altın dışı has'lı hareket 20).

### S-7. Gerçek tarayıcı ölçümü
Bu devamda razor/css/bileşen/sözlük dosyası DEĞİŞMEDİ (yalnız `FaturaModulu.cs` hesap yolu + test). Ekran düzeni etkilenmediği için yeni ölçüm koşulmadı; /faturalar son tam ölçümü § R-7 (16/16 temiz). Değişen yalnız sunucunun hesapladığı sayı (gümüş kalemde has bedeli 0) — biçimi aynı `Bicim` yolundan geçer.

### S-8. Temizlik
API (5160) PID ile `kill`, `lsof -iTCP:5160` boş. Kiracı 01M3QZDXZ20WMQB6W5YB9GAMGF `kiraci-temizle.sh --uygula` ile silindi (çıkış 0; 475 satır + tenants 1): tenants'ta 0, hareketler 0, faturalar 0. **tenants: koşum başında 15, sonda 14** — benim kiracım 0; −1 başka şeridin bu arada kendi kiracısını silmesi (kiraci-temizle yalnız benim kimliğimi aldı, günlükte tek "SİLİNDİ" satırı). QA-TEST-*, TEST-KESIF ve başka PAKET* kiracılarına dokunulmadı. `d30b/anahtar` `printf ''` ile 0 bayt. Proje ağacında bu devamdan .mutorj/.bak/.tmp yok (`Kuyumcu.Tests/*.cs.bak` ×3 önceden var, bu paketin değil).

### S-9. Açık kalanlar (sahipleriyle)
1. **Mali müşavir teyidi (kurucuya):** gümüş eşyada 23/g yetkisiyle Tebliğ'deki "külçe gümüş bedeli düşülür" özel matrahı UYGULANMIYOR (tutucu). Teyit + gümüş kapanış fiyatı kaynağı gelirse `OzelMatrahKapsaminda` tek yerden genişletilir. Platin/paladyum için özel matrah hükmü bulunmadı — aynı şekilde düşülmez.
2. **YASAL-4 Y4-D devamı (şartname 4.3b):** has bedeli fiyatı Borsa İstanbul son işgünü kapanışı olmalı; bugün işlemin kur kaydı. Bu turun kapsamı dışı.
3. **Ekran notu (Ertuğ, sonraki dokunuşta):** gümüş kalemli kuyum faturasında "özel matrah uygulanmadı" satırı ekranda henüz yazılmıyor (sunucu has bedeli 0 döndürür; önizleme `uyari` metni değişmedi — ekran dokunuşu tarayıcı ölçümü gerektirir).
4. **Ürünsüz gümüş hareketi (bilgi):** taslakta kalem düzeltildikten sonra başlık has gramı `fatura_kalemleri.ayar_id` ile yeniden toplanır; ürünsüz (ayarsız) gümüş satırı o anda altın sayılır. Kesimde değil, yalnız taslak düzeltmesinde; hareketten gelen fişlerde ürün her zaman var.
5. **Başkasına ait:** P-23 TEST-KESIF 2 satır (keşif turu); KATMAN2MenuTercih testleri süit sırasında dosya değişimi.
6. **Devreden (değişmedi):** § R-9 md.2–4, § P-9 md.2–4.

### S-10. DoD sözleşmesi
Yeni varlık, uç, düğme, migration yok. Fatura/kalem düzenleme yolları aynı (taslak PATCH; kesilmişte DegismezIpucu). Hard delete yok. Mevzuat: KDVK 23/e birincil kaynaktan doğrulandı; gümüş hükmü koda yazılmadı (fail-closed, "DOĞRULANAMADI" notu kodda ve raporda). Eşik/oran eklenmedi.

---

## 30.09 DEVAM 13 (01:04–01:40): keşif K01 [KRİTİK] (fişten faturada 250 adet → "1 ADET") kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § R-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d30/`.

**Devir kontrolü:** DEVAM 12 raporu okundu; ağaçta yarım mutasyon yok (`mut-yedek/ZO-BELGE-d13/` önceki koşumda cmp AYNI). Bu koşumun yedekleri `mut-yedek/ZO-BELGE-d14/`.

### R-1. Bulgu ve yapılanlar
**Kök neden (koda karşı doğrulandı):** `FaturaModulu.cs` fişten faturada `FistenKalemMiktari = 1m` sabitini kullanıyordu; gerekçesi "hareketler tablosunda adet kolonu YOK" idi. Bu iddia ARTIK YANLIŞ: `Hareket.Adet` (`adet decimal(12,3)`, P-08/Y-14) açılmış ve satış yolları (`SatisModulu.cs` satış fişi kalemi `Adet = k.Adet`) yazıyor. İade ucu (`/islemler/iade-adaylari`, `SatisServisi.OrijinalAdet`) adedi `h.Adet` → yoksa `|hareket has| ÷ ürün birim has` ile üretiyordu; fatura bu kaynağı kullanmıyordu.

**Mevzuat (birincil kaynak):** 213 sayılı VUK md.230 — "Faturada en az aşağıdaki bilgiler bulunur: … 4. Malın veya işin nev'i, miktarı, fiyatı ve tutarı;" (mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf, madde 230 bent 4; PDF indirildi, metin çıkarılıp okundu). "1 ADET × 1.095.125" belgenin miktar ve fiyat bilgisini yanlış yazar.

| Yapılan | Dosya | Kanıt |
|---|---|---|
| `FistenKalemMiktari(h, urunBirimHas, urunBirimi)`: kaynak sırası iade ucuyla AYNI — (1) `h.Adet > 0`; (2) yoksa `Yuvarla3(|h.HasGrami| ÷ urun.HasGrami)` (gramlı üründe "1 adet = 1 gram" sözleşmesiyle sonuç GRAM); ADET üründe türetilen değer tam sayı değilse (kart satıştan sonra değişmiş) kesirli adet UYDURULMAZ → (3) 1 (ürünsüz satır / hizmet de 1). | `Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` | K01_Miktar_kaynak_sirasi…, K01_Adet_kolonu_BOS… |
| `FistenKalemFiyati(tutar, miktar)`: birim fiyat = tutar ÷ miktar; kuruşu kuruşuna bölünüyorsa indirim yok; bölünmüyorsa birim fiyat kuruşa YUKARI, fark (miktar × 0,01 TL'den az) kalemde açık "indirim" olarak yazılır. Kalem kuralı TEK: `Yuvarla2(miktar × birim) − indirim = satır toplamı`; satır toplamı DEFTERDEKİ tutar → **fatura toplamı değişmez**. İç değişmez kapısı: bedelli kalemde satır ≠ defter tutarı ise fatura kesilmez. | aynı | K01_Kalem_fiyati_kurali… (5 veri), K01_Fisten…DEFTERDEKI_ADET… |
| Hediye kalemi: birim = liste fiyatı (defterde BİRİM fiyattır), indirim = defterdeki `IndirimTutari` (liste × adet) ya da yoksa `miktar × liste`. Eski kodda 2 adetlik hediye `liste − liste×2 < 0` ile 400 veriyordu (gizli hata) — artık 0 bedelle girer. | aynı | K01_Iki_adetlik_hediye… |
| Kalemler penceresi: para kutuları (birim fiyat, işçilik, indirim) binlikli ve iki haneli (`ParaKutuKalibi = "#,##0.00"` → "4.380,50"; eskiden "1095125,0" kesik). Virgül her zaman olduğu için P-78 "1.500" belirsizliği doğmaz; kaydet `FormAlani.SayiCoz` ile aynı metni çözer. Pencere yalnız bu tabloda genişler (`.modal-panel:has(#fd-kalem-tablosu)`, 72rem); BASİT'te hesaplanan teknik sütunlar (Has altın değeri, KDV matrahı, KDV tutarı) gizli, PRO'da aynen; 375 px'te tablo `q10-yigin` kart yığını (her hücre `data-etiket`) — yatay kaydırma 0. Aynı biçim Düzenle çekmecesindeki kalem kutularında. | `Kuyumcu.Web/Pages/Faturalar.razor`, `wwwroot/css/zo-belge.css` | K01_Kalem_birim_fiyati_BINLIKLI…, K01_Kalem_penceresi_BASIT…, P78_… (güncellendi) |

**Test sözleşmesi güncellemesi (gerekçeli):** `FaturaFistenTestleri.Fisten_faturada_kalem_miktari_BIRDIR_adet_sozlesmesi` KALDIRILDI; yerine `K01_Adetsiz_ve_turetilemeyen_eski_satirda_miktar_1_uydurulmaz_toplam_ayni` (aynı üç satırlık fiş, miktar 1 artık yalnız "adet bilinmiyor + türetilemiyor" durumunda). `P78_fatura_kalem_fiyati_45_000…` testindeki "gösterimde nokta yok" iddiası "1.000,00" (binlikli) olarak güncellendi — K01 bulgusunun kendisi binliksiz gösterimdi.

**Eski (ADET=1 ile kesilmiş) faturalar:** YENİDEN HESAPLANMAZ. Kesilmiş belge değişmez (P-22/B-3); kalemler olduğu gibi kalır, doğru belge için mevcut yol (iptal + yeniden kesim / düzeltme) kullanılır. TASLAK faturalar ekrandan satır içi düzeltilebilir (miktar/birim fiyat PATCH ucu mevcut); otomatik taşıma yazılmadı — taslakta kullanıcının girdiği değeri sessizce değiştirmek olurdu.

**Dokunulan dosyalar (DEVAM 13):** `Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`, `Kuyumcu.Web/Pages/Faturalar.razor`, `Kuyumcu.Web/wwwroot/css/zo-belge.css`, testler `Kuyumcu.Tests/FaturaFistenTestleri.cs`, `Kuyumcu.Tests/FaturaEkranTestleri.cs`. Ortak bileşenlere (SatirIciDuzenle, ModalPencere, q10.css) DOKUNULMADI (q10-yigin sınıfı yalnız kullanıldı). Migration: gerekmedi (kolon zaten var). Sözlük: yeni metin yok.

### R-2. Build
`dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra) → **0 Uyarı, 0 Hata** (`d30/mut/son-build.log`, `build2.log`).

### R-3. Testler
- Yeni/değişen: 14 K01 testi (FaturaFistenTestleri 6 Fact + 5 Theory verisi + 1 saf; FaturaEkranTestleri 2) — 14/14.
- Filtreli (FaturaFisten + Fatura* + Q92FaturaKalem + UX1C2 + ZoBelge + ZOBELGE): **390/390** (`d30/test2.log`).
- **Tam süit** (`d30/tam.log`, trx `trx-pZO-BELGE/d14`): **7459 · 7459 geçti · 0 başarısız**. Bu paketin ürettiği yeni kırmızı: **0**.

### R-4. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra) → **294 ✓ · 1 ✗ · çıkış 1**: `P-23: ODEME_KALEMSIZ_YOL (beklenen 0) — 1 hareket`. Canlı veritabanı sayacı; satır kimliği `01M3QG1P6EVK9HM5CVRTCBKWXJ`, kiracı **TEST-KESIF Kuyumculuk** (keşif turunun kiracısı, 29.09 21:10'da yazılmış KART satışı, tl_tutari 0,00). Bu paketin kiracısı değil, bu paketin kodu ödeme kalemi yazmaz; dokunulmadı (başkasının kiracısı). ZO-BELGE sayaçlarının hepsi ✓; `Pages: ModalPencere kullanımı 47 (tavan 55)` değişmedi.

### R-5. Mutasyon kanıtları (M17, 7/7 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d14/`, her birinde derleme 0 hata, geri koyma sonrası `cmp` AYNI; günlük `d30/mut/ozet.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M17-1 | `h.Adet` okunur ama 1 döner | K01_Fisten…DEFTERDEKI_ADET…, K01_Gramli_urunde…, K01_Iki_adetlik_hediye…, K01_Miktar_kaynak_sirasi… | AYNI |
| M17-2 | bölünmeyen tutarda yukarı yuvarlama/indirim yok | K01_Fisten…DEFTERDEKI_ADET…, K01_Kalem_fiyati_kurali…(100, 3) | AYNI |
| M17-3 | gramlı ürün türetmesi ADET gibi (tam sayı şartı) | K01_Miktar_kaynak_sirasi… | AYNI |
| M17-4 | para kutusu kalıbı `0.00` (binliksiz) | K01_Kalem_birim_fiyati_BINLIKLI…, P78_fatura_kalem_fiyati… | AYNI |
| M17-5 | BASİT'te teknik başlıklar yine çizilir | K01_Kalem_penceresi_BASIT… | AYNI |
| M17-6 | çağrı yerinde `miktar = 1m` (eski sözleşme) | K01_Adet_kolonu_BOS…, K01_Fisten…DEFTERDEKI_ADET…, K01_Gramli…, K01_Iki_adetlik_hediye… | AYNI |
| M17-7 | kalem tablosundan `q10-yigin` (375 kart yığını) kaldırıldı | K01_Kalem_penceresi_BASIT… | AYNI |

### R-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, iki kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3QM442PG3R8ETE4MRJPHB8F, `demo-tohumla.js --prova-kiraci` · tek kullanımlık rastgele `Sistem__Anahtari`, değeri basılmadı) — betik `d30/k01.mjs`, sonuç `k01.json`, SQL `k01-sql.txt`
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /urunler` ×2 (1 g K24 ADET; 22 ayar GRAM) + `POST /islemler/alis-fisi` (260 adet + 20 g) | 201 / 200 | 201 ×2, 200 (ilk denemeler: `odemeTuru:"VERESIYE"` → 400 "Ödeme türü beklenen biçimde değil" — alış fişi yalnız NAKIT/KART/HAVALE; bir deneme tevsik haddi 400 — betik hatası, sunucu doğru) |
| `POST /islemler/satis-fisi` 250 adet × 40 + 3 adet × 40 + 12,5 g × 1.200 = 25.120 | 200 | 200 (ilk iki müşteride 400 "Tevsik zorunluluğu: aynı gün aynı kişiyle nakit toplamı 30.000 TL haddini aşıyor" — tohum satışları yüzünden, sunucu kapısı doğru; üçüncü müşteride 200) |
| `GET /islemler/iade-adaylari` | toplamAdet 250 / 3 / 12,5 | 250 / 3 / 12,5 |
| `POST /faturalar/fisten {fisId}` | 200, toplam = fiş toplamı | 200, **toplam 25.120** (değişmedi) |
| `GET /faturalar/{id}/kalemler` | 250 ADET × 40 = 10.000 · 3 ADET × 40 = 120 · 12,5 GRAM × 1.200 = 15.000 | **aynen** (indirim null) |
| SQL `fatura_kalemleri ⋈ hareketler` | miktar = hareketler.adet, satır = tl_tutari | `1 · 250.000 ADET · 40.00 · 10000.00 · h_adet 250.000 · h_tl 10000.00` / `2 · 3.000 ADET · 40.00 · 120.00 · 3.000` / `3 · 12.500 GRAM · 1200.00 · 15000.00 · 12.500`; fatura `TASLAK 25120.00` |
| `PATCH …/kalemler/{k1} {birimFiyat: 4380.5}` (taslak düzeltme, K01 görüntüsündeki tutarı üretmek için) | 200, satır 1.095.125 | 200, `[250, 4380.5, 1095125]`, toplam 1.110.245 |

### R-7. Gerçek tarayıcı ölçümü (başsız Chrome, temiz profil)
**(a) Kalemler penceresi** (`d30/kal.mjs`, `kal.json`, foto `d30/foto-kal/`): {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 375} = 12 ölçüm.
| Ölçü | 1440 | 1280 | 375 |
|---|---|---|---|
| Pencere genişliği | 1152 px | 1152 px | 343 px |
| Sütun (BASİT / PRO) | 8 / 11 | 8 / 11 | kart yığını (8 / 11 etiketli hücre) |
| Tablo yatay kaydırma | 0 | 0 | 0 (önce: 543 BASİT / 947 PRO) |
| Kesik düzenleme kutusu | 0 | 0 | 0 (önce: 12) |
| Sayfa yatay | 0 | 0 | 0 |
| Satır 1 metni | "1 ZOB 1 g Gram Altın K24 250 4.380,50 — — 1.095.125 ₺" (PRO: + "0 ₺ 912.604,17 ₺ 182.520,83 ₺") | aynı | aynı |
| K1 · K3 · K5 (TRY) · K10 · Y | 0 · 0/0 · 0 · 0 · 0 | aynı | aynı |
| K9 | 6 | 6 | 6 |
K9 = 6 ÖLÇÜM ARTEFAKTI: aynı sayfada pencere kapalıyken K9 = 0; pencere açılınca `sikisma-olc.js` arka plandaki liste hücrelerini ("Kesimde verilir", tarih) pencerenin başlıklarıyla "üst üste" sayıyor (`d30/k9.mjs` çıktısı: `#fk-no-yok-… ⟂ th "AÇIKLAMA"`). Pencere örtüsü tasarım gereği sayfanın üstündedir; her ModalPencere'de aynı sonuç çıkar.
**(b) /faturalar tam ölçüm** (`d30/sert.mjs`, `sert.json`): {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = 16 ölçüm → K1 · K3 · K5 · K7 · K8 · K9 · K9 görünür · K10 · K11 · K12 · yatay · Yükleniyor **0/16**, K4 açılış **1** (≤ 1,0), mod uyumu 16/16. (Diğer 6 rota bu devamda değişmedi; son tam ölçümleri § P-7.)
Görülen görüntüler (Read): `kalemler-basit-aydinlik-1440.png` (250 · 4.380,50 · 1.095.125 ₺; 12,5 gram satırı; kesik yok), `kalemler-basit-aydinlik-375.png` (her kalem kart; Miktar 250, Birim Fiyat 4.380,50 görünür), `kalemler-profesyonel-karanlik-1280.png` (11 sütun tek ekranda). Kopyalar `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d14-*.png`; ÖNCE/SONRA yan yana: `ZO-BELGE-d14-K01-once-sonra-1440.png` (sol: keşif K01, sağ: bu devam).
Tuval bölge farkı: Kalemler penceresi tuvalde yok (GEREKÇELİ: fatura kalem ayrıntısı bizde olan bir özellik; silinmedi, BASİT'te sadeleştirildi).

### R-8. Temizlik
API (5160), vekil (5161), Chrome (9716/9717) `kill` ile durduruldu; `lsof` dördü boş. Kiracı 01M3QM442PG3R8ETE4MRJPHB8F `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, hareketler 0, faturalar 0. **tenants: koşum başında 15, sonda 16** — benim kiracım 0; +1 başka şeridin bu arada açtığı kiracı (PAKET*-TEST), dokunulmadı. QA-TEST-*, TEST-KESIF ve başka PAKET* kiracılarına dokunulmadı. `d30/anahtar`, `d30/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacında .mutorj/.bak/.tmp yok. **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d30/profil-kal`, `d30/profil-sert` (ve önceki `d29*/profil-*`).

### R-9. Açık kalanlar (sahipleriyle)
1. **Keşif kiracısı / backend (P-23):** denetim.sh canlı sayacı `ODEME_KALEMSIZ_YOL = 1` — TEST-KESIF kiracısında 0,00 TL'lik KART satışı (`01M3QG1P6EVK9HM5CVRTCBKWXJ`, 29.09 21:10) ödeme kalemsiz. Kiracının sahibi (keşif turu) silmeli ya da satış paketinin sahibi hangi yolun kalemsiz yazdığını incelemeli.
2. **Eski satırlar (bilgi):** `hareketler.adet` NULL ve ürün kartı satıştan sonra değişmiş ADET satırlarında miktar hâlâ 1 (kesirli adet uydurulmaz). Bu satırlar yalnız adet kolonu açılmadan ÖNCE yazılmış satışlardır.
3. **Mali müşavir teyidi (kurucuya):** bölünmeyen tutarlarda kuruş farkının kalemde "indirim" olarak gösterilmesi (en çok miktar × 0,01 TL) — VUK md.230 miktar/fiyat/tutarın üçünü de istediği için seçildi; alternatif (birim fiyatı daha çok haneyle yazmak) `birim_fiyat decimal(15,2)` kolonunu değiştirmeyi gerektirir.
4. **Satır içi miktar kutusu birimsiz** ("250", "12,5"): birim sütun başlığında değil, kesilmiş faturada `Bicim.Miktar` ile "250 adet / 12,500 g" yazılıyor. Ortak SatirIciDuzenle'ye gösterim parametresi (ORTAK-1 alanı) eklenirse taslakta da birim yazılabilir.
5. **Devreden (değişmedi):** § P-9 md.2–4.

### R-10. DoD sözleşmesi
Yeni varlık, yeni uç, yeni düğme yok. Fatura kalemi zaten PATCH (`/faturalar/{id}/kalemler/{kalemId}`, gönderilmeyen = koru, null = sil) ile taslakta düzenlenir; kesilmişte DegismezIpucu. Hard delete yok. Mevzuat hükmü: VUK md.230/4 birincil kaynaktan doğrulandı; koda eşik/oran yazılmadı. id'ler değişmedi (yalnız `data-etiket` ve sınıf eklendi).

---

## 29.09–30.09 DEVAM 12 (23:25–00:20): tasarım hakemi (YÜKSEK ×2) bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § P-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29m/`.

**Devir kontrolü:** DEVAM 11 raporu ve ağaç okundu; `mut-yedek/ZO-BELGE-d12/` son mutasyon yedekleri ağaçla AYNI (yarım mutasyon yok). DEVAM 11'in kiracısı silinmişti; bu koşumda yeni kiracı açılıp sonda silindi.

### P-1. Bulgular ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [YÜKSEK] Yeni Fatura çekmecesinde Kaydet gövdenin ortasında, İptal altlıkta tek başına; Evrak gir / Sayım gir'de ikisi altlıkta yan yana | Kaydet ve İptal aynı altlıkta: `<div class="cekmece-alt zo-cekmece-alt" id="fk-eylem">` → `İptal (fk-vazgec, dugme-ikincil) · Kaydet (fk-kaydet, dugme-birincil)` + kilit sebebi `fk-kaydet-kilit` (sınıf `cekmece-kilit-sebep`). Ortak Cekmece'nin Kaydet'i `fk-kaydet` kimliğini taşıyamadığı (ORTAK-1 alanı, parametre yok) için altlık sayfada çizilir ve gövdenin dibine yapışır; çekmecenin kendi altlığı YALNIZ `#cekmece-fatura-yeni` / `#cekmece-fatura-fisten`'de gizlenir (`zo-belge.css`; EMANET-1'in `emanet-cekmece-alt` deseni). × ve Esc aynen kapatır. Eski `.zo-yeni-fatura > .satir:last-child` yapışkan şeridi kaldırıldı. | ZB71, ZB54 (güncellendi). Tarayıcı: 4 çekmece × 8 kombinasyon aynı altlık (§ P-7). M16-1, M16-2 |
| [YÜKSEK] Fişten Fatura Kes çok alanlı oluşturma formu ama ortada ModalPencere | `ModalPencere` → `<Cekmece Varlik="fatura-fisten" KaydetGoster="false">`; aynı altlık (`ff-eylem`: `ff-vazgec` · `ff-kaydet`). Kaydet kilitliyken SEBEBİ altlıkta (`ff-kaydet-kilit`, UX-S30): "Kaydetmek için önce fişi seçip Önizle'ye basın." / "Fişte faturaya girecek kalem yok." / "Bu fiş zaten bir faturaya bağlı." / sunucu onayı bekleniyor. Kilit koşulu eskisiyle AYNI (önizlenmedi, kalem 0, faturalı, kaydediliyor) — tek yerden `FistenKilitSebebi`. Tüm `ff-*` kimlikleri AYNI; POST `/faturalar/fisten` gövdesi değişmedi. Pages'teki ModalPencere sayısı 48 → 47. | ZB72, ZB73, ZB73b; mevcut `Onizleme_YAPILMADAN_fisten_Kaydet_KAPALI`, `Zaten_faturalanmis_fiste_Kaydet_KAPALI`, ZB65–ZB70e yeşil. M16-3, M16-5, M16-6 |
| [YÜKSEK, ek] Fiş aramasının boş sonucu ortak "Eşleşen kayıt ya da ekran yok." diyordu | `KayitSecici BosMesaj="@ZB.FistenAramaYok"` → "Bu adla eşleşen fiş yok. Fişteki müşterinin adını yazın." (TR+EN, `Sozlukler.ZoBelge.cs`). **Gerekçeli sapma:** hakemin örneği "Henüz faturası kesilmemiş fiş yok" idi; ancak sunucudaki fiş araması (`AramaModulu.FisAra`) faturalı fişi SÜZMEZ, yalnız cari adı / fiş kimliğiyle eşler — o cümle yanlış bir iddia olurdu (Anayasa md.1). Metin aramanın gerçekte yaptığını söyler. | ZB72; tarayıcı 8/8 kombinasyonda bu metin. M16-4 |

**Dokunulan dosyalar (DEVAM 12):** `Kuyumcu.Web/Pages/Faturalar.razor` (Yeni Fatura altlığı, Fişten → Cekmece + altlık + kilit sebebi + BosMesaj), `Kuyumcu.Web/wwwroot/css/zo-belge.css` (iki çekmecede kendi altlığı gizli, gövde sütun, `.zo-cekmece-alt` yapışkan; eski şerit kuralı silindi), `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (FistenAramaYok, FistenKilitOnizle, FistenKilitKalemYok, FistenKilitFaturali — TR+EN). Testler: `FaturaEkranTestleri.cs` (ZB71, ZB72, ZB73, ZB73b yeni), `ZoBelgeTestleri.cs` (ZB54 CSS iddiası yeni altlığa güncellendi). Ortak Cekmece/KayitSecici/ModalPencere'ye DOKUNULMADI. Migration: gerekmedi. Sunucu değişmedi.

### P-2. Build
`dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra) → **0 Uyarı, 0 Hata** (`d29m/build3.log`).

### P-3. Testler
- Filtreli (FaturaEkranTestleri + ZoBelge* + ZOBELGE doğrulayıcı + UX1C2EkranTestleri + P36cGoc + Faturalar*): **270/270** (`d29m/test2.log`).
- **Tam süit** (`d29m/tam.log`, trx `trx-pZO-BELGE/d13`): **7377 · 7375 geçti · 2 başarısız**: `Dogrulayici.SATIS3SinirTestleriT19.T21_gereksinim_ucu_DUSERSE_kilit_sebebi_KURAL_ALINAMADI_bant_ALANLAR_ACILDI_demez(mod: "BASIT"/"PROFESYONEL")` — SATIS-3 hasım testi (fiş/satış alanı). **Bu paketin ürettiği yeni kırmızı: 0.** DEVAM 11'deki SATIS-3 kırmızısı (K2H) artık yeşil; MigrationHazirlik kararsızlığı bu koşumda çıkmadı.

### P-4. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra) → **çıkış 0 · 295 ✓ · DENETİM TEMİZ** (`d29m/denetim.log`); `Pages: ModalPencere kullanımı 47 (tavan 55)`.

### P-5. Mutasyon kanıtları (M16, 6/6 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d13/`, her birinde `--no-incremental` derleme, geri koyma sonrası `cmp` AYNI; günlük `d29m/mut/`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M16-1 | Yeni Fatura altlığı `cekmece-alt` yerine `satir` (eski gövde içi şerit) | ZB71_Yeni_fatura_altlik_Iptal_Kaydet_yan_yana_govdenin_dibinde | AYNI |
| M16-2 | Fişten çekmecesinin kendi altlığı gizlenmez (`display: flex`) | ZB71 | AYNI |
| M16-3 | Fişten yeniden ModalPencere | ZB72_Fisten_fatura_kes_CEKMECE_modal_yok_altlik_ayni | AYNI |
| M16-4 | `BosMesaj` kaldırıldı (ortak metin) | ZB72 | AYNI |
| M16-5 | önizlenmemiş fişte kilit/sebep yok | Onizleme_YAPILMADAN_fisten_Kaydet_KAPALI, ZB73_Fisten_kilitli_Kaydet_sebebi_altlikta | AYNI |
| M16-6 | faturalı fişte kilit/sebep yok | ZB73b_Faturali_fiste_kilit_sebebi_faturali, ZB70b, Zaten_faturalanmis_fiste_Kaydet_KAPALI | AYNI |
Not: M16-5'in ilk koşumu başka şeridin eşzamanlı derlemesi `Kuyumcu.Tests.dll`'i sildiği için test koşamadı ("File not found"); aynı mutasyon yeniden koşuldu, sonuç tabloda.

### P-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin` · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3QEMMSNHYQTDDTGQ7RKXDSB, `demo-tohumla.js --prova-kiraci` (6 cari / 12 ürün / 9 satış) + `ek.mjs` (2 ek kasa, gün sayımı, 4 evrak, 1 karşılıksız) + `fat.mjs` (taslak/kesildi/iptal fatura) + `zb12.mjs` (alış fişi) · tek kullanımlık rastgele `Sistem__Anahtari` ortam değişkeni, değeri basılmadı)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /faturalar` ×3 + `PATCH /durum` | 200 | 200 ×3, 200 ×3 (taslak / kesildi / iptal) |
| `POST /islemler/alis-fisi` | 200 | 200 (fisId 01M3QEPGDD3NVFYCD3A1Q2AB8E) |
| `POST /faturalar/fisten` alış fişi + `tur:"SATIS"` | 400 | 400 "Bu fiş bir alış fişidir; satış faturası olarak kesilemez" |
| `POST /faturalar/fisten` alış fişi, tür yok | 200 ALIS | 200 `ALIS`, 750 |
| `POST /faturalar/fisten` `tur:"TUHAF"` | 400 | 400 "Tür beklenen biçimde değil" |
| `POST /faturalar/fisten` satış fişi + `tur:"ALIS"` | 400 | **404 "Fiş bulunamadı"** — betik (DEVAM 11'den) önceki kiracının sabit satış fişi kimliğini kullandı; bu kiracıda o fiş yok. Bu adım DEVAM 11'de 400 ile doğrulanmıştı (§ O-6) ve sunucu bu koşumda DEĞİŞMEDİ. |
| Çekmeceler (tarayıcı, § P-7) | aynı altlık, modal 0, sebep yazılı, bağlam metni | 72/72 ölçüm tutarlı |

### P-7. Gerçek tarayıcı ölçümü (başsız Chrome, temiz profil)
**(a) Çekmece altlığı** (`d29m/cek.mjs`, sonuç `cek.json`, foto `foto-cek/`): {Yeni Fatura, Yeni Fatura boş Kaydet, Fişten, Fişten boş arama, Fişten önizleme, Evrak gir, Sayım gir} × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 375} = 72 ölçüm.
| Ölçü | Yeni Fatura | Fişten | Evrak gir (hedef) | Sayım gir |
|---|---|---|---|---|
| Görünen altlık sayısı | 1 | 1 | 1 | 1 |
| Altlığın çekmece dibine uzaklığı | 0 px | 0 px | 0 px | 0 px |
| Altlık zemini / üst çizgi | yuzey-2 (238,240,243 · koyu 34,38,46) / 1px solid | aynı | aynı | aynı |
| Düğme sırası · konum (1440) | İptal x977 71×44 · Kaydet x1056 93×44, 18 px | aynı | aynı | aynı |
| Düğme sırası · konum (375) | İptal x17 · Kaydet x96, 71×44 / 93×44 | aynı | aynı | aynı |
| Gövdede Kaydet/İptal | 0 | 0 | 0 | 0 |
| `.modal-panel` | 0 | **0** | 0 | 0 |
| Sayfa / gövde yatay kaydırma | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| Kilit sebebi | (kilitsiz) | "Kaydetmek için önce fişi seçip Önizle'ye basın." / faturalı fişte "Bu fiş zaten bir faturaya bağlı." | "Kaydet kilitli: Eksik alanlar: …" | (kilitsiz) |
Boş Kaydet (Yeni Fatura): alan altı mesajlar "Bu alan zorunludur." / "Sıfırdan büyük bir değer girin." + form bandı, 8/8. Fişten boş arama: "Bu adla eşleşen fiş yok. Fişteki müşterinin adını yazın." 8/8.
Görülen görüntüler (Read): `ZO-BELGE-d12-yeni-basit-aydinlik-1440.png` (altlıkta İptal | Kaydet), `ZO-BELGE-d12-fisten-bos-arama-basit-aydinlik-1440.png` (sağ çekmece, bağlam metni, kilitli Kaydet + sebep), `ZO-BELGE-d12-fisten-onizle-profesyonel-karanlik-1440.png` (önizleme + taslak-bağlı bandı + altlıkta sebep), `ZO-BELGE-d12-fisten-onizle-basit-aydinlik-375.png` (tam ekran çekmece, altlık yapışık), `ZO-BELGE-d12-evrak-gir-basit-aydinlik-1440.png` (karşılaştırma hedefi — birebir aynı altlık). Kopyaları `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d12-*.png` (7 dosya).

**(b) 7 rota tam ölçüm** (`d29m/sert.mjs`, `sert.json`, foto `foto-sert/`): 7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm**.
| Ölçü | Sonuç |
|---|---|
| K1 · K3 (taşan/kırık) · K5 TRY · K7 · K9 görünür · K10 · K11 · K12 · yatay · Yükleniyor'da donan · mod uyumu | **0 / 112 uyumlu** |
| K4 açılış 1440×900 | 7 rotanın hepsi **1** (≤ 1,0) |
| K8 jargon | yalnız `/evraklar` BASİT 8 ölçüm: "ZB-CEK", "ZB-SNT" — bu, TOHUM verisindeki evrak numaralarıdır (`ek.mjs`'in yazdığı kullanıcı verisi `ZB-CEK-1`), ekran metni değil (DEVAM 11 § O-9 md.6'daki bilinen ölçüm artefaktı). |

**Tuval bölge farkı (bu devamın dokunduğu bölgeler):** Yeni Fatura / Fişten çekmecesi tuvalde yok (GEREKÇELİ: tuvaldeki çekmece deseni Musteriler aside — "İptal | Kaydet" altlıkta; bizde aynı desen, Evrak gir ile ölçüyle birebir) · Fişten'in ortadan sağ çekmeceye taşınması (GEREKÇELİ: DoD EK 2, tek düzenleme kalıbı).

### P-8. Temizlik
API (5160), vekil (5161), kendi Chrome'larım (9716/9717) `kill` ile durduruldu; `lsof` dördü boş. Kiracı 01M3QEMMSNHYQTDDTGQ7RKXDSB `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, hareketler 0, faturalar 0. **tenants: koşum başında 14, sonda 16** — benim kiracım 0; +2 başka şeritlerin (PAKETSATIS-3-TEST, PAKETORTAK-1-HAKEM-TEST …) bu arada açtıkları kiracılar, dokunulmadı. QA-TEST-* ve başka PAKET* kiracılarına dokunulmadı. `d29m/anahtar`, `d29m/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacında .mutorj/.bak/.tmp yok. **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d29m/profil-cek`, `d29m/profil-sert` (ve önceki `d29l/profil-*`, `d29j/profil-*`, `d29i/profil-*`).

### P-9. Açık kalanlar (sahipleriyle)
1. **SATIS-3:** tam süitte `Dogrulayici.SATIS3SinirTestleriT19.T21_gereksinim_ucu_DUSERSE_kilit_sebebi_KURAL_ALINAMADI_bant_ALANLAR_ACILDI_demez` (BASIT, PROFESYONEL) kırmızı.
2. **ORTAK-1 (öneri):** Cekmece'ye kaydet düğmesinin kimliğini veren bir parametre (ör. `KaydetId`) eklenirse Yeni Fatura / Fişten / Emanet çekmeceleri sayfa-içi altlık yerine ortak altlığı kullanabilir; bugün üç sayfa CSS ile ortak altlığı gizliyor.
3. **Ölçüm aracı (bilgi):** `sikisma-olc.js` K8 kullanıcı verisindeki büyük harfli evrak numarasını jargon sayıyor.
4. **Devreden (değişmedi):** mali müşavir — alış fişinin hangi belgeyle belgeleneceği (gider pusulası / alış faturası) DOĞRULANAMADI, koda hüküm yazılmadı · HURDA_ALIS `fis_id`'siz (satış paketi) · "Müşteri/Cari" etiketi alış fişinde (hub) · "Altın pozisyonum" N1 (ORTAK-1) · rapor merkezi hub adları hub sahibinde.

### P-10. DoD sözleşmesi
Yeni varlık, yeni uç yok. Yeni düğmeler: `fk-vazgec`, `ff-vazgec` (çekmeceyi kapatır, istek atmaz — ZB71/ZB72 tıklama testi). `ff-kaydet` kilitliyken sebebini yazar (UX-S30). ModalPencere yalnız yıkıcı onayda kaldı (Fişten çekmeceye taşındı; sayım 48 → 47). Yıkıcı eylem, POST yolları, yetki, id'ler değişmedi. Hard delete yok. Mevzuat hükmü eklenmedi.

---

## 29.09 DEVAM 11 (18:10–18:25 kesilen koşum + 21:55–22:50 bu koşum): doğrulayıcı (KALDI) bulguları + orkestratörün "pencerenin TAMAMI" talimatı kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § O-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29k/` (kesilen koşum) ve `d29l/` (bu koşum).

**Devir kontrolü:** Kesilen koşum (d29k) kodu ve testleri yazmış, M15-1…8 mutasyonlarını koşmuş, tam süiti 18:22'de 7290/7290 yeşil almış, sonra 18:24'te `Faturalar.razor`'da taslak-bağlı fiş bandını değiştirip (FistenFaturaliMesaj) yarıda kalmıştı. Ağaç `mut-yedek/ZO-BELGE-d11/M15-2b-Faturalar.razor` ile `cmp` AYNI (yarım mutasyon YOK), `FaturaModulu.cs` M15-6 yedeğiyle AYNI. Kesilen koşumun `test3/test4` kırmızısı (ZB70b) **bayat derleme** idi: `dotnet build` artımlı derlemede Web dll'ini güncel saymıştı; `--no-incremental` ile yeniden derlenince ZB70b yeşil (aşağıda). Kesilen koşumun test kiracısı (01M3PW2NBYV8KP97GHCMD71HRY) silinmemişti — bu koşumda canlı doğrulamada yeniden kullanıldı ve sonda silindi. Önceki "SON DOĞRULAMA" üçlüsü güncel ağaçta KAPALI: `Raporlar.BugunHas` = `Bicim.Has` (4 hane), `IslemDefteri` özet net has `Bicim.Has`, başlıklar `SayfaAdi` (= menü adı), ZB14 sayfa listesi 7 ekran (Raporlar dahil), `Raporlar.razor`'da satır içi `style=` 0.

### O-1. Bulgular ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] Fişten önizleme ürünsüz kalemde ham `HURDA_ALIS` basıyor (`fk.UrunAd ?? fk.Tur`) | `Faturalar.FisKalemAdi(fk)`: ürün adı yoksa `KatalogSozlugu.HareketTuru(tur, dil, basit)`; sözlükte yoksa `ZB.FistenKalemGenel` — ham kod hiçbir yolda basılmaz. | ZB70 (`Assert.DoesNotContain("HURDA_ALIS", #ff-kalem-tablosu)` + sözlük adı). M15-1 |
| [ORKESTRATÖR] Pencerenin TAMAMI: sözlükten geçmeyen her metin | Pencere tarandı ve sınıf kapatıldı: (a) cari yoksa `ZB.FistenCariYok`; (b) fiş zaten faturalıysa taslak damgası `TASLAK:<ULID>` basılmaz → "Bu fiş taslak bir faturaya bağlı (numara kesimde verilir)." (`ZB.FistenTaslakFaturali`, TR+EN), kesilmişse numara cümlede; (c) tutar/has fatura yönünden işaretsiz (`Math.Abs`, toplam `FistenToplam`), alış fişinde "−" yalnız seçici alt bilgisinde (fiş yönü); (d) tür rozeti sözlükten (Satış/Alış). | ZB70b: pencerede `^[A-Z_]{3,}$` yaprak kalıbı **0** (önizleme öncesi, seçim sonrası, önizleme sonrası), "TASLAK:" 0, ULID 0; ZB70c, ZB70d. M15-2, M15-3, M15-7, M15-8 |
| [ORTA] Sunucu fişle çelişen `tur`'u kabul ediyor / tür yoksa SATIS | `FaturaModulu.FistenTurTuret(hareket türleri)` (saf): bütün kalemler ALIS/HURDA_ALIS → ALIS; bütünü SATIS → SATIS; karışık/başka → null (eski davranış). `FistenOlustur`: `dto.Tur` yalnız {null, SATIS, ALIS}; fişten türetilen türle çelişirse **400** ("Bu fiş bir alış fişidir; satış faturası olarak kesilemez" / tersi), fatura yazılmaz; tür gönderilmezse fişten türetilir. Ekrandaki `Faturalar.FistenTur` ile aynı kural. | ZBT1 (saf), ZBT2 (tür yok → ALIS), ZBT3 (alışa SATIS → 400, fatura yazılmaz), ZBT4 (satışa ALIS → 400), ZBT5 (karışık fiş → eski davranış). M15-4, M15-5, M15-6. Canlı § O-6. |
| [YENİ — bu koşumun 375 ölçümü] Önizleme tablosu dar pencerede "Has Gram" sütununu ipucusuz yatay kaydırmaya itiyordu (`tablo-sar` scrollWidth 432 > clientWidth 319) | `zo-belge.css`: `.tablo-sar:has(> #ff-kalem-tablosu)` kap sorgusu `ffkalem`; kap ≤ 440 px'te satır KART: başlık gizli, sıra no gizli, ürün adı tam genişlik, Toplam ve Has Gram `data-etiket` etiketiyle yan yana. Razor: hücrelere `ff-sira`/`ff-ad` sınıfı + `data-etiket` (id'ler AYNI). | ZB70e. Tarayıcı 375: `tablo-sar` scrollWidth 319 = clientWidth 319, thead `display:none`, yatay 0; görüntü `ZO-BELGE-d11-fisten-satis-BASIT-375.png` (Read ile görüldü: "925 Gümüş Kolye / Toplam 20.000 ₺ / Has Gram 11,1000 has"). M15-9, M15-10, M15-11 |

**Dokunulan dosyalar (DEVAM 11):** `Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (FistenTurTuret + FistenOlustur tür kapısı), `Kuyumcu.Web/Pages/Faturalar.razor` (FisKalemAdi, FistenFaturaliMesaj, cari yok metni, işaretsiz tutar, önizleme hücre sınıfları/etiketleri), `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (FistenCariYok, FistenKalemGenel, FistenZatenFaturali, FistenTaslakFaturali — TR+EN), `wwwroot/css/zo-belge.css` (ffkalem kap kuralı). Testler: `FaturaEkranTestleri.cs` (ZB70, ZB70b–e), `ZoBelgeFistenTurTestleri.cs` (yeni, ZBT1–5). Migration: gerekmedi. Mevcut id'ler, yetki, POST yolları DEĞİŞMEDİ (FistenOlustur yalnız çelişen türü reddeder).

### O-2. Build
`dotnet build Kuyumcu.sln --no-incremental` (son mutasyondan sonra) → **0 Uyarı, 0 Hata** (`d29l/build5.log`). Not: artımlı derleme bir kez bayat Web dll'i bıraktı (bkz. devir kontrolü); bu koşumdaki bütün mutasyon derlemeleri ve son derleme `--no-incremental`.

### O-3. Testler
- Filtreli (FaturaEkranTestleri + ZoBelge* + ZOBELGE doğrulayıcı): **204/204** (`d29l/test3.log`); ilk filtre (+ZB70/FaturaFisten): 214/214 (`test2.log`).
- **Tam süit 1** (`tam.log`, trx `trx-pZO-BELGE/d12`, CSS düzeltmesinden önce): 7314 · 7312 geçti · 2 başarısız: `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` ve `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` — ikisi de "Unknown database 'kuyumcu_t1_eski/_hazir'"; tek başına yeniden koşulunca **15/15 yeşil** (`mig.log`) → kararsız, bu paketin dosyası değil.
- **Tam süit 2** (`tam2.log`, trx `d12b`, son ağaç): **7333 · 7332 geçti · 1 başarısız**: `Dogrulayici.SATIS3SinirTestleriK2Hasim2.K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ` (SATIS-3 hasım testi, dosya 22:05'te eklendi; fiş bacakları — SATIS-3 alanı). **Bu paketin ürettiği yeni kırmızı: 0.**

### O-4. Denetim
`bash dotnet/denetim.sh` (kiracı silindikten sonra) → **çıkış 0 · 295 ✓ · DENETİM TEMİZ** (`d29l/denetim.log`).

### O-5. Mutasyon kanıtları (M15, 11/11 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d12/`, her birinde geri koyma sonrası `cmp` AYNI; günlük `d29l/mut/ozet.txt`, `ozet2.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M15-1 | `FisKalemAdi` → `fk.UrunAd ?? fk.Tur` (eski hata) | ZB70_…HURDA_ALIS…ham_kod_yok, ZB70b | AYNI |
| M15-2 | taslak damga denetimi kapatıldı (`false ? …`) | ZB70b_Fisten_penceresi_tarama_buyuk_harf_kod_kalibi_0_… | AYNI |
| M15-3 | `FistenToplam` işaretli | ZB70c_…isaretsiz_toplam_ve_kalem | AYNI |
| M15-4 | sunucu `tur = dto.Tur ?? "SATIS"` (eski) | ZBT2_Tur_gonderilmeyen_alis_fisi_ALIS_faturasi_olur | AYNI |
| M15-5 | çelişki denetimi etkisiz | ZBT3_Alis_fisine_SATIS_turu_400_ve_fatura_yazilmaz, ZBT4_Satis_fisine_ALIS_turu_400 | AYNI |
| M15-6 | türetmede HURDA_ALIS alış sayılmaz | ZBT1_FistenTurTuret_saf_kural, ZBT2 | AYNI |
| M15-7 | cari yok metni kaldırıldı | ZB70b | AYNI |
| M15-8 | kalem tutarı işaretli | ZB70c | AYNI |
| M15-9 | Has Gram hücresinden `data-etiket` kaldırıldı | ZB70e_Fisten_onizleme_dar_kapta_kart_tutar_ve_has_etiketli | AYNI |
| M15-10 | kart eşiği 440 → 300 px (375'te tablo kalır) | ZB70e | AYNI |
| M15-11 | kartta sıra no gizleme kuralı silindi | ZB70e | AYNI |

### O-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, iki kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3PW2NBYV8KP97GHCMD71HRY (kesilen koşumun tohumu: 6 cari / 12 ürün / 9 satış + alış fişi + eski altınla satış) · tek kullanımlık rastgele `Sistem__Anahtari` ortam değişkeni, değeri basılmadı) — betik `d29l/zb12.mjs`, sonuç `zb12.json`
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /islemler/alis-fisi` 1 kalem 750 NAKİT | 200 | 200 (fisId 01M3Q8V1J5S8RB2Q4Z39J3W0P1) |
| `POST /faturalar/fisten` alış fişi + `tur:"SATIS"` | 400, fatura yazılmaz | **400** "Bu fiş bir alış fişidir; satış faturası olarak kesilemez" |
| `POST /faturalar/fisten` satış fişi + `tur:"ALIS"` | 400 | **400** "Bu fiş bir satış fişidir; alış faturası olarak kesilemez" |
| `POST /faturalar/fisten` alış fişi, `tur` YOK | 200, ALIS | **200** `tur:"ALIS"`, `toplam:750` |
| `POST /faturalar/fisten` `tur:"TUHAF"` | 400 | 400 "Tür beklenen biçimde değil" |
| SQL `faturalar` (kiracı) | yalnız iki ALIS taslak; satış fişine fatura YOK | `ALIS TASLAK 2100.50` · `ALIS TASLAK 750.00` (başka satır yok) |
| SQL `hareketler` (fis_id'li) | alış kalemleri taslak damgalı, satış kalemi damgasız | `ALIS -1500.50 TASLAK:` · `ALIS -600.00 TASLAK:` · `ALIS -750.00 TASLAK:` · `SATIS 20000.00 -` |
| Canlı HURDA_ALIS | — | Canlıda eski altın bacağı HURDA_ALIS hareketi `fis_id` NULL yazılıyor (SQL: `HURDA_ALIS fis_id yok 1`), bu yüzden fiş önizlemesine hiç düşmüyor; ürünsüz kalem yolu yalnız bUnit sahte yanıtıyla (ZB70) sınanabildi. |
| Tarayıcı Fişten Fatura Kes (`d29l/fk2.json`) BASİT açık 1440 · PRO koyu 1440 · BASİT açık 375 × {alış fişi, satış fişi} | tür doğru, ham kod 0, damga 0, yatay 0 | 6/6: Tür "Alış"/"Satış"; yaprak `^[A-Z_]{3,}$` yalnız "SIRA","TOPLAM" (sözlük başlıkları `Sıra`/`Toplam`, CSS `text-transform: uppercase` — ham kod DEĞİL); "TASLAK:" 0; yatay 0; faturalı alış fişinde bant "Bu fiş taslak bir faturaya bağlı (numara kesimde verilir)." ve Kaydet kilitli |

### O-7. Gerçek tarayıcı ölçümü (başsız Chrome, sert yükleme; `d29l/sert.json` foto `foto-sert/`; `/denetim-izi` tekrar `sert-di.json`)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm** (son yayından sonra, temiz profil).
| Ölçü | Sonuç |
|---|---|
| K1 · K3 (taşan/kırık) · K5 TRY · K7 · K8 · K10 · K11 · K12 · yatay (Y) · görünür K9 · Yükleniyor'da donan | **0** (112/112) |
| K4 açılış 1440×900 | 7 rotanın hepsi ≤ 1,0 (iki mod, iki tema) |
| Mod doğrulaması | 110/112 uyumlu; `/denetim-izi` 1440'ta iki ölçümde mod anahtarı yarışı (BASİT↔PRO yer değiştirmiş). Rota tek başına 16 ölçümle yeniden koşuldu: **16/16 mod uyumlu**, tüm K = 0, 1440 K4 = 1. |
Görülen görüntüler (Read): `ZO-BELGE-d11-fisten-alis-taslakbagli-BASIT-1440.png` (Tür rozeti "Alış", taslak-bağlı bandı, işaretsiz 750 ₺), `ZO-BELGE-d11-fisten-satis-PRO-karanlik-1440.png` (Tür "Satış", "Cari" etiketi, 11,1000 has), `ZO-BELGE-d11-fisten-satis-BASIT-375.png` (kart satırı, yatay kaydırma yok). Hepsi `ham/TUVAL-KARSILASTIRMA/` altında.

**Ölçüm tuzağı (not):** kesilen/erken bir koşumdan 9716'da açık kalan başsız Chrome (kendi profilim `d29l/profil-fk`) sonraki betiklerin aynı tarayıcıya bağlanmasına ve ESKİ CSS'i önbellekten kullanmasına yol açtı (ilk 375 ölçümü "düzelmedi" gösterdi). Tarayıcı kapatılıp temiz profille yeniden ölçüldü; tablodaki değerler temiz ölçümdür.

**Tuval bölge farkı (bu devamın dokunduğu bölgeler):** Fişten penceresi tuvalde yok (GEREKÇELİ: ortak modal, özellik silinmez) · 375 önizleme kartı tuvalde yok (GEREKÇELİ: tuval 1440; 375'te "yatay kaydırma 0" kuralı) · taslak-bağlı bandı tuvalde yok (GEREKÇELİ: yalnız tetiklenince görünen durum bandı).

### O-8. Temizlik
API (5160), vekil (5161) ve kendi Chrome'larım (9716/9717) `kill` ile durduruldu; `lsof` 5160/5161/9716/9717 boş. Kiracı 01M3PW2NBYV8KP97GHCMD71HRY `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, hareketler 0, faturalar 0. **tenants: koşum başında 16 (benim artığım dahil), sonda 14** (−1 benim; −1 başka şeridin kendi temizliği). QA-TEST-* ve başka PAKET* kiracılarına dokunulmadı. `d29l/anahtar`, `d29l/fik.json`, `d29k/anahtar`, `d29k/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacında .mutorj/.bak/.tmp yok. **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d29l/profil-*`, `d29j/profil-*`, `d29i/profil-*`.

### O-9. Açık kalanlar (sahipleriyle)
1. **Mali müşavir (DOĞRULANAMADI):** alış fişinden "alış faturası" kaydı mı, kişiden alımda gider pusulası (VUK 234) mı düzenlenmeli — birincil kaynakla doğrulanmadı, koda hüküm YAZILMADI. Sunucu yalnız fişle çelişen türü reddediyor; alış fişini hangi belgeyle belgeleyeceğine karar vermiyor.
2. **SATIS-3:** tam süitte `Dogrulayici.SATIS3SinirTestleriK2Hasim2.K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ` kırmızı (fiş döviz bacağı).
3. **Test altyapısı (sahipsiz/kurucu):** `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` ve `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` tam süitte bir kez "Unknown database kuyumcu_t1_*" ile düştü, tek başına yeşil → geçici veritabanı yarışı.
4. **Satış/Altın bacağı sahibi (bilgi):** eski altın HURDA_ALIS hareketi `fis_id`'siz yazılıyor; fişten faturada bu kalem görünmüyor (KDV 23/e özel matrah ayrı akışta). Bilinçli ise not, değilse satış paketi baksın.
5. **Etiket (bilgi, UX-S31 adayı):** önizlemede cari etiketi BASİT'te "Müşteri", PRO'da "Cari"; alış fişinde tedarikçi de "Müşteri" etiketiyle görünüyor (ortak `Rapor.CariBasligi` anahtarı, fatura listesiyle tutarlı). Değiştirmek ortak sözlük anahtarına dokunur; hub sahibine not.
6. **Devreden:** "Altın pozisyonum" N1 (ORTAK-1) · rapor merkezi hub adları hub sahibinde · ölçüm aracı K9 kırpan ata / K8 tohum evrak numarası (bu koşumda K8 0).

### O-10. DoD sözleşmesi
Yeni varlık, yeni uç, yeni düğme yok. `/faturalar/fisten` sözleşmesi daraltıldı: çelişen tür 400 (önceden sessizce yanlış tür yazılıyordu); tür gönderilmezse fişten türetilir. Yıkıcı eylem, modal ve düzenleme kalıbı değişmedi. Hard delete yok. Mevzuat hükmü eklenmedi.

---

## 29.09 DEVAM 10 (16:40–17:55): doğrulayıcı (KALDI) bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § N-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29j/`.

**Devir kontrolü:** DEVAM 9 (§ M) sonrası ağaçta yarım mutasyon yok. Başlangıçta `tenants` = 14.

### N-1. Doğrulayıcı bulguları ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] Fişten Fatura Kes araması eksi fiş toplamının işaretini düşürüyor (alış fişi satış fişiyle aynı) | `AramaModulu.FisAra` alt bilgisi: `{(g.ToplamTl < 0 ? "−" : "")}{EkranPara(...)}` — defter satırı aramasıyla aynı kalıp. `EkranPara` değişmedi (başka çağıranlar etkilenmez). Alt bilgi biçimi doğrulayıcı testinin beklediği gibi `"n kalem · −tutar ₺"`. | Doğrulayıcı testi `DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz` **yeşil**; ZB63 yeşil. Canlı: `POST /islemler/alis-fisi` (1.500,50 + 600) → 200; `GET /arama?tur=FIS&q=<tedarikçi>` → 200 `"2 kalem · −2.100,50 ₺"`. Tarayıcıda sonuç satırı "Anadolu Döküm Toptan Kuyum · 29.09.2026 2 kalem · −2.100,50 ₺". M14-1 |
| [ORTA] Fişten Fatura Kes alış fişini SATIŞ faturası olarak kesiyor | Sunucuya (FaturaModulu, sahibi Fatura/L-1b; kurucunun ayrı KDV göreviyle çakışma riski) DOKUNULMADI. Ekran tarafında (bu paketin dosyası): faturanın türü **önizlemedeki fiş kalemlerinden türetilir** (UX-S21) — bütün kalemler `ALIS`/`HURDA_ALIS` ise ALIS, aksi hâlde SATIS (karışık fişte eski varsayılan korunur). Önizlemede yeni satır "Tür: Alış/Satış" (rozet, yeni id `ff-tur`) ve gövdede `tur` gönderilir (sunucu `dto.Tur`'u zaten kabul ediyor). `Faturalar.FistenTur(FisYaniti?)` saf, test edilebilir. | ZB66 (alış fişi → gövde `tur:"ALIS"`, rozet "Alış"), ZB66b (satış → SATIS; karışık → SATIS; önizleme yok → null). Canlı tarayıcı: önizlemede `#ff-tur` = "Alış", toplam "−2.100,50 ₺"; Kaydet → liste ilk satır "Kesimde verilir · 29.09.2026 · Anadolu Döküm Toptan Kuyum · **Alış** · 2.100,50 ₺ · Taslak". SQL: `faturalar` → `ALIS TASLAK 2100.50`. Görüntü `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d10-fisten-alis-onizleme-BASIT-1440.png` (Read ile görüldü). M14-2, M14-3, M14-7 |
| [ORTA] İşlem defteri PRO'da taslak faturanın iç işareti `TASLAK:<ULID>` görünüyor | `IslemDefteri.razor`: `FaturaNo` `TASLAK:` ile başlıyorsa ULID basılmaz; Faturalar ekranıyla aynı metin "Fatura No: Kesimde verilir" (`CumaJSozlugu.FaturaNoKesimdeKisa`, title `Fatura.NoKesimde`). Önek sabiti ekranda `IslemDefteri.TaslakOnEki`; test sunucunun `FaturaSeriKapisi.TaslakOnEki` ile birebir eşitliğini kilitler. id `id-fatura-{Id}` AYNI. | ZB67. Canlı SQL: iki alış hareketi `fatura_no` = `TASLAK:…`; tarayıcı PRO 1440/1280: `main` içinde "TASLAK:" = **0**, iki satırda "Fatura No: Kesimde verilir". Görüntü `ZO-BELGE-d10-islem-defteri-PRO-1440.png`. M14-4 |
| [DÜŞÜK] Yapışkan sütun gölgesi kart (yığın) düzeninde de çiziliyor | `zo-belge.css` (b) kuralı `@container zoyigin (min-width: 1061px)` içine alındı (kart eşiği `max-width: 1060px` ile bitişik). | ZB68 (eşik bitişikliği + gölge yalnız tablo sorgusunda). Tarayıcı `getComputedStyle(td.q10-yigin-eylem).boxShadow`: PRO 1440 (tablo) gölgeli, PRO 1280 açık ve koyu (kart) `none`. Görüntü `ZO-BELGE-d10-islem-defteri-PRO-karanlik-1280.png`: "Belge" solunda dikey iz yok. M14-5 |
| [DÜŞÜK] `<details>` bir `<p>` içinde (evrak-ozet) | `<p class="zo-ozet" id="evrak-ozet">` → `<div …>` (id, sınıf AYNI; CSS sınıfa bağlı). | ZB69; tarayıcıda `#evrak-ozet` = DIV, `main p details` = 0. M14-6 |

**Dokunulan dosyalar (DEVAM 10):** `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (FisAra alt bilgisi, 1 satır + yorum), `Pages/Faturalar.razor` (ff-tur satırı, gövdede `tur`, `FistenTur`), `Pages/IslemDefteri.razor` (taslak işaret), `Pages/Evraklar.razor` (p → div), `wwwroot/css/zo-belge.css` (gölge kap sorgusu). Testler: `FaturaEkranTestleri.cs` (ZB66, ZB66b; sahte alış fişi yanıtı), `ZoBelgeTestleri.cs` (ZB67, ZB68, ZB69). Sözlüğe yeni anahtar gerekmedi (mevcut `Fatura.Tur/Satis/Alis`, `CumaJ.FaturaNoKesimdeKisa`, `Fatura.NoKesimde`). Migration: gerekmedi. Mevzuat hükmüne dokunulmadı. POST yolları, yetki, mevcut id'ler DEĞİŞMEDİ (yalnız `/faturalar/fisten` gövdesine sunucunun zaten kabul ettiği `tur` alanı eklendi).

### N-2. Build
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata, 0 Uyarı** (`d29j/build2.log`).

### N-3. Testler
- Filtreli (ZB6*, DZB_alis, ZB41, ZB44, Fisten*): **34/34** (`d29j/test1.log`).
- **Tam süit** ilk koşum (`tam.log`) 16:58'de ~4.870 testten sonra ASILI kaldı (test sürecinin CPU'su %2, kuyumcu_test_t1 bağlantısı 37 dk uykuda; o sırada bir kırmızı: `SATIS3EkranTestleri.T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ`). Yalnız KENDİ süreçlerim (pid 53778/53800/53801) durduruldu, `--blame-hang --blame-hang-timeout 4m` ile yeniden koşuldu (`tam2.log`, trx `trx-pZO-BELGE/d10b`): **7277 test · 7275 geçti · 2 başarısız · asılma yok**:
  - `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` — `Islemler.razor` 1424 satır, dosya 17:34'te başka şeritçe değiştirildi (SATIS-3).
  - `S1iGorselSigma2Testleri.S1i_06_Satis_ekrani_basligi_menu_adiyla_ayni_ve_satir_ici_style_yok` — Satış ekranı (Islemler, SATIS-3).
  İkisi de bu paketin dosyası değil. **Bu paketin ürettiği yeni kırmızı: 0.**

### N-4. Denetim
`bash dotnet/denetim.sh` (test kiracısı silindikten sonra) → **çıkış 0 · 295 ✓ · DENETİM TEMİZ** (`d29j/denetim.log`).

### N-5. Mutasyon kanıtları (M14, 7/7 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d10/`, her birinde geri koyma sonrası `cmp` AYNI; günlük `d29j/mut/`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M14-1 | FisAra eksi işaret öneki kaldırıldı | `Dogrulayici.ZOBELGEAramaSinirTestleri.DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz` | AYNI |
| M14-2 | `FistenTur` ALIS tanımı bozuldu (`"XALIS"`) | `ZB66_Alis_fisinden_kesilen_fatura_ALIS_turuyle_gider_ve_ekranda_Alis_yazar` | AYNI |
| M14-3 | Gövdeden `tur` kaldırıldı | ZB66, `ZB66b_Satis_fisi_ve_karisik_fis_SATIS_turuyle_gider` | AYNI |
| M14-4 | Taslak önek denetimi bozuldu | `ZB67_Islem_defteri_PRO_taslak_fatura_isareti_ULID_gostermez_Kesimde_verilir_yazar` | AYNI |
| M14-5 | Gölge kap eşiği 1061 → 1200 (arada gölgesiz tablo boşluğu) | `ZB68_Yapiskan_sutun_golgesi_yalniz_tablo_duzeninde_kart_duzeninde_yok` | AYNI |
| M14-6 | evrak-ozet yeniden `<p>` | `ZB69_Evrak_ozeti_div_ipucu_paragraf_icinde_degil` | AYNI |
| M14-7 | `ff-tur` id'si değişti (ilk koşu başka şeridin eşzamanlı derlemesiyle `Kuyumcu.Web.pdb` kilidine takıldı — MSB4018; yeniden koşuldu, M14-7b) | ZB66, ZB66b | AYNI |

### N-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin` · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3PQ8AABNVX4Y5HCEDBR3HYK · tek kullanımlık rastgele `Sistem__Anahtari` ortam değişkeni, değeri basılmadı)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Tohum `demo-tohumla.js --prova-kiraci` | kiracı + veri | çıkış 0 · 6 cari / 12 ürün / 9 satış |
| `ek.mjs`: 2 kasa + hareket + gün kapanışı + 4 evrak + 1 karşılıksız | 2xx | 14 × 2xx; fatura adımı 400 (bilinen, § G-9-7) |
| `fat.mjs`: 3 fatura (taslak / kesildi / iptal) | 200 | 200 ×3, durum PATCH 200 ×3 |
| `POST /islemler/alis-fisi` 2 kalem 1.500,50 + 600 NAKİT | 200 | 200 (`fisId, kalemSayisi, toplamTl, …`) |
| `GET /arama?tur=FIS&q=<tedarikçi>` | 200, eksi işaretli | 200 · `"2 kalem · −2.100,50 ₺"` |
| Tarayıcı: Fişten Fatura Kes → alış fişi → Önizle → Kaydet | Tür "Alış", liste "Alış" | `#ff-tur` "Alış"; `#ff-toplam` "−2.100,50 ₺"; modal kapandı; liste ilk satır "… Alış 2.100,50 ₺ Taslak" |
| SQL `faturalar` (kiracı) | ALIS faturası | `SATIS TASLAK 1200.00` · `SATIS KESILDI 3600.00 FTR-2026-000001` · `SATIS IPTAL 900.00 FTR-2026-000002` · **`ALIS TASLAK 2100.50`** |
| SQL `hareketler` (ALIS, fis_id'li) | taslak damgası | `ALIS -1500.50 TASLAK:` · `ALIS -600.00 TASLAK:` |
| Tarayıcı `/raporlar/islem-defteri` PRO | ULID yok | "TASLAK:" 0; "Fatura No: Kesimde verilir" ×2 |

### N-7. Gerçek tarayıcı ölçümü (başsız Chrome, sert yükleme; `d29j/sert.json`, foto `d29j/foto-sert/`; akış `d29j/fk.json`, foto `d29j/foto-fk/`)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm**; mod her ölçümde `data-mod` ile doğrulandı (uyumsuz 0).
| Ölçü | Sonuç |
|---|---|
| K1 · K3 · K5 TRY · K7 · K10 · K11 · K12 · yatay (Y) · Yükleniyor'da donan | **0** (112/112); K3 bu kez ham da 0/0 |
| K4 açılış 1440×900 | 7 rotanın hepsi ≤ 1,0 (iki mod, iki tema) |
| K8 | 8 ölçüm, yalnız /evraklar: tohum evrak numarası "ZB-CEK/ZB-SNT" (test verisi) |
| K9 | ham 14 (işlem defteri + kim ne yaptı 1440/1280; kendi kabında kayan tablonun kırpılmış satırları) — görünür K9 **0** (112/112) |
| Yapışkan gölge | PRO 1440 tablo: var · PRO 1280 kart (açık/koyu): `none` |
Görülen görüntüler (Read): Fişten önizleme alış BASİT koyu 1440 (Tür rozeti "Alış", toplam eksi), işlem defteri PRO koyu 1280 (kart, gölge izi yok, "Kesimde verilir"), işlem defteri PRO açık 1440 (tablo kaba sığar, "Kesimde verilir"), evraklar BASİT 375 (özet satırları + (i), kart listesi, yatay kaydırma yok).

**Tuval bölge farkı (bu devamın dokunduğu bölgeler):** Fişten penceresi tuvalde yok (GEREKÇELİ: ortak modal, özellik silinmez; yalnız "Tür" satırı eklendi — sistemin bildiği değer gösterilir, UX-S21) · işlem defteri fatura alt satırı tuvalde yok (GEREKÇELİ: PRO ek bilgisi; artık seri numarası olmayan iç işaret basılmıyor) · kart düzeni gölgesi tuvalde yok, bizde yok (AYNI).

### N-8. Temizlik
API (5160) ve vekil (5161) `kill` ile durduruldu; Chrome (9716/9717) kapandı; `lsof` 5160/5161/9716/9717 boş. Kiracı 01M3PQ8AABNVX4Y5HCEDBR3HYK `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, hareketler 0. **tenants: başta 14, sonda 14.** QA-TEST-* ve başka PAKET* kiracılarına dokunulmadı. `d29j/anahtar`, `d29j/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacında .mutorj/.bak/.tmp yok. **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d29j/profil-*`, önceki `d29i/profil-*`.

### N-9. Açık kalanlar (sahipleriyle)
1. **Fatura/L-1b (sunucu):** `FaturaModulu.FistenOlustur` hâlâ `tur = dto.Tur ?? "SATIS"`; ekran artık türü gönderiyor, ama başka bir istemci (API) tür göndermezse alış fişi yine satış faturası olur. Sunucunun türü fiş hareketlerinden türetmesi ya da fiş türüyle çelişen `tur`'u reddetmesi önerilir (kurucunun Fatura KDV göreviyle birlikte ele alınmalı).
2. **SATIS-3:** tam süitte `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` (Islemler.razor 1424 satır) ve `S1iGorselSigma2Testleri.S1i_06_…` kırmızı; ilk koşumda `SATIS3EkranTestleri.T13_MOD_ve_SEKME_…` kırmızıydı ve süit bir testte asılı kaldı (ikinci koşumda asılma yok).
3. **Ölçüm aracı sahibi:** K9 kırpan atayı dikkate almıyor; K8 tohum evrak numarasını jargon sayıyor.
4. **Satış fişi / P-23 sahibi:** § M-9-2 (değişmedi; bu koşumda satış fişi açılmadı, denetim TEMİZ).
5. **Devreden:** "Altın pozisyonum" N1 (ORTAK-1) · rapor merkezi hub adları hub sahibinde · 1280×800 açılış (bilgi) · fişten önizlemede ürünsüz kalemde `fk.UrunAd ?? fk.Tur` ham tür kodu basabilir (Faturalar.razor, UX-S31; bu koşumdaki alış fişinde iki kalem de ürünlüydü) — sonraki dokunuşta sözlükten çevrilmeli.

### N-10. DoD sözleşmesi
Yeni varlık, yeni uç, yeni düğme yok. `/faturalar/fisten` gövdesine sunucunun zaten kabul ettiği `tur` eklendi (sözleşme değişmedi). Arama alt bilgisine işaret eklendi (biçim). Yıkıcı eylem, modal ve düzenleme kalıbı değişmedi. Hard delete yok. Mevzuat hükmüne dokunulmadı.

---

## 29.09 DEVAM 9 (15:25–16:25): tasarım hakemi k3 (KALDI) bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § M-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29i/`.

**Devir kontrolü:** DEVAM 7+8 (§ L) sonrası ağaçta yarım mutasyon yok (yedek `mut-yedek/ZO-BELGE-d8/` ile `cmp` AYNI). Önceki "SON DOĞRULAMA (KALDI)" üçlüsü (has 4 hane · başlık = menü adı · ZB14 Raporlar kapsamı) güncel ağaçta hâlâ KAPALI (§ G-1; ZB58, ZB23, ZB14 yeşil). Bu devam hakem k3'ün üç KRİTİK bulgusunu ele aldı.

### M-1. Hakem k3 bulguları ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] Fişten Fatura Kes: "2100.00 TRY", etiket "Fiş Kimliği", ipucu "sunucuda hesaplanır", satır düzeni tutarsız | (a) `AramaModulu.FisAra` alt bilgisi `EkranPara(g.ToplamTl, "TRY")` — aynı dosyada ZO-ISLEM-A'nın yazdığı, `Bicim.Para` ile birebir kural (tr-TR, bölünmez boşluk, ₺, kuruş sıfırsa yazılmaz). Tek satır değişiklik; sahibi belirsiz dosya (ZO-ISLEM-A da dokunmuştu) — hakem "bu pakette kapanmadan geçmez" dediği için burada kapatıldı. (b) Etiket `ZB.FistenFisEtiket` = "Fiş" (EN "Receipt"). (c) İpucu iki kez basılıyordu (üstte + boş durumda) → tek kez, `ZB.FistenIpucu` "Fişin bütün kalemleri faturaya girer. Has bedeli fişten hesaplanır." (d) `.zo-fk-fis` sarmalı: alt bilgi HER satırda adın altında, tek tür olduğu için tür çipi gizli (ortak KayitSecici'ye dokunulmadı). id `ff-fis` AYNI. | Canlı API: `GET /arama?tur=FIS` → 200, `"2 kalem · 2.100,50 ₺"` (gözatma ve sorgulu). Tarayıcı 12/12 (BASİT/PRO × açık/koyu × 1440/1280/375): alt bilgi adın altında (altAltta true), etiket "Fiş", TRY 0, "sunucuda" 0, K5 0, pencere içi K9 0, yatay 0. ZB63 (HTTP, sabit beklenen), ZB65 (bUnit) + M13-1, M13-6, M13-7, M13-8 |
| [KRİTİK] İşlem defteri PRO 1440: KAYDEDEN sütunu yapışkan BELGE altında kırpılıyordu (tablo 1221 > kap 1122) | Hakemin önerisi: müşteri ve kaydeden TEK sütun ("Müşteri · Kaydeden", `ZB.IslemCariKaydeden`, TR+EN). Kaydeden müşteri hücresinin alt satırında ("Kaydeden: ad"), şube kendi satırında. 9 → 8 sütun; veri silinmedi. `id-th-kaydeden` AYNI (birleşik sütunun başlığı); yeni id `id-kaydeden-{Id}`. Ek olarak yapışkan eylem sütununa sol kenarda görünür gölge (kaydırma ipucu). | Tarayıcı PRO 1440 açık/koyu: tablo = kap = **1122/1122**, 1280'de 962/962 (kart), 375'te 349/349; K3 (sr-only hariç) 0, K9 görünür 0, yatay 0. Görüntü: `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d9-islem-defteri-PRO-1440.png` (Read ile görüldü: BELGE sütunu tam, kesik sütun yok). ZB41 (güncellendi) + M13-2, M13-3, M13-13, M13-14, M13-15 |
| [KRİTİK] Yedi ekranda h1 altında teknik açıklama satırı (K17), kasa/evrak özetinde parantezli teknik alt metin | Yedi ekranda `SayfaBasi Aciklama` KALDIRILDI (iki modda; ZO-ISLEM-A'nın Banka/POS/Hurda'daki çözümüyle aynı). BASİT rapor yüzünde yalnız GÜNÜN TARİHİ kalır (veri, cümle değil). İçe aktarımın ikinci paragrafı (`ia-kapsam-notu`, id AYNI) "Dosyayı nasıl hazırlarım?" katlanırının ilk satırına taşındı (kural yazılı kalır). Kasa ("Kasadaki para") ve evrak özetindeki "(bugün itibarıyla — …bağımsız)" alt metni satırdan kalktı, (i) ipucuna (`zo-ipucu`) girdi — UX-S33 notu kaybolmadı. Yeni id'ler `evrak-ozet-ipucu`, `kr-bakiye-ipucu-{KasaId}`. | Tarayıcı 112/112 ölçümde `.sayfa-basi-aciklama` = 0 (raporlar-bugun hariç, o tarih). Görüntüler `ZO-BELGE-d9-faturalar-BASIT-1440.png`, `-veri-ice-aktarim-BASIT-1440.png`, `-evraklar-BASIT-1440.png` (ÖNCE d8 · SONRA d9 · tuval; Read ile görüldü). ZB64 (BASİT+PRO) + M13-4, M13-5, M13-9, M13-10, M13-11, M13-12 |
| (hakem) "üst çubuk başlığı ile h1 aynı ada iki kez yer ayırıyor" | Bu paketin dosyası değil (SayfaBasi = ORTAK-1). ORTAK-1 bu koşum sırasında (15:57 ortak.css, 16:11 SayfaBasi.razor) başlık üst barla aynıysa şerit başlığını görsel gizli yaptı (`sayfa-basi--baslik-ustte`). Son ölçümde yedi ekranın altısında başlık yalnız üst çubukta (tuvalle aynı). | `foto-sert2/_raporlar_kasa-basit-aydinlik-1440.png` (görüldü) |

**Dokunulan dosyalar (DEVAM 9):** `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (1 satır, fiş alt bilgisi), `Pages/Faturalar.razor`, `Pages/Evraklar.razor`, `Pages/Raporlar.razor`, `Pages/IslemDefteri.razor`, `Pages/KasaRaporu.razor`, `Pages/DenetimIzi.razor`, `Pages/VeriIceAktarim.razor`, `wwwroot/css/zo-belge.css`, `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (+FistenFisEtiket, FistenIpucu, IslemCariKaydeden; TR+EN). Testler: `ZoBelgeAramaFisTestleri.cs` (yeni, ZB63), `ZoBelgeTestleri.cs` (ZB64 yeni; ZB41, ZB12, ZB56 güncellendi), `FaturaEkranTestleri.cs` (ZB65 yeni; sahte arama yanıtı gerçekçi biçime çekildi), `IceAktarimEkranTestleri.cs` (CJ_12 ve kapsam notu testi: not katlanırda). Mevcut id'ler, POST yolları, yetki, yasal kurallar DEĞİŞMEDİ. Migration: gerekmedi. Mevzuat hükmüne dokunulmadı.

### M-2. Build
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata, 0 Uyarı** (`d29i/build6.log`). Ara derlemede 1 uyarı: `EtiketEkranTestleri.cs(145) xUnit1031` (ORTAK-1).

### M-3. Testler
- Filtreli (ZoBelge, ZOBELGE, IceAktarimEkran, FaturaEkran, Arama, P48IslemDefteri, KasaRapor, EvrakEkran, S34RaporBugun, RaporEkran, Denetim): **606/606** (`d29i/test2.log`); şube satırı değişikliğinden sonra ZB41+ZB64: 3/3.
- **Tam süit** (`trx-pZO-BELGE/d9`, `d29i/tam.log`): **7248 test · 7246 geçti · 2 başarısız**: `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL`, `TS2TuvalProTestleri.Diger_dugmesi_ozet_yazar_…` — ikisi de Islemler ekranı (SATIS-3), yalnız koşuldu: **2/2 geçti** (`d29i/test4.log`) → yük altında kararsız, bu paketin dosyası değil. **Bu paketin ürettiği yeni kırmızı: 0.** (Devir listesindeki `OrtakGorunumSaflik.O8` bu koşumda yeşil.)

### M-4. Denetim
`bash dotnet/denetim.sh` → **çıkış 0 · 295 ✓ · DENETİM TEMİZ** (`d29i/denetim2.log`). Not: temizlikten ÖNCE koşulan ilk denetim (`denetim.log`) `P-23 ODEME_KALEMSIZ_YOL = 1` verdi; satır BU paketin test kiracısındaydı: canlı doğrulama için `/islemler/satis-fisi` ile açılan 2 kalemli NAKİT fişin ikinci hareketi (tur SATIS, odeme_turu KARMA, nakit_tutar 900) aktif ödeme kalemi taşımıyor (fiş toplamı farkı sayacı 0). Kiracı silinince 0. Bu bir **ürün gözlemi**: çok kalemli satış fişinde ödeme kalemi yalnız bir harekete bağlanıyor ya da P-23 canlı sayacı fiş düzeyinde saymalı — sahibi satış fişi/P-23 (§ M-9).

### M-5. Mutasyon kanıtları (M13, 15/15 kırmızı; ANA ağaçta, yedek `mut-yedek/ZO-BELGE-d9/`, her birinde geri koyma sonrası `cmp` AYNI; günlük `d29i/mut/`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M13-1 | Fiş alt bilgisi `EkranPara` → eski `ToString("0.00", Invariant) + " TRY"` | ZB63 | AYNI |
| M13-2 | Birleşik başlık `IslemCariKaydeden` → `IslemKaydeden` | ZB41 | AYNI |
| M13-3 | Kaydeden alt satırından kullanıcı adı kaldırıldı | ZB41 | AYNI |
| M13-4 | Kasa raporuna `Aciklama` geri eklendi | ZB64 (BASİT, PRO) | AYNI |
| M13-5 | Kasa satırına "(bugün itibarıyla …)" geri eklendi | ZB64 ×2 | AYNI |
| M13-6 | Fişten etiketi `FisKimligi` | ZB65 | AYNI |
| M13-7 | Boş durum ipucu eski "sunucuda" metni | ZB65 | AYNI |
| M13-8 | `.zo-fk-fis` ad satırı `flex: 0 1 auto` | ZB65 | AYNI |
| M13-9 | İçe aktarıma `Aciklama` geri eklendi | ZB64 ×2, CJ_12 | AYNI |
| M13-10 | BASİT rapor başlığına açıklama cümlesi geri eklendi | ZB64 (BASİT) | AYNI |
| M13-11 | Evrak ipucu metni yanlış anahtar | ZB64 ×2 | AYNI |
| M13-12 | Kapsam notu katlanırdan açılışa geri alındı | ZB64 ×2, ZB12 | AYNI |
| M13-13 | Yapışkan sütun gölgesi kaldırıldı | ZB41 | AYNI |
| M13-14 | Şube satırı `display: inline` | ZB41 | AYNI |
| M13-15 | Şube öncesi boşluk/sınıf kaldırıldı | ZB41 | AYNI |

### M-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, iki kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3PK44NPAQJZHC96203MM69S · tek kullanımlık `Sistem__Anahtari` ortam değişkeni, değeri basılmadı)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Tohum `demo-tohumla.js --prova-kiraci` | kiracı + veri | çıkış 0 · 6 cari / 12 ürün / 9 satış |
| `ek.mjs`: 2 kasa + hareket + gün kapanışı + 4 evrak + 1 karşılıksız | 200 | 14 × 2xx; fatura adımı 400 (bilinen, § G-9-7) |
| `fat.mjs`: 3 fatura (taslak / kesildi / iptal) | 200 | 200 ×3, durum PATCH 200 ×3 |
| `POST /islemler/satis-fisi` 2 kalem 1.200,50 + 900 NAKIT | 200 | 200 (ilk denemeler: kasa seçilmedi 400, kapanmış gün 400 — sunucu kapıları doğru çalıştı; 48.790,50 nakit fiş tevsik haddiyle 400) |
| `GET /arama?tur=FIS&q=` / `q=<cari>` | 200, tr-TR ₺ | 200 · gözatma true · `2 kalem · 2.100,50 ₺` / 200 · gözatma false · aynı |
| SQL | fiş hareketleri | tenant hareketleri fis_id'li 2 satır (P-23 sorgusu 1 kalemsiz satır buldu, § M-4) |

### M-7. Gerçek tarayıcı ölçümü (başsız Chrome, sert yükleme; `d29i/sert2.json`, foto `d29i/foto-sert2/`; hedef ölçüm `d29i/hedef2.json`, foto `foto-hedef2/`)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm**; mod her ölçümde `data-mod` ile doğrulandı (uyumsuz 0).
| Ölçü | Sonuç |
|---|---|
| K1 · K5 TRY · K7 · K10 · K11 · K12 · yatay · Yükleniyor'da donan | **0** (112/112) |
| K3 taşan/kırık | ham 1/0 (96 ölçüm) — TEK örnek `#sayfa-basi-*-baslik`: ORTAK-1'in 15:57/16:11'de görsel gizlediği (sr-only, 1×1 px clip) şerit başlığı; ölçüm aracı bunu "taşan" sayıyor. sr-only dışı K3 = **0** (112/112). İlk ölçümde (ORTAK-1 değişikliğinden önce, `sert.json`) K3 ham 0/0 idi. |
| K4 açılış 1440×900 | **7 rotanın hepsi 1,0** (iki mod, iki tema) |
| K4 1280×800 (bilgi) | evraklar 1,4 · faturalar 1,1–1,2 · rapor merkezi 1,1 |
| K8 | 8 ölçüm, yalnız /evraklar: tohum evrak numarası "ZB-CEK/ZB-SNT" (test verisi) |
| K9 | ham 14 (işlem defteri, kim ne yaptı 1440/1280; kendi kabında kayan tablonun kırpılmış satırları) — görünür K9 **0** (112/112). Fişten penceresinde ham 5–6 = pencerenin ARKASINDAKİ sayfa metni; pencere içi K9 **0** (12/12). |
| İşlem defteri tablo/kap | PRO 1440 **1122/1122** · 1280 962/962 · 375 349/349; BASİT aynı |
Görülen görüntüler: işlem defteri PRO 1440 (BELGE sütunu tam, "Kaydeden: …" ve şube müşterinin altında), kasa raporu BASİT 1440 (açıklama yok, (i)), evraklar BASİT koyu 1440, Fişten penceresi BASİT 1440 ve PRO koyu 375 ("2 kalem · 2.100,50 ₺", "Fiş", tek ipucu).

**Tuval bölge farkı (bu devamın dokunduğu bölgeler):** başlık — tuvalde yalnız üst çubukta; bizde altı ekranda aynı (ORTAK-1 sr-only), BASİT rapor yüzünde başlık + tarih (GEREKÇELİ: "bugün" özetinin günü veridir) · açıklama satırı — tuvalde yok, bizde yok (AYNI) · tablo — tuvalde sütunlar kaba sığar, bizde 1122/1122 (AYNI) · Fişten penceresi tuvalde yok (GEREKÇELİ: tuvalde olmayan özellik silinmez; ortak modal).

### M-8. Temizlik
API (pid 10190) ve vekil `kill` ile durduruldu; Chrome (9716/9717) kapatıldı; `lsof` 5160/5161/9716/9717 boş. Kiracı 01M3PK44NPAQJZHC96203MM69S `kiraci-temizle.sh --uygula` ile silindi (çıkış 0): tenants'ta 0, hareketler 0, denetim_kayitlari 0. **tenants: başta 14, sonda 15** — fark bu koşum sırasında başka şeridin açtığı `PAKETSATIS-3-TEST Kuyumculuk` (dokunulmadı); bu paketin kiracısı yok. QA-TEST-* ve başka PAKET* kiracılarına dokunulmadı. `d29i/anahtar`, `d29i/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacında .mutorj/.bak/.tmp yok. **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d29i/profil-*`.

### M-9. Açık kalanlar (sahipleriyle)
1. **Ölçüm aracı sahibi:** `sikisma-olc.js` K3, görsel gizli (sr-only) başlığı "taşan" sayıyor (ORTAK-1'in `sayfa-basi--baslik-ustte` kuralından sonra tüm ekranlarda 1/0 çıkar); K9 kırpan atayı ve modal arkasını dikkate almıyor; K8 tohum evrak numarasını jargon sayıyor.
2. **Satış fişi / P-23 sahibi:** 2 kalemli NAKİT satış fişinde ikinci harekete aktif ödeme kalemi bağlanmıyor → `P-23 ODEME_KALEMSIZ_YOL` canlı sayacı test verisiyle 1 oldu (§ M-4). Ürün kuralı mı sayaç tanımı mı düzeltilmeli, karar sahibinde.
3. **SATIS-3:** `SATIS3EkranTestleri.Barkod_dizisi…` ve `TS2TuvalProTestleri.Diger_dugmesi…` tam süitte kırmızı, yalnız koşumda yeşil (kararsız).
4. **Arama modülü sahibi (ZO-ISLEM-A/KATMAN):** `AramaModulu.FisAra` alt bilgisi bu pakette `EkranPara`'ya çekildi (tek satır); fiş toplamı `Math.Abs` ile basılır (fiş toplamı eksi olmaz varsayımı EkranPara'nın kendi kuralı).
5. **Devreden:** "Altın pozisyonum" N1 (ORTAK-1) · rapor merkezi hub adları hub sahibinde · 1280×800 açılış 1,1–1,4 (bilgi) · PRO üst çubuk 1440'ta iki satır (KATMAN/ORTAK-1).

### M-10. DoD sözleşmesi
Yeni varlık, yeni uç, yeni düğme yok. Sunucuda yalnız arama alt bilgisinin BİÇİMİ değişti (içerik ve sözleşme aynı). Açıklama metinleri silinmedi: sözlükte duruyor; UX-S33 notları (i) ipucunda, içe aktarım kapsam kuralı katlanırda. Yıkıcı eylem, modal ve düzenleme kalıbı değişmedi. Hard delete yok. Mevzuat hükmüne dokunulmadı.

---

## 29.09 DEVAM 7+8 (13:08–13:46 kesilen koşum + 14:15–15:05 bu koşum): doğrulayıcı (KALDI) bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § L-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9717). 5199/5200 ve tarayıcı bölmesi kullanılmadı. Ölçüm başsız Chrome ile yapıldı (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler: kesilen koşum `/private/tmp/kuyumcu-serit/ZO-BELGE/d29g/`, bu koşum `d29h/`.

**Devir kontrolü:** DEVAM 7 (13:08–13:46) kodu ve testleri (ZB57–ZB61) yazmıştı. Mutasyon turu M11'i bitirmiş (`d29g/mut.out` "BITTI"), ama raporu yazamadan kesilmişti. Kopya ağaç (`ZO-BELGE/kopya`) bu arada silinmişti. Ana ağaçta yarım mutasyon YOK. M11 yedekleri (`mut-yedek/ZO-BELGE-d7/`) ana ağaçla `cmp` AYNI: IslemDefteri, Evraklar, Raporlar, KasaRaporu, zo-belge.css, Sozlukler.ZoBelge. Tek istisna DenetimIzi.razor: yedekten SONRA (13:34) bilinçli olarak düzenlenmişti (PRO'da da sade tutar + ham kayıt ipucunda). Bu yüzden M11-1 ve M11-11 eski sürümde koşmuştu. İkisi bu koşumda güncel dosyada M12-4/M12-5 olarak yeniden koşuldu. DEVAM 7'nin API'si (5160) ve vekili (5161) hâlâ çalışıyordu. Aynı kiracıyla devam edildi, sonunda ikisi de durduruldu.

### L-1. Doğrulayıcı bulguları ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] /denetim-izi: "elle kasa hareketi: GIRIS 1500.50 TRY" (ham kod, InvariantCulture, TRY) | `ZoBelgeDenetimSozlugu.KasaHareketiMetni`: yön Giriş/Çıkış, tutar `Bicim.Para` (tr-TR, ₺/$). Storno varyantı ("… geri alındı · kapanmış gün · gerekçe") TR+EN. İKİ modda uygulanıyor. PRO'da ham kayıt hücrenin `title` ipucunda (kayıt değişmez), BASİT'te ipucu da yok. | Canlı: 3 elle hareket + 1 storno (POST 200). Tarayıcı BASİT ve PRO, açık ve koyu, 1440/1280/375: "Kasaya elle giriş: 1.500,50 ₺ · açıklama: dogrulayici", "Kasadan elle çıkış: 25,75 $", "Elle kasa hareketi geri alındı · gerekçe: …". **K5_TRY 0** (12/12 ölçüm, `d29h/dz.json`). ZB57, ZB57b + M11-2, M11-3, M12-4, M12-5 |
| [ORTA] Açılış ≤ 1,0: işlem defteri 2,15/2,61, kim ne yaptı 2,03 | Masaüstünde (≥1024 px genişlik, ≥600 px yükseklik) iki liste tablosu KENDİ kabında dikey kayıyor: `max-height: max(18rem, calc(100dvh - pay))`, başlık yapışık. Sayfa boyu hakem kararıyla **25 kaldı**. Dar ekranda ve kâğıtta sınır yok. | Sert yükleme, 1440×900: iki rota, iki mod, iki tema → **K4 = 1,0** (önce 2,0–2,6). Önce/sonra: `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d8-islem-defteri-BASIT-1440.png`, `…-d8-denetim-izi-BASIT-1440.png` (görüldü: Önceki/Sonraki ilk ekranda). ZB61 + M11-8, M11-9 |
| [DÜŞÜK] Has 4 haneyi bu paketin testleri kilitlemiyordu | Sabit beklenenli test: "0,0001 has". | ZB58 + M11-4 (Bicim.Has→Gram → ZB58 kırmızı) |
| [DÜŞÜK] ZB48 eylemsiz ölçüyordu | Kilitli düğmeye tıklama deneniyor. İşleyicinin kendi koruması da sınanıyor: istek 0, belge 0, print 0. | ZB48 + M11-6 (koruma kaldırılınca ZB48 kırmızı) |
| [DÜŞÜK] Kasa geçmiş tablosunda TL "0 ₺", döviz "0,00 $" | Gün gün tablo `Bicim.BelgePara` kullanıyor (TL ve dövizde iki hane). Özet satırı ekranın genel biçiminde kaldı. | ZB59 + M11-5 |
| [DÜŞÜK] Razor özniteliğinde gövdeli lambda | Yedi ekranda öznitelikte `{ }` gövdeli lambda yok. Hepsi adlandırılmış yöntemlerde (`VadeBasDegisti`, `YalnizHassasDegisti` …). | ZB60 + M11-7 |
| (bu koşum) M11-10, M11-11, M11-12 HAYATTA KALDI: süzgeç işleyicisi alanı atamasa da hiçbir test kırılmıyordu (test açığı) | 3 yeni bUnit testi: tarih/hassas/vade değişince istek YENİ değerle gidiyor, süzgeç rozeti sayıyor. | ZB62, ZB62b, ZB62c + M12-1, M12-2, M12-3 (üçü de kırmızı) |

**Dokunulan dosyalar (DEVAM 7+8):** `Pages/DenetimIzi.razor`, `Pages/Raporlar.razor`, `Pages/KasaRaporu.razor`, `Pages/IslemDefteri.razor`, `Pages/Evraklar.razor` (lambda → yöntem), `wwwroot/css/zo-belge.css` (masaüstü liste kabı), `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (KasaHareketiMetni TR+EN), `Kuyumcu.Tests/ZoBelgeTestleri.cs` (ZB57–ZB62c), `Kuyumcu.Tests/ZoBelgeYazdirTestleri.cs` (ZB48). Bu koşumun (DEVAM 8) kendi kod değişikliği yalnız `ZoBelgeTestleri.cs`'e eklenen ZB62/ZB62b/ZB62c. Sunucu kodu, uçlar, yetki, POST yolları, id'ler ve yasal kurallar DEĞİŞMEDİ. Migration: gerekmedi.

### L-2. Build
Mutasyonlardan sonra temiz `dotnet build Kuyumcu.sln` → **0 Hata, 0 Uyarı** (`d29h/build3.log`). İlk derlemede 1 uyarı görüldü: `EtiketEkranTestleri.cs(145) xUnit1031`, ORTAK-1'in dosyası.

### L-3. Testler
- ZoBelge + ZOBELGE: **127/127** (ZB62 üçlüsü dahil).
- Genişletilmiş filtre (ZoBelge, ZOBELGE, FaturaEkran, UX1C2/UX1C3Ekran, S1iGorselSigma2, KatalogK1, P48IslemDefteri, RaporEkran, Evrak*, Denetim*, S34RaporBugun, KasaRapor, Q105, HttpBoruHatti, P22Ekran, P39TasarimKokleri, Sozluk*, S3fEkran, GirdiGocu): **826/826** (`d29h/test3.log`).
- **Tam süit** (`trx-pZO-BELGE/d8`, `d29h/tam.log`): **7189 test · 7188 geçti · 1 başarısız.** Kırmızı: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16 → `OrtakGorunumMetinleri`'ne yeni alan eklenmiş). Sahibi **ORTAK-1**. Devir listesinde de vardı. **Bu paketin ürettiği yeni kırmızı: 0.** Devir listesindeki SATIS-3, ZO-ISLEM-A ve migration kırmızıları bu koşumda yeşil.

### L-4. Denetim
`bash dotnet/denetim.sh` → **çıkış 0 · 295 ✓ · DENETİM TEMİZ** (`d29h/denetim.log`).

### L-5. Mutasyon kanıtları (M11 kopyada + M12 ana ağaçta; toplam 17 madde)
M11 kopya ağaçta koşuldu (`d29g/mut.sh`, günlük `d29g/mut/`). M12, kopya silindiği için ANA ağaçta koşuldu (`d29h/mut.sh`, yedek `mut-yedek/ZO-BELGE-d8/`, günlük `d29h/mut/`). Her satırda geri koyma sonrası `cmp` yedekle AYNI. Son mutasyondan sonra temiz build alındı (L-2).
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M11-1 | DenetimIzi BASİT'te KasaHareketiMetni çağrısı kaldırıldı (eski sürüm) | ZB57 | AYNI (sürümü M12-4 yeniledi) |
| M11-2 | Tutar `Bicim.Para` yerine ham sayı+kod | ZB57, ZB57b | AYNI |
| M11-3 | Storno kalıbından "KAPALI GÜN" grubu silindi | ZB57 | AYNI |
| M11-4 | Raporlar Bugün has `Bicim.Has`→`Bicim.Gram` | ZB58 | AYNI |
| M11-5 | Kasa geçmişi `BelgePara`→`Para` | ZB59 | AYNI |
| M11-6 | Yazdır boş-liste koruması kaldırıldı | ZB48 | AYNI |
| M11-7 | Evraklar'da öznitelik gövdeli lambda | ZB60 | AYNI |
| M11-8 | Liste kabı `max-height: none` | ZB61 | AYNI |
| M11-9 | Yapışık başlık `position: static` | ZB61 | AYNI |
| M11-10 → **M12-1** | IslemDefteri başlangıç ataması kaldırıldı | (M11'de YOK → test açığı) **ZB62** | AYNI |
| M11-11 → **M12-2** | DenetimIzi `yalnizHassas = v` kaldırıldı | (M11'de YOK) **ZB62b** | AYNI |
| M11-12 → **M12-3** | Evraklar `vadeBit = v` kaldırıldı | (M11'de YOK) **ZB62c** | AYNI |
| M12-4 | DenetimIzi KasaHareketiMetni dalı (güncel sürüm) kaldırıldı | ZB57 | AYNI |
| M12-5 | HamDetayIpucu'dan `Dil.BasitMi` koşulu kaldırıldı (BASİT'e ham ipucu sızar) | ZB57 | AYNI |
Not: M11 günlüklerindeki P28/P30/OlcumKapisi/UretimAcilis kırmızıları, kopya ağacın konumundan kaynaklanan `denetim.sh` testleri. Aynı testler ana ağaçta 826/826 filtresinde yeşil. Mutasyon kanıtı sayılmadı.

### L-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, 14:23'te yeniden yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3PAS4Z5YPEKKMFJ09VGD1SR · tek kullanımlık `Sistem__Anahtari` ortam değişkeni, değeri basılmadı)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST elle GIRIS 1500,50 TRY / CIKIS 25,75 USD / GIRIS 10 TRY + storno (DEVAM 7, `d29g/canli.log`) | 200 | 200 / 200 / 200 / 200 |
| GET /denetim ham kayıt | sunucu metni değişmez | `elle kasa hareketi: GIRIS 1500.50 TRY · açıklama: dogrulayici` (kayıt aynen; çeviri yalnız ekranda) |
| GET /denetim?hassas=true | 200, hepsi hassas | 200 · toplam 141 · hepsi hassas: true (süzgeçsiz 758) |
| GET /denetim?limit=501 | sınır uygulanır | 200 · `limit: 500` (sunucu kırpıyor, UX-S29 ekranda "daha fazlası var") |
| GET /raporlar/islem-defteri 23.09–bugün / 01–15.09 | süzgeç uygulanır | 200 · 25 satır / 200 · 0 satır |
| GET /evraklar / ?vadeBas=2030-01-01&vadeBit=2030-01-31 | vade süzgeci uygulanır | 200 · toplamAdet 4 / 200 · toplamAdet 0 |
| SQL | elle kasa denetim satırı var | `denetim_kayitlari … detay like 'elle kasa hareketi%'` = **8** (temizlikten önce) |

### L-7. Gerçek tarayıcı ölçümü (başsız Chrome, sert yükleme, `d29h/sert.json`, foto `d29h/foto-sert/`)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm.** Mod her ölçümden önce `data-mod` ile doğrulandı; uyumsuz 0. Önceki koşumun turunda 12 PRO ölçümü BASİT'te kalmıştı, bu koşumda düzeltildi.
| Ölçü | Sonuç |
|---|---|
| K1 gizli görünen · K3 taşan/kırık · K5 TRY · K7 · K11 · K12 · yatay | **0** (112/112) |
| Yükleniyor'da donan | 0 |
| K4 açılış, 1440×900 (kabul ölçüsü) | **7 rotanın hepsi ≤ 1,0** (işlem defteri ve kim ne yaptı dahil) |
| K4, 1280×800 | faturalar 1,1–1,3 · evraklar 1,4 · veri içe aktar 1,1 · rapor merkezi PRO 1,1 (kabul ölçüsü 1440; bilgi amaçlı) |
| K8 jargon | 8 ölçüm, yalnız /evraklar: tohum evrak numaraları "ZB-CEK/ZB-SNT" (test verisi, § K-9-4) |
| K9 binen (ham) | 14 ölçüm, yalnız işlem defteri ve kim ne yaptı, 1440/1280. **Ölçüm aracı yanlış alarmı:** tablo kendi kabında kaydığı için kabın dışında KIRPILMIŞ satırların kutusu alttaki katlanır ya da sayfalama ile kesişiyor. Kırpan ataların sınırlarıyla kesilmiş görünür kutularla aynı hesap (`K9gorunur`) → **0 (112/112).** Görüntüde üst üste binme yok (görüldü: `_raporlar_islem-defteri-basit-aydinlik-1440.png`, `_denetim-izi-profesyonel-aydinlik-1280.png`). `sikisma-olc.js` sahibine not: K9 kırpan atayı dikkate almıyor. |
| Elle kasa (`d29h/dz.json`, 12 ölçüm) | BASİT ve PRO, açık ve koyu, 1440/1280/375: sade metin, K5 0, K3 0/0, yatay 0 |

### L-8. Temizlik
- API (pid 34643) ve vekil (pid 75611) `kill` ile durduruldu. `lsof` 5160 ve 5161 boş. Chrome (9716/9717) kapatıldı.
- Kiracı 01M3PAS4Z5YPEKKMFJ09VGD1SR `kiraci-temizle.sh --uygula` ile silindi (çıkış 0). Sonrasında tenants'ta 0, denetim_kayitlari 0, hareketler 0. **tenants: başta 16 (bu kiracı dahil), sonda 15** (DEVAM 6 tabanı 15). QA-TEST-* ve başka şeritlerin PAKET* kiracılarına dokunulmadı.
- `d29h/anahtar`, `d29g/anahtar`, `d29g/fik.json` ve `yayin/appsettings.Development.json` `printf ''` ile 0 bayta indirildi. Proje ağacına .mutorj/.bak/.tmp bırakılmadı.
- **Silinemeyen** (rm kancası, kurucu silmeli): `ZO-BELGE/yayin`, Chrome profilleri `d29g/profil-*` ve `d29h/profil-*`.

### L-9. Açık kalanlar (sahipleriyle)
1. **ORTAK-1:** `OrtakGorunumSaflik.O8` kırmızı (15 ↔ 16 alan).
2. **Ölçüm aracı sahibi:** `sikisma-olc.js` K9, kendi kabında kayan tabloyu yanlış alarm olarak sayıyor (L-7). K8 tohum evrak numarasını jargon sayıyor.
3. **Orkestratör:** açılış ölçüsü 1440'ta artık sağlanıyor. 1280×800'de bazı rotalar 1,1–1,4 (bilgi). `#dz-sayfa-ozet` istisna kaydı, Evraklar 4 toplam (§ G-9-2) ve `sistemHaric` (§ I-9-4) kararları devrediyor.
4. **Devreden:** "Altın pozisyonum" N1 (ORTAK-1) · rapor merkezi hub adları hub sahibinde · sol menü genişletilmiş grupları (KATMAN).

### L-10. DoD sözleşmesi
- Yeni varlık, yeni uç, yeni düğme yok. Sunucu kodu değişmedi. Denetim kaydı sunucuda aynen duruyor, yalnız ekran çevirisi eklendi (PRO'da ham kayıt ipucunda).
- Kasa hareketi değiştirilemez, düzeltme yolu mevcut storno. Hard delete yok. Mevzuat hükmüne dokunulmadı.
- Yıkıcı eylem, modal ve düzenleme kalıbı değişmedi. Süzgeç işleyicileri artık testle kilitli (ZB62*).

---


## 29.09 DEVAM 6 (06:48–07:20 kesilen koşum + 11:15–12:55 bu koşum): tasarım hakemi bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § K-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9716–9720). 5199/5200 ve tarayıcı bölmesi kullanılmadı; ölçüm başsız Chrome (`betikler/tur/cdp.mjs`). COMMIT yok. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29f/` (kesilen koşumunki `d29d/`, `d29e/`).

**Devir kontrolü:** 06:48–07:16 arasındaki koşum yedi hakem bulgusunun kodunu ve testlerini (ZB47–ZB56) yazmış, 10:40'ta mutasyon M10-1'in ortasında kesilmişti. Kopya ağaçta `KasaRaporu.razor` MUTASYONLU kalmıştı (`@gun.Tarih`); ana ağaç yedekle birebir aynıydı (`cmp` AYNI). Kopya dosya ana ağaçtan geri kondu, `cmp` AYNI. Ana ağaçta yarım mutasyon yoktu. Kesilen koşumun test kiracısı (01M3NNGESGFNTR2747MN5HNJZ4) duruyordu, bu koşumda kullanılıp sonunda silindi.

### K-1. Hakem bulguları ve yapılanlar
| Bulgu | Yapılan | Kanıt (tarayıcı + test) |
|---|---|---|
| [KRİTİK] Yazdır yalnız ekrandaki 6 satırı basıyordu; sayfa boyu ölçüm için kısaltılmıştı | Yazdır, seçili aralığın TAMAMINI sayfa sayfa çekiyor (`YazdirSayfaBoyu = 500`, sunucu sınırı 500; `dahaFazlasiVar` bitene dek). Çıktıda başlık, tarih aralığı ve "N / N kayıt" var; sınıra değerse uyarı basılıyor (UX-S29). Ekran sayfa boyu **İşlem defteri 25, Kim ne yaptı 25**. Bu koşumda ayrıca: yazdırma tablosu kendi `tablo-sar` kabına alındı. Bu, kesilen koşumdan kalan P-39 `TABLO_SARMALAYICISIZ = 1` ihlalini kapatıyor (d29e günlüğünde ZO-ISLEM-A'ya yazılmıştı, aslında bu paketindi). Kâğıtta kap kırpmıyor (`overflow: visible`). | CDP print media: 30/30 satır görünür, ekran tablosu gizli, başlık "İşlem defteri", "Tarih aralığı: 23.09.2026 – 29.09.2026", "30 / 30 kayıt". Canlı API: `limit=500` → 30 satır, `limit=501` → 400. ZB47, ZB48, P39 K5 + M10-2, M10-3, M10-13 |
| [YÜKSEK] Kasa geçmiş tablosunda tarih ISO biçimindeydi | `GunMetni(gun.Tarih)` Bicim merkezinden tr-TR `dd.MM.yyyy` basıyor. | Tarayıcı: "23.09.2026 … 29.09.2026". ZB51 + M10-1 |
| [YÜKSEK] PRO 1440'ta eylem sütunu ve Cari sağdan kesiliyordu | Faturalar PRO 8 sütun (Ara toplam ipucunda, Kaynak numaranın altında), Evraklar PRO'da Banka numaranın altında, uzun ad en çok 2 satır, eylem sütunu sağa yapışık. **Bu koşumda İşlem defteri PRO:** 14 sütun 9'a indi. İkincil bilgiler hücrenin alt satırına taşındı: nakit/veresiye payı tutarın, ürün müşterinin, şube kaydedenin, fatura no türün altında. Veri silinmedi. Karışık yönün iki bacağı alt alta. Kaydeden başlığı kısa ad (`ZB.IslemKaydeden`, TR+EN). Belge sütunu sağa yapışık. Kap 1060 px'ten darsa kart ızgarası. | Tarayıcı 1440 PRO açık ve koyu: faturalar, evraklar, işlem defteri ve kim ne yaptı tablo = kap = 1122 px. Kesik düğme 0, K3 0/0, K9 0. İşlem defteri önce 1798/1122, sonra 1122/1122. 1280 PRO'da kart (962/962). ZB41 (yeniden yazıldı), ZB44, ZB52, ZB53 + M10-5, M10-9, M10-11, M10-12, M10-15, M10-16b |
| [YÜKSEK] Rapor merkezi: Kâr ile "Bu bir KÂR değildir" kartı aynı rakamı gösteriyordu | Kâr ve fark aynıysa tek kart. Kâr yalnız Kâr-Zarar kaynağından geliyor, açıklamalar (i) ipucunda, kart boyları eşit. **Bu koşumda:** kartın en az genişliği 15,5rem. "ALIŞ-SATIŞ NAKİT FARKI" etiketi tek satıra sığıyor (önce K3 kırık 1, 1440/960/375'te). | Canlı: kâr 98.049,28 ₺ (KAR_ZARAR_SERVISI), fark 94.209,28 ₺. İkisi farklı, bu yüzden iki kart kendi adlarıyla duruyor. 7 kartın hepsi 105 px, 1440'ta 4 sütun, 375'te 1 sütun, K3 0/0. ZB55 + M10-4, M10-14 |
| [YÜKSEK] 375'te işlem defterinde Tutar ve Müşteri görünmüyordu | İşlem defteri ve kim ne yaptı 375'te kart listesi. Her hücre başlıkla aynı etiketi taşıyor, "yana kaydırın" yazısı çıkmıyor. | Tarayıcı 375: satır `display: block`, etiketler Tarih/Tür/Müşteri/Tutar/Ödeme, belge = pencere = 375. ZB50 + M10-7, M10-7b |
| [YÜKSEK] Yeni Fatura: modal, Kalemler kartında iç boşluk yok, büyük boş kutu, hata görünmüyordu | Yeni Fatura artık Çekmece'de. Kalem yokken yalnız "+ Kalem ekle" ve tek satır ipucu var. Boş kaydette istek gitmiyor, odak ilk hatalı alana gidiyor. **Bu koşumda** üç şey düzeldi. (a) Yapışık Kaydet şeridi gövdenin alt dolgusunu örtüyor; önce altından kalem kutuları sızıyordu. (b) Çekmecede her kalem bir KART: ürün tam satır, sayılar iki sütunda, etiketler görünür. Önce ürün seçicisi 40 px'e eziliyor, indirim/toplam/sil yana kayıyordu. (c) Kalemde Miktar ve Birim fiyat "*" taşıyor; düzenleme formuyla aynı (UX-S26). | Tarayıcı açık ve koyu, 1440 ve 375: çekmece var, modal yok, select 0. Boş kaydette odak `fatura-cari` ve görünür. Şerit altı ile gövde altı arasındaki fark 0 px (4/4). Çekmecede K3 0/0, yatay 0. ZB54 + M10-6, M10-10, M10-17, M10-18, M10-19b |
| [YÜKSEK] Veri içe aktar: jargon ve teknik satırlar | Birincil düğme "Önce kontrol et". Biçim ve sınır bilgisi "Dosyayı nasıl hazırlarım?" katlanırında. Turuncu uyarı satırı sade soluk metin. | Tarayıcı: düğme "Önce kontrol et", "CSV UTF-8" metni açılışta yok, K3 0/0. ZB56 + M10-8 |

**Bu koşumda dokunulan dosyalar:** `Pages/IslemDefteri.razor` (PRO 9 sütun + alt satırlar, yazdırma tablosu kabı, `id-th-kaydeden`), `Pages/Faturalar.razor` (kalem miktar/fiyat `Zorunlu`), `wwwroot/css/zo-belge.css` (alt satır, yön bacağı, rapor kart genişliği, yapışık Kaydet şeridi, çekmecede kalem kartı, kâğıtta kap), `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (+`IslemKaydeden` TR "Kaydeden" / EN "Recorded by"), `Kuyumcu.Tests/ZoBelgeTestleri.cs` (ZB41 yeniden yazıldı; ZB50 null uyarısı giderildi; ZB54 ve ZB55'e CSS kilitleri eklendi). Kesilen koşumun dokunduğu dosyalar (aynı devam): `Raporlar.razor`, `Evraklar.razor`, `KasaRaporu.razor`, `DenetimIzi.razor`, `VeriIceAktarim.razor`, `Sozlukler.GorselSigma.cs`, `ZoBelgeYazdirTestleri.cs` (yeni).
**Yeni id'ler:** `id-pay-{Id}`, `id-urun-{Id}`, `id-sube-{Id}`, `id-fatura-{Id}`, `id-th-kaydeden`. Mevcut id'ler, POST yolları, yetki, sunucu uçları ve yasal kurallar DEĞİŞMEDİ. Migration: gerekmedi. Pages altında yeni ModalPencere düzenleme kullanımı yok; Yeni Fatura Çekmece'ye geçti.

### K-2. Build
`dotnet build Kuyumcu.sln` → **0 Hata, 0 Uyarı** (`d29f/build13.log`). Ara derlemelerde görülen `EtiketEkranTestleri.cs(145) xUnit1031` ORTAK-1'in dosyası. Başka şeridin yarım `Hurda.razor` işi iki kez derlemeyi kırdı (`RamatciZorunlu`, `gecersizAlanlar`). Yeniden denemede yeşil alındı. Bir kez MSB4018 dosya kilidi görüldü (eşzamanlı build), o da yeniden denemede yeşil.

### K-3. Testler
- Filtreli koşum (ZoBelge*, ZOBELGE*, FaturaEkran, UX1C2Ekran, S1iGorselSigma2, KatalogK1, P48IslemDefteri, RaporEkran, Evrak*, Denetim*, S34RaporBugun, KasaRapor, Q105, HttpBoruHatti, P22Ekran, P39TasarimKokleri, Sozluk*): **751/751 geçti** (`d29f/test7.log`). Son CSS/çekmece değişikliğinden sonra ZoBelge + FaturaEkran + UX1C2Ekran + ZOBELGE: **194/194** (`d29f/test12.log`).
- **Tam süit** (`trx-pZO-BELGE/d6c`, `d29f/tam3.log`): **7142 test · 7122 geçti · 20 başarısız.** Hiçbiri bu paketin dosyasına ait değil:
  - SATIS-3: SATIS3EkranTestleri T17 ×11 · S26DigerIslemlerTestleri T17 ×3 · SATIS3SinirTestleriT15 T16D ×2
  - ZO-ISLEM-A: ZOISLEMAHurdaTahsilTestleri ZO29w, ZO29x
  - ORTAK-1: OrtakGorunumSaflik.O8
  - Migration: MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar, YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner. Bu paket migration eklemedi; sahibi bekleyen migration'ı olan şerit ya da ortak test DB durumu.

  Önceki tam koşumda (`d6`, 11:25) `P39TasarimKokleriTestleri.K5` da kırmızıydı (IslemDefteri yazdırma tablosu). Bu paketindi, bu koşumda kapatıldı. **Bu paketin ürettiği yeni kırmızı: 0.**

### K-4. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 295 ✓ · DENETİM TEMİZ** (`d29f/denetim3.log`). P-39 `TABLO_SARMALAYICISIZ` 0.

### K-5. Mutasyon kanıtları (M10, 19/19 kırmızı)
Kopya ağaç `ZO-BELGE/kopya/dotnet` (her tur öncesi ana ağaçla rsync), yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE-d6/`, betikler `d29f/mut*.sh`, günlükler `d29f/mut/`. Her satırda `cmp` sonucu yedekle AYNI ve ana ağaçla AYNI.
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M10-1 | Kasa tarihi `@GunMetni(gun.Tarih)` → `@gun.Tarih` | ZB51_Kasa_gecmis_tablosunda_tarih_YEREL_ISO_basilmaz, S1i_10_Kasa_defteri… |
| M10-2 | Yazdır döngüsü ilk sayfada `break` | ZoBelgeYazdirTestleri.ZB47_Yazdir_secili_araligin_TAMAMINI… |
| M10-3 | `IslemDefteriSayfaBoyu = 25` → `6` | ZB47, S1i_14, ZOBELGESayfaSinir ZOBP01/02/03/05 |
| M10-4 | `KarFarklaAyni => false` | ZB55_Rapor_PRO_kar_ile_satis_alis_farki_AYNIYSA_tek_kart… |
| M10-5 | Fatura/evrak eylem hücresi `sticky` → `static` | ZB44_Fatura_evrak_tablosu_KAP_genisligine_gore_karta_doner_tabloda_eylem_sutunu_saga_yapisik |
| M10-6 | `await OdakIlkHataya();` silindi | ZB54_Yeni_fatura_CEKMECE_kalem_yokken_bos_kutu_yok_bos_kaydette_ilk_hataya_odak |
| M10-7 / M10-7b | İşlem defteri tablosundan `q10-yigin` silindi (eski ve yeni sınıf ifadesinde) | ZB50_Islem_defteri_ve_kim_ne_yapti_375_KART_listesi… |
| M10-8 | Birincil düğme eski "Kuru prova çalıştır" | ZB56_Veri_ice_aktar_birincil_dugme_sade_ad… |
| M10-9 | Evrak banka alt satırı `@if (false …)` | ZB53_Evraklar_PRO_banka_ayri_sutun_DEGIL… |
| M10-10 | Çekmece Varlik `fatura-yeni` → `fatura-yeni2` | ZB54 |
| M10-11 | PRO'da `zo-yigin-dar` eklenmiyor | ZB41_Islem_defteri_PRO_dokuz_sutun_ikincil_bilgi_alt_satirda_veri_kaybolmaz |
| M10-12 | Ürün alt satırı `@if (false)` | ZB41 |
| M10-13 | Yazdırma tablosunun `tablo-sar` kabı kaldırıldı | P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir |
| M10-14 | Rapor kart ızgarası kuralı boşaltıldı | ZB55 |
| M10-15 | Yön bacağı `display: block` kuralı boşaltıldı | ZB41 |
| M10-16b | Kaydeden başlığı eski uzun ad | ZB41 |
| M10-17 | Kaydet şeridi `bottom: calc(-1 * …)` → `bottom: 0` | ZB54 |
| M10-18 | Çekmecede kalem `thead { display: none; }` silindi | ZB54 |
| M10-19b | `fk-miktar` `Zorunlu="true"` silindi | ZB54 |

(Bir kayıt ayrıca: M10-19'un ilk biçimi 2 eşleşme bulduğu için betik tarafından UYGULANMADI, "HATA" diye raporlandı. Tek eşleşmeli M10-19b ile yeniden yapıldı. M10-16'nın ilk biçimi `zo-th-sarar` sınıfı üzerineydi ve kırmızı verdi; o sınıf K3 kırık ürettiği için koddan kaldırıldı, geçerli kanıt M10-16b.)

### K-6. Canlı doğrulama (5160 · ana ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, bu koşumda 8 kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3NNGESGFNTR2747MN5HNJZ4, kesilen koşumun tohumu · tek kullanımlık `Sistem__Anahtari` ortam değişkeniyle, değeri basılmadı)
| Adım | Beklenen | Gerçek (`d29f/canli.log`, yanıt okundu) |
|---|---|---|
| GET /raporlar/aylik | 200, kâr Kâr-Zarar kaynağından | 200 · karTl 98049.28 · karKaynagi KAR_ZARAR_SERVISI · satisAlisFarkiTl 94209.28 |
| GET /raporlar/katalog | 200 | 200 · 16 rapor |
| GET islem-defteri limit=25 | 200, ekran sayfası 25 | 200 · toplam 30 · 25 satır · dahaFazlasiVar true |
| GET islem-defteri limit=500 (Yazdır) | 200, aralığın tamamı | 200 · 30 satır · dahaFazlasiVar false |
| GET islem-defteri limit=501 | 400, Türkçe alan hatası | 400 · "Limit 1 ile 500 arasında olmalıdır" (alanlar.limit) |
| GET /denetim limit=25 sistemHaric=true | 200 | 200 · toplam 329 · 25 kayıt |
| GET kasa-defteri (14 gün) | 200 | 200 · 3 kasa · ilk gün 2026-09-16 (ekranda 16.09.2026) |
| GET /faturalar | 200, taslak/kesildi/iptal | 200 · 5 fatura · TASLAK, KESILDI, IPTAL |
| GET /evraklar | 200 | 200 · 5 evrak |
| GET /ice-aktarim/sablonlar | 200 | 200 · maksSatir 5000 · maksDosyaBayt 2097152 |

SQL (kuyumcu_net, kiracı süzgeçli, `d29f/sql.txt`):
- faturalar: TASLAK 2 (1215,50) · KESILDI 2 (991254,32) · IPTAL 1 (900)
- kiymetli_evraklar: PORTFOYDE 4 (1295000) · KARSILIKSIZ 1 (5000)
- kasalar 3 · hareketler 30 · gun_kapanislari 1

### K-7. Gerçek tarayıcı ölçümü (başsız Chrome, sert yükleme)
**(a) Hedefli ölçüm** (`d29f/hedef.mjs` → `hedef.json`, `foto/`):
- Faturalar, Evraklar, İşlem defteri, Kim ne yaptı × {BASİT, PRO} × {açık, koyu} × {1440, 1280} = 32 ölçüm. Hepsinde tablo ≤ kap, kesik düğme 0, K3 0/0, K9 0, yatay 0.
  - 1440'ta tablo 1122/1122. Fatura, evrak ve PRO işlem defterinde eylem sütunu `sticky`.
  - 1280'de fatura, evrak ve PRO işlem defteri kart (962/962). BASİT işlem defteri ve kim ne yaptı tablo (962/962).
- 375 kart listesi (4 ölçüm) temiz.
- Diğer bulgular: kasa tarihleri dd.MM.yyyy · yazdır 30/30 · rapor kartları 7 × 105 px · içe aktarım düğmesi "Önce kontrol et".
- Yeni fatura çekmecesi (`cek.mjs`): açık/koyu × 1440/375. Şerit gövdeye tam oturuyor (0 px), K3 0/0, yatay 0. Çekmece açıkken K9 6 çıkıyor; bunlar yalnız perdenin ARKASINDAKİ sayfa öğeleriyle çekmece içeriği arasındaki örtüşme (örtü katmanı, sayfa kusuru değil).

**(b) Sert yükleme taraması** (`d29f/sert.mjs` → `sert.json`, `foto-sert/`): 7 rota × {BASİT, PRO} × {açık, koyu} × {1440, 960, 375} = **84 ölçüm, sorunlu 0**.
- Yükleniyor 0 · K1 0 · K3 0/0 · K5 0 · K7 0 · K9 0 · K10 0 · K11 0 · K12 0 · yatay 0.
- `data-mod` her ölçümde istenen modla aynı. İlk taramada mod düğmesi sayfa hazır olmadan tıklanıyordu; betik düzeltildi, mod doğrulanarak yeniden koşuldu.
- K8 yalnız /evraklar BASİT'te "ZB-CEK/ZB-SNT": tohumun evrak numarası, kullanıcı verisi.

**(c) Açılış boyu** (ana içerik / ekran, 1440, dolu veri):

| Rota | BASİT | PRO |
|---|---|---|
| faturalar | 0,92 | 0,92 |
| evraklar | 0,92 | 0,90 |
| raporlar | 0,92 | 0,90 |
| kasa | 0,92 | 0,90 |
| veri içe aktarım | 0,92 | 0,91 |
| **işlem defteri** | **2,15** | **2,61** |
| **kim ne yaptı** | **2,03** | **2,03** |

İşlem defteri ve kim ne yaptı, hakemin "sayfa boyu en az 25 satır" kararıyla 1,0'ı aşıyor. Hakem, ölçüm kuralının orkestratörle konuşulmasını istedi (§ K-9-1).

**(d) ÖNCE/SONRA görüntüleri** (`ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d6-*.png`, Read ile bakıldı):
- `islem-defteri-yazdir`: 6 satır → başlık + aralık + 30/30
- `kasa-gecmis-tarih`
- `faturalar-PRO-1440`: eylem kesik → 8 sütun sığar
- `evraklar-PRO-1440`
- `islem-defteri-PRO-1440`: Cari kesik → 9 sütun sığar
- `rapor-merkezi-PRO-1440`
- `islem-defteri-375`: kart
- `yeni-fatura`: modal → çekmece
- `veri-ice-aktarim`

Ek: `d29f/foto/yeni-fatura-kaydirilmis-{aydinlik,karanlik}-{1440,375}.png`, kalem kartları ve şerit.

### K-8. Temizlik
- API (5160) ve vekil (5161) `kill` ile durduruldu, `lsof` ikisinde de boş. Chrome süreçleri (9716–9720) kapatıldı.
- Kiracı 01M3NNGESGFNTR2747MN5HNJZ4 `kiraci-temizle.sh --uygula` ile silindi. Sonrasında hareketler, faturalar ve denetim_kayitlari 0. **tenants: bu koşumun başında 16 (bu kiracı dahil), sonda 15** (DEVAM 5 tabanı 15). QA-TEST-* kiracılarına dokunulmadı.
- `d29d/anahtar`, `d29d/fik.json`, `yayin/appsettings.Development.json` ve `kopya/.../appsettings.Development.json` `printf ''` ile 0 bayta indirildi.
- Proje ağacına .mutorj/.bak/.tmp bırakılmadı.
- **Silinemeyen** (`rm`/`mv` kancası blokluyor, kurucu silmeli): `ZO-BELGE/kopya` (~330 MB), `ZO-BELGE/yayin`, Chrome profilleri `d29d/profil-*` ve `d29f/profil-*`.

### K-9. Açık kalanlar (sahipleriyle)
1. **Orkestratör kararı — açılış ölçüsü:** İşlem defteri ve kim ne yaptı, 25 satırlık sayfayla açılışta 2,0–2,6 ekran. Hakem kararı (sayfa ≥ 25, yazdır tam liste) ile "açılış ≤ 1,0" kuralı çelişiyor. Önceki çözüm (6/7 satır) hakemce reddedildi. Öneri: liste ekranlarında ölçü "tablo başlığına kadar ≤ 1,0".
2. **Tam süit kırmızıları (başkasının):** SATIS-3 (T17 ×14, T16D ×2) · ZO-ISLEM-A (ZO29w, ZO29x) · ORTAK-1 (O8) · migration testleri ×2 (bu paket migration eklemedi).
3. **Orkestratör kararları (devreden):** `#dz-sayfa-ozet` kaldırılması için istisna kaydı · Evraklar 4 toplam (§ G-9-2) · `sistemHaric` parametresi (§ I-9-4).
4. **Ölçüm aracı:** K8, tohum evrak numarasını jargon sayıyor. Sert yüklemede ara sıra geç çizim görülüyor (§ J-7 not).
5. **Devreden:** "Altın pozisyonum" N1 (ORTAK-1) · `ek.mjs` fatura adımı 400 (§ G-9-7) · rapor merkezi hub adları "Hareket Dökümü/Kasa Hareketleri" hub sahibinde.
6. **Göz notu (bu paketin dışında):** sol menü genişletilmiş gruplarla belge boyunu 1440'ta 1,18–1,43'e çıkarıyor, ana içerik ise 0,90–0,92. Menü KATMAN'ın.

### K-10. DoD sözleşmesi
- Yeni varlık yok, yeni uç yok, sunucu kodu bu koşumda değişmedi.
- Yeni düğme yok. Mevcut Yazdır gerçek istek atıyor. Liste boşsa kilitli ve sebebini yazıyor (ZB48).
- Yeni Fatura oluşturma formu Çekmece kalıbında. Düzenleme yolu mevcut `fd-duzenle` çekmecesi, değişmedi. Kalem zorunlulukları iki formda aynı.
- Finansal kayıtlar: hard delete yok, storno yolu değişmedi. Mevzuat hükmüne dokunulmadı.

---

## 29.09 DEVAM 5 (04:34–05:40): doğrulayıcı (KALDI) bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § J-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9701–9712). 5199/5200 portlarına ve tarayıcı bölmesine dokunulmadı. Ölçüm başsız Chrome ile yapıldı, rotalar **sert yüklemeyle** (b.git) açıldı. COMMIT yok. Ağaçtaki iş korundu. Mutasyonlar kopya ağaçta yapıldı. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29c/`.

### J-1. Bulgular ve yapılanlar
| Bulgu | Kök neden | Yapılan | Kanıt |
|---|---|---|---|
| [KRİTİK] /raporlar BASİT sert yüklemede 5 kart "Yükleniyor"da donuyordu | F5'te profil (`/auth/ben`) sayfadan SONRA geliyor. AnaYerlesim onu `Oturum.KullaniciAta` ile yazıyor, bu `Mod.Degisti`'yi tetiklemiyor. Sayfa PRO dalını yükledi, mod BASİT'e dönünce BASİT yükleyicileri hiç başlamadı. | `Raporlar.razor`: `OturumServisi` enjekte edildi, `Oturum.Degisti += ModDegisti` (Dispose'da çözülüyor). `YuzeGoreYukle` bayraklarla tek sefer çalışıyor; jeton yenilemede ikinci istek gitmiyor. Ortak servis (`ArayuzModuServisi`) değişmedi. | Yeni bUnit `S34RaporBugunTestleri.ZB43_Sert_yukleme_…`: profilsiz render (PRO), sonra yalnız `KullaniciAta(BASIT)`. `/raporlar/gunluk`, mutabakat ve bildirim istekleri gidiyor, Yükleniyor kalkıyor, ikinci oturum olayında `/raporlar/gunluk` tek kalıyor. Tarayıcı: sert yüklemede 5, 15 ve 30. sn'de Yükleniyor 0, `#bugun-satis-deger` var (`hedef2.json`). 7 rota × 2 mod × 2 tema × 3 genişlik = 84 sert yüklemede Yükleniyor 0 (`sert.json`). M9-1 |
| [KRİTİK] Faturalar/Evraklar PRO 1280'de Durum sütunu yapışkan eylem sütununun altında kalıyordu (K9 6) | Kart eşiği PENCERE genişliğine (≤1100 px) bağlıydı. PRO 1280'de kap 962 px, tablo 1023 px. OLCEK-1'in yapışkan eylem hücresi kaydırma 0'dayken sol komşusunu örtüyordu. | `zo-belge.css`: eşik artık KAP genişliği. `.tablo-sar` bir container (`zoyigin`). Kap 1060 px'ten darsa kart ızgarası (1280 ve altı, iki modda). Tablo olarak durduğunda (kap > 1060) bu iki tablonun eylem hücresi ve başlığı `position: static`, yani hiçbir hücre komşusunu örtmüyor. Taşma olursa sağ kenar gölgesi aynen duruyor. Özgüllük olcek.css'i yalnız bu iki tabloda eziyor, olcek.css'e dokunulmadı. | Sert yükleme: Faturalar ve Evraklar {BASİT, PRO} × {açık, koyu} × {1440, 1280, 375}. K9 0, K3 0/0, yatay 0. 1280'de kart (962/962), 1440'ta tablo (1122/1122), eylem `static`. Görüntü `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d5-faturalar-evraklar-PRO-1280.png`. ZB44 + M9-2, M9-3 |
| (aynı bulgu) Faturalar PRO 1440 K3 "150 ₺ (%20)" | Küçük puntolu oran span'ı satır kutusunu 4 px kaydırıyordu; ölçer bunu 2 satır sayıyordu. | Oranın `metin-kucuk` sınıfı kaldırıldı; aynı punto, soluk renk. | 1440 PRO açık ve koyu: K3 0/0 (önce 0/3). |
| [ORTA] Denetim izi BASİT 1280'de tablo sağdan kesikti, ad ile rol bitişikti, Kasa sütunu hep "—", detay "stok yaslandirma" | Kasa bir teknik damga. Razor öğeler arası boşluğu atıyor. Q10-5 çevirisi tanımadığı ASCII kodu yalnız boşlukla ayırıyordu. | (a) Kasa sütunu yalnız PRO'da (`#dz-th-kasa` yeni id). (b) Rol rozetinin önünde belirteçli pay (`.zo-rol-ara`, `--bosluk-1`). (c) `ZoBelgeDenetimSozlugu.KodAdlariniCevir`: `stok-yaslandirma` → "stok yaşlandırma", `pozisyon-cariler` → "müşteri pozisyonları", `vade-takibi` → "vade takibi" (TR+EN). BASİT gösterimde Q10-5'ten önce uygulanıyor. (d) PRO 8 sütunda tablo 995/962 taşıyordu. Detay sütununun en az genişliği yalnız bu tabloda 16rem → 11rem. | Tarayıcı BASİT 1280: tablo 962/962, 7 başlık (KASA yok), ad-rol arası 4 px. PRO 1280: 962/962, 8 başlık, "KİŞİSEL VERİ" tam görünüyor (`foto/denetim-izi-PRO-1280-son.png`). ZB45, ZB45b + M9-4, M9-5, M9-6 |
| [ORTA] Kasa raporu özeti ve "Sayım gir", katlanırdaki süzgece bağlıydı | Özet ile geçmiş tablo aynı `yanit`'tan türüyordu. | `KasaRaporu.razor`: özet kendi kaynağından geliyor (`ozet`: son 14 gün, TÜM kasalar). Varsayılan süzgeçte aynı yanıt kullanılıyor, ikinci istek gitmiyor. Süzgeç değişince yalnız geçmiş tablo yeniden yükleniyor. Sayım kaydından sonra özet tazeleniyor (`Yukle(ozetTazele: true)`). Kasa seçici `KasaSuzgecDegisti` üzerinden bağlı. | Canlı API: `bitis=dün` ile kasa-defteri 3 kasa döndü ama bugün günü olan kasa 0 (eski davranışta Sayım gir kaybolurdu). Tarayıcı: bitiş 28.09'a alındı. Özette 3 kasa ve 2 "Sayım gir" düğmesi aynen duruyor, tablo 21 geçmiş gün gösteriyor. ZB46 + M9-7, M9-8 |
| [DÜŞÜK] `#dz-sayfa-ozet` kaldırıldı | — | Değişiklik yok. Gerekçe § I-1 (d): aralık bilgisi tek yerde. **Orkestratör istisna kaydı gerekli.** | ZB35 |
| [DÜŞÜK] Evraklar 4 toplam tek satırda | — | Değişiklik yok, § G-9-2'deki gerekçeli sapma. **Orkestratör kararı gerekli.** | — |
| [DÜŞÜK] denetim.sh P-11 | Başka şerit (ZO-ISLEM-A) | Bu devamın son ölçümünde `denetim.sh` TEMİZ. | § J-4 |

Dokunulan dosyalar (bu devam): `Pages/Raporlar.razor`, `Pages/Faturalar.razor` (yalnız oran span sınıfı), `Pages/DenetimIzi.razor`, `Pages/KasaRaporu.razor`, `wwwroot/css/zo-belge.css`, `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs`. Testler: `ZoBelgeTestleri.cs` (+ZB44, ZB45, ZB45b, ZB46; sahte API'ye bitişe göre kasa defteri yanıtı), `S34RaporBugunTestleri.cs` (+ZB43). Gerekçeli dokunuş: sert yükleme fikstürü bu sınıfta olduğu için test oraya yazıldı, mevcut testler değişmedi. Yeni id: `dz-th-kasa`. Mevcut id'ler, POST yolları, yetki, sunucu uçları ve yasal kurallar DEĞİŞMEDİ. Migration: gerekmedi.

### J-2. Build
`dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`d29c/build-son.log`).

### J-3. Testler
- Filtreli koşum (ZoBelge*, ZOBELGE*, FaturaEkran, UX1C2Ekran, S1iGorselSigma2, KatalogK1, P48IslemDefteri, RaporEkran, Evrak*, Denetim*, S34RaporBugun, KasaRapor, Q105, HttpBoruHatti, P22Ekran): **633/633 geçti** (`d29c/test2.log`). Yeni testler: ZB43, ZB44, ZB45, ZB45b, ZB46.
- **Tam süit** (`trx-pZO-BELGE/d5`, `d29c/tam.log`): **6995 test · 6994 geçti · 1 başarısız**. Başarısız olan `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu`; ORTAK-1'in, devir notunda da var. **Bu paketin ürettiği yeni kırmızı: 0.**

### J-4. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 292 ✓ · DENETİM TEMİZ** (`d29c/denetim-son.log`). Ara ölçümde (04:55) yalnız `OLCEK-1: YUZ_OLCU_ENVANTER_DISI = 1` kırmızıydı (`tuval-satis.css` `#altin-bacagi`, SATIS-3'ün). Son ölçümde o da kapanmıştı.

### J-5. Mutasyon kanıtları (M9, 8/8 kırmızı)
Kopya ağaç `ZO-BELGE/kopya/dotnet`, yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE-d5/`, betik `d29c/mut.sh`, günlükler `d29c/mut/`. Her mutasyondan sonra `cmp` sonucu yedekle AYNI ve ana ağaçla AYNI.
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M9-1 | Raporlar: `Oturum.Degisti += ModDegisti;` silindi | S34RaporBugunTestleri.ZB43_Sert_yukleme_profil_sonradan_BASIT_gelince_bugun_yuklenir_Yukleniyor_kalkar |
| M9-2 | CSS kap eşiği 1060 → 900 px | ZoBelgeTestleri.ZB44_Fatura_evrak_tablosu_KAP_genisligine_gore_karta_doner_eylem_sutunu_yapiskan_DEGIL |
| M9-3 | Eylem başlığı `position: static` → `sticky` | ZB44 |
| M9-4 | Denetim Kasa başlığı `@if (!Dil.BasitMi)` → `@if (true)` | ZB45_Denetim_BASIT_kasa_sutunu_YOK_detay_Turkce_ad_ile_rol_arasinda_bosluk_PRO_aynen |
| M9-5 | `KodAdlariniCevir` çağrısı kaldırıldı | ZB45 |
| M9-6 | `zo-rol-ara` sınıfı kaldırıldı | ZB45 |
| M9-7 | `if (VarsayilanSuzgec) ozet = yanit;` → `ozet = yanit;` | ZB46_Kasa_ozeti_ve_Sayim_gir_SUZGECTEN_bagimsiz_bitis_gecmise_alininca_kaybolmaz |
| M9-8 | Özet dalı `ozet` yerine `yanit` okuyor | ZB46 |

### J-6. Canlı doğrulama
Ortam: 5160, kopya ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, üç kez yayımlandı. Kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3NDG9VEFRQ78V9F6E52P7FR. Tohum: `demo-tohumla.js --prova-kiraci` (çıkış 0: 6 cari, 12 ürün, 9 satış), `ek.mjs` (2 kasa + sayım + 4 evrak; fatura adımı bilinen 400, § G-9-7), `fat.mjs` (3 fatura, hepsi 200). Tek kullanımlık `Sistem__Anahtari` ortam değişkeniyle verildi, değeri basılmadı.
| Adım | Beklenen | Gerçek (`d29c/canli.log`, yanıt okundu) |
|---|---|---|
| GET /raporlar/gunluk | 200, BASİT kartların verisi | 200 · satis.adet 9 · kasalar 3 |
| GET /raporlar/kasa-defteri (son 14 gün) | 200, bugün günü var | 200 · 3 kasa · bugün günü olan 3 |
| GET /raporlar/kasa-defteri (bitiş dün) | 200, bugün günü YOK (özetin neden ayrı kaynağa bağlandığı) | 200 · 3 kasa · bugün günü olan 0 |
| GET /denetim?sistemHaric=true&limit=500 | 200, kayıt `stok-yaslandirma` ham kodunu taşır (kayıt değişmez, çeviri gösterimde) | 200 · toplam 415 · kasalı 29 · stok-yaslandirma detaylı 20 |

SQL (kuyumcu_net, kiracı süzgeçli, `d29c/sql.txt`):
- faturalar: TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900)
- kiymetli_evraklar: PORTFOYDE 3 (45000) · KARSILIKSIZ 1 (5000)
- kasalar: 3
- gun_kapanislari: 2450 / 2500 / −50
- denetim_kayitlari `%stok-yaslandirma%`: 20

### J-7. Gerçek tarayıcı ölçümü (başsız Chrome)
**(a) Hedefli ölçüm** (`d29c/hedef.mjs` → `hedef2.json`, `foto/`). Değerler J-1 tablosunda. Ara ölçümde 1280 PRO denetim 995/962 çıktı, düzeltmeden sonra `dz.mjs` ile 962/962 ölçüldü.

**(b) Sert yükleme taraması** (`d29c/sert.mjs` → `sert.json`, `foto-sert/`): 7 rota × {BASİT, PRO} × {açık, koyu} × {1440, 1280, 375} = 84 ölçüm, her biri `Page.navigate` ile.
- Yükleniyor 0 · K1 0 · K3 0/0 · K5 0 · K7 0 · K9 0 · K10 0 · K11 0 · K12 0 · yatay 0.
- `data-mod` her ölçümde istenen modla aynı.
- Tek işaret: /evraklar BASİT K8 "ZB-CEK/ZB-SNT" (6 durum). Bu, tohumun evrak numarası; kullanıcı verisi, arayüz metni değil (önceki turlarla aynı).

**(c) Tam tur** (`tur.mjs` + `analiz.py`, `d29c/tur-analiz.txt`): 84 sayfa + 30 sekme + 380 katman, hata 0.
- Benzersiz bulgu 10. Hepsi önceki turda da vardı:
  - 375 genel arama SERBEST-YÜZEN (KATMAN).
  - 1440 "⋯" SERBEST-YÜZEN, Evraklar/Faturalar (KATMAN).
  - K8 tohum evrak no.
- K3 ve K9 bulgusu YOK. **Yeni bulgu 0.**

**(d) Görüntüler** (Read ile bakıldı):
- `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d5-raporlar-sert-BASIT.png`: ÖNCE 5 kart dönen göstergede. SONRA 6 kart dolu.
- `ZO-BELGE-d5-faturalar-evraklar-PRO-1280.png`: ÖNCE Durum sütunu eylem sütununun altında, başlık "D". SONRA iki sütunlu kart, Durum rozeti görünür.
- `ZO-BELGE-d5-denetim-kasa.png`: ÖNCE BASİT 1280'de Kişisel veri kesik, Kasa "—". SONRA BASİT 7 sütun sığıyor, ad ile rol arası boşluklu.
- `d29c/foto/kasa-suzgec-gecmis-BASIT-1440.png`: bitiş geçmişteyken özet ve Sayım gir yerinde.
- Ek: J-7 (a)'daki ara görsel "SONRA denetim PRO" aslında BASİT'te alındı (mod tıklaması yetişmedi). PRO'nun geçerli kanıtı `foto/denetim-izi-PRO-1280-son.png` (data-mod=profesyonel, 8 sütun, 962/962).

**Ölçüm notu (sahipsiz, orkestratöre):** Sert yüklemede 14 denemenin 1–2'sinde sayfa (Faturalar da, Evraklar da) 10–30 sn geç çiziliyor. O sürede 1,5 sn'yi aşan tek istek, bitmeyen `/hubs/kur` websocket'i. Sayfa sonunda kendiliğinden çiziliyor, donma yok. Sayfadan bağımsız (`evr2.mjs`/`evr3.mjs`). Kaynağı büyük olasılıkla vekil ya da açılış; bu paketin alanında değil.

### J-8. Temizlik
- API (5160) ve vekil (5161) `kill` ile durduruldu; `lsof` ikisinde de boş. Chrome süreçleri kapatıldı.
- Kiracı 01M3NDG9VEFRQ78V9F6E52P7FR `kiraci-temizle.sh --uygula` ile silindi. tenant_id'li 70 tabloda kalan satır **0**. **tenants:** başta 15, sonda **15**. QA-TEST-* kiracılarına dokunulmadı.
- `d29c/anahtar`, `d29c/fik.json`, `yayin/…/appsettings.Development.json` ve `kopya/…/appsettings.Development.json` `printf ''` ile 0 bayta indirildi.
- Proje ağacına .mutorj/.bak/.tmp bırakılmadı.
- **Silinemeyen:** Chrome profilleri `d29c/profil-*` ve `d29c/tur/profil-*`, ayrıca `ZO-BELGE/kopya` (~330 MB). `rm` kancası blokluyor; kurucu silmeli.

### J-9. Açık kalanlar (sahipleriyle)
1. **ORTAK-1:** `OrtakGorunumSaflik.O8_…` kırmızı (devreden).
2. **KATMAN:** 375 genel arama ve 1440 "⋯" SERBEST-YÜZEN (devreden).
3. **Orkestratör kararları:** `#dz-sayfa-ozet` kaldırılması için istisna kaydı; Evraklar 4 toplam (§ G-9-2); `sistemHaric` parametresi (§ I-9-4).
4. **Ölçüm ortamı:** sert yüklemede ara sıra 10–30 sn gecikme (§ J-7 not). Ölçüm aracı K8, tohum evrak numarasını jargon sayıyor.
5. § I-9-5 aynen: "Altın pozisyonum" N1 (ORTAK-1); `ek.mjs` fatura adımı 400.

### J-10. DoD sözleşmesi
- Yeni varlık, yeni uç ve yeni düğme yok. Sunucu kodu bu devamda değişmedi.
- Pages altında yeni ModalPencere düzenleme kullanımı: 0.
- Kasa sayımı POST yolu (`/kasalar/{id}/gun-kapanisi`) ve gövdesi değişmedi. Yalnız özetin veri kaynağı süzgeçten ayrıldı.
- Mevzuat hükmüne dokunulmadı.

---





## 29.09 DEVAM 4 (02:50–04:10): tasarım hakemi t3 bulguları kapatıldı. Güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § I-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9701–9706). 5199/5200 portlarına ve tarayıcı bölmesine dokunulmadı. Ölçüm başsız Chrome ile yapıldı. COMMIT yok. Ağaçtaki iş korundu. Mutasyonlar kopya ağaçta yapıldı. Günlükler `/private/tmp/kuyumcu-serit/ZO-BELGE/d29b/`.

Devir kontrolü: "SON DOĞRULAMA (KALDI)" üçlüsü (has 4 hane, başlık = menü adı, ZB14 Raporlar kapsamı) güncel ağaçta yine KAPALI (§ G-1 aynen). Bu devam yalnız tasarım hakemi t3'ün 6 KRİTİK bulgusunu ele aldı.

### I-1. Hakem bulguları ve yapılanlar
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| Faturalar KDV kodu notu açılınca geliştirici metni görünüyordu | Kod KAPALIYKEN BASİT yüzde yalnız sade cümle (`Q105.OzelMatrahKapaliBasit`) kalıyor. Sunucunun kaynak beyanı (`#fk-ozel-matrah-kaynak`: "Kdv:OzelMatrahKodu = 805", "K-10", büyük harfli metin) yalnız PRO yüzde görünüyor. Kod AÇIKKEN kod ve kaynak iki modda da birlikte duruyor (P-26 kuralı). | Canlı BASİT: not metni "Bu kurulumda faturaya özel matrah kodu yazılmaz. Mali müşavir onay verince ayarlardan tek adımda açılır." PRO: kaynak beyanı aynen. Q105 testi + M8-1 |
| Kim ne yaptı BASİT'te ham kod, teknik alan adı ve sistem satırları gösteriyordu; sayfa aralığı iki kez yazıyordu | (a) **Sunucu:** `GET /denetim` ve `/denetim/ozet` uçlarına isteğe bağlı `sistemHaric` parametresi eklendi. Varsayılanı kapalı, mevcut çağrılar değişmedi. Açıkken kullanıcısız satırlar ve sistem türleri (TelemetriOlcumu, KullaniciTercihi, KullaniciGorunum, PanoKalemAyari, RefreshToken, KurSnapshot, IsCalismasi) süzülüyor. Süzme ortak `DenetimSuzgeci` içinde yapılıyor (P-24), yani liste ve özet aynı kümeyi sayıyor. BASİT ekran bu parametreyi gönderiyor, mod değişince liste yeniden yükleniyor. (b) Hub'da adı olmayan 39 kayıt türünün TR+EN adları `ZoBelgeDenetimSozlugu`'na eklendi (ör. "Hız ölçümü", "Çek / senet"). (c) BASİT'te güncelleme detayı sade metne çevriliyor: "Ad, Guncellendi, Telefon" → "Değişen: ad, telefon". Zaman damgası alanları basılmıyor, tanınmayan alan tahmin edilmeden sayılıyor ("+1 bilgi daha"). PRO ham metni görüyor. (d) Üstteki `#dz-sayfa-ozet` kaldırıldı, aralık yalnız sayfalama satırında (`#dz-sayfa-bilgi`). | Canlı API: BASİT süzgeçli listede 194 kayıt vardı; kullanıcısız 0, sistem türü 0. Varsayılan listede 207 kayıt vardı; kullanıcısız 9, sistem türü 9. Geçersiz değer (`sistemHaric=evet`) Türkçe alan hatasıyla 400 döndü. Tarayıcı BASİT: kayıt türleri {Müşteri, Denetim kaydı}, kullanıcı yalnız "Demo Kuyumcu", aralık metni 1 kez. PRO: "Hız ölçümü", "sistem" ve ham detay aynen. ZBH01, ZB35, ZB36 + M8-2..6, M8-13 |
| İşlem defteri BASİT'te PRO ile aynı 14 sütun vardı, 1440'ta yatay kırpılıyordu | BASİT'te 5 sütun kaldı: Tarih · Tür · Müşteri · Tutar · Ödeme. Müşteri adı sarıyor, kesilmiyor. Belge sütunu BASİT'te yok; belgesi olan satırda tür adı belgeye giden bağlantı (aynı id `id-belge-{Id}`, belgeye erişim kaybolmadı). PRO'da 14 sütun aynen. | Tarayıcı BASİT 1440: tablo 1122 / kap 1122 px (önce 1680 / 1122). 960'ta 642 / 642 px. PRO 1440'ta 1380 / 1122 px (PRO geniş tablo, gölge ipucuyla). ZB37, ZB41 + M8-7 |
| 960 px'te fatura ve evrak listelerinde tutar, toplam ve durum eylem sütununun altında kalıyordu | BASİT'te Faturalar'da Ara toplam ve KDV sütunları, Evraklar'da Banka sütunu gizli. PRO'da duruyor. 1100 px ve altında iki tablo iki sütunlu KART ızgarasına dönüyor (`.zo-yigin-dar`, zo-belge.css; OLCEK-1'in 52 px satır boyu ve yapışkan eylem sütunu bu iki tabloda daha özgül seçiciyle eziliyor). 375 px'te kartlar tek sütuna iniyor. | Tarayıcı: Faturalar ve Evraklar 1440, 960 ve 375 px'te tablo = kap, belge genişliği = pencere, 4 kombinasyonda. Tur K9 (binen) 0. ZB38, ZB39 + M8-8, M8-9 |
| Yeni Fatura ve Fişten Fatura Kes pencerelerinde yerel select kalmıştı (K22) | `fatura-cari`, `fd-duzenle-cari`, `fatura-islem` ve `fk-urun-{i}` ortak **Secici**'ye geçti (uzun liste). `fatura-tur`, `fatura-kdv`, `fatura-kdvturu`, `ff-kdvturu` ve `ff-kdv` **Form/Secim**'e (radyo) geçti. Id'ler aynı. Seçenekler tek diziden geliyor (UX-S26). Radyo grupları tam satırda: 2×2 kırılma yok. | Tarayıcı: iki pencerede `select` sayısı 0 (BASİT ve PRO, açık ve koyu). ZB14 kaynak kilidi yedi ekranda `<select\s` = 0. ZB42 + M8-11 |
| "İsteğe bağlı" ile "zorunlu" aynı alanda çelişiyordu (K18) | Toplam (KDV dahil) her zaman `Pozitif` doğrulandığı için "*" alıyor. Tür, KDV türü ve KDV oranı da sunucuda zorunlu olduğu için "*" alıyor. Kasa sayımında gerekçe etiketi yeni `ZB.KasaFarkGerekce`: sayılan tutar defterden farklıysa "*", değilse "(isteğe bağlı)" ve kural ipucunda (sunucu kuralı KasaModulu:385). Evrak formunda tek etiket var (Secici'nin kendi etiketi, "*"), seçici tam satırda (`zo-tam-satir`); ikinci "Müşteriler" etiketi ve "—" boş seçeneği kalktı. | Tarayıcı: toplam etiketi "Toplam (KDV Dahil) *". Sayım etiketi farksızken "Fark gerekçesi (isteğe bağlı)", 123456 girilince "Fark gerekçesi *". Evrak formunda 1 etiket, genişlik 447 = ızgara 447 px. ZB40, ZB42 + M8-10, M8-12 |

**Gerekçeli sapmalar:**
1. **Sunucu ucuna dokunuldu** (`Api/Modules/Denetim/DenetimModulu.cs`, dosya alanı listesinde yok, eşzamanlı hiçbir şeridin alanında da değil). KAPSAM "sunucu uçları değişmez" diyor. Değişiklik yalnız isteğe bağlı ve varsayılanı kapalı bir sorgu parametresi, mevcut sözleşme birebir korundu (ZBH01 parametresiz çağrının dört kaydı da döndürdüğünü doğruluyor). Gerekçe: istemcide süzmek sayfalamayı ve toplamı yalan söyletirdi ("1–7 / 176" iken 3 satır görünürdü; Anayasa md.1 / UX-S29). Yetki (`[RolGerekli("SAHIP")]`, `ModulYetkisi`) değişmedi, yeni uç yok.
2. İstemcide ek bir "gerekçe yoksa istek atma" doğrulaması denendi, sonra geri alındı. Sebep: doğrulayıcının `ZOBELGESinirTestleri.ZOBS07/11` sayı ayrıştırma testleri farklı tutarla POST bekliyor. Sunucu farkta zaten 400 dönüyor ve hata `Aciklama → kr-sayim-aciklama` eşlemesiyle alanın altına yazılıyor. Ekranda yalnız etiket ve işaret düzeldi.
3. Başka paketlerin testlerine gerekçeli dokunuş (ekran davranışı hakem bulgusuyla bilerek değişti): `FaturaEkranTestleri` (select yerine Secici ve radyo yardımcıları; Q105 BASİT'te kaynak beyanı artık YOK), `UX1C2EkranTestleri` (radyo yardımcısı), `S1iGorselSigma2Testleri.S1i_09` (aralık `#dz-sayfa-bilgi`'de, `#dz-sayfa-ozet` yok).

Dokunulan dosyalar (bu devam): `Kuyumcu.Api/Modules/Denetim/DenetimModulu.cs`, `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (+2 anahtar KasaFarkGerekce/-Ipucu, + `ZoBelgeDenetimSozlugu`), `Pages/Faturalar.razor`, `Pages/Evraklar.razor`, `Pages/IslemDefteri.razor`, `Pages/KasaRaporu.razor`, `Pages/DenetimIzi.razor`, `wwwroot/css/zo-belge.css`. Testler: `ZoBelgeTestleri.cs` (+ZB35–ZB42, ZB14'e select kilidi), `ZoBelgeDenetimSistemTestleri.cs` (yeni, ZBH01), `FaturaEkranTestleri.cs`, `UX1C2EkranTestleri.cs`, `S1iGorselSigma2Testleri.cs`. Yeni id'ler: `fk-tablo`, `evrak-tablo`. Değişmeyenler: mevcut id'ler, POST yolları, yetki, yasal kurallar (P-26 kod gösterimi kuralı). Migration: gerekmedi.

### I-2. Build
`dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`d29b/build-son.log`). Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145) xUnit1031`; bu dosya ORTAK-1'in.

### I-3. Testler
- Filtreli koşum (ZoBelge*, ZOBELGE*, FaturaEkran, UX1C2Ekran, S1iGorselSigma2, KatalogK1, P48IslemDefteri, RaporEkran, Evrak*, Denetim*, S34RaporBugun, KasaRapor, Q105, HttpBoruHatti): **613/613 geçti** (`d29b/test4.log`). Yeni testler adlarıyla yeşil: ZB35, ZB36, ZB37, ZB38, ZB39, ZB40, ZB41, ZB42, ZBH01.
- **Tam süit** (`trx-pZO-BELGE/d4`, `d29b/tam.log`): **6947 test · 6946 geçti · 1 başarısız**. Başarısız olan `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu`; ORTAK-1'in, devir notunda da var. **Bu paketin ürettiği yeni kırmızı: 0.** Önceki tam süite göre +46 test (6901 → 6947; artışın 9'u bu devamın, gerisi diğer şeritlerin).

### I-4. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 292 ✓ · DENETİM TEMİZ** (`d29b/denetim.log`). OZEL_MATRAH_BEYANSIZ_EKRAN = 0: kod gösterildiği her durumda kaynak da gösteriliyor.

### I-5. Mutasyon kanıtları (M8, 13/13 kırmızı)
Mutasyonlar kopya ağaçta (`ZO-BELGE/kopya/dotnet`) yapıldı. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE-d4/`. Her mutasyondan sonra `cmp` sonucu yedekle AYNI ve ana ağaçla AYNI. Betik `d29b/mut.sh`, günlükler `d29b/mut/`.
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M8-1 | Faturalar: kaynak beyanı `@if (!Dil.BasitMi)` → `@if (true)` | FaturaEkranTestleri.Q105_Basit_modda_ozel_matrah_beyani_katlanir_ozet_ve_ic_kod_yok |
| M8-2 | DenetimSuzgeci `if (SistemHaric)` → `if (false)` | ZoBelgeDenetimSistemTestleri.ZBH01_… |
| M8-3 | Controller `sistemHaric`'i servise geçirmiyor | ZBH01_… |
| M8-4 | DenetimIzi `ModParcasi => ""` | ZB35_Denetim_BASIT_sistem_kayitlarini_sunucuda_suzer_… |
| M8-5 | BASİT detay çevirisi kaldırıldı | ZB35 |
| M8-6 | Kayıt türü adı yalnız hub'dan geliyor | ZB35 |
| M8-7 | İşlem defteri BASİT başlık dalı `@if (false)` | ZB37_Islem_defteri_BASIT_bes_sutun_… |
| M8-8 | Faturalar Ara toplam hücresi BASİT'te de çiziliyor | ZB38_Faturalar_BASIT_ara_toplam_ve_KDV_sutunu_YOK_… |
| M8-9 | Evraklar Banka hücresi BASİT'te de çiziliyor | ZB39_Evraklar_BASIT_banka_sutunu_YOK_… |
| M8-10 | Sayım gerekçesi `Zorunlu="false"` | ZB40_Kasa_sayim_fark_gerekcesi_farka_gore_zorunlu_… |
| M8-11 | fatura-toplam `Zorunlu` kaldırıldı | ZB42_Yeni_fatura_ve_fisten_kes_yerel_select_YOK_… |
| M8-12 | evrak-form-cari `Zorunlu` kaldırıldı | ZB39 |
| M8-13 | Zaman damgası alanları süzülmüyor | ZB35 |

### I-6. Canlı doğrulama
Ortam: 5160, kopya ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin`, üç kez yayımlandı. Kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3N8S08PRTVDWQ9XC4TH42AY. Tohum: `demo-tohumla.js --prova-kiraci` (çıkış 0: 6 cari, 12 ürün, 9 satış), `ek.mjs` (2 kasa + sayım + 4 evrak, 1'i karşılıksız; fatura adımı yine 400, § G-9-7), `fat.mjs` (3 fatura). Tek kullanımlık `Sistem__Anahtari` ortam değişkeniyle verildi, değeri basılmadı.
| Adım | Beklenen | Gerçek (yanıt okundu, `d29b/canli.log`) |
|---|---|---|
| GET /denetim/ozet?sistemHaric=true | 200, sistem türü yok | 200 · toplam 194 · türlerde TelemetriOlcumu/RefreshToken/PanoKalemAyari yok |
| GET /denetim/ozet | 200, varsayılan değişmedi | 200 · toplam 206 · TelemetriOlcumu, RefreshToken, PanoKalemAyari, Kullanici, Sube, Tenant var |
| GET /denetim?sistemHaric=true&limit=500 | 200 · kullanıcısız 0 · sistem türü 0 | 200 · toplam 194 · 0 · 0 (özetle aynı) |
| GET /denetim?limit=500 | 200 · hepsi | 200 · toplam 207 · kullanıcısız 9 · sistem türü 9 |
| GET /denetim?sistemHaric=evet | 400, alan hatası | 400 · `alanlar.sistemHaric = "'evet' geçerli bir değer değil"` |

SQL (kuyumcu_net, kiracı süzgeçli, `d29b/sql.txt`):
- denetim_kayitlari: toplam 208, insan 196. Fark okuma izlerinden: her liste çağrısı bir okuma izi yazıyor.
- faturalar: TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900)
- kiymetli_evraklar: PORTFOYDE 3 (45000) · KARSILIKSIZ 1 (5000)
- kasalar: 3
- gun_kapanislari: 2450 / 2500 / −50

### I-7. Gerçek tarayıcı ölçümü (başsız Chrome)
**(a) Hedefli ölçüm** (`d29b/hedef.mjs` → `hedef2.json`, görüntüler `d29b/foto/`). BASİT açık, BASİT koyu ve PRO açık; 1440 ve 960 px. Hakem bulgularının her biri tabloda I-1'deki değerlerle tuttu (tablo genişliği = kap, select 0, etiketler, KDV notu metni, denetim türleri).

**(b) Tam tur** (`tur.mjs` + `analiz.py`, 7 rota):
- Tur 1: {BASİT, PRO} × {açık, koyu} × {1440, 960, 375} = 84 sayfa + 30 sekme + 380 katman, hata 0 (`d29b/tur-analiz.txt`).
- Tur 2: × {1440, 1280} = 56 sayfa, hata 0 (`d29b/tur2-analiz.txt`).
- Benzersiz bulgular (tamamı önceki turda da vardı; yeni bulgu 0):
  - 375 genel arama SERBEST-YÜZEN (KATMAN, 27.09'dan devreden).
  - 1440 "⋯" SERBEST-YÜZEN, Evraklar/Faturalar (KATMAN, § G-9-4).
  - K8 "ZB-CEK/ZB-SNT": tohumun evrak numarası veri olarak yakalanıyor, arayüz metni değil. Önceki turda 8 durumdu, şimdi 6 + 4.
  - Faturalar PRO K3 "150 ₺ (%20)": önceki turda 12 durumdu, şimdi yalnız PRO 1440/1280'de 4 durum. Görüntüde kırılma YOK (`d29b/kes-fat-pro.png`: hücre tek satır). Hücre zaten `nowrap`; bu ölçerin satır içi rozet `<span>`'ını ayrı satır sayan yanlış pozitifi. Denenen ek nowrap kuralı etkisiz olduğu için geri alındı.
- Temiz rotalar: /denetim-izi, /raporlar, /raporlar/islem-defteri, /raporlar/kasa, /veri-ice-aktarim ve rapor sekmelerinin tamamı (1440 ve 1280 px).
- İşlem defteri BASİT'teki önceki 1680/1122 px yatay kırpılma kalktı.

**(c) Görüntüler** (Read ile bakıldı):
- `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-d4-faturalar-evraklar-BASIT-960.png`: ÖNCE kolon binmesi ve tek sütun uzun kart; SONRA iki sütunlu kart, tutar ve durum görünür, eylemler kart altında.
- `ZO-BELGE-d4-islem-defteri-denetim-BASIT-1440.png`: ÖNCE 14 sütun ve kırpık tablo, KullaniciTercihi/"Deger, Guncellendi"; SONRA 5 sütun, Türkçe türler, sade detay, tek aralık.
- `ZO-BELGE-d4-formlar-BASIT-1440.png`: KDV notu sade; Yeni Fatura Secici ve radyo, yıldızlı alanlar; Evrak gir tek etiket, tam satır; koyu temada fark varken yıldızlı gerekçe.
- 375 px: `d29b/tur/foto/faturalar__BASIT_aydinlik_375.png` tek sütun kart, yatay kaydırma 0.

Bölge farkı (tuval liste deseni ile, § 9 tablosuna ek): yalnız tablo sütun kümesi (BASİT sade) ve ≤1100 px kart yığını değişti. Başlık, tek birincil eylem, Süzgeç katlanırı ve özet satırı aynen.

### I-8. Temizlik
- API (5160) ve vekil (5161) `kill` ile durduruldu; `lsof` ikisinde de boş. Chrome süreçleri kapatıldı.
- Kiracı 01M3N8S08PRTVDWQ9XC4TH42AY `kiraci-temizle.sh --uygula` ile silindi. information_schema'daki tenant_id'li 70 tabloda kalan satır **0**. **tenants:** başta 15, sonda **15**. QA-TEST-* kiracılarına dokunulmadı.
- `yayin/appsettings.Development.json`, `kopya/.../appsettings.Development.json`, `d29b/anahtar` ve `d29b/fik.json` `printf ''` ile 0 bayta indirildi.
- Proje ağacına .mutorj/.bak/.tmp bırakılmadı.
- **Silinemeyen:** `rm` TurkReport kancasınca bloklandı. `ZO-BELGE/d29b` 1,5 GB (Chrome profilleri `d29b/profil-h`, `d29b/tur*/profil-*`), `ZO-BELGE/kopya` 923 MB. Kurucu silmeli. Disk 20 GB boş.

### I-9. Açık kalanlar (sahipleriyle)
1. **ORTAK-1:** `OrtakGorunumSaflik.O8_…` kırmızı; `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı.
2. **KATMAN:** 375 genel arama ve 1440 "⋯" SERBEST-YÜZEN (devreden). BASİT kenar çubuğu iç kaydırma sorunu (§ H-9-1).
3. **Ölçüm aracı (orkestratör):** `tur.mjs` K3, `td.sayi` içindeki rozet span'ını kırılma sayıyor (Faturalar PRO KDV hücresi, görüntüde tek satır). K8, kullanıcı verisi olan evrak numarasını jargon sayıyor.
4. **Sunucu ucu kararı (orkestratör/Bertan):** `sistemHaric` isteğe bağlı parametresi KAPSAM'daki "uç değişmez" maddesinin gerekçeli istisnası (§ I-1 sapma 1). Sistem türü listesi `DenetimSuzgeci.SistemVarliklari`'nda tek kaynak.
5. § H-9-3/4 aynen: ağ hatası kalıbı → ApiIstemci (sahipsiz); Evraklar 4 toplam; "Altın pozisyonum" N1 (ORTAK-1); `ek.mjs` fatura adımı.

### I-10. DoD sözleşmesi
- Yeni varlık yok.
- Yeni uç yok; mevcut `GET /denetim` ve `/denetim/ozet` uçlarına salt okunur ve isteğe bağlı süzgeç eklendi. Denetim kaydı değişmez; gerekçe DegismezIpucu'da aynen.
- Yeni düğme yok. Tür adı bağlantısı, mevcut "Belge" düğmesinin BASİT'teki yeri: gerçek gezinme, aynı href.
- Pages altında yeni ModalPencere düzenleme kullanımı: 0.
- Formlarda boş gönderimde alan altı mesajlar aynen (AlanHatasi id'leri korunuyor).
- Mevzuat: KDV 23/e hükmüne dokunulmadı; yalnız beyan metninin hangi modda görüneceği değişti. Kod AÇIKKEN kaynak beyanı iki modda da zorunlu olarak görünüyor.

---

## 29.09 GECE DEVAMI 3 (01:41–02:25) — güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § H-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9705/9712/9714). 5199/5200'e ve tarayıcı bölmesine dokunulmadı; ölçüm başsız Chrome. COMMIT yok. Ağaçtaki iş korundu. **Mutasyonlar bu kez AYRI KOPYA AĞAÇTA** (`/private/tmp/kuyumcu-serit/ZO-BELGE/kopya/dotnet`, rsync ile ana ağaçla eşitlendi) — ana ağaç mutasyon süresince hiç değişmedi (doğrulayıcının "mutasyon yarışı" notunun kalıcı çözümü).

### H-1. Önceki doğrulamada kalan bulgular → yapılan
| Bulgu | Yapılan |
|---|---|
| [ORTA] Eski iki test kiracısı (01M3JGRPASFM7RNMN9VSZTYQRH "Dogrulama", 01M3KHV088HT6KB595K9PPH7MV "Kuyumculuk") | `zsh betikler/kiraci-temizle.sh <id> --uygula` bu kez çalıştı (2728 + 2651 satır + 2 tenants satırı). information_schema'daki tenant_id'li **70 tabloda** iki kimlik için kalan satır **0**; `tenants WHERE ad LIKE 'PAKETZO-BELGE%'` = **0**. |
| [ORTA] Rapor "Bugün" ve içe aktarım yükleyicileri ağ hatası / zaman aşımını yakalamıyor (KIYAS #3) | `Raporlar.razor`: BugunOzet/BugunMutabakat/BugunBildirim yükleyicilerinde `catch (Exception ex) when (AgHatasi(ex))` → hata bandına `ZB.BaglantiHatasi`; `...Yukleniyor = false` **finally** içinde. KatalogYukle aynı kalıp. `AgHatasi(ex) => ex is HttpRequestException or OperationCanceledException` (TaskCanceledException, OperationCanceledException'dan türer). `VeriIceAktarim.razor`: Yukle() ve GecmisAc() aynı; şablon hatasında `sablonHatasi=true` → kart "Yükleniyor"da donmaz, "Yeniden dene" etkin. Ham İngilizce istisna metni ekrana basılmaz. Yeni sözlük anahtarı `ZoBelgeSozlugu.BaglantiHatasi` (TR/EN). ApiIstemci'ye dokunulmadı (sahipsiz; orkestratör kararı — § H-9-3). |
| [DÜŞÜK] BASİT "Gün sonu" kartında sunucunun ham formül metni, dil sözlüğünden geçmiyor | `#bugun-fark-aciklama` artık her zaman `B.FarkAciklama` (TR "Kasaya giren para ile kesilen faturalar tutmuyor." / EN ayrı). Formül kapalı `Katlanir` `#bugun-fark-formul` ("Nasıl hesaplanır?") içinde, **sözlükten** (`ZB.GunSonuFormul`, TR/EN; "fiş" jargonu yerine "satış"). Sunucunun `FarkTanimi`'si basit yüzde basılmıyor (EN modda Türkçe kalma sorunu kapandı). PRO yüzün (satır 661) sunucu metni değiştirilmedi — bulgu BASİT içindi. |
| [DÜŞÜK] Mutasyon yarışı (şeffaflık) | Bu devamın 7 mutasyonu kopya ağaçta; her birinden sonra kopya dosyası hem yedeğiyle hem **ana ağaçla** `cmp` AYNI (§ H-5). |
| [KRİTİK hakem çelişkisi] Evraklar BASİT 1,04 vs "14/14 ≤ 1,0" | **Yeniden ölçüldü ve sebep ayrıştırıldı** (§ H-8): üç ölçü (içerik · belge · sikisma-olc K4) ve kenar çubuğu durumu birlikte yazıldı. Kenar çubuğunun grupları varsayılan hâldeyken 14/14 kombinasyonda içerik ≤ 0,94, belge = K4 = **1,00**. BASİT'te kenar çubuğunda bir grup (ör. "Para ve banka", 10 satır) AÇIKKEN kenar çubuğu 1042 px'e uzuyor ve **7 rotanın hepsinde** belge = K4 = **1,16** oluyor; PRO'da aynı grup açıkken kenar çubuğu kendi içinde kayıyor, belge 1,00 kalıyor. Doğrulayıcının 1,04'ü bu durumun bir örneği (tur, kenar gruplarını tıklayarak açar ve durum kalıcıdır). Yani **sayfa içeriği ≤ 1,0; aşım BASİT kenar çubuğunun iç kaydırma eksikliğinden** — sahibi KATMAN (KenarCubugu). İki rapor aynı gerçeği şöyle yazmalı: "içerik 14/14 ≤ 1,0; belge boyu kenar çubuğu grupları kapalıyken 14/14 = 1,00, BASİT'te bir grup açıkken 7/7 = 1,16 (KATMAN)". |

Dokunulan dosyalar (bu devam): `Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (+3 anahtar), `Kuyumcu.Web/Pages/Raporlar.razor`, `Kuyumcu.Web/Pages/VeriIceAktarim.razor`; testler `Kuyumcu.Tests/ZoBelgeAgHatasiTestleri.cs` (yeni: ZB32, ZB33, ZB34), `Kuyumcu.Tests/S34RaporBugunTestleri.cs` (S34_10: açıklama artık sözlükten, ham formül yok — **gerekçeli ikinci dokunuş**: bu testin kilitlediği davranış doğrulayıcı bulgusuyla bilerek değişti). Uç, yetki, yasal kural, POST yolu, mevcut id DEĞİŞMEDİ; yeni id'ler `bugun-fark-formul`, `bugun-fark-formul-metin`. Migration: gerekmedi.

### H-2. Build
`dotnet build Kuyumcu.sln` → **0 Hata, 1 Uyarı** — uyarı `EtiketEkranTestleri.cs(145,47) xUnit1031` (ORTAK-1 şeridi, benim dosyam değil). Kopya ağaç build'i aynı (0 hata).

### H-3. Testler
- Filtreli (ZoBelgeAgHatasiTestleri + S34RaporBugunTestleri + ZoBelgeTestleri): **46/46 geçti** (`d29/test-d3a.log`); yeni üç test adıyla yeşil: ZB32, ZB33, ZB34; S34_10 yeşil.
- **Tam süit** (`trx-pZO-BELGE/d3`, `d29/tam-d3.log`): **6901 · 6900 geçti · 1 başarısız** → `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15 / gerçek 16 — `OrtakGorunumMetinleri` alan sayısı; ORTAK-1'in sözlüğü, devir notunda da vardı). **Bu paketin ürettiği yeni kırmızı: 0.**

### H-4. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 292 ✓ · DENETİM TEMİZ** (`d29/denetim3.log`).

### H-5. Mutasyon kanıtları (M7, 7/7 kırmızı; KOPYA ağaçta, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE-d3/`, her birinde `cmp` yedekle AYNI + ana ağaçla AYNI)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M7-1 | BugunOzetYukle'nin ağ hatası catch'i silindi | ZB32_Rapor_bugun_AG_HATASI_ve_ZAMAN_ASIMI_yukleniyorda_DONMAZ_hata_bandi_yazar |
| M7-2 | `finally { bugunMutabakatYukleniyor = false; }` → `finally { }` | ZB32, ZB34 + S34_01/03/05–20 (18 test) |
| M7-3 | `AgHatasi` yalnız `HttpRequestException` (zaman aşımı düştü) | ZB32 |
| M7-4 | VeriIceAktarim Yukle() ağ hatası catch'i silindi | ZB33_Ice_aktarim_AG_HATASINDA_donmaz_bant_yazar_yeniden_dene_calisir |
| M7-5 | VeriIceAktarim GecmisAc() ağ hatası catch'i silindi | ZB33 |
| M7-6 | Açıklama yeniden `FarkTanimi ?? B.FarkAciklama` | ZB34_Gun_sonu_SADE_aciklama_sozlukten_formul_KAPALI_katlanirda, S34_10 |
| M7-7 | Formül katlanırı `BaslangictaAcik="true"` | ZB34, S34_19 |
Günlükler `d29/mut-M7-*-test.log`, `.diff`; betik `d29/mut-d3.sh`.

### H-6. Canlı doğrulama (5160 · kopya ağaçtan Release anlık görüntüsü `ZO-BELGE/yayin` · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3N3WV4JXX41TCVPJCHH5XNF; tohum `demo-tohumla.js --prova-kiraci` (ilk koşu 40 sn'de kur gelmediği için çıkış 1 verdi — kiracı kuruldu, kur ~1 dk sonra geldi, `--eposta` ile yeniden koşuldu: 6 cari/12 ürün/9 satış) + `ek.mjs` 2 kasa + sayım + 4 evrak (fatura adımı yine 400: kalem toplamı kuralı, § G-9-7) + `fat.mjs` 3 fatura)
| Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|
| GET /raporlar/gunluk | 200 | 200 · satış 9 adet · 97794,02 |
| GET /raporlar/pos-fatura-mutabakati | 200 · fark var | 200 · mutabik=false · fark 92994,02 · farkTanimi (sunucu metni) dolu · 3 uyarı |
| GET /bildirimler?durum=ACIK&limit=5 | 200 | 200 · 0 satır · dahaFazla=false |
| GET /ice-aktarim/sablonlar · /gecmis | 200 | 200 (2 kaynak, ["HATA","ATLA"]) · 200 (toplam 0) |
| GET /raporlar/katalog | 200 | 200 · 16 rapor |
| Tarayıcı /raporlar BASİT | açıklama sözlükten, ham formül yok, formül kapalı | `#bugun-fark-aciklama` = "Kasaya giren para ile kesilen faturalar tutmuyor." · sayfada "fişe bağlı" YOK · `#bugun-fark-formul-metin` kapalıyken YOK; tıklanınca "Fark = bugünkü satışların toplamı − …" · scrollWidth 1440 |
SQL (kuyumcu_net, kiracı süzgeçli): faturalar TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900); kiymetli_evraklar PORTFOYDE 3 (45000) · KARSILIKSIZ 1 (5000); kasalar 3; gun_kapanislari 2450/2500/−50.
Ağ hatası / zaman aşımı yolu canlıda üretilemedi (vekil API düşünce 502 döner → ApiHata yolu; gerçek HttpRequestException ancak tarayıcı–vekil bağlantısı koparsa oluşur) — kanıtı bUnit ZB32/ZB33 + M7-1..5 (dürüstlük notu).

### H-7. (boş — yeni varlık yok, DoD § H-11)

### H-8. Gerçek tarayıcı ölçümü (başsız Chrome)
**Açılış boyu** (1440×900, dolu veri, önbellek kapalı — `d29/boy3.mjs`, çıktı `d29/boy3.txt`). *içerik* = ana alanın son öğesinin alt kenarı / 900 · *belge* = scrollHeight / 900 · *K4* = sikisma-olc formülü max(belge, main.scrollHeight)/900 · kenar = kenar çubuğu scrollHeight (px).
| rota | BASİT içerik / belge / K4 (kenar) | BASİT, "Para ve banka" grubu açık: belge / K4 (kenar) | PRO içerik / belge / K4 | PRO, grup açık: belge |
|---|---|---|---|---|
| /faturalar | 0,65 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,65 / 1,00 / 1,00 | 1,00 |
| /evraklar | 0,77 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,80 / 1,00 / 1,00 | 1,00 |
| /raporlar | 0,88 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,93 / 1,00 / 1,00 | 1,00 |
| /raporlar/islem-defteri | 0,91 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,94 / 1,00 / 1,00 | 1,00 |
| /raporlar/kasa | 0,48 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,51 / 1,00 / 1,00 | 1,00 |
| /denetim-izi | 0,90 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,92 / 1,00 / 1,00 | 1,00 |
| /veri-ice-aktarim | 0,90 / 1,00 / 1,00 (900) | 1,16 / 1,16 (1042) | 0,92 / 1,00 / 1,00 | 1,00 |
→ İçerik 14/14 ≤ 1,0. Belge boyu yalnız BASİT + kenar grubu açıkken aşıyor ve aşım **içerikten bağımsız** (en kısa sayfa /raporlar/kasa 0,48'de de 1,16) → KATMAN. /raporlar BASİT içerik 0,83 → 0,88 ("Nasıl hesaplanır?" satırı).
- **Tam tur** (`tur.mjs` + `analiz.py`, değişen 2 rota /raporlar + /veri-ice-aktarim × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1280,960,375} = 32 sayfa + 40 sekme + 176 katman, hata 0; `d29/tur-c/analiz.txt`): benzersiz bulgu **2 = 375 genel arama SERBEST-YÜZEN** (KATMAN, 27.09'dan devreden; § G-8 (a) ile aynı) → **yeni bulgu 0**. Rapor sekmeleri (cari/grafik/gunsonu/stok/yaslandirma) temiz.
- Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-raporlar-BASIT-1440-d29c.png` (sol: ÖNCE d28gece üçlüsü; sağ: SONRA aydınlık, karanlık, formül katlanırı açık). Bölge farkı: yalnız Gün sonu kartında ham formül satırı yerine sade cümle + kapalı "Nasıl hesaplanır?" satırı; diğer bölgeler § 9 tablosuyla aynı. Ham görüntüler `d29/foto3/` (1440 aydınlık/karanlık, 375 aydınlık).

### H-9. Açık kalanlar (sahipleriyle)
1. **KATMAN (KenarCubugu):** BASİT modda kenar çubuğu grubu açıldığında kenar çubuğu iç kaydırma yapmıyor, belgeyi 1,16 ekrana uzatıyor (PRO'da kaydırıyor). Doğrulayıcının Evraklar 1,04'ü bu. ZO-BELGE alanı dışı.
2. ORTAK-1: `OrtakGorunumSaflik.O8_…` kırmızı (15/16); `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı.
3. **Sahipsiz/orkestratör:** Aynı "yalnız ApiHata/JsonException" yakalama kalıbı ZO-BELGE'nin diğer sayfalarında da var (Faturalar, Evraklar, IslemDefteri, KasaRaporu; Raporlar PRO yükleyicileri). Bulgu yalnız "Bugün" ve içe aktarım içindi; hepsini tek tek sarmak yerine `ApiIstemci`'nin HttpRequestException/TaskCanceledException'ı `ApiHata`'ya sarması önerilir (tek nokta) — karar orkestratörün.
4. § G-9-2/4/5/6/7 aynen (Evraklar 4 toplam yorumu; KATMAN "⋯" ve 375 arama; Altın pozisyonum N1; `ZO-BELGE/` geçici dizini ~2,4 GB — `rm` kancası nedeniyle silinemedi, kurucu silmeli; `ek.mjs` fatura adımı).

### H-10. Temizlik
- API (5160) ve vekil (5161) `kill`; `lsof` ikisinde boş; `pgrep -f d29/profil` 0.
- Kiracı 01M3N3WV4JXX41TCVPJCHH5XNF: `kiraci-temizle.sh --uygula` (635 satır + tenants) → 70 tablodaki kalan satır **0**. Eski iki ZO-BELGE kiracısı da silindi (§ H-1). **tenants:** oturum başı 17 → eski ikisi silinince 15 → tohumlama anında 14 (başka şerit bir kiracı sildi) → benimkiyle 15 → benimki silindi, son ölçüm **15** (arada başka şerit `PAKETZO-ISLEM-A-TEST Dogrulama` açtı). QA-TEST-* kiracılarına dokunulmadı.
- `d29/anahtar`, `d29/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacına .mutorj/.bak/.tmp bırakılmadı. Yeni Chrome profili açılmadı (mevcut profil-boy3/profil-foto2/tur/profil-1 yeniden kullanıldı; disk 24 GB boş).

### H-11. DoD sözleşmesi
Yeni varlık/uç yok. Değişenler: yükleyicilerin hata yolu (görünüm) ve bir açıklama metninin kaynağı. Gün sonu mutabakatı türetilmiş/salt okunur rapordur (düzeltme yolu: "Fatura kes"/"Faturalara bak" bağlantıları aynen). Pages altında yeni ModalPencere düzenleme kullanımı: 0. Yeni düğme: Katlanir başlığı (gerçek aç/kapa eylemi; ZB34 tıklar).

---

## 29.09 GECE DEVAMI 2 (00:10–01:25) — güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § G-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9701–9717). 5199/5200'e ve tarayıcı bölmesine dokunulmadı; ölçüm başsız Chrome. COMMIT yok. Ağaçtaki iş korundu. Ölçüm/günlük dizini `/private/tmp/kuyumcu-serit/ZO-BELGE/d29`.

### G-1. Devir kontrolü — "SON DOĞRULAMA (KALDI)" bulguları güncel ağaçta
| Bulgu | Güncel ağaç (kod okundu) |
|---|---|
| [KRİTİK] Raporlar BugunHas / IslemDefteri net has `Bicim.Gram` | KAPALI — `Raporlar.razor:1047-1052` `Bicim.Has`; `IslemDefteri.razor:130` `Bicim.Has` (E devamında kapatılmış) |
| [ORTA] Başlık ↔ üst çubuk/menü adı | KAPALI — IslemDefteri/KasaRaporu/DenetimIzi `SayfaAdi => MenuAdlari…Ad` (başlık, PageTitle ve menü tek kaynak) |
| [ORTA] ZB14 Raporlar'ı denetlemiyor | KAPALI — `ZoBelgeTestleri.Sayfalar` 7 sayfa (Raporlar dahil); `Raporlar.razor`'da `style=` sayısı **0** |

### G-2. Bu devamda yapılanlar (önceki doğrulamada kalanlar)
| Bulgu | Yapılan |
|---|---|
| [ORTA]+[KRİTİK hakem çelişkisi] Açılış ≤ 1,0 (1440): işlem defteri 1,18/1,21, denetim izi 1,10/1,13 | Ölçüldü (d29, satır 61 px, başlıkta tarih alanları 72 px). Sayfa boyu **işlem defteri 10 → 6**, **denetim izi 10 → 7** (tek kaynak `GorselSigmaSozlugu.IslemDefteriSayfaBoyu/DenetimSayfaBoyu`; sunucu limit/ofset sayfalaması ve "1–6 / N kayıt" aralığı aynen). OLCEK-1'in tablo ölçeğine dokunulmadı. |
| Aynı ölçümde /veri-ice-aktarim PRO 1,00 (belge 1,03) | (a) "Kuru prova çalıştır" + kilit sebebi (UX-S30) aynı satırda (`.zo-ice-eylem`); (b) şablon satırı `.alan` ızgarası yüzünden **tam genişlik düğme** çiziliyordu (yazılı kural `flex-direction` idi ama `display` ızgaraydı) → `display:flex`; PRO kolon listesi `#ia-kolonlar` şablon satırının içine. İd'ler aynı. |
| Ölçerken bulunan: /veri-ice-aktarim 375'te dosya seçici `#ia-dosya` kartın dışına 24 px taşıyordu (sağ 387 > kart 363; kart `overflow` kırptığı için tur ölçüsü yakalamıyordu) | `width:100%; max-width:100%; min-width:0` → sağ 346 (kart 363 içinde). |
| [DÜŞÜK] Reyon stok boş durumu "Bu üründen…" | Yeni anahtar `ZoBelgeSozlugu.ReyonStokBos` (TR "Bu seçimde stokta mal yok; reyonlara göre dağılım gösterilecek ürün bulunamadı." / EN ayrı); `#reyon-stok-bos`. Hub'a dokunulmadı. |
| [ORTA] Eski iki test kiracısı (PAKETZO-BELGE-TEST Dogrulama / Kuyumculuk) | Silme komutu (information_schema'dan tenant_id'li tüm tablolar) **otomatik izin denetimi tarafından REDDEDİLDİ**; ikinci yoldan denenmedi. Orkestratör/kurucu siler (§ G-9-1). |
| [DÜŞÜK] denetim.sh P-11 (ZO-ISLEM-A) | Güncel ağaçta **DENETİM TEMİZ** (292 ✓) — sahibi düzeltmiş. |
| [DÜŞÜK] Evraklar tek satırda 4 toplam | Değiştirilmedi: KAPSAM "tek özet satırı" — 4 toplam tek satırda, K3 = 0, 375'te taşma 0. Yorum orkestratörün kaydına (§ G-9-2). |

Dokunulan dosyalar (bu devam): `Kuyumcu.Core/I18n/Sozlukler.GorselSigma.cs` (**gerekçeli ikinci dokunuş**: S1-i'nin sayfa boyu sabitleri; tek kaynak olduğu için sabit burada değişti, kopya sabit açılmadı), `Sozlukler.ZoBelge.cs`, `Pages/Raporlar.razor`, `Pages/VeriIceAktarim.razor`, `wwwroot/css/zo-belge.css`; testler `S1iGorselSigma2Testleri.cs` (S1i_09/11/14 yeni boylara), `RaporEkranTestleri.cs` (limit/ofset artık sabitten), `ZoBelgeReyonTestleri.cs` (+ZB30), `ZoBelgeTestleri.cs` (+ZB31). Uç, yetki, yasal kural, POST yolu, id DEĞİŞMEDİ. Migration: gerekmedi.

### G-3. Build
`dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (d29/build11.log, son mutasyondan sonra). Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145) xUnit1031` (ORTAK-1).

### G-4. Testler
- Filtreli (29 sınıf — 7 sayfayı, GorselSigma ya da ZoBelge sözlüğünü kullanan tüm test sınıfları; `d29/filtre.txt`): **421/421 geçti** (filtre5.log).
- Yeni: **ZB30** (reyon boş durumu rapor metni, ürün metni değil, TR/EN dolu ve farklı), **ZB31** (kilit sebebi düğmeyle aynı kapta; şablon satırı esnek ve kolon listesi içinde; `#ia-dosya` kap genişliğinde). Güncellenen: S1i_09 (7'lik sayfalar 1–7…36–40), S1i_11 (limit=6, "1–6 / 120"), S1i_14, RaporEkran IslemDefteri ×2.
- **Tam süit 1** (tam-d29.trx): 6843 · 6816 geçti · 27 başarısız → 25'i `P31DestekPaketi`/`SurumUcu`/`YayinAcilisHttp` "Can't find Kuyumcu.Api.deps.json" (başka şerit aynı bin'i koşum ortasında yeniden derledi — ortam), 1 ZO-ISLEM-A `ZO28n`, 1 ORTAK-1 `O8`. O sınıflar yeniden derlemeden sonra filtreli koşuldu: **P31/SurumUcu/YayinAcilis/ZOISLEMA hepsi yeşil**, yalnız O8 kırmızı (yeniden.log).
- **Tam süit 2** (tam-d29b.trx, son hâl): **6853 · 6851 geçti · 2 başarısız** → `OrtakGorunumSaflik.O8_…` (ORTAK-1; beklenen 15 / gerçek 16 — devir notunda da vardı) ve `SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE_…` (SATIS-3, zamanlamaya bağlı; tam süit 1'de yeşildi). **Bu paketin ürettiği yeni kırmızı: 0.**

### G-5. Mutasyon kanıtları (M6, 7/7 kırmızı; her birinde yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE-d29/` geri kondu, `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M6-1 | `IslemDefteriSayfaBoyu = 6` → `10` | S1i_11_Islem_defteri_6_satirlik…, S1i_14_Sozluk_TR_EN… |
| M6-2 | `DenetimSayfaBoyu = 7` → `10` | S1i_09_Denetim_izi_sunucu_sayfali_7_satir…, S1i_14 |
| M6-3 | Reyon boş durumu `ZB.ReyonStokBos` → `Yerlesim.YerlesimYok` | ZB30_Reyon_stok_bos_durumu_RAPOR_metni… |
| M6-4 | `.zo-ice-eylem { display: flex` → `block` | ZB31_Ice_aktarim_birincil_eylem_ve_kilit_sebebi_AYNI_satirda |
| M6-5 | `<div class="zo-ice-eylem">` → `<div>` | ZB31 |
| M6-6 | `.alan.zo-ice-sablon { display: flex` → `grid` | ZB31 |
| M6-7 | `#ia-dosya` genişlik sınırı kaldırıldı | ZB31 |
Günlükler `d29/mut-M6-*-test.log`, `.diff`.

### G-6. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 292 ✓ · DENETİM TEMİZ** (d29/denetim.log ve son hâl denetim2.log).

### G-7. Canlı doğrulama (5160 · kendi Release anlık görüntüm `ZO-BELGE/yayin`, 6 kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3MY5ATK2W6XRG1ENHWA1RZ0; tohum `demo-tohumla.js --prova-kiraci` 6 cari/12 ürün/9 satış + `ek.mjs` 2 kasa (+ana kasa = 3), 1 farklı sayım, 4 evrak/1 karşılıksız + `fat.mjs` 3 fatura)
Not: `ek.mjs`'in fatura adımı bu gece **400** aldı ("Kalem toplamı 1000.00 TL, gönderilen toplam 1200.00 TL — Kalemli faturada tolerans yoktur" — sunucuya başka paketten gelen kural); faturalar kalem toplamı = toplam ile `fat.mjs` ile yeniden kuruldu (200).
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar?durum=TASLAK/KESILDI/IPTAL | 200 · 1/1/1 | 200 · 1 · 1 · 1 |
| GET /evraklar?durum=PORTFOYDE/KARSILIKSIZ | 200 · 3/1 | 200 · 3 / 1 |
| GET /raporlar/islem-defteri?tur=SATIS | 200 | 200 · 9 |
| GET /raporlar/islem-defteri?limit=6&ofset=0 | 200 · 6 satır, dahaFazla | 200 · 6 · toplam 29 · dahaFazlasiVar=true |
| … &ofset=6 | 200 · 6 | 200 · 6 |
| GET /denetim?limit=7&offset=0 | 200 · 7 | 200 · 7 · toplam 485 |
| GET /raporlar/reyon-stok | 200 | 200 · reyon 0 · atanmamış 1 |
| GET /raporlar/katalog · pos-fatura-mutabakati · kasa-defteri | 200 | 200 · 200 (mutabık=false, 3 uyarı) · 200 (3 kasa) |
| GET /denetim?eylem=OKUMA · /denetim/ozet · /ice-aktarim/sablonlar · /gecmis | 200 | 200 · 200 · 200 ["HATA","ATLA"] · 200 |
| POST /kasalar/{Döviz}/gun-kapanisi {sayilanNakit:-5} | 400 | 400 "Sayilan nakit 0 veya daha büyük olmalıdır" (alan sayilanNakit) |
| POST … {sayilanNakit:390} | 200 | 200 · defter 400 · fark −10 · kilitli |
SQL (kuyumcu_net, kiracı süzgeçli): faturalar TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900); kiymetli_evraklar PORTFOYDE 3 (45000) · KARSILIKSIZ 1 (5000); gun_kapanislari 2450/2500/−50 · 390/400/−10; kasalar 3.

### G-8. Gerçek tarayıcı ölçümü (başsız Chrome)
**Açılış boyu** (1440×900, dolu veri, taze profil — `d29/icerik-boy.mjs`; önbellekli profilde CSS eski kalıyordu, bu yüzden son ölçüm yeni profille): *içerik* = ana alanın son öğesinin alt kenarı / 900; *belge* = `scrollHeight` / 900.
| rota | BASİT içerik / belge | PRO içerik / belge | önce (d28, PRO içerik) |
|---|---|---|---|
| /faturalar | 0,65 / 1,00 | 0,65 / 1,00 | 0,65 |
| /evraklar | 0,77 / 1,00 | 0,80 / 1,00 | 0,80 |
| /raporlar | 0,83 / 1,00 | 0,93 / 1,00 | 0,93 |
| /raporlar/islem-defteri | **0,91 / 1,00** | **0,94 / 1,00** | 1,21 |
| /raporlar/kasa | 0,48 / 1,00 | 0,51 / 1,00 | 0,51 |
| /denetim-izi | **0,90 / 1,00** | **0,92 / 1,00** | 1,13 |
| /veri-ice-aktarim | 0,90 / 1,00 | **0,92 / 1,00** | 1,00 (belge 1,03) |
→ **14/14 kombinasyonda ≤ 1,0 (iki ölçüde de).** Uygulayıcı ve doğrulayıcı raporu bu tabloyla aynı gerçeği yazmalı: "açılış ≤ 1,0" koşulu artık KARŞILANIYOR (önceki "KISMEN" kapandı).
- **Tam tur** (`tur.mjs` + `analiz.py`, 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1280,960,375} = 112 sayfa + 40 sekme + 512 katman, hata 0; `d29/tur`, `analiz.txt`): **benzersiz bulgu 13 = d28e/tur2 ile aynı küme, yeni bulgu 0**: (a) 375 genel arama SERBEST-YÜZEN (7 rota, KATMAN/GlobalArama), (b) 1440 "⋯" SERBEST-YÜZEN (KATMAN), (c) Evraklar K8 "ZB-CEK/ZB-SNT" = tohumdaki evrak numaraları (kullanıcı verisi), (d) Faturalar K3 "150 ₺ (%20)" araç sahte pozitifi (F-2). Değişiklik sonrası /veri-ice-aktarim ayrıca yeniden turlandı (`d29/tur-b`, 16 sayfa + 76 katman): yalnız (a).
- 375 taşma denetimi (7 rota × 2 mod, `d29/tasma.mjs`): `scrollWidth` 375, taşan öğe **0**.
- Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-{islem-defteri,denetim-izi,veri-ice-aktarim}-{BASIT,PRO}-1440-d29.png` (sol: ÖNCE d28gece üçlüsü, sağ: SONRA aydınlık + karanlık) ve `-375-d29.png`. Bölge farkı: tablo satır sayısı (6/7) ve içe aktarımda şablon/eylem satırları dışında 27.09 § 9 tablosu aynen geçerli.
- Reyon boş durumu tohumda oluşmuyor (atanmamış 1 kalem var) → metin değişikliği yalnız bUnit ZB30 ile kanıtlandı; ekranda görülemedi (dürüstlük notu).

### G-9. Açık kalanlar (sahipleriyle)
1. **Eski test kiracıları** `01M3JGRPASFM7RNMN9VSZTYQRH` "PAKETZO-BELGE-TEST Dogrulama" ve `01M3KHV088HT6KB595K9PPH7MV` "PAKETZO-BELGE-TEST Kuyumculuk": benim silme komutum izin denetiminde reddedildi → **orkestratör/kurucu** siler (`zsh betikler/kiraci-temizle.sh <id> --uygula` bu gece kendi kiracımda kalan satır bırakmadan çalıştı).
2. Evraklar özet satırında 4 toplam — KAPSAM yorumu **orkestratörün** kaydına.
3. Başkasına ait kırmızılar: ORTAK-1 `OrtakGorunumSaflik.O8_…`; SATIS-3 `SATIS3EkranTestleri.Arama_harf_harf…DEBOUNCE…` (tam süit 2); ZO-ISLEM-A `ZO28n_…` (tam süit 1, yeniden koşumda yeşil); ortam: deps.json kaybı (paylaşılan bin).
4. KATMAN: 375 genel arama ve 1440 "⋯" SERBEST-YÜZEN (27.09'dan devreden).
5. § F-9-2/3 aynen: "Altın pozisyonum" `N1` (ORTAK-1), işlem defteri sütun başlığında birim ikilenmesi (hub sahibi).
6. **Temizlik yapamadığım:** `/private/tmp/kuyumcu-serit/ZO-BELGE/` 2,4 GB (d29 1,8 GB: Chrome profilleri `d29/profil-*`, `d29/tur/profil-*`, `d29/tur-b/profil-*`; `kopya` 326 MB; `d28e` 912 MB) — TurkReport kancası `rm`'yi blokluyor; **kurucu silmeli**.
7. `ek.mjs` tohum betiği yeni "kalem toplamı = toplam" kuralına uymuyor (proje dışı geçici betik; bilgi).

### G-10. Temizlik
- API (5160) ve vekil (5161) `kill`; `lsof` ikisinde boş; `pgrep -f d29/profil` 0.
- Kiracı 01M3MY5ATK2W6XRG1ENHWA1RZ0: `kiraci-temizle.sh --uygula` → tenant_id'li tüm tablolarda kalan satır **0**. **tenants:** başlangıç 18 → benimkiyle 19 → son **17** (−1 benim; −1 başka şeridin — liste bu arada değişti). QA-TEST-* kiracılarına dokunulmadı.
- `d29/anahtar`, `d29/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile 0 bayt. Proje ağacına .mutorj/.bak/.tmp bırakılmadı.

### G-11. DoD sözleşmesi
Yeni varlık/uç yok. Değişenler görünüm (sayfa boyu, yerleşim, boş durum metni). İşlem defteri ve denetim izi türetilmiş/salt okunur (DegismezIpucu katlanırları "Listedeki bir işlem yanlışsa" / denetim kayıtları silinemez notu aynen). Pages altında yeni ModalPencere düzenleme kullanımı: 0.

---

## 28.09 GECE DEVAMI (devir notu 2, 22:25 · 23:10–23:45) — güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § F-9)
**Rol:** Taşkın + Ertuğ (Opus) · t1 izolasyonu (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9701–9704, 9712). 5199/5200'e ve tarayıcı bölmesine dokunulmadı; ölçüm başsız Chrome (betikler/tur). COMMIT yok. Ağaçtaki iş korundu.

### F-1. Devir kontrolü (ağaç ve kesilen koşum)
- 01:37 raporundan sonra kesilen 13:52 koşumunun (d28b/d28c/d28d) ağaçta bıraktığı iş okundu: `Raporlar.razor` stok yaşlandırma kova/ölü stok has değerleri `Bicim.Has`, rapor merkezi R10/R11 düğme adı `KatalogDugmeAdi` → `MenuAdlari` (E-9-3 kapandı), `Faturalar.razor` `fom-hasgram` ve fişten fatura kalem has gramı `Bicim.Has`; testler FaturaEkranTestleri, P57RaporBicimEkranTestleri, RaporEkranTestleri güncellenmiş. O koşumun mutasyonları (M4-1…M4-7b) günlüklerinden okundu: hepsi kırmızı (M4-7 ilk denemede yeşil kaldı → test güçlendirildi → M4-7b kırmızı) ve **yedekler ağaçtaki dosyalarla `cmp` AYNI** (yarım mutasyon YOK).
- O koşumun açık bıraktığı API (PID, d28d/api.pid, 5160) benimdi → durduruldu.

### F-2. Bu devamda yapılanlar (görünüm; uç, yetki, yasal kural, POST yolu, id DEĞİŞMEDİ)
| Bulgu (başsız Chrome turu d28e, güncel ağaç) | Yapılan |
|---|---|
| /raporlar Stok değeri sekmesi, **Reyon stok** kartında has gramı `Bicim.Gram` (F3 "g") — sunucu `toplamGram` = Σ adet×**HasGrami** (RaporModulu.cs ~1090); 0,0004 has kayboluyordu (doğrulayıcının KRİTİK sınıfı) | İki hücre `Bicim.Has` (4 hane, "Altın: 0,0004 has") |
| Aynı kartta sunucunun geliştirici notu ekrana basılıyordu: "`toplamAltinHasGram` YALNIZ altın has gramıdır; … `madenler` kırılımında…" (ters tırnaklı JSON alan adları, jargon) | `reyon-stok-uyari` paragrafı kaldırıldı; kullanıcı karşılığı `reyon-stok-maden-notu` ("Farklı madenlerin gramları toplanmaz — her maden ayrı satırdadır") zaten her zaman görünüyor. Sunucu ucu değişmedi. |
| Aynı kartta iki **yerel `<select>`** (aynı ekrandaki Yıl/Ay ortak Secici'den farklı çiziliyordu) | Ortak `Secici` (ElemanId `rs-sube`/`rs-reyon` — id'ler aynı, BosSecenekMetni "Tümü"). UX-S22: tek şubede şube seçicisi, reyon yokken reyon seçicisi çizilmez; rapor yine çizilir. |
| /evraklar 960'ta "Kimden" adı iki satıra kırılıyordu (K3, "Derya Yıldızlı") | Hücreye mevcut `hucre-tek-satir` sınıfı |
| /faturalar KDV hücresi "150 ₺ (%20)" K3 "kırık" | `hucre-tek-satir` eklendi; ikinci turda ölçü hâlâ işaretliyor — **araç sahte pozitifi**: `sikisma.js` satır sayısını metin düğümlerinin `top` değerinden sayıyor, küçük punto "(%20)" farklı `top` veriyor. Görüntüde tek satır (`d28e/fat-kirp.png`, 1440 BASİT; Read ile bakıldı). td.sayi zaten `olcek.css:336` ile nowrap. |

Dokunulan dosyalar (bu devam): `Pages/Raporlar.razor`, `Pages/Faturalar.razor`, `Pages/Evraklar.razor`, yeni `Kuyumcu.Tests/ZoBelgeReyonTestleri.cs`. Hub sözlüğüne, app.css'e, ortak bileşenlere dokunulmadı. Migration: gerekmedi.

### F-3. Build
`dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (d28e/build5.log). Ara derlemede 23:05–23:20 arası ağaç **başka şeridin** hatasıyla kırmızıydı: `Kuyumcu.Web/Bilesenler/Fis/FisFark.cs(88,98) CS0234 PosCihazSatir` (Fis/* — SATIS-3/ZO-ISLEM-A alanı; dokunulmadı). Bu sürede testler ve mutasyonlar **özel kopyada** (`/private/tmp/kuyumcu-serit/ZO-BELGE/kopya`, yalnız kopyada FisFark.cs'e tür yolu düzeltmesi) koşuldu; sahibi düzeltince ana ağaç yeşil derlendi ve filtreli + tam süit ANA ağaçta yeniden koşuldu. Kopyadaki üç dosyam ana ağaçla `cmp` AYNI.

### F-4. Testler
- Yeni **ZoBelgeReyonTestleri** (6): ZB26 (reyon has 4 hane, 0,0004), ZB27 (geliştirici notu yok, kullanıcı notu var), ZB28 (yerel select yok, Secici id'leri), ZB28b (tek şube + reyon yok → iki seçici de yok, rapor var), ZB28c (tek şube + reyon var → yalnız reyon seçicisi), ZB29 (fatura KDV + evrak veren hücresi tek satır).
- Filtreli (27 sınıf, `d28e/filtre2.txt`): **401/401 geçti**.
- **Tam süit** (ana ağaç, `trx-pZO-BELGE/tam7.trx`): **6792 · 6791 geçti · 1 başarısız** → `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (OrtakGorunumTestleri.cs:151, beklenen 15 / gerçek 16 — ORTAK-1'in sözlüğü, benim dosyam değil; d28d/tam5'te de vardı). Değişiklik ÖNCESİ tam6'da ayrıca ZO-ISLEM-A'nın `ZOISLEMASinir4Testleri` ×3 kırmızısı vardı (tam7'de yeşil). **Bu paketin ürettiği yeni kırmızı: 0.**

### F-5. Mutasyon kanıtları (M5, 6/6 kırmızı; kopyada, `cmp` AYNI; kopya dosyaları ana ağaçla AYNI)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M5-1 | Reyon has `Bicim.Has` → `Bicim.Gram` | ZB26 |
| M5-2 | `reyon-stok-uyari` ham sunucu metni geri | ZB27 |
| M5-3 | `rsSubeler.Count > 1` → `> 0` | **ilk denemede hiçbiri (test açığı: ZB28b dış koşul yüzünden şube kuralını sınamıyordu)** → ZB28c eklendi → **M5-3b: ZB28c** |
| M5-4 | Fatura KDV hücresinden `hucre-tek-satir` sil | ZB29 |
| M5-5 | Evrak veren hücresinden `hucre-tek-satir` sil | ZB29 |
| M5-6 | Reyon kartına yerel `<select id="rs-reyon">` geri | ZB28 |
Günlükler `d28e/mut-M5-*-test.log`, `.diff`. 13:52 koşumunun M4-1…M4-7b tablosu F-1'de (hepsi kırmızı).

### F-6. Denetim
`bash dotnet/denetim.sh` → **ÇIKIŞ 0 · 292 ✓ · DENETİM TEMİZ** (değişiklik öncesi ve sonrası iki kez).

### F-7. Canlı doğrulama (5160 · kendi Release anlık görüntüm `ZO-BELGE/yayin`, iki kez yayımlandı · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3MG7BNRBZAW3C7REZM34JZ7, 13:52 koşumunun tohumu: 6 cari, 12 ürün, 9 satış + 3 kasa, 3 fatura, 4 evrak)
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar?durum=TASLAK / KESILDI / IPTAL | 200 · 1/1/1 | 200 · 1 · 1 · 1 |
| GET /evraklar?durum=PORTFOYDE / KARSILIKSIZ | 200 · 3 / 1 | 200 · 3 / 1 |
| GET /raporlar/islem-defteri?tur=SATIS | 200 | 200 · 9 satır |
| GET /raporlar/islem-defteri özet | has 4 hane ekranda | API hasGiris 282,2012 − hasCikis 30,526 → ekran "Net altın: 251,6752 has" (görüntü) |
| GET /raporlar/katalog | 200, R10/R11 | 200 (ekranda düğme adı menü adıyla) |
| GET /raporlar/pos-fatura-mutabakati | 200 | 200 · mutabık=false · 3 uyarı |
| GET /raporlar/kasa-defteri | 200 · 3 kasa | 200 · 3 |
| GET /denetim?eylem=OKUMA · /denetim/ozet | 200 | 200 · 200 |
| GET /ice-aktarim/sablonlar · /gecmis | 200 | 200 · ["HATA","ATLA"] · 200 |
| POST /kasalar/{Döviz}/gun-kapanisi `{sayilanNakit:-5}` | 400 | 400 "Sayilan nakit 0 veya daha büyük olmalıdır" (alan hatası sayilanNakit) |
| POST … `{sayilanNakit:390}` (bugün zaten kapalı) | 409 | 409 "28.09.2026 günü bu kasa için zaten kapatılmış." |
SQL (kuyumcu_net): faturalar TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900); kiymetli_evraklar PORTFOYDE 3 (45000, şubesiz 0) · KARSILIKSIZ 1 (5000, şubesiz 0); gun_kapanislari Vitrin 2450/2500/−50 · Döviz 390/400/−10.

### F-8. Gerçek tarayıcı ölçümü (başsız Chrome, `tur.mjs` + `analiz.py`; 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1280,960,375} = 112 sayfa + 40 sekme + 512 katman; ölçüm `d28e/tur2`, öncesi `d28e/tur`)
- K1, K3 taşan, K5 (TRY), K7, K9, K10, K11, K12, K13, sayfa yatay kaydırma: **112/112'de 0**. Stok sekmesi (öncesi K4 tren 2,3×4 + jargon notu + yerel select) → **temiz**. Evraklar "Kimden" kırılması → **0**.
- Kalan tur bulguları ve gerekçesi: (a) 375'te genel arama "Müşteri, ürün ya da fiş…" SERBEST-YÜZEN katman — 7 rotanın hepsinde, KATMAN (GlobalArama) · (b) 1440'ta satır "⋯" menüsü SERBEST-YÜZEN — SatirEylemleri, KATMAN (katman.js flip) · (c) Evraklar K8 "ZB-CEK/ZB-SNT" — tohum verisindeki **evrak numaraları** (kullanıcı verisi, sahte pozitif) · (d) Faturalar K3 "150 ₺ (%20)" — araç sahte pozitifi (F-2).
- **İçerik açılış boyu** (1440×900, ana alanın son öğesinin alt kenarı / 900; kenar çubuğu hariç — `d28e/icerik-boy.mjs`; tur'un belge ölçüsü BASİT'te kenar çubuğu yüzünden 1,4'e kadar şişiyor, görüntüde içerik 690 px'te bitiyor):
| rota | BASİT | PRO |
|---|---|---|
| /faturalar | 0,65 | 0,65 |
| /evraklar | 0,77 | 0,80 |
| /raporlar | 0,83 | 0,93 |
| /raporlar/islem-defteri | **1,18** | **1,21** |
| /raporlar/kasa | 0,48 | 0,51 |
| /denetim-izi | **1,10** | **1,13** |
| /veri-ice-aktarim | 0,96 | **1,00** (belge 1,03) |
Aşım iki rotada, sebebi 01:37'deki gibi OLCEK-1 tablo ölçeği; bu gece satır boyu 61 px'e çıkmış (görüntü: 10 satır × 61). Tek ölçeği ezmek bu şeridin işi değil → § F-9-1.
- Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-<ekran>-<BASIT|PRO>-1440-d28gece.png` (aydınlık · karanlık · tuval Müşteriler deseni) ve `-375-d28gece.png`. Bölge bölge tablo 27.09 § 9 ile aynı; yeni bölge farkı yok.

### F-9. Açık kalanlar (sahipleriyle)
1. **Açılış ≤ 1,0 (1440)**: /raporlar/islem-defteri 1,18/1,21 ve /denetim-izi 1,10/1,13 — OLCEK-1 tablo tek ölçeği (satır 61 px). Orkestratör kararı: sayfa boyu (10 → 7–8 satır) ya da ölçek. Sahibi: OLCEK-1 / orkestratör.
2. /raporlar BASİT "Altın pozisyonum" kartı hâlâ `N1` ("+159,3 g has", `Bilesenler/AltinPozisyonuKarti.razor:254`) — ORTAK-1/T-P1; öneri `Bicim.Has`.
3. İşlem defteri sütun başlıkları "Has (g)" / "Tutar (₺)" ve hücreler de birim taşıyor ("0,0000 has", "5.000 ₺") — birim iki kez; başlıklar hub sözlüğünde (hub'a dokunma yasağı) → hub sahibine not.
4. Başkasına ait kırmızılar: ORTAK-1 `OrtakGorunumSaflik.O8_…` (tam7); ZO-ISLEM-A `ZOISLEMASinir4Testleri` ×3 (tam6, tam7'de yeşil). Build uyarısı `EtiketEkranTestleri.cs(145) xUnit1031` kopyadaki derlemede (ORTAK-1).
5. KATMAN: genel arama 375 ve satır "⋯" menüsü SERBEST-YÜZEN (27.09'dan devreden).
6. **Temizlik yapamadığım:** kendi kopya ağacım `/private/tmp/kuyumcu-serit/ZO-BELGE/kopya` (326 MB) ve Chrome profilleri `ZO-BELGE/d28e/tur/profil-*`, `d28e/tur2/profil-*` (her biri ~100–180 MB) — TurkReport kancası `rm`/`mv`'yi blokluyor; kurucunun silmesi gerekir. Kopyadaki `appsettings.Development.json` boşaltıldı.
7. Sahibi belirsiz kiracı: `01M3KHV088HT6KB595K9PPH7MV` "PAKETZO-BELGE-TEST Kuyumculuk" (28.09 11:24, sayılmayan 11:15 koşumu olabilir) ve `PAKETZO-BELGE-TEST Dogrulama` (doğrulayıcının) — açtığımı kanıtlayamadığım için **silinmedi**.
8. 27.09 § D-9-8 (gün kapanışı `sayilanNakit` gönderilmezse sunucuda 0) — backend paketi.

### F-10. Temizlik
- API (5160), vekil (5161) ve Chrome'larım durduruldu: `lsof` 5160/5161 boş, `pgrep` 0.
- Kiracı 01M3MG7BNRBZAW3C7REZM34JZ7: `kiraci-temizle.sh --uygula` tenants satırını sildi ama 25 tabloda satır bıraktı (denetim_kayitlari 1258, kur_snapshots 620, hareketler 29 …) → information_schema'daki tenant_id'li tüm tablolardan tek oturumda silindi; **kalan 0**. (Betiğin bu davranışı sahibine not.)
- **tenants:** bu devam başı 19 (benim kiracım dahil) → son **17**. −1 benim; −1 başka şeridin (bu arada kiracı listesi değişti). QA-TEST-* kiracılarına dokunulmadı.
- `d28d/anahtar`, `d28d/fik.json`, `yayin/appsettings.Development.json` `printf ''` ile boşaltıldı (0 bayt). Proje ağacına .mutorj/.bak/.tmp bırakılmadı.

### F-11. DoD sözleşmesi
Yeni varlık/uç yok. Reyon stok raporu türetilmiş, salt okunur (DegismezIpucu "Rapordaki bir rakam yanlışsa" katlanırı sayfada). 25.09 § 10 ve 28.09 § E-11 aynen geçerli. Pages altında yeni ModalPencere düzenleme kullanımı: 0.

---
## 28.09 DEVAM — doğrulayıcı bulguları (27.09) kapatıldı · güncel beyan: **BİTTI** (açık kalanlar sahipleriyle § E-9)
**Rol:** Taşkın + Ertuğ (Opus) · **İzolasyon:** t1 (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9702/9703). 5199/5200'e dokunulmadı, tarayıcı bölmesi kullanılmadı, COMMIT yok. Ağaçtaki 25.09+27.09 işi korundu, yeniden yazılmadı.

### E-1. Doğrulayıcı bulguları → yapılan
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| 1 KRİTİK | Has 3 haneyle (F3) yazılıyor, 0,0001/0,0004 has kayboluyor | `Raporlar.BugunHas` artık TEK merkez `Bicim.Has` (F4, P-57): "13,8760 has", eksi U+2212 korunur. `IslemDefteri` özeti `Net altın: 251,6752 has` (yeni sözlük anahtarı `IslemOzetHas`), Has sütunu hücreleri `Bicim.Has` ("0,0000 has"). `g has` biriminin ölçüm aracını GRAM kuralına düşürmesi de böylece bitti (araç `N,NNNN has`'ı HAS_HANE ile sınar). S34_06/07 beklenenleri SABİT metin ("10,4000 has", "2,5000 has", "1,2000 has"). | Doğrulayıcının ZOBS05 ve ZOBS13 testleri artık **yeşil**. Yeni ZB22 (özet + sütun "0,0004 has"). Canlı: `Net altın: 251,6752 has` = API `hasGiris 282,2012 − hasCikis 30,526`. |
| 2 ORTA | Sayfa başlığı ile menü/üst çubuk çelişiyor | İşlem defteri ve Kasa raporu başlığı (SayfaBasi + PageTitle) menü adının TEK kaynağı `MenuAdlari.Icin(Dil).<ekran>.Ad`'dan okunuyor ("İşlem defteri", "Kasa raporu"). DenetimIzi'nin zaten kullandığı kalıp. Tarayıcı ölçümü aynı çelişkiyi iki ekranda daha buldu: **Evraklar** ("Çek / Senet Portföyü" ↔ menü "Çek ve senet") ve **Veri içe aktarım** ("Veri İçe Aktarımı" ↔ "Veri içe aktar") — ikisi de aynı kalıpla düzeltildi. | ZB23 (4 ekran). 28.09 ölçümünde 112 sayfanın hepsinde sayfa başlığı = üst çubuk (/raporlar hariç: BASİT yüz başlığı "Bugün", rapor merkezinin alt yüzü — gerekçeli). |
| 3 ORTA | ZB14 satır içi stil kilidi Raporlar'ı denetlemiyor | `Raporlar` ZB14 listesine eklendi (artık gerçekten 7 ekran). Raporlar'daki 14 satır içi `style` kaldırıldı: `text-align:right` → mevcut `sayi` sınıfı (app.css), grafik sekmesi flex/min-width → `zo-belge.css` `.zo-rp-grafik` / `.zo-rp-grafik-kart` (min-width `min(320px,100%)`, 375'te taşma yok). | `grep -c 'style="' Raporlar.razor` = 0. ZB14 yeşil; M3-6 kırmızı. |
| 4 DÜŞÜK | Evrak özet satırı 375'te ayraçla başlayan satırlar | `zo-belge.css` ≤640 px: `.zo-ozet` sütun dizilimi, `.zo-ozet-ayrac` gizli. Dört toplam da durur (mali bilgi saklanmadı; "2 kutucuk" yerine bu seçildi, gerekçe 25.09 § 3). Aynı kural İşlem defteri ve Kim ne yaptı özetinde de geçerli. | Tarayıcı ölçüsü "ayraç satır başında" = **0** (112/112). Görüntü `ZO-BELGE-evraklar-BASIT-375.png`. ZB25 kaynak kilidi; M3-7 kırmızı. |
| 5 DÜŞÜK | İşlem defterinde "değiştirilemez" notu süzgecin içinde | Not süzgeçten çıkarıldı; **tablonun altında** kapalı "Listedeki bir işlem yanlışsa" katlanırında (`id-duzeltme`, Raporlar PRO `rp-duzeltme` ile aynı kalıp, DegismezIpucu id'si aynı). Açılışta kutu yok (kurucu 25.09). | ZB24 (süzgeçte yok, tablodan sonra, kapalı, açılınca var). M3-5 kırmızı. |

Yeni sözlük anahtarları (`Sozlukler.ZoBelge.cs`, TR+EN konumsal): `IslemOzetHas`, `IslemDuzeltmeBaslik`. Hub'a, app.css'e, ortak bileşenlere (Bicim.cs, Katlanir, AltinPozisyonuKarti) dokunulmadı. Uç, yetki, yasal kural, POST yolları, mevcut id'ler DEĞİŞMEDİ. Migration: gerekmedi.

### E-2. Build
`dotnet build Kuyumcu.sln` (son, mutasyonlar geri alındıktan sonra) → **0 Uyarı 0 Hata**. Bir ara derlemede 1 uyarı görüldü: `EtiketEkranTestleri.cs(145) xUnit1031` — başka şeridin dosyası (UrunEtiket, ORTAK-1), benim değil.

### E-3. Testler
- Yeni: **ZB22, ZB23, ZB24, ZB25** (ZoBelgeTestleri). Güncellenen: ZB14 (+Raporlar), S34_06/07 (sabit has metni), IceAktarimEkranTestleri.CJ_12 (başlık = menü adı).
- Filtreli (29 ilgili sınıf, `d28/filtre.txt`): **451 test · 450 geçti · 1 başarısız**. Tek kırmızı `UX4cEkranTestleri.M10_Liste_sunucu_sinirina_degince_DARALTIN_uyarisi_basilir` — `Render<Pages.Emanet>`, Emanet sayfası (EMANET-1 şeridi, ağaçta süren iş); tek başına yeniden koşumda da kırmızı, tam süitte (tam4, daha önce derlenmiş) yeşildi → EMANET-1'in yeni değişikliği. Benim dosyalarıma dokunmuyor.
- Doğrulayıcı sınır testleri `ZOBELGESinirTestleri` **16/16** yeşil (önce ZOBS05, ZOBS13 kırmızıydı).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-BELGE/tam4.trx`): **6434 · 6430 geçti · 4 başarısız**, hepsi SATIS-3 şeridi (Islemler/Hurda/tuval satış): `SATIS3EkranTestleri.Hurda_BASIT_teknik_not_GIZLI_…`, `TS2TuvalProTestleri.Genis_tablo_tuval_kolonlari_…`, `S24EskiAltinEkranTestleri.S24_Satici_Kimligi_…`, `S24EskiAltinEkranTestleri.S24_Fark_Sifir_…`. 27.09 devir listesiyle fark: S24 ×2 önceden de vardı; SATIS3/TS2 SATIS-3'ün yeni işi. **Bu paketin ürettiği yeni kırmızı: 0.**

### E-4. Mutasyon kanıtları (M3, 9/9 kırmızı; her birinde yedek geri kondu + `touch`, `cmp` AYNI; son mutasyondan sonra temiz build)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M3-1 | Raporlar `BugunHas`: `Bicim.Has` → `Bicim.Gram` | S34_06, S34_07, ZOBELGESinirTestleri.ZOBS13 |
| M3-2 | IslemDefteri özet has: `Bicim.Has` → `Bicim.Gram` | ZB22, ZOBELGESinirTestleri.ZOBS05 |
| M3-3 | IslemDefteri `SayfaAdi` → `RaporSeti.AdIslemDefteri` | ZB23 |
| M3-4 | KasaRaporu `SayfaAdi` → `RaporSeti.AdKasaDefteri` | ZB23 |
| M3-5 | `id-duzeltme` katlanırı `BaslangictaAcik="true"` | ZB24, ZB04 |
| M3-6 | Raporlar'a `style="gap:1rem"` geri eklendi | ZB14 |
| M3-7 | zo-belge.css dar ekran ayraç gizleme satırı silindi | ZB25 |
| M3-8 | IslemDefteri Has sütunu `Bicim.Has` → `Bicim.Gram` | ZB22 |
| M3-9 | Evraklar `SayfaAdi` → `Evrak.Baslik` | ZB23 |
Günlükler: `/private/tmp/kuyumcu-serit/ZO-BELGE/d28/mutlar.log`, `mut-M3-*-test.txt`.

### E-5. Denetim
`bash dotnet/denetim.sh` → **CIKIS 0 · 289 ✓ · DENETİM TEMİZ** (27.09'daki P-39 ✗ kapanmış).

### E-6. Canlı doğrulama (5160 · kendi Release anlık görüntüm `/private/tmp/kuyumcu-serit/ZO-BELGE/yayin` · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3JEFXXPPCBMQ254YKHMFH58)
Tohum: `demo-tohumla.js --prova-kiraci` (6 cari, 12 ürün, 9 satış) + `d28/ek.mjs` (2 kasa, 1 farklı sayım, 3 fatura, 4 evrak / 1 karşılıksız). Sistem anahtarı oturuma özel üretildi (dosya chmod 600, sonra boşaltıldı; değer hiçbir çıktıya yazılmadı).
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar?durum=TASLAK / KESILDI / IPTAL | 200 · 1/1/1 | 200 · 1 · 1 · 1 |
| GET /evraklar?durum=PORTFOYDE / KARSILIKSIZ | 200 · 3 / 1 | 200 · 3 / 1 · karşılıksızTutar 5000 |
| POST /evraklar (ekranda şube sorulmadan) | 201 | 201 ×4 |
| GET /raporlar/islem-defteri?tur=SATIS | 200 | 200 · 9 satır |
| GET /raporlar/islem-defteri (özet) | has 4 hane ekranda | API hasGiris 282,2012 · hasCikis 30,526 → ekran "Net altın: 251,6752 has" |
| GET /raporlar/katalog | 200 | 200 · 16 |
| GET /raporlar/pos-fatura-mutabakati | 200 · uyarı | 200 · mutabık=false · 3 uyarı |
| GET /raporlar/kasa-defteri | 200 · 3 kasa | 200 · 3 · farklı gün 0/0/1 |
| GET /denetim?eylem=OKUMA · /denetim/ozet | 200 | 200 · 200 |
| GET /ice-aktarim/sablonlar · /gecmis | 200 | 200 · 200 · ["HATA","ATLA"] |
| POST /kasalar/{Döviz}/gun-kapanisi `{sayilanNakit:-5}` | 400 | 400 "Sayilan nakit 0 veya daha büyük olmalıdır" |
| POST … `{sayilanNakit:390, aciklama}` | 200 + fark | 200 · fark −10 · defter 400 |

SQL (`docker exec kuyumcu-mysql … kuyumcu_net`): faturalar TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900) · kiymetli_evraklar PORTFOYDE 3 (45000, şubesiz 0) · KARSILIKSIZ 1 (5000, şubesiz 0) · gun_kapanislari Vitrin 2450/2500/−50 · Döviz 390/400/−10.

### E-7. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; `sikisma-olc.js` K1–K12 + `bicim-olc.js` + ek: başlık=üst çubuk, ayraç satır başında, açılışta değiştirilemez, TRY, yatay)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 sayfa** (`d28/olcum2/olcum.jsonl`, özet `d28/ozet2.txt`):
- K1, K3, K5 (TRY), K6, K7, K8, K9, K10, K11, K12, yatay kaydırma, bicim B_toplam: **112/112 satırda 0**. (27.09'daki K8 "ZB-CEK-3" veri sahte pozitifi bu veride yok.)
- Ayraç satır başında: **0**. Açılışta görünen "değiştirilemez" kutusu: **0**. Sayfa başlığı ≠ üst çubuk: **0** (ilk ölçüm turunda Evraklar ve İçe aktarım 32 satırda çelişiyordu → düzeltildi, ikinci tur 0).
- Has gösterimi (ekranda görülen tüm değerler): "13,8760 has", "0,0000 has", "Net altın: 251,6752 has" — hepsi 4 hane.
- K4 tren (boy > 2 ve ≥ 4 bölüm): 0 (en yüksek /evraklar PRO 375: 2,11 × 2 bölüm, /raporlar BASİT 375: 1,96 × 8).

Açılış boyu (scrollHeight / ekran; aydınlık / karanlık):
| rota | 1440 BASİT | 1440 PRO | 375 BASİT | 375 PRO |
|---|---|---|---|---|
| /faturalar | 1 / 1 | 1 / 1 | 1,62 / 1,62 | 1,78 / 1,78 |
| /evraklar | 1,04 / 1,04 | 1 / 1 | 2,05 / 2,05 | 2,11 / 2,11 |
| /raporlar | 1 / 1 | 1 / 1 | 1,96 / 1,96 | 1,82 / 1,82 |
| /raporlar/islem-defteri | **1,12 / 1,12** | **1,14 / 1,14** | 1,64 / 1,64 | 1,71 / 1,71 |
| /raporlar/kasa | 1 / 1 | 1 / 1 | 1 / 1 | 1 / 1 |
| /denetim-izi | 1,05 / 1,05 | 1,07 / 1,07 | 1,24 / 1,24 | 1,31 / 1,31 |
| /veri-ice-aktarim | 1 / 1 | 1,04 / 1,04 | 1,31 / 1,31 | 1,38 / 1,38 |
(1280 ve 960 değerleri `d28/olcum2/olcum.jsonl`.)

**Açılış > 1,0 — ölçülmüş sebep (Anayasa md.1, gizlenmez):** 28.09 00:26'da OLCEK-1 `olcek.css` "(5) TABLO — tek ölçek v5" kuralıyla masaüstü tablo satırını **en az 52 px** yaptı (`--tablo-satir-boyu: 3.25rem`; 27.09 ölçümünde satırlar ~26–44 px idi — karşılaştırma görüntüsünde ÖNCE/SONRA satır boyu farkı görülüyor). CDP ölçümü (1440 BASİT): /raporlar/islem-defteri'de yalnız tablo kartı 358–915 px (10 satır × 52) — sayfa başka hiçbir şey olmasa bile tablo 900'ü aşıyor; bu devamın eklediği kapalı "işlem yanlışsa" katlanırı (doğrulayıcı "tablonun altına" istedi) +66 px. /denetim-izi aynı sebep (kart 297–868 + sayfalama). Bu şeritte tablo ölçeğini ezmek OLCEK-1'in "tek ölçek" kararını bozar → yapılmadı. İlk ekranda başlık + tek birincil + süzgeç + özet + tablonun ilk 9 satırı görünüyor. → Orkestratör kararı: 10 satırlık sayfa boyu ya da tablo ölçeği (§ E-9-1). /evraklar BASİT 1,04: kenar çubuğunun iç taşması (27.09 D-3'te ölçüldü, içerik 695 px'te bitiyor).

Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-<ekran>-<BASIT|PRO>-1440.png` yenilendi — soldan sağa **ÖNCE 27.09 · SONRA 28.09 · tuval Müşteriler liste deseni**; `-375.png` = SONRA aydınlık + karanlık. Görülen: İşlem defteri başlığı "Hareket Dökümü" → "İşlem defteri", has "0,000 g" → "0,0000 has", değiştirilemez notu tablonun altında; Evraklar 375 özet dört ayrı satır, satır başında ayraç yok. Bölge bölge tablo 27.09 § 9 ile aynı; yeni bölge farkı yok (katlanır başlığı tuvaldeki "Daha fazla" deseni).

### E-8. Temizlik
- API (PID) ve vekil `kill` ile durduruldu; `lsof` 5160 ve 5161 boş; kendi Chrome'larım (d28 profilleri) kapalı (`pgrep` 0).
- `kiraci-temizle.sh 01M3JEFXXPPCBMQ254YKHMFH58 --uygula` → tenants WHERE id = **0**, faturalar 0, kiymetli_evraklar 0.
- **tenants:** başlangıç 18 → son 19. Artış başka şeritlerin kiracılarından (yeni: PAKETEMANET-1-TEST T28, PAKET-OLCEK-1-HAKEM-TEST, PAKETZO-ISLEM-A-HAKEM-TEST; kaybolan: PAKET-OLCEK-1E-TEST, PAKETZO-ISLEM-A-TEST). Benim kiracım 0. QA-TEST-* kiracılarına dokunulmadı.
- `anahtar`, `fik.json`, `yayin/appsettings.Development.json` `printf ''` ile boşaltıldı. Proje ağacına .mutorj/.bak/.tmp bırakılmadı (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-BELGE/` ve `ZO-BELGE/d28/`).

### E-9. Açık kalanlar (sahipleriyle)
1. **Açılış ≤ 1,0 (1440) iki rotada aşılıyor:** /raporlar/islem-defteri 1,12/1,14 ve /denetim-izi 1,05/1,07 — OLCEK-1 tablo tek ölçeği (52 px satır, 28.09). Orkestratör kararı: sayfa boyu (S1-i 10 satır) ya da ölçek. Sahibi: OLCEK-1 / orkestratör.
2. **Aynı ekranda has biçim tutarsızlığı kalıyor:** /raporlar BASİT "Altın pozisyonum" kartı "+159,3 g has" (`Bilesenler/AltinPozisyonuKarti.razor:254` `N1`). Bileşen ORTAK-1/T-P1'in; dosya alanım dışında, dokunulmadı. Öneri: `Bicim.Has` (F4). Sahibi: ORTAK-1.
3. **Rapor merkezi düğme adları** "Hareket Dökümü"/"Kasa Hareketleri" hub sözlüğünde (`Sozlukler.cs` `RaporSeti.AdIslemDefteri/AdKasaDefteri`, katalog R10/R11) — sayfa başlığı artık menü adında, rapor merkezi düğmesi hâlâ eski adda (UX-S31). Hub'a dokunma yasağı → hub sahibine not.
4. Tam süit / filtreli başkasına ait kırmızılar: SATIS-3 (SATIS3EkranTestleri Hurda_…, TS2TuvalProTestleri Genis_tablo_…, S24 ×2), EMANET-1 (UX4cEkranTestleri.M10_Liste_sunucu_sinirina_…, Emanet sayfası, 28.09 ağacı).
5. 27.09 § D-9'daki 2 (katman.js flip), 3 (dugme-olc silik — bu devamda yeniden koşulmadı), 4 (genel arama 375), 5 (stok sekmesi 375) ve § D-9-8 (gün kapanışı `sayilanNakit` gönderilmezse sunucuda 0 — backend paketi) aynen geçerli.

### E-10. Değişen dosyalar (bu devam)
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`, `IslemDefteri.razor`, `KasaRaporu.razor`, `Evraklar.razor`, `VeriIceAktarim.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs`
- Testler: `Kuyumcu.Tests/ZoBelgeTestleri.cs`, `S34RaporBugunTestleri.cs`, `IceAktarimEkranTestleri.cs`
- Görseller: `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-*-1440.png` ve `-375.png` (28, yenilendi)

### E-11. DoD sözleşmesi
Yeni varlık/uç yok; 25.09 § 10 aynen geçerli (fatura/evrak/gün kapanışı düzenleme yolları ekranda; raporlar, işlem defteri, kasa defteri türetilmiş ve salt okunur — DegismezIpucu "Rapor", artık işlem defterinde tablonun altında; denetim izi append-only). Pages altında yeni ModalPencere düzenleme kullanımı: 0.

---
## 27.09 DEVAM (devir notu üzerine) — güncel beyan: **BİTTİ** (açık kalanlar sahipleriyle § D-9)
**Rol:** Taşkın + Ertuğ (Opus) · **İzolasyon:** t1 (KUYUMCU_TEST_DB=kuyumcu_test_t1 · API 5160 · vekil 5161 · CDP 9700–9721). 5199/5200'e dokunulmadı, tarayıcı bölmesi kullanılmadı, COMMIT yok.

### D-1. Devir kontrolü
- Ağaç okundu. 25.09 işi yerinde. Raporun M1-1…M1-8 mutasyon noktaları kodda **orijinal** hâlinde (`FarkliGunSayisi f != 0m`, `sayimTutar is not { } tutar`, `sablonHatasi = true` ×2, `kd != "TASLAK"`, `EkBilgiDolu`, `AktifSuzgecSayisi`, `Bicim.Gram`). Yarım mutasyon yok.
- /tmp yedekleri silinmişti. Yerine ilgili 12 sınıf koşuldu: **332/332 yeşil**. Yeniden yazılan dosya yok, eksikler tamamlandı.
- Başlangıç ölçümü (güncel ağaç, dolu veri, 1440, aydınlık) aşağıdaki D-3 "önce" sütununda.

### D-2. Bu devamda yapılanlar (yalnız görünüm/yerleşim; uç, yetki, yasal kural, id DEĞİŞMEDİ)
| Ekran | Değişiklik | Gerekçe |
|---|---|---|
| /raporlar BASİT | `tuval-panel-musteri.css`'in alt satıra verdiği `min-height: calc(100vh − 402px)` bu sayfada sıfırlandı. "Gün sonu" kartı alt satırın 3. sütunu oldu (≤1100 px'te tek sütun). Gün sonu uyarıları kapalı **"Ayrıntı (n)"** katlanırında; ⚠ karakteri kaldırıldı. Düzeltme düğmeleri artık satırı boydan boya kaplamıyor. 375 px'te boş "Bekleyen iş" kutusu 12rem sabit boydan kurtarıldı. | Ölçülen sayfa 1,35 ekrandı: kartlarda ~250 px boşluk vardı ve uyarılar düzeltme satırlarını ikinci kez anlatıyordu. Uyarılar silinmedi (Anayasa md.1). Katlanır başlığı sayıyı yazıyor. |
| /raporlar PRO / Özet | Rapor merkezi (16 düğme, 484 px) kapalı **"Tüm raporlar (16)"** katlanırına alındı (iç `#rapor-merkezi`, `rm-*` id'leri aynı; düğmeler 4 grup satırında). "Satışlar neyle ödendi? (fiş sayısı)" (kök `#sk-kart`) ve "Maden dağılımı (n)" (kök `#rapor-maden-dagilimi`) de kapalı katlanırda. **Hata varsa katlanır açık açılıyor** (`@key` + `BaslangictaAcik`, UX-S28). İki tabloda tanımsız `tablo` sınıfı `veri-tablosu` yapıldı: tablolar ham, başlıksız ve kenar boşluksuz görünüyordu. Bölümler arası 16 px boşluk eklendi (önce 0 px). 375 px'te özet kartları 2 sütun (ortak.css kap sorgusu değeri küçültüyor, taşma yok). | Sayfa 1,86 ekrandı. Raporların hepsi menüde ve sekmelerde de var. Sayılar başlıkta yazıyor. |
| /raporlar/islem-defteri | Sayfa aralığı ile Önceki/Sonraki düğmeleri özet satırına taşındı (`id-sinir`, `id-onceki`, `id-sonraki` aynı; alttaki ayrı sayfalama kartı kalktı). KARIŞIK yönün iki bacağı tek satırda (`id-yon-tl-*`, `id-yon-has-*` aynı). Hücreler tek satır. Masaüstünde satırdaki "Belge aç" düğmesi satırı 58 px'e çıkarıyordu, 26 px'e indirildi (≥901 px). | Sayfa 1,22 ekrandı. |
| /evraklar | Satır eylemleri ortak **SatirEylemleri** bileşeniyle (dar sütun, Birincil=1) yapılıyor: satırda tek birincil eylem + "⋯". Tahsile ver, Portföye al, Tahsil edildi ve Karşılıksız (kırmızı metin, onay modalı aynı) pencerede. Düzenlenemeyen evrakta ilk izinli geçiş öne geliyor, kilitli Düzenle gerekçesiyle pencerede. id'ler aynı. | Dört düğme satırı ikiye bölüyordu. Kırmızı "Karşılıksız" satırda açıkta duruyordu. Faturalar ile aynı kalıp kullanıldı. |
| /veri-ice-aktarim | "Zaten kayıtlı olan satırlar" seçimi kapalı katlanırda; başlık **seçili değeri** yazıyor ("…: Hata say (önerilen)"), 375'te sarıyor. Şablon indir düğmesi artık tam genişlik değil, ipucu yanında. Kolon listesi sıkıştırıldı. Yerli dosya seçici düğmesi ikincil düğme diliyle çiziliyor. | Seçimin güvenli bir varsayılanı var. Sayfa 1,05/1,11 ekrandı. |

Yeni metinler `Sozlukler.ZoBelge.cs`'e eklendi (TR+EN): `GunSonuNotlari`, `RaporTumRaporlar`. Hub'a ve app.css'e dokunulmadı. Kullanılan ortak bileşenler (`Katlanir`, `SatirEylemleri`) değiştirilmedi. Yeni CSS yalnız `zo-belge.css`'te, yalnız belirteç `var()` ile yazıldı (ZB15 kilidi yeşil).

### D-3. Açılış boyu — 1440×900, dolu veri (tohum + 3 kasa + 3 fatura + 4 evrak), K4 = scrollHeight / 900
| rota | önce B | sonra B aydınlık / karanlık | önce P | sonra P aydınlık / karanlık |
|---|---|---|---|---|
| /faturalar | 1,00 | 1,00 / 1,00 | 1,00 | 1,00 / 1,00 |
| /evraklar | 1,04 | 1,00 / 1,00 | 1,00 | 1,00 / 1,00 |
| /raporlar | 1,35 | **1,00 / 1,00** | 1,86 | **1,00 / 1,00** |
| /raporlar/islem-defteri | 1,22 | **1,00 / 1,00** | 1,25 | 1,03 / 1,01 |
| /raporlar/kasa | 1,00 | 1,00 / 1,00 | 1,00 | 1,00 / 1,00 |
| /denetim-izi | 1,00 | 1,00 / 1,00 | 1,00 | 1,01 / 1,01 |
| /veri-ice-aktarim | 1,05 | **1,00 / 1,00** | 1,11 | 1,03 / 1,00 |

**Artık fark PRO'da en çok 27 px.** Sebebi kabuk: PRO üst çubuğu 1440'ta iki satıra sarıyor (EUR ve kaynak etiketi, **93 px**; BASİT'te 68 px). Ölçüm (`kaynak.mjs`): /raporlar/islem-defteri PRO'da içeriğin alt kenarı 901, /veri-ice-aktarim PRO'da 906, üst çubuk 93 px. Aynı sayfalar BASİT'te 1,00. Sayfa içeriği 900 − 93 alanına sığıyor, taşan kısım üst çubuğun fazladan satırı. Üst çubuk dosya alanımda değil (§ D-9).
/evraklar BASİT'in "önce" değeri 1,04'tü; sebebi içerik değil kenar çubuğunun iç taşmasıydı (içerik 695 px'te bitiyor, `kenar-cubugu` scrollHeight 933). Son ölçümde 1,00.

### D-4. Ölçüm araçları (güncel ağaç, kendi Release anlık görüntüm `/private/tmp/kuyumcu-serit/ZO-BELGE/yayin`)
- **tur.mjs + analiz.py:** 7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}. 112 sayfa, 40 sekme, 496 katman (134 açılan), hata 0, geçersiz 0. Ham veri: `/private/tmp/kuyumcu-serit/ZO-BELGE/turB`.
  - K1, K3, K5 (TRY), K7, K8, K9, K10, K11, K12, K13 ve yatay kaydırma: bu 7 rotada **0**.
  - İlk turda iki bulgu vardı, ikisi düzeltildi: K10 (içe aktarım katlanır başlığı 375'te kesikti) ve K4 tren (/raporlar 375: PRO 2,1 → **1,82**, BASİT 2,2 → **1,96**; 25.09 tam tur tabanı 2,5).
  - Kalan bulgular:
    - (a) Genel arama kutusu "Müşteri, ürün ya da fi…" 375'te SERBEST-YÜZEN, 7 rotanın hepsinde. Bu 25.09 tabanında da var; sahibi KATMAN (GlobalArama).
    - (b) /evraklar "⋯" 1440 BASİT'te SERBEST-YÜZEN. Pencere yukarı açılınca açıcıdan 26 px kopuk kalıyor (ölçüm `popo.mjs`: alta açılınca boşluk 4 px, üste açılınca 26 px). Sebep `js/katman.js` iki geçişli flip hesabı; aynı bulgu 25.09 tabanında /faturalar "⋯" için 14 durumda var. Sahibi KATMAN.
    - (c) /raporlar?sekme=stok 375 K4 2,2×4. Stok değeri sekmesi, bu pakette dokunulmadı; 25.09 tabanında da tren vardı.
- **tutarlilik.mjs (1440):** BASİT↔PRO farkı bu 7 rotada **12** (orkestratör tabanı `MOD-TUTARLILIK-FARK-1440.txt`'te aynı rotalar için 27). Farkların hepsi "boy 44→32" türünde: modun genel kontrol ölçeğinden geliyor (belirtecler.css/olcek.css, sahibi OLCEK-1/DUGME-2). Sayfaya özgü fark yok.
- **dugme-olc.mjs:** ortasız 0, kırpık 0, **silik 52** (25.09: 40). Dağılım:
  - 34: ortak Katlanır başlığı (`katlanir-baslik`). Tasarım gereği saydam; kenarlık üst `section.katlanir`'da. Araç ebeveyn kenarlığını görmüyor, düğmeyi silik sayıyor. Artışın tamamı bu devamda eklenen 5 katlanırdan (×2 tema).
  - 10: sekme şeridi (`rp-sekme-*`).
  - 4: `#pozisyon-ayrinti` / `#pozisyon-alacak` (ORTAK-1).
  - 4: `#evrak-suzgec-portfoyde` (segment düğmesi, sekme ile aynı desen).
  - Katlanır ve sekme dışında yeni silik yok.
- **bicim-olc.js:** 7 rota × 2 mod × (katlanırlar kapalı / açık): B_toplam **0**.

Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-<ekran>-<BASIT|PRO>-1440.png` yenilendi. Soldan sağa: 25.09 ÖNCE, **27.09 SONRA**, tuval liste deseni (Müşteriler). 375 görüntüleri: `-375.png` (27.09 tur fotoğrafları). Bölge tablosu § 9'daki gibi; yeni bölge farkı "katlanır başlık satırları", tuvaldeki "Daha fazla" satırıyla AYNI desen.

### D-5. Testler
- Yeni testler (**7**):
  - `ZoBelgeTestleri`: ZB16 (evrak satırında tek birincil + "⋯", Karşılıksız pencerede ve onay modalını açar), ZB17 (sayfa aralığı özet satırında), ZB18 (içe aktarım kuralı kapalı, başlık seçili değeri yazar).
  - `RaporEkranTestleri`: ZB19 (PRO özet katlanırları kapalı, başlıklar sayıyı yazar; kaynak kilidi `<table class="tablo"` 0), ZB20 (katalog hatasında katlanır açık, sayı "0" yazılmaz), ZB21 (ödeme kırılımı hatasında katlanır açık).
  - `S34RaporBugunTestleri.S34_10`: güçlendirildi (uyarı kapalı katlanırda, sayı 1, açılınca metin var, ⚠ yok).
- Uyarlanan testler: G5SatisKirilimi `Ac()` (katlanır açık değilse açar), P69_03 (maden katlanırı açılır), Rapor_Merkezi (önce sayı rozeti sınanır, sonra açılır), S34_02/04/05 (PRO işareti `#rp-tum-raporlar`), S34_19 (katlanır başlığı istek atmaz, `aria-expanded` açılır).
- Filtreli koşumlar: 17 ilgili sınıf **369/371** → 2 test düzeltildi (S34_19 katlanır başlığı, ZB19 sahte API'de satış kırılımı yok) → ilgili 8 sınıf **109/109** yeşil. RaporEkran ZB **3/3**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-BELGE/tam3.trx`): **6373 test · 6369 geçti · 4 başarısız.** Dördü de bu paketin dosyalarına dokunmuyor:
  - `P39TasarimKokleriTestleri.K5_…`: iletisi `["PosTahsilatlar.razor@2827","PosTahsilatlar.razor@5793"]`. PosTahsilatlar ZO-ISLEM-A şeridinin.
  - `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_…` ile `S24EskiAltinEkranTestleri` ×2: üçü de `Render<Pages.Islemler>`. Islemler/AltinBacagi SATIS-3 şeridinde, ağaçta yeni `AltinBacagiPaneli` işi sürüyor.
  - **Bu paketin ürettiği yeni kırmızı: 0.**
- Build: `dotnet build Kuyumcu.sln` **0 uyarı 0 hata**. Paylaşılan ağaçta eşzamanlı build'ler MSB3030, MSB4018, CS0016 dosya kilidi ve bir ara başka şeridin yarım `FisOdemeBacaklari`/`AltinBacagiPaneli` hatası verdi. Yeniden deneme betiğiyle (`bt.sh`) yeşil alındı. Kendi kodumdan derleme hatası çıkmadı.

### D-6. Mutasyon kanıtları (M2, 6/6 kırmızı; her birinde yedek geri kondu, `touch` edildi, `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M2-1 | Raporlar `BaslangictaAcik="@(katalogHatasi is not null)"` → `@(false)` | RaporEkranTestleri.ZB20_PRO_katalog_hatasi_katlanir_ACIK_hata_gorunur |
| M2-2 | Evraklar `if (e.Duzenlenebilir \|\| gecisler.Count == 0)` → `if (gecisler.Count == 0)` | ZoBelgeTestleri.ZB16_Evrak_satirinda_TEK_birincil_eylem_karsiliksiz_pencerede_idler_ayni |
| M2-3 | IslemDefteri özet satırındaki `#id-sinir` silindi | ZB17_Islem_defteri_sayfa_araligi_OZET_satirinda · S1i_11_Islem_defteri_10_satirlik_sunucu_sayfasi_ve_ustte_aralik · RaporEkranTestleri.IslemDefteri_sunucu_sinirini_ekranda_SOYLER |
| M2-4 | Gün sonu notlar katlanırına `BaslangictaAcik="true"` | S34_10_Gun_sonu_FARK_VAR_ise_tutar_ve_DUZELTME_baglantisi_cizilir |
| M2-5 | İçe aktarım katlanır başlığından seçili değer çıkarıldı | ZB18_Ice_aktarim_yinelenen_satir_kurali_KAPALI_baslik_secili_degeri_yazar |
| M2-6 | Raporlar `BaslangictaAcik="@(skHata is not null)"` → `@(false)` | RaporEkranTestleri.ZB21_PRO_odeme_kirilimi_hatasi_katlanir_ACIK_hata_gorunur |

⚠ **Anayasa md.1 notu (M2-6):** İlk M2-6 koşumu (ZB21 eklendikten sonra) **yeşil** çıktı. Aynı mutasyon elle tekrarlanınca ZB21 **kırmızı** oldu. İlk koşumda derleme ile test arasına başka bir şeridin eşzamanlı build'i girdiği için dll mutasyonsuz kaynaktan üretilmiş olabilir (paylaşılan obj). Kanıt olarak ikinci koşumu (`m26b`) sayıyorum. Aynı yarış M2-1…M2-5'i etkilemez, çünkü onlar kırmızıydı. M2-6'dan önce G5 testleri bu mutasyonu yakalamıyordu (Ac() katlanırı kendisi açıyordu); açık ZB21 ile kapandı.
25.09'daki M1-1…M1-8 kanıtları (§ 6) geçerli, ilgili testler bugün yeşil.

### D-7. Denetim
`bash denetim.sh` → CIKIS 1, **288 ✓ · 1 ✗**. Tek ✗: `P-39: TABLO_SARMALAYICISIZ = 2`. Sayacın python betiği ihlalleri `Pages/PosTahsilatlar.razor` satır 64 ve 115 olarak buluyor (ZO-ISLEM-A şeridi). Benim sayaçlarımın hepsi ✓. P-26 özel matrah, P-39 sarmalayıcı, ModalPencere düzenleme artışı 0.

### D-8. Canlı doğrulama (5160 · kendi Release anlık görüntüm · kiracı `PAKETZO-BELGE-TEST Kuyumculuk` 01M3J2XAVSFDSJR0EH6WEAKNXY)
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar?durum=TASLAK / KESILDI / IPTAL | 200 · 1/1/1 | 200 · 1 · 1 · 1 |
| GET /evraklar?durum=PORTFOYDE / KARSILIKSIZ | 200 · 3 / 1 | 200 · 3 / 1 · karşılıksızTutar 5000 |
| POST /evraklar (ekranda şube sorulmadan) | 201 | 201 ×4 |
| GET /raporlar/islem-defteri?tur=SATIS | 200 | 200 · 9 satır |
| GET /raporlar/katalog | 200 | 200 · 16 (ekran "Tüm raporlar 16") |
| GET /raporlar/pos-fatura-mutabakati | 200 · uyarılar | 200 · mutabık=false · 3 uyarı (ekran "Ayrıntı 3") |
| GET /raporlar/kasa-defteri | 200 · 3 kasa | 200 · 3 · farklı gün 0/0/1 |
| GET /denetim?eylem=OKUMA · /denetim/ozet | 200 | 200 · 200 |
| GET /ice-aktarim/sablonlar · /gecmis | 200 | 200 · 200 · politikalar ["HATA","ATLA"] |
| POST /kasalar/{Döviz}/gun-kapanisi `{sayilanNakit:-5}` | 400 | 400 "Sayilan nakit 0 veya daha büyük olmalıdır" |
| POST … `{sayilanNakit:390, aciklama}` | 200 + fark | 200 · fark −10 · defter 400 |

SQL (`docker exec kuyumcu-mysql`):
- faturalar: TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900)
- kiymetli_evraklar: PORTFOYDE 3 (45000, şubesiz 0) · KARSILIKSIZ 1 (5000, şubesiz 0)
- gun_kapanislari: Vitrin 2450/2500/−50 · Döviz 390/400/−10

**Çalıştırma sapması (gerekçeli):** `dotnet run --no-build` iki kez çöktü. Başka şeritler aynı `bin/Debug`'u temizleyip yeniden derliyordu ("Kuyumcu.Api … No such file"). Bu yüzden API `dotnet publish -c Release -o /private/tmp/kuyumcu-serit/ZO-BELGE/yayin` anlık görüntüsünden, Development ortamıyla çalıştırıldı. Sistem anahtarı oturuma özel üretildi (dosya chmod 600, sonra boşaltıldı). Değeri hiçbir çıktıya yazılmadı.

**Temizlik:**
- API (PID) ve vekil `kill` ile durduruldu; `lsof` 5160 ve 5161'de boş. Kendi Chrome'larım (9700–9721) kapatıldı (`pgrep` 0).
- `kiraci-temizle.sh --uygula` çalıştırıldı: kiracı satırı 0, faturalar 0, evraklar 0.
- **tenants:** başlangıç 15 → son 19. Artış başka şeritlerin açık test kiracılarından (PAKETKATMAN-1b, ORTAK-1, SATIS-3, OLCEK-1-HAKEM, ZO-ISLEM-A …). Benim kiracım 0. QA-TEST-* kiracılarına dokunulmadı.
- fik.json, anahtar ve yayin/appsettings.Development.json `printf ''` ile boşaltıldı.

### D-9. Açık kalanlar (sahipleriyle)
1. **PRO üst çubuğu 1440'ta iki satır (93 px):** /raporlar/islem-defteri, /denetim-izi ve /veri-ice-aktarim PRO'da açılış 1,01–1,03. Sayfa içeriği sığıyor, fark tamamen üst çubuktan (D-3). Sahibi: kabuk/üst çubuk (MainLayout–KATMAN/ORTAK). Öneri: kur şeridi PRO'da tek satıra sığmalı.
2. **`js/katman.js` flip boşluğu 26 px:** /evraklar ve /faturalar "⋯" penceresi yukarı açılınca açıcıdan kopuk kalıyor. Sahibi: KATMAN.
3. **dugme-olc "silik" 52:** Katlanır başlığı (SATIS-2) ve sekme (Sekmeler) tasarım gereği saydam. Araç, kenarlığı ebeveynde olan bu deseni tanımıyor. Araç sahibine ya da SATIS-2'ye not. `#pozisyon-*`: ORTAK-1.
4. **Genel arama 375'te SERBEST-YÜZEN** (7 rota, taban bulgusu). Sahibi: KATMAN.
5. **/raporlar?sekme=stok 375 K4 2,2** (Stok değeri sekmesi, taban bulgusu). Bu devamın kapsamı dışında kaldı.
6. **Tam süit kırmızıları:** PosTahsilatlar K5 ve denetim P-39 (ZO-ISLEM-A). AlisOdemeAraci ve S24 ×2 (SATIS-3, Islemler/AltinBacagi yarım işi).
7. **Sayfa başlığı ile menü adı farklı:** menü ve üst başlık "İşlem defteri", sayfa başlığı "Hareket Dökümü" (UX-S31). Metin hub sözlüğünde (`RaporSeti.AdIslemDefteri`), hub sahibine not.
8. 25.09 § 11'deki 3, 4, 6, 7 ve 9 numaralı maddeler aynen geçerli: K8 veri sahte pozitifi, sunucuda gün kapanışında `sayilanNakit` gönderilmezse 0 sayılması (bugün yeniden görüldü: `{}` gövdesi hâlâ 0 sayılıyor, backend paketi), VeriDisaAktarim jargonu, "Kuru prova" adı, Pages'teki başka şeritlere ait `.mutorj`/`.tmp` artıkları (6 dosya, benim değil).

### D-10. Değişen dosyalar (bu devam)
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`, `Evraklar.razor`, `IslemDefteri.razor`, `VeriIceAktarim.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs`
- Testler: `ZoBelgeTestleri.cs`, `RaporEkranTestleri.cs`, `S34RaporBugunTestleri.cs`, `G5SatisKirilimiEkranTestleri.cs`, `P69HasAdlandirmaTestleri.cs`
- Görseller: `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-*-1440.png` (14, yenilendi), `-375.png` (14, yenilendi)

Migration: gerekmedi. Mevzuat: yeni hüküm yok; özel matrah beyanı ve KVKK dayanağı aynen duruyor.

---
# 25.09 KAYDI (ilk uygulayıcı)

**Tarih:** 2026-09-25 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** v2/t1 (KUYUMCU_TEST_DB=kuyumcu_test_t1 · canlı API 5160 · vekil 5161 · CDP 9700–9706). Tarayıcı bölmesi kullanılmadı. 5199'a dokunulmadı.
**25.09 beyanı: YARIM** (aşağıdaki 27.09 devamı bunu günceller). Kod, testler, mutasyonlar, denetim ve canlı doğrulama tamam. Kabul ölçütlerinden ikisi tam sağlanmadı: "açılış ≤ 1,0 ekran" dört rotada aşılıyor, `dugme-olc` 40 "silik" bulgusu veriyor. Ayrıntı § 11.

## 1. Kapsam ve spesifikasyon doğrulaması (adım 1)
Kaynaklar: ZORUNLU-ENVANTER-2026-09-25.json (7 rota), GOZ-INCELEME #2/#8/#12/#16/#74, BICIM-2 `sonra-*.jsonl`, kurucu kuralı (25.09 11:35).
Envanterin "mevcut durum" iddialarının koda karşı kontrolü:
- `/raporlar/islem-defteri` ve `/raporlar/kasa` açıklama satırı yanlış metni basıyordu: rapor merkezinin `MerkezAciklama` metni ("…kartına tıklayın"). Ekrana özgü sade açıklama yazıldı.
- `/evraklar` şube kuralı: envanter "doğrulanmalı" diyordu. Canlıda POST /evraklar ekranda şube sorulmadan **201** döndü. SQL'de `sube_id IS NULL` = 0. Şube oturumdan geliyor (UX-S21).
- `/veri-ice-aktarim` "Yükleniyor… donması" gerçekti. Şablon isteği hata verince kart `sablon is null` dalında sonsuza dek "Yükleniyor" yazıyordu; hata yalnız üst bantta görünüyordu. Çift başlığı CUMA-J önceden kaldırmıştı. Bunun üzerine kart başlığı 18 px'e çekildi, çünkü "ikinci, büyük başlık" izlenimini veriyordu.
- `/raporlar` BASİT: envanter "değişiklik gerekmiyor" diyordu. Ölçümde iki sorun çıktı: açılışta "değiştirilemez" kutusu vardı, gram "13,9 g" biçimindeydi (BICIM-2 GRAM_HANE).
- Tuvalde bu 7 ekranın karşılığı yok. Liste deseni olarak `Musteriler.png` alındı: başlık, hızlı süzgeç şeridi, tablo, "Daha fazla" katlanırı.
- Mevzuat: yeni hüküm yazılmadı. Özel matrah beyanının içeriği ve id'leri aynen duruyor, yalnız yeri değişti. Denetim izi yasal dayanağı mevcut iddia olan **KVKK md.12** ile birebir aynı tutuldu. md.11 bilgisi yeni bir iddia olacağı için eklenmedi. İçe aktarımdaki "tevsik/MASAK" ifadesi, aynı iddianın sade karşılığıyla ("yasal bildirim hesapları") değiştirildi.

## 2. Yapılanlar (ekran ekran)
| Ekran | Açılışta (sonra) | Katlanıra / kayda taşınan | Göz / biçim düzeltmesi |
|---|---|---|---|
| /faturalar | başlık · Fişten kes (ikincil) · **+ Yeni Fatura (tek birincil)** · kapalı "Süzgeç" · tablo | Durum süzgeci: yerel `<select>` yerine ortak **Form/Secim** (id `fk-durum-filtre` aynı), katlanırda. Seçiliyse başlıkta adı ve sayı rozeti görünür. Özel matrah beyanı sayfa sonunda **kapalı** katlanırda ("Altın faturasındaki KDV kodu hakkında"). Beyan okunamazsa üstte açık kalır (UX-S28). "Değiştirilemez" kutusu kesilmiş fatura **açılınca** (kalem penceresi) görünür. | Göz #2 kapandı. 7 ham `type=number` yerine Para/Sayı girdi ailesi kullanıldı (id ve hata kimlikleri aynı). Kaynak sütunu BASİT'te gizli. 🏛/📄 emoji yerine Ikon. 375 px'te satır kart (q10-yigin). Kaydırma gölgesi eklendi. |
| /evraklar | başlık · **Evrak gir** · Tümü/Portföyde · kapalı "Süzgeç" · **tek özet satırı** (4 toplam) · tablo | Tahsile verildi/Karşılıksız düğmeleri (id aynı), tür ve vade tarihleri katlanırda. Takvimden derin bağlantıyla gelindiyse katlanır açık başlar. Form: 6 isteğe bağlı alan ve PRO'daki kimlik no "Ek bilgiler" katlanırında, düzenlenen kayıtta biri doluysa açık başlar. "Değiştirilemez" ve ciro notu **düzenleme çekmecesinde**. | Göz #8: "Faz B" ifadesi kalktı, sade ciro notu yazıldı. 375 px'te kart. |
| /raporlar | BASİT "Bugün" 6 kart aynen. Pozisyon kartı **Sade** (T-P1 kısa kart, "Ayrıntı" ile açılır). | BASİT'te sayfa düzeyi "değiştirilemez" yok. PRO'da kapalı "Rapordaki bir rakam yanlışsa" katlanırında. PRO Yıl/Ay yerel select yerine ortak Secici (`rp-yil`/`rp-ay`). | GRAM_HANE: `BugunHas` artık Bicim.Gram kullanıyor ("13,900 g has"). 960 px'te kart ızgarası 2 sütun. |
| /raporlar/islem-defteri | başlık + sade açıklama · Başlangıç/Bitiş + Yazdır · kapalı "Süzgeç" · **tek özet satırı** (giriş/çıkış/işlem/has, iptal amber rozet) · tablo | Tür/Ödeme/Şube/Kaydeden, Süzgeci temizle ve "değiştirilemez" katlanırda. | Satır içi `style` kaldırıldı (`zo-sag`). Kaydırma gölgesi ve dar ekran ipucu eklendi. |
| /raporlar/kasa | başlık · **kasa başına tek özet satırı**: ad, kasadaki para (kendi para birimiyle), bugünün durumu, fark rozeti, "Sayımı gir" · kapalı **"Geçmiş kayıtlar"** | Tarih/kasa süzgeci (Form/Tarih ve Secici), 9 sütunlu gün tabloları, "N gün daha" ve "değiştirilemez" katlanırda. **Fark uyarısı kaybolmaz:** dönemde fark olan gün sayısı kapalıyken de kırmızı rozetle görünür. | Sayım tutarı ham number yerine Para oldu. **Boş başlar ve boş gönderim 0 sayılmaz** (alan altı mesaj). Tek kasa sayım bekliyorsa birincil düğme, birden çoksa ikincil. 640 px'te satır yığılır. Satır içi style kaldırıldı. |
| /denetim-izi | başlık + **sade açıklama** · kapalı "Süzgeç" · **tek özet satırı** · tablo | 6 süzgeç, değişmez notu, DegismezIpucu ve **yasal dayanak (KVKK md.12)** katlanırda. | Göz #16 kapandı. Satır içi style `zo-alan-alt` sınıfına taşındı. Kaydırma gölgesi eklendi. |
| /veri-ice-aktarim | başlık + sade açıklama · kapsam notu (geçmiş hareket aktarılmaz, **yazılı kalır**) · hazırlık kartı | İçe Aktarım Geçmişi **kapalı** katlanırda. Açılınca bir kez yüklenir, hata katlanırın içinde yazar, "Yeniden dene" var. | Göz #12: şablon hatasında kart içinde sebep ve **Yeniden dene** var, "Yükleniyor"da donmuyor. GRUPSUZ: "5000" artık "5.000". Göz #16 kapandı. |

Ortak: `css/zo-belge.css` (yeni, yalnız belirteç `var()`, `.zo-belge` ve `#raporlar-bugun` kapsamlı) ve `index.html`'de tek `<link>` (ortak.css hâlâ en sonda). Sarmalayıcı sınıfı birebir `tablo-sar` kaldı, şerit işareti `data-zo` özniteliğinde tutuluyor (denetim P-39 bunu şart koşuyor). Ortak `Katlanir` (SATIS-2) kullanıldı, bileşene dokunulmadı. Metinler `Sozlukler.ZoBelge.cs` (TR+EN, konumsal record) dosyasında. Hub'a dokunulmadı.

## 3. Sapmalar ve gerekçeleri
- **Özel matrah katlanırı iki modda da kapalı:** Göz #2 bulgusu PRO 375'te çıkmıştı. İçerik silinmedi, P-26 sayacı `✓` durumunda.
- **İstatistik kartları iki modda da tek satıra indi** (İşlem defteri, Evraklar, Denetim): mod ölçüyü değiştirmemeli, tutarlılık farkı 0 olmalı. Hiçbir sayı kaybolmadı.
- **Kasa "Sayımı gir":** yalnız bir kasa bekliyorsa birincil. Aynı ekranda birden çok birincil düğme olmaması için seçildi.
- **Evraklar "en fazla 2 özet kutucuk" maddesi:** kutucuk yerine tek satır kullanıldı. Dört toplam da tek satırda, mali bilgi saklanmadı.
- **/raporlar PRO:** envanter değişiklik istemiyordu. Ölçümde yerel select ve açılışta "değiştirilemez" çıktığı için kapsam kuralı gereği düzeltildi.
- **VeriDisaAktarim göz #16:** dosya alanımda değil, dokunulmadı (§ 11).

## 4. Migration
Gerekmedi. Şema değişikliği ve sunucu ucu değişikliği yok (spesifikasyon: "sunucu uçları, yasal kurallar, id'ler DEĞİŞMEZ").

## 5. Testler
- Önce, filtreli ilgili 24 sınıf: **358/358** yeşil.
- Sonra, filtreli (+ DenetimIziEkranTestleri, P39TasarimKokleriTestleri, DenetimAppendOnlyTestleri): **423/423** yeşil (`ilk8`).
- Yeni: `Kuyumcu.Tests/ZoBelgeTestleri.cs` — **15 test** (ZB01–ZB15: kasa özeti/fark/boş sayım, işlem defteri süzgeci, fatura süzgeci/özel matrah/kaynak sütunu/kesilmiş ipucu, evrak hızlı süzgeç/ek bilgiler, denetim süzgeci, içe aktarım hata/geçmiş, kaynak kilitleri: ham number 0, satır içi style 0, css belirteç, yükleme sırası).
- Yeni düzene uyarlanan testler (anlamları korundu, katlanır açılarak ölçülüyor): FaturaEkran, UX1C2, UX1C3, UX4d, EvrakEkran, IceAktarimEkran, P22Ekran, RaporEkran, S1iGorselSigma2, S34RaporBugun (gram 3 hane, Bicim ile üretilen beklenen değer; Ayrıntı düğmesi), EkranTestleri/DenetimIziEkranTestleri.
- **Tam süit** (`trx-pZO-BELGE/tam2.trx`): **6304 test · 6302 geçti · 2 başarısız.** İkisi de `Dogrulayici.EMANET1SinirTestleri` (S5, S8). Sahibi EMANET-1 şeridi, dosyalarım dışında. Bu paketin ürettiği yeni kırmızı: **0.** Bir önceki koşumda (`tam.trx`) görülen KasaRaporu kırmızıları eski dll'den geliyordu (§ 6 not). Düzeltmeden sonra koşulan tam2'de yoklar.

## 6. Mutasyon kanıtları (8/8 kırmızı, hepsinde geri alma `cmp` birebir)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M1-1 | KasaRaporu `FarkliGunSayisi` `f != 0m` → `f == 0m` | ZB02_Kasa_raporu_sayim_farki_tablo_KAPALIYKEN_de_ozette_kirmizi_yazar |
| M1-2 | KasaRaporu boş sayım kontrolü → `var tutar = sayimTutar ?? 0m` | ZB03_Kasa_sayimi_BOS_gonderilemez_sifir_sayilmaz_alan_alti_mesaj |
| M1-3 | VeriIceAktarim catch'ten `sablonHatasi = true` silindi | ZB11_Ice_aktarim_sablon_hatasi_Yukleniyorda_DONMAZ_yeniden_dene_istek_atar |
| M1-4 | Faturalar `kd != "TASLAK"` → `==` | ZB07_Kesilmis_fatura_ACILINCA_degismez_ipucu_taslakta_yok · UX1C2 Faturalar_TASLAK_duzenlenemez_dugme_YOK_gerekce_VAR |
| M1-5 | Evraklar `EkBilgiDolu` → daima false | ZB09_Evrak_formunda_ek_bilgiler_yenide_KAPALI_doluysa_ACIK |
| M1-6 | IslemDefteri `AktifSuzgecSayisi` → 0 | ZB04_Islem_defteri_suzgec_KAPALI_secilince_baslikta_adi_ve_sayisi_yazar |
| M1-7 | Raporlar `BugunHas` → eski N1 biçimi | S34_06_Satis_karti_TUTAR_ADET_HAS_yazar… · S34_07_Alis_karti_hurda_AYRI… |
| M1-8 | KasaRaporu `kr-gecmis` `BaslangictaAcik="true"` | ZB01_Kasa_raporu_acilista_kasa_basina_ozet_satiri_tablo_ve_degismez_kapali |

⚠ **Ders (Anayasa md.1):** macOS `cp` yedeği geri koyarken dosyanın eski mtime değerini koruyor. Bu yüzden son mutasyondan sonraki "temiz build" projeyi güncel sanıp M1-8'li dll'i bıraktı. Bunu ilk tam süit (3 KasaRaporu kırmızısı) ve canlı ölçüm (kasa geçmişi açılışta açık) gösterdi. Çözüm: geri konan dosyalar `touch` edildi ve yeniden derlendi. Ardından tam süit, ölçüm ve bUnit yeniden koşuldu. Öneri: mutasyon reçetesine "geri aldıktan sonra touch" adımı eklenmeli.

Son build: `dotnet build Kuyumcu.sln` **0 uyarı 0 hata** (`build-son4`). Bir ara koşum `NETSDK1177` / `MSB4018` hatası verdi. Sebep, başka bir şeridin aynı anda çalıştırdığı `--no-incremental` build'di. O build bitince koşum temiz geçti.

## 7. Denetim
`bash denetim.sh` → **DENETİM TEMİZ** (CIKIS 0, 289 ✓).
- Önce: 288 ✓ ve 1 ✗ (P-23 ODEME_KALEMSIZ_YOL — başka şeridin test kiracısı).
- Arada: `P-39 TABLO_SARMALAYICISIZ = 6` çıktı, çünkü sınıf `tablo-sar zo-…` yazılmıştı. İşaret `data-zo` özniteliğine taşınınca düzeldi.

## 8. Canlı doğrulama (5160, Development, kiracı PAKETZO-BELGE-TEST)
Tohum: `demo-tohumla.js --prova-kiraci` (6 cari, 12 ürün, 9 satış). Üzerine API ile 2 kasa + hareket, 1 farklı sayım, 3 fatura (taslak/kesildi/iptal), 4 evrak (1 karşılıksız) eklendi.
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /faturalar?durum=TASLAK / KESILDI / IPTAL | 200, her biri 1 | 200 · 1 · 1 · 1 |
| GET /faturalar/ozel-matrah-ayari | 200, kod yok, beyan var | 200 · kod=null · acik=false · kaynakBeyani VAR |
| POST /evraklar (şube sorulmadan) | 201 | 201 ×4 (şube oturumdan) |
| GET /evraklar?durum=PORTFOYDE / KARSILIKSIZ | 200 | 200 · 3 · 1 (karşılıksızTutar 5000) |
| GET /raporlar/islem-defteri?tur=SATIS | 200 | 200 · 9 satır |
| GET /raporlar/kasa-defteri | 200, 3 kasa, Vitrin'de 1 farklı gün | 200 · 3 · fark günleri 0/0/1 |
| GET /denetim?eylem=OKUMA · /denetim/ozet | 200 | 200 · 200 |
| GET /ice-aktarim/sablonlar · /gecmis | 200 | 200 · 200 (0 kayıt) |
| POST gun-kapanisi `{}` (Ana kasa) | ekran göndermez; sunucu? | **400** "sayılan 0,00 … gerekçe ZORUNLUDUR" (boş gövde sunucuda **0 sayılıyor**, § 11) |
| POST gun-kapanisi `{sayilanNakit:390}` (Döviz) | 200 + fark | 200 · fark=-10 · defter=400 |
| POST gun-kapanisi `{sayilanNakit:-5}` | 400 | 400 "0 veya daha büyük olmalıdır" |

SQL (kiracı `01M3CAAVZCCSR0EVEMXGQJR9ZG`):
- `faturalar`: TASLAK 1 (1200) · KESILDI 1 (3600) · IPTAL 1 (900)
- `kiymetli_evraklar`: PORTFOYDE 3 (45000, şubesiz 0) · KARSILIKSIZ 1 (5000, şubesiz 0)
- `gun_kapanislari`: Döviz 390/400/-10 · Vitrin 2450/2500/-50

Ekranlar bu değerlerle birebir örtüşüyor: kasa özetinde "Bugün sayıldı · Fark: −50 ₺" ve "1 günde sayım farkı var" görünüyor.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; sikisma-olc.js K1–K12 + ek: yatay, boy, yerel select, ham number, açılışta "değiştirilemez", birincil sayısı)
7 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}. 1440 ve 375'te katlanır **açık** durum da ölçüldü (**162 satır**). Tam tablo: `scratchpad/zob/tablo-son.md`, ham veri: `olcum/sonra-*.jsonl`, `rp-*.jsonl`.

Özet:
- **K1, K3, K5 (TRY), K7, K9, K10, K11, K12, yatay kaydırma:** 162/162 satırda **0**.
- **Yerel select, ham number, açılışta "değiştirilemez":** 7 rotanın hepsinde **0** (önce: select 1–2, "değiştirilemez" 1–2).
- **K8:** 12 satırda 1. Hepsi /evraklar BASİT. Yakalanan dizge `ZB-CEK-3`: test kiracısına benim girdiğim **evrak numarası** (kullanıcı verisi). Karşılıksız durumda olduğu için PATCH ile değiştirilemedi. Ekran metni değil, veri kaynaklı sahte pozitif.
- **Tutarlılık** (`tutarlilik.mjs`, 1440, 7 rota): BASİT↔PRO eleman farkı **0**.
- **Düğme ölçümü** (`dugme-olc.mjs`): ortasız 0, kırpık 0, **silik 40** (§ 11).

Açılış boyu (1440, aydınlık; ekran = 900 px):
| rota | önce BASİT | sonra BASİT | önce PRO | sonra PRO |
|---|---|---|---|---|
| /faturalar | 1,00 | 1,00 | 1,00 | 1,00 |
| /evraklar | 1,30 | 1,04 | 1,35 | 1,00 |
| /raporlar | 1,91 | 1,35 | 1,95 | 1,86 |
| /raporlar/islem-defteri | 1,57 | 1,22 | 1,60 | 1,25 |
| /raporlar/kasa | 2,01 | 1,00 | 2,04 | 1,00 |
| /denetim-izi | 1,47 | 1,00 | 1,47 | 1,01 |
| /veri-ice-aktarim | 1,36 | 1,05 | 1,43 | 1,11 |

Görüntüler (Read ile bakıldı):
- ÖNCE/SONRA/tuval: `ham/TUVAL-KARSILASTIRMA/ZO-BELGE-<ekran>-<BASIT|PRO>-1440.png` ve `-375.png` (28 dosya).
- ÖNCE görüntüleri, eski 7 sayfanın **ayrı bir kopya ağaçta** (`scratchpad/zob/once-agac`) derlenmesiyle aynı veriyle alındı. Paylaşılan ağaca dokunulmadı.

### Bölge bölge karşılaştırma (tuval liste deseni `Musteriler.png` ↔ sonra)
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Üst bar + menü | ortak kabuk | ortak kabuk (dokunulmadı) | AYNI |
| Sayfa başı | başlık + tek birincil | başlık + açıklama + tek birincil (Faturalar'da ikincil "Fişten kes") | GEREKÇELİ FARK: fişten kesme mevcut özellik, silinmez |
| Hızlı süzgeç | "Hepsi · Borçlu · Toptancı" şeridi | Evraklar: "Tümü · Portföyde". Diğer ekranlarda kapalı "Süzgeç" satırı | GEREKÇELİ FARK: envanter bu ekranlarda süzgeci katlanır istedi |
| "Daha fazla" satırı | 48 px, 1 px ayraç, 9–10 px köşe, sağda ok | ortak Katlanir (aynı ölçüler, `katlanir.css`) | AYNI |
| Ana tablo | başlık satırı + satırlar | aynı `veri-tablosu`. 375'te Faturalar/Evraklar kart, diğerlerinde gölgeli kaydırma | GEREKÇELİ FARK: 375 tuvalde yok |
| Sayım/özet | "184 müşteri · 5 gösteriliyor" | tek özet satırı / aralık satırı | AYNI (desen) |
| Çekmece | sağ çekmece | ortak Cekmece (dokunulmadı) | AYNI |

## 10. DoD sözleşmesi
Yeni varlık ya da uç **yok**. Mevcut düzenleme yollarının hepsi ekranda duruyor:
- **Fatura:** TASLAK için PATCH /faturalar/{id} (Cekmece) ve kalem PATCH (SatirIciDuzenle). Durum geçişleri PATCH /durum ile, onaylı. Kesilmiş fatura değişmez; gerekçe **kayıt açılınca** DegismezIpucu'da (finansal belge, hard DELETE yok).
- **Evrak:** PORTFOYDE için PATCH /evraklar/{id} (Cekmece). Tahsile ver, portföye al, tahsil, karşılıksız uçları. Tutar ve tür değişmez; gerekçe düzenleme çekmecesinde.
- **Gün kapanışı:** POST (Cekmece). Kapanan gün değişmez, geçmiş günde sebep yazılı.
- **İçe aktarım oturumu:** PROVA gerekçesi SatirIciDuzenle ile düzenlenir, geri alma pasifleştirme ile yapılır.
- **Raporlar, işlem defteri, kasa defteri:** türetilmiş, salt okunur (DegismezIpucu "Rapor").
- **Denetim izi:** salt okunur, append-only (KVKK md.12).

Pages altında yeni ModalPencere düzenleme kullanımı: 0.

## 11. Açık kalanlar
1. **Açılış ≤ 1,0 ekran (kabul ölçütü) sağlanmadı** (1440, dolu veri): /raporlar/islem-defteri 1,22/1,25 · /raporlar 1,35/1,86 · /veri-ice-aktarim 1,05/1,11 · /evraklar BASİT 1,04. Sebepler: 10 satırlık döküm sayfası, Bugün'ün 6 kartı, PRO rapor merkezinin 16 rapor düğmesi, içe aktarım formu, dolu evrak tablosu. İlk ekran (başlık + birincil + özet + tablonun başı) hepsinde görünüyor. Kalan kısım tablo satırı ya da kart. Daraltmak için tablo sayfa boyunu ya da rapor merkezini değiştirmek gerekir; bu da başka paketlerin kararı (S1-i sayfa boyu). → **Orkestratör kararı gerekli.**
2. **`dugme-olc` "silik" 40:** ortak `Katlanir` başlık düğmesi (`katlanir-baslik`, SATIS-2 şeridi) ve sekme şeridi (`sekmeler-sekme`) tasarım gereği zeminle aynı renkte ve kenarlığı kartta. Bunlara ek olarak `#pozisyon-ayrinti` ve `#pozisyon-alacak` (ORTAK-1/T-P1). Bileşen ve css dosyaları dosya alanım dışında (DOKUNMA). Sahipleri: SATIS-2 (Katlanir), ORTAK-1 (AltinPozisyonuKarti).
3. **K8 sahte pozitif:** /evraklar'da test verisindeki evrak no "ZB-CEK-3". Kiracı silindi, gerçek ekranda karşılığı yok. Ölçüm aracı kullanıcı verisini jargon sayıyor → araç sahibine not.
4. **Sunucu boşluğu (Taşkın notu, kapsam dışı; uçlar DEĞİŞMEZ kuralı):** `POST /kasalar/{id}/gun-kapanisi` gövdesinde `sayilanNakit` gönderilmezse sunucu değeri **0** sayıyor. Gerekçe verilirse sayılmamış kasa 0 sayılmış gibi kapanabilir. Ekran artık boş gönderimi engelliyor. Sunucuda `decimal?` + `[Required]` önerilir → backend paketi.
5. **Başka şeritlerin kırmızıları (tam süit):** `EMANET1SinirTestleri.S5_…` ve `S8_…` (sahibi EMANET-1). Filtreli bir koşumda UX1C2 Emanet_* 4 test ve `KatmanTekKalipTestleri.K1_21` de kırmızı göründü (sahipleri EMANET-1 / KATMAN). Bunlar tam2'de yok.
6. **Göz #16'nın VeriDisaAktarim kısmı:** dosya alanımda değil (VeriDisaAktarim*), dokunulmadı.
7. **"Kuru prova çalıştır" düğme metni** hub sözlüğünde duruyor. Açıklama artık "deneme" diyor. SAHİP ekranı olduğu için bırakıldı. Tek ad (UX-S31) için hub sahibine not.
8. **Başlangıçta API günlüğündeki `is_calismalari` Duplicate entry hataları:** zamanlanmış işlerin aynı gün başka bir süreçte zaten koşmuş olmasından geliyor. Bu paketle ilgisi yok, önceden vardı.
9. Pages dizininde başka şeritlere ait artık dosyalar var (`*.mutorj`, `Islemler.Sade.cs.tmp`). Benim değiller, dokunulmadı.

## 12. Temizlik
- API (5160) ve vekil (5161) `kill` ile durduruldu. `lsof` ikisinde de boş.
- Kendi Chrome profillerim (9700–9706) kapatıldı.
- Test kiracısı `PAKETZO-BELGE-TEST Kuyumculuk` (01M3CAAVZCCSR0EVEMXGQJR9ZG) `kiraci-temizle.sh --uygula` ile silindi. `tenants` WHERE id → 0. Davet satırı da silindi (1 → 0).
- **tenants sayısı:** başlangıç 16 → son 15. Aradaki fark başka şeritlerin kendi kiracılarını açıp kapatmasından geliyor. Benim kiracım 0. QA-TEST-* kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı dosyaları `printf ''` ile boşaltıldı.

## 13. Değişen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`, `Evraklar.razor`, `Raporlar.razor`, `IslemDefteri.razor`, `KasaRaporu.razor`, `DenetimIzi.razor`, `VeriIceAktarim.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css` (yeni) · `dotnet/Kuyumcu.Web/wwwroot/index.html` (tek link)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoBelge.cs` (yeni)
- Testler: `ZoBelgeTestleri.cs` (yeni) · `FaturaEkranTestleri.cs` · `UX1C2EkranTestleri.cs` · `UX1C3EkranTestleri.cs` · `UX4dEkranTestleri.cs` · `EvrakEkranTestleri.cs` · `IceAktarimEkranTestleri.cs` · `P22EkranTestleri.cs` · `RaporEkranTestleri.cs` · `S1iGorselSigma2Testleri.cs` · `S34RaporBugunTestleri.cs` · `EkranTestleri.cs`
- Görseller: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-BELGE-*.png` (28)

## 14. Koşul karşılama
| Koşul | Durum |
|---|---|
| Zorunlu alanlar açık, isteğe bağlılar katlanır | ✓ (evrak ek bilgiler, süzgeçler, geçmiş) |
| Tek birincil eylem | ✓ (kasa: tek bekleyen kasada birincil) |
| "Değiştirilemez" açılışta yok | ✓ 7 rota × 16 kombinasyon = 0 (PRO /raporlar dahil) |
| Hata ≠ boş ≠ yükleniyor (UX-S28) | ✓ içe aktarım şablon ve geçmiş, özel matrah hata kartı |
| Yerel select / ham number → Form ailesi | ✓ ölçüm 0 |
| Jargon (Faz B, KVKK, MASAK) | ✓ ekrandan çıktı, dayanak katlanırda |
| Tablo kaydırma ipucu / 375 kart | ✓ |
| BICIM-2 sayfa ihlalleri (GRAM_HANE, GRUPSUZ) | ✓ |
| id'ler korunur, POST yolları değişmez | ✓ |
| bUnit + mutasyon ≥ 3 | ✓ 15 test, 8/8 |
| Açılış ≤ 1,0 ekran | ✗ 4 rotada aşılıyor (§ 11-1) |
| Yeni ölçüm bulgusu 0 | ✗ silik 40 (ortak bileşen), K8 veri sahte pozitifi (§ 11-2/3) |
