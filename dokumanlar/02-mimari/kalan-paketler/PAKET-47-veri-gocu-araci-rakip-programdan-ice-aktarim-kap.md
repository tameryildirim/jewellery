# Paket 47 — Veri göçü aracı — rakip programdan içe aktarım (kapılı, kuru provalı)

> Katman: Katman 7 — Platform (paket 47) · Tahmin: Faz A (cari + ürün kartı, kuru prova, geri al): 2,5–3,5 gün · Faz B (açılış bakiyesi + açılış stoku + DEVIR partisi): 1,5–2,5 gün · Toplam 4–6 gün (PARITE tahminiyle uyumlu) gün · Önkoşullar: yok · Kaynak: katman mimarı K7-47 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 7
> ⚠ **VETO:** Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: YALNIZ SEÇENEK 3 (geçmiş hareket/fatura/kasa dahil serbest içe aktarım) VETO: tevsik günlük nakit kümülatifi ve MASAK bağlantılı işlem kümülatifi geriye dönük anlamsızlaşır, kur_snapshot_id uydurulur, denetim izi yalan söyler (Anayasa md.1). Seçenek 2 koşullu EVET (oylar bölümü).

## Hedef
Perakende kuyumcu bugün rakip programdan çıkarken müşteri kartlarını ve ürün kartlarını elle yeniden yazmak zorunda; bu, satış görüşmesindeki en büyük itirazdır ve pazar araştırmasında doğrulanmış şikâyetlerin ortak çekirdeğidir. Bu paket, Excel/CSV'den cari ve ürün kartlarını, ardından açılış bakiyelerini ve açılış stokunu, KURU PROVA + satır bazlı hata raporu + tek tıkla geri alınabilir oturumla sisteme alır. Amaç veriyi hızlı taşımak değil, TEK BİR doğrulanmamış kaydın bile mevzuat kapılarını atlamadan girmesini sağlamaktır.

## Kodda bugünkü durum
İçe aktarım YOKTUR. Dışa aktarım tarafı tamdır: Modules/Disaaktarim/Kaynaklar.cs'te 6 kaynak (cari:62, urun:100, satis:133, fatura:167, kasa:196, gider-pusulasi:224), DisaAktarimModulu.cs:333 [RolGerekli("SAHIP")], 346-366 talep/indirme uçları, Data/DisaAktarim.cs'te DisaAktarimTalebi varlığı. ADR-0014 'Sonuçlar' bölümü birebir: 'Geri alma (import) yoktur. Bu bilinçlidir'. Dosya yükleme altyapısı base64'tür (Modules/Urun/BelgeModulu.cs:14 'multipart altyapısı yok'), BelgeServisi.Ekle (BelgeModulu.cs:37) deseni hazır. Kart oluşturma servisleri hazır: CariServisi.Olustur (CariModulu.cs:221), UrunServisi.Olustur (UrunModulu.cs:236 TasKapisi.Dogrula çağrısıyla), CariServisi.Duzeltme (CariModulu.cs:555, CARI_DUZELTME hareketi + denetim izi), UrunServisi.StokSayimGuncelle (UrunModulu.cs:605, SAYIM_DUZELTME deftere yazar). Maliyeti bilinmeyen açılış stoku için 'DEVIR partisi + NULL maliyet' deseni migration 20260914084939_StokMaliyetPartileri.cs:105-126'da zaten kurulu ve 'SIFIR MALİYET YAZILMAZ' gerekçesi orada yazılı.

## Kapsam
**İçinde:**
- ÜÇ SEÇENEK ve oylama (aşağıda 'is_kurallari' ve 'acik_sorular')
- FAZ A: cari kartı + ürün kartı içe aktarımı (para yok, defter yok)
- CSV şablonu indirme ucu (dışa aktarım başlıklarının aynası)
- Kuru prova: satır bazlı doğrulama, hata/uyarı raporu, hiçbir yazma yok
- Uygulama: onaylı, tek işlemli (transaction), oturum kimliğiyle izlenebilir
- Yinelenen tespiti: cari için telefon+ad, ürün için Kod (SKU) — ATLA/GÜNCELLE/HATA seçenekleri
- FAZ B: açılış bakiyesi (cari TL/has) ve açılış stoku + DEVIR partisi
- Oturumu GERİ ALMA (yalnız Faz A kartları; Faz B defter yazdığı için storno ile)
- Denetim izi (hassas=true) + oran sınırı + SAHIP rolü
- İçe aktarım geçmişi ekranı

