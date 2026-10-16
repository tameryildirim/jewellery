# PAKET PARA-DEMO-2 — uygulama raporu

**Konu:** PARA-DEMO tasarım hakemi t2 bulguları ve açık kalanlar (tek bacaklı ödeme KARMA, Günü Kapat sahte açık, nakit çıkış kapısı, kasa kartında nakit/kart ayrımı, gün sonu tahsilat tanımı ve 8 ekran maddesi).
**Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · şerit t2 (API 5162, vekil 5163, CDP 9907, test DB `kuyumcu_test_t2`)
**Tarih:** 02.10.2026 · **Durum:** BİTTİ (tur 5 — doğrulayıcının ikinci turdaki 5 bulgusu kapatıldı: Panel nakit ayrımı, tahsilat tutarı, değişimde tek kimlik, BASİT kilit sebebi, elle kart/havale çıkış kapısı; 7 maddelik paket kapısı sağlandı; ayrıntı § 0c)

## 0. Devralma — iş neden durdu, nereden sürdü

- **07:47:** ilk uygulayıcı internet kesintisi (ENOTFOUND) yüzünden düştü. Kod ve 30 mutasyon o noktada hazırdı, rapor yazılmamıştı.
- **09:31:** makine yeniden başladı ve `/private/tmp` silindi. İkinci uygulayıcının tur 2 işi (FisVerdimAldim seçici satırı, `tuval-satis.css`, TS1 testi) ağaçta kaldı. Ölçüm, fotoğraf ve canlı kanıtların bir kısmı daha önce `ham/PARA-DEMO-2-olcum/` altına kopyalanmıştı.
- **Bu tur (tur 3) şunları yaptı:**
  - Yarım kalmış mutasyon olup olmadığını denetledi.
  - Derlemeyi ve tam süiti koştu, `denetim.sh` çalıştırdı.
  - Canlı kanıtları yeni bir test kiracısında **yeniden üretti**.
  - Canlı koşumda bulunan bir kusuru düzeltti: nakit kapısı mesajı "1000.00 TRY" yazıyordu (§ 2, madde 3b).
  - Test kiracılarını sildi ve bu raporu yazdı.
- **Yarım mutasyon denetimi:**
  - `ham/mut-yedek-PARA-DEMO-2/` altındaki 30 yedeğin 24'ü güncel kaynakla **birebir aynı** (`cmp`).
  - Kalan 6 fark (Cariler.razor ×4, CiktiModulu.cs ×2) mutasyon artığı değil. Hepsi mutasyondan **sonra** yapılmış bilinçli düzeltmeler:
    - Cariler: `#th-guncel-altin` alanına `hucre-tek-satir` sınıfı.
    - CiktiModulu: kâğıt ekstrede bakiye hücresi TL kalır. 1280 px'te "Satış · Veresiye" iki satıra kırılıyordu (kaynakta gerekçe yorumu var).
  - Kaynakta `if (false` / `@if (true)` / `=> null; // MUT` izi **yok**.
- **Bekleyen süreç:** 5162, 5163 ve 9900–9920 portlarında kalmış süreç yoktu (`lsof` boş).

## 0b. Tur 4 — doğrulayıcı bulgularının kapanışı (02.10, ~11:20–12:10)

| # | Bulgu (doğrulayıcı) | Ne yapıldı | Kanıt |
|---|---|---|---|
| K1 | **[KRİTİK] Gider yolunda nakit kapısı yok** (`GiderModulu.cs:335` yalnız toplam kapısı; nakit 1000 + kart 500 iken 1200 gider 200 dönüyordu, nakit payı −200) | `Kasa.KasaBakiyeKapisi.Dogrula(kasa, -tutar)` → `await Kasa.KasaBakiyeKapisi.NakitDogrula(db, islem.TenantId, kasa, -tutar, OdemeAraci.Nakit)` (gider satırı her zaman `OdemeTuru="NAKIT"`). `NakitDogrula` önce toplam kapısını uygular, sonra nakit payına bakar. Gün kilidi (`GunKapanisiKapisi`) sırası aynen korundu: kapılardan sonra, işlemden önce | Test `PD2_3_nakit_1000_kart_500_iken_1200_NAKIT_GIDER_400_tam_nakit_200` yeşil; mutasyon M3-8 kırmızı. Canlı: 1200 → **400**, 1000,01 → **400**, 1000 → **200** (kasaBakiyesi 500); nakit-ayrımı `nakit 0 · kart 500`. SQL: gider kasasında tek gider satırı `NAKIT −1000`; reddedilen istekler satır yazmadı. Ekranda (Gider, BASİT/PRO) hata bandı: "çekmecede 1.000,00 ₺ nakit var, 1.200,00 ₺ nakit isteniyor…" |
| K2 | **[ORTA] Elle DIGER girişi "Açılış devri" sayılıyordu** (`RaporModulu.cs:435` `OdemeTuru == "DIGER"`) | Açılış devri artık **ödeme türüyle değil, yazan kaydın kimliğiyle** ayrılır: `hareket_id` üzerinden `Tur="ACILIS"` hareketine bağlı kasa girişleri (AcilisServisi böyle yazar). Ayrı bir toplama sorgusu kuruldu (Join; storno çifti dışlanır; şube kapsamı aynı). Tahsilat kırılımı artık DIGER'i dışlamıyor; açılış devri tutarı yalnız `DIGER` kaleminden düşülüyor. **Ekip tanımı:** elle DIGER girişi tahsilatın "diğer" kaleminde kalır | Test `PD2_5_elle_DIGER_girisi_acilis_devri_DEGIL_diger_kaleminde_kalir`, doğrulayıcının S11'iyle aynı iddia (elle DIGER 250 → acilisDevri 0, diger 250). `PD2_5_gun_sonu_…` açılış devrini artık GERÇEK `/kurulum/acilis/prova→uygula` ucuyla kuruyor. Mutasyon M5-4/M5-5/M5-6 kırmızı. Canlı: elle DIGER 250 → `diger 250, acilisDevri 0`. Ardından kurulum açılışı 400 → `acilisDevri 400, diger 250`, toplam değişmedi (15.250). SQL: açılış satırı `DIGER 400 · bağlı · ACILIS`, elle satır `DIGER 250 · bağsız` |
| K3 | **[DÜŞÜK] Mesajlarda "1000.00 TRY"** (toplam kapısı `Mesaj` ve virman kart/havale payı aşımı) | Virman kart/havale payı aşımı mesajı artık `KasaBakiyeKapisi.ParaMetni` ile: "kart tahsilatı 500,00 ₺; 600,00 ₺ aktarılamaz." **Toplam kapısı `Mesaj` BİLİNÇLİ olarak değiştirilmedi** (gerekçeli sapma, § 2). Bu cümle sunucu→istemci sözleşmesidir: `KasaBakiyeMesaji.Bicimle` (İşlemler ödeme, Hurda) ve `Gider.razor YetersizMetni` sayıları invariant ayrıştırıp ekrana tr-TR/₺ ve İngilizce olarak yeniden yazar (`Toplu1Testleri.Kasa_bakiye_reddi_tr_TR_bicimli_TRY_yok`). Biçimi değiştirmek ayrıştırıcıyı kırar, İngilizce arayüzde de ham Türkçe cümle bırakırdı. Kaynakta gerekçe yorumu var | Virman testi iddiası; mutasyon M3-9 kırmızı. Canlı: KART virman 600 → 400, mesaj "500,00 ₺; 600,00 ₺" |
| K4 | **[DÜŞÜK] Gün sonu `tanim` API'de yalnız Türkçe** | `tanim` alanı API yanıtından **kaldırıldı**; API yalnız sayıları döner. Metin istemci sözlüğünde: `ParaDemo2Metinleri.TahsilatTanimMetni`, TR+EN (`Sozlukler.ParaDemo2.cs`). `MutabakatTahsilat` kaydından `Tanim` çıkarıldı. Tanım katlanırı her zaman çizilir; ayrı satırlar kartı yalnız virman/açılış tutarı varsa | `PD2_5_gun_sonu_…`: `tahsilat.tanim` YOK. bUnit `PD2E_5_…` katlanırı açıp metnin `ParaDemo2Sozlugu.Tr.TahsilatTanimMetni` olduğunu doğrular; mutasyon M5-7 kırmızı. `PD2_sozluk_TR_ve_EN_dolu_ve_farkli` yeni anahtarı da sınar. Canlı yanıtta `tanim` yok |
| K5 | **[ORTA] Raporda eksik/yanlış beyan** (gider yolu sınanmamıştı; DIGER tanımı yanlıştı) | Bu bölüm eklendi; § 1 madde 3/5, § 2, § 5, § 7, § 11 ve § 13 düzeltildi | — |
| (ek) | Ekranda bulundu: gün sonunda "tahsilata girmeyen" satırlar kartın kenarına yapışıktı (dolgu yok; tur 3 fotoğrafında da görünür) | Satırlar `kart-govde` içine alındı; tanım katlanırı kartın dışına çıkarıldı (iç içe kart yok) | Foto `tur4/foto/gunsonu-tanim__PRO_aydinlik_1280.png` |

## 0c. Tur 5 — doğrulayıcının ikinci tur bulgularının kapanışı (02.10, ~13:05–14:00)

Şerit t2 (API 5162, vekil 5163, CDP 9908, `kuyumcu_test_t2`). Başlangıçta 5162/5163/9900–9920'de kalmış süreç yoktu. Kanıtlar: `ham/PARA-DEMO-2-olcum/tur5/` (88 fotoğraf, `olcum6.jsonl`, `olcum7.jsonl`, `canli6.txt`, `sql-canli6.txt`, `mut/sonuc.txt`).

