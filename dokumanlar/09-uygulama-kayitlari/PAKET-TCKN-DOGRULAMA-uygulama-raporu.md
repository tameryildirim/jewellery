# PAKET TCKN-DOGRULAMA — uygulama raporu (Taşkın + Ertuğ, t9)

**Tarih:** 08.10.2026 · **Durum:** BİTTİ (paket kapısının 7 maddesi sağlandı; tam süitteki 6 kırmızı bu paketin değil — § 9) · COMMIT YOK · silinen dosya yok.

**Kurucu bulgusu:** Değişim ekranında satıcı TCKN alanına `11111111111` yazılabiliyordu, ekran hata vermiyordu.
**Sonuç:** TC kimlik no doğrulama kuralı artık tek kaynakta (`Kuyumcu.Core/Domain/KimlikNoDogrulayici.cs`). Sunucudaki her kimlik no giriş ucu ve ekrandaki her TCKN alanı bu kaynağı kullanıyor. Geçersiz numara sunucuda `400` + `alanlar.<alan>` + "TC kimlik numarası geçerli değil" alıyor ve satır yazılmıyor. Ekranda 11 hane yazılır yazılmaz alanın altında aynı metin çıkıyor, Kaydet kilitleniyor ve kilit sebebi de aynı metin.

## 1. Kaynaklar (MEVZUAT KURALI)

| Hüküm | Birincil kaynak | Durum |
|---|---|---|
| TCKN 11 basamak, son iki basamak doğrulama sayısı | NVİ — https://www.nvi.gov.tr/tokat/tc-kimlik-no-islemleri: "Son iki rakamı doğrulama sayısıdır." | Var olduğu DOĞRULANDI. NVİ formülü yayımlamıyor. |
| Formül: 10. hane = (7×(1+3+5+7+9. haneler) − (2+4+6+8. haneler)) mod 10 · 11. hane = (ilk 10 hanenin toplamı) mod 10 · 1. hane 0 olamaz | Birincil yayın yok. Kamuya açık, yerleşik formül; bu projede 2026-09'dan beri `GiderPusulasiKapisi.TcknGecerli`'de kullanılıyordu | Kurucunun açık emriyle (08.10) tüm giriş noktalarına yayıldı. **Mali müşavir / kurucu notu:** formülün resmî yayını yok (§ 11). |
| YKN (yabancı kimlik no, 99 ile başlar) | Ayrı bir formül birincil kaynakta bulunamadı | Görev hükmü gereği TCKN kuralı uygulandı (`YknMi` = TCKN kuralı + "99" öneki). |
| VKN (10 hane) kontrol hanesi | GİB'de birincil formül bulunamadı | **TEYİTSİZ:** yalnız 10 hane ve ASCII rakam kontrol ediliyor. Sağlama uygulanmadı, bugünkü davranış korundu. |

**Eski koddaki gizli hata:** `GiderPusulasiKapisi.TcknGecerli` C#'ın `%` işlecini kullanıyordu. Bu işleç negatif sayıda negatif kalan verir. `7×tek − çift < 0` olduğunda geçerli numara reddediliyordu (örnek: sentetik `19090909018`). Tek kaynakta matematiksel mod kullanılıyor. Bu durum gerileme testiyle ve M1-4 mutasyonuyla kanıtlandı.

## 2. Spesifikasyon ile kodun karşılaştırması ve sapmalar

