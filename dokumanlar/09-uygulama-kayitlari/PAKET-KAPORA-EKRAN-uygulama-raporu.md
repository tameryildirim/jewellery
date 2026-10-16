# PAKET KAPORA-EKRAN — Uygulama raporu (09.10.2026, t5 · API 5166 · vekil 5167)

**DURUM: BITTI (tur 2)** — hakem KRİTİK bulgusu kapandı, Y30 beklentisi güncellendi, tam süit 0 başarısız. Denetim TEMİZ DEĞİL ama kalan 2 ihlal başkasına ait (aşağıda). Commit YOK. Migration: gerekmedi.

## Yapılanlar
- 409 sonrası ekran: çekmece kapanır, sipariş yeniden okunur, kart güncel kaporayı gösterir, "+ Kapora al" kalmaz, bantta "Bu sipariş başka bir yerden değişti. Güncel hali gösteriliyor." (`SiparisDetay.razor`, `KaporaGecmisSozlugu.BaskaYerdeDegisti`).
- Jargon temizliği TR+EN: "storno / ters kayıt / sipariş kaydı / Açık hesap" kapora ekranında yok; sunucu 409 metni sade ("Bu siparişte kapora zaten alınmış. Değiştirmek için önce kaporayı geri verin.") ve EN karşılığı `SunucuIletiSozlugu.Veri.cs` içinde.
- Açıklama cümleleri kaldırıldı; yetkisiz kullanıcıya silik "Kaporayı geri ver" yerine okunur cümle; geri ver düğmesi 375 px'te tek satır (ölçülen yükseklik 44).
- Geçmiş listesi madde işaretsiz (`list-style:none`, ölçüm: `liStil=none`).
- Makbuz: siparişe bağlı kapora tahsilatında "Has gram" sütunu yok; siparişsiz tahsilat makbuzunda korunur (`CiktiModulu.cs`).

## Testler
Yeni: `KaporaEkranTestleri.cs` KEK01–KEK08 (8 test; saf 2 + bUnit 4 + makbuz 2) — filtreli koşum 8/8 yeşil.
Tam süit (trx `/private/tmp/kuyumcu-serit/trx-pKAPORA-EKRAN/…18_03_25.trx`): **11967 toplam · 11959 geçti · 5 başarısız · 3 atlandı.**
| Kırmızı | Sahibi |
|---|---|
| `P26TedarikTestleri.Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir` | **BU PAKET** — "storno" arıyor; düzeltme: satır 536 `"storno"` → `"geri ver"` (kurucu/denetçi onayı bekliyor) |
| `UX4dOturumTestleri.M15_Ucu_paralel_401_TEK_yenileme_ve_TEK_cikis_uretir` | UX-4d oturum şeridi |
| `Yildiz4BTestleri.Y4B_31_css_kapsam_ve_belirtec` | YILDIZ-4 (t1) |
| `Yildiz4CTestleri.Y4C_25_pro_kalem_duzeltmesi_ozgun_kurallarla_ileti_ve_toplam` | YILDIZ-4 (t1) |
| `YildizAmblemTestleri.YA_02_gradyan_duraklari_ve_SMIL_degerleri_tuvalle_birebir` | YILDIZ amblem |

## Mutasyon (perl tek eşleşme → filtreli test → yedekten geri → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1 | 409 dalı `StatusCode==409`→`4090` | KEK03 | AYNI |
| M2 | makbuz has-sütunu kuralı `false &&` | KEK07 | AYNI |
| M3 | geri-ver yetki koşulu kaldırıldı | KEK05 | AYNI |
| M4 | sunucu 409 metnine "(storno)" | KEK02 | AYNI |
| M5 | sözlüğe "ters kayıt" | KEK01 | AYNI |
| M6 | geçmiş listesi `list-style:disc` | KEK06 | AYNI |
| M7 | makbuz kuralı siparişe-bağlı koşulu kaldırıldı | KEK07 + KEK08 | AYNI |
7/7 kırmızı. Son derleme `dotnet build`: 0 uyarı 0 hata.

