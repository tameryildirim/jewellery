# PAKET KUR-BANT — uygulama raporu (09.10.2026 · t2 · canlı 5162 / vekil 5163 · CDP 9910/9911)

**Konu:** Elle kur/fiyat girişinde makul bant yoktu (GOC-A kapanış devri, YÜKSEK/VERİ: has fiyat 5 ₺ ve 99.999.999 ₺ kaydediliyordu).
**Rol:** Taşkın (backend) + Ertuğ (frontend). **COMMIT YOK** — değişiklikler çalışma ağacında.
**Durum (TUR 2):** BİTTİ — doğrulayıcının 5 bulgusu kapatıldı/devredildi (§ 0); paket kapısının 7 maddesi son kodla yeniden koşuldu. § 1–§ 13 TUR 1 kaydıdır; TUR 2 ile değişen hükümler § 0'da, çelişen yerde § 0 geçerlidir (ör. tur 1 canlı tablosundaki "MÜDÜR onaylı USD 600 → 201" artık 400).

## 0. TUR 2 (09.10 03:40–04:30) — doğrulayıcı bulguları

### 0.1 Bulgu → yapılan

| Önem | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| KRİTİK (tasarım hakemi) | TEZGAHTAR bant dışı fiyatta yetki cümlesi iki kez, iki boyda (gri büyük `#m-bant-yetki` + kırmızı hata bandı) | `Kur.razor`: yetkisiz rolde `manuelHata = null` (bant aynı cümleyi basmaz); `#m-bant-yetki` TEK yer, `metin-soluk metin-kucuk` (12 px). Id değişmedi. | KB24 (cümle markup'ta tam 1 kez + `metin-kucuk`); MKB2-4; Chrome: `yetkiSay=1`, `fontSize=12px` 8/8 kombinasyon; görüntü `kur_TEZGAHTAR_tezgahtar-bant__BASIT_aydinlik_1280.png` |
| ORTA | Onay yolu mutlak sınırı da aşıyor (SAHİP + gerekçe → has 5 TL 201) | Core `FiyatBandi.OnaylanabilirMi(h)` (yalnız `FiyatBantDisi`); sunucu `ManuelKurKaydetIc` önce mutlak sınır dışı alanı **onaydan bağımsız 400** ile reddeder; ekran mutlak dışında onay kutusunu HİÇ çizmez, "Form eksik ya da hatalı" bandı + alan altı neden. Onay yalnız ± %20'yi (mutlak içinde) geçer. Başlık yorumu düzeltildi ("sert kapı"). `KURBANTSinirHttpTestleri.H06` beklentisi 201 → **400** (adı `H06_Onayli_asim_mutlak_siniri_gecemez_400`). | KB09, KB09c×2, KB17×4, KB20, KB25, H06; MKB2-1/2/6; canlı: SAHİP has 5 onaylı 400, MÜDÜR 99.999.999 onaylı 400, SAHİP USD 600 / 4,99 onaylı 400 |
| DÜŞÜK | Servis içi rol kapısı (`!BantOnayYetkili(islem.Rol)`) test edilmiyor | `KB09b_Servis_rol_kapisi_TEZGAHTAR_dogrudan_cagride_403` (TEZGAHTAR, MUHASEBE): servis `AktifKullanici(Rol=…)` ile DOĞRUDAN çağrılır ([RolGerekli] yok) → `ForbiddenException`, kayıt yok; aynı çağrı SAHİP ile 201. | MKB2-3 (doğrulayıcının yaptığı mutasyonun aynısı) → KB09b ×2 KIRMIZI |
| DÜŞÜK | Bant iletisinde referans biçimi tutarsız (6.827,2 / 49,448) | `GirdiSozlugu.Mesaj`: `FiyatBantDisi` referansı PARA biçimi `#,0.00` (tr-TR / en-US) → "6.827,20 TL", "49,45 TL". (Core, Web'deki `Bicim.Para`'yı göremez; aynı iki ondalık kural.) | KB06 (6.827,20 · 49,45 · EN 6,827.20); MKB2-5; canlı ileti "(6.624,00 TL)", "(49,45 TL)" |
| ORTA | İşlem içi has gram fiyatları bantsız (hurda alış/satış, takas, eski altınla ödeme) | **GOC-C'ye YÜKSEK devir** (§ 0.6) — alan eşzamanlı GOC-C şeridinin (Satış modülü + Fis bileşenleri); görev sınırı dokunmayı yasaklıyor. | § 0.6 |

### 0.2 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs` — `OnaylanabilirMi`, başlık yorumu.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Girdi.cs` — bant referansı iki ondalık (yalnız `FiyatBantDisi` S1).
- `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs` — mutlak sınır sert kapısı (3 satır + yorum).
- `dotnet/Kuyumcu.Web/Pages/Kur.razor` — sert kapıda onay yolu yok; yetki cümlesi tek yer, küçük soluk.
- `dotnet/Kuyumcu.Tests/KurBantTestleri.cs` — KB06/KB08/KB15/KB20/KB24 güncellendi; YENİ KB09, KB09b×2, KB09c×2, KB17×4, KB25 (10 koşum).
- `dotnet/Kuyumcu.Tests/Dogrulayici/KURBANTSinirTestleri.cs` — H06 beklentisi 400 (bulgu kapandı).
Migration: **gerekmedi** (şema değişmedi). Dokunulan sayfa: yalnız `Pages/Kur.razor` (tur 1 ile aynı sayfa, aynı paket — ikinci dokunuş gerekçesi: doğrulayıcı KRİTİK bulgusu).

### 0.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --no-incremental` (son mutasyondan sonra, temiz): **0 hata, 6 uyarı** — tamamı başkasının dosyası (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`); bu paketin dosyalarında 0 uyarı.
- Filtreli: `KurBant|KURBANT` **70/70**; geniş filtre `KurBant|KURBANT|GOCA|UX1C2|Girdi|Kur` **1557/1557**.
- ⚠ Tuzak (kayıt): ilk koşumda Razor değişikliği DLL'e girmemişti (artımlı derleme `Kur.razor`'u eski hâliyle bıraktı; 3 bUnit testi eski davranışı gördü). `--no-incremental` derlemeyle düzeldi; mutasyon betiği bu yüzden her turda `--no-incremental` derler.

