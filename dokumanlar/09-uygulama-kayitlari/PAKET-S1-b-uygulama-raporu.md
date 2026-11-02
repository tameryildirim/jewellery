# PAKET S1-b — Kenar çubuğu: 6 grup katlanır + ekran adı önizlemesi + "Tüm ekranlar" bağlantısı

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** S-1 KABUK
**Durum beyanı:** BİTTİ (paket kapısının 7 maddesi aşağıda kanıtlı; ağaçtaki 2 kırmızı başka şeritlerin — § 9)

> Bu oturum, yarıda kalan önceki S1-b koşumunun (23.09 18:25–18:44, oturum kapanınca kesildi) ağaçta
> bıraktığı işi **devraldı**: önce derlendi, testleri koşuldu, tuvale karşı okundu; eksikler bu oturumda
> tamamlandı. Aşağıda "devralınan" ve "bu oturumda" ayrı yazılıdır.

---

## 1. Kaynaklar ve spesifikasyon doğrulaması

- Tuval (TEK doğruluk kaynağı): `dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html` `<nav>` (satır 637–9522 bayt aralığı okundu) + `canvas.json` notları n2, n6.
- Plan: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3 (bağlayıcı sınırlar), § 5A, § S-1 md. 1.1.
- "Mevcut durum" iddiaları koda karşı: `KenarCubugu.razor` P-37 hâli (düz gruplar, favoriler, sürükle-bırak, Alt+↑/↓) doğrulandı; `MenuTanimi.GrupSirasi` 6 grup, `Ogeler` 36 bağlantı + `AltBlok` 2 (Kılavuz, Ayarlar).
- Mevzuat hükmü **yok** (arayüz paketi) → mevzuat kuralı tetiklenmedi.

## 2. Yapılanlar

