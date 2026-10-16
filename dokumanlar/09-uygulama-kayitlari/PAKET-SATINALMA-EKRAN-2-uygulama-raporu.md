# PAKET SATINALMA-EKRAN-2 — Uygulama raporu

**Durum: BİTTİ (tur 3 eklendi, aşağıda)** (hasımsız paket; kendi kanıtım bağlayıcı). Şerit t5 · API 5166 · vekil 5167 · test DB `kuyumcu_test_t5`. COMMIT YOK. Para/veri/yasal davranışa dokunulmadı. Migration gerekmedi.

## 1. Yapılanlar (kalan madde → uygulama)
| Madde | Yapılan |
|---|---|
| KRİTİK iptalli siparişte "değiştirilemez" + "aktife al" çelişkisi | Önceki turda kapanmıştı; canlıda yeniden doğrulandı: kilit kutusu 0, "alış iadesi" önerisi 0, tek birincil "Siparişi aktife al" (8 + 4 kombinasyon) |
| KRİTİK irsaliye listesinde "Teslim alınan" 2 satır | `Irsaliyeler.razor` Yön hücresi `hucre-tek-satir` (`#irs-yon-<id>`). Ölçüm: satır sayısı 1 (12/12 kombinasyon) |
| İptal bölümünde aynı uyarı iki kez + açıklama satırı | "İptal nedeni en az N karakter" yalnız alan etiketinde kalır: ayrı `*-iptal-kilit` satırı kaldırıldı (sebep düğmenin `title`'ında), sayaç gizlendi (`GerekceAlani.SayacGoster`, varsayılan true, diğer ekranlar AYNEN). Mal kabul formundaki tekrar eden `sa-masak-not` cümlesi kaldırıldı (başlık aynı şeyi söylüyor) |
| Kâğıt dökümde yönerge cümlesi | `CiktiModulu.IrsaliyeBelgesi`: adres kayıtlı değilse "Teslim adresi" satırı HİÇ basılmaz; adres varsa basılır. Canlı: adressiz not `''`, adresli "Teslim adresi: …" |
| MASAK formunda "Fon Kaynağı" / "Ek Belge Referansı" jargonu | `MasakFormu` iki isteğe bağlı parametre (`FonEtiketi`, `EkBelgeEtiketi`; null = diğer ekranlar AYNEN). Mal kabulde "Paranın nereden geldiği" / "Belge numarası (fatura, sözleşme)" (TR+EN). Zorunluluk sunucu kuralıyla aynı (`aria-required=true` testli) |
| "Siparişi aktife al" tek eylemse birincil | Sipariş zaten birincil; irsaliye "İrsaliyeyi aktife al" artık `dugme-birincil` |
| Yıldız (ölçümde bulundu) | 375 px çekmece başlığı "SAS-2026-/00004" kırılıyordu (K3): `satinalma-ekran.css` ≤480 px 20 px tek satır. Çekmece `dt` ağırlığı 600 → 500 (yildiz-tipo-tara 56 kural dışı → 0) |

## 2. Sapmalar
- Odak binlik ayracı, DoD'da değildi; önceki turda kusur bulunamamıştı, değişmedi.
- Yıldız "tuval-kiyas" tabloları (Bugün/Tezgah/Müşteriler/Tüm ekranlar) çalıştırılmadı: dokunulan dosyalar bu ekranlarda kullanılmıyor (`GerekceAlani` varsayılanı AYNEN, `MasakFormu` parametreleri null = aynen, yeni CSS yalnız iki çekmece kimliği ve `#sa-detay-bilgi`/`#irs-detay-bilgi`). Çalıştırılan: `yildiz-tipo-tara.mjs` (/satin-alma + /irsaliyeler × 1280+375 × koyu+açık × BASIT+PRO): KURAL DIŞI 0.

## 3. Testler
Yeni: SE10 (iptal tek uyarı + irsaliye aktife al birincil), SE11 (MASAK etiketleri + zorunlu + tekrar not yok), SE12 (Yön hücresi tek satır); SH03 güncellendi (adres yoksa satır yok). Paket filtresi **44/44**.
Tam süit (son kodla, tek kez): **12429 toplam · 12421 geçti · 5 başarısız · 3 atlandı.** Hiçbiri bu paketin değil (aşağıda sahipleriyle).

## 4. Mutasyon (hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1 | adres yoksa yönerge cümlesi yeniden basılır | SH03 |
| M2 | mal kabulde MASAK etiket parametreleri geçilmez | SE11 |
| M3 | Yön hücresinden `hucre-tek-satir` kalkar | SE12 |
| M4 | irsaliye aktife al `dugme-ikincil` | SE10 |
| M5 | iptal düğmesi `title` sebebi kalkar | SE10 |
| M6 | iptal gerekçesinde sayaç yeniden gösterilir | SE10 |
| M7 | `sa-masak-not` geri gelir | SE11 |
(Anlamsız bir 8. deneme M8 testi kırmadı; hedefi olmayan mutasyondu, sayılmadı.) Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

## 5. denetim.sh
2 ✗, ikisi başkasının: `P-30 DEMO_INSAN_SURESI` (kurucu A1), `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (MASAK-ESZAMAN testi). Benim sayaçlarım ✓.

## 6. Canlı (5166, kiracı `PAKETSATINALMA-EKRAN-2-TEST t5`)
| Adım | Gerçek |
|---|---|
| İptal SAS-00004 → `aktife-al` | 200 `TASLAK`; SQL `TASLAK · NULL`; tekrar 409 |
| İptal IRS-00002 → `aktife-al` | 200 `ACIK`; SQL `ACIK · NULL` |
| Adressiz irsaliye dökümü | `not=''`, "kart bilgisinden" yok |
| Adresli (cari adresi eklenince) | "Teslim adresi: Kapalicarsi Kuyumcular Sk. No:7 Fatih" |
Temizlik: API, vekil durduruldu (5166/5167 boş); `kiraci-temizle.sh --uygula` ile kiracı silindi, ikinci çağrı "Kiracı YOK"; `tenants` 24 → 24.

## 7. Gerçek Chrome ölçümü (başsız, CDP; `olc.mjs` + `sikisma.js`/`ekstra.js`)
Durumlar: /satin-alma liste · iptalli sipariş çekmecesi · eşik üstü mal kabul + MASAK (fon+ek belge açık) · /irsaliyeler liste · iptalli irsaliye · açık irsaliye iptal bölümü · kâğıt dökümü. Platin: 1280+375 × BASIT+PRO × açık+koyu = 8 kombinasyon; Yıldız: 4 kombinasyon (1280 PRO açık, 1280 BASIT koyu, 375 BASIT açık, 375 PRO koyu; 375'te düzeltme sonrası yeniden). `modGercek` = istenen (tümü).
| Ölçüt | Sonuç (tüm kombinasyonlar) |
|---|---|
| K1 gizli görünen · K3 taşan/kırık · K5 TRY · K8 jargon · K10b · K12 · K13 · yatay kaydırma | **0** (Yıldız 375 başlık kırığı düzeltildi: 0) |
| K9 binen | liste 0; çekmece açıkken 6 = "arka plan sayfası ⟂ modal çekmece" çiftleri (ölçer tavanı, önceki raporlarla aynı, görüntüde binme yok) |
| İptalli sipariş: kilit kutusu / "değiştirilemez" / "alış iadesi" | 0 / yok / yok; birincil "Siparişi aktife al" |
| Yön hücresi satır sayısı | 1 ("Teslim alınan", "Gönderilen") |
| İptal bölümü: ayrı kilit satırı / "sipariş" kelimesi | 0 / yok |
| MASAK etiketleri | "Paranın nereden geldiği"; eski jargon yok |
| Kâğıt: adres satırı / yönerge | yok / yok (adressiz) |
Görüntüler (Read 4): `sa-masak__PRO_aydinlik_1280_platin`, `sa-iptalli__PRO_aydinlik_1280_platin`, `irs-acik-iptal-bolumu__PRO_aydinlik_1280_platin` (bu üçü sayaç/not kaldırılmadan ÖNCEKİ kodla; sonra sayısal olarak 12 kombinasyonda yeniden ölçüldü), `sa-iptalli__PRO_karanlik_375_yildiz` (başlık kırığını gösterdi, düzeltildi). Ham: `/private/tmp/kuyumcu-serit/pSE2/olcum/`. **Not:** düzeltme sonrası görüntü okunmadı (sayısal ölçüm 0).

## 8. Dokunulan sayfalar
`SatinAlma.razor`, `Irsaliyeler.razor` — SATINALMA-EKRAN'dan sonra **üçüncü dokunuş**; gerekçe: paket tanımındaki kalan bulguların kapatılması (kurucu temel kuralı, sade ve şık ilkesi).

## 9. Değişen dosyalar
`Api/Modules/Cikti/CiktiModulu.cs` · `Core/I18n/Sozlukler.SatinAlma.cs` · `Web/Pages/SatinAlma.razor` · `Web/Pages/Irsaliyeler.razor` · `Web/Bilesenler/MasakFormu.razor` (2 isteğe bağlı parametre) · `Web/Bilesenler/GerekceAlani.razor` (1 isteğe bağlı parametre) · `Web/wwwroot/css/satinalma-ekran.css` · `Tests/SatinalmaEkranKalanlarTestleri.cs`.

## 10. Açık kalanlar (sahibiyle)
- `RaporDortTestleri.RD_KH01/RD_KH02` (400 gün penceresi; RAPOR sahibi).
- `Yildiz1Testleri.Y1_10_kural_govdelerinde_sabit_renk_yok` (`.yz-tezgah-odeme-govde`; YILDIZ-TEZGAH-ODEME t7).
- `Q95SayimFormTestleri.Q95_ad_BOS_sube_DOLU_hata_yalniz_ADDA` (yeni Sayım sayfası, izlenmeyen dosyalar; SAYIM şeridi). Tek başına da kırmızı.
- `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` (SATIS şeridi; tek başına geçti).
- `P-30`, `GIRDI-2` (denetim).
- `MasakKabulNotu` sözlük anahtarı artık kullanılmıyor (sözlük kaydı duruyor).

## 11. Kurucuya / mali müşavire
Mevzuat değeri eklenmedi. MASAK kademeleri ve zorunluluk aynen; yalnız etiket dili değişti.

---
## TUR 3 (11.10.2026): tasarım hakemi KRİTİK bulgularının kapatılması (hasımsız paket; kendi kanıtım)
**Durum: BİTTİ.** Migration gerekmedi. Para/veri/yasal davranışa dokunulmadı. COMMIT YOK.

### T3.1 Yapılanlar
| Bulgu | Yapılan |
|---|---|
| KRİTİK: iptal bölümünde aynı uyarı iki kez + "Düzeltme şöyle yapılır" satırı | `SatinAlma.razor` ve `Irsaliyeler.razor` iptal bölümü kutu yerine TEK düz cümle (`#sa-iptal-kilit`, `#irs-iptal-kilit`); `AlternatifYol` (ikinci alış iadesi cümlesi) üç yerden kalktı (iptal bölümü ×2, `SatinalmaSiparisi` kilit kutusu). Sunucu/sözlük metni değişmedi |
| KRİTİK: ikinci "Bu kayıt değiştirilemez" kutusu (MASAK kimlik kilidi) | Mal kabul MASAK formunun altındaki `DegismezIpucu Varlik="MasakKimlikTespiti"` kaldırıldı; ekranda kilit kutusu 0, kilit mesajı tek cümle |
Dokunulan sayfalar: `SatinAlma.razor`, `Irsaliyeler.razor` (aynı paketin 3. turu; gerekçe: hakem KRİTİK bulgusu). Tek kalıp: yeni CSS/satır içi stil yok.

### T3.2 Testler
Yeni **SE13** (kabullü siparişte ve defter bağlı irsaliyede iptal bölümü: tek cümle, `.degismez-ipucu` 0, `.degismez-alternatif` 0, "alış iadesi" 1 kez); SE11'e "MASAK kilit kutusu yok" iddiası. Paket filtresi (SatinalmaEkranKalanlar + EksikB1Satinalma) **45/45**. Önceki 44 → 45.
Tam süit (son kodla, tek kez): 10 başarısız, hiçbiri bu paketin (aşağıda sahipleriyle). Build: **0 uyarı 0 hata**.

### T3.3 Mutasyon (hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M9 | sipariş iptal bölümü kutu + AlternatifYol'a döner | SE13 |
| M10 | MASAK kimlik kilit kutusu geri gelir | SE11 |
| M11 | irsaliye iptal bölümü kutu + AlternatifYol'a döner | SE13 |
Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata.

### T3.4 Gerçek Chrome (başsız, CDP; 5166/5167)
12 kombinasyon (Platin 8: 1280+375 × BASIT+PRO × açık+koyu; Yıldız 4) × 9 durum, `modGercek` hepsinde istenen.
| Ölçüt | Sonuç |
|---|---|
| K1 · K3 · K3c · K5 · K8 · K9b · K10b · K12 · K13 · yatay kaydırma | **0** (tüm durumlar) |
| K9 (binen) | liste ekranlarında 0; çekmece açıkken 6 = "arka plan ⟂ modal çekmece" çiftleri (ölçer tavanı, önceki turlarla aynı; görüntüde binme yok) |
| Kabullü sipariş / defter bağlı irsaliye iptal bölümü | tek cümle 1, kutu 0, "Düzeltme şöyle" 0, "alış iadesi" 1 (12/12) |
| İptalli sipariş / irsaliye | kilit kutusu 0; "aktife al" 1, birincil (12/12) |
| MASAK etiketleri | eski jargon yok; irsaliye "Yön" hücresi 1 satır; kâğıt: adressiz = adres satırı yok + yönerge yok; iptal bölümünde "sipariş" kelimesi yok |
Görüntüler (4): `sa-kabullu-iptal__PRO_aydinlik_1280_platin`, `irs-gelen-iptal-bolumu__BASIT_karanlik_375_platin`, `irs-gelen-iptal-bolumu__PRO_karanlik_375_yildiz`, `sa-kabullu-iptal__BASIT_aydinlik_375_yildiz` (`/private/tmp/kuyumcu-serit/pSATINALMA-EKRAN-2/canli/olcum/foto/`). Yıldız tuval-kıyas çalıştırılmadı (yeni CSS yok, yalnız işaretleme sadeleşti); Yıldız görüntülerinde eski tema kutusu sızıntısı yok.

### T3.5 Canlı (5166, geçici kiracı) + SQL
Kabullü sipariş iptali 409 (alış iadesi iletisi) · defter bağlı irsaliye iptali 409 · sipariş `aktife-al` 200, SQL `IPTAL 0` → `TASLAK 1 (iptal_gerekce null)`, tekrar 409 · irsaliye `aktife-al` 200, SQL `IPTAL` → `ACIK` · kâğıt dökümü adressiz: adres satırı ve "kart bilgisinden" yok; cariye adres eklenince "Teslim adresi" basılır. Temizlik: API+vekil durduruldu (5166/5167 boş), kiracı `kiraci-temizle.sh --uygula` ile silindi (ikinci çağrı "Kiracı YOK"; `PAKETSATINALMA-EKRAN-2%` 0). `tenants` 21 → 23: fark paralel şeritlerin kiracıları (PAKETHAKEM2, YILDIZKATMAN, YASAL-KAPI-3-HAKEM, P-SAYIM...), benim değil.

### T3.6 Açık kalanlar (sahipleriyle)
Tam süit kırmızıları: `Y1_10_kural_govdelerinde_sabit_renk_yok` (YILDIZ-TEZGAH-ODEME t7, `.yz-tezgah-odeme-govde`) · `Y5A_04_emanet_satirlari_fifo_storno_esik` (YILDIZ-5A t1) · `K16_A_subesinde…`, `K16_sube_suzgecli…`, `Ayni_cari_gun_GUNLUK…`, `K9_kural_kapali…` (TevsikIhlalDb, tevsik/YASAL şeridi) · `RD_KH01`, `RD_KH02` (RaporDort) · `Q95_ad_BOS_sube_DOLU…` (Sayım) · `Yasal4HttpTestleri.Mutabakat_bugun_iptal…` (YASAL-4). denetim.sh: `P-30 DEMO_INSAN_SURESI` (kurucu A1) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (MASAK-ESZAMAN) başkasının; benim sayaçlarım ✓.
Kurucu/mali müşavire: yeni soru yok.