| Kod | Değişiklik (tek eşleşme, `perl -0777 -pi`) | Kırmızı testler | cmp |
|---|---|---|---|
| MKB2-1 | Sunucu sert kapı `if (false && …)` | KB09c(5), KB09c(99.999.999), KB17×4, H06 (7) | AYNI |
| MKB2-2 | `OnaylanabilirMi` → `true` | KB09, KB20, KB25, KB09c×2, KB17×4, H06 (10) | AYNI |
| MKB2-3 | Servis rol kapısından `\|\| !BantOnayYetkili(islem.Rol)` silindi | KB09b(TEZGAHTAR), KB09b(MUHASEBE) | AYNI |
| MKB2-4 | Ekran yetkisiz rolde bant yine `KB.YetkiYok` (çift cümle) | KB24 | AYNI |
| MKB2-5 | Bant referansı biçimi `#,0.##########`'a geri | KB06 | AYNI |
| MKB2-6 | Ekran `sertVar = false` | KB20, KB25 | AYNI |
Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/KUR-BANT-t2/`, kayıtlar `/private/tmp/kuyumcu-serit/KUR-BANT/t2/MKB2-*-test.log`. Tur 1'in MKB-1…9'u değişmeyen kodu ölçer (§ 6).

- **Tam süit (tur 2, BİR kez, son kod)** `trx-pKUR-BANT/t2/_Mac_2026-10-09_04_06_21.trx`: toplam **11481** · geçti 11457 · **başarısız 22**. Hiçbiri bu paketin dosyasına bağlı değil (adlarda/hatalarda Kur/FiyatBandi/KurModulu/GirdiSozlugu yok); hepsi koşum sırasında başka şeritlerin değiştirdiği dosyalardan:
  - `KESIFP5SinirKutuTestleri` ×11, `KesifP1SayiTestleri.Sayi_TamSayi_…` — `Bilesenler/Form/Sayi.razor` (04:12'de, süit koşarken başka şerit değiştirdi; hata: `role="status" hidden>Tam sayı yazın` beklenmedik).
  - `P78.FisKalemTablosuTurkceTestleri` ×4, `KesifP5BilesenTestleri.FisKalemTablosu_birim_fiyat_binlikli`, `PatchDortFisTavanTestleri.P4_…` ×2 — `Bilesenler/Fis/FisKalemTablosu.razor` (03:57, GOC-C; hata: `Izgara<FisKalemi>` için `Api` servisi kayıtlı değil).
  - `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (Satış/GOC-C), `MobilPariteCssTestleri.B9_…` (`Pages/Islemler.razor` `<KartListe` yok — YILDIZ), `Q103IsletmeKimlikEkranTestleri.Q103_E03…("12345 67890")` (Isletme.razor — tur 1'de de kayıtlı).
  - **Bu paketin yeni kırmızısı: 0.** Test sayısı tur 2: KurBant 39 → 49 (+10); doğrulayıcı sınıfı 21 (H06 yeniden adlandı).
- `bash dotnet/denetim.sh`: ✗ yalnız 2, ikisi de başkasının: `P-30 DEMO_INSAN_SURESI` (tarih kapısı, tur 1'de de) ve `OLCEK-1 YUZ_OLCU_ENVANTER_DISI = 2` (`zo-islem.css` `#islem-kalem-tablo` font-size/weight — başka şerit). Bu paketin sayaçları (P-36b/c, GIRDI-2, SOZLUK, SATIR_ICI_STYLE, SABIT_RENK, ModalPencere) ✓.

### 0.4 Canlı doğrulama (5162, kiracı `PAKETKUR-BANT-TEST t2 …` id 01M4F36EBY4N7NZSEEWEF0HPY1)
Güncel kur (otomatik Altınkaynak): has 6.624 · USD 49,448. Red denemeleri öncesi/sonrası kayıt 1 → 1, iz 0 → 0.

| Adım | Beklenen | Gerçek | İleti (yanıttan okundu) |
|---|---|---|---|
| SAHİP has 5 onaysız | 400 | 400 | olağan aralığın dışında (500–50.000 TL) · altinHasFiyati |
| **SAHİP has 5 ONAYLI + gerekçe** | 400 | 400 | aynı · altinHasFiyati (tur 1: 201) |
| **MÜDÜR has 99.999.999 ONAYLI** | 400 | 400 | aynı · altinHasFiyati |
| **SAHİP USD 600 ONAYLI** | 400 | 400 | (5–500 TL) · usdTry (tur 1: 201) |
| SAHİP USD 4,99 ONAYLI | 400 | 400 | (5–500 TL) · usdTry |
| SAHİP has 0 / eksi / 1e10 / harf | 400 ×4 | 400 ×4 | 0'dan büyük · Eksi değer · çok büyük · geçerli bir sayı değil |
| SAHİP has 7.948,81 (+%20 üstü) | 400 | 400 | son geçerli fiyattan **(6.624,00 TL)** yüzde 20'den fazla |
| SAHİP USD 64,2824 (+%30) onaysız | 400 | 400 | son geçerli fiyattan **(49,45 TL)** … · usdTry |
| şubeli TEZGAHTAR +%30 ONAYLI | 403 | 403 | gerekli rol: dükkân sahibi ya da müdür |
| SAHİP onaylı +%30, gerekçe 5 karakter | 400 | 400 | Gerekçe en az 20 karakter (şu an 5) · bantGerekce |
| SAHİP onaylı +%30 (8.611,20, mutlak içi) | 201 | 201 | — |
| MÜDÜR onaylı USD +%30 (64,2824, mutlak içi) | 201 | 201 | — |

SQL:
```
kur_snapshots: 01M4F37BBB… 6624.0000 49.4480 ALTINKAYNAK · 01M4F37C6T… 8611.2000 NULL MANUAL · 01M4F37C8D… 8611.2000 64.2824 MANUAL
denetim_kayitlari KurBantAsimi:
01M4F37C6TESFBSC8Q6TGQAW6X  sahip=1  EKLEME  hassas=1  … altinHasFiyati=8611.2 (son geçerli 6624.0000) · gerekçe: Piyasa bugun sert yukseldi, …
01M4F37C8D4HJWSCZPR8H2B5SM  sahip=0  EKLEME  hassas=1  … usdTry=64.2824 (son geçerli 49.4480) · gerekçe: Doviz kurunu bankadan teyit ettim, dogru.
mutlak sınır dışı kayıt (has<500|>50000, usd<5|>500): 0
```
Temizlik: API PID'i ve vekil `kill` (5162/5163 dinleyen 0); `zsh kiraci-temizle.sh 01M4F36EBY4N7NZSEEWEF0HPY1 --uygula`; kendi davet satırı (`KURBANTDAV…`) silindi; kiracının kur/denetim satırı 0; **tenants 20 → 20**. QA-TEST- kiracılarına dokunulmadı.

### 0.5 Gerçek başsız Chrome (vekil 5163; SAHİP CDP 9910, TEZGAHTAR CDP 9911; `sikisma-olc.js`; ham `/private/tmp/kuyumcu-serit/KUR-BANT/t2/olcum/olcum.jsonl`, 32 geçerli ölçüm + görüntü)
Durumlar: `sayfa` · `sert` (SAHİP, has 99.999.999 → Kaydet: onay kutusu ÇIKMAMALI) · `onay-acik` (SAHİP, has 11.200 = +%69 → onay + "kısa" → Kaydet) · `tezgahtar-bant` (TEZGAHTAR, has 11.200 → Kaydet: yetki cümlesi 1 kez, küçük).

| durum | 1280 B-açık | 1280 B-koyu | 375 B-açık | 375 B-koyu | 1280 P-açık | 1280 P-koyu | 375 P-açık | 375 P-koyu |
|---|---|---|---|---|---|---|---|---|
| sayfa | K1 0·K3 0/0·K5 0·Y 0·K9 0 | = | = | = | = | = | = | = |
| sert | onay=yok · K1/K3/K5/Y 0 · K9 6 · cBinen 0 · cTaşan 0 | = | onay=yok · cBinen 1 | = | onay=yok · cBinen 0 | = | onay=yok · cBinen 1 | = |
| onay-acik | onay=var · K1/K3/K5/Y 0 · K9 6 · cBinen 1 · cTaşan 0 | = | = | = | = | = | = | = |
| tezgahtar-bant | onay=yok · yetki 1×/12 px · K1/K3/K5/Y 0 · K9 6 · cBinen 0 | = | yetki 1×/12 px · cBinen 0 | = | yetki 1×/12 px · cBinen 0 | = | yetki 1×/12 px · cBinen 1 | = |

K1 gizli-görünen · K3 taşan/kırık · K5 'TRY' kalıbı · Y yatay kaydırma — **32/32 sıfır**. K9 = 6 ve cBinen ≤ 1 tur 1 § 9'daki ölçer sınırıdır (açık çekmecenin ARKASINDAKİ sayfa öğeleri; 375'te kaydırılan gövdedeki `m-kaydet`/`m-gram` yapışkan alt/üst çubuğun altından geçiyor — örnekler `m-kaydet × cekmece-kur-manuel-iptal`, `cekmece-kapat × m-gram`); görüntülerde üst üste yazı yok. Mod `modG` alanıyla her satırda doğrulandı.
**Ölçüm sırasında bulunan (kapsam dışı) kusur:** TEZGAHTAR üst çubuktaki BASİT/PRO anahtarıyla modunu DEĞİŞTİREMİYOR — `ArayuzModuServisi` `PATCH /kullanicilar/ben` çağırıyor, uç `[ModulYetkisi("kullanici")]` → **403 "Bu ekran için yetkiniz yok (kullanici)"** (canlıda ölçüldü); dar uç `PATCH /kullanicilar/ben/arayuz-modu` 200 veriyor. TEZGAHTAR BASİT ölçümü bu dar uçla mod ayarlanıp yeniden yapıldı (ilk 4 satır PRO çıktığı için geçersiz sayıldı). Sahibi: Layout/ArayuzModuServisi (YILDIZ/kabuk şeridi) — § 0.7.
Görülen görüntüler (Read 4): `kur_TEZGAHTAR_tezgahtar-bant__BASIT_aydinlik_1280.png` (alan altı kırmızı neden + altında tek, küçük gri "Fiyat doğruysa bunu yalnız dükkân sahibi ya da müdür onaylayabilir." + Kaydet; kırmızı bant YOK), `kur_TEZGAHTAR_tezgahtar-bant__PRO_karanlik_375.png` (aynı, tek cümle, taşma yok), `kur_SAHIP_sert__BASIT_aydinlik_375.png` (99999999 → "olağan aralığın dışında (500–50.000 TL)" + "Form eksik ya da hatalı" bandı; onay kutusu yok), `kur_SAHIP_onay-acik__PRO_karanlik_1280.png` (6.628,00 TL iki ondalık; onay kutusu + "Neden bu fiyat?" kutusu).

### 0.6 GOC-C kuyruğuna DEVİR — **YÜKSEK**
**Başlık:** İşlem içi has gram fiyatlarında makul bant yok (hurda alışta has gram başına 5 TL kabul ediliyor, ödeme kasadan çıkıyor).
**Alanlar (gerçek satırlar 09.10):** `SatisModulu.cs` `BirimFiyatTlHasGram` (hurda alış ~251, hurda satış ~342), `EskiUrunBirimFiyatTlHasGram` (takas ~554), `OdemeSepeti.cs:81` `BirimFiyatTlHasGram` (eski altınla ödeme); ekran `hurda-fiyat` (FisHurdaAlis), `hrd-fiyat` (Hurda.razor), `takas-eski-fiyat`, `altin-fiyat` (AltinBacagiPaneli). Bugün yalnız `[Range(0, KolonTavani.UstTutar)]`.
**Hazır çağrı:** `FiyatBandi.Denetle(fiyat, FiyatTuru.Has, kurSnapshot.AltinHasFiyati)` → `GirdiKapisi.Denetle(h, alan)`; mutlak sınır (500–50.000) SERT kapı, ± sapma yalnız `bantOnay` + `bantGerekce` (SAHİP/MÜDÜR, `FiyatBandi.OnaylanabilirMi`, `denetim.YazmaKaydet(…, hassas: true)`) — KurModulu ile aynı kalıp; ekran aynı Core kuralı + aynı GirdiSozlugu metni.
**Önce ekip oyu:** hurda alış fiyatı piyasanın altında olağandır → hurda için sapma oranı (± %20 dar olabilir; ör. alt −%40 / üst +%10) ayrı oylanmalı. ~40 test fikstürü `BirimFiyatTlHasGram` 1.000/2.800 TL kullanıyor (fikstür kur 6.827,20) → mutlak alt sınır 500'ün üstünde kalır ama ± bant onlarını kırar; fikstür güncellemesi aynı pakette.
**Neden bu pakette yapılmadı:** Satış modülü + Fis bileşenleri GOC-C'nin eşzamanlı dosya alanı (görev: "Urun/Fiyat … GOC-C alanında — çakışırsa GOC-C'ye devir"); tam süitteki 8 Fis kırmızısı GOC-C'nin bu dosyalarda şu an çalıştığını gösteriyor.

### 0.7 Açık kalanlar (tur 2 ekleri; tur 1 § 12 geçerli)
1. **YÜKSEK → GOC-C:** § 0.6.
2. **ORTA → kabuk/YILDIZ şeridi:** TEZGAHTAR (ve muhtemelen MUHASEBE) üst çubuktan BASİT/PRO değiştiremiyor (`PATCH /kullanicilar/ben` 403; dar uç `…/ben/arayuz-modu` var ama UstBar onu kullanmıyor).
3. Başkasına ait tam süit kırmızıları (22) § 0.3'te sahipleriyle; denetim ✗ 2 (P-30 tarih, OLCEK-1 zo-islem.css).
4. Tur 1 § 12 madde 6 (375'te çekmece Kaydet'i gövdede, İptal alt çubukta) değişmedi — görüntüde de görülüyor; DÜŞÜK, ortak Cekmece bileşeni.

### 0.8 Koşul karşılama (tur 2 özet)
| Koşul | Durum | Kanıt |
|---|---|---|
| Bant ±%20 + mutlak sınır; mutlak sınır onayla aşılamaz (fail-closed) | KARŞILANDI | FiyatBandi.OnaylanabilirMi; KB09/KB09c/KB17/H06; canlı 4 onaylı 400 |
| Onay yolu yalnız SAHİP/MÜDÜR + gerekçe + denetim izi; TEZGAHTAR 403 iki katmanda | KARŞILANDI | KB13, KB15 (uç filtresi), **KB09b (servis kapısı, MKB2-3 kırmızı)**; SQL 2 iz satırı |
| Sunucu ve ekran aynı kural, aynı metin; para biçimi tutarlı | KARŞILANDI | GirdiSozlugu; KB06/KB20/KB21/KB24; canlı "(6.624,00 TL)", "(49,45 TL)" |
| Tasarım hakemi KRİTİK: tek cümle, tek yer, küçük | KARŞILANDI | KB24; Chrome yetkiSay=1 / 12 px ×8; 2 görüntü |
| HTTP 400 alanlı · 201 + iz · 403 · hasım değerleri | KARŞILANDI | § 0.4 |
| Mutasyon ≥ 3 kırmızı, cmp AYNI | KARŞILANDI | 6/6 (§ 0.3) |
| Gerçek Chrome 1280+375 · BASİT+PRO · açık+koyu | KARŞILANDI | § 0.5 (32 ölçüm, Read 4) |
| build 0 hata · denetim (paket sayaçları ✓) · tam süit 0 yeni kırmızı (bir kez) | KARŞILANDI | § 0.3 |
| Sınıf taraması: işlem içi has fiyatları | DEVİR (YÜKSEK → GOC-C) | § 0.6 |
| Test kiracısı silindi, tenants başlangıç | KARŞILANDI | 20 → 20 |
| TOKEN: tam süit bir kez, Read ≤ 4 | KARŞILANDI | 1 tam süit, 4 görüntü |

### 0.9 Kurucu / mali müşavir
Mevzuat sorusu yok (iş kuralı; yasal eşik yazılmadı). Kurucuya bilgi: tur 2 ile has 500–50.000 TL/g ve döviz 5–500 TL dışı fiyat **dükkân sahibi dahil kimse tarafından** kaydedilemez; piyasa bu aralığı aşarsa sınır `FiyatBandi.cs`'te tek sabitten değiştirilir (ekip oyu).

---
# TUR 1 kaydı

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)

