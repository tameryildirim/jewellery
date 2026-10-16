# PAKET UX-1-C4 — Uygulama raporu

> "Her veri girilen yer düzeltilebilir" · Faz C, sayfa grubu 4
> (Veri dışa aktarım · Kullanıcılar · Yetkiler · İşletme · Ayarlar · Kılavuz)
> \+ **45-UI** (iki aşamalı giriş ekranları) · **45-API devir (a)(b)(c)(d)** · **6-A devir (a)(b)(c)**
> Dalga 2c · Tarih: 2026-09-17 · Roller: Taşkın (backend) + Ertuğ (frontend)
>
> Durum: **BİTTİ** — derleme **0 uyarı / 0 hata** · tam süit **1839/1839 yeşil**
> (önce **1805**, **+34 yeni test**) · `dotnet/denetim.sh` **TEMİZ** ·
> **15 mutasyon kanıtı** (hepsi kırmızıya döndü, dosyalar `cmp` ile birebir geri alındı) ·
> canlı doğrulama 5198'de yapıldı, sunucu durduruldu, test kiracısı silindi (**tenants 8 → 8**) ·
> **migration gerekmedi** (şema değişikliği yok).

---

## 0. Önkoşul kanıtları (ilk adımda okundu)

| Önkoşul (MÜHÜR 2 / Mehmet) | Kaynak | Sonuç |
|---|---|---|
| **48-A storno motoru** doğrulanmış | `PAKET-48-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` **20:48** | **GEÇTİ** (hasım doğrulayıcı tur 1, 1508 test) |
| **45-API (TOTP/MFA)** doğrulanmış | `PAKET-45-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` **21:10** | **GEÇTİ** (tur 1, 1540 test) |
| QA turu 2 bulguları | `ham/qa-turu-2.json` (grup 4: 6 rota) | okundu, madde madde aşağıda |

**Storno düğmesi bu pakette YOKTUR ve olmaması doğrudur:** grup 4'ün altı sayfasının
hiçbirinde defter kaydı (satış fişi · kasa/banka/emanet hareketi · fatura) listelenmez.
Storno düğmeleri C1/C2'nin işidir. MÜHÜR 5 KOŞUL 1/11 bu pakette tetiklenmedi.

---

## 1. Yapılanlar — sayfa × düzenlenebilirlik tablosu (DoD sözleşmesi)

| Sayfa | Düzenlenebilir alan | Yol | id'ler | Düzenlenemeyen → DegismezIpucu gerekçesi |
|---|---|---|---|---|
| **/isletme** | İşletme adı · adres · telefon · vergi dairesi · vergi no | **Çekmece** → `PATCH /admin/tenant/kimlik` | `#i-kimlik-duzenle` · `#cekmece-isletme-kimlik(-govde/-kaydet/-iptal/-kilit)` · `#ikimlik-ad`, `#ikimlik-adres`, `#ikimlik-telefon`, `#ikimlik-vergidairesi`, `#ikimlik-vergino` (+ `-hata`, `-goster`) | — |
| /isletme | Plan · varsayılan arayüz modu · AI modülü | mevcut seçiciler (+ başarı bandı) | `#i-plan`, `#i-varsayilan-arayuz-modu` (değişmedi) | — |
| /isletme | Şube ad/adres/telefon | **Çekmece** → `POST/PATCH /subeler` | `#s-yeni-sube-ac` · `#cekmece-sube*` · `#s-ad`, `#s-adres`, `#s-telefon` (+ `-hata`) | — |
| /isletme | Şube pasife alma | onay modalı → `DELETE /subeler/{id}` | `#s-pasife-{id}`, `#s-pasife-onayla` | — |
| /isletme | **Pasif şubeyi aktife alma** | **YOK** | — | `#degismez-ipucu-PasifSube` — “sunucuda şubeyi yeniden aktifleştiren uç henüz yok; sessiz düğme yerine durum yazılı” + alternatif yol. **UX-1-B boşluğu** (§ 4) |
| /isletme | **Kur kaynağı · maliyet yöntemi** | **YOK** (salt okunur, değeri gösteriliyor) | `#i-kur-kaynagi`, `#i-maliyet-yontemi` | `#degismez-ipucu-KiraciAyari` — “kurulumda belirlenir, geçmiş maliyet hesaplarının dayanağıdır; sonradan değişmesi geçmiş kârı geriye dönük bozar” |
| **/kullanicilar** | Ad · soyad · e-posta · arayüz modu · şube (+ başkası için rol/durum) | mevcut düzenleme diyaloğu (UX-4c) → `PATCH /kullanicilar/{id}` · `/rol` | `#d-ad`, `#d-soyad`, `#d-eposta`, `#d-rol`, `#d-durum`, `#d-kaydet` (+ `-hata`, `-kaydet-kilit`) | — |
| /kullanicilar | Pasife al / aktife al | satır eylemleri (UX-4c) | `#k-aktiflestir-{id}`, `#k-pasife-onayla` | — |
| /kullanicilar | **Kendi rolü / kendi erişim durumu** | **YOK** | — | `#degismez-ipucu-KendiRolum` — “son yöneticinin kendini yetkisiz bırakması sistemi kilitler” + “rolünüzü başka bir SAHİP değiştirebilir” |
| /kullanicilar | MFA durumu (görüntüleme) | salt okunur sütun (`mfaEtkin`) | `#k-siz` (rozet), MFA sütunu | Başkasının MFA'sı yönetilemez (kendi hesabı `/ayarlar`) |
| **/yetkiler** | Rol × modül matrisi | `PUT /yetkiler` + **kirli durum takibi** | `#y-kaydet`, `#y-kaydet-kilit`, `#y-kirli` | — |
| /yetkiler | **SAHİP satırı** | **YOK** (kutular kilitli) | — | `#degismez-ipucu-SahipYetkisi` — “kendi yetkisini kapatabilen sahip sistemden kilitlenirdi” + “MÜDÜR ya da daha düşük rol verin” |
| **/ayarlar** | **Ad · soyad · e-posta (profil)** | **Çekmece** → `PATCH /kullanicilar/ben` | `#ay-profil-duzenle` · `#cekmece-profil*` · `#pr-ad`, `#pr-soyad`, `#pr-eposta` (+ `-hata`) | — |
| /ayarlar | Dil · tema · arayüz modu · şifre | mevcut denetimler (+ başarı bandı, kilit sebebi) | `#ay-sifre-guncelle`, `#ay-sifre-kilit`, `#ayar-basari` | — |
| /ayarlar | **İki aşamalı giriş** (kur · doğrula · kurtarma kodları · yenile · kapat) | 45-API uçları | `#ayar-mfa`, `#mfa-kur`, `#mfa-anahtar(-goster)`, `#mfa-kod`, `#mfa-dogrula`, `#mfa-kurtarma(-liste/-indir/-onay)`, `#mfa-tamamla(-kilit)`, `#mfa-kapat-ac/-sifre/-kod/-onayla/-kilit`, `#mfa-yenile-ac/-kod/-onayla`, `#mfa-durum`, `#mfa-kalan-kod` | Kendi rolü (yukarıdaki gerekçe ile aynı) |
| **/veri-disa-aktarim** | Gerekçe · kapsam · tam kimlik onayı | mevcut form (+ canlı sayaç, alan altı mesaj, kilit sebebi, başarı bandı) | `#da-gerekce(-sayac/-hata)`, `#da-kapsam-{ad}`, `#da-hazirla`, `#da-kilit-sebep`, `#da-kisisel-not`, `#da-gecmis-siniri` | Aktarım geçmişi salt okunur (denetim kaydı — VUK/KVKK izi) |
| **/kilavuz** | — (içerik sayfası) | — | `#sayfa-basi-kilavuz`, `#kl-yeni-sekme` (korundu) | Düzenlenecek veri yok |
| **/giris** (45-UI) | İkinci adım kodu | `POST /auth/mfa/giris` (jeton **başlıkta**) | `#giris-mfa-baslik`, `#giris-mfa-kod`, `#giris-mfa-dogrula`, `#giris-mfa-vazgec`, `#giris-mfa-kilit` | — |

