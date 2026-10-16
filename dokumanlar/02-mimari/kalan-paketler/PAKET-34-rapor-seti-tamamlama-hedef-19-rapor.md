# Paket 34 — Rapor seti tamamlama (hedef 19 rapor)

> Katman: Katman 5 — Raporlama (34, 35, 36, 37) · Tahmin: Faz A 4–5 gün · Faz B 4–5 gün (toplam 8–10; PARITE tahmini 6–9 ile uyumlu, TB-19 ve okuma izi borçları dahil edildiği için üst sınır bir gün yukarı) gün · Önkoşullar: 33 · Kaynak: katman mimarı K5 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 6

## Hedef
Perakende kuyumcunun günlük/aylık kapanışını, kimin ne sattığını, hangi araçla tahsil edildiğini ve hangi malın raf beklediğini tek yerden görebilmesi. Bugün 9 rapor ucu var ve dördü aynı sayfada dağınık duruyor; işlem defteri gibi en temel döküm HİÇ YOK. Paket, ortak rapor altyapısını kurar ve 10 yeni raporla toplamı 19'a çıkarır.

## Kodda bugünkü durum
RaporModulu.cs:518 gunluk, :525 aylik, :533 cari-yaslandirma, :538 vade-takibi, :542 pos-fatura-mutabakati, :549 stok-degeri; KarZararServisi.cs:285 kar-zarar, :294 kar-zarar/urun, :304 stok-degerlemesi → TOPLAM 9 uç. Döküm uçları: CariModulu.cs:606 /cari/{id}/ekstre, KasaModulu.cs:353 /kasalar/{id}/hareketler, BankaModulu.cs:353 /bankalar/{id}/hareketler. GENEL İŞLEM DEFTERİ YOK: SatisController'da (SatisModulu.cs:1713-1769) tek GET 'iade-adaylari' (:1748). Ekran: Raporlar.razor (340 satır) 3 uç + mutabakat; VadeTakibi.razor; KarZarar.razor. Sabitler.HareketTurleri (Entities.cs:24-26) 11 tür sayıyor ama kodun yazdığı SATIS_IADE (SatisModulu.cs:1104), ALIS_IADE (:1213) ve paket 33'ün CARI_DUZELTME'si listede YOK (liste hiçbir yerde zorlanmıyor, A17 notu).

## Kapsam
**İçinde:**
- Ortak altyapı: RaporAraligi, RaporFiltresi, RaporGuvenilirligi, SayfaliSonuc
- Rapor kataloğu ucu (19 raporun TEK sayım kaynağı)
- Faz A yeni raporlar: işlem defteri, kasa defteri, ödeme aracı kırılımı, personel (kaydeden kullanıcı), şube karşılaştırma
- Faz B yeni raporlar: kategori/reyon satış, stok devir hızı + ölü stok, hurda/pusula özeti, KDV-fatura özeti, iade özeti
- Faz B: TB-19 borcunun kapatılması (CariYaslandirma + StokDegeri sunucu toplamasına)
- Faz B: kişisel veri dönen raporlara okuma izi
- Sabitler.HareketTurleri'ne SATIS_IADE, ALIS_IADE, CARI_DUZELTME eklenmesi
- Rapor indeksleri (tek migration)
- Yeni rapor ekranları + yazdırma (@media print)

