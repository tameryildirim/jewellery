# PAKET Q9-5 — Uygulama raporu

**Konu:** Kasalar'da şube düzenlenemiyordu ve eksi bakiyede uyarı yoktu · Sayım'da alan hatası yanlış alana düşüyordu · Carilerde telefonla arama 0 sonuç veriyordu (QA turu 9, YÜKSEK ×4)
**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 2026-09-24
**Ortam:** test veritabanı `kuyumcu_test_l` · canlı API 5174 · vekil 5173 · test kiracısı `PAKETQ9-5-TEST Kuyumcu` (`01M39DAF2S4D1V6VCT0V3459FF`; `/auth/kayit` ve tek kullanımlık davetle açıldı, iş sonunda silindi)
**Commit:** yapılmadı. Migration: **gerekmedi** (şema değişmedi).

---

## 1. Bulgular yeniden üretildi mi? (yazmadan önce ölçüm)

| Bulgu (QA 9) | Kendi ölçümüm | Sonuç |
|---|---|---|
| /kasalar: "Kasayı Düzenle"de yalnız ad alanı var | Kod: `d-doviz` ve `d-sube` `<ProAlan>` içindeydi, yani **basit modda çizilmiyordu**. Profesyonel modda alan vardı ama kilit sebebi ancak kaydettikten sonra 409 ile görünüyordu. Sunucu (`KasaServisi.Guncelle`) şube değişikliğini zaten destekliyor, geçmişi olan kasada 409 veriyordu. | Gerçek bulgu (ekran tarafı) |
| /kasalar: eksi bakiyede uyarı yok | Canlıda eksi kasa, meşru bir yoldan üretildi: giriş 1000, çıkış 1000, ardından girişin stornosu (storno kasa kapısından muaf, `KasaBakiyeKapisi` bu istisnayı gerekçesiyle yazıyor) → bakiye **−1.000,00**. Sayfada uyarı yoktu, özet kartı yeşildi. | Gerçek bulgu |
| /sayim: Ad dolu, Şube boşken hata Ad'a düşüyor | (a) `sy-sube` için `AlanHatasi` hiç çizilmiyordu, class ve aria da yoktu; hata kayda geçiyor ama ekranda görünmüyordu. (b) Headless Chrome'da `sy-ad` Input.insertText ile yazılıp change olayı gelmeden "Oturumu Aç"a basıldı → `sy-ad class="girdi hatali"`, "Bu alan zorunludur." **QA bulgusu aynen üretildi** (ilk ölçüm turu, `q95-ekran.out`). | Gerçek bulgu (iki neden) |
| /cariler: telefonla arama 0 sonuç | `CariServisi.Listele` yalnız `ad LIKE %q%` arıyordu. Telefon kullanıcının yazdığı biçimle saklanıyor (`0312 444 33 22` ya da `05329998877`). | Gerçek bulgu |

## 2. Yapılanlar

