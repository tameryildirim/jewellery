# Sıradaki Adım — Ekip Oyu ve Mühürlenen Karar

**Tarih:** 2026-09-11 · **Toplayan:** Tamer Bey (Kurucu: *"ekibi topla, sıradaki adımı kararlaştırın"*)
**Durum:** Adım 1-4 bitti, 173/173 test yeşil. Soru: sırada ne var?

## 0. Oylar

| Kişi | Oy | Ana gerekçe |
|---|---|---|
| **Bertan** (Mimar) | **A** | B, E-2, E-3, F hepsi A'ya bağımlı ya da A'nın içinde; yanlış sıra çift iş doğurur. Şart: şifreleme kararı A'dan önce mühürlensin. |
| **Mehmet** (Güvenlik) | **E → A** | **Faz 0 vetosunu KALDIRDI.** Yeni şart: TCKN şifreleme ilk veri yazılmadan; okuma izi A'nın kabul ölçütüne. |
| **Eray** (Satış) | **D → A** | Boş grafik demo katili. **MASAK farklılaşma iddiası ÇÖKTÜ** — SarrafPro ve KuyumBase zaten satıyor. |
| **Aşkın** (QA) | **denetim izi testleri → A** | Denetim izinde **sıfır** otomatik test var; kapımdan geçmez. MASAK testlerinde 2 kör nokta. |

*Not: Aşkın'ın metni "önce C" yazıyor ama gerekçesinin tamamı denetim izi testleri üstüne; C (POS-fatura) hiçbir yerde savunulmuyor. Etiket hatası olarak okundu, oyu yukarıdaki gibi kayda geçti.*

**Dördü de A'yı hedef gösteriyor; fark, A'dan ÖNCE ne yapılacağında.** Bu farklar birbirini dışlamıyor — hepsi küçük ve bağımsız. Mühür aşağıda.

---

## 1. EKİBİN BULDUĞU, KARARI DEĞİŞTİREN GERÇEKLER

### Eray: MASAK farklılaşma iddiası öldü
`SATILABILIRLIK-DEGERLENDIRMESI §7` *"MASAK'ı uygulamanın içinde otomatikleştiren ürün görmedim"* demişti. Eray kanıt buldu:
- **SarrafPro** — tüm konumlandırması bu: *"Banka, e-Fatura ve MASAK Uyum Platformu"*; ayrı "MASAK Sorgusu" özellik sayfası; *"Şüpheli İşlem Bildirimi Artık Otomatik"* blog duyurusu.
- **KuyumBase** — özellikler sayfasında *"MASAK ve KMTS uyumu, şüpheli işlem takibi, MASAK/AML bildirimleri"*.

→ "İlk biz yaparız" **savunulamaz.** İddia en fazla "biz de yaparız, has altın defterimiz daha sağlam" olabilir. **SATILABILIRLIK §7 düzeltildi.** Sınır: rakip pazarlama metni; demo doğrulaması yok (Kurucu K3 hâlâ açık).

### Bertan: plan tablosunda kayda geçmemiş karar
PLAN Adım 3 *"`Cari`ye kimlik alanları eklenecek"* diyor; migration `cariler`e **dokunmadı**, `Cari`'de hâlâ yalnız `VergiNo` var. Kimlik `MasakKimlikTespiti`'ne **işlem bazlı** konuldu.
**Mühür: bu doğru karar, plan yanlıştı.** Gerekçe: MASAK "işlem anında tespit" ister; TCKN'yi cariye yazmak KVKK veri asgariliğine aykırı (her müşteri kartında kalıcı TCKN). Plan metni düzeltildi.

### Bertan: sayım kapandı
Elle `TenantId ==` şartı: 74 satır. 4'ü `IslemDogrulayici` (kapı — kalır), 1'i `DbContext` (filtre tanımı — kalır). **Temizlenecek: 69.** Ne 68 ne 73.

### Bertan: Takas tuzağı
`SatisModulu.cs:463` `farkTl = yeniDeger − eskiDeger`. 900.000'lik eski altınla 910.000'lik ürün takasında fark 10.000 → eşik hiç tetiklenmez, oysa el değiştiren değer 1,8 M. **Kapıya `Max(eskiDeger, yeniDeger)` verilir, fark değil.** A'nın zorunlu maddesi.