**Dışında:**
- CSV/Excel indirme (ADR-0014; /veri-disa-aktarim yolu)
- Sunucuda PDF üretimi (ADR-0007)
- Zamanlanmış/otomatik rapor e-postası (paket 6 çatısı)
- Kullanıcı tanımlı rapor tasarımcısı (mühürlü listede yok, backlog)
- Çok dönemli grafik/trend analizi (paket 35 ve 36)
- Tedarikçi vade raporu (A11 açık maddesi, ayrı iş)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| hareketler | YALNIZ İNDEKS | kolon eklenmez | (tenant_id, sube_id, tarih) · (tenant_id, kullanici_id, tarih) · (tenant_id, tur, tarih) · (tenant_id, urun_id, tarih) | 20260916HHMMSS_RaporIndeksleri |
| kasa_hareketleri | YALNIZ İNDEKS | kolon eklenmez | (tenant_id, kasa_id, tarih) — bugün yalnız (tenant_id, tarih) var (KuyumcuDbContext.cs:118) | aynı migration |
| gider_pusulalari / faturalar | DEĞİŞİKLİK YOK | - | (tenant_id, tarih) faturalarda zaten var (KuyumcuDbContext.cs:130); pusulada (tenant_id, teslim_tarihi) var | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/raporlar/katalog` | SAHIP,MUDUR,MUHASEBE | Rapor kataloğu: [{kod, ad, rota, uc, roller, faz}]. 19 raporun TEK sayım kaynağı; ekran menüsü bundan kurulur ve 'kaç rapor var' sorusu koddan cevaplanır. | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/islem-defteri?baslangic&bitis&tur&subeId&cariId&kullaniciId&odemeTuru&limit&ofset` | SAHIP,MUDUR,MUHASEBE | R10 — genel hareket dökümü. Yanıt: {toplamSatir, ofset, limit, ozet:{tlGiris,tlCikis,hasGiris,hasCikis}, satirlar:[{id,tarih,tur,yon,tlTutari,hasGrami,odemeTuru,nakitTutar,veresiyeTutar,fisId,cariAd,urunAd,subeAd,kullaniciAd,faturaNo}]}. TlTutari HAM İŞARETİYLE döner + ayrı yon alanı. | ModulYetkisi(rapor) · limit zorunlu · okuma izi (cariAd taşır) |
| GET | `/api/v1/raporlar/kasa-defteri?baslangic&bitis&kasaId` | SAHIP,MUDUR,MUHASEBE | R11 — kasa bazında devir/giriş/çıkış/kapanış + gün kapanışı farkı. gun_kapanislari (KuyumcuDbContext.cs:125) ile eşleştirilir; kapanmamış gün 'AÇIK' işaretlenir. | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/odeme-araci?baslangic&bitis&subeId` | SAHIP,MUDUR,MUHASEBE | R12 — NAKIT/KART/HAVALE/DOVIZ/ALTIN/VERESIYE kırılımı. Kaynak kasa_hareketleri.odeme_turu (bacak başına ayrı satır, A7 canlı kanıtı) + hareketler.veresiye_tutar. hareketler.odeme_turu KARMA olduğunda tek etiket YANILTIR, kullanılmaz. | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/personel?baslangic&bitis&subeId` | SAHIP,MUDUR,MUHASEBE | R13 — KAYDEDEN kullanıcı bazında işlem sayısı/ciro/ortalama sepet/iade oranı. Başlık ve yanıtta 'kaydeden' geçer, 'satan/tezgâhtar' GEÇMEZ. | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/sube-karsilastirma?baslangic&bitis` | SAHIP,MUDUR,MUHASEBE | R14 — şube bazında satış/alış/hurda/kâr(TL)/stok has. TRANSFER türü ciroya KATILMAZ. | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/kategori-satis?baslangic&bitis&kategoriId&reyonId` | SAHIP,MUDUR,MUHASEBE | R15 (Faz B) — kategori/reyon bazlı satış. Kategori hedefi ALT KATEGORİLERİ kapsar (A13/A14'teki kural). | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/stok-devir?gunEsigi=90&subeId&limit` | SAHIP,MUDUR,MUHASEBE | R16 (Faz B) — devir hızı + ölü stok. Hiç satılmamış ürün sonSatisTarihi=null ve ayrı 'hicSatilmadi' kümesinde; 9999 gün YAZILMAZ. | ModulYetkisi(rapor) · limit zorunlu |
| GET | `/api/v1/raporlar/hurda-ozet?baslangic&bitis` | SAHIP,MUDUR,MUHASEBE | R17 (Faz B) — hurda alım has/TL, satıcı tipi kırılımı, pusula sayısı, stopaj toplamı, belgesiz alım sayısı. | ModulYetkisi(rapor) · okuma izi (GiderPusulasi kişisel veri) |
| GET | `/api/v1/raporlar/kdv-ozet?yil&ay` | SAHIP,MUDUR,MUHASEBE | R18 (Faz B) — faturalar tablosundan KDV özeti: kdvTuru (STANDART/KUYUM) ayrımıyla araToplam, hasDeger, kdvMatrah, kdvTutar, toplam; durum kırılımı (TASLAK/KESILDI/ODENDI/IPTAL). SALT OKURDUR; matrah HESAPLAMAZ (o paket 38'dir). | ModulYetkisi(rapor) |
| GET | `/api/v1/raporlar/iade-ozet?baslangic&bitis&subeId` | SAHIP,MUDUR,MUHASEBE | R19 (Faz B) — satış/alış iade adedi, tutarı, iade oranı, en çok iade edilen ürün. Kaynağı dönem dışında kalan iade AYRI sayılır. | ModulYetkisi(rapor) |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporOrtak.cs` — RaporAraligi.Coz(DateOnly? bas, DateOnly? bit) → (DateTime Bas, DateTime Bit, DateOnly YerelBas, DateOnly YerelBit); bit<bas → BadRequestException; varsayılan bugün (Zaman.YerelBugun). RaporFiltresi record (SubeId, CariId, KullaniciId, KategoriId, ReyonId, Tur, OdemeTuru, Limit, Ofset) + Dogrula(). SayfaliSonuc<T>(ToplamSatir, Ofset, Limit, Satirlar). RaporGuvenilirligi.Hesapla(bilinen, bilinmeyen, ekKusur) → (Kod, Sebep) — KarZararServisi.cs:265'ten taşınır, oradaki çağrı yeni yardımcıya bağlanır.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs` — SatisRaporServisi + SatisRaporController ([Route("api/v1/raporlar")]): IslemDefteri (R10), OdemeAraci (R12), Personel (R13), SubeKarsilastirma (R14), IadeOzet (R19). Hepsi GroupBy/Sum sunucuda; IslemDefteri sayfalı.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/StokRaporlari.cs` — StokRaporServisi + controller: KategoriSatis (R15), StokDevir (R16). Kategori alt ağacı UrunKategorisi (tenant_id, ust_id) indeksinden yürünür.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/FinansRaporlari.cs` — FinansRaporServisi + controller: KasaDefteri (R11), HurdaOzet (R17), KdvOzet (R18).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporKatalogu.cs` — static readonly RaporKarti[] Hepsi — 19 satır (kod, ad TR/EN anahtarı, rota, uç, roller, faz). GET /raporlar/katalog bunu döner. Test katalog sayısının 19 olduğunu ve her rotanın gerçekten kayıtlı bir uca karşılık geldiğini doğrular.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — Faz B: CariYaslandirma (:286) ve StokDegeri (:428) sunucu toplamasına çevrilir (TB-19) — dönen JSON ŞEKLİ DEĞİŞMEZ (Raporlar.razor:259-264 kayıtları kırılmasın), yalnız satır sayısı limitlenir + toplamSatir eklenir. CariYaslandirma ve VadeTakibi'ne DenetimServisi.ListeOkumaKaydet çağrısı eklenir (servise DenetimServisi enjekte edilir).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Rapor.SatisRaporServisi>(); StokRaporServisi; FinansRaporServisi.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — OnModelCreating'e 5 indeks (veri_modeli tablosundaki). Yorumla gerekçe: hangi rapor hangi indeksi kullanıyor.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Sabitler.HareketTurleri'ne "SATIS_IADE","ALIS_IADE","CARI_DUZELTME" eklenir. Liste zorlanmıyor ama rapor filtresi (tur parametresi) bu listeyi doğrulama için kullanacak — eksik kalırsa iade satırları filtrelenemez.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — YENİ SozlukRaporSeti sınıfı (yaklaşık 60 anahtar: rapor adları, sütun başlıkları, 'kaydeden kullanıcı' ibaresi, 'hiç satılmadı', 'dönem dışı kaynaklı iade', güvenilirlik metinleri) + Sozluk'a RaporSeti alanı + TR ve EN blokları. SozlukNav'a IslemDefteri, SatisAnalizi, StokAnalizi, MaliRaporlar.
- **YENI** `dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs` — İstemci tarafı ince sarmalayıcı (ApiIstemci üzerinden), her rapor için tipli record + Get metodu. Sayfalar doğrudan Api.GetAsync yerine bunu kullanır ki sahte API deseni (CariEkranTestleri.cs:46) tek yerden kurulabilsin.

## Ekranlar
- `/raporlar` · `dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — DEĞİŞTİR: sayfanın en üstüne katalogdan beslenen 'Rapor Merkezi' kart ızgarası (19 rapor, faz/rol filtreli). Mevcut aylık+yaşlandırma+stok+mutabakat blokları KORUNUR. (menü/yetki: rapor modülü)
- `/raporlar/islem-defteri` · `dotnet/Kuyumcu.Web/Pages/IslemDefteri.razor` — YENİ: tarih aralığı + tür + şube + cari + kullanıcı + ödeme aracı süzgeçleri, sayfalı tablo, üstte özet kartları, Yazdır düğmesi (@media print). (menü/yetki: rapor modülü)
- `/raporlar/satis-analizi` · `dotnet/Kuyumcu.Web/Pages/SatisAnalizi.razor` — YENİ: tek sayfada üç sekme — Ödeme aracı (R12), Kaydeden kullanıcı (R13), Şube karşılaştırma (R14). Menü şişmesin diye birleştirildi. (menü/yetki: rapor modülü)
- `/raporlar/kasa` · `dotnet/Kuyumcu.Web/Pages/KasaRaporu.razor` — YENİ: kasa seçici + gün gün devir/giriş/çıkış/kapanış farkı; kapanmamış gün amber satır. (menü/yetki: rapor modülü)
- `/raporlar/stok-analizi` · `dotnet/Kuyumcu.Web/Pages/StokAnalizi.razor` — YENİ (Faz B): kategori/reyon satış + stok devir hızı + ölü stok listesi; 'hiç satılmadı' ayrı sekme. (menü/yetki: rapor modülü)
- `/raporlar/mali` · `dotnet/Kuyumcu.Web/Pages/MaliRaporlar.razor` — YENİ (Faz B): KDV-fatura özeti + hurda/pusula özeti + iade özeti. (menü/yetki: rapor modülü)

## İş kuralları (fail-closed)
- K5 TUZAĞI: her tarih aralığı RaporAraligi.Coz'dan geçer; sayfa/servis hiçbir yerde DateTime.Today ile UTC aralığı kurmaz. Aksi hâlde İstanbul'da 00:00–03:00 cirosu yanlış güne düşer (RaporModulu.cs:31-33).
- İŞARET KORUNUR: işlem defteri TlTutari'yi HAM işaretiyle döndürür. Mevcut raporlarda Math.Abs kullanımı (RaporModulu.cs:67, :72) toplam üretmek için doğrudur ama satır dökümünde yön bilgisini yok eder; defter satırında yön kaybolursa tediye ile tahsilat ayırt edilemez.
- ÖDEME ARACI KIRILIMI hareketler.odeme_turu'ndan OKUNMAZ. A6/A7 kanıtı: karma fişte odeme_turu yalnız 'KARMA' özet etiketidir; gerçek kırılım kasa_hareketleri'ndeki bacak satırları + hareketler.nakit_tutar/veresiye_tutar'dır. Eski satırlarda nakit_tutar NULL = BİLİNMİYOR (Entities.cs nakit_tutar açıklaması) → ayrı 'bilinmiyor' sütunu, sıfır sayılmaz.
- PERSONEL RAPORU 'KAYDEDEN' DER, 'SATAN' DEMEZ. hareketler.kullanici_id işlemi kaydeden kullanıcıdır; vezne modeli (PARITE 19) yazılmadığı için satan ile tahsil eden ayrışmaz. Rapor başlığı, sütun adı ve sözlük metni bunu açıkça yazar — aksi hâlde prim/performans kararı yanlış veriye dayanır.
- TRANSFER ŞUBE CİROSUNA KATILMAZ: transfer hem çıkış hem giriş şubesinde satır doğurur (hareketler.sube_id + karsi_sube_id); ciroya katılsa aynı mal iki şubede sayılır ve toplam şişer.
- HİÇ SATILMAYAN ÜRÜNE GÜN SAYISI UYDURULMAZ: sonSatisTarihi null kalır, 9999/0 yazılmaz; 'hiçSatılmadı' ayrı kümedir. Aksi hâlde ölü stok sıralaması yeni girmiş malı en tepeye koyar.
- İADE ORANI PAYDASI AYNI DÖNEMİN SATIŞIDIR; kaynağı dönem dışında kalan iade 'donemDisiKaynakli' olarak AYRI sayılır — yoksa %100'ü aşan oran çıkar.
- KDV ÖZETİ SALT OKURDUR: faturalar tablosundaki hazır alanları toplar, matrah HESAPLAMAZ. hasDeger bugün elle giriliyor (PARITE 38); rapor bunu 'elle girilmiş' diye işaretler ve güvenilirliği düşürür.
- SAYFALAMA ZORUNLU: satır dönen her uçta limit [Range(1,500)]; limit verilmeden çağrı 400. Sınırsız döküm ADR-0014 yoluna aittir.
- KİŞİSEL VERİ → OKUMA İZİ: cariAd/telefon/TCKN dönen her rapor DenetimServisi.ListeOkumaKaydet çağırır (KVKK md.12, ADR-0004 append-only).
- RAPOR DEFTERE YAZMAZ: Katman 5'te HareketFabrikasi.Ekle çağrısı YOKTUR; denetim.sh'nin Hareketler.Add=1 sayacı değişmemelidir.

## Testler
- `dotnet/Kuyumcu.Tests/RaporOrtakTestleri.cs` [saf]
  - RaporAraligi bitis<baslangic → BadRequest
  - RaporAraligi yerel gün sınırı 00:00 İstanbul = 21:00 UTC önceki gün
  - RaporFiltresi limit 0/501 → BadRequest
  - RaporGuvenilirligi bilinmeyen>0 → TAM DEĞİL
  - RaporKatalogu tam 19 kayıt taşır ve kodlar tekildir
- `dotnet/Kuyumcu.Tests/RaporSetiTestleri.cs` [DB]
  - İşlem defteri satırı TL işaretini korur (satış +, alış −)
  - İşlem defteri limitsiz çağrılamaz
  - İşlem defteri tür süzgeci SATIS_IADE'yi bulur
  - Karma fiş ödeme aracı kırılımında bacaklara bölünür (nakit+kart ayrı satır, KARMA etiketi tek satır sayılmaz)
  - Nakit payı NULL olan eski satır 'bilinmiyor' sütununa gider, sıfıra yazılmaz
  - Transfer şube cirosuna KATILMAZ
  - Kaydeden kullanıcı raporu iki kullanıcıyı ayrı sayar
  - Kasa defteri gün kapanışı farkını gösterir, kapanmamış günü AÇIK işaretler
  - Stok devir: hiç satılmayan ürün gün sayısı NULL
  - Kategori satış alt kategorileri kapsar
  - İade oranı paydası dönem satışı; dönem dışı kaynaklı iade ayrı sayılır
  - KDV özeti IPTAL faturayı toplama katmaz
  - Yaşlandırma raporu 500'den çok cari olsa da limitle döner ve toplamSatir doğrudur (TB-19)
  - Cari adı dönen rapor denetim izine liste okuma satırı yazar
  - Yabancı kiracının hareketi hiçbir raporda görünmez
- `dotnet/Kuyumcu.Tests/RaporYetkiTestleri.cs` [HTTP]
  - TEZGAHTAR jetonuyla /raporlar/islem-defteri → 403
  - MUHASEBE jetonuyla → 200
  - rapor modülü yetkisi kapatılınca MUDUR → 403
  - Yanıt camelCase sözleşmesine uyar (satirlar, toplamSatir)
- `dotnet/Kuyumcu.Tests/RaporEkranTestleri.cs` [bUnit]
  - IslemDefteri süzgeç değişince sunucuya yeni istek atar (yol ve query doğrulanır)
  - Sayfa ileri/geri ofseti günceller
  - SatisAnalizi sekme değişince ilgili uca gider
  - Rapor Merkezi katalogdan 19 kart çizer
  - Personel tablosu başlığı 'Kaydeden' içerir, 'Satan' İÇERMEZ

## Mutasyon kanıtları
- M109 — RaporAraligi K5 dönüşümü yerine DateTime.Today kullanılırsa → RaporOrtakTestleri.Yerel_gun_siniri_isletme_saatine_gore KIRMIZI
- M110 — işlem defteri limit kapısı kaldırılırsa → Islem_defteri_limitsiz_cagrilamaz KIRMIZI
- M111 — ödeme aracı kırılımı hareketler.odeme_turu'ndan okunursa → Karma_fis_odeme_aracina_bacaklara_bolunur KIRMIZI
- M112 — TRANSFER şube cirosuna katılırsa → Transfer_sube_cirosuna_KATILMAZ KIRMIZI
- M113 — hiç satılmamış ürüne 9999 gün yazılırsa → Hic_satilmayan_urun_gun_sayisi_NULL KIRMIZI
- M114 — personel raporu sütunu 'Satan' olursa → Personel_tablosu_KAYDEDEN_der KIRMIZI
- M115 — CariYaslandirma'dan ListeOkumaKaydet kaldırılırsa → Kisisel_veri_donen_rapor_okuma_izi_yazar KIRMIZI
- M116 — katalogdan bir rapor silinirse → Katalog_19_rapor_sayar KIRMIZI
- M117 — nakit payı NULL satır 0 sayılırsa → Bilinmeyen_nakit_payi_sifira_yazilmaz KIRMIZI
- M118 — kategori hedefi alt kategorileri kapsamazsa → Kategori_satis_alt_kategorileri_kapsar KIRMIZI
- M119 — iade oranı paydasına dönem dışı kaynaklı iade katılırsa → Donem_disi_kaynakli_iade_AYRI_sayilir KIRMIZI
- M120 — KDV özeti IPTAL faturayı toplarsa → Kdv_ozeti_IPTAL_faturayi_katmaz KIRMIZI
- M121 — TB-19 düzeltmesinde toplamSatir limitlenmiş satır sayısından üretilirse → Yaslandirma_toplamSatir_gercek_sayidir KIRMIZI
- M122 — işlem defteri TlTutari'ye Math.Abs uygulanırsa → Islem_defteri_TL_isaretini_korur KIRMIZI

## Canlı doğrulama
- Yeni kiracı aç (kur kaydı için 10 sn bekle — /kur/guncel id gelene kadar; A-tuzağı)
- 1 alış (HAVALE, 50.000) + 3 satış (nakit 5.000 · kart 20.000 · karma fiş 12.000 nakit+5.000 kart+3.000 veresiye) + 1 satış iadesi + 1 şubeler arası transfer + 1 hurda alış (pusulalı)
- GET /raporlar/katalog → 19 kayıt; her rotanın uç karşılığı curl ile 200
- GET /raporlar/islem-defteri?limit=50 → satır sayısı ve TL işaretleri SQL ile birebir; limit'siz çağrı → 400
- GET /raporlar/odeme-araci → NAKIT/KART/VERESIYE toplamları kasa_hareketleri SQL toplamıyla birebir; karma fiş bacakları ayrı görünüyor
- GET /raporlar/sube-karsilastirma → transfer cirosu 0; iki şube toplamı = kiracı cirosu
- GET /raporlar/personel → kaydeden kullanıcı adı ve işlem sayısı doğru
- GET /raporlar/kasa-defteri → gün kapanışı yapılan günde fark görünüyor, yapılmayan gün AÇIK
- TEZGAHTAR kullanıcı aç, jetonuyla /raporlar/islem-defteri → 403
- denetim_kayitlari'nda yaşlandırma çağrısı için OKUMA satırı var (SQL)
- Ekranda Yazdır → tarayıcı önizlemesinde tablo tam, menü/süzgeç gizli
- API'yi durdur (kur zamanlayıcısı satır biriktirmesin), test kiracısını sil, kiracı sayısını doğrula

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/ (yeni migration çifti)`
- `DOKUNULMAZ: /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh (yeni varlık yok, Hareketler.Add yok)`
- `DOKUNULMAZ: Kuyumcu.Api/Data/HareketFabrikasi.cs, Kuyumcu.Api/Program.cs`

## Mevzuat dayanağı
- VUK md.253 (defter ve belgelerin saklanması) / md.256 (ibraz) — **DOGRULANMALI** — Raporun kendisi saklama yükümlülüğü doğurmaz; kaynak kayıtlar (hareketler, faturalar, pusulalar) zaten append-only ve ADR-0008 saklama rejimine tabi. Bu pakette mevzuat hükmü KODA YAZILMAZ.
- KVKK md.12 (veri güvenliği) — okuma izi — **DOGRULANMALI** — Projede zaten uygulanan ilke (DenetimIzi.cs:64-81 HassasVarliklar, ADR-0004). Paket yeni hüküm getirmez, mevcut ilkeyi eksik kalmış iki uca yayar.
- KDV Kanunu md.23/(e) özel matrah (kuyum) — **DOGRULANMALI** — R18 SALT OKURDUR — faturalar tablosundaki hazır kdv_turu/has_deger/kdv_tutar alanlarını toplar. Matrah OTOMATİĞİ paket 38'dir; bu pakette hesap yapılmaz, dolayısıyla doğrulanmamış hüküm koda girmez.

## Riskler
- Rapor sayısını 19'a çıkarmak menüyü şişirir → Rapor Merkezi kart ızgarası + menüde tek 'Raporlar' girişi ile karşılandı; menüye 6 yeni NavLink eklenmesi REDDEDİLDİ (yalnız 3 eklenir).
- Mevcut Raporlar.razor kayıtları (259-264) sunucu yanıt şekline bağlı; TB-19 düzeltmesinde şekil değişirse ekran sessizce boşalır → şekil DEĞİŞMEZ kuralı + regresyon testi.
- Faz B'de CariYaslandirma'ya limit gelmesi, bugünkü 'tüm cariler' davranışını değiştirir → varsayılan limit 500 ve toplamSatir ile beyan; 500'ü aşan kiracıda ekranda 'daha fazla' düğmesi.
- Yeni indeksler yazma yolunu (her satışta 4 ek indeks bakımı) bir miktar yavaşlatır → k6 yük testi paket sonunda yeniden koşulmalı, p95 karşılaştırılmalı.
- Personel raporu prim/ceza amacıyla kullanılırsa 'kaydeden ≠ satan' ayrımı yanlış karar doğurur → metin kuralı + mutasyon M114 ile korunur.

## Açık sorular
- PARITE dosyası '19 rapor' der ama LİSTEYİ HİÇBİR YERDE VERMEZ — depo genelinde arandı, enumerasyon YOK (PARITE-IS-LISTESI-MUHURLU-2026-09-14.md:107 tek geçiş; PAZAR-ARASTIRMASI'nda rapor listesi yok). Yukarıdaki 19'luk küme BENİM ÖNERİMDİR, rakip kanıtı değildir. Rakip listesi doğrulanamıyorsa mühür kuralı gereği bu küme oylamayla sabitlenmeli.
- '5 uç + 3 döküm' ifadesindeki 3 dökümün hangileri olduğu belgede yazmıyor; cari ekstre + kasa hareketleri + banka hareketleri olduğu ÇIKARIMDIR (CariModulu.cs:606, KasaModulu.cs:353, BankaModulu.cs:353).
- Rapor Merkezi'nde rol bazlı gizleme mi yoksa 'yetkin yok' rozetli gösterim mi? Gösterim, kullanıcıya ürünün neler yapabildiğini anlatır; gizleme rol sızıntısını azaltır. Karar oylamaya.
- İşlem defteri varsayılan dönem: son 7 gün mü, bugün mü? Büyük kiracıda 7 gün ilk açılışta yavaş olabilir.

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Ortak altyapı (RaporAraligi, RaporFiltresi, RaporGuvenilirligi, SayfaliSonuc) doğru bir soyutlama ve bugün 4 yerde tekrarlanan K5 dönüşümünü tek noktaya alıyor. RaporModulu.cs'i şişirmeyip konu dosyalarına ayırma kararı TB-10 ile tutarlı. İşaret koruma ve ödeme aracı kırılımının kasa_hareketleri bacaklarından okunması kuralları kanıtlı ve doğru. Reddetme sebebim yok; koşullar bayat okuma ve katalog testinin diğer katmanı kırması üzerine.
  - Koşullar: KAPSAM İÇİ maddesinden 'Sabitler.HareketTurleri'ne SATIS_IADE, ALIS_IADE, CARI_DUZELTME eklenmesi' ÇIKARILACAK. Üçü de Entities.cs:31-32'de mevcut (A17-BİTTİ, ekip oyu Q2). Var olanı tekrar eklemek derlenmez ve spesifikasyonun bayat okuma üzerine kurulduğunu gösterir. · önkoşul_paketler [33] KALDIRILACAK: paket 33 2026-09-16 00:30'da BİTTİ (SONRAKI-OTURUM-PROMPT.md:826 A17-BİTTİ, 971/971 test yeşil). Paket 34'ün önkoşulu yoktur. · RaporKatalogu testi 'tam 19 kayıt' DEĞİL, 'en az 19 kayıt VE kodlar tekil VE her rotanın kayıtlı bir uç karşılığı var' olacak. Gerekçe: K6'nın paket 39'u /raporlar/has-defteri ve /raporlar/has-defteri/mutabakat, paket 42'si /raporlar/irsaliyesiz-sevk ekliyor; == 19 testi o paketleri kırar ve uygulayıcı testi 'düzeltmek' için katalogdan rapor silmeye kalkar. · Yeni controller'lara [RolGerekli("SAHIP","MUDUR","MUHASEBE")] ve [Yetki.ModulYetkisi("rapor")] AÇIKÇA yazılacak — RaporController'ın sınıf niteliği miras alınmaz, ayrı sınıf ayrı kapıdır. · ortak_kararlardaki 'denetim.sh varlık sayacı (bugün 31) değişmemeli; değişiyorsa biri ortak karar 1'i ihlal etmiştir' cümlesi YANLIŞ KURULMUŞ ve düzeltilecek: denetim.sh:66 BEKLENEN_VARLIK=15 ve karşılaştırma -ge'dir; sayı arttığında betik KIRILMAZ, dolayısıyla bu bir denetim sinyali değildir. Sinyal isteniyorsa betiğe ayrı bir üst-sınır kontrolü eklenmeli. · TB-19 kapsamı DÜRÜSTÇE yazılacak: sınırsız ToListAsync yalnız CariYaslandirma ve StokDegeri'nde değil, GunlukOzet, AylikOzet ve PosFaturaMutabakati'nde de var (üçü birden okundu). Faz B ya hepsini kapsasın ya da kapsamadıklarını AÇIK BORÇ olarak teknik borç envanterine yazsın. 'İki tanesi' demek eksik ölçümdür. · Faz A ve Faz B AYRI SEVK EDİLECEK (8-10 gün tek turda sevk edilemez). Faz A'nın kendi başına değeri: RaporOrtak + katalog + işlem defteri.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Salt okur, yeni tablo yok, ExecuteUpdate yok, Hareketler.Add yok, TB-19 sayfalama, K5 yerel gün. Güvenlik yönünden doğru; ancak spesifikasyon iki noktada koda göre eski (HareketTurleri) ve kişisel veri okuma izini Faz B'ye erteliyor — bu kabul edilemez, Cari zaten hassas varlık.
  - Koşullar: K34-1 (STALE — çift mekanizma): 'Sabitler.HareketTurleri'ne SATIS_IADE, ALIS_IADE, CARI_DUZELTME eklenmesi' maddesi ÇIKARILIR — üçü ZATEN Entities.cs:32'de (A17-BİTTİ, 2026-09-16). Uygulayıcı bu satırı tekrar ekler ya da 'yok' diye tasarım değiştirirse çift tanım doğar. · K34-2 (okuma izi Faz A'ya): cariAd dönen islem-defteri (R10) Faz A'dadır; DenetimServisi.ListeOkumaKaydet(islem, nameof(Cari), adet, 'islem-defteri') Faz A'da uygulanır, Faz B'ye ertelenmez (Cari HassasVarliklar'da — DenetimIzi.cs:71). Hurda-ozet için GiderPusulasi HassasVarliklar listesinde DEĞİL (DenetimIzi.cs:69-78) → HurdaModulu.cs:67 deseniyle 'GiderPusulasi' varlık türü adıyla ListeOkumaKaydet çağrılır VE bu tür HassasVarliklar'a eklenir (ekleme, hassas=1 süzgecinde görünmesi için şart). · K34-3: Yeni controller'lar (SatisRaporController, StokRaporController, FinansRaporController) sınıf düzeyinde [Authorize][RolGerekli('SAHIP','MUDUR','MUHASEBE')][ModulYetkisi('rapor')] taşır — RaporModulu.cs:508-512 ile birebir; RaporYetkiTestleri her yeni rota için TEZGAHTAR 403'ü ayrı ayrı doğrular (katalog üzerinden döngü). · K34-4: Rapor uçlarında cariId/kullaniciId/subeId süzgeç parametreleri kiracı sınırında doğrulanır (yabancı kiracının cariId'siyle süzme → boş sonuç DEĞİL 404; varlık sızdırılmaz). · K34-5: Katalog ucu rolleri sunucudan döner ama Rapor Merkezi'nde 'yetkin yok' rozetli GÖSTERİM DEĞİL GİZLEME uygulanır (rol matrisi sızıntısı azaltılır; sunucu son sözü söyler).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Ortak altyapı (RaporAraligi, güvenilirlik, sayfalama) doğru ve K5 tuzağıyla uyumlu; ama kabul ölçütünün çekirdeği olan 19 rapor listesi belgede yok (mimarın kendi önerisi), bir test DB katmanına yanlış yerleştirilmiş, Entities değişikliği bayat ve DenetimServisi enjeksiyonu mevcut testleri kırar. Faz A koşullarla sevk edilebilir; Faz B oylama bekler.
  - Koşullar: Entities.cs 'HareketTurleri'ne SATIS_IADE/ALIS_IADE/CARI_DUZELTME eklenir' maddesi ve 'mevcut_durum'daki 'listede YOK' iddiası BAYAT: Entities.cs:24-31 üçünü zaten içeriyor (ekip oyu Q2, 2026-09-15). Entities.cs dosya ayak izinden çıkarılır; paket 34 Entities.cs'e DOKUNMAZ. · 'Islem_defteri_limitsiz_cagrilamaz' RaporSetiTestleri (DB) altında yazılamaz: [Range(1,500)] yalnız controller model bağlamada çalışır. Ya RaporFiltresi.Dogrula() servis içinde limit<1 || limit>500 → BadRequestException fırlatır ve DB testi onu sınar (M110 böyle çalışır), ya da senaryo RaporYetkiTestleri (HTTP) altına alınır ve 400 gövdesi okunur. · 19'luk rapor kümesi mimarın kendi önerisidir (acik_sorular 1, PARITE:107 tek geçiş). Faz B raporlarının kabul ölçütü küme oylamayla mühürlenmeden YAZILMAZ; Faz A (katalog + 5 rapor + ortak altyapı) tek başına sevk edilir ve katalog sayısı testi 'tam 19' yerine 'Faz A'da kayıtlı N' + 'her katalog rotası gerçek bir controller ucuna karşılık gelir' (reflection ile [HttpGet] rotaları toplanır) biçiminde yazılır — sayı testi tek başına tautolojidir, rota testi gerçek koruma sağlar. · M115/Kisisel_veri_donen_rapor_okuma_izi_yazar: RaporServisi'ne DenetimServisi enjekte edilince mevcut 'new MRapor.RaporServisi(db)' çağrıları (RaporFaturaTestleri.cs:38,57,96 ve diğerleri) derlenmez → parametre opsiyonel (DenetimServisi? denetim = null) OLMAZ, çünkü null'da iz düşmez ve sessiz açık doğar; bunun yerine tüm mevcut çağrılar 'new DenetimServisi(db, new HttpContextAccessor())' (MasakEntegrasyonTestleri.cs:23 deseni) ile güncellenir ve M115 için test denetim_kayitlari'nda eylem=OKUMA, varlik_turu=Cari satırını SQL/DbSet ile sayar. · Yabancı kiracının hareketi hiçbir raporda görünmez' testi çok katmanlı koruma tuzağına düşer (global filtre); ikinci katman (elle TenantId şartı) filtresiz bağlamda ayrıca sınanır ya da bu test 'mutasyon kanıtı' listesinden çıkarılıp 'regresyon' diye etiketlenir. · RaporEkranTestleri (bUnit): süzgeç/sayfa/sekme testlerinde her assert WaitForAssertion; istek sayısı/yolu SahteApi kayıt listesinden okunur (CariEkranTestleri.cs:24,47-52 deseni); dosya --logger trx ile 3 ardışık koşu (§6:847 kararsız test açık). · Kültür: yeni ekranlardaki özet kartları/tablolar ve print çıktısı için GrafikKulturTestleri.cs:24-32 deseniyle en az bir tr-TR koşan test (sayı biçimi 'N2' bilinçli, viewBox/SVG ya da CSV'ye giden her sayı InvariantCulture). · TB-19 Faz B 'Yaslandirma_toplamSatir_gercek_sayidir': 500 cari fikstürü yerine limit parametrik (limit=2, 5 cari) — M121 yine yakalanır, test süresi şişmez. · Canlı doğrulama: her uç için HTTP kodu + eşleşen SQL (islem-defteri satır sayısı ve işaret, odeme-araci kasa_hareketleri SUM by odeme_turu, personel COUNT by kullanici_id); TEZGAHTAR 403; denetim_kayitlari OKUMA satırı; API durdurulup kiracı silinir.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- BAYAT OKUMA (üç mercek birleşik): 'Sabitler.HareketTurleri'ne SATIS_IADE/ALIS_IADE/CARI_DUZELTME eklenir' maddesi ÇIKARILIR — üçü Entities.cs:24-32'de zaten var (A17-BİTTİ, ekip oyu Q2). Entities.cs dosya ayak izinden çıkarılır; paket 34 Entities.cs'e DOKUNMAZ.
- Önkoşul [33] KALDIRILDI: paket 33 2026-09-16 00:30'da BİTTİ (SONRAKI-OTURUM-PROMPT.md:826, 971/971 yeşil). Paket 34'ün önkoşulu yoktur.
- Katalog testi 'tam 19' DEĞİL: 'en az N kayıt VE kodlar tekil VE her katalog rotası yansımayla toplanan gerçek bir [HttpGet] ucuna karşılık gelir'. ==19 testi paket 39 (has-defteri, mutabakat) ve 42'yi (irsaliyesiz-sevk) kırar; sayı testi tek başına tautolojidir.
- 19'luk küme mimarın kendi ÖNERİSİDİR (PARITE:107 tek geçiş, liste hiçbir yerde yok). Faz B raporlarının kabul ölçütü küme oylamayla mühürlenmeden yazılmaz; Faz A (RaporOrtak + katalog + 5 rapor + işlem defteri) ayrı ve tek başına sevk edilir.
- Okuma izi Faz A'ya: cariAd dönen işlem defteri için DenetimServisi.ListeOkumaKaydet(...,nameof(Cari),...,'islem-defteri') Faz A'da uygulanır (Cari hassas — DenetimIzi.cs:71). Hurda-özet için 'GiderPusulasi' türüyle kayıt + bu tür HassasVarliklar'a eklenir (DenetimIzi.cs:69-78).
- M115 enjeksiyon kırılması: RaporServisi'ne DenetimServisi girince mevcut 'new RaporServisi(db)' çağrıları (RaporFaturaTestleri.cs:38,57,96 vd.) derlenmez. Opsiyonel null parametre YASAK (sessiz açık); tüm çağrılar 'new DenetimServisi(db, new HttpContextAccessor())' deseniyle güncellenir.
- Yeni controller'lar (SatisRapor/StokRapor/FinansRapor) sınıf düzeyinde [Authorize][RolGerekli("SAHIP","MUDUR","MUHASEBE")][ModulYetkisi("rapor")] AÇIKÇA taşır — sınıf niteliği miras alınmaz; RaporYetkiTestleri katalog üzerinden döngüyle her rota için TEZGAHTAR 403 doğrular.
- Süzgeç parametreleri (cariId/kullaniciId/subeId) kiracı sınırında doğrulanır: yabancı kiracı kimliğiyle süzme boş sonuç DEĞİL 404 döner (varlık sızdırılmaz). Rapor Merkezi'nde 'yetkin yok' rozetli GÖSTERİM değil GİZLEME; sunucu son sözü söyler.
- Limit testi yeri düzeltilir: [Range(1,500)] yalnız controller bağlamada çalışır — RaporFiltresi.Dogrula() servis içinde limit<1||>500 → BadRequestException fırlatır ve DB testi onu sınar, ya da senaryo HTTP testine taşınıp 400 gövdesi okunur. TB-19 Faz B testi limit parametrik (limit=2, 5 cari).
- TB-19 kapsamı DÜRÜST yazılır: sınırsız ToListAsync CariYaslandirma ve StokDegeri dışında GunlukOzet, AylikOzet ve PosFaturaMutabakati'nde de var — ya hepsi kapsanır ya kapsanmayanlar açık borç yazılır. denetim.sh:66 BEKLENEN_VARLIK=15 ve -ge'dir; 'sayaç 31 değişmemeli' cümlesi yanlış kurulmuş, düzeltilir. Faz A/B ayrı sevk.
