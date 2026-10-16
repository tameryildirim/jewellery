# PAKET UX-4d — API tutarlılık ve hata mesajı düzeltmeleri (QA turu 1 bulguları)

> Uygulayıcı: Taşkın (03 Backend) + Ertuğ (03 Frontend) · Dalga 2 · 2026-09-16
> Kapsam: 24 madde (fikstür bulguları 1-7 · tarayıcı bulguları 8-15 · çizelge 16-23 · MÜHÜR 4 eki 24)
> **Durum: BİTTİ** — 24/24 madde uygulandı, 82 yeni test, 27 mutasyon kanıtı, canlı doğrulama 5198'de yapıldı.

---

## 1. Kapılar (kanıtla)

| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| `dotnet test` tam süit | **1394 / 1394 başarılı · 0 başarısız · 0 atlanan** (önce: 1312) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |
| Migration | **gerekmedi** — hiçbir şema değişikliği yok (açılış stoğu mevcut `stoklar` + `hareketler` yolunu kullanır) |
| Canlı doğrulama | 5198'de 10 senaryo (aşağıda), SQL kanıtıyla |
| Test kiracısı | `PAKETUX-4d-TEST Kuyumcu` oluşturuldu ve **tamamen silindi** (33 tabloda ARTIK_VAR=0, `tenants` 7 → 8 → **7**) |
| 5199 | **dokunulmadı** (PID 65793 ayakta kaldı) |
| COMMIT | **yok** |

`denetim.sh` çıktısı (özet): IgnoreQueryFilters 0 · Find/FindAsync 0 · ExecuteUpdate/Delete 4 (beyaz liste) ·
Hareketler.Add 1 (yalnız HareketFabrikasi) · IKiraciVarlik 33 · HasQueryFilter 2 · CORS beyaz liste → **DENETİM TEMİZ.**

---

## 2. Madde madde yapılanlar

### (1) Ürün kartında açılış stoğu yolu yok
`UrunOlusturDto.AcilisStogu { SubeId?, Adet }` eklendi. **İkinci yazma yolu AÇILMADI**: değer, ürün
modülünün zaten var olan `StokSayimGuncelle` yoluyla yazılır → deftere `HareketFabrikasi` üzerinden
`SAYIM_DUZELTME` satırı düşer, maliyet partisi "maliyeti bilinmeyen" olarak açılır (kâr şişmez).
Açıklama `"Açılış stoğu: N adet (ürün kartı)"`. Şube **sorulmaz** (UX-S22): gövde → oturum şubesi →
işletmenin tek aktif şubesi; birden çok şube varsa **tahmin edilmez**, 400 döner (Anayasa md.1).
Ekran: `Urunler.razor` yeni ürün modalında `#u-acilis-stok` + `#u-acilis-stok-ipucu`.

### (2) MASAK "Açıklama en az 20 karakter (şu an 0)" hangi alanı kastettiğini söylemiyor
Mesaj: `"MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an N)"`.

### (3) "Geçerli bir kimlik belgesi türü" mesajı kabul edilen değerleri listelemiyor + DTO'da regex yok
`Kuyumcu.Core.Domain.KimlikBelgesi` **tek kaynak** oldu (liste + `Desen` + `Ad` + `Mesaj`).
`MasakBilgiDto.BelgeTuru`'ya `[RegularExpression(KimlikBelgesi.Desen)]`; kural motoru da aynı mesajı kullanır.
`Kuyumcu.Api.Data.KimlikBelgeTuru` artık Core'a delege eder (eski çağrı yüzeyi korundu).

### (4) Kümülatif eşik mesajı ilk denemede toplamı vermiyor
Koşullu not kaldırıldı; kümülatif ölçü verildiğinde **her zaman** tek biçim:
`"(Bu cariyle bugün kümülatif X TL — eşik 185.000 TL; bu işlem Y TL)"`.

### (5) Hurda alışta NAKİT varsayılanı tevsik haddi yüzünden geçersiz
**Sunucu kuralı DEĞİŞMEDİ.** Ekrana öneri eklendi: `Kuyumcu.Web.Servisler.TevsikOneri.Gerekli(...)`
(tek kaynak, saf) → POS'ta `#islem-tevsik-ipucu` "Bu tutar tevsik haddini (₺30.000,00) aşıyor — nakit
ödeme reddedilir. HAVALE/EFT veya kart seçin." Had, sunucunun önizleme ucundan tazelenir.

### (6) Banka/kasa yön sözlüğü tutarsız
`Kuyumcu.Core.Domain.HareketYonu` tek sözlük: `KasaDesen`/`BankaDesen`/`KasaMesaj`/`BankaMesaj`.
İki DTO da buradan beslenir; **sessiz eşleme YOK** — kasaya `GELEN` gelirse 400 ve mesaj hem doğru
değerleri hem karşı sözlüğü söyler. `dto.Yon == "GIRIS"` gibi çıplak dizgeler sabitlere çevrildi.

### (7) `/api/v1/*` altında bilinmeyen yol 200 index.html dönüyor
`Program.cs`: `app.MapFallback("/api/{**kalan}", …)` → **404 + JSON** `{message,hata:"uç bulunamadı",error,statusCode,yol}`.
SPA geri dönüşü `/api` dışındaki yollarda aynen duruyor. (Mehmet: API yüzeyi HTML sızdırmaz.)

