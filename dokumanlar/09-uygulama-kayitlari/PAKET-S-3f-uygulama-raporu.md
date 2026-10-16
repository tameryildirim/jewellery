# PAKET S-3f — QA turu 5 kalan yüksek bulgular + düzenlenemeyen kalemler (2. dilim) — uygulama raporu

- Tarih: 2026-09-18
- Rol: Taşkın (backend) + Ertuğ (frontend) · Opus
- Dalga: 5-H (QA turu 5 düzeltmeleri)
- Kaynak: `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md`
- Durum: **YARIM** — 13 YÜKSEK bulgunun **11'i kapandı**, 2'si gerekçeyle devredildi
  (aşağıda § 9 "Açık kalanlar").

---

## 1. Yapılanlar (bulgu → düzeltme)

| # | Ekran | Bulgu (QA turu 5, YÜKSEK) | Yapılan | Katman |
|---|---|---|---|---|
| 1 | `/isletme` | İşletme kimliği telefonu doğrulanmıyor: `abc` kaydediliyor | Kural **şube formuyla TEK kaynaktan** (`Kuyumcu.Core.Domain.Iletisim`): DTO'ya `[RegularExpression]`, servise elle `TelefonGecerli` kapısı, ekrana alan altı mesaj + `KimlikGecerli` kilidi (UX-S30/S31) | API + Web |
| 2 | `/gider-pusulalari` | Boş adresle pusula kaydedilebiliyor (VUK md.234) | Adres **ve** mal açıklaması: gövdede gelirse **boş olamaz** (400). Ekranda alan altı Türkçe mesaj + Kaydet kilidi + kilit sebebi | API + Web |
| 3 | `/raporlar` | "Stok Dağılımı" pastası tek kategori %100 iken çizilmiyor (yozlaşmış yay) | `PastaGrafik`: yay ≥ 359,99° ise **iki yaylı tam daire** çizilir; `L` ile merkeze dönüş yok | Web |
| 4 | `/evraklar/takvim` | Portföyde 2 evrak varken "Portföyde evrak yok" | Sunucu takvim yanıtına **dönemden bağımsız portföy özeti** eklendi (`portfoyAdet/Tutar/İlkVade/SonVade/azamiGun`); ekran iki durumu **ayırır** (aralık boş ≠ portföy boş) ve portföy toplamını yazar | API + Web |
| 5 | `/evraklar/takvim` | Gün bağlantısındaki `vadeBas/vadeBit` hedef sayfada uygulanmıyor | `/evraklar` ilk yüklemede sorguyu okur, süzgeç kutularına yazar, listeyi süzer; "Vade süzgeci bağlantıdan uygulandı" rozeti + "Süzgeci temizle" | Web |
| 6 | `/evraklar/takvim` | (aynı bulgunun ikinci yarısı) aralık dışına çıkış yolu yok | "Aralığı tüm portföye genişlet" düğmesi; sunucu sınırı aşılıyorsa **kilitli + sebep** | Web |
| 7 | `/pos` | "Bağlı Kasa" oluşturmadan sonra düzenlenemiyor, gösterilen düzeltme yolu ilgisiz | PATCH `kasaId` **açıldı** (yalnız cihazın hiç tahsilatı yokken; şube kasadan türetilir). Tahsilatı varsa kilit + **bu alana özgü** gerekçe/yol. Ekran kararı sunucudan gelen `tahsilatVar` ile verilir | API + Web |
| 8 | `/kurulum` | Kuru prova NEGATİF açılış bakiyesini geçerli sayıyor | Kasa/banka satırında `tlTutar < 0` → yeni satır hatası `NEGATIF_ACILIS`; **cari'de değil** (müşteri alacaklı olabilir). İstemcide `min="0"` + alan altı mesaj | API + Web |
| 9 | `/kullanicilar` | 429'da sayfa sessizce boş kalıyor | Hata artık listeyi **silmez**; liste boşsa "liste yüklenemedi" durumu + hata metni + **"Yeniden dene"**; yalnız 429'da üstel geri çekilmeyle (2·4·8 sn, en çok 3) otomatik tekrar | Web |
| 10 | `/yetkiler` | ROL sütunu donuk değil, kutuya tıklanınca kayboluyor | `app.css` `.tablo-yapiskan-ilk` (position: sticky; left: 0), matris tablosuna uygulandı. Renk **belirteçten** gelir → karanlık tema kendiliğinden doğru | Web (CSS) |
| 11 | `/siparisler` | Durum geçişi onay sormadan uygulanıyor, geri alınamıyor, Düzenle'yi de kapatıyor | (a) Her geçiş **onay kipinden** geçer (DoD EK md.4); (b) `ONAYLANDI → TASLAK` geri geçişi **açıldı**, gerekçe zorunlu (≥10) ve denetim izine yazılır; (c) ONAYLANDI'da "Düzenlemek için önce Taslak'a geri alın" yolu ekranda yazılı | API + Web |
| 12 | `/kilavuz` | "Uygulamayı başlatma" yanlış adres + geliştirici yordamı anlatıyor | Bölüm son kullanıcı diline çevrildi: masaüstü kısayolu. **Sabit adres YAZILMADI** (kuruluma göre değişir; uydurmak Anayasa md.1 ihlali olurdu). `localhost:5000` · `start.bat` · `stop.bat` sayısı 0 | Varlık |
| 13 | `/kilavuz` | 9 ekran hiç anlatılmıyor | Vezne · Emanet · Siparişler · Sayım · POS · Uyum · Gider pusulaları · Yedekler · Veri aktarımı bölümleri yazıldı + sol menüye eklendi + **rota↔bölüm kontrol listesi** dosyaya gömüldü | Varlık |

