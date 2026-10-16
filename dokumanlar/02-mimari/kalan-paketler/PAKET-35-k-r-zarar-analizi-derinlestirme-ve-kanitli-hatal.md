# Paket 35 — Kâr-zarar analizi — derinleştirme ve kanıtlı hataların kapatılması

> Katman: Katman 5 — Raporlama (34, 35, 36, 37) · Tahmin: Faz A 2–3 gün · Faz B 2 gün (toplam 4–5; PARITE 3–4, fark üç kanıtlı hatanın kapatılmasından) gün · Önkoşullar: 34 · Kaynak: katman mimarı K5 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 6

## Hedef
Kâr-zarar çekirdeği var ama üç kanıtlı ölçüm hatası taşıyor ve kırılım sunmuyor. Paket önce hataları kapatır (iade görünmüyor, takas farkı hasılat sayılıyor, işçilik geliri hiç yok), sonra dönem karşılaştırması ve kategori/şube/kullanıcı kırılımını ekler.

## Kodda bugünkü durum
KarZararServisi.cs:35 Donem (TL+HAS iki bacak, güvenilirlik :117/:265), :128 UrunBazli, :205 StokDegerlemesi (konsinye hariç :209-211). Ekran KarZarar.razor (243 satır). KANITLI KUSUR 1 — İADE GÖRÜNMÜYOR: Donem hasılat süzgeci :47 'Tur == SATIS || Tur == TAKAS'; SATIS_IADE (SatisModulu.cs:1104) yok. Maliyet tarafında da iade görünmez: ters tüketim satırı CikisHareketId = iadeHareketId olarak yazılıyor (MaliyetMotoru.cs:233) ve :82'deki satisIdler kümesinde olmadığı için elenir. Sonuç: aynı dönemde satılıp tamamı iade edilen mal, kâr-zararda HÂLÂ TAM KÂR olarak durur. KANITLI KUSUR 2 — TAKAS: :47 takası hasılata katıyor ama takas satırının TlTutari fark TL'dir ve NEGATİF olabilir (SONRAKI-OTURUM-PROMPT A5: 'Takas'ta TlTutari = farkTl NEGATİF olabilir'); el değiştiren brüt değer değil. KANITLI KUSUR 3 — İŞÇİLİK: ISCILIK hareketi (SatisModulu.cs:1767) hasılata HİÇ girmiyor; Sozlukler.cs SozlukRapor'da IscilikGeliri anahtarı var ama onu besleyen uç yok. EKSİK: UrunBazli (:191-198) ve StokDegerlemesi (:228-250) güvenilirlik alanı DÖNDÜRMEZ; yalnız Donem döndürür.

## Kapsam
**İçinde:**
- Faz A: iade, takas ve işçiliğin kâr-zarara doğru yansıtılması + güvenilirliğin üç uca da yayılması
- Faz A: dönem karşılaştırma (önceki dönem / geçen yılın aynı dönemi)
- Faz B: kategori, şube ve kaydeden kullanıcı bazında kâr kırılımı
- Faz B: kâr köprüsü (hasılat → iade → maliyet → işçilik → kâr) tek tabloda
- Ekranda kırılım sekmeleri ve karşılaştırma sütunu

**Dışında:**
- Maliyet yöntemi değişikliği (FIFO/LIFO/ORTALAMA motoru hazır, dokunulmaz)
- Genel gider/kira/personel gideri dağıtımı (kod tabanında gider muhasebesi YOK; uydurulmaz)
- Vergi sonrası kâr / dönem sonu envanter değerlemesi beyanı
- Konsinye kârının ayrı raporlanması (satır var, ayrı rapor ayrı iş)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| hareketler | DEĞİŞİKLİK YOK | - | (tenant_id, urun_id, tarih) paket 34'te ekleniyor; kâr kırılımı onu kullanır | - |
| stok_parti_tuketimleri | DEĞİŞİKLİK YOK | - | (tenant_id, tarih) ve cikis_hareket_id zaten var (KuyumcuDbContext.cs:180-182) | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/raporlar/kar-zarar?baslangic&bitis&subeId&karsilastir=ONCEKI_DONEM|GECEN_YIL` | SAHIP,MUDUR,MUHASEBE | DEĞİŞTİR: mevcut uç. Yanıta eklenir — iadeTl/iadeHas, iadeMaliyetTl/Has, takasFarkTl (hasılattan AYRILIR), iscilikTl, karKoprusu[], onceki:{...} ve degisimYuzde. Mevcut alanlar KORUNUR (KarZarar.razor:160-164 kayıtları kırılmasın). | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/kar-zarar/urun?baslangic&bitis&subeId&limit` | SAHIP,MUDUR,MUHASEBE | DEĞİŞTİR: güvenilirlik + iade düşümü + hasKar sütunu eklenir. | ModulYetkisi(rapor) · limit [Range(1,500)] (bugün 50 varsayılan, KarZararServisi.cs:300) |
| GET | `/api/v1/raporlar/kar-zarar/kirilim?baslangic&bitis&eksen=KATEGORI|SUBE|KULLANICI|REYON&limit` | SAHIP,MUDUR,MUHASEBE | YENİ (Faz B): seçilen eksende hasılat/iade/maliyet/kâr TL + kâr HAS + marj + güvenilirlik. | ModulYetkisi(rapor) · eksen kapalı liste, geçersiz → 400 |
| GET | `/api/v1/raporlar/stok-degerlemesi?subeId` | SAHIP,MUDUR,MUHASEBE | DEĞİŞTİR: güvenilirlik alanı eklenir; konsinye dışlama gerekçesi yanıtta kalır. | ModulYetkisi(rapor) |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` — Donem: hasılat süzgecine SATIS ve SATIS_IADE alınır, TAKAS ayrı ölçülür (takasFarkTl olarak hasılattan AYRI raporlanır, toplama katılmaz — brüt değer defterde yok, uydurulmaz), ISCILIK ayrı kalem (iscilikTl) olarak kâra eklenir. Maliyet: satisIdler kümesine SATIS_IADE hareket kimlikleri de alınır; ters tüketim satırları (Adet<0, MaliyetMotoru.cs:236) NEGATİF maliyet olarak toplanır → iade edilen malın maliyeti geri çıkar. UrunBazli ve StokDegerlemesi'ne RaporGuvenilirligi. Yeni: Kirilim(eksen). Yeni: KarKoprusu satır üretimi. Karşılaştırma: aynı Donem hesabı önceki aralık için ikinci kez koşar (tek ek sorgu seti).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/KarZarar.razor` — Kâr köprüsü tablosu (hasılat → iade → net hasılat → maliyet → brüt kâr → işçilik → dönem kârı), karşılaştırma seçici + değişim yüzdesi rozetleri, takas farkı AYRI kart ('takas farkı hasılata katılmaz' açıklamasıyla), Faz B'de eksen sekmeleri (kategori/şube/kullanıcı/reyon). Mevcut TL/HAS bacak kartları ve has kaybı uyarısı (satır 73-79) KORUNUR.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukKarZarar'a ~18 anahtar: Iade, NetHasilat, BrutKar, IscilikGeliri, TakasFarki, TakasFarkiNotu, KarKoprusu, Karsilastir, OncekiDonem, GecenYil, Degisim, Eksen, Kategori, Sube, Kaydeden, Reyon + TR/EN blokları.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs` — kar-zarar/kirilim katalog kaydı.

## Ekranlar
- `/raporlar/kar-zarar` · `dotnet/Kuyumcu.Web/Pages/KarZarar.razor` — DEĞİŞTİR: kâr köprüsü + dönem karşılaştırma + takas farkı ayrı kart + (Faz B) eksen sekmeleri. (menü/yetki: rapor modülü (KenarCubugu.razor:47 mevcut))

## İş kuralları (fail-closed)
- İADE HEM HASILATTAN HEM MALİYETTEN DÜŞER. Tek taraflı düşüm (yalnız hasılat) iade edilen malın maliyetini gider olarak bırakır ve kârı olduğundan DÜŞÜK gösterir; hiç düşmemek bugünkü hâldir ve kârı YÜKSEK gösterir. İkisi birlikte, aynı sorgu turunda.
- İADE MALİYETİ ÇIKTIĞI MALİYETLE GERİ ALINIR: ters tüketim satırının BirimMaliyetTl'si orijinal partininkidir (MaliyetMotoru.cs:237-238). Rapor bu satırı bugünkü maliyetle yeniden değerlemez.
- TAKAS FARKI HASILATA KATILMAZ, AYRI GÖSTERİLİR. Gerekçe kodda yazılı: takas satırı TlTutari = farkTl'dir, el değiştiren brüt değer değildir (A5/A8 notları). Brüt değeri uydurmak yerine kalem ayrı raporlanır ve ne olduğu yazılır.
- İŞÇİLİK GELİRİ AYRI KALEMDİR: malın maliyeti yoktur, hasılatla aynı satıra konursa marj yüzdesi bozulur. Kâr köprüsünde brüt kârdan SONRA gelir.
- MALİYETİ BİLİNMEYEN SIFIR SAYILMAZ (KarZararServisi.cs:23-26 kuralı korunur) ve artık üç ucun ÜÇÜ de güvenilirlik döndürür.
- KARŞILAŞTIRMA DÖNEMİ EŞİT UZUNLUKTA OLUR: 15 günlük dönem 30 günlük ayla kıyaslanmaz; 'geçen yılın aynı dönemi' aynı gün sayısı kadar alınır.
- HAS BACAĞI ASLA GİZLENMEZ: her kırılım satırı TL kârıyla birlikte HAS kârını taşır (KarZararServisi.cs:14-21 gerekçesi); yalnız TL gösteren bir kırılım ekranı bu ürünün var oluş sebebini siler.

## Testler
- `dotnet/Kuyumcu.Tests/MaliyetTestleri.cs` [DB]
  - Aynı dönemde satılıp TAMAMI iade edilen mal → dönem kârı SIFIR (bugün kâr yazıyor — REGRESYON TESTİ)
  - Kısmi iade → hasılat ve maliyet orantılı düşer
  - İade maliyeti ÇIKTIĞI birim maliyetle geri alınır (yeni parti maliyetiyle değil)
  - Takas farkı hasilatTl'ye KATILMAZ, takasFarkTl alanında döner
  - Negatif takas farkı (tediye) hasılatı azaltmaz
  - İşçilik geliri iscilikTl'de, hasilatTl'de DEĞİL
  - Kâr köprüsü satırlarının toplamı donemKariTl'ye eşittir (kuruş sapması yok)
  - UrunBazli güvenilirlik döndürür ve maliyetsiz satışta TAM değildir
  - StokDegerlemesi güvenilirlik döndürür
  - Kırılım: kategori ekseninde alt kategoriler üst kategoriye toplanır
  - Kırılım: şube ekseninde toplam = subeId'siz dönem kârı
  - Kırılım: geçersiz eksen → 400
  - Karşılaştırma dönemi eşit gün sayısındadır
  - Konsinye ürün stok değerlemesine katılmaz (mevcut kural regresyonu)
- `dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs` [bUnit]
  - Kâr köprüsü tablosu iade satırını gösterir
  - Takas farkı ayrı kartta ve açıklama metniyle çıkar
  - Karşılaştırma seçilince sunucuya karsilastir parametresi gider
  - Eksen sekmesi değişince /kar-zarar/kirilim?eksen=... çağrılır
  - Her kırılım satırında HAS kâr sütunu vardır

## Mutasyon kanıtları
- M123 — iade hasılattan düşülmezse → Tamami_iade_edilen_mal_kar_birakmaz KIRMIZI
- M124 — iade maliyeti düşülmezse (ters tüketim elenirse) → Kismi_iade_maliyeti_orantili_duser KIRMIZI
- M125 — iade maliyeti bugünkü maliyetle geri alınırsa → Iade_CIKTIGI_maliyetle_geri_alinir KIRMIZI
- M126 — takas farkı hasılata katılırsa → Takas_farki_hasilata_KATILMAZ KIRMIZI
- M127 — işçilik hasılata katılırsa → Iscilik_geliri_ayri_kalemdir KIRMIZI
- M128 — UrunBazli'dan güvenilirlik kaldırılırsa → UrunBazli_guvenilirlik_dondurur KIRMIZI
- M129 — karşılaştırma dönemi takvim ayı olarak alınırsa (eşit gün sayısı yerine) → Karsilastirma_donemi_esit_gun_sayisidir KIRMIZI
- M130 — kırılım satırından HAS kâr sütunu kaldırılırsa → Kirilim_satirinda_HAS_kar_vardir KIRMIZI

## Canlı doğrulama
- Yeni kiracı; 10 adet ürün alışı (maliyet bilinir) + 4 adet satış + 1 adet iade + 1 takas (fark pozitif) + 1 işçilik
- GET /raporlar/kar-zarar → hasilatTl satış toplamı EKSİ iade; maliyetTl 3 adedin maliyeti; takasFarkTl ayrı; iscilikTl ayrı — dördü de SQL ile birebir
- Aynı dönemde satılıp tamamı iade edilen ikinci ürün → kâra katkısı 0 (SQL ile stok_parti_tuketimleri net adedi 0 doğrulanır)
- karsilastir=ONCEKI_DONEM → önceki dönem alanları ve degisimYuzde geliyor; dönem gün sayısı eşit
- GET /raporlar/kar-zarar/kirilim?eksen=SUBE → iki şube toplamı subeId'siz dönem kârına eşit
- GET /raporlar/kar-zarar/urun → güvenilirlik alanı var; maliyeti bilinmeyen ürün eklenince ORTA/DUSUK'e düşüyor
- Ekranda kâr köprüsü satır toplamı ile dönem kârı kartı aynı rakam
- Kiracıyı sil

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/KarZarar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/MaliyetTestleri.cs`
- `DOKUNULMAZ: Kuyumcu.Api/Modules/Maliyet/MaliyetMotoru.cs (parti mantığı doğru; rapor onu OKUR, değiştirmez)`
- `DOKUNULMAZ: denetim.sh, KuyumcuDbContext.cs, Entities.cs, ModulKayit.cs (yeni servis yok — KarZararServisi zaten kayıtlı, ModulKayit.cs:26)`

## Mevzuat dayanağı
- VUK md.274/275 (emtia maliyet bedeli / maliyet bedelinin unsurları) — **DOGRULANMALI** — Stok değerleme yöntemi (FIFO/LIFO/ortalama) zaten kiracı ayarında (Tenant.MaliyetYontemi, Entities.cs). Bu pakette hüküm KODA YAZILMAZ; rapor yalnız seçilen yöntemin sonucunu gösterir ve hangi yöntemin kullanıldığını yanıtta beyan eder.
- VUK md.186 (envanter) — **DOGRULANMALI** — Stok değerlemesi ekranının resmî envanter yerine geçtiği İDDİA EDİLMEZ; ekranda 'yönetim raporu' ibaresi kalır.

## Riskler
- İade düzeltmesi geçmiş dönem raporlarının rakamını DEĞİŞTİRİR (doğru yönde, ama değişir). Kurucuya/müşteriye 'eski rapor çıktısıyla yeni rapor tutmayacak' diye açıkça söylenmeli; sürüm notu şart.
- Takas farkının hasılattan çıkarılması ciroyu düşürür; 'ciro azaldı' diye algılanabilir → ekranda ayrı kart + açıklama metni zorunlu.
- Kırılım ekseni KULLANICI olduğunda paket 34'ün 'kaydeden ≠ satan' uyarısı burada da geçerli; aynı metin kuralı uygulanır.
- Karşılaştırma ikinci bir sorgu turu demek; büyük dönemde süre iki katına çıkar → karşılaştırma İSTEĞE BAĞLI parametre, varsayılan kapalı.

## Açık sorular
- Takasın brüt değeri deftere yazılmadığı için (yalnız fark TL) takas cirosu ÖLÇÜLEMİYOR. Seçenekler: (a) bugünkü gibi yalnız fark, açıkça beyan; (b) takas hareketine brüt değer kolonu eklemek (yeni kolon = ortak karar 1'in istisnası, ayrı paket); (c) takası ciro dışı saymak. Öneri (a), karar oylamaya.
- İşçilik gelirinin maliyeti yok kabul ediliyor (malzeme/ustalık gideri modellenmemiş, imalat Katman 1 kapsam dışı). Doğrulanması gereken varsayım değil, kapsam sınırı — ekranda yazılmalı mı?
- Geçmiş dönem raporlarının değişeceği müşteriye nasıl bildirilecek (sürüm notu metni Köprü'nün işi).

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Üç kusurun ikisini kodda BİREBİR doğruladım (iade süzgeçte yok, güvenilirlik yalnız tek uçta). İşçilik kaleminin hasılata hiç girmemesi de aynı süzgeçten okunuyor. Düzeltme yönü doğru: iadenin hem hasılattan hem maliyetten düşmesi, takas farkının ayrı kalem olması ve has bacağının her kırılımda durması bu ürünün var oluş sebebiyle tutarlı. Üçüncü kusurun (ters tüketim) dayanağı okunmadığı için koşullu.
  - Koşullar: OKUNMADI — ZORUNLU ÖLÇÜM: 'ters tüketim satırı CikisHareketId = iadeHareketId olarak yazılıyor (MaliyetMotoru.cs:233) ve Adet<0 (satır 236)' iddiası bu oturumda AÇILMADI. Uygulayıcı MaliyetMotoru.cs:185-245'i AÇSIN, ters satırın işaretini ve CikisHareketId'sini ÖLÇSÜN, sonra kod yazsın. Bu iddia yanlışsa iade maliyeti düzeltmesi TERS YÖNE çalışır ve kârı ikinci kez bozar. · REGRESYON ÖNCE: takasın hasılattan çıkarılması ve iadenin düşülmesi MEVCUT rakamları değiştirir. Değişimden ÖNCE bugünkü çıktıyı donduran bir karakterizasyon testi yazılsın; sonra kural değiştirilsin. Aksi hâlde 'düzeltme mi bozma mı' ayırt edilemez. · YANIT ŞEKLİ KORUNACAK: KarZarar.razor:160-176 mevcut alanlara bağlı; yeni alanlar EKLENİR, var olanlar yeniden adlandırılmaz/kaldırılmaz. Şekil regresyon testi zorunlu. · Guvenilirlik(...) RaporOrtak'a taşınırken İMZASI ve DÖNEN KOD KÜMESİ (VERI_YOK/TAM/YUKSEK/ORTA/DUSUK) birebir korunacak; eşikler (0.9 / 0.5) değiştirilmeyecek. Taşıma bir davranış değişikliği değildir. · önkoşul [34] doğru ve KORUNACAK (RaporGuvenilirligi ortak sınıfı 34'te doğuyor). Ama 34 Faz A yeterlidir; 35, 34 Faz B'yi BEKLEMEZ. · Takas farkının hasılattan çıkarılması müşteride 'ciro düştü' algısı yaratır: ekranda ayrı kart + açıklama metni ZORUNLU, opsiyonel değil. Bu bir tasarım tercihi değil, Anayasa md.1'in ekran karşılığıdır. · Faz A ve Faz B ayrı sevk edilecek; Faz A (üç kanıtlı hata + güvenilirliğin yayılması) tek başına değerlidir ve kırılım olmadan da sevk edilebilir.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: EVET** — Şema değişikliği yok, salt okur, mevcut KarZararController rotası ve rol kapısı korunuyor, HAS bacağı gizlenmiyor, kur uydurulmuyor. Kanıtlı üç ölçüm hatası (iade/takas/işçilik) doğru yönde kapanıyor; güvenlik yüzeyi genişlemiyor.
  - Koşullar: K35-1: Kirilim ucu 'eksen' parametresi kapalı liste (KATEGORI|SUBE|KULLANICI|REYON) — string doğrudan LINQ/SQL'e geçmez; geçersiz → 400 testi zaten yazılı, korunur. · K35-2: KULLANICI ekseni yalnız toplam döner (kullanıcı adı + rakam), e-posta/telefon dönmez.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Üç kusur kod gerçeğiyle doğrulandı (KarZararServisi.cs:47 'SATIS || TAKAS', MaliyetMotoru.cs:233-236 ters tüketim iade hareketine bağlı, satisIdler kümesinde yok). Mutasyon listesi test adlarıyla birebir eşleşiyor. Koşullar testlerin gerçekten ayırt edici olmasını sağlayan fikstür ayrıntılarıdır; tasarım değişikliği gerektirmez.
  - Koşullar: 'Tamami_iade_edilen_mal_kar_birakmaz' testi: iade tutarı satış tutarına EŞİT kurgulanır ve test hem hasilatTl−iadeTl=0 hem maliyetTl+iadeMaliyetTl=0 (iki bacak) assert eder; yalnız 'karTl==0' assert'i, hasılat ve maliyetin aynı miktarda yanlış olduğu hâli de yeşil gösterir. · 'Kırılım: geçersiz eksen → 400' DB testi için KarZararServisi.Kirilim içinde eksen kapalı liste kontrolü ELLE yazılır (BadRequestException); controller [RegularExpression] tek başına DB testinde görünmez. · M129 'Karsilastirma_donemi_esit_gun_sayisidir': yanıttaki onceki.baslangic/onceki.bitis alanları assert edilir (15 günlük dönem için 15 gün), sadece degisimYuzde varlığı yetmez. · M125 'Iade_CIKTIGI_maliyetle_geri_alinir': fikstürde iadeden ÖNCE farklı birim maliyetli ikinci parti girilir (MaliyetTestleri.cs:32-55 FIFO/LIFO deseni) ki 'bugünkü maliyetle yeniden değerleme' mutasyonu farklı rakam üretsin — aksi hâlde iki maliyet eşitken test yeşil kalır. · 14 yeni senaryo mevcut MaliyetTestleri.cs'e (zaten ~750 satır, 30 test) eklenmez; yeni dosya KarZararIadeTestleri.cs ([Collection("veritabani")]) — dosya_ayak_izi güncellenir. · KarZararEkranTestleri (bUnit): mevcut KarZarar.razor kayıtları (satır 160-176) için 'eski alanlar hâlâ var' regresyon assert'i; sekme/karşılaştırma testlerinde WaitForAssertion. · Canlı doğrulama zaten ölçülebilir (SQL: hareketler SUM by tur, stok_parti_tuketimleri net adet); 'geçmiş dönem rakamı değişir' riski için canlı adıma 'paket öncesi kar-zarar yanıtı kaydedilir, sonrası ile fark iade tutarına eşit' ölçümü eklenir.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- ZORUNLU ÖLÇÜM (Bertan): 'ters tüketim satırı CikisHareketId=iadeHareketId (MaliyetMotoru.cs:233) ve Adet<0 (satır 236)' iddiası bu oturumda AÇILMADI. Uygulayıcı MaliyetMotoru.cs:185-245'i açıp işareti ve CikisHareketId'yi ÖLÇSÜN, sonra kod yazsın; iddia yanlışsa düzeltme ters yöne çalışır ve kârı ikinci kez bozar.
- REGRESYON ÖNCE: takasın hasılattan çıkarılması ve iadenin düşülmesi mevcut rakamları değiştirir. Değişiklikten ÖNCE bugünkü çıktıyı donduran karakterizasyon testi yazılır; aksi hâlde 'düzeltme mi bozma mı' ayırt edilemez.
- YANIT ŞEKLİ KORUNUR: KarZarar.razor:160-176 mevcut alanlara bağlı — yeni alanlar EKLENİR, var olanlar yeniden adlandırılmaz/kaldırılmaz; şekil regresyon testi ve bUnit'te 'eski alanlar hâlâ var' assert'i zorunlu.
- Guvenilirlik(...) RaporOrtak'a taşınırken imzası ve dönen kod kümesi (VERI_YOK/TAM/YUKSEK/ORTA/DUSUK) birebir korunur, eşikler (0.9/0.5) değiştirilmez — taşıma davranış değişikliği değildir.
- Önkoşul [34] korunur (RaporGuvenilirligi 34'te doğuyor) ama 34 Faz A yeterlidir; 35, 34 Faz B'yi BEKLEMEZ.
- Kırılım ekseni KAPALI LİSTE (KATEGORI|SUBE|KULLANICI|REYON), kontrol KarZararServisi.Kirilim İÇİNDE elle (BadRequestException) — controller [RegularExpression] DB testinde görünmez; geçersiz → 400 testi korunur. KULLANICI ekseni yalnız ad + rakam döner, e-posta/telefon dönmez.
- 'Tamami_iade_edilen_mal_kar_birakmaz' testi İKİ BACAĞI ayrı assert eder (hasilatTl−iadeTl=0 VE maliyetTl+iadeMaliyetTl=0); yalnız karTl==0 assert'i, iki tarafın aynı miktarda yanlış olduğu hâli yeşil gösterir.
- M125 fikstürü: iadeden ÖNCE farklı birim maliyetli ikinci parti girilir (MaliyetTestleri.cs:32-55 FIFO deseni) ki 'bugünkü maliyetle yeniden değerleme' mutasyonu farklı rakam üretsin. M129: onceki.baslangic/onceki.bitis eşit gün sayısı assert edilir, degisimYuzde varlığı yetmez.
- 14 yeni senaryo MaliyetTestleri.cs'e eklenmez; yeni dosya KarZararIadeTestleri.cs ([Collection("veritabani")]) açılır ve dosya ayak izi güncellenir. Karşılaştırma İSTEĞE BAĞLI parametre, varsayılan kapalı.
- Takas farkının hasılattan çıkarılması için ekranda AYRI KART + açıklama metni ZORUNLU (Anayasa md.1'in ekran karşılığı, tasarım tercihi değil). Canlı adıma 'paket öncesi/sonrası kâr-zarar farkı = iade tutarı' ölçümü eklenir; geçmiş rapor değişimi sürüm notuyla duyurulur. Takas brüt cirosu ölçülemez → seçenek (a): yalnız fark, açıkça beyan. Faz A/B ayrı sevk.
