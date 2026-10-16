# PAKET P-73 — Kur zamanlayıcı aralığı gerçekçi varsayılana çekildi (10 → 60 sn)

> Uygulayıcı: Taşkın (backend) · Şerit B (Yedek + Kur + Denetim izi) · 2026-09-24 00:00–00:15 (UTC)
> Kaynak: QA tur 8 triyajı (`TRIYAJ-TUR8-2026-09-23.md` satır 109) · ciddiyet ORTA
> Durum beyanı: **BİTTİ** (paket kapısının 7 maddesi aşağıda kanıtlı; tam süitte başkasına ait kararsız kırmızılar "Açık kalanlar"da)

## 1. Spesifikasyon — koda karşı doğrulama (önce ölçüm)

| İddia | Ölçüm | Sonuç |
|---|---|---|
| `KurModulu.cs:1103` varsayılan 10, 5–3600 kenetli | `grep -n` | DOĞRU — `Math.Clamp(yapilandirma.GetValue("Kur:GuncellemeSaniye", 10), 5, 3600)` |
| appsettings.json:15 ve Production:38 = 10 | `grep -n` | DOĞRU; ayrıca **Development.json:22 = 10** ve iki `.ornek` şablonu (Development:57, Production:38) = 10 — spesifikasyonda yoktu, eklendi |
| "Değişmedi → yazma" koruması devreye giriyor mu (DOĞRULANMAMIŞTI) | `kuyumcu_net`: kiracı başına saatlik `kur_snapshots` (23.09 12:00 sonrası) | **Çoğunlukla GİRMİYOR**: 10 sn aralıkta kiracı başına saatte **332–340** satır (azami 360 tur). Koruma turların ~%6'sında devreye giriyor. |
| 6648 kaydın zamanlayıcıdan geldiği (DOĞRULANMAMIŞTI) | `select kaynak,count(*) from kur_snapshots group by kaynak` | Toplam 160 466 satır: ALTINKAYNAK 157 243 · ANKARA 2 720 · TRUNCGIL 487 · HAREM 2 · **MANUAL 14**. MANUAL dışındaki 160 452 satır otomatik yoldan (zamanlayıcı ya da `POST /kur/guncelle`) gelmiştir; ikisi ayrı damga taşımadığı için kesin ayrım yapılamaz. |
| HTTP testleri zamanlayıcıyı çalıştırıyor mu | `HttpOrtami.cs:85` | Hayır — `RemoveAll<IHostedService>()`. Açılış hatası HTTP fikstürünü etkilemez. |

Sapma: yok (hedefe birebir). Ek kapsam: `.ornek` şablonları; `YayinDosyaSozlesmesiTestleri.ProductionSablonuVeOrnegiBirebirAynidir` Production şablonu ile örneğinin birebir aynı olmasını şart koşuyor. İlk tam süit bu yüzden kırmızıydı (bu paketin ürünü), `.ornek` güncellenince yeşile döndü.

Mevzuat: bu paketin mevzuatla ilgisi yok (kur çekim sıklığı teknik bir ayar).

## 2. Yapılanlar

`Modules/Kur/KurModulu.cs` — `KurZamanlayici`:
- `public const int VarsayilanAralikSaniye = 60`, `AltSinirSaniye = 15`, `UstSinirSaniye = 3600`, `AralikAnahtari = "Kur:GuncellemeSaniye"`.
- `public static TimeSpan AraligiOku(IConfiguration)`:
  - anahtar **yok** → 60 sn (belgelenmiş varsayılan);
  - anahtar var ama tamsayı değil (`abc`, `60sn`, `60.5`, boş) → `InvalidOperationException` ("… ayrıştırılamadı …"); varsayılana düşülmez;
  - değer 15–3600 **dışında** → `InvalidOperationException` ("… = 5 sn kabul edilmez; geçerli aralık 15–3600 sn … sessizce sıkıştırılmaz …").
