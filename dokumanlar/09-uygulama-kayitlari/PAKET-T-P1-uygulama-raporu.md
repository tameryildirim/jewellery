# PAKET T-P1 — PANEL TUVALE (Panel.dc.html) · uygulama raporu

- Tarih: 2026-09-24 (ilk koşum 17:30–18:26, kesildi; "Try again" koşumu 19:45–20:25)
- Roller: Taşkın (backend) + Ertuğ (frontend) · Opus
- Dalga: TUVAL — PANEL + MÜŞTERİLER (v2 · vt `kuyumcu_test_t3` · API 5164 / vekil 5165)
- Durum: **BİTTİ** (şerit kapısının 7 maddesi sağlandı; tam süitte kalan 1 kırmızı T-SATIS şeridine ait, aşağıda sahibiyle)

## 0. Bu koşumda ne yapıldı (devralma)
İlk koşum kodu, testleri, 9 mutasyonun 8'ini ve tarayıcı ölçümünü yapmış, M-TP1-3'te (başka şeridin eşzamanlı derlemesi: `apphost: is already signed`) ve rapor aşamasından önce kesilmişti. Bu koşumda:
1. Geri alınan mutasyon yedekleri kaynakla `cmp` ile karşılaştırıldı, hepsi **AYNI** çıktı.
2. Derleme ve filtreli testler yeniden koşuldu: 116/116 yeşil.
3. M-TP1-3 yeniden koşuldu: TP1_07 kırmızı, `cmp AYNI`.
4. **denetim.sh'ta bu paketin kırdığı bir sayaç bulundu:** `Pages+Bilesenler+Layout record tanımı 6 (tavan 3)` ve `P-10 BU_PAKETTE_EKLENEN_RECORD +3`. Sebep: Panel.razor içine yazılmış 3 istemci record'u (`KarOzeti`, `BankaHesapOzeti`, `BankaListesi`). Düzeltme:
   - `/bankalar` için yeni tip yazılmadı. Zaten kilitli olan `Kuyumcu.Contracts.Sayfalar.Banka.HesapYaniti` kullanıldı; SozlesmeSekilTestleri bu tipi zaten kilitliyor.
   - Kâr özeti `Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs` dosyasına `PanelKarOzeti(decimal KarTl, decimal MaliyetiBilinmeyenAdet)` olarak taşındı (PLAN § 1 md.9: istemci record'u razor'a yazılmaz, buraya yazılır).
5. Taşımadan sonraki tam süitte `SozlesmeKapsamTestleri` 2 kırmızı verdi (`Kilitsiz_tip_listesi_circir_ile_donduruldu`: "yeni kilitsiz tip PanelKarOzeti"; `Kapsam_orani_beyan_edilen_degerle_ayni`: 59→60). Bu kırmızılar **bu paketin ürünüydü**. P-13 örneğinde olduğu gibi `SozlesmeSekilTestleri.Uclar()` tablosuna şekil kilidi eklendi: `/api/v1/raporlar/kar-zarar?baslangic=2026-01-01&bitis=2026-12-31 → PanelKarOzeti`. Sonuçta sözleşme testleri 36/36 yeşil, denetim `SOZLESME_KILITSIZ_TIP 59/98 (tavan 59)` ✓. Yeni mutasyon M-TP1-10 eklendi.
6. Kâr ve banka koduna yakın mutasyonlar yeniden koşuldu (M-TP1-1/5/6/8), hepsi kırmızı döndü ve `cmp AYNI`.
7. API yeniden derlendi ve yeniden başlatıldı. Canlı API doğrulaması ve gerçek Chrome ölçüm matrisi (13 görünüm) yeni ikili dosyalarla **yeniden** alındı. Karşılaştırma görüntüsü yeniden üretildi.
8. Tam süit, denetim, test kiracısının silinmesi ve bu rapor yapıldı.

**Dosya alanı dışına iki dokunuş (gerekçeli):**
- `Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs`: Panel'in kendi sözleşme dosyası. Başka hiçbir şeridin listesinde yok. Record'u razor'dan çıkarmak denetim kuralı gereği zorunluydu. Dosyaya yalnız 1 record eklendi.
- `Kuyumcu.Tests/SozlesmeSekilTestleri.cs`: yalnız 1 satır (yeni tipin şekil kilidi). Cırcır, kilitsiz yeni tipi kabul etmiyor. P-13 aynı yolu izlemişti.

