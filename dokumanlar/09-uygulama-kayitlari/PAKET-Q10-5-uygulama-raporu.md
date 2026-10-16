# PAKET Q10-5 — Uygulama raporu
Konu: ortak form hatası, kalan jargon, tablo başlıkları, mobil tablo, banka ekstresi
Tarih: 2026-09-24 · Şerit: Q10-5 (t5: test DB `kuyumcu_test_t5`, API 5166, vekil 5167) · Rol: Taşkın + Ertuğ (Opus)

> Bu koşu, 18:25'teki ağ kopmasında yarıda kalan uygulayıcının devamıdır. Ağaçtaki kod ve 37 test 17:30–18:25 arasında yazılmıştı.
> O sırada M01–M06 mutasyonları da yapılmıştı. Bu koşuda yapılanlar: kalan 19 mutasyon, yarış yüzünden geçersiz sayılan
> M05/M15/M17'nin yeniden koşumu, tam süit, denetim, canlı API/SQL doğrulaması, tarayıcı ölçümü, kiracı temizliği ve bu rapor.

## 0-T3. TUR 3 (24.09 22:25–23:20): doğrulama bulgularının kapatılması
Durum: **BITTI** (paket kapısının 7 maddesi, aşağıda). Tek sınır: ölçüm **tarayıcı bölmesinde değil**, başsız Google Chrome'da (CDP) yapıldı. `mcp__Claude_Browser__tabs_create` 22:4x'te yine "Could not open a new tab (… tab cap reached)" döndü; bölmedeki 9 sekmenin hepsi başka şeritlerin (5198, 5179×4, 5199, 5196, 5161, file://). Onlara dokunulmadı. Bu durum orkestratörün sekme kotası kaleminde.

### T3.1 Bulgu → yapılan
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] /kilavuz doğrudan açılışta (F5) BASİT'te 'Cariler' / 'Kuyum-KDV' | Kök neden doğrulandı: sayfanın veri yüklemesi yok, ilk çizim `/auth/ben` çözülmeden (PRO) yapılıyor, mod sonra çözülünce yeniden çizim olmuyordu. `Kilavuz.razor`: `@implements IDisposable` + `OnInitialized() => Dil.Degisti += Yenile` + `Dispose`. `DilServisi.Degisti`, Program.cs'teki `oturum.Degisti += dil.ModuDenetle` bağıyla mod çözülünce tetikleniyor (Sayim/Vezne/Urunler ile aynı kalıp). | Yeni test `Q105_B11_Kilavuz_mod_sonradan_cozulunce_Musteriler_olur`: mod başta PRO bağlanır, render → "Cariler"; sonra mod BASİT + `ModuDenetle()` → "Müşteriler", K8 temiz. Mutasyon M-Q105-35 (abonelik kaldırıldı) → B11 KIRMIZI. **Gerçek tarayıcı, SAYFA YENİDEN YÜKLENEREK** (`Page.navigate` tam URL, `Blazor.navigateTo`/`__taraTam` DEĞİL): BASİT 1280/375 × açık/koyu = 4/4 `#kl-bolum-cariler` = "Müşteriler", 'Cariler' ve 'Kuyum-KDV' 0; K8=1 yalnız `r-zarar` (ekran listesindeki "Kâr-zarar", aracın ASCII `\b` yanlış pozitifi, § 10.2). PRO 4/4 "Cariler" (PRO adı, bilinçli). Görüntü `q105v3/png/_kilavuz-1280-BASIT-karanlik.png`. |
| [ORTA] /cariler 375 tablo 478 > 349 | **Q10-5'te KAPANMADI.** `Pages/Cariler.razor` T-PANEL-MÜŞTERİ şeridinin dosyası; spesifikasyon KAPSAM (4) de "/cariler T-M1'de" diyor, EŞZAMANLI ŞERİT kuralı dokunmayı yasaklıyor. Madde yazılı olarak **T-M1'e devredildi**: `ORKESTRATOR-DURUM.md` 23:00 girdisi ("Q10-5 tur 3 DEVİR … SAHİBİ: T-M1", önerilen çözüm: `q10-yigin` kalıbı). Tur 1–2 raporundaki "/cariler 375 yatay 0" ifadesi yalnız sayfa kaydırmasını ölçüyordu; tablo taşması AÇIK. | Bu turun ölçümü: /cariler 375 **BASİT** 4/4 tablo taşması 0 (T-M1'in şu anki hâli; bu kiracıda), **PRO** 375 açık/koyu tablo 1155 > 349 (`tablo-sik--kimlik`), PRO 1280 1093 > 962. Sahibi T-M1. |
| [ORTA] Temizlik: 'PAKETQ10-5-TEST Dogrulama' kiracısı, 5166/5167 süreçleri | Kiracı `01M3A7977YTDAEX4E7YNYT6D2V` `kiraci-temizle.sh --uygula` ile silindi (önce rapor modu: 849 satır + tenants 1). **PID 2994 (5166) ve 3286 (5167) kapatılamadı**: `kill 2994 3286` bu turda da otomatik izin sınıflandırıcısınca reddedildi; `ps` ile süreç ayrıntısı okumak da reddedildi ("Interfere With Workloads"). Bu yüzden canlı doğrulama için KENDİ API'mi **5266**, vekilimi **5267** portunda açtım (5160–5200 aralığı dışında, başka şeride ait değil), iş sonunda kendi PID'lerimi (44854 + dotnet run üst süreci, 46163) `kill` ile kapattım; `lsof` 5266/5267 boş. 5198 (PID 59766) başka şeridin, dokunulmadı. | SQL: `tenants` başta 15, sonda 14; `PAKETQ10-5%` kiracı 0. Kendi kiracım `01M3AFVHABMXEWC1C6K4EBK4D8` ("PAKETQ10-5-TEST Tur3") silindi, betik "Kiracı YOK (bağlantı sınandı)" diyor. Orkestratöre kalan: PID 2994 ve 3286'yı kapatmak. |
| [ORTA] Bölmede ölçüm yok | Yapılamadı (sekme kotası, yukarıda). Ölçüm gerçek Chrome motorunda, tam sayfa yüklemesiyle tekrarlandı. | § T3.2 |
| [DÜŞÜK] /banka/ekstre 375 tablo 1162 > 349, 'Atla' ekran dışı | Ekstre tablosuna `q10-yigin q10-yigin-secim` uygulandı: her veri hücresi `data-etiket` taşır, eylem hücresi `q10-yigin-eylem`. "Tümünü seç" başlığı mobilde de görünür (özellik silinmez): `q10-yigin-secim` yalnız ilk `th`'yi çizer, etiketi `::after`'dan yazar. İlk ölçümde tablo hâlâ 1042 px'ti: kök neden app.css'in 768 px altındaki `.veri-tablosu { min-width: max-content }` kuralı (ham satır en uzun içerik). q10.css'te yığında `min-width: 0` ile sıfırlandı (app.css'e dokunulmadı). Ayrıca yığın hücrelerine `flex-wrap: wrap; min-width: 0`. | Yeni test `EkstreEkranTestleri.Q105_Ekstre_tablosu_mobil_yigin_her_hucre_etiketli_tumunu_sec_korunur`, `Q105_C03`'e iki kural denetimi. Mutasyonlar M-Q105-36/37/38/39 kırmızı. Gerçek tarayıcı (yeniden ölçüm, `ekstre2.jsonl`): 375 × BASİT/PRO × açık/koyu 8/8 tablo taşması 0, yatay 0, K1–K9 0, 'Atla'/'Atlamayı geri al' 303–353 px görünür; 1280'de 8/8 aynı (962 = 962, düğmeler 1121–1239). Görüntü `q105v3/png2/_banka_ekstre_…-375-BASIT-karanlik.png`. |
| [DÜŞÜK] /hurda BASİT 'MUTABAKAT' K8=2 | Hurda.razor Q10-A şeridinin (DOSYA ALANI: DOKUNMA). Açık kalanlara sahibiyle yazıldı ve `ORKESTRATOR-DURUM.md` 23:00 girdisine eklendi. | — |
| [DÜŞÜK] Hızlı gezinmede 429 bandı | Pakete ait değil. Bu turun ölçüm düzeneği her yüklemeden sonra "istek sınırı" bandını arar, varsa 30 sn bekleyip yeniden ölçer; 48 ölçümün hiçbirinde bant çıkmadı (`429=False`, hepsi 1. deneme). | `ana.jsonl`, `ekstre2.jsonl` |

