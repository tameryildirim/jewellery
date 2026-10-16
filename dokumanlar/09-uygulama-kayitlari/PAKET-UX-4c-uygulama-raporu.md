# PAKET UX-4c — Mantık ve tutarlılık düzeltmeleri (madde 10-13 + Q4 + dalga 1 taşınanları)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 2 · 2026-09-16
> Kapsam: **yalnız** madde 13 → 10 → 11 → 12 → Q4 → (a)(b)(c). Sayım/fiyat yetkisi (Q2) UX-4a'da kapandı, tekrar yazılmadı.
> **COMMIT YAPILMADI.** Çalışma ağacında kalındı.

---

## 1. Durum özeti (kanıtlı)

| Ölçüm | Önce | Sonra |
|---|---|---|
| `dotnet build Kuyumcu.sln --nologo` | 0 hata / **1 uyarı** (xUnit1031) | **0 hata / 0 uyarı** |
| `dotnet test` toplam | 1283 | **1312** |
| Başarısız | 0 | **0** |
| Yeni test | — | **29** (27 ekran/bileşen + 2 DB) |
| `dotnet/denetim.sh` | TEMİZ | **DENETİM TEMİZ** |
| Migration | — | **gerekmedi** (şema değişikliği yok) |
| tenants sayısı | 6 | **6** (test kiracısı silindi) |

Derleme son çıktısı:

```
    0 Uyarı
    0 Hata
```

Test son çıktısı:

```
Test Çalıştırması Başarılı.
Toplam test sayısı: 1312
```

---

## 2. Spesifikasyonun koda karşı doğrulanması (sapmalar ve gerekçeleri)

Spesifikasyon SALT OKUNUR taramadan üretilmişti ve satır numaraları kaymıştı; her kalem yeniden okundu.

| # | Spesifikasyon iddiası | Kodda bulunan | Yapılan |
|---|---|---|---|
| 13 | `catch (ApiHata) { liste = []; }` 8 sayfada | 17 `catch (ApiHata)` bulundu; bunların **9'u** gerçek "listeyi boşalt" kalıbıydı. Cariler.razor:489-508 ve UrunAgaci.razor:345-352 **zaten UX-4a/4b'de düzeltilmişti** | Kalan 9 kalıbın tamamı kaldırıldı; Cariler/UrunAgaci'nde eksik kalan **boş durum kapısı** eklendi (hata varken `BosDurum` çiziliyordu) |
| 13 | Kopya süreli başarı bandı metotları: Kullanicilar, Yetkiler, Ayarlar | Kur.razor ve **Islemler.razor**'da da aynı kopya vardı (toplam 5) | Beşi de silindi; süre `BasariBandi` bileşenine taşındı |
| 12 | `KullaniciVeri` kaydında SubeId yok, ekran subeId göndermiyor | Doğrulandı. Sunucu tarafı (`KullaniciOlusturDto.SubeId`, `KullaniciGuncelleDto.SubeId` + `SubeIdGonderildi`, `Temizle()` içinde `subeId`) **tamamen hazırdı** | Madde 12 **yalnız ekran işi** oldu; sunucuda satır değişmedi, **migration gerekmedi** |
| 11 | `Sayim.razor:108/160` Uygulanan/Atlanan çöpe atılıyor | Doğrulandı (`aktif = y.Detay;`) | Kapanış sonucu banda yazıldı |
| 11 | Tek şubede seçici gizlensin | Gizlemek tek başına YETERSİZ: `#sy-ac` düğmesi `string.IsNullOrEmpty(y.SubeId)` ile kilitli; seçici gizlenince değer boş kalıp düğme sonsuza kadar kilitli kalırdı | `YeniAc()` formu açarken tek şubeyi atıyor; ayrıca `Ac()` içinde ikinci kemer. Test bu kilidi **açıkça** ölçüyor (mutasyon M-UX4c-08) |
| 10 | Ayar dizileri üç ekranda farklı | Doğrulandı: Emanet 11, Urunler/Islemler 9 (K995 ve G999 yok) | `Kuyumcu.Core.Domain.Ayarlar.EkranSecenekleri` eklendi; üç ekran ona bağlandı. **Platin/paladyum bilerek dışarıda** — bu üç ekran altın-gümüş iş akışıdır, mevcut davranış korunur (gerekçe kodda XML yorumda) |
| Q4 | Kartın etiketi `Dil.T.Panel.NetKasa` = "Net Kasa" | Değer zaten **"Günlük Net Akış"**a çevrilmişti (kısmî düzeltme), ama neyin toplandığı hâlâ yazmıyordu | Mühürlü karar A uygulandı: etiket **"Günün nakit akışı"**, kartın altında **"(satış − alış − hurda)"**. Ekranda birleşik okunuşu mühürdeki cümlenin birebir aynısıdır. Değer DEĞİŞMEDİ |
| (a) | Hediye kaleminde liste_fiyati NULL ise indirim sessizce 0 | Doğrulandı ve **daha kötüsü** bulundu: birim fiyat da 0 olduğu için `satirToplam = 0` çıkıyor ve hemen altındaki "hediye 0 bedelle girmelidir" kapısı da SESSİZCE geçiyordu → faturada bedeli hiç görünmeyen hediye kalemi | **Fail-closed 400**: `"{i}. kalemde hediye kaleminde liste fiyatı yok — hediye kalemi liste fiyatı olmadan faturalanamaz"`. `ListeFiyati <= 0` da reddedilir |
| (b) | Faturalar.razor'da **iki** `style="flex:1"` | **Dört** tane vardı (iki modalın alt satırı) | Dördü de `.esnek` sınıfına çevrildi |
| (c) | RotaBaslikTestleri.cs:156 xUnit1031 | Doğrulandı — derlemedeki **tek uyarı** buydu | Test `async Task` yapıldı, çağrı `await` edildi → **0 uyarı** |