- **"Algoritma yalnız GiderPusulasiKapisi'nde" — DOĞRU.** `KimlikNo.Dogrula` (Cari) ve `VergiKimlik` (İşletme), P-16 ve Q10-3 kararlarıyla bilinçli olarak yalnız uzunluk kontrolü yapıyordu. Bu iki sınıf artık tek kaynağa bağlı.
- **StopajModulu, VezneModulu, SatisModulu (doğrudan):** kimlik no **yazan** bir uç yok. Stopaj yalnız CSV çıktısı veriyor. Vezne'de yalnız açıklama var. Satış satıcı TCKN'sini `GiderPusulasiKapisi.Dogrula` üzerinden kontrol ediyor (hurda alış, takas, eski altınla ödeme), yani tek kaynağa dolaylı bağlı. Bu dosyalarda değişiklik GEREKMEDİ.
- **MasakKuralMotoru'na dokunulmadı** (MASAK-370 şeridine ait). Kontrol, görev sınırına uygun olarak yalnız `MasakKapisi.Dogrula` içine kimlik numarası satırları olarak eklendi.
- **Evrak borçlu kimlik no:** DTO 10–20 karakter kabul ediyor (TCKN/VKN). Kural yalnız **11 hane rakam** girildiğinde uygulanıyor. Diğer biçimlerde mevcut DTO kuralı aynen kalıyor. Gerekçe: eski kabul edilen biçimleri kırmamak, tutucu davranmak.
- **MASAK belge türü:** kural yalnız `TC_KIMLIK` belgesinde uygulanıyor. Pasaport, sürücü belgesi, mavi kart ve vergi levhası bu kurala TABİ DEĞİL (görev hükmü: "belge türüne göre").
- **Yaptırım listesi `kimlik_no`** (sistemin içe aktardığı resmî liste) kapsam dışı: kullanıcı girişi değil.
- **Kullanıcı/personel:** TCKN alanı yok (grep ile tarandı).

## 3. Tarama tablosu (uç · alan · önce · sonra)

| Uç | Alan | Önce | Sonra |
|---|---|---|---|
| POST /api/v1/cari | vergiNo (tür TCKN ya da tür boş + 11 hane) | yalnız 11 hane | + kural · 400 `alanlar.vergiNo` |
| PATCH /api/v1/cari/{id} | vergiNo | yalnız 11 hane | + kural · 400 `alanlar.vergiNo`, değer korunur |
| PATCH /api/v1/admin/tenant/kimlik | vergiNo (11 hane = şahıs işletmesi TCKN) | 10/11 hane | + kural (11 hanede) · 400 `alanlar.vergiNo` |
| POST /api/v1/islemler/hurda-alis | satici.tckn | kural vardı (negatif mod hatası vardı) | tek kaynak · 400 `alanlar.tckn` (tek eksik TCKN ise) · sade metin |
| POST /api/v1/islemler/takas (Değişim) | satici.tckn | aynı kapı | aynı · 400 `alanlar.tckn` |
| Satış eski altın bacağı (OdemeSepeti) | bacak.satici.tckn | aynı kapı | aynı (tek kaynak) |
| PATCH gider pusulası yeniden düzenle (HurdaModulu) | tckn | aynı kapı | aynı (tek kaynak) |
| MASAK kimlik bloğu taşıyan tüm uçlar (satış, banka hareketi, hurda, kasa…) — `MasakKapisi.Dogrula` | masak.belgeNo (belgeTuru=TC_KIMLIK) | yalnız "boş değil" | + kural · 400 `alanlar.belgeNo` |
| POST /api/v1/evraklar · PATCH /api/v1/evraklar/{id} | borcluKimlikNo (11 hane) | 10–20 karakter | + kural · 400 `alanlar.borcluKimlikNo` |
| İçe aktarım (cari) | vergiNo | Cari servisinden geçer | aynı (CariServisi.Olustur) |

**Ekran:**

| Ekran / bileşen | Önce | Sonra |
|---|---|---|
| Değişim satıcı TCKN (FisTakasSatici) · Alış eski altın satıcı (FisHurdaAlis) — `HurdaGirisi.TcknHata` | yalnız 11 hane | 11 hane yazılınca anında "TC kimlik numarası geçerli değil."; kilit sebebi aynı metin (TakasBirlesikKilit, Islemler HURDA_ALIS kilidi, FisTakasSatici.EksikMetni) |
| Satışta eski altınla ödeme (AltinBacagiGirisi) | yalnız 11 hane | + kural · alan altı + eksik listesi |
| MASAK kimlik formu (MasakFormu) — Satış/Değişim, Banka dekont, Hurda | yalnız boş değil | TC_KIMLIK + 11 hane ya da rakam dışı karakter varsa anında alan altı (`#masak-belge-no-hata`); `Eksiksiz` false; kilit sebebi `TcknKilitMetni` (Islemler, Banka, Hurda) |
| Cariler yeni + düzenle | yalnız Kaydet'te | anında alan altı + Kaydet kilitli + `#cari-tckn-kilit` / çekmece kilidi |
| İşletme kimliği (Isletme.razor) | 10/11 hane | `IsletmeKimlikSozlugu.VergiNoMesaji` üzerinden anında alan altı ve kilit (sayfa kodu değişmedi) |
| Evraklar borçlu kimlik no | yok | alan altı (`#evrak-form-borclu-kimlik-hata`) + FormGecerli + kilit sebebi |
| Gider pusulası yeniden düzenle | 11 hane | + kural · alan altı + kilit sebebi |