- Alan `GuncellemeAraligi = AraligiOku(yapilandirma)`: kurucuda çağrılıyor, yani hatalı değerde barındırılan hizmet başlarken **uygulama açılmıyor**. `Math.Clamp` kaldırıldı.
- `EtkinAralik` salt okunur özelliği eklendi (test ve açılış logu için).
- Açılış logu (Information): `Kur zamanlayıcı başladı (etkin aralık: {Saniye} sn, pencere: {Bas}-{Bit})`.
- XML belge yorumu güncellendi: 60 sn, 15–3600, ölçülen 340 satır/saat gerekçesi.

Yapılandırma: `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, `appsettings.Development.json.ornek`, `appsettings.Production.json.ornek` → `"GuncellemeSaniye": 60`.

Kapsam dışı kalanlar:
- Ekran metni, Sozlukler.cs ve razor dosyalarına dokunulmadı (gerek yoktu).
- Migration yok: şema değişmedi.
- Yeni varlık yok, bu yüzden SÖZLEŞME-DoD (PATCH/DELETE) uygulanmaz.

## 3. DoD sözleşmesi
Yeni varlık, kayıt türü, uç ya da ekran **yok**. Değişen tek şey arka plan zamanlayıcısının yapılandırma okuması. Kur kayıtlarının düzenleme ve iptal yolu (S-3b, manuel kur iptali) aynen duruyor. Tek dokunulan sayfa: **yok**.

## 4. Testler (`Kuyumcu.Tests/S3eKurKaynagiHttpTestleri.cs` → yeni sınıf `P73KurZamanlayiciTestleri`, `[Collection("http")]`)

| Test | Ne sınar |
|---|---|
| `KurZamanlayici_VarsayilanAralik_AltmisSaniye` | Kod sabiti 60; anahtarsız yapılandırmada `AraligiOku` 60 sn; kurucudan `EtkinAralik` 60 sn. Taban + Development ve taban + Production katmanlarında etkin 60 sn. Yalnız tabanda 60 sn. İki `.ornek` şablonunda da 60. |
| `KurZamanlayici_AltSinirinAltindaDeger_AcilistaHataVerir` (Theory: 5, 14, 0, -10, 3601) | Kurucu `InvalidOperationException` atar; mesajda anahtar, değer, 15, 3600 ve "sessizce sıkıştırılmaz" geçer. |
| `KurZamanlayici_AltSinirinAltindaDeger_UygulamaAcilisiniDurdurur` | Gerçek host (`AddHostedService<KurZamanlayici>`, değer 5): `StartAsync` "= 5 sn" mesajıyla patlar. |
| `KurZamanlayici_OkunamayanDeger_AcilistaHataVerir_VarsayimYapmaz` (Theory: abc, 60sn, "", 60.5) | Okunamayan değerde hata verir, varsayılana düşmez. |
| `KurZamanlayici_SinirIcindekiDeger_AynenKullanilir` (Theory: 15, 60, " 120 ", 3600) | Sınır içindeki değer aynen kullanılır. |
| `KurZamanlayici_EtkinAralik_Loglanir` | Information seviyesinde "etkin aralık: 60 sn"; yapılandırılmış 90'da "etkin aralık: 90 sn". |
| `KurZamanlayici_BirSaatlikSimulasyon_KiraciBasinaEnFazla60Snapshot` | `ExecuteAsync` zaman çizelgesi (tur, sonra Delay), 1 saat, en kötü durum (her tur fiyat değişir), gerçek DB + `KurServisi.OtomatikGuncelle`. Tur sayısı = yazılan = 60 ≤ 60. |

Filtreli koşum (`KUYUMCU_TEST_DB=kuyumcu_test_q`, P73 + S3e + YayinDosya + Vade): **52/52 geçti**. P73 sınıfı tek başına: **17/17**.
Yeni test sayısı: **17** (7 metot; Theory'lerle 17 vaka).

## 5. Mutasyon kanıtları (yedek: `scratchpad/mut-yedek/P-73/`, her biri `cmp` ile birebir geri alındı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M73-1 | `VarsayilanAralikSaniye = 60` → `10` | `KurZamanlayici_VarsayilanAralik_AltmisSaniye`, `KurZamanlayici_EtkinAralik_Loglanir`, `KurZamanlayici_BirSaatlikSimulasyon_KiraciBasinaEnFazla60Snapshot` (3/17) | cmp AYNI |
| M73-2 | Kurucu, eski `Math.Clamp(GetValue(...), 15, 3600)` sessiz kenetlemesine döndü | `KurZamanlayici_AltSinirinAltindaDeger_AcilistaHataVerir` ×5 (5, 14, 0, -10, 3601), `KurZamanlayici_AltSinirinAltindaDeger_UygulamaAcilisiniDurdurur` (6/17) | cmp AYNI |
| M73-3 | Açılış logu `LogInformation` → `LogDebug` | `KurZamanlayici_EtkinAralik_Loglanir` | cmp AYNI |
| M73-4 | `appsettings.json` GuncellemeSaniye 60 → 10 | `KurZamanlayici_VarsayilanAralik_AltmisSaniye` | cmp AYNI |
| M73-5 | Okunamayan değerde varsayılana düşme (`if (!int.TryParse(ham, out _)) return 60`) | `KurZamanlayici_OkunamayanDeger_AcilistaHataVerir_VarsayimYapmaz` ×4 | cmp AYNI |
| M73-6 | `appsettings.Development.json.ornek` 60 → 10 | `KurZamanlayici_VarsayilanAralik_AltmisSaniye` | cmp AYNI |

Anayasa md.1 notu:
- **M73-5'in ilk koşumu yanlış yeşil verdi (17/17).** Aynı ağaçta paralel şeritler derleme yapıyordu ve test, bayat `Kuyumcu.Api.dll` ile koştu (o sırada 59 dotnet süreci açıktı). Mutasyon metni `diff` ile doğrulandı ve koşum yinelendi: 4 kırmızı.
- **M73-6'nın ilk koşumu, başka şeridin eşzamanlı Web derlemesi yüzünden derleme hatası verdi** (`MSB4018 GenerateWasmBootJson`, dosya kilidi). Yinelenince 1 kırmızı.
- Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

## 6. Derleme · tam süit · denetim
- `dotnet build Kuyumcu.sln --nologo`: **0 Uyarı 0 Hata**. İlk derlemede tek uyarı `MSB3026` idi (başka süreç test dll'ini kilitlemişti, yeniden denemede kopyalandı); CS uyarısı 0.
- Tam süit, koşum 1 (`trx-pP-73/_Mac_2026-09-24_02_56_56.trx`): 4778 test, 4775 geçti, 3 başarısız.
  - `YayinDosyaSozlesmesiTestleri.ProductionSablonuVeOrnegiBirebirAynidir`: **bu paketin ürünüydü**, `.ornek` güncellenerek düzeltildi.
  - `VadeEkranTestleri.VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron` ve `VadeEkranTestleri.VadeTakibi_HizliSecim_AriaPressedDogru`: bUnit `WaitFor` zaman aşımı. Filtreli yeniden koşumda yeşil.
- Tam süit, koşum 2 (`_Mac_2026-09-24_03_10_40.trx`, düzeltmeden sonra): **4813 test, 4812 geçti, 1 başarısız**. Toplamın artmasının sebebi, arada başka şeritlerin test eklemesi.
  - `EtiketEkranTestleri.TaslagiTemizleFormuBosaltir` ("taslak depodan silinmedi"): filtreli yeniden koşumda 40/40 yeşil.
- **Bu paketin ürettiği yeni kırmızı: 0.** Kalan kırmızıların hepsi bu paketin dokunmadığı ekranlarda (Vade, Etiket). Tek başına koşulunca yeşiller ve iki koşumda farklı testler kırmızı oldu; bu, paralel yük altındaki kararsızlığa işaret ediyor.
- `dotnet/denetim.sh`: **DENETİM TEMİZ** (rc=0).

## 7. Canlı doğrulama (port **5187**; talimattaki son izolasyon maddesi gereği 5198 yerine. 5190–5200 aralığına dokunulmadı)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `Kur__GuncellemeSaniye=5` ile açılış | Anlaşılır hata, uygulama durur | Süreç ~1 sn'de kapandı. Log: `Unhandled exception. System.InvalidOperationException: Yapılandırma hatası: 'Kur:GuncellemeSaniye' = 5 sn kabul edilmez; geçerli aralık 15–3600 sn (önerilen varsayılan 60). Değer sessizce sıkıştırılmaz …`. "Now listening" 0 kez geçti. |
| Varsayılan (Development) yapılandırmayla açılış logu | Etkin aralık 60 sn | `info: Kuyumcu.Api.Modules.Kur.KurZamanlayici[0]  Kur zamanlayıcı başladı (etkin aralık: 60 sn, pencere: 0-23)` |
| API ayakta | Yetkisiz istekte 401 | `GET /api/v1/kur/kaynaklar` → `HTTP 401` `{"message":"Token bulunamadı",...}` (yanıt okundu) |
| Tur temposu (Debug log + 2 sn yoklama) | ~60 sn | Tur #5 00:07:25 → tur #6 00:08:28 = **63 sn** (60 sn bekleme + ~3 sn tur süresi; bekleme turdan sonra başlıyor). 5 dakikada 6 tur. |
| Kiracı başına yazım (log `Kur güncellendi`) | ≤ tur sayısı | 6 turda 6 kiracıya 6'şar = 36 yazım. Saatte ≤ 60; 10 sn'de ölçülen 340'a karşı yaklaşık %83 azalma. |

SQL kanıtı ve dürüst uyarı:

```sql
select tenant_id, count(*) n from kur_snapshots
 where olusturuldu between '2026-09-24 00:03:20' and '2026-09-24 00:09:00'
 group by tenant_id order by n desc limit 5;
