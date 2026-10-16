# PAKET PATCH-2 — uygulama raporu (02.10, dalga PATCH-2 · t9 · 5188)

Rol: Taşkın (backend). Kaynak: `PAKET-PATCH-SOZLESME-uygulama-raporu.md` kapanış hasımının 2 KRİTİK + 1 DÜŞÜK bulgusu ve istemdeki tarama genişletmesi.
Kanıt kopyaları: `ham/PATCH-2-olcum/` (canlı betik + çıktı, mutasyon sürücüsü + her mutasyonun günlüğü `mut-log/`, denetim, tam süit özeti, tarama betikleri ve çıktısı). Mutasyon yedekleri: `/private/tmp/kuyumcu-serit/PATCH-2/mut-yedek/`. Proje ağacına TRX/yedek konmadı.

## 1. Spesifikasyon ↔ kod doğrulaması

| İddia (hasım) | Kodda durum | Sonuç |
|---|---|---|
| `UrunGuncelleDto` sayı üyelerinde Strict yok, web varsayılanı `AllowReadingFromString` | Doğru: yalnız `StokSayimDto`, `FiyatListesiGuncelleDto`, `KampanyaGuncelleDto` üyelerinde vardı. `Program.cs` JSON ayarı yoktu (AddControllers web varsayılanı) | Düzeltildi (§ 2) |
| `FaturaModulu.TaslakKalemGuncelle`: `miktar <= 0` yuvarlamadan önce | Doğru (satır 1338-1352) | Düzeltildi |
| JSON tür hatası 400 gövdesi `"$.alan"` + `"dto zorunludur"` | Doğru: `InvalidModelStateResponseFactory` anahtarı yalnız camelCase yapıyordu; gövde parametresi için çerçevenin yapay "required" hatası da listeleniyordu | Düzeltildi (yalnız `Program.cs` fabrikası; `Kuyumcu.Core` DogrulamaSozlugu değişmedi) |
| Hasımın kalıcı testleri `Sinir3_urun_PATCH_Turkce_dize_400_SQL_DEGISMEZ` ve `Sinir3_fatura_kalem_miktar_0_0004_…` KIRMIZI | Değişiklik öncesi kırmızıydı | İkisi de YEŞİL (161/161 ilgili kümede, sonra tam süitte) |

Sapma (gerekçeli): istem "UrunGuncelleDto'nun tüm decimal üyelerine öznitelik" diyor. Öznitelik **sınıf düzeyinde** konuldu (`[JsonNumberHandling(Strict)]` sınıfın tüm sayı üyelerine uygulanır): 11 üyenin hepsini kapsar ve ileride eklenen üyeyi de unutmaz. Kapsam yansıtmayla test edildi (`P2_UrunGuncelleDto_11_sayi_uyesinin_hepsi_kapsamda`). Aynısı `UrunOlusturDto` için de yapıldı (UX-S26: iki form aynı sözleşme).

## 2. Tırnaklı sayı — tek noktalı çözüm ve gerekçe

**Seçilen çözüm:** `Program.cs` MVC zincirine `.AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict)` + ikinci hat olarak ürün DTO'larının sınıf özniteliği + `KolonTavani.JsonListe` (JsonElement'ten kalem dizisi çözen Vezne/Satın alma PATCH yolları) için ayrı Strict ayar.

