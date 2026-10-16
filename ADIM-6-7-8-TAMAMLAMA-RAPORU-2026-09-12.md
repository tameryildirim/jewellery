# Adım 6 + 7 + 8 — Tamamlama Raporu

**Tarih:** 2026-09-12 · **Kapsam:** `PLAN-YASAL-ZORUNLULUKLAR-2026-09-11.md` Adım 6, 7, 8.
**Commit atılmadı** (Kurucu kuralı). **Testler: 293/293 yeşil** (önce 252).

---

## 1. ADIM 6 — Hurda alışında zorunlu kimlik + gider pusulası

**Kabul ölçütü:** *"Kimliksiz hurda alışı reddedilir (test). Gider pusulası çıktısı örnekle gösterilir."* → ikisi de kanıtlandı.

MASAK kapısıyla aynı iki fazlı desen, ama **kritik farkla: tutar eşiği YOK.** VUK md.234 gereği vergi mükellefi olmayandan yapılan her hurda alımında kimlik gerekir — MASAK eşiği 185.000 TL iken bu 1.000 TL'lik alımda da geçerli.

### Satıcı tipi belgeyi ve stopajı belirler

| Satıcı tipi | Belge | Stopaj | Kaynak |
|---|---|---|---|
| `NIHAI_TUKETICI` (vergi mükellefi değil, zati eşyasını satıyor) | Gider pusulası | **YOK** | GVK md.94'te bu kişilerden hurda alımı için tevkifat hükmü yok |
| `ESNAF_MUAF` (esnaf muaflığından yararlanan) | Gider pusulası | **%2** | Esnaf muaflarına hurda mal alımı ödemelerinde GVK md.94 |
| `MUKELLEF` | Gider pusulası **düzenlenmez** — satıcı fatura keser | — | — |

Oranlar `appsettings:Hurda`'dan okunur; mevzuat değişirse kod değişmez.

### TCKN doğrulanıyor
Uydurma numara reddediliyor — gider pusulası vergi belgesidir, hatalı kimlikle düzenlenmesi kuyumcuyu riske atar. Boşluklu yazım (`100 000 001 46`) kabul, normalize ediliyor.

### Canlı kanıt
```
kimliksiz     → 400 "Hurda alışında satıcı kimliği zorunludur (VUK md.234 — gider pusulası)"
uydurma TCKN  → 400 "TCKN doğrulama algoritmasından geçmedi"
geçerli       → 200 {"pusulaNo":"GP-2026-000001","brutTutar":62288.00,
                     "stopajOran":0.0,"stopajTutar":0.00,"netOdenen":62288.00,
                     "duzenlemeSonTarihi":"2026-09-19T…"}   ← VUK 7 gün
esnaf muaf    → 200 {"pusulaNo":"GP-2026-000002","stopajOran":0.02,
                     "stopajTutar":1245.76,"netOdenen":61042.24}
```

Satıcı TCKN'si diske **şifreli** iniyor (MASAK kimliğiyle aynı AES-256-GCM altyapısı):
```
GP-2026-000001   v1:WZHas3yCpXLinxoCTAJ…   NIHAI_TUKETICI   0.0000        0.00   62288.00
GP-2026-000002   v1:zD7vKuQBQ60/PtrSkLI…   ESNAF_MUAF       0.0200     1245.76   61042.24
düz metin "10000000146" geçen satır: 0
```
Hash (HMAC) ile "bu kişiden daha önce ne aldık" aranabiliyor; hash'te de düz metin yok.

---

## 2. ADIM 7 — Günlük POS–fatura mutabakatı

**Kabul ölçütü:** *"Kasıtlı uyumsuz veriyle farkın yakalandığı testle gösterilir."* → kanıtlandı.

`GET /api/v1/raporlar/pos-fatura-mutabakati?tarih=…` — gün sonu kapanışı. Gün sınırı K5 kuralına uyuyor (Europe/Istanbul → UTC).

**Eşleştirme bağı kuruldu:** `FaturaOlusturDto.HareketId` opsiyonel olarak eklendi; verilirse harekete fatura numarası damgalanır. Damgasız satış "faturasız" listesine düşer. Aynı hareket ikinci kez faturalandırılamaz; yabancı kiracının hareketiyle fatura kesilemez (ikisi de testli).