-- → her kiracı 33
```

Pencerede kiracı başına 33 satır var. Bunların 6'sı bu paketin 5187 örneğinden, kalan 27'si ise **5200 portunda koşan, bu paketin başlatmadığı bir API örneğinden** geliyor (PID 12577, `Kuyumcu.Api.dll --urls https://localhost:5200`, eski 10 sn yapılandırması). Bu yüzden DB seviyesindeki sayım tek başına kanıt değil; kanıt, 5187 örneğinin kendi logudur. 5200 örneğine dokunulmadı.

Temizlik:
- 5187'deki API (PID 36755) `kill` ile durduruldu. `lsof -iTCP:5187 -sTCP:LISTEN` boş, süreç yok. Hatalı yapılandırmalı deneme (PID 7513) zaten kendisi kapanmıştı.
- Test kiracısı **açılmadı**; doğrulama için giriş gerekmedi. `tenants` sayısı başta 10, sonda 10. `PAKET…P-73` adlı kiracı sayısı 0. QA-TEST- fikstürlerine dokunulmadı.

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Varsayılan yapılandırmayla etkin aralık 60 sn | KARŞILANDI | Test (anahtarsız + 3 appsettings + 2 ornek) + canlı log "etkin aralık: 60 sn" + ölçülen tur arası 63 sn |
| GuncellemeSaniye=5 ile açılışta anlaşılır hata, sessiz kenetleme yok | KARŞILANDI | Host testi + canlı açılışta Unhandled InvalidOperationException, dinleme yok |
| Etkin aralık açılış logunda | KARŞILANDI | `KurZamanlayici_EtkinAralik_Loglanir` + canlı `info:` satırı |
| 1 saatlik simülasyonda KurSnapshot ≤ 60 | KARŞILANDI | Simülasyon testinde 60 yazım (en kötü durum) |
| Okunamayan değerde varsayım yok | KARŞILANDI | `…OkunamayanDeger…` ×4 + M73-5 |
| Alt sınır 15 | KARŞILANDI | Theory 14 → hata, 15 → kabul |

