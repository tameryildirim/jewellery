# PAKET P-22 — SATIŞ / FATURA / SİPARİŞ / SAYIM / POS EKRAN BORÇLARI

**Dalga:** DALGA 5 · **Tarih:** 2026-09-21 · **Roller:** Taşkın (backend) + Ertuğ (frontend)
**Kimlikler:** B-3 · B-26 · B-33 · B-44 · B-45 · B-46 · B-55 · B-56
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 503-519 · **Sürüm:** 1.0

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam et modu, varsayım yok)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| **P-15** | ✅ VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-15-uygulama-raporu.md` |
| **P-21** | ✅ VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-21-uygulama-raporu.md` · `Modules/Satis/SatisModulu.P21.cs` mevcut · `siparisler/{id}/satisa-cevir` ucu canlı |

Kapsam daraltması **gerekmedi**.

---

## § 1 — SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI

| Plan iddiası | Ölçüm | Sonuç |
|---|---|---|
| Fatura numarası taslakta tüketiliyor | `FaturaModulu.cs` 261 · 713 · 944 — **üç** ayrı yerde, belge `Durum="TASLAK"` doğarken | ✅ DOĞRU |
| `/sayim` DURUM sütununda düz metin "İptal Et" | Sütun **zaten** sözlükten çeviriyordu (Sayim.razor:70); ham kod ise **detay başlığında** basılıyordu (`@a.Durum`) | ⚠ KISMEN — gerçek borç başlıktaydı, düzeltildi |
| Gün sonu kasa sayımı "hiçbir yerden girilemiyor" | `/kasalar` ekranından **girilebiliyordu**; `/raporlar/kasa` sütunları dolu ama **giriş yolu yoktu** | ⚠ KISMEN — plan DoD'u zaten "`/raporlar` kasa SAYILAN NAKİT ve FARK dolu" diyor, o hedefe sadık kalındı |
| POS "Bağlı Kasa" düzeltme yolu yok | Sunucu tahsilat **yokken** düzeltmeye izin veriyordu (S-3f); tahsilat varken **çıkmaz** (pasife al + yeni cihaz) | ⚠ KISMEN — çıkmaz kaldırıldı (B-55) |
| `GunKapanisi.cs` yeterli mi? | `Data/GunKapanisi.cs` **MEVCUT** (sayılan nakit · defter bakiye · fark · gerekçe · yeniden açma) | ✅ Ayrı tablo **gerekmedi** |
| `Pages/Raporlar/` dizini yok | Doğrulandı — dosya `Pages/KasaRaporu.razor` | ✅ |

Mevzuat hükmü kodlanmadı; bu paket vergi/beyan kuralına dokunmaz.

---

## § 2 — YAPILANLAR (kimlik kimlik)

### B-3 — TASLAK fatura ve kalemleri düzenlenebilir · seri tek noktada tüketilir

1. **`Data.Fatura.FaturaNo` → `string?`** (migration). TASLAK belgenin numarası **YOKTUR**.
2. **YENİ `Modules/Fatura/FaturaSeriKapisi.cs`** — seriyi tüketen **TEK NOKTA**:
   - `Tuket(db, fatura, yil)` fail-closed: numarası olan belgeye ikinci kez numara vermez, `Durum=="TASLAK"` olan belgeye hiç vermez.
   - `Isaret(faturaId)` → `TASLAK:{faturaId}` — hareket damgası **seriden değildir**, numara yakmaz.
   - `IsaretiKaldir` — taslak iptal edilince damga düşer, hareket yeniden faturalanabilir olur.
3. `FaturaModulu`'nun **üç** oluşturma yolu da artık `FaturaNo = null` yazar; hareketlere taslak işareti damgalanır. Yerel `SiradakiFaturaNo` yardımcısı **kaldırıldı**.
4. `DurumGuncelle`: `TASLAK→KESILDI` numarayı **şimdi** alır; `TASLAK→IPTAL` işareti temizler.
5. `BankaModulu` dekont→fatura yolu (belge doğrudan `KESILDI` doğar) da aynı kapıdan geçer; yerel üretici kaldırıldı.
6. `Data/FaturaHareketBagi.cs` taslak işaretini tanır (`KaynakTaslak`); **mutabakat davranışı korunur** — taslağı olan satış "faturasız" görünmez.
7. **YENİ uçlar:** `PATCH /faturalar/{id}` (başlık) ve `PATCH /faturalar/{id}/kalemler/{kalemId}` — yalnız TASLAK; PATCH sözleşmesi *gövdede yoksa koru, null ise sil*.
8. Ekran: TASLAK satırında **Düzenle** (Cekmece), kalem modalında **satır içi** düzenleme, numara sütununda "Numara kesimde verilir".

### B-26 + K-23 — sipariş durum geçişi onay ister ve geri alınabilir