## 1. Spesifikasyon ↔ kod doğrulaması ve sapmalar
| Konu | Tuvalde / spesifikasyonda | Kodda | Karar |
|---|---|---|---|
| Kâr satırı | "Kâr 4.120 ₺" | `GET /raporlar/kar-zarar?baslangic=bugün&bitis=bugün` (SAHIP/MUDUR/MUHASEBE) | Kâr uçtan gelir. **403 → kâr YAZILMAZ** (sıfır uydurulmaz, bant yok). 5xx → HataBandi (UX-S28). `maliyetiBilinmeyenAdet>0` ise rakam basılmaz, "Kâr: maliyet eksik" yazılır ve kayda bağlanır (uydurma yok). Zarar kırmızı. |
| Kasa kırılımı | "Nakit X ₺ · Banka Y ₺" | Nakit = TL kasalar, Banka = `/bankalar` içinde aktif TRY hesaplar | Döviz ve pasif hesap eklenmez (UX-S32). Banka 403 ise banka bacağı yazılmaz, 5xx ise bant çıkar. Döviz kasaları PRO'da / kasalar sayfasında. |
| Altın pozisyonum | "+312,8 g has · Vitrin · Alacak" | AltinPozisyonuKarti | BASİT'te yalnız net has + "Vitrin X g · Alacak Y g". Hurda, emanet, diğer madenler ve DegismezIpucu **silinmedi**: karttaki "Ayrıntı" düğmesi açar, PRO'da hepsi görünür. |
| Kim borçlu? | 3 satır + "Tümü" | `BorcluSatirSayisi = 3`. Sunucudan 500'e kadar satır istenir (UX-S29 sınır bandı korunur). | Toplam sayı silinmedi: "Tümü" bağlantısının title'ında ve görsel-gizli metinde. |
| Bekleyen iş | Renkli nokta + metin + sağda kaynak | Önem → nokta rengi, bildirim türü → sözlükten kaynak etiketi (ham kod yok) | Uyum sayaçları nokta satırı olarak yazılır (emoji yok). "Tümü" bağlantısı **tuvalde yok**, bizde var (gerekçe § 4). |
| Hava / selam | "İstanbul 21°" | S1-j davranışı korunur. Durum metni title'da. | Aynı |
| PRO yüz | — | P-37b ızgarası DEĞİŞMEDİ (S31_03 testi) | Aynı |

Mevzuat hükmü yok (eşik ya da vergi kuralı kodlanmadı). `S31_25` testi basit yüzde eşik sayısı gömülmediğini doğruluyor.

