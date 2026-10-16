# PAKET PATCH-4 — uygulama raporu (02–03.10, dalga PATCH-4 · t9 · 5188/5189)

Rol: Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-PATCH-3-uygulama-raporu.md` ve hasım t2 + tasarım t1 bulguları.
Kanıtlar `ham/PATCH-4-olcum/` altında: canlı betik ve çıktısı, ölçüm betikleri, `olcum*.jsonl`, 46 ekran görüntüsü (`foto/`), mutasyon sürücüsü (`mut4.py`), sonuçlar ve günlükler (`mut-log/`), denetim ve tam süit özeti. Mutasyon yedekleri `/private/tmp/kuyumcu-serit/PATCH-4/mut-yedek/`, TRX `/private/tmp/kuyumcu-serit/trx-pPATCH-4/` altında. Proje ağacına geçici dosya bırakılmadı.

## T2. TUR 2 (03.10, 01:15–02:35) — doğrulayıcı bulgularının kapatılması

Tur 1 doğrulamasında kalan dört bulgu bu turda kapatıldı. Aşağıdaki bölümler (§ 1–16) tur 1 kaydıdır; tur 2'de değişen sayılar bu bölümde. Kanıtlar `ham/PATCH-4-olcum/t2/` altında.

### T2.1 Bulgu → yapılan

| Bulgu | Yapılan | Dosya |
|---|---|---|
| **KRİTİK** — gerçek milyem `[Range(1, 1000)]` int işlenenli; 1000,4 / 1000,01 / 0,6 kabul ediliyor (has brütü aşıyor); servis içi kontrol yok | (1) DTO'lar `[Range(typeof(decimal), "1", "1000")]` (POST + PATCH). (2) **Servis içi kapı** `UrunServisi.GercekMilyemKapisi(decimal?)`: verildiyse `1 ≤ m ≤ 1000` ve **en çok 2 ondalık** (kolon `decimal(7,2)`; sessiz yuvarlama yerine red), değilse `BadRequest.AlanIle("gercekMilyem")`. `Olustur` ve `Guncelle` başında çağrılır (controller dışı çağıranlar — içe aktarım vb. — da kapıdan geçer). (3) Doğrulayıcının `PATCH4SinirTestleri.cs` içindeki **4 Skip kaldırıldı** (3 milyem + 1 yuvarlama) — hepsi yeşil | `UrunModulu.cs` |
| **ORTA** — `Ayarlar.ToHasGram` 6. hane ToEven; proje kuralı Yuvarla6 AwayFromZero | `Math.Round(…, 6, MidpointRounding.AwayFromZero)`. Etki yalnız tam yarım sınırında (7. hane = 5): K24 · 1,0003 g · 995 → **0,995299** (eskiden 0,995298). İstemci (hurda/alış önizleme) ve sunucu aynı işlevi çağırdığı için eşlik korunur | `Kuyumcu.Core/Domain/Ayarlar.cs` |
| ↳ yan etki | `Dogrulayici/SATIS3SinirTestleri.Tur4_has_4_hane_0_0001_gram_kaybolmaz` eski ToEven değerini (5,850058) "proje kuralı" diye kilitliyordu → beklenen **5,850059** yapıldı, yorumla gerekçelendi (tek satır; ikinci doğrulayıcının ToEven kilidi, PATCH-4 doğrulayıcısının açık talebiyle çelişiyordu — yeni kural ağır basar) | `SATIS3SinirTestleri.cs` (yalnız o assert) |
| **DÜŞÜK** — satır içi ADET düzenlemesi tavana tabi değil | `FisKalemTablosu.AdetDegistir` → `FisKalemGirisi.TavanAsildi(d, k.BirimFiyat)` (Ekle ve satır içi fiyat ile TEK kural); aşınca adet **değişmez**, alan altı ileti. bUnit vakası yazıldı | `FisKalemTablosu.razor`, `Sozlukler.PatchDort.cs` |
| ↳ Chrome bulgusu (bu turda bulundu ve kapatıldı) | İlk ölçümde PRO 1280/375'te adet hücresinin (≈ 52 px) altındaki uzun ileti **taştı** (K3 tasan=1, `ham/…/t2/foto/ilk-*`). İkinci denemede `overflow-wrap:anywhere` kelimeyi böldü (K3 kırık=1), üçüncüde `max-content` komşu hücreye bindi (K9=1). **Son çözüm:** ileti, varsa, satırın altındaki tam genişlik satırda (`tr.fis-pro-ayrinti.fis-pro-satir-hata`, td colspan 6 + dolgu 3 — mevcut ayrıntı satırı kalıbı), sağa yaslı (adet sütununun altında biter); kısa biçim "Satır tutarı çok büyük." (EN "Line total too large."). id aynı: `#alan-hata-si-kalem-adet-{id}`. İleti yokken boş satır çizilmez (bUnit ile kilitli) | `FisKalemTablosu.razor`, `tuval-satis.css` (+1 kural, sabit renk yok) |
| **DÜŞÜK** — kiracı sayısı kanıtı | Kiracı **kimliği** üzerinden: 75 tabloda satır toplamı 0 (aşağıda) | — |

### T2.2 Yeni testler (+15 vaka)