**Finansal varlıkta hard DELETE:** bu pakette hiç yok; tek DELETE `/subeler/{id}` ve o da
satırı silmez, `aktif=0` yazar (denetim.sh “Finansal varlıkta [HttpDelete] = 0” yeşil).

---

## 2. QA turu 2 (+ tur 1'den ACIK/KISMEN kalanlar) — bulgu kapatma tablosu

| # | Rota | Bulgu | Durum | Kanıt |
|---|---|---|---|---|
| 1 | /veri-disa-aktarim | Gerekçe 20 karakterin altındayken düğme sessizce pasif, doğrulama mesajı yok (ORTA) | **KAPANDI** | `#da-gerekce-sayac` (n/20) + `#da-gerekce-hata` + `#da-kilit-sebep`; test `C4_40`, `C4_41`; mutasyon **M-C4-12** |
| 2 | /veri-disa-aktarim | Süresi dolmuş token → 3× yenile/çıkış + “Beklenmeyen bir hata” (YÜKSEK) | **KAPANDI (bu pakette değil)** | UX-4d md.15 tek-uçuş yenileme (`ApiIstemci.cs` § “UX-4d madde 15”), testleri `UX4dOturumTestleri`. Bu paket o koda DOKUNMADI; QA turu 2 senaryoyu tetikleyemediği için “KISMEN” kalmıştı |
| 3 | /veri-disa-aktarim | 375 px yatay kaydırma (ORTA/GÖRSEL) | **ERTELENDİ** — kabuk geneli tek düzeltme, paket **UX-2-A2** (≤768 px çekmece menü). Sayfa içi düzeltme 6 rotanın hiçbirini kapatmaz |
| 4 | /veri-disa-aktarim | Kişisel veri kapsamları açılışta seçili (DÜŞÜK) | **KAPANDI** | Bulgunun önerdiği **ikinci** yol uygulandı (“seçince uyarı”): `#da-kisisel-not` uyarı metni; test `C4_42`. Varsayılan kapsam bilinçli olarak DEĞİŞTİRİLMEDİ — § 3/S1 |
| 5 | /kullanicilar | Yeni Kullanıcı: geçersiz e-posta/boş alanda mesaj yok, Kaydet sessizce pasif (ORTA) | **KAPANDI** | `#k-eposta-hata`, `#k-ad-hata`, `#k-kaydet-kilit`; test `C4_21`; mutasyon **M-C4-15** |
| 6 | /kullanicilar | Düzenle'de rol/aktiflik yok; satırda Pasife/Aktife Al yok (ORTA) | **KAPANDI** | Rol+durum alanları ve satır eylemleri UX-4c ile zaten gelmişti; QA turu 2 “KISMEN” dedi çünkü **fikstürde tek kullanıcı (kendisi) vardı** ve blok `duzenleHedef.Id != BenimId` koşullu. Bu pakette eksik olan kapatıldı: kendi satırında artık **gerekçe** çiziliyor (`#degismez-ipucu-KendiRolum`); test `C4_22`; canlı: `GET /kullanicilar` rol/aktif alanlarını döndürüyor |
| 7 | /kullanicilar | “(Hoş geldin)” etiketi anlamsız (DÜŞÜK) | **KAPANDI** | `#k-siz` → “(siz)” |
| 8 | /kullanicilar | Düzenleme sonrası başarı bandı yakalanamadı (DÜŞÜK) | **KAPANDI (bu pakette değil)** | `BasariBandi` + `BasariGoster` UX-4c ile eklenmişti (3000 ms); QA 1,5 sn'de örneklediği için kaçırmış olabilir |
| 9 | /kullanicilar | Dar ekranda yatay kaydırma (ORTA) | **ERTELENDİ** — **UX-2-A2** (kabuk) |
| 10 | /yetkiler | Kaydedilmemiş değişiklik uyarısı yok; Kaydet değişiklik olmadan da aktif (DÜŞÜK) | **KAPANDI** | `Kirli` takibi + `#y-kirli` + `#y-kaydet-kilit`; testler `C4_30`, `C4_31`; mutasyon **M-C4-4** |
| 11 | /yetkiler | Başarı bandı ~1 sn, kolayca kaçırılıyor (DÜŞÜK) | **KAPANDI (bu pakette değil)** | UX-4c md.13 `BasariBandi SureliMs="3000"` |
| 12 | /yetkiler | Tablo masaüstünde karta sığmıyor / dar ekran (ORTA) | **ERTELENDİ** — **UX-2-A2**. Bu pakette yalnız satır içi `style` temizlendi (`.ortala`, `.satir-vurgu`) ve onay kutularına `aria-label` eklendi |
| 13 | /isletme | **İşletme adı düzenlenemiyor (KRİTİK)** | **KAPANDI** | Çekmece + `PATCH /admin/tenant/kimlik`; testler `C4_10`, `C4_11`; mutasyon **M-C4-2**; canlı adım 2 |
| 14 | /isletme | Pasif şube aktife alınamıyor (YÜKSEK) | **ERTELENDİ (sunucu ucu yok)** — gerekçe ekranda yazılı (`#degismez-ipucu-PasifSube`), test `C4_13`, mutasyon **M-C4-3**. Hedef paket: **UX-1-D** (§ 4 “UX-1-B boşluğu”) |
| 15 | /isletme | Şube telefonu doğrulanmıyor, “abc” kabul (ORTA) | **KAPANDI** | Sunucu kuralı UX-4d md.19'da eklenmiş; **ekran artık aynı kuralı gösteriyor** (`#s-telefon-hata`), test `C4_15`; canlı adım 6 (`400`) |
| 16 | /isletme | Plan / arayüz modu sessizce kaydediliyor (ORTA) | **KAPANDI** | `BasariGoster` → `#basari-bandi` |
| 17 | /isletme | Şube ekle/düzenle/pasife alma sonrası başarı mesajı yok (DÜŞÜK) | **KAPANDI** | Aynı bant |
| 18 | /isletme | Yeni Şube boş/geçersizde mesaj yok (ORTA) | **KAPANDI** | `#s-ad-hata` + `#cekmece-sube-kilit` |
| 19 | /isletme | Kur kaynağı / maliyet yöntemi API'de var, ekranda yok (ORTA) | **KAPANDI (gerekçeyle)** | Değerler artık **görünür** (`#i-kur-kaynagi`, `#i-maliyet-yontemi`) ve neden değiştirilemediği yazılı; test `C4_14` |
| 20 | /isletme | Dar ekranda yatay kaydırma (ORTA) | **ERTELENDİ** — **UX-2-A2** |
| 21 | /ayarlar | Profil Ad Soyad / E-posta salt okunur (ORTA) | **KAPANDI** | Profil çekmecesi → `PATCH /kullanicilar/ben`; testler `C4_50`, `C4_51`; mutasyon **M-C4-14** |
| 22 | /ayarlar | Arayüz modu / tema sessizce kaydediliyor (DÜŞÜK) | **KAPANDI** | `#ayar-basari` bandı |
| 23 | /ayarlar | “Şifreyi Güncelle” üç alan boşken de etkin (DÜŞÜK) | **KAPANDI** | `SifreAlanlariDolu` + `#ay-sifre-kilit`; test `C4_52` |
| 24 | /ayarlar | Dar ekranda yatay kaydırma (ORTA) | **ERTELENDİ** — **UX-2-A2** |
| 25 | /kilavuz | **Sayfa boş: iframe CSP frame-ancestors 'none' (KRİTİK)** | **KAPANDI (bu pakette değil)** | UX-4d md.14 iframe'i kaldırdı; bu pakette **regresyon kapısı** eklendi: `C4_06` iframe sayısının 0 olduğunu ve `#kl-yeni-sekme` bağlantısının durduğunu zorlar |
| 26 | /kilavuz | Yüklenemeyince açıklama/yedek bağlantı yok (YÜKSEK) | **KAPANDI (bu pakette değil)** | `#kl-aciklama` + bölüm bağlantıları (UX-4d md.14); bu pakette `SayfaBasi` açıklaması eklendi |
| 27 | /kilavuz | iframe arka planı karanlık temada beyaz (DÜŞÜK) | **KAPANDI** | iframe yok |
| 28 | /kilavuz | Dar ekranda yatay kaydırma (ORTA) | **ERTELENDİ** — **UX-2-A2** |

