# PAKET PATCH-SOZLESME — uygulama raporu (02.10, dalga PATCH-SOZLESME · t9 · 5188)

Rol: Taşkın (backend). Ekran dosyasına dokunulmadı (Ertuğ işi bu pakette doğmadı — aşağıda gerekçe).
Yeniden başlatma devri (DEVİR NOTU 2): ağaçta önceki uygulayıcıdan yalnız `SubeModulu.cs` değişikliği vardı
(SubeGuncelleDto). Ağaç baştan yazılmadı: Şube gözden geçirildi ve korundu; tarama, kalan düzeltmeler,
testler, mutasyonlar ve canlı kanıt bu turda üretildi. Kanıt kopyaları: `ham/PATCH-SOZLESME-olcum/`,
mutasyon yedekleri: `ham/mut-yedek-PATCH/`.

## TUR 3 (02.10 ~15:20) — doğrulayıcı bulguları (kampanya 500 · sayım has yuvarlaması) + TÜM PATCH uçlarında kolon kapasitesi taraması

Tur 1 ve tur 2 bölümleri aşağıda korunmuştur. Kanıt kopyaları: `ham/PATCH-SOZLESME-olcum/tur3/` (canlı çıktı, mutasyon özeti + her mutasyonun kırmızı/özet günlüğü `mut-log/`, denetim, tam süit özeti, tarama tablosu); mutasyon yedekleri `ham/mut-yedek-PATCH/tur3/`.

### T3.1 Bulgu → karar

| Bulgu (doğrulayıcı) | Önem | Karar / değişiklik | Kanıt |
|---|---|---|---|
| Kampanya PATCH `{"deger":1e14}` → **500** (kampanyalar.deger decimal(15,2)); POST'ta da | KRİTİK | (a) `KampanyaDogrula` içinde (POST + PATCH ORTAK kapı) `KolonTavani.Denetle(deger, 2, D15_2)` → 400 alan:deger. Karşılaştırma kolona yuvarlanmış değerle: 9.999.999.999.999,995 → 400 (kolonda 10¹³ olurdu), …,994 → 200 (kolonda …,99). (b) DTO Range üst sınırı `double.MaxValue` → `10_000_000_000_000` (KampanyaDto + KampanyaGuncelleDto); tam kolon tavanını servis söyler. (c) Genel aralık çevirisi (DogrulamaSozlugu) üst sınır ≥10⁹ ise "0,01 veya daha büyük olmalıdır" diyordu — canlıda ölçüldü, YANILTICI; Range'e elle ileti verildi: "İndirim değeri 0,01 ile 9.999.999.999.999,99 arasında olmalıdır" | `PATCH_kampanya_kolon_tavanini_asan_deger_400_alan_deger_SQL_DEGISMEZ` ×3, `PATCH_kampanya_tam_tavan_9999999999999_99_200_kolonda_birebir`, `Kampanya_servis_kapisi_PATCH_ve_POST_kolon_tavani_400_alan_deger`, `Kampanya_DTO_Range_ust_siniri_kolon_kapasitesi`; doğrulayıcının `Sinir2_kampanya_kolon_tavanini_asan_deger_400_500_DEGIL` YEŞİL; M3-5…M3-8, M12-1; canlı + SQL |
| Sayım defterine yazılan has ToEven, fatura AwayFromZero (0,916667 × 1,5 → 1,375000 / 1,375001) | ORTA | İstemin KABUL maddesi kuralı belirledi (fatura kuralı = AwayFromZero). `StokSayimGuncelle`: hareket has gramı ve `stok.toplam_has_gram` artık `OzelMatrahMotoru.Yuvarla6` (fatura `KalemToplamHas` ile aynı fonksiyon). Ürün PATCH'inde stokların toplam has'ı da aynı fonksiyonla (tek kural). Proje genelindeki diğer 6 hane ToEven kullanımlarına dokunulmadı (bu paketin dosya alanı değil — açık kalanlar) | `PATCH_stok_sayim_defter_has_yarim_milyonda_AwayFromZero_fatura_ile_AYNI` (+ ve − yön), `PATCH_stok_sayim_stok_toplam_has_yarim_milyonda_AwayFromZero`; doğrulayıcının `Sinir2_stok_sayim_defter_has_…_1_375001` testinin Skip'i kaldırıldı (testin kendi yorumu "düzeltilince Skip kaldırılır" diyordu) → YEŞİL; M4-7, M4-8; canlı SQL `1.375001 / -1.375001` |
| (KABUL) TÜM PATCH uçlarında kolon kapasitesini aşan sayı → 400 taraması | — | Tarama T3.2. Bulunan 500'lerin hepsi kapatıldı (Kasa/Cari/Satış/Virman/Çıktı hariç) | T3.2 – T3.5 |

Yeni ortak yardımcı: `Kuyumcu.Api/Modules/KolonTavani.cs` — tavan sabitleri kolon tiplerinden (D12_2, D12_3, D12_4, D14_2, D15_2, D15_6); `Denetle` (yuvarlanmış değerle karşılaştırma, 400 + alan), `JsonSayi` (JsonElement uçlarında `GetDecimal` decimal aralığı dışında FormatException → 500 veriyordu; şimdi 400 alan), `JsonListe<T>` (kalem dizisi çözümünde JsonException → 400 alan). İş eşiği UYDURULMADI; tavanların tamamı `information_schema` kolon tipinden.