| İddia | Kodda bulunan | Karar |
|---|---|---|
| Manuel kurda has 5 ve 99.999.999 kaydediliyor | DOĞRU. `KurServisi.ManuelKurKaydet` yalnız K16 (> 0, 4 ondalık, tavan 999.999.999,999999) uyguluyordu. | Bant eklendi. |
| "SAHİP onaylı aşım 201 · TEZGAHTAR aşım 403" | `POST /kur/manuel` zaten `[RolGerekli("SAHIP","MUDUR")]`; TEZGAHTAR/MUHASEBE uca hiç giremiyor (403). | 403 iki katmanda: uç filtresi (değişmedi) + servis içinde `BantOnayYetkili` (kullanıcısız iç yol dahil, fail-closed). |
| "Döviz kuru elle girilen her yer" | Taramada (`grep [Kur] / GirdiKapisi.Kur`) döviz/has kuru **yalnız** `/kur/manuel`'de elle giriliyor; kasa/POS/döviz veresiye kuru güncel kayıttan türetiliyor (UX-S21). | Kur sınıfı tek uçta kapandı. |
| "Fiyat listesi, ürün alış maliyeti, işçilik, has fiyat elle girilen her yer" | Ayrıntı § 2 (sınıf taraması). İşlem içi has fiyatları (hurda alış/satış, takas, eski altınla ödeme) **Satış modülünde (GOC-C alanı)**. | Core kural hazır; Satış uçlarına bağlama GOC-C'ye devir (gerekçe § 2). |
| "Sınırlar ayar sabiti (AlanSinirlari), Core'da tek kaynak" | `AlanSinirlari` uzunluk sabitleri sınıfı. | Sınırlar kuralın kendi sınıfında: `Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs` (`SapmaYuzde`, `HasEnAz/EnCok`, `DovizEnAz/EnCok`) — ekran ve sunucu aynı sabiti okur; YasalParametre DEĞİL. |
| Önerilen bant | ± %20; önceki yoksa has 500–50.000 TL/g, USD/EUR 5–500 TL | Aynen. GBP de döviz sınırıyla (5–500); gram altın has sınırıyla. **Mutlak sınır her zaman** uygulanır (önceki değer olsa da — fail-closed), ± %20 önceki geçerli değer varsa ek olarak. |

