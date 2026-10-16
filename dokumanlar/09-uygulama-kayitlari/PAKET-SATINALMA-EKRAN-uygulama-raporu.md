# PAKET SATINALMA-EKRAN — Satın alma / irsaliye ekran kalanları (uygulama raporu)

**Durum: BİTTİ** (hasımsız paket: kendi kanıtım bağlayıcı). Şerit t5 · API 5166 · vekil 5167 · CDP 9511/9512 · test veritabanı `kuyumcu_test_t5`.
Kaynak: `PAKET-EKSIK-B1-SATINALMA-uygulama-raporu.md` (hasım GEÇTİ + tasarım hakemi bulguları). COMMIT YOK. Para/veri/yasal davranışına dokunulmadı.

## 1. Yapılanlar (spesifikasyon maddesi → uygulama)

| # | Madde | Yapılan |
|---|---|---|
| 1 | İrsaliye satırı kart kenarına yapışık (iç boşluk 0) | `SatinAlma.razor`: satırlar `kart-govde sa-irs-icerik` içine alındı. Yıldız şeması `.kart-govde` yatay boşluğunu 0 yaptığı için (tema-yildiz.css, başka şeridin dosyası, dokunulmadı) yeni `css/satinalma-ekran.css` (belirteçli, `html:root[data-sema]` özgüllüğü) + `index.html`'e tek `<link>` satırı. Ölçüm: iç boşluk 16 px, satırın kart kenarına uzaklığı 17 px (≥12) |
| 2 | Açıklama satırları açılışta | Mal kabul açıklaması, sipariş formu fiyat açıklaması, irsaliye formu "fiyat yazmaz" ve irsaliye yasal notu kapalı `Katlanir` ("Nasıl çalışır?") içine alındı; "aktife al" açıklaması düğme `title`'ına taşındı. Açılışta DOM'da 0 açıklama satırı |
| 3 | Seçici yer tutucusu "Henüz kayıt seçilmedi." | `Secici`nin mevcut `YerTutucu` parametresiyle: "Tedarikçi seçin", "Ürün seçin", "Alıcıyı seçin", "Fatura seçin" (TR+EN). Ortak Secici değişmedi |
| 4 | Mal kabul kimlik formu "bu müşterinin bugünkü toplamı" | `MasakFormu`ya iki isteğe bağlı parametre (`ToplamBaslikSablonu`, `ToplamAmacBaslikSablonu`; varsayılan null = diğer ekranlarda AYNEN). Mal kabul "bu tedarikçinin bugünkü toplamı" der |
| 5 | Taslakta boş irsaliye kartı | İrsaliye kartı yalnız irsaliye varsa çizilir |
| 6 | Eski kalan hata iletileri | `KabulAdetDegisti`, `MasakHazirla`, `DuzenleAc`, iptal onayından vazgeç (`IptalVazgec`, iki ekranda) hata/bant iletilerini temizler |
| 7 | Odakta binlik ayracı | **Kodda kusur BULUNAMADI.** Gerçek Chrome'da (gerçek fare tıklaması) dolu fiyat odakta "42500", çıkınca "42.500"; 8 kombinasyonda yazarken/odaklanınca ham. (Sentetik `focus()` ile ilk odakta biçimli kaldı: ölçüm yapıntısı, gerçek tıklamada yok.) Kod değişmedi; SE08 bUnit testi davranışı kilitledi |
| 8 | İptal bölümünde "Sipariş" metni (irsaliye çekmecesi) | Yeni TR+EN anahtarlar `IrsIptalEt`, `IrsIptalBaslik`; irsaliye iptal katlanırı, düğmesi ve onay penceresi irsaliye der. Ölçüm: "Sipariş" geçişi 0 |
| 9 | İptalden geri dönüş yolu | **Yeni uçlar** `POST /satinalma/{id}/aktife-al` (İPTAL → TASLAK; önceki durum kayıtlı olmadığı için tutucu, yeniden onay gerekir) ve `POST /irsaliyeler/{id}/aktife-al` (İPTAL → AÇIK/FATURALANDI; deftere bağlı irsaliye 409). `[RolGerekli("SAHIP","MUDUR")]`, `DenetimServisi.YazmaKaydet`, gerekçe temizlenir. Ekranda "Siparişi aktife al" / "İrsaliyeyi aktife al" düğmesi + başarı bandı |
| 10 | İrsaliye dökümünde teslim adresi | `CiktiModulu.IrsaliyeBelgesi`: not satırının başına "Teslim adresi: …". Sevk (GİDEN) = alıcının (cari) adresi; mal kabul (GELEN) = işletme adresi. Adres kayıtlı değilse UYDURULMAZ: "Teslim adresi: kayıtlı değil — kart bilgisinden girin" |

