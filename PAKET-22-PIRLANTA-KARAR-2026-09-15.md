# PAKET 22 — Pırlanta 4C + sertifika + PDF arşivi — KARAR — 2026-09-15

## Karar
1. `urunler`'e 4C: `pirlanta_karat` (decimal 8,3), `pirlanta_renk` (D…Z), `pirlanta_berraklik`
   (FL, IF, VVS1, VVS2, VS1, VS2, SI1, SI2, I1, I2, I3), `pirlanta_kesim` (EXCELLENT, VERY_GOOD,
   GOOD, FAIR, POOR); sertifika: `sertifika_kurumu` (GIA, HRD, IGI, EGL, DIGER), `sertifika_no`.
   Listeler kapalıdır (regex); serbest metin yalnız `DIGER` kurum adında.
2. **Fail-closed:** `Tip = PIRLANTA` üründe **karat zorunlu**; renk/berraklık/kesim isteğe bağlı
   ama verilirse listeden. Sertifika no verilirse kurum zorunlu. PIRLANTA dışı tipte 4C alanı
   gönderilirse 400 (yanlış tip seçimini sessizce kabul etme).
3. **Sertifika PDF'i veritabanında** (`urun_belgeleri`: tenant, urun_id, tur=SERTIFIKA, ad,
   mime, boyut, sha256, icerik LONGBLOB, yukleyen, tarih). ≤ 5 MB, yalnız `application/pdf`
   (magic byte `%PDF-` denetlenir). Neden DB: projede dosya depolama yok; dışa aktarım "diske
   yazılmaz" ilkesini koyduk; yedek betiği tüm tabloları checksum'lar → arşiv yedeğe girer;
   kiracı izolasyonu aynı global filtreyle sağlanır. Silme yok — belge pasife alınır.
4. Etiket/fiş: PIRLANTA satırında 4C özeti ("0,52 ct · G · VS1 · EX") ve sertifika no; sentetik
   ibare kuralı (md.11/(e), `TasKapisi`) aynen geçerli.

## Dayanak
- Kuyum Ticareti Hakkında Yönetmelik md.11/(e) (24.06.2026 değişikliği): sentetik taş ibaresi
  etiket, sertifika, fatura ve reklamda — mevcut `TasKapisi` uygular; 4C için ayrı zorunluluk
  metinde YOK (arama, 2026-09-15). Sertifika zorunluluğu mevzuatta bulunamadı; sektör pratiği
  ≥ 0,30 ct'te uluslararası sertifika (GIA/HRD/IGI) — sistem zorlamaz, alan sunar.
- Fiyat Etiketi Yönetmeliği md.5/(2)-b "ayırıcı özellik" → 4C özeti etikete girer.
- Tüketici lehine ispat: sertifikanın kendisi arşivlenir (PDF), sadece numarası değil.

## Kapsam dışı
Otomatik sertifika doğrulama (GIA Report Check API), taş bazlı çoklu pırlanta (bir yüzükte
yan taşlar — tek "ana taş" 4C'si tutulur; yan taş toplam karatı `tas_agirlik` gramla değil,
ayrı alanla gelmez — bilinçli), fiyatlamada 4C tabanlı otomatik değerleme.
