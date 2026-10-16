# PAKET ISLEM-BANT: uygulama raporu (09.10.2026, t2, canlı 5162, vekil 5163, CDP 9912/9913)

**Konu:** İşlemin içinde elle yazılan has gram fiyatı (hurda alış, hurda satış, takas, eski altınla ödeme) makul banttan geçmiyordu. Ayrıca tezgâhtar üst çubuktan BASİT/PRO modunu değiştiremiyordu (`PATCH /kullanicilar/ben` 403). Kaynak: KUR-BANT § 0.6 (YÜKSEK) ve § 0.7/2 (ORTA).
**Rol:** Taşkın (backend) ve Ertuğ (frontend). **COMMIT YOK**, değişiklikler çalışma ağacında duruyor.
**Durum:** BİTTİ. Paket kapısının 7 maddesi son kodla (tur 2, 09.10 21:55–22:45) yeniden koşuldu.

## 0. Tur 2: önceki doğrulamanın bulguları

| Bulgu | Karar | Kanıt |
|---|---|---|
| [KRİTİK] BASİT Değişim, ürün seçili + eski fiyat 3000: sütunlar üst üste, "Ekle" kayıp, teklif notu bant hatasının üstünde | KAPANDI | `degisim-gunsonu.css` sonundaki "ISLEM-BANT tur 2" bloğu: VERDİM sütunundaki elle satırı her genişlikte dar düzen (Ürün tam satır; Adet · Fiyat · Ekle altında), teklif notu sütun içinde sarar. Ek olarak (bu turda ölçülen) mutlak sınır dışı dev tutarda ALDIM alt çubuğu: has miktarı bölünmez, tutar gerekirse alt satıra iner. Hakemin durumu birebir yeniden kuruldu (TEZGAHTAR · arama "Yüzük" → "Listede olmayan ürünü elle ekle" → Tek Taş Yüzük · 14 Ayar → eski 3 g × 3000). 24 ölçümde K1/K3/K5/K9/K12/yatay 0; `#islem-kalem-ekle` görünür ve VERDİM sütunu içinde; `#takas-verdim-etiket`×`#takas-eski-ayar`, `#islem-teklif-yok`×`#takas-eski-fiyat-hata`, `#islem-kalem-ekle`×`#takas-eski-fiyat` kesişimi YOK (§ 9.1). |
| [DÜŞÜK] `IslemBantTestleri.cs(639,9)` xUnit2013 | KAPANDI | `Assert.Single(Regex.Matches(...))`; tur 2 temiz derlemede `IslemBant*` dosyalarında uyarı 0 (§ 7). |
| [DÜŞÜK] Aynı alanda iki sayı biçimi (`500–50.000 TL` / `3.413,60–7.509,92 TL`) | KAPANDI (biçim ortak; GIRDI şeridine devredilmedi, burada yapıldı) | `GirdiSozlugu.Mesaj`: `FiyatMutlakDisi` uçları da `#,0.00` → "(500,00–50.000,00 TL)" / döviz "(5,00–500,00 TL)". Eski metni sabit yazan 3 test güncellendi: `KurBantTestleri.cs:87` (KB06), `Dogrulayici/KURBANTSinirTestleri.cs:232` (D01), `Dogrulayici/ISLEMBANTSinirTestleri.cs:120`. IB06'ya TR+EN mutlak biçim iddiası eklendi. Canlı yanıt ve ekran aynı metni gösteriyor (§ 8, § 9.1). Mutasyon MIB-9. |
| [DÜŞÜK] Referans = kiracının son geçerli kur kaydı | Değişiklik yok (doğrulayıcı kabul etti) | § 1 |
| [DÜŞÜK] `Hurda.razor` yedekten geri yükleme | Değişiklik yok; orkestratöre not § 12/4 | — |
| [DÜŞÜK] Canlı betikte jeton başı çıktıya düştü | KAPANDI (kalıp) | Tur 2 betiği (`canli3/kur.py`, `ortak.py`) kayıt/girişte yalnız durum kodu ve "token var" yazar; giriş 429'da yeniden dener, gövdeyi basmaz. Bu turda hiçbir jeton/parola çıktıya düşmedi. |
| [DÜŞÜK] Gümüş/platin bandı yok | Açık kalan (§ 12/1) | — |

## 1. Spesifikasyon ile kodun karşılaştırılması (sapmalar)