## 4. Değişen dosyalar

**Yeni:** `dotnet/Kuyumcu.Core/Domain/KimlikNoDogrulayici.cs`, `dotnet/Kuyumcu.Core/I18n/Sozlukler.KimlikNo.cs` (TR+EN), `dotnet/Kuyumcu.Tests/TcknDogrulamaTestleri.cs`.

**Değişen (kod):** `Core/Domain/KimlikNo.cs`, `Core/Domain/VergiKimlik.cs`, `Core/I18n/Sozlukler.IsletmeKimlik.cs`, `Api/Modules/Hurda/GiderPusulasiKapisi.cs`, `Api/Modules/Cari/CariModulu.cs` (yalnız iki `AlanIle`), `Api/Modules/Admin/AdminModulu.cs` (yalnız bir `AlanIle`), `Api/Modules/Masak/MasakKapisi.cs` (yalnız kimlik no satırları), `Api/Modules/Evrak/EvrakModulu.cs`, `Web/Bilesenler/Fis/FisModelleri.cs`, `Web/Bilesenler/Fis/FisTakasSatici.razor`, `Web/Bilesenler/MasakFormu.razor`, `Web/Servisler/AltinBacagiGirisi.cs`, `Web/Pages/Islemler.razor` (iki satır içi değişiklik, satır sayısı aynı), `Web/Pages/Islemler.ParaDemo3.cs`, `Web/Pages/Cariler.razor`, `Web/Pages/Banka.razor`, `Web/Pages/Hurda.razor`, `Web/Pages/Evraklar.razor`, `Web/Pages/GiderPusulalari.razor`.

**Değişen (test fikstürü):** `IsletmeBilgileriTestleri.cs` (11111111111 → 11111111110), `Q103IsletmeKimlikTestleri.cs` (12345678901 → 12345678950, 98765432101 → 98765432150 + maske beklentisi), `EvrakTestleri.cs` (→ 12345678950 / 98765432228), `TM1MusterilerTuvalTestleri.cs`, `P16SatisKalemiTestleri.cs`, `TevsikIhlalTestleri.cs`, `TevsikEkranTestleri.cs`, `S21FisBilesenTestleri.cs`, `GiderPusulasiTestleri.cs` (mesaj metni), `EkranTestleri.cs` (ZO28d: "U1234567" pasaport numarası olduğu için belge türü PASAPORT yapıldı), `CariEkranTestleri.cs` (+2 test).

Hash/şifreleme testlerindeki rastgele 11 haneli dizgeler (AlanSifreleme, MasakServis, Destek tanılama) doğrulama yolundan geçmediği için değiştirilmedi.

**Dokunulan sayfalar (TEK DOKUNUŞ):** Islemler.razor, Cariler.razor, Banka.razor, Hurda.razor, Evraklar.razor, GiderPusulalari.razor. Gerekçe: kurucu bulgusu, yalnız kimlik numarası alanı ve kilit metni. Yeni form, uç ya da migration yok. Mevcut id'ler değişmedi. Yeni id'ler: `masak-belge-no-hata`, `cari-tckn-kilit`, `evrak-form-borclu-kimlik-hata`.

**Migration:** gerekmedi (şema değişikliği yok).

## 5. DoD sözleşmesi