### (8) DataAnnotations mesajları İngilizce/teknik
**Tek çıkış noktasında** çözüldü: `Kuyumcu.Core.I18n.DogrulamaSozlugu` (alan adı sözlüğü + İngilizce
varsayılan mesaj kalıpları → Türkçe) + `Program.cs` `InvalidModelStateResponseFactory` ve
`ModelBindingMessageProvider`.
- 400 gövdesi artık `alanlar: { "email": ["…"], … }` sözlüğü de taşır (ekran alan altına yazabilsin).
- `Range(0, double.MaxValue)` → **"… 0 veya daha büyük olmalıdır"** (1,79E+308 gösterilmez).
- Ayrıca QA'nın adıyla saydığı alanlara elle Türkçe mesaj: cari e-posta/telefon/kredi limiti/vade/tür,
  kategori `Sıra ≥ 0`, şube telefonu, kasa/banka yönü, MASAK belge türü, ürün ayarı.
- İstemci tarafı: `ApiHata.Alanlar` sözlüğü taşınır.

**SAPMA ve gerekçesi:** spesifikasyon "TÜM DTO'larda Türkçe `ErrorMessage`" diyordu. 80'den fazla
özniteliğe elle mesaj yazmak yerine çeviri tek noktada yapıldı. Gerekçe (kodda da yazılı):
ASP.NET Core sunucu tarafında DataAnnotations yerelleştirmesini **yalnız `ErrorMessage` elle verilmiş**
özniteliklerde uygular (`AddDataAnnotationsLocalization` varsayılan İngilizce mesajları çevirmez —
denendi); elle 80 kopya yazmak hem tekrar üretir hem yeni DTO'da yine İngilizce sızdırırdı. Bugünkü
ve gelecekteki tüm DTO'lar bu tek kapıdan geçer.

### (9) Manuel kur sonrası `/kur/gecmis` boş dönüyor
Kök neden: ekran `bitis=YYYY-MM-DD` gönderiyor, sunucu `Olusturuldu <= 00:00` uyguluyor ve **o günün
tamamını** dışarıda bırakıyordu. Saat bileşeni yoksa bitiş **günün sonuna** taşınır.

### (10) Uyum > Şüpheli işlem: "Gönderildi"/"Kapat" hiçbir istek atmıyor
Kök neden: değerler `window.prompt` ile isteniyordu; prompt engellenince `null` dönüp akış **sessizce**
bitiyordu (DoD EK md.1 ihlali). `prompt` **kaldırıldı**; iki geri alınamaz durum geçişi için ortak onay
modalı (DoD EK md.4) + alan altı Türkçe mesaj (referans zorunlu / gerekçe n/20). Sunucu tarafında
`SupheliGonderildi` ve `SupheliKapat` artık `DenetimServisi.YazmaKaydet` çağırıyor (Mühür 2).

### (11) Gün sonu mutabakatı çelişkili (SATIŞ FATURASI ₺68.000 ama Faturalı 0)
Kök neden: `hareketler.fatura_no` yalnız fatura **fişten** kesilince damgalanır; elle kesilen fatura
hiçbir satışa bağlanmaz ve rapor bu kümeyi **hiç göstermiyordu**. İki düzeltme:
1. "Faturalı" sayılmak için damga yetmez — damganın işaret ettiği fatura **iptal edilmemiş** olmalı.
2. **TEK FARK TANIMI** (kodda ve burada aynı cümle):
   `FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar`
   Üç sapma türü (faturasız satış · tutar sapması · bağsız fatura) bu tek formülde görünür.
   Yanıta `faturaEslesme { bagliSayisi, bagliTl, bagimsizSayisi, bagimsizTl, faturasizTl, farkTanimi,
   bagimsizFaturalar[] }` eklendi; `mutabik` bağsız faturayı da sayar.

### (12) Aylık "NET HAS GRAM 8,12" günlük 53,04'ten küçük
Aylık özet artık `netHasGramHareketi = tüm hareketlerin HasGrami toplamı` döndürüyor — **günlük özetle
birebir aynı ifade**. (Aylık ekran eskiden satış/alış farkını "net has" diye gösteriyordu.)

### (13) Cari "Pasife Al" onaysız + aktife alma yolu
- Ekran: `Cariler.razor` ortak onay modalı (`#c-pasife-al` → `#c-pasif-onayla`/`#c-pasif-vazgec`).
- Sunucu: **kasada** `aktif` alanı yoktu → `KasaGuncelleDto.Aktif` eklendi (pasife alma kapısı
  `DeaktifEt` ile **aynı kural**: bakiyesi sıfır olmayan kasa pasife alınamaz), `DELETE` idempotent
  yapıldı, iki geçiş de denetim izine düşüyor. Cari · banka hesabı · ürün zaten `aktif` taşıyordu.

### (14) `/kilavuz` sayfası BOŞ
Kök neden: `<iframe src="/kilavuz.html">` — sunucu `X-Frame-Options: DENY` + CSP `frame-ancestors 'none'`
gönderdiği için tarayıcı çerçeveyi engelliyordu. **CSP gevşetilmedi** (Mehmet). iframe kaldırıldı;
sayfa artık açıklama + 13 bölüm bağlantısı + "yeni sekmede aç" düğmesi çiziyor
(`target="_blank" rel="noopener noreferrer"`). HTML gövdesi fetch edilip `MarkupString` ile
**basılmıyor** (enjeksiyon yolu açılmadı).

