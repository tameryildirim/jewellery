# Paket planı — Müşteri görüşmesi + Faturalog eşitliği (16.10.2026)

**Kurucu kararı (16.10):** Müşterinin "böyle olacak" dediği her şey olur. Riskli konularda seçenekler kuyumcunun önüne **liste** olarak gelir, kuyumcu onaylayarak seçer. Mihenk = müşterinin iki programı (Faturalog + diğer program) tek yerde.
Girdi: `dokumanlar/01-gereksinimler/musteri-gorusmesi-2026-10-16.md` (görüşme, EK araştırma, EK-2 Faturalog eşitliği, EK-3 muhalif görüşler) + 3 kod haritası (16.10).

**Her paketin kapısı:** etki analizi → kod → hedefli testler (yeni + etkilenen) → `denetim.sh` (yeni kırmızı yok) → gerçek tarayıcı (1280 + 375, etkileşim) → ekran görüntüsü → yerel git kaydı. Tam test takımı günde bir.
**Değişmez kurallar:** mevzuat rakamı koda gömülmez (ADR-0005, yasal parametre tablosu) · e-Fatura sahte sağlayıcı yok (ADR-0012) · banka hareketi tek kapı `HareketEkleIc` · fatura numarası tek kapı `FaturaSeriKapisi.Tuket` · `KimlikNoDogrulayici` değişmez · altın dışı madende altın kuru yok (K20-4) · boş yeni alan = eski davranış aynen.

| Paket | Kapsam (müşteri sözü) | Kabul ölçütü | Bağımlılık |
|---|---|---|---|
| **M1 Fiyat motoru: satış milyemi + ofset** | "Bileziğin milyemi, çarpanı burada olacak, elimle müdahale edebileceğim; 925 ise 925" | Ürüne `satis_milyemi` (boş = eski davranış) + `fiyat_ofset_tl`; teklif = (gram−taş) × satış milyemi/1000 × has (+ liste marjı + ofset); kart düzenle formunda alan; işçilik milyemi = satış − ayar milyemi raporlanır; testler: boş/dolu/gümüş fail-closed | — |
| **M2 Ürün KDV oranı faturaya** | "KDV oranını düzenlediği bir şey olacak" | Fatura kalemi `Urun.KdvOrani` doluysa onu, boşsa varsayılanı kullanır (gizli hata düzeltmesi) | — |
| **M3 Sarrafiye** | "Eski çeyrek de eklenecek… Reşat yok" | Eski/yeni çeyrek-yarım-tam, Ata, Reşat, Hamit adet ürünleri; fiyat seçeneği listesi: kaynak kotasyonu (alış/satış) / sabit fiyat / ofset — kuyumcu seçer; ürün kartında birim türü gram/adet | M1 |
| **M4 Kural ve kategori varsayılanı** | "Ayarlar bölümü… bilezik ise şu kadar, kolyeyse bu kadar, tek tek yapma" | Kategoriye varsayılan satış milyemi/KDV; Ayarlar'da kural listesi (kategori/çeşit birincil, ad içeriği öneri); önizleme + toplu uygula; elle değişen "kural dışı" | M1, M2 |
| **M5 Ekran: gram + TL yan yana** | "Hem gramını görmem hem değerini görmem lazım" | Tezgâh satırı, ürün kartı, fiş: gram + TL; has yalnız ek görünüm | M1 |
| **M6 Fatura: belge türü listesi + kuyumcu haddi** | "Fişe gerek yok… TC'yi yazın, girdiğim anda fatura kesilsin" | Tezgâhta belge türü listesi (Fiş / Fatura — TC ile / Gün sonu toplu), kuyumcu varsayılanı Ayarlar'da; `KUYUMCU_FATURA_CARPANI` yasal parametre (3 × FATURA_HADDI_TL = 36.000) — üstünde ad + geçerli TCKN/VKN zorunlu; 11111111111 yalnız sınır altı anonim istisnası; satış anında fişten fatura otomatik | GİB gönderimi entegratöre bağlı |
| **M7 Banka: sağlayıcı + simülasyon + canlı** | "Düşer düşmez hemen şak ekranda görüyoruz… şimdilik simülasyon" | `IBankaSaglayici` + sahte sağlayıcı (demo "havale gönder") → ekstre sahnesi (onay kuyruğu, mükerrer, tek kapı korunur); arka plan çekici; ekranda canlı yenileme; (hesap, banka_referans) tekilliği | — |
| **M8 Banka: eşleştirme + cari borç + fatura önerisi** | Faturalog: "gelen ödeme cariyle eşleşip fatura oluşur" | VKN/TCKN ve açıklama eşleştirme önerisi; eşleşen tahsilat cari borcunu düşürür (bilinen hata — MASAK çift sayım tasarımıyla); açık fatura adayı tek tık; yeni cari önerisi; cariye elle IBAN | M7 |
| **M9 İşçilik belgesi + kâr raporu** | "Aradaki masrafı fatura etmen lazım, yoksa kâr görünür" | Alış işçiliğinde belge türü (fatura/gider pusulası) seçimi, belgesiz uyarı; kâr raporu 3 sütun: has kâr (g) · TL altın kârı · işçilik kârı | M1 |
| **M10 Fiyat kaynağı seçimi** | "Harem altından çekeceksin… ürün ürün farklı yerden, sabit fiyat" | Kaynak soyutlamasına Harem/İKO adaptörü + ürün başına kaynak seçimi + adet kademesi | **Kurucu: yazılı veri izni** |
| **M11 Faturalog kalanı** | Faturalog eşitliği | Mükellef sorgusu, e-Fatura/e-Arşiv ayrımı, gelen e-fatura, toplu (gün sonu), 10 yıl saklama, Luca/Zirve biçimi | **Entegratör seçimi** (ayrı oturum) + mali müşavir örnek dosyası |

**KDV "binde" oranı:** listede seçenek olarak yer alır ama **kilitli** — yasal hesap (KDVK 23/e: satış − has bedeli) varsayılan; "binde" her satışta gösterge/uyarı. Sabit oranla matrah ancak mali müşavir yazılı onayıyla açılır.

**Kurucu kararı bekleyenler:** entegratör seçimi · Harem/İKO veri izni · Borsa İstanbul kapanış fiyatı kaynağı · mali müşavir teyitleri (binde, stopaj, Luca/Zirve biçimi).
