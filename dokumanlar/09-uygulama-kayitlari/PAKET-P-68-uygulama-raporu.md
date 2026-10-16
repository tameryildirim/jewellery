# PAKET P-68 — /kurulum kilit gerekçesi doğruluğu + eksik adım bağlantıları + kabul edilmeyen döviz kasası

- Şerit: SERİT C (gider pusulası + vade + kurulum + rapor grafikleri). İzole test veritabanı `kuyumcu_test_k`, canlı port 5186.
- Rol: Taşkın (backend) + Ertuğ (frontend).
- Kaynak: `TRIYAJ-TUR8-2026-09-23.md`, P-68, ciddiyet ORTA.
- Tarih: 2026-09-24.
- Durum: **BİTTİ.** Paket kapısının yedi maddesi aşağıda kanıtıyla sağlandı.

## 1. Spesifikasyonun koda karşı doğrulanması

Dosya ayak izi `test -e` ile doğrulandı. Üç dosya da var:

| Dosya | Satır (önce) |
|---|---|
| `Kuyumcu.Web/Pages/Kurulum.razor` | 789 |
| `Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs` | 394 |
| `Kuyumcu.Tests/KurulumEkranTestleri.cs` | 449 |

Spesifikasyonun "mevcut durum" iddialarını koda karşı tek tek kontrol ettim:

| İddia | Kodda gördüğüm | Sonuç |
|---|---|---|
| Kilit gerekçesi eksik adımlarla birebir değil; kaydetme durumu eksik adım gibi görünüyor | `MuhurEksikleri` yalnız `EksikListe` ve gerekçe maddesinden oluşuyordu. Kaydetme sürerken `MuhurAcik=false` oluyordu, bu yüzden kutuda madde olmadan yalnız "Kilit sebebi:" yazıyordu (açıklamasız kilit). Maddeler `" · "` ile birleştirilmiş düz metindi, sayılabilir değildi. | Kısmen doğru. Hedefe sadık kaldım: maddeler `<li>` oldu, kaydetme durumu ayrı satıra taşındı. |
| Eksik adımlar tıklanamıyor | Doğru. Kutu düz metindi. | Düzeltildi. |
| Döviz kasası hedef seçicisine giriyor | Doğru. `HedefleriYukle` aktif kasaların hepsini ekliyordu, sunucu da provada `DOVIZ_HESAP_FAZ_B` ile reddediyordu. Kabul edilen para birimi sunucuda iki yerde sabit `"TRY"` olarak yazılıydı. | Düzeltildi. Tek kaynak `AcilisServisi.KabulEdilenParaBirimleri`. |
| `#kur-muhur-kilit` `role=status` taşımıyor | Doğru. `<p>` idi ve `role` özniteliği yoktu. | Düzeltildi. |

Mevzuat: bu pakette mevzuat hükmü yok. Döviz kasası açılışının olmaması bir ürün fazı sınırıdır (Faz B), vergi kuralı değildir.

## 2. Yapılanlar

### Backend (Taşkın)
- `AcilisServisi.KabulEdilenParaBirimleri` (`IReadOnlyList<string> = ["TRY"]`) eklendi. Prova doğrulamasındaki iki sabit `!= "TRY"` karşılaştırması bu listeyi kullanır oldu (kasa ve banka).
- `GET /kurulum/durum` yanıtına `acilisParaBirimleri` alanı eklendi, kaynağı aynı liste. Uç, yetki ve denetim akışı değişmedi. Bu salt okunur bir alan olduğu için yeni varlık yok ve PATCH/DELETE gerekmiyor.
- Migration: **gerekmedi.** Şema değişmedi.

### Sözleşme (Core)
- `KurulumDurumYaniti` kaydına sona `List<string>? AcilisParaBirimleri = null` eklendi, geriye uyumlu. `ParaBirimiKabul(pb)` yardımcısı da eklendi. Alan `null` ise ekran süzmez ve sabit liste uydurmaz; bu durumda sunucu provada reddeder.

