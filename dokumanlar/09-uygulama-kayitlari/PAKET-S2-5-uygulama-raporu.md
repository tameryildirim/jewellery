# PAKET S2-5: Kaydet → Yazdır + barkod odağı + para üstü + satıcı. Uygulama raporu

Rol: Taşkın (backend) ve Ertuğ (frontend). Dalga S2 (Tezgâh fişi, Pusula formülü). Tarih: 2026-09-24, 02:40 ile 03:25 arası.
Durum: **YARIM.** Plan 2.8'in "satıcı personel" (`personel_id`) kalemi yapılmadı. Sunucuda bu alanı yazan bir yol yok ve gereken değişiklik `Modules/Satis` içinde. O klasör S-B şeridinin alanı, bu şeridin dokunması yasak (§ 0, § 9).
Diğer bütün kalemler yapıldı: Kaydet → Yazdır, ham ULID'in ekrandan kalkması, barkod odağı ve para üstü. Paket kapısının yedi maddesi bu kalemler için sağlandı.
Commit: YOK. Tüm değişiklikler çalışma ağacında.

Şartname: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-2 madde 2.7 ve 2.8 · `KARARLAR-D1-D11-2026-09-23.md` D7 ("Para üstü önizlemesi: EVET — istemci hesabıdır, deftere girmez") · taslak `sadelestirme/tuval/project/Tezgah.dc.html` ("Kaydet ve yazdır").

---

## 0. Spesifikasyon koda karşı doğrulandı

| İddia | Kodda bulunan | Uyarlama |
|---|---|---|
| "Islemler.razor'dan /cikti'ya bağlantı YOK" | Doğru: grep 0. | Bağlantı eklendi (`FisYazdirBaglantisi`). |
| `/cikti/{Tur}/{Id}` hazır (P-43) | Sunucu `CiktiServisi.HareketBelgesi` **hareket** kimliği bekliyor (`x.Id == id`). SATIS_FISI yalnız SATIS, SATIS_IADE, ALIS ve ALIS_IADE türlerini kabul ediyor. | **Sapma (zorunlu):** Karma fiş ucu (`/islemler/satis-fisi`) yanıtta yalnız `fisId` döndürüyor, hareket kimliği yok (ölçüldü: canlı adım 11). Bu yüzden sayfa kayıttan sonra `GET /islemler/fis/{fisId}` ile fişi okuyor ve belge üreten ilk kalemin kimliğini alıyor. Bu, sayfanın zaten kullandığı bir okuma ucu; sunucuya dokunulmadı. Tek kalemli satış ve alış uçları hareketin kendisini (`id`+`tur`), iade uçları `hareketId` döndürüyor; bunlar doğrudan okunuyor. |
| "Başarı bandında Fiş yazdır" | `BasariBandi` 3 sn sonra kendini gizliyor (UX-4c madde 13). | **Sapma (gerekçeli):** Bağlantı bandın **hemen altına** konuldu ve bir sonraki Kaydet'e kadar ekranda kalıyor. 3 sn'de kaybolan bir yazdır düğmesine tezgâhtar yetişemez. Yeni sekmede açılıyor; satış ekranı ve barkod odağı yerinde kalıyor. |
| "Ham ULID gösterilmez" | `BasariMetni` şimdiye kadar `Fiş no: {fisId}` basıyordu (ULID). | `FisYazdir.OkunurNo`: 26 karakterlik Crockford-base32 değer banda basılmıyor. Sunucu okunur bir numara döndürürse (örneğin pusula no) o yine yazılıyor. ULID yalnız bağlantı adresinde duruyor. |
| "#islem-barkod odaklı (mekanizma mevcut)" | Mekanizma (`kuyumcuOdak.alana`, cekmece.js) vardı ama barkod alanına **bağlı değildi** (grep 0). | `FisKalemGirisi` sayfa açılınca ve her başarılı kayıttan sonra (`OdakJeton=basariJeton`) odağı barkod alanına taşıyor. Yeni betik eklenmedi. |
| Para üstü (D7) | Yok. | `FisParaUstu`: tek ödemeli NAKİT satışta ve karmada NAKİT bacaklarının toplamı üzerinden hesaplıyor. İstemci hesabı; hiçbir gövdeye konmuyor, Kaydet'i kilitlemiyor. Nakit 0'a düşünce (kayıttan sonra) alınan tutar sıfırlanıyor. |
| `personel_id` oturum kullanıcısı varsayılan, pro modda değiştirilebilir | Kolon var (`Entities.cs:981`, P-08). **Satış ve fiş uçları `personelId` almıyor.** `Modules/` altında `PersonelId` yazan kod yok (grep: yalnız `SatisRaporlari` okuyor). Canlı SQL: yeni satışlarda `personel_id IS NULL` = 1 (§ 6). | **YAPILMADI.** İstemci alanı sunucu kabul etmeden eklenirse ekran "satıcı kaydedildi" izlenimi verir ama veritabanına NULL yazılır. Bu, Anayasa md.1 ve UX-S25 ihlali olur. Gereken değişiklik `Modules/Satis` içinde (S-B şeridinin alanı, "DOKUNMA"). Devir tanımı § 9'da. |
| Taslak düğmesi "Kaydet ve yazdır" | `#islem-kaydet` metni "{Tür} Kaydet". bUnit testleri bu metne ve id'ye bağlı. | Düğme metni değişmedi. Yazdırma, kayıttan sonraki ayrı bir dokunuş. Tarayıcının yazdırma penceresini otomatik açmak için `Cikti.razor` dosyasında değişiklik gerekir; o dosya bu şeridin alanı dışında (§ 9). |
| Mevzuat | Bu paket yeni oran, eşik ya da kural eklemiyor. Para üstü deftere girmiyor (D7). | Mevzuat araştırması gerekmedi. |

