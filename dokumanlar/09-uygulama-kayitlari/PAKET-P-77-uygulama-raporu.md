# PAKET P-77 — Yedek doğrulamayı arayüzden tetikleme (`POST /yedekler/dogrulama-talep`) — UYGULAMA RAPORU

> Şerit: **SERİT E — YEDEK DOĞRULAMA + RPO + BİÇİM tr-TR** · izole test vt `kuyumcu_test_e` · canlı port **5184**
> Rol: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-24 03:38–04:20
> Kaynak şartname: `dokumanlar/09-uygulama-kayitlari/TRIYAJ-TUR8-2026-09-23.md` (P-77, YÜKSEK, 2,0 saat tahmin)
> Durum beyanı: **BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı — § 12)

## 1. ÖZET

Ekran "doğrulanmamış yedek" diye uyarıyor ama kullanıcının yapabileceği bir şey yoktu (çıkmaz sokak). Artık:

- **API:** `POST /api/v1/yedekler/dogrulama-talep` → **201**; kuyruğa (`yedek_talepleri`) `tur=DOGRULAMA` satırı yazar,
  açık denetim kaydı (`DenetimServisi.YazmaKaydet`, hassas) düşer. Bekleyen doğrulama talebi varken ikinci istek **409**.
  Rol kapısı mevcut yedek uçlarıyla birebir: sınıf düzeyi `[RolGerekli("SAHIP")]` + `[ModulYetkisi("yedek")]`.
- **GET `/yedekler/ozet`:** yeni alan `bekleyenDogrulamaTalebi` (`{id, olusturuldu}` ya da `null`).
  **GET `/yedekler/talepler`:** her satırda `tur` (YEDEK | DOGRULAMA).
- **Betik:** `yedek-al-ve-dogrula.sh talep` modu satırın türüne bakar: `DOGRULAMA` → `tatbikat_kos` (son BAŞARILI yedeği
  ayrı şemaya geri yükle + sağlama toplamı; dosyayı yalnız OKUR, silmez/değiştirmez); `YEDEK` → eskisi gibi `yedek_al ELLE`.
- **Ekran:** doğrulama uyarısının hemen altında "Şimdi doğrula" düğmesi (`#yd-dogrula`) + açıklama. Tıklanınca istek
  atılır, üst başarı bandı (talep no ile) ve düğmenin yanında `role="alert"` sonuç satırı (`#yd-dogrula-sonuc`, sunucunun
  `mesaj`ı) basılır; özet yeniden yüklenir ve bekleyen talep varken düğme **kilitlenir ve sebebini yazar**
  (`#yd-dogrula-kilit`: "Sırada bekleyen bir doğrulama var (… tarihinde istendi) …"). Hiç yedek yoksa da kilitlenir ve
  sebebi yazar. Talepler tablosunda türü rozetle gösterilir ("Yedek alma" / "Doğrulama").

## 2. ŞARTNAMENİN KODA KARŞI DOĞRULANMASI

| İddia (şartname) | Ölçüm | Sonuç |
|---|---|---|
| "YedekModulu.cs:405-427 uç listesinde doğrulama tetikleyen uç YOK" | Controller'da uçlar: `ozet`, liste, `tatbikatlar`, `talepler`, `POST talep`, `PATCH talep/{id}`, `PATCH talep/{id}/gerekce` (satır no kaymış: 543-578) | **Doğru** — doğrulama ucu yoktu |
| "mevcut talep kuyruğu deseni" | `yedek_talepleri` tek türlüydü; betik `WHERE durum='BEKLIYOR'` her satırı **yedek alma** sayıyordu | Kuyruğa tür eklemeden doğrulama isteği yazılsaydı betik onu **yeni yedek** diye işlerdi → **tür kolonu gerekti** (§ 3 S2) |
| "yetki SAHIP/MUDUR" | Mevcut yedek uçlarının kapısı ölçüldü: sınıf düzeyi `[RolGerekli("SAHIP")]` (MÜDÜR dahil herkese 403) | Ek koşul "mevcut rol kapısını ÖLÇ ve aynısını uygula" dediği için **yalnız SAHİP** (§ 3 S1) |
| Dosya ayak izi (5 dosya) | `test -e`: YedekModulu.cs, Yedekler.razor, Sozlukler.cs, YedekHttpTestleri.cs, YedekEkranTestleri.cs — **hepsi var** | Hub `Sozlukler.cs`'ye dokunulmadı; ayrı `Sozlukler.YedekDogrulama.cs` açıldı |