### 2.1 Kasa şubesi (sunucu + ekran)
- **Ölçüm (güvenli yolu seçmek için):** `kasa_hareketleri` tablosunda `sube_id` **YOK**. Şube raporları (`SatisRaporlari.cs:174-182`, `GiderOlcumu.cs:43-49`) hareketin şubesini **kasanın** şubesinden alıyor. Geçmişi olan kasanın şubesi değişirse o kasanın bütün eski hareketleri geriye dönük olarak başka şubenin raporuna geçer. **Karar:** geçmişi olan kasada kilit (sebebiyle), geçmişi boş kasada serbest değişiklik. Bu, sunucuda zaten var olan Mehmet K4 kuralıdır; ikinci bir kural yazılmadı.
- `KasaModulu.cs`: `GecmisiBosMu` kuralı `DegisimKilidiSebebi()` içine taşındı ve **tek kaynak** oldu (BAKIYE · HAREKET · GUN_KAPANISI kodları, `KasaDegisimKilidi` sabitleri). 409 mesajları birebir aynı kaldı.
- Yeni okuma ucu: **`GET /api/v1/kasalar/{id}/degisim-kilidi`** → `{kasaId, kilitli, sebep}`. Sınıfın `[ModulYetkisi("kasa")]` kapısı geçerli; öteki GET uçları gibi `[RolGerekli]` yok. Başka kiracının kasasına 404 döner.
- `Kasalar.razor` düzenleme çekmecesi: döviz ve şube `ProAlan` dışına alındı, **iki modda da görünüyor** (UX-S26: oluşturma formunda olan alan düzenlemede de olmalı; mod bir yetki değil). Çekmece açılınca kilit ucu okunuyor:
  - serbestse seçiciler açık, `d-doviz-kilit` kural notu görünüyor;
  - kilitliyse `d-doviz` ve `d-sube` **disabled**, altında `DegismezIpucu` (`#degismez-ipucu-kasa-sube`) sebebi ve **gerçekten var olan** düzeltme yolunu yazıyor: yeni kasa aç → para aktar (`/gider?sekme=virman`, `#d-kilit-virman-yol`; kasalar arası virman şubeler arasında da çalışıyor, `VirmanModulu.cs:171`) → bu kasayı pasife al;
  - kilit okunamazsa alanlar **kilitli kalıyor** ve "okunamadı" yazıyor (fail-closed; hata "serbest" gibi gösterilmiyor, UX-S28);
  - PATCH gövdesine kilitliyken `paraBirimi`/`subeId` konmuyor. Değişmeyen alanlar da gönderilmiyor (PATCH sözleşmesi).
- Mevcut `#d-sube` id'si korundu: tek şubede de seçici çiziliyor (`UX1C2EkranTestleri` bunu arıyor).

### 2.2 Eksi bakiye uyarısı (ekran)
- Özet kartı eksi olunca `kirmizi` şeritle ve "Eksi — düzeltme gerekir" alt metniyle çiziliyor.
- Sayfa düzeyinde `#kasa-eksi-ozet` uyarı bandı var: "{n} kasada eksi bakiye var. Nakit kasa eksiye düşemez; düzeltme gerekir." ve altında "Kasa raporunu aç" (`/raporlar/kasa`).
- Kartta `#kasa-eksi-{id}` var: kırmızı "Eksi bakiye" rozeti (P-50'deki `Gider.KasaBakiyeEksi` metni kullanıldı, aynı kavram için aynı ad), P-44/P-50 ile aynı dilde açıklama, tutar `metin-kirmizi`, ayrıca **"+ Kasaya giriş yap"** düğmesi. Bu düğme hareket çekmecesini GİRİŞ yönünde açıyor (P-50'deki `?yon=giris` yoluyla aynı).
- Ekranda jargon yok: "P-44", "storno" ve "defter" sözcükleri kullanılmadı.

### 2.3 Sayım formu (ekran)
- `sy-sube` için alan hatası ekleniyor: `AlanHatasi` (`#alan-hata-sy-sube`), `hatali` class'ı, `aria-invalid`, `aria-required`. Ad ve Şube etiketlerinde zorunlu işareti (`.zorunlu-isaret`) var. `sy-kapsamid` alanına da aynı eksik düzeltme uygulandı.
- `sy-ad` artık her tuşta da modele yazılıyor (`@bind` korundu, üstüne `@oninput` eklendi). Change olayı gelmeden gönderilen dolu ad artık "boş" sayılmıyor.

