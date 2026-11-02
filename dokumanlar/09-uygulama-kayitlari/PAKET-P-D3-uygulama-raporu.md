# PAKET P-D3 — TEMİZ DEMO KİRACISI (senaryonun istediği veriyle) — UYGULAMA RAPORU

> Rol: Taşkın (backend) + Ertuğ (frontend) · Opus · Dalga DEMO HAZIRLIK · 2026-09-23
> Spesifikasyon: `dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md` § 3 "GEREKEN VERİ" (V1–V17), § 0 Z4, § 4 D-e, § 7 Bölüm B.
> COMMIT YOK. Dosya silinmedi. 5199/5196/5197/5198/5200'e dokunulmadı; canlı doğrulama 5195'te yapıldı, iş sonunda durduruldu.

## 1. Özet

Demo kiracısının içeriği artık **senaryonun § 3 listesinin makinece okunur karşılığıdır** (`SENARYO` nesnesi, `demo-tohumla.js`).
Betik planı **yazmadan önce** § 3'e karşı sınar (`senaryoPlaniniDogrula`), içerik yazıldıktan **sonra** her V maddesini
**sunucudan geri okuyarak** ölçer (`senaryoOlcumunuDegerlendir`); bir madde tutmazsa `BAŞARISIZ` ile çıkar ve ölçümü
`ham/demo-tohum-KARSILANMADI-*.json`'a yazar (fail-closed, Anayasa md.1).

Gerçek demo kiracısı kuruldu ve **17/17 ölçüm maddesi geçti**:
`DEMO-Vitrin Kuyumculuk` · tenant `01M37H5Z2NZA72V8GY8K4NHDCW` · SAHİP giriş e-postası `demo-1790180523009@ornek.local`
(parola betikteki varsayılandır; bu rapora YAZILMADI). Kanıt: `ham/demo-tohum-2026-09-23-16-22-16-388.json`.

## 2. Spesifikasyonun koda karşı doğrulanması — sapmalar ve gerekçeleri