- `PatchDortHttpTestleri.P4_gercek_milyem_UC_ondalik_PATCH_ve_POST_400_veri_DEGISMEZ` — 916,555 PATCH ve POST → 400 `gercekMilyem`, SQL önce = sonra, ürün oluşmaz (bunu yalnız servis kapısı yakalar).
- `PatchDortMilyemKapisiTestleri.P4_GercekMilyemKapisi_SERVIS_ici_sinir_tablosu` — 11 satır (1, 1000, 916,5, 999,99 geçer; 0,99 / 0,6 / 1000,01 / 1000,4 / 916,555 / −920 / 0 → `BadRequestException` + alan işareti `gercekMilyem`).
- `PatchDortMilyemKapisiTestleri.P4_DTO_Range_ONDALIGI_yuvarlamadan_sinar` — 3 satır × (Oluştur + Güncelle DTO) `Validator.TryValidateObject`.
- `PatchDortFisTavanTestleri.P4_satir_ici_ADET_TAVANI_asarsa_KALEM_DEGISMEZ_alan_alti_mesaj` — 9e12 ₺ kalemde adet 2 → adet 1 kalır, ileti `tr.fis-pro-satir-hata` içinde; adet 1,1 kabul, ileti satırı yok.
- Doğrulayıcının 6 vakası (Skip'ten çıkan): `D_P4_gercek_milyem_1000_KABUL_1000_01_RET`, `D_P4_gercek_milyem_ARALIK_DISI_ondalik_PATCH_400` ×3, `…_POST_400` ×2, `D_P4_has_6_hane_YARIM_sinirinda_AwayFromZero`.

Filtreli: `PatchDort|PATCH4Sinir` **58/58**; geniş küme (PatchDort, PATCH4Sinir, SATIS3, HasHesap, Fis, PatchUc, P10Urun, Maden, B4Altin, Urun, P78Sayi, UX1C1Ekran, S21FisBilesen, TS2Sinir, OlcekTek) yeşil (`t2-filt8`: 1116/1116 + 1 atlanan).

### T2.3 Mutasyon (sürücü `t2/mut5.py`, sonuç `t2/mut-sonuc.txt`)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M6-1 | servis kapısı aralık kontrolü `if (false)` | P4_GercekMilyemKapisi_SERVIS_ici_sinir_tablosu | AYNI |
| M6-2 | 2 ondalık kontrolü `if (false)` | P4_gercek_milyem_UC_ondalik_PATCH_ve_POST_400…, P4_GercekMilyemKapisi_SERVIS… | AYNI |
| M6-3 | PATCH DTO `[Range(1, 1000)]` (eski int) | P4_DTO_Range_ONDALIGI_yuvarlamadan_sinar | AYNI |
| M6-4 | `Olustur` kapı çağrısı silindi | P4_gercek_milyem_UC_ondalik_PATCH_ve_POST_400… | AYNI |
| M6-5 | `Guncelle` kapı çağrısı silindi | P4_gercek_milyem_UC_ondalik_PATCH_ve_POST_400… | AYNI |
| M7-1 | `ToHasGram` AwayFromZero → ToEven | D_P4_has_6_hane_YARIM_sinirinda_AwayFromZero | AYNI |
| M8-1 | `AdetDegistir` tavan kapısı `if (false)` | P4_satir_ici_ADET_TAVANI_asarsa… | AYNI |
| M8-2 | ileti satırı koşulu `if (false)` | P4_satir_ici_ADET_TAVANI_asarsa… | AYNI |

**8/8 kırmızı, 8/8 cmp AYNI.** Md.1 notu: ilk koşumda M7-1 ve M8-1 ek olarak `P4_gercek_milyem_UC…` testini de kırmızı gösterdi. Sebep **paralel derleme yarışı**: `obj/Debug/Kuyumcu.Api.dll` (01:31:12) geri yüklenen kaynaktan (01:30:50) yeni ama M6-5 mutasyonlu içerikle kalmıştı; aynı dakikada başka bir şeridin `dotnet test … tam.trx` + MSBuild düğümleri çalışıyordu (`ps` ile görüldü). Kaynaklar `grep` ile temiz doğrulandı, `touch` + derleme sonrası 58/58 yeşil; M6-5, M7-1, M8-1 yeniden koşuldu — yalnız kendi testleri kırmızı (tablodaki sonuç bu koşumdur). Son mutasyondan sonra `dotnet build Kuyumcu.sln` **0 Uyarı 0 Hata**.

### T2.4 Tam süit ve denetim (son kod)

- `tam-t2c.trx` (02:08): 9925 · geçti 9919 · **başarısız 4**: `P4_satir_ici_ADET…` (Bunit ElementNotFound), `MadenIkiRaporlarEkranTestleri.MI22…`, `MadenIkiDbTestleri.MI05…`, `Yasal4…Mutabakat_bugun…`. Kendi testim tek başına **3/3 yeşil** (aynı dll); M8-2'nin derleme penceresiyle çakışan paralel derleme şüphesiyle süit hemen yeniden koşuldu.
- **`tam-t2d.trx` (02:20, esas alınan): Toplam 9925 · Geçti 9922 · Başarısız 1 · Atlandı 2.** Tek kırmızı `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_…` — tur 1'de de vardı, `DateTime.UtcNow` "bugün" karşılaştırması yerel 00:00–03:00 aralığında kırılıyor (koşum yerel 02:20); sahibi Yasal4/ZO-İŞLEM. **Bu paketin yeni kırmızısı: 0.**
- Ara süitlerde görülen `MADENAYRIMISinirTestleri.MAS20`, `KarRapor2DbTestleri.KR50`, `MadenIki*` kırmızıları MADEN-AYRIMI şeridinin eşzamanlı çalışmasından: MAS20, `ToHasGram` geçici olarak ToEven'a döndürülünce de kırmızıydı (`t2-yedek/mas20.log`, sonra yedek geri, cmp AYNI); son süitte hepsi yeşil.
- `bash dotnet/denetim.sh` → **303 ✓, 1 ✗ = P-30 DEMO_INSAN_SURESI** (istemde hariç). Tur 1'deki OLCEK-1 (`degisim-gunsonu.css`) artık temiz. Ham: `t2/denetim-t2.txt`.

### T2.5 Canlı doğrulama (API 5188 Release anlık görüntüsü, bağımsız SQL — kuyumcu_net)

Kiracı `01M3ZCAP3TRM7WY2GF274KV84X` (`PAKETPATCH-4-TEST …`). Tur 1'in 7 adımı yeniden koşuldu (hepsi aynı sonuç: tam gövde → `920.00 9.200000 B-YENI K22 10.000 92.000000` değişmedi; ödeme bacağı 4/4 400 alanlı; rezerv 2/2 400 `bitisGunu`). Yeni adımlar (`t2/canli-p4-kosum-t2.txt`), **5xx: 0**:

| Adım | Beklenen | Gerçek | SQL (milyem · has · barkod · ayar · stokAdet · stokHas) |
|---|---|---|---|
| PATCH gercekMilyem 1000.4 / 1000.01 / 0.6 | 400 alan gercekMilyem | 400 `[gercekMilyem]` ×3 "Gerçek milyem 1 ile 1000 arasında olmalıdır" | önce = sonra `920.00 9.200000 B-YENI K22 10.000 92.000000` (DEĞİŞMEDİ) |
| PATCH gercekMilyem 916.555 | 400 alan | 400 `[gercekMilyem]` "Gerçek milyem en çok 2 ondalık basamak taşıyabilir" | (DEĞİŞMEDİ) |
| POST gercekMilyem 1000.4 / 916.555 | 400 alan, ürün yok | 400 `[gercekMilyem]` ×2 | `P4-POST-%` ürün sayısı **0** |
| PATCH gercekMilyem 1000 (sınır) | 200 | 200 | `1000.00 10.000000 … 100.000000` |
| PATCH gercekMilyem 920 (geri) | 200 | 200 | `920.00 9.200000 … 92.000000` |
| POST K24 · 1,0003 g · 995 | has 0,995299 | 201 · yanıt `hasGrami=0.995299` | `995.00 0.995299` |

### T2.6 Gerçek (başsız) Chrome ölçümü — CDP 9983, vekil 5189, temiz profil (`profil-t2d`, `profil-t2e`)

**A. Satış satır içi adet tavanı** (`olc-p4d.mjs`, `t2/olcum-d.jsonl`): ürün seçilir, birim fiyat 9.000.000.000.000 ile eklenir, satır içi adet 2 kaydedilir (red), sonra 1,1 (kabul).

| Mod · tema · genişlik | Red: ileti / adet | Kabul: adet / ileti | K1 · K2 · K3 · K5 · K9 · K10 · K12 · Y |
|---|---|---|---|
| PRO açık 1280 | "Satır tutarı çok büyük." görünür / **1** | 1,1 / yok | 0 · 98 · 0/0 · 0 · 0 · 0 · 0 · 0 |
| PRO açık 375 | aynı / 1 | 1,1 / yok | 0 · 97 · 0/0 · 0 · 0 · 0 · 0 · 0 |
| PRO koyu 1280 | aynı / 1 | 1,1 / yok | 0 · 98 · 0/0 · 0 · 0 · 0 · 0 · 0 |
| PRO koyu 375 | aynı / 1 | 1,1 / yok | 0 · 97 · 0/0 · 0 · 0 · 0 · 0 · 0 |
| BASİT açık/koyu 1280 | satır içi adet düzenleyici **yok** (BASİT görünüm `FisVerdimAldim`; kalem tablosu PRO'ya özgü) | — | 0 · 69 · 0/1 · 0 · 1 · 0 · 0 · 0 |
| BASİT açık/koyu 375 | aynı | — | 0 · 97 · 0/1 · 0 · 0 · 0 · 0 · 0 |

BASİT'teki K3 kırık 1 ("Toplam · 10,00 g") ve K9 1 ("Tutar ⟂ Son 3 satış") tur 1'de de ölçülen, 13 haneli uç tutarda `FisVerdimAldim`'den gelen durumdur (bu paketin dosyası değil; § 15'te sahibiyle). K2 %69 (BASİT 1280) satış tuvalinin iki sütunlu düzeni (sağda son satışlar), tur 1 ile aynı.

**B. Ürün düzenle — sunucu 400'ünün ekranda karşılığı** (`olc-p4e.mjs`, `t2/olcum-e.jsonl`): çekmece açılınca `#d-gercek-milyem` = **920** (8/8); 916,555 yazılıp Kaydet → alan altında "Gerçek milyem en çok 2 ondalık basamak taşıyabilir" (`#alan-hata-d-gercek-milyem`), `aria-invalid=true`, çekmece açık kalır (8/8). SQL sonrası `920.00 9.200000 B-YENI 92.000000` (değişmedi). Çekmece K1 0 · K3 0/0 · K5 0 · Y 0; K9 2–3 tur 1 § 10-A gerekçesiyle aynı (kaydırılan içerik sabit başlık/alt çubuğun arkasından geçer).

**Read ile görülen görüntüler:** `t2-adet-tavan__PRO_aydinlik_1280` (ileti satırın altında, adet sütununun altında biter; genel toplam kartta), `t2-adet-tavan__PRO_karanlik_375` (iki katlı mobil satırın altında sağa yaslı ileti), `t2-milyem-3hane__BASIT_karanlik_375` (alan kırmızı çerçeve, ileti alanın altında), ilk denemelerin taşan/kırılan hâli (`ilk-t2-adet-tavan__*`).

### T2.7 Temizlik

API (PID), vekil durduruldu; Chrome betik sonunda kapandı. 5188 / 5189 / 9983 `lsof` → 0. 5199/5200'e dokunulmadı. `zsh betikler/kiraci-temizle.sh 01M3ZCAP3TRM7WY2GF274KV84X --uygula` rc 0 → **tenant_id kolonu olan 75 tabloda bu kiracının satır toplamı 0**, `tenants` içinde kimlik 0. (Bilgi: tenants başlangıç 16, son 16; başka şeritler eşzamanlı kiracı açıp sildiği için sayı tek başına kanıt sayılmaz.) `fikstur.json`, `sistem.anahtar` (bu turda kendi API'm için rastgele üretildi) `printf ''` ile boşaltıldı; `ham/…/t2` parola/jeton taraması 0.

