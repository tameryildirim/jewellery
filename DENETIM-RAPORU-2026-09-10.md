# Kuyumcu ERP — Araştırma Karşılaştırmalı Denetim Raporu

**Tarih:** 2026-09-10 · **Denetleyen:** Tamer Bey (ByGatech A.Ş.) · **Kapsam:** `projeler/jewellery` (.NET 8 API + Blazor WASM)
**Kıyas ölçütü:** `projeler/arastirmalar/KUYUMCU-YAZILIM-FIRSAT-2026-09-07.md` (pazar + 2026 mevzuat araştırması)

> **Anayasa Madde 1 uyarınca:** Bu raporda "var/yok/hatalı" denen her kalem ya çalışan sistem üzerinde ölçüldü ya da kod satırı gösterildi. Ölçülemeyenler açıkça "denenmedi" olarak işaretlendi.
> **Anayasa Madde 3 uyarınca:** Bu denetimde **hiçbir kod veya mevcut dosya değiştirilmedi.** Yalnız bu rapor dosyası eklendi.

---

## 0. Yönetici özeti

Ürünün **çekirdeği doğru yerden tutulmuş.** Araştırmanın "kuyumcunun vazgeçilmez programı" dediği şey — has-cari, milyem, paralel defter — bu kodda gerçekten var ve rakiplerin çoğunda olmayan **kuyum KDV'si** bile doğru hesaplanıyor. Yani temel atılmış.

Ancak üç ayrı kategoride sorun var:

| Kategori | Adet | En ağırı |
|---|---|---|
| 🔴 Kritik hata (kanıtlı) | 6 | Kiracılar arası para yazma — bir işletme diğerinin kasasına para yazabiliyor |
| 🟠 Araştırmaya göre eksik modül | 11 | MASAK / KMTS / e-Fatura uyum katmanı hiç yok — ürün farkımızın tamamı |
| 🟡 Teknik borç | 7 | Şema oluşturma yok, test yok |

**Tek cümlelik hüküm:** Bu kod **satılabilir bir ürün değil, sağlam bir prototip.** Muhasebe çekirdeğindeki 6 kritik hata düzeltilmeden tek kuyumcuya kurulamaz; uyum katmanı eklenmeden de rakipten (M&L) ayrışamaz.

---

## 1. Araştırmayla uyumlu güçlü yönler (korunacak)

Araştırma "sektör az sayıda programa mahkûm, çünkü kimse gram/milyem mantığını yapmıyor" demişti. Bu kod o mantığı yapıyor:

| Araştırma bulgusu | Kodda durumu | Kanıt |
|---|---|---|
| Has gram çekirdeği zorunlu | ✅ Var, `(gram − taş) × milyem/1000` | `Kuyumcu.Core/Domain/Ayarlar.cs` |
| **Gerçek milyem** (imalattaki fiili saflık) | ✅ Var — rakiplerde nadir | `UrunModulu.cs:1300` |
| Paralel defter (TL + has aynı satırda) | ✅ Var, append-only | `Data/Entities.cs` `hareketler` |
| Her işlem anındaki kura bağlı | ✅ `kurSnapshotId` zorunlu | tüm işlem uçları |
| Cari türleri: müşteri/tedarikçi/**atölye/sarraf** | ✅ Var — sektör tipolojisine uygun | `Sabitler.CariTurleri` |
| Emanet (müşteri altını) has bazlı | ✅ Var | `EmanetModulu.cs` |
| Cari yaşlandırma (0-30/31-60/61-90/90+) | ✅ Var | `RaporModulu.cs:122` |
| Kredi limiti kontrolü | ✅ Var, aşımda reddediyor | `CariModulu.cs:966` |
| Barkodla POS araması | ✅ Var | `/urunler/barkod/{kod}` |
| **Kuyum KDV** (has bedeli matrah dışı) | ✅ Var ve **doğru hesaplıyor** | ölçüm: aşağıda |
| Çoklu şube + şubeler arası transfer | ✅ Var | `SatisModulu.Transfer` |
| Rol bazlı ekran yetki matrisi (tenant başına) | ✅ Var — rakiplerde yok | `YetkiModulu.cs` |
| Canlı kur push (yoklama değil) | ✅ SignalR | `KurHub.cs` |
| Bulut/çok kiracılı mimari | ✅ Var | `tenant_id` her tabloda |

**Kuyum KDV ölçümü (araştırmanın 2026 mevzuat maddesine birebir denk düşüyor):**
Test faturası `FTR-2026-000003` — toplam 385.676,63 TL, has bedeli 381.476,63 TL matrahtan düşülmüş, **KDV yalnız 4.200 TL işçilik üzerinden 700,00 TL** hesaplanmış. Doğru davranış. Bu, sektörde satış argümanı olacak kalitede.

---

## 2. 🔴 Kritik hatalar (hepsi çalışan sistemde kanıtlandı)

### K1 — Kiracılar arası para ve borç yazma (GÜVENLİK, en ağır bulgu)
Satış işleminde `kasaId` ve `cariId` **hangi işletmeye ait olduğu doğrulanmıyor.** Bir kuyumcu, başka bir kuyumcunun kasasına para ve carisine borç yazabiliyor.

**Kanıtlanan test:** İki ayrı işletme açıldı. B işletmesi, A işletmesinin kasa ve cari kimliğini kullanarak satış gönderdi:

| Hedef | Öncesi | İstek | Sonrası |
|---|---|---|---|
| A işletmesinin kasası | 0,00 TL | HTTP 200 | **99.999,00 TL** |
| A işletmesinin carisi | 0,00 TL | HTTP 200 | **77.777,00 TL** |

**Kök neden:** `Modules/Satis/SatisModulu.cs:588` ve `:559` — sorgularda `TenantId` filtresi yok:
```csharp
?? await db.Kasalar.FirstOrDefaultAsync(k => k.Id == kasaId)          // satır 588
?? await db.CariBakiyeler.FirstOrDefaultAsync(b => b.CariId == cariId) // satır 559
```
Mimari doküman "her servis sorgusu tenant ile filtrelenir" diyor; bu iki yerde filtrelenmiyor. **SaaS olarak yayına çıkarsa müşteri verileri birbirine karışır.** Aynı açık `Emanet` ve `Cari` modüllerinde de `cariId` doğrulanmadan bakiye satırına gidiyor.

### K2 — Alışta çifte kayıt: parayı ödüyor, borç yine duruyor
`SatisModulu.cs:265` ve `:269` — alışta **hem** tedarikçi carisi borçlandırılıyor **hem** kasadan para çıkıyor. Peşin ödenen alış, tedarikçiye borçlu görünmeye devam ediyor.

Sonuç: peşin çalışan kuyumcuda tedarikçi bakiyeleri sürekli şişer, mutabakat tutmaz.

### K3 — Hurda alışta ÜÇ kayıt (aynı işlem üç yerde)
`SatisModulu.cs:315` ve `:318` — hurda alışta: kasadan para çıkıyor **+** müşteri carisine borç yazılıyor **+** müşterinin has bakiyesi artıyor.

**Kanıtlanan ölçüm (Ali Veli Çelik ekstresi):** 7,9 g 22 ayar hurda alındı, 48.416,29 TL **nakit ödendi**. Buna rağmen:
- kasa −48.416,29 TL (para gitti, doğru)
- cari TL −48.416,29 (biz ona borçluyuz — **yanlış, ödedik**)
- cari has +7,2364 (onun altını bizde duruyor — **yanlış, satın aldık**)

Kuyumcunun en sık yaptığı ikinci işlem bu. Bu haliyle hurda alan kuyumcunun defteri bir günde bozulur.

### K4 — Has bakiyesinin işareti modüller arasında çelişiyor
Aynı `has_gram_bakiye` alanı iki zıt anlamda kullanılıyor:
- **Emanet** (`EmanetModulu.cs:17`): artı = *"bizde müşterinin altını var"* → **biz borçluyuz**
- **Veresiye** (`CariModulu.cs:349`): altın müşteriye verilmesine rağmen bakiye yine **artı** → burada artı = *müşteri borçlu* olmalı

İki modül aynı sayıyı ters yönde yorumluyor. Üstelik Cariler ekranı has gramın yanına Alacak/Borç etiketi koymuyor (TL'ye koyuyor), yani kullanıcı hangisi olduğunu göremiyor. **Ürünün çekirdek vaadi "has-cari" olduğu için bu, kozmetik değil temel bir tasarım hatası.**

### K5 — Günlük ciro saat 03:00'e kadar sıfır görünüyor
Panel yerel tarihi soruyor, API tarihi UTC kabul ediyor (`RaporModulu.cs:35`, `Panel.razor:137`). İstanbul UTC+3 olduğu için gece 00:00–03:00 arası panel bir sonraki günü sorar, kayıtlar hâlâ önceki UTC gününde durur.

**Kanıtlanan ölçüm (bu rapor yazılırken):**

| Sorulan tarih | Günlük satış | Hareket |
|---|---|---|
| 2026-09-10 (yerel "bugün") | 0,00 TL | 0 |
| 2026-09-09 (UTC) | 743.121,39 TL | 33 |

Gece mesaisi yapan kuyumcu (Kapalıçarşı'da yaygın) kendi cirosunu göremez.

### K6 — Kasa para birimi yok sayılıyor
`SatisModulu.cs:601` — kasa hareketine para birimi **sabit "TRY"** yazılıyor ve kasa bakiyesine TL tutarı, kasanın dövizi ne olursa olsun ekleniyor. Dolar kasasına satış yapılırsa TL tutarı dolar bakiyesine eklenir.
*(Kod okumasıyla kesin; demo verisini bozmamak için test edilmedi.)*

---

## 3. 🟠 Araştırmaya göre eksik modüller (ürün farkımızın durduğu yer)

Araştırmanın ana tezi şuydu: **2026 mevzuat dalgası kuyumcuyu program değiştirmeye zorluyor, rakip masaüstü programlar bu yükü taşımıyor — fark buradan gelir.** Kodda bu katmanın **tamamı yok.** Arama sonucu: `masak`, `kmts`, `karekod`, `e-fatura`, `ubl`, `gib`, `fire`, `ramat` kelimelerinin hiçbiri kod tabanında geçmiyor.

| # | Araştırma bulgusu | Kodda | Etki |
|---|---|---|---|
| E1 | **MASAK** — 185.000 TL üstü kimlik tespiti, işlem beyanı, 10 iş günü içinde şüpheli işlem bildirimi, yaptırım listesi kontrolü | ❌ Yok | **Ürün farkının %1 numarası.** İdari para cezası riski satışı kendiliğinden yapar |
| E2 | **KMTS** — Darphane karekodu, külçe/bar/basılı ürün tekil takibi | ❌ Yok (`barkod`/`rfid` alanı var, KMTS kodu yok) | Nisan 2026 sonrası zorunluluk |
| E3 | **e-Fatura / e-Arşiv** — 12.000 TL üstü satışta fiş yasak | ❌ Yok. Fatura yalnız iç kayıt; GİB'e gitmiyor, XML/UBL yok | Yasal zorunluluk; bu haliyle kuyumcu ikinci program kullanmak zorunda |
| E4 | **Fatura kalem satırı** yok — fatura tek toplam tutardan oluşuyor | ❌ Yok (`FaturaOlusturDto`: yalnız `toplam`) | e-Fatura'nın ön şartı; mal cinsi/miktar/birim fiyat zorunlu |
| E5 | **Satıştan fatura kesme** — satış ile fatura arasında bağ yok | ❌ Yok. `hareketler.fatura_no` alanı tanımlı ama **hiçbir yerde yazılmıyor** (ölü alan) | Kuyumcu satışı iki kez giriyor |
| E6 | **Atölye: fire / ramat / ayar düşürme** — araştırmada Pusula, Dezy, Kuvvem'in öne çıkardığı modül | ❌ Yok. `ATOLYE` cari türü var, iş emri/fire hesabı yok | İmalatçı ve atölyeli kuyumcuya satılamaz |
| E7 | **Vitrin fiyat panosu** (SarrafPano, KuyumcuPano, OMG) | ❌ Yok | Araştırmada "düşük maliyetli müşteri kazanım kancası" olarak işaretlendi |
| E8 | **Kur kaynağı seçimi** — Harem / Sağlamoğlu / Özbağ / Nadir arasından seçme + kuyumcunun kendi marjı | ❌ Yok. Tek kaynak (truncgil) ve **alış/satış marjı alanı yok** | Kuyumcu kendi fiyatını kuramıyor |
| E9 | **Kıymetli taş detayı** (karat, renk, berraklık, kesim, sertifika no) | ❌ Yok, yalnız `tas_agirlik` | Pırlanta satan mağazaya satılamaz |
| E10 | **Etiket/barkod basımı** ve sayım (RFID) oturumu | ❌ Yok. `rfid` alanı var, sayım/fark raporu yok | Araştırma: RFID sayım hatasını %95 azaltıyor |
| E11 | **Taksitli altın satışı / altın günü birikimi** (2026 mevzuatı) ve **mali müşavir / muhasebe aktarımı** (Milyem Plus'ın 360+ mali müşaviri) | ❌ Yok | İki ayrı satış kanalı kapalı |

**Ek olarak fatura numarası GİB formatına uymuyor:** üretilen numara `FTR-2026-000001` (`FaturaModulu.cs:196`). e-Fatura seri numarası 3 haneli alfanumerik + 4 haneli yıl + 9 haneli sıra olmalı (örn. `ABC2026000000001`). Ayrıca numara `CountAsync + 1` ile üretiliyor — iki kullanıcı aynı anda fatura kesince **aynı numarayı** alır (`FaturaModulu.cs:200`, `BankaModulu.cs:320`).

---

## 4. 🟡 Teknik borç

| # | Bulgu | Kanıt |
|---|---|---|
| T1 | **Veritabanı şeması oluşturan hiçbir şey yok** — migration ya da SQL dosyası yok. Bu proje bugün temiz bir makinede kurulamıyor; 16 tabloyu bu denetimde repo dışı bir yardımcıyla oluşturdum | `dotnet/` altında migration klasörü yok |
| T2 | **Tek test bile yok** — 12.587 satır kod, sıfır test | test projesi yok |
| T3 | **Sayım deftere yazmıyor** — `StokSayimGuncelle` stoğu üzerine yazıyor, `hareketler`e kayıt düşmüyor. `SAYIM_DUZELTME` hareket türü tanımlı ama hiç kullanılmıyor → append-only denetim izi kırılıyor | `UrunModulu.cs:297` |
| T4 | **Kim değiştirdi kaydı yok** — cari/ürün/kullanıcı güncellemelerinde denetim logu tutulmuyor. Ana defterde `kullanici_id` var, düzenlemelerde yok | modül servisleri |
| T5 | **Raporlar tüm veriyi belleğe çekiyor** — günlük özet, yaşlandırma ve stok değeri raporları filtresiz `ToListAsync()` yapıyor. Birkaç yıllık hareket birikince yavaşlar | `RaporModulu.cs:39, 126, 184` |
| T6 | **`tenant_id` üzerinde indeks yok** — yalnız 3 bileşik indeks tanımlı | `KuyumcuDbContext.OnModelCreating` |
| T7 | **`bin/obj` git'e commit'li** (1408 dosya), `.gitignore` yalnız Node dünyasını kapsıyor. Her derleme depoyu kirletiyor | `git ls-files` |

---

## 5. Belge–kod çelişkileri (yeni devralan yanılır)

| Belge iddiası | Gerçek |
|---|---|
| `docs/MIMARI.md` — NestJS + React + Expo monorepo, 15 tablo, pnpm | Repoda o kod **yok**; yalnız .NET 8 + Blazor var, 16 tablo |
| `docs/DEPLOY.md` — Node kurulumu, pm2, IIS ARR proxy, `apps/web/dist` | .NET tek süreç sunuyor; bu adımların çoğu geçersiz |
| `docs/KULLANIM_KILAVUZU.md` — mobil uygulama bölümü, `start.bat` port 3000 | Mobil uygulama yok; `start.bat` port 5000 |
| `docs/DOTNET.md` — *"kurSnapshotId artık tenant doğrulamalı (güvenlik artışı)"* | **Doğrulama kodda yok.** `KurSnapshots` tablosu işlem modüllerinde hiç sorgulanmıyor |
| `docs/A11Y_RAPORU.md` — React/Expo dosya ve satır numaraları | Blazor sürümünde birçoğu zaten düzeltilmiş; rapor eski yığına ait |
| `.claude/launch.json` — `pnpm --filter @kuyum/web dev`, port 3000 | Geçersiz |

---

## 6. Önerilen sıra (ekip oyuna sunulacak)

Araştırmadaki tezi koruyarak: **önce defteri doğru tut, sonra uyumla ayrış.**

**Faz 0 — Durdurucu (bunlar olmadan tek kuyumcuya kurulmaz)**
1. K1 kiracı sızıntısı (Mehmet — güvenlik vetosu alanı)
2. K2 + K3 çifte/üçlü kayıt (Taşkın)
3. K4 has işaret sözleşmesinin tek bir kurala oturtulması (Bertan — mimari kararı)
4. T1 şema/migration + T2 çekirdek muhasebe testleri (Aşkın — fail-closed kapı)
5. K5 saat dilimi, K6 döviz kasası

**Faz 1 — Ürün farkı (araştırmanın satış argümanı)**
6. E1 MASAK uyum motoru — 185.000 TL eşiği geçince kimlik + beyan formu, yaptırım listesi kontrolü, şüpheli işlem taslağı, saklama arşivi
7. E3 + E4 + E5 e-Fatura zinciri (kalem satırı → GİB özel entegratör ortağı)
8. E8 kur kaynağı seçimi + kuyumcunun kendi marjı

**Faz 2 — Pazar genişletme**
9. E6 atölye fire/ramat · E2 KMTS karekod · E7 vitrin panosu (kanca) · E9 taş detayı · E10 etiket/sayım · E11 taksit + mali müşavir aktarımı

**Faz 3 — Belge temizliği:** `docs/` klasörünün .NET gerçeğine göre yeniden yazılması.

> CLAUDE.md'deki zorunlu oy kuralı gereği bu yol haritası **uygulanmadan önce ekip toplanıp oylayacak**; Tamer Bey mühürleyecek. Kod yazımı için Kurucu onayı aranmıyor, ancak Kurucu bu raporu okuduktan sonra "başla" derse Faz 0'a girilir.

---

## 7. Dürüst kayıt — bu denetimin bıraktığı izler

Madde 1 gereği gizlenmeyen yan etkiler:

1. **Demo işletmesine test kaydı düştü.** K1'i kanıtlamak için `demo@bygatech.com` işletmesinde "Kurban Kasa" (99.999,00 TL) ve "Kurban Musteri" (77.777,00 TL) oluştu. Bunlar **gerçek olmayan, sızıntı testinden kalan** kayıtlardır. Silme komutları güvenlik kancasıyla engellendiği için **temizlenmedi.**
2. **Şema repo dışı bir yardımcıyla oluşturuldu** (`EnsureCreated`). Bu, migration'ın yerini tutmaz — T1 hâlâ açık.
3. **Derleme, git'te izlenen `obj/` dosyalarını değiştirdi** (T7). Geri alma komutu kancayla engellendi; depo bu haliyle kirli.
4. **Denenmeyenler:** K6 döviz kasası (kod okumasıyla kesin, testle denenmedi) · yük/performans ölçümü yapılmadı (T5 muhakemeye dayanıyor) · e-Fatura, MASAK ve KMTS için resmî entegrasyon şartları bu denetimde yeniden doğrulanmadı, 2026-09-07 araştırmasındaki hallerine dayanıldı.

---

## 8. Ölçüm ortamı (tekrarlanabilirlik)

- macOS, .NET SDK 8.0.128 (homebrew), MySQL 8.4 Docker konteyneri `kuyumcu-mysql`
- Uygulama: `http://localhost:5050` (port 5000 macOS AirPlay tarafından kullanılıyor; `--urls` ile geçildi, ayar dosyasına dokunulmadı)
- `dotnet build Kuyumcu.sln -c Release` → **0 hata, 0 uyarı**
- Örnek veri: 9 cari, 14 ürün, 3 personel, 2 şube, 33 hareket, 4 fatura — tamamı API üzerinden, koda dokunulmadan yüklendi
- Canlı kur çekimi çalıştı: has altın 6.827,20 TL/g (kaynak alanına `HAREM` yazılıyor — **oysa veri truncgil'den geliyor**, `KurModulu.cs:135`. Madde 1 açısından yanlış kaynak beyanı; düzeltilmeli)