TASARIM_NOTU (gölgeleme, mutasyonla bulundu): bir tavanın testi, aynı istekte daha sonra çalışan türetilmiş tavan (satır/başlık/stok has toplamı) tarafından da yakalanıyorsa mutasyon hayatta kalıyordu (M5-5, M7-3 ilk koşum). Testler ayrıştırıldı (stok 0'lanarak; miktar 0,001 ile satır sığdırılarak). Vezne'de işçilik ve satır tutarı kapıları fiş toplamı kapısının tam alt kümesi olduğundan (işçilik ≤ satır ≤ fiş toplamı) ölü kod bırakılmadı: tek kapı `KalemleriKur` sonunda fiş toplamı (POST + PATCH ortak); fatura kaleminde satır toplamı kapısı da aynı gerekçeyle başlık toplamı kapısına bırakıldı.

### T3.2 Kolon kapasitesi taraması — TÜM PATCH uçlarının sayısal alanları

Yöntem: 61 PATCH ucunun gövdeden okuduğu her sayısal alan → yazıldığı kolon tipi (`information_schema.columns`, kuyumcu_net) → üst sınır kapısı var mı. 15 DTO'nun sayısal üye sayısı ayrıca betikle sayıldı (0). Ölçüt: kolona sığmayan değer ya da decimal aralığını aşan JSON sayısı 500 değil 400 + alan; satır değişmez.

| Uç | dosya | sayısal alan → kolon | Önceki | Karar |
|---|---|---|---|---|
| PATCH /fiyat/kampanyalar/{id} (+POST) | Fiyat/FiyatModulu.cs | deger → decimal(15,2) | **500** | **DÜZELTİLDİ** |
| PATCH /urunler/{id} | Urun/UrunModulu.cs | gramAgirlik, tasAgirlik → (12,4); türetilmiş stok.toplam_has_gram (15,6) | **500** (Range üst = double.MaxValue) | **DÜZELTİLDİ** |
| PATCH /urunler/{id} | Urun/UrunModulu.cs | satisFiyati, alisMaliyeti, konsinyeBirimBedel → (15,2); iscilikTl → (12,2); minStok → (12,3) | **500** | **DÜZELTİLDİ** |
| PATCH /urunler/{id} | Urun/UrunModulu.cs | gercekMilyem ≤1000 → (7,2); pirlantaKarat ≤999 → (8,3); hedefMarj ≤500 → (6,2); kdvOrani ≤100 → (5,2) | Range kolon içinde | uygun |
| PATCH /urunler/{id}/stok/{subeId} | Urun/UrunModulu.cs | adet → (12,3); adet×has → (15,6) | tur 2'de kapatıldı | uygun |
| PATCH /faturalar/{id} | Fatura/FaturaModulu.cs | toplam → faturalar.toplam (15,2); `GetDecimal` | **500** | **DÜZELTİLDİ** |
| PATCH /faturalar/{id}/kalemler/{k} | Fatura/FaturaModulu.cs | miktar (12,3) · birimFiyat (15,2) · iscilikTl (12,2) · indirimTutari (15,2) · türetilmiş has_grami (15,6) · başlık toplamı (15,2) | **500** (+ iki SaveChanges arasında kısmi yazım riski) | **DÜZELTİLDİ** (türetilmiş kapılar İLK kayıttan önce) |
| PATCH /siparisler/{id} | Siparis/SiparisModulu.cs | tahminiTutarTl → (14,2); kalem adet, gramAgirlik → (12,3); `GetDecimal` | **500** | **DÜZELTİLDİ** (kalem kapısı POST ile ortak `KalemKur`) |
| PATCH /vezne/fisler/{id} (+POST) | Vezne/VezneModulu.cs | tutarTl → talep_tutar_tl (15,2); kalem adet (12,3), birimFiyatTl (15,2); fiş toplamı → toplam_tl (15,2, vezneleme damgası) | **500** | **DÜZELTİLDİ** (`KalemleriKur` ortak kapı) |
| PATCH /satinalma/{id} | Satinalma/SatinalmaModulu.cs | kalem adet (12,3), birimFiyatTl (15,2); `Deserialize` | **500** | **DÜZELTİLDİ** (kalem kapısı POST ile ortak `KalemKur`) |
| PATCH /fiyat/listeler/{id} | Fiyat/FiyatModulu.cs | hasMarjYuzde −100..500 → (7,3); iscilikCarpanYuzde 0..500 → (7,2) | servis AraligiDogrula | uygun |
| PATCH /fiyat/listeler/{id}/urun-fiyatlari/{u} | Fiyat/FiyatModulu.cs | sabitFiyat ≤10⁸ → (18,2); marj/çarpan Range | kolon içinde | uygun |
| PATCH /pano/ayarlar/{kod} | Pano/PanoModulu.cs | marjlar/yuvarlama → ayar metni | MarjMutlakTavan + yuvarlama beyaz listesi | uygun |
| PATCH /pos/cihazlar/{id}, …/taksit-oranlari/{o} | Pos/PosModulu.cs | komisyon 0..1 → (5,4); valör 0..90 → int | Range | uygun |
| PATCH /telemetri/…/destek-dakikasi/{k} | Telemetri/TelemetriModulu.cs | dakika → int | Range(1, DakikaTavani) | uygun |
| PATCH /isler/ayarlar | Isler/IslerModulu.cs | saat/gün → ayar | Range | uygun |
| PATCH /urun-kategorileri/{id} · /gorunum/kullanici | Urun/KategoriModulu.cs · Gorunum/KullaniciGorunumModulu.cs | sira, yaziBoyutu → int | int gövde → int kolon (int32 dışı JSON çözücüde 400) | uygun |
| Diğer 40 PATCH ucu (şube, reyon, gider kategorisi, evrak, emanet hareketi, sayım oturumu, gider pusulası, irsaliye, admin ×6, MASAK ×4, kullanıcı ×7, banka ×3, içe aktarım, abonelik, tercih, yedek ×2, kiracı görünümü, sistem ×2, kur kaynağı, kurulum, fatura durumu) | — | sayısal alan YOK | — | kapsam dışı (sayı yok) |
| PATCH /kasalar/{id}, …/hareket/{h}, /cari/{id}, /cari/{id}/pep, /satis/rezervler/{id} | Kasa, Cari, Satış | — | — | **SONRAKİ PAKET** (PARA-DEMO-2 alanı, taranmadı/dokunulmadı) |

### T3.3 Testler

- Yeni sınıf `PatchSozlesmeKolonTavaniTestleri` (`Kuyumcu.Tests/PatchSozlesmeTestleri.cs` sonunda): 17 test metodu / 49 vaka. Her vaka: 400 + `alanlar.<alan>` (gövde AYRIŞTIRILARAK) ve SQL'de satır önce/sonra birebir.
- Filtreli `FullyQualifiedName~PatchSozlesme|FullyQualifiedName~PATCHSOZLESME`: **99/99 geçti** (tur 2: 49; doğrulayıcının Skip'li testi artık koşuyor ve yeşil).
- İlgili kümeler (Fatura|Siparis|Vezne|Satinalma|Tedarik|Kampanya|Sayim|P54|UrunHttp|Fiyat): **1025/1025 geçti** (Ürün ek kapıları öncesi koşum); son kodla tam süit T3.6.
- 1e30 vakası iki DTO ucunda (kampanya, ürün) çerçevenin JSON çözücüsünde 400 olur; `alanlar` anahtarı `"$.deger"` (tur 2 T2.9 genel açık) — testte yalnız 400 (500 değil) sınanır.

### T3.4 Mutasyon tablosu (tur 3)

Yöntem: kaynak → `ham/mut-yedek-PATCH/tur3/<dosya>.orijinal-<kod>` → `perl` TEK eşleşme (betik eşleşme sayısı 1 değilse durur) → `dotnet test --filter "PatchSozlesmeKolonTavani|PATCHSOZLESME"` → yedeği geri kopyala → `cmp`. Betik: `ham/PATCH-SOZLESME-olcum/tur3/mut3.zsh` (+ sürücüler).

⚠ Anayasa md.1 notu: koşum sırasında başka şeritler (PARA-DEMO-2/3, KAR-RAPOR) Kuyumcu.Web ve test projesini ara ara derlenemez bıraktı (`ParaDemo2Metinleri.KartHavaleEtiket`, `HurdaGirisi`, `KarZarar.razor decimal?`, `KARRAPORSinirTestleri Zaman`, `ParaDemo3Sozlugu`…). O anlarda testler HİÇ koşmadı; betik bunu "GECERSIZ-DERLEME-YOK" diye işaretledi (ilk sürümde "kırmızı yok" gibi görünüyordu — betiğe derleme yok denetimi eklendi). Geçersiz koşumlar (M4-7, M7-5…M7-10, M3-8) ağaç derlenir hâle gelince YENİDEN koşuldu; aşağıdaki tablo yalnız GEÇERLİ koşumlardır. Bir koşum bu nedenle yarıda kesildi (M5-6 sırasında): kalan mutasyonlu dosya yedekten geri kopyalandı, `cmp` AYNI.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M3-5 | KampanyaDogrula kolon kapısı silindi | Kampanya_servis_kapisi_PATCH_ve_POST_kolon_tavani_400_alan_deger | AYNI |
| M3-6 | KampanyaGuncelleDto Range üst → double.MaxValue | Kampanya_DTO_Range_ust_siniri_kolon_kapasitesi | AYNI |
| M3-7 | KampanyaDto Range üst → double.MaxValue | Kampanya_DTO_Range_ust_siniri_kolon_kapasitesi | AYNI |
| M3-8 | PATCH DTO Range ErrorMessage silindi | PATCH_kampanya_kolon_tavanini_asan_deger_400_alan_deger_SQL_DEGISMEZ(1e14) — ileti "veya daha büyük" | AYNI |
| M4-7 | stok toplam has Yuvarla6 → Math.Round(,6) | PATCH_stok_sayim_stok_toplam_has_yarim_milyonda_AwayFromZero | AYNI |
| M4-8 | defter has Yuvarla6 → Math.Round(,6) | PATCH_stok_sayim_defter_has_yarim_milyonda_AwayFromZero_fatura_ile_AYNI + doğrulayıcı Sinir2_stok_sayim_defter_has_… | AYNI |
| M5-5 | ürün gramAgirlik kapısı silindi | PATCH_urun_agirlik_kolon_tavanini_asar_400_alan_SQL_DEGISMEZ(gramAgirlik) | AYNI |
| M5-6 | tasAgirlik kapısı silindi | …(tasAgirlik) | AYNI |
| M5-7 | stok toplam has tavanı → decimal.MaxValue | PATCH_urun_gram_artinca_stok_toplam_has_tasarsa_400_urun_ve_stok_DEGISMEZ | AYNI |
| M5-8…M5-12 | satisFiyati / alisMaliyeti / konsinyeBirimBedel / iscilikTl / minStok kapıları silindi | …(satisFiyati) / (alisMaliyeti) / (konsinyeBirimBedel) / (iscilikTl) / (minStok) | AYNI ×5 |
| M7-2 | fatura kalem miktar kapısı | PATCH_fatura_kalem_kolon_tavani_…(miktar 10⁹) | AYNI |
| M7-3 | birimFiyat kapısı | …(miktar 0,001 + birimFiyat 10¹⁴) | AYNI |
| M7-4 | iscilikTl kapısı | …(iscilikTl 10¹⁰) | AYNI |
| M7-5 | indirimTutari kapısı | …(indirimTutari 10¹³) | AYNI |
| M7-6 | satır has gramı kapısı | PATCH_fatura_urunlu_kalem_has_gram_kolonu_tasarsa_400_alan_miktar_DEGISMEZ | AYNI |
| M7-7 | başlık toplamı kapısı | …(miktar 999999 × birimFiyat 10⁸) + PATCH_fatura_kalem_baslik_toplami_tasarsa_400_ILK_kayit_da_YAZILMAZ | AYNI |
| M7-8 | `Sayi` JsonSayi → GetDecimal | …(miktar 1e30) | AYNI |
| M7-9 | başlık toplam tavanı → decimal.MaxValue | PATCH_fatura_baslik_toplam_kolon_tavani_400_alan_toplam_DEGISMEZ(10¹³) | AYNI |
| M7-10 | `SayiYaDaNull` JsonSayi → GetDecimal | …(iscilikTl 1e30) | AYNI |
| M9-1…M9-5 | sipariş tahmini tutar / kalem adet / gram kapıları; tahminiTutarTl ve adet JsonSayi → GetDecimal | PATCH_siparis_kolon_tavani_400_alan_siparis_DEGISMEZ (tahminiTutarTl 10¹² / adet 10⁹ / gramAgirlik 10⁹ / tahminiTutarTl 1e30 / adet 1e30) | AYNI ×5 |
| M10-1…M10-3 | vezne kalem adet / birim fiyat / fiş toplamı kapıları | PATCH_vezne_kalem_kolon_tavani_… (adet 10⁹ / 0,001×10¹⁴ / 10⁶×10¹⁰ ve işçilik 10¹³) + Vezne_fis_toplami_kolonu_tasarsa_PATCH_ve_POST_400_alan_kalemler | AYNI ×3 |
| M10-4, M10-5 | tahsilat tutarTl kapısı; JsonSayi → GetDecimal | PATCH_vezne_tahsilat_tutar_kolon_tavani_400_alan_tutarTl_DEGISMEZ (10¹³ / 1e30) | AYNI ×2 |
| M11-1, M11-2 | satın alma kalem adet / birim fiyat kapıları | PATCH_satinalma_kalem_kolon_tavani_400_alan_kalemler_DEGISMEZ (adet 10⁹ / fiyat 10¹³) | AYNI ×2 |
| M12-1 | `Denetle` yuvarlamasız karşılaştırma | PATCH_kampanya_tam_tavan_9999999999999_99_200_kolonda_birebir (…,994 → 200 olmalı) | AYNI |
| M12-2 | `JsonListe` catch(JsonException) devre dışı | PATCH_vezne_kalem_…(adet 1e30) + PATCH_satinalma_kalem_…(adet 1e30) | AYNI |

**37/37 geçerli mutasyon kırmızı, 37/37 cmp AYNI.** Test açığı bulunup kapatılan yerler (gizlenmedi): M5-5 ilk koşumda hayatta kaldı (stok has kapısı gölgeliyordu → test fikstüründe stok 0); M7-3 ilk koşumda hayatta kaldı (başlık toplamı gölgeliyordu → miktar 0,001); M12-1 ilk koşumda hayatta kaldı (yuvarlamanın yalnız …,994 gibi değerleri KABUL ettiren tarafı sınanmıyordu → sınır vakası eklendi; bu vaka DTO Range'i double hassasiyetinde tavana çok yakın tutmanın …,994'ü reddettiğini de gösterdi → Range üst sınırı 10¹³, tam tavan servis kapısında). Son mutasyondan sonra temiz derleme: **0 Hata**; `Kuyumcu.Api` `--no-incremental` derlemesi **0 Uyarı 0 Hata** (tam çözümde görülen uyarılar başka şeritlerin test dosyalarında: SUBEKAPSAMASinirTestleri CS8601, KesifP2Testleri xUnit2029, ParaDemo3Testleri CS8631, bir de eşzamanlı derlemeden MSB dosya kilidi).

### T3.5 Canlı doğrulama (API 5188, bağımsız SQL — kuyumcu_net)

Kiracı `PAKETPATCH-TEST tur3 …` sistem davetiyle açıldı (rastgele anahtar /private/tmp altında chmod 600, yalnız API ortam değişkenine verildi; iş sonunda dosya boşaltıldı). Ham: `ham/PATCH-SOZLESME-olcum/tur3/canli-tur3.txt` (+ `canli-tur3.mjs`). Son koşum (ileti düzeltmesinden sonra, API yeniden başlatıldı): **32 kapı isteğinin 32'si 400**, 500 yok.

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| PATCH kampanya `{"deger":100000000000000}` | 400 alan:deger, doğru ileti | 400 "İndirim değeri 0,01 ile 9.999.999.999.999,99 arasında olmalıdır" | — |
| PATCH kampanya `9999999999999.995` / `1e30` | 400 | 400 "İndirim değeri çok büyük…" alan:deger / 400 (çözücü, "$.deger") | 3 ret sonrası `250.00` |
| PATCH kampanya `9999999999999.99` | 200 | 200 | `9999999999999.99` |
| POST kampanya deger 1e14 | 400 | 400 | kampanya sayısı `1` |
| PATCH ürün gramAgirlik 10⁸ · tasAgirlik 10⁸ · satisFiyati 10¹³ · iscilikTl 10¹⁰ · alisMaliyeti 10¹³ · minStok 10⁹ | 400 ×6, alan | 400 ×6, her biri kendi alanıyla | önce/sonra `10.0000 0.0000 9.160000 ∅ 0.00 ∅ ∅` (DEĞİŞMEDİ) |
| PATCH stok `{"adet":1.5}` (has 0,916667) | 200, has 1,375001 | 200, `toplamHasGram 1.375001` | stok `1.500 1.375001`, defter `1.375001` |
| PATCH stok `{"adet":0}` | 200, defter −1,375001 | 200 | defter `1.375001,-1.375001` |
| PATCH fatura kalem miktar 10⁹ · 1e30 · birimFiyat 10¹³ · iscilikTl 10¹⁰ · indirimTutari 10¹³ · (999999 × 10⁸) | 400 ×6 | 400 ×6 (son: "Fatura toplamı çok büyük", alan:miktar) | önce/sonra `2000.00 333.33 2.000 1000.00 ∅ ∅ 2000.00` |
| PATCH fatura başlık toplam 10¹³ / 1e30 | 400 ×2 alan:toplam | 400 ×2 | `1200.00 200.00 1000.00` değişmedi |
| PATCH sipariş tahminiTutarTl 10¹² / 1e30 · kalem adet 10⁹ · gram 10⁹ | 400 ×4 | 400 ×4 | `∅ 1.000 ∅` değişmedi |
| PATCH vezne kalem adet 10⁹ · 0,001×10¹⁴ · 2×6·10¹² · adet 1e30 | 400 ×4 alan:kalemler | 400 ×4 | `∅ 1.000/1000.00/0.00` değişmedi |
| POST vezne 2 kalem × 6·10¹² | 400 alan:kalemler | 400 "Fiş toplamı çok büyük…" | fiş sayısı `1 / 1` |
| PATCH tahsilat fişi tutarTl 10¹³ / 1e30 | 400 ×2 alan:tutarTl | 400 ×2 | `500.00` değişmedi |
| PATCH satın alma kalem adet 10⁹ · fiyat 10¹³ · adet 1e30 | 400 ×3 alan:kalemler | 400 ×3 | `2.000/100.00` değişmedi |
| PATCH /kur/kaynak MÜDÜR / SAHİP (regresyon) | 403 / 200 | 403 "gerekli rol: dükkân sahibi" / 200 `{"secili":"ALTINKAYNAK"}` | tenants.kur_kaynagi `ALTINKAYNAK` |

API (PID 68633) durduruldu: `lsof -nP -iTCP:5188 -sTCP:LISTEN` → 0 satır. Bu turda açılan 3 test kiracısı (ilk koşum SQL sorgusundaki tablo adı hatasıyla yarıda kaldı, ikinci koşum ileti düzeltmesinden önceydi, üçüncüsü son kanıt) `zsh betikler/kiraci-temizle.sh <id> --uygula` ile silindi; temizlikten sonra bir kiracıya ait 1 `kur_snapshots` satırı kalmıştı (silme sırasında kur yoklayıcısının yazdığı satır) → elle silindi; 75 tenant_id'li tabloda 3 kimlik için toplam satır **0**; `PAKETPATCH%` kiracı **0**; tenants başlangıçta **15** → sonda **15** (arada başka şeritlerin kiracıları açılıp kapandı).

⚠ Anayasa md.1 — sır olayı: canlı betiğin ilk koşumunda SQL yardımcı fonksiyonu hata verince Node'un hata iletisi docker komut satırını (MySQL parolası dahil) `/private/tmp/kuyumcu-serit/PATCH-SOZLESME/canli-tur3.txt` dosyasına yazdı. Dosya hemen `printf '' >` ile boşaltıldı, betik hatayı yutup yalnız çıkış kodunu yazacak biçimde düzeltildi; sonraki çıktılarda parola dizgesi `grep -c` ile aranıp **0** bulundu, `ham/` kopyalarında da 0. Değer bu rapora/proje ağacına girmedi. Parola /private/tmp'de birkaç saniye düz metin durduğundan döndürülmesi (rotasyon) kurucunun takdirine bırakılır.

### T3.6 Tam süit (son kodla, tek koşum) ve denetim

`KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test … --no-build -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-SOZLESME`:
**Toplam 9377 · Başarısız 3 · Atlandı 2 · geçti 9372.** Özet: `ham/PATCH-SOZLESME-olcum/tur3/tam-suit-ozet.txt`.

| Kırmızı | Sahibi | Tek başına tekrar | Bu paketle ilişki |
|---|---|---|---|
| ParaDemoTestleri…`ParaDemoDbTestleri.PD1_toplam_bakiye_sayilirsa_kart_havale_kadar_FAZLA_cikar_gerekce_ister` ("Sayım ile defter arasında … fark var" iletisi) | PARA-DEMO-2 (KasaModulu) | KIRMIZI (deterministik) | yok — ileti `Kasa/KasaModulu.cs`'de; Kasa HARİÇ alan, dokunulmadı |
| `Dogrulayici.PARADEMOSinirDbTestleri.PDS_kurus_0_005_fark_bir_kurusa_yuvarlanir_gerekce_ister` | PARA-DEMO-2 | KIRMIZI (deterministik) | yok — aynı Kasa iletisi |
| `Dogrulayici.KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri` (bUnit WaitFor zaman aşımı) | KATMAN2 ekran | YEŞİL (yük altı kararsızlık) | yok — ekran testi, bu paket ekrana dokunmadı |

**Bu paketin yeni kırmızısı: 0.**

`bash dotnet/denetim.sh`: rc=1, 302 ✓, 2 ✗: `P-30: DEMO_INSAN_SURESI` (istemde HARİÇ) ve `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47` — bu sayaç YALNIZ `Kuyumcu.Web/Pages|Bilesenler|Layout/*.razor` dosyalarını sayar; bu paket hiçbir Web/razor dosyasına dokunmadı (değişen dosyalar listesi) → sahibi o sırada Web ekranlarını değiştiren şerit (PARA-DEMO-2/3 · TOPLU-4), orkestratöre. Bu paketin sayaçları ✓. Ham: `ham/PATCH-SOZLESME-olcum/tur3/denetim.txt`.

### T3.7 Ekran
Bu turda razor/css/sözlük dosyası değişmedi. Değişiklik yalnız sunucu: kolon kapasitesini aşan değerde 500 yerine alan işaretli 400 (ekran mevcut alan altı hata kalıbıyla gösterir). Kullanıcının normal girdisi (kolon tavanı altındaki her değer) aynı sonucu üretir — tarayıcı ölçümü gerektiren görünüm değişikliği yok (§ 9 gerekçesi geçerli).

### T3.8 Değişen dosyalar (tur 3)

- `dotnet/Kuyumcu.Api/Modules/KolonTavani.cs` — YENİ (ortak kapı).
- `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` — KampanyaDogrula kapısı; iki DTO'da Range üst sınırı + ileti sabiti `KampanyaDto.KampanyaDegerAraligi`.
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — Guncelle: 7 kolon kapısı + stok toplam has (Yuvarla6 + D15_6); StokSayimGuncelle: has Yuvarla6 ×2.
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` — başlık toplamı; TaslakKalemGuncelle kapıları; `Sayi`/`SayiYaDaNull` JsonSayi.
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` — tahmini tutar, KalemKur, JsonSayi ×3.
- `dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs` — tutarTl, JsonListe, KalemleriKur (adet, fiyat, fiş toplamı).
- `dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs` — JsonListe, KalemKur.
- `dotnet/Kuyumcu.Tests/PatchSozlesmeTestleri.cs` — yeni sınıf `PatchSozlesmeKolonTavaniTestleri`.
- `dotnet/Kuyumcu.Tests/Dogrulayici/PATCHSOZLESMESinirTestleri.cs` — yalnız `Skip` kaldırıldı (+1 yorum satırı); test gövdesi değişmedi.
Migration: gerekmedi. Kasa/Cari/Satış/Virman/Çıktı dosyalarına ve ekran dosyalarına dokunulmadı.

### T3.9 DoD sözleşmesi
Yeni varlık/kayıt türü doğmadı; uçların rol/yetki öznitelikleri değişmedi; yalnız mevcut düzenleme yollarına giriş kapısı eklendi. Finansal varlıklara silme eklenmedi. Denetim kaydı çağrıları değişmedi.

### T3.10 Koşul karşılama (tur 3)

| Koşul | Durum | Kanıt |
|---|---|---|
| Kampanya PATCH kolon tavanı → 400 alan:deger, 500 YOK, SQL değişmez | KARŞILANDI | 6 test + doğrulayıcı testi yeşil · M3-5…M3-8, M12-1 · canlı |
| Kampanya POST'a aynı kapı; DTO Range üst sınırı çekildi | KARŞILANDI | servis testi + DTO testi · canlı POST 400 |
| Sayım defteri has fatura kuralıyla (AwayFromZero), milyonda yarım sınır testi | KARŞILANDI | 2 test + doğrulayıcı testi (Skip kaldırıldı) · M4-7, M4-8 · canlı 1.375001 / −1.375001 |
| TÜM PATCH uçlarında kolon kapasitesi taraması (tablo) | KARŞILANDI | T3.2 |
| Bulunan 500'lerin düzeltilmesi (hariç modüller dışında) | KARŞILANDI | 7 uç ailesi, 49 vaka, 37 mutasyon, canlı 32×400 |
| Build 0 hata | KARŞILANDI | 0 Hata; API projesi 0 Uyarı |
| Denetim (P-30 hariç) | PAKET ölçüsünde KARŞILANDI (bu paketin dokunduğu dosyaların sayaçları ✓); ağaç ölçüsünde P-11 ✗ başka şeridin razor dosyalarından (paralel şerit kapısı maddesi: orkestratörün kapısı) | T3.6 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 3 kırmızı: 2 PARA-DEMO-2 (Kasa), 1 kararsız |
| Test kiracısı silindi, tenants başlangıç değerine döndü | KARŞILANDI | 15 → 15; satır 0 |

### T3.11 Açık kalanlar (tur 3 eki)

- **Tek 6 hane yuvarlama kuralı (orkestratör/ekip):** proje genelinde has/6 hane yuvarlamada ToEven ve AwayFromZero karışık kullanılıyor (doğrulayıcı sayımı 29/20). Bu paket yalnız sayım + ürün stok has yolunu fatura kuralına bağladı. Kalanlar başka modüllerde (Satış/Kasa/Rapor vb.).
- **POST uçlarında aynı sınıf 500:** ürün OLUŞTURMA (`UrunModulu.Olustur`) gram/fiyat alanları, fatura OLUŞTURMA kalemleri ve başlık toplamı (`FaturaOlusturDto.Toplam` Range double.MaxValue), vezne tahsilat OLUŞTURMA `tutarTl` hâlâ kolon kapısına sahip değil (PATCH dışı; sipariş/vezne/satın alma kalem kapıları POST ile ortak fonksiyonda olduğu için orada kapandı). Sonraki pakete: aynı `KolonTavani` yardımcısıyla.
- **Genel ağ (orkestratör):** MySQL "Out of range" (1264) DbUpdateException'ı genel hata ara katmanında 400'e çevirmek tüm yazma yollarını korur; `Common/`/`Program.cs` bu paketin dosya alanı dışında.
- **Genel aralık iletisi:** `Kuyumcu.Core/I18n/DogrulamaSozlugu.AralikMesaji` üst sınır ≥10⁹ ise "X veya daha büyük olmalıdır" der — üst sınırı aşan değerde yanıltıcıdır. Bu pakette yalnız kampanya Range'ine elle ileti verildi; genel düzeltme Core'da (orkestratör).
- PATCH/Kasa, Cari, Satış ve tur 2 açıkları (T2.9) aynen geçerli.
- PARA-DEMO-2 kırmızıları (T3.6) ve P-11 sayacı sahiplerine.
- Kurucu: MySQL parolasının birkaç saniye /private/tmp'de düz metin durması (T3.5) — rotasyon kararı kurucuda.

---


## TUR 2 (02.10 ~12:30) — hasım bulgularının kapatılması

Tur 1 raporu aşağıda (§ 1–13) korunmuştur; tur 2'de değişen/eklenen her şey bu bölümdedir ve § 11/§ 12 güncellenmiştir.

### T2.1 Bulgu → karar

| Bulgu (hasım) | Önem | Karar / değişiklik | Kanıt |
|---|---|---|---|
| Stok sayımı eksi adeti kabul ediyor (`{"adet":-1}` → 200) | KRİTİK | `UrunServisi.StokSayimGuncelle` başında `yeniAdet < 0` → 400 "Sayılan adet eksi olamaz" alan:adet. Servis içinde (controller değil) — SayimModulu da aynı servisi çağırır, kapı her iki yolu korur | test `PATCH_stok_eksi_adet_400_alan_adet_stok_ve_defter_DEGISMEZ` · M4-2 · canlı -1 / -0,001 → 400, SQL adet+hareket sayısı değişmedi |
| Çok büyük adet 500 (`999999999.99`) | KRİTİK | Üst sınır KOLON KAPASİTESİNDEN türetildi (iş eşiği uydurulmadı): `StokAdetTavani = 999 999 999,999` (stok.adet decimal(12,3)) ve `|adet × has gram| ≤ HasGramTavani = 999 999 999,999999` (stok.toplam_has_gram / hareketler.has_grami decimal(15,6)) → 400 alan:adet | `PATCH_stok_kolon_tavanini_asan_adet_400_alan_adet_500_DEGIL` (iki kol: has taşması + yalnız adet taşması — has gramı 0,5 ürünle) · M4-3, M4-4 · canlı 999999999.99 ve 1000000000 → 400 |
| Tırnaklı Türkçe biçimli sayı sessizce invariant okunuyor (`"45.000"` → 45) | KRİTİK | Genel serileştirici ayarı DEĞİŞTİRİLMEDİ (istemci etkisi orkestratörde). Paketin dokunduğu uçların sayı alanlarına `[JsonNumberHandling(JsonNumberHandling.Strict)]`: `StokSayimDto.Adet`, `FiyatListesiGuncelleDto.HasMarjYuzde`, `.IscilikCarpanYuzde`, `KampanyaGuncelleDto.Deger`. Dize sayı → 400. İstemci (`Urunler.razor:2207`, `Fiyatlandirma.razor` ListeGovde/KampanyaGovde) decimal SAYI gönderir — etkilenmez (kampanya testinde sayı gövdesi 200 ile sınandı) | `PATCH_stok_tirnakli_sayi_400_stok_DEGISMEZ` ×3 · `PATCH_fiyat_listesi_tirnakli_sayi_400_marj_carpan_DEGISMEZ` ×2 · `PATCH_kampanya_tirnakli_deger_400_deger_DEGISMEZ` · M4-6, M2-4, M2-5, M3-4 · canlı 400 + SQL değişmedi |
| Stok sayımı yarım binde ToEven (1,0005 → 1,000) | KRİTİK | `Math.Round(yeniAdet, 3, MidpointRounding.AwayFromZero)` — fatura kalem miktarıyla aynı kural | `PATCH_stok_yarim_binde_AwayFromZero_1_0005_1_001_olur` · M4-5 · canlı 1.0005 → SQL `1.001` |
| Ürün PATCH'inde ad/kod kırpılmadan yazılıyor | DÜŞÜK | `yeniKod = dto.Kod?.Trim()`, `yeniAd = dto.Ad?.Trim()`; çakışma denetimi kırpılmış kodla ve `u.Id != id` ile; yazım kırpılmış değerle | `PATCH_urun_ad_kod_KIRPILARAK_yazilir_kirpilmis_kod_CAKISIR` (409 gövdesi AYRIŞTIRILARAK: servis mesajı + alanlar.kod — veritabanı benzersizlik ağına düşmediği kanıtlı) · M5-2, M5-3, M5-4 · canlı `[Burma bilezik] [PS-KIRP]`, 409 |
| Ürün/kategori isteğe bağlı alan silme "" ile (null=koru) | DÜŞÜK | Değiştirilmedi — hasımın önerisi "sonraki pakette Verildi bayraklı DTO". § 12'de açık | — |
| Kanıt dosyaları proje ağacında (mut-yedek + 12 MB trx) | DÜŞÜK | Silme kararı kurucu/orkestratörde (DOSYA SİLME YASAK); devir notu 2 kanıtların `ham/` altına kopyalanmasını İSTİYOR. Tur 2 kanıtları da `ham/PATCH-SOZLESME-olcum/tur2/` ve `ham/mut-yedek-PATCH/tur2/` altında; tur 2'de trx ağaca KOPYALANMADI (yalnız /private/tmp) | — |
| 5199 dinlemiyor | DÜŞÜK | Bu pakete atfedilmedi; 5199'a dokunulmadı | — |

Değişen dosyalar (tur 2): `Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (StokSayimDto, StokSayimGuncelle, Guncelle), `Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` (3 öznitelik), `Kuyumcu.Tests/PatchSozlesmeTestleri.cs` (+10 test vakası: 7 metot). Migration: gerekmedi. Ekran dosyası: yok.

### T2.2 Testler

- Yeni (tur 2): 7 test metodu / 10 vaka. Paket toplamı `PatchSozlesmeHttpTestleri` + `PatchSozlesmeFaturaTestleri`: 27 vaka.
- Filtreli `FullyQualifiedName~PatchSozlesme|FullyQualifiedName~PATCHSOZLESME` (hasımın `Dogrulayici/PATCHSOZLESMESinirTestleri.cs` dahil): **49/49 geçti** (tur 1'de hasım testlerinden 6 vaka kırmızıydı: eksi, çok büyük, "45.000", "1.500", yarım binde, kampanya "45.000").
- İlgili kümeler (Fiyat|Sube|Kategori|KesifP6|Kampanya|Stok|Sayim|CariBakiye|UrunHttp|Reyon|Urun): 1351 vaka, 1350 geçti, 1 kırmızı `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (bUnit WaitFor zaman aşımı, POS ekranı — bu pakete dokunmuyor); tek başına iki kez koşuldu: 2/2 YEŞİL → yük altı kararsızlık.

### T2.3 Mutasyon tablosu (tur 2)

Yöntem: kaynak → `ham/mut-yedek-PATCH/tur2/<dosya>.orijinal-<kod>` → `perl` TEK eşleşme (betik eşleşme sayısını 1 değilse durdurur) → filtreli test → yedeği geri kopyala → `cmp`. Betik: `ham/PATCH-SOZLESME-olcum/tur2/mut.zsh`; özet: `ham/PATCH-SOZLESME-olcum/tur2/mutasyonlar-tur2.txt`.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M4-2 | `if (yeniAdet < 0m)` → `if (false)` | PATCH_stok_eksi_adet_400_alan_adet_stok_ve_defter_DEGISMEZ | AYNI |
| M4-3 | adet tavanı kapısı `if (false` | PATCH_stok_kolon_tavanini_asan_adet_400_alan_adet_500_DEGIL (Expected BadRequest, Actual InternalServerError) | AYNI |
| M4-4 | has tavanı koşulu silindi | PATCH_stok_kolon_tavanini_asan_adet_400_alan_adet_500_DEGIL | AYNI |
| M4-5 | AwayFromZero → `Math.Round(yeniAdet, 3)` (ToEven) | PATCH_stok_yarim_binde_AwayFromZero_1_0005_1_001_olur | AYNI |
| M4-6 | StokSayimDto Strict özniteliği silindi | PATCH_stok_tirnakli_sayi_400_stok_DEGISMEZ ("45.000", "1.500", "7") | AYNI |
| M2-4 | HasMarjYuzde Strict silindi | PATCH_fiyat_listesi_tirnakli_sayi_400_marj_carpan_DEGISMEZ(hasMarjYuzde) | AYNI |
| M2-5 | IscilikCarpanYuzde Strict silindi | PATCH_fiyat_listesi_tirnakli_sayi_400_marj_carpan_DEGISMEZ(iscilikCarpanYuzde) | AYNI |
| M3-4 | Kampanya Deger Strict silindi | PATCH_kampanya_tirnakli_deger_400_deger_DEGISMEZ | AYNI |
| M5-2 | kod kırpılmadan yazılır | PATCH_urun_ad_kod_KIRPILARAK_yazilir_kirpilmis_kod_CAKISIR | AYNI |
| M5-3 | ad kırpılmadan yazılır | PATCH_urun_ad_kod_KIRPILARAK_yazilir_kirpilmis_kod_CAKISIR | AYNI |
| M5-4 | çakışma denetimi kırpılmamış `dto.Kod` ile | PATCH_urun_ad_kod_KIRPILARAK_yazilir_kirpilmis_kod_CAKISIR (veritabanı ağı farklı mesaj verdi) | AYNI |
| M4-1b | (tur 1 tekrar) adet yoksa `0m` | PATCH_stok_bos_govde_400_stok_SIFIRLANMAZ_deftere_YAZILMAZ | AYNI |
| M5-1b | (tur 1 tekrar) boş ad kapısı `if (false)` | PATCH_urun_tek_alan_aciklama_ad_kod_AYNI_bos_ad_kod_400 | AYNI |

Test açığı bulunup kapatılan iki yer (Anayasa md.1 — gizlenmedi): (a) M4-3 ilk koşumda HAYATTA kaldı — 1 000 000 000 adet has tavanına da takıldığı için adet tavanı tek başına sınanmıyordu; teste has gramı 0,5 olan ürün kolu eklendi → kırmızı. (b) M5-4 ilk koşumda veritabanı benzersizlik ağı da 409 verdiği için hayatta kaldı; ikinci denemede eklenen `Assert.Contains(..., r.Metin())` JSON'daki `'` kaçışı yüzünden mutasyonsuz da kırmızıydı (yanlış kırmızı) — gövde ayrıştırılarak sınanacak biçimde düzeltildi, mutasyonsuz yeşil / M5-4 ile kırmızı (Expected "'PS-KIRP' kodu zaten kullanımda", Actual "Bu stok kodu bu işletmede zaten kullanılı…").

13/13 kırmızı, 13/13 cmp AYNI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata** (tam derlemede başka şeritlerin 2 test-projesi uyarısı görülür: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(455)` xUnit2029 — bu paketin değil).

### T2.4 Canlı doğrulama (API 5188, bağımsız SQL — kuyumcu_net)

Kiracı `PAKETPATCH-TEST tur2 …` (01M3XYXE5SFJZZW58KQ48RRGC0) sistem davetiyle açıldı (rastgele anahtar /private/tmp altında chmod 600, yalnız API ortam değişkenine verildi; iş sonunda dosya boşaltıldı). Ham: `ham/PATCH-SOZLESME-olcum/tur2/canli-tur2.txt` (+ betik `canli-tur2.mjs`).

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| PATCH /urunler/{id} `{"ad":"  Burma bilezik  ","kod":"  PS-KIRP  "}` | 200, kırpılmış yazılır | 200 | önce `[Bilezik T2] [PS-T2-A]` → sonra `[Burma bilezik] [PS-KIRP]` |
| PATCH /urunler/{id2} `{"kod":" PS-KIRP "}` | 409 alan:kod | 409 `'PS-KIRP' kodu zaten kullanımda`, alanlar.kod | ikinci ürün `[PS-T2-B]` değişmedi |
| PATCH stok `{"adet":7}` | 200 | 200 | adet `7.000`, hareket 1 |
| PATCH stok `-1`, `-0.001` | 400 alan:adet | 400 "Sayılan adet eksi olamaz" | — |
| PATCH stok `999999999.99`, `1000000000` | 400 (500 DEĞİL) | 400 "Sayılan adet çok büyük — …" alan:adet | — |
| PATCH stok `"45.000"`, `"1.500"` | 400 | 400 | 6 ret sonrası adet `7.000`, hareket `1` (DEĞİŞMEDİ) |
| PATCH stok `1.0005` | 200, 1,001 | 200 | adet `1.001`, hareket 2 |
| PATCH stok `{"adet":2}` MÜDÜR | 200 | 200 | adet `2.000`, hareket 3 |
| PATCH /fiyat/listeler `{"hasMarjYuzde":"1.500"}` / `{"iscilikCarpanYuzde":"45.000"}` | 400 | 400 / 400 | önce `Toptanci TOPTAN 2.500 80.00 1` |
| PATCH /fiyat/listeler `{"hasMarjYuzde":3.75}` | 200 | 200 | sonra `Toptanci TOPTAN 3.750 80.00 1` (çarpan korundu) |
| PATCH /fiyat/kampanyalar `{"deger":"45.000"}` | 400, değer aynı | 400 | `… TUTAR_YUZDE 7.50 1` değişmedi |
| PATCH /fiyat/kampanyalar `{"deger":8.25}` | 200 | 200 | `… 8.25 1` |
| PATCH /subeler `{"telefon":…}` (tur 1 regresyon) | 200, ad/adres aynı | 200 | `Merkez Şube ∅ ∅ 1` → `Merkez Şube ∅ 02125551111 1` |
| PATCH /kur/kaynak MÜDÜR | 403 | 403 "gerekli rol: dükkân sahibi" | `NULL` değişmedi |
| PATCH /kur/kaynak SAHİP | 200 | 200 `{"secili":"ALTINKAYNAK"}` | `ALTINKAYNAK` |

Gözlem (açık kalan, bu paketin dosya alanı DIŞI): tırnaklı sayı 400 gövdesi çerçevenin JSON tür hatasından gelir ve `alanlar` anahtarı `"$.adet"` olur, ayrıca fazladan `"dto zorunludur"` iletisi eklenir (genel `InvalidModelStateResponseFactory` + `Kuyumcu.Core/I18n/DogrulamaSozlugu.Anahtar` "$." önekini atmıyor). Bu tüm API'de geçerli önceki davranıştır; istemci sayı gönderdiği için ekranda görünmez. Düzeltme genel ayar dosyalarında — orkestratöre.

API (PID 79172) durduruldu: `lsof -nP -iTCP:5188 -sTCP:LISTEN` → 0 satır. Test kiracısı `zsh betikler/kiraci-temizle.sh 01M3XYXE5SFJZZW58KQ48RRGC0 --uygula` ile silindi; tenants önce **17** → sonra **17**; `PAKETPATCH%` 0; kiracı kimliği 0 satır.

### T2.5 Denetim

`bash dotnet/denetim.sh`: rc=1, 303 ✓, tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ, bu paketle ilgisiz). Ham: `ham/PATCH-SOZLESME-olcum/tur2/denetim.txt`.