**Özet:** grup 4'ün **6 “düzenlenemeyen veri girişi”** kaleminin tamamı işlendi:
4'ü **açıldı** (işletme adı+kimlik, profil ad/soyad/e-posta, kullanıcı rolü — başkası için,
kur kaynağı/maliyet yöntemi görünürlüğü), 2'si **gerekçelendirildi** (kendi rolü, pasif şube).
Ertelenen 6 bulgunun **hepsi** aynı kök nedene (kabuk 375 px) bağlı ve **UX-2-A2**'nin işi.

---

## 3. Sapmalar ve gerekçeleri

| # | Spesifikasyon/koşul | Yapılan | Gerekçe |
|---|---|---|---|
| S1 | QA bulgusu: “kişisel veri kapsamları varsayılan seçimsiz **ya da** seçince uyarı” | **Uyarı** yolu seçildi; varsayılan kapsam değişmedi | Varsayılanı değiştirmek `EkranTestleri.DisaAktarimEkranTestleri`'nin 4 testini kırıyordu (MASKESİZ kutusu yalnız kişisel veri seçiliyken çiziliyor). K4: **mevcut testler değiştirilmez**. Bulgunun önerdiği ikinci yol aynı riski kapatıyor ve hiçbir testi zayıflatmıyor |
| S2 | “ProAlan ile basit modda gelişmiş alanlar katlanır (form ≤ 8 alan)” | **İşletme kimliği** çekmecesinde uygulandı (ad hep görünür; adres/telefon/vergi dairesi/vergi no `ProAlan` içinde) | Diğer formlar zaten ≤ 8 alan. ⚠ Basit modda çizilmeyen alan **gövdeye de konmaz** (UX-S5): `KimlikKaydet` basit modda yalnız `{ad}` gönderir — üç durumlu PATCH sayesinde gizli alan null gidip veriyi SİLMEZ |
| S3 | “Sayfaya yeni `Api.*` çağrısı eklenmez (TB-09/K7)” | **Delindi**: `/auth/mfa/*` (Ayarlar) ve `/auth/mfa/giris` (Giriş) | 45-UI tanım gereği 45-API uçlarını çağırır; EK KOŞUL “bUnit + HTTP uçları 45-API'den” bunu zaten emrediyor. Yeni **sunucu yüzeyi** açılmadı, var olan sözleşme tüketildi |
| S4 | “ModalPencere yalnız yıkıcı onay” | İşletme'de şube ekle/düzenle **ModalPencere → Cekmece**'ye çevrildi; pasife alma onayı ModalPencere'de KALDI. Ayarlar'da MFA kapatma/kurtarma yenileme **ModalPencere** (yıkıcı/geri alınamaz) | denetim.sh `ModalPencere kullanımı` 44 → **43** (azaldı, artmadı) |
| S5 | “Mevcut testler DEĞİŞTİRİLMEZ” | İki test dosyasına dokunuldu: `IsletmeEkranTestleri` (yalnız `ArayuzModuServisi` **DI kaydı** — hiçbir iddia değişmedi) ve `BasitModMenuTestleri` (7 → 8 yol) | Birincisi harness zorunluluğu (ProAlan servis ister); ikincisi **6-A devir maddesi (a)** “ekleme testle kanıtlanır” diyor — iddia **güçlendi** (`Icerir("/bildirimler")` eklendi) |
| S6 | Tek dokunuş (MÜHÜR 2 DoD/2) | **9 sayfa** açıldı: grup 4'ün 6'sı + `Giris.razor` + `Bildirimler.razor` + kabuk dışı `BasitMenu.cs` | `Giris.razor`: 45-UI'nin “Giriş sayfasında ikinci adım” maddesi bu pakete verildi (ikinci dokunuş — gerekçe kapı kaydına). `Bildirimler.razor`: 6-A devir (b) üç satır içi `style` → sınıf (görsel-dışı, DOM aynı) |
| S7 | 45-API devir (b) “uç yolları sapmış, tek ad kümesi seçilsin” | **Ad değişikliği YAPILMADI** — koddaki yollar zaten belgeyle aynı: `/auth/mfa/{durum,kur,dogrula,kurtarma-yenile,kaldir}` ve `/auth/giris` → `/auth/mfa/giris`. Sapma **yollarda değil, JETONUN TAŞINDIĞI YERDE** çıktı (§ 5) | Doğrulayıcının “baslat/kapat/giris-mfa” iddiası bugünkü koda karşı geçersiz; gerçek sapma canlı doğrulamada bulundu ve kapatıldı |
| S8 | `denetim.sh` `BEKLENEN_VARLIK` 15 → 17 talebi | **40** yapıldı | Gerçek sayı ölçüldü (`Entities.cs` 17 + ayrı dosyalar 23). 17'de bırakmak sayacı yine anlamsız kılardı (karşılaştırma `-ge`). MFA varlıkları (`KullaniciMfa`, `MfaKurtarmaKodu`) bu sayıya dâhil. Dosyaya “yeni varlık eklenince bu sayı da artırılır” notu yazıldı |