**Mevzuat/DOGRULANMALI hükmü:** bu paketin kapsamında yapılandırmadan okunması gereken yeni bir mevzuat hükmü YOK. (a) maddesindeki değişiklik mevcut KDV GUT I/B-10.1 gerekçesini **gevşetmiyor, sıkılaştırıyor** — hiçbir eşik/oran koda sabitlenmedi.

---

## 3. Madde madde yapılanlar

### Madde 13 — hata yutma ve başarı bandı (ortak altyapı, ÖNCE yapıldı)

**Envanter (uygulama öncesi `grep -rn --include='*.razor' "catch (ApiHata)" Pages/`):**

```
Kasalar.razor:267   catch (ApiHata) { KasaListesi = []; }
Kasalar.razor:406   catch (ApiHata) { gecmis = []; }
Urunler.razor:787   catch (ApiHata) { kategoriAgaci = []; }
Urunler.razor:789   catch (ApiHata) { reyonlar = []; }
Urunler.razor:796   catch (ApiHata) { cariler = []; }
Banka.razor:373     catch (ApiHata) { hesaplar = []; }
Banka.razor:382     catch (ApiHata) { hareketler = []; }
Banka.razor:501     catch (ApiHata) { cariler = []; }
Faturalar.razor:654 catch (ApiHata) { faturalar = []; }
Faturalar.razor:671 catch (ApiHata) { cariler = []; }
Emanet.razor:209    catch (ApiHata) { cariler = []; }
UrunAgaci.razor:262 catch (ApiHata) { subeler = []; }
Kur.razor:278       catch (ApiHata) { pariteler = []; }
Panel.razor:285     catch (ApiHata) { /* grafik dolgusu */ }
Islemler.razor:968  catch (ApiHata) { /* önizleme */ }
Islemler.razor:1492 catch (ApiHata) { /* bakiye tazelenemedi */ }
Emanet.razor:221    catch (ApiHata) { kur = null; kurHata = ...; }
```

**Envanter (uygulama sonrası):**

```
Islemler.razor:970   catch (ApiHata) { /* önizleme başarısızsa kapı yine sunucuda işler */ }
Islemler.razor:1494  try { await KasalariYukle(); } catch (ApiHata) { /* bakiye tazelenemedi, kritik değil */ }
Emanet.razor:299     catch (ApiHata) { kur = null; kurHata = Dil.T.Emanet.KurYok; }
```

Kalan üçü **kalıp değildir ve bilerek bırakıldı**: ikisi liste boşaltmaz ve kullanıcıya gösterilecek bir sonuç üretmez (POS tevsik önizlemesi — kapı zaten sunucuda; kasa bakiyesi tazeleme), üçüncüsü zaten **görünür bir hata bandı** doldurur (kur alınamazsa emanet kaydı yapılamaz). Bu üçü için regresyon kapısı `M13_Hata_yutan_kalip_HICBIR_sayfada_KALMADI` testinde tanımlı desen (`catch (ApiHata) { <alan> = []; }`) ile ayrıştırılır.