## 2. Değişen / eklenen dosyalar (bu paket)
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`: basit yüz tuval düzeni, kâr ve banka yükleyicileri. Record'lar kaldırıldı.
- `dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor`: `Sade` modu ve "Ayrıntı" düğmesi.
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-panel-musteri.css`: tuval ölçüleri, belirteçli, sabit renk yok.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.PanelTuval.cs`: yeni metinler, TR+EN.
- `dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs`: `PanelKarOzeti` (bu koşumda).
- `dotnet/Kuyumcu.Tests/S31PanelSadeTestleri.cs`: TP1_01…TP1_14 (16 yeni test).
- `dotnet/Kuyumcu.Tests/SozlesmeSekilTestleri.cs`: 1 şekil kilidi satırı (bu koşumda).

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeciyle ilgili satırlar: ` M dotnet/Kuyumcu.Web/Pages/Panel.razor`, `?? …/Sozlukler.PanelTuval.cs`, `?? …/AltinPozisyonuKarti.razor`, `?? …/css/tuval-panel-musteri.css`, `?? dotnet/Kuyumcu.Contracts/`, `?? dotnet/Kuyumcu.Tests/`. Contracts ve Tests dizinleri git'te hiç izlenmiyor, bu yüzden dosya bazında fark görünmüyor. `wwwroot/index.html` içindeki `css/tuval-panel-musteri.css` bağlantısı zaten vardı.

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): `Pages/Panel.razor`, `Bilesenler/AltinPozisyonuKarti.razor`.

Migration: **gerekmedi** (şema değişmedi). Backend ucu eklenmedi. Mevcut `/raporlar/kar-zarar`, `/bankalar`, `/raporlar/gunluk`, `/raporlar/cari-yaslandirma`, `/bildirimler` ve `/pozisyon` uçları okunuyor.

## 3. DoD sözleşmesi
Bu paket yeni varlık ya da kayıt türü **doğurmaz** (salt okuma yüzü). Paneldeki her sayı kendi kaydına bağlanır (UX-S23):

| Öğe | Bağlantı / eylem |
|---|---|
| Satış tutarı | `/raporlar/islem-defteri` |
| Kâr | `/raporlar/kar-zarar` |
| Kasa | `/kasalar` |
| Banka | `/banka` |
| Borçlu satırı | Tahsil et / Öde (mevcut akış) |
| Kim borçlu? "Tümü" | `/cariler` |
| Bekleyen iş "Tümü" | `/bildirimler` |
| Pozisyon "Ayrıntı" | Kartın kalanını açar (istek yok; `aria-expanded` değişir, bUnit TP1_09) |

Hiçbir düğme etkisiz değil. POST yolu yok (S31_25). Yetki ve yasal kapılar değişmedi.

## 4. Tuval karşılaştırması (ASIL KANIT)
Görüntüler:
- Yan yana karşılaştırma: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/T-P1-Panel-1440.png` (sol: tuval png, sağ: bizde, BASİT 1440×900 açık tema, gerçek Chrome, 20:03 koşumu).
- Tüm matris görüntüleri ve `olcum.json`: `ham/TUVAL-KARSILASTIRMA/T-P1-ekran/`.

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Selam | "Günaydın Ramazan", Sora kalın | "İyi akşamlar Ramazan" (saate göre), aynı ölçü | AYNI (metin saate bağlı, S1-j) |
| Tarih · şehir derece | "Çarşamba, 23 Eylül 2026 · İstanbul 21°" | "Perşembe, 24 Eylül 2026 · İstanbul 19°" | AYNI (veri canlı) |
| Birincil düğme | "+ Yeni satış" sağ üstte | Aynı konum, ince çizgi artı | AYNI |
| 3 kısa kart | eşit genişlik: Bugün ne sattım? / Kasada ne var? / Altın pozisyonum | Aynı sıra ve eşit ızgara, büyük sayı + tek satır kırılım | AYNI |
| Satış kırılımı | "3 satış · 10,4 g has · Kâr 4.120 ₺" | "3 satış · 17,1 g has · Kâr: maliyet eksik" | GEREKÇELİ FARK: test kiracısında 3 satırın maliyeti yok, kâr uydurulmaz. Maliyet tamsa "Kâr X ₺" (TP1_01). |
| Kasa kırılımı | "Nakit 44.200 ₺ · Banka 40.000 ₺" | "Nakit 41.147,72 ₺ · Banka 40.000 ₺" | AYNI (kuruş biçimi T-K1 Bicim merkezinden) |
| Pozisyon | "+312,8 g has · Vitrin · Alacak" | "+51,4 g has · Vitrin 51,4 g · Alacak 0,0 g" ve sağ üstte "Ayrıntı" | GEREKÇELİ FARK: "Ayrıntı" düğmesi hurda, emanet, diğer madenler ve DegismezIpucu'nu açar. Özellik silinmez kuralı. |
| Kim borçlu? | ad · renkli durum · tutar · Tahsil et/Öde, 3 satır + Tümü | Aynı düzen. Hasan Kaya "Vade bugün", Zeynep "3 gün kaldı", Mehmet Öz "Biz borçluyuz" −21.437,92 ₺ kırmızı + Öde | AYNI. Tuvalde "−22,0 g has" örneği has bakiyeli borç; bizde TL borç, has satırı TL'siz carilerde aynı yerde basılır. |
| Bekleyen iş | renkli nokta · metin · sağda kaynak | Turuncu nokta (UYARI) · "Vadesi yaklaşan veresiye borcu" · "Vade" | AYNI. Ek "Tümü" bağlantısı GEREKÇELİ FARK: bildirim sınırı (5) aşılınca listenin tamamına gitmek için (UX-S29/S23). |
| Kenar çubuğu / üst bar | — | T-KABUK şeridinin alanı | Kapsam dışı (bu şerit dokunmadı) |