---

## 4. UX-1-B boşluğu (fail-closed, sunucuya YAZILMADI)

| Boşluk | Ekranda ne yapıldı | Hedef |
|---|---|---|
| **Şube aktifleştirme ucu yok** — `SubeOlusturDto`'da `Aktif` alanı yok, `DELETE /subeler/{id}` yalnız `aktif=false` yazar | `#degismez-ipucu-PasifSube`: gerçek gerekçe + alternatif yol. **Ölü/disabled düğme konmadı** (DegismezIpucu hiç `<button>` üretmez) | **UX-1-D** ya da ayrı oy. `PAKET-UX-1-B-uygulama-raporu.md` § açık kalanlar bu ucu listelemiyor — boşluk orada doğdu |

Koşul gereği sunucuya yeni uç **yazılmadı**.

---

## 5. Canlı doğrulamada YAKALANAN GERÇEK HATA (Madde 1)

**45-UI girişin ikinci adımı çalışmıyordu.** İlk yazımda meydan okuma jetonu istek
**gövdesine** konmuştu (`{mfaToken, kod}`). Canlıda:

```
POST /auth/mfa/giris  {"mfaToken":"…","kod":"…"}   → HTTP 401
{"message":"Doğrulama oturumu geçersiz ya da süresi doldu, yeniden giriş yapın"}
```

Kök neden: `AuthController.GirisMfa` jetonu **`Authorization: Bearer` başlığından** okur
(`MfaGirisDto` yalnız `Kod` taşır). Gövdeye konan jeton sunucuya hiç ulaşmıyor, sonuç:
**MFA'sı açık hiçbir kullanıcı giriş yapamaz** — sessiz, ekranda “kod hatalı” gibi görünen bir kilit.