- Onay + gerekçe kipi **zaten** vardı (S-3f). Bu pakette **K-23** uygulandı: `ATOLYEDE`/`HAZIR`'dan **geri dönüş AÇILDI**, dört şart birlikte:
  1. **Yetki** — `SiparisServisi.GeriAlmaRolleri = [SAHIP, MUDUR]`; tezgâhtar ileri götürür, geri alamaz (403).
  2. **Gerekçe** — `GerekceGerekli` geri yönde true; boş/kısa gerekçe 400.
  3. **İz** — `siparis_gecisleri` append-only: kim · ne zaman · hangi durumdan hangi duruma · neden.
  4. **Görünürlük** — geçiş silinmez; kartta ve `/gecisler` ucunda durur.
- Atölye siparişinde `HAZIR → ONAYLANDI` **açılmadı**: malın atölyeden çıktığı iddiası ekrandan doğrulanamaz (md.1). İki adım geri gerekiyorsa iki geçiş yazılır, ikisi de izde kalır.
- Ekran: geri yönlü hedefler ayrı etiketli düğme (`sipd-geri-{durum}`); yetki yoksa **kilitli + sebep yazılı** (UX-S30). `geriYonluGecisler` · `geriAlmaYetkisi` · `geriAlmaRolleri` **sunucudan** gelir (UX-S21).
- **"Bu sürümde açık değildir" metni kaldırılmadı çünkü hiç yoktu** (grep ile doğrulandı).

### B-33 — `/sayim` DURUM sütunu

- Liste sütunu `DurumEtiketi()` ile sözlükten çevrilir, hücreye `id="sy-durum-{id}"` verildi; eylem metni bu sütunda **yok**, ayrı eylem sütununda.
- Detay başlığındaki **ham kod** (`@a.Durum`, `@a.KapsamTur`) da sözlüğe bağlandı (UX-S31).

### B-44 — sayım oturumunun adı düzeltilebilir

- **YENİ uç** `PATCH /sayim/{id}` — `ad` ve `notlar`; İPTAL oturum düzenlenemez (gerekçesi ekranda `DegismezIpucu` ile yazılı).
- Ekran: `SatirIciDuzenle` (tek alanlı düzenleme kalıbı).

### B-45 — gün sonu kasa sayımı `/raporlar` kasa defterinden girilebilir

- Yeni uç **açılmadı** (mevcut `POST /kasalar/{id}/gun-kapanisi` kullanıldı — ikinci bir yazma yolu doğurmamak için).
- `Pages/KasaRaporu.razor`: bugünün AÇIK satırında **"Sayımı gir"** düğmesi → Cekmece (sayılan nakit + fark gerekçesi) → POST → banda **sunucunun döndürdüğü fark** yazılır (UX-S27).
- Geçmiş güne düğme **çizilmez**, sebebi yazılır: sunucu yalnız içinde bulunulan yerel günü kapatır (UX-S30).
- Negatif sayımda alan altı Türkçe mesaj; sessiz etkisizlik yok.

### B-46 / B-55 / B-56 — POS bağlı kasa

- **B-46:** tahsilat yokken PATCH ile düzeltme yolu zaten vardı; korundu ve canlıda doğrulandı.
- **B-55 (YENİ):** `POST /pos/cihazlar/{id}/kasa-goc` — tahsilat **varken** bağlı kasayı taşır. Dört şart: rol kapısı (SAHIP/MUDUR) · gerekçe ≥ 20 karakter · `pos_kasa_gocleri` tutanağı + `DenetimServisi.YazmaKaydet` · tutanak `GET /pos/cihazlar/{id}/kasa-gocleri` ile görünür ve **append-only**.
  **Geçmiş değişmez:** yazılmış `pos_tahsilatlari` satırları kendi `kasa_hareket_id`'siyle eski kasada kalır; yanıt `gecmisEtkilenmedi: true` der ve ekran bunu açıkça yazar.
- **B-56:** PATCH ile sessiz kasa değişimi **hâlâ 409** — ama mesaj artık çıkmaz değil, göç ucunu gösterir. 5198'de **canlı** denendi (§ 7 C20).
- Ekran: kilitli alanın altındaki ölü `DegismezIpucu` yerine **gerçek düğme** → Cekmece (yeni kasa + `GerekceAlani`), uyarı metni "yazılmış tahsilatlar eski kasada kalır".

---

## § 3 — DoD SÖZLEŞMESİ (Bertan koşulu)