Yeni varlık ya da kayıt türü YOK. Mevcut uçlara yalnız doğrulama eklendi. PATCH sözleşmesi korunuyor: gönderilmeyen alan korunuyor (Cari/Evrak/İşletme "Verildi" bayrakları aynen). Geçersiz PATCH 400 dönüyor ve değer korunuyor (`Cari_PATCH_gecersiz_400_deger_korunur`, canlı SQL). Finansal satır yazılmadan red veriliyor.

## 6. Testler

| Koşum | Toplam | Geçti | Başarısız | Atlanan |
|---|---|---|---|---|
| Yeni TCKN testleri (TcknDogrulayici 40 + TcknUc 7 + TcknIstemciModel 4 + TcknMasakFormuEkran 1 + CariEkran.TCKN 2) | 54 | 54 | 0 | 0 |
| Filtreli (Tckn + CariEkran + HurdaKapi, temiz derleme sonrası) | 147 | 147 | 0 | 0 |
| **Tam süit** (tek koşum, `trx-pTCKN-DOGRULAMA/tam.trx`) | 10480 | 10471 | 7 | 2 |

Tam süitin 7 kırmızısından biri bu paketin ürünüydü: `S21FisSafTestleri.HurdaGirisi_satici_kimligi…`, uydurma "123-456-789-01" fikstürü. Fikstür düzeltildi, filtreli koşum YEŞİL. Kalan 6 kırmızı bu paketin değil (§ 9).

Derleme: `dotnet build Kuyumcu.sln` → **0 Hata · 0 Uyarı** (son temiz derleme; ara derlemelerde görülen 7 uyarı başka şeritlerin test dosyalarındaydı: SUBEKAPSAMA, KesifP2, RaporUc, SonKalanlar, Toplu6).

## 7. Mutasyonlar (15 + 1 tekrar; hepsi kırmızı, hepsi cmp AYNI)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | 10. hane kontrolü silindi | ilk koşumda **0 kırmızı (TEST AÇIĞI)** → `10000000157` örneği eklendi → TcknGecerli_sinirlari, Gider_pusulasi_kapisi_400_alan_tckn | AYNI |
| M1-2 | 11. hane → `return true` | TcknGecerli_sinirlari, TcknDenetle, TcknVeyaVknDenetle, YknMi, Cari_kurali…, Isletme_kurali…, Gider_pusulasi_kapisi_400… (9) | AYNI |
| M1-3 | ilk hane 0 kontrolü silindi | TcknGecerli_sinirlari | AYNI |
| M1-4 | matematiksel mod → C# `%` | TcknGecerli_sinirlari, Gider_pusulasi_kapisi_ayni_tek_kaynagi_kullanir | AYNI |
| M2-1 | KimlikNo.Dogrula kural satırı `if (false)` | Cari_POST…, Cari_PATCH…, Cari_kurali…, CariEkran TCKN_yeni_cari… | AYNI |
| M3-1 | VergiKimlik 11 hane kuralı kapatıldı | Isletme_kimlik_PATCH_gecersiz_400, Isletme_kurali… | AYNI |
| M4-1 | MasakKapisi TC_KIMLIK kontrolü kapatıldı | Masak_TC_kimlik_belge_no_gecersiz_400… | AYNI |
| M5-1 | Evrak BorcluKimlikDenetle kapatıldı | Evrak_borclu_kimlik_gecersiz_400_satir_yok | AYNI |
| M6-1 | GiderPusulasiKapisi.TcknGecerli → true | UydurmaTcknReddedilir, Gider_pusulasi_kapisi_400…, Hurda_alis…, Degisim_takas… | AYNI |
| M6-2 | `AlanIle("tckn")` kaldırıldı | Gider_pusulasi_kapisi_400_alan_tckn, Hurda_alis… | AYNI |
| M7-1 | HurdaGirisi.TcknHata kural satırı silindi | Satici_TCKN_11_hane_gecersiz… | AYNI |
| M8-1 | MasakFormu.Eksiksiz TCKN satırı silindi | Masak_TC_kimlik_belge_no_kilit_ve_alan_hatasi | AYNI |
| M8-2 | MasakFormu alan altı Hata parametresi kaldırıldı | TcknMasakFormuEkranTestleri.TC_kimlik… | AYNI |
| M9-1 | Cariler TcknAnindaHata → null | CariEkran TCKN_yeni_cari…, TCKN_duzenle… | AYNI |
| M10-1 | AltinBacagiGirisi kural satırı silindi | Altin_bacagi_satici_TCKN… | AYNI |

