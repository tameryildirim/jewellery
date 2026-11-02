# PAKET P-33 — KILAVUZ TEK KAYNAK + TEMİZ KURULUM + YEDEK TATBİKATI · uygulama raporu

**Tarih:** 2026-09-23 (21:44 → 23:35, ~115 ajan-dk; tahmin 210) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** 7c
**Kimlikler:** B-11 · B-5 · **Migration:** YOK (gerekmedi)

**Durum: BİTTİ** — paket kapısının 7 maddesi sağlandı (§ 8). Derleme 0 uyarı / 0 hata. P-33 testleri 46/46 yeşil.
`denetim.sh`'taki 5 P-33 sayacı ✓. 21 mutasyonun hepsi adı yazılı bir testi ya da sayacı kırdı; geri alınca `cmp` birebir aynı çıktı.
Canlı doğrulama 5198'de yapıldı ve test kiracısı silindi (tenants 10 → 10).
Tam süit 4541 testte 6 kırmızı verdi. Altısı da başka şeritlerin dosyalarında (§ 9); bu paketin yeni kırmızısı **0**.

---

## § 0. Önkoşul ölçümü

| Önkoşul | Diskte | Kanıt |
|---|---|---|
| P-14 menü / bilgi mimarisi | VAR | `PAKET-P-14-uygulama-raporu.md`; `Layout/MenuTanimi.cs` grupları (Günlük İş … Yönetim) kılavuzun içindekiler gruplarına aynı adla taşındı |
| P-18 mobil parite / belirteçler | VAR | `PAKET-P-18-uygulama-raporu.md`; kılavuz şablonu yalnız `belirtecler.css` belirteçleriyle yazıldı (tanımsız belirteç 0 — ölçüldü) |
| P-09 kimlik / yetki | VAR | `PAKET-P-09-uygulama-raporu.md`; `/yedekler/ozet` `[RolGerekli("SAHIP")]` canlıda 403 ile doğrulandı |

## § 1. Spesifikasyon ↔ kod sapmaları

| # | Plan diyor | Ölçülen | Uyarlama |
|---|---|---|---|
| S1 | "42 ekranın hepsi var" | `RotaBasliklari.Harita` bugün **47** rota taşıyor (`grep -cE '^\s*\["/'` → 47). | Hedef ekran sayısı değil, "Harita'daki HER rota" oldu. Sayı istemciye gömülmedi; test ve sayaç Harita'dan okuyor. |
| S2 | "bugün kilavuz.html 18-39 başlık, 13 ekran yok" | Eski HTML'de 23 bölüm vardı. 47 rotadan **24'ünün** kendi bölümü yoktu (ürün ağacı, etiketler, kâr-zarar, gider/virman, hurda, evrak, vitrin, kurulum, yetkiler, işletme, denetim izi, bildirimler vb.). | Hepsi yazıldı (§ 2a). |
| S3 | "Kılavuz RotaBasliklari'ndan üretilir" | Kılavuz metni koddan üretilemez, bir insanın yazması gerekir. | İki yönlü bağ kuruldu. (a) Kaynaktaki her `<!-- ekran: /rota -->` işareti Harita'yla birebir eşleşmek zorunda. İşaretin bağlı olduğu başlık ekranın menü adını (Sozluk.Nav) taşır. (b) `/kilavuz` ekranındaki ekran listesi `RotaBasliklari.Harita`'dan üretilir. Çıpa tek kaynaktandır (`KilavuzCapasi`). |
| S4 | "kurulum betikleri yeniden yazılmaz" | Tatbikat, `yedek-al-ve-dogrula.sh`'ta kitaplık yolu hatası ve kurulumun yazdığı `ASPNETCORE_URLS`'in etkisiz olduğunu buldu (§ 5). | Betikler yeniden yazılmadı. Yalnız iki nokta onarıldı ve adres/kısayol **eklendi** (K-26). Her onarım testle kilitlendi. |
| S5 | Dosya ayak izinde `Pages/Yedekler.razor` yok | BK-30 "son tatbikat tarihi `/yedekler` ekranında görünür" diyor. | `Yedekler.razor`'a tek kart ve bayatlık bandı eklendi. Sunucu tarafında `YedekModulu.cs` + `Contracts/YedeklerSozlesmeleri.cs` genişledi. Plan ile koşul çelişmiyor, DoD bunu gerektiriyor. |
| S6 | K-26 gelmezse kısayol bölümü koşullu | K-26 GELDİ: "kurulumda sorulur". | Kılavuz kesin yazıldı: "kurulumda sorulan adres kısayola yazılır" (testle kilitli). |
| S7 | BK-26 HARİÇ listesi gömülür | `denetim.sh` P-29 bloğu `HARIC_BEYAN_KOPYA` tavanını 2'de bırakmış ve "devir: P-33" yazmıştı (KULLANIM_KILAVUZU.md:396, MIMARI.md:369). | İkisi de kapandı. Kılavuz, HARİÇ tablosunu üretici aracılığıyla gömüyor. MIMARI.md'ye `HARIC-KAYNAK` işareti kondu. Tavan 2 → **0** oldu (cırcır). |

## § 2. Yapılanlar

