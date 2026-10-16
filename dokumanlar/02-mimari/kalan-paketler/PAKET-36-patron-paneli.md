# Paket 36 — Patron paneli

> Katman: Katman 5 — Raporlama (34, 35, 36, 37) · Tahmin: 2–3 gün (PARITE ile aynı) — yeni hesap olmadığı için sürenin çoğu ekran ve uyarı kurgusu gün · Önkoşullar: 34, 35, 37 · Kaynak: katman mimarı K5 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 6

## Hedef
İşletme sahibinin dükkâna girmeden telefondan bakıp 'bugün ne oldu, param nerede, kim bana borçlu, bir sorun var mı' sorularını 10 saniyede cevaplaması. Mevcut ana panel tezgâhtarın operasyon ekranıdır; kâr, net varlık ve alacak orada YOKTUR ve olmamalıdır.

## Kodda bugünkü durum
Panel.razor (304 satır, @page "/"): günlük satış/alış/hurda/net has + kasa listesi + aylık grafik + uyum uyarıları (satır 29-49, yalnız SAHIP/MUDUR/MUHASEBE — UyumGorebilir :238) + kur kaynağı seçici. Beslendiği uçlar: /raporlar/gunluk (:265), /raporlar/aylik (:276), /uyum özet+süre takibi (:245-246). 'Patron' özeti YOKTUR: kâr, net varlık, açık pozisyon, geciken alacak, şube karşılaştırması panelde hiç geçmiyor. Ekran 8 ayrı isteği kendi içinde değil, tek tek atacak biçimde kurulmuş.

## Kapsam
**İçinde:**
- Tek birleşik uç: /raporlar/patron-paneli (mevcut servisleri sunucuda birleştirir)
- Yeni /patron ekranı: bugün vs dün, bu ay vs geçen ayın aynı gün sayısı
- Kâr (TL + HAS), net varlık + açık pozisyon, geciken/yaklaşan alacak, kasa+banka toplamı
- İlk 5 kârlı ürün, şube karşılaştırma özeti
- Uyarı listesi (uyum süreleri, kapanmamış gün, mutabakatsız gün, ölü stok eşiği, negatif has pozisyonu)
- Panel.razor'a yalnız SAHIP/MUDUR için 'Patron paneli' bağlantısı