Düzeltme (sunucu **değiştirilmedi**, istemci sözleşmeye uyduruldu):
`ApiIstemci.PostJetonlaAsync<T>(yol, jeton, govde)` — açık Bearer başlığı, **401 yenileme
akışına girmez** (ortada yenilenecek oturum yok; 401 çağırana döner, kullanıcı sessizce
`/giris`'e atılmaz). `Giris.razor` bunu kullanır.

Kanıt: canlı adım 20 (aşağıda) **HTTP 200 + accessToken**; test `C4_60` artık
“gövdede `mfaToken` **YOK**” + “`Authorization: Bearer meydan-okuma-jetonu` **VAR**” diye
sözleşmeyi zorluyor (eski hâli yanlış bir sözleşmeyi doğruluyordu — S-2 dersinin aynısı).

---

## 6. 45-API hasım doğrulayıcı devir maddeleri

| Madde | Yapılan | Kanıt |
|---|---|---|
| **(a) ORTA — MFA kapatma başarısız olsa bile kurtarma kodu tüketiliyor** | `MfaServisi.Kaldir` artık **parolayı ÖNCE** doğruluyor; parola yanlışken kod **hiç çözümlenmiyor** (kod doğrulaması yan etkilidir, kodu tüketir). Tüketim ile kapatma **aynı `SaveChanges`**'te | Test `Kapatma_basarisizsa_kurtarma_kodu_HALA_gecerlidir` · mutasyon **M-C4-5** · canlı adım 13-15 (401 → kalan **10** → aynı kodla 200) |
| **(b) ORTA — uç adları sapması** | Yollar zaten belgeyle aynı (§ 3/S7); **ad değişikliği yapılmadı**. Gerçek sapma jeton taşıma yeriydi ve kapatıldı (§ 5) | Canlı adım 19-20 |
| **(c) DÜŞÜK — `mfa-kurtarma-kullanildi` denetim olayı yok** | `MfaKodTuru` (Yok/Totp/Kurtarma) + `KodCozumle` eklendi (`KodDogrula` **aynen duruyor**, mevcut testler bozulmadı). Kurtarma kodu kullanıldığında `Hassas=true` kayıt yazılır; detayda **kod YOK**, kalan adet var. Dört çağıranın hepsinde: kurulum doğrulama, kurtarma yenileme, kapatma ve **girişin ikinci adımı** | Testler `Kurtarma_kodu_kullanilinca_denetim_kaydi_yazilir_kod_YAZILMAZ`, `TOTP_ile_kapatmada_kurtarma_olayi_YAZILMAZ`, `KodCozumle_TOTP_ve_kurtarmayi_AYIRT_eder` · mutasyonlar **M-C4-6**, **M-C4-7** · canlı adım 16-17 (SQL) |
| **(d) DÜŞÜK — `denetim.sh` BEKLENEN_VARLIK sıkılaştırması** | 20 → **40** (gerçek sayı), MFA varlıkları dâhil, “yeni varlık eklenince güncellenir” notuyla | `denetim.sh` çıktısı § 9 |

⚠ **Kalan adet sayımı:** `CountAsync` kullanılmıyor — tüketim henüz kaydedilmediği için
sunucu tarafı sayım eski hâli görürdü (kalan 1 fazla yazılırdı). Satırlar **takipli** çekilip
bellekte sayılıyor. Canlı kanıt: denetim kaydı `kalan kurtarma kodu: 9` (doğru).

---

## 7. 6-A hasım doğrulayıcı devir maddeleri

| Madde | Yapılan | Kanıt |
|---|---|---|
| (a) `/bildirimler` basit mod menüsünde yok | `BasitMenu.Yollar`'a eklendi (7 → 8) | `BasitModMenuTestleri` güncellendi (iddia güçlendi) · mutasyon **M-C4-10** |
| (b) `Bildirimler.razor` üç satır içi `style` | `class="sayi"` (tabular-nums + sağa yasla) | Dosyada satır içi `style` sayısı **0** |
| (c) `denetim.sh` varlık sayacı anlamsız | § 6/(d) ile birleşti → **40** | `denetim.sh` TEMİZ |

---

## 8. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Tam süit (**önce**, `FullyQualifiedName!~UX1C4`) | 1805 | 1804 | 1 | `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_…` — **KARARSIZ**: tek başına 3/3 yeşil, tam süitte yeşil; yalnız o filtreli koşumda kırmızı. Benim değişikliklerimle ilgisi yok (POS ekranı bu pakette AÇILMADI) |
| Tam süit (**sonra**) | **1839** | **1839** | **0** | trx: `scratchpad/trx-pUX-1-C4-son/_Ramazan-MacBook-Air_2026-09-17_12_01_42.trx` |
| Yeni: `UX1C4EkranTestleri` (bUnit) | 29 | 29 | 0 | 6 sayfa başı + düzenleme yolları + DegismezIpucu + kilit sebepleri + 45-UI |
| Yeni: `UX1C4MfaDevirTestleri` (DB) | 5 | 5 | 0 | 45-API devir (a)(c) + `KodCozumle` + liste `mfaEtkin` |

**Yeni test sayısı: 34.**
⚠ C3 raporundaki taban (1798) ile benim ölçtüğüm taban (1805) arasında 7 test fark var;
bu fark benim değişikliklerimden **önce** ağaçta vardı, ölçümü olduğu gibi yazıyorum.

Çekmece Esc/odak sözleşmesi ayrıca sınanmadı — `Cekmece` bileşeninin kendi testi
(`KabukBilesenTestleri`, UX-2-A2) bunu zaten bir kez zorluyor; K6 “bileşen testinde bir kez” diyor.

---

## 9. Mutasyon kanıtları

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|---|
| M-C4-1 | Isletme.razor | `SayfaBasi Anahtar="isletme"` → `"isletme-BOZUK"` | `C4_04_Isletme_sayfa_basi_vardir` | CMP-OK |
| M-C4-2 | Isletme.razor | Kimlik PATCH gövdesinden `vergiNo` çıkarıldı | `C4_10_Isletme_kimligi_cekmeceden_PATCH_edilir` | CMP-OK |
| M-C4-3 | Isletme.razor | `DegismezIpucu Varlik="PasifSube"` → `"PasifSubeBOZUK"` | `C4_13_Pasif_subede_dugme_YOK_gerekce_VAR` | CMP-OK |
| M-C4-4 | Yetkiler.razor | `Kirli => true` (kirli durum takibi kaldırıldı) | `C4_30`, `C4_31` | CMP-OK |
| M-C4-5 | MfaModulu.cs | `Kaldir`: parola kapısı kaldırıldı, kod **her hâlükârda** çözümlenir (devir (a) geri alma) | `Kapatma_basarisizsa_kurtarma_kodu_HALA_gecerlidir` | CMP-OK |
| M-C4-6 | MfaModulu.cs | `Kaldir`'dan `KurtarmaKullanimiIzle` çağrısı silindi | `Kurtarma_kodu_kullanilinca_denetim_kaydi_yazilir_kod_YAZILMAZ` | CMP-OK |
| M-C4-7 | MfaModulu.cs | Kalan sayımındaki `.Count(k => !k.Kullanildi)` → `.Count()` (kaydedilmemiş tüketim gözden kaçar) | `Kurtarma_kodu_kullanilinca_denetim_kaydi_yazilir_kod_YAZILMAZ` | CMP-OK |
| M-C4-8 | Ayarlar.razor | MFA anahtarı maskesiz basılır (`GizMaske` kaldırıldı) | `C4_53_MFA_kurulumunda_giz_MASKELI_gosterilir` | CMP-OK |
| M-C4-9 | Ayarlar.razor | `#mfa-tamamla` `disabled="@(false)"` (kurtarma onayı zorunluluğu kalkar) | `C4_54_Kurtarma_kodlari_onaylanmadan_Tamamla_KAPALI` | CMP-OK |
| M-C4-10 | BasitMenu.cs | `/bildirimler` listeden çıkarıldı | `BasitMenu_listesi_TEK_yerdedir_ve_yedi_yol_icerir` | CMP-OK |
| M-C4-11 | KullaniciModulu.cs | Listede `mfaEtkin` bayrağı gönderilmez | `Kullanici_listesi_mfaEtkin_bayragini_dondurur` | CMP-OK |
| M-C4-12 | VeriDisaAktarim.razor | `GerekceKisa => false` (alan altı mesaj kaybolur) | `C4_40_Kisa_gerekcede_sayac_ve_alan_alti_mesaj_cikar` | CMP-OK |
| M-C4-13 | Giris.razor | `mfaGerekli` dalı devre dışı (ikinci adıma geçilmez) | `C4_60_MFA_gerekliyse_giris_IKINCI_ADIMA_gecer` | CMP-OK |
| M-C4-14 | Ayarlar.razor | Profil PATCH gövdesinden `email` çıkarıldı | `C4_50_Profil_cekmeceden_PATCH_edilir` | CMP-OK |
| M-C4-15 | Kullanicilar.razor | `YeniEpostaHatali => false` (e-posta mesajı kaybolur) | `C4_21_Yeni_kullanici_gecersiz_epostada_alan_alti_mesaj_verir` | CMP-OK |

**15/15 kırmızı.** Test açığı bulunmadı. Her mutasyondan sonra kaynak yedeğinden geri
kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı. Son mutasyondan sonra
`dotnet build Kuyumcu.sln --nologo` → **0 uyarı / 0 hata**.

---

## 10. Denetim çıktısı (`dotnet/denetim.sh`)

```
  ✓ IKiraciVarlik uygulayan varlık                40          ← 20'den GERÇEK sayıya çekildi
  ✓ Pages içinde istemci record tanımı          122 (tavan 122)
  ✓ app.css ham hex (kâğıt dâhil kaba sayım)     25 (tavan 25)
  ✓ Pages+Bilesenler+Layout satır içi style=    120 (tavan 196)   ← 126'dan DÜŞTÜ
  ✓ Pages: @onclick'siz ve disabled'sız <button> 57 (tavan 71)     ← 71'den DÜŞTÜ
  ✓ Pages: ModalPencere kullanımı                43 (tavan 45)     ← 44'ten DÜŞTÜ
  ✓ Pages: duzenlenenId kalıbı                    1 (tavan 1)
  ✓ Pages/*.razor dosya sayısı                   30 (tavan 30)     ← yeni sayfa açılmadı
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)     0
DENETİM TEMİZ.
```

Hiçbir sayaç **artmadı**; dördü düştü.

---

## 11. Canlı doğrulama (5198) — HTTP + SQL

Kiracı `PAKETUX1C4-TEST Kuyumculuk` (`POST /auth/kayit`), iş bitince 39 tablodan silindi.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /admin/tenant` | kimlik alanları döner | **200** — `adres/vergiDairesi/vergiNo/telefon/kurKaynagi/maliyetYontemi` alanları yanıtta **var** (ekranın okuduğu şekil doğrulandı) |
| 2 | `PATCH /admin/tenant/kimlik` (ad+adres+telefon+vd+vno) | 200, alanlar yazılır | **200** — `ad="… A.Ş."`, `adres="Kapalıçarşı No:1 Fatih"`, `vergiDairesi="Fatih"`, `vergiNo="1234567890"` |
| 3 | `PATCH …/kimlik {"ad":"  "}` | **400** | **400** `"İşletme adı boş bırakılamaz"` |
| 4 | `PATCH …/kimlik {"vergiNo":"12345"}` | **400** | **400** `"Vergi no 10 ya da 11 hane olmalıdır"` (alan bazlı) |
| 5 | `PATCH …/kimlik {"telefon":…}` tek alan | diğerleri **KORUNUR** | **200** — adres/vd/vno aynen duruyor (üç durumlu PATCH sözleşmesi) |
| 6 | `POST /subeler {"telefon":"abc"}` | **400** | **400** `"Telefon yalnız rakam, boşluk, parantez ve tire içerebilir…"` |
| 7 | `POST /subeler` geçerli telefon | 200 | **200** — şube oluştu |
| 8 | `GET /kullanicilar` | satırda `mfaEtkin` | **200** — `"mfaEtkin":false` (45-UI sütununun kaynağı) |
| 9 | `POST /auth/mfa/kur` | otpauth + giz | **200** — `otpauthUri` var, `hane=6`, `adimSaniye=30` (giz ekrana **basılmadı**) |
| 10 | `POST /auth/mfa/dogrula` (**bağımsız RFC 6238 uygulamasıyla üretilen kod**) | etkin + 10 kurtarma kodu | **200** — `etkin=true`, 10 kod, `kalan=10` |
| 11 | `GET /auth/mfa/durum` | giz **DÖNMEZ** | **200** — `{etkin,durum,yontem,kalanKurtarmaKodu,etkinlesti,kilitli}`, giz yok |
| 12 | `GET /kullanicilar` | `mfaEtkin=true` | **200** — `true` |
| 13 | **devir (a)** yanlış parola + doğru kurtarma kodu → kapatma | **401**, kod TÜKENMEZ | **401** `"Şifre veya doğrulama kodu hatalı"` |
| 14 | `GET /auth/mfa/durum` | `kalanKurtarmaKodu` **hâlâ 10** | **10** ✔ (eski davranışta 9 olurdu) |
| 15 | doğru parola + **aynı** kurtarma kodu | **200** | **200** `{"etkin":false}` — kod gerçekten yanmamış |
| 16 | SQL `denetim_kayitlari` | `mfa-kurtarma-kullanildi` Hassas=1 | `mfa-kuruldu(1)`, `mfa-etkinlesti(1)`, `mfa-kapatma-reddedildi(1)`, **`mfa-kurtarma-kullanildi · kalan kurtarma kodu: 9`(1)**, `mfa-kapatildi(1)` |
| 17 | SQL: kurtarma kodu **değeri** denetim izinde | **0** | **0** ✔ |
| 18 | SQL `tenants` kimlik kolonları | dolu | `ad / adres / vergi_dairesi=Fatih / vergi_no=1234567890 / telefon` ✔ |
| 19 | `POST /auth/giris` (MFA açıkken) | jeton **YOK**, `mfaGerekli` | **200** `{mfaGerekli:true, mfaToken:…, gecerlilikDakika:5}` |
| 20 | `POST /auth/mfa/giris` — jeton **Authorization başlığında** | 200 + accessToken | **200**, `accessToken` geldi, `kullanici.email` doğru — § 5'teki hatanın kapanış kanıtı |

**SQL temizlik kanıtı:**
```
tenants=8 · kalan_kullanici=0 · kalan_denetim=0 · kalan_kiraci(PAKETUX1C4%)=0 · QA-TEST fikstürleri=3 (DOKUNULMADI)
lsof -nP -iTCP:5198 -sTCP:LISTEN → 0 satır (sunucu durdu)
```

**Tarayıcı turu (375/768/1280 px, `scrollWidth ≤ clientWidth`) QA ajanınca yapılacaktır** —
bu paket başsız çalıştı; yukarıdaki HTTP/DB kanıtı uygulayıcının verdiği kanıttır.

---

## 12. Değişen dosyalar

*(Ağaç hiç commit'lenmediği için `git status --porcelain` her şeyi listeliyor; aşağıdaki liste
BU PAKETTE elle değiştirilenlerdir.)*

**Sunucu**
- `dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs` — `MfaKodTuru`, `KodCozumle`, `Kaldir` sırası, `KurtarmaKullanimiIzle`
- `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` — `GirisMfa`'da kurtarma kodu denetim olayı
- `dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs` — liste `mfaEtkin` bayrağı

**Ortak**
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukYonetim` (TR+EN, ~100 anahtar)
- `dotnet/denetim.sh` — `BEKLENEN_VARLIK` 20 → 40 + not

**İstemci**
- `dotnet/Kuyumcu.Web/Pages/Isletme.razor` · `Kullanicilar.razor` · `Yetkiler.razor` · `Ayarlar.razor` · `VeriDisaAktarim.razor` · `Kilavuz.razor`
- `dotnet/Kuyumcu.Web/Pages/Giris.razor` (45-UI ikinci adım — ikinci dokunuş)
- `dotnet/Kuyumcu.Web/Pages/Bildirimler.razor` (6-A devir (b): satır içi style)
- `dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs` (6-A devir (a))
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs` (`PostJetonlaAsync`)
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css` (`.ortala`, `.saga-yasla`, `.esit-pay`, `tr.satir-vurgu`, `.kimlik-listesi`, `.ayarlar-sutun`, `.bayrak`, `.uyari-metni` — hepsi belirteç, sabit renk yok)

**Testler**
- `dotnet/Kuyumcu.Tests/UX1C4EkranTestleri.cs` (**yeni**, 29 test)
- `dotnet/Kuyumcu.Tests/UX1C4MfaDevirTestleri.cs` (**yeni**, 5 test)
- `dotnet/Kuyumcu.Tests/IsletmeEkranTestleri.cs` (yalnız DI kaydı)
- `dotnet/Kuyumcu.Tests/BasitModMenuTestleri.cs` (6-A devir (a), iddia güçlendi)

### id envanteri diff (K4)
**Silinen/değişen id: 0.** Eklenen id'ler (§ 1 tablosundaki tüm `#…` değerleri) —
`#sayfa-basi-{veri-disa-aktarim,kullanicilar,yetkiler,isletme,ayarlar,kilavuz}` (+ `-baslik/-aciklama/-eylem`),
`#ikimlik-*`, `#cekmece-{isletme-kimlik,sube,profil}*`, `#i-kimlik-duzenle`, `#i-kur-kaynagi`,
`#i-maliyet-yontemi`, `#s-yeni-sube-ac`, `#s-pasife-*`, `#s-ad-hata`, `#s-telefon-hata`, `#s-hata`,
`#k-yeni`, `#k-siz`, `#k-eposta-hata`, `#k-ad-hata`, `#k-kaydet(-kilit)`, `#k-hata`, `#k-pasife-onayla`,
`#d-kaydet(-kilit)`, `#d-ad-hata`, `#d-eposta-hata`, `#d-hata`, `#s-kaydet`,
`#y-kaydet(-kilit)`, `#y-kirli`, `#da-*`, `#ay-*`, `#mfa-*`, `#giris-mfa-*`,
`#degismez-ipucu-{PasifSube,KiraciAyari,KendiRolum,SahipYetkisi}`.
Mevcut id'ler (`#s-ad`, `#s-adres`, `#s-telefon`, `#k-ad`, `#k-soyad`, `#k-eposta`, `#d-ad`,
`#d-soyad`, `#d-eposta`, `#d-rol`, `#d-durum`, `#kl-yeni-sekme`, `#kl-bolum-*`, `#arayuz-*`,
`#i-plan`, `#giris-eposta`, `#giris-sifre` …) **aynen korundu**.