### DoD sözleşmesi (Bertan koşulu)

Bu pakette **yeni varlık/kayıt türü DOĞMADI** — yalnız mevcut varlıkların düzenlenebilirlik
borcu kapatıldı. Dokunulan varlıkların durumu:

| Varlık | Düzenleme yolu | Yetki | Denetim | Not |
|---|---|---|---|---|
| Tenant kimliği (telefon) | `PATCH /admin/tenant/kimlik` (üç durumlu PATCH korunuyor) | `[RolGerekli("SAHIP")]` (mevcut) | `DenetimServisi.YazmaKaydet` **mevcut çağrı**, yeniden yazılmadı | Yalnız doğrulama eklendi |
| Gider pusulası (adres · mal açıklaması) | `PATCH /gider-pusulalari/{id}` | mevcut | mevcut `YazmaKaydet(hassas: true)` | **Düzeltilebilir ama silinemez** — VUK md.234 zorunlu unsuru. Diğer alanlar değişmez; yolu İPTAL + YENİ PUSULA (ekranda `DegismezIpucu`) |
| POS cihazı (bağlı kasa) | `PATCH /pos/cihazlar/{id}` `kasaId` | mevcut | mevcut | Tahsilat yazılmışsa **kilitli**; gerekçe ve yol bu alana özgü metinle ekranda |
| Sipariş (durum) | `POST /siparisler/{id}/gecis` — artık geri yönlü de | mevcut (`IPTAL` yalnız SAHIP/MÜDÜR) | `siparis_gecisleri` satırı + gerekçe | Geri geçiş gerekçesi denetim izinde; canlı SQL kanıtı § 8 |
| Evrak takvimi | Türetilmiş görünüm — düzenlenmez | — | — | `DegismezIpucu` mevcut; portföy özeti salt okunur |

Finansal varlıklarda hard DELETE **açılmadı**; hiçbir silme ucu eklenmedi.

---

## 2. Sapmalar ve gerekçeleri

