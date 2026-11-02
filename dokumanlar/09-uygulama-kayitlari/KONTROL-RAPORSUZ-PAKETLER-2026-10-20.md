# Kontrol — "uygulama raporu olmayan" paketler (2026-10-20)

> Yöntem: her paketin tanımı (`dokumanlar/02-mimari/kalan-paketler/`) okundu; kapsam maddeleri kodda
> (controller rotaları, razor sayfaları, servis sınıfları, testler) ve `git log`'da arandı. Kod DEĞİŞTİRİLMEDİ.
> Ön not: **PAKET-01/03/04/06 için rapor VAR** — dosya adları sıfırsız: `PAKET-1-uygulama-raporu.md`,
> `PAKET-3-…`, `PAKET-4-…`, `PAKET-6-…` (hepsi 2026-09-16, yalnız **Faz A**). Aşağıda bu dördünde Faz B denetlendi.

## PAKET-01 — Fiş / kalem modeli, fatura satır kalemi
- Faz A BİTTİ (rapor `PAKET-1-uygulama-raporu.md`: `Data/FaturaKalemi.cs`, `faturalar.kalem_kaynagi`, `POST /faturalar/fisten`, `GET /faturalar/{id}/kalemler`).
- Faz B-1 VAR: `GET /api/v1/islemler/fis/{fisId}` → `SatisModulu.cs:3265` (`FisOku`, :3005); fiş kalemleri + ödeme bacakları (`KuyumcuDbContext.cs:478`).
- Faz B-2 KISMEN: fiş kimliği `SatisModulu.cs` içinde 14 yerde (satış/takas), `HurdaModulu.cs`'de `FisId` 0 → hurda alışı fiş çatısına yayılmamış.
- Faz B-3 EKSİK: `RaporModulu.PosFaturaMutabakati` (:492) kalem/`KalemKaynagi` okumuyor; kalemli faturayla doğrulama yok.
- **Karar: KISMEN** (Faz B 1 tam / 1 kısmi / 1 yok). Boy **S**.

## PAKET-03 — Sipariş çatısı (durum makinesi)
- Faz A BİTTİ (rapor `PAKET-3-uygulama-raporu.md`; `Modules/Siparis/SiparisModulu.cs`, `Pages/Siparisler.razor` `/siparisler`, `SiparisDetay.razor` `/siparisler/{Id}`, tezgâhta `Bilesenler/Fis/FisDigerSiparis.razor`).
- Faz B hareket referansları VAR: `Data/Siparis.cs:72` `EmanetHareketId`, `:104` `KaporaHareketId`; kapora makbuzu bağlantısı `SiparisModulu.cs:681`.
- Faz B geciken/özet KISMEN: liste yanıtında `gecikenSayisi` + `durumKirilimi` (`SiparisModulu.cs:273-308`); ayrı `GET /siparisler/ozet` ucu yok.
- Faz B belge bağlama YOK: `Modules/Urun/BelgeModulu.cs:93` hâlâ `throw "Sipariş modülü henüz yok — SIPARIS sahibine belge bağlanamaz"`; `SiparisDetay.razor`'da `BelgeYukleyici` yok.
- **Karar: KISMEN** (belge bağlama + özet ucu eksik). Boy **S**.