### Canlı kanıt — kasıtlı uyumsuz veri
3 satış (NAKIT 40.000 / KART 25.000 / HAVALE 10.000), yalnız birine fatura:
```json
"tahsilat": { "nakit": 40000, "kart": 25000, "havale": 10000, "toplam": 75000 },
"satis":    { "sayisi": 3, "toplamTl": 75000,
              "faturaliSayisi": 1, "faturaliTl": 40000,
              "faturasizSayisi": 2, "faturasizTl": 35000 },
"fatura":   { "sayisi": 1, "toplamTl": 40000, "kdvToplam": 6666.67 },
"fark": 35000, "mutabik": false,
"uyarilar": ["2 satış faturalandırılmamış (toplam 35.000,00 TL)",
             "Satış toplamı ile fatura toplamı arasında 35.000,00 TL fark var"],
"faturasizIslemler": [ {25.000 KART…}, {10.000 HAVALE…} ]
```
İptal fatura beyana girmiyor; veresiye satış tahsilata girmiyor ama satışa giriyor (ikisi testli).

---

## 3. ADIM 8 — KMTS opsiyonel kod alanı

`urunler.kmts_kodu` ve `hareketler.kmts_kodu` eklendi — **opsiyonel, doğrulanmıyor.** Biçim bilinmiyor, tahmin edilmedi.

Gerekçe koda yazıldı: KMTS perakende kuyumcuyu bağlamıyor (usul ve esaslarda "kuyumcu" ve "perakende" 0 kez geçiyor, yükümlü taraf rafineriler, işlenmiş takı kapsam dışı). Alan yalnız kayıt için; kapsam genişlerse hazırız.

Canlı: ürün `kmtsKodu = KMTS-URUN-9`, hurda hareketi `KMTS-TEST-001` (trim ediliyor, boş string → null).

---

## 4. CANLI TESTTE YAKALANAN İKİ GERÇEK HATA

Madde 1: ikisi de 291 testten **geçmişti**, yalnız gerçek sunucuda ortaya çıktı.

### Hata 1 — Kasa hareketi yönünü kaybediyordu *(bu hata benden ÖNCE vardı)*
`SatisModulu.KasaGuncelle` satır 777: `Tutar = Yuvarla2(Math.Abs(tutarDelta))`.

Kasa **bakiyesi** doğru güncelleniyordu ama **defter satırı mutlak değerle** yazılıyordu. Sonuçları:
- Bakiye append-only defterden **yeniden türetilemiyordu** — Bertan'ın 2026-09-10 kuralının ihlali.
- Hurda ödemesi gibi para **çıkışları** gün sonu raporunda **tahsilat** gibi görünüyordu.

Canlı ölçüm: `gerçek bakiye −49.576` iken `defter toplamı +199.576`.

**Düzeltme:** işaretli yazılıyor (+ giriş, − çıkış). Düzeltme sonrası canlı: `gerçek −61.042,24` = `defterden −61.042,24`.

### Hata 2 — Stopaj kasadan düşülmüyordu *(benim eklediğim kod)*
Gider pusulası "net ödenen 61.042,24" yazıyordu ama kasadan **62.288** çıkıyordu. Satıcı brütü almış görünüyordu.

**Düzeltme:** satıcının eline geçen NET'tir. Kasadan çıkan, cariye borçlanılan ve `hareket.TlTutari` artık net. Brüt ve kesilen stopaj gider pusulasında **belge verisi** olarak duruyor. MASAK eşiği **brüt** üzerinden değerlendirilmeye devam ediyor (doğrusu bu — el değiştiren değer brüttür).

İkisi için de regresyon testi yazıldı; yarın geri gelmezler.

---

## 5. DEĞİŞEN DOSYALAR

