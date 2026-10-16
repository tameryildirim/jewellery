# PAKET DEGISIM-GUNSONU — Uygulama raporu

Taşkın (backend) + Ertuğ (frontend), Opus. 03.10.2026, tur 1. Dalga DEGISIM-GUNSONU · t2 · API 5162 / vekil 5163 / CDP 9910 · `KUYUMCU_TEST_DB=kuyumcu_test_t2`.
Kaynak: `PAKET-TOPLU-5-uygulama-raporu.md` (§ 2 devirler, T2-8) + görev metnindeki bulgular ve ek madde.
Kanıtlar: `ham/DEGISIM-GUNSONU-olcum/` (`A/` 48 ölçüm + görüntü — hakemin durumu; `B/`, `C/` senaryo ölçümleri; `son/` 48 ölçüm + görüntü — son kod; `canli/` API yanıtları + SQL).

**Sunucu kodu değişmedi** (RaporModulu.cs ve SatisModulu.cs'e dokunulmadı — gerekmedi, aşağıda gerekçe). **Migration yok.** Fark tanımı, MASAK kimlik kapısı ve tevsik (nakit sınırı) kapısı aynen.

## 1. Madde tablosu

| # | Bulgu | Önce | Sonra | Kanıt |
|---|---|---|---|---|
| G1 | [YÜKSEK] Gün sonu "Fark var" tutarı üstündeki rakamlarla çelişiyor (BASİT) | "Tahsilat 12.485,67" altında "Fark var 27.389,71" + "Kasaya giren para ile kesilen faturalar tutmuyor" | Tutar ADIYLA: **"Faturası kesilmemiş satış: X ₺"**; açıklama farkın türüne göre ("Bu satışların faturası henüz kesilmedi." / "Günün satışı ile kesilen faturalar tutmuyor." / "Bugün kesilen faturalar günün satışından fazla."). "Kasaya giren para" cümlesi kalktı (fark kasayla ilgili DEĞİL). Faturasız satırı adet söyler: "9 satışın faturası yok". Hesap sunucuda (m.Fark), cümle ve PRO kartı AYNI alan | `DG_G_BASIT_fark_satiri_…`, `DG_G_BASIT_mutabik_ise_…` · MDG-1, MDG-7, MDG-20 · `son/foto/gunsonu__BASIT_*` |
| G2 | [YÜKSEK] PRO "TOPLAM TAHSİLAT 12.485 · SATIŞ FATURASI 0 · FARK 27.389" çıkarma gibi okunuyor | Tahsilat ilk kart, fark üçüncü | Kartlar sunucu tanımının sırasıyla: **Satış toplamı − Satış faturası (farka giren) = <farkın adı>**, Durum; Toplam tahsilat farkın yanından ayrıldı (sonda, ikinci sıraya iner). Ölçüm: 103.766,52 − 98.766,52 = 5.000 (son) · 98.766,52 − 5.000 = 93.766,52 (B) | `DG_G_PRO_kartlar_SATIS_eksi_FATURA_esittir_FARK…`, `DG_G_PRO_fatura_satistan_FAZLA…` · MDG-2, MDG-3 |
| G3 | [DÜŞÜK] PRO tahsilat alt metni kalem ortasından kırılıyor, "Havale 0 ₺" | "Nakit · Kart · Havale / 0 ₺" | Sıfır kalem yazılmaz; her kalem bölünmez (U+00A0) → satır kalem sınırında kırılır ("Nakit 73.526,66 ₺ ·" / "Kart 57.724,26 ₺") | `DG_G_PRO_faturasiz_fark_karti…`, `PDE1_…` · MDG-4, MDG-5 |
| G4 | [DÜŞÜK] Kırılım tablosu sol dolgu ~5 px ≠ kart başlığı 25 px; adet yazımı tutarsız; faturasız tablosunda Tutar+Ödeme yapışık | ölçüm | Dolgu kartla eşit (1440: satır 25 = başlık 25; 375: 17 = 17 — `tdSol`/`baslikSol`); tek kalıp "N adet · tutar" (stopaj satırı "N adet · stopaj X"); faturasız tablosu tam genişlik (1122/1124 px), Saat %30, Ödeme türü sağa yaslı | `DG_G_PRO_ozet_tablosu_adet_TEK_kalip`, `DG_C_sabit_renk_YOK…` · MDG-6 · `son/foto/gunsonu-pro__PRO_karanlik_1280.png` |
| B1 | [ORTA] Değişim seçiliyken sayfa başlığı "Satış" | Üst bar h1 rotadan | Üst bar h1 = seçili tür ("Değişim" BASİT, "Takas" PRO; diğer türler de kendi adı). Yeni `Layout/UstBarBasligi.razor` sayfanın adını **sekme yuvasından AYRI** bir alana yazar → kur şeridi ve Basit/Profesyonel anahtarı GİZLENMEZ (TOPLU-5'te sekme yuvası denemesinin yan etkisi yok — ölçüldü: `kur:true, mod:true`). Rota değişince eski başlık okunmaz | `DG_B_Degisim_secilince…`, `DG_B_UstBar_h1…`, `DG_B_yuva_basligi…` · MDG-13, MDG-14, MDG-15 |
| T1 | [YÜKSEK] BASİT Değişim tuvaldeki Verdim/Aldım fiş kalıbında değil | uzun form, title-case etiketler, iki kimlik kutusu, "gider pusulası" jargonu, Vazgeç yok | `FisTakasTuval`: satış tezgâh fişiyle aynı kalıp — **VERDİM · ÜRÜN** (tek arama kutusu, adet, fiyat, "Ürün tutarı") / **ALDIM · ESKİ ALTIN** (ayar, gram, has fiyatı, "N has · değer"); altta **Fark** çubuğu ("38.933,98 ₺ — Müşteri öder") ve **Farkın ödemesi** (yalnız fark varken). Etiketler cümle düzeni ("Gram", "Has fiyatı (1 gram)", "Fiyat"). Kimlik: BASİT başlık "Eski altını getiren", not jargonsuz, MASAK kutusu açılınca satıcı kümesi çerçevesiz → ekranda TEK kimlik kutusu (ölçüm `gorunenKimlikBasligi: 1`). Vazgeç (istek atmaz). id'ler, bağlamalar, POST gövdesi AYNEN; PRO AYNEN | `DG_T_*` (5) · MDG-11, MDG-12, MDG-16, MDG-18, MDG-19 · `son/foto/degisim-*__BASIT_*` |
| N1 | [EK MADDE, yasal tutarlılık] Değişimde 30.000 ₺ üstü fark Nakit kalıyor | Satış/Alış'ta Kart/Havale'ye geçiyor | Aynı kaynak (sunucunun had'i `/tevsik/gunluk`; istemcide sayı YOK): \|fark\| > had → müşteri öderse **Kart**, biz ödersek **Havale**; tam hadde nakit serbest. İki kök neden bulundu ve düzeltildi: (a) sayfa Değişim'e Satış'ta had alınmadan geçerse had hiç sorulmuyordu → Değişim'e geçince sorulur (`TakasHaddiYukle`); (b) **canlı ölçümde bulundu**: fark yön değiştirince (müşteri öder → müşteriye iade) otomatik seçilen Kart, Havale'ye dönmüyordu → otomatik seçim yönle birlikte değişir (elle seçim ezilmez) | `DG_N_*` (8 bUnit + 10 saf) · sunucu `DG_N_sunucu_takas_nakit_farki_sinirda` (4) · MDG-8, MDG-9, MDG-10 · canlı § 4 |

### Spesifikasyondan sapmalar (gerekçeli)
- **Sunucu (RaporModulu `pos-fatura-mutabakati`) değişmedi.** Görev "kartta yazan tutar ile cümledeki fark aynı hesaptan gelmeli, hangisi doğru kanıtla" diyor: ikisi de `m.Fark`; sunucu farkı = satış toplamı − fişe bağlı − bağsız fatura, üç senaryoda bağımsız SQL ile birebir tuttu (§ 4). Sorun hesap değil ADLANDIRMA idi; bulgu da "Fark hesabına dokunulmasın" diyor.
- **SatisModulu değişmedi.** Takas nakit kapısı (`tevsik.Dogrula(|fark|)`, kasa + NAKIT) zaten Satış/Alış'la aynı kapı; canlıda 30.000,01 → 400 `NAKIT_SINIRI`, 30.000 → 200. Eksik olan istemcinin ön seçimiydi.
- **"Kaydet ve yazdır" etiketi Değişim'e GETİRİLMEDİ:** takas kaydı yazdırılabilir belge (yazdirIdler) üretmiyor; etiket yazdırma vaat edip yazdırmazsa yalan olur (T-S1 kuralı). "Kaydet" kaldı + Vazgeç eklendi. Değişim fişi yazdırma → açık kalan (§ 9).
- **Dosya alanı dışına çıkılan yer (zorunlu, minimal):** üst bar başlığı `Layout/`'tadır. `UstBarSekmeYuvasi.cs` (+ başlık alanı), `UstBar.razor` (1 ifade: yuvadaki başlık ?? rota adı), yeni `UstBarBasligi.razor`. Sekme yuvası (Icerik) davranışı aynen; eşzamanlı şeritler (PATCH-4, MADEN-AYRIMI) Layout'a dokunmuyor. Ayrıca yeni CSS dosyası `css/degisim-gunsonu.css` + `index.html` tek bağ satırı (TOPLU-5 kalıbı).
- 1280 hakem ölçüsüne ek olarak 1440 ve 375 ölçüldü; 960 kabulde istenmedi.

## 2. Mevzuat
Yeni yasal kural/eşik YAZILMADI. Nakit sınırı istemcide sayı olarak YOK: had sunucunun `/tevsik/gunluk` yanıtından okunur (Satış/Alış ile aynı kaynak; sunucuda 459 s. VUK GT tevsik kapısı — mevcut, birincil kaynakla daha önce doğrulanmış hâliyle). Had bilinmiyorsa ön seçim yapılmaz, Nakit kalır, son söz sunucuda (fail-closed; `DG_N_had_BILINMIYORSA…`). Takasa işlem tutarı kuralının uygulanmaması (SatisModulu notu: "mali müşavir teyidi beklemektedir") AYNEN — dokunulmadı.

## 3. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma-olc.js`, parolasız oturum `inceleme-vekili.py`)
Yayın: `dotnet publish -c Release` → `/private/tmp/kuyumcu-serit/DEGISIM-GUNSONU/yayin` (yalıtılmış kopyadan). Matris: BASİT {gün sonu kartı, Değişim boş, Değişim dolu (fark 38.933,98 müşteri öder), Değişim MASAK (fark 146.749,10 iade)} · PRO {gün sonu sekmesi, aynı üç Değişim} × {1440, 1280, 375} × {açık, koyu}.


**Tur `son/` — 48 ölçüm, hata 0, mod uyuşmazlığı 0**

| Senaryo | Mod | Tema | 1440 | 1280 | 375 |
|---|---|---|---|---|---|
| gunsonu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |

**Tur `A/` — 48 ölçüm, hata 0, mod uyuşmazlığı 0**

| Senaryo | Mod | Tema | 1440 | 1280 | 375 |
|---|---|---|---|---|---|
| gunsonu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | BASIT | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | aydinlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| gunsonu-pro | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-dolu | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |
| degisim-masak | PRO | karanlik | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 | K1 0 · K3 0/0 · K5 0 · K8 0 · K9 0 · Y 0 |

Ek senaryo ölçümleri: `B/` (bağsız fatura: BASİT açık 1440/375, PRO koyu 1440/375 — 4 ölçüm, hepsi 0), `C/` (fark 0: BASİT koyu 1440, PRO açık 1440/375 — 3 ölçüm, hepsi 0).
K9 (binen) A/son'da 0 (deneme turunda arama listesi açık kalınca 4 görülmüştü — liste seçimden sonra kapanıyor).

Hedefli ölçümler (son, 1440): BASİT fark satırı "Faturası kesilmemiş satış: 5.000 ₺", açıklama 14 px, "Kasaya giren para" YOK; PRO kartlar [SATIŞ TOPLAMI 103.766,52 · SATIŞ FATURASI 98.766,52 · FATURASI KESİLMEMİŞ SATIŞ 5.000 · DURUM Fark var · TOPLAM TAHSİLAT 131.250,92 (alt: "Nakit 73.526,66 ₺ · Kart 57.724,26 ₺", Havale yok)]; `tdSol 25 = baslikSol 25`; adet ["10 adet · …", "9 adet · …", "1 adet · …", "0 adet · 0 ₺", "2 adet · stopaj 0 ₺", …]; Değişim: h1 "Değişim" (PRO "Takas"), kur şeridi ve mod anahtarı görünür, şerit başlığı görsel gizli (çift başlık yok); dolu fişte ödeme "Kart" + not "Tutar 30.000 ₺ nakit sınırını aştığı için ödeme Kart olarak seçildi"; MASAK fişinde ödeme "Havale", tek kimlik başlığı, "pusula" metni yok.

Read ile bakılan görüntüler: `deneme/foto/gunsonu__BASIT_aydinlik_1440.png`, `deneme/foto/degisim-dolu__BASIT_aydinlik_1440.png` (arama listesi kesilmesi → DG-5 CSS ile düzeltildi), `A/foto/degisim-masak__BASIT_karanlik_1440.png` (tek kimlik kutusu, Havale), `A/foto/gunsonu-pro__PRO_aydinlik_1440.png` (faturasız tablosu yapışıklığı → düzeltildi), `son/foto/gunsonu-pro__PRO_karanlik_1280.png`, `son/foto/degisim-dolu__BASIT_aydinlik_375.png`.

## 4. Canlı doğrulama (API 5162, kiracı `PAKETDEGISIM-GUNSONU-TEST Kuyumculuk` 01M3Z9E3JKA2ZC0HTEHDYXE89E, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış)

Gün sonu — API `GET /raporlar/pos-fatura-mutabakati` ile BAĞIMSIZ SQL (`hareketler` SATIS toplamı − `faturalar` SATIS, IPTAL hariç, yerel gün 03.10 = UTC 02.10 21:00–03.10 21:00):

| Senaryo | İşlem | API fark | SQL fark | Ekrandaki ad | Dosya |
|---|---|---|---|---|---|
| A faturasız satış farkı | tohum (9 satış, fatura yok) | 98.766,52 | 98.766,52 | "Faturası kesilmemiş satış" | `canli/api-A.json`, `sql-A.txt` |
| B bağsız fatura farkı | `POST /faturalar` 5.000 (fişe bağsız) **200** | 93.766,52 (bağsız 1 · 5.000) | 93.766,52 | "Satış ile fatura farkı" | `api-B*.json/txt`, `sql-B.txt` |
| C fark 0 | `PATCH /faturalar/{id}/durum` IPTAL **200** + 9 × `POST /faturalar/fisten` **200** | 0 (mutabık, uyarı yok) | 0,00 (bağlı hareket 9) | "Fark" 0 ₺ · "Tutuyor"; BASİT fark satırı yok | `api-C.txt/json`, `sql-C.txt` |
| D (son ölçüm durumu) | `POST /islemler/satis` KART 5.000 **200** | 5.000 | 5.000,00 | "Faturası kesilmemiş satış" | `api-D.json`, `sql-D.txt` |

Değişim nakit sınırı (`canli/api-takas-sinir.txt`, `sql-takas-sinir.txt`; cari bugün nakitsiz, eski 1 g 24 ayar × 1.000):
- fark 30.000,01 NAKIT → **400** `NAKIT_SINIRI` "30.000,01 TL nakit, 30.000,00 TL nakit sınırını 0,01 TL aşıyor; bu ödeme kartla ya da havaleyle yapılmalı." — SQL: satır YOK.
- fark 30.000,00 NAKIT → **200** (pusula GP-2026-000001) — SQL `TAKAS NAKIT 30000.00` + kasa `NAKIT 30000.00`.
- fark 30.000,01 KART → **200** — SQL `TAKAS KART 30000.01` + kasa `KART 30000.01`.
- 29.999,99: sunucu testinde (`DG_N_sunucu_takas_nakit_farki_sinirda`) ve ekranda (bUnit) kabul/Nakit.
- Ekranda (başsız Chrome): fark 38.933,98 → Kart otomatik; fark −146.749,10 → Havale otomatik.

Kapanış: API ve vekil `kill`; 5162 / 5163 / 9910 dinleyici yok. `zsh kiraci-temizle.sh 01M3Z9E3JKA2ZC0HTEHDYXE89E --uygula` kod 0. SQL: bu kimlik için tenants 0, hareketler 0, faturalar 0. **tenants başlangıç 16 → son 16** (+1/−1 bu paket; arada PATCH-4 şeridinin kiracısı gitti, MADEN-AYRIMI'nınki geldi). QA-TEST-* kiracılarına dokunulmadı. Fikstür ve oturum anahtarı dosyaları `printf ''` ile boşaltıldı.

## 5. Testler
Yeni: `Kuyumcu.Tests/DegisimGunSonuTestleri.cs` — 26 test metodu, Theory dahil 38 koşum, 7 sınıf (saf sınır 10 koşum · Değişim ekranı 14 · yuva saf 1 · gün sonu ekranı 6 · sunucu DB sınırı 4 · üst bar ekranı 1 · CSS/bağ 2). `FullyQualifiedName~Kuyumcu.Tests.Degisim` filtresi 69 koşum verir (adı "Degisim" ile başlayan başka sınıflar dahil) — hepsi yeşil.
Beklentisi güncellenen eski testler (kasıtlı görünüm değişikliği; önce yedekleri `/private/tmp/kuyumcu-serit/DEGISIM-GUNSONU/once-yedek/`): `ParaDemoRaporEkranTestleri.PDE1_…` (sıfırsız, bölünmez kalem), `ZoBelgeAgHatasiTestleri.ZB34_…`, `S34RaporBugunTestleri.S34_10_…`, `S34_12_…` (açıklama/ad), `Toplu5GunSonuTestleri.T5_BASIT_faturasiz_…` (adet cümlesi), `SATIS3EkranTestleri.T17_Takas_*` (BASİT sade etiket).
Filtreli (gerçek ağaç, son kod): Degisim + ZB34 + S34 + PDE1 + Toplu5 + T17_Takas + KesifP5 + TK2 → **193/193 yeşil**.
**Tam süit (son kod, yalıtılmış kopya, `trx-pDEGISIM-GUNSONU`): 9846 test · 9839 geçti · 1 başarısız · 6 atlandı.** Tek kırmızı `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — bu paketten önce de (ilk filtreli koşumda, sunucu koduna dokunulmadan) kırmızıydı; neden: test `DateTime.UtcNow` tarihini sorguya veriyor, hareket yerel güne yazılıyor; koşum yerel 00:45–01:30 (+03) arasıydı → UTC günü bir gün geride (saat dilimi kararsızlığı, yerel 00:00–03:00). Bu paket RaporModulu/Yasal4'e dokunmadı → **yeni kırmızı farkı 0**.
(Not, md.1: ilk tam süit koşumu 2 ek kırmızı verdi — `DG_G_PRO_faturasiz…`, `PDE1_…`: MDG-5 mutasyonu geri alınırken dosya zaman damgası korunduğu için artımlı derleme bayat (mutasyonlu) ikiliyi bıraktı. Dosyalara `touch` → temiz derleme (0/0) → tam süit YENİDEN koşuldu; yukarıdaki sonuç budur.)

## 6. Mutasyon tablosu (20 adet; yöntem: yalıtılmış kopyada yedek → TEK eşleşmeli değişiklik → derle → filtreli test → geri yükle → `cmp` hem yedekle hem gerçek ağaçla AYNI; sonuçlar `mut.log`, `mut-sonuc-*.json`)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| MDG-1 | `Raporlar.GunSonu.cs` — fark türü: faturasız sınıflaması ters | DG_G_BASIT_fark_satiri_ADIYLA_faturasi_kesilmemis_satis_tutar_m_Fark, DG_G_PRO_faturasiz_fark_karti_ADI_ve_sifir_kalem_YOK_kalem_bolunmez, ZB34_Gun_sonu_SADE_aciklama_sozlukten_formul_KAPALI_katlanirda | AYNI |
| MDG-2 | `Raporlar.razor` — PRO ilk kart yine tahsilat (çıkarma okunuşu bozulur) | DG_G_PRO_faturasiz_fark_karti_ADI_ve_sifir_kalem_YOK_kalem_bolunmez, DG_G_PRO_kartlar_SATIS_eksi_FATURA_esittir_FARK_tahsilat_farkin_yaninda_DEGIL | AYNI |
| MDG-3 | `Raporlar.GunSonu.cs` — fatura kartı farka gireni değil kiracı geneli toplamı yazar | DG_G_PRO_kartlar_SATIS_eksi_FATURA_esittir_FARK_tahsilat_farkin_yaninda_DEGIL | AYNI |
| MDG-4 | `Raporlar.GunSonu.cs` — sıfır havale kalemi yazılır | DG_G_PRO_faturasiz_fark_karti_ADI_ve_sifir_kalem_YOK_kalem_bolunmez | AYNI |
| MDG-5 | `Raporlar.GunSonu.cs` — kalem içi bölünmez boşluk yok | DG_G_PRO_faturasiz_fark_karti_ADI_ve_sifir_kalem_YOK_kalem_bolunmez, PDE1_mutabakat_alt_kalemleri_HAVALE_dahil_toplamla_tutar | AYNI |
| MDG-6 | `Raporlar.GunSonu.cs` — adet yazımı eski karışık kalıp | DG_G_PRO_ozet_tablosu_adet_TEK_kalip | AYNI |
| MDG-7 | `Raporlar.razor` — BASİT açıklama yine "kasaya giren para" | DG_G_BASIT_fark_satiri_ADIYLA_faturasi_kesilmemis_satis_tutar_m_Fark, S34_10_Gun_sonu_FARK_VAR_ise_tutar_ve_DUZELTME_baglantisi_cizilir, S34_12_Fark_var_ama_faturasiz_ve_bagsiz_YOKSA_genel_kontrol_baglantisi_cizilir, ZB34_Gun_sonu_SADE_aciklama_sozlukten_formul_KAPALI_katlanirda | AYNI |
| MDG-8 | `Islemler.Degisim.cs` — yön yok: biz ödesek de Kart | DG_N_PRO_degisimde_biz_odersek_HAVALE_otomatik, DG_N_fark_YONU_degisince_otomatik_arac_da_degisir_KART_HAVALE, DG_takas_odeme_varsayilani_sinir | AYNI |
| MDG-9 | `Islemler.Satis4.cs` — fark yönü değişince otomatik araç değişmez | DG_N_fark_YONU_degisince_otomatik_arac_da_degisir_KART_HAVALE | AYNI |
| MDG-10 | `Islemler.Degisim.cs` — Değişimde had alınmaz | DG_N_had_Satista_ALINAMAMISSA_Degisime_gecince_sunucudan_alinir_KART | AYNI |
| MDG-11 | `Islemler.Degisim.cs` — BASİT Değişim eski uzun form | DG_T_BASIT_Vazgec_degisim_fisini_bosaltir_istek_ATMAZ, DG_T_BASIT_degisim_VERDIM_ALDIM_fisinde_id_ler_AYNEN_etiketler_sade, DG_T_BASIT_fark_ALDIM_altinda_yon_ve_tutarla_odeme_bolumu_acilir | AYNI |
| MDG-12 | `FisTakasSatici.razor` — iki kimlik kutusu geri | DG_T_BASIT_kimlik_TEK_kutu_jargon_YOK_MASAK_acikken_satici_kumesi_cercevesiz | AYNI |
| MDG-13 | `Islemler.razor` — sayfa üst bar başlığını yazmaz | DG_B_Degisim_secilince_sayfa_UST_BAR_basligini_Degisim_yazar_Satista_Satis | AYNI |
| MDG-14 | `UstBar.razor` — üst bar sayfa başlığını okumaz | DG_B_UstBar_h1_sayfanin_basligini_yazar_kur_seridi_ve_mod_anahtari_DURUR_sayfa_kapaninca_rota_adi | AYNI |
| MDG-15 | `UstBarSekmeYuvasi.cs` — başlık başka rotaya taşınır | DG_B_Degisim_secilince_sayfa_UST_BAR_basligini_Degisim_yazar_Satista_Satis, DG_B_yuva_basligi_yalniz_AYNI_rotada_sahibi_birakinca_kalkar | AYNI |
| MDG-16 | `Islemler.Degisim.cs` — Vazgeç değişim girdisini boşaltmaz | DG_T_BASIT_Vazgec_degisim_fisini_bosaltir_istek_ATMAZ | AYNI |
| MDG-17 | `index.html` — css bağı yok | DG_C_css_toplu5ten_SONRA_olcekten_ONCE_baglanir, DG_T_BASIT_Vazgec_degisim_fisini_bosaltir_istek_ATMAZ | AYNI |
| MDG-18 | `FisTakas.razor` — BASİT etiket yine "Gram Ağırlık (g)" | DG_T_BASIT_degisim_VERDIM_ALDIM_fisinde_id_ler_AYNEN_etiketler_sade | AYNI |
| MDG-19 | `FisTakasSatici.razor` — BASİT notta "gider pusulası" jargonu | DG_T_BASIT_kimlik_TEK_kutu_jargon_YOK_MASAK_acikken_satici_kumesi_cercevesiz | AYNI |
| MDG-20 | `Raporlar.GunSonu.cs` — eksi fark adı yok | DG_G_PRO_fatura_satistan_FAZLA_ise_adi_soyler | AYNI |
Son mutasyondan sonra: dosyalar `touch` + temiz `dotnet build Kuyumcu.sln` **0 Uyarı 0 Hata** (yalıtılmış kopya); gerçek ağaçta da `dotnet build Kuyumcu.sln` **0 Uyarı 0 Hata**.

## 7. Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (gerçek ağaç, son kod): 0 Uyarı, 0 Hata. (Ara derlemelerde görülen uyarılar başka dosyalardaydı: SUBEKAPSAMASinirTestleri.cs CS8601, KesifP2Testleri.cs xUnit2029, PatchDortTestleri.cs CS8602, FisKalemGirisi.razor CS0162; bir ara KarZararServisi.cs'te `AltinMi` derleme hatası — MADEN-AYRIMI şeridinin yarım işi, beklenip geçildi.)
- `bash denetim.sh`: **303 ✓, 1 ✗ = P-30 DEMO_INSAN_SURESI** (kabulde hariç). Ara koşumda `OLCEK-1 YUZ_OLCU_ENVANTER_DISI 2` görüldü — bu paketin CSS'i yüz sınıfıyla (`.tuval-tezgah`) dolgu yazıyordu → seçiciler `.islem-sayfa` ile yazıldı, sayaç 0 (`DG_C_sabit_renk_YOK…` kilitler). Sayaçlar: Pages ModalPencere / duzenlenenId artışı yok (yeni modal/düzenleme kalıbı açılmadı).
- `Islemler.razor` 1399 satır (S21 tavanı 1400 korunur) — parçalar `Islemler.Degisim.cs`'te RenderTreeBuilder ile.

## 8. DoD sözleşmesi
Yeni varlık / kayıt türü / uç YOK. Düzenleme yolları aynen: Değişim kaydı (takas hareketi) değişmez finansal kayıttır — düzeltme yolu mevcut storno/iade (DegismezIpucu sayfada aynen); fiş henüz kaydedilmeden **Vazgeç** (yeni, istek atmaz). Gün sonu raporu türetilmiş veridir (düzenleme yok; düzeltme yolu "Fatura kes" / "Faturalara bak" bağlantıları aynen). Her yeni metin `Sozlukler.Degisim.cs` (TR+EN, konumsal record). Yeni CSS yalnız belirteç (sabit renk testi). Mevcut id'ler korundu; yeni id'ler: `#takas-tuval`, `#takas-verdim`, `#takas-verdim-bos`, `#takas-verdim-toplam`, `#takas-verdim-tutar`, `#takas-aldim`, `#takas-aldim-toplam`, `#takas-aldim-has`, `#takas-aldim-tutar`, `#takas-tuval-fark`, `#takas-tuval-fark-tutar`, `#takas-tuval-fark-bekle`, `#takas-tuval-odeme`, `#takas-tuval-odeme-baslik`, `#bugun-fark-satir`, `#bugun-fark-etiket`, `#mut-kartlar`, `#mut-faturasiz-tablo`. Düğmeler: Vazgeç gerçek eylem (dolu fişte görünür, boş fişte çizilmez), kilitli Kaydet sebebini yazar (aynen).

Dokunulan sayfalar/bileşenler (TEK DOKUNUŞ kaydı): `Pages/Islemler.razor` (Değişim bölümü, ödeme yeri, alt satır Vazgeç, başlık — satır sayısı aynen), `Pages/Islemler.Degisim.cs` (yeni), `Islemler.Satis4.cs` (yön değişimi), `Islemler.Diger.cs` (Değişim'e geçişte had), `Pages/Raporlar.razor` (YALNIZ gün sonu kartı + gün sonu sekmesi), `Pages/Raporlar.GunSonu.cs` (yeni), `Bilesenler/Fis/FisTakas.razor`, `FisTakasSatici.razor`, `FisTakasTuval.razor` (yeni), `Layout/UstBar.razor`, `Layout/UstBarSekmeYuvasi.cs`, `Layout/UstBarBasligi.razor` (yeni) — gerekçe § 1 sapmalar.

## 9. Değişen dosyalar
Yeni: `dotnet/Kuyumcu.Core/I18n/Sozlukler.Degisim.cs`, `dotnet/Kuyumcu.Web/Pages/Islemler.Degisim.cs`, `dotnet/Kuyumcu.Web/Pages/Raporlar.GunSonu.cs`, `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisTakasTuval.razor`, `dotnet/Kuyumcu.Web/Layout/UstBarBasligi.razor`, `dotnet/Kuyumcu.Web/wwwroot/css/degisim-gunsonu.css`, `dotnet/Kuyumcu.Tests/DegisimGunSonuTestleri.cs`, bu rapor, `ham/DEGISIM-GUNSONU-olcum/`.
Değişen: `dotnet/Kuyumcu.Web/Pages/Islemler.razor`, `Islemler.Satis4.cs`, `Islemler.Diger.cs`, `Raporlar.razor`, `Bilesenler/Fis/FisTakas.razor`, `Bilesenler/Fis/FisTakasSatici.razor`, `Layout/UstBar.razor`, `Layout/UstBarSekmeYuvasi.cs`, `wwwroot/index.html` (tek bağ satırı); testler (yalnız beklenti): `ParaDemoEkranTestleri.cs`, `ZoBelgeAgHatasiTestleri.cs`, `S34RaporBugunTestleri.cs`, `Toplu5Testleri.cs`, `SATIS3EkranTestleri.cs`.
(`git status --porcelain` bu ağaçta proje dosyalarının çoğunu `??` gösteriyor — ilk commit sonrası izlenmiyor; liste elle ve önce-yedekleriyle tutuldu.) COMMIT yok, dosya silme yok.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Gün sonu: kart tutarı = cümledeki fark, aynı hesap; API = SQL | Sağlandı | § 4 (A/B/C/D dört senaryo) |
| Gün sonu: BASİT "Faturası kesilmemiş satış: X", PRO fark kartı adlandırıldı + tahsilat ayrıldı; fark hesabına dokunulmadı | Sağlandı | § 1 G1/G2, sunucu değişmedi |
| "Havale 0 ₺" gizli, kalem sınırında kırma, dolgu = kart, adet tek kalıp, faturasız tablo yapışık değil | Sağlandı | § 1 G3/G4, `son/` ölçümleri |
| Değişim seçiliyken başlık "Değişim" | Sağlandı | B1, `degisim__*` prob h1 |
| BASİT Değişim Verdim/Aldım fiş kalıbı; MASAK kuralı aynen (yalnız yerleşim) | Sağlandı | T1, kilit metni ve Kaydet kilidi aynen |
| 30.000 ₺ üstü ödeme aracı Satış/Alış'la aynı kaynak; sınır 29.999,99 / 30.000 / 30.000,01 | Sağlandı | N1, saf + bUnit + sunucu + canlı |
| Başsız Chrome 1440/1280/375 × BASİT+PRO × açık+koyu, Read | Sağlandı | § 3 (48 + 48 + 7) |
| Mutasyon ≥1 kırmızı, cmp AYNI | Sağlandı (20/20) | § 6 |
| build 0/0 | Sağlandı | § 7 |
| denetim TEMİZ (P-30 hariç) | Sağlandı | § 7 |
| Tam süit 0 yeni kırmızı | Sağlandı | § 5 (tek kırmızı önceden var, saat dilimi) |
| Test kiracısı silindi, tenants başlangıç değerinde | Sağlandı | § 4 |
| Bellek: en çok 1 API + 1 başsız Chrome | Uyuldu | |

## 11. Açık kalanlar / devir
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — saat dilimi kararsızlığı (test UTC günü veriyor; yerel 00:00–03:00 arası kırmızı). Sahibi: YASAL-4 testleri; öneri: `Zaman.YerelBugun()` ile sorgu.
- Değişim fişi yazdırma ("Kaydet ve yazdır"): takas kaydı yazdırılabilir belge döndürmüyor → çıktı/sunucu paketi.
- "Farkın ödemesi" içindeki "Ödeme Türü" etiketi FisOdemeKasa'nın ortak (title-case) etiketi — ortak bileşen sözlüğü işi (Satış/Alış'ta da aynı).
- PRO Değişim aynen bırakıldı (iki kimlik kutusu PRO'da yasal atıflı başlıklarla sürüyor) — PRO yerleşimi istenirse ayrı paket.
- TOPLU-5 § 2'nin kalan devirleri (bölünmüş ödeme ölçümü, Raporlar PRO 1280 seçici, müşteri detayı yüzen pencere, Günü Kapat sayılan nakit) aynen.

## 12. Kurucu / mali müşavir
Yeni soru yok. Mevcut açık soru aynen: takasta 459 s. VUK GT işlem tutarı kuralının (fişin toplamı) uygulanıp uygulanmayacağı — bugün yalnız el değiştiren NAKİT FARK denetlenir (SatisModulu notu); bu paket bu davranışı değiştirmedi.

## Tur 2 (03.10.2026 — kayıt PAKET DEGISIM-2 şeridinde tamamlandı)

Tur 2 kodu (tasarım hakemi t1 KRİTİK + doğrulayıcı DÜŞÜK bulguları) ağaçta duruyordu; rapor bölümü ve mutasyon kayıtları eksik kalmıştı (MDG-25/26 ilk koşumda ağaçtan sapmış kopyada koşulduğu için `cmp FARKLI`). Bu bölüm o kodu ve yeniden koşulan mutasyonları kaydeder. **Yasal kural, 30.000 sınırı, sunucu kodu DEĞİŞMEDİ; migration yok.**

### Tur 2 kod değişiklikleri
| Konu | Dosya | Ne yapıldı |
|---|---|---|
| Peşin ön dolum | `Pages/Islemler.Degisim.cs` (`SaticiOnDolumAdi`, `TakasSaticiOnDolumEsitle`) | "Peşin Müşteri" bir kişi değil sistem carisi → satıcı adına ÖN DOLMAZ (kutu boş, zorunlu). Gerçek müşteri seçilince onun adı dolar; müşteri değişince cariden gelmiş ad yeni kaynağa uyar, elle yazılan ad ezilmez. |
| Tek adım kilit | `Pages/Islemler.ParaDemo3.cs` (`TakasBirlesikKilit`) | Peşin + kimlik eşiği üstü (`PesinEsikUstu`, sunucunun Peşin kapısının aynası) → kimlik kutusu ekranda yok; kilit alan saymaz, tek adım söyler: "Önce müşteri seçin." Kaydet yine kilitli (kural aynen). |
| 1280 ALDIM 2+1 | `wwwroot/css/degisim-gunsonu.css` | ALDIM: Ayar · Gram üstte, Has fiyatı tam satır (bilinçli 2+1, her genişlikte); VERDİM Adet · Fiyat iki eşit sütun; birim eki kutunun sağında (nowrap). VERDİM arama kutusu sütunu tam kaplar (`.barkod-satiri { margin-right: 0 }`). |
| TakasTevsikTutari | `Pages/Islemler.Degisim.cs` | Nakit sınırı karşılaştırmasına giden tutar sunucununkiyle aynı: `|Yuvarla2(fark)|` (AwayFromZero). Kuruş altı pencerede (had + 0,004) istemci artık sunucudan farklı karar vermez. |
| TakasOdemeVarsayilani üretimde | `Pages/Islemler.Degisim.cs` | Yardımcı artık ÜRETİM yolunda (NakitSiniriAsildi TAKAS dalı çağırır): had yok/0 → null (eşik uydurulmaz); `TevsikOneri.Gerekli("NAKIT","SATIS", TakasTevsikTutari(f), had)` → müşteri öderse KART, biz ödersek HAVALE. |
| FarkGosterimi | `Pages/Raporlar.GunSonu.cs` + `Sozlukler.Degisim.cs` | "Satıştan fazla kesilen fatura" türünde tutar işaretsiz (ad yönü söyler; çift olumsuz yok); açıklama tarihsiz ("Kesilen faturalar günün satışından fazla." — PRO'da geçmiş gün seçilebilir). |

### Tur 2 mutasyonları (YENİDEN koşum, 03.10 03:16–03:30)
Yöntem: gerçek ağacın birebir kopyası `/private/tmp/kuyumcu-serit/DEGISIM-2/kok` (+ karşılaştırma için ikinci anlık görüntü `ana`, ikisi aynı anda `rsync`; `cmp` ağaçla eşit) → yedek → TEK eşleşmeli değişiklik → derle → filtreli test (`Kuyumcu.Tests.Degisim|DEGISIMGUNSONU|ZB34|S34_1|PDE1_|T5_BASIT_faturasiz|KesifP5`, 191 test) → yedeği geri koy → `cmp` hem yedekle hem anlık görüntüyle. Betik `mut3.py`, sonuç `ham/DEGISIM-2-olcum/mut-tur2-yeniden.json`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| MDG-21 | `Islemler.Degisim.cs` — Peşin Müşteri adı satıcıya ön dolar | DG_K_Pesin_Musteri_iken_satici_adi_ON_DOLMAZ_bos_ve_zorunlu (2 mod), DG_K_gercek_musteri_adi_on_dolar_Pesine_donulunce_BOSALIR_elle_yazilan_EZILMEZ | AYNI |
| MDG-22 | `Islemler.Degisim.cs` — müşteri değişince cariden gelen ad güncellenmez | DG_K_gercek_musteri_adi_on_dolar_Pesine_donulunce_BOSALIR_elle_yazilan_EZILMEZ | AYNI |
| MDG-23 | `Islemler.ParaDemo3.cs` — Peşin iken kilit yine görünmeyen alanları sayar | DG_K_Pesin_ve_kimlik_esigi_ustu_kilit_TEK_adim_alan_SAYMAZ (2 mod) | AYNI |
| MDG-24 | `Islemler.ParaDemo3.cs` — `MasakKutusuAcik` ↔ `MasakGerekli` | — (EŞDEĞER MUTANT, test açığı değil) | — |
| MDG-25 | `Islemler.Degisim.cs` — kuruş altı: istemci yuvarlamadan karşılaştırır | DG_takas_tevsik_tutari_SUNUCU_gibi_kurusa_yuvarlanir_AwayFromZero, DG_takas_uretim_yolu_sinir_karari, DGS_kurus_alti_pencere_istemci_sunucudan_SIKI_belgelenir, DGS2_takas_tevsik_tutari_kurus (12 koşum) | **AYNI** (ilk koşumdaki FARKLI kopyanın ağaçtan sapmasıydı) |
| MDG-26 | `Islemler.Degisim.cs` — takas varsayılanı hiç tetiklenmez | DG_N_BASIT_degisim_farki_nakit_siniri_satisla_ayni_kaynak, DG_N_ELLE_secilen_arac_fark_yonu_degisse_de_EZILMEZ, DG_N_PRO_degisimde_biz_odersek_HAVALE_otomatik, DG_N_fark_YONU_degisince_otomatik_arac_da_degisir_KART_HAVALE, DG_N_had_Satista_ALINAMAMISSA_Degisime_gecince_sunucudan_alinir_KART, DG_takas_sinir_karari_satisla_AYNI, DG_takas_uretim_yolu_sinir_karari, DGS_sunucunun_reddettigi_farkta_istemci_NAKIT_birakmaz, DGS_takas_varsayilan_sinir, DGS2_had_bir_kurus, DGS2_spesifikasyon_sinirlari (20 koşum) | **AYNI** |
| MDG-27 | `Raporlar.GunSonu.cs` — fazla faturada eksi işaret | DG_G_BASIT_fazla_fatura_tutari_ISARETSIZ_aciklama_TARIHSIZ, DG_G_PRO_fatura_satistan_FAZLA_ise_adi_soyler, DGS2_fark_gosterimi (5 koşum) | AYNI |
| MDG-28 | `Sozlukler.Degisim.cs` — açıklama geçmiş günde de "Bugün" | DG_G_BASIT_fazla_fatura_tutari_ISARETSIZ_aciklama_TARIHSIZ | AYNI |
| MDG-29 | `degisim-gunsonu.css` — ALDIM 2+1 düzeni yok | DG_C_sabit_renk_YOK_ve_olculen_kurallar_yerinde | AYNI |
| MDG-30 | `degisim-gunsonu.css` — arama kutusu VERDİM sütununu tam kaplamaz | DG_C_sabit_renk_YOK_ve_olculen_kurallar_yerinde | AYNI |

MDG-24 gerekçesi (md.1, gizlenmez): `TakasBirlesikKilit` içinde `PesinEsikUstu` erken dönüşü ve `masakGereksinim is { } g` koşulu, `MasakKutusuAcik = MasakGerekli && !PesinEsikUstu && masakGereksinim is not null` farkının tamamını zaten kapsar → iki ifade bu noktada her girdide aynı sonucu verir (eşdeğer mutant; hiçbir test onu ayıramaz, ayırması gerekmez). Ağaçta bugün `MasakGerekli` yazılıdır; tur 2'nin kopyasında `MasakKutusuAcik` + yorum satırı vardı (ilk koşumdaki ağaç↔kopya sapmasının biri buydu). Davranış farkı yok; ağaç olduğu gibi bırakıldı. Erken dönüş kuralı MDG-23 ile kilitlidir.