| Varlık / kayıt | Düzenleme yolu | Silme / pasife alma | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|
| `faturalar` (TASLAK) | `PATCH /faturalar/{id}` | hard DELETE **YOK** — `PATCH {id}/durum` = IPTAL | `[RolGerekli(SAHIP,MUDUR,MUHASEBE)]` | `DenetimYazmaDinleyicisi` (otomatik) | **Düzenle** düğmesi (Cekmece) |
| `fatura_kalemleri` (TASLAK) | `PATCH /faturalar/{id}/kalemler/{kalemId}` | hard DELETE **YOK** (satır silinmez, taslağın tamamı iptal edilir) | aynı | otomatik | satır içi düzenleme |
| `faturalar` (KESILDI/ODENDI) | **salt okunur** | IPTAL | — | — | `DegismezIpucu` + gerekçe |
| `sayim_oturumlari` | `PATCH /sayim/{id}` (ad · not) | `POST {id}/iptal` (pasifleştirme) | `[ModulYetkisi("sayim")] + [RolGerekli×4]` | otomatik | satır içi; İPTAL'de `DegismezIpucu` |
| `pos_cihazlari.kasa_id` | tahsilat yokken `PATCH`, varken `POST {id}/kasa-goc` | cihaz `pasife-al` | `[RolGerekli(SAHIP,MUDUR)]` | **`DenetimServisi.YazmaKaydet` çağrılır** | Cekmece + tutanak listesi |
| **`pos_kasa_gocleri` (YENİ)** | **append-only, DEĞİŞMEZ** | yok | okuma uçta, yazma SAHIP/MUDUR | tutanağın kendisi + YazmaKaydet | tutanak listesi |
| `gun_kapanislari` | yeniden açma (SAHİP, gerekçeli) | yok | mevcut | mevcut | `/raporlar/kasa` giriş + `/kasalar` |
| `siparis_gecisleri` | **append-only, DEĞİŞMEZ** | yok | — | izin kendisi | geçiş geçmişi |

**Neden salt okunur:** kesilmiş fatura numara serisini tüketmiş ve belge dışarıya verilmiştir; `pos_kasa_gocleri` ve `siparis_gecisleri` birer tutanaktır — değiştirilebilseydi "hangi tahsilat hangi kasaya düştü" / "kim neyi geri aldı" sorusunun cevabı sonradan yazılabilir olurdu.

---

## § 4 — MIGRATION

**Ad:** `20260921073615_P22TaslakFaturaSeriVePosKasaGocu`
**Dosyalar (0 bayt DEĞİL):** `.cs` **3 684 bayt** · `.Designer.cs` **207 928 bayt**
**Up():** `faturalar.fatura_no` → `nullable` · `pos_kasa_gocleri` tablosu + `(tenant_id, pos_cihaz_id, olusturuldu)` indeksi
**Down():** tablo düşürülür, `fatura_no` NULL'ları `''` yapılıp NOT NULL'a döner (üretilen hâli okundu, boş değil)
**Uygulama:** `dotnet ef database update --connection …kuyumcu_net` → **başarılı**
**Kanıt:** `__EFMigrationsHistory` **59 → 60** · `SHOW COLUMNS FROM faturalar LIKE 'fatura_no'` → `varchar(30) YES MUL NULL` · `SHOW TABLES LIKE 'pos_kasa_gocleri'` → var
`--no-build` **kullanılmadı**; build önce yeşildi.

---

## § 5 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Tam süit — **ÖNCE** (P-22 dokunmadan) | 3 515 | 3 509 | 6 | 6'sı da P-22'nin bilerek değiştirdiği sözleşmelerdi (aşağıda) |
| Tam süit — **SONRA** | **3 542** | **3 542** | **0** | `trx`: `scratchpad/trx-pP-22/` |

**Yeni test sayısı: 27** (3 515 → 3 542)
- `Kuyumcu.Tests/P22SunucuTestleri.cs` — **12** (B-3 ×6, B-44 ×3, B-55 ×3)
- `Kuyumcu.Tests/P22HttpTestleri.cs` — **8** (K-23 dört şartı, B-55, B-56, B-3, B-44 boru hattı)
- `Kuyumcu.Tests/P22EkranTestleri.cs` — **5** (bUnit: B-3, B-33, B-44, B-45 ×2)
- `S3fSunucuTestleri` — eski K-23 kilidi **1** test yerine **3** yeni test (net +2)

**Sözleşmesi bilerek değiştirilen eski testler (gizlenmedi, yeniden yazıldı):**