**Dışında:**
- Panel.razor'un (/) mevcut içeriğinin değiştirilmesi — tezgâhtarın ekranıdır, kâr/net varlık oraya KONULMAZ
- Yeni hesap mantığı (tek satır bile)
- Push bildirim / e-posta özeti (paket 6 çatısı)
- Kullanıcının panel bileşenlerini sürükle-bırak düzenlemesi
- Mobil uyarlama (paket 43)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| — | ŞEMA DEĞİŞİKLİĞİ YOK | - | paket 34'ün indeksleri yeter | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/raporlar/patron-paneli?tarih&subeId` | SAHIP,MUDUR | Tek turda birleşik özet: {tarih, bugun:{satisTl,alisTl,hurdaTl,netHasGram,islemSayisi}, dun:{...}, degisim:{...}, ay:{satisTl,karTl,karHas,gunSayisi}, gecenAyAyniGunSayisi:{...}, pozisyon:{netVarlikTl,acikPozisyonHasGram,guvenilirlik}, alacak:{gecikenToplam,yaklasanToplam,vadesizToplam}, nakit:{kasaToplamTl,bankaToplamTl,dovizler:[]}, enKarliUrunler:[5], subeler:[], uyarilar:[{kod,siddet,mesaj,rota}], sebepler:[]}. Hesaplamaların TAMAMI mevcut servislerden gelir. | ModulYetkisi(rapor) · RolGerekli TAM eşleşme |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/PatronPaneliServisi.cs` — PatronPaneliServisi(RaporServisi rapor, Maliyet.KarZararServisi karZarar, PozisyonServisi pozisyon, SatisRaporServisi satisRapor, Masak.MasakServisi/UyumServisi kaynakları) + controller. KENDİ SORGUSU YOKTUR (tek istisna: uyarı listesi için kapanmamış gün ve mutabakatsız gün sayımı — ikisi de mevcut servislerin döndürdüğü veriden türetilir). Bir alan hesaplanamazsa NULL + sebep; panel kısmi veriyle çizilir, tamamen boş kalmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Rapor.PatronPaneliServisi>();
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — YENİ SozlukPatron (~35 anahtar: Baslik, Bugun, Dun, BuAy, GecenAyAyniGun, Degisim, Kar, NetVarlik, AcikPozisyon, Alacak, Nakit, EnKarli, Subeler, Uyarilar, UyariYok, Hesaplanamadi, KismiVeri + uyarı kodlarının metinleri) + Sozluk alanı + SozlukNav.Patron + TR/EN.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Oturum.Kullanici?.Rol is "SAHIP" or "MUDUR" bloğuna NavLink /patron (en üstte, Panel'in hemen altında).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Panel.razor` — YALNIZ bir bağlantı kartı: SAHIP/MUDUR ise 'Patron paneli →'. Mevcut kartlar, grafik, uyum uyarıları ve kur seçici DEĞİŞMEZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs` — patron-paneli kaydı (faz: panel).

## Ekranlar
- `/patron` · `dotnet/Kuyumcu.Web/Pages/PatronPaneli.razor` — YENİ: 6 StatKart (Bugün ciro + değişim · Bu ay kâr TL · Bu ay kâr HAS · Net varlık · Açık pozisyon · Geciken alacak); altında Nakit (kasa/banka/döviz), En kârlı 5 ürün, Şube karşılaştırma, Uyarı listesi (her uyarı ilgili rotaya bağlantılı). Hesaplanamayan kart '—' + sebep rozeti. Yazdır düğmesi. (menü/yetki: SAHIP veya MUDUR (sunucu [RolGerekli("SAHIP","MUDUR")] ile kapalı; menü gizlemesi tek kapı değildir))
- `/` · `dotnet/Kuyumcu.Web/Pages/Panel.razor` — DEĞİŞTİR (küçük): SAHIP/MUDUR için patron paneline bağlantı kartı. Başka hiçbir değişiklik yok. (menü/yetki: herkes (mevcut))

## İş kuralları (fail-closed)
- PATRON PANELİ YENİ HESAP YAPMAZ. Her rakam mevcut servisten gelir. İkinci bir hesap yolu ikinci bir gerçek doğurur: panelde 1.000 TL, kâr-zarar ekranında 1.100 TL gösteren bir ürün güveni bitirir. Bu kural bir mutasyonla korunur (M138).
- AY KARŞILAŞTIRMASI EŞİT GÜN SAYISIYLA: 16 Eylül'ün ayı, Ağustos'un TAMAMIYLA değil Ağustos'un ilk 16 günüyle kıyaslanır. Aksi hâlde her ayın ilk yarısı 'düşüş' gösterir ve panel sürekli yanlış alarm verir.
- HESAPLANAMAYAN ALAN BOŞ + SEBEP; sıfır YAZILMAZ. (A13'teki LISTE_YOK/FIYAT_YOK deseni.) Kur yoksa net varlık '—' olur, 0 TL olmaz.
- PANELİN BİR BİLEŞENİ PATLARSA PANEL AYAKTA KALIR: her bölüm ayrı try/catch, hata sebebe dönüşür. Panel.razor:248'deki mevcut yaklaşımın (uyum verisi olmadan da çalışır) genelleştirilmesi.
- OPERASYON EKRANI (/) KİRLETİLMEZ: kâr, net varlık, açık pozisyon ve alacak toplamı ana panele KONULMAZ; tezgâhtar o ekranı açar.
- TEK İSTEK: ekran 8 ayrı uç çağırmaz. Gerekçe A2: Islemler.razor'un ilk yüklemede SignalR el sıkışmasını beklemesi satış ekranını 'Yükleniyor'da bırakmıştı; panelde de yükleme yolu tek ve kısa tutulur.
- UYARI LİSTESİ EYLEME BAĞLIDIR: her uyarı ilgili ekrana bağlantı taşır (uyum → /uyum, kapanmamış gün → /kasalar, geciken alacak → /raporlar/vade-takibi, ölü stok → /raporlar/stok-analizi). Bağlantısız uyarı gürültüdür.
- SALT OKUR: panel hiçbir şey yazmaz; kişisel veri listelemez (yalnız toplamlar) — bu yüzden okuma izi gerekmez, gerekçesi koda yazılır.

## Testler
- `dotnet/Kuyumcu.Tests/PatronPaneliTestleri.cs` [DB]
  - Panelin kâr rakamı KarZararServisi.Donem ile BİREBİR aynı
  - Panelin net varlık rakamı PozisyonServisi ile BİREBİR aynı
  - Panelin geciken alacak rakamı /raporlar/vade-takibi ile BİREBİR aynı
  - Ay karşılaştırması geçen ayın AYNI GÜN SAYISINI alır (ayın tamamını değil)
  - Kur yoksa netVarlikTl null, panel yine döner ve sebepler KUR_YOK içerir
  - Bir bileşen hata verse bile diğer bileşenler dolu döner
  - Kapanmamış gün varsa uyarı listesinde çıkar ve rotası /kasalar'dır
  - Uyum süresi geçmiş kayıt varsa uyarı listesinde çıkar
  - Yabancı kiracının verisi panele sızmaz
- `dotnet/Kuyumcu.Tests/PatronPaneliYetkiTestleri.cs` [HTTP]
  - MUHASEBE jetonuyla /raporlar/patron-paneli → 403
  - TEZGAHTAR → 403
  - MUDUR → 200
  - SAHIP → 200
- `dotnet/Kuyumcu.Tests/PatronPaneliEkranTestleri.cs` [bUnit]
  - Panel TEK istek atar (sahte API çağrı sayısı 1)
  - Hesaplanamayan kart '—' ve sebep rozeti gösterir
  - Uyarı satırı tıklanınca doğru rotaya gider
  - Açık pozisyon negatifken kırmızı
  - TEZGAHTAR oturumunda KenarCubugu'nda /patron bağlantısı ÇİZİLMEZ
  - Panel.razor'da patron bağlantısı yalnız SAHIP/MUDUR'da çıkar

## Mutasyon kanıtları
- M138 — patron panelinde kâr kendi sorgusuyla hesaplanırsa (KarZararServisi yerine) → Panel_kari_KarZararServisi_ile_BIREBIR KIRMIZI
- M139 — ay karşılaştırması geçen ayın tamamını alırsa → Ay_karsilastirmasi_ayni_gun_sayisini_alir KIRMIZI
- M140 — hesaplanamayan alan 0 yazılırsa → Kur_yoksa_netVarlik_null_sifir_degil KIRMIZI
- M141 — bir bileşenin hatası tüm paneli düşürürse → Bir_bilesen_patlarsa_panel_ayakta_kalir KIRMIZI
- M142 — RolGerekli'den MUHASEBE kısıtı kalkarsa (ya da eklenirse) → Patron_paneli_MUHASEBEye_kapalidir KIRMIZI (HTTP testi gerekir)

## Canlı doğrulama
- Paket 34/35/37 canlı doğrulamasının verisi duran kiracıda çalış
- GET /raporlar/patron-paneli → her rakamı kendi kaynağıyla tek tek karşılaştır (gunluk, kar-zarar, pozisyon, vade-takibi) — dördü de birebir
- Dünün verisini ekle, bugün/dün değişim yüzdesi elle doğrula (perl)
- Kasa gününü kapatmadan bırak → uyarı listesinde 'kapanmamış gün' çıkıyor ve /kasalar'a bağlanıyor
- MUHASEBE kullanıcısı aç, jetonuyla çağır → 403
- TEZGAHTAR oturumuyla ekranda menüde /patron YOK; doğrudan adrese gidilince sunucu 403 veriyor
- Kur kaydını silmeden ikinci bir taze kiracıda çağır → net varlık '—' + KUR_YOK, panel yine çiziliyor
- API'yi durdur, kiracıları sil

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Panel.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs`
- `DOKUNULMAZ: Entities.cs, KuyumcuDbContext.cs, denetim.sh, RaporModulu.cs, KarZararServisi.cs, PozisyonServisi.cs (hepsi OKUNUR, panel onları çağırır)`

## Mevzuat dayanağı
- — — **VARSAYIM** — Yönetim panelidir; mevzuat dayanağı ARANMADI ve İDDİA EDİLMİYOR. Panelin gösterdiği uyum uyarıları (MASAK süresi, gider pusulası süresi) mevcut ve daha önce doğrulanmış kurallardan gelir; bu pakette YENİ mevzuat kuralı koda girmez.

## Riskler
- Panel 'tek uç' olduğu için altındaki 4 servisin toplam süresine bağlıdır; büyük kiracıda yavaşlayabilir → her bileşen zaten sunucu toplaması (ortak karar 4); paket sonunda p95 ölçülmeli.
- Kâr ve net varlığın aynı ekranda görünmesi rol sızıntısı riskini büyütür → sunucu tarafı RolGerekli TAM eşleşme + HTTP testi (M142).
- Uyarı listesi şişerse kimse bakmaz → en fazla 5 uyarı, şiddete göre sıralı, gerisi 've N tane daha'.
- MUHASEBE'nin dışlanması tutarsız bulunabilir (kâr-zararı zaten görüyor) → acik_sorular'da oylamaya bırakıldı; karar değişirse tek nitelik satırı ve M142 testi güncellenir.

## Açık sorular
- Patron paneli MUHASEBE'ye kapalı mı kalsın? Kapalı tutmanın gerekçesi net varlık + açık pozisyonun sahip seviyesi olması; açmanın gerekçesi MUHASEBE'nin kâr-zararı ve stok maliyetini zaten görmesi. Oylama karar versin; tasarım tek nitelik satırıyla değişebilir.
- Panel varsayılan olarak hangi şubeyi gösterir — kiracı geneli mi, kullanıcının şubesi mi? Çok şubeli kuyumcuda sahip geneli, müdür kendi şubesini bekler.
- Uyarı eşikleri (ölü stok kaç gün, geciken alacak kaç TL üstü uyarı) yapılandırmadan mı okunsun? ADR-0005 mevzuat eşikleri içindir; bunlar mevzuat değil işletme tercihidir — ayrı bir ayar başlığı mı açılsın?
- Panel otomatik tazelensin mi (ör. 60 sn)? Tazeleme her seferinde 4 servis turu demek; canlı kur bandı zaten ayrı kanaldan geliyor.

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Paketin omurgası doğru: kendi hesabını yapmaması, operasyon ekranını kirletmemesi, hesaplanamayanı sıfır yazmaması ve her uyarının bir rotaya bağlı olması. Yeni tablo, yeni hesap ve yeni mevzuat kuralı yok — bu, paketi en düşük mimari riskli kalemlerden biri yapıyor. En sona alınması doğru; diğer üçünün uçları hazır olmadan yazılırsa geçici hesap doğar.
  - Koşullar: PatronPaneliController AYRI SINIF olacak ve [RolGerekli("SAHIP","MUDUR")] + [Yetki.ModulYetkisi("rapor")] AÇIKÇA taşıyacak. RaporController'a [HttpGet] eklenirse sınıf niteliği MUHASEBE'yi içeri alır ve M142 testi ilk günden yanlış yeşil verir. · MUHASEBE KARARI (açık soru kapatılıyor): patron paneli MUHASEBE'ye KAPALI kalsın. Gerekçe: panel kâr + net varlık + açık pozisyon + alacağı TEK EKRANDA topluyor; parçaların ayrı ayrı görünür olması, birleşiminin de görünmesi gerektiği anlamına gelmez. Karar tek nitelik satırıyla geri alınabilir ve M142 onu korur. · 'YENİ HESAP YAPMAZ' kuralı bir MUTASYONLA değil, İKİ mutasyonla korunsun: (a) M138 kâr için, (b) net varlık için aynı testin eşi. Tek kalem korunursa diğerleri sessizce kendi sorgusunu yazar. · Panel.razor'a YALNIZ bağlantı kartı eklenecek; mevcut kartlar/grafik/uyum uyarıları/kur seçici DEĞİŞMEYECEK ve bunu doğrulayan bir bUnit regresyon testi yazılacak. · önkoşul [34, 35, 37] doğru; ama 34 Faz A + 37 yeterlidir, 34 Faz B beklenmesin. · p95 ÖLÇÜMÜ paket kabul ölçütü olsun: panel tek uçtan 4 servisi çağırıyor; yük testi (yuk-testi/) koşulmadan 'bitti' denmesin. · UYARI EŞİKLERİ (açık soru kapatılıyor): ölü stok günü ve geciken alacak eşiği MEVZUAT DEĞİLDİR; ADR-0005'in 'Masak'/'Tevsik' bölümlerine KONULMAYACAK, ayrı bir "Isletme" yapılandırma başlığı altında duracak. Mevzuat eşikleriyle işletme tercihlerini aynı bölüme koymak ADR-0005'in anlamını aşındırır.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: EVET** — Kendi hesabı yok, salt okur, yalnız toplamlar (kişisel veri yok), rol TAM eşleşme ile SAHIP/MUDUR. Kâr + net varlığın tek ekranda toplanması rol sızıntısı riskini büyütür ama sunucu kapısı + HTTP testi bunu karşılıyor. MUHASEBE'nin dışlanması güvenlik lensinden DAHA SIKI seçenek; oylamada kalsın, ben sıkı tarafı destekliyorum.
  - Koşullar: K36-1: [RolGerekli('SAHIP','MUDUR')] ATTRIBUTE düzeyinde, servis içinde rol kontrolü yazılmaz; M142 HTTP testi MUHASEBE 403 + TEZGAHTAR 403 + MUDUR 200 + SAHIP 200 (dördü de). · K36-2: Uyarı listesindeki 'rota' alanı sunucudan gelen sabit sözlükten seçilir (kullanıcı girdisi değil); ekran bu rotayı NavigationManager ile açar, dış URL kabul etmez.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Tasarım ilkesi doğru (yeni hesap yok, tek uç, kısmi veri); ama iki mutasyon (M138, M141) mevcut test kurgusuyla yakalanamaz ve rol sözleşmesi açık. Koşullar bunları somut teste bağlar.
  - Koşullar: MUHASEBE kararı (acik_sorular 1) oylanmadan [RolGerekli] ve PatronPaneliYetkiTestleri yazılmaz; M142 iki yönlü ('kalkarsa ya da eklenirse') bir mutasyon olamaz — tek yön seçilir ve test o sözleşmeyi sınar. · M138 ('kâr kendi sorgusuyla hesaplanırsa') 'BİREBİR aynı' testiyle YAKALANMAZ: kendi sorgusu aynı rakamı üretirse test yeşil kalır. Yapısal koruma eklenir: TasarimBelirtecTestleri/denetim.sh deseniyle 'PatronPaneliServisi.cs içinde db.Hareketler / db.Set< / SumAsync 0 isabet' testi; ancak bununla birlikte M138 kanıt sayılır. · 'Bir bileşen hata verse bile diğerleri dolu döner' testi için hata enjeksiyonu TANIMLANIR: servisler somut sınıf (arayüz yok), sahte geçirilemez. Somut yol: kiracının MaliyetYontemi geçersiz yapılır (MaliyetTestleri.cs:284 Gecersiz_yontem_REDDEDILIR — KarZararServisi fırlatır) → panel kâr kartı NULL+sebep, diğer kartlar dolu. Bu yol yazılmadan M141 kanıt sayılmaz. · 'Panel TEK istek atar' bUnit testi: SahteApi kayıt listesi WaitForAssertion ile sayılır; KenarCubugu/Layout dışarıda tutulur (yalnız PatronPaneli bileşeni render edilir), aksi hâlde /kur/guncel gibi kabuk çağrıları sayıyı bozar. · 'Ay karşılaştırması aynı gün sayısı' testi ay sınırında deterministik olmalı: test tarihi parametreyle verilir (Zaman.YerelBugun'a bağlı test 1'inde koşarsa 'geçen ayın 1 günü' ile aynı sonucu verir ve M139'u yakalamaz) — sabit tarih (ör. 16 Eylül) ile yazılır. · Uyarı satırı 'doğru rotaya gider' bUnit: NavigationManager sahte (bUnit BunitNavigationManager) ile Uri assert edilir; tıklama sonrası WaitForAssertion. · Canlı doğrulama zaten ölçülebilir; 'MUHASEBE → 403' adımı rol kararına göre güncellenir.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- PatronPaneliController AYRI sınıf; [RolGerekli("SAHIP","MUDUR")] + [Yetki.ModulYetkisi("rapor")] attribute düzeyinde, servis içinde rol kontrolü yok (Bertan+Mehmet K36-1). RaporController'a eklenirse MUHASEBE içeri sızar.
- AÇIK SORU KAPANDI: panel MUHASEBE'ye KAPALI. Aşkın'ın 'oylanmadan test yazılmaz' şartı bu kararla karşılandı; M142 TEK yönlü sözleşmeyi sınar: MUHASEBE 403 + TEZGAHTAR 403 + MUDUR 200 + SAHIP 200.
- Test rol listesi TEK sabitten okunur (karar değişirse tek satır değişsin) — Aşkın; Bertan'ın 'tek nitelik satırı' şartıyla aynı yöne çözüldü.
- 'YENİ HESAP YAPMAZ' İKİ mutasyonla korunur (M138 kâr + net varlık eşi); ayrıca yapısal koruma: denetim.sh/TasarimBelirtecTestleri deseniyle PatronPaneliServisi içinde kendi sorgusunu yazmak yasaklanır (M138 birebir testle yakalanmaz — Aşkın).
- Panel.razor'a YALNIZ bağlantı kartı eklenir; mevcut kartlar/grafik/uyum uyarıları/kur seçici değişmez + bUnit regresyon testi.
- Uyarı 'rota' alanı sunucudaki sabit sözlükten seçilir, dış URL kabul edilmez; NavigationManager ile açılır, bUnit'te BunitNavigationManager Uri assert (Mehmet K36-2 + Aşkın).
- En fazla 5 uyarı, şiddete göre sıralı, gerisi 've N tane daha'.
- p95 ölçümü kabul ölçütüdür: tek uçtan 4 servis; yuk-testi/ koşulmadan 'bitti' denmez.
- Hata enjeksiyonu somut yolla tanımlanır (servisler arayüzsüz): kiracının MaliyetYontemi geçersiz yapılır (MaliyetTestleri.cs:284 deseni) — 'bir bileşen düşse diğerleri dolu döner'.
- 'Panel TEK istek atar' bUnit'inde yalnız PatronPaneli bileşeni render edilir (KenarCubugu/Layout hariç); ay karşılaştırma testi sabit tarih parametresiyle deterministik. Önkoşul: 34 Faz A + 37 yeterli, 34 Faz B beklenmez.