**Dışında:**
- Rakip programın veritabanına doğrudan bağlanma (MDB/DBF/SQL okuma) — her rakip için ayrı format, doğrulanamaz, bakım yükü sonsuz
- Satış/fatura/kasa hareketi geçmişinin içe aktarımı — geçmiş hareket tevsik/MASAK kümülatiflerini geriye dönük bozar, kur_snapshot_id üretilemez (PAKET-15 ve K6 ile aynı gerekçe)
- Kullanıcı/parola, refresh token, denetim izi içe aktarımı (ADR-0014 'Saklanmayanlar' listesinin aynası)
- Excel (.xlsx) ikili dosyası ayrıştırma — yeni bağımlılık; CSV ile sınırlıdır (kullanıcı Excel'den 'CSV UTF-8' kaydeder)
- e-Fatura/e-Belge verisi (ADR-0012)
- Otomatik alan eşleme tahmini (AI) — KVKK yurt dışı aktarımı sorunu (PAZAR-ARASTIRMASI-2026-09-16 satır 77)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| ice_aktarim_oturumlari | YENİ | id(36 PK) · tenant_id(36) · kullanici_id(36) · kaynak_ad(30: 'cari'|'urun'|'acilis-bakiye'|'acilis-stok') · dosya_adi(255) · dosya_sha256(64) · gerekce(500, min 20) · durum(20: PROVA|ONAYLI|UYGULANDI|GERI_ALINDI|IPTAL) · yinelenen_politikasi(10: ATLA|GUNCELLE|HATA) · toplam_satir(int) · gecerli_satir(int) · hatali_satir(int) · uygulanan_satir(int) · olusturuldu · uygulandi(null) · geri_alindi(null) · ip_adresi(45) | (tenant_id, olusturuldu) · (tenant_id, durum) | YYYYMMDDHHMMSS_IceAktarim |
| ice_aktarim_satirlari | YENİ | id(36 PK) · tenant_id(36) · oturum_id(36) · satir_no(int) · ham_json(text: dosyadaki alanlar) · sonuc(10: GECERLI|HATA|ATLANDI|UYGULANDI) · hata_kodu(40) · hata_mesaji(500) · hedef_varlik_turu(20) · hedef_varlik_id(36 null) · olusturuldu | (tenant_id, oturum_id, satir_no) · (tenant_id, oturum_id, sonuc) | aynı migration |
| stok_partileri | DEĞİŞİKLİK YOK — Faz B açılış stoku için mevcut kaynak='DEVIR' ve birim_maliyet_tl=NULL deseni kullanılır (migration 20260914084939_StokMaliyetPartileri.cs:105-126) | - | - | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/ice-aktarim/sablonlar` | SAHIP | Desteklenen kaynaklar + zorunlu/isteğe bağlı kolon listesi (dışa aktarım Basliklar dizisinin aynası) | — |
| GET | `/api/v1/ice-aktarim/sablon/{kaynak}.csv` | SAHIP | Boş CSV şablonu (UTF-8 BOM, başlık satırı) | — |
| POST | `/api/v1/ice-aktarim/prova` | SAHIP | Base64 CSV + kaynak + gerekçe + yinelenen politikası → oturum açar, satırları doğrular, HİÇBİR ŞEY YAZMAZ (hedef tablolara). Yanıt: oturumId + sayaçlar + ilk 100 hata | OranSiniri.IceAktarim · gerekçe ≥20 · dosya ≤ yapılandırılabilir tavan |
| GET | `/api/v1/ice-aktarim/{oturumId}/satirlar?sonuc=&limit=&offset=` | SAHIP | Satır sonuçları sayfalı (TB-19: limit zorunlu, tavan 200) | — |
| POST | `/api/v1/ice-aktarim/{oturumId}/uygula` | SAHIP | Onay metni + gerekçe tekrarı ile GEÇERLİ satırları uygular; her satır ilgili mevcut servis üzerinden yazılır | OranSiniri.IceAktarim · durum=PROVA olmalı · onay metni birebir |
| POST | `/api/v1/ice-aktarim/{oturumId}/geri-al` | SAHIP | FAZ A: bu oturumda OLUŞTURULAN kartları deaktif eder (soft delete). Oturumda GÜNCELLENEN kayıt varsa geri alma REDDEDİLİR | durum=UYGULANDI · yalnız kaynak∈{cari,urun} |
| GET | `/api/v1/ice-aktarim/gecmis?limit=` | SAHIP | Kim ne zaman ne aktardı (dışa aktarım /gecmis aynası) | — |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Data/IceAktarim.cs` — IceAktarimOturumu ve IceAktarimSatiri varlıkları, ikisi de : IKiraciVarlik. Data/DisaAktarim.cs dosyasının belge/gerekçe üslubu birebir taklit edilir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki DbSet eklenir (satır 85 DisaAktarimTalepleri'nin altına) — global filtre satır 98'deki jenerik KiraciFiltresi ile otomatik gelir; indeksler OnModelCreating'de.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Iceaktarim/CsvOkuyucu.cs` — RFC 4180 ayrıştırıcı: BOM atlama, tırnaklı alan, gömülü virgül/yeni satır, CRLF; baştaki CSV-enjeksiyon tek tırnağını söker; sayı/tarih ayrıştırma InvariantCulture; satır satır IAsyncEnumerable — ToListAsync yok. CsvYazici.cs'in tersidir, aynı klasörde değil YENİ Iceaktarim klasöründe (dışa aktarım modülü SAHIP-only ve kapalı bir bütündür).
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Iceaktarim/Kaynaklar.cs` — IIceAktarilabilir sözleşmesi: Ad, Basliklar (zorunlu/opsiyonel), SatirDogrula(ham) → (gecerli, hataKodu, dto), SatirUygula(servisler, dto, islem) → varlikId. Faz A'da CariKaynagi + UrunKaynagi; Faz B'de AcilisBakiyeKaynagi + AcilisStokKaynagi.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Iceaktarim/IceAktarimModulu.cs` — IceAktarimServisi (prova/uygula/geri-al/gecmis) + IceAktarimController. Sınıf düzeyinde [RolGerekli("SAHIP")]. Servis KENDİ BAŞINA Add yapmaz; CariServisi/UrunServisi'ni DI ile alır ve satır başına onların Olustur/Guncelle metodunu çağırır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Iceaktarim.IceAktarimServisi>() — Disaaktarim kaydının (satır 30) hemen altına.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Common/OranSiniri.cs` — public const string IceAktarim = "ice-aktarim"; + Program.cs'te politika kaydı (DisaAktarim politikasının yanına, daha düşük kota).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Yeni oran sınırı politikası; istek gövde tavanı içe aktarım ucunda yapılandırmadan (BelgeAyarlari deseni).
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — IceAktarim bölümü: MaksSatir, MaksDosyaBayt, OranSiniriSaatlik — ADR-0005 (eşikler koda gömülmez) ile aynı yaklaşım.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — SozlukIceAktarim sınıfı (TR+EN) + Nav.IceAktarim. SozlukDisaAktarim (satır 951) deseni birebir.
- **YENİ** `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor` — 3 adım: dosya seç → kuru prova sonucu (sayaçlar + hata tablosu) → onayla ve uygula. Şablon indirme bağlantısı. VeriDisaAktarim.razor deseni.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — SAHIP bloğuna (satır 68-74) '📥 @Dil.T.Nav.IceAktarim' bağlantısı, /veri-disa-aktarim satırının (74) hemen altına.
- **YENİ** `dotnet/Kuyumcu.Api/Data/Migrations/YYYYMMDDHHMMSS_IceAktarim.cs` — İki tablo + indeksler. ⚠ Önce dotnet build yeşil, sonra build'li migrations add (SONRAKI-OTURUM-PROMPT §6 tuzağı).

## Ekranlar
- `/veri-ice-aktarim` · `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor` — Kaynak seç · şablon indir · CSV yükle (base64) · gerekçe · yinelenen politikası · KURU PROVA çalıştır · satır bazlı hata tablosu · onay kutusu + 'UYGULA' · sonuç özeti · geri al düğmesi (yalnız Faz A) (menü/yetki: Yalnız SAHIP (KenarCubugu SAHIP bloğu); sunucu [RolGerekli("SAHIP")] son kapıdır)
- `/veri-ice-aktarim/gecmis` · `aynı dosya içinde sekme` — Geçmiş oturumlar: tarih, kaynak, dosya adı, sayaçlar, durum, gerekçe (menü/yetki: SAHIP)

## İş kuralları (fail-closed)
- ÜÇ SEÇENEK (oylama karar verecek) — (1) ADR KORUNUR + YALNIZ AÇILIŞ BAKİYESİ: hiçbir dosya okunmaz, yalnız mevcut CariServisi.Duzeltme ekranı 'açılış bakiyesi' olarak konumlandırılır. Artı: 0 yeni yazma yüzeyi, ADR'ye dokunulmaz, ~1 gün. Eksi: rakip vaadinin (ücretsiz veri aktarımı) karşılığı YOK, 500 müşteriyi elle yazma sorunu duruyor, pazar itirazı kapanmıyor. (2) KAPILI İÇE AKTARIM (ÖNERİM): her satır mevcut servis+kapılardan geçer, kuru prova zorunlu, geçmiş hareket aktarılmaz. Artı: ticari itiraz kapanır, mevzuat kapıları korunur, geri alınabilir. Eksi: 4-6 gün, yeni iki tablo, yeni ekran; ADR-0014'ün tek cümlesi için ADR-0016 gerekir. (3) ADR REVİZYONU (geniş): geçmiş hareketler dahil serbest içe aktarım. Artı: tam göç. Eksi: tevsik/MASAK kümülatifleri geriye dönük anlamsızlaşır, kur_snapshot_id uydurulur, denetim izi yalan söyler — Anayasa md.1'e aykırı; MİMAR OLARAK REDDEDİYORUM.
- FAIL-CLOSED 1: Doğrulanamayan satır UYGULANMAZ; kısmi satır kabul edilmez (yarım cari kartı yok).
- FAIL-CLOSED 2: Taş ağırlığı > 0 olup TasKokeni boş olan ürün satırı HATA'dır (ADR-0013; UrunModulu.cs:236 TasKapisi.Dogrula zaten fırlatır — içe aktarım bu istisnayı satır hatasına çevirir, YUTMAZ).
- FAIL-CLOSED 3: İçe aktarım hareketler tablosuna DOĞRUDAN yazamaz. Faz B açılış bakiyesi CariServisi.Duzeltme üzerinden CARI_DUZELTME olarak, açılış stoku UrunServisi.StokSayimGuncelle üzerinden SAYIM_DUZELTME olarak yazılır — ikisi de para el değiştirmeyen düzeltmedir (CariModulu.cs:173-177 gerekçesi).
- FAIL-CLOSED 4: Açılış stoku için maliyet SIFIR YAZILMAZ; kaynak='DEVIR', birim_maliyet_tl=NULL (migration 20260914084939:105-113 gerekçesi). Kullanıcı maliyet kolonu doldurduysa o kullanılır; boşsa NULL kalır.
- İŞARET SÖZLEŞMESİ: açılış bakiyesinde 'borc_tl' pozitif = müşteri bize borçlu → CariDuzeltmeDto.TlDelta pozitif (CariModulu.cs:180 'Pozitif = cari bize daha çok borçlanır'). 'borc_has_gram' pozitif = altın borcu artar → HasDelta pozitif (CariModulu.cs:183).
- GEREKÇE: her oturum ≥20 karakter gerekçe ister (dışa aktarımla simetrik, ADR-0014 Şart 1).
- KUR: Faz B her satır için tek bir kur_snapshot_id kullanır — oturum açılırken alınan güncel snapshot; satır başına kur uydurulmaz. Snapshot yoksa oturum açılmaz (tuzak: yeni kiracıda kur 10 sn sonra oluşur).
- GÜN KİLİDİ: Faz B uygulaması IslemDogrulayici.KasaDogrula/GunKapanisiKapisi'na tabi değildir çünkü kasa bacağı yoktur; ancak kapanmış güne tarih yazılmaz — tüm açılış kayıtları UYGULAMA ANI (UtcNow) ile tarihlenir, kullanıcı tarihi kabul edilmez.
- DEĞİŞMEZ: bir oturumun tüm satırları TEK transaction'da uygulanır; herhangi bir satır beklenmedik istisna atarsa tümü geri alınır ve oturum IPTAL olur (kısmi göç, en kötü hâldir).
- YİNELENEN: cari eşleşmesi telefon (normalize) + ad (TurkceMetin); ürün eşleşmesi Kod. Politika HATA ise eşleşen satır reddedilir, ATLA ise sayılır ve geçilir, GUNCELLE ise mevcut kart PATCH edilir ve geri alma kapanır.
- KİŞİSEL VERİ: yüklenen ham CSV SUNUCUDA SAKLANMAZ; yalnız ham_json satır tablosunda ve oturum UYGULANDI/IPTAL olduktan sonra ADR-0008 saklama kuralına bağlanır. Dosyanın kendisi diske yazılmaz (ADR-0014 'Saklanmayanlar' simetrisi).
- KVKK: içe aktarılan kimlik alanları (vergi no/TCKN) ADR-0003 alan şifrelemesinden geçer — servis yolu kullanıldığı için otomatiktir; içe aktarım kendi yazarsa bu kaybolur (fail-closed 3'ün ikinci gerekçesi).

## Testler
- `dotnet/Kuyumcu.Tests/IceAktarimTestleri.cs` [DB]
  - PROVA_hicbir_kayit_YAZMAZ_cari_sayisi_degismez
  - Tas_agirligi_var_kokeni_yok_satir_HATA_ADR0013
  - Gecersiz_ayar_kodu_satiri_HATA_diger_satirlar_GECERLI
  - Yinelenen_kod_politikasi_HATA_ise_satir_reddedilir
  - Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK
  - Uygulanan_cari_kartinda_vergi_no_SIFRELI_saklanir
  - Acilis_bakiyesi_CARI_DUZELTME_hareketi_uretir_ve_bakiyeyi_gunceller
  - Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir
  - Geri_al_yalniz_bu_oturumun_kartlarini_deaktif_eder
  - GUNCELLE_politikasi_kullanilmissa_geri_al_REDDEDILIR
  - Baska_kiracinin_oturumu_okunamaz_404
  - Satir_listesi_limitsiz_cagrilamaz_TB19
- `dotnet/Kuyumcu.Tests/IceAktarimHttpTestleri.cs` [HTTP]
  - MUDUR_dahi_ice_aktaramaz_yalniz_SAHIP_403
  - Gerekcesiz_prova_400
  - Onay_metni_YANLIS_ise_uygula_400
  - Oran_siniri_asilinca_429
  - Dosya_tavani_asilinca_400_ve_hicbir_oturum_acilmaz
  - Gecersiz_base64_400
  - Uygulanan_oturum_IKINCI_kez_uygulanamaz_409
- `dotnet/Kuyumcu.Tests/CsvOkuyucuTestleri.cs` [saf]
  - BOM_atlanir
  - Tirnakli_alanda_virgul_ve_yeni_satir_korunur
  - Bastaki_CSV_enjeksiyon_tirnagi_SOKULUR
  - Ondalik_NOKTA_ile_okunur_tr_TR_kulturunde_de
  - ISO8601_UTC_tarih_okunur
  - Bilinmeyen_kolon_HATA_degil_UYARI_zorunlu_kolon_eksikse_HATA
- `dotnet/Kuyumcu.Tests/IceAktarimEkranTestleri.cs` [bUnit]
  - Prova_sonucu_hata_tablosunda_satir_no_ve_mesaj_gorunur
  - Hatali_satir_varken_UYGULA_dugmesi_onay_kutusu_isaretlenmeden_pasif
  - SAHIP_olmayan_kullanicida_menu_baglantisi_YOK

## Mutasyon kanıtları
- CsvOkuyucu'daki zorunlu kolon kontrolü kaldırılınca → 'Bilinmeyen_kolon_HATA_degil_UYARI_zorunlu_kolon_eksikse_HATA' KIRMIZI
- Satır uygulamasında CariServisi.Olustur yerine doğrudan db.Cariler.Add yazılınca → 'Uygulanan_cari_kartinda_vergi_no_SIFRELI_saklanir' KIRMIZI (ve denetim.sh'ta Hareketler.Add sayımı Faz B'de bozulur)
- TasKapisi istisnası try/catch ile yutulunca → 'Tas_agirligi_var_kokeni_yok_satir_HATA_ADR0013' KIRMIZI
- Transaction kaldırılınca (satır satır SaveChanges) → 'Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK' KIRMIZI
- DEVIR partisinde NULL yerine 0 maliyet yazılınca → 'Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir' KIRMIZI
- [RolGerekli("SAHIP")] MUDUR'ü de kapsayacak şekilde genişletilince → 'MUDUR_dahi_ice_aktaramaz' KIRMIZI
- Elle TenantId şartı oturum sorgusundan silinince → global filtre tutar (test YEŞİL kalır); global filtre de devre dışı bırakılınca 'Baska_kiracinin_oturumu_okunamaz_404' KIRMIZI (ADR-0010 iki katman kanıtı)

## Canlı doğrulama
- API'yi --urls http://localhost:5199 ile başlat, SAHİP olarak giriş yap, /kur/guncel id gelene kadar bekle (tuzak: yeni kiracıda kur 10 sn)
- GET /api/v1/ice-aktarim/sablon/cari.csv → dosyayı indir, Excel'de aç, 3 gerçek müşteri + 1 kasıtlı hatalı satır (taş ağırlığı var kökeni yok) yaz, 'CSV UTF-8' kaydet
- POST /prova → yanıtta toplamSatir=4, gecerli=3, hatali=1 ve hata satırının satirNo'su doğru mu; ARDINDAN GET /api/v1/cari?limit=200 ile cari sayısının DEĞİŞMEDİĞİNİ göster (kuru prova kanıtı)
- POST /uygula → 3 cari oluştu; GET /api/v1/cari ile doğrula; MySQL'de cariler.vergi_no sütununun DÜZ METİN OLMADIĞINI göster (değeri ekrana basma, yalnız 'şifreli/şifresiz' de)
- GET /denetim-izi ekranında 'IceAktarim' kayıtlarının gerekçeyle göründüğünü gör
- POST /geri-al → 3 cari deaktif; GET /api/v1/cari?aktif=true ile doğrula
- Faz B: açılış bakiyesi CSV'si uygula → cari ekstresinde CARI_DUZELTME satırı ve bakiye; GET /raporlar/cari-yaslandirma'da 'vadesi bilinmeyen bakiye' olarak görünmeli
- MUDUR jetonuyla POST /prova → 403
- 11 kez arka arkaya /prova → 429
- Test kiracısını sil (API'yi önce durdur — kur zamanlayıcısı yazmaya devam eder)

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/OranSiniri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.Development.json`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/app.css (yalnız yeni sınıf gerekirse; önce grep — .uyari-kutu ZATEN var, app.css:392)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs (Faz B: Duzeltme servis olarak çağrılır; DEĞİŞTİRİLMEZ, yalnız DI ile alınır — değişiklik gerekirse ayrıca oylanır)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs (aynı gerekçe)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/adr/0016-veri-ice-aktarimi.md (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/adr/README.md (kayıt tablosuna 0016 satırı + 0014'ün import cümlesinin geçersizliği notu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh (DEĞİŞİKLİK GEREKMEZ — IKiraciVarlik sayacı alt sınır, satır 69-70)`

## Mevzuat dayanağı
- MASAK 185.000 TL kimlik tespiti eşiği ve bağlantılı işlem kümülatifi — **DOGRULANDI** — İçe aktarım geçmiş İŞLEM aktarmadığı için MASAK kapısı tetiklenmez; açılış bakiyesi para el değiştirmeyen düzeltmedir (CariModulu.cs:173-177 ile aynı gerekçe). Seçenek 3 seçilirse bu kümülatif geriye dönük ANLAMSIZLAŞIR — reddetme gerekçemin çekirdeği budur.
- 459 s. VUK Genel Tebliği — 30.000 TL tevsik haddi — **DOGRULANDI** — Aynı gerekçe: içe aktarımda nakit payı doğmaz, TevsikKapisi.Dogrula çağrılmaz. Geçmiş nakit satış aktarılsaydı günlük kümülatif yanlış hesaplanırdı.
- Kuyum Ticareti Yönetmeliği md.11/(e) — kıymetli taş kökeni beyanı (kodda: UrunModulu.cs:51-52 yorumu) — **DOGRULANMALI** — Kodda RG 24.06.2026/33290 atfı var; bu paket yeni bir hüküm EKLEMEZ, yalnız mevcut TasKapisi'ni içe aktarım yolunda da zorunlu kılar. Atfın birincil metinden teyidi bu paketin işi değildir.
- KVKK — yurt dışına aktarım için hiçbir ülkeye yeterlilik kararı yok (kvkk.gov.tr, PAZAR-ARASTIRMASI-2026-09-16 satır 77) — **DOGRULANDI** — Alan eşlemesi için AI/dış servis KULLANILMAZ; eşleme kullanıcı tarafından ekrandan yapılır.
- ADR-0008 saklama süresi / maskeleme — **DOGRULANDI (iç karar)** — ice_aktarim_satirlari.ham_json kişisel veri içerir; saklama politikasına bağlanmalı. Süre değeri bu pakette UYDURULMAZ, ADR-0008'deki değer kullanılır.

## Riskler
- Rakip CSV'lerinin kolon adları bilinmiyor (hiçbir rakip ürünün içi görülmedi — PARITE §6). Bu yüzden 'otomatik tanıma' vaat edilmez; kullanıcı bizim şablonumuza kopyalar. Bu, satış konuşmasında 'tek tık göç' diyemeyeceğimiz anlamına gelir — dürüst vaat: 'şablona yapıştır, biz kontrol edelim'.
- İçe aktarım, dışa aktarımın güvenlik yüzeyinin AYNASI değil TERSİDİR: sızdırma değil, ZEHİRLEME riski (uydurma cari, sahte stok). Mehmet'in (06 Güvenlik) vetosu bu pakette de beklenmelidir; gerekçe+SAHİP+kuru prova+geri al+denetim izi dörtlüsü onun altı şartının karşılığı olarak tasarlandı ama ONAYI ALINMADI.
- Faz B'de büyük dosya (ör. 5.000 ürün) tek transaction'da uygulanırsa MySQL kilit süresi ve bellek sorun olabilir. Tavan yapılandırmadan gelir; ölçülmeden 'sınırsız' denmez.
- GUNCELLE politikası geri almayı imkânsızlaştırır — kullanıcı bunu ekranda anlamazsa veri kaybı hissi doğar. Ekranda kırmızı uyarı şart.
- ADR-0014'ün import cümlesi bir GÜVENLİK gerekçesiydi; ADR-0016 yazılırken o gerekçenin nasıl karşılandığı madde madde gösterilmezse kayıt zayıf olur.

## Açık sorular
- OYLAMA: üç seçenekten hangisi? (Önerim: 2 — kapılı içe aktarım, Faz A önce.)
- Faz A'da yinelenen politikasının VARSAYILANI ne olsun: ATLA mı HATA mı? (Fail-closed mantığı HATA der; kullanılabilirlik ATLA der.)
- ice_aktarim_satirlari.ham_json ne kadar saklanacak? ADR-0008'deki hangi süre sınıfına girer — karar ADR-0008 sahibine ait.
- Oran sınırı rakamı: dışa aktarım saatte 10 istek. İçe aktarım için 5 mi 10 mu? Ölçülmedi, tahmin etmiyorum.
- Dosya/satır tavanı: MaksSatir 5.000 mi 20.000 mi? Gerçek bir dükkânın kart sayısı ölçülmedi.
- Faz B açılış stoku için ürün kartı YOKSA otomatik oluşturulsun mu, yoksa 'önce ürün dosyasını aktar' denip satır HATA mı olsun? (Önerim: HATA — tek dosyada iki varlık üretmek geri almayı karmaşıklaştırır.)
- GUNCELLE politikası hiç sunulmasın mı? (Basitlik için Faz A'da yalnız ATLA/HATA sunulabilir; oylama.)

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Ticari itiraz gerçek ve paket onu mevzuat kapılarını delmeden kapatıyor. Kuru prova + satır bazlı hata + tek transaction + geri alma dörtlüsü doğru. 'DEVIR partisi + NULL maliyet' deseninin yeniden kullanılması (sıfır maliyet yazılmaması) mevcut mekanizmanın doğru genişletilmesi. Base64 yükleme deseninin paket 4'ten devralınması yeni altyapı icat etmiyor.
  - Koşullar: SEÇENEK 2 (kapılı içe aktarım) onaylanır; SEÇENEK 3 MİMARİ OLARAK REDDEDİLİR — spec'in kendi gerekçesi doğrudur: geçmiş hareket aktarımı kur_snapshot_id uydurmayı, tevsik ve MASAK kümülatiflerini geriye dönük anlamsızlaştırmayı ve denetim izinin yalan söylemesini gerektirir. · İçe aktarım ADR'si paket 32 ile ORTAK yazılacak (iki ayrı ADR açılmayacak) ve ADR-0014'ün 'geri alma yoktur' cümlesinin hangi gerekçelerle nasıl karşılandığı MADDE MADDE gösterilecek — aksi hâlde kayıt zayıf olur. · Mehmet'in (06 Güvenlik) onayı ALINMADAN Faz A sevk edilmez: bu paketin riski sızdırma değil ZEHİRLEME (uydurma cari, sahte açılış stoku); gerekçe+SAHİP+kuru prova+geri al+denetim izi dörtlüsü onun şartlarının karşılığı olarak tasarlanmış ama onaylanmamış. · FAIL-CLOSED 3 (içe aktarım kendi yazma yolunu açmaz, mevcut servisleri çağırır) bir denetim kuralıyla zorlanacak: Modules/Iceaktarim altında Hareketler.Add / db.Cariler.Add / db.Urunler.Add = 0. · Faz A'da GUNCELLE politikası SUNULMASIN (yalnız ATLA/HATA): geri almayı imkânsızlaştırıyor ve ekranda anlaşılmazsa veri kaybı hissi üretiyor. İhtiyaç kanıtlanırsa Faz B'de açılır. · Faz B açılış stokunda ürün kartı yoksa satır HATA olsun (otomatik ürün üretilmesin) — tek dosyada iki varlık üretmek geri almayı karmaşıklaştırır.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Zehirleme yüzeyini kuru prova + SAHİP + gerekçe + geri al + denetim izi + mevcut servis yolu ile kapatıyor; mevzuat kapılarını atlamıyor (para/defter yok). Kişisel veri sahne tablosu tek ciddi açık.
  - Koşullar: YALNIZ SEÇENEK 2 (kapılı içe aktarım). Seçenek 3 (geçmiş hareket dahil serbest içe aktarım) VETO — tevsik/MASAK kümülatifi geriye dönük anlamsızlaşır, kur_snapshot_id uydurulur. · ham_json ADR-0003 ile çelişir: vergi no/TCKN/telefon sahne tablosunda DÜZ METİN duracak. Zorunlu: ham_json kimlik alanları MASKELENEREK yazılır (ör. TCKN son 4 hane), ham değer yalnız istek ömründe bellekte kalır ve doğrudan servis DTO'suna geçer; ya da ham_json AES şifreli. Saklama ADR-0008'e bağlı. · Yinelenen politikası VARSAYILANI HATA (fail-closed); GUNCELLE Faz A'da SUNULMAZ (geri almayı kapatır). · Oran sınırı 'ice-aktarim' 5/sa (dışa aktarımdan düşük — spec ilkesi); MaksSatir 5.000 başlangıç, yapılandırmadan; MaksDosyaBayt ≤ 2 MB. · Sınıf düzeyi [RolGerekli("SAHIP")] + denetim izi Hassas=true + geri alma SOFT delete; ExecuteUpdate/Delete yok. · Satır uygulaması mevcut servisler (CariServisi.Olustur/UrunServisi.Olustur/Duzeltme/StokSayimGuncelle) üzerinden — kendi Add'i yok; TasKapisi istisnası yutulmaz; tek transaction. · CSV bomb/alan sayısı tavanı (32'deki koşulla aynı); enjeksiyon tırnağı girişte sökülür, ekranda MarkupString yok. · ADR-0016 (b) 32 ile ORTAK yazılır.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Kuru prova + tek yazma yolu + geri alma tasarımı fail-closed; mutasyon listesi test adlarıyla eşleşiyor ve iki katmanlı kiracı koruması dürüstçe 'ikinci mutasyon gerekir' diye yazılmış. Ancak ADR kararı ve güvenlik onayı yok, transaction iç içe davranışı doğrulanmamış.
  - Koşullar: Oylama önce ADR kararı (Seçenek 1/2/3) vermeli; QA olarak Seçenek 2 (kapılı) kabul edilebilir, Seçenek 3 fail-closed ihlali. Faz B (açılış bakiyesi/stok) ADR-0016 oylanmadan başlamaz. · Mehmet'in (06 Güvenlik) onayı ALINMADI (risk bölümü açıkça yazıyor) — zehirleme yüzeyi için güvenlik oyu olmadan sevk yok. · IceAktarimTestleri 'Bir_satir_patlarsa_TUM_oturum_geri_alinir' testi mevcut servislerin (CariServisi.Olustur) kendi SaveChanges/transaction açıp açmadığına bağlı; CariModulu.cs:221 ve UrunModulu.cs:236 iç transaction kullanıyorsa dış transaction ile iç içe davranış MySQL'de doğrulanmalı — test bunu gerçek bir istisnayla (3. satırda TasKapisi) ölçsün ve satır sayısını SQL ile karşılaştırsın. · CsvOkuyucuTestleri 'Ondalik_NOKTA_ile_okunur_tr_TR_kulturunde_de' testi iş parçacığı kültürünü açıkça tr-TR'ye çekmeli (test projesinde kültür sabitlenmiyor). · Oran sınırı ve MaksSatir rakamları açık soruda; testler bu rakamları sabitlemesin, yapılandırmadan okuyup 'tavan+1 → 400' biçiminde yazılsın. · Paket 32 ile aynı ADR-0016 numarası ve aynı 'içe aktarım kapısı' kavramı — tek ADR yazılsın (çapraz bulgu).

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO: Mehmet — Seçenek 3 (geçmiş hareket/fatura/kasa dahil serbest içe aktarım) YASAK; tevsik ve MASAK kümülatifi geriye dönük anlamsızlaşır, kur_snapshot_id uydurulur, denetim izi yalan söyler (Anayasa md.1). Yalnız Seçenek 2 (kapılı) uygulanır.
- Kişisel veri açığı (Mehmet, ADR-0003 çelişkisi): ice_aktarim_satirlari.ham_json'da vergi no/TCKN/telefon DÜZ METİN duramaz — kimlik alanları maskelenerek yazılır (ör. TCKN son 4) ya da AES şifreli; ham değer yalnız istek ömründe bellekte. Saklama süresi ADR-0008'e bağlanır.
- FAIL-CLOSED: içe aktarım kendi yazma yolunu açmaz, mevcut servisleri (CariServisi.Olustur / UrunServisi.Olustur / Duzeltme / StokSayimGuncelle) çağırır. Denetim kuralı: Modules/Iceaktarim altında Hareketler.Add / db.Cariler.Add / db.Urunler.Add = 0. TasKapisi istisnası yutulmaz.
- Yinelenen politikası VARSAYILANI HATA (fail-closed); GUNCELLE politikası Faz A'da SUNULMAZ — geri almayı imkânsızlaştırır. İhtiyaç kanıtlanırsa Faz B'de açılır ve ekranda kırmızı uyarı ile.
- Sınıf düzeyi [RolGerekli("SAHIP")] + denetim izi Hassas=true + geri alma SOFT delete; ExecuteUpdate/Delete yok; tek transaction. Oran sınırı 'ice-aktarim' 5/sa (dışa aktarım 10/sa'nın altında), MaksSatir 5.000 başlangıç, MaksDosyaBayt ≤ 2 MB — hepsi yapılandırmadan.
- Testler rakamları sabitlemez: tavan değerleri yapılandırmadan okunup 'tavan+1 → 400' biçiminde yazılır (açık sorular oran sınırı/MaksSatir hâlâ oylamada).
- 'Bir_satir_patlarsa_TUM_oturum_geri_alinir' testi iç içe transaction davranışını gerçek istisnayla ölçer: CariModulu.cs:221 ve UrunModulu.cs:236 iç transaction açıyorsa MySQL'de dış transaction ile davranış doğrulanır, satır sayısı SQL ile karşılaştırılır.
- CSV bomb / alan sayısı tavanı (paket 32'deki koşulla aynı); enjeksiyon tırnağı girişte sökülür, ekranda MarkupString yok. 'Ondalik_NOKTA_ile_okunur' testi iş parçacığı kültürünü açıkça tr-TR'ye çeker.
- ADR-0016 paket 32 ile ORTAK ve TEK yazılır; ADR-0014'ün 'geri alma yoktur' güvenlik gerekçesinin nasıl karşılandığı MADDE MADDE gösterilir. Faz B (açılış bakiyesi/stok) ADR-0016 oylanmadan başlamaz.
- Faz B açılış stokunda ürün kartı yoksa satır HATA olur (otomatik ürün üretilmez). Satış vaadi dürüst kalır: 'tek tık göç' denmez, 'şablona yapıştır' denir (rakip CSV kolonları bilinmiyor — PARITE §6). Mimarın CariModulu/UrunModulu DEĞİŞMEZ kuralı korunur.