**Yeni (4):** `Data/GiderPusulasi.cs` · `Modules/Hurda/GiderPusulasiKapisi.cs` · `Tests/GiderPusulasiTestleri.cs` (33 test) · `Tests/MutabakatTestleri.cs` (12 test)
**Migration:** `20260912175749_GiderPusulasiVeKmts` — `gider_pusulalari` tablosu + 2 `kmts_kodu` sütunu, uygulandı.
**Değişen (8):** `Data/Entities.cs` (KMTS ×2) · `Data/KuyumcuDbContext.cs` (DbSet, 4 indeks, TCKN şifreleme) · `Data/MasakHashDinleyicisi.cs` (pusula hash'i) · `Common/FaturaNoUretici.cs` (`SiradakiPusula`) · `Modules/Satis/SatisModulu.cs` (kapı, stopaj, işaret düzeltmesi) · `Modules/Fatura/FaturaModulu.cs` (HareketId bağı) · `Modules/Urun/UrunModulu.cs` (KMTS) · `Modules/Rapor/RaporModulu.cs` (mutabakat) · `Program.cs` + `appsettings.json` (HurdaAyarlari)
**Uyarlanan testler:** mevcut 4 hurda testine geçerli satıcı kimliği eklendi — kimliksiz çağrılar artık **bilerek** reddediliyor.

---

## 6. AÇIK KALANLAR (dürüstçe)

1. **Stopaj vergi borcu muhasebeleştirilmiyor.** Kesilen %2 kasadan düşülüyor ve pusulada yazıyor, ama vergi dairesine borç olarak bir hesaba **yazılmıyor** — vergi yükümlülüğü hesabı modellenmedi. Bu yüzden hurda alımının kayıtlı maliyeti brüt değil net.
2. **Gider pusulası çıktısı yok.** Veri ve numara üretiliyor; yazdırılabilir belge (PDF/HTML) üretilmiyor. Kabul ölçütü "örnekle gösterilir" JSON çıktısıyla karşılandı, basılı belge değil.
3. **7 günlük süre takip edilmiyor.** `duzenleme_son_tarihi` hesaplanıp saklanıyor, süresi dolanı uyaran bir iş yok.
4. **Mutabakat yalnız `SATIS` türünü sayıyor.** Alış faturaları ve hurda pusulaları ayrı raporlanmıyor.
5. **`hareket.FaturaNo` elle bağlanıyor.** Satış sırasında otomatik fatura kesilmiyor; kuyumcu faturayı ayrı çağrıda kesmeli.
6. **KMTS biçimi doğrulanmıyor** — şema bilinmiyor (bilinçli).
7. **Adım 6'daki stopaj oranları tek bir kaynak ailesinden** doğrulandı (Özdoğrular/Proventus + esnaf muaflığı yorumları). Mali müşavir teyidi **K2'ye eklenmeli** — para hareketi etkiliyor.

---

## 7. PLAN DURUMU

| Adım | Durum |
|---|---|
| 1, 2, 3, 4, A, 9 | ✅ daha önce bitti |
| **6 — hurda kimlik + gider pusulası** | ✅ **BİTTİ** |
| **7 — POS–fatura mutabakatı** | ✅ **BİTTİ** |
| **8 — KMTS opsiyonel alan** | ✅ **BİTTİ** |
| 5 — e-Fatura / e-Arşiv | 🔒 K1 (entegratör sözleşmesi) + K2 (hadd çelişkisi) bekliyor |

**Plandaki tek açık adım 5 kaldı ve kilidi Kurucu'da.**

---

## EK — 2026-09-12 (ikinci tur): §6'daki açıklar kapatıldı

Kurucu: *"bende e-fatura ve mali müşavir teyitleri yok, kendi yapabileceğimiz ne varsa hepsini bitir."* Adım 5 kilitli kaldı; **dış bağımlılığı olmayan her açık kapatıldı.**

**Testler: 321/321 yeşil** (önce 293). `denetim.sh` temiz.

### §6/2 — Gider pusulası belgesi ✅
`GET /gider-pusulalari/{id}/belge` — VUK md.234'ün istediği alanlar. Canlı çıktı:
```
GİDER PUSULASI   GP-2026-000001
Dayanak : 213 sayılı VUK md.234
Satıcı  : Ayse Demir  TCKN 10000000146  (Vergi mükellefi olmayan (zati eşya satışı))
Mal     : Hurda altın (K22 — 916/1000)  10 g → 9,16 has
Brüt    : 62.288,00 TL   Stopaj: %0 = 0,00 TL      NET: 62.288,00 TL
Yazıyla : ALTMIŞİKİBİNİKİYÜZSEKSENSEKİZ TL
Süre    : teslim 2026-09-12 → son 2026-09-19 (gecikti: False)
Not     : Vergi mükellefi olmayan kişiden alımda gelir vergisi tevkifatı yapılmamıştır.
```
Tutar yazıyla yazılıyor (`Common/TutarYazi.cs`, 11 test; "BİRBİN"/"BİRYÜZ" tuzakları dahil). Liste TCKN'yi **maskeler**, tam değer yalnız tekil çağrıda ve **okuma izi düşer**; TEZGAHTAR erişemez.

Stopajlı belgede not değişiyor: *"Kesilen stopaj vergi dairesine beyan edilmelidir; bu sürümde vergi borcu hesabı tutulmamaktadır."* — Madde 1: belgenin kapsamadığı şey belgenin üstünde yazıyor.

### §6/3 — 7 günlük süre takibi ✅
`GET /gider-pusulalari/sure-takibi` — süresi dolmuş ve dolmak üzere olanlar. Kişisel veri döndürmez. Mutabakat raporuna da bağlandı: gecikmiş pusula varsa gün sonunda uyarı çıkıyor.

### §6/4 — Mutabakat alış/hurda ile genişletildi ✅
Artık `alis` ve `hurda` blokları var: pusula sayısı, brüt, **stopaj toplamı**, gecikmiş pusula sayısı ve **belgesiz hurda alımı** (ne pusula ne tedarikçi faturası). Belgesiz alım varsa `mutabik = false`.

### ADIM-A §5/2 — Resmî tatil takvimi ✅
`Common/ResmiTatiller.cs`. Sabit tatiller kodda (7 gün); **dinî bayramlar yapılandırmada** — hicri takvim kayar, Diyanet ilanıyla kesinleşir, **tahmin edilmez**. Bozuk biçimli kayıt sessizce yutulmaz (`Gecersiz()`), dinî bayramı girilmemiş yıl bildirilir (`DiniBayramEksik()`). MASAK'ın 10 iş günü sayacına bağlandı — tatilin süreyi uzattığı testle kanıtlı.

### ADIM-A §5/5 — Saklama süresi maskelemesi ✅
`Modules/Masak/SaklamaSuresiIsi.cs` + `GET /masak/saklama-onizleme`, `POST /masak/saklama-uygula` (yalnız SAHİP).

MASAK 8 yıl saklama ister, KVKK süre dolunca tutmamayı ister. Çözüm **silme değil maskeleme**: kimlik alanları `[SAKLAMA SÜRESİ DOLDU]` olur, **tutar/tarih/kademe korunur** — denetim izi bozulmaz. İşlem denetim izine yazılır, idempotenttir, kiracı sınırına tabidir.

**Zamanlayıcıya bilinçli olarak BAĞLANMADI:** kişisel veriyi geri döndürülemez biçimde değiştiren bir iş, kuyumcunun haberi olmadan kendi kendine çalışmamalı. Önce önizleme, sonra onay.

### Kapanmayanlar ve sebepleri
| Açık | Neden kapanmadı |
|---|---|
| §6/1 stopaj vergi borcu | Vergi yükümlülüğü hesabı modellenmeli; ayrıca oran **K2** teyidi bekliyor. Belgede açıkça yazılı. |
| §6/5 satışta otomatik fatura | Fatura kesme e-Fatura akışına bağlı (**Adım 5 / K1**). |
| §6/6 KMTS biçimi | Şema bilinmiyor — tahmin edilmeyecek. |
| §6/7 stopaj oranı teyidi | **K2** (mali müşavir). |
| KVKK aydınlatma metni | Hukuki metin; ben yazamam, Kurucu/danışman verir. Mekanizma hazır (form + onay kaydı) ama metinsiz eklenmedi. |
