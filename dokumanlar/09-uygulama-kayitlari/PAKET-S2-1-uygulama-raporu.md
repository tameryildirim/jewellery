# PAKET S2-1 — Islemler.razor'u bileşenlere bölme (tezgâh fişinin temeli) · uygulama raporu

**Tarih:** 2026-09-23 · **Şerit:** S2 (Tezgâh fişi) · **Rol:** Taşkın + Ertuğ (Opus)
**Spesifikasyon:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-2 madde 2.1
**Durum:** BİTTİ. Paket kapısının 7 maddesinin hepsi sağlandı. Tam süitte kalan 2 kırmızı başka şeritlere ait (§ 9).

---

## 1. Ne yapıldı

`Pages/Islemler.razor` **2.623 → 1.397 satır** (kabul ≤ 1.400). Fiş gövdesi `Bilesenler/Fis/` altına ayrıldı. Bileşenler parametre ve `EventCallback` ile çalışıyor. Hiçbiri API istemcisi enjekte etmiyor, hiçbiri POST atmıyor. `Kaydet()` sayfada kaldı ve **9 `Api.PostAsync` çağrısının hepsi onun içinde**. Bunu `Tek_POST_yolu_Islemler_Kaydet_icinde_kalir` testi sınıyor.

| Dosya | Satır | İçerik (Islemler.razor'dan birebir taşındı) |
|---|---|---|
| `Bilesenler/Fis/FisModelleri.cs` | 415 | `FisKalemi` (eski `Kalem`), `KalemGirisi`, `FisBacagi` (eski `Bacak`), `HurdaGirisi` (+ satıcı kimliği kuralları), `TakasGirisi` (+ fark önizlemesi), `TransferGirisi`, `OdemeKirilimi` (P-23 ayrıştırması), `FisHesap` (iskonto/toplam/bacak/kasa/POS hesapları), `FisYaniti` (fiş no, yerleşim uyarısı, sunucu iskontosu okuyucu) |
| `FisOdemeKasa.razor` | 221 | Ödeme türü, kasa, vade, karma anahtarı (`@bind-OdemeTuru/KasaId/VadeGun`). Yasal kapı bayrakları sayfadan gelir. |
| `FisOdemeBacaklari.razor` | 286 | Karma ödeme bacakları (döviz, kart POS/taksit, kalan ata) |
| `FisKalemGirisi.razor` | 95 | Barkod + ürün + adet + fiyat satırı. Barkod, teklif ve ekle istekleri sayfanın geri çağrısı. |
| `FisKalemTablosu.razor` | 175 | Kalem tablosu + satır içi düzeltme (`SatirIciDuzenle`). `KartListe` sarmalayıcısı sayfada kaldı (MobilParite B9). |
| `FisHurdaAlis.razor` | 111 | Hurda alanları + DP4-03 satıcı kimliği |
| `FisTakas.razor` | 86 | Takas alanları + fark önizlemesi |
| `FisIade.razor` | 63 | İade kaynağı, adet, orantılı tutar |
| `FisTransfer.razor` | 48 | Şube/ürün/adet. Reyon seçicileri DOM sırası korunsun diye sayfada kaldı. |
| `FisSonSatislar.razor` | 165 | Son satışlar kartı + ödeme kırılımı. İstekler sayfada. `DegismezIpucu` sayfadan `ChildContent` olarak gelir. |

**Sayfaya haber verme.** Bölünmenin tek yeni riski şuydu: alt bileşendeki olay yalnız alt bileşeni yeniden çizer. O zaman Kaydet kilidi, FARK ve MASAK tutarı bayat kalırdı. Bu yüzden her bileşen, her değişiklikten sonra `Degisti` geri çağrısını çağırıyor. Sayfa `Cizim()` (boş EventCallback alıcısı) ya da `TevsikOnizle`/`KalemDegisti` ile yeniden çiziliyor. Bu kural mutasyonla sınandı (M-S21-3, M-S21-10).

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon / eski durum | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | "Kalem tablosu, ödeme bacakları, hurda, takas" (4 blok) | + ödeme/kasa, iade, transfer, son satışlar ve yanıt okuyucular | Yalnız 4 blokla sayfa 1.676 satırda kalıyordu. ≤ 1.400 kabulü ancak bu ek taşımalarla sağlandı. Hepsi yapı taşıması, davranış aynı. |
| S2 | Barkod düğmesinde 🔎, bacak silmede ✕ | `<Ikon Ad="@Ikonlar.Arama" />`, `<Ikon Ad="@Ikonlar.Carpi" />` | `IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI` Bilesenler klasöründe emojiyi yasaklıyor. Düğme metni ve id'ler aynı. Görsel fark: glif yerine SVG ikon. |
| S3 | `style="flex:1"` ×2, `style="grid-column:span 1"` | `.esnek` (app.css'teki `.esnek { flex: 1; }` kuralının birebir aynısı), grid-column kaldırıldı (varsayılan değer) | Yeni dosyada satır içi style azaltıldı: sayaç 196 tavanında 113'e indi. Kalan 5 style (bacak başlığı 1, takas 4) olduğu gibi taşındı. Sınıf karşılıkları app.css'te yok ve app.css bu şeridin dosya alanında değil. |
| S4 | `DegismezIpucu` son satışlar kartındaydı | Sayfa `ChildContent` olarak veriyor, DOM yeri aynı | P-06 sayacı (DUZENLEME_KALIBI_YOK_SAYFA) mühürlü kalıbı **sayfa dosyasında** arıyor. Kalıp bileşene taşınınca sayaç kırıldı; bu yolla düzeltildi. |
| S5 | Ölü kod: `PosBarkodTus`, `BirimDegisti`, `KalemEklenemez` | Kaldırıldı | Hiçbir yerden çağrılmıyordu (grep ile doğrulandı). Özellik değil. |
| S6 | `HediyeDegisti` önizlemesi ateşle-unut | Aynen korundu (`_ = TevsikOnizle()`) | Davranış aynı kalsın diye. |

Tasarım tuvaline göre sapma yok. Bu paket görünümü değiştirmiyor, S-2'nin sonraki paketleri (2.2 ızgara, 2.3 VERDİM/ALDIM) bu bileşenleri kullanacak.

## 3. Id envanteri (kabul: diff BOŞ)

- `grep -o 'id="[^"]*"'` yöntemiyle önce ve sonra (Islemler + Fis bileşenleri) alınan listeler **birebir aynı**: 44 satır, 33 tekil id. `diff` boş.
- Daha geniş envanter (`id=`, `Id=`, `ElemanId=` literalleri): öncesinde 77 tekil vardı, **77'si de duruyor**. Farkın tamamı bağlama parametresi gürültüsü (`KaynakId="@iadeKaynakId"`, `KasaId="kasaId"`). Bunlar DOM id'si değil.
- Dinamik kalıplar da korundu: `islem-tur-{deger}`, `islem-bacak-{ix}-*`, `islem-bacak-sil-{ix}`, `islem-kalem-hediye-*`, `si-kalem-*-{UrunId}`, `islem-iade-et/storno/kirilim-{h.Id}`. `Id_envanteri_korunur_hicbir_id_silinmedi` testi bunu sınıyor.

## 4. Migration
**Gerekmedi.** Şema değişikliği yok.

## 5. DoD sözleşmesi
Bu paket **yeni varlık ya da kayıt türü doğurmuyor**, yeni uç açmıyor. Mevcut düzenleme yolları aynen yerinde:

| Kayıt | Düzenleme / düzeltme yolu | Nerede |
|---|---|---|
| Fiş kalemi (kaydetmeden önce) | Satır içi adet/fiyat/iskonto (`SatirIciDuzenle`), sil | `FisKalemTablosu` |
| Ödeme bacağı (kaydetmeden önce) | Araç/tutar/kasa/POS/taksit değiştir, sil, kalanı ata | `FisOdemeBacaklari` |
| Kaydedilmiş satış fişi | Değişmez: `DegismezIpucu` → "Satış İadesi". Storno düğmesi devre dışı ve gerekçesini yazıyor. | Sayfa → `FisSonSatislar` |

Her düğme ya istek atıyor ya da görünür bir iş yapıyor. Storno düğmesi disabled ve gerekçesini `DegismezIpucu` ile gösteriyor.

## 6. Testler

**Yeni: `Kuyumcu.Tests/S21FisBilesenTestleri.cs`, 21 test, 21/21 yeşil**
- **Saf hesap (11):** `FisHesap_satir_ve_fis_iskontosu_sunucu_formuluyle_ayni`, `FisHesap_iskonto_brutu_asamaz`, `FisHesap_nakit_kilidi_yalniz_NAKIT_bacakta_ve_sunucu_bayragiyla`, `FisHesap_bacak_kasasi_para_birimine_gore_TEK_ise_atanir`, `FisHesap_POS_cihazi_tek_ise_atanir_KART_disinda_temizlenir`, `HurdaGirisi_satici_kimligi_kurallari_sunucu_kapisinin_aynasi`, `HurdaGirisi_satici_adi_cariden_on_dolar_ama_yazilanin_ustune_yazmaz`, `TakasGirisi_fark_sunucu_formuluyle_ayni`, `TransferGirisi_ayni_subeye_ya_da_urunsuz_gonderilemez`, `FisYaniti_fis_no_uyari_ve_SUNUCUNUN_iskontosu_okunur`, `OdemeKirilimi_bilinmeyen_alan_tutucu_varsayilana_duser`
- **Yapı (5):** `Islemler_razor_1400_satiri_asmaz`, `Fis_bilesenleri_ApiIstemci_enjekte_etmez_ve_istek_atmaz`, `Tek_POST_yolu_Islemler_Kaydet_icinde_kalir`, `Id_envanteri_korunur_hicbir_id_silinmedi`, `Fis_bilesenlerinde_satir_ici_style_artmadi_ve_emoji_yok`
- **bUnit bileşen (4):** `Bacak_ekle_kalani_onerir_tek_kasayi_atar_ve_SAYFAYA_haber_verir`, `Kalani_ata_son_bacagi_fis_toplamina_tamamlar`, `Hurda_satici_tipi_secilince_ad_cariden_dolar_ve_sayfaya_haber_verir`, `Fis_bilesenleri_cizilirken_istek_atmaz`
- **bUnit sayfa (1):** `Takas_eksikken_KAYDET_kilitli_bilesen_doldurunca_ACILIR`. M-S21-10'un ortaya çıkardığı test açığını kapatmak için eklendi.

**POS testleri** (kabul: Karma, İade, Barkod, Teklif, BasitMod + P-10, P-16, UX-4b/4c, UX-1-C1, S3d, Yerleşim, Peşin Müşteri, Döviz, MobilParite, İkon; 22 sınıf, filtre `pos-filtre.txt`). Son koşum **234/235**, S21 ile birlikte. Kalan tek kırmızı `IkonTestleri…emoji` idi, sebebi GorunumKarti.razor (S1-f şeridi). Tam süitte bu artık yeşil.

**Tam süit** (`trx-pS2-1/_Mac_2026-09-23_23_33_33.trx`):

| Koşum | Toplam | Geçti | Kaldı | Kırmızılar |
|---|---|---|---|---|
| Taban (paket öncesi, 22:03) | 4.394 | 4.390 | 4 | `PesinMusteriTestleri.B3_11`, `B3_12`, `B3_15`; `IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI` (GorunumKarti.razor) |
| Son (23:33) | 4.541 | 4.539 | 2 | `B4AltinBacagiTestleri.AltinBacagi_Maden_Ayardan_Gelir_Gumus_Altina_Karismaz`; `TasarimBelirtecTestleri.Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez` |

**Madde (7): bu paketin ürettiği yeni kırmızı = 0.** Son kırmızıların ikisi de bu şeridin dosyalarına dokunmuyor. `B4AltinBacagiTestleri.cs` başka şeridin yeni (izlenmeyen) sunucu testi; SatisFisi'nin gümüş/altın madeni. TasarimBelirtec kırmızısı `app.css`'teki `@media print .gk { color: var(--kenar-yuzey) }` kuralından geliyor (GorunumKarti, S1-f). Taban kırmızılarının dördü başka şeritlerce kapatılmış.

## 7. Mutasyon kanıtları

Yöntem: kaynak yedeklendi → `perl -0777 -pi` ile tek eşleşmeli değişiklik → derleme (yarışta 5 deneme) → POS+S21 filtresi (234–235 test) → yedekten geri alındı → `cmp` birebir. Dosyalar sonunda `TEMIZ-*` kopyalarıyla birebir aynı (11/11 `cmp` AYNI). Son temiz derleme: **0 hata**. Tek uyarı xUnit2029 ve başkasına ait (`S1fGorunumKartiTestleri.cs:460`).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M-S21-1 | `FisHesap.SatirNet`: `if (isk > brut) isk = brut;` silindi | `S21FisSafTestleri.FisHesap_iskonto_brutu_asamaz` | cmp AYNI |
| M-S21-2 | `FisHesap.NakitKapali`: sunucu bayrağı (`tevsikNakitKapali &&`) düşürüldü | `KarmaOdemeEkranTestleri.Bacak_toplami_tuttugunda_KAYDET_ACILIR`, `KarmaOdemeEkranTestleri.Karma_fis_TEK_POST_ile_satis_fisi_ucuna_gider`, `UX4bEkranTestleri.Karma_fis_govdesinde_YALNIZ_BACAK_kasasi_gider`, `S21FisSafTestleri.FisHesap_nakit_kilidi_…` | cmp AYNI |
| M-S21-3 | `FisOdemeBacaklari.BacakEkle` sayfaya haber vermiyor (`Task.CompletedTask`) | `S21FisBilesenTestleri.Bacak_ekle_kalani_onerir_tek_kasayi_atar_ve_SAYFAYA_haber_verir` | cmp AYNI |
| M-S21-4 | `HurdaGirisi.TcknHata`: 11 hane → 10 | `S21FisSafTestleri.HurdaGirisi_satici_kimligi_kurallari_…`, `S3dEkranTestleri.Satici_bilgisi_dolunca_govdede_satici_blogu_gonderilir` | cmp AYNI |
| M-S21-5 | `TakasGirisi.Fark` işareti ters | `S21FisSafTestleri.TakasGirisi_fark_sunucu_formuluyle_ayni` | cmp AYNI |
| M-S21-6 | `FisKalemTablosu.AdetDegistir`: `k.Adet = d;` silindi | `UX1C1EkranTestleri.Pos_kalem_satirinda_ADET_satir_ici_duzenlenir` | cmp AYNI |
| M-S21-7 | `FisYaniti.FisNo`: `fisId`/`id` önceliği ters | `S21FisSafTestleri.FisYaniti_fis_no_uyari_ve_SUNUCUNUN_iskontosu_okunur` | cmp AYNI |
| M-S21-8 | `TransferGirisi.Tamam`: aynı şube kontrolü silindi | `S21FisSafTestleri.TransferGirisi_ayni_subeye_ya_da_urunsuz_gonderilemez` | cmp AYNI |
| M-S21-9 | `FisHurdaAlis`: cari adı ön dolumu kesildi (`CariAdi` → null) | `S21FisBilesenTestleri.Hurda_satici_tipi_secilince_…`, `S3dEkranTestleri.Satici_adi_secili_cariden_on_dolar_ve_kaynagi_yazar` | cmp AYNI |
| M-S21-10 | `Islemler.Gonderilebilir`: `"TAKAS" => true` | İlk koşumda **hiçbir test kırmızı olmadı (test açığı)**. `S21FisSayfaTestleri.Takas_eksikken_KAYDET_kilitli_bilesen_doldurunca_ACILIR` eklendi, tekrarda kırmızı oldu. | cmp AYNI |

**Olay kaydı (Anayasa md.1):** Scratchpad dizini eşzamanlı S1-f şeridiyle ortak çıktı. İlk mutasyon koşumu sırasında o şerit `scratchpad/mut.sh`'nin üzerine kendi betiğini yazdı. Sonuçları:
- M-S21-2 ve M-S21-3 onun betiğiyle koştu (`kuyumcu_test_s1f` DB). Geri alma ve `cmp` AYNI satırları logda duruyor.
- M-S21-4 onun betiğinde yarıda kaldı ve `FisModelleri.cs`'te iki mutasyon (TCKN 10, `id`/`fisId` sırası) geri alınmadan kaldı.

Durum dosya içeriği grep'lenerek fark edildi ve iki satır elle temizlendi. Tüm mutasyonlar bundan sonra yalnız bu şeride ait `scratchpad/pS2-1/s21mut.sh` ile yeniden koşuldu. Bu betik mutasyondan önce dosyanın `TEMIZ-*` kopyasıyla aynı olduğunu `cmp` ile doğruluyor, değilse DURUYOR. M-S21-4…10 bu betikle koştu. M-S21-5 ve M-S21-7'nin ilk koşumu derleme yarışına düştü ("1–2 Hata") ve geçersiz sayılarak tekrarlandı. Tablodaki sonuçlar geçerli koşumlardan.

## 8. Denetim (`dotnet/denetim.sh`)

**Son koşum: `DENETİM TEMİZ.` (EXIT 0, 283 ✓).**

Taban koşumunda 4 ✗ vardı: ArayuzModu 8/6, emoji 2, Kültürsüz ToString 28/27, P-14 BASIT_MOD_TUTARSIZ. Hiçbiri bu şeridin değildi ve sonra başka şeritlerce kapandı.

Bu paketin kendi ara koşumunda iki kırmızı çıktı ve ikisi düzeltildi:
- **P-06 DUZENLEME_KALIBI_YOK_SAYFA / DUZENLENEMEYEN_GIRIS.** Sebep: kalıplar bileşene taşınmıştı. Sapma S4 ile çözüldü; şimdi 0 ve 3/3.
- **B-3 ESIK_ISTEMCIDE 5/4.** Sebep: yeni yazdığım bir yorumda "30.000 TL" geçiyordu. Sayı yorumdan çıkarıldı; şimdi 4/4.

Aynı ara koşumda görülen `P-36b GIRDI_AILESI_DISI_BICIM` kırmızısı Panel.razor:646 kaynaklıydı (S3-A); son koşumda temiz.

Bu şeridi ilgilendiren sayaçların son değerleri: satır içi style 113 (tavan 196) · P-11 SATIR_ICI_STYLE 113/143 · emoji 0 · P-10 DTO_FORM_ESLESMEZ 0 · P-16 DTO_FORM_ESLESMEZ 0 · P-16 PASIF_CARI_SATISTA ekran 3 / sunucu 2 · Kültürsüz ToString 27/27 (taşınan `KalanAdet.ToString("0.###")` sayıyı değiştirmiyor).

## 9. Canlı doğrulama (5192, kuyumcu_net)

**Kurulum.** API, derleme yarışı yüzünden (başka şeridin derlemesi `Kuyumcu.Api` çıktısını geçici olarak siliyordu) `bin/Debug/net8.0`'ın scratchpad kopyasından koşturuldu (`api-bin/`). İçindeki `Kuyumcu.Web.dll` `FisOdemeBacaklari` sınıfını içeriyor (grep ile doğrulandı).
- `GET /` → 200, `<title>MİHENK`.
- Test kiracısı: `PAKETS2-1-TEST` (davetli kayıt).
- Gövdeler `Kaydet()`'in kurduğu şekilde gönderildi. Bacak alanları `FisBacagi`'dan, iskonto `FisHesap`'ten geliyor.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200+id | 200 | kur var |
| 3 | GET /cari/sistem-carisi | 200 | 200 | sistemCarisi=True |
| 4 | POST /cari + /urunler + /kasalar, PATCH stok 10 | 201/201/201/200 | 201/201/**200**/200 | fikstür (kasa oluşturma 200 döndü; kasa=var) |
| 6 | Karma fiş Peşin nakit 5.000 + kart 3.000 + havale 2.000 | 200 + fisId | **200** | fisId var |
| 7 | Karma fiş fark ≠ 0 (6.000/10.000) | 400 | **400** | "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır … (fark -4.000,00 TL)" |
| 8 | %10 fiş iskonto, bacak 10.000 (brüt) | 400 | **400** | "… bacaklar 10.000,00 TL, fiş 9.000,00 TL" — sunucu NET toplamı istiyor, `FisHesap.Toplam` ile aynı |
| 9 | %10 fiş iskonto, bacak 9.000 (net) | 200 + iskontoToplam | **200** | iskontoToplam=1000 (`FisYaniti.IskontoNotu` kaynağı) |
| 10 | Hurda alış + satıcı (TCKN 11 hane, adres boş) | 200 | **400** | "Satıcı adresi zorunludur (VUK md.234 — gider pusulası)" → **§ 10 açık kalan A-1** |
| 11 | Hurda alış satıcı TCKN'siz | 400 | **400** | "Satıcı TCKN zorunludur · Satıcı adresi zorunludur" |
| 12 | GET son satışlar (`FisSonSatislar` kaynağı) | 200 ≥1 | 200 | adet=2 |
| 13 | GET /islemler/fis/{id} (`OdemeKirilimi.Oku` kaynağı) | 200 + 3 bacak | 200 | kalemKapsami=KAYITLI, odemeKalemleri=3 |

**SQL kanıtı** (`scratchpad/pS2-1/canli-sql.txt`):
```
hareketler: SAYIM_DUZELTME 0.00 · SATIS KARMA 10000.00 (nakit 5000, fişli) · SATIS KARMA 9000.00 (nakit 9000, fişli)
odeme_kalemleri: NAKIT 9000 · NAKIT 5000 · KART 3000 · HAVALE 2000 (TRY)
HURDA_ALIS hareketi: 0 (iki hurda denemesi de reddedildi, deftere yazılmadı)
```

**Sonrası.**
- API durduruldu (PID 95973 `kill`). `lsof -iTCP:5192` boş.
- Test kiracısı `betikler/kiraci-temizle.sh --uygula` ile silindi: 70 tablo tarandı, 74 satır + 1 tenant.
- Davet satırı da silindi.
- `tenants` başlangıçta 10'du. Silmeden sonra 11 okundu; fazladan olan başka şeridin **`PAKETS1-f-TEST`** kiracısı, dokunulmadı. Bu şeridin payı 10'a döndü (`ad NOT LIKE 'PAKETS1-f%'` → 10).
- `QA-TEST-*` kiracılarına dokunulmadı.

**Kurucuya not (dürüst kapsam).** Bu paket istemci tarafında bir yapı değişikliği. Canlı sunucu yalnız gövdelerin kabul edildiğini kanıtlıyor. Tarayıcıda gerçek ekran turu **yapılmadı**, çünkü giriş için parola girilmesi gerekiyor ve bu ajan parola girmez. Ekran davranışı 234 POS/S21 bUnit testiyle sınandı. Kurucu 5199 inceleme sunucusunda /islemler'i açıp bir satış kaydederek gözle teyit edebilir.

## 10. Açık kalanlar

- **A-1 (mevzuat, sahibi S-B / mali müşavir).** Sunucu hurda alışta satıcı **adresini** zorunlu tutuyor (VUK md.234 mesajı). Ekrandaki `HurdaGirisi` kuralı ise (bölünme öncesi Islemler.razor'daki kuralın birebir aynısı) adresi zorunlu işaretlemiyor. Sonuç: kullanıcı adresi boş bırakırsa Kaydet açık görünüyor, sunucu 400 dönüyor ve mesaj hata bandına düşüyor (sessiz değil). Bu paket davranış değiştirmediği ve mevzuat kuralı birincil kaynaksız koda yazılmadığı için ekrana adres zorunluluğu **eklenmedi**. Ekran aynası S-2 sonraki paketlerinde ya da S-B'de, kural sahibinin teyidiyle eklenmeli.
- **A-2.** Tam süitte başkalarına ait 2 kırmızı var: `B4AltinBacagiTestleri.AltinBacagi_Maden_Ayardan_Gelir_Gumus_Altina_Karismaz` (B-4 / S-B, sunucu `Modules/Satis`) ve `TasarimBelirtecTestleri.Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez` (S1-f, app.css `@media print .gk`).
- **A-3 (süreç).** Eşzamanlı şeritler aynı scratchpad'i paylaşıyor (`mut.sh` üzerine yazıldı, § 7). Orkestratöre öneri: her şeride kendi alt dizini (`scratchpad/p<paket>/`) zorunlu olsun.
- **A-4.** Takas bloğunda 4 satır içi style kaldı (büyük harf etiket ×2, iç boşluk, üst boşluk). Belirteç sınıfına çevirmek app.css gerektiriyor ve app.css bu şeridin alanında değil. S-2.3 (VERDİM/ALDIM ızgarası) zaten bu bloğu yeniden çizecek.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Islemler.razor ≤ 1.400 satır | ✓ | 1.397 (`wc -l`), `Islemler_razor_1400_satiri_asmaz` |
| id envanteri diff BOŞ | ✓ | 33/33 id, 77/77 geniş envanter, `diff` boş |
| POS testleri yeşil | ✓ | 234/235; tek kırmızı başkasının emoji testiydi, son tam süitte o da yeşil |
| Davranış değişmez | ✓ | Taşıma birebir. Sapmalar S2/S3 yalnız görsel eşdeğer. Canlı kodlar önceki beklentilerle aynı. |
| TEK POST YOLU; bileşen API istemcisi yok | ✓ | 9 POST Kaydet içinde; yapı testi |
| Özellik silinmez | ✓ | Yalnız ölü kod (3 çağrılmayan metot) kaldırıldı |
| Mevcut DOM id'leri değişmez | ✓ | § 3 |
| Sabit renk / yeni satır içi style yok | ✓ | Yeni style yok; 3 style azaldı (116 → 113) |
| CDN / inline script yok | ✓ | Eklenmedi |
| Sozlukler.cs'e dokunulmadı | ✓ | Yeni metin yok; hepsi mevcut `Dil.T` anahtarları |
| Dosya alanı (Islemler.razor, Bilesenler/Fis/*) | ✓ | + yeni test dosyası `Kuyumcu.Tests/S21FisBilesenTestleri.cs` |
| Paket kapısı (7 madde) | ✓ | build 0 hata · 21/21 · denetim TEMİZ · 10/10 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 12. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı:
```
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/   (FisModelleri.cs, FisOdemeKasa, FisOdemeBacaklari, FisKalemGirisi,
                                         FisKalemTablosu, FisHurdaAlis, FisTakas, FisIade, FisTransfer, FisSonSatislar)
?? dotnet/Kuyumcu.Tests/S21FisBilesenTestleri.cs
?? dokumanlar/09-uygulama-kayitlari/PAKET-S2-1-uygulama-raporu.md
```
`Pages/Islemler.razor.mutorj` (0 bayt, 21 Eylül) önceden vardı; bu paketin değil. Commit yapılmadı.

Kanıt dosyaları: `scratchpad/pS2-1/` (`once/`, `TEMIZ-*`, `mutlar*.log`, `tam-*.log`, `denetim-son.log`, `canli-*.txt`, `temizlik.txt`) ve `scratchpad/trx-pS2-1/`.
