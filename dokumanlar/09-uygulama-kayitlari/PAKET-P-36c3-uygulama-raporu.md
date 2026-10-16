# PAKET P-36c/3 — HAM GİRDİ GÖÇÜ (P-36c'nin ikinci ayağı) · uygulama raporu

**Tarih:** 2026-09-21 · **Dalga:** DALGA 6 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Sürüm:** 1.0
**Önkoşul:** P-36b (girdi ailesi) · P-36c/1-2 (select göçü + pencere tek tipi)

> ⚠ **Durum beyanı: YARIM.** Paketin kod ayağı bitti (ölçülü), fakat paketin
> **kabul eşiği** (QA turu: görsel ≥ 4,0 · kullanıcı dostu ≥ 3,6) **ÖLÇÜLMEDİ**;
> spesifikasyon "ölçülmeyen eşik karşılanmamış sayılır" diyor. Ayrıca ortak girdi
> katmanında 3 bileşen hâlâ ham `<input>` taşıyor (sayaçla beyan edildi, gizlenmedi).
> Ayrıntı § 12 "Açık kalanlar".

---

## 1. Önceki doğrulamadan devralınan bulgular ve akıbetleri

| # | Bulgu (doğrulayıcı) | Akıbet |
|---|---|---|
| 1 | **[KRİTİK]** Ham girdi göçü hiç yapılmadı; `HAM_INPUT 357`'de duruyor | **KAPANDI.** Serbest ekranların TAMAMI göç etti. `HAM_INPUT 357 → 42`, yeni fail-closed sayaç `HAM_INPUT_SERBEST = 0`. |
| 2 | **[KRİTİK]** Kabul eşiği (QA turu) hiç ölçülmedi | **AÇIK.** QA turu çok-ajanlı bir orkestratör iş akışıdır (`betikler/qa-turu.js`, 10 ajan); uygulayıcı ajan onu koşamaz. Sahte puan ÜRETİLMEDİ. § 12/1. |
| 3 | **[ORTA]** `SABIT_RENK` tavanı (14) düşürülmedi | **KAPANDI.** Giriş illüstrasyonunun 14 SVG rengi belirtece çevrildi (`--giris-altin-*`, `--giris-tas-*`, `--giris-parilti` …). Ölçüm: **14 → 0**, tavan da 0'a indirildi. |
| 4 | **[ORTA]** DoD (c) ham verisinde 9 öğe-taşması rapora yazılmamış | **KAPANDI.** § 8.2'de 12 kaydın TAMAMI listelendi, gerekçelendirildi; `/vitrin`'de bulunan GERÇEK taşma düzeltildi (§ 8.3). |
| 5 | **[DÜŞÜK]** Beyan edilmeyen 0 baytlık artık `GeciciProbe.cs` | **BEYAN EDİLDİ.** § 13 temizlik listesi tam. Bu turda YENİ artık üretilmedi (mutasyon yedekleri scratchpad'de). |
| 6 | **[ORTA]** MySQL parolası önceki oturum çıktısına düşmüştü | **TEKRARLANMADI.** Parola bu oturumda hiç ekrana basılmadı; `sed -n '2p'` ile hedefli okundu (yorum satırı tuzağı atlandı). Döndürme kurucu kalemi olarak duruyor. |
| 7 | **[DÜŞÜK]** Kaldırılan element id'si git diff ile ölçülemiyor | **ÖLÇÜLDÜ.** Dokunulan 39 dosyanın paket ÖNCESİ kopyası alındı, sonra kimlik kümesi karşılaştırıldı; sonuç § 9. |

---

## 2. Yapılanlar

### 2.1 Ortak girdi ailesine ÜÇ yeni üye (12 → 15)

| Bileşen | Neden doğdu |
|---|---|
| `Bilesenler/Form/Sifre.razor` | Parola kutuları `/kullanicilar`, `/giris`, `/ayarlar`'da ham `<input type="password">` idi; `autocomplete` ipucu her yerde başkaydı. |
| `Bilesenler/Form/Arama.razor` | 20'den fazla ekranda `<input type="search" class="girdi" style="flex:1">` tekrarlanıyordu: satır içi `style`, etiketsizlik ve "bir yerde oninput bir yerde onchange" tutarsızlığı. Tek davranış: yazarken süzer. `Enter` geri çağrısı barkod okuyucu içindir. |
| `Bilesenler/Form/SaltOkunur.razor` | Türetilen değerler (`TL karşılığı`, `tek kasa`, `künye satırı`, `destek kodu`) `<input readonly disabled>` ile çiziliyordu: ekran okuyucuya "düzenlenebilir alan" görünüyor, ham girdi sayacına giriyor ve kaynağı her ekranda başka türlü yazılıyordu. Artık METİNDİR; makine değeri `data-deger`'de (Secici ile aynı sözleşme). |

### 2.2 Aile tabanına FORM DEFTERİ KÖPRÜSÜ (kimlik korunumunun anahtarı)

Göçten önce hata mesajı `<AlanHatasi Alan="x" Durum="form"/>` ile çiziliyor ve
`id="alan-hata-x"` üretiyordu. Girdi ailesi hatayı kendi kabuğunda çizdiği için o
satır silinseydi **mevcut eleman kimliği kaybolurdu**. `FormAlani`'ye eklendi:

* `Durum` (FormDurumu) + `HataAlani` → mesaj defterden okunur, kimlik `alan-hata-x` **birebir** aynı doğar;
* `HataKimligi` → ekranın eski ÖZEL hata kimliği (`#sifre-hata`, `#s-ad-hata`, `#tasi-adet-hata` …) korunur;
* `AlanKabugu.HataId` + `IstegeGizle`, `CokSatir.SayacGoster`, `Onay.EtiketGizli` / `aria-label`.

İkinci bir hata çizim yolu AÇILMADI: göç edilen alanda `AlanHatasi` satırı silinir.

### 2.3 Göç edilen ekranlar (serbest ekranların TAMAMI)

`Cariler(44) · Urunler(52) · Islemler(24) · Evraklar(13) · Banka(12) · Kullanicilar(11) ·
Fiyatlandirma(11) · FiyatListesiUrunPaneli(11) · Vezne(9) · Giris(9) · Gider(9) · Ayarlar(9) ·
MasakFormu(9) · Uyum(7) · Kur(7) · Emanet(7) · SiparisDetay(6) · Kasalar(6) · Vitrin(5) ·
UrunAgaci(5) · Isletme(5) · Hurda(5) · Bildirimler(5) · VeriDisaAktarim(4) · GiderPusulalari(4) ·
UrunEtiketleri(3) · PosTahsilatlar(3) · Kurulum(3) · DenetimIzi(3) · Yetkiler(2) · Yedekler(2) ·
VeriIceAktarim(2) · IslemDefteri(2) · EvrakTakvimi(2) · BankaEkstre(2) · VadeTakibi(1) ·
GerekceAlani(1) · HataBandi(1)`

**Kilitli ekranlara DOKUNULMADI** (spesifikasyon gereği, kendi paketlerine devredildi):
`Faturalar(9) · Siparisler(8) · Pos(8) · Sayim(4) · KasaRaporu(4) · SatisAnalizi(2) · KarZarar(2) · Raporlar(1)` = **38 ham girdi**.

### 2.4 Ekranın kendi ayrıştırıcıları kaldırıldı (tek kaynak)

`Islemler.OndalikOku`, `Hurda.FiyatDegisti`, `Kurulum.TlDegisti/HasDegisti`,
`Vezne.*Degisti`, `SiparisDetay.*Degisti`, `FiyatListesiUrunPaneli.SecimDegisti`,
`BankaEkstre.SecimDegisti/TumunuSec`, `Bildirimler.IsAcikDegisti`, `Isletme.AiDegistir/VezneDegistir`
artık `ChangeEventArgs` değil, ailenin ÇÖZDÜĞÜ tipi (`decimal?` / `string?` / `bool` / `DateOnly?`) alıyor.
Ayrıştırma tek yerde: `FormAlani.Ondalik / TamSayi / TarihCoz`.

### 2.5 Sözlük (TR + EN, çıplak metin yok)

`Ortak.DegerYok` ("değer yok" / "no value"), `Urun.RodyumKaplanmaz`, `Urun.Kod`
(önce ekranda çıplak `"Kod"` yazılıydı).

---

## 3. Sapmalar ve gerekçeleri

| Sapma | Gerekçe |
|---|---|
| Ailenin 12 üyesi **15** oldu | Parola, arama ve salt-okunur alanlar aile dışında kalsaydı `HAM_INPUT_SERBEST = 0` imkânsızdı; üçü de spesifikasyonun "tüm girdiler ortak bileşen" hedefinin parçasıdır. `GIRDI_AILESI_TAM` 15/15'e güncellendi. |
| Vezne'nin `Adet/BirimFiyat/İşçilik` alanları `Sayi` değil `Metin` | Bu alanlar model içinde **string**'dir ve doğrulaması sayfada yapılır. `Sayi`'ya çevirmek tip sözleşmesini değiştirirdi ("decimal→int yazma" yasağının kardeşi). Davranış birebir korundu. |
| `Vitrin`'in üç tablosu `.tablo-sar` ile sarıldı | Kapsam genişletme değil, **DoD (c) kapısı**: 375 px ölçümünde `main.ana-icerik` 393 px'e taşıyordu (§ 8.3). P-18'in kurduğu desen kullanıldı. |
| Tablo hücresindeki onay kutularına `EtiketGizli` | İlk ölçümde yetki matrisi 1440 px'te 2392→4855 px'e şişti: her hücre "Rol — Modül" metnini basıyordu. Etiket **DOM'da ve aria'da kaldı**, yalnız görsel olarak gizlendi. |
| `denetim.sh`'te `K21_DONANIM_BEYANI` kontrolü değişti | `#u-rfid-ipucu` artık ekran dosyasında düz metin değil; kabuk onu `<ElemanId>-ipucu` olarak TÜRETİR. Kaynak kontrolü "u-rfid alanı RfidIpucu taşıyor mu" diye soruyor; **DOM kimliği aynı**, ekran testi hâlâ `#u-rfid-ipucu` ile buluyor. |

---

## 4. Migration

**Gerekmedi.** Paket yalnız arayüz katmanına dokunur; hiçbir varlık, kolon, indeks
ya da uç sözleşmesi değişmedi. Veri tabanı şeması (56 migration) olduğu gibi durur.

---

## 5. Derleme

```
dotnet build Kuyumcu.sln --nologo   →   0 Uyarı · 0 Hata   (RC=0)
```

---

## 6. Test sayıları (önce / sonra)

| | Önce | Sonra |
|---|---|---|
| Toplam | 3624 | **3636** |
| Geçti | 3624 | **3636** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | 0 |

**Yeni test: 12** — `Kuyumcu.Tests/GirdiGocuTestleri.cs`
(GG1-GG2 Şifre · GG3-GG4 Arama · GG5-GG6 SaltOkunur · GG7-GG10 form defteri köprüsü
ve hata kimliği korunumu · GG11 CokSatir sayacı · GG12 Onay erişilebilir ad.)

**Uyarlanan mevcut testler (davranış değil, OLAY ve OKUMA YERİ):**
`.Change(` → `.Input(` (aile `oninput` dinler) ve `GetAttribute("value")` → `TextContent` /
`GetAttribute("data-deger")` (salt okunur alan artık girdi değil). Hiçbir iddia
zayıflatılmadı; ikisi GÜÇLENDİ (`TL_karsiligi_alani_salt_okunurdur` artık "girdi
YOK"u sınıyor, `Celik_… SESSIZ_KALMAZ` artık ipucu metnini değil GERÇEK hata
mesajını arıyor).

**İki kararsız (flaky) test kanıtla düzeltildi** — ürün hatası değil, test yarışı:
`BasitModPosTestleri.Iki_kasa_varken_secici_GORUNUR` ve
`UX4bEkranTestleri.POS_ana_kasa_secicisinde_DOVIZ_kasasi_YOKTUR` kasa listesi
sunucudan gelmeden seçiciyi açıp BOŞ okuyordu; `WaitForAssertion` eklendi.

**`SeciciYardim.Sec` tek kapı korundu**: ham `<select>` olmayan elemanda artık
DOM'a bakıp `oninput` mu `onchange` mi bağlı olduğunu görüyor — test tahmin etmiyor.

---

## 7. Mutasyon tablosu

Yöntem: kaynak `scratchpad/mut-p36c3/` altına kopyalandı → `perl -0777 -pi -e` ile
TEK eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı →
`cmp` ile birebir aynı olduğu KANITLANDI (her satırda "GERİ ALINDI (cmp birebir)").

| Kod | Kod / değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M-P36c3-01 | `FormAlani.HataElemanId` — `HataKimligi` önceliği silindi | `GG9_Ozel_hata_kimligi_birebir_korunur` | ✔ |
| M-P36c3-02 | `FormAlani.DefterHatasi` → daima `null` | `GG7`, `GG10` | ✔ |
| M-P36c3-03 | `AlanKabugu.HataEtkinId` → daima `{ElemanId}-hata` | `GG7`, `GG9`, `GG10` | ✔ |
| M-P36c3-04 | `SaltOkunur` — `data-deger` özniteliği silindi | `GG5` + `DovizBacagi` 3 testi | ✔ |
| M-P36c3-05 | `SaltOkunur` — boş değerde `BosMetin` yerine `""` | `GG6` | ✔ |
| M-P36c3-06 | `Arama` — Enter tuş kontrolü kaldırıldı (her tuş tetikler) | `GG4` | ✔ |
| M-P36c3-07 | `Arama` — `gorsel-gizli` etiket sınıfı değiştirildi | `GG3` | ✔ |
| M-P36c3-08 | *(açık)* desen İKİ yere uydu (yorum + kod) — **tek eşleşme kuralı ihlali**, mutasyon YORUMA gitti ve hiçbir test kırılmadı. Gizlenmedi, 08c olarak tekrarlandı. | — | ✔ |
| M-P36c3-08c | `Sifre` — `type="password"` → `type="text"` | `GG1` | ✔ |
| M-P36c3-09 | `CokSatir` — `SayacGoster` yok sayıldı | `GG11` + `UX1C4.C4_40` | ✔ |
| M-P36c3-10 | `Onay` — `aria-label` silindi | `GG12` + `UX1C3.C3_72` | ✔ |
| M-P36c3-11 | `Metin` — `@oninput` → `@onchange` | `CariEkranTestleri` 3 testi | ✔ |
| M-P36c3-12 | `AlanKabugu` — `IstegeGizle` yok sayıldı | `GG5` | ✔ |

**Test açığı bulundu mu:** M-P36c3-08'de evet — ama kusur testte değil MUTASYONDA idi
(desen iki yere uyduğu için yalnız yorumu değiştirdi). Doğru hedefle (08c) test
anında kırmızıya döndü. Son mutasyondan sonra TEMİZ derleme yapıldı (0 uyarı / 0 hata).

---

## 8. DoD (c) — 46 rota × 375 / 768 / 1440 px ölçümü

Gerçek Chrome (headless, CDP), örnekleme yok: `scratchpad/olc36c3/olc3.js`,
ham veri `scratchpad/olc36c3/olcum-3boy.json` (138 kayıt).
Ölçüt: `main.ana-icerik.scrollWidth > clientWidth`.

### 8.1 Özet

| Genişlik | Rota | TAŞAN |
|---|---|---|
| 375 px | 46 | **0** |
| 768 px | 46 | **0** |
| 1440 px | 46 | **0** |

### 8.2 Ham verideki ÖĞE taşmaları (12 kayıt) — gizlenmiyor

Bunlar sayfa taşması DEĞİLDİR: hepsi **kaydırma kabı içinde hapsedilmiş**
(`.tablo-sar` yatay kaydırma, `.sekme-serit` yatay kaydırılabilir sekme şeridi).
`main.ana-icerik` hiçbirinde taşmıyor, yani sayfada yatay kaydırma yok.

| Genişlik | Rota | Taşan öğe (ilk) | Gerekçe |
|---|---|---|---|
| 375 | /bildirimler | `table.veri-tablosu@675` | tablo sarmalayıcı |
| 375 | /denetim-izi | `table.veri-tablosu@1195` | tablo sarmalayıcı |
| 375 | /islemler | `button.sekme@463…673` | kaydırılabilir sekme şeridi (P-36 `.sekme-serit` min-width) |
| 375 | /isletme | `table.veri-tablosu@627` | tablo sarmalayıcı |
| 375 | /kullanicilar | `table.veri-tablosu@1077` | tablo sarmalayıcı |
| 375 | /vitrin | `table.tablo@393` | **bu pakette sarmalayıcıya alındı** (§ 8.3) |
| 768 | /bildirimler · /denetim-izi · /kullanicilar · /yedekler | `table.veri-tablosu` | tablo sarmalayıcı |
| 768 · 1440 | /yetkiler | `table.veri-tablosu.tablo-yapiskan-ilk` | yetki matrisi — yapışkan ilk sütun + yatay kaydırma |

`/islemler` sekme şeridinin 375 px davranışı gerçek tarayıcıda doğrulandı:
**taşma değil, kaydırılabilir şerit** — `main.ana-icerik` 375 = 375.

### 8.3 Ölçümün yakaladığı GERÇEK kusur ve düzeltmesi

İkinci koşumda `/vitrin` 375 px'te `msw 393 > mcw 375` verdi (`table.tablo@393`).
Üç Vitrin tablosu (`#vitrin-pano`, `#vitrin-atlananlar`, `#vitrin-ayarlar`)
sarmalayıcısızdı. P-18 deseniyle `.tablo-sar` içine alındı; üçüncü koşumda
375/768/1440 için **TAŞAN = 0**. (Yetki matrisi de ilk koşumda 4855 px'e şişmişti;
`EtiketGizli` ile 2392 px'e indi — sebebi ve ölçümü § 3.)

---

## 9. § Kimlik kapanış tablosu (mevcut element id'leri DEĞİŞMEZ)

Yöntem (doğrulayıcının DÜŞÜK bulgusuna cevap, commit gerektirmez): dokunulan **39**
dosyanın paket ÖNCESİ kopyası `scratchpad/mut-yedek/p36c3/` altına alındı; paket
sonunda `id="…"` / `ElemanId="…"` / `Id="…"` sabit değer kümeleri karşılaştırıldı.

| Sonuç | Sayı | Açıklama |
|---|---|---|
| Kaynak metninden kalkan kimlik | 47 | **46'sı kabuktan TÜRETİLİYOR** (`<ElemanId>-ipucu` / `-hata` / `-birim` / `-sayac`) — DOM'da birebir aynı. |
| Gerçekten kaybolan kimlik | **2** | `#hrd-kabul-notu` (Hurda) ve `#i-vezne-aciklama` (İşletme) — **ikisi de geri konuldu** (özgün `<span id=…>` korunarak). |
| Son durumda kayıp kimlik | **0** | — |

Dolaylı kanıt: kimliklere bağlı 3636 testin tamamı yeşil; kimlik kaybı olsaydı
`Bunit.ElementNotFoundException` ile kırmızıya dönerlerdi.

**Öneri (kurucuya/orkestratöre):** bu karşılaştırma bir kerelik betikle yapıldı.
Kalıcı hâli için dalga başında `Pages/` altındaki tüm `id="…"` değerlerinin anlık
görüntüsü alınıp paket sonunda karşılaştırılmalı; `denetim.sh`'a eklenebilir bir
sayaçtır (bu pakette **eklenmedi** — kapsam dışı, uydurma sayaç açılmadı).

---

## 10. § Önce / Sonra (mekanizma sayaçları — hepsi `dotnet/denetim.sh`)

| Sayaç | Önce | Sonra | Kural |
|---|---|---|---|
| `P-36: HAM_INPUT` | 357 | **42** | cırcır, yalnız azalır (tavan 357 → 42) |
| `P-36c: HAM_INPUT_SERBEST` | *(yoktu)* | **0** | **fail-closed** (yeni) |
| `P-36c: ORTAK_GIRDI_ISTISNA` | *(yoktu)* | **3** | cırcır, yalnız azalır (yeni) |
| `P-36c: GIRDI_AILESI_KULLANIM` | *(yoktu)* | **325** | cırcır, yalnız artar (taban 250) |
| `P-36b: GIRDI_AILESI_TAM` | 12/12 | **15/15** | fail-closed |
| `P-18: SABIT_RENK` | 14 (tavan 14) | **0** (tavan 0) | cırcır, yalnız azalır |
| `P-36: HAM_SELECT` | 30 | 30 | değişmedi (kilitli ekranlar) |
| `P-36c: HAM_SELECT_SERBEST` | 0 | 0 | korundu |
| `P-36c: PENCERE_GENISLIK_CESIDI` | 2 | 2 | korundu (tavan 3) |
| `P-36c: PENCERE_DUGME_SIRASI_TERS` | 0 | 0 | korundu |
| `P-36c: ODAK_TUZAGI` | 0 | 0 | korundu |
| `P-36c: BOS_DURUM_METINSIZ` | 0 | 0 | korundu |
| Tam süit | 3624 / 0 kırmızı | **3636 / 0 kırmızı** | — |

### Denetim çıktısı (son)

```
  ✓ P-18: SABIT_RENK                               0 (tavan 0 — cırcır; razor'da sabit renk yok)
  ✓ P-36: HAM_INPUT                                42 (tavan 42, yalnız azalır)
  ✓ P-36c: HAM_INPUT_SERBEST                       0 (serbest ekranlarda ham girdi yok)
  ✓ P-36c: ORTAK_GIRDI_ISTISNA                     3 (tavan 3 — cırcır; yalnız azalır)
  ✓ P-36c: GIRDI_AILESI_KULLANIM                   325 (taban 250, yalnız artar)
  ✓ P-36b: GIRDI_AILESI_TAM                        15/15
DENETİM TEMİZ.
```

---

## 11. DoD sözleşmesi (Bertan koşulu)

Bu paket **yeni varlık / kayıt türü DOĞURMAZ** — hiçbir tablo, uç, DTO ya da
yetki değişmedi; yalnız var olan alanların ÇİZİMİ ortak bileşene devredildi.
Bu yüzden PATCH / DELETE / `[ModulYetkisi]` / denetim kaydı kalemleri **konu dışıdır**.

Buna karşılık pakette **düzenlenebilirlik GENİŞLEDİ, hiç daralmadı**:

| Değişen | Önce | Sonra |
|---|---|---|
| Türetilen değer alanları (`dv-tl-karsilik`, `dt-kasa-tek`, `islem-bacak-*-tutar`, `ayar-kunye-satir`, `hata-kimlik-deger`, `evrak-form-tutar-salt`) | `<input readonly disabled>` — ekran okuyucuya "alan" görünüyor, klavye sırasında yok | `SaltOkunur` — metin + kaynak rozeti (UX-S21), makine değeri `data-deger` |
| Kilitli alanların sebebi | bazılarında yoktu | `DevredisiSebebi` ZORUNLU alan altı metni (UX-S30) |
| Alan altı hata | ekrana göre başka yerde/biçimde | tek kabuk, `role="alert"`, `aria-describedby` zinciri (DoD EK md.3) |

**Salt okunur kalanlar ve gerekçeleri:** `evrak-form-tutar-salt` (defter satırı
doğurdu — `DegismezGerekce` yazılı), `*-tl-karsilik` (kurdan türetilir, sunucu
hesaplar), `ayar-kunye-satir` / `hata-kimlik-deger` (sistem kimliği).

---

## 12. Açık kalanlar

1. **[KAPATAN: ORKESTRATÖR] Kabul eşiği ölçülmedi.** QA turu (`betikler/qa-turu.js`)
   10 ajanlı bir orkestratör iş akışıdır; uygulayıcı ajan onu koşamaz. Spesifikasyon
   "ölçülmeyen eşik karşılanmamış sayılır" dediği için **paket bu kalem yüzünden
   YARIM beyan edildi**. Sahte puan üretilmedi (Anayasa md.1).
   Taban: görsel 3,64 · kullanıcı dostu 2,98 → hedef 4,0 / 3,6.
2. **[SAYAÇLI] Ortak girdi katmanında 3 ham girdi.** `KayitSecici` (uzak arama),
   `SatirIciDuzenle` (`ElementReference` ile odak), `GlobalArama` (klavye gezinme +
   kısayol). Üçü de aileye alınabilir ama odak/klavye davranışları ayrı bir dilim
   ister. `ORTAK_GIRDI_ISTISNA = 3` cırcırıyla beyan edildi; yalnız azalabilir.
3. **[DEVREDİLDİ] Kilitli ekranlarda 38 ham girdi** — `Faturalar · Siparisler · Pos ·
   Sayim · KasaRaporu · SatisAnalizi · KarZarar · Raporlar`. Spesifikasyon gereği
   dokunulmadı; `P-36: HAM_INPUT` tavanı (42) onları ölçmeye devam ediyor.
4. **Vezne'nin sayısal alanları hâlâ metin kutusu** (model string). Sayısal klavye
   ve önizleme kazanmaları, model tipinin `decimal?`e çevrilmesiyle mümkün — tip
   sözleşmesi değişikliği, bu paketin kapsamı dışı.

---

## 13. Kurucuya / mali müşavire notlar

* **Temizlik (TurkReport hook `rm`'i bloklar — silme kurucudadır).** Bu turda YENİ
  artık üretilmedi; aşağıdakiler ÖNCEKİ turlardan kalmıştır ve eksiksiz sayılmıştır:
  `Kuyumcu.Tests/GeciciProbe.cs` (0 bayt) · `Kuyumcu.Tests/GeciciTaniTestleri.cs` (0 bayt) ·
  `Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak` ·
  `Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj` · `Kuyumcu.Api/Modules/Vade/VadeKapisi.cs.mutorj` ·
  `Kuyumcu.Web/Pages/{Fiyatlandirma,Islemler,Urunler,VadeTakibi,Sayim}.razor.mutorj`.
* **Güvenlik.** Yerel geliştirme kabının MySQL parolası önceki oturumun çıktısına
  düşmüştü; bu oturumda hiç basılmadı. O döküm paylaşılacaksa **parola döndürülsün**.
  Betiklerde `grep | head -1` yerine hedefli okuma kullanıldı (yorum satırı tuzağı).
* **Mevzuat.** Bu pakette hiçbir mevzuat hükmü koda yazılmadı; yalnız var olan
  beyanların ÇİZİMİ değişti (`u-rfid-ipucu` K-21 donanım beyanı dahil, DOM'da aynı).
* **Commit yapılmadı** (kurucu talimatı). Çalışma ağacında duruyor.

---

## 14. Değişen dosyalar

**Yeni (5):**
`Kuyumcu.Web/Bilesenler/Form/Sifre.razor` · `…/Arama.razor` · `…/SaltOkunur.razor` ·
`Kuyumcu.Tests/GirdiGocuTestleri.cs` · `dokumanlar/09-uygulama-kayitlari/PAKET-P-36c3-uygulama-raporu.md`

**Değişen — ortak katman (11):**
`Bilesenler/Form/{FormAlani.cs,SayisalAlan.cs,AlanKabugu.razor,Metin.razor,Sayi.razor,Para.razor,Gram.razor,Tarih.razor,TarihAralik.razor,Telefon.razor,Eposta.razor,CokSatir.razor,Onay.razor,Secim.razor,Anahtar.razor}` ·
`Bilesenler/{GerekceAlani.razor,HataBandi.razor}`

**Değişen — ekranlar (36):**
`Pages/{Ayarlar,Banka,BankaEkstre,Bildirimler,Cariler,DenetimIzi,Emanet,EvrakTakvimi,Evraklar,Fiyatlandirma,Gider,GiderPusulalari,Giris,Hurda,Islemler,IslemDefteri,Isletme,Kasalar,Kullanicilar,Kur,Kurulum,PosTahsilatlar,SiparisDetay,UrunAgaci,UrunEtiketleri,Urunler,Uyum,VadeTakibi,VeriDisaAktarim,VeriIceAktarim,Vezne,Vitrin,Yedekler,Yetkiler}.razor` ·
`Bilesenler/{FiyatListesiUrunPaneli,MasakFormu}.razor`

**Değişen — diğer (5):**
`Kuyumcu.Core/I18n/Sozlukler.cs` · `Kuyumcu.Web/wwwroot/css/{app.css,belirtecler.css}` ·
`dotnet/denetim.sh` · `Kuyumcu.Tests/{SeciciYardim.cs + 24 ekran testi dosyası}`
