# PAKET GOC-D — Yönetim ve rapor ekranlarının ortak girdi + ortak ızgara göçü · uygulama raporu (09.10.2026)

> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t5 · canlı API 5166 · vekil 5167 · CDP 9501–9520 · `KUYUMCU_TEST_DB=kuyumcu_test_t5`.
> Kaynak (tek doğruluk): `GIRDI-KURALLARI-ENVANTER-2026-10-08.md` — (B) K3/K8e/K31/K32, (E) Faz 2 GOC-D satırı, (F) F2/F3, (G) G1.
> COMMIT YOK. Dosya silinmedi/taşınmadı. 5199/5200/5241'e dokunulmadı. Sır/parola/jeton/TOTP gizi çıktıya yazılmadı (fikstürler chmod 600).
> Migration: **gerekmedi** (şema değişikliği yok). Tur: **1**.

## 1. Yapılanlar

### 1.1 Sunucu — "önce kapanacak açıklar" (E · GOC-D satırı)
| Açık | Önce | Şimdi | Dosya |
|---|---|---|---|
| **MFA kurtarma kodu girişi** | orkestratör düzeltmesi (C4_62/63) | KORUNDU; kutu sınırı `YonetimSinirlari.MfaKodGirisi=24` sabitinden; canlıda 19 karakterlik kurtarma koduyla giriş 200 | Giris/Ayarlar.razor |
| **Kayıt parolası** | `KayitDto.Sifre StringLength(128,8)` — "aaaaaaaa" kabul | Core `ParolaKurali` (8–72, ≥1 harf + ≥1 rakam; sıfırlama/kullanıcı uçlarıyla AYNI kural) · DTO `[Parola]` + servis yolu elle · metin `AuthSifirlaSozlugu.ParolaKurali` (ekranla AYNI anahtar) | Auth/AuthModulu.cs, Core/Domain/Girdi/YonetimKurallari.cs |
| Kayıt / kullanıcı e-postası | `[EmailAddress]` ("a@b" geçiyordu) | `[Eposta]` (Core EpostaDogrulayici) + servis yolu `GirdiKapisi` — KayitDto, KullaniciOlusturDto, KullaniciGuncelleDto | Auth, Kullanici |
| **Şube telefonu hane sayısı** | `[Telefon]` yalnız controller yolunda; servis yolunda 9 hane yazılıyordu | `SubeServisi.TelefonDenetle` (Core TelefonDogrulayici) POST + PATCH | Sube/SubeModulu.cs |
| **Rapor tarih aralıkları (400 + bas ≤ bit)** | bas>bit ve 400 gün çıplak Türkçe metinle; **3000 yılı sessiz boş rapor**; kâr-zarar ürün kırılımında hiç denetim yok; günlük/mutabakat bozuk günde sessiz boş özet; aylık 3000 yılı | Core `RaporDonemKurali.Denetle` (Rapor penceresi 2000..gelecek yıl sonu · ters → `Kabuk.TarihAraligi` (ekranın TarihAralik/Tarih bileşeniyle AYNI anahtar) · >400 gün → `GocDSozlugu.RaporDonemUzun`) — `RaporAraligi.Coz` (islem-defteri, odeme-araci, personel, sube-karsilastirma, finans), `KarZararServisi.Donem`+`UrunBazli`, `DenetimOkumaServisi` (5 yıl, K8e), `RaporGunuDogrula` (gunluk, pos-fatura-mutabakati), `AylikOzet` yıl | Rapor/RaporOrtak.cs, Rapor/RaporModulu.cs, Maliyet/KarZararServisi.cs, Denetim/DenetimModulu.cs |
| **Süzgeç beyaz listeleri** | bilinmeyen tür/ödeme/maden/eylem sorguya aynen gidiyor → "kayıt yok" | Core `RaporSuzgecKurali` — tür ∈ `Sabitler.HareketTurleri`, ödeme ∈ {NAKIT,KART,HAVALE,ALTIN,DIGER,VERESIYE,KARMA,DOVIZ}, maden ∈ `Maden.Hepsi`, denetim eylemi ∈ 5 sabit, varlık türü `^[A-Za-z][A-Za-z0-9_]{0,99}$` → 400 + alan + `GocDSozlugu.SuzgecBilinmiyor` | RaporOrtak.cs, RaporModulu.cs, DenetimModulu.cs |