Ek: sunucu iletilerinin TR/EN çiftleri `SunucuIletiSozlugu.Veri.cs`'ye eklendi (denetim.sh DIL-SUNUCU 3 → 0).

## 2. Sapmalar ve gerekçeleri
- Madde 7 (odak binlik): kusur yeniden üretilemedi → kod değiştirilmedi, davranış testle kilitlendi, sahte düzeltme yapılmadı (Anayasa md.1).
- `Katlanir` zaten hazırdı (SATIS-2); yerel kopya açılmadı.
- `MasakFormu` ve `index.html` dosya alanında yazılı değil; en küçük dokunuş (iki isteğe bağlı parametre / bir link satırı). Eşzamanlı YASAL-KAPI-2 çakışması gözlenmedi (yerinde düzenleme, başkasının satırına dokunulmadı); **sahiplere not**.
- Aktife al: sipariş her zaman TASLAK'a döner (iptal öncesi durum şemada yok; şema değiştirilmedi, migration gerekmedi).

## 3. Migration
Gerekmedi (şema değişmedi; yalnız mevcut kolonlar `durum`, `iptal_gerekce`, `guncellendi`).

## 4. DoD sözleşmesi
| Varlık | Düzenleme / geri dönüş |
|---|---|
| Satın alma siparişi | PATCH (yalnız TASLAK), onayla, iptal, **aktife al (yeni)**; hard DELETE yok |
| Sevk irsaliyesi | PATCH, faturaya bağla/kaldır, iptal, **aktife al (yeni)** |
| Mal kabul irsaliyesi (GELEN) | Defter bağlı: iptal 409 + "alış iadesi/storno" DegismezIpucu; aktife al 409 |
Her yeni düğme ya istek atar ya `disabled`; yıkıcı iptal ortak `OnayKipi`; ModalPencere artışı 0.

## 5. Testler
Yeni **11** test: `Kuyumcu.Tests/SatinalmaEkranKalanlarTestleri.cs` — bUnit SE01–SE08 (8), HTTP SH01–SH03 (3). Önce: `EksikB1Satinalma*` 29 → şimdi aynı filtre dahil 40, hepsi yeşil.
Tam süit (son kodla tek kez, trx `/private/tmp/kuyumcu-serit/trx-pSATINALMA-EKRAN`): **12345 toplam · 12339 geçti · 3 başarısız · 3 atlandı.** Kırmızılar BENİM DEĞİL: `RaporDortTestleri.RD_KH01`, `RD_KH02` ("Rapor dönemi en çok 400 gün" — tarihe bağlı eskimiş fikstür, RAPOR sahibi), `Yildiz4CTestleri.Y4C_44…(PROFESYONEL)` (YILDIZ-4C-2 şeridi; tek başına koşunca GEÇTİ). Yeni kırmızı farkı: 0.

