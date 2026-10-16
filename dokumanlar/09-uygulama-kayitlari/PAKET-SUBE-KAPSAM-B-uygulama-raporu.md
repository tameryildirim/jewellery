# PAKET ŞUBE-KAPSAM-B — Seçili şube bütün ekranı yönetir, İSTEMCİ tarafı — Uygulama raporu

> Tarih: 2026-10-01 (tur 3: 19:45–21:20) · Şerit: SUBE-KAPSAM-B / t1 (API 5160 · vekil 5161 · CDP 9701-9708 · test DB `kuyumcu_test_t1`)
> Rol: Ertuğ (frontend) + Taşkın (sözleşme okuma) · Uygulayıcı: Opus · COMMIT YOK
> Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-SUBE-KAPSAM-2026-09-30.md` (yalnız ŞK-B: § 4.3, § 4.4; kabul K-7…K-9, K-13, K-14; mutasyon M7, M8)
> Sunucu sözleşmesi (SK-A, kapıda) OKUNDU, DEĞİŞTİRİLMEDİ.

## 0. TUR 3 (01.10 19:45–) — doğrulayıcı bulgularının (tur 2 doğrulaması) kapanışı

> Tur 1 (§ 1–16) ve tur 2 (§ 0.1–0.6) bölümleri aşağıda AYNEN duruyor; bu bölüm tur 3'te yapılanları ve **son kodla yeniden üretilen** kanıtları verir. Çelişki varsa bu bölüm geçerlidir.

### 0.T3.1 Bulgu → yapılan
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| **[KRİTİK] Altın pozisyonu kartı seçili şubeyi değil tüm şubeleri gösteriyor** | SUNUCU: `GET /pozisyon?subeId=` — görünüm şubesi TEK yoldan çözülür (`GorunumSubesi.Coz`: pasif/yabancı şube 404, kilitli rol başka şube 403; TEZGÂHTAR rol kapısından zaten 403). `PozisyonServisi.Hesapla(islem, gorunumSubesi)`: VİTRİN + KONSİNYE (`Stoklar.Daralt`) ve HURDA (`HurdaStokKapisi.KiraciKirilimi(…, gorunumSubesi)` → `Daralt`, takas eski bacağı dahil) seçili şubeye daraltılır. CARİ alacak/borç + emanet müşteri hesabıdır, şubeye BÖLÜNMEZ (şartname § 1 yasal değişmez: "cari bakiye … kiracı genelinde") — iki durumda aynı; yanıt `kapsam` ("SUBE"/"TUM_SUBELER") + `cariKapsam` ("KIRACI_GENELI") taşır (sözleşmeye varsayılanlı iki alan; mevcut kurucular kırılmadı). İSTEMCİ: `AltinPozisyonuKarti` `SubeliSorgu.Ekle("/pozisyon")`, görünüm değişince yeniden çeker (eski şubenin geç yanıtı atılır), şube seçiliyken "Alacak · Borç: Tüm şubeler birlikte" notu (`#pozisyon-cari-kapsam`, Panel'in öteki kiracı geneli kartlarıyla aynı metin). | HTTP `SKB_P1/P2/P3`; bUnit `Pozisyon_karti_…`, `Panel_pozisyon_karti_…`; canlı API=SQL + Stok değeri ile aynı (§ 0.T3.5) |
| **[YÜKSEK] İşlem defterinde süzgeç başka şubeye çevrilince rozet yanlış şubeyi söylüyor; Reyon stok kutusunda da aynı ikili kaynak** | `GorunumRozeti` yeni `SuzgecKullan` + `SuzgecSubeId` parametreleri: sayfanın KENDİ şube süzgeci varsa rozet görünüm şubesini değil verinin gerçekten süzüldüğü şubeyi yazar (boş = "tüm şubeler"). İşlem defteri sayfa rozeti `SuzgecSubeId="@subeId"` taşır. Rapor merkezi Reyon stok kartı: kutu sayfanın görünüm şubesinden FARKLI şubeye çevrilince kart kendi rozetini (`#rs-kapsam-rozeti`) çizer; aynıyken ikinci rozet çizilmez. Kart rozetleri kapanmış-şube bandını tekrar etmez (yalnız `Id == "gorunum-rozeti"` sayfa rozeti). Görünüm şubesi DEĞİŞMEZ (süzgeç yalnız o sayfanın). | bUnit `IslemDefteri_suzgec_…`, `Raporlar_reyon_stok_…`, `Rozet_suzgec_kipi_…`; canlı defter senaryosu |
| **[YÜKSEK] Kapanan şube bandı her ekranda kalıcı, kurtulma yolu yok** | `KabukServisi.GorunumOku`: sunucu `gecersizTercih=true` derse çözülen değer (kendi şube / `TUMU`) tercihe BİR KEZ yazılır (`PATCH /tercihler/benim`) → bir sonraki açılışta bant çıkmaz. Bant "Tamam" düğmesiyle kapanır (`#gorunum-gecersiz-kapat` → `GecersizBandiKapat`: bütün sayfalarda kalkar). Temizleme yazımı düşerse sessiz değil: `#gorunum-gecersiz-hata` sebebi yazar. | bUnit `Kapanan_sube_tercihi_acilista_SUNUCUDA_temizlenir_…`, `…Tamam_ile_kapanir_baska_sayfada_da_cikmaz`, `…temizligi_duserse_sebep_gorunur`; canlı (§ 0.T3.5) |
| **[YÜKSEK] Hurda ekranında aynı anda iki hurda rakamı + 12 px açıklama satırı** | Satış OTURUM şubesinden yazılır (şartname § 1: görünüm şubesi yazma yolunu ETKİLEMEZ; sunucunun yazma kapısı `OturumSubesiCoz` SK-A/hurda modülündedir, değiştirilmedi). Görünüm kendi şubeden başkayken (başka şube ya da tüm şubeler) form İKİNCİ bakiye çizmez ve ikinci `/hurda/bakiye?subeId=<oturum>` isteği atılmaz; formun yerinde tek cümle + tek birincil düğme: "Hurda satışı yalnız kendi şubenizden (Merkez Şube) yapılır. Şu an başka şube gösteriliyor." · **"Kendi şubeme geç (Merkez Şube)"** → görünüm oturum şubesine döner (tercih yazılır; hata K-9 bandında), kart ile form aynı rakamı gösterir. Bant 14 px gövde metni, bilgi zemini (`bant-sube`), 12 px gri açıklama satırı kaldırıldı (`HurdaFormKendiSubende` metni artık kullanılmıyor — sözlükte duruyor). Mevcut `#hrd-form-sube-notu` kimliği korundu. | bUnit `Hurda_baska_sube_gorunurken_form_ikinci_bakiye_CIZMEZ_…`; canlı |
| **[YÜKSEK] Görünüm rozeti 12 px ve sıkışık** | Rozet artık hap değil (`rozet rozet-mavi` kaldırıldı): `.gorunum-rozeti` düz satır, `font-size: var(--yazi-md)` (açık ölçek 16 px, sıkı ölçek 14 px), 600 ağırlık, `var(--metin)` rengi, altta `--bosluk-3` boşluk. Yeni şerit CSS dosyası `css/sube-kapsam-b.css` (index.html'de `ortak.css`'ten önce). Kimlikler (`#gorunum-rozeti`, `#gorunum-rozeti-metin`) DEĞİŞMEDİ. | bUnit `Rozet_kucuk_hap_DEGIL_…` (sınıf + CSS bloğu + bağlantı sırası); canlı `getComputedStyle` ölçümü |
| **[ORTA] Menüdeki seçicide düzen zıplıyor, "Tüm şubeler" gri yer-tutucu, etiket teknik** | `SubeSecici`: "Tüm şubeler" artık BOŞ seçenek değil, GERÇEK seçenek (değer `TUMU` → `KabukServisi` null'a çevirir, sunucuya yine `TUMU` yazılır) → `yer-tutucu` sınıfı çizilmez. Etiket "Hangi şube?" / "Which branch?" (yeni sözlük anahtarı `HangiSube`; hub'daki `SubeKapsami` dokunulmadı). Zıplama: `app.css` `.sube-secici{align-items:center}` kenar menüsünde içerik boyuna daraltıp ortalıyordu → `css/sube-kapsam-b.css`: `.kenar-kullanici-menu .sube-secici{align-items:stretch}` + `.secici-alan{width:100%}`. `Secici.razor`'a DOKUNULMADI (TOPLU-1). | bUnit `Secici_etiketi_HANGI_SUBE_…`; `KabukAramaEkranTestleri.Coklu_subede_…` yeni etikete uyarlandı; canlı genişlik ölçümü |

### 0.T3.2 Sapmalar ve gerekçeleri (tur 3)
1. **Sunucu dosyasına dokunuldu (ŞK-B istemci şeridi, ŞK-A kapıda):** doğrulayıcının KRİTİK bulgusunun kabulü "GET /pozisyon şube parametresi" diyor; uç ŞK-A'nın 7 uç listesinde yoktu ve şube parametresi olmadan istemci tarafında kapatılamaz. Dokunulan: `Kuyumcu.Api/Modules/Pozisyon/PozisyonModulu.cs` (uç + servis parametresi), `Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs` (`KiraciKirilimi`'ne isteğe bağlı `gorunumSubesi` — varsayılan null = bugünkü davranış AYNEN; tek `SubeId ==` yeri yine `GorunumSubesi.Daralt`), `Kuyumcu.Contracts/Sayfalar/PozisyonSozlesmeleri.cs` (varsayılanlı iki alan). ŞK-A'nın `GorunumSubesi.Coz/Daralt/Kapsam` yardımcıları OKUNDU, DEĞİŞTİRİLMEDİ. Migration yok.
2. **Cari alacak/borç şubeye bölünmedi:** şartname § 1 ("cari bakiye … kiracı genelinde hesaplanır; şube seçimi bunlara asla sızmaz") + PAKET-46 kararı. Kart bunu not olarak söyler; tutarlılık denetimi (`defterBakiyeFarki`, `cari_bakiyeler` ↔ defter) kiracı geneli kaldığı için anlamını korur.
3. **Hurda formu seçili şubeden SATMAZ:** kabul metni "Ramata formu seçili şubenin hurdasını kullanır; tek rakam" diyor. Satış yolu oturum şubesinden yazar (`HurdaStokKapisi.OturumSubesiCoz`, ŞK-A/hurda modülü) ve şartname § 1 görünüm şubesinin yazma yolunu etkilemesini yasaklar. Ekip kararı (Ertuğ + Taşkın, Bertan'ın § 1 değişmezi): tek rakam şartı, form başka şubenin bakiyesini HİÇ çizmeyerek ve tek düğmeyle görünümü kendi şubeye çekerek sağlandı; görünüm = kendi şube iken form kartla AYNI rakamı (aynı yanıt) kullanır. Yazma yolunu görünüm şubesine açmak (SAHİP'in başka şubeden ramat satışı) ayrı bir sunucu kararıdır → § açık kalanlar.
4. **`index.html` (ortak dosya) bir satır:** yeni şerit CSS bağlantısı `kesif-p1.css`'ten sonra, `olcek.css`'ten ÖNCE (DUGME-2 / OL5 sırası "olcek → dugme → ortak" korunur). İlk denemede bağlantı `ortak.css`'ten hemen önceydi → tam süitte `OlcekTekTestleri.OL5…` ve `DugmeIkiTestleri.D2_01…` KIRMIZI verdi; taşındı, iki test yeniden koşuldu YEŞİL, denetim `DUGME2_BAGLANTI_SIRASI` ✓.
5. **Tek dokunuş (DoD EK 2) gerekçesi:** Hurda.razor, Raporlar.razor, IslemDefteri.razor bu paketin tur 1/2'sinde açılmıştı; tur 3'te AYNI paketin doğrulayıcı bulguları için yeniden açıldı (aynı dalga, aynı paket — ikinci dalga dokunuşu değil). `AltinPozisyonuKarti.razor` (B-2'nin bileşeni) ilk kez bu pakette açıldı: yalnız şube parametresi + yeniden yükleme + not satırı.
6. **5160 portunda bayat süreç:** tur 3 canlı doğrulamasına başlarken 5160'ı 18:19'da başlatılmış eski bir `dotnet run` (tur 2 doğrulayıcısının, eski ikili) tutuyordu; şeridin kendi portu olduğu ve sahibi işini bitirdiği için PID 47663/47669 `kill` ile durduruldu. 5199/5200 ve başka portlara dokunulmadı.

### 0.T3.3 Build / test / denetim (son kod)
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra): **0 Hata · 0 Uyarı** (`t3-b-temiz.log`); son not düzeltmesinden sonraki tam derleme **0 Hata · 2 Uyarı** — ikisi de başka şeritlerin test dosyalarında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65)` CS8601, `KesifP2Testleri.cs(454,9)` xUnit2029), bu paketin değil (`t3-b8.log`). İlk denemede paralel şeridin aynı anda `Kuyumcu.Web` derlemesi MSB4018 (WebCil dosya kilidi) verdi; yeniden koşuldu, yeşil.
- Filtreli (`GorunumSubesiEkranTestleri | SubeKapsamBPozisyonHttpTestleri | SUBEKAPSAMBSinirTestleri | KabukAramaEkranTestleri`): **98/98 geçti** (son kod).
- Etkilenen aileler (… `|AltinPozisyonu|Hurda|IslemDefteri|CumaJ|S1hGorsel|ORTAK1Sinir|TK2Kabuk|P37Revizyon|Raporlar|Panel`): 612 test — tek kırmızı yeni HTTP testinin Türkçe gövde kontrolüydü (`Ş` kaçışı; kural gereği gövde AYRIŞTIRILARAK sınanacak şekilde düzeltildi) → yeniden 3/3 yeşil.
- **Tam süit (tek koşum, son kod):** `KUYUMCU_TEST_DB=kuyumcu_test_t1 dotnet test … --no-build -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-B-tur3` → trx `_Mac_2026-10-01_20_56_02.trx`: **total 9041 · executed 9039 · passed 9033 · failed 6 · 2 atlandı**. Kırmızılar ve sahipleri:
  | Test | Sebep | Sahibi |
  |---|---|---|
  | `OlcekTekTestleri.OL5_index_olcek_cssi_ortak_cssten_HEMEN_ONCE_yukler` | o anki `index.html`'de bu paketin bağlantısı olcek↔ortak arasındaydı | **BU PAKET → düzeltildi** (sapma 4); yeniden koşum YEŞİL |
  | `DugmeIkiTestleri.D2_01_index_dugme_cssi_olcek_cssten_SONRA_ortak_cssten_ONCE_yukler` | aynı sebep | **BU PAKET → düzeltildi**; yeniden koşum YEŞİL |
  | `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu`, `…Kapsam_orani_beyan_edilen_degerle_ayni` | yeni kilitsiz istemci tipi `GunKapanisiOnizleme` (`Kasalar.razor:786/800`, `KasaRaporu.razor:434/531` — 20:57'de başka şerit tarafından eklendi; bu paket tur 3'te bu iki dosyaya DOKUNMADI) | gün kapanışı / sayım şeridi |
  | `IadeTestleri.Kapanmis_gune_iade_yazilamaz` | "Sayım ile defter arasında 1.000,00 TL fark … gerekçe ZORUNLUDUR" — gün kapanışı sayım kapısı | gün kapanışı / sayım şeridi |
  | `ParaDemoCariEkranTestleri.PDE5_detayda_odeme_turu_borc_etkisi_ve_YURUYEN_bakiye` | "· Kart" beklenirken "· Nakit" | PARA-DEMO şeridi |
  **Bu paketin yeni kırmızısı: 0** (kendi iki kırmızısı düzeltildi ve yeniden koşuldu).
- `bash denetim.sh` (son kod, `t3-denetim3.log`): tek ✗ **P-30 DEMO_INSAN_SURESI** (orkestratör istisnası). Ara koşumlarda görülen ✗'ler: `Pages: @onclick'siz … <button> 72 (tavan 71)` — bu turun Hurda düğmesi iki satıra bölündüğü için satır sayacı yakaladı → tek satıra alındı, **71 ✓**; `DUGME2_BAGLANTI_SIRASI` — o an TOPLU-3'ün `toplu3.css` bağlantısı dugme↔ortak arasındaydı (başka şerit; sonra kendisi taşıdı) + bu paketin bağlantısı → bu paketinki taşındı, **✓**; `P-23 ODEME_KALEMSIZ_YOL 1` — `PAKETPARA-DEMO-TEST` kiracısının bir TAHSILAT satırı (canlı veri, başka şerit), son koşumda ✓. Sayaçlar: `ModalPencere 48 (tavan 55)`, `SOZLESME_KILITSIZ_TIP 58 (tavan 58)`, `SABIT_RENK 0`, `SATIR_ICI_STYLE_P11 0`, `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 47 (tavan 47)`.

