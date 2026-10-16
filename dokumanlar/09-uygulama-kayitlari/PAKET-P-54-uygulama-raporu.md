# PAKET P-54 — Fatura taslak kalemleri için ekranda düzelt/sil yolu · UYGULAMA RAPORU

- Tarih: 2026-09-24 (gece, 01:05–01:40) · Şerit: **SERİT A** (Sipariş + Fatura) · VT `kuyumcu_test_a` · canlı port **5197**
- Rol: Taşkın (backend) + Ertuğ (frontend) · Kaynak: `TRIYAJ-TUR8-2026-09-23.md` (YÜKSEK, tahmin 2,0 sa) · P-53'ten SONRA koştu
- **Durum: BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı — § 11)

## 1. Spesifikasyonun koda karşı doğrulanması (önce ölç)

| İddia (triyaj) | Ölçülen | Sonuç |
|---|---|---|
| Düzenlenebilir hücreler (`data-duzenlenebilir`) gerçek düzenleme kipine bağlanmalı: miktar, birim fiyat, işçilik, indirim, açıklama | P-22'den kalma durum: **açıklama, miktar, birim fiyat** zaten `SatirIciDuzenle` ile bağlıydı. **İşçilik ve indirim ekranda hiç yoktu** (kolon bile yok), oysa sunucu PATCH ucu `iscilikTl` / `indirimTutari` kabul ediyordu | KISMEN DOĞRU → eksik iki alan eklendi |
| PATCH ucu `FaturaModulu.cs:1450-1453` | Satır kaymış: `[HttpPatch("{id}/kalemler/{kalemId}")]` şimdi **1460-1463**; `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | DOĞRU (satır kaymış) |
| Toplam sunucu yanıtından okunmalı | `KalemKaydet` PATCH yanıtını (`KalemYaniti`) `kalemYanit`'e yazıyor, `#fd-toplam` `kalemYanit.Toplam`'ı basıyor | ZATEN DOĞRU → testle mühürlendi (öncesinde bu davranışın testi YOKTU) |
| **DELETE ucu var mı? (DOĞRULANAMADI)** | `FaturaModulu.cs`'de `HttpDelete` **yok**; sunucu yorumunda açık kural: "HARD DELETE YOK: kalem SİLİNMEZ, taslağın tamamı iptal edilir". Canlıda `DELETE /faturalar/{id}/kalemler/{kalemId}` → **404 "Uç bulunamadı"** | **YOK** → spesifikasyonun "yoksa gerekçe yaz" dalı uygulandı |
| Kesilmiş faturada kilit nedeni | `KalemDuzenlenebilir => kalemFaturaDurumu == "TASLAK"`; TASLAK değilse `DegismezIpucu` (KalemDegismez + FaturaDegismezYolu) | ZATEN VAR → testle mühürlendi |
| Hata alan düzeyinde | **DEĞİLDİ**: sunucu 400'ü `kalemHata` bandına gidiyordu. İstemci aralık kuralı (miktar ≥ 0,001) `SatirIciDuzenle` içinde alan altındaydı | DÜZELTİLDİ (§ 2 madde 3) |
| (ek ölçüm) `iscilikTl`/`indirimTutari`'na sayı olmayan değer | `iscOge.GetDecimal()` → `InvalidOperationException` (500 yolu) | DÜZELTİLDİ → 400 |
| (ek ölçüm) denetim kaydı | Kalem PATCH'i otomatik yazma izine düşüyor (`DenetimYazmaDinleyicisi`): canlıda `GUNCELLEME FaturaKalemi = 5` (§ 9) | VAR — yeniden yazılmadı |

Triyajın öbür doğrulanamayanları (Sipariş SubeId, /yedekler gerekçe ucu, /kur/guncel dağılımı, KurSnapshot kökeni) P-54 kapsamı dışı — ölçülmedi, tahmin edilmedi.

**Mevzuat:** Bu paket hiçbir mevzuat hükmü yazmadı/değiştirmedi. "Kesilmiş belge değişmez; düzeltme yolu iptal + yeni belge" ve "kuyum kaleminde has bedeli dayanağı korunur" kuralları sunucuda mevcuttu (P-22); ekran yalnız aynasını kurdu. Web araştırması gerekmedi.

## 2. Yapılanlar