## 3. SAPMALAR VE GEREKÇELERİ

- **S1 — Rol: SAHİP/MÜDÜR değil, yalnız SAHİP.** Ek koşul (MEVZUAT/GÜVENLİK): "mevcut yedek uçlarının rol kapısını ÖLÇ ve
  aynısını uygula". Ölçülen kapı sınıf düzeyinde `[RolGerekli("SAHIP")]`'tir; MÜDÜR'e açmak, sınıf kapısını gevşetmek ya da
  bu uca özel ikinci bir kapı açmak demekti (yedek sistem genelidir, Mehmet koşulu). Test MÜDÜR'e **"yedek" modül yetkisini
  açıkça verip** yine 403 aldığını sınar (403'ün modül kapısından değil rol kapısından geldiği kanıtlanır).
  → MÜDÜR'ün de doğrulama isteyebilmesi istenirse bu bir ÜRÜN kararıdır; kurucuya/orkestratöre not (§ 13).
- **S2 — Dosya sınırının dışına çıkıldı (zorunlu, gerekçeli):** şartnamenin hedefi "kuyruğa doğrulama isteği yazar
  (betik tüketir)". Mevcut kuyrukta tür yoktu ve betik her bekleyen satırı yedek alma sayıyordu. Hedefe sadık kalmak için:
  - `Kuyumcu.Api/Data/Entities.cs` — `YedekTalebi.Tur` alanı + `YedekTalepTuru` sabitleri (yalnız ekleme, yedek sınıfı içinde).
  - `Kuyumcu.Api/Data/KuyumcuDbContext.cs` — `Tur` için `HasDefaultValue("YEDEK")` (tek satır ekleme).
  - Migration `20260924004513_P77YedekTalepTuru` (+ Designer + ModelSnapshot).
  - `dotnet/yedek-al-ve-dogrula.sh` — `talep_kuyrugu` türe göre iş seçer; `tatbikat_kos` doğrulanan koşu kimliğini
    `DOGRULANAN_KAYIT_ID`'ye yazar (talep satırının `yedek_kaydi_id`'si olur).
  - `Kuyumcu.Contracts/Sayfalar/YedeklerSozlesmeleri.cs` — `OzetVeri.BekleyenDogrulamaTalebi` ve `TalepVeri.Tur`
    (varsayılanlı, sona eklenen parametreler; eski yanıtlar bozulmaz). Kural "yeni istemci record'u razor'a değil buraya".
  Bu dosyaların hiçbiri başka şeridin listelenmiş dosyası değildir. Migration öncesi
  `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration"
  (başka şeridin yarım model değişikliği migration'a KARIŞMADI); üretilen Up() yalnız `tur` kolonunu ekler.
- **S3 — Başarı bandı `role`:** ortak `BasariBandi` bileşeni `role="status"` basar ve ortak bileşenler DEĞİŞTİRİLEMEZ.
  Şartnamedeki `role=alert` şartı, düğmenin yanındaki sayfa-yerel sonuç satırıyla (`#yd-dogrula-sonuc`, `role="alert"`,
  sunucunun `mesaj`ı) karşılandı; üst bant da (talep no ile) basılır.