## 9. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokunduğu)

```
 M dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs
 M dotnet/Kuyumcu.Api/appsettings.json
 M dotnet/Kuyumcu.Api/appsettings.Development.json
?? dotnet/Kuyumcu.Api/appsettings.Production.json          (izlenmeyen dosya; yalnız GuncellemeSaniye satırı)
?? dotnet/Kuyumcu.Api/appsettings.Production.json.ornek    (izlenmeyen; yalnız GuncellemeSaniye satırı)
?? dotnet/Kuyumcu.Api/appsettings.Development.json.ornek   (izlenmeyen; yalnız GuncellemeSaniye satırı)
?? dotnet/Kuyumcu.Tests/S3eKurKaynagiHttpTestleri.cs        (izlenmeyen; P73KurZamanlayiciTestleri sınıfı + 4 using eklendi)
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-73-uygulama-raporu.md
```

## 10. Açık kalanlar
1. **5200 portundaki API örneği** (PID 12577) hâlâ eski 10 sn yapılandırmasıyla koşuyor ve `kuyumcu_net`'e kiracı başına saatte ~340 satır yazıyor. Kazanç ancak o örnek yeni derlemeyle yeniden başlatılınca gerçekleşir. Sahibi: orkestratör / YAYIN şeridi (bu şerit başka portlara dokunamaz).
2. **5199 QA yayını** (qa-yayin, Release anlık görüntüsü) yeni yapılandırmayı ancak `yayinla.sh` ile yeniden yayınlanınca alır. Sahibi: YAYIN şeridi.
3. **İstemci yoklaması hâlâ 10 sn.** `Kuyumcu.Web/Servisler/CanliKurServisi.cs` (`Kur:YoklamaAraligiSaniye`) ve `UstBarSozlugu.cs:50` ("ekran 10 saniyede bir yeniden dener") bu paketin dosya sınırı dışında. Sunucu artık 60 sn'de bir güncellediği için 10 sn'lik istemci yoklaması gereksiz yük; bu, P-70'in (/kur/guncel istek sayısı) alanında. Metin değişikliği Sozlukler hub'ına dokunur, bu yüzden orkestratöre bırakıldı.
4. Tam süitte başkasına ait kararsız kırmızılar (tek başına yeşil):
   - `VadeEkranTestleri.VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron`
   - `VadeEkranTestleri.VadeTakibi_HizliSecim_AriaPressedDogru`
   - `EtiketEkranTestleri.TaslagiTemizleFormuBosaltir`

   Sahipleri Vade ve Etiket ekranlarının şeritleri; bu paketin dosyalarıyla kesişmiyorlar.
5. `docs/` altında 10 sn ifadesi varsa DALGA 7c şeridinin alanında; bu şerit docs/'a dokunmadı.
6. Geçmiş 160 452 otomatik satırın zamanlayıcı ile `POST /kur/guncelle` arasındaki kesin dağılımı ölçülemedi (ayrım damgası yok).

## 11. Kurucuya / mali müşavire notlar
Mevzuat içeriği yok. Kurucuya bilgi: vitrindeki has fiyatı artık en geç ~1 dakikada bir tazelenir. Canlı fiyat hassasiyeti gereken an için `POST /kur/guncelle` (elle "şimdi güncelle") yolu aynen duruyor. Gerekirse `Kur:GuncellemeSaniye` 15 sn'ye kadar indirilebilir; altı kabul edilmez.
