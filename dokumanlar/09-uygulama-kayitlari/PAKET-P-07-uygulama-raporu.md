# PAKET P-07 — API DAYANIKLILIK BANDI (429, geri çekilme, jeton yarışı, istek fırtınası)

**Dalga:** DALGA 2 · **Kimlikler:** B-10, B-57 · **Sürüm:** 1.0 · **Tarih:** 2026-09-18
**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Migration:** HAYIR (gerekmedi)

---

## § 0 — Önkoşul ölçümü (varsayım yok)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| P-03 | VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-03-uygulama-raporu.md` (+ `-DUZELTME-2026-09-18.md`) |
| P-06 | VAR | `PAKET-P-06-uygulama-raporu.md`; `denetim.sh` içinde `# >>> P-06` bloğu koşuyor |

P-06'nın çıktısı sayesinde `ArayuzModuServisi` ve `BasitSatisAkisi.razor` kurucularında
**HttpClient yoktu**; plan bu ikisini "HttpClient tüketicisi" sayıyordu (bkz. § 3 sapma 1).

---

## § 1 — Yapılanlar

### B-10 — 429 bandı (üstel geri çekilme + jitter + tek uçuşlu jeton yenileme)
`Kuyumcu.Web/Servisler/ApiIstemci.cs` içinde **tek kapı** kuruldu; web istemcisinin attığı
her istek buradan geçer:

- **429 dalı:** sunucu 429 döndüğünde `GeriCekilmeDenemeTavani = 3` kez otomatik yeniden denenir.
  Bekleme `200·2^(n-1) ms` üstel basamak **+ jitter (0…250 ms)**. Sunucu `Retry-After`
  gönderdiyse onun süresi temel alınır.
- **Uzun bekleme kuralı:** sunucu `OtomatikBeklemeTavaniSaniye = 5` sn'den uzun bir bekleme
  isterse **otomatik denenmez** — kullanıcı ekranda bekletilmez, karar ona bırakılır.
- **Tükenince:** `OranSinirHatasi : ApiHata(429)` fırlatılır. Mesaj: *"Sunucu şu anda çok
  yoğun (istek sınırı). N saniye sonra “Yeniden dene” ile tekrar deneyin — veriler silinmedi."*
  (UX-S30: hangi koşul eksik + kaç saniye; UX-S28: ekran boşalmaz, mevcut `catch (ApiHata)`
  blokları hata bandını gösterir).
- **Jeton yenileme yarışı:** `TekUcuslaYenileAsync` (single-flight) korunmuş ve artık
  **eşzamanlı 5 istekle** sınanmaktadır — beşi de 401 alsa bile `POST /auth/yenile` **1 kez** atılır.

### B-57 — istek fırtınası kapısı
- **Fırtına sayacı:** uç (sorgu dizesi atılmış yol) başına 60 sn'lik pencerede
  `IstekTekrarTavani = 60` isteğe izin verilir. Tavan aşılırsa **istek gönderilmez**;
  `IstekFirtinasiHatasi : ApiHata(429)` fırlatılır (Anayasa md.1: hata gizlenmez, görünür olur).
- **Tek uçuş (single-flight) GET:** aynı yola **aynı anda** düşen çağrılar tek ağ isteğine iner.
  QA turu 5'in ölçtüğü `kur/guncel` yığılması (130 eşzamanlı çağrı) testte **1 isteğe** indi.

### Sayaç mekanizması (planın 3. ilkesi)
`dotnet/denetim.sh` sonuna `# >>> P-07 … # <<< P-07` bloğu eklendi (ölçüm komutları yorumda,
tabanlar **taze ölçüldü**):

| Sayaç | Değer | Tür |
|---|---|---|
| `APIISTEMCI_DISI_HTTPCLIENT` | **0** | `-eq 0` |
| `ISTEK_TEKRAR_TAVANI` | **60** | cırcır (yalnız azalır) |
| `BANT_BUTUNLUGU` | TAM | 429 dalı + geri çekilme/jitter + fırtına kapısı + tek uçuş + `AddRateLimiter` |