## PAKET-04 — Dosya yükleme / belge deposu
- Faz A BİTTİ (rapor `PAKET-4-uygulama-raporu.md`; `belgeler` tablosu, magic byte, `Bilesenler/BelgeYukleyici.razor`, `/api/v1/belgeler`).
- Faz B ürün görseli VAR: `Bilesenler/UrunGorselGalerisi.razor`, `Urunler.razor:1021` (düzenleme) ve `:1253-1260` (liste "gorsel" sütunu). POS fiş kalemlerinde (`Fis/FisKalemTablosu.razor`, `FisProKalemKarti.razor`) görsel YOK.
- Faz B gider pusulası eki YOK: `BelgeModulu.cs` `GiderPusulasi` dalı "ayrı karara bağlandı (ADR-0008 blob karşılığı) — henüz açık değil" fırlatır (Mehmet K4-3 vetosu). `GiderPusulalari.razor`'da yükleyici yok.
- Faz B işletme logosu KISMEN: sunucu `BelgeSahipTuru.Isletme` dalı açık (`BelgeModulu.cs:86`), ama `Pages/Isletme.razor`'da logo yükleme ve `Cikti/CiktiModulu.cs`'de logo basımı yok.
- Tanım dışı eklenenler: `HURDA_ALIS` fotoğrafı (D1-12, `Hurda.razor:497`), kimlik tespit / garanti kartı imzası (D1-13, `js/imza.js`).
- **Karar: KISMEN** (logo yükleme/basım, pusula eki kararla kapalı, POS görseli). Boy **S**.

## PAKET-05 — Belge / etiket şablon motoru
- Tanımdaki yapı YOK: `isletme_kunyesi`, `belge_sablon_ayarlari`, `Kuyumcu.Core/Domain/BelgeYerlesimleri.cs`, `/ayarlar/belge-sablonlari`, `/api/v1/belge-sablonlari` — hiçbiri kodda yok.
- Aynı ihtiyaç BAŞKA mimariyle karşılandı: `Modules/Cikti/CiktiModulu.cs` + `Pages/Cikti.razor` `/cikti/{Tur}/{Id}` tek A4 şablon (P-17) + 80 mm termal seçici (D1-01); belge türleri `Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs` (SATIS_FISI, TAHSILAT_MAKBUZU, EMANET_TESLIM, CARI_EKSTRE, IRSALIYE, FATURA, CARI_MUTABAKAT, KVKK_AYDINLATMA, KIMLIK_TESPIT, GARANTI_KARTI, ODEME_PLANI, UYUM_RAPORU) + `/gider-pusulalari/{Id}/belge`.
- Künye: `Tenant` kimlik alanları (`Entities.cs:165-178` adres, vergi dairesi, vergi no, telefon — UX-1-B); unvan = `Tenant.Ad`; `HurdaModulu.cs:171` buradan beslenir. Logo yok.
- Etiket: D1-02 (`Modules/Urun/EtiketSablonuModulu.cs`, `Bilesenler/EtiketSablonuCekmecesi.razor`, hazır boylar, QR, ZPL) → tanımın Faz C etiket maddesini aşıyor.
- Eksik: sayım tutanağı (`SayimModulu` içinde tutanak yok), kasa gün kapanış fişi (`Kasa/` içinde yok), belge türü başına yerleşim ayarı/önizleme ekranı, logo.
- **Karar: KISMEN** (eşdeğer motor var; tutanak, kapanış fişi, logo, tür başına ayar yok). Boy **M**.

## PAKET-06 — Bildirim kanalı / zamanlanmış iş
- Faz A BİTTİ (rapor `PAKET-6-uygulama-raporu.md`); bugün 9 iş: `Modules/Isler/Isler/` (VADE, MASAK_SIB, SAKLAMA_ONIZLEME, SAKLAMA_UYGULA, TEVSIK_KURTARMA, TAKSIT, STOK_ESIK, STOPAJ, KASA_KAPANIS).
- Faz B TEVSIK_KURTARMA VAR: `TevsikKurtarmaIsi.cs` (YASAL-2; `appsettings.json:163` varsayılan AÇIK — tanımdaki "KAPALI" kararı VUK mük. 355 dayanağıyla değiştirilmiş).
- Faz B iş çalışmaları ekranı: ayrı `/bildirimler/is-calismalari` yok; `GET /isler`, `POST /isler/{kod}/calistir`, `PATCH /isler/ayarlar` `Pages/Bildirimler.razor:164-219` içinde gösteriliyor (eşdeğer).
- Faz B WhatsApp derin bağlantısı YOK (`wa.me` 0 eşleşme). Dış kanal tekeli korunuyor (`ModulKayit.cs:125-127`, tek `UygulamaIciKanal`).
- **Karar: KISMEN** (yalnız WhatsApp bağlantısı eksik). Boy **S**.