**Referans değer:** alan başına son GEÇERLİ (iptal edilmemiş) `kur_snapshots` kaydındaki dolu değer (son 50 kayıt içinde). Son kayıtta USD boşsa bir önceki dolu USD kullanılır; hiç yoksa yalnız mutlak sınır. Otomatik ya da manuel ayrımı yok (spesifikasyon: "son geçerli otomatik ya da manuel değer").
**Mevzuat:** Bu kural İŞ KURALIDIR (yazım hatası kapısı); hiçbir mevzuat hükmüne dayanmaz, birincil kaynak gerekmez. Yasal eşik/oran yazılmadı.

## 2. Sınıf taraması (envanter (B) Kur #8 + fiyat satırları)

| Sınıf | Alanlar (ekran → uç) | Bugünkü kapı | Bu pakette | Gerekçe / devir |
|---|---|---|---|---|
| Elle kur (has, gram altın, USD, EUR, GBP) | Kur.razor `m-has m-gram m-usd m-eur m-gbp` → `POST /kur/manuel` | K16 | **KAPANDI**: FiyatBandi (ekran + sunucu aynı kural/metin) + SAHİP/MÜDÜR onay yolu + denetim izi | — |
| Döviz kuru başka ekranda | yok (kasa/POS/döviz veresiye kuru güncel kayıttan) | UX-S21 | tarandı, elle giriş yok | — |
| İşlem içi has gram fiyatı | `hurda-fiyat` (FisHurdaAlis) → `SatisModulu.HurdaAlis`; `hrd-fiyat` (Hurda.razor) → `HurdaSatis`; `takas-eski-fiyat` → `Takas`; `altin-fiyat` (AltinBacagiPaneli) → `OdemeSepeti` | > 0 / ≥ 0, tavan | **DEVİR → GOC-C** | Satış modülü + Fis bileşenleri GOC-C'nin eşzamanlı alanı (görev sınırı: "çakışırsa GOC-C'ye devir"). Ayrıca mevcut ~40 test fikstürü `BirimFiyatTlHasGram` 1.000/2.800 TL kullanıyor (fikstür kur 6.827,20) — bağlamak başka şeridin testlerini kırardı. Hazır çağrı: `FiyatBandi.Denetle(fiyat, FiyatTuru.Has, <kurSnapshot.AltinHasFiyati>)` + aynı onay alanları. Ek ekip oyu gerekir: hurda alış fiyatı piyasanın altında olağan; ± %20 hurdada dar olabilir. |
| Fiyat listesi marj/çarpan % | `fl-marj`, `fl-carpan`, `flp-*` → FiyatModulu | `FiyatAraliklari` sert sınır (−100..500 / 0..500) + rahat aralık dışı risk onayı | değişiklik yok | Zaten "bant + onay" kalıbı var (K15). |
| Ürün satış fiyatı / alış maliyeti / işçilik (TL) | Urunler.razor `u-*`, `d-*` → UrunModulu | K9 tutar + kolon tavanı | değişiklik yok | Önceki değere göre % bant ürün tutarında anlamlı değil (aynı ekranda 10 TL ile 10 M TL pırlanta). Öneri (GOC-C, ekip oyu): kıymetli madende "alış maliyeti ÷ has gram" değerini güncel has fiyatına göre bantlamak. |
| Belge kalemleri (fatura, vezne, sipariş) birim fiyat/işçilik | Faturalar, Vezne, SiparisDetay | K9 | değişiklik yok | Referans değer yok; tutar kuralı (K9) yeterli. |
| Vitrin pano marjı TL | Vitrin.razor `pano-*` | ≤ 1.000.000 | değişiklik yok | Mevcut sınır. |

