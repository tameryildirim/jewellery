# Oturum düzeni ve durum defteri

> **18.10 00:00 — kurucu kararı: TEK OTURUM.** "Her şey bittiğinde sen tek olarak devam et… kafamız karışmasın."
> FORK M6b'yi bitirip durdu (28b89ca); ANA oturum tek başına sürüyor. Aşağıdaki iki-oturum kuralları
> (açık yollarla commit, geçiş kilidi, temiz yayın) yeniden paralel çalışma olursa geçerlidir; temiz yayın
> (`yayin-temiz.sh`) her durumda TEK yayın yoludur.

# (Arşiv) İki oturum — iş bölümü, kurallar

> Kurucu 17.10: "çakışma olmadan iki tarafta devam etsin, emirleri senden veriyorum, durmayın".
> Emri veren: **ANA oturum** (995ecd38). Çalışan: **ANA** ve **FORK** ("son kuyumö (fork)", local_4c95c907).
> **RAPOR oturumu** (salt okunur) bu dosyayı, `git log`'u ve iki oturumun kayıtlarını okuyup kurucuya özetler.

## Alanlar (dosya sahipliği)

| Oturum | Alan | Dosyalar |
|---|---|---|
| **FORK** | Tezgâh · satış · satın alma · cari kartı · ürün | `Pages/Islemler*`, `Bilesenler/Yildiz/YildizTezgah*`, `Modules/Satis/*`, `Modules/Satinalma/*`, `Pages/SatinAlma*`, `Pages/Cariler*`, `Modules/Cari/*` (CariIban hariç), `Modules/Urun/*`, `tuval-satis.css`, kılavuz |
| **ANA** | Banka · fatura · rapor/maliyet · muhasebe aktarımı · yayın altyapısı | `Modules/Banka/*`, `Modules/Fatura/*`, `Modules/Maliyet/*`, `Modules/Rapor/*`, `Modules/Disaaktarim/*`, `Pages/Banka*`, `Pages/KarZarar*`, `Pages/Fatura*`, `Modules/Cari/CariIbanModulu.cs`, `betikler/yayin*.sh`, `betikler/qa-sunucu.sh` |

Alan dışı bir dosyaya girmek gerekirse **önce öteki oturuma yaz**, cevabı bekle.

## Ortak dosyalar (yalnız EKLEME, kendi bloğunda)
`SunucuIletiSozlugu.Veri.cs` (sona kendi bloğun) · `denetim.sh` · `tema-yildiz.css` (sona kendi bloğun) ·
`KuyumcuDbContext.cs` · `DuzeltmePolitikasi.cs` · `ModulKayit.cs` · `ham/olcum-2026-09-18.json` · `Contracts/*`.
Başkasının satırını değiştirme/silme yok.

## Kurallar
1. **Commit yalnız açık yollarla**: `git add <dosya> …`. `git add -A`, `git commit -a`, `git add .` YASAK.
   Ortak dosyada ötekinin de hunk'ı varsa yalnız kendi bloğunu sahnele (HEAD + kendi blok → `git update-index --cacheinfo`).
2. **Geçiş (migration) kilidi**: `dotnet ef migrations add` öncesi öteki oturuma "GEÇİŞ KİLİDİ: <ad>" yaz →
   üret → hemen commit → yedek (`yedekler/kuyumcu_net-<gün>-<ad>-oncesi.sql.gz`) → `dotnet ef database update` → "GEÇİŞ KİLİDİ AÇIK" yaz.
   Aynı anda iki geçiş üretilmez (model özeti ortak).