### (15) Süresi dolmuş oturumda 3× `/auth/yenile` + 3× `/auth/cikis` + "Beklenmeyen bir hata"
`ApiIstemci`: yenileme **tek uçuş** (single-flight, `SemaphoreSlim` + paylaşılan `Task`), oturum
**tek kez** düşürülür, **tek kez** `/giris`'e yönlendirilir ve çağrılara `OturumDustuHatasi`
(`ApiHata`'dan türer, 401) fırlatılır → mevcut `catch (ApiHata)` blokları yakalar, Blazor'un genel
hata bandı çıkmaz. Mesaj: "Oturum süresi doldu — lütfen tekrar giriş yapın."

### (16) `/raporlar/vade-takibi` üç kart ₺0,00 ama 76.000 ₺ açık alacak var
Kök neden: vadesi ufuktan **uzak** borç hiçbir kovaya düşmüyordu. `ufukOtesiToplam` ve `acikToplam`
(ufuk seçicisinden bağımsız) eklendi; cari satırında `ufukOtesiTutar` + `acikToplamTutar`.
Ekranda iki yeni kart, ikincisi "(bugün itibarıyla — ufuk seçiminden bağımsız)" alt metniyle (UX-S33).

### (17) `/faturalar` kalem miktarı "1.000 g ADET"
Ortak biçimlendirici `Bicim.Miktar(n, birim)`: GRAM → `"1.000 g"`, ADET → `"1 adet"`. Ham kod ekrana
basılmaz (UX-S31).

### (18) `/uyum` belge no araması 0 sonuçta eski listeyi gösteriyor
Kök neden **sözleşme uyuşmazlığıydı**: uç `{adet, toplamTutarTl, …, kayitlar}` döndürüyor, istemci
`List<KimlikTespitSatir>` okumaya çalışıp her aramada JSON hatası alıyor ve eski liste ekranda kalıyordu.
- Sunucu: `kayitlar` artık **liste ucuyla aynı alan kümesini** taşıyor (belge no MASKELİ kalır).
- İstemci: `BelgeAramaSonucu` tipi + arama özeti (`#uy-arama-ozet`) + 0 sonuçta boş durum.

### (19) `/isletme` şube telefonu "abc" 200 kabul
`Kuyumcu.Core.Domain.Iletisim.TelefonDesen/TelefonMesaj` tek kural; şube **ve** cari aynı kuralı kullanır.

### (20) Pusula belgesi yalnız görüntülemede denetim izine "yazdırıldı" yazıyor
`GET {id}/belge` → **"görüntülendi"**; yeni `POST {id}/yazdirildi` → **"YAZDIRILDI (basım)"**.
Controller rol kapısı (`SAHIP·MUDUR·MUHASEBE`) aynen geçerli.

### (21) `/raporlar` aynı ay için iki farklı kâr (−31.800 vs +20.200)
Aylık özetteki `karTl = satış − alış` bir kâr değil, mal alım/satım **nakit farkı**ydı. Tek yetkili tanım
`KarZararServisi`: `karTl` oradan gelir, `karKaynagi = "KAR_ZARAR_SERVISI"`, eski sayı adıyla
(`satisAlisFarkiTl`) korunur, tam kâr-zarar özeti `karZarar` altında döner. Servis verilmezse
`karTl = null` + `karKaynagi = "HESAPLANAMADI"` — **sıfır yazılmaz** (Anayasa md.1).

### (22) Aynı istek 2× gidiyor (`/denetim-izi` filtresi, `/raporlar/kar-zarar` geçersiz aralık)
İki ekranda da "aynı sorgu üst üste çalıştırılmaz" koruması (`sonSorgu` anahtarı). Hata alınırsa
anahtar sıfırlanır (yeniden deneme yolu kapanmaz).

### (23) `/denetim-izi` ters tarih aralığı sessizce kabul, özet 0
Sunucu: `DenetimOkumaServisi.Listele` ve `Ozet` → **400** "Bitiş tarihi başlangıçtan önce olamaz
(başlangıç …, bitiş …)". Ekran: istek **hiç atılmaz**, alan altına Türkçe mesaj (`#f-aralik-hata`,
`#kz-aralik-hata`).

### (24) MÜHÜR 4 eki — sözleşmesiz kur kaynakları (K13)
**DOĞRULAMA (spesifikasyonun sorduğu):** kaynak seçimi bugün **kodda sabit listeden** geliyordu —
`KurKaynakDefteri` kurucusunda üç adaptör (`Altinkaynak`, `AnkaraAltinBorsasi`, `Truncgil`) koşulsuz
kuruluyor; yapılandırma yalnız `Kur:VarsayilanKaynak` ile **hangisinin varsayılan** olacağını seçiyordu.
Artık tek anahtar: **`Kur:SozlesmesizKaynaklarAcik`** (bool).
- Anahtar yoksa **KAPALI** (tutucu taraf; sözleşmesiz kaynak kendiliğinden açılmaz).
- `appsettings.json` → `false` · `appsettings.Development.json` → `true` (QA/demo).
- Kapalıyken adaptör **hiç kurulmaz** (HTTP çağrısı yapacak nesne ortada yok; ikinci okuma yolu yok),
  `VarsayilanKod = "MANUAL"`, dış çekim denemesi **503** + "Kuru elle girin: Kur ekranı → Manuel kur".
