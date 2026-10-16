# PAKET T-3 — Yedek otomasyonu ve doğrulanmış geri yükleme (Faz A) — uygulama raporu

- **Tarih:** 2026-09-18 · **Dalga:** 3 · **Rol:** Taşkın (backend) + Ertuğ (frontend)
- **Durum:** BİTTİ (Faz A) · derleme 0 hata / 0 uyarı · `dotnet/denetim.sh` TEMİZ · tam süit 2427/2427
- **Kırmızı çizgi:** commit YOK — tüm değişiklikler çalışma ağacında.

---

## 1. Ne yapıldı

| Katman | Çıktı |
|---|---|
| Veri | `yedek_kayitlari` (sistem düzeyi), `yedek_talepleri` (kiracılı), `yedek_tatbikatlari` (sistem düzeyi) + migration `20260917204558_YedekOtomasyonu` |
| API | `Modules/Yedek/YedekModulu.cs` — DTO + `YedekPolitikasi` + `YedekServisi` + `YedekController` (`[RolGerekli("SAHIP")]` sınıf düzeyi) |
| Uçlar | `GET /yedekler/ozet` · `GET /yedekler?limit=50` · `GET /yedekler/tatbikatlar` · `GET /yedekler/talepler` · `POST /yedekler/talep` (201) · `PATCH /yedekler/talep/{id}` (iptal) |
| Ekran | `Pages/Yedekler.razor` (4 StatKart, koşu/tatbikat/talep tabloları, gerekçeli elle yedek, yıkıcı onay modalı) + `KenarCubugu` bağlantısı (yalnız SAHİP, yalnız profesyonel mod) |
| Sözlük | `SozlukYedek` TR **ve** EN (47 anahtar) + `Nav.Yedekler` + `RotaBasliklari["/yedekler"]` |
| Betik | `dotnet/yedek-ortak.sh` (YENİ kitaplık) + `dotnet/yedek-al-ve-dogrula.sh` (GENİŞLETİLDİ: `tam | elle | talep | tatbikat | hepsi`) |
| Yayın | `deploy/systemd/kuyumcu-yedek-{tam,talep,tatbikat}.{service,timer}` |
| Runbook | `docs/YEDEK-GERI-YUKLEME.md` |
| Kapı | `denetim.sh` T3-1…T3-6 sayaçları + 4 tavan güncellemesi (gerekçeli) |

**Fail-closed kapılar (dördü de CANLI kanıtlandı — § 7):**
1. CHECKSUM uyuşmazlığı → `dogrulama=UYUSMADI`, çıkış ≠ 0.
2. Anahtar yok / < 32 karakter / alan anahtarıyla aynı → yedek **hiç alınmaz**, dosya üretilmez.
3. Dış kopya alınamadı (hedef tanımsız dâhil) → koşu `BASARISIZ`.
4. Son 30 günde doğrulanmış yedek yok → saklama temizliği **hiçbir dosyayı silmez**.

