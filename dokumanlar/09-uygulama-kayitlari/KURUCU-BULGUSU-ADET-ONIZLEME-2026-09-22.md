# KURUCU BULGUSU — "adet kısmında ne yazarsam altta da o görünüyor"

**Tarih:** 2026-09-22 · **Rol:** Ertuğ (03 Frontend) · **Sunucu:** 5198 (kurucunun 5199'una DOKUNULMADI)

---

## 0. ÖNCE DÜRÜSTLÜK: bu rapor P-23 raporu DEĞİLDİR (Anayasa md.1)

Bu oturuma iki iş geldi:

| Kaynak | İş |
|---|---|
| **Kurucu (gerçek kullanıcı sesi)** | *"adet kısmında ne yazarsam altta da o görünüyor"* |
| Workflow betiği (hesaplanmış görev) | PAKET P-23 — ödeme kalemi tablosu + vezne fişinin genelleştirilmesi (600 ajan-dk, "EN YÜKSEK risk") |

Harness'ın kendi kuralı: *"Where the computed task conflicts with this request, this request wins."*
Kurucunun cümlesi somut bir **hata ihbarıdır** ve önceliklidir. Bu oturumda **o hata bulundu, kanıtlandı, düzeltildi ve kapatıldı.**

**P-23'e HİÇ BAŞLANMADI.** `odeme_kalemleri` tablosu yok, migration yok, geri doldurma yok, storno entegrasyonu yok, R-17/R-18 AÇIK. `PAKET-P-23-uygulama-raporu.md` **yazılmadı** — çünkü yapılmayan iş için rapor yazmak md.1 ihlalidir. P-23 sonraki dalgaya **devredildi**.

---

## 1. BULGU — kusur nedir

`/islemler` ekranında **Adet** kutusunun hemen ALTINDA, kullanıcının yazdığı sayıyı tekrar eden bir önizleme satırı (`span#islem-adet-onizleme`) beliriyordu.

**5198'de ÖLÇÜLEN gerçek çıktı (düzeltme ÖNCESİ):**

| Kutuya yazılan | Altta görünen (`span.alan-onizleme`) |
|---|---|
| `1` | *(yok — doğru)* |
| `1,0` | **`1`** |
| `15,20` | **`15.2`** |
| `2,50` | **`2.5`** |
| `007` | **`7`** |

Yani kurucunun cümlesi birebir doğru: **ne yazarsan altta o görünüyor** — üstelik Türkçe ekranda **NOKTALI** (`15.2`), kullanıcının yazdığından daha kötü bir yazımla.

## 2. KÖK NEDEN — neden P-39 bunu "kapattı" sanmıştı

Kusuru önlemesi gereken kural zaten vardı (P-39 · K7, `TASARIM-DENETIMI-2026-09-21 § 5.1`): *"önizleme yalnız BİLGİ EKLİYORSA gösterilir."* Ama **ölçüt yanlış seçilmişti** — `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs`:

```csharp
var girilen = Rakamlar(ham);                 // "15,20" -> "1520"
var bicimli = Rakamlar(OnizlemeBicimi);      // "15.2"  -> "152"
return !bicimli.Contains(girilen, ...);      // "152" ⊅ "1520"  ->  "bilgi eklendi" (YANLIŞ)
```

Ölçüt **rakam dizisi** karşılaştırmasıydı. Sondaki ya da baştaki sıfır rakam dizisini bozduğu için (`"1520"` ⊄ `"152"`) kural "bu önizleme yeni bir şey söylüyor" sanıp mükerrer satırı yeniden çiziyordu. **Yazım farkı bilgi değildir.**

### Neden ne test ne denetim yakaladı (asıl ders)

- **Birim testleri** bileşeni `Deger` parametresiyle besliyordu. O yolda `ham`, `OnParametersSet` içinde makine biçiminde doğar (`"15.2"`) — **sondaki sıfır hiç oluşmaz**, kusur hiç uyanmaz. Kusuru doğuran tek yol **kullanıcının kutuya yazması**dır; hiçbir test yazmıyordu.
- **`denetim.sh` · `ONIZLEME_MUKERRER`** sayacı "her `alan-onizleme` `OnizlemeGorunur` kapısının arkasında mı" diye sorar. Kapının **içindeki ölçütü göremez**. Sayaç YEŞİLKEN kusur canlıda duruyordu.

## 3. DÜZELTME

`SayisalAlan.BicimBilgiEkliyor` ölçütü rakam dizisinden **sayısal değere** taşındı:

```csharp
if (Deger is null) return false;
if (OnizlemeBicimi.Length == 0) return false;
// Metin sayıya çevrilemiyorsa (kutuda çöp var, Deger son geçerli değeri
// tutuyor) sistemin hâlâ bildiği sayı BİLGİDİR: gösterilir.
return Ondalik(ham) != Deger;
```

`decimal` karşılaştırması **ölçeği yok sayar** (`15,20 == 15,2`), dolayısıyla yazım oyunlarına kapalıdır. Artık soru doğru soruluyor: *kutudaki metin bu sayıyı zaten söylüyor mu?* Söylüyorsa önizleme çizilmez.
Gerçekten yeni olan bilgiyi (Gram alanındaki **has karşılığı**) ayrı kapı olan `OnizlemeEkBilgiVar` ayakta tutar — o davranış **değişmedi**.

Kullanılmayan kalan `Rakamlar()` yardımcısı kaldırıldı (ölü kod).

### Korunan davranış (md.1)
Kutuya sayıya çevrilemeyen metin yazılırsa (`"1,2,3"`) hata bandı çıkar **ve** sistemin hâlâ tuttuğu son geçerli değer önizlemede gösterilir — bilinmeyen gizlenmez.

## 4. TESTLER — kusuru yeniden üreten

`Kuyumcu.Tests/P39TasarimKokleriTestleri.cs` içine **7 yeni test** (hepsi kullanıcı **yazarak**, parametreyle değil):

| Test | Ne sınar |
|---|---|
| `K7_Yazim_farki_BILGI_DEGILDIR_onizleme_yine_CIZILMEZ` (5 veri: `1,0` · `15,20` · `2,50` · `007` · `1.0`) | 5198'de ölçülen dört satırın birebir kendisi + nokta ile yazan kullanıcı |
| `K7_Yazarken_de_gecerli_ADET_onizlemesi_hic_dogmaz` | `1 → 15 → 15, → 15,2 → 15,20` ara adımlarının **hiçbirinde** önizleme parlamaz |
| `K7_Sayiya_cevrilemeyen_metinde_sistemin_bildigi_deger_GOSTERILIR` | md.1 güvencesi |

**Test sayısı: 3754 → 3761** (tam süit, 0 başarısız, 0 atlanan, 1 dk 11 sn).

## 5. MUTASYON KANITI

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M1 | `BicimBilgiEkliyor` ölçütü eski rakam-dizisi karşılaştırmasına döndürüldü (`perl -0777 -pi -e`) | `K7_Yazim_farki_BILGI_DEGILDIR_onizleme_yine_CIZILMEZ` ×5 veri (`1,0` · `15,20` · `2,50` · `007` · `1.0`) + `K7_Yazarken_de_gecerli_ADET_onizlemesi_hic_dogmaz` → **6 başarısız / 55** | `cmp` **birebir aynı** |
| M2 | aynı mutasyon, bu kez **denetim.sh sayacına** karşı | `ONIZLEME_OLCUTU_RAKAM_DIZISI = 3` → **DENETİM BAŞARISIZ** | `cmp` **birebir aynı** |

Mutasyon sonrası TEMİZ `dotnet build`: **0 uyarı / 0 hata**.

## 6. MEKANİZMA — kusur sınıfı geri gelemesin

`dotnet/denetim.sh` P-39 bloğuna **yeni cırcır sayacı** eklendi (ölçüm komutu betiğe yorum olarak yazıldı, taban **taze ölçüldü**, belgeden kopyalanmadı):

```
P-39: ONIZLEME_OLCUTU_RAKAM_DIZISI = 0   (ölçüt sayısal değer)
```

Ölçüt yeniden rakam dizisine dönerse ya da `return Ondalik(ham) != Deger;` satırı kaybolursa denetim **fail-closed** kırmızıya döner. Mevcut hiçbir tavan artırılmadı.

## 7. CANLI DOĞRULAMA (5198 — düzeltme SONRASI)

Giriş: mevcut `QA-TEST-FRONTEND` fikstür kiracısı ile `POST /api/v1/auth/giris` → **HTTP 200**. `GET /` → **HTTP 200**.

| Adım (`/islemler`, `#islem-adet`) | Beklenen | Gerçek |
|---|---|---|
| `1` yaz | önizleme yok | **yok** ✓ |
| `1,0` yaz | önizleme yok | **yok** ✓ |
| `15,20` yaz | önizleme yok | **yok** ✓ |
| `007` yaz | önizleme yok | **yok** ✓ |
| `2,50` yaz | önizleme yok | **yok** ✓ |
| `1,2,3` yaz (çöp) | hata bandı + son geçerli değer | `aria-describedby="alan-hata-islem-adet islem-adet-onizleme"`, önizleme `2.5` ✓ |

`aria-describedby` zinciri de temizlendi: ekran okuyucu kullanıcısına aynı sayı iki kez okunmuyor.

**Veritabanı:** `tenants = 9`, `__EFMigrationsHistory = 61` — **başlangıçtaki değerler**. Bu oturumda kiracı **açılmadı** (mevcut QA fikstürü yeniden kullanıldı, verisine dokunulmadı), silinecek bir şey yok. Migration **gerekmedi** (yalnız istemci tarafı ölçüt değişikliği).

**Sunucu temizliği:** 5198 durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**. 5199 hâlâ ayakta ve **hiç dokunulmadı**.

## 8. DEĞİŞEN DOSYALAR

```
dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs      (ölçüt + gerekçe yorumu, ölü kod temizliği)
dotnet/Kuyumcu.Tests/P39TasarimKokleriTestleri.cs      (+7 test)
dotnet/denetim.sh                                       (+1 cırcır sayacı)
```

Başka hiçbir dosyaya dokunulmadı. **Commit yok**, git komutu çalıştırılmadı (yalnız `git status --porcelain`).

---

## 9. AÇIK KALANLAR ve KURUCUYA NOTLAR

1. **P-23 hiç başlamadı.** R-17 ve R-18 AÇIK. Sonraki dalgaya devredildi.

2. **Sayı yazımı nokta mı virgül mü? — KURUCU KARARI GEREKİYOR.**
   `Bicim.Gram` (`"15.200 g"`), `Bicim.Has` (`"9.1600 has"`) ve `Bicim.Miktar` **InvariantCulture** kullanıyor, yani ondalık ayırıcı **nokta**. Buna karşılık `Bicim.Tl` **tr-TR** kullanıyor, yani **virgül** (`₺1.234,50`). Aynı ekranda **tutar virgüllü, gram noktalı** yazılıyor. Kurucunun bu bulguda gördüğü `15.2` de bundan.
   Bu, ev geneline yayılmış **bilinçli bir sözleşme** gibi duruyor ve yüzlerce test buna bağlı — bu oturumda **tek taraflı değiştirmedim** (izinsiz değişiklik yok). **Karar sizin:** gram/has da Türkçe virgülle mi yazılsın? Evetse bu ayrı bir paket olur (tek nokta: `Bicim.cs`, ardından test gerilemesi).

3. **Ölçüm yöntemi dersi (ekibe):** birim testi bileşeni **parametreyle** beslediğinde kullanıcı girdisinden doğan kusurları göremez. Girdi ailesi testlerinin bundan sonra **`.Input(...)` ile yazması** gerekiyor; bu oturumda yalnız K7 için yapıldı, ailenin kalanı taranmadı — **açık kalem**.

4. **`docker-compose.yml` içindeki `MYSQL_ROOT_PASSWORD` çalışan konteynere UYMUYOR.**
   Runbook'taki `docker exec kuyumcu-mysql mysql -uroot -p"$P"` reçetesi `ERROR 1045 Access denied` veriyor. DB sayımları `appsettings.Development.json` içindeki uygulama kullanıcısıyla alındı. Sır rotasyonu (K-5) yapılmış ama compose dosyası/runbook güncellenmemiş görünüyor — **düzeltilmesi gereken belge borcu**, sır değeri hiçbir yere yazılmadı.

5. **Aynı kusur sınıfı başka ekranlarda taranmadı.** `/islemler`, `/urunler/etiketler` ve `/sayim` canlıda tarandı (temiz). Modal/çekmece arkasındaki Adet alanları (`/vezne`, `/siparisler`, `/urunler` düzenleme, `/urunler/agac`, `/pos`) **tek tek açılıp taranmadı** — kök neden ortak bileşende olduğu için düzeltme hepsini kapsar, ama **canlı kanıtı yalnız üç ekran için var**. Dürüst kayıt budur.