- `GET /kur/kaynaklar` artık `sozlesmesizKaynaklarAcik`, `seciliSozlesmesiz`, `manuelKod` döner.
- `Kur.razor`: bayrak kapalıysa `#kur-dis-kapali`, seçili kaynak sözleşmesizse `#kur-sozlesmesiz`
  ("sözleşmesiz kaynak — yalnız pilot") şeridi; mevcut `.uyari-kutu` deseni, satır içi stil yok.
- TCMB resmî döviz (`TcmbDoviz`) ve MANUAL yolu olduğu gibi duruyor.

---

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Açılış stoğu (yeni alan) | **SALT_OKUNUR** | Kaydedildikten sonra ürün kartından değişmez; stok yalnız alış, satış ve sayımla değişir. Ekranda `#u-acilis-stok-ipucu` bunu yazar. |
| Kasa | `PATCH /kasalar/{id}` `{aktif}` + `DELETE` (idempotent) | Pasife alma kapısı: bakiye ≠ 0 ise reddedilir; iki geçiş denetim izine düşer. |
| Cari | `PATCH /cari/{id}` `{aktif}` + `DELETE` (onay modalıyla) | Zaten vardı; ekrana onay eklendi. |
| MASAK şüpheli bildirim | `PATCH {id}/gonderildi`, `PATCH {id}/kapat` | Durum geçişi tek yönlü (ACIK → GONDERILDI/KAPALI); **kayıt silinmez** (8 yıl saklama) — değişmezlik gerekçesi ekranda onay metninde yazılı. |
| Gider pusulası | **SALT_OKUNUR** + yeni `POST {id}/yazdirildi` | Basım bir *olay*tır, veri değişikliği değil; pusulanın kendisi VUK gereği düzeltme fişiyle düzeltilir (UX-1 Faz B/C). |
| Kur kaydı (MANUAL) | Yeni manuel kayıt | Geçmiş kur düzeltilmez; UX-1 Faz B/C'de DegismezIpucu eklenecek (açık kalan 3). |

**Finansal varlıklarda hard DELETE eklenmedi** — yeni yıkıcı uç yok; kasa `DELETE`'i pasifleştirmedir.

---

## 4. Test sayıları

| | Önce | Sonra |
|---|---|---|
| Toplam test | 1312 | **1394** |
| Başarısız | 0 | **0** |
| Yeni test | — | **82** |

Yeni dosyalar: `UX4dSafTestleri.cs` (29) · `UX4dServisTestleri.cs` (20) · `UX4dHttpTestleri.cs` (14) ·
`UX4dEkranTestleri.cs` (16) · `UX4dOturumTestleri.cs` (3).

**Kasıtlı davranış değişikliği nedeniyle güncellenen 4 mevcut test** (gizlenmiyor, hepsi gerekçeli):
- `PusulaBelgeTestleri.Belge_OkumaIziDuser` — "yazdırıldı" → "görüntülendi" (madde 20).
- `CariEkranTestleri.Pasife_al_DELETE_gonderir` — artık onay modalından geçiyor (madde 13).
- `MasakBaglantiliIslemTestleri.Ayni_gun_bolunen_satislar_TOPLANIR…` — yeni tek mesaj biçimi (madde 4).
- `MutabakatTestleri` (2 test) — yeni FARK tanımıyla yeniden okundu; **ikisi de aynı sayıyı** doğruluyor
  (5.000 ve 30.000), yani tanım değişikliği mevcut sinyali **kaybetmedi**; üstüne iptal-fatura ayrımı
  için yeni test eklendi.

---