| # | Bulgu (doğrulayıcı) | Ne yapıldı | Kanıt |
|---|---|---|---|
| T5-1 | **[KRİTİK] Panel "Kasada ne var?" kasanın tamamını "Nakit" yazıyordu** (`Panel.razor` `@T.Nakit @Bicim.Para(tl.Toplam…)`) | Panel BASİT kartı artık Kasalar ve Rapor merkeziyle **aynı uçtan** okur: `GET /kasalar/nakit-ayrimi` (şubeli, paralel yükleyici `YukleNakitAyrimi`, `Yuklemeler.AddRange` içinde; şube değişince yeniden). Alt satır: `Nakit X` (`#panel-soru-kasa-grup`) + `Kart/Havale Y` (`#panel-soru-kasa-karthavale`, yalnız ≠ 0 ise) + Banka. Ayrım okunamazsa toplam **"Nakit" diye basılmaz**: "TL kasalar X" yazar, altında kısa hata (`#panel-soru-kasa-ayrim-hata`). Büyük sayı (kasanın tamamı) değişmedi. PRO panelde "Kasada ne var?" kartı yok (kasa adlarıyla bakiye listesi; "Nakit" etiketi taşımaz) | bUnit `PD2E_4_Panel_Kasada_ne_var_alt_satiri_Nakit_ve_Kart_Havale_AYNI_ayrim`, `PD2E_4_Panel_ayrim_okunamazsa_toplam_NAKIT_diye_basilmaz_kisa_hata`. Mutasyon M4-4, M4-5 kırmızı. **Canlı (aynı kiracı, aynı an):** API `nakit 9000 · kart 8000 · havale −2496` = SQL (`bakiye 14504 − kart 8000 − havale −2496 = nakit 9000`); Panel `Nakit 9.000 ₺ · Kart/Havale 5.504 ₺`, Kasalar `Nakit 9.000 ₺ · Kart/Havale 5.504 ₺`, Rapor merkezi `Nakit 9.000 ₺ · Kart/Havale 5.504 ₺` — 1440/1280/375 × açık/koyu, 3 ekran aynı sayı (`olcum6.jsonl`, foto `panel__Basit_*`, `kasalar__*`, `raporlar__*`) |
| T5-2 | **[YÜKSEK] Tahsilat Tutar "isteğe bağlı" yazıp 0 dolu geliyor, açılışta "Eksik olan: İşlem tutarı 0'dan büyük olmalı"** | Tutar kutusu **boş** gelir (0 yazmaz). Altın açık değilken **zorunlu** (*); "isteğe bağlı" yazmaz. Kural tek cümle, sözlükten: "Tutar ya da altın girin." Yalnız **ilk Kaydet denemesinden sonra**, tutar alanının altında görünür. Boş Kaydet istek atmaz (DoD EK md.3: sessiz değil, alan altı mesaj). Kaydet boş tutar yüzünden kilitlenmez; kasa eksik ve nakit sınırı kilitleri aynen | bUnit `PD2E_7_tahsilat_Tutar_BOS_zorunlu_hata_YALNIZ_Kaydet_denemesinden_sonra` (BASİT+PRO), eski `CariEkranTestleri.Tutarsiz_tahsilat_kaydedilemez` yeni sözleşmeye göre güncellendi (boş deneme → POST yok + alan altı cümle). Mutasyon M7-3/M7-4/M7-5 kırmızı. Chrome 12 kombinasyon: açılışta `deger="" · hata yok · kilit yok · Kaydet açık`; boş denemede `"Tutar ya da altın girin."`, `/tahsilat` isteği 0 (foto `tahsilat-acilis__*`, `tahsilat-bos-deneme__*`) |
| T5-3 | **[YÜKSEK] Değişimde TCKN ve adres iki blokta iki kez soruluyordu, adres birinde zorunlu birinde isteğe bağlı** | Hurda Alış'taki "tek kimlik formu" kuralı (`Islemler.Satis4.cs KimlikBirlesik`) **değişime de** uygulandı: MASAK kimlik kutusu açıkken vatandaş satıcının adı, TCKN'si ve adresi **yalnız** MASAK kutusunda sorulur; satıcı bloğunda "…aşağıdaki müşteri kimliği kutusundan alınır — ikinci kez yazılmaz." notu. Değerler her çizimde satıcı gövdesine eşitlenir (VUK md.234 gider pusulası alanları aynen gider). Belge türü TC kimlik değilse satıcı TCKN'si satıcı bloğunda sorulur (uydurulmaz). Zorunluluk tek kural: `MasakFormu` yeni `AdresZorunlu` parametresiyle adres **zorunlu** (*) görünür (varsayılan false → diğer ekranlar aynen). MASAK kutusu açık değilse satıcı bloğu kendi alanlarını bir kez sorar (önceki gibi) | bUnit `PD2E_8_takas_MASAK_kutusu_acikken_TCKN_ve_adres_TEK_kez_kilit_alan_adiyla_GORUNUR` (BASİT+PRO; gövdede `satici.tckn = masak.belgeNo`). Mutasyon M8-4, M8-6 kırmızı. Chrome 12 kombinasyon: `saticiTckn=false saticiAdres=false masak=true adresZorunlu=true` (foto `takas-tek-kimlik__*`). **Canlı uçtan uca:** BASİT 1440'ta MASAK kutusuna TCKN + adres yazılıp kaydedildi → `hareketler TAKAS 17.207,24` + `gider_pusulalari GP-2026-000003 NIHAI_TUKETICI · satici_tckn var · hash var · adres var · 40 g` (`sql-canli6.txt`) |
| T5-4 | **[YÜKSEK] BASİT Değişimde kilit sebebi görünmez (`gorunmez`), metin eksik alana göre değişmiyordu** | `KaydetKilitSebebi` sırasında değişimin satıcı kimliği eksiği **genel yasal cümleden önce** gelir. Genel cümle ("Kimlik bilgilerini girin ve ödemeyi …") BASİT üst bantla aynı olduğu için Kaydet yanında gizleniyordu; alan adını taşıyan sebep banttan farklıdır → iki modda da görünür. BASİT: "Eksik: TCKN, adres." → TCKN girilince "Eksik: adres." PRO: "Satıcının eksik bilgisi: TCKN, adres — gider pusulası için gerekli (VUK md.234)." Kimlik tamamlanınca sıradaki engel yazılır (ör. MASAK işlem amacı) | bUnit aynı test (`gorunmez` sınıfı yok; metin adım adım değişir). Mutasyon M8-5 kırmızı. Chrome: 12 kombinasyonda `kilitSinif` `gorunmez` içermez, boy > 1×1 (1440: 832×19, 375 PRO: 351×38); canlı: "Eksik: TCKN, adres." → "Eksik: adres." (foto `takas-kilit__*`, `takas-masak-kilit__Basit_aydinlik_1440`) |
| T5-5 | **[ORTA] Elle KART/HAVALE kasa çıkışı kendi payına bakmıyordu** (doğrulayıcı `S16_elle_kart_cikisi_kart_payini_asamaz_1200_400` kırmızıydı) | `KasaBakiyeKapisi.NakitDogrula` yeni `aracPayiKapisi` parametresi: true iken KART çıkışı ≤ kart payı, HAVALE çıkışı ≤ havale payı (virmandaki kural tek merkezde; mesaj tr-TR ₺: "'X' kasasındaki kart tahsilatı 500,00 ₺; 1.200,00 ₺ kartla çıkarılamaz."). **Elle kasa çıkışı** (`KasaModulu.HareketGir`) bu kapıyı açar. `PD2_3_kart_cikisi_…TABI_DEGIL` testi kaldırılıp yerine `PD2_3_kart_cikisi_KART_PAYINI_asamaz_1200_400_500_gecer_havale_payi_0` yazıldı. **Sapma (gerekçeli):** belgeli yollarda (alış, iade, takas farkı, hurda alışı — `SatisModulu.KasaGuncelle`) kart/havale çıkışı yine yalnız toplam kapısına tabi. Önce tüm yollara uygulandı; 10 meşru test kırıldı (tedarikçiye havaleyle ödenen alış, kartlı iade, müşteriye kartla ödenen takas farkı — `IadeTestleri` ×5, `L1bH_04`, `NakitPayiYazmaTestleri.Takas_farki_KARTLA…`, `ZOP23`, `D3a`, `Eksi_farkli_takas`, `ZOSR20`). Bu yollarda para çekmeceden çıkmaz (POS/banka), **nakit payı değişmez → gün sonunda sahte açık oluşmaz**; doğrulayıcının "çekmecede 300 varken 700 sahte açık" önermesi bu yollar için geçerli değil (kart çıkışı çekmeceden para almaz, çekmecede 1000 nakit kalır, defter de 1000 der). Elle çıkışta ise karşılık belge yok; kapı orada ayrımın eksiye düşmesini engeller | Test yeşil; **S16 yeşil**. Mutasyon M3-10, M3-11 kırmızı (iki test). **Canlı (nakit 1000 + kart 500):** KART 1200 → **400**, HAVALE 1 → **400**, KART 500,01 → **400**, KART 500 → **200**, KART 0,01 → **400**, NAKİT 1000,01 → **400**, NAKİT 1000 → **200**. SQL: kasada yalnız 4 satır (2 giriş + KART −500 + NAKİT −1000); reddedilenler satır yazmadı. Belgeli yol: havale payı 3000 iken ~4.580 ₺ HAVALE hurda alışı **200** (toplam kapısı yeterli), havale payı −2.496, nakit 9.000 değişmedi |

**Ek koşullar:** `Islemler.razor` 1399 satır (S21 sınırı ≤ 1400 — ilk denemede 1404 olmuştu, yorum satırları sıkıştırıldı). Ölçer: Panel/Kasalar/Raporlar ve Değişim 48 kombinasyonda K1/K3/K5/K7/K8/K9/K10/K11/yatay = 0. Tahsilat penceresinde K9 2–6 ve 375'te K3 kırık 1: hepsi modalın **arkasında kalan** liste öğeleri (`#cari-segment-hepsi ⟂ #modal…`) ve modal başlığı "Tahsilat — Kemal Ardıç"ın 375'te iki satıra inmesi (başlık, kısa etiket değil); ölçerin modal örtüsünü ayırmamasından, bu turda değişmedi (fotoğrafla görüldü).