| # | Spesifikasyon iddiası / isteği | Kodda bulunan | Karar |
|---|---|---|---|
| S1 | Görev metni "Birleşik koşullar bölümü BAĞLAYICI" diyor | DEMO-SENARYOSU'nda böyle bir başlık **yok** (`grep` 0). Bağlayıcı liste § 3 V1–V17 (ek koşullar da bunu söylüyor) | § 3 esas alındı |
| S2 | V13 "kur tohum kuru bekler" | Yeni kiracıda kur Altınkaynak'tan ~2 sn'de geldi (ölçüldü) | Betik 40 sn bekler; gelmezse **uydurma kur girmez**, durur ve kurucuyu `/kur` elle girişe yönlendirir |
| S3 | Yeni kiracının fiyat listesi var sayılıyordu | Yeni kiracı **fiyat listesiz** doğuyor; `/fiyat/teklif` → `LISTE_YOK`, POS hiçbir ürüne otomatik fiyat vermez (ölçüldü, prova 1) | Betik ürünün kendi ucundan (`POST /fiyat/listeler`) varsayılan "Perakende" listesi açar (marj %4, kurgusal dükkân kararı — kurucu `/fiyatlandirma`'dan değiştirir) |
| S4 | V6 "2× 925 gümüş" | Gümüşte kotasyon yok; teklif `KOTASYON_YOK` döner, POS fiyat vermez | Gümüşe dükkân etiket fiyatı (2.400 / 3.200 TL, kurgusal) liste ucundan girildi |
| S5 | V8 "her üründe ≥3 stok" — ilk tasarım stoğu **toptancıdan alış** olarak yazdı | **ÖLÇÜLMÜŞ ARIZA (prova 3):** 12 ürünün stoğu ~1,18 milyon TL'lik bugüne tarihli ALIŞ oldu → MASAK 185.000 TL kimlik kapısı tetiklendi ve Panel'in "Net kasa" kartı (`SatisTl − AlisTl − HurdaTl`, `Pages/Panel.razor:376`) **−1.078.595 TL KIRMIZI** çizecekti — kırmızı çizginin tam ihlali | Stok, ürün kartının **açılış stoğu** ucundan açıldı (`acilisStogu`, UX-4d md.1 — deftere `SAYIM_DUZELTME`, alış sayılmaz). Toptancıdan yalnız 2 adet gümüş bileklik vadeli alındı (3.840 TL). Yeni ölçüm maddesi **PANEL_NET** bunu kalıcı olarak ölçer |
| S6 | Senaryo "30 etiketlik önizleme" | V5 "≥ 12 ürün" bağlayıcı; 12 × 3 = **36** etiket | V5 korundu. Kurucu "Tümünü seç × 3" dediğinde düğme **(36)** yazar — cümle "otuz altı" olmalı (bkz. § 9) |
| S7 | Senaryo "bugün kodda 3 şema" (Z6, Adım 10) | `GorunumSecenekleri.Semalar` bugün **8 şema** (platin, zumrut, grafit, bordo, okyanus, menekse, zeytin, fildisi) | Belge eskimiş; dükkân görünümü `platin / 18` seçildi, `--sema/--olcek` ile kurucu değiştirir |
| S8 | Kiracı adı `DEMO-<Müşterinin Dükkân Adı> Kuyumculuk` | Müşterinin dükkân adı repoda **yok** | Tahmin edilmedi: `DEMO-Vitrin Kuyumculuk`. Gerçek ad için betik `--ad "DEMO-<ad> Kuyumculuk"` ile yeniden koşulur |

## 3. Yapılanlar

1. **`dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js`**
   - `SENARYO` nesnesi (V3 kasa açılışı 150.000 TL, V4 5 müşteri + 1 toptancı, V5/V6 12 ürün karması, V7 dükkân kodu + EAN-13 barkod, V8 açılış stoğu, V9 9 satış, V10 2 açık veresiye + kısmi tahsilat, V11 1 emanet, V12 emanet için boş müşteri, V14 8 vitrin kalemi, V15 görünüm).
   - `senaryoPlaniniDogrula(p)` (saf) — plan yazılmadan önce; ihlal = hiç yazmaz.
   - `senaryoOlc()` — tohum sonrası **sunucudan** geri okuma (kasalar, cari, ürün, her ürünün `/stok`u, iade-adayları, emanet, kur, görünüm, pano ayarları, tevsik eşiği, `/raporlar/gunluk`).
   - `senaryoOlcumunuDegerlendir(o)` (saf) — 17 madde; bilinmeyen/`null` değer **TUTMAZ** (fail-closed).
   - Kasa açılış devri (`POST /kasalar/{id}/hareket` GIRIS) — kasa asla eksi değil.
   - Varsayılan fiyat listesi + gümüş etiket fiyatı, fiyat teklifleri **sunucudan** (altın fiyatı koda gömülmedi).
   - Vitrin: `PATCH /pano/ayarlar/{kod}` ile 8 kalem aktif, kalanı pasif. Görünüm: `PATCH /gorunum`.
   - Yeni bayraklar: `--plan-dogrula` (ağ çağrısız), `--sema`, `--olcek`, `--sahip-eposta`. `module.exports` + `require.main` koruması (testler saf fonksiyonları çağırır; doğrudan koşumda davranış aynı).
   - Doğrudan SQL yok (`DEMO_IZOLASYON` 0 kaldı); QA-TEST reddi, DEMO- ad kuralı, `--kosan` zorunluluğu korunarak.
2. **`dotnet/denetim.sh`** — yeni sayaç `P02-1e DEMO_SENARYO_VERI` (P-D3): iki kapının betikte durduğunu **ve** `node demo-tohumla.js --plan-dogrula`'nın 0 eksik verdiğini ölçer; node yoksa ÖLÇÜLEMEDİ = kırmızı.
3. **`dotnet/Kuyumcu.Tests/PD3DemoVeriTestleri.cs`** — 51 test (7 yöntem; Theory'lerle): GERÇEK node sürecinde saf fonksiyonlar çağrılır.
4. **`docs/RUNBOOK-DEMO.md` § 1** — tohum akışının yeni tarifi (tablolara dokunulmadı; `DEMO_ROTA_KRITIK` rota tablosu aynı).

**Migration:** gerekmedi (şema değişikliği yok; yalnız mevcut uçlar kullanıldı).
**Razor / Sozlukler / sahte API:** dokunulmadı (paket bir veri paketidir; ekran değişmedi → "Dokunulan sayfalar": YOK).

## 4. DoD sözleşmesi

Paket **yeni varlık/kayıt türü açmaz**. Tohumlanan her kayıt ürünün mevcut uçlarıyla doğar ve mevcut düzenleme yollarına tabidir:

| Kayıt | Oluşturan uç | Düzenleme / geri dönüş yolu |
|---|---|---|
| Cari (6) | `POST /cari` | `PATCH /cari/{id}` · pasife al (mevcut) |
| Ürün (12) + açılış stoğu | `POST /urunler` (`acilisStogu`) | Ürün `PATCH`; açılış stoğu **salt okunur** (UX-4d md.1 — stok yalnız alış/satış/sayımla değişir) |
| Fiyat listesi + gümüş sabit fiyatı | `POST /fiyat/listeler`, `POST …/urun-fiyatlari` | `PATCH /fiyat/listeler/{id}`, `PATCH …/urun-fiyatlari/{urunId}` |
| Kasa açılış devri | `POST /kasalar/{id}/hareket` | açıklama `PATCH …/hareket/{id}`; tutar **storno** (`POST …/storno`) — finansal, hard delete yok |
| Alış (1), satış (9), tahsilat (1) | `/islemler/alis`, `/islemler/satis`, `/cari/{id}/tahsilat` | Finansal: yalnız iade/storno (48-A motoru); hard delete YOK |
| Emanet girişi (1) | `POST /emanet/giris` | açıklama `PATCH /emanet/hareketler/{id}`; storno/çıkış |
| Vitrin kalem ayarı | `PATCH /pano/ayarlar/{kod}` | aynı uç + `POST …/varsayilana-don` |
| Dükkân görünümü | `PATCH /gorunum` (SAHİP) | aynı uç (null = varsayılan) |
| Demo kiracısının kendisi | davet + `/auth/kayit` | Silme **kurucu kararı** (`kiraci-temizle.sh` DEMO- adını reddeder) |

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| `FullyQualifiedName~PD3DemoVeriTestleri` (ilk yeşil) | 50 | 50 | 0 | |
| aynı, son hâli (denetim sayacı testi eklendi) | 51 | 51 | 0 | tam süitle birlikte ölçüldü |
| İlgili mevcut: `OluIcBaglantiTestleri|P30DemoKisitTestleri|MarkaAdiTestleri` | 14 | 14 | 0 | betiği okuyan önceki testler kırılmadı |
| Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_d`, trx `scratchpad/trx-pP-D3/_Mac_2026-09-23_19_38_17.trx`) | 4175 | 4173 | 2 | PD3'ün 51 testi dâhil hepsi geçti; 2 kırmızı başkasının (aşağıda) |

Önce (devir notu): 4077 test / 1 kırmızı. Bu paket **+51** test ekledi (4175'in kalan 47'si başka şeritlerin eşzamanlı eklediği testlerdir).

**Kırmızı listesi karşılaştırması (kapı maddesi 7):**
| Kırmızı test | Devir notunda? | Sahibi | Bu paketle ilişkisi |
|---|---|---|---|
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | EVET | P-30b (DALGA 7c) | yok (dokunulmadı) |
| `AltinPozisyonuTestleri.B2_05_hurda_has_grami_pozisyona_eklenir` (beklenen 918,5 / gerçek 916,000000) | hayır | **Pozisyon şeridi** — `AltinPozisyonuTestleri.cs` 19:07'de, `Modules/Pozisyon/PozisyonModulu.cs` 19:00–19:42 arasında başka şerit tarafından yazılıyor; tam süitten hemen önceki derlemede bu dosya `CS1002/CS1513` ile **yarım** hâldeydi | yok: test ne `demo-tohumla.js`'i ne `denetim.sh`'ı okur (`grep -c` = 0); bu paketin dosyalarıyla kesişim yok |

Fark (bu paketin ürettiği yeni kırmızı) = **0**.

**Derleme notu:** paket sonu `dotnet build Kuyumcu.sln --nologo` (19:27) → **0 Uyarı 0 Hata**. Tam süit öncesi ikinci derleme denemesi (19:38) başka şeridin yarım `PozisyonModulu.cs`'i yüzünden 7 hata verdi (bu paketin dosyası değil); süit o anda diskte bulunan, bu paketin testlerini içeren derlemeyle koştu (51/51 PD3 testi raporda görünüyor).

## 6. Mutasyon kanıtı (12/12 kırmızı, hepsi geri alındı, `cmp` birebir AYNI)

Yöntem: kaynak scratchpad `mut-yedek/P-D3/<kod>/` altına kopyalandı → `perl -0777 -pi -e` tek eşleşme → `dotnet test --filter PD3DemoVeriTestleri -v n` → yedek geri kopyalandı → `cmp` AYNI.

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M-D3-1 | plan V3 kasa kuralı `if (false)` | `Plan_dogrulayici_V_ihlalini_reddeder(p.kasaAcilisTl=40000, V3)`, `(p.kasaAsgariTl=10000, V3)` |
| M-D3-2 | plan V8 `kalan < stokAsgari` → `kalan < 0` | `Plan_dogrulayici_V_ihlalini_reddeder(p.urunler[4].acilisAdet=2, V8)` |
| M-D3-3 | `YASAK_KELIME` → `/\bXYZQ\b/` | `Plan_dogrulayici_V_ihlalini_reddeder(p.cariler[0].ad='QA-TEST G2T8 Musteri DUZENLENDI', V16)` |
| M-D3-4 | ölçüm V3'ten `kb.every(b > 0)` silindi | `Degerlendirici_bozuk_olcumu_reddeder(o.kasaBakiyeleri=[217353.27,-5], V3)` |
| M-D3-5 | PANEL_NET → `true` | `Degerlendirici_bozuk_olcumu_reddeder(o.panelNetTl=-1078595.36, PANEL_NET)`, `(o.panelNetTl=null, PANEL_NET)` |
| M-D3-6 | V8 `a === null ||` (fail-open) | `Degerlendirici_bozuk_olcumu_reddeder(o.stoklar.a=null, V8)` |
| M-D3-7 | `if (eksik.length) {` → `if (false) {` | `Icerik_doldurma_plani_yazimdan_once_sinar_ve_olcumu_fail_closed_uygular` |
| M-D3-8 | ürün gövdesinden `acilisStogu` silindi | `Icerik_doldurma_plani_yazimdan_once_sinar_ve_olcumu_fail_closed_uygular` |
| M-D3-9 | V14 üst sınır silindi | `Degerlendirici_bozuk_olcumu_reddeder(o.vitrinAktif=22, V14)` |
| M-D3-10 | ADIM4 `pahaliFiyat > 0` | `Degerlendirici_bozuk_olcumu_reddeder(o.pahaliFiyat=29000, ADIM4)`, `(o.tevsikEsigi=null, ADIM4)` |
| M-D3-11 | V12 `=== 0` → `!== 1` | `Degerlendirici_bozuk_olcumu_reddeder(o.emanetBosBakiye=0.5, V12)`, `(o.emanetBosBakiye=null, V12)` |
| M-D3-12 | denetim.sh'tan `node … --plan-dogrula` satırı silindi | ilk koşumda **YEŞİL KALDI = TEST AÇIĞI** (test yorumdaki `--plan-dogrula` geçişine bakıyordu) → test gerçek çağrı satırını arayacak biçimde güçlendirildi → tekrar: `Denetim_betigi_demo_senaryo_verisi_kapisini_tasir` KIRMIZI |

Not: M-D3-9..12'nin ilk denemesi, başka bir şeridin eşzamanlı derlemesi `Kuyumcu.Tests.dll`'i sildiği için **test koşmadan** bitti ("bağımsız değişkeni geçersiz"); derleme bitince yeniden koşuldu, sonuçlar yukarıdakidir. Son mutasyondan sonra `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata**.

## 7. Denetim

`bash dotnet/denetim.sh` (paket sonu):
```
  ✓ P-02: demo provası rota tek kaynak kapısı (D-c) fail-closed
  ✓ P-02: DEMO_SENARYO_VERI (P-D3)                 0 (plan § 3'ü karşılıyor · geri okuma fail-closed)
  ✓ P-30: DEMO_IZOLASYON                           0 (demo QA fikstüründen türemez, toplu temizlik süpüremez, doğrudan SQL yok)
  ✓ P-30: DEMO_ROTA_KRITIK                         0 (son iki prova: iki AYRI ajan · her rota kritiksiz · adım damgalı)
  ✗ P-13: GEREKCESIZ_KILITLI_ALAN                  1 (-eq 0 olmalı — Kurulum.razor:1)
DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli.
```
Benim sayaçlarım ✓. Tek ✗ **`Pages/Kurulum.razor`** — YAYIN şeridinin dosyası (bu pakette açılmadı; dokunma yasağı). İlk ara koşumumda (19:0x) denetim TEMİZ'di; ✗ sonradan, YAYIN şeridinin çalışmasıyla belirdi. Sahibi: YAYIN şeridi.

## 8. Canlı doğrulama (5195, `--no-build`, Development, `Sistem__Anahtari` rastgele 64 hane — değer yazılmadı)

### 8.1 Tohum ölçümü — DEMO-Vitrin Kuyumculuk (sunucudan geri okunmuş)

| Madde | Beklenen | Gerçek | Sonuç |
|---|---|---|---|
| V3 | her kasa > 0, biri ≥ 50.000 | Ana Kasa 217.419,20 | ✓ |
| V4 | ≥ 6 cari (≥ 5 müşteri) | 6 / 5 | ✓ |
| V5 | ≥ 12 ürün | 12 | ✓ |
| V7 | kodda DEMO- yok | [] | ✓ |
| V8 | her üründe ≥ 3 | 12 ürünün hepsi 3 | ✓ |
| V9 | ≥ 8 satış, 2.000–25.000, nakit ≤ 30.000 | 9 satış: 3.200 … 22.489,39 | ✓ |
| V10 | ≥ 2 açık veresiye | 2 | ✓ |
| V11 | mevcut emanet > 0 | 9,16 has (K22 10 g) | ✓ |
| V12 | ayrılan müşteride 0 | 0 | ✓ |
| V13 | kur dolu | Altınkaynak, has 6.744,57 | ✓ |
| V14 | vitrinde 7–10 | 8 | ✓ |
| V15 | platin / 18 | platin / 18 | ✓ |
| V16 | QA/TEST/DUZ yok | [] | ✓ |
| ADIM3 | ucuz ürün < tevsik eşiği | çeyrek 11.244,69 < 30.000 | ✓ |
| ADIM4 | pahalı ürün > eşik | bilezik 97.668,18 > 30.000 | ✓ |
| PANEL_NET | Panel net ≥ 0 | 96.697,08 | ✓ |

### 8.2 Senaryo adımı → ekranın çağırdığı uç → okunan yanıt (DEMO kiracısı, `ekran-olc.mjs`)

| Adım | Uç | HTTP | Okunan değer | Durum |
|---|---|---|---|---|
| 1 `/giris` | `/genel/kisitlar` | 200 | 8 hariç kalem; **tarayıcıda** açıldı: ByGatech başlığı + "Bu kurulumda HARİÇ olanlar" listesi çizildi | DOLU |
| 2 Panel günlük satış + Kasa Özeti | `/raporlar/gunluk` | 200 | satış 9 adet 100.537,08 TL · alış 3.840 · Ana Kasa 217.419,20 · net +96.697,08 | DOLU |
| 2 Panel 7 günlük grafik | `/raporlar/aylik` | 200 | 23 gün satırı, **1 dolu gün** (bugün) | TEK GÜN (bkz. § 9) |
| 2 Panel kur | `/kur/guncel` | 200 | has 6.744,57 · ALTINKAYNAK | DOLU |
| 2 Panel bekleyen işler | `/masak/ozet`, `/gider-pusulalari/sure-takibi`, `/masak/risk/ozet` | 200 | hepsi 0 → kutu "bekleyen iş yok" boş durumu çizer | **BOŞ** (bkz. § 9) |
| 2 Panel son işlemler | `/raporlar/islem-defteri` | 200 | 24 satır (EMANET_GIRIS, TAHSILAT, SATIS…) | DOLU |
| 3 ürün seçici / barkod | `/urunler`, `/urunler/barkod/8690000000012` | 200 | 12 ürün, 12'si barkodlu; barkod → "22 Ayar Çeyrek Altın" | DOLU |
| 3 fiyat (< 30.000) | `/fiyat/teklif` | 200 | çeyrek 11.244,01 (kaynak LISTE) | DOLU |
| 4 kanun kapısı | `/fiyat/teklif` + `/tevsik/gunluk?fisToplamTl=97662.24` | 200 | had 30.000 · `islemTutariKuraliDevrede=true` · kalan nakit 0 → uyarı tetiklenir | DOLU |
| 5 stok penceresi | `/urunler/{blz}/stok` | 200 | toplam 3, 1 şube satırı | DOLU |
| 6 etiketler | `/urunler?aktif=true&limit=500`, `/urunler/{id}/etiket` | 200 | 12 ürün; etiket verisi döndü (× 3 = 36 etiket) | DOLU |
| 7 `/kur` | `/kur/kaynaklar` | 200 | seçili ALTINKAYNAK | DOLU |
| 7 `/vitrin` | `/pano/guncel` | 200 | 8 kalem: Gram · Çeyrek · Yarım · Tam · Ata · 22 Bilezik · Has · 14 Ayar | DOLU |
| 8 `/emanet` | `/emanet/{Nermin}`, `/emanet/{Derya}` | 200 | boş müşteri 0 (0 kayıt) · mevcut kayıt 9,16 has | DOLU |
| 9 `/raporlar` aylık | `/raporlar/aylik` | 200 | satış 9 / 100.537,08 · alış 1 / 3.840 | DOLU |
| 9 stok değeri | `/raporlar/stok-degeri` | 200 | 12 ürün satırı | DOLU |
| 9 gün sonu mutabakatı | `/raporlar/pos-fatura-mutabakati` | 200 | dolu ama **mutabık=false**, fark 100.537,08 ("9 satış faturalandırılmamış") | DOLU, "FARK VAR" (bkz. § 9) |
| 10 `/ayarlar` Görünüm | `/gorunum`, `/kullanici/gorunum` | 200 | platin · 18 · temaModu sistem | DOLU |

Ek: `demo-tohumla.js --prova --kosan "P-D3-uygulayici-opus"` DEMO kiracısına koşuldu → **kritik=0**, 10/10 rota 200 (rapor scratchpad'e yazıldı, `ham/`'a DEĞİL — `DEMO_ROTA_KRITIK`'in "son iki prova iki ayrı ajan" ölçüsünü bu paket bozmasın diye).

**Tarayıcı turu sınırı (dürüst kayıt):** giriş ekranı tarayıcıda açılıp görüldü. Girişten sonraki ekranların **tarayıcı turu yapılmadı**: ajan güvenlik kuralı gereği bir kimlik doğrulama formuna parola yazamaz. Girişli ekranlar, her ekranın çağırdığı uçların yanıtı okunarak doğrulandı (yukarıdaki tablo). Bölüm C ekran turu (kurucu / ikinci ajan) **hâlâ zorunludur**.

### 8.3 SQL kanıtı (DEMO kiracısı `01M37H5Z2NZA72V8GY8K4NHDCW`)
```
kasalar: Ana Kasa | TRY | 217419.20
cariler: MUSTERI 5 · TEDARIKCI 1
urunler: 12 · stoklar: min 3.000, toplam 36.000
hareketler: ALIS 1 (-3840.00) · EMANET_GIRIS 1 · SATIS 9 (100537.08) · SAYIM_DUZELTME 12 (açılış stoğu) · TAHSILAT 1
yasak kelime / DEMO- önekli ad-kod: 0
```

### 8.4 Test kiracıları temizliği
- Açılan prova kiracıları: **5 × `PAKET-P-D3-TEST Kuyumculuk`** (4 tohum denemesi + 1 kez girişte 429 yiyen kayıt). Hepsi `kiraci-temizle.sh <id> --uygula` ile silindi (356 + 641 + 280 + 621 + 529 satır + tenants).
- `kiraci_davetleri`'nde bu 5 kiracının davet satırı (tenant_id kolonu yok) ayrıca silindi (5 satır; kalan 0).
- `tenants`: başlangıç **9** → son **10** = 9 + **DEMO-Vitrin Kuyumculuk** (paketin çıktısı; bilerek bırakıldı, `QA-TEST-*` fikstürlerine dokunulmadı).
- 5195 durduruldu: `lsof -nP -iTCP:5195 -sTCP:LISTEN` boş.

## 9. Açık kalanlar (orkestratöre / kurucuya)

1. **TARİH BAĞIMLILIĞI — EN ÖNEMLİSİ.** Tohum bugüne (23.09) tarihlidir. Panel'in "Günlük satış", "Son işlemler" kartları ve gün sonu mutabakatı **o günün** verisini okur. **Cumartesi (26.09) sabahı bu kiracıda Panel günlük kartları SIFIR görünür.** Çözüm: demo sabahı betik yeniden koşulur (≈ 14 sn) — yeni kiracı, `--ad "DEMO-<dükkân> Kuyumculuk" --sahip-eposta <hatırlanır adres>`. Eski DEMO kiracısını silmek kurucu kararıdır (`kiraci-temizle.sh` DEMO- adını reddeder). Tarihi SQL ile kaydırmak YASAK (RUNBOOK § 1).
2. **7 günlük grafik tek güne sıkışık** (TEK_GUN — uçlar işlem tarihi almaz). Senaryonun "son bir haftanın satışları DEMEZ" uyarısı geçerli.
3. **Bekleyen işler kutusu BOŞ** ("bekleyen iş yok"). Dolması için MASAK süresi geçmiş bildirim / gecikmiş gider pusulası / yüksek riskli müşteri gerekir; bunları demo için **uydurmak** yanıltıcı olur. Orkestratör karar versin: ya kutu "temiz dükkân" diye anlatılır ya senaryodan çıkarılır.
4. **Gün sonu mutabakatı "FARK VAR"** (9 satış faturasız). Kapatmak için her satışa fatura kesmek gerekir; `/faturalar` senaryoda **yasaklı ekran** ve 2 kritik bulgulu (SERİT A). Yapılmadı. Kurucu mutabakat kartında oyalanmamalı.
5. **Fiyat tutarsızlığı:** vitrin çeyrek satış 11.360 (Altınkaynak kotasyonu) ↔ POS teklifi 11.244 (has × liste marjı %4). Müşteri iki ekranı karşılaştırırsa fark görür. Ürün davranışı (Fiyat/Pano), bu paketin kapsamı dışı — ürün sahibine.
6. **Etiket sayısı 36** (12 ürün × 3), senaryo metni "30" diyor (S6).
7. **Senaryo belgesi 3 şema diyor, kodda 8** (S7) — belge sahibine.
8. **Dükkân adı bilinmiyor** (S8).
9. Tam süitteki başkasına ait kırmızılar: bkz. § 5 son satır; `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` sahibi **P-30b**. denetim.sh ✗ `P-13 GEREKCESIZ_KILITLI_ALAN (Kurulum.razor)` sahibi **YAYIN** şeridi. Ara derlemede görülen 2 uyarı (`OlcumKapisiTestleri.cs:525 CS8604`) başka şeridin o anki düzenlemesiydi; son `dotnet build Kuyumcu.sln` 0 uyarı.

## 10. Kurucuya / mali müşavire notlar

- **Mevzuat hükmü koda yazılmadı.** Tevsik eşiği (30.000 TL) ve MASAK eşiği (185.000 TL) sunucunun mevcut yapılandırmasından okundu (`/tevsik/gunluk`, `/masak/esikler`); betik yalnız "demo verisi bu sınırların altında/üstünde mi" diye ölçer. SENARYO'daki `nakitTavanTl: 30000` bir **demo verisi tavanıdır** (senaryo metninin "hiçbiri 30.000 ₺ üstü nakit değil" satırı), yasal hüküm değildir.
- Açılış stoğu **maliyeti bilinmeyen parti** olarak açılır (ürünün kendi kuralı) — kâr raporu bu adetlerde maliyet göstermez; `/raporlar/kar-zarar` zaten demoda yasak.
- Tüm adlar kurgusal; telefonlar `0532 000 01 0x`. Ürün fiyatı söylenmez (BK-49).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| § 3 V1–V17 | KARŞILANDI (V1 1 şube, V2 1 SAHİP, V17 P-D2'de) | § 8.1 17/17 |
| Kasa ASLA negatif | KARŞILANDI | Ana Kasa 217.419,20; PANEL_NET +96.697 |
| QA-TEST kiracılarına dokunma | KARŞILANDI | tenants listesi § 8.4 |
| DEMO- önekli ad | KARŞILANDI | `DEMO-Vitrin Kuyumculuk` |
| Yalnız ürün uçlarından tohum | KARŞILANDI | `DEMO_IZOLASYON` 0 |
| Her senaryo adımı canlı | KISMİ: uçlar 18/18 okundu; girişli ekranların tarayıcı turu yapılamadı | § 8.2 |
| Boş kalan adım raporda | KARŞILANDI | § 9 madde 1–4 |
| Paylaşımlı dosyalara dokunma (Sozlukler, PARA, YAYIN, SERİT A) | KARŞILANDI | § 12 |
| Mutasyon | 12/12 kırmızı | § 6 |

## 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzülmüş, bu paketinkiler)

```
dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js         (değişti)
dotnet/denetim.sh                                                  (P02-1e eklendi)
dotnet/Kuyumcu.Tests/PD3DemoVeriTestleri.cs                        (yeni)
docs/RUNBOOK-DEMO.md                                               (§ 1 metni)
dokumanlar/09-uygulama-kayitlari/ham/demo-tohum-2026-09-23-16-22-16-388.json  (yeni, tohum kanıtı)
dokumanlar/09-uygulama-kayitlari/PAKET-P-D3-uygulama-raporu.md     (bu rapor)
```
Not: depo tek "first commit" ile bu dosyaların çoğunu izlemediği için `git status` bunları `??` gösterir; ağaçtaki diğer `??`/`M` satırları başka şeritlerindir.