1. **`/kullanicilar` yüklenemedi durumunda `BosDurum` bileşeni KULLANILMADI.**
   İlk uygulamada kullanılmıştı; `UX4cEkranTestleri.M13` değişmezi ("hata boş duruma
   çevrilmez") kırıldı. Bileşen yerine adı konmuş ayrı bir blok (`#k-liste-yuklenemedi`)
   yazıldı — **test değil KOD düzeltildi**.
2. **Otomatik yeniden deneme yalnız 429'da.** 500/404'te otomatik tekrar **açılmadı**:
   kalıcı hatada sonsuz döngü riski. Düğme her durumda var.
3. **Geri geçiş yalnız `ONAYLANDI → TASLAK`.** `ATOLYEDE`/`HAZIR`'dan geri dönüş
   AÇILMADI: mal fiziken atölyeye çıkmış olabilir, geri alma kuralı **DOĞRULANMADI**
   (Anayasa md.1 — bilinmeyen tahmin edilmez). Kurucuya not § 10.
4. **`/siparisler` PATCH'i TASLAK'a bağlı kaldı.** Sunucu düzenlemesini ONAYLANDI'ya
   açmak yerine geri geçiş yolu açıldı; böylece onaylı belge yerinde değişmez, izi kalır.
5. **Kılavuzda sabit adres YAZILMADI.** Bulgunun önerisi "kurulumdan gelen gerçek adres"
   diyor; uygulamanın kendisi bu adresi bilmiyor. Uydurmak yerine "kısayol" anlatıldı ve
   adresin kurulumu yapan kişiden geldiği yazıldı.
6. **Tek ModalPencere.** Sipariş detayında ikinci bir `ModalPencere` açmak
   `dotnet/denetim.sh` tavanını (53) aşıyordu; iptal ve geçiş onayı **tek kip** içinde
   dallandı. Mevcut element kimlikleri (`sipd-iptal-*`) **değişmedi**.
7. **VUK md.234 hükmü koda sabit yazıldı mı?** Hayır — yapılandırma anahtarı da
   açılmadı. Kural "gönderilen alan boş olamaz" biçiminde, **oran/eşik içermeyen**
   bir biçim kuralıdır; mevzuatın değişen kısmı (stopaj oranı, süre) bu pakette
   ellenmedi. Mali müşavir notu § 10.

### Kapsam kilidi (kurucu talimatı 13)
Yeni ekran/modül/özellik AÇILMADI. Devredilen maddeler § 9'da "devredildi: …" ile yazılı.

---

## 3. Migration

**Gerekmedi** — şema değişikliği yok. Eklenen alanlar yalnız **yanıt sözleşmesi**
alanlarıdır (`EvrakTakvimi.Portfoy*`, `PosCihazSatir.TahsilatVar`); ikisi de var olan
kolonlardan türetilir. `kuyumcu_net` üzerinde `dotnet ef` ÇALIŞTIRILMADI.

## 4. Derleme

`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı / 0 Hata** (mutasyon turlarından sonra
tekrarlandı, sonuç aynı).

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `--filter S3fSunucuTestleri` (DB + saf) | 15 | 15 | 0 |
| `--filter S3fEkranTestleri` (bUnit) | 21 | 21 | 0 |
| `--filter S3fVarlikTestleri` (grafik · CSS · kılavuz) | 6 | 6 | 0 |
| `--filter SiparisEkranTestleri` (uyarlanan) | 17 | 17 | 0 |
| `--filter UX1C3EkranTestleri\|S3dHttpTestleri\|SiparisDurumMakinesiTestleri\|UX4cEkranTestleri` | 94 | 94 | 0 |
| **Tam süit** (`--logger trx`) | **2893** | **2893** | **0** |

- Paket öncesi toplam: **2851** (PAKET S-3e raporu § 5).
- **Yeni test sayısı: 42** = 15 (`S3fSunucuTestleri`) + 21 (`S3fEkranTestleri`) +
  6 (`S3fVarlikTestleri`). Süit toplamı 2851 → **2893** (fark tam 42). Değiştirilen
  4 mevcut test sayıyı artırmaz (§ "Değiştirilen MEVCUT testler").

### Değiştirilen MEVCUT testler (sözleşme değişikliği — gizlenmiyor)

| Test | Neden |
|---|---|
| `SiparisEkranTestleri.Gecis_dugmesi_gercek_istek_atar_...` | Geçiş artık onay kipinden geçer; test önce "istek YOK" sonra onay → istek ölçüyor |
| `SiparisDurumMakinesiTestleri.Izinli_gecis_listesi_...` | Beklenen tabloya `TASLAK` geri hedefi eklendi (3 tür) |
| `S3dHttpTestleri.PATCH_sozlesmesi_gonderilmeyen_alan_KORUNUR_null_SILINIR` | "null = sil" dalı VUK md.234 gereği kapatıldı → 400 bekleniyor. "Gövdede yoksa korunur" dalı **aynen duruyor** |
| `UX1C3EkranTestleri.C3_10_Pusula_detayi_CEKMECEDE_acilir_...` | Fikstürde `malAciklamasi: null` olduğu için Kaydet artık kilitli; iddia "kilit yok" yerine "kilit var **ve sebebi yazılı**" oldu |

## 6. Mutasyon kanıtları

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|---|
| M1 | `AdminModulu.cs` | telefon kapısı → `if (false)` | `Isletme_kimliginde_gecersiz_telefon_400_dondurur` | cmp = aynı |
| M2 | `HurdaModulu.cs` | adres zorunluluk kapısı silindi | `S3dHttpTestleri.PATCH_sozlesmesi_gonderilmeyen_alan_KORUNUR_null_SILINIR` | cmp = aynı |
| M3 | `AcilisServisi.cs` | negatif kasa kapısı silindi | `Negatif_KASA_acilisi_provada_SATIR_HATASI_olur_ve_jeton_URETILMEZ` | cmp = aynı |
| M4 | `SiparisDurumMakinesi.cs` | `TASLAK` geri hedefi kaldırıldı (3 dal) | `Izinli_gecis_listesi_tur_bazinda_birebir_dogrulanir`, `Onaylandi_durumundan_TASLAK_a_geri_donus_IZINLIDIR` | cmp = aynı |
| M5 | `SiparisDetay.razor` | düğme onay kipini atlayıp doğrudan `Gecis` çağırıyor | 5 test (`Durum_dugmesine_tek_tiklama_ISTEK_ATMAZ…`, `Onay_kipinde_ONAYLA…`, `Onay_kipinde_VAZGEC…`, `Geri_gecis_GEREKCE_ister…`, `SiparisEkranTestleri.Gecis_dugmesi…`) | cmp = aynı |
| M6 | `Evraklar.razor` | `SorgudanSuzgecOku()` çağrısı silindi | `Vade_sorgu_parametreleri_ILK_YUKLEMEDE_okunur_ve_listeye_uygulanir` | cmp = aynı |
| M7 | `EvrakTakvimi.razor` | portföy dalı → `@if (false)` | `Aralik_bossa_ama_PORTFOYDE_evrak_varsa…`, `Araligi_portfoye_genislet…` | cmp = aynı |
| M8 | `Kullanicilar.razor` | yüklenemedi dalı → `else if (false)` | `Oran_sinirinda_YENIDEN_DENE…`, `Yeniden_dene_GERCEK_istek_atar…` | cmp = aynı |
| M9 | `GiderPusulalari.razor` | `DuzenlemeGecerli` kapısı gevşetildi | 4 test (`Adres_bosaltilinca…`, `Mal_aciklamasi_bosaltilinca…`, `Bos_adresle_HICBIR_PATCH…`, `UX1C3…C3_10`) | cmp = aynı |
| M10 | `Pos.razor` | düzenlemede kasa seçici yine gizlendi | `Tahsilati_OLMAYAN_cihazda_bagli_kasa_secicisi…`, `Kasa_degistirilince_PATCH_govdesinde_kasaId_gider` | cmp = aynı |
| M11 | `PastaGrafik.razor` | tam daire dalı → `if (false)` | `Tek_kategori_yuzde_yuz_iken_pasta_TAM_DAIRE_cizer` | cmp = aynı |
| M12 | `Yetkiler.razor` | `tablo-yapiskan-ilk` sınıfı kaldırıldı | `Yetki_matrisinde_ilk_sutun_YAPISKAN_sinifini_tasir` | cmp = aynı |
| M13 | `app.css` | `position: sticky` → `static` | `app_css_yapiskan_ilk_sutun_kuralini_ICERIR…` | cmp = aynı |
| M14 | `kilavuz.html` | `<section id="vezne">` devre dışı | `Kilavuz_menudeki_HER_ekran_icin_bir_bolum_tasir` | cmp = aynı |
| M15 | `PosModulu.cs` | `TahsilatVar` projeksiyonu → `false` | **İLK TURDA HİÇBİR TEST KIRILMADI → TEST AÇIĞI.** 3 sunucu testi eklendikten sonra: `Tahsilati_OLAN_cihazda_TahsilatVar_TRUE_ve_kasa_degisimi_REDDEDILIR` | cmp = aynı |
| M16 | `PosModulu.cs` | tahsilat kapısı → `if (false)` | `Tahsilati_OLAN_cihazda_TahsilatVar_TRUE_ve_kasa_degisimi_REDDEDILIR` | cmp = aynı |
| M17 | `EvrakModulu.cs` | portföy özeti sabit `0/null` döndürüldü | **İLK TURDA HİÇBİR TEST KIRILMADI → TEST AÇIĞI.** 2 sunucu testi eklendikten sonra: `Takvim_ARALIK_bos_olsa_bile_PORTFOY_ozetini_dondurur` | cmp = aynı |

**İki test açığı bulundu ve kapatıldı (M15, M17).** İkisi de sunucu projeksiyonuydu:
ekran testleri sahte API kullandığı için sunucunun alanı yanlış doldurmasını göremiyordu.
Ders: **sahte API ile ölçülen her yeni yanıt alanı için sunucu tarafında ayrı bir test şart.**
Son mutasyondan sonra `dotnet build` → 0 hata, tam süit → 0 başarısız.

## 7. denetim.sh

`dotnet/denetim.sh` → **DENETİM TEMİZ.**

Ara koşumda **iki ihlal** çıktı ve düzeltildi (gizlenmiyor):
- `Pages: @onclick'siz ve disabled'sız <button> 73 (tavan 71 — ARTIŞ)` → sayaç **satır
  bazlıdır**; iki yeni düğmede `disabled=` / `@onclick=` alt satıra düşmüştü, `<button`
  ile aynı satıra alındı.
- `Pages: ModalPencere kullanımı 54 (tavan 53 — ARTIŞ)` → sipariş detayındaki iki kip
  **tek** `ModalPencere` içinde dallandırıldı (§ 2/6).

## 8. Canlı doğrulama (5198, Development)

Kendi örneğim `--urls http://localhost:5198`; `Sistem__Anahtari` yalnız bu süreç için
ortam değişkeniyle üretildi, değeri hiçbir yere yazılmadı. **5199'a DOKUNULMADI.**
Test kiracısı: `PAKETS-3f-TEST Kuyumcu` (`01M2SSGNJAF76SFFRTEXG8RXA0`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `PATCH /admin/tenant/kimlik {"telefon":"abc"}` | 400 | **400** · "Telefon yalnız rakam, boşluk, parantez ve tire içerebilir; başında + olabilir (10–13 hane)" |
| 2 | `PATCH … {"telefon":"+90 212 555 44 33"}` | 200 | **200** |
| 3 | SQL | telefon yazıldı | `SELECT id,ad,telefon FROM tenants …` → `PAKETS-3f-TEST Kuyumcu  +90 212 555 44 33` |
| 4 | `POST /kurulum/acilis/prova` KASA `-250` | temiz=false, `NEGATIF_ACILIS`, jeton yok | **200** · `temiz=False jeton=None hatalar=[(1,'NEGATIF_ACILIS')]` + tam Türkçe mesaj |
| 5 | `POST /kurulum/acilis/prova` KASA `+250` | temiz=true, jeton var | **200** · `temiz=True jeton var` |
| 6 | `POST /evraklar` (vade bugün+120) | 201 | **201** · `S3F-CEK-1`, 1.250,50 ₺ |
| 7 | `GET /evraklar/takvim?bas=bugün&bit=+10` | aralık boş **ama** portföy dolu | **200** · `gunler=0 toplamAdet=0` · `portfoyAdet=1 portfoyTutar=1250.5 ilk=2027-01-16 son=2027-01-16 azamiGun=366` |
| 8 | `POST /siparisler/{id}/gecis ONAYLANDI` | 200, izinli listesinde `TASLAK` | **200** · `izinli=['ATOLYEDE','HAZIR','TASLAK','IPTAL']` · `gerekceIsteyen=['TASLAK','IPTAL']` · `duzenlenebilir=False` |
| 9 | `… gecis TASLAK` **gerekçesiz** | 400 | **400** · "Bu geçiş için gerekçe zorunlu (en az 10 karakter)" |
| 10 | `… gecis TASLAK` gerekçeli | 200, `duzenlenebilir=true` | **200** · `durum=TASLAK duzenlenebilir=True` |
| 11 | SQL denetim izi | 3 geçiş satırı, geri geçişte gerekçe | `NULL→TASLAK`, `TASLAK→ONAYLANDI`, `ONAYLANDI→TASLAK  "Musteri kalem sayisini degistirdi, taslaga ge…"` |
| 12 | `GET /pos/cihazlar` (tahsilatsız cihaz) | `tahsilatVar=false` | **200** · `('S3f POS', kasa1, False)` |
| 13 | `PATCH /pos/cihazlar/{id} {"kasaId": kasa2}` | 200, kasa değişti | **200** · liste: `kasaId=01M2SSJG84…` (kasa2) |
| 14 | `PATCH … {"kasaId":"01YOKBOYLEKASA…"}` | 404 | **404** · "Kasa bulunamadı" |
| 15 | `PATCH … {"ad":"S3f POS yeni ad"}` (kasaId yok) | 200, kasa **korunur** | **200** · `ad=S3f POS yeni ad kasaId=01M2SSJG84…` (değişmedi) |
| 16 | `POST /islemler/hurda-alis` | 200 + otomatik pusula | **200** · `GP-2026-000001` |
| 17 | `PATCH /gider-pusulalari/{id} {"saticiAdres":""}` | 400 | **400** · "Satıcı adresi boş bırakılamaz: … (VUK md.234) …" |
| 18 | `PATCH … {"malAciklamasi":null}` | 400 | **400** · "Mal açıklaması boş bırakılamaz: … (VUK md.234) …" |
| 19 | `PATCH … {"saticiAdres":"Yeni mah. 5"}` | 200 | **200** |
| 20 | SQL | adres yazıldı, mal açıklaması **silinmedi** | `GP-2026-000001  Yeni mah. 5  22 ayar hurda bilezik` |
| 21 | `GET /kilavuz.html` | 200, `localhost:5000`/`start.bat` = 0, 9 bölüm var | **200** (60.885 bayt) · sayımlar 0 · 9/9 bölüm `True` |

**Canlıda ÖLÇÜLEMEYENLER (dürüst kayıt):**
- POS cihazında **tahsilat varken** kasa değişiminin reddi 5198'de denenmedi: gerçek
  POS tahsilatı için kart bacaklı satış + ürün/stok kurulumu gerekiyordu. Bu yol
  `S3fSunucuTestleri.Tahsilati_OLAN_cihazda_TahsilatVar_TRUE_ve_kasa_degisimi_REDDEDILIR`
  ile **gerçek MySQL üzerinde** ve gerçek `PosDefteri` satırıyla kanıtlanmıştır.
- `/kullanicilar` 429 davranışı canlıda tetiklenmedi (oran sınırını kasten doldurmak
  kurucunun sunucusunu da etkileyebilirdi); bUnit ile ölçüldü.

**Temizlik:** API PID durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır**.
Test kiracısı `information_schema`'daki **tenant_id kolonu olan 58 tablodan** + `tenants`
+ `kiraci_davetleri`'nden silindi. `SELECT COUNT(*) FROM tenants` → **8** (başlangıç
değeri 8). Kalan kiracılar: Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası
Kuyumculuk, Demo Kuyumcu, Ramazan Deneme, **QA-TEST-1/2/3** (fikstürlere DOKUNULMADI).
Sır dosyaları (`.sk`, `.pw`, `.tok`) `printf '' >` ile boşaltıldı.

## 9. Değişen dosyalar (bu paket)

**API**
- `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisKaynaklari.cs`
- `dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisDurumMakinesi.cs`
- `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs`

**Core**
- `dotnet/Kuyumcu.Core/Sozlesmeler/EvrakSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR + EN, 14 yeni anahtar)

**Web**
- `dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor`
- `dotnet/Kuyumcu.Web/Pages/EvrakTakvimi.razor`
- `dotnet/Kuyumcu.Web/Pages/Evraklar.razor`
- `dotnet/Kuyumcu.Web/Pages/Kurulum.razor`
- `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor`
- `dotnet/Kuyumcu.Web/Pages/Yetkiler.razor`
- `dotnet/Kuyumcu.Web/Pages/Pos.razor`
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/PastaGrafik.razor`
- `dotnet/Kuyumcu.Web/Servisler/PosServisi.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/Kuyumcu.Web/wwwroot/kilavuz.html`

**Testler**
- `dotnet/Kuyumcu.Tests/S3fSunucuTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/S3fEkranTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/S3fVarlikTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/SiparisEkranTestleri.cs` · `SiparisDurumMakinesiTestleri.cs` ·
  `S3dHttpTestleri.cs` · `UX1C3EkranTestleri.cs` (sözleşme uyarlaması — § 5)

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş kaydı):** `/isletme` · `/gider-pusulalari` ·
`/evraklar/takvim` · `/evraklar` · `/kurulum` · `/kullanicilar` · `/yetkiler` · `/pos` ·
`/siparisler` (detay) · `/kilavuz`. Hiçbiri bu dalgada ikinci kez açılmadı.

