# PAKET S1-g — Menü ve üst bar tuvale uyum · uygulama raporu

**Tarih:** 2026-09-24 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Durum:** BİTTİ (7 madde kanıtlı; açık kalanlar § 11)
**Kaynak:** kurucu 24.09 06:55 "menüyü felan kontrol et, olmamış" · tek doğruluk kaynağı `dokumanlar/02-mimari/sadelestirme/tuval/project/` (Panel.dc.html, Profesyonel.dc.html, Tezgah.dc.html, Ekranlar.dc.html, canvas.json).
**Migration:** gerekmedi (şema değişikliği yok). **Commit:** yok.

## 1. Spesifikasyon ↔ kod doğrulaması (ölçülen sapmalar hâlâ vardı mı?)

| # | Sapma (orkestratör ölçümü) | Kodda doğrulandı mı | Kök neden |
|---|---|---|---|
| 1 | PRO'da kapalı grupların 33 satırı `hidden` taşıyor ama görünüyor | EVET | `.kenar-satir{display:flex}` (app.css) UA `[hidden]{display:none}` kuralını eziyor; bUnit yalnız özniteliği ölçüyordu |
| 2 | Basit modda 8 satırlık grupsuz liste + "Sırayı sıfırla" | EVET | `KenarCubugu` başlığı `if (!Modu.BasitMi)` içinde; `Gorunur` = yetki **VE** `Modu.MenudeGorunur` (BasitMenu 8 yol) |
| 3 | Ekran adları tuvalle uyuşmuyor | EVET | Menü adları hub `Sozlukler.cs` `Nav.*` (P-14 adları: Cariler, Hareket Dökümü, Denetim İzi…) |
| 4 | Her satırda yıldız + `⠿` | EVET | `YildizDugmesi(oge)` + `<span class="kenar-tutamac">⠿</span>` her satırda |
| 5 | Kullanıcı kartı | kısmen | baş harf+ad+rol vardı; PRO'da şube yoktu |
| 6 | Üst bar "Şube kapsamı" iki kez; etiketsiz `⟳` | EVET | SubeSecici'de küçük etiket + Secici'nin kendi `<label>`'ı; `⟳` metin karakteri |
| 7 | Altın pozisyonu kırılımı kırılıyor | EVET | `satir-arasi` (space-between) + Alacak/Borç `dugme dugme-kucuk` rozeti |

## 2. Yapılanlar