### 0.T3.4 Mutasyon tablosu (tur 3)
Yöntem: `m3/mut.zsh` — kaynak `/private/tmp/kuyumcu-serit/mut-yedek/SUBE-KAPSAM-B/t3/<kod>.yedek` → `perl -0777 -pi` tek eşleşme → `-o m3/out` derleme → kaynak HEMEN geri + `cmp` → testler (filtre: `GorunumSubesiEkranTestleri|SubeKapsamBPozisyon|SUBEKAPSAMB|KabukArama`, 98 test). Günlük: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/m3/hepsi.log`, `m3/T3-*.log`.
| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| T3-1 | Kart `SubeliSorgu.Ekle("/pozisyon", Oturum)` → `"/pozisyon"` | `Pozisyon_karti_secili_subeyi_gosterir_subeId_tasir_secim_degisince_yeniden_ceker`, `Panel_pozisyon_karti_ilk_istekte_bile_secili_subeyle_gider` | cmp AYNI |
| T3-2 | Kart görünüm değişimi aboneliği silindi | `Pozisyon_karti_secili_subeyi_gosterir_…` | cmp AYNI |
| T3-3 | Sunucu vitrin `Stoklar…Daralt(gorunumSubesi)` silindi | `SKB_P1_secili_sube_vitrin_ve_hurda_YALNIZ_o_sube_SQL_ile_ayni_cari_kiraci_geneli` | cmp AYNI |
| T3-4 | `HurdaStokKapisi.KiraciKirilimi` hurda `Daralt` silindi | `SKB_P1_…` | cmp AYNI |
| T3-5 | Uç `GorunumSubesi.Coz` yerine `null` geçirir | `SKB_P1_…`, `SKB_P3_pasif_ve_yabanci_sube_404_tezgahtar_403` | cmp AYNI |
| T3-6 | `GorunumOku` içindeki geçersiz tercih temizliği silindi | `Kapanan_sube_tercihi_acilista_SUNUCUDA_temizlenir_bant_bir_kez_gorunur`, `Kapanan_sube_temizligi_duserse_sebep_gorunur` | cmp AYNI |
| T3-7 | `GecersizBandiKapat` bayrağı indirmiyor | `Kapanan_sube_bandi_Tamam_ile_kapanir_baska_sayfada_da_cikmaz`, `Kapanan_sube_temizligi_duserse_sebep_gorunur` | cmp AYNI |
| T3-8 | Rozet `SuzgecKullan`'ı yok sayar | `IslemDefteri_suzgec_baska_subeye_cevrilince_rozet_gosterilen_subeyi_yazar`, `Raporlar_reyon_stok_sube_kutusu_baska_subede_kart_rozeti_suzgec_subesini_yazar`, `Rozet_suzgec_kipi_bos_degerde_tum_subeler_kart_rozeti_bandi_tekrarlamaz` | cmp AYNI |
| T3-9 | İşlem defteri rozetinden `SuzgecKullan/SuzgecSubeId` kaldırıldı | `IslemDefteri_suzgec_…` | cmp AYNI |
| T3-10 | Seçici değeri `TUMU` yerine `""` (eski yer-tutucu) | `Secici_etiketi_HANGI_SUBE_tum_subeler_yer_tutucu_degil_gercek_secim` | cmp AYNI |
| T3-11 | Hurda `@if (FormAyriSubede)` → `@if (false)` (form başka şubede de çizilir) | `Hurda_baska_sube_gorunurken_form_ikinci_bakiye_CIZMEZ_kendi_subeme_gec_duzeltir` | cmp AYNI |
| T3-12 | Rozet sınıfı `gorunum-rozeti` → `rozet-satir` | `Rozet_kucuk_hap_DEGIL_govde_olcekli_satir_ve_css_bagli` | cmp AYNI |
| T3-13 | Raporlar `@if (RsSubeFarkli)` → `@if (false)` | `Raporlar_reyon_stok_sube_kutusu_…` | cmp AYNI |
| T3-14 | Kart `@if (veri.Kapsam == "SUBE")` notu kaldırıldı | `Pozisyon_karti_…`, `Panel_pozisyon_karti_…` | cmp AYNI |
| T3-15 | Kapanmış-şube bandı kart rozetlerinde de çizilir (`Id == VarsayilanId` koşulu silindi) | `Rozet_suzgec_kipi_bos_degerde_tum_subeler_kart_rozeti_bandi_tekrarlamaz` | cmp AYNI |
**Bulunan test açığı ve düzeltmesi:** T3-1…T3-4 koşumlarında `Rozet_kucuk_hap_DEGIL_…` mutasyondan BAĞIMSIZ kırmızıydı (CSS dosyasını `AppContext.BaseDirectory`'den arıyordu; mutasyon `-o` ile başka dizine derlediği için yol tutmuyordu) — test kaynak ağacını `[CallerFilePath]` ile bulacak şekilde düzeltildi; T3-5'ten itibaren yalnız gerçek kırmızılar görünür, T3-12 bu testin kendisini doğrular. T3-1…T3-4'ün kanıtı o testten bağımsız olarak listelenen ADLI testlerdir. Tur 2 mutasyonları (T2-1…T2-10b, M7-1r, M8-1r) kodları değişmediği için geçerlidir (`KabukServisi`'ne eklenen kod `d.SubeId = eski;` geri alma yoluna dokunmadı).

### 0.T3.5 Canlı doğrulama (5160 · vekil 5161 · başsız Chrome CDP 9705–9718)
Test kiracısı `PAKETSUBE-KAPSAM-B-TEST` (id `01M3W9C0JCBYQ8NNCCQY74B1HJ`) API ile kuruldu (`canli3/kur.py`; yerel sistem anahtarı bu sürece rastgele üretildi, değer basılmadı): Merkez Şube + **"Kadıköy Bahariye Caddesi Şubesi"** (doğrulayıcının uzun adı — sarma/zıplama ölçümü için), iki kasa, cari, TEZ A (Merkez), TEZ B (Kadıköy), MÜDÜR (Kadıköy); avans Merkez 5.000+2.000 / Kadıköy 3.000; hurda alışı Merkez 2 g K22 / Kadıköy 1 g K22; **vitrin stoku** 10 g K22 ziynet — Merkez 5 adet, Kadıköy 1 adet (`PATCH /urunler/{id}/stok/{subeId}`). Hepsi 200/201.

**Altın pozisyonu: API = bağımsız SQL = aynı sayfadaki Stok değeri** (`canli3/olc3.py`, çıktı `olc3.txt`; SQL: `sum(stoklar.toplam_has_gram)` konsinye hariç, `sum(hareketler.has_grami)` hurda türleri, şube başına):
| Görünüm | /pozisyon kod · kapsam | vitrin (API) | vitrin (SQL) | hurda (API) | hurda (SQL) | alacak (API) | net (API) | /raporlar/stok-degeri has |
|---|---|---|---|---|---|---|---|---|
| Merkez | 200 · SUBE / cari KIRACI_GENELI | 45.8 | 45.800000 | 1.832 | 1.832000 | 0 | 47.632 | 200 45.8 |
| Şube B | 200 · SUBE / cari KIRACI_GENELI | 9.16 | 9.160000 | 0.916 | 0.916000 | 0 | 10.076 | 200 9.16 |
| Tüm şubeler | 200 · TUM_SUBELER / cari KIRACI_GENELI | 54.96 | 54.960000 | 2.748 | 2.748000 | 0 | 57.708 | 200 54.96 |
TEZGAHTAR(A) GET /pozisyon?subeId=<B> → 403 Bu işlem için gerekli rol: dükkân sahibi ya da müdür ya da muhasebe
TEZGAHTAR(A) GET /pozisyon → 403 Bu işlem için gerekli rol: dükkân sahibi ya da müdür ya da muhasebe
MUDUR(B) GET /pozisyon?subeId=<M> → 200 
SAHIP GET /pozisyon?subeId=01ZZZZZZZZZZZZZZZZZZZZZZZZ → 404 Şube bulunamadı ya da pasif.
(Alacak 0: kiracıda has cinsinden cari bakiye yok — cari kapsamının şubeye bölünmediği HTTP testi `SKB_P1` ile kanıtlı: Şube B'de yazılan 2 g alacak Merkez görünümünde de 2 g.)

**Tarayıcı senaryosu** (`canli3/senaryo3.mjs`, günlük `senaryo/senaryo.jsonl`, görüntüler `senaryo/foto/`; 1280 ve 375 ayrı profillerle):
| Adım | Beklenen | Gerçek (1280 · 375) | Görüntü (Read ile GÖRÜLDÜ) |
|---|---|---|---|
| Menü seçici "Tüm şubeler" | etiket "Hangi şube?", değer gerçek seçim (yer-tutucu değil), tam genişlik | etiket "Hangi şube?" · metin "Tüm şubeler" · sınıf `secici-metin secici-metin--tam` (yer-tutucu YOK) · `data-deger=TUMU` · **genişlik 214 px** | `menu-secici-Tum-1280.png` |
| Menü seçici Kadıköy | genişlik zıplamaz | **genişlik 214 px** (aynı), yükseklik 44→46 px (uzun ad TOPLU-1 bileşen kuralıyla en çok iki satır) | `menu-secici-Kadikoy-1280.png` |
| Görünüm rozeti | ≥ 14 px | `getComputedStyle` **16 px**, yükseklik 22 px (375'te iki satır, 44 px), renk `var(--metin)` (koyu: rgb(218,222,230)); düz satır, hap değil | `panel-PRO-Kadikoy-1280.png` |
| Panel BASİT · Kadıköy | kart Kadıköy'ün stoku | **+10,1 g has · Vitrin 9,2 g** (= API 10,076 / 9,16 = SQL) + "Alacak: tüm şubeler birlikte"; Tüm şubelerde not yok | `panel-BASIT-Kadikoy-1280.png`, `…-375.png` |
| Rapor merkezi BASİT · Kadıköy | aynı kart, aynı rakam | **+10,1 g has · Vitrin 9,2 g**; Stok değeri ucu Kadıköy 9,16 has (çelişki yok) | `raporlar-BASIT-Kadikoy-1280.png` |
| Hurda · Kadıköy | tek rakam, ikinci bakiye yok | kart **0,916 g**; form yerine bant "Hurda satışı yalnız kendi şubenizden (Merkez Şube) yapılır…" + "Kendi şubeme geç (Merkez Şube)"; sayfada 1,832 YOK; form 0 | `hurda-BASIT-Kadikoy-1280.png`, `…-375.png` |
| "Kendi şubeme geç" | görünüm Merkez, form = kart | rozet "Gösterilen şube: Merkez Şube"; kart **1,832 g**; form açık, "Kalan hurda: 1,832 g" (aynı rakam) | `hurda-BASIT-Merkez-gecis-1280.png` |
| İşlem defteri · görünüm Kadıköy · süzgeç → Merkez Şube | rozet süzgeçteki şubeyi yazar | önce "Gösterilen şube: Kadıköy…", süzgeçten sonra **"Gösterilen şube: Merkez Şube"**, özet "Giriş 7.000 ₺" (= Merkez avansları 5.000+2.000) | `defter-suzgec-Merkez-1280.png`, `…-375.png` |
| Kapanan şube (Kadıköy `DELETE /subeler` → 200; tercih Kadıköy) | bant bir kez + kapatılabilir; tercih temizlenir | ÖNCE `GET /tercihler/benim`: kayıtlı=Kadıköy, gecersizTercih=**true**. İlk açılış: bant "Seçtiğiniz şube kapalı; kendi şubenize dönüldü." + **Tamam**; Tamam → bant 0, Kasalar'da da 0. SONRA `GET /tercihler/benim`: kayıtlı=**Merkez**, gecersizTercih=**false**. Yeni oturumda açılış: bant **0** | `pasif-ilk-375.png`, `pasif-ilk-1280.png`, `pasif-tamam-kasalar-375.png`, `pasif-sonra-1280.png` |

**K-13 ölçümü** (`betikler/tur/tur.mjs`, tercih Kadıköy — rozet, kart notu, hurda bandı görünür): 5 rota (`/`, `/hurda`, `/raporlar`, `/raporlar/islem-defteri`, `/kasalar`) × 1280+375 × BASİT+PRO × açık+koyu = **40 sayfa ölçümü + 20 sekme + 154 katman; geçersiz 0; K1 gizli-görünen / K3 taşan-kırık / K5 TRY / K9 binen / yatay kaydırma kusurlu sayfa 0**. Tam satırlar: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/canli3/ozet-tablo.md`; ham `canli3/tur-B/olcum-*.jsonl`, görüntüler `canli3/tur-B/foto/` (Read ile görülen: `hurda__PRO_aydinlik_1280.png`). `analiz.py` kalan bulguları: (a) **K8 jargon "SK-B"** `/` ve `/raporlar/islem-defteri` BASİT — test verisinin cari adı ("SK-B Canli Musteri"), arayüz metni değil; (b) **K12 ezik `#rp-yil/#rp-ay`** PRO 1280 — tur 2 § 11.3 b'de kayıtlı, bu paketle ilgisiz; (c) **K4 sayfa boyu 2,1 ekran** `/raporlar` PRO 375 (eşik 2; tur 2'de 2,0) — taşma/binme değil, sayfa uzunluğu: bu kiracıda stok tablosunda ürün satırı var ve rozet 375'te iki satır.
| Rota | Mod × tema × genişlik | K1 | K3 taşan/kırık | K5 TRY | K9 binen | Yatay | K2 % |
|---|---|---|---|---|---|---|---|
| / | BASİT/PRO × açık/koyu × 1280/375 (8) | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |
| /hurda | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |
| /raporlar | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 (PRO 1280 ölçüsüz — tur 2 ile aynı) |
| /raporlar/islem-defteri | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |
| /kasalar | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |

**1440 ek ölçümü** (görevin kabul satırı "1440/1280/375"): ikinci bir test kiracısı (`01M3WAP5EZN7NXKJ6HV2RE1J0J`, aynı `kur.py`) ile aynı 5 rota × BASİT+PRO × açık+koyu × **1440** = **20 sayfa; geçersiz 0; K1/K3/K5/K9/yatay kusurlu 0**; `analiz.py` kalanları yine yalnız (a) test verisi "SK-B" ve (b) önceden var olan `#rp-yil/#rp-ay` K12. Ham `canli3/tur-B1440/`, görülen `kok__BASIT_aydinlik_1440.png` (rozet "Gösterilen şube: Kadıköy Bahariye Caddesi Şubesi", kart +10,1 g has · Vitrin 9,2 g · "Alacak: tüm şubeler birlikte").

**Temizlik:** her iki koşumdan sonra API (`dotnet run` + dinleyici) ve vekil PID'leri `kill` → `lsof :5160` ve `:5161` **BOŞ**. `zsh kiraci-temizle.sh 01M3W9C0JCBYQ8NNCCQY74B1HJ --uygula` ve `zsh kiraci-temizle.sh 01M3WAP5EZN7NXKJ6HV2RE1J0J --uygula` → `tenants WHERE id IN (ikisi)` = **0**; kendi davet satırları (`kiraci_davetleri.kullanan_tenant_id`) → **0**. **tenants: canlı başlangıcında 15 → son 14** — fark bu paketin değil: arada başka şeritler kendi kiracılarını açıp sildi (listede `PAKETTOPLU3H-TEST` yeni; `PAKETPARA-DEMO-TEST`, `PAKETTOPLU-3-TEST` artık yok); bu paketin açtığı 2 kiracıdan kalan **0**. `QA-TEST-*` ve `PAKETSKB-TEST` (doğrulayıcının) kiracılarına DOKUNULMADI. Sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı (0 bayt).

