# Müşteri istekleri — tek tek kontrol (01.11.2026, demo öncesi)

**Kaynak:** `dokumanlar/01-gereksinimler/musteri-gorusmesi-2026-10-16.md` (ses kaydı 16.10, 35 dk; bölüm 1–8, EK-2, EK-4, EK-5, J).
**Nasıl kontrol edildi:** her madde kodda aranıp (dosya:satır) eğitim kopyasında (5192 API → 5181 vekil, demo sahip "Hakan Y.") gerçek tarayıcıda açıldı.
Canlı 5199 verisine YAZILMADI; tarayıcıda satış/fatura/havale KAYDEDİLMEDİ (yalnız ekranlar açıldı, satış sepeti "Vazgeç" ile boşaltıldı).
Ekran görüntüleri: `musteri-istek-kontrol-2026-11-01/` (pano genişliği ~800 px).

**Durum sözlüğü:** HAZIR = tarayıcıda gördüm · KISMEN = bir kısmı var (eksik yazılı) · YOK · DIŞ = başkasına bağlı (kimde olduğu yazılı).

| # | Müşterinin sözü (dakika) | Durum | Kanıt | Demoda nasıl gösterilir | Eksikse ne gerekir |
|---|---|---|---|---|---|
| 1a | "Altınkaynak uygun değil, Harem'den çekin" (09:40) | HAZIR · ⚠ DIŞ (izin) | 01-kur.jpg — /kur KAYNAK: Altınkaynak · Ankara Altın Borsası · Truncgil · Harem Altın; `Kuyumcu.Api/Modules/Kur/KurKaynaklari.cs:193` HaremAdaptoru | Kur ekranında KAYNAK'ı Harem Altın'a çevir, fiyatların değiştiğini göster | Harem resmî API değil, sözleşmesiz uçtan okunuyor (`KurKaynaklari.cs:11-13`, kurucu 17.10: "satıştan sonra izin başvurusu"). Ticari kullanım öncesi yazılı izin (kurucu) |
| 1b | "Otomatik gelse de elle müdahale edebilmeliyim" (09:27) | HAZIR | 01-kur.jpg "Elle gir" düğmesi; ürün kartında "Fiyat farkı (₺) — eksi yazılabilir" (03) `Entities.cs:581` FiyatOfsetTl | Kur'da "Elle gir"; ürün kartında fiyat farkı alanı | — |
| 1c | "Tek noktadan çekmeyecek; ürün başına farklı kaynak ya da sabit fiyat" (00:00) | KISMEN | Sabit fiyat: ürün kartı "Satış fiyatı (₺)"; cins: "Altın cinsi" (çeyrek/Reşat kurundan); kaynak tek ve işletme genelinde (/kur KAYNAK). Ürün başına KAYNAK alanı yok (`Entities.cs` Urun sınıfında kaynak alanı yok) | "Bu ürün sabit fiyat, bu ürün çeyrek kurundan" diye iki ürün göster | Ürün başına kaynak seçimi (Harem/Altınkaynak) yok — istenirse yeni iş |
| 1d | "1 gram buradan, 5 gram oradan" kademe (00:15) | DIŞ | J bölümü: M10, kademeli kaynak (Harem) veri izni | Söyleme — "izin sonrası" | Harem/İKO yazılı veri izni (kurucu) |
| 2a | Satış = gram × kuyumcunun milyemi × has (10:25) | HAZIR | 03-urun-milyem-kdv-pay.jpg — "Satış milyemi (örn. 950 — fiyat: gram × milyem × has)", "Alış milyemi (maliyet) 925"; `Entities.cs:570-579` | Burma bileziği aç, alış milyemi 925'i göster | — |
| 2b | "Bilezik çeşitlerinin milyemi ayrı" (17:40) | HAZIR | 04-fiyat-kurallari.jpg — kural "Bilezikler: adında 'bilezik' → alış 925 · KDV payı ‰1"; her ürün kartında ayrı milyem | Fiyat kuralları penceresini aç | Demo verisinde tek bilezik ürünü var (Burma, stok 0) — CNC/el örgüsü/kilitli örnek ürünü yok |
| 2c | "Gram ve değer birlikte görülmeli" (07:02) | HAZIR | 05-satis-gram-has-tl-36bin.jpg — satış satırı "1 adet · 10,00 g · 9,1600 g has · 59.835,32 ₺" | Tezgâh'ta bir ürün ekle | — |
| 2d | "Has değeri kâr gibi görünmesin, bilezik kârsız satılır" (05:31, EK-5) | HAZIR | 09-kar-zarar-iscilik-makas.jpg — Kâr-zarar: para kârı, altın (has) kârı, "İşçilik (makas) kârı" ve "Altın fiyat farkı" ayrı; maliyeti bilinmeyen satış kâra katılmıyor | Raporlar › Kâr-zarar › Geçen ay | Demo verisinde 26 satışın parti maliyeti yok, 16 satışın alış günü altın fiyatı yok → rapor uyarı satırlarıyla dolu görünür |
| 3 | Eski çeyrek, yarım, tam, ata, Reşat (07:27) | HAZIR · Gremse KISMEN | 02-urunler.jpg + /urunler metni: Yeni/Eski Çeyrek, Yeni/Eski Yarım, Yeni/Eski Tam, Yeni/Eski Ata, Reşat, Hamit ürün olarak var; /kur'da Yeni/Eski Gremse ve Ata Beşli kuru var | Ürünler › SARRAFİYE sekmesi | Gremse ve Ata Beşli için demo ÜRÜNÜ yok (kur var) — 2 ürün eklenmeli |
| 4a | Ürün kartında KDV oranı ve "binde X" pay (26:36) | HAZIR · ⚠ yasal | 03 — "KDV'ye giren pay (binde): Örn. 1 = toplamın binde 1'i KDV'ye girer" ve ayrı "KDV oranı (%)"; `Entities.cs:583-587` KdvMatrahBinde | Ürün kartında iki ayrı alanı göster | Sabit binde pay **matrahı gerçekten değiştiriyor**; EK-3/EK-4 "mevzuatta yok, yalnız uyarı / müşavir onaylı kilit" demişti. Faturalar ekranı "Özel matrah kodu: mali müşavir onayı bekleniyor" yazıyor (07). Mali müşavir görüşü (kurucu) |
| 4b | Ayarlar'da "adında bilezik geçiyorsa binde 1" kuralı (25:53) | HAZIR | 04-fiyat-kurallari.jpg — Ürünler › Fiyat kuralları: ad kuralı + "Önizle" | Kuralı aç, Önizle'ye bas | — |
| 4c | Yasal özel matrah (satış − has bedeli) | HAZIR (kod) | `Entities.cs:585` "Boş = yasal hesap (KDVK 23/e)"; 07 not satırı | Söz konusu olursa "binde boşsa yasal hesap" | Müşavir onayı (aynı) |
| 5a | "Fişe gerek yok, TC'yi yazınca fatura kessin" (34:50) | KISMEN | `Kuyumcu.Web/Pages/Islemler.Belge.cs:7-20` — müşterili satışta "Fatura kesilsin mi?", had üstünde zorunlu; Tezgâh'ta TC ile müşteri bul/aç (M6b). 07-faturalar: 21.10'a kadar kesilmiş FTR-2026-000001…12. Bu turda satış KAYDEDİLMEDİ | Müşteri seç → satış → "Fatura kesilsin mi? Evet" | Fatura ByGatech içinde kesiliyor; GİB'e gitmiyor (5b) |
| 5b | "Kesilince direkt gönderiyoruz" (20:32) | DIŞ | 08-e-fatura-giden-kutusu-bos.jpg — "Henüz gönderilen fatura yok"; `Kuyumcu.Api/Modules/EBelge/EBelgeSaglayici.cs:10-14` Nilvera bağlayıcısı "test ortamında henüz DENENMEDİ" | GÖSTERME — "entegratör bağlanınca" de | Entegratör seçimi + test hesabı (kurucu) |
| 5c | "30 bin geçince isim" (02:33) | HAZIR | 05 — "36.000 ₺ üstünde fatura zorunlu" + "Fatura için müşteri seçin" kilidi | 36 bin üstü ürün ekle, kilidi göster | (Müşteri 30 bin dedi; 2026 haddi 36.000 — H-2 sorusu) |
| 6a | "Para düştüğü anda ekranda" (01:10) | HAZIR (demo banka) | 11-banka-karar-kartlari.jpg — "Karar bekleyen gelen ödemeler"; "Havale gönder (demo)" | Banka › Havale gönder (demo) → kart birkaç saniyede düşer | Gönderen IBAN ya da TC şart (yalnız adla gelen kart açmaz — tasarım) |
| 6b | Eşleştirme + "borcundan düşülsün mü" | HAZIR | 11 — "Hesabına alacak yaz · Ürün seç ve sat · Satış zaten girildi"; `GelenOdemeModulu.cs:13-28` (TC → IBAN → ad yalnız öneri) | Kartta bir seçeneğe bas | — |
| 6c | Gelen + giden + bakiye (Faturalog gibi) | HAZIR (eğitim verisiyle) | `garanti-bakiye-2026-10-31/1280-acik-ayrinti.jpg` "Bankadaki bakiye · bloke · saat" + "ByGatech kaydıyla fark"; `1280-acik-giden-satiri.jpg` "↑ Giden" | Hesap › Hareketler | Bankadaki bakiye yalnız Garanti bağlayıcısında; gerçek bankadan henüz hiç bakiye gelmedi |
| 6d | Gerçek banka bağlantısı ("Kuveyt Türk direkt yaptı") (00:48) | DIŞ | Garanti: test onayı var; deneme "There is no configured IP on Portal Application" → IP 151.135.190.195 onaya gönderildi (01.11). Kuveyt Türk portal formu doldu, gönderilmedi. 8 bağlayıcı kodda | GÖSTERME (canlı banka yok) — demo bankası göster | Garanti IP onayı + kurucunun hesap onay numarası (consentId) + sabit IP sunucusu (kurucu, ücretli) |
| 7a | Alış işçilik faturası kayda girsin (10:53) | HAZIR | 10-alis-iscilik-belgesi.jpg — "Ödenen 93.358,40 · Has bedeli 92.450,05 · İşçilik 908,35 · Fatura ADT2026000001234" | Kâr-zarar › Alış işçilik belgeleri | — |
| 7b | Kâr raporunda işçilik ayrı | HAZIR | 09 — "İşçilik (makas) kârı 32.037,11 ₺" ayrı kutu | Aynı ekran | — |
| 8-F1 | Çok banka tek panel | KISMEN | Banka listesi çok hesap gösteriyor (Garanti test + Ziraat demo) | Banka ana ekranı | Gerçek bağlantı DIŞ (6d) |
| 8-F2 | Gelen ödeme → otomatik fatura | KISMEN | Kart "Ürün seç ve sat" → havaleden satış faturalı (`Islemler.Belge.cs:17`); `BankaModulu.cs:581` FaturayaDonustur | Karttan "Ürün seç ve sat" | Tam otomatik (onaysız) fatura yok — bilinçli: kuyumcu onaylar |
| 8-F3 | Transferden cari otomatik açma | HAZIR (kod) | `GelenOdemeModulu.cs:19-23` TC/IBAN varsa müşteri kendiliğinden açılır, ad benzerliği bağlamaz | Demo havalede yeni TC ile gönder → müşteri listesinde görünür | Bu turda yeni havale gönderilmedi (kayıt oluşturmamak için) |
| 8-F4/F5 | e-Fatura giden/gelen, mükellef sorgusu | DIŞ | `EBelgeModulu.cs:79,242` MukellefSorgula kodda; 08 kutu boş | GÖSTERME | Entegratör (kurucu) |
| 8-F6 | Toplu fatura | HAZIR (ekran) | 07 — "Toplu fatura" düğmesi; `Kuyumcu.Web/Pages/Faturalar.Toplu.cs` | Faturalar › Toplu fatura | GİB'e gönderim DIŞ |
| 8-F8 | 10 yıl saklama | KISMEN | Kayıtlar silinmiyor (defter); belge saklama süresi tanımı yok (EK-2) | Söyleme | Saklama politikası + entegratör arşivi |
| 8-F9 | Luca / Zirve aktarımı | KISMEN | `Kuyumcu.Api/Modules/Disaaktarim/MuhasebeAktarimlari.cs:11-22` luca-fis CSV + zirve-fatura xlsx; "son kanıt mali müşavirin bir kez yüklemesiyle" | Veri dışa aktarım ekranından dosya indir | Mali müşavir gerçek yükleme denemesi (DIŞ) |
| 8-F10/F11 | Kontör fiyatı, ISO rozeti | YOK | — | Söyleme | Kurucu/satış kararı |
| 8-F12 | Mobil | KISMEN | Bu turdaki tüm ekranlar ~800 px panoda çalıştı; `wwwroot/manifest.json` + service-worker var; mağaza uygulaması yok | Telefondan tarayıcıyla aç | Mağaza uygulaması yok |
| 9a | Nakit + kart karışık ödeme (19:16) | HAZIR | 06-karisik-odeme.jpg — "+ Başka ödeme": Kart + Havale satırları, "+ Taksit", "− Tek ödeme" | Satışta "+ Başka ödeme" | — |
| 9b | Fişten fatura (20:51) | HAZIR (ekran) | 07 — "Fişten Fatura Kes" düğmesi | Faturalar › Fişten Fatura Kes | Bu turda kesilmedi |

