# PAKET UX-4a — Mantık ve tutarlılık düzeltmeleri: Fiyatlandırma (madde 1-5) + fiyat/sayım yetki kapısı (madde 14)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 2 · 2026-09-16
> Kapsam: `PAKET-UX-4-mantik-ve-tutarlilik-duzeltmeleri.md` "Hemen düzeltilecekler" tablosunun **1, 2, 3, 4, 5 ve 14** numaralı maddeleri.
> Madde 6-13 BU pakette YAPILMADI (UX-4b/UX-4c).
> Uygulama sırası bağlayıcıydı ve aynen izlendi: **1 → 14 → 2 → 3 → 4 → 5**.
> **COMMIT YAPILMADI.** Çalışma ağacında kalındı.

---

## 1. Durum özeti

| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 hata · 0 uyarı** |
| Tam süit (`--logger trx`) | **1216 test · Geçti 1216 · Başarısız 0 · Atlanan 0** |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |
| Migration | **gerekmedi** (şema değişikliği yok — gerekçe § 4) |
| Mutasyon kanıtı | **7/7 mutasyon kırmızı test üretti**, hepsi `cmp` ile birebir geri alındı |
| Canlı doğrulama (5198) | 12 adım, hepsi beklenen HTTP koduyla + SQL kanıtı |
| Test kiracısı | `PAKETUX-4a-TEST` **silindi**; `tenants` sayısı 6 → 6 |

---

## 2. Madde madde yapılanlar

### Madde 1 — API: fiyat listesi güncellemede varsayılan bayrağının sessizce düşmesi (KRİTİK)

**Bulunan hata (spesifikasyondaki iddia koda karşı DOĞRULANDI):**
`FiyatServisi.ListeGuncelle` içinde
```csharp
liste.Varsayilan = dto.Varsayilan || liste.Varsayilan && dto.Varsayilan;
if (dto.Varsayilan) liste.Varsayilan = true;
```
Gövdede `varsayilan:false` gelen her PATCH — ki madde 2 ile eklenen düzenleme formu tam gövde gönderir — kiracının varsayılan listesini düşürüyordu. Sonuç zinciri: `ListeCoz` (`Varsayilan && Aktif`) null → `Teklif` null → `GET /fiyat/teklif` `{teklifVar:false, sebep:"LISTE_YOK"}` → **POS'ta tüm otomatik fiyatlandırma durur.** Bu yüzden 1 → 2 sırası bağlayıcıydı.

