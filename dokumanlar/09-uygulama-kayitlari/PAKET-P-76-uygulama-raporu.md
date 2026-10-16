# PAKET P-76 — Yedekler: RPO aşımının kırmızı gösterilmesi + insanca süre + uyarı bandı — UYGULAMA RAPORU

> Şerit: **SERİT E — YEDEK DOĞRULAMA + RPO + BİÇİM tr-TR** · izole test vt `kuyumcu_test_e` · canlı port **5184**
> Rol: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-24 04:20–04:45 (yerel)
> Kaynak şartname: `dokumanlar/09-uygulama-kayitlari/TRIYAJ-TUR8-2026-09-23.md` (P-76, YÜKSEK, 2,0 saat tahmin)
> Sıra: P-77 (bitti) → **P-76** → P-57
> Durum beyanı: **BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı — § 12)

## 1. ÖZET

RPO kartı taahhüt aşılsa da nötr (amber, `Renk=""`) renk ve ham "7985 dk" gösteriyordu. Artık:

- **Üç bantlı renk:** ölçülen ≤ taahhüt → `yesil`; taahhüt < ölçülen ≤ 2×taahhüt → `sari`; > 2×taahhüt → `kirmizi`.
  Nötr/boş renk kaldırıldı. Kural tek yerde: `YedekRpoSozlugu.Renk(olculen, taahhut)`.
- **Ölçüm yok (hiç başarılı yedek yok, `rpoDakikaOlculen=null`):** kart `sari` (uyarı), değer "ölçülmedi",
  altında `role="alert"` ayrı uyarı (`#yd-rpo-olculmedi`): "Hiç başarılı yedek alınmadı; bir arızada ne kadar veri
  kaybedileceği bilinmiyor." Bilinmeyen yeşil gösterilmez (Anayasa md.1).
- **Aşım uyarısı:** aşım varsa kart ızgarasının hemen altında `role="alert"` (`#yd-rpo-uyari`):
  "Veri kaybı riski: son başarılı yedek **5 gün 13 saat önce** alındı. Söz verilen en uzun aralık **6 saat**; bu süre
  **22,2 kat** aşıldı. Bir arıza olursa bu aralıkta girilen kayıtlar kaybolabilir."
- **İnsan okunur süre:** yeni `Bicim.Sure(dakika, gün, saat, dk)` (7985 → "5 gün 13 saat", 95 → "1 saat 35 dk",
  360 → "6 saat"). Kart değeri, alt satırdaki RPO/RTO taahhütleri ve ölçülen RTO bu biçimde.
- **Ham dakika ipucunda:** kartın `title` özniteliği: "Dakika olarak — Son başarılı yedekten bu yana: 7985 dk ·
  RPO taahhüdü: 360 dk · RTO taahhüdü: 120 dk · …".
