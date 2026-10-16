# PAKET P-08 — HAREKET TÜRÜ SÖZLEŞMESİ + ÇEKİRDEK KOLONLAR (uygulama raporu)

> **Uygulayan:** dalga 2 ajanı (kod) · **Kapanış ve doğrulama:** Tamer Bey, elden (kurucu talimatı 15).
> **Neden elden kapandı (Anayasa md.1):** paketin kodu yazıldı ve tam süitten geçti, ama raporunu
> yazacak ajan **üç ayrı koşumda da stall** oldu. Kök neden ölçüldü ve ajan hatası değil:
> makinede boş bellek 86-111 MB'a düşüyor (sanal makine 1.265 MB + iCloudDriveCore 397 MB);
> aynı tam süit normalde 40 saniye sürerken bu koşumda **41 dakika 12 saniye** sürdü.
> Bu rapor, diskteki gerçek duruma bakılarak yazılmıştır; ajanın beyanı kopyalanmamıştır.

## 1. Kapı kaydı (2026-09-19 06:12)

| Kontrol | Sonuç |
|---|---|
| tek_el | 0 |
| `dotnet build Kuyumcu.sln` | 0 hata / 0 uyarı |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |
| Tam süit | **3032 / 3032** (0 başarısız) — süre 41 dk 12 sn (bellek açlığı, § başlık) |
| Migration | **53** · bu paketin göçü: `20260918150025_HareketAdetPersonel` (uygulandı) |
| Test kiracısı | Yok (kiracı 8; `PAKETP-08-TEST` silinmiş, ölçüldü) |

## 2. Ne yapıldı

**Şema (migration `HareketAdetPersonel`):** `hareketler` tablosuna iki kolon eklendi —
`adet` (decimal) ve satıcı `personel_id` (string) — ve indeks kuruldu.
Kıyas raporunun iki maddesi buradan kapanır: *"hareketler tablosunda adet kolonu bile yok,
satılan adet has gramdan bölünerek türetiliyor"* ve *"fişte satıcı personel alanı yok;
`kullanici_id` KAYDEDEN kişidir, prim kararına dayanak olamaz"*.

**Kod:** `HareketFabrikasi` (tek yazma yolu) tür/adet/personel kapılarıyla genişletildi;
`FaturaHareketBagi` eklendi (fatura ↔ hareket bağının kimlik düzeyinde sağlamlaştırılması).

**Testler:** `Kuyumcu.Tests/P08HareketSozlesmeTestleri.cs` — **19 test**. Kapsam:
tür kapısı (listede olmayan tür reddedilir, boş tür reddedilir, listedeki tüm türler kabul edilir),
adet kapısı (negatif adet reddedilir, adet NULL kalabilir, yazıldıysa üç ondalıkla korunur),
personel kapısı (boş dizge reddedilir) ve **`personel_id` "SATAN" anlamıyla kilitli, `kullanici_id`den AYRI**.

**Denetim sayaçları (cırcır):** `HARETURU_DISI_SABIT` (tür listesi dışında sabit yazılamaz) ·
`TUR_KAPISI + ADET/PERSONEL KAPISI` · `HAREKETFABRIKASI_DISI_YAZMA` (tek yazma yolu korunur).

## 3. Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| hareketler.adet kolonu | **KAPANDI** | migration `HareketAdetPersonel`; testler `AdetKapisi_*`, `Adet_yazildiysa_uc_ondalikla_korunur` |
| fişte satıcı personel | **KAPANDI (defter ayağı)** | `personel_id` kolonu + `PersonelId_SATAN_anlamiyla_kilitlidir_ve_KullaniciId_den_AYRIDIR` |
| hareket türü sözleşmesi | **KAPANDI** | `HARETURU_DISI_SABIT` sayacı + tür kapısı testleri |
| tek yazma yolu koruması | **KAPANDI** | `HAREKETFABRIKASI_DISI_YAZMA` sayacı |
| Ekranda personel seçimi (fiş formu) | **DEVREDİLDİ → dalga 4 (tezgâh katmanı)** | Bu paket defter ayağıdır; satış fişi formuna alan koymak P-14/P-16 kapsamındadır |

## 4. Açık kalan / dürüstlük