## 6. Mutasyon tablosu (hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1 | irsaliye kartı koşulu `Count>0` → `true` | SE01 |
| M2 | mal kabul bilgi katlanırı açık başlasın | SE02 |
| M3 | tedarikçi seçici yer tutucusu kaldırıldı | SE03 |
| M4 | MASAK başlık şablonu geçilmedi (müşteri der) | SE04 |
| M5c | `KabulAdetDegisti` içindeki hata temizliği kaldırıldı | SE05 (ilk deneme M5b yeşildi = TEST AÇIĞI; SE05 "0" yazımıyla güçlendirildi, M5c kırmızı) |
| M6 | irsaliye onayı yine "Sipariş iptal edilsin mi?" | SE06 |
| M7b | sunucu aktife-al durum koruması kapatıldı | SH01 (M7 ilk koşum eşzamanlı derleme yüzünden eski ikili ile koştu, geçersiz; yeniden koşuldu) |
| M8 | teslim adresi GELEN/GİDEN yer değişti | SH03 |
| M9 | irsaliye aktife-alda gerekçe temizliği kaldırıldı | SH02 |
| M10b | aktife-al denetim kaydı kaldırıldı | SH01 |
| M11 | ekrandaki "aktife al" bölümü kapatıldı | SE07 |
Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: **0 hata** (6 uyarı başka şeritlerin test dosyalarında: SUBEKAPSAMA, KesifP2, SonKalanlar, Yildiz2).

## 7. denetim.sh
`DENETİM` çıktısında yalnız 2 ✗, ikisi başka sahibin: `P-30 DEMO_INSAN_SURESI` (kurucu kalemi A1) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, IBAN fikstürü). Benim sayaçlarım ✓ (DIL-SUNUCU dahil; ModalPencere artışı 0).

## 8. Canlı doğrulama (5166, gerçek yanıtlar; test kiracısı `PAKETSATINALMA-EKRAN-TEST t5`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| İptal sipariş SAS-00005 aktife-al | 200 TASLAK | 200 TASLAK; SQL: `TASLAK · NULL` (önce `IPTAL · olcum icin…`) |
| Aynısı tekrar | 409 | 409 "…(şu an: TASLAK)" |
| KISMI_KABUL sipariş aktife-al | 409 | 409 |
| İptal sevk IRS-00004 aktife-al | 200 ACIK | 200 ACIK; SQL `ACIK · NULL` |
| ACIK sevk / mal kabul irsaliyesi aktife-al | 409 | 409 ×2 |
| Olmayan kimlik / kimliksiz | 404 / 401 | 404 / 401 |
| Denetim SQL | kayıt var | `EKLEME … iptalden aktife alındı (taslak)` ×3, `… irsaliye IRS-00004 iptalden aktife alındı` |
| İrsaliye dökümü | adres | GİDEN: "Teslim adresi: Kapalıçarşı Kuyumcular Sk. No:7 Fatih"; GELEN: "Teslim adresi: Bedesten Cd. No:3 Eminönü" |
| Ekrandan tıklama (BASIT açık 1280) | bant + durum Taslak | "Sipariş yeniden taslak oldu: SAS-2026-00005…", durum Taslak |
TEZGAHTAR 403 ve başka kiracı 404 canlıda değil, HTTP testlerinde (SH01, SH02) kanıtlı.
Temizlik: API (5166) ve vekil (5167) durduruldu (portlar boş); `kiraci-temizle.sh --uygula` ile test kiracısı silindi (`PAKETSATINALMA-EKRAN%` = 0). `tenants` 21 → 23: fark eşzamanlı şeritlerin kiracıları (YILDIZ5A/B vb.), benimki değil.

## 9. Gerçek Chrome ölçümü (başsız, CDP; `sikisma-olc` K-ölçer + özel betik)
Rota `/satin-alma`, `/irsaliyeler` × {1280, 375} × {BASİT, PRO} × {açık, koyu} = 8 kombinasyon, her birinde 9 durum (liste, yeni form, kısmi kabul çekmecesi, taslak, iptal, sevk detayı, iptal onayı…), toplam 72 ölçüm; oturum modu doğrulandı (`modGercek` = istenen). Ham: `/private/tmp/kuyumcu-serit/pSATINALMA-EKRAN/olcum6/` (88 görüntü).

