# PAKET ISLEM-BANT-2: uygulama raporu (10.10.2026, t5, canlı 5166, vekil 5167, CDP 9501–9506)

**Konu:** ISLEM-BANT'ın kapanış devri (tasarım hakemi KALDI). KRİTİK: eksi/0 has fiyatında alan altında ileti yoktu ve ekranda "-22,90 TL" görünüyordu. YÜKSEK: hurda alışı TEZGAHTAR'a açıktı, Kaydet etkindi, sunucu 403'ü sonda söylüyordu. YÜKSEK: bant dışında Hurda'da Kaydet etkindi (Takas'la tutarsız). ORTA: etiket "Has Gram Fiyatı (₺)" ile "Has fiyatı (1 gram)" farklıydı. DÜŞÜK: `GET /kullanicilar/ben` TEZGAHTAR'a 403 veriyordu.
**Rol:** Taşkın (backend) + Ertuğ (frontend). **COMMIT YOK.** Değişiklikler çalışma ağacında.
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı (§ 10).

## 1. Spesifikasyon ile kodun karşılaştırılması, sapmalar

| Madde | Kodda bulunan | Karar ve gerekçe |
|---|---|---|
| "Ortak Para/Tutar bileşeni negatif ve sıfırı reddeder (GIRDI-AILESI-2 kalıbı)" | `Para.razor` zaten `Tur` parametresiyle (`TutarTuru.Pozitif`) bu kuralı taşıyor (Core `TutarKurali`, alan altı sözlük iletisi, `-` tuşu maskede kapalı). Hurda, takas, eski altın ve ramat fiyat kutularına verilmemişti. | **Ortak bileşen değişmedi** (geriye uyumluluk riski yok). Kural dört kutuya bağlandı: `hurda-fiyat`, `takas-eski-fiyat`, `altin-fiyat`, `hrd-fiyat`. İşçilik (`iscilik-tutar`) zaten `Pozitif`ti. İskonto `Yuzde` bileşeni, kapsam dışı. |
| Gerçek Chrome'da "-5" | Maske `-` tuşunu yazdırmıyor. Kutuda "5" kalıyor, bu da mutlak sınır dışı (has 500–50.000). | Kutunun altındaki tutar satırı **mutlak dışında da** çizilmiyor (`IslemBantGosterim.TutarGosterilir`). Kapsam: hurda alış hesap satırı, Değişim ALDIM tutarı ve eski altın "Fişe sayılan". Ölçülen sonuç: "-5" yazılınca alan altında sınır cümlesi var, tutar "—", Kaydet kilitli. |
| Tutarın modelde de gizlenmesi (ilk deneme) | `TakasGirisi.Fark`, `HurdaGirisi.HesaplananTutar` ve `AltinBacagiGirisi.Deger` kuruş ve büyük sayı testlerinin (SATIS3 Tur4/6/7/8/10) sınadığı sunucu formülü. | **Geri alındı.** Model formülü değişmedi, gizleme yalnız gösterimde. Fark kartı ve tuval farkı yalnız 0/eksi fiyatta "—" (`EskiFiyatHesabaGirer`). |
| Ramat satışı (`/hurda`) mutlak dışı tutar | `#hrd-tutar` mutlak dışı fiyatı Q102/ZOSH kuruş ve büyük sayı biçim testlerinin konusu. Bu ekranda fiyat kurdan geliyor, kutu "+ Detay ekle" arkasında. | Bu satırda yalnız 0/eksi fiyat "—" oluyor. Mutlak dışı fiyatta tutar kutudaki değerle tutarlı görünüyor. Kaydet sınır cümlesiyle **kilitli** (ölçüldü). Açık kalan § 11/2. |
| "Hurda alış ekranı TEZGAHTAR'a görünmez **ya da** görünürse Kaydet öncesi yetki cümlesi + kilit" | Sekme listesi BASİT ızgarasıyla (`BasitPos.Turler`) paylaşılıyor. Gizlemek tezgâh düzenini ve YILDIZ şeridinin dosyalarını etkilerdi. | **Görünür + kilit** seçildi. Formun başında `#hurda-yetki-yok` bandı var. Kaydet aynı cümleyle kilitli. Rol kümesi tek kaynakta: Core `IslemRolleri.HurdaAlis`. Sunucunun `[RolGerekli]` özniteliği derleme sabiti istediği için eşitlik bir sözleşme testiyle kilitlendi (IB2_05, mutasyon MIB2-8). |
| "Bant dışında Hurda'da da Kaydet kilitli (Takas ile aynı)" | Kodda bant dışı fiyat **hiçbir** ekranda Kaydet'i kilitlemiyordu. Hakemin "Takas'ta pasif" gözlemi başka eksik alanlardan geliyordu. ISLEM-BANT yalnız alan altı ileti ve sunucu 400'ünü veriyordu. | Dört ekranda **aynı kural** var (`IslemBantGosterim.KaydetKilidi`). Sıra: mutlak dışı → alanın cümlesi; oran dışı ve yetkisiz rol → yetki cümlesi; onay yok → "Fiyatı düzeltin ya da …işaretleyip gerekçe yazın"; gerekçe 20 karakterden kısa → K30 cümlesi. Bağlandığı yerler: Islemler (hurda alış, takas; `Islemler.IslemBant2.cs`), `Hurda.razor` (`Kilit`), eski altın (`AltinBacagiPaneli` eksik listesi). |
| Etiket tek ad | `Dil.T.Islem.HasGramFiyati` = "Has Gram Fiyatı (₺)", BASİT Değişim'de `DG.EskiHasFiyati` = "Has fiyatı (1 gram)". Ramatta "Birim fiyat (TL / has gram)" kullanılıyordu. | Tek ad "Has fiyatı (1 gram)" / "Fine gold price (1 gram)". Title Case ve "(₺)" kalktı (birim kutunun sağında yazıyor). Dört ekran aynı anahtarı okuyor (`Dil.T.Islem.HasGramFiyati`). |
| `GET /kullanicilar/ben` | Sınıf düzeyindeki `[ModulYetkisi("kullanici")]` TEZGAHTAR ve MUHASEBE'ye 403 veriyordu. | Uç `KendiProfilController`'a taşındı (`[HerkeseAcik(gerekçe)]`). Hedef jetondan geliyor, yol parametresi yok. `/kullanicilar/{id}` hâlâ SAHİP/MÜDÜR (canlıda TEZGAHTAR 403). |

**Mevzuat:** Yasal hüküm, eşik ya da oran yazılmadı. İş kuralı (yazım hatası kapısı) ve rol kümesi sunucudaki mevcut değerlerin aynısı. Birincil kaynak gerekmedi.

## 2. Yapılanlar
**Core**
- `Domain/IslemRolleri.cs` (YENİ): `HurdaAlis = [SAHIP, MUDUR, MUHASEBE]`, `Icinde(kume, rol)`.
- `I18n/Sozlukler.IslemBant2.cs` (YENİ): `HurdaAlisYetkiYok`, `BantOnayBekliyor` TR+EN. Rol adları `KesifP1Sozlugu.RolListesi`'nden geliyor (ham kod basılmıyor).
- `I18n/Sozlukler.cs`: `HasGramFiyati` TR "Has fiyatı (1 gram)" / EN "Fine gold price (1 gram)".

**API**
- `Modules/Kullanici/KullaniciModulu.cs`: `GET /kullanicilar/ben` artık `KendiProfilController` altında, her rol 200 ve yalnız kendi satırı (`Temizle` biçimi, parola özeti yok).

**Ekran**
- `Bilesenler/Fis/IslemBantGosterim.cs`: `Rol`, `OnayYetkili`, `HurdaAlisKilitli`, `FiyatHesabaGirer` (0/eksi), `TutarGosterilir` (0/eksi/mutlak), `KaydetKilidi`.
- `IslemBantOnay.razor`: yetki yüklemi tek yerden okunuyor (`IslemBantGosterim.OnayYetkili`).
- `FisHurdaAlis.razor`: `#hurda-yetki-yok` bandı (yeni id), `hurda-fiyat` için `Tur=Pozitif`, hesap satırı tutarı `TutarGosterilir` ile.
- `FisTakas.razor`: `takas-eski-fiyat` için `Tur=Pozitif`, iki modda tek etiket, PRO fark kartı 0/eksi fiyatta çizilmiyor.
- `FisTakasTuval.razor`: `#takas-aldim-tutar` ve fark metni geçersiz fiyatta "—".
- `FisModelleri.cs`: yalnız `TakasGirisi.EskiFiyatHesabaGirer` eklendi (formüller aynı).
- `AltinBacagiPaneli.razor`: `altin-fiyat` için `Tur=Pozitif`, bant kilidi eksik listesinin başında, has/TL satırı `HesapGorunur` ile.
- `Pages/Islemler.IslemBant2.cs` (YENİ, partial): `IslemBant2Kilidi`. `Pages/Islemler.razor`'da **mevcut iki satıra** birer koşul eklendi (`KaydetKilitSebebi` ve `Gonderilebilir`). Satır sayısı 1.397 değişmedi.
- `Pages/Hurda.razor`: `hrd-fiyat` için `Tur=Pozitif`, tek etiket, `Kilit` zincirine "fiyat boş/0" ve bant kilidi, `#hrd-tutar` 0/eksi fiyatta "—".

**Migration:** gerekmedi (şema değişmedi).

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Yeni varlık | YOK | — |
| `kullanicilar` (kendi satırı) | Okuma `GET /kullanicilar/ben` (her rol). Yazma: ISLEM-BANT'ın `PATCH /kullanicilar/ben` beyaz listesi değişmedi. | Başkasının satırına yol yok (canlı 403). |
| Bant onayı `bantOnay/bantGerekce` | İstek alanı, kalıcı kolon değil | Denetim izi ISLEM-BANT'taki gibi (değişmedi). |

Düğmeler: yeni düğme yok. Kilitli Kaydet her durumda sebebini `title` ve kilit satırında yazıyor (UX-S30). Yeni ModalPencere yok (denetim "48 ≤ 55").
**Dokunulan sayfalar:** `Pages/Islemler.razor` (2 mevcut satır) ve `Pages/Hurda.razor`. İkinci dokunuşun gerekçesi: ISLEM-BANT kapanış devri bu iki sayfayı bu paket için açtı (hakem bulguları bu sayfalarda). YASAL-KAPI-2 ile çakışma görülmedi: yalnız fiyat kutusu, `Kilit` zinciri ve tutar satırı.

## 4. Testler
**Yeni (40 koşum):** `Kuyumcu.Tests/IslemBant2Testleri.cs` ve `IslemBant2EkranTestleri.cs` (SATIS3 düzeneğine partial).
- Saf (17): IB2_01 kilit sırası (mutlak / yetki / onay / gerekçe, TR+EN). IB2_02 tutar kuralı (-5, 0, 0,001, 99.999.999, 2.000, 6.800; altın ve gümüş). IB2_03 takas modeli. IB2_04 rol kümesi (TEZGAHTAR kilitli; SAHİP/MÜDÜR/MUHASEBE ve oturumsuz açık). IB2_05 `[RolGerekli]` ile `IslemRolleri` birebir. IB2_06 yetki cümlesi TR/EN, ham kod yok. IB2_07 tek etiket, Title Case yok.
- bUnit bileşen (7): IB2_10 hurda -5/0 (ileti, `Fiyat=null`, tutar "—", `data-eksi` yok). IB2_11 Değişim tuvali -5/0/5 (ileti, ALDIM "—", "-22,90" yok, etiket). IB2_12 eski altın (TEZGAHTAR oran dışı → ilk eksik yetki cümlesi; -5 → işaretli, hesap yok). IB2_13 hurda TEZGAHTAR bandı, MUHASEBE görmez.
- HTTP gerçek MySQL (5): IB2_20 `GET /ben` SAHİP/MÜDÜR/MUHASEBE/TEZGAHTAR 200, id jetondaki kişi, parola özeti yok, TEZGAHTAR/MUHASEBE `/kullanicilar/{sahip}` 403. IB2_21 anonim 401.
- bUnit sayfa (11): IB2_30 TEZGAHTAR hurda BASİT+PRO (bant, Kaydet kilitli, aynı cümle, POST 0). IB2_31 MUHASEBE açık, POST 1. IB2_32 MÜDÜR 2.000 (onay bekliyor → kısa gerekçe kilitli → onay ve gerekçe ile POST, `bantOnay:true`). IB2_33 hasım -5/0/5/99999999/abc (aria-invalid, tutar "—", kilit, POST 0). IB2_34 takas TEZGAHTAR 3.000 → yetki cümlesi, -5 → alan altı ileti.

**Kasıtlı davranış değişikliğiyle güncellenen eski testler.** Yeni kural: bant dışı fiyatta Kaydet kilitli. Testlerin amacı korunarak fiyatlar banda alındı ya da onay eklendi (önceki hâlleri `/private/tmp/kuyumcu-serit/ISLEM-BANT-2/test-oncesi/`):
- `SATIS3EkranTestleri.cs`: `TakasDolu` "100" → "3500" ve "9000" → "7000" (had ilişkisi korundu). T16 belirsiz fiyat "1.500/1500" → "4.500/4500". K2 eski altın "1000" → "4000".
- `Dogrulayici/SATIS3SinirTestleri.cs`: T15S eksi fark "1 g × 50.000" yerine "8 g × 6.800" (−39.830,40; aynı had ilişkisi). T15S çok büyük fiyat: mutlak dışı olduğu için artık "KART'la bile kilitli, POST yok" bekleniyor (sunucu zaten 400).
- `B4AltinBacagiEkranTestleri.cs`: fark kilidi testi `hasFiyati: null` (yalnız mutlak). `B4AltinBacagiFisEkranTestleri.cs` ve `S24EskiAltinEkranTestleri.cs`: sahte kur 6.800 yerine 2.000 (testlerin 2.000 fiyatı ve net 9.160'ı korunur).
- `DegisimGunSonuTestleri.cs`: `Doldur` eksi farkı 1.000'in katıysa gramdan kuruyor (has 1.000 bant içinde kalıyor).
- `Dogrulayici/ZOISLEMASinirTestleri.cs`: ZOSH çok büyük kur ve ZOS11 çok büyük fiyat (mutlak dışı) için biçim iddiası aynen, artık "Kaydet kilitli + sınır cümlesi" bekleniyor. ZOS11 45.000 ve ZOSR18 5.000 (ramat bandı dışı) testlerine MÜDÜR/SAHİP onayı ve gerekçe eklendi.

**Filtreli koşumlar:** IB2 39/39 → 40/40. İlgili geniş filtre (IslemBant, SATIS3, Hurda, Takas, AltinBacagi, B4, Değişim, Kullanıcı, TS1/2, S22, P20, ParaDemo, Yıldız, ZOISLEMA, KurBant…): ilk koşumda 2.036'da 34 kırmızı (§ 1'deki model gizleme denemesi ve bant kilidi). Düzeltmelerden sonra 2.329'da 2 kırmızı: RD_KH02 tarih kapısı (başkasının) ve `Barkod_dizisi_ve_Enter…` (tek başına yeşil, zamanlama). Q102/ZOISLEMA/Hurda filtresinde (543) son durum 0 kırmızı.

## 5. Mutasyon kanıtı
Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ISLEM-BANT-2/`, betik `ISLEM-BANT-2/mut/mut.sh`. Her mutasyon tek eşleşmeyle `perl -0777 -pi` (`\Q…\E`), ardından derleme, filtre (`IB2_|IslemBant`, 107 test), yedekten geri kopyalama ve `cmp`. MIB2-6 ve MIB2-7'nin ilk denemesi başka şeridin o anki derleme kırığına düştü (`Yildiz4CTestleri.cs(424) YildizMusteriBaglami.DigerSuzgec`, 2 hata). O koşumlar sayılmadı; ağaç yeşile dönünce ikisi yeniden koşuldu. Son mutasyondan sonra `--no-incremental` temiz derleme: 0 hata.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| MIB2-1 | `HurdaAlisKilitli`: `!string.IsNullOrEmpty(rol) &&` yerine `false &&` | IB2_04 (TEZGAHTAR), IB2_13, IB2_30 ×2 (4) | AYNI |
| MIB2-2 | `KaydetKilidi`: onay yok satırı `if (false)` | IB2_01, IB2_32 ×2 (3) | AYNI |
| MIB2-3 | `Islemler.razor` `Gonderilebilir`: `IslemBant2Kilidi is null &&` silindi | IB2_30 ×2, IB2_32 ×2, IB2_33 (5, 99999999) (6) | AYNI |
| MIB2-4 | `FisTakas.razor`: `takas-eski-fiyat`'tan `Tur=Pozitif` silindi | IB2_11 (-5, 0), IB2_34 (3) | AYNI |
| MIB2-5 | `TutarGosterilir`: mutlak koşulu silindi (`fiyat is > 0m;`) | IB2_02 (0,001; 99.999.999), IB2_11 (5), IB2_33 (5; 99999999) (5) | AYNI |
| MIB2-6 | `KendiProfilController`: `[HttpGet]` yerine `[HttpGet("profil-yok")]` | IB2_20 ×4 (SAHİP, MÜDÜR, MUHASEBE, TEZGAHTAR) | AYNI |
| MIB2-7 | `AltinBacagiPaneli`: bant kilidi eksik listesinden çıkarıldı | IB2_12 | AYNI |
| MIB2-8 | `IslemRolleri.HurdaAlis`'ten MUHASEBE çıkarıldı | IB2_04 (MUHASEBE), IB2_05, IB2_13, IB2_31 (4) | AYNI |

8/8 mutasyon en az bir adı yazılı testi kırmızıya çevirdi, hepsi geri alındı ve `cmp` ile birebir aynı.

## 6. Derleme, denetim, tam süit
- **`dotnet build Kuyumcu.sln --no-incremental`** (`build-son.log`): **0 hata, 6 uyarı.** Altısı da başkasının dosyasında (SUBEKAPSAMA 49, KesifP2 455, SonKalanlar 593/824, Yildiz2 194/234). Bu paketin dosyalarında uyarı 0.
- Ara not (Anayasa md.1): artımlı test derlemesi bir kez eski dll ile koştu (kaynakta olmayan iddia kırmızı göründü). `--no-incremental` ile giderildi, bundan sonra kritik koşumlar temiz derlemeyle yapıldı.
- **`bash dotnet/denetim.sh`** (`denetim.log`): bu paketin sayaçları ✓. ModalPencere 48 ≤ 55, P-10/P-16 DTO_FORM_ESLESMEZ 0, P-18 SABIT_RENK 0, S2-3 SOZLUK_SAYI 0, B-3 ESIK_ISTEMCIDE 4 ≤ 4, HerkeseAcik gerekçeli. ✗ olan 2 madde başkasına ait:
  - `P-30 DEMO_INSAN_SURESI`: tarih kapısı, görevde hariç.
  - `GIRDI-2 TEST_GECERSIZ_FIKSTUR = 1`: `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN, MASAK-ESZAMAN şeridi (ISLEM-BANT devrinde de vardı).
- **Tam süit, son ürün koduyla BİR kez** (`trx-pISLEM-BANT-2/_Mac_2026-10-10_16_59_47.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t5): **toplam 12.193, çalışan 12.190, geçen 12.185, başarısız 5, atlanan 3.**
  - `S24EskiAltinEkranTestleri` ×2 **bu paketindi** (sahte kur 6.800, eski altın 2.000 = bant dışı). Test düzeneği kur 2.000'e alındı (§ 4). Filtreli yeniden koşum **12/12 yeşil** (`test15.log`). Ürün kodu değişmediği için tam süit yeniden koşulmadı (tur başına bir kez kuralı).
  - `RaporDortTestleri.RD_KH01` ve `RD_KH02`: "Rapor dönemi en çok 400 gün olabilir." Tarihe bağlı, RAPOR-4 dosyası. Bu paketin dokunduğu kodla ilgisi yok; ISLEM-BANT filtrelerinde de kırmızı.
  - `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12…`: 11 eşzamanlı istekten biri 409 yerine 500. Aynı test tek başına **yeşil** (`test15.log`), eşzamanlılık kararsızlığı. Bu paket pusula/Yasal4 koduna dokunmadı.
  - **Bu paketin yeni kırmızısı: 0** (S24 düzeltildi ve doğrulandı).

## 7. Canlı doğrulama (5166, Development, PID ile başlatılıp durduruldu)
Kiracı `PAKETISLEM-BANT-2-TEST …` (id 01M4JWSVXV898JSJQ8SBQMV10D). Kullanıcılar SAHİP/MÜDÜR/MUHASEBE/TEZGAHTAR. Güncel has 6.658, alış bandı 3.329,00–7.323,80. Yanıtların hepsi okundu (`canli/canli-sonuc.json`).

| Adım | Beklenen | Gerçek | Alan · ileti |
|---|---|---|---|
| GET /kullanicilar/ben SAHİP · MÜDÜR · MUHASEBE · TEZGAHTAR | 200 ×4 | 200 ×4 | id = jetondaki kişi ×4, `sifreHash` yok ×4 |
| GET /kullanicilar/{sahipId} TEZGAHTAR (yetki genişlemedi) | 403 | 403 | "Bu ekran için yetkiniz yok (kullanici)" |
| POST hurda-alis TEZGAHTAR 6.000 (sunucu kapısı aynı) | 403 | 403 | "gerekli rol: dükkân sahibi ya da müdür ya da muhasebe" (ekrandaki cümleyle aynı rol listesi) |
| POST hurda-alis SAHİP -5 / 0 | 400 / 400 | 400 / 400 | birimFiyatTlHasGram · Range / "0 TL olamaz" |
| POST hurda-alis SAHİP 0,001 / 5 / 99.999.999 | 400 ×3 | 400 ×3 | birimFiyatTlHasGram · "(500,00–50.000,00 TL)" |
| POST hurda-alis SAHİP 2.000 onaysız | 400 | 400 | "(3.329,00–7.323,80 TL)" |
| POST hurda-alis SAHİP 6.000 · MUHASEBE 6.000 | 200 · 200 | 200 · 200 | — |

SQL (`canli/sql-kanit.txt`): `hareketler` HURDA_ALIS **2** (SAHİP 1, MUHASEBE 1), ALIS 3 (fikstür). Reddedilen 8 deneme satır yazmadı. TEZGAHTAR'ın 403'ü de satır yazmadı. Chrome turunda Kaydet hiçbir durumda açılmadığı için tarayıcıdan da kayıt düşmedi. `denetim_kayitlari` IslemFiyatBantAsimi 0 (onaylı aşım denenmedi; o yol ISLEM-BANT'ta kanıtlı).

**Temizlik:** API (`dotnet run` PID 74695 ve dinleyen 74718) ile vekil (88672) `kill` ile durduruldu. 5166 ve 5167'de dinleyen 0. Bu paketin Chrome süreçleri (9501–9506) kapalı. Başka şeritlerin 9508/9510 Chrome'larına dokunulmadı. `zsh kiraci-temizle.sh 01M4JWSVXV898JSJQ8SBQMV10D --uygula` çalıştı. İkinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)" dedi. Kendi davet satırım silindi (1 satır). **tenants: başlangıç 21 → son 22.** `PAKETISLEM-BANT-2%` adlı kiracı 0. Fark eşzamanlı şeritlerin test kiracısı (PAKETYILDIZ4D-TEST, PAKETYILDIZTIPO-TEST vb. temizlik listesinde). QA-TEST- kiracılarına dokunulmadı. Parola içeren dosyalar (`canli/hesap.json`, `chrome/{tezgahtar,sahip,aktif}.json`) `printf ''` ile boşaltıldı. Jeton ya da parola çıktıya düşmedi.

## 8. Gerçek başsız Chrome (KABUL: 1280 + 375 · BASİT + PRO · açık + koyu)
Araç `ISLEM-BANT-2/chrome/olc.mjs` (cdp.mjs + `sikisma-olc.js`). Oturum vekille parolasız açıldı. Mod üst çubuk düğmesiyle seçilip `aria-pressed` ile doğrulandı. Tema `kuyumcuTema.uygula` ile değişti. Girdiler `Input.insertText` ile verildi, yani tarayıcının maskesi gerçekten çalıştı. **104 ölçüm** (TEZGAHTAR 32, SAHİP 72), ham veri `chrome/olcum-*.jsonl`, görüntüler `chrome/foto/`.

Durumlar:
- **takas** (TEZGAHTAR, /islemler?tur=TAKAS, 5 g): -5, 0, 3.000.
- **hurda** (TEZGAHTAR: yetki · SAHİP: -5, 0, 2.000).
- **ramat** (SAHİP, /hurda, 1 has): -5, 0, 9.000.
- **altin** (SAHİP, satış fişi + "Burma Bilezik" 30.000 + ALTIN bacağı 22 ayar 5 g): -5, 0, 2.000.

| rol · ekran | mod | tema | 1280×800 | 375×812 |
|---|---|---|---|---|
| TEZGAHTAR · takas | BASIT | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · hurda | BASIT | aydinlik | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · takas | BASIT | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · hurda | BASIT | karanlik | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · takas | PRO | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · hurda | PRO | aydinlik | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · takas | PRO | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| TEZGAHTAR · hurda | PRO | karanlik | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 1 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · hurda | BASIT | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · ramat | BASIT | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · altin | BASIT | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · hurda | BASIT | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · ramat | BASIT | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · altin | BASIT | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · hurda | PRO | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · ramat | PRO | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · altin | PRO | aydinlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · hurda | PRO | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · ramat | PRO | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |
| SAHIP · altin | PRO | karanlik | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi | 3 durum · K1 0 · K3 0 · K5 0 · K9 0 · Y 0 · Kaydet kilitli hepsi |

Her durumda ölçülen sonuç (24 ekran × mod × tema × genişlik birleşimi, 104 satır):

| Durum | Kutu | Alan altı | Kutu altı tutar | Kaydet | Sebep |
|---|---|---|---|---|---|
| takas / hurda / altin / ramat "-5" | "5" (maske eksiyi yazdırmaz) | "Bu fiyat olağan aralığın dışında (500,00–50.000,00 TL)…" | "—" (takas ALDIM, hurda hesap, eski altın has/TL). Ramat "Tutar: 5 ₺" (§ 1, § 11/2) | kilitli ×32 | alanın sınır cümlesi |
| "0" (dört ekran) | "0" | "Değer 0'dan büyük olmalı." | "—" ×32 | kilitli ×32 | "Has gram fiyatını düzeltin…" / eski altında «Has fiyatı (1 gram)» kutusu |
| takas 3.000 TEZGAHTAR | 3.000 | bant cümlesi | 13.740 ₺ (onaylanabilir aralık) | kilitli ×8 | "Fiyat doğruysa bunu yalnız dükkân sahibi ya da müdür onaylayabilir." |
| hurda alış TEZGAHTAR | — | — | — | kilitli ×8 | formun başında ve düğmede aynı cümle: "Eski altın alışını yalnız dükkân sahibi ya da müdür ya da muhasebe kaydedebilir…" |
| hurda 2.000 · ramat 9.000 · altın 2.000 SAHİP | — | bant cümlesi | var | kilitli ×24 | "Fiyatı düzeltin ya da "Fiyat doğru, yine de kaydet" kutusunu işaretleyip gerekçe yazın." (ramatta bu sefer bant dışında da kilitli) |

**104 ölçümün 104'ünde** K1 (gizli-görünen), K3 (taşan/kırık), K5 ('TRY'), K9 (binen) ve yatay kaydırma **0**. Kaydet 104'te kilitli ve sebepli. Canlıda 403 hiç tetiklenmedi (TEZGAHTAR için Kaydet tıklanamıyor).

Görülen görüntüler (Read 4, en riskli birleşimler):
- `takas_f-5_TEZGAHTAR_BASIT_karanlik_375.png` (hakem KRİTİK'inin karesi): "Has fiyatı (1 gram)" kırmızı çerçeveli, kutuda 5, altında sınır cümlesi ve "Elle girildi". ALDIM satırı "4,5800 has — —", Fark "—". "-22,90" yok, taşma yok.
- `hurda_yetki_TEZGAHTAR_PRO_aydinlik_1280.png`: formun başında turuncu uyarı bandı (yetki cümlesi), "Hurda Alış Kaydet" soluk ve kilitli, altında aynı cümle. Etiket "Has fiyatı (1 gram)".
- `hurda_f2000_SAHIP_BASIT_aydinlik_375.png`: bant cümlesi "(3.329,00–7.323,80 TL)", "Fiyat doğru, yine de kaydet" onay kutusu, tutar satırı tek satır, taşma yok.
- `altin_f-5_SAHIP_PRO_karanlik_1280.png`: eski altın panelinde kutuda 5, sınır cümlesi, "Has karşılığı —" ve "Fişe sayılan tutar —". Ödeme satırları bozulmadan.

## 9. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (bazı dizinler izlenmediği için dosya adıyla):
- **Core:** `Kuyumcu.Core/Domain/IslemRolleri.cs` (YENİ), `Kuyumcu.Core/I18n/Sozlukler.IslemBant2.cs` (YENİ), `Kuyumcu.Core/I18n/Sozlukler.cs` (2 değer).
- **API:** `Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs`.
- **Web:** `Bilesenler/Fis/IslemBantGosterim.cs`, `IslemBantOnay.razor`, `FisHurdaAlis.razor`, `FisTakas.razor`, `FisTakasTuval.razor`, `FisModelleri.cs`, `Bilesenler/AltinBacagiPaneli.razor`, `Pages/Islemler.razor` (2 satır içi), `Pages/Islemler.IslemBant2.cs` (YENİ), `Pages/Hurda.razor`. (`Servisler/AltinBacagiGirisi.cs`'e bir ara deneme yapıldı ve tamamen geri alındı; net değişiklik yok.)
- **Tests:** `IslemBant2Testleri.cs` (YENİ), `IslemBant2EkranTestleri.cs` (YENİ). Güncellenenler: `SATIS3EkranTestleri.cs`, `Dogrulayici/SATIS3SinirTestleri.cs`, `B4AltinBacagiEkranTestleri.cs`, `B4AltinBacagiFisEkranTestleri.cs`, `S24EskiAltinEkranTestleri.cs`, `DegisimGunSonuTestleri.cs`, `Dogrulayici/ZOISLEMASinirTestleri.cs`.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK: -5/0 → alan altı ileti, tutar yok, Kaydet kilitli (hurda/takas/eski altın, BASİT+PRO, 1280+375) | KARŞILANDI | Chrome § 8 (ramatta mutlak "5" tutarı § 11/2); IB2_10/11/12/33; MIB2-4/5 |
| SINIF: işlem içi fiyat/tutar kutuları aynı kural | KARŞILANDI (iskonto `Yuzde`, kapsam dışı) | `Tur=Pozitif` 4 kutu, işçilik zaten Pozitif |
| YÜKSEK: TEZGAHTAR hurda alış kilitli + sebep, rol kümesi tek kaynak, canlı 403 hiç tetiklenmez | KARŞILANDI | `IslemRolleri` + IB2_05 sözleşmesi; IB2_30; Chrome 8/8 kilitli; MIB2-1/8 |
| YÜKSEK: bant dışı Hurda'da Kaydet kilitli + sebep (Takas ile aynı) | KARŞILANDI | `KaydetKilidi` 4 ekranda; Chrome ramat 9.000 ×8 kilitli; IB2_32; MIB2-2/3/7 |
| ORTA: etiket tek ad (sözlük), Title Case yok | KARŞILANDI | IB2_07; Chrome görüntüleri |
| DÜŞÜK: GET /kullanicilar/ben her rol 200, yalnız kendi kaydı | KARŞILANDI | IB2_20; canlı ×4; MIB2-6 |
| İş kuralları hasımı: -5, 0, 0,001, harf, 99.999.999, TEZGAHTAR hurda alış | KARŞILANDI | IB2_02/33, canlı 400'ler, TEZGAHTAR 403 (sunucu) + ekran kilidi |
| Mutasyon ≥ 4 kırmızı, cmp AYNI | KARŞILANDI | 8/8 (§ 5) |
| build 0/0 (bu paketin dosyaları), denetim TEMİZ (P-30 hariç) | Paket kapısı KARŞILANDI | 0 hata; 6 uyarı başkasının; denetimde ✗ GIRDI-2 MASAK-ESZAMAN'ın (§ 6) |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 5 kırmızıdan 2'si bu paketin (S24, düzeltildi, 12/12); 3'ü başkasının ya da kararsız (§ 6) |
| Canlı API + SQL | KARŞILANDI | § 7 |
| Test kiracısı silindi | KARŞILANDI | § 7 |
| COMMIT yok, dosya silme yok, sır basılmadı | KARŞILANDI | — |

## 11. Açık kalanlar
1. **Başkasına ait kırmızılar / ✗:** `RaporDortTestleri.RD_KH01/RD_KH02` (tarih: "400 gün", RAPOR-4 sahibi). `Yasal4HttpTestleri.Y4A_tur4_eszamanli_12…` (kararsız, tek başına yeşil; YASAL-4 sahibi). Denetimde `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK-ESZAMAN) ve P-30 (tarih kapısı).
2. **Ramat satışı (`/hurda`) mutlak dışı fiyatta tutar satırı görünüyor** ("-5" maskeden "5" olarak geçince "Tutar: 5 ₺"). Kaydet sınır cümlesiyle kilitli. Gizlemek Q102/ZOSH/ZOS11 kuruş ve büyük sayı biçim testlerinin konusunu yok ediyordu. İstenirse bu testlerin sahibiyle birlikte ayrı küçük iş olarak ele alınmalı. DÜŞÜK.
3. **Eski altın bacağında otomatik kalan** mutlak dışı fiyatı hesaba katıyor (5 g × 5 → nakit 29.977,10). Kaydet kilitli olduğu için kayda gitmiyor. Gösterim kararı ödeme şeridinin. DÜŞÜK.
4. `PATCH /kullanicilar/ben/sifre` hâlâ sınıf düzeyindeki `[ModulYetkisi("kullanici")]` arkasında, yani TEZGAHTAR kendi parolasını bu uçtan değiştiremiyor (aynı hata sınıfı, bu paketin maddesi değil). Ayrı paket önerilir. ORTA.
5. Bant onay rol kümesi (SAHİP/MÜDÜR) sunucuda `KurServisi.BantOnayYetkili`, ekranda `IslemBantGosterim.OnayYetkili`. İki yerde aynı değer, sözleşme testi yok (MIB-6 ekran tarafını kırıyor). `IslemRolleri`'ne taşınması önerilir. DÜŞÜK.
6. Ekran `Gram Ağırlık (g)` gibi başka Title Case etiketler taşıyor (bu paketin maddesi yalnız has fiyatı etiketiydi).

## 12. Kurucu / mali müşavir
Mevzuat sorusu yok. Kurucuya bilgi:
- Tezgâhtar artık eski altın alışı ekranında, daha fiyat yazmadan "Eski altın alışını yalnız dükkân sahibi, müdür ya da muhasebe kaydedebilir" uyarısını görüyor. Kaydet düğmesi kilitli.
- Has fiyatı kutusuna eksi işareti yazılamıyor. 0 yazılınca kutunun altında uyarı çıkıyor, tutar "—" oluyor.
- Fiyat olağan aralığın dışındaysa dört ekranda da (değişim, eski altın alışı, eski altınla ödeme, ramata satış) Kaydet kilitli. Dükkân sahibi ya da müdür "Fiyat doğru, yine de kaydet" kutusunu işaretleyip gerekçe yazınca açılıyor.
