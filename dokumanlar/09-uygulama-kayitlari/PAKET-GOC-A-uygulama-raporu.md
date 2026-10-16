# PAKET GOC-A — Para ekranlarının ortak girdi + ortak ızgara göçü · uygulama raporu (08.10.2026)

> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t2 · canlı API 5162 · vekil 5163 · CDP 9901–9915 · `KUYUMCU_TEST_DB=kuyumcu_test_t2`.
> Kaynak (tek doğruluk): `dokumanlar/09-uygulama-kayitlari/GIRDI-KURALLARI-ENVANTER-2026-10-08.md` — (B) K1–K34, (C) bu ekranların 185 alan satırı, (E) Faz 2 GOC-A, (F) F2/F3, (G) G1 + G2'nin 28 satırı.
> COMMIT YOK. Dosya silinmedi/taşınmadı. 5199/5200 portlarına dokunulmadı. Sır/parola değeri hiçbir çıktıya yazılmadı (fikstür dosyaları chmod 600).


## 0. TUR 2 (08.10 akşam) — hasım doğrulayıcının kalan 4 bulgusu kapatıldı

| Bulgu | Önem | Yapılan | Dosya |
|---|---|---|---|
| G1: sunucu sınırına değen listede istemci sıralaması (Banka hareketleri 100, Hurda defteri 100, Gider pusulaları 100, Emanet son 200) | ORTA | Faturalar'daki "Tam" kalıbı 4 listeye uygulandı: liste sınıra değdiyse `SunucuSiralamaAdi = null` (sıralama düğmesi ÇIKMAZ) ve `IzgaraYerelSiralama.Uygula(…, null, …)`; sınır notu (UX-S29) `GocASozlugu.SiralamaSinirNotu` "Son {0} kayıt gösteriliyor. Liste uzun olduğu için sıralama kapalı." (#bh-sinir-notu, #gp-sinir-notu, #emanet-sinir-notu; Hurda'da sunucunun mevcut #hrd-sinir-notu'su). Banka: `hareketler.Count < 100`; Pusula: istemci `limit=PusulaSiniri(100)` açıkça gönderir, `Count < 100`; Hurda: sunucunun `dahaFazlasiVar` bayrağı; Emanet: sunucu yalnız emanet satırlarını süzdüğü için istemci bilemez → yanıta **`sinirDegdi` (okunan ham satır ≥ 200) + `limit`** eklendi (geriye uyumlu, `EmanetYaniti` varsayılanlı). Yanlış "TAM liste" yorumları düzeltildi. | Pages/Banka.razor, Hurda.razor, GiderPusulalari.razor, Emanet.razor · Modules/Emanet/EmanetModulu.cs · Contracts/Sayfalar/EmanetSozlesmeleri.cs |
| Kasa raporu 375 px'te 13+ haneli bakiyede kasa adı "Ana / Kasa" kırılıyor | DÜŞÜK | ≤640 px'te kasa satırı sarmalı esnek satır: ad doğal genişliğinde, bakiye sığmazsa alt satıra sağa yaslı iner (`.zo-kasa-satir` flex-wrap). Yalnız KasaRaporu kullanıyor (grep). | wwwroot/css/zo-belge.css |
| Servis kapısında çıplak Türkçe uzunluk iletisi + POS iletisinde iç kodlar | DÜŞÜK | `GirdiKapisi.Uzunluk` iletisi artık `GirdiSozlugu` `AdUzun` ("En çok {0} karakter yazılır.") — ekranın `SatirIciDuzenle EnFazla` iletisiyle AYNI anahtar; sabit etiket kalktı (6 çağrı güncellendi; doğrulayıcı testinin çağırdığı 4 parametreli imza aynı iletiyle korunur). POS bilinmeyen durum: `GocASozlugu.PosDurumBilinmiyor` + `Sozlukler.Tr.Pos.Durum*` etiketleri ("Bu durum listede yok. Seçilebilenler: Bekliyor, Kısmi yattı, Yattı, İptal."), ham girdi ve kod yanıtta yok. | Common/Dogrulama/GirdiKapisi.cs, Modules/Pos/PosModulu.cs, Fatura/Kasa/BankaModulu.cs (çağrılar) |
| KRİTİK (tasarım hakemi): Kur "Manuel Kur Gir" çekmecesinde ham kod MANUAL + "denetim izi" | KRİTİK | BASİT'te tek sade cümle `GocASozlugu.KurManuelSade` "İnternet yokken fiyatı buradan elle yazın." (#m-uyari); PRO'da `Kur.ManuelUyari` — ham kod MANUAL ve "denetim izi" HER İKİ dilde metinden çıkarıldı ("«elle girildi» diye işaretlenir ve işlem geçmişine yazılır"). Ölçümde çekmece AÇIK hâli ayrıca ölçüldü (K8 kör noktası). | Pages/Kur.razor, Core/I18n/Sozlukler.cs (yalnız iki metin), Sozlukler.GocA.cs (+4 anahtar TR+EN) |

**Testler (yeni, `Kuyumcu.Tests/GocATur2Testleri.cs`, 9 yöntem / 11 koşum — hepsi yeşil):** GT01 uzunluk iletisi sözlük · GT02 yeni metinler TR+EN, ManuelUyari'de MANUAL/denetim izi yok · GT03 (HTTP) POS durum=XYZ 400, ileti etiketli, gövdede BEKLIYOR/KISMI/YATTI/IPTAL/XYZ yok · GT04 (HTTP) Emanet yanıtı `sinirDegdi=false, limit=200` · GT05/GT06 Banka 3 satırda sıralama var/not yok, 100 satırda sıralama YOK + not · GT07 Gider pusulaları 2 ↔ 100 · GT09 Hurda `dahaFazlasiVar` false/true · GT08 Kur çekmecesi BASİT/PRO metni.
İlgili geniş filtre (GocA|GOCA|Banka|Hurda|P20b|Pusula|KesifP3|Emanet|Kur|Pos|ZOISLEM|Fatura|Kasa): **2900 geçti / 0 başarısız / 1 atlandı.**

**Mutasyon (tur 2, `tur2/mut2.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-A-t2/`, TEK eşleşme → filtreli test → geri kopya → `filecmp` AYNI):**
| Kod | Kural kapatıldı | Kırmızı test | cmp |
|---|---|---|---|
| M-T2-1 | Banka `HareketListeTam => true` | GT06_Banka_hareketleri_sunucu_sinirina_degdi_100_… | AYNI |
| M-T2-2 | Pusula `PusulaListeTam => true` | GT07_Gider_pusulalari_sinira_degen_listede_… | AYNI |
| M-T2-3 | Hurda `DefterListeTam => true` | GT09_Hurda_defteri_…(dahaFazla: True) | AYNI |
| M-T2-4 | Emanet `sinirDegdi = true` | GT04_Emanet_gecmisi_yaniti_sinir_bayragi_… | AYNI |
| M-T2-5 | Uzunluk iletisi eski çıplak metin | GT01_Servis_uzunluk_iletisi_sozluk_… | AYNI |
| M-T2-6 | POS iletisi iç kodlarla | GT03_POS_bilinmeyen_durum_400_… | AYNI |
| M-T2-7 | Kur BASİT'te de ayrıntılı metin | GT08_Kur_manuel_…(mod: "BASIT") | AYNI |
7/7 kırmızı. Ders (tur 1 notu tekrarlandı): `copy2` mtime'ı geri götürdüğü için geri alınan 7 dosyaya `touch` + temiz `dotnet build Kuyumcu.sln` → **0 Uyarı 0 Hata**; tur 2 testleri temiz derlemede yeniden 11/11 yeşil.

