# PAKET P-45 — Kâr-zarar defter has gramı: maden kırılımı (altın + gümüş TOPLANMAZ)

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** PARA VE SAYI DOĞRULUĞU (tur 8 triyajı)
**Gerçek süre:** ~1 s 20 dk (triyajın tahmini 2 saat — tahmin gerçeğe yakın, kalibrasyon için not).
**Durum:** BİTTİ (paketin tamamı uygulandı; kapı çıktısında P-45 DIŞI iki kırmızı var, § 7'de gerçek çıktısıyla).

---

## 1. Ölçülen kök neden (paket öncesi gerçek durum)

`KarZararServisi.Olc` satır projeksiyonu `h.Maden` kolonunu **hiç okumuyordu**:

```csharp
.Select(h => new { h.Id, h.Tur, h.TlTutari, h.HasGrami, h.KurSnapshotId })   // Maden YOK
...
var defterGram = satislar.Sum(h => Math.Abs(h.HasGrami)) - iadeler.Sum(h => Math.Abs(h.HasGrami));
```

Sonuç: 22 ayar altının has gramı ile 925 gümüşün has gramı **aynı torbada** toplanıyordu.
Canlı veritabanı ölçümü (`kuyumcu_net`, 2026-09-23):

| Kiracı | Maden | Tür | Satır | Σ\|has\| |
|---|---|---|---|---|
| QA-TEST-FRONTEND | ALTIN | SATIS | 4 | 46,6836 |
| QA-TEST-FRONTEND | GUMUS | SATIS | 2 | 17,4825 |

→ ekranın bastığı birleşik rakam **64,1661 g**, hiçbir şeyin karşılığı değildir.

**SAPMA (md.1):** triyajın yazdığı `44,4154 g = 32,7604 ALTIN + 11,6550 GÜMÜŞ` rakamı bugünkü
veriyle **doğrulanamadı**; aynı fikstürde bugünkü değerler 64,1661 = 46,6836 + 17,4825'tir
(fikstüre triyajdan sonra satış eklenmiş). Hata deseni birebir aynıdır, yalnız rakam tazelendi.
Rapora triyajın rakamı değil, **bugün ölçülen** rakam yazıldı.

## 2. Dosya ayak izi doğrulaması (`test -e`)

| Spesifikasyondaki dosya | Durum |
|---|---|
| `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` | VAR |
| `dotnet/Kuyumcu.Web/Pages/KarZarar.razor` | VAR |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` | VAR |
| `dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs` | VAR |
| `dotnet/Kuyumcu.Tests/KarZararIadeTestleri.cs` | VAR |
| `dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs` (yardımcılar) | VAR — `HareketToplami.MadenDagilimi/AltinHas` hazır, yeniden yazılmadı |

Satır numaraları kaymıştı (`defterGram` 280 değil **299**'du); koda bakılarak çalışıldı.

## 3. Yapılanlar

### Sunucu — `KarZararServisi.cs`
- Projeksiyona `h.Maden` eklendi.
- Defter gramı artık **maden bazında** kırılıyor. İkinci bir gruplama YAZILMADI: satış satırı
  `+|has|`, iade satırı `−|has|` ile `HareketKirilimi`'ne konup mevcut
  `HareketToplami.MadenDagilimi` / `AltinHas` yardımcılarından geçiriliyor (aylık özetle **aynı** kod yolu).
- `DonemOlcumu`'ye `HasDefterMadenDagilimi` eklendi; `HasilatHasDefterGram` **kaldırılmadı**, artık
  YALNIZ ALTIN taşıyor.
- Yanıta eklenenler: `altinHasilatHasDefterGram` (aynı değer, adı dürüst), `hasDefterMadenDagilimi`,
  `madenUyarisi` (yalnız >1 maden varsa). `hasilatHasDefterGram` **geriye dönük şekil sözleşmesi**
  olarak duruyor. `hasTanimNotu` metnine "defter gramı yalnız ALTIN taşır" cümlesi eklendi.

### `RaporOrtak.cs` + `RaporModulu.cs` (UX-S31 tek kaynak)
- `HareketToplami.MadenUyarisi(dagilim, alanAdi)` eklendi; `RaporModulu`'nun **özel kopyası** bu
  yardımcıya delege edildi (metin tek kaynakta, ikinci kopya doğmadı). Uyarı metninin kendisi değişmedi.

### Ekran — `KarZarar.razor` + `Servisler/KarZararModelleri.cs`
- Tek amber "Defter has gramı" kartı **kaldırıldı**; yerine **maden başına ayrı kart**
  ("Defter has gramı (Altın)", "Defter has gramı (Gümüş)") geldi. Ekranda birleşik rakam YOK.
- Sunucu kırılım döndürmezse (eski yanıt / satış yok) tek kart çizilir ve o kart da **ALTIN** etiketlidir —
  etiketsiz birleşik sayı hiçbir durumda basılmaz.
- >1 maden varsa `#kz-maden-uyari` kutusu çıkar (metin sözlükten, ham alan adı ekrana basılmaz — UX-S31).
- Yeni istemci modeli `MadenHasGrami` **sayfaya değil** `Servisler/KarZararModelleri.cs`'e yazıldı
  (denetim.sh SÖZLEŞME-1).
- Sahte API (`KarZararEkranTestleri`) yeni uçlara **gerçekçi** yanıt verecek şekilde güncellendi
  (`hasDefterMadenDagilimi` 2 maden + `madenUyarisi`).

### Sözlükler (TR + EN)
`HasDefterGramMaden` ("Defter has gramı ({0})" / "Ledger fine grams ({0})") ve `MadenToplanmaz`
(uyarı metni) eklendi. Çıplak metin yok; eksik anahtar derleme hatası verir (required init).

## 4. Migration
**Gerekmedi.** `hareketler.maden` kolonu zaten vardı (`Entities.cs`, migration `HareketMaden`);
paket yalnız okuma yolunu düzeltiyor. Yeni varlık/kayıt türü doğmadı.

## 5. DoD sözleşmesi (Bertan koşulu)
P-45 **hiçbir yeni varlık/kayıt türü** yaratmaz — yalnız var olan `hareketler` satırlarının
**okunma biçimini** düzeltir. Bu yüzden yeni PATCH/DELETE ucu doğmadı.

| Varlık | Düzenlenebilir mi | Nasıl / neden değil |
|---|---|---|
| `hareketler.maden` | Dolaylı | Ürünün ayarından türer; ürün kartından ayar değişince yeni hareketler yeni madenle doğar. Geçmiş defter satırı DEĞİŞMEZ (append-only). |
| Kâr-zarar raporu | Salt okunur | Sayfada zaten `DegismezIpucu Varlik="KarZarar"` var: rakamlar türetilir, düzeltme yolu ters hareket/storno'dur (48-A). |

Ekranda yeni düğme açılmadı → "tıklanınca hiçbir şey olmayan düğme" riski yok (DoD EK 1).
Yıkıcı eylem eklenmedi. Yeni ModalPencere düzenleme kullanımı **0** (DoD EK 1/2).

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş):** `Kuyumcu.Web/Pages/KarZarar.razor`.
(Spesifikasyon bu sayfanın P-64 ve P-62 ile çakıştığını yazıyor — ikinci dokunuş onların kapı kaydına gerekçe ister.)