| Ölçüt | 8 kombinasyonda sonuç |
|---|---|
| K1 gizli görünen · K3 taşan/kırık · K5 TRY · K8 jargon · K12 ezik · K13 kırpılan · K10b · K9b · yatay kaydırma | **0 / 72 ölçümde** |
| K9 binen | Çekmeceler kapalıyken (liste) **0**; çekmece açıkken ölçer tavanı 6: hepsi "arka plan sayfası ⟂ modal çekmece" çiftleri (önceki B1 raporu § K9 ile aynı, ortak `Cekmece.razor` davranışı, değişmedi). Görüntüde binme yok |
| İrsaliye iç boşluğu | 16 px, satır kenarı 17 px (≥12) — 8/8 |
| Açıklama satırı DOM'da (açılışta) | 0 — 8/8; akordeon açılınca görünür |
| Seçici yer tutucusu | "Tedarikçi seçin"; "Henüz kayıt seçilmedi" 0 — 8/8 |
| MASAK başlığı | "bu tedarikçinin bugünkü toplamı 242.200 ₺"; "müşteri" 0 — 8/8 |
| İrsaliye çekmecesi iptal metni | "Sipariş" 0; onay penceresinde "Sipariş iptal edilsin" 0 — 8/8 |
| Taslakta boş irsaliye kartı | 0 — 8/8 |
| Aktife al düğmesi (iptal sipariş / iptal irsaliye) | var — 8/8 |
| Odakta ham sayı | fiyat/adet "42500"/"1500" — 8/8 |
Görüntüler Read ile bakıldı (en çok 4): `sa-kismi__BASIT_aydinlik_1280`, `sa-kismi__PRO_karanlik_375`, `irs-detay__PRO_aydinlik_1280`, `irs-iptal__BASIT_karanlik_375`. Son turda yalnız "aktife al" açıklamasının `title`'a taşınması sayısal olarak yeniden ölçüldü (görüntü okunmadı).

## 10. Dokunulan sayfalar (tek dokunuş kuralı)
`SatinAlma.razor`, `Irsaliyeler.razor` — EKSIK-B1-SATINALMA'dan sonra **ikinci dokunuş**. Gerekçe: spesifikasyondaki hakem/hasım bulgularının uygulanması (bu paketin tanımı).

## 11. Değişen dosyalar
`Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs` · `Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs` · `Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs` · `Kuyumcu.Core/I18n/Sozlukler.SatinAlma.cs` · `Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs` · `Kuyumcu.Web/Pages/SatinAlma.razor` · `Kuyumcu.Web/Pages/Irsaliyeler.razor` · `Kuyumcu.Web/Bilesenler/MasakFormu.razor` (2 isteğe bağlı parametre) · `Kuyumcu.Web/wwwroot/css/satinalma-ekran.css` (yeni) · `Kuyumcu.Web/wwwroot/index.html` (1 link) · `Kuyumcu.Tests/SatinalmaEkranKalanlarTestleri.cs` (yeni).

## 12. Koşul karşılama
Build 0/0 ✓ · yeni testler 11/11 ✓ · mutasyon 11 kod ≥3 kırmızı, cmp AYNI ✓ · denetim: benim sayaçlarım ✓ ✓ · tam süit yeni kırmızı 0 ✓ · canlı + SQL ✓ · gerçek Chrome 8 kombinasyon ✓ · test kiracısı silindi ✓.