**Tam süit (tur başına bir kez, son kod):** 11181 · **11174 geçti · 5 başarısız · 2 atlandı** (`/private/tmp/kuyumcu-serit/trx-pGOC-A-t2/_Ramazan-MacBook-Air_2026-10-08_17_02_54.trx`). 5 kırmızının **hiçbiri bu paketin değil** — hepsi başka şeridin 16:58'de `wwwroot/js/tema.js`'e eklediği **'yildiz' renk şeması** (belirtecler.css'te bloğu yok): `TasarimBelirtecTestleri.Sema_beyaz_listesi_CSS_ile_AYNI_kodlari_tasir` ("tema.js 'yildiz' şemasını yazıyor ama belirtecler.css'te blok YOK"), `B1KullaniciGorunumHttpTestleri.B1H2_…` (8→9), `B1GorunumEkranTestleri.BE2_…` (9→10), `S1fGorunumKartiEkranTestleri.S1f_E1_…` (8→9), `S1f_E14_…` (18→19) — şema sayısı bir arttı. Bu paket tema.js/belirtecler.css'e dokunmadı. Tur 1'deki 4 YasalParam kırmızısı bu koşumda yok. **Bu paketin yeni kırmızısı: 0.**

**denetim.sh (son kod):** yalnız `✗ P-30 DEMO_INSAN_SURESI` (kabul ölçütü dışı); HAM_TABLO 70, IBAN_REGEX_DTO 0, GIRDI_TUR 11, UZUNLUK 129 — sayaçlar artmadı.

**Canlı (API 5162 Development `kuyumcu_net`, vekil 5163; betik `tur2/canli_t2.py`, sonuç `tur2/canli-sonuc.json`):**
| Adım | Beklenen | Gerçek |
|---|---|---|
| 105 geçerli dekont POST | 105 başarılı | 105 |
| GET `/bankalar/{id}/hareketler` (limitsiz) | 100 satır (sunucu sınırı) | 100 |
| SQL `SELECT COUNT(*) FROM banka_hareketleri WHERE tenant_id=… AND banka_hesap_id=…` | 105 | 105 → liste TAM DEĞİL, ekranda sıralama kapalı olmalı |
| Dekont açıklama 501 karakter | 400 `alan:aciklama` | 400 "Açıklama en fazla 500 karakter olabilir" (controller yolu, DTO StringLength — bkz. açık kalan 10) |
| SQL 501 karakterli dekont yazıldı mı | 0 | 0 |
| POS `durum=XYZ` | 400, iç kod yok | 400 "Bu durum listede yok. Seçilebilenler: Bekliyor, Kısmi yattı, Yattı, İptal." · gövdede iç kod YOK |
| POS `durum=BEKLIYOR` (geçerli) | 200 | 200 |
| Emanet giriş (geçerli) / GET emanet | 2xx · `sinirDegdi=false, limit=200` | 200 · false/200 |
| Kasa uç bakiye girişi (1.000.000.001.500,49) | 2xx | 200 |
| Kasa oluştur (kiracıda kasa yoktu) | 2xx | 200 (ilk denemede `subeId` eksik → 400 "Şube zorunludur", düzeltildi) |
Not (md.1): betikte oluşturma uçları için "201" beklendi, API 200 döndü → betik 3 satırı "UYUMSUZ" yazdı; bu bir beklenti yazım hatasıdır, işlem başarılıdır (2xx). İlk koşum kasa yokken düştü → aynı kiracıyla yeniden koşuldu (iki kiracı açıldı, ikisi de silindi).

**Gerçek başsız Chrome (vekil 5163, CDP 9906–9908, `tur2/olc2.mjs` + `sikisma-olc.js`; ham `tur2/olcum/olcum.jsonl`, 56 ölçüm, 56 görüntü):** 7 görünüm (/banka · /raporlar/kasa · /kur · /kur + manuel çekmece AÇIK · /gider-pusulalari · /hurda · /emanet) × 1280/375 × BASİT/PRO × açık/koyu.
| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K |
|---|---|---|---|---|---|---|---|---|
| /banka (100 hareket) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /raporlar/kasa (1.000.000.001.500,49 ₺) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /kur | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /kur manuel çekmece AÇIK | K9 5* | K9 5* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* | K9 6* |
| /gider-pusulalari | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /hurda | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /emanet | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
"0" = K1 gizli-görünen 0 · K3 taşan/kırık 0/0 · K5 TRY 0 · K7 0 · **K8 jargon 0 (çekmece açıkken de)** · K9 0 · yatay kaydırma 0.
\* K9 "binen" çekmece açıkken ölçücünün örtü altını saymasıdır: binen çiftlerin sayfa tarafındaki öğeleri (#kur-simdi-cek, #kur-sekme-guncel, #kur-sekme-pariteler, #kur-has-eur) için `elementFromPoint` her iki genişlikte **çekmece/perde** döndürdü (`tur2/ortu.mjs`) — yazı üst üste görünmüyor, sayfa örtü altında. Çekmece kapalı /kur: K9 0.
Özel ölçümler (olcum.jsonl `ozel`): /banka 100 satırda `#bh-hareket-tablo .izgara-sirala` = **0**, `#bh-sinir-notu` = "Son 100 kayıt gösteriliyor. Liste uzun olduğu için sıralama kapalı." (8/8 kombinasyon); /raporlar/kasa 375'te "Ana Kasa" başlık yüksekliği **23 px (tek satır)**, K3_kirik 0 (tur 1'de 1); #m-uyari BASİT'te "İnternet yokken fiyatı buradan elle yazın.", PRO'da yeni ayrıntılı metin, iki modda da "MANUAL" yok.
Görüntüler (Read ile görüldü, 4): `kur_manuel__BASIT_aydinlik_1280.png` (çekmecede tek sade cümle, ham kod yok) · `raporlar_kasa__PRO_aydinlik_375.png` ("Ana Kasa" tek satır, bakiye alt satırda sağa yaslı) · `banka__BASIT_karanlik_375.png` (27404 px tam sayfa — okunamayacak kadar uzun, kanıt sayılmadı) · `banka_not__BASIT_karanlik_1280_gorus.png` (görüş alanı; profil o an PRO'daydı, koyu tema: 100 satırın altında sınır notu görünür, başlıklarda sıralama oku yok).

**Temizlik:** API (PID kill) ve vekil durduruldu, `lsof` 5162/5163 boş. `zsh kiraci-temizle.sh <id> --uygula` ile **01M4DWQNG1N7TBXJFZPZZVKCZ3** ve **01M4DWRQJZJKE7Z5Q20AJBPGM5** silindi; `PAKETGOC-A%` kiracı 0; tur 2 davet satırları (2) silindi → 0. tenants: başlangıç 17 → şimdi 16 — fark benim değil: iki kiracımı ekleyip ikisini sildim; eşzamanlı başka şerit bir kiracısını silmiş (benim komutlarım yalnız kendi id'lerime koştu). QA-TEST-* kiracılarına dokunulmadı.

**Tur 2 değişen dosyalar:** `Kuyumcu.Api/Common/Dogrulama/GirdiKapisi.cs` · `Modules/Pos/PosModulu.cs` · `Modules/Emanet/EmanetModulu.cs` · `Modules/Banka/BankaModulu.cs`, `Modules/Fatura/FaturaModulu.cs`, `Modules/Kasa/KasaModulu.cs` (yalnız Uzunluk çağrıları) · `Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs` · `Kuyumcu.Core/I18n/Sozlukler.GocA.cs` · `Kuyumcu.Core/I18n/Sozlukler.cs` (yalnız Kur.ManuelUyari TR/EN metni) · `Kuyumcu.Web/Pages/{Banka,Hurda,GiderPusulalari,Emanet,Kur}.razor` · `Kuyumcu.Web/wwwroot/css/zo-belge.css` (yalnız ≤640 px kasa satırı) · `Kuyumcu.Tests/GocATur2Testleri.cs` (YENİ). Dokunulan sayfalar tur 1'deki 13 sayfanın alt kümesi (aynı paket, hasım bulgusu gerekçesi).

## 1. Yapılanlar

### 1.1 Sunucu — "önce kapanacak açıklar" (envanter E · GOC-A satırı) — hepsi Core kuralı + sözlük metni, sessiz yuvarlama/kırpma YOK
| Açık | Önce | Şimdi | Dosya |
|---|---|---|---|
| **IBAN #1** (GIRDI-2 DÜŞÜK devri) | `HareketEkleDto/HareketDuzeltDto.KarsiIban` yalnız `StringLength(34,15)` | `[Iban]` (TCMB 2008/6 md.4 + MOD 97-10) + servis yolu `IbanDenetle(…, "karsiIban")` (kayıt biçimi boşluksuz/büyük harf); ekstre onayı (bankanın belgesi) bilinçli kapsam dışı | `Modules/Banka/BankaModulu.cs` |
| IBAN sayacı | `IBAN_REGEX_DTO` yalnız regex arıyordu | yeni kural: **Dto sınıfında `*Iban` adlı string özellik `[Iban]` taşımıyorsa sayılır** (fail-closed 0) | `dotnet/denetim.sh` |
| **Kur #8** | USD "zorunlu" yıldızı ama 0/boş geçiyor; eksi gram altın SESSİZCE null; has fiyatı ekran >0 / sunucu ≥1; 5. ondalık Math.Round ile yuvarlanıyor | `[Kur]` (K16 >0, tavan) tüm alanlarda + servis `GirdiKapisi.Kur(…, 4)` (kolon decimal(15,4)) → 0/eksi/5 ondalık **400**; USD yıldızı kaldırıldı (sunucu zorunlu tutmuyor — tutarlılık), boş = null gider | `Modules/Kur/KurModulu.cs`, `Pages/Kur.razor` |
| Kur geçmişi ters aralık | sessizce boş liste | K8e → 400 `alan:bitis` | `KurModulu.cs` |
| **Vezne sessiz kalem silme + kesirli adet** | ADET üründe 1,5 adet geçiyor; adet `Math.Round(…,3)`; ekranda eksik satır uyarısız atılıyor | `VezneKalemDto` `[Gram] Adet` `[Tutar] BirimFiyatTl` `[Tutar(SifirOlabilir)] IscilikTl` `[Tutar] TutarTl`; servis ürün **birimini** okur: ADET → K14 tam sayı, GRAM → K10 3 ondalık; ileti `"1. kalem · adet: …"`. Ekran: ürünü seçili ama adedi/fiyatı eksik satır artık atılmaz (Q105 cümlesi), ürünsüz-değerli satır yeni cümle; POST/PATCH YOK | `Modules/Vezne/VezneModulu.cs`, `Pages/Vezne.razor` |
| Vezne iptal gerekçesi | en az 10, ekran 1000, sunucu 500'de UYARISIZ kesiyordu | K30 en az 20 + kolon 500 → fazlası **400** (kesme yok); `gerekceAsgari` yanıtı 20 | `VezneModulu.cs` |
| Vezne kalemsiz tutar POST | tavan/ondalık yalnız PATCH'te | POST da `GirdiKapisi.Tutar` | `VezneModulu.cs` |
| **Stopaj 0,004** | ">0" yuvarlamadan önce → 0,00 TL'lik satır | `[Tutar]` + servis K9 YUVARLAMADAN ÖNCE (sözlük metni) | `Modules/Hurda/StopajModulu.cs` |
| **Faturalar ileri tarih #9** | taslak/yeni fatura tarihi sınırsız | `[TarihPenceresi(Islem)]` (POST + fişten) + servis `BaslikGirdisiDenetle` + taslak PATCH `GirdiKapisi.IslemTarihi` (ileri gün yok, en çok 1 yıl geri) | `Modules/Fatura/FaturaModulu.cs` |
| Fatura tutar/miktar/açıklama | POST'ta tavan yok (500 riski); fazla ondalık sessiz yuvarlama; kalem açıklaması 300'de SESSİZCE kırpılıyor | `[Tutar]` toplam, kalemde `[Gram] Miktar` `[Tutar(SifirOlabilir)]` fiyat/işçilik/indirim; servis POST + kalem ekle + PATCH aynı kapı; açıklama 300/500 aşımı **400** | `FaturaModulu.cs` |
| **Gider pusulası bitiş günü** | `TeslimTarihi < bitis` → bitiş günü düşüyordu | saat yoksa bitiş **gün sonuna** taşınır + K8e ters aralık 400 | `Modules/Hurda/HurdaModulu.cs` |
| **Tutar tavan + ondalık** | kasa hareketi / gider / virman 0,005 → 0,01 yuvarlanıyordu (GIRDI-2 sapma 2) | `[Tutar]` + servis `GirdiKapisi.Tutar` (yuvarlama yok); gün kapat `SayilanNakit` `[Tutar(SifirOlabilir)]`; kasa farkı gerekçesi K30 (20–1000); kasa oluşturmada en az 2 | `Modules/Kasa/KasaModulu.cs`, `Modules/Gider/GiderModulu.cs`, `VirmanModulu.cs` |
| Emanet | gram double.MaxValue tavan; taş ≥ gram "has hesaplanmaz" diye YANLIŞ sebeple red | `[Gram]` + K10 taş<brüt `TasBrut` metni; çıkış `[Has]` | `Modules/Emanet/EmanetModulu.cs` |
| Hurda tahlil ek farkı | üst sınır yok, 7+ ondalık sessiz yuvarlama | `GirdiKapisi.IsaretliHas` (6 ondalık, |tavan|; sıfır reddi fabrikanın) | `Modules/Hurda/TahlilFarkiServisi.cs` |
| Gider pusulası satıcı adı | "1" / "." geçiyordu | K19 `AdKurali` (pusula kapısı; ekran AdSoyad) | `Modules/Hurda/GiderPusulasiKapisi.cs` |
| Banka | karşı taraf "12345" geçiyordu; POST açıklama sınırsız; PATCH tarih toleransı 1 gün | `[Unvan]` hesap sahibi/karşı taraf (+servis), açıklama 500 (düzeltme 2000), tarih K8a (POST + PATCH) | `BankaModulu.cs` |
| POS tahsilat listesi | bilinmeyen durum / okunamayan tarih / ters aralık sessizce süzgeçsiz | 400 (`alan:durum/bas/bit`) | `Modules/Pos/PosModulu.cs` |

Ortak servis kapısı: **YENİ** `Kuyumcu.Api/Common/Dogrulama/GirdiKapisi.cs` (Tutar · Gram · Has · IsaretliHas · Adet · Kur · IslemTarihi · Aralik · Gerekce · Uzunluk) — DataAnnotations yalnız controller yolunda koştuğu için servis yolu AYNI Core kuralını çağırır, mesaj `GirdiSozlugu.Tr`.
Sınır sabitleri: `AlanSinirlari` (Core/Domain/Girdi/GirdiHatasi.cs) — BankaAdi, HesapSahibi(150), DekontAciklama(500), DekontAciklamaDuzelt(2000), KasaAdi, HareketAciklama(500), KategoriAdi, VezneIptalGerekce(500), FaturaKalemAciklama(300), FaturaAciklama(500), PusulaSaticiAd, PusulaMal, StopajFisNo, PosCihazAd, PosKod; ekran `EnFazla` ve DTO `StringLength/MaxLength` aynı sabiti okur (sözleşme testi GA05).

### 1.2 Ekran — 13 ekran (Banka, BankaEkstre, Faturalar, Kasalar, KasaRaporu, Vezne, Kur, Pos, PosTahsilatlar, Gider, GiderPusulalari, Hurda, Emanet)
- **Ham `<input>/<select>/<table>` = 0** (13 sayfanın hepsinde, yorum dışı ölçüm). Faturalar 3 ham input + raw date, Pos 4 input + 2 select → ortak `Metin/Unvan/Secici/Tarih`.
- **Anlamsal bileşenler:** Vezne tutar/fiyat/işçilik/bacak `Metin` → `Para Tur=…`; adet → `Adet` (ADET ürün) / `Gram` (GRAM ürün; `VezneUrunSatiri.Birim`); Kur → `KurAlani EnCokOndalik=4`; dekont/kasa/gider/virman/fatura/stopaj tutarları `Para Tur=Pozitif|SifirOlabilir`; gün sayımı `SifirOlabilir` (Kasalar ↔ KasaRaporu tutarlı); Emanet `Gram Tur=Gram|Tas Brut`; Hurda ek fark 6 ondalık ±tavan, tahsil `SifirOlabilir`; Faturalar miktar `Gram Tur=Gram`, fatura tarihi `Tarih Tur=Islem`; GiderPusulalari satıcı `AdSoyad`, TCKN `TcKimlikNo`; banka sahibi/karşı taraf `Unvan EnFazla=150`; tarih süzgeçleri `Tarih Tur=Rapor` + K8e aralık hatası bitişin altında (bozuk aralıkta istek YOK).
- **Satır içi düzenleme:** açıklamalar `EnFazla` (sunucu sabiti), fatura kalem miktar/fiyat `EnCokOndalik` — fazlası Core sözlük metniyle alan altında.
- **Ortak ızgara (25 tablo → `Izgara`):** her tablo `grid:<ekran>:<grid-id>` tercihiyle sütun sırası (sürükle + klavye), göster/gizle, sıralama, kullanıcıya+ekrana kalıcı, "Varsayılana dön". Mevcut tablo/satır/hücre id'leri ve kart sınıfları (q10-yigin, zo-k-*, zo-yigin-dar) korundu (`TabloId`, `SatirId`, `HucreSinif`). Sıralama: liste TAM ise yerel (`IzgaraYerelSiralama`), sunucu sınırına değmiş/sayfalı listede (Faturalar ≥100, Vezne `dahaFazlasiVar`, Gider/Virman sınır, POS defteri, Ekstre) sıralama düğmesi ÇIKMAZ (UX-S29 — yanıltıcı sıra yok).
- Izgara kimlikleri: kur-ayar-fiyat · kur-parite · kur-gecmis-liste · pos-cihaz · pos-taksit · pos-t-valor-liste · pos-t-defter · emanet-hareket · bh-hareket · ekstre-satir · vz-liste-izgara · vz-detay-kalem · kr-gun · gd-gider · gd-kategori · gd-virman-liste · gp-liste · sp-donem (×2) · hrd-defter-izgara · hrd-sube · fk · fk-kalem · ff-kalem · fd-kalem-liste.

### 1.3 Ortak bileşenlere en küçük geriye uyumlu eklemeler (gerekçeli)
| Dosya | Ekleme | Gerekçe |
|---|---|---|
| `Bilesenler/Grid/Izgara.razor` | `KimlikOneki`, `TabloId`, `KartListeSinif`, `Zo` (hepsi null varsayılan) | aynı ızgara bir sayfada birden çok kez (kasa raporu kasa başına, stopaj açık/ödenmiş); mevcut tablo id'leri (`fk-tablo`, `kr-tablo-{id}`, `vz-liste`…) DEĞİŞMESİN; zo-belge.css `[data-zo]` kart/gölge kuralları sürsün |
| `Bilesenler/Grid/IzgaraYerelSiralama.cs` (YENİ) | tam listenin Deger'e göre sıralaması (tr-TR) | GRID-1 ızgarası sıralamayı sayfaya bırakır; tam listede yerel sıralama yanıltmaz |
| `Bilesenler/KartListe.razor` | `Zo` → `data-zo` (null = öznitelik yok) | yukarıdaki zo-belge.css kuralları |
| `Bilesenler/SatirIciDuzenle.razor` | `EnFazla`, `EnCokOndalik` (null = eski davranış) | envanter: "satır içi açıklamada ekranda sınır yok", "fazla ondalık sessizce yuvarlanıyor" |
| `Bilesenler/Form/Unvan.razor` | `EnFazla` (varsayılan AlanSinirlari.Unvan) | hesap sahibi/karşı taraf kolonu 150 |
| `Core/Domain/IzgaraTercihi.cs` | `BilinenEkranlar` += 13 para ekranı | beyaz liste fail-closed (yoksa tercih 400) |
| `Core/Sozlesmeler/VezneSozlesmeleri.cs` | `VezneUrunSatiri.Birim` (isteğe bağlı) | ADET/GRAM'a göre doğru bileşen |
| `Core/I18n/Sozlukler.GocA.cs` (YENİ, TR+EN) | VezneKalemUrunsuz, FaturaTarihIpucu, VirmanAciklamaKalan | çıplak metin yok |

## 2. Sapmalar ve gerekçeleri
1. **TarihAralik yerine iki `Tarih`** (Kur geçmişi, POS tahsilat, gider pusulası, kasa raporu): ilk sürüm TarihAralik kullandı; gerçek Chrome 1280'de POS süzgeç satırında **iki satıra kırıldı** (K7 hizasız 1, ekran görüntüsüyle görüldü) → iki ortak `Tarih Tur=Rapor` + aralık kuralı bitişin altında. Davranış (K8e) aynı, id'ler korundu.
2. **PosTahsilatlar iki gerekçe alanı `GerekceAlani`'na taşınmadı**: envanter TAM diyor; onaylı alan altı cümlesi ve kilit satırı (ZO-ISLEM-A 28.09 hakemi) sayaçlı kalıpta kayboluyordu (ZO28n kırmızı oldu) → `CokSatir` korundu, üst/alt sınır `AlanSinirlari` sabitinden.
3. **Ürün birimine göre adet kuralı (Vezne):** envanter K14 "gramlı üründe miktar gramdır" — DTO'ya `[Adet]` konsaydı GRAM ürünü bozardı; DTO `[Gram]` + servis birim kontrolü.
4. **Faturalar'da ADET birimli kalemde tam sayı kuralı UYGULANMADI** (açık kalan): fatura kalemi ürünsüz/ADET-GRAM karışık olabiliyor; kapsam/karar ihtiyacı.
5. **KDV oranı yasal listesi (donustur-kdv, fatura-kdv, ff-kdv) KODA YAZILMADI** — MEVZUAT KURALI: birincil kaynak (Cumhurbaşkanı Kararı / RG) bu pakette okunmadı → bugünkü davranış (0..1 aralığı) korundu; mali müşavir notu § 11.
6. **K21 adres eşiği (10) ve POS üye işyeri/terminal no karakter kümesi** TEYİTSİZ → uygulanmadı (bugünkü davranış).
7. **Kasalar `kasa-liste` / kasa geçmişi ve stopaj `sp-odeme-listesi`** `<table>` değil kart/ul listeleri (envanter G2 "kart-liste-div"); ızgaraya taşınmadı — kart düzeni ekranın ana etkileşimi; GRID-1 sayaç kapsamı `<table>`. Açık kalan olarak yazıldı.
8. **Tavan/Ondalık ileti dili değişti** (eski "Sayılan nakit eksi olamaz", "Tutar en az 0,01 olmalıdır …" → sözlük "Eksi değer yazılamaz.", "Virgülden sonra en çok 2 hane yazılır." …): kabul ölçütü "mesaj aynı sözlük anahtarı". Bu yüzden 30+ eski test YENİ sözleşmeye güncellendi (§ 4.2) — hiçbiri gevşetilmedi; her biri 400 + alan + "satır yazılmadı" iddiası ve tam kuruşla asıl iş kuralını (nakit payı, fark gerekçesi …) aynen sınar.
9. **denetim.sh P-18 `MOBIL_TABLO_SARMALAYICISIZ`**: `<KartListe` sayısı 9'un altına düştü (Banka + Gider ×3 ızgaraya geçti; ızgara KartListe'yi içinden kullanır) → sayaç `<Izgara …>` kullanımını da KartListe sayar (GRID-1'in MobilParite testine getirdiği kuralın aynısı; gevşetme değil: elle `tablo-sar` hâlâ 0 olmalı).
10. Izgara kimlik çakışması (gerçek: `gp-basari` sayfa bandı ↔ ızgaranın `-basari` onayı) bUnit'te bulundu → GridId'ler `gp-liste`, `fd-kalem-liste`, `gd-virman-liste`; tüm sayfalar suffix taramasıyla çakışmasız.

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Testler
### 4.1 Yeni — `Kuyumcu.Tests/GocATestleri.cs` (24 yöntem, 28 koşum)
- **GocASafTestleri** GA01 ızgara beyaz listesi · GA02 yerel sıralama (sayı/tarih/tr-TR/null) · GA03×3 kuruş altı 400 · GA04 eksi/sıfır/tavan/gerekçe/işaretli has/adet · GA05 ekran sınırı = DTO sınırı (Kur ekran ondalığı = KurKolonOndalik) · GA06 karşı IBAN `[Iban]`.
- **GocASunucuTestleri** (gerçek MySQL) GB01 karşı IBAN mod-97 400 + normalleşme · GB02 PATCH karşı IBAN 400 · GB03 dekont ileri/eski/3000 tarih + yalnız rakam karşı taraf · GB04 kur 0/eksi/5 ondalık 400, geçerli birebir · GB05 kur aralık · GB06 vezne kesirli adet + 10,005 · GB07 iptal gerekçe 16/501 400, 500 birebir (kesme yok) · GB08 fatura ileri tarih POST/PATCH, 3000 yılı, 12,345, kalem açıklaması 301 kırpılmaz · GB09 kasa fark gerekçesi + tek harf kasa · GB10 emanet taş=brüt, 4 ondalık · GB11×3 POS liste · GB12 pusula bitiş günü dahil + ters aralık · GB13 stopaj 0,004 · GB14 ızgara tercihi A kalıcı / B yok / bilinmeyen ekran 400.
- **GocAEkranTestleri** (bUnit) GE01 Vezne eksik ikinci kalem SESSİZCE ATILMAZ (POST yok) · GE02 ADET→Adet (data-tam) / GRAM→Gram · GE03 kalemsiz tutar Para (eksi yazılamaz, 10,005 alan altı) · GE04 Kur USD 0 alan altı + Kaydet kilitli.

### 4.2 Yeni sözleşmeye güncellenen mevcut testler (davranış değişikliği — gerekçe satırı her birinde)
PARADEMO2Sinir S3/S4 · PARADEMO2SinirTur6 DG_kurus · PARADEMOSinir PDS_kurus ×2, PDS3_virman · PATCH2Sinir PS2 · PATCH5Sinir (gün kapat ×3, kasa giriş/çıkış, gider) · PATCHSOZLESMESinir Sinir3 · BELGEKALANSinir DGR_BK_seri (2025) · BelgeKalan BK_Seri (2025), BK_Fatura_cekmece (gg.aa.yyyy) · Q101Sinir ×2 · QA10KRITIKSinir · SONKALANLARSinir DSK05/06/07/10/11/34 · YASAL4Sinir ×2 · MasakBanka Ileri_tarihli · PatchIki ×3 · PatchUc P3_1264 (dizi ileti) · Q92 · P54 · SonKalanlar SK06/07/10/12 + SKE06 (≥20 gerekçe) · P12 B38/P78 (GRAM ürün fikstürü) · P22 B3/B45 · FaturaEkran KuyumTaslagi · S3fEkran Kasa_degistirilince (fikstür `aktif:true` — eski `<select>.Change` seçeneksiz geçiyordu) · EkstreEkran Q105 · KesifP6 Kur_parite · KurEkran Gecmis · MobilParite B9 · ZOISLEMA ZO28e · KesifP3 GiderPusulalari · OrtakGorunumKiyas KY8/KY10 · ZoBelgeReyon ZB29 · ZoBelge ZB52 · Ekran (pusula düğme) .

### 4.3 Koşumlar
- Filtreli (son kod): GocA + P54 + TS1 → **63/63 yeşil**; geniş ekran/sunucu filtresi (tur 3: 6312, tur 4: 7073, tur 7: 2943 koşum) kırmızıları yukarıdaki güncellemelerle kapandı.
- **Tam süit (son kod, temiz derleme sonrası):** 11122 · **Geçti 11116 · Başarısız 4 · Atlandı 2** (`/private/tmp/kuyumcu-serit/trx-pGOC-A/tam2.trx`). 4 kırmızının **hiçbiri bu paketin değil** → § 10 (sahibiyle).
  - Not (md.1): bir önceki tam koşum (`tam-suit.log`) mutasyon betiğinin `copy2` ile mtime'ı geri götürmesi yüzünden **artımlı derlemenin mutasyonlu DLL'i tutmasıyla** 3 sahte kırmızı verdi (GE01/GA01/GB14); dosyalara `touch` + yeniden derleme yapıldı, aynı testler yeşil; ölçü olarak SON koşum alındı.
- Derleme: `dotnet build Kuyumcu.sln` → **0 Hata, 4 Uyarı** — 4 uyarı bu paketten ÖNCE de vardı (build2 tabanı): SUBEKAPSAMASinirTestleri.cs(49), KesifP2Testleri.cs(455), SonKalanlarTestleri.cs(587, 818 — dokunmadığım satırlar). Bir ara artımsız derleme başka şeridin eşzamanlı WebAssembly paketlemesiyle MSB4018 (dosya kilidi) verdi; yeniden denemede 0 hata.

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-A/` → TEK eşleşme → filtreli test → geri kopya → `filecmp` birebir; betik `GOC-A/mut.py`, sonuç `mut-sonuc.jsonl`)
| Kod | Kural kapatıldı | Kırmızı test | cmp |
|---|---|---|---|
| M1-1 | Vezne ADET birimde tam sayı (`if (false && birim != "GRAM")`) | GB06_Vezne_ADET_urunde_kesirli_adet… | AYNI |
| M1-2 | `HareketEkleDto.KarsiIban` `[Iban]` kaldırıldı | GA06_Karsi_IBAN_DTO_Core_IBAN_ozniteligi_tasir | AYNI |
| M1-3 | Fatura taslak PATCH `IslemTarihi` | GB08_Fatura_ileri_tarih_POST_ve_taslak_PATCH… | AYNI |
| M1-4 | Pusula listesi bitiş gün sonu | GB12_Gider_pusulasi_listesi_bitis_gunundeki… | AYNI |
| M1-5 | Vezne iptal gerekçe üst sınırı (500→100000) | GB07_Vezne_iptal_gerekcesi_20_alti_ve_500_ustu… | AYNI |
| M1-6 | Kur kolon ondalığı (`GirdiKapisi.Kur(has…)` silindi) | GB04_Kur_manuel_sifir_eksi_bes_ondalik… | AYNI |
| M1-7 | Ekran Vezne sessiz silme kapısı (`if (false && …)`) | GE01_Vezne_eksik_ikinci_kalem_SESSIZCE_ATILMAZ… | AYNI |
| M1-8 | Izgara beyaz listesinden "vezne" | GA01_…, GB14_Para_ekrani_izgara_tercihi… | AYNI |
| M1-9 | Kasa fark gerekçesi K30 | GB09_Kasa_farkli_gun_kapat_kisa_gerekce… | AYNI |
| M1-10 | Emanet taş<brüt | GB10_Emanet_tas_brutten_buyuk… | AYNI |
| M2-1 | denetim `IBAN_REGEX_DTO` yeni kuralı (KarsiIban `[Iban]` kaldırıldı) | sayaç 0 → **1** (`BankaModulu.cs:82`) | AYNI, geri 0 |
Son mutasyondan sonra dosyalar `touch` + temiz derleme (0 hata).

## 6. denetim.sh (son kod)
`DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, görev "P-30 hariç"). Bu paketin sayaçları (yalnız azalır, tavanlar indirildi):
| Sayaç | Önce (taban) | Sonra |
|---|---|---|
| GRID-1 HAM_TABLO_ORTAK_GRID_DISI | 95 | **70** (25 tablo) |
| GIRDI-2 GIRDI_TUR_GENEL_BILESEN | 15 | **11** |
| GIRDI-2 HAM_TARIH_GIRDISI | 7 | **6** |
| GIRDI-2 DTO_DECIMAL_RANGESIZ | 26 | **20** (bu modüllerde 0) |
| GIRDI-2 EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) | 168 | **129** (13 sayfada sayısal EnFazla 0) |
| GIRDI-2 IBAN_REGEX_DTO | 0 | 0 (kural genişledi, mutasyonla kanıtlı) |
| P-18 MOBIL_TABLO_SARMALAYICISIZ | ✓ | ✓ (32; § 2.9) |
| P-36 HAM_INPUT / HAM_SELECT | 26 / 17 | azaldı (13 sayfada 0); tavanlar başka paketin, dokunulmadı |

## 7. Canlı doğrulama (API 5162, Development, `kuyumcu_net`; test kiracısı `PAKETGOC-A-TEST …` = 01M4DMVGTB6JAXDEK82EY6AB7Q; A=SAHİP, B=MÜDÜR; betikler `canli_goca.py`, `canli_ek.py`, `veri_kur*.py`; sonuç `canli-sonuc.json`)
| Adım | Beklenen | Gerçek | İleti / alan |
|---|---|---|---|
| Banka karşı IBAN mod-97 bozuk | 400 | 400 | "IBAN kontrol hanesi tutmuyor…" · karsiIban |
| Banka tutar harf "abc" | 400 | 400 | "Tutar geçerli bir sayı değil…" · tutar |
| Banka dekont 3000 yılı | 400 | 400 | "Tarih bugünden ileri olamaz." · tarih |
| Banka karşı taraf "12345" | 400 | 400 | "En az bir harf olmalı." |
| Banka geçerli dekont (1.250,50, geçerli IBAN) | 200 | 200 | SQL: 1 satır, 1250.50, karsi_iban birebir |
| Kasa eksi / 0,005 / tavan üstü | 400 | 400 ×3 | "Eksi değer…" · "Virgülden sonra en çok 2…" · "Değer çok büyük…" |
| Kasa geçerli 500,25 | 200 | 200 | SQL: 1 satır 500.25; tutar=0 satır 0 |
| Kasa adı "A" | 400 | 400 | "Kasa adı en az 2 karakter…" · ad |
| Gün kapat fark + gerekçe "eksik" | 400 | 400 | "Gerekçe en az 20 karakter olmalı (şu an 5)."; SQL gun_kapanislari 0 |
| Vezne ADET ürün 1,5 adet | 400 | 400 | "1. kalem · adet: Küsuratlı yazılamaz; tam sayı girin." |
| Vezne geçerli fiş (2 adet) | 200 | 200 | SQL: 1 fiş ACIK, kalem adet 2.000 |
| Vezne iptal gerekçe 12 / 501 | 400 | 400 ×2 | "…en az 20 (şu an 12)" · "…en çok 500 (şu an 501)"; SQL iptal_gerekce NULL (kesilmedi, yazılmadı) |
| Vezne tahsilat 10,005 | 400 | 400 | tutarTl |
| Kur USD 0 / gram -5 / has 6800,12345 | 400 | 400 ×3 | SayiSifir · SayiEksi · "en çok 4 hane" |
| Kur geçerli 6800,1234 / USD 41,5678 | 201 | 201 | SQL: 6800.1234 · 41.5678 birebir |
| Kur geçmiş ters aralık | 400 | 400 | AralikTers · bitis |
| Fatura ileri tarih / 3000 yılı / 12,345 | 400 | 400 ×3 | TarihIleri · TarihIleri · ondalık |
| Fatura geçerli taslak | 200 | 200 | SQL: 1 fatura tarih 2026-10-08 |
| Emanet taş=brüt / 10,0005 g | 400 | 400 ×2 | "Taş ağırlığı brüt gramdan küçük olmalı." · 3 hane |
| POS liste durum XYZ | 400 | 400 | "Bilinmeyen durum…" |
| Gider pusulası ters aralık | 400 | 400 | bitis |
| Stopaj 0,004 | 400 | 400 | ondalık |
| Izgara tercihi A yaz/oku · B yok · bilinmeyen ekran | 200/200/200/400 | aynı | SQL: `grid:vezne:vz-liste-izgara` 115 karakter; denetim_kayitlari 1 |
**UYUMSUZ 0 / 39** (ilk koşumda 1 uyumsuz: kiracıda kasa yokken "Kasa geçerli hareket" 404 — betik hatası, ek koşumda kasa açılarak 200 ölçüldü).

### 7.1 Gerçek başsız Chrome (vekil 5163, CDP 9905–9908; `sikisma-olc.js`; `olc.mjs`; ham `GOC-A/olcum2/olcum.jsonl`, 104 ölçüm, 104 ekran görüntüsü)
13 rota × {1280×800, 375×812} × {BASİT, PRO} × {açık, koyu}; geçmiş kayıt akordeonları açılarak (ızgaralar görünür). Hücre = K1+K3(taşan/kırık)+K5 TRY+K7+K9+K10+K11+K12+yatay toplamı; modGercek = istenen mod 104/104.
| Rota | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K | Izgara (görünür) |
|---|---|---|---|---|---|---|---|---|---|
| /banka | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | bh-hareket |
| /banka/ekstre/{id} | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ekstre-satir |
| /faturalar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | fk |
| /kasalar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — (tablo yok) |
| /raporlar/kasa | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kr-gun-{kasa} |
| /vezne | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | vz-liste-izgara |
| /kur | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | kur-ayar-fiyat |
| /pos | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | pos-cihaz |
| /pos/tahsilatlar | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — (kayıt yok, boş durum) |
| /gider | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | gd-gider |
| /gider-pusulalari | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | gp-liste |
| /hurda | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hrd-defter-izgara, hrd-sube |
| /emanet | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — (müşteri seçilmeden liste yok) |
Ilk ölçüm turu (`olcum-tur1`) iki gerçek kusur buldu ve düzeltildi: `/pos/tahsilatlar` 1280 K7=1 (TarihAralik iki satıra kırılıyordu — görüntü `kirp-pos-t.png` Read ile görüldü) → § 2.1; `/banka/ekstre` K3 kırık "Yön: Gelen" → hücre tek satır. 375'te ızgaralar kart düzeninde (`tbody tr` display:grid) 10/10; 1280'de tablo.
Görüntüler (Read ile görülen 3): `olcum-tur1/kirp-pos-t.png` (kusur), `olcum2/k-fat375.png` (Faturalar 375 PRO koyu — kart, Sütunlar düğmesi, para ₺), `olcum2/k-vz1280.png` (Vezne 1280 BASİT — A'nın kayıtlı düzeni: "1 sütun gizli", TOPLAM önde ↓ azalan).
**Izgara tercihi tarayıcıda:** A (`izgara_ab_kur.mjs`) `/kur` → sütunlar `gram-fiyati, ayar` + "1 sütun gizli"; B aynı rota → varsayılan `ayar, milyem, gram-fiyati`, gizli yok.

### 7.2 Temizlik
API (5162) ve vekil (5163) durduruldu, `lsof` boş; Chrome süreçleri 0. `zsh kiraci-temizle.sh 01M4DMVGTB6JAXDEK82EY6AB7Q --uygula` → **tenants 16 = başlangıç 16**; davet satırı (`GOCADAVET…`) silindi (0). QA-TEST-* kiracılarına dokunulmadı.

## 8. DoD sözleşmesi
Yeni varlık/kayıt türü YOK (migration yok). Izgara düzeni GRID-1 sözleşmesiyle (`kullanici_tercihleri`: PATCH koru/null sil, DELETE "Varsayılana dön", `[HerkeseAcik]` + kimlik jetondan, `YazmaKaydet` denetim izi — canlı SQL'de 1 kayıt). Finansal kayıtlarda yeni silme yolu açılmadı; tüm değişiklikler girdi kapısı.

## 9. Değişen dosyalar (bu paket)
Sunucu: `Kuyumcu.Api/Common/Dogrulama/GirdiKapisi.cs` (YENİ) · `Modules/Banka/BankaModulu.cs` · `Modules/Kasa/KasaModulu.cs` · `Modules/Gider/GiderModulu.cs` · `Modules/Gider/VirmanModulu.cs` · `Modules/Vezne/VezneModulu.cs` · `Modules/Kur/KurModulu.cs` · `Modules/Fatura/FaturaModulu.cs` · `Modules/Hurda/StopajModulu.cs` · `Modules/Hurda/HurdaModulu.cs` · `Modules/Hurda/TahlilFarkiServisi.cs` · `Modules/Hurda/GiderPusulasiKapisi.cs` · `Modules/Emanet/EmanetModulu.cs` · `Modules/Pos/PosModulu.cs`.
Core: `Domain/Girdi/GirdiHatasi.cs` (AlanSinirlari +15) · `Domain/IzgaraTercihi.cs` (BilinenEkranlar) · `Sozlesmeler/VezneSozlesmeleri.cs` (Birim) · `I18n/Sozlukler.GocA.cs` (YENİ).
Web: 13 sayfa (Banka, BankaEkstre, Faturalar, Kasalar, KasaRaporu, Vezne, Kur, Pos, PosTahsilatlar, Gider, GiderPusulalari, Hurda, Emanet .razor) · `Bilesenler/Grid/Izgara.razor` · `Bilesenler/Grid/IzgaraYerelSiralama.cs` (YENİ) · `Bilesenler/KartListe.razor` · `Bilesenler/SatirIciDuzenle.razor` · `Bilesenler/Form/Unvan.razor`.
Denetim: `dotnet/denetim.sh` (IBAN_REGEX_DTO kuralı, 5 tavan indirildi, P-18 ızgara sayımı).
Testler: `GocATestleri.cs` (YENİ) + § 4.2 listesindeki 30 dosya.
Dokunulan sayfalar (TEK DOKUNUŞ kaydı): yukarıdaki 13 sayfa.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K1 ham `<input>/<table>` kalmaz, sayaçlar azalır | KARŞILANDI | 13 sayfada 0/0/0; HAM_TABLO 95→70, GIRDI_TUR 15→11, HAM_TARIH 7→6, DTO_DECIMAL 26→20, UZUNLUK 168→129 |
| K2 her açık için sunucu 400 testi + ekran alan altı, aynı sözlük anahtarı | KARŞILANDI | GB01–GB13, GE01–GE04, GA05; canlı § 7 |
| K3 mutasyon ≥3 kural, cmp AYNI | KARŞILANDI | 10 + 1 denetim mutasyonu, hepsi kırmızı, cmp AYNI |
| K4 canlı 5162 harf/yıl/eksi/kesir/IBAN red, geçerli kayıt, ızgara A kalıcı B varsayılan | KARŞILANDI | § 7 (39/39), § 7.1 tarayıcı A/B |
| K5 başsız Chrome 1280+375 · BASİT+PRO · açık+koyu, sikisma-olc | KARŞILANDI | 104 ölçüm hepsi 0 (2 kusur bulundu, düzeltildi, yeniden ölçüldü); Read 3 görüntü |
| K6 build 0/0, denetim TEMİZ (P-30 hariç), tam süit 0 yeni kırmızı | KARŞILANDI (uyarı notuyla) | 0 hata; 4 uyarı paket öncesinden; denetim yalnız P-30; tam süit 4 kırmızı başka şeridin (§ 11) |
| GIRDI-2 devri: KarsiIban `[Iban]` + sayaç kuralı | KARŞILANDI | GA06, M1-2, M2-1 |
| Gerekce/Ayar bileşeni | GEREKMEDİ | para ekranlarında gerekçe alanları mevcut GerekceAlani/CokSatir ile; Ayar seçici Secici (Emanet) yeterli |
| IBAN imleç (girdi-maske.js) DOKUNMA | KARŞILANDI | dosyaya dokunulmadı |
| Paralel şerit dosyaları (Rapor/KarZarar/Urun, EkstreModulu) DOKUNMA | KARŞILANDI | EkstreModulu.cs değişmedi |

## 11. Açık kalanlar
1. **Tam süitte başkasına ait 4 kırmızı** — sahibi YASAL-PARAM şeridi (`YasalParametreServisi.cs` 15:24'te başka şeritçe değiştirildi; migration `20261008104154_YasalParamAyniGunAn`): `YasalParam2HttpTestleri.Ayni_gun_duzeltme_ayni_operator_…`, `YasalParam2RiskTestleri.Risk_ogle_esik_dususu_…`, `YasalParam2SafTestleri.Ayni_gun_duzeltme_yanlis_deger_…`, `YasalParam2SafTestleri.Ayni_gun_esik_degisimi_AN_siniri_…` (beklenen 370000, gerçek 250000). Bu paket o dosyalara dokunmadı.
2. `TS1TuvalSatisTestleri.Pro_yuz_tezgah_ogeleri_YOK…` bir tam koşumda zaman aşımıyla kırmızı, tek başına koşumda yeşil (kararsızlık; Satış ekranı bu paketin değil).
3. KDV oranı yasal listesi (3 alan) — mevzuat birincil kaynağı gerekli (aşağıda).
4. Faturalarda ADET birimli kalemde tam sayı kuralı (karar gerekli).
5. Kasalar kasa kartları / kasa geçmişi, stopaj ödeme listesi (`<ul>`) ızgaraya taşınmadı (tablo değil, kart listesi).
6. Cari tahsilat ucu (Cari modülü — GOC-B) hâlâ 0,005 → 0,01 yuvarlıyor (SK12'de belgeli).
7. Hurda satış DTO'su (`/islemler/hurda-satis`, Satış modülü) fiyat 0'ı kabul ediyor — GOC-C.
8. K21 adres ≥10 ve POS üye işyeri/terminal no biçimi TEYİTSİZ (uygulanmadı).
9. Derleme uyarıları (4) başka dosyalarda, paket öncesinden (tur 2 son temiz derlemesi: 0 uyarı).
10. **Controller yolundaki uzunluk iletisi** (DTO `StringLength` → merkezi model doğrulama "X en fazla N karakter olabilir") ekranın `AdUzun` iletisinden farklı cümle; tur 2 yalnız servis kapısını (GirdiKapisi) sözlüğe taşıdı. Merkezi ileti eşlemesi tüm modülleri etkilediği için GIRDI ailesinin ortak işi (sahibi: GIRDI/GOC ortak kuyruk).
11. Tur 2 tam süitindeki 5 kırmızı — sahibi 'yildiz' renk şemasını ekleyen tema şeridi (tema.js 16:58): TasarimBelirtecTestleri.Sema_beyaz_listesi…, B1H2, BE2, S1f_E1, S1f_E14.
12. sikisma-olc.js K9, açık çekmece/modal altında kalan sayfa yazısını "binen" sayıyor (örtü kontrolü yok) — ölçü aracı sahibine (orkestratör betikleri) iletilmeli; K8'in kapalı çekmeceyi taramaması da aynı başlık (bu turda çekmece açık ölçülerek kapatıldı).

## 12. Kurucuya / mali müşavire
- **KDV oranları:** fatura ve "faturaya dönüştür" ekranlarında seçenekler %0/%1/%10/%20; sunucu yalnız 0..1 aralığını denetliyor (API'den %7/%37 geçer). Yasal oran listesinin birincil kaynağı (Cumhurbaşkanı Kararı, RG) okunmadan koda yazılmadı → **DOĞRULANAMADI — mali müşavir teyidi gerekir**; teyitten sonra liste yapılandırmadan okunacak şekilde bağlanmalı.
- **Fatura tarihi penceresi** (ileri gün yok, en çok 1 yıl geri) İŞ KURALI (envanter K8a); VUK belge düzenleme süreleri açısından geri pencerenin uygun olup olmadığı mali müşavire sorulmalı (bugün 1 yıl).
- **Kasa farkı gerekçesi en az 20 karakter** — proje kuralı K30'a bağlandı (gün açma/storno ile aynı).
