# PAKET TOPLU-7B uygulama raporu (09.10.2026)

Dalga TOPLU-7, hasımsız görünüm paketi (t2, 5162/5163). Para/veri/yasal davranışa dokunulmadı; hasımlı pakete devir gerekmedi.

## Yapılanlar
1. Aynı hata metni iki kez: `FormDurumu.SunucuHatasiTek` (ortak). Alana bağlı retde tam cümle yalnız alan altında, bant kısa genel cümle ("Kayıt yapılmadı. Kırmızı işaretli alanı düzeltin."), alan dışı retde bant tam cümle. Kasalar (yeni, düzenle), Faturalar (yeni), Kur (manuel), Emanet (giriş, çıkış) bu yola alındı. Gri ipucu kuralı modal dışına da yayıldı: `.alan-hatali .alan-ipucu` her kapta gizli (app.css).
2. Çekmece alt çubuğu: `Cekmece.Alt` parametresi. Kaydet altbilgide İptal'in SONUNA oturur (Manuel Kur, Yeni Kasa). Kimlik ve olay aynen.
3. K17: `SayfaBasi.AciklamaKatli` (varsayılan false). 8 para ekranında açıklama kapalı (i) satırında. BASIT sayfa durumunda görünen açıklama sayısı 9 → 2 (ölçüm).
4. Jargon/hukuk dili (TR+EN, `Sozlukler.cs`, `Sozlukler.KesifP1.cs`): Gider pusulaları açıklaması ve iptal metinleri, Hurda satıcı kimliği notu, Vezne defter kapıları ("tevsik"), Kur ("anlık görüntü"), POS komisyon ("mevzuat değil"), Hurda "tahlil/mutabakat farkı".
5. Faturalar yeni fatura: Tür, KDV türü, KDV oranı (hazır seçili) kapalı "Fatura ayrıntıları: Satış · Standart · %20" satırında (`<details>`, kimlikler ve durum değişkenleri aynen). Açılışta yalnız Müşteri + Toplam.
6. YASAL-PARAM-2 düzeltme formu: açıklama tek cümle; `CokSatir` value öznitelikli (kayıttan sonra eski gerekçe kalmıyordu, sayaç 0/1000 diyordu — kök neden ortak bileşen); sonuç bandı kayıttan sonra `kuyumcuOdak.alana("ysy-basari")` ile görünür bölgeye gelir; "Doğru değer" binlik gruplu ("1.500.000", ölçüldü).
7. "Hurda alımı gir": davranış doğru (ölçüm: /islemler?tur=HURDA_ALIS, BASIT'te "Alış · eski altın", PRO'da "Hurda Alış" sekmesi açık). Rota değişikliği gerekmedi; test ile sabitlendi.

## Sapmalar ve gerekçeleri
- İkincil düğme boyu: ölçüm (boy.mjs, 12 rota) üç kademe gösterdi: şerit/form 44·16, satır/kart 34·14, Emanet ana düğmeleri 52·17. Aynı kapsayıcıda karışık kardeş 0. Bu olcek.css (1b/1e) tasarımıdır, değiştirilmedi. Kabul "tek ölçek" ifadesinin sıkı yorumu (iki kademeyi bire indirmek) kurucu kararı gerektirir; açık kalanlara yazıldı.
- AdresZorunlu / MalAciklamasiZorunlu (pusula alan hataları), Kasa PRO açıklaması ("pasife alınan"), "Satıcı kimliği eksik" ifadesi: mevcut testler (S3f, KesifSozlukTarama, S3d) bu metinleri sabitlediği için geri alındı; "VUK md.234" atfı bu iki alan hatasında kaldı (yalnız "Satıcı kimliği eksik" cümlesinden md atfı çıktı).
- Takas hurda satıcı metinleri (ParaDemo2/3, Islemler) para ekranı değil, testlerle sabit; dokunulmadı.
- Hurda alımı derin bağlantısı `SorgudanSuzgecOku` yolunda `AltinAyarlariYukle` çağırmıyor (adres zorunluluğu varsayılan true = tutucu kalıyor). Yasal davranış olduğu için DOKUNULMADI; hasımlı pakete devir önerisi.
- Önceki turdaki 4 test sapması (S3d, S3f, P8_04, CariEkran "adres textarea metni") düzeltildi: CokSatir hem `value` hem metin düğümü taşır.
- Çok alt bileşen dokunuşu: `SayfaBasi.razor` ve `Cekmece.razor` listedeki ortak bileşen alanındadır (ek parametre, geriye uyumlu).