| Test | Eski varsayım | Yeni gerçek |
|---|---|---|
| `FaturaTestleri.FaturaNo_MevcutNumarayiTekrarUretmemeli` | taslak numara alır | numara kesimde verilir; MAX+1 kuralı orada sınanır |
| `FaturaFistenTestleri` ×2 · `FaturaOzelMatrahTestleri` ×2 | damga = fatura no | damga = taslak işareti; belge kesilince numaraya döner |
| `MutabakatTestleri.HareketsizFatura_EskiDavranisKorunur` | `FaturaNo` "FTR-" ile başlar | TASLAK belgede `null` |
| `SiparisDurumMakinesiTestleri.Izinli_gecis_listesi…` | ATOLYEDE/HAZIR geri dönüş yok | **K-23**: geri dönüş var |
| `S3fSunucuTestleri.Atolyede_ve_Hazir…_ACILMADI` | kilit | **K-23** ile kaldırıldı → 3 yeni test |
| `S3fSunucuTestleri.Tahsilati_OLAN_cihazda…REDDEDILIR` | mesaj "pasife al" | mesaj göç ucunu gösterir |
| `S3fEkranTestleri.…kasa_kilitli_ve_sebep…` | ölü uyarı | gerçek göç düğmesi |
| `UX1C2EkranTestleri.Faturalar_kalem_modalinda_DUZENLEME_YOK…` | kalem değişmez | TASLAK kalemi düzenlenebilir |
| `DuzeltmePolitikasiTestleri` · `OlcumKapisiTestleri` | 61 varlık | 62 (yeni `PosKasaGocu`) |

---

## § 6 — MUTASYON KANITI

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| **M22-1** | `FaturaSeriKapisi`: TASLAK koruması `if (false)` | `P22SunucuTestleri.B3_seri_kapisi_TASLAK_belgeye_numara_VERMEZ` | `cmp` **birebir aynı** |
| **M22-2** | `DurumGuncelle`: KESILDI'de seri tüketimi atlanır | `B3_TASLAK_fatura_numara_TUKETMEZ_kesimde_alir` · `B3_IPTAL_edilen_TASLAK_seride_BOSLUK_birakmaz` · `P22HttpTestleri.B3_taslak_fatura_PATCH_ucu_calisir…` | `cmp` **birebir aynı** |
| **M22-3** | Durum makinesi: `HAZIR` geri hedefleri silinir | `SiparisDurumMakinesiTestleri.Izinli_gecis_listesi…` · `S3fSunucuTestleri.K23_Atolyede_ve_Hazir…_ACIK` · `P22HttpTestleri.K23_tezgahtar…` · `K23_geri_alinan_gecis_IZDE…` · `K23_gecis_dokumu…` | `cmp` **birebir aynı** |
| **M22-4** | `SiparisServisi`: geri alma rol kapısı `if (false)` | `P22HttpTestleri.K23_tezgahtar_HAZIRdan_geri_ALAMAZ_mudur_ALIR` | `cmp` **birebir aynı** |
| **M22-5** | `PosServisi.KasaGoc`: gerekçe eşiği `< 0` | `P22SunucuTestleri.B55_gerekcesiz_kasa_gocu_400_doner_ve_kasa_DEGISMEZ` | `cmp` **birebir aynı** |
| **M22-6** | `SayimServisi.Guncelle`: boş ad kontrolü `< 0` | `P22SunucuTestleri.B44_bos_ad_400_doner_ve_KAYIT_DEGISMEZ` | `cmp` **birebir aynı** |
| **M22-7** | `KasaRaporu.razor`: negatif sayım koruması `if (false)` | `P22EkranTestleri.B45_negatif_sayim_SESSIZ_gecmez_alan_alti_mesaj_verir` | `cmp` **birebir aynı** |

Hiçbir mutasyon "testi kırmadan" geçmedi. Son mutasyondan sonra **temiz build: 0 hata, 0 uyarı**.

---

