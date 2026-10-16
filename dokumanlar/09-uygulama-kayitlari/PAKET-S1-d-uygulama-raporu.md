# PAKET S1-d — Yeni ekran: Tüm ekranlar (38 ekranlık karo menü)

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** S-1 KABUK
**Durum beyanı:** BİTTİ. Paket kapısının 7 maddesi aşağıda kanıtlı. Tam süitte kalan 2 kırmızı ve denetim.sh'taki 6 ✗ başka şeritlerin ara hâlidir; ayrıntı § 7.3, § 9 ve § 14'te.

---

## 1. Kaynaklar ve spesifikasyonun koda karşı doğrulanması

- **Tek doğruluk kaynağı:** `dokumanlar/02-mimari/sadelestirme/tuval/project/Ekranlar.dc.html` ile `canvas.json` (başlık "4 · Tüm ekranlar — 38 ekranın hepsi (karo menü)", not n2). Kenar çubuğu girişi için `Panel.dc.html`, `Musteriler.dc.html` ve `Profesyonel.dc.html` içindeki `<nav>` satırı okundu: "Tüm ekranlar (38)". `mockup-S1.html` ile `mockup-tema-8.html` okunmadı (görev notu).
- **Tuvalden okunan ölçüler:** Artboard 1440×900 ve **kenar çubuğu yok**. Üstte 68 px'lik koyu şerit (`#14213d`, 0 32 px iç boşluk, 20 px aralık) var: 34 px'lik vurgu rozeti ve Sora 21/700 marka (Panel'e götürür), Sora 22/600 "Tüm ekranlar" başlığı, 360 px arama kutusu (`#1f2f52` zemin, `#2c3d63` kenar, 9 px yarıçap, 16 px yazı) ve 15 px ipucu "38 ekranın hepsi burada · yıldızla favorine ekle". Gövde 20 px 32 px iç boşluklu, bölümler arası 14 px. Grup başlığı 15/600, büyük harf, .04em harf aralığı, `#5a6577`. Izgara 8 sütun, 10 px aralık. Karo: 68 px yükseklik, 0 14 px iç boşluk, 1 px `#d7dce4` kenar, 10 px yarıçap, beyaz zemin, 17/500 yazı. **Satış karosu tek vurgu karosudur** (`#2457d6`, beyaz yazı, 600). **Yalnız "Günlük iş" karolarında 20 px ikon var.** En altta Kılavuz, Ayarlar ve Vitrin panosu bağlantıları duruyor (10×16 px iç boşluk, 9 px yarıçap, 16 px yazı).
- **"Mevcut durum" iddiaları koda karşı sınandı:**
  - `MenuTanimi.Ogeler` bugün **38 öğedir** (5+6+10+3+6+8). Tuvaldeki 38 ile grup sırası ve öğe sırası birebir aynı. (S1-b raporundaki "36" o günün sayısıydı.)
  - Kenar çubuğundaki `#kenar-tum-ekranlar`, S1-b'de **kilitli bir `<button disabled>`** olarak konmuştu. Gerekçesi "hedef sayfa S1-d'de açılıyor" idi. Doğrulandı.
  - `/ekranlar` rotası, `RotaBasliklari`, `RotaModulleri` ve `MenusuzRotalar` kayıtları yoktu. Doğrulandı.
  - Görev "Panel'den bağlanır" diyor. Tuvalde Panel'den Tüm ekranlar'a giden **tek yol kenar çubuğundaki satırdır**; Panel gövdesinde karo bağlantısı çizilmemiş. `Panel.razor` ayrıca DEMO şeridinin dosyası (dokunma yasağı). Bu yüzden Panel bağlantısı, Panel ekranında da duran kenar çubuğu girişiyle sağlandı ve `Panel.razor` açılmadı.
- **Mevzuat:** Bu paket arayüz paketidir ve mevzuat hükmü içermez. Mevzuat kuralı tetiklenmedi.

## 2. Yapılanlar

| Dosya | İş |
|---|---|
| `Kuyumcu.Web/Pages/Ekranlar.razor` (YENİ) | `/ekranlar` sayfası tuvaldeki gibi kuruldu: koyu şerit (`#ekranlar-ust`: marka → `/`, h1, arama, ipucu), 6 grup bölümü (`#ekranlar-grup-<slug>`), karolar (`#ekran-karo-<slug>`) ve her karoda bir favori yıldızı (`#ekran-favori-<slug>`). Altta `#ekranlar-alt` (Kılavuz, Ayarlar, Vitrin panosu) duruyor. **Karo listesi, sırası ve adları `MenuTanimi`'nden üretilir**; sayfada tek bir sayı ya da çıplak metin yok. Yetkisiz karo **çizilmez**; mod süzgeç değildir. Arama Türkçe katlamalıdır (`AramaMetni.Katla`) ve Enter ilk eşleşen karoyu açar. Aramada sonuç yoksa boş durum ve "Aramayı temizle" çıkar. Yetki okunamazsa bant ve gerçek GET atan "Yeniden dene" düğmesi çizilir; favori yazılamazsa bant çıkar ve değişiklik geri alınır. Sayfa veri okumaz ve POST etmez. |
| `Kuyumcu.Core/I18n/EkranlarSozlugu.cs` (YENİ) | Sayfa metinleri TR+EN olarak yazıldı. `Sozlukler.cs` ortak hub'ına dokunulmadı. |
| `Kuyumcu.Web/Layout/MenuTanimi.cs` | `YetkiliMi` / `Yetkililer` yardımcıları eklendi: yetki süzgeci **tek yerde** duruyor. Ayrıca `Slug(yol)`, `TumEkranlarYolu = "/ekranlar"` ve `MenusuzRotalar["/ekranlar"] = "Layout/KenarCubugu.razor"` eklendi. |
| `Kuyumcu.Web/Layout/KenarCubugu.razor` | Kilitli düğme **`<NavLink href=TumEkranlarYolu>`** oldu ve `#kenar-tum-ekranlar` id'si aynı kaldı. `#kenar-tum-ekranlar-sebep` **silinmedi**: artık kilidin sebebini değil, bağlantının ne açtığını söylüyor (id sözleşmesi). `Gorunur()`, `MenuTanimi.YetkiliMi` + mod süzgeci olarak çalışıyor (davranış aynı). **Sayı artık yalnız yetkiden türüyor** (bkz. § 4 S3). Yerel `Slug` artık `MenuTanimi.Slug`'a yönleniyor. |
| `Kuyumcu.Web/Layout/AnaYerlesim.razor` | `TamEkranRotasi("/ekranlar")` eklendi: bu rota kenar çubuğu ve üst bar olmadan, tam ekran çizilir (tuval). Oturum kapısı ve `KabukHatasi` sarmalayıcısı aynen işliyor. |
| `Kuyumcu.Core/I18n/MenuSozlugu.cs` | `TumEkranlarKilitli` yerine `TumEkranlarAciklama` geldi (kilit kalktı). |
| `Kuyumcu.Web/Layout/RotaBasliklari.cs` · `Kuyumcu.Core/Erisim/RotaModulleri.cs` | `/ekranlar` başlığı sözlükten geliyor. Modül kapısı `null`: sayfa yalnız gezinmedir ve kapı karo başınadır. |
| `Kuyumcu.Web/wwwroot/css/app.css` | Sona tek bir **"S1-d"** bloğu eklendi (geri dönüş = bloğu kaldırmak). Tüm renkler belirteç; ham hex, `rgba` ve inline style yok. ≤1199 px'te 4 sütun, ≤768 px'te 2 sütun. |
| `dotnet/denetim.sh` | Üç sayaç tavanı gerekçeyle güncellendi: `P02_ROTA_BEKLENEN` 46→47, `Pages/*.razor` 46→47, SAYFA_BASI muafiyet ≤1→≤2. Başka satıra dokunulmadı. |
| `betikler/qa-turu.js` · `ham/olcum-2026-09-18.json` | Rota kapsamı 46→47 yapıldı (fail-closed QA turu yeni rota yüzünden başlamazdı). Tek satır ve tek alan değişti, not satırına ölçüm eklendi. |
| Testler | Yeni dosya `S1dEkranlarTestleri.cs`. `MenuKatlamaTestleri`: kilitli düğme testi bağlantı testiyle değiştirildi, basit mod sayısı ve yetkisiz sayı testleri eklendi. `IkonTestleri`, `RotaBaslikTestleri` ve `P37cPanelVeKapsamTestleri` beklenenleri gerekçeyle güncellendi. |

## 3. Tuval değerlerinin belirteç karşılığı (canlı ölçüm, Chrome, Platin aydınlık, 1440×900)

| Tuval | Belirteç | Ölçülen |
|---|---|---|
| Şerit `#14213d`, 68 px | `--kenar-yuzey` | rgb(20,33,61), 68 px |
| Arama `#1f2f52` / `#2c3d63`, 360 px | `--kenar-yuzey-2` / `--kenar-ayrac` | rgb(31,47,82) / rgb(44,61,99), 360 px |
| İpucu ve yer tutucu `#aab7cf` | `--menu-baslik` (S1-b ile aynı eşleme) | — |
| Başlık Sora 22/600 | `--yazi-marka` | Sora, 22 px, 600 |
| Grup başlığı 15/600 `#5a6577` büyük harf | `--metin-soluk` | 15 px, rgb(90,101,119), uppercase |
| Karo 68 px, `#d7dce4`, 10 px, beyaz, 17/500 Manrope | `--ayrac`, `--yuzey`, `--metin` | 68 px · rgb(215,220,228) · 10 px · rgb(255,255,255) · 17 px/500 · Manrope |
| Karo genişliği (8 sütun) | — | 163,25 px (tuval: (1440−64−70)/8 = 163,25) |
| Satış karosu `#2457d6` + beyaz 600 | `--vurgu` / `--vurgu-metin` | rgb(36,87,214) / rgb(253,253,255), 600 |
| Sayfa 900 px'e sığar | — | `scrollHeight` 900 |

## 4. Sapmalar ve gerekçeleri

| # | Tuval / görev | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | Karo adları: "Satış", "Müşteriler", "Kasa kuyruğu", "Kim ne yaptı", "Raporlar" grubu | Adlar **kenar çubuğuyla aynı sözlükten** geliyor (İşlemler, Cariler, Vezne, Denetim İzi, "Dökümler" …) | UX-S31: aynı ekran iki yerde iki ad taşıyamaz. Adların tuvaldekilere çevrilmesi `Sozlukler.cs` ortak hub'ında yapılacak **sözlük göçü** işidir (plan 1.8). Bu pakette hub'a dokunmak yasak. Göç yapılınca karolar kendiliğinden yeni adı alır. |
| S2 | "Mihenk" | `Marka.Ad` ("MİHENK") | S1-b'deki S3 ile aynı gerekçe: marka tek kaynağı DEMO şeridinde. |
| S3 | S1-b: sayı = yetki + **mod** süzgeci | Sayı = **yalnız yetki** = sayfadaki karo sayısı | Tuval basit yüzde (Panel.dc.html) de "(38)" yazıyor. Mod süzgeci kalsaydı basit modda bağlantı "(8)" der, sayfa 38 karo açardı. Bu, aynı soruya iki cevap demektir (UX-S22). Menünün kendisi basit modda **yine 8 satır** (test ile kilitli). |
| S4 | Tuvalde yıldız çizilmemiş, ama ipucu "yıldızla favorine ekle" diyor | Her karoda yıldız var. Dolu yıldız hep görünür; boş yıldız üstüne gelince, odakta ve dokunmatikte görünür. Karo sağ iç boşluğu 14→36 px (dokunmatikte `--dokunma-hedefi`+8 px). | Tam ekran sayfada kenar çubuğu yok, yani ipucunun bahsettiği yıldız başka yerde görünmüyor. Yıldız, kenar çubuğundaki yıldızla aynı kaydı (`menu-favoriler`) yazıyor. İç boşluk, canlı ölçümde 1440 px'te 10 karoda yıldızın uzun adın üstüne bindiğinin görülmesi üzerine açıldı. Düzeltmeden sonra çakışma 0 (§ 10.2). |
| S5 | Görev: "Panel'den bağlanır" | Bağlantı Panel ekranında da görünen kenar çubuğu girişinden veriliyor. `Panel.razor` açılmadı. | Tuvalde Panel → Ekranlar yolu yalnız `<nav>` satırı. `Panel.razor` ayrıca DEMO şeridinin dosyası. |
| S6 | Tuval ikonları (ev, kart, kilit, pano, zil) | `MenuOgesi.Ikon` (kenar çubuğuyla aynı ikonlar) | İkonun tek kaynağı menü tanımı. Tuvalin ikon çizimleri sprite'ta yok; yeni ikon eklemek bu paketin kapsamı dışında. |
| S7 | Tuvalde arama `<input>` | Ortak `Form.Arama` bileşeni koyu kabın içinde (`#ekranlar-ara` id'si korunuyor) | P-36 HAM_INPUT kapısı: ham girdi yasak. Görünüm CSS ile tuvaldeki gibi yapıldı. |
| S8 | Tam ekran: tuvalde üst bar yok | Kur şeridi, mod anahtarı ve çıkış bu sayfada yok. Marka ile Panel'e bir dokunuşta dönülüyor. | Tuval birebir uygulandı. Özellik silinmedi: hepsi diğer her ekranda duruyor. |

## 5. Migration
**Gerekmedi.** Şema değişikliği yok. Favori yıldızı mevcut `kullanici_tercihleri` tablosunu (`menu-favoriler` anahtarı) ve mevcut `PATCH/DELETE /tercihler/benim` uçlarını kullanıyor.

## 6. DoD sözleşmesi (Bertan koşulu)
Bu paket **yeni varlık ya da kayıt türü doğurmaz**.

| Kayıt | Düzenleme yolu | Silme / geri alma | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|
| Favori ekranlar (`kullanici_tercihleri.menu-favoriler`) | `PATCH /tercihler/benim` (gönderilmeyen anahtar korunur) | `DELETE /tercihler/benim/menu-favoriler` | Oturum sahibi (kendi tercihi) | `EKLEME/GUNCELLEME/SILME KullaniciTercihi` (canlıda görüldü, § 10.1) | Karodaki yıldız ekler ve çıkarır; kenar çubuğunun "Sırayı sıfırla" ve yıldızları aynı kaydı kullanır |
| Karo listesi | — | — | Karo başına menü kapısı (`MenuTanimi.YetkiliMi`) | — | Kayıt değil, gezinme |

**Her düğme iş yapar** (DoD EK md.1): yıldız PATCH atar, "Yeniden dene" GET atar, "Aramayı temizle" listeyi geri getirir. Üçü de bUnit ile sınandı. Sayfada disabled ya da etkisiz düğme yok (`Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar`).

## 7. Testler

### 7.1 Paket testleri — `S1dEkranlarTestleri` **30/30 yeşil** + `MenuKatlamaTestleri` (3 yeni/yeniden yazılmış)
Kapsanan konular:
- Karo sayısı ve sırası `MenuTanimi`'nden birebir; id'ler `#ekran-karo-<slug>`.
- İpucu sayısı karo sayısıyla aynı ve tuval metnini taşıyor.
- 6 grup tuval sırasında; karo adları kenar çubuğuyla aynı kaynaktan.
- TEZGAHTAR'da yetkisiz karo ve yıldız çizilmiyor, boş grup başlığı çizilmiyor.
- Basit modda 38 karo çiziliyor.
- Arama "SAYIM" → Sayım buluyor; sonuç yoksa boş durum çıkıyor ve temizle çalışıyor; arama boşken boş durum çizilmiyor.
- Enter ilk karoyu açıyor; aramasız Enter hiçbir yere gitmiyor.
- Yıldız PATCH atıyor (gövde JSON olarak ayrıştırılıyor); yazma hatasında bant çıkıyor ve değişiklik geri alınıyor.
- Yetki hatasında bant çıkıyor ve "Yeniden dene" gerçek GET atıyor.
- Tek birincil karo Satış; yalnız Günlük iş karoları ikonlu.
- Şerit: marka → `/`, h1, tuvaldeki yer tutucu.
- Alt bağlantılar Kılavuz/Ayarlar/Vitrin; `kur` yetkisi yoksa Vitrin çizilmiyor.
- EN dil ve sözlük iki dilde dolu.
- Rota başlığı, rota haritası ve muafiyet kaydı yerinde.
- `TamEkranRotasi` 5 vaka.
- Çıplak metin, inline style ve elle yazılmış "38" yok.
- CSS bloğu tuval ölçülerini belirteçle taşıyor.

MenuKatlama tarafında: giriş artık `<a href="/ekranlar">` ve id'ler yaşıyor; basit modda sayı yetkili ekran sayısı (menü yine 8 satır); yetkisiz ekran sayılmıyor (bağımsız sayımla karşılaştırılıyor).

### 7.2 Etkilenen mevcut testler — **193/193 yeşil** (son derlemeyle)
`IkonTestleri · S1dEkranlarTestleri · MenuKatlamaTestleri · MenuMimarisiTestleri · RotaBaslikTestleri · P37cPanelVeKapsamTestleri · OlcumKapisiTestleri · S1cUstBarTestleri`. Daha önce, aynı kodla `P09KimlikErisim · BasitMod* · KabukArama* · KabukBilesen* · TasarimBelirtec* · P39* · SozlesmeKapsam · YaziTipi* · TemaEkran* · P36*` de yeşildi (349/349 ve 227/227).

Güncellenen beklentiler ve gerekçeleri:
- `RotaBaslikTestleri.Yansima_TAM_42_rota_bulur` 46→47 (yeni rota).
- `P37c…Her_ekran_ortak_arac_seridini…` 46→47 sayfa, muaf listesine `Ekranlar.razor` eklendi (gerekçe dosyada `SAYFA_BASI_MUAFIYETI`).
- `IkonTestleri.KenarCubugu_menu_rotalari…` başa `/ekranlar` eklendi (kilitli düğme bağlantı oldu).
- `MenuKatlamaTestleri.Tum_ekranlar_dugmesi_KILITLI…` yerine `Tum_ekranlar_girisi_ARTIK_KARO_SAYFASINA…` geldi.

### 7.3 Tam süit
`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx` (KUYUMCU_TEST_DB=kuyumcu_test_s, `dotnet build-server shutdown` sonrası):

| Koşum | Toplam | Geçti | Başarısız | Not |
|---|---|---|---|---|
| S1-c sonu (devir) | 4196 | 4195 | 1 | P-30b kırmızısı |
| 1. koşum (`trx-pS1-d`) | 4237 | 2528 | 1709 | 1708'i `MySqlException: Unknown column 'sistem_carisi'`: başka şeridin yeni `SistemCarisi` alanı migration'dan önce ağaca girmişti (trx'ten sayıldı). **1 tanesi benim:** `IkonTestleri.KenarCubugu_menu_rotalari…` (`/ekranlar` bağlantısı sayıldı) → düzeltildi. |
| **2. (son) koşum** (`trx-pS1-d-2`) | **4241** | **4239** | **2** | Aşağıda |

| Kırmızı | Mesaj | Sahibi | Bu paketin ürünü mü? |
|---|---|---|---|
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` | "YENİ istemci tipi: CariVeri" | `CariVeri` tipini `Cariler.razor`/`Islemler.razor`/`Banka.razor`/`Faturalar.razor` kullanıyor; `Cariler.razor` 20:44'te, `Islemler.razor` 20:43'te başka bir şerit tarafından değiştirildi (Peşin Müşteri / SistemCarisi hattı olduğu düşünülüyor, **sahip adı doğrulanamadı**) | HAYIR. S1-d bu dosyaların hiçbirini açmadı. |
| `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` | Beklenen 59, gerçek 60 | Aynı kök (yeni istemci tipi sayıya girdi) | HAYIR |

**Madde (7) — yeni kırmızı üretmeme:** Bu paketin ürettiği tek kırmızı (`IkonTestleri`) düzeltildi ve son koşumda yeşil. Kalan iki kırmızının kaynağı S1-d'nin dokunmadığı dosyalarda. Fark = ∅. **Sağlandı.** (Devir notundaki P-30b kırmızısı `OlcumKapisiTestleri.denetim_sh_HER_CAGRI…` bu koşumda yeşil.)

## 8. Mutasyon kanıtları
Yöntem şöyle işledi:
1. Kaynak `scratchpad/mut-yedek/S1-d/` altına kopyalandı.
2. `perl -0777 -pi` ile `\Q…\E` literal değişiklik yapıldı; eşleşme sayısı ≠1 ise betik duruyordu.
3. `dotnet test --filter <ilgili> -v n` koşuldu.
4. Yedek geri kopyalandı ve `touch` edildi.
5. `cmp` ile birebir aynılık doğrulandı.

Son mutasyondan sonra `dotnet build` 0/0 çıktı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1d-1 | Karo listesinde yetki süzgeci kaldırıldı (`[.. MenuTanimi.Ogeler]`) | `S1dEkranlarTestleri.Yetkisiz_ekranin_karosu_CIZILMEZ_ipucu_da_saymaz` | AYNI |
| M1d-2 | `YetkiliMi` rol kapısı → `true` | `S1d…Yetkisiz_ekranin_karosu_CIZILMEZ…`, `MenuKatlamaTestleri.Tum_ekranlar_sayisi_YETKISIZ_ekrani_SAYMAZ`, `MenuKatlamaTestleri.Yetkisiz_rotada_o_rotanin_grubu_ACILMAZ` | AYNI |
| M1d-3 | Aramada Türkçe katlama kaldırıldı | `Arama_TURKCE_KATLAR_sayim_Sayim_i_bulur`, `Aramada_Enter_ILK_eslesen_karoyu_acar` | AYNI |
| M1d-4 | Boş durum koşulu tersine çevrildi | `Arama_sonucu_yoksa_BOS_DURUM_ve_temizle_dugmesi_listeyi_geri_getirir` | AYNI |
| M1d-5 | Yıldız `@onclick` → `Task.CompletedTask` | `Yildiz_PATCH_atar_govde_favori_listesini_tasir`, `Yildiz_yazilamazsa_BANT_cizilir_ve_GERI_ALINIR` | AYNI |
| M1d-6 | Kenar sayısı yeniden mod süzgecine bağlandı | `MenuKatlamaTestleri.Tum_ekranlar_sayisi_BASIT_MODDA_da_YETKILI_ekran_sayisidir_mod_suzgeci_yok` | AYNI |
| M1d-7 | Birincil karo `/islemler` → `/` | `TEK_birincil_karo_Satis_tir` | AYNI |
| M1d-8 | CSS karo yüksekliği 68 → 60 px | `S1d_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` | AYNI |
| M1d-9 | Yetki hata bandı gizlendi | `Yetki_okunamazsa_BANT_ve_yeniden_dene_GERCEK_istek_atar` | AYNI |
| M1d-10 | Enter ilk yerine son karoyu açıyor | `Aramada_Enter_ILK_eslesen_karoyu_acar` | AYNI |
| M1d-11 | Vitrin kapısı → `true` | `Vitrin_panosu_KUR_yetkisi_yoksa_CIZILMEZ` | AYNI |
| M1d-12 | `TamEkranRotasi` `""` (panel) rotasını da tam ekran sayıyor | `Tam_ekran_YALNIZ_karo_menude(yol: "/", beklenen: False)` | AYNI |
| M1d-13 | Tercih hata bandı gizlendi | `Yildiz_yazilamazsa_BANT_cizilir_ve_GERI_ALINIR` | AYNI |
| M1d-14 | İkon koşulu `Gunluk` → `!= Rapor` | `Yalniz_Gunluk_is_karolari_IKON_tasir` | AYNI |
| M1d-15 | `TumEkranlarYolu` → `/` | `MenuKatlamaTestleri.Tum_ekranlar_girisi_ARTIK_KARO_SAYFASINA_giden_BAGLANTIDIR` | AYNI |

⚠ **Dürüstlük notları:**
- **Test açığı bulundu ve kapatıldı:** M1d-2'nin ilk koşumunda `MenuKatlamaTestleri.Tum_ekranlar_sayisi_YETKISIZ…` kırmızı olmadı, çünkü beklenen değeri mutasyona uğramış fonksiyonun kendisiyle hesaplıyordu. Test, süzgeçten bağımsız bir sayımla güçlendirildi ve mutasyon yeniden koşuldu. Tablodaki sonuç ikinci koşumdur.
- **Eşzamanlı şerit çakışması:** M1d-5 ve M1d-6'nın ilk koşumları **test sonucu vermedi**, çünkü derleme başka şeritlerden kırıldı: `MasakVarliklari.cs CS0234` ve `ConvertDllsToWebCil MSB4018` dosya kilidi. Bu koşumlar kırmızı sayılmadı. Yedekler geri kondu (cmp AYNI), ağaç yeniden derlendi (0/0) ve iki mutasyon yeniden koşuldu.
- **app.css riski:** M1d-8 `app.css` üzerinde koşuldu. Geri alma "yedeği geri kopyala" yöntemiyle yapıldı. Mutasyon süresince (≈1,5 dk) başka bir şerit `app.css`'e yazmış olsaydı o yazı kaybolurdu. Bunu dışlayan bir kanıtım yok. Sonraki `app.css` değişikliklerim yerinde düzenlemeyle yapıldı.
- **Hayatta kalan mutasyon: 0.**

## 9. Denetim
`bash dotnet/denetim.sh`:
- **Paketin kendi sayaçlarının hepsi ✓:** `P-02 ROTA_KAPSAM 47` · `P-14 MENU_KAPSAM 0 (47 rota)` · `MENU_ELLE_BAGLANTI elle href 0` · `MENU_AYNI_AD 0` · `BASIT_MOD_TUTARSIZ 0` · `P-36 HAM_INPUT 42/42` · `HAM_INPUT_SERBEST 0` · `P-37c SAYFA_BASI_KAPSAM 45/45 (+2 yazılı muafiyet)` · `Pages/*.razor 47 (tavan 47)` · `app.css ham hex 25/25` · `satır içi style 125/196`.
- **İlk koşumlarda kendi ihlallerim** şunlardı ve hepsi giderildi:
  - Çok satırlı `<button`'da `@onclick` ikinci satırdaydı ve sayaç satır bazlı saydığı için "ölü düğme" görünüyordu. `@onclick` ilk satıra alındı.
  - Ham `<input>` vardı. Ortak `Form.Arama`'ya geçildi.
  - KenarCubugu'nda elle `href="/ekranlar"` vardı. `MenuTanimi.TumEkranlarYolu`'na bağlandı.
  - Sayfa sayısı, SAYFA_BASI muafiyeti ve rota tavanları gerekçeyle güncellendi.
  - Bir yorumdaki "<input>" sözcüğü HAM_INPUT sayacına giriyordu. Sözcük değiştirildi.
- **Son koşum `DENETİM BAŞARISIZ` verdi. Altı ✗'in hiçbiri bu paketin değil:**
  - `Doviz kolonu atama 4` → `Data/KuyumcuDbContext.cs:945`
  - `Pages: @onclick'siz <button> 74` → artış `Cariler.razor` ve diğerlerinde; `Ekranlar.razor` katkısı **0** (3 düğmesinin üçü de `@onclick` taşıyor)
  - `Kültürsüz ToString (Web) 28`: S1-d dosyalarında `ToString(` yok
  - `P-08 HARETURU_DISI_SABIT` → `Modules/Cari/SistemCarisi.cs:46 Tur = "MUSTERI"`
  - `P-09 OZNITELIKSIZ_UC` → `Modules/Destek/DestekModulu.cs:400`
  - `P-11 DIS_HATADA_VERI_DUSUREN_YOL 48>47`

  Paket başlarken (`denetim-once.txt`) ağaçta başka bir şeridin `Sınırsız ToListAsync 146>145` ihlali vardı. Ara koşumda (`denetim2.txt`) DENETİM TEMİZ çıktı. Kalan ✗'ler o koşumdan sonra ağaca giren başka şerit değişiklikleri.

## 10. Canlı doğrulama

### 10.1 API (port **5194**, `--no-build`, Development, gerçek MySQL `kuyumcu_net`)
Kiracı **PAKETS1-d-TEST** (`01M37N78G16TPXXF3RP305J0K0`):
- `POST /sistem/davetler`: ilk deneme **400** "X-Sistem-Operator başlığı zorunludur". Başlığı ben eklememiştim. Başlık eklenince **201** döndü.
- `POST /auth/kayit` **201**.
- Sistem anahtarı rastgele üretildi, yalnız ortam değişkeninde tutuldu ve ekrana yazılmadı.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /ekranlar` | 200, Blazor kabuğu | **200**, `<title>MİHENK</title>` + `blazor.webassembly.js` |
| 2 | `GET /css/app.css` | 200; kaynakla birebir; S1-d bloğu yayında | **200**, `cmp` **AYNI**, "S1-d — TÜM EKRANLAR (KARO MENÜ)" 1 eşleşme |
| 3 | `GET /auth/ben` | 200, SAHİP | **200**, rol SAHIP, etkin PROFESYONEL |
| 4 | `GET /yetkiler/benim` (sayfanın tek okuma ucu) | 200, modül listesi | **200**, 22 modül (`kur` dâhil → Vitrin çizilir) |
| 5 | Kimliksiz `GET /yetkiler/benim` | 401 | **401** "Token bulunamadı" |
| 6 | `GET /tercihler/benim` | 200, boş | **200** `{"tercihler":{}}` |
| 7 | `PATCH /tercihler/benim` (yıldızın gövdesi: `menu-favoriler` = `["/cariler","/sayim"]`) | 200 | **200**, "Tercihler kaydedildi" |
| 8 | `GET /tercihler/benim` | kayıt okunur | **200**, aynı liste |
| 9 | `PATCH` yıldızı kaldır (`["/cariler"]`) | 200 | **200** |
| 10 | `PATCH` bilinmeyen anahtar | 400 | **400** "Bilinmeyen tercih anahtarı: yok-anahtar" |
| 11 | SQL `kullanici_tercihleri` | `menu-favoriler ["/cariler"]` | **`menu-favoriler ["/cariler"]`** |
| 12 | `DELETE /tercihler/benim/menu-favoriler` | 200; satır 0 | **200** "Tercih sıfırlandı"; SQL satır **0** |
| 13 | SQL `denetim_kayitlari` | tercih izi | `EKLEME KullaniciTercihi 4`, `GUNCELLEME KullaniciTercihi 1`, `SILME KullaniciTercihi 1` (+ Tenant/Sube/Kullanici/RefreshToken 1'er, KurSnapshot 5) |

### 10.2 Tarayıcı ölçümü (gerçek Chrome, statik düzenek)
⚠ **Dürüst not:** Sayfa oturum ister. Tarayıcıda oturum açmak parola girmeyi ya da jeton enjekte etmeyi gerektirir; ajan güvenlik kuralı gereği ikisi de **yapılmadı** (S1-b ve S1-c ile aynı sınır).

Bunun yerine şu düzenek kuruldu:
- `scratchpad/harness-s1d/` altında ağaçtaki **gerçek** `app.css`, `belirtecler.css`, vendored fontlar ve `ikon.svg` kullanıldı.
- `Ekranlar.razor`'un ürettiği DOM'un kopyası hazırlandı: 38 karo, SAHİP, Cariler yıldızı dolu. 3 şema × aydınlık/karanlık.
- Düzenek 127.0.0.1:**5193**'te `python3 -m http.server` ile sunuldu. 5195–5200'e dokunulmadı.

| Ölçüm | Sonuç |
|---|---|
| Yatay taşma: 4 sayfa (platin aydınlık/karanlık, fildişi aydınlık, bordo karanlık) × 5 genişlik (1440/1280/**1024**/768/375) = 20 ölçüm | **20/20: `scrollWidth == clientWidth`, sayfa dışına taşan öğe 0** |
| Sütun sayısı | 1440/1280: 8 · 1024: 4 · 768/375: 2 |
| Karo yüksekliği | tüm genişliklerde **68 px** |
| Fontlar | `document.fonts`: Manrope + Sora loaded |
| Yıldız ile ad çakışması | **İlk ölçümde 1440 px'te 10 karo ve 375 px'te (dokunmatik, 44 px yıldız) 15 karo çakışıyordu.** Sağ iç boşluk ayrıldı (S4). Sonra 1440 / 1280 / 375 px'te çakışma **0**, ad taşması **0**, en fazla 2 satır. |
| Fildişi | krem şerit + koyu yazı okunaklı (görsel kontrol) |

### 10.3 Temizlik
- API (PID) ve düzenek sunucusu `kill` ile durduruldu; `lsof -iTCP:5194` ve `:5193` **boş**.
- Test kiracısı `tenant_id` kolonu olan **70 tablodan**, `kiraci_davetleri`'nden (kendi davetim) ve `tenants`'tan silindi. 71 tabloda kalan satır toplamı **0**.
- `tenants`: başlangıç **10** → paket sırasında 11 → son **10**.
- `QA-TEST-` fikstürlerine (4 kiracı) dokunulmadı.
- Geçici sır dosyaları (sistem anahtarı, parola, jeton, kayıt/davet gövdeleri) `printf "" >` ile boşaltıldı.
- Tarayıcı öykünmesi `desktop` presetine döndürüldü.

## 11. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/` altında yalnız **`Pages/Ekranlar.razor` (YENİ)** açıldı. Açılan Layout dosyaları: `KenarCubugu.razor` (S1-b'nin açtığı dosya; S-1 dosya listesinde; gerekçe: kilitli girişin bağlantıya dönmesi S1-b raporunda S1-d'ye devredilmişti) ve `AnaYerlesim.razor` (S-1 dosya listesinde).

**Dokunulmayan şerit dosyaları:** Modules/Kasa·Vezne·Maliyet·Rapor, Raporlar/Gider.razor, betikler/*.sh, Kurulum.razor, Siparis*/Faturalar.razor, **Panel.razor**, Sozlukler.Marka.cs, **Sozlukler.cs**, sunucu kodu (Kuyumcu.Api).

## 12. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (S1-d'ye ait olanlar; `Kuyumcu.Tests/`, `dokumanlar/`, `Kuyumcu.Core/Erisim/`, `denetim.sh` git'te izlenmediği için dosya düzeyinde yazıldı):
```
 M dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor
 M dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                       (sonda "S1-d" bloğu)
?? dotnet/Kuyumcu.Web/Pages/Ekranlar.razor                        (YENİ)
?? dotnet/Kuyumcu.Core/I18n/EkranlarSozlugu.cs                    (YENİ)
?? dotnet/Kuyumcu.Core/I18n/MenuSozlugu.cs                        (TumEkranlarKilitli → TumEkranlarAciklama)
?? dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs                        (YetkiliMi/Yetkililer/Slug/TumEkranlarYolu/MenusuzRotalar)
?? dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs                    (/ekranlar)
?? dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs                    (/ekranlar = null)
?? dotnet/denetim.sh                                              (3 tavan, gerekçeli)
?? dotnet/Kuyumcu.Tests/S1dEkranlarTestleri.cs                    (YENİ)
?? dotnet/Kuyumcu.Tests/MenuKatlamaTestleri.cs · IkonTestleri.cs · RotaBaslikTestleri.cs · P37cPanelVeKapsamTestleri.cs
?? dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js           (ROTA_KAPSAM_BEKLENEN 47)
?? dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json     (ROTA_KAPSAM 47)
?? dokumanlar/09-uygulama-kayitlari/PAKET-S1-d-uygulama-raporu.md
```
Proje ağacına .bak, .tmp ya da .mutorj bırakılmadı; yedekler scratchpad'de. (`dotnet/denetim.sh.mutbak` paketten önce de vardı ve benim değil.)

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| YENİ `Pages/Ekranlar.razor`, rota `/ekranlar`, tuval birebir | KARŞILANDI (sapmalar § 4) | § 3 canlı ölçüm; tuval ölçüleri CSS testiyle kilitli |
| 6 grup başlığı, 68 px karolar, 8 sütun | KARŞILANDI | `Alti_grup_basligi…`; ölçüm 68 px / 8 sütun / 163,25 px |
| Üstte geniş arama ("Ekran adı yaz: fatura, kasa, sayım…") | KARŞILANDI | `Ust_serit…`, `Arama_TURKCE_KATLAR…` |
| Altta Kılavuz / Ayarlar / Vitrin panosu | KARŞILANDI | `Alt_baglantilar…`, `Vitrin_panosu_KUR_yetkisi_yoksa…` |
| Karo sayısı MenuTanimi'nden ÜRETİLİR, elle yazılmaz | KARŞILANDI | `Karolar_MenuTanimi_ndan_URETILIR…`; kaynakta "38" yok (test) |
| Yetkisiz ekranın karosu ÇİZİLMEZ (M-UX3-06) | KARŞILANDI | M1d-1, M1d-2 |
| KenarCubugu'ndan ve Panel'den bağlanır | KARŞILANDI (Panel: kenar çubuğu girişiyle, S5) | `Tum_ekranlar_girisi_ARTIK…`, M1d-15 |
| Özellik silinmez | KARŞILANDI | Kilitli girişin id'leri yaşıyor; tam ekrandaki şerit dışı öğeler diğer ekranlarda duruyor (S8) |
| Mevcut DOM id'leri değişmez | KARŞILANDI | `#kenar-tum-ekranlar`, `#kenar-tum-ekranlar-sebep` korundu; yeni id'ler `ekranlar-*` / `ekran-karo-*` / `ekran-favori-*` |
| Sabit renk / inline style yok | KARŞILANDI | CSS testi (hex/rgba yok) + razor testi; denetim ham hex 25/25 |
| CDN / npm / inline script yok | KARŞILANDI | Script ya da link eklenmedi; fontlar zaten vendored |
| Tek POST yolu (Islemler Kaydet) | KARŞILANDI | Sayfa POST etmiyor; yalnız mevcut tercih PATCH'i |
| Sozlukler.cs'e dokunma; yeni metin ayrı dosyada | KARŞILANDI | `EkranlarSozlugu.cs` |
| UX-S22 (aynı soruya iki cevap yok) | KARŞILANDI | Sayı = karo sayısı (S3, M1d-6) |
| UX-S23 (sayı gösteren yer bağlantı olur) | KARŞILANDI | "Tüm ekranlar (N)" sayısı karo sayfasına götüren bağlantının içinde |
| UX-S28 (hata boşa çevrilmez) | KARŞILANDI | Yetki ve tercih bantları (M1d-9, M1d-13); boş durum yalnız başarılı ve boş aramada (M1d-4) |
| UX-S31 (tek ad, tek kaynak) | KARŞILANDI | Karo adları `MenuTanimi`/Sozluk.Nav'dan (S1) |
| DoD EK md.1 (her düğme istek atar) | KARŞILANDI | Yıldız PATCH, yeniden dene GET, temizle (testler) |
| DoD EK md.3 (boş durum + hata bandı) | KARŞILANDI | § 7.1 |
| İzolasyon (kuyumcu_test_s, 5194, build-server shutdown; 5195–5200'e dokunulmadı) | KARŞILANDI | Tüm koşumlar; düzenek 5193 |
| Paket kapısı 1–7 | KARŞILANDI | build 0/0 · paket testleri yeşil · kendi sayaçları ✓ · 15/15 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 14. Açık kalanlar
1. **Ağaçtaki kırmızılar (bu paketin DEĞİL):**
   - `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` ve `…Kapsam_orani_beyan_edilen_degerle_ayni`. Kök neden yeni istemci tipi `CariVeri`. `Cariler.razor` ve `Islemler.razor` 20:43–20:44'te başka bir şeritçe değiştirildi; sahip adı doğrulanamadı, Peşin Müşteri / `SistemCarisi` hattı olduğu düşünülüyor.
   - 1. koşumdaki 1708 `sistem_carisi` kırmızısı o şeridin migration'ı gelince 2. koşumda kendiliğinden kalktı.
2. **denetim.sh son koşumda 6 ✗ gösteriyor, hepsi başka şeritlerin** (§ 9). Orkestratör kapısında ölçülmeli.
3. **Oturumlu uçtan uca tarayıcı görüntüsü** ajan tarafından alınamadı (parola girilmez). 5199 QA turunda `/ekranlar` rotasının (qa-turu.js artık 47 rota bekliyor) gerçek oturumla görsel kontrolü önerilir.
4. **Karo adları tuvaldekilerden farklı** (S1). Tuval adlarına ("Satış", "Müşteriler", "Kasa kuyruğu", "Kim ne yaptı", "Raporlar" grubu …) geçiş `Sozlukler.cs` sözlük göçü paketinde yapılmalı. Karo ve menü aynı kaynaktan okuduğu için ikisi birlikte değişir.
5. `qa-turu.js` ve `ham/olcum-2026-09-18.json` orkestratör betik ve ölçüm dosyalarıdır. Bu pakette yalnız rota sayısı 46→47 yapıldı: yeni rota eklenince fail-closed kapı onları kırıyordu (`OlcumKapisiTestleri` iki test). Orkestratörün bilgisine.

## 15. Kurucuya / mali müşavire notlar
- **Mali müşavire not yok** (mevzuat hükmü içermez).
- **Kurucuya bir görsel tercih sorusu var:** Tam ekran karo sayfasında (tuval gibi) kur şeridi ve Basit/Profesyonel anahtarı görünmüyor. Tuval böyle çizdiği için birebir uygulandı. İstenirse bu sayfaya da üst bar eklenebilir; bu bir düzen kararıdır ve ekip oyuna sunulabilir.