### 2.1 Devralınan (önceki S1-b koşumu, ağaçta hazır bulundu — bu oturumda derlenip test edilip kabul edildi)
| Dosya | İş |
|---|---|
| `Kuyumcu.Web/Layout/KenarCubugu.razor` | Grup başlığı `<h2>` içinde `<button aria-expanded>`; kapalıyken altında **ekran adı önizlemesi** (`#kenar-onizleme-<slug>`, "Müşteriler · Ürünler · …"); satırlar DOM'dan silinmez, `hidden` ile katlanır; basit modda başlık çizilmez (7 satır düz); varsayılan = yalnız aktif rotanın grubu açık; "Tüm ekranlar (N)" düğmesi aramanın hemen altında; kullanıcı kartı (baş harf rozeti + ad + rol) en altta; ok düğmeleri yok (P-37), sıralama sürükle-bırak + Alt+↑/↓ korunur |
| `Kuyumcu.Web/Servisler/MenuTercihServisi.cs` | `menu-acik-gruplar` tercihi: `GrupAcik`, `GrupKatlamaDegistirAsync` (hata → geri al), `KatlamayiSifirlaAsync` |
| `Kuyumcu.Api/Modules/Tercih/TercihModulu.cs` | Beyaz listeye `menu-acik-gruplar` (migration YOK — `kullanici_tercihleri` anahtar→JSON deposu) |
| `Kuyumcu.Core/I18n/MenuSozlugu.cs` (YENİ) | Kenar çubuğu metinleri TR+EN (Sozlukler.cs ortak hub'ına dokunulmadı) |
| `Kuyumcu.Web/Bilesenler/Ikonlar.cs`, `wwwroot/img/ikon.svg` | `TumEkranlar`, `KatlamaAcik/Kapali` ikonları |
| `wwwroot/font/` (YENİ) | **Manrope + Sora VENDORED** (S1-a'dan devir 1): 4 woff2 (değişken, latin + latin-ext) + `OFL-Manrope.txt`, `OFL-Sora.txt` |
| `wwwroot/css/belirtecler.css` | `@font-face` ×4 (`/font/...`, `font-display: swap`), `--yazi-ailesi` Manrope, `--yazi-marka` Sora; **karar D-S1b-1** (şema↔durum rengi, S1-a'dan devir 2) |
| `wwwroot/css/app.css` | Sonda tek "S1-b" bloğu (geri dönüş = bloğu kaldır) |
| Testler | `MenuKatlamaTestleri.cs` (17), `TercihKatlamaHttpTestleri.cs` (6), `YaziTipiVeSemaRengiTestleri.cs` (11), `MenuMimarisiTestleri.cs` güncellemesi |

### 2.2 Bu oturumda eklenen / düzeltilen
| # | Bulgu (tuval/kural karşısında) | Düzeltme |
|---|---|---|
| 1 | Marka satırı elle **"Kuyumcu"** yazıyordu (tuval: "ByGatech"; P-D2 tek kaynağı `Marka.Ad`) | `@Marka.Ad` + `id="kenar-marka"`; Sora 21 px (tuval) |
| 2 | Kullanıcı kartı **ham rol kodu** basıyordu ("SAHIP") — UX-S31 ihlali; tuval "Sahip" | `RolEtiket()` sözlükten çevirir (`Dil.T.Kullanici.Rol*`); tanınmayan kod **tahmin edilmez**, boş kalır; `id="kenar-kullanici-rol"` |
| 3 | "Tüm ekranlar" kilit sebebi kullanıcıya **iç paket kodu** söylüyordu ("… (S1-d) …") — plan § 5B "jargon sıfır" | Metin: "Tüm ekranlar sayfası henüz hazır değil. O güne kadar her ekran aşağıda kendi grubunun altında duruyor — hiçbiri gizlenmedi." (TR+EN) |
| 4 | Katlama yazılamayınca bant **"sıra kaydedilemedi"** diyordu (yanlış bilgi); `MenuSozlugu.KatlamaKaydedilemedi` hiç kullanılmıyordu | `MenuTercihServisi.HataAnahtari`; bant hangi tercihin kaydedilemediğini söyler (id `#kenar-sira-hatasi` aynı) |
| 5 | `KatlamayiSifirlaAsync` (DELETE yolu) **ekranda hiçbir düğmeye bağlı değildi** — "sunucuda uç var, ekranda düğme yok" (UX-S25 / Sözleşme-DoD) | `#kenar-katlama-sifirla` "Grupları sıfırla": yalnız kayıtlı katlama tercihi varken çizilir (silinecek şey yoksa etkisiz düğme olmaz — DoD EK md.1), `DELETE /tercihler/benim/menu-acik-gruplar` atar |
| 6 | Tuvalde açık grubun başlığı küçük/BÜYÜK HARF/soluk ("GÜNLÜK İŞ ▾"), kapalı grubun başlığı 17 px kalın; kodda ikisi aynıydı. Menü genişliği tuvalde **268 px**, kodda 240 px | `app.css` S1-b bloğuna 3 kural (belirteçli, sabit renk yok) |
| 7 | `belirtecler.css` D-S1b-1 notu "ekip oyu — Bertan/Ertuğ/Aşkın/Mehmet" diyordu; bu oya ait **tutanak yok** | Not dürüstleştirildi: "S1-b uygulayıcısı, orkestratör talimatıyla; ayrı oy tutanağı YOKTUR — hasım doğrulayıcı kapısına tabidir" |
| 8 | `AktifGrupSlug`'daki yetki süzgecini hiçbir test korumuyordu (mutasyon M1-13 hayatta kaldı) | Test eklendi: `Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ` |

## 3. S1-a'dan devredilen iki iş

### 3.1 Manrope + Sora vendored (karar D9)
- Dosyalar `wwwroot/font/` altında: `manrope-latin.woff2` 24.576 B, `manrope-latin-ext.woff2` 15.240 B, `sora-latin.woff2` 25.240 B, `sora-latin-ext.woff2` 12.116 B — dördünün de ilk 4 baytı `774f4632` (= "wOF2"), yani gerçek woff2.
- OFL 1.1 metni yanında (`OFL-Manrope.txt`: "Copyright 2018 The Manrope Project Authors").
- CSP'ye **istisna eklenmedi**: canlı yanıt başlığı `font-src 'self'`. Tarayıcıda (5194) `/giris`: `manrope-latin.woff2 200`, `manrope-latin-ext.woff2 200`, `sora-latin(-ext).woff2 200`, `document.fonts` Manrope+Sora **loaded**, gövde `font-family: Manrope, …` **18px**, konsolda **0 hata** (CSP ihlali yok).
- Not: dosyaları önceki koşum indirdi; bu oturum **indirme yapmadı**, yalnız içeriği/imzayı/lisansı doğruladı.

### 3.2 Şema ↔ durum rengi çakışması — karar D-S1b-1
S1-a'nın ölçtüğü 10 çift (64'ten) için:
- **(a) Başarı/bilgi** çakışması → o şemada komşu hue'ya kaydırıldı. Dayanak **uydurma değil**: kurucunun onaylı tuvali Zümrüt şemasında başarıyı maviye çevirmiş (`Tezgah.dc.html` satır 176: `zumrut: … { basari: '#1d4ed8', basariZemin: '#e7eefc', basariKenar: '#c3d3f5' }` — bu oturumda dosyadan okundu). Zeytin'in başarı ailesi Zümrüt'ünkiyle **birebir aynı** (test kilitli); Platin bilgi → turkuaz, Okyanus bilgi → çivit.
- **(b) Uyarı/tehlike** çakışması → güvenlik uzlaşımı (kırmızı = tehlike, sarı = uyarı) korunur: Fildişi uyarı kehribardan sarıya; **Bordo ↔ tehlike MUAF** (kırmızıyı tehlikeden almak ya da kurucunun bordo vurgusunu değiştirmek ikisi de kabul edilemez). Ayrım biçimle: vurgu = katı dolgu, durum = zemin+kenar+metin bandı + ikon + etiket.
- Kilit: `Vurgu_ile_durum_rengi_AYIRT_EDILEBILIR_tek_muafiyet_bordo_tehlikedir` — muafiyet **tek** ve gerçekten kullanılıyor (3 blok); ikinci sessiz istisna açılırsa kırmızı.
- ⚠ Dürüstlük notu: bu karar için ayrı bir ekip oy tutanağı yoktur; uygulayıcı kararıdır, hasım doğrulayıcı onayına tabidir (belirtecler.css notu buna göre düzeltildi).

## 4. Sapmalar (tuvalden / plandan) ve gerekçeleri

| # | Tuval/plan | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | "Tüm ekranlar (38)" bağlantı | **Kilitli düğme** (`disabled` + sebep title + `aria-describedby`) | Hedef `/ekranlar` S1-d'de açılıyor; bugün bağlantı 404 olurdu, etkisiz düğme de yasak (DoD EK md.1). S1-d `<button disabled>` → `<a href="/ekranlar">` değişimini yapacak |
| S2 | "(38)" | **(36)** SAHİP için; sayı `MenuTanimi`'den ve **yetki+mod süzgecinden** türer | Ürünün gerçek grup bağlantısı 36'dır (tuvalin 38'i örnek veri). Kural: sayı elle yazılmaz. Tezgâhtarda daha az çıkar — doğru olan budur |
| S3 | "ByGatech" | "BYGATECH" (`Marka.Ad`) | Marka tek kaynağı P-D2'nin (DEMO şeridi, `Sozlukler.Marka.cs` dokunulmaz). Yazım farkı kurucu/DEMO kararı |
| S4 | Tuvalde Favoriler yok | Favoriler grubu **korundu** (yalnız yıldızlanınca çizilir) | Plan § 3 md.1 "özellik silinmez" |
| S5 | Tuvalde alt kartta yalnız dişli ikonu | Kılavuz · Ayarlar · Çıkış bağlantıları **korundu** | Özellik silinmez; çıkış yolu kaldırılamaz |
| S6 | Bildirimler rozeti "3" | Yapılmadı | Kapsam dışı (menü öğesi mevcut; sayaç rozeti ayrı iş) |
| S7 | Plan § 5A "240 px" | **268 px** | Görevde "taslak kazanır" hükmü |
| S8 | "Grupları sıfırla" düğmesi tuvalde yok | Eklendi, **yalnız kayıtlı tercih varken** görünür | Sözleşme-DoD: oluşturulan tercih ekrandan silinebilmeli; varsayılan kullanıcı onu hiç görmez |

## 5. Migration
**Gerekmedi.** Katlama tercihi mevcut `kullanici_tercihleri` (anahtar→JSON) tablosunda yeni beyaz liste anahtarı `menu-acik-gruplar` olarak tutulur.

## 6. DoD sözleşmesi (Bertan koşulu)

| Varlık / kayıt | Düzenleme | Silme / sıfırlama | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|
| `kullanici_tercihleri` · `menu-acik-gruplar` (YENİ anahtar) | `PATCH /api/v1/tercihler/benim` (gönderilmeyen anahtar KORUNUR, `null` SİLER — test `PATCH_gonderilmeyen_anahtari_KORUR`, `Katlama_tercihi_SIFIRLANABILIR_null_ve_DELETE`) | `DELETE /api/v1/tercihler/benim/{anahtar}` | `[Authorize]` + `[HerkeseAcik]` gerekçeli (kendi tercihin; kullanıcı kimliği jetondan) — modül kapısı bilinçli yok, aksi hâlde tezgâhtar kendi menüsünü katlayamazdı | `DenetimServisi.YazmaKaydet` (mevcut servis çağrılır; canlıda EKLEME 7 / SILME 1 satırı) | Grup başlığına bas = düzenle; "Grupları sıfırla" = sil |
Finansal varlık **yok**; hard DELETE yalnız arayüz tercihinde (iş verisi değil).

## 7. Testler

### 7.1 Filtreli koşumlar (hepsi `KUYUMCU_TEST_DB=kuyumcu_test_s`)
| Kapsam | Toplam | Başarılı | Başarısız |
|---|---:|---:|---:|
| Devir anı: S1-b + komşu sınıflar (MenuKatlama, TercihKatlamaHttp, YaziTipi, MenuMimarisi, TasarimBelirtec, BasitModMenu, DuzeltmePolitikasi, IceAktarimEkran, Ikon, KabukArama, P37Revizyon, P37bKarsilama, YedekEkran, Tercih*) | 214 | 214 | 0 |
| Bu oturumun değişikliklerinden sonra (MenuKatlama, TercihKatlamaHttp, YaziTipi, MenuMimarisi, TasarimBelirtec, BasitModMenu, P37Revizyon, P37bKarsilama, KabukArama, MarkaAdi, Ikon) | 177 | 177 | 0 |
| Son temiz ikiliyle `MenuKatlamaTestleri` | 25 | 25 | 0 |

Paketin test sınıfları tam süitte: **MenuKatlamaTestleri 25/25 · TercihKatlamaHttpTestleri 6/6 · YaziTipiVeSemaRengiTestleri 11/11 · MenuMimarisiTestleri 18/18 · TasarimBelirtecTestleri 28/28** (trx'ten sayıldı).
Bu oturumda eklenen test vakası: **8** (`Marka_satiri_TEK_KAYNAKTAN_gelir_eski_ad_yazilmaz`, `Kullanici_kartinda_rol_SOZLUKTEN_cevrilir_ham_kod_basilmaz` ×4, `Taninmayan_rol_TAHMIN_EDILMEZ_bos_kalir`, `Katlama_SIFIRLA_dugmesi_yalniz_kayitli_tercih_varken_cikar_ve_DELETE_atar`, `Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ`) + mevcut `Yazma_basarisizsa…` ve `Tum_ekranlar_dugmesi_KILITLI…` güçlendirildi.

### 7.2 Tam süit (paket sonu)
`dotnet test … --logger trx --results-directory scratchpad/trx-pS1-b-2`:
**Toplam 4168 · Geçti 4166 · Başarısız 2** (devir anı: 4077 / 4076 / 1 — sayı düşmedi, arada başka şeritler de test ekledi).

| Kırmızı | Sahibi | Bu paketin ürünü mü? |
|---|---|---|
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | **P-30b (DALGA 7c)** | HAYIR — devir notundaki tek kırmızı, aynı mesaj ("ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı" yok) |
| `AltinPozisyonuTestleri.B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi` (Expected 23, Actual 21) | **S-B şeridi, B-2 (Altın pozisyonu)** | HAYIR — bu test dosyası ve `Modules/Pozisyon/PozisyonModulu.cs` benim koşumum SIRASINDA yazıldı (dosya zaman damgaları t4 günlüğünden yeni; `PozisyonModulu.cs` bir ara derlenemez hâldeydi — CS1056 satır 92). S1-b Pozisyon'a dokunmaz |

**Madde (7) — yeni kırmızı üretmeme:** devir listesi {denetim_sh_…} ; benim ürünüm olabilecek fark = ∅ (ikinci kırmızı eşzamanlı B-2'nin). **Sağlandı.**

⚠ **Tuzak (kayda geçsin):** ilk tam süit koşumunda (`trx-pS1-b`) `Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ` da kırmızıydı. Kök sebep ölçüldü: mutasyon betiğim yedeği `shutil.copy2` / `cp -p` ile geri koyuyordu → dosya **eski mtime** ile döndü → MSBuild artımlı derleme `KenarCubugu.razor`'u "değişmemiş" sayıp **M1-13 mutasyonlu** `Kuyumcu.Web.dll`'i yerinde bıraktı. Kaynak `cmp` ile birebirdi ama ikili değildi. Çözüm: geri konan 4 dosya `touch` + `dotnet build Kuyumcu.sln --no-incremental` → test yeşil, tam süit yeniden koşuldu (yukarıdaki sonuç). **Sonraki mutasyon betiklerine ders:** geri koymadan sonra `touch` ya da `--no-incremental`.

## 8. Mutasyon kanıtları
Yöntem: yedek `scratchpad/mut-yedek/S1-b/<kod>-<dosya>` → tek eşleşmeli değişiklik → derle → filtreli test `-v n` → geri koy → `cmp` birebir. Son mutasyondan sonra temiz derleme (`--no-incremental`, 0 hata).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | Önizleme yalnız ilk ekranı yazar (`Take(1)`) | `Kapali_grubun_onizlemesi_o_gruptaki_TUM_ekran_adlarini_yazar` | AYNI |
| M1-2 | Kapalı grup satırı gizlenmez (`hidden="@(false)"`) | `Kapali_grubun_baglantilari_SILINMEZ_yalnizca_hidden_olur` | AYNI |
| M1-3 | Varsayılan "tüm gruplar açık" (`?? true`) | `Varsayilan_olarak_YALNIZ_aktif_rotanin_grubu_aciktir` + 9 test daha | AYNI |
| M1-4 | **M-UX3-06**: yetki süzgeci kaldırıldı | `Yetkisiz_ekran_KAPALI_grupta_da_onizlemede_de_yoktur` | AYNI |
| M1-5 | Basit modda da grup başlığı çizilir | `Basit_modda_grup_basligi_CIZILMEZ_ve_satirlar_katlanmaz` | AYNI |
| M1-6 | Yazma hatasında katlama geri alınmaz | `Yazma_basarisizsa_katlama_GERI_ALINIR_ve_bant_cizilir` | AYNI |
| M1-7 | Rol ham kodla basılır | `Kullanici_kartinda_rol_SOZLUKTEN_cevrilir_ham_kod_basilmaz(rol: "SAHIP")` | AYNI |
| M1-8 | Marka elle "Kuyumcu" | `Marka_satiri_TEK_KAYNAKTAN_gelir_eski_ad_yazilmaz` | AYNI |
| M1-9 | "Grupları sıfırla" tercih yokken de çizilir | `Katlama_SIFIRLA_dugmesi_yalniz_kayitli_tercih_varken_cikar_ve_DELETE_atar` | AYNI |
| M1-10 | Katlama hatasında "sıra kaydedilemedi" yazılır | `Yazma_basarisizsa_katlama_GERI_ALINIR_ve_bant_cizilir` | AYNI |
| M1-11 | Sunucu beyaz listesinden `menu-acik-gruplar` çıkarıldı | `TercihKatlamaHttpTestleri.Katlama_tercihi_YAZILIR_ve_geri_okunur`, `.PATCH_gonderilmeyen_anahtari_KORUR`, `.Katlama_tercihi_SIFIRLANABILIR_null_ve_DELETE`, `MenuKatlamaTestleri.Katlama_anahtari_sunucunun_BEYAZ_LISTESINDEDIR` (+1) | AYNI |
| M1-12 | Manrope `src` dış CDN'e (fonts.gstatic.com) | `Belirtecler_yazi_tipini_KENDI_KOKUMUZDEN_cagirir_CDN_yoktur` | AYNI |
| M1-13 | Aktif grup hesabından yetki süzgeci kaldırıldı | **ilk koşumda 0 kırmızı = TEST AÇIĞI** → test eklendi → `Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ` | AYNI |
| M1-14 | Zeytin `--basari` yeşile (#147f3c) döndürüldü | `Zeytin_basarisi_TUVALDEN_gelen_Zumrut_ailesiyle_AYNIDIR`, `Vurgu_ile_durum_rengi_AYIRT_EDILEBILIR_tek_muafiyet_bordo_tehlikedir`, `TasarimBelirtecTestleri.Kontrast_AA_saglanir` | AYNI |

## 9. Denetim (`bash dotnet/denetim.sh`)
İki kez koşuldu (değişiklikler sonrası ve paket sonu): **`DENETİM TEMİZ.`** exit 0, 273 ✓. Paketi ilgilendiren sayaçlar:
```
✓ Pages: ModalPencere kullanımı                55 (tavan 55)       ← artış 0
✓ P-02: ESKI_URUN_ADI (marka tek kaynak · P-D2) 0
✓ P-11: SATIR_ICI_STYLE                          125 (tavan 143)
✓ P-14: MENU_AYNI_AD 0 · MENU_KAPSAM 0 · MENU_ELLE_BAGLANTI elle href 0 · grup başlığı çizimi var
✓ P-18: SABIT_RENK 0 · TEMA_HAM_PALET_VURGU 0
✓ app.css ham hex 25 (tavan 25)
✓ P-37: MENU_OK_DUGMESI 0 · MENU_KALICI_PARAGRAF 0 · MENU_SESSIZ_YUTMA 0
✓ P-39: KONTROL_YUKSEKLIK_SAPMASI 0
```
Yeni sayaç **eklenmedi**: `denetim.sh` şu an P-30b şeridinin açık dosyası (tek kırmızının sebebi); çakışmayı önlemek için dokunulmadı. S1-b kuralları bUnit + mutasyonla kilitli.

## 10. Canlı doğrulama (port **5194**, `--no-build`, gerçek MySQL `kuyumcu_net`)
Kiracı **PAKETS1-b-TEST-2** (`01M37GQ7ZR7C2HFNSYFRNJ34MP`), davet `POST /sistem/davetler` 201 → `POST /auth/kayit` 201. (İlk tur `PAKETS1-b-TEST` / `01M37GAQ297HRFTVZE79CZQAZF` aynı sonuçları verdi; ancak o tur M1-13 kalıntılı Web ikilisiyle koştuğu için — § 7.2 tuzak — **temiz ikiliyle tekrarlandı**; aşağıdaki tablo tekrar turudur. İki kiracı da silindi.)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /tercihler/benim` | 200, boş | **200** `{"tercihler":{}}` |
| 2 | `PATCH {"menu-acik-gruplar":"[\"gunluk\",\"rapor\"]"}` | 200 | **200**, değer döndü |
| 3 | `PATCH {"menu-favoriler":…}` (katlama **gönderilmedi**) | 200, katlama KORUNUR | **200**, `menu-acik-gruplar` korundu |
| 4 | `PATCH {"menu-yok":…}` | 400 | **400** "Bilinmeyen tercih anahtarı: menu-yok" |
| 5 | Kimliksiz `GET` | 401 | **401** "Token bulunamadı" |
| 6 | 4001 karakterlik değer | 400 | **400** "… en çok 4000 karakter" |
| 7 | `DELETE /tercihler/benim/menu-acik-gruplar` | 200, yalnız o anahtar silinir | **200**, favori korundu |
| 8 | `GET` | favori var, katlama yok | **200** `{"menu-favoriler":"[\"/kasalar\"]"}` |
| 9 | `PATCH ["rapor"]` + SQL | satır yazılı | **200** · SQL: `menu-acik-gruplar ["rapor"]`, `menu-favoriler ["/kasalar"]` |
| 10 | Denetim izi (SQL) | `KullaniciTercihi` kayıtları | **EKLEME 7 · SILME 1** |
| 11 | `GET /font/manrope-latin.woff2`, `/font/sora-latin.woff2`, `/font/OFL-Manrope.txt` | 200, kaynakla birebir | **200** `font/woff2` / `text/plain`, `cmp` **AYNI** |
| 12 | `GET /css/app.css`, `/css/belirtecler.css` | 200, kaynakla birebir | **200**, `cmp` **AYNI** |
| 13 | CSP başlığı | `font-src 'self'`, istisna yok | **`font-src 'self'`** |
| 14 | Tarayıcı `/giris` | fontlar kendi kökümüzden, 0 CSP hatası | Manrope+Sora **loaded**, 4 istek **200**, gövde **18px Manrope**, konsol hatası **0** |

⚠ **Doğrulanamayan (dürüst not):** kenar çubuğunun kendisi oturum açılmadan çizilmez. Tarayıcıda oturum açmak parola girmeyi gerektirir; ajan güvenlik kuralı gereği **parola girilmedi**. Kenar çubuğunun davranışı (katlama, önizleme, basit mod, yetki, rol, marka, sıfırla) bUnit'te 25 testle ve 13 mutasyonla kanıtlı; 5199 QA turunda (orkestratör) görsel kontrol önerilir.

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5194 -sTCP:LISTEN` → **0 satır**). Her iki test kiracısı `tenant_id` kolonu olan **70 tablodan** + `tenants` + `kiraci_davetleri` satırından silindi; kalan satır **0**, `PAKETS1%` kiracı **0**, `PAKETS1%` davet **0**.
`tenants` sayısı: başlangıç **9** → son **14**. Fark **5** satırın hepsi `PAKET-P-D3-TEST Kuyumculuk` (DEMO şeridinin P-D3 koşumu, 16:00–16:05 arasında açılmış) — benim kiracım değil, dokunulmadı. `QA-TEST-` fikstürlerine dokunulmadı. Geçici sır dosyaları (sistem anahtarı, parola, jetonlu gövdeler) `printf "" >` ile boşaltıldı.

## 11. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/` altında **hiçbir sayfa açılmadı**. Açılan razor: `Layout/KenarCubugu.razor` (S-1 dosya listesinde).
Dokunulmayan şerit dosyaları: Modules/Kasa·Vezne·Maliyet·Rapor, Raporlar/Gider.razor, betikler/*.sh, Kurulum.razor, Siparis*/Faturalar.razor, Panel.razor, Sozlukler.Marka.cs, **Sozlukler.cs** (ortak hub), `denetim.sh`.

## 12. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (S1-b ile ilgili olanlar):
```
 M dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? dotnet/Kuyumcu.Core/I18n/MenuSozlugu.cs
?? dotnet/Kuyumcu.Web/Bilesenler/Ikonlar.cs            (önceki koşum: 3 ikon)
?? dotnet/Kuyumcu.Web/Servisler/MenuTercihServisi.cs
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/font/                     (4 woff2 + 2 OFL)
   dotnet/Kuyumcu.Web/wwwroot/img/ikon.svg              (önceki koşum)
   dotnet/Kuyumcu.Api/Modules/Tercih/TercihModulu.cs    (önceki koşum: beyaz liste 1 satır)
   dotnet/Kuyumcu.Tests/MenuKatlamaTestleri.cs          (önceki koşum + bu oturum 5 test/8 vaka)
   dotnet/Kuyumcu.Tests/TercihKatlamaHttpTestleri.cs    (önceki koşum)
   dotnet/Kuyumcu.Tests/YaziTipiVeSemaRengiTestleri.cs  (önceki koşum)
   dotnet/Kuyumcu.Tests/MenuMimarisiTestleri.cs         (önceki koşum)
 + dokumanlar/09-uygulama-kayitlari/PAKET-S1-b-uygulama-raporu.md (bu dosya)
```
(Ağaçta bu dosyaların çoğu dizin düzeyinde izlenmiyor — `??`; commit yapılmadı.)

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tuval `<nav>`: arama en üstte | KARŞILANDI | `<GlobalArama />` ilk öğe |
| "Tüm ekranlar (N)" aramanın hemen altında | KARŞILANDI (kilitli — S1) | `#kenar-tum-ekranlar`, sayı türetilmiş |
| 6 grup katlanır, `aria-expanded` | KARŞILANDI | `Grup_basligi_ARIA_EXPANDED…` |
| Kapalı grubun altında ekran adı önizlemesi (n2) | KARŞILANDI | `Kapali_grubun_onizlemesi…` + M1-1 |
| Kullanıcı kartı en altta | KARŞILANDI | `.kenar-kullanici`, rol sözlükten |
| Basit mod: başlık yok, 7 satır düz | KARŞILANDI | `Basit_modda_grup_basligi_CIZILMEZ…` + M1-5 |
| Mevcut id'ler korunur, yalnız eklenir | KARŞILANDI | eklenen: `kenar-grup-dugme-*`, `kenar-onizleme-*`, `kenar-tum-ekranlar(-sebep)`, `kenar-marka`, `kenar-kullanici-rol`, `kenar-katlama-sifirla`; mevcutlar (`kenar-nav`, `kenar-grup-<slug>`, `kenar-satir-*`, `kenar-sira-*`, `kenar-favori-*`) değişmedi |
| Ok düğmeleri yok, sıralama sürükle-bırak | KARŞILANDI | denetim `MENU_OK_DUGMESI 0` |
| Özellik silinmez | KARŞILANDI | satırlar `hidden`, favoriler/alt blok/çıkış korundu |
| Sabit renk / inline style yok | KARŞILANDI | `SABIT_RENK 0`, `ham hex 25/25`, yeni CSS yalnız `var()` |
| CDN/npm/inline script yok | KARŞILANDI | fontlar `/font/`, CSP `font-src 'self'`, M1-12 |
| M-UX3-06 (mod yetkiyi gevşetmez) | KARŞILANDI | M1-4, M1-13 |
| Fontlar vendored + OFL (devir 1) | KARŞILANDI | § 3.1 |
| Hue çakışması çözüldü + gerekçe (devir 2) | KARŞILANDI (uygulayıcı kararı, oy tutanağı yok) | § 3.2 |
| Sozlukler.cs'e dokunma, yeni metin ayrı dosya | KARŞILANDI | `MenuSozlugu.cs` |
| Sözleşme-DoD (PATCH+DELETE+yetki+denetim+ekran) | KARŞILANDI | § 6 |
| Paket kapısı 1–7 | KARŞILANDI | build 0 hata · paket testleri 0 kırmızı · denetim TEMİZ · 14 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 14. Açık kalanlar / kurucuya ve ekibe notlar
1. **Ağaçtaki 2 kırmızı (bu paketin DEĞİL):** `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` → **P-30b**; `AltinPozisyonuTestleri.B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi` → **B-2 (S-B şeridi)**, koşumum sırasında yazılıyordu.
2. **Derleme uyarıları (bu paketin DEĞİL):** `OlcumKapisiTestleri.cs(525)` CS8604 ×2 (P-30b), `PozisyonModulu.cs(52)` CS9113 (B-2).
3. **S1-d'ye devir:** `#kenar-tum-ekranlar` düğmesini `/ekranlar` bağlantısına çevir, `disabled`/sebep metnini kaldır; `Tum_ekranlar_dugmesi_KILITLI…` testini "bağlantı hedefi /ekranlar" testine dönüştür (id aynı kalmalı).
4. **Kurucuya (yalnız görsel tercih):** marka yazımı "BYGATECH" (tek kaynak) mı, tuvaldeki "ByGatech" mi — DEMO şeridinin `Marka` sabitine ait karar. Bordo şemasında vurgu ile tehlike dolgusu birbirine yakındır (1,36:1); bordo seçen dükkâna bu not düşülmeli.
5. **Hasım doğrulayıcıya:** D-S1b-1 kararı uygulayıcı kararıdır; onay/ret sizde.
6. **Kenar çubuğunun gerçek tarayıcıda görsel kontrolü** oturum açma (parola) gerektirdiği için bu ajanca yapılmadı → 5199 QA turunda bakılmalı.
7. **`Sozlukler.cs` `OlcekYogunAciklama`/`OlcekOkunakliAciklama`** hâlâ eski punto sayısını yazıyor (S1-a açık kalanı md.3) — ortak hub olduğu için dokunulmadı; B-x ya da S1-f düzeltmeli.
8. **Mutasyon tuzağı** (§ 7.2): `cp -p`/`copy2` ile geri koyma sonrası `touch` ya da `--no-incremental` şart — `mut.sh` benzeri betiklere eklenmeli.
