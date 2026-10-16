# PAKET KESIF-P1 · PARA-VERİ-YETKİ — uygulama raporu (30.09.2026, t6 · API 5168 · vekil 5169 · CDP 9601–9613 · `KUYUMCU_TEST_DB=kuyumcu_test_t6`)


## 00. TUR 3 (30.09 17:55–18:40) — orkestratör 18:10 bulgularının kapanışı

| Bulgu (orkestratör 18:10) | Kök neden (ölçüldü) | Düzeltme | Kanıt |
|---|---|---|---|
| (1) **Pos tablosu**: ad/banka tek satır zorlaması, eylem sütunu 1440'ta kesik | `hucre-tek-satir` ad/banka hücrelerinde; 7 sütun + 2 düğme 1440'ta taşıyordu (hakem 25 no'lu görüntü) | Tablo 7 → **5 sütun**: "Cihaz ve banka" (ad + altında banka, **SARAR**, `max-width:16rem; overflow-wrap:anywhere`), "Komisyon oranı" (altında "Valör n gün"), Bekleyen alacak, Durum, İşlemler. Eylem kümesi `.satir-eylem` (769–1399'da olcek.css 13b sağa yapışık; tablo zaten sığdığı için örtüşme yok). 375'te `q10-yigin` kart yığını (her hücre `data-etiket`, eylem `q10-yigin-eylem`). | Ölçüm (16 koşum): iç taşma **0** (1440/1280/960), görünmeyen düğme **0/6**, 375'te satır `display:block` (kart), sayfa yatay 0, ad/banka `hucre-tek-satir` 0. bUnit `Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin` + CSS `KesifP1_css_kurallari_yerinde_sabit_renk_yok`. Görüntü `m1/foto/_pos__BASIT_aydinlik_960.png`, `_pos__BASIT_karanlik_375.png` (Read ile görüldü). |
| (2) **GorunumKarti onay işareti** üç kalıp | yazı boyutunda ✓ "18 px" metnine **bitişik (0 px)**; ekran modunda hiç yoktu; şemada "Seçili" rozeti; app.css `.gk-sema-alt {flex-wrap:wrap}` 960'ta rozeti/✓'yi adın ALTINA düşürüyordu (ilk ölçüm: boşluk −83/−93) | **TEK KALIP**: her seçili kartta tek `✓` (`.gk-isaret`, Ikon), başlık satırının **SAĞINDA ayrı öğe** (satır sarmaz, `margin-inline-start:auto`, ad–✓ arası ≥ `--bosluk-1` = 4 px). Şema kartında da yalnız ✓ ("Seçili" sözcüğü `gorsel-gizli`, rozet zemini sıfırlandı; test uyumu için sınıf korundu). Ekran modu kartına ✓ eklendi. 960'ta seçenek iç payı 16 → 12 px (ilk ölçümde "Profesyonel" K3 taşan 1'di). | Ölçüm 16 koşum × 4 seçili kart: **ad–✓ boşluğu en az 8 px** (960 PRO "Profesyonel"), ✓ sağ payı **0 px** (hepsi sağa dayalı), K3 0. bUnit `GorunumKarti_secili_isaret_tek_kalip_sagda_ayri_oge`. Görüntü `m1/foto/_ayarlar__BASIT_aydinlik_960.png`. |
| (3) **FiyatListesiUrunPaneli**: teknik satırlar, 1440'ta eylem kesik, süzgeç etiketi dağınık | "Fiyatlar sunucuda hesaplanır; sayfa başına en fazla 50 satır…" + "Süzgeçe uyan toplam ürün: 12" satırları; 12 sütunlu PRO tablo 1440'ta 11 px taşıyor, 12 "Ürüne özel" düğmesinin **12'si** görünmüyordu; arama kutusunun görünür etiketi yoktu | İki teknik satır **kaldırıldı**; sınır yalnız sınıra değince tek kısa uyarı (`#flp-daha-fazla`, "Daha fazlası var: {0} üründen {1} tanesi görünüyor. Kategori seçin ya da arayın."). Kod ürün adının altına; **seçim kutusu + ad + kod + "Ürüne özel" İLK sütunda ve bu sütun SOLA YAPIŞIK** (`kesif-p1.css`) → PRO'da tablo kendi kabında sağa kayarken eylem hiç ekrandan çıkmaz; sola yapışık sütun kaydırma 0 iken doğal yerindedir → K9 binme **0** (sağa yapışık denemesi 960'ta K9 6 üretti, bırakıldı). BASİT'te Kaynak ve İşçilik sütunları çizilmez (8 sütun). Süzgeç: "Ürün ara" ve "Kategori" iki kutu yan yana, **ikisinin de etiketi üstte**; onay kutusu kendi satırında. | Ölçüm 16 koşum: 1440 ve 1280'de iç taşma **0** (BASİT + PRO), görünmeyen eylem **0/12** tüm genişliklerde, `#flp-sinir`/`#flp-toplam` 0, süzgeç etiket üst farkı **0 px**, kutu alt farkı **0 px** (≥769). 960 PRO iç taşma 276 / BASİT 139, 375 581–781: tablo KENDİ kabında kayar (sayfa yatay 0; orkestratör metni "PRO'da tablo kendi kabında kayar ama EYLEMLER sabit görünür"). bUnit `FiyatListesiUrunPaneli_teknik_satir_yok_suzgec_tek_etiket`, `…_sinira_degince_tek_kisa_uyari`, `…_PRO_eylem_ilk_sutunda_BASIT_kaynak_iscilik_gizli`. Görüntü `m2/foto/_fiyatlandirma__PRO_aydinlik_1440.png`, `…PRO_karanlik_375.png`, `m1/foto/_fiyatlandirma__BASIT_aydinlik_1440.png`. |
| (4) **İşletme**: "SAHİP", "AI modülü", "dört göz kuralı" | `Dil.T.Isletme.Aciklama` ("…yalnız SAHİP rolüne açıktır"), `YalnizSahip` (kilit sebebi), `AiModulu`, `Vezne.AyrimAciklama` — hepsi HUB sözlükte (P8'in) | HUB'a dokunmadan çağrı yerleri `Sozlukler.KesifP1.cs`'teki yeni metinlere bağlandı: "İşletme bilgisi, abonelik planı ve şube yönetimi." · kilit sebebi "Bu ayarı yalnız dükkân sahibi değiştirebilir." · **"Akıllı öneriler"** · "Açıkken fişi hazırlayan kişi aynı fişin parasını alamaz; parayı başka biri alır." | Ölçüm 16 koşum: görünür metinde `SAHİP|SAHIP` **0**, "AI modül" **0**, "dört göz" **0**, "Akıllı öneriler" 1. bUnit `Isletme_ham_rol_kodu_ve_jargon_yok`, `Isletme_kilit_sebebinde_ham_rol_kodu_yok` (kilitli işletmede). Görüntü `m1/foto/_isletme_sekme_ayarlar__PRO_aydinlik_1440.png`. /kur ikinci kaynak seçicisine dokunulmadı (P6). |

### Tur 3 ölçüm tablosu (başsız Chrome, CDP 9601, vekil 5169 → API 5168; `sikisma-olc.js` + ek ölçüm; `/private/tmp/kuyumcu-serit/KESIF-P1/canli/t3/m1/olcum.jsonl` 64 satır + `m2/olcum.jsonl` 16 satır son CSS ile /fiyatlandirma yeniden ölçümü)

| Rota | Genişlik | Mod × tema | K1 | K3 taşan/kırık | K5 TRY | K9 binen | K11 | Sayfa yatay | en-US | type=number | Ek (sayfaya özgü) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| /pos | 1440·1280·960 | BASİT/PRO × açık/koyu (12) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | tablo iç taşma 0, görünmeyen düğme 0/6 |
| /pos | 375 | BASİT/PRO × açık/koyu (4) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | kart yığını (tr=block), görünmeyen düğme 0/6 |
| /fiyatlandirma (panel açık) | 1440·1280 | 8 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | iç taşma 0; eylem görünmeyen 0/12 (PRO); teknik satır 0; etiket/kutu hiza farkı 0/0 |
| /fiyatlandirma | 960·375 | 8 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | tablo kendi kabında kayar; eylem görünmeyen 0/12 (sola yapışık) |
| /ayarlar | 1440·1280·960·375 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 4 seçili kartın hepsinde ✓ sağda (sağ pay 0), ad–✓ en az 8 px |
| /isletme?sekme=ayarlar | 1440·1280·960·375 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | SAHİP 0 · AI modül 0 · dört göz 0 |

Her satırda `html[data-mod]` ve `html[data-tema]` istenenle aynı (sunucu PATCH'iyle kuruldu). Görüntüler Read ile görüldü (yukarıda adı geçenler).

### Tur 3 ölçüleri
- **Build** `dotnet build Kuyumcu.sln --nologo --no-incremental` → **0 hata**, 1 uyarı (yalnız `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`, SUBE-KAPSAM-A'nın; bu paketin dosyalarında 0). Log `t3-build-son.log`.
- **Filtreli** (KesifP1 + KESIFP1 + etkilenen 21 sınıf): **836/836** (tur 3 testleri dahil, t3-test2.log); kilitli-işletme testi eklendikten sonra KesifP1 İşletme 7/7 (t3-test3.log); son söz tam süit.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pKESIF-P1-t3/_Ramazan-MacBook-Air_2026-09-30_18_31_19.trx`): **8028 toplam, 8026 geçti, 1 başarısız, 1 yürütülmedi**. Tek kırmızı `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` — tur 1 ve 2'deki devir listesiyle aynı, sahibi KATMAN-2/JETON. **Fark = 0.**
- **denetim.sh** → `DENETİM TEMİZ.` (✗ 0; log `t3-denetim2.log`).
- **Yeni test (tur 3): 9** — `GorunumKarti_secili_isaret_tek_kalip_sagda_ayri_oge`, `Isletme_ham_rol_kodu_ve_jargon_yok`, `Isletme_kilit_sebebinde_ham_rol_kodu_yok`, `FiyatListesiUrunPaneli_teknik_satir_yok_suzgec_tek_etiket`, `FiyatListesiUrunPaneli_sinira_degince_tek_kisa_uyari`, `FiyatListesiUrunPaneli_PRO_eylem_ilk_sutunda_BASIT_kaynak_iscilik_gizli`, `Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin`, `KesifP1CssTestleri.KesifP1_css_olcekten_once_yuklenir`, `KesifP1CssTestleri.KesifP1_css_kurallari_yerinde_sabit_renk_yok`. Paket toplamı 28 + 5 + 9 = **42**.
- **Mutasyon (tur 3, 17/17 kırmızı, hepsi geri alındı, cmp AYNI; betik `mut3.py`, sonuç `mut3-sonuc.json` + `mut3-sonuc-M1-38.json`):**

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1-23 | Pos ad hücresine `hucre-tek-satir` | Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin | AYNI |
| M1-24 | Pos tablosundan `q10-yigin` | Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin | AYNI |
| M1-25 | Pos eylem kabından `satir-eylem` | Pos_tablo_ad_banka_sarar_eylem_sabit_mobil_yigin | AYNI |
| M1-26 | ✓ yeniden "18 px" metninin içine | GorunumKarti_secili_isaret_tek_kalip_sagda_ayri_oge | AYNI |
| M1-27 | Ekran modu kartından ✓ kaldır | GorunumKarti_secili_isaret_tek_kalip_sagda_ayri_oge | AYNI |
| M1-28 | işaret satırı `wrap`, boşluk 0 | KesifP1_css_kurallari_yerinde_sabit_renk_yok | AYNI |
| M1-29 | Pos ad hücresi `nowrap`, en çok genişlik yok | KesifP1_css_kurallari_yerinde_sabit_renk_yok | AYNI |
| M1-30 | fiyat paneli ilk sütunu `position: static` | KesifP1_css_kurallari_yerinde_sabit_renk_yok | AYNI |
| M1-31 | index.html'den kesif-p1.css bağlantısı | KesifP1_css_olcekten_once_yuklenir | AYNI |
| M1-32 | teknik satır `#flp-sinir` geri | FiyatListesiUrunPaneli_teknik_satir_yok_suzgec_tek_etiket | AYNI |
| M1-33 | sınır uyarısı `>=` (her zaman) | FiyatListesiUrunPaneli_teknik_satir_yok_suzgec_tek_etiket | AYNI |
| M1-34 | Kaynak başlığı BASİT'te de | FiyatListesiUrunPaneli_PRO_eylem_ilk_sutunda_BASIT_kaynak_iscilik_gizli | AYNI |
| M1-35 | "Ürüne özel" BASİT'te de | FiyatListesiUrunPaneli_PRO_eylem_ilk_sutunda_BASIT_kaynak_iscilik_gizli | AYNI |
| M1-36 | "AI modülü" geri | Isletme_ham_rol_kodu_ve_jargon_yok | AYNI |
| M1-37 | "dört göz kuralı" açıklaması geri | Isletme_ham_rol_kodu_ve_jargon_yok | AYNI |
| M1-38 | kilit sebebi HUB `YalnizSahip` ("SAHİP") | Isletme_kilit_sebebinde_ham_rol_kodu_yok (ilk koşumda hiçbir test kırılmadı → TEST AÇIĞI; kilitli işletme testi eklendi, yeniden koşumda kırmızı) | AYNI |
| M1-39 | sayfa açıklaması HUB `Isletme.Aciklama` | Isletme_ham_rol_kodu_ve_jargon_yok | AYNI |

Tuzak (kayda geçti): `shutil.copy2` geri yüklemede ESKİ mtime'ı da geri getirir → artımlı derleme son mutantı güncel sanıp eski DLL'le koşabilir (M1-39'dan sonra 2 test yanlış kırmızı verdi). Dosyalara `touch` + betiğe `os.utime` eklendi; son durum `--no-incremental` temiz build ile doğrulandı ve tam süit o derlemeyle (`--no-build`) koşuldu.
- **Canlı (API 5168, kiracı `PAKETKESIF-P1-TEST Kuyumculuk` 01M3SDAGJQWEZKBYH342BVN6FS; davet 201, kayıt 201, MÜDÜR 201; manuel kur 201; 12 ürün 201; fiyat listesi 200; 3 POS cihazı 200, biri PATCH aktif=false 200):** `GET /pos/cihazlar` 200 → 3 cihaz (uzun adlı "Garanti BBVA Vitrin Tezgâhı Uzun Adlı POS Cihazı 1 / Türkiye Garanti Bankası A.Ş. / 0.0185 / 2 / aktif", "Yapı Kredi / 0.0199 / 1", "Eski Akbank POS / 0.021 / 3 / pasif"). SQL `pos_cihazlari` aynı 3 satır (oran 0.0185/0.0199/0.0210, valör 2/1/3, aktif 1/1/0); `urunler` 12. MÜDÜR `GET /admin/tenant` → **403** `{"message":"Bu işlem için gerekli rol: dükkân sahibi"}` (ham kod yok).
- **Test kiracısı**: `zsh kiraci-temizle.sh 01M3SDAGJQWEZKBYH342BVN6FS --uygula` → 71 tablo taraması; 242 satır silindi (denetim 131, kur_snapshots 76, ürün 12, refresh 10, …); geç yazılan 1 `kur_snapshots` satırı ayrıca silindi (ROW_COUNT 1); davet satırı silindi (ROW_COUNT 1). Son tarama: 71 tablonun hepsinde **0**; tenants başlangıç **14** → son **14**. API 5168, vekil 5169, CDP 9601 durduruldu (dinleyen 0).

### Tur 3 sapmaları
1. **Yeni şerit stil dosyası `wwwroot/css/kesif-p1.css` + index.html'e TEK satır bağlantı.** Gerekçe: dört düzeltmenin üçü (Pos ad hücresine en çok genişlik, fiyat panelinde sola yapışık ilk sütun, Görünüm kartında sarmayan işaret satırı) CSS'siz yapılamıyordu; ortak.css/katman.css/katlanir.css/dugme.css/olcek.css (P7) ve app.css yasak. Emsal: aynı gün YASAL şeridi `nakit-siniri.css`'i, önceki şeritler emanet/zo-islem/zo-belge/yasal.css'i aynı yolla ekledi. Dosya yalnız kimlik/bileşen kapsamlı (#pos-cihaz-tablo, #flp-tablo, .gk), sabit renk 0, moda özel seçici 0; olcek.css'ten ÖNCE yüklenir (OL5 sözleşmesi korunur — OlcekTek/KesifP7/DugmeIki testleri yeşil). index.html dosyasının sahibi triyajda yazılı değil; tek satırlık ekleme rapora ve devir notuna yazıldı.
2. **Fiyat panelinde eylem SAĞA değil SOLA yapışık.** Orkestratör "EYLEMLER sabit görünür" dedi; sağa yapışık sütun (olcek.css 13b kalıbı) 960'ta ölçerde K9 binen 6 üretti (yapışık hücre alttaki Kaynak/İndirim hücrelerini örtüyor). Eylem ilk sütuna (seçim kutusu + ad + kod ile) alındı ve o sütun sola yapışık yapıldı: her genişlikte görünür, K9 0.
3. **BASİT açıklama satırı** (`#flp-basit-not`, "Liste fiyatı salt okunur. Toplu uygulama ve ürüne özel fiyat profesyonel modda açılır.") KORUNDU: teknik değil, UX-3 "ProAlan + sebebini yazan basit alternatif" kuralının zorunlu metni.
4. HUB metinleri (`Isletme.Aciklama`, `YalnizSahip`, `AiModulu`, `Vezne.AyrimAciklama`) silinmedi (P8'in dosyası); yalnız bu sayfanın çağrı yerleri yeni metne bağlandı. `Vezne.AyrimAciklama` başka sayfada kullanılmıyor; HUB'daki "dört göz" metni P8 taramasına not.

### Tur 3'te değişen dosyalar
`Kuyumcu.Core/I18n/Sozlukler.KesifP1.cs` (+9 anahtar TR+EN) · `Kuyumcu.Web/Pages/Pos.razor` (tablo) · `Kuyumcu.Web/Bilesenler/GorunumKarti.razor` (işaret kalıbı) · `Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor` (teknik satırlar, süzgeç, ilk sütun, BASİT sütunları; `@inject ArayuzModuServisi`) · `Kuyumcu.Web/Pages/Isletme.razor` (4 çağrı yeri) · **yeni** `Kuyumcu.Web/wwwroot/css/kesif-p1.css` · `Kuyumcu.Web/wwwroot/index.html` (1 bağlantı satırı) · `Kuyumcu.Tests/KesifP1Testleri.cs` (+9 test; fiyat sahte API'sine ayarlanabilir toplam, işletme sahte API'sine salt okunur bayrağı). Tur 3 öncesi hâller `/private/tmp/kuyumcu-serit/KESIF-P1/tur3-once/`. Dokunulan sayfalar (aynı paketin düzeltme turu): Pos, Isletme, (bileşen) GorunumKarti, FiyatListesiUrunPaneli.

### Tur 3 açık kalanlar
- `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_…` kırmızı — sahibi KATMAN-2/JETON (değişmedi).
- `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601` — SUBE-KAPSAM-A.
- /fiyatlandirma 960 ve 375'te ürün tablosu kendi kabında kayar (PRO 276 px, BASİT 139 px @960): eylem ve ürün adı sola yapışık olduğundan görünür; BASİT'te 960'ta "Satış fiyatı" sütunu için kaydırma gerekir → P7/tasarım turuna öneri: dar genişlikte kart yığını.
- HUB sözlükte `Isletme.Aciklama`/`YalnizSahip` ("SAHİP") ve `Vezne.AyrimAciklama` ("dört göz") metinleri duruyor — başka çağıran yok; P8 temizlesin.
- Önceki turlardan: POS sunucu ölçek > 4 → 400 (TOPLU 18:10 kaydında), fiyat paneli "Gram" başlığı (P8).

## 0. TUR 2 (30.09 16:30–17:10) — doğrulayıcı bulgularının kapanışı

| Bulgu (doğrulayıcı) | Kök neden | Düzeltme | Kanıt |
|---|---|---|---|
| **[KRİTİK] POS komisyon yüzdesi 3+ ondalık kabul, DB sessizce yuvarlıyor** ('1,855' → gövde 0,01855 → GET 0,0186) | `Sayi` kutusunda ondalık sınırı yoktu; kolon `decimal(5,4)` | `SayisalAlan`'a **`EnCokOndalik`** parametresi (varsayılan KAPALI; `TamSayi` ile aynı kalıp): fazla hane → alan altında "Virgülden sonra en çok 2 hane yazın." + `GecersizAlanKapisi` kilidi + çağırana null → istek 0. `Pos.razor`: cihaz oranı ve taksit oranı kutularında `EnCokOndalik="2"`; ikinci savunma `YuzdeGecerli()` (0..100 VE ≤2 hane) Kaydet/Taksit ekle kilitlerinde; **satır içi** oran düzenlemesinde (`OranKaydet`) aynı kural → hata bandı, PATCH 0. Yüzde 2 hane ⇒ kesir 4 hane ⇒ kolona birebir sığar. | bUnit: `Pos_komisyon_uc_ondalik_reddedilir`, `Pos_taksit_oran_uc_ondalik_reddedilir`, `Pos_satir_ici_oran_uc_ondalik_istek_atmaz`, `Sayi_EnCokOndalik_fazla_haneyi_reddeder` + doğrulayıcının `Sinir_yuzde_uc_hane_sessiz_yuvarlanmaz` YEŞİL. Canlı (5168): '1,855' → alan hatası, Kaydet pasif, kilit "«Komisyon (%)»…", **yazma 0**; '1,85' → `POST /pos/cihazlar` gövde `pesinKomisyonOran:0.0185`; taksit '2,755' → pasif + sebep, yazma 0; '2,75' → `komisyonOran:0.0275`; satır içi '3,555' → bant "Virgülden sonra en çok 2 hane yazın.", yazma 0. SQL: `pos_cihazlari.pesin_komisyon_oran=0.0185`, `pos_taksit_oranlari` tek satır `6 / 0.0275`. |
| **[KRİTİK] Fiyat paneli has 0,0001 → "0"** | `GramYaz` = `"#,##0.##"` | `GramYaz` → **`Bicim.KarHasGram(v, sade:true)`** (projenin mevcut tek kaynağı): 2 hane ("13,92"), ANCAK 2 haneye yuvarlanınca sıfıra düşen sıfırdan farklı değer 4 haneye kadar ("0,0001"). Triyajın "13,92" kabul örneği korunur (hesap değil gösterim). Kullanılmayan `TrKultur` alanı kaldırıldı. Başlığın "Gram" değil "Has" olması P8'de (HUB sözlük) açık. | bUnit: `FiyatListesiUrunPaneli_kucuk_has_sifir_gorunmez` + doğrulayıcının `Sinir_has_dort_hane_sifir_gorunmez` YEŞİL. Canlı: 0,0002 g 14 ayar ürün (SQL `has_grami=0.000117`) panelde **"0,0001"**; 22 ayar "13,91". |
| [DÜŞÜK] `KesifP1Testleri.cs(448)` CS8604 | `YetkiKapisi.Rota()` `string[]?` | `Assert.NotNull(roller)` sonrası karşılaştırma | `dotnet build Kuyumcu.sln --no-incremental`: bu paketin dosyalarında uyarı **0**. Kalan TEK uyarı `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601` — SUBE-KAPSAM-A doğrulayıcısının dosyası (bu paketin değil). |
| [DÜŞÜK] 5199 dinlemiyor | paket dışı | 5199'a dokunulmadı (kural). Orkestratöre not. | — |

### Tur 2 ölçüleri
- **Build** `dotnet build Kuyumcu.sln --nologo --no-incremental` → 0 hata, 1 uyarı (yalnız `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`, SUBE-KAPSAM-A'nın; bu paketin dosyalarında 0). Log `/private/tmp/kuyumcu-serit/KESIF-P1/t2-build2.log`.
- **Filtreli** (KesifP1 + KESIFP1 sınır + P78 + PosEkran + FiyatListesiUrunGorunumu + FiyatlandirmaUX4a + BasitModPos): **156/156**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pKESIF-P1-t2/_Ramazan-MacBook-Air_2026-09-30_16_49_57.trx`): **7920 toplam, 7918 geçti, 1 başarısız, 1 yürütülmedi**. Tek kırmızı `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` — tur 1'deki devir listesinde de olan, sahibi KATMAN-2/JETON. **Fark = 0** (bu paket yeni kırmızı üretmedi).
- **denetim.sh** → `DENETİM TEMİZ.` (✗ 0; log `t2-denetim.log`).
- **Mutasyon (tur 2, 4/4 kırmızı, hepsi geri alındı, cmp AYNI; betik `mut2.py`, sonuç `mut2-sonuc.json`):**

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-19 | `OndalikIhlali => false` (SayisalAlan) | Pos_komisyon_uc_ondalik_reddedilir, Pos_taksit_oran_uc_ondalik_reddedilir, Sayi_EnCokOndalik_fazla_haneyi_reddeder | AYNI |
| M1-20 | `YuzdeGecerli` 2 hane koşulu yok (Pos) | Pos_satir_ici_oran_uc_ondalik_istek_atmaz | AYNI |
| M1-21 | `GramYaz` eski `"#,##0.##"` | FiyatListesiUrunPaneli_kucuk_has_sifir_gorunmez, Dogrulayici.Sinir_has_dort_hane_sifir_gorunmez | AYNI |
| M1-22 | `pos-cihaz-oran`'dan `EnCokOndalik="2"` kaldır | Pos_komisyon_uc_ondalik_reddedilir | AYNI |

Son mutasyondan sonra temiz `--no-incremental` build (yukarıda).
- **Gerçek tarayıcı (başsız Chrome, CDP 9614–9616, `sikisma-olc.js`, görüntüler Read ile görüldü)** — `/private/tmp/kuyumcu-serit/KESIF-P1/canli/m3/olcum.jsonl` (16) + `m3b` (1 yeniden ölçüm):

| Rota (durum) | Genişlik | Mod × tema | K1 | K3 taşan/kırık | K5 TRY | K9 | K11 | Yatay | en-US | type=number | ek |
|---|---|---|---|---|---|---|---|---|---|---|---|
| /pos (düzenle çekmecesi, taksit oranı '2,755' hatada) | 1280 · 375 | BASİT/PRO × açık/koyu (8) | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 | 0 | ondalık hatası görünür 8/8, Taksit ekle pasif 8/8 |
| /fiyatlandirma (ürün paneli açık) | 1280 · 375 | BASİT/PRO × açık/koyu (8) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | "0,0001" hücresi 8/8, "0" has 0/8 |

\* K9 tur 1'deki ile aynı: çekmecenin ARKASINDAKİ sayfa öğesi ⟂ çekmece etiketi (ör. `p.stat-etiket "BUGÜNE KADAR YATMASI GEREK" ⟂ label "Bağlı Kasa"`) — modal katman örtüşmesi. İlk koşumda /fiyatlandirma 375 PRO açık panel yükleniyordu (tam süit eşzamanlı koşuyordu; foto'da dönen halka) → aynı hücre `m3b`'de yeniden ölçüldü: "0,0001" 1, K*=0. Görüntüler: `canli/t2/1-oran-1855.png` (alan hatası + kilit sebebi), `canli/t2/4-panel.png` ("0,0001"), `canli/m3/foto/_pos__PRO_karanlik_375.png`, `canli/m3b/foto/_fiyatlandirma__PRO_aydinlik_375.png`.
- **Test kiracısı** `PAKETKESIF-P1-TEST Kuyumculuk` (01M3S9909HVEXPTP997ZZ5YFGQ): `zsh kiraci-temizle.sh … --uygula` → 97 satır + tenants 1 silindi; 70 tenant_id tablosunda kalan **0**; `kiraci_davetleri` davet satırı silindi (ROW_COUNT 1). tenants: başlangıç **14** → son **14** (liste başka şeritlerin eklemesi/silmesiyle değişti: P2 kiracısı gitti, `PAKETSKA-TEST` geldi — bu paketin değil). API 5168 (PID 58350) ve vekil 5169 durduruldu, portlar boş.

**Sapma (bilinçli):** doğrulayıcı "tercihen sunucu da ölçeği 4'ü aşan oranı 400 ile reddetsin" dedi. `Kuyumcu.Api/Modules/Pos*` bu paketin **sahip dosyaları dışında** (t2 SUBE-YASAL23 şeridi Pos GET uçlarında çalışıyor) → dokunulmadı, açık kalanlara sahibiyle yazıldı. İstemci üç giriş yolunda da (yeni cihaz, düzenle, taksit ekle, satır içi) kapalı; API'yi doğrudan çağıran için sunucu sertleştirmesi ayrı iş.

Kapsam: triyaj § KESIF-P1 — #5 (POS kalanı), #7, #10, #12, #30, #31, #44, #45, #60, #61, #71, #72 + KIYAS **TEMA-TEK** + TOPLU § ZO-ISLEM-A "POS geri al sonrası liste yenilenmiyor" + TOPLU § 08:50 "Pos ham type=number". Commit yok. Dosya silinmedi. Migration yok (şema değişmedi).

## 1. Yapılanlar

| Bulgu | Kök neden (koda karşı doğrulandı) | Çözüm |
|---|---|---|
| **#7** radyo bir adım geride | `Secim.razor` seçimde parametre `Deger`i yerelde değiştirip çağıranı çağırıyordu; PATCH sürerken çağıran ESKİ değerle yeniden çiziyor → `.secili` eskide, tarayıcının doldurduğu radyo yenide kalıyordu (render ağacı eskiyle aynı olduğu için Blazor DOM'daki radyoyu düzeltmiyor). **Canlıda 800 ms ağ gecikmesiyle yeniden üretildi** (`checked=karanlik`, `secili=aydinlik`). | `Secim`: istek SÜRERKEN görünen değer = kullanıcının seçtiği (`bekleyen`), istek bitince çağıranın `Deger`i (sunucu sonucu) tek doğru; başarısız kayıtta eski değere döner. Radyo ve `.secili` TEK değerden (`Gorunen`) çizilir. Tüm Secim kullanıcıları için geçerli; tek seçenek otomatik seçimi (`:51-58`) değişmedi. |
| **TEMA-TEK** | İki doğruluk kaynağı: `tema.js` localStorage + kullanıcı satırı (yalnız /ayarlar açılınca okunuyordu). /ayarlar'da 3 seçici: Görünüm kartı, `TemaSecici` (cihaz), Benim görünümüm tema radyosu. | `Program.cs`: kullanıcı görünümü **açılışta ve her oturum değişiminde** okunur ve uygulanır (sıra: dükkân → kullanıcı). localStorage yalnız ilk boyama önbelleği (yorum `tema.js`, `KullaniciGorunumServisi.cs`). /ayarlar'dan cihaz tema kartı (`#ayar-tema`) ve `#benim-tema` radyosu kaldırıldı → **tek seçici = Görünüm kartı**; yeri `#benim-tema-notu` ile yazılı. "Ayrıntılı görünüm ayarları" başlığındaki "bu cihaz" vaadi kaldırıldı. |
| **#12** onaysız kritik ayar | `Isletme.razor` Secici/Onay `DegerChanged` → anında PATCH. | Maliyet yöntemi, kur kaynağı, varsayılan ekran, vezne ayrımı artık **taslak**; değişiklik varsa `#i-ayar-taslak` satırı (Vazgeç + "Değişikliği kaydet") çıkar; kaydet → ortak **OnayKipi** (ModalPencere sayacı artmaz) etki cümleleriyle; onay → YALNIZ değişen alan(lar) için mevcut uç(lar) (tek değişiklik = tek PATCH; kur kaynağı yine `PATCH /kur/kaynak` → kur yeniden çekilir); Vazgeç → istek 0, eski değer. |
| **#71** kur kaynağı ham kod | `new SecenekOgesi(k, k)` | `KurKaynakAdi.Ad(k, Dil.T)` (üst çubukla aynı çevirici). |
| **#72** yetkisiz sayfa | Sayfa düzeyinde rol kapısı yoktu; 403 → `tenant=null` → sonsuz "Yükleniyor"; `RolFiltresi` ham rol kodu. | Yeni `Bilesenler/YetkiKapisi.razor` (harita `MenuTanimi.Ogeler/AltBlok.Roller`'dan OKUNUR), `AnaYerlesim` sayfayı sarar: yetkisiz rolde sayfa ÇİZİLMEZ, tek durum "Bu sayfayı yalnız dükkân sahibi açabilir" + Ana sayfaya dön. `RolFiltresi`: "Bu işlem için gerekli rol: dükkân sahibi ya da müdür" (ad tek kaynak `KesifP1Sozlugu.RolAdi`). `Isletme`: okuma hatasında "Yükleniyor" yerine sunucu metni. |
| **#10 #60 #61** fiyat paneli | `Say()` = `InvariantCulture` "#,##0.##"; `@s.AyarId`; kısa hücreler kırılıyordu. | Tutar `Bicim.Tl` ("92.206,95 ₺"), gram tr-TR açık kültür ("13,92"), ayar `KurKaynakAdi.AyarAdi` ("22 Ayar", ham kod yalnız `title`), kısa hücreler `hucre-tek-satir`. `Fiyatlandirma.razor` (yalnız gerekeni): marj/çarpan/kampanya değeri Invariant → `Bicim.Girdi` (tr-TR). |
| **#31 #30 #5** POS | Komisyon kesir isteniyor (`type=number max=1`), tablo Invariant "1.85 %"; valör/taksit `type=number` '1,5'→1; taksit satırı 3 sütun (124 px). | Komisyon **yüzde** girilir (`Sayi`, Birim %): '1,85' → gövde 0,0185; tablo `Bicim.Yuzde` "%1,85"; satır içi taksit oranı da yüzde. `SayisalAlan`'a **`TamSayi`** parametresi (varsayılan kapalı): kesirli metin alan hatası + kapı → Kaydet pasif, istek 0. Geçersiz sayı kapısı (`GecersizAlanKapisi`) çekmeceye bağlandı ('1.500' → kilit sebebi görünür). Taksit satırı `form-grid-2` + düğme ayrı satır. `Pos.razor`da `type="number"` = 0. Zorunlu alanlar `*` taşır. |
| **#44 #45** /ayarlar | 960'ta "Fildişi (açık menü)", "Çok büyük" kırılıyor; not 75 px sütuna sıkışıyor; ikon–metin arası 0 (Blazor boşluk düğümünü atıyor). | CSS'e dokunmadan (CSS dosyaları P7'nin): Fildişi adı + niteleyici ayrı satır (`#gk-sema-fildisi-nitel`), yazı boyutu düğmeleri mod düğmeleriyle aynı iki satırlı kalıp (`gk-secenek-iki`), alt not kendi satırında; ikonlu düğmelerde `span.satir-ici` (gap 4 px). |
| **TOPLU ZO-ISLEM-A** geri al | Eşleştirme seçicisi (KayitSecici) son listeyi önbellekte tutuyordu. | `PosTahsilatlar.razor`: geri alma başarılı → `@key` değişir → aday listesi TAZE çekilir; boş metin sorgudan bağımsız "Eşleşmemiş banka yatması bulunamadı." |

Yeni metinler yalnız `Kuyumcu.Core/I18n/Sozlukler.KesifP1.cs` (TR+EN, konumsal record).

## 2. Sapmalar ve gerekçeleri
1. **#7 kabulündeki "Benim görünümüm'de Tema"**: TEMA-TEK kabulü (seçici 3→1) ile çelişiyordu. Tek seçici olarak tuvalde onaylı, açılışta görünen **Görünüm kartı** bırakıldı; #7'nin Tema üçlüsü (Karanlık→Aydınlık→Sistem) o seçicide, Yazı boyutu üçlüsü (20→18→Dükkân varsayılanı) Benim görünümüm radyosunda ölçüldü. Tema alanının tek başına "dükkân varsayılanına dön" yolu: kartın "Dükkân varsayılanına dön" düğmesi + `PATCH /kullanici/gorunum {temaModu:null}` ucu (sunucu değişmedi). `TemaSecici.razor` bileşeni silinmedi (silme yasak), hiçbir sayfa çizmez (test kilitli).
2. **POS geri al** düzeltmesi `PosTahsilatlar.razor`dadır (triyaj "Pos.razor" diyor; kod o ekranda). t6 şerit listesinde "Pos" var, başka şerit bu dosyayı sahiplenmiyor.
3. **Ölçüm genişlikleri**: triyaj 1440/960/375, görev 1280/375 diyor → hepsi ölçüldü (1440/1280/960/375).
4. Değiştirilen mevcut testler (hepsi eski/kusurlu davranışı kilitliyordu, gerekçe testte yorumlu): `TemaEkranTestleri`, `GorunumEkranTestleri`, `S1hGorselSigmaTestleri`, `S1fGorunumKartiTestleri` (3 yer), `B1GorunumEkranTestleri` (2), `P13EkranTestleri` (ham kod + anında PATCH), `PosEkranTestleri` ("1.85 %"), `ZOISLEMAEkranTestleri` (boş metin), `BildirimHttpTestleri` (403'te ham "SAHIP"), `FiyatListesiUrunGorunumuTestleri` + `FiyatlandirmaUX4aEkranTestleri` ("40,000" en-US), `P78SayiGirisiTestleri` (satır içi oran kesir).
5. **Mevzuat**: bu pakette yasal kural/eşik yok. POS komisyonu banka sözleşmesidir (mevzuat değil; metin aynen söylüyor).

## 3. Migration
Gerekmedi (şema değişmedi).

## 4. Testler
- Yeni: **28** — `Kuyumcu.Tests/KesifP1Testleri.cs` 27 (Ayarlar 6, İşletme 5, YetkiKapısı 5, Fiyat paneli 2, POS 6, Sayı 2, HTTP 1) + `ZOISLEMAEkranTestleri.KesifP1_pos_geri_al_sonrasi_aday_listesi_yeniden_cekilir`.
- Zorunlu test adları: `Ayarlar_benim_tema_radyosu_PATCH_govdesiyle_ayni`, `Isletme_secim_istek_atmaz_Kaydet_onayla_tek_PATCH`, `Isletme_iptal_eski_degere_doner`, `YetkiKapisi_MUDUR_6_rota_Yukleniyor_yok`, `FiyatListesiUrunPaneli_en_US_kulturunde_tr_TR_bicim`, `FiyatListesiUrunPaneli_ayar_adi`, `Pos_komisyon_yuzde_govdeye_kesir`, `Pos_valor_kesirli_reddedilir`, `Pos_type_number_yok`, API `RolFiltresi_mesajinda_ham_rol_kodu_yok` — hepsi var ve yeşil.
- Filtreli: KesifP1 + etkilenen 18 sınıf **439/439**; HTTP 1/1; düzeltmeden sonra ilgili 6 sınıf 127/128 (kalan 1 başkasının, aşağıda).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pKESIF-P1/_Ramazan-MacBook-Air_2026-09-30_16_09_46.trx`): **7889 toplam, 7882 geçti, 6 başarısız**. 6'nın 3'ü benim eski-davranış testlerimdi (`FiyatListesiUrunGorunumu…`, `FiyatlandirmaUX4a…`, `P78…PosOranTurkce…`) → güncellendi, yeniden koşumda yeşil. 2'si yük altında zaman aşımı (`AlisOdemeAraciEkranTestleri.SATISTAN_ALISA…`, `KabukAramaEkranTestleri.Secilen_sube…`) → tek başına yeniden koşumda yeşil. 1'i başkasının: `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra…` (IstekFirtinasiHatasi 61/60; `KenarCubugu`/`MenuTercihServisi`/`ApiIstemci` — bu paketin dosyaları değil, render yolunda paket bileşeni yok) → açık kalanlar.
- Build (tur 1): artımlı `dotnet build Kuyumcu.sln` 0 uyarı 0 hata dedi; doğrulayıcının `--no-incremental` koşumunda `KesifP1Testleri.cs(448)` CS8604 çıktı → tur 2'de düzeltildi (bkz. § 0; bu paketin dosyalarında uyarı 0). `bash dotnet/denetim.sh` → **DENETİM TEMİZ.** (son kodla, 2 kez).

## 5. Mutasyon kanıtı (18/18 kırmızı, hepsi geri alındı, `cmp` AYNI; betik `/private/tmp/kuyumcu-serit/KESIF-P1/mut.py`)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | Secim `Gorunen => Deger` (bekleyen yok) | Ayarlar_benim_tema_radyosu_PATCH_govdesiyle_ayni, Ayarlar_yazi_kaydi_basarisizsa_radyo_eski_degere_doner | AYNI |
| M1-2 | Program.cs açılışta yalnız dükkân görünümü | TemaTek_sunucu_degeri_uygulanir_ve_acilista_yuklenir (ilk koşum boş çıktı verdi — derleme yarışı; yeniden koşumda kırmızı) | AYNI |
| M1-3 | Ayarlar'a `<TemaSecici />` geri | Ayarlar_tek_tema_secici_var, Ayarlar_sayfasinda_tema_karti_ve_secici_VARDIR | AYNI |
| M1-4 | Maliyet seçimi anında PATCH | Isletme_secim_istek_atmaz_Kaydet_onayla_tek_PATCH | AYNI |
| M1-5 | Onay vazgeç taslağı sıfırlamaz | Isletme_iptal_eski_degere_doner | AYNI |
| M1-6 | Kur kaynağı `SecenekOgesi(k,k)` | Isletme_kur_kaynagi_ham_kod_yok, Isletme_KurKaynagiVeMaliyetYontemi_BoskenSecilebilir | AYNI |
| M1-7 | YetkiKapısı rol karşılaştırması `false` | YetkiKapisi_MUDUR_6_rota_Yukleniyor_yok | AYNI |
| M1-8 | RolFiltresi ham `string.Join(" veya ", roller)` | RolFiltresi_mesajinda_ham_rol_kodu_yok | AYNI |
| M1-9 | Panel tutarı Invariant | FiyatListesiUrunPaneli_en_US_kulturunde_tr_TR_bicim | AYNI |
| M1-10 | `@s.AyarId` | FiyatListesiUrunPaneli_ayar_adi | AYNI |
| M1-11 | Kesir = yüzde (/100 yok) | Pos_komisyon_yuzde_govdeye_kesir, Pos_taksit_tam_sayi_ve_yuzde | AYNI |
| M1-12 | TamSayi ihlali `false` | Pos_valor_kesirli_reddedilir, Sayi_TamSayi_kesirliyi_reddeder…, Pos_taksit_tam_sayi_ve_yuzde | AYNI |
| M1-13 | Taksit satırı `form-grid-3` | Pos_type_number_yok | AYNI |
| M1-14 | Geri alda `adayAnahtari++` yok | KesifP1_pos_geri_al_sonrasi_aday_listesi_yeniden_cekilir | AYNI |
| M1-15 | Fildişi adı bölünmez | GorunumKarti_ad_parcalari, S1f_E1_Kart_tuvalin_dort_bolumunu… | AYNI |
| M1-16 | Basit düğmesinde satir-ici yok | Ayarlar_ikonlu_dugmelerde_ikon_metin_ayri_kapta | AYNI |
| M1-17 | 403'te yine "Yükleniyor" | Isletme_tenant_403te_sonsuz_Yukleniyor_yok | AYNI |
| M1-18 | POS tablo Invariant "1.85 %" | Pos_komisyon_yuzde_govdeye_kesir, PosEkrani_CihazListesiniGosterir | AYNI |

Son mutasyondan sonra temiz `dotnet build` 0/0.

## 6. Canlı doğrulama (5168/5169, kiracı `PAKETKESIF-P1-TEST Kuyumculuk` 01M3S2W9SW119Y7S0735802H6Y; davet 201, kayıt 201, MÜDÜR 201)

| Adım | Beklenen | Gerçek |
|---|---|---|
| #7 Yazı 20→18→Dükkân, 800 ms gecikme, tıklamadan 300 ms sonra | işaretli = .secili | 3/3 eşit (20/Çok büyük, 18/Büyük, ""/Dükkân varsayılanı); şema 3/3 eşit. Gecikmesiz: PATCH `{"yaziBoyutu":20}`/`18`/`null`, GET 20/18/null. (Düzeltme öncesi aynı koşulda: checked=karanlik, secili=aydinlik.) |
| TEMA-TEK: sunucu karanlik + cihaz önbelleği "aydinlik" | /panel → /ayarlar → /panel → yenile: `data-tema` sabit | karanlik ×4; ekrandan Aydınlık → /ayarlar, /panel, yenile: aydinlik ×3, GET `ozelTemaModu=aydinlik`. /ayarlar'da aydınlık seçeneği taşıyan kontrol 1, `#tema-*` 0, `#benim-tema` 0, `#ayar-tema` 0. |
| #12 /isletme?sekme=ayarlar | seçim/klavye(↓↓↑)/tekerlek istek 0; onay metni; iptal 0 + eski; onay tek PATCH | yazma 0; onay: "Maliyet yöntemi: belirlenmedi → LIFO — son giren ilk çıkar. Bundan sonraki satışların maliyeti LIFO — son giren ilk çıkar ile hesaplanır."; Vazgeç → yazma 0, ekran "Maliyet yöntemi seç"; Onay → `PATCH /admin/tenant/maliyet-yontemi {"yontem":"LIFO"}` ×1, bant "Kaydedildi: Maliyet yöntemi". SQL `tenants.maliyet_yontemi = LIFO`. |
| #71 kur seçenekleri | ham kod 0 | "Altınkaynak", "Ankara Altın Borsası", "Truncgil" (değerler kod). |
| #72 MÜDÜR 6 rota (isletme'de 11 sn beklendi) | tek durum; sekme/düzenle 0; Yükleniyor 0; SAHIP 0 | 6/6: "Bu sayfayı yalnız dükkân sahibi açabilir", sekme 0, düzenle 0, Yükleniyor yok, SAHIP yok. Sunucu: `GET /admin/tenant` 403 `{"message":"Bu işlem için gerekli rol: dükkân sahibi"}`. |
| #10/#60 panel, tarayıcı **en-US** (`navigator.language=en-US`) | tr-TR biçim; en-US kalıbı 0; ham ayar 0 | "22 Ayar · 13,91 · 92.567,60 ₺ · 3.702,70 ₺ · 96.270,30 ₺"; en-US 0; ham 0. |
| #31/#5 POS yeni cihaz | valör '1,5' hata+pasif+istek 0; '1.500' sebep; '1,85' → 0,0185 | valör: "Tam sayı yazın", Kaydet pasif, kilit "«Valör Günü» kutusundaki sayıyı düzeltin…", yazma 0; oran '1.500': kilit "«Komisyon (%)»…", yazma 0; kayıt `POST /pos/cihazlar … "pesinKomisyonOran":0.0185,"valorGun":1`; tablo "%1,85". SQL `pos_cihazlari.pesin_komisyon_oran = 0.0185`, `valor_gun = 1`. `input[type=number]` 0. |
| #30 taksit | '2,5' reddedilir; 6 + '2,75' → 0,0275 | pasif + sebep; `POST …/taksit-oranlari {"taksitAdedi":6,"komisyonOran":0.0275}`; SQL 6 / 0.0275; 1440'ta iki kutu aynı üst kenar (673/673), düğme tek satır. |
| TOPLU geri al | geri al sonrası yatma listede, F5'siz | kurulum: banka hesabı 200, POS↔hesap 200, alış 200, KART satış 200 (net 26.991,25), GELEN hareket 200, eşleştir 200 (aday 1→0). Ekrandan geri al → bant "Eşleşme geri alındı · 26.991,25 ₺"; 6 sn sonra aday 0 → **1** ("+26.991,25 ₺ Yapı Kredi · POS Hesabı · 30.09.2026"); öncesinde boş metin "Eşleşmemiş banka yatması bulunamadı." |

### Gerçek tarayıcı ölçümü (başsız Chrome, `sikisma-olc.js`, görüntüler Read ile görüldü)
`/private/tmp/kuyumcu-serit/KESIF-P1/canli/m1/olcum.jsonl` (80) + `m2/olcum.jsonl` (32); fotoğraflar `…/foto/`.

| Rota | Genişlik | Mod × tema | K1 | K3 taşan/kırık | K5 TRY | K9 | K11 | Yatay | en-US | ham kod | type=number |
|---|---|---|---|---|---|---|---|---|---|---|---|
| /ayarlar (ayrıntı açık) | 1440·1280·960·375 | BASİT/PRO × açık/koyu (16) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /isletme?sekme=ayarlar (taslak satırı açık) | 4 × 4 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /fiyatlandirma (ürün paneli açık) | 4 × 4 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /pos (sayfa) | 4 × 4 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /pos (düzenle çekmecesi açık) | 4 × 4 | 16 | 0 | 0/0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 |
| /pos/tahsilatlar | 4 × 4 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

\* K9'un 6 kaydının tamamı çekmecenin ARKASINDA kalan sayfa öğesi ⟂ çekmece öğesi (ör. `#sayfa-basi-pos-aciklama` ⟂ `label "Cihaz Adı"`) — modal katmanın örtüşmesi, yazı binmesi değil; çekmece kapalı aynı sayfa 16/16 K9=0.
- Mod/tema doğrulaması: her satırda `html[data-mod]` ve `html[data-tema]` istenenle aynı (tema sunucu PATCH'iyle kuruldu — tek kaynak).
- #45 ikon–metin arası: 3 düğmede **4 px** (≥ 4).
- İlk ölçümde bulunan ve düzeltilen: /ayarlar 960 K3 1 ("Fildişi (açık menü)"), /pos 960 K3 1 (`td "Yapı Kredi"`), çekmecede "(isteğe bağlı)" zorunlu alanlarda → düzeltildi, yeniden ölçüldü.

## 7. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Yeni uç yok. Düzenleme yolları: işletme ayarları (mevcut PATCH uçları, onaylı), POS cihazı (mevcut Cekmece/PATCH), taksit oranı (SatirIciDuzenle). Denetim kaydı mevcut uçlarda değişmedi. Yeni düğmeler: `#i-ayar-kaydet` / `#i-ayar-vazgec` yalnız değişiklik varken çizilir (ölü düğme yok), `#yetki-kapisi-panel` bağlantı. `#pos-taksit-ekle` kilitliyken sebebi `#pos-taksit-ekle-kilit`te.

## 8. Değişen dosyalar
Kod: `Kuyumcu.Core/I18n/Sozlukler.KesifP1.cs` (yeni) · `Kuyumcu.Api/Auth/RolFiltresi.cs` · `Kuyumcu.Web/Bilesenler/YetkiKapisi.razor` (yeni) · `Kuyumcu.Web/Layout/AnaYerlesim.razor` · `Kuyumcu.Web/Bilesenler/Form/Secim.razor` · `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` · `Kuyumcu.Web/Bilesenler/Form/Sayi.razor` · `Kuyumcu.Web/Bilesenler/GorunumKarti.razor` · `Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor` · `Kuyumcu.Web/Pages/Ayarlar.razor` · `Kuyumcu.Web/Pages/Isletme.razor` · `Kuyumcu.Web/Pages/Fiyatlandirma.razor` · `Kuyumcu.Web/Pages/Pos.razor` · `Kuyumcu.Web/Pages/PosTahsilatlar.razor` · `Kuyumcu.Web/Program.cs` · `Kuyumcu.Web/Servisler/KullaniciGorunumServisi.cs` (yorum) · `Kuyumcu.Web/wwwroot/js/tema.js` (yorum).
Test: `Kuyumcu.Tests/KesifP1Testleri.cs` (yeni) · `ZOISLEMAEkranTestleri.cs` · `BildirimHttpTestleri.cs` · `TemaEkranTestleri.cs` · `GorunumEkranTestleri.cs` · `S1hGorselSigmaTestleri.cs` · `S1fGorunumKartiTestleri.cs` · `B1GorunumEkranTestleri.cs` · `P13EkranTestleri.cs` · `PosEkranTestleri.cs` · `FiyatListesiUrunGorunumuTestleri.cs` · `FiyatlandirmaUX4aEkranTestleri.cs` · `P78SayiGirisiTestleri.cs`.
(`git status --porcelain` bu dosyaların çoğunu zaten `??` gösteriyor — ağaç ilk commit'ten beri izlenmiyor; liste bu paketin gerçekte dokunduklarıdır. Önceki hâller `/private/tmp/kuyumcu-serit/KESIF-P1/yedek-once/`.)
Tur 2'de değişen: `Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` (EnCokOndalik) · `Kuyumcu.Web/Pages/Pos.razor` (EnCokOndalik="2", YuzdeGecerli, satır içi kontrol) · `Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor` (GramYaz → Bicim.KarHasGram) · `Kuyumcu.Core/I18n/Sozlukler.KesifP1.cs` (OndalikSiniri TR+EN) · `Kuyumcu.Tests/KesifP1Testleri.cs` (+5 test, 448 uyarısı, panel sahte API'ye u3 satırı). Tur 2 öncesi hâller `/private/tmp/kuyumcu-serit/KESIF-P1/tur2-once/`. Doğrulayıcının `Dogrulayici/KESIFP1SinirTestleri.cs` dosyasına dokunulmadı.
Dokunulan sayfalar (TEK DOKUNUŞ; tur 2 aynı paketin düzeltme turu, yeni sayfa açılmadı): Ayarlar, Isletme, Fiyatlandirma, Pos, PosTahsilatlar (+ Layout/AnaYerlesim).

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| #7 radyo = .secili = PATCH = GET, 3/3 | ✓ | canlı r7d/r7e + bUnit + M1-1 |
| Tema tek kaynak, seçici 3→1 | ✓ | canlı tema bölümü + M1-2/M1-3 |
| #12 istek 0 / onay / iptal / tek PATCH / klavye-tekerlek | ✓ | canlı isletme + M1-4/M1-5 |
| #71 ham kod 0 | ✓ | canlı + M1-6 |
| #72 tek durum, Yükleniyor 0, SAHIP 0, sunucu rol adı | ✓ | canlı MÜDÜR 6/6 + M1-7/M1-8/M1-17 |
| #10/#60/#61 tr-TR, en-US 0, ham ayar 0, K3 0 | ✓ | canlı en-US + ölçüm + M1-9/M1-10 |
| #30/#31/#5 yüzde→kesir, TamSayi, type=number 0, '1.500' sebep, K3 0 | ✓ | canlı + SQL + M1-11..13/M1-18 |
| #44/#45 960 K3 0, ikon boşluğu ≥4 px | ✓ | ölçüm (4 px) + M1-15/M1-16 |
| TOPLU geri al listesi | ✓ | canlı + M1-14 |
| build 0/0, denetim TEMİZ | ✓ | b-son.log, denetim2.log |
| Yeni kırmızı üretmeme | ✓ | tam süitin 6 kırmızısından 3'ü güncellendi (yeşil), 2 zaman aşımı yeniden koşumda yeşil, 1 başkasının |
| Test kiracısı silindi | ✓ | `kiraci-temizle.sh … --uygula` (zsh); 70 tenant_id tablosu 0 (geç yazılan 1 `kur_snapshots` satırı ayrıca silindi, ROW_COUNT 1); davet satırı silindi (ROW_COUNT 1); `PAKETKESIFP1%` 0 |

## 10. Açık kalanlar
- **(tur 2)** Sunucu tarafı ölçek sertleştirmesi: `POST/PATCH /pos/cihazlar` ve `/taksit-oranlari` 4 haneyi aşan oranı hâlâ kabul edip kolon yuvarlıyor (API'yi doğrudan çağıran için). Dosya bu paketin sahip listesinde değil → Pos modülünün sahibine (SUBE-YASAL23 t2 şeridi ya da sonraki POS paketi) iş kalemi: ölçek > 4 → 400 + Türkçe mesaj + test.
- **(tur 2)** `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601` uyarısı — SUBE-KAPSAM-A doğrulayıcısının.
- **(tur 2)** 5199 inceleme sunucusu dinlemiyor — orkestratör kapısında yeniden yayın değerlendirilmeli (dokunulmadı).
- `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir` kırmızı (IstekFirtinasiHatasi 61/60) — sahibi KATMAN-2/JETON (KenarCubugu, MenuTercihServisi, ApiIstemci; 30.09 04:36–06:39 değişiklikleri). Bu paket o render yoluna dokunmadı.
- Fiyat paneli "Gram" başlığı altında `HasGrami` (has gram) gösteriliyor; başlık metni HUB sözlükte (P8). Değer biçimi düzeltildi, anlam/başlık P8'e not.
- tenants sayısı 12 → 14: artış `PAKETKESIF-P2-TEST Kuyumcu` ve `PAKETKESIF-P7-TEST Kuyumculuk` (başka şeritler). Bu paketin kiracısı 0.
- `/uyum` Saklama Süresi sekmesinin MÜDÜR'e görünmesi triyajda P6'ya ayrıldı — dokunulmadı.

## 11. Kurucu / mali müşavir
Soru yok (bu pakette mevzuat hükmü yok).
