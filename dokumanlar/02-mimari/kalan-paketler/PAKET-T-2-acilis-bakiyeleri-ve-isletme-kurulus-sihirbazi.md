# Paket T-2 — Açılış bakiyeleri ve işletme kuruluş sihirbazı

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: 3-4 gün (Faz A 2-2,5 gün · Faz B 1-1,5 gün). 47-A ile birlikte sevk edildiğinde ortak altyapı (CsvOkuyucu, oturum, kuru prova) paylaşıldığı için T-2 tek başına 5+ gün olurdu; birlikte toplam 6-8 gün. gün · Önkoşullar: 47-A — kapılı, kuru provalı içe aktarım (CsvOkuyucu, IIceAktarilabilir sözleşmesi, oturum/geri alma altyapısı). MÜHÜR 2 gereği T-2 onunla BİRLİKTE sevk edilir., 20 — metal tipi genişletme (açılış stoğunda platin/paladyum satırları doğru saflıkla girsin), 27 — çoklu birim cari bakiye (açılış bakiyesi TL + has gram dışına çıkacaksa şema oradan gelir), 34-A / 35-A — rapor mutabakatı (açılış devri raporlarda 'devir' olarak ayrışmalı), UX-3-B — basit/profesyonel mod altyapısı (sihirbaz basit modda doğar), UX-1-B — PATCH/pasife alma uçları (DoD sözleşmesi: yeni varlık düzeltme yoluyla doğar), 48-A — storno/ters kayıt motoru (geri alma ACILIS storno'su olarak yazılır; yoksa geri alma yalnız oturum iptaliyle sınırlı kalır) · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3
> ⚠ **VETO:** YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: İTİRAZ (bağlayıcı değil — veto yalnız Mehmet'indir): 'Sabitler.HareketTurleri'ne ACILIS eklenmesi' FAIL-CLOSED 1 olarak sunuluyor; oysa Entities.cs:31-32 kodun kendi yorumuyla 'Bu dizi kodda ZORLANMAZ (doğrulama yok)'. Kapı iddiası kanıtsız; dil düzeltilmeden ve gerçek kapı (denetim.sh sayacı + DuzeltmePolitikasi tam-örtüşme testi) yazılmadan Faz A mühürlenmemeli.

## Hedef
Yeni kurulan dükkânın ilk gününü tek sihirbazdan kurmak: şube, kullanıcılar, kasa/banka, cari ve stok açılış bakiyeleri. Açılış deftere YALNIZ HareketFabrikasi.Ekle ile "ACILIS" türünde düşer (mühür 2), kuru provalı, gerekçeli, denetim izli ve mühürlenene dek geri alınabilir.

## Kodda bugünkü durum
Kurulum/açılış kavramı kodda YOK: Sabitler.HareketTurleri (Entities.cs:24-34) 14 tür sayar, "ACILIS" yok; DuzeltmePolitikasi.HareketListesi (Meta/DuzeltmePolitikasi.cs:149-167) de yok. Parça servisler hazır: SubeServisi (Sube/SubeModulu.cs:112-133), KullaniciServisi (Kullanici/KullaniciModulu.cs:392-441), KasaServisi.Olustur/HareketGir (Kasa/KasaModulu.cs:74,134), BankaServisi (Banka/BankaModulu.cs:334-362), CariServisi.Duzeltme (Cari/CariModulu.cs:555 CARI_DUZELTME + gerekçe ≥20), UrunServisi.StokSayimGuncelle (Urun/UrunModulu.cs:605). Ekran tarafında yalnız /isletme (Kuyumcu.Web/Pages/Isletme.razor:1) var, sihirbaz yok. İçe aktarım hiç yok; dışa aktarım tam (Disaaktarim/Kaynaklar.cs, CsvYazici.cs:19).

## Kapsam
**İçinde:**
- Kurulum sihirbazı: adım 1 işletme/şube, adım 2 kullanıcılar+roller, adım 3 kasa/banka açılış, adım 4 cari açılış, adım 5 stok açılış, adım 6 mühürleme.
- Yeni hareket türü ACILIS: Sabitler.HareketTurleri + DuzeltmePolitikasi kaydı (SALT_OKUNUR/STORNO), tek yazma yolu HareketFabrikasi.Ekle.
- Açılış kalemlerinin kuru provası (hiç yazma), satır bazlı doğrulama raporu, tek transaction uygulama.
- Mühürlenene dek oturum bazlı geri alma (ters ACILIS kaydı değil, oturum iptali — bkz. iş kuralları).
- CSV şablonu indirme (cari açılış, stok açılış) — CsvYazici yeniden kullanımı; dosya okuma 47-A'nın CsvOkuyucu'sundan.
- Açılış stoku DEVIR partisi + birim_maliyet_tl NULL (sıfır maliyet yazılmaz).
- UX-3 BASİT mod: sihirbaz varsayılan basit; ileri alanlar <ProAlan> içinde (UX-S5).
- Kurulum durumu rozeti ve eksik adım listesi /isletme ekranında.

**Dışında:**
- Rakip programdan dosya içe aktarım motoru, eşleştirme, oturum/geri alma altyapısı — PAKET 47-A'dır; T-2 onu tüketir, yeniden yazmaz.
- Geçmiş hareket/fatura/kasa hareketi göçü (Mehmet vetosu, seçenek 3).
- Banka ekstresi içe aktarma ve IBAN eşleştirme (paket 32).
- İmalat/atölye açılışı, yarı mamul (Katman 1 kapsam dışı).
- E-Fatura/e-Arşiv açılış bildirimi (paket 38-B/E-BELGE).
- Storno/ters kayıt motoru (paket 48-A) — T-2 yalnız onun sözleşmesine kaydolur.
- Çok kiracılı toplu kurulum (bayi/servis sağlayıcı ekranı), F-2-min kayıt kilidi.
- Açılış tarihinin kullanıcıdan alınması (UtcNow zorunlu, bkz. iş kuralları).

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| hareketler | ŞEMA DEĞİŞMEZ — yalnız tur='ACILIS' yeni değeri kullanılır. Sabitler.HareketTurleri (Entities.cs:24) belge dizisine 'ACILIS' eklenir; kolon zaten MaxLength(20) string. | tur, tl_tutari, has_grami, kur_snapshot_id, cari_id, sube_id, kullanici_id (mevcut) | mevcut (tenant_id, tarih) indeksleri yeterli — DOĞRULANMALI: hareketler indeks listesi okunmadı | - |
| kurulum_durumu | YENİ — kiracı başına TEK satır: hangi adımlar bitti, mühür var mı, mühürleyen kim/ne zaman. IKiraciVarlik. | id, tenant_id (tekil), adim_bayraklari (json/text), muhurlendi bool, muhur_tarihi, muhurleyen_kullanici_id, olusturuldu, guncellendi | UNIQUE (tenant_id) | 20260917_KurulumVeAcilis |
| acilis_kalemleri | YENİ — uygulanan her açılış kaleminin izi: idempotans + oturum geri alma + 'bu bakiye nereden geldi' sorusunun cevabı. IKiraciVarlik. | id, tenant_id, oturum_id (47-A ice_aktarim_oturumlari ya da yerel ULID), tur (KASA|BANKA|CARI|STOK), hedef_id, hareket_id, kasa_hareket_id, parti_id, tl_tutar, has_gram, durum (UYGULANDI|GERI_ALINDI), olusturuldu | (tenant_id, oturum_id), (tenant_id, tur, hedef_id) UNIQUE — aynı hedefe ikinci açılış engellenir | 20260917_KurulumVeAcilis |
| stok_partileri | DEĞİŞİKLİK YOK — açılış stoğu kaynak='DEVIR', birim_maliyet_tl=NULL deseniyle girer (StokPartisi.cs:100 'NULL = maliyet bilinmiyor, sıfır DEĞİL'). | kaynak, birim_maliyet_tl, birim_maliyet_has, giris_hareket_id | mevcut | - |
| kasa_hareketleri | DEĞİŞİKLİK YOK — KasaHareket.HareketId zaten nullable (Entities.cs:459); açılış kasa satırı üretilen ACILIS hareketine bağlanır. | hareket_id (mevcut, nullable) | mevcut | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/kurulum/durum` | SAHIP, MUDUR | Adım adım kurulum durumu, eksikler, mühür bilgisi. Sihirbazın tek gerçeği. | [Authorize] + ModulYetkisi("kurulum") |
| GET | `/api/v1/kurulum/sablon/{tur}` | SAHIP | cari|stok açılış CSV şablonu (BOM'lu UTF-8, CsvYazici.Satir); başlıklar ve örnek satır. | OranSiniri 'disa-aktarim' politikası (10/sa) yeniden kullanılır |
| POST | `/api/v1/kurulum/acilis/prova` | SAHIP | KURU PROVA: satırları doğrular, satır bazlı hata kodu + özet döner, HİÇBİR yazma yapmaz. | Mühürlüyse 409; yazma yok |
| POST | `/api/v1/kurulum/acilis/uygula` | SAHIP | Prova jetonuyla tek transaction uygulama; kasa/banka/cari/stok açılışı ACILIS hareketleriyle yazılır. | Gerekçe ≥20 · KurDogrula · GunKapanisiKapisi (kasa bacağı) · KrediLimitiDogrula · Mühür 409 |
| POST | `/api/v1/kurulum/acilis/geri-al` | SAHIP | Mühürlenmemiş oturumu geri alır; 48-A storno motoru üzerinden ters ACILIS kaydı (ikinci silme yolu yok). | Gerekçe ≥20 · mühürlüyse 409 · DenetimServisi gerekçeli yazma izi |
| POST | `/api/v1/kurulum/muhurle` | SAHIP | Açılışı kapatır: sonrasında açılış uçları 409 verir, düzeltme yalnız normal defter yollarıyla yapılır. | Tüm zorunlu adımlar tamam değilse 400; geri dönülemez → onay + gerekçe |
| POST | `/api/v1/kurulum/sube` | SAHIP | Sihirbaz adım 1 — mevcut SubeServisi.Olustur'a ince sürücü (SubeModulu.cs:112). | Mevcut kapılar aynen |
| POST | `/api/v1/kurulum/kullanici` | SAHIP | Adım 2 — KullaniciServisi.Olustur sürücüsü; rol seçimi Sabitler.Roller'den (KullaniciModulu.cs:392). | Mevcut parola politikası ve rol kapıları aynen |
| PATCH | `/api/v1/kurulum/durum` | SAHIP | Adım atlama/geri dönme tercihi (UX-S3: gövdede olmayan alan korunur). | Mühürlüyse yalnız okunur |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs` — DTO + KurulumServisi + KurulumController tek dosya (modül deseni). Servis YALNIZ sürücüdür: SubeServisi/KullaniciServisi/KasaServisi/BankaServisi/CariServisi/UrunServisi çağırır, kendi db.*.Add'ini açmaz.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs` — Kuru prova + uygulama. Açılış defter bacağını HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket{Tur="ACILIS"…}) ile yazar — para el değiştirmediği için iz Yok (CariModulu.cs:173-177 gerekçesiyle aynı).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisKaynaklari.cs` — 47-A'nın IIceAktarilabilir sözleşmesini uygulayan AcilisBakiyeKaynagi + AcilisStokKaynagi (PAKET-47 spec:56). Şablon başlıkları burada tek yerde tanımlanır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs` — Sabitler.HareketTurleri dizisine "ACILIS" eklenir (satır 24-34). Yorumda 'açılış devri, para el değiştirmez' gerekçesi yazılır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` — HareketListesi'ne T("ACILIS", GerekceAnahtarlari.Defter|StornoYok, geri-al ucu) eklenir; kayıt yoksa DuzeltmePolitikasiTestleri kırmızıdır (UX-S1).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs` — KurulumDurumu ve AcilisKalemi varlıkları (IKiraciVarlik) — ya bu dosyaya ya Data/Kurulum.cs'e. denetim.sh BEKLENEN_VARLIK '-ge' olduğu için sayaç bozulmaz.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki yeni DbSet + HasQueryFilter kiracı süzgeci (mevcut kalıp).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` — KasaServisi.HareketGir'e opsiyonel 'bagliHareketId' parametresi: açılış kasa satırı üretilen ACILIS hareketine bağlanır. İkinci yazma yolu açılmaz, mevcut yol parametrelenir.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — KurulumServisi/AcilisServisi DI kaydı; Program.cs'e dokunulmaz (mevcut kalıp).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` — Yeni modül anahtarı "kurulum" tohumlanır (mevcut anahtarlar: admin/banka/cari/fatura/islem/kasa/kullanici/rapor/urun).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — Yeni sayaç: Modules/Kurulum altında 'Hareketler\.Add|db\.Cariler\.Add|db\.Urunler\.Add' = 0 (Bertan'ın 47 koşulu). Mevcut 'Hareketler.Add = 1' sayacı değişmez.

## Ekranlar
- `/kurulum` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Kurulum.razor` — Altı adımlı sihirbaz; her adımda kaydet→sonuç bandı (UX-S27), boş durum (UX-S8), kilitli düğme sebebi (UX-S30), ProAlan ile ileri alanlar (UX-S5). Varsayılan BASİT mod (UX-3). Id öneki 'kur-'. (menü/yetki: KenarCubugu.razor'a RolluGorunur("/kurulum","SAHIP") ile eklenir; mühürlendiyse menüde 'tamamlandı' rozetiyle kalır)
- `/kurulum (adım 4-5 sekmesi)` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Bilesenler/AcilisYukleyici.razor` — Şablon indir → yapıştır/yükle → KURU PROVA sonucu tablosu (satır no, hata kodu, açıklama) → GerekceAlani (UX-S10) → Uygula. Uygula, prova temiz değilken disabled ve sebebi yazılı. (menü/yetki: Sayfa içi; sunucu SAHIP kapısı son sözü söyler)
- `/isletme` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor` — DEGISTIR: üstte kurulum durumu kartı (eksik adımlar + /kurulum bağlantısı, mühür tarihi). UX-S23: sayı gösteren yer kayda bağlanır. (menü/yetki: Mevcut SAHIP kapısı)
- `/cariler · /urunler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Cariler.razor` — DEGISTIR (küçük): ekstre/parti listesinde ACILIS satırı 'Açılış devri' olarak çevrilir (UX-S31 ham kod ekrana basılmaz) ve DegismezIpucu ile düzeltme yolu yazılır (UX-S25). (menü/yetki: Mevcut)

## İş kuralları (fail-closed)
- FAIL-CLOSED 1 — Açılış deftere YALNIZ HareketFabrikasi.Ekle ile, tur='ACILIS' ile girer. Modules/Kurulum altında Hareketler.Add sıfırdır ve denetim.sh bunu sayar.
- FAIL-CLOSED 2 — Kuru prova zorunlu: uygula ucu, aynı içerik özeti (hash) üzerinden alınmış temiz prova jetonu olmadan 400 döner.
- FAIL-CLOSED 3 — Ödeme izi OdemeIzi.Yok'tur: açılışta para el değiştirmez, tevsik günlük nakit kümülatifi ve MASAK bağlantılı işlem kümülatifi ETKİLENMEZ (CariModulu.cs:173-177 gerekçesi).
- FAIL-CLOSED 4 — Açılış stoğunda maliyet SIFIR YAZILMAZ: kaynak='DEVIR', birim_maliyet_tl=NULL; kullanıcı maliyet verdiyse o yazılır ve kur_snapshot_id ile has karşılığı türetilir.
- FAIL-CLOSED 5 — Tarih kullanıcıdan alınmaz: tüm açılış kayıtları UtcNow ile tarihlenir; kapanmış güne yazma GunKapanisiKapisi.Dogrula (KasaModulu.cs:145) ile zaten reddedilir.
- FAIL-CLOSED 6 — Ürün kartı yoksa stok açılış satırı HATA'dır; otomatik ürün üretilmez (Bertan koşulu, PAKET-47:199).
- FAIL-CLOSED 7 — Aynı hedefe ikinci açılış yok: acilis_kalemleri (tenant_id, tur, hedef_id) UNIQUE; ikinci deneme 409 ve satır bazlı 'zaten açılış yapılmış' hatası.
- FAIL-CLOSED 8 — Mühürden sonra açılış/geri alma uçları 409; düzeltme yalnız normal defter yolu (48-A storno, CariServisi.Duzeltme) ile yapılır.
- FAIL-CLOSED 9 — Tek transaction: bir satır patlarsa oturumun tamamı geri alınır (BeginTransactionAsync, CariModulu.cs:583 kalıbı).
- FAIL-CLOSED 10 — Gerekçe ≥20 karakter hem uygula hem geri-al ucunda servis içinde elle doğrulanır (DataAnnotations yalnız controller yolunda çalışır — CariModulu.cs:560 uyarısı).
- İŞARET SÖZLEŞMESİ — cari açılışta borc_tl pozitif = müşteri bize borçlu → TlDelta pozitif; borc_has_gram pozitif = altın borcu artar; hareketin HasGrami işareti VERESIYE sözleşmesiyle aynı yönde yazılır (CariModulu.cs:596).
- DENETİM — her açılış kalemi ve mühür DenetimServisi gerekçeli yazma izi düşürür; denetim tablosu append-only kalır (ADR-0004).

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/AcilisBakiyeTestleri.cs` [DB (gerçek MySQL)]
  - Cari_acilisi_ACILIS_hareketi_uretir_ve_bakiyeyi_gunceller
  - Acilis_hareketinin_nakit_ve_veresiye_payi_SIFIRDIR
  - Acilis_tevsik_gunluk_nakit_toplamini_DEGISTIRMEZ
  - Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir
  - Kasa_acilisi_kasa_bakiyesini_ve_bagli_hareket_id_yi_doldurur
  - Ayni_hedefe_ikinci_acilis_REDDEDILIR
  - Bir_satir_patlarsa_TUM_oturum_geri_alinir
  - Urun_karti_yoksa_stok_acilis_satiri_HATA_olur
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/KurulumHttpTestleri.cs` [HTTP (WebApplicationFactory)]
  - Kuru_prova_HICBIR_satir_yazmaz_200_doner
  - Prova_jetonu_olmadan_uygula_400
  - TEZGAHTAR_acilis_uygula_403
  - Muhurlu_kiracida_acilis_uygula_409
  - Gerekce_19_karakter_400
  - Baska_kiracinin_carisine_acilis_404
  - Sablon_indirme_BOM_ile_baslar_ve_CSV_doner
  - Kurulum_durum_ucu_eksik_adimlari_listeler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/KurulumEkranTestleri.cs` [bUnit]
  - Prova_temiz_degilken_Uygula_dugmesi_KAPALI_ve_sebebi_yazili
  - Gerekce_20_karakterden_kisayken_Uygula_KAPALI
  - Hata_satirlari_tabloda_satir_no_ile_gosterilir
  - Muhurlendikten_sonra_adim_formlari_salt_okunur
  - Basit_modda_ileri_alanlar_ProAlan_icinde_gizli
  - Sunucu_hatasi_bos_duruma_cevrilmez_HataBandi_cikar
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs` [Saf (birim)]
  - ACILIS_turu_DuzeltmePolitikasi_haritasinda_KAYITLI
  - Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur
  - ACILIS_DUZENLENEBILIR_DEGILDIR
  - Acilis_isaret_sozlesmesi_VERESIYE_ile_ayni_yonde

## Mutasyon kanıtları
- OdemeIzi.Yok yerine OdemeIzi.Nakitten(tutar) yazılınca → 'Acilis_tevsik_gunluk_nakit_toplamini_DEGISTIRMEZ' KIRMIZI
- DEVIR partisinde NULL yerine 0m maliyet yazılınca → 'Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir' KIRMIZI
- (tenant_id, tur, hedef_id) UNIQUE kontrolü kaldırılınca → 'Ayni_hedefe_ikinci_acilis_REDDEDILIR' KIRMIZI
- Prova jetonu kontrolü kaldırılınca → 'Prova_jetonu_olmadan_uygula_400' KIRMIZI
- Mühür kontrolü kaldırılınca → 'Muhurlu_kiracida_acilis_uygula_409' KIRMIZI
- RolGerekli("SAHIP") kaldırılınca → 'TEZGAHTAR_acilis_uygula_403' KIRMIZI
- Gerekçe uzunluk kontrolü servisten alınınca → 'Gerekce_19_karakter_400' KIRMIZI
- Transaction kaldırılıp satır satır SaveChanges yapılınca → 'Bir_satir_patlarsa_TUM_oturum_geri_alinir' KIRMIZI
- Uygula düğmesi disabled="@(false)" yapılınca → 'Prova_temiz_degilken_Uygula_dugmesi_KAPALI_ve_sebebi_yazili' KIRMIZI (UX-S16 uyarısı: mutasyon izole kurulur)
- DuzeltmePolitikasi'ndan ACILIS satırı silinince → 'Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur' KIRMIZI
- HasGrami işareti ters çevrilince → 'Acilis_isaret_sozlesmesi_VERESIYE_ile_ayni_yonde' KIRMIZI
- Ürün kartı yoksa otomatik ürün üretilince → 'Urun_karti_yoksa_stok_acilis_satiri_HATA_olur' KIRMIZI

## Canlı doğrulama
- POST /api/v1/kurulum/acilis/prova (2 hatalı + 3 geçerli satır) → 200; yanıtta hatali=2, uygulanacak=3; SELECT COUNT(*) FROM hareketler WHERE tur='ACILIS' → değişmemiş olmalı
- POST /api/v1/kurulum/acilis/uygula (temiz jeton, gerekçe 25 krk) → 200; SELECT tur, tl_tutari, nakit_tutar, veresiye_tutar FROM hareketler WHERE tur='ACILIS' → nakit_tutar=0.00 AND veresiye_tutar=0.00
- SELECT tl_bakiye, has_gram_bakiye FROM cari_bakiyeler WHERE cari_id=... → CSV'deki borc_tl/borc_has_gram ile birebir eşleşmeli
- SELECT kaynak, birim_maliyet_tl, birim_maliyet_has FROM stok_partileri WHERE kaynak='DEVIR' → birim_maliyet_tl IS NULL (maliyet kolonu boş bırakılan satırlar için)
- SELECT k.bakiye, kh.hareket_id FROM kasalar k JOIN kasa_hareketleri kh ON kh.kasa_id=k.id → hareket_id DOLU ve ACILIS hareketine işaret etmeli
- Aynı gövdeyi ikinci kez POST → 409; SELECT COUNT(*) FROM acilis_kalemleri WHERE hedef_id=... → 1
- TEZGAHTAR jetonuyla POST /kurulum/acilis/uygula → 403; MUDUR ile → 403 (yalnız SAHIP)
- GET /api/v1/tevsik/gunluk-ozet (ya da eşdeğeri — DOĞRULANMALI) açılış öncesi/sonrası aynı nakit toplamını vermeli
- POST /api/v1/kurulum/muhurle → 200; ardından POST /acilis/uygula → 409
- SELECT COUNT(*) FROM denetim_kayitlari WHERE varlik='ACILIS' → uygulanan kalem sayısı + 1 (mühür)
- Test kiracısı silinir (kurulum_durumu, acilis_kalemleri, hareketler, stok_partileri dahil) ve silindiği SQL ile kanıtlanır

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisKaynaklari.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Kurulum.cs (YENI — KurulumDurumu + AcilisKalemi varlıkları)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs (DEGISTIR — Sabitler.HareketTurleri += ACILIS)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DEGISTIR — 2 DbSet + kiracı süzgeci)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs (DEGISTIR — HareketGir bagliHareketId)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs · Modules/Yetki/YetkiModulu.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Kurulum.razor · Bilesenler/AcilisYukleyici.razor (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor · Layout/KenarCubugu.razor · Kuyumcu.Core/I18n/Sozlukler.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/AcilisBakiyeTestleri.cs · KurulumHttpTestleri.cs · KurulumEkranTestleri.cs (YENI) · dotnet/denetim.sh (DEGISTIR)`

## Fazlar
- **Faz A — sevk edilebilir asgari** (Dalga 3, 47-A ile birlikte): ACILIS türü + DuzeltmePolitikasi kaydı; kurulum_durumu ve acilis_kalemleri tabloları; kasa/banka ve cari açılış (kuru prova + uygula + mühür); /kurulum sihirbazı adım 1-4 ve 6; SAHIP kapısı; denetim izi; DB+HTTP+bUnit testleri ve mutasyon kanıtları. Bu haliyle yeni dükkân ilk gününü kurabilir.
- **Faz B** (Faz A yeşil + 47-A Faz B bittikten sonra): Stok açılışı (DEVIR partisi + NULL maliyet), CSV şablon/toplu yükleme yolu, oturum geri alma (48-A storno ile), /isletme durum kartı, basit mod cilası, açılış devri raporu ayrımı (34-A/35-A ile).
## Mevzuat dayanağı
- VUK md.185-186 — dönem başı/işe başlama envanteri ve açılış bilançosu — **ORTA — DOĞRULANMALI (madde numarası hafızadan; mali müşavir teyidi gerekir)** — Açılış stoğu ve bakiyelerinin kayıt altına alınması bu paketin ticari gerekçesidir; program 'envanter defteri' üretmez, yalnız veriyi doğru taşır.
- VUK md.219 — kayıtların belgeye dayanması ve zamanında yapılması — **YÜKSEK (projede UX-S2 gerekçesi olarak zaten kullanılıyor, MIMARI-KALAN-PAKETLER:391)** — Açılış kaydı yerinde düzenlenemez; düzeltme ters kayıtla (48-A) yapılır. DuzeltmePolitikasi'nda ACILIS DUZENLENEBILIR olamaz.
- 459 sıra no.lu VUK Genel Tebliği — tevsik zorunluluğu (nakit had) — **YÜKSEK — kodda kanıtlı (TevsikKapisi.GunlukNakitToplam, Hareket.NakitTutar yorumu Entities.cs:320-340)** — Açılışta para EL DEĞİŞTİRMEZ → OdemeIzi.Yok; kümülatif nakit toplamı etkilenmez. Aksi tasarım haddi sahte biçimde doldururdu.
- MASAK — 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **ORTA-YÜKSEK — PAKET-47:159'da 'DOGRULANDI' notu var, tutar DOĞRULANMALI** — Açılış geçmiş İŞLEM aktarmadığı için MASAK kapısı tetiklenmez; geçmiş hareket göçü (seçenek 3) bu kümülatifi anlamsızlaştırdığı için reddedildi.
- 6698 KVKK — kurulumda toplu müşteri verisi girişi — **ORTA** — Cari açılış dosyası toplu kişisel veri taşır → UX-S20 görsel rejimi (kırmızı kart + 'bu görüntüleme kayda geçer') ve okuma izi uygulanmalı.

## Riskler
- MÜHÜR 2 ile PAKET-47 spesifikasyonu ÇELİŞİYOR: mühür 'ACILIS türü', PAKET-47:75 ise 'CariServisi.Duzeltme üzerinden CARI_DUZELTME' diyor. Bu tasarım mührü esas alır (yeni tür), ancak CariServisi.Duzeltme'nin tür parametreleştirilmesi gerekir — aksi hâlde ikinci yazma yolu doğar.
- Yeni hareket türü 15 yeri etkiler: rapor süzgeçleri, kâr-zarar, cari ekstre, dışa aktarım kaynakları ACILIS'i tanımazsa satırlar 'bilinmeyen tür' olarak sessizce düşer (34-A/35-A önkoşulunun sebebi).
- Açılış bakiyesi kredi limiti kapısına takılabilir (VadeKapisi.KrediLimitiDogrula, CariModulu.cs:574): mevcut borcu limitinin üstünde olan gerçek müşteri açılışta reddedilir. Açılış için kapı 'uyar ama geçir' mi olmalı — açık soru.
- Kasa açılışında GunKapanisiKapisi devrede (KasaModulu.cs:145): kurulumu gün kapanışından sonra yapan kullanıcı 409 alır ve sebebi anlaşılmazsa destek çağrısı doğar.
- acilis_kalemleri UNIQUE kısıtı, meşru ikinci parti açılışını (aynı ürün iki şubede) yanlış reddedebilir — hedef_id'ye şube boyutu eklenmeli (STOK için (sube_id, urun_id)).
- Zehirleme yüzeyi (Mehmet'in 47 gerekçesi): uydurma açılış bakiyesi kâr-zararı ve stok değerini kalıcı bozar. Mühür + SAHIP + gerekçe + kuru prova + denetim izi bunun karşılığıdır; mühürsüz bırakılırsa risk açık kalır.
- bUnit sahte API'ye yeni uçlar eklenmezse mevcut ekran testleri JsonException ile kırılır (UX-S17).
- Çok kiracılı testte açılış kalemleri temizlenmezse sonraki test koşusunda bakiye artıkları kalır (test kiracısı silme adımı zorunlu).

## Açık sorular
- ACILIS türü mü, CARI_DUZELTME mi? Mühür 2 ACILIS diyor, PAKET-47:75 CARI_DUZELTME. Öneri: ACILIS (mühür bağlayıcı) + CariServisi.Duzeltme'ye 'tur' parametresi — ekip oyuna sunulmalı.
- Açılışta kredi limiti kapısı uygulanacak mı? Öneri: uygulanmaz (mevcut borcun kaydı yeni risk doğurmaz), ancak uyarı satırı prova raporunda çıkar.
- Geri alma 48-A storno'su ile mi yoksa oturum iptali (kalem durumu GERI_ALINDI + ters ACILIS) ile mi? 48-A sevk edilmezse T-2 Faz B geri alması yalnız 'mühürsüz oturum' ile sınırlı kalır.
- Mühür zorunlu mu, opsiyonel mi? Zorunlu tutulursa kademeli kurulum yapan dükkân (stoğu bir hafta sonra girer) tıkanır.
- Açılış CSV satır tavanı ve oran sınırı kaç? Testler rakamı sabitlemeyip yapılandırmadan okumalı (Aşkın'ın 47 koşulu).
- Kur snapshot yoksa (ilk gün, kur hiç çekilmemiş) açılış nasıl yazılır? Öneri: MANUAL snapshot oluşturma adımı sihirbaza eklenir — DOĞRULANMALI.
- Açılış stoğunda maliyet girilirse has karşılığı hangi kurla hesaplanır — açılış anı mı, kullanıcının verdiği devir tarihi mi?
- 'kurulum' modül yetkisi yeni anahtar olarak açılsın mı, yoksa mevcut 'admin'/SAHIP kapısı yeterli mi (K4-F yetki tohumlama borcu)?

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — Sihirbazın 'ince sürücü' olması, açılışın OdemeIzi.Yok ile yazılması ve DEVIR partisinde maliyetin NULL kalması mimari olarak doğru; mevcut servisleri yeniden kullanıyor, ikinci defter yolu açmıyor. İki mimari düzeltme şart: (a) 'yeni tür eklemek fail-closed kapıdır' iddiası kodda karşılıksız, (b) PAKET-47 ile tür çelişkisi tasarımın kendi risk listesinde açık duruyor. Kapsam da iki fazla doğru bölünmüş, Faz A tek başına sevk edilebilir.
  - Koşullar: FAIL-CLOSED 1 dili düzeltilsin: Sabitler.HareketTurleri kodda zorlanmıyor (Entities.cs:31-32). Gerçek kapı denetim.sh sayacı + DuzeltmePolitikasi tam-örtüşme testidir; 'diziye ekledik' kapı sayılmasın. · DuzeltmePolitikasi kaydı mevcut T() imzasına uysun: 'GerekceAnahtarlari.Defter|StornoYok' diye bir bit birleşimi yok (DuzeltmePolitikasi.cs:150-165). Tek anahtar seçilsin: 48-A yoksa StornoYok, varsa Defter + hedef uç. · ACILIS vs CARI_DUZELTME çelişkisi kod yazılmadan mühürlensin. ACILIS seçilirse CariServisi.Duzeltme'ye 'tur' parametresi geçirilmeli; ayrı bir açılış yazma yolu açılırsa ikiz defter yolu doğar. · Kasa bacağı için KasaServisi.HareketGir parametrelemesi yerine CariModulu.cs:565-580'deki mevcut 'aynı transaction içinde KasaHareket.Add' kalıbı esas alınsın; iki farklı kasa yazma sözleşmesi doğmasın. · acilis_kalemleri UNIQUE anahtarı STOK için (tenant_id, tur, sube_id, urun_id) olmalı; (tur, hedef_id) aynı ürünün ikinci şubedeki meşru açılışını reddeder. · Kur anlık görüntüsü yoksa (ilk gün) KurDogrula kapısı kurulumu tıkar: MANUAL snapshot adımı Faz A'ya alınsın, Faz B'ye bırakılmasın (Sabitler.KurKaynaklari'nda MANUAL zaten var — Entities.cs:37).
- **YENİ: GÜVENLİK (Mehmet) — kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek şifreleme, içe aktarım riski, abonelik/plan kapılarının yazma yolunu bozmaması, telemetri/hata paketinde kişisel veri, MFA koşulu.: KOŞULLU** — Kuru prova + tek transaction + SAHIP-only + gerekçe≥20 + append-only denetim izi + UNIQUE kısıt sağlam bir zehirleme-karşıtı iskelet kuruyor. Ancak tasarımın kendi 'riskler' bölümü mührün çelişkili olduğunu (Mühür 2 vs PAKET-47:75) itiraf ediyor; bu, ikinci bir gizli yazma yolu doğurma potansiyeli taşıdığından EVET değil KOŞULLU veriyorum — ilk üç koşul kodlamadan önce kapatılmalı.
  - Koşullar: acik_sorular'daki 'ACILIS mi CARI_DUZELTME mi' çelişkisi KODLAMAYA BAŞLAMADAN ekip oyuyla çözülmeli — iki farklı yazma yolu (HareketFabrikasi.Ekle vs CariServisi.Duzeltme) aynı anda var olursa denetim izinde tutarsızlık ve zehirleme yüzeyi genişler. · Mühür ZORUNLU olmalı, opsiyonel değil: mühürsüz kalan kiracıda açılış uçları süresiz açık kalır, bu da 'aynı hedefe ikinci açılış' (UNIQUE) dışında sınırsız düzeltme penceresi anlamına gelir — Mehmet kapısı olarak mühür süresi (ör. 30 gün) sınırlanmalı ya da SAHIP dışı ikinci onay eklenmelidir. · Kredi limiti kapısının açılışta 'uyar ama geçir' mi 'reddeder' mi olacağı açık soru — reddetmeme kararı veriliyorsa, prova raporunda bu satırın SAHIP tarafından görüldüğüne dair ayrı bir onay adımı (gerekçe) eklenmeli. · acilis_kalemleri UNIQUE (tenant_id, tur, hedef_id) kısıtının STOK için (sube_id, urun_id) boyutunu içermediği risk olarak belirtilmiş — bu, meşru çok şubeli açılışı değil ama YANLIŞ yapılandırılırsa ikinci bir kiracının stoğuna yanlış hedef_id ile yazılabileceği ihtimalini de kapsıyor mu netleştirilmeli (kiracı izolasyonu HasQueryFilter ile ayrı garanti altında, ama hedef_id çakışması test edilmeli). · Cari açılış CSV'si toplu kişisel veri taşıyor (KVKK notu) — okuma izi (UX-S20 görsel rejimi) testlerde (KurulumEkranTestleri.cs) somut senaryo olarak YOK, eklenmeli.
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: KOSULLU** — Test/mutasyon kanıtı yapısı (8 DB + 8 HTTP + 6 bUnit + 4 saf) ve canlı doğrulamanın somutluğu (SQL COUNT, kasa hareket_id doldurma, denetim_kayitlari sayısı) kaliteli. Ancak tasarımın kendi içindeki kredi limiti kapısı çelişkisi ve STOK UNIQUE kısıtı itirafı, mühürlenmeden önce kapatılması gereken gerçek kalite açıkları; ikisi de şu an hiçbir testle güvence altına alınmamış.
  - Koşullar: 'uçlar' bölümü POST /api/v1/kurulum/acilis/uygula için KrediLimitiDogrula'yı zorunlu kapı sayarken 'açık_sorular' bölümü aynı kararı 'uygulanmaz' öneriyor — bu çelişki netleşmeden ilgili test yazılamaz, kararsızlık riski taşır. · AcilisBakiyeTestleri.cs listesinde kredi limiti aşımı senaryosu yok; karar netleşince en az bir mutasyon-kanıtlı DB testi eklenmeli. · KurulumHttpTestleri.cs'te yalnız TEZGAHTAR_acilis_uygula_403 var; canlı doğrulama listesinde MUDUR jetonuyla da 403 bekleniyor ama buna karşılık otomatik bir HTTP testi yok. · acilis_kalemleri tablosunun UNIQUE(tenant_id,tur,hedef_id) kısıtı STOK türü için tasarımın kendi 'riskler' bölümünde (sube_id,urun_id) olması gerektiği itiraf ediliyor; bu düzeltme Faz B kabul kriterine ve ilgili teste yazılı olarak bağlanmalı, yoksa 'Urun_karti_yoksa_stok_acilis_satiri_HATA_olur' testi yanlış şemayı maskeler. · Sabitler.HareketTurleri dizisinin runtime'da zorlanmadığı (Entities.cs:31 'dizi kodda ZORLANMAZ') gerçeğiyle T-2'nin 'ACILIS türü eklenir' maddesinin belge-değeri mi yoksa fail-closed kapı mı olduğu ayrımı testte/DoD'da netleştirilmeli.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- ACILIS mi CARI_DUZELTME mi çelişkisi KOD YAZILMADAN mühürlenir; tek yazma yolu (HareketFabrikasi.Ekle üzerinden) seçilir, ikinci açılış yazma yolu açılmaz.
- FAIL-CLOSED 1 dili düzeltilir: Sabitler.HareketTurleri kodda ZORLANMAZ (Entities.cs:31-32). Gerçek kapı denetim.sh sayacı + DuzeltmePolitikasi tam-örtüşme testidir.
- DuzeltmePolitikasi kaydı mevcut T() imzasına uyar: 'Defter|StornoYok' bit birleşimi yoktur; 48-A yoksa StornoYok, varsa Defter + hedef uç.
- Kasa bacağı CariModulu.cs:565-580'deki mevcut 'aynı transaction içinde KasaHareket.Add' kalıbını kullanır; ikinci kasa yazma sözleşmesi doğmaz.
- acilis_kalemleri UNIQUE anahtarı STOK için (tenant_id, tur, sube_id, urun_id) olur; bu düzeltme Faz B kabul kriterine ve testine yazılı bağlanır.
- Mühür ZORUNLUDUR (opsiyonel değil): süre sınırı (ör. 30 gün) ya da SAHIP dışı ikinci onay eklenir; 409 kodu T-1'in /saglik/hazir semantiğinden ayrı hata koduyla ayrışır.
- Kredi limiti kapısı kararı ('uyar ama geçir' mi 'reddet' mi) uçlar/açık_sorular çelişkisi giderilerek mühürlenir ve en az bir mutasyon kanıtlı DB testine bağlanır; geçirme seçilirse prova raporunda SAHIP onayı adımı eklenir.
- MANUAL kur anlık görüntüsü adımı Faz A'ya alınır (ilk gün KurDogrula kapısı kurulumu tıkamasın).
- Cari açılış CSV'sinin kişisel veri okuma izi (KVKK) KurulumEkranTestleri'ne somut senaryo olarak eklenir; MUDUR jetonuyla 403 HTTP testi yazılır.
- Sevk sırası: 47-A + T-2 birlikte, T-1'den ve T-3'ten ÖNCE; denetim.sh ve Entities/DbContext çakışması için yaz-seri T-2 → T-1 → T-3.
