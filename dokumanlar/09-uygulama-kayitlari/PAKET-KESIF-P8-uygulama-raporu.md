# PAKET KESIF-P8 — SÖZLÜK · HAM KOD · JARGON (uygulama raporu, 01.10.2026, şerit t9)

Rol: Taşkın (sözlük/test) + Ertuğ (ekran ölçümü). Hasımsız paket (kurucu 30.09 21:15) → kanıt bu rapordadır. COMMIT YOK.
Kaynak: `ham/KESIF-TRIYAJ-2026-09-30.md` § KESIF-P8 + § Ortak kurallar; kararlar `ham/KESIF-KARARLAR-2026-09-30.md` (1, 2).

## 1. Yapılanlar

### 1.1 HUB `Kuyumcu.Core/I18n/Sozlukler.cs` (TR + EN, iki modda geçerli)
| Anahtar | Önce | Sonra | Bulgu |
|---|---|---|---|
| Cari.DuzeltmeIpucu | "…deftere CARI_DUZELTME olarak…" | "…cari düzeltme fişi olarak…" (EN "account correction slip") | #21 |
| Urun.SayimIpucu · Sayim.KapatOnay | SAYIM_DUZELTME | "sayım düzeltmesi" (EN "stock-count correction") | #82 |
| Siparis.EmanetUyarisi | EMANET_GIRIS | "emanet girişi" | #33 |
| Urun.PariteKoduIpucu | (ÇEYREK_YENİ, REŞAT…) | (ör. yeni çeyrek, Reşat…) | P4 notu |
| Urun.RfidIpucu | "…kapsamı DIŞINDADIR (K-21)…" | "Etiketteki kodu elle yazın. Bu kurulumda RFID okuyucu bağlantısı yoktur; alan yalnız kodu saklar." (K-21 yalnız kod yorumunda) | devir KESIF-P4 |
| Fiyat.Gram (fiyat paneli sütunu) | "Gram" / "Grams" | "Has gram" / "Fine grams" (sütun `HasGrami` gösteriyor) | devir KESIF-P1 |
| Vezne.KurTahsilAninda | "…sunucudan alınır" | "Kur, tahsilat anındaki güncel kurdur" | #17 metin |
| Siparis.TeklifAl · TeklifKaynak | "…sunucudan al" · "sunucudan" | "Fiyat teklifi al" · "otomatik hesap" | #35 metin |
| Panel.DuzenKaydedilemedi · Kabuk.SiraKaydedilemedi | "…sunucuya kaydedilemedi" | "… kaydedilemedi" (tek kısa cümle) | TOPLU KATMAN-2 metin |
| Cari.UyrukIpucu (kullanılmıyor) | "İki harfli ülke kodu (TR, DE…)" | "Ülkeyi listeden seçin." (karar 4: MASAK alpha-3) | P2 eşgüdüm |
| Isletme.YalnizSahip · Vezne.AyrimAciklama (kullanılmıyor) | "SAHİP rolü", "dört göz" | P1'in sade metinleri | P1 notu |
| EN Kur.KayitDegismez · SayfaDuzeltme.DenetimAciklamasi | "append-only" | "only ever added / only grows" | tarama |