## Sayım
HAZIR 20 · KISMEN 7 · YOK 1 · DIŞ 4 (toplam 32 satır; "HAZIR · ⚠" olan 1a ve 4a ile "Gremse KISMEN" notlu 3 HAZIR sayıldı, uyarıları ayrıca aşağıda). HAZIR'ların 4'ü (4c, F3, F6, 9b) bu turda yalnız ekran/kod düzeyinde görüldü, kayıt oluşturularak denenmedi.

## Bu turda görülen sorunlar (kod değiştirilmedi)
1. **Demo verisi karışık:** banka ekranında aynı gün üç kez "Ayşe Demir 2.500 ₺ gönderdi" kartı; müşteri seçicide 5 ayrı "Ayşe Demir" (5 farklı TC — hata değil, 21.10 eğitim kaydından kalma). Demoda "aynı kişi 5 kez açılmış" gibi görünür. Canlı demo kiracısında (5199) aynısı var mı ayrıca bakılmalı.
2. **Gremse / Ata Beşli ürünü yok** (kur satırı var) — müşteri "hepsi tanımlanmalı" demişti.
3. **Kâr-zarar demo verisinde maliyetsiz satışlar** ("26 satışın partisinde alış maliyeti yok", "16 satışın alış günü altın fiyatı yok") — rapor doğru uyarıyor ama demoda güven sarsar.
4. Tezgâh'ta ilk barkod denemesinde (8690000000074) "Bu barkodla aktif ürün bulunamadı" çıktı; API aynı barkoda 200 döndü, ikinci denemede ekran ürünü ekledi. **Tekrarlanamadı** — sayfa tam yüklenmeden yazılmış olabilir; demoda barkodu sayfa açıldıktan birkaç saniye sonra okutun.
5. "Satış fiyatı/maliyet · eklenen · KDV payı" kırılımı (EK-5 önerisi) Tezgâh satır ayrıntısında görünmedi (satıra basınca yalnız Açıklama/Çıkar/Tamam). Kâr ayrımı raporda var.