### Ekran (Ertuğ) — `Pages/Kurulum.razor`
- `#kur-muhur-kilit` artık bir `<div role="status">`.
  - `MuhurEksikleri` `(Adim?, Metin)` çiftleri üretir: sunucunun `eksikAdimlar` dizisinden birebir ve aynı sırada, mühür gerekçesi kısaysa +1 anahtarsız madde.
  - Her eksik adım `<li id="kur-muhur-eksik-{ANAHTAR}">` içinde `<a href=…>` bağlantısıdır.
  - Gerekçe maddesi `#kur-muhur-eksik-gerekce` kimliğini taşır ve bağlantı değildir, çünkü bir adım değildir.
- Kaydetme sürerken ayrı bir `<p id="kur-muhur-kaydediliyor">` ("Kaydediliyor…") çıkar. Bu satır listede değildir, `li` içinde değildir ve madde sayısını değiştirmez.
- `EksikAdimYolu(anahtar)` hiçbir adım için boş dönmez:
  - `ISLETME` → `/isletme`, `KULLANICILAR` → `/kullanicilar`, `KUR` → `/kur`, `KASA_BANKA` → `/kasalar`. Bunlar mevcut `YonlendirmeYolu` üzerinden gelir; ikinci bir eşleme yazılmadı.
  - `CARI_ACILIS` ve `STOK_ACILIS` → `/kurulum#kur-acilis`.
  - Bilinmeyen anahtar → `/kurulum#kur-adim-{anahtar}`.
- Açılış hedef seçicisi: kabul edilmeyen para birimindeki kasa ve banka hesapları `kasalar`/`hesaplar` listelerine girmez; `disaridaKasalar`/`disaridaHesaplar` listelerine ayrılır. Sessiz kırpma yapılmaz (UX-S29): `#kur-acilis-doviz-disarida` notu, `#kur-acilis-doviz-{id}` satırlarıyla her birini "ad (birim) — kabul edilenler: …" biçiminde yazar. Kabul edilenler sunucudan okunur.
- P-49 davranışına dokunulmadı: mühür gerekçesi, işletme adını yazarak onay ve adım listesindeki "Adıma git" bağlantıları yerinde. Mevcut element id'leri değişmedi, satır içi style ve sabit renk yok.

### Metinler
- Hub dosyası `Sozlukler.cs`'e **dokunulmadı**. Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.KurulumKilit.cs` açıldı: konumsal record, TR + EN; bir anahtar eksikse derleme hatası verir.

### DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| (yeni varlık yok) | — | P-68 yalnız görünüm ve durum yanıtına salt okunur bir alan ekler. |
| `acilisParaBirimleri` (durum yanıtı) | Salt okunur | Ürün sürümü sabitidir (Faz A = TRY). Faz B'de yalnız `AcilisServisi.KabulEdilenParaBirimleri` genişler. |
| Dışarıda kalan döviz kasası | Kasa ekranında (`/kasalar`) düzenlenir | Açılış ekranı bu kasayı neden listelemediğini yazar. |

DoD EK kontrolleri:
- Yeni düğme yok. Yeni etkileşimli öğeler bağlantıdır (`href` dolu), boş düğme yok.
- `ModalPencere` kullanım sayısı artmadı.

## 3. Derleme
`dotnet build Kuyumcu.sln --nologo`: **0 Uyarı, 0 Hata**. Bu, mutasyonlardan sonraki temiz derlemedir (`p68-build2.log`).

## 4. Testler

| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `KurulumEkranTestleri` (önce) | 15 | 15 | 0 |
| `KurulumEkranTestleri` (sonra) | 20 | 20 | 0 |
| `KurulumHttpTestleri` (sonra) | 16 | 16 | 0 |
| `Kurulum` ve `Acilis` içeren tüm testler | 124 | 124 | 0 |
| **Tam süit** (trx `trx-pP-68/_Mac_2026-09-24_03_15_43.trx`) | **4813** | **4813** | **0** |

