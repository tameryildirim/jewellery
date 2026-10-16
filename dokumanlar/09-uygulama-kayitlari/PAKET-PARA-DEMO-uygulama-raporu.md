# PAKET PARA-DEMO — uygulama raporu (01.10.2026, şerit t2)

Kaynak: `demo-kayit-2026-10-01-uzun/ADIMLAR.md` (PARA BULGUSU 1–2 + "Para dışı hatalar" md.1, 2, 6).
Rol: Taşkın (backend) + Ertuğ (frontend). COMMIT YOK. Dosya silinmedi.

## 0. Özet
Beş maddenin beşi de güncel ağaçta ÖNCE ölçüldü ve hâlâ vardı; beşi de kapatıldı.

| # | Madde | Önce (güncel ağaç, API) | Sonra |
|---|---|---|---|
| 1 | Günü Kapat sahte açık + mutabakatta havale yok | Nakit kuruşu kuruşuna sayıldı → **400 "73.111,24 TL fark var"** (= kart 22.696,46 + havale 50.414,78). Mutabakat ekranında yalnız "Nakit · POS" | Sayılan nakit → **200, fark 0** (SQL `gun_kapanislari`: sayılan = defter = 193.505,02, fark 0,00). Ekranda Kasadaki toplam / Kart / Havale / Sayılacak nakit ayrı satır; mutabakat "Nakit · Kart · Havale" (toplam = alt kalemler) |
| 2 | Has ile tahsilat "Borcu azaltır" diyor | TL 10.422,07 → 10.422,07, has 0 → −1; pencere "Borcu azaltır" | Davranış aynı (ekip kararı B, aşağıda); metin doğru, TL borçluda uyarı + doğru yol; altınla tahsilat artık "NAKIT" etiketi taşımıyor |
| 3 | Takas kimliksiz | `POST /islemler/takas` satıcısız → **200** | Satıcısız → **400** (VUK md.234); vatandaşta gider pusulası aynı commit'te; esnaf muaf (stopajlı) → 400; mükellef → fatura no açıklamada |
| 4 | Peşin Müşteri + Veresiye | Sunucu 400 veriyor, Kaydet açık | Kaydet ÖNCEDEN kilitli, sebep yazılı (tarayıcıda ölçüldü) |
| 5 | Ekstre toplamı tutmuyor, ödeme türü yok | Kemal: satır TL toplamı **28.327,48**, bakiye **10.422,07**; ödeme sütunu yok | Satırda ödeme türü, borç etkisi, yürüyen bakiye; son satır bakiyesi = TL bakiye, borç etkileri toplamı = bakiye (ekran + kâğıt) |

## 1. Spesifikasyonun koda karşı doğrulanması
- `KasaModulu.cs` GunKapat: `fark = dto.SayilanNakit − kasa.Bakiye` (satır ~385) — doğrulandı.
- Kart/havale kasaya yazılıyor: `kasa_hareketleri.odeme_turu` NAKIT/KART/HAVALE; açılış devri `DIGER`, gider/virman/evrak `NAKIT`, döviz `DOVIZ` (AcilisServisi:368, GiderModulu:346, VirmanModulu:267, CariModulu:1496).
- `RaporModulu.PosFaturaMutabakati` sunucuda havaleyi ZATEN dönüyordu (`tahsilat.havale`); kayıp yalnız ekrandaydı (`Raporlar.razor` "Nakit · POS"). → RaporModulu.cs'e dokunulmadı.
- `CariModulu.TahsilatGirHareket`: TL ve has iki ayrı bacak (doğrulandı).
- `SatisModulu.Takas`: `GiderPusulasiKapisi` çağrılmıyordu (doğrulandı).
- Ekstre: sunucu `odemeTuru` ve `cariTlEtkisi` dönüyordu; ekran ham `tlTutari` basıyordu; kâğıt (`CiktiModulu.Ekstre`) ödeme ve bakiye sütunu taşımıyordu.

## 2. Yapılanlar
### Madde 1 — nakit sayımı (YÜKSEK)
- **Yeni kavram İCAT EDİLMEDİ**, defter/bakiye değişmedi, geri dönük veri düzeltmesi yok. Ayrım okuma tarafında: `Kasa/KasaNakitAyrimi.cs` → nakit defteri = kasa.Bakiye − Σkart − Σhavale (`kasa_hareketleri.odeme_turu`). Nakit dışı sayılan YALNIZ KART ve HAVALE (kapalı liste); DIGER/NAKIT/DOVIZ bugünkü gibi nakit (şüphede eski davranış).
- `GunKapat`: fark = sayılan − nakit defteri; `gun_kapanislari.defter_bakiye` = karşılaştırılan nakit defteri (fark = sayılan − defter değişmezi korunur); yanıt `toplamBakiye`, `kartToplam`, `havaleToplam` de döner (UX-S27). Hata metni kart/havale tutarını söyler.
- Yeni uç: `GET /api/v1/kasalar/{id}/gun-kapanisi/onizleme` → `GunKapanisiOnizleme` (Contracts, tek tip), `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` (kapanışla aynı küme). Salt okunur, yeni kayıt türü yok.
- Ekran: `Kasalar.razor` Günü Kapat çekmecesi ve `KasaRaporu.razor` sayım çekmecesi önizlemeyi okur. Kart/havale yoksa ekran eskisi gibi tek satır (zorunlu önce); varsa Kasadaki toplam · Kart (sayıma girmez) · Havale (sayıma girmez) · Sayılacak nakit + tek cümle not. Önizleme okunamazsa hata bandı + Kapat kilitli + sebep (UX-S28/S30, "0 TL" uydurulmaz).
- `Raporlar.razor` gün sonu: alt metin "Nakit · Kart · Havale (+ Diğer yalnız ≠0)" — toplam = alt kalemler.