- **S4 — Sıra:** şeridin koşum sırası P-77 → P-76 → P-57; şartname satırı "P-76'dan SONRA" diyor. Orkestratörün şerit
  sırası uygulandı. P-76 aynı dosyalara (Yedekler.razor, YedekEkranTestleri.cs) bu paketin üstüne kuracak.
- **S5 — Elle yedek talebinin 429 kapısı artık yalnız `tur=YEDEK` satırlarına bakar.** Aksi halde bekleyen bir doğrulama
  talebi elle yedek almayı (ya da tersi) engellerdi. T-3/P-56 davranışı ve testleri (`TalepAc_IkinciKez_429` vb.) aynen yeşil.
- **S6 — Hiç yedek yoksa sunucu talebi reddetmez;** ekran düğmeyi sebebiyle kilitler ("Henüz alınmış bir yedek yok …").
  Sunucu tarafında `yedek_kayitlari` kiracısız (sistem geneli) tablo olduğundan testte başarılı yedek tohumlamak diğer
  testlerin özet varsayımlarını bozardı. Böyle bir talep betikte `HATA` olarak kapanır ve talepler listesinde görünür.
- **S7 — Doğrulama talebinin gerekçesi sabit:** "Ekrandan başlatılan yedek doğrulama talebi" (≥20 karakter; UX-S21:
  sistemin bildiği şey kullanıcıdan istenmez). P-56'nın gerekçe düzeltme ucu bu satırda da çalışır.

## 4. MİGRATION

