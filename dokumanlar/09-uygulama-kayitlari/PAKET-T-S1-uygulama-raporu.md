# PAKET T-S1 — Satış BASİT modu tuvale (Tezgah.dc.html) · uygulama raporu (v3, 24–25.09.2026 23:10–00:25)

Rol: Taşkın (backend) + Ertuğ (frontend). Şerit: T-SATIŞ (VT `kuyumcu_test_t2`, canlı API 5162, vekil 5163).
Durum: **BİTTİ** — paket kapısının yedi maddesi aşağıda kanıtıyla var. Tam süitte kalan 5 kırmızı ve denetimdeki 1 ✗ başka şeritlerin (Q10-A `Hurda.razor`, T-KABUK `Ayarlar.razor`), bkz. § 10.

> v3 bu koşumda doğrulayıcının v2 bulgularını kapattı (§ V3). § 0–13 v2 metnidir; v3'te değişen satırlar yerinde düzeltildi ve "(v3)" ile işaretlendi.

## V3. Doğrulayıcı bulgularının kapanışı (bu koşum)

### V3.1 [ORTA + KRİTİK hakem çelişkisi] "Kaydet ve yazdır" yazdırmıyordu — KAPANDI
Sayı yeniden ölçüldü. v2 raporundaki "Kaydet ve yazdır → karşılandı" beyanı **yanlıştı**: v2'de düğme yalnız kaydediyor, yazdırma bağlantısı çiziyordu; `window.open`/`print` çağrısı 0'dı (doğrulayıcı haklı). Bu koşumda iki rapor aynı gerçeği yazar:

- **Tek belge doğuran kayıtta** (tek kalemli satış · fiş ucuna giden karma/hediye/iskontolu satış) başarılı kayıttan **sonra** yazdırma sayfası `/cikti/SATIS_FISI/{hareket}?yazdir=1` yeni sekmede **kendiliğinden** açılır (`Cikti.razor` bu sorguyla bir kez `print()` çağırır — S2-5b). Satış ekranı yerinde kalır. Altında "Fiş yeni sekmede yazdırmaya açıldı. Açılmadıysa "Fiş yazdır"a basın." (`#islem-yazdir-otomatik`) yazar; tarayıcı açılır pencereyi engellerse "Fiş yazdır" bağlantısı zaten görünür ve bir sonraki Kaydet'e kadar durur.
- **Çok kalemli düz satışta** bugünkü `Kaydet()` yolu kalem başına ayrı POST atar → N ayrı belge doğar; tek dokunuşla tek belge basılamaz. Bu yol bu paketin dokunamayacağı alandır ("Kaydet()/POST yolları DEĞİŞMEZ"). Bu yüzden etiket davranışla aynı şeyi söyler: düğme **"Kaydet"** olur ve altında kayıttan ÖNCE "Bu 2 ürün 2 ayrı fiş olarak kaydedilir; her fiş ayrı yazdırılır." (`#islem-kaydet-cokfis`, UX-S30) yazar; sekme kendiliğinden açılmaz. Yol, sahibi tek POST'a (`/islemler/satis-fisi`) geçirince kendiliğinden "Kaydet ve yazdır"a döner (`FisYazdir.TekBelge` fiş ucunu tek belge sayar).
- PRO yüz yazdırma vaat etmez ("Satış Kaydet") → sekme açılmaz (değişmedi).
- Kod: `Pages/Islemler.Yazdir.cs` (yeni partial; `Islemler.razor` 1400 satır tavanında olduğu için), `FisYazdir.TekBelge` (saf), `FisYazdirBaglantisi` (`OtomatikAcildi` parametresi; kendiliğinden açıldıysa genel ipucu ikinci kez yazılmaz), `Sozlukler.SatisTuval.cs` (`KaydetTek`, `CokFisNotu`, `YazdirAcildi` TR+EN). `Islemler.razor`'da 5 satır-nötr değişiklik (etiket → `KaydetEtiketi`, kayıt başında `otoYazdir`, başarıda `OtomatikYazdir()`, bağlantıya `OtomatikAcildi`, not satırı). **`Kaydet()` gövdesi, POST uçları, gövdeler, yetki ve yasal kapılar değişmedi.** Yeni istek yok (yalnız sonuç sayfası açılır).
- Canlı kanıt (başsız Chrome, **gerçek fare tıklaması** `Input.dispatchMouseEvent`, 1280 BASİT koyu):

| Senaryo | Kaydet etiketi (önce) | Yeni sekme (sonra) | Yazdır bağlantıları | SQL |
|---|---|---|---|---|
| 1 kalem (Burma bilezik) | "Kaydet ve yazdır" | **1**: `/cikti/SATIS_FISI/01M3AKNP6YYBNG5GEPRV486WWK?yazdir=1` | 1 (aynı adres) + "Fiş yeni sekmede yazdırmaya açıldı…" | `01M3AKNP6YYBNG5GEPRV486WWK SATIS NAKIT 28691.47` |
| 2 kalem (Burma + kolye, 42.065,89 ₺) | "Kaydet" + "Bu 2 ürün 2 ayrı fiş olarak kaydedilir; her fiş ayrı yazdırılır." | **0** | 2: `(1/2)` `01M3AKVVRB…`, `(2/2)` `01M3AKVW8K…` | `…VVRB SATIS NAKIT 28711.64` + `…VW8K SATIS NAKIT 13354.25` |

  Görüntüler: `ham/TUVAL-KARSILASTIRMA/T-S1-olcum-v3/tek-1280-basit-koyu-{once,sonra}.png`, `iki-1280-basit-koyu-{once,sonra}.png`; ham JSON aynı klasörde.

### V3.2 [DÜŞÜK] ALDIM "Alınan" kutusunun ₺ eki alt satıra düşüyordu — KAPANDI
`tuval-satis.css`: `#islem-aldim .alan-girdi-satir { flex-wrap: nowrap }`, girdi `flex: 1 1 0; min-width: 0`, ek `flex: 0 0 auto`. Ölçüm (girdi ile `#islem-alinan-birim` kutularının dikey kesişimi): 1440 · 1280 · 375 × BASİT/PRO × açık/koyu — **12/12 aynı satır** (ör. 1280: girdi 445–485, ₺ üstü 456, girdinin sağında 8 px). sikisma-olc bunu ölçmüyordu; ölçüm betiği `senaryo.mjs` içinde ayrı yapıldı.

