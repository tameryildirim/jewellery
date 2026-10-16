# PAKET P-36b — GİRDİ BİLEŞEN AİLESİ + GÖRÜNÜM AYARI
## Kuyumcu ERP · Dalga 6 · 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü

> ## ⚠ BU BELGE TARİHSELDİR — GÜNCEL DURUM: **BİTTİ**
>
> Aşağıdaki "YARIM" beyanı **uygulama turunun (12:29) hâlidir ve DOĞRUYDU.**
> Kapanış turu (2026-09-21, 12:35–12:50) DoD'nin açık üç maddesini kapattı:
>
> | DoD | Bu belgede | KAPANIŞ turundan sonra |
> |---|---|---|
> | (c) 375/768/1440 yatay kaydırma | YAPILMADI | **6/6 ölçüm, taşma 0** |
> | (d) `dotnet/denetim.sh` | 4 ihlal | **TEMİZ (EXIT=0)** |
> | (e) Tam süit | 27 kırmızı | **0 kırmızı / 3608** |
>
> Kanıtlar, mutasyonlar ve canlı tur: **`PAKET-P-36b-KAPANIS-raporu.md`**.
> Bu dosya kayıt bütünlüğü için DEĞİŞTİRİLMEDİ (yalnız bu kutu eklendi).

> **DURUM (uygulama turu, 12:29): YARIM.**
> **Paketin KAPSAMI %100 uygulandı** (üç YAPILACAK kalemi de bitti, 51 yeni test,
> 12 mutasyon kanıtı, canlı doğrulama 5198'de yapıldı, test kiracısı silindi).
> **DoD'nin iki maddesi karşılanmadı ve bunun sebebi bu paket DEĞİLDİR:** ağaçta
> paket başlamadan ÖNCE var olan **27 kırmızı test** ve **4 denetim ihlali**
> duruyor; hepsi P-37 şeridinin (menü tercihleri / kenar çubuğu) yarım işinden
> geliyor. Anayasa md.1 gereği bunlar ne gizlendi ne de başkasının defterine
> yazılarak "temizlendi"; § 9'da adıyla, kanıtıyla ve sahibiyle yazılıdır.
> Uygulayıcı BITTI demedikçe paket geçmez kuralı gereği beyan **YARIM**dır.

---

## § 0. ÖNCE BULUNAN: DEVRALMA NOTU GERÇEKLE UYUŞMUYOR (md.1)

`projeler/jewellery/CLAUDE.md` şöyle diyor:

> Durum (2026-09-21 05:15 kapısı, orkestratör kapısı): **3371/3371 test yeşil ·
> dotnet/denetim.sh TEMİZ** · 56 migration · 0 uyarı 0 hata.

**Ölçüm bunu doğrulamadı.** Paket başlamadan yapılan ilk koşumda:

| Ölçü | Devralma notu | GERÇEK (2026-09-21, paket öncesi) |
|---|---|---|
| Test sayısı | 3371 | **3557** (P-36 raporundaki sayı) |
| Kırmızı test | 0 | **27** |
| `dotnet/denetim.sh` | TEMİZ | **BAŞARISIZ (4 ihlal)** |
| Migration | 56 | **61** |

Sebep § 9'da. Bu bulgu paketi durdurmadı (hiçbiri P-36b'nin dokunduğu yola
değmiyor), ama **kapı kaydına yazılması zorunludur.**

---

## § 1. TABAN — BELGEDEN KOPYALANMADI, YENİDEN ÖLÇÜLDÜ