### 1.2 `Sozlukler.Katalog.cs` — BASİT katman
- Yeni `KesifP8BasitSozlugu` (136 karşılık, her satır `(lambda yol, TR, EN)` üçlüsü → eksik dil/yanlış anahtar = derleme hatası), `BasitSozluk.Kur()`'a TEK satırla bağlandı. Kapsam: BASİT sözlükte `cari`, `pasif`, `sunucu`, `storno`, `append-only` geçen her PRO metnin sade karşılığı ("Kullanımdan kaldır", "Kullanım dışı", "Müşteri", "Geri al", "yalnız eklenir", "Liste sınırı", "sistem hesaplar"), + TOPLU birleştirmeleri: Veri içe aktar `DosyaSec`/`PolitikaHata` ("CSV", "Hata say"), BASİT MASAK başlık/açıklama/kademe metni, Kasalar uzun "para nereye gitti" cümlesinin kısaltılması.
- Mevcut `BasitDilMetinleri.Yonetim_SubeAktifeAlmaGerekce` TR/EN değerinden "sunucu" çıkarıldı.
- PRO sözlük DEĞİŞMEDİ (D10: "Cari", "Pasife Al", "Storno", yasal tam metin PRO'da yaşar — SADE-1).

### 1.3 Devir: denetim.sh P-10 + P10 testi
- `dotnet/denetim.sh` P-10 `K21_DONANIM_BEYANI`: HUB'da "K-21" aramak yerine `RfidIpucu` düz cümleyi ("RFID okuyucu bağlantısı yoktur") taşıyor VE "K-21" taşımıyor mu diye sorar; Urunler bağı aynen.
- `Kuyumcu.Tests/P10EkranTestleri.cs` `RFID_alaninin_altinda_DONANIM_KAPSAM_DISI_beyani_yazar`: "RFID okuyucu bağlantısı yoktur" VAR, "K-21" ve "DIŞINDADIR" YOK.

### 1.4 Yeni test `Kuyumcu.Tests/KesifSozlukTaramaTestleri.cs` (9 test)
P8_01 BASİT TR yansımalı tarama (2.806 yaprak) · P8_02 PRO TR+EN BÜYÜK_ALT_ÇİZGİ 0 (+ D10 "Cariler"/"Pasife Al" PRO'da kalır) · P8_03 BASİT EN ham kod/storno/append-only 0 · P8_04 karşılık sözleşmesi (PRO'dan farklı, PRO değeri gerçekten taramaya takılıyor, `{n}` yer tutucuları TR/EN birebir, aynı yol iki katman listesinde yok) · P8_05 bulgu metinleri + devirler (TR/EN × iki mod) · P8_06 DilServisi basit/PRO geçişi (KAT_K8_03 kalıbı) · P8_07 razor cırcırı.

## 2. Sapmalar ve gerekçeler
1. **Yeni BASİT anahtarlar `BasitDilMetinleri`'ne değil ayrı `KesifP8BasitSozlugu`'na yazıldı.** `KatalogK1Testleri.KAT_K8_02` her `BasitDilMetinleri` PRO değerinin BÜYÜK harfli D10 kalıbını ("Cari", "Pasife al") taşımasını şart koşuyor; P8 metinlerinin çoğu küçük harfli ("cari", "pasife alınacak", "sunucu") → oraya eklemek KAT-1 testini kırardı. CUMA-J emsaliyle ayrı liste, aynı katman.
2. **Razor cırcırı tabanı 9 değil 1.** Spesifikasyonun 9 yeri (`@x.AyarId`) bugün kodda 8 öznitelik (`title=`, `value=`, `Deger=`) + 0 görünür basım; karar 2 ham değeri `title`'da serbest bırakıyor. Test görünür basımı sayar (öznitelik ve `KurKaynakAdi.AyarAdi(...)` hariç): taban **1** = `Pages/SiparisDetay.razor` `@(k.AyarId ?? "—")` (sahibi KESIF-P6/sipariş detay). Yalnız azalabilir.
3. **"Yeni Ürün" → "Yeni ürün" (bütün "Yeni X" cümle düzeni) YAPILMADI.** 18 HUB etiketi; `UrunEkranTestleri` 8 testte `Dugme(e, "Yeni Ürün")` büyük/küçük harf duyarlı arıyor (başka paketlerin testleri). Dosya alanım dışında → devir (aşağıda).
4. **(TUR 2 ile KAPANDI — § 13)** Tur 1'de append-only PRO metinleri değişmemişti; tasarım hakemi #87'yi PRO'da açık buldu. Tur 2'de PRO TR+EN düz cümleye çevrildi, `RiskEkranTestleri:228` beklentisi "yalnız eklenen" + "append-only YOK" olarak güncellendi (hakem bulgusunun doğrudan gereği; gerekçe § 13).
5. `Isletme.Aciklama` ("SAHİP rolü") geri alındı: `OrtakGorunumHakemTestleri.HK3` PRO metninin BASİT ekranda OLMADIĞINI sınıyor; P1 metniyle eşitlenirse kırılırdı. Metin hiçbir ekranda kullanılmıyor.
6. Mevzuat: yasal hüküm eklenmedi/değiştirilmedi. MASAK BASİT açıklamasında "Bu tutar yasal eşiği aştığı için" → "Yasal kimlik sınırı aşıldığı için" (iddiayı genelleştirir, yeni hüküm koymaz; "8 yıl" aynen). Davranış/para/veri değişikliği YOK.

## 3. Migration
Gerekmedi (yalnız metin/test).

## 4. Testler
- Filtreli: `KesifSozlukTaramaTestleri|KatalogK1Testleri|P10UrunEkranTestleri.RFID|CumaJTestleri` → **32/32 geçti**. BASİT mod kullanan 29 ekran test dosyası → **564/564 geçti**.
- Tam süit (tur 1, son kodla; trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P8/_Mac_2026-10-01_05_57_11.trx`): **8475 toplam · 8462 geçti · 12 başarısız · 1 yürütülmedi**. Kırmızılar (hepsi eşzamanlı şeritlerin ara durumu): DuzeltmePolitikasi.Her_varlik_haritada_VARDIR, OlcumKapisi.BEKLENEN_VARLIK…, OlcumKapisi.P02_blogundaki…, PusulaBelge ×2, S3b Gider_pusulasi… (YASAL-4: yeni `StopajOdemesi` varlığı / pusula) · KesifP6Http POS komisyon, S3eKurKaynagi ×3, UyumEkran.Saklama ×2 (KESIF-P6: Kur/Uyum.razor çalışılıyor).
- **Fark kanıtı (madde 7):** aynı kırmızı kümesi (+ sonradan YASAL-4'ün `#u-kmts` değişikliğiyle kırılan P10Urun 4 KMTS testi) P8 değişiklikleri GERİ ALINMIŞ ağaçta ve P8'li ağaçta koşuldu: ikisinde de 40 testten aynı 7 kırmızı → `diff` boş, **FARK 0**. Dosyalar geri kondu, `cmp` 4/4 AYNI.
- Yeni test sayısı: 9 (P8_02 ve P8_05 TR/EN teorisi → 11 koşum).

## 5. Mutasyon tablosu (kopya → perl tek değişiklik → build → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M8-1 | Katalog: `Kasa.PasifeAl` BASİT = "Pasife Al" | P8_01, P8_04, P8_06 | AYNI |
| M8-2 | HUB `Sayim.KapatOnay` geri SAYIM_DUZELTME | P8_01, P8_02(tr), P8_05(tr) | AYNI |
| M8-3 | HUB `RfidIpucu` geri "DIŞINDADIR (K-21)" | P8_05(tr), P10Urun.RFID_alaninin_altinda… | AYNI |
| M8-4 | Katalog `Kur()` P8 katman satırı yorumlandı | P8_01, P8_03, P8_06 | AYNI |
| M8-5 | Raporlar.razor ayar hücresi `@s.AyarId` düz basım | P8_07 | AYNI |
| M8-6 | HUB `Fiyat.Gram` geri "Gram" | P8_05(tr) | AYNI |
(M8-3/M8-4 koşumlarında görülen P10Urun KMTS kırmızıları mutasyondan bağımsız; temiz ağaçta da kırmızı — YASAL-4.) Son mutasyondan sonra temiz build: 0 hata, 2 uyarı (KesifP2Testleri:454, SUBEKAPSAMASinirTestleri:49 — başkasının).

## 6. Denetim
`dotnet/denetim.sh` son koşum: tek ✗ **P-30 DEMO_INSAN_SURESI** (kapsam dışı, dispatch'te hariç). `✓ P-10: K21_DONANIM_BEYANI ekranda yazılı`. İlk koşumdaki IKiraciVarlik 75/74, ToListAsync 149/145, OLCUM_TABANI_KOPYA ✗'leri YASAL-4'ün Api ara durumuydu (P8 Api'ye dokunmadı); son koşumda kapanmışlardı.

## 7. Canlı doğrulama + gerçek tarayıcı (API 5188 · vekil 5189 · başsız Chrome CDP 9987-9990)
Kiracı `PAKETKESIF-P8-TEST Kuyumculuk` (01M3TPE2KEGXG7EY03MDYXK2D3), kendi rastgele sistem anahtarıyla `demo-tohumla.js --prova-kiraci` → ürün uçlarından dolduruldu. SQL: cariler MUSTERI 6 / TEDARIKCI 1 · urunler 12 · hareketler SATIS 9, SAYIM_DUZELTME 12, EMANET_GIRIS 1, ALIS 1, TAHSILAT 1. API `GET /cikti/SATIS_FISI/{id}` 200, `GET /cikti/CARI_EKSTRE/{id}` 200.
Ölçüm `sikisma-olc.js` (`__olcTam`) + görünür metin taraması, 14 rota × 1280/375 × BASİT/PRO × açık/koyu = **112 ölçüm** (`/private/tmp/kuyumcu-serit/KESIF-P8/olcum/olcum.jsonl`, `tablo.txt`, görüntüler `olcum/foto/`).

| Ölçüt | Sonuç |
|---|---|
| BASİT görünür metin 'cari' / 'Pasife Al' — /cariler /kasalar /kurulum /urunler (+Yeni ürün çekmecesi) /uyum Risk (#18 #74 #76 #81 #83 #87) | **0 / 0** (8 kombinasyon her rota) |
| Ham kod BÜYÜK_ALT_ÇİZGİ, 'K-21' — /siparisler /urunler /islemler /uyum (#21 #33 #82 #87) | **0** (BASİT+PRO, 112 ölçüm) |
| BASİT 'sunucu' / 'storno' / 'append-only' | 0 — yalnız /cikti/CARI_EKSTRE'de 1 'sunucu' (API belge metni, aşağıda devir) |
| BASİT /cikti/CARI_EKSTRE 'cari' 2 | basılı belge başlığı "CARİ EKSTRESİ" + taraf etiketi "Cari" (API `CiktiModulu`, karar 1 basılı belgeyi resmî adla bırakır); ekran açıklaması artık "müşteri hesap dökümü" |
| BASİT /islemler 'pasif' 1 | "Pasif müşteriler listede yok." — `Sozlukler.SatisEkrani.cs` (SATIŞ şeridi), HUB dışı → devir |
| K1 gizli-görünen · K3 taşan/kırık · K5 'TRY' kalıbı | **0 / 0 / 0** (112 ölçüm) |
| K9 binen | yalnız Yeni ürün çekmecesi açıkken 6 (çekmece sayfanın üstünde: ölçer katman örtüşmesini sayıyor, ör. "#urun-sinir ⟂ Kod"), BASİT ve PRO aynı; metin kaynaklı değil |
| Yatay kaydırma | **0** — 14 rota × 1280/375 × BASİT/PRO |
| Dar sütun | metin paketi yerleşime dokunmadı |
Görüntüler Read ile görüldü: `kasalar__BASIT_aydinlik_1280.png` (⋯ menüsü, "Pasife Al" yok), `uyum-risk__BASIT_karanlik_375.png` ("Yüksek riskli müşteriler", "PROFİLİ HİÇ HESAPLANMAMIŞ MÜŞTERİ"), `kurulum__BASIT_aydinlik_375.png` ("sistem satırları kontrol eder", "müşteri düzeltme fişi"), `urun-yeni-rfid__BASIT_aydinlik_1280.png` (RFID: "Bu kurulumda RFID okuyucu bağlantısı yoktur", KMTS: "sistem doğrulamaz", Tedarikçi: "Malı bize satan kişi ya da firma").
Temizlik: API (PID) ve vekil durduruldu, 5188/5189 dinleyici yok; `zsh kiraci-temizle.sh 01M3TPE2KEGXG7EY03MDYXK2D3 --uygula` → kiracı 0 satır, hareketler/cariler 0. tenants: başlangıç 13 → son 14; fark P8 değil: `PAKETKESIF-P6-TEST Canli` 03:04 UTC'de (benim başlangıç sayımımdan sonra) P6 şeridince açıldı. Fikstür/anahtar dosyaları boşaltıldı.

## 8. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, uç YOK, ekran düğmesi YOK. Yalnız sözlük metni (iki dil) + test + denetim kuralı. PATCH/DELETE/RolGerekli/denetim kaydı maddeleri uygulanamaz (salt metin).

## 9. Dokunulan sayfalar
Hiçbir .razor kalıcı değişmedi (Raporlar.razor yalnız M8-5 mutasyonunda geçici değişti, cmp AYNI geri).

## 10. Değişen dosyalar
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (HUB)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Katalog.cs`
- `dotnet/Kuyumcu.Tests/KesifSozlukTaramaTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/P10EkranTestleri.cs`
- `dotnet/denetim.sh` (P-10 K21 satırı)
- bu rapor

## 11. Açık kalanlar (sahibiyle)
1. **"Yeni X" cümle düzeni** (18 HUB etiketi, ör. "Yeni Ürün") — HUB değişikliği 1 dakikalık; ama `UrunEkranTestleri` (8 test) vb. düğmeyi büyük harfli metinle arıyor. Orkestratör kararı: testler `#urun-yeni-ac` kimliğine çekilsin, sonra P8 metni değiştirsin.
2. **/cikti basılı belge** (`Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs:277` "CARİ EKSTRESİ", `:231` "Sunucu sınırı: …", `:251` taraf "Cari") — karar 1 resmî adı "Cari Hesap Ekstresi" diyor; API metni sahipsiz → orkestratör.
3. `Sozlukler.SatisEkrani.cs` `PasifKisa` "Pasif müşteriler listede yok." (BASİT /islemler) → SATIŞ şeridi sahibi.
4. `MenuSozlugu.cs:82` kenar çubuğu hata bandı uzun cümlesi (TOPLU KATMAN-2) → Menü sözlüğü sahibi.
5. Giriş ekranındaki ham istisna adı `(OturumDustuHatasi)` — `Giris.razor` sahipsiz → orkestratör.
6. Razor cırcırı: `Pages/SiparisDetay.razor` `@(k.AyarId ?? "—")` → KESIF-P6/sipariş detay; düzelince `AyarKoduTabani` 0'a indirilir.
7. ~~PRO append-only~~ KAPANDI (tur 2, § 13). Kalan: `Kurulum.NegatifAcilisGerekce` "(K-24)" iç kodu (PRO metni; ayrı karar).
8. Ağaç ölçüsü (orkestratör kapısı): tam süitte YASAL-4 (StopajOdemesi, #u-kmts) ve KESIF-P6 (Kur/Uyum) ara kırmızıları; denetimde P-30.

## 12. Kurucu / mali müşavir
Yok — mevzuat hükmü yazılmadı; MASAK BASİT açıklaması yalnız sadeleşti (hangi tutarın — tek işlem mi günlük toplam mı — eşiği aştığı ekranda belirtilmiyor; tutar/kural metni P5 yerleşim paketinde ele alınmalı, hüküm teyidi gerekirse mali müşavire).

## 13. Tur 2 + 3 — hakem bulgusu #87 (PRO'da "APPEND-ONLY … ezme") kapatıldı
**Bulgu (KRİTİK, tasarım hakemi):** PRO /uyum Risk ekranında müşteri seçilince DegismezIpucu HUB `Uyum.ProfilDegismezNot` metnini basıyordu: "Risk profili APPEND-ONLY denetim kaydıdır … gerekçeli elle ezme" (EN "APPEND-ONLY … override").

**Yapılan (yalnız metin + test; para/veri/yasal davranış YOK):**
| Anahtar (HUB `Sozlukler.cs`) | Önce | Sonra |
|---|---|---|
| Uyum.ProfilDegismezNot TR | "…APPEND-ONLY denetim kaydıdır … gerekçeli elle ezme — ikisi de YENİ satır…" | "Risk profili yalnız eklenen bir denetim kaydıdır, değiştirilmez: … yeniden hesaplayın ya da gerekçe yazarak skoru elle belirleyin — ikisi de yeni satır yazar, eskisi durur." |
| Uyum.ProfilDegismezNot EN | "…an APPEND-ONLY audit record … override…" | "…an audit record that only grows and is never changed … set the score by hand with a justification…" |
| Uyum.ElleGerekceEnAz TR | "Gerekçesiz ezme…" | "Gerekçesiz elle değiştirme…" |
| Siparis.AppendOnly TR / EN | "append-only'dir" / "is append-only" | "yalnız eklenen bir kayıttır" / "Rows are only ever added to this log" |
| Denetim.DegistirilemezNot TR / EN (:4970/:7884) | "append-only'dir" / "is append-only" | "yalnız eklenen bir kayıttır" / "only grows" |
| Duzeltme.DenetimGerekce EN | "is append-only" | "only grows" |
| SayfaDuzeltme varlık adı | (yok → ham "CariRiskProfili" basılıyordu; canlı ölçümde bulundu) | "Risk profili" / "Risk profile" |

**Testler:** `KesifSozlukTaramaTestleri` P8_02 artık PRO TR+EN'de "append-only" yasağı taşır; P8_05 "CariRiskProfili" varlık adı; P8_06 PRO'da `Siparis.AppendOnly` ve `Uyum.ProfilDegismezNot` için "append-only" YOK, "ezme" YOK, "Risk profili yalnız eklenen" ile başlar. `RiskEkranTestleri.Gecmis_profil_satiri_icin_DegismezIpucu_gosterilir` (başka paketin testi, satır 228) "APPEND-ONLY" VAR beklentisi → "yalnız eklenen" VAR + "append-only" YOK olarak güncellendi: hakemin istediği metin değişikliğinin zorunlu sonucu; testin amacı (DegismezIpucu görünür) korunur.

**Build:** `dotnet build Kuyumcu.sln` → 0 Hata, 0 Uyarı (son mutasyondan sonra temiz build dahil).
**Filtreli:** KesifSozlukTarama + Risk + KatalogK1 + P10Urun + CumaJ + DenetimIzi + Siparis → **222/222 geçti**.

**Mutasyon (tur 3; kopya `/private/tmp/kuyumcu-serit/mut-yedek/KESIF-P8-t3/` → perl tek eşleşme → filtreli test → geri kopya → cmp):**
| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M8-7 | HUB ProfilDegismezNot TR geri "APPEND-ONLY denetim kaydıdır" | P8_02(tr), P8_06, RiskEkranTestleri.Gecmis_profil_satiri_icin_DegismezIpucu_gosterilir | AYNI |
| M8-8 | HUB Siparis.AppendOnly TR "append-only kayıttır" | P8_02(tr), P8_06 | AYNI |
| M8-9 | HUB Denetim.DegistirilemezNot EN "is append-only" | P8_02(en) | AYNI |
(M8-8'in ilk denemesi paralel şeridin derlemesiyle dosya kilidi çakıştı — MSB4018 GetFileHash, test koşmadı; aynı mutasyon yeniden koşuldu, sonuç yukarıda.)

**Tam süit (tur 3, son kodla, `--no-build`; trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P8-t3/`):** 8557 toplam · 8550 geçti · **6 başarısız**:
- `KesifP6PanelTestleri.Panel_kur_kaynagi_secici_yok_PATCH_yok` ×4 — koşum sırasında `Pages/Panel.razor` başka şeritçe 09:08'de değiştirildi (P8 Panel.razor'a dokunmadı); hemen ardından yeniden koşumda **4/4 geçti** → geçici, P8 değil.
- `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` (59≠58) + `Kilitsiz_tip_listesi_circir_ile_donduruldu` (`KaynakSonuc` artık kilitli) — tip `Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs` (KESIF-P6 kur kaynağı); tur 2 süitinde de kırmızıydı; P8 Contracts/Api'ye dokunmadı → **sahibi KESIF-P6**.
→ **P8'in yeni kırmızısı: 0.**

**Denetim:** `dotnet/denetim.sh` → tek ✗ P-30 DEMO_INSAN_SURESI (kapsam dışı, dispatch'te hariç); `✓ P-10: K21_DONANIM_BEYANI ekranda yazılı`.

**Canlı + gerçek tarayıcı (API 5188 · vekil 5189 · başsız Chrome CDP 9988/9989):** kiracı `PAKETKESIF-P8-TEST Tur2` (01M3TSW32773SRV4VKZC4PCPWD), API ile açılmış müşteri "P8 Tur2 Müşteri". SQL: `cariler` 1, `cari_risk_profilleri` 1 (Risk ekranında geçmiş profil satırı + DegismezIpucu görünür). Ölçüm `sikisma-olc.js` (`__olcTam`) + görünür metin taraması (`olc-t3.mjs`, `/private/tmp/kuyumcu-serit/KESIF-P8/olcum-t3/olcum.jsonl`, görüntüler `olcum-t3/foto/`):

| Rota | Genişlik | Mod | Tema | K1 gizli-görünen | K3 taşan/kırık | K5 TRY | K9 binen | Yatay | append-only | ezme | ham kod | "yalnız eklenen" |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /uyum Risk + müşteri | 1280 · 375 | BASİT | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /uyum Risk + müşteri | 1280 · 375 | PRO | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | **0** | **0** | 0 | 1 |
| /denetim-izi | 1280 · 375 | BASİT | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| /denetim-izi | 1280 · 375 | PRO | açık · koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
(16 ölçüm; K2 kart sağ kenar %97-98 = ana alanın tamamı, dar sütun yok. PRO'da "cari" 7-10 sayımı D10 gereği PRO'da kalan "Cari seç"/"Yüksek riskli cariler" etiketleridir.)
Görüntüler Read ile görüldü: `uyum-risk-cari__PRO_aydinlik_1280.png` ve `uyum-risk-cari__PRO_karanlik_375.png` — "Bu kayıt değiştirilemez" kutusu: "Risk profili yalnız eklenen bir denetim kaydıdır, değiştirilmez: … gerekçe yazarak skoru elle belirleyin — ikisi de yeni satır yazar, eskisi durur." APPEND-ONLY ve "ezme" yok; 375'te taşma yok.
`Siparis.AppendOnly` (SiparisDetay `#sipd-appendonly`) tarayıcıda ölçülmedi: test kiracısında sipariş yok, sipariş açmak veri yazar; metin P8_06 + P8_02 ile kanıtlı.

**Temizlik:** API (PID) ve vekil durduruldu, 5188/5189 dinleyici yok; `zsh kiraci-temizle.sh 01M3TSW32773SRV4VKZC4PCPWD --uygula` → kiracı 0, cariler 0; tenants 16 → **15** (kalan PAKET* kiracıları TOPLU1/YASAL4/P6/Q10/T-S2 — başka şeritlerin). Fikstür/anahtar dosyaları `printf ''` ile boşaltıldı.

**Değişen dosyalar (tur 2-3 eki):** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`, `dotnet/Kuyumcu.Tests/KesifSozlukTaramaTestleri.cs`, `dotnet/Kuyumcu.Tests/RiskEkranTestleri.cs` (satır 228, gerekçe yukarıda), bu rapor. Hiçbir .razor değişmedi.