Mutasyonlardan sonra hepsi `touch` edilip temiz derlendi ve TCKN filtresi 147/147 yeşil.

⚠ **Süreç olayı (açık yazıyorum):** mutasyon betiği yedeği `shutil.copy2` ile geri yükledi. Bu komut eski değişiklik zamanını korur. Bu yüzden MSBuild, geri yüklenen dosyaları "güncel" saydı. Ortak ağaçta Core, Api ve Web DLL'leri ~00:45–00:58 arasında **son mutasyonla derlenmiş hâlde** kaldı (Core: M1-1, Api: M6-2, Web: M10-1). Canlı API'nin ilk koşumunda hurda red gövdesinde `alanlar` çıkmayınca fark edildi. Dosyalar `touch` edildi, temiz derleme yapıldı (00:58) ve canlı adımlar yeniden koşuldu. Kaynak dosyalar her zaman cmp AYNI idi. Bu aralıkta ortak ağaçta test koşan başka şeritler bu DLL'lerle koşmuş olabilir. Orkestratöre not: betik kalıbında yedekten sonra `touch` zorunlu.

## 8. Denetim

`dotnet/denetim.sh` → 306 ✓, tek ✗ **P-30 DEMO_INSAN_SURESI** (görevde hariç tutulan, bu paketle ilgisiz). P-30 dışında TEMİZ.

## 9. Tam süitteki başkasına ait kırmızılar (açık kalanlar)

| Test | Belirti | Sahibi (tahmin değil, gözlem) |
|---|---|---|
| RaporYaslandirmaEkranTestleri ×2, Toplu3YaslandirmaEkranTestleri.T3_Sifir_hucre… | "1.000 ₺" beklenirken "1.000,00 ₺" | Para biçimi / rapor ekranı — bu paket Bicim'e ve rapor ekranlarına dokunmadı. Eşzamanlı şerit RAPOR-3 (t1). |
| G5SatisKirilimiEkranTestleri.G5_Ekran_kasaya_giren_karti… | "10.840 ₺" yok, ",00" biçimi | aynı sınıf |
| UX1C3EkranTestleri.C3_50_Adet_para_gibi_bicimlenmez | WaitFor zaman aşımı (biçim) | aynı sınıf; ilk filtreli koşumda da kırmızıydı |
| Yasal4HttpTestleri.Mutabakat_bugun_iptal… | 1 beklenirken 0 | Devir kaydında "Yasal4 UTC gün sınırı — önceden var" (03.10) |

## 10. Canlı doğrulama (API 5188, kuyumcu_net, temiz derleme)

Test kiracısı `PAKETTCKN-DOGRULAMA-TEST` (01M4C5HN5EEP80G99KXBF4ZXYS). Sistem daveti 201, kayıt 201, giriş 200. Sistem anahtarı ve parola rastgele üretildi, 600 izinli dosyada tutuldu, hiçbir çıktıya yazılmadı.

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| POST /cari vergiNo=11111111111 | 400 alanlar.vergiNo | **400** · `alanlar:[vergiNo]` · "TC kimlik numarası geçerli değil" | cariler 0→0 |
| POST /cari 10000000146 (TCKN) | 201 | **201** | cariler 0→1 |
| PATCH /cari/{id} 11111111111 | 400, değer korunur | **400** `[vergiNo]` | vergi_no korundu=1 |
| PATCH /admin/tenant/kimlik 11111111111 | 400 | **400** `[vergiNo]` | tenants.vergi_no yazılmadı (0) |
| PATCH /admin/tenant/kimlik 10000000146 | 200 | **200** | yazıldı (1) |
| POST /islemler/hurda-alis satıcı 11111111111 | 400 alanlar.tckn | **400** `[tckn]` "Hurda alış: TC kimlik numarası geçerli değil" | hareketler 2→2 · gider_pusulalari 1→1 |
| POST /islemler/hurda-alis 10000000146 | 200 + pusula | **200** | hareketler 0→1 · gider_pusulalari 0→1 |
| POST /islemler/takas (Değişim) satıcı 11111111111 | 400 alanlar.tckn | **400** `[tckn]` | hareketler 3→3 |
| POST /islemler/takas 10000000146 | 200 | **200** | hareketler 3→4 |
| POST /evraklar borcluKimlikNo 11111111111 | 400 | **400** `[borcluKimlikNo]` | kiymetli_evraklar 0→0 |
| POST /evraklar 10000000146 | 201 | **201** | 0→1 |
| POST /bankalar/{id}/hareket 400.000 + MASAK TC_KIMLIK 11111111111 | 400 alanlar.belgeNo | **400** `[belgeNo]` "MASAK: TC kimlik numarası geçerli değil" | masak_kimlik_tespitleri 0→0 · banka_hareketleri 0→0 |
| aynı, 10000000146 | 200 + tespit | **200** | tespit 0→1 · banka 0→1 |
| aynı, PASAPORT U1234567 | 200 (kurala tabi değil) | **200** | tespit 1→2 |