### 2.4 Cari araması (yalnız sunucu; `Cariler.razor`'a DOKUNULMADI)
- Yeni dosya `Modules/Cari/CariArama.cs`, içinde `TelefonCekirdegi(q)`: telefon biçimli aramayı rakamlara indiriyor, baştaki sıfırları ve ülke kodunu (90) atıyor. 3 rakamdan kısa çekirdekte telefon araması yapılmıyor; harf içeren arama telefon sayılmıyor.
- `CariServisi.Listele`: `ad LIKE` **veya** `email LIKE` **veya** (telefon biçimliyse) rakama indirgenmiş kayıtlı telefon `LIKE %çekirdek%`. Kayıt tarafında SQL `REPLACE` ile boşluk, `-`, `(`, `)`, `+` ve `.` atılıyor. Kiracı filtresi aynı kaldı. Arama kutusu zaten "Ad, telefon veya e-posta" diyordu; e-posta da bu yüzden eklendi.

### 2.5 Sözlük
- Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.KasaSubeKilit.cs`, TR ve EN, konumsal record (eksik anahtar derleme hatası verir). HUB'a (`Sozlukler.cs`) dokunulmadı.

## 3. Sapmalar ve gerekçeleri
1. **Tek şubede seçiciyi gizlemedim (UX-S22).** Mevcut `#d-sube` id'si bağlayıcı ("mevcut element id'leri DEĞİŞMEZ") ve `UX1C2EkranTestleri.Kasalar_duzenleme_CEKMECEDE…` bu id'yi arıyor. Seçici tek seçenekle çiziliyor; davranış değişmedi.
2. **Sayım `sy-ad` için `@bind:event="oninput"` KULLANILMADI.** Bu değişiklik `UX4cEkranTestleri.M11…` testini kırdı, çünkü o test `.Change` kullanıyor. Onun yerine `@bind` (change) ile ek `@oninput` birlikte kullanıldı; iki olay da modeli yazıyor.
3. **Eksi kasayı düzelten bir hareket yazılmadı.** Düzeltme kullanıcının kararıdır (sayım + giriş). Ekran bunun gerçek yolunu gösteriyor.
4. **Gözlem:** eksi bakiyeye meşru yoldan ulaşılabiliyor: girişin stornosu kasa kapısından muaf. `KasaBakiyeKapisi` bu istisnayı "defter bütünlüğü" gerekçesiyle bilinçli olarak yazmış. Bu yüzden uyarı yalnız eski kayıtlar için değil, bugün için de gerekli. Kural değiştirilmedi.

## 4. DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Salt okunur kısmı ve nedeni |
|---|---|---|
| Kasa (ad) | `PATCH /kasalar/{id}` (mevcut), çekmece `#cekmece-kasa` | — |
| Kasa (şube, para birimi) | Geçmişi boşsa aynı PATCH, iki modda da ekranda | Bakiye, hareket ya da gün kapanışı varsa kilitli. Neden: şube raporları hareketin şubesini kasadan türetiyor. `DegismezIpucu` gerçek yolu söylüyor (yeni kasa + virman + pasife alma). |
| Kasa (aktif/pasif) | Mevcut (DELETE, PATCH aktif) | — |
| Kilit bilgisi (`degisim-kilidi`) | Yeni **varlık değil**, türetilmiş salt okunur yanıt | Yazma yok → denetim kaydı gerekmiyor |
| Cari araması | Salt okunur; yeni varlık yok. Liste okuma izi (`ListeOkumaKaydet`) aynı kaldı | — |
| Sayım oturumu | Değişmedi (yalnız form doğrulaması) | — |

Yeni varlık yok, yeni yazma yolu yok, `HareketFabrikasi` ve `OdemeKalemFabrikasi` atlanmadı, finansal satır silinmedi. Şube değişikliği mevcut `DenetimServisi.YazmaKaydet` çağrısıyla izleniyor (canlı SQL § 7).