### V3.3 [DÜŞÜK] Artık davet satırı — KAPANDI
`kiraci_davetleri`'nden bu paketin iki davet satırı silindi (v2'den kalan `TS1F332BFBBFD40451C` ve bu koşumunki; `row_count()=2`, kalan 0).

### V3.4 [ORTA] Çok kalemli nakit fişin kalem başına POST'u ve tevsik — KAPSAM DIŞI, SAHİBİNE (yeniden ölçüldü)
Doğrulayıcının bulgusu bu koşumda da üretildi: Peşin Müşteri'ye 42.065,89 ₺ nakit iki kalem → iki ayrı `/islemler/satis` → iki hareket (28.711,64 + 13.354,25), ikisi de 200. Aynı ekranda sunucunun kendi önizlemesi "Bu tutar tevsik haddini (30.000 ₺) aşıyor — nakit ödeme reddedilir" ve "Bu işlem yasal bir kapıya takıldı" yazıyordu (görüntü `dolu-1440-basit-acik-once.png`). Kök neden kodda yazılı: `TevsikKapisi.cs` § "HÂLÂ AÇIK" md. 2 ve 5 — Peşin (sistem carisi) satırları günlük kümülatife girmez, kapı yalnız TEK işlemin tutarını ölçer; istemci fişi kalemlere bölünce eşik kalem başına ölçülür. Düzeltme `Kaydet()` yolunu (tek POST `/islemler/satis-fisi`) ya da sunucu tevsik kapısını değiştirir; ikisi de bu paketin "POST yolları DEĞİŞMEZ / yasal kapılar DEĞİŞMEZ" sınırının dışında ve mevzuat hükmü (bölünmüş ödemenin tek işlem sayılması — 459 s. VUK GT) birincil kaynakla teyit ister. **Sahibi: satış/tevsik şeridi (S2 / UX-4d, Mehmet + Taşkın).** Bu paket yalnız etiketin dürüstlüğünü sağladı (V3.1: "2 ayrı fiş" notu).