- Metinler TR+EN ayrı dosyada: `Kuyumcu.Core/I18n/Sozlukler.YedekRpo.cs` (HUB `Sozlukler.cs`'ye dokunulmadı).

Sunucu (YedekModulu.cs) DEĞİŞMEDİ: gereken her değer (`rpoDakikaOlculen`, `rpoDakikaTaahhut`, `rto…`) zaten
`GET /yedekler/ozet` yanıtındaydı; eşik/bant hesabı yeni bir iş kuralı değil, sunucunun verdiği iki sayının oranıdır.

## 2. ŞARTNAMENİN KODA KARŞI DOĞRULANMASI

| İddia (şartname) | Ölçüm | Sonuç |
|---|---|---|
| "Yedekler.razor:58-61 `Renk=@(RpoIcinde ? 'yesil' : '')`" | Satır 58-61'de birebir bu ifade vardı (P-77 sonrası) | **Doğru** |
| "doğrulama kartı 'kirmizi' alıyor, RPO kartı boş alıyor" | Doğrulama kartı `Dogrulandi ? "yesil" : "kirmizi"`; RPO `""` | **Doğru**. Not: `""` app.css'te `.stat-kart` varsayılanı = `--uyari-vurgu` (amber) — "nötr" değil "hep amber"; yine de aşımda kırmızıya dönmüyordu |
| "ham 'dk'" | `RpoOlculenMetni => $"{d} dk"` | **Doğru** |
| Dosya ayak izi (`test -e`) | Yedekler.razor (618 sat.), Bicim.cs (107), Sozlukler.cs (9312 — HUB, dokunulmadı), YedekEkranTestleri.cs (441) | **Hepsi var** |
| "7985 dk / 360 dk" (tur 8) | Canlıda bugün (01:36 UTC) `rpoDakikaOlculen=381` — tur 8'den sonra yeni yedek alınmış | Kural değerden bağımsız; 7985 bUnit'te birebir sınandı, 381 canlı değeriyle ayrıca sınandı (§ 10) |

## 3. SAPMALAR VE GEREKÇELERİ

- **S1 — RPO kartı `StatKart` yerine aynı sınıflarla sayfa içinde yazıldı.** Ortak `StatKart` bileşeni ipucu (`title`)
  parametresi taşımıyor; "ham dakika ipucunda kalır" koşulu için ya bileşene parametre eklemek ya da kartı sayfada
  yazmak gerekiyordu. Şeridin dosya sınırı ortak bileşeni kapsamıyor → kart birebir aynı DOM/sınıflarla
  (`kart stat-kart <renk>` · `kart-govde` · `stat-etiket/stat-deger/stat-alt`) `Yedekler.razor` içinde yazıldı ve
  `id="yd-rpo-kart"`, `id="yd-rpo-deger"` aldı. Mevcut id'lerin hiçbiri değişmedi. Görsel sonuç aynı.
  → Orkestratöre not: `StatKart`'a isteğe bağlı `Ipucu` parametresi eklenirse bu kart yeniden `StatKart`'a döner.
- **S2 — `sari` sınıfı:** app.css'te `.stat-kart.sari` kuralı yok; `.stat-kart` varsayılanı zaten `--uyari-vurgu`
  (amber/sarı) olduğundan `sari` sınıfı doğru rengi alır. app.css ortak dosya → değiştirilmedi, yeni sabit renk yok.
- **S3 — "Son yedeğin ne zaman alındığı":** özet yanıtı son BAŞARILI yedeğin zamanını ayrı alan olarak vermiyor
  (`sonYedek` son DENEMEDİR, başarısız olabilir). RPO ölçümü sunucuda tam olarak "son başarılı yedekten bu yana geçen
  dakika"dır; uyarı bunu "… **N önce** alındı" diye yazar. Böylece sunucuya alan eklemeden doğru bilgi verildi;
  YedekModulu.cs'e dokunulmadı.
- **S4 — Metin kaynağı:** şartname satırı `Sozlukler.cs`'yi (HUB) sayıyor; orkestratör koşulu gereği ayrı
  `Sozlukler.YedekRpo.cs` açıldı (P-77'nin `Sozlukler.YedekDogrulama.cs` deseni).
- **S5 — Bant kuralının yeri:** `Renk`/`Asildi` saf fonksiyonları sözlük dosyasındaki statik sınıfta (Core) durur —
  ekran tek satırla çağırır. Eşik (taahhüt) istemciye gömülmedi, sunucudan (`Yedek:RpoDakika`) gelir.
- **S6 — Mevcut test `Yedekler_SonYedekBoyutVeDogrulamaDurumunuGosterir`** `"95 dk"`, `"360 dk"`, `"120 dk"`'yı
  `Markup` içinde arar; ham değerler artık `title` özniteliğinde durduğu için test DEĞİŞTİRİLMEDEN yeşil kaldı.
  Fikstürdeki sabit `95/360` alanları testten ayarlanabilir alanlara (`rpoOlculen`, `rpoTaahhut`) çevrildi
  (varsayılanlar aynı: 95/360).

Mevzuat: bu paket mevzuat hükmü içermez (RPO iç hizmet taahhüdüdür, yapılandırmadan okunur).

## 4. MİGRATION

Gerekmedi (şema değişikliği yok).

## 5. DoD SÖZLEŞMESİ

Yeni varlık/kayıt türü **yok**. RPO kartı salt okunur bir ÖLÇÜMDÜR: değeri sunucu son başarılı yedekten hesaplar;
kullanıcı düzeltemez, düzeltme yolu yeni yedek almaktır — bu yol aynı ekranda mevcuttur (elle yedek talebi, P-56/T-3;
doğrulama talebi, P-77). Taahhüt (`Yedek:RpoDakika`) yapılandırma değeridir. Yeni düğme eklenmedi (her düğme
kuralı etkilenmedi). Yeni form yok. ModalPencere kullanımı artmadı (denetim sayaçları ✓).

**Dokunulan sayfalar:** `Pages/Yedekler.razor` (şerit zinciri P-56 → P-77 → **P-76** — aynı şeridin sıralı paketi;
kapı kaydı gerekçesi: triyaj "Dosya çakışma haritası" satır 147).

## 6. DEĞİŞİKLİKLER

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Web/Pages/Yedekler.razor` | RPO kartı üç bantlı renk + `title` ipucu + `id`'ler; `#yd-rpo-uyari` / `#yd-rpo-olculmedi` (`role="alert"`); `RpoRengi`, `Sure`, `Ham`, `RpoHamIpucu`, `RpoAsimMetni`; `RpoOlculenMetni`/`RpoRtoAlt` insan okunur |
| `dotnet/Kuyumcu.Web/Servisler/Bicim.cs` | **Yalnız ekleme:** `Sure(int, string, string, string)` (26 satır eklendi, 0 satır silindi — mevcut imzalar aynı) |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.YedekRpo.cs` | **YENİ** — `YedekRpoMetinleri` (TR+EN, konumsal record) + `YedekRpoSozlugu.Al/Renk/Asildi` |
| `dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs` | fikstür RPO alanları ayarlanabilir; 7 yeni test |

## 7. TESTLER

Yeni testler (7; hepsi `YedekEkranTestleri`, bUnit + saf):

| Test | Sınadığı |
|---|---|
| `Yedekler_RpoIkiKattanFazlaAsim_KartKirmiziOlur` | 7985/360 → `kirmizi`; sınır 721 (2×+1) → `kirmizi` |
| `Yedekler_RpoHafifAsim_KartSariOlur` | 500 → `sari`; tam 720 (2×) → `sari` |
| `Yedekler_RpoTaahhutIcinde_KartYesilOlur` | 360 (= taahhüt) → `yesil`, uyarı yok |
| `Yedekler_RpoAsimi_RoleAlertUyarisiGosterir` | `#yd-rpo-uyari` `role=alert`, "22,2 kat", "5 gün 13 saat önce", "6 saat"; 540 → "1,5 kat" |
| `Yedekler_RpoOlculmedi_YesilGosterilmez` | null → `sari` (yeşil değil), "ölçülmedi", ayrı `role=alert` uyarı |
| `Yedekler_RpoSuresi_InsanOkunurBicimde` | değer tam "5 gün 13 saat", görünür metinde "7985" yok, `title`'da "7985 dk"/"360 dk"; `Bicim.Sure` sınırları (42, 95, 360, 1440, −5) |
| `Yedekler_RpoCanliOlcum381_SariVeUyari` | canlı ölçümün birebir değeri (381/360) → `sari`, "6 saat 21 dk", "1,1 kat" |

Koşumlar (`KUYUMCU_TEST_DB=kuyumcu_test_e`):

| Kapsam | Toplam | Geçti | Başarısız |
|---|---|---|---|
| `YedekEkranTestleri` + `YedekMenuTestleri` (son) | 19 | 19 | 0 |
| Yedekler'i çizen diğer sınıflar (`YedekTatbikatTazelik`, `P13EkranTestleri`) + Yedek ekran/menü (ilk) | 34 | 34 | 0 |
| **Bicim'i kullanan 16 test sınıfının TAMAMI** (Cari/Fatura/P16/P17/P25Olcek/RaporlarEkran/S23PesinBandiHttp/S1cUstBar/S31PanelSade/S25YazdirParaUstu/S23FisMusteri/S34RaporBugun/S3aSessizVeriKaybi/UX1C3/UX4dSaf/YedekEkran) | 263 | 263 | 0 |
| Tam süit #1 (7. test eklenmeden, 04:34) | 4873 | 4872 | 1 |
| Tam süit #2 (son, 04:38) | 4874 | 4871 | 3 |

Tam süit #1'deki tek kırmızı `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`
(bUnit `WaitForFailedException` — zaman aşımı); tek başına 3 kez koşuldu: **3/3 yeşil (6/6)** → yük altında
kararsızlık; S-2b şeridinin sınıfı, bu paketin dokunduğu hiçbir koda bağlı değil. Tam süit #2'de yeşildi.

Tam süit #2'deki 3 kırmızı (hepsi **SERİT D**'nin o sırada yazılmakta olan `SatisOdemeKirilimi` işinden):
- `SozlesmeYansimaTestleri.Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor` — `Kuyumcu.Api.Modules.Rapor.SatisOdemeKirilimi+Satir` ikinci tanım
- `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` — beklenen 59, gerçek 60
- `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` — yeni kilitsiz tip `SatisOdemeKirilimiYaniti`

Kaynak dosyalar: `Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs`, `Kuyumcu.Web/Pages/Raporlar.razor`,
`Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` — SERİT D'nin dosya sınırı; bu şerit DOKUNMAZ.
Tam süit #1'de (aynı P-76 kodu) bu üç test yeşildi → P-76'nın ürünü değildir. trx:
`scratchpad/trx-pP-76/_Mac_2026-09-24_04_34_13.trx` (4873/4872/1) · `_Mac_2026-09-24_04_38_53.trx` (4874/4871/3).

## 8. MUTASYON KANITLARI

Yöntem: kaynaklar `scratchpad/mut-yedek/P-76/`'ya kopyalandı → `perl -0777 -pi -e` tek eşleşme (md5 değişimi
denetlendi) → `dotnet build Kuyumcu.Tests` (0 hata) → `dotnet test --filter YedekEkranTestleri -v n` → yedek geri
kopyalandı → `cmp` birebir.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M76-1 | razor: `class="kart stat-kart @RpoRengi"` → eski `@(RpoIcinde ? "yesil" : "")` | **Yedekler_RpoIkiKattanFazlaAsim_KartKirmiziOlur**, Yedekler_RpoHafifAsim_KartSariOlur, Yedekler_RpoOlculmedi_YesilGosterilmez (17'de 3 kırmızı) | cmp birebir |
| M76-2 | razor: `#yd-rpo-uyari`'dan `role="alert"` kaldırıldı | **Yedekler_RpoAsimi_RoleAlertUyarisiGosterir** (17'de 1) | cmp birebir |
| M76-3 | sözlük: `null => "sari"` → `null => "yesil"` | **Yedekler_RpoOlculmedi_YesilGosterilmez** (17'de 1) | cmp birebir |
| M76-4 | Bicim.Sure: ilk satırdan sonra `return $"{d} {dakikaBirim}";` (ham dakika) | **Yedekler_RpoSuresi_InsanOkunurBicimde**, Yedekler_RpoAsimi_RoleAlertUyarisiGosterir (17'de 2) | cmp birebir |
| M76-5 (ek) | sözlük: `d <= 2L * taahhut` → `d < 2L * taahhut` (sınır) | **Yedekler_RpoHafifAsim_KartSariOlur** (17'de 1) | cmp birebir |
| M76-6 (ek) | razor: kat oranı ters (`taahhut / d`) | **Yedekler_RpoAsimi_RoleAlertUyarisiGosterir** (17'de 1) | cmp birebir |

6/6 mutasyon kırmızı; test açığı yok. Son mutasyondan sonra üç kaynak `cmp` ile yedekle aynı ve
`dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata** (04:33). 7. test eklendikten sonraki son derleme → 0 Hata, 1 Uyarı
(`EtiketEkranTestleri.cs(145,47) xUnit1031` — SERİT D'nin dosyası, bu paketin değil).

## 9. DENETİM (`dotnet/denetim.sh`)

- Koşum #1 (P-76 kodu tam, 04:35): **"DENETİM TEMİZ."** (çıkış 0).
- Koşum #2 (04:44, SERİT D'nin yarım işi ağaca düştükten sonra): çıkış 1, tek ihlal
  `✗ P-03: CONTRACTS_DISI_MODEL (ikinci tanım) 1` — `SatisOdemeKirilimi+Satir` (SERİT D). P-76'nın sayaçlarının
  hepsi ✓ (ModalPencere, satır içi style, sabit renk, sözlük sayaçları dahil).

## 10. CANLI DOĞRULAMA (port 5184, `kuyumcu_net`, kiracı `PAKETP-76-TEST Kuyumculuk` = `01M38GW389TSXZZBVFYSTBAVTK`)

Başlangıç `COUNT(*) tenants` = **10** (`QA-TEST-%` = 4). Kiracı: `kiraci_davetleri`'ne tek kullanımlık davet
(SHA-256 jeton özeti) → `POST /auth/kayit`. Jeton/şifre/yanıt dosyaları iş sonunda `printf ''` ile boşaltıldı.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | `POST /api/v1/auth/kayit` | 201 | **201**, `tenantId=01M38GW389TSXZZBVFYSTBAVTK`, accessToken var |
| 2 | `GET /api/v1/yedekler/ozet` (SAHİP) | 200, RPO alanları | **200** `"rpoDakikaOlculen":381,"rpoDakikaTaahhut":360,"rtoDakikaTaahhut":120,"rtoDakikaOlculen":1`, `sonYedek.zaman=2026-09-23T19:14:27`, `durum=BASARILI` |
| 3 | SQL çapraz kontrol | ölçüm = son BAŞARILI yedekten bu yana dakika | `SELECT baslangic,durum,TIMESTAMPDIFF(MINUTE,baslangic,UTC_TIMESTAMP()) FROM yedek_kayitlari WHERE durum='BASARILI' ORDER BY baslangic DESC LIMIT 1` → `2026-09-23 19:14:27 · BASARILI · 381` (UTC_TIMESTAMP 2026-09-24 01:36:01) — **birebir** |
| 4 | Ekran kuralı canlı değerde | 360 < 381 ≤ 720 → `sari`, "6 saat 21 dk", "1,1 kat" uyarısı | Canlı yanıtın RPO değerleriyle birebir bUnit testi `Yedekler_RpoCanliOlcum381_SariVeUyari` → **yeşil** |

Not: tur 8'deki 7985 dk bugün yok (arada yeni yedek alınmış); 7985/360 durumu bUnit'te birebir sınandı (kırmızı + 22,2 kat).
Tarayıcıda oturum açıp ekranı görmek (parola girişi) bu ajanın yapmadığı bir adımdır; ekran davranışı bUnit ile, veri
kaynağı canlı API + SQL ile kanıtlandı.

**Temizlik:** API (`dotnet run` PID 59929, dinleyen 59931) `kill` → `lsof -nP -iTCP:5184 -sTCP:LISTEN` **boş**.
`tenant_id` sütunu olan **70 tablodan** test kiracısının satırları silindi (kalan satır toplamı **0**); davet ve
`tenants` satırı silindi. Son ölçüm: `COUNT(*) tenants` = **10** (başlangıç 10) · `PAKETP-76%` = 0 · davet = 0 ·
`QA-TEST-%` = 4 (dokunulmadı).

## 11. DEĞİŞEN DOSYALAR (`git status --porcelain | grep -v -E "/(bin|obj)/"` — bu paketin payı)

```
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs                 (önceki paketlerden de M; P-76: yalnız Sure eklendi)
?? dotnet/Kuyumcu.Web/Pages/Yedekler.razor               (izlenmeyen dosya; P-76 değişikliği)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.YedekRpo.cs        (YENİ)
   dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs            (izlenmeyen test dosyası; P-76 değişikliği)
```

## 12. KOŞUL KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| Ölçülen > 2×taahhüt → `kirmizi` | ✓ | Yedekler_RpoIkiKattanFazlaAsim_KartKirmiziOlur · M76-1 |
| taahhüt < ölçülen ≤ 2×taahhüt → `sari`; içinde `yesil` | ✓ | Yedekler_RpoHafifAsim_KartSariOlur, Yedekler_RpoTaahhutIcinde_KartYesilOlur · M76-5 |
| Aşımda `role=alert` uyarı, kaç kat aşıldığı DOM'da | ✓ | Yedekler_RpoAsimi_RoleAlertUyarisiGosterir · M76-2, M76-6 |
| Ölçülen null → yeşil değil, "ölçülmedi" | ✓ | Yedekler_RpoOlculmedi_YesilGosterilmez · M76-3 |
| 7985 dk → "5 gün …", ham dakika ipucunda | ✓ | Yedekler_RpoSuresi_InsanOkunurBicimde · M76-4 |
| Bicim.cs mevcut imzalar değişmez; Bicim'i kullanan testlerin hepsi koşulur | ✓ | diff: 26 ekleme / 0 silme; 16 sınıf 263/263 |
| HUB Sozlukler.cs'ye dokunulmaz; TR+EN | ✓ | `Sozlukler.YedekRpo.cs` (konumsal record) |
| P-56 / P-77 davranışı ve testleri bozulmaz | ✓ | YedekEkranTestleri 19/19 (P-56, P-77 testleri dahil) |
| Kapı (1) build 0 hata | ✓ | § 8 |
| Kapı (2) kendi testlerim 0 başarısız | ✓ | § 7 |
| Kapı (3) kendi sayaçlarım ✓ | ✓ | § 9 (koşum #1 TEMİZ; #2'deki tek ✗ SERİT D) |
| Kapı (4) mutasyon kanıtı | ✓ | § 8 (6/6 kırmızı, cmp birebir) |
| Kapı (5) canlı doğrulama | ✓ | § 10 |
| Kapı (6) test kiracısı silindi | ✓ | tenants 10 → 10 |
| Kapı (7) yeni kırmızı yok | ✓ | tam süit #1: tek kırmızı kararsız S-2b testi (3/3 yeşil tek başına); #2: 3 kırmızı SERİT D (`SatisOdemeKirilimi`), #1'de aynı P-76 koduyla yeşildi → P-76 farkı 0 |

## 13. AÇIK KALANLAR (sahibiyle)

- **SERİT D:** `SatisOdemeKirilimi` (Modules/Rapor/SatisRaporlari.cs + RaporlarSozlesmeleri.cs) — 3 kırmızı sözleşme
  testi + `denetim.sh` `P-03 CONTRACTS_DISI_MODEL` ihlali (§ 7, § 9). Yarım iş; şerit bitiminde kapanmalı.
- **S-2b:** `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` yük altında bUnit zaman aşımı
  (tek başına 3/3 yeşil) — kararsızlık.
- **SERİT D:** `EtiketEkranTestleri.cs(145,47)` xUnit1031 uyarısı (0 uyarı hedefi).
- **Orkestratör (öneri):** ortak `StatKart`'a isteğe bağlı `Ipucu` parametresi eklenirse RPO kartı yeniden ortak
  bileşene döner (S1). Bu şeridin yetkisinde değil.

## 14. KURUCUYA / MALİ MÜŞAVİRE NOTLAR

Mevzuat hükmü yok. Ürün notu: RPO taahhüdü (varsayılan 360 dk = 6 saat) yapılandırmadan gelir (`Yedek:RpoDakika`);
ekrandaki bantlar bu değere göre hesaplanır.