Yeni testler (6):
1. `KurulumEkranTestleri.Kilit_gerekcesi_sunucunun_eksik_adimlariyla_birebir_esler`: beklenen sayı sahte yanıttaki `eksikAdimlar` JSON'ından okunur. Maddeler eksik adım sayısı + 1 (gerekçe kısa) çıkar; gerekçe dolunca eksik adım sayısına iner. Kimlikler sunucu sırasıyla aynıdır ve ham anahtar metinde görünmez.
2. `KurulumEkranTestleri.Eksik_adim_maddeleri_ilgili_rotaya_baglanir`: dört adım için `href` dolu olmalı ve beklenen rotaya eşit olmalı.
3. `KurulumEkranTestleri.Kaydediliyor_durumu_eksik_adim_gibi_listelenmez`: PATCH yanıtı `TaskCompletionSource` ile bekletilir. Bu sırada `#kur-muhur-kaydediliyor` görünür, madde sayısı değişmez, hiçbir `li` bu metni taşımaz ve satır `li` içinde değildir. Yanıt gelince satır kaybolur.
4. `KurulumEkranTestleri.Kabul_edilmeyen_para_birimli_kasa_hedef_seciciye_girmez`: TRY ve USD kasa verilir. Tek hedef kaldığı için seçici gizlenir ve "Merkez Kasa" yazar; USD kasası tabloda yoktur; `#kur-acilis-doviz-k2` USD ve TRY'yi yazar.
5. `KurulumEkranTestleri.Kilit_kutusu_role_status_tasir`
6. `KurulumHttpTestleri.Durum_acilis_para_birimlerini_AcilisServisi_ile_ayni_kaynaktan_doner`: bu testi ek olarak yazdım; sunucu tarafındaki tek kaynağı kilitler.

Sahte API güncellemesi: `/kasalar` yanıtı alana alındı (`kasalarJson`), PATCH `/kurulum/durum` için bekletici eklendi. Mevcut varsayılan yanıtlar değişmedi.

## 5. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/P-68/` altına kopyalandı. Her mutasyon `perl -0777 -pi -e` ile tek eşleşmeli değişiklikti. Ardından `dotnet test --filter … -v n` koşuldu, yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M68-1 | `MuhurEksikleri`'ne sabit fazladan bir madde | `Kilit_gerekcesi_sunucunun_eksik_adimlariyla_birebir_esler`, `Kaydediliyor_durumu_eksik_adim_gibi_listelenmez` | birebir |
| M68-2 | `EksikAdimYolu` → `""` | `Eksik_adim_maddeleri_ilgili_rotaya_baglanir` | birebir |
| M68-3 | `if (kaydediliyor) yield return (null, Kaydediliyor)` eksik listesine | `Kaydediliyor_durumu_eksik_adim_gibi_listelenmez` | birebir |
| M68-4 | Kasa para birimi süzgeci kaldırıldı (`kasalar.Add(h)`) | `Kabul_edilmeyen_para_birimli_kasa_hedef_seciciye_girmez` | birebir |
| M68-5 (ek) | `role="status"` kaldırıldı | `Kilit_kutusu_role_status_tasir` | birebir |
| M68-6 (ek, sunucu) | Durum yanıtından `acilisParaBirimleri` kaldırıldı (`KurulumModulu.cs`) | `KurulumHttpTestleri.Durum_acilis_para_birimlerini_AcilisServisi_ile_ayni_kaynaktan_doner` | birebir |

Not: M68-2'nin ilk denemesinde derleme, kardeş şeridin eş zamanlı derlemesi yüzünden kilitlenen `Kuyumcu.Web.pdb` hatası verdi ("being used by another process"). Mutasyon tekrarlandı ve sonuç yukarıdaki satırdır. Her mutasyondan sonra geri alma `cmp` ile kanıtlandı; son olarak temiz derleme alındı (0 uyarı, 0 hata).

