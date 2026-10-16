# PAKET TOPLU-4 — Uygulama raporu (toplu tasarım kuyruğu, hasımsız)

Taşkın + Ertuğ (Opus). Tarih 02.10.2026, tur 1 10:44–12:05 · **tur 2 12:33–13:20** (tasarım hakemi 02.10 bulguları). Dalga TOPLU-4 · t1 · API 5160 / vekil 5161 / CDP 9701–9705 · `KUYUMCU_TEST_DB=kuyumcu_test_t1`.
Kaynak: `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` bölümleri "01.10 19:50 TOPLU-2 hakem t2", "01.10 20:00 SUBE-KAPSAM-B t2", "01.10 21:19 TOPLU-2 kapanışı", "02.10 05:57 TOPLU-3 t2".

## 0. Devralma (neden durmuştuk)

- 07:47: ilk uygulayıcı internet kesintisiyle (ENOTFOUND) düştü. Rapor yazılmamıştı.
- 09:31: makine yeniden başladı ve `/private/tmp` silindi. Önceki ölçüm, görüntü ve mutasyon kanıtları gitti. Devir notunda geçen `ham/TOPLU-4-olcum` dizini **yoktu**; yeniden oluşturuldu.
- Ağaçtaki kod değişiklikleri yerinde duruyordu. Kod baştan yazılmadı; her madde ağaçta gözden geçirildi.
- Kaynakta yarım mutasyon izi arandı (`if (false`, `@if (true)`, `if (true)`): **0**. Mutasyonlar baştan yeniden koşuldu (23 adet).
- 5160/5161 ve CDP 9700–9720 portlarında kalmış süreç yoktu.
- Eski şeritten kalan `PAKETTOPLU-4-TEST Kuyumculuk` kiracısı (01M3X8VQ1S0Q8HXVHD6SFG9TB2) bu paketindi. Yeniden kullanıldı (kayıt ucu 3/saat sınırı) ve iş sonunda silindi.

## 0b. Tur 2: tasarım hakemi bulguları (02.10, 12:33–13:20)

Tur 1 bitince tasarım hakemi /raporlar/vade-takibi'de iki KRİTİK ve bir kapsam dışı bulgu yazdı. Tur 2 yalnız bunları ele aldı. Tur 1'in kodu ve kanıtları yerinde; baştan yazılmadı.