---

## 13. Koşul karşılama tablosu

| Koşul (kaynak) | Durum | Kanıt |
|---|---|---|
| (1) Her sayfada `SayfaBasi` | ✅ | 6 sayfa; testler `C4_01…C4_06`; mutasyon M-C4-1 |
| (2) Her veri girişi için düzenleme yolu | ✅ | § 1 tablosu; Çekmece ×3, mevcut diyaloglar, satır eylemleri |
| (2b) Storno düğmesi | **yok — kapsam dışı** | Grup 4'te defter kaydı listelenmiyor (§ 0) |
| (3) Pasife al / aktifleştir | ✅ / kısmen | Şube pasife al ✔, kullanıcı pasif/aktif ✔; **pasif şube aktifleştirme** → UX-1-B boşluğu (§ 4) |
| (4) Düzenlenemeyen alanlarda DegismezIpucu + “neden” | ✅ | 4 adet; testler `C4_13/14/22/32`; `GerekceDurust` iç paket/dalga/ADR kodu sızdırmadığını zorluyor |
| (5) Hata yutma kalıbı | ✅ | UX-4c/UX-4d yaptı; `MfaDurumYukle` catch'i **listeyi boşaltmaz**, `HataBandi`'na yazar (UX-S28) |
| (6) ProAlan ile basit modda katlama (form ≤ 8) | ✅ | İşletme kimliği; gövdeye de konmaz (UX-S5) |
| K4 id sözleşmesi | ✅ | Silinen/değişen id 0 (§ 12) |
| K6 Çekmece sözleşmesi | ✅ | `#cekmece-{varlik}` + `role=dialog aria-modal aria-labelledby` + `#cekmece-kapat` + `-govde/-kaydet/-iptal`; Esc/odak `Cekmece` bileşeninde |
| K7 bileşen bütçesi | ✅ (S3 hariç) | Yalnız SayfaBasi + Cekmece + DegismezIpucu + ProAlan + mevcut bantlar; `SatirIciDuzenle`/`GerekceAlani` bu grupta gerekmedi (tek alanlı düzeltme yok) |
| İyimser kaydetme YOK | ✅ | Çekmeceler sunucu 200'den sonra kapanır; Yetkiler “kirli” damgası PUT 200'den sonra kalkar (test `C4_31`) |
| Satır içi `style=""` yazılmaz | ✅ | 7 sayfada **0**; genel sayaç 126 → 120 |
| Sabit renk yok, `var()` | ✅ | Yeni CSS'in tamamı belirteçli (`--amber-50`, `--uyari`, `--bosluk-2`) |
| `.sayi` tabular-nums | ✅ | `Bildirimler` th'leri + `#da-gerekce-sayac` |
| Ekran başına tek birincil eylem | ✅ | `SayfaBasi.Eylem` yuvası |
| DoD/1 her düğme istek atar ya da disabled+sebep | ✅ | denetim.sh “ölü düğme” 71 → 57; her disabled'ın yanında `-kilit` metni |
| DoD/2 tek düzenleme kalıbı | ✅ | ModalPencere 44 → 43; yeni `duzenlenenId` kalıbı açılmadı (sayaç 1) |
| DoD/3 boş/geçersizde alan altı Türkçe mesaj | ✅ | 8 yeni `#…-hata` alanı |
| DoD/4 yıkıcı eylem onay modalı + geri dönüş | ✅ | Şube/kullanıcı pasife alma onaylı; MFA kapatma onaylı + geri dönüş “yeniden kur” |
| MÜHÜR 2 DoD/2 tek dokunuş | ⚠ gerekçeli | § 3/S6 — `Giris.razor` ve `Bildirimler.razor` ikinci dokunuş, gerekçesi yazılı |
| MÜHÜR 5 KOŞUL 3 record dondurma | ✅ | Pages record sayısı **122 = tavan**; yeni istemci kayıt tipi açılmadı, mevcutları sunucu şekline hizalandı (`TenantVeri`, `KullaniciVeri`, `AuthYaniti`) |
| Sozlukler TR+EN, çıplak metin yok | ✅ | `SozlukYonetim` iki dilde; ekranda sabit dize yok |
| Sır/giz ekrana-loga yazılmaz | ✅ | Giz varsayılan maskeli (mutasyon M-C4-8), `durum` ucu giz döndürmez, denetim izinde kod 0 kez (canlı 17) |
| Migration prosedürü | **gerekmedi** | Şema değişikliği yok; `dotnet ef` çalıştırılmadı |
| Test kiracısı silindi | ✅ | tenants 8 → 8; QA-TEST-1/2/3 fikstürlerine dokunulmadı |