## Migration
Gerekmedi.

## Testler
- Yeni: 12 test (`Toplu7BTestleri.cs`, T7B01a/b, T7B02–T7B11).
- Tam süit, tur 2 (son kodla, tek kez): toplam 11730, geçti 11726, başarısız 2, atlanan 2. Kırmızılar MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar ve YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner; ikisi tek başına koşunca 15/15 geçti (paylaşılan test veritabanı kararsızlığı, bu paketin değil).
- Tur 1 tam süit: 11730 toplam, 9 kırmızı; 4'ü bu paketin (yukarıda düzeltildi), KaporaGecmisSatiri/SozlesmeYansima + KE07 (EKSIK-B1), KesifP5 SATIS4c ve Y4A (tek başına geçer, kararsız).

## Mutasyon (10/10 kırmızı, cmp AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M7B-1 | SunucuHatasiTek bantı tam cümle döndürür | T7B01a, T7B02 |
| M7B-2 | CokSatir value özniteliği yok | T7B05, T7B10 |
| M7B-3 | SayfaBasi katlı dal kapalı | T7B06 |
| M7B-4 | Cekmece `@Alt` yok | T7B07, T7B03 |
| M7B-5 | global gri ipucu gizleme kuralı yok | T7B04 |
| M7B-6 | Pusula açıklaması eski VUK metni | T7B08 |
| M7B-7 | Faturalar ayrıntı satırı kimliği değişti | T7B09 |
| M7B-8 | YasalParamFormu bandı görünür yapmaz | T7B10 |
| M7B-9 | HurdaAlimiAdresi "/islemler" | T7B11 |
| M7B-10 | Kasalar yeni catch eski çift yazım | T7B02 |
Yedekler /private/tmp/kuyumcu-serit/mut-yedek/TOPLU-7B ve TOPLU-7B-son; hepsi `cmp` ile AYNI, son mutasyondan sonra temiz build 0 uyarı 0 hata. Not: ilk mutasyon turunda başka şeridin eşzamanlı build'i eski ikiliyi bıraktı; dosyalar touch edilip yeniden derlendi.

## Denetim
`denetim.sh`: tek ihlal P-30 DEMO_INSAN_SURESI (görev tanımında hariç). İlk koşumda ToListAsync 142>140 ve CONTRACTS_DISI_MODEL (KaporaGecmisSatiri, EKSIK-B1) vardı; ikinci koşumda yoktu. Bu paketin sayaçları ✓.