**Not:** `git status` çalışma ağacının tamamını kirli gösteriyor (proje hiç
commit'lenmediği için). Yukarıdaki liste **bu paketin** dokunduğu dosyalardır.
Ayrıca kaynak ağacında **devralınan** `Kuyumcu.Api/Modules/Satis/SatisModulu.cs.mutorj`
`Pages/*.razor.mutorj` ve `Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` artıkları vardır; `denetim.sh` beyaz listesinde oldukları için
temiz sayılıyorlar, bu pakette **oluşturulmadılar ve silinmediler**.

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Her düzeltme için hatayı üreten test | ✅ | § 5 · 42 yeni test |
| Her düzeltme için mutasyon kanıtı | ✅ | § 6 · M1–M17, hepsi kırmızı |
| Test açığı varsa test güçlendirme | ✅ | M15 · M17 → 5 yeni sunucu testi |
| 5198 canlı doğrulama (HTTP + SQL) | ✅ (kısmi) | § 8 · 21 adım; 2 adım ölçülemedi, gerekçesi yazılı |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 7 |
| Tam süit 0 başarısız | ✅ | 2893/2893 |
| Derleme 0 uyarı 0 hata | ✅ | § 4 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 8 temizlik · tenants 8 → 8 |
| COMMIT YOK | ✅ | hiçbir git yazma komutu çalıştırılmadı |
| Yeni istemci record açılmadı | ✅ | Mevcut `PosCihazSatir`/`Kart`/`EvrakTakvimi`'ne alan eklendi |
| Tip sözleşmesi (ad + tip) | ✅ | `TahsilatVar` bool ↔ bool (`PosModulu.cs:134` ↔ `PosServisi.cs:24`); `PortfoyAdet/Tutar` decimal ↔ decimal (`EvrakSozlesmeleri.cs:72-73`); `GerekceIsteyenGecisler` `string[]` ↔ `List<string>` (`SiparisModulu.cs:319` ↔ `SiparisDetay.razor:294`) |
| PATCH semantiği korundu | ✅ | Telefon (§ 8/1-3, test), POS kasaId (§ 8/15, test), pusula (§ 8/20, S3dHttpTestleri) |
| Sozlukler TR + EN | ✅ | 14 anahtar × 2 dil |
| Yeni CSS'te sabit renk yok | ✅ | `S3fVarlikTestleri.app_css_…_SABIT_RENK_kullanmaz` |
| Boş durum + hata/başarı bandı | ✅ | `/kullanicilar` yüklenemedi ≠ boş durum; `/evraklar/takvim` iki ayrı boş durum |
| Kilitli düğme sebebini söyler | ✅ | `#evrak-takvim-genislet-kilit`, `#cekmece-gider-pusulasi-kilit`, `#pos-cihaz-kasa-kilit`, `#ikimlik-telefon-hata` |
| Ateşle-unut yan kanal beklenmiyor | ✅ | 429 testi yalnız düğmeyi ölçer; geri çekilme zamanlayıcısı test edilmez |
| Migration prosedürü | — | Gerekmedi (§ 3) |

## 11. Açık kalanlar

1. **`/kullanicilar` — tek SAHİP olan kiracıda kendi rolü değiştirilemiyor.**
   `devredildi: yeni paket (SAHİP devir akışı)`. Önerilen çözüm ("Yeni SAHİP ata ve
   devret") **yeni bir akış = yeni özellik**; kurucu talimatı 13 (kapsam kilidi)
   gereği bu pakette AÇILMADI. Bugünkü kilit metni hâlâ "başka bir SAHİP değiştirir"
   diyor ve tek sahipli kiracıda **uygulanamaz** — metin de düzeltilmedi, çünkü doğru
   metin ancak akış varken yazılabilir.
2. **`/raporlar` — pasta düzeltmesi tarayıcıda görsel olarak doğrulanmadı.**
   `getBBox()` ölçümü DOM gerektirir; bUnit path dizgesini ölçer. `devredildi: QA turu 6`
   (tarayıcı turunda yeniden denenmeli).
3. **POS kasa değişiminin tahsilatlı reddi 5198'de denenmedi** (§ 8).
4. **`/pos` cihazda tahsilat varken "yeni kasaya taşı" akışı açılmadı** —
   `devredildi: yeni paket`; bugünkü yol "pasife al + yeni cihaz" ve ekranda yazılı.
5. **`ATOLYEDE`/`HAZIR` geri dönüşü** açılmadı (§ 2/3).
6. QA turu 5'in ORTA/DÜŞÜK bulguları bu paketin kapsamında değildi; dokunulmadı.

## 12. Kurucuya / mali müşavire notlar

1. **VUK md.234 — zorunlu unsurlar.** Gider pusulasında satıcının adresi ve malın
   açıklaması artık **silinemiyor** (düzeltilebiliyor). Bu, belgenin zorunlu
   unsurlarının boş bırakılamayacağı okumasına dayanır. **Mali müşavir teyidi
   istenir:** mevcut kayıtlarda bu alanlar boşsa, ekran kullanıcıyı doldurmaya
   zorlayacaktır (Kaydet kilitli + sebep yazılı). Stopaj oranı ve 7 günlük düzenleme
   süresi bu pakette **ellenmedi**.
2. **Sipariş geri geçişi.** `ONAYLANDI → TASLAK` artık mümkün ve **gerekçesi denetim
   izine yazılıyor**. Sipariş defter hareketi doğurmadığı için bu geri alma mali bir
   kayıt üzerinde değildir. `ATOLYEDE`/`HAZIR` geri dönüşü, mal fiziken atölyeye
   çıkmış olabileceği için **kasten açılmadı** — kurucu kararı istenir.
3. **Kasa/banka açılışında negatif yasağı.** Eksi açılış bakiyesi artık reddediliyor;
   cari açılışında negatif serbest (müşteri alacaklı olabilir). Kredili mevduat hesabı
   kullanan bir dükkân varsa bu kural **onu kısıtlar** — kurucu kararı istenir
   (bugünkü davranış tutucu: reddet).
4. **Kılavuzdaki uygulama adresi.** Kılavuz artık sabit bir adres yazmıyor; kuruluma
   göre değişen adresin **kısayola gömülmesi** ve kurucu tarafından müşteriye yazılı
   verilmesi varsayıldı. Teslimat paketinde kısayol yoksa bu bölüm yanıltıcı olur.
