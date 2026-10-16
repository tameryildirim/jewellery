# PAKET KESIF-P6 · SİPARİŞ-UYUM-KUR-YÖNETİM — uygulama raporu (01.10.2026, şerit t2)

Uygulayıcı: Taşkın (backend) + Ertuğ (frontend). COMMIT YOK. Migration gerekmedi (şema değişmedi).

## 1. Yapılanlar

| Bulgu | Değişiklik | Dosya |
|---|---|---|
| #13 | "Siparişi oluştur" `btn btn-birincil`, İptal `btn btn-ikincil` (ölçüm: ikisi de 44 px). Form açıkken üstteki #sip-yeni-ac ikincil olur (TOPLU DUGME-2: ekranda tek birincil). Tür süzgeç düğmelerindeki `dugme-kucuk` kaldırıldı (ortak boy). | Siparisler.razor |
| #34 | Kalem TABLOSU yerine her kalem bir kart (`.sip-kalem`, auto-fit ızgara). 1440: kap 1074 = scrollWidth 1074; 375: ana alan 375/375. ADET her genişlikte görünür. Kimlikler aynı. | Siparisler.razor, zo-islem.css |
| #35 | Müşteri TEK ortak Secici (#sip-f-cari; ayrı "Ara" kutusu kaldırıldı, seçenekte yalnız ad, ham tür kodu yok). Formdaki tüm yerli `select` (tür, şube, ürün) Secici oldu; kalem ayarı serbest metin değil Ayar seçicisi (Ayarlar.EkranSecenekleri — Emanet ile aynı dizi). BASİT: etiket "Müşteri", teklif düğmesi "Fiyatı hesapla" ("sunucu" 0). K17: SayfaBasi açıklama satırı kaldırıldı (bilgi #sip-defter-notu'da tek kez). | Siparisler.razor, Sozlukler.KesifP6.cs |
| #29 / #75 | Pariteler BİRİM sözlükten (Has gram / Gram / Adet). Kod sütunu kaldırıldı; ekip kararı #2 gereği ham kod yalnız PRO'da ad hücresinin `title` ipucunda. | Kur.razor |
| #86 | Beyan başlığında kademe sözlükten ("İşlem amacı beyanı"); AMAC_BEYANI görünür metinde 0. | Uyum.razor |
| #72 alt | Saklama Süresi sekmesi yalnız SAHİP'te çizilir (uçların hepsi [RolGerekli("SAHIP")]). MÜDÜR'de saklama ucuna istek 0, 403 bandı 0. | Uyum.razor |
| #28 | Kod değişikliği gerekmedi — ölçüldü: 960 px BASİT/PRO açık/koyu "İKİ AŞAMALI" başlığı tablo kabında tam görünür (sağ 871 ≤ kap 935), K9 0. | — |
| #47 | Kod değişikliği gerekmedi — ölçüldü: "abc" ve "1,500.75" → kutu yazılanı korur, aria-invalid, alan altı "Sayı anlaşılmadı…", Kaydet pasif + sebep; bUnit'te tıklamada POST 0. Keşifteki sessiz geri dönüş ORTAK-1 sayı kapısından (Banka'da CascadingValue) sonra yok. | test eklendi |
| YETKİ | /kur ekranındaki ikinci kaynak seçicisi kaldırıldı → salt okunur kaynak adı; SAHİP'e "İşletme ayarlarına git" (/isletme — onay kalıbı orada), diğer rollere "yalnız dükkân sahibi değiştirebilir". Sunucu: PATCH /kur/kaynak `[RolGerekli("SAHIP")]`. | Kur.razor, KurModulu.cs |
| POS | `OranDogrula`: decimal(5,4) ölçeğinden ince oran (0,01855) → 400, yuvarlanmaz. Üç yazma yolu: cihaz oluştur, cihaz PATCH, taksit oranı. | PosModulu.cs |
| ORTAK-1 DÜŞÜK | Uyum risk notu ile kart arası boşluk (#uy-risk-kapali). | zo-islem.css |

### Sapmalar ve gerekçeler
- Spesifikasyondaki "mevcut durum" #28 ve #47 için koda/tarayıcıya karşı doğrulandı: ikisi de önceki paketlerle (ortak.css K9 kuralı, ORTAK-1 sayı kapısı) zaten kapanmıştı; kod yazılmadı, kanıt ve test eklendi.
- S3eKurKaynagiEkranTestleri'nde kaynak seçicisini sınayan 3 test, ekip oyuyla değişen sözleşmeye göre yeniden yazıldı (seçici yok / salt okunur / PATCH yok / SAHİP–MÜDÜR farkı). Kaynak değişikliğini "yeniden okuma tetikleyicisi" olarak kullanan 3 kararlılık testi manuel kur kaydına (güncel+geçmiş yeniden okunur) ve parite için açılış okumasına taşındı; kural (UX-S28) aynen sınanıyor.
- ArayuzModuTestleri `Kur_kaynagi_TEZGAHTARA_403_MUDURE_aciktir` → `Kur_kaynagi_TEZGAHTAR_ve_MUDURE_403_SAHIPE_aciktir` (yeni yetki kararı).
- EkranTestleri.UyumEkranTestleri: saklama testleri sekmeyi sıra no ile ([2]) seçiyordu → `#uy-sekme-saklama` + SAHİP oturumu; ayrıca YASAL-3'ün SaklamaBekletmePaneli için sahte API'ye gerçek şekilli `/masak/saklama-bekletmeleri` yanıtı eklendi (önceden '{}' → NRE ile zaten kırmızıydı).
- SiparisEkranTestleri: `#sip-f-cari` / `#sip-f-kalem-urun-0` `.Change()` → `SeciciYardim.Sec` (kimlikler aynı, bileşen değişti).
- Kalem tablosu q10-yigin yerine kart ızgarası: Secici her hücrede kendi etiketini çizdiği için tablo hücresinde mükerrer başlık olurdu; kabul ölçütünün diğer kolu (kaba sığar, ADET görünür, 375 taşma 0) ölçüldü.

## 2. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Mevcut uçlar: PATCH /kur/kaynak (SAHİP; İşletme onay kalıbından), POS cihaz/taksit PATCH (mevcut). Kur ekranında kaynak bilerek salt okunur: gerekçe metni (#kur-kaynak-yalniz-sahip / #kur-kaynak-isletmede) ve tek düzeltme yolu (İşletme bağlantısı) ekranda.

## 3. Testler
- Önce (bu oturumun ilk geniş koşumu): 8475 filtreli; tam süit sonu: **8533 test, 8514 geçti, 18 başarısız** (tur 2 koşumu, `/private/tmp/kuyumcu-serit/KESIF-P6/tam3.log`).
- Yeni testler: KesifP6EkranTestleri (15 durum: Siparis_olustur_dugmesi_tasarim_sinifi, Siparis_musteri_tek_secici_ham_tur_yok ×2, Siparis_kalemleri_kart_satiri_ADET_alani_her_kalemde_var, Kur_parite_birim_sozlukten ×2, Uyum_beyan_kademesi_sozlukten, Kademe_cevirisi_tek_kaynak ×5, Uyum_saklama_sekmesi_MUDUR_gorunmez ×3), KesifP6HttpTestleri (3), BankaEkranTestleri.Banka_dekont_abc_kaydet_pasif (×2: "abc", "1,500.75"), S3e yeni 4 test. Toplam 24 yeni durum.
- Bu paketin testleri tam süitte 0 kırmızı. Tur 1 tam süitte iki yeni S3e testi kırmızıydı (rol çizimden önce atanınca ilk yükleme beklemede kalıyordu) → rol çizimden sonra atanacak şekilde düzeltildi, tur 2'de yeşil.

## 4. Mutasyon tablosu (hepsi geri alındı, `cmp` birebir AYNI; yedek /private/tmp/kuyumcu-serit/mut-yedek/KESIF-P6/)

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M6-1 | PATCH /kur/kaynak rolü SAHIP→SAHIP,MUDUR | KESIFP6_Kur_kaynagi_MUDURE_403_ve_kiraci_ayari_DEGISMEZ, Kur_kaynagi_TEZGAHTAR_ve_MUDURE_403_SAHIPE_aciktir |
| M6-2 | POS ölçek kapısı kapatıldı | KESIFP6_POS_komisyon_orani_4_haneden_INCE_ise_400_yuvarlanmaz |
| M6-3 | Cari seçeneğine ham tür eklendi | Siparis_musteri_tek_secici_ham_tur_yok (×2) |
| M6-4 | Oluştur düğmesinden btn-birincil kaldırıldı | Siparis_olustur_dugmesi_tasarim_sinifi |
| M6-5 | Parite birimi ham kod | Kur_parite_birim_sozlukten (×2) |
| M6-6 | Beyan kademesi ham kod | Uyum_beyan_kademesi_sozlukten |
| M6-7 | SahipMi → true | Uyum_saklama_sekmesi_MUDUR_gorunmez (MUDUR, MUHASEBE) |
| M6-8 | title her modda ham kod | Kur_parite_birim_sozlukten(BASIT) |
| M6-9 | BASİT teklif metni HUB ("sunucudan") | Siparis_musteri_tek_secici_ham_tur_yok(basit: True) |
| M6-10 | SAHİP/diğer dalı ters | KESIFP6_Kur_kaynagi_bu_ekranda_PATCH_ATMAZ…, KESIFP6_MUDUR_kur_kaynagini_DEGISTIREMEZ… |

Son mutasyondan sonra temiz build: 0 hata 0 uyarı (`build-son.log`).

## 5. Denetim
`bash denetim.sh` → 302 ✓, 2 ✗: **P-30 DEMO_INSAN_SURESI** (kurucu kalemi — kabulde hariç) ve **TOPLU-1: TARIH_TEK = 24** (TOPLU-1 şeridinin yeni sayacı; listedeki Kur.razor:148/376/626-627 ve Banka.razor:904/916 satırları bu paketten ÖNCE de vardı, bu paket eklemedi). Bu paketin sayaçlarında ihlal yok.

## 6. Canlı doğrulama (API 5162, test kiracısı PAKETKESIF-P6-TEST Canli)

| Adım | Beklenen | Gerçek |
|---|---|---|
| MÜDÜR PATCH /kur/kaynak {ANKARA} | 403 | 403 "Bu işlem için gerekli rol: dükkân sahibi" |
| SAHİP PATCH /kur/kaynak {YOKBOYLE} | 400 | 400 "Geçersiz kur kaynağı…" |
| SAHİP PATCH /kur/kaynak {ANKARA} | 200 | 200 {secili: ANKARA}; SQL tenants.kur_kaynagi = ANKARA |
| POST /pos/cihazlar oran 0,01855 | 400 | 400 "Komisyon oranı en çok 4 ondalık haneli olabilir" |
| POST /pos/cihazlar oran 0,0185 | 200 | 200, pesinKomisyonOran 0.0185 |
| PATCH cihaz 0,01855 | 400, kayıt değişmez | 400; SQL pos_cihazlari.pesin_komisyon_oran = 0.0185 |
| POST taksit oranı 0,01855 / 0,0399 | 400 / 200 | 400 / 200; SQL pos_taksit_oranlari: 3 taksit 0.0399 (yalnız geçerli olan) |
| Dekont 250.000 + MASAK | tespit + beyan AMAC_BEYANI | 200; GET detay beyan.kademe = AMAC_BEYANI → ekranda "İşlem amacı beyanı" |

### Gerçek tarayıcı (başsız Chrome, CDP 9905, vekil 5163; sikisma-olc.js) — ölçüm dosyaları `/private/tmp/kuyumcu-serit/KESIF-P6/olcum*.jsonl`, görüntüler `foto/`

| Rota/durum | Genişlik | Mod × tema | K1 | K3 | K5 | K7 | K9 | K10 | K11 | Yatay | Özel ölçüm |
|---|---|---|---|---|---|---|---|---|---|---|---|
| /siparisler yeni form + 2 kalem | 1440 · 1280 · 960 · 375 | BASİT/PRO × açık/koyu (16) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | kalem sW=cW (1074/914/594/317); select 0; Ara kutusu 0; BASİT "sunucu" 0; Oluştur `btn btn-birincil` 44 px = İptal 44 px |
| /kur Pariteler | 1440 · 1280 · 960 · 375 | 16 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | ham kod 0; birimler [Has gram, Gram, Adet] |
| /kur Kaynak (SAHİP) | 1280 · 375 | PRO × açık/koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | seçici yok; "Ankara Altın Borsası"; bağlantı /isletme |
| /kur Kaynak (MÜDÜR) | 1280 · 375 | PRO × açık/koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | seçici yok; "yalnız dükkân sahibi…" |
| /uyum (SAHİP) | 1280 · 960 · 375 | 12 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | saklama sekmesi var |
| /uyum (MÜDÜR) | 1280 · 375 | 8 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | saklama sekmesi 0, 403 bandı 0 |
| /uyum kimlik detay penceresi | 1280 · 375 | 8 | 0 | 0/0 | 0 | 0 | 6* | 0 | 0 | 0 | başlık "Beyan (İşlem amacı beyanı)", AMAC_BEYANI 0 |
| /kullanicilar | 1280 · 960 · 375 | 12 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | İKİ AŞAMALI tam görünür (960: 871 ≤ 935) |
| /banka | 1280 · 960 · 375 | 12 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| /banka dekont "abc" | 1440 · 1280 · 375 | 8+ | 0 | 0/0 | 0 | 0 | 4–6* | 0 | 0 | 0 | aria-invalid, alan altı hata, Kaydet pasif + sebep |
| /banka dekont "1,500.75" | 1280 | PRO koyu | 0 | 0/0 | 0 | 0 | 5* | 0 | 0 | 0 | geçersiz, Kaydet pasif |

\* K9: açık pencere (sağ çekmece / modal) arkasındaki sayfa metniyle kesişme sayılıyor ("TOPLAM BAKİYE ⟂ Dekont Gir", "SÜRESİ YAKLAŞAN ⟂ T.C. kimlik kartı") — katman üstü örtme, gerçek binme değil; aynı sayfalar penceresiz ölçümde K9 = 0.
K8: /kullanicilar BASİT'te 1 ("E-POSTA" sütun başlığı jargon kataloğunda) — HUB metni, P8'in alanı; bu paket değiştirmedi.
Görüntüler Read ile incelendi: siparis__BASIT_aydinlik_1440-tam, siparis__PRO_karanlik_1440-tam, siparis__BASIT_aydinlik_375-tam, kur-kaynak__PRO_karanlik_1280, mudur-kur-kaynak__PRO_aydinlik_375, kur-parite__PRO_aydinlik_1280, uyum__BASIT_aydinlik_1280, banka-dekont__PRO_karanlik_375.

### Temizlik
API (PID dosyası api.pid) ve vekil kill ile durduruldu; 5162/5163/9905 dinleyen yok. Test kiracısı `zsh kiraci-temizle.sh 01M3TPQQC9MQA397JXBZEHWY93 --uygula` ile silindi; `select count(*) from tenants where id=…` = 0. tenants: başlangıç 14 → kiracım eklenince 15 → son 13. Fark (−1) eşzamanlı başka şeridin kendi test kiracısını silmesinden; QA-TEST-* ve DEMO kiracılarına dokunulmadı.

## 7. Değişen dosyalar
- dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP6.cs (yeni)
- dotnet/Kuyumcu.Web/Pages/Siparisler.razor · Kur.razor · Uyum.razor
- dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css
- dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs (yalnız kaynak ucu rolü) · Modules/Pos/PosModulu.cs (OranDogrula)
- Testler: KesifP6EkranTestleri.cs (yeni), KesifP6HttpTestleri.cs (yeni), BankaEkranTestleri.cs, SiparisEkranTestleri.cs, S3eKurKaynagiEkranTestleri.cs, ArayuzModuTestleri.cs, EkranTestleri.cs
- Dokunulan sayfalar (TEK DOKUNUŞ): Siparisler.razor, Kur.razor, Uyum.razor; tur 2'de Panel.razor (yalnız kur kartı kaynak bölümü — gerekçe § 11) ve Banka.razor (yalnız hareket satırı karşı taraf hücresi sınıfı). Kullanicilar.razor açılmadı.

## 8. Koşul karşılama
| Koşul | Durum |
|---|---|
| #13 tasarım sınıfı + eşit boy | KARŞILANDI (44/44 px) |
| #34 1440'ta sığar, ADET görünür, 375 taşma 0 | KARŞILANDI |
| #35 tek seçici, ham tür 0, Ayar seçicisi, BASİT "sunucu" 0 | KARŞILANDI |
| #29/#75 birim sözlükten, ham kod 0 (BASİT+PRO; PRO title) | KARŞILANDI |
| #86 kademe sözlükten | KARŞILANDI |
| #28 960 K9 0, İKİ AŞAMALI tam | KARŞILANDI (ölçüm) |
| #47 abc / 1,500.75 → hata, Kaydet pasif, istek 0 | KARŞILANDI |
| #72 alt MÜDÜR'de sekme yok, 403 bandı 0 | KARŞILANDI |
| MÜDÜR PATCH /kur/kaynak 403 (canlı) | KARŞILANDI |
| POS 0,01855 → 400 | KARŞILANDI |
| Build 0/0 | KARŞILANDI |
| Denetim (P-30 hariç) | Bu paketin sayaçları temiz; TOPLU-1 TARIH_TEK ✗ başka şeridin |
| Tam süit 0 yeni kırmızı | KARŞILANDI (18 kırmızı, hiçbiri bu paketin dosya/testi değil) |

## 9. Açık kalanlar (sahibiyle)
- Tam süit kırmızıları (18): GiderEkranTestleri ×4, P19EkranTestleri ×3, Bicim2GiderPostTestleri ×1 (Gider.razor 06:10'da başka şeritçe değişti); DuzeltmePolitikasiTestleri (StopajOdemesi — YASAL-4 Y4-B), Yasal4HttpTestleri STOPAJ (YASAL-4); Toplu1SafTestleri.TARIH_TEK, Toplu1FaturaTestleri, FaturaEkranTestleri.KuyumTaslaginaToplamGonderilmez, CariBakiyeEkranTestleri, KesifP2EkranTestleri.CariDetay (TOPLU-1 Cariler/Faturalar); Q105SinirTestleri + P12EkranTestleri (Vezne — TOPLU-1); SATIS3EkranTestleri.Barkod (yalnız son koşumda; zamanlama kararsızlığı).
- Denetim TOPLU-1 TARIH_TEK: Kur.razor ve Banka.razor'daki eski tarih biçimleri bu sayaçta; dosyalar bu paketin sahipliğinde — TOPLU-1 düzeltmesi bu satırlara girecekse kapıda birleştirme gerekir.
- İşletme ekranındaki kaynak seçicisinin onay kalıbı (KesifP1 testleri onaylıyor) korunuyor; HUB metni `Kur.KaynakAciklama` ("değiştirdiğinizde…") artık salt okunur kartta da görünüyor — P8 (HUB) sade metni güncellemeli.
- Satış ödeme bacağındaki `kasa_hareketleri.komisyon_oran` (decimal(5,4)) yazma yolu bu paketin POS kapısı dışında; aynı ölçek kontrolü gerekebilir (ayrı paket).
- Banka TOPLU § ZO-ISLEM-A maddeleri: tur 2'de KAPATILDI (§ 11).
- /kullanicilar BASİT K8 = 1 ("E-POSTA" — P8 HUB).
- Test projesinde iki uyarı başka şeritlerin dosyalarında idi (SUBEKAPSAMASinirTestleri CS8601, KesifP2Testleri xUnit2029); son build'de 0 uyarı.

## 10. Kurucu / mali müşavir
Mevzuat hükmü içermez (POS ölçeği teknik sütun sınırıdır; MASAK kademe eşikleri değişmedi, yalnız ad çevirisi).

## 11. Tur 2 — doğrulayıcı bulgularının kapatılması (01.10.2026 09:00–09:25)

### 11.1 Bulgular ve yapılanlar
| Bulgu | Yapılan | Dosya |
|---|---|---|
| [KRİTİK] Panel kur kartındaki onaysız `#kur-kaynak` seçicisi (MÜDÜR'e yalnız 403; SAHİP'e onaysız ikinci yol) | Seçici ve `PATCH /kur/kaynak` çağrısı Panel'den kaldırıldı. Kart artık salt okunur: `#panel-kur-kaynak-ad` (kaynak adı), SAHİP'e `#panel-kur-kaynak-isletme-bag` (/isletme — onay kalıbı orada), diğer rollere `#panel-kur-kaynak-yalniz-sahip` ("Kur kaynağını yalnız dükkân sahibi değiştirebilir."). Kaynak artık TEK yerde (İşletme, onaylı) değişir. | Panel.razor (yalnız kaynak bölümü), Sozlukler.KesifP6.cs |
| [ORTA] Sipariş müşteri seçimi 200 üstünde sunucu aramasını kaybetmişti | `CariUzakMi` (aktif müşteri > Secici.YerelTavan) → `#sip-f-cari` `UzakTur=CARI` ile sunucuda arar (`/arama?q=…&tur=CARI`); küçük dükkânda yerel liste (istek yok). | Siparisler.razor |
| [ORTA] Banka ZO-ISLEM-A (375 rozetler, iki birincil, boş '—') | 375'te `#bh-hareket-tablo { min-width: 0 }` (app.css'teki max-content tabloyu 452 px'e genişletiyordu → rozetler görünüm dışında, sağ 466 > 375). Karşı taraf + açıklama ikisi boşken iki '—' açıcısı TEK satırda (`zo-karsi-ikisi-bos`); açıcılar ve kimlikleri korunur. İki birincil: ölçüldü, sayfada tek birincil (`banka-yeni`). | Banka.razor, zo-islem.css |
| [DÜŞÜK] PosModulu XML yorumu yanlış üyede | `OranDogrula` summary'si kendi metodunun üstüne taşındı; `OranOlcegi`'nde tek summary. | PosModulu.cs |
| (tam süitte çıkan, bu paketin) SozlesmeKapsamTestleri ×2 | Panel'deki `PatchAsync<KaynakSonuc>` kalkınca kilitsiz tip 59 → 58: cırcır listesinden `KaynakSonuc` silindi, denetim.sh `SOZLESME_KILITSIZ_TAVAN` 59 → 58 (tavan yalnız azalır kuralı). | SozlesmeKapsamTestleri.cs, denetim.sh |

**TEK DOKUNUŞ gerekçesi (kapı kaydı için):** Panel.razor TOPLU-1 şeridinin tarih düzeltmelerine de açık. Bu pakette yalnız kur kartının kaynak bölümü (satır ~213-228 ve @code'daki kaynak yazma kodu) değişti. Gerekçe: P6'nın sunucu yetki kararı (PATCH /kur/kaynak yalnız SAHİP) Panel'deki seçiciyi MÜDÜR için ölü düğmeye çevirdi. Bu paketin yol açtığı gerilemeyi düzeltmek için dokunuldu. TOPLU-1'in tarih satırlarına dokunulmadı.

**Eşzamanlılık olayı (Anayasa md.1, açıkça):** M6-12 mutasyonu sırasında (09:02-09:04) TOPLU-1 şeridi Panel.razor'un anlık kopyasını aldı (`/private/tmp/kuyumcu-serit/TOPLU-1/tarih-yedek2/Panel.razor.yeni`). Benim geri almamdan sonra bu kopyayı 09:04:08'de ağaca geri yazdı. Mutasyon satırı (`<div id="kur-kaynak" role="combobox"></div>`) böylece canlı dosyaya sızdı. Fark edildi ve yalnız o satır silindi. Canlı dosya, mutasyon öncesi yedekle `cmp` ile birebir AYNI. Son kontrolde de `grep -c 'role="combobox"></div>' Panel.razor` = 0. TOPLU-1'in `Panel.razor.yeni` dosyası hâlâ mutasyonlu kopyayı taşıyor. **TOPLU-1 o dosyayı yeniden uygulamamalı.** Orkestratör birleştirmede `grep -c 'role="combobox"></div>' Panel.razor` = 0 olduğunu denetlemeli. Aynı yarışta iki mutasyon koşumu bayat derlemeyle yanlış "yeşil" verdi. M6-12 elle yeniden derlendi ve kırmızı olduğu doğrulandı (aşağıda).

### 11.2 Mutasyonlar (tur 2; yedek /private/tmp/kuyumcu-serit/mut-yedek/KESIF-P6/t3/, hepsi geri alındı, cmp AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M6-11 | Panel `KaynakDegistirebilir`: `== "SAHIP"` → `!= "SAHIP"` | KesifP6PanelTestleri.Panel_kur_kaynagi_SAHIPE_Isletme_baglantisi_verir; Panel_kur_kaynagi_diger_rollere_sebep_yazar_baglanti_yok (MUDUR, TEZGAHTAR) |
| M6-12 | Panel kartına `#kur-kaynak` combobox geri eklendi | KesifP6PanelTestleri.Panel_kur_kaynagi_secici_yok_PATCH_yok (SAHIP, MUDUR, MUHASEBE, TEZGAHTAR) — 4/7 kırmızı |
| M6-13 | Siparisler `CariUzakMi` → `false` | KesifP6EkranTestleri.Siparis_musteri_200_ustu_dukkanda_sunucuda_aranir_ve_secilir |
| M6-14 | Banka `IkisiBos` → `false` | BankaEkranTestleri.KESIFP6_Banka_iki_bos_alan_tek_satir_sinifi_acicilar_korunur |
| M6-15 | POS ölçek kapısı `if (false)` | KesifP6HttpTestleri.KESIFP6_POS_komisyon_orani_4_haneden_INCE_ise_400_yuvarlanmaz |
Son mutasyondan sonra temiz build: 0 uyarı 0 hata (`build-t3son.log`; kapsam düzeltmesinden sonra `build-t3b.log` 0/0).

### 11.3 Testler
- Filtreli (KesifP6*, Banka/Siparis/Uyum ekran, S3e, ArayuzModu, S35, S1c, KesifP1): **255/255 geçti**.
- Tam süit (tur başına bir kez, `tam4.log`, trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P6`): **8557 test, 8553 geçti, 3 başarısız, 1 atlandı**.
  - SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu ve Kapsam_orani_beyan_edilen_degerle_ayni → **bu paketin**. Düzeltildi (11.1); filtreli koşumda 3/3 geçti.
  - Yasal4HttpTestleri.K4_teslimden_9_gun_sonra_409… → **YASAL-4 (t1)**. Gider pusulası alanında. Sonraki filtreli koşumda geçti, yani kararsız ya da o şeritçe düzeltilmiş.
  - Tur 1 listesindeki 18 kırmızının hiçbiri artık yok. Bu paketin ürettiği yeni kırmızı: 0 (iki kapsam kırmızısı kapatıldı).
- Denetim (`denetim3.log`): 303 ✓, 1 ✗ = **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, kabulde hariç). `P-03: SOZLESME_KILITSIZ_TIP 58/111 (tavan 58)` ✓.

### 11.4 Canlı (API 5162, kiracı PAKETKESIF-P6-TEST Tur2 = 01M3TT2MKJF3KBYW18DCPF10HB)
| Adım | Beklenen | Gerçek |
|---|---|---|
| MÜDÜR PATCH /kur/kaynak {ANKARA} | 403 | 403 "Bu işlem için gerekli rol: dükkân sahibi"; SQL `tenants.kur_kaynagi` = NULL (değişmedi) |
| SAHİP GET /kur/kaynaklar | 200 | 200, secili ALTINKAYNAK |
| SAHİP POST /pos/cihazlar oran 0,01855 | 400 | 400 "Komisyon oranı en çok 4 ondalık haneli olabilir…" |
| SAHİP POST /pos/cihazlar oran 0,0185 | 200 | 200, pesinKomisyonOran 0.0185; SQL pos_cihazlari: yalnız "P6 T3 POS 0.0185 / 0.0185" |

### 11.5 Gerçek tarayıcı (başsız Chrome CDP 9906/9907, vekil 5163, sikisma-olc.js; `olcum-t3.jsonl`, `olcum-t3-tam.jsonl`, görüntüler `/private/tmp/kuyumcu-serit/KESIF-P6/foto/t3*`)
| Rota | Rol | Genişlik | Mod × tema | K1 | K3 | K5 | K7 | K8 | K9 | K10 | K11 | Yatay | Özel |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| / (Panel kur kartı) | MÜDÜR | 1280 · 375 | BASİT/PRO × açık/koyu (8) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | #kur-kaynak 0; PRO: ad "Altınkaynak", sebep "yalnız dükkân sahibi…", bağlantı yok; BASİT: kur kartı zaten çizilmiyor |
| / (Panel kur kartı) | SAHİP | 1280 · 375 | BASİT/PRO × açık/koyu (8) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | #kur-kaynak 0; PRO: ad + "İşletme ayarlarına git" → /isletme |
| /banka Hareketler açık | MÜDÜR | 1280 · 375 | BASİT/PRO × açık/koyu (8) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | rozet 6, görünüm dışı rozet 0 (önce 6 @466); karşı taraf taşan 0 (önce 3); main sW=cW (375/375, 1012/1012); birincil 1 (banka-yeni) |
Read ile görülen görüntüler: t3m-panel-kur__PRO_aydinlik_1280-tam, t3m-panel-kur__PRO_aydinlik_375, t3s-panel-kur__PRO_karanlik_375-kirp, t3m-banka-hareket__PRO_aydinlik_375-tam, t3m-banka-hareket__PRO_karanlik_375. Banka 375'te Gelen/Giden ve Bekliyor rozetleri kartın içinde, uzun karşı taraf adı sarılıyor, boş satırda iki '—' tek satırda.

### 11.6 Temizlik
API (5162) ve vekil (5163) kill ile durduruldu; 5162/5163/9906/9907 dinleyen yok. `zsh kiraci-temizle.sh 01M3TT2MKJF3KBYW18DCPF10HB --uygula` çalıştırıldı: tenants 16 → 15, kiracım 0 satır. QA-TEST-* kiracıları (4) dokunulmadı.

### 11.7 Tur 2'de değişen dosyalar
Panel.razor, Siparisler.razor, Banka.razor, zo-islem.css, PosModulu.cs (yalnız yorum), Sozlukler.KesifP6.cs (Panel metinleri), KesifP6PanelTestleri.cs (yeni), KesifP6EkranTestleri.cs, BankaEkranTestleri.cs, SozlesmeKapsamTestleri.cs (1 satır), denetim.sh (tavan 59→58).

## 12. Tasarım hakemi turu (01.10.2026 ~10:00–11:45): kalan 6 bulgunun kapatılması

### 12.1 Bulgular ve yapılanlar
| Bulgu | Yapılan | Dosya |
|---|---|---|
| [KRİTİK] Sipariş detayında biçimsiz tarayıcı düğmeleri (`#sipd-duzenle`, `#sipd-gecis-ONAYLANDI` class='' → h30, bg rgb(239,239,239)) | Detaydaki HER düğme ortak aileden. `#sipd-duzenle` = `btn btn-ikincil` olarak kart başlık satırına alındı (sağa yaslı). Tek düğmelik `#sipd-duzenleme` kartı yalnız düzenleme açıkken ya da değişmezlik notu için çizilir. Durum geçişi: ilk ileri geçiş `btn btn-birincil`, diğerleri `btn-ikincil`; ekranda TEK birincil. Etiket eylem dilinde: ONAYLANDI→"Onayla", ATOLYEDE→"Atölyeye gönder", HAZIR→"Hazır olarak işaretle", TESLIM_EDILDI→"Teslim et", TASLAK→"Taslağa al" (`KesifP6Sozlugu.GecisEylemi`; bilinmeyen hedef uydurulmaz, durum adı yazılır). Düzenleme Kaydet/Vazgeç, iptal ve geçiş onayı düğmeleri, kalem sil/ekle: hepsi `btn` ailesi. Özet alanları 12 px kalın-etiket dökümüydü; artık Uyum çekmecesiyle aynı tanım listesi (`#sipd-ozet`; `#sipd-sube` kimliği değer hücresinde, AYNI). | SiparisDetay.razor, Sozlukler.KesifP6.cs, zo-islem.css |
| [YÜKSEK] K17 açıklama satırları ve "işlem uçları" jargonu | Kaldırılanlar: `#sip-defter-notu` (liste kartı), `#sipd-defter-notu` (detay), `#sipd-appendonly` ("Bu döküm yalnız eklenir…"), detay SayfaBasi açıklaması. Sözlük anahtarları HUB'da (P8'in) kaldı, ekrana basılmıyor. | Siparisler.razor, SiparisDetay.razor |
| [YÜKSEK] "Fiyatı hesapla" serbest tanımla açılmıyor, sebep metni yanlış yol gösteriyordu | Sebep metni koşulla eşleştirildi: kur yoksa `KurYok`, değilse `P6.TeklifUrunGerekli` = "Fiyat için listeden ürün seçin." (EN: "Select a product from the list to get a price."). Fiyat kuralı değişmedi: fiyat sunucuda ve yalnız ürünle hesaplanır (UX-S21). Serbest tanımdan fiyat hesaplamak yeni bir fiyatlama kuralı olurdu, bu pakette yazılmadı. | Siparisler.razor, Sozlukler.KesifP6.cs |
| [YÜKSEK] Uyum `tanim-listesi` CSS'siz, "Uyruk TUR" ham kodu | Uyum'daki 4 `dl`'ye `uy-tanim` eklendi. zo-islem.css'te iki sütunlu ızgara var: soluk etiket (`--metin-soluk`, 14 px 600) ve koyu değer (`--metin`); ≤40rem tek sütun. Uyruk `KesifP6Sozlugu.Uyruk` ile MASAK formunun ülke listesinden (`UlkeListesi`) ada çevrilir: TUR → "Türkiye"; listede olmayan kod uydurulmaz. Beyan "İşlem açıklaması" değeri 12 px → gövde boyu. Global `.tanim-listesi`'ye dokunulmadı (GiderPusulalari/YASAL-4 zo-belge.css ile kullanıyor), kapsam sınıfla dar tutuldu. | Uyum.razor, zo-islem.css, Sozlukler.KesifP6.cs |
| [YÜKSEK] Kullanıcılar 1440'ta Eylemler ekran dışında, satırlar 170 px, her satırda dolu kırmızı düğmeler | Satır eylemleri ortak `SatirEylemleri`'nde (Banka kalıbı, `DarSutun`, `Birincil=1`): satırda yalnız "Düzenle" + "⋯". Şifre, Erişimi kapat (tehlike), Aktife al ve Sahipliği devret pencerede; koşullar BİREBİR aynı, özellik silinmedi. Mevcut kimlikler `k-aktiflestir-`, `k-devret-` AYNI. Yeni kimlikler: `k-duzenle-`, `k-sifre-`, `k-pasif-`. Ayrı E-posta sütunu kaldırıldı: e-posta adın altında soluk ikinci satır, 18rem'de kesilir, tamamı `title`'da. `#k-tablo` ≤64rem (1024 px) kart yığını: q10-yigin kalıbı + `data-etiket`. | Kullanicilar.razor, zo-islem.css |
| [YÜKSEK] Yeni sipariş isteğe bağlı alanları açık (K16), detayda geçmiş açık (K15) | Söz verilen gün, Açıklama ve Tahmini tutar + "Fiyatı hesapla" artık "Daha fazla (söz verilen gün, açıklama, tahmini tutar)" bölümünde (Banka dekont `zo-detay` kalıbı) ve açılışta KAPALI. İçinde değer varsa form yeniden açılınca açık gelir, yazılan kaybolmaz. Detayda "Durum geçmişi" ortak `Katlanir` (`#sipd-gecmis`, kapalı, sayı rozetli). Veri kartla geldiği için açılışta istek atılmaz. | Siparisler.razor, SiparisDetay.razor |

**TEK DOKUNUŞ / dosya alanı gerekçesi:** SiparisDetay.razor, Triyaj § KESIF-P6 "Sahip dosyalar" listesinde yok. Hakemin KRİTİK ve K15/K17 bulguları doğrudan bu sayfayı hedefliyor ve sayfa sipariş akışının parçası (P6 = Sipariş). Eşzamanlı şeritlerin (YASAL-4, TOPLU-1, KESIF-P8) dosya listelerinde de yok. Değişiklik yalnız düğme sınıfları ve etiketleri, başlıktaki Düzenle, özet `dl` ve geçmiş akordeonuyla sınırlı; düzenleme mantığı, uçlar ve kimlikler aynı. Kullanicilar.razor bu turda ilk kez açıldı (sahip dosya).

### 12.2 Testler
- Yeni bUnit (KesifP6EkranTestleri, 15 durum): `Siparis_detay_dugmeleri_btn_sinifi_tek_birincil_eylem_dili`, `Siparis_detay_aciklama_satiri_yok_gecmis_kapali`, `Siparis_serbest_tanimli_kalemde_teklif_sebebi_urun_ister`, `Siparis_istege_bagli_alanlar_acilista_kapali_Daha_fazla_ile_acilir`, `Uyum_tanim_listesi_bicimli_ve_uyruk_ulke_adi`, `Uyruk_cevirisi_tek_kaynak` ×4, `Siparis_gecis_eylem_dili` ×5, `Kullanicilar_satir_eylemleri_menude_satirda_tek_duzenle`. Sahte API'ye gerçek şekilli `/siparisler/s1` ve `/kullanicilar` yanıtları eklendi.
- Sözleşmesi bilerek değişen eski testler (gerekçe: hakem bulgusu): SiparisEkranTestleri. Gün/teklif kullanan 5 test önce `#sip-f-detay-ac`'a tıklıyor. `Gecis_dokumu_append_only_notuyla_cizilir` → `Gecis_dokumu_kapali_katlanirda_aciklama_satiri_yok`. OrtakGorunumHakemTestleri.HK8 artık not satırının YOKLUĞUNU sınıyor; HK14 td'ye `data-etiket`. Q93SiparisBaslikEkranTestleri: 3 test önce `#sipd-gecmis-ac`. OrtakGorunumKiyasTestleri KY8/KY10: Kullanicilar kaynak metni (e-posta ikinci satır, `data-etiket`).
- Filtreli koşumlar: etkilenen 17 sınıf **298/298** (`t2-f4.log`); düzeltmelerden sonra **345/346** (`t2-f6.log`). Tek kırmızı `PosBarkodTestleri.Bulunamayan_barkod_KULLANICIYA_soylenir` aynı sırada API derlemesi yarıştı (NETSDK1177 apphost "already signed"); tek başına koşumda 7/7 geçti, kararsızlık. Son: OrtakGorunumKiyas + KesifP6Ekran + OrtakGorunumHakem **74/74** (`t2-f7.log`).
- Tam süit (bu turda BİR kez, `tam-t2.log`, trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P6-t2`): **8723 test, 8715 geçti, 7 başarısız**.
  - Bu paketin 2 kırmızısı: OrtakGorunumKiyasSaflik.KY8 ve KY10 (Kullanicilar.razor kaynak metni). Testler yeni sözleşmeye güncellendi, filtreli koşumda yeşil (74/74). Tam süit son kodla yeniden koşulmadı (tur başına bir kez kuralı); bu paketin kalan yeni kırmızısı 0.
  - Başkasının 5 kırmızısı: FaturaFistenTestleri.Ayni_fisten_ikinci_fatura_400…, FaturaKalemTestleri.Ayni_fisten_ikinci_kalemli_fatura_400_doner, Toplu1FaturaTestleri.T2_Fisten_yaris_reddi_sade_cumle ×3. Sunucu mesajı "Bu fişin kalemleri taslak bir faturaya ba…" olarak değişmiş; FaturaModulu.cs / Faturalar.razor 11:22–11:23'te başka şeritçe değiştirildi (TOPLU-1 / YASAL-4 Faturalar alanı). Bu paket Fatura dosyalarına dokunmadı.

### 12.3 Mutasyonlar (yedek /private/tmp/kuyumcu-serit/mut-yedek/KESIF-P6-t2/, betik `mut-t2.sh`, hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M6-11 | Geçiş düğmesi `class=""` | Siparis_detay_dugmeleri_btn_sinifi_tek_birincil_eylem_dili |
| M6-12 | Geçiş etiketi `DurumMetni(h)` ("Onaylandı") | Siparis_detay_dugmeleri_btn_sinifi_tek_birincil_eylem_dili |
| M6-13 | Teklif sebebi eski `KalemGerekli` | Siparis_serbest_tanimli_kalemde_teklif_sebebi_urun_ister |
| M6-14 | `detayAcik = true` alan başlangıcı | **hiçbir test kırılmadı**: eşdeğer mutant, form açılırken `FormuAcKapat` değeri yeniden hesaplıyor. Anlamlı karşılığı M6-14b ile koşuldu. |
| M6-14b | `FormuAcKapat`: `detayAcik = true` | Siparis_istege_bagli_alanlar_acilista_kapali_Daha_fazla_ile_acilir; Siparis_serbest_tanimli_kalemde_teklif_sebebi_urun_ister; Siparis_musteri_tek_secici_ham_tur_yok(basit: True) |
| M6-15 | Geçmiş `Katlanir BaslangictaAcik="true"` | SiparisEkranTestleri.Gecis_dokumu_kapali_katlanirda_aciklama_satiri_yok; Siparis_detay_aciklama_satiri_yok_gecmis_kapali |
| M6-16 | Uyruk ham kod `@d.Uyruk` | Uyum_tanim_listesi_bicimli_ve_uyruk_ulke_adi |
| M6-17 | Kullanıcılar `DarSutun/Birincil=1` kaldırıldı (satırda 2 eylem + ⋯) | Kullanicilar_satir_eylemleri_menude_satirda_tek_duzenle |
| M6-18 | `.uy-tanim` `display: grid` → `block` | Uyum_tanim_listesi_bicimli_ve_uyruk_ulke_adi |
| M6-19 | Detaya `#sipd-defter-notu` geri eklendi | Siparis_detay_aciklama_satiri_yok_gecmis_kapali |
Mutasyonlardan sonra temiz build 0 hata (`build-t2son4.log`). 5 uyarı yalnız `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601` kaynaklı, doğrulayıcının dosyası; bu paketin dosyalarında uyarı 0.

### 12.4 Denetim (`denetim-t2son.log`): 300 ✓, 4 ✗ — hiçbiri bu paketin dosyalarında değil
- `Pages: @onclick'siz ve disabled'sız <button> 72 (tavan 71)`. Bu paketin sayfalarında sayı tur öncesiyle aynı: SiparisDetay 2→2, Siparisler 2→2, Uyum 9→9, Kullanicilar 0→0. Bu turdaki ara ölçümde (10:3x) sayaç ✓ idi. +1, 11:23'te değişen Faturalar.razor'dan (7 satır, başka şerit).
- `P-08 FATURA_NO_SERBEST_METIN 3 > 2`: `HurdaModulu.cs:259` (YASAL-4 gider pusulası servisi).
- `P-30 DEMO_INSAN_SURESI`: kurucu kalemi, kabulde hariç.
- `CARI_BAKIYE_AYNA_UYUSMAZ (canlı) YENİ 1`: canlı DB'deki başka kiracı verisi. Bu paketin kiracısı silindi, cari bakiye yazan kod değişmedi.

### 12.5 Canlı (API 5162, kiracı "PAKETKESIF-P6-TEST Tur2 Altınkaya Kuyumculuk" = 01M3V77WC3220JSMFR4K2GD179)
| Adım | Beklenen | Gerçek |
|---|---|---|
| MÜDÜR PATCH /kur/kaynak {ANKARA} | 403 | 403 "Bu işlem için gerekli rol: dükkân sahibi"; SQL `tenants.kur_kaynagi` = NULL (değişmedi) |
| SAHİP GET /kur/kaynaklar | 200 | 200, secili ALTINKAYNAK |
| SAHİP POST /pos/cihazlar 0,01855 | 400 | 400 "Komisyon oranı en çok 4 ondalık haneli olabilir (örn. %1,85 → 0,0185)" |
| SAHİP POST /pos/cihazlar 0,0185 | 200 | 200; SQL pos_cihazlari: yalnız "P6 T3 POS 0.0185 / 0.0185" |
| Banka GELEN 1.250.000,75 + MASAK (uyruk TUR) | tespit kaydı | SQL masak_kimlik_tespitleri.uyruk = TUR → ekranda "Türkiye" |
| POST /siparisler ×2 (serbest tanımlı kalemler) | 200, TASLAK | S-2026-000001/2, SQL durum TASLAK |

### 12.6 Gerçek tarayıcı (başsız Chrome CDP 9906, vekil 5163, sikisma-olc.js; `olcum-t2c.jsonl` + `olcum-t2d.jsonl`, görüntüler `/private/tmp/kuyumcu-serit/KESIF-P6/foto-t2/`)
Her satır: 1440 · 1280 · 960 · 375 × BASİT/PRO × açık/koyu = 16 ölçüm.
| Durum | K1 | K3 | K5 | K7 | K8 | K9 | K10 | K11 | Yatay | Özel ölçüm |
|---|---|---|---|---|---|---|---|---|---|---|
| /siparisler yeni form (açılış) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | gün/açıklama/teklif görünür 0; "Daha fazla…" açıcı 1; `#sip-defter-notu` 0; "uçlar" 0; main sW=cW |
| /siparisler serbest tanım + Daha fazla | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | teklif pasif; sebep "Fiyat için listeden ürün seçin."; "serbest tanım" 0 |
| /siparisler/{id} detay (TASLAK) | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | gri/sınıfsız düğme 0; birincil 1 = "Onayla"; Düzenle `btn btn-ikincil` h44 başlıkta; Onayla/İptal h44; düzenleme kartı 0; defter notu/append-only 0; geçmiş aria-expanded=false |
| /uyum Görüntüle çekmecesi | 0 | 0/0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | dl display grid ×2, sütun 144/287 px (375: tek sütun); dt açık rgb(90,101,119) / dd rgb(23,32,51), koyu 166,171,180 / 218,222,230; uyruk "Türkiye", ham TUR 0 |
| /uyum Saklama sekmesi | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | dl grid, iki sütun (375 tek) |
| /kullanicilar | 0 | 0/0 | 0 | 0 | 1** (BASİT) / 0 (PRO) | 0 | 0 | 0 | 0 | tablo sW ≤ cW her genişlikte: 1440 1122/1122 · 1280 962/962 · 960 642/642 (kart yığını) · 375 349/349; eylem sütunu sağ kenarı 1415 < 1440; satır 61 px (önce 170); satırda tehlike düğmesi 0 |
\* K9: sağ çekmece açıkken arkasındaki sayfa ("+ Yeni bildirim", "SÜRESİ YAKLAŞAN") çekmece içeriğiyle kesişiyor sayılıyor; katman örtmesi, binme değil (tur 1'deki aynı not; çekmecesiz Saklama ölçümünde K9 = 0).
\** K8: "karaduman-yildirimoglu", fikstür kullanıcısının e-posta adresinin bir parçası (veri; jargon değil). "E-POSTA" sütun başlığı kalktığı için önceki K8 kaynağı yok oldu.
Read ile GÖRÜLEN görüntüler: sip-detay__BASIT_aydinlik_1440, sip-detay__PRO_karanlik_375, sip-yeni__BASIT_aydinlik_1440-tam, sip-dolu__BASIT_aydinlik_1440-tam, sip-dolu__BASIT_aydinlik_1280, uyum-goruntule__BASIT_aydinlik_1440, uyum-saklama__BASIT_aydinlik_1440-tam, kullanicilar__PRO_aydinlik_1440, kullanicilar__BASIT_karanlik_960 (yığın düzeltmesinden önce ve sonra).
Ara bulgu ve düzeltmesi: ilk ölçümde /kullanicilar 1440 tablo 1141/1122 (19 px taşma), 960 1023/642 ve K9 5–6 çıktı. E-posta ikinci satıra alındı, ≤64rem yığın eklendi. Yığında satır 445 px idi (olcek.css `height: 61px` hücre kuralı); `height:auto`, `border:0` ve ad kabı ile 253 px'e indi. Sonraki ölçüm yukarıdaki tablodur.

### 12.7 Temizlik
API (5162) ve vekil (5163) kill ile durduruldu; 5162/5163 dinleyen yok. `zsh kiraci-temizle.sh 01M3V77WC3220JSMFR4K2GD179 --uygula` çalıştırıldı. tenants 14 (başlangıç) → 15 → **14**. Kiracının kullanicilar/siparisler satırları 0. QA-TEST-* ve DEMO kiracılarına dokunulmadı. Tur başındaki yarım tam süit koşumu (kod değişeceği için) kendi PID'leriyle durduruldu, son kodla yeniden koşuldu.

### 12.8 Bu turda değişen dosyalar
Kuyumcu.Web/Pages/SiparisDetay.razor, Siparisler.razor, Uyum.razor, Kullanicilar.razor · Kuyumcu.Web/wwwroot/css/zo-islem.css · Kuyumcu.Core/I18n/Sozlukler.KesifP6.cs · Testler: KesifP6EkranTestleri.cs, SiparisEkranTestleri.cs, OrtakGorunumHakemTestleri.cs, Q93SiparisBaslikEkranTestleri.cs, OrtakGorunumKiyasTestleri.cs. Migration yok.

### 12.9 Koşul karşılama (hakem bulguları)
| Koşul | Durum | Kanıt |
|---|---|---|
| sipd-* düğmeleri btn ailesi, tek birincil, "Onayla" | KARŞILANDI | bUnit + 16 ölçüm (gri 0, birincil 1) |
| Düzenle başlık satırında, tek düğmelik kart yok | KARŞILANDI | `#sipd-kart > .kart-baslik #sipd-duzenle`; görüntü |
| K17 açıklama satırları 0 (liste + detay + append-only) | KARŞILANDI | HK8, Siparis_detay_aciklama…; ölçüm |
| Teklif sebep metni koşulla eşleşir | KARŞILANDI | bUnit + ölçüm "Fiyat için listeden ürün seçin." |
| Uyum tanım listesi biçimli, uyruk ülke adı | KARŞILANDI | bUnit + ölçüm (grid, renkler, "Türkiye") |
| Kullanıcılar 1440 ve 960'ta sW ≤ cW, eylemler ⋯ menüsünde | KARŞILANDI | ölçüm 1122/1122 · 642/642; bUnit |
| K16 isteğe bağlı alanlar "Daha fazla"da, K15 geçmiş kapalı | KARŞILANDI | bUnit + ölçüm |
| Build 0 hata · denetim (bu paketin sayaçları) · mutasyon · canlı · kiracı silindi · yeni kırmızı 0 | KARŞILANDI | § 12.2–12.7 |

### 12.10 Açık kalanlar (sahibiyle)
- Tam süit başkasına ait 5 kırmızı: Fatura fişten-fatura mesaj testleri ×2 ve Toplu1FaturaTestleri.T2 ×3 → FaturaModulu.cs/Faturalar.razor sahibi (TOPLU-1 / YASAL-4).
- Denetim: @onclick'siz düğme +1 (Faturalar.razor), P-08 (HurdaModulu YASAL-4), CARI_BAKIYE_AYNA canlı +1 (başka kiracı verisi), P-30 (kurucu).
- `Sozlukler.cs` HUB'daki `Siparis.DefterEtkisiYok`, `Siparis.AppendOnly`, `Siparis.DetayEkranAciklama` ve OrtakGorunum `SiparisDefterNotuBasit` artık ekranda kullanılmıyor. Silmek ya da Yardım'a taşımak P8'in (HUB) kararı; KesifSozlukTaramaTestleri bu anahtarları hâlâ sınıyor.
- Serbest tanım + ayar + gramdan fiyat hesaplamak yeni bir sunucu fiyat kuralıdır, ayrı paket önerisi. Bu turda yalnız sebep metni düzeltildi.
- Doğrulayıcı dosyası `Dogrulayici/SUBEKAPSAMASinirTestleri.cs` CS8601 uyarısı (5 kez) sahibine.