Kanıtlar `ham/TCKN-DOGRULAMA-olcum/canli-tablo*.json` dosyalarında. İlk koşumda iki adım işe yaramadı: (a) stok yoktu, takas "ürün stoğu bulunamadı" döndü; (b) hurda kırmızısı mutasyonlu DLL ile koşmuştu (§ 7). İki adım da ürün alışı yapılıp temiz derleme sonrası yeniden koşuldu. İlk koşumun kayıtları da dosyalarda duruyor.

**Temizlik:** `zsh kiraci-temizle.sh <id> --uygula`. Kiracının tenant_id taşıyan tüm tablolarda kalan satırı **0** (information_schema taraması). tenants **18 → 18**. API (PID ile), vekil ve Chrome durduruldu. 5188/5189 dinleyen yok. 5199'a dokunulmadı. Not: kullanılmış sistem daveti `davetler` tablosunda duruyor (tenant kolonu yok, temizlik betiği kapsamıyor — MASAK-370'teki ile aynı durum).

## 11. Gerçek tarayıcı ölçümü (başsız Chrome, vekil 5189, CDP 9981–9984)

3 ekran × 3 genişlik (1440 / 1280 / 375) × 2 mod (BASİT / PRO) × 2 tema (açık / koyu) = **36 ölçüm**. Görüntüler ve `olcum.jsonl` `ham/TCKN-DOGRULAMA-olcum/` altında. Read ile bakılan görüntüler: degisim BASIT açık 1440, masak BASIT açık 1440, cariler BASIT açık 375, degisim PRO koyu 375, cariler PRO koyu 1280, masak PRO koyu 375.

| Ekran | Senaryo | Alan altı hata (görünür) | Kaydet | Kilit metni | K1 · K3 · K5 · yatay |
|---|---|---|---|---|---|
| Değişim (/islemler?tur=TAKAS, müşteri seçili, Vatandaş) | satıcı TCKN 11111111111 | 12/12 ✓ "TC kimlik numarası geçerli değil." | 12/12 kilitli | 12/12 aynı metin | 0 · 0/0 · 0 · 0 |
| MASAK kimlik formu (/banka Dekont 400.000, T.C. kimlik kartı) | belge no 11111111111 | 12/12 ✓ | 12/12 kilitli | 12/12 aynı metin (`#dekont-kaydet-sebep`) | 0 · 0/0 · 0 · 0 |
| Cariler Yeni müşteri | vergi no 11111111111 | 12/12 ✓ | 12/12 kilitli | 12/12 aynı metin (`#cari-tckn-kilit`) | 0 · 0/0 · 0 · 0 |

K9 (binen) = 4–6 yalnız modal ekranlarda (masak, cariler) çıktı. Örnekler: `#banka-yeni ⟂ h3.zo-masak-baslik`, `#cari-segment-hepsi ⟂ Telefon etiketi`. Bunlar modalın arkadaki sayfa öğeleriyle çakışması; ölçerin modal katmanını ayırmamasından kaynaklanıyor. Bu paketin öğeleri değil. Değişim'de K9 = 0. K2 (dar sütun) modal pencerede %58–60, sayfa düzeninde %69–98: modal genişliği tasarım gereği.