### Tur 5 mutasyon tablosu (yedekler `ham/mut-yedek-PARA-DEMO-2/*-t5__*`, hepsi geri alındıktan sonra `cmp` AYNI, son build 0 uyarı 0 hata)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M3-10 | `KasaBakiyeKapisi`: araç payı koşulu `if (false && …)` | `PARADEMO2SinirTestleri.S16_elle_kart_cikisi_kart_payini_asamaz_1200_400`, `ParaDemo2Testleri.PD2_3_kart_cikisi_KART_PAYINI_asamaz_1200_400_500_gecer_havale_payi_0` | AYNI |
| M3-11 | `KasaModulu`: `aracPayiKapisi: true → false` | aynı iki test | AYNI |
| M4-4 | Panel: `ay.Nakit → tl.Toplam` | `PD2E_4_Panel_Kasada_ne_var_alt_satiri_Nakit_ve_Kart_Havale_AYNI_ayrim` | AYNI |
| M4-5 | Panel: `YukleNakitAyrimi()` yükleyiciden çıkarıldı | `PD2E_4_Panel_…AYNI_ayrim`, `PD2E_4_Panel_ayrim_okunamazsa_…` + S31 iki test (4 kırmızı) | AYNI |
| M7-3 | Cariler: hata `thDenendi &&` olmadan (açılışta görünür) | `PD2E_7_tahsilat_Tutar_BOS_…` (BASİT, PRO) | AYNI |
| M7-4 | Cariler: boş denemede `return` kaldırıldı (POST gider) | `CariEkranTestleri.Tutarsiz_tahsilat_kaydedilemez`, `PD2E_7_…` (BASİT, PRO) | AYNI |
| M7-5 | Cariler: `Deger="@thTutar"` (0 görünür) | `PD2E_7_…` (BASİT, PRO) | AYNI |
| M8-4 | Satis4: `KimlikBirlesik` değişim koşulu `false` | `PD2E_8_takas_MASAK_kutusu_acikken_…` (BASİT, PRO) | AYNI |
| M8-5 | Islemler: kilit sırasında değişim kimlik eksiği kaldırıldı | `PD2E_8_takas_MASAK_kutusu_acikken_…` (BASİT, PRO) | AYNI |
| M8-6 | MasakFormu: adres `Zorunlu="@AdresZorunlu"` kaldırıldı | `PD2E_8_takas_MASAK_kutusu_acikken_…` (BASİT, PRO) | AYNI |

