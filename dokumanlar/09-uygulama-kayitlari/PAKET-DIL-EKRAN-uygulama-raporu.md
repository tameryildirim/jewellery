# PAKET DIL-EKRAN — uygulama raporu (08.10 · t2 · 5162/5163) — TUR 1 + TUR 2

Durum: **BITTI** (hasımsız paket; kendi kanıtım bağlayıcı). Commit yok. Dosya silme yok. Paket 2 tur aldı (tur 1: ekran/başlık/menü adları; tur 2: tasarım hakemi bulguları — birim, ayar, maden adı, dar ekran).

## TUR 2 — tasarım hakemi bulguları (hepsi kapandı)

| Hakem bulgusu | Kök neden | Çözüm |
|---|---|---|
| ' has' EN'de Türkçe (Panel "13,8760 has", Cariler "0 has", Ürünler, Etiketler, İşlem defteri) | `Bicim.Has` ve `Bicim.HasIsaretli` sabit " has"; `Cariler.HasBicim` kendi sabiti; `Emanet.HasSayi` " has" ile keserdi | `DilEkranSozlugu.HasBirimi(dil)` (TR has · EN fine); `Bicim.Dil` statik, `DilServisi` yükleme/değiştirmede yazar (yalnız tarayıcıda — bUnit paralel testlerine sızmaz; `Bicim.DilYayilimi`); Cariler/Emanet aynı kaynağa bağlandı; `KesifP2` EN BirimHas "fine" |
| '3 adet' EN'de Türkçe | `Bicim.Miktar` sabit " adet" | `DilEkranSozlugu.AdetBirimi(dil)` (TR adet · EN pcs) |
| '22 Ayar' EN'de Türkçe (rozet, Kurlar, seçiciler) | `KurKaynakAdi.AyarAdi` çekirdek Türkçe adı döndürüyordu; 4 yerde `Ayarlar.Bul(x)?.Ad` doğrudan basılıyordu (FisModelleri ×2, FisKalemGirisi, AltinBacagiPaneli) | `DilEkranSozlugu.AyarAdi` (EN 22k · 995 Commercial fine · 925 Silver · 950 Platinum · 999 Palladium · Stainless steel); dört yer tek çeviriciye bağlandı; fiş ürün etiketi Türkçe ürün adı (veri) çekirdek adı söylüyorsa tekrar etmez |
| (ölçümde çıkan, hakemin görmediği aynı sınıf) sunucunun gönderdiği `MadenAdi` ("Gümüş: 90,00 g", Hurda, Emanet, Raporlar, KarZarar, Etiketler) | 22 yerde `x.MadenAdi` ham basılıyordu; `Maden.Ad()` Türkçe | `KurKaynakAdi.MadenAdi(kod[, sunucuAdı])` + `DilEkranSozlugu.MadenAdi` (Gold/Silver/Platinum/Palladium/Steel/Rhodium; tanınmayan kod AYNEN) |
| EN /ekranlar 375 px yatay taşma 47 px | `tuval-kabuk.css` ipucu `white-space: nowrap` | ≤1199 px'te sarılır, kendi satırını kaplar |
| EN /ekranlar 1440 karo adı kırılması (K3_kirik 8) | 163 px karo, 14 px dolgu + 17 px yazı → 133 px alan | ≥1400 px'te dolgu 10 px, yazı 16 px (alan 143 px); EN menü adları kısaltıldı: "Cheques & notes", "Expenses" (Esanlam "Expenses and transfers"), "Scrap & refinery", "Compliance" (Esanlam "MASAK"); arama eşanlamı korundu |