**Neden genel ayar (tek nokta):**
- Tarama: `[FromBody]` ile bağlanan ve sayı üyesi olan **71 DTO** var (`ham/PATCH-2-olcum/dto-tarama.txt`); öncesinde yalnız **3**'ünde Strict vardı. DTO başına öznitelik 68 dosya/sınıf değişikliği demekti, Kasa/Cari/Satış/Çıktı (PARA-DEMO alanı) DTO'larına dokunmayı gerektirirdi ve yeni DTO'da unutulurdu. Genel ayar bu modüllerin DOSYALARINA dokunmadan onları da korur (canlı: Cari veresiye `{"tlTutari":"1.500"}` → 400 alan:tlTutari).
- **İstemciyi kırmaz — kanıt:** `Kuyumcu.Web/Servisler/ApiIstemci.cs` gövdeyi `JsonContent.Create(govde, JsonSerializerDefaults.Web)` ile yazar; Web varsayılanının `AllowReadingFromString` bayrağı yalnız OKUMAYI etkiler, yazma decimal'i sayı olarak yazar. Betik `istemci_tara2.py`: Web projesindeki **213 yazma çağrısının** (Post/Patch/Put/PostJetonla/PostBayt) gövde argümanlarında `ToString(`/`$"…"`/`string.Format`/`Bicim.` arandı → 5 aday, beşi de tarih (`Bicim.Makine(DateOnly/DateTime)`). Betik `ad_esle.py`: istemcide sunucu sayı alanı adıyla aynı adı taşıyan `string` özellikler (63) tek tek bakıldı — form durum sınıflarıdır (Vezne `FormKalemi.Adet` vb.), gövdeye `Sayi(...)`/`FormAlani.Ondalik(...)` ile decimal'e çevrilerek konur. Tam süit (9480 test; HTTP + bUnit) yeşil.
- Yazma (yanıt serileştirme) davranışı değişmez; Strict yalnız okumada dize sayıyı reddeder.
- Dış (üçüncü taraf) çağrıcı riski: `[AllowAnonymous]` yazma uçları Abonelik/Telemetri/Sistem (sistem anahtarlı operatör uçları) ve Meta — hiçbiri üçüncü taraf webhook değil.

**JsonElement gövdeli uçlar (7 dosya):** Fatura/Sipariş `KolonTavani.JsonSayi`/`Sayi` zaten `ValueKind != Number` → 400 veriyordu (dize sessizce okunmuyordu); bu pakette Fatura `Sayi`, `SayiYaDaNull` ve başlık toplamı hatasına `AlanIle(...)` eklendi (öncesinde alansız 400 idi — canlıda görüldü, düzeltildi). Vezne/Satın alma kalem dizisi `JsonListe` → Strict.

### Tarama tablosu — tırnaklı sayı ("1.500")

| Uç ailesi | Bağlama | Önce | Şimdi | Kanıt |
|---|---|---|---|---|
| PATCH /urunler/{id} (11 sayı alanı) | DTO | **200, 1,50 yazılıyordu** | 400 alan:<ad>, SQL aynı | test ×33 vaka + canlı 22/22 |
| POST /urunler | DTO | sessiz okuma | 400 alan:satisFiyati, ürün açılmaz | test + canlı |
| POST /faturalar (toplam, kalem miktar/fiyat) | DTO | sessiz okuma | 400 alan:toplam / kalemler[0].… | test + canlı |
| POST /faturalar/{id}/kalemler | DTO | sessiz okuma | 400 alan:miktar | test + canlı |
| PATCH /faturalar/{id}/kalemler/{k}, PATCH /faturalar/{id} | JsonElement `Sayi` | 400 ama ALANSIZ | 400 alan:miktar/birimFiyat/iscilikTl/toplam | test ×3 + canlı |
| PATCH /vezne/fisler/{id} kalemler | JsonElement `JsonListe` | **sessiz okuma** | 400 alan:kalemler | test + canlı |
| PATCH /satinalma/{id} kalemler | JsonElement `JsonListe` | **sessiz okuma** | 400 alan:kalemler (aynı fonksiyon) | M1-4 ile aynı kapı |
| POST /irsaliyeler kalemler | DTO | sessiz okuma | 400 alan:kalemler[0].adet | test + canlı |
| PATCH /siparisler/{id} | JsonElement `JsonSayi` | zaten 400 (Number değil) | aynı | — |
| Kasa / Cari / Satış / Çıktı / Virman DTO'ları (PARA-DEMO alanı) | DTO | sessiz okuma | 400 (genel ayar; dosyalarına DOKUNULMADI) | canlı Cari veresiye 400 alan:tlTutari |
| PATCH /satis/rezervler/{id} adet (`SatisModulu.P21.cs:367`) | JsonElement `GetDecimal()` | dize → InvalidOperationException → **500** (sessiz okuma yok) | DEĞİŞMEDİ — Satış alanı | açık kalan (§ 12) |
| Diğer 60+ DTO (Fiyat, Pos, Yerleşim, Evrak, Hurda, Gider, Emanet, Banka, Telemetri, Sayım, Pano, Masak, Kur, İşler, Görünüm, Sistem) | DTO | sessiz okuma | 400 (genel ayar) | tam süit yeşil |

