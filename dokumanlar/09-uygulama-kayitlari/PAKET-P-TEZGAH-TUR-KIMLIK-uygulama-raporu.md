# PAKET P-TEZGAH-TUR-KIMLIK: uygulama raporu (TUR 2 — önceki doğrulamanın bulguları kapatıldı)

> Tur 1 raporu aşağıda (§ "TUR 1") tarihçe olarak AYNEN duruyor. Bu bölüm tur 2'nin TEK DOĞRU kaydıdır. Şerit t9 · API 5188 · vekil 5189 · CDP 9983 · test DB `kuyumcu_test_t9`. COMMIT YOK. Sunucu kuralları (MASAK 200k/370k, nakit 30k) DEĞİŞMEDİ — yalnız ekran.

## T2.1 Bulgu → yapılan (dosya)

| # | Bulgu (önem) | Yapılan | Dosya |
|---|---|---|---|
| 1 | Yıldız kimlik çekmecesinde birincil "Tamam" yarım yay, Kapat'tan zayıf (KRİTİK + ORTA) | Çekmece altlığında birincil düğme tuval .b1: dolu hap (açıkta `var(--metin)` koyu, koyuda açık), `border:0`, radius 999px, 44 px, yazı ortalı; Kapat .b2 alt çizgili kalır. Genel `.yz .cekmece-alt .dugme` ile `.ana-icerik .dugme-birincil !important` çakışması bu kuralla tek görünüme bağlandı. Ölçüm: 20/20 Yıldız bileşiminde `dolu=evet` (açık rgb(20,20,20), koyu rgb(243,242,239)), görüntü `foto/o-yildiz-aydinlik-PROFESYONEL-1440-kimlik.png`. | `tema-yildiz.css` TUR-KIMLIK (4) |
| 2 | Yıldız Alış/İade'de Kaydet kilitli, nedeni 1 px `gorunmez` (KRİTİK) | `KilitGorunur => FisDolu \|\| tur != "SATIS"`: Satış dışı türlerde kilit cümlesi boş açılışta da görünür (Satış'ta boş fiş eskisi gibi sessiz — "barkodu okut" satırı aynı şeyi söyler). Yıldız'da cümle Kaydet'in SOLUNDA aynı satırda (`order:0`, 15 px, `--metin-soluk`); 1366×768'de alta itilip ekran dışına düşmüyordu artık (önce kilit y=750–770 > 768, sonra y=704). | `Islemler.Odeme.cs`, `tema-yildiz.css` (5) |
| 3 | Yıldız PRO'da Alış eski form (KRİTİK) | Yıldız'da "Alış" sekmesi İKİ MODDA da müşteriden altın alışı (HURDA_ALIS — BASİT ile aynı ekran). Toptancıdan ürün alışı (ALIS) "Diğer"de kalır (özellik silinmedi); seçilince sekmede "Ürün alışı" adıyla görünür (iki "Alış" olmaz). Yıldız öteki tür yüzünde PRO'ya özgü eski satırlar çizilmez: sayfa açıklama satırı ("Satış, alış, hurda alışı …"), "Kısayollar F2/F4/F8/F9" (tuşlar çalışır), "Pasif cariler …" notu. Ölçüm: Alış 20/20 eski öğe 0 · birincil 1 · "Cari" 0 · Kısayollar 0 · açıklama 0. | `FisTurSecici.razor`, `Sozlukler.TurKimlik.cs` (UrunAlisi TR+EN), `tema-yildiz.css` (6) |
| 4 | PRO kimlik çekmecesinde "MASAK … mali müşavir teyidi bekleniyor" paragrafı (KRİTİK) | Yıldız tezgâhında `MasakFormu KuralGizli="@YildizTezgahMi"` → `#masak-kural` iki modda da çizilmez (BASİT zaten çizmiyordu; Cariler'de zaten KuralGizli=true). Eşik başlıkta tek kez kalır. Ölçüm: 20/20 `#masak-kural yok`, "müşavir/teyid" 0. | `Islemler.razor` (tek öznitelik, satır sayısı 1399 AYNEN, Toplu5 imza dizgesi korunur) |
| 5 | Y2: müşteri listesi iç kutu, alttaki yazı yarıdan kesik (ORTA) | 50vh tavanı kalktı; ≥1001 px'te katman açıkken barkod alanı, kalem listesi ve boş fiş yazısı çizilmez (seçimle geri gelir), katman Toplam'a kadar tüm boşluğu kullanır, Toplam tam görünür. Ölçüm 1366×768: katman 251→412 px, kesik alt yazı 1→0, kesik kalem satırı 0, Toplam tam; 1440×900'de iç kaydırma YOK. **KISMEN:** tuval satırı ~82 px olduğundan 768 px boyda 5 müşteride katman hâlâ kendi içinde kayar (1366: 1 satır, 1024: 2 satır kaydırmayla). Satır yoğunluğu tuval kararı → toplu kuyruk (O9 ile). | `tema-yildiz.css` (7) |
| 6 | O10 "Ürünü seçip fiyatı elle yaz" (DÜŞÜK) | "Ürün seç, fiyatı kendin yaz" / EN "Pick a product, enter the price yourself"; SATIS3 T13 yeni cümleye çekildi ("olmayan" demez beklentisi korunur). | `Sozlukler.Satis3.cs`, `SATIS3EkranTestleri.cs` |
| 7 | Alış PRO'da "Cari seçin..." (DÜŞÜK) | Etiket "Müşteri" olan yerde (BASİT ya da PRO tuval) yer tutucu `BasitDilSozlugu.Cari_CariSecin` ("Müşteri seçin..."); Yıldız'da kilit cümlesi de "Müşteri seçin" (`CariSecinKilit`). Platin PRO muhasebe dili (D10) AYNEN. | `FisMusteriSatiri.razor`, `Islemler.TurKimlik.cs`, `Islemler.razor` (aynı satırda tek ifade) |
| 8 | İlk eksik alana odak belge türü/uyruğu atlıyor (DÜŞÜK) | `IlkEksikKimlikAlani` `MasakEksikAlanlari` ile birebir sıra: belge türü → belge no → ad soyad → uyruk → amaç → açıklama → fon → ek belge. Uyruk geçersizse MasakFormu (EksikGoster açıkken) "Diğer bilgiler" katlanırını bir kez açar ve açık tutar. | `Islemler.TurKimlik.cs`, `MasakFormu.razor` |
| 9 | SATIS3T24 ölü değişken (DÜŞÜK) | Ölü değişken silindi; `EksikKilit(title, belgeNoAdi)` — beklenen alan adı formdaki etiketle aynı kaynaktan (`MasakFormu.BelgeNoEtiketiFor`). | `SATIS3T24YasalMesajTestleri.cs` |

**Spesifikasyon sapması (ekip kararı, gerekçeli):** tur 1'de PRO "Alış" = ürün alışıydı; hakem "BASİT'ten farklı ekran + eski form" dedi. Tezgâhtar dilinde "alış" müşteriden altın almaktır; toptancı alışı satınalma işidir → Yıldız'da iki modda tek anlam. Platin'e dokunulmadı (Platin'de sekme yok, özgün ızgara aynen).
**Mevzuat:** yeni hüküm/eşik yazılmadı. "mali müşavir teyidi bekleniyor" ibaresi sözlükte (`YasalParamSozlugu.KimlikKurali`, TEYİTSİZ damgası) DURUYOR — yalnız tezgâh ekranında gösterilmiyor; Yasal sınırlar (yönetici) ekranında görünmeye devam eder (bilinçli: teyit bekleyen kural orada izlenir).

## T2.2 DoD sözleşmesi
Yeni varlık/uç/migration YOK. Yeni düğme yok (sekme etiketi değişti); Tamam gerçek (TK_01, TK_12), kilitli Kaydet sebebi görünür (TK_11). ModalPencere artışı 0.

## T2.3 Testler
- Yeni (tur 2, 7 koşum): TK_10 ×2 (MASAK paragrafı/müşavir yok), TK_11 ×2 (Alış boş açılışta kilit görünür + Cari jargonu yok), TK_12 (odak sırası + uyruk katlanırı), TK_13 (CSS: Tamam dolu, kilit yanında, katman, eski satırlar), TK_14 (Ürün alışı Diğer'den, iki Alış yok). Güncellenen: TK_05 PRO → HURDA_ALIS, SATIS3 `Hurda_KILIT_SEBEBI_bos_acilista_da_GORUNUR` (eski "boş açılışta GİZLİ" kuralı paket kuralıyla ters — gerekçe hakem KRİTİK), SATIS3 T13, SATIS3T24.
- Paket filtresi `PTezgahTurKimlik`: **19/19** yeşil.
- İlgili geniş filtre (PTezgahTurKimlik·SATIS3·YasalKapi·YasalParam·Toplu5·ParaDemo·Yildiz·Masak·MASAK·S24·L1·DegisimIki·CumaS·S25·UX1C1·TS1·S21·S3d·Satis4·ZoIslem·IslemBant·Takas·Hurda·Iade): **2807/2809 başarılı, 0 başarısız, 2 atlanan**.
- TAM SÜİT (tur başına 1, son kodla, trx `/private/tmp/kuyumcu-serit/trx-pP-TEZGAH-TUR-KIMLIK-t2`): **12638 toplam · 12632 başarılı · 3 başarısız · 3 atlanan.** 3 kırmızının üçü de başkasına ait ve tur 1 listesinde zaten var (FARK = 0, yeni kırmızı yok): `FaturaEkranTestleri.Y4C_K12(basit: True)` (rastgele GUID içinde "805" — kararsız, Faturalar), `RaporDortTestleri.RD_KH01`, `RD_KH02` (RAPOR-DORT, tarihe bağlı).
- Ara koşumda görülen `YildizKatmanTestleri.YK_42/YK_43` (t1 YILDIZ-KATMAN'ın eşzamanlı `YildizVazgec` düzenlemesi) ve `YK3_04` (bUnit olay kimliği yarışı) tek başına yeniden koşulunca yeşil; tam süitte de yeşil.
- Build: `dotnet build Kuyumcu.sln` **0 hata · 0 uyarı** (son temiz build, mutasyonlardan sonra).

## T2.4 Mutasyon (yedek `/private/tmp/kuyumcu-serit/mut-yedek/P-TEZGAH-TUR-KIMLIK/`, `perl -0777 -pi` tek eşleşme → filtreli test → geri → cmp)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M2-1 | CSS Tamam `background: var(--metin)` → `transparent` | TK_13 | AYNI |
| M2-2 | `KilitGorunur` → yalnız `FisDolu` | SATIS3 Hurda_KILIT_SEBEBI ×2, TK_11 ×2 | AYNI |
| M2-3 | `KuralGizli="@YildizTezgahMi"` kaldırıldı | TK_10 (PRO) | AYNI |
| M2-4 | odakta uyruk satırı kaldırıldı | TK_12 | AYNI |
| M2-5 | `AnaTurler` moda bağlı eski hâl | TK_05 (PRO), TK_14, TK_11 (PRO) | AYNI |
| M2-6 | `CariSecinKilit` Yıldız dalı kapatıldı | TK_11 ×2 | AYNI |
| M2-7 | `uyrukZorlaAcik = false` | TK_12 | AYNI |
| M2-8 | Ürün alışı etiketi kaldırıldı | TK_14 | AYNI |
| M2-9 | CSS kilit `order: 0` → `99` | TK_13 | AYNI |

9/9 kırmızı, 9/9 cmp AYNI (ham: `mut-sonuc3.txt`). Betikteki artık "M1-5b DEGISMEDI" satırı eski betikten kopyalanan eşleşmesiz satırdır (dosya değişmedi).

## T2.5 denetim.sh
`DENETİM BAŞARISIZ` — yalnız iki bilinen, başkasına ait: `P-30 DEMO_INSAN_SURESI` (kapı dışı) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK-ESZAMAN). Bu paketin sayaçları ✓ (315 ✓ satırı).

## T2.6 Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9983, tek profil `prof`, önbellek kapalı; ölçer `betikler/sikisma-olc.js` + paket ölçeri `olcum2.mjs`)
Senaryo: Tezgâh → (Yıldız) "Değiştir" katmanı ölçümü → Burma Bilezik 3×90.000 = 270.000 ₺ → Mehmet Demir → Kart → "+ Kimlik bilgisi" → boş Tamam → "Ayşegül Karaosmanoğlu" → amaç+beyan → Tamam → Vazgeç → **Alış** sekmesi (iki modda HURDA_ALIS) → Cariler Tahsilat 250.000 Kart. 2 şema × 2 tema × 2 mod × 5 genişlik = **40 bileşim**, hata 0.

| şema · tema · mod | gen. | tür seçici | Kaydet sebebi görünür/ekranda | birincil ekran·çekmece | zorunlu ilk ekran·işaretli | Tamam dolu (bg) | MASAK paragrafı/müşavir | boş Tamam→hata (çekmece) / dolu→ | Ad Soyad kırpma | Alış: kilit cümlesi görünür·ekranda | Alış eski·birincil·Cari·Kısayol·açıklama | Y2 katman px·iç kaydırma·Toplam tam·kesik alt yazı | Tahsilat zorunlu·birincil | sıkışma (K9 hariç) fiş/çekmece/alış/tahsilat · yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| yildiz · karanlik · BASIT | 1024 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 368·evet·evet·0 | 4/5·1 | 1/3/0/0 · 0 |
| yildiz · karanlik · BASIT | 1280 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 444·evet·evet·0 | 4/5·1 | 0/2/0/0 · 0 |
| yildiz · karanlik · BASIT | 1366 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 412·evet·evet·0 | 4/5·1 | 0/2/0/0 · 0 |
| yildiz · karanlik · BASIT | 1440 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 508·hayır·evet·0 | 5/5·1 | 0/2/0/0 · 0 |
| yildiz · karanlik · BASIT | 375 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 524·hayır·HAYIR·1 | 3/5·1 | 0/2/0/0 · 0 |
| yildiz · aydinlik · BASIT | 1024 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 368·evet·evet·0 | 4/5·1 | 1/3/0/0 · 0 |
| yildiz · aydinlik · BASIT | 1280 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 444·evet·evet·0 | 4/5·1 | 0/2/0/0 · 0 |
| yildiz · aydinlik · BASIT | 1366 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 412·evet·evet·0 | 4/5·1 | 0/2/0/0 · 0 |
| yildiz · aydinlik · BASIT | 1440 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 508·hayır·evet·0 | 5/5·1 | 0/2/0/0 · 0 |
| yildiz · aydinlik · BASIT | 375 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 524·hayır·HAYIR·1 | 3/5·1 | 0/2/0/0 · 0 |
| yildiz · karanlik · PROFESYONEL | 1024 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 368·evet·evet·0 | 4/5·1 | 1/1/0/0 · 0 |
| yildiz · karanlik · PROFESYONEL | 1280 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 444·evet·evet·0 | 4/5·1 | 0/0/0/0 · 0 |
| yildiz · karanlik · PROFESYONEL | 1366 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 412·evet·evet·0 | 4/5·1 | 0/0/0/0 · 0 |
| yildiz · karanlik · PROFESYONEL | 1440 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 544·evet·evet·0 | 5/5·1 | 0/0/0/0 · 0 |
| yildiz · karanlik · PROFESYONEL | 375 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(243, 242, 239)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·hayır ('Müşteri seçin') | 0·1·0·0·0 | 586·hayır·HAYIR·1 | 3/5·1 | 0/2/0/0 · 0 |
| yildiz · aydinlik · PROFESYONEL | 1024 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 368·evet·evet·0 | 4/5·1 | 1/1/0/0 · 0 |
| yildiz · aydinlik · PROFESYONEL | 1280 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 444·evet·evet·0 | 4/5·1 | 0/0/0/0 · 0 |
| yildiz · aydinlik · PROFESYONEL | 1366 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 412·evet·evet·0 | 4/5·1 | 0/0/0/0 · 0 |
| yildiz · aydinlik · PROFESYONEL | 1440 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·evet ('Müşteri seçin') | 0·1·0·0·0 | 544·evet·evet·0 | 5/5·1 | 0/0/0/0 · 0 |
| yildiz · aydinlik · PROFESYONEL | 375 | gorunur  | evet/evet | 1·1 | 5/5·5/5 | evet (rgb(20, 20, 20)) | yok/yok | 2 (açık) / kapandı, Kaydet açık | 0 | evet·hayır ('Müşteri seçin') | 0·1·0·0·0 | 586·hayır·HAYIR·1 | 3/5·1 | 0/2/0/0 · 0 |
| platin · aydinlik · BASIT | 1024 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · BASIT | 1280 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · BASIT | 1366 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · BASIT | 1440 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · BASIT | 375 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · PROFESYONEL | 1024 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · PROFESYONEL | 1280 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · PROFESYONEL | 1366 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · PROFESYONEL | 1440 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · PROFESYONEL | 375 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · PROFESYONEL | 1024 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · PROFESYONEL | 1280 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · PROFESYONEL | 1366 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · PROFESYONEL | 1440 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · aydinlik · PROFESYONEL | 375 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · BASIT | 1024 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · BASIT | 1280 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · BASIT | 1366 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · BASIT | 1440 | gorunur  | evet/evet | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 5/5·1 | 0/—/—/0 · 0 |
| platin · karanlik · BASIT | 375 | gorunur  | evet/hayır | 1·— | 5/5·5/5 | — | — | — | 0 | — | — | — | 4/5·1 | 0/—/—/0 · 0 |

Notlar:
- **Tahsilat "4/5" sütunu ölçer kusurudur:** `olcum2.mjs` çekmece gövdesini ölçmeden önce `scrollTo(0,0)` yapıyor ve YK3'ün formu görünür bölgeye kaydırmasını geri alıyor (tur 1 `olcum.jsonl`'da da aynı: `aciklama` ekran dışı). Gerçek kullanıcı akışında (kaydırma sıfırlanmadan, `t2-th.mjs`): Yıldız BASİT ve Platin PRO × 1024/1280/1366/1440/375 = **10/10 bileşimde 5/5 zorunlu + Kaydet ilk ekranda**. (Tur 1 raporundaki "tahsilat 40/40 5/5" ifadesi bu kusur yüzünden ham veriyle uyuşmuyordu — Madde 1 gereği düzeltme olarak yazıyorum.)
- **Y2 375 satırları** ("Toplam HAYIR · kesik alt yazı 1"): 375'te sayfa bütün olarak kayar (sütun kutusu yok); Toplam ve boş fiş yazısı ekranın altında, kesik değil — ölçüt yalnız ≥1001 px için anlamlı.
- **Alış kilit cümlesi 375 PRO "ekranda hayır":** PRO Alış formu 375'te uzun, Kaydet + yanındaki cümle kaydırınca görünür (Y5 P-FORM-GORUNURLUK konusu); cümle görünür (gorunmez değil).
- Platin "Kaydet sebebi ekranda hayır" (375 ve PRO 1024): tur 1'deki ile aynı, Y5 konusu.
- Sıkışma: Yıldız çekmece 2–3 = K15 (çekmece başlığı `position:fixed`, ölçer yanılgısı — t1) + 1024 K3_kirik `#yildiz-tezgah-toplam-ayrinti` (D3); Alış yüzü 0; yatay 0.
- `tuval-kiyas.mjs` Tezgâh 1440: koyu 36 öğe AYNI 35 FARK 1, açık AYNI 35 FARK 1 — tek FARK `ödeme1 kasa` satır yüksekliği (t7 alanı, tur 1 ile aynı). `yildiz-tipo-tara.mjs /islemler` BASİT+PRO × 1440+375 × koyu+açık: KURAL DIŞI 0, yatay 0, kesik yazı 3 (tur 1 ile aynı: ekran okuyucu etiketi + t7 kasa).
- Sekme yazılarında İ/Ğ: 1440 tam ekran görüntüsünde küçültme yüzünden noktasız görünüyordu; 3× yakın çekimde (`foto/t2-zoom-sekme.png`) "İADE · TAHSİLAT · DİĞER" doğru (DOM metni de doğru).
- **Read ile bakılan 4 görüntü:** `o-yildiz-aydinlik-PROFESYONEL-1440-kimlik.png` (Tamam dolu hap, MASAK paragrafı yok, alan altı hatalar), `o-yildiz-karanlik-PROFESYONEL-1366-alis.png` (PRO Alış = tuval formu, "Müşteri seçin" kilidi Kaydet'in solunda, Kısayollar/açıklama yok), `t2-musteri-c-1366.png` (ara hâl: kalem satırı yarıdan kesik görüldü → kalem listesi de katman açıkken gizlendi, yeniden ölçüldü: kalemKesik 0), `t2-zoom-sekme.png`.

## T2.7 Canlı doğrulama (API 5188, kiracı `PAKETP-TEZGAH-TUR-KIMLIK-TEST 932d52`) — sunucu kuralları aynen
| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST /islemler/satis 3×90.000 KART, kimliksiz | 400 MASAK | 400 "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · …" |
| 2 | kimlik var, amaç/beyan yok | 400 | 400 "İşlem amacı belirtilmelidir · … en az 20 karakter … (şu an 0)" |
| 3 | beyan 4 karakter | 400 | 400 "… en az 20 karakter olmalıdır (şu an 4)" |
| 4 | eksiksiz | 200 | 200 |
| 5 | NAKİT 30.000,01 | 400 NAKIT_SINIRI | 400 `NAKIT_SINIRI` "İşlemin toplamı 30.000,01 TL, 30.000,00 TL nakit sınırını aşıyor; …" |
| 6 | NAKİT 29.999,99 | 200 | 200 |

SQL (kişisel veri basılmadan): önce `masak_kimlik_tespitleri|masak_beyanlari|hareketler` = 0|0|6 → sonra 1|1|8; son tespit: `TC_KIMLIK · 270000.00 · HAREKET · hareket_id dolu · hash var`.
**Temizlik:** `zsh betikler/kiraci-temizle.sh 01M4M8VABPZ3SHWWHE5N5DACHE --uygula` → 75 tablo, 923 satır + tenants 1 SİLİNDİ; davet satırı ayrıca silindi (kalan 0); bu kiracı tenants'ta 0. tenants: başlangıç 22 → bitiş 20 (bu paketin kiracısı açılıp silindi; aradaki −2 eşzamanlı şeritlerin kendi temizliği — benim silme listemde yalnız kendi kiracım). API (PID 10183) ve vekil kendi PID'leriyle durduruldu, Chrome profili kapatıldı; 5188/5189/9983 dinleyen 0. 5199/5200/5230/5241'e dokunulmadı.

## T2.8 Koşul karşılama (tur 2)
| Koşul | Durum | Kanıt |
|---|---|---|
| 200k+ tüm zorunlular ilk ekranda + işaretli | KARŞILANDI | Yıldız çekmece 20/20 5/5·5/5; Platin fiş 20/20; tahsilat gerçek akışta 10/10 5/5 (T2.6 not) |
| Tamam çalışır ve birincil görünür | KARŞILANDI | 20/20 boş→2 alan altı hata (açık), dolu→kapandı + Kaydet açık; dolu hap 20/20; TK_01/TK_12; M2-1 |
| Kaydet kilitliyse sebebi yanında | KARŞILANDI | Satış 40/40; Alış 20/20 görünür (1366'da ekranda); TK_11; M2-2/M2-9 |
| İşlem türü seçici görünür, tek tık | KARŞILANDI | 40/40; TK_05/TK_14 |
| Alış Yıldız'da eski öğe 0 (iki mod) | KARŞILANDI | 20/20 eski 0, Cari 0, Kısayollar 0, açıklama 0; M2-5 |
| Birincil düğme 1 | KARŞILANDI | ekran 40/40, çekmece 20/20, Alış 20/20, tahsilat 40/40 |
| Ad Soyad kırpma 0 | KARŞILANDI | 40/40 + tahsilat |
| PRO çekmecede MASAK/müşavir paragrafı yok | KARŞILANDI | 20/20; TK_10; M2-3 |
| Y2 müşteri listesi iç kutu değil | **KISMEN** | kesik alt yazı 0, Toplam tam, 1440×900 iç kaydırma yok; 768 boyda 5 müşteride katman kendi içinde kayar (toplu kuyruk O9) |
| O10 iç dil | KARŞILANDI | iki cümle de düz Türkçe; T13/TK_08 |
| Odak sırası = eksik listesi | KARŞILANDI | TK_12; M2-4/M2-7 |
| Canlı SQL aynı | KARŞILANDI | T2.7 |
| Mutasyon ≥4 kırmızı, cmp AYNI | KARŞILANDI | 9/9 |
| build 0/0 | KARŞILANDI | 0 hata 0 uyarı |
| denetim TEMİZ (P-30 hariç) | KARŞILANMADI (başkasının) | GIRDI-2 MASAK-ESZAMAN fikstürü |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 3 kırmızı, hepsi tur 1 listesinde, başkasının |
| tuval-kiyas Tezgâh FARK korunur | KARŞILANDI | FARK 1 = t7 kasa satırı (tur 1 ile aynı) |

## T2.9 Açık kalanlar (sahipleriyle)
- **Y2 kalan (toplu kuyruk, O9 ile):** 768 px boyda tuval satırı (~82 px) yüzünden 5+ müşteride katman içi kaydırma; çözüm satır yoğunluğu ya da katmanın sağ sütunu da örtmesi — tuval kararı (tasarım hakemi + kurucu).
- **t7 YILDIZ-TEZGAH-ODEME:** `ödeme1 kasa` satır yüksekliği FARK 1; 375 `.yz-tezgah-kasa` kesik.
- **t1 YILDIZ-KATMAN:** çekmece başlığı K15 ölçer yanılgısı; bu paketin Tamam kuralı `.yz .cekmece-alt .dugme.dugme-birincil` — t1 Yıldız Cekmece varyantını yazarken bu kuralı devralmalı (çakışma riski orkestratöre).
- **Kabuk (Y17):** öteki türlerde sayfa başı "Hurda Alış 04:50 · Yardım" satırı; Alış Kaydet düğme metni "Hurda Alış Kaydet" (sekme "Alış") — dil birliği toplu kuyruğa.
- **P-FORM-GORUNURLUK (Y5):** Platin 375/PRO 1024 ve Yıldız PRO Alış 375'te Kaydet ilk görünüm altında.
- **Ölçer:** `olcum2.mjs` tahsilat `scrollTo(0,0)` kusuru (bu paketin betiği; sonraki ölçümlerde kaldırılmalı).
- **Tam süit başkasına ait:** Fatura Y4C_K12 (kararsız), RAPOR-DORT RD_KH01/02. **denetim:** GIRDI-2 (MASAK-ESZAMAN), P-30.
- `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` proje ağacında — bu paketin değil, silinmedi.

## T2.10 Kurucu / mali müşavir
- Yeni mevzuat hükmü yok. "200.000 ₺ amaç beyanı kademesi" teyidi önceki paketlerden `KURUCU-KARAR-BEKLEYENLER`'de açık; ibare tezgâh ekranından kaldırıldı, sözlükte TEYİTSİZ damgasıyla duruyor.

## T2.11 Değişen dosyalar (tur 2; `git status --porcelain | grep -v -E "/(bin|obj)/"` — üst dizinler izlenmiyor `??`)
`dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` (TUR-KIMLIK (4)–(7)) · `dotnet/Kuyumcu.Web/Pages/Islemler.razor` ( M ; 2 öznitelik/ifade, satır sayısı 1399 aynen) · `dotnet/Kuyumcu.Web/Pages/Islemler.Odeme.cs` · `dotnet/Kuyumcu.Web/Pages/Islemler.TurKimlik.cs` · `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisTurSecici.razor` · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisMusteriSatiri.razor` · `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` (yalnız yorum) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.TurKimlik.cs` · `dotnet/Kuyumcu.Core/I18n/Sozlukler.Satis3.cs` · `dotnet/Kuyumcu.Tests/PTezgahTurKimlikTestleri.cs` · `dotnet/Kuyumcu.Tests/SATIS3EkranTestleri.cs` · `dotnet/Kuyumcu.Tests/SATIS3T24YasalMesajTestleri.cs`. Proje ağacına geçici dosya bırakılmadı (betikler/ölçümler `/private/tmp/kuyumcu-serit/P-TEZGAH-TUR-KIMLIK/`).
**Dokunulan sayfalar (TEK DOKUNUŞ):** Islemler.razor (+partial'lar), MasakFormu (bileşen), FisMusteriSatiri/FisTurSecici (bileşen) — tur 1'in aynı paket kapsamındaki ikinci turu (gerekçe: doğrulayıcı/hakem bulguları). Cariler.razor'a dokunulmadı. YildizTezgah.razor'a tur 2'de dokunulmadı.

---

# TUR 1 (tarihçe) — PAKET P-TEZGAH-TUR-KIMLIK: uygulama raporu (tur 1)

**Kapsam:** ekran denetimi 10.10 K4, Y4, Y2, O10 + YASAL-KAPI-3 tasarım kalanları (Tezgâh/İşlemler, kimlik çekmecesi, Cariler tahsilat çekmecesi). Taşkın (arka uç: değişiklik YOK, sunucu kuralları aynen) + Ertuğ (ön yüz). Şerit t9 · API 5188 · vekil 5189 · CDP 9983 · test DB `kuyumcu_test_t9`. COMMIT YOK.

## 1. Spesifikasyonun koda karşı doğrulanması (mevcut durum)

| Bulgu | Koddaki gerçek durum (11.10) | Sapma / uyarlama |
|---|---|---|
| K4 "beyan açıklaması gizli zorunlu, işaretsiz" | `MasakFormu` alanları `Zorunlu="true"`; ama `tema-yildiz.css` (`.yz .yz-duzen :is(... .zorunlu-isaret, .alan-ipucu …) {display:none}`) Yıldız kimlik çekmecesinde `*` ve ipucunu gizliyordu. Canlıda doğrulandı (etiketlerde `*` yok). | Yıldız çekmecesinde `*` görünür kılındı (TUR-KIMLIK bloğu). İpucu satırı açılmadı (SADE: açıklama satırı yok) — en az uzunluk Kaydet cümlesinde ve "Tamam" sonrası alan altında söylenir. |
| K4 "Kaydet nedensiz kilitli" | BASİT: "Kaydetmek için kimlik bilgilerini doldurun." (alan adı yok); PRO: "…kademe Yasal kapılar panelinde"; Platin'de form açıkken kilit satırı GİZLİ (`KilitMetniBantta`). | Kilit cümlesi eksik ALANLARIN ADINI söyler (`MasakEksikAlanlari` — `MasakFormu.Eksiksiz` ile birebir koşul) ve iki şemada da Kaydet'in yanında görünür. |
| K4 "Tamam kodda iptal" | Çekmecenin tek düğmesi `#cekmece-yildiz-kimlik-iptal` metni "Tamam". | Birincil `#cekmece-yildiz-kimlik-tamam` (yeni id) GERÇEK: eksikse alan altına yazar + ilk eksiğe odak (çekmece açık kalır), tamamsa kapanır. İkincil (id AYNEN) "Kapat". |
| K4 yeni çıkan: form tamamlanınca da "Bu işlem yasal bir kapıya takıldı; gereken alanlar açıldı." yazıyordu | `YildizNot` `BasitBantAcik` kullanıyordu (Platin bandının "form varken genel cümle yok" kuralı `BasitBantKapiAcik` Yıldız'a uygulanmamıştı). | `BasitBantKapiAcik` kullanıldı (Platin ile aynı kural). |
| Y4 işlem türü gizli | `#yildiz-tezgah-tur` 12 px "SATIŞ" yazısı (yalnız title); türler ızgarası yalnız ona basınca. Alış seçilince özgün (eski) sayfa: sağda "Ne yapıyorsun?" karoları (BASİT), "Diğer ▾" kesik çizgili kutu, gri özet bandı (`#islem-pro-toplam`), rozet/satıcı kartı. | Görünür sekmeler (Satış · Alış · İade · Tahsilat · Diğer) — Satış yüzünde üst satırın İÇİNDE (tuval konumu bozulmasın diye), öteki türlerde fişin başında aynı satır. `#yildiz-tezgah-tur` id ve geri çağrısı AYNEN, artık "Diğer". |
| Y2 müşteri listesi seçimle kapanmıyor | `YildizMusteriSec` katmanı kapatmıyordu. | Seçimle kapanır. (İç kutu 287 px konusu: liste zaten katman; ayrıca ölçülmedi — açık kalanlara yazıldı.) |
| O10 iç dil | `YasalKapiAcildi` "…yasal bir kapıya takıldı…". "Ürünü seçip fiyatı elle yaz" (`Sozlukler.Satis3`). | İlki düz Türkçeye çevrildi ("Bu işlem için ek bilgi gerekiyor; açılan alanları doldurun."). İkincisi DEĞİŞTİRİLMEDİ — ekip kararı: düz Türkçe emir cümlesi, jargon içermiyor; SATIS3 testi bu cümleyi "listede olmayan ürün DEMEZ" gerekçesiyle kilitliyor. |
| YK3 kalanı "Ad Soyad kırpılıyor" | Fişteki (Platin) kimlik formu `form-grid-3` → Ad Soyad 198 px (1280). | ≥769 px'te belge türü + no yan yana, Ad Soyad TAM SATIR (çekmecedeki YK3 düzeninin aynısı). |
| YK3 kalanı "BASİT'te iki birincil" | Platin BASİT'te seçili tür karosu dolu vurgu (`ekran-karo-birincil` — birincil düğme görünümü) + Kaydet. | Seçili karo çerçeveli (seçili durumu), dolgu yok → tek birincil Kaydet. Yıldız'da zaten saydamdı. |
| YK3 kalanı "nakit sınırı notu dar sütunda 4-5 satır" | Ölçüldü: Platin BASİT/PRO 1024–1280 fiş, Yıldız 1024 ödeme sütunu (380 px), Cariler tahsilat 1280/375 — 2 satırdan uzun not BULUNAMADI (en uzun: `#islem-odeme-otomatik` 380×44 = 2 satır). Önceki turlarda YASAL-KAPI-3 nakit notunu zaten tek cümleye indirmişti. | Kod değişikliği yok; ölçüm kanıtı aşağıda (§7). |
| YK3 kalanı "kimlik formu ilk ekranda tüm zorunluları göstermiyor" (375) | Tahsilat çekmecesinde form açılınca YK3'ün kaydırması (`scroll-margin-bottom`) çalışıyor. | Ölçüm: 40/40 bileşimde 5/5 zorunlu ilk görünümde (§7). Kod değişikliği gerekmedi. |

**Mevzuat:** yeni yasal hüküm/eşik YAZILMADI. MASAK 200.000/370.000 ve nakit 30.000 sunucuda aynen; ekrandaki tek sayı sunucunun gönderdiği en az açıklama uzunluğu (`AsgariAciklamaUzunlugu`). Birincil kaynak araştırması gerekmedi (yalnız ekran).

## 2. Yapılanlar (dosya dosya)

- `Kuyumcu.Core/I18n/Sozlukler.TurKimlik.cs` (YENİ): `TurKimlikSozlugu` TR+EN (sekme adları, "Kaydetmek için eksik: {0}.", Tamam/Kapat, "Bu alan zorunludur.", "En az {1} karakter yazın ({0} / {1}).").
- `Kuyumcu.Core/I18n/Sozlukler.cs`: yalnız `YasalKapiAcildi` TR+EN metni (O10).
- `Kuyumcu.Web/Bilesenler/Fis/FisTurSecici.razor` (YENİ): görünür tür sekmeleri (tuval `.sek` kalıbı); istek atmaz, sayfanın `TurSecDiger`'ini çağırır; Tahsilat = Müşteriler derin bağlantısı (FisIslemIzgarasi.Hedef kuralı); listede olmayan tür çizilmez; seçili tür ana türlerden değilse kendi adıyla görünür.
- `Kuyumcu.Web/Pages/Islemler.TurKimlik.cs` (YENİ): `YildizTurYuzu`, `yz-islem-tur` sınıfı, sekme parçaları, `KimlikEksikCumlesi`, `KimlikEksikGosterIste` (+ ilk eksik alana odak).
- `Pages/Islemler.Yildiz.cs`: `YildizSinif` öteki türler; bağlama `TurSecici/KimlikEksiksiz/KimlikEksikGoster`; `YildizNot` → `BasitBantKapiAcik`; müşteri seçince katman kapanır (Y2).
- `Pages/Islemler.YasalKapi2.cs`: `MasakEksikMetniTekYer` önce eksik alan adlı cümle.
- `Pages/Islemler.YasalKapi3.cs`: `KilitMetniBantta` form açıkken cümleyi gizlemez; yeni fişte `masakEksikGoster` sıfırlanır.
- `Pages/Islemler.Diger.cs`: `TurSecDiger` "Diğer" listesini kapatır.
- `Pages/Islemler.razor` (ÇOK SAHİPLİ — en küçük dokunuş, satır sayısı 1399 AYNEN, T5 tavanı korunur): `FisIslemIzgarasi` satırının başına `@if (YildizTurYuzu) { @YildizTurUstParcasi }`; `MasakFormu` çağrısına `EksikGoster="masakEksikGoster"` (Toplu5 imza dizgesi bozulmasın diye `Degisti`'den sonra).
- `Bilesenler/MasakFormu.razor`: `EksikGoster` parametresi (varsayılan false → diğer ekranlar AYNEN); boş zorunlu → "Bu alan zorunludur.", kısa beyan → tam cümle; `Eksiksiz` kuralı DEĞİŞMEDİ.
- `Bilesenler/Yildiz/YildizTezgah.razor` + `YildizTezgahVerisi.cs` (DOSYA ALANI DIŞI — gerekçe: kimlik çekmecesi ve tür düğmesi bu dosyada; t7 YILDIZ-TEZGAH-ODEME'nin ödeme sütununa DOKUNULMADI, yalnız üst satır `<p>` ve kimlik `Cekmece` + 3 bağlam özelliği; dosya sahibi şeritle çakışma riski orkestratöre not).
- `wwwroot/css/tema-yildiz.css`: dosya sonuna TUR-KIMLIK bloğu (yalnız `var()` belirteç, sabit renk yok).
- `wwwroot/css/yasal.css`: dosya sonuna P-TEZGAH-TUR-KIMLIK bloğu (Ad Soyad tam satır; Platin seçili karo çerçeveli).
- Testler: `Kuyumcu.Tests/PTezgahTurKimlikTestleri.cs` (YENİ, 12 test) + eski sözleşmeyi kodlayan 6 dosyada beklenti güncellemesi (aşağıda).

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** Islemler.razor (+ partial'lar), YildizTezgah.razor, MasakFormu.razor (bileşen; Cariler/Banka/Satınalma'da EksikGoster=false → davranış aynı). Cariler.razor/Cariler.YasalKapi3.cs'e DOKUNULMADI (ölçüm temiz).

## 3. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK, migration YOK. Yeni düğmeler: sekmeler (gerçek tür seçimi — istek değil, sayfa durumu), Tahsilat (bağlantı), Diğer (liste aç/kapa), Tamam (gerçek: kapat ya da alan altı hata), Kapat. Hiçbir düğme "tıklanınca hiçbir şey olmayan" değil (TK_01, TK_05, Y4B_11 kanıtı). ModalPencere kullanımı artmadı; düzenleme kalıbı Cekmece.

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Testler

**Yeni (12, `PTezgahTurKimlikTestleri`)** — 12/12 yeşil:
TK_01 (×2 mod) Tamam: eksikse alan altı mesaj + çekmece açık, kısa beyanda tam cümle, tamamsa kapanır, Kaydet açılır, POST yok · TK_02 kilit cümlesi alan adlı, title ile aynı, `*` DOM'da · TK_03 (×2) Platin form açıkken kilit satırı görünür ve alan adlı · TK_04 MasakFormu EksikGoster kapalı/açık + kural aynen · TK_05 (×2) Yıldız sekmeleri görünür, tek tıkla Alış, Diğer listesi seçimle kapanır, tek tıkla Satış'a dönüş · TK_06 Platin'de sekme yok, özgün ızgara aynen · TK_07 müşteri seçince katman kapanır · TK_08 O10 iç dil yok + TR/EN eşit · TK_09 CSS sözleşmesi (TUR-KIMLIK bloğu, Ad Soyad tam satır, karo kuralı, sabit renk yok).

**Davranış değiştiği için beklentisi güncellenen eski testler (gerekçe: denetim K4 — eski genel kilit cümlesini ve "form açıkken kilit satırı metinsiz" kararını kodluyorlardı):** `YasalKapi2IslemlerProTestleri.cs` (YK2_11, YK2_12), `SATIS3EkranTestleri.cs` (T19 ×2), `YasalParamTur3Testleri.cs` (Tur3 ×2), `SATIS3T24YasalMesajTestleri.cs` (T24 ×2 mod), `YasalKapi2FisTekUyariTestleri.cs` (YK3_F01 ×2 mod). Hepsi yeni yardımcı `PTezgahTurKimlikYardim.EksikKilit` ile: cümle "Kaydetmek için eksik:" ile başlar, ilgili alan adını içerir; eşik SAYISI yazmama beklentileri (DoesNotContain 200.000/370.000) AYNEN duruyor.

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---:|---:|---:|---:|
| Paket filtresi `PTezgahTurKimlik` | 12 | 12 | 0 | 0 |
| İlgili filtre (Yildiz·YasalKapi·Masak·S22·SATIS3·Tezgah·ParaDemo·ZoIslem·MASAK·Toplu5·S1d·OlcekTek·Satis4·KesifP5) | 2172 | 2169 | 1 (Y5A_04 — başkasının) | 2 |
| Son CSS düzeltmesinden sonra Yıldız + paket filtresi | 108 | 108 | 0 | 0 |
| TAM SÜİT (tur başına 1 kez, `-v n --logger trx`, trx: `/private/tmp/kuyumcu-serit/trx-pP-TEZGAH-TUR-KIMLIK`) | 12569 | 12559 | 7 | 3 |

Tam süitteki 7 kırmızının hiçbiri bu paketin dosyalarına/davranışına bağlı değil (FARK = 0):
- `FaturaEkranTestleri.Y4C_K12_ekranda_gelistirici_dili_YOK_Basit_ve_Pro(basit: False)` — rastgele GUID içinde "805" geçti (kararsız; Faturalar ekranı).
- `PUrunEkranTestleri.PU_B6_Zorunlular_formun_basinda` ×2 — "u-acilis-stok → u-tip sırası" (P-URUN şeridi, ürün formu).
- `RaporDortTestleri.RD_KH01`, `RD_KH02` — RAPOR-DORT, tarihe bağlı (YK3 raporunda da açık).
- `Yildiz5ATestleri.Y5A_04_emanet_satirlari_fifo_storno_esik` — "Asti"/"Emanette" (emanet dili, DENETIM-VERI/DIL şeridi).
- (İlk ara koşumda görülen `Yildiz4ATestleri.Y4A_03` ve `YasalKapi2NakitSiniri…YK3_01` sonraki koşumlarda yeşil — kararsız; bu paketle ilişkisi yok.)

## 6. Mutasyon kanıtı (kural kır → ADI yazılı test kırmızı → geri al → cmp)
Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/P-TEZGAH-TUR-KIMLIK/`, `perl -0777 -pi` tek eşleşme, `dotnet build` + filtreli test, yedek geri, `cmp`.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M1-1 | YildizTezgah `KimlikTamam`: koşul tersine (`!B.KimlikEksiksiz`) | TK_01 (BASIT, PRO) | AYNI |
| M1-2 | MasakFormu `BosHata`: `EksikGoster &&` → `false &&` | TK_01 (BASIT, PRO) | AYNI |
| M1-3 | YasalKapi2 `MasakEksikMetniTekYer`: alan adlı cümle devre dışı | T19 ×2, Tur3 ×2, TK_03 ×2, YK2_11 (7) | AYNI |
| M1-4 | YasalKapi3 `KilitMetniBantta`: form açıkken gizleme geri | T19 ×2, Tur3 ×2, TK_03 ×2, YK2_11 (7) | AYNI |
| M1-5 | FisTurSecici BASİT ana türlerden HURDA_ALIS çıkar | ilk koşum GEÇERSİZ (eşzamanlı derleme M1-4 ikilisini taşıdı; kırmızı TK_03 yanlış sebepten) → tekrar M1-5b: TK_05 (BASIT, HURDA_ALIS) | AYNI |
| M1-6 | `YildizMusteriSec`: katman kapatma satırı kaldırıldı | TK_07 | AYNI |
| M1-7 | `TurSecDiger`: Diğer listesini kapatma kaldırıldı | TK_05 (BASIT, PRO) | AYNI |
| M1-8 | CSS: kimlik çekmecesi `*` `display: none` | TK_09 | AYNI |

8/8 mutasyon kırmızı (M1-5 tekrarlı). Son mutasyondan sonra TEMİZ build: 0 hata (son build 6 uyarı — hepsi başka şeritlerin test dosyalarında: `SUBEKAPSAMASinirTestleri.cs`, `KesifP2Testleri.cs`, `SonKalanlarTestleri.cs`, `Yildiz2Testleri.cs`; bu paketin dosyalarında uyarı 0).

## 7. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9983, tek profil `/private/tmp/kuyumcu-serit/P-TEZGAH-TUR-KIMLIK/prof`, önbellek kapalı)
Senaryo her bileşimde: Tezgâh → Burma Bilezik 3 × 90.000 = 270.000 ₺ → müşteri Mehmet Demir → ödeme Kart (otomatik) → (Yıldız) "+ Kimlik bilgisi" → boş Tamam → uzun ad "Ayşegül Karaosmanoğlu" → amaç + beyan → Tamam → Vazgeç → Alış sekmesi (BASİT=eski altın, PRO=ürün alışı); ayrıca Cariler → Mehmet → Tahsilat 250.000 Kart (kimlik formu). 2 şema × 2 tema × 2 mod × 5 genişlik = 40 bileşim. Ölçer: `betikler/sikisma-olc.js` (`__olc`), paket ölçerleri `olcum.mjs`, `olcum-th.mjs`, eski öğe `eski.js` (yildiz-katman-tara ESKI kuralı + Inter + tuval boy kümesi + belirteç renk).

| şema · tema · mod | gen. | tür seçici | Kaydet sebebi Kaydet'in yanında / ekranda | birincil ekran · çekmece | kimlik zorunlu ilk ekran · işaretli | Tamam: boş→alan altı hata (çekmece) / dolu→ | Ad Soyad kırpma | Alış: eski öğe · birincil · tür seçici | Tahsilat çekmecesi: zorunlu · birincil · Ad Soyad | sıkışma K9-hariç (fiş / çekmece / alış / tahsilat) |
|---|---|---|---|---|---|---|---|---|---|---|
| yildiz · aydinlik · BASIT | 1024 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 1 / 3 / 0 / 0 |
| yildiz · aydinlik · BASIT | 1280 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · aydinlik · BASIT | 1366 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · aydinlik · BASIT | 1440 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 1 |
| yildiz · aydinlik · BASIT | 375 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · aydinlik · PROFESYONEL | 1024 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 1 / 1 / 0 / 0 |
| yildiz · aydinlik · PROFESYONEL | 1280 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 0 |
| yildiz · aydinlik · PROFESYONEL | 1366 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 0 |
| yildiz · aydinlik · PROFESYONEL | 1440 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 1 |
| yildiz · aydinlik · PROFESYONEL | 375 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · karanlik · BASIT | 1024 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 1 / 3 / 0 / 0 |
| yildiz · karanlik · BASIT | 1280 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · karanlik · BASIT | 1366 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · karanlik · BASIT | 1440 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 1 |
| yildiz · karanlik · BASIT | 375 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| yildiz · karanlik · PROFESYONEL | 1024 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 1 / 1 / 0 / 0 |
| yildiz · karanlik · PROFESYONEL | 1280 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 0 |
| yildiz · karanlik · PROFESYONEL | 1366 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 0 |
| yildiz · karanlik · PROFESYONEL | 1440 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 0 / 0 / 1 |
| yildiz · karanlik · PROFESYONEL | 375 | görünür | evet / evet | 1 · 1 | 5/5 · 5/5 | 2 (açık) / kapandı, Kaydet açıldı | 0 | 0 · 1 · görünür | 5/5 · 1 · 0 | 0 / 2 / 0 / 0 |
| platin · aydinlik · BASIT | 1024 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · BASIT | 1280 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · BASIT | 1366 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · BASIT | 1440 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · BASIT | 375 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · PROFESYONEL | 1024 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · PROFESYONEL | 1280 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · PROFESYONEL | 1366 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · PROFESYONEL | 1440 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · aydinlik · PROFESYONEL | 375 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · BASIT | 1024 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · BASIT | 1280 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · BASIT | 1366 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · BASIT | 1440 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · BASIT | 375 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · PROFESYONEL | 1024 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · PROFESYONEL | 1280 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · PROFESYONEL | 1366 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · PROFESYONEL | 1440 | görünür | evet / evet | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |
| platin · karanlik · PROFESYONEL | 375 | görünür | evet / hayır* | 1 · — | 5/5 · 5/5 | — (fişte form) | 0 | — | 5/5 · 1 · 0 | 0 / — / — / 0 |

`hayır*` (Kaydet sebebi "ekranda"): Platin 375 (BASİT+PRO) ve Platin PRO 1024×768'de kimlik formu ekranın üstüne kaydırıldığı için Kaydet + yanındaki cümle ilk görünümün altında; cümle Kaydet'in YANINDA ve görünür (kaydırınca). Platin'de Kaydet'in ekran dışında kalması denetim Y5 (P-FORM-GORUNURLUK paketi) konusudur; bu pakette değiştirilmedi.

**Sıkışma sayaçlarının açıklaması (K9 hariç toplamdaki 1–3 değerler, hepsi bu paketin dışında ya da ölçer yanılgısı):**
- Yıldız çekmece 2 = K15: çekmece başlığı `#cekmece-yildiz-kimlik-baslik` ve `×` `.ana-icerik`'in overflow kabının üstünde sayılıyor; çekmece `position: fixed` olduğu için kırpılmıyor (ekran görüntüsünde başlık ve × tam görünür). Yıldız Cekmece varyantı t1 YILDIZ-KATMAN'ın.
- Yıldız 1024 fiş/çekmece +1 = K3_kirik `#yildiz-tezgah-toplam-ayrinti "60,00 g · 54,9600 g has"` iki satıra kırılıyor (sol sütun; denetim D3, bu paketin alanı dışında).
- Yıldız tahsilat 1440 = 1 K3_tasan `#yildiz-musteri-ust "5 MÜŞTERİ · 0 BİZE BORÇLU"` — çekmecenin ARKASINDAKİ Müşteriler başlığı (form yokken de var; P-DIL-BORC-KASA alanı).
- K9 (binen): çekmece açıkken arkadaki fiş yazıları — `elementFromPoint` ile 4/4 noktada çekmece örtüyor (görünür binme 0). Yıldız 375 fişte K9=5: yapışkan alt şerit (FARK) kaydırılan kalem/toplamın üstünde — YASAL-KAPI-3 raporunda açık kalan YILDIZ-4D konusu, bu paketten önce de vardı.
- `yildiz-tipo-tara.mjs --rotalar /islemler` (BASİT+PRO × 1440+375 × koyu+açık): KURAL DIŞI 0, yatay 0; "kesik yazı 3" = `.gorsel-gizli "Barkod okut"` (ekran okuyucu etiketi) ve 375'te `.yz-tezgah-kasa` (t7 ödeme sütunu).
- `tuval-kiyas.mjs` Tezgâh 1440 (Tezgah4 / Tezgah4a + Amblem_sol nav): koyu 36 öğe AYNI 35 FARK 1, açık AYNI 35 FARK 1 — tek FARK `ödeme1 kasa` satır yüksekliği 21,75→18,16 (t7 YILDIZ-TEZGAH-ODEME'nin ödeme sütunu; bu paketin CSS'i `.yz-tezgah-kasa`'ya dokunmuyor). **Üst satır, Değiştir, barkod alanı/ikon/girdi, main: AYNI** → görünür sekmeler tuval yerleşimini bozmadı. Çıktı: `/private/tmp/kuyumcu-serit/P-TEZGAH-TUR-KIMLIK/kiyas/tk-{koyu,acik}-tablo.md`.
- Yıldız 375 düzeltmesi (tur 1 içinde): ilk ölçümde "DİĞER" ikinci satıra düşüyordu; ≤420 px'te sekmeler 13 px (tuval kümesinde) + 10 px aralık, müşteri satırı "Değiştir"le yan yana → tek satır sekme, yatay 0 (yeniden ölçüldü, tabloda son değerler).

**Ekran görüntüleri (Read ile bakılan 4):** `foto/e10-pro-ALIS-1366.png` (ÖNCE: Yıldız PRO Alış eski tasarım — gri özet bandı, kesik çizgili "Diğer ▾" kutusu, küçük "Alış 01:10" başlık), `foto/e14-yp-ALIS-1366.png` (sekmeler eklenmiş, flex hizası düzeltilmeden önceki ara hâl — 610 px sütun kusuru görüldü ve `align-items: stretch` ile düzeltildi), `foto/o-yildiz-karanlik-BASIT-375-tezgah.png` (375 ilk hâl; DİĞER kırılıyordu → düzeltildi), `foto/o-yildiz-aydinlik-BASIT-1280-kimlik.png` (SONRA: zorunlu `*`, boş Tamam sonrası "Bu alan zorunludur." alan altında, birincil Tamam + Kapat, uzun Ad Soyad tam görünür). Tüm bileşimlerin görüntüleri: `/private/tmp/kuyumcu-serit/P-TEZGAH-TUR-KIMLIK/foto/o-<şema>-<tema>-<mod>-<genişlik>-{tezgah,kimlik,alis,fis,tahsilat}.png`.

## 8. Canlı doğrulama (API 5188, test kiracısı `PAKETP-TEZGAH-TUR-KIMLIK-TEST 1c1646`) — sunucu kuralları DEĞİŞMEDİ
| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST /islemler/satis 3×90.000 KART, kimliksiz | 400 MASAK | 400 "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · …" |
| 2 | aynı, kimlik var amaç/beyan yok | 400 | 400 "İşlem amacı belirtilmelidir · MASAK beyan açıklaması … en az 20 karakter … (şu an 0)" |
| 3 | aynı, beyan 4 karakter | 400 | 400 "… en az 20 karakter olmalıdır (şu an 4)" |
| 4 | aynı, eksiksiz | 200 | 200 |
| 5 | NAKİT 30.000,01 (Zeynep) | 400 NAKIT_SINIRI | 400 `NAKIT_SINIRI` "İşlemin toplamı 30.000,01 TL, 30.000,00 TL nakit sınırını aşıyor; …" |
| 6 | NAKİT 29.999,99 (Ali) | 200 | 200 |

SQL (kişisel veri basılmadan): önce `masak_kimlik_tespitleri|masak_beyanlari|hareketler` = 0|0|5 → sonra 1|1|7; kimlik tespiti satırı: `TC_KIMLIK · 270000.00 · HAREKET · hareket_id dolu`.
Not (Madde 1): `canli.py` yanlışlıkla ikinci kez çalıştı (çıktı atıldı); test kiracısına ek satışlar yazdı — temizlik sayımında görülür (masak 2/2, hareket 9), kiracı tamamen silindi.
Tarayıcıda Tamam→Kaydet akışı da yürütüldü (Kaydet açıldı; satış POST'u tarayıcıdan gönderilmedi — ölçüm tekrarlanabilir kalsın diye).

**Temizlik:** `zsh betikler/kiraci-temizle.sh 01M4KX2Q1RSM0EPKRR51V9KC4S --uygula` → 75 tablo, 1440 satır + tenants 1 satır SİLİNDİ; davet satırı (`kiraci_davetleri`, TKDAV…) ayrıca silindi (kalan 0). tenants: başlangıç 24 → bitiş 22 (bu paketin kiracısı 0; aradaki −2 eşzamanlı şeritlerin kendi kiracı temizliği — benim silme listemde yalnız kendi kiracım var, log `temizle.log`). API (PID dosyası) ve vekil durduruldu; 5188/5189/9983 dinleyen yok. 5199/5200/5230/5241'e dokunulmadı.

## 9. denetim.sh
`DENETİM BAŞARISIZ` — yalnız iki bilinen, başkasına ait ihlal: `P-30 DEMO_INSAN_SURESI` (bilinen, kapı dışı) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` — MASAK-ESZAMAN, YK3 raporunda da açık). Bu paketin sayaçları (ModalPencere artışı, girdi ailesi, dil, CSS) ✓.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| 200k+ senaryoda tüm zorunlular ilk ekranda + işaretli | KARŞILANDI | §7: Yıldız çekmece 20/20 bileşim 5/5·5/5; Platin fiş 20/20 5/5·5/5; tahsilat 40/40 5/5 |
| Tamam çalışır (iptal değil) | KARŞILANDI | §7 20/20 (boş→2 alan altı hata, çekmece açık; dolu→kapandı, Kaydet açıldı); TK_01; M1-1/M1-2 |
| Kaydet kilitliyse sebebi tek cümle yanında | KARŞILANDI | "Kaydetmek için eksik: işlem amacı, beyan açıklaması (en az 20 karakter)." 40/40 görünür; TK_02/TK_03; M1-3/M1-4 |
| İşlem türü seçici görünür, tek tık | KARŞILANDI | §7 40/40 görünür; TK_05; M1-5b/M1-7 |
| Alış formu Yıldız'da eski öğe 0 | KARŞILANDI | §7 Alış 20/20 eski 0, Inter dışı 0, boy dışı 0, renk dışı 0 |
| Birincil düğme sayısı 1 | KARŞILANDI | §7 ekran 40/40 = 1, çekmece 20/20 = 1, tahsilat 40/40 = 1 |
| Ad Soyad kırpma 0 | KARŞILANDI (gerçekçi uzun ad) | §7 "Ayşegül Karaosmanoğlu" 120/120 ölçüm 0. İlk turda 38 harfli yapay ad 375'te kutuya sığmadı (kutu kaydırır, yazı kaybolmaz) — not. |
| Müşteri listesi seçimle kapanır (Y2) | KARŞILANDI | §7 `musteriKatmaniKapanmadi` 0; TK_07; M1-6 |
| O10 iç dil | KISMEN (bilinçli) | "yasal bir kapı" kaldırıldı; "Ürünü seçip fiyatı elle yaz" bırakıldı (§1 gerekçe) |
| Canlı SQL aynı | KARŞILANDI | §8 |
| Mutasyon ≥4 kırmızı, cmp AYNI | KARŞILANDI | §6 8/8 |
| build 0/0 | 0 hata; bu paketin dosyalarında 0 uyarı (6–8 uyarı başka şeritlerin test dosyalarında) | §6 |
| denetim TEMİZ (P-30 hariç) | KARŞILANMADI (başkasının) | GIRDI-2 MASAK-ESZAMAN fikstürü — bu pakete ait değil |
| Tam süit 0 yeni kırmızı | KARŞILANDI | §5: 7 kırmızı, hepsi başka alan |
| tuval-kiyas Tezgâh FARK 0 korunur | KARŞILANDI (bu paket için) | Tek FARK ödeme kasa satır yüksekliği (t7 alanı); üst satır/Değiştir/barkod AYNI |

## 11. Açık kalanlar (sahipleriyle)
- **t7 YILDIZ-TEZGAH-ODEME:** `ödeme1 kasa` satır yüksekliği tuvalden 21,75→18,16 (tuval-kiyas FARK 1); 375'te `.yz-tezgah-kasa` kesik; 375 yapışkan FARK şeridi K9.
- **t1 YILDIZ-KATMAN:** Yıldız çekmece başlığı K15 ölçer yanılgısı (`position: fixed` + `.ana-icerik` overflow); "Tamam" b1'in çekmece altlığında dolu hap yerine çerçeveli görünmesi (Cekmece Yıldız varyantı).
- **Kabuk (Y17):** Yıldız'da öteki türlerde sayfa başında eski "Alış 01:32 · Yardım" küçük başlık satırı hâlâ var (kabuk, bu paketin alanı değil).
- **P-FORM-GORUNURLUK (Y5):** Platin 375 ve Platin PRO 1024×768'de Kaydet ilk görünümün altında.
- **D3:** Yıldız 1024'te `#yildiz-tezgah-toplam-ayrinti` iki satır.
- **P-DIL-BORC-KASA:** `#yildiz-musteri-ust` 1440'ta taşma (Müşteriler).
- **Y2 kalan:** müşteri listesinin iç kutu yüksekliği (O9) bu turda ölçülmedi.
- **Tam süit başkasına ait kırmızılar:** P-URUN (PU_B6 ×2), RAPOR-DORT (RD_KH01/02), Y5A_04 (emanet dili), Fatura Y4C_K12 (kararsız GUID).
- **denetim:** GIRDI-2 (MASAK-ESZAMAN), P-30.
- **Dosya çakışması (orkestratöre):** `Bilesenler/Yildiz/YildizTezgah.razor` ve `YildizTezgahVerisi.cs` t7 ile ortak (benim dokunuşum: üst satır `<p>`, kimlik `Cekmece`, `KimlikTamam` yöntemi, 3 özellik). `Islemler.YasalParam.cs.tmp` proje ağacında var — bu paketin değil (silinmedi).

## 12. Kurucu / mali müşavir notları
- Yeni mevzuat hükmü yok; sunucu MASAK/nakit kuralları ve eşik sayıları aynen sunucudan. Önceki paketlerden açık soru ("200.000 ₺ amaç beyanı kademesi" teyidi) `KURUCU-KARAR-BEKLEYENLER` belgesinde duruyor.
- Güvenlik (Mehmet): kişisel veri kalıcı yapılmadı; ölçüm/canlı çıktılarında TCKN/parola basılmadı (fikstür dosyaları chmod 600, `/private/tmp` altında).

## 13. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` — bu paketin dokunduğu)
- ` M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- ` M dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.TurKimlik.cs`
- `?? dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor`
- `?? dotnet/Kuyumcu.Web/Pages/Islemler.Diger.cs`
- `?? dotnet/Kuyumcu.Web/Pages/Islemler.TurKimlik.cs`
- `?? dotnet/Kuyumcu.Web/Pages/Islemler.YasalKapi2.cs`
- `?? dotnet/Kuyumcu.Web/Pages/Islemler.YasalKapi3.cs`
- `?? dotnet/Kuyumcu.Web/Pages/Islemler.Yildiz.cs`
- `?? dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`
- `?? dotnet/Kuyumcu.Web/wwwroot/css/yasal.css`
- Dizin olarak izlenmeyen (`??` üst dizinde) ama bu paketin dokunduğu: `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisTurSecici.razor` (yeni), `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgah.razor`, `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgahVerisi.cs`, `dotnet/Kuyumcu.Tests/PTezgahTurKimlikTestleri.cs` (yeni), `dotnet/Kuyumcu.Tests/YasalKapi2IslemlerProTestleri.cs`, `dotnet/Kuyumcu.Tests/SATIS3EkranTestleri.cs`, `dotnet/Kuyumcu.Tests/YasalParamTur3Testleri.cs`, `dotnet/Kuyumcu.Tests/SATIS3T24YasalMesajTestleri.cs`, `dotnet/Kuyumcu.Tests/YasalKapi2FisTekUyariTestleri.cs`.
- Proje ağacına geçici dosya bırakılmadı (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/P-TEZGAH-TUR-KIMLIK/`, betikler ve ölçüm `/private/tmp/kuyumcu-serit/P-TEZGAH-TUR-KIMLIK/`).