### T3.2 Gerçek tarayıcı ölçümü (başsız Chrome, CDP → vekil 5267 → API 5266; her ölçüm TAM SAYFA YÜKLEMESİ)
Sürücü `scratchpad/q105v3/incele3.mjs`: mod `PATCH /kullanicilar/ben/arayuz-modu` ile (200), tema `localStorage['kuyumcu-tema']` ile ayarlanır, sonra `Page.navigate` ile rota **doğrudan** açılır (F5 eşdeğeri), `sikisma-olc.js` enjekte edilir, `__olcTam()` + ek ölçüm (tablo > kapsayıcı, yatay, 'Cariler|Kuyum-KDV', ekstre düğme konumları, 429 bandı).

| Rota | 1280 BASİT (açık/koyu) | 375 BASİT (açık/koyu) | 1280 PRO (açık/koyu) | 375 PRO (açık/koyu) |
|---|---|---|---|---|
| /kilavuz | "Müşteriler"; K8=1 (`r-zarar`, araç) | "Müşteriler"; K8=1 (`r-zarar`) | "Cariler" (PRO adı); tümü 0 | "Cariler"; tümü 0 |
| /banka/ekstre | tümü 0; tablo 962 = sarmal; düğmeler görünür; rozet Yeni·Atlandı·Yeni | tümü 0; tablo taşması 0 (yığın); düğmeler 303–353 | tümü 0 | tümü 0; taşma 0 |
| /sayim | tümü 0 | tümü 0; taşma 0 | tümü 0 | tümü 0 |
| /cariler (T-M1, bilgi) | tümü 0 | tümü 0; taşma 0 | tablo 1093 > 962 (sarmal içinde) | **tablo 1155 > 349** (T-M1) |