Ölçülmeyen ekranlar (koddaki kural + bUnit/model testi var, gerçek tarayıcı ölçümü YOK): Evraklar borçlu kimlik, Gider pusulası yeniden düzenle, Alış eski altın satıcısı (FisHurdaAlis), Satış eski altın bacağı, İşletme kimliği → § 12.

## 12. Mevcut veri sayımı (kuyumcu_net, SALT OKUMA, numara basılmadı)

| Tablo.alan | Kiracı | TCKN sayılan | Geçersiz |
|---|---|---|---|
| cariler.vergi_no | QA-TEST-1 Kuyumculuk | — | **3** |
| cariler.vergi_no | QA-TEST-2 Kuyumculuk | — | **1** |
| cariler.vergi_no (toplam TCKN sayılan) | tüm kiracılar | 4 | 4 |
| tenants.vergi_no (11 hane) | — | 0 | 0 |
| gider_pusulalari.satici_tckn (şifreli, uygulama anahtarıyla bellekte çözüldü) | QA-TEST-1/2/3/FRONTEND, PAKET-SKB-HAKEM-TEST | 7 | 0 |
| masak_kimlik_tespitleri.belge_no (TC_KIMLIK, şifreli) | Demo Kuyumcu | 1 | 0 |
| kiymetli_evraklar.borclu_kimlik_no | — | 0 | 0 |

Çözülemeyen şifreli satır: 0. **Düzeltme yapılmadı** (kurucu kararı). Bu 4 satır kalıcı QA fikstürü (QA-TEST-*) içinde. Bu kayıtların PATCH'i artık vergi no alanı da gövdedeyse ya da tür değişirse 400 alır (Cari "nihai durum" kuralı). Ekranda düzenleme çekmecesi, alan altında geçersiz numarayı gösterip Kaydet'i kilitler, kullanıcı numarayı düzeltmeye yönlendirilir.

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Tek kaynak Core KimlikNoDogrulayici, GiderPusulasiKapisi bağlı, YKN/VKN | KARŞILANDI | § 1, M1-x, M6-1 |
| (2) Tüm yazma uçları, 400 alanlı, sade Türkçe; pasaport tabi değil | KARŞILANDI | § 3, § 10 |
| (3) Ekran: anında alan altı + kilit sebebi aynı metin | KARŞILANDI (ölçülen 3 ekran + bUnit/model) | § 11, M7–M10 |
| (4) Mevcut veri sayımı, yalnız adet | KARŞILANDI | § 12 |
| (5) Fikstürler geçerli numaraya; sınır testleri | KARŞILANDI | § 4, TcknDogrulayiciTestleri |
| Build 0/0 · denetim TEMİZ (P-30 hariç) · mutasyon · tam süitte bu paketten 0 kırmızı | KARŞILANDI | § 6–9 |
| KVKK: numara basılmadı | KARŞILANDI | yalnız sentetik test numaraları |

## 14. Kurucuya / mali müşavire

1. TCKN kontrol formülünün NVİ tarafından yayımlanmış resmî metni yok. NVİ yalnız son iki hanenin doğrulama sayısı olduğunu söylüyor. Uygulanan formül yerleşik ve kamuya açık formül. Kurucu emriyle uygulandı.
2. VKN için GİB'in yayımladığı bir kontrol hanesi formülü bulunamadı. Şu an yalnız 10 hane kontrol ediliyor (TEYİTSİZ). İstenirse ayrı kalem olarak açılmalı.
3. YKN (99…) için ayrı bir resmî formül bulunamadı. TCKN kuralı uygulanıyor.
4. QA-TEST-1 ve QA-TEST-2'de 4 cari kaydında geçersiz TCKN var. Düzeltme kararı kurucuda.
5. Değişim red metni "Hurda alış: …" önekiyle başlıyor. Bu, kapının ortak önekinden geliyor ve önceden de böyleydi. Metin dili toplu kuyruğa önerilir (DÜŞÜK).