## § 7 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198` (nohup, **PID 62506**, iş sonunda `kill`).
Kiracı: `kiraci_davetleri`ne SHA-256 özetli satır + `POST /auth/kayit` → **201**, **`PAKET22-TEST`**.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C1 | `POST /faturalar` (toplam 1 200) | 200, `faturaNo = null`, `TASLAK` | **200** · `faturaNo: None` · `durum: TASLAK` |
| C2 | `PATCH /faturalar/{id}` açıklama + toplam 2 400 | 200; KDV yeniden hesaplanır | **200** · `toplam 2400.00` `kdvTutar 400.00` `araToplam 2000.00` |
| C3 | `PATCH {id}/durum → KESILDI` | 200; numara **şimdi** | **200** · `FTR-2026-000001` |
| C4 | Kesilmiş belgeyi `PATCH` | **400** | **400** "Yalnız TASLAK fatura düzenlenebilir; bu belge KESILDI durumunda." |
| C5 | 2 taslak aç → ikisini de İPTAL → yeni taslak kes | seride **boşluk yok** → `000002` | iptal **200/200** · kes **200** · **`FTR-2026-000002`** (eski davranışta `000004` olurdu) |
| C6 | Sipariş aç → ONAYLANDI → ATOLYEDE → HAZIR | 200×4 | **200 · 200 · 200 · 200** · `S-2026-000001` |
| C7 | Gerekçesiz `HAZIR → ATOLYEDE` | **400** | **400** "Bu geçiş için gerekçe zorunlu (en az 10 karakter)" |
| C8 | Gerekçeli `HAZIR → ATOLYEDE` | 200 | **200** · `durum: ATOLYEDE` |
| C9 | `GET /siparisler/{id}/gecisler` | geri dönüş **izde görünür** | **200** · 5 satır · son satır `('HAZIR','ATOLYEDE','müşteri beğenmedi…')` |
| C10 | `POST /sayim` ad "Vitirn" | 200 | **200** · `ad: Vitirn` `durum: ACIK` |
| C11 | `PATCH /sayim/{id}` ad düzelt | 200 | **200** · `ad: Vitrin sayımı` |
| C12 | `PATCH /sayim/{id}` ad `"  "` | **400** | **400** "Sayım oturumunun adı boş olamaz" |
| C13 | `POST /kasalar/{id}/gun-kapanisi` 1 500 | 200; fark hesaplanır | **200** · `sayilan 1500` `defter 0.0` `fark 1500.0` |
| C14 | `GET /raporlar/kasa-defteri` | SAYILAN NAKİT ve FARK **DOLU** | **200** · `{'durum':'KAPALI','sayilanNakit':1500.0,'fark':1500.0}` |
| C15 | Aynı günü ikinci kez kapat | **409** | **409** "21.09.2026 günü bu kasa için zaten kapatılmış." |
| C16 | `POST /pos/cihazlar` | 200 | **200** |
| C17 | Tahsilat **yokken** `PATCH` bağlı kasa (B-46) | 200 | **200** · kasa değişti |
| C18 | KART satış (5 000 TL) | 200; POS tahsilatı doğar | **200** · `tahsilatVar: True` |
| C19 (B-56) | Tahsilat **varken** `PATCH` bağlı kasa | **409** + göç ucu | **409** "…sessizce değiştirilemez. Doğru yol: POST /api/v1/pos/cihazlar/{id}/kasa-goc…" |
| C20 (B-55) | Gerekçesiz `kasa-goc` | **400** | **400** "Kasa göçü için gerekçe zorunlu (en az 20 karakter)." |
| C21 (B-55) | Gerekçeli `kasa-goc` | 200; geçmiş etkilenmez | **200** · `tahsilatSayisi 1` · `gecmisEtkilenmedi True` |
| C22 | `GET …/kasa-gocleri` | tutanak görünür | **200** · `[('P22 İkinci Kasa','P22 Merkez Kasa',1,'cihaz yanlış kasaya ba','P22 Test')]` |

### SQL kanıtı (kiracı silinmeden önce)

```
pos_tahsilat        1
pos_goc             1
taslak_faturalar    2      (ikisi de IPTAL, numarasız)
numarali_faturalar  2      FTR-2026-000001 KESILDI · FTR-2026-000002 KESILDI
siparis_gecis       5
gun_kapanis         1  sayilan 1500.00  fark 1500.00
```

### Temizlik

| Kontrol | Sonuç |
|---|---|
| API durduruldu (`kill 62506`) | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş** |
| 5199 (kurucunun sunucusu) | **dokunulmadı** — hâlâ dinliyor |
| Kiracı satırları | `tenant_id` kolonu olan **61 tablodan** DELETE → **KALAN_SATIR = 0** |
| Davet satırı | silindi (0) |
| `tenants` sayısı | başlangıç **8** → son **8** ✔ |
| `QA-TEST-*` fikstürleri | **dokunulmadı** |
| `__EFMigrationsHistory` | 60 (migration yerinde) |

---

## § 8 — DENETİM (`dotnet/denetim.sh`)

**Eklenen blok:** `# >>> P-22 … # <<< P-22` (ölçüm komutları yorumda, tabanlar **taze ölçüldü**)

```
═══ P-22 belge serisi ve durum sütunu kapısı ═══
  ✓ P-22: SERI_TUKETIM_NOKTASI                     1 (tek nokta: FaturaSeriKapisi.Tuket)
  ✓ P-22: TASLAK_NUMARA_SIZINTISI                  0 (numara yalnız kesimde yazılır)
  ✓ P-22: DURUM_SUTUNU_EYLEM_METNI                 0 (durum sütununda eylem yok)
```

- `SERI_TUKETIM_NOKTASI ≤ 1` (cırcır — yalnız azalır). Taban 3 → **1**.
- `TASLAK_NUMARA_SIZINTISI = 0` — belge modüllerinde (`Fatura` + `Banka`) `FaturaNo =` ataması yalnız `null` ya da seri kapısı üzerinden. Kapsam bilerek dar: `hareketler.fatura_no` HURDA_ALIS satırlarında **tedarikçinin** numarasını taşır, ona bakan sayaç yalan söylerdi.
- `DURUM_SUTUNU_EYLEM_METNI = 0` — `-durum-` id'li `<td>` içinde `@onclick` yok.
- `BEKLENEN_VARLIK` 61 → **62** (yeni `PosKasaGocu`), `ham/olcum-2026-09-18.json` ile birlikte güncellendi. **Tavan artırılmadı**; `Pages` ölü düğme ve ModalPencere tavanları **korundu** (yeni iki form `ModalPencere` yerine **Cekmece** ile yazıldı — DoD EK md.2).