### Bertan: D iki sayfayı birden boş bırakıyor
`Raporlar.razor:161,187` de aynı `Gunler`i bekliyor; `CubukNoktalari.Count == 0` olduğu için grafik bloğu **hiç render edilmiyor**. Tek eksik alan, iki sayfa.
"Net Kasa": `SatisTl − AlisTl − HurdaTl` — kasa bakiyesi değil, günlük brüt akış farkı. **Etiket yanlış, rakam değil.**

### Mehmet + Aşkın: denetim izi yarım
- `DenetimServisi.OkumaKaydet` yazılmış, **hiçbir yerden çağrılmıyor** (grep: 0). KVKK'nın "kim hangi TCKN'yi gördü" sorusu bugün cevapsız.
- `DenetimYazmaDinleyicisi` için **sıfır** otomatik test; test boru hattında (`TestOrtami.Kur`) **hiç yok**.

### Aşkın: MASAK testlerinde kör noktalar
- **19 karakter test edilmemiş** — 18 (false) ve 20 (true) var, tam sınır yok. `<` yerine `<=` yazılsa yakalanmazdı.
- `KimlikBelgeTuru`'nun 5 değerinden yalnız `TcKimlik` test edilmiş.
- Negatif işaret eşik **sınırında** test edilmemiş (yalnız −500.000).

---

## 2. MÜHÜR — SIRA

### Ön hazırlık (küçük, bağımsız, HEPSİ A'dan önce, paralel)
| # | İş | Kimin şartı |
|---|---|---|
| Ö1 | Denetim izi dinleyicisini **test boru hattına** ekle + ≥9 test | Aşkın, Mehmet |
| Ö2 | MASAK testlerine 19-karakter, tüm belge türleri, negatif sınır | Aşkın |
| Ö3 | **D** — `AylikOzet.gunler` + "Net Kasa" etiketi | Eray (demo katili), Bertan (küçük, çakışmasız) |
| Ö4 | SATILABILIRLIK §7 ve PLAN Adım 3 metni düzelt | Madde 1 |

### Şifreleme kararı (kod değil, karar — A bunsuz başlamaz)
**Mühür:**
- `MasakKimlikTespiti.BelgeNo` → **AES-256-GCM, rastgele nonce** (deterministik DEĞİL — aynı TCKN her kayıtta farklı şifreli metin).
- Eşitlik araması için ayrı **`belge_no_hash`** kolonu: **HMAC-SHA256(anahtar, normalize(TCKN))**. "Bu TCKN daha önce işlem yaptı mı" sorgusu buradan; şifreli kolon aranmaz.
- `AdSoyad` şifrelenmez (arama/görüntüleme ihtiyacı; TCKN kadar kritik değil). Mehmet md.2 "belge no" der, ad soyadı saymaz.
- **Anahtar `appsettings`'e KONMAZ.** Ortam değişkeni `KUYUMCU_SIFRELEME_ANAHTARI` (base64, 32 bayt). Yoksa uygulama **açılmaz** (fail-closed). Geliştirmede `appsettings.Development.json` ayrı bir dev anahtarı taşıyabilir, üretim dosyasında yer almaz.
- Anahtar rotasyonu Faz B — ama kolonda **anahtar sürümü** (`sifre_surumu`, tinyint) bugünden konur ki rotasyon göç gerektirmesin.
- Veri **bugün 0 satır** → göç yok, `AlterColumn` (Base64 çıktısı için uzunluk) + `AddColumn` yeter.

### A — MASAK'ı işlem akışına bağla (asıl adım)
Bertan'ın iki fazlı tek kapı mimarisi aynen:
- **Faz 1** `MasakKapisi.Dogrula` — transaction **açılmadan** önce, `IslemDogrulayici` çağrılarının yanında. Eksik → `BadRequestException(tüm eksikler)`. İşlem hiç başlamaz.
- **Faz 2** `MasakKapisi.Kaydet` — transaction **içinde**, hareket `Add`'lendikten sonra. Tespit ile hareket **atomik**.
- **Kapıya tabi 7 akış:** Satis, Alis, HurdaAlis, Takas (**Max, fark değil**), Iscilik, VeresiyeGir, TahsilatGir. Transfer **hayır** (karşı taraf yok).
- **Emanet ve elle kasa hareketi → K2'ye soru** (TL tutar 0, değer has-gram; hukuki, tahmin yok).
- `SatisServisi` ctor'una `MasakKuralMotoru` **opsiyonel** parametre; null'da **varsayılan eşiklerle** kurulu motor — "motor yoksa kontrol yok" YAZILMAZ. 9 test satırı elden geçmez.
- DTO'lara **tek, opsiyonel** `MasakBilgiDto` alanı. Zorunlu alan eklenmez (Aşkın: derleme kırılır).
- **E-3 A'nın içinde:** MASAK uçları `[RolGerekli]` ile doğar, TEZGAHTAR kapalı. Ek maliyet ≈ 0.
- **Okuma izi A'nın içinde:** her MASAK okuma ucu `DenetimServisi.OkumaKaydet` çağırır — Mehmet'in kabul şartı.
- **Yeni entegrasyon testi:** 185.000 TL üstü kimliksiz `Satis` → reddedilir. Bugün böyle bir test **yok**; MASAK'ın gerçekten bağlandığının tek kanıtı bu olur.
- `Islemler.razor` 5 POST + 1 paylaşılan MASAK form bileşeni. Veresiye/tahsilat Web UI'sı yok (grep 0) — API-only kalır.