## 1. Yapılanlar

| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/Bilesenler/Fis/FisYazdir.cs` (YENİ) | Saf kurallar: `BelgeliTurler` (sunucunun SATIS_FISI kümesiyle aynı; test S-4 yansıma ile karşılaştırıyor), `Adres`, `HareketId`, `IlkKalem`, `UlidMi`, `OkunurNo`, `ParaUstu` (kuruşa AwayFromZero). İstek atmıyor. |
| `Kuyumcu.Web/Bilesenler/Fis/FisYazdirBaglantisi.razor` (YENİ) | `#islem-yazdir` bağlantısı (`<a target=_blank>`). Çok kalemli tek-uç satışta `#islem-yazdir-2..n` (Fiş yazdır (i/n)). Bunlara ek olarak `#islem-yazdir-ipucu`, `#islem-yazdir-hata` (UX-S28) ve `#islem-yazdir-sar` var. |
| `Kuyumcu.Web/Bilesenler/Fis/FisParaUstu.razor` (YENİ) | `#islem-alinan` (Para, TRY), `#islem-para-ustu` ("Para üstü" / "Eksik", eksikte `metin-kirmizi`) ve `#islem-para-ustu-sar`. |
| `Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` | `@inject IJSRuntime` ve `OdakJeton` parametresi eklendi. `OnAfterRenderAsync` jeton değişince `kuyumcuOdak.alana("islem-barkod")` çağırıyor. |
| `Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor` | `Toplam` parametresi eklendi. Tek ödemeli SATIŞ + NAKİT durumunda `<FisParaUstu Nakit=Toplam/>` çiziliyor. |
| `Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor` | Karmada `<FisParaUstu Nakit=Σ NAKİT bacak/>` çiziliyor. |
| `Kuyumcu.Web/Pages/Islemler.razor` | `yazdirIdler` ve `yazdirHata` alanları eklendi. Tek-uç satış, alış ve iade yanıtları `YazdirEkle` ile okunuyor. `YazdirKalemiBul()`: karma ve iskontolu fişte GET `/islemler/fis/{fisId}` çağrılıyor, hata yutulmuyor. Bandın altında `<FisYazdirBaglantisi/>` var. `BasariMetni` ULID basmıyor. `OdakJeton`/`Toplam` bağları eklendi. **Kaydet() yerinde, POST sayısı aynı; tek POST yolu korundu.** Satır sayısı 1399 → 1399: 6 fazla boş satır ve sahipsiz kalmış 4 satırlık yorum bloğu kaldırıldı. |
| `Kuyumcu.Core/I18n/Sozlukler.FisYazdir.cs` (YENİ) | `FisYazdirMetinleri` konumsal record, TR + EN (eksik anahtar = derleme hatası). Sozlukler.cs hub'ına dokunulmadı. |
| `Kuyumcu.Tests/S25YazdirParaUstuTestleri.cs` (YENİ) | 9 bUnit + 10 saf (teori dahil) + 2 HTTP = **21 test** (§ 3). |

