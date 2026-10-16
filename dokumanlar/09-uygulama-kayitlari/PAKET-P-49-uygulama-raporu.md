# PAKET P-49 — /kurulum mühür ayrımı: ayrı mühür gerekçesi + işletme adı yazdıran onay

- **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus uygulayıcı
- **Dalga:** YAYIN HATTI + ÖLÇÜM ARACI (tur 8 triyajı Katman A + P-49)
- **Kaynak:** `TRIYAJ-TUR8-2026-09-23.md` (P-49, KRİTİK) + görev metnindeki ek koşullar
- **Süre:** triyaj tahmini 2,0 saat (tahmin) · **gerçek süre ≈ 0,5 saat** (23.09.2026 19:19 → 19:48, ölçüm: `date`)
- **Durum:** BİTTİ (paket kapısının 7 maddesi sağlandı — aşağıda kanıtlarıyla)

---

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (spesifikasyon) | Kodda bulunan | Sonuç |
|---|---|---|
| Dosya ayak izi (4 dosya) | `test -e` → dördü de VAR (Kurulum.razor 731, KurulumModulu.cs 364, KurulumEkranTestleri.cs 351, KurulumHttpTestleri.cs 332 satır) | DOĞRU |
| Mühür ile açılış aynı gerekçeyi kullanıyor | `MuhurAcik => … && !GerekceKisa` ve `MuhurEksikleri` açılışın `gerekce` alanına bakıyordu; mühür gövdesi `gerekce = gerekce.Trim()` | DOĞRU |
| Gövdede sabit `onay = true` | `OnayEvet` → `/kurulum/muhurle` gövdesinde `onay = true` sabit | DOĞRU |
| "Onay kipi YOK" (QA) | YANLIŞ — Kurulum.razor 362-375'te ortak `ModalPencere` vardı; ayırt edici ikinci kapı yoktu | Spesifikasyon notu doğru (satır 361-373 → gerçek 362-375, kayma 1-2 satır) |
| Sunucu işletme adını karşılaştırmıyor | `KurulumMuhurDto` yalnız `Gerekce` + `Onay` taşıyordu | DOĞRU |

**Sapma yok**; yalnız uygulama gereği iki yardımcı dosyaya dokunuldu (bkz. § 8).

## 2. Yapılanlar

### Backend (`Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs`)
- `KurulumMuhurDto.IsletmeAdiOnayi` (string?, DataAnnotations YOK — kural servis içinde elle denetlenir).
- `KurulumServisi.Muhurle`: gerekçe → `Onay` bayrağı → **işletme adı kapısı** → zorunlu adımlar sırasıyla. Ad boş/yanlışsa
  `400 "Mühürleme geri alınamaz — onaylamak için işletme adını birebir yazın"`. Ekran atlanıp uç doğrudan çağrılsa da kapı ayakta.
- `IsletmeAdiEslesir(girilen, kayitli)` — TEK KURAL: baş/son boşluk hariç **birebir** (Ordinal, büyük/küçük harf duyarlı);
  kayıtlı ad boşsa **fail-closed** (hiçbir girdi eşleşmez).
- `GET /kurulum/durum` yanıtına `isletmeAdi` eklendi (UX-S21: sistem bilir, ekran uydurmaz). `KurulumDurumYaniti`
  (`Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs`) sona isteğe bağlı `string? IsletmeAdi = null` parametresi aldı —
  `SozlesmeSekilTestleri` şekil kilidi yeşil.
- Yetki değişmedi: controller `[RolGerekli("SAHIP","MUDUR")]` + `[ModulYetkisi("kurulum")]`, uç `[RolGerekli("SAHIP")]`.
  Denetim izi mevcut `denetim.YazmaKaydet(..., "kurulum MÜHÜRLENDİ · …")` çağrısıyla (yeniden yazılmadı).

### Frontend (`Kuyumcu.Web/Pages/Kurulum.razor`)
- Mühür kartına **ayrı** `GerekceAlani Id="kur-muhur-gerekce"` (etiket "Mühür gerekçesi", asgari = sunucunun `gerekceAsgari`).
- `muhurGerekcesi` değişkeni açılış `gerekce`sinden **bağımsız**; `MuhurAcik` ve `MuhurEksikleri` artık `MuhurGerekceKisa`'ya bakar.
  Açılış tarafı (`GerekceKisa`, Uygula, Geri al) **değişmedi**.