## PAKET-36 — Patron paneli
- `GET /raporlar/patron-paneli` ucu ve `/patron` ekranı YOK (grep 0).
- Karşılığı dağınık: `Raporlar.razor` Basit yüz "Bugün" (S3-4: sattım · aldım · altın pozisyonu · kasa · bekleyen iş · gün sonu), `GET /raporlar/gunluk|aylik` (`RaporModulu.cs:1550-1557`), `GET /raporlar/sube-karsilastirma` (`SatisRaporlari.cs:545`), `/api/v1/pozisyon`, kâr-zarar.
- Karar belgesi: `KAPSAM-ENVANTERI.md` D-9 → **1.1'e ertelendi** (`MIMARI-KALAN-PAKETLER-2026-09-15.md:27` KOŞULLU KABUL).
- **Karar: KAPSAM DIŞI (1.1)** — birleşik uç/ekran yok, parçaları var. Boy **M**.

## PAKET-37 — Pozisyon / net varlık ekranı
- `Modules/Pozisyon/PozisyonModulu.cs` → `GET /api/v1/pozisyon` + `/pozisyon/cariler` (paket B-2, rapor `PAKET-B-2-uygulama-raporu.md`; şube parametresi SUBE-KAPSAM-B). Ekranda Panel/Raporlar "altın pozisyonu" kartı.
- Kapsanan: vitrin + konsinye has (konsinye hariç tutulur), cari has alacak/borç, emanet nötr, maden ayrımı (`MADEN-AYRIMI`).
- Eksik: para birimi bazında kasa+banka toplamı ve TL net varlık (`PozisyonModulu`'nde kasa/banka 0 eşleşme), maliyeti bilinmeyen stok beyanı/güvenilirlik satırı. Karar: D-10 → 1.1.
- **Karar: KISMEN** (altın pozisyonu var, net varlık/çok para birimi yok). Boy **M**.

## PAKET-39 — Has altın defteri otomatiği
- `GET /raporlar/has-defteri*`, `has_defteri_donemleri`, ekran: YOK (grep 0). `KarZararServisi.cs:213-221` yalnız "hasılat has defter gramı" ölçümü.
- Karar: `PAKET-P-01-uygulama-raporu.md:107` R-12 "paket 39 has defteri dökümü → `surum.json` 39 → 1.1". `KARAR-HAS-DEFTERI-2026-09-23.md` + B-4/G-5 yalnız kâr-zarar modelini belgeler.
- **Karar: KAPSAM DIŞI (1.1'e ertelendi) — kodda YOK.** Boy **L**.

## PAKET-42 — Kâğıt sevk irsaliyesi
- Başka modelle VAR (P-26 / Y-35, rapor `PAKET-P-26-uygulama-raporu.md`): `Data/Tedarik.cs:167` `Irsaliye` (GELEN/GIDEN; ACIK/FATURALANDI/IPTAL), `IrsaliyeKalemi`, `Modules/Irsaliye/IrsaliyeModulu.cs` (liste, `bagsiz`, oluştur, PATCH, fatura bağı/kaldır, aktife al, iptal gerekçeli), `Pages/Irsaliyeler.razor` `/irsaliyeler`, çıktı `/cikti/IRSALIYE` ("SEVK İRSALİYESİ" / "MAL KABUL İRSALİYESİ", `CiktiModulu.cs:420`). Faz B "faturası kesilmemiş irsaliye" uyarısı → `bagsiz` ucu.
- Tanımdan eksik: TASLAK→SEVK_EDILDI→TESLIM_EDILDI durum makinesi; taşıyan/plaka/fiili sevk tarihi/teslim alan alanları (entity'de yok); SUBE_TRANSFER/ATOLYE_GONDERIM/KONSINYE türleri; `raporlar/irsaliyesiz-sevk` (sevk edilmiş ama irsaliyesiz hareket) raporu; satış/transfer akışından "irsaliye oluştur" kısayolu (Islemler/Faturalar'da 0).
- **Karar: KISMEN** (ticari irsaliye var, VUK sevk alanları ve irsaliyesiz sevk raporu yok). Boy **M**.

## PAKET-43 — Mobil dar ekran uyarlaması
- Tanımdaki adlar (`YerlesimDurumu`, `.kart-mod`, `MenuyuAc`) yok; işlev başka paketlerle geldi: hamburger `UstBar.razor:15` `#ust-menu-ac` (`Kabuk.MenuAcik`, `app.css:961-964` ≤768 px'te görünür), `app.css` 52 `@media`, tüm CSS'te 119 dar ekran kuralı, `@media (max-width:768px)` çekmece 100 vw (`app.css:932`).
- Ölçüm kanıtı: `olcumler/P-18-mobil-375-*.json`, YILDIZ-1/2/3/4A ve UX-2-A2 raporlarında 375 px; CLAUDE.md gerçek tarayıcı kuralı (1280+375) her ekran paketinde zorunlu.
- **Karar: BİTTİ (eşdeğer; ayrı rapor yok, kanıt dağınık).** Boy —.

## PAKET-44 — PWA kabuğu
- D3-01 (commit `635d216`, 19.10) ile KISMEN: `wwwroot/manifest.json` (standalone, 192/512/maskable), `icon-192/512.png`, `apple-touch-icon.png`, `index.html:74-79` iOS meta; test `Kuyumcu.Tests/D3PwaTestleri.cs`. `.json/.html/.js` için `Cache-Control: no-cache` (`Program.cs:584-591`).
- Eksik: service worker (csproj'da `ServiceWorkerAssetsManifest` yok, `wwwroot`'ta SW yok, `js/pwa.js` yok), CSP'de `manifest-src`/`worker-src` yok (`Program.cs:494-505`), nginx/IIS manifest MIME/no-cache satırı yok, Faz B (`beforeinstallprompt` "Ana ekrana ekle", "Yeni sürüm hazır" bandı, çevrimdışı göstergesi) yok. Karar: D-11 → 1.1.
- **Karar: KISMEN** (manifest/ikon tamam, SW ve Faz B yok). Boy **S-M**.

## PAKET-PWA-1 — PWA asgarisi
- Aynı kanıt: manifest + ikonlar + iOS meta VAR (D3-01). Eksik: SW (dev boş / yayın kabuk), `kuyumcuPwa.baslat()` ve reddetme listesi, CSP ekleri, `denetim.sh` JS/HTML bloğu, Faz B düğme/bant.
- **Karar: KISMEN** (≈1/3). Boy **S**.

## PAKET-46 — Şube bazlı görünürlük / yetki matrisi
- Faz B şube kapsamı BİTTİ başka adla: `PAKET-SUBE-KAPSAM-A/B` raporları; `Common/GorunumSubesi.cs` (`Coz`/`Daralt`, 15 okuma ucu), `SubeYetkisi.TumSubeler`, `SubeSecici` (UstBar → kullanıcı kartı menüsü), `GET /pozisyon?subeId`, tevsik/MASAK kümülatifi kiracı geneli.
- Faz A kısmen: kullanıcı başına iskonto sınırı `Kullanici.IskontoSiniriYuzde` (`Entities.cs:307`, D1-05) + iskonto/zararına/tutar eşik onayı (`Modules/Satis/SatisOnayi.cs`); salt okunur muhasebe hesabı (D1-14).
- Eksik: KASIYER/CIRAK rolleri (0 eşleşme), `kullanici_yetkileri` tablosu, maliyet/kâr görme kapısı (`MaliyetGorur`/`KarGorur` 0), `/yetkiler` ince yetki sekmesi, `GET/PUT /yetkiler/kullanicilar`, reddin denetim izine REDDEDILDI yazılması (yalnız Admin/Destek'te).
- **Karar: KISMEN** (şube tarafı tam, rol/ince yetki tarafı eksik). Boy **M**.

## PAKET-E-BELGE — e-Fatura / e-Arşiv
- Büyük ölçüde VAR (commit `320edd8`, `8bd3946`, `e52f0c2`; 19-20.10): `Modules/EBelge/` — `EBelgeKurucu.cs` (UBL-TR, özel matrah kodu fail-closed :55), `EBelgeSaglayici.cs` (`IEBelgeSaglayici`, `NilveraSaglayici`: gönder, GİB durum, `MukellefSorgula` :141, PDF), `EBelgeIscisi` BackgroundService (arka plan gönderim/yeniden deneme), `Data/EBelge.cs` (`ebelge_ayarlari` şifreli anahtar, `ebelge_gonderimleri` ETTN/durum/deneme), 8 gün e-Arşiv iptali (:21-30), `Sablon/bygatech-fatura.xslt`; rotalar `api/v1/ebelge/{ayar GET/PUT/DELETE, gonderimler, faturalar/{id}, yeniden-dene, pdf}`; ekran `Faturalar.razor:924-933` durum + `Bilesenler/EBelgeCekmecesi.razor`, `Isletme.razor:423`; 4 test dosyası (`EBelge*Testleri.cs`). Mükellef kimliği: `Tenant.VergiNo/VergiDairesi/Adres` (unvan = Ad).
- Tanımdan sapan/eksik: XSD/şematron doğrulaması yok (`XmlSchema` 0); `e_belge_olaylari` append-only olay izi yok (tek satırda durum); XML indirme ucu yok (PDF var); ayrı `/e-belgeler` sayfası yok (çekmece); `tenants.sube_kodu`/`unvan` kolonu yok.
- Dış bağımlılık (DURUM-OTURUMLAR 20.10): Nilvera test hesabı (kurucu), özel matrah kodu K-10 mali müşavir teyidi, 5199 tarayıcı görüntüsü.
- **Karar: KISMEN (≈%85; kalan S)**. Boy **S**.

## PAKET-F-8 — Birim ekonomi / fiyatlandırma dosyası
- Belge paketi, kod üretmez. ÇIKTI VAR: `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md` (Faz A; 19 kaynaklı maliyet satırı, lisans 0 TL kanıtı, üç kademe öneri, MÜHÜR 2 ilan kilidi) + `09-uygulama-kayitlari/ham/f8-birim-ekonomi.json`; `CLAUDE.md` § MÜHÜR 2-4 referansı.
- Kalan: entegratör/mühür/yaptırım listesi yazılı teklifleri (kurucu kalemi, K-2/K-3).
- **Karar: BİTTİ (Faz A belge; teklif toplama kurucuda).** Boy —.

## PAKET-UX-4 — Mantık ve tutarlılık düzeltmeleri
- Tek tanım, dört alt rapor: `PAKET-UX-4a` (madde 1-5 + 14), `UX-4b` (6-9), `UX-4c` (10-13 + Q4 + dalga 1 taşınanları), `UX-4d` (QA turu 1'in 24 maddesi, "24/24 uygulandı, 82 test, 27 mutasyon"). Tanımın 14 "hemen düzeltilecek" maddesi + Mühür 4 eki (madde 24 kur kaynakları) kapalı; `SONRAKI-OTURUM-PROMPT.md:895` "UX-4d GEÇTİ (1394 test)".
- **Karar: BİTTİ** (rapor adı alt paketlerde). Boy —.

## Ek özellik taraması
| Özellik | Kodda | Kanıt / not |
|---|---|---|
| Zekât hesabı | **YOK** | `zek[aâ]t` 0 eşleşme; rakip planı D3-10 kuyruğu (S). |
| Doğum günü / vade mesajı | **KISMEN** | Doğum tarihi yalnız alan (`CariModulu.cs:77`, kimlik tespit çıktısı); mesaj yok. Vade: yalnız uygulama içi bildirim (`VadeHatirlatmaIsi`, `TaksitHatirlatmaIsi`); SMS/WhatsApp/e-posta kanalı yok (tek `UygulamaIciKanal`); D2-10/D2-11 commit'i yok. |
| E-posta gönderimi (SMTP) | **YOK** | ADR-0016: "Gerçek SMTP bu üründe YOK" (`ParolaSifirlamaModulu.cs:74-103`, dosya postası yalnız Development/Testing); csproj'da MailKit yok. |
| PDF katalog | **YOK** | `/vitrin`, `/vitrin/ekran` pano + fiyat listesi (paket 49) var; PDF kütüphanesi yok; tek PDF entegratörün resmi PDF'i (`ebelge/faturalar/{id}/pdf`). D2-11 içinde. |
| Fiyat devre kesici | **YOK** | `KurKilidiModulu.cs` (D1-04 süreli kur kilidi) ve 3 dk kur tazeliği var; ani dalgalanma kesicisi yok (D3-04). |
| Toptan çanta takibi | **YOK** | Reyon tipi `CANTA` (`UrunKategorisi.cs:80`, paket 25) fiziksel yerleşim; konsinye var; B2B bayi/çanta takibi yok (D3-12). |
| Müşteri siparişi ekranı | **VAR** | `/siparisler`, `/siparisler/{Id}` (paket 3-A), tezgâhta `FisDigerSiparis.razor`, kur kilidi siparişe bağlı (D1-04). |

## Özet tablo
| Paket | Karar | Eksik maddeler | Boy |
|---|---|---|---|
| 01 Fiş/kalem (Faz B) | KISMEN | hurda alışına fiş kimliği; POS–fatura mutabakatının kalemli doğrulaması | S |
| 03 Sipariş (Faz B) | KISMEN | sipariş belge bağlama (`BelgeModulu.cs:93` kapalı); ayrı `/siparisler/ozet` | S |
| 04 Belge deposu (Faz B) | KISMEN | işletme logosu yükleme/basım; POS'ta ürün görseli; pusula eki (ADR-0008 kararı) | S |
| 05 Şablon motoru | KISMEN (eşdeğer) | sayım tutanağı; kasa gün kapanış fişi; logo; belge türü başına yerleşim ayarı ekranı | M |
| 06 Bildirim/iş (Faz B) | KISMEN | WhatsApp derin bağlantısı | S |
| 36 Patron paneli | KAPSAM DIŞI (1.1) | birleşik uç + `/patron` ekranı (parçaları var) | M |
| 37 Pozisyon/net varlık | KISMEN | kasa+banka para birimi bazında; TL net varlık; maliyeti bilinmeyen stok beyanı | M |
| 39 Has defteri | KAPSAM DIŞI (1.1) — YOK | tamamı (uç, ekran, mutabakat, dönem kilidi) | L |
| 42 Sevk irsaliyesi | KISMEN | sevk durum makinesi; taşıyan/plaka/sevk tarihi; transfer/atölye türleri; irsaliyesiz sevk raporu; akıştan kısayol | M |
| 43 Mobil | BİTTİ (eşdeğer) | — (ayrı rapor yok) | — |
| 44 PWA kabuğu | KISMEN | service worker; CSP `manifest-src/worker-src`; nginx/IIS MIME; Faz B kurulum düğmesi + sürüm bandı | S-M |
| PWA-1 | KISMEN | SW + `pwa.js` + reddetme listesi; CSP; `denetim.sh` JS bloğu; Faz B | S |
| 46 Şube/yetki matrisi | KISMEN | KASIYER/CIRAK rolleri; `kullanici_yetkileri`; maliyet/kâr görme kapısı; ince yetki ekranı/uçları | M |
| E-BELGE | KISMEN (≈%85) | XSD doğrulama; olay izi tablosu; XML indirme; dış: Nilvera test hesabı, özel matrah kodu | S |
| F-8 | BİTTİ (belge) | kurucu: yazılı teklifler | — |
| UX-4 | BİTTİ (4 alt rapor) | — | — |
