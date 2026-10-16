# ADR-0013 — Taş kökeni bilinmiyorsa ürün satılamaz (fail-closed)

**Durum:** Kabul edildi · **Tarih:** 2026-09-14 · **Oylama:** oybirliği (Tamer Bey mühürledi)

## Bağlam

Kuyum Ticareti Yönetmeliği md.11'e **RG 24.06.2026 / 33290** ile eklenen **(e) bendi**, laboratuvar ortamında üretilen veya insan müdahalesiyle oluşturulan kıymetli taş ürünlerinin "sentetik", "laboratuvar üretimi", "yapay üretim" ibarelerinden **en az birine** etiket, ürün sertifikası, fatura, internet sayfası ve tanıtım materyallerinde **açıkça yer vermeksizin satışa sunulamayacağını** düzenler; ayrıca doğal taşlarla **ayrı bölümlerde / ayrı kategorilerde** sunulmasını ister.

Ürün kartında bugüne kadar yalnız `tas_agirlik` vardı. Köken bilgisi hiç tutulmuyordu. Mevcut 29 ürünün **6'sı taşlı** ve hepsinin kökeni **bilinmiyor**.

Üç seçenek tartışıldı:

- **(a)** Köken alanını ekle, boş bırakılabilsin, satışta yalnız **uyar**.
- **(b)** Köken boşsa ürünü **doğal** say (yönetmeliğin zorunlu ibaresi yalnız yapay taşlar için).
- **(c)** Köken boşsa taşlı ürün **satılamaz** (fail-closed).

## Karar

**(c)** seçildi.

1. `tas_kokeni` ve `tas_ibaresi` sütunları eklendi (ikisi de **nullable** — göç geriye dönük kayıtları bozamaz).
2. `TasKapisi.Dogrula` ürün oluşturma ve güncellemede çalışır: taş ağırlığı > 0 ve köken yok/`TAS_YOK` ise **400**.
3. `TasKapisi.SatisaUygunMu` satış ve takasta çalışır: göç öncesi kökensiz kayıt **satılamaz**, transaction hiç açılmaz.
4. Köken tek başına güncellense bile kapı çalışır — doğrulama, has hesabını etkileyen alanların bloğunun **dışındadır**.
5. `TAS_YOK` bir kaçış değeri değildir: taşlı üründe reddedilir.
6. `?tasKategori=DOGAL_TAS | YAPAY_TAS | TASSIZ | KOKEN_EKSIK` ile vitrin ayrımı yapılır.

## Gerekçe

**(b) reddedildi** çünkü bilgi eksikliği, taşın doğal olduğu anlamına gelmez. Varsayılanı "doğal" yapmak, sentetik bir taşı yanlışlıkla doğal olarak etiketlemek demektir — bu, yönetmeliğin **engellemek için var olduğu** tam olarak o durumdur. Sessiz bir varsayım, işletmeyi yaptığını bilmediği bir ihlale sokar.

**(a) reddedildi** çünkü uyarı, satışı durdurmaz. Yönetmelik "satışa sunamaz" der; uyarı verip satışa izin veren bir sistem, ihlali kayda geçirmiş ama engellememiş olur.

Bu, MASAK kimlik tespiti (ADR-0005) ve tevsik kapısıyla **aynı desendir**: mevzuat kapıları sisteme ihlali engelletir, ihlali sonradan raporlamaz.

## Sonuçlar

**Olumlu**
- Etiket/fatura/internet ibaresi, kategori ayrımı ve satış engeli tek bir doğrulama noktasından gelir (`Modules/Urun/TasKapisi.cs`).
- Mutasyon kanıtı alındı: fail-closed satırlar devre dışı bırakıldığında **7 test kırmızıya** döndü, geri alınınca yeşil.
- Türkçe büyük **İ** harfi kapanı yakalandı: `OrdinalIgnoreCase` "SENTETİK" ile "sentetik"i eşleştirmez. Karşılaştırma `tr-TR` kültürüyle yapılıyor — kuyumcunun Türkçe büyük harfle yazdığı etiket reddedilmiyor.

**Olumsuz — ve bunun bedeli işletmenindir**
- **`kuyumcu_net` içindeki 6 taşlı ürün bugün satılamaz durumdadır.** Kökenleri bilinmediği için sistem tarafından **tahmin edilmemiştir** (Anayasa md.1). İşletme bunları `?tasKategori=KOKEN_EKSIK` ile listeleyip tek tek girmek zorundadır. Bu, bilinçli bir veri borcudur; gizlenmemiştir.
- Fatura **satır kalemi modeli yoktur** (`faturalar` tablosu başlık düzeyindedir). İbare bu nedenle faturaya otomatik **basılamıyor**; `/urunler/{id}/etiket` ucundan alınan metin elle taşınmak zorunda. → **TB-17**.