### Tarama tablosu — "önce yuvarla, sonra ≤ 0" (miktar/adet → decimal(12,3))

| Yol | Önce | Şimdi | Kanıt |
|---|---|---|---|
| Fatura taslak kalem PATCH (`TaslakKalemGuncelle`) | **0,0004 → 0,000 yazılıyordu** (hasım KRİTİK) | önce `Math.Round(…,3,AwayFromZero)`, sonra ret; 400 alan:miktar | test ×3 + hasım testi + canlı |
| Fatura OLUŞTURMA (`KalemYolu`) | kontrol ham değerle | yuvarlanmış değerle; 400 alan:kalemler (controller yolunda DTO Range(0.001) önce yakalar → alan `kalemler[0].miktar`) | servis testi + canlı |
| Fatura `TaslakKalemEkle` | kontrol ham değerle | yuvarlanmış değerle; 400 alan:miktar | servis testi + canlı |
| Fatura fişten kesim `FistenKalemFiyati` | miktar ≤0 → 1 (miktar defterden türetilir, kullanıcı girdisi değil) | DEĞİŞMEDİ | — |
| Vezne `KalemleriKur` (POST+PATCH) | ham adet, MySQL 0,000 yazardı | yuvarla→kontrol; 400 alan:kalemler | test + canlı |
| Sipariş `KalemKur` (POST+PATCH) | ham adet | yuvarla→kontrol; 400 alan:adet | test + canlı |
| Satın alma `KalemKur` (POST+PATCH) | ham adet | yuvarla→kontrol; 400 alan:kalemler | test + canlı |
| Satın alma mal kabul | ham adet | yuvarla→kontrol; 400 alan:kalemler | test |
| İrsaliye oluşturma | ham adet | yuvarla→kontrol; 400 alan:kalemler | test + canlı |
| Stok sayım PATCH | tur 2'de zaten yuvarla→kontrol (`<0`) | — | — |
| Satış `AlisOlustur` (`SatisModulu.cs:954` `k.Adet <= 0m`), Rezerv oluştur (`P21.cs:212`) | ham adet kontrolü | DEĞİŞMEDİ — Satış alanı (PARA-DEMO şeridi) | açık kalan (§ 12) |
| Satış rezerv PATCH (`P21.cs:367`) | `Yuvarla3` sonra `<= 0` — doğru sıra | — | — |

## 3. JSON tür hatası 400 gövdesi (DÜŞÜK)