---

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | `dotnet/betikler/yedek.sh` (YENİ dizin) | `dotnet/yedek-ortak.sh` + genişletilmiş `dotnet/yedek-al-ve-dogrula.sh` | Tamer Bey mührü (a) + MÜHÜR 6/K3: betik dizini `dotnet/` kökü, ikinci betik açılmaz. Canlı doğrulama komutları da bu yola uyarlandı. |
| S2 | `deploy/YEDEK-GERI-YUKLEME.md` | `docs/YEDEK-GERI-YUKLEME.md` | Tamer Bey mührü (b): runbook'lar yalnız `docs/`. |
| S3 | `Yedek:AnahtarDosyasi` (appsettings'te anahtar YOLU) | Alan kaldırıldı; anahtar **yalnız** `Yedek__SifrelemeAnahtari` ortam değişkeninden | T-1 zaten bu hattı kurmuş (`deploy/.env.ornek:39`, `UretimAcilisDenetimi:123-128`). İkinci anahtar yolu = ikinci sır yüzeyi (MÜHÜR 3 tek hat). |
| S4 | Şifreleme: `age` ya da `gpg` (K9 — paket başında karar) | **gpg simetrik AES-256**, parola `--passphrase-fd 3` | Ekip kararı: gpg her dağıtımın temel deposunda var, müşteri sunucusuna ikinci ikili dosya taşımıyor; `age` bu makinede kurulu değil ve kurulum yükü doğuruyordu. Kanıt: `file` çıktısı "PGP symmetric key encrypted data - AES with 256-bit key". |
| S5 | Dış kopya: Backblaze B2 + rclone | Hedef **yapılandırılabilir**: dizin/NAS ya da `rclone:uzak:kova` | K8: "hedef yapılandırmayla değiştirilebilir". `rclone` bu makinede yok; dizin hedefiyle uçtan uca kanıtlandı, rclone dalı koda yazıldı ama **koşturulmadı** (açık kalan A4). |
| S6 | Saklama temizliği her koşuda | `YEDEK_SAKLAMA_ETKIN=1` gerektirir; systemd birimi verir | K8: "yerel silme geliştirme makinesinde koşmaz". |
| S7 | `DisKopyaHatasi_KosuBASARISIZ` / `Saklama_...` testleri DB katmanında | `YedekPolitikasi` saf sınıfı + betik sözleşme testleri | Kuralın işletim tarafı kabuk betiğindedir; birim testi betiği koşturamaz (docker+gpg+MySQL bağımlılığı = kararsızlık). Kural metni C#'ta tek yerde, betik tarafı hem `denetim.sh` T3 sayaçları hem `Betik_FailClosed_Kapilari_Yerinde` testiyle korunuyor; **işletim davranışı canlı ölçümle** kanıtlandı (§ 7). |
| S8 | Tarayıcıda `5199/yedekler` doğrulaması | Yapılmadı | 5199 kurucunun inceleme sunucusu; göreve göre dokunulmaz. Ekran davranışı 7 bUnit testiyle ve 5198 üzerindeki gerçek API yanıtlarıyla doğrulandı. |
| S9 | Faz A'da artımlı yedek | YOK (gecelik 03:15 + 09/15/21:15 **TAM**) | MÜHÜR 6/K1. `log_bin=1`, `gtid_mode=OFF` **ölçüldü**; PITR 1.1'e. |

---

## 3. Migration

- **Ad:** `20260917204558_YedekOtomasyonu` (`.cs` 7.115 bayt, `.Designer.cs` 156.010 bayt — ikisi de > 0).
- `Up()` içeriği okundu: `CreateTable yedek_kayitlari / yedek_talepleri / yedek_tatbikatlari` + 4 `CreateIndex`.
- **kuyumcu_net'e uygulandı:** `dotnet ef database update` → `Applying migration '20260917204558_YedekOtomasyonu'. Done.`
- Doğrulama: `SHOW TABLES LIKE 'yedek%'` → 3 tablo · `SELECT COUNT(*) FROM __EFMigrationsHistory` → **42**.
- Kiracı kapsamı: `YedekTalebi : IKiraciVarlik` (jenerik `HasQueryFilter`); `YedekKaydi` ve `YedekTatbikati` `KuyumcuDbContext` beyaz listesinde **açık gerekçeyle** kiracısız (Tenants/KiraciDaveti kalıbı). `KiraciYazmaDenetcisi` kiracısız varlıkları zaten atlıyor (`KiraciYazmaDenetcisi.cs:68-73`), istisna eklenmedi.

---

## 4. Test sayıları

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Önce (paket öncesi taban — 2.427 − 34) | 2.393 | 2.393 | 0 |
| T-3 filtreli (`~Yedek`) | 36 | 36 | 0 |
| Tam süit (son koşum) | **2.427** | **2.427** | **0** |

**Yeni test sayısı: 34** — `[Fact]` sayımı: `YedekTestleri.cs` 9 · `YedekHttpTestleri.cs` 7 · `YedekKiraciTestleri.cs` 3 · `YedekEkranTestleri.cs` 7 (6 ekran + 1 menü) · `YedekPolitikaTestleri.cs` 8. (Filtreli koşumdaki 36 sayısı, `[Theory]` üretilen vaka varyasyonlarını da içerir.)

**Değiştirilen mevcut test:** `RotaBaslikTestleri.Yansima_TAM_36_rota_bulur` → `..._37_...` (yeni `/yedekler` rotası; sayı sabiti bilinçli olarak uygulayıcıyı haritaya satır eklemeye zorluyor).

### ⚠ Kararsızlık bulgusu (Anayasa md.1 — gizlenmedi)
Tam süidin 2. ve 3. koşumunda **her seferinde FARKLI tek bir POS ekran testi** kırmızı döndü:

| Koşum | Kırmızı test | Tek başına koşumu |
|---|---|---|
| 2 | `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR` | 2 kez koşuldu, 2 kez **YEŞİL** (10/10) |
| 3 | `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_...` | **YEŞİL** (35/35) |
| 4 | — | tam süit **2427/2427 yeşil**, çıkış 0 |

Her ikisi de `Islemler.razor`/POS hattındadır; T-3 bu dosyaların hiçbirine dokunmadı. Belirti (paralel koşumda dönüşümlü kırmızı, izole koşumda yeşil) **paylaşılan durumdan kaynaklanan kararsızlığa** işaret ediyor. Bulgu açık kalanlara yazıldı (A1); T-3 kapsamında düzeltilmedi çünkü kapsam dışı dosyalara izinsiz dokunmak gerekirdi.

---

## 5. Mutasyon tablosu

| Kod | Değişiklik | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| M-T3-1 | `KosuSonucu`'da dış kopya kontrolü `if (false)` | `YedekPolitikaTestleri.DisKopyaHatasi_KosuBASARISIZ` | cmp birebir |
| M-T3-2 | `SaklamaSilinebilir` → `true` | `Saklama_SonDogrulanmisYedekYoksa_HicbirKayitSilinmez` | cmp birebir |
| M-T3-3 | PATCH iptali → `db.YedekTalepleri.Remove(kayit)` | `YedekTestleri.Talep_IptalEdilince_HardDeleteOlmaz_DurumIPTAL` | cmp birebir |
| M-T3-4 | Sınıf düzeyi `[RolGerekli("SAHIP")]` kaldırıldı | `YedekHttpTestleri.Ozet_SahipOlmayan_403` | cmp birebir |
| M-T3-5 | 429 kapısı `if (false)` | `YedekHttpTestleri.TalepAc_IkinciKez_429` | cmp birebir |
| M-T3-6a | DTO `[MinLength(20)]` kaldırıldı | `YedekHttpTestleri.TalepAc_GerekceKisa_400` | cmp birebir |
| M-T3-6b | Servis `GerekceEnAz = 20 → 0` | `TalepAc_GerekceKisa_ServisteDeReddedilir` **+** `IptalGerekcesiKisa_Reddedilir` | cmp birebir |
| M-T3-7 | Talep listesine `.IgnoreQueryFilters()` | `YedekKiraciTestleri.BaskaKiracininTalebi_Listede_Gorunmez` | cmp birebir |
| M-T3-8 | Gerekçeli `denetim.YazmaKaydet(...)` satırı silindi | `YedekHttpTestleri.TalepAc_Gecerli_201_DenetimKaydiOlusur` | cmp birebir |
| M-T3-9 | Ekranda `Dogrulandi => true` (sabit yeşil) | `YedekEkranTestleri.DogrulanmamisYedek_KirmiziRozetVeUyariBandi` | cmp birebir |
| M-T3-10 | `GET /yedekler/{id}/indir` ucu eklendi | `YedekHttpTestleri.YedekDosyasiIndirmeUcu_YOK_404` | cmp birebir |
| M-T3-11 | API'ye `Process.Start` eklendi | `denetim.sh` T3-1 → `✗ 1 (beklenen 0)`, çıkış 1 | cmp birebir |
| M-T3-12 | Betikte şifreleme atlanıp düz `.sql` yazımı | `Betik_FailClosed_Kapilari_Yerinde` **+** `denetim.sh` T3-2 çıkış 1 | cmp birebir |
| M-T3-13 | Betikte dış kopya hata dalı `if false` | `Betik_FailClosed_Kapilari_Yerinde` | cmp birebir |
| M-T3-14a | Giriş betiğine ikinci `mysqldump` çağrısı | `Betik_TekDumpYolu_IkinciYedekBetigiYok` | cmp birebir |
| M-T3-14b | `kurulum.sh`'a ikiz dump yolu | `denetim.sh` T3-4 → `✗ 2 dosya (beklenen 1)` | cmp birebir |

**Kapanan test açığı:** M-T3-8'in ilk denemesi **hiçbir testi kırmadı** — otomatik yazma dinleyicisi zaten `EKLEME` izi üretiyordu ve test yalnız izin VARLIĞINI arıyordu. Test, izin **gerekçeyi taşıdığını** arayacak biçimde güçlendirildi (`Detay` içinde gerekçe metni) ve mutasyon tekrarlandı → kırmızı. Aynı biçimde M-T3-6a'yı yakalayabilmek için HTTP testine "reddin model doğrulama katmanından geldiği" (alan→mesaj zarfı) koşulu eklendi.

Son mutasyondan sonra TEMİZ derleme: `dotnet build Kuyumcu.sln --nologo` → **0 hata, 0 uyarı**.

---

## 6. denetim.sh

```
  ✓ T-3: API'de Process.Start                      0
  ✓ T-3: betikte şifresiz .sql yazımı              0
  ✓ T-3: betikte argv parolası                     0
  ✓ T-3: dump çağrı noktası                        1 dosya (yedek-ortak.sh)
  ✓ T-3: yedek indirme ucu (açılmadı)              0
  ✓ T-3: appsettings'te yedek anahtarı             0

DENETİM TEMİZ.   (çıkış kodu 0)
```

Gerekçeli tavan güncellemeleri (hepsi dosyada yorumla yazılı): `IKiraciVarlik` 45 → **46** (`YedekTalebi`; iki kiracısız varlık beyaz listede) · Pages record tavanı 122 → **129** · `ModalPencere` 46 → **47** (yıkıcı onay; `duzenlenenId` kalıbı **değişmedi**, hâlâ 1) · Pages dosya sayısı 36 → **37**.

---

## 7. Canlı doğrulama (ölçülen)

### 7.1 Betik (docker'lı yol, `kuyumcu_net`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `yedek-al-ve-dogrula.sh tam` | çıkış 0, `.enc/.gpg` dosya var, `*.sql` YOK | **çıkış 0**; `kuyumcu_net-20260917-234821.sql.gz.gpg` 3.760.268 bayt; dizinde `.sql` sayısı **0** |
| `file <dosya>` | düz SQL değil | `PGP symmetric key encrypted data - AES with 256-bit key salted & iterated - SHA256` |
| SQL: son koşu | TAM/BASARILI/sifreli=1/KOPYALANDI | `TAM  BASARILI  3793300  1  KOPYALANDI  54` |
| `... tatbikat` | çıkış 0, uyuşmayan 0, sonuç BASARILI | **çıkış 0** · `TABLO=54 UYUSAN=54 UYUSMAYAN=0` · `sema_dusuruldu=1` · `rto_dakika_olculen=1` |
| **FAIL-CLOSED 2:** anahtar tanımsız | çıkış ≠ 0, dosya üretilmez | **çıkış 1**, Türkçe hata, dizinde **0 dosya** |
| **FAIL-CLOSED 3:** dış kopya hedefi tanımsız | koşu BASARISIZ | **çıkış 1**; satır: `TAM BASARISIZ HATA "dış kopya alınamadı"` |
| **FAIL-CLOSED 4:** doğrulanmış yedek yokken saklama | hiçbir dosya silinmez | `HİÇBİR dosyayı silmedi (fail-closed)`; 40 günlük sahte dosya **duruyor** |
| Saklama (doğrulanmış yedek varken) | eski dosya silinir, üstveri kalır | `saklama: 1 dosya silindi (> 30 gün)`; `yedek_kayitlari` satır sayısı **korundu** (8) |
| **Mehmet kapanış kanıtı:** `ps aux` | parola görünmez | 7.201 satırlık örnekte (ölçüm kabuğu hariç) `mysqldump` **7**, `gpg` **7** görünürken DB parolası **0**, yedek anahtarı **0**, `--password=` **0** |

### 7.2 API (`http://localhost:5198`, test kiracısı `PAKETT-3-TEST`)

| İstek | Beklenen | Gerçek |
|---|---|---|
| `GET /api/v1/yedekler/ozet` (SAHIP) | 200, alanlar dolu | **200** · `sonYedek.durum=BASARILI` · `sifreli=true` · `disKopya=KOPYALANDI` · `rpoDakikaOlculen=12` · `rpoDakikaTaahhut=360` · `rtoDakikaTaahhut=120` · `rtoDakikaOlculen=1` |
| aynı uç (TEZGAHTAR) | 403 | **403** — `"Bu işlem için gerekli rol: SAHIP"` |
| `POST /yedekler/talep` (gerekçe 39 karakter) | 201 | **201** · `id=01M2RJW1E5KE6PHFPMXBMFY9ZD`, `durum=BEKLIYOR` |
| aynı talep tekrar | 429 | **429** — `"...bekleyen bir yedek talebi zaten var..."` |
| `POST /yedekler/talep` (gerekçe "kısa") | 400 | **400** · `alanlar.gerekce=["Gerekçe en az 20 karakter olmalıdır"]` |
| `GET /yedekler/abc/indir` | 404 | **404** (uç hiç açılmadı) |
| Betik `talep` modu (kuyruk) | talep ISLENDI + koşu id | **çıkış 0** · `ISLENDI  y202609172103053835` · yeni koşu `ELLE BASARILI KOPYALANDI` |
| `PATCH /yedekler/talep/{id}` | 200, durum IPTAL, satır silinmez | **200** · `durum=IPTAL` · talep adedi **2 → 2** (silinmedi) |
| `SELECT COUNT(*) denetim_kayitlari WHERE varlik_turu='YedekTalebi' AND eylem='EKLEME'` | açılan talep sayısıyla tutarlı | **5** = 2 talep × (otomatik iz + gerekçeli iz) + 1 iptal izi |

### 7.3 Kapanış
- API (PID 91845) durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. Kurucunun 5199 sunucusuna **dokunulmadı** (hâlâ çalışıyor, PID 35609).
- Test kiracısı `PAKETT-3-TEST` `tenant_id` kolonu olan tüm tablolardan silindi → **tenants 8 → 9 → 8** (başlangıç değerine döndü), kalan T-3 test satırı **0**.
- Tatbikat/geri-yükleme şeması kalıntısı: **0**. `QA-TEST-` önekli 3 kalıcı QA kiracısına **dokunulmadı** (sayı 3).

### 7.4 Ortam ölçümleri (varsayım değil, ölçüm)
- `log_bin=1`, `gtid_mode=OFF` → PITR **Faz A kapsamında değil** (K1), ölçüm kayda geçti.
- Şemada InnoDB olmayan tablo sayısı: **0** → `--single-transaction` tutarlı.
- Toplam şifreli yedek boyutu: ~3,79 MB (54 tablo, belge deposu boşken). Büyüme izlenmeli.

---

## 8. İki gerçek hata bulundu ve düzeltildi (md.1)

1. **Sahte YEŞİL tatbikat.** İlk tatbikat koşusu "DOĞRULANDI" dedi ama yalnız **1 tablo** karşılaştırmıştı: `docker exec -i` açık stdin'i yutuyor ve `parmak_izi()` döngüsünü besleyen tablo listesini ilk çağrıda tüketiyordu. Düzeltme: `my()` içinde `</dev/null` + tablo listesinin döngüden önce tümüyle okunması. Düzeltme sonrası karşılaştırma **54 tablo**.
2. **Bayat istatistikten sahte KIRMIZI.** "Dump'tan sonra yazıldı mı" ayrımı `information_schema.UPDATE_TIME` ile yapılıyor; MySQL 8'de bu değer `information_schema_stats_expiry=86400` ile önbelleklidir ve bayat değer değişken tabloyu "gerçek uyuşmazlık" gösteriyordu. Düzeltme: sorgu öncesi `SET SESSION information_schema_stats_expiry=0`.

**Değişken tablo ayrımı (tasarım notu).** Kaynak parmak izi dump'tan sonra, yaşayan veritabanından alınır; arada yazılan tablo doğal olarak uyuşmaz. Betik bunu **ölçerek** ayırır (UPDATE_TIME ≥ dump başlangıcı ⇒ "değişken", değilse **gerçek uyuşmazlık** ve koşu başarısız). Yön fail-closed'dur: yanlış YEŞİL üretmez, gerektiğinde yanlış KIRMIZI üretir.

---

## 9. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| `YedekTalebi` | **PATCH** `/yedekler/talep/{id}` (iptal; gönderilmeyen alan korunur, null siler) + ekranda "Talebi iptal et" düğmesi ve ortak yıkıcı onay modalı | Hard DELETE **yok** — satır kalır, durum `IPTAL`. `[RolGerekli("SAHIP")]` + `DenetimServisi.YazmaKaydet` (yeniden yazılmadı, çağrıldı). |
| `YedekKaydi` | **SALT_OKUNUR** (`GerekceAnahtarlari.Sistem`), ekranda `DegismezIpucu` | Koşu bir ÖLÇÜMDÜR; düzeltme yolu **yeni bir koşudur** (`POST /yedekler/talep`), satırın kendisi değil. |
| `YedekTatbikati` | **SALT_OKUNUR** (`GerekceAnahtarlari.Denetim`) | Geri yükleme provasının kanıtı; denetim kaydı gibi değişmez. Yeniden koşturma yolu aylık timer'dır. |

Finansal varlık **eklenmedi**; hard DELETE hiçbir yeni tabloda yok. Her düğme ya gerçek istek atar ya `disabled` + sebep taşır (`#yd-talep-kilit`, `#yd-iptal-onay`); işlenmiş talepte eylem yerine `DegismezIpucu` çizilir. Tek düzenleme kalıbı korundu: `ModalPencere` yalnız yıkıcı onay için, `duzenlenenId` kalıbı **açılmadı**.

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/Yedekler.razor` (YENİ) · `Layout/KenarCubugu.razor` (tek `NavLink` bloğu eklendi) · `Layout/RotaBasliklari.cs` (tek satır).

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Betik konumu tek (`dotnet/` kökü) | ✅ | `dotnet/yedek-ortak.sh`, `dotnet/yedek-al-ve-dogrula.sh`; `dotnet/betikler/` **açılmadı** |
| İkiz dump yolu doğmaz, denetim sayacıyla zorlanır | ✅ | `denetim.sh` T3-4 = 1 dosya; M-T3-14a/b kırmızı |
| systemd T-1 düzenine yaslanır (kuyumcu kullanıcısı, EnvironmentFile) | ✅ | `deploy/systemd/kuyumcu-yedek-*.service` — `User=kuyumcu`, `EnvironmentFile=/etc/kuyumcu/.env` |
| Yedek anahtarı alan anahtarından AYRI, appsettings'te yalnız yol/sayı | ✅ | `anahtar_dogrula()` eşitliği reddeder; `denetim.sh` T3-6 = 0 |
| Dış kopya kimliği appsettings'e yazılmaz | ✅ | rclone kimliği ortam değişkeni (`RCLONE_CONFIG_*`), runbook § 2 |
| Parola argv'de geçmez, `ps aux` ile kanıtlı | ✅ | § 7.1 son satır (0 eşleşme / 7.201 satır) |
| Tatbikat raporu yalnız tablo adı taşır (test + mutasyon) | ✅ | `TatbikatRaporu_YalnizTabloAdiTasir_DegerYazilmaz`; canlı `rapor` alanı: `uyusmayan_tablolar= · degisken_tablolar=yedek_kayitlari` |
| Tatbikat HAM restore, `dotnet ef` kullanılmaz, şema düşürülür (test edilir) | ✅ | Betikte `coz | gunzip | mysql`; `sema_dusuruldu=1`; canlı şema kalıntısı 0 |
| Kiracısız muafiyet Tenants kalıbından, KiraciYazmaDenetcisi istisnası sayılır | ✅ | `KuyumcuDbContext` beyaz listesi; denetçiye **istisna eklenmedi** (kiracısız varlık zaten atlanıyor) |
| Ekranda "tüm kiracıları kapsayan sistem yedeği" uyarısı | ✅ | `#yd-sistem-uyari` + bUnit testi |
| Çapraz kiracı HTTP testi + test kiracısı silindi kanıtı | ✅ | `BaskaKiracininTalebi_PatchEdilemez_404`; tenants 8 → 8 |
| RPO ölçülen / RTO taahhüt ayrı alanlar, tatbikat raporunda dolu | ✅ | `rpoDakikaOlculen=12`, `rtoDakikaOlculen=1`, taahhütler ayrı; ekranda iki etiket |
| Faz A'da PITR/artımlı yok, log_bin ölçüldü | ✅ | `log_bin=1`, `gtid_mode=OFF` raporda |
| "Rakipte yok bizde var" dili kullanılmadı | ✅ | Runbook ve ekran metinleri yalnız kendi kanıtımızı anlatır |
| Ücretsiz pilot: fiyat/teklif/tahsilat metni üretilmedi | ✅ | Bu pakette hiçbir fiyat metni yok |

---

## 11. Açık kalanlar

- **A1 — POS ekran testlerinde kararsızlık (kapsam dışı, T-3 dosyası değil):** tam süidin 2 koşumunda dönüşümlü olarak `KarmaOdemeEkranTestleri` ve `UX1C1EkranTestleri` içinden birer test kırmızı döndü, izole koşumda yeşil; 4. koşum 2427/2427 yeşil. Kök neden (paylaşılan statik durum / paralel bUnit) ayrı bir bakım kaleminde aranmalı.
- **A2 — KURUCU KALEMİ (T-3 kapanmadan tamamlanmalı, K9):** yedek şifreleme anahtarının **sunucu dışı** saklama yeri ve kayıp hâlinde kurtarma yolu. Runbook § 2'de yer ayrıldı; karar girilene kadar paket "üretime hazır" sayılmaz.
- **A3 — Üretim yayını MFA kapısına bağlı (MÜHÜR 3):** 45-API bitmeden gerçek üretim yayını yapılmaz; systemd timer'larının "üretimde çalışıyor" iddiası ancak üretim sunucusunda kanıtlanabilir (macOS'ta systemd yok).
- **A4 — rclone/B2 dalı koşturulmadı:** bu makinede `rclone` kurulu değil; dış kopya dizin hedefiyle kanıtlandı. İlk üretim kurulumunda B2 dalı bir kez uçtan uca koşturulmalı.
- **A5 — `OnFailure` bildirim kanalı seçilmedi** (e-posta/webhook): birim dosyalarında yorumlu satır olarak duruyor, T-4 kalemi.
- **A6 — Üretimde `Yedek__SifrelemeAnahtari` zorunlu kılınmadı:** `UretimAcilisDenetimi` bugün yalnız "tanımlıysa alan anahtarından farklı olmalı" diyor. Yedek otomasyonu artık var; anahtarın **varlığının** üretimde fail-closed zorunlu kılınması T-1 hattında tek elden yapılmalı (bu paket `Program.cs`/denetçiye dokunmadı).
- **A7 — Geliştirme veritabanında T-3 koşu geçmişi kaldı:** `yedek_kayitlari` 8 satır, `yedek_tatbikatlari` 2 satır (sistem düzeyi olduğu için kiracı silmesiyle gitmez). Dosyaları scratchpad'dedir; kurucu isterse temizlenebilir — silme kararı kurucunundur.