Yapılanlar:
- `Bilesenler/HataBandi.razor` → isteğe bağlı `Id`; sayfa düzeyindeki bantlar `Id="hata-bandi"`.
- `Bilesenler/BasariBandi.razor` → `SureliMs` (0 = kalıcı), `Id`, `Jeton`. Süre/iptal mantığı artık **tek yerde**; `CancellationTokenSource` `Dispose`'da kapatılır.
- Tüm yakalamalar `catch (Exception ex) when (ex is ApiHata or JsonException)` biçimine çevrildi (sahte API `{}` tuzağı).
- **Boş durum kapısı:** Kasalar, Banka, Faturalar, Cariler, Kullanicilar, Kur (parite) → `else if (<hata> is null && liste.Count == 0)`.
- Kopya süreli bant metotları silindi: Kullanicilar, Yetkiler, Kur, Ayarlar, **Islemler**. Ayarlar'daki `iptalKaynagi` CancellationTokenSource'u da gereksiz kaldığı için kaldırıldı (`Dispose` sadeleşti).
- Panel: `grafikHata` alanı + `#panel-grafik-hata` bandı → "bu ay hiç satış olmadı" ile "rapor ucu cevap vermedi" artık ayrı.

### Madde 10 — Emanet

- Cari seçici **aramaya** çevrildi: `#e-cari-ara` (`oninput`) → `GET /cari?q=...&limit=50` (Cariler.razor deseni). Seçim yine `#e-cari` select'inde (mevcut bUnit sözleşmesi `e.Find("select")` korundu).
- **UX-S29:** sonuç 50'ye değdiyse `#e-cari-sinir` → "aramayı daraltın".
- Çıkış modalına `#c-ayar` + `#c-gram` eklendi; altında `#c-hesaplanan` → `= 9.1600 has (ayar ve gramdan hesaplandı)`. **Gövdede yine `hasGrami` gider** (UX-S21; sunucu sözleşmesi değişmedi, `#c-has` alanı da duruyor → eski testler kırılmadı).
- Ayar listesi tek kaynağa bağlandı (bkz. §2).

### Madde 11 — Sayım

- `Kapat()` → `basariMesaj = string.Format(Dil.T.Sayim.KapanisSonucu, y.Uygulanan, y.Atlanan)` → `#basari-bandi`.
- `#sy-iptal` artık **onay modalı** açar (`#sy-iptal-sayilan` kaç satır sayıldığını yazar, metin "GERİ ALINAMAZ" der); POST yalnız `#sy-iptal-onayla` ile gider. Hata modalın içinde gösterilir.
- Tek şubede `#sy-sube` **çizilmez**, `#sy-sube-tek` salt okunur satır olarak yazılır; `SubeId` form açılışında atanır → `#sy-ac` kilitli kalmaz. İki şubede seçici geri gelir.

### Madde 12 — Kullanıcı

- `KullaniciVeri`'ye `SubeId` eklendi (kayıt sonuna, varsayılanlı → mevcut sahte API yanıtları kırılmadı).
- Listeye **Şube** kolonu (`SubeAd()`: şube yoksa "—").
- Yeni ve düzenle modallarına `#k-sube` / `#d-sube`; **tek şubede** `#k-sube-tek` / `#d-sube-tek` salt okunur + gövdeye otomatik (`SubeGovde()`).
- `#k-aktiflestir-{id}` → `PATCH /kullanicilar/{id} {aktif:true}`. **PATCH sözleşmesi korundu:** başka hiçbir alan gönderilmiyor (test bunu `AlanGonderildi` ile ölçüyor). Hiyerarşi kapısı sunucuda (`HedefUzerindekiYetkiKontrol`) duruyor; ekran yalnız düğmeyi gizliyor.
- Şifre kuralı sunucu regex'iyle **eşitlendi** (`^(?=.*[0-9])(?=.*[a-zA-Z]).+$` + 8-72): `SifreUzunlukTamam / SifreHarfTamam / SifreRakamTamam / SifreGecerli`. `#k-sifre-kural` ve `#s-sifre-kural` canlı kural listesi (UX-S30).
- `/kullanicilar/ben` ucuna **bilerek** `subeId` gönderilmiyor (kullanıcı kendi şubesini seçemez) — kodda yorumlandı.

### Q4 — Panel kartı

Mühürlü karar A: etiket "Günün nakit akışı", alt metin "(satış − alış − hurda)". Değer (`SatisTl - AlisTl - HurdaTl`) **değişmedi**. TR + EN eklendi.

### Dalga 1 taşınanları

- **(a)** `FaturaServisi.FistenOlustur` → hediye kaleminde `ListeFiyati` null/≤0 ise **400**.
- **(b)** Faturalar.razor'daki 4 satır içi `style="flex:1"` → `.esnek` (app.css'te tek tanım, sabit renk yok).
- **(c)** `RotaBaslikTestleri.Dil_TR_den_EN_e_degisince_baslik_da_degisir` → `async Task` + `await`.

