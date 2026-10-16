# PAKET P-64 — Rapor grafiklerinin bağlanması + dönem karşılaştırma deltasının KANITI

**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** C (izole vt `kuyumcu_test_k`, canlı port 5186)
**Tarih:** 2026-09-24 03:52 · **Kaynak:** QA turu 8 triyajı (E47 + C46 birleşik) · **Durum:** BITTI (7 madde sağlandı; ağaçtaki 6 kırmızı başka şeridin — § 9)

---

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)

| İddia (triyaj) | Kodda ölçülen | Karar |
|---|---|---|
| Delta kodu var (`KarZarar.razor:150-163`), testi yok | Doğru: `@if (o.Onceki is { } onc)` bloğu vardı; test fikstürü hep `"onceki":null` | Fikstür ikinci senaryoyla genişletildi (§ 3) |
| `degisimYuzde` null → ekranda '-' | **Sapma:** eski `DegisimMetni` null iken HİÇBİR ŞEY basmıyordu (boş) | `DeltaMetni` null → **"—"** (projenin tüm "bilinmiyor" hücrelerinde kullanılan tire — Marj sütunu, kasa raporu farkı). 0% yazılmaz. Neden tire olduğu `#kz-yuzde-yok` notuyla söylenir |
| KPI kartlarında delta yok | Doğru: yalnız "Dönem kârı" kartında `Alt` vardı, o da karşılaştırma kapalıyken null | Hasılat, Kâr, Dönem kârı (TL) ve HAS Kâr kartlarına ok + yüzde; karşılaştırma KAPALIYKEN delta yazılmaz |
| CubukGrafik/PastaGrafik yalnız 1/46 sayfada | Doğru: yalnız `Raporlar.razor` | Kâr-zarar'a 1 çubuk + 1 pasta, satış analizine 1 çubuk |
| Satış analizi özet kartı yalnız GİRİŞ | Doğru | Kart: `Giriş: …` değer + `Çıkış: … · N Hareket` alt metin |
| "Kapsayıcıya asgari genişlik" | Ortak `app.css` bu şeride KAPALI, satır içi style YASAK, projede scoped CSS yüklenmiyor (`index.html`'de `*.styles.css` yok) | Genişlik CSS ile değil **yerleşimle** çözüldü: grafikler `form-grid-2` ızgarasında (masaüstünde yarım, mobilde tam genişlik). Canlıda ölçüldü: oranların hepsi ≥ %70 (§ 6) |
| Çıkış TL toplamı sunucuda yok | `OdemeAraciSatiri`'nda `CikisTlToplam` yok; `RaporServisi.cs` bu şeridin sınırı dışında | İstemcide sunucunun `girisTlToplam` kuralıyla **aynı** kural: yalnız `paraBirimi == "TRY"` satırlarının `cikisTutar` toplamı. Döviz TL'ye katılmaz (UX-S32) — test M64-8 ile kanıtlı |
| Negatif kâr | **Bulunan ek hata:** `CubukGrafik` negatif değerde eksi yükseklikli `<rect>` üretiyordu (tarayıcı çizmez → zarar "yok" görünürdü) | Sıfır tabanlı çizim; negatif çubuk tabanın altına iner, taban çizgisi basılır. Tüm değerler ≥ 0 iken çıktı eskisiyle aynı (`GrafikKulturTestleri` yeşil) |

Mevzuat: bu pakette mevzuat hükmü YOK (grafik/sunum). Migration: **gerekmedi**. API değişikliği: **yok** (sunucu `onceki` + `degisimYuzde`'yi zaten döndürüyordu — canlıda kanıtlandı, § 6).

## 2. Yapılanlar

- `Kuyumcu.Web/Pages/KarZarar.razor`
  - `#kz-tl-kpi` (yeni id): Hasılat / Kâr / Dönem kârı kartlarında `KpiDelta` → `▲ %110,23 önceki döneme göre`; Kâr kartında marj korunur (`%24,32 · ▼ %48,86 …`). HAS Kâr kartına `karHasYuzde`.
  - `#kz-grafikler` (yeni): `#kz-grafik-cubuk` — Hasılat/Maliyet/Kâr/Dönem kârı; 1. seri seçilen dönem, 2. seri karşılaştırılan dönem (lejantlı; karşılaştırma yoksa 2. seri lejantta yok). `#kz-grafik-pasta` — ürün bazlı maliyet dağılımı (ilk 5 + "Diğer ürünler"; maliyeti ≤ 0 satır dilim olmaz; veri yoksa `BosDurum`).
  - `#kz-karsilastirma` bloğu korunur; yüzdeler `#kz-delta-hasilat`, `#kz-delta-donem-kari` içinde; null ise "Değişim: —" + `#kz-yuzde-yok` açıklaması.
  - P-45 davranışı (maden başına ayrı defter has kartı) DOKUNULMADI; `P45_04` yeşil.
- `Kuyumcu.Web/Pages/SatisAnalizi.razor` — `#sa-ozet` kartı Giriş + Çıkış; `#sa-grafik` ödeme aracına göre giriş/çıkış çubuk grafiği (lejant + "yalnız TL" notu). Grafik yalnız veri varken; boşken mevcut `BosDurum`.
- `Kuyumcu.Web/Bilesenler/CubukGrafik.razor` — negatif değer desteği + isteğe bağlı `Seri1Adi`/`Seri2Adi` lejantı (renk kutusu `fill` öznitelikli SVG — satır içi style yok, sabit renk yok, `var(--grafik-*)`).
- `Kuyumcu.Core/I18n/Sozlukler.RaporGrafik.cs` (YENİ, hub'a dokunulmadı) — TR+EN konumsal record, eksik anahtar = derleme hatası.
- `PastaGrafik.razor`, `KarZararModelleri.cs`: **değişmedi** (cmp ile doğrulandı; `git status`'taki `M` başka paketlerin).

## 3. DoD sözleşmesi

| Varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| Kâr-zarar grafikleri / KPI deltası | Salt okunur | Türetilmiş rapor; sayfada mevcut `DegismezIpucu` (TuretilmisGerekce + RaporDuzeltmeYolu) korunur |
| Satış analizi grafiği / özet | Salt okunur | Aynı; sayfadaki `DegismezIpucu` korunur |

Yeni varlık/kayıt türü YOK → PATCH/DELETE/denetim kaydı gerekmez. Yeni düğme YOK. Yeni form YOK. ModalPencere kullanımı artmadı (denetim: 48/55).

**Dokunulan sayfalar (DoD EK 2):** `Pages/KarZarar.razor`, `Pages/SatisAnalizi.razor`.

## 4. Testler

| Kapsam | Önce | Sonra |
|---|---|---|
| `KarZararEkranTestleri` | 9 | 15 (+6: P64_01, P64_02, P64_03, P64_04, Sayfa_EnAzBirVeriGrafigiCizer, Karsilastir_KpiKartindaDeltaBasar) |
| `SatisAnaliziEkranTestleri` (YENİ dosya) | 0 | 2 (Sayfa_EnAzBirVeriGrafigiCizer, OzetKarti_CikisTutariniDaGosterir) |
| Filtreli koşum (KarZarar + SatisAnalizi + RaporEkran + Raporlar + GrafikKultur + S3fVarlik + P13KartAyarUyum) | — | **63/63 geçti** |
| Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_k`, trx: `scratchpad/trx-pP-64`) | — | 4855 toplam · 4849 geçti · **6 başarısız — hepsi S25b (başka şerit, § 9)** |

Fikstür: sahte API `karsilastir=` parametresi GİTTİĞİNDE `KarZararServisi.Donem`'in gerçek şeklinde dolu `onceki{…}` + `degisimYuzde{…}` döndürür (gerçek sunucu davranışı gibi); ikinci fikstür önceki dönemi 0 ve yüzdeleri null verir. Yer tutucu bulunamazsa fikstür patlar (sessiz eski gövdeye düşmez).

P64_04 ek testtir (spesifikasyonda yok): bulunan negatif çubuk hatasının kanıtı.

## 5. Mutasyon kanıtı (yedek: `scratchpad/mut-yedek/P-64/`, her biri tek eşleşme, geri alındı, `cmp` birebir)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M64-1 | `@if (o.Onceki is { } onc)` → `@if (false && …)` (blok kaldırıldı) | **P64_01**, P64_02 | aynı |
| M64-2 | `null => "—"` → `null => "%0"` | **P64_02** | aynı |
| M64-3 | `var kz = … &karsilastir=…` → `var kz = q;` | **P64_03**, P35_03 | aynı |
| M64-4 | `<CubukGrafik …GelirGiderNoktalari…/>` silindi | **KarZararEkranTestleri.Sayfa_EnAzBirVeriGrafigiCizer** | aynı |
| M64-5 | Hasılat kartındaki `Alt="@KpiDelta(o, o.DegisimYuzde?.HasilatTlYuzde)"` silindi | **Karsilastir_KpiKartindaDeltaBasar** | aynı |
| M64-6 | CubukGrafik `Math.Abs(v)` → `v` (eksi yükseklik) | **P64_04** (GrafikKulturTestleri yeşil kaldı — eski çıktı korunuyor) | aynı |
| M64-7 | SatisAnalizi `<CubukGrafik …GirisCikisNoktalari…/>` silindi | **SatisAnaliziEkranTestleri.Sayfa_EnAzBirVeriGrafigiCizer** | aynı |
| M64-8 | `CikisTl`'den `.Where(ParaBirimi=="TRY")` düşürüldü (döviz TL'ye katılır) | **OzetKarti_CikisTutariniDaGosterir** | aynı |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata**. Uyarılar: `EtiketEkranTestleri.cs(145)` xUnit1031 (S-3A şeridinin dosyası, bu paketin değil) + eşzamanlı derlemeden MSB3026 kopya yeniden denemeleri. Bu pakette bir CS8604 uyarısı çıktı (`u.Ad` nullable) ve düzeltildi (`u.Ad ?? u.Kod ?? u.UrunId`).

## 6. Denetim

`dotnet/denetim.sh` → 285 ✓ · **1 ✗: `Sınırsız ToListAsync (Modules · B-8) 146 (tavan 145 — ARTIŞ)`** — bu paket `Modules/` altına dokunmadı (API değişikliği yok); ihlal başka şeridin. Bu paketin sayaçları ✓: satır içi style 110/196 (artış yok), Pages+Bilesenler record 3/3, ModalPencere 48/55, P-18 SABIT_RENK 0, P-24 GRAFIK_KANITI_GETBBOX ✓.

## 7. Canlı doğrulama (5186, `ASPNETCORE_ENVIRONMENT=Development`, `--no-build`)

Test kiracısı: tek kullanımlık davet (SQL ile `kiraci_davetleri`'ne eklendi) → `POST /auth/kayit` **201**, `PAKETP-64-TEST Kuyumculuk` (`01M38E3A7VCC6S7H7RV354CFFF`). Jeton/parola ekrana basılmadı, dosyaları boşaltıldı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /raporlar/kar-zarar?…&karsilastir=ONCEKI_DONEM` (boş kiracı) | 200, onceki dolu şekil, yüzde null | **200**; `onceki` 2026-08-08–08-31 (24 gün) sıfırlar; `degisimYuzde` dört alan **null** → ekran "—" basacak durum |
| Veri: kasa (`POST /kasalar` 200), ürün (`POST /urunler` 201), kasa girişi 29.000 (200), alış 4×7.000 (200), satış 9.000 / 9.500 / 8.800 (200×3) | 200/201 | Hepsi başarılı. İlk denemeler gerçek iş kurallarına takıldı (MASAK 185.000 TL, tevsik 30.000 TL, kasa bakiyesi yetersiz) → tutarlar düşürüldü |
| 8.800'lük satış önceki döneme taşındı | 1 satır | `UPDATE hareketler SET tarih='2026-08-20 12:00' WHERE id=… AND tenant_id=<test>` → `row_count()=1` (yalnız kendi test kiracısı; rapor fikstürü) |
| `GET /raporlar/kar-zarar?…&karsilastir=ONCEKI_DONEM` | onceki + degisimYuzde DOLU | **200**; `onceki.hasilatTl=8800`, `degisimYuzde={hasilatTlYuzde:110.23, karTlYuzde:-48.86, donemKariTlYuzde:-48.86, karHasYuzde:-48.86}` |
| Ekran `/raporlar/kar-zarar` (headless Chrome + CDP, 1366 px; oturum kayıt yanıtındaki jetonla, form doldurulmadı) — karşılaştırma KAPALI | Delta yok | `#kz-karsilastirma` YOK; KPI alt metinleri yalnız `%24,32` ve `İade adedi: 0` |
| `#kz-karsilastir` = ONCEKI_DONEM | Delta görünür | `#kz-karsilastirma`: "Değişim 2026-08-08 – 2026-08-31 (24) · Hasılat: ₺8.800,00 Değişim: ▲ %110,23 · Dönem kârı: ₺8.800,00 Değişim: ▼ %48,86"; KPI: "Hasılat ₺18.500,00 ▲ %110,23 önceki döneme göre", "Kâr ₺4.500,00 %24,32 · ▼ %48,86 önceki döneme göre", "Dönem kârı … ▼ %48,86 önceki döneme göre"; hata bandı yok |
| getBBox — kar-zarar çubuk | parça > 0, oran ≥ %40 | parça 8 · svg 469 px · içerik 412 px → **%87,8** (karşılaştırmalı: 435 px → **%92,8**) |
| getBBox — kar-zarar pasta | parça > 0, oran ≥ %40 | parça 1 · svg 220 px · içerik 180 px → **%81,8** (kapsayıcı 469 px; svg/kapsayıcı %46,9) |
| `/raporlar/satis-analizi` getBBox | parça > 0, oran ≥ %40 | parça 2 · svg 1000 px · içerik 703 px → **%70,3** |
| Satış analizi özet kartı | Giriş + Çıkış | "NAKIT Giriş: ₺56.300,00 · Çıkış: ₺28.000,00 · 5 Hareket" |

Sonra: API (PID 2139) ve headless Chrome durduruldu — `lsof :5186` boş, `:9337` boş. Kiracı `betikler/kiraci-temizle.sh … --uygula` ile silindi (98 satır + tenants 1); 20 sn sonra `tenant_id` kolonu olan TÜM tablolarda kalan satır **0**; davet satırı silindi (`row_count()=1`); **tenants = 10 (başlangıçla aynı)**. QA-TEST-* kiracılarına dokunulmadı.

API günlüğünde 30 adet `is_calismalari` "Duplicate entry … MASAK_SIB_SURESI / SAKLAMA_ONIZLEME" hatası: aynı vt'ye bağlı birden çok sunucunun (kardeş şeritler) aynı arka plan işini aynı gün anahtarıyla yazmaya çalışması; bu paketle ilgisi yok, bilgi olarak not edildi.

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| kar-zarar ve satis-analizi'nde main içinde parça > 0 SVG (getBBox) | ✓ | § 7: 8 / 1 / 2 parça |
| Dolu onceki ile `#kz-karsilastirma` + önceki hasılat + yüzde metinde | ✓ | P64_01; canlı "₺8.800,00 … ▲ %110,23" |
| degisimYuzde null → '-' (0% değil) | ✓ | P64_02 ("Değişim: —", "%0" yok); M64-2 |
| Özet kartı GİRİŞ + ÇIKIŞ | ✓ | OzetKarti_CikisTutariniDaGosterir; canlı |
| en_genis_px / svg_genislik_px ≥ %40 | ✓ | %87,8 / %81,8 / %70,3 |
| Canlı ONCEKI_DONEM seçiliyken delta görünür | ✓ | § 7 |
| dotnet test 0 başarısız (paket ölçüsü) / denetim TEMİZ (paket sayaçları) | ✓ paket · ✗ ağaç | Bu paketin testleri 63/63; tam süitteki 6 kırmızı ve denetimdeki 1 ✗ başka şeritlerin (§ 9) |

## 9. Açık kalanlar

1. **Tam süit 6 kırmızı — sahibi S-2 TEZGAH FİŞİ / S25b satıcı şeridi:** `S25bSafTestleri.S25b_S2_SaticiKapisi_yalniz_SAHIP_MUDUR_baskasini_secer` (4 veri satırı: MUHASEBE, TEZGAHTAR, "sahip", null), `S25bSaticiHttpTestleri.S25b_H5_TEZGAHTAR_baskasini_secemez_403_kendini_secebilir`, `S25bSaticiHttpTestleri.S25b_H7_personeller_ucu_SAHIP_tumunu_TEZGAHTAR_yalniz_kendini_gorur`. Kaynak `Modules/Satis/SatisModulu.Satici.cs` koşum sırasında (03:47) değişiyordu. Bu paket o dosyalara dokunmadı; fark = 0.
2. **denetim.sh `Sınırsız ToListAsync (Modules · B-8)` 146/145** — `Modules/` altında; bu paket API'ye dokunmadı. Sahibi orkestratör kapısında belirlenmeli.
3. CubukGrafik eksen etiketleri `preserveAspectRatio="none"` yüzünden dar kapsayıcıda küçük/yatay sıkışık görünüyor (ekran görüntüsünde okunaklı ama küçük). Bileşenin eski davranışı; düzeltme `app.css` veya bileşen yeniden tasarımı ister (ortak dosya) — P-62/P-63 veya görsel şeride önerilir.
4. Önceki dönem maliyeti canlı fikstürde 0 çıktı (alış, taşınan satıştan sonra tarihli olduğu için maliyet o tarihte bilinmiyor) — sunucunun mevcut maliyet kuralı; ekran bunu doğru gösterdi, bu paket kapsamında değil.
5. "Çıkış TL" istemcide türetiliyor; sunucu `cikisTlToplam` alanı döndürürse (RaporServisi/SatisRaporlari sahibi) istemci ona geçmeli.

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` içinden bu paketinkiler)

- `dotnet/Kuyumcu.Web/Pages/KarZarar.razor`
- `dotnet/Kuyumcu.Web/Pages/SatisAnalizi.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/CubukGrafik.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.RaporGrafik.cs` (yeni)
- `dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/SatisAnaliziEkranTestleri.cs` (yeni)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-64-uygulama-raporu.md` (bu rapor)

## 11. Kurucuya / mali müşavire not

Mevzuat hükmü içermez. Delta yüzdeleri sunucunun `(şimdi − önceki) / |önceki| × 100` formülüdür; önceki dönem 0 ise yüzde hesaplanmaz ve ekranda "—" yazar.