## 6. denetim.sh
`dotnet/denetim.sh`: çıkış kodu 0, **"DENETİM TEMİZ."**

## 7. Canlı doğrulama (port 5186, `kuyumcu_net`)
Test kiracısı `PAKETP-68-TEST Kuyumcu` (tenantId `01M38CAXD2CCJ7GRGG6435BFFD`) davetle açıldı. Davet jetonu yerelde üretildi ve veritabanına yalnız SHA-256 özeti yazıldı. Jeton, parola ve erişim jetonu ekrana basılmadı; bunları tutan dosyalar `printf ''` ile boşaltıldı. Yanıt gövdeleri okundu.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201**; tenantId, accessToken ve refreshToken döndü |
| 2 | `GET /kurulum/durum` (kasa yok) | 200, `eksikAdimlar` gerçek, `acilisParaBirimleri` var | **200**; `muhurlenebilir=False`, `eksikAdimlar=['KASA_BANKA']`, `acilisParaBirimleri=['TRY']` |
| 3 | `POST /kasalar` TRY ve USD | kayıtlar oluşur | İki kasa oluştu (`paraBirimi` TRY ve USD döndü) |
| 4 | `GET /kurulum/durum` | eksik boşalır | **200**; `muhurlenebilir=True`, `eksikAdimlar=[]`, `acilisParaBirimleri=['TRY']` |
| 5 | `GET /kasalar` | `paraBirimi` alanı taşır (ekran süzgecinin girdisi) | **200**; `PAKETP68 TL Kasa TRY True`, `PAKETP68 Dolar Kasa USD True` |
| 6 | `POST /kurulum/acilis/prova` USD kasa | 200, `temiz=false`, `DOVIZ_HESAP_FAZ_B` | **200**, `temiz=False`, `[(1,'DOVIZ_HESAP_FAZ_B','Döviz kasası/hesabı açılışı bu sürümde yoktur; TL hesapla devam edin')]` |
| 7 | `POST /kurulum/acilis/prova` TL kasa | 200, `temiz=true` | **200**, `temiz=True`, `hatalar=[]` |

SQL kanıtı:
```
SELECT ad,para_birimi,aktif FROM kasalar WHERE tenant_id='01M38CAXD2CCJ7GRGG6435BFFD';
PAKETP68 TL Kasa     TRY  1
PAKETP68 Dolar Kasa  USD  1
SELECT COUNT(*) FROM acilis_kalemleri WHERE tenant_id=…;   -> 0   (prova hiçbir şey yazmadı)
SELECT COUNT(*) FROM kasa_hareketleri WHERE tenant_id=…;   -> 0
```

Canlı doğrulamanın sınırı: ekran davranışını (madde sayısı, bağlantılar, süzgeç, `role=status`) tarayıcıda gösteremedim, çünkü parola girerek oturum açmak benim yapamayacağım bir işlem. Ekran kabulü bUnit testlerine ve mutasyon kanıtlarına dayanır; canlı ortamda sunucu sözleşmesi, yani ekran süzgecinin girdisi doğrulandı.