---

## 4. DoD sözleşmesi (Bertan koşulu)

Bu pakette **yeni varlık / yeni kayıt türü OLUŞTURULMADI** — dolayısıyla yeni PATCH/DELETE ucu, yeni `[ModulYetkisi]` ve yeni denetim kaydı doğmadı. Aşağıda dokunulan kayıt türlerinin düzenlenebilirlik durumu:

| Varlık / kayıt | Düzenleme yolu | Yetki kapısı | Denetim | Ekranda eylem |
|---|---|---|---|---|
| **Kullanıcı** (`kullanicilar`) | `PATCH /kullanicilar/{id}` (gönderilmeyen alan korunur, açık null şubeyi temizler) + `PATCH /kullanicilar/{id}/rol` + `DELETE` (pasife alma) | `[RolGerekli("SAHIP","MUDUR")]` + `HedefUzerindekiYetkiKontrol` hiyerarşisi | `DenetimYazmaDinleyicisi` (mevcut, yeniden yazılmadı) | **Düzenle · Şifre · Pasife al · Aktifleştir (YENİ)** |
| **Kullanıcı şubesi** (`kullanicilar.sube_id`) | Aynı PATCH; `SubeIdGonderildi` üçlü-durumu korundu | Aynı | Aynı | Yeni/Düzenle modalında `#k-sube` / `#d-sube`; tek şubede salt okunur |
| **Sayım oturumu** (`sayim_oturumlari`) | **Salt okunur — bilerek.** Kapanış ve iptal geri alınamaz; satırlar deftere `SAYIM_DUZELTME` olarak yazılır | Mevcut | Mevcut | Geri alınamaz iki eylem de artık **onay** ister (Kapat: onay kutucuğu; İptal: onay modalı — YENİ). Gerekçe metni `IptalOnayMetin`'de yazılı |
| **Emanet hareketi** (`hareketler`) | **Salt okunur — finansal defter; hard DELETE YASAK.** Düzeltme ters hareketle | Mevcut | Mevcut | Bu pakette **değişmedi**; `DegismezIpucu` + "Düzelt" düğmesi UX-1 Faz B/C kapsamındadır (spesifikasyonda öyle listelenmiş) — açık kalan olarak §8'de |
| **Fatura kalemi** (`fatura_kalemleri`) | Bu pakette yalnız **oluşturma kapısı sıkılaştı**; içerik PATCH'i UX-1 Faz B/C'de | `[RolGerekli]` mevcut | Mevcut | Değişmedi |