## 13. Açık kalanlar (sahibiyle)
- `RaporDortTestleri.RD_KH01/RD_KH02` kırmızı: tarih penceresi 400 günü aşıyor (rapor sahibi).
- `Yildiz4CTestleri.Y4C_44…` tam süitte kırmızı, tek koşuda yeşil: kararsızlık (YILDIZ-4C-2).
- `P-30` insan süresi beyanı (kurucu A1); `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`MASAKESZAMANSinirTestleri.cs:187`).
- `tema-yildiz.css` `.kart-govde` yatay boşluğu 0: diğer kartlarda da aynı sorun olabilir (YILDIZ-4C-2'ye not).
- Odakta binlik ayracı: kullanıcı hangi alanda gördüyse alan adı gerekir; ölçülen alanlarda yok.

## 14. Kurucuya / mali müşavire
Mevzuat değeri eklenmedi. Teslim adresi dökümde bilgi amaçlıdır; yasal sevk irsaliyesi/e-İrsaliye yerine geçmez (önceki raporda mali müşavir sorusu duruyor).

---
# TUR 2 — tasarım hakemi KRİTİK bulgusu (iptalli sipariş çekmecesi kendiyle çelişiyor)

**Durum: BİTTİ.** Bulgu: iptalli siparişte "Bu kayıt değiştirilemez" kilit kutusu + "alış iadesi yapın" önerisi ile "Siparişi aktife al" yan yana.

## Yapılan
- `SatinAlma.razor`: `DegismezIpucu` (SatinalmaSiparisi) artık `Durum != "IPTAL"` iken çizilir; iptalli siparişte kutu YOK. "Alış iadesi" yolu (`AlternatifYol`) yalnız mal gelmiş (`KabulEdilenAdet > 0`) siparişte basılır.
- İptalli sipariş: tek cümle `#sa-iptalli-not` ("Sipariş iptal edildi. İstersen taslağa döndürebilirsin.") + "Siparişi aktife al" artık `dugme-birincil` (dolu renk, tek birincil).
- `Sozlukler.SatinAlma.cs`: yeni `IptalliNot` TR+EN. (Sunucu `kilitSebebi` metnine dokunulmadı; ekran onu iptalli kayıtta göstermiyor.)
- Test: `SE09_iptalli_siparis_kilit_kutusu_yok_tek_cumle_ve_aktife_al_birincil_duzeltme_yolu_yok` (bUnit). Paket testleri 11 → 12; `SatinalmaEkranKalanlar|EksikB1Satinalma` filtresi **41/41 yeşil**.

