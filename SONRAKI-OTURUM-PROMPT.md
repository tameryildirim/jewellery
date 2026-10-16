# Sonraki oturum için devir promptu

> Bu dosyanın **1. bölümünü** yeni bir oturuma olduğu gibi yapıştır. Gerisi referanstır.

---

## 1. YAPIŞTIRILACAK PROMPT

```
Kuyumcu ERP projesine devam ediyoruz. Proje kökü:
/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery

ÖNCE ŞUNLARI OKU (sırayla):
1. SONRAKI-OTURUM-PROMPT.md          ← bu dosya, tam durum burada
2. PARITE-IS-LISTESI-MUHURLU-2026-09-14.md  ← mühürlü iş listesi, 48 paket
3. dokumanlar/08-degerlendirme/teknik-borc-envanteri.md
4. dokumanlar/02-mimari/adr/README.md  ← 15 karar kaydı

BAĞLAYICI KURALLAR (ihlal etme):
• Dil TÜRKÇE.
• KOD COMMİT ETME. Bu projede tek commit var (bbfdc95, devir) ve kurucu
  "kodları commitleme yok sakın ha" dedi. Çalışma ağacında kal.
• Anayasa md.1 — "Ne atlama, ne varsayım": başarısız komut gizlenmez, gerçek
  çıktısıyla raporlanır. "Bitti" ancak kanıtla. Bilinmeyen tahmin edilmez.
• Her yeni kural için MUTASYON KANITI: kuralı boz → testin kırmızıya döndüğünü
  isimleriyle göster → geri al. "Testler yeşil" tek başına kanıt değildir —
  bu projede yeşilden geçmiş 10+ gerçek hata bulundu.
• Her adım sonunda: dotnet test (0 başarısız) + ./denetim.sh (TEMİZ) +
  CANLI sunucuda gerçek veriyle doğrulama. Üçü de olmadan "oldu" denmez.
• Canlı doğrulamada açtığın test kiracılarını sonunda TEMİZLE
  (kuyumcu_net başlangıç durumu: 5 kiracı — Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası Kuyumculuk, Demo Kuyumcu, Ramazan Deneme).
• Sır/parola ekrana basma; "var/yok" de.

DURUM (2026-09-22 06:04, orkestratör kapısı): 3869/3869 test yeşil (orkestratör kapısı, trx kayıtlı) · dotnet/denetim.sh TEMİZ · 66 migration · 0 uyarı 0 hata · commit yok. Kalan işin tek kaynağı: dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md (§ A18 MÜHÜR 2 dalga planı).
PAKET 33 BİTTİ (bkz. A17-BİTTİ). ŞİMDİ (A18): kalan 27 paket + Katman 8 deneyim (UX-1 her veri girişi düzenlenebilir · UX-2 görsel tasarım sistemi · UX-3 basit/profesyonel mod) için mimari `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md` + `kalan-paketler/`; uygulama DALGALAR hâlinde Opus uygulayıcı + hasım doğrulayıcı + orkestratör kapısıyla sürüyor. Kurucu talimatı (2026-09-16): soru sorma, ekip oyuyla karar ver, kapsam dışı (Katman 1) hariç hepsini bitir, sonra UX.

ŞU AN NEREDE KALDIK: EKRAN BOŞLUKLARI KAPANDI (2026-09-14, ikinci oturum).
26 ekransız uç → 9'a indi; kalan 9 elle doğrulandı, hepsi ya değişkenle
çağrılıyor ya da listenin zaten taşıdığı detay ucu. Yeni ekranlar: Ürün
düzenle/sil/barkod/etiket/sayım · Ürün Ağacı (kategori+reyon) · Emanet ·
Kur · İşletme+Şube · kullanıcı ve kasa erişim kapatma. Çıkışta sunucu
tarafı token iptali (güvenlik) kapandı.
⚠ KAPSAM DÜZELTMESİ (kurucu, 2026-09-14): MÜŞTERİ PERAKENDE KUYUMCU DÜKKÂNI.
"Bizim imalatla bir işimiz yok, sadece sipariş veriyorum, oluyor bitiyor."
Mühürlü listedeki KATMAN 1 (imalat/atölye, 7 paket · 24–36 gün) ve içindeki
fire/iş emri/atölye kasası KAPSAM DIŞIDIR — önerme. Atölye zaten ATOLYE
türünde bir cari; ondan gelen mal normal ALIŞ olarak giriliyor.

SIRADAKİ: KARMA ÖDEME (paket 14) BİTTİ — Faz A + Faz B, canlıda kanıtlandı.
Sırada: paket 16 iade, paket 30 gün sonu kapanışı, paket 28 vade takibi.

Mevzuat araştırmasından çıkan iki bulgu KAPANDI (bkz. A8).
Gün sonu kapanışı (paket 30) BİTTİ (bkz. A9).
Katman 2 TAMAM (14–18). Katman 3 TAMAM: 23 sayım (A14), 21 çift birim (A15), 22 pırlanta 4C +
sertifika (A16); 24/26 zaten vardı, 20 büyük ölçüde var, 25 reyon ürün kartı düzeyinde (belgeli sınır).
Sırada: **PARITE Katman 4 (cari ve finans)** — ilk paket için fark analizi.

Başlamadan önce bana hangi kalemden devam edeceğini tek cümleyle söyle.
```

---

## 2. NEREDEN GELDİK

**Devir (9 Eylül 2026, commit `bbfdc95`):** çalışan ama testi ve yasal uyum katmanı olmayan bir prototip. 11.314 satır kod, **0 test**, migration yok (temiz makinede kurulamıyordu), kanıtlanmış **6 kritik hata** (en ağırı: bir işletme başkasının kasasına para yazabiliyordu).

**Bugün (14 Eylül 2026):**