### ⚠ DENETİM TAM TEMİZ DEĞİL (Anayasa md.1 — gizlenmiyor)

```
  ✗ P-02: QA_TUR_SURESI_OLCUM (BK-39)  devir turu 6 sonrası tur 7 KOŞTU ama süre ölçümü YAZILMADI
DENETİM BAŞARISIZ
```

Bu ihlal **P-22 dışıdır ve P-22 tarafından doğurulmamıştır**: `ham/qa-tur-suresi.json` içinde `gecerli_ortalama_saat: null`, `devir_turu: 6`, ve `ham/qa-turu-7.json` mevcut — yani devir penceresi orkestratör tarafındaki QA turu 7 süresi yazılmadığı için kapandı. **Uydurma bir süre yazmadım** (md.1). Kapatması orkestratöre/QA turunu koşan tarafa aittir. P-22'nin kendi üç sayacı **yeşildir** ve paketin dokunduğu hiçbir sayaç kırmızı değildir.

---

## § 9 — SAPMALAR VE GEREKÇELERİ

1. **K-23'ün "yetki anahtarı" şartı ROL kapısıyla karşılandı.** Modül anahtarları `Kuyumcu.Core.Erisim.RotaModulleri.Harita`dan **türetilir** — rotası olmayan bir "eylem anahtarı" bu mimaride yoktur; uydurulsaydı ikinci bir yetki gerçeği doğardı. Eylem düzeyinde "herkes yapamaz" kapısının projedeki yerleşik deseni `SiparisServisi.IptalRolleri`dir; `GeriAlmaRolleri` birebir aynı desendir ve HTTP testiyle kilitlidir. *(Koşul metni "P-09'un anahtar göçü desenine uy" diyordu; anahtar göçü yalnız **yeniden adlandırma** için kullanılır — yeni bir anahtar göç haritasına giremez, `YetkiModulu.cs:74` yorumunda yazılı.)*
2. **Fatura kalemi SİLİNMEZ.** "Kalemleri düzenlenebilir" DoD'u **değer düzenlemesiyle** karşılandı; satır silme açılmadı çünkü finansal varlıkta hard DELETE yasaktır. Taslağın tamamı numara yakmadan iptal edilebildiği için kullanıcı çıkmazda değildir.
3. **KUYUM (özel matrah) taslağının toplamı başlıktan değiştirilemez**; kalem düzenlemesinde has bedelinin dayanağı **eski satırdan geri çözülür**, çözülemiyorsa **durulur** (güncel kura sessizce geçilmez — md.1). Ekranda ve 400 mesajında doğru yol yazılıdır.
4. **B-45 için yeni uç açılmadı**; mevcut gün kapanışı ucu kullanıldı (tek yazma yolu). Sunucu yalnız içinde bulunulan yerel günü kapattığı için geçmiş gün satırında düğme yok, sebebi yazılı.
5. **Yeni iki form `ModalPencere` değil `Cekmece`** ile yazıldı: ikisi de çok alanlı **form**tur, yıkıcı onay değil (DoD EK md.2) — ve `ModalPencere` tavanı artmadı.
6. **Tahmin aşımı:** plan 300 ajan-dk; gerçekleşen bunun üzerinde. Sebep: `FaturaNo` nullable'a çevrilmesi 25 dosyaya dokundu ve 6 eski testin sözleşmesini değiştirdi; ayrıca canlı doğrulamada kiracı fikstürü (davet → kayıt → kur → stok → kasa → POS → kart satışı) baştan kuruldu.

---

## § 10 — DOKUNULAN SAYFALAR (DoD EK 2 — tek dokunuş)