## Mutasyon (tur 2; hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M12 | iptal koşulu (`Durum != "IPTAL"`) kaldırıldı → kilit kutusu geri gelir | SE09 |
| M13 | iptalli not metni başka metinle değişti | SE09 |
| M14 | aktife al düğmesi `dugme-ikincil` | SE09 |
(M13'ün ilk denemesi apphost imzalama hatasıyla derlenmedi — geçersiz sayıldı, yeniden koşuldu; `AlternatifYol` koşulu iptal koşulunun arkasında olduğundan iptalli kayıtta ayrıca sınanamaz, kapandı siparişte canlı ölçümle kanıtlandı.) Sonda TEMİZ `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

## Tam süit (tur başına bir kez, son kodla)
trx `/private/tmp/kuyumcu-serit/trx-pSATINALMA-EKRAN-t2`: **12363 toplam · 12336 geçti · 24 başarısız · 3 atlandı.** Hiçbiri bu pakete ait değil (paketin dosyaları: SatinAlma.razor, Sozlukler.SatinAlma.cs, SE09). Sahipleriyle:
- SATIS-2 şeridi (Satış özet/ödeme etiketi, POS tek kasa): `SATIS2SinirTestleri.Ozet_*` ×5 ("Nakit · Ana Kasa" beklenirken "Ana Kasa"), `SATIS3EkranTestleri.T24 ×2, YKG01, T16`, `SATIS3SinirTestleriT18`, `UX4bEkranTestleri` ×3 (POS_TEK_kasada, Karma_*), `KesifP5EkranTestleri.SATIS4a`, `BasitModPosTestleri.YASAL_KAPI_masak_basit_modda…`, `CumaSAldimSadeTestleri.CUMA_S_01/02/04`, `DegisimEkranTestleri.DG_N_PRO…`.
- YILDIZ (Yıldız takvimi/kasa raporu): `Toplu1SafTestleri.TARIH_TEK…` (EvrakTakvimi.Yildiz.cs:72, KasaRaporu.Yildiz.cs:56), `FaturaEkranTestleri.Y4C_K12…(basit:false)`.
- RAPOR: `RaporDortTestleri.RD_KH01/RD_KH02` (400 gün penceresi; tur 1'de de kırmızıydı).
Tur 1'deki kırmızılar (RD_KH01/02, Y4C_44) dışındakiler çalışma ağacındaki eşzamanlı SATIS-2/Yıldız yarım işlerinden; SatinAlma dosyalarının hiçbir kırmızıyla ilgisi yok (hata iletileri okundu). Yeni kırmızı farkı (bu paketin ürünü): **0**.

## denetim.sh
4 ✗, hiçbiri bu paketin: `P-30 DEMO_INSAN_SURESI` (kurucu A1), `B-3 ESIK_ISTEMCIDE 5>4` (`Bilesenler/Fis/FisOdemeKasa.razor:15,85` ve `Islemler.YasalKapi3.cs:23` — YASAL-KAPI-2/fiş şeridi), `TOPLU-1 TARIH_TEK 2` (Yıldız), `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1`. DIL-SUNUCU ve ModalPencere sayaçları ✓.

## Canlı doğrulama (5166, yeni test kiracısı `PAKETSATINALMA-EKRAN-TEST t5`)
| Adım | Gerçek |
|---|---|
| İptalli SAS-00005 → `POST /satinalma/{id}/aktife-al` | 200 `TASLAK`; SQL `SAS-2026-00005 TASLAK NULL` (iptal gerekçesi temizlendi) |
| Aynısı tekrar / KISMI_KABUL sipariş | 409 / 409 "Yalnız iptal edilmiş sipariş aktife alınabilir (şu an: …)" |
| SQL son | 00001 TASLAK · 00002 ONAYLANDI · 00003 KISMI_KABUL · 00004 KAPANDI · 00005 TASLAK |
İrsaliye aktife-al uçları bu turda değişmedi (tur 1 kanıtı geçerli; yeni kiracıda iptalli irsaliye kurulmadığından tekrarlanmadı).
Temizlik: 5166 ve 5167 durduruldu (portlar boş); kiracı `kiraci-temizle.sh --uygula` ile silindi (`id` sorgusu 0). `tenants` 23 → 25: fark eşzamanlı şeritlerin kiracıları (PAKETYILDIZ5B, YASAL-KAPI-3, PAKET-EKRAN-DENETIM, YILDIZ-TEZGAH-ODEME…), benimki değil.

## Gerçek Chrome (başsız, CDP 9511; ham `/private/tmp/kuyumcu-serit/pSATINALMA-EKRAN/olcum7`, 16 görüntü)
`/satin-alma` iptalli (SAS-00005) ve kapanmış (SAS-00004) sipariş çekmeceleri × {1280, 375} × {BASİT, PRO} × {açık, koyu} = 8 kombinasyon × 2 durum, `modGercek` = istenen.
| Ölçüt | İptalli sipariş | Kapanmış sipariş |
|---|---|---|
| Kilit kutusu (`#degismez-ipucu-SatinalmaSiparisi`) | **yok** 8/8 | var 8/8 (olması gereken) |
| "alış iadesi" metni | **yok** 8/8 | var 8/8 |
| Tek cümle notu | "Sipariş iptal edildi. İstersen taslağa döndürebilirsin." 8/8 | — |
| "Siparişi aktife al" `dugme-birincil` | evet 8/8 | — |
| K1 gizli görünen · K3 taşan/kırık · K5 TRY · K8 jargon · K12 ezik · K13 kırpılan · K10b · K9b · yatay kaydırma | 0 | 0 |
| K9 binen | 6 (tavan; "arka plan sayfası ⟂ modal çekmece", tur 1 ve B1 raporuyla aynı, ortak `Cekmece.razor`) | 6 |
Görüntüler Read ile görüldü (2): `sa-iptal__BASIT_aydinlik_1280.png` (kilit kutusu yok, tek cümle, dolu mavi birincil düğme) ve `sa-iptal__PRO_karanlik_375.png` (koyu, 375: not iki satıra sarıyor, düğme tam genişlik, taşma yok).

## Dokunulan sayfalar
Yalnız `SatinAlma.razor` (tur 1'deki ikinci dokunuşun devamı, aynı paket); `Sozlukler.SatinAlma.cs`; `SatinalmaEkranKalanlarTestleri.cs`.

## Koşul karşılama (tur 2)
Build 0/0 ✓ · SE09 + 40 eski test yeşil ✓ · mutasyon 3 kırmızı, cmp AYNI ✓ · tam süit yeni kırmızı 0 ✓ · denetim: kendi sayaçlarım ✓ (4 ✗ başkasının) · canlı + SQL ✓ · Chrome 8 kombinasyon ✓ · kiracı silindi ✓.