### 1.2 Ekran — 12 sayfa
- **Ham girdi:** `type="date"` 5 → **0** (KarZarar `kz-bas/kz-bit`, SatisAnalizi `sa-baslangic/sa-bitis`, Raporlar `mut-tarih` → ortak `Tarih Tur=Rapor`; kimlikler AYNI). DenetimIzi ve IslemDefteri tarihleri `Tur=Rapor`. Ters/uzun/pencere dışı dönem bitiş alanının altında sunucuyla AYNI metin, hatalı dönemde **istek atılmaz** (SatisAnalizi, KarZarar, IslemDefteri, DenetimIzi; IslemDefteri'nde Yazdır kilitli + sebebi).
- **Anlamsal bileşen:** İşletme vergi no `Metin` → `TcknVeyaVkn` (KimlikNoDogrulayici — yeniden yazılmadı; rakam dışı tuş yazılmaz); kilit sebebi alan altıyla aynı. Kayıt parolası yazarken kural (8 karakter sonrası) alan altı + `kayit-gonder` kilidi + `#kayit-kilit` (aynı metin). Kayıt e-postası Core kuralı. Kullanıcılar e-posta/parola Core kuralı (eski yerel regex kalktı).
- **Uzunluk = sunucu (tek sabit):** 22 sayısal `EnFazla` → `AlanSinirlari`/`YonetimSinirlari`/`ParolaKurali` sabitleri. Ekranın sunucudan UZUN izin verdiği alanlar kapandı: yedek gerekçeleri 1000→500, davet jetonu 100→200 (DTO 200), parola 128/72 → 72.
- **Ortak ızgara (22 tablo → `Izgara`)**, `grid:<ekran>:<grid-id>`: `ayarlar:destek-gecmis` · `kullanicilar:k-tablo` · `isletme:sube-liste` · `yedekler:yd-talep/yd-kosu/yd-tatbikat` · `satis-analizi:sa-odeme/sa-personel/sa-sube` · `kar-zarar:kz-kopru/kz-urun` · `raporlar:sk-tablo/rapor-maden/rapor-stok/reyon-stok/sy-olu/mut-ozet/mut-bagsiz/mut-faturasiz` · `denetim-izi:dz-tablo` · `islem-defteri:id-tablo` · `yetkiler:yetki-tablo`. Mevcut tablo/satır/hücre kimlikleri korundu. Sunucu sayfalı listelerde (denetim, işlem defteri) sıralama düğmesi YOK; sınıra değen listelerde (destek 20, yedek talep 20/tatbikat 12/koşu 50, ölü stok, kâr-zarar ürün) sıralama KAPALI + GOC-A `SiralamaSinirNotu` (UX-S29); sırası anlam taşıyan köprü/özet/matris tablolarında sıralama yok. Eski `tfoot` toplamları (satış kırılımı, stok maden toplamları) ızgaranın altında AYNI kimlikle (`#sk-toplam`, `#rapor-stok-altin-toplam`, `#rapor-stok-toplam-*`).
- **Ölçümle bulunan ve düzeltilen:** 375 PRO gün sonu "03:47" kart düzeninde ikiye kırılıyordu (K3=3) → saat hücresi tek satır + `degisim-gunsonu.css` dar ekranda `width:auto`; gün sonu özet kartlarında "Kalem/Değer" etiket gürültüsü → KartEtiketsiz.

### 1.3 Ortak dosyalara en küçük eklemeler (gerekçeli)
| Dosya | Ekleme | Gerekçe |
|---|---|---|
| `Core/Domain/IzgaraTercihi.cs` | `BilinenEkranlar` += 10 GOC-D ekranı | beyaz liste fail-closed |
| `Core/Domain/Girdi/YonetimKurallari.cs` (YENİ) | RaporDonemKurali, ParolaKurali, RaporSuzgecKurali, YonetimSinirlari | "Core/Domain/Girdi yalnız eksik kural"; GirdiHatasi.cs/Sozlukler.Girdi.cs'e dokunulmadı (KUR-BANT şeridi aynı saatte düzenliyordu) |
| `Core/I18n/Sozlukler.GocD.cs` (YENİ) | 4 metin TR+EN | yeni kural metinleri + gün sonu özet sütun başlıkları |
| `Contracts/Sayfalar/RaporlarSozlesmeleri.cs`, `YetkilerSozlesmeleri.cs` | 4 ızgara satır record'u | denetim.sh "razor içinde record yazılmaz" (tavan 3) |
| `wwwroot/css/degisim-gunsonu.css` | dar ekranda `#mut-faturasiz-tablo` ilk sütun `width:auto` | ölçüm K3 |
| `dotnet/denetim.sh` | GRID1 35→13, HAM_TARIH 5→0, GIRDI_TUR 2→0, EKRAN_UZUNLUK 30→8 | cırcır (yalnız azalır) |

## 2. Sapmalar ve gerekçeleri
1. **Ham tablo istisnaları (2, ham sayılmaya devam eder, tavan gevşetilmedi):** Raporlar `#yas-tablo` — sütunlara HİZALI alt toplam satırları (tfoot + colspan döviz özeti) ve başlıkta ⓘ ipucu; ortak ızgarada alt-toplam/başlık şablonu yok, ortak bileşen "yalnız kullan". İşlem defteri `#id-yazdir-tablo` — ekranda görünmeyen kâğıt belgesi (belge tavanı 4 dolu). Ortak ızgaraya alt toplam şablonu gelince taşınır.
2. **400 gün sınırı** (envanter K8e "5 yıl" önerir): sunucunun mevcut performans sınırı `RaporAraligi.EnFazlaGun=400` korundu (gevşetme sunucuyu yorar); denetim izi (sayfalı) için K8e'nin 5 yılı. Metin GirdiSozlugu'nda "yıl" kalıbı olduğundan gün metni GocDSozlugu'nda.
3. **Ters aralık metni** `Kabuk.TarihAraligi` ("Bitiş tarihi başlangıç tarihinden önce olamaz.") — ortak TarihAralik bileşeninin anahtarı; sunucu ve 4 ekran artık aynı cümle (eski çıplak "…başlangıçtan önce olamaz" kalktı).
4. **Giriş/sıfırlama-isteği e-postası** `[EmailAddress]`'te bırakıldı: kimlik doğrulama davranışı DEĞİŞMEZ (eski kayıtlı adreslerle giriş kırılmasın). Parola sıfırlama tamamlama ve kullanıcı uçlarının parola METİNLERİ değiştirilmedi (kural zaten aynıydı).
5. **İşletme vergi no** `TcknVeyaVkn`: rakam dışı tuş yazılmaz, 11 hanede kesilir (Q10-3 "sessiz kırpma" kaygısı: kırpılan değer algoritmadan geçmeden Kaydet açılmaz). Sunucu `VergiKimlik` iletisi korunur (AdminModulu paket dışı); ekranın hane iletisi GirdiSozlugu'ndan — kilit sebebi alan altıyla birebir.
6. **Hücre içi `title`** (denetim detayı, ayar kodu, ödeme kodu, cari adı): ortak ızgaranın td özniteliği satıra göre değişmediğinden ipucu hücrenin İÇ öğesinde — sözleşme aynı (denetçi ham kaydı ipucunda görür).
7. **Güncellenen mevcut testler** (gerekçe satırı her birinde; hiçbiri gevşetilmedi): ZoBelgeTestleri ×4, Q105 B05, BelgeKalan BK_DenetimIzi, KesifP3 Raporlar_stok_ayar_adi (title iç öğede) · UX4dEkran M23 / UX4dServis M23 / RaporUc R3_41 (ters metni sözlük anahtarı) · OrtakGorunumHakem HK14, OrtakGorunumKiyas KY8/KY10, MobilParite B30 (kaynak kalıbı ızgarada) · RaporEkran IslemDefteri_her_dugme (ızgara Sütunlar düğmesi aria-expanded ölçülür) · MadenIki MI22, RaporGorunum RG21 (toplamlar ızgara altında aynı kimlik) · Q103 E01/E03 (vergi no anlamsal alan; "12345 67890" artık temizlenip 10 hane VKN — VKN kontrol hanesi TEYİTSİZ).

## 3. Migration
Gerekmedi.

## 4. Testler
- **Yeni `Kuyumcu.Tests/GocDTestleri.cs` — 19 test:** GocDSafTestleri GDS01 ızgara beyaz listesi · GDS02 rapor dönemi (ters/3000/1999/400–401 gün/5 yıl/EN) · GDS03 parola (aaaaaaaa, 12345678, 7/8/72/73 karakter, sıfırlama ekranı aynı sınıf) · GDS04 süzgeç listeleri · GDS05 DTO öznitelik + ekran sabiti = DTO sınırı · GDS06 RaporAraligi servis yolu; GocDSuzgecEslesmeTestleri GDS07 ekran seçenekleri ⊂ sunucu listesi; GocDSunucuTestleri (MySQL+HTTP) GDH01 kayıt aaaaaaaa/12345678/a@b 400 kiracı DOĞMAZ + geçerli 201 · GDH02 şube 9 hane HTTP + SERVİS yolu POST/PATCH · GDH03 rapor uçları 3000/ters/10 yıl/bozuk gün/yıl 3000 + geçerliler 200 · GDH04 süzgeç 400 + geçerli 200 · GDH05 TEZGÂHTAR 6 yönetim ucunda 403 · GDH06 kullanıcı a@b 400 satır YOK · GDH07 ızgara tercihi A kalıcı/B yok; GocDEkranTestleri GDE01 kayıt parolası alan altı + kilit aynı metin POST YOK · GDE02 satış analizi ters/402 gün/3000 → istek ATILMAZ · GDE03 ortak ızgara + ham date yok; GocDIsletmeEkranTestleri GDE04 şube 9 hane alan altı + kilit + POST YOK + ızgara · GDE05 vergi no harf yazılamaz.
- Filtre `~GocD`: **19/19**. Etkilenen ekranların filtreli kümesi (216 sınıf): 2716/2716 (ara2).
- **Tam süit (son kod, temiz derleme sonrası, `trx-pGOC-D/tam2.trx`):** Toplam 11489 · Geçti 11466 · Başarısız 21 · Atlanan 2. **Bu paketin kırmızısı: 0** (§ 7). İlk tam koşum (`tam.trx`, 11450) 37 kırmızı verdi: 13'ü bu paketin sözleşme değişikliğiydi (yukarıdaki güncellemeler — düzeltildi), kalanlar § 7'deki başka şerit kırmızıları + başka şeridin eşzamanlı derlemesinin test bin'inden NUlid/IdentityModel dll'lerini sildiği an (yeniden derlenip tekrar koşuldu, yeşil).
- Derleme: `dotnet build Kuyumcu.sln` **0 Hata**; 6 uyarı başka şeritlerin test dosyalarında (SUBEKAPSAMASinir, KesifP2, SonKalanlar ×2, Yildiz2 ×2) — bu paketin dosyalarında uyarı 0 (GocDTestleri'ndeki tek CS8793 düzeltildi).

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-D/` → TEK eşleşme → derle → `~GocD` → geri kopya → `filecmp` birebir; betik `GOC-D/mut.py`, sonuç `mut-sonuc.jsonl`)
| Kod | Kural kapatıldı | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | RaporDonemKurali ters aralık | GDS02, GDS06, GDH03, GDH04, GDE02 | AYNI |
| M1-2 | 400 gün sınırı ×100 | GDS02, GDS06, GDH03, GDE02 | AYNI |
| M1-3 | ParolaKurali harf şartı | GDS03, GDH01 | AYNI |
| M1-4 | İşlem defteri ödeme aracı beyaz listesi | GDH04 | AYNI |
| M1-5 | Şube telefonu servis yolu kapısı | GDH02 | AYNI |
| M1-6 | Kâr-zarar dönem kapısı | GDH03 | AYNI |
| M1-7 | Denetim eylem beyaz listesi | GDH04 | AYNI |
| M1-8 | Ekran: satış analizi dönem denetimi | GDE02 | AYNI |
| M1-9 | Ekran: kayıt parolası yazarken kuralı | GDE01 | AYNI |
| M1-10 | Rapor penceresi → Serbest (3000 geçer) | GDS02 | AYNI |
10/10 kırmızı, 10/10 cmp AYNI. ⚠ Dürüstlük notu: ilk koşumda M1-3 yalnız GDS03'ü kırdı (GDH01 "aaaaaaaa" rakamsız olduğu için yine reddediliyordu) → GDH01'e harfsiz "12345678" eklendi, M1-3 yeniden koşuldu (GDH01 de kırmızı). M1-5 ilk koşumda **derlenmedi** (başka şeridin eşzamanlı FisKalemTablosu.razor yarım düzenlemesi) — sonuç geçersiz sayıldı, temiz yeniden koşuldu. Tablo SON koşumlardır. Son mutasyondan sonra temiz derleme 0 Hata.

## 6. denetim.sh (son kod)
`DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, görev "P-30 hariç").
| Sayaç | Önce (GOC-C sonu) | Sonra |
|---|---|---|
| GRID-1 HAM_TABLO_ORTAK_GRID_DISI | 35 | **13** (bu paket; 2 gerekçeli istisna § 2.1). Son koşumda başka şerit (GOC-C-2) kendi fiş tablolarını taşıyıp tavanı **8**'e indirmişti — ✓ 8/8 |
| GIRDI-2 HAM_TARIH_GIRDISI | 5 | **0** |
| GIRDI-2 GIRDI_TUR_GENEL_BILESEN | 2 | **0** |
| GIRDI-2 EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) | 30 | **8** (kalan 8'i YasalParamFormu — paket dışı) |
| P-36 HAM_INPUT / HAM_SELECT | 13 / 14 | **8 / 14** (paket ekranlarında 0 / 0) |
| Razor record tanımı | 3 | 3 (yeni satır record'ları Contracts'ta) |
| P-18 YETKILER_ROL_YAPISKAN | ✓ | ✓ (ızgarada `YapiskanIlkSutun="true"`) |

## 7. Tam süitte kalan kırmızı — sahibi (bu paketin YENİ kırmızısı: 0)
- `FisKalemTablosu` ortak ızgaraya taşınıyor (GOC-C-2 şeridi): P78 FisKalemTablosuTurkce ×4, KESIFP5SinirKutu ×10, KesifP5 FisKalemTablosu_birim_fiyat, PatchDortFisTavan ×2 — "Cannot provide a value for property 'Api' on Izgara<FisKalemi>" (test bağlamında ApiIstemci kaydı yok). **Sahibi: GOC-C-2.**
- MobilPariteCss B9 — `/islemler` (Islemler.razor, GOC-C alanı) `<KartListe`/ızgara kalıbı. **Sahibi: GOC-C-2.**
- KesifP1Sayi `Sayi_TamSayi_kesirliyi_reddeder…` — ortak Sayi bileşeni metni (Bilesenler/Form). **Sahibi: GIRDI-AILESI / GOC-C-2.**
- SATIS3SinirT15 `T15S_kontrol_fark_5420…` — bUnit zaman aşımı (satış ekranı). **Sahibi: SATIS şeridi (kararsızlık).**

## 8. Canlı doğrulama (API 5166 Development, `kuyumcu_net`; betik `GOC-D/canli_gocd.py`; son koşum kiracı `PAKETGOC-D-TEST …` = 01M4F4NSD55F7FW6E592T5NBQ9; sonuç `GOC-D/canli-sonuc.json`) — **UYUMSUZ 0 / 55**
| Adım | Beklenen | Gerçek | İleti · alan / SQL |
|---|---|---|---|
| Kayıt parola "aaaaaaaa" | 400 | 400 | "En az 8 karakter; içinde en az bir harf ve bir rakam olsun." · sifre |
| Kayıt e-posta "a@b" | 400 | 400 | "E-posta ad@alanadi.com biçiminde olmalı." · email |
| SQL: kiracı doğmadı, davet tüketilmedi | 0 | 0 | tenants=0 · davet kullanildi=0 |
| Kayıt geçerli | 201 | 201 | tenants=1 |
| Giriş e-posta "abc" / yanlış parola | 400 / 401 | 400 / 401 | email · "E-posta veya şifre hatalı" |
| Kullanıcı e-posta "a@b" / zayıf parola | 400 / 400 | 400 / 400 | email (SQL a@b=0) · sifre |
| Kullanıcı B (MÜDÜR), T (TEZGÂHTAR), C geçerli | 201 ×3 | 201 ×3 | — |
| MFA kurulum yanlış kod / "12ab" / doğru TOTP | 401 / 400 / 200 | aynı | doğru kodda SQL `kullanici_mfa.durum=ETKIN`, 10 kurtarma kodu |
| MFA'lı giriş → ikinci adım; yanlış kod; 2 karakter; **kurtarma kodu (19 karakter)** | 200 / 401 / 400 / 200 | aynı | mfaGerekli=True · accessToken VAR |
| Şifre sıfırlama tamamla zayıf parola; istek "abc" | 400 / 400 | 400 / 400 | yeniSifre · email (kanal kapalı: `acik=false`) |
| Şube telefon 9 hane | 400 | 400 | "Telefon alan koduyla 10 haneli olmalı (şu an 9)." · telefon · SQL 0 satır |
| Şube telefon 10 hane | 200 | 200 | SQL `tel=0212 555 12 34` |
| Şube PATCH 8 hane | 400 | 400 | SQL tel değişmedi |
| İşlem defteri ters · 3000 · 10 yıl | 400 ×3 | 400 ×3 | Kabuk.TarihAraligi · bitis / "Tarih 31.12.2027 tarihinden sonra olamaz." · baslangic / "Rapor dönemi en çok 400 gün olabilir." · bitis |
| Ödeme aracı 10 yıl · Personel 3000 · Kâr-zarar ters/3000 · Kâr-zarar ürün 10 yıl | 400 ×5 | 400 ×5 | bitis / bitis / bitis / baslangic / bitis |
| Günlük 3000 · Mutabakat "bozuk" · Aylık yıl 3000 · Denetim ters/3000 | 400 ×5 | 400 ×5 | tarih / tarih ("Tarihi gün.ay.yıl olarak yazın") / yil / bitis / baslangic |
| Süzgeç tür/ödeme/maden/eylem "XYZ", varlık "a b" | 400 ×5 | 400 ×5 | "Bu süzgeç değeri tanınmıyor. Listeden seçin." · tur/odemeTuru/maden/eylem/varlikTuru |
| Geçerli: işlem defteri (SATIS, NAKIT) · kâr-zarar bu ay · denetim EKLEME | 200 ×3 | 200 ×3 | toplamSatir=3 |
| TEZGÂHTAR: /kullanicilar, /yetkiler, /denetim, /yedekler/ozet, /raporlar/islem-defteri, POST /subeler | 403 ×6 | 403 ×6 | rol iletileri |
| Izgara tercihi A yaz/oku · B'de yok | 200 ×3 | 200 ×3 | SQL `kullanici_tercihleri` 1 satır, kullanıcı A=1 |
Not: ilk koşumda (kiracı 01M4F213F9X3P4KRS96P78F8W5, `canli-sonuc-1.json`) 1 uyumsuz — betik şube oluşturma için 201 bekliyordu, uç 200 döner (sözleşme); beklenti düzeltilip temiz kiracıyla yeniden koşuldu. Daha önceki ön koşum (01M4F20NQK054QHCWF4TH8CYP5) betik SQL tablo adı (`kullanici_mfa`) hatasında durmuştu.

### 8.1 Gerçek başsız Chrome (vekil 5167, CDP 9501–9520; `sikisma-olc.js`; `GOC-D/olc.mjs` + `olc-anonim.mjs`; ham `GOC-D/olcum2/olcum.jsonl` 128 ölçüm + `olcum3` 8 yeniden ölçüm (gün sonu düzeltmesi) + `olcum4` 16 (İşletme Şubeler, Yedekler Tatbikatlar sekmeleri); her ölçümün ekran görüntüsü)
Hücre = K1 gizli-görünen + K3 taşan/kırık + K5 TRY + K9 binen + yatay kaydırma (SON değer). modGercek = istenen mod 144/144. Oturumsuz sayfalarda üst bar/mod yok (mod sütunları aynı ölçümü gösterir).
| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K |
|---|---|---|---|---|---|---|---|---|
| /ayarlar?sekme=destek | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /kullanicilar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /isletme (kimlik) · ?sekme=subeler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yedekler elle · koşular · tatbikatlar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /raporlar özet · stok · yaşlandırma · gün sonu | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 ¹ |
| /raporlar/kar-zarar köprü · ürünler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /raporlar/satis-analizi | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /denetim-izi | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /raporlar/islem-defteri | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yetkiler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /giris (kayıt, zayıf parola yazılı) · /sifre-sifirla | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
¹ İlk ölçümde 375 PRO gün sonu K3=3 ("03:47" kartta kırık) → § 1.2 düzeltmesi → `olcum3` 0.
K7 hizasız 0 · K11 hata bandı 0 (tüm ölçümler). Ölçümün ilk denemesi (`olcum/`) tema adını yanlış verdiği (acik/koyu — `kuyumcuTema` aydinlik/karanlik bekler) için GEÇERSİZ sayıldı ve tümüyle yeniden yapıldı.
Görülen ekran görüntüleri (4): `olcum3/foto/raporlar-gunsonu__PRO_aydinlik_375.png` (saat tek satır, özet kartları etiketsiz) · `olcum2/foto/kullanicilar__PRO_karanlik_1280.png` (ortak ızgara; A'nın canlıda yazdığı tercih: "1 sütun gizli", Ad ↓ — kalıcılık kanıtı) · `olcum2/foto/giris-kayit__YOK_karanlik_375.png` (zayıf parolada alan altı + kilit sebebi aynı metin) · `olcum/foto/raporlar-gunsonu__PRO_acik_375.png` (düzeltme öncesi K3).

### 8.2 Temizlik
API (5166) ve vekil (5167) kendi PID'leriyle durduruldu (`lsof` boş). Test kiracıları 01M4F20NQK054QHCWF4TH8CYP5, 01M4F213F9X3P4KRS96P78F8W5, 01M4F4NSD55F7FW6E592T5NBQ9 `zsh kiraci-temizle.sh <id> --uygula` ile silindi; davet satırları silindi. tenants: başlangıç **18** → son **18**; `PAKETGOC-D%` = 0. QA-TEST kiracılarına dokunulmadı.

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Izgara tercihi mevcut `tercihler/benim` PATCH/DELETE (varsayılana dön) ile düzenlenir/silinir. Rapor uçları türetilmiş veridir (salt okunur, DegismezIpucu mevcut). Şube/kullanıcı/işletme kimliği düzenleme yolları değişmedi (Cekmece/ModalPencere — Kullanıcılar ModalPencere kalıbı paket öncesinden; ikinci düzenleme kalıbı açılmadı).

## 10. Değişen dosyalar (bu paket; `git status --porcelain | grep -v -E "/(bin|obj)/"` süzümü)
Yeni: `Kuyumcu.Core/Domain/Girdi/YonetimKurallari.cs`, `Kuyumcu.Core/I18n/Sozlukler.GocD.cs`, `Kuyumcu.Tests/GocDTestleri.cs`.
Değişen — Api: `Modules/Auth/AuthModulu.cs`, `Modules/Kullanici/KullaniciModulu.cs`, `Modules/Sube/SubeModulu.cs`, `Modules/Rapor/RaporOrtak.cs`, `Modules/Rapor/RaporModulu.cs`, `Modules/Denetim/DenetimModulu.cs`, `Modules/Maliyet/KarZararServisi.cs` (yalnız dönem doğrulaması). Core: `Domain/IzgaraTercihi.cs`. Contracts: `Sayfalar/RaporlarSozlesmeleri.cs`, `Sayfalar/YetkilerSozlesmeleri.cs`. Web: `Pages/{Giris,SifreSifirla,Ayarlar,Kullanicilar,Isletme,Yedekler,Raporlar,KarZarar,SatisAnalizi,DenetimIzi,IslemDefteri,Yetkiler}.razor`, `wwwroot/css/degisim-gunsonu.css`. `dotnet/denetim.sh`. Testler (sözleşme güncellemesi, § 2.7): ZoBelgeTestleri, Q105FormJargonTabloTestleri, UX4dEkranTestleri, UX4dServisTestleri, OrtakGorunumHakemTestleri, OrtakGorunumKiyasTestleri, RaporEkranTestleri, KesifP3Testleri, MobilPariteTestleri, RaporUcTestleri, MadenIkiTestleri, RaporGorunumTestleri, BelgeKalanTestleri, Q103IsletmeKimlikTestleri.
(Not: `dotnet/denetim.sh.mutbak` bu paketin ürünü değildir — mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/GOC-D/` altında.)

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Ekranlarda ham `<input>/<table>` kalmaz | KARŞILANDI (2 gerekçeli istisna) | denetim § 6; HAM_INPUT/SELECT paket ekranlarında 0/0 |
| Her açık için sunucu 400 testi + ekran alan altı, aynı sözlük anahtarı | KARŞILANDI | GDH01–04 + GDE01/02/04; § 8 iletiler |
| Mutasyon ≥ 3 farklı kural, cmp AYNI | KARŞILANDI (10) | § 5 |
| Canlı: geçersiz denemeler + giriş/MFA/sıfırlama + geçerli kayıt + ızgara A/B | KARŞILANDI (UYUMSUZ 0/55) | § 8 |
| Gerçek başsız Chrome 1280+375 · B+P · aydınlık+karanlık, Read ≤ 4 | KARŞILANDI (144 ölçüm, 4 görüntü) | § 8.1 |
| build 0 hata, denetim TEMİZ (P-30 hariç), tam süit 0 YENİ kırmızı | KARŞILANDI | § 4, § 6, § 7 |
| Güvenlik: kimlik doğrulama/yetki davranışı değişmedi, TEZGÂHTAR 403 | KARŞILANDI | GDH05 + canlı 403 ×6; giriş/sıfırlama-istek DTO'ları dokunulmadı |
| Mevzuat | yasal eşik yok (iş kuralları; K8e/K31/K32) | — |

## 12. Açık kalanlar
1. Raporlar `#yas-tablo` ve İşlem defteri `#id-yazdir-tablo` ortak ızgarada değil (§ 2.1) — ortak ızgaraya alt-toplam/başlık-ipucu şablonu gerekir (GRID ortak bileşen sahibine).
2. Yedekler ekranında durum kodları (`BEKLIYOR`, `BASARILI`, koşu türü) sözlükten çevrilmeden basılıyor (UX-S31) — paket öncesi hâl, ızgaraya aynen taşındı.
3. Gün sonu faturasız işlemler kartında (375) etiket girintileri sayfanın eski `:first-child/:last-child` dolgu kuralından ötürü farklı (taşma/kırılma yok, ölçüm 0) — görsel ince ayar.
4. Kullanıcılar oluştur/düzenle hâlâ ModalPencere (paket öncesi; Cekmece'ye taşıma bu paketin kapsamı değil).
5. İşletme vergi no: VKN kontrol hanesi algoritması birincil kaynakla doğrulanmadı (envanter K4 TEYİTSİZ) — 10 hane biçim denetimi yapılır, algoritma yok.

## 13. Kurucuya / mali müşavire
Yok (paket mevzuat eşiği içermiyor). K4 VKN kontrol hanesi için GİB birincil kaynağı bulunursa doğrulayıcıya eklenebilir (TCKN-DOGRULAMA notu ile aynı).