## 5. Gerçek tarayıcı ölçümü (sikisma-olc.js, gerçek Chrome başsız, kendi profili, vekil 5165)
Koşum: 2026-09-24 ~20:00, record taşımasından sonraki derleme. 13 görünüm, hepsi `hazir=True`, şema `platin`.

| Görünüm | K1 | K2 (%) | K3 taşan/kırık | K4 | K5 TRY | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT koyu | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık | 0 | 98 | 0/0 | 1.3×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT koyu | 0 | 98 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT açık | 0 | 97 | 0/0 | 1.7×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT koyu | 0 | 97 | 0/0 | 1.7×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO açık | 0 | 98 | 0/0 | 1.2×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO koyu | 0 | 98 | 0/0 | 1.4×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO açık | 0 | 98 | 0/0 | 1.9×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO koyu | 0 | 98 | 0/0 | 1.6×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO açık | 0 | 97 | 0/0 | 2.7×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO koyu | 0 | 97 | 0/0 | 2.7×0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT açık (son) | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- K4 "tren" kusuru yok: sayfa boyu 2 ekranı aşmıyor. Tek istisna 375 PRO (2.7 kat) ama orada bölüm sayısı 0, yani K4 koşulu (≥ 4 bölüm) oluşmuyor.
- K2 %97–98: dar sütun yok.
- 375'te kartlar tek sütuna iniyor. Borçlu satırında ad + eylem üstte, durum + tutar altta (görüntü `TP1-375-basit-koyu-tam.png`).
- Mod ayarı `PATCH /kullanicilar/ben/arayuz-modu` ile yapıldı ve 200 döndü.
- Açık ve koyu zemin: `rgb(242,244,247)` / `rgb(14,18,25)`.

## 6. Testler
| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Filtre `Panel\|AltinPozisyonu\|Pozisyon` (record taşımasından önce) | 116 | 116 | 0 |
| Filtre `Panel\|AltinPozisyonu\|Pozisyon\|Banka` (record taşımasından sonra) | 198 | 198 | 0 |
| Filtre `SozlesmeKapsam\|SozlesmeSekil\|SozlesmeYansima` (kilitten sonra) | 36 | 36 | 0 |
| Tam süit ara koşum (kilitten önce, trx-pT-P1-2) | 5402 | 5392 | 10 (2'si bu paketin → düzeltildi, 8'i başka şeridin) |
| **Tam süit son koşum** (trx-pT-P1-3, `KUYUMCU_TEST_DB=kuyumcu_test_t3`) | **5403** | **5402** | **1** |

Son tam süitteki tek kırmızı `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir`. Yakaladığı yerler `FisKalemTablosu.razor@957`, `@2206`, `FisVerdimAldim.razor@2630`, hepsi T-SATIS şeridinin (`Bilesenler/Fis/*`) dosyası. Bu paketin yeni kırmızısı **0**.

Ara koşumda başka şeritlere ait kırmızılar da görüldü ve son koşumda kendiliğinden kapandı; o şeritler eşzamanlı çalışıyordu:
- TK1ParaBicimi ×4 ve RaporEkran.IslemDefteri_TL (T-KABUK/T-K1, Bicim.cs)
- Q105_B06 (Q10-5)
- TS1 Nakit_uyarisi (T-SATIS)

Yeni test sayısı: 16 (TP1_01, 02a, 02b, 03, 03b, 04–14) ve 1 şekil kilidi teorisi satırı.