| İddia (KUR-BANT § 0.6) | Kodda bulunan (09.10) | Karar |
|---|---|---|
| Satırlar: `SatisModulu.cs` hurda alış ~251, hurda satış ~342, takas ~554, `OdemeSepeti.cs:81` | Doğru, satırlar kaymıştı. Hurda alış `HurdaAlis` (DTO 251), hurda satış `HurdaSatis` (DTO 342), takas `Takas` (DTO 554), eski altın `OdemeSepeti.AltinBacaginiCoz` (statik, db'siz) | Eski altın kapısı statik `AltinBacaginiCoz` içine konmadı. `SatisFisi` içinde, sepet çözüldükten sonra tek satırla çağrılıyor. `OdemeSepeti.cs` **değişmedi** (cmp ile birebir aynı). |
| "Hazır çağrı `FiyatBandi.Denetle(fiyat, Has, kurSnapshot.AltinHasFiyati)`" | `Denetle` kur ekranının simetrik ± %20 kuralı. Hurda alışta asimetrik oran isteniyor. | Core'a **yeni işlem bağlamı** eklendi: `FiyatBandi.IslemDenetle(fiyat, IslemFiyatBaglami, referansHas, maden)`. Mutlak sınır sabitleri (500–50.000) Kur ile aynı tek sabit. `OnaylanabilirMi` yeni kodu da tanıyor. |
| Referans "kurSnapshot.AltinHasFiyati" | Görev metni: "referans = o anki geçerli has fiyatı" | Referans, kiracının **son iptal edilmemiş** kur kaydındaki has fiyatı (`IslemFiyatKapisi.Referans`). Ekrandaki canlı kur da bu kayıt. İptal edilmiş kayıt referans olmuyor (IB09c, MIB-5). |
| Bant tüm madenlere | Gümüş/platin has fiyatı ~80 TL. Kur kaydı altın has fiyatı tutuyor. | Bant **yalnız ALTIN** madende uygulanıyor. Gümüş için referans yok (IB04, IB14, canlı). Gümüş bandı ileriye bırakıldı (§ 12). |
| Onay alanları bacakta (`OdemeBacagiDto`) | denetim.sh P-10/P-16 sayaçları: sunucunun kabul ettiği alanı form da taşımalı | Onay alanları satış fişinde **fiş düzeyinde** (`SatisFisiDto.BantOnay/BantGerekce`). Ekran bu iki alanı fiş gövdesinde gönderiyor (P-16 ✓). |
| Ekran: "yalnız alan doğrulama gösterimi" | Onay kutusu olmadan SAHİP/MÜDÜR olağandışı fiyatı ekrandan hiç kaydedemezdi. P-16 de gönderimi istiyor. | Ortak `Bilesenler/Fis/IslemBantOnay.razor` yazıldı (Kur ekranıyla aynı kalıp ve aynı metinler). Dört ekrana bağlandı. `Islemler.razor`'a **yeni satır eklenmedi**: üç gövde satırına alan eklendi, dosya 1397/1400'de kaldı. |
| TEZGAHTAR mod: "PATCH /kullanicilar/ben alan beyaz listesi" | `KendinGuncelle` sınıf düzeyindeki `[ModulYetkisi("kullanici")]` arkasındaydı. Dar uç `/ben/arayuz-modu` vardı ama üst çubuk onu kullanmıyordu. | `PATCH /kullanicilar/ben` ayrı bir controller'a (`KendiProfilController`, `[HerkeseAcik(gerekçe)]`) taşındı. Servis içinde beyaz liste var: "kullanici" modülü olmayan rol yalnız `arayuzModu` yazabiliyor, başka alan gönderirse **403**. Yetki genişlemiyor. Üst çubuk kodu değişmedi. |

**Mevzuat:** Bu kural bir İŞ KURALIDIR (yazım hatası kapısı). Hiçbir yasal eşik ya da oran yazılmadı, birincil kaynak gerekmedi.

## 2. Ekip oyu: işlem bandı oranları
- **Dükkânın ALDIĞI eski altın** (hurda alış, takasta alınan, eski altınla ödeme): has fiyatının **%50–%110'u**. Gerekçe: alış fiyatı piyasanın altında olur (eritme firesi, ayar belirsizliği, kâr payı). Kur ekranındaki ± %20'nin alt ucu (%80) gerçek hurda alışlarını durdururdu. %50 altı yazım hatası sinyali ("2.000" yerine "6.000"). Üst uç %110: piyasanın üstünde alış olağan değil, onaylı geçer. Takas ve eski altınla ödeme ekonomik olarak hurda alışıyla aynı olduğu için aynı bandı kullanıyor.
- **Dükkânın SATTIĞI hurda** (ramatçıya): kur ekranıyla aynı **± %20** (%80–%120). Satış fiyatı piyasaya yakın olur.
- **Mutlak sınır** (has 500–50.000 TL/g): SERT kapı, onayla da aşılamıyor (Kur tur 2 kararı).
- Bant uçları ekranda TL olarak gösteriliyor: alt uç kuruşa yukarı, üst uç aşağı yuvarlanıyor, böylece ekranda yazan uç kabul ediliyor (IB07).

## 3. Yapılanlar
**Core**
- `Domain/Girdi/FiyatBandi.cs`: `IslemFiyatBaglami` (HurdaAlis/HurdaSatis/Takas/EskiAltinOdeme), `AlisAltYuzde=50`, `AlisUstYuzde=110`, `IslemBandi`, `IslemBantUclari`, `IslemDenetle`. `OnaylanabilirMi` artık `IslemFiyatBantDisi` kodunu da tanıyor.
- `Domain/Girdi/GirdiHatasi.cs`: `GirdiKodu.IslemFiyatBantDisi`.
- `I18n/Sozlukler.Girdi.cs`: TR "Bu has fiyatı güncel has fiyatına göre olağan aralığın dışında ({0}–{1} TL). Rakamı yanlış yazmış olabilirsiniz; kontrol edin." ve EN karşılığı. İki uç para biçiminde (iki ondalık, tr-TR/en-US).
- `I18n/Sozlukler.IslemBant.cs` (YENİ): `YalnizModDegisir` TR+EN.

**API**
- `Modules/Satis/IslemFiyatKapisi.cs` (YENİ): referans okuma, mutlak sınırda sert 400, oran bandında onaysız 400 + alan + sözlük metni, onaylı ama rol SAHİP/MÜDÜR değilse 403, gerekçe K30 (≥ 20), ardından `DenetimServisi.YazmaKaydet(…, "IslemFiyatBantAsimi", <referans kur id>, detay, hassas: true)`. Denetim kaydı çağıranın SaveChanges'i ile yazılıyor. Kullanıcısız iç yolda onay mümkün değil (fail-closed 403).
- `Modules/Satis/SatisModulu.cs`: `HurdaAlisDto`, `HurdaSatisDto`, `TakasDto`, `SatisFisiDto`'ya `BantOnay` ve `[StringLength(1000)] BantGerekce` eklendi. Dört metoda **birer satır** `IslemFiyatKapisi.Denetle(...)` çağrısı eklendi. Başka değişiklik yok.
- `Modules/Kullanici/KullaniciModulu.cs`: `KendinGuncelle(dto, islem, yalnizMod)` beyaz listesi. Mod değişimi denetim izi yazıyor (dar uçla aynı "arayuz-modu: X"). Yeni `KendiProfilController`. Eski aksiyon kaldırıldı.
- `Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs`: `HurdaSatisIstegi` sonuna isteğe bağlı `BantOnay`, `BantGerekce` eklendi (geriye uyumlu).

**Ekran**
- `Bilesenler/Fis/IslemBantGosterim.cs` (YENİ): referans canlı kurdan, metin sunucuyla aynı. `IslemBantOnayDurumu` gövdeye yalnız onay işaretliyken gidiyor.
- `Bilesenler/Fis/IslemBantOnay.razor` (YENİ): yalnız oran bandı aşımında görünüyor. SAHİP/MÜDÜR için `#{önek}-bant-onay` ve `#{önek}-bant-gerekce` (≥ 20, canlı "Gerekçe en az 20 karakter" uyarısı). Diğer rollerde tek, küçük, soluk `#{önek}-bant-yetki` cümlesi. Bant içindeyken hiçbir şey çizilmiyor (zorunlu önce kuralı).
- `FisHurdaAlis.razor` (`hurda-*`), `FisTakas.razor` (`takas-*`), `AltinBacagiPaneli.razor` (`altin-*`), `Pages/Hurda.razor` (`hrd-*`): alan altında sunucuyla aynı cümle ve onay bileşeni. Mevcut id'ler değişmedi.
- `FisModelleri.cs` (`HurdaGirisi.Bant`, `TakasGirisi.Bant`), `Servisler/AltinBacagiGirisi.cs` (`Bant`), `FisAltinBacagi.cs` (`Bant(bacaklar)`).
- `Pages/Islemler.razor`: hurda-alis, takas ve satis-fisi gövde satırlarına `bantOnay`/`bantGerekce` eklendi (aynı satırlar, satır sayısı 1397 değişmedi).

**Migration:** gerekmedi (şema değişmedi; iz mevcut `denetim_kayitlari` tablosunda).

## 4. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Yeni varlık | YOK | — |
| `denetim_kayitlari` "IslemFiyatBantAsimi" | Salt okunur (append-only, ADR-0004) | İz değiştirilemez, tasarım gereği. |
| Bant dışı onaylı işlem (hareket) | Mevcut storno motoru (48-A) | Finansal kayıtta yerinde düzeltme yok. |
| `bantOnay` / `bantGerekce` | İstek alanı, kalıcı kolon değil | — |
| `kullanicilar.arayuz_modu` (kendi) | `PATCH /kullanicilar/ben` (her rol, yalnız mod) + dar uç `/ben/arayuz-modu` | PATCH sözleşmesi: alan yoksa korunur, null ise kiracı varsayılanı devralınır (IB22). |

Düğmeler: `#*-bant-onay` yalnız durum değiştiriyor (gerekçe kutusunu açıyor). Kaydet POST atıyor ya da sunucunun 400/403'ü bantta görünüyor. Yeni ModalPencere yok (denetim "ModalPencere 48 ≤ 55").
**Dokunulan sayfalar:** `Pages/Islemler.razor` (3 gövde satırı) ve `Pages/Hurda.razor` (alan gösterimi, onay bileşeni, gövde). İkinci dokunuşun gerekçesi: KUR-BANT § 0.6 devri bu sayfaları bu paket için açtı. Paralel şerit notu: Satış modülünde t5 şeridinin alanına yalnız fiyat bandı tek satır çağrıları ve DTO alanları eklendi. `OdemeSepeti.cs` değişmedi.

## 5. Testler
**Yeni: `Kuyumcu.Tests/IslemBantTestleri.cs`, 50 koşum**
- Saf (IB01–IB08): alış bandı tam sınırlar (3.413,60 / 7.509,92 / ±0,01), satış bandı (5.461,76 / 8.192,64), mutlak (5, 499,99, 50.000,01, 99.999.999; referans olsa da olmasa da, onaylanamaz), boş/0/eksi/gümüş, sözlük TR+EN, kuruş yuvarlaması, ekran–sunucu aynı cümle.
- Servis (IB09, IB09b, IB09c): kullanıcısız, TEZGAHTAR ve MUHASEBE doğrudan çağrıda 403 ve iz yok. SAHİP/MÜDÜR onaysız 400, onaylı iz (varlık = referans kur, hassas, detay). Referans iptal edilen kaydı atlıyor.
- HTTP gerçek MySQL (IB10–IB18): hurda alış hasım değerleri (5, 99.999.999, 2.000, 3.413,59, 7.509,93, 0, eksi, harf) 400 + alan ve hareket yazılmıyor. Bant içi ve tam sınır 200. SAHİP/MÜDÜR onaylı 200 + iz. Kısa gerekçe 400 `bantGerekce`. Onaylı mutlak 400. MUHASEBE onaylı 403. Gümüş 80 TL 200. Referans kayması. Hurda satış ± %20. Takas TEZGAHTAR 403 / SAHİP onaylı 200. Eski altınla ödeme TEZGAHTAR 403 / SAHİP onaylı 200.
- Kendi mod HTTP (IB20–IB24): TEZGAHTAR ve MUHASEBE mod 200 + iz. TEZGAHTAR'ın ad, soyad, e-posta, subeId:null ve aktif gönderimi 403, satır değişmiyor. PATCH sözleşmesi (yok → koru, null → devral, geçersiz → 400). SAHİP/MÜDÜR eski davranış korunuyor. Anonim 401.
- bUnit (IB30–IB38): alan altı aynı cümle (hurda, takas, eski altın paneli). Canlı kur yokken yalnız mutlak sınır. SAHİP/MÜDÜR onay ve gerekçe uyarısı. TEZGAHTAR/MUHASEBE'de tek, küçük yetki cümlesi. Mutlak sınırda ve bant içinde onay yolu yok. Onay fiş gövdesine bacaktan taşınıyor. İşaretsizken alan gitmiyor.

**Kasıtlı davranış değişikliğiyle güncellenen eski test:** `S1fGorunumKartiTestleri.S1f_H5_…` (eski yol 403 bekliyordu). Yeni adı `S1f_H5_Tezgahtar_eski_yolda_yalniz_mod_200_dar_ucta_200_alir`: mod 200, ad 403.

**Fikstür etkisi (KUR-BANT § 0.6'nın öngördüğü):** İlk koşumda filtreli 4.590 testin 166'sı kırmızıydı. Hepsi yeni bandın iletisiyle düşüyordu: fikstür kuru 6.827,20 iken hurda/takas fiyatları 1.000–3.000 TL/has g idi. Seçilen yol, testlerin **tutarlarına dokunmamak**, çünkü MASAK, tevsik ve kasa eşikleri o tutarlara bağlı. Yeni yardımcı `Kuyumcu.Tests/IslemBantFikstur.ReferansKur(db, kiracı, has)` kiracıya testin fiyatlarıyla tutarlı, **daha yeni ve geçerli** bir kur kaydı ekliyor. Bu kayıt bandın referansı oluyor. İşlem gövdesindeki `kurSnapshotId` değişmiyor.

Fikstür sarılan dosyalar: AcikKalanlar, B4AltinBacagi(+Http), CiftBacak, DegisimGunSonu, G5SatisKirilimiHttp, GiderPusulasi, GocA (yalnız GB12), GocC, KarZararIade, KasaBakiyeKapisi, KesifP3, MasakEntegrasyon, Muhasebe, P20HurdaSatis, P20bTahlilFarki, ParaDemo, ParaDemo2, PosTahsilat, Q102HurdaSinir, TcknDogrulama, Yasal3Masak, Yasal4Http, ZOISLEMAOdemeTutarKapisi, ZOISLEMASube, Dogrulayici/{CIFTBACAK, PARADEMO2, Q102, YASAL3, YASAL4, ZOISLEMA}Sinir.

Mutlak sınırı bilerek aşan 7 "kuruş sınırı" testinde **aynı brüt** geçerli fiyatla yeniden kuruldu. Hepsine yorum düşüldü:
- ZOSR20: 0,0001 g × 50 yerine 0,000001 g × 3.000.
- DQ102S: 0,5 × 0,01 yerine 0,000002 × 2.500 (yarım kuruş korunuyor).
- YASAL3 ×3: 1 g × 198.999,9x yerine 4,011 × 49.613,56 / 3,985 × 49.937,26 / 5,62 × 35.409,25. Brütler kuruşu kuruşuna aynı.
- HurdaAkis kimliksiz: 100 yerine 2.800 TL.
- MASAK takas: 10 g × 90.000 yerine 20 g × 45.000 (aynı 824.400).
- B4 MASAK: 20 g × 9.000 yerine 48 g × 3.750 (aynı 180.000).
- B4 tek kur: daha yeni kur 7.000 yerine 3.600.

**Filtreli koşumlar:** 4.590 (166 kırmızı) → 4.716 (49 kırmızı) → 4.741 (3) → son ilgili filtre 0 kırmızı. IslemBant 50/50.

## 6. Mutasyon kanıtı
Yedek `/private/tmp/kuyumcu-serit/mut-yedek/ISLEM-BANT/`, `perl -0777 -pi` ile tek eşleşme, filtre `IslemBant`, geri alındıktan sonra `cmp` ile kontrol. Kayıtlar `/private/tmp/kuyumcu-serit/ISLEM-BANT/mut/`. Son mutasyondan sonra `--no-incremental` temiz derleme: 0 hata.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| MIB-1 | Sert kapı: `!OnaylanabilirMi(h) \|\|` silindi (onay mutlak sınırı aşar) | IB12 ×2 (SAHİP, MÜDÜR), IB16, IB18 (4) | AYNI |
| MIB-2 | `AlisAltYuzde = 50` yerine `20` | IB01 ×3, IB02, IB07, IB08, IB09 ×3, IB09b ×2, IB10, IB12 ×2, IB13, IB15, IB17, IB18, IB35, IB37 … (26) | AYNI |
| MIB-3 | Rol kapısı `!BantOnayYetkili(islem.Rol)` silindi | IB09 (TEZGAHTAR, MUHASEBE), IB13, IB16, IB17, IB18 (6) | AYNI |
| MIB-4 | Beyaz liste `yalnizMod &&` yerine `false &&` | IB21 ×5 (ad, soyad, e-posta, subeId, aktif) | AYNI |
| MIB-5 | Referanstan `!s.IptalEdildi` silindi | IB09c | AYNI |
| MIB-6 | Ekran onay yetkisi `is "SAHIP" or "MUDUR"` yerine `is not null` | IB35 ×2 | AYNI |
| MIB-7 | Denetim izi `YazmaKaydet` çağrısı kaldırıldı | IB09b ×2, IB12 ×2, IB16, IB17, IB18 (7) | AYNI |
| MIB-8 | Controller `yalnizMod: !tamYetki` yerine `true` (SAHİP kısıtlanır) | IB23 | AYNI |
| MIB-9 (tur 2) | `Sozlukler.Girdi.cs`: `h.Kod is IslemFiyatBantDisi or FiyatMutlakDisi` yerine `h.Kod == IslemFiyatBantDisi` (mutlak ileti eski biçime döner) | IB06, KB06, `ISLEMBANTSinirServisTestleri.IBS_mutlak_disi_TEZGAHTAR_onayli_bile_400_403_degil_iz_yok`, `KURBANTSinirDogrulayiciTur3Testleri.D01_Doviz_mutlak_sinir_4_hane_alt_ve_ust` (4) | AYNI (`mut/mib9-*.log`) |

Tur 2 CSS değişikliği (görünüm) birim testle değil gerçek Chrome ölçümüyle kanıtlandı (§ 9.1); CSS için mutasyon uygulanmadı.

## 7. Derleme, denetim, tam süit
- Tur 1'deki "Bu paketin dosyalarında 0 uyarı" iddiası **yanlıştı** (xUnit2013, doğrulayıcı DÜŞÜK); düzeltildi.
- **Tur 2 `--no-incremental` temiz derleme** (`build-t3-temiz2.log`): **0 hata, 6 uyarı**; altısı da başkasının dosyasında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65)`, `KesifP2Testleri.cs(455,9)`, `SonKalanlarTestleri.cs(593,9)` ve `(824,9)`, `Yildiz2Testleri.cs(193,83)` ve `(232,81)`). Bu paketin dosyalarında uyarı **0**. İlk temiz derleme denemesi `Kuyumcu.Web/obj/.../tmp` dosyasına başka sürecin erişimi yüzünden MSB4018 IOException ile düştü (`build-t3-temiz.log`, paralel şerit derlemesi); kod değişmeden yeniden koşuldu ve yeşil geçti.
- **Filtreli koşum** (IslemBant, KurBant, ISLEMBANT, KURBANT): 140/140 geçti (`test-t3.log`).
- **Tam süit, son kodla BİR kez** (`trx-pISLEM-BANT-t3/_Mac_2026-10-09_22_36_00.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t2): **toplam 11.986, çalışan 11.983, geçen 11.983, başarısız 0, atlanan 3.** Bu paketin yeni kırmızısı: **0**. (Tur 2 ilk denemesinin 18:22 koşumundaki 3 kırmızı — Y4C_02, P26 Y30, Y4B_31 — başka şeritlerindi ve bu koşumda yok.)
- `bash dotnet/denetim.sh` (`denetim-t3.log`): bu paketin sayaçları ✓ (P-10/P-16 DTO_FORM_ESLESMEZ 0, ModalPencere tavanın altında, SABIT_RENK 0, SOZLUK 0, HerkeseAcik gerekçeli). ✗ olan 3 madde başkasına ait:
  - `P-30 DEMO_INSAN_SURESI`: tarih kapısı (görevde hariç tutulan).
  - `GIRDI-2 TEST_GECERSIZ_FIKSTUR = 1`: `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN, MASAK-ESZAMAN şeridi.
  - `B-3 ESIK_ISTEMCIDE = 6 (tavan 4)`: eşleşen 6 satırın hiçbiri bu paketin değil — `Servisler/TevsikOneri.cs:8`, `Pages/Islemler.razor:516` (taban), `Pages/Islemler.YasalKapiGeri.cs:76-77` (YASAL-KAPI-GERI şeridi, yeni), `Bilesenler/Fis/FisOdemeKasa.razor:15,85` (yorum satırları, ödeme şeridi). Bu paket "30.000/185.000" içeren satır eklemedi.

## 8. Canlı doğrulama (tur 2, 09.10 22:00)
Ortam: 5162 (`ASPNETCORE_ENVIRONMENT=Development`, PID ile başlatıldı/durduruldu), kiracı `PAKETISLEM-BANT-TEST …` (id 01M4H0H0XMWG39ZWP74Z6CBQ0Z), SAHİP/MÜDÜR/MUHASEBE/TEZGAHTAR. Güncel has 6.666, alış bandı 3.333,00–7.332,60, satış bandı 5.332,80–7.999,20. Yanıtların hepsi okundu (`canli3/canli-sonuc-*.json`). İlk koşum 13. adımda giriş hız sınırına (429) takıldı; betik girişte yeniden deneyecek şekilde düzeltilip 13. adımdan sürdürüldü.

| Adım | Beklenen | Gerçek | Alan · ileti |
|---|---|---|---|
| hurda-alis SAHİP has 5 | 400 | 400 | birimFiyatTlHasGram · "olağan aralığın dışında (500,00–50.000,00 TL)" |
| hurda-alis SAHİP 99.999.999 | 400 | 400 | aynı |
| hurda-alis SAHİP 0 / eksi / harf | 400 ×3 | 400 ×3 | birimFiyatTlHasGram · "0 TL olamaz" / Range / "geçerli bir sayı değil" |
| hurda-alis SAHİP 2.000 onaysız | 400 | 400 | birimFiyatTlHasGram · "(3.333,00–7.332,60 TL)" |
| hurda-alis SAHİP 2.000 onaylı, gerekçe 5 karakter | 400 | 400 | bantGerekce · "en az 20 karakter (şu an 5)" |
| hurda-alis SAHİP has 5 ONAYLI | 400 | 400 | mutlak (onay aşamaz), "(500,00–50.000,00 TL)" |
| hurda-alis MUHASEBE 2.000 ONAYLI | 403 | 403 | "yalnız dükkân sahibi ya da müdür onaylayabilir" |
| hurda-alis SAHİP 6.000 / 2.000 ONAYLI | 200 / 200 | 200 / 200 | — |
| hurda-satis SAHİP 9.000 onaysız | 400 | 400 | "(5.332,80–7.999,20 TL)" |
| hurda-satis MUHASEBE 9.000 ONAYLI | 403 | 403 | yetki cümlesi |
| hurda-satis MÜDÜR 6.700 / 9.000 ONAYLI | 200 / 200 | 200 / 200 | — |
| takas TEZGAHTAR 3.000 / ONAYLI / 99.999.999 | 400 / 403 / 400 | 400 / 403 / 400 | eskiUrunBirimFiyatTlHasGram |
| takas TEZGAHTAR 6.000 · SAHİP 3.000 ONAYLI | 200 · 200 | 200 · 200 | — |
| satis-fisi eski altın SAHİP 2.000 / TEZGAHTAR ONAYLI / SAHİP 100 ONAYLI / SAHİP 2.000 ONAYLI | 400 / 403 / 400 / 200 | 400 / 403 / 400 / 200 | birimFiyatTlHasGram |
| PATCH /kullanicilar/ben TEZGAHTAR {arayuzModu:BASIT} | 200 | 200 | arayuzModu=BASIT |
| PATCH /ben TEZGAHTAR {ad} · {arayuzModu, subeId:null} | 403 · 403 | 403 · 403 | "Bu bilgiyi değiştirme yetkiniz yok…" |
| PATCH /ben MUHASEBE BASIT · TEZGAHTAR KARMA · TEZGAHTAR PRO | 200 · 400 · 200 | 200 · 400 · 200 | — |

30 adımın 30'u beklenen kodla döndü. SQL (`/private/tmp/kuyumcu-serit/ISLEM-BANT/canli3/sql-kanit.txt`):
```
denetim_kayitlari varlik_turu='IslemFiyatBantAsimi' (varlik_id = referans kur 01M4H0HA23DP…, eylem EKLEME, hassas=1, ip var):
 SAHIP  HurdaAlis … birimFiyatTlHasGram=2000 (güncel has 6666.0000, bant %50–%110) · gerekçe: …
 MUDUR  HurdaSatis … birimFiyatTlHasGram=9000 (güncel has 6666.0000, bant %80–%120) · gerekçe: …
 SAHIP  Takas … eskiUrunBirimFiyatTlHasGram=3000 (… bant %50–%110) · gerekçe: …
 SAHIP  EskiAltinOdeme … birimFiyatTlHasGram=2000 (… bant %50–%110) · gerekçe: …
