# PAKET TOPLU-6 — Uygulama raporu (kalan görünüm kuyruğu, hasımsız)

Taşkın + Ertuğ (Opus). 03.10.2026, tek tur (+ tur içi düzeltme). Dalga TOPLU-6 · t9 · API 5188 / vekil 5189 / CDP 9980–9981 · `KUYUMCU_TEST_DB=kuyumcu_test_t9`.
Kaynak: `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` bölümleri "02.10 13:30 — TOPLU-4 t2 kalanları", "02.10 07:20 — YASAL-LISTE-DEMO", "03.10 00:15 — TOPLU-5 t2 kalanları" ve tekrar eden "koyu temada seçili sekme/segment soluk". Ayrıntılı hakem kanıtı: wf_74145c34-719 journal (TOPLU-4 hakemi t2).
Kanıtlar: `ham/TOPLU-6-olcum/` — `once/` (önce ölçümü), `son2/` (**bağlayıcı son ölçüm**, 240 ölçüm + 240 görüntü), `son3/` (son düzeltmeden sonra değişen 5 senaryo, 60 ölçüm), `canli/` (API + SQL). `son/` klasörü GEÇERSİZDİR: tarayıcı profilindeki önbellek eski `dugme.css`/`sade.css`'i sunmuştu (koyu sekme ölçümü önce ile birebir aynı çıktı → fark edildi, CDP `Network.setCacheDisabled` + yeni profil ile `son2` alındı).

**Para / veri / yasal davranış DEĞİŞMEDİ.** Sunucu kodu değişmedi, migration yok (gerekmedi). Gider/virman kapı kuralları, yasal liste fail-closed uyarısı ve koşulları aynen; yalnız metin, yer ve görünüm.

## 0. TUR 2 (03.10 05:10–06:05) — tasarım hakeminin KRİTİK bulgusu: BASİT "Para aktarımı" adı yarım

**Bulgu:** BASİT'te sekme/kart/liste "Para aktarımı" derken birincil düğme "Virmanı kaydet", üst başlık ve menü "Gider ve virman".

**Yapılan (dosya alanı içinde — `Pages/Gider.razor`, `Sozlukler.Toplu6.cs`):** BASİT'te ekran gövdesinin "virman" dediği her yer tek ada bağlandı (PRO adları AYNEN):

| Yer | Önce (BASİT) | Sonra (BASİT) | PRO |
|---|---|---|---|
| `#gd-v-kaydet` birincil düğme | Virmanı kaydet | **Para aktar** (`T6.VirmanYapBasit`) | Virmanı kaydet |
| Kayıt sonrası başarı bandı (`#gd-basari`, `#gd-v-sonuc`) | Virman kaydedildi: … | **Para aktarıldı: Ana Kasa → Vadesiz TL · 10 ₺** | Virman kaydedildi: … |
| Geri alma onayı (`#gd-storno-onay`) | Virmanın İKİ BACAĞI birlikte… | Bu para aktarımının iki tarafı birlikte geri alınacak… | aynen |
| Geri alma sonucu bandı | Virman geri alındı (iki taraf birlikte) (QA10) | Para aktarımı geri alındı (iki taraf birlikte) | aynen |
| Eksi kasa uyarısı (`#gd-eksi-kasa-uyari`) | …virmanla para aktarın. | …bankadan para aktarın. | aynen |
| Sayfa açıklaması (`#sayfa-basi-gider-aciklama`, ⓘ) | Virman ciroya girmez… | Para aktarımı ciroya girmez… | aynen |

Canlı ölçüm (başsız Chrome, `t2b/olcum.jsonl`, 24 ölçüm): BASİT 1440/1280/375 × açık/koyu → `kaydet:"Para aktar"`, ana alanda görünen "virman" geçen metin **0** (`gorunenVirman:[]`); gerçek kayıt sonrası bant "Para aktarıldı: Ana Kasa → Vadesiz TL · 10 ₺". PRO'da "Virmanı kaydet" / "Virman kaydedildi" aynen.

**Yapılmayan — üst bar başlığı ve menü adı "Gider ve virman" (YAZILI İZİN GEREKİR):** ad `Kuyumcu.Core/I18n/Sozlukler.Menu.cs` (`MenuAdlari.GiderVirman`) → `Layout/RotaBasliklari.cs` ve `Layout/MenuTanimi.cs`'ten gelir; üçü de bu paketin dosya alanı DIŞINDA. Ayrıca (a) `Sozlukler.Menu.cs` başlığına göre menü adları **kurucunun tuvaliyle birebir** (`tuval/project/Ekranlar.dc.html:68` "Gider ve virman"; "plan ile çelişirse TUVAL geçerlidir"), (b) menü adı bugün moddan bağımsızdır (BASİT'e özel ad yeni bir altyapı ister), (c) UX-S31 "menüde X yazan ekranın üst barında Y yazamaz" kuralı gereği başlığı `UstBarBasligi` ile yalnız sayfadan değiştirmek menü↔başlık ayrışması doğururdu — yapılmadı. Bulgunun kendi şartı ("dosya alanı dışındaysa orkestratörden yazılı izin") karşılanmadı: alt ajandan orkestratöre dönüş kanalı yok, izin gelmedi. **Orkestratöre öneri (tek satırlık değişiklik):** `Sozlukler.Menu.cs` → `GiderVirman: new("Gider ve para aktarımı", "Gider", "Gider ve virman")` (Esanlam eski adı aramada korur; S1gMenuTuvalTestleri:134 beklentisi birlikte güncellenir) — ya da BASİT/PRO ayrı menü adı altyapısı. Tuvalden sapma olduğu için kurucu onayı önerilir. Görüntü: `t2b/foto/virman-kayit__BASIT_aydinlik_1440.png` (sol menü ve üst başlık "Gider ve virman", gövde "Para aktar").