## 7. Mutasyon kanıtı
Yöntem: yedek `scratchpad/mut-yedek/T-P1/` altına alındı → tek eşleşmeli değişiklik → filtreli test → yedek geri kopyalandı → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M-TP1-1 | Banka süzgeci `ParaBirimi=="TRY"` → `true` (döviz eklenir) | TP1_04_Kasa_karti_NAKIT_ve_BANKA_TL_toplar_doviz_ve_pasif_banka_EKLENMEZ | AYNI |
| M-TP1-2 | `BorcluSatirSayisi 3 → 5` | TP1_06_Kim_borclu_EN_COK_3_satir_2_tahsil_1_ode_sayi_SILINMEZ | AYNI |
| M-TP1-3 | Kaynak etiketi varsayılanı → ham tür kodu | TP1_07_Bekleyen_is_satirinda_KAYNAK_etiketi_ham_kod_basilmaz (+S31_20) | AYNI |
| M-TP1-4 | Pozisyon kartı `Tam => true` (sade yok) | TP1_09_Pozisyon_karti_SADE_kisa_Ayrinti_dugmesi_kalanini_acar | AYNI |
| M-TP1-5 | 403'te `karTl = 0` (uydurma sıfır) | TP1_02a_Kar_403_ise_KAR_YAZILMAZ_uydurma_sifir_yok_bant_yok | AYNI |
| M-TP1-6 | Kâr hatası yutulur | TP1_02b_Kar_ucu_PATLARSA_bant_cikar_satis_kalir | AYNI |
| M-TP1-7 | Profil geç gelince yükleyici çağrısı silinir | TP1_14_Profil_ILK_cizimden_SONRA_gelirse_basit_yuz_yukleyicileri_YINE_acilir | AYNI |
| M-TP1-8 | Maliyet eksik koşulu `>0 → >999` | TP1_03_Maliyeti_bilinmeyen_varsa_RAKAM_basilmaz_eksik_der | AYNI |
| M-TP1-9 | CSS satır ızgarası tuval ölçüsü → `auto` | TP1_12_CSS_blogu_TUVAL_OLCULU_ve_SABIT_RENKSIZ | AYNI |
| M-TP1-10 | `PanelKarOzeti` şekil kilidi satırı silinir | SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu + Kapsam_orani_beyan_edilen_degerle_ayni | AYNI |

Kayıtlar `scratchpad/mut-tur1.log`, `mut.log`, `mut-tur3.log`, `mut-tur4.log` dosyalarında. M-TP1-1/5/6/8 record taşımasından sonra yeniden koşuldu. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata, 1 uyarı. Uyarı `EtiketEkranTestleri.cs(145)` xUnit1031, başka şeridin dosyası.

## 8. denetim.sh
Sonuç: `DENETİM BAŞARISIZ`. Kalan 2 ✗ ikisi de T-SATIS şeridinin `Bilesenler/Fis/*` dosyalarında:
- `P-39 TABLO_SARMALAYICISIZ 3`: FisKalemTablosu ×2, FisVerdimAldim.
- `B-3 ESIK_ISTEMCIDE 5 (tavan 4)`: FisOdemeKasa.razor:14/74 ve FisNakitUyari.razor:4/11 içinde "30.000".

Bu paketin sayaçları:

| Sayaç | Değer | Sonuç |
|---|---|---|
| record tanımı | 3 (tavan 3) | ✓ (ilk koşumda 6'ydı, bu koşumda düzeltildi) |
| P-10 BU_PAKETTE_EKLENEN_RECORD | 0 | ✓ |
| P-03 SOZLESME_KILITSIZ_TIP | 59/98 (tavan 59) | ✓ |
| P-18 SABIT_RENK | 0 | ✓ |
| P-37c PANEL_AKIS_DISI | 0 | ✓ |
| ModalPencere | 48 (tavan 55) | ✓ |
| S1-g MENU_TUVAL_UYUM | — | ✓ |

## 9. Canlı doğrulama (API 5164, test kiracısı `PAKETT-P1-TEST Kuyumculuk`)
Yanıtlar okundu (`scratchpad/tp1-canli-2.txt`; ilk koşumun rol denemesi `canli.txt`):

| İstek | Kod | Okunan |
|---|---|---|
| POST /auth/giris (SAHIP) | 200 | Jeton alındı (ekrana basılmadı) |
| GET /raporlar/kar-zarar?baslangic=bitis=2026-09-24 | 200 | satisAdedi 3, karTl 119121.3, maliyetiBilinmeyenAdet 3 → ekranda "Kâr: maliyet eksik" (rakam yok) |
| GET /raporlar/gunluk | 200 | satış adet 3, toplamTl 119121.3, toplamHas 17.1292 → "119.121,30 ₺ · 3 satış · 17,1 g has" |
| GET /kasalar | 200 | Nakit TRY 41147.72 |
| GET /bankalar | 200 | Ziraat "Vadesiz TL" TRY 40000.0, aktif → "Banka 40.000 ₺"; toplam 81.147,72 ₺ |
| GET /raporlar/cari-yaslandirma | 200 | Hasan Kaya 96825.86, Zeynep Demir 11147.72, Mehmet Öz −21437.92 |
| GET /bildirimler | 200 | 2 × VADE_HATIRLATMA/UYARI → turuncu nokta + kaynak "Vade" |
| GET /pozisyon | 200 | ALTIN netGram 51.3876, vitrin 51.3876, alacak 0 → "+51,4 g has · Vitrin 51,4 g · Alacak 0,0 g" |
| TEZGAHTAR: kar-zarar / bankalar / gunluk | 403 / 403 / 403 | Kâr yazılmaz, banka bacağı yazılmaz, satış kartında yetki notu (bUnit TP1_02a, TP1_05, S31_10) |

SQL kanıtı (`kuyumcu_net`, kiracı 01M39Y3C…):
- `kasalar`: Nakit · TRY · 41147.72
- `banka_hesaplari`: Vadesiz TL · TRY · 40000.00 · aktif=1

Bunlar API ve ekrandaki değerlerle birebir.

Kapanış:
- API (PID 60018 ve alt süreci) ve vekil (PID 88074) durduruldu. `lsof` 5164/5165 BOŞ.
- Test kiracısı `information_schema`'dan `tenant_id` kolonu olan 70 tablodan ve `tenants` tablosundan silindi. Sonrasında kalan satır 0.
- tenants 15 → **14**. Başka şeritlerin `PAKET…-TEST` kiracılarına ve `QA-TEST-` kiracılarına dokunulmadı.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Tuval birebir (yerleşim, metin, ölçü, belirteç) | ✓ | § 4 tablosu + karşılaştırma png |
| Özellik silinmez (pozisyon ayrıntıları, döviz kasası, borçlu sayısı) | ✓ | Ayrıntı düğmesi, PRO yüz, "Tümü" title (TP1_06, TP1_09) |
| Kâr uydurulmaz | ✓ | TP1_02a/02b/03 + canlı "maliyet eksik" |
| UX-S28 hata ≠ boş | ✓ | S31_17, S31_23, TP1_02b, TP1_05 |
| UX-S29 sınır | ✓ | S31_15, S31_21 |
| UX-S31 ham kod yok | ✓ | TP1_07 |
| UX-S32 para birimi | ✓ | TP1_04, S31_11 |
| Sözlük TR+EN | ✓ | TP1_13, S31_26 |
| CSS kendi dosyasında, sabit renk yok, app.css'e dokunulmadı | ✓ | TP1_12, SABIT_RENK 0 |
| PRO yüz değişmedi | ✓ | S31_03 |
| DOM id'leri korundu | ✓ | Mevcut bUnit id testleri yeşil |
| Gerçek tarayıcı ölçümü, K1–K11 = 0 | ✓ | § 5 |
| Şerit kapısı 7 madde | ✓ | § 6–9 |

## 11. Açık kalanlar
- **T-SATIS** (`Bilesenler/Fis/*`):
  - `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` kırmızı.
  - denetim `P-39 TABLO_SARMALAYICISIZ 3` ve `B-3 ESIK_ISTEMCIDE 5/4`.
- **Başka şerit**: `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı.
- Denetim bilgi notu (✗ değil): `YAYIN_TAZE` BAYAT. 5199 anlık görüntüsü orkestratör tarafından yenilenmeli.
- Karşılaştırma görüntüsündeki kenar çubuğu ve üst bar farkları T-KABUK'un alanı. Bu paket o dosyalara dokunmadı.

## 12. Kurucuya / mali müşavire notlar
Mevzuat hükmü yok. Kâr rakamı yalnız sunucunun kâr-zarar ucundan gelir. Maliyeti bilinmeyen satış varsa ekran rakam yerine "maliyet eksik" der. Bu, satışların maliyet girişi tamamlanana kadar panelde kâr görünmeyeceği anlamına gelir; bilinçli bir tercih.