### T2.8 Mevcut veri taraması (kuyumcu_net, SALT OKUMA — tekrar)

Milyem dağılımı: K22→916 (60), K14→585 (16), K18→750 (15), K24→1000 (13), G925→925 (13) — **standart dışı milyem yok**; has ≠ (gram−taş)×milyem/1000 olan ürün **0**. Barkodu X/Y/D-B/YALNIZ-BARKOD ya da kodu DP4%/P4-% olan ürün **0** (hasım test kaydı kalmamış). 02.10'dan sonra oluşan 3 ürün `PAKETMADEN-AYRIMI-TEST-DOG` kiracısında (MADEN-AYRIMI şeridi; dokunulmadı).

### T2.9 Tur 2'de değişen dosyalar

- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — DTO decimal Range ×2, `GercekMilyemKapisi` + 2 çağrı
- `dotnet/Kuyumcu.Core/Domain/Ayarlar.cs` — `ToHasGram` AwayFromZero (**dosya alanı dışı; doğrulayıcının açık talebi**)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.PatchDort.cs` — `SatirTutariCokBuyuk` TR+EN
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor` — AdetDegistir tavanı; adet iletisi ayrı satırda
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` — `.fis-pro-satir-hata > td` (1 kural)
- `dotnet/Kuyumcu.Tests/PatchDortTestleri.cs` — +3 metot (+15 vaka), 1 vaka güçlendirildi
- `dotnet/Kuyumcu.Tests/Dogrulayici/PATCH4SinirTestleri.cs` — yalnız 4 `Skip` kaldırıldı
- `dotnet/Kuyumcu.Tests/Dogrulayici/SATIS3SinirTestleri.cs` — 1 assert (5,850058 → 5,850059) + yorum

Dokunulan sayfalar: tur 2'de `Pages/*.razor` açılmadı (yalnız bileşen `FisKalemTablosu.razor`).

### T2.10 Kurucu / mali müşavir

Mevzuat hükmü eklenmedi. Not: 6. hane yuvarlama yönü (yarım sınırda yukarı) bir **muhasebe kuralı** değil, projenin iç tutarlılık kuralıdır (Yuvarla6 ile aynı); etki milyonda bir gram, yalnız tam yarım sınırda. Mali müşavirin has gram yuvarlama yönüne ilişkin özel bir tercihi varsa tek noktada (`Ayarlar.ToHasGram`) değiştirilebilir.

## 1. Spesifikasyonun kodla karşılaştırılması

| İddia (bulgu) | Koddaki durum | Sonuç |
|---|---|---|
| KRİTİK: Ürün düzenle Kaydet, gerçek milyemi sessizce standarda çeviriyor | Doğruydu. İki kök neden vardı. (a) `GET /urunler/{id}` varlığı döndürüyordu ve varlıkta yalnız `milyem` alanı var, `gercekMilyem` yok. Bu yüzden çekmecede `dGercekMilyem` hep boş kalıyor, her kayıtta `null` gidiyordu. (b) `UrunModulu.Guncelle` `dto.AyarId is not null` koşulunu "ayar değişti" diye yorumluyor, `GercekMilyem ?? standart` hesabına düşüyordu | Düzeltildi (§ 2) |
| KRİTİK: "Kod (isteğe bağlı)" boşaltılınca kayıt sessizce olmuyor | **PATCH-3 tur 2'de kapanmış.** `d-kod` alanı `Zorunlu="true" Durum="form" HataAlani="d-kod"` taşıyor | Bu pakette Chrome ile yeniden ölçüldü, kod değişmedi (§ 7-B) |
| YÜKSEK: "Gram (isteğe bağlı)" boşaltılınca hata çıkıyor | **PATCH-3 tur 2'de kapanmış.** Alan `Zorunlu="@(dBirim != "GRAM")"` taşıyor | Yeniden ölçüldü, kod değişmedi (§ 7-B) |
| DÜŞÜK: Ödeme bacağı DTO'sunda tavan yok | Doğruydu (`OdemeSepeti.cs` TutarTl/DovizTutar `double.MaxValue`, ALTIN bacağında aralık yok) | Düzeltildi |
| DÜŞÜK: Rezerv PATCH'te geçmiş bitiş günü alansız 400 dönüyor | Doğruydu. Oluşturma yolu (`RezervOlustur`) da alansızdı | İkisi de düzeltildi |
| ORTA: Satış PRO'da çok büyük tutar; istemcide tavan yok, toplam taşıyor | Doğruydu | İstemci tavanı + toplam kartı sığdırma (§ 4) |
| DÜŞÜK: 6 hane ToEven çağrıları | Dosya alanı dışında (AcilisYazici, MaliyetMotoru …) | Yapılmadı, orkestratör kuyruğunda (§ 12) |

**Sapma (gerekçeli):** İstemci tavanı `Islemler.razor`'a değil, `Bilesenler/Fis/FisKalemGirisi.razor` (Ekle düğmesi) ve `FisKalemTablosu.razor` (satır içi fiyat) dosyalarına konuldu. Gerekçe: istem, Islemler.razor'da TOPLU-5 ile çakışma olursa durmamı söylüyordu. TOPLU-5 bu dosyayı aynı gün değiştirmişti, 00:26'da da başka bir şerit değiştirdi. Bileşen dosyaları TOPLU-5'in listesinde yok. Toplam kartı için `tuval-satis.css`'e 3 satır eklendi. Bu dosyalar istemdeki "DOSYA ALANI" listesinde yok; kapsamdaki maddeyi uygulamak için gerekliydi ve rapora açıkça yazılıyor.

## 2. Öncelik 1 — gerçek milyem korunur (VERİ)

**Sunucu (`UrunModulu.cs`):**
- `UrunGuncelleDto.GercekMilyem` artık **üç durumlu**: gövdede yoksa korunur, `null` gelirse ayarın standardına döner, değer gelirse yazılır. Mevcut "Verildi" deseni kullanıldı (`GercekMilyemVerildi`); yeni bir kalıp açılmadı.
- "Gönderildi" yerine "değişti" kuralı: `ayarDegisti = hedefAyarId != mevcut.AyarId`. Hedef milyem şöyle belirlenir: değer verildiyse o değer, `null` verildiyse yeni ayarın standardı; hiç verilmediyse ayar değiştiyse yeni standart, değişmediyse **mevcut milyem**.
- `hasEtkileyen` yalnız gerçekten değişen bir değerle (birim, gram, taş, ayar, milyem) tetiklenir. Aynı değer gönderilirse has gram ve `stoklar.toplam_has_gram` **hiç yeniden hesaplanmaz**. Tarihsel stok has toplamı (adet × has'tan farklı birikmiş değer) bu sayede korunur.
- Yanıtta `gercekMilyem`: `UrunServisi.YanitJson` varlığın tüm alanlarını (Web varsayılanı + Strict sayı) ve buna ek `gercekMilyem` alanını döndürür. Bu alan milyem standarttan farklıysa o değeri, değilse `null` taşır. Uygulanan uçlar: GET `/urunler/{id}`, GET `/urunler` (liste), PATCH ve POST yanıtları (controller'da). Servis içi çağıranlar (içe aktarım, testler) varlığı almaya devam eder. İlk denemede servis içinde zenginleştirme yapılmıştı ve 9 mevcut test kırıldı (yansımayla `Prop` okuyorlardı); bu yüzden zenginleştirme controller katmanına taşındı.

**İstemci (`Urunler.razor` düzenle çekmecesi):**
- Açılışta `dGercekMilyem = u.GercekMilyem` artık sunucudan dolu geliyor. Ayrıca `dGercekMilyemIlk` ve `dAyarIlk` saklanıyor.
- Gövdeye `gercekMilyem` **yalnız değiştiyse** konuyor: değer girildiyse değer gidiyor, boşaltıldıysa `null` gidiyor, değişmediyse anahtar hiç gönderilmiyor. BASİT modda alan gizliyken de değer korunuyor.
- UX-S24: Ayar değişince eski ayarın gerçek milyemi yeni ayara taşınmıyor. Kullanıcı kutuyu elle değiştirmediyse kutu boşalıyor ve ipucu "standart milyem kullanılır" diyor. Ayar eski değerine dönerse kayıtlı değer geri geliyor. Bu davranış `d-ayar` seçicisinin `:after` olayına ve maden değişikliği yoluna bağlandı.

**Alan taraması tablosu ("her kayıtta gönderilen ama sunucudan dolmayan alan" kalıbı).** Gövdedeki 38 anahtarın hepsi, GET yanıtında bulunup bulunmadıklarına göre tek tek denetlendi. Bu denetim kalıcı test olarak yazıldı: `P4_duzenle_govdesindeki_HER_anahtar_GET_yanitinda_VAR_…`. Test anahtarları `Urunler.razor` kaynağından okur, bu yüzden ekrana yeni bir alan eklenir de yanıtta yoksa test kırmızıya döner.

| Anahtar(lar) | Çekmece nereden dolduruyor | Yanıtta var mı | Durum |
|---|---|---|---|
| **gercekMilyem** | `u.GercekMilyem` | **YOKTU, şimdi var** | **KUSUR, düzeltildi** |
| tip, kod, ad, ayarId, rodyumKaplama, birim, gramAgirlik, tasAgirlik | tam kart | var | sorun yok (zorunlu alanlarda `null` = koru) |
| tasIbaresi, kategoriId, reyonId, iscilikTl, satisFiyati, barkod | tam kart | var | sorun yok |
| pirlantaKarat/Renk/Berraklik/Kesim, sertifikaKurumu/No | tam kart | var | sorun yok (PIRLANTA dışı tipte `null` = bilinçli silme, PATCH-3) |
| konsinye, konsinyeCariId, konsinyeBirimBedel | tam kart | var | sorun yok |
| aciklama, pariteKodu, rfid, gorselUrl, kmtsKodu (koşullu) | tam kart | var | sorun yok |
| tedarikciId, alisMaliyeti, hedefMarj, kdvOrani, seriNo, olcu, minStok | tam kart | var | sorun yok |
| tasKokeni | `u.TasKokeni ?? ""` | var | Not: taş 0 ise ekran her zaman `"TAS_YOK"` gönderir. Eski kayıtta kolon NULL ise bu gönderim NULL'u `TAS_YOK` yapar. Anlam aynı, has ve fiyat etkisi yok (açık kalan olarak not edildi) |

## 3. PATCH-3 kalanları (sunucu)

- `OdemeSepeti.cs`: `TutarTl` ve `DovizTutar` için `Range(0.01, KolonTavani.UstTutar)`. ALTIN bacağında `GramAgirlik` için `Range(0, UstAdet)`, `BirimFiyatTlHasGram` için `Range(0, UstTutar)`. Alt sınır bilinçli olarak 0 bırakıldı: "> 0" kuralı ve iletisi `Coz` içinde aynen kalıyor. Sonuç: 400 yanıtı `alan: odemeler[i].<ad>` taşıyor.
- `SatisModulu.P21.cs`: geçmiş bitiş günü hatası hem PATCH'te hem oluşturmada `.AlanIle("bitisGunu")` taşıyor.

## 4. Satış istemci tutar tavanı (ORTA bulgu)

- `PatchDortSozlugu.TutarTavani = 9.999.999.999.999,99` (sunucudaki `KolonTavani.D15_2`). İkisinin eşitliği bir testle kilitli; değerler ayrışırsa test kırmızıya döner.
- `FisKalemGirisi.EkleTiklandi`: birim fiyat ya da adet × fiyat tavanı aşarsa (taşma da aşım sayılır) kalem **eklenmez**, fiyat kutusunun altında Türkçe ileti çıkar ve `aria-invalid` işaretlenir. Tavanın altındaki doğrulama ve ekleme sayfada (`Islemler.KalemEkle`) değişmeden kalıyor.
- `FisKalemTablosu.FiyatDegistir`: satır içi fiyat düzenlemesinde aynı kural uygulanıyor.
- İleti TR "Tutar çok büyük — satır tutarı en çok 13 haneli olabilir." / EN "Amount is too large — a line total can have at most 13 digits." (yeni dosya `Sozlukler.PatchDort.cs`).
  - İletinin ilk sürümü rakamı yazıyordu ("… en çok 9.999.999.999.999,99 ₺ olabilir."). Chrome ölçümünde PRO 375 px'te bu bölünmez rakam dar sütunda **taştı** (K3 = 1). İleti bu yüzden hane sayısıyla yeniden yazıldı ve ölçüm tekrarlandı: K3 = 0.
- `tuval-satis.css`: `.fis-pro-toplam > div { min-width: 0 }` ve `.fis-pro-toplam-deger { white-space: normal; overflow-wrap: anywhere }` eklendi. Sığmayan toplam artık hücrenin içinde kırılıyor. 12 haneli toplam 1280'de tek satırda, 375'te iki satırda duruyor; kart kenarını aşmıyor (`tasma:false`). Sabit renk yok, satır içi stil yok.

## 5. DoD sözleşmesi

Yeni varlık yok, **migration gerekmedi** (şema değişmedi). Uçlar, `[RolGerekli]`/`[ModulYetkisi]` öznitelikleri ve `DenetimServisi.YazmaKaydet` çağrıları değişmedi. Finansal varlığa silme eklenmedi.

| Varlık | Düzenleme yolu | Bu paketteki değişiklik |
|---|---|---|
| Ürün | PATCH /urunler/{id} (SAHIP, MUDUR, MUHASEBE) · pasife al/aktife al | `gercekMilyem` üç durumlu; has yalnız gerçek değişiklikte yeniden hesaplanıyor; yanıtta `gercekMilyem` |
| Rezervasyon | PATCH /islemler/rezervler/{id} · iptal | Geçmiş bitiş günü 400 yanıtı alan taşıyor |
| Satış fişi / hareketler | Değişmez; düzeltme storno/iade ile (48-A) | Yalnız ödeme bacağı girdi aralıkları ve istemci tavanı |

## 6. Testler

Yeni dosya `Kuyumcu.Tests/PatchDortTestleri.cs`: **18 metot, 25 vaka**.
- `PatchDortHttpTestleri`, HTTP katmanında 12 vaka (SQL'de önce/sonra okuyarak): ekranın tam gövdesi ile 920 korunuyor, has ve stok has değişmiyor · aynı ayar ve gram has'ı yeniden hesaplatmıyor · `null` standarda dönüyor · değer yazılıyor · ayar gerçekten değişince yeni standart uygulanıyor · GET ve liste `gercekMilyem` döndürüyor · kalıp taraması · ödeme bacağı tavanı ×4 · rezerv geçmiş gün (oluşturma + PATCH).
- `PatchDortUrunEkranTestleri`, bUnit ile 5 vaka: çekmece 920 gösteriyor · yalnız barkod değişince gövdede `gercekMilyem` anahtarı yok · boşaltınca `null` gidiyor · değiştirince yeni değer gidiyor · ayar değişince milyem taşınmıyor, ayar geri dönünce değer geri geliyor.
- `PatchDortFisTavanTestleri`, 8 vaka: tavan sunucuyla birebir · tavan tablosu (5 satır, yarım gram dahil) · Ekle düğmesi kalem eklemiyor ve ileti veriyor · satır içi fiyat kalemi değiştirmiyor.

Filtreli koşumlar: PatchDort **25/25**. Geniş küme (PatchUc, P10, Urun, PATCH2Sinir, Rezerv, CiftBacak, B4Altin, DovizBacagi, Fis, Islem, TasKokeni, IceAktarim, Barkod, PatchSozlesme, SozlesmeKapsam, Pirlanta, Maden, Parite, SatisPro, UX4, P16, PatchDort): **2123/2123**.

## 7. Mutasyon tablosu

Yöntem: kaynak `mut-yedek/<dosya>.orijinal-<kod>` olarak kopyalanır. Tek eşleşmeli değişiklik yapılır. Ardından `build --no-incremental` ve `test --filter PatchDort|PatchUc|P10UrunEkran` koşulur. Yedek yeni mtime ile geri kopyalanır ve `cmp` ile karşılaştırılır. Sürücü: `ham/PATCH-4-olcum/mut4.py`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | `ayarDegisti = dto.AyarId is not null` (eski kusur) | P4_…TAM_govdesi_…920_KORUNUR…, P4_…ayni_ayarId…HAS_YENIDEN_HESAPLATMAZ | AYNI |
| M1-2 | `GercekMilyemVerildi` → `GercekMilyem is not null` | P4_…gercekMilyem_null_STANDARDA_doner… | AYNI |
| M1-3 | `hasEtkileyen` eski "gönderildi" kuralı | P4_…TAM_govdesi…, P4_…ayni_ayarId… | AYNI |
| M1-4 | `GercekMilyemi` hep null | P4_urun_GET_ve_LISTE…, P4_…TAM_govdesi… (yanıt) | AYNI |
| M1-5 | GET controller zenginleştirmesiz | P4_duzenle_govdesindeki_HER_anahtar…, P4_urun_GET_ve_LISTE… | AYNI |
| M1-6 | Liste zenginleştirmesiz | P4_urun_GET_ve_LISTE… | AYNI |
| M2-1 | İstemci `gercekMilyem` koşulsuz gönderiyor | P4_yalniz_barkod_degisince_govdede_gercekMilyem_ANAHTARI_YOK… | AYNI |
| M2-2 | Çekmece `dGercekMilyem = null` (eski kusur) | P4_duzenle_acilinca_d_gercek_milyem_KAYITLI_920…, (+1) | AYNI |
| M2-3 | Ayar değişince temizleme kapalı | P4_ayar_degisince_eski_ayarin_gercek_milyemi_TASINMAZ… | AYNI |
| M3-1…M3-4 | Ödeme bacağı Range (TutarTl, DovizTutar, Gram, BirimFiyat) | P4_satis_fisi_odeme_bacagi_kolon_tavani… (ilgili satır) | AYNI ×4 |
| M4-1 | PATCH bitisGunu `.AlanIle` silindi | P4_rezerv_gecmis_bitis_gunu… | AYNI |
| M4-2 | Oluşturma bitisGunu `.AlanIle` silindi | P4_rezerv_gecmis_bitis_gunu… | AYNI |
| M5-1 | `f > Tavan` kontrolü kapalı | P4_tavan_…(adet: 0.5, …) | AYNI |
| M5-2 | Ekle kapısı kapalı | P4_tavani_asan_fiyatla_Ekle_KALEM_EKLEMEZ… | AYNI |
| M5-3 | Satır içi fiyat kapısı kapalı | P4_satir_ici_fiyat_TAVANI_asarsa… | AYNI |
| M5-4 | Tavan sabiti 10 kat | P4_istemci_tutar_tavani_SUNUCU…BIREBIR (+3) | AYNI |

**Sonuç: 19/19 kırmızı, 19/19 cmp AYNI.**

Anayasa md.1 gereği gizlenmeyen notlar:
- **Paralel derleme kesintileri.** İlk koşumda M3-4, M4-1, M4-2 ve M5-1 `BUILD-KIRMIZI` verdi. Sebep başka şeritlerin ağaçta yarım bıraktığı düzenlemelerdi (`Islemler.razor` `TakasTuvali`; sonra `KarZararServisi` `AltinMaden`; sonra `DegisimGunSonuTestleri` CS9007). Bunlar bu paketin kırmızısı değil.
- **Sürücünün durdurulması.** İlk sürücü durdurulduğunda `FisKalemGirisi.razor` M5-2 mutasyonunda kalmıştı. Dosya yedekten geri kopyalandı ve `cmp` AYNI çıktı. Ardından listedeki tüm dosyalarda orijinal dizgenin tek kez geçtiği doğrulandı (sonuç `bozuk: []`).
- **Yeniden koşum.** Ağaç derlenir hâle geldikten sonra bu dört mutasyon ve M5-2…M5-4 yeniden koşuldu (`mut-sonuc-2.txt`, `mut-sonuc-3.txt`). Tablodaki sonuçlar bu son koşumlardır.
- **Son derleme.** Son mutasyondan sonra `dotnet build Kuyumcu.sln` sonucu **0 Hata**. 3 uyarının tamamı başka şeridin dosyasında: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` CS8601. Bu paketin uyarısı 0 (`PatchDortTestleri.cs:321` CS8602 giderildi).

## 8. Tam süit ve denetim

`KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-4` (son kod, tek koşum): **Toplam 9823 · Geçti 9818 · Başarısız 3 · Atlandı 2.** Kırmızıların üçü de bu pakete ait değil:

| Kırmızı | Durum | Sahip / kanıt |
|---|---|---|
| `OlcekTekTestleri.OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` | 2 tekrarda da kırmızı | `degisim-gunsonu.css` içindeki `.tuval-tezgah #takas-tuval-fark-bekle` ve `#takas-tuval-odeme` padding bildirimleri (takas tuvali şeridi). Bu paketin CSS'i OZ ölçü özelliği kullanmıyor (`min-width`, `white-space`, `overflow-wrap`) |
| `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` | Tek başına 2/2 yeşil → kararsız | Barkod/Enter yolu, Ekle düğmesine bağlı değil |
| `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_…` | 2 tekrarda da kırmızı | `DateTime.UtcNow` ile "bugün" karşılaştırması yapıyor. Koşum yerel 00:53 (+03), UTC 21:53 saatinde yapıldı, yani gün sınırı farkına düşüyor. Test hurda mutabakatını sınıyor; bu paket o koda dokunmadı. Sahibi: Yasal4/ZO-İŞLEM şeridi |

**Bu paketin yeni kırmızısı: 0.**
`bash dotnet/denetim.sh`: rc=1, **302 ✓**, 2 ✗:
- P-30 DEMO_INSAN_SURESI (istemde hariç).
- `OLCEK-1: YUZ_OLCU_ENVANTER_DISI = 2`: OL26 ile aynı iki `degisim-gunsonu.css` satırı (başka şerit).
Ham çıktı: `ham/PATCH-4-olcum/denetim.txt`.

## 9. Canlı doğrulama (API 5188 Release anlık görüntüsü, bağımsız SQL — kuyumcu_net)

Betik `canli-p4.mjs`, çıktı `canli-p4-kosum.txt`. Kiracı `01M3Z7M6PEKQG87WTDB3PJWE8E` (`PAKETPATCH-4-TEST …`, sistem davetiyle açıldı). **5xx sayısı: 0.**

| Adım | Beklenen | Gerçek | SQL (milyem · has · barkod · ayar · stokAdet · stokHas) |
|---|---|---|---|
| POST ürün `gercekMilyem:920` | 201, yanıtta 920 | 201 · `gercekMilyem=920` | `920.00 9.200000 B-ESKI K22 10.000 92.000000` |
| GET tekil + liste | `gercekMilyem` 920 | 200 · 920 / 920 | — |
| PATCH **ekranın tam gövdesi** (35 alan; barkod değişik, gercekMilyem yok) | 200, milyem/has/stokHas aynı | 200 · yanıtta 920 | önce `920.00 9.200000 B-ESKI … 92.000000` → sonra `920.00 9.200000 B-YENI … 92.000000` |
| PATCH `{ayarId:K22, birim, gram:10, tas:0}` | değişmez | 200 | (DEĞİŞMEDİ) |
| PATCH `{ayarId:K22, gercekMilyem:null}` | standarda döner | 200 · yanıt null | `916.00 9.160000 … 91.600000` |
| PATCH `{gercekMilyem:920}` | geri yazılır | 200 | `920.00 9.200000 … 92.000000` |
| PATCH `{ayarId:K18, gram:10}` | yeni ayarın standardı | 200 | `750.00 7.500000 … K18 … 75.000000` |
| satış-fişi ödeme `tutarTl / dovizTutar / gramAgirlik / birimFiyatTlHasGram` 1e20 | 400 alanlı | 4/4 400 · `odemeler[0].<ad>` · Türkçe ileti | hareket sayısı 1 / 1 |
| rezerv POST ve PATCH bitiş 2000-01-01 | 400 alan:bitisGunu | 400 `[bitisGunu]` ×2 | rezerv `1.000 NULL` önce = sonra |

Not (eski kusur yeniden üretilmedi): hasımın eski gövdesi `{ayarId:'K22', gercekMilyem:null, …}` artık spesifikasyona göre "standarda dön" anlamına geliyor (yukarıdaki 4. satır). Düzeltilen ekran bu gövdeyi **göndermiyor**: değişmeyen milyemde anahtar yok. Bu, bUnit ve Chrome ölçümünde ayrıca kanıtlandı.

## 10. Ekran — gerçek (başsız) Chrome ölçümü

Kullanılan araçlar: `betikler/tur/cdp.mjs` (CDP 9983, profil `/private/tmp/kuyumcu-serit/PATCH-4/profil`), `inceleme-vekili.py` (5189 → 5188, parolasız oturum) ve `sikisma-olc.js`.

**A. Ürün düzenle çekmecesi** (`olc-p4.mjs`, `olcum.jsonl`). Kutu ortaya kaydırıldıktan sonra ölçüldü.

| Rota | Genişlik | Mod | Tema (data-tema/gövde) | `#d-gercek-milyem` | Sayfa K1·K3·K5·Y | Çekmece K1·K3·K5·K9·K10·Y |
|---|---|---|---|---|---|---|
| /urunler → Düzenle | 1280 | BASİT | aydinlik rgb(242,244,247) | **920** | 0·0/0·0·0 | 0·0/0·0·3·0·0 |
| | 375 | BASİT | aydinlik | **920** | 0·0/0·0·0 | 0·0/0·0·4·0·0 |
| | 1280 | BASİT | karanlik rgb(14,18,25) | **920** | 0·0/0·0·0 | 0·0/0·0·3·0·0 |
| | 375 | BASİT | karanlik | **920** | 0·0/0·0·0 | 0·0/0·0·4·0·0 |
| | 1280 / 375 | PRO | aydinlik | **920** / **920** | 0·0/0·0·0 | 3 / 4 (K9) |
| | 1280 / 375 | PRO | karanlik | **920** / **920** | 0·0/0·0·0 | 3 / 4 (K9) |

- **K9 gerekçesi** (PATCH-3 § 11 ile aynı): Sayfa kapsamındaki 6 sayım, çekmece katmanının sayfanın üstüne binmesinden geliyor. Çekmece içindeki 3–4 sayım, kaydırılan içeriğin sabit başlığın ve alt eylem çubuğunun arkasından geçmesinden geliyor (örnekler: "× ⟂ Maden", "Kategori ⟂ İptal"). Çekmece işaretlemesi değişmedi; yalnız `d-ayar`'a `:after` eklendi.
- Dar sütun: sayfa K2 %97–98, çekmece %96–97. TRY 0, gizli olup görünen 0, yatay kaydırma 0.
- **Uçtan uca işlev** (PRO, açık, 1280; gerçek tıklama): çekmece açıldı, gerçek milyem 920 göründü. Yalnız barkod `B-TARAYICI` yapılıp Kaydet'e basıldı, çekmece kapandı. Yeniden açılınca gerçek milyem 920 ve barkod `B-TARAYICI` göründü. **SQL: `920.00 9.200000 B-TARAYICI K22 10.000 92.000000`** (`sql-tarayici.txt`).
- Ayar değişikliği: K18 seçilince kutu boşaldı (`""`), K22'ye dönülünce 920 geri geldi (`ayar-K18-gm-bos.png`).

**B. Kod ve gram boş bırakılarak Kaydet** (PATCH-3 bulgusunun yeniden ölçümü): PRO açık 1280 ve BASİT koyu 375'te ikisi de yıldızlı, "(isteğe bağlı)" ibaresi yok. Kaydet sonrası iki alan da `aria-invalid=true`. Kod altında "Bu alan zorunludur." görünüyor, gram altında "Sıfırdan büyük bir değer girin." yazıyor (ikincisi ilk hatanın altında kaldığı için görüş alanı dışında). Odak `d-kod`'da. Çekmece açık kalıyor, PATCH gitmiyor.

**C. Satış istemci tavanı + toplam** (`olc-p4c.mjs`, `olcum-c.jsonl`, son ileti ile):

| Mod · tema · genişlik | 99.999.999.999.999 ₺ ile Ekle | Red ölçümü K1·K3·K5·K9·Y | 999.999.999.999 ₺ ile Ekle | Kabul ölçümü K3 / PRO genel toplam taşma |
|---|---|---|---|---|
| BASİT açık/koyu 1280 | kalem 0, ileti fiyat altında, aria-invalid | 0·0/0·0·0·0 | eklendi | 0/1 (aşağıda) |
| BASİT açık/koyu 375 | aynı | 0·0/0·0·0·0 | eklendi | 0/1 |
| PRO açık/koyu 1280 | kalem 0, ileti | 0·0/0·0·0·0 | kalem 1, "999.999.999.999 ₺" | 0/0 · `tasma:false` |
| PRO açık/koyu 375 | kalem 0, ileti | 0·**0/0**·0·0·0 (ilk iletide 1/0 idi, düzeltildi) | kalem 1 | 0/0 · `tasma:false` (iki satıra kırılıyor) |

**Read ile görülen görüntüler:**
- `urun__PRO_aydinlik_1280`: Gerçek Milyem 920, ipucu altında.
- `urun__BASIT_karanlik_375`: 920 koyu temada okunuyor.
- `ayar-K18-gm-bos`: 18 Ayar seçili, kutu boş.
- `zorunlu__BASIT_karanlik_375`: kod kırmızı çerçeveli ve yıldızlı, altında ileti.
- `satis-tavan__PRO_aydinlik_375`: ilk iletinin taştığı hâl.
- `satis-buyuk__PRO_aydinlik_1280` ve `satis-buyuk__PRO_karanlik_375`: genel toplam kart içinde kalıyor.
- `t2-satis-buyuk__BASIT_aydinlik_375`.

BASİT kabul ölçümündeki K3 kırık = 1 ("Toplam · 10,00 g" etiketi 12 haneli toplamın yanında iki satıra iniyor) ve 1280'deki K9 = 1 ("Tutar ⟂ Son 3 satış"), BASİT satış görünümünden (`FisVerdimAldim`) geliyor. Yalnız 12 haneli uç tutarda oluşuyor. Bu paketin dosyası değil, açık kalan olarak sahibine yazıldı.

## 11. Mevcut veri taraması (kuyumcu_net, SALT OKUMA — düzeltme yapılmadı)

- `urunler` milyem dağılımı: G925→925 (12), K14→585 (16), K18→750 (15), K22→916 (59), K24→1000 (12). **Standart dışı milyemli ürün yok.**
- Ürün has'ı ile `(gram − taş) × milyem / 1000` arasında fark: **0** ürün.
- ADET ürünlerde `|has| / adet` değeri ürünün has'ından farklı olan hareket (bütün türlerde, 58 hareket tarandı): **0**. Bir ürün geçmişte 920 iken 916'ya dönmüş olsaydı eski hareketlerinde birim has 9,20 kalırdı; böyle bir iz yok.
- `stoklar.toplam_has_gram` ile `adet × has` arasında fark: **0**.
- Denetim kaydında 29 `Urun GUNCELLEME` var, ancak ayrıntı alanı milyem taşımıyor. Bu yüzden önceki milyem bu kaynaktan geri okunamıyor (sınırlama olarak not edildi).
- **Sonuç:** Mevcut veride standarda dönmüş gerçek milyem izi bulunamadı.
- Hasım t2 test ürünü: 2026-10-02'den sonra oluşturulmuş ürün ve barkodu 'Y'/'X' olan ürün **yok**. kuyumcu_net'te kalmış hakem test kaydı görülmedi.

## 12. Temizlik

- API (PID dosyası), vekil ve Chrome (9983) durduruldu. 5188, 5189 ve 9983 portlarında `lsof` → 0. 5199/5200'e dokunulmadı.
- `zsh betikler/kiraci-temizle.sh 01M3Z7M6PEKQG87WTDB3PJWE8E --uygula` → rc 0. tenant_id kolonu olan **75** tabloda bu kiracının toplam satırı **0**.
- tenants başlangıçta **15**, sonda **15**. QA-TEST- kiracılarına dokunulmadı.
- `fikstur.json` ve `sistem.anahtar` dosyaları `printf ''` ile boşaltıldı. Ham çıktılarda parola/anahtar taraması → 0.

## 13. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` (yalnız bu pakete ait olanlar):
- ` M dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — GercekMilyem üç durumlu; "değişti" kuralı; `YanitJson` / `Yanit` / `GercekMilyemi`; controller GET, liste, PATCH ve POST yanıtları
- ` M dotnet/Kuyumcu.Web/Pages/Urunler.razor` — yalnız düzenle çekmecesi: `d-ayar :after`, `dGercekMilyemIlk`/`dAyarIlk`, koşullu `gercekMilyem` anahtarı, `DuzenleAyarDegisti`
- `?? dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs` — 4 Range
- `?? dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` — 2 × `.AlanIle("bitisGunu")`
- `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.PatchDort.cs` (YENİ) — TR+EN ileti ve tavan sabiti
- `?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor` — `EkleTiklandi`, `TavanAsildi`, `TavanMesaji` (id'ler aynı)
- `?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor` — satır içi fiyat tavanı
- `?? dotnet/Kuyumcu.Web/wwwroot/css/tuval-satis.css` — 3 satır (PRO toplam kartı)
- `dotnet/Kuyumcu.Tests/PatchDortTestleri.cs` (YENİ; test dizini izlenmiyor)

Dokunulan sayfalar (tek dokunuş kaydı): `Pages/Urunler.razor`. Bu, PATCH-3'ün aynı çekmecesine ikinci dokunuştur. Gerekçe: veri bozan kusurun istemci yarısı bu dosyada; spesifikasyon da açıkça bu çekmeceyi istiyor. Sayfa dışında dokunulan bileşenler: `FisKalemGirisi.razor`, `FisKalemTablosu.razor`.

## 14. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Öncelik 1: sunucuda "ayar değişti mi" kuralı + GercekMilyem üç durumlu | KARŞILANDI | § 2; M1-1, M1-2, M1-3 |
| GET ve listede gercekMilyem; çekmece kayıtlı değeri gösteriyor | KARŞILANDI | M1-4/5/6, M2-2; Chrome 8/8 "920" |
| Gerileme: tam gövde → milyem 920, has ve stok has DEĞİŞMEZ | KARŞILANDI | HTTP testi + canlı SQL + Chrome uçtan uca SQL |
| Alan taraması tablosu | KARŞILANDI | § 2 tablo + kalıcı kalıp testi |
| Mevcut veri taraması (salt okuma), hasım test kaydı listesi | KARŞILANDI | § 11 (bulgu 0) |
| Kod boş → sessiz etkisizlik | KARŞILANDI (PATCH-3'te kapanmıştı) | Chrome yeniden ölçüm § 10-B |
| Gram boş → etiket çelişkisi | KARŞILANDI (PATCH-3'te kapanmıştı) | § 10-B |
| Ödeme bacağı DTO tavanı, 400 alan:odemeler[i].<ad> | KARŞILANDI | M3-1…4; canlı 4/4 |
| Rezerv geçmiş bitiş günü alanlı | KARŞILANDI (+ oluşturma yolu) | M4-1/2; canlı |
| Satış PRO çok büyük tutar: istemci tavanı + toplam sığdırma | KARŞILANDI (sapma § 1: bileşen dosyaları) | M5-1…4; Chrome 8/8 |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | 19/19 |
| Build 0 hata / bu paketin uyarısı 0 | KARŞILANDI | 0 Hata; 3 uyarı başka şeridin dosyasında |
| denetim TEMİZ (P-30 hariç) | Bu paketin sayacı ✓ | Tek ek ✗ OLCEK-1 envanteri `degisim-gunsonu.css` (başka şerit) |
| Tam süit: 0 yeni kırmızı | KARŞILANDI | 3 kırmızının üçü de başka sahipli ya da kararsız (§ 8) |
| Test kiracısı silindi, tenants başlangıç değerinde | KARŞILANDI | satır 0; 15 → 15 |
| Sır basılmadı | KARŞILANDI | dosyalar boşaltıldı, tarama 0 |
| Migration | Gerekmedi | şema değişmedi |

## 15. Açık kalanlar (sahibiyle)

- **OL26 / denetim OLCEK-1 envanteri:** `degisim-gunsonu.css` içindeki `.tuval-tezgah #takas-tuval-fark-bekle` ve `#takas-tuval-odeme` padding bildirimleri. Sahibi takas tuvali / değişim gün sonu şeridi.
- **Yasal4 `Mutabakat_bugun_…`:** UTC ile yerel gün sınırına bağlı kırmızı (yerel 00:00–03:00 aralığında). Sahibi Yasal4/ZO-İŞLEM. Önerilen düzeltme: testte "bugün" `Zaman.YerelBugun()` ile kurulsun.
- **SATIS3 `Barkod_dizisi_…`:** kararsız (tam süitte bir kez kırmızı, tek başına 2/2 yeşil). Sahibi SATIS-3.
- **BASİT satış görünümü** (`FisVerdimAldim`): 12 haneli uç tutarda "Toplam · 10,00 g" iki satıra iniyor ve 1280'de "Tutar" ile "Son 3 satış" üst üste biniyor. PRO kalem tablosunun tutar hücresi 10 haneden uzun tutarı tasarım gereği elipsle kesiyor (sütun 7 haneye göre ölçülmüş, `olcek.css § 10b`); tam değer genel toplamda görünüyor. Sahibi İşlemler/SATIŞ şeridi.
- **Range iletisinde ham etiket:** "Tutar TL …" ve "Doviz tutar …" görünüyor. `DogrulamaSozlugu`'nda `TutarTl` ve `DovizTutar` için Türkçe ad yok. Dosya alanı dışında; sahibi doğrulama sözlüğü (PATCH-3 kalıbıyla iki satır).
- **tasKokeni normalleşmesi:** Taş 0 olan eski kayıtta NULL değer `TAS_YOK` olur (§ 2 tablo). Anlam değişmiyor; ürün kararı gerekirse ekran yalnız değişince göndersin.
- **6 hane ToEven çağrıları** (AcilisYazici 147/179/223/278, MaliyetMotoru 65/77/164 …): dosya alanı dışında; orkestratör kuyruğunda (PATCH-3 § 14).
- **Hasımın eski gövdesi:** `{gercekMilyem:null}` artık bilinçli "standarda dön" anlamı taşıyor (spesifikasyon kararı). Bu gövdeyi gönderen eski istemci sürümü varsa standarda döner. Kuyumcu.Web bu sürümle göndermiyor.

## 16. Kurucu / mali müşavir

Mevzuat hükmü içeren değişiklik yok. Milyem ve has hesabının formülü değişmedi; yalnız gereksiz yeniden hesaplama ve sessiz standarda dönüş kaldırıldı. Eşik, oran ya da vergi kuralı eklenmedi. Mevcut veride düzeltme gerektiren kayıt bulunmadı (§ 11), bu yüzden kurucu kararı gerekmiyor.