- Onay modalı mühür kipinde **type-to-confirm**: `Metin ElemanId="kur-onay-isletme-adi"` (etiket: "Onaylamak için işletme adını birebir yazın: {ad}").
  Ad birebir değilse `#kur-onay-evet` **disabled**, yanında `#kur-onay-kilit` sebep satırı (UX-S30) ve `aria-describedby`;
  yanlış yazımda alan altında Türkçe hata (`#kur-onay-isletme-adi-hata`, DoD EK madde 3). Modal her açılışta ad alanını sıfırlar.
- Gövde: sabit `onay = true` **kaldırıldı** → `onay = IsletmeAdiDogru`, `isletmeAdiOnayi = <yazılan>`, `gerekce = muhurGerekcesi`.
- `OnayEvet` mühür kipinde ad birebir değilse hiçbir istek atmaz (savunma derinliği).
- Geri al kipi: modal, düğme, gövde **aynı** (ad alanı yalnız `onayTuru == "muhur"`).

### Sözlük (`Kuyumcu.Core/I18n/Sozlukler.cs`, TR + EN)
- `MuhurOnay` metni: "Bu işlem geri alınamaz. … **tek düzeltme yolu storno** (ters kayıt ya da cari düzeltme fişi) olur." (EN karşılığı eklendi).
- Yeni anahtarlar: `MuhurGerekce`, `MuhurIsletmeAdiYaz`, `MuhurIsletmeAdiUyusmuyor` (TR+EN; eksik anahtar derleme hatası verir).

