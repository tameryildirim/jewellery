# PAKET 17 — Çoklu fiyat listesi + kampanya/hediye — KARAR — 2026-09-15

## Karar özeti
1. **Fiyat listesi = has değeri üzerine MARJ (%) + işçilik çarpanı (%)**, liste başına;
   cariye atanabilir; kiracı başına bir VARSAYILAN liste. Fiyat **sunucuda** hesaplanır
   (PANO-KARAR §S2/§88 ile aynı ilke: marj ticari sırdır, hesap tek yerde, iki uygulama
   iki farklı kuruş üretmez). Ekran yalnız sunucunun kırılımını gösterir. Faz 0 §11'in
   (a) koşulu (kotasyona eklenecek marj alanı) bu pakette açılır; (c) koşulu (parite
   eşleşmesi) HİÇ dokunulmaz — fiyat `has_grami × has kuru` tabanında kalır.
2. **Kampanya** = süreli indirim (≤ 3 ay), hedefi ürün/kategori/tümü, indirimi işçilik
   yüzdesi / tutar yüzdesi / sabit TL. Satış satırına **indirim öncesi liste fiyatı**,
   **indirim tutarı** ve **kampanya kimliği** yazılır (FEY md.11/1-2: ikisi birlikte ve
   aynı büyüklükte gösterilir; fiş/etiket bunu basar).
3. **"İndirimden önceki fiyat" = son 30 günde o ürüne fiilen uygulanan en düşük liste
   fiyatı** (TRHTUY md.14; Kılavuz md.7/3). Ayrı fiyat-geçmişi tablosu YOK: değer,
   `hareketler.liste_fiyati` satırlarından türetilir (append-only defter tek kaynak).
   Son 30 günde satış yoksa cari liste fiyatı esas alınır (Kılavuz md.7/12 — yeni satışa
   sunulan mal). Has bileşeni kurla değişir; karşılaştırma **satış anında uygulanan liste
   fiyatı** üzerinden yapılır ve bu, kodda ve kılavuz notunda açıkça yazılır.
4. **Hediye** = fişte `hediye=true` kalem: birim fiyat SUNUCUDA 0'a zorlanır, liste fiyatı
   `indirim_tutari` olarak yazılır (KDV GUT: "tutar iskontosu" biçiminde gösterim),
   stok ve maliyet normal çıkar. **YALNIZ aynı fişte bedelli en az bir kalem varsa**
   (KDV GUT I/B-10.1: "bir malın yanında başka bir mal verilmesi" → bedelsiz teslim
   değil, KDV hesaplanmaz, yüklenilen KDV indirilir). Tek başına hediye → **400**:
   KDVK md.27 emsal bedel üzerinden KDV doğurur, sistem bunu sessizce yazmaz.
5. **Kapsam dışı (bilinçli):** hediye çeki/kupon (ön ödemeli yükümlülük — ayrı paket),
   sadakat programı (Kılavuz md.7/14 ayrı kurallar), kişiselleştirilmiş fiyat.

## Dayanak — birincil metinler
- **Fiyat Etiketi Yönetmeliği md.11**: indirimli fiyat + indirimden önceki fiyat tarife,
  fiyat listesi ve etikette **birlikte ve aynı büyüklükte**; indirimli satış süresi
  **3 ay** (tasfiyede 6 ay). Md.6: kıymetli taş/madenden mallarda etiket zorunluluğu yok;
  **işçilik dahil mi ayrıca belirtilir**. Md.2: tüketiciye sunulan mallar (perakende
  kuyumcu kapsamda). Metinde "on/otuz gün" YOK — gün kuralı FEY'de değil.
- **Ticari Reklam ve Haksız Ticari Uygulamalar Yönetmeliği md.14** + **Reklam Kurulu
  Kılavuzu (09.01.2024, 341 s. karar)** md.7/3: "indirimden önceki satış fiyatının
  tespitinde indirimin uygulandığı tarihten önceki **30 gün** içinde uygulanan en düşük
  fiyat esas alınır"; md.7/12 yeni satışa sunulan mal → sunuluş fiyatı; md.7/13
  "1 alana 1 bedava", "500 TL üzeri 50 TL indirim", **hediye çeki ve indirim kuponu**
  indirimli satış kapsamında DEĞİL (ancak avantaj ispatlanır); md.6/11 dinamik/gerçek
  zamanlı fiyat (kur) kişiselleştirilmiş fiyat sayılmaz; md.7/6 ispat fiş/log kayıtlarıyla.
- **KDV Genel Uygulama Tebliği I/B-10.1 (Promosyon Ürünleri)**: satılan malın yanında
  ek bedel alınmadan verilen mal için bedelsiz teslim söz konusu değil; KDV hesaplanmaz,
  yüklenilen KDV indirilir; belgede açıklayıcı not / tutar iskontosu. **KDVK md.27**:
  bedeli bulunmayan teslimde matrah emsal bedel.
- **Sektör notu** (Alanya kuyumcuları, 09.02.2025): "etiket 80 bin, kampanyalı 20–25
  bin" ve "şu kadar alana gram altın hediye" uygulamaları yanıltıcı; indirim işçilikte
  ve kısa süreli olur — tasarım işçilik/marj indirimini varsayılan yapar, has bileşenine
  indirim ÖN TANIMLI değildir.

## Kaynaklar
- https://kontder.com.tr/mevzuatdetay/fiyat-etiketi-yonetmeligi (FEY md.5, 6, 11)
- https://tuketici.ticaret.gov.tr/data/65a6387813b876fb4cb78471/... (Kılavuz PDF, 7 sayfa — yerel kopya okundu)
- https://www.muhasebetr.com/yazarlarimiz/hakancoskun/008/ ve https://www.karenaudit.com/promosyon-amacli-olarak-teslim-edilen-urunlerde-kdv-ve-fatura-duzeni/ (KDV GUT I/B-10.1 aktarımı)
- https://www.yenialanya.com/haber/23696622/kuyumculardan-indirim-uyarisi
- FAZ0-DUZELTME-RAPORU-2026-09-10.md §11 · PANO-KARAR-2026-09-11.md §S2, §88, §133