**Testler (tur 2):** `Toplu6Testleri.cs` +5 metot / +8 koşum: `T6_Para_aktarimi_BASIT_kaydet_dugmesi_ve_aciklama_tek_ad`, `T6_Para_aktarimi_basari_bandi_moda_gore_tek_ad` (BASİT/PRO), `T6_Para_aktarimi_geri_alma_onayi_ve_sonucu_moda_gore_tek_ad` (BASİT/PRO), `T6_Gider_eksi_kasa_uyarisi_moda_gore_tek_ad` (BASİT/PRO), `T6_Para_aktarimi_BASIT_sozluk_metinleri_virman_demez`; `T6_Para_aktarimi_PRO_adlari_AYNEN` PRO düğme + açıklama ile güçlendirildi. Beklentisi güncellenen eski test: `QA10KritikTestleri.QA10_BASIT_virman_geri_alininca_bantta_STORNO_ve_BACAK_gecmez` (BASİT bant artık T6 metni; "storno"/"bacak" yokluğu iddiaları aynen). Bulunan gerçek kusur: ilk yazımda geri alma sonucu ternary'si PRO dalının içine konmuştu (ölü kod) — yeni test kırmızı verdi, BASİT dalına taşındı.

**Mutasyon (tur 2, 7 adet; yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-6/t2m/`, her biri derle → filtreli test → geri yükle → `cmp` AYNI; son temiz build 0/0):**

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M6-48 | `#gd-v-kaydet` BASİT dalı kaldırıldı | T6_Para_aktarimi_BASIT_kaydet_dugmesi_ve_aciklama_tek_ad | AYNI |
| M6-49 | başarı bandı BASİT dalı kaldırıldı | T6_Para_aktarimi_basari_bandi_moda_gore_tek_ad(BASIT) | AYNI |
| M6-50 | sayfa açıklaması BASİT dalı kaldırıldı | T6_Para_aktarimi_BASIT_kaydet_dugmesi_ve_aciklama_tek_ad | AYNI |
| M6-51 | geri alma onayı BASİT dalı kaldırıldı | T6_Para_aktarimi_geri_alma_onayi_ve_sonucu_moda_gore_tek_ad(BASIT) | AYNI |
| M6-52 | geri alma sonucu eski QA10 metnine döndü | T6_…geri_alma…(BASIT), QA10_BASIT_virman_geri_alininca_bantta_STORNO_ve_BACAK_gecmez | AYNI |
| M6-53 | eksi kasa uyarısı BASİT dalı kaldırıldı | T6_Gider_eksi_kasa_uyarisi_moda_gore_tek_ad(BASIT) | AYNI |
| M6-54 | sözlük `VirmanKaydedildiBasit` → "Virman kaydedildi" | T6_Para_aktarimi_BASIT_sozluk_metinleri_virman_demez | AYNI |

(M6-48'in ilk koşumu başka şeridin eşzamanlı derlemesi yüzünden `Kuyumcu.Web.dll` kilit hatasıyla derlenemedi — geçersiz sayıldı, tek başına yeniden koşuldu: kırmızı.)

**Gerçek tarayıcı (tur 2):** kendi yayın kopyası `yayin-t2` (dotnet publish Release), API 5188, vekil 5189, başsız Chrome CDP 9982 (yeni profil, önbellek kapalı). Matris 2 senaryo (`virman-form`, `virman-kayit` = gerçek 10 ₺ aktarım) × {1440,1280,375} × {BASİT,PRO} × {açık,koyu} = **24 ölçüm + 24 görüntü** (`t2b/`): K1 0 · K3 0/0 · K5 TRY 0 · K8 0 · K9 0 · yatay 0 — 24/24. Koyu seçili sekme ("Para aktarımı"): kap 14,46 / zemin 16,25; açık 15,97 / 14,50. Read ile bakılan: `t2b/foto/virman-kayit__BASIT_aydinlik_1440.png`, `t2b/foto/virman-form__BASIT_karanlik_375.png`. (`t2/` ilk koşumdur: 1280'de K9 1 = önceki senaryonun açık bıraktığı Hedef açılır listesi formun üstündeydi — senaryo seçici metni düzeltildi, `t2b` temiz; ürün kusuru değil.)

**Canlı (tur 2, `canli-t2/`):** kiracı `PAKETTOPLU-6-TEST T2 Kuyumculuk` (01M3ZT3TB7GTM79Z8DZZ5ZXY77, `demo-tohumla.js --prova-kiraci`). `POST /bankalar` **200** (Ziraat / Vadesiz TL); `GET /bankalar` **200** 1 hesap; `GET /kasalar` **200** Ana Kasa 216250.91. Ekrandan 12 aktarım (her kombinasyonda 10 ₺). SQL: kasalar Ana Kasa **216130.91** (= 216250.91 − 120), banka_hesaplari Vadesiz TL **120.00**, banka_hareketleri **12 satır / 120.00** — para davranışı aynen, yalnız ad değişti. Kapanış: API/vekil/Chrome `kill`; 5188/5189/9982 dinleyici 0. `zsh kiraci-temizle.sh 01M3ZT3TB7GTM79Z8DZZ5ZXY77 --uygula` kod 0; SQL: bu kimlik için tenants 0, kasalar 0, banka_hareketleri 0. tenants: başlangıç 15 → son 16 — fark, 02:47 UTC'de **başka şeridin** açtığı `PAKETRAPOR-GORUNUM-TEST dogrulama` (01M3ZTH48PAX4KETRPP29DT8M9; dokunulmadı); bu paketin kiracısı 0. Fikstür, anahtar ve yayın kopyasındaki geliştirme ayarı `printf ''` ile boşaltıldı.

**Derleme / denetim / tam süit (tur 2, son kod):** `dotnet build Kuyumcu.sln` **0 uyarı 0 hata**. `denetim.sh`: 303 ✓, tek ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç); önceki turdaki P-36b ✗ artık ✓. Tam süit (`/private/tmp/kuyumcu-serit/trx-pTOPLU-6-t2`): **10177 test · 10173 geçti · 1 başarısız · 3 atlandı**. Tek kırmızı `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (bUnit WaitFor zaman aşımı, render 133) — İşlemler alış/satış ödeme aracı ekranı, bu paketin dosyası değil; sınıf tek başına koşulunca **6/6 yeşil** (yük altında kararsızlık). **Bu paketin yeni kırmızı farkı: 0.**

## 1. Madde tablosu (önce / sonra / kanıt)

| # | Madde (kaynak) | Önce (ölçüm `once/`) | Sonra (ölçüm `son2/`,`son3/`) | Test · mutasyon |
|---|---|---|---|---|
| T6-1 | ORTA (tekrar eden) Koyu temada seçili sekme/segment soluk — ortak bileşen | Koyu: seçili dolgu `#090d16` / kap yüzeyi `#1a1e26` = **1,16**, sayfa zemini **1,04** (/raporlar, /uyum, /gider, /evraklar, /cariler segment, vade 7 gün); /kur şeridi 1,36 | TEK yerde (`dugme.css` `--secili-dolgu*`, iki karanlık kurulumu): karanlıkta ters dolgu (açık hap + koyu yazı). Ölçüm 9 sekme kimliği × koyu: kap **14,46**, zemin **16,25**, yazı **16,83**; /kur şeridi 16,25 (sade.css aynı belirtece bağlandı); üst çubuk Basit/Profesyonel aynı belirteç (toplu6.css). Aydınlık AYNEN (15,97). Ana eylem mavisi değil. Görüntü: `son2/foto/raporlar__PRO_karanlik_1280.png`, `vade__BASIT_karanlik_1280.png` | `T6_Koyu_*` (3), `T6_Kur_ve_doviz_sekme_seridi…`, `T6_Ust_cubuk…` · M6-1a, M6-1b, M6-2, M6-3 |
| T6-2 | YÜKSEK Vitrin BASİT: ayar tablosu açılışta açık; 375'te başlıklar yapışık, sağ sütun kesik | BASİT açık, 22 satır; 375: tablo-sar 743/317 (kesik) | Ortak `Katlanir` (#vitrin-ayar-kat, rozet = kalem sayısı): BASİT KAPALI, PRO açık; "Sıra" yalnız PRO; ≤600 px kart düzeni (her değerin solunda kendi başlığı, `data-etiket`), başlık satırı dar ekranda çizilmez. 375: 283/283, K9 0 (son3). "Kalem" başlığı sola hizalı | `T6_Vitrin_*` (4) + `T6_Vitrin_375…` · M6-10, M6-11, M6-12, M6-13, M6-46, M6-47 |
| T6-3 | YÜKSEK Kur "Ayar bazlı gram fiyatları" başlık hizası | th sola, td sağa | Milyem/Gram fiyatı `th.sayi`: th–td sağ kenar farkı **0/0/0** px; başlık üst kutudan **24 px** | `T6_Kur_sayisal…` (Theory), `T6_Kur_ayar_fiyat_basligi…` · M6-15, M6-16 |
| T6-4 | YÜKSEK Vade takibi tablosu sağdan kesik | 1440: 7 sütun; telefon "05320000102" | BASİT 5 sütun (iki tarih sütunu PRO'da); telefon "0532 000 01 02" (tel: bağlantısı ham); satır içi `style` 0. tablo-sar 1440/1280/375 sw=cw | `T6_Vade_*` (3) · M6-18, M6-19 |
| T6-5 | ORTA Liste sayaç notu sınırın altında da "en çok 50" | "12 kayıt gösteriliyor · en çok 50" | "12 kayıt gösteriliyor" (sınır `title`'da); sınırda eski cümle + daraltma uyarısı aynen (Ürünler + Ürün etiketleri) | `T4_Urunler_sinir…` (güncellendi), `T6_Urun_etiketleri…` · M6-29, M6-30 |
| T6-6 | ORTA Açıklama satırları kalanlar (Uyum, Vade, Kur, Vitrin) | Uyum arama notu 12 px satır; Kur BASİT alt not 12 px; Vitrin "Atlanan kalem yok" kartı | Uyum arama notu ⓘ (#uy-arama-ipucu); Kur notu ⓘ (#kur-pro-notu-ipucu, bağlantı içinde); Vitrin boş atlanan kartı BASİT'te çizilmez (PRO aynen) | `T6_Uyum_risk…`, `T6_Kur_BASIT_alt_not…`, `T6_Vitrin_atlanan…` · M6-9, M6-17, M6-14 |
| T6-7 | ORTA Kurulum: eksik adımın düğmesi yok, sayaç baştan kırmızı, alanı olmayan "Açıklama ⓘ" | Eksik "Müşteri açılış bakiyeleri" satırında eylem yok; "0 / 20" kırmızı; "Açıklama ⓘ" | "Şimdi gir" bağlantısı (açılış bölümünü açar, /kurulum#kur-acilis); sayaç boşken nötr (rgb(23,32,51)), yazınca eşik altında kırmızı (ortak GerekceAlani); "Nasıl girilir?" / "Mühürden sonra ne olur?" | `T6_Kurulum_*` (3) · M6-20, M6-21, M6-22, M6-23 |
| T6-8 | ORTA Sipariş formu kalanları | PRO "oturumdan" rozeti; "Daha fazla (…)" tam genişlik çubuk | Rozet iki modda yok; "+ Söz günü, açıklama, tahmini tutar ekle" bağlantı kalıbı (303×44); "Daha az göster" yalnız bölüm boşken (gizli değer gövdeye gitmez). "Kalemi çıkar" tek kalemde zaten disabled+title (ölçüldü: `kalemSil:true`) | `T6_Siparis_*` (3) · M6-24…M6-28 |
| T6-9 | DÜŞÜK "Yeni X" düğme dili | Siparişler "Yeni sipariş" (+ yok) | "+ Yeni sipariş" (Uyum/Ürünler kalıbı) | `T6_Siparis…` (FormAcik) · M6-24 |
| T6-10 | DÜŞÜK Kim borçlu satır düğmesi genişliği değişken | 375'te değişken | min-width 6,875 rem: 1440 3×110 px aynı x; 375 eşit | `T6_Kim_borclu…` · M6-41 |
| T6-11 | ORTA Yasal liste bandı kartlara 0 px yapışık | 0 px | **24 px** (16/16 ölçüm `bosluk:24`) | `T6_Yasal_bant…` · M6-4 |
| T6-12 | ORTA Bant çıkmaz sokak ("destek hattını arayın", numara yok; "Ayrıntı →") | Metin + "Ayrıntı →" | "Yasal liste henüz yüklenmedi." + "Uyum ekranında gör →"; Uyum → Risk liste panelinde kimin yüklediği yazılı ("sistem yöneticisi … yüklendiğinde uyarı kendiliğinden kalkar" — POST /sistem/yaptirim-listesi kaynak koddan doğrulandı). Koşul/fail-closed aynen | `T6_Bant…`, `T6_Uyum_liste_paneli…`, `T3_Liste_yuklenmemisse…` (güncellendi) · M6-5, M6-44, M6-6 |
| T6-13 | ORTA "Bekleyen iş" kutusu bant varken "Başka iş yok…" | "Başka iş yok. Yasal uyarılar üstteki kutularda." | "Bekleyen 1 iş üstteki uyarıda." (sayı bandın iş sayısından) | `T6_Panel_bekleyen…` · M6-7 |
| T6-14 | DÜŞÜK Uyum Risk 12 px "Neden?" + boş başlıkta yalnız "+ Yeni" | 12 px; başlık alanı yalnız düğme | 14 px; Risk sekmesi eylemsiz sayfa başı (Nakit sekmesi gibi; bildirim diğer sekmelerde aynen) — BASİT başlık alanı 0 px | `T6_Yasal_bant…`, `T6_Uyum_risk…` · M6-4(css), M6-8 |
| T6-15 | ORTA Virman BASİT jargon (tur 2'de birincil düğme, bantlar, açıklama da — § 0) + açıklama satırı + hesap yoksa boş durum | Sekme "Virman", liste "Virman", "Henüz virman kaydı yok.", açıklama satırı | BASİT her yerde "Para aktarımı/aktarımları", bant bağlantısı "Para aktar"; açıklama ⓘ; banka yoksa bilgi bandı + "Banka hesabı ekle"; tek hesapta form yerine boş durum + ekleme yolu (canlı: 1 kasa, 0 banka → boş durum). PRO adları aynen | `T6_Para_aktarimi_*` (4) · M6-35, M6-36, M6-37 |
| T6-16 | ORTA Gider nakit aşımı mesajı yanıltıcı | Bant "Kasadaki kartla alınan … çekmecede değil" (tutar toplamı da aşarken) | Kilit: "Çekmecede 193.526,66 ₺ nakit var; 250.000 ₺ gider için yetmiyor." Kart payı cümlesi YALNIZ tutar kasa toplamına sığıyorsa (200.000 → var; 250.000 → yok, bant yalnız çıkış yolu). Kural ve sunucu reddi aynen | `T6_Gider_nakit…`, PD3T2 iki test (güncellendi) · M6-33, M6-34 |
| T6-17 | DÜŞÜK Gider/Virman disabled gerekçesi uzakta, 12 px | Düğme yanında 12 px | İlgili alanın altında 14 px alan mesajı (kategori · kasa · tutar / kaynak · hedef · tutar); id'ler aynen | `T6_Gider_kategori…`, `T6_Gider_kasa…`, `T6_Para_aktarimi_BASIT…`, `T6_Alan_kilit…` · M6-31, M6-32, M6-38, M6-39 |
| T6-18 | ORTA Yazdırma önizlemesi Geri/Yazdır dengesiz | Yazdır x=823 (belge sağı 1254) | Geri solda (454), Yazdır sağ kenarda (sağ 1254 = belge sağı); 375'te Yazdır tam genişlik üstte, Geri metin bağlantısı (`son2/foto/ekstre__PRO_karanlik_375.png`) | `T6_Cikti_seridi…` · M6-40 |
| T6-19 | DÜŞÜK Döviz Veresiye kutusu boş gelsin | Kutu "0" | Açılışta boş (canlı: `deger:""`); yazınca değer aynen; gövde `dvTutar` aynen | `T6_Doviz_veresiye…` · M6-42 |
| T6-20 | DÜŞÜK Raporlar PRO 1280 yıl/ay seçici ezik | 80×44 / 76×44 | Ölçümde ezik değil (taşma yok, tek satır) — KAPALI, kod değişmedi | `son2` raporlar 1280 |

## 2. Yapılmayan / devredilen maddeler (gerekçeli)

| Madde | Neden | Devir |
|---|---|---|
| ORTA Has ondalık biçimi (karar 5: 4 ondalık) | `Bicim.cs` MADEN-2 şeridinin dosyası (DOKUNMA) — o şerit 04:30'da Bicim.cs'i değiştiriyordu | MADEN-2 |
| DÜŞÜK PRO Panel "Kur (24K Altın)" başlık iki kez | Panel.razor yalnız yasal bant yerleşimi dosya alanımda | Panel sahibi (MADEN-2 sonrası) |
| DÜŞÜK BASİT bölünmüş ödemede çift başlık | İşlemler/Fiş bileşenleri DEGISIM-GUNSONU alanı | DEGISIM-GUNSONU |
| DÜŞÜK Basılı ekstrede teknik not + BASİT başlık "Hesap ekstresi" | Not ve başlık belge bileşeni/sunucu belge verisinden geliyor (Cikti.razor dosya alanım yalnız araç şeridi) | Çıktı/belge sahibi |
| Vade "(bugün itibarıyla — gün seçiminden bağımsız)" alt metni | UX-S33 bu alt metni ZORUNLU kılar — kaldırılmadı | — (gerekçeli kabul) |
| Kurulum "Kasa ve banka açılışı eksi olamaz…" paragrafı | TL alanının `aria-describedby` hedefi (#kur-tl-kural); ⓘ içine taşımak hata nedenini erişilebilirlikten düşürür | — (gerekçeli kabul) |
| "Para aktarımı" adı Kasalar ekranında | Kasalar.razor dosya alanım dışında | Kasalar sahibi |
| Sınırın altında "en çok N" | UX-S29 "sınır ekranda yazar" ile hakem önerisi çatışıyor: sınır artık `title`'da (sınıra değince metinde). `S33_10`, `UX1C1 Etiketler…` beklentileri buna göre güncellendi — orkestratör onayı önerilir | Orkestratör |

## 3. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma-olc.js`, önbellek kapalı)

Yayın: `dotnet publish` kendi kopyası (`/private/tmp/kuyumcu-serit/TOPLU-6/yayin-son2`, son düzeltmeyle `yayin-son3`), parolasız oturum `inceleme-vekili.py` (5189). Matris: 20 senaryo × {1440, 1280, 375} × {BASİT, PRO} × {açık, koyu} = **240 ölçüm + 240 görüntü, hata 0, mod uyuşmazlığı 0** (`son2/olcum.jsonl`); son düzeltme sonrası 5 senaryo × 12 = **60 ölçüm, tüm sayaçlar 0** (`son3/`).

| Senaryo | K1 | K3 taşan/kırık | K5 TRY | K8 | K9 | Yatay | Not |
|---|---|---|---|---|---|---|---|
| / (panel) | 0 | 0/0 | 0 | 0 | 0 | 0 | bant↔kart 24 px; düğmeler 3×110 |
| /vitrin | 0 | 0/0 | 0 | 0 | 0 (son3) | 0 | BASİT kat kapalı, PRO açık |
| /vitrin (kat açık) | 0 | 0/0 | 0 | 0 | 0 (son3) | 0 | 375 kart düzeni 283/283 |
| /kur | 0 | 0/0 | 0 | 0 | 0 | 0 | th–td fark 0 |
| /raporlar/vade-takibi | 0 | 0/0 | 0 | 0 | 0 | 0 | BASİT 5 sütun |
| /kurulum, /kurulum (açık) | 0 | 0/0 | 0 | 0 | 0 | 0 | sayaç nötr |
| /siparisler yeni form | 0 | 0/0 | 0 | 0 | 0 | 0 | rozet 0 |
| /urunler | 0 | 0/0 | 0 | 0 | 0 | 0 | "12 kayıt gösteriliyor" |
| /uyum risk, /uyum | 0 | 0/0 | 0 | 0 | 0 | 0 | Neden? 14 px |
| /gider, nakit aşımı ×2 | 0 | 0/0 | 0 | 0 | 0 | 0 | kilit 14 px tutar altında |
| /gider?sekme=virman | 0 | 0/0 | 0 | 0 | 0 | 0 | hesap yok boş durumu |
| /cikti ekstre | 0 | 0/0 | 0 | 0 | 0 | 0 | Yazdır sağ = belge sağı |
| /raporlar | 0 | 0/0 | 0 | 0 | 0 | 0 | yıl/ay ezik değil |
| /cariler (+Döviz Veresiye) | 0 | 0/0 | 0 | 0 | 72+72 | 0 | K9 = açık döviz çekmecesinin katmanı (TOPLU-5'te de belgelendi); kutu boş |
| /evraklar | 0 | 0/0 | 0 | 0 | 0 | 0 | — |

Koyu seçili sekme kontrastı (sekme kimliği × 6 bileşim): cari-segment-hepsi, evrak-suzgec-tumu, gd-sekme-giderler, gd-sekme-virman, rp-sekme-ozet, uy-sekme-risk, uy-sekme-tespit → kap 14,46 / zemin 16,25 / yazı 16,83; kur-sekme-guncel, vade-ufuk-7 → 16,25 / 16,25 / 16,83 (önce 1,04–1,36).
Read ile bakılan görüntüler: `son2/foto/raporlar__PRO_karanlik_1280.png`, `vade__BASIT_karanlik_1280.png`, `ekstre__PRO_karanlik_375.png`; `son/foto/panel__BASIT_aydinlik_1440.png` ve `…_375.png`, `kurulum-acik__BASIT_aydinlik_1440.png`, `ekstre__BASIT_aydinlik_1440.png`, `vitrin-acik__BASIT_aydinlik_375.png` (bu "son" görüntüleri yalnız razor/toplu6.css değişikliklerini gösterir — koyu sekme için `son2` geçerlidir); `once/foto/raporlar__PRO_karanlik_1440.png`, `panel__BASIT_aydinlik_1440.png`, `vitrin__BASIT_aydinlik_375.png`, `uyum-risk…`, `kurulum…`, `siparis-yeni…`.
Ölçüm yan bulgusu: başsız Chrome'da tema değişiminden hemen sonra çekilen görüntüde düğmeler geçiş (transition) ortasında karanlık görünüyordu ("Kim borçlu" düğmeleri siyah) — ölçüm betiğine tema sonrası 1,5 sn bekleme kondu; ürün kusuru DEĞİL.

## 4. Canlı doğrulama (API 5188, test kiracısı `PAKETTOPLU-6-TEST Kuyumculuk` 01M3ZHNX0J9TZ8VK9M1CXXF6AM)

- Kurulum: `demo-tohumla.js --prova-kiraci` (ürünün kendi uçları; 6 cari, 12 ürün, 9 satış). `POST /gider-kategorileri` (Kira) **200**.
- `GET /masak/uyum-ozet` **200** listeEski:true, listeYasiGun:null → bant "Yasal liste henüz yüklenmedi." + "Uyum ekranında gör →", bekleyen "Bekleyen 1 iş üstteki uyarıda.".
- `GET /kasalar/nakit-ayrimi` **200** toplam 216250.91 = nakit 193526.66 + kart 22724.25 → ekranda 250.000 TL'de kilit "Çekmecede 193.526,66 ₺ nakit var; 250.000 ₺ gider için yetmiyor.", bant yalnız çıkış yolu; 200.000 TL'de bant kart payı cümlesi.
- `GET /bankalar` **200** hesaplar:[] + 1 kasa → Para aktarımı boş durumu + /banka bağlantısı.
- `GET /kurulum/durum` **200** CARI_ACILIS bitti:false → "Şimdi gir". `GET /raporlar/vade-takibi?ufukGun=7` **200** telefon "05320000102" → ekranda "0532 000 01 02". `GET /pano/ayarlar` **200** 22 kalem → katlanır rozeti 22.
- (İlk denemede ardışık 3 istek **401** "Geçersiz veya süresi dolmuş token" — betik her istekte yeniden giriş yapıyordu, giriş sınırı; tek girişle tekrarlandı, 200. Yanıtlar `canli/api.txt`.)
- SQL (`canli/sql.txt`): kasalar Ana Kasa 216250.91 TRY; banka_hesaplari 0; gider_kategorileri Kira; cariler.telefon ham "05320000102…" (veri DEĞİŞMEDİ, yalnız görünüm). Not: `pano_ayarlari` tablo adı yanlış tahmin edildi (ERROR 1146) — sorgu kanıta katılmadı, pano verisi API yanıtıyla gösterildi.
- Kapanış: API ve vekil `kill`; 5188, 5189, 9980–9995 dinleyici yok. `zsh kiraci-temizle.sh 01M3ZHNX0J9TZ8VK9M1CXXF6AM --uygula` kod 0. SQL: bu kimlik için tenants 0, cariler 0, hareketler 0. **tenants başlangıç 15 → son 15.** QA-TEST-* kiracılarına dokunulmadı. Fikstür, anahtar ve yayın kopyalarındaki geliştirme ayar dosyaları `printf ''` ile boşaltıldı.

## 5. Testler

- Yeni: `Kuyumcu.Tests/Toplu6Testleri.cs` — 41 test metodu (Theory dahil **44 koşum**), 10 sınıf (koyu sekme, CSS, kaynak, yasal bant, Vitrin, Kur, Vade, Kurulum+GerekceAlani, Siparişler, Gider/Para aktarımı). Filtreli: 44/44 yeşil; geniş ilgili küme (Toplu1–6, ParaDemo*, KesifP6/P7, Vitrin, Vade, Kurulum, UX*, S31/S35, Tevsik, Olcek, OrtakGorunum, Cari*) 1323/1323 (1 atlanan) — kod son haline getirildikten sonra Toplu4/Toplu6/KesifP6/S33_10/UX1C1 kümesi 173/173.
- Beklentisi güncellenen eski testler (kasıtlı görünüm değişikliği): `Toplu4VitrinTestleri.Ciz` (katlanırı açar) + `T4_Vitrin_degistirilebilir…` (BASİT 7→6 sütun), `Toplu4SiparisTestleri.T4_PRO_tur_grubu…` (rozet yok), `Toplu4UrunSayacTestleri.T4_Urunler_sinir…` (sade sayaç), `KesifP6EkranTestleri.Siparis_istege_bagli…` (bağlantı metni), `Toplu3PanelBandiTestleri.T3_Liste_yuklenmemisse…` (T6 metni), `ParaDemo3Tur2GiderTestleri.PD3T2_gider_kilidi…` ve `PD3T2_yetersiz_bandi…` (kilit tek cümle; toplamı aşan tutarda bant çıkış yolu), `S33UrunlerEtiketlerEkranTestleri.S33_10…` ve `UX1C1EkranTestleri.Etiketler_sunucu_SINIRINI…` (sınır title'da).
- **Tam süit tur 1** (`trx-pTOPLU-6`): 10063 test · 10058 geçti · **3 başarısız** — üçü de bu paketin (S33_10, UX1C1 Etiketler: sayaç değişikliği; T6_Para_aktarimi_BASIT: yük altında zamanlama). Düzeltildi: iki beklenti güncellendi; virman kilit yeri kural METNİNDEN türetildi (sayı kapısı durumundan bağımsız) ve test son çizimi bekler.
- **Tam süit tur 2** (son kod): 10063 test · 10040 geçti · **21 başarısız** · 2 atlandı. 21 kırmızının TAMAMI has/kâr biçimi alanında: ZOBELGESinirTestleri(+Tur3) 8, RAPORYASLANDIRMA 1, KarRaporKarZarar 1, KarZarar 1, MadenIkiKarZarar 4, MadenIkiPanel 1, P57 RaporEkran 1, P69HasAdlandirma 1, S31PanelSade 2 ("−22,0000 has", "10,4000 has"), Toplu4Panel 1 (has satırı). Sebep: MADEN-2 şeridi tur 1 ile tur 2 arasında `Servisler/Bicim.cs` (04:30), `Panel.razor` (04:29), `KarZarar.razor` (04:32), `Raporlar.razor` (04:31) dosyalarını değiştirdi (has 4 ondalık kararı). Bu paket bu dosyaların has/kâr kodlarına dokunmadı (Panel'de yalnız yasal bant metni, 2 satır). Toplu6 testleri tur 2'de yeşil. **Bu paketin yeni kırmızı farkı: 0.**

## 6. Mutasyon tablosu (47 adet; yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-6/` → TEK eşleşmeli değişiklik → gerekirse derle → filtreli test → geri yükle → `cmp` AYNI)

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M6-1a | dugme.css karanlık medya bloğunda dolgu → --kenar-yuzey | T6_Koyu_secili_esleme…, T6_Koyu_temada_secili_dolgu… | AYNI |
| M6-1b | dugme.css data-tema=karanlik dolgu → --vurgu | T6_Koyu_secili_esleme… | AYNI |
| M6-2 | sade.css şerit seçili → sabit --kenar-yuzey | T6_Kur_ve_doviz_sekme_seridi… | AYNI |
| M6-3 | toplu6.css üst çubuk → --kenar-yuzey | T6_Ust_cubuk_anahtari… | AYNI |
| M6-43 | index.html toplu6.css bağlantısı silindi | T6_index_toplu6_css… | AYNI |
| M6-4 | yasal bant margin-bottom → 0 | T6_Yasal_bant… | AYNI |
| M6-13 | 375 `content: attr(data-etiket)` → none | T6_Vitrin_375… | AYNI |
| M6-46 | Vitrin th:first-child → center | T6_Vitrin_375… | AYNI |
| M6-47 | 375 thead display:none → table-header-group | T6_Vitrin_375… | AYNI |
| M6-16 | Kur başlık margin-top → 0 | T6_Kur_ayar_fiyat_basligi… | AYNI |
| M6-41 | Kim borçlu min-width → auto | T6_Kim_borclu… | AYNI |
| M6-28 | Sipariş tetik rengi → --metin | T6_Siparis_istege_bagli_tetik… | AYNI |
| M6-39 | alan kilit → 12px | T6_Alan_kilit… | AYNI |
| M6-40 | Çıktı 375 order -1 → 0 | T6_Cikti_seridi… | AYNI |
| M6-7 | Panel bekleyen → Yasal3 "Başka iş yok" | T6_Panel_bekleyen… | AYNI |
| M6-8 | Uyum risk sekmesi tekrar "+ Yeni" | T6_Uyum_risk… | AYNI |
| M6-9 | Uyum arama Ipucu geri | T6_Uyum_risk… (test güçlendirildi: ilk koşumda `=>` yüzünden regex boş alt dize alıyordu → mutasyon YAKALANMADI; düzeltilip tekrar → kırmızı) | AYNI |
| M6-30 | Ürün etiketleri sayaç → eski kalıp | T6_Urun_etiketleri… | AYNI |
| M6-42 | DV kutusu → dvTutar | T6_Doviz_veresiye… | AYNI |
| M6-5 | Bant metni → Toplu3 "destek hattı" | T6_Bant_cikmaz_sokak… | AYNI |
| M6-44 | Bant bağlantısı → "Ayrıntı" | T6_Bant_cikmaz_sokak… | AYNI |
| M6-6 | ylp-yukleyen silindi | T6_Uyum_liste_paneli… | AYNI |
| M6-10 | Vitrin katlanır hep açık | T6_Vitrin_BASIT_ayar_tablosu… | AYNI |
| M6-11 | Sıra th ProAlan'sız | T6_Vitrin_BASIT_ayar_tablosu… | AYNI |
| M6-12 | Kalem td data-etiket'siz | T6_Vitrin_hucreler_375… | AYNI |
| M6-14 | Boş atlanan kartı BASİT'te zorla | T6_Vitrin_atlanan_yok… | AYNI |
| M6-15 | Milyem th sayi'siz | T6_Kur_sayisal_sutun… (BASİT, PRO) | AYNI |
| M6-17 | Kur notu ⓘ açık | T6_Kur_BASIT_alt_not… | AYNI |
| M6-18 | Vade tarih sütunları BASİT'te | T6_Vade_BASIT_tablo… | AYNI |
| M6-19 | Telefon ham | T6_Vade_telefon… | AYNI |
| M6-20 | "Şimdi gir" yok | T6_Kurulum_eksik_adim… (BASİT, PRO) | AYNI |
| M6-21 | Şimdi gir bölümü açmaz | T6_Kurulum_eksik_adim… | AYNI |
| M6-22 | Etiket → "Açıklama" | T6_Kurulum_alani_olmayan… | AYNI |
| M6-23 | Sayaç boşken kırmızı | T6_Gerekce_sayaci… | AYNI |
| M6-24 | "+ " öneki yok | T6_Siparis_* (2) | AYNI |
| M6-25 | Detay metni → "Daha fazla" | T6_Siparis_sube_satirinda… | AYNI |
| M6-26 | Detay hep kapatılabilir | T6_Siparis_Daha_az_goster… | AYNI |
| M6-27 | Şube rozeti geri | T6_Siparis_sube_satirinda… | AYNI |
| M6-29 | Ürünler sayaç → eski | T4_Urunler_sinir… | AYNI |
| M6-31 | Kategori kilidi → tutar altında | T6_Gider_kategori_yokken… | AYNI |
| M6-32 | Kasa kilidi → tutar altında | T6_Gider_kasa_secilmeden… | AYNI |
| M6-33 | Nakit kilit → eski cümle | T6_Gider_nakit_asimi… | AYNI |
| M6-34 | Kart payı hep | T6_Gider_nakit_asimi… | AYNI |
| M6-35 | Sekme adı → Virman | T6_Para_aktarimi_BASIT… | AYNI |
| M6-36 | Banka yok bandı kapalı | T6_Para_aktarimi_banka_yoksa… | AYNI |
| M6-37 | Hesap yok boş durumu kapalı | T6_Para_aktarimi_tek_hesapta… | AYNI |
| M6-38 | Kaynak kilidi → tutar altında | T6_Para_aktarimi_BASIT… | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata**, 2 uyarı (ikisi de `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` CS8601 — başka şeridin dosyası). Ham sonuçlar: `/private/tmp/kuyumcu-serit/TOPLU-6/mut-sonuc.jsonl`.

## 7. Derleme ve denetim

- `dotnet build Kuyumcu.sln`: 0 hata, 2 uyarı (başka şerit).
- `dotnet/denetim.sh` (son koşum): 302 ✓; ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç) ve ✗ **P-36b GIRDI_AILESI_DISI_BICIM 3** — üçü de `Pages/Raporlar.razor:1814,1824` `ToString("N…")`; dosya 04:31'de MADEN-2 şeridince değiştirildi, bu paket Raporlar.razor'a dokunmadı (önceki iki koşumumda bu satır ✓ idi). Bu paketin sayaçları ✓ (onclick'siz düğme 71/71 — ilk koşumda Siparişler "Daha az göster" düğmesinin `@onclick`'i ikinci satırdaydı, sayaç 72 oldu; aynı satıra alındı).

## 8. DoD sözleşmesi

Yeni varlık/kayıt türü, yeni uç, yeni form YOK. Yeni düğmeler: "Daha az göster" (#sip-f-detay-gizle, gerçek eylem), "Şimdi gir" ve "Banka hesabı ekle" bağlantı (gezinme). Mevcut id'ler korundu (#gd-kaydet-kilit, #gd-v-kaydet-kilit, #kur-pro-notu, #ara-belge-ipucu, #gd-virman-aciklama yer değiştirdi, kimlik aynı). Yeni id'ler: vitrin-ayar-kat(-ac/-sayi/-govde), kur-ayar-fiyat-baslik/-tablo, kur-pro-notu-satir/-ipucu, uy-arama-satir/-ipucu, ylp-yukleyen, gd-virman-baslik-satir/-ipucu, gd-v-banka-yok/-ekle, gd-v-hesap-yok-ekle, sip-f-detay-gizle. Sabit renk/satır içi stil yok (VadeTakibi'ndeki 4 satır içi `style` kaldırıldı).

## 9. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokundukları)

Web: `Pages/Vitrin.razor`, `Pages/Kur.razor`, `Pages/VadeTakibi.razor`, `Pages/Kurulum.razor`, `Pages/Siparisler.razor`, `Pages/Uyum.razor`, `Pages/Gider.razor`, `Pages/Urunler.razor`, `Pages/UrunEtiketleri.razor`, `Pages/Cariler.razor` (DV kutusu, 2 satır), `Pages/Panel.razor` (yalnız yasal bekleyen metni, 2 satır), `Bilesenler/Uyum/UyumYasalBantlari.razor`, `Bilesenler/Uyum/YasalListePaneli.razor`, `Bilesenler/GerekceAlani.razor` (ortak sayaç — Kurulum maddesi; boşken nötr), `wwwroot/css/toplu6.css` (yeni), `wwwroot/css/dugme.css`, `wwwroot/css/sade.css`, `wwwroot/index.html`.
Core: `I18n/Sozlukler.Toplu6.cs` (yeni, TR+EN; tur 2'de +5 anahtar).
Testler: `Toplu6Testleri.cs` (yeni), `Toplu3Testleri.cs`, `Toplu4Testleri.cs`, `KesifP6EkranTestleri.cs`, `ParaDemo3Testleri.cs`, `S33UrunlerEtiketlerEkranTestleri.cs`, `UX1C1EkranTestleri.cs`, tur 2: `QA10KritikTestleri.cs` (1 beklenti).
Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Vitrin, Kur, VadeTakibi, Kurulum, Siparisler, Uyum, Gider, Urunler, UrunEtiketleri, Cariler, Panel. Cikti.razor'a dokunulmadı (yalnız CSS).

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Madde tablosu önce/sonra/kanıt | Karşılandı | § 1 |
| Başsız Chrome 1440/1280/375 · BASİT+PRO · açık+koyu, Read | Karşılandı | `son2/` 240 + `son3/` 60; § 3 |
| Koyu seçili sekme kontrastı ≥ 3:1 ölçülü | Karşılandı | 14,46–16,25 (önce 1,04–1,36) |
| Her değişiklik için test + mutasyon | Karşılandı | 44 koşum, 47 mutasyon hepsi kırmızı, cmp AYNI |
| Build 0 hata | Karşılandı (2 uyarı başka şeridin dosyasında) | build8 |
| denetim TEMİZ (P-30 hariç) | Bu paketin sayaçları ✓; P-36b ✗ MADEN-2'nin Raporlar.razor değişikliği | § 7 |
| Tam süit 0 yeni kırmızı | Karşılandı (fark 0) | § 5 |
| Canlı API + SQL | Karşılandı | § 4 |
| Test kiracısı silindi, tenants 15→15 | Karşılandı | § 4 |
| Para/veri/yasal davranışa dokunma | Karşılandı | kural/kapı/uç değişmedi |
| COMMIT YOK, dosya silme yok, sır basılmadı | Karşılandı | — |

## 11. Açık kalanlar (sahibiyle)

- **Tur 2 — BASİT üst bar başlığı ve menü adı "Gider ve virman"** (hakemin KRİTİK bulgusunun dosya alanı dışındaki kısmı): `Sozlukler.Menu.cs`/`RotaBasliklari.cs`/`MenuTanimi.cs` — orkestratörün YAZILI İZNİ ve tuvalden sapma için kurucu onayı gerekir (öneri § 0).
- Tur 2 tam süit kararsız kırmızısı `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` — İşlemler/ödeme aracı sahibi (tek başına 6/6 yeşil).
- Tur 2 yan not: BASİT geri alma bandında QA10 `VirmanBacakSayisi` PRO'ya özgü kaldı; BASİT `Storno` düğme etiketi "Geri al (düzeltme fişi)" olarak zaten sade.

- Tam süit 21 kırmızı ve denetim P-36b (Raporlar.razor:1814,1824) — **MADEN-2** (Bicim.cs/Panel/KarZarar/Raporlar has-kâr biçimi değişikliği, 03.10 04:29–04:33).
- P-30 DEMO_INSAN_SURESI — kronometreli demo ölçümü (kurucu/orkestratör).
- § 2'deki devirler: has ondalığı (MADEN-2), Panel kur kartı başlığı, bölünmüş ödeme (DEGISIM-GUNSONU), basılı ekstre notu/başlığı, Kasalar'da "Para aktarımı" adı, UX-S29 ↔ sade sayaç kararı (orkestratör).
- `ham/TOPLU-6-olcum/son/` önbellek nedeniyle geçersiz ölçümdür (silinmedi — dosya silme yasak); geçerli olan `son2/` ve `son3/`.

## 12. Kurucu / mali müşavir

Mevzuat hükmü içeren değişiklik yok. Yasal liste bandı yalnız dil değiştirdi; listenin kimin tarafından yüklendiği kaynak koddan doğrulandı (`SistemModulu` POST /sistem/yaptirim-listesi, sistem anahtarı). Mali müşavire soru yok.
