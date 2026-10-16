# Paket 37 — Pozisyon / net varlık ekranı

> Katman: Katman 5 — Raporlama (34, 35, 36, 37) · Tahmin: 2–3 gün (PARITE ile aynı) gün · Önkoşullar: 34, 35 · Kaynak: katman mimarı K5 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 6

## Hedef
Kuyumcunun 'bu dükkânda benim neyim var, ne kadarı benim değil, altında ne kadar açık pozisyondayım' sorusuna tek ekrandan cevap. Bugün parçalar var (kasa, banka, stok, cari, emanet) ama hiçbiri toplanmıyor ve açık pozisyon kavramı kod tabanında HİÇ YOK.

## Kodda bugünkü durum
Kod tabanında 'pozisyon' / 'net varlık' araması 0 eşleşme (Kuyumcu.Api + Kuyumcu.Web + Kuyumcu.Core, obj/bin hariç). Parçalar: kasalar.bakiye + para_birimi (Entities.cs:413-421), banka_hesaplari.bakiye + para_birimi (:442-456), stoklar.toplam_has_gram (RaporModulu.cs:428 StokDegeri, maden ayrımı :466-489), stok maliyeti (KarZararServisi.cs:205 StokDegerlemesi, konsinye dışlanır :209-211), cari_bakiyeler.tl_bakiye + has_gram_bakiye (Entities.cs:140-148), emanet net has (EmanetModulu.cs:213-218 EmanetNet — cari bazında, kiracı geneli toplamı YOK). Kur: KurSnapshot.AltinHasFiyati/UsdTry/EurTry/GbpTry (Entities.cs:100-110).

## Kapsam
**İçinde:**
- Tek uç ve tek ekran: varlık / yükümlülük / net varlık / açık pozisyon
- Para birimi bazında kasa+banka toplamı (TRY/USD/EUR/GBP)
- Altın has pozisyonu: stok + cari has alacağı − emanet − cari has borcu
- Diğer madenlerin ayrı satırda gram olarak gösterimi (TL karşılığı YOK)
- Konsinye malın 'elde ama bize ait değil' satırı
- Maliyeti bilinmeyen stok beyanı + güvenilirlik
- Şube kırılımı (opsiyonel subeId)