## Demoda SÖYLENMEMESİ gerekenler
- "Faturalar GİB'e gidiyor" — gitmiyor; entegratör bağlı değil, giden kutusu boş.
- "Bankanızdan para gerçek zamanlı geliyor" — gerçek bankadan henüz tek hareket gelmedi; gösterilen demo bankası.
- "Harem fiyatı resmî/izinli" — sözleşmesiz uç; ticari kullanım izni yok.
- "Binde 1 KDV payı yasal" — müşavir onayı yok (ekran da "mali müşavir onayı bekleniyor" diyor).
- "Luca/Zirve'ye birebir aktarılıyor" — dosya üretiliyor, mali müşavir yüklemesiyle denenmedi.
- "1 gram / 5 gram kademeli fiyat var" — yok (veri izni bekliyor).

## Müşteriye sorulacaklar (H bölümünden hâlâ açık)
1. "Binde 1 / binde 15" oranının dayanağı (belge) — satış − has bedeli farkından mı çıkıyor? (H-1, 31:22 "listenin burasına çek")
2. 30 bin mi dediniz — 2026 kuyumcu fatura haddi 36.000 ₺; farkında mısınız? (H-2)
3. Faturalog'u hangi akış için kullanıyorsunuz; faturayı hangi entegratör kesiyor; kontör fiyatınız? (H-3) — entegratör seçimini belirler.
4. Hangi bankalarla çalışıyorsunuz? (H-4) — ilk gerçek bağlantı onunla.
5. Bilezik çeşitlerinizin gerçek milyemleri (CNC, el örgüsü, burma, kilitli…) ve ne kadarı işçilik? (H-5)
6. Eski/yeni çeyrek fiyat farkını neye göre koyuyorsunuz? (H-6)
7. Hurda alışta gider pusulasında stopaj yapıyor musunuz? (H-7)
8. Pırlantalı üründe matrahı nasıl hesaplıyorsunuz? (H-8)
9. Ürün başına ayrı fiyat KAYNAĞI (bu ürün Harem, şu ürün Altınkaynak) gerçekten gerekli mi, yoksa sabit fiyat + fiyat farkı yetiyor mu? (1c)