| Bulgu | Önce (hakem fotosu) | Sonra (başsız Chrome, 12 bileşim) | Kanıt |
|---|---|---|---|
| **K1** "Vadesi bilinmeyen" kartında geliştirici dili, 6 satır, kart 200 px | Alt metin "…bu alan eklenmeden önce yazılmış kayıtlardan geliyor; bugüne yazılmaz." | Alt metin **"Vadesi girilmemiş"**, 1 satır. Açıklama ⓘ içinde: "Bu borçlar için vade günü girilmemiş; bu yüzden gecikmiş ya da yaklaşan sayılmaz." Kart 127–146 px, komşu kartlarla aynı boyda. Ekranda "eklenmeden" sözcüğü 0. | `VadeTakibi.razor` (#vade-vadesiz-kart). StatKart işaretlemesi sayfada yazıldı; ortak bileşene parametre açılmadı. `Sozlukler.Toplu4.cs`. Test `T4_Vade_vadesiz_karti_KISA_alt_metin_aciklama_ipucunda` (BASİT+PRO) |
| **K1** "Bu kayıt değiştirilemez" kutusunda 3 satır açıklama | Başlık + gerekçe + "Düzeltme şöyle yapılır: Rapor rakamları…" | 1440/1280'de **tek satır**, 45 px: "Bu kayıt değiştirilemez · Rakamlar işlemlerden gelir; burada düzeltilmez." + ⓘ. Ters kayıt yolu ⓘ içinde aynen duruyor. 375'te iki satır (351 px kutu, sözcük sınırından kırılıyor). `.degismez-alternatif` 0. | `VadeTakibi.razor` (#vade-degismez-satir), `toplu4.css` (.t4-degismez-satir). Test `T4_Vade_degismez_notu_TEK_satir_duzeltme_yolu_ipucunda` |
| **K1** Durum hücresinde ham "VADESİNDE" | "VADESİNDE", "GECİKTİ", "YAKLAŞIYOR" | "Vadesinde", "Gecikti (12 gün)", "Yaklaşıyor (n gün)". EN: "On time / Overdue / Due soon". Ekranda ham büyük harf kod 0. | `Sozlukler.cs`: yalnız 6 DEĞER değişti, yeni anahtar yok. Bu anahtarlar yalnız VadeTakibi'de kullanılıyor. Testler `T4_Vade_durum_hucresi_ham_kod_degil_sozlukten`, `VadeEkranTestleri.Gecikme_gun_sayisi_gosterilir` (beklenti "Gecikti (12 gün)" olarak güncellendi) |
| **K2** Yetim "Kaç gün ilerisi ⓘ" satırı + 180 px boş kart | Kutunun altında ikinci bir etiket satırı vardı; segment ve kutu aynı seçimi yapıyordu | Yetim satır **kalktı**. ⓘ gün segmentinin yanında (1440: x=772, "60 gün" düğmesinin 8 px sağında). Serbest gün kutusu **"+ Başka gün"** metin bağlantısının arkasında ve kapalı geliyor (44 px dokunma hedefi, kutu çerçevesi yok). Kapalıyken "Kaç gün ilerisi" ekranda 0 kez, açıkken 1 kez görünüyor (kutunun etiketi). Kullanıcı kutuya yazınca kat açık kalıyor; 30 yazınca da kapanmıyor. Kart kabı kalktı. | `VadeTakibi.razor` (#vade-baska-gun, BaskaGunAcik), `toplu4.css`. Testler `T4_Vade_ipucu_gun_seciminin_YANINDA_yetim_satir_YOK`, `T4_Vade_serbest_gun_kutusu_Baska_gun_katinda_KAPALI_yazinca_acik_kalir`, `T4_Vade_ipucu_tetigi_filtre_yuvasinda_genislemez_dar_ekranda_yuva_genisliginde_acilir` |
| KAPSAM DIŞI: PRO Stok yaşı sekmesinde ham API alan adı (`altinHasGram`, `madenDagilimi`) | — | **Yapılmadı, hasımlı pakete devredildi.** Metin sunucudan geliyor: `StokYaslandirmaRaporu.cs:262`, `RaporModulu.cs:1157`, `RaporOrtak.cs:211`. RaporModulu.cs PARA-DEMO şeridinde; bu paket ona dokunmadı. | § 11 |

**Kimlikler:** mevcut id'ler korundu (#vade-ufuk, #vade-ufuk-7/30/60, #vade-ufuk-ipucu, #vade-ufuk-aciklama, #degismez-ipucu-VadeTakibi). Kaldırılan tek id `#vade-ufuk-ipucu-satir`. Bu id'yi tur 1'de bu paket eklemişti ve yetim satırın kabıydı; kalkması bulgunun çözümü. Yeni id'ler: #vade-baska-gun, #vade-baska-gun-ac, #vade-vadesiz-kart, #vade-vadesiz-alt, #vade-vadesiz-ipucu, #vade-vadesiz-aciklama, #vade-degismez-satir, #vade-degismez-ipucu, #vade-degismez-aciklama.

**Tur 2 içinde bulunup düzeltilen ara kusur:** ilk ölçümde 12 bileşimin hepsinde K3 "kırık etiket" sayacı 2 çıktı. ⓘ'yi saran tek çocuklu `span`, kapalı ⓘ'nin gizli paragrafıyla birlikte ölçerde iki satır sayılıyordu. Sarmalayıcı kaldırıldı; ⓘ doğrudan filtre yuvasına ve değişmezlik satırına kondu. 375'teki 44 px dokunma hedefi kuralı bu iki seçiciye de yazıldı. Yeniden ölçümde K3 0/0 çıktı. İlk ölçüm `ham/TOPLU-4-olcum/t2/vade-sarmalayicili.jsonl` dosyasında.

### Tur 2 ölçüm tablosu (/raporlar/vade-takibi · `sikisma-olc.js` + hedefli ölçüm)

| Mod | Tema | Genişlik | K1 gizli | K3 taşan/kırık | K5 TRY | K7 | K8 jargon | K9 binen | K10 | K12 | Yatay | Vadesiz kart boy · alt satır | Değişmez not boy · gerekçe satır | ⓘ katmanları [sol, sağ] / ekran |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASİT | açık | 1440 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 146 · 1 | 45 · 1 | [772,1028] [888,1144] [1160,1416] / 1440 |
| BASİT | açık | 1280 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 130 · 1 | 45 · 1 | [772,1028] [922,1178] [1000,1256] / 1280 |
| BASİT | açık | 375 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 144 · 1 | 83 · 2 | [12,363] [29,346] [12,363] / 375 |
| BASİT | koyu | 1440 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 146 · 1 | 45 · 1 | 1440 açık ile aynı |
| BASİT | koyu | 1280 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 130 · 1 | 45 · 1 | 1280 açık ile aynı |
| BASİT | koyu | 375 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 144 · 1 | 83 · 2 | 375 açık ile aynı |
| PRO | açık | 1440 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 127 · 1 | 45 · 1 | [772,1028] [888,1144] [1160,1416] / 1440 |
| PRO | açık | 1280 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 130 · 1 | 45 · 1 | [772,1028] [922,1178] [1000,1256] / 1280 |
| PRO | açık | 375 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 144 · 1 | 83 · 2 | [12,363] [29,346] [12,363] / 375 |
| PRO | koyu | 1440 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 127 · 1 | 45 · 1 | 1440 açık ile aynı |
| PRO | koyu | 1280 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 130 · 1 | 45 · 1 | 1280 açık ile aynı |
| PRO | koyu | 375 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 144 · 1 | 83 · 2 | 375 açık ile aynı |

- K2 (en sağdaki kartın ana alana oranı): 1440 ve 1280'de %98, 375'te %97. Dar sütun yok.
- Durum hücreleri 12 bileşimde de "Vadesinde, Vadesinde"; ham kod 0.
- "+ Başka gün" 12/12 bileşimde kapalı geliyor, 108×44 px.
- ⓘ tetiği 1440/1280'de 20 px daire, 375'te 44×44 dokunma kutusu.
- Görüntülere Read ile bakıldı: `t2/foto/vade__BASIT_aydinlik_1440.png`, `vade-acik__PRO_aydinlik_1440.png` (kat ve ⓘ açık), `vade-acik__PRO_karanlik_375.png`. 24 görüntünün tamamı `ham/TOPLU-4-olcum/t2/foto/` altında.
- 375 notu: segment düğmeleri 375'te tam genişlikte (app.css'in mevcut kuralı), bu yüzden ⓘ tetiği segmentin altında kendi satırında duruyor. Katman yuva genişliğinde açılıyor, taşma yok.

### Tur 2 canlı veri (API 5160 + vekil 5161, ürün uçlarıyla tohum)

- Kiracı `PAKETTOPLU-4-TEST T2 Kuyumculuk` (01M3Y023Q7G8FTTMT8C93TC1Y9), `demo-tohumla.js --prova-kiraci` ile ürünün kendi uçlarından kuruldu: 6 cari, 12 ürün, 9 satış. Sistem anahtarı bu oturum için rastgele üretildi ve yalnız ortam değişkeniyle verildi. Değeri hiçbir çıktıya yazılmadı; dosyası iş sonunda boşaltıldı.
- Vadesiz bakiye için:
  - `POST /cari` cari kimliğini döndürdü.
  - `POST /cari/{id}/duzeltme` (+12.500 TL) ilk denemede kurSnapshotId'siz gönderildi ve **400** "Kur kaydı zorunludur" döndü. Bu doğru bir ret.
  - `/kur/guncel` kimliğiyle yeniden gönderilince **200** döndü.
- `GET /raporlar/vade-takibi?ufukGun=7` **200** döndü:
  - vadesizToplam 12500.0 · ufukOtesiToplam 32570.73 · acikToplam 45070.73
  - Tuna Akarsu: vade 2026-10-17, gecikmeGun −15, VADESINDE
  - Kemal Ardıç: vade 2026-11-01, gecikmeGun −30, VADESINDE
  - Ekrandaki kartlar (12.500 ₺ · 32.570,73 ₺ · 45.070,73 ₺) ve "Vadesinde" satırları bu yanıtla birebir aynı.
- SQL (`hareketler` ⋈ `cariler`, bu kiracı), `ham/TOPLU-4-olcum/t2/sql-vade.txt`:
  - Kemal Ardıç: SATIS 14932.75 veresiye, vade 2026-11-01 + TAHSILAT −4480.00 (açık kalan 10452.75)
  - Tuna Akarsu: SATIS 22117.98 veresiye, vade 2026-10-17
  - Vadesiz Bakiye Müşterisi: CARI_DUZELTME 12500.00, vade NULL
- Kapanış:
  - Vekil ve API `kill` ile durduruldu. 5160, 5161 ve CDP 9710/9711'de dinleyici yok.
  - `zsh kiraci-temizle.sh 01M3Y023Q7G8FTTMT8C93TC1Y9 --uygula` kod 0 ile çıktı (75 tablo tarandı).
  - SQL: bu kimlik için tenants 0, cariler 0, hareketler 0, kullanicilar 0.
  - **tenants: tur 2 başında 16, sonunda 16.** QA-TEST-* ve başka şeritlerin kiracılarına dokunulmadı.

### Tur 2 testler, mutasyon, denetim

- Yeni testler (Toplu4Testleri.cs): `Toplu4VadeTestleri` sınıfında 4 metot (3'ü BASİT+PRO Theory, toplam 7 koşum) ve `Toplu4KaynakTestleri`'nde 1 CSS testi. Toplam **5 yeni metot, 8 koşum**. `VadeEkranTestleri.Gecikme_gun_sayisi_gosterilir` testinin beklentisi güncellendi.
- Filtreli koşum (Toplu4 · VadeEkran · UX1C3Ekran · OrtakGorunumHakem · DuzeltmeEkran · VadeTakibi): **133/133 yeşil**.
- **Mutasyon: 10 mutasyonun hepsi en az bir testi kırmızı yaptı; geri alınınca `cmp` AYNI.**

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M8b-1 | Vadesiz alt metnini eski uzun açıklamaya döndür | `T4_Vade_vadesiz_karti_KISA_alt_metin_aciklama_ipucunda` (BASİT, PRO) |
| M8b-2 | ⓘ'nin önüne yetim "Kaç gün ilerisi" satırını geri koy | `T4_Vade_ipucu_gun_seciminin_YANINDA_yetim_satir_YOK` (BASİT, PRO) |
| M8b-3 | "+ Başka gün" katını hep açık getir | `T4_Vade_serbest_gun_kutusu_Baska_gun_katinda_KAPALI_yazinca_acik_kalir` |
| M8b-4 | Yapışkan bayrağı kaldır (30 yazınca kat kapanır) | aynı test |
| M8b-5 | Değişmezlik notuna düzeltme yolu paragrafını geri ekle | `T4_Vade_degismez_notu_TEK_satir_duzeltme_yolu_ipucunda` (BASİT, PRO) |
| M8b-6 | Değişmezlik gövdesini `display: block` yap (iki satır) | aynı test (BASİT, PRO) |
| M8b-7 | ⓘ tetiğinden `flex: 0 0 auto` kaldır (yuva onu 220 px'e genişletir) | `T4_Vade_ipucu_tetigi_filtre_yuvasinda_genislemez_dar_ekranda_yuva_genisliginde_acilir` |
| M8b-8 | Dar ekrandaki ⓘ `position: static` kuralını sil | aynı test |
| M8b-9 | "Vadesinde" değerini "VADESİNDE"ye döndür | `T4_Vade_durum_hucresi_ham_kod_degil_sozlukten` (BASİT, PRO) |
| M8b-10 | 375'teki ⓘ 44 px dokunma hedefi kuralını sil | `T4_Vade_ipucu_tetigi_filtre_yuvasinda_genislemez_…` |

- Mutasyon izi taraması (`open="@true"`, `baskaGunAcildi = false`, `VADESİNDE`): 0. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata**, 2 uyarı. Uyarılar başka şeritlerin dosyalarında (§ 3).
- **Tam süit, tur 2** (son kod, 13:05, `--logger trx`, `/private/tmp/kuyumcu-serit/trx-pTOPLU-4-t2`): **9263 test · 9259 geçti · 1 başarısız · 3 atlandı.**
  - Başarısız olan: `Dogrulayici.PARADEMO2SinirTestleri.S16_elle_kart_cikisi_kart_payini_asamaz_1200_400`. Beklenen BadRequest, gelen OK; kasa kart payı kapısını sınıyor.
  - Test dosyası 12:13'te PARA-DEMO hakemince yazıldı ve kasa uçlarını sınıyor. Bu paket kasa ya da API koduna dokunmadı; tur 2 değişikliği yalnız VadeTakibi.razor, toplu4.css, iki sözlük dosyası ve iki test dosyasında.
  - **Sahibi: PARA-DEMO. Bu paketin ürettiği yeni kırmızı: 0.**
  - Tur 1'deki 3 kırmızı (PD6 ve iki kararsız migration testi) bu koşumda geçti.
- **denetim.sh** (`bash` ile): 303 ✓, 1 ✗. Tek ihlal `P-30: DEMO_INSAN_SURESI (BK-45)`: kronometreli demo provası istiyor, bu paketle ilgisi yok ve kabulde P-30 hariç tutuluyor. P-37c PANEL_AKIS_DISI 0 ✓. Çıktı `ham/TOPLU-4-olcum/t2/denetim-t2.log`.

## 1. Madde tablosu (öncelik sırası)

"Önce" sütunu kaynak kuyruktaki hakem ölçümüdür; eski ölçüm dosyaları `/private/tmp` ile silindi. "Sonra" sütunu bugün başsız Chrome'da yapılan ölçümdür: 1440/1280/375 · BASİT+PRO · açık+koyu, `olc-t4.mjs` ve `tur.mjs`.

| # | Madde | Önce | Sonra (gerçek tarayıcı) | Kanıt |
|---|---|---|---|---|
| 1 | **KRİTİK** Panel "Kim borçlu?" | 375'te uzun ad harf harf kırılıyor, "Biz borçluyuz" tutara biniyor; 1440'ta has tutarı "Tahsil et" düğmesinin arkasında | 12 BASİT bileşimin hepsinde **harf harf kırılma 0, binme 0** (ad↔tutar, durum↔tutar, tutar↔eylem). TL ve has iki ayrı satır, ikisi de tam görünür, taşma yok. Uzun ad 1440'ta 4 satır, 1280'de 2 satır, sözcük sınırından kırılıyor. 375'te iki satırlı kart: ad+eylem / durum+tutar. Yatay kaydırma 0. PRO panelde bu kart yok (`.panel-sade`). | `Panel.razor`, `toplu4.css` T4-1. Testler `T4_Kim_borclu_TL_ve_has_AYRI_satir_parcalari`, `T4_Kim_borclu_css_tutar_sutunu…`. Görüntüler `foto/panel__BASIT_aydinlik_1440.png`, `panel__BASIT_karanlik_375.png` (Read ile bakıldı) |
| 2 | Kurulum geliştirici notları | "Stok açılışı (bu sürümde yok)", PRO "kararı beklemede … tutucudur", "KURU PROVA", "sunucu" | Ekranda bu sözcükler **0** (12 bileşim). Kilit sebebi düğmenin ekrandaki adını söylüyor ("Kontrol et"). PRO negatif açılış metni: "…Kredili mevduat (KMH) ayrı bir üründür ve açılışta eksi bakiye olarak girilmez." Çoğunu TOPLU-3 kapatmıştı; kalan kilit sebebi ve PRO metni burada. | `Kurulum.razor:616`, `Sozlukler.Toplu2.cs`. Testler `KurulumEkranTestleri.Prova_temiz_degilken…`, `T4_Sozluk_TR_EN_dolu_ve_jargonsuz`. Görüntü `foto/kurulum__BASIT_aydinlik_1280.png` |
| 3 | Vitrin Panosu açıklamaları + "Sunucu sınırı:" | Açıklama satırları açıkta, "Sunucu sınırı:" öneki; rozet ve "Varsayılan" sütunu BASİT'te de görünüyor | "Sunucu" sözcüğü 0. Tavan notu ve ayar açıklamaları tek ⓘ içinde. BASİT: rozet 0, 7 sütun. PRO: rozet 1, 8 sütun ("Varsayılan" dahil). 14 px altında `p` 0. | `Vitrin.razor`, `Sozlukler.Toplu4.cs`. 4 test. Görüntü `foto/vitrin__PRO_karanlik_1280.png` |
| 4 | Siparişler Yeni formu | İki vazgeç düğmesi, "oturumdan" rozeti, bölünmüş süzgeç, BASİT'te tek seçenekli Tür grubu | Form açıkken üstteki düğme gizli; görünen vazgeç 1. BASİT: rozet 0, tür grubu yok. PRO: tür grubu durum süzgeciyle aynı şeritte. | `Siparisler.razor`. 3 test (Theory dahil). Görüntü `foto/siparis__PRO_aydinlik_1280.png` |
| 5 | Liste ekranlarında 12 px "En çok 50/100 kayıt" notu (Cariler hariç) | Ayrı 12 px paragraf | /urunler: "12 kayıt gösteriliyor · en çok 50", **14 px**, sayaç satırında. Daraltma cümlesi yalnız sınıra ulaşınca, aynı satırda. Aynı düzen UrunEtiketleri'nde de var. Diğer listelerde (POS vb.) not zaten yalnız sınıra değince çıkıyor. | `Urunler.razor`, `UrunEtiketleri.razor`. 2 bUnit testi, 1 CSS testi |
| 6 | Eylem düğmesi üstünde ~60 px boş bant | Etiketsiz şeritte etiket payı | /faturalar (etiketsiz şerit): `padding-top` **0 px**, düğme şeridin üstünden 12 px aşağıda (12 bileşim). Etiketli şeritte P-37 hizası değişmedi (app.css). | `toplu4.css` T4-6. Test `T4_Etiketsiz_seritte…` |
| 7 | "Yeni X" düğme dili | Karışık büyük harf | Cümle düzeni: "Yeni ürün", "Yeni fatura", "Yeni kullanıcı" … (9 anahtar) | `Sozlukler.cs` (yalnız DEĞER; yeni anahtar yok). Test `T4_Yeni_x_dugme_dili…` |
| 8 | Kur / Vade / Vitrin / Uyum 12 px açıklama satırları; Kur rozeti | Kur rozeti 12 px yetim satırda, açıklama yalnız `title`'da | Kur rozeti **14 px**, kaynak satırında, yanında ⓘ (1440/1280'de aynı satırda; 375'te alt satıra sarıyor). Vade ufuk açıklaması ve Uyum risk açıklamaları ⓘ içinde. **Bu turda düzeltildi:** 375'te ⓘ katmanı ekranın solundan taşıyordu (/kur x=−6, /vade x=−91). Katman artık satıra bağlı: /kur [54–321]/375, /vade [29–346]/375. Tur ölçerinde iki rota temiz. | `Kur.razor`, `VadeTakibi.razor`, `Uyum.razor`, `toplu4.css` (T4-2 dar ekran). Testler `T4_Kur_ucretsiz…`, `T4_Uyum_ve_Vade…`, `T4_Kalan_12px…`, yeni `T4_Dar_ekranda_ipucu_katmani…`. Görüntüler `foto2/kur__*_375.png`, `foto2/vade__*_375.png` |
| 9 | Hurda "Daha fazla" açıcısı | Tam genişlik, oklu, form alanı gibi | "+ Alıcının kabulü, fiyat, ramatçı ve tahsilat ekle" metin bağlantısı. Kenar 0, 44 px dokunma, 16 px. 1440'ta 349 px genişlik (ana alan 1172). Açıkken "Daha az göster" detayın **üstünde**, ok 180°. | `toplu4.css` T4-9, `Sozlukler.ZoIslem.cs`. Test `T4_Hurda_acici…`. Görüntü `foto/hurda__*` |
| 10 | Uyum PEP kartı 3+3 düzeni | PEP kartı genişleyince boş hücre | 1440/1280: 6 kart, 3 sütun × 2 satır. 375: 2 sütun × 3 satır. | `toplu4.css` T4-10. Test `T4_Uyum_risk_ozeti_3_arti_3…` |
| 11 | Yaşlandırma eksi tutar rengi + has notu ⓘ | 0-30 dilimindeki eksi tutar yeşil; tablo altında açıklama paragrafı | PRO yaşlandırma: eksi hücre 2 adet, açık temada `rgb(192,38,45)`, koyuda `rgb(255,170,162)` (tehlike belirteci). Artı tutar metin renginde. Has notu ⓘ içinde; `p.yas-not` 0. (Yaşlandırma yalnız PRO'da.) | `Raporlar.razor:1826`, `toplu4.css`. Testler `T4_Eksi_tutar_rengi…`, `T4_Has_notu…` |
| 12 | Kurulum adım listesindeki sayılar | Açıklamasız rakam, kaymış satır | "2 şube", "1 kullanıcı", "344 kur kaydı", "1 kasa ya da banka hesabı", "2 müşteri açılışı". Tek başına duran rakam 0. Masaüstünde sabit 4 sütun. | `Kurulum.razor:981-985`, `Sozlukler.Toplu4.cs`, `toplu4.css`. 2 test |
| 13 | P-37c PANEL_AKIS_DISI sayacı + P37c test metni | Metin app.css'teki eski kuralı anlatıyordu | Sayaç **gevşetilmedi** (0). Metin: "taban akışta · şeritte T2-16 tetiğe bağlı katman + perde z40<41". P37c testi "BUGÜNKÜ DAVRANIŞ İKİ KATMANDIR" açıklamasını taşıyor. denetim.sh ✓. | `denetim.sh` (yalnız metin), `P37cPanelVeKapsamTestleri.cs`. Test `T4_P37c…` |
| 14a | SUBE-B: PRO kullanıcı kartında şube | Çok şubeli SAHİP'te kart şubeyi yazmıyordu | PRO 12 bileşimin hepsinde kart "· Merkez Şube" yazıyor (`#ust-sube-gorunum`, ipucu "Bakılan şube — menüden değiştirilir"). | `SubeSecici.razor`. Test `T4_Cok_subeli_SAHIP…` |
| 14b | SUBE-B: İşlem defterinde şube iki kez | "Gösterilen şube: X" + süzgeçte X | **Ağaçtaki düzeltme gerçek tarayıcıda TUTMUYORDU.** `:has(#id-sube)` kuralı süzgeç akordeonu kapalıyken işlemiyor, çünkü `#id-sube` o zaman DOM'da yok. Ölçüm: rozet görünür 1, "Gösterilen şube: Merkez Şube" ve "Süzgeç: Merkez Şube" ikisi birden ekranda. **Düzeltme:** rozet `#id-rozet-kap` sarmalayıcısına alındı (`display: contents`). Şube seçiliyse sınıf `id-rozet-suzgecte` rozeti gizliyor; bileşen yine çiziliyor, `Degisti` geri çağrısı değişmedi. Yeniden ölçüm: 12 bileşimde rozet **0**, şube üst bölgede bir kez ("Süzgeç: Merkez Şube"). Tüm şubeler görünümünde rozet kalıyor (test). | `IslemDefteri.razor`, `toplu4.css`. Yeni testler `T4_Defter_sube_seciliyken_rozet_sarmalayicisi…`, `T4_Defter_tum_subelerde_rozet_GORUNUR_kalir`. Görüntü `foto2/defter__PRO_karanlik_1280.png` (önce: `foto/defter__PRO_aydinlik_1280.png`) |

## 2. Sapmalar ve gerekçeleri

- **14b:** önceki uygulayıcının CSS düzeltmesi bUnit/kaynak testinden geçiyordu ama gerçek tarayıcıda işe yaramıyordu. Kurucunun gerçek tarayıcı kuralı tam olarak bunu yakaladı. Razor sarmalayıcısıyla kapatıldı; eski `:has(#id-sube)` kuralı akordeon açıkken geçerliliğini koruyor.
- **8:** ⓘ katmanının 375'te ekran dışına taşması kaynak kuyrukta yoktu; ölçüm sırasında bulundu ve bu madde kapsamında düzeltildi (`.t4-ipucu-satir`, ≤640 px).
- Para, veri ve yasal davranışa dokunulmadı. Hasımlı pakete devredilen bir şey yok.
- Hiçbir PARA-DEMO dosyasına dokunulmadı. Sozlukler.cs HUB'una yeni anahtar eklenmedi (madde 7 yalnız değer).

## 3. Testler

- Toplu4 testleri: **33/33 yeşil**. Önceki ağaçta 28 metot vardı; yeni eklenenler: 2 bUnit (defter) + 1 CSS (ⓘ dar ekran).
- Filtreli koşum (Toplu4|KurulumEkran|P37c|Toplu3|Toplu2): 134/134 yeşil. Bu, defter testleri eklenmeden önceki sayıdır.
- **Tam süit, tur 1** (10:52, eski kod): 9176 test · 9173 geçti · **1 başarısız** · 2 atlandı. Başarısız olan: `ParaDemo2Testleri.PD2_3_nakit_1000_kart_500…`. Bu PARA-DEMO şeridinin `KasaBakiyeKapisi.cs` dosyasıydı, 10:53'te o şerit tarafından düzenleniyordu; tur 2'de geçti.
- **Tam süit, tur 2** (son kod, 12:0x): 9242 test · 9237 geçti · **3 başarısız** · 2 atlandı:
  - `ParaDemoTur2Testleri.PD6_kart_payini_ASAN_virman_400…`: beklenen "500.00", gelen "500,00". `VirmanModulu.cs` 11:35'te PARA-DEMO şeridince değiştirildi. **Sahibi: PARA-DEMO.**
  - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` ve `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner`: "Unknown database kuyumcu_t1_eski/_hazir". İkisi tek başına yeniden koşuldu: **2/2 geçti**. Kararsız (yan veritabanı yarışı).
  - **Bu paketin ürettiği yeni kırmızı: 0.**
- Derleme: tur 1 `dotnet build Kuyumcu.sln` 0 hata, 2 uyarı. Uyarılar başka şeritlerin dosyalarında: `KesifP2Testleri.cs:455` xUnit2029 ve `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` CS8601. Son temiz derleme 0 uyarı, 0 hata.

## 4. Mutasyon tablosu (23 mutasyon, hepsi kırmızı; geri alınınca `cmp` AYNI)

Yöntem: yedek `/private/tmp/kuyumcu-serit/TOPLU-4/mut/<kod>/`, `perl` ile tek eşleşme kontrollü değişiklik, filtreli test, geri kopyala, `cmp`. Sonuçlar `ham/TOPLU-4-olcum/mut/`. Son mutasyondan sonra temiz derleme yapıldı.

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1-1 | Panel tutar parçası `<span class="panel-tutar-parca">` kaldırıldı | T4_Kim_borclu_TL_ve_has_AYRI_satir_parcalari |
| M1-2 | `minmax(130px, max-content)` → `1fr` | T4_Kim_borclu_css_tutar_sutunu_icerik_kadar_dar_kapta_iki_satir |
| M2-1 | Kilit sebebi `T3.KurulumKontrolEt` → `ProvaCalistir` | KurulumEkranTestleri.Prova_temiz_degilken_Uygula_dugmesi_KAPALI_ve_sebebi_yazili |
| M2-2 | PRO KMH metnine "beklemede … tutucudur" | T4_Sozluk_TR_EN_dolu_ve_jargonsuz |
| M3-1 | Vitrin rozetinin `<ProAlan>` sarmalayıcısı kaldırıldı | T4_Vitrin_degistirilebilir_rozeti_ve_Varsayilan_sutunu_YALNIZ_PRO |
| M3-2 | Tavan notuna "Sunucu sınırı:" öneki | T4_Vitrin_tavan_notu…sunucu_oneki_yok + T4_Sozluk… |
| M4-1 | `hidden="@formAcik"` → `@(false)` | T4_Form_acikken_ustteki_dugme_gizli_TEK_vazgec (BASIT, PRO) |
| M5-1 | Sınır koşulu `>=` → `>` | T4_Urunler_sinirda_daraltma_cumlesi_ayni_satirda |
| M5-2 | `.t4-sayac` 14 px → `--yazi-xs` | T4_Kalan_12px_aciklama_ve_sayac_satirlari_en_az_14px |
| M6-1 | Etiketsiz şeritte `padding-top: 0` → etiket payı | T4_Etiketsiz_seritte_eylem_ustu_etiket_payi_SIFIR |
| M7-1 | "Yeni ürün" → "Yeni Ürün" | T4_Yeni_x_dugme_dili_cumle_duzeninde |
| M8-1 | `id="kur-kaynak-satir"` bozuldu | T4_Kur_ucretsiz_rozeti_kaynak_satirinda_ve_aciklama_ipucunda |
| M8-3 | ⓘ `position: static` → `relative` (dar ekran) | T4_Dar_ekranda_ipucu_katmani_satira_bagli_tam_genislik_ekran_disina_tasmaz |
| M9-1 | Hurda gizle oku `rotate(180deg)` → `none` | T4_Hurda_acici_metin_baglantisi_kalibi_gizle_tetigin_yerinde |
| M10-1 | Uyum `repeat(3,…)` → `repeat(4,…)` | T4_Uyum_risk_ozeti_3_arti_3_dar_ekranda_2 |
| M11-1 | Eksi sınıfı koşulu `< 0m` → `> 0m` | T4_Eksi_tutar_rengi_ISARETE_bagli_dilim_rengine_degil |
| M11-2 | `--tehlike` → `--basari` | T4_Eksi_tutar_rengi_ISARETE_bagli_dilim_rengine_degil |
| M12-1 | Şube sayısı biçimi → çıplak `{0}` | T4_Kurulum_adim_sayilari_neyin_sayisi_oldugunu_soyler (BASIT, PRO) |
| M13-1 | denetim.sh P-37c metni eskiye döndürüldü | T4_P37c_seritte_T216_tetige_bagli_katman_ve_perde_altta |
| M14-1 | `SeciciGorunur &&` → `false &&` | T4_Cok_subeli_SAHIP_kartinda_bakilan_sube_yazar |
| M14-2 | `:has(#id-sube)` rozet `display: block` | T4_Islem_defteri_sube_suzgeci_varken_gorunum_rozeti_gizli |
| M14-3 | Sarmalayıcı sınıfı `id-rozet-suzgecte` boşaltıldı | T4_Defter_sube_seciliyken_rozet_sarmalayicisi_gizler_suzgec_basligi_subeyi_yazar |
| M14-4 | `.id-rozet-suzgecte > p` `display: block` | T4_Defter_sube_seciliyken_rozet_sarmalayicisi… |

## 5. denetim.sh

`dotnet/denetim.sh` → 303 ✓, **1 ✗**. Tek ✗ şu satır: `P-30: DEMO_INSAN_SURESI (BK-45)` (kronometreli demo ölçümü; kabulde **"P-30 hariç"** muaf, bu paketle ilgisiz). P-37c üç sayacı ✓: `PANEL_AKIS_DISI 0`, `SAYFA_BASI_KAPSAM 46/46`, `ARTIK_DOLU_BAYT 0`. Tam çıktı: `ham/TOPLU-4-olcum/denetim.log`.

## 6. Canlı doğrulama (API 5160 + SQL)

- API `dotnet run --no-build --urls http://localhost:5160` ile açıldı (PID dosyada tutuldu). Vekil `inceleme-vekili.py` ile 5161'de; fikstür chmod 600, iş sonunda `printf ''` ile boşaltıldı.
- `GET /raporlar/cari-yaslandirma` → 200. Satırlar: "Anadolu Döküm Toptan Kuyumculuk Sanayi ve Ticaret" tl 24423.82 / has 12.3 (dilim BELIRSIZ, ACILIS) · "Tuna Akarsu" 22096.97 · "Kemal Ardıç" 10442.56 …
- `GET /kurulum/durum` → 200: `sayilar {sube 2, kullanici 1, kur 339, kasa 1, banka 0, acilisCari 2}`. Ekrandaki "2 şube / 1 kullanıcı / … kur kaydı / 1 kasa ya da banka hesabı / 2 müşteri açılışı" bununla birebir. Kur sayısı ölçüm sırasında arttı (canlı kur).
- SQL: `cariler` uzun adlı müşteri (MUSTERI) ve "Anadolu Döküm Toptan Kuyum" (TEDARIKCI, Panel'de "Biz borçluyuz −3.840 ₺") · `subeler` 2.
- Başsız Chrome: tek Chrome + tek API, sırayla. Hedefli ölçüm 144 + 36 kayıt. `tur.mjs` 13 rotada 1424 kayıt (156 sayfa, 102 sekme, 1166 katman, hata 0, geçersiz 0); düzeltmeden sonra 4 rotada 254 kayıt yeniden.
- **Yeniden turdaki kalan bulgular** yalnız /kurulum "ekran-dışı" bulgularıdır, hepsi DİKEY. Akordeon ya da ⓘ katmanının alt kenarı görüntü alanının altında kalıyor, sayfa aşağı kayıyor; yatayda hepsi ekran içinde (ör. 1280: x 380–636, alt 821 > 800).
- Görüntüler Read ile bakıldı: panel 1440 açık, panel 375 koyu, hurda 1440, raporlar BASİT 1440, işlem defteri PRO 1280 (önce/sonra), sipariş PRO 1280, kurulum BASİT 1280, vitrin PRO koyu 1280, kurulum PRO 375 açıcı.
- Kanıt dizini: `dokumanlar/09-uygulama-kayitlari/ham/TOPLU-4-olcum/`. İçinde `foto/` (144) ve `foto2/` (36) görüntüleri, `hedefli-1.jsonl`, `hedefli-2.jsonl`, `hedefli-ozet.txt`, `tur-analiz.txt`, `tur2-analiz.txt`, `tur-olcum-1.jsonl`, `tur2-olcum-2.jsonl`, `mut/`, `denetim.log`, `tam-suit-ozet.txt` ve betikler.

**Kapanış:** API ve vekil `kill` ile durduruldu; 5160, 5161 ve CDP 97xx'te dinleyici yok. `zsh kiraci-temizle.sh 01M3X8VQ1S0Q8HXVHD6SFG9TB2 --uygula` kod 0 ile çıktı; ikinci koşum "Kiracı YOK" dedi. SQL: tenants, cariler, hareketler ve kullanicilar tablolarında o kiracıya ait 0 satır.

**tenants:** başlangıç 18. Bu sayıya bu paketin önceki denemeden kalan kiracısı ve PARA-DEMO-2 şeridinin 2 prova kiracısı dahildi. Son sayım 15: bu paket kendi 1 kiracısını sildi, PARA-DEMO-2'nin 2 kiracısını kendi şeridi sildi (bu paket yalnız kendi kimliğini verdi). QA-TEST-* ve başka şeritlerin kiracılarına dokunulmadı.

## 7. DoD sözleşmesi

Yeni varlık ya da kayıt türü yok. Yeni uç yok, migration yok (**gerekmedi**). Yeni form yok. Yeni düğme yok; Hurda açıcısı ve sipariş vazgeçi mevcut düğmeler, gerçek eylem yapıyorlar. Mevcut element id'leri değişmedi. Yeni id'ler: `id-rozet-kap` (önek `id-`).

## 8. Dokunulan sayfalar (TEK DOKUNUŞ kaydı)

Panel.razor (Kim borçlu kartı) · Kurulum.razor · Vitrin.razor · Siparisler.razor · Urunler.razor · UrunEtiketleri.razor · Kur.razor · VadeTakibi.razor (tur 2: aynı paketin hakem düzeltmesi, ikinci dalga dokunuşu değil) · Uyum.razor (+Uyum bileşenleri) · Hurda.razor · Raporlar.razor (yaşlandırma) · IslemDefteri.razor · Bilesenler/SubeSecici.razor.

## 9. Değişen dosyalar (bu paket)

`dotnet/Kuyumcu.Web/Pages/{Panel,Kurulum,Vitrin,Siparisler,Urunler,UrunEtiketleri,Kur,VadeTakibi,Uyum,Hurda,Raporlar,IslemDefteri}.razor`, `Bilesenler/SubeSecici.razor`, `wwwroot/css/toplu4.css`, `wwwroot/index.html` (yükleme sırası), `Kuyumcu.Core/I18n/Sozlukler.Toplu4.cs`, `Sozlukler.Toplu2.cs` (PRO KMH değeri), `Sozlukler.ZoIslem.cs` (DetayEkle), `Sozlukler.cs` (yalnız değerler: "Yeni x"; tur 2'de Vade Gecikti/Yaklasiyor/Vadesinde TR+EN), `Kuyumcu.Tests/Toplu4Testleri.cs`, `VadeEkranTestleri.cs` (tur 2, tek beklenti), `KurulumEkranTestleri.cs`, `P37cPanelVeKapsamTestleri.cs`, `dotnet/denetim.sh` (yalnız P-37c metni).

Ağacın çoğu git'te izlenmiyor (`??`), bu yüzden `git status --porcelain` paket ayrımı yapamıyor. Liste dosya içi `TOPLU-4` işaretlerinden ve tarih damgalarından çıkarıldı.

## 10. Koşul karşılama

| Koşul | Durum |
|---|---|
| 14 öncelik maddesi | KARŞILANDI (14b ve 8 tur 1'de gerçek tarayıcı bulgusuyla tamamlandı) |
| Tasarım hakemi 02.10 iki KRİTİK bulgusu (vade takibi) | KARŞILANDI (tur 2, § 0b: 12 bileşimde ölçüm 0, 10/10 mutasyon kırmızı) |
| Hakem KAPSAM DIŞI bulgusu (PRO Stok yaşı ham alan adı) | DEVİR: hasımlı paket (RaporModulu.cs PARA-DEMO şeridinde) |
| Başsız Chrome 1440/1280/375 · BASİT+PRO · açık+koyu, görüntü Read ile | KARŞILANDI |
| Her değişiklik için test + mutasyon | KARŞILANDI (23/23 kırmızı, cmp AYNI) |
| build 0 hata | KARŞILANDI |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI |
| Tam süit 0 yeni kırmızı | KARŞILANDI (tur 1: 3 kırmızı, 1 PARA-DEMO + 2 kararsız; tur 2: 1 kırmızı, PARA-DEMO hakem testi S16) |
| Test kiracısı silindi | KARŞILANDI |
| COMMIT yok, dosya silme yok, PARA-DEMO dosyalarına dokunulmadı | KARŞILANDI |

## 11. Açık kalanlar (sahibiyle)

- **Hasımlı pakete devir (tasarım hakemi 02.10, KAPSAM DIŞI):** PRO /raporlar Stok yaşı sekmesi ekrana ham API alan adı basıyor: "Stokta birden çok maden var. `altinHasGram` yalnız ALTIN has gramidir; diğer madenler `madenDagilimi` altında ayrı verilir". Sunucu notunun kaynağı: `Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs:262`, `RaporModulu.cs:1157`, `RaporOrtak.cs:211`. RaporModulu.cs PARA-DEMO şeridinde ve not bir rapor sözleşmesinin parçası; bu yüzden hasımsız görünüm paketi ona dokunmadı. Öneri: sunucu notunu kullanıcı diline çevirmek ya da istemcide sözlükle eşlemek.
- **PARA-DEMO (tur 2):** `Dogrulayici.PARADEMO2SinirTestleri.S16_elle_kart_cikisi_kart_payini_asamaz_1200_400` kırmızı (beklenen 400, gelen 200).
- **375 vade (bilgi):** segment düğmeleri 375'te tam genişlik olduğu için ⓘ kendi satırında duruyor; ölçer temiz. Tasarım hakemi ⓘ'yi "+ Başka gün" satırına almak isterse bu tek bir CSS işi.

- **PARA-DEMO:** `ParaDemoTur2Testleri.PD6_kart_payini_ASAN_virman_400…` kırmızı. Mesajın tr-TR biçimine geçişi testle uyuşmuyor (`VirmanModulu.cs` 11:35).
- **Kararsız test (orkestratör):** `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar`, `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner`. Tam süitte yan veritabanı yarışı var; tek başına geçiyor.
- **Kapsam dışı ölçüm bulguları (sonraki toplu tasarım):**
  - /kullanicilar 375 ve PRO'da "Düzenle" katmanı SERBEST-YÜZEN.
  - /raporlar PRO 1280/1440'ta `#rp-yil`/`#rp-ay` ve /urunler PRO'da `#urun-durum-filtre` ezik seçici (K12).
  - /kurulum PRO 375'te açılış formundaki `#kur-acilis-tur` listesi SERBEST-YÜZEN (form TOPLU-4 kapsamında değişmedi).
  - Panel BASİT 375 "K4 tren 2,1×5".
  - Uyum ve Vade stat kartı etiketleri 12 px (`stat-etiket`, ortak bileşen; açıklama değil etiket).
  - Rapor merkezi BASİT altındaki 12 px "Aylık özet … profesyonel görünümde" satırı.
  - Altın pozisyonu kartında (`AltinPozisyonuKarti.razor`) her zaman görünen "Sunucu bu listede en çok {0} kayıt döndürür" satırı (SUBE-B alanı).
  - Vitrin PRO 1280'de ayar tablosu kart listesine dönüyor ("Düzenle" ve "Varsayılan" ikinci satırda); ölçer temiz, ama tasarım hakemi bakmalı.
- `dotnet/denetim.sh.mutbak`: 0 bayt, 18.09 tarihli artık. Bu paketin değil; silme kurucuya bırakıldı.

## 12. Kurucuya / mali müşavire

Mevzuat hükmüne dokunulmadı; soru yok. Panel'deki yasal liste bandı (yasal liste yüklenmemiş) doğru bir fail-closed uyarısıdır; bu paket onu değiştirmedi.
