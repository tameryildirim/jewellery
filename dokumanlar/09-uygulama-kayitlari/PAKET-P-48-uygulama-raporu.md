# PAKET P-48 — İşlem defteri YÖN türetimi (has gramı ve sıfır TL dâhil) — uygulama raporu

- **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · Dalga PARA VE SAYI DOĞRULUĞU (tur 8 triyajı)
- **Durum:** BİTTİ (paralel şerit kapısının 7 maddesi sağlandı; tam süitteki 3 kırmızının hiçbiri bu paketin değil — § 6)
- **Gerçek süre:** 21:09 → ~21:30 ≈ **21 dakika** (triyajın tahmini 1,5 saat idi; tahmin DEĞİL, saat damgası ölçümü)
- **Migration:** gerekmedi (şema değişmedi)

## 1. Spesifikasyon ↔ kod doğrulaması

| İddia (triyaj) | Ölçüm | Sonuç |
|---|---|---|
| `SatisRaporlari.cs:107` `yon = h.TlTutari >= 0m ? GIRIS : CIKIS` | satır **108**'de aynen vardı | DOĞRU (1 satır kaymış) |
| `HasGrami`/`Maden` aynı select'te var ama yöne girmiyor | doğru | DOĞRU |
| Dosya ayak izi 4 dosya | `test -e` ile 4'ü de VAR | DOĞRU |
| `Kuyumcu.Web/Servisler/Dil.cs` yok | bu paket o dosyayı kullanmadı | — |
| Ekran `IslemDefteri.razor:171` `s.Yon == "GIRIS" ? Giris : Cikis` | aynen vardı — null/KARISIK'ı da "Çıkış" basardı | DOĞRU |

**Ayak izine ek (sapma, gerekçeli):** `Kuyumcu.Web/Servisler/RaporServisi.cs` — istemci DTO'su `DefterSatiri` `string Yon` idi; null yönü taşıyabilmesi için `string? Yon` yapıldı ve sona **isteğe bağlı** `YonTl`, `YonHas` eklendi (varsayılanlı olduğundan mevcut sahte API fikstürleri kırılmaz). Yeni bUnit testi mevcut `RaporEkranTestleri.cs` yerine **yeni dosyada** (`P48IslemDefteriYonTestleri.cs`) — ortak test dosyasına dokunmamak için.