---

## 12. Kurucuya / mali müşavire notlar

1. **Anahtar kaybı = tüm yedeklerin kaybı.** Yedek şifreleme anahtarının sunucu dışında güvenli bir kopyası (kasa / parola yöneticisi) **şart**. Bu karar A2'de kurucu kalemi olarak bekliyor.
2. **"30 gün" bir YEDEK dönüşüm süresidir, kayıt saklama süresi değildir.** Defter/belge verisi VUK md. 253 (5 yıl) ve TTK md. 82 (10 yıl) boyunca üretim veritabanında durur. Satış/sözleşme metinlerinde "verinizi 30 gün saklıyoruz" **denmemeli**. Sürelerin şirketin özel durumuna uygunluğu **mali müşavire doğrulatılmalı** — kodda hiçbir yasal süre sabit yazılmadı, saklama günü yapılandırmadan gelir ve Math.Clamp ile sıkıştırılır.
3. **KVKK md. 12/1 karşılığı:** yedek kişisel veri taşır; şifreli olması, indirme ucunun bulunmaması ve erişimin denetim izine yazılması bu maddenin doğrudan karşılığıdır. Tatbikat raporuna veri değeri yazılmaz.
4. **e-Defter/e-Belge ikincil kopya (saklayıcı kuruluş) yükümlülüğü bu paketin kapsamı DIŞINDADIR ve DOĞRULANMALIDIR:** bizim yedeğimiz GİB'in ikincil kopya şartının yerine geçmez. 38-B / E-BELGE hattı açılırsa ayrı değerlendirilmeli.
5. **RPO 6 saat / RTO 2 saat şu an yalnız TAAHHÜT hedefidir**, ölçülen değerler ayrı alanlarda tutulur. Bu hedefler S-1 ticari paketine yazılacaksa sözleşme metni kurucu imzası ister.