### T2.6 Ekran
Tur 2'de de razor/css/sözlük dosyası değişmedi; değişiklik yalnız sunucu davranışı. İstemcinin gönderdiği gövdeler (sayı tipli) aynı sonucu üretir — tarayıcı ölçümü kapsam dışı (§ 9 gerekçesi geçerli).

### T2.7 Tam süit (tur 2, son kodla, tek koşum)

`KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --no-build -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-SOZLESME` (12:23 trx):
**Toplam 9247 · Geçti 9244 · Başarısız 1 · Atlandı 2.** Özet: `ham/PATCH-SOZLESME-olcum/tur2/tam-suit-ozet.txt` (12 MB trx bu tur ağaca kopyalanmadı).

| Kırmızı | Sahibi | Tek başına tekrar | Bu paketle ilişki |
|---|---|---|---|
| Dogrulayici.PARADEMO2SinirTestleri.S16_elle_kart_cikisi_kart_payini_asamaz_1200_400 (Expected BadRequest, Actual OK) | PARA-DEMO-2 (t2) — test dosyası 12:13'te değişti (o şeridin hasım doğrulayıcısı şu an yazıyor), kural KasaModulu elle kart çıkışında | KIRMIZI (deterministik) | yok — Kasa modülü bu paketin HARİÇ alanı, dokunulmadı |