## 5. Mutasyon tablosu (27 mutasyon — hepsi kırmızıya döndü)

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M1-1 | Açılış stoğu hiç uygulanmasın | `M1_Urun_kartindan_ACILIS_STOGU_girilince_stok_ve_DEFTER_satiri_dogar`, `M1_Birden_cok_aktif_sube_varsa_sube_TAHMIN_EDILMEZ_400_doner` | AYNI |
| M2-1 | Mesajdan alan adı çıkarılsın | `M2_MASAK_aciklama_mesaji_HANGI_ALANI_kastettigini_soyler` | AYNI |
| M3-1 | Belge türü mesajı değerleri saymasın | `M3_Kimlik_belgesi_mesaji_KABUL_EDILEN_DEGERLERI_sayar`, `M3_Gecersiz_belge_turunde_mesaj_KABUL_EDILEN_DEGERLERI_listeler` | AYNI |
| M4-1 | Kümülatif notu eski koşullu hâline dönsün | `M4_Kumulatif_mesaj_ILK_denemede_de_toplami_ve_esigi_verir` | AYNI |
| M5-1 | Eşik `>=` yerine `>` | `M5_Nakit_tevsik_haddini_asinca_BANKA_yolu_onerilir` | AYNI |
| M6-1 | Kasa yönü banka değerlerini de kabul etsin | `M6_Kasa_yonu_yalniz_GIRIS_CIKIS_kabul_eder_GELEN_reddedilir` | AYNI |
| M6-2 | Kasa DTO deseni birleştirilsin | `M6_Kasaya_GELEN_gonderilince_400_doner_ve_dogru_degerler_listelenir` | AYNI |
| M7-1 | `/api` geri dönüşü devre dışı | `M7_Api_onekinde_bilinmeyen_yol_404_JSON_doner_index_html_DEGIL` | AYNI |
| M8-1 | Çeviri kalıpları boşaltılsın | `M8_EmailAddress…`, `M8_MaxLength…`, `M8_Range_gercek_ust_sinir…`, `M8_Range_ust_siniri_double_MaxValue…`, `M8_Required…` (5 test) | AYNI |
| M9-1 | Bitiş yeniden `<= 00:00` | `M9_Kur_gecmisi_BITIS_gununde_olusan_kaydi_da_dondurur` | AYNI |
| M10-1 | "Gönderildi" düğmesinin `@onclick`'i kaldırılsın | `M10_Gonderildi_dugmesi_ONAY_modali_acar_ve_ISTEK_atar`, `M10_Referans_bos_birakilirsa…` | AYNI |
| M10-2 | Kapatma gerekçesi 20 karakter kuralı kalksın | `M10_Kapat_dugmesi_20_karakterden_kisa_gerekcede_istek_ATMAZ` | AYNI |
| M11-1 | "Faturalı" için iptal kontrolü kalksın | `M11_IPTAL_edilmis_faturanin_damgasi_satisi_FATURALI_yapmaz` | AYNI |
| M12-1 | Aylık net has yeniden satış−alış | `M12_Aylik_net_has_gram_GUNLUK_ile_AYNI_tanimdir` | AYNI |
| M13-1 | Kasa `aktif` geçişi uygulanmasın | `M13_Pasife_alinan_kasa_PATCH_ile_AKTIFLESTIRILEBILIR`, `M13_Bakiyeli_kasa_PATCH_ile_de_pasife_ALINAMAZ_kapi_ayni` | AYNI |
| M13-2 | Cari "Pasife Al" doğrudan istek atsın (onaysız) | `CariEkranTestleri.Pasife_al_DELETE_gonderir` | AYNI |
| M14-1 | Kılavuz yeniden iframe'e dönsün | `M14_Kilavuz_sayfasinda_IFRAME_YOKTUR`, `M14_Kilavuz_yeni_sekmede_acilir…` | AYNI |
| M15-1 | Tek uçuş kaldırılsın (`??=` → her çağrı yeni) | `M15_Ucu_paralel_401_TEK_yenileme_ve_TEK_cikis_uretir` | AYNI |
| M16-1 | `acikToplam`'dan ufuk ötesi çıkarılsın | `M16_Ufkun_OTESINDEKI_acik_alacak_toplamda_GORUNUR` | AYNI |
| M17-1 | ADET yeniden `Gram(n) + " ADET"` | `M17_Miktar_ADET_birimi_icin_adet_yazar_ve_g_YAZMAZ` | AYNI |
| M18-1 | Arama yanıtı eski 5 alana dönsün | `M18_Belge_no_arama_ucu_LISTE_UCUYLA_AYNI_alan_kumesini_dondurur` | AYNI |
| M18-2 | 0 sonuçta eski liste korunsun | `M18_Dolu_listede_arama_0_sonuc_verirse_ESKI_liste_EKRANDAN_KALKAR` | AYNI |
| M19-1 | Şube telefon deseni kaldırılsın | `M19_Sube_telefonu_abc_ARTIK_400_doner` | AYNI |
| M20-1 | Görüntüleme yeniden "YAZDIRILDI" yazsın | `M20_Pusula_belgesi_GORUNTULEME_ile_YAZDIRMA_ayri_denetim_olayidir`, `PusulaBelgeTestleri.Belge_OkumaIziDuser` | AYNI |
| M21-1 | Aylık kâr yeniden satış−alış | `M21_Aylik_kar_KAR_ZARAR_SERVISINDEN_gelir_satis_eksi_alis_DEGIL` | AYNI |
| M22-1 | Çift tetikleme koruması kalksın | `M22_Denetim_izinde_AYNI_filtre_ikinci_kez_istek_ATMAZ` | AYNI |
| M23-1 | Sunucu aralık kontrolü etkisizleştirilsin | `M23_Denetim_izinde_TERS_tarih_araligi_400_doner_bos_liste_DEGIL` | AYNI |
| M23-2 | Ekran aralık kontrolü etkisizleştirilsin | `M23_Kar_zararda_TERS_aralik_istek_ATMAZ` | AYNI |
| M24-1 | Bayrak yok sayılsın (adaptörler hep kurulsun) | `M24_Sozlesmesiz_kaynaklar_KAPALIYKEN_hic_adaptor_kurulmaz`, `M24_Kapali_bayrakta_dis_cekim_denenmez_503…` | AYNI |
| M24-2 | "Dış kaynak kapalı" şeridi hiç çizilmesin | `M24_Bayrak_kapaliyken_DIS_KAYNAK_KAPALI_seridi_ve_manuel_yol_yazar` | AYNI |

**Kapatılan test açıkları (mutasyon önce kırmızıya döndürmedi → test güçlendirildi, mutasyon tekrarlandı):**
- M5: ekran içindeki kural saf `TevsikOneri` tipine taşındı, 4 test eklendi.
- M10: `window.prompt` yerine gerçek alanlar geldiği için ilk mutasyon `NullReferenceException`'a
  düşüp isteği engelliyordu; mutasyon `@onclick` kaldırma biçimine çevrildi.
- M11: iptal-fatura senaryosu için yeni test (`M11_IPTAL_edilmis_faturanin_damgasi…`).
- M15: sahte HTTP işleyicisine 40 ms gecikme + `Task.Run` eklendi ki üç istek **gerçekten** eşzamanlı olsun.
- M18: sunucu sözleşmesi için HTTP testi, ekran için "dolu liste → 0 sonuç" testi eklendi.