`dotnet build Kuyumcu.sln` YEŞİL → `dotnet ef migrations add P77YedekTalepTuru --project Kuyumcu.Api` (build'li).
`20260924004513_P77YedekTalepTuru.cs` 869 bayt, Designer 236 084 bayt. Up():

```csharp
migrationBuilder.AddColumn<string>(name: "tur", table: "yedek_talepleri", type: "varchar(20)",
    maxLength: 20, nullable: false, defaultValue: "YEDEK").Annotation("MySql:CharSet", "utf8mb4");
```
`dotnet ef database update --connection …kuyumcu_net…` → "Applying migration '20260924004513_P77YedekTalepTuru'. Done."
Kanıt: `information_schema.columns` → `tur | YEDEK` (varsayılan); `__EFMigrationsHistory` son satır
`20260924004513_P77YedekTalepTuru`, toplam 71; mevcut 6 talep satırı `YEDEK` türünde. Yeni varlık yok (mevcut
`IKiraciVarlik` `YedekTalebi`'ne kolon); `(tenant_id, durum, olusturuldu)` indeksi kuyruk taramasını karşılıyor.

## 5. DEĞİŞİKLİKLER (dosya dosya)

| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Api/Modules/Yedek/YedekModulu.cs` | `DogrulamaTalepAc` servisi (409 kapısı, `Tur=DOGRULAMA`, `YazmaKaydet`), `[HttpPost("dogrulama-talep")]` → 201; `Ozet` yanıtına `bekleyenDogrulamaTalebi`; `Talepler` yanıtına `tur`; `TalepAc` bekleyen kapısı `tur=YEDEK` ile daraltıldı, satır `Tur=YEDEK` yazar |
| `Kuyumcu.Api/Data/Entities.cs` | `YedekTalebi.Tur` + `YedekTalepTuru` |
| `Kuyumcu.Api/Data/KuyumcuDbContext.cs` | `Tur` DB varsayılanı `YEDEK` |
| `Kuyumcu.Api/Data/Migrations/20260924004513_P77YedekTalepTuru{,.Designer}.cs` + `KuyumcuDbContextModelSnapshot.cs` | Yeni migration |
| `dotnet/yedek-al-ve-dogrula.sh` | `talep_kuyrugu` türe göre `tatbikat_kos` / `yedek_al ELLE`; `DOGRULANAN_KAYIT_ID` (bash -n temiz) |
| `Kuyumcu.Contracts/Sayfalar/YedeklerSozlesmeleri.cs` | `BekleyenDogrulamaVeri`, `OzetVeri.BekleyenDogrulamaTalebi`, `TalepVeri.Tur` |
| `Kuyumcu.Core/I18n/Sozlukler.YedekDogrulama.cs` (YENİ) | `YedekDogrulamaMetinleri` TR+EN (konumsal record — eksik anahtar derleme hatası), `TurAdi` |
| `Kuyumcu.Web/Pages/Yedekler.razor` | `#yd-dogrula-alani`, `#yd-dogrula`, `#yd-dogrula-aciklama`, `#yd-dogrula-kilit`, `#yd-dogrula-sonuc`; talep türü rozeti; `DogrulamaTalepAc` (kilitliyken erken dönüş) |
| `Kuyumcu.Tests/YedekHttpTestleri.cs` | 4 yeni HTTP testi |
| `Kuyumcu.Tests/YedekEkranTestleri.cs` | 2 yeni bUnit testi + sahte API'de gerçekçi `dogrulama-talep` yanıtı, özette `bekleyenDogrulamaTalebi` |

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş):** `Pages/Yedekler.razor` (bu şeridin zinciri: P-77 → P-76).
Mevcut element id'leri değişmedi; yeni id'ler `yd-` önekli. Sabit renk/satır içi style yok; `Sozlukler.cs` hub'ına dokunulmadı.

## 6. DoD SÖZLEŞMESİ

| Varlık / kayıt | Oluşturma | Düzenleme | Pasife alma / geri dönüş | Yetki | Denetim | Ekran |
|---|---|---|---|---|---|---|
| `YedekTalebi` (`tur=DOGRULAMA`) — YENİ kayıt TÜRÜ | `POST /yedekler/dogrulama-talep` (201) | Gerekçe: `PATCH /yedekler/talep/{id}/gerekce` (P-56, BEKLIYOR iken; PATCH sözleşmesi: yoksa koru, `null` → 400 "silinemez" — gerekçe zorunlu alandır, P-56 testleri) | `PATCH /yedekler/talep/{id}` → `IPTAL` (satır silinmez, iz korunur). Geri dönüş: iptalden sonra yeni talep açılabilir (test + canlı adım 11) | `[RolGerekli("SAHIP")]` + `[ModulYetkisi("yedek")]` (sınıf düzeyi) | `YazmaKaydet` (açılış, hassas) + iptal `YazmaKaydet` | "Şimdi doğrula" düğmesi; talep satırında İptal et (BEKLIYOR) / `DegismezIpucu` (işlenmiş) — mevcut P-56 tablosu, tür rozetli |
| `yedek_talepleri.tur` | Sunucu belirler | **Değişmez** — tür, betiğin hangi işi koşacağıdır; yanlış türde talep iptal edilip doğrusu açılır | — | — | — | Rozet (salt okunur) |

Hard DELETE yok. Finansal varlık yok.

## 7. TESTLER

| Test | Tür | Sonuç |
|---|---|---|
| `YedekApi_DogrulamaTalebi_KuyrugaYazilir` | HTTP+DB | ✓ (201, DB'de tek DOGRULAMA satırı, bekleyen YEDEK talebi engellemez, özet+talepler `tur`) |
| `YedekApi_BekleyenTalepVarken_IkinciTalep409` | HTTP+DB | ✓ (önce özet `null`; 201 → 409, satır sayısı 1; iptal → yeni talep 201) |
| `YedekApi_DogrulamaTalebi_YetkisizRol403` | HTTP+DB | ✓ (MÜDÜR'e "yedek" modülü verilir, `yetkiler/benim` doğrulanır; TEZGÂHTAR ve MÜDÜR 403, 0 satır) |
| `YedekApi_DogrulamaTalebi_DenetimIzineYazilir` | HTTP+DB | ✓ (izde "doğrulama talebi açıldı" Detay'ı) |
| `Yedekler_SimdiDogrulaDugmesi_GorunurVeCalisir` | bUnit | ✓ (düğme açık, tıkla → POST, bant + `role=alert` sonuç, ardından sebepli kilit) |
| `Yedekler_BekleyenTalepVarken_DugmeKilitliVeSebepYazar` | bUnit | ✓ (disabled, sebep + tarih yazılı, tıklama istek göndermez) |

Koşumlar (`KUYUMCU_TEST_DB=kuyumcu_test_e`):
- Filtreli `FullyQualifiedName~Yedek`: önce **60** (P-56 raporu kapsamı) → sonra **66/66 Başarılı** (6 yeni).
  (İlk bUnit koşumunda `Yedekler_BekleyenTalepVarken_…` kırmızıydı: bUnit disabled düğmeye de tıklama iletiyor → sayfaya
  "kilitliyken istek gitmez" savunması eklendi; tekrar 66/66.)
- **Tam süit** (`--logger trx --results-directory …/trx-pP-77`): trx `Counters total="4867" executed="4867"
  passed="4867" failed="0"` — **0 kırmızı**. Tam süitin P-77 öncesi toplamı bu koşumda ölçülmedi.

## 8. MUTASYON TABLOSU

Yöntem: kaynak `scratchpad/mut-yedek/P-77/` altına kopya → `perl -0777 -pi -e` tek eşleşme → `dotnet build` → filtreli
`dotnet test` → yedeği geri kopyala → `cmp` birebir.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M77-1 | `DogrulamaTalepAc`: `if (bekleyen)` → `if (false)` (409 kapısı kaldırıldı) | `YedekApi_BekleyenTalepVarken_IkinciTalep409` (1/4) | cmp BİREBİR |
| M77-2 | Controller sınıf düzeyi `[RolGerekli("SAHIP")]` silindi | `YedekApi_DogrulamaTalebi_YetkisizRol403` (1/4) | cmp BİREBİR |
| M77-3 | Doğrulama `denetim.YazmaKaydet(...)` çağrısı etkisizleştirildi | `YedekApi_DogrulamaTalebi_DenetimIzineYazilir` (1/1) — DB kanıtı: izde yalnız `EKLEME · NULL` satırı kaldı | cmp BİREBİR |
| M77-4 | `@if (DogrulaKilitSebebi is { } …)` → `@if (false && …)` (düğme disabled, sebep metni yok) | `Yedekler_BekleyenTalepVarken_DugmeKilitliVeSebepYazar` + `Yedekler_SimdiDogrulaDugmesi_GorunurVeCalisir` (2/4) | cmp BİREBİR |
| M77-5 (ek) | 409 kapısından `t.Tur == DOGRULAMA` süzgeci kaldırıldı (bekleyen YEDEK talebi doğrulamayı engeller) | `YedekApi_DogrulamaTalebi_KuyrugaYazilir` (1/4) | cmp BİREBİR |

⚠ **Paralel derleme tuzağı (Anayasa md.1 — gizlenmedi):** M77-3'ün İLK koşumu **YEŞİL** çıktı. Kök neden: aynı ağaçta
başka şeridin eşzamanlı `dotnet build`/`dotnet test`'i vardı (`Kuyumcu.Tests.dll … being used by another process`);
koşulan ikili mutasyonlu kaynaktan derlenmemişti — kanıt: o koşumun `kuyumcu_test_e` denetim izinde `YazmaKaydet` satırı
DURUYORDU. Bu koşum kanıt sayılmadı; `--no-incremental` ile tekrar → KIRMIZI ve DB'de yalnız `NULL` detaylı satır.
M77-4'ün ilk denemesi `Kuyumcu.Web.pdb … used by another process` derleme hatasıyla koşmadı (kanıt sayılmadı), tekrarlandı.
Son mutasyondan sonra kaynak `cmp` ile birebir; temiz `dotnet build Kuyumcu.sln` → **0 Hata** (3 uyarı, hepsi başka
şeritlerin test dosyalarında: `EtiketEkranTestleri.cs(145)` xUnit1031 — SERİT D; `S25bSaticiYazdirTestleri.cs(448)`
xUnit2031 — S-2b).

## 9. DENETİM (`dotnet/denetim.sh`)

İlk koşum **BAŞARISIZ**: `✗ Kültürsüz biçimli ToString (Web) 28 (tavan 27 — ARTIŞ)` — kaynağı bu paket (kilit sebebindeki
tarih `ToString("dd.MM.yyyy HH:mm")`). Düzeltme: `ToString("g", CultureInfo.GetCultureInfo(Dil.Locale))` (sayfadaki
`SonTatbikatMetni` deseni). İkinci koşum: **`DENETİM TEMİZ.`** (rc=0).

## 10. CANLI DOĞRULAMA (port 5184, `kuyumcu_net`, kiracı `PAKETP-77-TEST Kuyumculuk` = `01M38FHTD380B8B350JPKBTA1P`)

Kiracı: `kiraci_davetleri`'ne tek kullanımlık davet (SHA-256 jeton özeti) → `POST /auth/kayit` → **201**.
Başlangıç `COUNT(*) tenants` = **10**.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | `GET /yedekler/ozet` | 200, `bekleyenDogrulamaTalebi:null` | **200** `"bekleyenDogrulamaTalebi":null` |
| 2 | `POST /yedekler/dogrulama-talep` (SAHİP) | 201/200 | **201** `{"id":"01M38FJ9B1W553KQ8M9NNKDX57","tur":"DOGRULAMA","durum":"BEKLIYOR",…,"mesaj":"Doğrulama talebi kuyruğa alındı; zamanlayıcı en geç 60 saniye içinde başlatır."}` |
| 3 | Aynı istek tekrar | 409 | **409** "Bu işletme için bekleyen bir yedek doğrulama talebi zaten var; sonuçlanmadan ikinci doğrulama talebi açılamaz." |
| 4 | `GET /yedekler/ozet` | bekleyen talep dolu | **200** `"bekleyenDogrulamaTalebi":{"id":"01M38FJ9B1W553KQ8M9NNKDX57",…}` |
| 5 | `GET /yedekler/talepler` | `tur` alanı | **200** `…"tur":"DOGRULAMA"` |
| 6 | MÜDÜR kullanıcısı (`POST /kullanicilar` 201) + `PUT /yetkiler` MÜDÜR'e `yedek` (200; `yetkiler/benim` → `["rapor","yedek"]`) → `POST dogrulama-talep` | 403 | **403** "Bu işlem için gerekli rol: SAHIP" |
| 7 | SQL kuyruk | 1 DOGRULAMA/BEKLIYOR satırı | ✓ (aşağıda) |
| 8 | SQL denetim izi | açık kayıt | ✓ (aşağıda) |
| 9 | `PATCH /yedekler/talep/{id}` iptal (pasife alma) | 200 IPTAL | **200** `"durum":"IPTAL"` |
| 10 | `GET /yedekler/ozet` | `null` | `"bekleyenDogrulamaTalebi":null` |
| 11 | `POST dogrulama-talep` (iptalden sonra) | 201 | **201** `01M38FK0X1TRB60PGGNJPNQ1JB` → ardından o da iptal edildi (200) |

**SQL kanıtı** (`kuyumcu_net`, `--default-character-set=utf8mb4`):
```
SELECT id,tur,durum,gerekce,iptal_gerekce FROM yedek_talepleri WHERE tenant_id='01M38FHTD380B8B350JPKBTA1P' ORDER BY olusturuldu;
01M38FJ9B1W553KQ8M9NNKDX57  DOGRULAMA  IPTAL  Ekrandan başlatılan yedek doğrulama talebi  canli dogrulama sonunda talep iptal ediliyor
01M38FK0X1TRB60PGGNJPNQ1JB  DOGRULAMA  IPTAL  Ekrandan başlatılan yedek doğrulama talebi  canli dogrulama sonunda ikinci talep de iptal
(adım 7'de, iptalden önce: 01M38FJ9B1W553KQ8M9NNKDX57 DOGRULAMA BEKLIYOR)

SELECT varlik_id,eylem,hassas,detay FROM denetim_kayitlari WHERE tenant_id=… AND varlik_turu='YedekTalebi' ORDER BY tarih,id;
01M38FJ9B1W553KQ8M9NNKDX57  EKLEME      1  yedek doğrulama talebi açıldı (ekrandan · son başarılı yedek geri yüklenip doğrulanacak)
01M38FJ9B1W553KQ8M9NNKDX57  EKLEME      0  NULL
NULL                        OKUMA       0  liste: 1 kayıt
01M38FJ9B1W553KQ8M9NNKDX57  EKLEME      1  yedek talebi iptal edildi · gerekçe: canli dogrulama sonunda talep iptal ediliyor
01M38FJ9B1W553KQ8M9NNKDX57  GUNCELLEME  0  Durum, IptalGerekce, Islendi
01M38FK0X1TRB60PGGNJPNQ1JB  EKLEME      1  yedek doğrulama talebi açıldı (…)
01M38FK0X1TRB60PGGNJPNQ1JB  EKLEME      0  NULL
01M38FK0X1TRB60PGGNJPNQ1JB  EKLEME      1  yedek talebi iptal edildi · gerekçe: canli dogrulama sonunda ikinci talep de iptal
01M38FK0X1TRB60PGGNJPNQ1JB  GUNCELLEME  0  Durum, IptalGerekce, Islendi
```
Test talepleri **betik tüketmeden** iptal edildi (gerçek geri yükleme provası canlı veritabanında tetiklenmedi).

**Temizlik:** API (`dotnet run` PID 65126; dinleyen 65172) `kill` → `lsof -nP -iTCP:5184 -sTCP:LISTEN` **boş**.
`tenant_id` sütunu olan **70 tablodan** test kiracısının satırları silindi (kalan satır toplamı **0**); davet satırı ve
`tenants` satırı silindi. Son ölçüm: `COUNT(*) tenants` = **10** (başlangıç 10) · `PAKETP-77%` = 0 · davet = 0 ·
`QA-TEST-%` = 4 (dokunulmadı). Jeton/şifre/yanıt dosyaları `printf ''` ile boşaltıldı.

## 11. DEĞİŞEN DOSYALAR (`git status --porcelain | grep -v -E "/(bin|obj)/"` — bu paketin payı)

```
 M dotnet/Kuyumcu.Api/Data/Entities.cs
 M dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
?? dotnet/Kuyumcu.Api/Data/Migrations/   (içinde: 20260924004513_P77YedekTalepTuru.cs, .Designer.cs, KuyumcuDbContextModelSnapshot.cs güncellendi)
?? dotnet/Kuyumcu.Api/Modules/Yedek/     (YedekModulu.cs)
?? dotnet/Kuyumcu.Contracts/             (Sayfalar/YedeklerSozlesmeleri.cs)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.YedekDogrulama.cs
?? dotnet/Kuyumcu.Web/Pages/Yedekler.razor
?? dotnet/yedek-al-ve-dogrula.sh
?? dotnet/Kuyumcu.Tests/ …               (YedekHttpTestleri.cs, YedekEkranTestleri.cs)
```
(Dizinlerin çoğu depoda henüz izlenmiyor — `??` satırları paket öncesinden beri öyle; commit YOK.)

## 12. KOŞUL KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| POST yetkili kullanıcıda 200/201 + kuyrukta 1 kayıt | ✓ | `YedekApi_DogrulamaTalebi_KuyrugaYazilir` · canlı 2 + SQL 7 |
| Bekleyen talep varken ikinci istek 409 | ✓ | `…_IkinciTalep409` · M77-1 · canlı 3 |
| Yetkisiz rolde 403 | ✓ | `…_YetkisizRol403` (MÜDÜR'e modül verilmişken bile) · M77-2 · canlı 6 |
| Talep denetim izine yazılır | ✓ | `…_DenetimIzineYazilir` · M77-3 · SQL 8 |
| Ekranda düğme; tıklamadan sonra başarı bandı + sebepli kilit | ✓ | `Yedekler_SimdiDogrulaDugmesi_GorunurVeCalisir`, `Yedekler_BekleyenTalepVarken_…` · M77-4 |
| GET özet bekleyen doğrulama bilgisini taşır | ✓ | HTTP testleri · canlı 1/4/10 |
| Doğrulama yedek dosyasını silmez/değiştirmez | ✓ (kod) | Betik `tatbikat_kos`: dosyayı `coz < "$dosya"` ile yalnız okur; `saklama_temizle` yalnız `yedek_al`'da |
| P-56 davranışı ve testleri bozulmadı | ✓ | `FullyQualifiedName~Yedek` 66/66; tam süit 4867/4867 |
| Kapı (1) build 0 hata | ✓ | § 8 son satır |
| Kapı (2) paketin testleri 0 başarısız | ✓ | 66/66 |
| Kapı (3) denetim.sh sayaçları | ✓ | § 9 `DENETİM TEMİZ.` |
| Kapı (4) mutasyon kanıtı | ✓ | § 8 (5/5 kırmızı, cmp birebir) |
| Kapı (5) canlı doğrulama | ✓ | § 10, 11 adım |
| Kapı (6) test kiracısı silindi | ✓ | tenants 10 → 10 |
| Kapı (7) yeni kırmızı yok | ✓ | tam süit 0 kırmızı (fark 0) |

## 13. AÇIK KALANLAR / KURUCUYA NOTLAR

1. **Rol kararı (S1):** şartname MÜDÜR'ü de saydı; ölçülen mevcut kapı yalnız SAHİP. MÜDÜR'e açılması istenirse ürün
   kararı gerekir (yedek sistem genelidir — Mehmet'in görüşü alınmalı).
2. **Betik tüketimi canlıda koşulmadı:** `talep_kuyrugu`'nun DOGRULAMA dalı `bash -n` ile sözdizimi temiz ve kod okumasıyla
   doğrulandı; gerçek geri yükleme provası canlı veritabanında bilinçli olarak tetiklenmedi (tüm kiracıların verisini ayrı
   şemaya geri yükler; şifreleme anahtarı + yedek dizini ortamı gerekir). İlk üretim/QA kurulumunda Batuhan'ın bir kez
   `yedek-al-ve-dogrula.sh talep` ile uçtan uca koşması önerilir.
3. **Eşzamanlılık:** 409 kapısı `AnyAsync` + ekleme (T-3/P-56 ile aynı desen); aynı milisaniyede iki istek teorik olarak iki
   satır açabilir. Benzersiz kısıt (tenant+tur+BEKLIYOR) MySQL'de kısmi indeks gerektirdiğinden açılmadı; betik iki
   satırı da işler (zararsız, yalnız mükerrer prova).
4. **Hiç yedek yokken** sunucu talebi kabul eder, betik HATA ile kapatır; ekran bu durumda düğmeyi sebebiyle kilitler (S6).
5. **Paylaşılan scratchpad:** tüm şeritler aynı scratchpad dizinini kullanıyor (ör. başka şeridin `tam.rc` dosyası bu
   şeridin koşum dosyası sanılabilirdi). Kanıt olarak yalnız bu pakete özgü `trx-pP-77/` sonucu kullanıldı.
   Orkestratöre öneri: şerit başına alt dizin.
6. Mevzuat hükmü yok (VUK/KDV/MASAK kapsamında değil); birincil kaynak araştırması gerekmedi.
