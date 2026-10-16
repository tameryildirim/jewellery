# PAKET PARA-DEMO-3 — uygulama raporu

**Konu:** PARA-DEMO-2 kapanışında tasarım hakeminin bıraktığı kalemler:
- kasa mesajında para biçimi (KRİTİK),
- takas kilidinin sebebi (YÜKSEK),
- demo açılış devrinin "Gelir" yazılması (YÜKSEK),
- gün sonu "Karışık" ile ekstre tutarlılığı (ORTA),
- Gider ekranındaki "Kasa bakiyesi" (ORTA).

| Bilgi | Değer |
|---|---|
| Rol | Taşkın (backend) + Ertuğ (frontend), Opus |
| Şerit | t2: API 5162, vekil 5163, CDP 9910, test DB `kuyumcu_test_t2` |
| Tarih | 02.10.2026 |
| Tur | 2 (tur 1 + doğrulayıcı ve tasarım hakemi bulgularının kapanışı, § 0) |
| Durum | **BİTTİ (tur 2).** 7 kapı maddesi tur 2 kodu üzerinde yeniden sağlandı (§ 0.6). Tam süit 9517 test, **0 başarısız** (3 atlanan). |

Kaynak: `PAKET-PARA-DEMO-2-uygulama-raporu.md`. Bu raporda "Birleşik koşullar" başlıklı bir bölüm yok. Kapsamı görev metnindeki 5 bulgu ve NOT belirledi. Spesifikasyonda bir mevzuat hükmü DOĞRULANMALI olarak işaretlenmemişti ve bu pakette yasal eşik ya da oran yazılmadı.

Kanıtlar `ham/PARA-DEMO-3-olcum/` altında:
- 61 fotoğraf,
- `olcum.jsonl` (60 ölçüm),
- `canli.txt`, `sql-acilis.txt`, `mutasyon-sonuc.txt`, `temizle.txt`,
- betikler (`olc.mjs`, `canli.py`).

## 0. TUR 2 — önceki doğrulamada kalan bulgular (02.10, ~19:40–20:25)

Bu bölüm tur 2'nin tamamıdır; § 1–13 tur 1 kaydıdır (tur 2'de değişen açık kalanlar § 0.8'de güncellendi).
Tur 2 kanıtları: `ham/PARA-DEMO-3-olcum/tur2/` (84 ölçüm `olcum2.jsonl`, 84 fotoğraf `foto/`, `canli2.txt`, `canli2b.txt`,
`temizle2.txt`, `denetim-t2.txt`, `ozet2.txt`, betikler) · mutasyon sonuçları `ham/PARA-DEMO-3-olcum/mutasyon-sonuc-tur2.txt`,
yedekler `ham/PARA-DEMO-3-olcum/mut-yedek/tur2/`.

### 0.1 Bulgu bulgu

| # | Bulgu (önem) | Yapılan | Kanıt | Durum |
|---|---|---|---|---|
| 1 | Gün sonu ↔ ekstre ödeme adı eski KARMA ve İngilizcede farklı (ORTA) | **Tek ad üreticisi:** `ParaDemo3Sozlugu.OdemeAdi` artık adları ekstrenin sözlüğünden (`ParaDemoSozlugu.OdemeAdi`) üretir; bilinmeyen kodda katalog davranışı aynen. `Cariler.razor` `EkstreOdemeAdi` karma dalı **aynı yöntemi** çağırır. Sonuç: eski KARMA her iki ekranda "Karma ödeme" / "Mixed payment"; gerçek karma "Kart + Havale" / "Card + Transfer"; HAVALE "Havale"/"Transfer" | `PD3T2_gun_sonu_ve_ekstre_odeme_adi_birebir_ayni` (tr, en): **iki ekran render edilir** (Cariler ekstre + Raporlar gün sonu), 5 ödeme biçimi (bacaklı karma, bacaksız ESKİ karma, havale, nakit, kart) birebir karşılaştırılır + elle yazılmış sabit beklenen. Tarayıcı: PRO gün sonu "Kart + Havale", ekstre "· Kart + Havale" (12/12) | KAPANDI |
| 2 | Gider istemci kilidi toplam bakiyeye bakıyor (DÜŞÜK) | `GiderKuralKilidi`: nakit ayrımı biliniyorsa **önce** `giderTutar > SecilenAyrim.Nakit` → "Çekmecedeki nakit yetmiyor: X var" (toplamı aşan tutarda da). Erken uyarı bandı da nakit cümlesiyle. Ayrım okunamazsa eski toplam kilidi | bUnit `PD3T2_gider_kilidi_cekmecedeki_nakde_bagli` (BASİT, PRO: 150.000 açık · 200.000 kilitli); canlı: aynı tutar (194.415,54) sunucuda da `KASA_NAKIT_YETERSIZ` (istemci ile sunucu aynı hüküm); tarayıcı `gider-200` 12/12 `kilitli=true` | KAPANDI |
| 3 | Elle NAKİT çıkışta toplam cümlesi "kasada X var" (kart/havale dahil) diyor (DÜŞÜK) | İstemci: `KasaBakiyeMesaji.Bicimle(mesaj, dil, nakitAyrimi)` — kasada kart/havale payı varsa toplam reddi **nakit cümlesiyle** yazılır (çekmecedeki nakit + kart + havale ayrı). Kasalar elle hareket, Gider bandı ve virman bu yolu kullanır. Sunucu kapı sırası değiştirilmedi (§ 0.2) | bUnit `PD3T2_elle_nakit_cikis_…` (tr, en), saf `PD3T2_toplam_reddi_kart_payi_varsa_…`; tarayıcı `kasa-gider-red` 12/12 bant "çekmecede 193.415,54 ₺ nakit var, 2.000.000 ₺ nakit isteniyor. Kasadaki kart (25.661,43 ₺) ve havale (2.000 ₺) …", `toplamVarDiyor=false` | KAPANDI |
| 4 | Araç payı reddi istemcide çevrilmiyor; "harf"/"karakter" tutarsız (DÜŞÜK) | `KasaBakiyeMesaji` araç payı kalıbı (`KASA_ARAC_PAYI_YETERSIZ` cümlesi) → sözlük `AracPayiYetersiz` TR+EN. Terim: `AlanBeyanAciklama` "en az {0} karakter" / "at least {0} characters" (MasakFormu ipucuyla aynı) | HTTP `PD3T2_arac_payi_reddi_istemcide_TR_EN_cevrilir` (gerçek sunucu cümlesi); `PD3_sozluk_…` ipucu ile kilit terimi karşılaştırılır; tarayıcı takas kilidi 12/12 `karakter=true` | KAPANDI |
| 5 | 5199 dinlemiyor (ortam, paket dışı) | Dokunulmadı (kural: 5199 kurucunun). Orkestratöre | — | PAKET DIŞI |
| 6 | Virman reddi ekran dışında, sessiz (KRİTİK, tasarım hakemi) | Ret artık `#gd-v-hata` (HataBandi) olarak **"Virmanı kaydet"in hemen altında**, formun içinde; üstteki `#gd-hata`'ya yazılmaz. Başarı sonucu da düğmenin altında `#gd-v-sonuc` (UX-S27) | bUnit `PD3T2_virman_reddi_dugmenin_altinda_formun_icinde` (tr, en: `#gd-hata` yok, DOM sırası düğme < bant); tarayıcı `virman-red` 12/12 `gorunurAlanda=true` (1280: y=503–567/800, 375: y=527–648/812, 1440: 603–667/900), üst bant yok | KAPANDI |
| 7 | Gider bandı bağlantıları metinden ayırt edilmiyor (KRİTİK, tasarım hakemi) | Metin `<p id="gd-yetersiz-metin">`; altında ayrı satır `#gd-yetersiz-eylemler` (`satir mt-1`), iki eylem `dugme dugme-ikincil dugme-kucuk` (kimlikler aynı, yeni CSS yok) | bUnit `PD3T2_yetersiz_bandi_eylemleri_ayri_satirda_ikincil_kucuk_dugme`; tarayıcı `gider-200`/`gider-asim` 24/24: 2 düğme, **silik 0**, kırpık 0, kenar kontrastı 3,17–3,18, alt çizgi yok, ayrı satır (y düğme > metin) | KAPANDI |