1. **Genel `[hidden]` kuralı** — `app.css` başına `[hidden] { display: none !important; }` (+ gerekçe yorumu). Aynı sınıf hatanın başka bileşenlerdeki örneklerini de kapatır (canlı ölçüm: tüm sayfada `hidden` olup görünen öğe **0**).
2. **Basit modda 6 katlanır grup** — başlık her modda çizilir; satır `hidden="@(!acik)"`; menü süzgeci yalnız YETKİ (`MenuTanimi.YetkiliMi`). Mod süzgeci (`ArayuzModuServisi.MenudeGorunur`) kaldırıldı; menü araması (`GlobalArama`) da aynı süzgeçle (menüden dar arama olmasın — UX-S22). Yetki süzgeci aynen durur (M-UX3-06). `BasitMenu.cs` SİLİNMEDİ; artık menüyü daraltmaz, basit çekirdeği belgeler ve `BASIT_MOD_TUTARSIZ` sayacını besler.
3. **Tuval adları** — yeni sözlük `Kuyumcu.Core/I18n/Sozlukler.Menu.cs` (`MenuAdlari`, TR+EN, konumsal record → eksik ad derleme hatası). Her ekranın **tam adı** (menü/karo/sayfa başlığı), **kısa adı** (kapalı grup önizlemesi: Vezne · POS · Çek-senet · Gider · Hurda · Vade · Satış · Kasa · Yedek…) ve **görünmeyen arama eş anlamlısı** (eski ad: "Cariler", "Denetim izi" — eski adı bilen kullanıcı aramada ekranı bulur). `MenuOgesi` imzası `Adi` işlevini taşır; `Etiket` özelliği eski imzayla (`oge.Etiket(Dil.T)`) çalışır. Grup başlıkları cümle düzeni; CSS `text-transform: none`. `RotaBasliklari` menüdeki 40 rota için menü adını döndürür. Hub `Sozlukler.cs`'e dokunulmadı.
4. **Yıldız/tutamaç menüden kalktı** — favori ekleme "Tüm ekranlar"daki yıldızda (#ekran-favori-*, mevcut); PRO'da menünün en üstünde "Favoriler" (favori varsa), basit yüzde yok (tuval). Sıralama özelliği SİLİNMEDİ: satırın kendisi sürüklenir (siralistesi.js `data-sira-anahtar`) + Alt+↑/↓. "Sırayı sıfırla" yalnız kayıtlı sıra varken çizilir (`MenuTercihServisi.OzelSiraVar`, salt okunur yeni özellik); "Grupları sıfırla" her modda (kayıtlı katlama varken).
5. **Kullanıcı kartı** — baş harf + ad + rol; PRO'da ` · <şube adı>` (`SubeSecici KartIci="true"`, #ust-sube-sabit id'si korundu). Şube adı ancak biliniyorsa yazılır (liste okunamazsa tahmin YOK).
6. **Üst bar** — SubeSecici üst bar yüzeyi yalnız gerçek seçim varken (çoklu şube + tüm-şube rolü) çizilir, TEK etiketle; tek şubede hiçbir şey. `⟳` kalktı: yetkiliye saat metninin kendisi düğmedir (`#ust-kur-saat` button, `aria-label` "Kuru güncelle — HH:mm", `aria-describedby` açıklama; title S1-c sözleşmesindeki tam tarih). Yetkisizde saat düz metin.
7. **Altın pozisyonu kırılımı** — `.pozisyon-kirilim` (flex-wrap, sabit column-gap) + her öğe `.pozisyon-kirilim-oge` (nowrap, aynı biçim, bağlantı görünümü). Davranış (bağlantı/düğme, UX-S23) aynı.
8. **Kılavuz** — `KilavuzUretimTestleri` kılavuz başlığının menü adını taşımasını zorunlu tutuyor: `docs/KULLANIM_KILAVUZU.md` 15 başlık yeni ada çevrildi (çapa id'leri aynı), `wwwroot/kilavuz.html` `KILAVUZ_YAZ=1` ile yeniden üretildi.
9. **denetim.sh** — P-14 MENU_AYNI_AD sayacı yeni sözlükten okur ve genel-ek atmayı bıraktı (§ 3); yeni `# >>> S1-g` bloğu (MENU_TUVAL_UYUM).

## 3. Sapmalar ve gerekçeler

- **MENU_AYNI_AD tanımı değişti (P-14).** Tuval "Kasalar / Kasa raporu", "Vade takvimi / Vade takibi", "POS cihazları / POS alacakları" adlarını birlikte kullanıyor; P-14 ölçüsü (genel ek sözcükleri atan `Sadelestir`) bunları çarpışma sayıyordu. Kurucu kuralı gereği tuval geçerli olduğundan ölçü `MenuTanimi.AyirtEdiciAd` oldu: katlama + çoğul eki kırpma, genel ekler korunur. Kural hâlâ **0**: "Kasalar↔Kasa", "İşlemler↔İşlem" gibi gerçek çarpışmalar yakalanır (testte kilitli).
- **PRO'da kapalı grup önizlemesi durdu.** Profesyonel.dc.html'de PRO kapalı grupta yalnız "6 ▸" var; ancak görev "önizleme tüm ekran adlarını yazar" testinin yeşil kalmasını istedi ve "menüde hiçbir ekran kaybolmadı" kuralı bunu destekliyor. Önizleme iki modda da var.
- **Grup başlığı büyük harf değil.** Panel.dc.html açık grup başlığını büyük harfle çiziyor. Görev metni ve Profesyonel.dc.html cümle düzenini söylüyor; görev metnine uyuldu.
- **Dosya alanı dışında dokunulanlar (zorunlu):** `SubeSecici.razor` (sapma 6), `GlobalArama.razor` + `ArayuzModuServisi.cs` (mod süzgeci), `MenuTercihServisi.cs` (salt okunur `OzelSiraVar`), `Pages/Ekranlar.razor` (arama eş anlamlısı), `docs/KULLANIM_KILAVUZU.md` + `wwwroot/kilavuz.html` (test kilidi), `denetim.sh`. `belirtecler.css`'e dokunulmadı (genel kural app.css başında). P-78 dosyalarına dokunulmadı.
- **Ekran görüntüleri:** tarayıcı bölmesinde 1280×800 alındı (PRO ve Basit Panel). Araç görüntüyü diske yazmıyor, bu yüzden dosya yolu verilemiyor. Sayısal kanıtlar § 7'de.

## 4. Dokunulan sayfalar (TEK DOKUNUŞ)
`Pages/Ekranlar.razor` (yalnız arama satırı). Bileşen/düzen: `Layout/KenarCubugu.razor`, `Layout/UstBar.razor`, `Bilesenler/SubeSecici.razor`, `Bilesenler/GlobalArama.razor`, `Bilesenler/AltinPozisyonuKarti.razor`.

## 5. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Başlangıç (07:0x, değişiklik öncesi, tam süit) | 4946 | 4941 | 5 (B4AltinBacagi…Oran_Varsa_Onerilir, P16 ×2, UX4b Manuel_kur…GBP, UrunEkran PIRLANTA… — hepsi başka şeritlerin) |
| Son tam süit (trx `scratchpad/trx-pS1-g/_Mac_2026-09-24_07_17_18.trx`) | **5018** | **5018** | **0** |

Yeni test: **17** (S1gMenuTuvalTestleri 15 · S1cUstBarTestleri.S1g_kur_guncelle_SAATIN_kendisidir_etiketsiz_ikon_YOK · AltinPozisyonuKartiTestleri.S1g_kirilim_ogeleri_AYNI_bicimde_rozet_metne_karismaz). Bu paket yeni kırmızı ÜRETMEDİ (fark 0; başlangıçtaki 5 kırmızı son koşumda başka şeritlerce kapanmıştı).

**Yeniden yazılan testler (silinmedi) — eski → yeni, gerekçe:**

| Test | Eski | Yeni | Tuval kanıtı |
|---|---|---|---|
| MenuKatlamaTestleri.Basit_modda_grup_basligi_CIZILMEZ_ve_satirlar_katlanmaz | basitte başlık 0, satır katlanmaz, 8 bağlantı | **Basit_modda_da_ALTI_grup_basligi_cizilir_ve_kapali_gruplar_katlanir**: 6 başlık, yalnız Günlük iş açık, 38 bağlantı DOM'da, 5 satır açık | Panel.dc.html 32-43, Tezgah.dc.html 33-45, canvas.json n2 |
| MenuKatlamaTestleri.Tum_ekranlar_sayisi_BASIT_MODDA… (son assert) | menü yine 8 | menü = yetkili ekran sayısı | Panel.dc.html 30 "(38)" |
| MenuMimarisiTestleri.Basit_modda_BOS_grup_basligi_cizilmez | basitte başlık 0 | yetkisiz TEZGAHTAR'da boş grup başlığı yok | Tezgah.dc.html |
| MenuMimarisiTestleri.MENU_AYNI_AD_sifir_TR… | Sadelestir | AyirtEdiciAd (§ 3) | Ekranlar.dc.html |
| MenuMimarisiTestleri.Dort_etiket_carpismasi_COZULDU | hub Nav çiftleri | menü adları + gerçek çarpışma kilidi | Ekranlar.dc.html |
| MenuMimarisiTestleri.Eski_carpisan_adlar_GERI_GELMEZ | "İşlem Defteri, Kasa Defteri, Vade Takibi, POS Alacakları" | "İşlemler, Kasa Defteri, Cariler, Hareket Dökümü, Kasa Hareketleri, POS Hakedişleri, Gecikmiş Alacaklar, Denetim İzi" | tuval 3 adı geri getirdi |
| MenuMimarisiTestleri.Cizilen_etiketler_menu_tanimiyla_AYNIDIR | hub Nav adları | MenuAdlari + eski adlar yok | Ekranlar.dc.html |
| BasitModMenuTestleri.Basit_modda_menu_YALNIZ_BasitMenu_listesidir | 8 yol | **Basit_modda_menu_YETKILI_HER_ekrani_tasir_BasitMenu_ile_daralmaz** | Panel.dc.html 30-43 |
| BasitModMenuTestleri.Basit_modda_profesyonel_ekranlar_menude_YOKTUR | 16 yol yok | **…menude_KAYBOLMAZ_kapali_grupta_durur** | Panel.dc.html 38-43 |
| BasitModMenuTestleri.Yetkili_TEZGAHTAR_basit_modda… | kur/emanet yok | kur/emanet var; yetkisiz banka/fatura yok | Tezgah.dc.html 33-39 |
| P37RevizyonTestleri.Suruklenebilir_her_satirda_tutamac_ve_KLAVYE_yolu_vardir | her satırda `.kenar-tutamac` | **Suruklenebilir_her_satir_KLAVYE_yolu_tasir_tutamac_ve_yildiz_GORUNMEZ** | Panel/Profesyonel.dc.html |
| P37RevizyonTestleri.Menude_kalici_paragraf_SIFIRDIR (son assert) | #kenar-sira-sifirla her zaman | kayıtlı sıra yokken yok (varken: S1g testi) | tuvalde düğme yok |
| KabukAramaEkranTestleri.Tek_subede… / Sube_kilitli_rol… | salt okunur satır üst barda | üst barda yok, kartta şube adı | Profesyonel.dc.html 52 |
| KabukAramaEkranTestleri.Coklu_subede… | seçici var | + "Şube kapsamı" tek kez | — |
| YedekEkranTestleri.Menu_SahipDisindaGorunmez_BasitModdaGizli | basit SAHİP'te yok | **…BasitModdaDaGorunur** (MÜDÜR'de yine yok) | Panel.dc.html 43 |
| RotaBaslikTestleri.Dil_TR_den_EN… · S1dEkranlarTestleri.Ingilizce… · KilavuzUretimTestleri.Kilavuz_ekrani_ekran_adlarini… | hub Nav adı | MenuAdlari adı | Ekranlar.dc.html |