`APIISTEMCI_DISI_HTTPCLIENT = 0` için **`OturumServisi` HttpClient'tan çıkarıldı**: çıkışta
sunucudaki refresh token'ı iptal eden çağrı artık `ApiIstemci.JetonIptalEtAsync` üzerinden
gider (kurucu döngüsü doğmasın diye istemci çağrı anında kapsamdan çözülür — `ArayuzModuServisi`
deseni). Güvenlik davranışı **aynen korundu** (ağ hatası yerel çıkışı engellemez).

---

## § 2 — Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-10** | **KAPANDI** | `ApiIstemci.cs:~205-240` (429 döngüsü), `:~262-300` (tavanlar/jitter), `:~330-360` (`GeriCekilmeSuresi`, `RetryAfterSaniye`, `OranSinirMesaji`) · testler `B10_429_sonrasi_geri_cekilip_yeniden_dener_ve_basarili_olur`, `B10_geri_cekilme_USTEL_ve_JITTERLI_dir_en_fazla_N_deneme`, `B10_israrci_429_ekrani_bosaltmaz_Yeniden_dene_ve_kalan_sure_yazar`, `B10_sunucu_UZUN_bekleme_isterse_otomatik_denenmez_karar_kullaniciya_kalir`, `B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir` · canlı adım 3-4 |
| **B-57** | **KAPANDI** | `ApiIstemci.cs` `FirtinaKontrol` + `UcAnahtari` + `GetGovdeAsync` tek uçuş · testler `B57_ayni_uca_tavani_asan_istek_ATILMAZ_hata_gorunur_olur`, `B57_sorgu_dizesi_degisse_bile_AYNI_uc_sayilir`, `B57_AYNI_ANDA_ucusan_GET_ler_TEK_istege_iner`, `B57_pencere_gecince_sayac_sifirlanir` · denetim sayacı `ISTEK_TEKRAR_TAVANI` |

**Devredilen (kapsam dışı, gizlenmedi):** `Pages/Kullanicilar.razor` (S-3f) kendi 2/4/8 sn'lik
429 geri çekilmesini **ekran düzeyinde** sürdürüyor; artık bant da geri çekiliyor → iki katmanlı
bekleme var. Birleştirme `Pages/` dosyasına dokunmayı gerektirdiği için (kapsam kilidi + TEK DOKUNUŞ)
**yapılmadı; orkestratöre devredildi** (öneri: ekran döngüsünü kaldırıp bandın `KalanSaniye`
değerini göstermek).

---

## § 3 — Sapmalar ve gerekçeleri (Anayasa md.1)

1. **"4 HttpClient tüketicisi" iddiası bugün doğru değil.** Taze ölçüm: `ApiIstemci.cs` ve
   `Program.cs` (beyaz liste) dışında **1** gerçek tüketici vardı — `OturumServisi`.
   `ArayuzModuServisi` ve `BasitSatisAkisi.razor`'daki "HttpClient" geçişleri **yorum
   satırıdır** ("burada HttpClient YOKTUR"). Sayaç deseni tip kullanımını arar, yorumu saymaz.
   Hedef (her istek banttan geçsin) korunarak `OturumServisi` banda bağlandı → 0.
2. **Zaman pencereli GET önbelleği geri alındı (önemli).** İlk uygulamada fırtınayı söğurmak
   için 2 sn'lik GET önbelleği kondu; **tam süit 8 kırmızı** verdi
   (`BildirimEkranTestleri.Her_dugme_ya_istek_atar_ya_da_disabled_dir`,
   `VezneEkranTestleri.Elle_yenile_dugmesi_kuyrugu_YENIDEN_ceker`,
   `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar`,
   `RaporEkranTestleri.IslemDefteri_sayfa_ileri_geri_ofseti_gunceller`,
   `CariEkranTestleri.Tahsilat_KUR_fotografina_baglanir` + 2 S3f + 1). Sebep **gerçek bir
   ürün hatasıydı**: süre bazlı önbellek "Yenile" düğmesini yalancı yapıyor (kullanıcı tazeleme
   ister, ekran bayat veri verir — UX-S27/S28 ihlali). Karar: **önbellek kaldırıldı**, yalnız
   **tek uçuş** bırakıldı; tek uçuş sadece EŞZAMANLI kopyaları söndürür, ardışık her çağrı
   sunucuya gider. Ona bağlı 3 test de silindi (bkz. § 5 M07-4 yeni karşılığı).