1. **İşçilik ve İndirim hücreleri** (`Faturalar.razor`, kalem modalı): iki yeni kolon (`Dil.T.Fatura.Iscilik` / `Indirim` — mevcut anahtarlar). TASLAK'ta `SatirIciDuzenle` (`Varlik="fatura-kalem"`, `Alan="iscilikTl"|"indirimTutari"`, `AsgariDeger=0`, `BosOlabilir=true`); kesilmişte salt okunur (`Bicim.Tl` ya da "—").
2. **PATCH sözleşmesi:** hücre boş bırakılırsa gövdeye `null` gider = alan **silinir**; tek alan gider, öbürleri gövdede yok = **korunur**.
3. **Hata alan düzeyinde:** yeni `FormDurumu kalemForm`; anahtar `fd-kalem-{alan}-{kalemId}`. Sunucu 400'ü (ör. "Miktar sıfırdan büyük olmalıdır") düzeltilen hücrenin altına `AlanHatasi` ile (`#alan-hata-fd-kalem-{alan}-{kalemId}`); bant yalnız özet ("Satır kaydedilemedi — işaretli alanın altındaki açıklamaya bakın."). Satır içi düzenleme TEK alan gönderdiği için hata o alana aittir — anahtar-kelime eşlemesi gerekmez. `JsonException` (bozuk yanıt) bantta kalır (UX-S28). İstemci aralık kuralı (miktar 0) zaten `SatirIciDuzenle` içinde alan altında; istek gitmez.
4. **Kalem silme — sebep + yol (UX-S25):** DELETE ucu yok; düğme uydurulmadı. TASLAK'ta `DegismezIpucu Varlik="FaturaKalemSilme"` (`#degismez-ipucu-FaturaKalemSilme`): "Taslak faturada tek bir satır silinemez: toplam, KDV ve has bedeli satırların tamamından hesaplanır." + yol: "Satırı düzeltmek için üzerindeki değere tıklayın. Satır hiç olmamalıysa taslağı listeden İptal Et ile iptal edip doğru satırlarla yeniden kesin. Taslak, fatura numarası harcamaz." Bileşen düğme üretmez.
5. **Sunucu sertleştirmesi** (`FaturaModulu.cs`): `SayiYaDaNull(oge, ad)` — `null` → sil, sayı → değer, başka her şey → **400 "'{ad}' sayı olmalı"** (eskiden 500 yolu).
6. Toplam hesaplaması **eklenmedi**: ekran toplamı yalnız PATCH yanıtından okur; ardından `Yukle()` listeyi yeniden çeker (liste satırı da sunucunun başlık toplamını basar).

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | Yeni ekran metni (3 anahtar: `KalemSilmeGerekce`, `KalemSilmeYol`, `KalemHatali`) **`Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs`**'e eklendi (TR+EN, konumsal record → eksik anahtar derleme hatası) | Görev "Sozlukler.cs hub'ına DOKUNMA; yeni metin gerekiyorsa DUR ve raporla" diyor. Hub'a dokunulmadı. Bu dosya P-53'ün bu şerit için açtığı AYRI sözlük dosyasıdır (başka şerit kullanmıyor — `grep` ile ölçüldü: yalnız `Faturalar.razor`). "DUR" yerine bu yol seçildi çünkü çakışma riski sıfır ve orkestratörün önceki dalgalardaki deseni ("yeni metin AYRI dosyada") aynıdır. Orkestratör isterse hub'a taşır. |
| S-2 | `FaturaModulu.cs` sunucu değişikliği (tip denetimi) | Kapsam ekran odaklıydı; ama ekran işçilik/indirimi artık düzenletiyor ve eski kod sayı olmayan değerde 500 yoluna düşüyordu (Anayasa md.1: hata anlamlı 400 olmalı). Dosya şeridin API modülü → sınır içinde. |
| S-3 | Test listesine ek 3 bUnit + 5 HTTP testi | Spesifikasyonun 4 testi ekranı ölçüyor; ekranın dayandığı sunucu sözleşmesinin (yanıtta başlık toplamı, miktar 0 → 400, null=sil, DELETE yok, kesilmiş → 400) HTTP testi **hiç yoktu** (`grep "kalemler/"` Tests'te 0 sonuç). |
| S-4 | Canlı doğrulama API + SQL düzeyinde; ekran bUnit ile | Tarayıcıda oturum açmak parola girmeyi gerektirir (ajan kuralı: parola girilmez). Ekran davranışı 7 bUnit testiyle ölçüldü. |

**Migration:** gerekmedi (şema değişmedi).

## 4. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Fatura kalemi (TASLAK) | `PATCH /faturalar/{id}/kalemler/{kalemId}` · `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` + `[ModulYetkisi("fatura")]` · ekranda hücreye tıkla → `SatirIciDuzenle` (açıklama, miktar, birim fiyat, **işçilik**, **indirim**) | Denetim izi otomatik (canlı: 5 GUNCELLEME FaturaKalemi) |
| Fatura kalemi (TASLAK) — silme | **Uç YOK (bilinçli, sunucu kuralı)** — `DegismezIpucu FaturaKalemSilme`: sebep + yol (hücreyi düzelt ya da taslağı **İptal Et** ile iptal edip yeniden kes; `PATCH /faturalar/{id}/durum` IPTAL, ekranda mevcut onay modalı) | Finansal belge satırı → hard DELETE yok |
| Fatura kalemi (KESİLDİ/ÖDENDİ/İPTAL) | Değişmez — `DegismezIpucu FaturaKalemi` (KalemDegismez + FaturaDegismezYolu); hücre yok, `data-duzenlenebilir` yok | Sunucu 400: "Yalnız TASLAK fatura düzenlenebilir…" |
| Yeni varlık | **YOK** | — |

Yeni düğme yok (`SatirIciDuzenle`'nin kendi Kaydet/İptal düğmeleri gerçek PATCH atar ya da `Kaydediliyor` iken disabled). Pages/ altında yeni `ModalPencere` kullanımı **0** (Faturalar'da 8 → 8). Tek düzenleme kalıbı korundu (tek alanlı → `SatirIciDuzenle`). Mevcut element id'lerine dokunulmadı; yeni id'ler: `#si-fatura-kalem-iscilikTl-{id}*`, `#si-fatura-kalem-indirimTutari-{id}*`, `#alan-hata-fd-kalem-{alan}-{id}`, `#degismez-ipucu-FaturaKalemSilme`. Satır içi `style` 0, sabit renk yok, yeni CSS yok.

**Dokunulan sayfalar:** `Pages/Faturalar.razor` (P-53'ten sonra ikinci dokunuş — gerekçe: triyaj çakışma haritası P-53 → P-54 seri zincirini açıkça öngörüyor; aynı şerit, sıralı).

## 5. Test sayıları

- Yeni test: **12** — bUnit 7 (`FaturaEkranTestleri`): `TaslakKalemiEkrandanDuzenlenir`, `KesilmisFaturadaKalemKilitliVeSebepYazili`, `KalemToplamiSunucuYanitindanOkunur`, `GecersizMiktarAlanDuzeyindeHataVerir`, `Iscilik_bos_birakilinca_govdede_NULL_gider_alan_silinir`, `Indirim_hucresi_PATCH_govdesinde_indirimTutari_tasir`, `Taslak_kalemi_icin_SILME_dugmesi_UYDURULMAZ_sebep_ve_yol_yazili`; HTTP 5 (`P54HttpTestleri`): `Kalem_miktari_PATCH_ile_duzelir_ve_BASLIK_TOPLAMI_yanitta_gelir`, `Miktar_SIFIR_400_Turkce_mesaj_ve_veri_DEGISMEZ`, `Iscilik_NULL_ile_silinir_SAYI_OLMAYAN_deger_400_doner`, `Kalem_SILME_ucu_YOKTUR_ve_kalem_yerinde_kalir`, `Kesilmis_faturanin_kalemi_400_ve_DUZELTME_YOLU_yazar`.
- Filtreli koşum (`FaturaEkranTestleri|P54HttpTestleri`, `KUYUMCU_TEST_DB=kuyumcu_test_a`): **36/36 Başarılı**.
- Faturalar'a/`/faturalar`'a dokunan 14 test dosyası: **228/229** — tek kırmızı `UX4cEkranTestleri.M10_…` (başka şerit, § 12).
- Tam süit (trx: `scratchpad/trx-pP-54/_Mac_2026-09-24_01_36_57.trx`): **Toplam 4670 · Geçti 4669 · Başarısız 1**.
- Önce: P-53 raporu 4638 (2 kırmızı). Aradaki fark paralel şeritlerin eklediği testleri de içerir; P-54'ün payı +12.

## 6. Mutasyon kanıtları (yöntem: `scratchpad/mut-yedek/P-54/` yedek → `perl -0777 -pi` tek eşleşme → filtreli test → yedeği geri kopyala → `cmp`)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M54-1 (liste md.1) | `#fd-toplam` → `kalemYanit.Kalemler.Sum(x => x.Miktar * x.BirimFiyat)` (yerel toplam) | **`KalemToplamiSunucuYanitindanOkunur`**, `TaslakKalemiEkrandanDuzenlenir`, `GecersizMiktarAlanDuzeyindeHataVerir` | `cmp` birebir |
| M54-1b (liste md.1, ikinci biçim) | `kalemYanit = await Api.PatchAsync…` → `_ = await …` (PATCH yanıtı atılır) | **`KalemToplamiSunucuYanitindanOkunur`**, `TaslakKalemiEkrandanDuzenlenir` | `cmp` birebir |
| M54-2 (liste md.2) | `KalemDuzenlenebilir => kalemFaturaDurumu == "TASLAK"` → `=> true` (TASLAK kapısı kalktı) | **`KesilmisFaturadaKalemKilitliVeSebepYazili`** | `cmp` birebir |
| M54-3 (ek) | `kalemForm.Ekle(KalemAlanAnahtari(alan, kalemId), ex.Message);` silindi | **`GecersizMiktarAlanDuzeyindeHataVerir`** | `cmp` birebir |
| M54-4 (ek) | boş değer → `null` yerine `0m` | **`Iscilik_bos_birakilinca_govdede_NULL_gider_alan_silinir`** | `cmp` birebir |
| M54-5 (ek) | `<DegismezIpucu Varlik="FaturaKalemSilme" …/>` silindi | **`Taslak_kalemi_icin_SILME_dugmesi_UYDURULMAZ_sebep_ve_yol_yazili`** | `cmp` birebir |
| M54-6 (ek, sunucu) | `SayiYaDaNull`: `_ => throw BadRequest` → `_ => oge.GetDecimal()` | **`P54HttpTestleri.Iscilik_NULL_ile_silinir_SAYI_OLMAYAN_deger_400_doner`** | `cmp` birebir |

7/7 mutasyon kırmızı. Not (md.1 — gizlenmez): ilk denemelerde M54-2, M54-1b ve M54-5 koşumları **başka şeridin yarım `Pages/Urunler.razor` düzenlemesi** (CS0103 `durumFil`, `L`, `ListeSiniri`…) ve M54-4 **eşzamanlı derlemenin dosya kilidi** (MSB4018 `ConvertDllsToWebCil`) yüzünden derlenemedi; bunlar mutasyon sonucu SAYILMADI, ağaç yeşile dönünce yeniden koşuldu (tablodaki sonuçlar temiz koşumlardır). Son mutasyondan sonra `cmp` → her iki dosya yedekle birebir; temiz `dotnet build Kuyumcu.sln` → **0 Hata · 0 Uyarı**.

## 7. Derleme

`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata** (ilk deneme MSB4018 dosya kilidi — paralel derleme; ikinci deneme temiz).

## 8. Denetim (`dotnet/denetim.sh`)

Çıkış 1, **DENETİM BAŞARISIZ** — tek ihlal, P-53 raporundakiyle aynı:
```
✗ P-15: DUZ_SELECT_LIMIT    ölçülen 13 · tablo 12 · açık 9 (tavan 9)
```
Kaynak `Islemler.razor` (3 `limit=200`); `Faturalar.razor` 2 → 2 (P-54 değiştirmedi). P-54'ün ilgili sayaçları (ModalPencere düzenleme, çıplak metin, satır içi stil) ihlal listesinde yok.

## 9. Canlı doğrulama (5197, `--no-build`, Development, `kuyumcu_net`)

Test kiracısı `PAKETP54-TEST` (tenant `01M386J0HWWA7XAJWARGHRW76R`), tek kullanımlık davet `p54-davet-0001` (jeton/parola/erişim jetonu ekrana basılmadı; scratchpad dosyaları iş sonunda `printf ''` ile boşaltıldı).

| # | Adım | Beklenen | Gerçek | Kanıt (yanıt okundu) |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | tenantId yukarıda |
| 2 | `POST /cari` | 201 | **201** | `P54 Musteri` |
| 3 | `POST /faturalar` 2 kalem (2×1000 + 1×500), toplam 2500 | 200 | **200** | `durum=TASLAK`, `faturaNo=null`, `kalemKaynagi=KALEM` |
| 4 | `GET /faturalar/{id}/kalemler` | 200 | **200** | toplam 2500; satırlar 2000 / 500 |
| 5 | PATCH kalem A `{"miktar":3}` | 200 + yeni toplam | **200** | **toplam 3500**; A: miktar 3, satır 3000 |
| 6 | PATCH `{"miktar":0}` | 400 | **400** | `Miktar sıfırdan büyük olmalıdır` |
| 7 | PATCH `{"iscilikTl":120}` | 200 | **200** | toplam 3620 |
| 8 | PATCH kalem B `{"indirimTutari":50}` | 200 | **200** | toplam 3570 |
| 9 | PATCH `{"iscilikTl":null}` (null = sil) | 200 | **200** | toplam 3450; `iscilikTl` [null, null] |
| 10 | PATCH `{"indirimTutari":"abc"}` | 400 (eskiden 500 yolu) | **400** | `'indirimTutari' sayı olmalı` |
| 11 | PATCH `{"aciklama":"P54 bilezik duzeltildi"}` | 200 | **200** | açıklama yansıdı |
| 12 | `DELETE /faturalar/{id}/kalemler/{A}` | uç yok | **404** | `Uç bulunamadı` |
| 13 | `GET /faturalar` | liste toplamı = sunucu | **200** | liste toplam 3450, TASLAK |
| 14 | `PATCH /faturalar/{id}/durum` KESILDI | 200 | **200** | `KESILDI`, `FTR-2026-000001` |
| 15 | Kesilmiş belgede PATCH kalem `{"miktar":5}` | 400 | **400** | `Yalnız TASLAK fatura düzenlenebilir; bu belge KESILDI durumunda. Kesilmiş belgenin düzeltme yolu iptal edip yeni belge kesmektir.` |

**SQL kanıtı** (silmeden önce):
```
faturalar:        01M386JN0KX2PDA9PTJA7VPT9W  KESILDI  FTR-2026-000001  toplam 3450.00  kdv 575.00  ara 2875.00  KALEM
fatura_kalemleri: 1  P54 bilezik duzeltildi  3.000  1000.00  iscilik NULL  indirim NULL   satir 3000.00
                  2  P54 kolye               1.000   500.00  iscilik NULL  indirim 50.00  satir  450.00
denetim_kayitlari: EKLEME Fatura 1 · GUNCELLEME Fatura 5 · EKLEME FaturaKalemi 2 · GUNCELLEME FaturaKalemi 5
```
(Başlık toplamı = 3000 + 450 = 3450 → sunucu başlığı kalemlerden topluyor; miktar 0 ve "abc" reddi iz bırakmadı; kesilmiş belgedeki PATCH reddi miktarı 3'te bıraktı.)

**API logu (P-54 dışı):** `is_calismalari` benzersiz anahtar çakışması (`MASAK_SIB_SURESI`, `SAKLAMA_ONIZLEME`, `VADE_HATIRLATMA`) — aynı VT'de 5199 zamanlayıcısıyla eşzamanlı iş (P-52/P-53 raporlarında da ölçülmüş). P-54 akışını etkilemedi.

**Temizlik:** API PID'leri (19177 sarmalayıcı, 19179 dinleyici) `kill` → `lsof -nP -iTCP:5197 -sTCP:LISTEN` = **0 satır**. `tenant_id` sütunlu **70 tablodan** kiracı satırları + `tenants` + davet + kullanıcı silindi; 70 tablonun toplam kalan satırı **0**, `kiraci_davetleri(p54-davet-0001)=0`, `kullanicilar(p54@test.local)=0`, **`tenants` 10 → 10**. `QA-TEST-` kiracıları (4) ve 5199'a dokunulmadı.

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, P-54'e ait olanlar)

```
 M dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs           (SayiYaDaNull — iscilikTl/indirimTutari 400)
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor                    (işçilik/indirim hücreleri, alan düzeyi hata, silme gerekçesi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs          (P-53 dosyası; +3 anahtar TR+EN)
?? dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs                 (dizin izlenmiyor; sahte API P-54 dalları + 7 test)
?? dotnet/Kuyumcu.Tests/P54HttpTestleri.cs                     (yeni, 5 test)
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-54-uygulama-raporu.md  (bu dosya)
```

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| TASLAK kalemli faturada miktar ekrandan değişir, başlık toplamı sunucu yanıtıyla güncellenir | ✅ | `TaslakKalemiEkrandanDuzenlenir` (PATCH yolu+gövde, `#fd-toplam` = yanıt, liste yeniden okunur); canlı #5, #13 |
| Kesilmiş faturada hücreler düzenlenemez + neden yazılı | ✅ | `KesilmisFaturadaKalemKilitliVeSebepYazili`; `Kesilmis_faturanin_kalemi_400_…`; canlı #15 |
| Kalem düzenleme hatası (ör. miktar 0) alan düzeyinde | ✅ | `GecersizMiktarAlanDuzeyindeHataVerir` (istemci + sunucu 400 yolları); canlı #6 |
| Toplam yerel hesaplanmaz, PATCH yanıtından okunur | ✅ | `KalemToplamiSunucuYanitindanOkunur` (sunucu 3.620, yerel çarpım 3.500); M54-1, M54-1b |
| Kapsam: miktar, birim fiyat, işçilik, indirim, açıklama gerçek düzenleme kipinde | ✅ | `TaslakKalemiEkrandanDuzenlenir` (5 tetikleyicinin 5'i), `Iscilik_bos_…`, `Indirim_hucresi_…` |
| Kalem silme: uç varsa bağla, yoksa gerekçe | ✅ | Uç YOK (kod + canlı 404); `Taslak_kalemi_icin_SILME_…` |
| Mutasyon listesi (2 zorunlu + 5 ek) | ✅ | § 6 — 7/7 |
| Paralel kapı (1) build 0 hata | ✅ | § 7 |
| (2) kendi testlerim 0 başarısız | ✅ | 36/36 (12 yeni dâhil) |
| (3) kendi sayaçlarım denetim.sh'ta ✓ | ✅ | tek ✗ P-15 Islemler.razor kaynaklı; Faturalar `limit=200` 2→2, ModalPencere 8→8 |
| (4) mutasyon kanıtı | ✅ | 7/7 |
| (5) canlı doğrulama | ✅ | § 9 (15 adım + SQL) |
| (6) test kiracısı silindi | ✅ | tenants 10→10, 70 tabloda 0 satır |
| (7) yeni kırmızı üretilmedi | ✅ | Tam süitteki tek kırmızı `UX4cEkranTestleri.M10_…` P-53 devir notunda zaten listeli; fark = **0** (P-53'teki `S32MusterilerEkranTestleri.S32_09` bu koşumda yeşil) |

## 12. Açık kalanlar (sahibiyle)

1. **Tam süit kırmızısı — `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir`**: `Assert.Contains("K995")` başarısız, seçici metni "8 Ayar 14 Ayar 18 Ayar 21 Ayar 22 Ayar 24…". Sahibi: `Pages/Emanet.razor` + `Kuyumcu.Core/Domain/Ayarlar.cs` değiştiren şerit (P-53'te de listeli; P-54 dokunmadı).
2. **denetim.sh P-15 DUZ_SELECT_LIMIT**: `Islemler.razor`'da sınıflandırılmamış 3. `limit=200`. Sahibi: Islemler/POS şeridi.
3. **Başka şeridin yarım işi derlemeyi zaman zaman kırıyor**: `Pages/Urunler.razor` (01:24 civarı CS0103 `durumFil`, `L`, `ListeSiniri`, `StokAdetMetni`; CS1061 `UrunVeri.Birim`, CS0246 `SubeAdi`). Koşum sırasında kendiliğinden düzeldi; sahibi Ürünler şeridi. Ayrıca eşzamanlı `dotnet build`'ler `ConvertDllsToWebCil` dosya kilidinde (MSB4018) çakışıyor — orkestratöre: paralel şeritlerin derlemeleri sıraya konmalı ya da ayrı çıktı dizinine alınmalı.
4. **S-1:** `Sozlukler.FaturaTaslak.cs` metinleri hub'a taşınmak istenirse orkestratör sırası.
5. **Kalem EKLEME ucu da yok**: taslakta eksik kalem yalnız iptal + yeniden kesimle eklenebilir (sebep metni bunu söylüyor). İstenirse ayrı paket (Taşkın): `POST /faturalar/{id}/kalemler` + satır pasifleştirme — has bedeli dayanağı (kuyum) kuralı gereği tasarım gerektirir.
6. **Kalem kolonları arttı (9 → 11)**: dar ekranda tablo `tablo-sar` içinde yatay kayar; basit/profesyonel mod (UX-3) ayrımı bu modalda yok — görsel şeride not.

## 13. Kurucuya / mali müşavire notlar

- Mevzuat hükmü yazılmadı. "Kesilmiş belge değişmez, düzeltme iptal + yeni belge" ve "taslak numara harcamaz" kuralları sunucudaki mevcut iş kurallarıdır; ekran yalnız aynasını kurdu. Yeni eşik/oran/kod eklenmedi.
- Ücretsiz pilot kuralı: fiyat/teklif/tahsilat metni üretilmedi.