Sunucu (Modules/Satis, Modules/Cari, Modules/Cikti) **değişmedi.**

**DOM id envanteri:** `Islemler.razor` için önce/sonra `grep -o 'id="[^"]*"'` karşılaştırıldı: silinen 0, değişen 0 (`scratchpad/pS2-5/id-once.txt` = `id-sonra.txt`). Fiş bileşenlerine yalnız ekleme yapıldı. Yeni id'ler: `islem-yazdir`, `islem-yazdir-{n}`, `islem-yazdir-sar`, `islem-yazdir-ipucu`, `islem-yazdir-hata`, `islem-alinan`, `islem-para-ustu`, `islem-para-ustu-sar`.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Islemler.razor`. Bu sayfa S-2 şeridinin kendi sayfası; S2-1..S2-4 ile aynı şerit ve plan sırası S2-5. Başka sayfa açılmadı.

**Tasarım belirteçleri:** Yeni satır içi `style` yok, sabit renk yok. Yalnız mevcut sınıflar kullanıldı (`btn btn-ikincil`, `form-grid-2`, `hucre-vurgu`, `metin-kirmizi`, `metin-kucuk metin-soluk`). İkon mevcut `Ikonlar.Faturalar` ("receipt"); yazıcı ikonu yok ve yeni ikon eklenmedi.

## 2. DoD sözleşmesi

| Kayıt / öğe | Düzenleme yolu |
|---|---|
| "Alınan" tutarı | Kaydedilmiyor (D7: istemci hesabı, deftere girmez). Kayıttan önce serbestçe değişiyor; fiş kapanınca sıfırlanıyor. |
| Yazdırılan fiş | Türetilmiş çıktı. `Cikti.razor` üzerinde düzenleme yolu yok ve `DegismezIpucu` bunu yazıyor (P-17). Düzeltme kaynakta yapılır: iade ya da storno. |
| Yeni kayıt türü | Yok, migration yok. |
| Yeni tıklanabilirler | `#islem-yazdir` gerçek bir sayfa açıyor (href). Kimlik yoksa çizilmiyor, hata varsa sebebi yazıyor. Boşa tıklanan düğme yok (DoD EK 1). |

## 3. Testler

Önce (tam süit, değişiklikten önce, 02:45; `scratchpad/pS2-5/trx-once`): **4761 · 4751 geçti · 10 kırmızı** (`scratchpad/pS2-5/once-kirmizi.txt`). Sahipleri:
- GunKapanisiEkranTestleri ×6 ve KasaKapatmaTestleri ×3. Hata: "ArayuzModuServisi kayıtlı değil (ProAlan)". Sahibi S3-B (`Kasalar.razor`).
- AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ. Tam süitte zaman aşımı; filtreli koşumda yeşil (t1, t5). Yük altında kararsız.

Yeni testler (21, hepsi yeşil; `scratchpad/pS2-5/t4.log`):