| | Devir | Bugün |
|---|---|---|
| Üretim kodu | 11.314 | **22.838** |
| Test kodu | 0 | **9.826** |
| Test | yok | **662** (0 başarısız) |
| Migration | 0 | **13** |
| Tablo | 16 | **27** |
| ADR | 0 | **15** |
| Belge | 5 (4'ü geçersiz) | 34 |

**Kapanan devir borçları:** 6 kritik hatanın 6'sı · 11 eksik modülün 7'si · 7 teknik borcun 5'i (T3=TB-18 bu oturumda kapandı).

**Tamamlanan planlar:** PLAN-2 Adım 0–5 (test altyapısı, uyum ekranları, tevsik kapısı, sentetik taş, veri dışa aktarımı, barkod/etiket) · K4 referans kapısı denetimi · TB-12 yük testi.

---

## 3. BU OTURUMDA NE YAPILDI

### Kâr-zarar çekirdeği (kurucunun istediği iş)
Bu katmandan önce kod tabanında **hiçbir maliyet alanı yoktu** — kâr-zarar hesaplanamıyordu.

- `Data/StokPartisi.cs` — parti (lot) + tüketim varlıkları
- `Modules/Maliyet/MaliyetMotoru.cs` — FIFO / LIFO / Ağırlıklı Ortalama, parti aç/tüket/taşı
- `Modules/Maliyet/KarZararServisi.cs` — **iki bacaklı kâr** (TL + HAS), ürün bazlı kârlılık, stok değerlemesi
- `Pages/KarZarar.razor` — ekran

**Kuyumculuğun özü — canlı kanıtlandı:** has 6.800 iken 68.000 TL'ye alınan mal, has 8.000 iken 75.000 TL'ye satıldığında **TL'de +7.000 kâr, HAS'ta −0,625 gram zarar**. Yalnız TL gösteren rapor kuyumcuya eridiğini söylemez.

**Maliyeti bilinmeyen mal sıfır sayılmaz.** Devir öncesi stok için `NULL` maliyetli DEVIR partisi açıldı; rapor bunları toplama katmaz, `maliyetiBilinmeyenAdet` + `guvenilirlik` ile açıkça söyler.

### Stok paketleri
- **Maden genişletme**: platin (PT850–999), paladyum (PD500–999), K995 ticari has, G999. `Ayar` artık `Maden` taşıyor.
- **Çok seviyeli ürün ağacı** (`UrunKategorisi`) — döngü koruması, filtre alt ağacı kapsar
- **Reyon** (vitrin/kasa/depo/çanta) — ⚠ kapsam sınırı: reyon ÜRÜN KARTINA bağlı, stok satırına değil
- **Konsinye** — sahibi ve birim bedeli olmadan kaydedilemez/satılamaz; satılınca sahibine borçlanır, stok değerine katılmaz
- **Sayım artık deftere yazılıyor** — `SAYIM_DUZELTME` hareketi + parti tüketimi (**TB-18 kapandı**)

### Cari ekranı (kurucunun son sorusu)
Cari modülünün 9 ucundan yalnız 3'ü ekranda kullanılıyordu. Eklenen: **Düzenle · Tahsilat · Veresiye · Pasife Al** + 15 ekran testi.

### Bulunan gerçek hatalar (hepsi testlerden geçmişti)
1. **`AlisOlusturDto`'da ödeme aracı yoktu** → her peşin alış NAKİT sayılıyor, 30.000 TL üstü **havaleli alış hiç kaydedilemiyordu**. Alış girmeyince maliyet partisi doğmuyor, kâr-zarar zinciri baştan kopuyordu. Hurda alışta da aynısı.
2. **Stok raporu farklı madenleri topluyordu** — 100 g altın + 500 g gümüş = "562,5 g has". Anlamsız. `toplamHas` artık yalnız altın, `madenDagilimi` ayrı.
3. **Cari düzenlemede tam kart çekilmezse vergi no/adres silinirdi** (listeden gelen özetle kaydedilirse).

### Mutasyon kanıtları (8 adet, hepsi doğrulandı)
bilinmeyen maliyet sıfır sayılınca · transfer maliyeti taşımayınca · sayım deftere yazmayınca · madenler toplanınca · konsinye borcu yazılmayınca · kategori döngü koruması kalkınca · cari düzenlemede tam kart çekilmeyince · tahsilatta ödeme türü gönderilmeyince → ilgili testler kırmızı.

**Bir test boşluğu mutasyonla yakalandı:** transfer testi motoru doğrudan çağırıyordu, satış akışını değil. Uçtan uca test eklendi.

---

## 4. SIRADA NE VAR

### A) Ekran boşlukları — KAPANDI (2026-09-14, ikinci oturum)

113 uçtan **26'sının ekranı yoktu → 9'a indi**. Kalan 9'un tamamı elle
doğrulanmış YANLIŞ POZİTİF ya da kullanıcıya yansımayan detay ucudur:

| Kalan uç | Neden boşluk değil |
|---|---|
| `/faturalar` · `/gider-pusulalari` · `/masak/kimlik-tespitleri` | çağrılıyor; yol değişkene/çok satırlı interpolasyona kuruluyor, ölçüm betiği göremiyor |
| `/bankalar/{}` · `/kasalar/{}` · `/kullanicilar/{}` · `/faturalar/{}` · `/cari/{}/bakiye` | detay ucu; listenin/ekstrenin taşıdığı veriyi tekrar eder |
| `/kullanicilar/ben` | `/auth/ben` ile aynı bilgi; ekran onu kullanıyor |

**Bu oturumda eklenen ekranlar** (hepsi ekran testli + mutasyon kanıtlı):

| Ekran | Kapatılan uçlar |
|---|---|
| Ürünler (genişletildi) | düzenle (tam kart) · pasife al · barkodla bul · etiket · sayım düzeltme · kategori/reyon süzgeci ve bağlama · konsinye alanları |
| **Ürün Ağacı** `/urunler/agac` (yeni) | kategori ağacı GET/POST/PATCH · reyon GET/POST |
| **Emanet** `/emanet` (yeni) | emanet giriş · çıkış · cari bazlı listeleme |
| **Kur** `/kur` (yeni) | manuel kur · geçmiş · sarrafiye pariteleri · dış kaynaktan çek |
| **İşletme** `/isletme` (yeni) | tenant bilgisi · plan · AI anahtarı · şube düzenle/detay/pasife al |
| Kullanıcılar (genişletildi) | **erişim kapatma** — ayrılan çalışan giriş yapmaya devam ediyordu |
| Kasalar (genişletildi) | kasa pasife alma (bakiyeli kasa kapatılamaz) |
| Çıkış (güvenlik) | `POST /auth/cikis` — web istemcisi bu ucu HİÇ çağırmıyordu |

**Bu oturumda bulunan gerçek hata:** kategori yanıtında `altlar` alanı gelmezse
ağaç yürüyüşü NullReference atıp **ürün ekranının tamamını** çökertiyordu.
Kategori isteğe bağlı bir yardımcıyken ürün listesi bile açılmıyordu.

**Mutasyon kanıtı: 12 adet**, hepsi doğrulandı — çıkışta sunucuya haber
verilmeyince · düzenlemede tam kart çekilmeyince · kategori döngü koruması
kalkınca · pasif kategoriler çekilmeyince · `altlar` null toleransı kalkınca ·
emanette kurSnapshotId gönderilmeyince · iade fazlalık kapısı kalkınca ·
manuel kur formu sıfırdan başlayınca · şube düzenlemede detay çekilmeyince ·
son aktif şube korunmayınca · başarısız plan değişikliği geri alınmayınca ·
kendini pasife alma engeli kalkınca → ilgili testler kırmızı.

**Bir mutasyon YANLIŞ kuruldu ve fark edildi:** Blazor'da `disabled="false"`
bir *dizgedir* ve düğmeyi yine devre dışı bırakır; `disabled="@(false)"`
yazılmadan mutasyon etkisizdir. İlk denemede test yeşil kalmıştı.

### A2) Tezgâh/POS — paket 18 BİTTİ (2026-09-14)

**Satış ekranında barkod okutma.** `/urunler/barkod/{kod}` ucu vardı, satış
ekranı kullanmıyordu; tezgâhtar her kalemi açılır listeden arıyordu. Artık
okut → fiyat canlı kurdan hesaplanır → kalem doğrudan düşer.

⚠ **Gümüş tuzağı korundu:** gümüşün gram kotasyonu modelde olmadığı için
fiyat otomatik hesaplanmaz (has gramı ALTIN kuruyla çarpmak 12 g gümüş
kolyeyi 76.972 TL yapıyordu). Okutunca sessizce hiçbir şey olmaması
okutucuyu bozuk sandırırdı: ürün seçili bırakılır, fiyat elle istenir.

**Bu pakette bulunan 2 gerçek kusur:**
1. `Islemler.razor` ilk yüklemede `Canli.BaslatAsync()`'i BEKLİYORDU; bu da
   SignalR el sıkışmasını bekliyordu. Proxy/kopuk hatta satış ekranı
   "Yükleniyor"da kalıyordu. Push kanalı artık ateşle-unut; kur zaten
   REST'ten alınıyor, push yalnız hızlandırma.
2. `CanliKurServisi` yalnız `IAsyncDisposable` uyguluyordu; senkron kapatılan
   bir DI kapsamına konduğunda `InvalidOperationException` atıyordu.
   `IDisposable` de eklendi.

**Mutasyon kanıtı (2 adet):** okutunca kalem eklenmeyince · gümüşte sessizce
geçilince → ilgili testler kırmızı.

### A3) ALIŞ/HURDA ödeme aracı — CANLI HATA KAPANDI (2026-09-14)

`Islemler.razor` ödeme türü seçicisini YALNIZ satışta çiziyordu; ALIŞ ve
HURDA_ALIS gövdelerine `odemeTuru` alanı HİÇ konmuyordu. Sunucu varsayılanı
devreye giriyordu (`AlisOlusturDto.OdemeTuru = "NAKIT"`, bilinçli fail-closed).

⚠ **Sonuç: 30.000 TL üstü HAVALELİ alış EKRANDAN HİÇ GİRİLEMİYORDU** — tevsik
kapısına takılıyordu. Sunucu tarafındaki aynı hata 2026-09-14'te kapatılmıştı,
ekran açık kalmıştı. Perakende dükkânda mal alımı günlük iştir.

Seçici artık alış ve hurda alışta da çıkıyor, sunucunun kabul ettiği üç değerle
sınırlı (NAKIT|KART|HAVALE — alışta ALTIN/VERESIYE yoktur, veresiye payı
`OdenenTl` ile belirlenir). Satıştan alışa geçince ALTIN/VERESIYE seçimi
taşınmıyor. 6 ekran testi + 3 mutasyon kanıtı.

**Ayrıca düzeltildi:** POS barkod testleri yük altında kırılgandı —
`KeyDown` eşzamansız işleyiciyi beklemiyordu. `WaitForAssertion` ile
koşul beklenir hâle getirildi (sabit uyku değil). 5 ardışık koşu 740/740.

### A4) KARMA ÖDEME — TASARIM HAZIR, UYGULAMA BAŞLAMADI

18 ajanlı iş akışı (5 keşif · 3 bağımsız tasarım · 9 hasım yargı · 1 sentez)
kesin bir plan üretti. Seçilen omurga: **yeni tablo YOK**; `hareketler`e
`nakit_tutar` + `veresiye_tutar` + (Faz B) `fis_id` kolonları.
Ayrı `fisler`/`odeme_bacaklari` tablosu REDDEDİLDİ: tevsik toplamının tek
kaynağı yeni bir tabloya taşınırsa, fiş satırı yazmayan her nakit yolu
(Takas, İşçilik, Tahsilat, tüm eski kayıtlar) toplamdan düşer — açık
kapanacakken BÜYÜR.

**Faz A (~3 gün, karma özelliğinden BAĞIMSIZ sevk edilebilir):** tevsik açığını
kapatır. **Faz B (~5 gün):** çok bacaklı fiş + yeni `POST /islemler/satis-fisi`.
**Faz C (karara bağlı):** işlem tutarı kuralı + eşzamanlılık kilidi.

**Kodla DOĞRULANMIŞ, bugün canlı olan açıklar:**
1. `tevsik.Dogrula` yalnız 4 yerden çağrılıyor (SatisModulu 315/402/495,
   CariModulu 448). **Takas (:668) ve İşçilik (:734) kapıyı HİÇ çağırmıyor**
   ama `OdemeTuru = kasa is not null ? "NAKIT" : "VERESIYE"` damgalayıp
   kümülatif toplamı BESLİYOR. Yani başkasının haddini doldururken kendileri
   hiç denetlenmiyor.
2. Takas'ta `TlTutari = farkTl` NEGATİF olabilir (tediye) ve yine "NAKIT"
   damgalanıyor; bugünkü `SUM(ABS(tl_tutari))` bunu kümülatife katıyor.
3. `OdemeEtiketi` (SatisModulu.cs:802) ÖLÜ KOD — çağıranı yok. 14 Eylül'de
   düzeltilen hatanın ta kendisi (her peşin ödemeyi koşulsuz "NAKIT" sayar).
   Karma yazan biri "hazır yardımcı" diye çağırırsa hata geri gelir.
4. `Hareketler.Add` 11 yerde (10 değil): SatisModulu 321/407/505/588/668/734,
   CariModulu 393/452, EmanetModulu 90/154, UrunModulu 603.

### A5) KARMA ÖDEME — FAZ A TAMAMLANDI (2026-09-15)

**Kapanan açık:** `hareketler.odeme_turu` tek etiket olduğu için kısmi/karma
ödemenin NAKİT payı 459 s. VUK kümülatif toplamına giremiyordu. 12.000 nakit +
38.000 veresiye şeklinde bölünen bir alış, kümülatifte HİÇ görünmüyordu.

**Yapılanlar**
| Adım | İçerik |
|---|---|
| A-1 | `hareketler.nakit_tutar` + `veresiye_tutar` (işaretsiz, `null` = BİLİNMİYOR) |
| A-2 | `HareketFabrikasi.Ekle(db, OdemeIzi, taslak)` — iz ZORUNLU konumsal parametre; vermeyen kod DERLENMEZ. `denetim.sh` artık `Hareketler.Add` sayısının 1 (yalnız fabrika) olduğunu sayar |
| A-3 | Migration `20260914184552_HareketOdemeIzi` + `(tenant_id, cari_id, tarih)` indeksi + KANITA DAYALI geri-doldurma |
| A-4 | 11 yazma noktasının tamamı fabrikaya bağlandı |
| A-5 | `GunlukNakitToplam` İKİ AYRIK SORGUYA çevrildi (CASE yok): yeni satırlar `nakit_tutar`, eski satırlar `odeme_turu='NAKIT'`. Çift sayma yapısal olarak imkânsız |
| A-6 | **TAKAS ve İŞÇİLİK'e eksik tevsik kapısı eklendi** + `OdemeTuru` beyanı + kasa etiketi düzeltmesi |
| A-7 | Ölü `OdemeEtiketi` silindi |
| A-9 | Dışa aktarıma `nakitTutar`/`veresiyeTutar` kolonları |

**Kodla doğrulanmış, kapatılan fail-open açık:** `tevsik.Dogrula` yalnız 4
yerden çağrılıyordu; **Takas ve İşçilik kapıyı HİÇ çağırmıyor** ama hareketi
"NAKIT" damgalayıp kümülatifi besliyordu — başkasının haddini doldururken
kendileri denetlenmiyordu. Takas'ta fark NEGATİF (tediye) olabildiği için nakit
payı `Math.Abs` ile alınır; tediye de hadde tabidir.

**GERİ-DOLDURMA — kanıt var, tahmin yok.** İki kesin kaynak: (1) TL bacağı sıfır
olan hareketler (emanet/transfer) → (0,0); (2) kasa defterinde TEK eşleşen satırı
olan hareketler → araç ve tutar kanıttan. Kanıtı olmayan satır NULL kalır.
Canlı sonuç: 76 hareketin **37'si dolduruldu, 39'u "bilinmiyor" kaldı**;
görünür nakit 394.322,34 TL'den 888.512,15 TL'ye çıktı. SQL metni
`NakitPayiGeriDoldurma` sabitlerindedir; migration ile TEST AYNI METNİ koşar.

**Mutasyon kanıtı: 9 adet** (M18–M26) — fabrika izi yok sayılınca · Takas kapısı
kalkınca · İşçilik kapısı kalkınca · KARMA satırın tamamı nakit sayılınca ·
Takas'ta `Math.Abs` kalkınca · geri-doldurmada "zaten dolu" koruması kalkınca ·
`HAVING COUNT(*)=1` kalkınca · veresiye payı koşulsuz 0 yazılınca · parasız
hareket doldurması kalkınca → ilgili testler kırmızı.

**CANLI KANIT (kuyumcu_net, 2026-09-15)**
- Kısmi ödemeli alış 50.000 TL → `odemeTuru=KARMA`, `nakitTutar=12.000`, `veresiyeTutar=38.000`
- Ardından 19.000 TL nakit satış → **400** ("toplam 31.000 TL, haddi 1.000 TL aşıyor") — ESKİDEN GEÇİYORDU
- Aynı tutar HAVALE → **200**, `nakitTutar=0` (fazladan kapatmıyor)
- İşçilik 20.000 nakit → **400** (37.000 > 30.000) · Takas 20.420 nakit fark → **400** (37.420 > 30.000) · aynı takas KART → **200**
- Migration ileri/geri koşturuldu: Down iki kolonu da düşürdü, Up geri-doldurmayı **birebir aynı** üretti (37 satır, 888.512,15 TL) — deterministik

**⚠ BU OTURUMDA BULUNAN AYRI BİR KUSUR — `yedek-al-ve-dogrula.sh` BOZUKTU.**
Adım 2'de sağlama SQL'i `GROUP_CONCAT` ile tek satırda birleştiriliyordu;
MySQL'in `group_concat_max_len` varsayılanı **1024 bayttır** ve şema 27 tabloya
çıkınca metin cümlenin ortasından kesiliyordu (ölçüm: uzunluk tam 1024, son
ifade "CHECKS"). Betik `set -e` ile düşüyor, yani **yedeğin geri
yüklenebildiği bir süredir kanıtlanamıyordu** — betiğin kendi başında kırmızı
bayrak diye anlatılan durum. `GROUP_CONCAT` kaldırıldı, tablo listesi satır
satır okunuyor. Düzeltme sonrası: 27 tablo, 27 sağlama birebir, çıkış 0.

**Bu oturumda kuyumcu_net'e eklenen test verisi (4 hareket):** ALIS −50.000
(nakit payı 12.000) · SATIS +19.000 (havale) · ISCILIK +5.000 (nakit) ·
TAKAS +20.420 (kart). Defter append-only olduğu için SİLİNMEDİ.
Yedek: `scratchpad/kuyumcu_net-20260915-111100.sql` (migration ÖNCESİ hâl).

### A6) KARMA ÖDEME — FAZ B TAMAMLANDI (2026-09-15)

**Yeni uç:** `POST /api/v1/islemler/satis-fisi` — tek fişte nakit + kart +
havale + veresiye. Mevcut `POST /islemler/satis` ucu DEĞİŞMEDİ ve
`SatisOlusturDto.OdemeTuru` regex'ine "KARMA" EKLENMEDİ; tek ödemeli akışın
davranışı hiç kıpırdamadı.

| Adım | İçerik |
|---|---|
| B-1 | `hareketler.fis_id` + `(tenant_id, fis_id)` indeksi + migration `20260915082007_HareketFisId`. **Ayrı `fisler` tablosu AÇILMADI** — fiş, aynı `fis_id`'yi taşıyan hareket kümesidir |
| B-2 | `OdemeSepeti` — bacak doğrulamasının tek merkezi: 1–5 bacak · her tutar > 0 · en fazla bir veresiye · veresiyede cari zorunlu · veresiyede kasa yasak · diğerlerinde kasa zorunlu · **bacak toplamı = fiş toplamı, TOLERANS YOK** |
| B-3 | `SatisFisi` servisi — `Satis()`'in KAPI PARİTESİ satır satır: kur · cari · ürün · md.11/(e) taş kökeni · konsinye · stok · MASAK (fiş toplamı) · K6 kasa para birimi (bacak başına) · tevsik (fişin nakit toplamı, TEK KEZ) |
| B-4 | Fatura damgası **fişin TÜM kalemlerine** vurulur. Tek satıra vurulsaydı N−1 kalem damgasız kalır, gün sonu KDV mutabakatı (KDV beyanının dayanağı) her gün yanlış alarm verirdi |
| B-5 | `GET /api/v1/tevsik/gunluk` — salt okunur önizleme. Tezgâhtar haddi artık Kaydet'e bastıktan sonra değil, müşteri karşısındayken görüyor |
| B-7 | Ekranda karma anahtarı + bacak listesi + Kalan göstergesi + "Kalanı ata". Karma fiş **TEK POST** ile gider; kalem başına gönderim bacakları anlamsız kılardı |

**PAY DAĞITIMI — sıralı, orantılı DEĞİL.** Orantılı dağıtım her kalemde
yuvarlama yapar ve payların toplamı fiş toplamından kuruş sapabilir. Sıralı
doldurmada yalnız çıkarma vardır; `Σ pay = toplam` TANIM GEREĞİ sağlanır.

### A7) YASAL ARAŞTIRMA — 19 AJAN, HEPSİ DOĞRULANDI (2026-09-15)

6 soru araştırıldı, her biri iki hasım mercekten (kaynak gerçekten öyle mi
diyor · daha yeni değişiklik/karşı yorum var mı) sınandı.

| Soru | Puan | Sonuç |
|---|---|---|
| Karma ödemede had | **8.0/10 ✓✓** | Had **TOPLAM işlem tutarına** bakar. 50.000 TL'lik satışın 20.000'ini nakit almak İHLALDİR |
| MASAK eşiği ve bölme | **8.5/10 ✓✓** | 185.000 TL, işlem tutarına bakar, **bağlantılı işlemler TOPLANIR** |
| Hurda stopaj brüt/net | **8.0/10 ✓✓** | Vatandaştan alımda **%2 stopaj HİÇ YOK**; brüt = net. %2 yalnız esnaf muaflığındakine |
| Tezgâh müşterisi | 7.0/10 | Kural "aynı kişi" der, cari şartı yok; pratikte müşteri anahtarı gerekir |
| Takas/trampa | 7.0/10 ✗ | Mevzuatta **hüküm YOK**, çıkarım. Aksi yönde özelge bulundu |
| Altınla ödeme | 6.5/10 ✗ | Mevzuatta **hüküm YOK** |

**UYGULANAN:** işlem tutarı kuralı (Kapı 2) — `TevsikEsikleri.IslemTutariKuraliEtkin`,
**varsayılan AÇIK**. Satış, alış, hurda alış ve fiş uçlarında işler.
**TAKASA UYGULANMADI** ve gerekçesi koda yazıldı: trampa tebliğde hiç geçmiyor,
güven düşük, doğrulama merceği aksi yönde özelge buldu — doğrulanmamış bir
yorum yüzünden her altın takası bloke edilmez.

**YANLIŞ BİLGİ OLARAK ELENDİ:** "kuyumcuda nakit yasaklandı" ve "75.000 TL
sınırı" — hiçbir Resmî Gazete metni bulunamadı, koda YAZILMADI.

**Mutasyon kanıtı: 5 adet** (M27–M31) — bacak eşitliği toleranslı olunca ·
Kapı 2 kapanınca · paylar orantılı dağıtılınca · fatura damgası tek satıra
vurulunca · ekranda bacak toplamı kapısı kalkınca → ilgili testler kırmızı.
M30 ilk denemede hiçbir testi kırmadı; **test boşluğuydu**, fatura damgası
testleri yazılıp mutasyon tekrarlandı.

**CANLI KANIT (kuyumcu_net, 2026-09-15)**
- 20.000 TL'lik 2 kalemli fiş = 12.000 nakit + 5.000 kart + 3.000 veresiye → **200**
- Defter: iki kalem AYNI `fis_id`, Σnakit 12.000, Σveresiye 3.000, sıralı dağıtım
- Kasada **bacak başına ayrı satır** (NAKIT 12.000, KART 5.000), özet satır YOK
- Cari bakiye +3.000
- Bacak toplamı 19.999 ≠ 20.000 → **400** ("fark −1,00 TL")
- 40.000 TL'lik fişin 5.000'i nakit → **400** (§4.1.2) · aynı fiş tamamen kart → **200**
- Önizleme ucu: 20.000'lik fişte kalan hak 30.000; 40.000'likte **0 + kilit**

**Bu oturumda kuyumcu_net'e eklenen test verisi:** 4 hareket (önceki turda) +
`FIS-TEST Musteri` carisi + 1 stok alışı + 2 karma fiş. Defter append-only
olduğu için silinmedi.

### A8) ARAŞTIRMA BULGULARININ UYGULANMASI (2026-09-15)

**(a) Hurda stopajı — DÜZELTME GEREKMEDİ.** Araştırma "vatandaştan alımda %2
kesilmez" dedi (8.0/10). Kod KONTROL EDİLDİ: `GiderPusulasiAyarlari`
zaten `NihaiTuketiciStopajOran = 0m`, `EsnafMuafStopajOran = 0.02m` tutuyor ve
`GiderPusulasiTestleri` üç satıcı tipini de sınıyor. Bir önceki oturumda devir
belgesine "kod stopaj kesiyor" diye yazmıştım — bu KODA BAKMADAN yapılmış bir
varsayımdı, geri alındı.

**(b) MASAK bağlantılı işlem kümülatifi — EKLENDİ.** Tedbirler Yönetmeliği
m.5/1-(b) eşiği "işlem tutarı YA DA birbiriyle bağlantılı birden fazla işlemin
TOPLAM tutarı" üzerinden kurar (8.5/10, iki mercek de doğruladı). Kapı çağrı
BAŞINA bakıyordu: aynı müşteriye aynı gün 3×150.000 TL satışta hiçbiri
185.000'e değmiyor, kimlik tespiti HİÇ tetiklenmiyordu — eşik bölme
(structuring) tam olarak budur.

`MasakKapisi.DogrulaKumulatif(...)` eklendi; aynı YEREL GÜNDE aynı cariyle
yapılmış MASAK'a tabi işlemlerin TL toplamı okunur ve eşik onun üzerinden
değerlendirilir. Kayda geçen tutar yine işlemin KENDİ tutarıdır. Sekiz çağrı
noktası da (Satis, SatisFisi, Alis, HurdaAlis, Takas, Iscilik, VeresiyeGir,
TahsilatGir) buna bağlandı.

⚠ **Kodda yazılı iki eksik ölçüm:** TAKAS satırı deftere `farkTl` yazar, el
değiştiren brüt değeri değil (kapı tek işlemde doğru ölçüyü kullanır ama
kümülatif onu göremez); HURDA_ALIS satırı stopaj sonrası net taşır (vatandaşta
stopaj sıfır olduğu için yalnız ESNAF_MUAF'ta %2 eksilir). İkisi de sapmayı
GEVŞEK yönde yapar. Ayrıca cari YOKSA kümülatif kurulamaz — kişi bilinmiyor.

**Mutasyon kanıtı: 3 adet** (M32–M34) — kümülatif kalkınca · müşteri ayrımı
kalkınca · eşik operatörü `>=` yerine `>` olunca → ilgili testler kırmızı.
(Operatör farkı önemli: tevsik haddi `>`, MASAK eşiği `>=`.)

**CANLI KANIT:** 100.000 TL satış geçti → ikinci 100.000 TL **400**
("185.000 TL ve üstü işlemde kimlik tespiti zorunludur … aynı gün aynı
müşteriyle bağlantılı işlemlerin toplamı 200.000,00 TL") → aynı satış
kimlikle **200**.

### A9) GÜN SONU KAPANIŞI — paket 30 BİTTİ (2026-09-15)

Kasa bakiyesi sistemin İDDİASIDIR; çekmecedeki para GERÇEKTİR. Fark ancak
sayılıp kaydedilirse görülür; kaydedilmeyen fark ertesi güne devreder ve
hangi günden geldiği bir daha bulunamaz.

**Yeni:** `gun_kapanislari` tablosu (migration `20260915085659_GunKapanisi`),
`kasa_id + yerel_gun` üzerinde **BENZERSİZ indeks** — eşzamanlı iki kapanış
isteği aynı günü iki kez kapatamasın diye veritabanı düzeyinde zorlanır.

| Uç | İş |
|---|---|
| `POST /kasalar/{id}/gun-kapanisi` | Sayım + fark kaydı + gün kilidi. **Fark varsa gerekçe ZORUNLU** |
| `POST /kasalar/{id}/gun-kapanisi/{tarih}/ac` | Yeniden açma — **yalnız SAHİP**, gerekçe en az 20 karakter |
| `GET /kasalar/{id}/gun-kapanislari` | Kapanış geçmişi |

**GÜN KİLİDİ tek noktadan uygulanır.** `IslemDogrulayici.KasaDogrula`'ya
opsiyonel `an` parametresi eklendi; verilirse `GunKapanisiKapisi` çalışır.
Yazma yollarının HEPSİ bağlandı (satış, fiş, alış, hurda, takas, işçilik,
`OdemeCoz`) **ve elle kasa hareketi ucu da** — o unutulsaydı kapanmış gün
oradan sessizce delinirdi.

**Tasarım kararları ve gerekçeleri**
- Yeniden açma kaydı SİLMEZ: kim, ne zaman, hangi gerekçeyle açtığı aynı
  satırda saklanır. Silinseydi denetimde "gün kapanmış mıydı" sorusu
  cevapsız kalırdı.
- Yeniden açılan gün tekrar kapatılınca AYNI satır güncellenir — kasa+gün
  benzersizliği korunur, tarihçe tek yerde kalır.
- Fark İŞARETLİ saklanır (eksik negatif, fazla pozitif). Mutlak değere
  çevrilseydi "fazla mı eksik mi" bilgisi kaybolurdu.
- Ekranda sayım alanı BOŞ başlar. Defter bakiyesiyle doldurulsaydı kullanıcı
  saymadan Kaydet'e basar; kapanış tutanak değil tıklama olurdu.
- Kilit KASA BAŞINADIR: bir kasanın kapanması dükkânı durdurmaz.

**Mutasyon kanıtı: 7 adet** (M35–M41) — kilit hiç uygulanmayınca · yeniden
açılmış gün kilitli sayılınca · gerekçe zorunluluğu kalkınca · fark mutlak
değere çevrilince · elle kasa hareketinde kilit kalkınca · ekranda sayım
alanı defterle dolu başlayınca · ekranda gerekçe kapısı kalkınca → kırmızı.

**CANLI KANIT (kuyumcu_net):** gerekçesiz farklı sayım **400** → gerekçeyle
**200** (fark −750, kilitli) → kapanmış güne kasa hareketi **400** → ikinci
kapanış **409** → yeniden açma **200** → sonrasında hareket **200** →
geçmişte kapanış ve açma gerekçesi görünüyor.

### A10) İADE — paket 16 BİTTİ (2026-09-15)

İade bağımsız bir işlem DEĞİLDİR; satışın aynasıdır. Orijinal işlem
bulunmadan iade alınabilseydi hiç satılmamış mal "iade" diye stoğa sokulur
ve kasadan para çıkarılabilirdi.

**Yeni:** `hareketler.iade_kaynak_hareket_id` (migration
`20260915091709_HareketIadeKaynagi`) + indeks. İade satırı hangi işlemi
tersine çevirdiğini taşır; bu bağ olmadan "bu maldan ne kadarı zaten iade
edilmiş" sorusu cevaplanamaz.

| Uç | İş |
|---|---|
| `GET /islemler/iade-adaylari?tur=SATIS` | İade edilebilecek işlemler; tamamı iade edilmiş olanlar listeye GİRMEZ |
| `POST /islemler/satis-iade` | Mal stoğa, para kasadan geri |
| `POST /islemler/alis-iade` | Aynası: mal stoktan, para kasaya (MUHASEBE+) |

**MALİYET — `MaliyetMotoru.PartiIade`.** İade edilen mal **ÇIKTIĞI maliyetle**
geri girer. Yeni parti açılsaydı, altın yükselirken iade alınan mal daha
pahalı maliyetle stoğa girer ve satılmamış bir malda sahte kâr/zarar doğardı.
Tüketim satırı SİLİNMEZ; **negatif adetli ters kayıt** eklenir — silinseydi
"bu parti ne zaman tüketildi, ne zaman geri geldi" tarihçesi yok olurdu.

**Kapılar:** kalan iade hakkı (aynı satış iki kez tam iade edilemez) · iade
tutarı orantılıyı AŞAMAZ (aşabilseydi iade, kasadan para çıkarmanın
denetimsiz yolu olurdu) · alış iadesinde stok yeterliliği · gün kilidi ·
tevsik (iadede para kasadan ÇIKAR, tediye de hadde tabidir) · MASAK kümülatifi.

**Mutasyon kanıtı: 8 adet** (M42–M49) — önceki iadeler sayılmayınca · tutar
tavanı kalkınca · parti geri konmayınca · ters kayıt yerine pozitif yazılınca ·
alış iadesinde stok kapısı kalkınca · ekranda kalan hak kapısı kalkınca ·
kaynak seçilmeden gönderilebilince · orantılı tutar hesabı bozulunca → kırmızı.

⚠ **İlk denemede M47/M48 hiçbir testi kırmadı.** Testler YANLIŞ SEBEPTEN
geçiyordu: Kaydet düğmesi zaten kasa seçilmediği için kapalıydı, iade kapısı
kaldırılınca da kapalı kaldı. Testler izole edildi (`DigerKosullariSagla`) ve
"kalan 3 iken AÇIK, 4 iken KAPALI" biçimine getirildi; mutasyon tekrarlandı.

**CANLI KANIT:** 4 adet × 2.500 = 10.000 TL satış → 1 adet iade **200**
(2.500 TL, maliyet 1.000 TL geri) → 5 adet iade **400** ("kalan 3,000") →
9.000 TL tutar **400** ("en fazla 2.500,00") → kalan 3 adet **200** →
tekrar iade **400** ("tamamı zaten iade edilmiş").
Defter: net TL −4.000 (yalnız alış maliyeti), net has +40 (dört ürün stokta),
parti 4/4 geri kondu, tüketim satırları +4 / −1 / −3 = **net sıfır**.

### B) Kâr-zarar + stok kapsamında kalan 3 paket

| Paket | Büyüklük | Not |
|---|---|---|
| Çift birim stok (adetli + gramlı aynı üründe) | 3–4 gün | Ürün kartına birim alanı + bağımsız gram stok |
| Sayım oturumu — toplu barkod okutma + fark raporu | 4–6 gün | Deftere yazma kısmı BİTTİ (TB-18) |
| Pırlanta 4C + sertifika arşivi | 4–6 gün | Önce **dosya deposu altyapısı** gerekiyor |

### C) Mühürlü parite listesinden kalanlar
`PARITE-IS-LISTESI-MUHURLU-2026-09-14.md` — 48 A-sınıfı paket, 149–220 iş günü. Kâr-zarar + stok dışındaki katmanlar (imalat/atölye, satış tipleri, cari-finans, rapor seti, uyum, platform) henüz başlanmadı. **En büyük açık: imalat/atölye — fire kavramı bile yok.**

### D) Kurucuya bağlı, bizim elimizde değil
- **VB-02**: `kuyumcu_net`'te 2 satırlık sızma testi kalıntısı ("Kurban Kasa", "Kurban Musteri"). Silinmeden **FK dalgaları başlayamaz**. Onay bekliyor.
- **TB-01/K9**: `appsettings` dosyalarında commit'li 2 sır (devirden). Biz 3'üncüyü ekledik (şifreleme anahtarı) — dosya `.gitignore`'da ama **git hâlâ izliyor**; dikkatsiz bir `git add` geçmişe yazar.
- **T7**: 1.408 `bin/obj` dosyası hâlâ depoda izleniyor.
- K1 e-Fatura sözleşmesi · K2 mali müşavir teyidi · K4 pilot · K5 MASAK Online · K10 mülkiyet belgesi
- **Commit kararı**: bugün depoyu temiz makineye kopyalayan biri yalnız 9 Eylül'deki hâli alır — 34 belgeyi, 662 testi, uyum katmanını değil.

---

## 5. ÇALIŞMA ORTAMI

```bash
# Docker + MySQL
open -a "Docker Desktop"          # kapalıysa
docker exec kuyumcu-mysql mysqladmin ping -uroot -p'<parola>' --silent

# Sunucu (5000 portu macOS AirPlay'de dolu — 5199 kullan)
cd /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api \
  --no-build --no-launch-profile --urls http://localhost:5199

# Kapılar
dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo
./denetim.sh          # kiracı izolasyonu + güvenlik yüzeyi
./yetim-tara.sh       # yetim kayıt + kiracı sızıntısı (çıkış 2 = güvenlik olayı)
k6 run yuk-testi/k6-senaryo.js

# Migration (ASPNETCORE_ENVIRONMENT yeterli değil, bağlantıyı AÇIKÇA ver)
dotnet ef database update --project Kuyumcu.Api \
  --connection "server=localhost;port=3306;database=kuyumcu_net;user=root;password=<parola>"
```

**Demo hesabı:** `demo-1789378226@bygatech.com` / `Gucluparola1!` (Demo Kuyumcu, SAHİP) — bu oturumda kâr-zararı göstermek için açıldı, içinde 2 alış + 2 satış var. Diğer kiracıların parolaları bilinmiyor (BCrypt).

**Canlı doğrulamada takılacağın kapılar (doğru davranış, veriyi ona göre kur):**
- MASAK: 185.000 TL üstü işlem kimlik tespiti ister
- Tevsik: 30.000 TL üstü **nakit** ödeme reddedilir → `odemeTuru: "HAVALE"` kullan
- Taş kökeni: taş ağırlığı girilmiş ürün kökensiz satılamaz
- Konsinye: sahibi/bedeli olmadan kaydedilemez

### A11) VADE TAKİBİ — paket 28 BİTTİ (2026-09-15)

İki açık birden kapandı:
1. **Vade yoktu.** Yaşlandırma raporu borcun KAÇ GÜNDÜR durduğuna bakıyordu;
   90 gün vadeli 60 günlük borç "61-90" diliminde, yani geciken gibi görünüyordu.
   Artık borç doğuran her veresiye satırı `hareketler.vade_tarihi` (DATE — yerel
   takvim günü, K5 tuzağına düşmesin) taşır. Vade: işlemdeki `vadeGun` → carinin
   `varsayilan_vade_gun` → 0 (aynı gün muaccel). Peşin/iade/tahsilat satırı vade
   TAŞIMAZ (fabrika değişmezi).
2. **Kredi limiti yalnız `CariModulu.VeresiyeGir`'de işliyordu.** Veresiye SATIŞ,
   karma fişin veresiye bacağı, takas/işçilik farkı limiti HİÇ görmüyordu; 1.000 TL
   limitli müşteriye ekrandan 50.000 TL veresiye satılabiliyordu. Tek kapı:
   `Modules/Vade/VadeKapisi.cs` (`Hazirla` = vade + limit; `KrediLimitiDogrula`;
   `AcikBorclar`). Alış/hurda (BİZİM borcumuz) vadeyi yazar, limiti uygulamaz —
   tedarikçi carisindeki limitin yönü veride ayrışmıyor, varsayım yapılmadı.

**Derleyici zorlaması:** `OdemeIzi(Nakit, Veresiye, Vade)` üçüncü konumsal alan.
`OdemeIzi.Veresiyeden(tutar, vade)` — vade ZORUNLU, varsayılanı yok; borç azaltan
yön için ayrı isim `VeresiyeKapanisi(tutar)`. 11 yazma noktası derleyici tarafından
tek tek yakalandı ve elden geçirildi. `denetim.sh`: `VeresiyeKapanisi(` = 2 (yalnız
tanım + `Etiketten` içi). Ekran: Cariler düzenle modalına **kredi limiti + varsayılan
vade** alanları (limit başından beri sunucudaydı, HİÇBİR ekranda girilemiyordu),
İşlemler'de yalnız borç doğuran işlemde görünen `Vade (gün)` girdisi, yeni sayfa
`/raporlar/vade-takibi` (geciken üstte · yaklaşan · vadesi bilinmeyen AYRI sütun).

**Uçlar:** `GET /raporlar/vade-takibi?ufukGun=7` → `{gecikenToplam, yaklasanToplam,
vadesizToplam, cariler:[{..., borclar:[{vade, acikTutar, gecikmeGun, durum}]}]}`.
Yalnız `tl_bakiye > 0` (bize borçlu) cariler; tedarikçi borcu bu ekranın işi değil.
DTO'lara `vadeGun` (0–3650): Satis, SatisFisi, Alis, HurdaAlis, Takas, Iscilik,
VeresiyeGiris; `Cari` create/patch: `varsayilanVadeGun`. Migration
`20260915115025_VadeTakibi` `kuyumcu_net`'e uygulandı.

**Açık borç türetimi (FIFO) — CANLI DOĞRULAMADA KUSUR BULUNDU VE DÜZELTİLDİ:**
İlk tasarım "bakiye otoritedir, vadeli satırlara yeniden eskiye dağıt" idi. Canlıda
4.000 tahsilatla kapanmış borcun satırı aday havuzunda kaldığı için, sonradan gelen
1.500 TL vadesiz bakiye artışı çoktan ödenmiş ESKİ borca yazıldı; müşteri hiç
gecikmediği hâlde "12 gün gecikti" göründü ve `vadesizToplam` 0 çıktı. Algoritma
**defteri eskiden yeniye yürüyen parti/FIFO** modeline çevrildi: borç doğuran satır
parti açar, azaltan satır (TAHSILAT tam TL bacağı; diğerlerinde veresiye payı) en
eski partiden tüketir; defter–bakiye farkı ASLA yutulmaz, "vadesi bilinmeyen" olarak
dışarı verilir. Mahsup tablosu yine KASTEN yok (ona yazmayan her yol bakiyeden
sapardı). Regresyon testi: `Odenmis_borc_sonraki_vadesiz_bakiyeyi_UZERINE_ALMAZ`.
Fazla tahsilat = bizim müşteriye borcumuz → vadesiz olarak döner, gecikme değil.

**Canlı (VADE-TEST kiracısı, silindi):** limit 10.000 · veresiye 4.000 (vade 15 g →
30 Eyl) **200** · 8.000 daha → **400** "Kredi limiti aşılıyor. Mevcut bakiye: 4000 TL,
Eklenecek: 8000 TL" · vade verilmeden satış → carinin varsayılanı 30 g (15 Eki) ·
tahsilat 4.000 → EN ESKİ (30 Eyl) kapandı, 15 Eki kaldı · vade 3 Eyl'e çekilince
12 gün GECİKTİ · karma fiş 1.000 nakit + 3.000 veresiye, 2 kalem → her kalemde
5 Eki, paylar 500+2.500 · son rapor: geciken 4.000 · yaklaşan 3.000 · vadesiz 1.500
(birebir beklenen). `vadeGun=-1`/`4000` → 400 (Range), cari yokken `vadeGun` → 400;
üçü de hiçbir satır yazmadı (SQL ile sayıldı). Cari PATCH: alan gövdede yoksa vade
KORUNUR, `null` gönderilince silinir.

**Testler:** `VadeTakibiTestleri` (21) + `VadeEkranTestleri` (5). Mutasyon
**M50–M67** kırmızı: vade yazılmıyor · limit atlanıyor · limit bakiyeyi görmüyor ·
varsayılan vade yoksayılıyor · işlem vadesi ezilmiyor · FIFO ters · vadesiz bakiye
yutuluyor · gecikme ters · bugünkü vade gecikmiş sayılıyor · fabrika veresiyesiz
vade kabul ediyor · defter ters yürünüyor · defter–bakiye farkı yutuluyor · tahsilat
borcu kapatmıyor · rapor tedarikçi borcunu alacak sayıyor · ekran: sıralama, ufuk
sunucuya sorulmuyor, vadesiz gizleniyor. **Dürüst notlar:** M58 ("peşin satışa vade
yazılıyor") TEK mutasyonla kırılmadı — üç bağımsız katman koruyor (çağrı yeri
`vade: null`, `Etiketten` `_ => Yok`, fabrika değişmezi); üçü birden bozulunca
`Pesin_satis_vade_YAZMAZ` + fabrika testi kırmızıya döndü. M67 gerçek test açığıydı
→ `Rapor_BIZIM_tedarikciye_borcumuzu_LISTELEMEZ` eklendi, tekrar koşuldu, kırmızı.
Tam süitte 1/4 kırmızı çıkan `ALIS_secicisinde_ALTIN_ve_VERESIYE_YOKTUR` (izole
15/15 yeşil): `TurSec` içindeki ateşle-unut `IadeAdaylariYukle()` yük altında okuma
sırasında yeniden çizim tetikliyordu → `WaitForAssertion` ile elemanı her denemede
yeniden bulur; sonrasında 3 ardışık tam koşu 880/880.

**Kalan açıklar (koda yazıldı):** tedarikçi vadesi (bizim borcumuz) için ekran yok —
`AcikBorclar` yön-duyarlı, rapor yalnız alacak; vade alanı öncesi eski satırlar
"vadesi bilinmeyen" kalır (geri doldurma yapılmadı — vade sözü veride yok, uydurulmaz);
ALTIN ödeme ve TOCTOU (A9'daki gibi) değişmedi. `Kuyumcu.Tests/VadeTakibiTestleri.cs.ek`
BOŞ artık dosya (derlenmez, `.ek`) — hook `rm` bloklar, kurucu elle silsin.

### A12) DÖVİZLE TAHSİLAT — paket 15'in YASAL biçimi BİTTİ (2026-09-15)

**Plan "döviz alış/satış (sarraflık)" diyordu; KODLANMADI.** Yetki belgesiz kuyumcu
için ticari döviz alım-satımı yasak ve aktif denetlenen fiil: 32 Sayılı Karar
md.4/(b) ve md.6/(3) (yalnız banka, PTT, yetkili müessese, BİAŞ üyesi kıymetli maden
aracı kuruluşu), Tebliğ 2018-32/45 md.13/(3), 1567 s. Kanun md.3/(5) → 2026'da
**719.030–3.595.198 TL** + 1–6 ay kapatma; Bakanlık 22.02.2026: "kuyumcuların döviz
alım satım yetkisi bulunmuyor", 2025'te 147 kapatma. Kaynaklar ve gerekçe:
`PAKET-15-DOVIZ-KARAR-2026-09-15.md`. Yasal kapı: Tebliğ 2008-32/34 md.8
(2025-32/72, RG 06.03.2025) menkul satış bedelinin dövizle kararlaştırılmasına izin
verir → **altın bedelinin efektif dövizle TAHSİLİ** kodlandı; döviz satışı/alışı ucu
YOK, tahsil edilen dövizin tek çıkışı bankaya devir (mevcut TRANSFER).

**Nasıl:** `OdemeBacagiDto` yeni araç **DOVIZ** (`paraBirimi` USD/EUR/GBP +
`dovizTutar` efektif miktar). TL karşılığı **sunucuda** işlem anındaki kur kaydından
(`KurSnapshot.UsdTry/EurTry/GbpTry`) hesaplanır ve istemcinin gönderdiğiyle KURUŞU
KURUŞUNA eşit olmalıdır — kasiyer kur seçemez (seçebilseydi her fiş fiilen döviz
ticareti olurdu). Kur o gün çekilmemişse **fail-closed** red. Döviz kasasına
**efektif miktar** yazılır (USD), TL değil (K6); TL karşılığı hareket satırında
(`TlTutari` + `KurSnapshotId`) durur → kasa bakiyesi de kur da defterden türetilir.
`OdemeSepeti.Coz` 4. konumsal parametre `DovizKurlari` (varsayılanı yok). Kasa para
birimi bacağa göre doğrulanır (dolar avro kasasına giremez; aynı kasa iki farklı
birimle bağlanamaz). **Tevsik:** efektif döviz NAKİT sayılır (`NakitToplam`'a dahil,
`nakit_tutar`'a yazılır) — yabancı paranın hadde hangi kurla çevrileceğine dair fıkra
birincil metinden teyit edilemedi; bu yüzden işlem anındaki kur kullanılır ve döviz
DAİMA nakit sayılır (kapı yalnız sıkılaşır). Ekran: karma fişte DOVIZ bacağı — birim
seçici, miktar girdisi, **salt okunur TL** (kurdan), kasa listesi bacağın para birimine
filtreli; gövdeye `paraBirimi`/`dovizTutar` eklenir. Tek bacaklı `Satis`/`Alis`/
`Hurda` uçlarına DOVIZ eklenmedi (tedarikçiye dövizle ödeme kapsam dışı). Migration
YOK (mevcut sütunlar yetti).

**Canlı (DOVIZ-TEST kiracısı, silindi; usdTry=48,71):** 100 USD + 1.000 KART, fiş
5.871 → **200**; Dolar Kasa **100,00 USD**, `kasa_hareketleri` USD 100 DOVIZ, SATIS
5.871 TL / nakit 4.871 / kur kaydı eşit, TL kasa −19.000, stok 9 · TL 4.871,01 → **400**
"100,00 USD × 48,7100 = 4.871,00 TL, gönderilen 4.871,01" · USD bacağı avro kasasına →
**400** · TL kasasına → **400** · 800 USD = 38.968 TL → **400** tevsik · NAKIT bacağında
`dovizTutar` → **400**; beş red sıfır satır yazdı (SQL).

**Testler:** `OdemeSepetiTestleri` +8, `SatisFisiTestleri` +5 (DB), `DovizBacagiEkranTestleri`
5. Mutasyon **M68–M75** kırmızı: kur denetimi yok · döviz nakit sayılmıyor · kur yokken
kur=1 · kasa para birimi denetlenmiyor · döviz kasasına TL · ekran TL elle yazılabilir ·
ekran kasa filtresi yok · ekran kur yokken miktar TL sayılıyor. 898/898 ×3, denetim TEMİZ.

**Kalan açıklar:** tedarikçiye dövizle ödeme (alış/hurda) yok; döviz kasasından bankaya
devir mevcut TRANSFER ile — döviz bozdurma (banka DAB) kaydı için ayrı uç yok;
GBP kuru kaynakta varsa çalışır, yoksa fail-closed. "Tahsil edilen döviz üçüncü kişiye
satılamaz" hükmü birincil metinde bulunamadı — varsayılmadı, yalnız o yönde uç açılmadı.

### A13) FİYAT LİSTESİ + KAMPANYA + HEDİYE — paket 17 BİTTİ (2026-09-15)

Karar ve mevzuat: `PAKET-17-FIYAT-LISTESI-KARAR-2026-09-15.md`. **Fiyat = has×kur×(1+marj%) +
işçilik×çarpan%**, marj LİSTEDE (Faz 0 §11'in (a) koşulu açıldı; (c) parite eşleşmesine
dokunulmadı). Hesap SUNUCUDA: `Modules/Fiyat/FiyatMotoru.cs` (saf) + `FiyatModulu.cs`
(`FiyatServisi`, uçlar). POS'taki `OtomatikFiyat` istemci formülü SİLİNDİ; tek kaynak
`GET /fiyat/teklif?urunId&cariId&kurSnapshotId` → kırılım (liste/has/marj/işçilik/indirim/
indirim öncesi). Teklif yoksa alan boş + sebep (`LISTE_YOK` / `FIYAT_YOK`); rakam uydurulmaz.
Gümüşe altın kuruyla fiyat yok (motor kural 2), kur yoksa yok (kural 3).

**Varlıklar:** `fiyat_listeleri` (ad, tür, has_marj_yuzde, iscilik_carpan_yuzde, varsayilan,
aktif), `kampanyalar` (başlangıç/bitiş DATE, hedef TUMU/KATEGORI/URUN, indirim ISCILIK_YUZDE/
TUTAR_YUZDE/TUTAR_TL, deger); `cariler.fiyat_listesi_id`; `hareketler.liste_fiyati`,
`indirim_tutari`, `kampanya_id`, `hediye`. Migration `20260915132909_FiyatListesiVeKampanya`
kuyumcu_net'e uygulandı. Kiracının ilk listesi otomatik varsayılan; varsayılan pasife
alınamaz; cariye atanan liste varsayılanı ezer. Kampanya ≤ **92 gün** (FEY md.11/3), hedef
doğrulanır, kategori hedefi alt kategorileri kapsar, birden fazla uygunsa EN YÜKSEK tek
indirim (toplanmaz). Satış satırına liste fiyatı + indirim + kampanya yazılır (FEY md.11/1-2);
**indirim öncesi fiyat = son 30 günün en düşük liste fiyatı** (TRHTUY md.14; defterden
türetilir, ayrı tablo yok). **Hediye:** fişte `hediye=true` → sunucu fiyatı 0'a zorlar,
indirim = liste×adet (KDV GUT tutar iskontosu), stok/maliyet düşer, kampanya damgası
TAŞIMAZ; **tek başına hediye 400** (KDVK md.27 emsal bedel). Hediye yalnız fiş ucunda;
POS hediye varsa tek bacaklı fişe döner; ALTIN ödeme ile hediye çakışır → Kaydet kapalı.
Ekranlar: `/fiyatlandirma` (listeler+kampanyalar, 92 gün ekranda da kilitli), Cariler
modalında fiyat listesi seçici, POS'ta kırılım + kalem hediye kutusu.

**Canlı (FIYAT-TEST, silindi; has 6.718,66):** liste yokken `LISTE_YOK` · ilk liste otomatik
varsayılan · teklif **65.120,08** = 61.542,93 + %5 3.077,15 + 500 (perl ile bağımsız
hesaplandı, birebir) · 95 gün kampanya → **400** "en fazla 92 gün… md.11/3" · 250 TL kampanya
→ indirim 250, satış 64.870,08, indirimOncesi 65.120,08 · fiş bedelli+hediye → **200**;
SQL: bedelli satır liste 65.120,08 / indirim 250 / kampanyalı; hediye satırı **0 TL** /
indirim 65.120,08 / hediye=1; stok 8 · tek başına hediye → **400**, satır yok · toptancıya
Toptan listesi (marj −1, işçilik 0) → marj **−615,43**, işçilik 0 · varsayılan liste pasife
→ **400**. Canlıda görülen kusur: hediye satırı kampanya kimliği almıştı → düzeltildi (M90).

**Testler:** `FiyatMotoruTestleri` 12 (saf), `FiyatListesiTestleri` 11 (DB),
`FiyatlandirmaEkranTestleri` 5, `PosTeklifEkranTestleri` 4, CariEkran +1; `PosBarkodTestleri.
Fiyat_CANLI_kurdan_hesaplanir` → `Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir`
(kural değişikliği, gerekçe testte). Mutasyon **M76–M90** (15) kırmızı. 931/931 ×5,
denetim TEMİZ (28 varlık), 0 uyarı.

**Kalan açıklar:** hediye çeki/kupon ve sadakat programı kapsam dışı (ayrı paket);
kampanya "indirim öncesi" gösterimi etiket/fiş yazdırmada henüz basılmıyor (alanlar
satırda var; UrunEtiketleri/fiş şablonu sonraki adım); tedarikçi fiyat listesi (alış) yok;
gümüş gram kotasyonu + marj hâlâ yok (Faz 1 notu geçerli).

### A14) SAYIM OTURUMU — paket 23 BİTTİ (2026-09-15)

Tek ürün sayımı (TB-18, `UrunServisi.StokSayimGuncelle`) zaten deftere yazıyordu; oturum,
vitrini/reyonu/kategoriyi toptan sayıp FARK RAPORU verir. `Data/SayimOturumu.cs`
(`sayim_oturumlari`, `sayim_satirlari` — `(tenant, oturum, urun)` benzersiz),
`Modules/Sayim/SayimModulu.cs`, `/sayim` sayfası. Migration `20260915143225_SayimOturumu`
kuyumcu_net'e uygulandı. VUK md.186 envanter / md.253 saklama: oturum silinmez, İPTAL olur.

**Kurallar:** açılışta kapsamdaki her ürünün sistem adedi satıra DONDURULUR (stok satırı yoksa 0)
· aynı şubede ikinci açık oturum 400 · sayım ürün kimliği ya da barkodla, tekrar sayım üzerine
yazar · rapor: sistem (donmuş) · sayılan · GÜNCEL · fark · has farkı · "oturumda hareket var"
(açılıştan sonra o üründe SAYIM_DUZELTME dışı hareket) · **kapanışta sayılan ve fark ≠ 0 satırlar
TB-18 yoluyla SAYIM_DUZELTME olur, satıra hareket bağlanır; SAYILMAYAN SATIR SIFIRLANMAZ**
(bilinmeyen ≠ sıfır) · **hareketli satır yalnız `hareketliSatirlariUygula=true` ile uygulanır**
(sayılan güncel stoğun üzerine yazılır; ekranda uyarı + onay kutusu) · kapanmış/iptal oturumda
say/kapat 400 · kapsam hedefi (reyon/kategori, alt kategoriler dahil) doğrulanır.

**Canlı (SAYIM-TEST, silindi):** 2 ürün × 5 stok · oturum 2 satır/2 sayılmadı · ikinci oturum
**400** · barkodla 3 → fark −2, has −18,32 · oturumda 1 satış → hareketli 1, güncel 4 · onaysız
kapat → uygulanan 0 / atlanan 1, stok 4 ve 5 (dokunulmadı) · kapanmışta say **400** · yeni oturum
2 + onaylı kapat → uygulanan 1, `SAYIM_DUZELTME 0 TL / −18,32 has "4 → 2"`, stok SY-1 **2**, SY-2 **5**.

**Testler:** `SayimOturumuTestleri` 8 (DB), `SayimEkranTestleri` 5. Mutasyon **M91–M96** kırmızı
(ikinci oturum · sayılmayan sıfırlanıyor · onaysız uygulama · kapalı oturum · defter yazımı ·
ekran onayı). 944/944 ×3, denetim TEMİZ (30 varlık), 0 uyarı.

**Kalan açıklar:** sayım tutanağı PDF/yazdırma yok · reyon kapsamı `urunler.reyon_id`
üzerinden (paket 25 stok bölümlemesi gelince satır bazına iner) · gramlı (çift birim) sayım
paket 21 ile.

### A15) ÇİFT BİRİM STOK — paket 21 BİTTİ (2026-09-15)

`urunler.birim` (ADET | GRAM, varsayılan ADET). **Sözleşme: GRAM üründe 1 adet = 1 gram** —
`GramAgirlik` sunucuda 1'e zorlanır (istemci ne gönderirse göndersin), taş 0, `HasGrami`
gram başına has olur. Böylece stok `adet` (decimal 12,3), `adet × HasGrami` ve
`adet × birimFiyat` hesapları HİÇ değişmeden gramlı stoğu taşır; iki ayrı has yolu açılmadı
(iki yerde iki farklı yuvarlama olurdu). Güncellemede birim GRAM'a çekilince gram 1'e iner ve
has yeniden hesaplanır (`hasEtkileyen` koşuluna `Birim` eklendi — ilk sürümde eksikti, test
yakaladı). Ekranlar: ürün formunda Birim seçici (GRAM'da gram girdisi kilitli), POS miktar
girdisi gramlı üründe 0,001 adımlı ve " g" etiketli, sayım girdisi birime göre adımlı; sayım
raporu satırlarında `birim`. Migration `20260915145346_UrunBirim` kuyumcu_net'e uygulandı.

**Canlı (BIRIM-TEST, silindi):** GRAM ürün gramAgirlik 250 gönderildi → **1,0000 / has 0,995** ·
10 g alış, **2,5 g satış → 16.250 TL, has −2,4875**, stok 7,5 · gramlı sayım 7,2 → fark **−0,300 /
−0,2985 has**, kapanış SAYIM_DUZELTME, stok 7,2 · ADET ürün 7,5 g / ADET (etkilenmedi).

**Testler:** `UrunBirimTestleri` 4 (DB). Mutasyon **M97–M98** kırmızı. 948/948, denetim TEMİZ,
0 uyarı. **Mevzuat:** Kuyum Ticareti Yönetmeliği'nde grama özgü belge hükmü bulunamadı;
mevcut fiş/etiket ayar+gram taşıyor.

**Kalan açıklar:** "aynı üründe adetli + gramlı" (PARITE 21 ifadesi) = iki birim aynı kartta
DEĞİL; gramlı ürün ayrı karttır — bilinçli sadeleştirme. Gram fiyat listesi/teklif: gramlı
üründe `FiyatMotoru` has×kur×marj yolu doğal çalışır (has/gram), işçilik gram başına.

### A16) PIRLANTA 4C + SERTİFİKA PDF — paket 22 BİTTİ (2026-09-15)

Karar: `PAKET-22-PIRLANTA-KARAR-2026-09-15.md`. `urunler`: `pirlanta_karat/renk/berraklik/kesim`,
`sertifika_kurumu/no`; kapalı listeler `Core/Domain/Pirlanta.cs`; kapı `Modules/Urun/PirlantaKapisi.cs`
— **PIRLANTA tipinde karat zorunlu (fail-closed), PIRLANTA dışı tipe 4C 400, sertifika no için kurum
zorunlu, DIGER kurum `DIGER:ad`**. Sertifika PDF **veritabanında** (`urun_belgeleri`, LONGBLOB ≤ 5 MB,
sha256, `%PDF-` magic byte denetimi — içerik türü beyanına güvenilmez); silme yok, pasife alma.
Uçlar: `GET/POST /urunler/{id}/belgeler` (base64 JSON), `GET /urunler/belgeler/{belgeId}/icerik`
(dosya akışı), `DELETE …/belgeler/{belgeId}` (pasif). Etiket projeksiyonunda `pirlantaOzet`
("0,52 ct · G · VS1 · EX") — FEY md.5/(2)-b "ayırıcı özellik". Ürün formunda PIRLANTA seçilince 4C
alanı; karatsız Kaydet kapalı. Migration `20260915162035_PirlantaVeBelge` kuyumcu_net'e uygulandı.

**Canlı (PIRLANTA-TEST/TEST2, silindi):** karatsız → **400**; 4C+GIA → 200, SQL birebir; ZIYNET+karat →
**400**; etiket `pirlantaOzet` doğru; PDF yükleme 200 (sha yanıtta); PDF olmayan → **400** "imza %PDF-
bulunamadı"; indirilen içerik `application/pdf`, imza `%PDF-1.7`, **SHA-256 kaynakla birebir**; DELETE →
liste boş, satır aktif=0 (silinmedi); yabancı kiracı içerik → **404**. İlk canlı turda 7/8 adımları benim
betiğimin `"Id"`/`"id"` hatası yüzünden SPA'ya düşmüştü (text/html) — ürün hatası değildi, doğru kimlikle
yinelendi ve kanıtlandı.

**Testler:** `PirlantaKapisiTestleri` 6 (saf), `PirlantaBelgeTestleri` 2 (DB), UrunEkran +1. Mutasyon
**M99–M103** kırmızı. 957/957 ×3, denetim TEMİZ (31 varlık), 0 uyarı.

**Kalan açıklar:** ürün DÜZENLEME modalında 4C/sertifika alanları ve PDF yükleme ekranı yok (uçlar var,
oluşturma formu var); GIA Report Check otomatik doğrulama yok; yan taşlar için çoklu 4C yok (tek ana taş).

### A17) paket 33 MANUEL DÜZELTME FİŞİ — 2026-09-15 YARIM KALMIŞTI, 2026-09-16 BİTTİ (tarihçe aşağıda; kanıt A17-BİTTİ bölümünde)

**Hedef:** `POST /api/v1/cari/{id}/duzeltme` — gerekçeli (≥ 20 karakter) elle bakiye düzeltmesi;
deftere `CARI_DUZELTME` hareketi; `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`; para/vade izi YOK
(`OdemeIzi.Yok` — vade raporunda "vadesi bilinmeyen" görünür, bilinçli); borcu ARTIRAN delta kredi
limitine tabi (`Vade.VadeKapisi.KrediLimitiDogrula`), azaltan değil; MASAK yok (para el değiştirmez,
SAYIM ile aynı gerekçe). Hareket işaretleri VERESİYE sözleşmesiyle aynı: `TlTutari = tlDelta`,
`HasGrami = -hasDelta`; bakiye `BakiyeGuncelle(id, tenant, tlDelta, hasDelta, now)`; `Aciklama`
gerekçeyi içerir; `kurSnapshotId` zorunlu. Sıfır delta (tl=0 ve has=0) → 400.

**YAZILDI (derlenmedi, koşulmadı):**
- `dotnet/Kuyumcu.Tests/CariDuzeltmeTestleri.cs` — 4 test; **`MCari.CariDuzeltmeDto`** ve
  **`CariServisi.Duzeltme(string id, CariDuzeltmeDto dto, AktifKullanici islem)`** imzasını bekliyor.
  DTO alanları: `TlDelta` (decimal), `HasDelta` (decimal), `Gerekce` (string, ≥20), `KurSnapshotId`.
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor` — "✍ Düzeltme Fişi" düğmesi, modal (`#dz-tl`, `#dz-has`,
  `#dz-gerekce`, `#dz-kaydet`; gerekçe <20 ya da iki delta 0 → Kaydet kapalı), `DuzeltmeGonder()`
  gövdesi `{ tlDelta, hasDelta, gerekce, kurSnapshotId = kurId }`. ⚠ `kurId` bu sayfada YOK —
  tahsilat `await GuncelKurId()` kullanıyor; `DuzeltmeGonder` içinde de `kurSnapshotId = await GuncelKurId()`
  yapılmalı (satır ~599).
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `Cari.Duzeltme/DuzeltmeIpucu/TlDelta/HasDelta/Gerekce` TR+EN eklendi.

**YAZILMADI (sırayla yapılacak):**
1. `Kuyumcu.Api/Modules/Cari/CariModulu.cs`: `CariDuzeltmeDto` (Range/MinLength(20) Gerekce), `Duzeltme`
   servis metodu (desen: `VeresiyeGir` satır ~380–425; `HareketFabrikasi.Ekle(db, OdemeIzi.Yok, …)`,
   `Tur = "CARI_DUZELTME"`), denetleyicide `[HttpPost("{id}/duzeltme")] [RolGerekli("SAHIP","MUDUR","MUHASEBE")]`.
   Denetim izi: `DenetimServisi` (Data/DenetimIzi.cs:87) yalnız `OkumaKaydet/ListeOkumaKaydet` sunuyor —
   yazma için ayrı metod YOK; hassas yazma kaydı istenirse önce oraya `YazmaKaydet` eklenmeli (ya da
   `hareketler` satırı + `Aciklama` gerekçesi yeter sayılıp not düşülmeli — karar verilmedi, VARSAYMA).
2. `Kuyumcu.Api/Data/Entities.cs` `HareketTurleri` dizisine `"CARI_DUZELTME"` ekle (liste hiçbir yerde
   zorlanmıyor ama doğru kalsın). `MasakKapisi` bağlantılı işlem tür listesine EKLEME (para yok).
3. `Cariler.razor` `kurId` → `await GuncelKurId()` düzelt; ekran testi (`CariEkranTestleri`: modal açılır,
   gerekçe kısa → kapalı, gövde alanları) yaz.
4. Migration GEREKMEZ (yeni sütun yok). `dotnet build` → `dotnet test` (0 başarısız) → mutasyonlar
   (M104 gerekçe kontrolü kaldırıldı · M105 limit kontrolü kaldırıldı · M106 sıfır delta kabul ·
   M107 rol kaldırıldı — HTTP testi gerekir · M108 ekran gerekçe kilidi) → temiz derleme → tam süit ×3
   → `./denetim.sh` → canlı (yeni kiracı, düzeltme 200/400'ler, SQL, kiracı SİL, kiracı sayısı 5) →
   bu belgeye A17'yi BİTTİ olarak yaz.

**Sonrası (Katman 4 kalanlar):** 27 çoklu birim cari bakiye (USD/EUR — TL+has var), 29 çek/senet,
31 POS komisyon/valör, 32 banka ekstresi içe aktarma. Sonra Katman 5 raporlama.

**Elle silinecek artıklar (hook `rm` bloklar):** `dotnet/Kuyumcu.Api/Data/Migrations/2026091513{1748,2123,2303,2645}_FiyatListesiVeKampanya(.Designer).cs`
(0 bayt, 8 dosya), `dotnet/Kuyumcu.Tests/VadeTakibiTestleri.cs.ek` (0 bayt).
**kuyumcu_net:** 5 kiracı (3 temel + "Demo Kuyumcu" + "Ramazan Deneme" — bu oturumdan önce vardı, dokunulmadı).

---

### A17-BİTTİ) PAKET 33 TAMAMLANDI (2026-09-16 00:30 — ekip oyu + Opus uygulayıcı + hasım doğrulayıcı + orkestratör kapısı)

**Ekip oyu (Bertan · Mehmet · Aşkın), Tamer Bey mührü:** Q1 denetim izi → **B** (Mehmet güvenlik önceliği: `DenetimServisi.YazmaKaydet(islem, varlikTuru, varlikId, detay, hassas=true)` eklendi; `Olustur()` yolunu kullanır, SaveChanges çağırmaz, çağıranın tx'inde yazılır; `Duzeltme` bunu `VarlikTuru="CariDuzeltme"`, `Detay="tl=+1250 has=-0.5 · <gerekçe>"` ile çağırır) · Q2 HareketTurleri → **B** (`CARI_DUZELTME`, `SATIS_IADE`, `ALIS_IADE` eklendi; `SERTIFIKA` hareket türü değil, `UrunBelgesi.Tur`) · Q3 ekran → **B** (Cariler.razor'a OturumServisi; düğme yalnız SAHIP/MUDUR/MUHASEBE; sunucu 403 son kapı) · Q4 CSS → **B** (dokunulmadı; `.uyari-kutu` ZATEN app.css:392'de VAR — A17'deki "yok" notu YANLIŞTI).

**Kod:** CariModulu.cs `CariDuzeltmeDto` (:178) + `CariServisi.Duzeltme` (:555; sıra: TenantCarisiBul → gerekçe<20 → 400 → sıfır delta → 400 → KurDogrula → KrediLimitiDogrula (tx dışı, ≤0 atlar) → SubeId → tx → `HareketFabrikasi.Ekle(OdemeIzi.Yok, Tur=CARI_DUZELTME, TlTutari=+tlDelta, HasGrami=−hasDelta, Aciklama=gerekçe)` → `BakiyeGuncelle(+tl,+has)` → YazmaKaydet → SaveChanges → Commit) + `[HttpPost("{id}/duzeltme")] [RolGerekli("SAHIP","MUDUR","MUHASEBE")]` (:746). MASAK/tevsik çağrılmaz (para el değiştirmez). Migration gerekmedi. Cariler.razor:599 `kurId` → `await GuncelKurId()` (orkestratör; derlemeyi açan tek satır — Api Web'i referansladığı için bütün solution kırmızıydı).

**Testler:** 957 → **971** (+14): CariDuzeltmeTestleri 5 (4 + `HareketTurleri_listesi_deftere_YAZILAN_turleri_icerir`; ilk teste denetim izi assert'leri), CariEkranTestleri +6 (modal · 19→kapalı/20→açık · gövde JSON · TEZGAHTAR'da düğme yok), **HttpCariDuzeltmeTestleri (YENİ) 3** (TEZGAHTAR 403 · SAHIP 200 · kısa gerekçe 400). Tam süit: uygulayıcı 971/971 ×2, doğrulayıcı 971/971, orkestratör 971/971 (11 s, trx). 0 uyarı 0 hata.

**Mutasyon kanıtları M104–M109 (hepsi kırmızı, `cmp` ile birebir geri alındı):** M104 gerekçe<20 kaldırıldı → `Gerekce_20_karakterden_kisa_REDDEDILIR_ve_sifir_delta_REDDEDILIR` · M105 KrediLimitiDogrula kaldırıldı → `Borc_artiran_duzeltme_kredi_limitine_TAKILIR_azaltan_takilmaz` (doğrulayıcı bağımsız tekrarladı) · M106 sıfır delta kaldırıldı → aynı test · M107 `[RolGerekli]` kaldırıldı → `HttpCariDuzeltmeTestleri.TEZGAHTAR_duzeltme_fisi_kesemez_403` · M108 ekranda gerekçe kilidi kaldırıldı → `CariEkranTestleri.Gerekce_19_karakterse_KAYDET_KAPALI_20_karakterde_ACILIR` · M109 YazmaKaydet çağrısı kaldırıldı → `Duzeltme_deftere_CARI_DUZELTME_yazar_ve_bakiyeyi_delta_kadar_degistirir`.

**dotnet/denetim.sh:** TEMİZ (Hareketler.Add=1 · IgnoreQueryFilters=0 · new KuyumcuDbContext=1 · Find=0 · ExecuteUpdate/Delete=4 · VeresiyeKapanisi=2 · IKiraciVarlik=31 · HasQueryFilter=2).

**Canlı (port 5198, kiracı "DUZELTME-TEST Kuyumcu", SİLİNDİ; tenants=5):** kısa gerekçe → **400** · sıfır delta → **400** · TEZGAHTAR token → **403** · SAHIP +1250/−0,5 → **200** (yanıt okundu) · limit 1000 iken +1500 → **400** · SQL: hareketler tur=CARI_DUZELTME tl=1250 has=0,5 nakit=0 veresiye=0 vade NULL aciklama=gerekçe; cari_bakiyeler 1250/−0,5; denetim_kayitlari 2 satır (CariDuzeltme hassas=1 gerekçeli + otomatik Hareket EKLEME hassas=0). 5198 kapatıldı; 5199 (kurucunun inceleme sunucusu, `.claude/launch.json` "kuyumcu-api") dokunulmadı.

**Rapor:** `dokumanlar/09-uygulama-kayitlari/PAKET-33-uygulama-raporu.md`. **Kalan (bilinçli):** düzeltmeyle doğan borç vade raporunda "vadesi bilinmeyen"; HTTP yolunda kısa gerekçe 400'ünü DataAnnotations üretir (servis içi kontrol de var — savunma derinliği). **Kurucuya elle (hook `rm` bloklar):** 16 adet `*.mutorj` (önceki oturumun mutasyon yedekleri; kaynakta mutant YOK — 12'si özdeş, 4'ü sonraki paket 21/22 eklemeleri yüzünden farklı; ASLA kaynağa geri kopyalama), 8 sıfır baytlık migration, `VadeTakibiTestleri.cs.ek`. `mut.sh` ve 3 adet `kuyumcu_net-20260915-*.sql` yedeği /private/tmp altındaki *toner-tasarruf-platformu* scratchpad'inde (15 Eylül oturumu yanlış kökten açılmıştı) — kalıcı yere alınmalı; betik metni jewellery hafızasında `kuyumcu-mut-sh-betigi.md`.



### A18) OTONOM DALGA UYGULAMASI — kalan 27 paket + Katman 8 deneyim (2026-09-16'dan itibaren, SÜRÜYOR)

**Kurucu talimatı (2026-09-16 00:xx–07:xx):** "Soru sorma; kalan tüm işleri alt ajanlarla tartışarak ve denetleyerek bitir (kapsam dışı Katman 1 hariç); sonra sınırsız yetki: her veri girilen yer düzenlenebilsin, görsel çekicilik (modern/sade/şık), basit/profesyonel mod; işler bitince ekran testlerini tarayıcıdan tıklayarak yap; piyasayı araştır; sabaha kadar durma; kota dolarsa kaldığımız yerden devam."

**Düzen:** Her karar ekip oyuyla (Bertan mimari · Mehmet güvenlik/veto · Aşkın QA — Mehmet/Aşkın farklı model) → Tamer Bey mühürler. Uygulama **yaz-seri**: dalga içinde paketler SERİ; paket başına Opus uygulayıcı → hasım doğrulayıcı (farklı model) → ≤2 düzeltme turu → **orkestratör kapısı** (`dotnet build` 0/0 + `dotnet/denetim.sh` + tam süit trx). Kapıdan geçmeyen paket dalgayı durdurur. Betikler: `dokumanlar/09-uygulama-kayitlari/betikler/` (dalga-uygula.js · mimari-eksik-14.js · sentez.js · mimari-render.js · piyasa-arastirmasi.js). Kaldığımız yerin TEK KAYNAĞI: **`dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md`**. Ham JSON çıktılar: `dokumanlar/09-uygulama-kayitlari/ham/`.

**Yapılanlar (kanıtlı):**
- **Paket 33** → A17-BİTTİ.
- **UX-2 Faz A1 BİTTİ (dalga 0):** `wwwroot/css/belirtecler.css` (ham palet + semantik belirteçler + boşluk/yarıçap/gölge/tipografi), app.css değerleri `var()`'a bağlandı (seçici/özgüllük değişmedi; `.belge*`, `.urun-etiket-*`, `@media print` temadan MUAF — testle zorlanır), karanlık tema (`prefers-color-scheme` + `:root[data-tema]`), `TemaServisi` + `TemaSecici` (Ayarlar `#ayar-tema`), `js/tema.js` (adlandırılmış `kuyumcuTema.uygula`, localStorage beyaz liste, blazor'dan önce, eval/satır içi script YOK), `.dugme*` takma adları, `Layout/RotaBasliklari.cs` (27 rota, şablon eşleştirme, `_ => "Kuyumcu"` geri dalı kaldırıldı), dar ekran temel CSS, kilavuz.html belirteçlere bağlandı, Sozlukler TR+EN, "auto-generated" uyarısı düzeltildi. **İki gerçek hata:** DilServisi'nin `eval`i CSP'de engelliydi (dil değişimi HTML lang'ı güncellemiyordu) → `js/dil.js`; `.dugme*` sınıf ailesi CSS'te hiç tanımlı değildi, 17 düğme stilsizdi. Test 971 → **1027** (+56: TasarimBelirtec 12, RotaBaslik 33, Tema 11, GrafikKultur 4). Mutasyon M-UX2A1-01…05 kırmızı (iki test açığı bulunup kapatıldı). Kalan: 375px yatay kaydırma (UX-2 A2 çekmece). Rapor: `PAKET-UX-2-uygulama-raporu.md`.
- **Piyasa araştırması BİTTİ:** `PAZAR-ARASTIRMASI-2026-09-16.md` (152 kaynak). Kilit: 2026 e-Arşiv tutarsız zorunlu (bilanço esası) → K1 BLOKER; gerçek usule geçiş dalgası; Dezy 500 TL/ay + KuyumHub AI/açık bankacılık; hızlı kazanımlar: vitrin panosu yayını, WhatsApp derin bağlantı, KDV 23/e (805+808+230) — paket 38 öne alınır; sentetik taş ibaresi fatura satırına; mobil/PWA en görünür açık. Doğrulanamayan: KMTS perakende kapsamı, kart taksit kısıtı (koda yazılmadı).
- **Mimari:** 11 paket (1, 4, 34–42) + UX-1/2/3 tasarlandı, oylandı (kısmi vetolar: 1↔38 tek `OzelMatrahMotoru`; 40 Faz B KVKK; 39 kabul ölçütü; 41 dekont tarihi). 16 paket (3, 5, 6, 19, 20, 25, 27, 29, 31, 32, 43–48) ilk turda çıktı boyutu (64k) yüzünden düştü → paket başına ajan, 3'erli parti, orta çaba ile yeniden (wf_3e8c2c0b). Sentez (`betikler/sentez.js`, dosyadan okur) tüm tasarımlar bitince; sonra `mimari-render.js` ile belgeler yeniden üretilir.

**Dalga 1 (08:20 yeniden başlatıldı, wf_6f08ce84):** paket 4 Faz A → UX-1 Faz A → UX-3 Faz A → paket 1 Faz A (koşullar `ORKESTRATOR-DURUM.md`'de). İlk deneme (wf_9938c4ae) "3 dk ilerleme yok" ile düştü — 16 paralel Opus ajanı + yüksek çaba API hız sınırına takıldı; **kural:** eşzamanlılık ≤ 4, çaba 'medium', adımlar küçük.

**Sonraki dalgalar (sentez onaylayınca):** küçük altyapı (6-A, 5-A, 3-A) → günlük dükkân işi (19, 25, 27, 29, 31, 32, 20) → uyum (38 ÖNE, 39, 40-A, 41, 42) → rapor (34–37) → platform (44, 45, 46, 43, 47, 48) → UX fazları (UX-2 A2/B/C/D, UX-1 B/C/D, UX-3 B/C) araya serpilir; her dalga sonunda tarayıcı QA turu + belge güncellemesi + 5199 yeniden başlatma.

**Devralma güvencesi:** zamanlanmış görev `kuyumcu-erp-kaldigi-yerden-devam` (04:05/10:05; son 20 dk'da orkestratör dosyası değişmişse devralmaz). Kota sınırı 2026-09-16 01:00–06:40 arasında doldu; iş 06:52'de kaldığı yerden sürdü.


**A18 — Sentez tamam (2026-09-16 11:15).** 27 paket + UX-1/2/3 için karar tablosu, dalga planı ve çakışma haritası `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md` içinde yeniden üretildi (`betikler/sentez-parcali.js` → `ham/sentez.json` → `birlestir.js` → `mimari-render.js` → `ana-belge-ek.js`; her paket belgesinde "Birleşik koşullar (uygulayıcı için bağlayıcı)" bölümü var). Sonuç: 43 EVET, diğer 29 KOŞULLU KABUL (7'si veto koşullu: 1, 32, 39, 40, 41, 45, 47). Toplam tahmin 126–172 gün (tek geliştirici eşdeğeri).
**Dalga planı (yaz-seri; her dalga içinde SERİ, paralel güvenli çift YOK):**
- Dalga 1 (sürüyor): 4-A ✔ → UX-1-A ✔ → UX-3-A ✔ → 1-A (uygulanıyor).
- Dalga 2: **2a** UX-4a → 49 → UX-4b → UX-4c (`betikler/dalga2-args.json`, kurucu şikâyeti) · **2b** 38-A → 6-A → 3-A (38-A öne: 1↔38 tek `OzelMatrahMotoru` vetosu; 1-A ile senkron noktası).
- Dalga 3: 19 → 25 → 20 → 27 → UX-1-B → UX-2-A2.
- Dalga 4: 29 → 31 → 41 → 32 (toplu onay 41'den sonra, Mehmet vetosu) → UX-3-B.
- Dalga 5: 42-A → 5-A → 39-A → 40-A (Faz B KVKK onayı olmadan yok) → UX-2-C.
- Dalga 6: 34-A → 35-A → 37 → 36 → UX-1-C (rapor zinciri katı sıralı).
- Dalga 7: 45 (MFA jetonu ayrı audience) → 46 → 44 → 47-A (yalnız seçenek 2, kapılı içe aktarım) → 48-A.
- Dalga 8: Faz B/C kuyruğu (1-B, 4-B, 6-B, 3-B, 38-B, 42-B, 5-B/C, 39-B, 48-B/C, UX-1-D, 34-B, 35-B, 47-B, 40-B) → UX-3-C → UX-2-B → 43 (mobil) → UX-2-D.
Her dalga `betikler/dalga-uygula.js` ile (Opus uygulayıcı, çaba medium → hasım doğrulayıcı → ≤2 düzeltme → orkestratör kapısı). Kurucuya kalanlar ve riskler ana belgenin sonunda.

**A18 — REVİZE DALGA PLANI (2026-09-16 12:12 mührü; kurucu: "satışa hazır diyene kadar devam, her veri girişi düzenlenebilir, en güzel arayüz").** Tam metin: ana belge § "Uygulama dalgaları — REVİZE". Sıra: **2a** UX-4a → 49 → UX-4b → UX-4c · **2b** UX-1-B → 38-A → 6-A → 3-A · **2c** UX-2-A2 + UX-1-C (27 sayfa, tek geçiş) · **3** 19 → 25 → 20 → 27 → UX-3-B → dalga sonu yenileme (Panel, İşlemler, Cariler, Ürünler, Fiyat listeleri) = satılabilir demo · **4** 29 → 31 → 48-A → UX-1-D → 41 → 32 → yenileme (Kasalar, Banka, tahsilat) · **5** 42-A → 5-A → 39-A → 40-A → UX-2-C · **6** 34-A → 35-A → 37 → 36 · **7** 45 → 46 → 43 → 44 → 47-A → satışa hazırlık kurulu · **8** Faz B/C kuyruğu → UX-3-C → UX-2-D. Her dalga sonu: `betikler/kapi.sh` + `betikler/qa-turu.js` (27 sayfa tıklama; düzenlenemeyen alan = engel) + `betikler/hazirlik-kurulu.js` (7 mercek; HAZIR yalnız herkes hazır + ort ≥ 8); HAZIR çıkınca kalanlar 1.1. Mehmet (pazarlığa kapalı): finansal varlıklarda hard DELETE yok, her PATCH/DELETE ucu ModulYetkisi + rol + denetim kaydı; 41 → 32; 45 MFA asgaride. DoD sözleşmesi: her yeni varlık PATCH/DELETE(pasife alma) + yetki + denetim kaydı + DegismezIpucu ile doğar (dalga-uygula.js kuralı).

**A18 — DALGA 1 BİTTİ (2026-09-16 12:30 orkestratör kapısı: build 0/0 · denetim TEMİZ · 1193/1193).** 4-A (belge deposu: `belgeler` tablosu, BelgeKapisi magic-byte, /belgeler uçları, BelgeYukleyici.razor, migration 20260916060026_BelgeDeposu) · UX-1-A (DuzeltmePolitikasi.cs 32 varlık haritası, /meta/duzeltme-politikalari, GerekceAlani + DegismezIpucu) · UX-3-A (kullanicilar.arayuz_modu + tenants.varsayilan_arayuz_modu, ArayuzModu.Coz sunucuda, ProAlan, BasitMenu, #ust-mod-anahtar, PATCH /admin/tenant) · 1-A (fatura_kalemleri, OzelMatrahMotoru.cs tek KDV motoru, GET /islemler/fis/{fisId}, POST /faturalar/fisten adet=1 sözleşmesi, migration 20260916081339_FaturaKalemleri). Raporlar `dokumanlar/09-uygulama-kayitlari/PAKET-*-uygulama-raporu.md`. Taşınan bulgular (dalga 2): hediye kaleminde indirim NULL→0 (ORTA), Faturalar.razor 2 satır içi style, gizli xUnit1031 (RotaBaslikTestleri.cs:156). Kurucu kararı bekleyen veri: 9 Eylül tarihli 2 kiracı sızıntısı kanıt satırı (hareketler 01M240M0TN4TQHKRMVJTYWPJ37, kasa_hareketleri 01M240M0SY6B83WH6SDNAWN167; yetim-tara.sh çıkış 2).

**A18 — QA TURU 1 (2026-09-16 13:20):** 27 sayfa uygulama içi tarayıcıda tıklandı (fikstür kiracısı QA-TEST-1, token ile oturum): 133 bulgu, 12 kritik, 29 düzenlenemeyen veri girişi; görsel 3,85/5, kullanıcı dostu 3,07/5. Çizelge `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16.md`. Bulgular dalga 2b (UX-4d, 23 madde) ve 2c (UX-1-C1..C4 sayfa bazlı, UX-2-A2 kabuk) argümanlarına işlendi; her dalga sonunda tur yinelenir.

**A18 — HAZIRLIK KURULU TUR 1 (2026-09-16 13:29): HAZIR DEĞİL, ortalama 3,14/10** (`dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-1.md`: 20 engel, 8 maddelik asgari). Altyapı 7-8 (tek yazma kapısı, izolasyon, 1216 test), ürün yüzeyi 1-2 (düzeltilebilirlik, raporlar, mobil kabuk, e-Fatura, MFA, kurulum/yedek/abonelik yok). Kurul önerisi oylanıyor: 48-A + 45 öne, 34-A/35-A + 47-A dalga 3, yeni paketler T-1..T-4 (teslimat), F-1..F-8 (abonelik/üretim/telemetri/birim ekonomi), S-1 (ticari paket), PWA-1, E-BELGE (38-B). Kurucuya imza: e-Fatura entegratörü + mali mühür, sızıntı satırlarının silinmesi, alan adı/TLS + yaptırım listesi bedeli.

**A18 — MÜHÜR 2 (2026-09-16 13:38; kurul tur 1 sonrası yeniden oylama, 4 KOŞULLU, veto yok; tam metin ana belge § "Uygulama dalgaları — MÜHÜR 2").** Nihai sıra: **2a** UX-4a ✔ → 49 → UX-4b → UX-4c · **2b** UX-4d → UX-1-B → 38-A → 48-A (storno motoru) → 45-API → 6-A → 3-A · **2c** UX-2-A2 + UX-1-C1..C4 + 45-UI tek geçiş → UX-5 · **3** 19 → 25 → 20 → 27 → UX-3-B → 34-A → 35-A → 47-A+T-2 → T-1(+F-6) → F-2-min → T-3 → T-4a → B@3 (F-8 paralel belge) · **4** 41 → 32 → 40-A → 29-A → B@4 · **5** E-BELGE → 42-A → 5-A → 39-A → UX-2-C · **6** S-1 → 37 → 36 → T-4b · **7** 43 → 44 → PWA-1 → 46 → 31 → kurul 3 · **8** 1.1 kuyruğu. Asgari 28 kalem; asgari dışı 31, 46, 36, 37, F-1/F-3/F-4/F-9/F-10, UX-3-C, UX-2-D. UX-1-D iptal (storno düğmesi UX-1-C içinde). Ticari kapı (Eray): 2b+2c + QA turu 2 + kurul 2 bitene dek yalnız ücretsiz pilot; fiyat F-8 dolmadan ilan edilmez. Takvim (DOĞRULANMALI): e-Fatura hariç çekirdek 4-6 hafta, e-Fatura dâhil 8-10 hafta. Kurucuya bugün: e-Fatura entegratör sözleşmesi + mali mühür, sızıntı satırları kararı, alan adı/TLS + yaptırım listesi bedeli. Yeni paketler tasarlanıyor: T-1(+F-6), T-2, T-3, T-4, F-2-min, F-8, S-1, PWA-1, E-BELGE.

**A18 — MÜHÜR 3 (2026-09-16 14:28 ilk tur / 14:37 tam metinli yeniden oylama): 9 yeni teslimat/ticari paket** (T-1+F-6 yayın/kurulum, T-2 açılış bakiyeleri sihirbazı, T-3 yedek otomasyonu, T-4 destek/işletim görünürlüğü, F-2-min kayıt ucu kilidi, F-8 birim ekonomi belgesi, S-1 satış paketi, PWA-1 kabuk, E-BELGE e-Fatura Faz B) tasarlandı, 3 mercekle oylandı, hepsi KOŞULLU KABUL (T-4/F-2-min/E-BELGE ilk turda kesik girdi yüzünden tam metinle yeniden oylandı, `betikler/oy-tamamla.js`). Ortak kararlar (tek yayın yolu, sır disiplini tek hat, tek-el dosya bazında, MFA kapısı, sayısal iddia yasağı) ana belge § "Yeni paketler — MÜHÜR 3". E-BELGE dalga 5'e yalnız Faz A (Faz B entegratör sözleşmesine bağlı). Dalga 3 args (`betikler/dalga3-args.json`): 19→25→20→27→UX-3-B→34→35→47→T-2→T-1→F-2-min→T-3→T-4a.

**A18 — F-8 BİRİM EKONOMİ Faz A (2026-09-16 14:40):** `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md` (+ `dokumanlar/09-uygulama-kayitlari/ham/f8-birim-ekonomi.json`): 19 kaynaklı + 4 tahmin satır; kiracı başı doğrudan maliyet (10/50/100 kiracı) tabloda; kademe ÖNERİSİ (ilan değil, kurucu onayı yok); dürüst okuma: Dezy'ye karşı fiyat üstünlüğü yok, Mikro Jump/Kuyumcum.net'e karşı kaynaklı tasarruf; entegratör teklif sırası Paraşüt→QNB eFinans→eLogo; kurucu imza/para kalemleri `KURUCUYA-KARARLAR-2026-09-16.md` md. 1, 3, 8-11 (kur verisi izni, SMS başlığı, bulut hesabı, fiyat ilanı).

**A18 — MÜHÜR 4 (2026-09-16 14:56; kod öncesi açık kararlar, 5 mercek KOŞULLU, veto yok; ana belge § MÜHÜR 4, `ham/muhur4.md`):** F-2-min sistem yöneticisi kimliği = yapılandırma sırrı ('SISTEM' rolü yok; zorunlu X-Sistem-Operator + gerekçe + istek kimliği + IP denetim kaydına; MFA takvim kapısı + T-1 nginx IP kısıtı); F-2-min Faz B dalga 6'ya (pilot davetleri curl + runbook docs/); yaptırım taraması seçimi dalga 8'e (kalıp: tek IYaptirimTarayici + çoklu adaptör; yön ücretsiz OFAC/BM/AB); kur pilot varsayılanı TCMB + elle has/gram, sözleşmesiz Truncgil/Altınkaynak/AAB adaptörleri kapatılabilir (UX-4d md. 24, dalga 2b); T-1'e kalıcı günlük asgarisi (Program.cs'e kod yok). Paket belgelerine "Mühür 4 eki" işlendi (F-2-min, T-1, UX-4, 40); `ana-belge-ek.js` artık `ham/ek-PAKET-*.md` eklerini render sonrası kendisi geri koyar.

**A18 — DALGA 2a durumu (14:56):** UX-4a ✔ (1216 test) · 49 ✔ (1245 test, migration 20260916101547_FiyatListesiUrunFiyatlari) · UX-4b uygulandı, hasım doğrulayıcı tur 1 KALDI (kurucunun POS çift kasa bulgusu madde 7(d) atlanmış; 1279/1279 test yeşil, diğer kontroller OK) → düzeltme turu 1 sürüyor · UX-4c sırada. Dalga sonu: `kapi.sh` → Release yayın (5199, "kuyumcu-qa") → QA turu 2 (`betikler/qa-turu-args.json` tur 2; önceki bulgular `ham/qa-turu-1.json`, çizelge `QA-TARAYICI-TURU-2026-09-16-tur2.md`) ∥ dalga 2b (`betikler/dalga2b-args.json`) → kurul tur 2 (`betikler/kurul2-args.json`, kanıt yolları kapı çıktısından).

**A18 — RAKİP KIYAS RAPORU (2026-09-16 16:50; kurucu isteği "rakiplere göre neredeyiz, e-Fatura gibi elimizde olmayanlar hariç, rakip arayüzlerine bak"):** `RAKIP-KIYAS-RAPORU-2026-09-16.md` — 13 rakip 3 katman; 8 boyutta sıra (bugün satış vitrininde ~9-10., uyum çekirdeği + kalite ilk 3; 2b+2c sonrası ~5-6., 1.0 asgarisiyle 3-4.); var/yok tablosu; § 5 arayüz dersleri + § 5b canlı bakış (17:01, 6 site metin tabanlı — tarayıcı bölmesi gizli olduğu için ekran görüntüsü alınamadı; bölme açılınca tekrarlanır). Talimat 12 ORKESTRATOR-DURUM'a eklendi; rakip deneme hesabı kurucu kararı (KURUCUYA-KARARLAR md. 12). Rapor her kurul turunda güncellenir.

**A18 — QA TURU 2 + KURUL TUR 2 (2026-09-16 ~17:30):** QA turu 2: 27 sayfa, 149 bulgu, 11 kritik, 30 düzenlenemeyen; önceki 147 bulgudan 12 KAPANDI / 13 KISMEN / 122 AÇIK (fiyatlandırma kritiği kapandı; düzenlenebilirlik işi 2b/2c'de); yeni yüksek: /urunler Stok modalı NRE + /uyum belge no araması deserialize (ikisi 'API yanıt şekli ↔ istemci record' uyumsuzluğu) → dalga 2c args'a işlendi. Çizelge `QA-TARAYICI-TURU-2026-09-16-tur2.md` (ilk özet 90 KB kesik girdiyle yazılmış, `qa-ozet-yeniden.js` ile tam ham veriden düzeltildi; qa-turu.js sınırı 220 KB). Fikstür QA-TEST-2 (tur 1 refresh token'ı rotasyon/çalıntı tespitiyle iptal — betiğe giriş ile ikinci şans eklendi). **Kurul tur 2: HAZIR DEĞİL, 3,0/10** (`HAZIRLIK-KURULU-2.md`, 20 engel, asgari 11 madde; Mehmet 2 veto açık: MFA, e-Arşiv, sızıntı satırları). Mühürün plan değişikliği önerileri (2b/2c/3 sırası, SÖZLEŞME-1/S-2/T-5/34-Ö/V-1, 3'ü 2c ile paralel) yeniden oylamaya gönderildi. Rakip kıyas raporu `RAKIP-KIYAS-RAPORU-2026-09-16.md` (kurucu isteği). Dalga 2b: UX-4d GEÇTİ (1394 test), UX-1-B uygulanıyor.

**A18 — MÜHÜR 5 (2026-09-16 17:36; kurul tur 2 sonrası revizyon, 4 mercek KOŞULLU, Bertan+Mehmet vetoları gömülü; ana belge § MÜHÜR 5):** 2b sırası aynen (koşan akış kesilmez; 38-A'nın storno uçları 48-A bitmeden canlıya açılmaz) · 2c: **S-2** (Stok NRE + /uyum zarfı + yanıt şekli taraması + denetim.sh record sayacı) → UX-2-A2 → UX-1-C0 (ortak form/Toast/onay, C1'in ilk adımı) → C1..C4 + 45-UI → UX-5; SÖZLEŞME-1 1.1'e, record dondurma · 3: **T-5** (migration hijyeni, silme kurucuda) → 19 → 25 → 20 → 27 → UX-3-B → 34-A(+34-Ö) → 35-A → 47-A+T-2 → T-1+F-6 → F-2-min → T-3 → T-4a · 4: 29-A → 41 → 32 → 40-A → 31 → UX-1-D · kod hattında paralellik yok, yalnız 3-P belge/betik hattı (V-1 sürüm mührü, F-8, F-6/F-7 taslak, T-3 betik taslağı) · V-1: 1.0 = 2b+2c+3+4 asgarisi + Mehmet 4 uyum maddesi, 43 açık kalem · kapi.sh QA metrikleri, tarayıcı turu 1280 px. Args: `dalga2c-args.json`, `dalga3-args.json` güncel.

**A18 — 3-P BELGE HATTI + V-1 TASLAK (2026-09-16 ~18:10):** `dokumanlar/02-mimari/SURUM-MUHRU-V1-2026-09-16.md` (TASLAK; 1.0 = 34 paket, 1.1 = 11, karar bekliyor 6; Mehmet KOŞULLU — U1 MFA / U2 e-Arşiv / U3 sızıntı satırları / U4 storno-denetim izi kapanınca imza; `ham/surum.json` + `ham/dalga.json` → ana belge karar tablosunda Sürüm/Dalga sütunları). `dokumanlar/02-mimari/taslak/F-6-F-7-URETIM-PROFILI-TASLAGI-2026-09-16.md` ve `T-3-YEDEK-BETIKLERI-TASLAGI-2026-09-16.md` (kod değil; T-1/T-3 uygulayıcıları doğrular; mühürlü kararlar: güvenlik başlıkları tek kaynak Program.cs, systemd deploy/systemd/, runbook docs/, yedek betikleri dotnet/ kökü, gömülü parola kaldırılır). `kapi.sh` QA metrikleri basıyor; tarayıcı turu 1280 px. Dalga 2b: UX-4d ✔, UX-1-B ✔ (1439 test, migration IsletmeKimlikBilgileri), 38-A uygulanıyor.

**A18 — DALGA 2b BİTTİ (2026-09-16 22:11 kapısı: build 0/0 · denetim TEMİZ · 1650/1650 · 33 migration; test 1312 → 1650).** Yedi paket, hepsi hasım doğrulamasından geçti: **UX-4d** (24 madde: /api SPA fallback 404 JSON, Türkçe DataAnnotations + 400 alan→mesaj sözlüğü, MASAK mesajları, kasa/banka yön sözlüğü tek kaynak, ürün açılış stoğu, kur geçmişi bitiş günü, sözleşmesiz kur adaptörleri bayrakla kapatılabilir, gün sonu/aylık has/kâr tek tanım, /kilavuz iframe kaldırıldı, oturum yenileme tek uçuş) · **UX-1-B** (Reyon PATCH+pasife alma, /admin/tenant/kimlik + migration, KasaGuncelleDto alan bazlı PATCH, Banka DELETE=pasife alma, DuzeltmePolitikasi'nda UC_YOK kalmadı, denetim.sh 'finansal varlıkta [HttpDelete]=0') · **38-A** (tek OzelMatrahMotoru + KdvAyarlari, önizleme ucu, sıfır toleranslı tutarlılık kapısı; canlı: has bedeli matrah dışı, KDV 860,09 TL) · **48-A** (storno motoru: POST /islemler/{id}/storno, storno_kaynak_id, denetim bağlam damgası, StornoSuzgeci; TAKAS/HURDA_ALIS/TRANSFER ve çok bacaklı fiş 409 — ayrı paket) · **45-API** (RFC 6238 TOTP, AES-256-GCM giz, kurtarma kodları, ayrı audience meydan okuma jetonu — Mehmet ADR-0018 vetosu kapandı; SAHİP kilitlenme runbook'u T-1'e devredildi) · **6-A** (bildirim + zamanlanmış iş çatısı; ADR-0027 modül-başı tohumlama mekanizması burada kuruldu) · **3-A** (sipariş/tamir çatısı, append-only geçiş izi, sipariş deftere yazmaz — canlı kanıt). Raporlar `PAKET-{UX-4d,UX-1-B,38,48,45,6,3}-uygulama-raporu.md`. Devirler args'a işlendi: ELLE yolunda sessiz sıfır → UX-1-C2 · storno düğmeleri + kapsam dışı türlerde DegismezIpucu → C1/C2 · StornoSuzgeci raporlara → 34/35 · MFA runbook + giz rotasyonu → T-1 · MFA ekran bulguları → C4 · /bildirimler basit mod + denetim.sh varlık sayacı → C4.

## 6. TUZAKLAR (bu oturumda ısırdı)

- **API yanıtı camelCase** (`"id"`): canlı betikte `"Id"` arayınca boş kimlik → istek SPA fallback'e düşer, `text/html` 200 gelir ve yanlış yeşil okunur. Kimlik boşsa dur.

- **Adı yakalanamayan kararsız test:** tam süit 8 koşuda 1 kez 1 kırmızı verdi (`-v q` ad basmaz); `-v n` ile 5 ardışık koşu yeşil, yeniden üretilemedi. ÇÖZÜLMEDİ. Sonraki oturum tam süiti `--logger trx` ile koşup adı yakalasın; bUnit yarışı olması muhtemel (önceki örnek: `TurSec` ateşle-unut).
- **Razor öznitelikte `{ }` lambda** bu oturumda İKİNCİ kez ısırdı (`@bind:after="() => { … }"`). Kural: her zaman adlandırılmış metot.
- **Anonim tip yanıt nesnesinde özellik adı PascalCase** (`Id`, `Ad`); testte `GetProperty("id")` null döner → `OrdinalIgnoreCase` ile ara.

- **`dotnet ef migrations add/remove --no-build` derleme kırıkken ASLA:** `add` eski ikilinin modeline karşı BOŞ migration üretir (4 kez üretti); `remove --force` eski ikilinin gördüğü SON migration'ı siler — bu oturumda **VadeTakibi migration dosyaları silindi**, kuyumcu_test'te sütun kayboldu, 11 test kırıldı. Bilinen içerikle + yalnız özniteliklerden oluşan Designer ile elle geri yazıldı. Önce `dotnet build` yeşil, sonra build'li `migrations add`.
- **Kurucu elle silsin (hook `rm` bloklar):** `dotnet/Kuyumcu.Api/Data/Migrations/` altında 0 baytlık 8 dosya: `20260915131748_`, `20260915132123_`, `20260915132303_`, `20260915132645_FiyatListesiVeKampanya(.Designer).cs` — derlenmez, zararsız, kirlilik. Ayrıca `dotnet/Kuyumcu.Tests/VadeTakibiTestleri.cs.ek` (0 bayt).
- **`params T[]` parametreye hedef-tipli `new()` verilmez** (`Fis(k, new() {...})` derlenmez); tipi yaz.
- **bUnit sahte API `_ => "{}"` varsayılanı listeye çevrilemez** → `JsonException` ApiHata DEĞİLDİR; isteğe bağlı çağrılarda `catch (Exception ex) when (ex is ApiHata or JsonException)`. Yeni uç eklendiğinde ilgili sahte API'lere gerçekçi yanıt ekle (5 CariEkran testi bundan kırıldı).
- **Yeni kiracıda kur kaydı 10 sn sonra oluşur;** canlı senaryoda `/kur/guncel` boş dönebilir → `id` gelene kadar bekle, yoksa her istek `kurSnapshotId required` 400 verir.

- **zsh + sed `[0-9.]` aralığı Türkçe JSON'da "character not in range"** verir (kabuk yereli). JSON'dan alan çekerken `perl -CSD -ne` kullan; boş değişken sessizce 400'e yol açtı.
- **Razor öznitelik içinde `{ }` bloklu lambda derlenmez** (`@bind:after="() => { ... }"`). Adlandırılmış metot yaz.
- **Mutasyon zinciri ikili dosyaları mutant bırakır:** `mut.sh` kaynağı geri alır ama son `dotnet test` mutantı derlemiştir. Canlıdan ÖNCE temiz `dotnet build`.

- **python3 ARTIK ÇALIŞIYOR** (2026-09-15 22:56 doğrulandı: `python3 --version` → 3.9.6, exit 0; Xcode lisansı kabul edilmiş). Eski "exit 69 → perl" kuralı bayat; perl yine güvenli (Türkçe JSON'da `sed [0-9.]` tuzağı sürüyor). `denetim.sh`, `yetim-tara.sh`, `yedek-al-ve-dogrula.sh` ve `yuk-testi/` proje kökünde değil **`dotnet/`** altındadır.
- **zsh kelime bölme:** `for t in $LISTE` satır sonlarında BÖLMEZ → kiracı silme SQL'i tek "tablo adı" olup ERROR 1103 verdi (hiçbir şey silinmedi). `${=LISTE}` kullan.
- **`dotnet ef` ortam değişkeni:** `ASPNETCORE_ENVIRONMENT` yetmez, `DOTNET_ENVIRONMENT=Development` ya da `--connection`; aksi halde `appsettings.json` (database=kuyumcu) → "Access denied".
- **API portu:** 5000 macOS ControlCenter'da; `ASPNETCORE_URLS` işe yaramaz, **`--urls http://localhost:5199`** gerekir. Yeniden başlatınca eski JWT 401 → tekrar giriş.
- **Kur zamanlayıcısı 10 sn'de bir `kur_snapshots` yazar** — test kiracısını silmeden önce API'yi durdur (122 satır birikmişti).

- **Türkçe harf:** `ToUpperInvariant("sentetik")` → `"SENTETIK"` (noktasız I). `OrdinalIgnoreCase` "SENTETİK" ile "sentetik"i eşleştirmez. → `Kuyumcu.Core.Domain.TurkceMetin` kullan.
- **Kültüre bağlı sayı biçimi:** `{1.6:0.##}` tr-TR'de `"1,6"` üretir; SVG `viewBox` ayrıştırılamaz ve **barkod hiç çizilmez**. Testler invariant kültürde koştuğu için yeşildi. → SVG/CSV sayıları `CultureInfo.InvariantCulture`.
- **JSON kaçışı:** testte `Assert.Contains("Ayşe", govde)` tutmaz (`ş`). Gövdeyi JSON olarak ayrıştırıp karşılaştır.
- **CSS sınıf çakışması:** `.etiket` kur bandında zaten kullanılıyordu; ürün etiketi sınıfları `urun-etiket-*` oldu. Yeni sınıf adı seçerken önce `grep`.
- **`-o /dev/null` ile curl:** hataları gizler. Canlı doğrulamada yanıtı OKU.
- **ULID öneki zaman damgasıdır:** `Ulid()[..10]` aynı ms'te çakışır; rastgele kısım için `[^10..]` kullan.
- **`git checkout` / `stash` / `reset` ASLA** — TurkReport hook'u bloklar, önceki bir oturumda `.git` bozulup tüm kod silinmişti.
