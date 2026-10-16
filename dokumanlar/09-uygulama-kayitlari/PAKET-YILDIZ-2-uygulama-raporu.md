# PAKET YILDIZ-2 uygulama raporu (08.10.2026)

Kapsam: TEMA-YILDIZ-PAKET-NOTU § 4 adım D (kabuk). Hasımsız paket; kendi kanıtı bağlayıcı. Para/veri/yasal davranışa DOKUNULMADI (mevzuat yok).

## Yapılanlar
- `Bilesenler/Kabuk/YildizUstMenu.razor` (yeni): halkasız elmas + BUGÜN · TEZGÂH · MÜŞTERİLER · KASA · TÜM EKRANLAR (seçili: 1 px alt çizgi + aria-current), sağda HAS · 22 AYAR · saat · kullanıcı. Kullanıcı adı hesap menüsünü açar (dil, şube, Kılavuz, Ayarlar, Çıkış — kenar çubuğundan taşındı, özellik silinmedi). ≤1000 px: tek "Menü" düğmesi + açılır liste (aria-expanded/controls, Esc, ekran değişince kapanır); ≤600 px kur kümesi gizli.
- `Layout/YildizKabuk.cs` (yeni): şema kararı (kullanıcı çözülmüş görünümü, yoksa dükkân), beş öğe, yetki süzgeci (sol menüyle AYNI MenuTanimi.YetkiliMi), aktif öğe.
- `Layout/AnaYerlesim.razor`: yalnız `else if (YildizMi)` dalı + `TamEkranda && !YildizMi`; diğer 8 şemada markup birebir aynı. Şema değişince (Ayarlar) kabuk anında geçer.
- `Pages/Ekranlar.razor`: yalnız Yıldız'da her karoya ikon + hover açıklaması + özet satırı ("39 ekran · 6 bölüm · hiçbiri gizli değil"; yetkiye göre süzülmüşse "yetkin olanlar") + bölüm sayısı. Sayılar türetilir.
- `Core/I18n/Sozlukler.Yildiz.cs` (yeni, TR+EN): kabuk etiketleri + 39 ekran açıklaması (tuval Tum4 metinleri).
- `wwwroot/css/tema-yildiz.css`: kabuk bölümü eklendi; hepsi `html:root[data-sema="yildiz"]` kapsamlı, belirteç var(), sabit renk yok, 200 ağırlık yok, 15 px altı 500.

## Sapmalar ve gerekçeleri
- Üst bar (sayfa başlığı · mod anahtarı · yardım · kayıt arama · kuru-şimdi-sor düğmesi) Yıldız'da sessiz ikinci şerit olarak KALDI (özellik silinmez; Basit/Pro anahtarı orada). Kur değerleri üst menüde; üst bardaki kur bandı gizlendi.
- Tüm ekranlar sayfasında üst bar şeridi gizli (sayfa kendi başlığı + aramasını taşır; tuval Tum4 de böyle).
- Karo adları kısa ad yerine TAM ad (kısa adlar "Vade/Satış/Kasa" gibi çakışıyor, UX-S31).
- 375 px'te kareler kısa kare (ikon üstte), açıklama dokunmada gösterilmez (hover yok).

## Testler
Yeni: Yildiz2Testleri 18 (saf 4 · bUnit menü 5 · bUnit Tüm ekranlar 3 · kaynak 1 · CSS 4 + id/style 1). Filtreli 18/18. Tam süit (KUYUMCU_TEST_DB=kuyumcu_test_t5, bir kez, son kodla): 11262 toplam, 11260 geçti, 2 atlandı, 0 başarısız. Derleme 0 uyarı 0 hata.

## Mutasyon (cmp AYNI ile geri alındı)
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M1 | Yıldız dalı kapatıldı | Y2_30 |
| M2 | Kasa hedefi /kasa | Y2_01, Y2_03, Y2_10, Y2_11 |
| M3 | yetki süzgeci kaldırıldı | Y2_03, Y2_11 |
| M4 | Yıldız'da tüm karolara ikon yok | Y2_20 |
| M5 | [hidden] display:none kaldırıldı | Y2_40 |
| M6 | bir ekran açıklaması silindi | Y2_02 |
| M7 | özet yetkiye bakmıyor | Y2_21 |
| M8 | aria-current kaldırıldı | Y2_10 |
| M9 | Esc kapatmıyor | Y2_12 |

## Denetim
denetim.sh: tek ✗ = P-30 DEMO_INSAN_SURESI (paket dışı, YILDIZ-1 raporunda da var); OLCEK-1, P-36 vb. ✓.

## Gerçek (başsız) Chrome ölçümü
Araç: betikler/tur (sikisma.js K1-K13 + ekstra). Rotalar /, /ekranlar, /islemler, /cariler, /kasalar × 1440/1280/375 × BASİT/PRO × açık/koyu (60 sayfa); sonra /ekranlar + / yeniden (CSS düzeltmesinden sonra). Sonuç: /, /kasalar temiz; yatay kaydırma 0, taşan 0, binen 0, [hidden] görünen 0, TRY 0.
Kalan bulgular:
- /ekranlar K3 "Gider ve para aktarımı" kare içinde iki satır (kelime sınırında, kelime kırılmıyor; 142 px kare). K4 tren 2,2x (1440) / 3,4x (375): 39 kareli katalog sayfasının doğası (tuvalin kendisi 1,6x).
- /cariler K12 "Tümü" seçici ve /islemler 375 SERBEST-YUZEN katman: Yıldız'dan bağımsız (aynı K12 Platin şemada da var, w=83) — sahibi Cariler/ortak Secici ve Satış şeritleri.
Görüntüler (Read ile bakıldı): 375 /ekranlar (açık), 375 menü açık /cariler (koyu), 1280 /ekranlar (koyu), 1440 hover (açık, kare 3 px kalktı, açıklama göründü).
Hover ölçümü: visibility visible, opacity 1, transform translateY(-3px).

## Canlı (5166, vekil 5167) — iki rol
PAKETYILDIZ2-TEST kiracısı: SAHİP /ekranlar → 39 kare, "39 ekran · 6 bölüm · hiçbiri gizli değil". TEZGAHTAR (şubeli) → 17 kare, 4 bölüm, "yetkin olanlar", yönetim kareleri yok, üst menü 5 kelime. İş sonu: API/vekil durduruldu (5166/5167 boş), kiracı silindi, tenants 17 -> 18 -> 17.

## Koşul karşılama
Sol menü gizli + üst menü + 1 px çizgi ✓ · Tüm ekranlar 39 kare yetkiye göre süzülü ✓ · 375 açılır menü taşma 0 ✓ · diğer şemalar kabuk markup'ı aynı (varsayılan dal değişmedi, Y2_30; Y-1 piksel karşılaştırması css kapsamı Y1_02) ✓ · mutasyon ✓ · ölçüm ✓ (yukarıdaki kalan bulgular dışında) · build 0/0 ✓ · tam süit 0 başarısız ✓ · denetim: yalnız P-30.

## Açık kalanlar
- Diğer 8 şema için bu pakette yeni piksel karşılaştırma koşusu yapılmadı: varsayılan dal markup'ı değişmedi (kod kanıtı) ve CSS yalnız Yıldız kapsamında; YILDIZ-1 karşılaştırması (32/32 aynı) geçerli.
- Ekran ince ayarı (adım E) kapsam dışı.
## Kurucuya / mali müşavire
Mevzuat konusu yok.