## 6. Mutasyon kanıtı (yedek `scratchpad/mut-yedek/S1-g/`, her biri `cmp` ile birebir geri alındı)

| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| MS1g-1 (a) | app.css'ten genel `[hidden]` kuralı silindi | S1gMenuTuvalTestleri.Genel_hidden_kurali_app_css_BASINDA_ve_IMPORTANT · denetim.sh `✗ S1-g: MENU_TUVAL_UYUM` · **tarayıcı**: CSSOM'dan kural silinince görünen satır 11 → **38** | cmp AYNI |
| MS1g-2 (b) | basit modda grup başlığı çizimi kapatıldı (`if (!Modu.BasitMi)`) | MenuKatlamaTestleri.Basit_modda_da_ALTI_grup…, BasitModMenuTestleri.Basit_modda_profesyonel_ekranlar_menude_KAYBOLMAZ…, MenuMimarisiTestleri.Basit_modda_BOS_grup_basligi_cizilmez | cmp AYNI |
| MS1g-3 (c) | "Müşteriler" → "Cariler" (eski ad) | S1gMenuTuvalTestleri.Menu_adlari_TUVALLE_BIREBIR…, S1gMenuTuvalTestleri.Eski_ad_ile_arama…, MenuMimarisiTestleri.Eski_carpisan_adlar_GERI_GELMEZ | cmp AYNI |
| MS1g-4 (d) | menü satırına yıldız düğmesi eklendi | S1gMenuTuvalTestleri.Menude_yildiz_ve_tutamac_SIFIR (BASIT, PROFESYONEL), P37RevizyonTestleri.Suruklenebilir_her_satir… | cmp AYNI |
| MS1g-5 | SubeSecici'ye ikinci "Şube kapsamı" etiketi geri kondu | KabukAramaEkranTestleri.Coklu_subede_TUM_SUBELER_rolu_secici_gorur (ilk koşum artımlı derlemenin eski ikiliyi kullanması yüzünden YEŞİL çıktı; hata ayıklamada iki etiketin çizildiği görüldü, yeniden koşumda KIRMIZI) | cmp AYNI |
| MS1g-6 | Alacak yeniden `dugme dugme-kucuk` | AltinPozisyonuKartiTestleri.S1g_kirilim_ogeleri_AYNI_bicimde… | cmp AYNI |
| MS1g-7 | önizleme kısa ad yerine tam ad | S1gMenuTuvalTestleri.Kapali_grup_onizlemesi_tuvalin_KISA_adlarini… | cmp AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