| Test | Sınanan |
|---|---|
| S25_E1 | Açılışta `kuyumcuOdak.alana("islem-barkod")` 1 kez çağrılıyor; kalem eklerken odak çalınmıyor; kayıttan sonra 2. kez çağrılıyor |
| S25_E2 | Tek kalem: `#islem-yazdir` href `/cikti/SATIS_FISI/h1`, `_blank`, fiş GET yok; bant gizlenince bağlantı kalıyor |
| S25_E3 | Karma: GET `/islemler/fis/f1` tek kez; HURDA_ALIS kalemi atlanıyor → `h-sat` |
| S25_E4 | ULID banda ve bağlantı metnine basılmıyor, adreste duruyor |
| S25_E5 | Fiş GET 500 → `#islem-yazdir-hata` sunucu mesajıyla, bağlantı yok, POST 1 |
| S25_E6 | 12000 → para üstü 2.000,00; 9000 → "Eksik" 1.000,00 kırmızı; gövdede `alinan` ve 12000 yok |
| S25_E7 | Karma NAKİT 6000 + KART 4000; alınan 6500 → 500; tek `#islem-alinan` |
| S25_E8 | Kayıttan sonra alınan sıfırlanıyor, sonraki fişe taşınmıyor |
| S25_E9 | Satış iadesi yanıtı `hareketId` → bağlantı |
| S25_S1 (5 teori + 1) | ParaUstu hesabı ve yuvarlama; alınan yoksa null |
| S25_S2 | HareketId / IlkKalem / Adres uydurmuyor, kaçış yapıyor |
| S25_S3 | UlidMi / OkunurNo |
| S25_S4 | `BelgeliTurler` = sunucunun `CiktiServisi.BeklenenTurler(SATIS_FISI)` kümesi (yansıma) |
| S25_S5 | Sözlük TR+EN dolu, ham kod yok |
| S25_H1 (HTTP) | Gerçek boru hattı: satis-fisi → `fisId` (hareket yok) → `/islemler/fis` → IlkKalem → `/cikti/SATIS_FISI/{id}` 200, `tur`=SATIS_FISI, `belgeNo`=fisId |
| S25_H2 (HTTP) | Tek kalem satış yanıtı HareketId verir → çıktı 200 |

Gerileme seti (S21–S24, BasitModPos, KarmaOdemeEkran, PosEkran, PosTeklifEkran, PosBarkod, B4AltinBacagiFis, AlisOdemeAraci, Iade, UX4b, UX3, Cikti, P17): **280/280 yeşil** (`t5.log`). Bu sette `S21 Islemler_razor_1400_satiri_asmaz`, `Fis_bilesenleri_ApiIstemci_enjekte_etmez` ve `Tek_POST_yolu` da yeşil.

Sonra (tam süit, 03:09, `scratchpad/trx-pS2-5/`): **4812 · 4808 geçti · 4 kırmızı**, hepsi `EtiketEkranTestleri` (DepoErisilemezseSayfaCalisir, EksikUrunlerGeriYuklemedeBildirilir, EtiketTaslagiGeriYuklenir, TaslagiTemizleFormuBosaltir).
- Sahibi S3-A: `UrunEtiketleri.razor` koşum sırasında, 03:07–03:11'de o şerit tarafından değiştirildi.
- Bu paketin dosyalarını kullanmıyorlar.
- Koşumdan sonra aynı filtreyle yeniden koşuldu: **65/65 yeşil** (`t6.log`).
- Önceki 10 kırmızı bu koşumda yeşil.
- **Bu paketin ürettiği yeni kırmızı: 0.**

## 4. Mutasyon kanıtları (9/9 kırmızı, hepsi `cmp` birebir)

Yöntem: yedek `scratchpad/mut-yedek/S2-5/` → tek eşleşmeli `perl -0777 -pi` (eşleşme sayısı betikte 1 olarak doğrulandı) → derleme → `--filter S25` → geri kopya → `cmp`. Betik `scratchpad/pS2-5/mut.sh`, sonuç `mut-sonuc.txt`. Son mutasyondan sonra `dotnet build Kuyumcu.sln` rc=0.