Finansal varlıklarda hard DELETE eklenmedi; eklenen tek yazma yolu kullanıcı **aktifleştirme**dir (soft state).

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/UX4c/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı (her satırda "GERİ ALINDI (cmp birebir)").

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M-UX4c-01 | Kasalar: boş durum kapısından `listeHata is null` çıkarıldı | `M13_Kasalar_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ` |
| M-UX4c-02 | Kasalar: `catch (ApiHata) { KasaListesi = []; }` kalıbı geri getirildi | `M13_Hata_yutan_kalip_HICBIR_sayfada_KALMADI`, `M13_Kasalar_500_...` |
| M-UX4c-03 | Emanet: arama sorgusu `"/cari"` (limitsiz) hâline döndürüldü | `M10_Elli_birinci_musteri_ARAMAYLA_secilebilir` |
| M-UX4c-04 | Emanet: `cHas = HesaplananIadeHas` → `cHas = cGram` (has çevirisi yok) | `M10_Cikista_22_ayar_10_gram_govdeye_9_16_HAS_GRAM_olarak_gider` |
| M-UX4c-05 | Core: `EkranSecenekleri` eski 9'luk elle diziye döndürüldü | `M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir` |
| M-UX4c-06 | Sayım: kapanış sonucu mesajı `null` yapıldı | `M11_Kapanista_UYGULANAN_ve_ATLANAN_sayilari_basari_bandinda_yazar` |
| M-UX4c-07 | Sayım: `#sy-iptal` doğrudan `Iptal`e bağlandı (onaysız) | `M11_Iptal_dugmesi_ONCE_ONAY_MODALI_acar_POST_ATMAZ` |
| M-UX4c-08 | Sayım: `YeniAc()` içindeki tek şube ataması silindi | `M11_Tek_subede_sube_secici_GIZLI_ve_govdede_subeId_DOLU` ¹ |
| M-UX4c-09 | Kullanıcı: oluşturma gövdesinden `subeId` çıkarıldı | `M12_Iki_subede_k_sube_secilince_govdede_subeId_gider`, `M12_Tek_subede_secici_GIZLI_ama_govde_yine_DOLU` |
| M-UX4c-10 | Kullanıcı: `YeniGecerli` yalnız uzunluğa baktırıldı (eski hâli) | `M12_aaaaaaaa_sifresinde_KAYDET_KILITLI_kural_listesi_gorunur` |
| M-UX4c-11 | Kullanıcı: "Aktifleştir" dalı `false` yapıldı | `M12_Pasif_kullanici_AKTIFLESTIR_dugmesiyle_geri_acilir` |
| M-UX4c-12 | Kullanıcı: aktifleştirme gövdesine fazladan `ad` eklendi (PATCH sözleşmesi ihlali) | `M12_Pasif_kullanici_AKTIFLESTIR_dugmesiyle_geri_acilir` |
| M-UX4c-13 | Panel: kart `Alt` metni kaldırıldı | `Q4_Panel_karti_NE_OLDUGUNU_soyler` |
| M-UX4c-14 | Panel: `grafikHata` ataması kaldırıldı (sessiz yutma) | `Q4_Panel_grafigi_veri_yok_ile_UC_CEVAP_VERMEDI_yi_ayirir` |
| M-UX4c-15 | API: hediye fail-closed kapısı `if (false)` yapıldı | `Hediye_kaleminde_LISTE_FIYATI_YOKSA_fatura_400_ile_REDDEDILIR`, `Hediye_kaleminde_liste_fiyati_SIFIR_da_REDDEDILIR` |
| M-UX4c-16 | Faturalar: satır içi `style="flex:1"` geri getirildi | `M13_b_Faturalar_ekraninda_SATIR_ICI_stil_kalmadi` |
| M-UX4c-17 | BasariBandi: zamanlayıcı çağrısı kaldırıldı | `M13_BasariBandi_SURELI_ise_kendi_kendine_KAYBOLUR` ² |
| M-UX4c-18 | BasariBandi: jeton karşılaştırması kaldırıldı | `M13_AYNI_mesaj_JETON_artinca_YENIDEN_gorunur` ³ |
| M-UX4c-19 | Cariler: boş durum kapısından `listeHata is null` çıkarıldı | `M13_Cariler_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ` ⁴ |
| M-UX4c-20 | Banka: boş durum kapısından `listeHata is null` çıkarıldı | `M13_Banka_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ` |
| M-UX4c-21 | Faturalar: boş durum kapısından `listeHata is null` çıkarıldı | `M13_Faturalar_500_donunce_hata_bandi_gorunur_bos_durum_GORUNMEZ` |

**Bulunan ve kapatılan 4 test açığı (Anayasa md.1 — gizlenmedi):**

1. **M-UX4c-08 ilk turda HİÇBİR testi kırmadı.** Sebep: bUnit `disabled` düğmeye de olay gönderiyor, `Ac()` içindeki ikinci atama testi kurtarıyordu. Test `Assert.False(e.Find("#sy-ac").HasAttribute("disabled"))` ile güçlendirildi — asıl tuzak (gizlenen seçici yüzünden düğmenin sonsuza kadar kilitli kalması) artık ölçülüyor. Mutasyon tekrarlandı → kırmızı.
2. **M-UX4c-17 ilk turda hiçbir testi kırmadı.** `BasariBandi`'nin süreli davranışını ölçen test yoktu. `M13_BasariBandi_SURELI_ise_kendi_kendine_KAYBOLUR` ve `M13_BasariBandi_SURESIZ_ise_KALICI_kalir` yazıldı. Tekrarlandı → kırmızı.
3. **M-UX4c-18 ilk turda hiçbir testi kırmadı.** `M13_AYNI_mesaj_JETON_artinca_YENIDEN_gorunur` yazıldı (iki kez üst üste aynı işlemi kaydeden kullanıcı ikinci onayı da görmeli). Tekrarlandı → kırmızı.
4. **M-UX4c-19 ilk turda hiçbir testi kırmadı** (Cariler için 500 senaryosu yoktu). `M13_Cariler_500_...` yazıldı. Tekrarlandı → kırmızı.

Son mutasyondan sonra temiz derleme: **0 uyarı / 0 hata** (§1).

---

## 6. Canlı doğrulama (localhost:5198 — 5199'a dokunulmadı)