---

## 14. Açık kalanlar

1. **Pasif şubeyi aktife alma ucu** (UX-1-B boşluğu) — ekranda gerekçeli, sunucuda yok. → **UX-1-D** ya da ayrı oy.
2. **375 px yatay kaydırma** — grup 4'ün 6 rotasında da var, kök neden kabuk. → **UX-2-A2**.
3. **/yetkiler matrisi dar kapsayıcıya sığmıyor** (masaüstünde bile) — akordeon/yapışkan sütun kararı. → **UX-2-A2 / UX-3-B**.
4. **45-UI'nin gerçek doğrulayıcı uygulamayla (Google/Microsoft Authenticator) ekran görüntülü kanıtı ALINAMADI** — oturum başsız. Yerine **bağımsız RFC 6238 uygulamasıyla** uçtan uca kurulum + iki adımlı giriş canlıda kanıtlandı (adım 9-12, 19-20). 45-API raporunda da aynı sınır vardı; ekran görüntüsü kanıtı **QA tarayıcı turuna** kalıyor.
5. **45-API açık kalanları hâlâ açık** (bu paketin kapsamı dışı): SAHİP kilitlenme runbook'u (DevOps) — **Faz A canlıya alınamaz**; anahtar rotasyon planına “MFA gizlerinin yeniden şifrelenmesi” maddesi; Faz B yönetici mfa-sıfırlama ucu.
6. **`UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_…` kararsız** — tek başına ve tam süitte yeşil, bir filtreli koşumda kırmızı. Gizlenmiyor; C1'in eline/kapı nöbetine bildiriliyor.
7. **Ağaçta `.mutorj` artıkları duruyor** (`UrunModulu.cs.mutorj`, `SatisModulu.cs.mutorj`, `Islemler.razor.mutorj` … 10 dosya). Benim bıraktığım değil; silme hook tarafından bloklanıyor → **kurucu kalemi 4**.
8. **Kurtarma kodları düz metin `.txt` olarak indiriliyor** — şifreli/parola korumalı indirme tercih edilirse ayrı karar gerekir.