## 7. Canlı doğrulama (API 5178 · vekil 5177 · kendi sekmem tab-12, 1280×800)
Kiracı **PAKETS1-g-TEST** (`01M38SQMS97D2QADWJZ42DMYWH`): `POST /sistem/davetler` **201** (kendi rastgele sistem anahtarımla, değer yazılmadı) → `POST /auth/kayit` **201**. Tek şube ("Merkez Şube").

| Adım | Beklenen | Gerçek (getComputedStyle) |
|---|---|---|
| PRO, `/` | görünen `.kenar-satir` = 5, hidden-ama-görünen 0 | toplam 38 · **görünen 5** (Panel, Satış, Emanet, Sipariş ve tamir, Bildirimler) · **hidden-görünen 0** |
| PRO grup başlıkları | 6, Günlük iş açık | "Günlük iş\|true", diğer 5 "\|false" |
| Yıldız / tutamaç | 0 / 0 | **0 / 0** |
| Kart (PRO) | RA · ad · "Sahip · şube" | "RA / Ramazan Akkocan / Sahip · Merkez Şube" |
| Üst bar "Şube kapsamı" (tek şube) | 0 (seçici yok) | **0** |
| `⟳` | yok; saat düğme + etiket | `#ust-kur-saat` BUTTON, aria-label "Kuru güncelle — 07:10"; açıklama gorsel-gizli (1×1, clip) |
| Basit, `/` | 6 başlık + 5 önizleme, görünen satır 5 | 6 başlık (text-transform none), **önizleme 5**, **görünen 5**, hidden-görünen 0, "Tüm ekranlar (38)", favori bloğu yok, "Sırayı sıfırla" yok, kart "Sahip" |
| Önizleme adları | tuval kısa adları | "Kasalar · Vezne · POS · POS alacakları · Banka · Çek-senet · Vade takvimi · Kur · Gider · Hurda" vb. |
| Altın pozisyonu kırılımı | tek satır, hizalı | Vitrin top 259 h19 14px · Alacak top 259 h19 14px, zemin şeffaf (rozet yok) |
| Rapor grubunu aç | 6 satır daha görünür | 11 satır görünür |
| /ekranlar | tuval adları + yıldız | 38 karo tuval adlarıyla; `#ekran-favori-kur` tıklandı → aria-pressed true |
| PRO, /denetim-izi | Favoriler en üstte; başlık menü adı | ilk çocuk `kenar-grup-favoriler`, satır "Kur"; h1 "**Kim ne yaptı**"; sayfadaki tüm `[hidden]` görünen **0** |
| 375 px | yatay taşma 0 | PRO **0**, Basit **0** |
| 1280 px | yatay taşma 0 | **0** |