Ek (ölçümün yakaladığı, tur 2'de düzeltildi): ekstre 375 px'te "· Kart + Havale" iki satıra kırılıyordu (K3 kırık 1, BASİT/PRO açık/koyu 4/4). `Cariler.razor` ödeme `span`'ına mevcut `hucre-tek-satir` sınıfı eklendi → son ölçümde K3 kırık **0** (12/12).

### 0.2 Sapmalar (tur 2)
- **Sunucu kapı sırası değişmedi (bulgu 3).** Seçenek "önce nakit reddi" idi; `NakitDogrula` sırasını değiştirmek hurda/alış/iade/elle/gider yollarının hepsinde `KASA_BAKIYE_YETERSIZ` bekleyen başka şeritlerin testlerini kırar. Bulgunun ikinci seçeneği istemcide uygulandı: ekran nakit ayrımını biliyor ve kasada kart/havale payı varsa toplam reddi nakit cümlesine çevrilir (nakit çıkışta tutar toplamı aşıyorsa nakdi de aşar — söylenen sebep doğru). Ayrım yoksa ya da kasa yalnız nakitse eski cümle aynen.
- **Gider `YetersizBakiyeReddi`** artık `KASA_NAKIT_YETERSIZ` kodunu da kabul eder: bayat veride nakit reddi de üstteki banda değil formdaki banda (eylem düğmeleriyle) düşer.
- **Ek dokunuş:** `Cariler.razor` (ekstre karma adı tek üreticiye + `hucre-tek-satir`) — dosya alanındaki "ekstre ödeme türü etiketi"; PD2'de açılmıştı, gerekçe aynı bulgu zinciri.

### 0.3 Derleme ve testler (tur 2)
- `dotnet build Kuyumcu.sln --no-incremental`: **0 hata**, 2 uyarı (başka şeridin: `SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029). Kendi dosyalarımda 0 (ara derlemede kendi testimde çıkan CS8631 düzeltildi).
- Yeni testler (10 yöntem, 15 koşum): `PD3T2_arac_payi_reddi_istemcide_TR_EN_cevrilir`, `PD3T2_toplam_reddi_kart_payi_varsa_nakit_cumlesiyle_yalniz_nakitte_aynen`, `PD3T2_elle_nakit_cikis_toplam_reddi_kart_payi_varsa_cekmecedeki_nakdi_soyler` (tr, en), `PD3T2_gider_kilidi_cekmecedeki_nakde_bagli` (BASİT, PRO), `PD3T2_yetersiz_bandi_eylemleri_ayri_satirda_ikincil_kucuk_dugme`, `PD3T2_virman_reddi_dugmenin_altinda_formun_icinde` (tr, en), `PD3T2_gun_sonu_ve_ekstre_odeme_adi_birebir_ayni` (tr, en). Güncellenen: `PD3_G_…` (EN beklenen artık sabit "Card + Transfer", eski KARMA ekstre adı), `PD3_sozluk_…` (terim).
- Filtreli: ParaDemo3+PARADEMO3 35/35 (ilk koşum); geniş filtre (ParaDemo, PARADEMO, Toplu1, TOPLU1, Gider, Kasa, Virman, P19, BelgeKalan, Cari, Rapor, Takas, Masak, SATIS3) **2137/2139, 0 başarısız** (2 atlanan başkalarının Skip'i).
- **Tam süit (son kod):** `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-3-tur2/_Mac_2026-10-02_20_13_24.trx` → **9517 toplam · 9514 geçti · 0 başarısız · 3 atlandı** (tur 1'de 9395). Yeni kırmızı 0; tur 1'deki 3 PATCH-SOZLESME kırmızısı da artık yeşil.

### 0.4 Mutasyon tablosu (tur 2)
Yöntem: yedek (`mut-yedek/tur2/`) → tek eşleşmeli değişiklik (eşleşme sayısı 1 doğrulanır) → filtreli test → yedeği geri kopyala → `cmp`. Son mutasyondan sonra `--no-incremental` temiz derleme (0 hata) ve tam süit.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M5-1 | `ParaDemo3Sozlugu.OdemeAdi`: ekstre sözlüğü yerine katalog | `PD3T2_gun_sonu_ve_ekstre_odeme_adi_birebir_ayni` (tr, en), `PD3_G_gun_sonu_faturasiz_KARMA_…` | AYNI |
| M5-2 | `Cariler.razor` karma dalı: tek üretici yerine katalog adlarıyla birleştirme | `PD3T2_gun_sonu_ve_ekstre_odeme_adi_birebir_ayni` (tr, en) | AYNI |
| M6-1 | Gider nakit kilidi `> Nakit` → `> Toplam` | `PD3T2_gider_kilidi_cekmecedeki_nakde_bagli` (BASİT, PRO) | AYNI |
| M6-2 | Erken uyarı bandının nakit dalı devre dışı | `PD3T2_gider_kilidi_cekmecedeki_nakde_bagli` (BASİT, PRO) | AYNI |
| M6-3 | Nakit kilidi yalnız toplamın altında (eski sıra: toplamı aşınca "kasada X var") | `PD3T2_yetersiz_bandi_eylemleri_ayri_satirda_ikincil_kucuk_dugme` | AYNI |
| M7-1 | "Kasaya giriş yap" düğme sınıfları kaldırıldı | `PD3T2_yetersiz_bandi_eylemleri_ayri_satirda_ikincil_kucuk_dugme` | AYNI |
| M8-1 | Virman reddi yine üstteki `hata`'ya | `PD3T2_virman_reddi_dugmenin_altinda_formun_icinde` (tr, en) | AYNI |
| M9-1 | Araç payı kalıbı devre dışı | `PD3T2_arac_payi_reddi_istemcide_TR_EN_cevrilir` | AYNI |
| M9-2 | Nakit ayrımlı biçimleyici ayrımı yok sayar | `PD3T2_elle_nakit_cikis_…` (tr, en), `PD3T2_toplam_reddi_…`, `PD3T2_virman_reddi_…` (tr, en) (+ aynı filtredeki diğer PD3T2) | AYNI |
| M10-1 | Kasalar catch'i ayrımı geçirmez (`&& false`) | `PD3T2_elle_nakit_cikis_toplam_reddi_kart_payi_varsa_cekmecedeki_nakdi_soyler` (tr, en) | AYNI |
| M11-1 | Terim "karakter" → "harf" | `PD3_sozluk_TR_ve_EN_dolu_ve_BASIT_metinlerde_MASAK_yok` | AYNI |

Not: M10-1'in ilk biçimi (`null is { } hk`) derlenmedi (CS8117); mutant `&& false` olarak yeniden koşuldu — iki satır da `mutasyon-sonuc-tur2.txt`'te. 12/12 kırmızı (geçerli 11 mutant + M6-3).

### 0.5 denetim.sh (tur 2)
`bash denetim.sh` → tek ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç). Diğer tüm sayaçlar ✓ (`tur2/denetim-t2.txt`). Pages/ altında yeni ModalPencere düzenleme kullanımı yok (yeni bileşen: HataBandi/BasariBandi).

### 0.6 Canlı doğrulama (5162; kiracı `PAKETPARA-DEMO-3-TEST2 Kuyumculuk` = `01M3YRG19JCGQAKGZKP8YE47VF`, değiştirilmiş `demo-tohumla.js --prova-kiraci` ile; yanıtlar okundu)

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET `/kasalar/nakit-ayrimi` (Ana) ↔ SQL | nakit = bakiye − kart − havale | API `toplam 221076.97 · nakit 193415.54 · kart 25661.43 · havale 2000`; SQL `221076.97 · 25661.43 · 2000.00` → 221.076,97 − 25.661,43 − 2.000 = 193.415,54 ✓ |
| POST elle CIKIS NAKIT 2.000.000 | 400 toplam kodu, tr-TR | 400 `KASA_BAKIYE_YETERSIZ` "… 216.076,97 ₺ var, 2.000.000,00 ₺ isteniyor …", `TRY` yok (ekran bunu nakit cümlesine çevirir, § 0.7) |
| POST elle CIKIS KART kart payı+1 (22.662,43) | 400 araç payı kodu | 400 `KASA_ARAC_PAYI_YETERSIZ` "'Ana Kasa' kasasındaki kart tahsilatı 22.661,43 ₺; 22.662,43 ₺ kartla çıkarılamaz." (istemci kalıbının girdisi) |
| POST `/giderler` 194.415,54 (nakit < tutar < toplam) | 400 nakit kodu (istemci kilidiyle aynı hüküm) | 400 `KASA_NAKIT_YETERSIZ` "çekmecede 193.415,54 ₺ nakit var, 194.415,54 ₺ nakit isteniyor …" |
| POST `/virman` Ana → Vitrin Yanı Yedek 2.000.000 | 400 | 400 `KASA_BAKIYE_YETERSIZ` "… 221.076,97 ₺ var, 2.000.000,00 ₺ …", `TRY` yok |
| POST `/virman` 194.415,54 nakit | 400 nakit kodu | 400 `KASA_NAKIT_YETERSIZ` |
| SQL `kasa_hareketleri` (Ana) önce/sonra | yazma yok | 9/9 (ilk set) · 11/11 (karma fiş sonrası set) |
| Karma fiş KART 3000 + HAVALE 2000 | 200 + 2 bacak | 200; SQL `odeme_kalemleri` `KART 3000.00 0 · HAVALE 2000.00 1`; ekstre `["SATIS","KARMA",["KART","HAVALE"]]`; gün sonu `["KARMA",["KART","HAVALE"]]` |

**Temizlik:** API/vekil/Chrome `kill`; 5162, 5163, 9910 için `lsof` boş. `fikstur.json` ve tek kullanımlık sistem anahtarı `printf ''` ile boşaltıldı (değer basılmadı). `zsh kiraci-temizle.sh 01M3YRG19JCGQAKGZKP8YE47VF --uygula` → 525 satır + tenants 1 satır; sorgu `id=… or ad like 'PAKETPARA-DEMO-3%'` = **0**. `tenants` sayımı başta 23, sonda 15: fark aynı sürede başka şeritlerin kendi kiracılarını silmesi; benim açtığım tek kiracı silindi, QA-TEST-* kiracılarına dokunulmadı.

### 0.7 Gerçek tarayıcı (başsız Chrome CDP 9910, vekil 5163, `sikisma-olc.js` + dugme-olc kontrast ölçüsü)
7 ekran × 1440/1280/375 × BASİT/PRO × açık/koyu = **84 ölçüm** (son kodla; ara ölçüm `olcum2-ara.jsonl`'da). Ekranlar: `kasa-gider-red` (Kasalar › Ana Kasa › + Hareket › Gider › 2.000.000 › Kaydet), `gider-200` (tutar = nakit + 1.000), `gider-asim` (2.000.000), `virman-red` (Ana → Vitrin Yanı Yedek, 2.000.000, Kaydet), `gunsonu`, `ekstre` (karma fişli cari), `takas-kilit` (Değişim, satıcı Vatandaş).

| Ekran (12 kombinasyon) | K1 | K3 taşan/kırık | K5 TRY | yatay | düğme silik | Ek ölçüt |
|---|---|---|---|---|---|---|
| kasa-gider-red | 0 | 0/0 | 0 | 0 | — | bant nakit cümlesi 12/12, `toplamVarDiyor=false` |
| gider-200 | 0 | 0/0 | 0 | 0 | 0 (2 düğme) | `kilitli=true` 12/12, kilit "Çekmecedeki nakit yetmiyor: 193.415,54 ₺ var" |
| gider-asim | 0 | 0/0 | 0 | 0 | 0 (2 düğme) | kilit yine nakit cümlesi; eylemler ayrı satır |
| virman-red | 0 | 0/0 | 0 | 0 | — | `gorunurAlanda=true` 12/12, üst bant yok |
| gunsonu | 0 | 0/0 | 0 | 0 | — | PRO faturasız "Kart + Havale", `Karışık` yok (BASİT tabloyu göstermez) |
| ekstre | 0 | 0/**0** | 0 | 0 | — | "· Kart + Havale" (375 kırığı düzeltildi) |
| takas-kilit | 0 | 0/0 | 0 | 0 | — | `karakter=true` 12/12; BASİT `MASAK=false`, PRO gerekçede MASAK (tasarım gereği) |

K9 (binen): `kasa-gider-red` 5–6, `ekstre` 6 — çekmece/detay penceresi alttaki sayfanın üstünde (örnek: `#cari-segment-hepsi ⟂ p.stat-etiket`, `#kasa-yeni ⟂ Tutar`); PD2'den beri bilinen ölçer yanlış pozitifi, fotoğraflarda yazı binmesi yok. Diğer 5 ekranda K9 = 0.

Read ile görülen fotoğraflar (`tur2/foto/`): `gider-200__Basit_aydinlik_1280` (nakit kilidi + ayrı satırda iki ikincil düğme), `gider-asim__Profesyonel_karanlik_375` (koyu temada düğmeler zeminden ayrışıyor, kilit nakit cümlesi), `virman-red__Basit_aydinlik_1280` ve `virman-red__Basit_aydinlik_375` (ret "Virmanı kaydet"in hemen altında, görünür alanda), `kasa-gider-red__Profesyonel_karanlik_1280` (çekmecede nakit cümlesi, TRY yok), `ekstre__Profesyonel_aydinlik_375` ("Satış · Kart + Havale" tek satırda).

### 0.8 Açık kalanlar — tur 2 güncellemesi
- § 12 madde 4 (Gider kilidi toplam bakiye) **KAPANDI** (§ 0.1 #2). Madde 5 (araç payı çevirisi) **KAPANDI** (#4). Madde 8 (PATCH kırmızıları) tur 2 tam süitinde yeşil.
- Hâlâ açık: § 12 madde 1 (ret gövdesine ayrı tutar alanları — ortak hub ApiException/ApiIstemci), 2 (gün kapanışı `{N2} TL`), 3 (denetim izi invariant metni), 6 (TOPLU-1 Skip), 7 (ölçer K9 yanlış pozitif).
- Yeni: `EFT` kodu ekstre sözlüğünde yok; tek üretici bilinmeyen kodda katalog adına ("Havale") düşer, ekstre ise null döndürür (ekstrede EFT satırı üretilmiyor, gün sonunda da yalnız `HAVALE` görülüyor — ölçümde karşılaşılmadı). Sahibi: PARA-DEMO sözlük bakımı.
- 5199 dinlemiyor (doğrulayıcı madde 5): ortam, paket dışı — orkestratöre.

### 0.9 Değişen dosyalar (tur 2)
`dotnet/Kuyumcu.Core/I18n/Sozlukler.ParaDemo3.cs` (4 yeni anahtar TR+EN, terim, tek ad üreticisi) · `dotnet/Kuyumcu.Web/Servisler/KasaBakiyeMesaji.cs` (araç payı kalıbı, nakit ayrımlı aşırı yükleme, `NakitCumlesi`) · `dotnet/Kuyumcu.Web/Pages/Gider.razor` (bant eylemleri, nakit kilidi/erken uyarı, nakit kodu, virman reddi/sonucu formda) · `dotnet/Kuyumcu.Web/Pages/Kasalar.razor` (catch'e ayrım) · `dotnet/Kuyumcu.Web/Pages/Cariler.razor` (karma adı tek üretici, `hucre-tek-satir`) · `dotnet/Kuyumcu.Web/Pages/Islemler.ParaDemo3.cs` (yorum) · `dotnet/Kuyumcu.Tests/ParaDemo3Testleri.cs`. Migration yok. Sunucu dosyası değişmedi. COMMIT yok, dosya silme yok.


## 1. Bulgu bulgu — önce ve sonra

| # | Bulgu | Önce | Sonra (kanıt) | Durum |
|---|---|---|---|---|
| K | **[KRİTİK]** Kasalar › + Hareket › Gider › 2.000.000 | Çekmecede: "312547.91 TRY var, 2000000.00 TRY isteniyor" | **Sunucu:** `KasaBakiyeKapisi.Mesaj` artık tr-TR ve ₺ işaretiyle yazıyor (nakit kapısıyla aynı `ParaMetni`). Ret gövdesine makine kodu eklendi: `kod=KASA_BAKIYE_YETERSIZ`; nakit için `KASA_NAKIT_YETERSIZ`, araç payı için `KASA_ARAC_PAYI_YETERSIZ`. **İstemci:** `KasaBakiyeMesaji` iki biçimi de okur (yeni tr-TR ve geçiş için eski invariant). Nakit kapısı cümlesini de sözlükten TR+EN olarak yeniden yazar. **Ekranlar:** Kasalar.razor `HareketGir`, Gider (gider ve virman catch'leri) ve Gider `YetersizMetni` bu tek ayrıştırıcıdan geçer. **Canlı:** 2.000.000 → 400. Gövde "216.395,57 ₺ var, 2.000.000,00 ₺ isteniyor", `TRY` yok. Çekmecede "'Ana Kasa' kasasında yeterli para yok: kasada 221.395,57 ₺ var, 2.000.000 ₺ isteniyor." **Ölçüm:** 12 kombinasyonda K5=0 ve `TRY=false` | KAPANDI |
| T | **[YÜKSEK]** Değişim kimlik kilidi | BASİT'te "Eksik: TCKN, adres." yazıyordu, işlem amacı ve beyan listede yoktu. TCKN ve adres girilince "MASAK eşiği aşıldı…" jargonuna dönüyordu | Yeni `Islemler.ParaDemo3.cs` → `TakasBirlesikKilit`. Satıcının gider pusulası eksikleri (`HurdaGirisi` kuralları) ile kimlik tespiti eksikleri (`MasakFormu.Eksiksiz` ile birebir aynı koşullar) **tek listede**, tekrarsız birleşir. BASİT: "Eksik: TCKN, adres, işlem amacı, beyan açıklaması (en az 20 harf)." → TCKN ve adres girilince "Eksik: işlem amacı, beyan açıklaması (en az 20 harf)." (MASAK kelimesi 0). PRO: "Eksik bilgi: … — gider pusulası için gerekli (VUK md.234); müşteri kimlik tespiti için gerekli (MASAK)." **Ölçüm:** 12 kombinasyon × 2 adım | KAPANDI |
| A | **[YÜKSEK, kapsam dışı sahipli]** Demo açılış devri "Gelir" yazılıyordu | `kasa_hareketleri` 150.000 NAKIT, `hareket_id NULL`. BASİT Tahsilat 312.547,91 ₺ | `demo-tohumla.js` **yalnız açılış bölümü** değişti: açılış artık `/kurulum/acilis/prova → /uygula` (SAHİP) ile yazılıyor. **Canlı tohum:** SQL'de açılış satırı `DIGER 150000.00 · bağlı · ACILIS`. Elle "Açılış devri" satırı **0**. API tahsilat toplamı 66.395,57, açılış devri 150.000. SQL'de girişler − ACILIS bacağı = **66.395,57** (API=SQL). Ekranda BASİT "Tahsilat 71.395,57 ₺" (sonradan eklenen 5.000'lik karma fiş dahil). PRO'da "Açılış devri (tahsilata girmez) 150.000 ₺" ayrı satırda | KAPANDI |
| G | **[ORTA]** Gün sonu "Karışık", ekstre "Kart + Havale" | Aynı fiş iki ekranda iki farklı adla görünüyordu | `RaporModulu` gün sonu `faturasizIslemler[]` satırlarına `odemeAraclari` alanı eklendi. Kaynak `odeme_kalemleri`, CariModulu ekstresiyle aynı sorgu. İstemcide tek ad üretici var: `ParaDemo3Sozlugu.OdemeAdi`. **Canlı:** karma fiş (KART 3000 + HAVALE 2000) ekstrede `["KART","HAVALE"]`, gün sonunda `["KART","HAVALE"]`, SQL `odeme_kalemleri` KART 3000 / HAVALE 2000. PRO tabloda "Kart + Havale", `karisik=false` (12 ölçüm) | KAPANDI |
| N | **[ORTA]** Gider "Kasa bakiyesi" toplamı gösteriyordu | "Kasa bakiyesi: 377.738,62 ₺" | Önce `#gd-kasa-nakit` "Çekmecedeki nakit: 193.619,08 ₺" (yeşil rozet), ikinci sırada `#gd-kasa-bakiye` "Kasa toplamı (kart ve havale dahil): 221.395,57 ₺". Kaynak `/kasalar/nakit-ayrimi` (Kasalar ve Panel ile aynı uç). Ayrım okunamazsa nakit satırı **çizilmez**: eski "Kasa bakiyesi" etiketi ve kısa hata gösterilir. **Canlı:** API nakit 193.619,08. SQL'de bakiye 221.395,57 − kart 25.776,49 − havale 2.000 = 193.619,08 | KAPANDI |

### Kasa kapısı mesajları tarama tablosu (NOT'un istediği)

| Mesaj (sunucu) | Kaynak | Önce | Sonra | Ekrana giden yol |
|---|---|---|---|---|
| Toplam kapısı "bakiyesi yetersiz" | `KasaBakiyeKapisi.Mesaj`. İki aşırı yüklemesi var: kasa nesnesi alan (hurda, alış, iade, takas farkı, elle çıkış, gider, açılış geri alma) ve projeksiyon alan (virman) | `312547.91 TRY` (invariant) | tr-TR ₺ + `kod` | Kasalar elle hareket, Gider, Gider virman, İşlemler ödeme ve Hurda `KasaBakiyeMesaji.Bicimle` ile sözlükten (TR+EN) yazılır. Ayrıştırıcıdan geçmeyen ekranlarda da artık ham "TRY" çıkmaz, çünkü sunucu metni zaten tr-TR |
| Nakit payı "nakit yetersiz" | `KasaBakiyeKapisi.NakitMesaji` | tr-TR ₺ (PD2) | aynı + `kod`. İstemci sözlüğü TR+EN (PD2 açık kalan 7 kapandı) | aynı ekranlar |
| Elle kart/havale araç payı | `KasaBakiyeKapisi.AracMesaji` | tr-TR ₺ (PD2) | aynı + `kod` | istemci çevirisi yok, TR metin. TRY yok |
| Virman kart/havale payı | `VirmanModulu` | tr-TR ₺ (PD2 `ParaMetni`) | değişmedi | — |
| Gün kapanışı farkı | `KasaModulu.GunKapat` (`{x:N2} TL`) | sunucu kültürüyle N2 + "TL" | **değişmedi** (§ 2) | TRY kalıbı yok. Açık kalan 2 |
| Kapalı gün | `GunKapanisiKapisi` (`{N2} TL`) | aynı | dosya alanı dışında, dokunulmadı | açık kalan 2 |
| Denetim izi metni | `KasaModulu:361`, `GiderModulu:367`, `VirmanModulu:191` (`0.00 TRY`) | invariant | dokunulmadı. Kullanıcı hata bandı değil, denetim kaydı | açık kalan 3 |

## 2. Sapmalar ve gerekçeler

- **Yapısal alan, kısmi uygulama.** NOT, kalıcı çözüm olarak "tutarlar ayrı JSON alanlarında" diyor.
  - Gövdeye ayrı tutar alanları eklemek `Common/ApiException.cs` (HataYakalayici) ve `Servisler/ApiIstemci.cs` (ApiHata) değişikliği gerektirir. Bu iki dosya paketin alanı dışında ve ortak hub. Görev "alan dışına çıkman gerekirse DUR" diyor.
  - Bu yüzden yapısal kısım **mevcut** `kod` alanıyla yapıldı. Var olan `AlanIsareti.KodIle` mekanizması kullanıldı, ApiException'a dokunulmadı. Gider ekranı reddi artık metne değil koda bakarak tanıyor (metin eşleşmesi yedek olarak duruyor).
  - Tutarlar hâlâ metinden ayrıştırılıyor. Ancak hiçbir ekranda `TRY` çıkmıyor: sunucu metni tr-TR, istemci iki biçimi de okuyor.
  - Ayrı tutar alanları (`bakiye`, `istenen`, `paraBirimi`) için açık kalan 1'e bakın.
- **Eski biçim okunmaya devam ediyor.** Geçiş uyumu için gerekli: eski sürüm sunucu ya da önbellekteki yanıt yine okunur. Mevcut doğrulayıcı testleri (`TOPLU1SinirTestleri`, `GiderEkranTestleri`) eski biçimi kullanıyor ve yeşil kalıyor.
- **Kesme işaretli kasa adı.** Yeni ayrıştırıcı "Merkez'deki Kasa" adını da okuyor (`.+?`). TOPLU-1'in `Skip`'li testi `T2_Kasa_adi_kesme_isaretli` artık geçmeli, ama Skip'i kaldırmak başkasının test dosyasına dokunmak olur. Kaldırılmadı, kendi testim (`PD3_K_istemci_iki_bicim_…`) aynı iddiayı sınıyor.
- **Gün kapanışı fark mesajı geri alındı.** Önce `KasaModulu.GunKapat` fark mesajı da `ParaMetni`'ye çevrilmişti. Tam süitte iki mevcut test bu metni "800,00 TL fark" diye birebir arıyor (`ParaDemoDbTestleri.PD1_…`, `PARADEMOSinirDbTestleri.PDS_kurus_…`). Bulgu bu mesajı istemiyordu ve TRY kalıbı da taşımıyor. Değişiklik geri alındı, `KasaModulu.cs`'e bu paketten net değişiklik yok. Kültürden bağımsız biçim için açık kalan 2'ye bakın.
- **PRO takas metni, PARA-DEMO-2 ile aynı.** Kimlik tespitine özgü eksik yoksa PRO cümlesi PARA-DEMO-2'deki satıcı cümlesinin aynısı ("Satıcının eksik bilgisi: … (VUK md.234)"). Böylece PARA-DEMO-2'nin `PD2E_8_…` sözleşmesi korundu.
- **Ek dosya dokunuşları (alan genişlemesi, gerekçeli):**
  - `Kuyumcu.Web/Servisler/UyumServisi.cs`: `FaturasizIslem` kaydına varsayılanı null olan `OdemeAraclari` parametresi. İstemci DTO'su; olmadan gün sonu adı üretilemez.
  - `Kuyumcu.Web/Pages/Raporlar.razor`: **tek hücre** (gün sonu faturasız tablosunun ödeme hücresi, `gs-faturasiz-odeme`). KAR-RAPOR şeridinin kâr kartı ve stok yaşı bölümlerine dokunulmadı.
  - Bulgu "RaporModulu gün sonu satırında da bacak türleri yazılsın" diyor. Sunucu alanı tek başına ekranı değiştirmez.
- **Gider istemci kilidi değişmedi.** Kilit hâlâ toplam bakiyeye bakıyor (PARA-DEMO-2 açık kalan 6). Bu paket yalnız gösterimi ayırdı. Nakit payı aşımında sunucu reddi artık sözlükten ve tr-TR olarak gösteriliyor.

## 3. Migration
Gerekmedi. Şema değişikliği yok.

## 4. Derleme ve testler

- **`dotnet build Kuyumcu.sln --no-incremental`:** 0 hata.
  - 2 uyarı başka şeridin test dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601 ve `KesifP2Testleri.cs:455` xUnit2029. İkisi de PARA-DEMO-2'den beri var.
  - Bu paketin dosyalarında uyarı 0. İlk koşumda kendi testimde çıkan CS8631 düzeltildi.
- **Tuzak.** Mutasyon koşumlarından sonra artımlı derleme bayat DLL bıraktı:
  - Bir kez Gider M4-2 mutantı canlı sunucuda göründü. Görüldü, `--no-incremental` ile temiz derlendi ve ölçüm yeniden yapıldı.
  - Bir kez de geri alınan `KasaModulu` değişikliği DLL'de kaldı.
  - Son derleme `--no-incremental` ile yapıldı.
- **Yeni testler (15):** `Kuyumcu.Tests/ParaDemo3Testleri.cs`. HTTP ve saf 6, Kasalar bUnit 3, Gider bUnit 3, Rapor bUnit 1, takas bUnit 2.
- **Filtreli koşumlar:**
  - ParaDemo3 / ParaDemo2 / PARADEMO2 / Toplu1 / TOPLU1 / GiderEkran / KasaBakiyeKapisi / SATIS3Ekran / SATIS3T24: **327/329** (2 atlanan: başkalarının Skip'i).
  - Geniş filtre (Kasa, Gider, Virman, Takas, Masak, Hurda, Rapor, Mutabakat, Acilis, Islem, S21, Iade, NakitPayi): **1864/1866**, 0 başarısız.
- **Tam süit:** aşağıda, § 4b.

### 4b. Tam süit
İki koşum yapıldı. Sonuçlar trx sayaçlarından okundu.

| Koşum | trx | Toplam | Geçti | Kırmızı | Atlandı |
|---|---|---|---|---|---|
| 1 | `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-3` | 9395 | 9386 | **7** | 2 |
| 2 (son kod, `--no-incremental` derlemeden sonra) | `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-3-t2` | 9395 | 9390 | **3** | 2 |

**Koşum 1'deki 7 kırmızı:**
- 2'si bu paketindi: `ParaDemoDbTestleri.PD1_toplam_bakiye_sayilirsa_…` ve `PARADEMOSinirDbTestleri.PDS_kurus_0_005_…`. Sebep `KasaModulu` gün kapanışı fark mesajının değiştirilmesiydi. Değişiklik geri alındı (§ 2). Koşum 2'de ikisi de yeşil.
- 5'i PATCH-SOZLESME şeridinin.

**Koşum 2'deki 3 kırmızı, hepsi PATCH-SOZLESME (t9) şeridinin alanında:**
- `PATCHSOZLESMESinirTur3HttpTestleri.Sinir3_urun_PATCH_Turkce_dize_400_SQL_DEGISMEZ` ×2 ("45.000", "1.500"): ürün PATCH.
- `PATCHSOZLESMESinirTur3FaturaTestleri.Sinir3_fatura_kalem_miktar_0_0004_…`: fatura kalemi PATCH.

Bu paket Urun/Fatura PATCH dosyalarına dokunmadı. O şeridin işi aynı ağaçta sürüyor: koşum 1'de aynı şeridin `PatchSozlesmeKolonTavani…` testleri de kırmızıydı, koşum 2'de yeşil.

**Önce/sonra:** PARA-DEMO-2 tur 5 tam süiti 9313 testti. Bugün 9395. Fark: bu paketin 15 testi ve öteki şeritlerin aynı sürede eklediği testler.

**Paket kapısı (7): bu paketin yeni kırmızısı 0.**

## 5. Mutasyon tablosu

Yöntem: yedek → `perl -0777 -pi` ile tek eşleşme → filtreli test → geri kopyala → `cmp`. Yedekler `ham/PARA-DEMO-3-olcum/mut-yedek/`, sonuçlar `mutasyon-sonuc.txt`. Hepsi geri alındıktan sonra `cmp` **AYNI**, son derleme `--no-incremental` 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | `KasaBakiyeKapisi.Mesaj`: bakiye yeniden invariant + kod (`312547.91 TRY`) | `PD3_K_elle_cikis_toplam_kapisi_mesaji_trTR_…`, `PD3_K_virman_toplam_kapisi_projeksiyon_yolu_da_trTR`, `PD3E_K_elle_gider_2_000_000_…` ×3 (BASİT tr, PRO tr, BASİT en) | AYNI |
| M1-2 | `Dogrula(kasa)`: `.KodIle(YetersizKodu)` kaldırıldı | `PD3_K_elle_cikis_toplam_kapisi_mesaji_trTR_isaretli_TRY_yok_kod_KASA_BAKIYE_YETERSIZ` | AYNI |
| M1-3 | Kasalar.razor `HareketGir` catch: `KasaBakiyeMesaji.Bicimle` kaldırıldı | `PD3E_K_elle_gider_2_000_000_…` ×3 | AYNI |
| M1-4 | `KasaBakiyeMesaji` yeni kalıp: ` ` → boşluk (tr-TR biçim tanınmaz) | `PD3E_K_…` ×3, `PD3_K_elle_cikis_…`, `PD3_K_istemci_iki_bicim_ve_kesme_isaretli_kasa_adi` | AYNI |
| M1-5 | `KasaBakiyeMesaji` nakit kalıbı devre dışı (`if (false && …)`) | `PD3_K_nakit_kapisi_kod_ve_istemci_ayni_dil_ve_bicimle_yazar` | AYNI |
| M2-1 | Islemler.razor: `TakasBirlesikKilit` → `TakasSaticiKilidi` (eski kilit) | `PD3E_T_BASIT_degisim_kilidi_tum_eksik_alanlari_sayar_MASAK_kelimesi_yok`, `PD3E_T_PRO_degisim_kilidi_tek_cumle_iki_gerekce` | AYNI |
| M2-2 | `Islemler.ParaDemo3.cs`: işlem amacı eksik listesinden çıkarıldı | aynı iki test | AYNI |
| M3-1 | `RaporModulu`: `odemeAraclari` koşulu `"KARMA_X"` (hiç dolmaz) | `PD3_G_gun_sonu_faturasiz_KARMA_satiri_odemeAraclari_KART_HAVALE_tek_bacak_null` | AYNI |
| M3-2 | Raporlar.razor: hücre eski `KatalogSozlugu.OdemeAraci` ("Karışık") | `PD3E_G_PRO_gun_sonu_faturasiz_karma_satiri_Kart_arti_Havale_Karisik_YOK` | AYNI |
| M4-1 | Gider.razor: nakit ayrımı hiç yüklenmez (`.Where(a => false)`) | `PD3E_N_gider_once_cekmecedeki_nakit_sonra_toplam` (BASİT, PRO) | AYNI |
| M4-2 | Gider.razor: nakit rozeti toplamı basar (`nakitKasa.Bakiye`) | `PD3E_N_gider_once_cekmecedeki_nakit_sonra_toplam` (BASİT, PRO) | AYNI |

**M4-1 ve M4-2'nin yeniden koşumu.** Gider.razor'daki P-11 düzeltmesinden sonra (§ 6) iki mutasyon yeniden koşuldu. Sonuç aynı: kırmızı ve `cmp` AYNI.

**demo-tohumla.js** için birim test yok (JS betiği). Kanıtı canlı tohumlama ve SQL (§ 7).

## 6. denetim.sh

- **İlk koşum:** 2 ✗.
  - `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47`. Sebebi bu paketti: Gider `NakitAyrimiYukle` catch'i `nakitAyrimlari = []` yapıyordu.
  - P-30.
- **Düzeltme:** catch artık son bilinen veriyi silmiyor. Hata varken nakit satırı çizilmiyor (`SecilenAyrim` hata yokken dolu). Gider.razor P-11 sayısı 0.
- **İkinci koşum:** tek ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç tutuldu). Diğer tüm sayaçlar ✓ (`denetim2.txt`).

## 7. Canlı doğrulama (5162, kiracı `PAKETPARA-DEMO-3-TEST Kuyumculuk` = `01M3Y98STF6V26CXEYJ5TXB97X`)

Kiracı **değiştirilmiş `demo-tohumla.js`** ile kuruldu: `--prova-kiraci`, rapor `/private/tmp`. Sonuç: "TOHUMLANDI … {cari:6, urun:12, satis:9}". Yanıtlar okundu (`canli.txt`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| SQL: açılış satırı | ACILIS hareketine bağlı, elle "Gelir" yok | `DIGER 150000.00 · bağlı=1 · ACILIS`. Elle "Açılış devri" bağsız satır **0** |
| GET `/raporlar/pos-fatura-mutabakati` | tahsilat açılışı içermez | 200 `toplam 66395.57, nakit 43619.08, kart 22776.49, acilisDevri 150000` |
| SQL: girişler − ACILIS bacağı | = API toplamı | **66395.57** (DIGER 150000 + NAKIT 43619.08 + KART 22776.49 − 150000) |
| POST `/kasalar/{ana}/hareket` CIKIS 2.000.000 NAKIT | 400, tr-TR, kod | 400 `kod=KASA_BAKIYE_YETERSIZ` "'Ana Kasa' kasasının bakiyesi yetersiz: 216.395,57 ₺ var, 2.000.000,00 ₺ isteniyor. …", `TRY` yok |
| SQL: satır sayısı önce/sonra | yazma yok | 9 / 9 |
| GET `/kasalar/nakit-ayrimi` (ana) | nakit = bakiye − kart − havale | API `toplam 216395.57 · nakit 193619.08 · kart 22776.49`. SQL `216395.57 − 22776.49 − 0 = 193619.08` |
| POST elle çıkış nakit + 1 (193.620,08) | 400 nakit kodu | 400 `kod=KASA_NAKIT_YETERSIZ` "çekmecede 193.619,08 ₺ nakit var, 193.620,08 ₺ nakit isteniyor. Kasadaki kart (22.776,49 ₺) ve havale (0,00 ₺) …", `TRY` yok |
| POST `/islemler/satis-fisi` KART 3000 + HAVALE 2000 | 200 | 200 |
| GET `/cari/{id}/ekstre` | `odemeAraclari ["KART","HAVALE"]` | `KARMA · ["KART","HAVALE"]` |
| GET gün sonu `faturasizIslemler` | aynı liste | `KARMA · ["KART","HAVALE"]`, tlTutari 5000 |
| SQL `odeme_kalemleri` (fiş) | KART 3000 sıra 0, HAVALE 2000 sıra 1 | `KART 3000.00 0 · HAVALE 2000.00 1` |

**Not.** Fiş sonrası kasa toplamı 221.395,57 ve nakit 193.619,08 ekranda da aynı (§ 8).

**Temizlik:**
- `zsh kiraci-temizle.sh 01M3Y98STF6V26CXEYJ5TXB97X --uygula` → 413 satır + tenants 1 satır silindi.
- Yeniden sorgu: "Kiracı YOK". `tenants where id=… or ad like 'PAKETPARA-DEMO-3%'` = **0**.
- `tenants` başta 15, sonda 19. Fark başka şeritlerin aynı sürede açtığı kiracılar: `PAKETPATCH-SOZLESME-TEST dogrulama …` ×4 ve diğerleri. QA-TEST-* kiracılarına dokunulmadı.

**Süreçler:**
- API (5162), vekil (5163) ve Chrome (9910) `kill` ile durduruldu. Üçü için `lsof` boş.
- `fikstur.json` ve sistem anahtarı dosyası `printf ''` ile boşaltıldı. Anahtar bu tur için üretilmiş rastgele bir değerdi ve ekrana basılmadı.

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9910, vekil 5163, `sikisma-olc.js`)

- **Kapsam:** 5 ekran × 1440/1280/375 × BASİT/PRO × açık/koyu = **60 ölçüm** (`olcum.jsonl`). Fotoğraflar: `foto/<ekran>__<Mod>_<tema>_<genişlik>.png`.
- **Ekranlar:**
  - `kasa-gider-red`: Kasalar › Ana Kasa › + Hareket › Gider › 2.000.000 › Kaydet
  - `gider`: Gider › Yeni gider
  - `takas-kilit-1`: Değişim; Tuna Akarsu, 60 g eski altın + Burma Bilezik, satıcı Vatandaş
  - `takas-kilit-2`: aynı, TCKN ve adres girildikten sonra
  - `gunsonu`: `/raporlar?sekme=gunsonu`

| Ekran | 1440 B/P (açık·koyu) | 1280 B/P | 375 B/P | Not |
|---|---|---|---|---|
| kasa-gider-red | K1 0 · K3 0/0 · **K5 0** · yatay 0 · K9 2 | aynı | K9 6 (B) / 4 (P) | K9: çekmece sayfanın üstünde. PARA-DEMO-2'de bilinen yanlış pozitif; fotoğrafta yazı binmesi yok. Bant metni 12/12 aynı, `TRY=false`, ham sayı yok |
| gider | 0/0/0/0/0/0 | 0 | 0 | 12/12 `nakit-once`: "Çekmecedeki nakit: 193.619,08 ₺", ardından "Kasa toplamı (kart ve havale dahil): 221.395,57 ₺" |
| takas-kilit-1 | 0 | 0 | 0 | BASİT 6/6 "Eksik: TCKN, adres, işlem amacı, beyan açıklaması (en az 20 harf).". PRO 6/6 iki gerekçeli tek cümle. Kilit görünür (`gorunmez` yok; 1440: 832×19, 375: 351×38) |
| takas-kilit-2 | 0 | 0 | 0 | BASİT 6/6 "Eksik: işlem amacı, beyan açıklaması (en az 20 harf).", `MASAK=false`. PRO 6/6 "… — müşteri kimlik tespiti için gerekli (MASAK)." |
| gunsonu | 0 | 0 | 0 | BASİT "Tahsilat 71.395,57 ₺: Nakit 43.619,08 ₺ · Kart 25.776,49 ₺ · Havale 2.000 ₺" (açılış yok). PRO faturasız tabloda "Kart + Havale", `karisik=false`, "Açılış devri (tahsilata girmez) 150.000 ₺" |

Sütunlar: K1 gizli-görünen · K3 taşan/kırık · K5 TRY · K9 binen · yatay kaydırma. Satır başına ayrıntı `olcum.jsonl`'da. 60 ölçümün hepsinde **K5 (TRY) = 0** ve **yatay = 0**.

**Read ile görülen fotoğraflar:**
- `kasa-gider-red__Basit_aydinlik_1440`: çekmecede hata bandı tr-TR.
- `kasa-gider-red__Basit_aydinlik_375`: çekmece tam ekran, bant 3 satıra sarıyor, taşma yok.
- `gider__Basit_aydinlik_1440` ve `gider__Profesyonel_karanlik_375`: nakit (yeşil rozet) üstte, toplam altta.
- `takas-kilit-1__Basit_aydinlik_1440`: kilit cümlesi Kaydet altında, zorunlu (*) alanlarla aynı liste.
- `takas-kilit-1__Profesyonel_karanlik_375`: PRO cümlesi 4 satıra sarıyor, taşma yok.
- `gunsonu__Basit_aydinlik_1440`: Gün sonu kartında tahsilat 71.395,57.
- `gunsonu__Profesyonel_karanlik_1280` ve `gunsonu-faturasiz-karma__Profesyonel_karanlik_1280`: Açılış devri ayrı satırda, faturasız tabloda "Kart + Havale".

## 9. DoD sözleşmesi

- **Yeni varlık ya da kayıt türü yok.** Yeni uç yok.
- **Yeni yanıt alanları:**
  - Ret gövdelerinde `kod`, yalnız kasa kapısı reddinde. Mevcut YASAL-3 sözleşmesi: işaretsiz gövdeler değişmez.
  - Gün sonu `faturasizIslemler[].odemeAraclari`: salt okunur türetilmiş görünüm, yalnız KARMA satırda dolu.
- **Demo açılışı.** Artık kurulum açılış yolunu kullanıyor. Bu kayıt **düzeltilebilir**: mühürlenmemiş oturum `/kurulum/acilis/geri-al` ile ters kayıtla geri alınır (`DuzeltmePolitikasi` "AcilisKalemi").
- **Düğmeler.** Değişim Kaydet kilitliyken sebebini yazar (alan alan). Gider satırları yalnız gösterim.

## 10. Değişen dosyalar (paket alanı; `git status --porcelain | grep -v /(bin|obj)/` + izlenmeyen dizinlerdeki dosyalar)

- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaBakiyeKapisi.cs`: `Mesaj` tr-TR, 3 makine kodu.
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`: **yalnız** gün sonu `faturasizIslemler` ve `odemeAraclari`.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.ParaDemo3.cs` (yeni, TR+EN).
- `dotnet/Kuyumcu.Web/Servisler/KasaBakiyeMesaji.cs`: iki biçim ve nakit cümlesi.
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`: `FaturasizIslem.OdemeAraclari`.
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor`: `HareketGir` catch, tek satır.
- `dotnet/Kuyumcu.Web/Pages/Gider.razor`: nakit/toplam gösterimi, `NakitAyrimiYukle`, `YetersizMetni`/`Reddi`, iki catch.
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`: kilit sırasında tek ifade (`TakasBirlesikKilit`), satır sayısı 1399.
- `dotnet/Kuyumcu.Web/Pages/Islemler.ParaDemo3.cs` (yeni).
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`: tek hücre.
- `dotnet/Kuyumcu.Tests/ParaDemo3Testleri.cs` (yeni).
- `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js`: **yalnız** `kasa-acilis` adımı. Önceki hâli `ham/PARA-DEMO-3-olcum/demo-tohumla.js.once`.
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`: bu paketten **net değişiklik yok** (§ 2, geri alındı).

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):**
- Kasalar.razor, Gider.razor, Islemler.razor: üçü de PARA-DEMO-2'de açılmıştı. Gerekçe: aynı bulgu zincirinin kapanış hakemi.
- Raporlar.razor: gün sonu hücresi.

**Kurallar:** COMMIT yok. Dosya silme yok.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) build 0 hata | KARŞILANDI | `--no-incremental` 0 hata. 2 uyarı başka şeridin test dosyalarında |
| (2) kendi testlerim 0 başarısız | KARŞILANDI | ParaDemo3 15/15, geniş filtre 1864/1866 (2 Skip) |
| (3) sayaçlarım denetim.sh'ta ✓ | KARŞILANDI | P-11 düzeltildi. Tek ✗ P-30 (hariç) |
| (4) mutasyon kanıtı | KARŞILANDI | 11/11 kırmızı, cmp AYNI (§ 5) |
| (5) canlı doğrulama, API=SQL | KARŞILANDI | § 7 |
| (6) test kiracısı silindi | KARŞILANDI | § 7, sorgu 0 |
| (7) bu paketin yeni kırmızısı 0 | KARŞILANDI | § 4b |
| Kabul: hiçbir ekranda ham TRY yok | KARŞILANDI | Tarama tablosu (§ 1) + 60 ölçümde K5=0 (Kasalar elle hareket, Gider, İşlemler değişim, gün sonu) |
| Kabul: takas kilit sebebi eksik alana göre ve sade | KARŞILANDI | § 1 T, § 8 |
| Kabul: demo-tohumla sonrası Tahsilat açılışı içermez (API=SQL) | KARŞILANDI | § 7 |
| Kabul: gün sonu ve ekstre karmayı aynı dille yazar | KARŞILANDI | § 7, § 8 |
| Kabul: Gider'de nakit ve toplam ayrı | KARŞILANDI | § 8 |
| Gerçek tarayıcı kuralı | KARŞILANDI | 1440/1280/375 × BASİT/PRO × açık/koyu, fotoğraflar Read ile görüldü |
| COMMIT yok, dosya silme yok, sır basılmadı | KARŞILANDI | — |

## 12. Açık kalanlar

1. **Ret gövdesine ayrı tutar alanları** (`bakiye`, `istenen`, `paraBirimi`).
   - Gerekenler: `ApiException.cs` HataYakalayici ve `ApiIstemci.cs` (ortak hub).
   - Bugün kod alanı var. Tutarlar tr-TR metinden ayrıştırılıyor.
   - Sahibi: Taşkın + Ertuğ, ortak hub kararıyla.
2. **Gün kapanışı mesajları sunucu kültürüyle `{N2} TL` yazıyor** (`KasaModulu.GunKapat` fark mesajı, `GunKapanisiKapisi`).
   - Sunucu en-US kültürde koşarsa "1,000.00 TL" olur.
   - Düzeltmek iki mevcut testin ("800,00 TL fark") sözleşmesini değiştirir.
   - Sahibi: PARA-DEMO kasa kapısı bakımı.
3. **Denetim izi metinleri invariant "0.00 TRY"** (`KasaModulu:361`, `GiderModulu:367`, `VirmanModulu:191`). Kullanıcı hata bandı değil, denetim kaydı ekranında görünür.
4. **Gider istemci kilidi toplam bakiyeye bakıyor** (PARA-DEMO-2 açık kalan 6, değişmedi). Nakit payı aşımında sunucu reddi bantta, sözlükten ve tr-TR olarak gösteriliyor.
5. **Araç payı ("kart tahsilatı … kartla çıkarılamaz") mesajının istemci çevirisi yok.** İngilizce arayüzde Türkçe görünür. TRY kalıbı yok.
6. **TOPLU-1'in `T2_Kasa_adi_kesme_isaretli` testi hâlâ `Skip`.** Ayrıştırıcı artık bu adı okuyor (kendi testim yeşil). Skip'i kaldırmak sahibine kaldı.
7. **Ölçer K9** çekmeceyi alttaki sayfaya "binen" sayıyor (bilinen yanlış pozitif, betik bakımı).
8. **Tam süitte başkasına ait kırmızılar** (sahibi PATCH-SOZLESME / t9): `PATCHSOZLESMESinirTur3HttpTestleri.Sinir3_urun_PATCH_Turkce_dize_400_SQL_DEGISMEZ` ×2 ve `PATCHSOZLESMESinirTur3FaturaTestleri.Sinir3_fatura_kalem_miktar_0_0004_…`.

## 13. Kurucu / mali müşavir

- **Mevzuat.** Bu pakette yasal eşik ya da oran yazılmadı.
  - Takas kilidi yeni bir yasal kural koymuyor. Sunucunun bildirdiği gereksinimi (`/masak/gereksinim`) ve var olan satıcı kuralını ad ad yazıyor.
  - PRO gerekçe metni (VUK md.234, MASAK) PARA-DEMO-2'deki dayanaklarla aynı. Yeni bir hüküm iddiası yok.
- **Kurucuya.** Demo kiracısı yeniden tohumlanırsa açılış devri artık Kurulum açılışı olarak görünür. Betik açılışı mühürlemez; mühürleme geri alınamaz bir işlem ve betikte hiç `muhurle` çağrısı yok. Mühür gerekirse "Kurulum › Mühürle" adımı elle yapılır.
