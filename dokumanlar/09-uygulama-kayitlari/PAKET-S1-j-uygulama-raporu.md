# PAKET S1-j — MENÜ ARAMASI: ilk tuşta çökme · sonuçlar menünün içinde · kayıt araması Ctrl+K'ya · hava CSP · konsol 401

**Tarih:** 2026-09-24 (15:05 – 16:22) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** DEMO DÜZELTME (v2 şablonu)
**Yalıtım:** test VT `kuyumcu_test_i` · canlı API 5178 · inceleme vekili 5177 · 5179/5181/5199/5200 portlarına dokunulmadı.
**Kaynak:** kurucu 24.09 14:40 (/urunler/agac, "Menüde ara" kutusuna "sat") + orkestratör ölçümü (ORKESTRATOR-DURUM 14:50) + katalog K11/K12.

## 1. Özet
| Kusur (önce, ölçülmüş) | Şimdi (ölçülmüş) |
|---|---|
| İlk tuşta `IndexOutOfRangeException` (`MenuTanimi.Sadelestir("")` → `sozcukler[0]`), Blazor "Beklenmeyen bir hata oluştu" bandı `display: block` | Sadelestir boş/boşluklu metinde `""` döner; menü kutusu eşleşmeyi `MenuTanimi.AramaEslesir` ile yapar (boş sorgu hiçbir şeyle eşleşmez); keydown metni OKUMAZ. 16 tarayıcı ölçümünde bant `none`, istisna 0 |
| "sat" → menünün DIŞINDA, içeriğin üstünde BEYAZ pencere (`rgb(255,255,255)`, 480×256 px, EKRANLAR + KAYITLAR + "En çok 10 kayıt…") | Kenar çubuğunun KENDİSİ süzülür: görünen `.kenar-satir` = 2 (Satış, Satış analizi), eşleşen gruplar açık, diğer satır/grup/"Tüm ekranlar"/Kılavuz/Ayarlar `[hidden]`; menü dışı arama paneli 0 |
| Kayıt araması menü kutusuna gömülüydü | Kayıt araması SİLİNMEDİ: üst barda yazılı "Müşteri, ürün ara" düğmesi (`#ust-kayit-ara`) + Ctrl/Cmd+K → palet; "Anadolu" → "Anadolu Kuyum Ticaret" bulundu (8/8 ölçüm) |
| Panel: istemci `api.open-meteo.com`'a doğrudan fetch → CSP `connect-src 'self'` ihlali (her Panel açılışında `log-security-error`) | İstemci yalnız `GET /api/v1/hava` (kendi sunucusu) çağırır; sunucu dış kaynağı çağırır, 30 dk önbellekler, kapalı/hata/zaman aşımında 204 → hava HİÇ çizilmez (uydurma yok). CSP gevşetilmedi. Panel açılışında CSP ihlali 0; hava "İstanbul 20°C Az bulutlu" |
| Konsolda 401 | Kaynak ÖLÇÜLDÜ: süresi dolmuş erişim jetonuyla ilk istek 401 → `/auth/yenile` 200 → istek yinelenir (beklenen yenileme akışı, § 8.3). Taze oturumla 4 akışta 401 = 0 |