### Madde 2 — has ile tahsilat (ORTA) — ekip kararı
**Karar: B (davranış aynı; metin ve akış gerçeği söyler).** Gerekçe (Taşkın + Mustafa + Mehmet):
1. Altını TL'ye çevirmek bir ALIŞ FİYATI ister; sistem alış fiyatını kur kaydından TÜRETMEZ (K-KUR-3, `HurdaAyarlari.AlisIskontoOran` notu: "fiyat dükkânın kararıdır").
2. Müşterinin altınını para karşılığı almak bir alımdır (TBK md.283) → gider pusulası/kimlik gerekir (madde 3 ile aynı hüküm). Tahsilat penceresinde bunu sessizce yapmak kapıyı atlatırdı.
3. Doğru yol zaten var: Satış → Hurda Alış (kimlik + fiyat, pusula) ardından aynı tutarın TL tahsilatı. Uyarı bunu yazar.
Yapılan: tahsilat ipucu "Para TL borcunu düşürür; altın, müşterinin altın hesabına yazılır"; alan adı "Altınla ödeme (saf altın gramı)"; TL borçlu müşteride altın girilince uyarı kutusu (borç tutarı + doğru yol); kayıt sonrası başarı bandı "Borç: … · Altın: …". Para davranışı DEĞİŞMEDİ (çift bacak testleri yeşil). Ek düzeltme: yalnız altınla (TL 0) tahsilatta hareket artık `odeme_turu = NULL` yazar (P-23 `OdemeTutarKapisi.Etiket`, takastakiyle aynı) — eskiden formun "NAKIT" varsayılanı yazılıyor, ekstrede "Nakit" görünüyor ve denetim P-23'te kalemsiz NAKIT satırı doğuyordu (canlıda ölçüldü).