### 2a. Kılavuz tek kaynak (B-11)
- **`docs/KULLANIM_KILAVUZU.md` — TEK KAYNAK, yeniden yazıldı.** Eski Node/`start.bat`/mobil içeriği kaldırıldı. 44 bölüm var, 47 ekranın hepsi işaretli, menü gruplarıyla düzenlendi. Yeni bir "Bir iş günü" akışı eklendi.
  - Sabit adres/port yazılmadı.
  - Mevzuat hükmü yazılmadı: eşik, oran ya da madde numarası yok, kural soruları mali müşavire yönlendiriliyor. `Kilavuz_mevzuat_ESIK_RAKAMI_tasimaz` testi bunu kilitliyor.
  - Eski HTML'deki "KDV yalnız işçilik+kâra uygulanır" gibi kaynaksız hüküm cümleleri kaldırıldı.
- **`docs/kilavuz-sablon.html` — YALNIZ SUNUM (yeni).**
  - Baş kısım ve CSS yalnız belirteçlerle yazıldı.
  - Tasarım galerisi (dört durum, eski HTML'den birebir) şablona taşındı.
  - Oturumsuz erişim kararı şablonda yazılı.
- **`Kuyumcu.Tests/Araclar/KilavuzUretici.cs` (yeni)** — Markdown'ın küçük bir alt kümesini HTML'e çevirir.
  - Metni kaçırır (escape eder).
  - Her bölümün çıpası zorunludur; aynı çıpa ya da aynı ekran iki kez kullanılırsa üretim durur.
  - Dış bağlantı yasak.
  - HARİÇ listesinin § 1 tablosunu yalnız Kalem ve Durum sütunlarıyla gömer.
  - HTML'e bir üretim damgası basar: md + şablon + HARİÇ sha256.
- **`dotnet/kilavuz-uret.sh` (yeni)** — yeniden üretme yolu. Güncellik testini `KILAVUZ_YAZ=1` ile koşar; ikinci bir üretim yolu yok.
- **`Kuyumcu.Web/wwwroot/kilavuz.html` — ÜRETİLDİ** (62.848 bayt; YayinVarlikDuman testinin 60.000 bayt alt sınırını geçiyor). Mevcut `section id`'ler korundu (tanitim, baslat, giris, arayuz, kur, panel, cariler, urunler, islemler + satis/alis/takas/transfer, kasalar, banka, faturalar, raporlar, kullanicilar, ayarlar, vezne, emanet, siparisler, sayim, pos, uyum, giderpusulasi, yedekler, veriaktarim, galeri, sozluk, sss). Bu sayede `YardimBaglantisi` ve eski yer imleri kırılmadı.
- **`Kuyumcu.Core/Kilavuz/KilavuzCapasi.cs` (yeni)** — rota şablonunu `e-…` çıpasına çevirir. Hem üretici hem `/kilavuz` ekranı bunu kullanır.
- **`Pages/Kilavuz.razor`** — "Ekran ekran" listesi eklendi.
  - Liste `RotaBasliklari.Harita`'dan üretilir: ekran adı Sozluk.Nav'dan, bağlantı yeni sekmede açılır.
  - Yeni id'ler: `#kl-ekranlar`, `#kl-ekran-aciklama`, `#kl-e-<rota>`. Mevcut id'ler (`kl-yeni-sekme`, `kl-aciklama`, `kl-bolumler`, `kl-bolum-*`) değişmedi.
  - iframe yok (Mehmet kararı korundu).
- **`Kuyumcu.Core/I18n/KilavuzSozlugu.cs` (yeni)** — TR+EN metinler: `KilavuzSozlugu` ve `YedekTatbikatSozlugu`. Ortak hub olan `Sozlukler.cs`'e dokunulmadı.
- **Eski port / başlatıcı temizliği:**
  - `docs/DOTNET.md:36`: localhost:5000 ve Node cümlesi kaldırıldı.
  - `docs/MIMARI.md:53, 300, 336-338`: start.bat, localhost:4000 ve tek tık başlatıcı bölümü kaldırıldı; 369'daki HARİÇ kopyası da temizlendi.
  - `docs/DEPLOY.md:4`: start.bat adı kaldırıldı.

### 2b. Temiz kurulum + adres/kısayol (B-5 · K-26)
- **`dotnet/kurulum.sh`** — üç seçenek eklendi: `--adres`, `--kisayol` (yalnız kısayolu yeniden üretir, sırlara dokunmaz), `--kisayol-dizin`.
  - Adres sorulur; varsayılan `http://localhost:5199`. Etkileşimsiz koşuda varsayılan kullanılır ve bu ekrana yazılır.
  - Adres doğrulanır: yol taşıyan ya da şemasız adres 2 koduyla çıkar ve hiçbir dosya yazılmaz.
  - Adres `.env`'de tek satır olarak tutulur: `BYGATECH_ADRES`.
  - Masaüstü kısayolu üretilir: macOS `.webloc`, Linux `.desktop`. Dosya yazıldıktan sonra adresi gerçekten taşıdığı denetlenir; taşımıyorsa betik durur.
  - Bağlama noktası adresten türetilir: `Urls` ve `ASPNETCORE_URLS` (§ 5 bulgu 2).
- **`dotnet/kurulum.ps1`** — Windows ikizi aynı kurallarla yazıldı (`-Adres`, `-SadeceKisayol`, `-KisayolDizin`, `BYGATECH.url`, `Urls`). Bu makinede pwsh yok; yalnız metin sözleşmesi testiyle kilitli.
- **`docs/DEPLOY.md`** — § 3a (adres ve kısayol) eklendi. § 8'de tek yedek yolu ve 30 günlük tatbikat kuralı yazıldı. § 10 kabul listesine 3 madde eklendi.
- **`docs/RUNBOOK-TESLIMAT.md` (YENİ — Köprü K-7 / BK-32)** — içerik:
  - kurulum adımı;
  - adres ve kısayol (K-26);
  - destek kanalı (ürün içi + insan kanalı; insan kanalı KURUCU KALEMİ olarak boş);
  - yedek anahtarı (K-18 KURUCU KALEMİ, boş);
  - **YEREL KAP TATBİKATI** kaydı ve açık kalemler.

### 2c. Yedek + geri yükleme tatbikatı (B-5 · BK-30 · Köprü K-5)
- **Sunucu** (`Modules/Yedek/YedekModulu.cs`):
  - Yeni ayar `YedekAyarlari.TatbikatAzamiGun`. Varsayılan 30, sıkıştırma aralığı **1..30**: yapılandırma kuralı gevşetemez.
  - `YedekPolitikasi.TatbikatBayatMi` (sınır dâhil) ve `TatbikatYasGun` eklendi.
  - `/yedekler/ozet` dört yeni alan döner: `sonBasariliTatbikat`, `tatbikatYasGun`, `tatbikatAzamiGun`, `tatbikatBayat`. Tazelik **yalnız BAŞARILI** tatbikattan ölçülür.
- **Sözleşme** (`Contracts/Sayfalar/YedeklerSozlesmeleri.cs`) — `OzetVeri`'ye aynı adlı ve aynı tipli 4 alan eklendi. Alan gelmezse `TatbikatBayat = true` sayılır (fail-closed). Pages'e yeni record açılmadı.
- **Ekran** (`Pages/Yedekler.razor`) — "Son geri yükleme tatbikatı" kartı eklendi. Tarih kültürlü biçimde basılır. Kart bayatsa kırmızı olur ve `#yd-tatbikat-bayat` uyarı bandı (`role=alert`) çıkar; bant ne olduğunu ve ne yapılacağını söyler. 30 sayısı istemcide gömülü değil, sunucudan gelir.
- **Tatbikatlar** — gerçekten koşuldu, checksum karşılaştırmalı, § 5. `docs/YEDEK-GERI-YUKLEME.md` § 10'a tatbikat kaydı ve `SON_TATBIKAT:` satırı yazıldı.
- **`dotnet/yedek-al-ve-dogrula.sh`** — tek satırlık kitaplık yolu onarımı (§ 5 bulgu 1).

## § 3. DoD sözleşmesi (düzenlenebilirlik)

| Varlık / kayıt | Oluşma | Düzenleme | Pasife alma / silme | Gerekçe |
|---|---|---|---|---|
| Yedek tatbikatı (`yedek_tatbikatlari`) | Betik yazar | YOK | YOK | Geri yükleme kanıtıdır; `DuzeltmePolitikasi`'nda S("YedekTatbikati", Denetim) — DEĞİŞMEZ. Ekranda tablo ve kart salt okunur. Doğru düzeltme yolu yeni tatbikat koşmaktır (bayat bandı komutu söyler). |
| Kurulum adresi (`BYGATECH_ADRES`) | `kurulum.sh` / `.ps1` | `--kisayol --adres <yeni>` / `-SadeceKisayol -Adres` (satır güncellenir, kısayol yeniden üretilir) | — | Yapılandırma satırıdır, ekran varlığı değildir. |
| Kılavuz içeriği | `docs/KULLANIM_KILAVUZU.md` | Kaynak düzenlenir, sonra `kilavuz-uret.sh` koşar | — | HTML elle düzenlenmez: test ve sayaç yakalar. |

Yeni veritabanı varlığı açılmadı, bu yüzden PATCH/DELETE ucu da gerekmedi. Mevcut `/yedekler/ozet` ucu `[RolGerekli("SAHIP")]` ile korunuyor (canlıda TEZGAHTAR → 403). Okuma denetim kaydı mevcut `ListeOkumaKaydet` ile yazılıyor.

## § 4. Testler (yeni: 36 yöntem / 46 durum)

| Dosya | Tür | Durum sayısı |
|---|---|---|
| `KilavuzUretimTestleri.cs` | saf + bUnit | 25 |
| `KurulumAdresKisayolTestleri.cs` | betik gerçekten koşar + metin sözleşmesi | 8 |
| `YedekBetigiCagriTestleri` (aynı dosyada) | betik gerçekten koşar | 2 |
| `YedekTatbikatTazelikTestleri.cs` | saf | 8 |
| `YedekTatbikatTazelikHttpTestleri` (aynı dosyada) | HTTP + DB | 1 |
| `YedekTatbikatEkranTestleri` (aynı dosyada) | bUnit | 2 |

Filtreli koşum: `KilavuzUretim|KurulumAdres|YedekBetigiCagri|YedekTatbikat` → **46/46 Başarılı**.
Etkilenen mevcut testler: `YedekEkran`, `P13Ekran`, `UX4d`, `S3fVarlik`, `S1cUstBar`, `TasarimBelirtec`, `MarkaAdi`, `YayinVarlikDuman`, `UX1C4`, `YedekHttp`, `YedekPolitika`, `YayinDosyaSozlesmesi`, `SozlesmeYansima`, `OluIcBaglanti`, `OlcumKapisi`, `P29`. Koşum **258/258** yeşil.
Sahte API'ler gerçekçi yanıt verecek şekilde güncellendi: `YedekEkranTestleri` ve `P13EkranTestleri` özet JSON'larına 4 yeni alan eklendi.

Tam süit (trx: `scratchpad/trx-pP-33`): **4541 test · 4535 geçti · 6 başarısız · 0 atlandı**. Kırmızıların sahipleri § 9'da.

## § 5. Tatbikatlar (gerçek koşum)

### 5a. YEREL KAP TATBİKATI — temiz kurulum (K-6/K-9 yok → "üretim kanıtı" DENMEZ)
Boş bir MySQL 8.4 kabında (`bygatech-tatbikat-mysql`, `127.0.0.1:3317`, `--rm`) şu sıra koşuldu:
1. `kurulum.sh --adres http://localhost:5197` (çıkış 0).
2. Şema: `dotnet ef database update` → **70 migration**.
3. Uygulama Production ortamında açıldı: `/saglik` **200 Healthy**, `/saglik/hazir` **200 `{"hazir":true,"kod":"HAZIR"}`**, oturumsuz `/api/v1/yedekler/ozet` **401**.
4. Sistem daveti → `auth/kayit` **201** (tenants = 1).
5. `yedek-al-ve-dogrula.sh` çıkış 0: **76/76 tablo CHECKSUM eşleşti**, tatbikat şeması düşürüldü, `t202609231929549543`.
6. Sahip ile `/yedekler/ozet` **200**: `tatbikatBayat=false`, `tatbikatYasGun=0`.
7. Kap durduruldu ve silindi. Sır dosyaları `printf ''` ile boşaltıldı.

**Tatbikatın bulduğu kusurlar (gizlenmedi):**
1. `./dotnet/yedek-al-ve-dogrula.sh` (DEPLOY § 8'deki çağrı) **hiç çalışmıyordu**: `dotnet/yedek-al-ve-dogrula.sh: line 37: dotnet/yedek-ortak.sh: No such file or directory`.
   Onarım: `. "./yedek-ortak.sh"` (betik zaten kendi dizinine `cd` ediyor). Testle kilitlendi: `YedekBetigiCagriTestleri` (depo ve dotnet dizinlerinden).
2. Kurulumun yazdığı `ASPNETCORE_URLS` **etkisizdi**. `appsettings.Production.json` içindeki `"Urls": "http://127.0.0.1:5000"` onu eziyor ve uygulama `127.0.0.1:5000`'i dinliyordu (lsof ile ölçüldü).
   Onarım: kurulum etkin anahtar `Urls`'i de yazıyor. Sonra uygulama `127.0.0.1:5197`'yi dinledi (lsof). Testle kilitlendi.
3. `Sistem__Anahtari` kurulumda üretilmiyor; ilk davet için elle eklenmesi gerekiyor. **Açık** (§ 10).
4. Bu makine macOS olduğundan `yayin-al.sh`'ın linux/win çıktısı ve `kuyumcu-migrate` bundle'ı koşturulamadı. Yayın dışı çalıştırmada statik `kilavuz.html` 404 döner; bu bir kusur değil, çalıştırma biçiminin sonucu.

### 5b. Yedek + geri yükleme tatbikatı — `kuyumcu_net` (canlı geliştirme veritabanı)
`yedek-al-ve-dogrula.sh` (tam + tatbikat) çıkış 0 verdi:
- 76 tablo; 75 tablo doğrudan eşleşti.
- `yedek_kayitlari` dump'tan sonra yazıldığı için "değişken" sayıldı (betiğin ölçülmüş ayrımı). Gerçek uyuşmazlık 0.
- Yedek şifreli, dış kopya alındı. Şema düşürüldü. Süre 36 sn.

SQL kanıtı: `t202609231915063497 · 2026-09-23 19:15:07 · BASARILI · 76/76 · sure 36 · sema_dusuruldu 1`; `y202609231914272146 · BASARILI · DOGRULANDI · sifreli 1 · KOPYALANDI · 76`.

## § 6. Mutasyon tablosu (21/21 kırmızı · hepsi `cmp` birebir)

Yöntem: kaynak `scratchpad/p33/mut-yedek/` altına kopyalandı. `perl -0777 -pi` ile tek bir değişiklik yapıldı ve ilgili filtreli test koşuldu. Derlenen kodda Tests/bin dll'inin mutasyondan sonra yazıldığı denetlendi; eşzamanlı şerit derlemeleri çakıştığında yeniden denendi. Ardından yedek geri kopyalandı ve `cmp` ile doğrulandı.

⚠ **İlk koşumlarda bir yöntem hatası oldu.** Geri yükleme `cp -p` ile yapılınca dosyanın eski mtime'ı geri geliyor ve artımlı derleme bir sonraki mutasyonda eski dll'i kullanıyordu. Bu yüzden M33-03'ün ilk sonucu kirli çıktı. Yardımcı betik onarıldı (geri yüklemede `-p` kaldırıldı, dll damgası denetleniyor), M33-03 yeniden koşuldu ve en sonda temiz derleme alındı.

| Kod | Değişiklik | Kırmızıya dönen test (ad) |
|---|---|---|
| M33-01 | Üretici: çıpasız `##` bölümüne izin | `KilavuzUretimTestleri.Uretici_cipasiz_bolumu_REDDEDER` |
| M33-02 | Üretici: `<` kaçırması kaldırıldı | `Uretici_metni_KACIRIR_kaynaktaki_isaretleme_calismaz` |
| M33-03 | `KilavuzCapasi`: `{Id}` → "kayit" yerine "id" | `KilavuzCapasi_rotadan_TEKIL_capa_uretir` (3 durum), `Her_ekran_capasi_uretilen_HTML_de_gercek_id_olarak_durur`, `Kilavuz_ekrani_HER_rotaya_kilavuzdaki_GERCEK_capayla_baglanir`, `Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL` |
| M33-04 | Kaynaktan `/vitrin` işareti silindi | `RotaBasliklari_HER_rota_kilavuzda_TAM_BIR_kez_isaretli`, `Kilavuz_html_TEK_KAYNAKTAN…`, `Uretim_DAMGASI_kaynak_karmasini_tasir` |
| M33-05 | `kilavuz.html` elle düzenlendi | `Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL` |
| M33-07 | `Kilavuz.razor` ekran bağlantısı çıpasız | `Kilavuz_ekrani_HER_rotaya_kilavuzdaki_GERCEK_capayla_baglanir` |
| M33-08 | `TatbikatBayatMi` `>` → `>=` | `YedekTatbikatTazelikTestleri.Tam_30_gun_once_BAYAT_DEGIL_bir_saniye_fazlasi_BAYAT` |
| M33-09 | Özet: `Sonuc == BASARILI` süzgeci kaldırıldı | `YedekTatbikatTazelikHttpTestleri.Ozet_son_BASARILI_tatbikati_ve_tazeligini_SUNUCUDA_hesaplar` |
| M33-10 | Azami gün sıkıştırması 30 → 3650 | `Yapilandirma_30_gun_sinirini_GEVSETEMEZ` ("31", "90") |
| M33-11 | Yedekler.razor bayat bandı kaldırıldı | `YedekTatbikatEkranTestleri.Bayat_tatbikat_KIRMIZI_UYARI_bandi_ve_hic_yapilmadi_yazar` |
| M33-12 | `kurulum.sh` bağlama noktası sabit 5000 | `KurulumAdresKisayolTestleri.Kurulum_verilen_ADRESI_tek_satira_ve_KISAYOLA_yazar_sirri_EKRANA_basmaz` |
| M33-13 | Kısayola varsayılan adres gömüldü | `Kurulum_verilen_ADRESI…`, `Adres_degisince_KISAYOL_yeniden_uretilir…`, `Kisayol_modu_adres_verilmezse_KAYITLI_satiri_kullanir` |
| M33-14 | Adres doğrulaması kaldırıldı | `Gecersiz_adres_REDDEDILIR_hicbir_dosya_yazilmaz` (3 durum) |
| M33-15 | Yedek betiği eski kitaplık yoluna döndürüldü | `YedekBetigiCagriTestleri.Yedek_betigi_HER_cagri_dizininden_kitapligini_bulur(depo)` |
| M33-16 | Kaynağa `http://localhost:5199` yazıldı | `Kullanici_belgesinde_SABIT_PORT_ve_eski_baslatici_YOK` (+ güncellik ve damga) |
| M33-17 | Kurulum `Urls` satırını yazmıyor | `Kurulum_verilen_ADRESI…`, `Adres_degisince_KISAYOL…` |
| D33-1 | (denetim) `/vitrin` işareti silindi | `KILAVUZ_EKSIK_EKRAN 1 — /vitrin` + `KILAVUZ_URETILMIS 0` |
| D33-2 | (denetim) kaynak değişti, üretim yapılmadı | `KILAVUZ_URETILMIS 0` |
| D33-3 | (denetim) DOTNET.md'ye `start.bat` yazıldı | `ESKI_PORT_REFERANSI 1` |
| D33-4 | (denetim) DEPLOY.md'ye ham `mysqldump` komutu yazıldı | `YEDEK_TEK_YOL 0 — 1 ihlal` |
| D33-5 | (denetim) `SON_TATBIKAT` 2026-08-01 yapıldı | `YEDEK_TATBIKAT_BAYAT 1 (kayıt=53gün)` |

(M33-06, D33-2 ile aynı kuralı ölçtüğü için ayrı numara almadı.) Denetim mutasyonları `denetim.sh`'ın `# >>> P-33` bloğu, betiğin kendi metninden `sed` ile çıkarılarak koşuldu. Son mutasyondan sonra `dotnet build Kuyumcu.sln` **0 uyarı / 0 hata** verdi.

## § 7. denetim.sh

`# >>> P-33` bloğu eklendi. Her sayacın ölçüm komutu ve TAZE tabanı blok yorumunda yazılı.

```
  ✓ P-33: KILAVUZ_EKSIK_EKRAN                      0 (47 rota, hepsi kılavuzda)
  ✓ P-33: KILAVUZ_URETILMIS                        1 (damga = md + şablon + HARİÇ karması)
  ✓ P-33: ESKI_PORT_REFERANSI                      0
  ✓ P-33: YEDEK_TEK_YOL                            1 (belgelerde ikinci yedek komutu yok)
  ✓ P-33: YEDEK_TATBIKAT_BAYAT                     0 (azami 30 gün; kayıt=0gün canlı=0gün)
  ✓ P-29: HARIC_BEYAN_KOPYA                        0 (tavan 2 → 0, cırcır — P-33 devri kapandı)
```

- **Ara bulgu:** blok ilk hâlinde `T-3: dump çağrı noktası` sayacını 2'ye çıkardı; `denetim.sh`'ın kendi regex satırı sayılıyordu. Regex `[^#|]` ile düzeltildi ve T-3 sayacı yeniden 1 oldu.
- **Ara bulgu:** `Yedekler.razor`'daki ilk tarih biçimi `Kültürsüz ToString` sayacını 28'e çıkarmıştı. Biçim `CultureInfo(Dil.Locale)` ile düzeltildi ve sayaç 27 (tavan) oldu.
- **Son tam koşum:** `DENETİM BAŞARISIZ`, tek ✗ var: `P-36b: GIRDI_AILESI_DISI_BICIM 1`. Sebebi `Pages/Panel.razor:646`'daki `ToString("N2"…)`; Panel şeridinin dosyasıdır ve bu pakete ait değildir (§ 9).
- **Ağaç ölçüsü:** "denetim TEMİZ" koşulu ağaç ölçüsüdür ve orkestratörün kapısıdır; bu paketin sayaçlarının hepsi ✓.

## § 8. Canlı doğrulama (5198 · Development) + SQL

Kiracı `PAKETP33-TEST Kuyumculuk` (`01M37Z1VGWDXRGGRH4KAH6GWFR`) sistem davetiyle kayıt yoluyla açıldı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| C1 `GET /kilavuz.html` (oturumsuz) | 200, üretilmiş, betiksiz | **200** `text/html`, 62.848 bayt, servis edilen dosya diskteki dosyayla **birebir** (`cmp`), `kaynak-sha256` = `shasum docs/KULLANIM_KILAVUZU.md` (5dd4a5f2…), 47 `ekran-capa`, `<script` 0 |
| C2 davet + kayıt | 201 | davet `BEKLIYOR` → kayıt **201** |
| C4 SAHİP `GET /api/v1/yedekler/ozet` | 200, tatbikat taze | **200** `sonBasariliTatbikat=2026-09-23T19:15:07`, `tatbikatYasGun=0`, `tatbikatAzamiGun=30`, `tatbikatBayat=false`, `dogrulama=DOGRULANDI`, `disKopya=KOPYALANDI` |
| C5 TEZGAHTAR `GET /yedekler/ozet` | 403 | kullanıcı **201** → **403** "Bu işlem için gerekli rol: SAHIP" |
| C6 oturumsuz `GET /yedekler/ozet` | 401 | **401** "Token bulunamadı" |
| C7 `GET /kilavuz` (SPA) | 200 | **200** `<title>BYGATECH</title>` |

SQL kanıtı:
```
son_basarili  t202609231915063497  2026-09-23 19:15:07  76  76
gun           0
kiraci        01M37Z1VGWDXRGGRH4KAH6GWFR  PAKETP33-TEST Kuyumculuk
```

Temizlik: API durduruldu (`lsof :5198` boş).
- `betikler/kiraci-temizle.sh … --uygula`: 70 tablodan 22 satır silindi, `tenants` satırı da silindi.
- Kendi `kiraci_davetleri` satırım silindi (row_count 1).
- Sonuç: **tenants = 10** (başlangıçla aynı), PAKETP33 kiracısı 0, PAKETP33 daveti 0. QA-TEST-* ve DEMO-Vitrin kiracılarına dokunulmadı.

Ek not:
- 5198'in ilk açılışı ve ilk davet isteği, başka şeridin eşzamanlı derlemesi `bin/`'den `System.IdentityModel.Tokens.Jwt.dll`'i geçici olarak düşürdüğü için başarısız oldu (**500**; günlükte `FileNotFoundException`).
- `dotnet build Kuyumcu.Api` sonrası dll geri geldi ve adımlar yeniden koşuldu.
- **5199'a dokunulmadı.**

## § 9. Açık kalanlar

1. **Tam süitteki 6 kırmızı başka şeritlerin dosyalarında** (bu paketin yeni kırmızısı **0**):
   - `S1fGorunumKartiEkranTestleri.S1f_E11_…`, `S1fGorunumKaynakTestleri.S1f_K3_…` — S-1 şeridi (S1-f görünüm kartı / belirteçler).
   - `B4AltinBacagiTestleri.AltinBacagi_GiderPusulasi_Ayni_Committe_Dogar` — B-4 şeridi.
   - `S31PanelSadeTestleri.S31_23_…` — S-3.1 Panel şeridi.
   - `S21FisSayfaTestleri.Takas_eksikken_…` ve `P06DuzenlenebilirlikTestleri.P06_YAZAN_…` (Islemler.razor) — S-2.1 fiş şeridi.
   - P-31'in devir notundaki 3 kırmızı (S1e, S1c) bu koşumda yeşildi.
2. **denetim ✗ `P-36b: GIRDI_AILESI_DISI_BICIM 1`** — `Pages/Panel.razor:646` (Panel şeridi).
3. **`Sistem__Anahtari` kurulumda üretilmiyor.** Temiz kurulumda ilk davet için elle ekleniyor (RUNBOOK-TESLIMAT § 1). Sır setine eklenmesi Mehmet onayına bağlı → **devredildi: P-35 öncesi Mehmet turu**.
4. **`kurulum.ps1` gerçek koşum** — bu makinede pwsh yok; metin sözleşmesiyle testli. Windows makinesinde koşum → **devredildi: Batuhan (pilot kurulumu)**.
5. **docker-compose.uretim.yml gözlemi (kapsam dışı, düzeltilmedi):** compose `ASPNETCORE_URLS: http://0.0.0.0:5000` veriyor. Ancak § 5 bulgu 2'ye göre `appsettings.Production.json`'daki `"Urls": "http://127.0.0.1:5000"` bu değeri ezer. Böyle olursa kap içinde yalnız loopback dinlenir ve `127.0.0.1:5000:5000` yayını dışarıdan ulaşılamaz olabilir. Docker yolunda **ölçülmedi** → **devredildi: Batuhan (T-1 docker yolu)**.
6. **Yardım bağlantısı:** `Layout/YardimBaglantisi.cs` 22 ilk kesimi eski çıpalara bağlıyor; eşleşmeyen ekranlar kılavuzun başına gidiyor. Artık her ekranın `e-…` çıpası var, ama dosya S-1 şeridinin (S1-c) ve bu pakette dokunulmadı → **devredildi: S-1**.
7. **Kurucu kalemleri:** K-6/K-9 (bulut ve TLS → tatbikat yerel kalır), K-18 (yedek anahtarının sunucu dışında saklanması), K-19 (destek insan kanalı). RUNBOOK-TESLIMAT'ta boş satır olarak yazılı.

## § 10. Kurucuya / mali müşavire notlar
- **Mevzuat:** bu paket hiçbir vergi, MASAK veya e-belge hükmü yazmadı. Kılavuzdaki eski "Kuyum-KDV: KDV yalnız işçilik+kâra" gibi kaynaksız hüküm cümleleri **kaldırıldı**; yerine "ayardan gelir, mali müşavirinize teyit ettirin" yazıldı. Birincil kaynak araştırması gerekmedi.
- **Sır disiplini:** kurulum ve tatbikat sırları yalnız scratchpad'deki geçici `.env` dosyalarında durdu ve iş bitince `printf ''` ile boşaltıldı. Hiçbir sır ekrana ya da rapora basılmadı. Tatbikat yedekleri (şifreli `.gpg`) scratchpad'de duruyor: `p33/yedek`, `p33/yedek-dis`, `p33/kap/yedek*`. Anahtarları boşaltılmış değişkenlerdeydi, dosyalar açılamaz. Silinmeleri kurucuya bırakıldı (rm kancası).

## § 11. Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-11** Kılavuzun tek kaynaktan yeniden yazımı | **KAPANDI** | `docs/KULLANIM_KILAVUZU.md` (tek kaynak, 47/47 ekran) → `dotnet/kilavuz-uret.sh` → `wwwroot/kilavuz.html` (damgalı). Testler: `Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL`, `RotaBasliklari_HER_rota_kilavuzda_TAM_BIR_kez_isaretli`, `Ekran_isaretinin_basligi_ekranin_MENU_ADINI_tasir`, `Kilavuz_ekrani_HER_rotaya_kilavuzdaki_GERCEK_capayla_baglanir`. Sayaçlar: `KILAVUZ_EKSIK_EKRAN 0`, `KILAVUZ_URETILMIS 1`, `ESKI_PORT_REFERANSI 0`. Canlı: C1 (servis edilen = diskteki, karma eşleşiyor). |
| **B-5** Temiz makinede sıfırdan kurulum tatbikatı | **KAPANDI (YEREL KAP TATBİKATI)** · üretim/bulut tatbikatı **devredildi: K-6/K-9 → pilot kurulumu** | `docs/RUNBOOK-TESLIMAT.md` § 5 (boş kap → kurulum.sh → 70 migration → Production açılışı 200/200 → ilk kiracı 201 → yedek+geri yükleme 76/76 → `/yedekler` taze). `docs/YEDEK-GERI-YUKLEME.md` § 10 kayıt + `SON_TATBIKAT`. Testler: `KurulumAdresKisayolTestleri` (8), `YedekBetigiCagriTestleri` (2), `YedekTatbikatTazelik*` (11). Sayaçlar: `YEDEK_TEK_YOL 1`, `YEDEK_TATBIKAT_BAYAT 0`. Canlı: C4/C5/C6. |

## § 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kılavuz RotaBasliklari'ndan üretilir, her ekran var | ✓ | § 2a, § 11 B-11 |
| Eski port/adres referansları kalkar; kullanıcı belgesinde sabit port yok | ✓ | `ESKI_PORT_REFERANSI 0`; `Kullanici_belgesinde_SABIT_PORT_ve_eski_baslatici_YOK` |
| Temiz kurulum tatbikatı kaydedilir, mevcut betiklerle; K-6/K-9 yok → "YEREL KAP TATBİKATI" | ✓ | RUNBOOK-TESLIMAT § 5 başlığı; "üretim kanıtı denmez" yazılı |
| Yedek + geri yükleme tatbikatı checksum ile; son tatbikat `/yedekler`'de, 30 günden eski olamaz | ✓ | § 5b SQL; C4; `YEDEK_TATBIKAT_BAYAT 0`; M33-08/09/10/11 |
| Oturumsuz erişim kararı yazılı; Kilavuz.razor iframe kullanmaz | ✓ | `kilavuz-sablon.html` "OTURUMSUZ ERİŞİM KARARI"; `Kilavuz_BETIK_CERCEVE_ve_veri_cagrisi_TASIMAZ…`; `M14_Kilavuz_sayfasinda_IFRAME_YOKTUR` yeşil; C1 oturumsuz 200 |
| K-26 → kılavuz kesin yazılır, kurulum adresi sorar, kısayolu üretir, tek satır | ✓ | `Kisayol_adresi_bolumu_KESIN_yazilir_K26`; `KurulumAdresKisayolTestleri`; tatbikat `BYGATECH.webloc → http://localhost:5197` |
| RUNBOOK-TESLIMAT (kurulum, adres/kısayol, destek, yedek anahtarı K-18) | ✓ (destek insan kanalı ve K-18 KURUCU KALEMİ olarak boş satır) | `docs/RUNBOOK-TESLIMAT.md` |
| Sayaçlar (5) cırcır, taze taban, ölçüm komutu yorumda | ✓ | § 7 |
| BK-26 HARİÇ gömülür; HARIC_BEYAN_KOPYA | ✓ tavan 2 → 0 | § 1 S7 |
| Migration yok | ✓ | şema değişmedi |
| Yeni istemci record'u Pages'e açılmadı; ad+tip eşleşiyor | ✓ | `OzetVeri` Contracts'ta genişletildi; `SozlesmeYansimaTestleri` yeşil |
| Mutasyon kanıtı | ✓ 21/21 (16 kod + 5 sayaç) | § 6 |
| Canlı doğrulama 5198 + SQL | ✓ | § 8 |
| Test kiracısı silindi | ✓ tenants 10 → 10 | § 8 |
| Paket yeni kırmızı üretmedi | ✓ 0 | § 9 |

## § 13. Dokunulan sayfalar (TEK DOKUNUŞ)
`Pages/Kilavuz.razor` · `Pages/Yedekler.razor` (BK-30 gereği, § 1 S5)

## § 14. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)
```
 M docs/DEPLOY.md
 M docs/DOTNET.md
 M docs/KULLANIM_KILAVUZU.md
 M docs/MIMARI.md
 M dotnet/Kuyumcu.Web/Pages/Kilavuz.razor
 M dotnet/Kuyumcu.Web/wwwroot/kilavuz.html
?? docs/RUNBOOK-TESLIMAT.md                      (YENİ)
?? docs/kilavuz-sablon.html                      (YENİ)
?? docs/YEDEK-GERI-YUKLEME.md                    (§ 10 eklendi; dosya depoda izlenmiyor)
?? dotnet/Kuyumcu.Core/Kilavuz/KilavuzCapasi.cs  (YENİ)
?? dotnet/Kuyumcu.Core/I18n/KilavuzSozlugu.cs    (YENİ)
?? dotnet/kilavuz-uret.sh                        (YENİ)
?? dotnet/kurulum.sh · dotnet/kurulum.ps1 · dotnet/yedek-al-ve-dogrula.sh · dotnet/denetim.sh   (izlenmeyen, değişti)
?? dotnet/Kuyumcu.Api/Modules/Yedek/YedekModulu.cs · dotnet/Kuyumcu.Web/Pages/Yedekler.razor     (izlenmeyen, değişti)
   dotnet/Kuyumcu.Contracts/Sayfalar/YedeklerSozlesmeleri.cs                                    (değişti)
   dotnet/Kuyumcu.Tests/Araclar/KilavuzUretici.cs · KilavuzUretimTestleri.cs ·
   KurulumAdresKisayolTestleri.cs · YedekTatbikatTazelikTestleri.cs                              (YENİ)
   dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs · P13EkranTestleri.cs                              (sahte özet JSON'u)
```