---

## 15. Kurucuya / mali müşavire notlar

- **Kurucuya (güvenlik, acil değil ama yayından önce):** iki aşamalı giriş artık ekrandan
  kurulabiliyor. **Kurtarma kodlarını kaybeden ve telefonunu değiştiren SAHİP hesabı
  sistemden kilitlenir** — yönetici sıfırlama ucu yoktur (45-API Faz B). DevOps runbook'u
  yazılmadan üretim yayını yapılmamalıdır (45-API açık kalan 1).
- **Kurucuya (kişisel veri):** `/kullanicilar` listesi artık her çalışanın iki aşamalı
  girişinin açık/kapalı olduğunu gösteriyor. Yalnız **bayrak**tır; giz, kurtarma kodu
  sayısı ya da kilit bilgisi paylaşılmaz ve liste yalnız SAHİP/MÜDÜR'e açıktır.
- **Kurucuya (veri dışa aktarım):** kişisel veri içeren kapsamlar hâlâ **açılışta seçili**
  geliyor; artık ekranda uyarı var. “Varsayılan seçimsiz” istenirse tek satırlık değişiklik
  + 4 mevcut testin güncellenmesi gerekir (§ 3/S1) — ekip oyuna açık.
- **Mali müşavire:** işletme kimlik alanları (**unvan · adres · vergi dairesi · vergi no**)
  artık ekrandan düzeltilebiliyor ve fatura/gider pusulasında **satıcı kimliği** olarak
  basılıyor. Bu alanların **geriye dönük** değiştirilmesi, daha önce basılmış belgelerle
  bugünkü kaydın farklı görünmesine yol açar. Her değişiklik denetim izine (hangi alanların
  değiştiği) yazılıyor; **belge yeniden basımında hangi tarihteki kimliğin kullanılacağı**
  mali müşavir teyidi ister. Bugünkü davranış: **güncel kimlik basılır**.
- **Mali müşavire:** kur kaynağı ve maliyet yöntemi (FIFO/LIFO/ORTALAMA) ekrandan
  **bilerek** değiştirilemez bırakıldı — geçmiş maliyet/kâr hesaplarının dayanağıdır.
  Değişmesi gerekirse geçmişin nasıl ele alınacağı ayrı bir karardır.