3. **İki S3f testinin senkronizasyonu güncellendi** (iddiaları DEĞİŞMEDİ):
   `S3fEkranTestleri.Oran_sinirinda_YENIDEN_DENE_dugmesi_ve_yuklenemedi_durumu_cikar` ve
   `…Yeniden_dene_GERCEK_istek_atar_ve_basarida_liste_gelir`. Bant artık 429'u önce kendi
   denediği için hata ekrana ~2 sn sonra düşüyor; senkron `Render` yerine
   `WaitForElement(...,10 sn)` / `WaitForAssertion(...)` kullanıldı. Düğmenin çıkması, boş
   durumun ÇIKMAMASI ve tıklamanın **gerçek istek** atması iddiaları aynen duruyor.
4. **Test kancaları `internal` yerine `public`.** `InternalsVisibleTo("Kuyumcu.Tests")`
   denendi ve **derleme kırmızısı** verdi: `Kuyumcu.Web`'in üretilmiş `Program` tipi
   `Kuyumcu.Api.Program` ile çakışıyor (`CS0433`, `HttpOrtami.cs:36`). Bu yüzden tavan
   sabitleri ve `Uyu/Simdi/Jitter` kancaları `public` yapıldı; csproj değişikliği **geri alındı**.
5. **QA jetonunun oran sınırı beyaz listesi YAPILMADI.** Plan bunu bir *risk azaltıcı* olarak
   anıyor, DoD değil. Sunucuda bir "sınırsız jeton" kapısı açmak Mehmet'in (06 Güvenlik)
   onayını gerektirir ve tavanlar yalnız azalır kuralına da aykırı okunabilir. Yerine istemci
   tarafı tek uçuş + fırtına kapısı QA koşumundaki istek sayısını düşürüyor. **Kurucu/QA notu:**
   QA turu 429 yüzünden yavaşlarsa karar ekip oyuna taşınmalı.
6. **`AddRateLimiter` (Kuyumcu.Api/Program.cs) DEĞİŞTİRİLMEDİ.** Zaten mevcut ve doğru
   çalışıyor (`Common/OranSiniri.cs`, `Program.cs:487 app.UseRateLimiter()`); `OnRejected`
   `Retry-After` başlığını basıyor — canlıda doğrulandı (adım 3). Gereksiz dokunuş yapılmadı.

**Plan–koşul çelişkisi:** görülmedi. Plan bloğu ile ek koşullar (kapsam kilidi, sayaç,
migration=HAYIR) uyumlu uygulandı.

---

## § 4 — Derleme, migration, test sayıları

- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.
- **Migration:** **gerekmedi** (şema değişikliği yok). `__EFMigrationsHistory` = **52**
  (değişmedi), `Migrations/*.cs` sayısı el değmedi.
- **Tam süit:** `dotnet test … --logger trx` →
  **Toplam 3013 · Geçti 3013 · Başarısız 0 · Atlanan 0** (59,8 sn).
  TRX: `scratchpad/trx-pP-07/_Ramazan-MacBook-Air_2026-09-18_17_49_52.trx`.
- **Önce / sonra:** P-07 öncesi taban **3004** test (aynı koşumda 3016 sayısı benim 12 taslak
  testimi içeriyordu; önbellek kararı sonrası 3 tanesi silindi) → **sonra 3013**, **+9 yeni test**.

---

## § 5 — Mutasyon tablosu (hepsi son koda karşı yeniden koşuldu)