### Madde 3 — takasta kimlik (YASAL) — mevzuat
Birincil kaynaklar:
- **VUK md.234** (mevzuat.gov.tr, 213 sayılı Kanun PDF): "…bu Kanun kapsamındaki belgeleri düzenleme zorunluluğu bulunmayanlara yaptırdıkları işler veya onlardan satın aldıkları mallar … için … gider pusulası düzenlerler." İkinci fıkra: pusula "emtiayı satın alan ile satanın adlariyle soyadlarını … ve adreslerini" ihtiva eder.
- **TBK (6098) md.282–283** (mevzuat.gov.tr): "Satış sözleşmesine ilişkin hükümler, mal değişim sözleşmesine de uygulanır; buna göre taraflardan her biri, vermeyi üstlendiği şey bakımından satıcı…" → takasta dükkân eski altını müşteriden SATIN ALIR.
Sonuç: hurda alışı ve eski altınla ödemedeki kapının AYNISI takasa da uygulanır (fail-closed). `TakasDto.Satici` (HurdaSaticiDto) + `hurda.Dogrula` (eşiksiz); vatandaşta gider pusulası takas satırıyla aynı commit'te (`GiderPusulasiKapisi.Kaydet`, brüt = eski altın bedeli); mükellefte pusula yok, fatura no takas açıklamasına; **stopajlı tip (esnaf muaf, oran ≠ 0) reddedilir** — takas farkı stopajsız hesaplanıyor, iki tutar tutmazdı; "Hurda Alış + Satış ayrı girin" denir (yeni stopaj hesabı İCAT EDİLMEDİ). Yanıt `saticiTipi` + `giderPusulasi` döner; ekranda pusula no başarı bandında.
Ekran: `FisTakasSatici.razor` (id'ler `takas-satici-*`), hurda alışıyla aynı model/doğrulama; eksikse Kaydet kilitli + sebep; esnaf muafta kilit + yol.
**Mali müşavir sorusu:** (a) takasta esnaf muafiyetli satıcıdan alınan altın için stopaj takas farkından mı mahsup edilir? (b) mükellef satıcıyla takasta satıcının faturası dışında belge gerekir mi? Bugün tutucu davranış: (a) reddedilir, (b) yalnız fatura no saklanır.

### Madde 4 — Peşin Müşteri + Veresiye
`Islemler.ParaDemo.cs` `PesinVeresiyeVar` (tek ödeme VERESIYE ya da karmada tutarı olan VERESIYE satırı) → `Gonderilebilir` false, `KaydetKilitSebebi` "Peşin Müşteri'ye veresiye yazılamaz — müşteri seçin ya da ödemeyi değiştirin." Sunucu kapısı aynen duruyor. Geri dönüş: gerçek müşteri seçilince kilit kalkar (test).

### Madde 5 — ekstre
- Ekran (`Cariler.razor` detay): tür yanında ödeme ("· Nakit/Kart/Havale/Veresiye/Altın", sözlükten); satır tutarı = `cariTlEtkisi`; peşin ödenmiş işlem "x ₺ · ödendi, borca yazılmadı"; her satırda "Bakiye …" (en yeni = kayıtlı TL bakiye, eskisi = sonraki − etki; etkisi bilinmeyen satırdan eskisi "—").
- Kâğıt (`CiktiModulu.Ekstre`): sütunlar Tarih · İşlem (tür · ödeme) · Açıklama · Tutar · Has etkisi · Borç etkisi · Bakiye; etki kuralı ekranla tek (`CariBakiyeKurali.TlEtkisi`, `CariServisi.BakiyeHasEtkisi`).

## 3. Sapmalar ve gerekçeler
- **Port:** görev 5162/5163 diyordu; 5162'de SKB-tasarım şeridinin API'si (PID 57354, log `SKB-tasarim/api-5162.log`) çalışıyordu → DOKUNULMADI; kendi canlı doğrulamam 5164 (API) / 5165 (vekil), CDP 9901–9913.
- RaporModulu.cs'e dokunulmadı (sunucu havaleyi zaten dönüyordu); düzeltme `Raporlar.razor`'da.
- Yeni bileşen/istemci record yerine Contracts'ta tek tip `GunKapanisiOnizleme` (denetim P-10 / SozlesmeKapsam).
- `Islemler.razor` 1.400 satır tavanı: mantık `Islemler.ParaDemo.cs`'e taşındı; razor 1.399 satır.
- Önizleme yanıtı okunamazsa (JsonException) da hata sayılır.

## 4. Migration
Gerekmedi (şema değişikliği yok; `gun_kapanislari.defter_bakiye` anlamı "karşılaştırılan nakit defteri" olarak belgelendi).

## 5. Testler
- Yeni: **34** (`ParaDemoTestleri.cs` 20: DB 13 + HTTP/saf 7; `ParaDemoEkranTestleri.cs` 14 bUnit). Filtreli koşum: 34/34.
- Bu paketin kural değişikliğine göre GÜNCELLENEN mevcut testler (kural aynası, gerekçe yorumda): takasa satıcı (`ParaDemoTestVerisi.TakasSaticisi` = MÜKELLEF, sayısal alanları değiştirmez) — CiftBacak, KarZararIade, MasakEntegrasyon, MadenDefteri, Muhasebe, NakitPayiYazma, P23cGenellestirme, PosTahsilat, TasKokeni, Yasal3Masak, ZOISLEMAOdemeTutarKapisi, Dogrulayici/CIFTBACAKSinir, Dogrulayici/YASAL3Sinir; ekran takas akışlarına satıcı doldurma (`ParaDemoEkranYardim`) — SATIS3Ekran, Dogrulayici/SATIS3Sinir, S21FisBilesen; önizleme ucuna gerçekçi sahte yanıt — P22Ekran, S36KasaBildirim, ErisimKapatma (GunKapanisiEkran), ZoBelge (ZB03 POST sayımı), Dogrulayici/ZOBELGESinir; Q94 ekstre sütunları; IadeTestleri.Kapanmis_gune (kartlı satışta sayılan nakit = açılış).
- **Tam süit (son kodla):** 9041 toplam · **9039 geçti · 0 başarısız** · 2 atlandı (önceden atlanan `SATIS3EkranTestleri.DOG_T24…` vb.). trx: `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO/son2.trx`. Ara tam koşum (tur 1) başka şeridin yarım `Toplu3Testleri.cs` derleme hatasıyla koşamadı; ikinci koşumda 7 kırmızıdan 5'i benim (düzeltildi), 2'si (`DugmeIki D2_01`, `OlcekTek OL5` — index.html CSS sırası) başka şeridin ve son koşumda yeşildi; `SATIS3EkranTestleri.Barkod_dizisi…` bir kez kırmızı, tekrar koşumda ve son tam koşumda yeşil (zamanlama kararsızlığı, bu paketin dokunduğu yol değil).

## 6. Mutasyonlar (17/17 kırmızı, hepsi geri alındı, `cmp` AYNI)
Yöntem: `/private/tmp/kuyumcu-serit/mut-yedek/PARA-DEMO/` yedek → tek eşleşmeli değişiklik → `dotnet test --filter ParaDemo` → geri kopya → cmp. Sonuç `/private/tmp/kuyumcu-serit/PARA-DEMO/mut-sonuc.json`.

| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M1-1 | KasaNakitAyrimi havaleyi nakit sayar | PD1_nakit_1000_kart_500_havale_300…, PD1_toplam_bakiye_sayilirsa…, PD1_eksik_nakit…, PD1_onizleme…, PD1_HTTP_onizleme_ve_gun_kapat… |
| M1-2 | GunKapat kasa.Bakiye ile karşılaştırır | PD1_nakit_1000…, PD1_toplam_bakiye…, PD1_eksik_nakit…, PD1_HTTP_onizleme_ve_gun_kapat… |
| M1-3 | Kasalar GunFark toplamla | PDE1_kart_ve_havale_AYRI_satir…, PDE1_toplam_bakiye_yazilirsa_FARK… |
| M1-4 | KasaRaporu farkı toplamla | PDE1_kasa_raporu_sayimi_NAKIT… |
| M1-5 | Mutabakatta havale 0 | PDE1_mutabakat_alt_kalemleri_HAVALE… |
| M2-1 | TL borçluda altın uyarısı çıkmaz | PDE2_TL_borclu_musteride_altin… |
| M2-2 | Altınla tahsilata NAKIT etiketi | PD2_has_ile_tahsilat_TL_borcu_DEGISTIRMEZ… |
| M3-1 | Takas kimlik kapısı kalkar | PD3_takas_kimliksiz…, PD3_takas_TCKNsiz…, PD3_takas_mukellef_fatura_nosuz…, PD3_HTTP_takas_kimliksiz_400 |
| M3-2 | Esnaf muaf stopajlı takas geçer | PD3_takas_esnaf_muaf_stopajli_REDDEDILIR |
| M3-3 | Takasta pusula yazılmaz | PD3_takas_vatandastan_GIDER_PUSULASI… |
| M3-4 | Takas ekranı kimliksiz açılır | PDE3_takas_satici_kimligi_yokken… |
| M4-1 | Peşin+veresiye kilidi kalkar | PDE4_Pesin_Musteri_VERESIYE… |
| M5-1 | Kâğıtta borç etkisi = ham tutar | PD5_ekstre_isleme_odeme_yazilir… |
| M5-2 | Kâğıtta yürüyen bakiye ters | PD5_ekstre_isleme_odeme_yazilir… |
| M5-3 | Ekranda yürüyen bakiye ters | PDE5_detayda…, PD5_ekstre… |
| M5-4 | Peşin satış borç gibi görünür | PDE5_detayda…, PD5_ekstre… |
| M5-5 | Kart "Nakit" yazılır | PDE5_detayda…, PD5_ekstre… |

⚠ Tuzak (rapora not): `shutil.copy2` yedeğin ESKİ mtime'ını geri yazdığı için MSBuild artımlı derleme mutasyonlu DLL'i "güncel" sanıyordu; son mutasyondan sonra tüm geri alınan dosyalara `touch` + temiz build yapıldı, canlı ve tarayıcı ölçümleri bu temiz derlemeyle YENİDEN alındı (ilk tarayıcı turu atıldı).

## 7. denetim.sh
Son kodla: 303 ✓, tek ✗ **P-30 DEMO_INSAN_SURESI** (kapsam dışı, kabulde hariç). Ara koşumlarda görülen ✗'ler: P-10/OLCUM_TABANI (benim 2 sayfa record'um → Contracts'a taşındı, düzeldi), P-23 ODEME_KALEMSIZ_YOL (benim test kiracımdaki altınla tahsilatın NAKIT etiketi → kökten düzeltildi, madde 2), "@onclick'siz button 72/71" ve DUGME2_BAGLANTI_SIRASI (başka şeridin Hurda.razor/IslemDefteri.razor/index.html düzenlemesi, son koşumda yok).

## 8. Canlı doğrulama (API 5164, Development, kuyumcu_net; kiracı `PAKETPARA-DEMO-TEST Kuyumculuk`, `demo-tohumla.js --prova-kiraci`)
Son temiz derlemeyle (kiracı 01M3W9YQ…):
| İstek | Beklenen | Gerçek |
|---|---|---|
| POST /islemler/satis HAVALE | 200 | 200 |
| GET /kasalar/{id}/gun-kapanisi/onizleme | toplam = nakit+kart+havale | 266.658,42 = 193.505,02 + 22.711,80 + 50.441,60 |
| POST /kasalar/{id}/gun-kapanisi sayılan = SQL nakit (gerekçesiz) | 200, fark 0 | 200, fark 0; SQL `gun_kapanislari` 193505.02 / 193505.02 / 0.00; ardından gün gerekçeyle yeniden açıldı (200) |
| GET /raporlar/pos-fatura-mutabakati | alt kalemler = toplam | 193.505,02+22.711,80+50.441,60+0 = 266.658,42 |
| POST /cari/{Kemal}/tahsilat hasGrami 1 | TL aynı, has −1 | TL 10.429,13 → 10.429,13; has 0 → −1 (SQL cari_bakiyeler); hareket odeme_turu NULL |
| POST /islemler/takas satıcısız | 400 | 400 "satıcı kimliği zorunludur (VUK md.234…)" |
| POST /islemler/takas MÜKELLEF + KART | 200, pusula yok | 200, `giderPusulasi` null; SQL takas açıklaması "… satıcı faturası PD2026000001", pusula sayısı 0 |
| POST /islemler/takas ESNAF_MUAF (önceki temiz koşum) | 400 | 400 "stopaj kesilir … ayrı ayrı girin" |
| POST /islemler/satis Peşin + VERESIYE | 400 (sunucu kapısı aynen) | 400 |
| GET /cari/{Kemal}/ekstre | Σ cariTlEtkisi = bakiye | 10.429,13 = 10.429,13 (ham tlTutari toplamı 28.346,64) |
| GET /cikti/CARI_EKSTRE/{Kemal} | son bakiye = TL bakiye | "Satış · Veresiye … 14.899,13" … "Tahsilat · Altın … 10.429,13" |
Vatandaş (TCKN'li) takas canlıda girilmedi (kimlik numarası yazılmadı); DB testi `PD3_takas_vatandastan_GIDER_PUSULASI…` kanıtlar.
Temizlik: API/vekil durduruldu (5164/5165 ve CDP portlarında dinleyen yok); `zsh kiraci-temizle.sh … --uygula` → 432 satır + kiracı silindi; tenant_id taşıyan 75 tablonun hepsinde 0 satır; `PAKETPARA%` kiracı 0. tenants: başlangıç 14 → son 16 (fark başka şeritlerin: PAKETTOPLU3H-TEST, PAKET-SKB-TEST…). Fikstür/anahtar dosyaları `printf ''` ile boşaltıldı.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, cdp.mjs + sikisma-olc.js; vekil 5165)
8 senaryo × 1280/375 × BASİT/PRO × açık/koyu = 60 ölçüm (peşin-veresiye yalnız PRO: BASİT'te ödeme seçicisi yok, ödeme Nakit sabit). Tablo: `/private/tmp/kuyumcu-serit/PARA-DEMO/olcum-tablo.txt`, görüntüler `/private/tmp/kuyumcu-serit/PARA-DEMO/ekran/foto/`.
Tüm satırlarda: K1 gizli 0 · K5 TRY 0 · K7 hiza 0 · K10 0 · K11 hata 0 · K12 0 · yatay 0. Gerekçeli sıfır-dışılar:
- K9 "binen" 3–6 (çekmece/pencere açık ekranlar): çiftlerin hepsi ARKA SAYFA öğesi ⟂ ÜST KATMAN öğesi (ör. `#kasa-yeni` ⟂ `#gun-kart`) — katman örtüşmesi, yazı binmesi değil; görüntülerde katman içinde binme yok.
- K3 kırık 1 (375, tahsilat penceresi): pencere başlığı "Tahsilat — Kemal Ardıç" iki satır — mevcut başlık, bu paket değiştirmedi.
- K8 jargon 1 (BASİT, kâğıt ekstre): test kiracısının adındaki "TEST" kelimesi (fikstür), ürün metni değil.
Görülen görüntüler (Read): Günü Kapat 1280 açık / 375 açık+koyu (fark 0 ₺, kart/havale ayrı), kasa raporu sayım 1280, gün sonu mutabakatı 1280 koyu ("Nakit 207.290,29 · Kart 36.485,68 · Havale 50.414,78" = 294.190,75), tahsilat 375 (uyarı kutusu), cari detay 1280 koyu / 375 açık (ödeme · etki · bakiye), kâğıt ekstre 1280 açık+koyu / 375 koyu, takas BASİT 375 (Satıcı kimliği), Peşin+Veresiye PRO 1280 (Kaydet kilitli + sebep).

## 10. DoD sözleşmesi
- Yeni varlık/kayıt türü YOK. Yeni uç `GET …/gun-kapanisi/onizleme` salt okunur (türetilmiş), `[ModulYetkisi("kasa")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`.
- Takas gider pusulası mevcut varlık: düzeltme yolu mevcut (`/gider-pusulalari` iptal + yeniden düzenle, HurdaModulu) — takas satırına bağlı pusula da aynı yoldan; takas satırı finansal: storno yok (DuzeltmePolitikasi TAKAS = StornoYok, ekranda gerekçe mevcut).
- Gün kapanışı: yeniden açma yolu mevcut (SAHİP, gerekçeli).
- Her yeni düğme/kilit: Kaydet kilitleri sebep yazar (UX-S30); boş durum/hata bantları (önizleme hatası) var; yeni CSS yok, satır içi style yok, Sozlukler.cs HUB'a anahtar eklenmedi (`Sozlukler.ParaDemo.cs` TR+EN).

## 11. Koşul karşılama
| Koşul | Durum |
|---|---|
| Her madde önce/sonra ölçüm | KARŞILANDI (§0, §8) |
| Canlı API + bağımsız SQL | KARŞILANDI (§8) |
| Kabul: nakit 1000 + kart 500 + havale 300 → sayılan 1000 fark 0 | KARŞILANDI (PD1 DB+HTTP+bUnit, canlıda gerçek veriyle) |
| Sınır testleri | KARŞILANDI (toplam sayılırsa +800 fazla, eksik −100, kart tahsilat, TCKN'siz, esnaf, mükellef fatura nosuz, borçsuz müşteride uyarı yok, önizleme hatası) |
| Mutasyon (kritik kural başına ≥1 kırmızı, cmp AYNI) | KARŞILANDI 17/17 |
| Tarayıcı 1280+375 · BASİT+PRO · açık+koyu, görüntü Read | KARŞILANDI (§9) |
| Build 0/0 | KARŞILANDI uygulama projeleri 0 uyarı 0 hata; çözümde 2 uyarı başka şeridin test dosyalarında (SUBEKAPSAMASinirTestleri.cs:49, KesifP2Testleri.cs:454) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI |
| Tam süit 0 yeni kırmızı | KARŞILANDI (9039/9041, 0 başarısız) |
| Mevzuat birincil kaynak | KARŞILANDI (VUK 234, TBK 282–283; mevzuat.gov.tr) |
| Test kiracısı silindi | KARŞILANDI |

## 12. Değişen dosyalar (bu paket)
API: `Modules/Kasa/KasaNakitAyrimi.cs` (yeni), `Modules/Kasa/KasaModulu.cs`, `Modules/Satis/SatisModulu.cs` (TakasDto.Satici + Takas), `Modules/Cari/CariModulu.cs` (tahsilat etiketi), `Modules/Cikti/CiktiModulu.cs` (ekstre).
Contracts: `Sayfalar/KasalarSozlesmeleri.cs` (GunKapanisiOnizleme), `Sayfalar/CarilerSozlesmeleri.cs` (EkstreHareket.OdemeTuru).
Core: `I18n/Sozlukler.ParaDemo.cs` (yeni).
Web: `Pages/Kasalar.razor`, `Pages/KasaRaporu.razor`, `Pages/Raporlar.razor` (yalnız gün sonu alt metni), `Pages/Cariler.razor` (tahsilat penceresi + detay satırları), `Pages/Islemler.razor`, `Pages/Islemler.ParaDemo.cs` (yeni), `Pages/Islemler.Diger.cs` (takas adres zorunluluğu yapılandırması), `Bilesenler/Fis/FisModelleri.cs` (TakasGirisi.Satici), `Bilesenler/Fis/FisTakasSatici.razor` (yeni).
Tests: `ParaDemoTestleri.cs`, `ParaDemoEkranTestleri.cs` (yeni) + §5'te sayılan güncellemeler.
Dokunulan sayfalar: Kasalar, KasaRaporu, Raporlar, Cariler, Islemler, Cikti (sunucu belgesi).

## 13. Açık kalanlar
- Takasta MASAK kimlik kutusu açıkken satıcı kimliği ayrıca sorulur (hurda alışındaki "tek kimlik formu" birleştirmesi takasa taşınmadı) — UX iyileştirmesi, sonraki tur.
- Kasadan bankaya virman NAKIT damgalı olduğundan, kart/havale parası virmanla kasadan çıkarılırsa nakit defteri düşer; bugün kart/havale bankaya ayrı yoldan gitmiyor (POS yan defter). Gerçek kullanımda ölçülmeli.
- BASİT modda peşin+veresiye ölçülemez (ödeme seçicisi yok) — kilit PRO'da ölçüldü.
- Başka şeritlerin: çözüm derlemesindeki 2 uyarı (SUBEKAPSAMASinirTestleri.cs:49 — ŞUBE-KAPSAM doğrulayıcısı; KesifP2Testleri.cs:454), ara koşumlarda TOPLU-3 / SKB test dosyalarının yarım derleme hataları (son koşumda yok).

## 14. Kurucu / mali müşavir
1. Takasta esnaf muafiyetli satıcıdan alınan eski altının stopajı takas farkından mahsup edilebilir mi? (Bugün: reddedilir, ayrı Hurda Alış + Satış istenir.)
2. Mükellef satıcıyla takasta satıcının faturası yeterli mi? (Bugün: fatura no takas açıklamasına yazılır.)
3. Müşterinin TL borcunu altınla kapatması: Hurda Alış (pusula) + TL tahsilat yolu doğru mu, yoksa tek belgede mahsup mu tercih edilir?

---

# TUR 2 — hasım bulgularının kapanışı (02.10.2026 05:00–05:15, şerit t2)

## T2.0 Özet
| Bulgu | Önem | Durum | Kanıt |
|---|---|---|---|
| Komisyonlu elle KART girişinde hareket BRÜT, bakiye NET → nakit defteri kayıyor | KRİTİK | KAPANDI — yol (a): `KasaModulu.HareketGir` hareket satırına bakiyeye yazılan NET tutarı yazar (Σ hareket = bakiye); komisyon oranı satırda kalır | `PDS_hasim_komisyonlu_elle_kart_girisi_nakdi_degistirmez` Skip'siz YEŞİL; PD7 ×2; canlı: 1000 TL %2 kart → hareket 980, nakit defteri 1000 değişmedi, SQL Σhareket 2280 = bakiye 2280 |
| GunKapat servis yolu eksi sayılanı kabul ediyor | DÜŞÜK | KAPANDI — `GunKapat` başında `SayilanNakit < 0 → BadRequestException` | `PDS_eksi_sayilan_nakit_servis_yolunda_da_reddedilmeli` Skip'siz YEŞİL; HTTP −5 → 400 |
| Virman NAKIT damgalı → kart/havale payı bankaya aktarılınca nakit defteri düşüyor | ORTA | KAPANDI — ekip oyu: "kasadan çıkan virmanda aracı seçtir" (yeni "tahsil edilecek" bacağı = yeni muhasebe kavramı, İCAT EDİLMEDİ). `VirmanDto.KasaAraci` NAKIT (varsayılan, bugünkü davranış) · KART · HAVALE; pay sınırı sunucuda; bankadan çıkan virmanda KART/HAVALE 400; kasa→kasa aktarımda hedef bacak da aynı araçla; ekranda seçici yalnız kasada kart/havale payı varsa görünür | PD6 ×5, PDE6 ×5; canlı: KART 500 virman → kart payı 1480→980, nakit 1000 değişmedi; payı aşan 5000 → 400; bankadan KART → 400; ALTIN → 400 |
| Nakit çıkış kapısı toplam bakiyeye bakıyor | DÜŞÜK | AÇIK (bulgunun kendisi "sonraki paket" diyor; spesifikasyon istemiyor) — aşağıda §T2.9 | — |
| 5199 kapalı / 5198'de yabancı API | DÜŞÜK | ORKESTRATÖR işi; bu şerit 5199/5198'e DOKUNMADI (ölçümde ikisi de boştu) | `lsof` |

## T2.1 Mevcut durum doğrulaması
Tur 2'nin kod değişiklikleri ağaçta hazır bulundu (01.10 21:39–21:48, 02.10 05:01 Gider.ParaDemo.cs); bu oturumda okunarak doğrulandı, derlendi (0 uyarı 0 hata), filtreli testler 63/63 yeşil, sonra mutasyon + tam süit + canlı + tarayıcı kanıtı üretildi. Kod değişiklikleri:
- `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — HareketGir `Tutar = netTutar`; GunKapat eksi kontrolü.
- `Kuyumcu.Api/Modules/Gider/VirmanModulu.cs` — `VirmanDto.KasaAraci` (+ `[RegularExpression]` ve servis içi elle kontrol), kart/havale payı kapısı (`KasaNakitAyrimi.Hesapla`), bankadan çıkışta red, `BacakYap(..., kasaAraci)`.
- `Kuyumcu.Api/Modules/Kasa/KasaNakitAyrimi.cs` — virman kapısı için kasa kimliği + bakiye aşırı yüklemesi.
- `Kuyumcu.Web/Pages/Gider.razor` (+ yeni `Gider.ParaDemo.cs`) — "Ne aktarılıyor?" seçicisi (`gd-v-arac`), ipucu (`gd-v-arac-ipucu`), ayrım okunamazsa hata bandı (`gd-v-arac-hata`), payı aşan tutarda Kaydet kilidi + sebep (`gd-v-kaydet-kilit`); gövdeye `kasaAraci`. Mevcut id'ler değişmedi.
- `Kuyumcu.Core/I18n/Sozlukler.ParaDemo.cs` — virman metinleri TR+EN (HUB'a anahtar eklenmedi).
- Testler: `ParaDemoTur2Testleri.cs` (7 HTTP+DB), `ParaDemoTur2EkranTestleri.cs` (5 bUnit), `Dogrulayici/PARADEMOSinirTestleri.cs` (2 Skip kaldırıldı).
TEK DOKUNUŞ gerekçesi (Gider.razor): virman formu başka dalgada açılmıştı; bu dokunuş hasım ORTA bulgusunun (VirmanModulu NAKIT damgası) ekran ayağıdır — gerekçe `Gider.ParaDemo.cs` başlığında da yazılı.

## T2.2 Migration
Gerekmedi (şema değişikliği yok; `kasa_hareketleri.odeme_turu` mevcut kolon).

## T2.3 Testler
- Yeni (tur 2): 12 (`ParaDemoTur2Testleri` 7 + `ParaDemoTur2EkranTestleri` 5) + 2 Skip kaldırılan hasım testi. Filtreli koşum (`ParaDemo|PARADEMO`): **63/63 geçti**.
- Tam süit (son kodla, tek koşum): **9084 toplam · 9082 geçti · 0 başarısız · 2 atlandı** — trx `/private/tmp/kuyumcu-serit/trx-pPARA-DEMO/tur2.trx` (Counters failed=0). Yeni kırmızı: 0.

## T2.4 Mutasyonlar (9/9 kırmızı, hepsi geri alındı, `cmp` AYNI)
Yöntem: `/private/tmp/kuyumcu-serit/mut-yedek/PARA-DEMO-t2/` yedek → `perl -0777 -pi` tek eşleşme → `dotnet test --filter "ParaDemoTur2|PARADEMOSinir"` → yedeği geri kopyala → `cmp`. Son mutasyondan sonra dosyalar `touch` + temiz build (0/0), filtreli 63/63.
| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M1 | HareketGir hareketi yine BRÜT yazar | PDS_hasim_komisyonlu_elle_kart_girisi_nakdi_degistirmez · PD7_komisyonlu_elle_KART_girisi_hareketi_NET_yazar… · PD7_komisyonlu_elle_hareketin_STORNOSU… |
| M2 | GunKapat eksi kontrolü etkisiz | PDS_eksi_sayilan_nakit_servis_yolunda_da_reddedilmeli |
| M3 | Virman bacağı hep NAKIT damgalı | PD6_kart_payi_bankaya_virmanla_aktarilinca_nakit_defteri_DEGISMEZ… · PD6_kart_virmaninin_STORNOSU_kart_payini_geri_getirir |
| M4 | Pay sınırı gevşer | PD6_kart_payini_ASAN_virman_400_hicbir_sey_yazilmaz |
| M5 | Bankadan çıkan virmanda KART kabul | PD6_bankadan_cikan_virmanda_KART_araci_400 |
| M6 | Ekranda pay kilidi kalkar | PDE6_kart_payini_ASAN_tutarda_Kaydet_KILITLI_sebep_yazar_POST_yok |
| M7 | Araç seçicisi hiç görünmez | PDE6_kart_payini_ASAN… · PDE6_kasada_kart_payi_varsa_arac_secicisi_gorunur_KART_govdeye_gider |
| M8 | Ayrım okunamazsa hata yutulur | PDE6_ayrim_okunamazsa_HATA_bandi_cikar_sifir_uydurulmaz |
| M9 | Gövdeye kasaAraci gönderilmez | PDE6_kasada_kart_payi_varsa_arac_secicisi_gorunur_KART_govdeye_gider |
Özet dosyası: `/private/tmp/kuyumcu-serit/PARA-DEMO/mut/ozet.txt`.

## T2.5 denetim.sh
302 ✓, 2 ✗: **P-30 DEMO_INSAN_SURESI** (kapsam dışı, kabulde hariç) ve **P-13 IC_KOD_SIZINTISI = 1 (Kurulum.razor:927)** — BU PAKETİN DEĞİL: `Kurulum.razor` 01.10 21:38'de TOPLU-3 şeridince değiştirildi (`YakindaMi(...) => a.Anahtar == "STOK_ACILIS"` C# satırı; sayaç `=>`'daki `>`'ı çıktı metni sanıyor — yalancı pozitif ya da satır `@code` dışına taşınmalı; sahibi TOPLU-3). PARA-DEMO'nun dokunduğu dosyalarda ✗ yok.

## T2.6 Canlı doğrulama (API **5162**, Development, kuyumcu_net; kiracı `PAKETPARA-DEMO-T2-TEST Kuyumculuk`, davet + kayıt API ile; sır değerleri yazılmadı)
| İstek | Beklenen | Gerçek |
|---|---|---|
| GET …/gun-kapanisi/onizleme (boş kasa) | 0/0/0/0 | 200 `{toplam 0, kart 0, havale 0, nakit 0}` |
| POST /kasalar/{id}/hareket NAKIT 1000 · KART 500 · HAVALE 300 | 200 | 200 ×3 (bakiye 1000→1500→1800) |
| GET önizleme (kabul senaryosu) | nakit 1000 | 200 `{toplam 1800, kart 500, havale 300, nakit 1000}` |
| POST hareket KART 1000 komisyon 0,02 (**KRİTİK**) | bakiye +980, nakit aynı | 200 bakiye 2780; önizleme `{toplam 2780, kart 1480, havale 300, nakit 1000}` |
| POST /bankalar | 200 | 200 |
| POST /virman KASA→BANKA 500 kasaAraci=KART (**ORTA**) | 200, nakit aynı | 200; önizleme `{toplam 2280, kart 980, havale 300, nakit 1000}` |
| POST /virman KART 5000 (payı aşan) | 400 | 400 "'Ana Kasa' kasasındaki kart tahsilatı 980.00 TRY; 5000.00 TRY aktarılamaz." |
| POST /virman BANKA→KASA kasaAraci=KART | 400 | 400 "Kart / havale payı yalnız KASADAN çıkan virmanda seçilir…" |
| POST /virman kasaAraci=ALTIN | 400 | 400 alan hatası `kasaAraci` |
| POST …/gun-kapanisi sayılan −5 | 400 | 400 "Sayilan nakit 0 veya daha büyük olmalıdır" |
| POST …/gun-kapanisi sayılan **1000** (kabul: nakit 1000 + kart + havale) | 200, fark 0 | 200 `{defterBakiye 1000, toplamBakiye 2280, kartToplam 980, havaleToplam 300, fark 0}` |
Bağımsız SQL (`docker exec kuyumcu-mysql …`):
```
kasa_hareketleri:  NAKIT 1000.00 (kom 0) · KART 500.00 · HAVALE 300.00 · KART 980.00 (kom 0.0200) · KART -500.00 (virman)
Σ tür:             NAKIT 1000.00 · KART 980.00 · HAVALE 300.00
kasalar.bakiye 2280.00 = Σ kasa_hareketleri.tutar 2280.00     banka_hesaplari.bakiye 500.00
gun_kapanislari:   2026-10-02 · sayılan 1000.00 · defter 1000.00 · fark 0.00
```
Temizlik: API (PID 32423/32433) ve vekil durduruldu — 5162/5163/9905'te dinleyen yok. `zsh kiraci-temizle.sh 01M3X5XM285YQ2BZXSPQFXYCZQ --uygula` → kiracı silindi; tenant_id taşıyan **75 tablonun toplamı 0 satır**; tenants **17 → 17** (başlangıçla aynı). Fikstür ve sistem anahtarı dosyaları `printf ''` ile boşaltıldı. QA-TEST-* kiracılarına dokunulmadı.

## T2.7 Gerçek tarayıcı ölçümü (başsız Chrome `cdp.mjs`, CDP 9905, vekil 5163, `sikisma-olc.js`)
Senaryo: /gider?sekme=virman → Kaynak = Ana Kasa → "Ne aktarılıyor?" = Kartla alınan para (kasada 980 ₺) → Hedef = banka hesabı → Tutar 5000 (payı aşan). Betik `/private/tmp/kuyumcu-serit/PARA-DEMO/olc.mjs`, satırlar `ekran/olcum.jsonl`, görüntüler `ekran/foto/gider-virman__<MOD>_<tema>_<genişlik>.png`.
| Mod (gerçek) | Tema | Genişlik | Kilit sebebi | Kaydet | K1 | K2 | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10 | K11 | K12 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASIT | açık | 1280 | "Seçilen ödemeden kasada yalnız 980 ₺ var." | kilitli | 0 | 98 | 0/0 | 0 | 0 | 2* | 0 | 0 | 0 | 0 | 0 |
| BASIT | açık | 375 | aynı | kilitli | 0 | 97 | 0/0 | 0 | 0 | 2* | 0 | 0 | 0 | 0 | 0 |
| BASIT | koyu | 1280 | aynı | kilitli | 0 | 98 | 0/0 | 0 | 0 | 2* | 0 | 0 | 0 | 0 | 0 |
| BASIT | koyu | 375 | aynı | kilitli | 0 | 97 | 0/0 | 0 | 0 | 2* | 0 | 0 | 0 | 0 | 0 |
| PRO | açık | 1280 | aynı | kilitli | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | açık | 375 | aynı | kilitli | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 1280 | aynı | kilitli | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 375 | aynı | kilitli | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
\* K8 "jargon" = test fikstürünün banka hesabı adındaki "PARA-DEMO" kelimesi (ürün metni değil).
Görülen görüntüler (Read): BASIT açık 1280 (seçici "Kartla alınan para (kasada 980 ₺)", ipucu, kilit sebebi Kaydet'in yanında), PRO koyu 375 (seçici ve ipucu sığıyor, yatay kaydırma yok).
Gözlem (bu paketin değil, önceden var): virman formunda Hedef etiketi iki kez basılıyor (`<label for="gd-v-hedef">` + Secici `Etiket`) — açık kalanlar.

## T2.8 DoD sözleşmesi (tur 2)
- Yeni varlık/kayıt türü YOK. `VirmanDto.KasaAraci` mevcut virman kaydının bir alanıdır; virmanın düzeltme yolu mevcut storno ucudur (`POST /virman/{bacakId}/storno`, [RolGerekli SAHIP,MUDUR,MUHASEBE]); storno aracı aynen ters çevirir (PD6_kart_virmaninin_STORNOSU… testi). Finansal kayıt hard DELETE yok.
- Elle kasa hareketinin düzeltme yolu mevcut storno (`/kasalar/{id}/hareket/{hareketId}/storno`); NET yazımla storno bakiyeyi ve Σ hareketi birlikte sıfırlar (PD7 storno testi).
- Kilitli düğme sebebini yazar (UX-S30), ayrım okunamazsa hata bandı (UX-S28), seçici yalnız gerektiğinde (UX-S22, zorunlu önce); yeni CSS / satır içi style yok.

## T2.9 Açık kalanlar
- **Nakit çıkış kapısı toplam bakiyeye bakıyor** (hasım DÜŞÜK, SatisModulu KasaBakiyeKapisi.Dogrula(kasa, tutarDelta); elle CIKIS ve NAKIT virman da aynı): nakit çıkışlarında `KasaNakitAyrimi.Nakit` ile kapı sonraki pakette. Bu turda yapılmadı: hurda alış/iade/gider/virman yollarının hepsini etkiler; spesifikasyon istemiyor; bulgunun kendisi "sonraki paket" diyor.
- Eski kayıtlar: 02.10 öncesi komisyonlu elle KART hareketleri BRÜT yazılmış olabilir (geri dönük veri düzeltmesi YOK kuralı). Etkilenen kasada nakit defteri komisyon kadar sapar; `kasa_hareketleri` içinde `komisyon_oran > 0` ve `odeme_turu = KART` olan satırlarda kasalar.bakiye ≠ Σ tutar farkıyla tespit edilip düzeltme fişiyle kapatılması kurucu kararı.
- Kasadan bankaya NAKIT virmanın üst sınırı yine toplam bakiye (yukarıdaki maddeyle aynı kök).
- Gider virman formunda "Hedef" etiketi iki kez görünüyor (önceden var; ORTAK-1/S1-i dönemi). Tek satır silinerek düzelir; tam süit tur başına bir kez kuralı yüzünden bu turda dokunulmadı.
- Başka şeridin: denetim **P-13 IC_KOD_SIZINTISI 1 (Kurulum.razor:927, TOPLU-3)**; P-30 (kapsam dışı). 5199 kapalı (orkestratör).

## T2.10 Kurucu / mali müşavir
4. Elle kart hareketinde komisyon bugün kasaya NET yazılıyor (komisyon gider olarak ayrıca kaydedilmiyor). Komisyonun gider hesabına ayrı bacak olarak yazılması isteniyor mu? (Bugünkü davranış korunuyor; yeni muhasebe kavramı icat edilmedi.)