### A'dan sonra
- **B** hurda kimlik + gider pusulası — A'nın kapısını `HurdaAlis`'te zorunlu kıl + pusula. (Eray: "yeni zorunluluk değil, zaten elle yapılan kağıt işi" — satış dili bu.)
- **F** 69 satır temizliği — A/B `SatisModulu`/`CariModulu`'yu kapattıktan sonra, aynı dosyalarda tek geçiş.
- **C** POS-fatura mutabakatı — bağımsız, acil değil.

---

## 3. RİSK — ÖLÇÜLDÜ
- **A'nın eşik yüzünden kıracağı test: 0/173.** Testlerdeki en yüksek tutar 100.000 TL, eşik 185.000 (Bertan + Aşkın bağımsız saydı).
- Gerçek risk derleme (ctor + DTO) — opsiyonel parametre ve nullable alanla sıfırlanır.

## 4. MEHMET'İN VETOSU
**Faz 0 vetosu KALKTI** (yazılı: "Kaldırıyorum. 6 şartın 6'sı da bende doğrulandı"). Yeni Faz-0-tipi veto **yok**; A'ya iki kabul şartı eklendi (şifreleme önce, okuma izi içinde).

## 5. KURUCU'YA
- **K2'ye iki soru eklendi:** (1) emanet giriş/çıkışı MASAK eşiğine tabi mi (has-gram → TL çevrimi gerekir mi)? (2) elle kasa hareketi bir "işlem" mi?
- **K3 artık daha acil:** SarrafPro ve KuyumBase demosu alınmadan "MASAK'ta daha iyiyiz" denmez.
- Üretim şifreleme anahtarını kim üretir/saklar — Kurucu kararı (Vault var: `bygatech-vault` konteyneri ayakta).

**Mühür: Tamer Bey, 2026-09-11.** Ön hazırlık başladı.

---

## 6. UYGULAMA DURUMU (aynı gün, devam)

| İş | Durum | Kanıt |
|---|---|---|
| Ö1 denetim izi testleri | ✅ | `DenetimIziTestleri.cs` 13 test; dinleyici test boru hattına eklendi |
| Ö2 MASAK test boşlukları | ✅ | 19-karakter, 5 belge türü, negatif sınır, null belge no (+4 test) |
| Ö3 D — grafik + etiket | ✅ | `AylikOzet.gunler` (yerel gün, sıfır dolgu) · "Günlük Net Akış" · `AylikOzet_GunlukKirilimDoner` testi UTC/yerel gün sınırını da kanıtlıyor |
| Ö4 belge düzeltmeleri | ✅ | SATILABILIRLIK §7, PLAN Adım 3 |
| **Şifreleme (E-2)** | ✅ **kod bitti** | `Common/AlanSifreleme.cs` AES-256-GCM + HMAC arama · `MasakHashDinleyicisi` (hash+sürüm unutulamaz) · `belge_no` 200, `belge_no_hash`, `sifre_surumu` + indeks · migration `MasakBelgeNoSifreleme` (0 satır → göç yok) · **ham SQL testi: diskte düz TCKN YOK, "v1:" önekli** · aynı TCKN boşluklu yazılsa da hash ile bulunuyor, şifreli sütunlar birbirinden farklı · anahtar yoksa `Yukle` fırlatıyor (test) |
| **Toplam test** | **225/225 yeşil** | |

Şifreleme kararı A'dan ÖNCE mühürlenmişti (Bertan + Mehmet şartı) — **karşılandı.** A'ya geçilebilir.