### Tur 5 dokunulan dosyalar
`Kuyumcu.Api/Modules/Kasa/KasaBakiyeKapisi.cs`, `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` (tek satır), `Kuyumcu.Web/Pages/Panel.razor` (**yalnız "Kasada ne var?" kartı** + yükleyicisi), `Kuyumcu.Web/Pages/Cariler.razor` (tahsilat çekmecesi), `Kuyumcu.Web/Pages/Islemler.razor` (iki parametre + kilit sırası, net satır sayısı aynı), `Kuyumcu.Web/Pages/Islemler.Satis4.cs`, `Kuyumcu.Web/Bilesenler/Fis/FisTakasSatici.razor`, `Kuyumcu.Core/I18n/Sozlukler.ParaDemo2.cs` (2 anahtar TR+EN: `KartHavaleEtiket`, `TutarYaDaAltin`), testler `ParaDemo2Testleri.cs`, `ParaDemo2EkranTestleri.cs` (3 yeni sınıf/test), `S31PanelSadeTestleri.cs` (sahte API'ye yeni uç için gerçekçi yanıt — davranış iddiası değişmedi), `CariEkranTestleri.cs` (bir testin sözleşmesi güncellendi).
**Alan dışı dokunuş (gerekçeli):** `Kuyumcu.Web/Bilesenler/MasakFormu.razor` — yalnız varsayılanı `false` olan `AdresZorunlu` parametresi (2 satır + belge yorumu). Doğrulayıcının "zorunluluk tek kurala bağlansın" koşulu bu bileşenin etiketi değişmeden karşılanamıyordu; diğer kullanımlar parametreyi vermez → davranış aynen. İşlemler ekranında parametre `KimlikBirlesik && BirlesikSatici.AdresZorunlu` ile verilir: Hurda Alış'ın tek kimlik formunda da adres artık "isteğe bağlı" değil zorunlu görünür (kilit zaten adres istiyordu — aynı kuralın etiketi). Eşzamanlı şeritlerin (TOPLU-4, YASAL-LISTE-DEMO) alanında değil.

### Tur 5 tarayıcı ölçüm tablosu (başsız Chrome, CDP 9908, vekil 5163; `sikisma-olc.js`; fotoğraflar `ham/PARA-DEMO-2-olcum/tur5/foto/<ekran>__<Mod>_<tema>_<genişlik>.png`, Read ile görüldü: panel BASİT 1440 açık, 375 koyu; tahsilat boş deneme 1440 açık, 375 PRO koyu; değişim tek kimlik/kilit 1440 BASİT, 1280 PRO, 375 PRO koyu)

| Ekran | Genişlik | Mod | açık K1/K3taşan/K3kırık/K5/K9/yatay | koyu K1/K3taşan/K3kırık/K5/K9/yatay |
|---|---|---|---|---|
| panel | 1440 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 1440 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 1440 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| panel | 1280 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 1280 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 1280 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| panel | 375 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 375 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 375 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| panel | 1440 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 1440 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 1440 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| panel | 1280 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 1280 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 1280 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| panel | 375 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| kasalar | 375 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| raporlar | 375 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 1440 | Basit | 0/0/0/0/2/0 | 0/0/0/0/2/0 |
| tahsilat-bos-deneme | 1440 | Basit | 0/0/0/0/2/0 | 0/0/0/0/2/0 |
| takas-tek-kimlik | 1440 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 1280 | Basit | 0/0/0/0/5/0 | 0/0/0/0/5/0 |
| tahsilat-bos-deneme | 1280 | Basit | 0/0/0/0/6/0 | 0/0/0/0/6/0 |
| takas-tek-kimlik | 1280 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 375 | Basit | 0/0/1/0/6/0 | 0/0/1/0/6/0 |
| tahsilat-bos-deneme | 375 | Basit | 0/0/1/0/6/0 | 0/0/1/0/6/0 |
| takas-tek-kimlik | 375 | Basit | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 1440 | Profesyonel | 0/0/0/0/6/0 | 0/0/0/0/6/0 |
| tahsilat-bos-deneme | 1440 | Profesyonel | 0/0/0/0/6/0 | 0/0/0/0/6/0 |
| takas-tek-kimlik | 1440 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 1280 | Profesyonel | 0/0/0/0/6/0 | 0/0/0/0/6/0 |
| tahsilat-bos-deneme | 1280 | Profesyonel | 0/0/0/0/6/0 | 0/0/0/0/6/0 |
| takas-tek-kimlik | 1280 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |
| tahsilat-acilis | 375 | Profesyonel | 0/0/1/0/6/0 | 0/0/1/0/6/0 |
| tahsilat-bos-deneme | 375 | Profesyonel | 0/0/1/0/6/0 | 0/0/1/0/6/0 |
| takas-tek-kimlik | 375 | Profesyonel | 0/0/0/0/0/0 | 0/0/0/0/0/0 |

PRO Panel'de "Kasada ne var?" kartı yok, PRO Rapor merkezinde "bugün" kartı yok (ölçüm yine 0). Tahsilat satırlarındaki K9/K3 kırık: § 0c ek koşullar (modal arkası, başlık).

### Tur 5 derleme · testler · denetim · temizlik
- `dotnet build Kuyumcu.sln`: **0 hata**; 2 uyarı başka şeridin dosyalarında (`SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029 — tur 4'te de vardı). Bu turda kendi testimde çıkan xUnit2031 uyarısı düzeltildi.
- Filtreli koşumlar: ParaDemo2/PARADEMO2/S31Panel/ParaDemo **182/182** (PD2_3 ilk denemede ham JSON aranıyordu, gövde ayrıştırılarak düzeltildi); ilgili geniş filtre (Panel, Cari, Takas, Masak, Hurda, Iade, Kasa, Islem, L1b, ZOISLEMA, YASAL3, NakitPayi, S21 …) **2526/2528** → iki kırmızı düzeltildi (S21 satır sınırı, `Tutarsiz_tahsilat` sözleşmesi) → yeşil.
- **Tam süit (bir kez, son kodla):** 9313 test · **9309 geçti · 2 kırmızı · 2 atlandı** (5 dk 14 sn, trx `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-2-t5`). Devir listesindeki (tur 4) iki kırmızı (`ParaDemoTur2Testleri.PD6_…`, `SATIS3…DEBOUNCE`) bu koşumda **yeşil**. Yeni iki kırmızı bu paketin değil:
  - `PatchSozlesmeKolonTavaniTestleri.PATCH_siparis_kolon_tavani_400_alan_siparis_DEGISMEZ(…"1e30")` — 500 Internal server error; sipariş PATCH ucu, PATCH-SOZLESME şeridinin o sırada koşan işi (aynı ağaçta `dotnet test --filter …PatchSozlesmeKolonTavani…` süreci gözlendi). Tek başına yeniden koşumda başka bir satırı (`adet`) kırmızı: şerit yarım. Bu paketin dosyalarına dokunmuyor.
  - `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12_yeniden_duzenle_TEK_gecerli_pusula…` — 11 eşzamanlı isteğin birinde 500; **tek başına yeniden koşumda yeşil** (kararsız, sahibi YASAL-4).
  - → Paket kapısı (7): **bu paketin yeni kırmızısı 0.**
- `bash denetim.sh`: tek ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç tutulan). Diğer tüm sayaçlar ✓.
- Test kiracısı `PAKETPARA-DEMO-2-T6-TEST` (`01M3Y2KD0P24DRQ9CNQT0KAH1Q`) `zsh kiraci-temizle.sh --uygula` ile silindi; yeniden sorgu: "Kiracı YOK" ve `tenants where id=…` = 0. `tenants` başta 16, sonda 15 (aradaki fark eşzamanlı şeritlerin kendi kiracılarını silmesi; QA-TEST-* dokunulmadı).
- Süreçler: kendi API (5162), vekil (5163), Chrome (9908) `kill` ile durduruldu; `lsof` üçü için boş. Fikstür ve sistem anahtarı dosyaları `printf ''` ile boşaltıldı (sistem anahtarı bu tur için üretilmiş rastgele değerdi, ekrana basılmadı).

## 1. Madde madde — önce/sonra

| # | Madde | Önce (hakem t2 / ölçüm) | Sonra (kanıt) | Durum |
|---|---|---|---|---|
| 1 | Tek bacaklı Kart/Havale = KARMA | `hareketler.odeme_turu=KARMA`; ekstrede "Karma ödeme", gün sonunda "Karışık" | `OdemeSepeti` tek bacağı o bacağın türüne indirir; KARMA yalnız ≥2 farklı araçta. Canlı: tek HAVALE fişi → `HAVALE`, ekstre `odemeTuru=HAVALE`, kâğıt "Satış · Havale". Gerçek karma: `KARMA` + `odemeAraclari=["KART","HAVALE"]`, kâğıt "Satış · Kart + Havale". BASİT tek ödeme satırında Nakit/Kart/Havale seçici var; "Tek ödemeye dön" seçili aracı korur | KAPANDI |
| 2 | Günü Kapat sahte fark | "Sayılan nakit (isteğe bağlı)" 0 dolu, kırmızı sahte fark | Kutu zorunlu (*) ve boş. Fark ve gerekçe yalnız sayı girilince görünür. Kilit sebebi tek kez yazar. Birincil düğme altlıkta Vazgeç'in yanında (foto `gunkapat__*`, ölçüm `sayilan="" fark=false`) | KAPANDI |
| 3 | Nakit çıkış kapısı | Hurda alış, iade, elle çıkış, NAKİT virman ve **gider** kasanın toplam bakiyesine bakıyordu | `KasaBakiyeKapisi.NakitDogrula` → `KasaNakitAyrimi.Nakit`, beş yolun hepsinde (gider tur 4'te eklendi, doğrulayıcı bulgusu). Canlı (nakit 1000 + kart 500): 1200 nakit hurda alış **400**, elle çıkış 1200 **400**, NAKİT virman 1200 **400**, **gider 1200 400 · gider 1000,01 400 · gider 1000 200**, KART virman 300 **200**, 1000,01 **400**, tam 1000 **200**; hurda satırı **0**. Toplam kapısını kullanan tek diğer çağrı `AcilisServisi` (açılış geri alma). Orada para el değiştirmez (DIGER), bu yüzden nakit kapısı bilinçli olarak uygulanmaz | KAPANDI (gider yolu tur 4'te) |
| 3b | (tur 3, canlıda bulundu) nakit kapısı mesajı | "çekmecede 1000.00 TRY nakit var…" (ölçer K5 / UX-S32 ihlali, hata bandında görünür) | "çekmecede 1.000,00 ₺ nakit var, 1.200,00 ₺ nakit isteniyor. Kasadaki kart (500,00 ₺)…". 8 kombinasyonda K5=0 (foto `tur3/kasa-nakit-hata__*`) | KAPANDI |
| 4 | Kasa kartı / "Kasada ne var?" ayrımı | Yalnız toplam vardı | Alt satır "Nakit X · Kart/Havale Y" (Kasalar ve Rapor merkezi). BASİT gün sonu kartında tahsilat kırılımı tek satır. Uç: `GET /kasalar/nakit-ayrimi` | KAPANDI |
| 5 | Gün sonu "TOPLAM TAHSİLAT" tanımı | Açılış devri ve virman girişi toplama giriyordu; uyarılarda "TL" yazıyordu | Toplam = nakit + kart + havale + diğer (kasaya elle girişler dahil). `virmanGiris` (karşı bacaklı giriş) ve `acilisDevri` (**ACILIS hareketine bağlı kasa bacağı**, tur 4) ayrı alanda, toplama girmez. Tanım cümlesi istemci sözlüğünden TR+EN gelir (tur 4; API yalnız sayı döner). PRO'da ayrı satır, tanım katlanırda. Uyarılar `TlMetni` ile: "128.981,98 ₺" | KAPANDI (tur 4 düzeltmesiyle) |
| 6 | Has tahsilat uyarısı | Büyük harfli, gerçek düğme adını kullanmıyordu | Tek cümle: "Altın, müşterinin … borcunu düşürmez; … önce «Alış · eski altın» ile alın…" + «Alış · eski altın» ekranına götüren bağlantı. TakasEsnafKilit metni de sadeleşti | KAPANDI |
| 7 | Tahsilat penceresi | İsteğe bağlılar açıktı, gram silinince "0,000 g" kalıyordu | "+ Altınla öde" ve "+ Not ekle" kapalı. Güncel borç başlık altında hep görünür (`#th-guncel`). Gram artığı yok | KAPANDI |
| 8 | Takas kimlik kapısı | Kilit sebebi sabitti, hata rengi hemen çıkıyordu | Kilit sebebi eksik alana göre güncellenir ("Eksik: TCKN, adres."). Hata rengi ilk dokunuş ya da Kaydet denemesinden sonra (ölçüm `kirmizi=0`). Düğme iki modda "Kaydet". PRO metni madde no + sade dil | KAPANDI |
| 9 | Ekstre işaret/has | Has satırında has bakiyesi yoktu; eksi işareti karışıktı | Ekranda has satırının bakiyesinde has bakiyesi var ("10.458,91 ₺ · −1,0000 has"). Eksi her yerde U+2212. Has etkisi olmayan satırda boş has hücresi. Kâğıtta bakiye hücresi TL kalır (gerekçe § 0) | KAPANDI |
| 10 | Gider virman "Hedef" iki kez | 2 | `hedefEtiket=1` (12 kombinasyon) | KAPANDI |
| 11 | Kayıttan sonra ödeme paneli başarı bandını örtüyor | `satis-06a-basari-bandi.png` | PRO kayıttan sonra ödeme katmanı kapanır, bant örtülmez (bUnit PD2E_11) | KAPANDI |
| 12 | PRO 375 ödeme katmanı taşıyor | x=49, w=343, sağ kenar 392 > 375 | 375'te tam genişlik alt çekmece: `x=0 w=375 sag=375` (foto `satis-odeme__PRO_*_375`) | KAPANDI |
| 13 | Peşin Müşteri + Veresiye kilidi | — (önce ölç) | PARA-DEMO'da zaten yapılmış. Ölçüm: "Peşin Müşteri'ye veresiye yazılamaz — müşteri seçin" kilidi PRO'da her genişlikte var (BASİT'te veresiye seçeneği yok: `yalniz-PRO`) | ÖNCEDEN KAPANMIŞ — kanıtlandı |

## 2. Sapmalar ve gerekçeler

- **Madde 9, kâğıt ekstre:** Kâğıtta bakiye hücresine has bakiyesi eklenince 1280 px'te İşlem sütunu sıkıştı ve "Satış · Veresiye" iki satıra kırıldı (ölçüldü). Kâğıtta has bakiyesi Tutarlar bölümünde, ekranda (Cariler detayı) has satırının bakiyesinde yazar. Hedef korunuyor: has bakiyesi görünür, işaret tek tip.
- **Madde 3b, tur 3 ek düzeltme:** Paket kapsamındaki yeni nakit kapısı mesajı kültürsüz "0.00 TRY" basıyordu. `KasaBakiyeKapisi.ParaMetni` tr-TR biçimini ve ₺/$/€ işaretini kullanır; tanınmayan kod olduğu gibi yazılır. Eski **toplam** kapısı mesajı (`Mesaj`, "bakiyesi yetersiz") bu pakette doğmadı, dokunulmadı (bkz. açık kalanlar).
- **Madde 5, tanım (tur 4'te düzeltildi):** Tur 3 raporu "elle giriş tahsilata dahil" diyordu, ama kod elle **DIGER** girişini "açılış devri" sayıp tahsilattan düşüyordu (doğrulayıcı S11: 250 → acilisDevri 250).
  - Şimdi her türden elle giriş (NAKİT/KART/HAVALE/DİĞER) tahsilatta kalır; DİĞER türündekiler "diğer" kalemine yazılır.
  - Açılış devri yalnız kurulumun açılış kaydına bağlı satırdır.
  - Elle girişin "tahsilat" sayılıp sayılmayacağı mali müşavir sorusu olarak duruyor (§ 13). Cevap gelene dek bugünkü davranış korunur; tanım cümlesi bunu açıkça yazar.
- **Madde 3b / K3, toplam kapısı mesajı (tur 4 kararı):** `KasaBakiyeKapisi.Mesaj` ("bakiyesi yetersiz: 1000.00 TRY var…") **değiştirilmedi.**
  - Gerekçe: bu cümle istemcide üç yerde ayrıştırılıp tr-TR ₺ ve İngilizce cümleye çevriliyor (`KasaBakiyeMesaji.Bicimle`, `Gider.razor`). Ekrana ham haliyle çıkmıyor (Gider fotoğraflarında K5=0).
  - Biçimi değiştirmek bu sözleşmeyi bozardı.
  - Kalıcı çözüm (yapılandırılmış hata kodu + alanlar) bu paketin alanı dışında: açık kalan 1.
- **Gider ekranı istemci kilidi (tur 4):** "Tutar kasa bakiyesini aşıyor" kilidi yalnız TOPLAM bakiyeye bakıyor.
  - Nakit payı aşıldığında Kaydet açık kalır; sunucunun 400 nakit mesajı hata bandında görünür (ölçüldü).
  - İstemci kilidini nakit payına bağlamak bu turda yapılmadı: açık kalan 6.

## 3. Migration

Gerekmedi. Şema değişikliği yok: nakit/kart ayrımı mevcut `kasa_hareketleri.odeme_turu` alanından türetiliyor.

## 4. Derleme ve testler

| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` (tur 3 başı) | 0 hata · 0 uyarı |
| `dotnet build Kuyumcu.sln` (tur 3 sonu, son mutasyondan sonra temiz) | 0 hata · 2 uyarı — ikisi de **başka şeridin** dosyasında: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(455)` xUnit2029. Bu paket dokunmadı; ilk derlemede yoktular |
| Filtreli: `ParaDemo2*` + `TS1TuvalSatis` | 62/62 başarılı |
| Filtreli (tur 3 değişikliği sonrası): `ParaDemo2*` + `KasaBakiye*` + `NakitKasa*` | 40/40 başarılı |
| **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t2`, trx `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-2`) | **9176 toplam · 9174 başarılı · 0 başarısız · 2 atlanan** (2 dk 46 sn) |

- **Yeni test sayısı:** 32 (ParaDemo2Testleri 16 HTTP/DB/saf + ParaDemo2EkranTestleri 16 bUnit). Tur 3'te mevcut `PD2_3_nakit_1000_kart_500_iken_1200_nakit_ELLE_CIKIS_400_hicbir_sey_yazilmaz` testine mesaj biçimi iddiaları eklendi. Tur 2'de TS1TuvalSatisTestleri bir iddiası yeni yerleşime uyarlandı (kasa adı seçicinin altında).
- **Tam süit zamanlaması:** Tam süit, tur 3 mesaj düzeltmesinden **önce** koştu (tur başına bir kez kuralı). Düzeltme yalnız `KasaBakiyeKapisi.NakitMesaji` ve tek test dosyasına dokunuyor. Mesajı kullanan tüm testler (yalnız ParaDemo2Testleri) filtreli koşumda yeşil. Başka testte "nakit yetersiz" ya da `NakitMesaji` geçmiyor (grep ile doğrulandı).
- **Yeni kırmızı:** 0 (tam süitte kırmızı yok).

### 4b. Tur 4 derleme ve testler

| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` (tur 4 kod değişikliği sonrası, tam) | 0 hata · 2 uyarı — ikisi de başka şeridin satırı: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601 ve `KesifP2Testleri.cs(455)` xUnit2029 (455. satır PARA-DEMO-2 iddiası değil; bu paketin iddiaları 429/437) |
| `dotnet build Kuyumcu.sln` (son mutasyondan sonra, temiz) | 0 hata · 0 uyarı (artımlı) |
| Filtreli: `ParaDemo2*` + `Toplu1Testleri` + `GiderEkran*` + `KasaBakiye*` + `NakitKasa*` + `Q101Gider*` + `Kurulum*` | **151/151** başarılı |
| Filtreli (Raporlar düzen düzeltmesi sonrası): `ParaDemo2*` + `Raporlar*` + `Mutabakat*` | **81/81** başarılı |
| **Tam süit** (tek koşum, `KUYUMCU_TEST_DB=kuyumcu_test_t2`, trx `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO-2-t4`) | **9242 toplam · 9238 başarılı · 2 başarısız · 2 atlanan** (6 dk 28 sn) |

**Tam süitteki 2 kırmızı:**
1. `ParaDemoTur2Testleri.PD6_kart_payini_ASAN_virman_400_hicbir_sey_yazilmaz` — **bu paketin ürünü.** Test, virman kart payı mesajının eski biçimini iddia ediyordu (`"kart tahsilatı 500.00"`); K3 düzeltmesi biçimi "500,00 ₺" yaptı. İddia yeni biçime uyarlandı (UX-S32 gerekçesi test içinde). Filtreli yeniden koşum: `ParaDemoTur2Testleri` + SATIS3 arama testi 3 kez × **8/8 başarılı**. Tam süit tur başına bir kez kuralı gereği yeniden koşulmadı; bu düzeltme yalnız tek bir test iddiasını değiştiriyor.
2. `SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE_ile_TEK_istek_ve_liste_gorunur` (Expected 1, Actual 0; zamanlama) — **bu paketin değil.** Bu paket arama/debounce koduna dokunmadı. Yalıtılmış 3 koşumda 3/3 yeşil. Aynı test PATCH-SOZLESME şeridinin tam süitinde de kırmızı çıkmış ve SATIS-3 / PATCH-SOZLESME / ZO-ISLEM-A raporlarında yük altında kararsız olarak kayıtlı. Sahibi: SATIS-3 (ekran arama).

- **Sonuç: bu paketin yeni kırmızısı 0** (PD6 düzeltildi; SATIS3 bilinen kararsızlık, başka şerit).
- **Yeni test (tur 4):** 2 (`PD2_3_…_NAKIT_GIDER_400_tam_nakit_200`, `PD2_5_elle_DIGER_girisi_…`) + 3 testte güçlendirme (`PD2_5_gun_sonu_…` gerçek açılış ucu + `tanim` yok; `PD2_3_NAKIT_virman_…` kart mesaj biçimi; `PD2E_5_…` sözlük metni). Paketin toplam yeni testi: **34**.

## 5. Mutasyon tablosu

Tur 1–2 sonuçları `ham/PARA-DEMO-2-olcum/sonuc.json` dosyasında, yedekleri `ham/mut-yedek-PARA-DEMO-2/` altında. Hepsinde derleme hatası yok ve geri alınınca `cmp` birebir AYNI.

| Kod | Dosya | Kırmızı test(ler) |
|---|---|---|
| M1-1 | Satis/OdemeSepeti.cs | PD2_saf_fis_etiketi_tek_arac_kendisi_iki_farkli_arac_KARMA · PD2_1_tek_bacak_KART_ve_iki_KART_bacagi_KART_etiketi · PD2_1_tek_bacak_HAVALE_fisi_hareket_HAVALE_ekstre_Havale_gun_sonu_havale |
| M1-2 | Cari/CariModulu.cs | PD2_1_gercek_karma_KARMA_kalir_ekstre_Kart_arti_Havale_yazar |
| M1-3 | Cikti/CiktiModulu.cs | PD2_1_gercek_karma_KARMA_kalir_ekstre_Kart_arti_Havale_yazar |
| M1-4 | Pages/Islemler.ParaDemo2.cs | PD2E_1_BASIT_tek_odeme_satirindan_HAVALE_secilir_TEK_odemeli_uca_HAVALE_gider · PD2E_1_Tek_odemeye_don_SECILI_araci_korur |
| M1-5 | Pages/Islemler.ParaDemo2.cs | PD2E_1_Tek_odemeye_don_SECILI_araci_korur |
| M2-1, M2-2 | Pages/Kasalar.razor | PD2E_2_Gunu_Kapat_acilista_sayilan_BOS_fark_YOK_kilit_sebebi_TEK_ve_altlikta |
| M3-1 | Kasa/KasaBakiyeKapisi.cs | 5 test, ör. PD2_3_NAKIT_virman_nakit_payini_asarsa_400_KART_virman_kendi_payindan_200 · PD2_3_nakit_1000_kart_500_iken_1200_nakit_ELLE_CIKIS_400_hicbir_sey_yazilmaz · PD2_3_kartli_satisin_NAKIT_IADESI_nakit_payini_asarsa_400 |
| M3-2 | Kasa/KasaBakiyeKapisi.cs | PD2_3_kart_cikisi_nakit_kapisina_TABI_DEGIL_toplam_kapisi_aynen |
| M3-3 | Kasa/KasaNakitAyrimi.cs | PD2_3_ayrim_ayni_istekte_EKLENEN_kart_satirini_sayar_kaydedilmis_satiri_iki_kez_saymaz |
| M3-4 | Satis/SatisModulu.cs | PD2_3_nakit_1000_kart_500_iken_1099_TL_nakit_HURDA_ALISI_400 · PD2_3_kartli_satisin_NAKIT_IADESI_nakit_payini_asarsa_400 |
| M3-5 | Kasa/KasaModulu.cs | PD2_3_nakit_1000_kart_500_iken_1200_nakit_ELLE_CIKIS_400_hicbir_sey_yazilmaz · PD2_3_sinir_tam_nakit_kadar_cikis_200_bir_kurus_fazlasi_400 |
| M3-6 | Gider/VirmanModulu.cs | PD2_3_NAKIT_virman_nakit_payini_asarsa_400_KART_virman_kendi_payindan_200 |
| **M3-7 (tur 3)** | Kasa/KasaBakiyeKapisi.cs — `"TRY" => "₺"` eşlemesi silindi (perl, tek eşleşme) | PD2_3_nakit_1000_kart_500_iken_1200_nakit_ELLE_CIKIS_400_hicbir_sey_yazilmaz (1/8 kırmızı) → yedek geri kopyalandı → `cmp=AYNI` → temiz build |
| M4-1 | Kasa/KasaModulu.cs | PD2_4_nakit_ayrimi_ucu_kasa_basina_nakit_kart_havale_doner |
| M4-2 | Pages/Kasalar.razor | PD2E_4_kasa_kartinda_Nakit_ve_Kart_Havale_alt_satiri |
| M4-3 | Pages/Raporlar.razor | PD2E_4_BASIT_gun_sonu_karti_tahsilat_kirilimi_TEK_satir_ve_kasa_ayrimi |
| M5-1 | Rapor/RaporModulu.cs | PD2_5_gun_sonu_toplam_tahsilat_virman_ve_acilis_devrini_DISARIDA_birakir |
| M5-2 | Rapor/RaporModulu.cs | PD2_5_gun_sonu_uyarilarinda_TL_kodu_YOK_tutar_TL_isaretli |
| M5-3 | Pages/Raporlar.razor | PD2E_5_PRO_gun_sonu_virman_ve_acilis_AYRI_satir_toplama_girmez_tanim_katlanirda |
| M6-1 | Pages/Cariler.razor | PD2E_6_has_uyarisi_BASIT_gercek_dugme_adi_tek_cumle_buyuk_harf_yok_baglanti |
| M7-1, M7-2 | Pages/Cariler.razor | PD2E_7_tahsilat_guncel_borc_HEP_gorunur_istege_baglilar_KAPALI |
| M8-1 | Pages/Islemler.ParaDemo.cs | PD2E_8_takas_kilit_sebebi_EKSIK_ALANA_gore_guncellenir_hata_rengi_dokununca |
| M8-2 | Bilesenler/Fis/FisTakasSatici.razor | PD2E_8_takas_kilit_sebebi_EKSIK_ALANA_gore_guncellenir_hata_rengi_dokununca |
| M8-3 | Pages/Islemler.Yazdir.cs | PD2E_8_takas_Kaydet_yazisi_PRO_da_da_Kaydet |
| M9-1 | Pages/Cariler.razor | PD2E_1_9_ekstre_karma_Kart_arti_Havale_has_satiri_has_bakiyesi_bos_isaret_yok |
| M9-2 | Cikti/CiktiModulu.cs | Q94_cari_ekstresinde_isaret_KORUNUR_bicim_ayni · PD5_ekstre_has_ile_tahsilat_Has_yazar_TL_etkisi_sifir |
| M10-1 | Pages/Gider.razor | PD2E_10_virman_formunda_Hedef_etiketi_TEK |
| M11-1 | Pages/Islemler.razor | PD2E_11_PRO_kayittan_sonra_odeme_katmani_KAPALI_basari_bandi_ortulmez |

| **M3-8 (tur 4)** | Gider/GiderModulu.cs — `NakitDogrula(…, OdemeAraci.Nakit)` → eski `Dogrula(kasa, -tutar)` | PD2_3_nakit_1000_kart_500_iken_1200_NAKIT_GIDER_400_tam_nakit_200 |
| **M3-9 (tur 4)** | Gider/VirmanModulu.cs — kart payı mesajında `ParaMetni(pay…)` → `pay.ToString("0.00", Invariant) + " TRY"` | PD2_3_NAKIT_virman_nakit_payini_asarsa_400_KART_virman_kendi_payindan_200 |
| **M5-4 (tur 4)** | Rapor/RaporModulu.cs — tahsilat kırılımı yeniden `OdemeTuru != "DIGER"` ile süzülür (eski kural) | PD2_5_elle_DIGER_girisi_acilis_devri_DEGIL_diger_kaleminde_kalir |
| **M5-5 (tur 4)** | Rapor/RaporModulu.cs — açılış devrinin DIGER'den düşülmesi `if (false && …)` | PD2_5_elle_DIGER_girisi_… · PD2_5_gun_sonu_toplam_tahsilat_virman_ve_acilis_devrini_DISARIDA_birakir |
| **M5-6 (tur 4)** | Rapor/RaporModulu.cs — açılış kimliği `h.Tur == "ACILIS"` → `!=` | PD2_5_elle_DIGER_girisi_… · PD2_5_gun_sonu_toplam_tahsilat_… |
| **M5-7 (tur 4)** | Pages/Raporlar.razor — tanım metni `TahsilatTanimMetni` → `TahsilatTanimBaslik` | ParaDemo2RaporEkranTestleri.PD2E_5_PRO_gun_sonu_virman_ve_acilis_AYRI_satir_toplama_girmez_tanim_katlanirda |

**Tur 4 mutasyon yöntemi:** `t4mut/mut.zsh`. Her kural için yedek `ham/mut-yedek-PARA-DEMO-2/<kod>-<dosya>` alındı, `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı, filtreli test koşuldu, yedek geri kopyalandı. **6/6'sında `cmp=AYNI`.** Kaynakta mutasyon izi yok (grep). Son mutasyondan sonra temiz derleme: 0 hata. Ham sonuç: `ham/PARA-DEMO-2-olcum/tur4/mutasyon-sonuc.txt`. M5-7'nin ilk koşumu yanlış sınıf filtresi verdiği için 0 test koştu ("eşleşen test yok"). Doğru filtreyle (`ParaDemo2RaporEkranTestleri`) yeniden koşuldu ve kırmızı oldu. İkinci koşum betiği önceki 5 mutasyonu da tekrarladı; sonuçlar aynı, hepsinde `cmp=AYNI`.

**Madde 12 (yalnız CSS):** bUnit CSS yerleşimini ölçemez. Kanıt gerçek tarayıcı ölçümüdür: `katman x=0 w=375 sag=375 vw=375`, 4 kombinasyon. M12-1 yedeği (FisVerdimAldim, 09:17) güncel kaynakla birebir AYNI, yani kaynakta artık yok. O mutasyonun sonucu makine çöküşünde kayboldu ve sayılmadı.

## 6. denetim.sh

`bash ./denetim.sh` (zsh ile koşulmaz) → çıkış 1. **Tek ✗: P-30 DEMO_INSAN_SURESI (BK-45)** ("demo-prova … 15/20 dk BEYAN, kronometreli ölçüm ŞART"). Bu, kabul ölçütünde muaf tutulan P-30 maddesi. Bu paketin sayaçlarında ✗ yok. Kalan satırlar ⚠/bilgi notları (CARI_BAKIYE tohum kalıntıları, kurucu kararında). **Tur 4 (02.10 ~12:00, son kodla) yeniden koşuldu:** çıkış 1, tek ✗ yine P-30 (`ham/PARA-DEMO-2-olcum/tur4/denetim-ozet.txt`).

## 7. Canlı doğrulama (5162, yeni kiracı `PAKETPARA-DEMO-2-T4-TEST`, tur 3 kodu — tur 4 için § 7b)

Ham çıktı `ham/PARA-DEMO-2-olcum/tur3/canli4.txt`, betik `tur3/canli.py`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| satış fişi, tek HAVALE 7000 | 200 | 200 · fisId 01M3XSF8H1… |
| satış fişi, KART 2000 + HAVALE 3000 | 200 | 200 · fisId 01M3XSF8ZA… |
| ekstre, tek HAVALE satırı | odemeTuru=HAVALE, araçlar null | HAVALE · null |
| ekstre, karma satır | KARMA + ["KART","HAVALE"] | KARMA · ["KART","HAVALE"] |
| kâğıt ekstre İşlem sütunu | "Satış · Havale", "Satış · Kart + Havale" | aynen |
| pos-fatura-mutabakatı tahsilat | havale 10000, kart 2000, toplam 12000 | aynen; virmanGiris 0, acilisDevri 0, tanim dolu |
| uyarılar | ₺ biçimi, "TL" yok | "2 satış faturalandırılmamış (toplam 12.000,00 ₺)" |
| nakit-ayrımı (nakit 1000 + kart 500) | 1500/1000/500/0 | aynen |
| hurda alış ~1200 NAKİT | 400 | 400 · "çekmecede 1.000,00 ₺ nakit var, 1.199,96 ₺…" (canlı çıktı tur 3 mesaj düzeltmesinden önce alındı; biçim düzeltmesi aşağıdaki tarayıcı ölçümünde) |
| elle çıkış NAKİT 1200 | 400 | 400 |
| virman NAKİT 1200 | 400 | 400 |
| virman KART 300 | 200 | 200 |
| elle çıkış NAKİT 1000,01 | 400 | 400 |
| elle çıkış NAKİT 1000 (tam) | 200 | 200 · bakiye 200,00 |
| mutabakat (virman sonrası) | virman toplama girmez | toplam 13500, virmanGiris 300 ayrı |

**Bağımsız SQL** (`tur3/sql-canli4.txt`):

```
hareketler: 01M3XSF8H1TYQ1PW79Y4CTVFKF HAVALE 7000.00 · 01M3XSF8ZAGHZD4HZPJ3EXTBPA KARMA 5000.00
satış kasası bacakları: HAVALE 7000 · KART 2000 · HAVALE 3000
PD2 Kasa T4: bakiye 200.00 · hareketler NAKIT +1000, KART +500, KART −300 (virman), NAKIT −1000 (tam)
PD2 Hedef T4: 300.00 · hurda satırı (reddedilen istek): 0
```

### 7b. Tur 4 canlı doğrulama (5162, yeni kiracı `PAKETPARA-DEMO-2-T5-TEST`, tur 4 kodu)

Ham çıktı `ham/PARA-DEMO-2-olcum/tur4/canli5.txt`, betik `tur4/canli.py`, SQL `tur4/sql-canli5.txt`. Tur 3 adımlarının hepsi yeniden koşuldu, sonuçlar aynı. Tur 3'te biçim düzeltmesinden önce alınan hurda mesajı artık "1.000,00 ₺ … 1.199,96 ₺". Bunlara ek olarak:

| Adım | Beklenen | Gerçek |
|---|---|---|
| gider kasası: giriş NAKİT 1000 + KART 500 | 200 | 200 / 200 |
| POST /giderler 1200 | **400**, nakit mesajı | **400** · "çekmecede 1.000,00 ₺ nakit var, 1.200,00 ₺ nakit isteniyor. Kasadaki kart (500,00 ₺)…" |
| POST /giderler 1000,01 | 400 | 400 |
| POST /giderler 1000 (tam) | 200 | 200 · kasaBakiyesi 500,0 |
| nakit-ayrımı (gider kasası) | nakit 0, kart 500 | `toplam 500 · nakit 0 · kart 500` |
| virman KART 600 (kart payı 500) | 400, ₺ biçimi | 400 · "kart tahsilatı 500,00 ₺; 600,00 ₺ aktarılamaz." |
| elle DIGER giriş 250 → mutabakat | diger 250, acilisDevri 0, `tanim` yok | `diger 250 · toplam 15250 · virmanGiris 300 · acilisDevri 0`, `tanim` alanı yok |
| kurulum açılışı prova → uygula (400) | 200 / 200 | 200 / 200 |
| mutabakat (açılış sonrası) | acilisDevri 400, diger 250, toplam aynı | `acilisDevri 400 · diger 250 · toplam 15250` |

**Bağımsız SQL** (`tur4/sql-canli5.txt`):

```
hareketler: SATIS HAVALE 7000.00 · SATIS KARMA 5000.00 · ACILIS 400.00
PD2 Gider Kasa T5 (bakiye 500): NAKIT +1000 · KART +500 · NAKIT −1000 (gider_kategori dolu)  ← reddedilen 1200 ve 1000,01 satır yazmadı
PD2 Diger Kasa T5 (250):  DIGER +250 · hareket_id YOK          → tahsilat "diğer"
PD2 Acilis Kasa T5 (400): DIGER +400 · hareket_id → ACILIS     → açılış devri
PD2 Kasa T5 (200): NAKIT +1000, KART +500, KART −300, NAKIT −1000 · hurda satırı 0
```

## 8. Gerçek tarayıcı ölçümü (başlıksız Chrome, cdp.mjs, vekil ile parolasız oturum)

- **Tur 1–2 ölçümleri** (`ham/PARA-DEMO-2-olcum/olcum3*.jsonl`, 120 fotoğraf):
  - Kapsam: 10 ekran × 1440/1280/375 × BASİT/PRO × açık/koyu.
  - **K1 = K5 = K7 = K10 = K11 = K12 = 0, yatay = 0.**
- **Kalan bulgular ve değerlendirmeleri:**
  - **K9 (binen):** sağ çekmece ya da tahsilat/ekstre penceresi alttaki tabloyu örtüyor (ör. `th "MÜŞTERİ" ⟂ #modal…`, `#kasa-yeni ⟂ #gun-toplam`). Bu kasıtlı katman: ölçer, örtü katmanını altındaki sayfayla karşılaştırıyor. Fotoğraflarda yazı üstüne binme yok (gunkapat 1280, satis-odeme PRO 375, kasa hareketi 1280/375 görüldü).
  - Tur 1'de satis-odeme BASİT 1280'de gerçek bir K9 vardı ("11.079,41 ₺ ⟂ Son 3 satış"). Tur 2'de kasa adı seçicinin altına indi; yeniden ölçüm `olcum3c` BASİT 6/6 temiz.
  - **K3 0/1:** tahsilat 375. Pencere başlığı "Tahsilat — Kemal Ardıç" iki satıra kırılıyor, ad bütün kalıyor (fotoğraf görüldü).
  - **K8:** kâğıt BASİT'te "PAKETPARA-DEMO" = test kiracısının adı, ürün metni değil.
  - **Ölçer hataları:** 4 satırda `TypeError: Illegal invocation` (ölçer betiği hatası). Bu kombinasyonlar `olcum3b`/`olcum3c` ile yeniden ölçüldü.
- **Tur 3 ölçümü:** nakit kapısı hata bandı (`tur3/olcum.jsonl`, 8 fotoğraf).
  - Rota `/kasalar?kasa=…&yon=cikis`, Gider 150 → bant görünür.
  - 1280/375 × BASİT/PRO × açık/koyu: **K5 (TRY) = 0**, K1 = K3 = K7 = K10 = K11 = K12 = 0, yatay = 0. K9 5–6 yine çekmece-sayfa örtüsü.
  - Fotoğraf görüldü: `kasa-nakit-hata__BASIT_aydinlik_1280.png`, `kasa-nakit-hata__PRO_karanlik_375.png`.
- **Read ile görülen fotoğraflar:**
  - `gunkapat__BASIT_aydinlik_1280`: Sayılan nakit boş ve zorunlu, fark yok, kilit sebebi tek, Günü Kapat Vazgeç'in yanında.
  - `satis-odeme__BASIT_aydinlik_1280`: tek ödeme satırında Kart seçici, kasa adı altında, tutar kırpılmıyor.
  - `satis-odeme__PRO_karanlik_375`: ödeme katmanı tam genişlik alt çekmece.
  - `tahsilat__BASIT_aydinlik_375`: güncel borç başlıkta, has uyarısı tek cümle ve bağlantılı.
  - `raporlar__BASIT_karanlik_1280`: "Kasada ne var?" Nakit · Kart/Havale satırı; gün sonu tahsilat kırılımı tek satır.

### 8b. Tur 4 tarayıcı ölçümü (başlıksız Chrome, CDP 9907, vekil 5163)

- **Rotalar:** `/raporlar?sekme=gunsonu` (tanım katlanırı açık) ve `/gider` (nakit payını aşan gider → hata bandı).
- **Kapsam:** 1440/1280/375 × BASİT/PRO × açık/koyu = 24 ölçüm (`tur4/olcum4.jsonl`). `gider BASIT açık 1280` bu koşumda sayfa 10 sn içinde yüklenmedi (yükleme göstergesi; aynı dakikada API günlüğünde başka kiracının iş-çalışması çakışması vardı). Bu kombinasyon `olcum5.jsonl` ile yeniden ölçüldü, bant görüldü. Önceki koşum `olcum3.jsonl` aynı kombinasyonu da bantla ölçmüştü.

| Rota | Genişlik × mod × tema | K1 | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 binen | K10 | K11 | K12 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|
| gün sonu (tanım açık) | 1440/1280/375 × BASİT/PRO × açık/koyu (12) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| gider nakit hata bandı | 1440/1280/375 × BASİT/PRO × açık/koyu (12) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- **K2 (en geniş blok / ana alan):** 97–98 %. Dar sütun yok.
- **Gün sonu:** BASİT gün sonunda "tahsilata girmeyen" kartı ve tanım katlanırı yok. Bu bilinçli: BASİT'te tahsilat tek satır (madde 4). PRO'da tanım metni sözlükten okundu (ölçüm `veri.tanim` = `TahsilatTanimMetni`).
- **Gider:** 12/12 kombinasyonda hata bandında "nakit yetersiz … 1.000,00 ₺ … 1.200,00 ₺ …" görüldü ("TRY" yok).
- **Read ile görülen fotoğraflar** (`ham/PARA-DEMO-2-olcum/tur4/foto/`):
  - `gunsonu-tanim__PRO_aydinlik_1280`: ayrı satırlar dolgulu kartta (300 ₺ / 400 ₺), tanım katlanırı kartın dışında ve açık; Diğer 250 ₺ toplamın alt satırında.
  - `gunsonu-tanim__PRO_karanlik_375`: aynısı, dar ekranda taşma yok.
  - `gider-nakit-hata__PRO_karanlik_375`: hata bandı sayfanın üstünde, metin 4 satıra sarıyor, taşma yok.
  - `gider-nakit-hata__BASIT_aydinlik_1280`: bant + form; Kaydet etkin.
- **İlk koşumdaki ölçüm sorunları ve çözümleri:**
  - Gider kasa seçicisi özel combobox olduğu için `select` ile seçilemedi. Betik `li[data-deger]` tıklamasına çevrildi.
  - Tur 3 kasası 500 ₺'ye inmişti; istemci toplam kilidi Kaydet'i kilitledi. Ölçüm için 1000 + 500'lük yeni bir "PD2 Ekran Kasa T5" kuruldu.
  - İki 375 kombinasyonunda bant 2,2 sn içinde gelmedi. Bant çıkana dek (≤6 sn) bekleyen `olcum4` ile 12/12 bant görüldü.

## 9. DoD sözleşmesi

- **Yeni varlık/kayıt türü yok.**
- **Yeni uç** `GET /api/v1/kasalar/nakit-ayrimi`: salt okunur türetilmiş görünüm (kasa hareketlerinden hesaplanır), düzenlenecek kaydı yok. Kasa modülünün mevcut yetki özniteliklerini taşır.
- **Ödeme türü indirgeme:** Yalnız yeni yazılan kayıtları etkiler. Eski tek bacaklı KARMA kayıtlar için **geri dönük veri düzeltmesi yapılmadı** (paket kararı). Bunlar `hareketler` defterinde (finansal, hard DELETE/UPDATE yasak) "Karma" olarak kalır; düzeltme yolu storno motorudur.
- Düğmeler: Günü Kapat kilitliyken sebebi yazar. Takas Kaydet kilitliyken eksik alanı yazar.

## 10. Değişen dosyalar (paket alanı; `git status --porcelain | grep -v /(bin|obj)/`)

Tur 4'te:
- `dotnet/Kuyumcu.Api/Modules/Gider/GiderModulu.cs`: gider yolunda nakit kapısı (K1).
- `dotnet/Kuyumcu.Api/Modules/Gider/VirmanModulu.cs`: kart/havale payı mesajı `ParaMetni` (K3).
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaBakiyeKapisi.cs`: yalnız `Mesaj` üstüne gerekçe yorumu (davranış aynı).
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`: açılış devri kimlikle; `tanim` alanı kaldırıldı (K2, K4).
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.ParaDemo2.cs`: `TahsilatTanimMetni` TR+EN.
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`: `MutabakatTahsilat.Tanim` kaldırıldı.
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`: tanım sözlükten; ayrı satırlar `kart-govde` dolgusunda; katlanır kartın dışında.
- `dotnet/Kuyumcu.Tests/ParaDemo2Testleri.cs`: 2 yeni test, 2 testte güçlendirme.
- `dotnet/Kuyumcu.Tests/ParaDemo2EkranTestleri.cs`: PD2E_5 sözlük metni.
- `dotnet/Kuyumcu.Tests/ParaDemoTur2Testleri.cs`: PD6 mesaj biçimi iddiası.

Tur 3'te:
- `KasaBakiyeKapisi.cs`: `NakitMesaji` → `ParaMetni`.
- `ParaDemo2Testleri.cs`: mesaj biçimi iddiaları.

Paket boyunca (PARA-DEMO-2 izi taşıyan dosyalar):

- **API:**
  - Satis/OdemeSepeti.cs, Satis/SatisModulu.cs
  - Kasa/KasaBakiyeKapisi.cs, Kasa/KasaNakitAyrimi.cs, Kasa/KasaModulu.cs
  - Gider/VirmanModulu.cs
  - Rapor/RaporModulu.cs (yalnız gün sonu)
  - Cari/CariModulu.cs (ekstre), Cikti/CiktiModulu.cs (ekstre)
- **Core:** I18n/Sozlukler.ParaDemo2.cs
- **Web, sayfalar:** Islemler.razor, Islemler.ParaDemo.cs, Islemler.ParaDemo2.cs, Islemler.Odeme.cs, Islemler.Yazdir.cs, Kasalar.razor, Raporlar.razor, Cariler.razor, Gider.razor
- **Web, bileşenler:** Fis/FisVerdimAldim.razor, Fis/FisTakasSatici.razor, Fis/FisOdemeKasa.razor, Cekmece.razor (`KaydetMetni`/kimlik parametreleri; verilmezse eski davranış)
- **Web, CSS:** wwwroot/css/tuval-satis.css
- **Testler:**
  - Yeni: ParaDemo2Testleri.cs, ParaDemo2EkranTestleri.cs
  - Uyarlanan: TS1TuvalSatisTestleri.cs, CariEkranTestleri.cs, ParaDemoTestleri.cs, ParaDemoEkranTestleri.cs, Q94CiktiBelgeTestleri.cs, S36KasaBildirimTestleri.cs, SATIS3EkranTestleri.cs, UX4bEkranTestleri.cs, ErisimKapatmaTestleri.cs, KesifP2Testleri.cs (PARA-DEMO-2 yorumlu iddia güncellemeleri)

**Dokunulan sayfalar (DoD EK 2):** Islemler, Kasalar, Raporlar (yalnız "Kasada ne var" ve gün sonu), Cariler (tahsilat çekmecesi ve ekstre), Gider (virman formu). Tur 4'te yalnız Raporlar gün sonu bloğu yeniden açıldı (gerekçe: doğrulayıcı bulgusu K4, aynı paket ve aynı dalga). Gider.razor'a tur 4'te dokunulmadı. **Panel.razor'a bu paket dokunmadı.** `git status`'taki M işareti TOPLU-4 şeridine ait.

## 11. Koşul karşılama (tur 4 itibarıyla)

| Koşul | Durum | Kanıt |
|---|---|---|
| 13 madde, önce/sonra | KARŞILANDI | § 1 (madde 3 ve 5 tur 4'te düzeltildi, § 0b) |
| Doğrulayıcı bulguları (KRİTİK 1, ORTA 2, DÜŞÜK 2) | KARŞILANDI — 4'ü kodla kapandı, K3'ün toplam-kapısı yarısı gerekçeli sapma | § 0b, § 2 |
| Para/veri: canlı API + bağımsız SQL | KARŞILANDI | § 7, § 7b |
| Sınır testleri (1000+500 → 1200 → 400; 1000,01 → 400; 1000 → 200) — hurda, elle çıkış, NAKİT virman, **gider** | KARŞILANDI | § 7b, PD2_3_* (gider dahil) |
| Mutasyon (kritik kural başına ≥1 kırmızı, cmp AYNI) | KARŞILANDI | § 5: 31 + tur 4'te 6 (M3-8, M3-9, M5-4…M5-7) |
| Ekran: başlıksız Chrome 1440/1280/375 × BASİT/PRO × açık/koyu, Read ile görüldü | KARŞILANDI | § 8, § 8b (tur 4: 24 ölçüm, tüm sayaçlar 0) |
| Kabul: tek bacak HAVALE → hareket HAVALE, ekstre Havale, gün sonu havale | KARŞILANDI | § 7b (yeniden üretildi) |
| Kabul: Günü Kapat açılışta fark görünmez | KARŞILANDI | ölçüm `sayilan="" fark=false`, foto (tur 1–3; tur 4 dokunmadı) |
| Kabul: nakit 1000 + kart 500 iken 1200 nakit hurda alışı 400 | KARŞILANDI | § 7b |
| Build 0 hata | KARŞILANDI | 0 hata; 2 uyarı başka şeridin satırlarında (§ 4b) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | § 6 — tur 4'te yeniden koşuldu: tek ✗ P-30 DEMO_INSAN_SURESI |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9238/9242; 2 kırmızıdan PD6 bu paketin iddiasıydı, düzeltildi ve filtreli 8/8 yeşil; SATIS3 debounce başka şeridin bilinen kararsızlığı (§ 4b) |
| Test kiracısı silindi | KARŞILANDI | tur 4 kiracısı `01M3XW54MXJSB0BMS42NDKW7Q4` `zsh kiraci-temizle.sh --uygula` ile silindi; sorgu: o id ya da `PAKETPARA%` = 0. `tenants` başta 18, sonda 16 (aradaki fark eşzamanlı şeritlerin kendi kiracılarını silmesi; QA-TEST-* dokunulmadı) |
| COMMIT yok, dosya silme yok | KARŞILANDI | — |
| Süreçler kapatıldı | KARŞILANDI | 5162/5163/9907 dinleyen yok (`lsof` 0) |

## 12. Açık kalanlar

1. **Toplam kapısı mesajı** (`KasaBakiyeKapisi.Mesaj`, "bakiyesi yetersiz: 1000.00 TRY var") makine biçiminde kaldı (§ 2 gerekçesi: istemcideki 3 ayrıştırıcıyla sözleşme). Ekrana ham çıkmıyor. Kalıcı çözüm: hata gövdesine yapılandırılmış alanlar (`kod`, `bakiye`, `istenen`, `paraBirimi`), sonra cümle iki tarafta biçimlenir. Toplu kuyruk; sahibi kasa kapısı bakımı (Taşkın) + istemci ayrıştırıcıları (Ertuğ).
2. **Eski KARMA tek bacak kayıtlar:** geri dönük düzeltme yok (paket kararı). Eski fişler ekstrede "Karma ödeme" kalır.
3. **Derleme uyarıları** (başka şerit): `SUBEKAPSAMASinirTestleri.cs:49` CS8601 ve `KesifP2Testleri.cs:455` xUnit2029. Orkestratör kapısında 0 uyarı için sahiplerine.
4. **Ölçer K9:** çekmece/pencere örtüsünü "binen" sayıyor (yanlış pozitif; betik bakımı).
5. **M12-1:** CSS mutasyonunun sonucu makine çöküşünde kayboldu. Madde 12'nin kanıtı tarayıcı ölçümü.
6. **Gider ekranı istemci kilidi** yalnız toplam bakiyeye bakıyor. Nakit payı aşımında Kaydet açık kalıyor, sunucu 400 nakit mesajını bantta gösteriyor (ölçüldü). İstemci kilidini `/kasalar/nakit-ayrimi` ile nakit payına bağlamak UX-S30 iyileştirmesi (sahibi Ertuğ, Gider.razor).
7. **Nakit kapısı mesajı İngilizce arayüzde Türkçe** (sunucu cümlesi; toplam kapısındaki gibi bir istemci çevirisi yok). Madde 1 ile aynı yapılandırılmış hata çözümüne bağlı.
8. **`SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE…`** tam süit yükünde kararsız (sahibi SATIS-3).
9. **(tur 5) Belgeli kart/havale çıkışı araç payını eksiye düşürebilir:** tedarikçiye havaleyle ödenen alış/hurda alışı, kartlı iade kasanın havale/kart payını eksiye indirebilir (canlı: havale −2.496 ₺; Panel/Kasalar "Kart/Havale 5.504 ₺" yazar). Nakit payı ve gün sonu sayımı DOĞRU kalır (para çekmeceden çıkmaz). Kalıcı çözüm banka bacağının açılması (kart/havale tahsilatı ve ödemesi banka hesabına yazılır). Bu yollara pay kapısı konmadı, çünkü 10 meşru akış kırılıyordu (§ 0c T5-5). Kurucu kararı gerekir (§ 13).
10. **(tur 5) MASAK amaç alanı eksikken kilit cümlesi genel** ("Kimlik tespiti eksik: MASAK eşiği aşıldı…"). Değişimde satıcı kimliği eksikleri artık adıyla yazar; ama MASAK'ın kendi alanları (işlem amacı, beyan açıklaması) adıyla yazmaz ve BASİT'te "MASAK" kelimesi görünür. Bu kilit bütün türler için ortak (İşlemler genel MASAK kuralı). Sahibi yasal kapılar (Mehmet + Ertuğ).
11. **(tur 5) Tahsilatta altın açılınca Tutar "(isteğe bağlı)" yazar.** Bilinçli: altın girilince TL tutar gerçekten isteğe bağlıdır; kural cümlesi ("Tutar ya da altın girin.") yalnız ikisi de boşken denemede çıkar.
12. **(tur 5) Ölçer K9/K3, açık modalda arka plan öğelerini sayıyor** (tahsilat penceresi; madde 4'teki yanlış pozitifin aynısı).
13. **(tur 5) Tam süitteki iki başkasına ait kırmızı:** `PatchSozlesmeKolonTavaniTestleri.PATCH_siparis_kolon_tavani…` (sahibi PATCH-SOZLESME şeridi, iş sürüyor), `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12…` (sahibi YASAL-4, kararsız; tek başına yeşil).

## 13. Kurucu / mali müşavir

- **Mali müşavir sorusu:** Gün sonu "toplam tahsilat" şu an satış + tahsilat + **kasaya her türden elle giriş** (NAKİT/KART/HAVALE/DİĞER; DİĞER "diğer" kaleminde). Virman ve açılış devri hariç. Açılış devri artık yalnız kurulumun açılış kaydına bağlı satır; tur 4'te düzeltildi, eskiden elle DİĞER girişi de açılış sayılıyordu. Kasaya elle konan para (ör. sermaye/ortak koyması) "tahsilat" sayılmalı mı, ayrı satırda mı gösterilmeli? Cevap gelene dek bugünkü davranış korunuyor. Ekrandaki tanım cümlesi (sözlükten, TR+EN) kapsamı açıkça yazıyor.
- **Kurucu kararı (tur 5):** Dükkân tedarikçiye havaleyle ödeme yaptığında ya da kartla iade ettiğinde bu para bugün kasaya "havale/kart çıkışı" diye yazılıyor; kasada o kadar havale/kart tahsilatı yoksa kasanın "Kart/Havale" payı eksiye düşüyor. Nakit sayımı etkilenmiyor. İki seçenek: (a) banka hesabı tanımlıysa bu ödemeler bankadan çıksın (banka bacağı), (b) kasada yeterli havale/kart payı yoksa bu ödemeler engellensin. (b) bugün meşru akışları kırar; öneri (a).
- **Mevzuat:** Bu pakette yasal eşik/oran yazılmadı. Nakit kapısı mevzuat hükmü değil, kasa fiziği kuralı ("çekmecede olmayan nakit verilemez").