**Kod ölçümü (canlı veritabanı, kuyumcu_net, tüm kiracılar, `sign(tl), sign(has)` dağılımı):** SATIS (+,−) 60 · ALIS (−,+) 44 · HURDA_ALIS (−,+) 9 · SATIS_IADE (−,+) 4 · TAKAS (+,−) 3 · HURDA_SATIS (+,−) 1 → bu **121 satır artık "Karışık"** basar (iki bacak ayrı). EMANET_GIRIS (0,−) 6 · EMANET_CIKIS (0,−) 4 · TRANSFER (0,−) 2 eskiden **yanlışça "Giriş"** görünüyordu → artık "Çıkış". SAYIM_DUZELTME (0,+) 30 → "Giriş" (eskisiyle aynı, ama artık has'tan). (0,0) satır **yok**.

## 2. Yapılanlar

1. **Saf fonksiyon** `IslemYonu` (`Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs` sonu): `Isaret(decimal)` → GIRIS/CIKIS/null; `Turet(tl, has)`: iki bacak sıfırdan farklı ve zıt → **KARISIK**; değilse TL işareti; TL=0 ise has işareti; ikisi 0 → **null**. Sınıf başı yorumu: *"TÜR ADI YÖN KAYNAĞI DEĞİLDİR: EVRAK_GIRIS adlı satır TL'si -3.250,40 ise ÇIKIŞ'tır"* (kabul koşulu 2).
2. **Uç** `GET /raporlar/islem-defteri` satırı: `yon = IslemYonu.Turet(...)` + yeni `yonTl`, `yonHas` alanları (bacak yönleri). Uç, yetki, sayfalama, özet değişmedi.
3. **Ekran** `IslemDefteri.razor` yön hücresi: `YonMetni()` sözlükten çevirir (GIRIS→Giriş, CIKIS→Çıkış, KARISIK→Karışık, null→"—", tanınmayan kod→"Bilinmiyor"; ham kod ASLA basılmaz). KARISIK iken altında iki ayrı satır: `Para: Giriş` / `Has: Çıkış`. null iken `title` = "Tutar ve has gramı sıfır — yön yok". Yeni id'ler: `id-yon-{id}`, `id-yon-tl-{id}`, `id-yon-has-{id}` (mevcut id değişmedi). Satır içi style / sabit renk eklenmedi (mevcut `metin-kucuk`, `metin-soluk` sınıfları).
4. **Sözlük** (`Sozlukler.cs`, yalnız EKLEME, TR+EN): `RaporSeti.YonKarisik` (Karışık/Mixed), `YonYok` (—/—), `YonYokIpucu`, `YonTlBacagi` (Para/Money), `YonHasBacagi` (Has/Fine).
5. **Mevcut test güncellemesi (gerekçeli):** `RaporSetiTestleri.Islem_defteri_TL_isaretini_korur` satış (+5.000 TL, −1 g) için "GIRIS", alış (−3.000, +2 g) için "CIKIS" bekliyordu — bu tam da P-48'in düzelttiği tek-bacak varsayımıdır. Kabul koşulu 3 (TL>0 & Has<0 → Karışık) ile çeliştiği için beklenti `KARISIK` + `yonTl` olarak güncellendi; işaret koruma (M122) iddiaları aynen duruyor.

## 3. DoD sözleşmesi

Yeni varlık/kayıt türü **yok**; yeni yazma ucu yok. İşlem defteri türetilmiş rapordur — düzenleme yolu yoktur; gerekçe sayfada zaten yazılı (UX-S25 yorumu, P-34). Finansal satırlara dokunulmadı. Yeni düğme yok (DoD ek 1 kapsamı dışı).

## 4. Testler

| Test | Tür | Sonuç |
|---|---|---|
| `RaporSetiTestleri.P48_01_TL_sifir_has_negatif_satir_CIKIS_doner` | DB + saf | Geçti |
| `RaporSetiTestleri.P48_02_Zit_isaretli_satir_KARISIK_doner` | DB + saf | Geçti |
| `RaporSetiTestleri.P48_03_Iki_bacak_sifirsa_yon_null_doner` | DB + saf | Geçti |
| `P48IslemDefteriYonTestleri.P48_04_Ekran_ham_yon_kodunu_basmaz` | bUnit | Geçti |

Filtreli koşum (P48 + RaporSeti + RaporEkran + P37b + P17CiktiPano + Sozluk): **113/113**. Yeni test **4**. Tam süit: önce (P-47 raporu) 4262 → şimdi **4361** (aradaki fark diğer şeritlerin testleri dâhil).

> Ortam notu: ilk filtreli koşumda 20 DB testi `Can't create database 'kuyumcu_test'; database exists` ile düştü — paralel şeritlerle **aynı test veritabanı** yarışı. Bu paket bundan sonra `KUYUMCU_TEST_DB=kuyumcu_test_p48` ile koştu (TestOrtami.cs'nin desteklediği değişken); iş sonunda o veritabanı `DROP` edildi. Bir kez de `NETSDK1177 apphost is already signed` derleme hatası alındı (eşzamanlı derleme); tekrar derleme 0 hata.

## 5. Mutasyon kanıtları (yedek `scratchpad/mut-yedek/P-48/`, `perl -0777 -pi` tek eşleşme, geri kopya + `cmp`)

| Kod | Değişiklik | Kırmızı dönen test(ler) | Geri alındı |
|---|---|---|---|
| M48-1 | Select'te `yon = IslemYonu.Turet(...)` → eski `h.TlTutari >= 0m ? "GIRIS" : "CIKIS"` | P48_01, P48_02, P48_03 (P48_04 yeşil — beklenen) | `cmp` birebir |
| M48-1b | `IslemYonu.Turet` gövdesi → `return tlTutari >= 0m ? Giris : Cikis;` | P48_01, P48_02, P48_03 | `cmp` birebir |
| M48-2 | Razor hücresinde `@YonMetni(s.Yon)` → `@s.Yon` (ham kod) | P48_04 (DB testleri yeşil — beklenen) | `cmp` birebir |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata** (2 uyarı MSB3026 — `Kuyumcu.Tests.dll` başka şeridin koşan test süreci tarafından kilitliydi, kopya yeniden denemesiyle geçti; kod uyarısı değil).

## 6. Tam süit + denetim

`dotnet test … --logger trx --results-directory …/trx-pP-48` → trx `Counters total="4361" passed="4358" failed="3"`.

| Kırmızı | Kök neden | Sahibi |
|---|---|---|
| `S1eGirisTestleri.Tuval_IKI_PANEL_sol_kart_ve_sag_panel` | beklenen "MİHENK’e hoş geldin", gelen "Hoş geldin" (Giriş sözlüğü) | S-1 şeridi / S1-e (yarım iz) |
| `S1eGirisTestleri.Sozluk_IKI_DILDE_dolu_ve_farkli` | "Hoş geldin" içinde "MİHENK" yok | S-1 şeridi / S1-e |
| `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` | CSS'te `position: relative; overflow: hidden; min…` | S-1 şeridi / S1-c |

Hiçbiri İşlem defteri / RaporSeti sözlüğü / SatisRaporlari'nı sınamıyor. **P-48'in ürettiği yeni kırmızı = 0.** P-47 devir notundaki 19 kırmızı bu koşumda yok (kapanmışlar).

`bash dotnet/denetim.sh` → exit 0, **`DENETİM TEMİZ.`** (✗ satırı 0).

## 7. Canlı doğrulama (localhost:5198 · kuyumcu_net; 5199'a dokunulmadı)

Test kiracısı `PAKET48-TEST Kuyumculuk` (`01M37RCDHZAXF9JEAWHTHJJ5HK`), tek kullanımlık davet satırı SQL ile eklenerek açıldı. Hareketler meşru yoldan (`POST /cari/{id}/duzeltme` → HareketFabrikasi.Ekle; `HasGrami = -hasDelta`) üretildi.

| # | İstek | Beklenen | Gerçek | Yanıt (okundu) |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | tenantId + accessToken |
| 2 | `POST /cari` (MUSTERI) | 201 | **201** | cari id |
| 3 | `GET /kur/guncel` | id | var | — |
| 4 | duzeltme tl 0, hasDelta 5,496 | 200 | **200** | cari gövdesi |
| 5 | duzeltme tl −3.250,40, has 0 | 200 | **200** | cari gövdesi |
| 6 | duzeltme tl 1.500, hasDelta 2,5 | 200 | **200** | cari gövdesi |
| 7 | duzeltme tl 0, has 0 | 400 | **400** | "Düzeltme için en az bir fark girilmelidir" — (0,0) hareket meşru yoldan ÜRETİLEMEZ |
| 8 | `GET /raporlar/islem-defteri?limit=50` | 200 | **200** | aşağıda |

Yanıt satırları (okundu):
```
CARI_DUZELTME 1500.0  -2.5    yon= KARISIK yonTl= GIRIS yonHas= CIKIS
CARI_DUZELTME -3250.4  0.0    yon= CIKIS   yonTl= CIKIS yonHas= None
CARI_DUZELTME 0.0     -5.496  yon= CIKIS   yonTl= None  yonHas= CIKIS
```
SQL kanıtı (`select tur, tl_tutari, has_grami from hareketler where tenant_id=…`): `0.00 / -5.496000` · `-3250.40 / 0.000000` · `1500.00 / -2.500000` — uçtaki işaretlerle birebir.

(0,0) → null durumu canlıda üretilemediği (adım 7) ve canlı veritabanında da (0,0) satır olmadığı için yalnız P48_03 (DB) + P48_04 (ekran "—") ile kanıtlıdır. Ekran tarayıcıda açılmadı (giriş formuna parola yazmak ajan için yasak); ekran davranışı bUnit P48_04 ile canlı yanıtla aynı şekilde (`yon`/`yonTl`/`yonHas`) sınandı.

**Temizlik:** API PID durduruldu, `lsof -iTCP:5198` boş. `tenant_id` kolonu olan **70 tablodan** test kiracısı satırları silindi (kalan 0, tek tek sayıldı), `tenants` satırı ve davet satırı silindi. **tenants: 11 → 11** (başlangıç değeri). `QA-TEST-` kiracılarına dokunulmadı. `kuyumcu_test_p48` veritabanı DROP edildi.

## 8. Değişen dosyalar (`git status --porcelain | grep -v /(bin|obj)/` ile teyit)

- `dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs` (yon satırı + `IslemYonu` sınıfı)
- `dotnet/Kuyumcu.Web/Pages/IslemDefteri.razor` (yön hücresi + `YonMetni`)
- `dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs` (`DefterSatiri.Yon` nullable + `YonTl`/`YonHas`)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (5 anahtar × sınıf/TR/EN, yalnız ekleme)
- `dotnet/Kuyumcu.Tests/RaporSetiTestleri.cs` (P48_01..03 + mevcut testte gerekçeli beklenti güncellemesi)
- `dotnet/Kuyumcu.Tests/P48IslemDefteriYonTestleri.cs` (YENİ, P48_04)

**Dokunulan sayfalar (DoD ek 2):** `Pages/IslemDefteri.razor` — sıradaki sahipler P-58 → P-63 → P-62 (çakışma haritası sırası P-48 ilk).

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| TlTutari=0, HasGrami=−5,496 satırı "Çıkış" | KARŞILANDI | P48_01, P48_04 (h2), canlı satır 3 |
| TlTutari=−3.250,40 EVRAK_GIRIS "Çıkış" + tür adının yönü belirlemediği kod yorumunda | KARŞILANDI | P48_01, P48_04 (h3), `IslemYonu` sınıf yorumu + select yorumu, canlı satır 2 |
| TL>0 & Has<0 → "Karışık", iki bacak ayrı okunur | KARŞILANDI | P48_02, P48_04 (`id-yon-tl-h1` = "Para: Giriş", `id-yon-has-h1` = "Has: Çıkış"), canlı satır 1 |
| TL=0 & Has=0 → "—", "Giriş" yazmaz | KARŞILANDI | P48_03 (yon null), P48_04 (h4 "—", Giriş içermez, title gerekçe) |
| Tür adı yön kaynağı değil | KARŞILANDI | `Turet` imzası türü almıyor |
| dotnet test 0 başarısız, denetim.sh TEMİZ | Paket kapısı: KARŞILANDI · Ağaç kapısı: orkestratörde | P-48 yeni kırmızı 0; denetim TEMİZ; 3 kırmızı S-1 şeridinin (§ 6) |
| Mutasyon listesi | KARŞILANDI | § 5 (3/3 kırmızı, cmp birebir) |

## 10. Açık kalanlar / notlar

1. **UX notu (Ertuğ/kurucu kararı gerekebilir):** kural gereği satış/alış/hurda/iade/takas satırlarının tamamı artık "Karışık" başlığıyla + iki bacak satırıyla görünür (canlı veride 121 satır). Bu doğru ve tahminsizdir, ama sadeleştirme planı (MİHENK, "jargon sıfır") açısından tezgâhtara "Karışık" kelimesi yerine doğrudan "Para giriş · Has çıkış" göstermek daha sade olabilir — P-58/P-62/P-63 bu sayfayı açarken değerlendirilmeli. Kod tek noktada (`YonMetni` + sözlük) değişir.
2. **İşaret sözleşmesi gözlemi (ölçüldü, dokunulmadı):** canlı veride `TAHSILAT` satırları TL **negatif** (8 satır) → yeni kuralla da "Çıkış" görünür; tahsilatta kasaya para girer. `hareketler.tl_tutari` işaretinin cari perspektifli olduğu (VERESİYE sözleşmesi) anlaşılıyor; bu durumda "Yön" kasanın değil **carinin** yönüdür. Spesifikasyon işaretten türetmeyi emrediyor ve tahmin yasak olduğundan kod bunu değiştirmedi; sütun başlığının anlamı ("kasaya göre mi, cariye göre mi") orkestratör/Bertan kararı olarak açık bırakıldı.
3. **Başkasına ait kırmızılar (sahibiyle):** S1eGirisTestleri ×2 ve S1cUstBarTestleri ×1 → S-1 şeridi (S1-c/S1-e).
4. **Ortam:** paralel şeritler aynı `kuyumcu_test` veritabanını yarıştırıyor (bu paketin ilk koşumunda 20 sahte kırmızı). Öneri: her şerit `KUYUMCU_TEST_DB=kuyumcu_test_<şerit>` ile koşsun. Ayrıca `TestOrtami.cs`'de test MySQL parolası için sabit bir varsayılan değer kaynakta duruyor (değeri buraya yazılmadı) — Mehmet'in gündemine.
5. Mevzuat hükmü içermez (yön türetimi saf muhasebe gösterimi); web araştırması gerekmedi.