SQL kanıtı (`kullanici_tercihleri`, kiracı filtresi): `menu-acik-gruplar ["gunluk","rapor"]` · `menu-favoriler ["/kur"]`.
Kapanış: sekme kapatıldı; vekil + API PID'leri `kill` edildi, 5177/5178 dinleyen yok; `kiraci-temizle.sh --uygula` 38 satır + tenants 1 satır sildi; `kiraci_davetleri`'ndeki kendi davet satırım silindi (1 → 0); **tenants 10 → 10** (başlangıçla aynı). Parola/jeton dosyaları boşaltıldı.

## 8. Denetim
`bash dotnet/denetim.sh` → `✓ P-14: MENU_AYNI_AD 0 (40 etiket)` · `✓ MENU_KAPSAM 0` · `✓ BASIT_MOD_TUTARSIZ 0` · `✓ MENU_ELLE_BAGLANTI` · `✓ S1-g: MENU_TUVAL_UYUM [hidden] kuralı 1 · menü yıldız/tutamaç 0 · üst bar metin ikon 0` · **DENETİM TEMİZ.**

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü yok. Kullanılan kayıtlar mevcut kullanıcı tercihleri: favori (PATCH, Tüm ekranlar yıldızı, geri alma aynı yıldız), menü sırası (sürükle/Alt+↑↓ PATCH; "Sırayı sıfırla" DELETE, kayıtlı sıra varken), grup katlaması (PATCH; "Grupları sıfırla" DELETE). Menü/sayfa adları sözlük verisidir, düzenlenmez.