## 2. Spesifikasyonun koda karşı doğrulanması ve sapmalar
1. **Çökme iddiası doğru** — `MenuTanimi.cs:283` `kalan = [sozcukler[0]]` boş metinde; `GlobalArama.TusaBasildi → Hedefler → EslesenEkranlar → Sadelestir(metin.Trim())`. ÖNCE kopyasında yeniden üretildi (§ 9.1: `crit: … IndexOutOfRangeException`).
2. **"Yeni GET /api/v1/hava" — uç ZATEN VARDI** (P-37, `Kuyumcu.Api/Modules/Hava/HavaModulu.cs`, varsayılan KAPALI, 204). Yeni uç açılmadı; mevcut uç kullanıldı, önbellek **15 → 30 dk** yapıldı (koşul), API `appsettings.Development.json`'a `Hava` bölümü eklendi (geliştirme/pilot AÇIK; üretim `appsettings.json`'da anahtar yok → KAPALI). Asıl kusur istemcideydi: `Kuyumcu.Web/Servisler/HavaServisi.cs` dış kaynağa doğrudan gidiyordu → artık `ApiIstemci` ile `/hava`.
3. **Dosya alanı dışına çıkılan yerler (gerekçeli):**
   - `Kuyumcu.Web/Servisler/HavaServisi.cs` — hava istemcisinin kendisi (Panel.razor yalnız bu servisi çağırır; Panel.razor'a DOKUNULMADI).
   - `Kuyumcu.Web/Bilesenler/Form/Arama.razor` — menü kutusu ham `<input>` olarak yazılınca denetim `HAM_INPUT 43 > 42` ve `HAM_INPUT_SERBEST 1` kırmızı verdi. Kural "ortak Form bileşeni kullanılmalı" → menü kutusu `<Arama>` oldu; bileşene **eklemeli** iki parametre kondu (`TusBasti` her tuşta geri çağrı, `EkOznitelikler` aria öznitelikleri). Verilmezse davranış eskisiyle aynı (mevcut 435 ilgili test yeşil).
   - `Kuyumcu.Web/wwwroot/js/siralistesi.js` — Ctrl/Cmd+K artık palet kapalıyken `.NET KisayolAc`'ı çağırır (kutu DOM'da yokken odaklanacak şey yok).
   - `dotnet/denetim.sh` — `HAVA_BEKLETME_KAPISI` yeni mimariye göre yeniden yazıldı (istemci: iptal + sessiz düşme + kendi HttpClient'ı/dış adresi 0; sunucu: Timeout + varsayılan KAPALI); `APIISTEMCI_DISI_HTTPCLIENT` beyaz listesinden HavaServisi ÇIKARILDI (sıkılaştırma).
   - `Kuyumcu.Web/wwwroot/appsettings.json` + `.Development.json` — istemci tarafı hava anahtarları kaldırıldı (UX-S22: aynı soruya iki cevap olmasın; karar sunucuda).
4. **Kısayol rozeti** ("Ctrl K") menü kutusundan kalktı: Ctrl+K artık KAYIT paletini açıyor; menü kutusunda göstermek yanıltırdı. Rozet üst bardaki düğmede.
5. **Eşleşme kuralı:** menü araması `Sadelestir` kullanmaz (genel ekleri attığı için "rapor" yazan "Kasa raporu"nu bulamazdı). Yeni tek kural `MenuTanimi.AramaEslesir`: sorgunun her sözcüğü hedefin (ad + S1-g eski adı) bir sözcüğünün başı; Türkçe harf katlama `AramaMetni.Katla` (tek kaynak). Palet (GlobalArama EKRANLAR) aynı kuralı kullanır.
6. **Tarayıcı ölçümünde bulunan ek kusur:** hava artık gerçekten geldiği için basit Panel 375 px'te "Az bulutlu" iki satıra bölünüp üst üste bindi (K3 kırık 1 · K9 binen 2). `app.css` sonuna `.karsilama-hava-sade { display:inline-block; white-space:nowrap }` → 0.
7. **Tam süitte bulunan kural:** `S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` S1-c bloğundan dosya SONUNA kadar `overflow` yasaklıyor; paletin `overflow-y` satırı kaldırıldı, yükseklik sınırı P-15'in `.kabuk-arama-paneli` (70vh + kendi içinde kaydırma) kuralından gelir (palet 800/812 px ekranda sığdı, yatay 0).
8. **Dar ekran:** palet açılırken açık menü çekmecesi kapanır (`Kabuk.Kapat()`); 375 ölçümünde çekmece paletin perdesinin üstünde kalıyordu.
9. **Mevzuat:** bu pakette mevzuat hükmü yok. **Lisans notu (kurucu kalemi):** Open-Meteo ücretsiz katmanı ticari olmayan kullanım içindir (CC BY 4.0, uç açıklamasında zaten yazılı); üretimde varsayılan KAPALI kaldı.

## 3. Yapılanlar (dosya dosya)
* `Kuyumcu.Web/Layout/MenuTanimi.cs` — Sadelestir boş koruması; `AramaSozcukleri`, `AramaVar`, `AramaEslesir(hedef, sorgu)`, `AramaEslesir(oge, t, sorgu)`.
* `Kuyumcu.Web/Layout/KenarCubugu.razor` — menü içi arama: `<Arama ElemanId="menu-arama-girdi">` (yer tutucu "Menüde ara"), × (`#menu-arama-temizle`), görünmez durum satırı (`#menu-arama-durum`, eşleşme sayısı), eşleşmeyen satır/grup/favori/"Tüm ekranlar"/alt blok `hidden`, arama sürerken grup başlığı düğme değil düz başlık (katlama tercihine YAZMAZ), eşleşme yoksa `#menu-arama-yok` "Eşleşen ekran yok", ↑/↓ (`secili` sınıfı + `aria-activedescendant`), Enter (seçili ya da ilk eşleşen), Esc/× eski hâl. Alt blok bağlantılarına `#kenar-alt-<slug>` id'si EKLENDİ (mevcut id değişmedi).
* `Kuyumcu.Web/Bilesenler/GlobalArama.razor` — menüden ayrıldı: üst bar düğmesi `#ust-kayit-ara` + palet (`#kabuk-arama-palet`, `role=dialog`, perde `#kabuk-arama-perde`); eski kimlikler korundu (`#kabuk-arama-girdi/-paneli/-kapat/-ekranlar/-kayitlar/-kayit-N/-hata/-durum`); Esc/Kapat/perde paleti kapatır; × yalnız metni siler; `[JSInvokable] KisayolAc`; açılınca odak kutuya; boş metinde eşleşme aranmaz. ModalPencere kullanılmadı (DoD EK md.2).
* `Kuyumcu.Web/Layout/UstBar.razor` — yalnız `<GlobalArama />` tetiği (`.ust-sag` başı).
* `Kuyumcu.Web/Bilesenler/Form/Arama.razor` — `TusBasti`, `EkOznitelikler` (eklemeli).
* `Kuyumcu.Web/Servisler/HavaServisi.cs` — `ApiIstemci` ile `/hava`; 3 sn iptal; her istisna → null; derecesiz yanıt → null (uydurma yok).
* `Kuyumcu.Api/Modules/Hava/HavaModulu.cs` — önbellek 30 dk. `Kuyumcu.Api/appsettings.Development.json` — `Hava` (Etkin, Şehir, Enlem, Boylam).
* `Kuyumcu.Core/I18n/Sozlukler.MenuArama.cs` (YENİ) — `MenuAramaSozlugu` TR+EN (yer tutucu, etiket, temizle, "Eşleşen ekran yok", eşleşme sayısı, "Müşteri, ürün ara", ipucu, palet başlığı).
* `Kuyumcu.Web/wwwroot/css/app.css` — yalnız dosya SONUNA S1-j blokları (belirteçle; sabit renk yok; `overflow` yok).
* `Kuyumcu.Web/wwwroot/js/siralistesi.js`, `denetim.sh`, Web `appsettings*.json` — § 2.3.
* Testler: `S1jMenuAramaTestleri.cs` (YENİ, 39), `S1jHavaTestleri.cs` (YENİ, 7), `KabukAramaEkranTestleri.cs`, `P37RevizyonTestleri.cs`, `P37bKarsilamaTestleri.cs` (sözleşme değişikliği işlendi, kapsam daralmadı).

### Dokunulan sayfalar (TEK DOKUNUŞ kaydı)
Razor: `Layout/KenarCubugu.razor`, `Layout/UstBar.razor`, `Bilesenler/GlobalArama.razor`, `Bilesenler/Form/Arama.razor`. `Pages/*.razor`: **hiçbiri** (Panel.razor değişmedi).

## 4. DoD sözleşmesi
Bu paket **yeni varlık / kayıt türü doğurmaz**: menü araması durumu yalnız bileşen belleğindedir (tercih tablosuna yazılmaz — testle kanıtlı: aramada PATCH/DELETE 0); hava verisi veritabanına yazılmaz (yalnız bellekte 30 dk). Düzenlenecek/pasife alınacak kayıt yoktur. Her düğme iş yapar: `#menu-arama-temizle` (metni siler), `#ust-kayit-ara` (paleti açar), `#kabuk-arama-kapat` (kapatır), `#kabuk-arama-temizle` (metni siler), sonuç düğmeleri (gezinir). Hava ucu `[Authorize]` + `[HerkeseAcik]` (P-37'den, değişmedi; kiracı verisi taşımaz). Migration: gerekmedi.

## 5. Derleme ve testler
* `dotnet build Kuyumcu.sln --nologo` (son temiz derleme, son mutasyondan sonra): **0 Uyarı · 0 Hata**. (Ara derlemelerde görülen `EtiketEkranTestleri.cs(145) xUnit1031` uyarısı bu paketin dosyası değildir; son temiz derlemede görünmedi.)
* Filtreli: `S1j|KabukArama|P37|S1cUstBar|Form|Arama|Menu` → **435/435**.
* **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_i`, trx `…Desktop-ByGatech-projeler-jewellery/…/scratchpad/trx-pS1-j/_Ramazan-MacBook-Air_2026-09-24_16_13_05.trx`): **5267 / 5267 geçti, 0 başarısız.** Önce 5209 (KAT-1 devri, kırmızı listesi boş) → sonra 5267 (**+58**: S1jMenuArama 39 · S1jHava 7 · KabukArama +9 · P37b +3; P37Revizyon 3 test yerinde yeniden yazıldı). **Bu paketin ürettiği yeni kırmızı: 0.**
* Ara koşumlarda görülen ve giderilen: (a) `S1c_CSS_blogu…` (paletin `overflow-y`'si, § 2.7); (b) paralel sınıflar arası kararsızlık — yeni sınıfım kurucusunda statik `KabukServisi.Ortak.AramaSubesiAta(null)` çağırıyordu (`KabukAramaEkranTestleri.Secilen_sube…` bir kez kırmızı) → çağrı kaldırıldı; `Input`'tan hemen sonra `Find` statik olayın tetiklediği yeniden çizim yüzünden eski ağacı görebiliyordu (`Menu_aramasinda_yazinca_TEMIZLE…` tam süitte bir kez) → testler değerin çizilmesini bekler (`WaitForAssertion`/`WaitForElement`). Sonrasında odaklı koşum 3/3 + 4/4 temiz, tam süit 0 kırmızı.
* `bash dotnet/denetim.sh` → **DENETİM TEMİZ** (ilgili sayaçlar: `HAVA_BEKLETME_KAPISI 1`, `APIISTEMCI_DISI_HTTPCLIENT 0`, `HAM_INPUT 42 (tavan 42)`, `HAM_INPUT_SERBEST 0`, `ORTAK_GIRDI_ISTISNA 3 (tavan 3)`, `GIRDI_AILESI_KULLANIM 374`, `KARSILAMA_HAVA_KONUM_IZNI 0`).

### Sınır testleri (kör inceleme / v2)
Sadelestir: `""`, `" "`, `"   "`, `"\t"` → `""`; `s/ş/İ/ı/Rapor`. Eşleşme: `sat`, `SATIŞ`, `cari`, `masak`, `kâr`, `ısparta`, `rapor`, `denetim izi`, `""`, `"   "`, `"***"`, `"<>"`, 5000×`ş`. Menü (bUnit): `""`, `"   "`, `s`, `ş`, `İ`, `ı`, `*?%&<>\"'` — tuşlar (s/↓/↑/Enter) input'tan ÖNCE ve sonra; 2000 karakter; basit mod; favoriler; alt blok ("ayar"); eşleşme yok. Palet: aynı 7 sınır metni + Esc + Ctrl+K kapalıyken.

## 6. Mutasyon kanıtları (kopya → `perl -0777 -pi` tek değişiklik → filtreli test → geri kopya → `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M1 | `MenuTanimi.Sadelestir` boş koruması (`if (sozcukler.Length == 0) return "";`) silindi | `Sadelestir_bos_ve_bosluklu_metinde_COKMEZ_bos_doner` ×4 (`""`, `" "`, `"   "`, `"\t"`) | cmp AYNI |
| M2 | Menü süzmesi kapatıldı (`hidden="@(AramaVar ? !Eslesir(oge) : !acik)"` → `hidden="@(!acik)"`) | bUnit: `Sat_yazinca_gorunen_menu_satirlari_YALNIZ_eslesenlerdir`, `Basit_modda_da_menu_suzulur`, `Eski_adla_arama_calisir_cari_Musterileri_gosterir` · **gerçek tarayıcı** (mutasyonlu derleme, 1280 BASİT açık ve 375 PRO koyu): görünen satır **11** (beklenen 2) | cmp AYNI |
| M3a | API hava ucu ağ hatasında uydurma sıcaklık döndürür (`return null;` → `return new HavaSonucu(21m, 0, "İstanbul", "UYDURMA");`) | `S1jHavaSaftestleri.Kaynaga_ULASILAMAZSA_UYDURMA_sicaklik_yok_null` | cmp AYNI |
| M3b | İstemci derecesiz/boş yanıtta uydurma sıcaklık üretir | `P37bKarsilamaTestleri.Hava_sunucu_DERECESIZ_yanit_verirse_0_derece_UYDURULMAZ`, `Hava_KAPALIYKEN_kutucuk_cizilmez_ve_HATA_BANDI_atilmaz` | cmp AYNI |
| M4 | Menü Esc'i etkisiz (`AramayiTemizle()` çağrılmaz) | `Esc_ve_Temizle_menuyu_KATLAMA_TERCIHLI_haline_dondurur_tercihe_YAZMAZ` | cmp AYNI |
| M5 | Ctrl+K geri çağrısı paleti açmaz (`KisayolAc` içinden `Ac()` silindi) | `KabukAramaEkranTestleri.CtrlK_kisayolu_kapaliyken_paleti_ACAR` | cmp AYNI |
| M6 | `Form/Arama` tuş geri çağrısını iletmez | `Ok_tuslari_eslesenler_arasinda_gezer_Enter_ekrana_gider`, `Enter_secim_yokken_ILK_eslesene_gider`, `Esc_ve_Temizle…` | cmp AYNI |
M1/M2/M4/M5/M6 test sertleştirmesinden SONRA yeniden koşuldu (aynı sonuç). Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata; tam süit ve son tarayıcı turu bu ikiliyle.

## 7. Canlı doğrulama (API 5178 · kuyumcu_net · test kiracısı `PAKETS1-j-TEST Kuyumculuk` `01M39PS16PJG85PRHX431XX9YB`)
| İstek | HTTP | Yanıttan okunan |
|---|---|---|
| POST /api/v1/auth/kayit (tek kullanımlık davet, not `PAKETS1-j-TEST`) | 201 | alanlar `accessToken, refreshToken, tenantId` (değerler yazılmadı) |
| GET /api/v1/hava (oturumsuz) | 401 | `"Token bulunamadı"` |
| GET /api/v1/hava (oturumlu, 1.) | 200 (0,43 sn) | `{"derece":19.7,"kod":2,"sehir":"İstanbul","kaynak":"Open-Meteo (CC BY 4.0)"}` |
| GET /api/v1/hava (oturumlu, 2.) | 200 (0,00 sn) | aynı gövde — önbellekten |
| POST /api/v1/cari `{"tur":"MUSTERI","ad":"Anadolu Kuyum Ticaret"}` | 201 | id `01M39PSPH9Y41RQCVR10DP0RZB` |
| GET /api/v1/arama?q=Anadolu&limit=10 | 200 | `sayi 1`, `rota "/cariler?cariId=01M39PSPH9Y41RQCVR10DP0RZB"` |
SQL: `select id,ad,tur from cariler where tenant_id='01M39PS16PJG85PRHX431XX9YB'` → `01M39PSPH9Y41RQCVR10DP0RZB · Anadolu Kuyum Ticaret · MUSTERI`.
HTTP testleri (TestServer, "Testing" ortamı, hava yapılandırılmamış): `GET /api/v1/hava` oturumlu **204 + boş gövde**, oturumsuz **401**.

## 8. Gerçek tarayıcı ölçümü (GERÇEK TARAYICI KURALI)
Araç: başsız Chrome + CDP (`scratchpad/s1j-tara.mjs`), güncel `betikler/sikisma-olc.js` (`__olcTam`, K1–K11), vekil 5177 → API 5178, kendi oturumum (`/__giris`); **gerçek tuş olayları** (`Input.dispatchKeyEvent`: keydown → input sırası kurucunun tarayıcısındaki gibi). Mod `PATCH /kullanicilar/ben` ile (tümü 200; `data-mod` doğrulandı), tema `data-tema` aydinlik/karanlik doğrulandı. Ayrıca tarayıcı bölmesinde kendi sekmem (tab-41, 1280, PRO) "sat" yazıldı: görünen satır 2, bant `none`, açılır pencere 0 (javascript_tool).

### 8.1 Rota × genişlik × mod × tema (son tur `tam5`, 16:14)
Hücre: sayfa boyu/ekran · K2 (en sağ kart %) · yalnız sıfır olmayan sayaçlar (K1,K3 taşan/kırık,K5,K6,K7,K8,K9,K10,K11,yatay).
| Ölçüm | B-açık-1280 | P-açık-1280 | B-koyu-1280 | P-koyu-1280 | B-açık-375 | P-açık-375 | B-koyu-375 | P-koyu-375 |
|---|---|---|---|---|---|---|---|---|
| `/` (Panel) | 1.2 · 98 · 0 | 1.7 · 98 · 0 | 1.2 · 98 · 0 | 1.7 · 98 · 0 | 2.0 · 97 · 0 | 2.9 · 97 · 0 | 2.0 · 97 · 0 | 2.9 · 97 · 0 |
| `/islemler` | 2.2 · 88 · 0 | 1.7 · 88 · 0 | 2.2 · 88 · 0 | 1.7 · 88 · 0 | 2.7 · 97 · 0 | 2.3 · 97 · 0 | 2.7 · 97 · 0 | 2.3 · 97 · 0 |
| `/urunler/agac` | 1.3 · 98 · 0 | 1.3 · 98 · 0 | 1.3 · 98 · 0 | 1.3 · 98 · 0 | 1.2 · 97 · 0 | 1.3 · 97 · 0 | 1.2 · 97 · 0 | 1.3 · 97 · 0 |
| `/urunler/agac` + "sat" | 1.1 · 0 | 1.1 · 0 | 1.1 · 0 | 1.1 · 0 | 1.2 · 0 | 1.3 · 0 | 1.2 · 0 | 1.3 · 0 |
| Hata bandı ("sat") | none | none | none | none | none | none | none | none |
| Görünen `.kenar-satir` ("sat") | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| Görünen başlık / alt / Tüm ekranlar | günlük+rapor / 0 / gizli | aynı | aynı | aynı | aynı | aynı | aynı | aynı |
| Menü dışı arama paneli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `[hidden]` görünen · kenar yatay · üst bar yatay | 0·0·0 | 0·0·0 | 0·0·0 | 0·0·0 | 0·0·0 | 0·0·0 | 0·0·0 | 0·0·0 |
| ↓↓ seçili | satış-analizi | ← | ← | ← | ← | ← | ← | ← |
| Esc → eski hâl (gizlilik haritası birebir) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| "kasa" → × → eski hâl | 3 satır → ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| "zzqx" → "Eşleşen ekran yok" (bant none) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Ctrl+K → odak `#kabuk-arama-girdi` → "Anadolu" | Müşteri · Anadolu Kuyum Ticaret | Cari · Anadolu… | Müşteri · … | Cari · … | Müşteri · … | Cari · … | Müşteri · … | Cari · … |
| Palet kenar boşluğu sol/sağ · yatay · Esc kapattı | 352/352 · 0 · ✓ | ← | ← | ← | 16/16 · 0 · ✓ | ← | ← | ← |
| Panel hava | "· İstanbul 20°C Az bulutlu" | "İstanbul 20°C Az bulutlu" | ← | ← | ← | ← | ← | ← |
| Konsol olayı (Panel + 3 akış) | vekil-WS 2 · diğer 0 | ← | ← | ← | ← | ← | ← | ← |
K2 < %70 yok; K4: `/islemler` BASİT 375 2,7 boy (bölüm 3 — katalogdaki gerekçeli durum, bu paketin kapsamı değil).
Görseller (SONRA): `dokumanlar/09-uygulama-kayitlari/ham/S1j-ekran/menu-sat-{1280,375}-{basit,pro}-{acik,koyu}.png` (8), `ctrlk-anadolu-…png` (8), `panel-1280-{basit,pro}-acik.png`.

### 8.2 ÖNCE (aynı ağacın S1-j öncesi hâli, scratch kopyada yeniden kuruldu; 16:20)
`scratchpad/once/dotnet` = güncel ağaç − S1-j değişiklikleri (eski GlobalArama işaretlemesi + Sadelestir koruması yok + eski istemci HavaServisi + eski Web appsettings), aynı 5178/5177, geçici kiracı `PAKETS1-j-TEST Once`.
| Ölçüm | 1280 PRO açık | 375 BASİT açık |
|---|---|---|
| "sat" sonrası hata bandı | **block** | **block** |
| Konsol | `crit: … Unhandled exception rendering component: Index was outside the bounds of the array. System.IndexOutOfRangeException` | aynı |
| Menü dışı panel | görünür, sol 12 / üst 151, **480×256**, zemin **rgb(255,255,255)**, metin "En çok 10 kayıt gösterilir… EKRANLAR Satış Satış analizi KAYITLAR" | 343×352, beyaz |
| Panel açılışı | `log-security-error: Connecting to 'https://api.open-meteo.com/v1/forecast?…'` + `Fetch API cannot load …`; hava çizilmedi | aynı |
Görseller: `ham/S1j-ekran/ONCE-menu-sat-1280-pro-acik.png`, `ONCE-menu-sat-375-basit-acik.png`.

### 8.3 401 kaynağı (deney)
Oturum açıkken `localStorage['kuyumcu-access']` bozuk bir jetonla değiştirildi, `/` yeniden yüklendi. Ağ: `401 /api/v1/gorunum` → `200 /api/v1/auth/yenile` → `200 /api/v1/gorunum` → sonraki 17 istek 200; sayfa çizildi ("İyi günler Test"), bant `none`. Konsolda TEK satır: "Failed to load resource: … 401". Erişim jetonu ömrü 15 dk (`Jwt:AccessDakika`); kurucunun/orkestratörün oturumu 15 dk'yı aşınca ilk istek 401 alır, tek uçuşlu yenileme onu kapatır — **beklenen yenileme akışıdır**, ürün hatası değil. Taze oturumla 16 ölçümün hiçbirinde 401 yok.
**Vekil WebSocket satırı:** her yüklemede `WebSocket connection to 'ws://localhost:5177/hubs/kur…' failed: Unexpected response code: 200` + `MONO_WASM: WebSocket error` — inceleme vekili (python `http.server`) WebSocket yükseltmesini desteklemiyor, SignalR uzun yoklamaya düşüyor (ağda `/hubs/kur?id=…` 200). Ölçüm aracının satırıdır; vekilsiz sunucuda (5199) ölçmedim (dokunma yasağı) → aşağıda açık kalan.

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Çökme: Sadelestir boş/boşlukta boş döner; boş sorguda eşleşme aranmaz; keydown/input sırasından bağımsız; sınır testleri | SAĞLANDI | § 5 sınır testleri, M1, § 8.1 bant none ×16, § 8.2 önce block |
| (2) Menü içi arama: kenar çubuğu süzülür, eşleşen grup açılır, eşleşmeyen [hidden]; Tüm ekranlar + favoriler süzülür; eşleşme yoksa tek satır; ×/Esc eski hâl; ↑/↓, Enter; eski adla arama; açılır pencere / KAYITLAR / "En çok 10" menüde YOK | SAĞLANDI | S1jMenuArama 39 test, P37 `Menu_aramasi_menunun_DISINDA_pencere_ACMAZ`, M2 (bUnit + tarayıcı), M4, M6, § 8.1 |
| (3) Kayıt araması silinmez: Ctrl+K + üst bar düğmesi; GlobalArama menüden ayrıldı; yer tutucu "Menüde ara" | SAĞLANDI | KabukArama testleri, M5, § 8.1 Ctrl+K "Anadolu" 8/8 |
| (4) Hava sunucudan (/api/v1/hava, 30 dk, hata → 204, uydurma yok); CSP gevşetilmez | SAĞLANDI | § 7, S1jHava 7 test, P37b 3 yeni test, M3a/M3b, § 8.1/8.2 CSP 0 ↔ önce ihlal; `connect-src 'self'` değişmedi |
| (5) Konsol: /urunler/agac, /, /islemler, arama akışında hata 0; 401 kaynağı bulunur | SAĞLANDI (vekil WS satırı hariç, gerekçeli) | § 8.1 "diğer 0", § 8.3 deney |
| KABUL (a) bant none, istisna 0 | SAĞLANDI | § 8.1 |
| KABUL (b) görünen .kenar-satir = eşleşen (2); içerik üstünde pencere 0 | SAĞLANDI | § 8.1 |
| KABUL (c) × ve Esc sonrası eski hâl | SAĞLANDI | § 8.1 gizlilik haritası birebir |
| KABUL (d) Ctrl+K "Anadolu" bulur | SAĞLANDI | § 7 + § 8.1 |
| KABUL (e) Panel CSP ihlali 0, hava sunucudan ya da hiç | SAĞLANDI | § 8.1 |
| KABUL (f) sikisma-olc K1..K11 = 0 ilgili ekranlarda | SAĞLANDI | § 8.1 (hava taşması bulundu ve düzeltildi, § 2.6) |
| Önce/sonra ekran görüntüsü | SAĞLANDI | § 8.1, § 8.2 |
| Mutasyonlar (Sadelestir · süzme bUnit+tarayıcı · uydurma hava) | SAĞLANDI | § 6 |
| Paket kapısı 1–7 (build 0 · testlerim 0 kırmızı · sayaçlarım ✓ · mutasyon · canlı · kiracı silindi · yeni kırmızı 0) | SAĞLANDI | § 5–7, § 10 |

## 10. Temizlik
API 5178 ve vekil 5177 durduruldu (`lsof` ikisi de boş); başsız Chrome'lar betik sonunda kapandı. `PAKETS1-j-TEST Kuyumculuk` (`01M39PS16PJG85PRHX431XX9YB`, 311 satır) ve `PAKETS1-j-TEST Once` (`01M39S5ZEH1B644N492DFGFKYE`, 23 satır) `betikler/kiraci-temizle.sh --uygula` ile silindi; `tenant_id` kolonu olan 70 tablonun tamamında kalan satır **0**; `kiraci_davetleri`'nden iki davet satırım silindi (kalan 0). `tenants`: başta **10**, sonda **10** (QA-TEST-* ve DEMO-* dokunulmadı). Fikstür/jeton dosyaları `printf ''` ile boşaltıldı. ÖNCE kopyası `scratchpad/once/` altında (proje ağacı dışında).

## 11. Değişen dosyalar (bu paket)
```
dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
dotnet/Kuyumcu.Web/Layout/UstBar.razor
dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor
dotnet/Kuyumcu.Web/Bilesenler/Form/Arama.razor
dotnet/Kuyumcu.Web/Servisler/HavaServisi.cs
dotnet/Kuyumcu.Web/wwwroot/css/app.css            (yalnız dosya sonu: S1-j blokları)
dotnet/Kuyumcu.Web/wwwroot/js/siralistesi.js
dotnet/Kuyumcu.Web/wwwroot/appsettings.json
dotnet/Kuyumcu.Web/wwwroot/appsettings.Development.json
dotnet/Kuyumcu.Api/Modules/Hava/HavaModulu.cs
dotnet/Kuyumcu.Api/appsettings.Development.json
dotnet/Kuyumcu.Core/I18n/Sozlukler.MenuArama.cs    (YENİ)
dotnet/denetim.sh
dotnet/Kuyumcu.Tests/S1jMenuAramaTestleri.cs       (YENİ)
dotnet/Kuyumcu.Tests/S1jHavaTestleri.cs            (YENİ)
dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs
dotnet/Kuyumcu.Tests/P37RevizyonTestleri.cs
dotnet/Kuyumcu.Tests/P37bKarsilamaTestleri.cs
dokumanlar/09-uygulama-kayitlari/ham/S1j-ekran/*.png (20)
dokumanlar/09-uygulama-kayitlari/PAKET-S1-j-uygulama-raporu.md
```
(Ağaç tümüyle commit'siz olduğu için `git status --porcelain` bu dosyaların çoğunu önceki şeritlerden de "??"/" M" gösterir; liste dosya zaman damgasından ve düzenleme kaydımdan çıkarıldı.)

## 12. Açık kalanlar
1. **401 satırı (süresi dolmuş oturum):** beklenen yenileme akışı; konsolu tamamen temiz tutmak istenirse `ApiIstemci` isteği göndermeden önce jetonun `exp`'ine bakıp süresi dolmak üzereyse önce yenileyebilir (merkezi dosya, bu paketin alanı dışı — ayrı küçük paket önerisi, sahibi: Taşkın).
2. **Vekil WebSocket satırı:** inceleme vekili (`betikler/inceleme-vekili.py`) WebSocket yükseltmesini iletmiyor; QA/kurucu vekil üzerinden bakarsa her yüklemede bu satırı görür. Vekile WS geçişi eklenmesi (sahibi: Batuhan / orkestratör betikleri) ya da ölçümde bu satırın ayrı sınıflanması önerilir. Vekilsiz sunucuda ölçülmedi (5199 dokunma yasağı).
3. **Open-Meteo ticari kullanım:** üretimde hava KAPALI (varsayılan). Açmak için lisans/ücretli plan kararı — **kurucu kalemi** (değişmedi).
4. Paralel test sınıfları arasında statik `KabukServisi.Ortak` paylaşımı (önceden var olan tasarım) kararsızlık üretebiliyor; bu paketin testleri buna dayanıklı yazıldı, diğer sınıflardaki aynı kalıp (ör. `P37RevizyonTestleri` kurucusundaki `AramaSubesiAta(null)`) ayrı bir temizlik işidir.