3. **Yayın yalnız** `zsh dokumanlar/09-uygulama-kayitlari/betikler/yayin-temiz.sh <etiket>` ile: yalnız COMMIT'lenmiş kod
   (`~/ByGatech-yerel/jewellery-yayin` ayrı kopyasından) yayına girer; ana klasördeki yarım iş karışmaz, sunucu
   yeniden başlarken de bayat sayılmaz. Yayından önce öteki oturuma "YAYIN" yaz; **aynı anda iki yayın yok**
   (17.10 23:23'te iki yayın çakıştı, kopyalama kilide çarptı). Eski `yayin-ac.sh` kullanılmaz.
4. **Test veritabanı**: FORK `kuyumcu_test_t1` / `t3`, ANA `kuyumcu_test_ana`. RAPOR test koşmaz.
5. **Her commit sonrası** aşağıdaki deftere tek satır: saat · oturum · commit · ne · test/denetim · yayın.
6. Kurucu kuralları geçerli: eksik/ertelenmiş iş yok; tarayıcıda gerçek veriyle kanıt; hata gizlenmez.

## Sıradaki işler (18.10 — kurucu: "Tüm eksikleri tamamla")
15.10 rakip eşitleme planının ilk dalgası (`RAKIP-OZELLIK-ESITLEME-PLANI-2026-10-15.md` § 5 D1). Bitti: D1-01…07, 09…20 (D1-17 zaten vardı). Kalan yalnız D1-08 eritme/fire/takoz — K-13 kurucu kararı bekliyor (denetim HURDA_FIRE_SIZINTISI kilidi). D1-14 Luca/Logo dökümü dış bağımlılık (örnek dosya); D1-20 "dene-gör" anlamı belirsiz (varsayılmadı).

## Sıradaki işler (17.10 gece — tarihî)
- **FORK**: M6b (Tezgâh'ta TC yazılınca müşteri bul/aç — `CariServisi.OlusturId` + `GelenOdemeServisi.Coz` hazır),
  M5 (gram ve TL yan yana: tezgâh satırı, ürün kartı, fiş), Tezgâh Basit modda 14 → en çok 12 düğme.
- **ANA**: M7 (banka sağlayıcı soyutlaması + sahte sağlayıcı "havale gönder" + arka plan çekici + ekranda canlı yenileme,
  (hesap, banka_referans) tekilliği), M8 kalanı (açıklamadan eşleştirme önerisi, açık fatura önerisi),
  M11'in entegratörsüz kısmı (gün sonu toplu e-Arşiv onay listesi, Luca/Zirve aktarımı), P-23 canlı 2 kayıt.
- **Kurucu kararı bekleyen**: e-Fatura entegratörü, Harem veri izni, P-30 kronometreli demo ölçümü.

## Durum defteri
| Saat | Oturum | Commit | İş | Test / denetim | Yayın |
|---|---|---|---|---|---|
| 17.10 13:20 | ANA | ec3b4ec | M7a gelen ödeme: müşteri bul/aç, borçtan düş kararı, IBAN listesi | 12708/0 · P-23,P-30 | m7a |
| 17.10 22:55 | ANA | 84de2f5 | M9 kâr ayrımı (makas/altın farkı) + alış işçilik belgesi | M9 10/10 · P-23,P-30 | — |
| 17.10 23:20 | FORK | e46f970 | Tezgâh "Fatura kesilsin mi?" + tek adımlı "Mal geldi" | (FORK bildirir) | fatura-sorusu-1710 |
| 17.10 23:25 | ANA | — | Temiz yayın altyapısı (yayin-temiz.sh, .kaynak-kok) | yayın + kirli ağaçta yeniden başlatma 200 | temiz-1710 @ 8546bec |
| 17.10 23:25 | ANA | — | M9 tarayıcı kanıtı: kartlar, uyarı, fatura girişi, 375 px, koyu | ✓ | — |
| 17.10 23:22 | FORK | 8546bec | Fatura sorusu tek satır (alt blok + İndirim'i örtüyordu) | tarayıcı 1280/375 açık/koyu | fatura-sorusu-1710b |
| 17.10 23:25 | FORK | 6cad8f8 | Evet/Hayır hizası — ⚠ ANA'nın yayinla.sh + qa-sunucu.sh değişiklikleri de yanlışlıkla bu commit'e girdi (commit -a; içerik doğru) | — | fork-6cad8f8 @ d2a1feb |
| 17.10 23:35 | FORK | (bu commit) | Evet/Hayır dikey orta (yz-b2 align-self eziyordu; ölçüm orta 720/720) | tarayıcı ölçümü | (sonraki) |
| 17.10 23:55 | FORK | (bu commit) | M6b: tezgâhta TC/VKN ile müşteri bul ya da aç (POST /islemler/musteri-ac, tezgâhtar dahil; seçicide kendiliğinden seçim + "Müşteriyi aç ve seç") | 12635/12635 · denetim P-23,P-30 · M6b mutasyon kanıtı | — (ANA yayınlar) |
| 18.10 00:14 | ANA | ef5d9eb · 1c08dba | Fiyat kuralı 8 (KDV payı fiyatın üstüne, ses 04:59) · M7 demo banka + çekici + canlı yenileme | tam 12639/0 | m7-kdv |
| 18.10 00:13 | ANA | fccd58d · 1f40460 | Eksik müşteri TC'si: havaleden öğrenilir, tezgâhta kilidin altında yazılır | uçtan uca FTR-2026-000005 | tc-1810 |
| 18.10 00:30 | ANA | f1b1053 · 4ca8929 · cc6e9ff | tezgah-hazir birleşti; gece yarısı test kaymaları (Y5A_04, Yasal4, Tevsik) | gece aralığında 12644/0 | — |
| 18.10 00:36 | ANA | c1ced63 | Güvenlik: Blazor WebAssembly 8.0.32 (GHSA-qjpx-jqm5-h2c4) | NU1903 yok | guvenlik-1810 |
| 18.10 00:50 | ANA | b681d67 · 4e54e82 | M5 gram + TL yan yana (fatura kalemi Gram, fiş Gram); fatura özetinde KDV | 12645/0 | m5b-1810 |
| 18.10 01:00 | ANA | 5efa368 | Denetim P-23 fiş düzeyi (yanlış pozitif kapandı) — denetimde yalnız P-30 | — | — |
| 18.10 01:10 | ANA | 2469d1a · 5682ce9 | Toplu fatura (Faturalog F6) | 12645/0 + yeni | toplu-1810 |
| 18.10 15:00 | TEK | 0b2a129 · 6700472 · 7c42a92 | D1-01 fatura yazdırma (A4 + 80 mm fiş, Faturalar'da Yazdır) | 12649/0 · denetim yalnız P-30 | d1-01c |
| 18.10 15:35 | TEK | 008d868 · 9835b7e | D1-03 işlem bekletme · D1-10 mutabakat formu · D1-12 hurda fotoğrafı | 12656/0 | d1-12b |
| 18.10 16:00 | TEK | 86cddc8 · 3c8a8dd | D1-19 KVKK (sürümlü metin, müşteri beyanı, form; /kvkk ekranı, kılavuz) | 12659/0 | d1-19b |
| 18.10 16:25 | TEK | eb037cb · 346a8cf | D1-15 eşik altı stok bildirimi (STOK_ESIK) + hurda analizi | 12660/0 | d1-12f |
| 18.10 16:47 | TEK | 047bc67 · 8d93f83 · 317a859 | D1-11 kör sayım · çalışan kasası · kapanış hatırlatması (19:00) | 0 başarısız | d1-11 |
| 18.10 17:08 | TEK | 017f55c · 175907d | D1-13 kimlik tespit formu · garanti kartı · ekranda imza | 0 başarısız | d1-13 |
| 18.10 19:29 | TEK | 4cdd044 · 621df9f | D1-09 hesap dönüştürme (TL ↔ has ↔ döviz) | 0 başarısız | d1-09 |
| 18.10 19:55 | TEK | cca5cb5 · 16c3c6d | D1-16 akıllı içe aktarım · toptancı barkodu · ürün seti | 12674/0 | d1-16 |
| 18.10 20:15 | TEK | 64d54d5 · cfd724f | D1-14 mali müşavir salt okunur hesabı (sunucu kapısı, bant, rozet) | 12676/0 · denetim yalnız P-30 | d1-14b |
| 18.10 20:41 | TEK | a1e7ec5 | D1-04 kur kilidi (tezgâhtar ≤60 dk, yönetici ≤30 gün; satış + sipariş teslimi; müşteriye bağlı) | 12680/0 · denetim yalnız P-30 · mutasyon yakalandı | d1-04 |
| 18.10 21:06 | TEK | e5a0497 | Küçük 3: alış fişi/hurda satışı bayat kur kapısı · iş zamanlayıcısı Duplicate gürültüsü · fatura PATCH kuruşu (ZO-BELGE kapandı) | 12682/0 | kucuk-3 |
| 18.10 21:48 | TEK | 3f8c31c · 52ea975 | D1-05 satış onayı (eşikler + kişisel iskonto sınırı, tezgâhta onay bandı, Bildirimler'de Onayla/Reddet) · mesai dışı satış / peş peşe iade bildirimi | 12686/0 · denetim yalnız P-30 · mutasyon yakalandı | d1-05b |
| 18.10 22:12 | TEK | 891f544 | D1-06 taksitli/senetli satış (ödeme planı, senet, gecikmiş taksit bildirimi, imzalı çıktı) | 12689/0 (+2 ekran testi ayrı koşuldu, yeşil) · denetim yalnız P-30 · mutasyon yakalandı | d1-06 |
| 18.10 22:41 | TEK | 8361d19 · 76658a5 | D1-07 altınla öde (borç kapanır, fazlası emanete) · kara liste · risk noktası | 12695/0 (+1 ekran testi ayrı) · denetim yalnız P-30 · mutasyon yakalandı | d1-07b |
| 18.10 23:05 | TEK | cf67bb9 | D1-02 etiket şablonu (boy, alan sırası sürükle-bırak) · QR (ISO 18004, dış kütüphanesiz; tarayıcıda 6/6 + canlı etiket çözüldü) · basım anı fiyatı · Zebra ZPL | 12700/0 · denetim yalnız P-30 | d1-02 |
| 18.10 23:37 | TEK | 36306a9 · 0360727 | D1-18 ŞİB hazırlık · 5549 md.4/2 gizlilik (salt okunur ŞİB göremez) · BM listesi günlük kendiliğinden (canlıda 1.010 satır yüklendi) · uyum raporu · denetim CSV | 12702/0 · denetim yalnız P-30 · mutasyon yakalandı | d1-18b |
| 18.10 23:51 | TEK | c42661e · 53d68d2 | D1-20 prim/hedef (satan bazlı, iade asıl satandan, hedef güne orantılı) | 12703/0 · mutasyon yakalandı | d1-20b |
| 19.10 00:21 | TEK | 3e2e56d · 4c0d288 · b7f4a5d | D2-12 yüzük ölçüsü · dilek listesi · sadakat puanı (mali etkisiz) · uykuda müşteriler (Müşteriler sekmesi + Raporlar) · MASAK tatil testi düzeltmesi | 12704/0 · denetim yalnız P-30 | d2-12c |
| 19.10 00:26 | TEK | 635d216 | D3-01 ana ekrana ekle (manifest, ByGatech amblemi ikonları, iOS etiketleri; Chrome manifesti hatasız okudu) | 12705/0 | d3-01 |
| 19.10 01:02 | TEK | 7242eba · 0e7cd72 | D3-11 dış API (anahtar özetle saklanır, KUR/URUN kapsamı, iptal) + canlı kur kanalı /hubs/disa-kur — canlıda uçtan uca: 200 / kapsam dışı 403 / iptal sonrası 401 | 12707/0 · mutasyon yakalandı | d3-11b |
| 21.10 21:34 | TEK | 990ec75 | Görsel denetim (RAPOR.md) KRİTİK+YÜKSEK kapandı: K-1, Y-1..Y-8, O-16, O-19 — tarayıcı ölçümü 26/26 (dogrula-ky.mjs) | 12.810/12.814 (1 dalgalı Y4A eşzamanlı, tek başına 3/3) · denetim yalnız P-30 | gorsel-ky · 5179 /urunler 1024 çakışma yok |
| 21.10 22:24 | TEK | 853de63 | Görsel denetim ORTA+DÜŞÜK kapandı (O-1..O-22, D-1..D-14; O-6/O-7/D-7/O-20 tuvalle çelişir → kurucu kararı; O-17 rem uygulama içi yazı ölçeğiyle karşılanıyor; O-22 demo düğmesi yalnız Demo banka sağlayıcısında) — tarayıcı 27/27 (dogrula-od.mjs) | 12.812/12.815 + K1_07b · denetim yalnız P-30 · O-18 mutasyon yakalandı | gorsel-od |
| 21.10 22:40 | TEK | 414b7ed | Fiyat kalemleri tek kaynak (Core FiyatKalemleri): kur ekranı 22 kalem yeni+eski tam liste, gün farkı sunucudan, ONS $, ayar adları Core, vitrin varsayılanı 11 kalem (yeni+eski) | 12.817/12.820 · denetim yalnız P-30 | kur-tek-kaynak · 5179 /kur 22 kalem, gün farkı ▲ 0,3% |
| 21.10 23:05 | TEK | 0df7cbd | Döviz alışı: kur kaydına usd/eur/gbp_alis (migration KurDovizAlis; yedek yedekler/kuyumcu_net-20261021-2300-kur-doviz-alis-oncesi.sql.gz, 107 tablo; kuyumcu_net + kuyumcu_egitim güncellendi); 5 kaynak alış taşır; kur ekranında USD/EUR/GBP alış+satış | 12.819/12.822 · denetim yalnız P-30 | doviz-alis · 5179 /kur USD 49,59/49,75 |
| 21.10 23:35 | TEK | 9f15998 | Tuvalle çelişen 4 bulgu (kurucu kararı): O-6 tek başlık 40/300 (375: 28), O-7 ölçek 12·14·16·18·22·28·40 (30 ekranda 22 boy → 7), D-7 gövde 400 / başlık 300, O-20 takvim gün adları ızgarada | 12.819 + tipografi testi · denetim yalnız P-30 | tipografi · 5179 /cariler h1 40/300, gövde 400 |
| 23.10 22:30 | TEK | d84c504 | Görsel denetim 2 (yeniden koşum: 364 durum, 91 açılır, 52 klavye, 52 EN; 45/46 kapalı doğrulandı) + açılır yüzeylerde gölge/perde (kurucu) + yeni bulgular G2-1..G2-6 (satış yapışkan blok, tezgâhtar 403 → 0, /gider CLS 0, seçici odağı, 1024 ızgaraları, sayfa dibi liste) — RAPOR: gorsel-denetim-2026-10-23/RAPOR.md | 12.820/12.823 · denetim yalnız P-30 · dogrula-3 12/12 | gorsel-2-golge |
| 24.10 18:20 | TEK | e65bba8 | Kurucu 24.10 (3 ekran görüntüsü): /ayarlar güvenlik sekmesinde tekerlek iç kutuda hapsoluyordu → katlanır kutularda overscroll contain → auto (KaydirmaZinciri testi + kaydirma-zinciri.mjs gerçek tekerlek; MUTASYON=1 eskiyi yakalar); Bugün "Yeni satış/Günü kapat" kısa ekranda altta kalıyordu → sol sütun yapışkan, görüş alanı boyu (1280×800: düğme altı 744/800); satış ekranı sağ sütun iç kaydırması kaldırıldı → sayfa bütün kayar, Fark/Kaydet görüş alanı dibine yapışık (1280×600: alt 560/600); açılır liste yüzeyi beyaz → zeminle aynı + gölge (açık rgb(245,244,241)=zemin, koyu rgb(0,0,0)=zemin, gölge var); yedek tatbikatı 24.10 (107/107 tablo, t202610241518561164) → P-33 yeşil | 12.824/12.827 · denetim yalnız P-30 · 5199=200, 5179 kanıt | kurucu-2410 |
| 24.10 18:45 | TEK | (bu commit) | Kurucu 24.10 ikinci görüntü: Bugün sol sütun yapışkanı tutmuyordu — `.ana-icerik{overflow:auto}` yapışkanı hapsediyordu (kap kaymıyor, pencere kayıyor) → Bugün için Tezgâh istisnasının aynısı (overflow-y visible); ölçüm 1280×800: kaydırınca sütun üstü 72 sabit, düğme altı 672/800 (önce −91 / 509); kilit Y4C_44 | 12.825/12.828 · denetim yalnız P-30 · 5199=200 | kurucu-2410b |
| 24.10 21:40 | TEK | (bu commit) | Kurucu 24.10: sayfa değişiminde dönen halka solda — yalnız Tezgâh (/islemler): `.islem-sayfa{align-items:start}` sarmalayıcıyı 42 px boyuna büzüyordu → sarmalayıcı satırı doldurur, halka ortada (1280×800: 640/452; önce 21/147). 24 rota tarandı, diğerleri ortada (640) ya da bölüm içi | 12.825/12.828 · denetim yalnız P-30 · 5199=200 | kurucu-2410c |
| 24.10 22:15 | TEK | (bu commit) | Kurucu 24.10 /kur: KAYNAK satırı SAHİP için yerinde açılır seçici (yazma yolu mevcut PATCH /kur/kaynak, yalnız SAHİP; diğer roller salt okunur) — Altınkaynak→Harem→Altınkaynak canlı denendi (üst şerit ve 25→20 satır değişti, hata 0); her 5 sn veri: sunucu zamanlayıcısı alt sınır 15→5 ve sevk ayarı 5 sn (appsettings ×3 + .ornek ×2), ekran her 5 sn güncel+kalemleri yeniden okur (PeriodicTimer, Dispose'da durur; /kur/guncel push bağlıyken önbellekten). Testler: S3e Yıldız seçici (SAHİP PATCH / MÜDÜR yok), P73 sınır 4/5 | 12.829/12.832 · denetim yalnız P-30 · 5199=200 | kurucu-2410d |
| 26.10 03:40 | TEK | (bu commit) | Kurucu 26.10: BYGATECH işareti + menü sayfa kayınca yerinde durur (tüm ekranlar) — .yildiz-ust sticky/top 0, zemin --yuzey-govde (altından akan içerik görünmez; tuvaldeki saydam zemin aynı renk olduğundan göze fark yok), katman pencere 50 / çekmece 40 altında; Bugün sol sütunu (top 72) ve 375 menü listesi (top 100%) şeride göre. Ölçüm 5181: 1280 scrollY 1277 → şerit top 0, zemin = gövde (açık 245,244,241 / koyu 0,0,0); 375 koyu top 0, 64 px. Test Y4C_45 | (aşağıdaki satırla birlikte) | kurucu-2610a |
| 26.10 03:40 | TEK | (bu commit) | Kurucu 26.10 ("KAYNAK HAREM ALTIN · TÜM AYRINTILAR kıdemli frontendci işi mi?"): ölçüm 1280 canlı — (1) seçici en uzun seçeneğe göre 190 px, alt çizgi değerden ~100 px sarkıyor, oku yok (appearance none), ağırlık 400 (genel `select … 400 !important` yeniyordu) ve satırı sarıp GÜN FARKI'nı 40 px aşağı tek başına düşürüyordu (satır 84 px); (2) "+ Tüm ayrıntılar" genel .yz-ekle align-self:flex-start ile düğme satırının tepesine asılıydı (merkez farkı 16 px). Düzeltme: seçici satırın öteki öğeleriyle AYNI (soluk etiket + koyu 500 değer, çizgi yok), genişlik SEÇİLİ ada göre (gizli ölçü yazısı b.yz-kaynak-olcu + select aynı ızgara hücresi, %100), sağda ::after ok (resim yok), fare üstü/odakta koyulaşma, odak halkası kabukla aynı; bilgi satırı ≤1440'ta gap 12/32 (sarma liste gibi okunur, 53 px); eylem satırı .yz-ekle align-self:center (4 özet ekranı: kur/kasalar/hurda/kasa raporu). Ölçüm 5181: select 500, genişlik 152 = ölçü 152, ok var, Tüm ayrıntılar merkez farkı 0 (/kur ve /kasalar), 375 taşma yok. Testler Y4C_46 + S3e ölçü yazısı | tam süit + denetim aşağıda | kurucu-2610a |
| 27.10 17:40 | TEK | (bu commit) | Garanti BBVA entegrasyonu: kurucu onayıyla API portalında "ByGatech" uygulaması açıldı (yalnız Account Information + Account Transactions, salt okuma; koşullar kabul; anahtar Confidential/oob, callback boş; Publish → Pending Approval). Portaldaki RESMİ belge (swagger + Account_Transactions belgesi) GarantiBaglayicisi ile karşılaştırıldı: uç nokta/token/oob/her istekte yeni token/consentId/tarih biçimi/pageSize≤500/30 gün/result kodları/A-B/MUS-CORRACNT-DEK UYUYOR (şemadaki endtDate/txtn…/returnCcode yazım hataları; tablo+örnek bizimkini doğruluyor). Kapatılan: (1) başka bankadan EFT'de gönderen ad/IBAN/VKN yalnız EFT zenginleştirmesinde → okunuyor (gidende alıcı; MUS/CORRACNT varsa ezilmez); yön alanı iki yazımla da okunur; (2) 400 gövdesindeki Türkçe messageText (ör. clientId-consentId hatalı, kod 4) JsonBankaHatasi.Coz ile kullanıcıya. Testler +3 (D2BankaBaglayici 16/16); mutasyon: EFT ad ataması kapatılınca test KALDI, geri alınca geçti. Canlı Garanti'de henüz denenmedi (onay bekleniyor). | build 0 hata · tam 12.834/0 · denetim yalnız P-30 | — |
| 31.10 22:30 | TEK | (bu commit) | Kurucu 31.10 ("Faturalog'daki gibi hesaba ait bakiye + gelen/giden"): Garanti Hesap Bilgisi servisi (resmi belge GARANTI-HESAP-BILGISI-BELGE-2026-10-31.md) → bankadaki bakiye + kullanılabilir (bloke) hesaba yazılır (BankaHesap.BankaBakiye/BankaKullanilabilir/BankaBakiyeZamani, migration BankaBakiyesi); IBankaBakiyeVeren yalnız Garanti; bakiye hatası hareketleri düşürmez; belge URI 404 → swagger yolu; Yıldız listesinde "Bankada" sütunu (yalnız bakiye veren hesap varsa; telefonda etiketli), ayrıntı kartında "Bankadaki bakiye · bloke · saat" + ByGatech kaydıyla fark uyarısı; giden hareketler listede ↑ Giden (tarayıcıda doğrulandı). Testler +6 (ayrıştırma 3, uçtan uca 3), 4 mutasyon yakalandı; tam süit 12840/0; denetim yalnız P-30; tarayıcı 1280+375 açık+koyu (garanti-bakiye-2026-10-31/). TEYİTSİZ: gerçek uç nokta yolu (canlı consentId yok). |
| 01.11 01:30 | TEK | (bu commit) | Kurucu 01.11 ("dökümanda ne yazıyorsa o"): Garanti Hesap Bilgisi swagger'ı (kurucunun indirdiği api.json) unitNum/accountNum/IBAN'ı ZORUNLU sayıyor → bakiye ve hareket isteklerine şube/hesap no IBAN'dan (00062 + 16 hanenin 5/11 bölümü; belge örneği TR620006200029500006291296 → 295/6291296) örnekteki gibi metin olarak eklendi; Garanti IBAN'ı değilse yollanmaz. Testler +2, 1 mutasyon yakalandı; hedefli 27/0; tam süit 12840 geçti / 2 KIRMIZI — TevsikIhlal K12 + Son_gunden_sonra (bankayla ilgisiz, takvime bağlı: 10 günlük tarama penceresinin en eski günü 23.10, 29 Ekim tatili + hafta sonu yüzünden son bildirim günü 02.11 → 01.11'de 'süre doldu' senaryosu kurulamıyor; 03.11'den itibaren yeniden yeşil beklenir, ayrı iş açıldı); denetim yalnız P-30. Canlı Garanti denemesi: 'There is no configured IP on Portal Application' → portal IPs=151.135.190.195 onaya gönderildi. |
| 01.11 22:30 | TEK | (bu commit) | Kurucu 01.11 (yarınki demo): ses kaydındaki istekler EĞİTİM KOPYASINDA (kuyumcu_egitim, 5192/5181) uçtan uca gerçekten kaydedilerek denendi, başsız Chrome 1280 + 375: Harem kur + elle gir; CNC bilezik ürünü (alış milyemi 930); ad kuralı önizleme; TC ile müşteri aç → satış → FTR-13 (116.637,89, KDV matrahı binde 1 = 116,64 / KDV 23,33); nakit+kart FTR-14; 30 bin nakit sınırı; demo havale → TC eşleşmesi → havaleden satış FTR-15; toptancı mal kabul (MASAK 200 bin kimlik) + işçilik belgesizlik uyarısı → belge girildi; hurda alış → GP-2026-000002; fişten fatura FTR-16 (çift fatura engeli görüldü); toplu fatura; Luca/Zirve dışa aktarım. **GİB: GİTMİYOR** — ebelge_ayarlari 0, ebelge_gonderimleri 0 (eğitim ve canlı), ekranda 'Bağlı değil'; demo işletme VKN 0000000000. Rapor: demo-uctan-uca-2026-11-01/ (PDF 20 sayfa + 38 görüntü). Kod değişmedi; canlı 5199'a yazılmadı. |
| 02.11 12:30 | TEK | (bu commit) | Kurucu 02.11 ("tüm projeyi bu yönde değiştir, eski ürün adı kelimesi kalmasın, ticari sorun yaşamak istemiyorum; tasarımları da ona göre değiştir, sorma"): ÜRÜN ADI → **ByGatech** (şirket adı). Gerekçe: önceki ad TÜRKPATENT canlı sorguda başka kuyumcu adına 09/14 tescilli (2006/60385 + 2026/066974 başvuru) + aynı adla canlı kuyumcu yazılımı; kurucunun Mühür (2010/37455 Katre Kuyumculuk 09/14/35) ve Sarraf (2025/134505 Backhub Yazılım 09/42 + App Store uygulaması) adayları da tescilli çıktı; Zerdefter vb. türetilmiş adlar kurucuca reddedildi (AD-ARASTIRMA-2026-11-01.md § 02.11). Kod: tek kaynak Marka.Ad="ByGatech" (AdAscii aynı); KabukTuvalSozlugu.MarkaYazimi türetme yerine Marka.Ad'ı okur; amblem 8 harf → SVG yazı kutusu 140 birim (kabuk 140×16, giriş/sıfırlama 280×32, x=72; ölçülen yazı 105–107 birim, sığıyor); sürüm satırı "ByGatech 1.0" ("· ByGatech" eki kalktı); PWA title/manifest/apple-title; SW öneki bygatech-kabuk- + aktivasyonda yabancı önbellekler silinir; BYGATECH_ADRES (.env) + BYGATECH.desktop; EBelge:OzelSablon anahtarı + bygatech-fatura.xslt (gömülü kaynak adı); parola e-postası, banka ipuçları, bakiye farkı metinleri Marka.Ad'dan; start/stop.bat. Belgeler: 572 metin dosyasında eski ad → ByGatech (ByGatech→BYGATECH); 5 PDF/zip yalnız yeniden adlandı (içerik değişmez); MUAF: AD-ARASTIRMA-2026-11-01.md + MARKA-ARASTIRMA-*-2026-11-01.md (ad araştırması — eski adın niçin bırakıldığının kaydı) ve git geçmişi. Çalışma: yerel dal bygatech (git branch -m), launchd etiketi com.bygatech.kuyumcu (plist betikler/, günlük bygatech-sunucu.log), yayin-temiz.sh buna göre; GitHub deposu adı + eski LaunchAgent dosyasının silinmesi KURUCUDA. Kapı: build 0 hata; hedefli 580/0; denetim.sh yalnız P-30 DEMO_INSAN_SURESI ✗ (bilinen); tarayıcı 5181 (eğitim kopyası, yeni derleme) 1280+375 giriş+kabuk: başlık "Giriş yap — ByGatech", amblem BYGATECH, taşma yok — foto ad-degisikligi-2026-11-02/foto/. Tam süit 12840 geçti / 2 KIRMIZI (TevsikIhlal K12 + Son_gunden_sonra — 01.11'den beri bilinen takvim bağımlı, ad değişikliğiyle ilgisiz) / 3 atlanan; kılavuz.html kilavuz-uret.sh ile yeniden üretildi (damga). Not: `grep -i mihenk` Türkçe İ'li yazımı yakalamaz — 55 dosya ikinci turda ayrıca tarandı. |
| 02.11 16:20 | TEK | (bu commit) | Kurucu 02.11 ("tüm projeyi sunucudan yayınlayalım; en ucuzunu araştır"): VPS seçimi DeHost TR VPS-4GB (2 vCPU/4 GB/40 GB NVMe/1 sabit IPv4/TR; 3 aylık 467,96 ₺ KDV dahil; alan adı 2020, AS47516; Lunhost 5 aylık firma elendi; Natro/Turhost 3 kat pahalı) — hesap/ödeme/1 TL doğrulama kurucu. Sunucu **5.10.220.150**, Ubuntu 24.04: root parolası rastgele yenilendi (panel sayfası okunurken satır kayda düşmüştü), SSH yalnız anahtar (cloud-init 50-*.conf 'yes' diyordu → 00-bygatech.conf; parolayla giriş denemesi reddedildi — ölçüldü), ufw 22/80/443, fail2ban, unattended-upgrades, TZ Istanbul. Yayın yolu DEPLOY.md § 4a: yayin-al.sh linux-x64 → rsync /opt/kuyumcu/yayin → kurulum.sh sırlar (/etc/kuyumcu/.env) → MySQL 8.4 docker yalnız 127.0.0.1 (deploy/docker-compose.mysql.yml, YENİ; bellek sınırı 1800 MB — 1200'de RSS 1,05 GiB OOM riskiydi) → kuyumcu.service active, /saglik 200 → nginx ters vekil (repo şablonu `http2 on;` nginx 1.24'te düşüyor → `listen ... http2` biçimine alındı) → TLS: önce geçici kendinden imzalı, sonra **Let's Encrypt 5-10-220-150.sslip.io** (tarayıcı paneli kendinden imzalıyı reddetti; sslip ile gerçek sertifika, certbot.timer). Veri: Mac kuyumcu_net (demo) dump → içe aktarıldı (107 tablo, 128 migration, 35 fatura); alan şifreleme anahtarı Mac ile eşitlendi (karma 342eff58…; aksi hâlde şifreli TC alanları okunamazdı). Yedek: timerlar kuruldu; ilk koşu gpg HOME salt-okunur → GNUPGHOME drop-in; tatbikat 'belgeler' GERÇEK uyuşmazlık verdi → kök neden Linux GNU grep longblob akışını binary sayıp satır düşürüyor (macOS BSD grep'te görünmez) → yedek-ortak.sh `--hex-blob` + yedek-al-ve-dogrula.sh `grep -a`; yeniden: tam yedek 29 MB, **tatbikat DOĞRULANDI 107/107**; Yedek/T3 testleri 243/0, denetim yalnız P-30. Mac dış kopya: betikler/vps-yedek-cek.sh ilk çekim 2 dosya + sunucu anahtarıyla gpg çözüldü; launchd plist hazır (com.bygatech.vps-yedek) — otomatik kurulum sınıflandırıcıca engellendi, KURUCU kurar. Tarayıcı kanıtı: https://5-10-220-150.sslip.io/giris 1280 + 375 (vps-yayin-2026-11-02/foto), SSH tüneli + inceleme vekili ile giriş → Panel/Cariler 7 müşteri. Runbook docs/RUNBOOK-VPS.md (YENİ). Kurucuya kalan: Garanti portal IPs=5.10.220.150, alan adı (bygatech.com.tr boş), launchd yedek işi, GitHub depo adı; sunucuda yalnız demo verisi. |
| 03.11 09:00 | TEK | (bu commit) | Kurucu 02–03.11: (1) GİRİŞ HATASI — kurucunun gmail'i sunucuda iki işletmede (Ramazan Deneme SAHIP + Altın Çarşı MUDUR) → giriş "Bu e-posta birden fazla işletmede kayıtlı, tenantId belirtiniz" (jargon, kapı yok). Düzeltme: AuthModulu.Giris tüm adayların parolasını doğrular; birden fazla eşleşme → `{isletmeSecimi, isletmeler[]}` (jeton YOK), Giris.razor "Hangi işletme?" seçicisi (Secim bileşeni, rol adı sözlükten) → tenantId ile ikinci istek; tek eşleşme doğrudan girer; yanlış parola genel 401. (2) KURUCU KARARI "aynı e-posta ile kayıt yapılamasın": bir e-posta aynı anda YALNIZ BİR işletmede aktif — kullanıcı ekleme, e-posta değiştirme, pasiften aktife alma (iki PATCH yolu) 409 "Bu e-posta başka bir işletmede kullanılıyor; her e-posta tek işletmede olabilir" (alan=email); pasif kayıt engel değil; kiracı içi tekillik eski mesajla. Kiracı süzgeci SistemKapsami madde 11 ile aşılır: parametreli SALT OKUMA COUNT (EpostaBaskaKiracidaAktifMi), başka kiracının satırı okunmaz; IgnoreQueryFilters/new DbContext YOK. Testler: AuthTestleri +3, GirisIsletmeSecimiTestleri IS01–04 (bUnit), KullaniciEpostaTekilligiTestleri ET01–05. KAPI: build 0 hata · hedefli 66/0 · tam süit 12852 geçti / 2 bilinen (Tevsik takvim) / 3 atlanan · denetim yalnız P-30 · VPS yayın (yayin-al linux-x64 + rsync + restart) sağlık 200, https 200. **TEYİTSİZ:** tarayıcı kanıtı (sunucuda sahip parolası değişti → inceleme vekili json eski; kurucu panelde giriş yapmadı); sunucudaki eski çift kayıt (gmail) pasife alınmadı (uzak UPDATE izin katmanında reddedildi, kurucu kararı). (3) GARANTİ 03.11: IP onayı GELDİ (apistore e-postası); sunucu çekicisi yanıtı artık IP hatası değil, **kod 4 "clientId-consentId hatalı"** (consentId belge örneği). EHÖ başvurusu bireysel internet şubesinde Devam → garantibbva.com.tr 404 (3 farklı Client ID değeriyle); 444 0 333 5 temsilci çözemedi; portal belgeleri yeniden okundu (tek sunucu, mock yok, consentId yalnız "enrollment"; Products/Requests boş). Kalan: apistore@ e-postası (metin hazır) · ticari hesap · VakıfBank Online Ekstre (kod hazır) · çekiciye kod 4'te geri çekilme (kurucu izni). Runbook § 8 + hafıza güncel. |
| 03.11 22:15 | TEK | (bu commit) | Kurucu 03.11 (Kasa ekranı görüntüsü): "Bugün'deki Yeni satış gibi, sol alttaki düğmeler ve bilgiler kaydırınca yerinde kalsın, üst üste binme olmasın; bu ve bunun gibi ekranlarda". Etki analizi: özet kalıbı (YildizOzet) 4 ekranda — Kur · Kasalar · Hurda · Kasa raporu. Düzeltme (yalnız tema-yildiz.css): ≥1001 px `.yz-ozet-sol` Bugün ile AYNI kural (align-self start · sticky top 72 · min-height 100vh−72−64−48) + Bugün/Tezgâh istisnasının aynısı `.ana-icerik:has(.yz-ozet){overflow-x:clip; overflow-y:visible}` (ilk denemede app.css `overflow:auto` yapışkanı hapsetti: 1280 kaydırmada sol sütun −777 px'e kaçtı — ölçüldü, düzeltildi). ≤1000 px tek sütun, yapışmaz (Bugün gibi). KANIT (5198 çalışma ağacı, inceleme vekili 5184, gerçek tarayıcı): Kur 1280 Pro açık sayfa sonunda sol sütun top 72 / düğmeler 632–688 / binme yok · koyu (data-tema=karanlik) aynı · Basit (data-mod) aynı · 375 taşma 0, sol sütun statik, düğme altı 479 < liste üstü 519 · Kasalar/Hurda/Kasa raporu 1280 tek ekrana sığıyor, düğmeler 696–752, değişiklik yok. Görüntüler `gorsel-denetim-2026-11-03/kanit/` (8). KAPI: build 0 · denetim yalnız P-30 (CSS; test süiti koşulmadı — kod değişmedi). |
| 03.11 23:30 | TEK | (bu commit) | Kurucu 03.11 ("Fatura kesilsin mi? Evet diyorum, satış tamamlandı uyarısı gelmiyor, sanki kaydetmemiş gibi; tam ekran loading + giriş/sol üstteki amblem animasyonu; fatura kes dedim fatura düzenlemiyor, ses kaydında direkt fatura ekranına yönlenmeli"). Teşhis (5199, API): satışlar KAYITLI, faturalar KESİLDİ (kurucunun ikisi: 22:38 60.537,80 FTR-2026-000010 · 22:40 60.538,17 FTR-2026-000011 — geri bildirim olmadığı için aynı satış iki kez); tek geri bildirim sağ altta 'Son fiş: …' satırıydı (Yıldız'da OnayBandi çizilmez); e-belge ayarı kapalı (Nilvera bağlı değil) → GİB'e gitmiyor (beklenen). Müşteri görüşmesi 34:50: "TC'yi yazdığım anda fatura kesilsin". UYGULAMA: (1) YildizTezgah #yildiz-tezgah-perde — kayıt boyunca tam ekran perde (giriş amblemi AmblemTasi(GirisTasi) + "Kaydediliyor…", role=status/aria-busy, eylem yok); fişli satış bitince aynı perde role=dialog "Satış tamamlandı" + Son fiş satırı + Fiş yazdır/Ürün belgesi + TEK birincil "Yeni satış" (Escape de kapatır; odak düğmede — kuyumcuOdak.alana; barkod odağı perde kapanınca). (2) Faturalı satış (yanıtta fatura.id) → Nav /cikti/FATURA/{id}?donus=/islemler (uygulama içi fatura kâğıdı; 19.10 'pencere açılmaz' kuralı korundu). Islemler.Belge.cs KayitSonuBelge + sonFaturaId; Islemler.Yildiz.cs TamamBaslik/TamamKapat; YildizTezgahSozlugu Kaydediliyor/Tamamlandi/Yeni; tema-yildiz.css .yz-perde (z 70, zemin %94 --yz-perde-zemin, amb-form-gel). Islemler.razor 1399 satır (tavan 1400). Testler: YildizTezgahPerdeTestleri P01–P03, FaturaSorusuEkranTestleri FS_06 (fatura kâğıdına geçiş + Hayır'da perde), S25_E1 güncellendi (odak perde → Yeni satış → barkod). KAPI: build 0 · hedefli 3.279/0 · TAM SÜİT 12858 geçti / 0 başarısız / 3 atlanan · denetim yalnız P-30 · başsız Chrome 5198 kanıtı: 1280 'Kaydediliyor…' anı + 'Satış tamamlandı' (açık/koyu) + 375 taşma 0 + Yeni satış kapanış + Evet → /cikti/FATURA/… (FTR-2026-000014/15); görüntüler gorsel-denetim-2026-11-03/kanit-perde/ (7). Kurucunun 'kalem düzenleme açıkken kaydıramıyorum' bildirimi başsız Chrome 1280/1440'ta tekrar ETMEDİ (her noktada 289–309 px kaydı; yazarken sıfırlanmıyor) — kurucudan tarayıcı/fare bilgisi bekleniyor. |
| 03.11 23:40 | TEK | (bu commit) | AÇIK BİLDİRİM (kurucu): Tezgâh'ta kalem düzenleme açıkken sayfa aşağı/yukarı kaymadı — **trackpad, Chrome, hiç kımıldamadı, sonradan kendiliğinden düzeldi** (aralıklı). Başsız Chrome 1280/1440'ta tekerlek olayıyla her noktadan (girdi, kalem düğmesi, sağ sütun, form) 289–309 px kaydı; yazarken sıfırlanmadı; DOM'da wheel dinleyicisi, overscroll-contain ya da overflow tuzağı bulunmadı (yalnız .ana-icerik overflow:auto, kendi içinde kaymıyor). Tekrar ÜRETİLEMEDİ → kapatılmadı; yeniden görülürse: o anda `document.activeElement`, `scrollY`, pencere boyu ve açık katman (müşteri/ödeme) not edilecek. Kurucu kararı: geç, sonra tekrar bakılır. |
| 03.11 23:50 | TEK | (bu commit) | Kurucu "sunucuya at": VPS yayını (yayin-al linux-x64 @1347b282 → rsync → chown → systemctl restart kuyumcu) — active, 127.0.0.1:5000/saglik 200, https://5-10-220-150.sslip.io/saglik 200, yeni tema-yildiz.css sunucuda (.yz-perde 12 eşleşme). Yeni geçiş yok (migrate koşulmadı). Kapsam: 03.11'in dört commit'i (giriş seçici + e-posta tekilliği, özet ekran yapışkan sol sütun, kayıt perdesi + fatura kâğıdı, kaydırma kaydı). Sunucuda oturumlu tarayıcı kanıtı TEYİTSİZ (sahip parolası kurucuda). |
| 05.11 11:50 | TEK | (bu commit) | Kurucu: Garanti EHÖ onayı girildi, yanıt kod 4 (onay no ↔ client id eşleşmiyor; 00'lı client id oturumda 401). Kurucu isteği: "Bilgileri değiştir"de her şey baştan girilmesin + bilgi değiştirmeden dene. YAPILAN: aynı bankada yeniden kayıtta boş bırakılan HER alan (gizli/açık) eskisini korur (sunucu + ekran: alanlar isteğe bağlı, yer tutucu "Kayıtlı — değiştirmeyecekseniz boş bırakın", boş Kaydet = yalnız yeniden dene); başarısız deneme "son deneme <zaman>, başarısız" (eskiden "henüz denenmedi"). "Şimdi dene" zaten bilgi değiştirmeden deniyor. Testler: BankaBaglantiBolumuTestleri (4, bUnit) + D2 HTTP (+1). 12863/12866 yeşil (3 atlanan), denetim yalnız P-30 ✗. |
| 05.11 12:45 | TEK | (bu commit) | 5199 yayını (4677476a, yayin-temiz) KANITI — başsız Chrome + inceleme vekili, kiracı Altın Çarşı, geçici "Kanıt Garanti" hesabı (sahte kimlik → banka 401 → "son deneme 05.11.2026 12:42, başarısız" satırı; eski "henüz denenmedi" yok). Bilgileri değiştir: 3 alan aria-required=false, yer tutucu "Kayıtlı — aynı kalır" (ilk sürüm "…boş bırakın" 375/1280'de kesiliyordu → kısaltıldı), Kaydet ve dene hiçbir şey yazmadan etkin; yalnız onay no yazılıp kaydedildi → PUT 200, uygulama kimliği l7••••• korundu, yeni deneme 12:44/12:45. 375: taşma 0, form 16–359 px. Görüntüler `gorsel-denetim-2026-11-05/kanit-banka-baglanti/` (5). Kanıt hesabı + bağlantısı silindi, vekil/başsız Chrome durduruldu. Sunucuya atılmadı (kurucu "sunucuya at" derse). |
| 05.11 14:35 | TEK | (bu commit) | Kurucu "sunucuya da at": VPS yayını (yayin-al linux-x64 @cb14da4b → rsync 30 dosya → chown → systemctl restart kuyumcu) — active, 127.0.0.1:5000/saglik 200, https://5-10-220-150.sslip.io/ 200; canlı WASM Kuyumcu.Core.wasm içinde "Kayıtlı — aynı kalır" 1 eşleşme (banka bağlantısı alanlar isteğe bağlı + "son deneme …, başarısız" sunucuda). Yeni geçiş yok (migrate koşulmadı). Sunucuda oturumlu tarayıcı kanıtı TEYİTSİZ (sahip parolası kurucuda). |
| 05.11 14:40 | TEK | (bu commit, dal `ios`) | Kurucu "iOS uygulaması, aynı IP": (1) Telefona hemen: PWA zaten canlı (D3-01, manifest+apple-touch-icon sunucuda 200) → Safari Paylaş → Ana Ekrana Ekle; adres verildi. (2) `ios/` yeni Xcode projesi (xcodegen 2.46, SwiftUI + WKWebView): sunucu adresi Info.plist `SunucuAdresi`, kalıcı çerez, kamera/mikrofon izni, aşağı çek yenile, çevrimdışı kartı "Yeniden dene", dış alanlar Safari'de, JS uyarıları yerel pencere, simge PWA ikonundan 1024. Simülatör derlemesi 0 hata; iPhone 18 Pro simülatörde sunucudaki giriş ekranı açıldı (kanıt `gorsel-denetim-2026-11-05/ios/`). Gerçek telefon derlemesi: "No Accounts" — Xcode'a Apple ID kurucu girer (parola/hesap işi bende değil); telefon o an bağlı değildi (devicectl: unavailable). Adımlar `ios/OKUBENI.md`. |
| 05.11 15:10 | TEK | (bu commit, dal `ios`) | Kurucu "Apple'ın kendi özellikleri + Face ID ile giriş": iOS kabuğuna SF Pro yazı tipi (belge başı betik+biçem), sistem yazı boyutu (WebKit text-size-adjust 0,9–1,25; pageZoom denendi → yatay taşma, vazgeçildi; CSS var ölçeği denendi → Yıldız teması 246 sabit px, etkisiz), dokunma titreşimi (köprü `kabuk`), Face ID ile giriş (Anahtar Zinciri biometryCurrentSet/userPresence; giriş formu submit yakalanır → rota "/" → "kaydedilsin mi?" → "Face ID ile giriş yap" düğmesi formu doldurur; Face ID girişinde ve aynı e-posta kayıtlıyken yeniden sorulmaz; reddedilirse kasa silinir), Face ID kilidi + titreşim + yazı boyutu + sunucu adresi iPhone Ayarlar'da (Settings.bundle), NSFaceIDUsageDescription, ATS yerel ağ (yalnız localhost testi). Kanıt (iPhone 18 Pro sim, yerel 5199, demo tohum hesabı): SF Pro giriş ekranı · kaydet sorusu · Face ID düğmesi · Face ID ile giriş sonrası ana ekran (soru tekrar çıkmadı) · normal vs XXL yazı (`gorsel-denetim-2026-11-05/ios/`). TEYİTSİZ: simülatörde Face ID penceresi görünmedi (biyometri kaydı notifyutil ile; gerçek iPhone'da LAContext ister) · koyu mod: demo kullanıcının tema tercihi "aydınlık" olduğundan sistem koyu moduna geçmedi (uygulama tercihi; "sistem" seçilince izler). Release derlemesi 0 hata, test kancaları Release'te yok (strings 0). Telefona kurulum hâlâ Apple ID (Xcode Accounts) + kablo bekliyor. |
| 05.11 16:50 | TEK | (bu commit, dal `ios`) | Kurucu "mobil Apple kütüphanesindeki gibi, renkler kalsın" + "dönen elmas/amblem gerçek elmas" + "bizim çizime benzesin": yerli kabuk — alt sekme çubuğu (SF Symbols, 5 rota), üst çubuk (3B amblem · marka · kur · kullanıcı kısaltması), yerli hesap sayfası (Form/gruplu liste: dil, Basit/Pro, Face ID kilit, titreşim, yazı boyutu, Kılavuz, Ayarlar, Çıkış onaylı); web üst menüsü uygulamada gizli, köprüyle sürülür. 3B amblem web çizimiyle birebir (CSS matrix3d → 11 köşe/26 kenar/20 yüzey), SceneKit cam + parlayan kenarlar; giriş ekranında web ambleminin yerine biner. Hazır model aranıp (poly.pizza/Sketchfab/Womp) kurucuya gösterildi, kurucu "bizim çizime benzesin" dedi → hazır model yok. Renkler Yıldız belirteçlerinden (Marka/Ayrac/MetinSoluk/Kenarlik/ArkaPlan açık+koyu). Face ID e-postası Anahtar Zinciri'nde (yeniden kurulumda soru tekrarlanmaz), kullanıcı kısaltması "DK". Kanıt (iPhone 18 Pro sim, yerel 5199): giriş 3B elmas · ana (sekme/üst çubuk) · Müşteriler sekmesi · hesap sayfası açık/koyu (`ios/ios-yerli-*.jpg`). Tuzak: iki simülatör açıkken `simctl … booted` belirsiz → UDID ile hedeflendi. |

**Kurucu kararı bekleyen (kodlanmadı — mevcut kararlarla çelişiyor):**
- D1-08 eritme/fire/takoz ve D3-09 atölye/iş emri: K-13 ve H-5 "imalat kapsam dışı" kararları (denetim HURDA_FIRE_SIZINTISI kilidi, HARIC-LISTESI giriş ekranında).
- D3-03 çevrimdışı satış: satış sunucu kapılarına bağlı (3 dk kur tazeliği P0-3, MASAK/tevsik/yaptırım); çevrimdışı satış bu kapıları ancak sonradan çalıştırabilir.
- D1-20 "dene-gör": 20.10 kapatıldı — rakip sitelerinde ve planın kaynaklarında tanım YOK (web araması + belge taraması); tanımsız özellik kodlanmaz.

## Müşteriye sorulacaklar (ses kaydında belirsiz — varsayımla kodlanmadı)
1. **31:22 "bu olursa bunu listenin burasına çekin… KDV'sini böyle yazdın":** "liste" fiyat listesi mi, ürün kategorisi mi,
   yoksa bugünkü programınızda başka bir liste mi? (Ürün kuralı KDV/milyem/binde'yi zaten koşula göre dolduruyor.)
2. **23:08 "14 döneminde değişiyor":** KDV payı (binde) döneme göre mi değişiyor? Hangi dönem, hangi oran?
3. **24:11–24:25 "10 milyonluk ciro… binde 15'ini ver":** bu KDV mi, başka bir vergi/götürü mü? (mali müşavir teyidi)

## Kurucu kararı / dış bağımlılık bekleyenler
- e-Fatura/e-Arşiv entegratörü (GİB'e gönderim, gelen e-fatura, mükellef sorgusu)
- **Banka (19.10 D2-05b, c62beb8 + a20b27e):** 8 hazır bağlayıcı kodlandı (Ziraat, İş, Garanti, Akbank, VakıfBank, QNB, Kuveyt Türk,
  Albaraka); şifreli kimlik, kaydet+dene, sade hata, 120 sn çekim aralığı. 12.723 test 0 başarısız, denetim yalnız P-30.
  Tarayıcı: 1280/375 · açık/koyu ölçüldü (taşma 0). **Canlı bankada denenmedi** — açık kalanlar: (1) pilot işletmenin servis
  kullanıcısıyla her bankada ilk çekim (Ziraat/İş KISMEN: tarih biçimi + uç; QNB yön işareti; Albaraka ad alanı);
  (2) Garanti geliştirici portalına ByGatech uygulaması kaydı (kurucu yapar; client id/secret → ortam Banka__GARANTI__clientId/clientSecret);
  (3) sunucunun sabit çıkış IP'si (Banka__CikisIp; Akbank zorunlu); (4) HBH/6493 hukuki görüş (TEYİTSİZ).
  Yapı Kredi, Halkbank, DenizBank, Enpara: uç nokta/kimlik herkese açık değil → ekstre dosyasıyla (mevcut).
- **e-Fatura / e-Arşiv (20.10, ddfd465 + 320edd8):** faturalı satışta çıktı açılmaz; kesilen satış faturası arka planda
  entegratöre gider (katman entegratörden bağımsız, ilk bağlayıcı Nilvera — uçlar developer.nilvera.com OpenAPI'sinden).
  e-Fatura/e-Arşiv otomatik seçimi, yeniden deneme, GİB sonucu, resmi PDF, 8 günlük e-Arşiv iptali; İşletme › e-Fatura bağlantısı.
  12.737 test 0 başarısız, denetim yalnız P-30. **Açık:** (1) Nilvera test hesabı (kurucu açar) → test ortamında uçtan uca;
  (2) özel matrah kodu (Kdv:OzelMatrahKodu, K-10 mali müşavir teyidi) — tanımsızken kuyum faturası GÖNDERİLMEZ, sebep ekranda;
  (3) tarayıcı ekran görüntüsü: panelde oturum yok (5199 Production, davet anahtarı tanımsız) — kurucu giriş yapınca alınacak.
  Not: 20.10'da Docker durmuştu (MySQL + 5199 düşmüş); Docker açıldı, servis kickstart edildi. Boş yedek dosyası
  yedekler/kuyumcu_net-20261020-1039-ebelge-oncesi.sql.gz (20 bayt, Docker kapalıyken) geçersiz — geçerli yedek: …-ebelge-oncesi-2.sql.gz.
- **M8 kalanı (20.10, bu commit):** banka açıklamasındaki TC/VKN ile müşteri bulunur, "Gönd: AD" yalnız öneri; gelen ödeme borcu
  TAMAMEN kapatınca satışın kesilmiş faturası ÖDENDİ olur ve banka hareketine bağlanır (kısmi ödemede değişmez; geri alınca KESİLDİ).
  12.759 test 0 başarısız · denetim yalnız P-30 · 2 mutasyon yakalandı. **M11 entegratörsüz kısmı:** gün sonu toplu fatura 18.10'da
  vardı (FaturaToplu), kesilen fatura artık e-belgeye kendiliğinden gider → kapandı; Luca/Zirve biçimi örnek dosya bekliyor (dış).
  **Tezgâh Basit ≤12 düğme:** S22 testi kilitliyor → kapalı. **Kodlanabilir iş kalmadı;** kalanlar dış bağımlılık/kurucu kararı.
- **Tarayıcı kanıtı açığı (20.10):** e-Fatura çekmecesi ve fatura listesindeki e-belge durumu için 5199'da oturum yok; QA-TEST-1
  kiracısına test kullanıcısı ekleme denemesi izin kapısında reddedildi (hiçbir şey yazılmadı). Kurucu panelde giriş yapınca
  1280/375 · açık/koyu ölçülecek. Canlı rotalar ayakta: /api/v1/ebelge/* ve /bankalar/baglayicilar yetkisiz istekte 401.
  Günlük: "Banka çekici turu hatası" 1 kez, Docker kapalıyken MySQL'e ulaşılamadığından; sonrasında yok. Luca/Zirve herkese açık
  biçim araştırması başlatıldı (ARASTIRMA-LUCA-ZIRVE-AKTARIM-2026-10-20.md).
- **Luca/Zirve aktarımı (20.10 — KODLANDI, resmi herkese açık biçimlerden):** Veri dışa aktarımına iki kaynak: `luca-fis`
  (Luca "Excel Veri Aktarımı" şablonu birebir: Fiş No;Fiş Tarihi;Hesap Kodu;Evrak No;Evrak Tarihi;Açıklama;Borç;Alacak;Miktar;Döviz;Kur;
  fatura başına 1 mahsup fişi 120/600/391; ≤50 fiş/dosya; UTF-8 BOM + Windows-1254 kopyaları) ve `zirve-fatura` (Zirve "Fatura Excel"
  Sayfa1 başlıkları, kütüphanesiz .xlsx — openpyxl ile açılıp doğrulandı). Hesap kodu / VKN eksik fatura dosyaya girmez, OKUBENI'de
  listelenir. Kaynak: ARASTIRMA-LUCA-ZIRVE-AKTARIM-2026-10-20.md. **Dış bağımlılık:** mali müşavirin bir kez yüklemesi (Luca kodlaması
  ve Zirve .xlsx kabulü TEYİTSİZ); 600/391 alt hesapları yapılandırmadan.
- **Raporsuz paket kontrolü (20.10, 5dd6ef7 + c3167ab):** KONTROL-RAPORSUZ-PAKETLER-2026-10-20.md ile 17 paket kod/test/canlıya
  karşı tarandı; bulunan boşluklar iki partide kapandı. 1. parti: siparişe belge eki, hurda alışına fiş kimliği, işletme logosu
  çıktıda, cariden WhatsApp bağlantısı, zekât hesaplayıcı. 2. parti: PWA kabuğu (service worker yalnız yayında, /api dışarıda; yeni
  sürüm bandı; çevrimdışı göstergesi; Ayarlar › Uygulama › Ana ekrana ekle), e-belge olay izi (ebelge_olaylari, append-only) +
  resmi XML + /e-belgeler giden kutusu (ortak ızgara, kılavuz bölümü), sipariş özeti ucu, POS–fatura mutabakatında kalem toplamı
  kontrolü. 12.780 test 0 başarısız · denetim yalnız P-30 · 54 rota / 13 betik / 97 varlık. Yedek: kuyumcu_net-20261020-16xx-
  ebelge-olay-oncesi.sql.gz. **Kalan boşluklar (sırada):** PAKET-46 roller (KASIYER/CIRAK, kullanıcı yetkileri, maliyet görme kapısı),
  PAKET-37 kasa+banka döviz bazlı net varlık, PAKET-42 sevk durum makinesi, PAKET-36 /patron paneli, PAKET-05 sayım tutanağı +
  gün kapanış fişi, PAKET-04 POS kalem görseli, PAKET-39 has defteri L, doğum günü listesi, PDF katalog, D1-20 prim simülasyonu.
  **Yayın + tarayıcı kanıtı (20.10 20:10):** yayin-temiz raporsuz-2 → 5199 @ 283a8f1 (bütünlük kapısına service-worker.published.js
  gerekçeli istisnası eklendi; Blazor yayında onu service-worker.js yerine yazar). /e-belgeler demo kiracısında (inceleme vekili 5179)
  1280 açık/koyu + 375 açık ölçüldü: taşma yok, boş durum + 6 sayaç; satır çizimi EBelgelerEkranTestleri (sahte API) ile — gerçek
  gönderim Nilvera test hesabına bağlı (dış). Service worker: uygulama içi tarayıcı paneli SW kaydını reddediyor ("unknown error",
  panel kısıtı); gerçek Chrome (headless, CDP) ile 5199'da kayıt activated, önbellek bygatech-kabuk-… oluştu — ÇALIŞIYOR. Tema
  Karanlık'a alınıp geri Aydınlık'a döndürüldü. Vekil WebSocket (hubs/kur) taşımıyor — bilinen vekil kısıtı.
- **Ses kaydı 2. dinleme + rakip eğitim/belge taraması (20.10):** ses kaydında yeni madde yok (musteri-gorusmesi § J). Rakip
  taraması RAKIP-EGITIM-VE-BELGE-TARAMASI-2026-10-20.md: § 4.1 "hemen yapılabilir" 18 kalem (gişe zimmeti, günlük servet özeti,
  banka PDF/Excel ekstre okuma, hurda pazarlık payı, fiyat teklif fişi, kasa sayım kuralları, canlı fiyat QR…) → kapatma planı sırada.
- **PAKET-37 (20.10, 8803ce2):** altın pozisyonu kartında "Bugünkü servet" — kasa+banka para birimi bazında (kur: güncel kur
  kaydı USD/EUR/GBP), altın net pozisyonu × has fiyatı, cari TL/USD/EUR bakiyeleri; kur ya da has fiyatı yoksa ilgili kalem ve
  toplam NULL + sebep kodu (uydurma yok). Mutasyon kanıtı: cari kalemi toplamdan düşünce test kırmızı. 12.785 test · denetim P-30.
- **PAKET-42 (20.10):** sevk irsaliyesi VUK alanları (taşıyan, plaka, fiili sevk, teslim alan/zaman — irsaliyeler tablosuna 5 kolon,
  geçiş IrsaliyeSevkAlanlari; yedek kuyumcu_net-20261020-2024-kdv-zorla-oncesi.sql.gz) + durum makinesi TASLAK→SEVK_EDILDI→
  TESLIM_EDILDI (atlama 400, sevk sonrası başlık 409, tekrar 409), kâğıt çıktıda sevk satırı, /irsaliyeler detayında "Yola çıktı" /
  "Teslim edildi" katlanırları, "İrsaliyesiz satışlar" katlanırı (SATIS hareketi ↔ irsaliye satırı hareket bağı; kalemde hareketId).
  Mutasyon kanıtı: "önce sevk" kapısı kalkınca test kırmızı. SUBE_TRANSFER/ATOLYE türleri ve satıştan "irsaliye oluştur" kısayolu
  (Faz B) kapsam dışı bırakıldı — ihtiyaç çıkınca.
- **Y-02 (20.10, rakip taraması):** tezgâh Profesyonel modda "Kısayollar" katlanır satırı (F2/F4/F8/F9/Ctrl K) + kılavuzda tablo.
  Islemler.razor 1400 satır tavanı için ayrı bileşen (Bilesenler/Fis/FisKisayollari.razor).
- **Y-11 (20.10, rakip taraması — fiyat teklif fişi):** tezgâhta "Fiyat teklifi ver" = bekletme + KUR KİLİDİ (zorunlu; teklif kilit
  süresince geçerli, tezgâhtar 60 dk, sahip/müdür 30 güne kadar) → numara TKL-xxxxxxxx (ULID sonu; sıra iddiası yok), geçerlilik =
  kilit bitişi, döküm /cikti/TEKLIF/{id} ("FİYAT TEKLİFİ", kalemler, kilitli has fiyatı, geçerlilik, "satış değildir; stok ayrılmaz").
  Bekleyen listesinde teklif satırı geçerlilik/doldu + "Teklifi yazdır"; "Al" aynı kur kilidini tezgâha bağlar; süresi dolan teklif
  alınmaz (409), dökümde "SÜRESİ DOLMUŞTUR". bekleyen_fisler +3 kolon (geçiş BekleyenFisTeklif). Mutasyon kanıtı: süre kapısı kalkınca
  test kırmızı. Belge türü 12 → 13.
- **Y-10 (20.10):** müşteri fişinde (satış fişi / tahsilat makbuzu) "Önceki bakiye → Yeni bakiye" (TL; has varsa has) — ekstreyle
  AYNI yürüyen-bakiye kuralı (kayıtlı bakiyeden sonraki hareketler geri alınır); Peşin müşteride ve etkisiz fişte satır yok.
- **Y-15 (20.10):** İşletme › Ayarlar › "Mali müşavirin hesap kodları" (satış 600 / KDV 391, alt kırılım 600.01); GET/PUT
  /muhasebe-hesaplari; Luca fişi ve OKUBENI etkin kodları kullanır; biçim kapısı (rakam+nokta) 400.
- **Tarayıcı kanıtı (20.10 21:15, 5199 @ 9f72145):** /irsaliyeler IRS-2026-00004 (demo, Derya Yıldızlı) → "Yola çıktı" paneli →
  Sevk: Yola çıktı · Aras Kargo · 34ABC123 → Teslim paneli; "İrsaliyesiz satışlar" 21 satır. Tezgâh (pro): "Fiyat teklifi ver"
  düğmesi, kilitsizde "Önce kuru kilitleyin…" + süre seçici (15 dk…30 gün). Kısayol satırı Yıldız'da gizli başlıkta kalmıştı →
  Yıldız eylem satırına taşındı (bu commit); arayüz modu Basit'e geri alındı. İrsaliyesiz listede has/tutar artık işaretsiz.
- **D1-20 dene-gör (20.10):** Satış analizi › Prim sekmesinde "Deneyin: bu oranla ne öderdim?" — GET /raporlar/prim?oran=X, dönemin
  primi herkes için o oranla hesaplanır, yanıt SimulasyonOran taşır, ekranda "Deneme — hiçbir şey kaydedilmedi" bandı; kayıtlı
  yüzde değişmez (test). Mutasyon kanıtı: oran yok sayılınca 1000 ≠ 400. Y-10/Y-15 mutasyonları da yakalandı. 12.794 test 0 başarısız.
- **DG-1 doğum günü listesi (20.10):** GET /cari/dogum-gunleri?gun=30 (aktif + doğum tarihi yazılı; yıl dönümü ≤ N gün; 29 Şubat
  kuralı), Müşteriler ekranında tuvalin alt notunun altında KAPALI katlanır "Yaklaşan doğum günleri (30 gün)" — ad · tarih ·
  bugün/yarın/N gün sonra · yaş · WhatsApp (kutlama metni hazır). Mutasyon: pasif cari süzgeci kalkınca test kırmızı.
  Not: tuval dışı öğe → kurucunun görsel onayı; kapalı katlanır olduğundan ekranın açılış görünümü değişmedi.
- **Tarayıcı kanıtı (20.10 21:45, 5199 @ 5e8ca25):** İşletme › Ayarlar › "Mali müşavirin hesap kodları" paneli DOM'da (etiket "Satış
  hesabı (boşsa 600)"), ekran görüntüsü sekme kaydırmasında yakalanamadı — yeniden alınacak; Satış analizi › Prim ve hedef › "Deneyin: bu
  oranla ne öderdim?" %5 → Toplam prim 46.648,88 ₺ + "Deneme — … hiçbir şey kaydedilmedi" bandı (görüntü alındı).
- **PAKET-05 kalanı (20.10):** /cikti/SAYIM_TUTANAGI/{oturum} (sistem/sayılan/fark, eksik-fazla, net has farkı, "sayılmadı
  bilinmeyendir", ara tutanak/iptal uyarısı; Sayım başlığında Yazdır) ve /cikti/GUN_KAPANISI/{kapanış} (defter/sayılan/fark,
  kapatan, açıklama; gün kapanış yanıtı kapanisId taşır, Kasalar'da "Yazdır — Gün kapanışı"). Belge türü 13 → 15. Mutasyon:
  kapanisId düşünce test kırmızı. Müşteriler ekranı DG-1 katlanırı canlıda görüldü (demo: "Bu dönemde doğum günü yok.").
- **Y-17 gider fiş fotoğrafı (20.10):** belge deposuna GIDER dalı (sahip = gider kategorili kasa satırı; yabancı/olmayan 404),
  Gider ekranında kayıt sonrası "Fiş fotoğrafı ekle" katlanırı + geçmiş satırda "Belge" çekmecesi (kamera). Mutasyon: gider
  süzgeci kalkınca test kırmızı. Sayım tutanağı bağlantısı Yıldız sayım bilgi satırına da kondu (klasik başlık Yıldız'da yok).
  **Kapsandı sayılanlar:** Y-07 kur kaynağı seçimi (ALTINKAYNAK/ANKARA/TRUNCGIL/MANUAL + has/döviz kaynağı), Y-08 hurda
  pazarlık payı (IslemFiyatKapisi: bant dışı → SAHİP/MÜDÜR onayı + gerekçe), Y-12, Y-13.
- **Y-14 canlı fiyat bağlantısı (20.10):** dış API anahtarına süre (bitis; geçiş ApiAnahtariBitis, yedek
  kuyumcu_net-20261020-2223-api-anahtari-bitis-oncesi.sql.gz), GET /api/disa/v1/pano?anahtar= (KUR kapsamı; süresi dolan 401,
  kapsam dışı 403), oturumsuz kabuksuz /fiyat/{Anahtar} ekranı (vitrin panosu; ibare seçilmemişse fiyat yok; 60 sn yenileme),
  İşletme › Dış API çekmecesinde "Canlı fiyat bağlantısı oluştur (30 gün)" + kopyala. Rota 54 → 55. QR üretimi YOK (kütüphane/CDN
  yasağı; bağlantı kopyalanır). Mutasyon: süre kontrolü kalkınca test kırmızı.
- **Ertelenen (tuval dışı öğe, kurucu görsel onayı):** PAKET-04 fiş kaleminde ürün görseli (Yıldız tezgâh kalem satırında görsel
  yuvası yok); Y-09 stok durumları (rezerve/tamirde/yolda — stok modeline durum kolonu, M).
- **Y-04 servet farkı (20.10):** servet_gunluk tablosu (kiracı × gün; geçiş ServetGunluk, yedek kuyumcu_net-20261020-2243-servet-gunluk-
  oncesi.sql.gz); gün kapanışı PozisyonServisi.ServetAnlikGoruntuKaydet çağırır (hata kapanışı düşürmez); NetVarlikOzeti OncekiGun/
  OncekiNetTl/OncekiAltinNetHasGram; kartta "Son gün kapanışına göre (gg.aa.yyyy) fark: ±TL · has ±g". "Dünle fark" yerine dürüst
  "son kapanışla fark" (kapanış yoksa satır yok). Mutasyon: kapanışta görüntü yazılmazsa test kırmızı.
- **Y-05 personel avansı (20.10):** kasa_hareketleri.personel_id (geçiş KasaHareketPersonel; yedek kuyumcu_net-20261020-2303-personel-
  avans-oncesi.sql.gz); gider kaydında isteğe bağlı personel (aktif kullanıcı; yabancı 400), listede "avans · Ad"; prim raporunda
  AvansTl ve NetOdemeTl (prim − avans), Satış analizi prim sekmesinde Avans · Net ödenecek sütunları; Yıldız gider formunda seçici.
  Commit db65b28, yayın y05 (23:28). Tarayıcı kanıtı 5179: /gider formunda "Personel avansı (isteğe bağlı)" seçici personel listesini
  açıyor; /raporlar/satis-analizi?sekme=prim tablosunda Avans · Net ödenecek sütunları; 375 px taşma yok (scrollWidth 375).
- **Y-03 gişe zimmeti (21.10, rakip Ariyen "Gişe Kasam" 7 adım):** gise_zimmetleri + gise_zimmet_kalemleri (geçiş GiseZimmeti; yedek
  kuyumcu_net-20261020-2347-gise-zimmeti-oncesi.sql.gz); /api/v1/gise: zimmetle (SAHIP/MUDUR; kendine veremez; şube stoğu kapısı; kasa ve
  personel başına tek açık zimmet 409) → tezgâhtar kabul (403 başkası; açılış nakdi kaynak kasadan gişeye VirmanServisi) → takviye talebi
  (ürün ya da nakit; karar SAHIP/MUDUR ve isteyen ≠ karar veren 403) → kapat (ürün başına sayılan zorunlu; beklenen = zimmet + takviye −
  tezgâhtarın SATIS−SATIS_IADE defteri; nakit beklenen KasaNakitAyrimi; fark varsa K30 gerekçe; nakit farkı gişeye sayım farkı hareketi;
  sayılan nakit devir kasasına virman; SAYIM satırları dondurulur) → iptal yalnız TEKLIF'te. Kör sayımda tezgâhtar beklenen nakdi görmez.
  /gise ekranı (menü Para ve banka 43, rota 56, varlık 100, ölçüm tabanı güncellendi); tezgâhtara açılışta kendi zimmeti doğrudan gelir;
  takviye ve akşam sayımı kapalı katlanır; Yıldız açıklama/ikon; kılavuz "Gişe zimmeti" bölümü. Commitler 4b3e215 (paket), c531073 +
  5586acc (uzak ürün seçicisi ad: ürün kartından okunur; ilk düzeltme derlenmeden yayınlandı → 5199 ~5 dk düştü, ders: hafıza
  yayin-oncesi-derle), 0ac733d (gerekçe alanı yalnız fark varsa), a3e2741 (liste tarihi işletme saati). Yayın y03e 00:3x.
  Mutasyon: satılan düşülmezse (beklenen 5≠3) ve kendi-talebi kapısı kalkarsa (403→200) Y03 testi kırmızı. 12.808 test.
  Tarayıcı kanıtı 5179 (sahip) + 5180 (tezgâhtar Emre Duman vekili): Gişe 1 kasası açıldı (demo kalıntısı, bakiye 0); zimmet
  KLY-14-004 × 2 + 500 ₺ → "Tezgâhtar onayı bekliyor" → tezgâhtar kabul "Gişede · Beklenen nakit 500 ₺" → akşam sayımı 2 / 500 ₺ →
  "Gişe kapandı; 500 ₺ Ana Kasa kasasına devredildi", Ana Kasa bakiyesi 904.389,07 ₺'ye geri döndü, Gişe 1 = 0; 375 px taşma yok
  (scrollWidth 375), geçmiş kartı. Sunucu kapısı canlıda görüldü: "Şube stoğu yetersiz: BLZ-22-015 için 0 var, 2 zimmetlenemez" (alan altı).
  Demo kalıntısı: Gişe 1 kasası + 1 kapanmış zimmet (Emre Duman).
- **Y-01 eğitim videoları (21.10, rakip Hasfiyat/Pusula/KuyumSmart):** ekran başına 9 sesli video (satış 1:10 · hurda 0:53 · emanet 0:48 ·
  banka 0:44 · fatura 0:50 · MASAK 0:47 · kasa kapanışı 0:51 · etiket 0:41 · rapor 0:53; toplam 7,9 dk, 9,4 MB) — onaylı ses (Chatterbox v3,
  02.10 ayarı; 80 cümle), 1440×900 ekran kaydı, bölüm işaretleri gömülü. Yayın: wwwroot/video/egitim-<id>.mp4 (SW ön belleğine girmez);
  Kılavuz ekranında (/kilavuz) oynatıcı + bölüm düğmeleri (tıklanınca o saniyeye atlar; EgitimVideolari.cs üretilmiş liste); kılavuz md
  "Eğitim videoları" bölümü + 9 ekran bölümüne tek satır (üretici dış bağlantıya izin vermediği için iç çıpa). Hat: egitim-videolari-2026-10-21/
  (README; plan.py → seslendir.sh → egitim-kaydet.mjs → uret-egitim.py → kilavuz-bolum.py). Yalıtılmış ortam: kuyumcu_egitim şeması
  (20.10 23:47 yedeği), API 5192 (Masak__PesinPencereDakika=0), vekil 5181; canlı demoya dokunulmadı; kayıt sonrası kapatıldı.
  Commitler b48b381 (paket; kök *.mp4 kuralı videoları dışarıda bırakmıştı → /video 404) + cbeabab4 (istisna + mp4'ler). Yayın y01b 02:0x.
  Tarayıcı bulgusu ve düzeltmesi: tezgâh alt düğme satırı (Beklet · Fiyat teklifi ver · Vazgeç · Kaydet) 1280/1440'ta sağdan taşıyordu
  (Kaydet sağ kenarı 1348 > 1280) → tema-yildiz.css sarma; 1280'de ölçüm: sağ kenar 1216, satır iki sıra. Testler: Y01EgitimVideolariTestleri
  (liste 9 · süre 30–180 sn · mp4 var/boyut · SW mp4 içermez · Kılavuz oynatıcı/bölüm düğmesi JS çağrısı); ortak.css sabit renk testi
  (O7) kırmızıya düştü → var(--metin). 12.811 test. Tarayıcı kanıtı 5179: /kilavuz 9 oynatıcı + bölümler; "0:28 Ödeme" düğmesi videoyu
  28. saniyeden oynatıyor (paused=false); 375 px taşma yok.
  Mutasyon: avans düşülmezse test kırmızı. Maaş alanı yok (sistemde maaş tutulmuyor) — "maaş+prim−avans tek fiş" kapsam dışı.
- **Tarayıcı kanıtı (20.10 23:00, 5199):** Y-14 İşletme › Dış API › "Canlı fiyat bağlantısı oluştur" → bağlantı üretildi → 5199'da
  oturumsuz /fiyat/{anahtar} açıldı (demo işçilik ibaresi seçmediği için fail-closed uyarısı; 1280 + 375). Sayım Yıldız "Yazdır" →
  SAYIM TUTANAĞI dökümü. Gider Yıldız formu: Belge/yükleyici artık bant altında (yeniden yayınla doğrulanacak).
- **Ekip kararı (20.10) — PAKET-46 ince yetkiler/KASIYER-CIRAK ERTELENDİ:** rol×modül matrisi + işletme onay eşikleri (iskonto/tutar/
  zararına) + kişisel iskonto sınırı + kâr-zarar uçlarının MUHASEBE+ kapısı mevcut; yeni rol 4 ekran + 30 teste yayılan değişiklik,
  müşteri talebi yok → satışa hazırlık listesinde değil. Y-12 (kasa eksiye düşemez, sayılmayan satır = null) ve Y-13 (yaptırım
  eşleşmesinde liste adı tezgâhtara gösterilmez) ZATEN SAĞLANIYOR — kapatıldı.
- Harem veri izni · P-30 kronometreli demo ölçümü (insan)