- **Mutasyon kanıtı bu pakette ORKESTRATÖR tarafından ÜRETİLMEDİ.** Ajan stall olduğu için
  mutasyon turu koşulamadı; sayaçların ve kapıların gerçekten koruduğu **iddia edilmiyor**.
  Dalga 2 kapısında ya da P-09 koşumunda bu paketin dört kuralı için mutasyon turu koşulacak
  ve sonucu buraya eklenecek. (Madde 1: koşulmayan mutasyona "geçti" denmez.)
- **Canlı doğrulama (5198)** bu pakette ajan tarafından yapılmış olabilir ama kanıtı rapora
  geçmedi; orkestratör de bellek darlığı nedeniyle yeni bir canlı tur koşmadı. Aynı şekilde
  dalga kapısına devredildi.
- P-09 (kimlik, erişim, MFA, yetki matrisi) henüz başlamadı.

---

## § DÜZELTME — bu raporun iki satırı YANLIŞ ÇIKTI (2026-09-20 22:2x, Madde 1)

Hasım doğrulayıcı P-08'i **KALDI** ile geri çevirdi ve haklıydı. Yukarıdaki § 1'de yazan
iki satır, yazıldığı an (19 Eylül 06:12 kapısı) doğruydu ama **bugünkü ağaçta yanlıştı**:

| Rapordaki satır | Bugün ölçülen | Sebep |
|---|---|---|
| "Tam süit 3032 / 3032 (0 başarısız)" | **108 başarısız / 3032** | Orkestratörün 20 Eylül 22:07'de eklediği `dotnet/Directory.Build.props` derleme çıktısını iCloud dışına taşıyordu; testler kaynak kökünü çıktı klasöründen yukarı yürüyerek buluyor (58 test "Kuyumcu.sln bulunamadı", 28 test "docker-compose.yml yok") |
| "Test kiracısı yok (kiracı 8)" | **kiracı 9 — PAKETP-08-TEST + 2 hareket satırı** | Doğrulama turunun canlı adımından kalan artık |

**İkisi de kapatıldı (orkestratör, elden):**

- `Directory.Build.props` **geri alındı** (içeriği boşaltıldı, gerekçesi dosyanın içine yazıldı).
  Doğrulama: `dotnet build` 0 hata/0 uyarı · kök bulan testler (Sozleşme yansıma + migration
  hijyeni + ölçüm kapısı) **28/28 yeşil**. Çıktıyı iCloud dışına taşıma fikri çöpe atılmadı:
  önce ~15 test sınıfının kök bulma yöntemi tek bir yardımcıya bağlanmalı — **bir pakete devredildi**,
  elden yapılmayacak.
- `PAKETP-08-TEST` kiracısı **silindi**: 58 kiracı tablosundan satırları + `tenants` kaydı.
  Kanıt: `SELECT COUNT(*) FROM tenants` → **8**, `... WHERE ad LIKE 'PAKET%'` → **0**.
  `QA-TEST-1/2/3` fikstürlerine dokunulmadı (silme betiği de bu adları reddediyor).

**Ders (kayda geçti):** rapor yazıldığı anın ölçümünü taşır; ağaç değişince rapor eskir.
Bu yüzden paket kapanışı rapora değil, **kapı koşumuna** bağlıdır — P-08 hâlâ kapalı değildir:
mutasyon turu ve canlı doğrulama borcu duruyor, tam süit yeniden koşulacak.

---

## § TUR 2 — KALAN BORÇ KAPANDI (2026-09-20)

Yukarıda "açık" bırakılan iki borç (mutasyon turu koşulmadı · canlı doğrulama kanıtı yok)
bu tarihte kapatıldı. Kanıtlar ayrı dosyada:
**`dokumanlar/09-uygulama-kayitlari/PAKET-P-08-uygulama-raporu-TUR-2-2026-09-20.md`**

Özet: 9 mutasyon → 9'u da adıyla KIRMIZI, 9'u `cmp` BİREBİR geri alındı ·
canlı tur 5198'de (fatura bağı fail-closed 400, adet deftere yazıldı, eski satır
"bilinmiyor" işaretlendi) · `Down()` gerçekten koşturuldu (53 → 52 → 53) ·
tam süit **3032/3032** (40,65 sn) · `denetim.sh` **TEMİZ** · test kiracısı silindi
(tenants = 8) · 5199'a dokunulmadı.

**Kalan tek açık:** Y-13'ün yazma ayağı (hiçbir uç `personel_id` doldurmuyor) →
**P-14/P-16'ya devredildi**; kimlik durumu **KISMEN**.