## 5. Testler
- Yeni dosya: `Kuyumcu.Tests/Q95KasaSayimCariTestleri.cs` — **32 test** (16 saf Theory satırı, 6 HTTP, 7 bUnit Kasalar, 4 bUnit Sayım; Kasalar Theory'si iki modda koşuyor).
- Filtreli koşum `FullyQualifiedName~Q95`: **32/32 geçti**.
- Etkilenen komşu testler (S36, UX1C2, UX4c, Sayim*, Cari*, KasaDuzenleme, GiderEkran, P19, ErisimKapatma, S3cForm, P22Ekran, *Kasa*): **640/640** geçti (ara koşum, t3). Sayım değişikliğinden sonra Q95 + UX4c + Sayım + S36 + UX1C2: **185/185** (t4).
- **Tam süit (1. koşum, son sayım değişikliğinden önce):** 5177 test, 5176 geçti, **1 başarısız**: `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir`. Bu test `wwwroot/css/app.css` içindeki S1-c bloğunda `overflow` buluyor. **app.css'e bu paket dokunmadı** (eşzamanlı S1-h/S1-i şeridinin dosyası). Bu kırmızı benim ürünüm değil.
- **Tam süit (2. koşum, son hâl):** 5180/5180 geçti (§ 10).
- Koşum sırasında başka bir şeridin yarım işi test projesini bir süre derlemedi (`S1iGorselSigma2Testleri.cs(410,22): error CS0103: 'Dil'`). Dosyaya dokunmadım; bir sonraki denemede derleme geçti.

## 6. Mutasyon kanıtları (12/12 kırmızı, hepsi geri alındı, `cmp` ile birebir aynı)
Yöntem: yedek `scratchpad/mut-yedek/Q9-5/`, tek eşleşmeli `perl -0777 -pi`, `dotnet test --filter FullyQualifiedName~Q95`, yedeğin geri kopyalanması ve `cmp`.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M95-1 | `CariArama`: +90 ülke kodunu atma satırı silindi | `Q95_telefon_cekirdegi_bicimden_bagimsizdir` (+90, 0090, 905… ×3) · `Q95_cari_TELEFONLA_bulunur…` | AYNI |
| M95-2 | `CariModulu`: telefon koşulu `false &&` yapıldı | `Q95_cari_TELEFONLA_bulunur_bosluk_sifir_ve_90_fark_etmez` | AYNI |
| M95-3 | `CariModulu`: kayıt tarafındaki `Replace(" ","")` silindi | `Q95_cari_TELEFONLA_bulunur…` | AYNI |
| M95-4 | `KasaModulu`: HAREKET kilidi `return null` yapıldı | `Q95_degisim_kilidi_BAKIYE_HAREKET_GUN_KAPANISI…` | AYNI |
| M95-5 | `Kasalar.razor`: `d-sube Devredisi=@(false)` | `Q95_HAREKETLI_kasada_sube_KILITLI…` · `Q95_TEK_subede_kilitli…` · `Q95_kilit_OKUNAMAZSA…` | AYNI |
| M95-6 | `Kasalar.razor`: okuma hatasında kilit `null` (serbest) | `Q95_kilit_OKUNAMAZSA_alanlar_KILITLI_kalir…` | AYNI |
| M95-7 | `Kasalar.razor`: eksi özet bandı `> 1` koşuluna bağlandı | `Q95_EKSI_kasada_rozet_uyari…` | AYNI |
| M95-8 | `Kasalar.razor`: özet kartı her zaman `yesil` | `Q95_EKSI_kasada_rozet_uyari…` (ilk deneme başka şeridin eşzamanlı derlemesiyle dosya kilidine takıldı, `MSB4018`; tekrarında kırmızı) | AYNI |
| M95-9 | `Sayim.razor`: `AlanHatasi sy-sube` silindi | `Q95_ad_DOLU_sube_BOS_hata_SUBENIN_altinda…` | AYNI |
| M95-10 | `Kasalar.razor`: şube seçicisi yeniden `<ProAlan>` içine alındı | `Q95_BASIT_ve_PRO_modda…(BASIT)` · `Q95_HAREKETLI…` · `Q95_TEK_subede…` · `Q95_kilit_OKUNAMAZSA…` | AYNI |
| M95-11 | `Sayim.razor`: `sy-sube` hata class'ı silindi | `Q95_ad_DOLU_sube_BOS…` | AYNI |
| M95-12 | `Sayim.razor`: `@oninput` silindi | `Q95_ad_YALNIZ_yazildi_change_gelmeden_gonderildi_hata_ADA_dusmez` | AYNI |

Test edilemeyen savunma: `KasaGuncelle` içindeki `!DegisimKilitli` gövde kapısı. Seçici disabled olduğu için bUnit'te değer değiştirilemiyor, bu yüzden bu kapıyı kıran mutasyon hayatta kalırdı. Sunucu aynı durumda zaten 409 dönüyor (`KasaDuzenlemeTestleri` ve M95-4). Bu satırı mutasyon tablosuna koymadım.

Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 hata** (son derleme 0 uyarı; ilk tam derlemedeki tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031, başka dosya).

## 7. Canlı doğrulama (API 5174, `kuyumcu_net`, kendi kiracım)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /cari?q=0312 444 33 22` | Ayşe Telefonlu | 200 `[('Ayşe Telefonlu','0312 444 33 22')]` |
| `GET /cari?q=03124443322` | aynı | 200, aynı tek satır |
| `GET /cari?q=+90 312 444 33 22` / `q=4443322` | aynı | 200, aynı tek satır (ikisi de) |
| `GET /cari?q=05329998877` / `0532 999 88 77` | Mehmet Bitişik | 200, tek satır (ikisi de) |
| `GET /cari?q=Ayşe` (adla) | bozulmadı | 200 Ayşe |
| `GET /cari?q=04449999999` | boş | 200 `[]` |
| `GET /kasalar/{AnaKasa}/degisim-kilidi` (−1000) | kilitli BAKIYE | 200 `{"kilitli":true,"sebep":"BAKIYE"}` |
| `GET /kasalar/{YanlisSube}/degisim-kilidi` | serbest | 200 `{"kilitli":false,"sebep":null}` |
| `PATCH /kasalar/{AnaKasa} {subeId: Çarşı}` | 409 | 409 "Bakiyesi sıfır olmayan kasanın para birimi ya da şubesi değiştirilemez" |
| **Ekrandan** (basit mod, 1280): Yanlış Şube Kasası → Düzenle → Şube = Çarşı Şube → Kaydet | başarı + SQL | bant "Kasa bilgileri kaydedildi.", çekmece kapandı |

SQL: `kasalar` → `Yanlış Şube Kasası | sube = Çarşı Şube | 0.00`, `Ana Kasa | Merkez Şube | -1000.00`.
`denetim_kayitlari` (varlik_turu=Kasa): `EKLEME 01M39DB1KSX1W8GT22C4DCBD0G "kasa güncellendi · değişen alanlar: sube_id"` ile `GUNCELLEME … "Guncellendi, SubeId"` (2026-09-24 10:00:29).

## 8. Gerçek tarayıcı ölçümü (kural 2026-09-24)
Araç: `betikler/sikisma-olc.js`, headless Chrome + CDP ile (`scratchpad/q95-ekran.mjs`, vekil 5173 üzerinden, parola yazılmadan `/__giris`). Her ekran 1280×800 ve 375×812'de, BASİT ve PROFESYONEL modda, açık ve koyu temada ölçüldü (8 kombinasyon). Tarayıcı bölmesinde kendi sekmemde (tab-36) ayrıca şunlar ölçüldü: /kasalar 1280 PRO ve BASIT'te `__olc` (hepsi 0), kilitli çekmece ekran görüntüsü, 375 koyu /sayim'de gerçek klavye ve fareyle form (hata Şube'nin altında, Ad işaretsiz) ve 375 /cariler'de gerçek klavyeyle `03124443322` ile `0312 444 33 22` (ikisi de Ayşe'yi buldu).