Tur 2'de gerçek Chrome ölçümünün (EN, 375) bulduğu EN'e özgü ek kusurlar (TR'de yoktu) — hepsi kapandı:
- 375 px sekme şeritleri: 2 sütunlu ızgara 170 px hücrede "Payment Method Breakdown" (227 px), "Suspicious Transactions" (193 px), "Expense Categories" taşıyordu (K3 taşan 5) → `app.css` ≤600 px esnek sarma; `toplu5.css` `#gd-sekme-seridi` nowrap → wrap.
- /kurulum 375 adım satırı (K3 kırık 3) → EN metinler kısaltıldı ("{0} rate(s)", "{0} account(s)", "{0} customer(s)", "Go to step").
- /isletme 375 PRO başlığı "Business & Branches" 360 > 351 px → "Shop & Branches".
- /bildirimler PRO 1440 "Run key" sütunu ISO tarih "2026-10-09" tire yerinden bölünüyordu → hücre sınıfı `hucre-tek-satir` (elle tetiklenen satır hariç).
- K5 "TRY": EN etiketlerde ISO kodu "(TRY)" vardı (TR "TL") → "(₺)" ("Amount (₺)", "Money in and out by payment method (₺)", "₺ balance", "₺ equivalent" …; 18 EN etiket).
- Denetim izi detayı: sunucunun yazdığı sabit Türkçe başlıklar ("liste: N kayıt", "kimlik tespitleri listesi", "yedek özeti", "{tür} belgesi görüntülendi", "stopaj listesi indirildi …") yalnız EN GÖSTERİMDE çevrilir, BASİT ve PRO'da (ham kayıt title'da, DB değişmez): `Q105Sozlugu.DenetimCumleleriEn`.

## (A) Önce / sonra (EN modda Türkçe düğüm; gerçek başsız Chrome, 42 rota + sekmeler + her seçicinin açılır listesi, dolu kiracı 12 ürün / 7 cari / 25 hareket)

Tur 1 sonu (boş kiracı): BASİT+EN **2169 düğüm / 172 tekil** → 0; PRO+EN **50 / 38** → 0 (tur 1 raporunun tablosu aşağıda).
Tur 2 (veri dolu kiracı; "önce" = tur 1 kodu, dosya `t2/once-en-BASIT-1440.json`; "sonra" = `t2/son2/s-*.json`):