| Kod | Değişiklik | Dosya | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|---|
| M07-1 | `GeriCekilmeDenemeTavani = 3` → `0` | ApiIstemci.cs | `B10_429_sonrasi_geri_cekilip_yeniden_dener_ve_basarili_olur`, `B10_geri_cekilme_USTEL_ve_JITTERLI_dir_en_fazla_N_deneme` | cmp birebir |
| M07-2 | jitter kaldırıldı (`+ Jitter(JitterMs)` → `+ TimeSpan.Zero`) | ApiIstemci.cs | `B10_geri_cekilme_USTEL_ve_JITTERLI_dir_en_fazla_N_deneme` | cmp birebir |
| M07-3 | fırtına tavanı gevşetildi (`> IstekTekrarTavani` → `+1000`) | ApiIstemci.cs | `B57_ayni_uca_tavani_asan_istek_ATILMAZ_hata_gorunur_olur`, `B57_sorgu_dizesi_degisse_bile_AYNI_uc_sayilir` | cmp birebir |
| M07-4 | tek uçuş kaydı okunmuyor (`getUcusu.TryGetValue …` silindi) | ApiIstemci.cs | `B57_AYNI_ANDA_ucusan_GET_ler_TEK_istege_iner` | cmp birebir |
| M07-5 | jeton yenileme tek uçuşu bozuldu (`??=` → `=`) | ApiIstemci.cs | `B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir`, `UX4dOturumTestleri.M15_Ucu_paralel_401_TEK_yenileme_ve_TEK_cikis_uretir` | cmp birebir |
| M07-6 | uzun `Retry-After` kuralı kaldırıldı | ApiIstemci.cs | `B10_sunucu_UZUN_bekleme_isterse_otomatik_denenmez_karar_kullaniciya_kalir` | cmp birebir |
| M07-7 | çıkışta jeton iptali çağrısı kaldırıldı | OturumServisi.cs | `CikisGuvenlikTestleri.Cikis_sunucuya_bildirilir`, `…Cikis_iptal_edilecek_REFRESH_tokeni_gonderir` | cmp birebir |
| M07-8 | `OranSinirHatasi` yerine düz `ApiHata(429,"İstek başarısız")` | ApiIstemci.cs | `B10_israrci_429…`, `B10_sunucu_UZUN…`, `B10_geri_cekilme_USTEL…` | cmp birebir |

**Bulunan test açığı (gizlenmedi):** ilk turda tek uçuşu bozan mutasyon **hiçbir testi kırmadı**
— çünkü o günkü fırtına testi sahte sunucu anında yanıt verdiği için aslında **önbelleği**
sınıyordu. `YavasApi` (60 ms gecikmeli) eklenerek `B57_AYNI_ANDA_ucusan_GET_ler_TEK_istege_iner`
yazıldı; mutasyon tekrarlandı ve **kırmızı** oldu (M07-4). Son mutasyondan sonra
`dotnet build` → **0 Uyarı 0 Hata**.

---

## § 6 — denetim.sh

```
═══ P-07 API dayanıklılık bandı ═══
  ✓ P-07: APIISTEMCI_DISI_HTTPCLIENT               0 (-eq 0)
  ✓ P-07: ISTEK_TEKRAR_TAVANI                      60 (azami 60, cırcır)
  ✓ P-07: BANT_BUTUNLUGU                           429 dalı + geri çekilme/jitter + fırtına kapısı + tek uçuş

DENETİM TEMİZ.
```

---