Tur 1'in 4 kırmızısı (PARA-DEMO-2 ×3, SATIS-3 kararsız) bu koşumda yeşil. **Bu paketin yeni kırmızısı: 0.**

### T2.8 Koşul karşılama (tur 2 eki)

| Koşul | Durum | Kanıt |
|---|---|---|
| [KRİTİK] eksi adet 400 | KARŞILANDI | test + M4-2 + canlı |
| [KRİTİK] çok büyük adet 500 değil, 400 alan:adet | KARŞILANDI | test (2 kol) + M4-3/M4-4 + canlı |
| [KRİTİK] tırnaklı Türkçe biçimli sayı reddi (dokunulan uçlar) | KARŞILANDI (alan anahtarı "$.adet" genel sorunu açık — T2.4 gözlem) | 6 vaka + M4-6/M2-4/M2-5/M3-4 + canlı |
| [KRİTİK] AwayFromZero | KARŞILANDI | test + M4-5 + canlı 1.001 |
| [DÜŞÜK] ad/kod kırpma | KARŞILANDI | test + M5-2/3/4 + canlı |
| [DÜŞÜK] "" ile silme → Verildi DTO | AÇIK (sonraki paket — hasım önerisi) | § 12 |
| Build 0/0 | KARŞILANDI | temiz build 0 Uyarı 0 Hata |
| denetim (P-30 hariç) TEMİZ | KARŞILANDI | 303 ✓, yalnız P-30 ✗ |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 1 kırmızı PARA-DEMO-2'nin |
| Test kiracısı silindi | KARŞILANDI | 17 → 17 |

