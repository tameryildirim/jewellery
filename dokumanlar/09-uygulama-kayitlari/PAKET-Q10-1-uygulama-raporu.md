# PAKET Q10-1 — Gider toplamı storno sonrası NET + basit modda "STORNO" jargonu — uygulama raporu

**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** DEMO DÜZELTME Q10-A · **Tarih:** 2026-09-24
**Ortam:** test DB `kuyumcu_test_l` · canlı API 5174 · vekil 5173 · başsız Chrome CDP 9351 · tarayıcı bölmesinde kendi sekmem (tab-49, iş sonunda kapatıldı)
**Test kiracısı:** `PAKETQ10-1-TEST Kuyumcu` (`01M39WM75113312RGQ1ZEDX4M0`) — `/sistem/davetler` + `/auth/kayit` ile açıldı, iş sonunda silindi (332 satır + tenants + 2 davet satırı).
**Durum beyanı:** BİTTİ. Yeniden koşum (r2, 24.09 20:09–20:40) aşağıda; o koşumda tam süit **5403/5403 yeşil** ve `denetim.sh` **TEMİZ** çıktı.

---

## 0. Yeniden koşum (r2) — önceki doğrulamada kalan bulguların kapatılması

**0.1 [ORTA] Ağaçta kalan doğrulayıcı mutasyonu.** Mutasyonun geri alındığını bağımsız olarak doğruladım:
- `GiderModulu.cs:359` şu an `g.Sum(x => -x.Tutar)`. `Math.Abs` yok.
- `cmp`: `mut-yedek/Q10-1/GiderModulu.cs` ile ağaçtaki dosya AYNI. `dogrulama-yedek/pQ10-1/GiderModulu.cs` ile ağaçtaki dosya AYNI. `RaporModulu.cs` için de iki yedek AYNI.
- Bu durumdaki ağaçtan temiz derleme aldım: `dotnet build Kuyumcu.sln` → 0 hata, 0 uyarı.

Orkestratöre not: 18:19–19:45 arasında bu ağaçtan alınan derlemeler hatalı `Math.Abs` koduyla çalışmış olabilir (5199 Release anlık görüntüsü dahil). O aralıkta yayın alındıysa yeniden alınmalı.

**0.2 [KRİTİK] Tarayıcı ölçüm çelişkisi (375 px ekran görüntüleri).** 8 kombinasyonu baştan, yeni bir test kiracısıyla ölçtüm.
- Kiracı: `PAKETQ10-1-TEST Kuyumcu` (`01M3A6J8J9WW08NKZEV6A70YFH`). API 5174'te, vekil 5173'te çalıştı.
- Araç: gerçek Google Chrome (`--headless=new`, CDP 9351). Sayaçlar `sikisma-olc.js` / `__olcTam` ile alındı.
- Her kombinasyonun PNG'si alındı ve **tek tek gözle açılıp kontrol edildi**.
- 429 sayısı 0, 5xx sayısı 0, konsol istisnası 0.

| Rota | Genişlik | Mod | Tema | `#gd-toplam` (ölçülen) | Storno satırı açıklaması | K1 | K3 taşan/kırık | K5 TRY | K8 jargon | K9 | yatay | K2 % | PNG |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /gider | 1280 | BASİT | açık | Toplam gider: 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | `_gider-1280-BASIT-aydinlik.png` |
| /gider | 375 | BASİT | açık | Toplam gider: 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | `_gider-375-BASIT-aydinlik.png` |
| /gider | 1280 | PRO | açık | Toplam gider: 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | `_gider-1280-PROFESYONEL-aydinlik.png` |
| /gider | 375 | PRO | açık | Toplam gider: 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | `_gider-375-PROFESYONEL-aydinlik.png` |
| /gider | 1280 | BASİT | koyu | Toplam gider: 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | `_gider-1280-BASIT-karanlik.png` |
| /gider | 375 | BASİT | koyu | Toplam gider: 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | `_gider-375-BASIT-karanlik.png` |
| /gider | 1280 | PRO | koyu | Toplam gider: 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | `_gider-1280-PROFESYONEL-karanlik.png` |
| /gider | 375 | PRO | koyu | Toplam gider: 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | `_gider-375-PROFESYONEL-karanlik.png` |