Kapanış:
- API durduruldu (PID 87147 ve çocuğu 87170). `lsof -iTCP:5186 -sTCP:LISTEN` boş.
- Test kiracısı `information_schema`'dan `tenant_id` kolonu bulunan 70 tablonun hepsinden, `tenants`'tan ve tüketilen `kiraci_davetleri` satırından silindi.
- İlk silmeden sonra `kur_snapshots` ve `denetim_kayitlari` tablolarında birer satır geri geldi. Sebebi büyük olasılıkla başka bir sunucudaki arka plan kur işinin kiracıyı silinmeden önce okumuş olması (yarış). Bu satırlar da silindi; 20 saniye sonraki yeniden sayımda **kalan satır 0.**
- `tenants`: başlangıçta **11**, sonda **10**. Sondaki 10 kiracının hiçbiri benim değil; silmelerim yalnız kendi tenantId'imle yapıldı. Başlangıçtaki 11. kiracı büyük olasılıkla kardeş bir şeridin o anda açık olan geçici test kiracısıydı ve o şerit tarafından silinmiş olmalı. Bunu doğrulayamadım; kanıt olarak sondaki 10 kiracının listesi alındı: Demo ×2, ByGatech, Düzeltme Sonrası, Ramazan Deneme, QA-TEST-1/2/3/FRONTEND, DEMO-Vitrin. `QA-TEST-*` kiracılarına (4) dokunulmadı.
- API başlarken `is_calismalari` benzersiz anahtar çakışması (`MASAK_SIB_SURESI-2026-09-24`) günlüğe düştü. Bu, aynı veritabanında birden çok sunucu koşarken iş idempotensi kapısının beklenen davranışıdır; P-68 ile ilgisi yok.

## 8. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` bu dosyaları ve dizinlerini izlenmeyen (`??`) olarak gösterir.
- `dotnet/Kuyumcu.Web/Pages/Kurulum.razor` (789 → 885 satır)
- `dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs`
- `dotnet/Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KurulumKilit.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/KurulumEkranTestleri.cs` (449 → 597 satır)
- `dotnet/Kuyumcu.Tests/KurulumHttpTestleri.cs`

Dosya sınırı sapması: `KurulumSozlesmeleri.cs`, Kurulum'un tek istemci sözleşmesi olduğu için değiştirildi. Pages altına yanıt modeli yazmak denetim sayacıyla yasak, bu yüzden başka yol yoktu. Kardeş şeritlerin listesinde bu dosya yok. Ortak bileşenlere ve `Sozlukler.cs` hub'ına dokunulmadı.

Dokunulan sayfalar: `Pages/Kurulum.razor` (P-49'dan sonra planlı ikinci dokunuş; triyajın sıralamasında P-49 → P-68 gerekçeli).

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| `#kur-muhur-kilit` madde sayısı = `eksikAdimlar` uzunluğu + (gerekçe kısa ise 1) | KARŞILANDI | Test 1 ve M68-1 |
| Her eksik adım bir bağlantıdır ve `href` boş değildir | KARŞILANDI | Test 2 ve M68-2 |
| Kabul edilmeyen para birimindeki kasa seçiciye girmez, gerekçesi yazılı | KARŞILANDI | Test 4, M68-4, canlı 5–6 |
| `#kur-muhur-kilit` `role=status` taşır | KARŞILANDI | Test 5 ve M68-5 |
| Kaydetme sırasında yanlış eksik adım maddesi üretilmez | KARŞILANDI | Test 3 ve M68-3 |
| Kabul listesi AcilisServisi'nden okunur, ekranda sabit liste yok | KARŞILANDI | Test 6 ve M68-6 |
| P-49 davranışı bozulmadı | KARŞILANDI | Eski 15 ekran testi ve Kurulum HTTP testleri yeşil |
| Paralel şerit kapısı (7) — yeni kırmızı yok | KARŞILANDI | Tam süit 4813/4813, kırmızı listesi boş |

## 10. Açık kalanlar
- Ekranın canlı tarayıcı doğrulaması yapılamadı (oturum açma parola gerektiriyor). Orkestratörün ya da kurucunun QA sunucusunda `/kurulum` sayfasına bir kez bakması önerilir.
- Banka tarafındaki döviz süzgecinin kodu kasa ile aynı; ayrı bir bUnit testi yok, kapsamı kasa testiyle ortak.
- Başka şeride ait kırmızı test yok (tam süit 0 başarısız).

## 11. Kurucuya / mali müşavire notlar
- Mevzuat hükmü yazılmadı. Döviz kasası açılışının olmaması bir ürün fazı sınırıdır (Faz B) ve tek yerden (`AcilisServisi.KabulEdilenParaBirimleri`) genişletilir.
