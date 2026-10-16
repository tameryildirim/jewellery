# PAKET GOC-C — Ürün ve fiş ekranlarının ortak girdi + ortak ızgara göçü · uygulama raporu (09.10.2026)

> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t1 · canlı API 5160 · vekil 5161 · CDP 9711–9718 · `KUYUMCU_TEST_DB=kuyumcu_test_t1`.
> Kaynak (tek doğruluk): `GIRDI-KURALLARI-ENVANTER-2026-10-08.md` — (B) K1–K34, (C) bu ekranların 291 satırı (109'u TAM değildi), (E) Faz 2 GOC-C, (F) F2/F3, (G) G1.
> COMMIT YOK. Dosya silinmedi/taşınmadı. 5199/5200'e dokunulmadı. Sır/parola değeri çıktıya yazılmadı (fikstürler chmod 600).
> Migration: **gerekmedi** (şema değişikliği yok). Tur: **2** (tur 2 = hasım doğrulayıcı bulgularının kapanışı → **§ 14**; § 1–13 tur 1 kaydıdır, tur 2 ile değişen sayılar § 14'tedir).

## 1. Yapılanlar

### 1.1 Sunucu — "önce kapanacak açıklar" (E · GOC-C satırı) + devirler
| Açık | Önce | Şimdi | Dosya |
|---|---|---|---|
| **#5 Taş > gram** | taş 10 g / brüt 5 g kabul → has gram EKSİ | `UrunServisi.GocCKapisi` (POST + PATCH ortak): ADET birimde `taş > 0 && taş ≥ brüt` → 400 `TasBrut` ("Taş ağırlığı brüt gramdan küçük olmalı.") alan `tasAgirlik`. PATCH'te KAYITLI + GELEN birleşimi sınanır (yalnız gramı düşüren PATCH de yakalanır) | Urun/UrunModulu.cs |
| **#6 Adet tam sayı** | adetli üründe 1,5 adet satış/takas/transfer/iade/rezerv/konsinye/değişim/sayım/yerleşim/açılış | Yeni `Urun/UrunAdetKapisi` (Core `AdetKurali` — ekran `<Adet>` ile AYNI sınıf): **ADET birimde tam sayı ≥ 1**, GRAM birimde kesir serbest (K14 "gramlı üründe miktar gramdır"). Çağrı yerleri: tekli satış, alış, alış fişi kalemi, satış fişi kalemi, transfer, satış/alış iadesi, takas (verilen ürün), rezerv (oluştur + PATCH), konsinye çıkış/iade, değişim (geri + yeni), yerleşim/taşıma/geri al, stok sayım (PATCH stok + sayım oturumu, 0 serbest), ürün açılış stoğu, kritik stok eşiği. Sipariş kalemi `[Adet]` (parça sayısı) | Satis/SatisModulu.cs, Satis/SatisModulu.P21.cs, Yerlesim/YerlesimServisi.cs, Sayim/SayimModulu.cs, Urun/* |
| **#7 Takas 0** | `TakasDto Range(0,…)` 0 g / 0 TL kabul | DTO alt sınırı 0'ın üstü + servis yolu `TakasPozitif` (SayiSifir/SayiEksi sözlük iletisi) — eski gram, eski has gram fiyatı, verilen ürün fiyatı | Satis/SatisModulu.cs |
| **İskonto** | DTO Range(0,100) + `YuzdeDogrula` serbest metin; 10,555 sessiz yuvarlanıyordu; eksi iskonto tek kalemde sessiz yok sayılıyordu (ekran) | `[Yuzde]` (Core K15: 0–100, 2 ondalık) fiş + satır; servis yolu `IskontoHesabi.YuzdeDogrula` → `GirdiKapisi` + `YuzdeKurali` (ekran `<Yuzde>` ile aynı sözlük anahtarı) | Satis/SatisModulu.cs, Satis/IskontoHesabi.cs |
| **Barkod 60/50** | ekran 60, sunucu 50, karakter kümesi yok | `[Barkod]` (K18 ASCII 32–126, 1–50) POST/PATCH + servis yolu `BarkodKurali`; ayrı MaxLength kaldırıldı (ikinci farklı ileti üretiyordu); ekran `BarkodAlani` (maxlength 50, Türkçe harf/boşluk tuşta yazılamaz) | Urun/UrunModulu.cs |
| **Sipariş söz günü** | geçmiş/0001/9999 serbest | `SiparisServisi.SozGunuDogrula` (TarihPenceresi Vade, en geç bugün+2 yıl — K8c İŞ KURALI): POST'ta her zaman; PATCH'te **yalnız DEĞİŞEN** gün (kayıtlı ve zamanla geçmişte kalmış gün düzenlemeyi kilitlemez) | Siparis/SiparisModulu.cs |
| Sipariş kalem / tahmini / kapora | gram 0 geçiyordu; DTO decimal aralıksız | `[Gram]` + servis `GirdiKapisi.Gram` (> 0, 3 ondalık), `[Adet]` + servis, `[Tutar(SifirOlabilir)]` tahmini, `[Tutar]` kapora | Siparis/SiparisModulu.cs |
| **RAPOR-4 devri** — ürün servis yolunda eksi alış maliyeti | DTO Range yalnız controller yolunda | `GocCKapisi`: alış maliyeti, satış fiyatı, işçilik, konsinye bedeli K9a (eksi yok) servis yolunda; DTO `AlisMaliyeti` `[Tutar(SifirOlabilir)]` (ekranın Para iletisiyle aynı) | Urun/UrunModulu.cs |
| Sayım | gövdede adet yoksa SESSİZ 0; ad kırpılmıyordu | `SayilanAdet` nullable + "girilmedi" 400; eksi/kesir 400; ad kırpılmış boş/uzun 400 | Sayim/SayimModulu.cs |
| Kurulum açılış | fazla ondalık sessiz yuvarlanıyordu; DTO decimal aralıksız | `AcilisKaynaklari.OndalikKapisi` (TL 2, has 6 ondalık + has tavanı; prova ve uygula aynı) + Range | Kurulum/AcilisKaynaklari.cs, AcilisServisi.cs |
| İşçilik (işlem) | 0,001 TL geçiyordu | `GirdiKapisi.Tutar` (K9) | Satis/SatisModulu.cs |
| Kampanya tarihleri | 0001 / 3000 yılı | `[TarihPenceresi(Serbest)]` (1900–2100) POST + PATCH | Fiyat/FiyatModulu.cs |

Sınır sabitleri `AlanSinirlari` (Core/Domain/Girdi/GirdiHatasi.cs) += 32 GOC-C sabiti (UrunKod/Ad/Aciklama/SertifikaNo/SeriNo/TasIbaresi/Olcu/Kmts/Rfid/GorselUrl, ReyonAdi, UrunKategoriAdi, KategoriSiraEnCok, FiyatListesiAdi, KampanyaAdi, FiyatUrunNotu, PanoEtiket, IslemAciklama, SaticiAd/Adres, TedarikciFaturaNo, SiparisAciklama/KalemTanim/SozEnCokYil, SayimAdi, AktarimGerekce/Onay, Arama, EtiketKopyaEnCok). Ekran `EnFazla` = DTO MaxLength = kolon (SQL ile doğrulandı: urunler.kod 100, ad 300, barkod 50, siparis_kalemleri.tanim 200, sayim_oturumlari.ad 150, fiyat_listesi_urun_fiyatlari.not 200 …). Ekranın sunucudan UZUN izin verdiği alanlar kapandı: barkod 60→50, tedarikçi fatura no 60→50, fiyat ürün notu 500→200, içe/dışa aktarım gerekçesi 1000→500, onay metni 200→100.

### 1.2 Ekran — 12 sayfa + Fis/* + AltinBacagiPaneli + FiyatListesiUrunPaneli
- **Ham `<input>/<select>` = 0** bu ekranlarda (Sayım: ad/şube/kapsam/kapsam kimliği/barkod/adet/onay; Siparişler: tanım/gram/adet/söz günü/açıklama/gecikenler onayı). `type="date"` 0 (Siparisler `sip-f-gun` → ortak `Tarih`).
- **Anlamsal bileşenler:** milyem ×2 → `Milyem`; TCKN ×5 (hurda/takas/altın bacağı satıcısı) → `TcKimlikNo` (KimlikNoDogrulayici — yeniden yazılmadı); satıcı adı ×3 → `AdSoyad`; barkod ×2 → `BarkodAlani`; gram/taş → `Gram Tur=Gram/Tas Brut=…` (taş ≥ brüt yazarken alan altı + Kaydet kilidi); alış maliyeti/satış/işçilik → `Para Tur=SifirOlabilir`, konsinye bedeli/işçilik tutarı/sabit fiyat/takas verilen ürün fiyatı/sipariş kalem fiyatı → `Tur=Pozitif`; hedef marj → `Yuzde Marj`; KDV, fiş iskontosu, kampanya yüzde → `Yuzde`; kampanya TL → `Para`; adet (satış ADET üründe, sipariş ×2, transfer, takas, etiket kopya, açılış ADET) → `Adet` (iletisi Core AdetKurali — sunucuyla AYNI cümle); vade gün → `GunSayisi`; sayım/eşik/yerleşim adet → `Sayi TamSayi=(ADET) EnCokOndalik=3`; kategori/pano sırası tam sayı (eskiden (int) ile sessiz kırpılıyordu); söz günü `Tarih Tur=Vade EnGec=+2y` (SiparisDetay: kayıtlı geçmiş gün korunabilir); kampanya bitiş `EnErken=başlangıç`.
- **Kaydet kilidi:** Siparisler formuna `GecersizAlanKapisi` (CascadingValue) — kilit sebebi alan altı metniyle aynı; Urunler/SiparisDetay/Fiyatlandirma/FiyatListesi panelindeki mevcut kapılar yeni kurallarla besleniyor.
- **Ortak ızgara (23 tablo → `Izgara`)**, her biri `grid:<ekran>:<grid-id>` tercihiyle sütun sırası, göster/gizle, sıralama, kullanıcıya+ekrana kalıcı, "Varsayılana dön": `urunler:urun-liste` · `urunler:urun-stok` · `urun-agaci:reyon-liste` · `urun-agaci:reyon-icerik` · `urun-etiketleri:et-liste` · `vitrin:vitrin-pano` · `vitrin:vitrin-atlananlar` · `fiyatlandirma:fl-tablo` · `fiyatlandirma:km-tablo` · `fiyatlandirma:flp-tablo` · `fiyatlandirma:flp-ozet-tablo` · `sayim:sy-tablo` · `sayim:sy-satirlar` · `kurulum:kur-acilis-tablo` · `kurulum:kur-prova-hatalar` · `veri-ice-aktarim:ia-hata-tablosu` · `veri-ice-aktarim:ia-gecmis-tablosu` · `veri-disa-aktarim:da-gecmis` · `siparisler:sip-liste` · `siparis-detay:sipd-kalemler` · `islemler:rezerv-liste` · `islemler:konsinye-liste` · `islemler:siparis-kalemler`. Mevcut kimlikler (tablo id `TabloId`, `KartListeId`, satır/hücre/eylem id'leri) korundu. Sunucu sınırına değen listede (ürünler 50, etiketler 500, sipariş 200, sayım 100, aktarım geçmişi 20, içe aktarım hataları 100, fiyat paneli sayfalı, vitrin tavanı) sıralama KAPALI + GOC-A `SiralamaSinirNotu` (UX-S29). Ham kod ekrana basılmıyor (sipariş kalemi ayarı artık ad; UX-S31); sayım sayıları tr-TR (eskiden kültürsüz "0.###").
- **Devirler (URUN-ACILIS-ATOMIK KAPSAM DIŞI):** (1) PRO 1280 ürün listesi Durum sütunu kesik + Aktif rozeti Stok düğmesine biniyor → liste ortak ızgarada, ölçüm 8/8 kombinasyonda K3/K9 = 0, görüntüde Durum sütunu tam, rozet ile düğme ayrık; (2) gram binlik gruplanmıyor → liste hücresi `Bicim.GramGruplu` (N3; `Bicim.Gram` grupsuz sözleşmesi BicimTestleri'nde korunur); (3) yeni ürün formu açılışta isteğe bağlı alanlarla dolu → **PRO'da da** yalnız zorunlularla açılır, isteğe bağlılar "Daha fazla"da (kural kapısı olan taş kökeni, 4C, çelik satış fiyatı katlanmaz); düzenleme Cekmece kalıbında (değişmedi).

### 1.3 Ortak bileşenlere en küçük geriye uyumlu eklemeler (gerekçeli)
| Dosya | Ekleme | Gerekçe |
|---|---|---|
| `Bilesenler/Form/SayisalAlan.cs` + `AnlamsalSayilar.cs` | `protected virtual bool TamSayiCoreKuralinda => false`; `Adet` true döner | `Adet`'te kesir iletisi KESIF-P1 metni yerine Core `AdetKurali` (sunucu UrunAdetKapisi ile AYNI sözlük anahtarı — kabul 2). Diğer bileşenlerde davranış birebir. |
| `Servisler/Bicim.cs` | `GramGruplu(n)` | devir "gram binlik gruplanmıyor"; `Bicim.Gram` (BicimTestleri: grupsuz) dokunulmadı |
| `wwwroot/css/zo-islem.css` | `.sip-kalem-izgara` sütun alt sınırı 9rem → 11rem | ölçüm K7: ortak kabuktaki "(isteğe bağlı)" eki "Serbest tanım" etiketini iki satıra kırıp hizayı 26 px kaydırıyordu |
| `Core/Domain/IzgaraTercihi.cs` | `BilinenEkranlar` += 12 GOC-C ekranı | beyaz liste fail-closed |
| `Core/Domain/Girdi/GirdiHatasi.cs` | `AlanSinirlari` GOC-C sabitleri | ekran = sunucu tek sabit |
| `dotnet/denetim.sh` | 5 tavan indirildi | cırcır |

## 2. Sapmalar ve gerekçeleri
1. **Ham tablo istisnaları (5, ham sayılmaya devam eder, tavan gevşetilmedi):** satış tuvalinin `FisKalemTablosu` ×2 (satır içi fiyat/adet düzenlemesi), `FisVerdimAldim` (satır içi ödeme seçici), `FisSonSatislar` (satır altı ödeme kırılımı, colspan) — kurucu "satış ekranı davranışı değişmez" + ortak ızgarada satır altı/hücre-içi düzenleme şablonu yok; `Vitrin #vitrin-ayarlar` — başlıktaki "değiştirilebilir" rozeti ve ilk sütun nowrap kuralı (KatmanTekKalip K1-25, Toplu4 T4, Vitrin testleri) ızgara başlığında taşınamıyor (ızgara başlığı yalnız metin). Ortak bileşen (Grid/*) "yalnız kullan" kuralı gereği eklenmedi → § 9.
2. **Takas ondalık hanesi değiştirilmedi:** #7 kapsamı SIFIR/EKSİ reddi; eski gram 4 hane ve yarım kuruş fiyat ölçüsü mevcut sınır testlerinin (CIFTBACAK, YASAL3) sözleşmesi.
3. **Sayım / kritik eşik / yerleşim adedi `Sayi TamSayi`** (0 geçerli olduğundan `Adet` değil): ekran kesir iletisi KESIF-P1 metni ("Tam sayı yazın…"), sunucu Core `SayiTam` ("Küsuratlı yazılamaz…") — aynı anlam, iki cümle (§ 9).
4. **Rezerv/konsinye/değişim adet kutuları** (Fis/FisDiger*): ürünün birimi bileşende bilinmediği için ekranda yalnız 3 ondalık sınırı; ADET birimde tam sayı SUNUCUDA (400 + ileti bantta).
5. **Sipariş gerekçeleri** (iptal/geri geçiş asgari 10, proje K30 20): sunucu sabiti (`GerekceAsgari`) değiştirilmedi; ekrandaki gömülü 10 kalktı, sunucu sabiti okunuyor.
6. **K21 adres ≥ 10 uygulanmadı** (TEYİTSİZ, GOC-A/B ile aynı karar) — satıcı adresi `Metin` + tek sabit.
7. **Yeni ürün penceresi ModalPencere'de kaldı** (Cekmece'ye taşınmadı): URUN-ACILIS-ATOMIK tur 2'nin sabit altlık (hata bandı + kilit sebebi + Kaydet her zaman görünür) düzeltmesini bozmamak için; devirdeki "düzenleme Cekmece kalıbı" düzenleme çekmecesidir (zaten Cekmece).
8. **Güncellenen mevcut testler** (gerekçe satırı her birinde; hiçbiri gevşetilmedi, kural ters yönden sınanıyor): SiparisEkran ×4, Toplu6, KesifP6 ×2, Q95 ×3, UX1C2 ×2, SayimEkran, UX4c, Q105 ×1, S3a, SiparisTestleri (gecikme zamanla doğar), PatchIki, PatchUc ×2, PatchSozlesme ×3, OrtakGorunumKiyas KY10, P10 ×5, UrunEkran ×4, KesifP4 ×4, P16, Yasal4Ekran, S33_17 (PRO artık katlı — devir), KesifP1 (ayar title'ı hücre içinde), PosTahsilat (0 TL/has takas fikstürü geçersiz), Dogrulayici: KESIFP6Sinir (0/-5 gram ve kesirli adet artık kilit), URUNACILISATOMIKSinir ×4, PATCH3Sinir ×2, PATCHSOZLESMESinir ×2, L1Sinir, L1bSinir (kesir yuvarlama ölçüleri GRAM birimli üründe korunur — `TestKurulum.UrunuGramBirimiYap`).

## 3. Migration
Gerekmedi.

## 4. Testler
- **Yeni `Kuyumcu.Tests/GocCTestleri.cs` — 19 test:** GocCSafTestleri GCS01 ızgara beyaz listesi · GCS02 ekran sınırı = DTO sınırı (22 alan) · GCS03 DTO öznitelikleri · GCS04 adet kapısı ADET/GRAM · GCS05 ürün girdi kapısı (taş/eksi tutar/eşik/barkod tam sınırlar) · GCS06 söz günü (dün/+2y+1/bugün/sınır) · GCS07 iskonto (101/−5/10,555) · GCS08 kurulum ondalık. GocCSunucuTestleri (gerçek MySQL + HTTP) GCH01 ürün POST (taş, eksi maliyet, barkod 51/ÇAĞ, açılış 1,5) → 400 + satır YOK, geçerli kayıt + SQL · GCH02 PATCH (KAYITLI taş + düşen gram, eksi maliyet) kayıt değişmez · GCH03 satış fişi kesirli adet / %101 / −5 → hareket YOK, geçerli satış · GCH04 takas 0/0/0/1,5 HTTP + servis yolu · GCH05 sipariş geçmiş/2y+/1,5/0 gram, PATCH kayıtlı geçmiş gün korunur · GCH06 sayım + rezerv kesir (GRAM'da geçer) · GCH07 kampanya 0001/3000 · GCH08 ızgara tercihi A kalıcı / B yok. GocCEkranTestleri (bUnit) GCE01 sipariş geçmiş gün + 1,5 adet alan altı + kilit (sebep = alan altı metni) POST YOK · GCE02 ürün PRO zorunlu önce + taş ≥ gram alan altı/kilit + barkod 50/Türkçe harf · GCE03 sayım ızgarası, ham select 0, ortak seçici.
- Filtre `~GocC`: **19/19**.
- **Tam süit (son kod, temiz derleme 0/0 sonrası):** `Toplam 11351 · Geçti 11348 · Başarısız 1 · Atlanan 2` (`/private/tmp/kuyumcu-serit/trx-pGOC-C/tam.trx`). Tek kırmızı § 7. GOC-B sonu 11285 / 0 kırmızı.
- Ara ölçüm (tur içi, `ara1.trx`, 11348 test): 20 kırmızı → 15'i bu paketin sözleşme değişikliğinden (yukarıdaki güncellemeler), 2'si DIL-EKRAN şeridi (o an), 1 Toplu5 Gider CSS (başka şerit), 3'ü saat sınırı (§ 7). Son koşumda yalnız 1 kaldı.

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-C/` → TEK eşleşme → derle → `~GocC` → geri kopya + mtime ileri → `filecmp` birebir; betik `GOC-C/mut.py`, sonuç `mut-sonuc.jsonl` + `mut-sonuc-b.jsonl`)
| Kod | Kural kapatıldı | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | #5 taş < brüt (servis) | GCS05, GCH01, GCH02 | AYNI |
| M1-2 | #6 ADET tam sayı kapısı (her birim GRAM) | GCS04, GCH01, GCH03, GCH04, GCH06 | AYNI |
| M1-3 | #7 takas sıfır reddi (servis yolu) | GCH04 | AYNI |
| M1-4 | Söz günü kapısı (POST) | GCH05 | AYNI |
| M1-5 | İskonto Core kuralı → yalnız aralık | GCS07 | AYNI |
| M1-6 | RAPOR-4 eksi alış maliyeti (servis) | GCS05 | AYNI |
| M1-7 | Kurulum TL ondalık kapısı | GCS08 | AYNI |
| M1-8 | Ekran: söz günü penceresi (Vade) | GCE01 | AYNI |
| M1-9 | Ekran: taş alanı brüte bağlı değil | GCE02 | AYNI |
| M1-10 | Sayım ADET kesir kapısı | GCH06 | AYNI |
10/10 kırmızı, 10/10 cmp AYNI. ⚠ Dürüstlük notu: ilk koşumda M1-4 "GCH04" (M1-3 kalıntısı — derleme `dotnet test` içinde geç algılandı) ve M1-8 derleme hatası (başka şeridin eşzamanlı Web derlemesi `ConvertDllsToWebCil` dosya kilidi) verdi; betik "önce `dotnet build`, sonra `--no-build` test" olarak düzeltildi ve üçü (M1-3/4/8) temiz yeniden koşuldu — tablo SON koşumdur. Son mutasyondan sonra temiz derleme 0 Uyarı 0 Hata.

## 6. denetim.sh (son kod)
`DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, görev "P-30 hariç").
| Sayaç | Önce (GOC-B sonu) | Sonra |
|---|---|---|
| GRID-1 HAM_TABLO_ORTAK_GRID_DISI | 58 | **35** (5 gerekçeli istisna § 2.1) |
| GIRDI-2 GIRDI_TUR_GENEL_BILESEN | 9 | **2** (kalanlar paket dışı) |
| GIRDI-2 HAM_TARIH_GIRDISI | 6 | **5** (kalan SatisAnalizi ×2 … paket dışı) |
| GIRDI-2 DTO_DECIMAL_RANGESIZ | 17 | **10** (Urun/Siparis/Kurulum/Satis'te 0; kalan Satinalma ×2, Pano ×1 …) |
| GIRDI-2 EKRAN_SUNUCU_UZUNLUK_FARKI (vekil) | 86 | **30** (paket ekranlarında 0) |
| P-36 HAM_INPUT / HAM_SELECT | 23 / 17 | **13 / 14** (paket ekranlarında 0) |
| P-15 DUZ_SELECT_LIMIT | ✓ | ✓ (ara ölçümde yorumdaki "limit=200" sözcüğü sayılmıştı — yorum düzeltildi) |

## 7. Tam süitte kalan kırmızı — sahibi
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` (Expected 1, Actual 0): test `Mutabakat(k, DateTime.UtcNow)` ile UTC günü sorar; koşum İstanbul 01:xx'te (UTC önceki gün) yapıldı → gün sınırı kararsızlığı. Bu paket hurda/pusula/mutabakat yoluna dokunmadı; GOC-B koşumunda (22:xx) yeşildi. Aynı sınıftan `TevsikIhlalDbTestleri.K16_*` ×2 ara koşumda (00:30) kırmızı, son koşumda (01:xx) yeşil. **Sahibi: YASAL-4 / tevsik testleri (saat dilimi).** Bu paketin YENİ kırmızısı: **0**.

## 8. Canlı doğrulama (API 5160, Development, `kuyumcu_net`; kiracı `PAKETGOC-C-TEST 80c8d2` = 01M4ERASRRQXT53T3MEF08DWJH; A=SAHİP, B=MÜDÜR; betik `GOC-C/canli_gocc.py`, sonuç `GOC-C/canli-sonuc.json`) — **UYUMSUZ 0 / 39**
| Adım | Beklenen | Gerçek | İleti · alan / SQL |
|---|---|---|---|
| Ürün taş 10 g > brüt 5 g | 400 | 400 | "Taş ağırlığı brüt gramdan küçük olmalı." · tasAgirlik |
| Ürün alış maliyeti −100 | 400 | 400 | "Eksi değer yazılamaz." · alisMaliyeti |
| Ürün barkod 51 karakter | 400 | 400 | "Barkod 1–50 karakter olmalı." · barkod |
| Ürün barkod "ÇAĞ-001" | 400 | 400 | "Barkoda Türkçe harf ve özel işaret yazılamaz." · barkod |
| Ürün gram harf ("on") | 400 | 400 | "Gram ağırlık geçerli bir sayı değil …" · gramAgirlik |
| Ürün açılış stoğu 1,5 (ADET) | 400 | 400 | "Küsuratlı yazılamaz; tam sayı girin." · acilisStogu.adet |
| SQL: 400'lerde ürün yok | 0 | 0 | urunler=0 |
| Ürün geçerli (5 g, taş 0,5, barkod, açılış 10) | 201 | 201 | `GC-OK gram=5.0000 tas=0.5000 has=4.122000 barkod=GC-OK-01` · stok 10.000 |
| PATCH gram 0,4 < kayıtlı taş 0,5 | 400 | 400 | TasBrut · SQL gram=5.0000 (değişmedi) |
| PATCH alış maliyeti −1 | 400 | 400 | SayiEksi · SQL alis=0.00 |
| Stok sayım 2,5 (ADET) | 400 | 400 | SayiTam · SQL stok 10.000 |
| Satış fişi 1,5 adet | 400 | 400 | "Burma bilezik: Küsuratlı yazılamaz …" · kalemler |
| Satış fişi iskonto %101 / −5 / 10,555 | 400 ×3 | 400 ×3 | YuzdeAralik / YuzdeAralik / SayiOndalik · fisIskontoYuzde |
| SQL: 400'lerde hareket yok | 0 | 0 | — |
| **Satış** geçerli (1 adet, %10) | 200 | 200 | `SATIS adet=1.000 tl=900.00` |
| İade 0,5 (ADET) | 400 | 400 | SayiTam · adet |
| **İade** geçerli 1 adet | 200 | 200 | `SATIS_IADE adet=1.000 tl=-900.00` |
| Takas eski gram 0 / eski fiyat 0 / verilen fiyat 0 / 1,5 adet | 400 ×4 | 400 ×4 | eskiUrunGram / eskiUrunBirimFiyatTlHasGram / yeniUrunBirimFiyatTl / yeniUrunAdet (SayiTam) |
| SQL: takas 400'lerinde hareket yok | 0 | 0 | — |
| **Takas** geçerli (3 g K22 ↔ 1 adet) | 200 | 200 | `TAKAS adet=1.000 eskiHas=2.748000 tl=11756.00` |
| **Hurda** alış geçerli (1,5 g K22) | 200 | 200 | `HURDA_ALIS has=1.374000 tl=-4122.00` |
| Sipariş söz günü dün / 3000 | 400 ×2 | 400 ×2 | "Tarih 09.10.2026 tarihinden önce olamaz." / "… 09.10.2028 tarihinden sonra olamaz." · sozVerilenGun |
| Sipariş kalem 1,5 adet / 0 gram | 400 ×2 | 400 ×2 | SayiTam · kalemler[0].adet / SayiSifir · kalemler[0].gramAgirlik |
| Sipariş geçerli (+7 gün) | 200 | 200 | `S-2026-000001 soz=2026-10-16` |
| Kampanya 0001 / 3000 | 400 ×2 | 400 ×2 | TarihErken 01.01.1900 · baslangic / TarihGec 31.12.2100 · bitis |
| Izgara tercihi A yaz/oku · B yok | 200/200/200 | aynı | SQL `grid:urunler:urun-liste len=99` |
Not: ilk koşum (kiracı 01M4ERA98X0BCVRDZRG44CWCW3, `canli-sonuc-1.json`) 4 uyumsuz verdi — betik beklentisi satış/iade/takas için 201 yazılmıştı (uç 200 döner) ve hurda alışı yeni kasanın bakiyesini aşıyordu ("Nakit kasa eksiye düşemez" — doğru davranış); beklenti/miktar düzeltilip temiz kiracıyla yeniden koşuldu. Kur yeni kiracıda gelmediğinden `/kur/manuel` ile girildi.

### 8.1 Gerçek başsız Chrome (vekil 5161, CDP 9711–9718; `sikisma-olc.js`; `GOC-C/olc.mjs`; ham `GOC-C/olcum/olcum.jsonl` 112 ölçüm + `olcum2/` 16 yeniden ölçüm; her ölçümün ekran görüntüsü)
14 görünüm (yeni ürün: "Daha fazla" açık; yeni sipariş: "Daha fazla" açık; katlanırlar açık) × {1280×800, 375×812} × {BASİT, PRO} × {açık, koyu}; modGercek = istenen mod 112/112. Hücre = K1 + K3 taşan/kırık + K5 TRY + K7 + K12 + yatay kaydırma (SON değer).
| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K | K9 | ızgara |
|---|---|---|---|---|---|---|---|---|---|---|
| /urunler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /urunler yeni ürün | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 (¹) | 1 |
| /urunler/agac | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /urunler/etiketler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /vitrin | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /fiyatlandirma | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 (²) |
| /sayim | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /kurulum | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 (³) |
| /veri-ice-aktarim | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 (³) |
| /veri-disa-aktarim | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 (³) |
| /islemler (satış) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /siparisler | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /siparisler yeni | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| /siparisler/{id} | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
K5 (TRY kalıbı) 0/112, yatay kaydırma 0/112, K1 0/112, K11 hata bandı 0/112.
(¹) K9: açık yeni ürün penceresinin ALTINDA kalan sayfa yazısı (ölçerin örtü kontrolü yok — GOC-A #12 / GOC-B (¹) ile aynı sınır); görüntüde örtüşme yok.
(²) İlk BASİT açık turunda fiyat listesi/kampanya tohumlanmadan önce 0; sonraki 7 kombinasyonda 2 ızgara.
(³) Bu ekranlarda test kiracısında satır yok (boş durum) — ızgara görünümü bUnit/HTTP testleriyle kapsandı, tarayıcıda ölçülmedi (§ 9).
**Ölçümün bulduğu gerçek kusurlar (düzeltildi, `olcum2` 16/16 = 0):** (a) /urunler/agac 1280 (3 kombinasyon) K3 kırık: reyon ızgarasında şube hücresi "Merkez Şube" iki satır → `hucre-tek-satir`; (b) /siparisler yeni 1280 (4 kombinasyon) K7: ortak kabuktaki "(isteğe bağlı)" eki "Serbest tanım" etiketini kırıp etiketleri 26 px kaydırıyordu → `.sip-kalem-izgara` sütun alt sınırı 11rem.
**Read ile görülen görüntüler (4/4):** `olcum/foto/siparisler-yeni__PRO_aydinlik_1280.png` (ortak Gram/Adet/Tarih/Metin; K7 kusuru burada görüldü), `olcum/foto/urunler__PRO_aydinlik_1280.png` (A'nın canlı testte kaydettiği düzen tarayıcıda: "1 sütun gizli", GRAM ↓ azalan, ayar sona taşınmış; Durum sütunu tam, Aktif rozeti Stok düğmesinden ayrık; gram "5,000 g"), `olcum/foto/urunler-yeni__PRO_karanlik_375.png` (koyu 375 yeni ürün: zorunlu alanlar önde, 375 kart düzeni), `olcum/foto/islemler__PRO_aydinlik_1280.png` (satış ekranı: arama kutusu, fiş tuvali, ödeme şeridi değişmedi).

### 8.2 Temizlik
API 5160 (PID) ve vekil 5161 durduruldu, `lsof` boş; ölçüm Chrome'ları kapandı. `zsh kiraci-temizle.sh <id> --uygula` ×3 (01M4EPSTJKTQBTWFWGSD25PCSM kursuz ilk deneme, 01M4ERA98X0BCVRDZRG44CWCW3, 01M4ERASRRQXT53T3MEF08DWJH) + davet satırları silindi; kontrol: `PAKETGOC-C-TEST%` kiracı 0, davet 0. **tenants 19 = başlangıç 19.** QA-TEST-* kiracılarına dokunulmadı.

## 9. Açık kalanlar
1. **5 ham tablo** (§ 2.1): satış tuvali ×4 + Vitrin ayar tablosu — ortak ızgaraya "satır altı" ve "başlık şablonu (rozet)" gelince taşınır (Grid/* sahibi: GRID-1).
2. Sayım/eşik/yerleşim kesir iletisi ekranda KESIF-P1 cümlesi, sunucuda Core `SayiTam` cümlesi (aynı anlam) — `Sayi`'ya "Core tam sayı iletisi" bayrağı ortak bileşen kararı.
3. Rezerv/konsinye/değişim adet kutularında ADET tam sayı yalnız sunucuda (bileşen birimi bilmiyor).
4. Değişim yeni ürün fiyatı 0 (envanter C) ve hediye olmayan satış kaleminde 0 TL fiyat sunucuda hâlâ geçer — iş kuralı kararı (hediye ayrımı) gerektirir.
5. Kurulum/içe/dışa aktarım ızgaraları tarayıcıda verili hâlde ölçülmedi (test kiracısında satır yok) — bUnit kapsamında.
6. Sipariş iptal/geri geçiş gerekçe asgarisi 10 (proje K30 20) — sunucu sabiti değişikliği ayrı karar.
7. `Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` (0 bayt, başka paket) ağaçta — dokunulmadı.
8. `/islemler` "Son satışlar" kartı canlı kiracıda satış/iade/takas yapılmasına rağmen "henüz satış kaydı yok" gösterdi (görüntü 4) — bu paketin değiştirmediği akış (FisSonSatislar'da yalnız adet biçimi değişti); ayrı inceleme önerilir.
9. `Yasal4Http Mutabakat` ve `TevsikIhlalDb K16` testleri gün sınırında (İstanbul–UTC) kararsız (§ 7).

## 10. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Izgara düzeni GRID-1 sözleşmesiyle (`kullanici_tercihleri`: PATCH koru/null sil, DELETE "Varsayılana dön", denetim izi). Ürün PATCH (koru/null sil) sözleşmesi aynen; yeni kurallar yalnız GELEN değere uygulanır (taş kuralı kayıtlı+gelen birleşimine). Sipariş PATCH'inde kayıtlı (zamanla geçmişte kalmış) söz günü düzenlemeyi kilitlemez. Finansal kayıtlarda yeni silme yolu açılmadı.

## 11. Değişen dosyalar (bu paket)
Api: `Modules/Urun/UrunModulu.cs`, `Modules/Urun/UrunAdetKapisi.cs` (YENİ), `Modules/Satis/SatisModulu.cs`, `Modules/Satis/SatisModulu.P21.cs`, `Modules/Satis/IskontoHesabi.cs`, `Modules/Siparis/SiparisModulu.cs`, `Modules/Sayim/SayimModulu.cs`, `Modules/Kurulum/AcilisKaynaklari.cs`, `Modules/Kurulum/AcilisServisi.cs`, `Modules/Yerlesim/YerlesimServisi.cs`, `Modules/Fiyat/FiyatModulu.cs`.
Core: `Domain/Girdi/GirdiHatasi.cs` (AlanSinirlari), `Domain/IzgaraTercihi.cs` (BilinenEkranlar).
Web: `Pages/{Urunler,UrunAgaci,UrunEtiketleri,Vitrin,Fiyatlandirma,Sayim,Kurulum,VeriIceAktarim,VeriDisaAktarim,Islemler,Siparisler,SiparisDetay}.razor`, `Bilesenler/Fis/{FisKalemGirisi,FisIskontoSatiri,FisTransfer,FisTakas,FisTakasSatici,FisHurdaAlis,FisOdemeKasa,FisSonSatislar,FisDigerAvans,FisDigerDegisim,FisDigerKonsinye,FisDigerRezerv,FisDigerSiparis}.razor`, `Bilesenler/AltinBacagiPaneli.razor`, `Bilesenler/FiyatListesiUrunPaneli.razor`, `Bilesenler/Form/{SayisalAlan.cs,AnlamsalSayilar.cs}`, `Servisler/Bicim.cs`, `wwwroot/css/zo-islem.css`.
Denetim: `dotnet/denetim.sh` (5 tavan indirildi).
Testler: `GocCTestleri.cs` (YENİ) + `TestOrtami.cs` (`UrunuGramBirimiYap`) + § 2.8 listesi.
Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Urunler, UrunAgaci, UrunEtiketleri, Vitrin, Fiyatlandirma, Sayim, Kurulum, VeriIceAktarim, VeriDisaAktarim, Islemler, Siparisler, SiparisDetay (+ Fis/*, AltinBacagiPaneli, FiyatListesiUrunPaneli). Alan dışı dokunuşlar (gerekçe § 1.3): Form/SayisalAlan+AnlamsalSayilar, Servisler/Bicim.cs, css/zo-islem.css.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K1 ham input/table kalmaz, sayaçlar azalır | KARŞILANDI (5 gerekçeli tablo istisnası) | input/select/date 0; HAM_TABLO 58→35, GIRDI_TUR 9→2, HAM_TARIH 6→5, DTO 17→10, UZUNLUK 86→30, HAM_INPUT 23→13, HAM_SELECT 17→14 |
| K2 her açık için sunucu 400 testi + ekran alan altı, aynı sözlük anahtarı | KARŞILANDI (§ 9.2 ek not) | GCH01–GCH07, GCS04–GCS08, GCE01–GCE02; canlı § 8 iletileri ekran Core metinleriyle aynı |
| K3 mutasyon ≥3 kural, cmp AYNI | KARŞILANDI | 10/10 kırmızı, 10/10 AYNI |
| K4 canlı: taş>gram, kesirli adet, takas 0, %101, eksi iskonto, 61/51 barkod, geçmiş söz günü, eksi alış maliyeti, harf, 3000 yılı red; satış/takas/hurda/iade akışı SQL; geçerli kayıt; ızgara A kalıcı B varsayılan | KARŞILANDI | § 8 39/39 + SQL; tarayıcıda A düzeni görüntüde |
| K5 başsız Chrome 1280+375 · BASİT+PRO · açık+koyu, Read ≤ 4 | KARŞILANDI | 112 + 16 ölçüm; 2 gerçek kusur bulundu/düzeltildi; Read 4 |
| K6 build 0/0, denetim TEMİZ (P-30 hariç), tam süit 0 yeni kırmızı | KARŞILANDI | 0 Uyarı 0 Hata; denetim yalnız P-30; 11348/11351, tek kırmızı saat dilimi (§ 7), yeni kırmızı 0 |
| Devirler (3 tasarım + RAPOR-4) | KARŞILANDI | § 1.2 devirler, § 1.1 RAPOR-4, görüntü 2–3 |
| TCKN tek kaynak | KARŞILANDI | `TcKimlikNo` (KimlikNoDogrulayici); yeni kural yazılmadı |
| Satış ekranı davranışı değişmez + akış canlı | KARŞILANDI | satış/iade/takas/hurda SQL § 8; satış tuvali tabloları korunarak bırakıldı; görüntü 4 |

## 13. Kurucuya / mali müşavire
- Bu paket mevzuat hükmü eklemedi. **Sipariş söz günü en çok 2 yıl**, **kampanya yıl penceresi 1900–2100**, **ADET birimde tam sayı** İŞ KURALIdır (envanter K8c/K8/K14), mevzuat değildir.
- Hurda/takas satıcı adresi ≥ 10 karakter (K21) TEYİTSİZ olduğundan uygulanmadı (VUK md.234 yalnız adresin varlığını ister — mevcut kapı korunuyor).

---

## 14. TUR 2 — hasım doğrulayıcı bulgularının kapanışı (09.10.2026 01:40–02:10)

### 14.1 Bulgu → düzeltme
| Bulgu | Önem | Kök neden (doğrulandı) | Düzeltme | Dosya |
|---|---|---|---|---|
| İçe aktarım açılış stoğu ADET birimli ürüne kesirli adet yazıyor (#6 · K14) | KRİTİK | `AcilisStokKaynagi.Dogrula` yalnız `a <= 0 \|\| OndalikAsar(a,3)`; `TopluDogrula` ürünü birimsiz seçiyordu; `AcilisYazici.StokYaz` birim bilmiyordu | (1) `TopluDogrula` `u.Birim`'i de seçer; ADET birimde `AdetKurali` (Core — ekran `<Adet>` ve `UrunAdetKapisi` ile AYNI kural) bozuksa **kuru provada satır hatası** `ADET_GECERSIZ` + sözlük iletisi ("'KOD' adetle satılan ürün: Küsuratlı yazılamaz; tam sayı girin."); GRAM birimde kesir serbest. (2) Son savunma: `StokYaz` yuvarlamadan ÖNCE `UrunAdetKapisi.Dogrula(urun.Birim, adet, "adet")`. (3) `GOC47BSinirTestleri.Dg3_uctan_uca_…` K14 ile çelişen fikstür: ürün `TestKurulum.UrunuGramBirimiYap` ile GRAM birimli yapıldı (kuruş/has ölçüsü aynen sınanır). (4) `GOCCSinirVtTestleri.Ice_aktarim_…_YAZILMAZ` Skip kaldırıldı → **yeşil** | Iceaktarim/DefterKaynaklari.cs, Kurulum/AcilisYazici.cs, Tests/Dogrulayici/GOC47BSinirTestleri.cs, Tests/Dogrulayici/GOCCSinirTestleri.cs |
| /kurulum "Bu adıma git" bağlantısı "Bu adımı atla" düğmesinin altında (K9) | ORTA | Hasımın teşhisi doğru: iki öğe aynı `grid-area: eylem` hücresine ayrı ayrı konuyordu (toplu3 T3-7 ≤640, toplu4 T4-12b ≥641). Tur 1 ölçümünde K9=0 çıkmasının sebebi: tur 1 kiracısında kasa açılmıştı (KASA_BANKA bitti → atla düğmesi yok). | Eylemler TEK sarmalayıcıda: `<div class="kur-adim-eylemler" id="kur-adim-{Anahtar}-eylemler">` (yeni id; mevcut `-git`/`-atla` id'leri değişmedi); toplu4.css: sarmalayıcı `grid-area: eylem`, esnek + sarar (≥641: 10rem sütunda alt alta); ≤640 `flex-direction: column` (ilk ölçümde yan yana iki düğme adı "Kasa ve banka açılışı"nı 375'te kırıyordu — K3 kırık 1 → 0). Mantık eşdeğer: git/şimdi-gir koşulu aynen, atla koşulu `!Yakinda && Zorunlu && !Bitti` (eskiden else-if zinciri), sayı metni `!Yakinda && (!Zorunlu \|\| Bitti)` | Pages/Kurulum.razor, wwwroot/css/toplu4.css |
| İçe aktarım ürün kartı kuru provası taş ≥ brüt / eksi tutar göstermiyor | DÜŞÜK | `UrunKaynagi.Dogrula` yalnız `DtoDogrulayici.IlkHata` + `TasKapisi` | `Dogrula` sonunda `UrunServisi.GocCKapisi` (tek kaynak) çağrılır, `ApiException` → satır hatası `ALAN_GECERSIZ` (TAS_KOKENI ile aynı kalıp). Canlıda prova: "Taş ağırlığı brüt gramdan küçük olmalı." satır 2 | Iceaktarim/Kaynaklar.cs |
| 400 gövdesinde `message` bazen dizge bazen dizi | DÜŞÜK | (doğrulayıcı: ayrı paket) | **Bu pakette yapılmadı** — ApiException ↔ model doğrulama yanıtının tek biçime bağlanması tüm uçları etkiler; hasım "ayrı paket" dedi → § 14.8 açık kalan | — |

### 14.2 Yeni / güncellenen testler (tur 2)
- **YENİ `Kuyumcu.Tests/GocCTestleriTur2.cs` — 4 test:** `GocCTur2SafTestleri.GCT2_S01_urun_karti_provasi_tas_brute_esit_ya_da_buyukse_satir_hatasi_sozluk_metniyle` (taş 5 = brüt 5 ve 6,5 > 5 → `ALAN_GECERSIZ` + `TasBrut` sözlük metni; 4,99 ve taşsız geçer) · `GCT2_S02_urun_karti_provasi_eksi_satis_fiyati_satir_hatasi` · `GocCTur2VtTestleri.GCT2_V01_acilis_stogu_provasi_ADET_birimde_kesir_SATIR_HATASI_GRAM_birimde_gecer` (3 satır: ADET 1,5 → satır 2 `ADET_GECERSIZ` + `SayiTam` metni; GRAM 1,5 ve ADET 2 geçer; hataliSatir = 1) · `GCT2_V02_StokYaz_son_savunma_ADET_birimde_kesirli_acilis_400_hicbir_satir_yazilmaz`.
- **`KurulumEkranTestleri.GOCC_T2_eksik_kasa_adiminda_git_ve_atla_AYNI_eylem_sarmalayicisinda`** (bUnit, DurumEksik fikstürü: KASA_BANKA sarmalayıcısının çocukları SIRAYLA `-git`, `-atla`; li'nin doğrudan a/button çocuğu yok; toplu4.css sarmalayıcı bloğunda `grid-area: eylem` + `flex-wrap: wrap`).
- `GOCCSinirVtTestleri.Ice_aktarim_acilis_stogu_ADET_birimli_urune_kesirli_adet_YAZILMAZ` Skip kaldırıldı (yeşil); `GOC47BSinirTestleri.Dg3_uctan_uca_…` GRAM birimli ürüne çevrildi (beklenen değerler değişmedi).
- Filtreli koşum (GocC + GOCCSinir + GOC47BSinir + KurulumEkran + Toplu3/4/6 + SADE1 + IceAktarim + Acilis): **472/472**. CSS son düzeltmesinden sonra toplu4.css okuyan sınıflar (KurulumEkran, Toplu4, Toplu5) + GocC + Toplu3: **91/91**.

### 14.3 Mutasyon (tur 2; yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-C-t2/`, betik `GOC-C/t2/mut.sh`: kopya → `perl -0777 -pi` TEK eşleşme → derle → filtreli test → geri kopya → `cmp`)
| Kod | Kural kapatıldı | Kırmızı test | cmp |
|---|---|---|---|
| MT2-1 | Prova: ADET birim kesir satır hatası (`… && false`) | GocCTur2VtTestleri.GCT2_V01_… | AYNI |
| MT2-2 | StokYaz son savunma satırı silindi | GocCTur2VtTestleri.GCT2_V02_… | AYNI |
| MT2-3 | Ürün kartı provası GocCKapisi'ne taş yerine 0 | GocCTur2SafTestleri.GCT2_S01_… | AYNI |
| MT2-4 | toplu4.css sarmalayıcıdan `grid-area: eylem` kaldırıldı | KurulumEkranTestleri.GOCC_T2_… | AYNI |
| MT2-5 | Kurulum.razor sarmalayıcı sınıfı değişti | KurulumEkranTestleri.GOCC_T2_… | AYNI |
5/5 kırmızı, 5/5 cmp AYNI. Tur 1 tablosu (§ 5, 10/10) geçerliliğini korur (o kurallara tur 2'de dokunulmadı). Son mutasyondan sonra dokunulan dosyalar `touch` + temiz `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata**. (Not: ara bir TAM yeniden derlemede başka şeritlerin test dosyalarından 6 analizör uyarısı görüldü — SUBEKAPSAMASinir, KesifP2, SonKalanlar ×2, Yildiz2 ×2; hiçbiri bu paketin dosyası değil; artımlı son derleme 0/0.)

### 14.4 Tam süit (tur 2, BİR kez, `trx-pGOC-C/tam2.trx`)
`Toplam 11369 · Yürütülen 11367 · Geçti 11366 · Başarısız 1 · Atlanan 2`. Tek kırmızı: `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — tur 1 § 7'deki AYNI test, AYNI sebep (koşum İstanbul 02:0x = UTC önceki gün; test UTC gününü sorar). Tur 1 kırmızı listesiyle fark **0** → bu paketin YENİ kırmızısı **0**. Not: tam süit başladıktan sonra yalnız toplu4.css'e ≤640 `flex-direction: column` kuralı eklendi; CSS'i okuyan test sınıfları yeniden koşuldu (91/91, § 14.2).

### 14.5 denetim.sh (tur 2 son kod)
Yalnız `✗ P-30 DEMO_INSAN_SURESI` (kurucu kalemi, görev "P-30 hariç"). Sayaçlar değişmedi/artmadı: HAM_TABLO_ORTAK_GRID_DISI 35, GIRDI_TUR_GENEL_BILESEN 2, HAM_TARIH_GIRDISI 5, DTO_DECIMAL_RANGESIZ 10, EKRAN_SUNUCU_UZUNLUK_FARKI 30, HAM_INPUT 13, HAM_SELECT 14.

### 14.6 Canlı doğrulama (tur 2; API 5160 Development, `kuyumcu_net`; kiracı `PAKETGOC-C-TEST t2 …` = 01M4EW3WAMRHP43MRZZMNJRVPX; betik `GOC-C/t2/canli_t2.py` = tur 1 betiğinin tamamı + tur 2 ekleri; sonuç `GOC-C/t2/canli-sonuc.json`) — **UYUMSUZ 0 / 45**
Tur 1'in 39 adımı yeni kiracıda baştan sona yeniden koşuldu ve tamamı tuttu (satış 1 adet %10 → `SATIS adet=1.000 tl=900.00`; iade → `SATIS_IADE adet=1.000 tl=-900.00`; takas → `TAKAS adet=1.000 eskiHas=2.748000 tl=11756.00`; hurda → `HURDA_ALIS has=1.374000 tl=-4122.00`; sipariş `S-2026-000001 soz=2026-10-16`; taş>gram, eksi alış maliyeti, 51 karakter/Türkçe barkod, harf, kesirli adet, takas 0, %101/−5/10,555 iskonto, geçmiş/3000 söz günü, 0001/3000 kampanya → 400 + SQL'de kayıt yok; ızgara tercihi A kalıcı `len=99`, B'de yok). Tur 2 ekleri:
| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| ADET ürün + GRAM ürün oluştur | 201 ×2 | 201 ×2 | — |
| İçe aktarım prova `GC-IA-ADET,1.5` + `GC-IA-GRAM,1.5` | 200, hatalı 1 | 200, hatalı 1 | satır 2 `ADET_GECERSIZ` "'GC-IA-ADET' adetle satılan ürün: Küsuratlı yazılamaz; tam sayı girin." |
| İçe aktarım uygula | 200 | 200 | `durum=UYGULANDI uygulananSatir=1 hataliSatir=1` |
| SQL: ADET üründe kesirli stok | 0 | 0 | `stoklar` ADET ürün satırı = 0 |
| SQL: GRAM ürüne 1,5 | adet=1.500 | adet=1.500 | — |
| Ürün kartı provası taş 6 ≥ brüt 5 (+ geçerli satır taş 1) | 200, hatalı 1 | 200, hatalı 1 | satır 2 `ALAN_GECERSIZ` "Taş ağırlığı brüt gramdan küçük olmalı." |
| SQL: prova ürün yazmadı | 0 | 0 | `urunler kod LIKE 'GC-IA-T%'` = 0 |

### 14.7 Gerçek başsız Chrome — /kurulum, "kasa açılışı EKSİK" yeni kiracı (vekil 5161, CDP 9711, `GOC-C/olc.mjs SADECE=kurulum`; kiracı `PAKETGOC-C-TEST t2k …` = 01M4EW4MTTVGD4NKAYB7QADPJ8, yalnız kayıt — kasa yok)
| Ölçüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K |
|---|---|---|---|---|---|---|---|---|
| ilk (`t2/olcum`) K9 binen | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| ilk K3 kırık | 0 | 0 | 0 | 0 | **1** | **1** | **1** | **1** |
| son (`t2/olcum2`) K1/K3/K5/K7/K9/K11/K12/yatay | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
modGercek = istenen mod 8/8. İlk ölçümün K3'ü ("Kasa ve banka açılışı" 375'te iki satır — iki düğme yan yana eylem sütununu genişletiyordu) ≤640 `flex-direction: column` ile kapandı. K2 (kart sağ %) 1280 98 / 375 BASİT 97 / 375 PRO 120 — tur 1 ile birebir aynı, yatay kaydırma 0.
**Read ile görülen görüntüler (tur 2: 2):** `t2/olcum2/foto/kurulum__PRO_karanlik_375.png` (hasımın kusur gördüğü kombinasyon: Kasa ve banka satırında "Bu adıma git" ve altında "Bu adımı atla" ikisi de görünür, örtüşme yok; ad tek satır) · `t2/olcum2/foto/kurulum__BASIT_aydinlik_1280.png` (eylem sütununda iki düğme alt alta, diğer satırlar hizalı).

### 14.8 Temizlik (tur 2)
API (dotnet run PID 21483 + dinleyen 21621) ve vekil durduruldu; `lsof` 5160/5161 boş; Chrome `olc.mjs` içinde kapandı. `zsh kiraci-temizle.sh <id> --uygula` ×2 (01M4EW3WAMRHP43MRZZMNJRVPX, 01M4EW4MTTVGD4NKAYB7QADPJ8) + 2 davet satırı silindi; kontrol: `PAKETGOC-C-TEST%` kiracı 0, `GOCCDAVET%` davet 0. **tenants 18 = tur 2 başlangıcı 18** (tur 1 sonundaki 19'dan farkı başka bir şeridin temizliği; QA-TEST-* kiracılarına dokunulmadı).

### 14.9 Açık kalanlar (tur 2 sonrası, § 9'a ek)
1. **400 gövdesi `message` biçimi** (servis yolu dizge, DTO öznitelik yolu dizi) — ayrı paket (ApiException + model doğrulama yanıtı tek biçim). Sahibi: ortak hata sözleşmesi (Taşkın), GOC-C kapsamı dışı.
2. Tam süitteki tek kırmızı `Yasal4HttpTestleri.Mutabakat_…` — sahibi YASAL-4 testleri (UTC/İstanbul gün sınırı); bu paketin değil.
3. § 9'daki 1–9 maddeleri aynen açık (5 ham tablo, Sayi tam sayı iletisi, rezerv/konsinye adet ekranda birim bilgisi, 0 TL hediye ayrımı, sipariş gerekçe asgarisi, `.cs.tmp`, son satışlar kartı).

### 14.10 Değişen dosyalar (tur 2)
Api: `Modules/Iceaktarim/DefterKaynaklari.cs`, `Modules/Iceaktarim/Kaynaklar.cs`, `Modules/Kurulum/AcilisYazici.cs`. Web: `Pages/Kurulum.razor` (tur 1'de de açılmıştı — aynı paket, hasım bulgusu gerekçesi; TEK DOKUNUŞ ihlali değil), `wwwroot/css/toplu4.css` (alan dışı CSS dosyası — Kurulum adım ızgarası kuralları bu dosyada olduğu için en küçük ekleme; yalnız `#kur-adimlar … .kur-adim-eylemler`, belirteç `var(--bosluk-2)`, sabit renk yok). Testler: `GocCTestleriTur2.cs` (YENİ), `KurulumEkranTestleri.cs` (+1 test), `Dogrulayici/GOCCSinirTestleri.cs` (Skip kaldırıldı), `Dogrulayici/GOC47BSinirTestleri.cs` (Dg3 GRAM birim).