- **Görüntüler:** `scratchpad/q101-ekran/r2-B-kismi/`. Senaryo: 3 gider (100 / 200,25 / 50), 200,25 storno edildi. Önceki 125,50 çifti de listede duruyor, beklenen toplam 150.
- **Tam storno senaryosu** (125,50 + storno): `scratchpad/q101-ekran/r2-A-tamstorno/`. 8 kombinasyonun hepsinde `Toplam gider: 0 ₺` ölçüldü. Açık temadaki 4 kombinasyonun PNG'lerinde satırlar ve `0 ₺` görünüyor. Koyu temada BASİT modu açan PATCH isteği 429 aldı; o iki koşuda satırlar boş geldi, ama toplam yine `0 ₺` ölçüldü. Bu yüzden tam storno kabulü açık tema PNG'lerine ve API'ye dayanıyor, koyu tema kabulü ise r2-B'ye.
- **375 px görüntüleri görsel olarak doğrulandı (4/4):**
  - "Giderler" başlığının yanında `Toplam gider: 150 ₺` okunuyor.
  - BASİT modda satırlar `Geri alındı: …`, PRO modda `STORNO: …` gösteriyor.
  - Tablo kendi sarmalayıcısı içinde yatay kayıyor, altında ipucu metni var. Sayfa düzeyinde yatay kaydırma 0.
- **Tarayıcı bölmesi:** kendi sekmemi açamadım. `tabs_create` iki denemede de "tab cap reached" döndü; bölmedeki 9 sekmenin hepsi başka şeritlere ait ve kural gereği onlara dokunmadım. Bu yüzden r2'de bölme görüntüsü yok. Kanıt, aynı Chrome motorunda alınmış 16 PNG ile ölçüm `sonuc.json` dosyaları.
- **Çelişkinin kapanışı:** 375 px'teki dört kombinasyonun hepsi ekran görüntüsüyle kanıtlı. "Yalnız JS ölçümüyle kanıtlı" kombinasyon kalmadı. Rapordaki 8/8 iddiası r2 görüntüleriyle yeniden üretildi.

**0.3 Canlı (5174, r2), yanıtlar okundu:**

| Adım | HTTP | Sonuç |
|---|---|---|
| POST /kasalar, POST /kasalar/{k}/hareket (GIRIS 10.000), POST /gider-kategorileri, POST /giderler 125,50 | 200 ×4 | — |
| POST …/storno | 200 | `kasaBakiyesi: 10000.0` |
| GET /giderler | 200 | `toplamlar [{TRY, 0.0, adet 2}]` |
| 3 gider (100 / 200,25 / 50) + 200,25 storno | 200 | storno yanıtı `kasaBakiyesi: 9850.0` |
| GET /giderler | 200 | `toplamlar [{TRY, 150.0, adet 6}]` |

SQL (`kasa_hareketleri`, kiracı):
- Satırlar: `10000.00 · -125.50 · +125.50 (STORNO) · -100.00 · -200.25 · -50.00 · +200.25 (STORNO)`
- `SUM(-tutar) WHERE gider_kategori_id IS NOT NULL` = **150.00**
- `kasalar.bakiye` = **9850.00**

**0.4 Test ve denetim (r2):**
- Derleme: 0 hata, 0 uyarı.
- Filtreli koşum (`Q101|GiderEkranTestleri|P19|P25OlcekTestleri|RaporSetiTestleri|P69`): **134/134 geçti**.
- Tam süit (`trx-pQ10-1-r2`): **5403 test, 5403 geçti, 0 başarısız**. Q101 test adlarının 38'i geçti (teori durumları dahil).
- `denetim.sh`: **DENETİM TEMİZ** (288 ✓).
- Önceki koşudaki 12 kırmızı ve 5 ✗ bu koşuda kapanmış: başka şeritler kendi işlerini tamamlamış.