| Ölçüm (EN) | Önce | Sonra |
|---|---|---|
| Birim/ad düğümü (has, adet, Ayar, Gümüş; veri hariç) BASİT 1440 | **9 düğüm** ("0 has" ×7, "On deposit 9,1600 has", "Gümüş: 90,00 g (83,25 g pure)") + hakemin gördüğü Ürünler/Etiketler/Kurlar/Siparişler satırları | **0** |
| Türkçe harfli/sözcüklü UI düğümü, 8 koşum (1440+375 × BASİT+PRO × açık+koyu), veri/marka hariç | — | **0** (BASİT 4/4 koşumda 0; PRO'da yalnız denetim izi ham-kayıt `title` ipuçları, aşağıda) |
| Seçici açılır listesi (BASİT 38 · PRO 78 seçenek/koşum) Türkçe ya da birim | — | **0** |
| `yatay` EN /ekranlar 375 | 47 | **0** |
| K3_kirik EN /ekranlar 1440 | 8 | **0** |
| K3_tasan/kirik EN 375 (gider 3, uyum 1, satis-analizi 1, kurulum 3, isletme 1) | 9 | **0** |
| K5_TRY EN | 3 (islem-defteri 1, satis-analizi 2) | **0** |
| TR modda İngilizce kalıntı (BASİT+PRO, 1440) | — | **0** |

Not (kendi hatam, kayda): ilk 10 koşumluk matris tarayıcı önbelleğindeki ESKİ CSS ile koştu (profil önceden açılmıştı) ve CSS düzeltmelerini göremedi; geçersiz sayıldı, `Network.setCacheDisabled` ile TAZE profil + önbellek kapalı yeniden koşuldu (`son2/`). Sonuçlar yalnız son koşumdan.

Ölçüm matrisi (`son2/s-*.json`, her satır 42 rota; sayaçlar: K1 gizli-görünen · K3 taşan/kırık · K5 TRY · K7 hizasız · K8 jargon · K9 binen · K10 elipsis · K11 hata bandı · K12 ezik · K13 kırpılan · yatay):

| Koşum | İhlal |
|---|---|
| EN BASİT 1440 açık / koyu | **0 / 0** |
| EN BASİT 375 açık / koyu | **0 / 0** |
| EN PRO 1440 açık / koyu | K12_ezik: /cariler 1, /urunler 1 (TR'de de AYNEN var — önceden var, EN'e özgü değil) |
| EN PRO 375 açık / koyu | **0 / 0** |
| TR BASİT 1440 | K8_jargon /isletme 1, K3_kirik /ekranlar 2 ("Gider ve para aktarımı" TR adı; tur öncesinden var) |
| TR PRO 1440 | K12_ezik /cariler 1, /urunler 1, K3_kirik /ekranlar 2 (aynı önceden var) |

Sınıflama — **veri / bilinçli** (çevrilmez): kullanıcı/cari/ürün/şube adları ("Kemal Ardıç", "22 Ayar Burma Bilezik" ürün ADI), "Peşin Müşteri" (sistem carisi), "Merkez Şube" (kayıtta yazılı varsayılan şube adı), "İstanbul" (hava durumu şehri), "Altınkaynak" (kur kaynağı marka adı), "Türkçe" (dilin kendi adı), "ByGatech" (marka), destek künye satırı, **PRO denetim izi `title` ipuçları** ("liste: 25 kayıt · islem-defteri" — ham defter kaydı, denetçi için BİLİNÇLİ ham; ekranda görünen metin çevrilidir).

## Ekran görüntüleri (Read 4/4): `/private/tmp/kuyumcu-serit/DIL-EKRAN/t2/foto/`
1. EN Tüm ekranlar BASİT 1440 (`en-_ekranlar-BASIT-aydinlik-1440.png`): 8 sütun, 39 karo, "Cheques & notes", "Expense vouchers", "Overdue tracking", "Transaction ledger" tek satır.
2. EN Panel PRO 1440 koyu (`en-_-PRO-karanlik-1440.png`): "FINE 6.606 ₺ · GRAM 22K", "0,0000 fine", "Last 7 Days Sales (₺)".
3. EN Ayarlar > Appearance BASİT 1440 (`en-ayarlar-gorunum-BASIT-1440.png`): 8 şema + Star adları İngilizce.
4. EN menü BASİT 375 koyu (`en-menu-BASIT-375-koyu.png`): tüm grup/ekran adları İngilizce.

## Testler
- **Yeni dosya `Kuyumcu.Tests/DilEkran2Testleri.cs`: 15 test** (has/fine·adet/pcs EN-TR, DilServisi yayılımı, Cariler.HasBicim, sözlük birimleri, HER ayar için AyarAdi, maden adı, denetim detayı EN, EN menü adı ≤18 karakter + Esanlam, CSS kilitleri, ham MadenAdi/birim literali yok, dar ekran sekme CSS, Bildirimler ISO tarih sınıfı, EN kurulum/TRY, ham `Ayarlar.Bul(..).Ad` yok + FisHesap etiketleri, DenetimIzi PRO+BASİT çeviri). `DisableParallelization` koleksiyonu (`Bicim.Dil` statik; her test geri alır). Toplam DilEkran* = 37 test (tur 1: 22 + tur 2: 15).
- `DilEkranTestleri.cs`: xUnit2008 uyarısı giderildi (tur 1'den kalan).
- `Toplu5Testleri.cs` (başka şeridin testi) `T5_Gider_sekme_seridi_dar_ekranda_TEK_satir`: tek assertion güncellendi (`flex-wrap: nowrap` → `wrap`; TR adlar yine tek sıraya sığar, EN uzun adlar sarar). Gerekçe: aksi hâlde EN 375'te yazı sekmeden taşıyordu. T5 sahibine bilgi.
- Filtreli: DilEkran* 37/37 yeşil.
- **Tam süit (tur 2, SON kodla bir kez; `KUYUMCU_TEST_DB=kuyumcu_test_t2`, trx `/private/tmp/kuyumcu-serit/trx-pDIL-EKRAN/tur2son/`)**: toplam 11364 · geçti 11358 · atlanan 3 · **başarısız 3** — üçü de BAŞKASININ: `OlcekTekTestleri.OL16_mod_yuzu_sinifinda_important_OLCU_yok` ve `OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` (sahibi YILDIZ/t5: `tema-yildiz.css` `.tuval-tezgah .islem-alt-satir > .btn { border-radius: 999px !important }` ve padding envanteri; dosyaya DOKUNMADIM) · `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` (Yasal şeridi, sunucu; `Kuyumcu.Api`'ye DOKUNMADIM). Bu paketin YENİ kırmızısı: **0**. (Aynı koşumun daha önceki tam süiti: 11351 test, 9 başarısız; 1'i benim T5 testi — yukarıda düzeltildi; 8'i başkalarının (YILDIZ Y3_10/Y3_27, GOC-C GCS04/GCH01/03/04/06, Yasal4 Mutabakat); diğer şeritler ilerledikçe son koşumda başkalarının 3'ü kaldı.)
- Build: `dotnet build Kuyumcu.sln` **0 hata 0 uyarı** (son temiz build, mutasyon sonrası).

## Mutasyon tablosu (tur 1: M1–M11, aşağıda; tur 2: M12–M31; hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M12 | `Bicim.Has` birimi sabit "has" | `Has_ve_Miktar_birimi_EN_modda_fine_ve_pcs_TR_modda_has_ve_adet` |
| M13 | `Bicim.Miktar` birimi sabit "adet" | aynı test |
| M14 | `AyarAdi` K22 → çekirdek ad | `Ayar_adi_TR_cekirdek_adi_EN_Turkce_harf_ve_Ayar_icermez_HER_ayar_icin` |
| M15 | `MadenAdi` ALTIN EN "Altın" | `Maden_adi_EN_modda_Ingilizce_TR_modda_cekirdek_ad_tanınmayan_kod_AYNEN` |
| M16 | `Cariler.HasBicim` sabit "has" | `Cari_ekraninin_kendi_has_bicimleyicisi_birimi_dilden_alir` + `Razor_kaynaginda_ham_MadenAdi_ve_birim_literali_yok_…` |
| M17 | EN `GiderVirman` adı "Expenses and transfers" | `EN_menu_adlari_gunluk_is_disindaki_karolarda_en_cok_18_karakter` |
| M18 | CSS ipucu `nowrap` geri | `Tuval_css_dar_ekranda_ipucunu_sarar_ve_8_sutunda_karo_dolgusunu_daraltir` |
| M19 | `liste: N records` → "liste: N kayıt" | `Denetim_detayi_BASIT_EN_gosterimde_liste_basi_ve_sabit_gerekceler_Ingilizce` |
| M20 | `DilServisi.DegistirAsync` yayılımı silindi | `DilServisi_dil_degisince_Bicim_Dil_i_yayar_yalniz_yayilim_acikken` |
| M21 | `Emanet.HasSayi` birimi sabit " has" | `Razor_kaynaginda_ham_MadenAdi_ve_birim_literali_yok_Emanet_has_sayisi_birimi_dilden_keser` |
| M22 | `Raporlar` `md.MadenAdi` ham | aynı test (+ denetim sayacı verir) |
| M23 | EN `Satis` adı "Satış" (menü anahtarı EN→TR) | `Her_menu_ekraninin_EN_adi_TR_adindan_FARKLI…`, `Kenar_cubugu_EN_modda…(BASIT)`, `(PROFESYONEL)`, `Ust_bar_basliklari_EN_modda…` |
| M24 | `app.css` ≤600 sekme şeridi ızgaraya geri | `Dar_ekranda_sekme_seridi_esnek_sarar_ve_gider_seridi_de_sarar` |
| M25 | Bildirimler anahtar hücre sınıfı eski | `Bildirimler_calisma_anahtari_ISO_tarihi_tire_yerinden_bolunmez_elle_tetiklenen_satir_haric` |
| M26 | EN "{0} rate record(s)" geri | `EN_kurulum_adim_metinleri_kisa_ve_ekranda_ISO_para_kodu_TRY_yok` |
| M27 | EN "Only TRY movements" geri | aynı test |
| M28 | EN /isletme başlığı "Business & Branches" geri | aynı test |
| M29 | `FisHesap.AyarSecenekleri` ham `Ad` | `Web_kaynaginda_ham_ayar_adi_yok_secici_ve_etiketler_KurKaynakAdi_AyarAdi_kullanir` |
| M30 | Q105 "yedek özeti" çevirisi silindi | `Denetim_detayi_BASIT_EN_gosterimde_…` |
| M31 | `DenetimIzi.DetayMetni` EN çevirisi atlandı | ilk koşumda HİÇBİR test kırmızı olmadı → TEST AÇIĞI: `Denetim_izi_ekrani_detay_metnini_BASIT_ve_PRO_modda_EN_cumle_cevirisinden_gecirir` eklendi, mutasyon tekrarlandı → kırmızı, cmp AYNI |

M16 ilk denemesi pattern hatası (derleme hatası; eski ikili çalıştı) → pattern düzeltilip tekrarlandı. Mutasyon yedekleri `/private/tmp/kuyumcu-serit/DIL-EKRAN/t2/mut-yedek/`.
Tur 1 mutasyonları M1–M11 (menü kök nedeni, KurAdi, dil.js, PageTitle, SunucuMetni, Kayıtlı adet, TCKN, IsAdi, Kur.razor) tur 1 raporundaki gibidir ve tam süitte yeşildir.

## Denetim
`bash dotnet/denetim.sh` (zsh DEĞİL): **DENETİM BAŞARISIZ yalnız şu satırlarla**: `P-30 DEMO_INSAN_SURESI` (görev gereği hariç) ve `OLCEK-1: YUZ_ONEMLI_OLCU 1` + `YUZ_OLCU_ENVANTER_DISI 5` — ikisi de `tema-yildiz.css` (YILDIZ şeridi, t5) kaynaklı, bu paketin dosyası değil. **`DIL-EKRAN: EN_MODDA_TURKCE_BASLIK = 0`** (✓). Sayaç tur 2'de genişledi: razor işaretlemesindeki sözlük dışı Türkçe başlık/title/aria + çıplak birim literali ("… has"/"… adet" ile biten dizge) + ham sunucu `X.MadenAdi` + ham `Ayarlar.Bul(..).Ad`; yalnız AZALIR, tavan 0, ölçülemezse ✗. Genişletilmiş sayacın tur-2 başlangıç değeri kayda alınmadı (düzeltmeler sayaçtan önce yazıldı); mutasyonlar (M16, M21, M22, M29) sayacın doğru yakaladığını gösterir.

## Canlı doğrulama (API 5162, vekil 5163/5164, gerçek başsız Chrome; tarayıcı bölmesi KULLANILMADI)
- Kiracılar: `PAKETDIL-EKRAN-TEST` (boş, `01M4EKF4VXF19YV8PWZSRQWZ95`) ve `PAKETDIL-EKRAN-VERI-TEST Kuyumculuk` (`01M4EKFP134XWTHVPAFJEN1PPZ`, `demo-tohumla.js --prova-kiraci` ile ürünün KENDİ uçlarından tohumlandı: HTTP 201 davet + 201 kayıt; sistem anahtarı tek kullanımlık, 600 yetkili dosyada, ekrana yazılmadı).
- SQL: başlangıç `tenants`=18; test kiracıları açıkken 20; veri kiracısı `cariler=7 urunler=12 hareketler=25 kullanicilar=1`; ürün ayarları K22×7 · G925×2 · K24×1 · K14×2 (EN ekranda 22k/925 Silver/24k/14k görünen satırların kaynağı). `kiraci-temizle.sh <id> --uygula` ile ikisi silindi → `tenants`=**18**, `PAKETDIL%` = 0, kullanıcı/ürün satırı 0. 5162/5163/5164 dinleyicisi yok (yalnız kendi PID'lerim durduruldu); 5199/5200'e dokunulmadı.
- Ölçüm araçları: `t2/olc.mjs` (sikisma.js + Türkçe düğüm taraması + seçici listeleri; CDP 9906, önbellek kapalı), `t2/foto4.mjs`, `t2/mut.sh`.

## Değişen dosyalar (tur 2; tur 1 listesi altta)
Core: `I18n/DilEkranSozlugu.cs`, `Sozlukler.cs` (AdimaGit, Isletme.Baslik, TRY→₺), `Sozlukler.Menu.cs`, `Sozlukler.KesifP2.cs`, `Sozlukler.Q105.cs`, `Sozlukler.RaporGrafik.cs`, `Sozlukler.Goc47B.cs`, `Sozlukler.Toplu4.cs`, `Sozlukler.Toplu5.cs`, `Sozlukler.Yasal4.cs`, `Sozlukler.ZoBelge.cs`, `Sozlukler.FisDiger.cs`.
Web: `Servisler/Bicim.cs`, `Servisler/DilServisi.cs`, `Bilesenler/KurKaynakAdi.cs`, `Bilesenler/AltinBacagiPaneli.razor`, `Bilesenler/Fis/FisModelleri.cs`, `Bilesenler/Fis/FisKalemGirisi.razor`, `Pages/{Cariler,Emanet,Hurda,KarZarar,Raporlar,UrunEtiketleri,Urunler,SiparisDetay,DenetimIzi,Bildirimler}.razor`, `wwwroot/css/{tuval-kabuk,app,toplu5}.css`.
Test: `DilEkran2Testleri.cs` (yeni), `DilEkranTestleri.cs`, `Toplu5Testleri.cs` (tek assertion).
Betik: `dotnet/denetim.sh` (yalnız EN_MODDA_TURKCE_BASLIK sayacı).
Tur 1: `Core/I18n/{DilEkranSozlugu.cs (yeni), Sozlukler.cs, Sozlukler.Menu.cs, Sozlukler.KimlikNo.cs}`, `Web/Layout/RotaBasliklari.cs`, `Pages/{Kur,Bildirimler,Uyum,Isletme,Raporlar,Kilavuz,Urunler,Kurulum,VeriDisaAktarim,VeriIceAktarim,Yedekler}.razor`, `Bilesenler/Uyum/YasalListePaneli.razor`, `wwwroot/js/dil.js`, `Tests/DilEkranTestleri.cs`. Migration: **gerekmedi**. `Kuyumcu.Api` dosyalarına DOKUNULMADI. Başka şeridin alanına en küçük dokunuşlar: `Bildirimler.razor` (GOC-B; tek hücre sınıfı), `Toplu5Testleri.cs` (T5; tek assertion), `toplu5.css` (T5; tek kural) — sahiplerine devir notu: davranış EN uzun adlar içindir, TR görünümü değişmedi.

## Koşul karşılama
| Koşul | Durum |
|---|---|
| (A) önce N → sonra 0 (veri hariç), BASİT+PRO, tüm menü ekranları, gerçek Chrome | KARŞILANDI (tur 1 + tur 2 tabloları) |
| Hakem bulgusu 1–4 (has, adet, Ayar, /ekranlar 375 + karo kırılması) | KARŞILANDI (ölçüm: yatay 0, K3_kirik 0, birim düğümü 0) |
| TR modda İngilizce kalıntı 0 | KARŞILANDI |
| bUnit: her menü anahtarı EN ≠ TR | KARŞILANDI (istisna "POS" gerekçeli) |
| Mutasyon: menü adı EN→TR kırmızı, cmp AYNI | KARŞILANDI (M2, M23) |
| Denetim yeni sayaç 0 · build 0/0 · denetim TEMİZ (P-30 hariç) | sayaç 0, build 0/0 · denetimde P-30 dışında 2 OLCEK-1 satırı ✗ — **sahibi YILDIZ (tema-yildiz.css)**, bu paketin değil |
| Tam süit 0 YENİ kırmızı | KARŞILANDI (3 kırmızı, üçü başkasının, sahipli) |
| 1440 + 375 · BASİT + PRO · açık + koyu sayısal ölçüm | KARŞILANDI (8 EN + 2 TR koşum × 42 rota) |
| Ekran görüntüsü Read ≤ 4 | 4 |
| Para/veri/yasal davranışa dokunmama | KARŞILANDI — yalnız görünen birim/ad/etiket; sayı biçimi tr-TR sabit; EN yasal cümlelerde "TRY" yalnız `NakitKapali` ("30,000 TRY threshold") ve `MarjAralik` kaldı (yasal eşik/limit cümleleri; ölçümde ekranda görünmedi; dokunulmadı) |
| Mevzuat | Hiçbir yasal kural/eşik/oran eklenmedi ya da değiştirilmedi |

## Açık kalanlar
- **Başkalarının kırmızıları (tam süit):** `OlcekTekTestleri.OL16`, `OL26` + denetim `OLCEK-1` ×2 — sahibi YILDIZ (`tema-yildiz.css` `!important` + padding envanteri); `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` — sahibi Yasal şeridi.
- EN `NakitKapali` ve `MarjAralik` cümlelerinde ISO "TRY" kaldı (yasal eşik metni; hasımlı pakete devir).
- PRO ölçüm kalıntısı `K12_ezik` (/cariler, /urunler 1'er) ve TR `K3_kirik` /ekranlar ("Gider ve para aktarımı"), TR `K8_jargon` /isletme: önceden var, TR'de de aynı → dil paketinin dışı; sahibi görünüm şeridi.
- Kılavuz belgesi gövdesi (`wwwroot/kilavuz.html`) yalnız Türkçe; EN'de bölüm bağlantıları İngilizce ama belge Türkçe (tur 1'den).
- API hata/doğrulama mesajları sunucuda Türkçe üretilir ve çoğu ekranda ham basılır (hata kodu→sözlük eşlemesi ayrı paket); tarama boş/örnek veride bu mesajları üretmedi.
- Sistem varsayılan adları ("Merkez Şube", "Peşin Müşteri") DB verisidir (kiracıya dil yazıp varsayılanı o dilde üretmek kurucu kararı).
- Taş ibaresi seçenekleri (değer = etiket, yasal ibare) hasımlı pakete devir (tur 1'den).
- Genişletilmiş denetim sayacının tur-2 başlangıç değeri kayda alınmadı (yukarıda).

## Kurucuya / mali müşavire not
Yok (mevzuat metni değişmedi; yalnız görünen birim/ad/etiket çevrildi).

---
# TUR 1 (özet — ayrıntı bu dosyanın önceki sürümünde)
**Kök neden:** `MenuAdlari.Icin(Sozluk t)` dili `ReferenceEquals(t, Sozlukler.En)` ile anlıyordu; BASİT mod sözlüğü PRO'nun KOPYASI olduğundan kimlik değişir → BASİT+EN'de menü, "Tüm ekranlar" karoları, gruplar, favori düğmeleri TÜRKÇE kalıyordu. Çözüm: `Sozluk.DilKodu` (kopyada korunur); aynı kalıp `KimlikNoSozlugu` ve `RotaBasliklari`'nda da kapatıldı. İkinci kusur: `<html lang>` yalnız dil DÜĞMESİNE basınca değişiyordu (EN tercihte `lang=tr` kalıyor, CSS uppercase "DAİLY WORK") → `dil.js` yüklemede uygular.
**Tur 1 (A) tablosu:** BASİT+EN 2169 düğüm / 172 tekil → 0; PRO+EN 50 / 38 → 0; document.title adı eksik 4 ekran → 0; `<html lang>` EN tercihte `tr` → `en`.
**Tur 1 kapatılan noktalar:** menü/karo/grup/favori/sürükle etiketi/kenar çubuğu önizlemesi; Kur kalemi adları (22 kod); zamanlanmış iş adları (6); sunucu cümleleri (Saklama, plan, FARK tanımı); yaptırım dayanağı; Kılavuz bölüm başlıkları; Ürünler "Kayıtlı adet"; 4 ekranda eksik `PageTitle`.
**Tur 1 mutasyonları M1–M11:** M1 `MenuAdlari.Icin` ReferenceEquals · M2 EN `Satis` "Satış" · M3 `KurAdi` EN kapalı · M4 `dil.js` lang · M5 `Kurulum.razor` PageTitle · M6 `/ekranlar` başlığı TR · M7 `SunucuMetni` EN kapalı · M8 Urunler "Kayıtlı adet" · M9 `KimlikNoSozlugu` ReferenceEquals · M10 Bildirimler `IsAdi` atlandı · M11 `Kur.razor` `KurAdi` çağrıları — hepsi ilgili `DilEkranTestleri` testlerini kırmızı yaptı, `cmp` AYNI.