API `ASPNETCORE_ENVIRONMENT=Development ... --urls http://localhost:5198` ile PID 61217 olarak başlatıldı, iş bitince yalnız o PID durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**; 5199 kurucunun sunucusu **dokunulmadan** ayakta kaldı).

Test kiracısı: `PAKETUX-4c-TEST` (tenant `01M2N3EMYH76H619N6K8D6YPMY`).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| C1 | `POST /kullanicilar` gövdesinde `subeId` | 201 + yanıtta subeId | **HTTP 201**, `"subeId":"01M2N3EMYHCNK91Z8V6D02G8CP"` | yanıt gövdesi okundu |
| C2 | `DELETE /kullanicilar/{id}` (pasife al) | 200 | **HTTP 200** `{"mesaj":"Kullanıcı deaktif edildi"}` | — |
| C3 | `PATCH /kullanicilar/{id}` **yalnız** `{"aktif":true}` | 200; ad/soyad/rol/subeId KORUNUR | **HTTP 200**, `"ad":"Sube","rol":"TEZGAHTAR","subeId":"01M2N3EMYHCNK91Z8V6D02G8CP","aktif":true` | PATCH sözleşmesi canlıda doğrulandı |
| C4 | 60 cari yazıldı, `GET /cari` (sorgusuz) | Sunucu varsayılanı 50 | **adet = 50** (51. müşteri erişilemez — hatanın kendisi) | — |
| C5 | `GET /cari?q=Musteri 51&limit=50` | 51. müşteri gelir | **adet = 1 → ['UX4c Musteri 51']** | madde 10 düzeltmesinin canlı kanıtı |
| C6 | `POST /sayim` (ürünsüz) | 400 | **HTTP 400** "Kapsamda sayılacak ürün yok" | fail-closed doğru |
| C7 | Ürün + stok 2 adet, `POST /sayim/{id}/say` (sayılan 3) | 200 | **HTTP 200** | — |
| C8 | `POST /sayim/{id}/kapat` | `uygulanan`/`atlanan` döner | **`{"uygulanan":1,"atlanan":0,...}`** | madde 11'in banda yazdığı iki sayı |

**SQL kanıtı:**

```sql
SELECT id, email, sube_id, aktif FROM kullanicilar WHERE tenant_id='01M2N3EMYH76H619N6K8D6YPMY';
-- 01M2N3EMYHP5QF4KW1XDVD1GD6  ux4c@test.local    01M2N3EMYHCNK91Z8V6D02G8CP  1
-- 01M2N3F3PAJ4WTBFRN9372MW2J  ux4c-t@test.local  01M2N3EMYHCNK91Z8V6D02G8CP  1

SELECT COUNT(*) FROM cariler WHERE tenant_id='…';            -- 60
SELECT id, ad, durum FROM sayim_oturumlari WHERE tenant_id='…';
-- 01M2N3GZCRYT5AFT71X0PZ766F  UX4c sayim  KAPANDI
SELECT urun_id, sistem_adet, sayilan_adet FROM sayim_satirlari WHERE tenant_id='…';
-- 01M2N3G55TG0V6GV9ZHVJAY6ZG  2.000  3.000
SELECT urun_id, adet FROM stoklar WHERE tenant_id='…';
-- 01M2N3G55TG0V6GV9ZHVJAY6ZG  3.000      ← 2 → 3: "uygulanan:1" gerçekten uygulandı
```

**Temizlik:** `information_schema`'dan `tenant_id` kolonu olan **33 tablo** bulundu, hepsinden test kiracısı silindi, ardından `tenants` satırı silindi. Artık kontrolü: **ARTIK_VAR=0** (33 tablonun hepsinde 0 satır). `SELECT COUNT(*) FROM tenants` → **6** (başlangıçtaki değer). `QA-TEST-1 Kuyumculuk` ve diğer 5 kiracı **dokunulmadı**.