| Kod | Değişiklik | Kırmızı dönen test(ler) | cmp |
|---|---|---|---|
| M25-1 | `ParaUstu`: `a - nakit` → `nakit - a` | S25_E6; S25_S1 ×4 | BİREBİR |
| M25-2 | `OkunurNo`: ULID ayıklaması kaldırıldı | S25_E4; S25_S3 | BİREBİR |
| M25-3 | `IlkKalem`: tür süzgeci kaldırıldı (ilk kalem ne olursa) | S25_E3; S25_S2 | BİREBİR |
| M25-4 | `Islemler`: `await YazdirKalemiBul();` silindi | S25_E3; S25_E5 | BİREBİR |
| M25-5 | `FisKalemGirisi`: odak yalnız ilk çizimde (jeton yok sayıldı) | S25_E1 | BİREBİR |
| M25-6 | `FisParaUstu`: nakit 0'da sıfırlama kaldırıldı | S25_E8 | BİREBİR |
| M25-7 | `FisOdemeBacaklari`: NAKİT → KART toplamı | S25_E7 | BİREBİR |
| M25-8 | `Islemler`: `yazdirHata = ex.Message` yutuldu | S25_E5 | BİREBİR |
| M25-9 | `HareketId`: `hareketId` dalı silindi | S25_E9; S25_S2 | BİREBİR |

## 5. Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo`: **0 Hata, 0 Uyarı** (`build2.log`, `mut-son-build.log`).
- `bash dotnet/denetim.sh`: **DENETİM TEMİZ** (rc=0, `denetim.log`). İlgili sayaçlar:

| Sayaç | Değer | Tavan |
|---|---|---|
| satır içi style= | 110 | 196 |
| @onclick'siz ve disabled'sız `<button>` | 58 | 71 |
| ModalPencere kullanımı | 48 | 55 |
| P-18 SABIT_RENK | 0 | — |
| S2-3 FIS_BILESENI_ISTEK | 0 | — |

## 6. Canlı doğrulama (5192, `kuyumcu_net`)

Sürücü `scratchpad/pS2-5/canli.sh` + `canli.py`. Ekranın `FisYazdir` kuralları Python'da birebir uygulandı. Davet satırı doğrudan eklendi, parola rastgele; hiçbir sır basılmadı.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | POST /urunler (barkodlu) + kasa | 201 | 201 | ürün, kasa var |
| 4 | PATCH stok 10 | 200 | 200 | — |
| 5 | GET /cari/sistem-carisi | 200 | 200 | Peşin var |
| 6 | **Tık 1 okut**: GET /urunler/barkod/{kod} | 200 + aynı ürün | 200 | ayni_urun=True |
| 7 | **Tık 2 Kaydet**: POST /islemler/satis (basit gövde, NAKİT 1.000, Peşin) | 200 + id + tur | 200 | tur=SATIS, hareketId var |
| 8 | Kasa farkı | +1000 (alınan değil) | +1000 | 0 → 1000 |
| 9 | **Tık 3 Yazdır**: GET /api/v1/cikti/SATIS_FISI/{hareket} | 200 | 200 | belgeAdi=SATIŞ FİŞİ, belgeNo var |
| 10 | Tarayıcı rotası GET /cikti/SATIS_FISI/{hareket} | 200 | 200 | blazor.webassembly var |
| 11 | Karma POST /islemler/satis-fisi (NAKİT 600 + KART 400) | 200 + fisId | 200 | fisId var, yanıtta hareket kimliği **YOK** (sapmanın ölçülmüş gerekçesi) |
| 12 | GET /islemler/fis/{fisId} → ilk belgeli kalem | 200 + kalem | 200 | kalem var |
| 13 | GET /cikti/SATIS_FISI/{kalem} | 200, belgeNo = fisId | 200 | belgeNo_fisId=True |
| 14 | Olmayan hareket → çıktı | 404 | 404 | "Hareket bulunamadı" |

Not: 9. adımın özetinde `toplam=[None, None]` yazıyor. Bu sürücünün yanlış alan adı okumasından kaynaklanıyor (`CiktiTutar` alanı `tutar` değil); yanıt 200, belge adı ve belge no doğru.

