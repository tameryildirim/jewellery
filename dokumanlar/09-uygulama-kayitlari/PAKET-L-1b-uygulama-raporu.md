# PAKET L-1b — TEVSİK AÇIĞI (ALIŞ): çok kalemli ALIŞ TEK FİŞ — uygulama raporu

- Tarih: 2026-09-25 (devralma 09:28, bitiş ~10:05)
- Roller: Taşkın (backend) + Ertuğ (frontend) · Opus uygulayıcı
- Dalga: CUMA S · izolasyon: test DB `kuyumcu_test_t2`, canlı API 5162, vekil 5163
- Ciddiyet: KRİTİK (yasal — 459/575 s. VUK GT tevsik haddi)

## 0. Devralma (ilk uygulayıcı ENOTFOUND ile kesilmişti)

| Kontrol | Sonuç |
|---|---|
| Ağaçtaki yarım iş | Sunucu (AlisFisi + ortak yardımcılar), istemci (AlisFisUcunaGider dalı), 3 test dosyası YAZILMIŞ; rapor, tam süit, denetim, tarayıcı ölçümü YOK |
| Yarım mutasyon | `mut-yedek/L-1b/M-L1b-1..5.orj` (önceki oturumun scratchpad'i) ağaçtaki dosyalarla `cmp` → 5/5 BİREBİR; ağaçta mutasyon kalmamış |
| Kalan canlı süreç | 5162/5163 dinleyen süreç yoktu |
| Kalan test kiracısı | `PAKETL-1b-TEST canli` (01M3BBSH…) kuyumcu_net'te bulundu → 70 tablodan silindi, kalan satır 0 |
| Yaklaşım | Sıfırdan yazılmadı; kod koda karşı okundu, tüm kanıtlar (mutasyonlar dahil) bu oturumda YENİDEN üretildi |

## 1. ÖNCE ÖLÇ (kod yazmadan önceki ölçümün kaydı)

- `/islemler/alis` (tek kalem) kapı sırası: `MASAK DogrulaKumulatif(toplamTl)` → `OdemeCoz` (kasa para birimi TRY + gün kilidi) → `VadeKapisi` → **tevsik**: `odenen > 0 && OdemeTuru == "NAKIT"` ise `tevsik.Dogrula(nakit = odenen, islemToplamTutari = toplamTl)` — yön: TEDİYE (kasadan çıkan nakit).
- Peşin Müşteri (sistem carisi) iki kapıda da fail-closed: MASAK kapısı `tutar > tevsik.Had` ise, tevsik kapısı `max(nakit, işlem toplamı) > Had` ise **ödeme türünden bağımsız** 400 (`alanlar.cariId`).
- Gerçek cari: aynı gün nakit kümülatifi + §4.1.2 işlem tutarı kuralı (had üstü işlemde nakit payı reddedilir).
- Ürün ALIŞ'ı **belge üretmiyor** (gider pusulası yalnız `/islemler/hurda-alis` ve satış fişinin altın bacağında). Kalem başına belge YOK → çok satırlı belge sorusu bu pakette doğmadı; belge davranışı DEĞİŞTİRİLMEDİ.
- Alış gövdesi: `cariId, urunId, adet, birimFiyatTl, kasaId, odenenTl, odemeTuru (NAKIT|KART|HAVALE), vadeGun, kurSnapshotId, aciklama, masak, reyonId`. Ayar/gram/milyem üründen gelir (gövdede yok); hurda bu uçta yok.
- Canlı ölçüm (tek kalem, Peşin): 30.000,00 NAKIT → **200**; 30.000,01 NAKIT → **400**; 30.000,01 KART → **400** (satır C1–C3 aşağıda).

## 2. Yapılanlar

### Sunucu — `Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `POST /api/v1/islemler/alis-fisi` — `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` = `/islemler/alis` ile **birebir aynı**; sınıf düzeyi `[ModulYetkisi("islem")]`.
- `AlisFisiDto` / `AlisFisKalemDto`: fiş düzeyi alanlar tek kalemli DTO ile aynı anlamda; kalemler liste.
- `SatisServisi.AlisFisi`: servis içi elle doğrulama (boş/100 üstü kalem, ödeme türü, adet ≤ 0, negatif fiyat) → ürünler çözülür → **fiş BRÜT toplamı** → `AlisKapilari(...)` (MASAK · kasa/gün kilidi · vade) BİR KEZ → `OdemeSepeti.PaylariDagit` ile ödenen/kalan kalemlere → `AlisTevsikKapisi(...)` BİR KEZ fiş toplamıyla → tek transaction içinde kalem başına `AlisKalemiYaz` (ortak `fis_id`) → MASAK kaydı ilk harekete fiş toplamıyla → `SaveChanges` + commit.
- Kopya yok: tek kalemli `Alis` de aynı üç yardımcıyı (`AlisKapilari`, `AlisTevsikKapisi`, `AlisKalemiYaz`) çağırır. Deftere yazım yalnız `HareketFabrikasi.Ekle`.
- Yanıt (UX-S27): `fisId, hareketId, hareketIdleri, kalemSayisi, toplamTl, odenenTl, kalanTl, odemeTuru, yerlesimUyarisi`.
- Bu oturumdaki ek düzeltme: canlı C14'te boş kalem listesi "Kalemler en az 1 karakter olmalıdır" iletisi veriyordu (liste için yanlış). DTO özniteliklerine Türkçe `ErrorMessage` verildi → "Alış fişinde en az bir kalem olmalıdır" (canlı C14b ile doğrulandı; `L1bH_09` güçlendirildi).

### İstemci — `Kuyumcu.Web/Pages/Islemler.razor`, `Islemler.Yazdir.cs`
- `AlisFisUcunaGider => tur == "ALIS" && FisYazdir.FisUcuMu(kalemler.Count, ozelFis:false)` (≥2 kalem; eşik rakamı YOK).
- `Kaydet()` içinde tek POST dalı: ≥2 kalemli ALIŞ → `POST /islemler/alis-fisi` (tek istek); tek kalemli alış eski `/islemler/alis` yolunda. Yanıt okunur (fiş no, yerleşim uyarısı, yazdırma).
- MASAK formu tetikleyici tutar: ALIŞ'ta en büyük kalem yerine **fiş toplamı** (sunucu fiş toplamına bakıyor; eşik istemciye gömülmedi).
- Satış dalına ve L-1'in `satis-fisi` yoluna DOKUNULMADI.

### Migration
Gerekmedi (yeni kolon/varlık yok; `hareketler.fis_id` zaten vardı).

## 3. DoD sözleşmesi (varlık bazında)

| Kayıt | Uç / yol | Düzenlenebilirlik |
|---|---|---|
| Alış fişi hareketleri (`hareketler`, tur=ALIS, ortak fis_id) | yazım: POST /islemler/alis-fisi | Finansal kayıt — hard DELETE/PATCH YOK (DoD: yalnız storno). Düzeltme yolu: mevcut 48-A storno motoru / Alış İadesi (kalem başına, kasa satırı kalemin hareketine bağlı olduğu için storno doğru tutarı geri alır). Yeni varlık türü DOĞMADI. |
| kasa_hareketleri / odeme_kalemleri | kalem başına mevcut `KasaGuncelle` | Finansal — değişmez; storno ile |
| stok / stok_partileri | kalem başına mevcut `StokDegistir` / `PartiAc` | Mevcut stok düzeltme yolları |
| Denetim kaydı | `DenetimYazmaDinleyicisi` (SaveChanges) | Değişmedi; yeniden yazılmadı |

## 4. Test sayıları

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Önce (L-1b öncesi, önceki oturumun trx-pL-1b-on) | 5975 | 5975 | 0 |
| L1b filtreli (`FullyQualifiedName~L1b`) | 16 | 16 | 0 |
| Geniş ilgili (`L1|Alis|Tevsik|Islemler`, DTO düzeltmesi sonrası) | 226 | 226 | 0 |
| Tam süit (1. koşum, `trx-pL-1b`) | 5990 | 5989 | 1 — `CumaMMusterilerEkranTestleri.CUMA_M_11_Yeni_musteri_penceresi_Vazgec` (CUMA-M şeridi, bkz. § 12) |
| Tam süit (2. koşum, DTO düzeltmesi sonrası, `trx-pL-1b-son`) | 6003 | 6001 | 2 — `KatmanTekKalipTestleri.K1_01_index_katman_css_app_cssten_SONRA_ve_katman_js_yuklenir`, `KatmanTekKalipTestleri.K1_07_seciciler_kalan_alanla_sinirlanir` (KATMAN-1 şeridi; test dosyası 09:54'te eklendi, uygulaması yarım — bkz. § 12). CUMA_M_11 bu koşumda YEŞİL (sahibi düzeltmiş). L1b 16/16 yeşil. |

Yeni testler (16): `L1bAlisFisiEkranTestleri` 4 (bUnit) · `L1bAlisFisiHttpTestleri` 11 (L1bH_01–09, L1bH_02 teorisi 3 durum) · `L1bAlisFisiTxTestleri` 1 (servis, gerçek MySQL, transaction gözcüsü).

## 5. Mutasyon kanıtları (bu oturumda yeniden üretildi; yedek `scratchpad/mut-yedek/L-1b/`)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-L1b-1 | istemci: `AlisFisUcunaGider => false && …` (çok kalem eski kalem-başı POST'a döner) | `L1bAlisFisiEkranTestleri.Iki_kalemli_ALIS_TEK_alis_fisi_istegi_kalem_basi_alis_SIFIR_govde_ayristirilir`, `…Sunucu_tevsikle_reddederse_hata_GORUNUR_basari_YOK_kalem_basi_yedek_POST_YOK` | cmp birebir |
| M-L1b-2 | sunucu: tevsik kalem tutarıyla (`foreach kalem → AlisTevsikKapisi(kt.Tutar, kt.Tutar)`) | `L1bAlisFisiHttpTestleri.L1bH_04_gercek_cari_had_ustu_HAVALE_200_TEK_fisId_NAKIT_400_islem_tutari_kurali` | cmp birebir |
| M-L1b-3 | sunucu: transaction kaldırıldı (`tx = null`) | `L1bAlisFisiTxTestleri.L1bT_01_alis_fisi_SaveChanges_ACIK_transaction_icinde_ve_sonra_commit_edilmis` | cmp birebir |
| M-L1b-4 | istemci: MASAK tetik tutarı fiş toplamı yerine en büyük kalem | `L1bAlisFisiEkranTestleri.MASAK_formu_FIS_toplamiyla_acilir_her_kalem_esik_altinda_olsa_da` | cmp birebir |
| M-L1b-5 | sunucu: kalemler ortak `fis_id` taşımaz (`FisId = null`) | `L1bH_04…`, `L1bH_05_kalem_basina_stok_parti_kasa_hareketi_odeme_kalemi_ortak_fis_kismi_odeme_cariye_borc`, `L1bH_02(sapma:0)`, `L1bH_02(sapma:-0,01)` | cmp birebir |
| M-L1b-6 | DTO: liste iletisi eski varsayılana (`[Required, MinLength(1)]`) | `L1bH_09_bos_kalem_ve_sifir_adet_400_Turkce_mesaj_yazim_yok` | cmp birebir |

Notlar (Anayasa md.1):
- M-L1b-2'de Peşin testleri (L1bH_01/02) YEŞİL kaldı — bu bir test açığı değil, ÇİFT KAPI: Peşin Müşteri için MASAK kapısı (`MasakKapisi.DogrulaKumulatif`) da `tutar > tevsik.Had` kontrolünü fiş toplamıyla yapıyor. Tevsik kapısının kalem-tutarı mutasyonunu gerçek cari testi (L1bH_04) yakalıyor.
- M-L1b-3: fiş tek `SaveChanges` ile yazıldığından HTTP atomiklik testleri (L1bH_06/07) transaction kaldırılınca KIRMAZ (tek SaveChanges zaten atomik); açık transaction'ın varlığını `L1bT_01` (TransactionGozcusu) kanıtlıyor. Atomiklik davranışı ayrıca canlıda C13 ile ölçüldü.
- Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata (1 uyarı: `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` — bu paketin dosyası değil; ilk temiz derlemede 0 uyarı, artımlı derlemede görünüyor).

## 6. denetim.sh
`DENETİM TEMİZ.` (rc=0) — ilgili sayaçlar ✓ (HareketFabrikasi tek yol, S2-3 alan işareti, P-11 DIS_HATADA_VERI_DUSUREN_YOL 46/47, JS sözdizimi 0).

## 7. Canlı doğrulama (API 5162, kiracı `PAKETL-1b-TEST canli`, kur hazır, kasa 500.000 ₺ açılış)

| # | İstek | Beklenen | Gerçek | ALIS satır |
|---|---|---|---|---|
| C1 | tek kalem `/alis` Peşin NAKIT 30.000,00 (ÖNCE ÖLÇ) | — | **200** fisId=null | 0→1 |
| C2 | tek kalem Peşin NAKIT 30.000,01 | — | **400** "…tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor…" alan cariId | 1→1 |
| C3 | tek kalem Peşin KART 30.000,01 | — | **400** aynı ileti | 1→1 |
| C4 | fiş 2 kalem Peşin NAKIT 29.999,99 | 200 tek fiş | **200** kalemSayisi=2 toplamTl=29999.99 | 1→3 |
| C5 | fiş 2 kalem Peşin NAKIT 30.000,00 | C1 ile aynı → 200 | **200** kalemSayisi=2 | 3→5 |
| C6 | fiş 2 kalem Peşin NAKIT 30.000,01 | C2 ile aynı → 400 | **400** aynı ileti, alan cariId | 5→5 |
| C7 | fiş 2 kalem Peşin KART 30.000,01 | C3 ile aynı → 400 | **400** | 5→5 |
| C8+C9 | AÇIĞIN KANITI: eski yol 20.000 + 20.000 Peşin NAKIT | ikisi de geçer (açık) | 200 + 200 | 5→7 |
| C10 | aynı iki kalem YENİ fiş (40.000) | 400 | **400** "40.000,00 TL … eşiğine ulaşıyor" | 7→7 |
| C11 | gerçek cari (Ayşe) HAVALE 3 kalem 45.000 | 200 tek fis_id | **200** kalemSayisi=3 | 7→10 |
| C12 | gerçek cari NAKIT 2 kalem 30.000,01 | 400 (§4.1.2) | **400** "Tevsik zorunluluğu: işlemin toplam tutarı 30.000,01 TL…" | 10→10 |
| C13 | atomiklik: 2. kalem geçersiz ürün | 404, 1. kalem yazılmaz | **404** "Ürün bulunamadı veya aktif değil"; YZK-1 stoğu değişmedi | 10→10 |
| C14 | boş kalem listesi | 400 Türkçe alan iletisi | 400 "Kalemler en az 1 karakter olmalıdır" → DÜZELTİLDİ | 10→10 |
| C14b | (düzeltme sonrası, API yeniden başlatıldı) | 400 | **400** "Alış fişinde en az bir kalem olmalıdır" alan kalemler | 12→12 |

SQL kanıtı (kuyumcu_net, kiracı süzgeçli; fis_id son 6 hane):
```
fis_id | kalem | SUM(tl)   | SUM(nakit) | odeme_turu
(yok)  | 3     | -70000.00 | 70000.00   | NAKIT     ← C1, C8, C9 (tek kalemli eski yol, fis_id NULL)
Z02V40 | 2     | -29999.99 | 29999.99   | NAKIT     ← C4
JEVVVW | 2     | -30000.00 | 30000.00   | NAKIT     ← C5
N8V2JH | 3     | -45000.00 | 0.00       | HAVALE    ← C11
XDEN27 | 2     | -40000.00 | 0.00       | HAVALE    ← tarayıcı (Ayşe, HAVALE)
kasa_hareketleri (fişli): Z02V40 2/-29999.99 · JEVVVW 2/-30000.00 · N8V2JH 3/-45000.00 · XDEN27 2/-40000.00
odeme_kalemleri: Z02V40 2/29999.99 NAKIT · JEVVVW 2/30000.00 NAKIT · N8V2JH 3/45000.00 HAVALE · XDEN27 2/40000.00 HAVALE
stoklar: KLY-1 5 · KPE-1 1 · YZK-1 6   (reddedilen C2/C3/C6/C7/C10/C12/C13 ve tarayıcı reddi → 0 hareket, 0 stok)
```

## 8. Gerçek tarayıcı (kendi sekmem tab-84, vekil 5163, parolasız oturum)

**Sapma (gerekçeli):** kabul "Satış ekranı ALIŞ modu BASİT 1440" diyor; ürün ALIŞ'ı BASİT yüzde **yok** — Basit ızgara (`FisIslemIzgarasi.razor`) yalnız Satış · "Alış · eski altın" (= HURDA_ALIS) · Tahsilat · Ödeme · İade · Değişim · Emanet · İndirim karolarını taşır; Basit'e geçiş türü SATIS tuvaline sıfırlar (ekran görüntüsüyle doğrulandı). Bu nedenle ALIŞ senaryosu **PROFESYONEL 1440** ile koşuldu; BASİT yüzde ise değişen sayfanın (Islemler.razor) Satış tuvali ölçüldü.

Senaryo (PRO 1440×1300 açık): Alış sekmesi · Peşin Müşteri · Nakit · Kolye 20.000 + Yüzük 20.000 → Alış Kaydet → `read_network_requests`:
```
POST /api/v1/islemler/alis-fisi → 400 Bad Request     (tek istek; /islemler/alis 0)
```
Ret bandı `.bant-hata` görünür: "Peşin Müşteri ile bu tutarda işlem yapılamaz: 40.000,00 TL, tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor…"; kalemler ve toplam (40.000 ₺) korunur. Ardından Ayşe Deneme + Havale → Alış Kaydet → `POST /islemler/alis-fisi → 200 OK` (tek istek), başarı bandı "İşlem başarıyla kaydedildi!" + "Fiş yazdır" + yerleşim uyarısı bandı; SQL'de fis XDEN27, 2 satır.

Ölçüm (`sikisma-olc.js` → `__olcTam`, ALIŞ durumu: 2 kalem + ret bandı açıkken):

| Rota | Genişlik | Mod | Tema | K1 gizli | K2 kart% | K3 taşan/kırık | K5 TRY | K6 seçici | K7 hizasız | K8 jargon | K9 binen | K10 elipsis | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /islemler (ALIŞ) | 1280 | PRO | açık | 0 | 98 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (ALIŞ) | 1280 | PRO | koyu | 0 | 98 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (ALIŞ) | 375 | PRO | açık | 0 | 97 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (ALIŞ) | 375 | PRO | koyu | 0 | 97 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (Satış tuvali) | 1280 | BASİT | açık | 0 | 68 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (Satış tuvali) | 1280 | BASİT | koyu | 0 | 68 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (Satış tuvali) | 375 | BASİT | açık | 0 | 97 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 | 0 |
| /islemler (Satış tuvali) | 375 | BASİT | koyu | 0 | 97 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 | 0 |

Koyu tema doğrulaması: `prefers-color-scheme: dark` true, body arka planı rgb(14,18,25); açıkta rgb(242,244,247). BASİT 1280 K2=68: Basit tuval sağda "Ne yapıyorsun?" karo sütunu taşıdığı için en geniş kart ana alanın %68'i — %60 tabanının üstünde, gerekçe gerekmez.
Ekran görüntüleri (oturum dökümünde): PRO 1440 açık ret bandı · PRO 1440 başarı bandı · PRO 1280 koyu ret bandı · PRO 375 açık (Peşin uyarısı + nakit haddi alan iletisi) · PRO 375 koyu ret bandı · BASİT 1280 koyu. Not: bu tarayıcı bölmesinde sayfa kaydırıldıktan sonra alınan görüntüler boş geliyor (yakalama kusuru); bu yüzden 1440/1280 görüntüleri 1300 px yükseklikte kaydırmasız alındı.

## 9. Değişen dosyalar (bu paketin alanı)
```
 M dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs        (AlisFisi ucu + servis + ortak yardımcılar + DTO; bu oturumda DTO iletisi)
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor                (ALIŞ ≥2 kalem → alis-fisi; MASAK tetik tutarı fiş toplamı)
 ?? dotnet/Kuyumcu.Web/Pages/Islemler.Yazdir.cs           (AlisFisUcunaGider; dosya L-1'den beri izlenmiyor)
 ?? dotnet/Kuyumcu.Tests/L1bAlisFisiEkranTestleri.cs
 ?? dotnet/Kuyumcu.Tests/L1bAlisFisiHttpTestleri.cs       (bu oturumda L1bH_09 güçlendirildi)
 ?? dotnet/Kuyumcu.Tests/L1bAlisFisiTxTestleri.cs
 ?? dokumanlar/09-uygulama-kayitlari/PAKET-L-1b-uygulama-raporu.md
```
(Ağaçtaki diğer `M`/`??` satırları başka paketlerin/şeritlerin; `git status` ilk commit'ten beri tüm projeyi değişmiş gösteriyor.) Dokunulan sayfalar (DoD EK 2): `Pages/Islemler.razor` (+ `Islemler.Yazdir.cs`) — gerekçe: L-1b kapsamı, ALIŞ dalı.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| ÖNCE ÖLÇ raporda | ✓ | § 1, C1–C3 |
| Yeni uç, alis ile aynı rol kümesi | ✓ | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` iki uçta; L1bH_08 (TEZGAHTAR 403, rol kümesi karşılaştırması) |
| Tek transaction, hep ya hiç | ✓ | L1bT_01 (M-L1b-3), L1bH_06/07, C13 |
| Kapılar fişin BRÜT toplamıyla bir kez | ✓ | M-L1b-2 → L1bH_04; C4–C7, C10, C12 |
| Kalemler aynı fis_id | ✓ | M-L1b-5; SQL § 7 |
| Deftere yazım yalnız HareketFabrikasi.Ekle | ✓ | `AlisKalemiYaz`; denetim.sh TEMİZ |
| Stok/hurda girişi ortak yardımcıda, kopya yok | ✓ | `AlisKalemiYaz` iki uçta ortak |
| İstemci ≥2 kalem tek POST, tek kalem eski yol | ✓ | bUnit 3 test + M-L1b-1; tarayıcı ağ kaydı |
| Belge: çok satırlı gider pusulası | Uygulanmaz | Ürün alışı belge üretmiyor; belge davranışı değişmedi (§ 1) |
| Sunucu ek savunma (ardışık fiş) UYGULANMAZ | ✓ | uygulanmadı |
| KABUL canlı 29.999,99 / 30.000,00 / 30.000,01 | ✓ | C4/C5/C6 = C1/C1/C2 ile aynı sonuç |
| Ret halinde 0 hareket | ✓ | SQL § 7 |
| Gerçek müşteri ≥30.000 izinli ödeme türü → 200 tek fis_id | ✓ | C11 (HAVALE, 45.000, tek fis) |
| bUnit alis 0 / alis-fisi 1; tek kalem değişmedi | ✓ | `Iki_kalemli_…`, `Tek_kalemli_ALIS_bugunku_alis_ucunda_kalir_davranis_DEGISMEDI` |
| HTTP atomiklik / yetki / kiracı izolasyonu | ✓ | L1bH_06, L1bH_08, L1bH_07 |
| Gerçek tarayıcı tek istek + ret bandı | ✓ (PRO; BASİT'te ALIŞ yok — sapma § 8) | § 8 |
| Mutasyon 3 zorunlu madde | ✓ (+3 ek) | § 5 |
| SATIS / satis-fisi yoluna dokunulmadı | ✓ | diff yalnız ALIŞ dalı; L1 testleri yeşil (geniş koşum 226/226) |
| Eşik istemciye gömülmedi | ✓ | istemcide rakam yok; tevsik haddi yapılandırmadan |
| Build 0 hata | ✓ | § 5 |
| denetim.sh TEMİZ | ✓ | § 6 |
| Test kiracısı silindi, tenants sayısı | ✓ | § 11 |
| Paketin yeni kırmızısı yok | ✓ | tam süitteki kırmızılar CUMA-M (1. koşum) ve KATMAN-1 (2. koşum) şeritlerinin (§ 12) |

## 11. Temizlik
- API (5162) ve vekil (5163) yalnız kendi PID'leriyle durduruldu; `lsof` boş.
- Kiracı `PAKETL-1b-TEST canli` (bu oturumunki) + önceki oturumdan kalan aynı adlı kiracı: 70 tablodan silindi, kalan satır 0; davet satırı silindi.
- tenants sayısı: bu oturum başında (önceki artık silindikten sonra) 14 → kendi kiracım eklenip silindikten sonra 15. Fark bana ait DEĞİL: aradaki sürede CUMA-M şeridi `PAKETCUMA-M-TEST canli` (06:40 UTC) açtı. `PAKETL-1b%` adlı kiracı sayısı: 0.
- Tarayıcı sekmem kapatıldı; görünüm boyutu sıfırlandı. Proje ağacına geçici dosya bırakılmadı.

## 12. Açık kalanlar
1. **Tam süitte başkasının kırmızıları:** (a) 1. koşum: `CumaMMusterilerEkranTestleri.CUMA_M_11_Yeni_musteri_penceresi_Vazgec` ("Vazgeç" beklenir, "İptal" gelir) — sahibi **CUMA-M şeridi**; 2. koşumda yeşil. (b) 2. koşum: `KatmanTekKalipTestleri.K1_01_…` ("katman.css app.css'ten sonra yüklenmeli (app=420, katman=-1)") ve `K1_07_seciciler_kalan_alanla_sinirlanir` ("--secici-azami-boy" yok) — sahibi **KATMAN-1 şeridi** (index.html / katman.css / katman.js; test 09:54'te eklendi). L-1b dosyalarıyla ilişkileri yok, dokunulmadı. L-1b'nin YENİ kırmızısı: 0.
2. **Metin (bu paket dışı, sözlük):** ALIŞ modunda Peşin Müşteri kimlik uyarı bandı "Bu tutarda **satış** için müşterinin kimliği gerekiyor" diyor — alışta "işlem" olmalı. Mevcut sözlük metni; L-1b kapsamı dışı, Ertuğ/sözlük kalemi olarak önerilir.
3. **Gözlem (bu paket dışı):** PRO ALIŞ'ta ret bandı açıkken BASİT'e geçilince Satış tuvalinde "Bu işlem yasal bir kapıya takıldı…" ve Peşin kimlik uyarısı taşınmış görünüyor (fiş boşken). Mod geçişinde durum temizliği — ayrı inceleme önerilir.
4. **Derleme uyarısı:** `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` artımlı derlemelerde görünüyor — bu paketin dosyası değil.

## 13. Kurucuya / mali müşavire notlar
- **S-6 (mevcut) teyidi:** tevsik haddi 30.000 ₺ yapılandırmadan okunuyor (575 s. VUK GT, RG 30.11.2024/32738 — orkestratör kaynağı); bu paket haddi değiştirmedi, yalnız bölmeyi kapattı.
- **YENİ SORU (MALİ MÜŞAVİR):** Ürün ALIŞ'ı (vergi mükellefi olmayan bireyden ürün/ziynet alımı) hiçbir belge üretmiyor; gider pusulası (VUK md.234) yalnız hurda alış yolunda ve satış fişinin altın bacağında düzenleniyor. Bireyden ürün alışında gider pusulası gerekip gerekmediği ve gerekiyorsa çok kalemli fişte TEK çok satırlı pusula düzenlenip düzenlenemeyeceği birincil kaynakla/müşavirce teyit edilmeli. Bu pakette belge davranışı DEĞİŞTİRİLMEDİ (kural: doğrulanmayan mevzuat koda yazılmaz).
- **S-L1 (mevcut):** aynı kasiyerin ardışık ayrı fişlerle bölmesine karşı sunucu savunması UYGULANMADI (kapsam gereği mali müşavirde).