`Pages/Faturalar.razor` · `Pages/Sayim.razor` · `Pages/KasaRaporu.razor` · `Pages/Pos.razor` · `Pages/SiparisDetay.razor`
Hiçbiri bu dalgada başka bir paket tarafından açılmadı (P-22 dalga 5'in bu dilimindeki tek el).

---

## § 11 — DEĞİŞEN DOSYALAR

> Depoda tek commit olduğu için `git status --porcelain` neredeyse tüm ağacı "untracked" gösteriyor; aşağıdaki liste **bu pakette elle değiştirilen/oluşturulan** dosyalardır.

**Sunucu**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                     (Fatura.FaturaNo nullable + YENİ PosKasaGocu)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs             (DbSet + indeks + append-only koruma)
dotnet/Kuyumcu.Api/Data/FaturaHareketBagi.cs            (TASLAK işareti + IsaretliHareketler)
dotnet/Kuyumcu.Api/Common/FaturaNoUretici.cs            (null numara sayaca girmez)
dotnet/Kuyumcu.Api/Modules/Fatura/FaturaSeriKapisi.cs   (YENİ — tek tüketim noktası)
dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs       (3 yol + kesim + TASLAK PATCH'leri)
dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs         (seri kapısına bağlandı)
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisDurumMakinesi.cs (K-23 tablo + GeriYonlu)
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs     (GeriAlmaRolleri + kart alanları)
dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs         (PATCH ad/not)
dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs             (KasaGoc + KasaGocleri + 409 mesajı)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs   (PosKasaGocu damgası)
dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs         (nullable fatura no)
dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs     (nullable fatura no)
dotnet/Kuyumcu.Api/Data/Migrations/20260921073615_P22TaslakFaturaSeriVePosKasaGocu.cs (+ .Designer.cs)
```
**Sözleşme / sözlük**
```
dotnet/Kuyumcu.Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs
dotnet/Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                   (TR+EN: 21 yeni anahtar)
```
**Ekran**
```
dotnet/Kuyumcu.Web/Pages/Faturalar.razor
dotnet/Kuyumcu.Web/Pages/Sayim.razor
dotnet/Kuyumcu.Web/Pages/KasaRaporu.razor
dotnet/Kuyumcu.Web/Pages/Pos.razor
dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor
```
**Test / denetim / ölçüm**
```
dotnet/Kuyumcu.Tests/P22SunucuTestleri.cs   (YENİ)
dotnet/Kuyumcu.Tests/P22HttpTestleri.cs     (YENİ)
dotnet/Kuyumcu.Tests/P22EkranTestleri.cs    (YENİ)
dotnet/Kuyumcu.Tests/RaporFaturaTestleri.cs · FaturaFistenTestleri.cs · FaturaOzelMatrahTestleri.cs
dotnet/Kuyumcu.Tests/MutabakatTestleri.cs · SiparisDurumMakinesiTestleri.cs
dotnet/Kuyumcu.Tests/S3fSunucuTestleri.cs · S3fEkranTestleri.cs · UX1C2EkranTestleri.cs
dotnet/denetim.sh                            (P-22 bloğu + BEKLENEN_VARLIK 61→62)
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json   (BEKLENEN_VARLIK 61→62)
dokumanlar/09-uygulama-kayitlari/PAKET-P-22-uygulama-raporu.md (bu dosya)
```
`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve `SONRAKI-OTURUM-PROMPT.md`'ye **dokunulmadı**. Proje ağacına `.mutorj/.bak/.tmp` **bırakılmadı** (mutasyon yedekleri scratchpad'de).

---

## § 12 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-3** — TASLAK fatura ve kalemleri düzenlenebilsin | **KAPANDI** | `Modules/Fatura/FaturaSeriKapisi.cs` · `FaturaModulu.TaslakGuncelle` / `TaslakKalemGuncelle` · testler `B3_*` (6) + `P22HttpTestleri.B3_*` + bUnit `B3_taslak_satirinda_Duzenle_dugmesi_PATCH_atar` · canlı C1-C5 · sayaç `SERI_TUKETIM_NOKTASI=1` |
| **B-26** — /siparisler durum geçişi onaysız ve geri alınamaz | **KAPANDI** | Onay kipi S-3f'ten korundu; geri alma K-23 ile açıldı → `SiparisDurumMakinesi` tablo · `SiparisServisi.GeriAlmaRolleri` · `P22HttpTestleri.K23_*` (4) · canlı C6-C9 |
| **B-33** — /sayim DURUM sütununda düz metin "İptal Et" | **KAPANDI** | `Sayim.razor` `DurumEtiketi()` + `id="sy-durum-{id}"` · bUnit `B33_sayim_DURUM_sutunu_gercek_durumu_yazar` · sayaç `DURUM_SUTUNU_EYLEM_METNI=0` |
| **B-44** — Sayım oturumunun adı düzeltilemiyor | **KAPANDI** | `PATCH /sayim/{id}` (`SayimModulu.Guncelle`) · `SatirIciDuzenle` · testler `B44_*` (3) + HTTP + bUnit · canlı C10-C12 |
| **B-45** — Gün sonu kasa sayımı hiçbir yerden girilemiyor | **KAPANDI** | `KasaRaporu.razor` "Sayımı gir" → `POST /kasalar/{id}/gun-kapanisi` · bUnit `B45_*` (2) · canlı C13-C15 (`/raporlar` kasa SAYILAN NAKİT 1500, FARK 1500) |
| **B-46** — POS "Bağlı Kasa" düzeltme yolu yok | **KAPANDI** | Tahsilatsız yol S-3f'ten korundu ve canlı doğrulandı (C17); tahsilatlı yol B-55 ile açıldı |
| **B-55** — POS tahsilat varken "yeni kasaya taşı" göç akışı | **KAPANDI** | `PosServisi.KasaGoc` + `PosKasaGocu` varlığı + migration · `POST/GET …/kasa-goc(leri)` · Pos.razor Cekmece · testler `B55_*` (3) + HTTP · canlı C20-C22 |
| **B-56** — POS kasa değişiminin tahsilatlı reddi 5198'de canlı denenmedi | **KAPANDI** | **5198'de CANLI denendi** → C19: `409` ve mesaj göç ucunu gösteriyor · `P22HttpTestleri.B56_*` · `S3fSunucuTestleri` mesaj kilidi güncellendi |

Kapatılamayan kimlik **yok**; devredilen kimlik **yok**.

---

## § 13 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Her iş kuralı için hatayı yeniden üreten test | ✅ | § 5 (27 yeni test) |
| Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | ✅ | § 6 (7 mutasyon, hepsi kırmızı, hepsi `cmp` birebir) |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | ✅ | § 7 (22 adım) |
| `dotnet/denetim.sh` TEMİZ | ⚠ **HAYIR** | P-22 sayaçları yeşil; tek kalan ihlal **P-02/BK-39** (P-22 dışı, § 8) |
| Tam süit 0 başarısız | ✅ | 3 542 / 3 542 |
| Test kiracısı silindi | ✅ | KALAN_SATIR = 0 · tenants 8 → 8 |
| Uygulama raporu | ✅ | bu dosya |
| Sayaç eklendi (cırcır), tavan artırılmadı | ✅ | § 8 |
| Migration: build yeşilken, `--no-build` yok, boyutlar > 0 | ✅ | § 4 |
| Yeni istemci record `Pages` içinde açılmadı | ✅ | `Kuyumcu.Contracts/Sayfalar/` |
| PATCH: gönderilmeyen alan korunur | ✅ | `B3_PATCH_gonderilmeyen_alani_KORUR` · canlı C2 |
| Finansal varlıkta hard DELETE yok | ✅ | § 3 |
| Sozlukler TR + EN | ✅ | 21 anahtar, iki dilde |
| COMMIT yok / git komutu yok / rm-mv yok | ✅ | çalışma ağacında kalındı |

---

## § 14 — AÇIK KALANLAR

1. **`denetim.sh` tam temiz değil** — `P-02: QA_TUR_SURESI_OLCUM (BK-39)`. P-22 dışı; `ham/qa-tur-suresi.json` içindeki `gecerli_ortalama_saat` hâlâ `null` ve devir turu 6 iken tur 7 koştu. **Orkestratöre eskale ediliyor**: ya gerçek tur süresi ölçülüp dosyaya yazılmalı ya da devir kararı kurulca yenilenmelidir. Uydurma değer yazılmadı.
2. **Fatura kalemi EKLEME/SİLME** taslakta hâlâ yok (yalnız değer düzenlemesi). Kapsam büyütmemek için açılmadı; ihtiyaç doğarsa ayrı bir kimlikle triyaja girmeli (Y-42 kuralı).
3. **KUYUM taslağında başlık toplamı** düzenlenemiyor (has bedelinin dayanağı yeniden hesaplanamaz). Doğru yol ekranda yazılı: taslağı iptal edip yeniden kes — artık numara yakmıyor.
4. **Atölye siparişinde `HAZIR → ONAYLANDI`** açılmadı (iki adım geri iki geçişle yazılır). Karar gerekiyorsa K-23'ün ikinci diliminde ele alınmalı.

---

## § 15 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **Fatura numarası artık taslakta tüketilmiyor.** Bu, VUK açısından **daha doğru** bir davranıştır (belge numarası ancak belge düzenlendiğinde tüketilir), ama **mevcut veride** eski taslakların numaraları duruyor — göç yapılmadı, eski satırlar olduğu gibi korundu. İlk gerçek kullanımdan önce mali müşavire "seri, kesim anında tüketiliyor" bilgisinin teyit ettirilmesi tavsiye edilir.
2. **K-23 kararı ürün içinde uygulandı:** sipariş durumu geri alınabilir, ama yalnız SAHİP/MÜDÜR, gerekçeyle ve iz silinmeden. Bu bir **tasarım** kararıdır; sektörde yasak olmadığı KARARLAR-URUN-2026-09-21 § K-23'te kanıtlanmıştır.
3. **K-5 (sır rotasyonu) hatırlatması:** MySQL kök parolası `docker-compose.yml` içinde **düz metin** duruyor (dosyanın kendi 3. satırındaki uyarı da bunu söylüyor). Bu paket ona dokunmadı; rotasyon hâlâ kurucu kalemidir.