**Dışında:**
- Tarihsel pozisyon serisi / snapshot tablosu (ortak karar 1 gereği REDDEDİLDİ)
- Döviz cari bakiyesi (PARITE 27 yapılmadı; cari_bakiyeler'de USD/EUR kolonu YOK)
- Gümüş/platin TL değerlemesi (gram kotasyonu modelde yok — A2 gümüş tuzağı)
- Çek/senet portföyü (PARITE 29, yapılmadı)
- Sabit kıymet, demirbaş, borç/kredi kalemleri (kod tabanında yok)
- Vergi karşılığı, amortisman

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| — | ŞEMA DEĞİŞİKLİĞİ YOK | - | (tenant_id, tur, tarih) paket 34'te eklendi; emanet net toplamı onu kullanır | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/raporlar/pozisyon?subeId&kurSnapshotId` | SAHIP,MUDUR,MUHASEBE | Yanıt: {kurSnapshotId, kurZamani, altinHasFiyati, usdTry, eurTry, gbpTry, varliklar:{kasalar:[{paraBirimi,tutar,tlKarsiligi}], bankalar:[...], stok:{altinHasGram, tlMaliyet, maliyetiBilinmeyenAdet, digerMadenler:[{maden,gram}]}, cariAlacak:{tl,has}}, yukumlulukler:{cariBorc:{tl,has}, emanetHasGram}, netVarlikTl, netVarlikHesaplanabilir, acikPozisyonHasGram, bizeAitOlmayan:{konsinyeUrunSayisi, konsinyeTahminiTl}, guvenilirlik, sebepler:[]}. Kur yoksa tlKarsiligi ve netVarlikTl NULL + sebep KUR_YOK. | ModulYetkisi(rapor) · kur fail-closed |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/PozisyonServisi.cs` — PozisyonServisi(KuyumcuDbContext db, Maliyet.KarZararServisi karZarar) + PozisyonController ([Route("api/v1/raporlar")]). Kasa/banka: GroupBy(ParaBirimi).Sum(Bakiye) SUNUCUDA. Stok has: GroupBy maden (RaporModulu.cs:466-489 ile aynı kural — Ayarlar.MadenBul). Stok maliyeti: KarZararServisi.StokDegerlemesi yeniden kullanılır (kopyalanmaz; iki gerçek doğmasın). Cari: CariBakiyeler'den SUNUCUDA iki yönlü toplam (TlBakiye>0 alacak, <0 borç; has için ayrı). Emanet: hareketler'den Tur IN (EMANET_GIRIS, EMANET_CIKIS) SUM(has_grami) — EmanetModulu.cs:213'teki tek-cari sorgusunun kiracı-geneli hâli. Kur: kurSnapshotId verilmezse en son KurSnapshot (indeks (tenant_id, olusturuldu) var).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` — StokDegerlemesi'nin iç hesabı tipli bir sonuç record'una çıkarılır (public StokDegerlemeSonucu HesaplaAsync(...)), controller metodu onu sarar. Dönen JSON ŞEKLİ DEĞİŞMEZ (KarZarar.razor:173-176).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Rapor.PozisyonServisi>();
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — YENİ SozlukPozisyon (~30 anahtar: Baslik, Aciklama, Varliklar, Yukumlulukler, NetVarlik, AcikPozisyon, Kasa, Banka, Stok, CariAlacak, CariBorc, Emanet, EmanetNotu, BizeAitDegil, Konsinye, KurYok, DigerMadenler, DigerMadenNotu, MaliyetiBilinmeyen, Guven*) + Sozluk'a alan + SozlukNav.Pozisyon + TR/EN.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Yetki.Erisebilir("rapor") bloğuna NavLink /raporlar/pozisyon.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs` — pozisyon kaydı.

## Ekranlar
- `/raporlar/pozisyon` · `dotnet/Kuyumcu.Web/Pages/Pozisyon.razor` — YENİ: üstte 4 StatKart (Net varlık TL · Açık pozisyon has gram · Toplam alacak · Toplam borç); altında Varlıklar / Yükümlülükler / Bize ait olmayan üç kart; kur bandı (hangi kur kaydı, ne zaman); güvenilirlik rozeti + sebep listesi; Yazdır düğmesi. Açık pozisyon negatifse kırmızı ve 'sattığın malın yerine aynı gramı koyamazsın' uyarısı (KarZararServisi.cs:14-21 ile aynı dil). (menü/yetki: rapor modülü)

## İş kuralları (fail-closed)
- EMANET VARLIK DEĞİL, YÜKÜMLÜLÜKTÜR. Müşterinin bizde duran altını bizim değildir; varlık sayılırsa net varlık ve açık pozisyon ikisi birden şişer ve kuyumcu kendini olduğundan güvende sanır.
- KONSİNYE NET VARLIĞA KATILMAZ ama 'elde duran, bize ait olmayan' satırında GÖRÜNÜR (KarZararServisi.cs:207-211 kuralının devamı). Hiç göstermemek de yanlış olurdu: vitrinde duran malın nereden geldiği sorulur.
- AÇIK POZİSYON = (stok altın has + cari has alacağı) − (emanet has + cari has borcu). Yalnız stoğa bakmak yükümlülükleri saklar.
- FARKLI MADENLER TOPLANMAZ ve GÜMÜŞ/PLATİN TL'YE ÇEVRİLMEZ: gram kotasyonu modelde yok; altın kuruyla çarpmak 12 g gümüş kolyeyi 76.972 TL yapıyordu (A2 kanıtı). Diğer madenler yalnız gram olarak listelenir, netVarlikTl'ye katılmaz ve bu durum sebepler[] içinde beyan edilir.
- KUR YOKSA FAIL-CLOSED: TL karşılığı üretilmez (NULL + sebep KUR_YOK), gram ve efektif döviz miktarları yine döner. Kur=1 varsaymak A12'de M70 ile yasaklanmış bir hatadır.
- TEK KUR KAYDI: tüm çevrimler aynı kurSnapshotId ile yapılır ve o kimlik + zamanı yanıtta döner. İki farklı kurla çevrilen bir tablo kendi içinde tutarsız olur.
- MALİYETİ BİLİNMEYEN STOK SIFIR TL SAYILMAZ: netVarlikTl'ye katılmaz, adedi ve güvenilirlik etkisi beyan edilir.
- DÖVİZ CARİ BAKİYESİ YOKTUR (cari_bakiyeler yalnız TL + has): rapor cari bacağını TL ve has olarak verir ve 'döviz cari bakiyesi bu sürümde tutulmuyor' sebebini yazar. Uydurma USD alacak üretilmez.
- POZİSYON ANLIK GÖRÜNTÜSÜ SAKLANMAZ: snapshot tablosu açılmaz (ortak karar 1). Tarihsel seri isteniyorsa ayrı paket olarak oylanır.
- SALT OKUR: hiçbir bakiye, parti ya da hareket yazılmaz.

## Testler
- `dotnet/Kuyumcu.Tests/PozisyonTestleri.cs` [DB]
  - Kasa ve banka bakiyeleri para birimi bazında toplanır, karışmaz
  - USD kasa TL'ye kur kaydındaki usdTry ile çevrilir; sonuç kuruşu kuruşuna
  - Kur kaydı yoksa netVarlikTl NULL ve sebepler KUR_YOK içerir
  - Emanet YÜKÜMLÜLÜK tarafındadır; açık pozisyonu AZALTIR
  - Emanet çıkışı sonrası yükümlülük düşer (net has = giriş + çıkış toplamı)
  - Konsinye ürün netVarlikTl'ye katılmaz, bizeAitOlmayan'da görünür
  - Gümüş stok toplamHas'a KATILMAZ, digerMadenler'de gram olarak döner ve TL karşılığı YOKTUR
  - Maliyeti bilinmeyen parti netVarlikTl'ye 0 TL olarak katılmaz, adedi beyan edilir
  - Cari pozitif bakiye alacak, negatif bakiye borç tarafına gider
  - Açık pozisyon = stok has + cari has alacak − emanet − cari has borç (dört bileşenli senaryo)
  - subeId verilince kasa/stok o şubeye süzülür, cari (şubesiz) süzülmez ve bu sebepler'de yazar
  - Yabancı kiracının kasası/emaneti pozisyona girmez
- `dotnet/Kuyumcu.Tests/PozisyonEkranTestleri.cs` [bUnit]
  - Açık pozisyon negatifken kırmızı uyarı çıkar
  - Kur yokken TL kartları '—' gösterir ve KUR_YOK sebebi ekranda yazar
  - Emanet Yükümlülükler kartında, Varlıklar kartında DEĞİL
  - Konsinye 'Bize ait değil' kartında
  - Diğer madenler gram olarak, TL sütunu olmadan çizilir

## Mutasyon kanıtları
- M131 — emanet varlık tarafına alınırsa → Emanet_YUKUMLULUKTUR KIRMIZI
- M132 — konsinye net varlığa katılırsa → Konsinye_net_varliga_katilmaz KIRMIZI
- M133 — kur yokken kur=1 varsayılırsa → Kur_yoksa_TL_karsiligi_NULL KIRMIZI
- M134 — gümüş altın kuruyla TL'ye çevrilirse → Gumus_TL_karsiligi_URETILMEZ KIRMIZI
- M135 — maliyeti bilinmeyen stok 0 TL ile toplanırsa → Maliyeti_bilinmeyen_stok_sifir_sayilmaz KIRMIZI
- M136 — negatif cari bakiye alacak gibi toplanırsa → Negatif_cari_bakiye_BORCTUR KIRMIZI
- M137 — açık pozisyondan cari has borcu düşülmezse → Acik_pozisyon_cari_has_borcunu_dusar KIRMIZI

## Canlı doğrulama
- Yeni kiracı (kur kaydı gelene kadar bekle); TRY kasa + USD kasa + 1 banka hesabı aç
- 10 adet altın ürün alışı (maliyet bilinir) + 1 gümüş ürün + 1 konsinye ürün
- 1 veresiye satış (cari alacak) + 1 emanet girişi (müşteri altını)
- GET /raporlar/pozisyon → kasa/banka/stok/cari/emanet rakamları SQL ile birebir
- Açık pozisyon elle hesapla (perl ile, zsh sed tuzağına düşme) ve yanıtla karşılaştır
- Gümüş satırı digerMadenler'de, TL karşılığı yok; toplamHas yalnız altın
- Konsinye ürün bizeAitOlmayan'da, netVarlikTl'ye katılmamış
- Emanet çıkışı yap → yükümlülük düşüyor, açık pozisyon artıyor
- Kur kaydı olmayan ikinci kiracıda (kur zamanlayıcısı öncesi) → netVarlikTl null + KUR_YOK
- TEZGAHTAR jetonu → 403
- İki kiracıyı da sil

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs`
- `DOKUNULMAZ: Entities.cs, KuyumcuDbContext.cs, denetim.sh, HareketFabrikasi.cs, EmanetModulu.cs (okunur, değişmez)`

## Mevzuat dayanağı
- — — **VARSAYIM** — Bu ekran için doğrudan bir mevzuat dayanağı ARANMADI ve İDDİA EDİLMİYOR: net varlık/açık pozisyon bir YÖNETİM raporudur, resmî bir beyan ya da defter değildir. Ekranda 'yönetim raporu, resmî envanter/bilanço yerine geçmez' ibaresi bulunur.
- Emanet altının mülkiyeti (vedia/emanet) — TBK hükümleri — **DOGRULANMALI** — Emanetin yükümlülük sayılması muhasebe mantığından türetildi, doğrulanmış bir hükümden değil. Kural koda giriyor ama YALNIZ raporlama yönünde (fail-safe: yükümlülük saymak pozisyonu daha kötümser gösterir, iyimser değil).

## Riskler
- Net varlık rakamı müşteride 'bilanço' sanılabilir → ekran ve yanıt metninde açık ibare zorunlu.
- Şube kırılımında cari bakiyeleri şubesizdir (cari_bakiyeler'de sube_id YOK); subeId verilince tablo yarı-şubeli olur → sebepler[] ile beyan edilir, sessiz geçilmez.
- Çek/senet (PARITE 29) ve döviz cari bakiyesi (PARITE 27) yapılmadığı için net varlık EKSİKTİR; bu eksiklik güvenilirlik sebeplerine yazılmalı, yoksa rakam olduğundan emin görünür.
- KarZararServisi'nden StokDegerlemesi'nin dışarı açılması mevcut JSON şeklini bozarsa KarZarar.razor sessizce boşalır → şekil regresyon testi.

## Açık sorular
- Rol: SAHIP+MUDUR mı, yoksa diğer raporlarla aynı SAHIP+MUDUR+MUHASEBE mi? Net varlık işletme sırrı yoğunluğu yüksek; ama MUHASEBE zaten kâr-zarar ve stok maliyetini görüyor, kısıtlamak tutarsız olur. Öneri: aynı üçlü. Karar oylamaya.
- Konsinye malın 'tahmini TL' değeri konsinye_birim_bedel × adet olarak mı verilsin, yoksa hiç TL verilmesin mi? Bedel bizim ödeyeceğimiz tutardır, malın satış değeri değildir.
- Tarihsel pozisyon serisi (dün/geçen ay ne kadardı) istenirse snapshot gerekir ve ortak karar 1 ile çelişir — ayrı paket olarak mı açılsın?

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Şema değişikliği YOK, yeni tablo YOK, snapshot tablosunu reddetmesi ortak karar 1 ile tutarlı ve doğru. Emanetin yükümlülük sayılması, konsinyenin net varlığa katılmaması, gümüşün altın kuruyla çevrilmemesi ve kur yokken fail-closed davranış — dördü de projenin mevcut kanıtlı kararlarının doğru uzantısı. Tek gerçek risk emanet işaretinin ölçülmeden varsayılması.
  - Koşullar: OKUNMADI — ZORUNLU ÖLÇÜM: emanet net has'ın işaret sözleşmesi. EmanetModulu.cs:213-218 bu oturumda AÇILMADI. Uygulayıcı EMANET_GIRIS ve EMANET_CIKIS satırlarının has_grami İŞARETİNİ deftere bakarak ölçsün; 'giriş + çıkış toplamı' varsayımıyla kod yazmasın. İşaret ters varsayılırsa yükümlülük varlık gibi görünür ve paketin TEK amacı çöker. · KarZararServisi.StokDegerlemesi dışa açılırken DÖNEN JSON ŞEKLİ DEĞİŞMEYECEK (KarZarar.razor:173-176 buna bağlı). Şekil regresyon testi paketin ilk testi olsun. · ROL KARARI (açık soru kapatılıyor): SAHIP + MUDUR + MUHASEBE — diğer rapor uçlarıyla AYNI üçlü. Gerekçe: MUHASEBE zaten kâr-zararı ve stok maliyetini görüyor (KarZararController satır 285-307); net varlığı ondan saklamak tutarsızdır ve sızıntıyı azaltmaz. · KONSİNYE KARARI (açık soru kapatılıyor): konsinye için TL DEĞERİ ÜRETİLMEYECEK. Yalnız adet + 'değerlenmedi, bize ait değil' beyanı. Konsinye birim bedeli bizim ÖDEYECEĞİMİZ tutardır, malın değeri değildir; TL basmak onu net varlığın yanında ikinci bir varlık rakamı gibi gösterir. · TEK KUR KAYDI kuralı teste bağlanacak: yanıttaki tüm TL çevrimleri aynı kurSnapshotId ile yapılmış olmalı ve o kimlik yanıtta dönmeli. İki kurla çevrilen tablo kendi içinde tutarsızdır. · EKSİKLİK BEYANI ZORUNLU: döviz cari bakiyesi (PARITE 27) ve çek/senet (PARITE 29) yapılmadığı için net varlık EKSİKTİR; bu iki eksiklik sebepler[] içinde HER ZAMAN dönsün, yalnız risklerde yazılı kalmasın. · önkoşul [34, 35]: 35 gerçekten gerekli değil (pozisyon kâr hesabı kullanmıyor); önkoşul [34]'e daraltılsın. Gereksiz önkoşul zinciri paketi geciktirir.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Salt okur, snapshot tablosu yok, kur fail-closed, emanet yükümlülük (kötümser yön = güvenli yön), farklı madenler toplanmıyor. Tek açık: istemciden gelen kurSnapshotId kiracı sınırında açıkça doğrulanmalı.
  - Koşullar: K37-1: `kurSnapshotId` sorgu parametresi verilirse `k.Id == kurSnapshotId && k.TenantId == islem.TenantId` ile çekilir (ADR-0010 çift katman); yabancı kiracının kur kaydı → 404 (kur=1 varsayımı gibi sessiz düşüş YOK). Test 'yabancı kurSnapshotId 404' + mutasyon. · K37-2: Cari alacak/borç toplamı yalnız TOPLAM döner; cari listesi/adı dönmez (kişisel veri yok → okuma izi gerekmez; bu gerekçe koda yazılır). · K37-3: PozisyonController sınıf düzeyinde [Authorize][RolGerekli('SAHIP','MUDUR','MUHASEBE')][ModulYetkisi('rapor')].
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Şema değişikliği yok, tüm hesaplar mevcut servislerden; iş kuralları (emanet yükümlülük, gümüş TL'ye çevrilmez, kur fail-closed) mutasyonlarla eşleşiyor. Ancak 'kur yok' fikstürü mevcut TestKurulum ile kurulamaz, rol ve konsinye TL soruları test sözleşmesini belirsiz bırakıyor.
  - Koşullar: 'Kur kaydı yoksa netVarlikTl NULL' testi: TestKurulum.KiraciKur (TestOrtami.cs:163-167) HER ZAMAN bir KurSnapshot yazar; test ikinci kiracıyı kurup vt.OlusturSistem() ile KurSnapshots satırını SİLER ya da KiraciKur'a 'kurYaz:false' parametresi ekler (opsiyonel, varsayılan true; mevcut çağrılar değişmez). Fikstür belgelenmeden bu test yazılamaz. · Rol kararı (SAHIP+MUDUR mı, +MUHASEBE mi) oylanmadan controller niteliği ve HTTP testi yazılmaz; testte rol listesi TEK sabitten okunur ki karar değişince tek satır değişsin. · 'Konsinye tahmini TL' açık sorusu karara bağlanana kadar test konsinyeTahminiTl'yi ASSERT ETMEZ (yalnız konsinyeUrunSayisi); aksi hâlde karar testi kırar. · Emanet işareti: test EmanetServisi üzerinden giriş+çıkış yapar (EmanetModulu.cs:213-217 EmanetNet SUM(has_grami) desenine göre giriş +, çıkış −); ham Hareketler.Add ile işaret uydurulmaz. · 'Yabancı kiracının kasası/emaneti pozisyona girmez' çok katmanlı koruma tuzağı: mutasyon kanıtı sayılmaz; ikinci katman filtresiz bağlamda ayrıca sınanır ya da regresyon diye etiketlenir. · KarZararServisi.StokDegerlemesi'nin tipli record'a çıkarılması: JSON ŞEKLİ regresyon testi mevcut MaliyetTestleri.Stok_degerlemesi_elde_kalan_mali_maliyetiyle_verir (satır 524) DEĞİŞMEDEN yeşil kalmalı — kabul ölçütü budur. · PozisyonEkranTestleri: kur yokken '—' ve KUR_YOK rozet testi tr-TR kültüründe de koşar (GrafikKulturTestleri deseni) — StatKart sayı biçimi kültür tuzağına açık. · Canlı doğrulama: 'kur kaydı olmayan ikinci kiracı' için kur zamanlayıcısı 10 sn'de kayıt yazar (§6:865) → istek ilk 10 sn içinde atılır ya da zamanlayıcı kapalı API ile koşulur; iki kiracı silinmeden API durdurulur.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- ROL KARARI (açık soru kapandı): SAHIP+MUDUR+MUHASEBE — PozisyonController sınıf düzeyinde [Authorize][RolGerekli(...)][ModulYetkisi("rapor")] (Bertan + Mehmet K37-3). Test rol listesini tek sabitten okur (Aşkın).
- Önkoşul [34,35] → [34]'e daraltıldı: pozisyon kâr hesabını kullanmıyor (Bertan).
- Emanet işaret sözleşmesi ÖLÇÜLEREK saptanır (EmanetModulu.cs:213-218 bu oturumda açılmadı); test EmanetServisi üzerinden giriş+çıkış yapar, ham Hareketler.Add ile işaret uydurulmaz (Bertan+Aşkın).
- KarZararServisi.StokDegerlemesi dışa açılırken JSON ŞEKLİ DEĞİŞMEZ (KarZarar.razor:173-176); şekil regresyon testi paketin ilk testi ve MaliyetTestleri...satır 524 değişmeden yeşil kalır.
- KONSİNYE (açık soru kapandı): TL değeri ÜRETİLMEZ; yalnız adet + 'değerlenmedi, bize ait değil' beyanı. Test konsinyeTahminiTl assert etmez, yalnız konsinyeUrunSayisi.
- TEK KUR KAYDI: tüm TL çevrimleri aynı kurSnapshotId ile; kimlik yanıtta döner ve teste bağlanır. kurSnapshotId verilirse k.Id && k.TenantId eşleşmesi (ADR-0010); yabancı kiracı kur kaydı → 404, sessiz kur=1 düşüşü YOK (Mehmet K37-1).
- EKSİKLİK BEYANI ZORUNLU: döviz cari bakiyesi (PARITE 27) ve çek/senet (PARITE 29) eksik → sebepler[] içinde HER ZAMAN döner; şube kırılımında cari bakiyelerin şubesiz olduğu da beyan edilir.
- Cari alacak/borç yalnız TOPLAM döner, cari adı/listesi dönmez → okuma izi gerekmez, gerekçe koda yazılır (Mehmet K37-2).
- 'Kur yoksa netVarlikTl NULL' testi için TestKurulum.KiraciKur'a kurYaz:false seçeneği ya da KurSnapshots satırının vt.OlusturSistem() ile silinmesi gerekir (Aşkın).
- '—' ve KUR_YOK rozet testi tr-TR kültüründe de koşar (GrafikKulturTestleri deseni); canlı doğrulamada kur zamanlayıcısı 10 sn'de kayıt yazar → istek ilk 10 sn'de atılır ya da zamanlayıcı kapalı API kullanılır; 'yabancı kiracı pozisyona girmez' çok katmanlı → regresyon.