**0.5 Temizlik:**
- API ve vekili kendi PID'lerimle durdurdum. 5174 ve 5173 portlarında dinleyen yok.
- Kiracı silindi: `tenant_id` kolonu olan tüm tablolarda kalan satır 0. `tenants` satırı ve 1 `kiraci_davetleri` satırı da silindi.
- `tenants` sayısı: başlangıçta 14, sonda 14. Arada başka şeritler kendi kiracılarını açıp kapadı.
- Tek kullanımlık sistem anahtarı, fikstür ve jeton dosyalarını `printf ''` ile boşalttım.
- Bu koşumda API açılışında `is_calismalari` benzersiz anahtar çakışması (`MASAK_SIB_SURESI`) günlüğe düştü. Sebebi aynı gün birden çok API örneğinin arka plan işini çalıştırması; bu paketle ilgisi yok.

**0.6 [DÜŞÜK] Dönemler arası storno:** `StornoSuzgeci` tarih süzgeci taşımıyor. Bu yüzden bir dönemdeki kaynağın stornosu sonraki dönemde yapılırsa kaynak dönemin raporu geriye dönük değişir. Bu, paket 48'in mevcut kalıbıdır; bu paket yeni bir karar eklemedi. Kapanmış (kilitli) dönemlerde bu davranış **mali müşavir teyidi gerektirir**. `/giderler` toplamı işaretli olduğu için dönemsel netliği korur.

---

## 1. Bulgu önce yeniden üretildi (ölçüm, kod yazmadan önce)

| Kanıt | Ölçülen |
|---|---|
| API `GET /giderler` (125,50 gider + storno sonrası) | gider satırı `tutar: 125.50, stornolandi: true`, storno satırı `tutar: -125.50, stornoSatiri: true, aciklama: "STORNO: …"` |
| Kod (`Gider.razor:487`) | `giderler.Sum(g => g.StornoSatiri ? -g.Tutar : g.Tutar)` → sunucu storno satırını **zaten eksi** döndürüyor; eksi × eksi = artı |
| Tarayıcı (başsız Chrome, 1280 + 375, BASİT + PRO, açık tema) | `Toplam gider: ₺251,00` (4/4 kombinasyon) · BASİT modda `K8_jargon = 2` (`STORNO:` öneki + `Stornolandı` rozeti) |
| Kasa bakiyesi | 10.000,00 → 10.000,00 (doğruydu; değişmedi) |

**Kök neden:** hesap **istemcideydi**; sunucu işaretli tutar döndürüyor, istemci işareti ikinci kez çeviriyordu. Ek olarak toplam yalnız ekrana gelen ilk 100 satırı topluyor ve para birimlerini karıştırıyordu (TRY + USD tek rakam).

Önce ekran görüntüleri: `scratchpad/q101-ekran/once/*.png` (koyu tema koşuları 429 oran sınırına takıldı, veri gelmedi — geçersiz sayıldı; açık tema 4 kombinasyon geçerli).

## 2. Yapılanlar

### 2.1 /gider toplamı (KRİTİK)
- **Sunucu** (`Modules/Gider/GiderModulu.cs`): `GET /giderler` yanıtına `toplamlar: [{paraBirimi, tutar, adet}]` eklendi. Aynı süzgeçle, **satır sınırından bağımsız**, **para birimi başına**, **işaretli** (`SUM(-tutar)`) hesaplanır: storno satırı ters işaretli olduğu için 125,50 + storno = 0,00. `GiderOlcumu` (kâr-zarar) ile aynı kural.
- **Sözleşme** (`Contracts/Sayfalar/GiderSozlesmeleri.cs`): `GiderToplamVeri` + `GiderYaniti.Toplamlar` (varsayılanı null).
- **Ekran** (`Pages/Gider.razor`): istemci hesabı kaldırıldı; `#gd-toplam` sunucu toplamını gösterir (çok para biriminde `150 ₺ · 15,75 $`). Sunucu toplam döndürmezse rakam uydurulmaz, `—` yazılır.