**Yapılan:**
- `FiyatListesiDto.Varsayilan` `bool` → **`bool?`** (PATCH sözleşmesi UX-S26: gönderilmeyen alan = koru). XML yorumuna sözleşme yazıldı.
- `ListeGuncelle` yeniden kuruldu: `varsayilanIstendi = dto.Varsayilan == true`. **null ve false KORUR**, yalnız `true` atar (koruma satırı: `if (varsayilanIstendi) liste.Varsayilan = true;`).
- Varsayılanlık yalnız **başka bir listeye devredilerek** kalkar (o listeye `varsayilan:true` PATCH'i).
- **Fail-closed kapı eklendi:** güncelleme sonrası kiracıda *aktif bir varsayılan liste* kalmıyorsa `400` — "Bu değişiklik kiracıyı varsayılan fiyat listesiz bırakır…". Kontrol, kiracının tüm listeleri belleğe alınarak `SaveChangesAsync`'ten ÖNCE yapılır (varsayılanlık tek satıra bakarak doğrulanamayan bir kiracı değişmezidir).
- `ListeOlustur` uyarlandı: `Varsayilan = dto.Varsayilan == true` (davranış aynı; ilk liste yine zorla varsayılan).

Mevcut "Varsayılan fiyat listesi pasife alınamaz" kapısı **aynen korundu**.

### Madde 14 — yetki kapısı (Mehmet: açık delik ilk sevkte kapanır)

**Bulunan durum (doğrulandı):** `FiyatModulu` yalnız `[ApiController, Authorize]` taşıyordu; `SayimModulu` de öyle. Menüde ise ikisi de `Yetki.Erisebilir("rapor")` bloğundaydı. Yani rapor yetkisi verilen **tezgâhtar uçtan marjı değiştirebiliyordu**, rapor yetkisi olmayan müdür ekranı göremiyordu.

**Yapılan (Q2 mührüne birebir):**
- `FiyatModulu` sınıfına `[Yetki.ModulYetkisi("urun")]`.
- `SayimModulu` sınıfına `[Yetki.ModulYetkisi("urun")]`.
- Fiyat **yazma** uçlarına `[RolGerekli("SAHIP","MUDUR")]`: `POST/PATCH /fiyat/listeler`, `POST/PATCH /fiyat/kampanyalar`. (Okuma — `GET listeler`, `GET kampanyalar`, `GET teklif` — modül yetkisiyle açık kalır; POS'un tezgâhtarı fiyat teklifi alabilmelidir.)
- `KenarCubugu.razor`: `/fiyatlandirma` ve `/sayim` bağlantıları `Gorunur("rapor", …)` → `Gorunur("urun", …)`. Simge `🏷️` → **`📋`** (Etiketler'den ayrıldı).
- Menü metni: `Dil.T.Nav.Fiyatlandirma` zaten **"Fiyat Listeleri" / "Price Lists"** idi — sözlük değişmedi (spesifikasyondaki "Fiyatlandırma → Fiyat listeleri" talebi hâlihazırda karşılanmış; koda bakılarak doğrulandı).

**Göç/tohumlama gerekmedi:** spesifikasyon "yeni yetki anahtarı + göç gerektirir" diyordu; kodda `Moduller.Tumu` zaten `"urun"` içeriyor ve `Moduller.Varsayilan`'da MUDUR/MUHASEBE/TEZGAHTAR üçünde de var. Yeni anahtar AÇILMADI → mevcut kiracıların yetkileri değişmez, migration yok. **Sapma ve gerekçesi § 4'te.**

### Madde 2 — Fiyatlandırma ekranı: düzenleme yolu (UX-S25)

- Liste ve kampanya tablolarına **Eylemler** sütunu: `#fl-duzenle-{id}`, `#fl-durum-{id}` (Pasife al / Aktifleştir); `#km-duzenle-{id}`, `#km-durum-{id}`.
- Mevcut `yeniListe` / `yeniKampanya` formları **çift amaçlı**: form nesnesinde `Id` doluysa `PATCH /fiyat/listeler/{id}` (veya `/kampanyalar/{id}`), boşsa `POST`. İkinci bir form/mekanizma açılmadı.
- `Aktif` ve `Varsayilan` alanları forma taşındı (`#fl-aktif`, `#fl-varsayilan`, `#km-aktif`).
- Varsayılan listede **Pasife al kapalı** ve yanında sebebi yazılı (`#fl-kilit-{id}`: "Başka bir listeyi 'Varsayılan' işaretleyip kaydedin; varsayılanlık ona geçer") — UX-S30. `DegismezIpucu` KULLANILMADI: o bileşen *doğası gereği değişmez* kayıtlar içindir ve hiç düğme çizmez; fiyat listesi düzenlenebilir bir kayıttır, yalnız tek bir eylemi (pasife alma) koşullu kapalıdır. Gerekçe § 4'te.
- Durum değiştirme **tam gövdeyle** PATCH atar (ad/marj/çarpan satırdan taşınır) — kısmi gövde marjı sıfırlardı.

### Madde 3 — "Bu listeyle ürün fiyatları" paneli (UX-S23)

- Yeni bileşen: `dotnet/Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor` (paket 49'da genişletilecek şekilde **tek bileşende**).
- Liste satırındaki ada tıklanınca (`#fl-goster-{id}`) açılır; ilk 50 ürün için **mevcut** `GET /fiyat/teklif` çağrılır. Sütunlar: ürün · ayar · gram · has değeri · marj TL · işçilik TL · liste fiyatı · indirim · satış fiyatı.
- **Toplu marj ÖNİZLEMESİ salt hesap** (`#flp-marj`): sunucunun döndürdüğü kırılım üzerinden aritmetik; hiçbir istek atmaz, hiçbir şey kaydetmez. `ProAlan` içine alındı (UX-3: profesyonel alan; gövdeye hiç girmediği için UX-S5 ihlali doğurmaz).
- Gümüş ürün için **istek bile atılmaz**, `#flp-rozet-{id}` "Gümüş — otomatik fiyat yok" basar. Teklifsiz diğer durumlarda sunucunun `sebep` alanı rozete çevrilir.
- UX-S29: `#flp-sinir` "İlk 50 ürün gösteriliyor…" notunu basar.
- **YENİ TABLO YAZILMADI.** Tek sapma: mevcut `GET /fiyat/teklif` ucuna **isteğe bağlı `listeId` sorgu parametresi** eklendi (§ 4 — zorunluydu, aksi halde panel "bu listeyle" derken varsayılan listenin fiyatını basardı = Anayasa md.1 ihlali).

### Madde 4 — sessiz etkisizlik uyarıları

- `#km-iscilik-uyari`: indirim türü `ISCILIK_YUZDE` seçiliyken kalıcı bilgi kutusu — "Sabit satış fiyatı girilmiş ürünlerde işçilik indirimi uygulanmaz." (Kaynak: `FiyatMotoru.Indirim` kural 1.)
- `#km-gumus-uyari` + **`#km-kaydet` kilitli**: hedef ÜRÜN ve seçilen ürün gümüş (ayar `G*`) ise — "Gümüş ürünler altın has kuruyla otomatik fiyatlanmaz". (Q3 mührü: has kuruyla gümüş fiyatlama yolu **AÇILMADI**; gümüşte yalnız sabit fiyatlı geçersiz kılma vardır ve o paket 49'undur.)

### Madde 5 — geri bildirim, ilk liste kuralı, hata yutmama

- `<Yukleniyor />` açılışta; `<BasariBandi>` kayıt/durum değişimi sonrası (`#fl-basari`, `#km-basari`); `<BosDurum>` listeler boşken — metin: **"Henüz fiyat listesi yok — Fiyat listesi tanımlanmadan POS otomatik fiyat veremez."**
- Hiç liste yokken `#fl-varsayilan` **işaretli + `disabled`**, altında `#fl-varsayilan-not`: "İlk liste zorunlu olarak varsayılandır." (Razor tuzağı: boole `@( )` içinde verildi; `disabled="false"` dizgesi kullanılmadı.)
- Liste varken kutucuk işaretlenirse "Mevcut varsayılan listenin varsayılanlığı kalkar" uyarısı basar.
- **Hata yutma kalıbı kaldırıldı** (UX-S28 / Anayasa md.1): eski `catch (ApiHata) { … }` yolunda boş durum "kayıt yok" gibi görünüyordu. Artık `yuklendi && hata is null && liste boş` üçlü koşulu; hata bandı ayrı durur ve boş durum ÇIKMAZ.

---

## 3. DoD sözleşmesi (Bertan koşulu)

Bu pakette **yeni varlık / kayıt türü DOĞMADI**; var olan iki kayıt türünün düzenleme yolu açıldı.

| Varlık | Düzenleme ucu | Silme / pasife alma | Yetki kapısı | Denetim | Ekran eylemi |
|---|---|---|---|---|---|
| `fiyat_listeleri` | `PATCH /fiyat/listeler/{id}` (tam gövde; `varsayilan` gönderilmezse/false korunur) | Hard DELETE **yok** → `aktif:false` ile pasifleştirme. Varsayılan liste pasife alınamaz (400). | `[ModulYetkisi("urun")]` + `[RolGerekli("SAHIP","MUDUR")]` | `DenetimYazmaDinleyicisi` (DbContext boru hattı) — yeniden yazılmadı | `#fl-duzenle-{id}` · `#fl-durum-{id}` (+ kapalıysa `#fl-kilit-{id}` gerekçesi) |
| `kampanyalar` | `PATCH /fiyat/kampanyalar/{id}` | Hard DELETE **yok** → `aktif:false` | aynı | aynı | `#km-duzenle-{id}` · `#km-durum-{id}` |
| `GET /fiyat/teklif` | — (salt okunur hesap ucu) | — | `[ModulYetkisi("urun")]` | — | Panel `#flp-panel` |

**Salt okunur kalanlar ve gerekçeleri:** `hareketler`, `kasa_hareketleri`, `banka_hareketleri`, `faturalar` bu pakette HİÇ dokunulmadı; finansal varlıklarda hard DELETE yasağı ve `HareketFabrikasi` tek yazma yolu değişmedi (`denetim.sh`: `Hareketler.Add` sayısı **1**).

---

## 4. Sapmalar ve gerekçeleri

1. **`GET /fiyat/teklif` ucuna isteğe bağlı `listeId` parametresi eklendi.**
   Koşul "YENİ uç veya tablo YAZILMAZ" idi; yeni uç da tablo da yazılmadı, mevcut uca opsiyonel bir sorgu parametresi eklendi. Zorunluydu: teklif ucu listeyi **cariye göre** çözer (`ListeCoz`), yani parametresiz çağrıldığında *varsayılan* listenin fiyatını döner. Panel "Bu listeyle ürün fiyatları" başlığı altında varsayılan listenin fiyatını basarsa kullanıcıyı yanıltırdı (Anayasa md.1). `listeId` verilmezse POS davranışı **birebir aynıdır**; verilirse liste kiracı içinde aranır, bulunamazsa 404 (çapraz kiracı denemesi canlıda 404 ile doğrulandı).

2. **Yeni yetki anahtarı ("fiyat") açılmadı; mevcut `"urun"` kullanıldı.**
   Spesifikasyon "yeni yetki anahtarı varsayılan tohumlama + göç gerektirir" diyordu. Q2 mührü `"urun"` dedi ve kod da bunu destekliyor (`Moduller.Tumu` ve üç rolün varsayılanı `"urun"` içeriyor). Sonuç: **migration ve tohumlama göçü gerekmedi**, mevcut kiracıların yetki matrisi değişmedi.

3. **Varsayılan listenin "Pasife al" düğmesi `DegismezIpucu` yerine kapalı düğme + gerekçe ile çizildi.**
   `DegismezIpucu` hiç `<button>` üretmez ve *doğası gereği değişmez* kayıtlar içindir (`DuzeltmeEkranTestleri` bunu zorluyor). Fiyat listesi düzenlenebilir bir kayıttır; yalnız tek bir eylemi koşullu kapalıdır. Doğru sözleşme UX-S30'dur: "kilitli düğme sebebini söyler". Spesifikasyonun madde 2 satırı da "varsayılan listede pasife al **disabled**" diyordu.

4. **`dto.Varsayilan` üç durumlu (`bool?`) yapıldı.**
   Talimat "false gelince korunur" diyordu; `bool?` ile hem `null` (gönderilmedi) hem `false` KORUR — yani talimat harfiyen karşılanır — ve XML yorumu sözleşmeyi açıkça yazar. Mevcut çağıranlar (`Varsayilan = true/false`) örtük dönüşümle derlenmeye devam eder.

5. **Yasal/mevzuat hükmü sabit yazılmadı.** Bu pakette yeni mevzuat hükmü uygulanmadı; mevcut FEY md.11/3 (92 gün) ve TRHTUY md.14 kodları **aynen korundu**, dokunulmadı.

---

## 5. Migration

**Gerekmedi.** Şema değişikliği yok: yeni varlık, yeni kolon, yeni indeks yok. `FiyatListesiDto.Varsayilan`'ın `bool?` olması bir **DTO** değişikliğidir; `FiyatListesi` varlığındaki `Varsayilan` kolonu `bool` kalmıştır. `dotnet ef` hiç çalıştırılmadı (derleme kırıkken çalıştırma yasağı da bu yüzden devreye girmedi). Migration sayısı **22** olarak kaldı.

---

## 6. Test sayıları

| | Önce | Sonra |
|---|---|---|
| Toplam test | 1193 | **1216** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | **0** |

**Yeni test sayısı: 23**

- `Kuyumcu.Tests/FiyatVarsayilanKorumaTestleri.cs` — **5** (DB/servis)
  `PATCH_varsayilan_FALSE_gelince_varsayilanlik_KORUNUR` · `Varsayilanlik_baska_listeye_DEVREDILIR_ve_tek_varsayilan_kalir` · `Varsayilansiz_birakan_guncelleme_400_verir` · `Varsayilan_liste_PASIFE_ALINAMAZ` · `TeklifListeyle_SECILEN_listenin_marjini_uygular`
- `Kuyumcu.Tests/HttpFiyatYetkiTestleri.cs` — **7** (gerçek HTTP boru hattı)
  `TEZGAHTAR_fiyat_listesini_GUNCELLEYEMEZ_403` · `TEZGAHTAR_kampanya_OLUSTURAMAZ_403` · `TEZGAHTAR_fiyat_listelerini_OKUYABILIR` · `MUDUR_fiyat_listesini_GUNCELLEYEBILIR` · `Urun_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403` · `Varsayilan_false_PATCH_sonrasi_teklif_URETILMEYE_devam_eder` · `Teklif_listeId_ile_SECILEN_listeyi_fiyatlar`
- `Kuyumcu.Tests/FiyatlandirmaUX4aEkranTestleri.cs` — **11** (bUnit)
  `Duzenle_formu_MEVCUT_degerlerle_dolar_ve_PATCH_e_gider` · `Varsayilan_listede_PASIFE_AL_kapali_ve_sebebi_yazili` · `Pasife_al_TAM_govdeyle_PATCH_atar` · `Kampanya_duzenleme_PATCH_e_gider_ve_aktif_tasir` · `Liste_secilince_urun_paneli_acilir_ve_teklif_listeId_ile_cagrilir` · `Toplu_marj_onizlemesi_SALT_HESAP_tir_istek_atmaz` · `ISCILIK_YUZDE_secilince_bilgi_kutusu_gorunur` · `Gumus_hedefli_kampanyada_KAYDET_kilitli_ve_uyari_var` · `Kayit_sonrasi_BASARI_bandi_cikar` · `Hic_liste_yokken_VARSAYILAN_isaretli_ve_KAPALI_bos_durum_var` · `Sunucu_hatasinda_HATA_bandi_cikar_BOS_DURUM_cikmaz`

Ayrıca mevcut `FiyatlandirmaEkranTestleri.cs`'in sahte API'si güncellendi (yeni uçlara gerçekçi yanıt: `ayarId`/`hasGrami` taşıyan ürün, `/kur/guncel`, `/fiyat/teklif`) — tuzak listesindeki "`{}` varsayılanı" kuralı gereği.

---

## 7. Mutasyon tablosu

Yöntem: kaynak `…/scratchpad/mut-yedek/UX-4a/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı. Son mutasyondan sonra temiz `dotnet build`: **0 uyarı 0 hata**.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alım |
|---|---|---|---|---|
| **M1-1** | `FiyatModulu.cs` | `if (varsayilanIstendi) liste.Varsayilan = true;` → `liste.Varsayilan = varsayilanIstendi;` (eski hata geri getirildi) | `PATCH_varsayilan_FALSE_gelince_varsayilanlik_KORUNUR`, `Varsayilansiz_birakan_guncelleme_400_verir`, `Varsayilan_false_PATCH_sonrasi_teklif_URETILMEYE_devam_eder` | `cmp` birebir |
| **M14-1** | `FiyatModulu.cs` | Sınıftaki `[Yetki.ModulYetkisi("urun")]` kaldırıldı | `Urun_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403` | `cmp` birebir |
| **M14-2** | `FiyatModulu.cs` | `PATCH listeler/{id}` ucundan `RolGerekli("SAHIP","MUDUR")` kaldırıldı | `TEZGAHTAR_fiyat_listesini_GUNCELLEYEMEZ_403` | `cmp` birebir |
| **M14-3** | `SayimModulu.cs` | Sınıftaki `[Yetki.ModulYetkisi("urun")]` kaldırıldı | `Urun_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403` | `cmp` birebir |
| **M2-1** | `Fiyatlandirma.razor` | `ListeKaydet` daima POST atacak biçimde değiştirildi (çift amaçlı form bozuldu) | `Duzenle_formu_MEVCUT_degerlerle_dolar_ve_PATCH_e_gider` | `cmp` birebir |
| **M3-1** | `FiyatListesiUrunPaneli.razor` | Teklif URL'inden `&listeId={ListeId}` kaldırıldı | `Liste_secilince_urun_paneli_acilir_ve_teklif_listeId_ile_cagrilir` | `cmp` birebir |
| **M4-1** | `Fiyatlandirma.razor` | `#km-kaydet` `disabled` ifadesinden `GumusHedef` kaldırıldı | `Gumus_hedefli_kampanyada_KAYDET_kilitli_ve_uyari_var` | `cmp` birebir |
| **M5-1** | `Fiyatlandirma.razor` | `else if (yuklendi && hata is null && listeler.Count == 0)` → `else if (listeler.Count == 0)` (hata yutma geri geldi) | `Sunucu_hatasinda_HATA_bandi_cikar_BOS_DURUM_cikmaz` | `cmp` birebir |
| **M5-2** | `Fiyatlandirma.razor` | `new ListeForm { Varsayilan = IlkListe }` → `new ListeForm()` | `Hic_liste_yokken_VARSAYILAN_isaretli_ve_KAPALI_bos_durum_var` | `cmp` birebir |

(9 mutasyon uygulandı; hiçbiri "test açığı" vermedi — her biri en az bir adlandırılmış testi kırdı.)

---

## 8. `dotnet/denetim.sh` çıktısı

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)         1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)      1
  ✓ VeresiyeKapanisi (yalnız Etiketten)           2
  ✓ IKiraciVarlik uygulayan varlık                32
  ✓ HasQueryFilter                                 2

═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı                                 var
  ✓ HTTPS yönlendirme                             var
  ✓ sıkıştırma                                 var
  ✓ sağlık ucu                                   var
  ✓ ArayuzModu (yalnız 6 dosya)                   6
  ✓ ArayuzModu kapı yüzeyinde (0 olmalı)        0
  ✓ CORS                                           beyaz liste

DENETİM TEMİZ.
```

---

## 9. Canlı doğrulama (localhost:5198 — kurucunun 5199 sunucusuna DOKUNULMADI)

Test kiracısı: `PAKETUX-4a-TEST` (`01M2MSYA5AFCGYQC03J67AH6TX`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` (PAKETUX-4a-TEST) | 201 | **201** |
| 2 | `GET /fiyat/listeler` (SAHIP) | 200 `[]` | **200 `[]`** |
| 3 | `POST /fiyat/listeler` `{varsayilan:false}` (ilk liste) | 200, `varsayilan:true` (sunucu zorlar) | **200, `"varsayilan":true`** |
| 4 | `GET /kur/guncel` | id gelene kadar bekle | **`01M2MSYXM79MEGZTRZ0RDA39K1`, hasFiyatı 6771,45** |
| 5 | `POST /urunler` K22 10 g (işçilik 500) ve G925 12 g | 201 · 201 | **201 · 201** (`hasGrami` 9,16 / 11,10) |
| 6 | `GET /fiyat/teklif` (ÖNCE) | `teklifVar:true` | **`{"teklifVar":true,"listeFiyati":65627.80,"marjTl":3101.32,...}`** |
| 7 | **Madde 1:** `PATCH /fiyat/listeler/{id}` tam gövde `varsayilan:false` | 200 ve `varsayilan:true` KALIR | **200, `"varsayilan":true`, ad ve marj güncellendi** |
| 8 | `GET /fiyat/teklif` (SONRA) | `teklifVar:true` (POS durmaz) | **`{"teklifVar":true,"listeFiyati":66248.07,"marjTl":3721.59,...}`** |
| 9 | **Madde 3:** ikinci liste (%25 marj, %50 çarpan) + `GET /fiyat/teklif?...&listeId={L2}` | seçilen listenin marjı uygulanır | **`listeFiyati 77783.10, marjTl 15506.62, iscilikTl 250.00`** (varsayılanda 66248,07 / 3721,59 / 500,00) |
| 10 | **Madde 4 gerekçesi:** gümüş üründe teklif | `teklifVar:false` | **`{"teklifVar":false,"sebep":"FIYAT_YOK"}`** |
| 11 | **Madde 14:** TEZGAHTAR `PATCH /fiyat/listeler/{id}` | 403 | **403 — "Bu işlem için gerekli rol: SAHIP veya MUDUR"** |
| 12 | TEZGAHTAR `GET /fiyat/listeler` | 200 (okuma açık) | **200** |
| 13 | **Madde 14:** `urun` yetkisi alınan MUDUR `GET /fiyat/listeler` | 403 | **403 — "Bu ekran için yetkiniz yok (urun)"** |
| 14 | aynı MUDUR `GET /sayim` | 403 | **403 — "Bu ekran için yetkiniz yok (urun)"** |
| 15 | Varsayılan listeyi pasife alma denemesi | 400 | **400 — "Varsayılan fiyat listesi pasife alınamaz…"** |
| 16 | Varsayılanlığı ikinci listeye devretme | 200, devir | **200, Toptan `varsayilan:true`** |

**SQL kanıtları** (`docker exec kuyumcu-mysql mysql … kuyumcu_net`):

```
-- adım 7 sonrası: ad + marj değişti, varsayilan KORUNDU
SELECT id, ad, varsayilan, aktif, has_marj_yuzde FROM fiyat_listeleri WHERE tenant_id='…';
01M2MSYR4XHBFT9A4FDCNRV861   Perakende düzeltildi   1   1   6.000

-- adım 16 sonrası: tek varsayılan, devredildi
Perakende düzeltildi   0   1
Toptan                 1   1

-- adım 13 öncesi yetki kaldırma kanıtı
SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='…' AND rol='MUDUR' AND modul='urun';  → 0
```

**Temizlik:** API (PID 97696) durduruldu — `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**; 5199'daki kurucu sunucusu çalışmaya devam ediyor (dokunulmadı). Test kiracısı `information_schema`'dan çekilen **32 tenant_id'li tablodan** ve `tenants`'tan silindi; `fiyat_listeleri`/`urunler`/`kullanicilar` sayıları **0**, `tenants` sayısı **6 → 6**.

---

## 10. Değişen dosyalar

Yeni:
- `dotnet/Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor`
- `dotnet/Kuyumcu.Tests/FiyatVarsayilanKorumaTestleri.cs`
- `dotnet/Kuyumcu.Tests/HttpFiyatYetkiTestleri.cs`
- `dotnet/Kuyumcu.Tests/FiyatlandirmaUX4aEkranTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-4a-uygulama-raporu.md`

Değişen:
- `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR + EN, 29 yeni anahtar)
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor`
- `dotnet/Kuyumcu.Tests/FiyatlandirmaEkranTestleri.cs` (sahte API'ye gerçekçi yanıtlar)

> Not: `git status --porcelain` çalışma ağacında önceki paketlerden gelen çok sayıda değişiklik gösteriyor; yukarıdaki liste YALNIZ bu pakette dokunulan dosyalardır. Yeni CSS yazılmadı (mevcut sınıflar: `kart`, `tablo-sar`, `veri-tablosu`, `satir-eylemler`, `uyari-kutu`, `rozet`, `metin-kucuk`, `metin-soluk`). Satır içi `style=""` EKLENMEDİ (tablo başlıklarındaki mevcut `style="text-align:right"` ifadeleri paket 17'den kalma ve dokunulmadı). Mevcut element id'leri değişmedi; yeni id'ler `fl-…`, `km-…`, `flp-…` önekli.

---

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam yalnız 1,2,3,4,5,14 | ✔ | 6-13 dosyalarına (Urunler, Islemler, Kur, Cariler, Emanet, Sayim.razor, Kullanicilar) dokunulmadı |
| Sıra 1 → 14 → 2 → 3 → 4 → 5 | ✔ | Bu raporun § 2 sırası uygulama sırasıdır |
| Q2: FiyatModulu + SayimModulu `[ModulYetkisi("urun")]` | ✔ | canlı adım 13-14, M14-1/M14-3 |
| Q2: fiyat yazma uçlarına `[RolGerekli("SAHIP","MUDUR")]` | ✔ | canlı adım 11, M14-2 |
| Q2: menü "rapor" → "urun", metin "Fiyat listeleri", simge ayrı | ✔ | `KenarCubugu.razor` 📋; sözlük zaten "Fiyat Listeleri" |
| Q2: HTTP testleri (TEZGAHTAR 403 · yetkisiz MUDUR 403 · mutasyon) | ✔ | `HttpFiyatYetkiTestleri` 7 test + M14-1/2/3 |
| Q3: gümüşte yalnız sabit fiyat; kampanya hedefi gümüşse kaydet kilitli + uyarı | ✔ | `#km-gumus-uyari`, `Gumus_hedefli_kampanyada_KAYDET_kilitli_ve_uyari_var` |
| Q3: has kuruyla gümüş fiyatlama yolu açılmadı | ✔ | `FiyatMotoru` kural 2 dokunulmadı |
| Madde 1: false gelince koru; varsayılansız kalacaksa 400 | ✔ | canlı adım 7, `Varsayilansiz_birakan_guncelleme_400_verir` |
| Madde 1: mutasyon + canlı teklifVar:true | ✔ | M1-1, canlı adım 8 |
| Madde 2: Düzenle + Pasife al/Aktifleştir, çift amaçlı form, Aktif+Varsayılan formda | ✔ | 4 bUnit testi |
| Madde 2: ikinci mekanizma açılmadı, UX-1/UX-3 bileşenleri kullanıldı | ✔ | `ProAlan` kullanıldı; `DegismezIpucu` gerekçeli kullanılmadı (§ 4.3) |
| Madde 3: mevcut `/fiyat/teklif`, ilk 50 ürün, toplu marj salt hesap, tek bileşen | ✔ | `FiyatListesiUrunPaneli.razor`, `Toplu_marj_onizlemesi_SALT_HESAP_tir_istek_atmaz` |
| Madde 3: yeni uç/tablo yazılmadı | ⚠ kısmi | Yeni uç/tablo YOK; mevcut uca opsiyonel `listeId` eklendi (§ 4.1 gerekçe) |
| Madde 5: Yukleniyor/BasariBandi/BosDurum, ilk listede kilitli kutu | ✔ | 3 bUnit testi |
| Madde 5: hata yutma kalıbı kaldırıldı | ✔ | `Sunucu_hatasinda_HATA_bandi_cikar_BOS_DURUM_cikmaz`, M5-1 |
| UX-S21…S33 + belirteç sistemi (sabit renk yok, satır içi style yok) | ✔ | yeni CSS yazılmadı; `TasarimBelirtecTestleri` yeşil |
| Kapılar (tevsik/MASAK/gün kilidi/kasa para birimi) ve `HareketFabrikasi` değişmedi | ✔ | `denetim.sh`: `Hareketler.Add` = 1 |
| 5199'a dokunulmadı | ✔ | 5199 PID 88090 çalışmaya devam ediyor |
| COMMIT yok | ✔ | `git log` başı `bbfdc95` (değişmedi) |

---

## 12. Açık kalanlar

1. **Madde 6-13 yapılmadı** — UX-4b/UX-4c paketleri (Urunler konsinye sessiz silinmesi · POS türetilebilir alanlar · Kur GBP · Cari kasa bacağı · Emanet · Sayım ekranı · Kullanıcı şubesi · genel hata yutma taraması).
2. **Paket 49'a bırakılanlar:** toplu önizleme ucu (`GET /fiyat/listeler/{id}/onizleme`), ürün bazlı geçersiz kılma (gümüşte sabit fiyat), toplu marj UYGULAMA, panelde sayfalama/tam ürün görünümü. Panel bunun için tek bileşende tutuldu.
3. **Kampanya formundaki diğer bulgular** (örnek ürün önizlemesi, "N ürün etkilenecek" satırı, ürün seçicinin `datalist`e çevrilmesi, `Has marjı`/`İşçilik çarpanı` ipucu satırları) bu paketin 1-5/14 kapsamında DEĞİLDİ — spesifikasyonun "Tüm bulgular" listesinde duruyor.
4. **Liste başına bağlı cari sayısı** sütunu yapılmadı (spesifikasyonda ORTA öncelikli, yeni uç gerektiriyor).
5. **Kiracı içi yan etki:** `"urun"` modül yetkisi alınmış bir rol artık Cariler ekranındaki fiyat listesi seçicisini de dolduramaz (`Cariler.razor` `/fiyat/listeler` çağrısı 403 alır). Mevcut kod bu durumu zaten güvenli karşılıyor (seçici boş kalır + hata bandı; cari düzenleme çökmez). Varsayılan yetki matrisinde üç rolde de `"urun"` olduğu için üretimde tetiklenmez.
6. **Önceki oturumlardan kalan artıklar (kurucu elle silsin — hook `rm` blokluyor):** `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs.mutorj`, `FiyatMotoru.cs.mutorj`, `dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor.mutorj`, `Sayim.razor.mutorj`, `Islemler.razor.mutorj`, `Urunler.razor.mutorj`, `VadeTakibi.razor.mutorj`, `dotnet/Kuyumcu.Tests/VadeTakibiTestleri.cs.ek` ve `Data/Migrations/` altındaki 0 baytlık dosyalar. **Bu pakette üretilmedi**, tarihleri 15 Eylül. Bu paket proje ağacına hiçbir `.mutorj/.bak/.tmp` bırakmadı (yedekler scratchpad'de).
7. **Kararsız test:** tam süit bu pakette tek koşuda 1216/1216 yeşil verdi; önceki oturumdan miras "8 koşuda 1 kırmızı" belirsizliği bu oturumda **yeniden üretilmedi**, çözülmüş sayılamaz.

---

## 13. Kurucuya / mali müşavire notlar

- **Yasal hüküm eklenmedi.** Bu paket hiçbir mevzuat eşiğini/oranını koda yazmadı; mevcut FEY md.11/3 (kampanya en fazla 92 gün) ve TRHTUY md.14 (indirim öncesi 30 günlük en düşük fiyat) kuralları aynen korundu. Mali müşavir onayı gerektiren yeni bir şey yok.
- **Ticari karar — bilgi amaçlı:** artık fiyat listesi ve kampanya oluşturmak/değiştirmek **yalnız SAHİP ve MÜDÜR** rollerine açıktır; tezgâhtar ve muhasebe fiyatları görebilir ama değiştiremez. Bu Q2 ekip oyuyla mühürlendi; farklı bir dağılım isterseniz tek satır değişiklik yeter.
- **Davranış değişikliği — bilinmesi gereken:** varsayılan fiyat listesinin varsayılanlığı artık ekrandan "kaldırılamaz", yalnız **başka bir listeye devredilir**. Bu bilinçlidir: varsayılansız kiracıda POS hiçbir ürüne otomatik fiyat veremez.