SQL kanıtı (test kiracısı):
```
-- hareketler (tur, tl, has, odeme_turu, nakit, fis_id_dolu, personel_id_bos, kullanici_id_dolu)
SAYIM_DUZELTME  0.00    91.600000  NULL   0.00    0 1 1   (stok kurulumu)
SATIS           1000.00 -9.160000  NAKIT  1000.00 0 1 1
SATIS           1000.00 -9.160000  KARMA  600.00  1 1 1
-- kasa_hareketleri: NAKIT 1000.00 · KART 400.00 · NAKIT 600.00   (para üstü/alınan hiçbir yerde yok)
-- odeme_kalemleri: NAKIT 1000.00 · NAKIT 600.00 · KART 400.00
```
`personel_id_bos = 1` her satışta. Kayıtlar giren kullanıcıyla tutuluyor (`kullanici_id` dolu) ama "satan" alanı yazılmıyor. Bu, § 0'daki YAPILMADI kaleminin canlı kanıtı.

**Tarayıcıda uçtan uca tıklama yapılmadı.** Test kullanıcısının parolasını bir giriş alanına yazmak ajan güvenlik kuralıyla yasak. "3 tık" sunucu tarafında ekranın gönderdiği gövdelerle (adım 6-10) ve ekran tarafında bUnit ile (E-1 odak, E-2 bağlantı) kanıtlandı. Tarayıcıdan tıklama, kurucunun ya da QA turunun işi.

API günlüğünde `is_calismalari` benzersiz anahtar çakışması var (`MASAK_SIB_SURESI` günlük işi). Bu işi başka bir örnek aynı gün zaten koşmuş; arka plan zamanlayıcısına ait, bu paketle ilgisi yok.

**Temizlik:**
- API durduruldu (`kill` PID 80258 ve alt süreci); `lsof -iTCP:5192 -sTCP:LISTEN` boş.
- `PAKETS2-5-TEST` kiracısı `betikler/kiraci-temizle.sh --uygula` ile tenant_id taşıyan 70 tablodan silindi (56 satır + tenants 1).
- Davet satırı silindi (kalan 0).
- `tenants`: önce **11**, sonra **11**.
- `QA-TEST-*`, DEMO ve başka şeritlerin kiracılarına dokunulmadı.
- 5190-5200 aralığında 5192 dışındaki portlara dokunulmadı.

## 7. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)

```
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FisYazdir.cs                       (YENİ)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdir.cs                        (YENİ; dizin S2-1'den beri izlenmiyor)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdirBaglantisi.razor           (YENİ)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisParaUstu.razor                   (YENİ)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor                (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor                  (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor             (değişti)
?? dotnet/Kuyumcu.Tests/S25YazdirParaUstuTestleri.cs                     (YENİ; Tests dizini izlenmiyor)
```
Proje ağacında `.mutorj/.bak/.tmp` bırakılmadı. `Modules/Satis/*.mutorj` dosyaları başka şeride ait; bu paket oluşturmadı.

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| 2.7 Başarı sonrası "Fiş yazdır" → /cikti/{Tur}/{Id} | ✓ (bandın altında; gerekçe § 0) | E-2, E-3, E-9, H-1, H-2; canlı 9, 10, 13 |
| Ham ULID gösterilmez | ✓ | E-4, M25-2 |
| Sayfa açılınca #islem-barkod odaklı | ✓ (+ her kayıttan sonra) | E-1, M25-5 |
| Nakit bacağında "alınan" → para üstü (D7) | ✓ | E-6, E-7, E-8, S-1; canlı 8 (deftere girmiyor) |
| KABUL: para üstü doğru hesaplanır (test) | ✓ | S25_S1 ×6, M25-1 |
| KABUL: /cikti bağlantısı 200 | ✓ | canlı 9, 10, 13; H-1, H-2 |
| KABUL: canlı "3 tık" okut → Kaydet → Yazdır | ✓ API düzeyinde (tarayıcı tıklaması hariç, § 6) | canlı 6-10 |
| personel_id oturum kullanıcısı varsayılan, pro modda değişir | **✗ YAPILMADI** | § 0, § 9; canlı SQL personel_id NULL |
| Tek POST yolu, fiş bileşeni istek atmaz | ✓ | S21 yapı testleri; denetim FIS_BILESENI_ISTEK 0 |
| DOM id silinmedi | ✓ | id envanteri farkı 0 |
| Islemler.razor ≤ 1.400 satır | ✓ (1399) | S21_1400 yeşil |
| Sabit renk / inline style yok | ✓ | denetim |
| Sozlukler hub'ına dokunulmadı, TR+EN | ✓ | Sozlukler.FisYazdir.cs, S-5 |
| Paket kapısı (1) build 0 hata | ✓ | 0/0 |
| (2) kendi testlerim 0 başarısız | ✓ | 21/21 |
| (3) sayaçlarım ✓ | ✓ | DENETİM TEMİZ |
| (4) mutasyon kanıtı | ✓ | 9/9 |
| (5) canlı doğrulama | ✓ (personel hariç) | § 6 |
| (6) test kiracısı silindi | ✓ | 11 → 11 |
| (7) yeni kırmızı yok | ✓ | § 3 |