## 3. Yapılanlar

**Core (tek kaynak)**
- `Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs` (YENİ): `FiyatTuru {Has, GramAltin, Doviz}`, sabitler, `MutlakSinir`, `Denetle(deger, tur, onceki)`. null/≤0 → null (o mesaj K16'nın).
- `GirdiHatasi.cs`: `GirdiKodu.FiyatMutlakDisi, FiyatBantDisi`.
- `Sozlukler.Girdi.cs`: iki kodun TR+EN metni (konumsal; eksik dil derleme hatası). TR: "Bu fiyat olağan aralığın dışında (500–50.000 TL). Rakamı yanlış yazmış olabilirsiniz; kontrol edin." / "Bu fiyat son geçerli fiyattan (6.618 TL) yüzde 20'den fazla farklı. …". EN metinde de "TL" (K5 'TRY' kalıbı yok).
- `Sozlukler.KurBant.cs` (YENİ): onay yolu metinleri TR+EN (OnayEtiket, GerekceEtiket, GerekceIpucu, YetkiYok, OnayGerekli, OnayliKaydedildi).

**API (`Kuyumcu.Api/Modules/Kur/KurModulu.cs` — yalnız doğrulama)**
- `ManuelKurDto`: `BantOnay` (bool?), `BantGerekce` ([StringLength(1000)]).
- `ManuelKurKaydet(dto, AktifKullanici)` (uç yolu) + `ManuelKurKaydet(dto, tenantId)` (iç/test yolu, kullanıcısız → onay İMKÂNSIZ, fail-closed 403). Sıra: K16 (tüm alanlar) → `BantAsimlari` → aşım varsa: onay yoksa **400 + alan + sözlük metni** (`GirdiKapisi.Denetle`); onay var ama rol SAHİP/MÜDÜR değil → **403**; gerekçe K30 (≥ 20) → `denetim.YazmaKaydet(islem, "KurBantAsimi", <snapshotId>, "… alan=değer (son geçerli X) · gerekçe: …", hassas: true)` → kayıt (aynı SaveChanges).
- `BantOnayYetkili(rol)` TAM eşleşme (`"SAHIP" or "MUDUR"`). `SnapshotKaydet` isteğe bağlı `id` aldı (denetim izi `varlik_id` = kur kaydı).
- Uç: `servis.ManuelKurKaydet(dto, Islem)`; `[RolGerekli("SAHIP","MUDUR")]` DEĞİŞMEDİ.

**Ekran (`Kuyumcu.Web/Pages/Kur.razor` — yalnız alan doğrulama)**
- Kaydet'te aynı Core kuralı; referans = ekrandaki güncel (geçerli) kur. Aşımda **POST YOK**; her aşan alanın altında sunucuyla AYNI cümle (m-gram, m-eur, m-gbp alanlarına `Durum/HataAlani` eklendi; id'ler değişmedi).
- Aşım varken: SAHİP/MÜDÜR'e `#m-bant-onay` ("Fiyat doğru, yine de kaydet") + işaretlenince `#m-bant-gerekce` (CokSatir, ≥ 20, sayaç); diğer rollere yalnız `#m-bant-yetki` cümlesi. Aşım yokken hiçbiri görünmez (zorunlu önce kuralı). Onay işaretliyken neden alanın altında KALIR.
- Gövdeye `bantOnay`/`bantGerekce` yalnız aşım + onayda gider; başarı bandı onaylı kayıtta "olağandışı fiyat onayınız işlem kaydına yazıldı".
- Sunucu 400'ü `bantGerekce` → `m-bant-gerekce` eşlemesiyle alana düşer.

**Migration:** gerekmedi (şema değişmedi; denetim izi mevcut `denetim_kayitlari`).

## 4. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| `kur_snapshots` (elle kur, onaylı aşım dahil) | Yeni varlık YOK. Yanlış elle kur → mevcut `POST /kur/manuel/{id}/iptal` (S-3b, gerekçeli, geçersiz damgası; ekranda satır eylemi var). Doğru değer = yeni elle giriş. | Defter yerinde değişmez (S-3b kuralı). İptal edilen kayıt bant referansı OLMAZ (test KB12, mutasyon MKB-5). |
| `denetim_kayitlari` "KurBantAsimi" | Salt okunur (append-only, ADR-0004) | Denetim izi değiştirilemez — tasarım gereği. |
| Onay alanları (`bantOnay`, `bantGerekce`) | Kalıcı alan değil; istek alanı | — |