## 3. Migration
**Gerekmedi** — şema değişikliği yok (`IsletmeAdiOnayi` saklanmaz, yalnız istekte karşılaştırılır; ad `tenants.ad`'dan okunur).

## 4. Testler

| Kapsam | Önce | Sonra |
|---|---|---|
| KurulumEkranTestleri | 11 | 15 (+4) |
| KurulumHttpTestleri | 13 | 14 (+1) |
| Yeni test toplamı | — | **5** |

Yeni: `KurulumEkranTestleri.Acilis_gerekcesi_muhur_dugmesini_etkinlestirmez` · `Ayri_muhur_gerekcesi_dolunca_dugme_etkinlesir` ·
`Onay_kipinde_isletme_adi_yazilmadan_evet_disabled` · `Onay_kipi_metni_geri_donulemezligi_ve_storno_yolunu_yazar` ·
`KurulumHttpTestleri.Muhur_isletme_adi_onayi_yanlissa_400`.

Güncellenen mevcut testler (sözleşme değiştiği için gövdeye ad eklendi; davranış beklentisi aynı):
`Muhurlu_kiracida_acilis_uygula_409` (gövdeye doğru ad), `Muhur_ONAYSIZ_400` (doğru ad + `onay=false` → 400'ü YALNIZ bayrak üretsin; ayrıca kiracının mühürsüz kaldığı sınanır),
`Muhurleme_ONAY_MODALI_ister_ve_onaylanmadan_istek_ATILMAZ` (mühür gerekçesi ayrı alana, Evet'ten önce ad yazılır),
`AcilisBakiyeTestleri.Muhurden_sonra_acilis_KAPANIR` (servis çağrısına `IsletmeAdiOnayi = "T2-muhur"`).

Filtreli koşumlar:
- P-49 listesi + ilgili 4 mevcut test: **9/9 Başarılı** (`-v n`, adlarıyla).
- `Kurulum|AcilisBakiye|SozlesmeSekil|Sozluk|Dil` kümesi: **462/462 Başarılı**.

### Tam süit (`--logger trx`, `trx-pP-49/p49-tam.trx`)
**Toplam 4196 · Başarılı 4194 · Başarısız 2 · Atlanan 0.** Paket testleri (Kurulum*/AcilisBakiye mühür): 31/31 Başarılı.

| Kırmızı | Sahibi | Kanıt |
|---|---|---|
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | P-30b (DALGA 7c) | Devir notundaki bilinen tek kırmızı |
| `AltinPozisyonuTestleri.B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi` | PARA şeridi (`Modules/Pozisyon/`, `AltinPozisyonuTestleri.cs` — ikisi de izlenmeyen yeni dosya, bu paketin değil) | Süit koşarken `PozisyonModulu.cs` 19:43'te değişti (paralel şeridin mutasyonu/düzenlemesi; 19:38-19:40 arası dosyada `)$1 {` kırığı gözlendi). Yalnız başına **iki kez** koşuldu → 2/2 **Başarılı**. Sıralama beklentisi (ULID) kararsız ya da o anki ara durum. |

**FARK (bu paketin ürettiği yeni kırmızı) = 0.**

## 5. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/P-49/` altına kopyalandı → `perl -0777 -pi -e` tek eşleşme → `dotnet build --no-incremental` → filtreli `dotnet test -v n` → yedek geri → `cmp` birebir.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M49-1 | `MuhurAcik` içinde `!MuhurGerekceKisa` → eski `!GerekceKisa` | **Acilis_gerekcesi_muhur_dugmesini_etkinlestirmez**, Ayri_muhur_gerekcesi_dolunca_dugme_etkinlesir, Onay_kipinde_isletme_adi_yazilmadan_evet_disabled, Onay_kipi_metni_…, Muhurleme_ONAY_MODALI_… (5/15) | cmp birebir |
| M49-2 | Ekrandaki type-to-confirm karşılaştırması → `return true;` | **Onay_kipinde_isletme_adi_yazilmadan_evet_disabled** (1/15) | cmp birebir |
| M49-3 | `KurulumServisi.Muhurle`'deki `IsletmeAdiEslesir` kapısı silindi | **Muhur_isletme_adi_onayi_yanlissa_400** (1/16) | cmp birebir |
| M49-4 | `if (!dto.Onay) throw …` satırı silindi | **Muhur_ONAYSIZ_400** (1/16) | cmp birebir |
| M49-5 (ek) | Mühür gövdesi `gerekce = muhurGerekcesi` → açılış `gerekce`si | **Onay_kipinde_isletme_adi_yazilmadan_evet_disabled** (1/15) | cmp birebir |
| M49-6 (ek) | Sunucu karşılaştırması `Ordinal` → `OrdinalIgnoreCase` | **Muhur_isletme_adi_onayi_yanlissa_400** (1/15) | cmp birebir |

Test açığı çıkmadı. Son mutasyondan sonra `dotnet build Kuyumcu.sln --no-incremental`: **0 Hata**; 3 uyarının hiçbiri bu paketin değil
(2 × CS8604 `OlcumKapisiTestleri.cs:525` — DALGA 7c; 1 × MSB3026 paralel derlemenin kilitlediği `.gz` kopyası).
Paketin dokunduğu dosyalarda uyarı yok.

## 6. Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı 0 Hata** (ilk derleme); temiz derleme yukarıda.
- `bash dotnet/denetim.sh` (son koşum) → **DENETİM TEMİZ** (273 ✓). Paketin sayaçları: `Pages: ModalPencere kullanımı 55 (tavan 55)` — yeni
  düzenleme modalı açılmadı, mevcut ortak modal kullanıldı; `P-13: GEREKCESIZ_KILITLI_ALAN 0`; `P-13: IC_KOD_SIZINTISI 0`.
- Ara koşumda P-13 sayacı yeni `#kur-onay-evet` kilidini (sebep satırı ±14 penceresinin dışında kaldığı için) 1 saydı → `aria-describedby="kur-onay-kilit"` bağlandı → 0.
  Aynı ara koşumdaki `app.css ham hex 29/25` ve `Sınırsız ToListAsync 146/145` artışları başka şeritlerin o anki değişikliğiydi
  (app.css 19:38'de, PozisyonModulu.cs 19:38'de değişti; bu paket ikisine de dokunmadı); son koşumda ikisi de temiz.
- ⚠ Gözlem: paralel şeritler aynı `bin/obj`'yi paylaştığı için bir artımlı derleme yeni test yöntemlerini **içermeyen** bir dll bıraktı
  (`--list-tests` 4 yeni testi göstermedi). `--no-incremental` ile giderildi; tüm test/mutasyon ölçümleri artımsız derlemeden sonra alındı.

## 7. Canlı doğrulama (5198, `ASPNETCORE_ENVIRONMENT=Development`, `--no-build`)
Test kiracısı: **`PAKETP49-TEST Işık Kuyumcu`** (davet `/sistem/davetler` → `/auth/kayit` → `/auth/giris`; sistem anahtarı oturum başına
üretilip ortam değişkeniyle verildi, değeri hiçbir çıktıya yazılmadı, iş sonunda dosyası boşaltıldı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /kurulum/durum` | 200 + `isletmeAdi` = kiracı adı | 200 · `isletmeAdi = "PAKETP49-TEST Işık Kuyumcu"` · eksik `[KUR, KASA_BANKA]` |
| `POST /kurulum/muhurle` ad YOK | 400 | 400 "…onaylamak için işletme adını birebir yazın" |
| ad `""` | 400 | 400 (aynı mesaj) |
| ad küçük harf | 400 | 400 (aynı mesaj) |
| ad "Baska Kuyumcu" | 400 | 400 (aynı mesaj) |
| doğru ad + `onay=false` | 400 | 400 "…onay kutusu işaretlenmelidir" |
| red sonrası durum | mühürsüz | `muhurlendi = false` |
| `PATCH /kurulum/durum` eksikleri atla | 200 | 200 · `muhurlenebilir = true` |
| doğru ad (baş/son boşluklu) | 200 | 200 · `muhurlendi = true` |
| ikinci mühür denemesi | 409 `Acilis Muhurlu` | 409 `Acilis Muhurlu` |

SQL kanıtı (`kuyumcu_net`):
```
SELECT t.ad, k.muhurlendi, k.muhur_gerekcesi, k.atlanan_adimlar FROM kurulum_durumu k JOIN tenants t ON t.id=k.tenant_id WHERE k.tenant_id='01M37JB63JWK8BG7VX710AYH17';
→ PAKETP49-TEST Işık Kuyumcu | 1 | P49 canli: dogru ad ile muhur | KUR,KASA_BANKA
SELECT eylem, varlik_turu, detay FROM denetim_kayitlari WHERE tenant_id='…' AND varlik_turu='ACILIS';
→ EKLEME | ACILIS | kurulum MÜHÜRLENDİ · P49 canli: dogru ad ile muhur      (tek satır — reddedilen 5 deneme iz/kayıt bırakmadı)
```
Ekran (UI) kabul koşulları bUnit ile kanıtlandı (§ 4); tarayıcıyla ayrıca gezilmedi.

Kapanış: API `kill` (pid 17747 + sarmalayıcı 17669) → `lsof -iTCP:5198 -sTCP:LISTEN` **boş**.
Kiracı silme: `zsh betikler/kiraci-temizle.sh 01M37JB63JWK8BG7VX710AYH17 --uygula` → 27 satır + tenants 1 satır silindi;
**tenants sayısı 10 → 10** (başlangıçla aynı; QA-TEST-* ve DEMO kiracılarına dokunulmadı).

## 8. Değişen dosyalar
(`git status --porcelain | grep -v -E "/(bin|obj)/"` — Kurulum dosyaları dizin olarak izlenmediğinden `??` görünür; bu paketin dokunduğu dosyalar:)
- `dotnet/Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs` (ayak izi)
- `dotnet/Kuyumcu.Web/Pages/Kurulum.razor` (ayak izi)
- `dotnet/Kuyumcu.Tests/KurulumEkranTestleri.cs` (ayak izi)
- `dotnet/Kuyumcu.Tests/KurulumHttpTestleri.cs` (ayak izi)
- `dotnet/Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs` (ayak izi dışı — `KurulumDurumYaniti.IsletmeAdi`; ekranın adı sunucudan okuması için zorunlu)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (ayak izi dışı — TR+EN zorunluluğu; yalnız Kurulum bloğuna 3 anahtar + `MuhurOnay` metni)
- `dotnet/Kuyumcu.Tests/AcilisBakiyeTestleri.cs` (ayak izi dışı — `Muhurden_sonra_acilis_KAPANIR` servis çağrısına tek alan; yoksa yeni kapı yüzünden 400 verirdi)

Yasaklı çakışma dosyalarına (denetim.sh, OlcumKapisiTestleri.cs, KasaBakiyeKapisi.cs, Kasa/Vezne/Maliyet/Rapor modülleri, Raporlar.razor, Gider.razor, 5199 yayını) **dokunulmadı**.

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş):** `Pages/Kurulum.razor`. (Çakışma haritası sırası P-49 → P-68 → P-71 → P-62.)

## 9. DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Gerekçe |
|---|---|---|
| Yeni varlık | **YOK** | Paket yeni tablo/kayıt türü doğurmadı. |
| `KurulumDurumu.Muhurlendi` (+ tarih/gerekçe/kullanıcı) | **Salt okunur, bilinçli tek yönlü** | Mühür geri dönülemez bir kapıdır; mühürlü ekranda `DegismezIpucu Varlik="AcilisKalemi"` (`#kur-muhur-rozeti`) değişmezliği ve düzeltme yolunu (cari düzeltme fişi / storno) yazar. Onay modalı artık "geri alınamaz" ve "tek düzeltme yolu storno" der. |
| `IsletmeAdiOnayi` | Saklanmaz | Yalnız istek anında karşılaştırılan onay girdisi. |
| Adım atlama tercihi | `PATCH /kurulum/durum` (mevcut, değişmedi) | Gönderilmeyen alan korunur / null siler — mevcut test yeşil. |
- Her düğme: `#kur-muhurle` gerekçe < 20 iken disabled + `#kur-muhur-kilit`; `#kur-onay-evet` mühür kipinde ad birebir değilken disabled + `#kur-onay-kilit`; doğruysa gerçek `POST` atar (bUnit kanıtı).
- Tek düzenleme kalıbı: yeni modal açılmadı, mevcut ortak `ModalPencere` yalnız yıkıcı onay için kullanılıyor (sayaç 55/55).
- Ücretsiz pilot kuralı: fiyat/teklif/tahsilat metni üretilmedi.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Açılış gerekçesine 29 karakter yazmak `#kur-muhurle`'yi etkinleştirmez | KARŞILANDI | `Acilis_gerekcesi_muhur_dugmesini_etkinlestirmez` (29 uzunluğu testte assert); M49-1 kırmızı |
| Ayrı mühür gerekçesi 20 karakter olunca düğme etkinleşir | KARŞILANDI | `Ayri_muhur_gerekcesi_dolunca_dugme_etkinlesir` (19 → kilitli, 20 → açık) |
| Ad birebir yazılmadan `#kur-onay-evet` disabled | KARŞILANDI | `Onay_kipinde_isletme_adi_yazilmadan_evet_disabled` (küçük harf, eksik, aksansız, başka ad); M49-2 kırmızı |
| `POST /kurulum/muhurle` ad yanlış/boş → 400 (ekran atlansa bile) | KARŞILANDI | `Muhur_isletme_adi_onayi_yanlissa_400` + canlı 4 × 400; M49-3, M49-6 kırmızı |
| Geri al akışı davranışça değişmez | KARŞILANDI | Geri al gövdesi/koşulu dokunulmadı; `Kurulum*` 29 + ilgili küme 462/462 yeşil |
| Sabit `onay = true` kaldırıldı | KARŞILANDI | Gövde `onay = IsletmeAdiDogru`; bUnit gövdeyi JSON ayrıştırarak sınar |
| Modal metni geri dönülemezliği ve storno yolunu yazar | KARŞILANDI | `Onay_kipi_metni_geri_donulemezligi_ve_storno_yolunu_yazar` |
| `Muhur_ONAYSIZ_400`, `Muhurlu_kiracida_acilis_uygula_409` yeşil | KARŞILANDI | Tam süitte Başarılı; M49-4 kırmızı |
| Paket kapısı (7) yeni kırmızı yok | KARŞILANDI | Fark = 0 (§ 4) |

## 11. Açık kalanlar
1. **`OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`** kırmızı — sahibi **P-30b (DALGA 7c)**; devir notunda bilinen.
2. **`AltinPozisyonuTestleri.B2_08_…`** tam süitte bir kez kırmızı, yalnız başına 2/2 yeşil — sahibi **PARA şeridi** (`Modules/Pozisyon/`); orkestratör kapısında yeniden ölçülmeli (kararsız sıralama olabilir).
3. Gözlem (kapsam dışı, P-68 adayı): mühürden sonra `POST /kurulum/acilis/prova` **200** döndü (kuru prova, yazma yok). `uygula`/`muhurle` 409 veriyor; prova'nın da 409 vermesi istenirse ayrı karar.
4. Canlı doğrulamanın davet satırı `kiraci_davetleri` tablosunda kaldı (id `01M37JB5ZRE8KCA60750PDCBHW`, durum kullanıldı). Tabloda `tenant_id` yok; kiracı temizlik betiği kapsamı dışında — silme kararı kurucuda/orkestratörde.
5. 5199 yayını bu paketten **etkilenmedi**; değişikliğin incelemeye girmesi sürümlü yayın betiğiyle olur.

## 12. Kurucuya / mali müşavire notlar
- Mevzuat hükmü **yazılmadı**. Modal metnindeki "storno" düzeltme yolunun ürün içi adıdır (48-A motoru); mevcut `AcilisDegismez` metnindeki VUK md.219 atfı bu pakette değiştirilmedi.
- İşletme adı eşleşmesi **büyük/küçük harf duyarlıdır** (Türkçe İ/ı dahil). Kurucu daha gevşek bir kural isterse (ör. harf duyarsız) tek noktada değişir:
  `KurulumServisi.IsletmeAdiEslesir` + ekrandaki ikizi; testler bunu kilitler.
