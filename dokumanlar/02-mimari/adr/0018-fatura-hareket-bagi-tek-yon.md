# ADR-0018 — Fatura ↔ hareket bağı TEK YÖNDEDİR: `fatura_kalemleri.hareket_id`

- **Tarih:** 2026-09-18
- **Durum:** KABUL EDİLDİ
- **Bağlam:** PLAN-TEK-SEFER-2026-09-18 § 2 / P-08 · kimlik Y-25 · koşul **BK-04** (Bertan K4)
- **Karar sahibi:** Bertan (02 Baş Mimar) koşulu; P-08 uygulaması yazdı

## Bağlam

Plan taslağı Y-25'i "`hareketler` tablosuna `fatura_id` kolonu aç" diye tarif ediyordu.
Hasım turu bu öncülü **çürüttü** (doğrulama verdicti YANLIS): kimlik düzeyinde bağ
**zaten vardır** — `fatura_kalemleri.hareket_id` (`Data/FaturaKalemi.cs:34`), fatura
kesilirken doldurulur (`Modules/Fatura/FaturaModulu.cs`, kalemli yol).

Ayrıca `hareketler.fatura_no` adında serbest metin bir **damga** kolonu vardır
(`Data/Entities.cs`, `[Column("fatura_no"), MaxLength(50)]`). Bugüne dek "bu satış
faturalı mı" sorusu bu metinden okunuyordu.

## Ölçülen sorun (varsayım değil)

1. **`fatura_no` bağ değil, KOPYADIR.** Fatura iptal edilse de damga hareket
   satırında kalır; bağın canlılığı kolondan okunamaz.
2. **`fatura_no` AŞIRI YÜKLÜDÜR.** Aynı kolon `HURDA_ALIS` satırlarında bizim
   belgemizi değil **tedarikçinin fatura numarasını** taşır
   (`Modules/Satis/SatisModulu.cs`: `FaturaNo = dto.Satici!.TedarikciFaturaNo?.Trim()`).
   İki anlam tek kolonda durduğu sürece "hangi faturamıza bağlı" sorusunun cevabı
   bu kolondan **doğru okunamaz**.
3. **İki paralel bağ = çifte sayma.** `hareketler.fatura_id` de açılsaydı mutabakat
   (P-24) aynı satışı hem kalem hem kolon üzerinden saymak zorunda kalırdı.

## Karar

1. **Kardinalite: 1 fatura ↔ N hareket.** Bir fatura birden çok defter satırına
   dayanabilir (çok kalemli fiş); **bir defter satırı en fazla bir geçerli faturaya**
   bağlanır.
2. **OTORİTE `fatura_kalemleri.hareket_id`'dir.** Ters yönde `hareketler.fatura_id`
   kolonu **AÇILMAZ**.
3. **`hareketler.fatura_no` EMEKLİYE AYRILDI.** Yeni okuma yolu yazılmaz. Geriye
   dönük (kalemli fatura yolundan önce kesilmiş belgeler) okuma **yalnız**
   `Data/FaturaHareketBagi.cs` içinde, `Kaynak = "DAMGA"` etiketiyle yapılır.
   Kolon **yazılmaya devam eder** (basılı belgede ve dışa aktarımda numara lazımdır);
   emekli olan **okuma yoludur**, kolonun kendisi değil.
4. **TEK SORGU YOLU:** `FaturaHareketBagi.Coz(...)` ve `FaturaHareketBagi.IlkBag(...)`.
   Bir hareket hem kalemle hem damgayla eşleşirse **KALEM kazanır**; sonuç hareket
   kimliğine göre tekilleştirilir → çifte sayma yapısal olarak imkânsızdır.
5. **İPTAL bağ saymaz.** `IlkBag` yalnız `Durum != 'IPTAL'` faturaları görür:
   iptal edilen belgenin kalemi durur ama o satış yeniden faturalanabilmelidir.
6. **Beyaz liste (tek istisna):** `RaporModulu.PosFaturaMutabakati` içindeki
   *belgesiz hurda* kontrolü `fatura_no`yu okumaya devam eder — orada sorulan soru
   "bu alımın **bir** belgesi var mı"dır, "hangi faturamıza bağlı"dır değil.
   İstisna kodda yorum olarak ve `denetim.sh` P-08 sayacında **adıyla** yazılıdır.

## Sonuçlar

- **Olumlu:** bağ tek yerden okunur; iptal edilmiş faturanın damgası artık satışı
  "faturalı" göstermez; kalemi bağlı ama damgası düşmüş bir satış ikinci kez
  faturalanamaz (önceki hâlde faturalanabiliyordu).
- **Olumsuz / bedeli:** eski satırlar için damga yolu hâlâ gereklidir; bu yol
  silinemez, yalnız tek dosyaya hapsedilmiştir. Tamamen kapanması `fatura_no`
  okuma yolu sayacının 0'a inmesiyle (P-24 mutabakat paketi) mümkündür.
- **Migration:** bu ADR **hiçbir bağ migration'ı açmaz** (BK-04 gereği yön önce
  yazıldı; yön "kolon açmamak" çıktı). P-08 migration'ı yalnız `adet` ve
  `personel_id` kolonlarını açar.

## Sayaç

`denetim.sh` → `# >>> P-08` bloğu → `FATURA_NO_SERBEST_METIN` (cırcır, yalnız azalır).