### 2.2 Basit modda jargon (D10)
- Yeni `Kuyumcu.Core/I18n/Sozlukler.StornoOneki.cs` (hub'a dokunulmadı): `StornoOnekiSozlugu.Goster(aciklama, dil, basit)` fonksiyonu basit modda baştaki `STORNO:` önekini `Geri alındı:` (EN `Undone:`) olarak gösterir. Eşleşme büyük/küçük harfe duyarsızdır. "Geri alındı" metni KAT-1'in `KatalogSozlugu.GeriAlindi` kaydından okunur (tek kaynak). Defterdeki veri değişmez.
- Basit modda `Stornolandı` rozeti → `Geri alındı`. Değişmezlik notu ("… STORNO + yeni kayıttır") → yeni TR+EN basit metin. PRO modda üçü de olduğu gibi kalır.
- **Tarayıcıda bulunan ek kusur:** mod düğmesine basınca /gider gövdesi yenilenmiyordu (tablo eski modda kalıyordu). Sayfa artık `Dil.Degisti` ve `Modu.Degisti` olaylarına abone (`IDisposable`). Kendi sekmemde ölçüldü: BASİT → PRO → BASİT geçişi sayfa yenilenmeden doğru metni gösteriyor.

### 2.3 Storno geçen diğer özetlerin taraması (aynı kalıp → aynı pakette düzeltildi)
`StornoSuzgeci` (paket 48 Faz A, çift dışlama, Mehmet koşulu) uygulandı; `kasa_hareketleri` için simetrik `StornoCiftiniDisla` aşırı yüklemesi eklendi (`Modules/Satis/StornoSuzgeci.cs`).

| Ekran / özet | Kaynak | Ölçülen (önce) | Beklenen | Sonra | Karar |
|---|---|---|---|---|---|
| /gider "Toplam gider" | Gider.razor (istemci) | 251,00 ₺ | 0,00 | **0** (tam) · **150** (kısmi: 100+200,25+50, 200,25 storno) | Düzeltildi (sunucuya taşındı) |
| Aylık özet "cari tahsilatı" (`cariTahsilatiTl`) | RaporModulu.AylikOzet `Σ\|tl\|` | 1.500 tahsilat + storno + 400 → **3.400** (test) | 400 | 400 | Düzeltildi |
| Aylık özet "tahsilat — kasaya giren TL" | RaporModulu.AylikOzet `tutar>0` | canlı: 10.000 giriş + 2 gider stornosu → **10.325,75** (gider stornosu tahsilat sayılıyordu) | 10.000 | **10.000** (canlı) | Düzeltildi |
| Gün sonu / POS-fatura mutabakatı "tahsilat" | RaporModulu.PosFaturaMutabakati `tutar>0` | aynı kalıp | 10.000 | **nakit 10.000** (canlı) | Düzeltildi |
| Gider kategorisi toplamı | GiderModulu.KategoriListele `Σ(-tutar)` | işaretli | 150 | 150 (canlı) | Zaten doğru, dokunulmadı |
| Kâr-zarar gider | GiderOlcumu `Σ(-tutar)` | işaretli | 150 | 150 (canlı `giderTl`) | Zaten doğru |
| Kasalar bakiye / TL toplam | sunucu `bakiye` alanı | 9.850 | 9.850 | 9.850 (SQL) | Zaten doğru |
| Emanet "elimizdeki has" | sunucu `mevcutHasGramBakiye` (işaretli) | — | — | kod okuması | Zaten doğru |
| Banka toplamı | hesap bakiyeleri toplamı (sunucu) | — | — | kod okuması | Zaten doğru; **Banka\* Q10-B alanı, dokunulmadı** |
| Kasa defteri (R11) giriş/çıkış sütunları | FinansRaporlari `tutar>0` / `tutar<0` | brüt akış | brüt | — | Doğru: defter brüt akış gösterir, ters kayıt kendi sütununda görünür, net ve devir doğru |
| Hurda | — | — | — | — | Storno yok (bilinçli tasarım) |
| Satış analizi / KarZarar "geri alınan" | `Σ\|tl\|` stornoSatirlari | ayrı gösterim | — | — | Doğru: storno ciroya katılmaz, ayrıca sayılır. **KarZarar Q10-B alanı** |
| Vezne | fiş toplamı (satır) | — | — | — | Storno yolu yok. **Q10-B alanı**, dokunulmadı |

## 3. Sapmalar ve gerekçeleri
1. **Toplam sunucuya taşındı.** Spesifikasyon "hesap istemcideyse istemcide düzelt" diyor, ama UX-S21 "sunucu kaynak" diyor. İstemcide işareti düzeltmek iki kusuru açık bırakırdı: 100 satır sınırı ve para birimi karışması. Sunucu toplamı ikisini de kapatıyor. Sözleşme yalnız ekleme yapıyor (eski istemciyle uyumlu).
2. **Rapor özetleri düzeltildi.** Kapsam "aynı kalıp varsa aynı pakette düzelt" diyordu. `RaporModulu` Q10-B'nin dosya listesinde yok. Aylık özette dönem sorgusuna storno çifti süzgeci eklendi. İşaretli toplamlar (satış, net has) değişmez, çünkü çift net sıfırdır ve SATIS/ALIS/HURDA zaten stornolanamaz. Değişenler: mutlak toplamlar ve stornolanabilir türlerin adetleri. **Anlam notu:** bir aydaki kaynağın stornosu sonraki ayda yapılırsa kaynak geriye dönük olarak dışlanır. Bu, `StornoSuzgeci`'nin mevcut anlamıdır (hükümsüz işlem hiç yapılmamış sayılır).
3. **Mod geçiş aboneliği:** spesifikasyonda yoktu, gerçek tarayıcı ölçümünde bulundu. Aynı sayfada küçük bir düzeltme.

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Testler
Yeni: **24 test** (3 dosya), hepsi yeşil.
- `Q101GiderToplamTestleri` (HTTP + MySQL, 6): tam storno 0,00 ve kasa bakiyesinin geri dönmesi · kısmi storno 150,00 · limit=1 iken toplam 60 · TRY/USD ayrı · kategori süzgeci · boş liste = boş dizi.
- `Q101GiderToplamEkranTestleri` (bUnit + saf, 15): net sıfır · kısmi 150 · para birimleri ayrı · toplam gelmezse "—" · BASİT modda `Geri alındı:` ve gövdede "STORNO" yok · PRO modda korunur · mod geçişi yeniden çizer · `Goster` teorisi (7 durum) + null/boş.
- `Q101RaporStornoTestleri` (servis + MySQL, 3): aylık cari tahsilatı · aylık kasa girişi · gün sonu nakit tahsilatı.

Filtreli koşumlar: `Q101|GiderEkranTestleri|P19` → **78/78 geçti** · `Q101|P25OlcekTestleri|RaporSetiTestleri|P69` → **65/65 geçti**.

**Tam süit** (`trx-pQ10-1`): **5331 test · 5319 geçti · 12 başarısız.** 23 Q101 testi tam süitte geçti. 24. test (mod geçişi) tam süitten sonra eklendi ve filtreli koşumda geçti. Kırmızıların hiçbiri bu paketin dosyalarına ait değil (bkz. § 10).

## 6. Mutasyon kanıtı (12/12 kırmızı, hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| MQ101-1 | sunucu toplamı `Sum(-Tutar)` → `Sum(Math.Abs(Tutar))` | Q101_gider_ve_stornosu_toplamda_NET_SIFIR…, Q101_kismi_storno…, Q101_para_birimleri_ayri… |
| MQ101-2 | ekran `giderToplamlari = giderYanit.Toplamlar` → `[]` | Q101_ekran_kismi_stornoda…150, Q101_ekran_para_birimlerini_ayri_yazar, Q101_ekran_toplam_gelmezse_rakam_uydurmaz |
| MQ101-3 | `Goster`: `!basit` → `basit` | Q101_basit_modda_STORNO…, Q101_profesyonel_modda…, Q101_storno_oneki_gosterimi |
| MQ101-4 | toplam sorgusu `Take(limit)` ile kırpıldı | Q101_toplam_satir_sinirindan_BAGIMSIZDIR |
| MQ101-5 | gruplama para biriminden koparıldı | Q101_para_birimleri_ayri_toplanir_KARISMAZ |
| MQ101-6 | basit `Stornolandı` rozeti geri alındı | Q101_basit_modda_STORNO_oneki_Geri_alindi_olur |
| MQ101-7 | basit değişmezlik notu geri alındı | Q101_basit_modda_STORNO_oneki_Geri_alindi_olur (*) |
| MQ101-8 | açıklama `Goster` çağrısı kaldırıldı | Q101_basit_modda_STORNO_oneki_Geri_alindi_olur (*) |
| MQ101-9 | aylık dönem sorgusundan süzgeç kaldırıldı | Q101_aylik_cari_tahsilati_storno_ciftini_saymaz |
| MQ101-10 | aylık kasa girişinden süzgeç kaldırıldı | Q101_aylik_kasa_girisi_storno_ciftlerini_saymaz |
| MQ101-11 | gün sonu kasa girişinden süzgeç kaldırıldı | Q101_gun_sonu_tahsilati_storno_ciftlerini_saymaz |
| MQ101-12 | `Modu.Degisti += Yenile` kaldırıldı | Q101_mod_gecisi_tabloyu_yeniden_cizer |

(*) MQ101-7/8 koşusunda 3 ekran testi daha kırmızıydı. Sebep bu mutasyon değildi: aynı anda başka bir şerit `Bicim` para biçimini değiştirdi (`150,00 ₺` → `150 ₺`, NBSP). Testler bu değişiklikten bağımsız hâle getirildi (`N(Bicim.Tl(..))`) ve yeniden yeşil. MQ101-9/10'un ilk koşusu Q10-B'nin `Panel.razor` derleme hatasına denk geldi; ağaç derlenince yeniden koşuldu (tablodaki sonuç ikinci koşunundur). Son mutasyondan sonra temiz `dotnet build`: **0 hata**. Tek uyarı MSB3101 (obj önbellek yazma çakışması, paralel şerit derlemesi, kod uyarısı değil). Ara derlemelerdeki xUnit1031 (EtiketEkranTestleri) ve RZ10012 (Islemler) uyarıları başka şeritlerin dosyalarına ait.

## 7. denetim.sh
`DENETİM BAŞARISIZ`: 283 ✓ · 5 ✗. **5 ✗'nin hiçbiri bu pakete ait değil:**
- `Pages+Bilesenler+Layout record tanımı 6`, `P-10 BU_PAKETTE_EKLENEN_RECORD +3`, `P-02 OLCUM_TABANI_KOPYA` → Panel.razor `KarOzeti/BankaHesapOzeti/BankaListesi` (Q10-B)
- `P-39 TABLO_SARMALAYICISIZ 3` → FisKalemTablosu/FisVerdimAldim (Q10-B, Bilesenler/Fis)
- `B-3 ESIK_ISTEMCIDE 5` → Bilesenler/Fis/FisNakitUyari.razor "30.000 ₺" (Q10-B)

Bu paketin sayaçları ✓: SABIT_RENK 0, MOBIL_TABLO_SARMALAYICISIZ 0, ESSIZ_BACAK 0, STORNO/ModalPencere/Hareketler.Add sayaçları değişmedi. Yeni razor record yok (yeni tipler Contracts'ta), inline style yok, yeni CSS yok, app.css'e dokunulmadı.

## 8. Canlı doğrulama (5174, yanıtlar okundu)
| Adım | HTTP | Sonuç |
|---|---|---|
| POST /giderler 125,50 | 200 | id döndü |
| POST /kasalar/{k}/hareket/{id}/storno | 200 | `kasaBakiyesi 10000.00` |
| GET /giderler (düzeltmeden sonra) | 200 | `toplamlar [{TRY, 0.0, adet 2}]` |
| 3 gider (100 / 200,25 / 50) + 200,25 storno | 200 | `toplamlar [{TRY, 150.0, adet 6}]` |
| GET /gider-kategorileri | 200 | Kira: 6 adet · 150,0 |
| GET /raporlar/aylik | 200 | `tahsilat.toplamTl 10000.0, cariTahsilatiTl 0` (eski kod 10.325,75 verirdi) |
| GET /raporlar/pos-fatura-mutabakati | 200 | `tahsilat.nakit 10000.0` |
| GET /raporlar/kar-zarar | 200 | `giderTl 150.0` |

SQL (`kasa_hareketleri`, kiracı): `10000.00 · -125.50 · +125.50(storno) · -100 · -200.25 · -50 · +200.25(storno)` → `SUM(-tutar) gider = 150.00` · `kasalar.bakiye = 9850.00`.

## 9. Gerçek tarayıcı ölçümü (sonra) — `sikisma-olc.js` (`__olcTam`), başsız Chrome + kendi sekmem
| Rota | Genişlik | Mod | Tema | Toplam | Storno açıklaması | K1 | K3 taşan/kırık | K5 TRY | K6 | K7 | K8 jargon | K9 | K10 | K11 | Yatay | K2 kart % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /gider | 1280 | BASİT | açık | 150,00 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0/1 | 0 | **0** | 0 | 0 | 0 | 0 | 98 |
| /gider | 375 | BASİT | açık | 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0/1 | 0 | **0** | 0 | 0 | 0 | 0 | 97 |
| /gider | 1280 | PRO | açık | 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0/1 | 0 | — | 0 | 0 | 0 | 0 | 98 |
| /gider | 375 | PRO | açık | 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0/1 | 0 | — | 0 | 0 | 0 | 0 | 97 |
| /gider | 1280 | BASİT | koyu | 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0/1 | 0 | **0** | 0 | 0 | 0 | 0 | 98 |
| /gider | 375 | BASİT | koyu | 150 ₺ | Geri alındı: … | 0 | 0/0 | 0 | 0/1 | 0 | **0** | 0 | 0 | 0 | 0 | 97 |
| /gider | 1280 | PRO | koyu | 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0/1 | 0 | — | 0 | 0 | 0 | 0 | 98 |
| /gider | 375 | PRO | koyu | 150 ₺ | STORNO: … | 0 | 0/0 | 0 | 0/1 | 0 | — | 0 | 0 | 0 | 0 | 97 |

Tüm kombinasyonlar: 429 sayısı 0, 5xx sayısı 0, konsol istisnası 0. K4 boyKat 1,5–1,8, tek bölüm. Önce: 1280/375 BASİT/PRO açık → `₺251,00`, BASİT `K8_jargon 2`. İlk satırdaki `150,00 ₺` ile diğerlerindeki `150 ₺` farkının sebebi: başka bir şeridin `Bicim` değişikliği koşu sırasında diske düştü. Bu paketin konusu değil.
Kendi sekmemde (tab-49): 1280 PRO `Toplam gider: 150 ₺`, yatay 0, gizli-görünen 0, TRY 0. BASİT → PRO → BASİT geçişi yenilemesiz doğru. 375 BASİT jargon 0, yatay 0.
Ekran görüntüleri: `scratchpad/q101-ekran/once/*.png` (önce) · `scratchpad/q101-ekran/sonra/*.png` (8 kombinasyon) + ölçüm `sonuc.json`.

## 10. Açık kalanlar (sahibiyle)
- *(r2 notu: bu madde ile bir sonraki madde ilk koşuya aittir. r2'de tam süit 5403/5403 yeşil, denetim.sh TEMİZ; bkz. § 0.4.)*
- **Tam süitteki 12 kırmızı (ilk koşu), Q10-B şeridi:** BasitModPosTestleri ×2, S22TezgahFisiTestleri ×2 (Pos/Islemler ızgarası) · S23FisMusteriEkranTestleri.S23E_12 (`#islem-tur-ALIS`) · P39TasarimKokleriTestleri.K5 (FisKalemTablosu/FisVerdimAldim) · OlcumKapisiTestleri ×2, SozlesmeKapsamTestleri ×2, SozlesmeYansimaTestleri ×2 (Panel.razor `KarOzeti/BankaListesi/BankaHesapOzeti` iç record'ları).
- **denetim.sh'taki 5 ✗, Q10-B şeridi** (§ 7).
- **Jargon, bu paketin kapsamı dışında:** `STORNO:` açıklama öneki /kasalar hareket geçmişinde ve /emanet açıklamasında da basit modda görünebilir. `StornoOnekiSozlugu.Goster` yeniden kullanılabilir durumda, ama o sayfalar bu paketin dosya alanında değil. Önerilen sahip: sonraki K8 turu.
- **Sayfa gövdelerinin mod geçişine abone olmaması** (Gider'de kapatıldı) başka sayfalarda da olabilir. Aynı kalıp Cariler/Urunler/Islemler/Ayarlar'da var, diğer sayfalar ölçülmedi.
- **Tenant sayısı:** başlangıç 10, son 14. Bu paketin kiracısı silindi (0 satır). Artan 4 kiracı başka şeritlerin test kiracıları: PAKETQ10-4-TEST, PAKETT-S1-TEST, PAKETQ10-5-TEST, PAKETT-P1-TEST. Bu paket onlara dokunmadı.

## 11. DoD sözleşmesi
Yeni varlık ya da kayıt türü açılmadı. `GiderParaToplami` / `GiderToplamVeri` salt okunur, türetilmiş bir özettir: kaydedilmez, düzenlenemez. Kaynağı gider satırlarıdır ve gider satırının düzeltme yolu değişmedi: "Geri al (düzeltme fişi)", yani storno + yeni kayıt, `DegismezIpucu` gerekçesiyle. Finansal hard DELETE yok. Tek yazma yolu atlanmadı (kod yalnız okuma sorgularına dokundu).

## 12. Değişen dosyalar
- `dotnet/Kuyumcu.Api/Modules/Gider/GiderModulu.cs` (Listele: `toplamlar`, `GiderParaToplami`)
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` (aylık dönem sorgusu + aylık/gün sonu kasa girişlerine storno çifti süzgeci)
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoSuzgeci.cs` (KasaHareket aşırı yüklemesi)
- `dotnet/Kuyumcu.Contracts/Sayfalar/GiderSozlesmeleri.cs` (`GiderToplamVeri`, `GiderYaniti.Toplamlar`)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.StornoOneki.cs` (YENİ)
- `dotnet/Kuyumcu.Web/Pages/Gider.razor` (toplam, açıklama öneki, basit rozet ve notu, mod aboneliği; DOM id'leri ve rota değişmedi)
- `dotnet/Kuyumcu.Tests/Q101GiderToplamTestleri.cs`, `Q101GiderToplamEkranTestleri.cs`, `Q101RaporStornoTestleri.cs` (YENİ)

Dokunulan sayfalar: `Pages/Gider.razor` (tek sayfa).

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Bulgu önce kendi ölçümle üretildi | Karşılandı | § 1 (API + tarayıcı 251,00) |
| 125,50 + storno → 0,00 | Karşılandı | HTTP testi + canlı `toplamlar 0.0` |
| Kısmi senaryo (3 gider, 1 storno) | Karşılandı | test 150,00 + canlı ve tarayıcıda 150 |
| Tüm stornolu özetler tarandı | Karşılandı | § 2.3 tablo |
| Basit modda STORNO → Geri alındı, PRO'da kalır | Karşılandı | bUnit + tarayıcı K8 = 0 (4/4 BASİT) |
| Kasa bakiyesi değişmez | Karşılandı | SQL 9850 = 10000 − 150 |
| Finansal sınır testleri | Karşılandı | limit, para birimi, süzgeç, boş, kısmi |
| Gerçek tarayıcı 1280+375 × BASİT+PRO × açık+koyu | Karşılandı | § 9 (8/8) |
| DOM id/rota değişmez, sabit renk/inline style yok, hub'a dokunulmadı, app.css'e dokunulmadı | Karşılandı | diff + denetim ✓ |
| Mutasyon | Karşılandı | 12/12 kırmızı, cmp AYNI |
| Paket yeni kırmızı üretmedi | Karşılandı | 12 kırmızının tamamı Q10-B dosyalarında |
| Test kiracısı silindi | Karşılandı | kiracı 0 satır, davet 0 |
| 5199/5177/5178 portlarına dokunulmadı; 5174/5173/9351 kapatıldı | Karşılandı | `lsof` boş |

**Mevzuat:** bu paket mevzuat hükmü içermez (gösterim ve toplama doğruluğu). Kurucu ya da mali müşavir teyidi gerektiren bir nokta yok.