Mutasyon yedekleri scratchpad'de tutuldu; proje ağacına `.mutorj`/`.bak`/`.tmp` **bırakılmadı**
(ağaçtaki mevcut `*.mutorj` dosyaları **önceki oturumlardan kalmadır**, bu pakette oluşturulmadı ve
silinmedi — silme kurucuya bırakıldı).

---

## 6. Canlı doğrulama (http://localhost:5198, Development)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C1 | `GET /api/v1/olmayan` | 404 JSON, HTML yok | **404** `{"message":"Uç bulunamadı","hata":"uç bulunamadı","error":"Not Found","statusCode":404,"yol":"/api/v1/olmayan"}` |
| C2 | `POST /cari` `{email:"bu-eposta-degil", krediLimitiTl:-5}` | 400 Türkçe + `alanlar` | **400** `message:["E-posta geçerli bir e-posta adresi olmalıdır (örnek: ad@ornek.com)","Kredi limiti 0 veya daha büyük olmalıdır"]`, `alanlar:{email:[…],krediLimitiTl:[…]}` |
| C3 | `POST /subeler` `{telefon:"abc"}` | 400 | **400** `alanlar:{telefon:["Telefon yalnız rakam, boşluk, parantez ve tire içerebilir; başında + olabilir (10–13 hane)"]}` |
| C4 | `GET /kur/kaynaklar` (Development) | bayrak açık | `{"kaynaklar":["ALTINKAYNAK","ANKARA","TRUNCGIL"],"sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}` |
| C5 | `POST /kur/manuel` 6750 → `GET /kur/gecmis?bitis=bugün` | manuel kayıt geçmişte | **3 kayıt**, ilki `('MANUAL', 6750.0)` |
| C6 | `POST /urunler` `acilisStogu:{adet:7}` | stok 7 + defter satırı | `toplamAdet 7.000`, `toplamHasGram 64.120000`; SQL: `SAYIM_DUZELTME · "Açılış stoğu: 7 adet (ürün kartı)" · 64.120000` |
| C7 | `POST /kasalar/{id}/hareket` `{yon:"GELEN"}` | 400, doğru değerler | **400** `alanlar:{yon:["Kasa hareketi yönü yalnız GIRIS veya CIKIS olabilir (banka hareketlerinde GELEN/GIDEN kullanılır)"]}` |
| C8 | `PATCH /kasalar/{id}` `{aktif:false}` → `{aktif:true}` | pasif → aktif | `aktif=false` sonra **`aktif=True`** |
| C9 | MASAK: geçerli belge türü + kısa açıklama, 250.000 TL | tek biçim mesaj | **400** `"MASAK: MASAK beyan açıklaması (masak.aciklama) en az 20 karakter olmalıdır (şu an 4) (Bu cariyle bugün kümülatif 250.000,00 TL — eşik 185.000 TL; bu işlem 250.000,00 TL)"` — **ilk denemede** kümülatif notu var |
| C9b | MASAK `belgeTuru:"EHLIYET"` | DTO düzeyinde ret | **400** `alanlar:{"masak.belgeTuru":["Kimlik belgesi türü geçersiz — kabul edilen değerler: TC_KIMLIK\|PASAPORT\|SURUCU_BELGESI\|MAVI_KART\|VERGI_LEVHASI"]}` |
| C10 | API `Kur__SozlesmesizKaynaklarAcik=false` ile yeniden başlatıldı | adaptör yok, manuel çalışır | `{"kaynaklar":[],"varsayilan":"MANUAL","sozlesmesizKaynaklarAcik":false}` · `POST /kur/guncelle` → **503** "Dış kur kaynakları kapalı … Kuru elle girin" · `POST /kur/manuel` → **201** 7100 |

**SQL kanıtı** (`kuyumcu_net`, test kiracısı):
```
hareketler:      SAYIM_DUZELTME | "Açılış stoğu: 7 adet (ürün kartı)" | has_grami 64.120000
stoklar:         adet 7.000 | toplam_has_gram 64.120000
kur_snapshots:   MANUAL 7100.0000 · ALTINKAYNAK 6799.2700 · ALTINKAYNAK 6795.1200
```

**Temizlik:** `information_schema`'dan `tenant_id` kolonu olan **33 tablo** bulundu, hepsinden test
kiracısı silindi, ardından `tenants` satırı silindi. **ARTIK_VAR=0.** `SELECT COUNT(*) FROM tenants` →
**7** (başlangıçtaki değer). `QA-TEST-1` ve `QA-TEST-2` fikstürlerine **dokunulmadı**.
`lsof -nP -iTCP:5198 -sTCP:LISTEN` boş; 5199 (PID 65793) ayakta bırakıldı.

---

## 7. Değişen dosyalar (bu pakete ait)

**Core (3):**
- `Kuyumcu.Core/Domain/Sozlesmeler.cs` — **YENİ** (`HareketYonu`, `KimlikBelgesi`, `Iletisim`)
- `Kuyumcu.Core/I18n/DogrulamaSozlugu.cs` — **YENİ** (alan adları + İngilizce→Türkçe kalıplar)
- `Kuyumcu.Core/I18n/Sozlukler.cs` — 24 yeni anahtar × TR + EN