## 10. Değişen dosyalar
Yeni: `dotnet/Kuyumcu.Core/I18n/Sozlukler.Menu.cs`, `dotnet/Kuyumcu.Tests/S1gMenuTuvalTestleri.cs`.
Değişen: `dotnet/Kuyumcu.Web/Layout/{MenuTanimi.cs,KenarCubugu.razor,RotaBasliklari.cs,UstBar.razor}` · `dotnet/Kuyumcu.Web/Servisler/{BasitMenu.cs,ArayuzModuServisi.cs,MenuTercihServisi.cs}` · `dotnet/Kuyumcu.Web/Bilesenler/{SubeSecici.razor,GlobalArama.razor,AltinPozisyonuKarti.razor}` · `dotnet/Kuyumcu.Web/Pages/Ekranlar.razor` · `dotnet/Kuyumcu.Web/wwwroot/css/app.css` · `dotnet/Kuyumcu.Web/wwwroot/kilavuz.html` (üretildi) · `docs/KULLANIM_KILAVUZU.md` · `dotnet/denetim.sh` · testler: `MenuKatlamaTestleri, MenuMimarisiTestleri, BasitModMenuTestleri, P37RevizyonTestleri, KabukAramaEkranTestleri, RotaBaslikTestleri, S1dEkranlarTestleri, KilavuzUretimTestleri, YedekEkranTestleri, S1cUstBarTestleri, AltinPozisyonuKartiTestleri`.
(Çalışma ağacı paketten önce de kirliydi; `git status --porcelain` farkı: yalnız yeni `Sozlukler.Menu.cs` yeni satır olarak eklendi, diğerleri zaten ` M`.)

## 11. Koşul karşılama ve açık kalanlar
Kabul (a)–(h) sağlandı (§ 7, § 5, § 6). Açık kalanlar:
1. **Sayfa GÖVDESİ başlıkları ve tarayıcı sekme başlığı hub adını yazıyor** (ör. /denetim-izi sekmesi "Denetim İzi — ByGatech", /raporlar/islem-defteri gövdesi "Hareket Dökümü"). Menü ve üst bar h1 artık tuval adı; sayfa gövdeleri (`<PageTitle>`, h1) hub `Sozlukler.cs`'ten okuyor. Görev hub'a dokunmayı yasakladığı için bu pakette yapılmadı → sonraki paket (UX-S31).
2. `KabukAramaEkranTestleri.Secilen_sube_kabuk_durumuna_yazilir_ve_arama_ucuna_gecer` bir ara koşumda (filtreli, çok sınıflı) bir kez kırmızı oldu; tek başına 3/3 ve son tam süitte yeşil. `KabukServisi.Ortak` statik durum paylaşımından kaynaklanan kararsızlık olduğu düşünülüyor; mevcut test tasarımı (sahibi P-15).
3. `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı bir ara derlemede görüldü (başka şeridin dosyası). Son derlemede 0 uyarı.
4. PRO kapalı grupta önizleme (tuvalde yok) ve Panel.dc.html açık grup büyük harfi konusundaki kararlar § 3'te; kurucu farklısını isterse CSS/tek koşulla değişir.