## 9. Açık kalanlar

1. **Satıcı personel (plan 2.8, üçüncü kalem): S-B şeridine devir.**
   - `POST /islemler/satis` ve `/islemler/satis-fisi` (gerekirse `satis-iade` ve `takas` da) `personelId?` almalı.
   - Değer gelmezse oturum kullanıcısı (`islem.KullaniciId`) yazılmalı.
   - Gelirse kiracının aktif kullanıcısı olduğu doğrulanmalı; değilse 400 dönmeli.
   - Değer `HareketFabrikasi.Ekle` taslağına `PersonelId` olarak geçmeli. Anlam kilidi "SATAN" (`Sabitler.HareketPersonelRolu`).
   - Testte `personel_id` DB'de doğrulanmalı. Migration gerekmiyor.
   - Uç hazır olunca bu şerit iki iş yapar: pro modda `#islem-personel` seçicisi (varsayılan oturum kullanıcısı), basit modda salt okunur "Satan: {ad}" satırı.
   - Paket bu kalem yüzünden **YARIM**.
2. **Otomatik yazdırma penceresi.** `Cikti.razor` belgeyi açıyor, tarayıcı penceresi `#cikti-yazdir` ile açılıyor; bu, 4. dokunuş. `?yazdir=1` ile otomatik `window.print` eklemek `Cikti.razor` değişikliği ister; o dosya bu şeridin alanı dışında. Karar orkestratörde.
3. **Çok kalemli tek ödemeli satış.** Tek-uç yol kalem başına ayrı hareket yazıyor; ekran her biri için ayrı "Fiş yazdır (i/n)" bağlantısı veriyor. Tek belge istenirse basit modda bu yolun da satis-fisi ucuna geçmesi gerekir; bu, plan dışı bir davranış değişikliği.
4. **Tam süitteki başkasına ait kırmızılar:**
   - EtiketEkranTestleri ×4: S3-A (`UrunEtiketleri.razor`), koşum sırasında düzenleniyordu, sonradan 65/65 yeşil.
   - Önceki listedeki GunKapanisi ×6 ve KasaKapatma ×3: S3-B (`Kasalar.razor`), bu koşumda yeşil.
   - AlisOdemeAraci ×1: yük altında kararsız, filtreli koşumda yeşil.

## 10. Kurucuya / mali müşavire not

Bu paket mevzuat hükmü içermiyor. Para üstü yalnız ekranda gösterilen bir bilgi: kasaya ve deftere fişin nakit tutarı yazılıyor, "alınan" tutarı hiçbir kayda girmiyor (D7 oyu, canlı adım 8). Mali müşavir teyidi gerekmiyor.