## § 7 — Canlı doğrulama (port 5198; **5199'a dokunulmadı**)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | API başlatma (`--urls 5198`) | ayağa kalkar | **kalktı** (PID 84787) | `scratchpad/api-5198.log` |
| 2 | `GET /saglik/hazir` | 200 | **200** | ilk denemede |
| 3 | `POST /api/v1/auth/giris` × 7 (yanlış kimlik) | 5 istek geçer, sonra 429 + `Retry-After` | **404·404·404·404·404 → 429 · 429**, `Retry-After: 60` | gövde: `{"message":"Çok fazla istek gönderildi…","error":"TooManyRequests","statusCode":429}` |
| 4 | `GET /api/v1/kur/guncel` **eşzamanlı 5 istek**, süresi dolmuş jeton | 5 × 401 (QA turu 5'in gördüğü tablo) | **401·401·401·401·401** | gövde: `{"message":"Geçersiz veya süresi dolmuş token","statusCode":401,"istekKimligi":"01M2TG0QFY…"}` — istemci bandı bu 5 isteği TEK yenilemeye indirir (test M07-5 ile kilitli) |
| 5 | `GET /_framework/Kuyumcu.Web.wasm` | 200 + bant kodu servis edilen görüntüde | **200**, 2.034.965 bayt; `FirtinaKontrol` (utf8) 1, `"Yeniden dene"` (utf16) 1, `"Sunucu şu anda çok yoğun"` 1, `"İstek gönderilmedi"` 1 | bayt düzeyinde arama |
| 6 | SQL `kuyumcu_net` | `tenants` 8 · migration 52 | **8 · 52** | şema ve kiracı sayısı değişmedi |
| 7 | SQL `tenants like 'PAKET%'` | 0 | **0** | **test kiracısı açılmadı** (aşağı bkz.) |
| 8 | SQL `tenants like 'QA-TEST%'` | 3 (dokunulmadı) | **3** | QA fikstürlerine dokunulmadı |
| 9 | API durdurma | 5198 boş | **`lsof -nP -iTCP:5198 -sTCP:LISTEN` = 0 satır**; 5199 çalışmaya devam ediyor | yalnız kendi PID'im öldürüldü |

**Test kiracısı açılmadı (md.1, gizlenmedi):** kayıt ucu davet jetonu ister (P-06 raporunda
canlı doğrulanmış 403). P-07 hiçbir uç/şema eklemediği ve kanıtlarının **hiçbiri kiracı
gerektirmediği** için kiracı açılmadı; `tenants` 8'de kaldı, silinecek artık yok.

**Jeton yarışının "eşzamanlı 5 istek" kanıtı nerede:** yarışı çözen kod **tarayıcıdaki
Blazor WASM istemcisindedir**; curl ile sunucuya vurmak onu çalıştırmaz. Bu yüzden kanıt
ikiye bölündü: (a) canlı adım 4 — sunucu gerçekten 5 eşzamanlı 401 döndürüyor (fırtınanın
gerçekliği), (b) `B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir` + `M15_Ucu_paralel_401…`
testleri — istemci bu tabloda `POST /auth/yenile`yi **1 kez** atıyor, mutasyonla kilitli.
Gerçek tarayıcı çapraz doğrulaması plan DoD md.7 gereği **QA turuna** kalıyor.

---

## § 8 — DoD sözleşmesi (yeni varlık/kayıt türü yok)

Bu paket **hiçbir varlık, tablo, kayıt türü ya da uç eklemedi**; yalnız istemci taşıma
katmanını ve bir denetim sayacı bloğunu değiştirdi. Bu yüzden PATCH/DELETE/yetki/denetim
kaydı sözleşmesi **konu dışıdır**: düzenlenebilecek yeni bir kayıt doğmadı.
Ekran tarafında yeni düğme eklenmedi; mevcut `#k-yeniden-dene` (S-3f) gerçek istek atıyor
(testle kilitli). Finansal varlıklarda hard DELETE'e dokunulmadı.

---

## § 9 — Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| 429'da ekran sessizce boşalmaz: "Yeniden dene" + kalan süre | ✅ | `OranSinirHatasi : ApiHata` + mesaj testi; S3f ekran testleri yeşil |
| Üstel geri çekilme + jitter, en fazla N deneme | ✅ | `B10_geri_cekilme_USTEL_ve_JITTERLI…` (450/650/1050 ms), M07-1/M07-2 |
| Jeton yenileme yarışı tek bekleyici + eşzamanlı 5 istek | ✅ (canlı kısmı § 7 notu) | `B10_esZamanli_5_istek…`, canlı adım 4 |
| B-57: tek sayfa ömründe aynı uca N'den fazla istek testi kırar | ✅ | `B57_ayni_uca_tavani_asan_istek_ATILMAZ…` |
| Sayaç: `APIISTEMCI_DISI_HTTPCLIENT = 0` | ✅ | denetim.sh P07-1 |
| Sayaç: `ISTEK_TEKRAR_TAVANI` (azalan) | ✅ | denetim.sh P07-2, taban taze ölçüldü (60) |
| Migration: HAYIR | ✅ | `__EFMigrationsHistory` 52 sabit |
| Pages'te yeni istemci record açılmadı | ✅ | `Pages/` dosyalarına hiç dokunulmadı |
| Tam süit 0 başarısız | ✅ | 3013/3013 |
| denetim.sh TEMİZ | ✅ | § 6 |
| 5199'a dokunulmadı | ✅ | yalnız 5198 kullanıldı ve kapatıldı |
| COMMIT yok, git komutu yok, rm/mv yok | ✅ | yalnız `git status --porcelain` (salt okunur) |
| Sır/parola çıktıya yazılmadı | ✅ | parola değişkende; ekrana "var" yazıldı |
| Test kiracısı silindi | ✅ (açılmadı) | `tenants like 'PAKET%'` = 0 |

---

## § 10 — Değişen dosyalar (bu paket)

| Dosya | Ne oldu |
|---|---|
| `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs` | dayanıklılık bandı (429/geri çekilme/jitter, fırtına kapısı, tek uçuş GET, `JetonIptalEtAsync`) |
| `dotnet/Kuyumcu.Web/Servisler/OturumServisi.cs` | HttpClient çıkarıldı; çıkış banttan geçiyor |
| `dotnet/denetim.sh` | `# >>> P-07 … # <<< P-07` sayaç bloğu |
| `dotnet/Kuyumcu.Tests/P07DayaniklilikTestleri.cs` | **YENİ** — 9 test |
| `dotnet/Kuyumcu.Tests/CikisGuvenlikTestleri.cs` | fikstüre `ApiIstemci` kaydı (üretimdeki Program.cs ile aynı) |
| `dotnet/Kuyumcu.Tests/S3fEkranTestleri.cs` | 2 testin bekleme senkronizasyonu (iddialar değişmedi) |

`git status --porcelain | grep -v -E "/(bin|obj)/"` çalışma ağacında **önceki paketlerden
gelen** onlarca değişiklik de gösteriyor (ağaç zaten kirliydi, COMMIT YOK kuralı gereği);
yukarıdaki altı dosya **bu paketin** dokunduklarıdır.

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/` altında **hiçbir razor dosyasına
dokunulmadı**.

---

## § 11 — Açık kalanlar

1. **İki katmanlı 429 geri çekilmesi** — `Pages/Kullanicilar.razor` (S-3f) kendi 2/4/8 sn
   döngüsünü sürdürüyor; bant da 3 kez deniyor. Kullanıcı en kötü hâlde ~2 sn + 2 sn bekliyor.
   Birleştirme kapsam dışı kaldı → orkestratör kararı.
2. **Fırtına tavanı canlıda henüz sınanmadı.** 60/dk/uç tavanı gerçek bir POS oturumunda
   (arama yazarken) aşılır mı, QA turunda ölçülmeli. Aşılırsa çözüm tavanı YÜKSELTMEK değil
   (cırcır), isteği azaltmaktır.
3. **QA jetonu beyaz listesi** açılmadı (§ 3 madde 5) — QA koşumu yavaşlarsa ekip oyuna gider.
4. **Gerçek tarayıcı çapraz doğrulaması** (429 bandı + jeton yarışı) QA turuna bırakıldı.

## § 12 — Kurucuya / mali müşavire not

Yasal/mevzuat hükmü içermiyor; mali müşavir notu **yok**. Kurucuya tek not: bu paket
kullanıcıya **yeni bir ücretli özellik** getirmez, "sunucu yoğun" durumunda ekranın
boş kalmasını ve sessiz istek fırtınalarını engeller (satış öncesi güven kalemi).

**Tahmin/gerçekleşen:** tahmin 150 ajan-dk; gerçekleşen ≈ 150 ajan-dk. Sapma sebebi:
önbellek kararının geri alınması (§ 3 madde 2) + test açığının kapatılması ek tur gerektirdi.