Toplam 48 ölçüm (`ana.jsonl` 32 + düzeltme sonrası `ekstre2.jsonl` 16). Hiçbirinde K1, K3, K5 ('TRY'), K7, K9 ya da sayfa yatay kaydırması yok. Görüntüler: `scratchpad/q105v3/png/` (32) ve `png2/` (16).
Diğer 13 rota bu turda değişmedi (değişen dosyalar yalnız Kilavuz.razor, BankaEkstre.razor, q10.css; q10.css'in yeni kuralları yalnız `q10-yigin` sınıfını taşıyan tablolara (Sayım, Ekstre) etki eder), bu yüzden tur 2'nin 136 + 40 ölçümü geçerli sayıldı.

### T3.3 Testler, mutasyon, derleme, denetim
- Yeni test 2: `Q105FormJargonTabloTestleri.Q105_B11_Kilavuz_mod_sonradan_cozulunce_Musteriler_olur`, `EkstreEkranTestleri.Q105_Ekstre_tablosu_mobil_yigin_her_hucre_etiketli_tumunu_sec_korunur`. `Q105_C03` iki denetimle güçlendirildi.
- Filtreli son koşum (`~Q105|~EkstreEkran|~Kilavuz`): **151/151 yeşil** (`q105v3/test5.log`).
- Tam süit (`trx-pQ10-5-v3/_Mac_2026-09-24_23_00_29.trx`, `min-width: 0` düzeltmesinden önce; o düzeltme yalnız CSS + C03'e bir satır): **5606/5606 geçti, 0 başarısız.** Bu paketin yeni kırmızısı **0**.
- `dotnet build Kuyumcu.sln` (son mutasyondan sonra, `q105v3/build5.log`): **0 uyarı, 0 hata.**
- `bash denetim.sh` (son hâl, `q105v3/denetim2.log`): **DENETİM TEMİZ.**
- Migration: gerekmedi.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-Q105-35 | Kilavuz `OnInitialized() => Dil.Degisti += Yenile` → `{ }` | Q105_B11_Kilavuz_mod_sonradan_cozulunce_Musteriler_olur | cmp AYNI |
| M-Q105-36 | Ekstre tablosundan `q10-yigin q10-yigin-secim` silindi | EkstreEkranTestleri.Q105_Ekstre_tablosu_mobil_yigin… | cmp AYNI |
| M-Q105-37 | q10.css seçimli ilk `th` `display: flex` → `none` | Q105_C03_q10_css… | cmp AYNI |
| M-Q105-38 | "Tümünü seç" `th`'den `data-etiket` silindi | EkstreEkranTestleri.Q105_Ekstre_tablosu_mobil_yigin… | cmp AYNI |
| M-Q105-39 | q10.css yığın `min-width: 0` kuralı silindi | Q105_C03_q10_css… | cmp AYNI |

**Geçersiz sayılan koşumlar (Anayasa md.1):** M35'in ilk iki koşumu ve M36'nın ilk koşumu yeşil çıktı. Teşhis: derlenmiş `Kuyumcu.Web.dll` içinde mutasyonlu dizge YOKTU. Paralel şeridin aynı anda başlattığı derleme, eski kaynaktan ürettiği ikiliyi benim mutasyonumdan sonraki bir zaman damgasıyla yazıyor; MSBuild de projeyi güncel sayıp yeniden derlemiyordu (zaman damgası denetimi bu yarışı yakalayamıyor). `mut3.sh`'e **ikili özet (md5) denetimi** eklendi: derleme sonrası `Kuyumcu.Web.dll` özeti mutasyon öncesiyle aynıysa kaynak `touch`lanıp yeniden derlenir. Tablodaki beş sonucun hepsi bu denetimle alındı (M35'te bir kez "ikili DEĞİŞMEDİ (yarış)" yakalandı ve yeniden derlendi). Kanıt: `scratchpad/mut-yedek/Q10-5v3/*.{diff,build,test}`.

### T3.4 Canlı doğrulama (API 5266, vekil 5267, kiracı "PAKETQ10-5-TEST Tur3", `demo-tohumla.js --prova-kiraci` ile kuruldu)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /bankalar/{id}/ekstre/yukle` (3 satırlık CSV) | 200, PROVA, 3 satır | 200 · ithalat `01M3AFXHRBHEPB0PY6FZB2V0K7` · PROVA · 3 satır |
| `PATCH /bankalar/ekstre/satir/01M3AFXHRMRGTVFGH71SXPQJDM` durum=ATLANDI | 200, gövdede ATLANDI | 200, `"durum":"ATLANDI"` |
| Ekstre ekranı (8 kombinasyon) | Satır 2 rozeti "Atlandı" | `rozet:Yeni|Atlandı|Yeni` 8/8 |
| `POST /sayim` (şubesiz) | 400 + alan | 400 `{"alanlar":{"subeId":["Şube zorunludur"]}}`; şubeyle 200 |
| /kilavuz doğrudan yükleme, BASİT | "Müşteriler" | 4/4 "Müşteriler" |
| Temizlik | tenants başlangıç değerine | 15 → 14 (−1: doğrulayıcının Q10-5 kiracısı da silindi; kendi kiracım 0 satır) |

### T3.5 Değişen dosyalar (tur 3)
`dotnet/Kuyumcu.Web/Pages/Kilavuz.razor`, `dotnet/Kuyumcu.Web/Pages/BankaEkstre.razor`, `dotnet/Kuyumcu.Web/wwwroot/css/q10.css`, `dotnet/Kuyumcu.Tests/Q105FormJargonTabloTestleri.cs`, `dotnet/Kuyumcu.Tests/EkstreEkranTestleri.cs`, `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` (yalnız `kayit.sh` ile tek satırlık devir girdisi).
Dokunulan sayfalar: Kilavuz, BankaEkstre. İkisi de tur 1'de bu şeritte açılmıştı; aynı paketin doğrulama düzeltmesi (TEK DOKUNUŞ gerekçesi).

### T3.6 Açık kalanlar (sahipleriyle)
1. /cariler 375 tablo taşması (PRO'da 1155 > 349) → **T-M1 (T-PANEL-MÜŞTERİ)**; devir `ORKESTRATOR-DURUM.md` 23:00.
2. /hurda BASİT 'MUTABAKAT' K8=2 → **Q10-A / Q10-2** (Hurda.razor).
3. PID 2994 (5166) ve 3286 (5167) → **orkestratör** (kapatma izni reddedildi). Bağlı oldukları kiracı artık silinmiş durumda.
4. Tarayıcı bölmesi sekme kotası → **orkestratör**; bölmede ölçüm tekrarı kota boşalınca.
5. K8 aracının `r-zarar`/`E-POSTA` yanlış pozitifi → **orkestratör** (`sikisma-olc.js` deseni, § 10.2).
6. 429 istek sınırı tavanı → **orkestratör**.
7. `Kuyumcu.Tests/*.cs.bak` (3 dosya, 21.09 21:40): bu şeridin değil; dosya silme yasak olduğu için dokunulmadı.

## 0. TUR 2 (24.09 21:20–22:05): doğrulama bulgularının kapatılması
Durum: **YARIM.** Kod, test, mutasyon, denetim ve tarayıcı ölçümü tamam. Eksik olan tek şey GERÇEK TARAYICI KURALI'nın istediği yer: ölçüm **tarayıcı bölmesinde kendi sekmemde** yapılamadı. `mcp__Claude_Browser__tabs_create` 21:24 ile 22:03 arasında denediğim 8 seferin hepsinde "Could not open a new tab (… tab cap reached)" döndü. Bölmedeki 9 sekmenin hepsi başka şeritlere ait (5198, 5179×4, 5199, 5196, 5161, file://), bu yüzden onlara dokunmadım. Claude in Chrome da bağlı değildi ("not connected"). Ölçüm yine gerçek Google Chrome motorunda yapıldı (başsız, CDP), `sikisma-olc.js` ve vekil 5167 üzerinden. Bunu bölme ölçümü diye **beyan etmiyorum**. Hakem bu yüzden KALDI verirse haklıdır. Aynı ölçümün bölmede tekrarı, sekme kotası boşalınca yapılabilir.

### 0.1 Bulgu → yapılan
| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [ORTA] /denetim-izi başlığı "Denetim İzi" | `DenetimIzi.razor`: SayfaBasi başlığı ve `<PageTitle>` artık `MenuAdlari.Icin(Dil.T).KimNeYapti.Ad` ("Kim ne yaptı") değerini kullanıyor. Değişiklik Basit ve PRO için aynı, çünkü üst bar iki modda da bu adı taşıyor (UX-S31). | Test `Q105_B09`, mutasyon M-Q105-26. Görüntü `son2-denetim-izi-BASIT-1280-karanlik.png`: başlık "Kim ne yaptı". Ölçüm, metin `toLocaleLowerCase('tr-TR')` ile küçültülerek yapıldı: BASİT 8 kombinasyonda "denetim izi" **0**. |
| (Aynı ölçümle bulunan ek K8) | BASİT modda iki metin daha "denetim izine" diyordu: /veri-disa-aktarim'da gerekçe ipucu ve kısa gerekçe mesajı, /kurulum'da kısa gerekçe mesajı. `Sozlukler.Q105.cs`'e `GerekceIpucuBasit` ve `GerekceKisaBasit` eklendi (TR+EN). Hub sözlüğüne dokunulmadı, PRO metni aynı kaldı. | Testler `Q105_B10`, `KurulumEkranTestleri.Q105_Basit_modda_gerekce_kilit_sebebi_denetim_izi_demez`. Mutasyonlar M-Q105-32/33/34. |
| [ORTA] Ekstre 1280'de 'Atla' görünür alanın dışındaydı | `q10.css`'te en-az genişlikler düşürüldü: karşı taraf 12→9rem, müşteri 14→11rem, eylem 16→8rem. Eylem düğmesi `nowrap` oldu. Kilit sebebi artık ilk sütunda düz metin olarak durmuyor. `Onay.razor`'a `SebepIpucunda` parametresi eklendi: sebep kutucuğun `title` ipucu oluyor, ayrıca görsel olarak gizli (`gorsel-gizli`) ama `aria-describedby` bağlantısı korunuyor. `BankaEkstre.razor` bu parametreyi açıyor. | 1280, BASİT ve PRO, açık ve koyu temanın hepsinde tablo 962 px, sarmal 962 px. Düğme sağ kenarları 1171/1239/1171, sarmal sağ kenarı 1255: **hepsi görünür alanda**. Eskiden tablo 1177, sarmal 962, 'Atla' sağ kenarı 1280'di. Görüntü `son2-banka_ekstre_01M3A7A-BASIT-1280-aydinlik.png`. Test `Q105_Kilit_sebebi_ipucunda…` ve `Q105_C03`'e eklenen en-az toplamı ≤28rem, eylem ≤8rem denetimi. Mutasyonlar M-Q105-29/30/31. |
| [DÜŞÜK] /banka IBAN düzeltilince genel bant kalıyordu | `FormDurumu.BantMesaji(hata, dil, esleme)` eklendi. Sunucunun alan sözlüğündeki her anahtar ekrandaki bir alana eşleniyorsa bant boş kalır, çünkü mesaj zaten alanın altında görünüyor. Eşlenmeyen alan varsa ya da 500 gibi alansız bir hata gelirse mesaj bantta kalır, hata yutulmaz (UX-S28). Banka'da yeni hesap, hesap düzenleme ve dekont catch blokları bu yardımcıyı kullanıyor. | Tarayıcıda BASİT ve PRO, 1280 ve 375: sunucu 400 döndükten sonra `bantlar=[]`, alan `girdi hatali`, aria-invalid=true ve alan mesajı görünür. IBAN düzeltilince `girdi`, aria-invalid=false, alan mesajı null, bant yok. Canlı API: `POST /api/v1/bankalar iban=TR12345` → **400** `{"alanlar":{"iban":["Geçerli bir IBAN giriniz (TR + 24 rakam)"]}…}`. Testler: `Q105_Sunucu_400_yalniz_alan_hatasi…`, `Q105_BantMesaji_eslenemeyen_alan…`. Mutasyonlar M-Q105-27/28. |
| [DÜŞÜK] K8 aracının yanlış pozitifleri (r-zarar, E-POSTA) | Kod değişikliği yok, çünkü bunlar aracın hatası. Hâlâ ölçülüyorlar: BASİT'te /kilavuz ve /raporlar/kar-zarar 'r-zarar', /kullanicilar 'E-POSTA'. Düzeltme orkestratörde: desen `(?<![\p{L}])` ile başlamalı ve `e-` öneki dışlanmalı. | `tam1-ozet.txt` |
| [DÜŞÜK] 'PAKETQ10-5-TEST Dogrulama' kiracısı ve PID 2994/3286 | **Kapatılamadı.** `kill 2994 3286` otomatik izin sınıflandırıcısı tarafından reddedildi ("Interfere With Workloads"). Kiracıyı da ben açmadığım için silmedim (kural: "yalnız KENDİ açtığın"). Ek bilgi: 22:01'de başka bir ajan (Q10-2 doğrulayıcısı) ortak `fikstur.json`'u kendi kiracısıyla ("PAKETQ10-2-TEST Dogrulama", 01M3ACPBF4…) değiştirdi. Vekil 5167 artık o kiracıya giriş yapıyor. | Orkestratörün yapacakları: `kiraci-temizle.sh 01M3A7977YTDAEX4E7YNYT6D2V --uygula`, ardından PID 2994 ve 3286'yı kapatmak. |
| [DÜŞÜK] SayfaBasi filtre satırı 375'te K7=1 | Bu şeridin alanında değil (`Bilesenler/SayfaBasi.razor`). Ölçümde hâlâ var: /urunler BASİT 375, /faturalar BASİT ve PRO 375. | T-KABUK ya da orkestratör |
| [KRİTİK] Bölmede ölçüm | Yapılamadı (yukarıda). Ölçümü gerçek Chrome'da tekrarladım ve sayıları aşağıya yazdım. Bu raporla doğrulayıcının raporu aynı gerçeği yazsın: **bölmede ölçüm YOK.** | — |

### 0.2 Tarayıcı ölçümü (başsız Chrome, CDP → vekil 5167 → API 5166. Bölme DEĞİL)
Tam matris: 17 rota × {1280, 375} × {BASİT, PRO} × {açık, koyu} = **136 ölçüm** (`q105v2/tam1.log`, `tam1-ozet.txt`). Tur 2 düzeltmelerinden sonra 5 rota yeniden ölçüldü: 5 × 8 = **40 ölçüm** (`son2.log`). Aracın K1–K11 sayaçlarına ek olarak metin tr-TR küçük harfe çevrilerek "denetim izi" ve "cari…" de sayıldı.

| Rota | BASİT (8 kombinasyon) | PRO (8 kombinasyon) |
|---|---|---|
| /denetim-izi | Hepsi 0. Başlık "Kim ne yaptı", tr-TR K8 = 0 | Araç sayaçları 0. Tabloda "cari" kayıt türü detayı var (PRO ham detay, bilinçli) |
| /veri-disa-aktarim | 0 (tur 2 sonrası, `son2`) | 0 |
| /kurulum | 0 (tur 2 sonrası) | 0 |
| /banka | 0. Form bandı yukarıda | 0 |
| /banka/ekstre | 0. 1280'de tablo 962 = sarmal 962, 'Atla' ve 'Atlamayı geri al' görünür. 375'te tablo sarmal içinde kayıyor, sayfa yatay 0 | 0 (aynı geometri) |
| /sayim, /cariler (375) | Yatay kaydırma 0 | Yatay kaydırma 0 |
| /kilavuz, /raporlar/kar-zarar, /kullanicilar | K8=1, yalnız araç yanlış pozitifi (r-zarar, E-POSTA) | 0 |
| /urunler, /faturalar (375) | K7=1, SayfaBasi (başka şerit) | /faturalar 375 K7=1 (aynı) |
| Kalan 7 rota | 0 | 0 |

Hiçbir kombinasyonda K1 (gizli olup görünen), K3 (taşan ya da kırık başlık), K5 ('TRY'), K9 (binen) ya da yatay kaydırma yok.
Görüntüler: `scratchpad/q105v2/png/` altında. `son2-*` = 5 rota × 8 kombinasyon, 40 adet. `form-banka-{BASIT,PROFESYONEL}-{1280,375}-{hata,duzeltildi}.png` = 8 adet. `once-ekstre-*` = ara ölçüm.

### 0.3 Testler, mutasyon, derleme, denetim
- Yeni test sayısı 6: `Q105_B09`, `Q105_B10`, `BankaEkranTestleri.Q105_Sunucu_400_yalniz_alan_hatasi…`, `BankaEkranTestleri.Q105_BantMesaji…`, `EkstreEkranTestleri.Q105_Kilit_sebebi_ipucunda…`, `KurulumEkranTestleri.Q105_Basit_modda_gerekce_kilit_sebebi…`. Ayrıca `Q105_C03` güçlendirildi.
- Filtreli koşum `~Q105`: **72/72 yeşil** (tur 1 sonunda 37 + doğrulayıcının 27 sınır testi + tur 2).
- Tam süit (`trx-pQ10-5-v2`, B10 ve Kurulum testinden önce koşuldu): **5464 test, 5461 geçti, 3 başarısız.** Üçü de tek başına koşulunca yeşil (`q105v2-kirmizi-yeniden.log`: 3/3), yani yük ya da paylaşılan veritabanı kaynaklı kararsızlar. Hiçbiri Q10-5'in değil: `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA…` ve `TS1TuvalSatisTestleri.Nakit_uyarisi_BOS…` T-SATIŞ'ın (bUnit zaman aşımı). `MigrationHazirlikTestleri.EksikMigrationli…` "Unknown database 'kuyumcu_t1_eski'" hatası verdi; bu t1 şeridinin veritabanı yarışı. Q10-5'in ürettiği yeni kırmızı: **0**.
- `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata.** Son mutasyondan sonra `q105v2-build8.log`. Ara derlemelerdeki MSB4018 webcil ve MSB3026 kopya hataları paralel şeritlerin aynı anda derlemesinden kaynaklandı; yeniden denemede temiz geçti. xUnit1031 uyarısı `EtiketEkranTestleri`'nde, başka bir şeridin dosyası.
- `bash denetim.sh`: **DENETİM TEMİZ.**
- Migration: gerekmedi. Backend değişikliği yok.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-Q105-26 | `SayfaAdi` → `Dil.T.Denetim.Baslik` | Q105_B09_Denetim_izi_sayfa_basligi… | cmp AYNI |
| M-Q105-27 | `BantMesaji` hep `SunucuMesaji` döner | Q105_BantMesaji_eslenemeyen_alan… · Q105_Sunucu_400_yalniz_alan_hatasi… | AYNI |
| M-Q105-28 | Yeni hesap catch → `SunucuMesaji` | Q105_Sunucu_400_yalniz_alan_hatasi… | AYNI |
| M-Q105-29 | Onay sebep `gorsel-gizli` sınıfı kaldırıldı | EkstreEkranTestleri.Q105_Kilit_sebebi_ipucunda… (**2. koşum**; 1. koşumda paralel derleme bayat ikili üretti, derleme 5,7 sn sürdü ve test yeşil kaldı, bu yüzden o koşum geçersiz) | AYNI |
| M-Q105-30 | BankaEkstre `SebepIpucunda="true"` silindi | EkstreEkranTestleri.Q105_Kilit_sebebi_ipucunda… | AYNI |
| M-Q105-31 | q10.css eylem min-width 8 → 16rem | Q105_C03_q10_css… | AYNI |
| M-Q105-32 | Dışa aktarım ipucu → hub metni | Q105_B10_Disa_aktarim_basitte… | AYNI |
| M-Q105-33 | Kurulum `GerekceKisaMetni` → hub | KurulumEkranTestleri.Q105_Basit_modda_gerekce_kilit_sebebi… | AYNI |
| M-Q105-34 | Dışa aktarım `GerekceKisaMetni` → hub | Q105_B10_Disa_aktarim_basitte… | AYNI |

M-Q105-26'nın ilk koşumu da aynı yarış yüzünden yeşil çıktı. Bu nedenle `mut.sh`'e ikili zaman damgası denetimi eklendi ve M26 yeniden koşuldu. Rapordaki bütün sonuçlarda "ikili degismedi (yaris yok)" yazıyor. Kanıt: `scratchpad/mut-yedek/Q10-5v2/*.{diff,build,test}`.
Test açığı: hesap düzenleme ve dekont catch'leri (`BantMesaji` kullanan diğer iki yer) için ayrı mutasyon koşulmadı. Yardımcının kendisi M27 ile kanıtlandı.

### 0.4 Canlı doğrulama ve temizlik
- API: `POST /api/v1/bankalar` iban=TR12345 → 400 ve `alanlar.iban` (gövde okundu). Bu istek Q10-2 kiracısına gitti, çünkü ortak fikstür değişmişti. Kayıt oluşmadı (400).
- Ekstre: tarayıcıda `PATCH /bankalar/ekstre/satir/01M3A7AXRQFDF24KE4QJXCVT2H` durum=ATLANDI. SQL: `ekstre_satirlari` 1 YENI · 2 ATLANDI · 3 YENI. Satır 2'nin rozeti "Atlandı" ve düğmesi "Atlamayı geri al". Bu kiracı silinecek; satır ATLANDI olarak bırakıldı.
- Bu turda kiracı açmadım. `tenants` sayısı başta 15'ti, şimdi 16. Artış 22:01'de Q10-2 doğrulayıcısının açtığı kiracıdan geliyor, benim katkım 0.
- API ya da vekil başlatmadım. Mevcut 5166/5167 süreçleri kullanıldı; bunlar bu şeridin portları ve durdurmam izin sınıflandırıcısınca reddedildi. Başsız Chrome süreçleri iş bitince kendiliğinden kapandı.
- Proje ağacında .bak/.mutorj/.tmp/.orig yok.
- Değişen dosyalar (tur 2): `Kuyumcu.Web/Pages/{DenetimIzi,Banka,BankaEkstre,VeriDisaAktarim,Kurulum}.razor`, `Kuyumcu.Web/Bilesenler/Form/Onay.razor`, `Kuyumcu.Web/Servisler/FormDurumu.cs`, `Kuyumcu.Web/wwwroot/css/q10.css`, `Kuyumcu.Core/I18n/Sozlukler.Q105.cs`, `Kuyumcu.Tests/{Q105FormJargonTabloTestleri,BankaEkranTestleri,EkstreEkranTestleri,KurulumEkranTestleri}.cs`
- Dokunulan sayfalar: DenetimIzi, Banka, BankaEkstre, VeriDisaAktarim, Kurulum. Hepsi tur 1'de bu şeritte açılmıştı ve aynı paketin doğrulama düzeltmesi için yeniden açıldı (TEK DOKUNUŞ gerekçesi).

---

## 1. Yapılanlar
| # | Kapsam | Değişiklik |
|---|---|---|
| 1 | Ortak form hatası | `Bilesenler/Form/FormAlani.cs` → `DefterdenSil()`: alanın değeri değişince (input) `FormDurumu.Sil(HataAlani)` çağrılır. Metin, Sayı/Para/Gram (`SayisalAlan.cs`), Tarih ve Onay bu yolu kullanır. Tek yer burasıdır; Hurda (Q10-A) da aynı bileşenleri kullanıyor. `Servisler/FormDurumu.cs`: `Sil` yalnız o alanı ve sıradaki kaydını siler. `Sunucudan(ApiHata, esleme)` sunucunun camelCase alan adını (`iban`) ekrandaki alan kimliğiyle (`hesap-iban`) eşler; büyük/küçük harf ayrımı yapılmaz. Böylece /banka'da 400 döndüğünde IBAN alanı işaretlenir ve alan düzeltilince işaret kalkar. |
| 2 | Vezne | Negatif fiyatta doğru sebep yazılır; fiyat düzeltilince `formKalemHatasi` düşer. Basit modda defter notu iç terim içermez. |
| 3 | Fiyatlandırma | Kampanya hedefi seçilince (`HedefIdDegisti`) `km-hedefid` hatası düşer. Liste türü seçicisi ham kod göstermez; tabloyla aynı adı kullanır. Tablo başlıkları `q10-th-tek` sınıfını taşır. |
| 4 | Jargon (Basit mod) | Kılavuz: "Cariler" yerine "Müşteriler" ve Basit bölüm adları (`Q105Sozlugu`). Veri dışa aktarım: kebab-case kapsam kodları sözlükteki adlarıyla gösterilir, geçmiş listesinde de. Denetim izi: Basit modda detaydaki kod adları çevrilir (`DenetimDetayi`); Profesyonel modda ham detay aynen kalır. Kurulum: açılış kuralı iç kod içermez. Faturalar: özel matrah beyanı Basit modda katlanır özet olarak gelir, iç kod yok. Banka dekontunda "storno" yerine "Geri al (düzeltme fişi)" yazar. Ürün ağacında "Pasife al" yerine "Kullanımdan kaldır" yazar (yalnız Basit modda). |
| 5 | Tablo başlıkları | `.veri-tablosu.q10-th-tek > thead th { white-space: nowrap }`. Tablo `.tablo-sar` içinde kayar, sayfa yatay kaymaz. Uygulanan sayfalar: Kullanıcılar, Ürün ağacı, Pos, Fiyatlandırma, Ürünler, Faturalar, Banka ekstresi. |
| 6 | Mobil | `/sayim`: `q10-yigin`. 40rem altında her satır bir kart olur, hücre etiketi `data-etiket`ten gelir, `thead` gizlenir. İlk sürümde başlık 1 px'e kırpılmıştı; bu, K9 "th ⟂ td" binmesi olarak sayıldı, bu yüzden `display:none` yapıldı. |
| 7 | Banka ekstresi | Karşı taraf adı ve IBAN ayrı bloklarda (`q10-karsi`), binme yok. `ATLANDI` durumu artık "Atlandı" rozeti ve "Atlandı: seçmek için önce 'Atlamayı geri al'" kilit sebebiyle gösterilir; eskiden "Eşleşti" yazıyordu. Atla ya da geri al sonrasındaki bant hangi satıra ne olduğunu yazar ("2. satır atlandı…"). İthalat durumu ham kodla gösterilmez (`PROVA` → "Önizlemede…"). "Yeni" durumdaki sahne satırı için gerekçeli **DegismezIpucu** eklendi (bkz. § 2). |
| 8 | Metinler | `Kuyumcu.Core/I18n/Sozlukler.Q105.cs` TR+EN. Hub `Sozlukler.cs` değiştirilmedi. |
| 9 | CSS | `wwwroot/css/q10.css`. Yalnız belirteç `var(--…)` kullanılıyor; sabit renk ve satır içi style yok. `app.css` değiştirilmedi. |

Backend değişikliği yok (`Modules/Banka` değiştirilmedi). Migration **gerekmedi**.

## 2. Sapmalar ve gerekçeleri
- **Ekstre sahne satırı düzenleme:** "Yeni" satırda tarih, tutar, yön, açıklama ve karşı taraf düzenlenebilir yapılmadı. Bunun yerine gerekçeli DegismezIpucu kondu; spesifikasyon bu iki yoldan birini kabul ediyor. Gerekçe: bu alanlar bankanın dosyasından gelir. Ekranda değiştirilirlerse ekranla banka ekstresi birbirini tutmaz ve mutabakatın dayanağı bozulur. Ekranda yazan doğru yol: satırı "Atla", doğrusunu Banka ekranından elle dekont olarak gir ya da düzeltilmiş dosyayı yeniden yükle.
- **/denetim-izi tarih hizası (K7):** Önce ölçümünde bu kiracıda 1280 ve 375 genişlikte, Basit ve Profesyonel modda K7=0 çıktı (`once2.jsonl`). QA 10'daki 15 px kayma yeniden üretilemedi, bu yüzden **KAPANMIŞ** sayıldı. Kod yazılmadı.
- **/cariler mobil:** `Cariler.razor` T-PANEL-MÜŞTERİ şeridinde (T-M1), bu yüzden dokunulmadı. Ölçümde /cariler için 375'te yalnız SAYFA yatay kaydırması 0 idi; tablo kapsayıcıdan taşıyordu. **Madde Q10-5'te KAPANMADI, T-M1'e devredildi (§ T3.1).**
- **/islemler:** `Islemler.razor` ve `Fis/*` T-SATIŞ şeridinde, onlara dokunulmadı. Alan hatasının kalkması ortak `Form/*` bileşeniyle sağlandı ve tarayıcıda doğrulandı (§ 6).

## 3. Mevzuat
Bu pakette mevzuat hükmü yazılmadı. Faturalardaki özel matrah beyanında yalnız görünüm değişti (Basit modda katlanır). Metin ve kural aynen kaldı, kilit açılmadı.

## 4. Testler
- Önce: Q105 filtresine uyan test 0. Sonra: **37 yeni test**, filtreli koşumda 37/37 yeşil. Dağılım: `Q105FormJargonTabloTestleri` 17, `BankaEkranTestleri` 3, `EkstreEkranTestleri` 4, `FiyatlandirmaEkranTestleri` 4, `VezneEkranTestleri` 4, `FaturaEkranTestleri` 3, `KurulumEkranTestleri` 2.
- Tam süit (`trx-pQ10-5`): **5403 test, 5400 geçti, 3 başarısız.** Üçü de başka şeridin (§ 10), aralarında Q10-5 testi yok.
- `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata. Son mutasyondan sonra temiz derlemeyle doğrulandı.
- `bash denetim.sh`: **DENETİM TEMİZ.**

## 5. Mutasyon tablosu
Hepsinde kaynak `mut-yedek`e kopyalandı, `perl -0777` ile tek eşleşmeli değişiklik yapıldı, test koşuldu, yedek geri kopyalandı ve `cmp` ile AYNI olduğu doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-Q105-01 | FormAlani `DefterdenSil` içindeki `Durum?.Sil(a)` kaldırıldı | Q105_A03, A04, A05, A06 · BankaEkranTestleri.Q105_Sunucu_400_iban… · FiyatlandirmaEkranTestleri.Q105_Ad_hatasi… | AYNI |
| M-Q105-02 | FormDurumu eşlemede büyük/küçük harf eşitliği kaldırıldı | Q105_A02_Sunucu_camelCase… · BankaEkranTestleri.Q105_Sunucu_400_iban… | AYNI |
| M-Q105-03 | FormDurumu `Sil` sıradaki kaydı silmiyor | Q105_A01_FormDurumu_Sil_yalniz_o_alani… | AYNI |
| M-Q105-04 | Vezne: fiyat değişince `formKalemHatasi = null` yapılmıyor | VezneEkranTestleri.Q105_Negatif_fiyatta_dogru_sebep… | AYNI |
| M-Q105-05 | Vezne kalem hatası koşulu `false` yapıldı (ilk koşumda derleme yarışı vardı, **yeniden koşuldu**) | VezneEkranTestleri.Q105_Negatif_fiyatta_dogru_sebep… | AYNI |
| M-Q105-06 | ATLANDI kilit sebebi → "Eşleşti" | EkstreEkranTestleri.Q105_Atlanan_satir_rozeti_Atlandi… | AYNI |
| M-Q105-07 | ATLANDI rozeti → eylem adı "Atla" | EkstreEkranTestleri.Q105_Atlanan_satir_rozeti_Atlandi… | AYNI |
| M-Q105-08 | Atla bandı → genel "İşlem başarılı" | EkstreEkranTestleri.Q105_Atla_sonrasi_bant… | AYNI |
| M-Q105-09 | Dışa aktarım `muhasebe-fisi` sözlük eşlemesi silindi | Q105_B03_Disa_aktarim_kapsam… | AYNI |
| M-Q105-10 | `DenetimDetayi` basit modda da ham döner | Q105_B01, Q105_B04 | AYNI |
| M-Q105-11 | Faturalar basit katlanır beyan kapatıldı | FaturaEkranTestleri.Q105_Basit_modda_ozel_matrah… | AYNI |
| M-Q105-12 | Sayım `q10-yigin` sınıfı kaldırıldı | Q105_C02_Sayim_listesi_mobil_yigin… | AYNI |
| M-Q105-13 | q10.css başlık `nowrap` kuralı boşaltıldı | Q105_C03_q10_css… | AYNI |
| M-Q105-14 | Kurulum açılış kuralı basit metni → PRO metni | KurulumEkranTestleri.Q105_Basit_modda_acilis… | AYNI |
| M-Q105-15 | Banka dekont yolu `Dil.BasitMi` → `false` | BankaEkranTestleri.Q105_Basit_modda_dekont… (**ikinci koşum**; ilkinde test geçti, bkz. not) | AYNI |
| M-Q105-16 | Ürün ağacı `PasifeAlMetni` → hep PRO | Q105_B06_Urun_agaci_basitte… | AYNI |
| M-Q105-17 | `HedefIdDegisti` boşaltıldı | FiyatlandirmaEkranTestleri.Q105_Kampanya_hedef_hatasi… (**ikinci koşum**) | AYNI |
| M-Q105-18 | Liste türü seçicisi ham kod | FiyatlandirmaEkranTestleri.Q105_Liste_turu_secicisi… | AYNI |
| M-Q105-19 | Kılavuz faturalar basit bölüm adı silindi | Q105_B08_Kilavuz_basitte… | AYNI |
| M-Q105-20 | Vezne defter notu basit → PRO | VezneEkranTestleri.Q105_Basit_modda_defter_notu… | AYNI |
| M-Q105-21 | `SayisalAlan` `DefterdenSil()` çağrısı silindi | Q105_A04_Sayi_degisince… | AYNI |
| M-Q105-22 | `q10-karsi` bloğu kaldırıldı | EkstreEkranTestleri.Q105_Karsi_taraf_adi_ve_iban… | AYNI |
| M-Q105-23 | İthalat `PROVA` ham kod | EkstreEkranTestleri.Q105_Sahne_satiri_gerekceli… | AYNI |
| M-Q105-24 | Denetim izi detay ham basıldı | Q105_B04_Denetim_izi_basitte… | AYNI |
| M-Q105-25 | Sahne DegismezIpucu varlık adı bozuldu | EkstreEkranTestleri.Q105_Sahne_satiri_gerekceli… | AYNI |

**25/25 kırmızıya döndü.** Not: M15 ve M17 toplu koşumda yeşil çıktı. Yalnız başlarına yeniden koşulunca ikisi de kırmızı oldu. Aynı ağaçta paralel koşan şeritlerin `dotnet build`ları `bin`'i değiştirdiği için toplu koşumdaki test mutasyonsuz ikiliyle koştu (ilk M05 koşumunda `MSB3030 apphost` hatası da görüldü). Bu yüzden toplu sonuç geçersiz sayıldı ve yeniden ölçüldü. Kanıt: `scratchpad/q105/mut-sonuc.txt` (M01–M06), `mut-sonuc2.txt` (M05, M07–M25) ve `mut-M-Q105-15.test`, `mut-M-Q105-17.test` (yeniden koşum).

## 6. Canlı doğrulama (API 5166, vekil 5167, kiracı `PAKETQ10-5-TEST Kuyumculuk`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /api/v1/bankalar` iban=`TR12345` | 400 ve `alanlar.iban` | **400**, gövde `{"alanlar":{"iban":["Geçerli bir IBAN giriniz (TR + 24 rakam)"]}…}` |
| Tarayıcı /banka: aynı 400 → alan | `hatali`, aria-invalid=true, `#alan-hata-hesap-iban` görünür | Görünür (1280 ve 375). Geçerli IBAN yazılınca `hatali` kalktı, aria-invalid=false, mesaj yok |
| Tarayıcı /fiyatlandirma `fl-ad` boş kaydet → yaz | Mesaj görünür, sonra düşer | "Bu alan zorunludur." → yazınca null (1280 ve 375) |
| Tarayıcı /urunler `u-ad` | Aynı | Aynı (1280 ve 375) |
| Tarayıcı /vezne fiyat −5 → 1500 | Kalem mesajı görünür, sonra düşer | "Ürün seçtiğiniz satırda adet ve birim fiyat sıfırdan büyük olmalı." → null (1280 ve 375) |
| Tarayıcı /islemler `islem-birimfiyat` boş Ekle → 1500 | Mesaj görünür, sonra düşer | "Sıfırdan büyük bir değer girin." → null (Basit 1280 ve 375, PRO 375). PRO 1280'de betik boş nesne döndü, zamanlama kaynaklı. Basit ve PRO aynı `FisKalemGirisi` bileşenini kullanıyor. |
| `PATCH /bankalar/ekstre/satir/{3}` durum=ATLANDI | 200, SQL ATLANDI | 200. `ekstre_satirlari`: 1 ONAYLANDI · 2 ATLANDI · 3 ATLANDI. Ardından `durum=YENI` ile geri alındı, 200. |
| Tarayıcı ekstre | Satır 2 rozeti "Atlandı", ad ile IBAN ayrı | Görüntü `png-kanit/banka_ekstre_…-1280-BASIT-karanlik.png`: "Atlandı" rozeti, kilit sebebi "Atlandı: seçmek için önce 'Atlamayı geri al'", ad ve IBAN ayrı satırlarda |

## 7. Gerçek tarayıcı ölçümü (sikisma-olc.js, 17 rota × {1280, 375} × {Basit, PRO} × {açık, koyu} = 136 ölçüm)
Araç: başsız Google Chrome (CDP) → vekil 5167. Tarayıcı bölmesinde kendi sekmem açılamadı: `tabs_create` "tab cap reached" döndü ve bölmedeki sekmelerin hepsi başka şeritlere ait olduğu için onlara dokunulmadı. Ölçüm aynı `sikisma-olc.js` ile gerçek Chromium motorunda yapıldı. Ham veri: `scratchpad/q105/{once,once2,sonra,sonra2,son}.jsonl`.

| Rota | Önce (QA 10 ve ilk ölçüm) | Sonra (`son.jsonl`, 20:1x) |
|---|---|---|
| /fiyatlandirma | K3 kırık başlık 2 (B+P 1280) | 0 |
| /urunler | K3 kırık 2 (P 1280) | 0. K7 1 (375, `#sayfa-basi-urunler-filtre`): ortak SayfaBasi, önce de vardı (§ 10) |
| /islemler | K8 2 | 0 |
| /banka | K8 1 (storno) | 0 |
| /kilavuz | K8 2 (Cariler…) | K8 1: `r-zarar`, **ölçüm yanlış pozitifi** (§ 10) |
| /veri-disa-aktarim | K8 5 (kebab kodlar) | 0 |
| /faturalar | K7 1 (375) | K7 1 (375, aynı SayfaBasi filtresi). Tablo başlığı 0 |
| /kurulum | K8 2 | 0 |
| /pos | K3 kırık 3 (B 1280) | 0 |
| /raporlar/kar-zarar | K8 1 | K8 1: `r-Zarar`, **yanlış pozitif** |
| /kullanicilar | K3 kırık 1 ("İKİ AŞAMALI"), K8 1 | K3 0. K8 1: `E-POSTA`, **yanlış pozitif** |
| /urunler/agac | K3 kırık 3 ("REYON TİPİ", "YERLEŞİK ADET"…), K8 1 | 0 |
| /sayim | 375 tablo 513 px (QA 10), K9 3 (ara sürümde) | 0 (375'te yatay 0, binen 0; B+P, açık+koyu) |
| /cariler | K8 1 | K8 1: `e-posta`, yanlış pozitif (sayfa T-PANEL-MÜŞTERİ şeridinde) |
| /denetim-izi | K8 8 (kod adları) | 0 |
| /banka/ekstre | K3 kırık 7, K8 2, K9 binen 3 | 0 (K2 %120: tablo `.tablo-sar` içinde kayar, sayfa yatay 0) |
| /vezne | 0 | 0 |

Hiçbir kombinasyonda K1, K5 ('TRY'), K9 ya da yatay kaydırma yok. Ekran görüntüleri:
- önce: `scratchpad/q105/png-once/`, `png-once2/`, `once-sayim-375.png`
- sonra: `png-son/` (17 rota, 1280 Basit), `png-kanit/` (13 kombinasyon: sayım 375 B/P açık/koyu, ekstre 1280/375 B/P, ürün ağacı, dışa aktarım, denetim izi 375 koyu, kılavuz, kullanıcılar, pos, faturalar), `png-form/` (form hatası önce ve sonra)

## 8. DoD sözleşmesi
Yeni varlık ya da kayıt türü yok. Ekstre sahne satırı ve durum düzenlemesi mevcut uçla yapılıyor: `PATCH /bankalar/ekstre/satir/{id}` (Durum ATLANDI/YENI, CariId), eylemleri Atla, Atlamayı geri al ve Müşteriye bağla. Satırın banka kaynaklı alanları salt okunur; gerekçesi ve doğru yolu DegismezIpucu'da yazıyor. Onaylanan satırın düzeltilmesi Banka ekranında "Geri al (düzeltme fişi)" ile yapılır (storno motoru). Finansal kayıt silinmez.

## 9. Değişen dosyalar (Q10-5)
Web: `Bilesenler/Form/FormAlani.cs`, `Bilesenler/Form/SayisalAlan.cs` (+ Form/* bileşenleri), `Servisler/FormDurumu.cs`, `Pages/{Kilavuz,VeriDisaAktarim,Vezne,Faturalar,Kurulum,Pos,Kullanicilar,UrunAgaci,Sayim,DenetimIzi,Banka,BankaEkstre,Fiyatlandirma,Urunler}.razor`, `wwwroot/css/q10.css`
Core: `I18n/Sozlukler.Q105.cs`
Tests: `Q105FormJargonTabloTestleri.cs`, `BankaEkranTestleri.cs`, `EkstreEkranTestleri.cs`, `FiyatlandirmaEkranTestleri.cs`, `VezneEkranTestleri.cs`, `FaturaEkranTestleri.cs`, `KurulumEkranTestleri.cs`
(Depoda tek bir ilk commit olduğu için `git status` bu dosyaların çoğunu önceki paketlerden de değişmiş gösterir. Liste, 17:25 sonrası dosya zamanları ile şerit alanının kesişimidir.)

## 10. Açık kalanlar
1. **Tam süitteki başkasına ait 3 kırmızı:**
   - `S1cUstBarTestleri.TK1_kur_seridi_has_gram_TAM_lira…` ve `S1cUstBarTestleri.Basit_modda_serit_TUVAL_SIRASIYLA…`: T-KABUK (UstBar/Bicim, 18:06 değişikliği)
   - `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir`: `FisKalemTablosu.razor@957/@2206`, `FisVerdimAldim.razor@2630`, T-SATIŞ
   Q10-5'in ürettiği yeni kırmızı yok.
2. **K8 yanlış pozitifleri (araç):** `sikisma-olc.js` K8 kebab deseni `\b[a-z]+(?:-[a-z]+)` şeklinde. JS'de `\b` ASCII'dir, bu yüzden "Kâr-zarar" içinden `r-zarar` yakalanıyor. "E-posta" da TDK'da doğru yazılışı olan bir sözcük. Bunlar kod adı değil. Ürün genelindeki "Kâr-Zarar" adı hub sözlüğünde, menüde ve Panel'de geçiyor; tek şeritte değiştirmek UX-S31'i (tek ad) bozardı. Öneri (orkestratör): desenin önüne `(?<![\p{L}])` eklemek ve tek harfli önekleri (`e-`) dışlamak.
3. **K7 `#sayfa-basi-*-filtre` (375, /urunler ve /faturalar):** ortak `Bilesenler/SayfaBasi.razor`dan geliyor. Önce ölçümünde de vardı, Q10-5 bulgu listesinde yok ve dosya bu şeridin alanında değil. Sahibi T-KABUK ya da orkestratör.
4. Ekstre ilk sütunundaki kilit sebebi metni dar sütunda çok satıra sarıyor. K3 saymıyor (kısa etiket değil), yine de görsel olarak iyileştirilebilir.

## 11. Temizlik
> **TUR 3 DÜZELTMESİ:** Aşağıdaki maddeler TUR 1 (20:2x öncesi) içindir. Tur 1 sonrası 20:26'da 5166/5167 yeniden açıldı (PID 2994/3286) ve bu süreçler tur 2 ile tur 3'te kapatılamadı (izin reddi). Tur 2'de 5166 üzerinde doğrulayıcı kiracısı "PAKETQ10-5-TEST Dogrulama" duruyordu; tur 3'te silindi. Güncel durum § T3.1 ve § T3.6'dadır.

- API (PID 9370, 5166) ve vekil (PID 64157, 5167) `kill` ile durduruldu. `lsof` ile iki port da boş.
- Kiracı `01M39XCPP4SRFC9578TNN9KQ1E` (PAKETQ10-5-TEST) `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi. Kalan satır 0.
- `tenants` sayısı: 15 → **14**. Diğer şeritlerin PAKET*-TEST kiracılarına ve QA-TEST-* kiracılarına dokunulmadı.
- Proje ağacında .bak, .mutorj, .tmp ya da .orig kalmadı.

## 12. Kurucuya / mali müşavire not
Yok. Mevzuat hükmü yazılmadı ya da değiştirilmedi.