**API (14):**
- `Program.cs` (madde 7 + 8) · `appsettings.json` · `appsettings.Development.json` (madde 24)
- `Data/MasakVarliklari.cs` · `Modules/Masak/MasakKuralMotoru.cs` · `MasakKapisi.cs` · `MasakModulu.cs`
- `Modules/Kasa/KasaModulu.cs` · `Modules/Banka/BankaModulu.cs` · `Modules/Kur/KurModulu.cs`
- `Modules/Rapor/RaporModulu.cs` · `Modules/Denetim/DenetimModulu.cs` · `Modules/Sube/SubeModulu.cs`
- `Modules/Cari/CariModulu.cs` · `Modules/Urun/UrunModulu.cs` · `Modules/Urun/KategoriModulu.cs`
- `Modules/Hurda/HurdaModulu.cs`

**Web (14):**
- `Servisler/TevsikOneri.cs` — **YENİ** · `Servisler/ApiIstemci.cs` · `Servisler/Bicim.cs` · `Servisler/UyumServisi.cs`
- `Pages/Kilavuz.razor` (yeniden yazıldı) · `Uyum.razor` · `Kur.razor` · `Cariler.razor` · `Urunler.razor`
- `Pages/Faturalar.razor` · `DenetimIzi.razor` · `KarZarar.razor` · `VadeTakibi.razor` · `Islemler.razor`

**Test (8):**
- **YENİ:** `UX4dSafTestleri.cs` · `UX4dServisTestleri.cs` · `UX4dHttpTestleri.cs` · `UX4dEkranTestleri.cs` · `UX4dOturumTestleri.cs`
- **Güncellenen:** `AcikKalanlarTestleri.cs` · `CariEkranTestleri.cs` · `MasakBaglantiliIslemTestleri.cs`

---

