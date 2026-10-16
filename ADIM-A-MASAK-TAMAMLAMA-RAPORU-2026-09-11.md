# Adım A — MASAK'ın İşlem Akışına Bağlanması · Tamamlama Raporu

**Tarih:** 2026-09-11 · **Karar:** `SIRADAKI-ADIM-KARAR-2026-09-11.md` §2 · **Commit atılmadı** (Kurucu kuralı).
**Bütün sayılar ölçüldü.** "Bitti" yalnız kanıtla.

---

## 0. TEK CÜMLE

Bertan'ın tespiti *"bugün 5 milyon TL'lik nakit satış tek kontrol görmeden kapanıyor"* artık **yanlış**: 185.000 TL ve üstü her satış, alış, hurda alışı, takas, işçilik, veresiye ve tahsilat MASAK kapısından geçiyor; eksikse işlem **hiç başlamıyor**; tam ise kimlik tespiti hareketle **aynı commit'te**, TCKN **diskte şifreli** yazılıyor. **251/251 test yeşil, mevcut testlerden 0 kırıldı.**

---

## 1. KANIT TABLOSU

| # | Karar maddesi (§2/A) | Sonuç | Kanıt |
|---|---|---|---|
| 1 | İki fazlı tek kapı (Dogrula tx öncesi / Kaydet tx içi) | ✅ | `Modules/Masak/MasakKapisi.cs` |
| 2 | 7 akış kapıya tabi, Transfer değil | ✅ | grep: `masak.Dogrula/Kaydet` SatisModulu **10**, CariModulu **4** |
| 3 | **Takas: Max(eski,yeni), fark değil** | ✅ | test `Takas_FarkKucukAmaDegerBuyuk_EsigeTakilir`: 824.400/830.000, fark 5.600 → eşiğe takıldı, tespit tutarı **830.000** |
| 4 | ctor'a OPSİYONEL motor; null → varsayılan eşik ("kontrol yok" değil) | ✅ | `SatisServisi`/`CariServisi`; test `DiMotoruVerilirse_OnunEsigiKullanilir` |
| 5 | DTO'lara tek nullable `MasakBilgiDto` | ✅ | 7 DTO; **9 test satırı elden geçmedi** |
| 6 | E-2 şifreleme A'dan ÖNCE | ✅ | `AlanSifreleme.cs`; 24 test; ham SQL: `v1:` önek, düz TCKN **0** |
| 7 | E-3 uçlar `[RolGerekli]`, TEZGAHTAR kapalı | ✅ | canlı: TEZGAHTAR `/masak/kimlik-tespitleri` → **403**, `/masak/esikler` → 200 |
| 8 | Okuma izi her MASAK okuma ucunda (Mehmet md.4) | ✅ | canlı: 4 `OKUMA` izi (liste ×2, tam kimlik, arama); test `Tekil_TamBelgeNoDoner_VeOkumaIziDuser` |
| 9 | Liste maskeli, tekil tam (Mehmet md.2) | ✅ | canlı: liste `*******0146`, tekil `10000000146` |
| 10 | Yeni entegrasyon testi: 185k+ kimliksiz satış reddedilir | ✅ | `MasakEntegrasyonTestleri` 9 test |
| 11 | Blazor formu (5 POST + bileşen) | ✅ derlendi | `Bilesenler/MasakFormu.razor` + `Islemler.razor` (eşik aşılınca açılır, Gönder kilitlenir, başarıda model sıfırlanır) — **UI otomatik testi yok** (bUnit yok; Aşkın'ın tespiti) |
| 12 | Mevcut testlerden kırılma | **0/234** | Bertan+Aşkın öngörüsü: en yüksek test tutarı 100k < 185k |

---

## 2. CANLI HTTP KANITI (gerçek sunucu, taze kiracı)

```
POST /islemler/satis  200.000 TL, masak yok
  → HTTP 400
  → "MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur ·
      İşlem amacı belirtilmelidir · Açıklama en az 20 karakter olmalıdır (şu an 0)"
  → yan etki: hareket 0, kasa değişmedi, tespit 0        (Faz 1 tx ÖNCESİ)

POST /islemler/satis  aynı, tam masak bloğu
  → HTTP 200
  masak_kimlik_tespitleri: belge_no = "v1:v3NirQI…"  sifre_surumu=1  hareket_id bağlı=1
  masak_beyanlari:         kademe = AMAC_BEYANI  islem_amaci = Yatırım
  SELECT COUNT(*) WHERE belge_no LIKE '%10000000146%'  → 0

GET /masak/kimlik-tespitleri            → belgeNo "*******0146"        (maskeli)
GET /masak/kimlik-tespitleri/{id}       → belgeNo "10000000146"        (tam, iz düştü)
GET /masak/kimlik-tespitleri/ara?belgeNo=100 000 001 46 → adet 1        (hash, boşluk fark etmez)
denetim_kayitlari: 4 × OKUMA, hassas=1, detay'da TCKN yok

TEZGAHTAR → /masak/kimlik-tespitleri 403 · /masak/esikler 200
```

---

## 3. CANLI TESTTE YAKALANAN GERÇEK HATA

**Fail-closed anahtar yüklemesi çalışmıyordu.** `AddSingleton<IAlanSifreleyici>(_ => Yukle())` **tembeldi**: ilk DbContext çözülene kadar çağrılmıyor, anahtarsız uygulama `/saglik`'a 200 dönüyordu. Düzeltme: `Yukle` `Build()` **öncesinde** çağrılıp örnek kaydedildi.

**Ölçüm dürüstlüğü:** ilk iki doğrulama denemem hatalıydı — `dotnet run`, `launchSettings.json`'daki `ASPNETCORE_ENVIRONMENT=Development`'ı uyguluyor ve dev anahtarını yüklüyordu. Doğru test `--no-launch-profile` ile:
```
Production + anahtar yok  → süreç çıkış 134, "Şifreleme anahtarı yok … fail-closed"
Production + env anahtarı → /saglik 200
```

---

## 4. YAZILAN / DEĞİŞEN

**Yeni (5):** `Modules/Masak/MasakKapisi.cs` · `Modules/Masak/MasakModulu.cs` (servis + 10 uç) · `Web/Bilesenler/MasakFormu.razor` · `Tests/MasakEntegrasyonTestleri.cs` (9) · `Tests/MasakServisTestleri.cs` (17)
**Değişen (7):** `SatisModulu.cs` (5 akış + 5 DTO + ctor) · `CariModulu.cs` (2 akış + 2 DTO + ctor) · `MasakVarliklari.cs` (CariId nullable) · `ModulKayit.cs` · `Program.cs` (eager anahtar) · `Web/Pages/Islemler.razor` · `Core/I18n/Sozlukler.cs` (17 anahtar TR+EN)
**Migration:** `MasakCariIdOpsiyonel` (dev DB'ye uygulandı)

**Test toplamı: 251/251** (ön hazırlık öncesi 173 → 251).

---

## 5. AÇIK KALANLAR (dürüstçe)

1. **UI otomatik testi yok.** `MasakFormu`'nun eşik aşılınca göründüğü yalnız derleme + elle kontrolle kanıtlanabilir; bUnit/e2e altyapısı yok (Aşkın kapısı: "ölçülemeyene test edildi demem").
2. **Resmî tatil takvimi yok.** `SonBildirimTarihi` hafta sonunu atlar, resmî tatil listesi `null` geçiliyor → bayram günleri iş günü sayılır; süre **erken** dolar (kuyumcu lehine hata, ama yanlış).
3. **Emanet ve elle kasa hareketi kapıya bağlı değil** — K2 hukuki soru (has-gram → TL çevrimi gerekir mi?).
4. **Şüpheli bildirim MASAK'a gerçekten gönderilmez** — "gönderildi" işareti elle, referans no elle. Otomatik iletim (SarrafPro iddiası) bu adımda yok.
5. **Saklama süresi dolunca maskeleme/arşiv işi yok** — `saklama_bitis` var, onu okuyan iş yok.
6. **Anahtar rotasyonu yok** — `sifre_surumu` var, rotasyon kodu Faz B.
7. **Reddedilen yazma DB izine düşmüyor** — uygulama günlüğüne düşüyor (tasarım sınırı, belgeli).
8. **Veresiye/tahsilat için Web UI yok** — kapı API'de; form yok (zaten yoktu).
9. **KVKK aydınlatma metni yok** (Mehmet md.6) — form gösteriyor ama onay/aydınlatma akışı yok.

---

## 6. SIRADA (karar §2 "A'dan sonra")

**B** — hurda kimlik + gider pusulası (kapı `HurdaAlis`'te zaten çalışıyor; kalan: pusula belgesi + hurda için eşik-altı da kimlik zorunluluğu — VUK 234 gereği hurda alımında **tutardan bağımsız** gider pusulası/kimlik gerekir, MASAK eşiğinden ayrı) → **F** 69 satır temizliği → **C** POS-fatura.

**Kurucu'ya:** K1 (e-Fatura entegratör), K2 (mali müşavir: fatura haddi + emanet/kasa MASAK sorusu + resmî tatil kaynağı), K3 (SarrafPro/KuyumBase demo — farklılaşma iddiası çöktü), üretim şifreleme anahtarı (Vault).
