# PAKET TOPLU-7A uygulama raporu (09.10.2026, devralan tamamlama)

Dalga TOPLU-7, hasımsız görünüm paketi. Önceki uygulayıcı işi bitirdi, son adımda ağ kesintisiyle sonuç yazamadı. Bu oturum yalnız doğrulayıp raporu tamamladı. Kod değiştirilmedi.

## Yapılanlar
- /urunler PRO 1280: barkod hücresi harf harf kırılıyordu (77 px). zo-islem.css'te barkod hücresine içerik kutusunda asgari genişlik verildi. 13 haneli EAN tek satır (118 px).
- Karma ödeme aşımı: ileti ✕ düğmesinden sonra gelir, ✕ satırdan kopmaz, ileti en son dizilir.
- "Kalanı ata": hedef son yazılan satır. 3 satırda tek tıkla aşım kapanır, hiçbir satır silinmez. Tutar formülü değişmedi. İleti tutarı 0 olan satırın altına yazılmaz.
- Stok rozeti: Bicim.Miktar ADET kolu binlikli ("1.000.000 adet").
- girdi-maske.js: tutar "0" iken rakam yazılınca "0" yerine geçer ("099999" olmaz).
- Tam sayı alanında "1.500" için ondalık önerisi (Belirsiz) çıkmaz. Sözlük: Sozlukler.SayiGirisi.cs.
- Kararsız SATIS2 testi güçlendirildi (WaitForAssertion).

## Sapmalar
Yok. Para, veri ve yasal davranışa dokunulmadı. Hasımlı pakete devir gerekmedi.

## Migration
Gerekmedi.

## Testler
- Tam süit (tek kez, son kodla): toplam 11643, geçti 11639, başarısız 1, atlanan 3.
- Tek kırmızı: EksikB1YazdirTestleri.B15_hasim_kapora_gercek_para_degilse_400(VERESIYE). Bu EKSIK-B1 şeridinin (t5) testidir, bu pakette üretilmedi.
- Yeni test: 11 adet (Toplu7ATestleri.cs T7A01-T7A11), ayrıca SATIS2/SATIS3 test düzeltmeleri.

## Mutasyon (8/8, cmp AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M7A-1 | Miktar ADET binliksiz | Toplu7ABicimTestleri.T7A06 |
| M7A-2 | ileti 0 tutarlı satıra da yazılır | Toplu7AOdemeTestleri.T7A02 |
| M7A-3 | Kalanı ata tek hedef değil | Toplu7AOdemeTestleri.T7A03 |
| M7A-4 | ileti ✕'ten önce | Toplu7AOdemeTestleri.T7A01 |
| M7A-5 | maske "0" üstüne yazma kalktı | Toplu7AMaskeTestleri.T7A09 |
| M7A-6 | tam sayıda eski Belirsiz | SATIS3EkranTestleri.T17 + Toplu7ABicimTestleri.T7A08 |
| M7A-7 | barkod min-width kalktı | Toplu7ACssTestleri.T7A10 |
| M7A-8 | ileti order kalktı | Toplu7ACssTestleri.T7A11 |

Yedekler canlı dosyalarla orkestratörce karşılaştırıldı: hepsi AYNI.

## Denetim
denetim.sh yeniden koşuldu: tek ihlal P-30 DEMO_INSAN_SURESI (görev tanımında hariç). Başka ihlal yok.

## Gerçek Chrome ölçümü (basliksiz, 8 kombinasyon)
/urunler PRO: önce 1280 K3_kirik=5 (barkod 77 px, 2 satır), sonra 0 (118 px, tek satır). 375'te 0. Taşan 0, yatay kaydırma 0. Rozet "1000000 adet" -> "1.000.000 adet". BASIT'te K3=0. Karma ödeme 8 kombinasyon ölçüldü, görüntüler /private/tmp/kuyumcu-serit/TOPLU-7A/karma-*.png ve urunler-*.png.

## Canlı doğrulama (5160)
- Kalanı ata sonrası gövde: 200, SQL KART 4000 + NAKIT 6000 (0 TL satır gönderilmez).
- 3 satır aşım 6.000x3 (fiş 10.000): 400, Türkçe ileti, hareketler 7/7 değişmedi.
- Stok 999.999 açılış: API 200, SQL adet=999999.000. 5 adet 13 haneli EAN: 200.

## Temizlik
5160 API ve 5161 vekil durduruldu (dinleyen yok). Test kiracısı PAKETTOPLU-7A-TEST silindi, tenants 19 -> 18.

## Koşul karşılama
Build 0/0 (tam süit derlemesiyle), denetim P-30 hariç temiz, mutasyon 8/8, tam süit yeni kırmızı 0, gerçek Chrome ölçümü yapıldı, COMMIT yok.

## Açık kalanlar
- EKSIK-B1 sahibi: B15 kırmızısı.
- P-30: kronometreli demo ölçümü (kurucu).
- Görüntü Read sayısı bu oturumda 0 (önceki oturumda ölçüm sayısal); kurucu gözle bakmak isterse yukarıdaki png yolları.

## Son doğrulama (09.10 ikinci devralan)
- dotnet build Kuyumcu.sln: 0 hata; 6 uyarı hepsi Kuyumcu.Tests içinde başka şeritlerin dosyalarında (SUBEKAPSAMASinirTestleri, KesifP2Testleri, SonKalanlarTestleri).
- Filtreli koşum (Toplu7A* + SATIS2/SATIS3 ekran testleri, KUYUMCU_TEST_DB=kuyumcu_test_t2): 198 toplam, 197 geçti, 0 başarısız, 1 atlanan (önceden atlanmış DOG_T24).
- denetim.sh: P-30 (hariç) ve TOPLU-1 TARIH_TEK=2 (Bilesenler/Yildiz/YildizBugunVerisi.cs:72-73, YILDIZ-4 şeridi dosyası, bu paketin değil).
- Mutasyon yedekleri canlı dosyalarla cmp AYNI (M7A-4 yeniden doğrulandı).
- Canlı ölçüm t2'de yeniden koşulmadı; kod ölçümden sonra değişmedi, t1 ölçümü (olc7-sonra4.json) geçerli. Ekran görüntüsü Read ile görüldü: urunler-sonra4-PRO-karanlik-1280.png, 13 haneli EAN tek satır, "1.000.000 adet".