## 6. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Önce (kapı kaydı, 2026-09-22) | 3869 | 3869 | 0 | 0 |
| `--filter P45_` | 5 | **5** | 0 | 0 |
| `KarZarar\|Rapor\|Maden` regresyon | 196 | **196** | 0 | 0 |
| Tam süit (trx) | **3990** | 3988 | **2** | 0 |

**Yeni test sayısı: 5.**
`P45_01_Defter_has_grami_altin_ve_gumusu_TOPLAMAZ` · `P45_02_Altin_defter_grami_aylik_ozetle_BIREBIR_ayni` ·
`P45_03_Iade_maden_bazinda_dusulur` · `P45_05_Tek_maden_varsa_uyari_cikmaz` (DB, `KarZararIadeTestleri.cs`) ·
`P45_04_Ekran_maden_basina_ayri_kart_cizer` (bUnit, `KarZararEkranTestleri.cs`).

### Tam süitteki 2 kırmızı — İKİSİ DE P-45 DIŞI (md.1: gizlenmedi)

| Test | Gerçek hata | Tanı |
|---|---|---|
| `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` | `WaitForFailedException … Check count: 1, Component render count: 9` | **KARARSIZ**: yalnız paralel tam süitte düştü, **tek başına koşturulunca YEŞİL** (kanıt: izole koşum 2 testten 1'i geçti, bu test geçti). P-45 dosyalarına dokunmuyor. |
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | `Assert.Contains Failure … Not found: "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı"` | **BAŞKA PAKETİN AÇIK KIRMIZISI**: test `denetim.sh` kaynağında henüz yazılmamış bir dizgeyi arıyor (`grep -c` = 0). Test dosyası da betik de P-45 ayak izinde değil; test başlığı "P-30b doğrulayıcı, ÖLÇÜLDÜ 2026-09-23" diyor — aynı ağaçta paralel çalışan pakete ait. |

## 7. denetim.sh

`bash dotnet/denetim.sh` → **rc=1, "DENETİM BAŞARISIZ"**. Tek ihlal satırı:

```
✗ P-02: demo provası rota tek kaynak kapısı (D-c) demo-tohumla.js'te @page doğrulaması YOK
```

**P-45 ile ilgisi yok:** ihlal `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` dosyasına
ait; o dosyanın değişim zamanı **2026-09-23 16:31** — yani bu oturum sürerken başka bir ajan
tarafından değiştirildi. P-45'in dokunduğu hiçbir dosya bu kapıda sayılmıyor; P-45'e ait tüm
sayaçlar (SÖZLEŞME-1 istemci modeli, ModalPencere, sabit renk, satır içi style) **temiz**.
**DENETİM TEMİZ beyanı VERİLEMEZ** — bu, P-45'in değil o paketin kapatacağı bir kalemdir.

## 8. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| **M45-1** | `defterGram`'ı eski `satislar.Sum(\|HasGrami\|) − iadeler.Sum(\|HasGrami\|)` toplamına döndür | `P45_01`, **`P45_02`**, `P45_03` | `cmp` BİREBİR AYNI |
| **M45-2a** | Yanıttan `hasDefterMadenDagilimi` alanını çıkar | `P45_01`, `P45_03`, `P45_05` | `cmp` BİREBİR AYNI |
| **M45-2b** | Ekranda maden döngüsünü kaldır, tek birleşik kart bas | **`P45_04`** | `cmp` BİREBİR AYNI |
| **M45-3** | İade düşümünü maden ayrımı olmadan (hepsi ALTIN) yap | **`P45_03`** | `cmp` BİREBİR AYNI |

**SAPMA (md.1):** spesifikasyon M45-2 için "`hasDefterMadenDagilimi` alanını çıkar → **P45_04** KIRMIZI"
diyordu. Ölçüldü: **olmuyor** — `P45_04` bUnit testidir ve sahte API'nin **sabit JSON**'unu okur,
sunucu yanıtına bağlı değildir. Sunucu mutasyonu 01/03/05'i kırar (M45-2a). Ekran tarafının gerçekten
sınandığını kanıtlamak için mutasyon **ikiye bölündü** ve ekran mutasyonu (M45-2b) eklendi;
`P45_04` orada kırmızıya döndü. Test açığı yok.

Son mutasyondan sonra: `cmp` ile iki dosyanın da birebir geri döndüğü kanıtlandı, ardından
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.

## 9. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Hata**. Son koşum **0 Uyarı**.
Ara koşumlarda görülen 2 uyarı (`CS8604`, `Kuyumcu.Tests/OlcumKapisiTestleri.cs:525`) **P-45'e ait değildir**
— aynı ağaçta paralel çalışan paketin dosyasıdır; izinsiz düzeltilmedi.

## 10. Canlı doğrulama (localhost:5198 · `kuyumcu_net`)

Test kiracısı **`PAKET45-TEST`** (`01M377A7V1MTW0A5VMCNGMEYKH`) — davet satırı elle eklenip
kayıt ucundan açıldı, iş sonunda tamamen silindi. **5199 portuna dokunulmadı** (koşum boyunca 2 dinleyici ayakta).
Veri: 3 adet K22 altın (3 × 9,16 = 27,48 g has) + 1 adet 925 gümüş (9,25 g has), ikisi de NAKİT satış.

| # | İstek | Beklenen | Gerçek | Yanıt (özet) |
|---|---|---|---|---|
| 1 | `POST /api/v1/urunler` K22 | 200 | **200** | `maden: "ALTIN"`, `hasGrami: 9.16` |
| 2 | `POST /api/v1/urunler` G925 | 200 | **200** | `maden: "GUMUS"`, `hasGrami: 9.25` |
| 3 | `POST /islemler/alis` ×2 | 200 | **200** | stok girdi |
| 4 | `POST /islemler/satis` altın 3 ad. | 200 | **200** | `tlTutari: 4500.00` |
| 5 | `POST /islemler/satis` gümüş 1 ad. | 200 | **200** | `tlTutari: 200.00` |
| 6 | `GET /raporlar/kar-zarar?baslangic=2026-09-01&bitis=2026-09-30` | 200 + maden kırılımı | **200** | `hasDefterMadenDagilimi = [{ALTIN, 27.48, 1}, {GUMUS, 9.25, 1}]` · `altinHasilatHasDefterGram = 27.48` · `hasilatHasDefterGram = 27.48` · `madenUyarisi` DOLU |
| 7 | Birleşik rakam taraması | hiçbir alanda geçmez | **geçmiyor** | `"36.73" in yanıt → False` (27,48 + 9,25 = 36,73) |
| 8 | `GET /raporlar/aylik?yil=2026&ay=9` | ALTIN satırı eşit | **200** | `satis.madenDagilimi` ALTIN `toplamHas = 27.48` → **FARK = 0,0** (tolerans 0,000001) |
| 9 | Tek maden hâli | uyarı çıkmaz | **regresyon testi P45_05** ile sabitlendi | `madenUyarisi = null`, dağılım 1 satır |

**SQL kanıtı (canlı kiracı, silinmeden önce):**

| Sorgu | Sonuç |
|---|---|
| `SELECT maden,tur,COUNT(*),ROUND(SUM(ABS(has_grami)),4) FROM hareketler WHERE tenant_id=…` | `ALTIN/ALIS 1 27.4800` · `GUMUS/ALIS 1 9.2500` · `ALTIN/SATIS 1 27.4800` · `GUMUS/SATIS 1 9.2500` |

**Temizlik:**
- API PID durduruldu · `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**; 5199 hâlâ 2 dinleyiciyle ayakta.
- `tenant_id` kolonu olan **70 tablonun tamamından** kiracı satırları silindi; doğrulama sorgusu
  (`COUNT(*) … HAVING n>0`) → **hiç satır dönmedi**. Eklenen `kiraci_davetleri` satırı da silindi.
- `SELECT COUNT(*) FROM tenants` → **başlangıç 9 → son 9**. `QA-TEST-*` fikstürlerine **dokunulmadı**
  (yalnız okundu).

## 11. Kabul koşulları karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| 3 altın + 1 gümüş satışında `hasDefterMadenDagilimi` maden bazında döner, birleşik rakam hiçbir alanda geçmez | ✅ | Canlı § 10 satır 6-7; `P45_01` (`Assert.DoesNotContain(birleşik)`) |
| `altinHasilatHasDefterGram` = `/raporlar/aylik` `satis.madenDagilimi` ALTIN `toplamHas` (≤0,000001 g) | ✅ | Canlı § 10 satır 8 (FARK 0,0); `P45_02` |
| Ekranda her maden ayrı değer; tek birleşik has gramı rakamı YOK | ✅ | `P45_04` (iki etiket + `DoesNotContain(">Defter has gramı<")` + toplam 36,73 yok) |
| Birden fazla maden varsa ekranda uyarı metni görünür | ✅ | `P45_04` `#kz-maden-uyari` → "toplanmaz" |
| `dotnet test` 0 başarısız | ⚠ | P-45 kapsamında **0**; tam süitte 2 kırmızı, **ikisi de P-45 dışı** (§ 6) |
| `denetim.sh` TEMİZ | ⚠ | **TEMİZ DEĞİL**; tek ihlal P-02 demo betiği, P-45 dışı (§ 7) |

## 12. Değişen dosyalar
```
dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs
dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs
dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Pages/KarZarar.razor
dotnet/Kuyumcu.Web/Servisler/KarZararModelleri.cs
dotnet/Kuyumcu.Tests/KarZararIadeTestleri.cs
dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs
```
(`git status --porcelain` bu ağaçta tek "first commit" yüzünden dosyaların çoğunu `??`/`M` gösteriyor;
yukarıdaki liste bu pakette **gerçekten düzenlenen** dosyalardır. COMMIT YAPILMADI.)

## 13. Açık kalanlar
1. **Tam süitteki 2 kırmızı ve denetim.sh ihlali P-45 dışıdır** — sahibi paketlerin kapatması gerekir
   (orkestratör kapısı bunu P-45'e yazmamalı).
2. `Kuyumcu.Tests/OlcumKapisiTestleri.cs:525` iki CS8604 uyarısı — izinsiz düzeltilmedi.
3. Kâr-zarar ekranının **diğer** has kartları (`hasilatHas`, `maliyetHas`, `karHas`) TL'nin has
   karşılığıdır ve kur üzerinden **altın** cinsindendir; gümüş satışının TL'si de altın kuruyla
   has'a çevriliyor. Bu P-45'in kapsamı DEĞİLDİ (kapsam: defter gramı) ama **ölçülmüş bir açıktır**:
   gümüş ağırlıklı dükkânda `hasilatHas` bacağı yanıltır. Ayrı paket önerilir.

## 14. Kurucuya / mali müşavire not
- Paket **mevzuat hükmü içermiyor**; yapılandırmaya yazılan yeni bir varsayım yok.
- Defterdeki `maden` kolonunun geçmiş satırları, migration `HareketMaden` sırasında **ürünün
  bugünkü ayarından** türetilmişti (Entities.cs'te yazılı varsayım). Ürünün ayarı satıştan sonra
  değiştiyse geçmiş satır yanlış madenle etiketli olabilir — P-45 bu varsayımı **değiştirmez**,
  yalnız görünür kılar. Geçmiş dönem raporu resmî bir yere verilecekse bu kalem teyit edilmeli.