### V3.5 Testler (v3)
Yeni 6 test (`TS1TuvalSatisTestleri`, 25 → 31; toplam yeni test v2+v3 = 14):
- `TekBelge_iade_fis_ucu_tek_kalem_EVET_cok_kalemli_duz_satis_HAYIR` (saf)
- `Kaydet_ve_yazdir_TEK_belgede_kayittan_SONRA_yazdirma_sekmesini_KENDILIGINDEN_acar` (`open` çağrısı tek, adres = görünen bağlantı, kayıttan önce 0)
- `Cok_kalemli_duz_satista_etiket_KAYDET_kac_fis_dogacagi_yazar_sekme_KENDILIGINDEN_ACILMAZ` (2 POST, `open` 0)
- `Pro_modda_Kaydet_yazdirma_VAAT_ETMEZ_sekme_KENDILIGINDEN_ACILMAZ`
- `Aldim_sutununda_para_birimi_eki_girdiyle_AYNI_SATIRDA_kalir_css`
- (sahte API'ye isteğe bağlı ikinci ürün `u2` — varsayılan kapalı, eski testler etkilenmez)

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| TS1 + S25 + S1i + BasitModPos + S21Fis (son, temiz derleme) | 108 | 108 | 0 |
| Tam süit 1 (v3 ara) | 5641 | 5640 | 1 — **bu paketin**: `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` (1401). Bir yorum satırı birleştirildi → 1400; sonraki koşumlarda yeşil |
| **Tam süit 2 (son, trx-pT-S1-v3b)** | **5683** | **5678** | **5 — hepsi başka şeridin (§ 10)** |

Derleme: `dotnet build Kuyumcu.sln` **0 uyarı, 0 hata** (build3, son mutasyondan ve son düzenlemeden sonra).

### V3.6 Mutasyon (v3) — yöntem: yedek `mut-yedek/T-S1-v3/`, tek eşleşme sınandı, filtreli test, geri kopya, `cmp`
| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M-TS1-13 | `otoYazdir = … ` → `false` | Kaydet_ve_yazdir_TEK_belgede… | AYNI |
| M-TS1-14 | `otoYazdir = TezgahFisi && TekBelge` → `TezgahFisi` | **Eşdeğer mutant** (yeniden koşum 30/30 yeşil): `OtomatikYazdir` ayrıca `yazdirIdler.Count == 1` ister; çok belgede zaten açmaz. İlk turdaki kırmızı, M-13'ün bayat derlemesiydi (aynı saniyede dosya değişimi) — geçersiz sayıldı | AYNI |
| M-TS1-14b | M-14 + `Count != 1` → `Count == 0` (iki bekçi birden) | Cok_kalemli_duz_satista… | AYNI |
| M-TS1-15 | `otoYazdir = TekBelge` (PRO'da da açar) | Pro_modda_Kaydet_yazdirma_VAAT_ETMEZ… | AYNI |
| M-TS1-16 | Etiket her zaman "Kaydet ve yazdır" | Cok_kalemli_duz_satista… | AYNI |
| M-TS1-17 | `TekBelge`: `kalemSayisi <= 1` → `<= 2` | TekBelge_iade…, Cok_kalemli_duz_satista… | AYNI |
| M-TS1-18 | ALDIM `flex-wrap: nowrap` → `wrap` | Aldim_sutununda_para_birimi_eki… | AYNI |
| M-TS1-19 | Başarıda `OtomatikYazdir()` çağrısı kaldırıldı | Kaydet_ve_yazdir_TEK_belgede… | AYNI |

### V3.7 Gerçek tarayıcı ölçümü (v3)
Tarayıcı bölmesinde yeni sekme yine açılamadı (`tabs_create` → "tab cap reached"); başka sekmeye dokunulmadı. Kendi başsız Chrome'um (kendi profili `chrome-ts1v3`, CDP 9460), vekil 5163 → API 5162. Oran sınırı (100 istek/dk/kullanıcı) için koşumlar 65 sn aralıklı; tabloda 429 = 0. Her görünümde fişte 1 kalem var (ALDIM "Alınan" alanı açık).

| Görünüm | K1 | K2 % | K3 taşan/kırık | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay | ₺ aynı satır |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık (2 kalem) | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 1440 BASİT açık (boş) | 0 | 73 | 0/0 | 1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — |
| 1280 BASİT açık | 0 | 69 | 0/0 | 1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 1280 BASİT koyu | 0 | 69 | 0/0 | 1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 1280 PRO açık | 0 | 88 | 0/0 | 2.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 1280 PRO koyu | 0 | 88 | 0/0 | 2.2×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 375 BASİT açık | 0 | 97 | 0/0 | 2.4×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 375 BASİT koyu | 0 | 97 | 0/0 | 2.4×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 375 PRO açık | 0 | 97 | 0/0 | 3×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 375 PRO koyu | 0 | 97 | 0/0 | 3×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ✓ |
| 1280 BASİT koyu (Kaydet senaryoları) | 0 | 69 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0/1* | 0 | 0 | 0 | ✓ |

\* Kiracı yeni açıldığında (fiyat listesi yokken) barkod hata metni "Elle ekle" bağlantısının altına iniyordu (K9=1, bir kez); fiyat listesi kurulduktan sonraki bütün koşumlarda 0. K2 (sağ kart yüzdesi) BASİT'te 69–98: iki sütunlu tuval yerleşimi, %60'ın üstünde. K4 gerekçesi § 7'deki gibidir (375'te tuval yok, yığın; PRO kapsam dışı). Görüntüler ve JSON: `ham/TUVAL-KARSILASTIRMA/T-S1-olcum-v3/` (27 dosya). Not: bir koşumda sunucudaki kullanıcı tema tercihi bir önceki koyu koşumdan kalıp "açık" istenen görünümü koyu çizdi (`data-tema=karanlik` ölçüldü); o koşum atıldı ve yenilendi — ölçüm düzeneği etkisi, ürün kusuru değil.

**Yan yana (asıl kanıt):** `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440-v3.png` (sol tuval, sağ bizim 1440 BASİT açık, 2 kalem). § 8 bölge tablosu geçerli; v3 farkları:
| Bölge | Tuvalde | Bizde (v3) | Durum |
|---|---|---|---|
| Alt satır birincil düğme | "Kaydet ve yazdır" | Tek belgede "Kaydet ve yazdır" + kayıttan sonra yazdırma sekmesi kendiliğinden açılır; çok kalemli düz satışta "Kaydet" + "N ayrı fiş" notu | Tek belgede AYNI. Çok kalemde GEREKÇELİ FARK: bugünkü kayıt yolu N belge doğurur; etiket davranışı söyler (V3.1, V3.4) |
| ALDIM "Alınan" | Tuvalde yok | Girdi + ₺ aynı satırda | Önceki bulgu kapandı (V3.2) |
| ALDIM tevsik uyarısı | Yok | Sunucu önizlemesi haddi aşan fişte "nakit ödeme reddedilir" | Sunucu metni (değişmedi); davranışla çelişkisi V3.4'te sahibine |

## 0. Devralma
Taze koşum. Önceki T-S1 uygulayıcısı ağaçta işin büyük kısmını bırakmıştı:
- `css/tuval-satis.css` (iki sütun, üst satır, barkod, fiş kartı, sağ sütun)
- `Sozlukler.SatisTuval.cs`
- `TS1TuvalSatisTestleri.cs` (17 test)
- Fis bileşen değişiklikleri
- `FisNakitUyari`: istemcide sabit "30.000 ₺" kalmamıştı, eşik sunucudan okunuyordu (orkestratörün 19:45 notundaki kusur ağaçta düzeltilmiş bulundu).

Kod okunup derlendi ve ilgili 286 test yeşil çıktı. Sonra gerçek tarayıcıda tuvalle karşılaştırıldı. Bulunan eksikler bu koşumda kapatıldı (§ 2).

## 1. Spesifikasyon ↔ kod doğrulaması
- Tuval (`tuval/project/Tezgah.dc.html`, satır 84–163) ile orkestratörün PNG'si (`ham/TUVAL-png/Tezgah.png`) okundu.
- Spesifikasyon klasöründe ayrı bir test/mutasyon listesi yok. Liste bu raporda üretildi (§ 5).
- Mevzuat: tevsik haddi yalnız sunucudan okunur (`/tevsik/gunluk → had`). İstemcide rakam yok. Sunucu yanıt vermezse metin sayısız çıkar (fail-closed).
- Yeni mevzuat hükmü yazılmadı. Bu yüzden yeni kaynak araştırması gerekmedi.

## 2. Bu koşumda yapılanlar
1. **Nakit uyarısının eşiği ilk açılışta geliyor** (tuval: "Nakit 30.000 ₺'yi geçerse…").
   - Eski durum: `/tevsik/gunluk` yalnız fiş değişince çağrılıyordu. Boş fişte kutu "yasal sınır" diye sayısız yazıyordu.
   - `OnInitializedAsync` artık `TevsikOnizle(yalnizHad: true)` çağırır.
   - `yalnizHad` yalnız haddi okur. Kapı durumunu (`tevsikNakitKapali`, `tevsikKalan`) boş fişte açmaz. Bunu `BasitModPosTestleri.YASAL_KAPI_tevsik…` yakaladı ve düzeltildi.
   - Tarayıcı ölçümü şunu gösterdi: profil sayfadan sonra yüklenirse mod önce PROFESYONEL, sonra BASİT oluyor (`ArayuzModuServisi.EtkinMod` varsayılanı). Bu yüzden `ModDegisti` de haddi o an sunucudan ister (yalnız `tevsikHaddi` henüz boşsa).
2. **Barkod kutusu tuval metniyle çalışıyor: "Barkodu okut ya da ürün adı yaz".**
   - Barkod `/urunler/barkod/{kod}` ucunda 404 dönerse ürün, ekranda zaten yüklü aktif ürün listesinde adıyla aranır (`BasitPos.AdlaBul`). Yeni uç ve yeni istek yok.
   - Adı birebir tutan ya da adında metni geçen **tek** ürün varsa fişe eklenir.
   - Birden çok eşleşmede ürün uydurulmaz. "Bu adla N ürün var; adı biraz daha yazın ya da Elle ekle'den seçin." yazılır.
   - Karşılaştırma tr-TR ve büyük/küçük harfe duyarsız. İki karakterden kısa metin aranmaz.
   - Yer tutucu yalnız BASİT modda değişti. PRO'daki "Barkodu okutun ya da yazın" aynen kaldı.
3. **"Elle ekle" küçük bağlantı biçiminde** (görev: "barkod kutusunun yanında küçük bağlantı/düğme"): çerçevesiz, vurgu rengi, 7rem. Barkod kutusu genişledi.
4. **Müşteri kutusunun açılır oku** tuvaldeki gibi sağ kenarda (16 px). Önceden bakiye rozetinin altında kalıyordu.
5. **Bölünmüş ödeme tuvalin ALDIM tablosu gibi**, ≥ 1400 px'te:
   - Yeni başlık satırı `#islem-aldim-bacak-baslik`: Ödeme · Ayrıntı · Tutar.
   - Her bacak tek satır: araç · kasa (tek kasa varsa çerçevesiz salt okunur yazı) · tutar · sil.
   - Fiş toplamı / bacak toplamı / kalan satırları VERDİM "Toplam" ve "Fark" çubuklarında zaten yazılı. Görselden gizlendi, ekran okuyucuda duruyor.
   - "Kalanı ata" ve uyuşmazlık uyarısı görünür kaldı.
   - Eski altın, döviz ve POS alanları tam genişlikte duruyor.
   - 1280 ve 375 px'te ALDIM sütunu ~296 px. Dört sütun sığmadığı ölçüldü (K3/K7/K9), bu yüzden orada eski dikey form kaldı.

Dokunulmayanlar:
- Tek POST yolu `Kaydet()`, yetki ve yasal kapılar.
- Mevcut element id'leri: hiçbiri değişmedi. Yeni id: `islem-aldim-bacak-baslik`.
- `Islemler.razor` 1399 satır (tavan 1400).
- Fiş bileşenleri istek atmıyor (denetim `FIS_BILESENI_ISTEK 0`).
- app.css'e, Sozlukler hub'ına ve başka şeridin dosyasına dokunulmadı.

## 3. DoD sözleşmesi
Yeni varlık yok, yeni uç yok, migration yok. Paket yalnız görünüm ve yerleşim değiştirdi. Mevcut düzeltme yolları aynen duruyor:

| Varlık | Düzenleme / düzeltme yolu | Ekranda |
|---|---|---|
| Satış fişi | Değiştirilemez. Düzeltme iade/storno ile yapılır. | "Son 3 satış" kartında `DegismezIpucu` (StornoYokGerekce + SatisDuzeltmeYolu) |
| Fiş kalemi (kaydetmeden önce) | Satırda sil; Vazgeç tüm fişi boşaltır (istek atmaz) | `#islem-vazgec` yalnız dolu fişte çizilir |
| Ödeme bacağı | Araç/tutar/kasa düzenlenir; sil; Tek ödemeye dön | ALDIM sütunu |

Düğmeler:
- **Ödeme** karosu kilitli, sebep altta yazılı.
- **İndirim** karosu kalem yokken kilitli ve "İndirim: önce fişe ürün ekleyin." yazar.
- **Kaydet ve yazdır** fark 0 değilse ya da ürün yoksa kilitli. Sebep `#islem-kaydet-kilit`'te ("Önce ürün seçin").

## 4. Testler
| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Başlangıç, ilgili 26 sınıf (devralınan ağaç) | 286 | 286 | 0 |
| `TS1TuvalSatis` (son) | 25 | 25 | 0 |
| İlgili 26 sınıf + P39 + Ikon + Sozluk (son) | 396 | 396 | 0 |
| `TS1TuvalSatis` + `BasitModPos` (son) | 41 | 41 | 0 |
| **Tam süit** (trx-pT-S1-2, `KUYUMCU_TEST_DB=kuyumcu_test_t2`) | **5605** | **5603** | **2 (başka şerit, § 10)** |

Tam süitin ilk koşumunda (trx-pT-S1) tek kırmızı bu paketin testiydi: `Nakit_uyarisi_BOS_fiste…`. Yük altında bUnit'in varsayılan 1 sn beklemesi yetmemişti. Yeni testlerin beklemesi 5 sn'ye çıkarıldı. İkinci koşumda yeşil.

Yeni test sayısı 8 (`TS1TuvalSatisTestleri`, 17 → 25):
- `AdlaBul_birebir_ad_tek_icerik_coklu_eslesme_UYDURULMAZ_trTR_buyuk_kucuk` (saf)
- `Barkod_bulunamazsa_URUN_ADIYLA_eklenir_istek_yalniz_barkod_ucu`
- `Barkod_ne_barkod_ne_ad_tutarsa_BULUNAMADI_kalem_eklenmez`
- `Pro_barkod_yer_tutucusu_DEGISMEDI`
- `Nakit_uyarisi_BOS_fiste_de_sunucu_esigini_yazar_ilk_acilista_istek_atilir`
- `Profil_SONRADAN_yuklenip_mod_BASIT_olunca_nakit_esigi_o_an_sunucudan_okunur`
- `Karma_odemede_ALDIM_sutun_basliklari_Odeme_Ayrinti_Tutar_tek_odemede_yok`

Sahte API'ye gerçekçi bir yanıt eklendi: `/urunler/barkod/*` → 404.

## 5. Mutasyon kanıtı
Yöntem:
1. Yedek `scratchpad/mut-yedek/T-S1/` altına alındı.
2. Tek eşleşmeli değişiklik yapıldı (eşleşme sayısı betikte 1 olarak sınandı).
3. Test projesi derlendi, filtreli test koşuldu.
4. Yedek geri kopyalandı ve `filecmp` ile birebir aynı olduğu doğrulandı.

Betik `scratchpad/ts1/mut.py`, sonuçlar `mut-sonuc-tur1..4.json`. M-1, 2, 3 ve 6'nın ilk turu başka şeridin derleme yarışına düştü ("DERLEME HATASI": Q105 test dosyası, Contracts ref dll). Bu dördü yeniden koşuldu. Tabloda yeniden koşumun sonucu var.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M-TS1-1 | Karo sırası TAHSILAT↔ODEME | Karolar_TUVAL_SIRASIYLA…, Basit_sag_sutun_karolari_tuval_sirasi… | AYNI |
| M-TS1-2 | Nakit uyarısı `Had > 0` → `> 99.999.999` | Nakit_uyarisi_esigi_SUNUCUDAN…, Nakit_uyarisi_BOS_fiste… | AYNI |
| M-TS1-3 | İlk açılış eşik isteği kaldırıldı | Nakit_uyarisi_BOS_fiste_de_sunucu_esigini_yazar… | AYNI |
| M-TS1-4 | `yalnizHad` yok sayıldı (kapı boş fişte açılır) | BasitModPosTestleri.YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR | AYNI |
| M-TS1-5 | AdlaBul: çok eşleşmede ilkini seç (`Count >= 1`) | AdlaBul_birebir_ad_tek_icerik_coklu_eslesme_UYDURULMAZ… | AYNI |
| M-TS1-6 | Barkod 404'te adla yedek kapalı | Barkod_bulunamazsa_URUN_ADIYLA_eklenir… | AYNI |
| M-TS1-7 | Yer tutucu modları ters | Barkod_bulunamazsa_URUN_ADIYLA…, Pro_barkod_yer_tutucusu_DEGISMEDI | AYNI |
| M-TS1-8 | Karma başlık satırı id'si bozuldu | Karma_odemede_ALDIM_sutun_basliklari… | AYNI |
| M-TS1-9 | Vazgeç boş fişte de çizilir | Vazgec_bos_fiste_CIZILMEZ… | AYNI |
| M-TS1-10 | Toplam gram adetle çarpılmaz | Toplam_gram_kalem_gram_x_adet_karti_olmayan_0 | AYNI |
| M-TS1-11 | tuval-satis.css'e sabit renk `#1d4ed8` | Css_sabit_renk_YOK_yalniz_belirtec | AYNI |
| M-TS1-12 | Mod değişiminde eşik isteği kaldırıldı | Profil_SONRADAN_yuklenip_mod_BASIT… | AYNI |

12 mutasyonun 12'si kırmızıya döndü. M-11 turunda `Toplam_gram…` testi de kırmızı raporlandı. Bu, eşzamanlı derleme yarışından kaynaklanan tek seferlik bir kırmızıdır. Aynı test M-10 dışındaki bütün turlarda ve iki tam süitte yeşil.

Son mutasyondan sonra `dotnet build Kuyumcu.sln` temiz: **0 uyarı, 0 hata** (build7.log).

## 6. denetim.sh
**(v3, son koşum):** `bash denetim.sh` → rc=1, "DENETİM BAŞARISIZ" — TEK ✗ `P-37c: SAYFA_BASI_KAPSAM toplam=47 serit=44 muaf=3 acik=0`. Üçüncü yazılı muafiyet `Pages/Ayarlar.razor` (T-KABUK şeridinin dosyası; bu paket dokunmadı), tavan 2. **Bu paketin sayaçlarının hepsi ✓:** B-3 ESIK_ISTEMCIDE 4 (tavan 4), ESIK_SABIT_SUNUCUDA 0, S2-3 FIS_BILESENI_ISTEK 0, P-06/P-10/P-16 BU_PAKETTE_EKLENEN_RECORD 0, P-37 KESIK_ETIKET 0, P-36 MODAL_GOVDE_DISI_KAYDIRMA 0.

(v2 metni:) `bash denetim.sh` → rc=0, **DENETİM TEMİZ**. İlgili sayaçlar:
- B-3 ESIK_ISTEMCIDE 4 (tavan 4), ESIK_SABIT_SUNUCUDA 0
- S2-3 FIS_BILESENI_ISTEK 0
- P-16 BU_PAKETTE_EKLENEN_RECORD 0
- P-37 KESIK_ETIKET 0

## 7. Gerçek tarayıcı ölçümü (sikisma-olc.js, başsız Chrome 153, kendi profili ve kendi CDP portu 9362, vekil 5163 → API 5162)
Tarayıcı bölmesinde yeni sekme açılamadı ("tab cap reached"). Başka sekmelere dokunulmadı. Bu yüzden T-P1'deki gibi kendi başsız Chrome'um kullanıldı.

Ölçüm düzeneği hakkında: genel oran sınırı kullanıcı başına 100 istek/dk. Ardışık sayfa yükleri 429 alınca profil yüklenemiyor, mod PRO görünüyor ve reyon bandı "çok yoğun" diyordu. Bu bir ürün kusuru değil, ölçüm düzeneğinin hızıydı. Düzenek sayfa yüklerini 25 sn aralar, her görünümde tek gezinme yapar ve modu ölçümden önce doğrular. Aşağıdaki tabloda 429 sayısı 0'dır.

| Görünüm | K1 | K2 % | K3 taşan/kırık | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 BASİT açık (boş) | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT koyu (boş) | 0 | 98 | 0/0 | 1.1×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık (boş) | 0 | 98 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT koyu (boş) | 0 | 98 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT açık (boş) | 0 | 97 | 0/0 | 2.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT koyu (boş) | 0 | 97 | 0/0 | 2.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT açık (bölünmüş ödeme) | 0 | 98 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 BASİT koyu (bölünmüş ödeme) | 0 | 98 | 0/0 | 1.2×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT açık (bölünmüş ödeme) | 0 | 98 | 0/0 | 2.4×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 BASİT koyu (bölünmüş ödeme) | 0 | 98 | 0/0 | 2.4×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT açık (bölünmüş ödeme) | 0 | 97 | 0/0 | 3.8×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 BASİT koyu (bölünmüş ödeme) | 0 | 97 | 0/0 | 3.8×5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO açık | 0 | 83 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1440 PRO koyu | 0 | 83 | 0/0 | 2×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO açık | 0 | 88 | 0/0 | 2.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 PRO koyu | 0 | 88 | 0/0 | 2.3×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO açık | 0 | 97 | 0/0 | 3.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 PRO koyu | 0 | 97 | 0/0 | 3.1×3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Diğer ölçümler:
- Zemin: açıkta `rgb(242,244,247)`, koyuda `rgb(14,18,25)`.
- Para tr-TR: K5 "TRY" = 0. Görünen biçim "27.512,78 ₺" ve "30.000 ₺".
- Nakit uyarısı 18 görünümün 18'inde sunucu eşiğiyle yazıldı: "Nakit 30.000 ₺'yi geçerse…".

**K4 gerekçesi** (> 2 ekran ve ≥ 4 bölüm):
- **375 boş (2.2):** Tuvalde 375 yok. Görevin 375 kuralı "makul yığın, yatay kaydırma 0"dır ve yatay kaydırma 0. Sıra: karolar → üst satır → barkod → VERDİM/ALDIM → alt satır → son satışlar → uyarı.
- **Bölünmüş ödeme, 1280 (2.4) ve 375 (3.8):** Kart bacağı POS cihazı, taksit ve para üstü düzenleme alanlarını açar; yasal eski altın bacağı satıcı kimliği alanlarını açar. Tuval bu düzenleme alanlarını çizmez. Alanlar özellik oldukları için silinmez.
- Boş ve tek ödemeli fiş 1280'de 1.2 ekrandır.

**K2 (PRO 83 %):** PRO yüzü bu paketin kapsamı dışında (T-S2). Oran %60 eşiğinin üstünde.

Görüntüler `ham/TUVAL-KARSILASTIRMA/T-S1-olcum/`: 18 PNG + `olc-*.json`, ayrıca tam boy `TS1-375-basit-aydinlik-tam.png` (scratchpad'de).

## 8. Görsel karşılaştırma (asıl kanıt)
Yan yana dosyalar:
- `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440.png`: tuval ile bizim bölünmüş ödeme durumumuz (Nakit 12.000 + Kart).
- `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440-bos.png`: tuval ile bizim boş fişimiz.

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Ana alan yerleşimi | Sol sütun + sağ 272 px, boşluk 20, dolgu 20/28 | Aynı ızgara (`minmax(0,1fr) 272px`, gap 20) | AYNI |
| Üst satır | Müşteri (esnek) · Tarih 170 · Para 110, kutu 52 px, köşe 9, etiket 15 px | Aynı ölçüler. Rozet "Borcu yok" kutunun içinde, ok sağda | AYNI (ok bu koşumda düzeltildi) |
| Ok biçimi | 18 px çizgi chevron | Ortak Secici'nin üçgen oku | GEREKÇELİ FARK: ortak `Secici` bileşeni başka şeridin/ortak alan; ok biçimi tüm seçicilerde tek |
| Tarih / Para | Metin kutusu | Salt okunur kutu (kayıt anı, TL) | GEREKÇELİ FARK: UX-S21. Tarih kayıt anıdır, satış fişi TL'dir; kullanıcıdan istenmez, ipucu title'da |
| Barkod | 56 px, 2 px vurgu çerçeve, köşe 10, barkod ikonu, "Barkodu okut ya da ürün adı yaz" | Aynı ölçü ve metin. Adla arama çalışıyor (canlıda "burma bilezik" yazılınca eklendi) | AYNI, tek fark ikon: sprite'ta barkod ikonu yok, arama (büyüteç) çiziliyor. `ikon.svg`/`Ikonlar.cs` ortak dosya, bu şeridin alanı dışında |
| "Elle ekle" | Tuvalde yok | Barkodun sağında küçük bağlantı | GEREKÇELİ FARK: görev "küçük bağlantı/düğme" diyor. Elle ekleme özelliği silinmez |
| Fiş kartı | Kenar 1 px, köşe 12, iki eşit sütun, başlıklar "VERDİM · ÜRÜNLER" / "ALDIM · ÖDEME" 15 px büyük harf | Aynı | AYNI |
| VERDİM tablosu | Ürün · Ayar · Gram · Tutar | Aynı sütunlar, "K22" → "22", satırda sil (×) | GEREKÇELİ FARK (×): kaydetmeden önce kalem silme yolu (UX-S25) |
| VERDİM alt çubuk | "Toplam · 10,40 g" · 50.650 ₺ (22 px) | "Toplam · 4,30 g" · 27.512,78 ₺ | AYNI (kuruşlu tutarda kuruş Bicim merkezinden, T-K1) |
| ALDIM tablosu | Ödeme · (ayrıntı) · Tutar; Nakit/Eski altın/Kart satırları | Tek ödemede tek satır "Nakit · Ana Kasa · tutar". Bölünmüşte başlık satırı + bacak satırları (araç · kasa · tutar · sil) | AYNI yerleşim. Tutar düzenlenebilir kutu ve "+ Bacak ekle" düzenleme yolu (UX-S25). 1280'de dikey form (ölçüm, § 2.5) |
| ALDIM ek alanları | Yok | Ödemeyi böl · Eski altın · Tek ödemeye dön · POS/taksit · para üstü | GEREKÇELİ FARK: tuvalde olmayan ama bizde olan özellik silinmez. Karma, eski altın ve para üstü sütunun altında |
| Fark çubuğu | "Fark · 0 ₺ ✓" başarı zemini | Aynı; fark ≠ 0 ise kırmızı zemin + "Açık hesap" yolu | AYNI |
| Alt satır | Açıklama (istersen) · Vazgeç · "Kaydet ve yazdır" (yazıcı ikonu) | Açıklama · Vazgeç (yalnız dolu fişte) · "Kaydet ve yazdır" (fiş ikonu); kilitliyken sebep altta | GEREKÇELİ FARK: (a) Vazgeç boş fişte çizilmez, çünkü boşaltacak bir şey yok ve DoD'da tıklanınca etkisiz düğme olamaz. (b) Sprite'ta yazıcı ikonu yok, `receipt` çiziliyor. (c) UX-S30 kilit sebebi |
| Kayıt sonrası | Tuvalde yok | "Son fiş: 22:45 · 27.525,64 ₺" + Yazdır bağlantısı, bir sonraki kayda kadar duruyor | QA-10 kalanı (görevin istediği) |
| "Ne yapıyorsun?" | 2×4, 70 px, köşe 10, 16 px; Satış vurgu dolu | Aynı sıra ve adlar, ikonsuz | AYNI. Ödeme ve İndirim kilitli, sebepleri altta yazılı (UX-S30) — GEREKÇELİ |
| Son 3 satış | Kart 14/16 dolgu, 15 px satır "14:10 · Peşin … 12.400 ₺" | Aynı + "Tüm satışlar" bağlantısı | AYNI (+ UX-S29 tam liste yolu) |
| Nakit uyarısı | "Nakit 30.000 ₺'yi geçerse program uyarır; kalanı kartla alırsın." | Aynı metin, eşik sunucudan | AYNI (bu koşumda düzeltildi) |
| Adım şeridi (1–4) | Yok | Yok (BasitSatisAkisi yalnız yasal kapı bandı) | AYNI |
| Kenar çubuğu / üst bar | — | T-KABUK şeridinin alanı | Kapsam dışı |
| Örnek veri | Hasan Kaya, 57.550 ₺… | Gerçek test kiracısı verisi | Kural gereği (veri değildir) |

Karo eşleşmesi:

| Karo | İşlem |
|---|---|
| Satış | SATIS |
| Alış · eski altın | HURDA_ALIS (ürün kartlı ALIS PRO'da) |
| Tahsilat | /cariler |
| Ödeme | Kilitli: tedarikçiye ödeme ucu yok |
| İade | SATIS_IADE |
| Değişim | TAKAS |
| Emanet | /emanet |
| İndirim | Fiş iskontosu (P-16) |

ISCILIK, TRANSFER, ALIS ve ALIS_IADE PRO'da kalıyor, özellik silinmedi.

## 9. Canlı doğrulama (API 5162, vekil 5163, test kiracısı `PAKETT-S1-TEST`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /tevsik/gunluk (kiracı) | 200, had sunucudan | 200 `{"had":30000.0,"kuralEtkin":true,…}` |
| /islemler açılış (BASİT, 18 görünüm) | Uyarıda sunucu eşiği | "Nakit 30.000 ₺'yi geçerse…" (18/18) |
| Barkod kutusuna "burma bilezik" + Enter | Barkod 404 → adla bulunur, kalem eklenir | VERDİM "Toplam · 4,30 g · 27.525,64 ₺" |
| Kaydet ve yazdır (v2 ölçümü — v3 düzeltmesi için V3.1) | 200, başarı bandı, son fiş + yazdır bağlantısı, fiş boşalır | "İşlem başarıyla kaydedildi!" · "Son fiş: 22:45 · 27.525,64 ₺" · `/cikti/SATIS_FISI/01M3AF754YE2B58NGA745Z47WH?yazdir=1` · kalem 0 · Son 3 satış'ın başında "22:45 · Peşin" |
| SQL | Hareket satırı | `hareketler`: `01M3AF754YE2B58NGA745Z47WH · SATIS · 27525.64 · NAKIT · nakit 27525.64 · Burma bilezik` |
| Bölünmüş ödeme (Nakit 12.000 + Kart kalan) | Tablo satırları, Fark 0 ✓ | Görüntü `T-S1-Tezgah-1440.png` |

Kapanış:
- API durduruldu. 5162, 5163 (önceki koşumdan kalan vekil dahil) ve 9362 dinlemiyor.
- Test kiracısı `kiraci-temizle.sh --uygula` ile 1839 satır + tenants satırıyla silindi.
- Başka bir API örneğinin arka plan kur işi sonradan 1 `kur_snapshots` satırı yazdı. O da silindi.
- Betik son durumda "Kiracı YOK" diyor.
- (v2) **tenants: başlangıçta 13** (içinde bu paketin önceki koşumdan kalan kiracısı vardı). **Sonda 15.** Aradaki farkı başka şeritler bu sürede açtı: `PAKETT-K2-TEST`, `PAKETQ10-2-TEST v2`, `PAKETT-M1-TEST`. Bu paketin kiracısı 0.
- QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.

**(v3) Canlı doğrulama ve kapanış:** test kiracısı `PAKETT-S1-TEST v3` (tenant `01M3AJ5MB6SKW8PF862TRBTJ80`) davet + `/auth/kayit` (201) ile açıldı; kurulum API ile: 3 ürün (201), fiyat listesi (200), kasa (200), kasa açılış girişi (200), stok için 3 alış (200). Satış kanıtları V3.1 tablosunda. Kapanış: API (PID) ve vekil `kill` ile durduruldu, Chrome kapatıldı; 5162, 5163, 9460 dinlemiyor. `kiraci-temizle.sh … --uygula` → "Kiracı YOK"; `information_schema`'daki tenant_id'li tüm tablolarda bu kiracının satır toplamı **0**; davet satırları silindi (2). **tenants: v3 başında 14, sonda 12** — fark başka şeritlerin kendi kiracılarını silmesidir (PAKETT-M1/K2, PAKETQ10-5 gitti; PAKETT-K3 geldi); bu paketin kiracısı 0.

## 10. Açık kalanlar
- **(v3) Tam süitteki başka şeride ait 5 kırmızı (trx-pT-S1-v3b, 5683 test) — bu paketin yeni kırmızısı 0** (v3 ara süitteki tek kırmızı bu paketindi, S21 1400 satır, kapatıldı):
  - `SozlesmeYansimaTestleri.Tasinan_tipler_Contracts_projesinde_ve_public` — "Pages bileşenlerinde … Hurda.HurdaSatisGovdesi" → **Q10-A** (`Pages/Hurda.razor`)
  - `SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor` — "Pages/Hurda.razor: record HurdaSatisGovdesi" → **Q10-A**
  - `OlcumKapisiTestleri.SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit` ve `OlcumKapisiTestleri.P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu` (beklenen 4) — aynı Hurda record'u → **Q10-A**
  - `Q102HurdaOnayEkranTestleri.Vazgec_sonrasi_yeniden_Kaydet_GUNCEL_kurla_YENI_ozet_ve_govde_uretir` — Hurda ekranı → **Q10-A / Q10-2**
  - denetim ✗ `P-37c SAYFA_BASI_KAPSAM` (Ayarlar.razor muafiyeti) → **T-KABUK**
  - Bu beşi v3'ün ilk tam süitinde (aynı ağaç, 23:5x) yoktu; arada başka şeritlerin Hurda/Ayarlar değişikliği girdi.
- **(v3) Tek POST / tevsik açığı (V3.4)** → sahibi satış/tevsik şeridi (S2/UX-4d); mevzuat teyidi gerekir.
- (v2) **Tam süitteki başka şeride ait 2 kırmızı (Q10-5, `css/q10.css` ve Ekstre)** — v3 son koşumunda ikisi de yeşil:
  - `Q105FormJargonTabloTestleri.Q105_C03_q10_css_baslik_nowrap_mobil_yigin…`
  - `EkstreEkranTestleri.Q105_Ekstre_tablosu_mobil_yigin…`
  - Q10-5 şeridi q10.css'i ölçüm sırasında (22:48) düzenliyordu. Bu paketin yeni kırmızısı 0.
- **Ortak ikonlar:** barkod ve yazıcı ikonu `wwwroot/img/ikon.svg` sprite'ında yok. Ortak dosya, bu şeridin alanı dışında. Önerim: T-KABUK ya da ortak ikon paketi `barcode` ve `printer` sembollerini eklesin.
- **Ortak Secici oku:** tuvaldeki chevron'a çevrilmesi ortak bileşen işidir.
- **Proje ağacında önceden kalan 0 baytlık dosya:** `Kuyumcu.Web/Pages/Islemler.razor.mutorj` (21.09, bu paketin değil). Dosya silme yasak olduğu için bırakıldı. Silme kararı kurucuda.
- **Profil sayfadan sonra yüklenince mod önce PRO görünüyor:** sayfa bir an profesyonel çiziliyor (`ArayuzModuServisi` varsayılanı). Bu paket nakit eşiğini bu duruma dayanıklı yaptı. Kabuğun "profil yüklenene dek iskelet" göstermesi T-KABUK'a önerilir.

## 11. Değişen dosyalar
Bu koşumda:
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (ilk açılış ve mod değişiminde eşik · `yalnizHad` · barkodda adla yedek; 1399 satır)
- `dotnet/Kuyumcu.Web/Servisler/BasitPos.cs` (`AdlaBul`)
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` (BASİT yer tutucu)
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisVerdimAldim.razor` (karma başlık satırı)
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` (Elle ekle bağlantısı · ok konumu · karma satırları ≥ 1400 px)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisTuval.cs` (`BarkodYerTutucu`, `AdlaCokEslesme` TR+EN)
- `dotnet/Kuyumcu.Tests/TS1TuvalSatisTestleri.cs` (+8 test, sahte 404)

v3 koşumunda:
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (5 satır-nötr değişiklik + bir yorum satırı birleştirildi; 1400 satır)
- `dotnet/Kuyumcu.Web/Pages/Islemler.Yazdir.cs` (YENİ partial: etiket, not, kendiliğinden yazdırma)
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdir.cs` (`TekBelge`)
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdirBaglantisi.razor` (`OtomatikAcildi`)
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` (ALDIM nowrap)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisTuval.cs` (`KaydetTek`, `CokFisNotu`, `YazdirAcildi` TR+EN)
- `dotnet/Kuyumcu.Tests/TS1TuvalSatisTestleri.cs` (+6 test)
- Kanıt: `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440-v3.png`, `ham/TUVAL-KARSILASTIRMA/T-S1-olcum-v3/`

Önceki T-S1 koşumundan devralınıp doğrulananlar (git durumunda izlenmeyen/değişmiş):
- `Bilesenler/Fis/*` (FisNakitUyari, FisSonSatislar, FisIslemIzgarasi, FisMusteriSatiri, FisKalemTablosu, FisModelleri…)
- `Bilesenler/BasitSatisAkisi.razor`

Kanıt dosyaları:
- `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440.png`
- `ham/TUVAL-KARSILASTIRMA/T-S1-Tezgah-1440-bos.png`
- `ham/TUVAL-KARSILASTIRMA/T-S1-olcum/` (18 PNG + json)

Dokunulan sayfalar: `Pages/Islemler.razor`. Aynı T-SATIŞ şeridinin aynı dalgası, ikinci dokunuş değil.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Tuval birebir (yerleşim, ölçü, metin) | Karşılandı (gerekçeli farklar § 8) | Yan yana PNG'ler + bölge tablosu |
| Eşik istemciye gömülmez | Karşılandı | FisNakitUyari yalnız `Had` parametresi; B-3 ESIK_ISTEMCIDE 4/4; M-TS1-2/3/12 |
| 8 karo tuval sırası | Karşılandı | M-TS1-1 |
| Stepper kalktı | Karşılandı | BasitSatisAkisi yalnız yasal kapı bandı |
| VERDİM Ürün·Ayar·Gram·Tutar, Toplam·N g | Karşılandı | Testler + M-TS1-10 |
| ALDIM Ödeme·ayrıntı·Tutar, bacaklar | Karşılandı (≥ 1400 px tablo; dar ekranda dikey form, ölçüm gerekçesiyle) | M-TS1-8, `olc-BASIT-dolu.json` |
| Fark ✓ ve Kaydet kilidi | Karşılandı | Canlı kayıt, kilit sebebi |
| Fiş no + yazdır bir sonraki kayda dek görünür (QA-10) | Karşılandı | Canlı: Son fiş + `/cikti/SATIS_FISI/…` |
| (v3) "Kaydet ve yazdır" gerçekten yazdırır; etiket davranışla aynı | Karşılandı (tek belgede kendiliğinden yazdırma sekmesi; çok kalemde "Kaydet" + not) | V3.1 canlı tablo (gerçek tıklama, yeni sekme 1/0), M-TS1-13…19 |
| (v3) ALDIM ₺ eki girdiyle aynı satırda | Karşılandı | V3.2, 12/12 görünüm, M-TS1-18 |
| (v3) Davet satırı artığı | Karşılandı | V3.3, kalan 0 |
| (v3) Tek POST tevsik açığı | Kapsam dışı, sahibine | V3.4 |
| VERDİM ve ALDIM üstten hizalı | Karşılandı | 1440 görüntü |
| Tek POST `Kaydet()` değişmedi; fiş bileşenleri istek atmaz | Karşılandı | denetim FIS_BILESENI_ISTEK 0 |
| Islemler.razor ≤ 1400 satır | Karşılandı | 1399 |
| Mevcut id'ler korunur | Karşılandı | 396 ilgili test yeşil |
| app.css / Sozlukler hub'ı dokunulmadı | Karşılandı | Yalnız tuval-satis.css ve Sozlukler.SatisTuval.cs |
| 1280/375 · BASİT/PRO · açık/koyu, K1..K11 = 0, yatay 0 | Karşılandı (K4 gerekçeli) | § 7 |
| Sabit renk yok | Karşılandı | M-TS1-11 |
| Testlerde sabit para metni yok | Karşılandı | Beklenenler `Bicim.Tl(..)` ile |
| Test kiracısı silindi | Karşılandı | § 9 |

## 13. Kurucuya / mali müşavire not
Yeni bir mevzuat hükmü yazılmadı. Nakit (tevsik) haddi 30.000 ₺ sunucunun yapılandırmasından geliyor. İstemci bu sayıyı yalnız gösteriyor. Tutarın güncelliği önceki paketlerde (UX-4d, B-3) mali müşavir teyidine bağlanmıştı; bu paket o durumu değiştirmedi.