Her düğme: `#m-kaydet` ya POST atar ya da alan altı/bant metniyle durur (sessiz etkisizlik yok); `#m-bant-onay` yalnız durum değiştirir (gerekçe kutusunu açar). Yeni ModalPencere yok (çekmece kalıbı korunuyor).

**Dokunulan sayfalar:** `Pages/Kur.razor`. TEK DOKUNUŞ gerekçesi: GOC-A bu sayfaya dokunmuştu; KUR-BANT, GOC-A kapanış devrinin bu sayfa için açtığı madde (aynı manuel kur çekmecesi).

## 5. Testler (`Kuyumcu.Tests/KurBantTestleri.cs`, 39 koşum)

- Saf (24): KB01 bant içi + tam sınır (8.192,64 / 5.461,76 / 500 / 50.000), KB02 ± %20 dışı (değerler), KB03 mutlak dışı (5, 499,99, 50.000,01, 99.999.999; önceki olsa da), KB04 döviz 4,99/5/500/500,01, KB05 null/0/eksi bu kuralın değil, KB06 sözlük TR+EN, KB07 `BantOnayYetkili` (SAHIP/MUDUR evet; TEZGAHTAR/MUHASEBE/küçük harf/null hayır).
- Servis DB (1): KB08 kullanıcısız yolda onaylı aşım 403 + kayıt yok.
- HTTP gerçek MySQL (9): KB10 hasım (5, 99.999.999, 0, eksi, 1e10, harf, USD 4,99, GBP 1.000) 400 alanlı + sayı değişmez; KB11 tam sınır 201 / +0,01 400 / referans kayar; KB12 iptal kaydı referans olmaz + alan başına son dolu değer; KB13 SAHİP/MÜDÜR onaylı 201 + gerekçe kısa 400 `bantGerekce` + denetim izi (varlik_id, kullanici_id, hassas, detay); KB14 bant içi kayıtta iz yok; KB15 TEZGAHTAR/MUHASEBE onaylı 403 + kayıt yok; KB16 gram altın/EUR alanları + bant içi onay iz bırakmaz.
- bUnit (5): KB20 POST YOK + alan altı sunucuyla aynı metin + onay yolu yalnız aşımda; KB21 bant metni, onay + kısa gerekçe POST YOK, neden alanda kalır; KB22 gövdede `bantOnay:true` + gerekçe + başarı bandı; KB23 bant içinde onay alanı null; KB24 TEZGAHTAR onay yolu yok, `#m-bant-yetki` cümlesi.