arayuz-modu izleri (Kullanici, hassas=0): TEZGAHTAR BASIT · MUHASEBE BASIT · TEZGAHTAR PROFESYONEL
hareketler: ALIS 2 · HURDA_ALIS 3 · HURDA_SATIS 2 · SATIS 1 · TAKAS 2   (reddedilen denemeler satır yazmadı)
kullanicilar: TEZGAHTAR ad='Tezgahtar' (403'ler yazmadı), sube dolu, arayuz_modu=PROFESYONEL
```
**Temizlik:** API (PID 66789) ve vekil `kill` ile durduruldu; 5162 ve 5163'te dinleyen 0; CDP 9914–9917 Chrome süreçleri kapatıldı. `zsh kiraci-temizle.sh 01M4H0H0XMWG39ZWP74Z6CBQ0Z --uygula` çalıştırıldı; ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)" dedi. Kendi davet satırım silindi (1 satır). **tenants: başlangıç 20 → son 24.** Fark bu paketin değil: `PAKETISLEM%` adlı kiracı 0; aradaki 4 kiracı eşzamanlı şeritlerin (YILDIZ4C, KAPORA-EKRAN-HAKEM, YASAL-KAPI-GERI, 7B-HAKEM vb.) çalışırken açtığı test kiracılarıdır, temizlik betiğinin listesinde görünüyor. QA-TEST- kiracılarına dokunulmadı. Parola içeren fikstür dosyaları (`canli3/hesap.json`, `chrome/tezgahtar.json`, `chrome/sahip.json`) `printf ''` ile boşaltıldı. Bu turda jeton/parola çıktıya düşmedi.

## 9. Gerçek başsız Chrome

### 9.1 Tur 2: BASİT Değişim (hakem KRİTİK) ve mutlak sınır iletisi
Ortam: vekil 5163 (TEZGAHTAR), CDP 9914/9916, temiz profil (ilk denemede profil önbelleği eski CSS'i tuttu; ölçüm yeni profille tekrarlandı), `sikisma-olc.js`. Durum hakeminkiyle aynı: arama "Yüzük" → "Listede olmayan ürünü elle ekle" → Ürün = Tek Taş Yüzük · 14 Ayar → elle satırı (Adet · Birim fiyat · Ekle) ve "Fiyat listesi tanımlı değil" notu görünür; ALDIM: 22 ayar, 3 g, has fiyatı 3000 (oran bandı dışı) ya da 99.999.999 (mutlak dışı). Ham veri `chrome/olcum-degisim-t3.jsonl`, `chrome/olcum-degisim-mutlak-t3.jsonl`; görüntüler `chrome/foto/degisim_*`, `chrome/foto/mutlak_*`.

| durum | mod | tema | 1440×900 | 1280×800 | 375×812 |
|---|---|---|---|---|---|
| eski fiyat 3000 | BASİT | açık | K1 0 · K3 0/0 · K5 0 · K9 0 · K12 0 · Y 0 · Ekle görünür, sütun içinde · kesişim 0/3 · bant 1 | aynı | aynı |
| eski fiyat 3000 | BASİT | koyu | aynı | aynı | aynı |
| eski fiyat 3000 | PRO | açık | K1/K3/K5/K9/K12/Y 0 · Ekle görünür · bant 1 | aynı | aynı |
| eski fiyat 3000 | PRO | koyu | aynı | aynı | aynı |
| eski fiyat 99.999.999 | BASİT | açık | 0 · ileti "(500,00–50.000,00 TL)" | 0 | 0 |
| eski fiyat 99.999.999 | BASİT | koyu | 0 | 0 | 0 |
| eski fiyat 99.999.999 | PRO | açık | 0 | 0 | 0 |
| eski fiyat 99.999.999 | PRO | koyu | 0 | 0 | 0 |

Hakemin ölçtüğü 1440 K3 1 · K9 2 · K12 1 ve 1280 K3 1 · K9 4 · K12 1 değerleri **0**'a indi. Ara ölçüm (Anayasa md.1): mutlak durumunda 1280 BASİT'te önce K3 kırık 1 (`#takas-aldim-has` "2,7480 has" iki satır), sonra K3 taşan 1 (`#takas-aldim-tutar` "274.799.997,25 ₺") çıktı; ALDIM alt çubuğuna sarma kuralı eklenerek son ölçümde 0.

Görülen görüntüler (Read, bu tur 4):
- `dbg-t3.png` (1440 BASİT, ayıklama): VERDİM'de arama, Ürün seçici, Adet · Birim fiyat · Ekle sütun içinde; teklif notu iki satıra sarıyor; ALDIM'daki bant cümlesi okunuyor.
- `degisim_TEZGAHTAR_BASIT_aydinlik_1440.png`: hakemin karesi. Üst üste binme yok, "Ekle" tam görünür, "Tek Taş Yüzük · 14 Ayar" tek satır, bant cümlesi "(3.333,00–7.332,60 TL)" ve altında tek, küçük yetki cümlesi.
- `degisim_TEZGAHTAR_BASIT_karanlik_1280.png`: aynı düzen dar sütunda; bindirme yok.
- `mutlak_TEZGAHTAR_BASIT_karanlik_1280.png`: "Bu fiyat olağan aralığın dışında (500,00–50.000,00 TL)…"; ALDIM alt çubuğunda "2,7480 has" üstte, dev tutar altta sağa yaslı.

### 9.2 Tur 1 ölçümleri (değişmeyen ekranlar)
Ortam: vekil 5163 (SAHİP, sonra TEZGAHTAR fikstürü), CDP 9912 (SAHİP) ve 9913 (TEZGAHTAR), `sikisma-olc.js`. Ham veriler `/private/tmp/kuyumcu-serit/ISLEM-BANT/chrome/olcum-*.jsonl`, görüntüler `chrome/foto/`.

Durumlar:
- `hurda`: SAHİP, /islemler?tur=HURDA_ALIS, 5 g, 2.000, onay ve "kisa" gerekçe.
- `takas`: 3 g, 3.000. SAHİP'te onay açık, TEZGAHTAR'da yetki cümlesi.
- `ramat`: SAHİP, /hurda, 1 has, 9.000, onay.
- `altin`: SAHİP, satış fişi, ürün, ödemeyi böl, ALTIN 22 ayar 5 g 2.000, onay.

| durum (rol) | 1280 B-açık | 1280 B-koyu | 375 B-açık | 375 B-koyu | 1280 P-açık | 1280 P-koyu | 375 P-açık | 375 P-koyu |
|---|---|---|---|---|---|---|---|---|
| hurda (SAHİP) | K1 0 · K3 0/0 · K5 0 · K9 0 · Y 0 · bant 1 · onay+gerekçe | = | = | = | = | = | = | = |
| takas (SAHİP) | aynı, onay+gerekçe | = | = | = | = | = | = | = |
| ramat (SAHİP) | aynı, onay+gerekçe | = | = | = | = | = | = | = |
| altin (SAHİP) | aynı, onay+gerekçe | = | = | = | = | = | = | = |
| takas (TEZGAHTAR) | K1/K3/K5/K9/Y 0 · bant 1 · onay YOK · yetki 1×/12 px | = | = | = | = | = | = | = |

40 ölçümün 40'ında K1 gizli-görünen, K3 taşan/kırık, K5 'TRY', K9 binen ve yatay kaydırma **sıfır**. Mod her satırda `modG` alanıyla doğrulandı. **TEZGAHTAR üst çubuktaki BASİT ve PROFESYONEL düğmeleriyle modunu değiştirebildi** (`aria-pressed` ok, 403 bandı yok, SQL izi var). KUR-BANT § 0.7/2 kapandı.
- Ara ölçüm: `altin` durumunun ilk koşumunda K9=5 çıktı. Sebep, betiğin açık bıraktığı barkod öneri listesiydi ve ürün fişe eklenmemişti. Betik gerçek fare tıkı ve birim fiyatla düzeltildi; son koşumda K9 0.

Görülen görüntüler (Read 4):
- `hurda_SAHIP_BASIT_aydinlik_375.png`: alan altında kırmızı bant cümlesi (3.318,00–7.299,60 TL), "Fiyat doğru, yine de kaydet" işaretli, "Neden bu fiyat?" kutusu, "Gerekçe en az 20 karakter olmalı (şu an 4)". Taşma yok.
- `takas_TEZGAHTAR_PRO_karanlik_375.png`: bant cümlesi ve altında tek, küçük, gri "Fiyat doğruysa bunu yalnız dükkân sahibi ya da müdür onaylayabilir."; onay kutusu yok.
- `ramat_SAHIP_PRO_karanlik_1280.png`: Birim fiyat 9000, bant cümlesi (5.311,20–7.966,80 TL), onay ve gerekçe.
- `takas_SAHIP_PRO_aydinlik_1280.png`: üç alanlı ızgara bozulmadan bant cümlesi fiyat sütununda, onay ve gerekçe tam genişlikte.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kaynak § 0.6 aynen: hazır çağrı, satırlar, hurda oranı ekip oyu, fikstür etkisi | KARŞILANDI | § 1, § 2, § 5 fikstür |
| Kur kuralı işlem içinde: mutlak sert 500–50.000; sapma yalnız SAHİP/MÜDÜR onayı + 20 karakter gerekçe + denetim izi; referans güncel has | KARŞILANDI | IslemFiyatKapisi; IB09–IB18; canlı 4 iz satırı |
| Hurda alış oranı %50–%110 (ekip oyu, gerekçeli) | KARŞILANDI | § 2; IB01; MIB-2 |
| Hasım değerleri: 0, eksi, 5, 99.999.999, harf, onaysız %30, TEZGAHTAR onayı | KARŞILANDI | IB10, IB17, IB18; canlı tablo |
| Her işlem türünde bant içi / dışı / sert sınır testi + HTTP 400 alanlı + ekranda aynı metin | KARŞILANDI | IB10–IB18, IB30–IB32, IB08; Chrome |
| SAHİP onaylı aşım 200 + denetim izi SQL | KARŞILANDI | canlı + SQL |
| TEZGAHTAR onayı 403 | KARŞILANDI | IB17, IB18, IB09; canlı takas ve fiş |
| TEZGAHTAR PATCH yalnız mod 200, başka alan 400/403 | KARŞILANDI | IB20–IB22; canlı; Chrome üst çubuk |
| Yetki genişlemesi yok (yalnız mod) | KARŞILANDI | IB21 ×5, MIB-4, IB23/MIB-8 |
| Mutasyon ≥ 4 kırmızı, cmp AYNI | KARŞILANDI | 9/9 (§ 6) |
| Gerçek Chrome 1280+375 · BASİT+PRO · açık+koyu | KARŞILANDI | Tur 1: 40 ölçüm; tur 2: Değişim 24 ölçüm (1440 dahil), Read 4 |
| Hakem KRİTİK (BASİT Değişim bindirme) | KARŞILANDI | § 0, § 9.1 |
| Tek sayı biçimi (mutlak + bant) | KARŞILANDI | IB06, MIB-9, canlı, Chrome |
| build 0 hata, denetim TEMİZ (P-30 hariç) | Paket kapısı KARŞILANDI: build 0 hata, paketin sayaçları ✓; ağaçta ✗ olan GIRDI-2 ve B-3 başka şeritlerin (§ 7) | § 7 |
| Tam süit 0 yeni kırmızı (bir kez) | KARŞILANDI | 11.983/11.983, 0 başarısız (tur 2) |
| Test kiracısı silindi | KARŞILANDI | kendi kiracım 0; tenants 20 → 24 farkı eşzamanlı şeritlerin (§ 8) |
| COMMIT yok, dosya silme yok, sır basılmadı | KARŞILANDI (tur 1 jeton başı olayı kayıtlı; tur 2'de yok) | — |

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` listesi. Bazı dizinler depoda izlenmediği için dosya adıyla yazıldı.
- **Core:** `Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs`, `Kuyumcu.Core/Domain/Girdi/GirdiHatasi.cs`, `Kuyumcu.Core/I18n/Sozlukler.Girdi.cs`, `Kuyumcu.Core/I18n/Sozlukler.IslemBant.cs` (YENİ)
- **Contracts:** `Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs`
- **API:** `Kuyumcu.Api/Modules/Satis/IslemFiyatKapisi.cs` (YENİ), `Kuyumcu.Api/Modules/Satis/SatisModulu.cs`, `Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs`
- **Web:** `Bilesenler/Fis/IslemBantGosterim.cs` (YENİ), `Bilesenler/Fis/IslemBantOnay.razor` (YENİ), `Bilesenler/Fis/FisHurdaAlis.razor`, `Bilesenler/Fis/FisTakas.razor`, `Bilesenler/Fis/FisModelleri.cs`, `Bilesenler/Fis/FisAltinBacagi.cs`, `Bilesenler/AltinBacagiPaneli.razor`, `Servisler/AltinBacagiGirisi.cs`, `Pages/Islemler.razor`, `Pages/Hurda.razor`
- **Tests:** `IslemBantTestleri.cs` (YENİ), `IslemBantFikstur.cs` (YENİ), `S1fGorunumKartiTestleri.cs` ve § 5'teki 31 fikstür dosyası. Tur 2: `KurBantTestleri.cs` (KB06 metni), `Dogrulayici/KURBANTSinirTestleri.cs` (D01 metni), `Dogrulayici/ISLEMBANTSinirTestleri.cs` (mutlak metni).
- **Tur 2 web:** `Kuyumcu.Web/wwwroot/css/degisim-gunsonu.css` (yalnız sona eklenen "ISLEM-BANT tur 2" seçicileri; mevcut kural değişmedi).
- **Dokunulan sayfalar (tur 2):** razor sayfası açılmadı; yalnız CSS ve sözlük.
- Başka şeridin bu pakete uyumu: `MasakEszaman2Testleri.cs:45` "ISLEM-BANT bandının içinde" yorumuyla 16:30'da güncellenmiş. Benim değişikliğim değil.

## 12. Açık kalanlar
1. **Gümüş/platin/paladyum işlem bandı yok.** Kur kaydı yalnız altın has fiyatı tutuyor, referans olmadığı için bant uygulanmıyor (fail-open değil, kapsam dışı). Gümüş için referans kaynağı (kur kaydına gümüş has fiyatı) ayrı paket ister. DÜŞÜK/ORTA.
2. Takas BASİT tuvalinde (`FisTakasTuval`) onay bileşeni `FisTakas` "ESKI" bölümüyle birlikte geliyor. Ayrı bir BASİT-tuval ölçümü yalnız `takas` durumunun BASİT satırlarıyla yapıldı.
3. Başkasına ait denetim ✗'leri (§ 7) sahipleriyle: P-30 tarih kapısı; GIRDI-2 MASAK-ESZAMAN (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`); B-3 ESIK_ISTEMCIDE 6/4 (`Pages/Islemler.YasalKapiGeri.cs:76-77` YASAL-KAPI-GERI, `Bilesenler/Fis/FisOdemeKasa.razor:15,85` yorumları). Tur 1'deki P-23 ve TOPLU-1 (YILDIZ) bu koşumda ✓.
4. `Pages/Hurda.razor` bu paket sırasında bir kez kendi yedeğimden geri yüklendi (ilk form-engelleme denemesi geri alındı). O andaki fark yalnız benim satırlarımdı. Arada başka şerit dokunduysa üzerine yazılmış olabilir. Orkestratör birleştirmede `Hurda.razor`'u gözden geçirsin.

## 13. Kurucu / mali müşavir
Mevzuat sorusu yok: iş kuralı, yasal eşik yazılmadı. Kurucuya bilgi:
- Hurda alış, takas ve eski altınla ödemede has gram fiyatı güncel has fiyatının %50'si ile %110'u arasında olmalı.
- Ramat satışında fiyat ± %20 içinde olmalı.
- Dışına çıkan fiyatı yalnız dükkân sahibi ya da müdür, "Fiyat doğru, yine de kaydet" kutusunu işaretleyip en az 20 karakter gerekçe yazarak kaydedebilir. Bu kayıt işlem izine yazılır.
- 500 TL altı ve 50.000 TL üstü has fiyatı kimse kaydedemez.
- Oranlar `FiyatBandi.cs`'te tek sabitten değiştirilir.