| Sayaç | Paket ÖNCESİ | Paket SONRASI | Ölçüm komutu (denetim.sh'a yorum olarak yazıldı) |
|---|--:|--:|---|
| HAM_SELECT | 176 | 176 | `grep -rho '<select[ >]' Pages Bilesenler Layout --include='*.razor' --exclude='Secici.razor' \| wc -l` |
| **HAM_INPUT** | **365** | **357** | aynı desen `<(input\|textarea)`, `--exclude-dir=Form` |
| **GIRDI_AILESI_DISI_BICIM** | **8** | **0** | `grep -rhoE 'ToString\("(C2\|N2\|N3\|F2\|F3\|F4)"' … \| wc -l` |
| GIRDI_AILESI_TAM | 0 | **12/12** | `Bilesenler/Form/<Ad>.razor` varlık sayımı |
| GIRDI_AILESI_TEK_KAYNAK | — | **1** (satır içi renk/px 0) | `grep -rlE 'style="[^"]*(#\|px\|rgb)' Bilesenler/Form \| wc -l` → 0 |
| TEMA_KONTRAST_IHLALI | — | **0** (2 şema, ikisi de ÖLÇÜLÜYOR) | § 4'teki üç koşul |
| SECICI_UZAK_KIP | 0 | **1** (KayitSecici kullanımı 4, tavan 4) | `grep -c 'UzakMi' Secici.razor` · `grep -rl KayitSecici Pages \| wc -l` |

> ⚠ **HAM_INPUT tavanı 365 → 357'ye DÜŞTÜ, ARTIRILMADI.** `Bilesenler/Form/`
> ölçümün dışındadır; gerekçe Secici.razor'un dışlanmasıyla aynıdır: ortak
> ailenin KENDİ `<input>`u sayaca girseydi, aile büyüdükçe "ham girdi arttı"
> görünür ve sayaç yalan söylerdi. Bu istisna denetim.sh yorumunda yazılıdır.

---

## § 2. YAPILACAK 1 — GİRDİ AİLESİ (kurucu talimatı 20/a) ✔

`dotnet/Kuyumcu.Web/Bilesenler/Form/` altında **12 bileşen + 1 kabuk + 2 taban**:

| Dosya | İş |
|---|---|
| `FormAlani.cs` | Ortak parametre sözleşmesi + TEK sayı/tarih çözücüsü |
| `SayisalAlan.cs` | Sayi · Para · Gram'ın ortak beyni (ayrıştırma, iç hata, önizleme) |
| `AlanKabugu.razor` | **TEK görsel kabuk**: etiket · zorunluluk · kaynak rozeti · ipucu · kilit sebebi · alan altı hata |
| `Metin` `Sayi` `Para` `Gram` `Tarih` `TarihAralik` `Telefon` `Eposta` `CokSatir` `Onay` `Secim` `Anahtar` | 12 üye |

**Sözleşme `<Secici>`nin (P-36) BİREBİR aynısıdır** — ikinci bir parametre dili
açılmadı: `ElemanId · Etiket · Deger/DegerChanged · Zorunlu · Devredisi +
DevredisiSebebi · Hata · Ipucu · Yukleniyor · YerTutucu · KaynakRozeti`.
Türetilen kimlikler: `<ElemanId>-sar · -ipucu · -durum · -hata · -onizleme`
(+ `-bas/-bit`, `-secenek-<n>`, `-sayac`, `-kaynak`, `-tek`, `-birim`).

Karşılanan UX sözleşmesi maddeleri:

| Madde | Nerede | Kanıt |
|---|---|---|
| UX-S21 (değerin kaynağı rozetle söylenir) | `KaynakRozeti` | `G5` |
| UX-S22 (tek seçenek → seçici gizlenir, değer otomatik konur) | `Secim` | `G16` + mutasyon `M36b-3` |
| UX-S29 (sunucu sınırı ekranda) | `CokSatir` sayacı, `EnFazla` | `G13` |
| UX-S30 (kilitli alan sebebini söyler) | kabuk | `G4`, `G18`, `G19` |
| UX-S31 (ham kod basılmaz, tek sözlük) | `Sozlukler.Form` TR+EN | derleme (required alan) |
| UX-S32 (para birimi tutarla taşınır) | `Para` | `G8` |
| DoD EK md.1 (her düğme ya iş yapar ya kilitli+sebep) | `Anahtar` | `G18` + mutasyon `M36b-12` |
| DoD EK md.3 (sessiz etkisizlik yasak) | `SayisalAlan.icHata` | `G7` + mutasyon `M36b-2` |
| ≥44px dokunma hedefi | `app.css` P-36b bloğu | `min-height: var(--dokunma-hedefi)` |

**Anayasa md.1'in ekrana yansıması:** kullanıcı sayı alanına "abc" yazarsa alan
**0'a düşmez**; değer değişmez, kutu kırmızı olur ve altına *"Sayı anlaşılmadı.
Örnek: 12,5 ya da 12.5"* yazılır. Bilinmeyen tahmin edilmez.

**Biçimlendirme tek yerden:** ailede `ToString("N2")` YOKTUR; para/gram/miktar
önizlemesi `Servisler/Bicim.cs` üzerinden basılır. Ailenin dışında kalan 8 ham
biçimlendirme de bu pakette Bicim'e taşındı (§ 5).

### Pilot göç (2 ekran, 8 ham girdi)

| Ekran | Alanlar | id değişti mi |
|---|---|---|
| `/ayarlar` profil çekmecesi | `pr-ad` → `<Metin>` · `pr-soyad` → `<Metin>` · `pr-eposta` → `<Eposta>` | **HAYIR** (id'ler birebir) |
| `/isletme` kimlik çekmecesi | `ikimlik-ad` · `-adres` · `-vergidairesi` · `-vergino` → `<Metin>` · `-telefon` → `<Telefon>` | **HAYIR** |

Hata kimlikleri de korundu (`#pr-eposta-hata`, `#ikimlik-vergino-hata` …), ama
artık **ailenin sözleşmesinden doğuyor** — sayfada ikinci bir hata çizim yolu
kalmadı. Sayfa mantığı (ProfilGecerli, KimlikKaydet, VergiNoHatali) **tek satır
bile değişmedi**; `string` ↔ `string?` köprüsü sayfada dar bir özellikle kuruldu.

> **Test sözleşmesi değişti (bilinçli, gizlenmedi):** aile `@oninput` kullanır
> (hata yazarken çıkar), eski ham girdiler `@bind`in varsayılanı olan `onchange`
> ile çalışıyordu. Göç eden 6 alanın 9 test çağrısı `Change(...)` → `Input(...)`
> olarak güncellendi (`UX1C4EkranTestleri`). Beklenti değişmedi, olay değişti.

---

## § 3. YAPILACAK 2 — SECİCİ UZAK KİPİ ✔

P-36'da `UzakTur` verilince bileşen **istisna atıyordu**. P-36b'de kip uygulandı;
mekanizma `KayitSecici`den (P-15) **taşındı, yeniden yazılmadı**:

* sunucuda arama `/arama?q=…&tur=…&limit=10` (+ kabuk şube kapsamı)
* asgari **2 karakter** (`Secici.AsgariSorgu` = `KayitSecici.AramaServisiAsgari`)
* **250 ms** gecikme (`VarsayilanBeklemeMs`)
* **yarış koşulu koruması**: geç gelen yanıt erken yanıtın üstüne yazılmaz
* **hata ≠ boş durum** (UX-S28) · **sınır uyarısı** (UX-S29) · **ULID basılmaz** (B-25)
* kapanınca bayat sonuç listesi tutulmaz

**Sessiz kip seçimi hâlâ yasak:** `UzakTur` + `Secenekler` birlikte verilirse
çalışma zamanı istisnası atılır (`S36_14` güncellendi).

**ApiIstemci kurucuda istenmedi** (ArayuzModuServisi deseni): Yerel kipi çizen
onlarca bUnit sınıfı sahte API kurmak zorunda kalmasın diye kapsamdan o an
çözülür; çözülemezse **sessizce boş liste değil, hata bandı** gösterilir.

### `KayitSecici` kararı — ÖLÇÜLDÜ, gerekçesi yazıldı

| Seçenek | Ölçüm | Karar |
|---|---|---|
| **Şimdi göç et** (4 kullanım: `PosTahsilatlar:159`, `Uyum:602`, `Hurda:195`, 🔒`Faturalar:492`) | 4 sayfa açılır; biri (`Faturalar`) **kilitli listede**, ikisi bu dalgada hiç açılmadı → 4 ayrı "tek dokunuş" gerekçesi gerekir | **HAYIR** |
| **Sarmala** (KayitSecici içini Secici'ye çevir) | Görünümü farklı (sürekli açık arama kutusu, açılır liste değil); sarmalamak iki farklı görünümü tek bileşene sıkıştırır — kurucunun "hepsi aynı olsun" şartını bozar | **HAYIR** |
| **Uzak kipi Secici'ye ekle, göçü P-36c'ye bırak, kullanımı CIRCIRLA** | Bugün 4; sayaç `KAYIT_SECICI_KULLANIM ≤ 4` kilitlendi → **beşinci kullanım açılamaz**, iki bileşen yan yana büyüyemez | **EVET** |

---

## § 4. YAPILACAK 3 — /ayarlar GÖRÜNÜM BÖLÜMÜ (talimat 16) ✔

**Kilit:** `/ayarlar` KİLİTLİ listedeydi (P-31 oraya yazacaktı). **P-31 hiç
başlamadı**, kilit YALNIZ yeni "Görünüm" bölümü için açıldı, mevcut bölümlere
dokunulmadı; `ham/TEK-EL-KILIT` defterine yazıldı (açma + kapama kaydı).

### 4.1 Nerede saklanıyor — ve neden migration GEREKMEDİ

Görev metni *"görünüm tercihi için **ŞARTLI** kolon"* dedi. **Şart oluşmadı:**

* `kiraci_ayarlari` (`KiraciAyari`) tablosu kiracı düzeyinde anahtar/değer
  saklamak için **zaten var**, `IKiraciVarlik`, `(tenant_id, anahtar)` üzerinde
  **benzersiz indeksli**, global kiracı filtresi + `KiraciYazmaDenetcisi` +
  denetim izi kendiliğinden işliyor.
* `Tenant` **IKiraciVarlik DEĞİLDİR**; oraya konacak kolon bu üç mekanizmanın
  **dışında** kalırdı — `KiraciAyari`nin kendi XML belgesi bunu açıkça söylüyor.

→ **Migration üretilmedi.** `__EFMigrationsHistory` **61 → 61** (değişmedi).
Yeni anahtarlar: `gorunum_semasi`, `gorunum_yazi_olcegi` (`KiraciAyarAnahtari`).

### 4.2 Uç sözleşmesi

| Uç | Kapı | Davranış |
|---|---|---|
| `GET /api/v1/gorunum` | `[HerkeseAcik]` | Her kullanıcı kendi işletmesinin görünümünü okur (okuyamazsa ekran varsayılana düşer ve çizemez olurdu) |
| `PATCH /api/v1/gorunum` | **`[RolGerekli("SAHIP")]`** | *"renk ve yazı tipi benim işim"* — tezgâhtar dükkânın rengini değiştiremez |

PATCH sözleşmesi: **gövdede yoksa KORU, `null` ise VARSAYILANA DÖN** (ayar satırı
silinir), boş gövde **400**. "Alan yok" ile "alan null" ayrımı DTO'da gönderilme
bayrağıyla tutuluyor — tam gövdeyle üzerine yazıp öteki alanı imha etmek yok.
Beyaz liste **servis içinde ELLE** denetleniyor (DataAnnotations yalnız
controller yolunda çalışır). `DenetimServisi.YazmaKaydet` **çağrılıyor**.

### 4.3 Renk şemaları — oranlar HESAPLANDI, tahmin edilmedi

`belirtecler.css`'e iki şema eklendi (kaynak: `FRONTEND-SARTNAME § 6.1` B ve C).
`TasarimBelirtecTestleri.Kontrast_AA_saglanir` artık `:root[data-sema="…"]`
bloklarını da **aydınlık temanın üstüne bindirerek** ölçüyor:

| Şema | En DÜŞÜK metin oranı | En düşük kenar oranı | AA (4,5 / 3,0) |
|---|--:|--:|---|
| **ALTIN** (varsayılan, mevcut) | 5,02:1 (`--vurgu`/`--yuzey`) | 3,16:1 (`--kenarlik`) | **geçer** |
| **KURUMSAL MAVİ** | 6,70:1 (`--vurgu`) · 6,90:1 (`--metin-soluk`/`--yuzey-3`) | 4,83:1 | **geçer** |
| **ZÜMRÜT** | 6,46:1 (`--metin-soluk`/`--yuzey-3`) · 7,68:1 (`--vurgu`) | 4,83:1 | **geçer** |

> **Eşiği geçmeyen şema listeye konmadı.** `FRONTEND-SARTNAME § 6.1`'deki D
> "Gece Altını" bir şema değil, **karanlık temanın mevcut paletidir**; ayrı
> seçenek olarak açılmadı (aşağıdaki sınıra bakın).

**Bilinçli sınır (eksik iş değil):** renk şeması **aydınlık temanın** şemasıdır.
Şema blokları CSS'te karanlık tema bloklarından **ÖNCE** durur; özgüllükleri
eşit olduğu için karanlıkta ölçülmüş "Gece Altını" paleti kazanır. Blok aşağı
taşınırsa karanlık temada koyu zemin üstüne koyu metin biner — bu uyarı CSS'in
kendi başlığına yazıldı.

### 4.4 Yazı tipi ölçeği

| Ölçek | Gövde | Durum |
|---|---|---|
| **yogun** (Ö1) | 14px | **VARSAYILAN** — mevcut kiracının ekranı bu paketten sonra HİÇ değişmez |
| **okunakli** (Ö2) | 16px | seçilerek açılır |
| ~~Ö3 "veri yoğun"~~ | 13px | **AÇILMADI** — ≥44px dokunma hedefi kuralıyla çelişir (Ertuğ'un gerekçesi) |

Ölçek blokları **yalnız ölçü belirteci** değiştirir; tek bir renk belirteci
tanımlamaz (yoğunluk bloklarının kuralının aynısı) — kontrast sözleşmesi sabit.

### 4.5 Anında uygulama

`js/tema.js` içine **üçüncü ad alanı** `kuyumcuGorunum` eklendi (yeni `<script>`
etiketi EKLENMEDİ — index.html betik sayısı sayaca bağlı). Beyaz liste HEM
sunucuda HEM tarayıcıda uygulanır (iki taraf da güvenmez — Mehmet K1); beyaz
liste dışı değerde öznitelik **kaldırılır**, boş dizge yazılmaz.

Ekran tarafı: bölüm iki `<Secim>` taşır (aile kendi kendini kullanıyor),
seçim **anında PATCH atar**, sunucunun döndürdüğü görünüm uygulanır ve başarı
bandı yazılır (UX-S27). SAHİP değilse ya da ayar sunucudan okunamadıysa alanlar
**kilitli** ve **sebebi yazılı** (UX-S30); ekran sunucu kapısını taklit etmez.

---

## § 5. BİÇİMLENDİRME TEK KAYNAĞA TAŞINDI (GIRDI_AILESI_DISI_BICIM 8 → 0)

| Yer | Önce | Sonra |
|---|---|---|
| `PusulaBelgesi.razor:126,127` | `ToString("N3", Kultur)` | `Bicim.BelgeSayisi(…, Dil.Locale, 3)` |
| `PusulaBelgesi.razor:196` | `ToString("N2", Kultur) + " ₺"` | `Bicim.BelgeParasi(…, Dil.Locale)` |
| `GiderPusulalari.razor:105,169,269` | aynı desen | aynı çözüm |
| `Uyum.razor:755` | aynı desen | aynı çözüm |
| `Panel.razor:143` | `@k.Bakiye.ToString("N2")` + ayrı `<span>@k.ParaBirimi</span>` | `@Bicim.Para(k.Bakiye, k.ParaBirimi)` |

`Bicim.BelgeSayisi` / `BelgeParasi` **çıktıyı birebir korur** (kâğıt belgede
sembol sonda durur, ekranda başta) — bu bir yeniden biçimlendirme değil, tek
kaynağa taşımadır. `Panel.razor:143` ise gerçek bir **UX-S32 ihlaliydi**: tutar
"varsayılan TL" sayılıp para birimi ayrı bir etiket olarak basılıyordu.

> **Kapsam dürüstçe dar:** `N0` ile basılan **adet** sayaçları (`KarZarar` 4,
> `DenetimIzi` 2) para/gram DEĞİLDİR ve bu sayaca girmez.

---

## § 6. TESTLER

| Küme | Yeni | Not |
|---|--:|---|
| `GirdiAilesiBilesenTestleri` (bUnit) | **23** | 12 bileşen + kabuk + aile tamlığı + tek kaynak |
| `SeciciUzakKipTestleri` (bUnit) | **10** | uzak kip sözleşmesi |
| `GorunumHttpTestleri` (gerçek HTTP) | **10** | kapı · PATCH sözleşmesi · denetim · kiracı sızıntısı |
| `GorunumEkranTestleri` (bUnit) | **8** | /ayarlar Görünüm bölümü |
| **Toplam yeni** | **51** | |
| Güncellenen | 1 | `S36_14` (uzak kip artık var; ikili kaynak hâlâ istisna) |

**Tam süit:** `Toplam 3608 · Başarılı 3581 · Başarısız 27 · Atlanan 0`
(`scratchpad/trx2-pP-36b/*.trx`, `<Counters total="3608" passed="3581" failed="27"/>`).
Paket öncesi 3557 → **+51**, birebir yeni test sayısı kadar.
**27 kırmızının hiçbiri P-36b'nin dokunduğu yolda değildir** (§ 9).

> **Kendi kararsız testimi buldum ve düzelttim (md.1):** ilk tam koşumda
> `SU8_Varsayilan_bekleme_kayit_secici_ile_ayni` kırmızıydı. Sebep bendeydi:
> `Secici.BeklemeMs` **statik**ti ve paralel koşan sınıflar birbirinin değerini
> eziyordu. Statik kaldırıldı, gecikme **parametre** oldu (`VarsayilanBeklemeMs`
> sabiti ayrı). Test "kararsız" diye işaretlenip geçilmedi, **kök nedeni
> giderildi**.

---

## § 7. MUTASYON KANITI — 12/12

Yöntem: kaynağı `scratchpad/mut-yedek/P-36b/` altına kopyala → `perl -0777 -pi -e`
ile **tek** değişiklik → filtreli `dotnet test` → yedeği geri kopyala → `cmp` ile
birebir doğrula. **Hepsi "GERİ ALINDI (cmp birebir aynı)".**

| Kod | Bozulan kural | KIRMIZIYA DÖNEN TEST |
|---|---|---|
| M36b-1 | `FormAlani.Ondalik` virgül→nokta normalleştirmesi | `G6_Sayi_virgul_ve_nokta_ayirici_kabul_edilir(ham:"12,5")` |
| M36b-2 | Bozuk sayıda hata yerine sessizce `0` yazmak | `G7_Bozuk_sayi_sessiz_etkisiz_kalmaz` |
| M36b-3 | Tek seçenekte değeri otomatik koymamak (UX-S22) | `G16_Tek_secenekte_secici_gizlenir_ve_deger_otomatik_konur` |
| M36b-4 | Uzak kipte asgari sorgu uzunluğu (2 → 1) | `SU2_Kisa_sorgu_istek_atmaz_sebebi_yazili` |
| M36b-5 | Hatayı boş listeye çevirmek (UX-S28) | `SU5_Hata_bos_duruma_cevrilmez` |
| M36b-6 | PATCH'te gönderilmeyen alanı da yazmak | `GH5_Gonderilmeyen_alan_korunur` + `GH8_Bos_govde_400_doner` |
| M36b-7 | Şema beyaz listesini kaldırmak | `GH7_Bilinmeyen_sema_400_doner` |
| M36b-8 | `[RolGerekli("SAHIP")]` kapısını kaldırmak | `GH3_Tezgahtar_gorunumu_degistiremez` |
| M36b-9 | Kurumsal şemanın `--metin-soluk`unu AA altına düşürmek | `Kontrast_AA_saglanir` → *"SEMA-KURUMSAL: --metin-soluk(#9ca3af)/--yuzey(#ffffff) = 2.54:1"* |
| M36b-10 | Kabukta alan altı hatayı çizmemek | `G3_Hata_alan_altinda_ve_erisilebilir` (+`G7`, `G11`) |
| M36b-11 | İstemci tarafı beyaz listeyi kaldırmak | `GE7_Sunucudan_gelen_bilinmeyen_sema_varsayilana_duser` |
| M36b-12 | Kilitli anahtardan `disabled`ı kaldırmak | `G18_Kilitli_anahtar_istek_atmaz_ve_sebebi_yazili` |

**Test açığı bulunmadı** — 12 mutasyonun 12'si kırmızı verdi. M36b-9 ayrıca
şemaların **gerçekten ölçüldüğünü** kanıtlar: sayaç "var" demiyor, hesap yapıyor.
Son mutasyondan sonra `dotnet build Kuyumcu.sln --nologo` → **0 Hata 0 Uyarı**.

---

## § 8. CANLI DOĞRULAMA (http://localhost:5198 — 5199'a DOKUNULMADI)

| # | Adım | Beklenen | GERÇEK |
|---|---|---|---|
| C1 | `GET /api/v1/gorunum` jetonsuz | 401 | **401** |
| C2 | `GET /gorunum` (SAHİP, kayıt yok) | varsayılan | **200** `{"sema":"altin","yaziOlcegi":"yogun"}` |
| C3 | `PATCH {"sema":"kurumsal"}` | 200 + sonuç | **200** `{"gorunum":{"sema":"kurumsal","yaziOlcegi":"yogun"},"mesaj":"Görünüm kaydedildi"}` |
| C4 | `PATCH {"yaziOlcegi":"okunakli"}` | şema KORUNUR | **200** `{"sema":"kurumsal","yaziOlcegi":"okunakli"}` |
| C5 | `PATCH {"sema":"pembe"}` | 400 beyaz liste | **400** `"Bilinmeyen renk şeması: pembe"` |
| C6 | `PATCH {}` | 400 | **400** `"Hiçbir alan gönderilmedi — değiştirilecek bir şey yok."` |
| C7 | `PATCH {"sema":null}` | varsayılana dön | **200** `{"sema":"altin","yaziOlcegi":"okunakli"}` |
| C8 | `/css/belirtecler.css` yayında | `data-sema`+`data-olcek` | **6 eşleşme** |
| C9 | `/js/tema.js` yayında | `kuyumcuGorunum` | **1 eşleşme** |
| C10 | `/css/app.css` yayında | girdi ailesi sınıfları | **5 eşleşme** (`alan-kaynak`, `alan-onizleme`, `anahtar-topuz`) |
| C11 | TEZGÂHTAR `PATCH` | 403 | **403** `"Bu işlem için gerekli rol: SAHIP"` |
| C12 | TEZGÂHTAR (şubesiz) `GET` | 200 | **403** `"Şube bağlamı çözülemedi…"` — ⚠ bkz. not |
| C12b | TEZGÂHTAR (şube atanmış) `GET` | 200 | **200** `{"sema":"altin","yaziOlcegi":"okunakli"}` |
| C13 | 5198 kapatıldı | dinleyen yok | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0** |
| C14 | 5199 korundu | dokunulmadı | **2 dinleyici duruyor** |

> **C12 notu (md.1 — gizlenmedi):** şubesiz TEZGÂHTAR `/gorunum`u okuyamıyor, ama
> sebep **bu uç değil**, tüm istekleri kapsayan şube bağlamı ara katmanıdır.
> Davranış doğru tarafta hata veriyor (fail-closed) ve ekran bunu kaldırıyor:
> `GorunumServisi.Yuklendi=false` kalır, Görünüm bölümü **kilitlenir ve sebebini
> yazar** (`GE4`). Yine de **kurucuya not**: şubesiz kullanıcı ürünün büyük
> kısmını kullanamıyor; bu P-36b'nin değil, kullanıcı oluşturma akışının konusu.

**SQL kanıtı (kuyumcu_net):**

```
kiraci_ayarlari (test kiracısı):  gorunum_yazi_olcegi = okunakli     -- C7 sonrası; sema satırı SİLİNDİ (null = varsayılan)
denetim_kayitlari:               Gorunum | EKLEME | "Görünüm ayarı güncellendi: renk şeması"
                                 Gorunum | EKLEME | "Görünüm ayarı güncellendi: yazı tipi ölçeği"
                                 Gorunum | EKLEME | "Görünüm ayarı güncellendi: renk şeması"
__EFMigrationsHistory:           61  (paket öncesi 61 — migration ÜRETİLMEDİ)
```

**Temizlik:** `information_schema`'dan `tenant_id` kolonu olan **62 tablo**
bulundu, hepsinden test kiracısı silindi, `kiraci_davetleri` ve `tenants` satırı
da silindi. **ARTIK_SATIR_SAYISI = 0.**
`SELECT COUNT(*) FROM tenants` → **9** (başlangıçtaki değer).
`kiraci_ayarlari` → **1** (başlangıçtaki değer).
`QA-TEST-` önekli kalıcı QA fikstürlerine **dokunulmadı**.
Canlı doğrulama için üretilen parola / sistem anahtarı / jeton hiçbir yere
yazılmadı; geçici dosyalar `printf '' >` ile boşaltıldı.

---

## § 9. AĞAÇTA BULUNAN, P-36b'YE AİT OLMAYAN KIRIKLAR (Anayasa md.1)

Bunların **hiçbiri bu pakette doğmadı** ve hiçbiri bu pakette **düzeltilmedi**:
başkasının paketinin kodunu izinsiz değiştirmek ya da başkasının taban dosyasını
"düzeltmek" kuralı bozardı ve sorumluluğu gizlerdi. **Orkestratöre eskale.**

### 9.1 27 kırmızı test

| Küme | Adet | KÖK NEDEN (kanıt) | Sahibi |
|---|--:|---|---|
| `BasitModMenuTestleri` · `MenuMimarisiTestleri` · `KabukAramaEkranTestleri` · `IceAktarimEkranTestleri` · `YedekMenuTestleri` · `IkonTestleri` (2) | **22** | `System.InvalidOperationException : Cannot provide a value for property 'Tercih' on type 'Kuyumcu.Web.Layout.KenarCubugu'. There is no registered service of type 'MenuTercihServisi'` — P-37 `KenarCubugu`a yeni bir servis enjekte etti, bu 6 test fikstüründe **kaydetmedi** | **P-37** |
| `IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI` | 1 | `Emoji ikon geri sızdı: Layout/KenarCubugu.razor:147` (★ / ☆ favori yıldızı) | **P-37** |
| `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` | 1 | `Politika haritasında damgası olmayan varlık(lar): KullaniciTercihi` — P-37 DbSet ekledi, `DuzeltmePolitikasi`ye damga koymadı | **P-37** |
| `OlcumKapisiTestleri` (2) | 2 | Canlı `IKiraciVarlik` sayısı **63**, `ham/olcum-2026-09-18.json` **62** | ölçüm dosyasını güncellemeyen paket |
| `TasarimBelirtecTestleri.Mod_yazicisi_…_yeni_betik_eklemez` | 1 | `index.html` **7** `<script>`, test **6** bekliyor (`siralistesi.js` / `indir.js` eklenmiş, sayaç güncellenmemiş) | ilgili paket |

**P-36b'nin kendi kümeleri tamamen yeşildir:** `GirdiAilesiBilesenTestleri` 23/23 ·
`SeciciUzakKipTestleri` 10/10 · `SeciciBilesenTestleri` 15/15 ·
`GorunumHttpTestleri` 10/10 · `GorunumEkranTestleri` 8/8 · `UX1C4EkranTestleri` 34/34.

### 9.2 4 denetim ihlali

```
✗ IKiraciVarlik uygulayan varlık        63 (tam 62 olmalı — P-02 -eq kapısı)
✗ index.html <script> sayısı (sabit 6)   7 (beklenen 6)
✗ Layout+Bilesenler emoji ikon           2 (beklenen 0)   → KenarCubugu.razor ★ ☆
✗ P-02: OLCUM_TABANI_KOPYA               1 sapma
```

Dördü de yukarıdaki kırmızıların denetim.sh karşılığıdır; dördü de P-36b'nin
dokunmadığı dosyalardan gelir (`index.html`, `Layout/KenarCubugu.razor`,
`Kuyumcu.Api/Data/*` yeni varlık, `ham/olcum-2026-09-18.json`).

**P-36b'nin eklediği 5 sayacın 5'i de YEŞİL.**

---

## § 10. DoD SÖZLEŞMESİ

| Varlık / kayıt | Düzenlenebilir mi | Uç | Gerekçe |
|---|---|---|---|
| **Kiracı görünümü** (`KiraciAyari` satırları `gorunum_semasi`, `gorunum_yazi_olcegi`) | **EVET** — istediğin kadar değiştirilir, `null` ile varsayılana döner | `GET /api/v1/gorunum` · `PATCH /api/v1/gorunum` `[RolGerekli("SAHIP")]` | Bir **tercihtir**, finansal varlık değildir; hard DELETE yasağı kapsamaz. Ekran karşılığı: `/ayarlar` → Görünüm bölümü, iki seçici, anında uygulanır |
| Girdi ailesi bileşenleri | Kayıt değil, **giriş denetimidir**; taşıdığı değer çağıran formun kendi kaydetme yoluyla düzenlenir | çağıranın ucu | — |
| `<Secici>` Uzak kipi | Kayıt üretmez, kayıt **seçer** | `/arama` (salt okur) | — |

**Yeni finansal varlık DOĞMADI**, deftere yazan yol açılmadı, `HareketFabrikasi`
dışına çıkılmadı, `ExecuteUpdate/Delete` beyaz listesi genişlemedi.

**"Sunucuda uç var, ekranda düğme yok" YOK** (UX-S25): `PATCH /gorunum`un ekran
karşılığı `/ayarlar` → Görünüm'dür ve iki seçici de gerçek istek atar.
**"Tıklanınca hiçbir şey olmayan düğme" YOK** (DoD EK md.1): kilitli seçicilerin
`disabled`ı ve **görünür sebebi** vardır (`GE3`, `GE4`, `G18`).

---

## § 11. DOKUNULAN SAYFALAR (tek dokunuş kaydı)

| Dosya | İlk mi | Gerekçe |
|---|---|---|
| `Pages/Ayarlar.razor` | ilk (KİLİT AÇILDI) | Görünüm bölümü + profil pilot göçü; kilit `ham/TEK-EL-KILIT`e yazıldı |
| `Pages/Isletme.razor` | ilk | kimlik çekmecesi pilot göçü |
| `Pages/PusulaBelgesi.razor` · `Pages/GiderPusulalari.razor` · `Pages/Uyum.razor` | ilk | biçimlendirmenin Bicim.cs'e taşınması |
| `Pages/Panel.razor` | **İKİNCİ** | P-36 kontrast için açmıştı; satır 143'teki UX-S32 ihlali o turda ölçülmemişti ve `GIRDI_AILESI_DISI_BICIM` 0'a inmeden paket kapanamazdı — gerekçe kilit defterine yazıldı |
| `Bilesenler/Secici.razor` · `Bilesenler/ModalPencere.razor`(dokunulmadı) | ilk | uzak kip |

---

## § 12. DEĞİŞEN DOSYALAR

**Yeni (17):**
```
dotnet/Kuyumcu.Web/Bilesenler/Form/FormAlani.cs
dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs
dotnet/Kuyumcu.Web/Bilesenler/Form/AlanKabugu.razor
dotnet/Kuyumcu.Web/Bilesenler/Form/{Metin,Sayi,Para,Gram,Tarih,TarihAralik,
                                    Telefon,Eposta,CokSatir,Onay,Secim,Anahtar}.razor
dotnet/Kuyumcu.Api/Modules/Gorunum/GorunumModulu.cs
dotnet/Kuyumcu.Contracts/Sayfalar/GorunumSozlesmeleri.cs
dotnet/Kuyumcu.Web/Servisler/GorunumServisi.cs
dotnet/Kuyumcu.Tests/GirdiAilesiBilesenTestleri.cs
dotnet/Kuyumcu.Tests/SeciciUzakKipTestleri.cs
dotnet/Kuyumcu.Tests/GorunumHttpTestleri.cs
dotnet/Kuyumcu.Tests/GorunumEkranTestleri.cs
```
**Değiştirilen (22):**
```
dotnet/Kuyumcu.Web/_Imports.razor
dotnet/Kuyumcu.Web/Program.cs
dotnet/Kuyumcu.Web/Bilesenler/Secici.razor
dotnet/Kuyumcu.Web/Servisler/Bicim.cs
dotnet/Kuyumcu.Web/Pages/{Ayarlar,Isletme,Panel,PusulaBelgesi,GiderPusulalari,Uyum}.razor
dotnet/Kuyumcu.Web/wwwroot/css/{belirtecler.css,app.css}
dotnet/Kuyumcu.Web/wwwroot/js/tema.js
dotnet/Kuyumcu.Api/Data/VezneFisi.cs            (yalnız 2 ayar anahtarı sabiti)
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs        (yalnız 1 DI kaydı)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs           (SozlukForm + SozlukGorunum, TR+EN)
dotnet/denetim.sh                               (HAM_INPUT tavanı 365→357 + P-36b bloğu)
dotnet/Kuyumcu.Tests/{SeciciBilesen,TasarimBelirtec,UX1C4Ekran,SurumEkran,TemaEkran}Testleri.cs
dokumanlar/09-uygulama-kayitlari/ham/TEK-EL-KILIT
```
**COMMIT YAPILMADI.** `git status --porcelain` ağacın tamamını değişmiş gösterir
(önceki paketlerin birikmiş işi); yukarıdaki liste **bu paketin** dosyalarıdır.

**Ağaçta bulunan, bize ait OLMAYAN artıklar (md.1 — sayıldı, gizlenmedi):**
önceki paketlerin mutasyon/yedek artıkları proje ağacında duruyor —
`dotnet/denetim.sh.mutbak`, **10 adet `*.mutorj`** (`HareketFabrikasi.cs`,
`Fiyatlandirma.razor`, `Islemler.razor`, `Urunler.razor`, `VadeTakibi.razor`,
`Sayim.razor`, `VadeKapisi.cs` …) ve **3 adet `*.bak`**
(`AramaHttpTestleri.cs.bak`, `MadenDefteriTestleri.cs.bak`,
`P21CiroRucuTestleri.cs.bak`). Silme hook tarafından bloklu → **kurucuya
bırakıldı**. **P-36b bu artıkların hiçbirini üretmedi:** bu paketin mutasyon
yedekleri proje ağacına değil,
`scratchpad/mut-yedek/P-36b/` altına kopyalandı (§ 7 yöntemi).

---

## § 13. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her bileşen için bUnit testi | ✔ | 23 test; `G21` aile tamlığını ayrıca kilitler |
| (b) Mutasyon kanıtı (kural → kırmızı test → cmp) | ✔ | 12/12, § 7 |
| (c) 375/768/1440 pilot ekranlarda yatay kaydırma 0 | **YAPILMADI** | Tarayıcı turu koşulmadı — § 14 |
| (d) `dotnet/denetim.sh` TEMİZ | **HAYIR** | 4 ihlal, **dördü de paket öncesinden** (§ 9.2); P-36b'nin 5 sayacı yeşil |
| (e) Tam süit 0 başarısız | **HAYIR** | 3581/3608; 27 kırmızının **hepsi paket öncesinden** (§ 9.1) |
| (f) Test kiracısı silindi | ✔ | ARTIK=0, tenants 9→9 |
| (g) Rapor + Kimlik kapanış tablosu | ✔ | bu dosya + § 15 |
| Derleme 0 hata / 0 uyarı | ✔ | `build`: `0 Uyarı 0 Hata` |
| HAM_INPUT tavanı DÜŞTÜ | ✔ | 365 → **357** |
| GIRDI_AILESI_DISI_BICIM = 0 | ✔ | 8 → **0** |
| TEMA_KONTRAST_IHLALI = 0 | ✔ | 2 şema, oranlar HESAPLANDI, mutasyon M36b-9 |
| Tabanlar TAZE ölçüldü, komutlar denetim.sh'a yazıldı, tavan artırılmadı | ✔ | § 1 |
| Migration prosedürü | gerekmedi | § 4.1 — gerekçeli |
| Mevcut element id'leri DEĞİŞMEDİ | ✔ | `pr-*`, `ikimlik-*` birebir |
| Yeni id kalıbı `<önek>-<alan>` | ✔ | `-sar -ipucu -durum -hata -onizleme -bas -bit -secenek-<n>` |
| Sozlukler TR+EN, çıplak metin yok | ✔ | `SozlukForm` + `SozlukGorunum` (required → eksik anahtar = derleme hatası) |
| Pages içinde yeni istemci record açılmadı | ✔ | `Kuyumcu.Contracts/Sayfalar/GorunumSozlesmeleri.cs` |
| Razor özniteliğinde bloklu lambda yok | ✔ | `SemaSec`/`OlcekSec` adlandırılmış metot |
| bUnit sahte API yeni uca GERÇEKÇİ yanıt | ✔ | `GorunumEkranTestleri.SahteApi` `/gorunum`u gerçekten modelliyor |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✔ | `GIRDI_AILESI_TEK_KAYNAK` + `Ham_hex_beyaz_listeyi_asmaz` (tavan 0) |
| 5199'a dokunulmadı | ✔ | 2 dinleyici duruyor |
| COMMIT YOK | ✔ | çalışma ağacında |

---

## § 14. AÇIK KALANLAR

1. **375/768/1440 tarayıcı ölçümü yapılmadı** (DoD (c)). Pilot ekranlar
   (`/ayarlar`, `/isletme`) için `main.ana-icerik.scrollWidth` ölçümü
   koşulmadı; CSS tarafında satır kapsayıcıları `flex-wrap`lı ve girdi satırı
   `min-width:0` taşıyor ama **ölçülmedi, dolayısıyla karşılanmadı sayılır.**
2. **Göç P-36c'ye devredildi:** 357 ham girdi ve 176 ham `<select>` duruyor;
   ikisinin de tavanı kilitli, geri gidilemez.
3. **`KayitSecici`nin 4 kullanımı** göç etmedi (karar ve gerekçe § 3);
   `KAYIT_SECICI_KULLANIM ≤ 4` cırcırı beşinciyi engelliyor.
4. **Renk şeması karanlık temada uygulanmıyor** (bilinçli sınır, § 4.3).
   Karanlık için ayrı şema istenirse 2 palet daha ölçülmeli.
5. **§ 9'daki 27 kırmızı + 4 denetim ihlali** — P-37 şeridine ait, orkestratör
   kararı bekliyor.

---

## § 15. KİMLİK KAPANIŞ TABLOSU

| Kimlik | Konu | Durum | Kanıt |
|---|---|---|---|
| **kurucu talimatı 20/a** | "tüm inputlar için bize ait birer component" | **KAPANDI (aile)** · göç açık | 12/12 bileşen, 23 test, `GIRDI_AILESI_TAM` |
| **kurucu talimatı 16** | "renk ve yazı tipi benim işim" | **KAPANDI** | /ayarlar Görünüm, 2 şema + 2 ölçek, AA ölçüldü |
| **FRONTEND-KIMLIK § 6.3** | `<Secici>` Uzak kipi · KayitSecici'nin yutulması | **KİP KAPANDI** · göç P-36c | 10 test + 2 mutasyon |
| **FRONTEND-KIMLIK § 6.5** | 2 karakter · 250ms · yarış koşulu · sınır · hata≠boş | **KAPANDI** | `SU2`–`SU10` |
| **FRONTEND-SARTNAME § 6.1** | Renk şemaları AA | **KAPANDI** | hesaplanmış oranlar § 4.3 + M36b-9 |
| **FRONTEND-SARTNAME § 6.2** | Yazı tipi ölçeği | **KAPANDI** | `data-olcek="okunakli"`, varsayılan değişmedi |
| **UX-S21 / S22 / S25–S32** | (girdi ailesi ve Görünüm kapsamında) | **UYGULANDI** | § 2 tablosu |
| **UX-S32** | Para birimi tutarla taşınır | **Panel.razor'da KAPANDI** | `Bicim.Para(bakiye, paraBirimi)` |
| **B-25** | ULID ekrana basılmaz | **Uzak kipte UYGULANDI** | `SU4` |
| **DoD EK md.1 / md.3** | kilitli düğme sebebi · sessiz etkisizlik | **UYGULANDI** | `G18`, `G7`, `GE3`, `GE4` |
| **DoD (c) mobil ölçüm** | 375/768/1440 | **ÖLÇÜLMEDİ** | § 14 md.1 |
| **DoD (d) denetim TEMİZ** | | **HAYIR — paket dışı 4 ihlal** | § 9.2 |
| **DoD (e) süit 0 kırmızı** | | **HAYIR — paket dışı 27 kırmızı** | § 9.1 |

---

## § 16. KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **Renk ve yazı tipi artık sizin şeridiniz.** `/ayarlar` → **Görünüm**:
   üç renk şeması (Altın klasik · Kurumsal mavi · Zümrüt) ve iki yazı boyutu
   (Yoğun · Okunaklı). Seçim **tüm işletme için** geçerli, **anında** uygulanıyor
   ve **yalnız işletme sahibi** değiştirebiliyor. Bugünkü görünüm varsayılan
   kaldı: **hiçbir şey kendiliğinden değişmedi.**
2. **Listedeki her şema okunabilirlik eşiğini ölçülerek geçiyor.** Gözle
   bakılmadı; WCAG 2.1'in kendi formülüyle hesaplandı ve test bunu her koşuda
   yeniden hesaplıyor. Eşiği geçmeyen bir renk listeye **konamıyor** — bunu
   kanıtlamak için kasten bozduk, test kırmızı oldu (M36b-9).
3. **Sorulan karar cevaplandı ve migration gerekmedi.** P-36 raporu *"tercih
   tarayıcıda mı kiracıda mı saklansın?"* diye sormuştu. **Kiracıda** saklanıyor
   (her cihazda aynı görünüyor) ve bunun için **yeni bir veritabanı kolonu
   açılmadı** — kiracı ayarları tablosu zaten vardı. Veritabanı şeması
   değişmedi.
4. **Bu paket bitmedi diye işaretlendi ve sebebi şudur:** paketin kendi işi
   tamam, ama **ağaçta bizden önce kırılmış 27 test ve 4 denetim ihlali**
   duruyor (§ 9). Hepsi menü/kenar çubuğu şeridinin (P-37) yarım işinden
   geliyor. Bunları izinsiz düzeltmedik — başkasının kodunu değiştirmek ve
   başkasının ölçüm defterini "düzeltmek" kural dışı olurdu. **Devralma notundaki
   "3371/3371 yeşil · denetim TEMİZ" ifadesi güncel değil.**
5. **Küçük ama gerçek bir bulgu:** şubesi atanmamış bir tezgâhtar ürünün büyük
   kısmını kullanamıyor (her istekte "Şube bağlamı çözülemedi"). Bu bir
   P-36b konusu değil; kullanıcı oluşturma akışında şube atamasının zorunlu
   hâle getirilmesi öneriliyor.
6. **Mali müşavire not gerekmedi** — bu pakette hiçbir mevzuat hükmü koda
   girmedi; para/gram biçimlendirmesinin çıktısı birebir korundu.