**Canlıda YAPILAMAYAN doğrulama (gizlenmiyor):** (a) maddesinin canlı tekrarı mümkün değil — `liste_fiyati` NULL olan bir hediye hareketi **sunucunun kendi satış yolu üretemez** (`SatisModulu` hediyede liste_fiyatı'nı her zaman yazar). Bu yüzden kapı bir *derinlemesine savunma* kapısıdır ve deftere doğrudan yazan **DB testiyle** (`Hediye_kaleminde_LISTE_FIYATI_YOKSA_fatura_400_ile_REDDEDILIR`) kanıtlandı; mutasyon M-UX4c-15 kapıyı kaldırınca test kırmızıya dönüyor.

---

## 7. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından **bu pakete ait** olanlar:

**API (1):**
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` — (a) hediye fail-closed

**Core (2):**
- `dotnet/Kuyumcu.Core/Domain/Ayarlar.cs` — `EkranSecenekleri` tek kaynağı
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — 17 yeni anahtar × TR + EN

**Web (19):**
- `Bilesenler/BasariBandi.razor`, `Bilesenler/HataBandi.razor`
- `Pages/Ayarlar.razor`, `Banka.razor`, `Cariler.razor`, `Emanet.razor`, `Faturalar.razor`, `Islemler.razor`, `Kasalar.razor`, `Kullanicilar.razor`, `Kur.razor`, `Panel.razor`, `Sayim.razor`, `UrunAgaci.razor`, `Urunler.razor`, `Yetkiler.razor`
- `wwwroot/css/app.css` — `.esnek`, `.kural-listesi`, `.kural-tamam` (sabit renk yok, yalnız `var()`)

**Test (3):**
- `dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs` — **YENİ**, 27 test
- `dotnet/Kuyumcu.Tests/FaturaFistenTestleri.cs` — +2 test
- `dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs` — (c) xUnit1031

Proje ağacına `.mutorj` / `.bak` / `.tmp` **bırakılmadı**; mutasyon yedekleri scratchpad'de tutuldu. (Not: ağaçtaki mevcut `*.mutorj` dosyaları **önceki oturumlardan kalmadır**, bu pakette oluşturulmadı ve silinmedi — silme kurucuya bırakıldı.)

---

## 8. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Sıra 13 → 10 → 11 → 12 → Q4 → (a)(b)(c) | ✅ | Ortak altyapı (BasariBandi/HataBandi) önce yazıldı, sonraki maddeler onu kullandı |
| Madde 13: kalıp TÜM sayfalardan kaldırıldı + envanter raporda | ✅ | §3, öncesi/sonrası grep çıktıları |
| Madde 13: boş durum yalnız başarılı+boşta | ✅ | `M13_Istek_BASARILI_ve_liste_bossa_bos_durum_CIKAR` + 5 sayfada 500 testi |
| Madde 13: en az 3 sayfada adlı 500 testi | ✅ | **5 sayfa**: Kasalar, Banka, Faturalar, Cariler, Kullanicilar |
| Madde 13: JsonException tuzağı | ✅ | Tüm yeni yakalamalar `when (ex is ApiHata or JsonException)` |
| Madde 13: kopya metotlar silindi | ✅ | `M13_Sureli_basari_bandinin_KOPYA_metotlari_silindi` (kaynak taraması) |
| Madde 10: arama, `= hesaplanan has`, gövdede `hasGrami`, tek ayar kaynağı | ✅ | 3 test + M-UX4c-03/04/05 |
| Madde 11: kapanış sonucu, iptal onayı, tek şube | ✅ | 4 test + M-UX4c-06/07/08 |
| Madde 12: şube atama, `Aktifleştir`, şifre kuralı, PATCH ve hiyerarşi korundu | ✅ | 5 test + M-UX4c-09/10/11/12; C1-C3 canlı |
| Q4: mühürlü etiket | ✅ | `Q4_Panel_karti_NE_OLDUGUNU_soyler` + M-UX4c-13 |
| (a) hediye fail-closed + test | ✅ | 2 DB testi + M-UX4c-15 |
| (b) satır içi stil → sınıf | ✅ | `M13_b_...` + M-UX4c-16 |
| (c) xUnit1031 → await | ✅ | Derleme **0 uyarı** |
| UX-S21…S33 | ✅ | S21 (c-hesaplanan), S22 (tek şube/kasa), S25 (Aktifleştir), S27 (kapanış sonucu, iptal onayı), S28 (hata bantları), S29 (aramayı daraltın), S30 (şifre kural listesi), S31 (tek ayar kaynağı) |
| Mevcut DOM id sözleşmeleri kırılmadı | ✅ | 1283 eski testin tamamı yeşil |
| Yeni id'ler `<önek>-<alan>` | ✅ | `e-cari-ara`, `e-cari-sinir`, `c-ayar`, `c-gram`, `c-hesaplanan`, `sy-sube-tek`, `sy-iptal-sayilan`, `sy-iptal-onayla`, `sy-iptal-vazgec`, `k-sube`, `k-sube-tek`, `k-sifre-kural`, `k-aktiflestir-{id}`, `d-sube`, `d-sube-tek`, `s-sifre-kural` |
| Yeni CSS'te sabit renk yok, satır içi stil yok | ✅ | `.esnek`, `.kural-listesi` yalnız `var(--metin-soluk)` / `var(--basari)` |
| Razor özniteliğinde `{ }` lambda yok · `disabled="false"` dizgesi yok | ✅ | Derleme + testler |
| Sozlukler TR + EN, çıplak metin yok | ✅ | 17 anahtar iki dilde |
| Tam süit 0 başarısız | ✅ | 1312/1312 |
| `denetim.sh` TEMİZ | ✅ | §1 |
| Migration | — | **gerekmedi**; şema değişmedi |
| Test kiracısı silindi, tenants eski değerine döndü | ✅ | §6 temizlik |
| 5199'a dokunulmadı | ✅ | `lsof` çıktısı: PID 88090 ayakta |
| COMMIT yok | ✅ | `git status` kirli, hiçbir `git add/commit` çalıştırılmadı |

---

## 9. Açık kalanlar

1. **Emanet hareketinde `DegismezIpucu` + "Düzelt" düğmesi yok.** Spesifikasyon bunu UX-1 Faz B/C'ye atamış (§ "UX-1 Faz B/C'ye eklenen düzenleme boşlukları"); UX-4c kapsamı yalnız madde 10'un üç kalemiydi. `EmanetModulu`'nde hâlâ PATCH/DELETE yok, ters hareket tek yol ve ekran bunu SÖYLEMİYOR.
2. **Sayım satırı geri alma** (yanlış sayılan satırın `sayilanAdet`'ini null'a çekme) sunucuda uç gerektiriyor — madde 11'in mühürlü kapsamında değildi.
3. **Islemler.razor ve Emanet.razor'da kalan 3 `catch (ApiHata)`** (§3) bilinçli bırakıldı; bunlar kalıp değil ama "hiç hata yutma" ilkesinin tam karşılığı da değil. Bir sonraki dalgada POS tevsik önizlemesi için pasif bir uyarı rozeti düşünülebilir.
4. **Panel'de tarih seçici yok** (spesifikasyondaki üçüncü Panel bulgusu). Grafik hatası artık görünüyor ama dönem hâlâ sabit "bugün"; kapsam dışıydı.
5. **Ürün ve POS ekranlarına K995/G999 eklenmesi davranış değişikliğidir**: artık 995 ticari has külçe ürün olarak kaydedilebilir ve hurda alışta seçilebilir. İstenen buydu (spesifikasyonun TERİM bulgusu), ama kuyumcunun mevcut fiyatlandırma listeleri bu ayarları içermiyorsa POS otomatik fiyat veremez — **kurucuya bilgi**.
6. Ağaçta **önceki oturumlardan kalma `*.mutorj` dosyaları** var (`HareketFabrikasi.cs.mutorj`, `SatisModulu.cs.mutorj`, `Sayim.razor.mutorj`, …). Bu pakette oluşturulmadılar; silme hook nedeniyle **kurucuya bırakıldı**.

---

## 10. Kurucuya / mali müşavire notlar

- **(a) hediye kalemi — mali müşavire:** Fişten fatura üretilirken hediye kaleminin **liste fiyatı** artık zorunludur. Gerekçe: KDV GUT I/B-10.1'in "yanında bedelli mal vardır" savunması, hediyenin **belgedeki değerine** dayanır. Daha önce liste fiyatı boş gelen hediye, faturaya 0 bedel + 0 indirim olarak giriyor ve belgede hiç görünmüyordu; bu, bir incelemede savunmasız bir satırdır. Yeni davranış: fatura **kesilmez**, hata verilir. Hiçbir eşik/oran koda sabitlenmedi, mevcut hesap değişmedi.
- **Kullanıcı şubesi — kurucuya:** Artık her kullanıcıya şube atanabiliyor. Şube JWT'ye giriyor ve hareketlere yazılıyor; **mevcut kullanıcıların şubesi boş** olduğu için satış/alış/kasa ekranları onlardan şubeyi sormaya devam edecek. Tek seferlik iş: Kullanıcılar ekranından herkese şubesini atayın; tek şubeli işletmede bu otomatik doldurulur.
- **Sayım iptali — kurucuya:** "İptal" artık onay istiyor ve kaç satırın sayıldığını yazıyor. İptal hâlâ **geri alınamaz** ve sayılan satırlar stoğa uygulanmaz.
- **Panel kartı — kurucuya:** "Net Kasa" kartı gerçek kasa bakiyesi değildi; adı "Günün nakit akışı (satış − alış − hurda)" oldu. **Rakam değişmedi**, yalnız ne olduğu yazıyor. Gerçek kasa bakiyeleri aynı ekranda alttaki Kasalar kartında.