## Gerçek Chrome ölçümü (başlıksız, 8 kombinasyon × 12 para rotası, sayfa + açılan form)
1280/375 · BASIT/PRO · açık/koyu: taşan 0, kırık 0, '[hidden] görünen' 0, yatay kaydırma 0, form açılışında tekrarlı hata 0. K9 "binen" 6–10 sayısı modal arkasındaki sayfa öğeleridir (bilinen ölçer kusuru, kuyruktaki "ölçer düzeltmesi" maddesi).
| Ölçüm | Önce | Sonra |
|---|---|---|
| Yeni Kasa sunucu reddi, aynı cümle | 2 kez (alan altı + bant) | 1 kez (bant kısa genel cümle) |
| Yasal düzeltme, kayıt sonrası gerekçe | eski metin kalıyor, sayaç 0/1000 | boş, sayaç 0/1000 |
| Yasal düzeltme sonuç bandı | viewport dışı (top 850 / 800) | görünür (top 377 / 800; 375'te 364 / 812) |
| K17 görünen açıklama (BASIT, 12 rota) | 9 | 2 (vez-defter notu + etiket); PRO 6 |
| Çekmece Manuel Kur/Yeni Kasa | Kaydet gövdede | altbilgi: İptal@817 → Kaydet@896 (1280), @17 → @96 (375) |
| Yeni fatura açılışı | 3 radyo grubu açık (8 radyo) | grup kapalı, 3 etiket görünür |
Görüntüler (Read ile görüldü): hy-BASIT.png, fat-once-fk-yeni-BASIT-aydinlik-1280.png, ret-sonra-kur-BASIT-karanlik-375.png, o8-son-fk-yeni-PRO-karanlik-375.png (dizin /private/tmp/kuyumcu-serit/TOPLU-7B). Diğer kombinasyonların görüntüleri aynı dizinde o8-son-*.png, ret-*.png, yasal-*.png.
Ağ taklidi uyarısı: sunucu reddi ve yasal düzeltme akışı gerçek Chrome'da `fetch` yakalanarak taklit edildi (yasal parametre tablosuna YAZILMADI; kiracı jetonu sistem ucuna 503 fail-closed aldı). Gerçek sunucu reddi (aynı ad kasa 409) ayrıca canlı doğrulandı.

## Canlı doğrulama (5162) ve SQL
| Adım | Beklenen | Gerçek |
|---|---|---|
| Kasa listesi | 200 | 200 (1 kasa) |
| Yeni kasa "T7B Ret Kasa" | 200 | 200; SQL kasa sayısı 1 |
| Aynı adla 2. kasa | 409 | 409 "adlı kasa bu şubede zaten mevcut"; SQL 1 (çift yok) |
| Boş ad | 400 | 400 "Ad zorunludur", alanlar=ad |
| GET /yasal-parametreler | 200 | 200, yonetimAcik=false |
| Kiracı jetonu /sistem/... | 403 beklenmişti | 503 (Sistem:Anahtari yapılandırılmamış, fail-closed); yasal_parametreler satırı 24, değişmedi |

## Temizlik
5162 API ve 5163 vekil durduruldu (dinleyen yok). Test kiracısı silindi (`kiraci-temizle.sh --uygula`); tenants 18 → 19 → 18, kasalar 0.

## Değişen dosyalar (bu paket)
Sozlukler.cs, Sozlukler.KesifP1.cs, Sozlukler.YasalParam2.cs, Sozlukler.Toplu7B.cs (yeni), FormDurumu.cs, Form/CokSatir.razor, SayfaBasi.razor, Cekmece.razor, YasalParamFormu.razor, Pages: Kasalar, Kur, Faturalar, Emanet, Vezne, Pos, Gider, GiderPusulalari, BankaEkstre (.razor), wwwroot/css/app.css, katlanir.css, Kuyumcu.Tests/Toplu7BTestleri.cs (yeni). Ayrıca ağaçta başka şeritlerin yüzlerce değişikliği var.

## Koşul karşılama
Build 0/0 ✓ · yeni test 12 ✓ · mutasyon 10/10, cmp AYNI ✓ · denetim yalnız P-30 ✓ · tam süit yeni kırmızı 0 (2 kararsız kırmızı tek başına geçer) ✓ · gerçek Chrome 8 kombinasyon ✓ · canlı + SQL ✓ · kiracı silindi ✓ · COMMIT yok ✓ · para/veri/yasal davranış değişmedi ✓ · ikincil düğme "tek ölçek": KISMİ (üç kademe bilinçli, değiştirilmedi).

## TUR 3 — doğrulayıcı KRİTİK bulgusu: ikincil düğme TEK ÖLÇEK (09.10)
Önceki doğrulama "h34/14 ↔ h44/16" çiftini aynen yeniden üretti; "kurucu kararı" gerekçesi kabul edilmedi. Kapatıldı.
- Kural (olcek.css, dosya sonu, yeni bölüm): `.ana-icerik` ve `.cekmece` içindeki HER `.btn-kucuk`/`.dugme-kucuk` = 44 px / 16 px (`--dugme-yuksekligi-kucuk` ELEMANIN üstünde `--dugme-yuksekligi`ye eşitlenir, yazı `--yazi-kontrol`; app.css ve olcek.css padding/min-height hesapları kendiliğinden uyar; belirteçlerin kendisi DEĞİŞMEDİ, çünkü `--yazi-kontrol-kucuk` düğme dışında ~20 yerde kullanılıyor). Çekmece `.btn/.dugme` yazısı 18 → 16 px.
- İstisnalar (gerekçeli): (a) tablo satırı içi eylem (`td/th` içi) ve `.satir-menu`: yoğun liste, yanında 44 px kardeş yok; ölçülen 13 rotada bu sınıfta 34 px düğme KALMADI. (b) Emanet `btn-buyuk` 52/17: sayfanın iki büyük seçim düğmesi (Emanet al / iade et) çift olarak aynı boyda, ikincil olan "iade et" de 52 → aynı kardeş grubunda tutarsızlık yok; ayrı boy ailesi (olcek.css § 2 "üç boy"), değiştirilmedi.
- Not: görev metnindeki rota `/yasal-param` YOK (404 "Sayfa bulunamadı"; K8 jargon 1 = 404 sayfasının yankıladığı adres). Gerçek rota `/yasal-sinirlar`; ölçüm onunla yapıldı.
Ölçüm (tutarlilik.mjs, 13 para rotası, çıktı /private/tmp/kuyumcu-serit/t7b2/tut):
| Sınıf grubu | Önce (1440) | Sonra 1440 | Sonra 375 |
|---|---|---|---|
| btn-ikincil btn-kucuk | h34/14 ×6 + h44/16 ×2 | h44/16 ×8 | h44/16 ×8 |
| sınıfsız (dugme-kucuk) | h44/16 ×6 + h34/14 ×2 | h44/16 ×8 | h44/16 ×8 |
| btn-birincil btn-kucuk | h44/16 ×4 | h44/16 ×4 | h44/16 ×4 |
| btn-buyuk (Emanet) | h52/17 ×2 | h52/17 ×2 (istisna) | aynı |
BASIT ve PRO satırları birebir aynı. Sekiz kombinasyon (1280/375 · BASIT/PRO · açık/koyu) × 13 rota = 104 ölçüm (sikisma-olc K1..K12 + yatay + ana alandaki küçük düğme boyu): taşan 0, kırık 0, [hidden] görünen 0, yatay kaydırma 0, 'TRY' 0, hizasız 0, dikey düğme 0, ana alan küçük düğme boyu hepsi 44/16. Çekmece (Manuel Kur Gir): İptal/Kaydet 44/16 (BASIT açık+koyu 1280/375, PRO koyu 1280/375); PRO açık kombinasyonunda çekmece ölçümü zamanlama yüzünden boş döndü (çekmece açılmadı), aynı CSS kuralı diğer 3 kombinasyonda doğrulandı, API kapatıldığı için tekrarlanmadı. Görüntüler (Read ile görüldü): /private/tmp/kuyumcu-serit/t7b2/g/kur-cekmece-BASIT-karanlik-375.png (düğmeler İptal | Kaydet, aynı boy), kasalar-PRO-karanlik-1280.png (+ Hareket / Geçmiş / Günü Kapat / ⋯ ile + Yeni Kasa aynı yükseklik).
Testler: yeni T7B12 (toplam yeni test 13). Mutasyon 3/3 kırmızı, cmp AYNI: M7B-11 `--dugme-yuksekligi-kucuk: var(--dugme-yuksekligi-kucuk)` → T7B12; M7B-12 `font-size: var(--yazi-kontrol-kucuk)` → T7B12; M7B-13 `.cekmece .btn-kucuk` seçicisi silindi → T7B12. Yedek /private/tmp/kuyumcu-serit/mut-yedek/TOPLU-7B-t2/olcek.css.
Tam süit (tur 3, tek kez, KUYUMCU_TEST_DB=kuyumcu_test_t2): toplam 11800, başarılı 11797, başarısız 0, atlanan 3. Build 0 hata 0 uyarı (ara bir build başka şeridin yarım yazılmış KAPORAESZAMAN test dosyası yüzünden kırmızıydı, sonraki build temiz). denetim.sh: tek ihlal P-30 DEMO_INSAN_SURESI (görev tanımı gereği hariç; kurucu kronometresi). Kiracı PAKET7B2-TEST silindi (kiraci-temizle.sh --uygula); tenants 24 → 25 → 24; 5162 API ve 5163 vekil durduruldu.
Ek değişen dosyalar (tur 3): Kuyumcu.Web/wwwroot/css/olcek.css, Kuyumcu.Tests/Toplu7BTestleri.cs.

## Açık kalanlar
- (KAPANDI, aşağıdaki Tur 3) İkincil düğme ölçeği.
- Pusula alan hatalarında "VUK md.234" atfı kaldı (testle sabit); S3f testi güncellenirse sadeleşir.
- Hurda derin bağlantı yolunda adres zorunluluğu yapılandırması yüklenmiyor (tutucu); hasımlı pakete.
- P-30 kronometreli demo ölçümü (kurucu).
- MigrationHazirlik / YayinAcilis testleri paylaşılan test veritabanında kararsız; KaporaGecmisSatiri çift tanımı EKSIK-B1 şeridinde.
- Kurucu/mali müşavir: mevzuat hükmü eklenmedi veya değiştirilmedi; sorulacak soru yok.