## 8. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| 24 maddenin hepsi uygulandı | ✅ | §2 |
| Her madde için adlı test | ✅ | 82 yeni test, §4-5 |
| Her madde için mutasyon → kırmızı test | ✅ | 27 mutasyon, §5 (açık kalan yok) |
| MASAK/tevsik kapıları DEĞİŞMEDİ | ✅ | Yalnız mesaj metinleri ve ekran varsayılanı; `MasakKuralMotoru` eşikleri ve `TevsikKapisi` dokunulmadı |
| HareketFabrikasi tek yazma yolu | ✅ | `denetim.sh`: `Hareketler.Add` = 1; açılış stoğu mevcut `StokSayimGuncelle` yolunu kullanır |
| İkinci düzenleme kalıbı açılmadı | ✅ | Yeni ModalPencere'ler yalnız **yıkıcı onay** (cari pasife alma, MASAK gönderildi/kapat) |
| Her düğme istek atar ya da disabled+açıklama | ✅ | madde 10 (prompt kaldırıldı) · M10 mutasyonları |
| Sessiz form yasağı (alan altı Türkçe mesaj) | ✅ | `#uy-referans-hata`, `#uy-kapatma-hata`, `#f-aralik-hata`, `#kz-aralik-hata`, `alanlar` sözlüğü |
| Yıkıcı eylemde ortak onay modalı | ✅ | Cariler `#c-pasif-onayla`; MASAK iki modal |
| Yeni uçlarda `[RolGerekli]`/`[ModulYetkisi]` + denetim kaydı | ✅ | `POST gider-pusulalari/{id}/yazdirildi` controller rol kapısı altında + okuma izi; MASAK durum geçişlerine `YazmaKaydet` eklendi |
| Finansal varlıkta hard DELETE yok | ✅ | Yeni DELETE ucu açılmadı |
| Mevcut element id'leri değişmedi | ✅ | 1312 eski testin tamamı yeşil |
| Yeni id'ler `<önek>-<alan>` | ✅ | `uy-*`, `kl-*`, `kur-*`, `c-pasif-*`, `u-acilis-stok`, `f-aralik-hata`, `kz-aralik-hata`, `islem-tevsik-ipucu` |
| Yeni CSS yok / satır içi stil yok | ✅ | Mevcut `.uyari-kutu`, `.kural-listesi`, `.metin-kirmizi` sınıfları kullanıldı |
| Sozlukler TR + EN, çıplak metin yok | ✅ | 24 anahtar iki dilde |
| Razor özniteliğinde `{ }` lambda yok · `disabled="false"` dizgesi yok | ✅ | Derleme 0 uyarı + testler |
| bUnit sahte API yeni uçlara gerçekçi yanıt | ✅ | `UX4dEkranTestleri.SahteApi` (kur/kaynaklar, masak/*, denetim/*, kar-zarar) |
| UX-S21…S33 | ✅ | S21 (açılış şubesi türetilir, tevsik ipucu) · S22 (tek şube sorulmaz) · S25/S26 (kasa aktifleştirme, arama sonucu liste alanlarıyla) · S27 (arama özeti, onay modalları) · S28 (madde 18 · madde 23) · S30 (tevsik ipucu, gerekçe n/20) · S31 (yön/belge türü/ayar/birim tek kaynak) · S33 (vade "dönemden bağımsız" kartı) |
| Tam süit 0 başarısız | ✅ | 1394/1394 |
| `denetim.sh` TEMİZ | ✅ | §1 |
| Migration | — | **gerekmedi** |
| Test kiracısı silindi, tenants eski değerine döndü | ✅ | §6 temizlik (ARTIK_VAR=0, tenants 7) |
| QA-TEST-* fikstürlerine dokunulmadı | ✅ | `SELECT ad FROM tenants` çıktısı |
| 5199'a dokunulmadı | ✅ | `lsof` → PID 65793 ayakta |
| COMMIT yok | ✅ | Hiçbir `git add/commit` çalıştırılmadı |
| Mimari belge / paket spesifikasyonu değiştirilmedi | ✅ | Yalnız bu rapor yazıldı |

---

## 9. Açık kalanlar

1. **`Raporlar.razor` ekranı yeni alanları henüz ÇİZMİYOR.** Madde 11 (`faturaEslesme`), 12
   (`netHasGramHareketi`), 21 (`karTl` + `karKaynagi` + `satisAlisFarkiTl`) **sunucuda** düzeltildi ve
   testlendi; rapor ekranındaki kartlar hâlâ eski alanları okuyor. Kullanıcı çelişkili sayıyı artık
   görmüyor (tanımlar tek), ama yeni açıklayıcı kırılımlar (bağsız fatura listesi, "mal alım-satım farkı"
   etiketi) ekrana bağlanmalı. **Bir sonraki dalgaya**.
2. **Madde 13'ün "aktife al" düğmesi** cari ve kasa ekranlarında henüz yok (uçlar hazır, PATCH `aktif:true`
   çalışıyor — canlı C8 ile kanıtlandı). Ekran düğmesi UX-1 Faz B/C kapsamında.
3. **Kur geçmişinde manuel kur düzeltme yolu / DegismezIpucu** eklenmedi — spesifikasyon bunu
   "UX-1 Faz B/C'ye eklenen düzenleme boşlukları" başlığına atamıştı.
4. **`ApiHata.Alanlar` sözlüğünü henüz hiçbir ekran alan altına yazmıyor.** Altyapı hazır (sunucu
   döndürüyor, istemci taşıyor); ekranların tek tek bağlanması ayrı bir iş.
5. **`TevsikOneri` haddi** sunucu önizleme ucu yalnız karma ödemede çağrıldığı için tekil ödeme yolunda
   30.000 TL varsayılanına düşüyor. Sunucu kuralı yine kapıdır; öneri şeridi yanlış haddi gösterebilir
   (yalnız görsel). Önizleme ucunun her ödeme yolunda çağrılması ayrı bir iş.
6. Ağaçta **önceki oturumlardan kalma 10 adet `*.mutorj` dosyası** var. Bu pakette oluşturulmadılar;
   silme hook nedeniyle **kurucuya bırakıldı**.

---

## 10. Kurucuya / mali müşavire notlar

- **Açılış stoğu — mali müşavire:** Ürün kartından girilen açılış stoğu deftere `SAYIM_DUZELTME`
  türüyle, **maliyeti bilinmeyen** parti olarak düşer. Bu bilinçli bir tutuculuktur: maliyeti sıfır
  yazmak kârı şişirirdi. Sonuç: bu ürünler satıldığında kâr-zarar raporu "maliyeti bilinmeyen adet"
  uyarısı verir. İşletme kuruluşunda gerçek alış maliyetleriyle açılış yapılması için **Paket T-2**
  (Açılış bakiyeleri sihirbazı) beklenmelidir.
- **Gün sonu FARK tanımı — mali müşavire:** Gün sonu mutabakatındaki FARK artık tek cümledir:
  *satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar*. Ayrıca **iptal edilmiş faturanın
  damgasını taşıyan satış artık "faturalı" sayılmıyor.** Bu, KDV beyanının dayanağını doğru tarafa
  çeker; geçmiş günlerin raporları yeniden çalıştırıldığında sayılar değişebilir (daha doğru olacak
  şekilde). Hiçbir defter kaydı değiştirilmedi, yalnız rapor okuması düzeltildi.
- **Aylık kâr — kurucuya:** `/raporlar` ekranındaki "kâr" artık `KarZararServisi`'nin tanımıdır
  (hasılat − satılan malın maliyeti). Eski sayı (satış − alış) kayboldu değil, **adıyla** duruyor:
  "mal alım-satım farkı". İkisi farklı sorulara cevap verir; aynı ay için iki farklı "kâr" görmeyeceksiniz.
- **Kur kaynakları — KURUCU İMZA KALEMİ (madde 24):** Truncgil · Altınkaynak · Ankara Altın Borsası
  uçları **sözleşmesizdir**. Üretim varsayılanı artık **KAPALI**: kurulan sistem TCMB resmî döviz +
  elle girilen has fiyatıyla çalışır. Ücretli/ticari sürümde bu kaynakları açmak için sağlayıcıyla
  **yazılı izin ya da abonelik** gerekir (ör. altinapi.com). Bu Kurucu'nun imza/para kalemidir
  (KURUCUYA-KARARLAR md. 8). Pilotta açmak isterseniz tek anahtar: `Kur:SozlesmesizKaynaklarAcik=true`;
  ekran o zaman "sözleşmesiz kaynak — yalnız pilot" şeridi gösterir.
- **Kılavuz — kurucuya:** `/kilavuz` sayfası artık boş değil, ama kılavuz **ayrı sekmede** açılıyor.
  Sebebi güvenlik başlıklarıdır (uygulamanın çerçeveye gömülmesine karşı koruma) ve bu koruma
  gevşetilmedi. Kılavuzu uygulama içine almak isterseniz içeriğin Razor bileşenlerine taşınması
  gerekir — ayrı bir iş kalemidir.
- **Oturum süresi — kurucuya:** Süresi dolmuş oturumda artık "Beklenmeyen bir hata oluştu" yerine
  doğrudan giriş sayfasına dönülüyor ve "Oturum süresi doldu" yazıyor.