### T2.9 Açık kalanlar (tur 2 eki — § 12'ye ek)

- **Genel doğrulama gövdesi (orkestratör):** JSON tür hatasında `alanlar` anahtarı `"$.alan"` olarak dönüyor ve fazladan `"dto zorunludur"` iletisi ekleniyor (`Program.cs` InvalidModelStateResponseFactory + `Kuyumcu.Core/I18n/DogrulamaSozlugu.Anahtar`). Bu paketin dosya alanı dışında; ekranda alan altı mesajı eşleşmez (istemci sayı gönderdiği için kullanıcı yolu tetiklemez).
- **Genel `AllowReadingFromString` (orkestratör):** tırnaklı sayı yalnız bu paketin dokunduğu 4 alanda kapatıldı; tüm API için `JsonNumberHandling.Strict` genel kararı istemci etkisi incelenerek orkestratörce verilmeli.
- **Ürün OLUŞTURMA (POST) ad/kod kırpmıyor** (`UrunModulu.cs` Olustur ~436-437; çakışma denetimi de kırpılmamış kodla). PATCH dışı → bu paketin alanı değil; sonraki pakete.
- **Sayım oturumu ucu** (`SayimModulu` `SayilanAdet`) kendi DTO'sundan geçer; eksi/tavan/yuvarlama kapısı artık ortak servis `StokSayimGuncelle` içinde olduğu için o yolu da korur, ama o DTO'da Strict sayı kapısı yok (bu paket dokunmadı).
- Kanıt dosyalarının (ham/mut-yedek-PATCH, ham/PATCH-SOZLESME-olcum/tam-suit.trx 12 MB) silinmesi: kurucu/orkestratör kararı.


---

# TUR 1 RAPORU (korunmuştur)

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)

