# PAKET SADE-1 — Göz incelemesi artıkları · uygulama raporu

**Tarih:** 2026-09-25 · **Rol:** Taşkın (backend, bu pakette uç değişmedi) + Ertuğ (frontend) · **Şerit:** SADE (t7 · API 5170 · vekil 5171 · CDP 9800–9807)
**Kaynak:** `ham/GOZ-INCELEME-2026-09-25.json` (listede 0-tabanlı dizin: #11, #13, #14, #15, #17, #18, #23, #24, #25, #26, #27, #31, #38, #41, #42, #63, #64, #73)
**Durum:** BİTTİ — tur 2 (önceki doğrulamanın 9 bulgusu kapatıldı/ölçüldü, § 0) dahil 7 paket kapısı maddesi kanıtlı. **COMMIT YOK.**

## 0. TUR 2 (2026-09-25 öğleden sonra) — önceki doğrulamanın bulguları

| Bulgu (doğrulayıcı) | Kök neden / karar | Yapılan | Kanıt |
|---|---|---|---|
| [ORTA] #25 hukuki tam metin MOD'a bağlıydı, ROL'e değil | `#kur-sozlesmesiz-ayrinti` yalnız PRO "Kaynak" sekmesinde; PRO'ya geçen tezgâhtar okuyabiliyordu; rozetin `title`'ı da tam metni herkese veriyordu | `Kur.razor`: `SahipMi` (Oturum.Kullanici.Rol == "SAHIP") — ayrıntı kutusu YALNIZ SAHİP'e; diğer rollerde yerine tek cümle sebep `#kur-sozlesmesiz-sahipte` ("…kullanım şartını dükkân sahibi görür", UX-S30); rozet `title`'ı SAHİP'te tam metin, diğerlerinde aynı kısa cümle. Yeni metin `Sozlukler.Sade.cs` `KurDenemeIpucu` (TR+EN). Metin SİLİNMEDİ. | bUnit `Kur_hukuki_tam_metin_ROLE_bagli_PRO_modda_TEZGAHTAR_gormez_sebep_yazili`, `Kur_hukuki_tam_metin_SAHIP_Kaynak_sekmesinde_okur`, `UX4d.M24` (SAHİP oturumuyla) · mutasyon M1-16, M1-18 kırmızı · **gerçek tarayıcı** (tezgâhtar kullanıcısı API ile açıldı): TEZGÂHTAR PRO 1440/375 aydınlık + 1440 karanlık → `ayrintiGorunur=false`, sayfada "Ticari kullanımda" metni YOK, sebep satırı var; SAHİP PRO 1440/375 → ayrıntı görünür. `ham/TUVAL-KARSILASTIRMA/SADE-1-kur-kaynak-rol-1440.png` (sol tezgâhtar, sağ sahip) · `kurrol/foto/kur__SAHIP_PRO_aydinlik_375__kaynak.png` GÖRÜLDÜ. "Ayarlar/Kur kaynağı ekranına taşıma" doğrulayıcının da yazdığı gibi ikinci dalga işi → § 13. |
| [DÜŞÜK] Ayarlar dükkân görünümü: istek sürerken hata bandı yanıp sönebilir | `SahipMi && !Gorunum.Yuklendi` — "denendi" bilgisi yoktu | `GorunumServisi.OkumaDenendi` eklendi (YukleAsync `finally`'de true; başarısızlıkta `Degisti` tetiklenir ki açık sayfa yeniden çizilsin). `Ayarlar.razor` koşulu `SahipMi && Gorunum.OkumaDenendi && !Gorunum.Yuklendi`, sayfa `Gorunum.Degisti`'ye abone. "Yeniden dene" ağ hatası/zaman aşımını da banda düşürür. | bUnit `Ayarlar_gorunum_OKUNURKEN_hata_bandi_YANIP_SONMEZ_bitince_basari_yoksa_cikar` (istek askıda → bant YOK; 500 dönünce bant VAR) · mutasyon M1-13, M1-17, M1-19, M1-20 kırmızı · tarayıcı (aşağıdaki #41 satırı) A anı: istek 3 sn tutulurken bant 8/8 kombinasyonda YOK. |
| [DÜŞÜK] #14 asıl kök neden 429 altında yeniden üretilmedi | — | Gerçek tarayıcıda (başsız Chrome, CDP `Fetch.requestPaused`) `GET /api/v1/kurulum/durum` her denemede **429 + Retry-After: 1** ile yanıtlandı (ISTEK-1'in ApiIstemci geri çekilmesi birleşik ağaçta devrede). | 8/8 kombinasyon (BASİT/PRO × aydınlık/karanlık × 1440/375): istek sürerken dönen halka, 429 sonrası `#kur-hata` bandı ("Biraz yoğunluk var… Yeniden dene") + `#kur-yeniden`; "Yükleniyor" yazısı 0; "Yeniden dene" gerçek istek attı ve 6 adım yüklendi, bant kalktı. `hata2/olcum.jsonl`; yan yana `SADE-1-kurulum-429-1440.png`; `kurulum__BASIT_aydinlik_1440__B_hata.png`, `kurulum__PRO_karanlik_375__C_yeniden.png` GÖRÜLDÜ. |
| [DÜŞÜK] /kilavuz PRO 375 "tren" K4 2.1×4 | PRO'da 47 ekranlık liste açık + 40vh kutu | `sade.css` (9): ≤600 px'te liste kutusu 24vh (liste tam, kendi kutusunda kayar). | tur 2: /kilavuz 375 PRO aydınlık/karanlık **2.0×4** (eşik >2), `analiz.py sonra11` /kilavuz TEMİZ. `sonra12/foto/kilavuz__PRO_karanlik_375.png` GÖRÜLDÜ. |
| (tur 2 ölçümünde) /kurulum 960 PRO K4 2.1×4 | açılış kartının boş durumu `app.css .bos-durum { height:12rem }` | `sade.css` (10): yalnız `#kur-acilis .bos-durum` boyu içeriğe göre (metin + ikon kalır). | 960 PRO aydınlık/karanlık **2.0×4**; 1440 1.7, 1280 1.9. (375: 2.4–2.5 — ÖNCE'de de 2.3–2.6; § 13.) |
| [DÜŞÜK] sade.css yükleme sırası (katman.css'ten sonra, ortak.css'ten önce) | ORTAK-1 testi O1 ortak.css'in EN SONDA olmasını zorunlu kılıyor | Değişmedi (gerekçe § 2.1) — orkestratör tek kurala bağlayacak. | `OrtakGorunumSaflik` yeşil. |
| [DÜŞÜK] Tur aracında "aydınlık" kareler koyu | Aynı kullanıcıyla aydınlık+karanlık işçi eşzamanlı → sunucudaki tema tercihi yarışıyordu | KENDİ kopyamda (`scratchpad/tur-sade/tur.mjs`): her gezinmeden sonra tema yeniden uygulanır ve `data-tema` okunup `temaGercek` alanına yazılır; temalar SIRAYLA koşuldu (`kos3.sh`). Ortak araç (betikler/tur) orkestratörün. | tur 2: 224/224 sayfada istenen tema = gerçek tema (uyumsuz 0). Önceden koyu çıkan `kur__BASIT_aydinlik_1280.png` artık açık — GÖRÜLDÜ. |
| [KRİTİK-hakem] derleme uyarı sayısı 0 mı 1 mi | Tur 1'deki "0 uyarı" ARTIMLI derlemeydi; o koşumda test projesinin analizör uyarısı raporlanmadı. | Yeniden ölçüldü. | `dotnet build Kuyumcu.sln --no-incremental` (son kod): **1 Uyarı, 0 Hata** — `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` (UrunEtiket → ORTAK-1 dosyası, SADE-1'in değil). Doğru sayı 1'dir; tur 1 raporundaki "0 uyarı" iddiası bu ölçümle DÜZELTİLDİ. |
| [KRİTİK-hakem] #41 tarayıcıda ölçülmemişti | Canlı GET /gorunum 200 döndüğü için hata anı üretilememişti | Hata anı gerçek tarayıcıda ÜRETİLDİ: CDP ile `GET /api/v1/gorunum` 429 döndürüldü (açılıştaki okuma dahil), `/ayarlar?sekme=dukkan`. | 8/8 kombinasyon (BASİT/PRO × aydınlık/karanlık × 1440/375): A (istek sürerken) bant YOK · B (429) `#gorunum-okuma-hata` görünür, `role=alert`, zemin aydınlıkta rgb(255,235,232) / karanlıkta rgb(67,29,27) (gri paragraf DEĞİL), "Yeniden dene" var · C "Yeniden dene" GERÇEK istek attı, bant kalktı. Ölçüm K1 0 · K3 0/0 · K5 0 · K9 0 · K10 0 · yatay 0. `hata3/olcum.jsonl`; yan yana ÖNCE/SONRA `SADE-1-ayarlar-hata-bandi-1440.png`; `ayarlar__PRO_aydinlik_1440__B_hata.png`, `ayarlar__PRO_aydinlik_375__B_hata.png`, `ayarlar__BASIT_karanlik_1440__B_hata.png` GÖRÜLDÜ. **Durum: KARŞILANDI (tarayıcı + bUnit + mutasyon).** |
| [DÜŞÜK] başka şeritlerin kırmızıları | — | Tam süit yeniden koşuldu (§ 5). | Son tam süit 6189 / 6187 / **2** — ikisi de `KatmanTekKalipTestleri` (KATMAN, js/katman.js). SADE-1 kaynaklı kırmızı 0. |

**Dürüstlük notu (tur 2):** #41 için ilk denemede bayrağı sayfaya koydum (Ayarlar açılışta GET /gorunum'u kendisi çağırıyordu). Ara tam süitte bu `GorunumEkranTestleri.GE4_Ayar_okunmadiysa_kilitli_kalir`'ı kırdı (sayfa okuma yapınca sahte API başarı döndü). Doğrulayıcının önerdiği yola (bayrak `GorunumServisi`'nde) döndüm; GE4 yeşil. `GorunumServisi.cs` görevdeki SADE-1 dosya listesinde yok ama hiçbir eşzamanlı şeridin listesinde de yok (KATMAN'ın dosyası `GorunumKarti`); değişiklik yalnız EKLEMEDİR (yeni özellik + başarısızlıkta `Degisti`), `S1fGorunumKartiEkranTestleri` ve `GorunumEkranTestleri` yeşil. İlk toplu mutasyon koşumunda M1-3 bayat derleme yüzünden YANLIŞ testi (Kurulum) kırmızı gösterdi; tek başına yeniden koşuldu → doğru test (`Pos_kullanimdan_kaldir_…`) kırmızı.

## 1. Yapılanlar — bulgu bulgu (kök neden → düzeltme)

| # | Rota | Kök neden (koda karşı doğrulandı) | Düzeltme | ÖNCE → SONRA görüntüsü |
|---|---|---|---|---|
| 11 | /veri-disa-aktarim | Sayfada `SayfaBasi` (h2 şerit) + altında sayfanın KENDİ `.sayfa-baslik` `<h1>`+açıklama bloğu vardı; aynı başlık iki kez. SayfaBasi (ORTAK-1) doğru; tekrar SAYFA tarafındaydı. | Sayfanın `.sayfa-baslik` bloğu kaldırıldı. Tek `<h1>` üst barda (UstBar), şerit başlığı SayfaBasi. | `tur/sonuc3/foto/veri_disa_aktarim__BASIT_aydinlik_1440.png` → `tur-sade/sonra2/foto/veri_disa_aktarim__BASIT_aydinlik_1440.png` · yan yana: `ham/TUVAL-KARSILASTIRMA/SADE-1-veri-disa-aktarim-1440.png` |
| 13 | /yedekler | Aynı kök neden (#11). | Aynı düzeltme. | `yedekler__BASIT_aydinlik_1440.png` (ÖNCE sonuc3 / SONRA sonra2) · `SADE-1-yedekler-1440.png` |
| 14 | /kurulum | `durum is null` iken düz `<p>Yükleniyor…</p>` basılıyordu: istek sürerken dönen halka/aria-busy yoktu (tur bunu "yüklendi" sanıp fotoğrafladı) ve **ağ hatası / zaman aşımı (HttpRequestException, TaskCanceledException) yakalanmıyordu** → yazı sonsuza dek kalabiliyordu. | İstek sürerken `<Yukleniyor/>` (aria-busy); hata olursa HataBandi + gerçek istek atan "Yeniden dene" (`#kur-yeniden`); iki istisna da banda çevrildi. Adım kartının başlığı sayfa başlığını ikinci kez yazıyordu ("Kuruluş Sihirbazı") → "Adımlar". | `kurulum__BASIT_aydinlik_1440.png` (sonuc3 → sonra3) · `SADE-1-kurulum-1440.png` |
| 15 | /kurulum 375 | `li` içinde rozet+ad+bağlantı+sayı satır içi akıyordu; 375'te düğme sayı/metinle üst üste. | `sade-adim-listesi` esnek satır: rozet · ad (esner) · sayı · düğme; dar ekranda sayı+düğme alt satıra akar; kart gövdesine yatay pay. | `kurulum__PRO_karanlik_375.png` (sonuc3 → sonra3) · `SADE-1-kurulum-375.png` |
| 17 | /yedekler sekme | Sayfa içi şerit seçili sekmeyi dolu mavi hapla çiziyordu; tuvaldeki Basit/Profesyonel hapı koyu zemin + açık yazıdır. | `#yd-sekme-seridi`, `#kur-sekme-seridi` tuval segment diline (seçili: `--kenar-yuzey`/`--kenar-metin-guclu`; karanlıkta vurgu yüzeyi) — `Sekmeler` bileşenine dokunulmadı. | `SADE-1-yedekler-1440.png` |
| 18 | /kilavuz | 13 bölüm + 47 ekran madde işaretli uzun liste. | Bölümler kart ızgarası; 47 ekranlık liste BASİT'te "Tüm ekranların listesi (47)" katlanında (PRO'da açık), açılınca kendi kutusunda kayar (en çok 40vh). Liste ve id'ler SİLİNMEDİ. | `kilavuz__BASIT_aydinlik_1440.png` (sonuc3 → sonra4) · `SADE-1-kilavuz-1440.png` |
| 23 | /hurda | Bakiye isteği düşünce `veriGeldi=false` ve liste boş → kart yalnız başlık. | İstek düştüyse "Hurda bakiyesi okunamadı — sebebi yukarıdaki uyarıda yazıyor."; veri gerçekten boşsa mevcut BosDurum. Kart gövdesine yatay pay. | `hurda__BASIT_aydinlik_1440.png` (sonra2: boş durum) · `hurda__PRO_aydinlik_960.png` (sonra6: okunamadı satırı) · `SADE-1-hurda-960.png` |
| 24 | /hurda | **Ölçüldü:** sunucu `HurdaStokKapisi.SubeGerekli` (400) — oturum kullanıcısının şubesi yok (tur kullanıcısı MÜDÜR, şubesiz). Sunucu davranışı TASARIM GEREĞİ (OturumSubesinde); hata değil. Ekran ise formu etkin bırakıyordu. | Veri okunamadıysa form `fieldset disabled` (içindeki her girdi/düğme kilitli, soluk) ve formun başında sebep (UX-S30): şubesiz kullanıcıya "Hesabınıza bir şube atanmamış. Dükkân sahibi Kullanıcılar ekranından size şube atayınca bu ekran açılır." Kaydet düğmesinin kilit sebebi de aynı. | canlıda şubesiz MÜDÜR ile: `sonra6/foto/hurda__PRO_aydinlik_960.png` · `SADE-1-hurda-960.png` |
| 25 | /kur | Günlük kur ekranının tepesinde hukuki "sözleşmesiz — yalnız pilot…" paragrafı. | Tepe: tek satır rozet "Deneme kur kaynağı" (`#kur-sozlesmesiz`, tam metin `title`'da). Tam hukuki metin SİLİNMEDİ: PRO "Kaynak" sekmesindeki kaynak seçme kartında `#kur-sozlesmesiz-ayrinti` (kaynağı seçen SAHİP/MÜDÜR'ün ekranı; uç `[RolGerekli("SAHIP","MUDUR")]`). | `kur__BASIT_aydinlik_1440.png` (sonuc3 → sonra2) · `SADE-1-kur-1440.png` |
| 26 | /pos/tahsilatlar | Takvim kartının boş durumu ile başlıksız defterin boş durumu aynı metin/ikon; ayrıca istek düşse de defter boş durumu çiziliyordu (UX-S28 ihlali). | Takvim: "Bu tarihlerde bankanın yatıracağı bir ödeme yok."; defter kendi başlıklı kartında ("Kartlı tahsilatlar"); istek düşerse boş durum ÇİZİLMEZ (bant yukarıda). | `pos_tahsilatlar__BASIT_aydinlik_1440.png` · `SADE-1-pos-tahsilatlar-1440.png` |
| 27 | /banka | Tablo altında 4 ayrı DegismezIpucu kutusu. | Tek kısa satır "Kaydedilen banka hareketleri değiştirilemez. Nasıl düzeltileceğini görmek için tıklayın." (`<details id="bh-degismez-ayrinti">`); 4 kutunun hepsi (gerekçe + düzeltme yolu, UX-S25) içeride AYNEN, id'ler aynı; açılınca kendi kutusunda kayar. | canlı veriyle (hesap + 3 dekont): `sonra8/foto/banka__PRO_aydinlik_960.png` · `SADE-1-banka-960.png` |
| 31 | /kur | Ayar hücresi amber rozet (tıklanır hap gibi). | Düz metin (`hucre-vurgu`). | `SADE-1-kur-1440.png` |
| 38 | /yetkiler | GET /yetkiler 403 olunca bandın altında tek satırlık (Sahip) tablo + konu dışı "tabloyu kaydırın" + aynı şeyi tekrar eden kilit kutusu. | Matris okunamadıysa YALNIZ bant (`matrisOkunamadi`); tablo, kaydırma ipucu ve kilit kutusu çizilmez. Okununca her şey eskisi gibi. | canlıda şubesiz MÜDÜR (403): `sonra6/foto/yetkiler__PRO_karanlik_375.png` · `SADE-1-yetkiler-375.png` |
| 41 | /ayarlar | Görünüm okuma hatası yalnız Secim'in gri "kilit sebebi" satırıydı; `GorunumServisi.YukleAsync` yalnız açılışta çağrılıyor, 429 vb. sonrası hiç yeniden denenmiyordu. | Sahipte okuma hatası = hata bandı (`#gorunum-okuma-hata`, role=alert) + GERÇEK istek atan "Yeniden dene" (GET /gorunum). "Benim görünümüm" kartında da aynısı (`#benim-okuma-hata`). Rol kilidi (hata değil) gri satır olarak kaldı. GorunumKarti bileşenine (KATMAN) dokunulmadı. | Tarayıcıda hata durumu üretilemedi (canlı GET /gorunum 200) → kanıt bUnit + mutasyon M1-13. |
| 42 | /ekranlar | Ölçüldü: karo ikonları zaten `var(--vurgu)` renkli ince çizgi (tuvaldeki gibi); favori bandı 429 kaynaklı GERÇEK hata (UX-S28 gereği gizlenmez). | **Değişiklik yok (gerekçeli).** Tuvalle bölge farkı yalnız ikon seçimi (Panel=ev/ızgara, Satış=kart/zincir) — ikonlar `MenuTanimi`'nden gelir, SADE-1 dosya alanı dışında → açık kalan. | `SADE-1-ekranlar-tuval-1440.png` |
| 63 | /pos | Açıklama "Resmî tatil takvimi YOKTUR: valör günü…". | BASİT: "Kartla alınan para bankadan birkaç gün içinde hesaba geçer; resmî tatillerde gecikebilir." PRO: özgün ayrıntı metni AYNEN. | canlı veriyle (2 POS cihazı): `sonra9/foto/pos__BASIT_aydinlik_1440.png` · `SADE-1-pos-1440.png` |
| 64 | /pos | "Kullanımdan kaldır" (geri alınabilir: Aktifleştir) her satırda dolu kırmızı. | Satırda nötr ikincil düğme; onay modalındaki onay düğmesi birincil (kırmızı yalnız geri alınamaz işlemde). PATCH yolu ve onay modalı DEĞİŞMEDİ. | `SADE-1-pos-1440.png` |
| 73 | /sayim | "IPTAL" durumu `Dil.T.Sayim.Iptal` = "İptal Et" (düğme metni) ile basılıyordu. | Durum sütunu rozet: AÇIK / KAPANDI (hub metni) / "İptal edildi" (yeni). | canlı veriyle (1 açık + 1 iptal): `sonra5/foto/sayim__PRO_aydinlik_960.png` · `SADE-1-sayim-960.png` |

Ek (ölçümde görülen, bulgu listesinde olmayan ama aynı kök nedenli): /kurulum, /veri-disa-aktarim, /hurda kartlarında gövde öğeleri `.kart-govde` sarmalı olmadan karta yapışıktı; kurulumda sağdaki sayı kırpılıyordu → yalnız bu üç sayfada (`:has(> #sayfa-basi-…)` kapsamı) yatay pay.

### Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
VeriDisaAktarim.razor · Yedekler.razor · Kurulum.razor · Hurda.razor · Kur.razor · Pos.razor · PosTahsilatlar.razor · Banka.razor · Yetkiler.razor · Ayarlar.razor (GorunumKarti HARİÇ) · Sayim.razor · Kilavuz.razor. Dokunulmayan: Ekranlar.razor, Fiyatlandirma.razor (bulgu yok / gerekçeli).

## 2. Sapmalar ve gerekçeler
1. **index.html sırası:** görev "sade.css ortak.css'ten SONRA" diyordu; ORTAK-1'in testi `OrtakGorunumSaflik.O1_index_ortak_cssi_EN_SONDA_yukler` ortak.css'in EN SON olmasını zorunlu kılıyor (ilk tam süitte bu test benim sıram yüzünden kırmızıydı). Başka şeridin testine dokunmamak için sade.css **katman.css'ten sonra, ortak.css'ten önce** yüklenir. sade.css seçicileri id / `:has` kapsamlıdır; ortak.css ile ortak seçici yok (grep ile doğrulandı) → sıra sonucu değiştirmez. Orkestratöre not.
2. **UX4dEkranTestleri.M24** (mevcut test) güncellendi: artık "sözleşmesiz" metnini rozetin `title`'ında ve "Deneme" rozetini arar — kurucu kararı (#25: "metni silme, günlük ekranda tek satır rozet") gereği davranış bilerek değişti.
3. **#24** için sunucu kapısı hata DEĞİL (şubesiz kullanıcı tasarım gereği reddedilir) → sunucu değişmedi; ekran kilitlendi ve sebebi yazıldı (görevdeki NOT'un ikinci dalı).
4. **#42** değişiklik yapılmadı (yukarıda gerekçe).
5. **Sayım AÇIK/KAPANDI** metinleri hub'daki büyük harfli haliyle kaldı (P22 testi "AÇIK" bekliyor, `OrdinalIgnoreCase` Türkçe ı/İ'yi eşlemiyor); yalnız IPTAL düzeltildi (bulgunun kendisi).
6. Mutasyonlar `perl -0777 -pi` ile, betik (`scratchpad/mut.py`) içinden ENV ile tırnaklanmış TEK eşleşme şartıyla uygulandı (eşleşme ≠ 1 ise durur).

## 3. Migration
Gerekmedi (şema değişikliği yok; API'ye dokunulmadı).

## 4. Mevzuat
Mevzuat hükmü yazılmadı/değiştirilmedi. #25 kur kaynağı "sözleşmesiz" metni hukuki bir iç uyarıdır; metin değiştirilmedi, yalnız YERİ (günlük ekran → kaynak seçim kartı + rozet ipucu) değişti.

## 5. Testler
Yeni: `Kuyumcu.Tests/SADE1EkranTestleri.cs` (20 test, kendi sahte API'si: tanımsız uç = 404/ApiHata, "{}" ile sessiz başarı YOK) + `BankaEkranTestleri.SADE1_Degismez_kutulari_TEK_ozet_satirinin_arkasinda_katli_metinler_silinmedi` (1) = **21 yeni test**. Güncellenen: `UX4dEkranTestleri.M24_…` (1).

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Filtreli SADE1 | 20 | 20 | 0 | ilk koşum |
| Filtreli ilgili ekran testleri (25 sınıf deseni) | 600 | 600 | 0 | Banka, Kurulum, P20/P20b, UX4d, UX1C4, Yedek, Pos, P22, Sayim, UX4c, Kur, Q102, UX1C2, P12, Q105, TK3Ayarlar, BasitModPos, Q91, Kilavuz, Ekranlar, Hurda, Fiyatlandirma, Ayarlar, SADE1 |
| Tam süit #1 (11:22) | 6097 | 6096 | 1 | `OrtakGorunumSaflik.O1_index_ortak_cssi_EN_SONDA_yukler` — BENİM css sıram; düzeltildi, yeniden koşuldu: 58/58 yeşil |
| Tam süit #2 (son, taze build 0 uyarı 0 hata) | 6103 | 6077 | 26 | 26'nın HİÇBİRİ SADE-1 dosyası değil (aşağıda sahipleriyle); SADE-1'e ait sınıfların hepsi yeşil: SADE1 20/20, Banka 12/12, UX4d 16/16, P22 5/5, Kurulum 23/23, P20 14/14, Pos 6/6, Yedek 18/18, UX1C4 29/29, OrtakGorunumSaflik 8/8 |

trx: `scratchpad/trx-pSADE-1/` ve `scratchpad/trx-pSADE-1-son/`.

**Tur 2 koşumları** (yeni test: +3 → SADE1EkranTestleri 23; paket toplamı 24 yeni test; güncellenen: UX4d.M24):
| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Filtreli (SADE1, UX4d, Kur*, S3eKurKaynagi, Ayarlar*, Kilavuz*, TK3) — ilk | 179 | 179 | 0 | SADE1 23/23 |
| Tam süit (ara; başka şeritlerin yarım işiyle aynı anda) | 6169 | 6061 | 108 | 1'i BENİM: GE4 (dürüstlük notu) → düzeltildi. Kalanlar BICIM-2'nin sayı girişi biçimi ("6800"→"6.800", "0.5"→"0,500": `Bilesenler/Form/SayisalAlan.cs` 13:12, `Para.razor`, `Bicim.cs`) + CUMA-S satış ekranları |
| Filtreli (SADE1, Gorunum*, UX4d, Kur*, Ayarlar*, Kilavuz*, TK3, S1fGorunum, KullaniciGorunum) — son kod | 254 | 253 | 1 | `KurEkranTestleri.Manuel_form_mevcut_degerlerle_acilir` ("6800" ≠ "6.800" — BICIM-2 biçimi; Kur.razor'ın manuel formuna dokunulmadı); son tam süitte YEŞİL |
| **Tam süit (son, `--no-incremental` derleme sonrası)** | **6189** | **6187** | **2** | `KatmanTekKalipTestleri.K1_15_…`, `K1_17_…` — KATMAN (js/katman.js). SADE-1'e ait sınıfların hepsi yeşil. trx: `scratchpad/trx-pSADE-1-tur2b/` |

→ Madde (7): devir notundaki kırmızı listesi (18: SATIS2/UX1C1/KatalogSonSatis/TS1/S21/P39) ile karşılaştırıldı; son tam süitteki 2 kırmızı KATMAN'ın; **SADE-1'in ürettiği yeni kırmızı 0.**

**Tam süit #2'deki başkasına ait kırmızılar (#1'de YOKTU → aradaki sürede başka şeritlerin yarım işi):**
- CUMA-S (Pages/Islemler*, Bilesenler/Fis/*): UX1C1EkranTestleri.Pos_satirdan_IADE_ET… (`#islem-iade-et-h1` yok), UX1C1.Pos_kaydedilen_satislar…, UX1C1.Pos_storno_dugmesi…, UX1C1.P23_kapsam_bayragi…, UX1C1.P23_kirilim_kalemleri…, UX1C1.P23_odeme_kirilimi…, TS1TuvalSatisTestleri ×3, TS2TuvalProTestleri ×4, S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz, S22TezgahFisiTestleri.Pro_modda_izgara_YOK…, KatalogSonSatisTestleri ×2, CumaSAldimSadeTestleri.CUMA_S_04…
- ISTEK-1 (ApiIstemci/ListeOnbellegi/Urunler): Istek1OnbellekTestleri ×5, Istek1EkranTestleri.ISTEK1_25…
- ORTAK-1 (StatKart): OrtakGorunumStatKartTestleri.S2…, S3…

→ **Madde (7) sağlandı:** bu paketin ürettiği tek kırmızı (O1) giderildi; kalan fark başka şeritlerin.

## 6. Mutasyon kanıtları

**Tur 2** (aynı yöntem, `scratchpad/mut.py`, yedek `scratchpad/mut-yedek/SADE-1/`):
| Kod | Dosya | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|---|
| M1-1 (yeniden) | VeriDisaAktarim.razor | çift başlığı GERİ KOY | SADE1.Veri_disa_aktarim_basligi_TEK_kez_yazilir_sayfada_h1_yok | aynı |
| M1-2 (yeniden) | Kurulum.razor | yükleme hatasını YUT | SADE1.Kurulum_sunucu_hatasinda_SONSUZ_yukleniyor_yok_bant_ve_yeniden_dene_var | aynı |
| M1-3 (yeniden, tekil) | Pos.razor | birincil düğme sınıfını geri al | SADE1.Pos_kullanimdan_kaldir_GERI_ALINABILIR_tehlike_rengi_tasimaz_onay_birincil | aynı |
| M1-16 | Kur.razor | `@if (SahipMi)` → `@if (true)` | SADE1.Kur_hukuki_tam_metin_ROLE_bagli_PRO_modda_TEZGAHTAR_gormez_sebep_yazili | aynı |
| M1-18 | Kur.razor | rozet title'ı herkese tam metin | SADE1.Kur_hukuki_tam_metin_ROLE_bagli_PRO_modda_TEZGAHTAR_gormez_sebep_yazili | aynı |
| M1-13 (güncel koşul) | Ayarlar.razor | bant koşulu `@if (false)` | SADE1.Ayarlar_gorunum_OKUNURKEN_…, SADE1.Ayarlar_gorunum_okunamadiysa_HATA_BANDI_… | aynı |
| M1-17 | Ayarlar.razor | `Gorunum.OkumaDenendi &&` kaldır (eski koşul) | SADE1.Ayarlar_gorunum_OKUNURKEN_hata_bandi_YANIP_SONMEZ_bitince_basari_yoksa_cikar | aynı |
| M1-19 | GorunumServisi.cs | `OkumaDenendi = true` kaldır | SADE1.Ayarlar_gorunum_OKUNURKEN_…, SADE1.Ayarlar_gorunum_okunamadiysa_HATA_BANDI_… | aynı |
| M1-20 | GorunumServisi.cs | başarısızlıkta `Degisti` bildirimini kaldır | SADE1.Ayarlar_gorunum_OKUNURKEN_hata_bandi_YANIP_SONMEZ_bitince_basari_yoksa_cikar | aynı |
Son mutasyondan sonra kalıntı grep'i (`@if (false)`, `@if (true)` Kur/Ayarlar: 0; `OkumaDenendi = true`: 1) ve TEMİZ `--no-incremental` derleme: 0 hata, 1 uyarı (ORTAK-1'in xUnit1031).

**Tur 1:** (her biri: kaynak yedek → tek eşleşmeli değişiklik → build → filtreli test → yedek geri → `cmp` birebir)

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|---|
| M1-1 | VeriDisaAktarim.razor | çift başlık bloğunu GERİ KOY | SADE1EkranTestleri.Veri_disa_aktarim_basligi_TEK_kez_yazilir_sayfada_h1_yok | aynı |
| M1-2 | Kurulum.razor | yükleme hatasını YUT (`catch (ApiHata) { }`) | SADE1EkranTestleri.Kurulum_sunucu_hatasinda_SONSUZ_yukleniyor_yok_bant_ve_yeniden_dene_var | aynı |
| M1-3 | Pos.razor | birincil düğme sınıfını GERİ AL (`pos-pasif-onayla` → dugme-tehlike) | SADE1EkranTestleri.Pos_kullanimdan_kaldir_GERI_ALINABILIR_tehlike_rengi_tasimaz_onay_birincil | aynı |
| M1-4 | Kurulum.razor | ağ hatasını yut (`catch (HttpRequestException) { }`) | SADE1EkranTestleri.Kurulum_AG_hatasinda_da_bant_cikar_yukleniyor_kalmaz | aynı |
| M1-5 | Hurda.razor | form kilidi `disabled="@(false)"` | SADE1EkranTestleri.Hurda_sube_baglami_yoksa_form_KILITLI_sebep_yazili_bakiye_karti_bos_degil | aynı |
| M1-6 | Kur.razor | ayar hücresine rozeti geri koy | SADE1EkranTestleri.Kur_ayar_hucresi_HAP_degil_duz_metin | aynı |
| M1-7 | Kur.razor | rozet yerine hukuki paragraf | SADE1EkranTestleri.Kur_sozlesmesiz_kaynak_gunluk_ekranda_TEK_satir_rozet_hukuki_paragraf_yok + UX4dEkranTestleri.M24_Sozlesmesiz_kaynak_seciliyken_PILOT_uyarisi_gorunur | aynı |
| M1-8 | Yetkiler.razor | `matrisOkunamadi = true` kaldır | SADE1EkranTestleri.Yetkiler_okunamazsa_YALNIZ_bant_bos_tablo_ve_tekrar_kutusu_yok | aynı |
| M1-9 | Banka.razor | katlanı `open` ile aç | BankaEkranTestleri.SADE1_Degismez_kutulari_TEK_ozet_satirinin_arkasinda_katli_metinler_silinmedi | aynı |
| M1-10 | Sayim.razor | IPTAL → "İptal Et" | SADE1EkranTestleri.Sayim_IPTAL_durumu_EYLEM_metniyle_basilmaz | aynı |
| M1-11 | Kilavuz.razor | katlanı hep açık | SADE1EkranTestleri.Kilavuz_BASIT_uzun_ekran_listesi_KATLI_bolumler_kart_izgarasi | aynı |
| M1-12 | PosTahsilatlar.razor | takvim boş metnini defterinkine eşitle | SADE1EkranTestleri.Pos_tahsilat_takvim_ve_defter_bos_durumlari_FARKLI_ve_basliklidir | aynı |
| M1-13 | Ayarlar.razor | okuma hatası bandını kapat (`@if (false)`) | SADE1EkranTestleri.Ayarlar_gorunum_okunamadiysa_HATA_BANDI_ve_yeniden_dene_gercek_istek | aynı |
| M1-14 | Pos.razor | BASİT açıklamayı jargonlu metne döndür | SADE1EkranTestleri.Pos_BASIT_aciklama_jargonsuz_PRO_ayrinti_korunur | aynı |
| M1-15 | Hurda.razor | "okunamadı" satırını kapat | SADE1EkranTestleri.Hurda_sube_baglami_yoksa_form_KILITLI_sebep_yazili_bakiye_karti_bos_degil | aynı |

**Dürüstlük notu (Anayasa md.1):** ilk toplu koşumda M1-11 ve M1-13 başka şeridin eşzamanlı derlemesiyle çakışıp DERLEME-HATASI verdi; M1-12 ise bayat dll ile yanlış testi (Kılavuz) kırdı. Kök neden: `shutil.copy2` yedeği ESKİ mtime ile geri koyuyordu → sonraki artımlı derleme bazen değişikliği görmüyordu. Betik düzeltildi (geri yüklemeden sonra `os.utime`), üçü tek tek yeniden koşuldu ve yukarıdaki doğru testler kırmızıya döndü (M1-11 ikinci denemede de bayat derlemeyle kırmızı vermedi, üçüncü tekil koşumda kırmızı). Son mutasyondan sonra tüm SADE-1 razor dosyaları `touch` edildi, **temiz build 0 uyarı 0 hata**, kalıntı grep'i 0 (`open="@(true)"`, `@if (false)`, `catch (ApiHata) { }`, `disabled="@(false)"`, `bh-degismez-ayrinti" open` → hepsi 0), ilgili 64 test yeşil.

## 7. denetim.sh

**Tur 2 (son kod):** `bash denetim.sh` → **DENETİM TEMİZ** (rc=0). SADE-1 sayaçları: satır içi style 105 (tavan 196) · ModalPencere 48 (tavan 55) · @onclick'siz düğme 58 (tavan 71) · P-18 SABIT_RENK 0 · HAM_SELECT 29 · IgnoreQueryFilters 0 · Hareketler.Add 1 · ToListAsync (Modules) 145 (tavan 145). (Ara koşumdaki tek ✗ P-23 ODEME_KALEMSIZ_YOL 3 — SQL ile kiracısı bulundu: `PAKETSATIS-2-TEST Kuyumculuk`; son koşumda kalkmış.)

**Tur 1:**
Son koşum (bash): `DENETİM BAŞARISIZ` — 2 ihlal, ikisi de SADE-1'in DEĞİL:
- `Sınırsız ToListAsync (Modules · B-8) 146 (tavan 145)` — Kuyumcu.Api/Modules; bu paket API'ye dokunmadı.
- `P-23: ODEME_KALEMSIZ_YOL 3` — canlı DB ölçümü; 3 satırın kiracısı SQL ile bulundu: `PAKETSATIS-2-TEST Kuyumculuk` (SATIS/KARMA, 08:19) — SATIS-2 şeridi.
(Ara koşumda görülen record tavanı/OLCUM_TABANI ihlalleri KarZarar/Urunler/Siparisler razor record'larından — ORTAK-1/ISTEK-1; son koşumda kalkmışlar.)
SADE-1 sayaçları ✓: satır içi style 105 (tavan 196), ModalPencere 48 (tavan 55, artış 0), P-18 SABIT_RENK 0, HAM_SELECT 29 (artış 0), @onclick'siz düğme 58 (tavan 71), IgnoreQueryFilters 0, Hareketler.Add 1.

## 8. Canlı doğrulama (API 5170, kendi kiracım `PAKETSADE-1-TEST Kuyumculuk`, demo-tohumla.js ile ürün uçlarından tohumlandı: 6 müşteri, 12 ürün, 9 satış; + 1 banka hesabı 3 dekont, 2 POS cihazı, 2 sayım oturumu, 1 şubesiz MÜDÜR)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris (SAHİP) | 200 | 200 (rol SAHIP, şube var) |
| GET /kurulum/durum | 200, adımlar | 200; ISLETME/KULLANICILAR/KUR/KASA_BANKA bitti, CARI_ACILIS/STOK_ACILIS eksik |
| GET /hurda/bakiye (SAHİP) | 200 | 200, satır 0 → ekranda boş durum |
| GET /hurda/bakiye (şubesiz MÜDÜR) | 400 SubeGerekli | 400 "Hurda işlemleri için şube bağlamı gerekli." → ekranda form kilitli + sebep |
| GET /yetkiler (MÜDÜR) | 403 | 403 "Bu işlem için gerekli rol: SAHIP" → ekranda yalnız bant |
| GET /kur/kaynaklar | 200 | 200, seçili ALTINKAYNAK, seciliSozlesmesiz=true → rozet |
| GET /pos/tahsilatlar, /pos/valor-takvimi | 200 | 200 / 200 (0 satır) → iki farklı boş durum |
| POST /sayim ×2 + POST /sayim/{id}/iptal | 200 | 200/200/200 → liste [ACIK, IPTAL] → ekranda "AÇIK" / "İptal edildi" |
| POST /bankalar + 3× /hareket | 200 | 200 ×4 |
| POST /pos/cihazlar ×2 | 200 | 200 ×2 |
| GET /gorunum, /disa-aktarim/kaynaklar | 200 | 200 / 200 (12 kaynak) |

### Tarayıcı ölçümü (başsız Chrome, `betikler/tur` kopyası, tarayıcı bölmesi KULLANILMADI)
14 rota × {1440, 1280, 960, 375} × {BASİT, PRO} × {aydınlık, karanlık} = 224 sayfa + sekmeler + açıcılar; ek MÜDÜR koşumu (/hurda, /yetkiler) 32 sayfa. Son hâl kaynakları: sonra2 (tümü) · sonra3 (/kurulum son css) · sonra4 (/kilavuz son css) · sonra5 (/sayim veriyle) · sonra8 (/banka veriyle, son css) · sonra9 (/pos veriyle) · sonra6 (MÜDÜR).
**Toplam (256 sayfa satırı): K1 gizli-görünen 0 · K5 'TRY' 0 · K9 binen 0 · K10 kesik 0 · K12 ezik 0 · K13 kırpılan 0 · yatay kaydırma 0 · K3 16** — K3'ün tamamı /ayarlar 960'ta `GorunumKarti` (`span.gk-sema-ad "Fildişi (açık menü)"`, "Bilgisayara uy") — KATMAN'ın bileşeni, ÖNCE ölçümünde de vardı (sonuc3: Ba960,Bk960,Pa960,Pk960).
**analiz.py (katman/açıcı dahil) SONRA yeni bulgu:** 0. ÖNCE'de de olan ve SADE-1'in üretmediği: /kurulum 375 K4 "tren" (ÖNCE 2.3–2.6×4, SONRA 2.4–2.7×4; sayfa gerçekten uzun — açılış formu + mühür), /ayarlar 375 K4 tren ve 960 K3 (GorunumKarti), /ayarlar?sekme=guvenlik K4 (ÖNCE MÜDÜR kullanıcısıyla ölçüldü; SONRA SAHİP → MFA + destek bölümleri ek; SADE-1 Güvenlik sekmesine dokunmadı).
Ara ölçümde benim ürettiğim ve GİDERİLEN bulgular: /kilavuz açılan liste ekran dışı (→ 40vh kaydırılır kutu), /kilavuz 1280 ve 375 "kısa etiket kırılıyor" (→ min 15rem sütun, 375'te tek sütun), /banka açılan ayrıntı ekran dışı (→ 50vh kaydırılır gövde), /kurulum 960 PRO K4 (→ adım satırı sıkılaştırıldı). Son koşumlarda 0.

Ekran görüntüleri Read ile GÖRÜLDÜ (yan yana, `ham/TUVAL-KARSILASTIRMA/`): SADE-1-veri-disa-aktarim-1440 · -kurulum-1440 · -kurulum-375 · -hurda-960 · -kur-1440 · -pos-tahsilatlar-1440 · -banka-960 · -yetkiler-375 · -pos-1440 · -sayim-960 · -kilavuz-1440 · -yedekler-1440 · -ekranlar-tuval-1440 (+ -ayarlar-tuval-1440 üretildi).

**Ölçüm aracının bilinen sınırı:** aynı kullanıcıyla aydınlık+karanlık işçiler aynı anda koşunca kullanıcı tema tercihi sunucuda yarışıyor; bazı "aydinlik" etiketli fotoğraflar karanlık çıktı (ÖNCE sonuc3'te de aynı). Bu yüzden her iki tema da fiilen ölçüldü ama etiket–görüntü eşleşmesi birkaç karede kayık olabilir.

### Tuval karşılaştırması (yalnız tuvali olan ekranlar)
| Ekran | Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|---|
| /ekranlar | üst şerit, arama, sayaç ipucu | var | var | AYNI |
| /ekranlar | grup başlıkları + 8 sütun karo ızgara | var | var | AYNI |
| /ekranlar | karo ikonları | Panel=ev, Satış=kart | Panel=ızgara, Satış=zincir | GEREKÇELİ FARK — ikonlar MenuTanimi'nden, SADE-1 alanı dışında (açık kalan) |
| /ekranlar | favori hata bandı | yok | yalnız istek hatasında | GEREKÇELİ FARK — UX-S28 |
| /ayarlar | sekme şeridi üst barda, profil/görünüm kartları | var | var | AYNI (T-K3 düzeni; SADE-1 yalnız hata bandı ekledi, yalnız hata anında görünür) |
Diğer 12 rotanın tuvali yok; ölçüt: makul yığın, yatay kaydırma 0, K1..K13 yeni bulgu 0 (sağlandı).

### Ölçüm tablosu (rota × genişlik × mod × tema)
| rota | gen. | mod | tema | K1 gizli | K3 taşan/kırık | K4 boy×bölüm | K5 TRY | K9 binen | K10 kesik | K12 ezik | K13 kırp | yatay | kaynak/foto |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /veri-disa-aktarim | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_aydinlik_1440.png |
| /veri-disa-aktarim | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_karanlik_1440.png |
| /veri-disa-aktarim | 1440 | PRO | aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_aydinlik_1440.png |
| /veri-disa-aktarim | 1440 | PRO | karanlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_karanlik_1440.png |
| /veri-disa-aktarim | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_aydinlik_1280.png |
| /veri-disa-aktarim | 1280 | BASIT | karanlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_karanlik_1280.png |
| /veri-disa-aktarim | 1280 | PRO | aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_aydinlik_1280.png |
| /veri-disa-aktarim | 1280 | PRO | karanlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_karanlik_1280.png |
| /veri-disa-aktarim | 960 | BASIT | aydinlik | 0 | 0/0 | 1.6×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_aydinlik_960.png |
| /veri-disa-aktarim | 960 | BASIT | karanlik | 0 | 0/0 | 1.6×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_karanlik_960.png |
| /veri-disa-aktarim | 960 | PRO | aydinlik | 0 | 0/0 | 1.5×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_aydinlik_960.png |
| /veri-disa-aktarim | 960 | PRO | karanlik | 0 | 0/0 | 1.5×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_karanlik_960.png |
| /veri-disa-aktarim | 375 | BASIT | aydinlik | 0 | 0/0 | 2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_aydinlik_375.png |
| /veri-disa-aktarim | 375 | BASIT | karanlik | 0 | 0/0 | 2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__BASIT_karanlik_375.png |
| /veri-disa-aktarim | 375 | PRO | aydinlik | 0 | 0/0 | 2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_aydinlik_375.png |
| /veri-disa-aktarim | 375 | PRO | karanlik | 0 | 0/0 | 2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/veri_disa_aktarim__PRO_karanlik_375.png |
| /yedekler | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_aydinlik_1440.png |
| /yedekler | 1440 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_karanlik_1440.png |
| /yedekler | 1440 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_aydinlik_1440.png |
| /yedekler | 1440 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_karanlik_1440.png |
| /yedekler | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_aydinlik_1280.png |
| /yedekler | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_karanlik_1280.png |
| /yedekler | 1280 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_aydinlik_1280.png |
| /yedekler | 1280 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_karanlik_1280.png |
| /yedekler | 960 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_aydinlik_960.png |
| /yedekler | 960 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_karanlik_960.png |
| /yedekler | 960 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_aydinlik_960.png |
| /yedekler | 960 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_karanlik_960.png |
| /yedekler | 375 | BASIT | aydinlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_aydinlik_375.png |
| /yedekler | 375 | BASIT | karanlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__BASIT_karanlik_375.png |
| /yedekler | 375 | PRO | aydinlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_aydinlik_375.png |
| /yedekler | 375 | PRO | karanlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yedekler__PRO_karanlik_375.png |
| /kurulum | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_aydinlik_1440.png |
| /kurulum | 1440 | BASIT | karanlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_karanlik_1440.png |
| /kurulum | 1440 | PRO | aydinlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_aydinlik_1440.png |
| /kurulum | 1440 | PRO | karanlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_karanlik_1440.png |
| /kurulum | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.9×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_aydinlik_1280.png |
| /kurulum | 1280 | BASIT | karanlik | 0 | 0/0 | 1.9×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_karanlik_1280.png |
| /kurulum | 1280 | PRO | aydinlik | 0 | 0/0 | 1.9×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_aydinlik_1280.png |
| /kurulum | 1280 | PRO | karanlik | 0 | 0/0 | 1.9×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_karanlik_1280.png |
| /kurulum | 960 | BASIT | aydinlik | 0 | 0/0 | 2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_aydinlik_960.png |
| /kurulum | 960 | BASIT | karanlik | 0 | 0/0 | 2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_karanlik_960.png |
| /kurulum | 960 | PRO | aydinlik | 0 | 0/0 | 2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_aydinlik_960.png |
| /kurulum | 960 | PRO | karanlik | 0 | 0/0 | 2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_karanlik_960.png |
| /kurulum | 375 | BASIT | aydinlik | 0 | 0/0 | 2.4×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_aydinlik_375.png |
| /kurulum | 375 | BASIT | karanlik | 0 | 0/0 | 2.4×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__BASIT_karanlik_375.png |
| /kurulum | 375 | PRO | aydinlik | 0 | 0/0 | 2.6×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_aydinlik_375.png |
| /kurulum | 375 | PRO | karanlik | 0 | 0/0 | 2.6×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra3/kurulum__PRO_karanlik_375.png |
| /kilavuz | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_aydinlik_1440.png |
| /kilavuz | 1440 | BASIT | karanlik | 0 | 0/0 | 1×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_karanlik_1440.png |
| /kilavuz | 1440 | PRO | aydinlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_aydinlik_1440.png |
| /kilavuz | 1440 | PRO | karanlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_karanlik_1440.png |
| /kilavuz | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.5×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_aydinlik_1280.png |
| /kilavuz | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_karanlik_1280.png |
| /kilavuz | 1280 | PRO | aydinlik | 0 | 0/0 | 1×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_aydinlik_1280.png |
| /kilavuz | 1280 | PRO | karanlik | 0 | 0/0 | 1×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_karanlik_1280.png |
| /kilavuz | 960 | BASIT | aydinlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_aydinlik_960.png |
| /kilavuz | 960 | BASIT | karanlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_karanlik_960.png |
| /kilavuz | 960 | PRO | aydinlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_aydinlik_960.png |
| /kilavuz | 960 | PRO | karanlik | 0 | 0/0 | 1.2×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_karanlik_960.png |
| /kilavuz | 375 | BASIT | aydinlik | 0 | 0/0 | 1.6×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_aydinlik_375.png |
| /kilavuz | 375 | BASIT | karanlik | 0 | 0/0 | 1.6×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__BASIT_karanlik_375.png |
| /kilavuz | 375 | PRO | aydinlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_aydinlik_375.png |
| /kilavuz | 375 | PRO | karanlik | 0 | 0/0 | 1.7×4 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra4/kilavuz__PRO_karanlik_375.png |
| /hurda | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.8×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_aydinlik_1440.png |
| /hurda | 1440 | BASIT | karanlik | 0 | 0/0 | 1.8×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_karanlik_1440.png |
| /hurda | 1440 | PRO | aydinlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_aydinlik_1440.png |
| /hurda | 1440 | PRO | karanlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_karanlik_1440.png |
| /hurda | 1280 | BASIT | aydinlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_aydinlik_1280.png |
| /hurda | 1280 | BASIT | karanlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_karanlik_1280.png |
| /hurda | 1280 | PRO | aydinlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_aydinlik_1280.png |
| /hurda | 1280 | PRO | karanlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_karanlik_1280.png |
| /hurda | 960 | BASIT | aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_aydinlik_960.png |
| /hurda | 960 | BASIT | karanlik | 0 | 0/0 | 2.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_karanlik_960.png |
| /hurda | 960 | PRO | aydinlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_aydinlik_960.png |
| /hurda | 960 | PRO | karanlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_karanlik_960.png |
| /hurda | 375 | BASIT | aydinlik | 0 | 0/0 | 2.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_aydinlik_375.png |
| /hurda | 375 | BASIT | karanlik | 0 | 0/0 | 2.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__BASIT_karanlik_375.png |
| /hurda | 375 | PRO | aydinlik | 0 | 0/0 | 2.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_aydinlik_375.png |
| /hurda | 375 | PRO | karanlik | 0 | 0/0 | 2.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/hurda__PRO_karanlik_375.png |
| /hurda (MÜDÜR, şubesiz) | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_aydinlik_1440.png |
| /hurda (MÜDÜR, şubesiz) | 1440 | BASIT | karanlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_karanlik_1440.png |
| /hurda (MÜDÜR, şubesiz) | 1440 | PRO | aydinlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_aydinlik_1440.png |
| /hurda (MÜDÜR, şubesiz) | 1440 | PRO | karanlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_karanlik_1440.png |
| /hurda (MÜDÜR, şubesiz) | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_aydinlik_1280.png |
| /hurda (MÜDÜR, şubesiz) | 1280 | BASIT | karanlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_karanlik_1280.png |
| /hurda (MÜDÜR, şubesiz) | 1280 | PRO | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_aydinlik_1280.png |
| /hurda (MÜDÜR, şubesiz) | 1280 | PRO | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_karanlik_1280.png |
| /hurda (MÜDÜR, şubesiz) | 960 | BASIT | aydinlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_aydinlik_960.png |
| /hurda (MÜDÜR, şubesiz) | 960 | BASIT | karanlik | 0 | 0/0 | 1.7×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_karanlik_960.png |
| /hurda (MÜDÜR, şubesiz) | 960 | PRO | aydinlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_aydinlik_960.png |
| /hurda (MÜDÜR, şubesiz) | 960 | PRO | karanlik | 0 | 0/0 | 1.6×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_karanlik_960.png |
| /hurda (MÜDÜR, şubesiz) | 375 | BASIT | aydinlik | 0 | 0/0 | 1.9×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_aydinlik_375.png |
| /hurda (MÜDÜR, şubesiz) | 375 | BASIT | karanlik | 0 | 0/0 | 1.9×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__BASIT_karanlik_375.png |
| /hurda (MÜDÜR, şubesiz) | 375 | PRO | aydinlik | 0 | 0/0 | 1.9×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_aydinlik_375.png |
| /hurda (MÜDÜR, şubesiz) | 375 | PRO | karanlik | 0 | 0/0 | 1.9×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/hurda__PRO_karanlik_375.png |
| /kur | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_aydinlik_1440.png |
| /kur | 1440 | BASIT | karanlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_karanlik_1440.png |
| /kur | 1440 | PRO | aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_aydinlik_1440.png |
| /kur | 1440 | PRO | karanlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_karanlik_1440.png |
| /kur | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_aydinlik_1280.png |
| /kur | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_karanlik_1280.png |
| /kur | 1280 | PRO | aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_aydinlik_1280.png |
| /kur | 1280 | PRO | karanlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_karanlik_1280.png |
| /kur | 960 | BASIT | aydinlik | 0 | 0/0 | 1.3×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_aydinlik_960.png |
| /kur | 960 | BASIT | karanlik | 0 | 0/0 | 1.3×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_karanlik_960.png |
| /kur | 960 | PRO | aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_aydinlik_960.png |
| /kur | 960 | PRO | karanlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_karanlik_960.png |
| /kur | 375 | BASIT | aydinlik | 0 | 0/0 | 1.4×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_aydinlik_375.png |
| /kur | 375 | BASIT | karanlik | 0 | 0/0 | 1.4×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__BASIT_karanlik_375.png |
| /kur | 375 | PRO | aydinlik | 0 | 0/0 | 1.5×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_aydinlik_375.png |
| /kur | 375 | PRO | karanlik | 0 | 0/0 | 1.5×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/kur__PRO_karanlik_375.png |
| /pos | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_aydinlik_1440.png |
| /pos | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_karanlik_1440.png |
| /pos | 1440 | PRO | aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_aydinlik_1440.png |
| /pos | 1440 | PRO | karanlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_karanlik_1440.png |
| /pos | 1280 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_aydinlik_1280.png |
| /pos | 1280 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_karanlik_1280.png |
| /pos | 1280 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_aydinlik_1280.png |
| /pos | 1280 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_karanlik_1280.png |
| /pos | 960 | BASIT | aydinlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_aydinlik_960.png |
| /pos | 960 | BASIT | karanlik | 0 | 0/0 | 1.4×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_karanlik_960.png |
| /pos | 960 | PRO | aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_aydinlik_960.png |
| /pos | 960 | PRO | karanlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_karanlik_960.png |
| /pos | 375 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_aydinlik_375.png |
| /pos | 375 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__BASIT_karanlik_375.png |
| /pos | 375 | PRO | aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_aydinlik_375.png |
| /pos | 375 | PRO | karanlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra9/pos__PRO_karanlik_375.png |
| /pos/tahsilatlar | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_aydinlik_1440.png |
| /pos/tahsilatlar | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_karanlik_1440.png |
| /pos/tahsilatlar | 1440 | PRO | aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_aydinlik_1440.png |
| /pos/tahsilatlar | 1440 | PRO | karanlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_karanlik_1440.png |
| /pos/tahsilatlar | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_aydinlik_1280.png |
| /pos/tahsilatlar | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_karanlik_1280.png |
| /pos/tahsilatlar | 1280 | PRO | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_aydinlik_1280.png |
| /pos/tahsilatlar | 1280 | PRO | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_karanlik_1280.png |
| /pos/tahsilatlar | 960 | BASIT | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_aydinlik_960.png |
| /pos/tahsilatlar | 960 | BASIT | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_karanlik_960.png |
| /pos/tahsilatlar | 960 | PRO | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_aydinlik_960.png |
| /pos/tahsilatlar | 960 | PRO | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_karanlik_960.png |
| /pos/tahsilatlar | 375 | BASIT | aydinlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_aydinlik_375.png |
| /pos/tahsilatlar | 375 | BASIT | karanlik | 0 | 0/0 | 2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__BASIT_karanlik_375.png |
| /pos/tahsilatlar | 375 | PRO | aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_aydinlik_375.png |
| /pos/tahsilatlar | 375 | PRO | karanlik | 0 | 0/0 | 2.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/pos_tahsilatlar__PRO_karanlik_375.png |
| /banka | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_aydinlik_1440.png |
| /banka | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_karanlik_1440.png |
| /banka | 1440 | PRO | aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_aydinlik_1440.png |
| /banka | 1440 | PRO | karanlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_karanlik_1440.png |
| /banka | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.5×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_aydinlik_1280.png |
| /banka | 1280 | BASIT | karanlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_karanlik_1280.png |
| /banka | 1280 | PRO | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_aydinlik_1280.png |
| /banka | 1280 | PRO | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_karanlik_1280.png |
| /banka | 960 | BASIT | aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_aydinlik_960.png |
| /banka | 960 | BASIT | karanlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_karanlik_960.png |
| /banka | 960 | PRO | aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_aydinlik_960.png |
| /banka | 960 | PRO | karanlik | 0 | 0/0 | 1.2×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_karanlik_960.png |
| /banka | 375 | BASIT | aydinlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_aydinlik_375.png |
| /banka | 375 | BASIT | karanlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__BASIT_karanlik_375.png |
| /banka | 375 | PRO | aydinlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_aydinlik_375.png |
| /banka | 375 | PRO | karanlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra8/banka__PRO_karanlik_375.png |
| /yetkiler | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_aydinlik_1440.png |
| /yetkiler | 1440 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_karanlik_1440.png |
| /yetkiler | 1440 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_aydinlik_1440.png |
| /yetkiler | 1440 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_karanlik_1440.png |
| /yetkiler | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_aydinlik_1280.png |
| /yetkiler | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_karanlik_1280.png |
| /yetkiler | 1280 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_aydinlik_1280.png |
| /yetkiler | 1280 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_karanlik_1280.png |
| /yetkiler | 960 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_aydinlik_960.png |
| /yetkiler | 960 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_karanlik_960.png |
| /yetkiler | 960 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_aydinlik_960.png |
| /yetkiler | 960 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_karanlik_960.png |
| /yetkiler | 375 | BASIT | aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_aydinlik_375.png |
| /yetkiler | 375 | BASIT | karanlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__BASIT_karanlik_375.png |
| /yetkiler | 375 | PRO | aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_aydinlik_375.png |
| /yetkiler | 375 | PRO | karanlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/yetkiler__PRO_karanlik_375.png |
| /yetkiler (MÜDÜR, şubesiz) | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_aydinlik_1440.png |
| /yetkiler (MÜDÜR, şubesiz) | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_karanlik_1440.png |
| /yetkiler (MÜDÜR, şubesiz) | 1440 | PRO | aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_aydinlik_1440.png |
| /yetkiler (MÜDÜR, şubesiz) | 1440 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_karanlik_1440.png |
| /yetkiler (MÜDÜR, şubesiz) | 1280 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_aydinlik_1280.png |
| /yetkiler (MÜDÜR, şubesiz) | 1280 | BASIT | karanlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_karanlik_1280.png |
| /yetkiler (MÜDÜR, şubesiz) | 1280 | PRO | aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_aydinlik_1280.png |
| /yetkiler (MÜDÜR, şubesiz) | 1280 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_karanlik_1280.png |
| /yetkiler (MÜDÜR, şubesiz) | 960 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_aydinlik_960.png |
| /yetkiler (MÜDÜR, şubesiz) | 960 | BASIT | karanlik | 0 | 0/0 | 1.1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_karanlik_960.png |
| /yetkiler (MÜDÜR, şubesiz) | 960 | PRO | aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_aydinlik_960.png |
| /yetkiler (MÜDÜR, şubesiz) | 960 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_karanlik_960.png |
| /yetkiler (MÜDÜR, şubesiz) | 375 | BASIT | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_aydinlik_375.png |
| /yetkiler (MÜDÜR, şubesiz) | 375 | BASIT | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__BASIT_karanlik_375.png |
| /yetkiler (MÜDÜR, şubesiz) | 375 | PRO | aydinlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_aydinlik_375.png |
| /yetkiler (MÜDÜR, şubesiz) | 375 | PRO | karanlik | 0 | 0/0 | 1×1 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra6/yetkiler__PRO_karanlik_375.png |
| /ayarlar | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.3×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_aydinlik_1440.png |
| /ayarlar | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_karanlik_1440.png |
| /ayarlar | 1440 | PRO | aydinlik | 0 | 0/0 | 1.2×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_aydinlik_1440.png |
| /ayarlar | 1440 | PRO | karanlik | 0 | 0/0 | 1.2×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_karanlik_1440.png |
| /ayarlar | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.3×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_aydinlik_1280.png |
| /ayarlar | 1280 | BASIT | karanlik | 0 | 0/0 | 1.3×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_karanlik_1280.png |
| /ayarlar | 1280 | PRO | aydinlik | 0 | 0/0 | 1.4×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_aydinlik_1280.png |
| /ayarlar | 1280 | PRO | karanlik | 0 | 0/0 | 1.4×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_karanlik_1280.png |
| /ayarlar | 960 | BASIT | aydinlik | 0 | 0/4 | 1.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_aydinlik_960.png |
| /ayarlar | 960 | BASIT | karanlik | 0 | 0/4 | 1.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_karanlik_960.png |
| /ayarlar | 960 | PRO | aydinlik | 0 | 0/4 | 1.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_aydinlik_960.png |
| /ayarlar | 960 | PRO | karanlik | 0 | 0/4 | 1.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_karanlik_960.png |
| /ayarlar | 375 | BASIT | aydinlik | 0 | 0/0 | 2.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_aydinlik_375.png |
| /ayarlar | 375 | BASIT | karanlik | 0 | 0/0 | 2.6×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__BASIT_karanlik_375.png |
| /ayarlar | 375 | PRO | aydinlik | 0 | 0/0 | 2.7×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_aydinlik_375.png |
| /ayarlar | 375 | PRO | karanlik | 0 | 0/0 | 2.7×9 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ayarlar__PRO_karanlik_375.png |
| /ekranlar | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_aydinlik_1440.png |
| /ekranlar | 1440 | BASIT | karanlik | 0 | 0/0 | 1×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_karanlik_1440.png |
| /ekranlar | 1440 | PRO | aydinlik | 0 | 0/0 | 1×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_aydinlik_1440.png |
| /ekranlar | 1440 | PRO | karanlik | 0 | 0/0 | 1×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_karanlik_1440.png |
| /ekranlar | 1280 | BASIT | aydinlik | 0 | 0/0 | 1.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_aydinlik_1280.png |
| /ekranlar | 1280 | BASIT | karanlik | 0 | 0/0 | 1.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_karanlik_1280.png |
| /ekranlar | 1280 | PRO | aydinlik | 0 | 0/0 | 1.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_aydinlik_1280.png |
| /ekranlar | 1280 | PRO | karanlik | 0 | 0/0 | 1.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_karanlik_1280.png |
| /ekranlar | 960 | BASIT | aydinlik | 0 | 0/0 | 1.8×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_aydinlik_960.png |
| /ekranlar | 960 | BASIT | karanlik | 0 | 0/0 | 1.8×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_karanlik_960.png |
| /ekranlar | 960 | PRO | aydinlik | 0 | 0/0 | 1.8×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_aydinlik_960.png |
| /ekranlar | 960 | PRO | karanlik | 0 | 0/0 | 1.8×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_karanlik_960.png |
| /ekranlar | 375 | BASIT | aydinlik | 0 | 0/0 | 4.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_aydinlik_375.png |
| /ekranlar | 375 | BASIT | karanlik | 0 | 0/0 | 4.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__BASIT_karanlik_375.png |
| /ekranlar | 375 | PRO | aydinlik | 0 | 0/0 | 4.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_aydinlik_375.png |
| /ekranlar | 375 | PRO | karanlik | 0 | 0/0 | 4.2×6 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/ekranlar__PRO_karanlik_375.png |
| /sayim | 1440 | BASIT | aydinlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_aydinlik_1440.png |
| /sayim | 1440 | BASIT | karanlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_karanlik_1440.png |
| /sayim | 1440 | PRO | aydinlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_aydinlik_1440.png |
| /sayim | 1440 | PRO | karanlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_karanlik_1440.png |
| /sayim | 1280 | BASIT | aydinlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_aydinlik_1280.png |
| /sayim | 1280 | BASIT | karanlik | 0 | 0/0 | 1.5×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_karanlik_1280.png |
| /sayim | 1280 | PRO | aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_aydinlik_1280.png |
| /sayim | 1280 | PRO | karanlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_karanlik_1280.png |
| /sayim | 960 | BASIT | aydinlik | 0 | 0/0 | 1.4×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_aydinlik_960.png |
| /sayim | 960 | BASIT | karanlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_karanlik_960.png |
| /sayim | 960 | PRO | aydinlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_aydinlik_960.png |
| /sayim | 960 | PRO | karanlik | 0 | 0/0 | 1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_karanlik_960.png |
| /sayim | 375 | BASIT | aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_aydinlik_375.png |
| /sayim | 375 | BASIT | karanlik | 0 | 0/0 | 1.1×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__BASIT_karanlik_375.png |
| /sayim | 375 | PRO | aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_aydinlik_375.png |
| /sayim | 375 | PRO | karanlik | 0 | 0/0 | 1.2×2 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra5/sayim__PRO_karanlik_375.png |
| /fiyatlandirma | 1440 | BASIT | aydinlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_aydinlik_1440.png |
| /fiyatlandirma | 1440 | BASIT | karanlik | 0 | 0/0 | 1.3×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_karanlik_1440.png |
| /fiyatlandirma | 1440 | PRO | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_aydinlik_1440.png |
| /fiyatlandirma | 1440 | PRO | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_karanlik_1440.png |
| /fiyatlandirma | 1280 | BASIT | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_aydinlik_1280.png |
| /fiyatlandirma | 1280 | BASIT | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_karanlik_1280.png |
| /fiyatlandirma | 1280 | PRO | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_aydinlik_1280.png |
| /fiyatlandirma | 1280 | PRO | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_karanlik_1280.png |
| /fiyatlandirma | 960 | BASIT | aydinlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_aydinlik_960.png |
| /fiyatlandirma | 960 | BASIT | karanlik | 0 | 0/0 | 1.4×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_karanlik_960.png |
| /fiyatlandirma | 960 | PRO | aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_aydinlik_960.png |
| /fiyatlandirma | 960 | PRO | karanlik | 0 | 0/0 | 1.1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_karanlik_960.png |
| /fiyatlandirma | 375 | BASIT | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_aydinlik_375.png |
| /fiyatlandirma | 375 | BASIT | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__BASIT_karanlik_375.png |
| /fiyatlandirma | 375 | PRO | aydinlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_aydinlik_375.png |
| /fiyatlandirma | 375 | PRO | karanlik | 0 | 0/0 | 1×3 | 0 | 0+0 | 0+0 | 0 | 0 | 0 | sonra2/fiyatlandirma__PRO_karanlik_375.png |

TOPLAM {'K1': 0, 'K3': 16, 'K5': 0, 'K9': 0, 'K10': 0, 'K12': 0, 'K13': 0, 'Y': 0} satir 256

## 8b. Tur 2 canlı doğrulama (API 5170 + vekil 5171, CDP 9810–9818)
Kiracı `PAKETSADE-1-TEST Kuyumculuk` (01M3C0V9QQSNC27KB11T3XYN0A) `demo-tohumla.js --prova-kiraci` ile ÜRÜN UÇLARINDAN kuruldu (6 müşteri, 12 ürün, 9 satış) + API ile: POST /bankalar 200 + 3× /hareket 200 (ilk denemede GIRIS/CIKIS → 400 "yalnız GELEN veya GIDEN" — gövde düzeltildi), POST /pos/cihazlar ×2 200, POST /sayim 200 → /iptal 200 → POST /sayim 200 (liste [ACIK, IPTAL]; ikinci açık oturum 400 — tek açık oturum kuralı), POST /kullanicilar (TEZGÂHTAR) 201, GET /kur/kaynaklar 200 {secili ALTINKAYNAK, seciliSozlesmesiz true}, GET /gorunum 200, GET /kurulum/durum 200 (6 adım).
Tarayıcı: hata anı betiği `tur-sade/hata-ani.mjs` (16 ölçüm), rol betiği `tur-sade/kur-rol.mjs` (5 ölçüm), tam tur `tur-sade/tur.mjs` 14 rota × {1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık} = **224 sayfa** (+144 sekme, 424 açıcı), `analiz.py sonra11`: benzersiz bulgu 8 — hepsi ÖNCE'de de olan: /ayarlar 960 K3 (GorunumKarti, KATMAN), /ayarlar 375 K4 2.6–2.7×9, /ayarlar?sekme=guvenlik K4, /kurulum 375 K4 2.4–2.5 (ÖNCE 2.3–2.6); /kurulum 960 PRO 2.1 → (10) ile 2.0. /kilavuz TEMİZ. Tablo § 15.

## 9. Temizlik
**Tur 2:** API (dotnet run PID 53908 + dinleyen 53916; ilk koşum 97615/97625) ve vekil `kill` ile durduruldu; `lsof` 5170: 0, 5171: 0. Kiracı `kiraci-temizle.sh 01M3C0V9QQSNC27KB11T3XYN0A --uygula` ile silindi; information_schema'daki 70 tenant_id'li tabloda kalan satır **0**; tenants'ta kayıt 0; davet satırı (01M3C0V8X4BDH5Q09CX94PSDSV) silindi. tenants: başlangıç 17 → son 16 (SADE-1 net etkisi 0: açtığı tek kiracıyı sildi; fark başka şeridin silmesi). QA-TEST-* kiracılarına dokunulmadı. Sır dosyaları (sistem anahtarı, iki fikstür) `printf "" >` ile boşaltıldı (0 bayt). Proje ağacına .mutorj/.bak/.tmp bırakılmadı (`Islemler.Sade.cs.tmp` CUMA-S'in).

**Tur 1:**
- API (dinleyen PID 9716 + `dotnet run` PID 4025) ve vekil (PID 98321) `kill` ile durduruldu; `lsof` 5170 ve 5171: boş. Başka şeridin vekiline (5195/5196) dokunulmadı.
- Test kiracısı `PAKETSADE-1-TEST Kuyumculuk` (01M3BSQSXQWJJSV3Y9Y39CMXTP) `betikler/kiraci-temizle.sh --uygula` ile 70 tablodan silindi (2498 satır + tenants 1); döngüyle yeniden sayıldı: kalan 0 satır. Davet satırı (`kiraci_davetleri` 01M3BSQSNYTGZMYPSRFV1PG0F7, tenant_id kolonu yok) ayrıca silindi.
- tenants: başlangıçta 17 (diğer şeritlerin test kiracıları dâhil) → son 15. Fark diğer şeritlerin kendi kiracılarını açıp silmesinden; SADE-1'in net etkisi 0 (açtığı tek kiracıyı sildi). QA-TEST-* kiracılarına dokunulmadı.
- Sır dosyaları (sistem anahtarı, iki fikstür) `printf "" >` ile boşaltıldı; hiçbir parola/jeton çıktıya yazılmadı. Proje ağacına .mutorj/.bak/.tmp bırakılmadı (mevcut `Fiyatlandirma.razor.mutorj`, `Sayim.razor.mutorj`, `Islemler.Sade.cs.tmp` bu paketten önce/başkasına ait).

## 10. Değişen dosyalar (SADE-1)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Sade.cs` (yeni, TR+EN konumsal record)
- `dotnet/Kuyumcu.Web/wwwroot/css/sade.css` (yeni; sabit renk yok)
- `dotnet/Kuyumcu.Web/wwwroot/index.html` (sade.css bağlantısı)
- `dotnet/Kuyumcu.Web/Pages/{VeriDisaAktarim,Yedekler,Kurulum,Hurda,Kur,Pos,PosTahsilatlar,Banka,Yetkiler,Ayarlar,Sayim,Kilavuz}.razor`
- `dotnet/Kuyumcu.Tests/SADE1EkranTestleri.cs` (yeni), `BankaEkranTestleri.cs` (+1 test), `UX4dEkranTestleri.cs` (M24 güncellendi)
- `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/SADE-1-*.png` (14)
- **Tur 2:** `Pages/Kur.razor` (rol kapısı), `Pages/Ayarlar.razor` (OkumaDenendi koşulu + Degisti aboneliği), `Servisler/GorunumServisi.cs` (OkumaDenendi — gerekçe § 0), `Sozlukler.Sade.cs` (+KurDenemeIpucu TR+EN), `css/sade.css` (+ (9) kılavuz 375, (10) kurulum boş durum), `SADE1EkranTestleri.cs` (+3 test, askıda istek düzeneği), `UX4dEkranTestleri.cs` (M24 SAHİP oturumu), `ham/TUVAL-KARSILASTIRMA/SADE-1-{ayarlar-hata-bandi,kur-kaynak-rol,kurulum-429}-1440.png` (3 yeni)
(`git status --porcelain | grep -v -E "/(bin|obj)/"` — sayfa dosyalarının çoğu zaten izlenmeyen (`??`) durumda; liste yukarıdaki gibidir.)

## 11. DoD sözleşmesi
Yeni varlık/kayıt türü YOK; uç eklenmedi/değişmedi. Mevcut yollar: POS "Kullanımdan kaldır" PATCH + onay modalı + "Aktifleştir" geri dönüşü aynen; Banka hareketleri salt okunur/storno — gerekçeler (DegismezIpucu ×4) katlanın içinde aynen; Sayım iptali onaylı yol aynen. Yeni düğmeler (`#kur-yeniden`, `#gorunum-yeniden`, `#benim-yeniden`) GERÇEK GET isteği atar (bUnit ile sınandı). Yeni DOM id'leri önekli (`kur-`, `hrd-`, `pos-t-`, `bh-`, `kl-`, `gorunum-`, `benim-`); mevcut id'ler değişmedi.

## 12. Koşul karşılama
| Koşul | Durum |
|---|---|
| Çift başlık yok (h1 sayısı sayfada 0, başlık metni 1 kez) | ✓ bUnit + tarayıcı |
| Kurulum adımları yüklenir, sonsuz "Yükleniyor" yok, hata → bant | ✓ bUnit (sunucu + ağ hatası) + tarayıcı |
| Teknik/hukuki metin BASİT'te tek cümle, ayrıntı silinmez | ✓ (#25, #63, #27, #18) |
| Tehlike rengi yalnız geri alınamaz işlemde | ✓ (#64) |
| Sekme şeridi tuval segment dili | ✓ (#17; yalnız SADE-1 sayfalarındaki şeritler, kimlikle kapsanmış) |
| En az 3 mutasyon (çift başlık, yükleme hatası yutma, birincil düğme sınıfı) | ✓ 15 mutasyon, hepsi kırmızı + cmp |
| app.css'e yazılmadı, Sozlukler hub'ına dokunulmadı | ✓ |
| Native select yerine Secici | — bu bulgularda native select yoktu (HAM_SELECT sayacı artmadı) |
| Gerçek tarayıcı 1440/1280/960/375 × BASİT/PRO × açık/koyu | ✓ tur 1: 256 satır · tur 2: 224 satır (§ 15, tema eşleşmesi 224/224), yeni K bulgusu 0 |
| #41 hata anı tarayıcıda | ✓ tur 2: CDP 429, 8/8 kombinasyon (§ 0) |
| #25 hukuki metin rol kapısı | ✓ tur 2: bUnit + mutasyon + tarayıcıda tezgâhtar/sahip |
| #14 429 altında yeniden ölçüm | ✓ tur 2: 8/8 kombinasyon, sonsuz "Yükleniyor" 0 |
| Derleme | ✓ 0 hata; `--no-incremental` 1 uyarı (EtiketEkranTestleri xUnit1031 — ORTAK-1) |

## 13. Açık kalanlar (sahibiyle)
1. **/ekranlar karo ikonları** tuvaldekinden farklı (Panel, Satış…) — ikonlar `MenuTanimi`'nde; SADE-1 dosya alanı dışında → KATMAN/ORTAK sahibine.
2. **/ayarlar 960 K3** (GorunumKarti "Fildişi (açık menü)", "Bilgisayara uy" kırılıyor) → KATMAN (GorunumKarti).
3. **/kurulum 375 K4 "tren"** (ÖNCE'de de vardı): açılış formu + mühür tek sayfada; sekmeye bölmek ayrı bir tasarım işi.
4. Tam süit #2'deki 26 kırmızı → CUMA-S (Islemler/Fis/Satış tuvali), ISTEK-1 (önbellek), ORTAK-1 (StatKart) — yukarıda adlarıyla.
5. denetim.sh: ToListAsync tavanı (Modules) ve P-23 ODEME_KALEMSIZ_YOL (PAKETSATIS-2-TEST kiracısı) → SATIS-2 / ilgili backend şeridi.
6. Tur aracında tema yarışı (aynı kullanıcı + eşzamanlı aydınlık/karanlık işçi) → ölçüm aracı sahibine (orkestratör).
7. ~~#41 tarayıcı kanıtı~~ → tur 2'de KAPANDI (§ 0).
8. **#25 ikinci dalga:** hukuki tam metnin Ayarlar → Kur kaynağı ekranına taşınması (doğrulayıcı notu) — şimdilik /kur "Kaynak" sekmesinde, YALNIZ SAHİP görür. Gözlem: tezgâhtar /kur "Kaynak" sekmesinde kaynak seçicisini etkin görüyor (PATCH ucu SAHİP/MÜDÜR; sunucu reddeder) — seçicinin rol kilidi + sebep satırı (UX-S30) ayrı iş; bu pakette dokunulmadı.
9. /ekranlar 375 K4 4.2×6 (ÖNCE 3.6×6; tüm ekran kataloğu tek sayfa) — analiz.py bu satırları "geçersiz" sayıyor çünkü /ekranlar üst barında Basit/Profesyonel anahtarı yok (modGercek "?"); ölçüm § 15 tablosunda açıkça var. Katalog düzeni SADE-1 bulgusu değildi.
10. Derleme uyarısı `EtiketEkranTestleri.cs(145,47) xUnit1031` → ORTAK-1 (UrunEtiket).
11. Tam süitteki 2 kırmızı `KatmanTekKalipTestleri.K1_15/K1_17` → KATMAN.
12. sade.css yükleme sırası ↔ O1 testi çelişkisi → orkestratör.
13. Ortak tur aracında tema yarışı → orkestratör (kendi kopyamda düzeltildi, § 0).

## 14. Kurucuya / mali müşavire not
Mevzuat kapsamlı değişiklik yok. #25: "sözleşmesiz kur kaynağı" uyarısı silinmedi, günlük ekranda "Deneme kur kaynağı" rozeti oldu; tam metin kaynak seçim kartında. Ticari kullanım öncesi kur sağlayıcı sözleşmesi kararı kurucudadır (değişmedi).

## 15. Tur 2 ölçüm tablosu (rota × genişlik × mod × tema; kaynak `scratchpad/tur-sade/sonra11`, /kurulum ve /kilavuz PRO satırları son css ile `sonra12`)
| rota | gen. | mod | tema (istenen/gerçek) | K1 | K3 taşan/kırık | K4 boy×bölüm | K5 TRY | K9 binen | K10 | K12 | K13 | yatay | foto |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /veri-disa-aktarim | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_aydinlik_1440.png |
| /yedekler | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_aydinlik_1440.png |
| /kurulum | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.7×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_aydinlik_1440.png |
| /kilavuz | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_aydinlik_1440.png |
| /hurda | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.8×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_aydinlik_1440.png |
| /kur | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_aydinlik_1440.png |
| /pos | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_aydinlik_1440.png |
| /pos/tahsilatlar | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_aydinlik_1440.png |
| /banka | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_aydinlik_1440.png |
| /yetkiler | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_aydinlik_1440.png |
| /ayarlar | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.2×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_aydinlik_1440.png |
| /ekranlar | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_aydinlik_1440.png |
| /sayim | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_aydinlik_1440.png |
| /fiyatlandirma | 1440 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_aydinlik_1440.png |
| /veri-disa-aktarim | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_aydinlik_1280.png |
| /yedekler | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_aydinlik_1280.png |
| /kurulum | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.9×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_aydinlik_1280.png |
| /kilavuz | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_aydinlik_1280.png |
| /hurda | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_aydinlik_1280.png |
| /kur | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_aydinlik_1280.png |
| /pos | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_aydinlik_1280.png |
| /pos/tahsilatlar | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_aydinlik_1280.png |
| /banka | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_aydinlik_1280.png |
| /yetkiler | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_aydinlik_1280.png |
| /ayarlar | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_aydinlik_1280.png |
| /ekranlar | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_aydinlik_1280.png |
| /sayim | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_aydinlik_1280.png |
| /fiyatlandirma | 1280 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_aydinlik_1280.png |
| /veri-disa-aktarim | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.6×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_aydinlik_960.png |
| /yedekler | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_aydinlik_960.png |
| /kurulum | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_aydinlik_960.png |
| /kilavuz | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_aydinlik_960.png |
| /hurda | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_aydinlik_960.png |
| /kur | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_aydinlik_960.png |
| /pos | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_aydinlik_960.png |
| /pos/tahsilatlar | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_aydinlik_960.png |
| /banka | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_aydinlik_960.png |
| /yetkiler | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_aydinlik_960.png |
| /ayarlar | 960 | BASIT | aydinlik/aydinlik | 0 | 0/4 | 1.5×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_aydinlik_960.png |
| /ekranlar | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_aydinlik_960.png |
| /sayim | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_aydinlik_960.png |
| /fiyatlandirma | 960 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_aydinlik_960.png |
| /veri-disa-aktarim | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_aydinlik_375.png |
| /yedekler | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.6×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_aydinlik_375.png |
| /kurulum | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2.4×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_aydinlik_375.png |
| /kilavuz | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.6×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_aydinlik_375.png |
| /hurda | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_aydinlik_375.png |
| /kur | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_aydinlik_375.png |
| /pos | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_aydinlik_375.png |
| /pos/tahsilatlar | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_aydinlik_375.png |
| /banka | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.4×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_aydinlik_375.png |
| /yetkiler | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_aydinlik_375.png |
| /ayarlar | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 2.6×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_aydinlik_375.png |
| /ekranlar | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 4.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_aydinlik_375.png |
| /sayim | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_aydinlik_375.png |
| /fiyatlandirma | 375 | BASIT | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_aydinlik_375.png |
| /veri-disa-aktarim | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_karanlik_1440.png |
| /yedekler | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_karanlik_1440.png |
| /kurulum | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.7×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_karanlik_1440.png |
| /kilavuz | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_karanlik_1440.png |
| /hurda | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.8×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_karanlik_1440.png |
| /kur | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_karanlik_1440.png |
| /pos | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_karanlik_1440.png |
| /pos/tahsilatlar | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_karanlik_1440.png |
| /banka | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_karanlik_1440.png |
| /yetkiler | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_karanlik_1440.png |
| /ayarlar | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.2×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_karanlik_1440.png |
| /ekranlar | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_karanlik_1440.png |
| /sayim | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_karanlik_1440.png |
| /fiyatlandirma | 1440 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_karanlik_1440.png |
| /veri-disa-aktarim | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_karanlik_1280.png |
| /yedekler | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_karanlik_1280.png |
| /kurulum | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.9×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_karanlik_1280.png |
| /kilavuz | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_karanlik_1280.png |
| /hurda | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_karanlik_1280.png |
| /kur | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_karanlik_1280.png |
| /pos | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_karanlik_1280.png |
| /pos/tahsilatlar | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_karanlik_1280.png |
| /banka | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_karanlik_1280.png |
| /yetkiler | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_karanlik_1280.png |
| /ayarlar | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_karanlik_1280.png |
| /ekranlar | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_karanlik_1280.png |
| /sayim | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_karanlik_1280.png |
| /fiyatlandirma | 1280 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_karanlik_1280.png |
| /veri-disa-aktarim | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.6×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_karanlik_960.png |
| /yedekler | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_karanlik_960.png |
| /kurulum | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_karanlik_960.png |
| /kilavuz | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_karanlik_960.png |
| /hurda | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_karanlik_960.png |
| /kur | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_karanlik_960.png |
| /pos | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_karanlik_960.png |
| /pos/tahsilatlar | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_karanlik_960.png |
| /banka | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_karanlik_960.png |
| /yetkiler | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_karanlik_960.png |
| /ayarlar | 960 | BASIT | karanlik/karanlik | 0 | 0/4 | 1.5×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_karanlik_960.png |
| /ekranlar | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_karanlik_960.png |
| /sayim | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_karanlik_960.png |
| /fiyatlandirma | 960 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_karanlik_960.png |
| /veri-disa-aktarim | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__BASIT_karanlik_375.png |
| /yedekler | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.6×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__BASIT_karanlik_375.png |
| /kurulum | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 2.4×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kurulum__BASIT_karanlik_375.png |
| /kilavuz | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.6×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kilavuz__BASIT_karanlik_375.png |
| /hurda | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 2.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__BASIT_karanlik_375.png |
| /kur | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__BASIT_karanlik_375.png |
| /pos | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__BASIT_karanlik_375.png |
| /pos/tahsilatlar | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__BASIT_karanlik_375.png |
| /banka | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.4×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__BASIT_karanlik_375.png |
| /yetkiler | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__BASIT_karanlik_375.png |
| /ayarlar | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 2.6×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__BASIT_karanlik_375.png |
| /ekranlar | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 4.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__BASIT_karanlik_375.png |
| /sayim | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__BASIT_karanlik_375.png |
| /fiyatlandirma | 375 | BASIT | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__BASIT_karanlik_375.png |
| /veri-disa-aktarim | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_aydinlik_1440.png |
| /yedekler | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_aydinlik_1440.png |
| /kurulum | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.7×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_aydinlik_1440.png |
| /kilavuz | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_aydinlik_1440.png |
| /hurda | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.8×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_aydinlik_1440.png |
| /kur | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_aydinlik_1440.png |
| /pos | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_aydinlik_1440.png |
| /pos/tahsilatlar | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_aydinlik_1440.png |
| /banka | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_aydinlik_1440.png |
| /yetkiler | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_aydinlik_1440.png |
| /ayarlar | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_aydinlik_1440.png |
| /ekranlar | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_aydinlik_1440.png |
| /sayim | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_aydinlik_1440.png |
| /fiyatlandirma | 1440 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_aydinlik_1440.png |
| /veri-disa-aktarim | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_aydinlik_1280.png |
| /yedekler | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_aydinlik_1280.png |
| /kurulum | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.9×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_aydinlik_1280.png |
| /kilavuz | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.5×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_aydinlik_1280.png |
| /hurda | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_aydinlik_1280.png |
| /kur | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_aydinlik_1280.png |
| /pos | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_aydinlik_1280.png |
| /pos/tahsilatlar | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_aydinlik_1280.png |
| /banka | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_aydinlik_1280.png |
| /yetkiler | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_aydinlik_1280.png |
| /ayarlar | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.4×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_aydinlik_1280.png |
| /ekranlar | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_aydinlik_1280.png |
| /sayim | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_aydinlik_1280.png |
| /fiyatlandirma | 1280 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_aydinlik_1280.png |
| /veri-disa-aktarim | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.6×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_aydinlik_960.png |
| /yedekler | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_aydinlik_960.png |
| /kurulum | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_aydinlik_960.png |
| /kilavuz | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.6×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_aydinlik_960.png |
| /hurda | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_aydinlik_960.png |
| /kur | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_aydinlik_960.png |
| /pos | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_aydinlik_960.png |
| /pos/tahsilatlar | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_aydinlik_960.png |
| /banka | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_aydinlik_960.png |
| /yetkiler | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_aydinlik_960.png |
| /ayarlar | 960 | PRO | aydinlik/aydinlik | 0 | 0/4 | 1.6×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_aydinlik_960.png |
| /ekranlar | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_aydinlik_960.png |
| /sayim | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_aydinlik_960.png |
| /fiyatlandirma | 960 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_aydinlik_960.png |
| /veri-disa-aktarim | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_aydinlik_375.png |
| /yedekler | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.7×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_aydinlik_375.png |
| /kurulum | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.5×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_aydinlik_375.png |
| /kilavuz | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_aydinlik_375.png |
| /hurda | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_aydinlik_375.png |
| /kur | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.5×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_aydinlik_375.png |
| /pos | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_aydinlik_375.png |
| /pos/tahsilatlar | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_aydinlik_375.png |
| /banka | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.4×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_aydinlik_375.png |
| /yetkiler | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_aydinlik_375.png |
| /ayarlar | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 2.7×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_aydinlik_375.png |
| /ekranlar | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 4.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_aydinlik_375.png |
| /sayim | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_aydinlik_375.png |
| /fiyatlandirma | 375 | PRO | aydinlik/aydinlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_aydinlik_375.png |
| /veri-disa-aktarim | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_karanlik_1440.png |
| /yedekler | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_karanlik_1440.png |
| /kurulum | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.7×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_karanlik_1440.png |
| /kilavuz | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_karanlik_1440.png |
| /hurda | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.8×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_karanlik_1440.png |
| /kur | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_karanlik_1440.png |
| /pos | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_karanlik_1440.png |
| /pos/tahsilatlar | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_karanlik_1440.png |
| /banka | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_karanlik_1440.png |
| /yetkiler | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_karanlik_1440.png |
| /ayarlar | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_karanlik_1440.png |
| /ekranlar | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_karanlik_1440.png |
| /sayim | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_karanlik_1440.png |
| /fiyatlandirma | 1440 | PRO | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_karanlik_1440.png |
| /veri-disa-aktarim | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_karanlik_1280.png |
| /yedekler | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_karanlik_1280.png |
| /kurulum | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.9×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_karanlik_1280.png |
| /kilavuz | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.5×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_karanlik_1280.png |
| /hurda | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_karanlik_1280.png |
| /kur | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_karanlik_1280.png |
| /pos | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_karanlik_1280.png |
| /pos/tahsilatlar | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_karanlik_1280.png |
| /banka | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_karanlik_1280.png |
| /yetkiler | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_karanlik_1280.png |
| /ayarlar | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.4×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_karanlik_1280.png |
| /ekranlar | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_karanlik_1280.png |
| /sayim | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_karanlik_1280.png |
| /fiyatlandirma | 1280 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_karanlik_1280.png |
| /veri-disa-aktarim | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.6×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_karanlik_960.png |
| /yedekler | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_karanlik_960.png |
| /kurulum | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_karanlik_960.png |
| /kilavuz | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.6×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_karanlik_960.png |
| /hurda | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_karanlik_960.png |
| /kur | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.3×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_karanlik_960.png |
| /pos | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_karanlik_960.png |
| /pos/tahsilatlar | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.5×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_karanlik_960.png |
| /banka | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_karanlik_960.png |
| /yetkiler | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_karanlik_960.png |
| /ayarlar | 960 | PRO | karanlik/karanlik | 0 | 0/4 | 1.6×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_karanlik_960.png |
| /ekranlar | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.8×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_karanlik_960.png |
| /sayim | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_karanlik_960.png |
| /fiyatlandirma | 960 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_karanlik_960.png |
| /veri-disa-aktarim | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/veri_disa_aktarim__PRO_karanlik_375.png |
| /yedekler | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.7×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yedekler__PRO_karanlik_375.png |
| /kurulum | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2.5×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kurulum__PRO_karanlik_375.png |
| /kilavuz | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2×4 | 0 | 0 | 0 | 0 | 0 | 0 | sonra12/kilavuz__PRO_karanlik_375.png |
| /hurda | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2.3×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/hurda__PRO_karanlik_375.png |
| /kur | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.5×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/kur__PRO_karanlik_375.png |
| /pos | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos__PRO_karanlik_375.png |
| /pos/tahsilatlar | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2.1×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/pos_tahsilatlar__PRO_karanlik_375.png |
| /banka | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.4×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/banka__PRO_karanlik_375.png |
| /yetkiler | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×1 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/yetkiler__PRO_karanlik_375.png |
| /ayarlar | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 2.7×9 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ayarlar__PRO_karanlik_375.png |
| /ekranlar | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 4.2×6 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/ekranlar__PRO_karanlik_375.png |
| /sayim | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1.2×2 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/sayim__PRO_karanlik_375.png |
| /fiyatlandirma | 375 | PRO | karanlik/karanlik | 0 | 0/0 | 1×3 | 0 | 0 | 0 | 0 | 0 | 0 | sonra11/fiyatlandirma__PRO_karanlik_375.png |
