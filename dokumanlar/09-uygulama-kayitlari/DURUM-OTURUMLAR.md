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
| 19.10 00:26 | TEK | 635d216 | D3-01 ana ekrana ekle (manifest, Mihenk amblemi ikonları, iOS etiketleri; Chrome manifesti hatasız okudu) | 12705/0 | d3-01 |
| 19.10 01:02 | TEK | 7242eba · 0e7cd72 | D3-11 dış API (anahtar özetle saklanır, KUR/URUN kapsamı, iptal) + canlı kur kanalı /hubs/disa-kur — canlıda uçtan uca: 200 / kapsam dışı 403 / iptal sonrası 401 | 12707/0 · mutasyon yakalandı | d3-11b |

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
  (2) Garanti geliştirici portalına Mihenk uygulaması kaydı (kurucu yapar; client id/secret → ortam Banka__GARANTI__clientId/clientSecret);
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
- Luca/Zirve aktarımı (mali müşavirden örnek dosya) · Harem veri izni · P-30 kronometreli demo ölçümü (insan)