**Kasıtlı davranış değişikliğiyle güncellenen 2 eski test** (yorumlu): `Dogrulayici/GOCASinirTestleriTur2.DG2_S04` (0,0001 TL has artık bant dışı → 4 hane çözünürlüğü 6.827,0001 ile), `UX1C2EkranTestleri.Kur_manuel_kayit_POST_sonrasi_KENDI_basari_mesajini_yazar` (sahte güncel 4.100'e karşı 6.800 = +%66 → 4.500).

Filtreli koşum (KurBant + kur'a dokunan 8 eski sınıf): **158/158 geçti**; ekran ilgili son koşum 84/84.

## 6. Mutasyon kanıtı (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KUR-BANT/`, `perl -0777 -pi`, filtre `KurBant`, geri al → `cmp` AYNI; son mutasyondan sonra temiz build 0 hata)

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| MKB-1 | FiyatBandi: üst bant koşulu silindi | KB02, KB11, KB12, KB13×2, KB16, KB21, KB22 | AYNI |
| MKB-2 | `HasEnCok` 50.000 → 500.000.000 | KB03×4, KB10, KB20 | AYNI |
| MKB-3 | Servis rol kapısı `if (false)` | KB08 | AYNI |
| MKB-4 | Onay yokken 400 kapısı `if (false)` | KB10, KB11, KB12, KB16 | AYNI |
| MKB-5 | Referans sorgusunda `!IptalEdildi` silindi | KB12 | AYNI |
| MKB-6 | `denetim.YazmaKaydet` silindi | KB13 (SAHIP, MUDUR) | AYNI |
| MKB-7 | Ekran `BantOnayYetkili` + TEZGAHTAR | KB24 | AYNI |
| MKB-8 | Ekran aşım kapısı `if (false)` | KB20, KB21, KB24 | AYNI |
| MKB-9 | Ekran alan altı bant metni satırı silindi | KB20, KB21, KB24 | AYNI |

## 7. Derleme · denetim · tam süit

- `dotnet build Kuyumcu.sln`: **0 hata**. Tam derlemede 6 uyarı, hepsi başkasının test dosyasında (SUBEKAPSAMASinirTestleri.cs:49, KesifP2Testleri.cs:455, SonKalanlarTestleri.cs:593/824, Yildiz2Testleri.cs:193/231); bu paketin dosyalarında 0 uyarı.
- `bash dotnet/denetim.sh` (son kod): bu paketin sayaçları ✓ (P-36b/P-36c girdi ailesi, GIRDI-2 ×7, SOZLUK, SATIR_ICI_STYLE, SABIT_RENK, ModalPencere 47/55 …). ✗ olanlar başkasının: `Pages+Bilesenler+Layout record tanımı 5>3` ve `P-10 BU_PAKETTE_EKLENEN_RECORD` (record'lar Yetkiler/KarZarar/SatisAnalizi/Siparisler/Giris.razor'da — Kur.razor'da record YOK), `P-02 OLCUM_TABANI_KOPYA`, `P-18 YETKILER_ROL_YAPISKAN` (Yetkiler.razor 03:10'da başka şerit), `P-30 DEMO_INSAN_SURESI` (tarih kapısı; ilk koşumda da tek ✗ buydu).
- **Tam süit 1** (bu paketin kodu, `trx-pKUR-BANT/_Mac_2026-10-09_02_53_34.trx`): total 11410 · yürütülen 11408 · geçti 11406 · **başarısız 2**: `Dogrulayici.KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri`, `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — ikisi de önceki devirlerde kayıtlı kararsız testler (EMANET-1, GOC-C, DEGISIM-GUNSONU raporları).
- **Tam süit 2** (son kod; tek değişiklik onaylı durumda alan altı metnin kalması, `trx-pKUR-BANT/son/`): 11410 · geçti 11396 · **başarısız 12** — hepsi süit 1'den SONRA başka şeritlerin değiştirdiği dosyalardan: `SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor` (SatisAnalizi.razor `record OdemeSatiri`, 03:05), `OlcumKapisiTestleri.SAYFA_RECORD_TAVAN…` ve `P02_blogundaki…` (aynı record sayısı), `OrtakGorunumKiyasSaflik.KY8/KY10` + `OrtakGorunumHakemTestleri.HK14` (Kullanicilar.razor, 03:01), `Q103IsletmeKimlikEkranTestleri.Q103_E01×2, Q103_E03×3` (Isletme.razor), `UX4dServisTestleri.M23_…` (ileti "Bitiş tarihi başlangıç tarihinden önce…" — DenetimModulu.cs/Sozlukler.cs başka şerit). Hiçbiri Kur/FiyatBandi/KurModulu dosyasına dokunmuyor; bu paketin testleri iki koşumda da yeşil. **Bu paketin yeni kırmızısı: 0.**
- Test sayısı: önce 11371 (hesap: 11410 − 39 yeni), sonra 11410.

## 8. Canlı doğrulama (5162, `ASPNETCORE_ENVIRONMENT=Development`, kiracı `PAKETKUR-BANT-TEST …` id 01M4EZ1KC98K7QPDYV9CY874SK)

Güncel kur (Altınkaynak, otomatik): has 6.618 · USD 49,458. Kayıt sayısı red denemelerinden önce 1, sonra 1.

| Adım | Beklenen | Gerçek | İleti |
|---|---|---|---|
| SAHİP has 5 | 400 | 400 | olağan aralığın dışında (500–50.000 TL) · alanlar=altinHasFiyati |
| SAHİP has 99.999.999 | 400 | 400 | aynı · altinHasFiyati |
| SAHİP has 0 | 400 | 400 | Değer 0'dan büyük olmalı |
| SAHİP has eksi | 400 | 400 | Eksi değer yazılamaz |
| SAHİP has 1e10 | 400 | 400 | Değer çok büyük (en çok 999.999.999,999999) |
| SAHİP has harf | 400 | 400 | geçerli bir sayı değil · altinHasFiyati |
| SAHİP USD 4,99 | 400 | 400 | olağan aralığın dışında (5–500 TL) · usdTry |
| SAHİP has 7.941,61 (6.618 + %20 + 0,01) | 400 | 400 | son geçerli fiyattan (6.618 TL) yüzde 20'den fazla farklı |
| TEZGAHTAR (şubesiz) bant dışı | 403 | 403 | şube bağlamı (önce şube kapısı) |
| TEZGAHTAR (şubeli) bant dışı onaysız | 403 | 403 | "gerekli rol: dükkân sahibi ya da müdür" |
| TEZGAHTAR (şubeli) bant dışı ONAYLI | 403 | 403 | aynı; kayıt sayısı 9 → 9 |
| SAHİP onaylı, gerekçe 5 karakter | 400 | 400 | Gerekçe en az 20 karakter olmalı (şu an 5) · bantGerekce |
| SAHİP onaylı +%30 (8.603,40) | 201 | 201 | — |
| MÜDÜR onaylı USD 600 | 201 | 201 | — |
| SAHİP bant içi +%5 (9.033,57) onaysız | 201 | 201 | — |

SQL kanıtı:
```
kur_snapshots (tenant): 01M4EZ2EZB… 6618.0000 49.4580 ALTINKAYNAK · 01M4EZ2FKG… 8603.4000 NULL MANUAL · 01M4EZ2FP0… 8603.4000 600.0000 MANUAL · 01M4EZ2FPQ… 9033.5700 NULL MANUAL
denetim_kayitlari varlik_turu='KurBantAsimi':
01M4EZ2FKGTZ1Y9E7TWX368RM2  SAHIP  EKLEME  hassas=1  elle kur MAKUL BANT DIŞI onayla kaydedildi · altinHasFiyati=8603.4 (son geçerli 6618.0000) · gerekçe: Piyasa bugun sert yukseldi, …
01M4EZ2FP048Z08H0AN1VKQG6W  MUDUR  EKLEME  hassas=1  elle kur MAKUL BANT DIŞI onayla kaydedildi · usdTry=600 (son geçerli 49.4580) · gerekçe: Doviz kurunu bankadan teyit ettim, dogru.
```
(MÜDÜR satırında USD referansı 49,458: son MANUAL kayıtta USD boş → alan başına bir önceki dolu değer — canlıda da doğrulandı.)

Temizlik: API (PID kendi) ve vekil durduruldu (5162/5163 dinleyen 0); `zsh kiraci-temizle.sh <id> --uygula`; davet satırı silindi; **tenants 17 → 17**; kiracıya ait kur_snapshots/denetim/kullanıcı satırı 0.

## 9. Gerçek başsız Chrome ölçümü (vekil 5163, CDP 9910, `sikisma-olc.js`, `/private/tmp/kuyumcu-serit/KUR-BANT/olc.mjs`, ham `olcum/olcum.jsonl` 24 ölçüm + 24 görüntü)

Durumlar: `sayfa` (/kur), `bant-hata` (çekmece, has 99.999.999 → Kaydet), `onay-acik` (+ onay işaretli, gerekçe "kısa" → Kaydet). K1 gizli-görünen · K3 taşan/kırık · K5 'TRY' · Y yatay · K9 sayfa geneli binen · cBinen çekmece içi binen · cTaşan çekmece dışına taşan · K2 en geniş blok %.

| durum | w | mod | tema | K1 | K3 | K5 | Y | K9 | cBinen | cTaşan | K2 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| sayfa | 1280 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 98 |
| bant-hata | 1280 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 98 |
| onay-acik | 1280 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 98 |
| sayfa | 375 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 97 |
| bant-hata | 375 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |
| onay-acik | 375 | BASIT | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |
| sayfa | 1280 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 98 |
| bant-hata | 1280 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 98 |
| onay-acik | 1280 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 98 |
| sayfa | 375 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 97 |
| bant-hata | 375 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |
| onay-acik | 375 | BASIT | karanlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |
| sayfa | 1280 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 98 |
| bant-hata | 1280 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 98 |
| onay-acik | 1280 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 98 |
| sayfa | 375 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 97 |
| bant-hata | 375 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 97 |
| onay-acik | 375 | PRO | aydinlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |
| sayfa | 1280 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 98 |
| bant-hata | 1280 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 98 |
| onay-acik | 1280 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 98 |
| sayfa | 375 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 0 | - | - | 97 |
| bant-hata | 375 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 | 97 |
| onay-acik | 375 | PRO | karanlik | 0 | 0/0 | 0 | 0 | 6 | 1 | 0 | 97 |

**K9 = 6 ve cBinen = 1 açıklaması (görüntüyle doğrulandı, kusur değil):** K9 çekmece açıkken ARKADAKİ sayfa öğelerini sayıyor (`#kur-simdi-cek ⟂ Altın has fiyatı etiketi` — ölçer sınırı, YASAL-PARAM-2/URUN-FORM-2 devrinde kayıtlı). Çekmece içi tek "binen" çift her durumda, kaydırılabilir gövdenin yapışkan başlığın/alt çubuğun ALTINDAN geçen içeriği (`m-bant-gerekce-hata × cekmece-kur-manuel-iptal`, `cekmece-kapat × m-gram`, `m-kaydet × …-iptal`); görüntülerde içerik kaydırma alanında, üst üste yazı yok. Çekmece dışına taşan 0, yatay kaydırma 0, 'TRY' 0.
Görülen görüntüler (Read 3): `kur_onay-acik__BASIT_aydinlik_1280.png` (has kutusu altında bant metni 2 satır, onay kutusu + "Neden bu fiyat?" + 4/1000 sayaç), `kur_onay-acik__PRO_karanlik_375.png` (gerekçe hatası alan altında, hata bandı, Kaydet), `kur_bant-hata__BASIT_aydinlik_375.png` (alan altı metin + onay kutusu + "Fiyat olağandışı…" bandı).

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Bant kuralı ±%20 + mutlak (has 500–50.000, döviz 5–500), fail-closed | KARŞILANDI | FiyatBandi.cs; KB01–KB05; canlı tablo |
| Onay yolu yalnız SAHİP/MÜDÜR, gerekçe + denetim izi | KARŞILANDI | KB13, KB15, KB08, KB24; SQL denetim satırları |
| Sınırlar ayar sabiti, Core tek kaynak, YasalParametre değil | KARŞILANDI | FiyatBandi sabitleri; ekran + sunucu aynı sınıf |
| GirdiKapisi + sözlük iletisi; sunucu ve ekran aynı metin | KARŞILANDI | GirdiKapisi.Denetle + GirdiSozlugu; KB10/KB20 metin eşitliği |
| HTTP 400 alanlı + ekran aynı metin | KARŞILANDI | KB10, KB11, KB16; canlı `alanlar` |
| SAHİP onaylı aşım 201 + denetim SQL; TEZGAHTAR 403 | KARŞILANDI | KB13/KB15; canlı (şubeli tezgâhtar 403, kayıt 9→9) |
| Hasım değerleri (5, 0, eksi, 99.999.999, 1e10, harf) | KARŞILANDI | KB10 + canlı |
| Sınır testleri (içi/dışı, tam sınır, önceki yok) | KARŞILANDI | KB01–KB04, KB11, KB12 |
| Mutasyon ≥ 3 kırmızı, cmp AYNI | KARŞILANDI | 9/9 (§ 6) |
| Canlı 5162 + SQL | KARŞILANDI | § 8 |
| Gerçek Chrome 1280+375 BASİT+PRO açık+koyu | KARŞILANDI | § 9 (24 ölçüm, Read 3) |
| build 0 hata · denetim (paketin sayaçları) · tam süit 0 yeni kırmızı | KARŞILANDI | § 7 |
| Sınıf taraması (fiyat/işçilik/maliyet/döviz/has) | KARŞILANDI (tarama) · işlem içi has fiyatları DEVİR | § 2 |
| GOC-C eşzamanlılık: Urun/Fiyat uçlarına dokunulmadı | KARŞILANDI | değişen dosyalar § 11 |
| Test kiracısı silindi, tenants başlangıç değeri | KARŞILANDI | 17 → 17 |
| TOKEN: tam süit bir kez / Read ≤ 4 | KISMEN: tam süit 2 kez | İkinci koşum, ilk koşumdan sonra yapılan 3 satırlık ekran düzeltmesini (onay işaretliyken alan altı metnin kalması) son kodla ölçmek için; Read 3. |

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeci; bazı dizinler depoda izlenmediği için dosya adıyla)

- `dotnet/Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs` (YENİ)
- `dotnet/Kuyumcu.Core/Domain/Girdi/GirdiHatasi.cs` (2 kod)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Girdi.cs` (2 metin × 2 dil + switch)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KurBant.cs` (YENİ)
- `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs`
- `dotnet/Kuyumcu.Web/Pages/Kur.razor`
- `dotnet/Kuyumcu.Tests/KurBantTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/Dogrulayici/GOCASinirTestleriTur2.cs` (1 değer, yorumlu)
- `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` (1 değer, yorumlu)

Proje ağacına geçici dosya bırakılmadı (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/KUR-BANT/`).

## 12. Açık kalanlar

1. **DEVİR → GOC-C:** işlem içi has gram fiyatlarına (hurda alış/satış, takas eski altın, eski altınla ödeme) FiyatBandi bağlanması + aynı onay alanları; önce ekip oyu (hurda için bant oranı) ve ~40 fikstürün (`BirimFiyatTlHasGram` 1.000/2.800) güncellenmesi.
2. **Öneri → GOC-C (ekip oyu):** kıymetli maden ürününde alış maliyeti/gram ↔ güncel has fiyatı bandı.
3. Ekran referansı güncel kurun kendisi; güncel kayıtta bir döviz alanı boşsa ekran yalnız mutlak sınıra bakar, sunucu daha eski dolu değere bakar → bu nadir durumda ekranın geçirdiğini sunucu 400 ile alan altına düşürür (sessiz değil).
4. Ölçer: sikisma-olc K9 açık çekmecede arka planı sayıyor (bilinen, orkestratör betikleri).
5. Başkasına ait (tam süit 2 / denetim): SatisAnalizi/KarZarar/Yetkiler/Siparisler/Giris.razor record'ları (SozlesmeYansima, OlcumKapisi, denetim record tavanı), Kullanicilar.razor (KY8/KY10/HK14), Isletme kimlik (Q103 ×5), DenetimModulu ileti (UX4d M23), P-18 YETKILER_ROL_YAPISKAN, P-02 OLCUM_TABANI_KOPYA, P-30 DEMO_INSAN_SURESI; kararsız: KATMAN2 Asenkron_tasimada…, Yasal4 Mutabakat_bugun…
6. GOC-A devrinden kalan, bu pakette dokunulmayan DÜŞÜK: manuel kur çekmecesinde 375'te Kaydet gövdede, İptal alt çubukta.

## 13. Kurucu / mali müşavir

Mevzuat sorusu yok (iş kuralı). Kurucuya bilgi: bant %20 ve sınırlar (has 500–50.000 TL/g, döviz 5–500 TL) ekip oyuyla konan TUTUCU değerlerdir; piyasa bu aralığın dışına çıkarsa dükkân sahibi/müdür gerekçe yazarak kaydedebilir, sabitler `FiyatBandi.cs`'te tek yerde değişir.