### 0.T3.6 DoD sözleşmesi (tur 3 eklentileri)
| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Yeni varlık | YOK (migration yok) | Pozisyon yanıtına varsayılanlı iki alan (`kapsam`, `cariKapsam`) — sözleşme tipi, tablo değil. |
| Görünüm şubesi tercihi | Seçici (PATCH) + **otomatik temizleme** (kapanan şubede açılışta bir kez PATCH) + bandın **Tamam** düğmesi | Yazma ŞK-A'nın `TercihModulu` yolundan; denetim kaydı orada (`YazmaKaydet`), yeniden yazılmadı. |
| Altın pozisyonu | Salt okunur türetilmiş değer (B-2'nin `DegismezIpucu` gerekçesi aynen) | Şube parametresi yalnız gösterimi daraltır. |
| Düğmeler (DoD EK 1) | `#gorunum-gecersiz-kapat` (Tamam → `GecersizBandiKapat`), `#hrd-kendi-subeye-gec` (→ `GorunumSubesiAtaAsync`, sunucuya PATCH; hata `#hrd-kendi-subeye-gec-hata`) | İkisi de gerçek iş yapar; bUnit tıklama testleri var. Ekran başına tek birincil düğme: Hurda'da form yokken tek birincil "Kendi şubeme geç". |
| ModalPencere | Yeni kullanım 0 (48, tavan 55) | |

### 0.T3.7 Tur 3 değişen dosyalar
Kaynak: `Kuyumcu.Api/Modules/Pozisyon/PozisyonModulu.cs`, `Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs`, `Kuyumcu.Contracts/Sayfalar/PozisyonSozlesmeleri.cs`, `Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor`, `Kuyumcu.Web/Bilesenler/GorunumRozeti.razor`, `Kuyumcu.Web/Bilesenler/SubeSecici.razor`, `Kuyumcu.Web/Servisler/KabukServisi.cs`, `Kuyumcu.Web/Pages/Hurda.razor`, `Kuyumcu.Web/Pages/IslemDefteri.razor` (1 satır), `Kuyumcu.Web/Pages/Raporlar.razor` (reyon kartı rozeti + `RsSubeFarkli`), `Kuyumcu.Web/wwwroot/index.html` (1 bağlantı), `Kuyumcu.Web/wwwroot/css/sube-kapsam-b.css` (YENİ), `Kuyumcu.Core/I18n/Sozlukler.SubeKapsamB.cs` (+5 anahtar TR+EN: `HangiSube`, `BandiKapat`, `GecersizTemizlenemedi`, `HurdaSatisKendiSubede`, `HurdaKendiSubeyeGec`). Test: `Kuyumcu.Tests/GorunumSubesiEkranTestleri.cs` (+11 bUnit, 1 uyarlama), `Kuyumcu.Tests/SubeKapsamBPozisyonHttpTestleri.cs` (YENİ, 3 HTTP), `Kuyumcu.Tests/KabukAramaEkranTestleri.cs` (1 test yeni etikete uyarlandı). `Secici.razor`, `Sozlukler.cs` (hub), `GorunumSubesi.cs` (ŞK-A), ortak/TOPLU-1 CSS'leri DEĞİŞMEDİ. Satır içi style 0, sabit renk 0.
Dokunulan sayfalar (DoD EK 2): Hurda.razor, IslemDefteri.razor, Raporlar.razor (aynı paketin tur 3'ü — gerekçe sapma 5).

### 0.T3.8 Koşul karşılama (tur 3)
| Koşul | Durum | Kanıt |
|---|---|---|
| Altın pozisyonu kartı seçili şubenin stokunu gösterir; `GET /pozisyon` şube parametresi; SubeliSorgu kalıbı | KARŞILANDI | HTTP `SKB_P1`; bUnit `Pozisyon_karti_…`, `Panel_pozisyon_karti_…`; canlı tablo |
| Yetki: tezgâhtar başka şube → 403 | KARŞILANDI | `SKB_P3` + canlı TEZ A `?subeId=<B>` 403; pasif/yabancı şube 404 |
| "Tüm şubeler"de toplam; API = bağımsız SQL; Stok değeri ile tutarlı | KARŞILANDI | canlı: 54,96 = 45,8 + 9,16 = SQL = `/raporlar/stok-degeri` |
| İşlem defteri ve Reyon stok rozetleri gösterilen veriyi söyler | KARŞILANDI | bUnit 2 + canlı defter "Merkez Şube" |
| Kapanan şube bandı kapatılabilir / geçersiz tercih kendiliğinden temizlenir (bir kez) | KARŞILANDI (ikisi birden) | bUnit 3 + canlı önce/sonra `gecersizTercih` true → false, ikinci açılış bant 0 |
| Hurda Ramata formu: tek rakam | KARŞILANDI (sapma 3: görünüm ≠ kendi şube iken form ikinci bakiye çizmez; "Kendi şubeme geç" ile form = kart) | bUnit + canlı 0,916 / geçişte 1,832 = 1,832 |
| Rozet ≥ 14 px | KARŞILANDI | canlı 16 px (`getComputedStyle`); CSS `var(--yazi-md)` (sıkı ölçekte 14 px) |
| Seçici zıplamaz; "Tüm şubeler" gri yer-tutucu değil; etiket "Hangi şube?" | KARŞILANDI | canlı genişlik 214 = 214 px, sınıf yer-tutucu yok, etiket "Hangi şube?" |
| Mutasyon | KARŞILANDI | 15/15 kırmızı, hepsi cmp AYNI |
| Başsız Chrome 1440 + 1280 + 375 · BASİT + PRO · açık + koyu | KARŞILANDI | 60 sayfa ölçümü (40 + 20), kusurlu 0; görüntüler Read ile görüldü |
| build 0 hata · denetim TEMİZ (P-30 hariç) · tam süit 0 yeni kırmızı · test kiracısı silindi | KARŞILANDI | § 0.T3.3, § 0.T3.5 |

### 0.T3.9 Açık kalanlar (tur 3)
1. **Başkasına ait tam süit kırmızıları:** `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` + `Kapsam_orani_beyan_edilen_degerle_ayni` (`GunKapanisiOnizleme`, Kasalar/KasaRaporu 20:57 — gün kapanışı/sayım şeridi), `IadeTestleri.Kapanmis_gune_iade_yazilamaz` (sayım gerekçe kapısı — aynı şerit), `ParaDemoCariEkranTestleri.PDE5_…` (PARA-DEMO şeridi).
2. **Hurda satışını görünüm şubesinden yazma** (SAHİP'in başka şubenin hurdasını satması) bir SUNUCU yazma-yolu kararıdır (şartname § 1 bugün yasaklıyor) → orkestratör/Bertan kararına.
3. **PRO panelde altın pozisyonu kartı** yeni kiracının varsayılan kutucuk düzeninde çizilmiyor (canlıda `#pozisyon-net` yok); kart bileşeni aynı olduğundan şube davranışı BASİT/Rapor merkezi kanıtıyla aynıdır. Doğrulayıcının PRO görüntüsündeki kiracıda kart açıktı.
4. Doğrulayıcının `PAKETSKB-TEST` kiracısı (15:19) kuyumcu_net'te duruyor — bu paketin değil, silinmedi.
5. Tur 2'nin § 0.1 kapsam dışı notu (Raporlar ve İşlem defteri `GorunumDegisti` yarış koruması) aynen açık.

## 0. TUR 2 (01.10 17:20–18:10) — doğrulayıcı bulgularının kapanışı

> Tur 1 raporu (§ 1–16) aşağıda AYNEN duruyor; bu bölüm tur 2'de yapılanları ve **son kodla yeniden üretilen** kanıtları verir. Çelişki varsa bu bölüm geçerlidir.

### 0.1 Bulgu → yapılan
| Bulgu (doğrulayıcı) | Yapılan | Kanıt |
|---|---|---|
| **[KRİTİK] KasaRaporu: şube seçiliyken `/kasalar` düşerse hata siliniyor, rapor "Bu dönemde kayıt yok" diyordu** | `KasaRaporu.razor`: ayrı `kasaHata` alanı (Yukle()'deki `hata = null` onu ezmez), `HataBandi Mesaj="@(kasaHata ?? hata)"`; `kasalarOkundu` bayrağı — şube seçiliyken daraltma listesi okunamadıysa `Daralt` **null** döner (satır atıp "kayıt yok" demez, süzmeden başka şubenin kasasını da göstermez — fail-closed); son bilinen seçenek listesi catch'te SİLİNMEZ (denetim P-11 cırcırı 47'de kaldı). Defter isteği yine atılır (doğrulayıcının S2b senkron noktası). | `SUBEKAPSAMBSinirTestleri.S2b_…` **YEŞİL**; yeni `KasaRaporu_sube_seciliyken_kasalar_duserse_HATA_gorunur_bos_durum_ve_baska_sube_kasasi_YOK`, `KasaRaporu_sube_seciliyken_defter_YALNIZ_o_subenin_kasalarini_gosterir`; mutasyon T2-1 (hata korunmasın) → S2b + yeni test KIRMIZI; T2-2 (`!kasalarOkundu` dalı silindi) → yeni test KIRMIZI |
| **[ORTA] Emanet + Ürünler stok adetleri şube parametresi testsiz** | Yeni bUnit: `Emanet_cari_secilince_emanet_istegi_subeId_tasir_ve_rozet_cizer` (cari seçili fikstür — `Istek1EkranTestleri.Cariler` tek kaynak), `Urunler_stok_adetleri_subeId_tasir_urun_listesi_tasimaz_rozet_cizer` (liste kiracı geneli — `subeId` TAŞIMAZ; adet "3 adet" çizilir) | T2-3 (Emanet `SubeliSorgu.Ekle` kaldırıldı) → Emanet testi KIRMIZI; T2-4 (Ürünler stok `Ekle` kaldırıldı) → Ürünler testi KIRMIZI |
| **[ORTA] § 4.4 "Tüm şubeler: Hurda'da şube sütunu" karşılanmamıştı** | `Hurda.razor`: birleşik kapsamda (`kapsam = TUM_SUBELER`) sunucunun `HurdaBakiyeYaniti.Subeler` kırılımı çizilir — tablo `#hrd-sube-kirilimi` (Şube · Maden · Has gram; satır id `hrd-sube-{subeId}-{maden}`); **PRO'da açık**, **BASİT'te kapalı katlanır** `#hrd-sube-kirilimi-katlanir` (kurucu kuralı: isteğe bağlı olan kapalı); tek şube görünümünde hiç çizilmez. Sayılar sunucudan (UX-S21), ekranda türetilmez. Sözlük: `HurdaSubeKirilimi` ("Şubelere göre"/"By branch"), sütun başlığı mevcut `SubeSutunu`. | 3 yeni test (PRO açık / BASİT kapalı→açılınca görünür / tek şubede yok); T2-5 (`subeKirilimi` atanmadı) → PRO + BASİT testleri KIRMIZI; canlı: "Merkez Şube Altın 1,832 g · Şube B Altın 0,916 g" = SQL |
| **[DÜŞÜK] Kiracı geneli kartlar şube rozeti altında; `KiraciGeneli` rozeti kullanılmıyordu** | `Raporlar.razor` cari yaşlandırma sekmesine `<GorunumRozeti KiraciGeneli="true" Id="yas-kapsam-rozeti" />` ("Müşteri hesapları şubeye bölünmez — tüm şubeler birlikte"). `GorunumRozeti` yeni `Id` parametresi (varsayılan `gorunum-rozeti` — mevcut id DEĞİŞMEDİ; ikinci rozet kimlik çakıştırmaz; kapanmış-şube bandı ikinci rozette tekrarlanmaz). `Panel.razor`: şube seçiliyken (çoklu görünüm + `SubeId` dolu) "Kim borçlu?" (`#panel-soru-borclu-kapsam`), "Bekleyen iş" (Basit `#panel-soru-bekleyen-kapsam`, Pro kutucuk `#panel-bekleyen-kapsam`) ve banka satırı (`#panel-soru-kasa-banka-kapsam`) "Tüm şubeler birlikte" alt notu taşır; "Tüm şubeler"de not çizilmez. Sözlük: `KartTumSubelerNotu`. **Raporlar'da "vade sekmesi" YOK** (sekmeler: özet/grafik/cari/stok/yaşlandırma/gün sonu; "yaşlandırma" sekmesi STOK yaşıdır ve şubelidir) — kiracı geneli olan tek sekme cari olduğundan rozet oraya kondu. | 3 yeni test; T2-6 (koşul `SubeId: not null` kaldırıldı) → `Panel_tum_subelerde_kiraci_geneli_not_CIZILMEZ` KIRMIZI; T2-6b (borçlu notu kaldırıldı) → `Panel_sube_seciliyken_…_TUM_SUBELER_notu_tasir` KIRMIZI; T2-7 (cari rozeti kaldırıldı) → `Raporlar_cari_sekmesi_KIRACI_GENELI_rozeti_tasir_sayfa_rozeti_tek` KIRMIZI |
| **[DÜŞÜK] Seçim değişince yeniden yüklemede yarış koruması yok** | `KabukServisi`: `GorunumDurumu.Nesil` — her yazım nesli artırır; geç gelen ESKİ yazımın yanıtı/hatası yeni seçimi ezmez ve kullanıcıya bant olarak dönmez. `Panel.razor` (`YukleOzet`, `YukleKar`, `YukleGrafik`, `YukleDefter`) ve `KasaRaporu.razor` (`KasalariYukle`, `Yukle`): istek anındaki şube saklanır, yanıt geldiğinde seçim değişmişse (`Eskidi`) yanıt ATILIR (yeni seçimin yüklemesi kartı doldurur). | `Hizli_A_B_gecisinde_A_nin_GEC_hatasi_B_secimini_geri_ALMAZ`, `Panel_eski_subenin_GEC_yaniti_yeni_secimin_kartini_EZMEZ`, `KasaRaporu_eski_subenin_GEC_defter_yaniti_yeni_secimi_EZMEZ` (sahte API'de kapı/geciktirme); T2-8, T2-9, T2-10b → ilgili test KIRMIZI. Canlı: Panel'de Merkez→Şube B 150 ms arayla → rozet/seçici "Şube B", günlük kart Şube B rakamı (−2.564,80 ₺ = SQL) |
| [DÜŞÜK] Ortam: 5199 dinlemiyor | Paketle ilgisiz; DOKUNULMADI (5199/5200'e hiçbir komut gönderilmedi). Orkestratöre. | — |

**Kapsam dışı bırakılan (gerekçeli):** Raporlar ve İşlem defteri'nin `GorunumDegisti` akışları da aynı yarış desenine açıktır (sıralı `await`; canlıda kanıtlanmadı) — doğrulayıcı Panel + KasaRaporu'nu adlandırdı, tur sınırı (en çok 2 tur) nedeniyle bunlara aynı `Eskidi` kalıbı uygulanmadı → § 15'e eklendi. Emanet'in ikinci çağrısı `HareketleriTazele` (yazma sonrası tazeleme) ayrı bir testle korunmuyor; ilk çağrı (`CariDegisti`) korunuyor.

### 0.2 Tur 2 değişen dosyalar
`dotnet/Kuyumcu.Web/Pages/KasaRaporu.razor`, `Pages/Panel.razor`, `Pages/Hurda.razor`, `Pages/Raporlar.razor` (yalnız cari sekmesine 1 rozet satırı), `Bilesenler/GorunumRozeti.razor` (Id parametresi), `Servisler/KabukServisi.cs` (Nesil), `Kuyumcu.Core/I18n/Sozlukler.SubeKapsamB.cs` (+2 anahtar TR+EN), `Kuyumcu.Tests/GorunumSubesiEkranTestleri.cs` (+13 test, sahte API'ye hata kodu + kapı). Hepsi şerit dosya alanında; Emanet/Urunler kaynağına tur 2'de dokunulmadı (yalnız test). Sozlukler.cs HUB'a anahtar eklenmedi. Yeni CSS yok, satır içi style yok, yeni ModalPencere yok.

### 0.3 Build / test / denetim (son kod)
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra temiz): **0 Hata, 2 Uyarı** (başka şeritlerin test dosyaları: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65)` CS8601, `KesifP2Testleri.cs(454,9)` xUnit2029). Son artımlı build 0/0. Günlük: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/t2-b-son.log`, `t2-b-son2.log`.
- Filtreli (`GorunumSubesiEkranTestleri | SUBEKAPSAMBSinirTestleri`): **50/50 geçti** (tur 1: 31 + tur 2: 13 + doğrulayıcının 6 sınır testi).
- **Tam süit (son kod):** `KUYUMCU_TEST_DB=kuyumcu_test_t1 dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-B-tur2-son` → **8965 · 8963 geçti · 0 başarısız · 2 atlandı** (trx `_Ramazan-MacBook-Air_2026-10-01_17_56_08.trx`). Bu paketin yeni kırmızısı: 0.
- `bash denetim.sh`: tek ✗ **P-30 DEMO_INSAN_SURESI** (orkestratör istisnası). Ara koşumda `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47` ✗ çıktı — sebep bu turun `KasaRaporu` catch'indeki `kasalar = [];` satırıydı; kaldırıldı (son bilinen liste korunur) → **47 (tavan 47) ✓**. Diğer: `ModalPencere 48 (tavan 55)`, `SOZLESME_KILITSIZ_TIP 58 (tavan 58)`, `SABIT_RENK 0`, `SATIR_ICI_STYLE_P11 0`, `TARIH_TEK 0`. Günlük: `t2-denetim2.log`.

### 0.4 Mutasyon tablosu (tur 2)
Yöntem tur 1 ile aynı (`m2/mut.zsh`: yedek → `perl -0777 -pi` tek eşleşme → özel çıktı dizinine derleme → kaynak HEMEN geri + `cmp` → testler). Filtre: `GorunumSubesiEkranTestleri|SUBEKAPSAMBSinirTestleri`.
| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| T2-1 | KasaRaporu bandı `@(kasaHata ?? hata)` → `@hata` | `SUBEKAPSAMBSinirTestleri.S2b_Sube_secili_iken_kasalar_dusarse_hata_bandi_gorunur_bos_durum_degil`, `KasaRaporu_sube_seciliyken_kasalar_duserse_HATA_gorunur_bos_durum_ve_baska_sube_kasasi_YOK` | cmp AYNI |
| T2-2 | `Daralt` içindeki `if (!kasalarOkundu) return null;` silindi | `KasaRaporu_sube_seciliyken_kasalar_duserse_…` | cmp AYNI |
| T2-3 | Emanet `CariDegisti` `SubeliSorgu.Ekle` kaldırıldı | `Emanet_cari_secilince_emanet_istegi_subeId_tasir_ve_rozet_cizer` | cmp AYNI |
| T2-4 | Ürünler stok adetleri `SubeliSorgu.Ekle` kaldırıldı | `Urunler_stok_adetleri_subeId_tasir_urun_listesi_tasimaz_rozet_cizer` | cmp AYNI |
| T2-5 | Hurda `subeKirilimi = b.Subeler ?? [];` silindi | `Hurda_tum_subelerde_PRO_sube_kirilimi_ACIK_…`, `Hurda_tum_subelerde_BASIT_sube_kirilimi_KAPALI_…` | cmp AYNI |
| T2-6 | Panel `SubeDaraltmali` koşulundan `SubeId: not null` kaldırıldı | `Panel_tum_subelerde_kiraci_geneli_not_CIZILMEZ` | cmp AYNI |
| T2-6b | Panel borçlu kartı notu kaldırıldı | `Panel_sube_seciliyken_kiraci_geneli_kartlar_TUM_SUBELER_notu_tasir` | cmp AYNI |
| T2-7 | Raporlar cari sekmesi kiracı geneli rozeti kaldırıldı | `Raporlar_cari_sekmesi_KIRACI_GENELI_rozeti_tasir_sayfa_rozeti_tek` | cmp AYNI |
| T2-8 | KabukServisi catch'teki nesil koruması silindi | `Hizli_A_B_gecisinde_A_nin_GEC_hatasi_B_secimini_geri_ALMAZ` | cmp AYNI |
| T2-9 | Panel `YukleOzet` eski yanıt koruması silindi | `Panel_eski_subenin_GEC_yaniti_yeni_secimin_kartini_EZMEZ` | cmp AYNI |
| T2-10 → **TEST AÇIĞI bulundu** | KasaRaporu `Yukle` eski yanıt koruması silindi | İlk koşum **0 kırmızı** → yeni test `KasaRaporu_eski_subenin_GEC_defter_yaniti_yeni_secimi_EZMEZ` yazıldı | cmp AYNI |
| T2-10b | aynı mutasyon, yeni testle | `KasaRaporu_eski_subenin_GEC_defter_yaniti_yeni_secimi_EZMEZ` | cmp AYNI |
| M8-1r | (son kodla tekrar) `d.SubeId = eski;` silindi | `K9_Tercih_yazimi_500_donerse_secici_ESKI_degere_doner_ve_HataBandi_gorunur` | cmp AYNI |
| M7-1r | (son kodla tekrar) Panel `/raporlar/gunluk` `Ekle` kaldırıldı | `M7_Panel_…`, `Panel_secim_degisince_…`, `Sayfa_yenilemede_…`, `Panel_eski_subenin_GEC_yaniti_…` | cmp AYNI |
Günlükler: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/m2/T2-*.log`, `M7-1r.log`, `M8-1r.log`. (T2-3'ün ilk koşumu paralel şeridin `Kuyumcu.Web/obj` dosya kilidiyle MSB4018 derleme hatası verdi — kaynak cmp AYNI geri alındı, tekrar koşuldu; sonuç yukarıda.)

### 0.5 Canlı doğrulama (5160 · vekil 5161 · başsız Chrome CDP 9711-9719)
Test kiracısı `PAKETSUBE-KAPSAM-B-TEST` (id `01M3VZ9M1KZ766FKES6TFN3Q17`) API ile yeniden kuruldu (yerel sistem anahtarı rastgele, değeri basılmadı): Merkez Şube + Şube B, iki kasa, bir cari, TEZ A (Merkez), TEZ B (Şube B), MÜDÜR (Şube B); avans Merkez 5.000+2.000, Şube B 3.000; hurda alışı Merkez 2 g K22, Şube B 1 g K22 (hepsi 200/201).

**API = SQL** (`canli2/olc.py`):
| Görünüm | `/raporlar/gunluk` hareket · hurda TL · kasa | SQL hareket · hurda TL · kasa | `/kasalar` | `/hurda/bakiye` has |
|---|---|---|---|---|
| Merkez | 200 · 3 · 5.129,60 · 1.870,40 (SUBE) | 3 · −5.129,60 · 1.870,40 | [Merkez Kasa SKB] | 1,832 (SUBE) |
| Şube B | 200 · 2 · 2.564,80 · 435,20 (SUBE) | 2 · −2.564,80 · 435,20 | [B Kasası SKB] | 0,916 (SUBE) |
| Tüm şubeler | 200 · 5 · 7.694,40 · 2.305,60 (TUM_SUBELER) | 5 · −7.694,40 · 2.305,60 | [ikisi] | 2,748 (TUM_SUBELER) |
`/hurda/bakiye` birleşik `subeler` = [Merkez Şube ALTIN 1,832 · Şube B ALTIN 0,916]; SQL `sum(has_grami)` şube başına 1,832000 / 0,916000. TEZGÂHTAR A `?subeId=B` → `/kasalar`, `/hurda/bakiye`, `/emanet/{cari}`, `/pos/tahsilatlar` **403** "Bu şubeyi görme yetkiniz yok."; şubesiz 200.

**Tarayıcı senaryosu** (`canli2/senaryo2.mjs`, günlük `canli2/senaryo/senaryo.jsonl`, görüntüler `canli2/senaryo/foto/`):
| Adım | Beklenen | Gerçek | Görüntü |
|---|---|---|---|
| PRO · Tüm şubeler · /hurda | kart 2,748 g + şube kırılımı açık | rozet "Gösterilen: tüm şubeler (birlikte)"; "Şubelere göre": Merkez Şube Altın 1,832 g · Şube B Altın 0,916 g | `hurda-PRO-tum-1280.png` (Read ile GÖRÜLDÜ) |
| BASİT · Tüm şubeler · /hurda | kırılım kapalı katlanır | "Şubelere göre 2" kapalı, tablo 0; açınca 2 satır | `hurda-BASIT-tum-acik-1280.png` |
| BASİT · Şube B · /hurda | kırılım yok, kart 0,916 g | tablo/katlanır 0; kart 0,916 g | — |
| BASİT · Şube B · Panel | borçlu + bekleyen kartı "Tüm şubeler birlikte" | ikisi de var; kasa 435,20 ₺ (= SQL Şube B) | `panel-BASIT-SubeB-1280.png` (Read ile GÖRÜLDÜ) |
| BASİT · Tüm şubeler · Panel | not yok | borçlu/bekleyen notu 0 | — |
| PRO · Şube B · /raporlar cari sekmesi | sayfa rozeti 1 + kiracı geneli rozeti | "Gösterilen şube: Şube B" (×1) + "Müşteri hesapları şubeye bölünmez — tüm şubeler birlikte" (×1) | `raporlar-PRO-SubeB-cari-1280.png` |
| PRO · Şube B · /raporlar/kasa | yalnız B kasası | "B Kasası SKB · 435,20 ₺"; hata bandı yok | `kasaraporu-PRO-SubeB-1280.png` |
| PRO · Panel · Merkez → Şube B (150 ms arayla) | son seçim kazanır | rozet + seçici "Şube B"; gün özeti net has 0,9160 · nakit akışı −2.564,80 ₺ (= SQL Şube B); iki günlük isteği (Merkez, B) | `panel-PRO-hizli-SubeB-1280.png` |
KasaRaporu'nun "`/kasalar` düşerse" dalı canlıda üretilemedi (sunucuyu düşürmek gerekir) — bUnit + doğrulayıcı sınır testi + mutasyonla kanıtlı.

**K-13 ölçümü** (`betikler/tur/tur.mjs`, değişen ekranlar; Hurda TÜM ŞUBELER tercihiyle — kırılım görünür, diğerleri ŞUBE B tercihiyle — notlar ve rozetler görünür): 4 rota × 1280+375 × BASİT+PRO × açık+koyu = **32 sayfa ölçümü + 144 katman; kusurlu 0 · geçersiz 0**. Satırlar: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/canli2/ozet-tablo.md`; ham `canli2/tur-{TUMU,B}/olcum-*.jsonl`, görüntüler `canli2/tur-{TUMU,B}/foto/`.
| Rota (tercih) | Mod × tema × genişlik | K1 gizli-görünen | K3 taşan/kırık | K5 TRY | K9 binen | Yatay | K2 % |
|---|---|---|---|---|---|---|---|
| /hurda (Tüm) | BASİT/PRO × açık/koyu × 1280/375 (8) | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |
| / (Şube B) | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 |
| /raporlar (Şube B) | 8 | 0 | 0/0 | 0 | 0 | 0 | 98 / 97 (PRO 1280 ölçüsüz) |
| /raporlar/kasa (Şube B) | 8 | 0 | 0/0 | 0 | 0 | 0 | 375'te 97; 1280 tek blok |
Not: `/raporlar` PRO 1280'de K12 ezik 2 = önceden var olan `#rp-yil/#rp-ay` bulgusu (§ 11.3 b; bu paketle ilgisiz). Read ile görülen: `hurda__PRO_karanlik_375.png` (kırılım tablosu 375'te 3 sütun sığıyor, taşma yok), `kok__BASIT_aydinlik_375.png` (rozet + iki kartta "Tüm şubeler birlikte").

**Temizlik:** API (dotnet run + dinleyici) ve vekil PID'leri `kill` → `lsof :5160` ve `:5161` BOŞ. `zsh kiraci-temizle.sh 01M3VZ9M1KZ766FKES6TFN3Q17 --uygula` → `tenants WHERE id=…` = 0; kendi davet satırı (`kiraci_davetleri.kullanan_tenant_id`) silindi (1 satır → kalan 0). **tenants: başlangıç 13 → son 13.** `QA-TEST-*` kiracılarına dokunulmadı. Sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı (0 bayt).

### 0.6 Koşul karşılama (tur 2 özeti)
Tur 1 koşulları (§ 14) son kodla yeniden kanıtlandı: K-7/K-8 kodu değişmedi (tur 1 tarayıcı kanıtı geçerli; Panel yükleyicilerine yalnız eski-yanıt koruması eklendi, `Sayfa_yenilemede_…` testi yeşil); K-9 (M8-1r kırmızı); K-13 (§ 0.5, 32 ölçüm 0 kusur); K-14 (testler yeşil); build 0 hata; denetim TEMİZ (P-30 hariç); tam süit 0 başarısız; test kiracısı silindi, tenants 13 → 13.

## 1. Özet

- Kurucunun sorusu karşılandı: üstte (kenar çubuğunda) şube seçilince **Panel, Kasalar, Hurda, POS alacakları, Emanet, Ürün stok adetleri, Raporlar (aylık, stok değeri, stok yaşı, gün sonu), Kâr-zarar, İşlem defteri, Kasa raporu** o şubeye göre gelir; **"Tüm şubeler"** seçilince birleşik gelir. Canlıda (5160) iki şubeli test kiracısında ölçüldü: ekran rakamı = API = SQL (fark 0).
- Seçim **sunucuya tercih** olarak yazılır (`PATCH /tercihler/benim {"gorunum-subesi": ULID|"TUMU"}`): sayfa yenilenince (K-7) ve **ikinci tarayıcı profilinde** (K-8) aynı şube seçili gelir — başsız Chrome ile kanıtlandı.
- Yazma düşerse seçici eski değere döner + hata bandı (K-9, bUnit + M8 mutasyonu).
- "Tüm şubeler" yetkisi istemcide rol listesinden DEĞİL, sunucunun tek kaynağından (`gorunumSubesi.tumSubeler`) okunur (§ 4.3.5).
- Tek şubeli dükkânda ve şubeye kilitli tezgâhtarda rozet ve seçici **çizilmez** (K-14, bUnit + tarayıcı).
- **Canlı ölçümde bulunan ve kapatılan kusur:** sayfa yenilemede (F5) Panel ilk günlük isteğini profil gelmeden ŞUBESİZ atıyor, tercih gelince ikinci kez şubeli atıyordu ("önce birleşik, sonra şube" titremesi). `KabukServisi.GorunumBekle` profil gelene kadar bekleyecek şekilde düzeltildi; yeni test + MB-9 mutasyonu; canlıda tekrar ölçüldü → F5'te tek istek, şubeli.
- Build 0 hata (2 uyarı başka şeritlerin test dosyalarında); denetim.sh **TEMİZ (P-30 hariç — orkestratörün istisnası)**; tam süit **8936 · 8934 geçti · 0 başarısız · 2 atlandı**; 13 mutasyon koşumu, her biri en az bir ADI yazılı testi kırmızıya çevirdi, hepsi `cmp AYNI` geri alındı.

## 2. Devralma notu (bu oturumdan önceki yarım deneme)

Aynı paketin bir önceki koşumu (01.10 15:08–15:41) kodu ve 17 bUnit testini yazmış, 6 mutasyon koşmuş, tam süit ortasında kesilmişti (rapor yok). Bu oturumda: (1) önceki mutasyonların hiçbirinin kaynakta kalmadığı `cmp` ile doğrulandı (M7-1, M8-1, MB-3…MB-6 yedekleri = mevcut dosyalar); (2) kod satır satır gözden geçirildi; (3) eksikler tamamlandı (§ 3); (4) bütün kanıtlar (build, test, mutasyon, denetim, tam süit, canlı, tarayıcı) bu oturumda **yeniden** üretildi — önceki koşumun sonuçlarına dayanılmadı.

## 3. Yapılanlar

### 3.1 Kabuk ve tek yardımcı (§ 4.3.1–4.3.3, 4.3.5–4.3.6)
| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/Servisler/KabukServisi.cs` | `AramaSubesi` → **`GorunumSubesi`** (yeniden adlandırma; arama da aynı değeri okur). Oturum başına `GorunumDurumu` (`ConditionalWeakTable<OturumServisi,…>` — paralel bUnit sınıfları birbirinin seçimini göremez). `GorunumHazirla` (GET `/tercihler/benim` → `gorunumSubesi`), `GorunumSubesiAtaAsync` (PATCH; hata → eski değere dönüş + sebep), `GorunumBekle` (**bu oturumda düzeltildi:** jeton var/profil yokken profili bekler, en çok 5 sn), `SubeListesiAta`. Kilitli rolde istemci daraltma TUTMAZ (sunucu oturum şubesini uygular). |
| `Kuyumcu.Web/Servisler/SubeliSorgu.cs` (YENİ) | `Ekle(yol, oturum)` — seçim varsa `subeId=` ekler, yoksa yolu AYNEN döner; `Sube(oturum)`; `HazirAsync(oturum)`. |
| `Kuyumcu.Web/Bilesenler/SubeSecici.razor` | Seçim → `GorunumSubesiAtaAsync`; K-9 hata bandı `#ust-sube-kayit-hata`; tercih okunamazsa seçici GİZLİ + `#ust-sube-tercih-hata` (UX-S28); `TumSubeler` istemci rol kopyası KALDIRILDI → sunucudan. Mevcut id'ler değişmedi. |
| `Bilesenler/GlobalArama.razor`, `KayitSecici.razor` | `AramaSubesi` → `GorunumSubesiIcin(Oturum)` (yalnız okuma değişimi). |
| `Bilesenler/Secici.razor` | YALNIZ ad değişimi (`AramaSubesi` → `GorunumSubesi`, 1 satır). Görünüm kuralı (TOPLU-1) bozulmadı. |
| `Bilesenler/GorunumRozeti.razor` (YENİ) | Başlık altı tek satır: "Gösterilen şube: X" / "Gösterilen: tüm şubeler (birlikte)"; `KiraciGeneli` parametresiyle "Müşteri hesapları şubeye bölünmez…". Kapanmış şube tercihinde sarı bant `#gorunum-gecersiz`. Tek şubede / kilitli rolde HİÇBİR ŞEY çizmez. `Degisti` geri çağrısı: seçim değişince sayfa listelerini yeniden yükler (tek abonelik kalıbı). Ad bilinmiyorsa ULID basılmaz (`DegerYok`). |
| `Kuyumcu.Core/I18n/Sozlukler.SubeKapsamB.cs` (YENİ) | 9 anahtar TR+EN, konumsal record (eksik anahtar = derleme hatası). Hub `Sozlukler.cs`'e anahtar EKLENMEDİ. |
| `Servisler/PosServisi.cs` | `Tahsilatlar(..., subeId)`, `ValorTakvimi(..., subeId)` isteğe bağlı parametre. |
| `Servisler/RaporServisi.cs` | **Bu oturumda:** `PosFaturaMutabakati(tarih?, subeId?)` (gün sonu; § 4.3.3). |
| `Servisler/ListeOnbellegi.cs` | **Değişiklik gerekmedi:** kayıt anahtarı yolun TAMAMIDIR (sorgu dahil), `/kasalar` ile `/kasalar?subeId=…` ayrı kayıttır; test `Kasalar_listesi_secili_subeyle_istenir_onbellek_anahtari_subeye_gore_ayri` bunu sınar (şartname § 8 riski kod okumasıyla + testle kapandı). |

### 3.2 Ekranlar (yalnız şube parametresi + rozet + yeniden yükleme)
| Sayfa | Şubeli çağrılar | Not |
|---|---|---|
| `Pages/Panel.razor` | `/raporlar/gunluk`, `/raporlar/aylik`, `/raporlar/kar-zarar`, işlem defteri | Seçim değişince şubeli kartlar yeniden yüklenir; kiracı geneli kartlar (borçlu, vade, banka, uyum) yeniden çekilmez. |
| `Pages/Kasalar.razor` | `/kasalar` | "Tüm şubeler"de kasa kartında şube adı (ProAlan — Basit'te katlı); yeni kasa formunda şube varsayılanı = görünüm şubesi. |
| `Pages/Hurda.razor` | `/hurda/bakiye`, `/hurda/defter` | Birleşik görünümde boş durum "Hiçbir şubede…" (SK-A açık kalan 1 kapandı). Satış formu YAZMA (oturum) şubesinin bakiyesini ayrı okur ve bunu yazar (UX-S22 — aynı soruya iki cevap olmasın). |
| `Pages/PosTahsilatlar.razor` | `/pos/tahsilatlar`, `/pos/valor-takvimi` | |
| `Pages/Emanet.razor` | `/emanet/{cariId}` | |
| `Pages/Urunler.razor` | `/urunler/stok-adetleri` | Ürün listesi kiracı geneli; yalnız adetler şubeli. |
| `Pages/Raporlar.razor` | `/raporlar/aylik`, `/raporlar/gunluk` (Basit "Bugün"), `/raporlar/odeme-araci`, `/raporlar/stok-degeri`, reyon stoku süzgeci, **`/raporlar/stok-yaslandirma`** ve **`/raporlar/pos-fatura-mutabakati`** (ikisi bu oturumda) | Seçim değişince Basit'te bugün özeti + gün sonu, Pro'da aylık + stok yaşı + diğerleri + mutabakat yeniden yüklenir. |
| `Pages/KarZarar.razor` | `/raporlar/kar-zarar`, `/kar-zarar/urun`, `/stok-degerlemesi` | Sorgu anahtarına şube eklendi (şube değişince aynı tarih aralığı yeniden çekilir). |
| `Pages/IslemDefteri.razor` | `/raporlar/islem-defteri` (sayfanın şube süzgecinin başlangıç değeri) | |
| `Pages/KasaRaporu.razor` | kasa SEÇENEKLERİ `/kasalar?subeId=`; defter yanıtı seçenek listesine daraltılır | Defter ucu (R11) şube parametresi taşımaz — § 4.2.2'nin dediği gibi yalnız seçenekler daraltıldı. |

### 3.3 Testler
`Kuyumcu.Tests/GorunumSubesiEkranTestleri.cs` (YENİ) — **31 koşum** (19 `[Fact]` + 1 `[Theory]` × 12 satır):
SubeliSorgu saf (2) · kilitli rol (1) · K7 açılışta okunur (1) · seçim sunucuya yazılır ULID/TUMU (1) · **K9** 500 → eski değer + HataBandi (1) · tercih okunamazsa seçici gizli + bant (1) · tüm-şube yetkisi sunucudan (1) · **M7** Panel istekleri `subeId` + rozet (1) · Panel seçim değişince yeniden yükler (1) · Panel Basit kâr (1) · **sayfa yenilemede ilk istek bile şubeli — titreme yok** (1, bu oturumda) · Raporlar Basit bugün + gün sonu (1, bu oturumda) · Kasalar + önbellek anahtarı (1) · Kasalar tüm şubelerde şube adı (1) · **K14** tek şube (1) · **K14** kilitli tezgâhtar (1) · kapanmış şube sarı bant (1) · sözlük TR/EN (1) · **tablo güdümlü 12 satır** (Hurda bakiye/defter, POS tahsilat/valör, Kâr-zarar özet/stok, Kasa raporu, İşlem defteri, Raporlar aylık/stok değeri/stok yaşı/gün sonu — her biri `subeId=s2` taşır + rozet "Şube 2") (bu oturumda).
Sahte API gerçek ucu taklit eder (PATCH değeri saklar, GET çözülmüş hâlini döner); fikstürler sahibi olan test sınıflarının sabitlerinden okunur (kopya yok), POS gövdeleri `PosEkranTestleri` ile aynı şekil.

Mevcut testlere uyarlama (yeniden adlandırma + tek kaynak gereği, davranış beklentisi değişmedi): `KabukAramaEkranTestleri.cs` (sahte sunucuya `/tercihler/benim` yanıtı; seçim oturuma bağlı okunur), `TK2KabukTuvalTestleri.cs` (tercih yanıtına `gorunumSubesi`), `P37RevizyonTestleri.cs` (`AramaSubesiAta(null)` → `GorunumSubesiYerelAta(null)`).

## 4. Sapmalar ve gerekçeleri
1. **`Raporlar.razor` "yalnız stok" sınırının ötesi:** şerit notu Raporlar'da yalnız stok çağrısını sayar; ancak § 4.3.3 "Gün sonu (pos-fatura-mutabakati)" ekranını ve sayfa başlığındaki rozet aynı sayfadaki aylık/gün sonu kartlarını da kapsar. Rozet "Merkez" derken aylık/gün sonu tüm şubeleri gösterseydi UX-S22 ihlali olurdu. Dokunulan satırlar yalnız çağrı/yeniden yükleme satırlarıdır; RAPOR-YASLANDIRMA şeridinin yaşlandırma satırlarına dokunulmadı (aynı dosyada eşzamanlı değişiklikleri korundu, `grep` ile doğrulandı).
2. **Gün sonu çağrısı `UyumServisi` yerine `RaporServisi.PosFaturaMutabakati`:** `UyumServisi.cs` şerit dosya alanında DEĞİL. İlk denemede çağrı sayfada `Api.GetAsync<Mutabakat>` ile kuruldu → `SozlesmeKapsamTestleri` (kilitsiz tip cırcırı) KIRMIZI verdi (yeni kilitsiz istemci tipi `Mutabakat`). Alan içindeki `RaporServisi`'ne taşındı; cırcır yeşil, denetim `SOZLESME_KILITSIZ_TIP 58 (tavan 58)`. Tip `UyumServisi.cs`'teki tek tanımdır.
3. **`GorunumBekle` düzeltmesi (§ 1):** şartnamede yok; canlı ölçüm buldu. Bekleme yalnız "jeton var, profil yok" durumunda (bUnit'te jeton yok → bekleme yok, mevcut 8900+ test etkilenmedi — tam süit kanıtı).
4. **`ListeOnbellegi.cs`'e dokunulmadı:** § 8 riski ("anahtar url+subeId olmalı") koddaki mevcut davranışla zaten karşılanıyor (§ 3.1).

## 5. Migration
**Gerekmedi.** İstemci paketi; şema değişikliği yok (tercih deposu SK-A'da hazır).

## 6. Mevzuat
Bu paket yasal kural/eşik/oran YAZMADI; tevsik/MASAK kümülatifinin şubeye bölünmemesi SK-A'da sunucuda (birincil kaynaklar SK-A raporu § 5). İstemci yalnız GÖSTERİMİ daraltır; yazma ve yasal kapılar oturum/kiracı genelinde kalır.

## 7. DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Görünüm şubesi tercihi (`kullanici_tercihleri` · `gorunum-subesi`) | Ekranda: kenar çubuğundaki şube seçici (seç = PATCH; "Tüm şubeler" = `TUMU`). Sunucu: `PATCH /tercihler/benim` (koru/null=sil) · `DELETE /tercihler/benim/gorunum-subesi` (SK-A) | Yeni varlık DEĞİL. Denetim kaydı SK-A'nın mevcut `YazmaKaydet` çağrısıyla. Yazma düşerse eski değere dönüş + bant (K-9). |
| Yeni varlık | YOK | — |
| Düğmeler | Yeni düğme eklenmedi (seçici mevcut `Secici` bileşeni). Rozet etkileşimsiz bilgi satırıdır. | DoD EK (1) karşılanır. |
| ModalPencere | Yeni kullanım 0 (denetim `Pages: ModalPencere kullanımı 48 (tavan 55)`, artış yok). | |

## 8. Testler (sayılar)
| Kapsam | Toplam | Geçti | Başarısız | Atlandı |
|---|---|---|---|---|
| `GorunumSubesiEkranTestleri` (yeni, son kod) | 31 | 31 | 0 | 0 |
| Etkilenen aileler (GorunumSubesiEkran · Rapor* · KarZarar · PosEkran · Hurda* · Kasa* · Emanet* · IslemDefteri · Panel* · KabukArama · TK2Kabuk · P37Revizyon · UX1C3 · BelgeKalan · KesifP3 · P69Has · P57 · G5Satis · Urunler) | 1342 | 1341 | 0 | 1 |
| Raporlar düzeltmesi sonrası (GorunumSubesiEkran · SozlesmeKapsam · Rapor* · UX1C3 · BelgeKalan · KesifP3 · P69Has · P57 · G5Satis) | 337 | 337 | 0 | 0 |
| **Tam süit (son kod, tek koşum)** | **8936** | **8934** | **0** | **2** |

Tam süit komutu: `KUYUMCU_TEST_DB=kuyumcu_test_t1 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --no-build --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-B-final` (temiz `dotnet build Kuyumcu.sln` sonrası, 4,3 dk). trx: `/private/tmp/kuyumcu-serit/trx-pSUBE-KAPSAM-B-final/_Ramazan-MacBook-Air_2026-10-01_16_56_07.trx`. **Bu paketin yeni kırmızısı: 0.**
Ara koşum notu (dürüstlük): Raporlar gün sonu düzeltmesinden ÖNCEKİ ara tam süit (16:37) `SozlesmeKapsamTestleri` × 2 kırmızı verdi — sebep bu paketti (§ 4.2), düzeltildi, son koşumda yeşil. Ondan önceki ara koşumdaki `RAPORYASLANDIRMASinirTestleri` × 2 kırmızısı RAPOR-YASLANDIRMA şeridine aitti; son koşumda yok.

## 9. Mutasyon tablosu
Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/SUBE-KAPSAM-B/t2/<kod>.yedek` → `perl -0777 -pi -e` tek eşleşme → **özel çıktı dizinine** derleme (`-o …/m2/out`, paralel şeritlerin `bin`'ini ezmemek için) → kaynak HEMEN geri kopyalanır + `cmp` → `dotnet test …/out/Kuyumcu.Tests.dll --filter GorunumSubesiEkranTestleri`. Betik: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/m2/mut.zsh`, günlükler `m2/<kod>.log`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → 0 hata.

| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M7-1 (şartname M7) | Panel `/raporlar/gunluk` çağrısında `SubeliSorgu.Ekle` kaldırıldı | `M7_Panel_sube_secili_iken_gunluk_aylik_defter_istekleri_subeId_tasir_rozet_yazar`, `Panel_secim_degisince_subeli_kartlari_YENIDEN_yukler` | cmp AYNI |
| M8-1 (şartname M8) | `GorunumSubesiAtaAsync` hata dalında `d.SubeId = eski;` silindi | `K9_Tercih_yazimi_500_donerse_secici_ESKI_degere_doner_ve_HataBandi_gorunur` | cmp AYNI |
| M8-2 | SubeSecici yazma hatasını banda yazmıyor (`kayitHata =` atılmıyor) | `K9_Tercih_yazimi_500_…` | cmp AYNI |
| MB-1 | SubeSecici `TumSubeler` yeniden istemci rol listesinden | `Tum_sube_yetkisi_istemci_rol_listesinden_DEGIL_sunucudan_okunur`, `Tercih_okunamazsa_secici_GIZLI_sebep_bantta_istekler_daraltmasiz` | cmp AYNI |
| MB-2 | Rozet `CokluGorunum` koşulu kaldırıldı (`@if (true)`) | `K14_Tek_subeli_kiracida_Panel_ve_Kasalarda_rozet_ve_secici_YOK` | cmp AYNI |
| MB-3 | Kasalar `/kasalar` çağrısında `Ekle` yok | `Kasalar_listesi_secili_subeyle_istenir_onbellek_anahtari_subeye_gore_ayri` | cmp AYNI |
| MB-4 | Raporlar stok yaşı `subeId` geçirmiyor | `Subeli_ekran_cagrisi_secili_subeyi_tasir_ve_rozet_cizer(Raporlar, /raporlar/stok-yaslandirma)` | cmp AYNI |
| MB-5 | Hurda `/hurda/bakiye` çağrısında `Ekle` yok | `Subeli_ekran_cagrisi_…(Hurda, /hurda/bakiye)` | cmp AYNI |
| MB-6 | `SubeliSorgu.Ekle` hep yolu döner (`\|\| true`) | 13 kırmızı: `SubeliSorgu_secim_yokken_…`, `M7_…`, `Panel_basit_…`, `Panel_secim_degisince_…`, `Kasalar_listesi_…`, tablo satırları (Hurda ×2, KarZarar ×2, KasaRaporu, Raporlar aylık/stok-degeri/gün sonu) | cmp AYNI |
| MB-7 → **TEST AÇIĞI bulundu** | Kilitli rolde istemci durumu şubeyi tutsun (`d.TumSubeler &&` silindi) | İlk koşum: **0 kırmızı** (davranış `GorunumSubesiIcin`'deki ikinci koşulla maskeleniyordu, ama arama bileşenlerinin okuduğu genel değer etkilenirdi). Test güçlendirildi (`GorunumIcin(Oturum)!.SubeId` null olmalı). | cmp AYNI |
| MB-7b | aynı mutasyon, güçlendirilmiş testle | `Sube_kilitli_rolde_istemci_subeId_GONDERMEZ_sunucu_oturum_subesini_uygular` | cmp AYNI |
| MB-8 → **TEST AÇIĞI bulundu** | Basit "Bugün" gün sonu `subeId` taşımasın | İlk koşum: **0 kırmızı** (tablo testi Pro yüzü ölçüyordu). Yeni test `Raporlar_basit_yuzde_bugun_ozeti_ve_gun_sonu_subeId_tasir`. | cmp AYNI |
| MB-8c | (son kodla) `PosFaturaMutabakati(null, null)` | `Raporlar_basit_yuzde_bugun_ozeti_ve_gun_sonu_subeId_tasir` | cmp AYNI |
| MB-10 | (son kodla) Pro mutabakat `subeId` null | `Subeli_ekran_cagrisi_…(Raporlar, /raporlar/pos-fatura-mutabakati)` | cmp AYNI |
| MB-9 | `GorunumBekle` profil beklemesi kapatıldı (`if (false)`) | `Sayfa_yenilemede_ilk_istek_bile_secili_subeyle_gider_titreme_yok` | cmp AYNI |

## 10. denetim.sh
Son koşum (son kodla): `DENETİM BAŞARISIZ` — **tek ✗ P-30 DEMO_INSAN_SURESI** (demo prova kronometre beyanı; orkestratör koşulunda istisna "denetim TEMIZ (P-30 hariç)"). Bu pakete ait sayaçlar ✓: `TOPLU-1: TARIH_TEK 0` (ilk denemede gün sonu tarihi `ToString("yyyy-MM-dd")` ile yazılmıştı → ✗ 1 → `Bicim.Makine`'ye çevrildi → 0), `P-03: SOZLESME_KILITSIZ_TIP 58 (tavan 58)`, `Pages: ModalPencere kullanımı 48 (tavan 55)`, `P-04: GUVENLIK_ELLE_IZOLASYON 0`. Günlük: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/c-denetim-son.log`.

## 11. Canlı doğrulama (5160, `ASPNETCORE_ENVIRONMENT=Development`, `--no-build`)
Test kiracısı `PAKETSUBE-KAPSAM-B-TEST` API ile kuruldu (yerel sistem anahtarı bu sürece rastgele üretildi, değer basılmadı; davet → kayıt). Merkez Şube + Şube B, iki kasa, bir cari, TEZGÂHTAR A (Merkez), TEZGÂHTAR B (Şube B), MÜDÜR (Şube B). Veri: Merkez 5.000 + 2.000 TL avans (TEZ A), Şube B 3.000 TL avans (TEZ B), hurda alışı Merkez 2 g K22 (SAHİP), Şube B 1 g K22 (MÜDÜR). İlk kurulumda 10 g / 4 g hurda denemesi **400** aldı (tevsik 30.000 TL nakit sınırı; B kasası bakiyesi yetersiz) — gramlar düşürülerek tekrarlandı.

### 11.1 API (istemcinin gönderdiği `?subeId=` ile) = SQL
| Görünüm | `/raporlar/gunluk` hareket · hurda TL · kasa toplamı | SQL hareket · hurda TL · kasa bakiyesi | `/kasalar` | `/hurda/bakiye` has | `/hurda/defter` | `/raporlar/aylik` | `/emanet/{cari}` | `/pos/tahsilatlar` |
|---|---|---|---|---|---|---|---|---|
| Merkez | 200 · 3 · 5.129,60 · 1.870,40 (SUBE) | 3 · −5.129,60 · 1.870,40 | 200 [Merkez Kasa SKB] | 200 · 1,832 (SUBE) | 200 · 1 | 200 | 200 · 0 | 200 · 0 |
| Şube B | 200 · 2 · 2.564,80 · 435,20 (SUBE) | 2 · −2.564,80 · 435,20 | 200 [B Kasası SKB] | 200 · 0,916 (SUBE) | 200 · 1 | 200 | 200 · 0 | 200 · 0 |
| Tüm şubeler | 200 · 5 · 7.694,40 · 2.305,60 (TUM_SUBELER) | 5 · −7.694,40 · 2.305,60 | 200 [ikisi] | 200 · 2,748 (TUM_SUBELER) | 200 · 2 | 200 | 200 · 0 | 200 · 0 |

SQL (hurda has, şube başına): Merkez 1,832000 · Şube B 0,916000 → toplam 2,748 = birleşik. Kasa bakiyeleri `kasalar.bakiye` ile birebir. (SQL'de hurda TL eksi işaretli tutulur — kasadan çıkış; ekran "ödenen" olarak pozitif gösterir.)
TEZGÂHTAR A `?subeId=B`: `/kasalar`, `/hurda/bakiye`, `/emanet/{cari}`, `/pos/tahsilatlar` → **403** "Bu şubeyi görme yetkiniz yok."; `subeId`'siz → 200 (yalnız kendi şubesi). (`/raporlar/gunluk` tezgâhtara rol gereği 403 — şubeden bağımsız.)

### 11.2 Gerçek tarayıcı senaryosu (başsız Chrome, CDP, 1280×800, PRO)
Betik `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/canli/senaryo.mjs`; günlük `senaryo/senaryo.jsonl`; görüntüler `senaryo/foto/`.
| Adım | Beklenen | Gerçek (ölçüldü) | Kanıt |
|---|---|---|---|
| Seçici "Şube B" | rozet "Gösterilen şube: Şube B"; günlük isteği `subeId=B` | rozet ✓; istek `…gunluk?tarih=2026-10-01&subeId=…3DNS`; Net has 0,9160 · nakit akışı −2.564,80 ₺ (= SQL Şube B) | `2-panel-subeB-1280.png` |
| **K-7** F5 | aynı şube seçili, aynı rakamlar | rozet "Şube B", seçici "Şube B"; **tek** günlük isteği, şubeli (düzeltme öncesi: önce şubesiz sonra şubeli — § 1) | `3-panel-F5-subeB-1280.png` (Read ile görüldü) |
| **K-8** ikinci profil (ayrı `--user-data-dir`) | aynı seçim | rozet "Şube B", seçici "Şube B", istek şubeli | `4-profil2-subeB-1280.png` |
| "Tüm şubeler" | birleşik | rozet "Gösterilen: tüm şubeler (birlikte)"; istek şubesiz; Net has 2,7480 · −7.694,40 ₺ (= SQL toplam) | `5-panel-tum-1280.png` (Read ile görüldü) |
| Kasalar, tüm şubeler | iki kasa, kartta şube adı | "Şube: Merkez Şube", "Şube: Şube B" | `6-kasalar-tum-1280.png` |
| "Merkez Şube" → Kasalar | yalnız Merkez kasası | istek `/kasalar?subeId=…GMJV`; tek kart 1.870,40 ₺ | `7-kasalar-merkez-1280.png` |
| Hurda, Merkez | 1,832 g | `/hurda/bakiye?subeId=…GMJV`; kart 1,832 g | `8-hurda-merkez-1280.png`, `tur/foto/hurda__BASIT_aydinlik_1280.png` (Read ile görüldü) |
| TEZGÂHTAR A | seçici ve rozet YOK | `seciciSayisi 0 · rozetSayisi 0`; Kasalar yalnız Merkez Kasa | `tezgahtar-kasalar-1280.png` (Read ile görüldü) |
| Kayıtlı şube (B) kapatıldı | sarı bant "kendi şubenize dönüldü", oturum şubesi | API `gecersizTercih: true`, `subeId = Merkez`; ekranda `#gorunum-gecersiz` "Seçtiğiniz şube kapalı; kendi şubenize dönüldü."; istek `subeId=Merkez`; seçici/rozet 0 (tek aktif şube) | `tekgecersiz-panel-1280.png` (Read ile görüldü) |
| **K-14** tek aktif şube, tercih silindi | rozet ve seçici 0 | Panel + Kasalar: `rozetSayisi 0 · seciciSayisi 0`, istekler şubesiz | `tektemiz-panel-1280.png`, `tektemiz-kasalar-1280.png` |

### 11.3 K-13 sığma/taşma ölçümü (`betikler/tur/tur.mjs` + `sikisma.js`, SAHİP tercihi = Merkez → rozet her ekranda görünür)
10 rota × 1280 + 375 × BASİT + PRO × açık + koyu = **80 sayfa ölçümü** (+ 52 sekme + 332 açıcı katman; `hata 0 · geçersiz 0`). Ham: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/canli/tur/olcum-{1..4}.jsonl`, görüntüler `tur/foto/` (80 sayfa + sekme/katman).
**Sonuç: 80/80 satırda K1 gizli-görünen 0 · K3 taşan/kırık 0/0 · K5 TRY 0 · K9 binen 0 · yatay kaydırma 0.** K2 sağ kart %97–98 (dar sütun yok; `/raporlar/kasa` ve `/emanet` 1280'de tek blok → ölçü yok).

| Rota | Mod | Tema | Genişlik | K1 | K3 taşan/kırık | K5 TRY | K9 binen | Yatay | K2 % |
|---|---|---|---|---|---|---|---|---|---|
| /, /kasalar, /hurda, /raporlar, /pos/tahsilatlar, /raporlar/kar-zarar, /raporlar/islem-defteri, /raporlar/kasa, /emanet, /urunler | BASİT | açık | 1280 | 0 | 0/0 | 0 | 0 | 0 | 98 (2 rota ölçüsüz) |
| (aynı 10 rota) | BASİT | açık | 375 | 0 | 0/0 | 0 | 0 | 0 | 97 |
| (aynı 10 rota) | BASİT | koyu | 1280 | 0 | 0/0 | 0 | 0 | 0 | 98 |
| (aynı 10 rota) | BASİT | koyu | 375 | 0 | 0/0 | 0 | 0 | 0 | 97 |
| (aynı 10 rota) | PRO | açık | 1280 | 0 | 0/0 | 0 | 0 | 0 | 98 |
| (aynı 10 rota) | PRO | açık | 375 | 0 | 0/0 | 0 | 0 | 0 | 97 |
| (aynı 10 rota) | PRO | koyu | 1280 | 0 | 0/0 | 0 | 0 | 0 | 98 |
| (aynı 10 rota) | PRO | koyu | 375 | 0 | 0/0 | 0 | 0 | 0 | 97 |
(Rota başına satırlar: `/private/tmp/kuyumcu-serit/SUBE-KAPSAM-B/canli/ozet-tablo2.md`, 80 satır.)

`analiz.py` diğer bulgular (bu paketle ilgisiz, önceden var olan): (a) 375'te genel arama katmanı "SERBEST-YUZEN" (GlobalArama — SK-A raporunda da var); (b) K12 ezik `#rp-yil`/`#rp-ay` (Raporlar PRO 1280 yıl/ay seçicisi — rozet ayrı satırdadır, genişliği etkilemez); (c) K8 "jargon SK-B" — test verisindeki cari adı "SK-B Canli Musteri"dir, ekran metni değil.
Read ile görülen görüntüler: `kasalar__BASIT_aydinlik_375.png` (rozet buton altında tek satır, taşma yok), `kok__BASIT_karanlik_375.png`, `kasalar__PRO_aydinlik_1280.png`, `raporlar__PRO_karanlik_1280.png` (Merkez: net has 1,8320 · tahsilat 7.000 ₺ = SQL), `hurda__BASIT_aydinlik_1280.png`.

### 11.4 Temizlik
API PID ve vekil PID `kill` → `lsof :5160` ve `:5161` boş. Test kiracısı `zsh kiraci-temizle.sh 01M3VSYS2BAPRG1BYWPJ5DMEM6 --uygula` ile silindi ("SİLİNDİ … kalan kiracı sayısı: 14"); kendi davet satırı silindi (kalan 0); `tenants WHERE id=<benim>` = 0. **tenants: başlangıç 13 → son 14** — fark başka şeritlerin o arada açtığı test kiracılarıdır (`PAKETTOPLU-2-TEST…`, `PAKETRAPOR-YASLANDIRMA-TEST…` vb.; listede görünüyor), bu paketin kiracısı 0. `QA-TEST-*` kiracılarına dokunulmadı. Yerel sistem anahtarı ve fikstür dosyaları `printf ''` ile boşaltıldı.

## 12. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/Panel.razor`, `Kasalar.razor`, `Hurda.razor`, `PosTahsilatlar.razor`, `Emanet.razor`, `Urunler.razor`, `Raporlar.razor`, `KarZarar.razor`, `IslemDefteri.razor`, `KasaRaporu.razor`; `Bilesenler/SubeSecici.razor`, `GorunumRozeti.razor` (yeni), `GlobalArama.razor`, `KayitSecici.razor`, `Secici.razor` (yalnız ad).

## 13. Değişen dosyalar (bu paket)
Not: depo tek ilk commit'li olduğundan `git status --porcelain` çıktısında bu dosyaların çoğu `??` (izlenmeyen) görünür; liste paketin yazdığı dosyalardır.
- `dotnet/Kuyumcu.Web/Servisler/KabukServisi.cs`
- `dotnet/Kuyumcu.Web/Servisler/SubeliSorgu.cs` (YENİ)
- `dotnet/Kuyumcu.Web/Servisler/PosServisi.cs`
- `dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/GorunumRozeti.razor` (YENİ)
- `dotnet/Kuyumcu.Web/Bilesenler/SubeSecici.razor`, `GlobalArama.razor`, `KayitSecici.razor`, `Secici.razor`
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`, `Kasalar.razor`, `Hurda.razor`, `PosTahsilatlar.razor`, `Emanet.razor`, `Urunler.razor`, `Raporlar.razor`, `KarZarar.razor`, `IslemDefteri.razor`, `KasaRaporu.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SubeKapsamB.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/GorunumSubesiEkranTestleri.cs` (YENİ), `KabukAramaEkranTestleri.cs`, `TK2KabukTuvalTestleri.cs`, `P37RevizyonTestleri.cs`
- bu rapor

## 14. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K-7 yenilemede seçim kalır, Panel aynı rakam | KARŞILANDI | § 11.2 adım F5 + `3-panel-F5-subeB-1280.png`; bUnit `K7_…`, `Sayfa_yenilemede_…` |
| K-8 ikinci profil aynı seçim | KARŞILANDI | § 11.2 `4-profil2-subeB-1280.png` |
| K-9 yazma 500 → eski değer + HataBandi | KARŞILANDI | bUnit `K9_…`; M8-1/M8-2 kırmızı |
| K-13 1280+375 · BASİT+PRO · açık+koyu, taşma 0 | KARŞILANDI | § 11.3, 80 ölçüm, sayaçlar 0 |
| K-14 tek şube / kilitli rol: rozet + seçici 0 | KARŞILANDI | bUnit `K14_…` ×2 + tarayıcı `tektemiz-*`, `tezgahtar-*` |
| M7 / M8 mutasyonları | KARŞILANDI | § 9 |
| Canlı: Panel/Kasalar/Hurda/Emanet/Raporlar sayıları = SQL, "Tüm şubeler" toplam | KARŞILANDI | § 11.1 + § 11.2 (Emanet ve POS'ta veri 0 — ikisi de 200 ve SQL 0; emanet işlemi kurulmadı) |
| Kilitli tezgâhtarda seçici yok; başka şube 403 | KARŞILANDI | § 11.1, § 11.2 |
| Build 0 hata | KARŞILANDI | `c-b-son.log`: 0 Hata, 2 Uyarı (başka şeritlerin test dosyaları: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`, `KesifP2Testleri.cs(454,9)`) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | § 10 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § 8: 8936 / 0 başarısız |
| Test kiracısı silindi | KARŞILANDI | § 11.4 |
| COMMIT yok · dosya silme yok · sır basılmadı · 5199/5200 dokunulmadı | KARŞILANDI | — |

## 15. Açık kalanlar
0. **(Tur 2)** Kim borçlu / Bekleyen iş / banka satırı ve Raporlar cari sekmesi KAPANDI (§ 0.1). Kalan: Basit Panel **"Altın pozisyonum"** kartı (`AltinPozisyonuKarti` bileşeni — şerit dosya alanında değil; `/pozisyon` sunucuda şubesiz) şube seçiliyken kiracı genelini gösterir (canlı görüntüde "+2,7 g has" = iki şubenin toplamı, rozet "Şube B"). Sahip: Panel/Katman-8 + SK-A devamı. Ayrıca Raporlar ve İşlem defteri `GorunumDegisti` akışlarına eski-yanıt koruması (Panel/KasaRaporu kalıbı) uygulanmadı — [DÜŞÜK, olası], sahip: bu şeridin bir sonraki dokunuşu ya da TOPLU şerit.
1. **[ORTA → tur 2'de kısmen kapandı] Panel'de kiracı geneli kartlar şube rozetinin altında:** Basit Panel "Altın pozisyonum" (`/pozisyon` — SK-A'da şube parametresi YOK) ve "Kim borçlu?" kiracı genelini gösterir; sayfa rozeti "Gösterilen şube: Merkez" der. Bu paketin Panel yetkisi "yalnız şube parametresi" olduğundan kartlara "tüm şubeler" alt notu eklenmedi. Sahip: Panel/Katman-8 tasarım şeridi + `/pozisyon` için sunucu (SK-A devamı). Aynı durum Raporlar "Cari yaşlandırma" sekmesi için (kiracı geneli — `GorunumRozeti KiraciGeneli` sekme düzeyinde kullanılabilir).
2. **[DÜŞÜK] Tek aktif şubede kapanmış-şube bandı kalıcı:** kayıtlı tercih kapanan şubeyi gösteriyor ve geriye tek aktif şube kaldıysa seçici çizilmediği için kullanıcı bandı kapatamaz (tercih sunucuda geçersiz kalır). Öneri: tek şubede istemci tercihi `null`'a yazsın ya da sunucu okurken kendiliğinden temizlesin. Sahip: SK-A/TercihModulu + bu bileşen.
3. **[DÜŞÜK] `UyumServisi.Mutabakat/BugunMutabakati`** artık Raporlar'dan çağrılmıyor (gün sonu `RaporServisi.PosFaturaMutabakati` üzerinden şubeli); başka çağıran yoksa UyumServisi şeridi bu iki metodu `subeId` alacak şekilde birleştirebilir (tek kaynak). Sahip: Uyum servis şeridi.
4. **§ 4.2.3 `kapsam: "KIRACI_GENELI"` alanı** `/cariler*`, `/faturalar*`, `/bankalar*` yanıtlarında hâlâ yok (SK-A açık kalan 7) → Cariler/Faturalar/Banka ekranlarına "müşteri hesapları şubeye bölünmez" rozeti bu pakette KONMADI (o sayfalar şerit dosya alanında değil; `Cariler.razor`/`Faturalar.razor` açıkça "Dokunmaz"). Bileşen hazır: `<GorunumRozeti KiraciGeneli="true" />`.
5. Önceden var olan ölçüm bulguları (§ 11.3 a–b): GlobalArama 375 serbest yüzen katman; Raporlar yıl/ay seçicisi K12 ezik. Sahip: Katman-8/TOPLU tasarım şeridi.
6. Mehmet (06 Güvenlik): SK-A'nın SistemKapsami madde 10 onayı orkestratör kapısında (SK-A açık kalan 3) — bu paket sunucuya dokunmadı.

## 16. Kurucuya / mali müşavire not
- Kurucuya: "Ayarlarda şube seçince listeler ve paneldeki bilgiler o şubeye göre gelsin; Tüm şubeler deyince karışık gelsin" isteği çalışır durumda ve gerçek tarayıcıda ölçüldü. Seçim sunucuda saklandığı için sayfa yenilense de, başka bilgisayardan aynı kullanıcıyla girilse de aynı şube açılır. Tezgâhtar seçici görmez, yalnız kendi şubesini görür.
- Mali müşavire: bu paket yeni yasal kural getirmedi; şube seçimi yalnız ekranda GÖSTERİMİ daraltır, tevsik/MASAK toplamları her zaman bütün işletme üzerinden hesaplanır (SK-A).