| Kaynak bulgu | Güncel ağaçta durum | Karar |
|---|---|---|
| (1) PATCH /subeler tek alanda 'Ad zorunludur' (SUBE-KAPSAM-B) | Önceki uygulayıcı ayrı `SubeGuncelleDto` yazmıştı (ağaçta) | KORUNDU; test + mutasyon + canlı eklendi |
| (2) Fatura taslak kalem has birikimi (Yuvarla6(eski×yeni/eski)) | **Zaten düzeltilmiş** (PAKET CARI-BAKIYE, `FaturaModulu.cs` TaslakKalemGuncelle: `BirimHasGrami` saklanır, toplam = birim × miktar) | Kod değişmedi. İstenen **1000 gidiş-dönüş** testi yazıldı; mutasyonla (orantılı hesaba dönüş) kırmızı kanıtlandı; canlıda 100 gidiş-dönüş API ile ölçüldü |
| (3) PATCH /kur/kaynak [SAHIP,MUDUR] | **Zaten düzeltilmiş** (KESIF-P6: `KurModulu.cs:1064-1065` `[RolGerekli("SAHIP")]`; Kur.razor seçici salt okunur + İşletme bağlantısı) | Kod değişmedi. Mutasyon (MUDUR'u geri ekle) → `KesifP6HttpTestleri.KESIFP6_Kur_kaynagi_MUDURE_403_ve_kiraci_ayari_DEGISMEZ` KIRMIZI; canlı MÜDÜR 403 / SAHİP 200 |
| Spesifikasyonun 'Birleşik koşullar' bölümü | TOPLU-TASARIM-KUYRUGU-2026-09-29.md'de bu başlık yok; bağlayıcı koşullar istemdeki EK KOŞULLAR'dan alındı | — |

## 2. Tarama tablosu — TÜM PATCH uçları (61 uç, 37 dosya)

Sözleşme ölçütleri: (a) DTO'da `[Required]`/elle 'zorunlu' kısmi gövdede tetikleniyor mu · (b) gönderilmeyen alan mevcut değeri ezer mi · (c) boş dize/null ayrımı · (d) yetki.
"Tek alanlı eylem" = ucun tek anlamı o alanı yazmak (rol değiştir, durum geçişi, kaynak seç); orada alan zorunluluğu PATCH sözleşmesini ihlal etmez (boş gövdede yapılacak iş yoktur → açık 400).

| Uç | dosya:satır | Yetki | Bulgu | Karar |
|---|---|---|---|---|
| PATCH /subeler/{id} | Sube/SubeModulu.cs:193 | SAHIP,MUDUR | ESKİ: OLUŞTURMA DTO'su → tek alanda 'Ad zorunludur' | **DÜZELTİLDİ** (SubeGuncelleDto, Verildi bayrakları; ad null/boş → 400 alan:ad; adres/telefon null = sil) |
| PATCH /fiyat/listeler/{id} | Fiyat/FiyatModulu.cs:959 | SAHIP,MUDUR | ESKİ: FiyatListesiDto (Ad [Required]) → {aktif:false} 400; gönderilmeyen tür/marj/çarpan PERAKENDE/%0/%100 ile **sessizce eziliyordu** | **DÜZELTİLDİ** (FiyatListesiGuncelleDto; birleşik değer AraligiDogrula'dan geçer; ad/tür/marj/çarpan null → 400 alan) |
| PATCH /fiyat/kampanyalar/{id} | Fiyat/FiyatModulu.cs:967 | SAHIP,MUDUR | ESKİ: KampanyaDto (Ad/Baslangic/Bitis [Required]); gönderilmeyen tarih 0001-01-01, indirim türü/değer varsayılanla **eziliyordu** | **DÜZELTİLDİ** (KampanyaGuncelleDto; birleşik kayıt KampanyaDogrula'dan geçer; silinemeyen alan null → 400 alan; HedefId null = sil) |
| PATCH /urunler/{id}/stok/{subeId} | Urun/UrunModulu.cs:1138 | SAHIP,MUDUR | ESKİ: `decimal Adet` → gövdede adet YOKSA **stok 0'a çekilip deftere sayım düzeltmesi yazılıyordu** | **DÜZELTİLDİ** (tek alanlı eylem: `decimal? Adet`, yoksa 400 alan:adet, stok/defter dokunulmaz) |
| PATCH /urunler/{id} | Urun/UrunModulu.cs:1132 | SAHIP,MUDUR,MUHASEBE | null=koru doğru; ESKİ: ad/kod boş dize → kolona "" (adsız ürün) | **DÜZELTİLDİ** (ad/kod boş → 400 alan) · not: null=koru (silme yok) zorunlu alanlar için doğru, isteğe bağlı alanlar "" ile temizlenir (P-10 kalıbı) — değişmedi |
| PATCH /urun-kategorileri/{id} | Urun/KategoriModulu.cs:535 | SAHIP,MUDUR,MUHASEBE | null=koru doğru; ESKİ: ad boş dize → "" | **DÜZELTİLDİ** (ad boş → 400 alan:ad) |
| PATCH /reyonlar/{id} | Urun/KategoriModulu.cs:560 | SAHIP,MUDUR | kısmi, ad<2 elle | uygun |
| PATCH /fiyat/listeler/{id}/urun-fiyatlari/{urunId} | Fiyat/FiyatModulu.cs:1056 | SAHIP,MUDUR | Gonderildi bayrakları, kısmi | uygun |
| PATCH /faturalar/{id} | Fatura/FaturaModulu.cs:1810 | SAHIP,MUDUR,MUHASEBE | JsonElement TryGetProperty; cari null → 400 | uygun |
| PATCH /faturalar/{id}/kalemler/{kalemId} | Fatura/FaturaModulu.cs:1825 | SAHIP,MUDUR,MUHASEBE | kısmi; has birikimi CARI-BAKIYE'de giderilmiş | uygun (1000 tur testi bu pakette) |
| PATCH /faturalar/{id}/durum | Fatura/FaturaModulu.cs:1830 | SAHIP,MUDUR,MUHASEBE | Durum [Required] | tek alanlı eylem — uygun |
| PATCH /kur/kaynak | Kur/KurModulu.cs:1064 | **SAHIP** | Kaynak [Required]; KESIF-P6'da yalnız SAHİP | tek alanlı eylem — uygun (rol kanıtı bu pakette) |
| PATCH /hurda/{id} | Hurda/HurdaModulu.cs:645 | SAHIP,MUDUR | JsonElement kısmi; VUK zorunlu unsur yalnız null gönderilince | uygun |
| PATCH /evraklar/{id} | Evrak/EvrakModulu.cs:1115 | SAHIP,MUDUR,MUHASEBE | Verildi bayrakları | uygun |
| PATCH /emanet/hareketler/{hareketId} | Emanet/EmanetModulu.cs:446 | 4 rol | AciklamaVerildi | uygun |
| PATCH /gider-kategorileri/{id} | Gider/GiderModulu.cs:496 | SAHIP,MUDUR,MUHASEBE | Verildi bayrakları | uygun |
| PATCH /sayim/{id} | Sayim/SayimModulu.cs:326 | 4 rol | JsonElement; ad null → 400 | uygun |
| PATCH /kurulum/durum | Kurulum/KurulumModulu.cs:373 | SAHIP | tek liste alanı | tek alanlı eylem — uygun |
| PATCH /telemetri/kiracilar/{id}/destek-dakikasi/{kayitId} | Telemetri/TelemetriModulu.cs:130 | SAHIP + sistem anahtarı | Verildi bayrakları | uygun |
| PATCH /siparisler/{id} | Siparis/SiparisModulu.cs:1026 | 4 rol | JsonElement; damga yoksa kilit atlanır | uygun |
| PATCH /vezne/fisler/{id} | Vezne/VezneModulu.cs:968 | 4 rol | JsonElement; silinemeyen alan null → 400 | uygun |
| PATCH /admin/tenant | Admin/AdminModulu.cs:419 | SAHIP (sınıf) | VarsayilanArayuzModu [Required] | tek alanlı eylem — uygun |
| PATCH /admin/tenant/kimlik | Admin/AdminModulu.cs:428 | SAHIP | Gonderildi bayrakları | uygun |
| PATCH /admin/tenant/maliyet-yontemi | Admin/AdminModulu.cs:436 | SAHIP | Yontem [Required] | tek alanlı eylem — uygun |
| PATCH /admin/plan | Admin/AdminModulu.cs:451 | SAHIP (sınıf) | Plan [Required] | tek alanlı eylem — uygun |
| PATCH /admin/ai | Admin/AdminModulu.cs:460 | SAHIP (sınıf) | Etkin [Required] | tek alanlı eylem — uygun |
| PATCH /admin/vezne | Admin/AdminModulu.cs:469 | SAHIP (sınıf) | Zorunlu ?? 400 | tek alanlı eylem — uygun |
| PATCH /masak/saklama-bekletme/{id} | Masak/SaklamaYaptirimModulu.cs:189 | SAHIP | Verildi | uygun |
| PATCH /masak/yaptirim-eslesmeleri/{id} | Masak/SaklamaYaptirimModulu.cs:209 | SAHIP | Verildi | uygun |
| PATCH /masak/supheli/{id}/gonderildi | Masak/MasakModulu.cs:424 | SAHIP,MUDUR | GonderimReferansi [Required] | tek alanlı eylem — uygun |
| PATCH /masak/supheli/{id}/kapat | Masak/MasakModulu.cs:429 | SAHIP,MUDUR | Gerekce [Required, ≥20] | tek alanlı eylem (gerekçeli kapatma) — uygun |
| PATCH /pano/ayarlar/{kod} | Pano/PanoModulu.cs:365 | SAHIP,MUDUR | Verildi | uygun |
| PATCH /isler/ayarlar | Isler/IslerModulu.cs:275 | SAHIP,MUDUR (sınıf) | Verildi | uygun |
| PATCH /satinalma/{id} | Satinalma/SatinalmaModulu.cs:731 | SAHIP,MUDUR | JsonElement; tedarikçi null → 400 | uygun |
| PATCH /irsaliyeler/{id} | Irsaliye/IrsaliyeModulu.cs:439 | SAHIP,MUDUR | JsonElement; değişen alan yoksa açık 400 ("düzenlenebilir alan gönderilmedi") | uygun (boş gövde 400 — eylemsiz gövde, sessiz değil) |
| PATCH /sistem/davetler/{id} | Sistem/SistemModulu.cs:521 | sistem anahtarı | Gerekce [Required] (operatör gerekçesi, varlık alanı değil); alanlar Gonderildi | uygun |
| PATCH /sistem/kiracilar/{id} | Sistem/SistemModulu.cs:530 | sistem anahtarı | aynı | uygun |
| PATCH /kullanicilar/ben | Kullanici/KullaniciModulu.cs:599 | oturum sahibi | null=koru | uygun |
| PATCH /kullanicilar/ben/sifre | Kullanici/KullaniciModulu.cs:604 | oturum sahibi | YeniSifre [Required] | tek alanlı eylem — uygun |
| PATCH /kullanicilar/{id} | Kullanici/KullaniciModulu.cs:614 | SAHIP,MUDUR | null=koru | uygun |
| PATCH /kullanicilar/{id}/rol | Kullanici/KullaniciModulu.cs:620 | SAHIP,MUDUR | Rol [Required] | tek alanlı eylem — uygun |
| PATCH /kullanicilar/{id}/sifre | Kullanici/KullaniciModulu.cs:626 | SAHIP,MUDUR | [Required] | tek alanlı eylem — uygun |
| PATCH /kullanicilar/{id}/aktif | Kullanici/KullaniciModulu.cs:640 | SAHIP,MUDUR | Aktif [Required] | tek alanlı eylem — uygun |
| PATCH /kullanicilar/ben/arayuz-modu | Kullanici/KullaniciModulu.cs:686 | HerkeseAcik (kişisel) | Gonderildi | uygun |
| PATCH /pos/cihazlar/{id} | Pos/PosModulu.cs:1246 | SAHIP,MUDUR | null=koru + açık *Temizle bayrağı | uygun |
| PATCH /pos/cihazlar/{id}/taksit-oranlari/{oranId} | Pos/PosModulu.cs:1276 | SAHIP,MUDUR | aynı | uygun |
| PATCH /banka/ekstre/satir/{id} | Banka/EkstreModulu.cs:757 | SAHIP,MUDUR,MUHASEBE (sınıf) | boş gövde → açık 400 ("değiştirilecek alan yok") | uygun (eylem ucu) |
| PATCH /iceaktarim/{id} | Iceaktarim/IceAktarimModulu.cs:932 | SAHIP (sınıf) | Verildi | uygun |
| PATCH /banka/{id} | Banka/BankaModulu.cs:801 | SAHIP,MUDUR | null=koru | uygun |
| PATCH /banka/{id}/hareketler/{hareketId} | Banka/BankaModulu.cs:832 | SAHIP,MUDUR,MUHASEBE | Verildi | uygun |
| PATCH /abonelik/plan | Abonelik/AbonelikModulu.cs:71 | SAHIP | Plan [Required] | tek alanlı eylem — uygun |
| PATCH /tercihler/benim | Tercih/TercihModulu.cs:257 | HerkeseAcik (kişisel) | sözlük, kısmi | uygun |
| PATCH /yedek/talep/{id} | Yedek/YedekModulu.cs:646 | SAHIP (sınıf) | Gonderildi | uygun |
| PATCH /yedek/talep/{id}/gerekce | Yedek/YedekModulu.cs:651 | SAHIP (sınıf) | Gonderildi + MinLength | uygun |
| PATCH /gorunum (kiracı) | Gorunum/GorunumModulu.cs:328 | SAHIP | Verildi | uygun |
| PATCH /gorunum/kullanici | Gorunum/KullaniciGorunumModulu.cs:223 | HerkeseAcik (kişisel) | Verildi | uygun |
| PATCH /kasalar/{id} | Kasa/KasaModulu.cs:785 | SAHIP,MUDUR | — | **SONRAKİ PAKET** (PARA-DEMO-2 alanı, dokunulmadı) |
| PATCH /kasalar/{id}/hareket/{hareketId} | Kasa/KasaModulu.cs:815 | SAHIP,MUDUR,MUHASEBE | — | **SONRAKİ PAKET** (PARA-DEMO-2) |
| PATCH /cari/{id} | Cari/CariModulu.cs:1800 | SAHIP,MUDUR,MUHASEBE | — | **SONRAKİ PAKET** (PARA-DEMO-2) |
| PATCH /cari/{id}/pep | Cari/CariModulu.cs:1809 | SAHIP,MUDUR,MUHASEBE | — | **SONRAKİ PAKET** (PARA-DEMO-2) |
| PATCH /satis/rezervler/{id} | Satis/SatisModulu.P21.cs:1226 | SAHIP,MUDUR,TEZGAHTAR | — | **SONRAKİ PAKET** (PARA-DEMO-2) |

Virman ve Cikti modüllerinde PATCH ucu yok (tarandı: 0 eşleşme). Ham çıktılar: `ham/PATCH-SOZLESME-olcum/patch-uclari-ham.txt`, `patch-dtolari-ham.txt`.

## 3. Yapılan değişiklikler

- `Kuyumcu.Api/Modules/Sube/SubeModulu.cs` — (önceki uygulayıcı, korunarak) `SubeGuncelleDto` + servis ad kapısı.
- `Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` — `FiyatListesiGuncelleDto`, `KampanyaGuncelleDto` (Verildi bayrakları); `ListeGuncelle` kısmi + tam gövdeli iç yük (`FiyatListesiGuncelleDto.Tam`, mevcut testler/çağrılar kırılmaz); `KampanyaGuncelle` birleşik kayıt + `Silinemez` yardımcısı; controller bağları kısmi DTO'ya.
- `Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — `StokSayimDto.Adet` → `decimal?` + controller'da 400 alan:adet; `Guncelle` ad/kod boş dize kapısı.
- `Kuyumcu.Api/Modules/Urun/KategoriModulu.cs` — kategori adı boş dize kapısı.
- `Kuyumcu.Tests/PatchSozlesmeTestleri.cs` — YENİ, 17 test.

İstemci uyumu: `Fiyatlandirma.razor` liste/kampanya için TAM gövde gönderiyor (`ListeGovde`, `KampanyaGovde`, `ListeDurumDegistir`, `KampanyaDurumDegistir`); `Urunler.razor:2207` stok için `{adet}` gönderiyor; `Isletme.razor:1172/1190` şube için tam gövde/`{ad,aktif}`. Kısmi DTO tam gövdeyi aynı sonuçla işler — `PATCH_fiyat_listesi_istemcinin_TAM_govdesi_hala_calisir` istemci gövdesini birebir sınar. İstemci değişmedi.

Migration: gerekmedi (şema değişikliği yok). Sozlukler: yeni ekran metni yok (sunucu 400 iletileri mevcut kalıpla Türkçe istisna metni).

## 4. DoD sözleşmesi

Yeni varlık/kayıt türü DOĞMADI. Düzeltilen her uç mevcut varlığın düzenleme yoludur: Şube (PATCH + DELETE=pasif, ekranda Düzenle/Aktife al — Isletme.razor), Fiyat listesi ve Kampanya (PATCH + aktif/pasif — Fiyatlandirma.razor), Ürün/Kategori (PATCH + DELETE pasif), Stok sayımı (deftere HareketFabrikasi yoluyla yazan eylem; değişmedi). Finansal varlıklara (hareketler/kasa/banka/fatura) hard DELETE eklenmedi. Denetim kaydı çağrıları değişmedi.

## 5. Testler

Yeni: 17 (`PatchSozlesmeHttpTestleri` 16 · `PatchSozlesmeFaturaTestleri` 1).
- Filtreli `FullyQualifiedName~PatchSozlesme`: 17/17 geçti.
- İlgili mevcut kümeler (Fiyat|Sube|Kategori|KesifP6|Kampanya|Stok|Sayim|CariBakiye|UrunHttp|Reyon): 1043/1043 geçti.
- Tam süit: § 8.

## 6. Mutasyon tablosu (her biri: yedek → tek eşleşme → filtreli test → geri al → cmp)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | Şube: `if (dto.AdVerildi)` → `if (true)` (ad her PATCH'te zorunlu) | PATCH_sube_bos_govde_200…, PATCH_sube_yetki…, PATCH_sube_tek_alan_telefon…, PATCH_sube_null_adres… (4) | AYNI |
| M1-2 | Şube: boş ad 400 kapısı silindi | PATCH_sube_null_adres_SILER_null_ad_400_alan_ad | AYNI |
| M1-3 | Şube PATCH rolüne MUHASEBE+TEZGAHTAR eklendi | PATCH_sube_yetki_TEZGAHTAR_ve_MUHASEBE_403_kayit_DEGISMEZ | AYNI |
| M2-1 | Liste: `tur = liste.Tur` → `"PERAKENDE"` (gönderilmeyen ezilir) | PATCH_fiyat_listesi_yalniz_aktif…, PATCH_fiyat_listesi_bos_govde… | AYNI |
| M2-2 | Liste: marj null kapısı kapatıldı | PATCH_fiyat_listesi_bos_govde_200_DEGISMEZ_ve_null_ad_marj_400 | AYNI |
| M2-3 | Liste: boş ad kapısı silindi | PATCH_fiyat_listesi_bos_govde_200_DEGISMEZ_ve_null_ad_marj_400 | AYNI |
| M3-1 | Kampanya: gönderilmeyen değer `1m` ile ezilir | PATCH_kampanya_yalniz_aktif…, PATCH_kampanya_bos_govde…, PATCH_kampanya_tek_alan_birlesik… (3) | AYNI |
| M3-2 | Kampanya: birleşik kayıt doğrulaması (KampanyaDogrula) kaldırıldı | PATCH_kampanya_tek_alan_birlesik_kayitla_DOGRULANIR | AYNI (ilk koşum başka şeridin Toplu4Testleri.cs derleme hatasına çarptı; yeniden koşuldu) |
| M3-3 | Kampanya: bitiş null → sessizce korunur | PATCH_kampanya_bos_govde_200_DEGISMEZ_null_tarih_deger_400 | AYNI |
| M4-1 | Stok: adet yoksa `0m` (eski davranış) | PATCH_stok_bos_govde_400_stok_SIFIRLANMAZ_deftere_YAZILMAZ | AYNI |
| M5-1 | Ürün: boş ad kapısı silindi | PATCH_urun_tek_alan_aciklama_ad_kod_AYNI_bos_ad_kod_400 | AYNI |
| M6-1 | Kategori: boş ad kapısı silindi | PATCH_kategori_tek_alan_sira_ad_AYNI_bos_ad_400_yetki_TEZGAHTAR_403 | AYNI |
| M7-1 | Fatura kalem: has orantılı yeniden hesaba döndü | PatchSozlesmeFaturaTestleri.Fatura_taslak_kalem_1000_gidis_donus_PATCH_has_toplami_BIREBIR, CariBakiyeTestleri.Fatura_taslak_miktar_gidis_donus_has_kaymaz | AYNI |
| M8-1 | Kur kaynağı rolüne MUDUR eklendi | KesifP6HttpTestleri.KESIFP6_Kur_kaynagi_MUDURE_403_ve_kiraci_ayari_DEGISMEZ | AYNI |

14/14 mutasyon kırmızı, 14/14 geri alma cmp AYNI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata. Ham: `ham/PATCH-SOZLESME-olcum/mutasyon-sonuc.txt`.

## 7. Canlı doğrulama (API 5188, bağımsız SQL — kuyumcu_net)

Kiracı daveti sistem anahtarıyla (anahtar dosyası `betikler/sistem.anahtar` boştu; süreç için /private/tmp altında chmod 600 rastgele anahtar üretilip yalnız API ortam değişkenine verildi, ekrana basılmadı).

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| PATCH /subeler {telefon} | 200, ad/adres aynı | 200 | önce `Merkez Şube ∅ ∅ 1` → sonra `Merkez Şube ∅ 02125551111 1` |
| PATCH /subeler {} | 200 | 200 | değişmedi |
| PATCH /subeler {ad:null} | 400 alan:ad | 400 `alanlar.ad` | — |
| PATCH /subeler MÜDÜR | 200 | 200 | — |
| PATCH /fiyat/listeler {aktif:false} | 200, ad/tür/marj/çarpan aynı | 200 | önce `Toptanci TOPTAN 2.500 80.00 0 1` → sonra `Toptanci TOPTAN 2.500 80.00 0 0` |
| PATCH /fiyat/listeler {} | 200 | 200 | değişmedi |
| PATCH /fiyat/listeler {hasMarjYuzde:null} | 400 alan | 400 `alanlar.hasMarjYuzde` | — |
| PATCH /fiyat/listeler jetonsuz | 401 | 401 | — |
| PATCH /fiyat/kampanyalar {aktif:false} | 200, diğerleri aynı | 200 | önce `Bayram 2026-10-02 2026-11-01 TUMU TUTAR_YUZDE 7.50 1` → sonra `… 7.50 0` |
| PATCH /fiyat/kampanyalar {} | 200 | 200 | — |
| PATCH /fiyat/kampanyalar {bitis:null} | 400 alan | 400 `alanlar.bitis` | — |
| PATCH /fiyat/kampanyalar {bitis:2026-09-01} (başlangıçtan önce) | 400 | 400 "Kampanya bitişi başlangıçtan önce olamaz" | `2026-10-02 2026-11-01` değişmedi |
| PATCH stok {adet:7} | 200 | 200 (ilk denemede yeni kiracıda kur yoktu → 400 "Sayım kaydı için en az bir kur girişi gerekir"; kur çekildikten sonra 200) | `adet 7.000` |
| PATCH stok {} | 400 alan:adet, stok 7 kalır | 400 `alanlar.adet` | `7.000` |
| PATCH /urunler {aciklama} / {ad:"  "} | 200 / 400 | 200 / 400 `alanlar.ad` | `Bilezik PATCH PS-1 Vitrin 3` |
| PATCH /urun-kategorileri {sira:3} / {ad:" "} | 200 / 400 | 200 / 400 `alanlar.ad` | `Bilezikler 3 1` |
| PATCH /kur/kaynak MÜDÜR | 403 | 403 "gerekli rol: dükkân sahibi" | `kur_kaynagi NULL` (değişmedi) |
| PATCH /kur/kaynak SAHİP | 200 | 200 `{"secili":"ALTINKAYNAK","uyari":null}` | `ALTINKAYNAK` |
| Fatura taslak kalem 100 gidiş-dönüş (1,001 ↔ 3) API | 200×200, has birebir | 200 olmayan tur 0 | kalem önce `27.480000 / birim 9.160000` → sonra `27.480000 / 9.160000 / 3.000`; başlık `27.480000` → `27.480000` |

Not: canlı ürünün birim has'ı (9,16) yuvarlak olduğu için canlı tur birikim kaymasını tek başına göstermez; kayma kanıtı birim has 1,234567 ile 1000 turluk test + M7-1 mutasyonudur.
Ham: `ham/PATCH-SOZLESME-olcum/canli-1.txt`, `canli-2.txt`, `canli-3-fatura.txt`.
API (PID 77524/77529) durduruldu: `lsof -iTCP:5188 -sTCP:LISTEN` boş.
Test kiracıları: 3 açıldı (ilk iki koşum betik çıktı hatasında yarıda kaldı) — 01M3XVF6TECAJ7B5X90J9W9YK9, 01M3XVFEPAXHBBZWTKB770V5VP, 01M3XVFNWEHGQZSF4N1JDRMT9N; üçü `zsh kiraci-temizle.sh <id> --uygula` ile silindi. tenants: önce 16 → sonra 16; `PAKETPATCH%` 0.

## 8. Tam süit ve denetim

(Bkz. aşağıdaki "Tam süit sonucu" bölümü — koşum sonunda eklendi.)

denetim.sh (bash): rc=1, tek ihlal `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ tutulan, bu paketle ilgisiz); diğer tüm sayaçlar ✓. Ham: `ham/PATCH-SOZLESME-olcum/denetim-ozet.txt`.

## 9. Ekran / gerçek tarayıcı kuralı

Bu pakette hiçbir razor/css/bileşen/sözlük dosyası değişmedi (`git status` § 10). Değişiklik yalnız sunucu davranışıdır ve istemcinin gönderdiği tam gövdeler birebir aynı sonucu üretir (HTTP testiyle kanıtlı). Bu nedenle tarayıcı ölçümü kapsam dışıdır; ekran dosyası değiştirme gereği doğmadı (paralel TOPLU-4 alanına girilmedi).

## 10. Değişen dosyalar (bu paket)

```
 M dotnet/Kuyumcu.Api/Modules/Sube/SubeModulu.cs        (önceki uygulayıcı; korundu)
 M dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs
 M dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs
 M dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs
?? dotnet/Kuyumcu.Tests/PatchSozlesmeTestleri.cs
?? dokumanlar/09-uygulama-kayitlari/ham/PATCH-SOZLESME-olcum/   (kanıt)
   dokumanlar/09-uygulama-kayitlari/ham/mut-yedek-PATCH/*.M*    (mutasyon yedekleri)
```
(Depo ilk commit'ten beri tüm ağaç 'M/??' görünüyor; liste bu paketin dokunduğu dosyalardır.)

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tarama tablosu (tüm uçlar) | KARŞILANDI | § 2 (61 uç) |
| Düzeltilen her uçta tek-alan PATCH + SQL'de diğerleri aynı | KARŞILANDI | 6 uç × test + canlı SQL |
| Boş gövde testi | KARŞILANDI | şube/liste/kampanya/ürün/kategori 200 değişmez · stok 400 (eylem) |
| Yetki testi | KARŞILANDI | her düzeltilen uçta TEZGAHTAR (ve ilgiliyse MUHASEBE) 403 |
| Canlı API + bağımsız SQL (5188) | KARŞILANDI | § 7 |
| Mutasyon (her kural ≥1 kırmızı, cmp AYNI) | KARŞILANDI | § 6 (14/14) |
| Fatura 1000 gidiş-dönüş 0 birikim | KARŞILANDI | Fatura_taslak_kalem_1000_gidis_donus_PATCH_has_toplami_BIREBIR + M7-1 |
| Kur kaynağı MÜDÜR 403 / SAHİP 200 | KARŞILANDI | KesifP6 testi + M8-1 + canlı |
| Build 0/0 | KARŞILANDI | temiz build 0 uyarı 0 hata |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | yalnız P-30 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9210 test, 4 kırmızı başka şeritlerin (PARA-DEMO-2 ×3, SATIS-3 kararsız ×1) |
| Kasa/Cari/Satis/Virman/Cikti'ye dokunulmadı | KARŞILANDI | yalnız tabloda "sonraki paket" |
| Test kiracısı silindi, tenants başlangıç | KARŞILANDI | 16 → 16 |

## 12. Açık kalanlar

- Kasa (2 uç), Cari (2 uç), Satış rezerv (1 uç) PATCH taraması → PARA-DEMO-2 sonrası paket (bu turda yalnız listelendi, incelenmedi).
- Ürün PATCH'inde isteğe bağlı metin alanlarının silinmesi "" ile (P-10 kalıbı); null=koru. Sözleşmenin "null = sil" yorumuyla tam örtüşmez; istemci "" gönderdiği için değiştirilmedi — ileride Verildi bayraklı DTO'ya geçiş önerilir (istemci etkisi incelenmeli).
- Kategori `ustId`: "" = köke taşı, null = koru (aynı not).
- `betikler/sistem.anahtar` dosyası 0 bayt — canlı doğrulama betikleri için davet üretimi bu dosyaya dayanıyorsa çalışmaz (orkestratöre not).
- Başka şeritlere ait derleme uyarıları (yalnız test projesi tam derlemesinde görüldü): `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(455)` xUnit2029; mutasyon sırasında TOPLU-4'ün `Toplu4Testleri.cs(515)` geçici derleme hatası (sonradan düzeldi).
- Canlı doğrulamada 3 sistem daveti (kiracı dışı tablo) tüketildi; kiracı silindiği için yetim bilgi satırı olarak kalır.

## 13. Kurucu / mali müşavir

Mevzuat hükmü yazılmadı/değiştirilmedi. Kampanya süresi tavanı (FEY md.11/3) mevcut koddaki kuralla aynen uygulanır (birleşik kayıtta da).

## Tam süit sonucu (§ 8 eki)

`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-SOZLESME` (KUYUMCU_TEST_DB=kuyumcu_test_t9, son kodla, tek koşum):
**Toplam 9210 · Geçti 9204 · Başarısız 4 · Atlanan 0.** trx kopyası: `ham/PATCH-SOZLESME-olcum/tam-suit.trx`.

| Kırmızı | Sahibi | Tek başına tekrar | Bu paketle ilişki |
|---|---|---|---|
| ParaDemo2Testleri.PD2_5_elle_DIGER_girisi_acilis_devri_DEGIL_diger_kaleminde_kalir | PARA-DEMO-2 (t2; VirmanModulu.cs 11:35, ParaDemo2Testleri.cs 11:26 değişiyor — yarım iş) | KIRMIZI (Expected 250, Actual 0) | yok — Kasa/Virman/gün sonu; bu paket o dosyalara dokunmadı |
| ParaDemo2Testleri.PD2_5_gun_sonu_toplam_tahsilat_virman_ve_acilis_devrini_DISARIDA_birakir | PARA-DEMO-2 | YEŞİL (yük altında kararsız) | yok |
| ParaDemoTur2Testleri.PD6_kart_payini_ASAN_virman_400_hicbir_sey_yazilmaz | PARA-DEMO-2 (Virman mesajı) | KIRMIZI (Assert.Contains alt dize) | yok |
| SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE_ile_TEK_istek_ve_liste_gorunur | SATIS-3 / kuyrukta bilinen yük-altı kararsızlık sınıfı | YEŞİL | yok |

Bu paketin yeni kırmızısı: **0** (dört kırmızının hiçbiri Şube/Fiyat/Ürün/Kategori/Fatura/Kur yoluna değmiyor; PatchSozlesme 17/17 ve ilgili 1043 test yeşil).