## Denetim (`bash denetim.sh`)
Kapora sayaçları ✓. Başarısız 2 madde bu paketin değil: `P-30 DEMO_INSAN_SURESI` (kronometreli ölçüm — kurucu) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` — MASAK-ESZAMAN-2 dalgası). Denetim bu yüzden TEMİZ DEĞİL, ama ihlal bu paketten değil.

## Canlı (5166, kiracı PAKETKAPORA-EKRAN-TEST)
| Adım | Beklenen | Gerçek |
|---|---|---|
| İki sekme yarışı: A kaporayı alır, B (bayat) alır | B 409 → B'de kart güncel, "+ Kapora al" yok | `al:false, form:false, tutar "1.500 ₺"`, bant "Bu sipariş başka bir yerden değişti…", jargon:false |
SQL: S-2026-000001 → kapora bağlı, siparişe bağlı 2 TAHSILAT (−1500 storno'lu, −1000 güncel); S-2026-000002 → kapora yok; kasa bakiyesi 1000,00 = kasa hareketleri toplamı 1000,00 (çift tahsilat yok).

## Gerçek Chrome ölçümü (başsız Chrome, `sikisma-olc.js`; `/private/tmp/kuyumcu-serit/pKAPORA-EKRAN/olcum/`)
`yok` ve `zengin` durumları × {1280, 375} × {BASİT, PRO} × {açık, koyu} = **16/16 ölçüm**: yatay kaydırma yok, jargon:false, K2 kart %97–98, geri ver düğmesi 44 px (375'te tek satır), liste işaretsiz. Gerekçeli: K8=1 yalnız BASİT'te ("S-A/S-B" test verisi kimlik kısaltması, arayüz metni değil); K4 boyKat 1,4–2,1 kart bölüm sayısına bağlı, taşma yok. Makbuz 1280 ayrıca ölçüldü (Has sütunu yok). Read ile bakılan görüntüler (3/4): `yaris-3-sonra`, `zengin_BASIT_karanlik_375`, `makbuz-1280`.

## Temizlik
API, `dotnet run` sarmalayıcısı ve vekil kendi PID'lerimle durduruldu (5166/5167 boş). Test kiracısı `kiraci-temizle.sh --uygula` ile silindi; PAKETKAPORA-EKRAN% = 0; tenants 21 → **20**. Sır dosyaları `printf '' >` ile boşaltıldı.

## Dokunulan sayfalar
`SiparisDetay.razor` (gerekçe: KAPORA-ESZAMAN hakem bulguları — aynı kapora kartı), `Cikti.razor` yalnız devir (değişmedi bu turda).

## Açık kalanlar
1. **Y30 testi** (yukarıda): bir satırlık güncelleme onay bekliyor — kurucu/Aşkın.
2. Tam süitteki 4 başkasına ait kırmızı (tablo) ve denetimdeki P-30 / GIRDI-2 (MASAK-ESZAMAN-2).
## Kurucu / mali müşavir
Mevzuat hükmü yok; para/veri/yasal davranış değişmedi.

## TUR 2 (09.10 22:16) — hakem KRİTİK bulgusu + Y30
- **K3 (375, modal onay düğmesi iki satır)**: `KaporaGecmisMetinleri.GeriVerOnay` ("Geri ver" / "Refund") eklendi; `SiparisDetay.razor` modal onay düğmesi bunu kullanır (başlık "Kaporayı geri ver" aynı). Yeni test KEK09 (etiket kısa) + KEK04 modal onay etiketini sözlükle karşılaştırır.
- **Y30**: `P26TedarikTestleri.cs:536` beklentisi `"storno"` → `"geri ver"` (sade 409 metni: "…önce kaporayı geri verin."; satır 470'teki ayrı test dokunulmadı). Y30 yeşil.
- Mutasyon: M8 (modal onay düğmesi eski uzun etikete) → KEK04 kırmızı, cmp AYNI; M9 (sözlük "Kaporayı geri ver") → KEK09 kırmızı, cmp AYNI. Toplam 9/9 kırmızı.
- Filtreli: KaporaEkran* + Y30 = 10/10 yeşil. Build 0 hata (6 uyarı, hepsi başkasının test dosyalarında: KesifP2, SUBEKAPSAMASinir, SonKalanlar x2, Yildiz2 x2).
- **Tam süit** (trx `/private/tmp/kuyumcu-serit/trx-pKAPORA-EKRAN/_Mac_2026-10-09_22_16_30.trx`): 11970 toplam · 11967 geçti · **0 başarısız** · 3 atlandı (önceki turdaki 4 başkası kırmızısı artık yeşil).
- **Denetim**: kapora sayaçları ✓; başarısız 2 madde başkasına ait: `P-30 DEMO_INSAN_SURESI` (kurucu kronometre) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK-ESZAMAN-2).
- **Gerçek Chrome, modal ölçümü** (`olcum/modal.mjs`, `olcum.jsonl`): {BASİT, PRO} × {açık, koyu} × {1280, 375} = 8/8; onay düğmesi metni "Geri ver", yükseklik 44 = Vazgeç ile aynı (tek satır), K3 = 0 (sıfır olmayanlar yalnız K2 %97–98, K4 1,5–2,1, K9=6 modal arka planı), yatay kaydırma yok. Read ile görülen: `modal_PRO_karanlik_375.png`, `modal_BASIT_aydinlik_375.png` (iki düğme yan yana, "Vazgeç | Geri ver" tek satır).
- **Canlı (5166)**: ikinci kapora POST → 409 TR "Bu siparişte kapora zaten alınmış. Değiştirmek için önce kaporayı geri verin." / EN "A deposit has already been taken on this order. To change it, refund the deposit first."; SQL: kiracıda 1 TAHSILAT hareketi, 1 kasa hareketi (çift tahsilat yok). Tarayıcı yarışı turu 1'de kanıtlandı (kod yolu değişmedi).
- Temizlik: API/vekil kendi PID'lerimle durdu (5166/5167 boş), kiracı `kiraci-temizle.sh --uygula` ile silindi (PAKETKAPORA-EKRAN% = 0). tenants: tohumlamadan önce 23, sonra 22 — benim kiracım gitti; 1 fark eş zamanlı başka şerit temizliği olabilir, doğrulanamadı. Sır dosyaları boşaltıldı.
