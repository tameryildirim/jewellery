# PAKET YASAL-1 — Tarihli yasal parametreler: uygulama raporu

- Tarih: 30.09.2026
- Uygulayan: Taşkın (backend) ve Ertuğ (frontend) rolleri
- Tur: 3 (orkestratör 14:50 bulguları; tur 2 ve tur 1 kayıtları aşağıda — sonraki turla çelişen önceki tur cümlesi geçersizdir)
- Commit yapılmadı. Değişiklikler çalışma ağacında duruyor.
- Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-YASAL-1-ESIK-PARAMETRE-2026-09-30.md`

## TUR 3 — Orkestratör bulgularının kapatılması (30.09.2026, 14:50 kaydı)

### T3.1 Sonuç

Orkestratörün iki bulgusu kapatıldı:

1. **İleri tarihli satır artık "Geçmiş değerler"de değil.** Kartta, açılışta görünen tek satır olarak duruyor: "Yaklaşan değişiklik: 3 Ekim 2026 tarihinden itibaren 40.000 ₺". Geçmiş listesinde bugün geçerli satır, bitişi ileri tarihli değer yüzünden dolu olsa da "sürüyor" yazıyor.
2. **Eşik ekranda kuruşla yazılmıyor.** Sistem 2.000.000,01 ile karşılaştırıyor, ekran "2.000.000 ₺ üzeri" yazıyor. Kimlik eşiği "185.000 ₺ ve üzeri" oluyor. Aynı kural Panel bandında da geçerli.

Paket kapısının yedi maddesi:

| Madde | Sonuç |
|---|---|
| (1) Build | `dotnet build Kuyumcu.sln`: 0 hata, 0 uyarı (son mutasyondan sonra `--no-incremental` ile temiz derleme; bir `MSB3026` dosya kopyalama yeniden denemesi uyarısı yalnız ara derlemede görüldü, paralel şerit kilidi) |
| (2) Paket testleri | Yeni 5 test yeşil; `Yasal` süzgeci 71/71 |
| (3) Sayaçlar | YASAL_PARAM_TARIHSIZ / YAZMA / OKUMA_DISI 0 ✓. B-3 ESIK_ISTEMCIDE 4 (tavan 4) ✓: ilk koşumda bu turun yorum satırı 5'e çıkarmıştı, yorumdaki rakam kaldırıldı. Kalan 3 ✗ bu paketin değil (§ T3.8) |
| (4) Mutasyon | 8/8 mutasyonun her biri adı yazılı en az bir testi kırmızıya çevirdi, hepsinde `cmp` AYNI (§ T3.5) |
| (5) Canlı | API 5160 + vekil 5161. Faz A kuyumcu_net (yazma yok), Faz B kuyumcu_test_t1 (ileri tarihli iki satır) — § T3.6 |
| (6) Test kiracısı | kuyumcu_net: `zsh kiraci-temizle.sh 01M3S3D7W3738QZ5EZFH3ZBHAF --uygula` + kendi davet satırı silindi; tenants 13 → 14 → 13 |
| (7) Yeni kırmızı | tam süit 7847: 7835 geçti, 11 kaldı, 1 atlandı; 11 kırmızının hepsi başka şeritlerin alanında → bu paketin yeni kırmızısı 0 (§ T3.8) |

### T3.2 Yapılan

| Bulgu | Yapılan | Kanıt |
|---|---|---|
| (1) İleri tarihli satır geçmişte | Sunucu: `YasalParametreServisi.Sonrakiler(gun)` (ufuksuz, tatil hariç, başlangıca göre artan) + `GET /yasal-parametreler` yanıtına `sonrakiler` alanı. Ekran: kartta `#ys-yaklasan-<anahtar>` satırı; geçmiş listesi `GecmisteMi` ile süzülür (başlangıç ≤ bugün); `BugununSatiriMi` olan satır "sürüyor" | `Tur3_ileri_tarihli_satir_GECMISTE_degil_kartta_yaklasan_bugunku_satir_suruyor` (bUnit) · `Tur3_liste_esik_turu_ve_tutari_doner_ileri_tarihli_satir_Sonrakilerde_bugunku_satirlarda_DEGIL` (HTTP) · canlı: geçmiş "30 Kasım 2024 – sürüyor: 30.000 ₺ ; 1 Ocak 2016 – 29 Kasım 2024: 7.000 ₺", 40.000 yok |
| (2) Eşik dili | Sunucu: `YasalAnahtar.BuyukEsitKarsilastirilan` (MasakKuralMotoru'nun `t >= eşik` karşılaştırdığı 4 anahtar) + `EsikGosterimi`: `X,01` → ("USTU", X), tam sayı → ("VE_USTU", X), diğerleri null. Liste satırına `esikTuru`/`esikTutari`, yaklaşan değişikliğe `esikTuru`/`yeniEsik`/`eskiEsik`/`eskiEsikTuru`. Ekran: `YasalBicim.Esikli` → "{0} üzeri" / "{0} ve üzeri" (TR+EN sözlük). Rakam istemciye gömülmedi | `Tur3_esik_gosterimi_ic_karsilastirma_kurusu_gostermez_BuyukEsit_olmayan_ham_kalir` (saf) · `Tur3_esik_kurussuz_uzeri_ve_ve_uzeri_yazilir_ic_karsilastirma_degeri_basilmaz` (bUnit) · `Tur3_bant_esik_degisikligi_kurussuz_yazar` (bUnit) · canlı 16/16 ölçümde "X,01" kalıbı 0 |

**Yorum (bulgu 1'in "listede … ileri tarihli 'başlıyor <tarih>'" cümlesi):** Aynı bulgu ileri tarihli satırın "Geçmiş değerler"de olmamasını istiyor. Bu yüzden "başlıyor" bilgisi geçmiş listesinde değil kartın "Yaklaşan değişiklik: <tarih> tarihinden itibaren <değer>" satırında veriliyor. Türkçe ek uyumu ('ten/'dan) tarihe göre değiştiğinden "tarihinden itibaren" kalıbı kullanıldı (Panel bandıyla aynı kalıp).

**Tevsik haddi neden "üzeri" almıyor:** Kapı `tutar > Had` karşılaştırıyor (459 s. VUK GT: "7.000 TL'yi aşan"). Kartın adı zaten "Bir kişiyle aynı gün nakitte en yüksek tutar (üstü bankadan ödenir)". 30.000 ₺ izin verilen en yüksek tutar olduğundan değer "30.000 ₺" kalıyor. "30.000 ₺ üzeri" yazmak adla çelişirdi. Fatura haddi de (henüz kullanılmıyor) adı "Bu tutarı aşan satışta fatura zorunlu" olduğu için ham kalıyor.

### T3.3 Birincil kaynak metinleri (eşik ifadesi)

| Anahtar | Programın karşılaştırması | Ekran | Birincil kaynak ve ifade |
|---|---|---|---|
| MASAK_KIMLIK_ESIGI_TL | `t >= 185.000` | 185.000 ₺ ve üzeri | Tedbirler Yönetmeliği md.5/1-b, https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf (bu turda indirilip `pdftotext` ile okundu): "yüzseksenbeşbin TL (kripto varlık hizmet sağlayıcılar için onbeşbin TL) veya üzerinde olduğunda" |
| MASAK_AMAC_BEYANI_ESIGI_TL (TEYİTSİZ) | `t >= 200.000` | 200.000 ₺ ve üzeri | HMB MASAK Genel Tebliği metni (şartname § 3.2'deki URL; PDF başlığı "SIRA NO: 30", dosya adı "SIRA-NO-31"), md. (3): "ikiyüzbin TL ile ikimilyon TL (bu tutarlar dahil) arasındaki nakit işlemlerde" |
| MASAK_BEYAN_FORMU_ESIGI_TL (TEYİTSİZ) | `t >= 2.000.000,01` | 2.000.000 ₺ üzeri | Aynı metin md. (4): "ikimilyonbir TL ile yirmimilyon TL (bu tutarlar dahil) arası nakit işlemlerde" |
| MASAK_EK_BELGE_ESIGI_TL (TEYİTSİZ) | `t >= 20.000.000,01` | 20.000.000 ₺ üzeri | Aynı metin md. (5): "yirmimilyon TL'yi aşan nakit işlemlerde" |
| TEVSIK_HADDI_TL | `t > 30.000` | 30.000 ₺ (ad: "en yüksek tutar") | 459 s. VUK GT (şartname § 3.1): "7.000 TL'yi aşan tutardaki" → 575 s. ile "30.000" |

⚠ **Mali müşavir notu (yeni, § T3.9/10):** Tebliğ metni beyan formu alt sınırını "ikimilyonbir TL" (2.000.001 TL) diye yazıyor. Program 2.000.000,01 TL'den başlıyor, yani 2.000.000,01 – 2.000.000,99 TL aralığında programın davranışı daha tutucu. Bu değer TEYİTSİZ durumda ve kural fail-closed çalışıyor; değer bu turda DEĞİŞTİRİLMEDİ. Tebliğin yükümlülüğü "finansal kuruluşlar"a verdiği (§ 3.2) ve RG'deki nihai metnin doğrulanamadığı sorusu da aynen açık.

### T3.4 Değişen dosyalar (tur 3)

- **Api:**
  - `Data/YasalParametre.cs`: `BuyukEsitKarsilastirilan`, `EsikUstu`/`EsikVeUstu`, `EsikGosterimi`.
  - `Common/YasalParametreServisi.cs`: `Sonrakiler`.
  - `Modules/Yasal/YasalParametreModulu.cs`: `sonrakiler`, eşik alanları.
- **Contracts:** `Sayfalar/YasalSinirlarSozlesmeleri.cs`. Yalnız sona eklenen isteğe bağlı parametreler, eski yanıtlarla uyumlu.
- **Core:** `I18n/Sozlukler.YasalSinirlar.cs`: `EsikUstu`, `EsikVeUstu`, `Yaklasan`, `YaklasanSatir` (TR+EN).
- **Web:**
  - `Pages/YasalSinirlar.razor`
  - `Bilesenler/YasalDegisiklikBandi.razor`: yalnız `Cumle` içindeki iki biçim çağrısı.
  - `Servisler/YasalBicim.cs`: `Esikli`, `SatirDegeri`, `Gun`.
  - `wwwroot/css/yasal.css`: `.yasal-yaklasan`, belirteçli, sabit renk yok.
- **Testler:** `YasalSinirlarEkranTestleri.cs` (+3), `YasalParametreHttpTestleri.cs` (+2).

Migration yok, şema değişmedi. Mevcut element id'leri değişmedi; yeni id: `ys-yaklasan-<anahtar>`. Panel.razor'a dokunulmadı.

**Dokunulan sayfalar (DoD EK 2):** `YasalSinirlar.razor`, `YasalDegisiklikBandi.razor`. İkisi de bu paketin kendi dosyası ve aynı paketin 3. turu olduğundan ikinci dokunuş gerekçesi gerekmiyor.

### T3.4b DoD sözleşmesi (tur 3)

Yeni varlık ya da kayıt türü eklenmedi. `YasalParametre` değişmez kalıyor: ekrandan yazma yolu yok, `DegismezIpucu` gerekçesi kilit katlanırında. İleri tarihli satır yalnız sistem ucundan yazılıyor (tur 2'deki gibi). Yeni alanlar (`esikTuru`, `esikTutari`, `sonrakiler`, bant eşik alanları) salt okunur ve türetilmiş; sunucu bunları karşılaştırma kuralından hesaplıyor. Yeni düğme yok: `.yasal-yaklasan` eylemsiz bilgi satırı. Tüm düğmeler katlanır başlığı olarak kalıyor (`Ekran_SAHIP_kartlar…` testi `Assert.All(button → katlanir-baslik)`).

### T3.5 Mutasyon kanıtı (tur 3)

Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-1-t3/` altına kopyalandı. Tek eşleşmeli birebir metin değişikliği yapıldı (python; eşleşme sayısı 1 değilse iptal). Ardından `dotnet test --filter FullyQualifiedName~Yasal -v n` koşuldu, yedek geri yazıldı ve `cmp` ile karşılaştırıldı. Betik: `/private/tmp/kuyumcu-serit/YASAL-1/t3/mut.py`.

⚠ **Yöntem düzeltmesi:** İlk koşuda geri yükleme `shutil.copy2` ile yapılıyordu; bu komut eski değiştirme zamanını geri yazdığı için MSBuild Web projesini yeniden derlemedi. Bu yüzden M30'un ilk koşusunda M29'un bayat Web derlemesi de etkiliydi: iki ekran testi fazladan kırmızıydı. M30 temiz olarak yeniden koşuldu (aşağıdaki satır budur). Betik `shutil.copy`'ye çevrildi. Son mutasyondan sonra `--no-incremental` temiz derleme yapıldı.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M26 | Geçmiş listesi süzgeci kaldırıldı (`liste.Where(GecmisteMi)` → `liste`) | Tur3_ileri_tarihli_satir_GECMISTE_degil… | AYNI |
| M27 | Bugünkü satır "sürüyor" değil, son gün (`… \|\| BugununSatiriMi(g)` silindi) | Tur3_ileri_tarihli_satir_GECMISTE_degil… | AYNI |
| M28 | Kartta yaklaşan satırı çizilmez (`Sonraki(s.Anahtar)` → `Sonraki("YOK")`) | Tur3_ileri_tarihli_satir_GECMISTE_degil… | AYNI |
| M29 | "USTU" dalı ölü → ham değer (2.000.000,01 ₺) | Tur3_esik_kurussuz…, Tur3_bant_esik… | AYNI |
| M30 | `X,01` → tutar kuruşlu kalır | Tur3_esik_gosterimi…(saf), Tur3_liste_esik_turu…(HTTP) | AYNI |
| M31 | Liste yanıtında eşik alanları null | Tur3_liste_esik_turu…(HTTP) | AYNI |
| M32 | `Sonrakiler` gün süzgeci yok (geçmiş satırlar da döner) | Tur3_liste_esik_turu…(HTTP) | AYNI |
| M33 | Bantta eski değerin eşik türü düşer | Tur3_bant_esik_degisikligi_kurussuz_yazar | AYNI |

Hepsinde derleme hatası 0. (M33'ün ilk denemesi `null` tür belirsizliği yüzünden derlenmedi, yani mutasyon uygulanamadı. `(string?)null` ile yeniden koşuldu.)

### T3.6 Canlı doğrulama

**Faz A: kuyumcu_net, API 5160, tabloya YAZMA YOK.** Kiracı `PAKETYASAL-1-TEST Tur3`, sistem davetiyle kuruldu. Sistem anahtarı bu API örneğine ortam değişkeniyle, geçici ve rastgele üretilerek verildi; `appsettings.json`'daki anahtar kullanılamaz durumda (503). Günlük: `…/t3/canli/a-canli.log`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /yasal-parametreler (SAHİP) | 200, 16 satır, sonrakiler 0 | 200, 16, 0 |
| MASAK_BEYAN_FORMU | degerSayi 2000000.01, USTU, 2000000 | 2000000.01, USTU, 2000000 |
| MASAK_EK_BELGE | USTU, 20000000 | USTU, 20000000 |
| MASAK_KIMLIK / MASAK_AMAC | VE_USTU 185000 / VE_USTU 200000 | aynı |
| TEVSIK / FATURA | esikTuru null | null / null |
| GET yaklasan | [] | [] |
| GET gecmis TEVSIK | (30000, 2024-11-30, –), (7000, 2016-01-01, 2024-11-30) | aynı |

SQL (kuyumcu_net), önce ve sonra: `yasal_parametreler` 21, tohum dışı satır 0 (değişmedi).

⚠ kuyumcu_net'te başka bir şeridin `20260930115854_UyrukAlpha3` migration'ı uygulanmamış (`/saglik/hazir` → SEMA_ESKI). Bu şerit onu UYGULAMADI (sahibinin işi).

**Faz B: kuyumcu_test_t1, API 5160 (`ConnectionStrings__Kuyumcu` test DB, parola değişkenden).** Kiracı `PAKETYASAL-1-TEST Tur3B`. Günlük: `…/t3/canli/b-canli.log`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Sistem POST TEVSIK 40.000, +3 gün | 201, kapanan tohum-02 | 201, yasal1-tohum-02 |
| Sistem POST BEYAN_FORMU 3.000.000,01, +5 gün | 201, kapanan tohum-09 | 201, yasal1-tohum-09 |
| GET liste: bugünkü tevsik | 30000 (40000 bugünkü satırlarda yok) | 30000 |
| GET liste: sonrakiler | TEVSIK 40000 2026-10-03; BEYAN 3000000.01 2026-10-05 | aynı |
| GET yaklasan: BEYAN | USTU, yeni 3000000, eski 2000000, eskiTür USTU | aynı |
| GET gecmis TEVSIK (API tüm satırları döner, süzgeç ekranda) | 3 satır | (40000, 10-03, –), (30000, 2024-11-30, 2026-10-03), (7000, …) |
| Ekran geçmişi (tarayıcı) | 40.000 yok, "30 Kasım 2024 – sürüyor" | "30 Kasım 2024 – sürüyor: 30.000 ₺ ; 1 Ocak 2016 – 29 Kasım 2024: 7.000 ₺" |
| Ekran kartı | "Yaklaşan değişiklik: 3 Ekim 2026 tarihinden itibaren 40.000 ₺" | aynı; BEYAN kartında "… 5 Ekim 2026 tarihinden itibaren 3.000.000 ₺ üzeri" |
| Panel bandı | kuruşsuz | "…beyan formu doldurulur: 2.000.000 ₺ üzeri yerine 3.000.000 ₺ üzeri." |

kuyumcu_test_t1'deki kiracı ve iki ileri tarihli satır, tam süitin veritabanını yeniden kurmasıyla gitti (süit sonradan koşuldu). 5160/5161 kapalı (`lsof` boş). Chrome süreçleri kapatıldı.

### T3.7 Tarayıcı ölçümü (başsız Chrome, CDP 9701–9704, `sikisma-olc.js` `__olcTam`)

Her mod/tema bileşimi için kullanıcı satırına `PATCH /kullanicilar/ben/arayuz-modu` ve `PATCH /kullanici/gorunum` yazıldı (200/200). Tema sayfada `data-tema` ile doğrulandı. Bileşimler aynı kullanıcıyla sırayla koşuldu, paralel değil.

| Rota | Genişlik | Mod × tema | Durum | K1 | K2 % | K3 | K4 | K5 TRY | K7 | K8 | K9 | K10 | K11 | K12 | Yatay | "X,01" |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /yasal-sinirlar | 1280 | BASİT/PRO × açık/koyu (4) | açılış | 0 | 98 | 0/0 | 1,3×9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 1280 | 4 | hepsi açık | 0 | 98 | 0/0 | 5,1×20 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 375 | 4 | açılış | 0 | 97 | 0/0 | 2,1–2,2×9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar | 375 | 4 | hepsi açık | 0 | 97 | 0/0 | 11,0–11,1×20 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| / (Panel, 2 bant) | 1280 | 4 | açılış | 0 | 98 | 0/0 | 1,4×5 (B) · 1,8×0 (P) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| / (Panel, 2 bant) | 375 | 4 | açılış | 0 | 97 | 0/0 | 2,4×5 (B) · 3,2×0 (P) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

K4 (boy): 375 px'te açılış 2,1–2,2 ekran. Sebep, tutar kartlarının tek sütuna dizilmesi (5 kart). Tur 2'deki 2,0'dan artış iki yaklaşan satırından geliyor (canlı test verisi; gerçekte ileri tarihli satır nadirdir). "Hepsi açık" ölçümü tüm katlanırlar açıkken yapıldı ve kullanıcının kendi açtığı durumdur. Dar sütun yok (K2 97–98 %).

Görüntüler `/private/tmp/kuyumcu-serit/YASAL-1/t3/canli/foto/` altında, 24 dosya: `ys_{BASIT,PROFESYONEL}_{aydinlik,karanlik}_{1280,375}_{acilis,hepsi-acik}.png` ve `panel_*_acilis.png`. Ölçüm JSON'ları `…/canli/olc_*.json`.

Read ile görülenler:
- `ys_BASIT_aydinlik_1280_acilis`: 5 tutar kartı. "185.000 ₺ ve üzeri", "200.000 ₺ ve üzeri", "2.000.000 ₺ üzeri", "20.000.000 ₺ üzeri". Tevsik ve beyan formu kartlarında mavi "Yaklaşan değişiklik" satırı.
- `ys_PROFESYONEL_karanlik_375_acilis`: koyu temada yaklaşan satırı koyu bilgi zemininde okunuyor, taşma yok.
- `ys_BASIT_aydinlik_1280_hepsi-acik` kırpımı:
  - Tevsik geçmişi yalnız "30 Kasım 2024 – sürüyor: 30.000 ₺" ve "1 Ocak 2016 – 29 Kasım 2024: 7.000 ₺".
  - Kimlik geçmişi "… 85.000 ₺ ve üzeri …".
  - Beyan formu geçmişi yalnız "Sürüyor: 2.000.000 ₺ üzeri".
- `panel_PROFESYONEL_aydinlik_1280_acilis`: iki mavi bant, kuruş yok.

### T3.8 Test sayıları ve denetim

| Koşum | Toplam | Geçti | Kaldı | Atlandı |
|---|---|---|---|---|
| Tur 2 son tam süit | 7794 | 7793 | 0 | 1 |
| Tur 3 yeni testler (`Tur3` süzgeci, Yasal sınıfları) | 5 | 5 | 0 | 0 |
| `Yasal` süzgeci | 71 | 71 | 0 | 0 |
| Yasal + Menü + Panel + Kılavuz | 402 | 401 | 1 (KATMAN2, aşağıda) | 0 |
| **Tam süit, tur 3 son kod** (trx `/private/tmp/kuyumcu-serit/trx-pYASAL-1-t3/_Ramazan-MacBook-Air_2026-09-30_15_18_00.trx`) | **7847** | **7835** | **11** | **1** (`SATIS3EkranTestleri.DOG_T24…`, önceden atlanan) |

**(7) Yeni kırmızı farkı:** Tur 2 devrinde tam süit 0 kırmızıydı, şimdi 11 kırmızı var. Bu 11'in hiçbiri bu paketin dosyalarına dokunmuyor. Hiçbirinin yığın izinde ya da iletisinde `Yasal*`, `YasalBicim` veya `Sozlukler.YasalSinirlar` geçmiyor. Hepsi eşzamanlı şeritlerin alanındaki sayfa ve bileşenlere ait:

| Kırmızı test | İleti (özet) | Sahibi |
|---|---|---|
| PosEkranTestleri.PosEkrani_CihazListesiniGosterir · P78.PosOranTurkceTestleri.Pos_komisyon_orani… | "1.85 %" bulunamadı / "0,0275" ≠ "2,75" (oran biçimi değişmiş) | KESİF-P (Pos, Bicim) |
| FiyatlandirmaUX4aEkranTestleri.Liste_secilince… · FiyatListesiUrunGorunumuTestleri.Liste_secilince… | "40,000" bulunamadı (sayı biçimi) | KESİF-P (Fiyatlandirma, Bicim) |
| P13EkranTestleri.Isletme_KurKaynagi… · SozlesmeYansimaTestleri.Tasinan_tipler… | ham "ALTINKAYNAK" yerine sade ad / `Isletme.AyarDegisikligi` razor record | KESİF-P (Isletme, 15:05) |
| S1fGorunumKartiEkranTestleri.S1f_E15_Ayarlar… | Ayarlar kart başlığı değişmiş | KESİF-P (Ayarlar) |
| ZOISLEMAEkranTestleri.ZO28b_pos_eslestir… | WaitFor zaman aşımı | KESİF-P (Pos) |
| GorunumSubesiTestleri.K1… · K6… | yeni test sınıfı, şube süzgeci henüz yok (1000,50 ≠ 3000,75; 404 ≠ 200) | SUBE-KAPSAM-A (bu dalganın sıradaki paketi, yarım) |
| KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON… | ApiIstemci fırtına tavanı 61/60 (3/3 koşuda) | ApiIstemci / KATMAN2 sahibi |

Paralel şerit kapısına göre bu paketin ürettiği yeni kırmızı **0**.

`bash denetim.sh` (çıkış 1). Bu paketin sayaçları: YASAL_PARAM_TARIHSIZ 0 ✓, YASAL_PARAM_YAZMA 0 ✓, YASAL_PARAM_OKUMA_DISI 0 ✓, B-3 ESIK_ISTEMCIDE 4/4 ✓, B-3 ESIK_SABIT_SUNUCUDA 0 ✓.

Kalan üç ✗ tek kaynaktan geliyor: `Kuyumcu.Web/Pages/Isletme.razor:717 private sealed record AyarDegisikligi(...)`. Dosya 15:05'te başka bir şerit tarafından değiştirildi (KESİF-P ekran paketleri, `Isletme` onların alanı; bu şerit dokunmadı). Bu kaynak şu üç sayacı kırıyor:
- "Pages+Bilesenler+Layout record tanımı 4 > 3"
- "P-10 BU_PAKETTE_EKLENEN_RECORD +1"
- "P-02 OLCUM_TABANI_KOPYA (record=4)"

### T3.9 Açık kalanlar (tur 3 eki, sahibiyle)

1. **KESİF-P şeridi:** `Isletme.razor` içindeki razor record → `Kuyumcu.Contracts`'a taşınmalı (denetim 3 ✗).
2. **Sahibi başka şerit (kuyumcu_net):** `20260930115854_UyrukAlpha3` kuyumcu_net'e uygulanmamış (`SEMA_ESKI`).
3. **KATMAN2 / ApiIstemci sahibi:** `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` 3/3 koşuda kırmızı, hata "Aynı adrese (api/v1/tercihler/benim) bir dakika içinde 61 istek atıldı; tavan 60". Bu paketin dosyalarıyla ilgisi yok (ApiIstemci/KATMAN2 testleri).
4. Tur 2'nin açık kalanları (§ T2.10) aynen geçerli.

### T3.10 Kurucu / mali müşavir (tur 3 eki)

10. Nakit İşlem Beyan Formu alt sınırı: tebliğ metni "ikimilyonbir TL ... (bu tutarlar dahil)" diyor, program 2.000.000,01 TL'den başlıyor (daha tutucu). Hangi değerin uygulanacağı ve kuyumcuya uygulanıp uygulanmadığı (yükümlülük "finansal kuruluşlar"da) mali müşavir teyidi gerektiriyor. Satır TEYİTSİZ; değer değiştirilmedi.
11. Ekran dili: "ve üzeri" (kimlik, amaç beyanı) ile "üzeri" (beyan formu, ek belge) ayrımı programın karşılaştırmasından türetildi ve kaynak ifadeleriyle uyumlu. Mali müşavir farklı ifade isterse yalnız sözlük değişir.

---

## TUR 2 — Doğrulama bulgularının kapatılması (30.09.2026)

### T2.1 Sonuç

Önceki doğrulamanın 13 bulgusu (5 hakem KRİTİK, 3 ORTA, 4 DÜŞÜK + keşif raporu taraması) kapatıldı ya da gerekçeli olarak YASAL-3/4'e yazıldı. Paket kapısının yedi maddesi sağlandı:

| Madde | Sonuç |
|---|---|
| (1) Build | `dotnet build Kuyumcu.sln` 0 uyarı, 0 hata (son mutasyondan sonra temiz derleme dahil) |
| (2) Paket testleri | Yasal süzgeci 32/32; Yasal + Düzeltme + Değişmez süzgeci 255/255; menü/ekranlar/S1 süzgeci 664/664 |
| (3) Sayaçlar | `denetim.sh` DENETİM TEMİZ (çıkış 0); YASAL_PARAM_* 0, MENU_KAPSAM 0, MENU_AYNI_AD 0 |
| (4) Mutasyon | 25 mutasyonun 25'i en az bir ADI yazılı testi kırmızıya çevirdi, hepsinde `cmp` AYNI (§ T2.5) |
| (5) Canlı | API 5160 + vekil 5161, kuyumcu_net (yazmasız) ve kuyumcu_test_t1 (ileri tarihli yazma) — § T2.6 |
| (6) Test kiracısı | silindi; tenants 12 → 12 |
| (7) Yeni kırmızı | tam süit 7794 test: 7793 geçti, 0 başarısız, 1 atlandı → yeni kırmızı 0 |

### T2.2 Bulgu → yapılan

| # | Bulgu | Yapılan | Kanıt (test / canlı) |
|---|---|---|---|
| ORTA-1 | Geçmişte bitiş günü dahilmiş gibi yazılıyordu | Bitiş HARİÇ olduğundan ekrana son gün (bitiş − 1) yazılır: `YasalBicim.SonGun`. Başlangıcı doğrulanmamış satırın geçmişinde başlangıç tarihi hiç basılmaz ("Sürüyor: %20 (başlangıç tarihi doğrulanmadı)", "Son gün 23 Şubat 2021: 20.000 ₺ (…)") | `Ekran_gecmis_katlanir…` "1 Ocak 2016 – 29 Kasım 2024" kilitli; canlı 16/16 ölçümde aynı metin |
| ORTA-2 | Tablodaki RESMI_TATIL uyarıyı kapatıyor, MASAK hesabı görmüyordu | `BayramEksik` yalnız süre hesabının okuduğu yapılandırmaya bakar (YASAL-3'e kadar). Sistem ucu RESMI_TATIL yazmayı da 409 ile reddeder (satır hiçbir hesabı etkilemezdi) | `Tur2_bayram_eksik_TABLO_satiriyla_KAPANMAZ…`; canlı POST RESMI_TATIL 409 |
| ORTA-3 | Gider pusulası/stopaj/KDV tarihli okunmuyor; ileri tarihli satır gününde uygulanmaz; yeniden başlatma tuzağı | (a) Sistem ucu yalnız programın tarihli okuduğu anahtarlara (`YasalAnahtar.TablodanOkunan`: TEVSIK_HADDI_TL + MASAK_*) yazar; diğerleri 409 "Bu rakamı program henüz tarihli okumuyor". (b) Kapıların tarihli okumaya geçmesi YASAL-4'te (şartname dosya alanı: `GiderPusulasiKapisi` gövdesi `SatisModulu` içinde `new` ile kuruluyor). (c) Yeniden başlatma tuzağı ADR-0005'e **işletim runbook'u** olarak yazıldı; K-7'nin yumuşatılması Bertan'a soru olarak kondu | `Tur2_tablodan_OKUNMAYAN_anahtara…409`; canlı 5 anahtar 409, satır sayısı 21 → 21 |
| DÜŞÜK-4 | Risk penceresinde ilk dönem alttan açıktı | Dönemler pencerenin EN ESKİ `Tarih` gününden başlar (`MinAsync`) → geç girilen/içe aktarılan kayıt kendi gününün eşiğini alır | `K13_ek_pencere_ICINDE_olusturulan_ama_Tarihi_pencereden_ESKI…` (120 gün önceki işlem, eşik 100 gün önce değişti → 1) |
| DÜŞÜK-5 | Kodlanmamış FATURA_HADDI_TL "Doğrulandı" ile etkin kartta | Sunucu her satıra `kullaniliyor` döner (`YasalAnahtar.ProgramdaKullanilan`); kullanılmayanlar (FATURA_HADDI_TL, TEVSIK_BILDIRIM_IS_GUNU, STOPAJ_BEYAN_GUNU, 805, KMTS) "Henüz kullanılmayan sınırlar" katlanırında, başında "Program bu sınırları henüz denetlemiyor; bilgi için gösterilir." | `Tur2_liste…kullanilmayan_isaretli`, ekran testi; canlı `fatura=ys-kapali` |
| DÜŞÜK-6 | STOPAJ_BEYAN_GUNU "26 gün" | Yeni birim `AYIN_GUNU` (migration ile), ekranda "İzleyen ayın 26. günü" | ekran testi; canlı SQL birim AYIN_GUNU |
| DÜŞÜK-7 | Tur 1 canlıda append-only tabloda elle DELETE/UPDATE | Tur 2'de kuyumcu_net'e HİÇ yazılmadı (yalnız 409/403 denemeleri; satır 21 → 21, tohum dışı 0). İleri tarihli 201 yazması ayrı API örneğiyle **kuyumcu_test_t1** üzerinde yapıldı (test süreçleri bu veritabanını her koşumda yeniden kurar). **Yetim denetim kaydı:** tur 1'de `SistemYazmaKaydet` ile yazılan kayıt, silinen `STOPAJ_BEYAN_GUNU` ileri tarihli satırını gösteriyor; denetim kaydı değiştirilemez olduğu için silinmedi, burada beyan edilir | § T2.6 |
| KRİTİK-8 | Dayanakta şartname/iç notlar | Yeni kolon `ic_not` (API'ye ve ekrana basılmaz); `dayanak` yalnız resmî atıf. Atfı doğrulanamayan satırda dayanak boş → ekranda "Resmî dayanağı henüz doğrulanmadı." Migration `20260930074122_YasalParametreIcNot` | `Tur2_liste_dayanakta_ic_not_YOK…`, `Tur2_dayanagi_bos_satir…`; canlı: iç not içeren dayanak 0, icNot alanı 0, sayfa metninde "şartname/pakette" yok (16/16) |
| KRİTİK-9 | Panel bandı doğrudan açılışta çizilmiyordu | Bant (ve sayfa) `OturumServisi.Degisti`'ye abone; rol yetkili olunca BİR KEZ yükler. Oturum yokken sayfa "yetki yok" yerine yükleniyor gösterir | `Tur2_bant_oturum_BASTA_NULL_sonra_SAHIP…`, `Tur2_ekran_oturum_BASTA_NULL…`; canlı: girişten hemen sonraki ilk Panel + 3 tam yenilemede `.yasal-bant` = 1/1/1/1 |
| KRİTİK-10 | Sayfaya menüden/Tüm ekranlar'dan ulaşılamıyordu | `MenuTanimi` "Belge ve uyum" grubuna rol süzgeçli (SAHIP/MUDUR/MUHASEBE) öğe; `MenuAdlari.YasalSinirlar` (TR/EN); `MenusuzRotalar`'dan çıkarıldı; "Tüm ekranlar" karosu bundan üretilir (39). Menü sayısı testleri 38→39 / 40→41 gerekçeyle güncellendi | canlı: /ekranlar karo 1 (BASİT + PRO), kenar çubuğu bağlantısı görünür 1 (BASİT + PRO) |
| KRİTİK-11 | Yanlış "ne zamandan beri" ve birim | Başlangıç yalnız DOĞRULANDI satırda yazılır, diğerlerinde "doğrulanmadı"; birim AYIN_GUNU | ekran testi; canlı `kdvBas = "Ne zamandan beri: doğrulanmadı"` |
| KRİTİK-12 | Açılışta açıklama satırı ve uzun gerekçe | `SayfaBasi Aciklama` kaldırıldı; değişmezlik tek satırlık kapalı katlanır "Devletin rakamları — değiştirilemez", gerekçe (`DegismezIpucu`) içinde; ortak bileşenin "Bu kayıt değiştirilemez" başlığı bu katlanırda CSS ile gizlendi (başlık zaten söylüyor, "kayıt" yanlış ad) | canlı: açılışta 1280'de sayfa 1,1 ekran (tur 1: 3 ekran), 375'te 2,0 ekran |
| KRİTİK-13 | Panel'de her gün duran sarı bayram bandı | Panel'den KALDIRILDI (sahibin yapabileceği bir şey yok; tablo ekrandan düzenlenmez, § 5). Durum "Yasal sınırlar → Bu yılın bayram tatilleri" katlanırında yapılacak yolla: "Henüz girilmedi. Bayram günlerini program sağlayıcısı girer; …". Operatör `GET /sistem/yasal-parametreler/saglik` yanıtında `bayramEksik` görür. 2026 bayram tohumu yapılmadı: Diyanet/RG ilanı bu turda birincil kaynaktan okunmadı (mevzuat kuralı) ve MASAK hesabı tabloyu YASAL-3'e kadar okumaz | `Tur2_bayram_eksik_SARI_BANT_Panelde_HIC_cizilmez…`; canlı `bayram=0` |

**Sapma (hakem önerisinden):** KRİTİK-13'te "Bekleyen iş listesine satır" yerine uyarı Panel'den tamamen kaldırıldı. Gerekçe: (1) şartname § 10 Panel.razor'a yalnız tek satırlık bant çağrısı izni veriyor; Bekleyen iş listesine satır eklemek listenin boş durum mantığını (`BekleyenYok`) değiştirmeyi gerektirir; (2) satır sahibe yapılacak bir iş sunmazdı (bakım ByGatech'te, şartname § 11/3 önerisi). Kurucu Bekleyen iş satırını isterse Panel sahibiyle birleştirilecek.

**Keşif raporu (KESIF-BULGULARI-2026-09-30) taraması:** 91 kusurun hiçbiri `/yasal-sinirlar` ya da bu paketin dosyalarında değil (rapor, sayfa oluşmadan önceki kopyada çalıştı). Genel sınıflar bu sayfada ölçüldü: ham kod/teknik metin (K8) 16/16 ölçümde 0 (tur 2'de "UBL-TR" terimi yakalandı ve dayanaktan iç nota taşındı), taşma/kırılma 0, hiza 0, TRY kalıbı 0, yatay kaydırma 0; ham rol kodu basılmıyor (#72 sınıfı). #42 (`/uyum` Risk sekmesinde `MasakRisk:Etkin` anahtarı) ve #72 (`RolFiltresi` ham rol kodu) başka şeritlerin dosyası — açık kalanlarda.

### T2.3 Migration

`20260930074122_YasalParametreIcNot` (build'li `dotnet ef migrations add`; `--no-build` yok). Up: `ic_not varchar(300) NULL` + 21 tohum satırında dayanak/iç not ayrımı (`WHERE id = 'yasal1-tohum-NN'`, tohum dışı satıra dokunmaz) + `STOPAJ_BEYAN_GUNU` birimi `AYIN_GUNU`. Down: özgün dayanak metinleri ve `GUN` birimi geri yazılır, kolon düşer. kuyumcu_net uygulaması: "Applying migration '20260930074122_YasalParametreIcNot'. Done." (satır 18'deki "UBL-TR" düzeltmesi için Down→Up yeniden uygulandı: "Reverting … Done." / "Applying … Done."). SQL: 21 satır; tohum-16 birim AYIN_GUNU; dayanak'ta iç not içeren satır 0; DOĞRULANDI 9, kaynak_url boş 0.

### T2.4 Değişen dosyalar (tur 2)

Api: `Data/YasalParametre.cs` (IcNot, AyinGunu, TablodanOkunan, ProgramdaKullanilan), `Data/Migrations/20260930074122_YasalParametreIcNot.cs` + `.Designer.cs`, `KuyumcuDbContextModelSnapshot.cs` (ef üretti), `Common/YasalParametreServisi.cs`, `Modules/Yasal/YasalParametreModulu.cs`, `Modules/Masak/RiskServisi.cs` (yalnız eşik dönemi başlangıcı), `Modules/Sistem/SistemModulu.cs` (yalnız sağlık ucu). Contracts: `YasalSinirlarSozlesmeleri.cs` (`Kullaniliyor`). Core: `Sozlukler.YasalSinirlar.cs`, `Sozlukler.Menu.cs` (YasalSinirlar adı). Web: `Pages/YasalSinirlar.razor`, `Bilesenler/YasalDegisiklikBandi.razor`, `Servisler/YasalBicim.cs`, `Layout/MenuTanimi.cs`, `wwwroot/css/yasal.css`, `wwwroot/kilavuz.html` (üretici ile). Testler: `YasalParametreTestleri.cs`, `YasalParametreHttpTestleri.cs`, `YasalSinirlarEkranTestleri.cs`, `MenuMimarisiTestleri.cs` (38→39), `S1gMenuTuvalTestleri.cs` (yeni ad). Belgeler: `docs/KULLANIM_KILAVUZU.md` (Yasal sınırlar bölümü), `dokumanlar/02-mimari/adr/0005-…md` (işletim runbook'u). Panel.razor tur 2'de DEĞİŞMEDİ. Yeni test: 8.

### T2.5 Mutasyon kanıtı (tur 2, son kod)

Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-1-t2/` altına kopyalandı → TEK eşleşmeli birebir metin değişikliği (python, eşleşme sayısı 1 değilse iptal; `perl -0777` yerine, razor/C# metnindeki `$`/`@` kaçışı yüzünden) → süzgeçli `dotnet test` → yedek geri → `cmp`. Hepsinde diff 2 satır, derleme hatası 0, `cmp` AYNI. Sonra temiz build 0/0.

| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M1 | `GecerliMi` `<` → `<=` | K1×2, K2×2, K4-saf, K13, K13_ek, K4_K9 HTTP (8) |
| M2 | İşlem günü UTC | K4-saf, K2_sinir, K4_K9 HTTP |
| M3 | Değer yokken 30000 | K1_tohum…503, K3_tablo_bosken…503 |
| M4b | Gece tazelemesi yok | K4_gun_degisince_onbellek_tazelenir… |
| M5 | Geçmiş/bugün kontrolü etkisiz | K5_gecmis_ve_bugun…, Liste_SAHIP…, Tur2_tablodan_OKUNMAYAN… |
| M6 | Açılış eşitlik denetimi etkisiz | K7_appsettings…ACILMAZ |
| M7 | Risk bugünkü eşikle | K13_risk…, K13_ek… |
| M12 | Geçmişte bitiş −1 yok | Ekran_gecmis_katlanir_ilk_acilista… |
| M13 | Bayram uyarısı tabloyla kapanır | Tur2_bayram_eksik_TABLO_satiriyla_KAPANMAZ… |
| M14 | Tablodan okunmayan anahtara yazma açık | Tur2_tablodan_OKUNMAYAN_anahtara…409 |
| M15 | Risk dönemi pencere başından | K13_ek_pencere_ICINDE_olusturulan… |
| M16 | `kullaniliyor` hep doğru | Tur2_liste_dayanakta_ic_not_YOK… |
| M17 | Bant oturum aboneliği yok | Tur2_bant_oturum_BASTA_NULL_sonra_SAHIP… |
| M17b | Sayfa oturum aboneliği yok | Tur2_ekran_oturum_BASTA_NULL_sonra_MUHASEBE… |
| M18 | Doğrulanmamış başlangıç tarihi basılır | Ekran_SAHIP_kartlar… |
| M19 | AYIN_GUNU "26 gün" | Ekran_SAHIP_kartlar… |
| M21 | Açıklama satırı geri | Ekran_SAHIP_kartlar… |
| M22 | Kullanılmayan grup yalnız KAPALI | Ekran_SAHIP_kartlar… |
| M23 | Boş dayanakta sade cümle yok | Tur2_dayanagi_bos_satir… |
| M24 | Menü öğesi silindi | Menu_36…, MENU_KAPSAM_sifir…, S1g Menu_adlari_TUVALLE… |
| M25 | Doğrulanmamış geçmişte tarih basılır | Tur2_dayanagi_bos_satir… |

(Tur 1'in M8–M11'i bu turda değişmeyen kodda; tur 1 sonuçları § T1-7'de.)

### T2.6 Canlı doğrulama

**Faz A — kuyumcu_net, API 5160, YAZMA YOK** (`/private/tmp/kuyumcu-serit/YASAL-1/t2/canli/canli-a.log`). Kiracı `PAKETYASAL-1-TEST Tur2` (01M3RP10E7XWK18M9MP90GWQ8Y).

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET liste SAHIP/MÜDÜR/MUHASEBE/TEZGÂHTAR | 200/200/200/403 | 200/200/200/403; 16 satır, tevsik 30000 |
| Dayanakta iç not / `icNot` alanı | 0 / 0 | 0 / 0 |
| STOPAJ_BEYAN_GUNU birimi · FATURA kullaniliyor · TEVSIK kullaniliyor | AYIN_GUNU · false · true | AYIN_GUNU · False · True |
| Geçmiş TEVSIK | (30000, 2024-11-30, –), (7000, 2016-01-01, 2024-11-30) | aynı |
| tevsik/gunluk 2024-11-29 / 2024-11-30 | 7000 / 30000 | 7000 / 30000 |
| Sistem POST GIDER_PUSULASI_GUN, STOPAJ_BEYAN_GUNU, KDV_GENEL_ORAN, FATURA_HADDI_TL, RESMI_TATIL (+5 gün) | 409 ×5 | 409 ×5 "Bu rakamı program henüz tarihli okumuyor…" |
| Sistem POST TEVSIK dün / bugün / SAHİP JWT / anahtarsız | 409/409/401-403/401-403 | 409/409/403/403 |
| SQL satır sayısı önce/sonra · tohum dışı | 21/21 · 0 | 21/21 · 0 |
| Sistem sağlık | eşit, bayramEksik görünür | 21=21 esit True, bayramYili 2026, bayramEksik True |

**Faz B — kuyumcu_test_t1, API 5160 (`ConnectionStrings__Kuyumcu` test DB)** (`…/canli/b/`). Kiracı `PAKETYASAL-1-TEST Tur2B`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Sistem POST TEVSIK 40.000, +3 gün | 201 | 201, kapananSatirId yasal1-tohum-02 |
| Aynı başlangıç ikinci kez | 409 | 409 |
| yaklasan | kalanGun 3, 30000 → 40000 | kalanGun 3, eski 30000, yeni 40000 |
| Girişten hemen sonra ilk Panel (vekil `/__giris` → `location.replace("/")`) | `.yasal-bant` 1, bayram bandı 0 | 1 / 0 — "3 Ekim 2026 tarihinden itibaren … 30.000 ₺ yerine 40.000 ₺." |
| Panel 3 tam yenileme | 1/1/1 | 1/1/1 |
| `/yasal-sinirlar` tam yükleme | liste var, yetki notu yok, açıklama yok | true / false / false |
| /ekranlar karo (BASİT, PRO) · kenar çubuğu bağlantısı (BASİT, PRO) | 1,1 · görünür | 1,1 · görünür (`kenar-satir`, "Yasal sınırlar") |

Test kiracısı (kuyumcu_net) `zsh kiraci-temizle.sh 01M3RP10E7XWK18M9MP90GWQ8Y --uygula` ile silindi, davet satırı silindi; tenants 12 → 12 (QA-TEST-* kiracılarına dokunulmadı). kuyumcu_test_t1'deki kiracı ve ileri tarihli satır, test süreçlerinin veritabanını yeniden kurmasıyla gitti (tam süit sonradan koşuldu). 5160/5161 kapalı (`lsof` boş).

### T2.7 Tarayıcı ölçümü (başlıksız Chrome, CDP 9711–9714, `sikisma-olc.js`)

`/yasal-sinirlar` — açılış ve tüm katlanırlar açık (16 ölçüm):

| Genişlik | Mod | Tema | Durum | K1 | K3 | K4 (ekran×bölüm) | K5 TRY | K7 | K8 | K9 | K10 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280 | BASİT/PRO | açık/koyu | açılış | 0 | 0/0 | 1,1×9 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | BASİT/PRO | açık/koyu | açılış | 0 | 0/0 | 2,0×9 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 | BASİT/PRO | açık/koyu | hepsi açık | 0 | 0/0 | 5,0–5,4×20 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | BASİT/PRO | açık/koyu | hepsi açık | 0 | 0/0 | 10,8–12,6×20 | 0 | 0 | 0 | 0 | 0 | 0 |

K2 (kart sağ kenarı) 98/97 %; dar sütun yok. Panel `/` bantlı (test DB, 8 kombinasyon açılış): K1/K3/K5/K7/K8/K9/K10/Yatay hepsi 0.

Görüntüler (Read ile incelendi): `/private/tmp/kuyumcu-serit/YASAL-1/t2/canli/foto/` — `ys_{BASIT,PRO}_{aydinlik,karanlik}_{1280,375}_{acilis,hepsi-acik}.png` (16), `panel_*_acilis.png` (8), `b1_giris-sonrasi-panel_1280.png`, `b2_panel-yenileme3_1280.png`, `b3_ekranlar_{basit,profesyonel}_1280.png`, `m_{basit,profesyonel}_faturalar_1280.png`. Gözle görülen: açılışta tek satırlık kilit katlanırı + 6 tutar kartı; tarih uydurulmuyor; geçmiş "1 Ocak 2016 – 29 Kasım 2024"; kullanılmayan sınırlar ayrı; bayram durumu en altta kapalı katlanırda.

### T2.8 Test sayıları

| Koşum | Toplam | Geçti | Kaldı | Atlandı |
|---|---|---|---|---|
| Tur 1 son tam süit | 7778 | 7776 | 1 (kararsız, başka şerit) | 1 |
| Yasal süzgeci (tur 2) | 32 | 32 | 0 | 0 |
| Yasal + Düzeltme + Değişmez | 255 | 255 | 0 | 0 |
| Menü/Basit/Ekranlar/S1 | 664 | 664 | 0 | 0 |
| **Tam süit, tur 2 son kod** (trx `/private/tmp/kuyumcu-serit/trx-pYASAL-1-t2/`) | **7794** | **7793** | **0** | **1** (`SATIS3EkranTestleri.DOG_T24…`, önceden atlanan) |

### T2.9 DoD sözleşmesi (tur 2 değişikliği)

`YasalParametre` değişmez kalır (ekrandan yazma yok; DegismezIpucu gerekçesi kilit katlanırının içinde). Yeni `ic_not` alanı yalnız migration ve sistem ucundan (isteğe bağlı `icNot`) yazılır; API'ye dönmez. Sistem ucu artık yalnız `TablodanOkunan` anahtarlara yazar.

### T2.10 Açık kalanlar (sahibiyle)

1. **YASAL-4:** `GiderPusulasiKapisi` (stopaj, 7 gün), `HurdaModulu` (`PusulaDuzenlemeGunu`), KDV oranı tarihli okumaya geçmeli; geçince anahtarlar `TablodanOkunan`'a eklenir.
2. **YASAL-3:** MASAK süre hesabı `RESMI_TATIL` satırlarını okumalı; o zaman `BayramEksik` tabloyu da sayar, RESMI_TATIL sistem ucuna açılır.
3. **Bertan:** K-7'nin, tablodan okunan anahtarlarda yumuşatılması (runbook ADR-0005 § "İşletim runbook'u").
4. **Panel sahibi:** bayram uyarısının "Bekleyen iş" satırı olarak istenmesi hâlinde Panel.razor'da `BekleyenYok` mantığıyla birlikte (bu şeridin dosya alanı dışında).
5. **ORTAK / Uyum şeridi:** KESİF #42 (`/uyum` Risk sekmesinde yapılandırma anahtarı), #72 (`RolFiltresi` iletisinde ham rol kodu).
6. **SATIŞ şeridi:** `Islemler.razor` `tevsikHaddi ?? 30_000m` istemci yedeği (tur 1 § 12/5).
7. Yetim denetim kaydı (tur 1 canlı sınamasından, § T2.2 DÜŞÜK-7) — bilgi amaçlı, silinmedi.

### T2.11 Kurucu / mali müşavir

Tur 1 § 13'teki 7 soru aynen geçerli. Ek: (8) Yıllık bayram bakımını ByGatech destek mi yapacak (öneri: evet; ekran bunu "program sağlayıcısı girer" diye yazıyor — kurucu teyidi gerekir). (9) 2027 dinî bayram günleri Diyanet ilanından birincil kaynakla doğrulanıp yapılandırmaya girilmeli (bu turda okunmadı; tahmin yazılmadı).

---

# TUR 1 KAYDI (30.09.2026)

## T1-1. Sonuç

Tevsik haddi, MASAK eşikleri ve yasal süreler artık `yasal_parametreler` tablosundan okunuyor. Her değerin bir geçerlilik aralığı var ve değer, **işlemin kendi yerel gününe** göre seçiliyor:

- Bugünkü işlem bugünkü rakamla değerlendiriliyor.
- Geçmiş bir işlem kendi günündeki rakamla değerlendiriliyor.
- İleri tarihli bir değer, günü gelince kendiliğinden devreye giriyor. Sunucuyu yeniden başlatmak gerekmiyor.
- Tabloda o gün için değer yoksa işlem yapılmıyor ve 503 dönüyor. Kod içindeki varsayılan rakama düşülmüyor.
- `appsettings` içindeki değer tablonun bugünkü değerinden farklıysa sunucu açılmıyor.

Ekran tarafında iki yeni parça var:

- **"Yasal sınırlar" sayfası.** Salt okunur. Yalnız Sahip, Müdür ve Muhasebe görür.
- **Panel bandı.** İki ayrı bant çıkıyor:
  - Mavi bant: yeni bir rakam 7 gün içinde başlıyorsa haber verir.
  - Sarı bant: bu yılın bayram tatilleri girilmemişse uyarır.

Kapı maddeleri: yedi paket maddesinin hepsi sağlandı (tablo § 10). Tam süitteki tek kırmızı test bu pakete ait değil ve kararsız bir test (§ 6).

## T1-2. Mevcut durum kontrolü ve sapmalar

Şartnamedeki "mevcut durum" iddiaları kodda doğrulandı: eşikler açılışta tek sefer okunup sabit tutuluyordu ve tarih boyutu yoktu. Aşağıdaki noktalarda şartnameden ayrıldım; her birinin gerekçesi yazılı.

| # | Şartname ne diyordu | Ne yaptım | Gerekçe |
|---|---|---|---|
| S1 | "Tarihsiz özellik silinir" | `TevsikKapisi.Esikler` silindi, yerine `EsiklerGunu(gun)` geldi. `TevsikKurali.Esikler` ve `MasakKuralMotoru.Esikler` kaldı; artık tablodan **bugünün** değerini döndürüyor. | `MasakKapisi` gövdesi ve `SatisModulu`/`CariModulu` başka şeritlerin alanı. Bunlar kapıyı kendi içinde `new` ile kuruyor. Tarih çözümü DI'dan gelen tekil kural nesnelerine taşındı. Sonuç olarak yeni işlem bugünün tablo değerini, geçmiş değerlendirme o günün değerini okuyor; gece yarısı geçişi yeniden başlatma istemiyor. Tarihsiz okuma, `YASAL_PARAM_TARIHSIZ = 0` sayacıyla kilitlendi (§ 7). |
| S2 | Başlangıcı geçmiş bir tarih olan değer 409 | **Bugün** başlayan değer de 409 | Bugün başlayan bir değer, aynı günün sabah işlemini eski, öğleden sonraki işlemini yeni rakamla değerlendirirdi. |
| S3 | Test K-4: "test saati 00:00'ı geçince 35.000 nakit 200" | HTTP testi yarın başlayan 40.000'i sistem ucundan ekliyor ve bugün 30.000,01'in 400 döndüğünü görüyor. Ardından aynı süreçteki aynı tekil kural, yarın 00:01 anında 35.000'i geçiriyor. Ayrı bir saf test, sahte saatle gece tazelemesini ölçüyor. | Satış akışında işlem anı `DateTime.UtcNow`'dan geliyor ve o akış başka şeridin dosyası; saat soyutlaması oraya eklenemedi. |
| S4 | Test K-13: "2024 tarihli işlem" | Risk penceresi (son 90 gün) içinde eşik değiştirildi: 20 gün önce 185.000, 10 gün önce 400.000 | Risk skoru yalnız son 90 günü okuyor; 2024 tarihli bir işlem pencereye hiç girmiyor. Ölçülen kural aynı: işlem kendi gününün eşiğiyle sınıflanıyor. |
| S5 | Menüde "Ayarlar → Yasal sınırlar" | Menü öğesi eklenmedi. Sayfa `MenusuzRotalar` listesinde "Panel.razor" gerekçesiyle duruyor; Panel bandı ve adres çubuğu ile açılıyor. | Menü dosyası (`MenuTanimi`, `Sozlukler.Menu`) ve Ayarlar sayfası bu şeridin dosya alanı dışında. Menü sayısını kilitleyen testler (38/40) başka şeritlere ait. Açık kalanlar listesinde. |
| S6 | Gider pusulası kapısı ve KDV ayarları tablodan okuyacak | Bu iki kapı hâlâ `appsettings`'ten okuyor. Değerleri tabloya yazıldı ve açılış eşitlik denetimine girdi, yani iki kaynak sessizce ayrışamıyor. | `GiderPusulasiKapisi`, `SatisModulu` içinde `new` ile kuruluyor. Tablodan okumaya geçmek YASAL-4'ün işi. |
| S7 | Resmî tatiller tabloya taşınır | Tablo desteği eklendi (`RESMI_TATIL`, `Tatiller(yil)`, `BayramEksik`) ve Panel'deki sarı bant bunu okuyor. MASAK bildirim süresi hesabının tabloyu okuması YASAL-3'e bırakıldı. | `MasakModulu` içinde bu şeride yalnız eşik ucu açık. |
| S8 | Yardım metni | `docs/KULLANIM_KILAVUZU.md` dosyasına "Yasal sınırlar" bölümü eklendi, `kilavuz.html` üreticiyle yeniden üretildi. | Kılavuz testleri, her rotanın kılavuzda bir bölümü olmasını zorunlu tutuyor. |
| S9 | Açılış eşitlik denetimi | Veritabanına ulaşılamazsa ya da tablo yoksa denetim atlanıyor ve günlüğe uyarı yazılıyor (`YASAL_PARAM_DENETIM_YAPILAMADI`). | Bu iki durumun kendi kapısı zaten var: `/saglik/hazir` 503 dönüyor, üretimde bekleyen migration çıkış kodu 3 veriyor. Bu olmadan `YayinAcilisHttpTestleri` testlerinden üçü kırılıyordu. Yasal kapılar tabloyu okuyamazsa işlem yine yapılmıyor. |

## T1-3. Mevzuat (birincil kaynak)

Bu oturumda **yeniden okunan** kaynaklar:

| Anahtar | Değer ve aralık | Kaynak | Alıntı |
|---|---|---|---|
| TEVSIK_HADDI_TL | 7.000 · 01.01.2016 – 30.11.2024 | https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm | "Bu Tebliğ 1/1/2016 tarihinde yürürlüğe girer." |
| TEVSIK_HADDI_TL | 30.000 · 30.11.2024 – | https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm | md.5 "Bu Tebliğ yayımı tarihinde yürürlüğe girer." |
| GIDER_PUSULASI_GUN | 7 · 01.11.2021 – | 7338 s. K. md.23, https://www.resmigazete.gov.tr/eskiler/2021/10/20211026-1.htm | Yürürlük: "yayımı tarihini takip eden ayın başında" |
| MASAK_SIB_IS_GUNU | 10 · 01.04.2008 – | Tedbirler Yön. md.28/2; md.51 yürürlük 1/4/2008 (konsolide PDF, değişiklik dipnotu yok) | "en geç on iş günü içinde" |
| MASAK_SAKLAMA_YIL | 8 · 01.04.2008 – | Tedbirler Yön. md.46/1 (dipnot yok) | "sekiz yıl süre ile muhafaza" |
| MASAK_KIMLIK_ESIGI_TL | 185.000 · 14.01.2023 – | Tedbirler Yön. md.5/1-b, dipnot (6), 6702 s. CK | "“seksenbeşbin” ibaresi “yüzseksenbeşbin”" |

Şartnamenin birincil kaynağına dayanarak **DOGRULANDI** damgasıyla tohumlanan, bu oturumda ayrıca açılmayan değerler:

- `TEVSIK_BILDIRIM_IS_GUNU`: 5 iş günü, 02.08.2024'ten itibaren.
- `FATURA_HADDI_TL`: 9.900 TL (2025) ve 12.000 TL (2026).

Tohum durumu:

- **DOGRULANDI:** 9 satır.
- **TEYITSIZ:** 10 satır. Bunlar:
  - MASAK kimlik eşiğinin eski değerleri 20.000, 75.000 ve 85.000. Konsolide dipnottan tarihleri doğru, ancak cumhurbaşkanı kararlarının yürürlük maddesi ayrıca okunmadı.
  - Amaç beyanı, beyan formu ve ek belge eşikleri. Başlangıç olarak yönetmeliğin yürürlük günü verildi; böylece bugünkü davranış birebir korunuyor.
  - Stopaj oranları ve stopaj beyan günü.
  - KDV genel oranı. Bu pakette birincil kaynağı okunmadı.
- **KAPALI:** 2 satır. Özel matrah kodu 805 ve KMTS kod deseni. Kural açılmadı.

Kaynağı olmayan geçmiş aralık tohumlanmadı; o günler için sorgu 503 veriyor. Örneğin 2016 öncesi tevsik haddi yok.

## T1-4. Veri modeli ve migration

Migration adı: `20260930054500_YasalParametreler`.

- Tablo: `yasal_parametreler`. Kiracıya bağlı değil (`IKiraciVarlik` uygulamıyor), `KuyumcuDbContext` beyaz listesinde gerekçesiyle duruyor.
- İndeksler:
  - Tekil `(anahtar, gecerlilik_baslangic)`.
  - İşlevsel tekil `ux_yasal_param_acik_uc`: aynı anahtarda iki açık uçlu satır olamıyor.
- Tohum: 21 satır.

`kuyumcu_net` veritabanına uygulama:

- `dotnet ef database update` çıktısı: "Applying migration '20260930054500_YasalParametreler'. Done."
- `SELECT teyit, COUNT(*)` sonucu: DOGRULANDI 9 · TEYITSIZ 10 · KAPALI 2.
- DOGRULANDI olup `kaynak_url` alanı boş olan satır sayısı: **0**.

## T1-5. Kod

### T1-Yeni dosyalar

- `Data/YasalParametre.cs`: varlık, anahtar beyaz listesi ve birimler.
- `Common/YasalParametreServisi.cs`:
  - `Sayi`, `Metin`, `Satir`, `IslemGunu` (yerel gün, K5).
  - `Donemler` (ilerleme güvencesi var), `Yaklasan`, `Tatiller`, `BayramEksik`, `AcilisFarklari`.
  - `Ekle`: sistem ucu. Beyaz liste dışı anahtar 400, geçmiş ya da bugün başlayan değer 409, çakışma 409. Önceki satırın yalnız bitiş alanı bir kez dolduruluyor. `DenetimServisi.SistemYazmaKaydet` çağrılıyor ve önbellek tazeleniyor.
  - `YasalAcilisDenetimi`: açılışta tabloyla karşılaştırılan yapılandırma değerleri.
- `Modules/Yasal/YasalParametreModulu.cs`: üç okuma ucu.
  - `GET /api/v1/yasal-parametreler?tarih=` ve `GET .../gecmis?anahtar=`: yalnız SAHİP, MÜDÜR ve MUHASEBE (`[RolGerekli]`).
  - `GET .../yaklasan`: tüm roller (`[HerkeseAcik]`, gerekçeli).
  - Yanıtlar `Kuyumcu.Contracts` tipleriyle kilitli.
- `Pages/YasalSinirlar.razor`, `Bilesenler/YasalDegisiklikBandi.razor`, `Servisler/YasalBicim.cs`, `wwwroot/css/yasal.css` (yalnız `var()` belirteçleri).
- `Kuyumcu.Core/I18n/Sozlukler.YasalSinirlar.cs`: Türkçe ve İngilizce metinler.
- `Kuyumcu.Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs`.

### T1-Değişen dosyalar

- **Tevsik:**
  - `TevsikKurali`: `EsiklerGunu`, tarihli `Degerlendir`, saf statik çekirdek.
  - `TevsikKapisi`: yalnız imza satırı değişti (işlem gününün eşiği) ve `EsiklerGunu` eklendi.
  - `TevsikController`: `?tarih=` parametresi.
- **MASAK:**
  - `MasakKuralMotoru`: `EsiklerGunu`, `Gereksinim` ve `Dogrula`'nın tarihli sürümleri. Bildirim süresi tespit gününden, saklama süresi kayıt gününden okunuyor.
  - `RiskServisi` (K-13): pencere eşik dönemlerine bölünüyor ve her dönem kendi eşiğiyle SQL'de sayılıyor.
  - `MasakModulu`: `esikler?tarih=`.
- **Diğer:**
  - `Program.cs`: servis kaydı ve açılış eşitlik denetimi.
  - `SistemModulu`: `POST /sistem/yasal-parametreler` ve `GET .../saglik`.
  - `SistemKapsami`: izin listesine madde 9 eklendi.
  - `appsettings.json`: yalnız `_not` metinleri.
- **Ortak kayıtlar** (yeni rota için zorunlu):
  - `RotaBasliklari`, `RotaModulleri` (değer `null` = rol kapısı), `MenusuzRotalar`, `Panel.razor` (tek satır), `index.html` (tek link), `DuzeltmePolitikasi` (tek `S(...)` satırı).
  - Sayaç eşlemeleri: `denetim.sh` (`P02_ROTA_BEKLENEN` ve sayfa tavanı 47'den 48'e, yeni YASAL-1 bloğu), `olcum-2026-09-18.json` (ROTA_KAPSAM 48), `qa-turu.js` (48), `RotaBaslikTestleri` ve `P37cPanelVeKapsamTestleri` (48 ve 45).
- `ADR-0005`: tarih boyutu güncellemesi.

### T1-DoD sözleşmesi

| Varlık | Düzenleme yolu |
|---|---|
| YasalParametre | Kiracı ekranından **değişmez**: ulusal rakamdır ve yanlış giriş ceza doğurur. Sayfada `DegismezIpucu` gerekçeyi yazıyor. Doğru yol ileri tarihli yeni değerdir: `POST /api/v1/sistem/yasal-parametreler` (sistem anahtarı, denetim kaydı yazıyor). Tablo yalnız eklemeyle büyüyor (Remove ve ExecuteUpdate sayısı 0). Geçmiş düzeltmesi yalnız migration ile yapılıyor. `DuzeltmePolitikasi` içinde `S(..., Sistem)` olarak damgalı. |

### T1-KESİF raporuyla (30.09) hizalama

Keşif raporundaki kusur sınıfları bu ekranda baştan önlendi:

- Ham kod basılmıyor. Teyit durumu sözlükten rozet olarak çıkıyor; anahtar adı ekranda yok. Testte `DOGRULANDI`/`TEYITSIZ`/`TEVSIK_HADDI_TL` metninin olmadığı ölçülüyor.
- Yetkisiz rolde ham rol kodu yerine sade bir not yazıyor ve istek atılmıyor. Keşif #72'deki "Yükleniyor…" takılması bu sayfada oluşmuyor.
- Para ve tarih biçimi tr-TR (`Bicim.Tl`, "30 Kasım 2024"). Ölçümde TRY kalıbı 0.
- Basit modda yapılandırma anahtarı ya da hukuki jargon görünmüyor (keşif #42 sınıfı). Resmî dayanak metni yalnız kapalı "Dayanak ve geçmiş" bölümünde.
- Hata durumu boş listeye çevrilmiyor (UX-S28).

## T1-6. Test sayıları

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Yeni testler (YasalParametre ve YasalSinirlar süzgeci) | 25 | 25 | 0 |
| İlgili süzgeç: Tevsik, Masak, Risk, Peşin, S23, Sistem, AcikKalan, Mutabakat, ZOISLEMA | 775 | 774 (1 atlandı) | 0 |
| Ekran, menü, rota, kılavuz ve ölçüm süzgeci | 2881 | 2879 → düzeltme sonrası yeşil | 0 |
| Tam süit, tur 1 ilk koşum | 7778 | 7770 | 7 |
| Tam süit, son kod | 7778 | 7776 (1 atlandı) | 1 |

Tam süitin ilk koşumundaki 7 kırmızının 6'sı bu paketin ürünüydü ve düzeltildi:

- DuzeltmePolitikasi damgası eksikti.
- SozlesmeKapsam ×2: yeni istemci tipleri kilitsizdi. Sunucu artık Contracts tiplerini döndürüyor.
- YayinAcilis ×3: açılış denetimi DB hatasını yutmuyordu (§ 2 S9).

Kalan tek kırmızı: `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`. Hata `WaitForFailedException` (bUnit bekleme süresi aşıldı). Aynı sınıf üç kez yalnız başına koşuldu: 156/156 × 3 yeşil. İlk koşumdaki `SATIS3EkranTestleri.Barkod_dizisi...` da aynı biçimde kararsızdı ve tek koşumda geçti. İkisi de Islemler/SATIS şeridinin dosyası; bu paket o dosyalara dokunmadı. **Yeni kırmızı: 0.**

## T1-7. Mutasyon kanıtı

Her mutasyon geri alındıktan sonra `cmp` ile orijinal dosyayla karşılaştırıldı; sonuç her seferinde **AYNI**. Son mutasyondan sonra temiz derleme: 0 uyarı, 0 hata.

| Kod | Değişiklik | Kırmızıya dönen testler |
|---|---|---|
| M1 | `GecerliMi`: `gun < Bitis` yerine `<=` | K1 ×2, K2 ×2, K4-saf, K13, K4_K9 HTTP. İlk denemede K13 sonsuz döngüye girdi; `Donemler` içine ilerleme güvencesi eklenip yeniden koşuldu, 7 kırmızı. |
| M2 | `IslemGunu` yerel gün yerine UTC | K2_sinir, K4-saf, K4_K9 HTTP |
| M3 | Değer yoksa `30000m` dön | K1_tohum…2015_tanimsiz_503, K3_tablo_bosken…503 |
| M4 | Sistem ucu yazınca `Tazele()` silindi | K4_K9 HTTP |
| M4b | Gece (gün değişimi) tazelemesi silindi | K4_gun_degisince_onbellek_tazelenir |
| M5 | Geçmiş/bugün başlangıç kontrolü etkisiz | K5_gecmis_ve_bugun…409, Liste_SAHIP… |
| M6 | Açılış eşitlik denetimi etkisiz | K7_appsettings…ACILMAZ |
| M7 | Risk yalnız bugünkü eşikle | K13_risk…GUNUNDEKI |
| M8 | MASAK kimlik eşiği tarihsiz | Liste_SAHIP… (`masak/esikler?tarih=2022-07-01` = 85.000) |
| M9 | Tevsik kapısı işlem günü yerine bugün | K4_K9 HTTP |
| M10 | Bant ufku 7 yerine 8 gün | K9_bant_8_gun_kala_CIZILMEZ |
| M11 | Rozet ham teyit kodunu bassın | Ekran_SAHIP…ham_kod_yok |

## T1-8. Denetim

`dotnet/denetim.sh` sonucu: **DENETİM TEMİZ** (çıkış 0). Yeni sayaçlar ve ilgili çıktılar:

- `YASAL_PARAM_TARIHSIZ` 0 (K-8)
- `YASAL_PARAM_YAZMA` 0
- `YASAL_PARAM_OKUMA_DISI` 0
- Pages dosya sayısı 48 (tavan 48)
- ROTA_KAPSAM 48

## T1-9. Canlı doğrulama

Ortam: API 5160 (`kuyumcu_net`), vekil 5161. Test kiracısı `PAKETYASAL-1-TEST Sinir` (01M3RFD9W1H7MTSRPCJHDPS9R2). Günlükler: `/private/tmp/kuyumcu-serit/YASAL-1/canli/canli.log` ve `canli2.log`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /yasal-parametreler` SAHİP / MÜDÜR / MUHASEBE / TEZGÂHTAR | 200 / 200 / 200 / 403 | 200 / 200 / 200 / 403. Tevsik 30000, DOGRULANDI, 16 satır. |
| `?tarih=2024-11-29` tevsik | 7000 | 7000 |
| `?tarih=2015-12-31` tevsik satırı | yok | yok |
| `gecmis?anahtar=TEVSIK_HADDI_TL` | 30000 (açık uçlu), 7000 (2016-01-01 – 2024-11-30) | aynı |
| `tevsik/gunluk` (bugün / 2024-11-29) | 30000 / 7000 | 30000 / 7000 |
| `masak/esikler` (2022-07-01 / bugün) | 85000 / 185000 | 85000 / 185000, saklama 8, bildirim 10 |
| Sistem POST: geçmiş tarih / bugün / SAHİP JWT / anahtarsız | 409 / 409 / 401-403 / 401-403 | 409 / 409 / 403 / 403 |
| Sistem POST: ileri tarih (+5 gün) / aynı başlangıç ikinci kez | 201 / 409 | 201 (`kapananSatirId=yasal1-tohum-16`) / 409 |
| `yaklasan` | ileri tarihli satır, kalan 5 gün | STOPAJ_BEYAN_GUNU 5 gün, 26 → 27, bayramEksik true |
| `sistem/.../saglik` | önbellek = veritabanı | 22 = 22, esit true |
| Nakit tahsilat 30.000,01 / 30.000,00 | 400 / 200 | 400 "Tevsik zorunluluğu… 0,01 TL aşıyor" / 200 |
| SQL: kiracının hareketleri | yalnız 30.000,00 | tek satır: -30000.00 NAKIT, nakit_tutar 30000.00 |

Canlı testteki ileri tarihli satır, gerçek tevsik haddini etkilememesi için kullanılmayan bir anahtara (STOPAJ_BEYAN_GUNU) yazıldı. İş bitince silindi ve `yasal1-tohum-16` satırı yeniden açık uçlu yapıldı. Kontrol: 21 satırın 21'i tohum, 16'sı açık uçlu.

## T1-10. Tarayıcı ölçümü

Başlıksız Chrome kullanıldı: `tur/tur.mjs` ve kendi `acik.mjs` betiğim (CDP portları 9701–9719). Ölçüm aracı `sikisma-olc.js`. `/yasal-sinirlar` sayfasında tüm katlanır bölümler açıkken:

| Genişlik | Mod | Tema | K1 | K3 taşan/kırık | K5 TRY | K7 | K8 jargon | K9 binen | K10 | Yatay | K2 % | K4 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280 | BASİT | açık / koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | 2.8×18 |
| 375 | BASİT | açık / koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | 5.7×18 |
| 1280 | PRO | açık / koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | 2.8×18 |
| 375 | PRO | açık / koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | 5.8×18 |

K4 değerleri kullanıcının bütün bölümleri açtığı durumdur. Açılış hâlinde (analiz3):

- 1280'de K4 bulgusu yok.
- 375'te 2.5×8. Altı tutar kartı açık; süre/oran ve kullanılmayan sınırlar kapalı geliyor ("zorunlu önce").
- Panel (`/`) BASİT 375'te 2.1×5. Bu bu paketten önce de olan bir durum.
- "SERBEST-YUZEN" bulgusu 375'teki global arama katmanı (kabuk bileşeni). Bu paketin değil.

Ekran görüntüleri (Read ile incelendi):

- `/private/tmp/kuyumcu-serit/YASAL-1/canli/tur3/foto/`: `yasal_sinirlar__{BASIT,PRO}_{aydinlik,karanlik}_{1280,375}.png` ve Panel bandı için `kok__…png`. Mavi ve sarı bant 1280 PRO koyu ve 375 BASİT açıkta görüldü.
- `/private/tmp/kuyumcu-serit/YASAL-1/canli/acik/yasal_acik_*.png`: katlanırlar açık 8 kombinasyon. Geçmiş listesi doğru: "30 Kasım 2024 – sürüyor: 30.000 ₺ / 1 Ocak 2016 – 30 Kasım 2024: 7.000 ₺". **[Tur 2 düzeltmesi: bu kayıt YANLIŞTI — bitiş hariç olduğundan "29 Kasım 2024" yazılmalıydı; § T2.2 ORTA-1.]**

İlk turda sayfa 1280'de 3 ekran, 375'te 6,2 ekran boyundaydı ("tren"). Düzeltme:

- Kart ızgarası 3 sütuna çıkarıldı.
- Her karttaki iki katlanır tek "Dayanak ve geçmiş" bölümünde birleşti.
- Süreler/oranlar ve kullanılmayan sınırlar kapalı gruplara alındı.

## T1-11. Test kiracısı

- `zsh kiraci-temizle.sh 01M3RFD9W1H7MTSRPCJHDPS9R2 --uygula` çalıştırıldı.
- Davet satırı ayrıca silindi (0 kaldı).
- tenants sayısı: önce **12**, sonra **12**.
- 5160 ve 5161 portları kapatıldı (`lsof` boş).

## T1-12. Açık kalanlar

1. **Menü öğesi (sapma S5):** "Yasal sınırlar" menüye ve "Tüm ekranlar" listesine eklenmedi. Menü sahibinin (P-14 / ORTAK) `MenuTanimi` ile `MenuAdi` kaydını açması ve sayaç testlerini (38/40) güncellemesi gerekiyor.
2. **YASAL-4:** `GiderPusulasiKapisi` (stopaj oranları, 7 gün) ve `HurdaModulu.PusulaDuzenlemeGunu` hâlâ `appsettings`'ten okuyor. Değerler tabloyla eşitlik denetiminde.
3. **YASAL-3:** MASAK bildirim süresi hesabı tatil günlerini hâlâ yapılandırmadan alıyor, tablodaki `RESMI_TATIL` satırlarını okumuyor.
4. **YASAL-2:** geç fark edilen ihlal (TB-15/TB-16) servisi kullanacak. Servis hazır.
5. **SATIŞ şeridi:** `Kuyumcu.Web/Pages/Islemler.razor:518` içinde `tevsikHaddi ?? 30_000m` istemci yedeği var. Tablo tanımsızken ön izleme ucu 503 döndüğünde ekran 30.000 varsayıyor. Sunucu kapısı yine 503 verip işlemi yapmıyor, ama ön izlemedeki bu sessiz varsayılanın kaldırılması gerekiyor.
6. **Kararsız testler:** `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA…` ve `SATIS3EkranTestleri.Barkod_dizisi…` bUnit bekleme süresine takılıyor. Sahibi Islemler/SATIS şeridi.
7. API'nin 403 iletisi ham rol kodu içeriyor ("Bu işlem için gerekli rol: SAHIP veya MUDUR…"). Bu sayfa kendi sade notunu gösteriyor, ama `RolFiltresi` iletisi genel olarak ham kod taşıyor (KESİF #72 sınıfı; ORTAK).

## T1-13. Kurucu ve mali müşavir için sorular

1. Taksitli satışta (TB-15) hangi günün haddi uygulanmalı: işlem günü mü, ödeme günü mü? Kodun önerisi işlem günü; teyit gerekli.
2. `MASAK_AMAC_BEYANI_ESIGI_TL` = 200.000 TL için dayanak bulunamadı. Şu an TEYITSIZ olarak bugünkü davranış korunuyor.
3. Nakit işlem beyan formu (2 M) ve ek belge (20 M) eşiklerinin Resmî Gazete'deki nihai metni doğrulanamadı. TEYITSIZ.
4. Stopaj: %2 oranında 2009 sonrası bir değişiklik olup olmadığı, mükellef olmayan kişi için %0 oranı ve beyan günü (26) doğrulanamadı. Üçü de TEYITSIZ.
5. KDV genel oranı %20 bu pakette birincil kaynakla okunmadı. TEYITSIZ, yalnız eşitlik denetiminde kullanılıyor.
6. Kimlik eşiğinin eski değerleri (20.000, 75.000, 85.000) için değişiklik kararlarının RG yürürlük maddesi ayrıca okunmadı. Başlangıç olarak RG yayım günü alındı, TEYITSIZ.
7. 2026–2027 dinî bayram günleri girilmedi (Diyanet ilanı gerekiyor). Panel'deki sarı bant sahibine bunu hatırlatıyor. Girişi kimin yapacağı (öneri: ByGatech) kurucunun kararı.

## T1-14. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| K-1 | ✓ | `K1_tohum_tevsik_haddi…` (7000 / 30000 / 503) |
| K-2 | ✓ | `K2_sinir…` (UTC 20:59:59 → 7000, 21:00 → 30000) |
| K-3 | ✓ | `K3_tablo_bosken…` (503, hareket 0) |
| K-4 | ✓ | `K4_K9…` HTTP ve `K4_gun_degisince…` (saf); sapma S3 |
| K-5 | ✓ | `K5_gecmis_ve_bugun…` ve canlı 409/403 |
| K-6 | ✓ | `K6_ayni_anahtarda…REDDEDILIR` (DB) ve `K6_…409` (HTTP) |
| K-7 | ✓ | `K7_appsettings…ACILMAZ` |
| K-8 | ✓ | `denetim.sh YASAL_PARAM_TARIHSIZ = 0` |
| K-9 | ✓ | HTTP yaklaşan uç, bUnit 7 gün çizilir / 8 gün çizilmez, canlı ekran görüntüsü |
| K-10 | ✓ | 8 görüntü ve ölçüm tablosu (§ 10) |
| K-11 | ✓ | canlı SQL (DOGRULANDI 9, kaynak eksik 0) ve `K11_…` |
| K-12 | ✓ | Tevsik/Masak/Risk süzgeci 774/774 (1 atlandı) |
| K-13 | ✓ | `K13_risk…` (sapma S4) |
| Build 0/0 | ✓ | son derleme 0 uyarı, 0 hata |
| Mutasyon | ✓ | 12/12 kırmızı, cmp AYNI |
| Test kiracısı | ✓ | 12 → 12 |
| Yeni kırmızı | 0 | § 6 |