| Rota | Genişlik × mod × tema (8) | K1 gizli | K3 taşan/kırık | K5 TRY | K6 seçici | K7 hiza | K8 jargon | K9 binen | K10 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|
| /kasalar (liste, eksi uyarı) | hepsi | 0 | 0/0 | 0 | 0 | 0 | 0 | **0** (ilk turda 375'te 1: bantta satır içi bağlantı; bağlantı ayrı satıra alındı, 2. turda 0) | 0 | 0 |
| /kasalar [düzenle-kilitli] | hepsi | 0 | 0/0 | 0 | 0/2 | 0 | 0 | 5–6 † | 0 | 0 |
| /kasalar [düzenle-serbest] | hepsi | 0 | 0/0 | 0 | 0/2 | 0 | 0 | 4–6 † | 0 | 0 |
| /sayim [yeni form, şube boş] | hepsi | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /cariler [q=0312 444 33 22] + [q=03124443322] | hepsi + 1 | 0 | 0/0 | 0 | 0/1 | 0 | BASIT'te 2 ‡ | 0 | 0 | 0 |

† **K9 gerekçesi (ölçüldü):** çakışan öğeler çekmecenin ARKASINDA kalan sayfa başlığı (`#sayfa-basi-kasalar-baslik`) ile çekmece içindeki etiketler. Çekmece sabit bir örtü olduğu için geometri üst üste biniyor, görsel binme yok. Kanıt: bu paketin dokunmadığı "Yeni Kasa" çekmecesi aynı ölçümde **K9=4** veriyor (tab-36). Ölçüm aracının örtü katmanını ayırmaması kaynaklı; her çekmecede görülüyor.
‡ **K8:** "Cariler/Cari" başlığı `Cariler.razor`'da. O dosya eşzamanlı S1 şeridinin ve bu pakette DOKUNMA listesinde. Bulgu önceden vardı, benim değişikliğim değil.

Kanıt değerleri (CDP): kasalar `statSinif "kart stat-kart kirmizi"`, `bakiye "-₺1.000,00"`, özet "1 kasada eksi bakiye var…" · kilitli çekmece `dsubeKapali true, ddovizKapali true, yol /gider?sekme=virman` · serbest çekmece `dsubeKapali false, ipucu false` · sayım (düzeltmeden sonra) `syAdHata null, subeHata "Bu alan zorunludur.", subeSinif "secim hatali", aria-invalid true, isaret 2` · cari aramasının 9 koşumunda da `Ayşe Telefonlu 0312 444 33 22`.
Ekran görüntüleri: `dokumanlar/09-uygulama-kayitlari/ekran/Q9-5/*.png` (42 dosya, her koşum için bir tane; son turda liste, sayım ve cari görüntüleri yenilendi).

## 9. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ** (288 ✓, 0 ✗). İlgili sayaçlar: ModalPencere 48 (tavan 55, artış yok; yeni düzenleme Cekmece'de), satır içi style 105, SABIT_RENK 0, IgnoreQueryFilters 0, ExecuteUpdate/Delete beyaz liste 5, `Hareketler.Add` 1 (fabrika).

## 10. Tam süit (son hâl) ve paralel şerit kapısı (7. madde)
`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj -v n --logger trx --results-directory scratchpad/trx-pQ9-5-son` (`KUYUMCU_TEST_DB=kuyumcu_test_l`), bütün değişikliklerden ve son mutasyondan sonra: **Toplam 5180 · Geçti 5180 · Başarısız 0** (rc=0). 1. koşumdaki `S1cUstBar…` kırmızısı bu arada sahibinin şeridinde kapanmış; bu paket o dosyaya dokunmadı.
**Paralel şerit kapısının 7. maddesi:** bu paketin ürettiği yeni kırmızı **0**.
Son `denetim.sh`: **DENETİM TEMİZ**.

## 11. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs` (M)
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` (M; yalnız `Listele` arama koşulu)
- `dotnet/Kuyumcu.Api/Modules/Cari/CariArama.cs` (yeni)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KasaSubeKilit.cs` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor` (M)
- `dotnet/Kuyumcu.Web/Pages/Sayim.razor` (M; dosya git'te izlenmiyor)
- `dotnet/Kuyumcu.Tests/Q95KasaSayimCariTestleri.cs` (yeni)
- `dokumanlar/09-uygulama-kayitlari/ekran/Q9-5/*.png`, bu rapor

`git status` aynı dosyalarda başka şeritlerin önceki değişikliklerini de gösteriyor (örneğin `Cariler.razor` M). `Cariler.razor`, `app.css`, `Sozlukler.cs` ve Contracts'a **bu paket dokunmadı**.

**Dokunulan sayfalar (tek dokunuş):** `Pages/Kasalar.razor`, `Pages/Sayim.razor`.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Kasanın şubesi değişir ya da gerekçeli kilit | Karşılandı | Ekrandan değişiklik + SQL + denetim kaydı; kilitli kasada DegismezIpucu (§ 7-8) |
| Kasa hareketlerinin şube raporuna etkisi ölçüldü, güvenli olan seçildi | Karşılandı | `kasa_hareketleri`'nde sube_id yok; raporlar kasadan türetiyor (§ 2.1) |
| Eksi kasada satırda ve özette uyarı | Karşılandı | `#kasa-eksi-ozet`, `#kasa-eksi-{id}`, kırmızı özet, 8 kombinasyon |
| Sayımda hata doğru alanda, zorunlu işaretli | Karşılandı | 8 kombinasyon + bölmede gerçek klavye |
| '0312 444 33 22' ve '03124443322' ile cari bulunur | Karşılandı | HTTP + 9 tarayıcı koşumu + bölme |
| Cariler.razor'a dokunulmadı | Karşılandı | yalnız sunucu ve servis |
| Test + mutasyon | Karşılandı | 32 test, 12/12 mutasyon |
| Özellik silinmedi, DOM id ve rota değişmedi | Karşılandı | `#d-sube`, `#d-doviz`, `#d-doviz-kilit` korundu; yeni id'ler önekli |
| Tek yazma yolu atlanmadı; mevzuat kilidi açılmadı | Karşılandı | yazma yolu yok; mevzuat konusu yok |
| app.css'e ve Sozlukler HUB'ına dokunulmadı | Karşılandı | yeni metinler `Sozlukler.KasaSubeKilit.cs` içinde |
| Test kiracısı silindi, tenants başlangıç sayısına döndü | Karşılandı | 70 tablo + tenants + davet; kalan 0; **tenants 11 → 11** |

## 13. Açık kalanlar
- (Kapandı) 1. tam süitteki `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL…` kırmızısı (app.css, S1-h/S1-i şeridi) 2. koşumda yeşildi.
- `/cariler` basit modda "Cari/Cariler" jargonu (K8=2). **Sahibi: Cariler.razor, S1 şeridi.**
- Ölçüm aracındaki K9, çekmece örtüsünü arka plandaki sayfayla çakışma sayıyor (bütün çekmecelerde yanlış alarm). **Sahibi: ölçüm betiği (orkestratör).**
- Storno ile eksi bakiyeye inilebiliyor (gerekçeli istisna). Eksi durum artık görünür, ama düzeltme fişi gibi bir akış yok. **Karar: ürün / mali müşavir.**
- Bu pakette mevzuat hükmü yok; mali müşavire not gerekmiyor.

## 14. Kapanış
API (5174), vekil (5173), ölçüm sunucusu (5187) ve headless Chrome (9341) durduruldu; dördünde de `lsof` boş. `fikstur.json` ve `kayit.json` boşaltıldı (0 bayt). 5199 portuna dokunulmadı; `QA-TEST-` kiracılarına dokunulmadı.