`Program.cs` `InvalidModelStateResponseFactory`:
- `$` ile başlayan ModelState anahtarı `Program.JsonYoluAlani` ile temizlenir: `$.satisFiyati` → `satisFiyati`, `$.kalemler[0].adet` → `kalemler[0].adet`, `$` → `govde`.
- JSON hatası varken gövde parametresinin (BindingSource.Body) yapay "dto zorunludur" hatası düşürülür.
- İleti `Program.JsonHataMesaji`: sayı türüne çevrilemeyen değer → "<Alan adı> geçerli bir sayı değil: tırnaksız sayı gönderin (ondalık ayırıcı nokta) ve kaydedilebilir büyüklükte olsun"; sayı dışı tür → "… beklenen türde değil"; bozuk JSON → "… okunamadı". Alan adı mevcut `DogrulamaSozlugu.AlanAdi` sözlüğünden (Core'a dokunulmadı).
- Canlı: `{"gramAgirlik":1e30}` → 400 `alanlar=[gramAgirlik]` (önce `{"dto":…,"$.gramAgirlik":…}`).

## 4. Yapılan değişiklikler (dosya alanı içinde)

- `Kuyumcu.Api/Program.cs` — genel Strict; doğrulama yanıt fabrikası; `Program.JsonYoluAlani`, `JsonHataMesaji`.
- `Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — `UrunGuncelleDto`, `UrunOlusturDto` sınıf düzeyi Strict.
- `Kuyumcu.Api/Modules/KolonTavani.cs` — `JsonListe` Strict ayarı.
- `Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` — 3 kalem yolunda yuvarla→kontrol; `Sayi`/`SayiYaDaNull`/başlık toplamı hatalarına alan.
- `Kuyumcu.Api/Modules/Vezne/VezneModulu.cs`, `Siparis/SiparisModulu.cs`, `Satinalma/SatinalmaModulu.cs` (×2), `Irsaliye/IrsaliyeModulu.cs` — yuvarla→kontrol + alan.
- `Kuyumcu.Tests/PatchIkiTestleri.cs` — YENİ.
Migration: **gerekmedi** (şema değişmedi). Ekran/razor/css/sözlük dosyası: değişmedi. Kasa/Cari/Satış/Virman/Çıktı dosyalarına dokunulmadı.

## 5. DoD sözleşmesi
Yeni varlık/kayıt türü doğmadı; uç, rol ([RolGerekli]/[ModulYetkisi]) ve denetim kaydı çağrıları değişmedi; finansal varlıklara silme eklenmedi. Yalnız mevcut oluşturma/düzenleme yollarının giriş kapıları sıkılaştırıldı (400 + alan).

## 6. Testler

`Kuyumcu.Tests/PatchIkiTestleri.cs`: 24 test metodu / **62 vaka** (sınıflar `PatchIkiHttpTestleri`, `PatchIkiFaturaServisTestleri`, `PatchIkiSafTestleri`). Her HTTP vakası: 400 + `alanlar.<ad>` (gövde AYRIŞTIRILARAK; `$` önekli anahtar ve `dto` YOK) ve veritabanında satır önce/sonra birebir.
- Filtreli `PatchIki`: 62/62. Filtreli ilgili küme (PatchIki|PATCHSOZLESME|PatchSozlesme|ZOBELGESinir|P54Http|Siparis|Satinalma|Irsaliye|Vezne|Fatura): 771 vaka, ilk koşumda 1 kırmızı (`P2_satinalma…` — yanıtta alanlar yoktu; alan eklendikten sonra tek başına yeşil) → son kodla tam süitte yeşil.
- Hasım testleri `Sinir3_urun_PATCH_Turkce_dize_400_SQL_DEGISMEZ` (×3) ve `Sinir3_fatura_kalem_miktar_0_0004_yuvarlaninca_sifir_iscilikli_satirda_da_RET` YEŞİL.
- Test sayısı: önceki tam süit (PATCH-SOZLESME tur 3) 9377 → bu koşum 9480 (aradaki fark başka şeritlerin testlerini de içerir; bu paketin katkısı 62 vaka).

## 7. Mutasyon tablosu
Yöntem: kaynak → `/private/tmp/kuyumcu-serit/PATCH-2/mut-yedek/<dosya>.orijinal-<kod>` → TEK eşleşme (eşleşme ≠ 1 ise durur) → `dotnet build` → `dotnet test --filter "PatchIki|PATCHSOZLESMESinirTur3"` → yedeği geri kopyala → `cmp`. Sürücü: `ham/PATCH-2-olcum/mut.py`; günlükler `ham/PATCH-2-olcum/mut-log/`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | Genel `AddJsonOptions(Strict)` silindi | P2_GENEL_fatura_POST_tirnakli_toplam…, P2_GENEL_fatura_kalem_POST_tirnakli_miktar…, P2_GENEL_ic_ice_kalem_yolu_anahtari_kalemler_0_adet | AYNI |
| M1-2 | `UrunGuncelleDto` sınıf Strict silindi | P2_urun_DTO_sinif_Strict_web_varsayilaninda_da_tirnakli_sayiyi_REDDEDER (11 vaka), P2_UrunGuncelleDto_11_sayi_uyesinin_hepsi_kapsamda | AYNI |
| M1-3 | `UrunOlusturDto` sınıf Strict silindi | P2_urun_DTO_sinif_Strict_web_varsayilaninda_da_tirnakli_sayiyi_REDDEDER (11 vaka) | AYNI |
| M1-4 | `JsonListe` Strict satırı silindi | P2_JsonListe_vezne_PATCH_kalem_tirnakli_adet_400_alan_kalemler_fis_DEGISMEZ | AYNI |
| M2-1 | TaslakKalemGuncelle `miktar = Math.Round(...)` silindi | Sinir3_fatura_kalem_miktar_0_0004_yuvarlaninca_sifir_iscilikli_satirda_da_RET, P2_fatura_kalem_PATCH_miktar_yuvarlaninca_sifir… (3 vaka) | AYNI |
| M2-2 | KalemYolu → `g.Miktar <= 0m` | P2_servis_fatura_OLUSTUR_miktar_0_0004_iscilikli_RET_fatura_ACILMAZ | AYNI |
| M2-3 | TaslakKalemEkle → `g.Miktar <= 0m` | P2_servis_TaslakKalemEkle_miktar_0_0004_iscilikli_RET_kalem_EKLENMEZ | AYNI |
| M2-4 | Vezne yuvarlama silindi | P2_vezne_POST_ve_PATCH_kalem_adet_0_0004_400_alan_kalemler | AYNI |
| M2-5 | Sipariş yuvarlama silindi | P2_siparis_POST_kalem_adet_0_0004_400_siparis_ACILMAZ | AYNI |
| M2-6 | Satın alma KalemKur yuvarlama silindi | P2_satinalma_POST_kalem_ve_MAL_KABUL_adet_0_0004_400 | AYNI |
| M2-7 | Mal kabul yuvarlama silindi | P2_satinalma_POST_kalem_ve_MAL_KABUL_adet_0_0004_400 | AYNI |
| M2-8 | İrsaliye yuvarlama silindi | P2_irsaliye_POST_kalem_adet_0_0004_400_irsaliye_ACILMAZ | AYNI |
| M2-9 | Satın alma adet hatasından `AlanIle` silindi | P2_satinalma_POST_kalem_ve_MAL_KABUL_adet_0_0004_400 | AYNI |
| M3-1 | Yapay "dto zorunludur" düşürme satırı silindi | P2_GENEL_bozuk_JSON…, P2_GENEL_fatura_POST…, P2_GENEL_fatura_kalem_POST…, P2_GENEL_ic_ice…, P2_urun_PATCH_1e30… (+ ürün dize vakaları) | AYNI |
| M3-2 | `JsonYoluAlani` "$." soyma silindi | aynı küme + P2_JSON_yolu_alan_anahtarina_cevrilir | AYNI |
| M3-3 | Fatura `Sayi` hatasından `AlanIle(ad)` silindi | P2_fatura_kalem_PATCH_JsonElement_tirnakli_sayi_400_alan_SQL_DEGISMEZ (miktar, iscilikTl) | AYNI |

16/16 kırmızı, 16/16 cmp AYNI. Gizlenmeyen not (Anayasa md.1): ilk mutasyon koşumu başka şeridin (KAR-RAPOR, `Kuyumcu.Web/Pages/KarZarar.razor`) o anki derleme kırığı yüzünden build'siz koştu ve sonuçları GEÇERSİZ sayıldı; sürücü durduruldu, yarıda kalan M2-1 dosyası yedekten geri yüklenip `cmp` ile doğrulandı, sürücü "build kırmızıysa test koşma, dosyayı geri al, 60 sn bekle, tekrar dene" biçimine getirildi ve tüm liste yeniden koşuldu (tablodaki sonuçlar ikinci koşumdur). Ürün DTO özniteliği genel ayarla gölgelendiği için HTTP testleriyle değil, web varsayılanıyla çözümleyen saf testle sınanır (M1-2/M1-3). Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

## 8. Tam süit ve denetim
`KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-2` (son kodla, tek koşum): **Toplam 9480 · Geçti 9478 · Başarısız 0 · Atlandı 2** (`TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli`, `SATIS3EkranTestleri.DOG_T24_…` — önceden Skip'li, bu paketle ilgisiz). **Bu paketin yeni kırmızısı: 0.** Özet: `ham/PATCH-2-olcum/tam-suit-ozet.txt`.

`bash dotnet/denetim.sh`: rc=1, **303 ✓**, tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ). Ham: `ham/PATCH-2-olcum/denetim.txt`.

## 9. Canlı doğrulama (API 5188, bağımsız SQL — kuyumcu_net)
Kiracı `PAKETPATCH-2-TEST …` sistem davetiyle açıldı (rastgele anahtar /private/tmp altında chmod 600, yalnız API ortam değişkenine verildi; iş sonunda dosya boşaltıldı). Betik `ham/PATCH-2-olcum/canli-p2.mjs`; çıktılar `canli-p2.txt` (1. koşum) ve `canli-p2b.txt` (alan düzeltmesinden sonra, API yeniden başlatılarak; son kanıt). Çıktılarda parola dizgesi `grep -c` → **0**.

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| PATCH ürün 11 alan × `"1.500"`,`"45.000"` | 400 alan:<ad>, temiz | **22/22** 400, her biri kendi alanıyla, `dto`/`$` yok | önce=sonra `10.0000 0.0000 9.160000 20000.00 0.00 ∅… guncellendi 13:35:43.899953` (DEĞİŞMEDİ) |
| PATCH ürün `{"gramAgirlik":1e30}` | 400 alan:gramAgirlik | 400 `alanlar=[gramAgirlik]` | aynı |
| PATCH ürün `{"satisFiyati": }` (bozuk) | 400 | 400 alan:satisFiyati "Satış fiyatı okunamadı…" | aynı |
| PATCH ürün `{"satisFiyati":1500}` | 200 | 200 | `1500.00` |
| POST ürün satisFiyati `"1.500"` | 400 | 400 alan:satisFiyati | ürün sayısı `1 / 1` |
| PATCH kalem `{"iscilikTl":100}` | 200 | 200 | `1.000 1200.00 100.00 1300.00 1300.00 1` |
| PATCH kalem `{"miktar":0.0004}` / `0.0001` | 400 alan:miktar | 400 ×2 "Miktar sıfırdan büyük olmalıdır" | — |
| PATCH kalem `{"miktar":"1.500"}` | 400 alan:miktar | 400 alan:miktar (1. koşumda alansızdı → düzeltildi) | — |
| POST kalem ekle 0,0004 + işçilik / `"1.500"` | 400 | 400 alan:miktar ×2 | sonra `1.000 1200.00 100.00 1300.00 1300.00 1` (DEĞİŞMEDİ, kalem sayısı 1) |
| PATCH kalem `{"miktar":0.0005}` (sınır) | 200, 0,001 | 200 | `0.001 1200.00 100.00 101.20 101.20 1` |
| POST fatura kalem 0,0004 + işçilik | 400 | 400 alan:kalemler[0].miktar | fatura sayısı `1 / 1` |
| POST fatura toplam `"1.500"` | 400 alan:toplam | 400 alan:toplam | aynı |
| POST vezne adet 0,0004 | 400 alan:kalemler | 400 alan:kalemler | fiş sayısı 0→(sonraki geçerli fiş)1 |
| PATCH vezne kalem 0,0004 / `"1.500"` | 400 alan:kalemler | 400 ×2 | kalem `1.000/1000.00` önce=sonra |
| POST sipariş adet 0,0004 | 400 | 400 alan:adet | `0 / 0` |
| POST satın alma adet 0,0004 | 400 | 400 alan:kalemler | `0 / 0` |
| POST irsaliye 0,0004 / `"1.500"` | 400 | 400 alan:kalemler / alan:kalemler[0].adet | `0 / 0` |
| POST cari veresiye `{"tlTutari":"1.500"}` (dokunulmayan modül) | 400 | 400 alan:tlTutari | hareket `0 / 0` |

(1. koşumda Kasa hareketi denendi: yeni kiracıda kasa olmadığı için 404 döndü — kanıt değeri yok; 2. koşumda Cari veresiye ile değiştirildi.)

API iki kez başlatıldı (PID 49414, sonra alan düzeltmesi derlemesinden sonra ikinci örnek) ve ikisi de yalnız kendi PID'iyle durduruldu: `lsof -nP -iTCP:5188 -sTCP:LISTEN` → 0 satır. İki test kiracısı (`01M3YCY130170B70PMGY8N76GG`, `01M3YD6Z92CJQJ6NRYTMF6JMQZ`) `zsh betikler/kiraci-temizle.sh <id> --uygula` ile silindi; 75 tenant_id'li tabloda iki kimlik için toplam satır **0**; `PAKETPATCH-2%` kiracı **0**. tenants başlangıçta **16**, sonda **18** — fark bu paketin değil: listede eşzamanlı şeritlerin kiracıları görülüyor (`PAKETKAR-RAPOR-2-TEST-E/F`, `PAKET-PARA-DEMO-3-TEST …`); bu paketin kiracı sayısına katkısı 0. QA-TEST- kiracılarına dokunulmadı.

## 10. Ekran / gerçek tarayıcı kuralı
Razor, css, bileşen, biçim, sözlük ya da dil verisi değişmedi. Değişiklik sunucu girdisinin sıkılaştırılmasıdır; istemcinin gönderdiği gövdeler (sayı tipli — § 2 taraması, 213 çağrı) aynı sonucu üretir, bUnit dahil tam süit yeşil. Yeni 400 iletileri yalnız istemcinin üretmediği (elle/dış) isteklerde görülür. Bu nedenle tarayıcı ölçüm tablosu üretilmedi (PATCH-SOZLESME § 9 gerekçesi ile aynı); hasım/orkestratör ekrandan doğrulama isterse ürün düzenle + fatura kalem düzenle akışı ilk adaydır.

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (bu pakete ait olanlar; ağaçtaki diğer değişiklikler başka şeritlerindir):
- ` M dotnet/Kuyumcu.Api/Program.cs`
- ` M dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- ` M dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`
- `?? dotnet/Kuyumcu.Api/Modules/KolonTavani.cs` (önceki paketin yeni dosyası; bu pakette düzenlendi)
- `?? dotnet/Kuyumcu.Api/Modules/Vezne/`, `Siparis/`, `Satinalma/`, `Irsaliye/` (izlenmeyen dizinler; bu pakette ilgili `*Modulu.cs` düzenlendi)
- `dotnet/Kuyumcu.Tests/PatchIkiTestleri.cs` (YENİ; test dizini izlenmiyor)

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| [KRİTİK] ürün PATCH "1.500" → 400, SQL değişmez; hasım testi yeşil | KARŞILANDI | 33 test vakası + hasım testi + M1-1/M1-2 + canlı 22/22 |
| Strict silinince test kırmızı | KARŞILANDI | M1-2, M1-3 (saf), M1-1 (genel) |
| [KRİTİK] fatura kalem 0,0004 → 400, satır yazılmaz (tüm kalem yolları) | KARŞILANDI | Fatura ×3 yol + Vezne/Sipariş/Satın alma ×2/İrsaliye; M2-1…M2-9; canlı |
| [DÜŞÜK] JSON tür hatası gövdesi: `$.x` → `x`, "dto zorunludur" yok, anlamlı ileti | KARŞILANDI | M3-1, M3-2; canlı 1e30 |
| Tarama tabloları (tırnaklı sayı + yuvarlama) | KARŞILANDI | § 2 |
| İstemci sayıyı number mı string mi gönderiyor — kanıt | KARŞILANDI | § 2 (213 çağrı taraması, ApiIstemci) |
| Build 0/0 | KARŞILANDI | temiz build 0 Uyarı 0 Hata |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | 303 ✓, yalnız P-30 ✗ |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9478/9480 geçti, 0 başarısız |
| Canlı API + bağımsız SQL (5188) | KARŞILANDI | § 9 |
| Test kiracısı silindi | KARŞILANDI | satır 0; kiracı farkı başka şeritlerin |
| Sır basılmadı | KARŞILANDI | çıktılarda parola sayımı 0; anahtar dosyası boşaltıldı |

## 13. Açık kalanlar
- **Satış (PARA-DEMO şeridi alanı, dokunulmadı):** `SatisModulu.cs:954` alış kalem `k.Adet <= 0m` ve `SatisModulu.P21.cs:212` rezerv oluştur `dto.Adet <= 0m` kontrolleri yuvarlamadan önce — 0,0004 aynı sınıf (kolona 0,000). `P21.cs:367` rezerv PATCH `adOge.GetDecimal()` dize/1e30'da 500 verir (sessiz okuma değil). Sahibi: Satış/PARA-DEMO şeridi; çözüm bu paketteki kalıp (önce `Yuvarla3`, sonra kontrol; `KolonTavani.JsonSayi`).
- Ürün DTO özniteliği genel ayarla gölgelidir (bilinçli ikinci hat); genel ayar kaldırılırsa ürün uçları yine korunur.
- PATCH-SOZLESME tur 3 açıkları (tek 6 hane yuvarlama kuralı, POST kolon tavanları, genel MySQL 1264 → 400 ağı, `DogrulamaSozlugu.AralikMesaji`) aynen geçerli; bu paket onlara dokunmadı.
- Canlıda görülen: fatura kalem ekle 0,0004 için controller yolunda ileti "Miktar 0,001 veya daha büyük olmalıdır" (DTO Range), servis yolunda "Miktar sıfırdan büyük olmalıdır" — iki ileti aynı kuralı söyler; tek ileti kararı sözlük sahibine.

## 14. Kurucu / mali müşavir
Mevzuat hükmü içeren değişiklik yok (yalnız giriş doğrulaması ve kolon hanesine yuvarlama). Mali müşavire soru yok.

## 15. Devir doğrulaması (02.10, düşen uygulayıcının yerine — BAŞTAN YAZILMADI)
- 5188/5189 ve CDP 9980-9995 portlarında kalmış süreç: **yok** (lsof boş).
- Kaynakta `if (false` / `@if (true)` izi (Api/Web/Core, .cs/.razor): **0**.
- Bu paketin kaynak dosyalarının son değişikliği 16:38:38; tam süit TRX'i 16:39:13'te başlamış → raporlanan 9480/9478/0 son kodla koşulmuştur (tekrar koşulmadı).
- `dotnet build Kuyumcu.sln --nologo`: **0 Uyarı, 0 Hata** (rc=0).
- Filtreli `PatchIki|PATCHSOZLESMESinir` (KUYUMCU_TEST_DB=kuyumcu_test_t9): **104/104 başarılı, 0 başarısız**.
- `bash dotnet/denetim.sh`: rc=1, **303 ✓**, tek ✗ P-30 DEMO_INSAN_SURESI (istemde hariç).
- kuyumcu_net: bu paketin iki kiracı kimliği / `PAKETPATCH-2%` adı → **0**; tenants şu an 17 (diğer şeritlerin kiracıları: PAKETKAR-RAPOR-2-TEST-E/F vb.).
- Kanıt dosyaları `ham/PATCH-2-olcum/` altında mevcut (canli-p2*.txt, mut-log 17 dosya, tam-suit-ozet.txt, denetim.txt, tarama betikleri).
