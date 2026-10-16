# PAKET CUMA-M: Müşteriler, "Vazgeç" düğmesi ve telefon biçimi. Uygulama raporu

**Tarih:** 25.09.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** vt `kuyumcu_test_t1`, API 5160, vekil 5161
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı. Tam süitteki 6 kırmızı test KATMAN-1 şeridine ait (§ 9).

## 0. Devralma (ilk uygulayıcı ENOTFOUND ile kesilmişti)
- `git status` ve `CUMA-M` işaretleriyle yarım iş çıkarıldı. Kod, testler ve 7 mutasyon ilk uygulayıcıda tamamlanmıştı. Raporu yazılmamıştı; son ölçüm matrisinde 429 riski vardı.
- `mut-yedek/CUMA-M/MCUMA-M-1..7.yedek` dosyaları ağaçtaki dosyalarla `cmp` ile karşılaştırıldı. **7/7 AYNI** çıktı, yani ağaçta yarım kalmış mutasyon yok. Karşılaştırılan dosyalar: TelefonBicimi.cs, Form/TelefonBicimli.razor, Cekmece.razor, Pages/Cariler.razor.
- Açık kalmış API süreci yoktu (5160 ve 5161 boştu). Önceki CUMA-M kiracısı da kalmamıştı; tenants sorgusunda `PAKETCUMA-M` yoktu.
- Sıfırdan yazılan kod yok. Bu oturumda derleme, testler, mutasyonlar (yeniden, 8 madde), gerçek tarayıcı matrisi, canlı SQL, temizlik ve bu rapor yapıldı.

## 1. Yapılanlar
1. **Vazgeç düğmesi.** Müşteri düzenleme çekmecesinin (`#cekmece-cari-iptal`) ve Yeni müşteri penceresinin (`#cari-yeni-vazgec`) ikincil düğmesi artık **"Vazgeç"** diyor. Aynı sayfadaki formu kapatan öbür düğmeler de "Vazgeç" oldu: döviz veresiye ve döviz tahsilat çekmeceleri, tahsilat, veresiye, düzeltme fişi ve pasife alma onayı. Metin tek bir kaynaktan geliyor: `OnayKilitSozlugu.Vazgec`. Bu sözlükte TR "Vazgeç", EN "Go back" var. Sözlükler hub'ına dokunulmadı. Mevcut id'ler aynen korundu.
2. **Çekmece bileşeni.** `Bilesenler/Cekmece.razor` bileşenine isteğe bağlı `IptalMetni` parametresi eklendi. Parametre verilmezse düğme eskisi gibi `Dil.T.Ortak.Iptal` gösteriyor, yani öbür 35 çekmece değişmedi.
3. **Telefon biçimi.** Yeni dosya `Servisler/TelefonBicimi.cs` üç iş yapıyor. `Bicim.cs` dosyasına dokunulmadı.
   - `Goster`: liste görünümü. Örnek: "0532 411 22 33". Boş telefon "—" olarak gösteriliyor. Tanınmayan numara aynen gösteriliyor.
   - `Girdi`: kutuda görünen değer.
   - `Sunucuya`: sunucuya giden değer. Tanınan TR numarası "05324112233" olarak gidiyor. Tanınmayan değer kırpılıp aynen gidiyor ve son sözü sunucu söylüyor.
   - Tanınan biçimler: ulusal 10 hane (ilk hane 2, 3, 4, 5 ya da 8), 0 ile başlayan 11 hane, +90 ve 90 önekleri. Boşluk, tire, parantez ve nokta serbest.
4. **Telefon girdi bileşeni.** Yeni bileşen `Bilesenler/Form/TelefonBicimli.razor` eklendi; mevcut `<Telefon>` değişmedi.
   - Yazarken değer yeniden yazılmıyor, böylece imleç atlamıyor.
   - Biçim yalnız alandan çıkınca (change olayında) uygulanıyor.
   - Bu bileşen `cari-telefon` (yeni müşteri) ve `duz-telefon` (düzenle) alanlarında kullanılıyor. PRO satır içi telefon da `Girdi` ve `Sunucuya` ile çalışıyor.
5. **PATCH sözleşmesi.** Kullanıcı telefona dokunmadıysa PATCH gövdesine saklı ham değer aynen konuyor (`duzTelefonKayitli`). İlgisiz bir düzeltme, eski biçimdeki bir kaydı sessizce değiştirmiyor. Kullanıcı telefonu silerse `null` gidiyor ve telefon siliniyor.
6. **Mevcut kayıtlar.** Göç yapılmadı; eski kayıtlar yalnız gösterimde biçimleniyor.

## 2. Önce ölçülenler (spesifikasyon karşısında)
- **Sunucu ne saklıyor?** İlk uygulayıcının `kuyumcu_net` ölçümüne göre 52 telefonun 51'i yalnız rakamdan oluşuyor ve "05324112233" biçiminde. Sunucu yazılanı aynen saklıyor; bu canlıda da doğrulandı (§ 7). Bu yüzden normalize biçim "0" + 10 rakam seçildi.
- **Arama.** `CariArama` önceki bir paketten geliyor ve karşılaştırmayı iki tarafta da rakam çekirdeğiyle yapıyor. Canlıda "0532 411" ve "0532411" ikisi de Hasan Kaya'yı buldu (§ 7). Sunucuda arama kodu değiştirilmedi.

## 3. Sapmalar ve gerekçeleri
| Sapma | Gerekçe |
|---|---|
| `Bilesenler/Cekmece.razor` dosya alanı dışında ama değişti | Çekmecenin ikincil düğme metni yalnız bu bileşenden verilebiliyor. Değişiklik isteğe bağlı bir parametre; verilmezse düğme eskisi gibi. Öbür 35 çekmece değişmedi. 2 mutasyon (M4, M8) davranışı kilitliyor. |
| TM1 testlerinde 4 `InlineData` beklentisi güncellendi | "5324112233" ve "+90…" girdileri eskiden başka bir biçimde gösteriliyordu. Tuval biçimi "0532 411 22 33" olunca beklenti değişti. Kural yeni, eski test yanlış değil. |
| Tuvalde Telefon alanı zorunlu (*), bizde isteğe bağlı | Sunucu sözleşmesi ve Kaydet yolu bu pakette değişmez. Zorunluluk bir veri kuralıdır; görünüm paketinin konusu değil. |
| "+90 532 411" araması sonuç vermiyor | Kabul koşulu yalnız "0532 411" ve "0532411" istiyor; ikisi de sağlandı. "+90" ile kısmi arama `Kuyumcu.Api` tarafında (`CariArama`) ve bu paketin dosya alanı dışında. Öneri: § 10. |

## 4. Derleme ve testler
- `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata.** Son mutasyondan sonra temiz derleme yapıldı.
- Filtreli koşum `CumaM|TM1|Cari|Cekmece`: **417/417 başarılı.** Yalnız `CumaM`: **37/37 başarılı.**
- **Bu pakete ait testler: 37** (`CumaMMusterilerTestleri.cs`):
  - Saf testler (`CumaMTelefonBicimiTestleri`), 01–05. Kabul listesi birebir: 05324112233 → 0532 411 22 33, +905324112233 → 0532 411 22 33, 02121234567 → 0212 123 45 67, boş/null, 7 hane, harfli girdi, yabancı numara.
  - bUnit testleri (`CumaMMusterilerEkranTestleri`), 10–19.
  - HTTP testi (`CumaMHttpTelefonTestleri`), 20: kaydedilen değer normalize biçimde ve arama iki biçimde de buluyor.
- **Tam süit** (trx `trx-pCUMA-M/_Ramazan-MacBook-Air_2026-09-25_09_47_08.trx`): **6003 test; 5997 başarılı, 6 başarısız.** Kırmızıların hepsi `KatmanTekKalipTestleri.K1_*` (§ 9). İlk uygulayıcının 06:55'teki koşumunda 5990 test ve 1 kırmızı vardı (`CJ_02`, CUMA-J şeridi); o test şimdi geçiyor. **Bu paketin ürettiği yeni kırmızı: 0.**

## 5. Mutasyon kanıtı (bu oturumda yeniden koşuldu)
Her mutasyonda aynı döngü uygulandı. Kaynak dosya `mut-yedek/CUMA-M/r2/` altına kopyalandı. `perl` ile tek eşleşmeli bir değişiklik yapıldı. Ardından `dotnet test --filter CumaM|TM1` koşuldu (99 test). Sonra yedek geri kopyalandı ve `cmp` ile ağaçtakinin yedekle birebir aynı olduğu doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen testler | cmp |
|---|---|---|---|
| M1 | `Goster` kimlik fonksiyonu yapıldı (`return tel.Trim()`) | CUMA_M_01, CUMA_M_13, TM1_02, TM1_11, DTM1_telefon_bos_null_ve_tanimsiz_bicim, DTM1_r3_telefon_hane_sinirlari (15 kırmızı) | AYNI |
| M2 | `Sunucuya` kimlik fonksiyonu yapıldı | CUMA_M_03, CUMA_M_15, CUMA_M_18, CUMA_M_19, CUMA_M_20 (HTTP) (7 kırmızı) | AYNI |
| M3 | `Girdi` kimlik fonksiyonu yapıldı | CUMA_M_04, CUMA_M_13, CUMA_M_14, CUMA_M_18 (5 kırmızı) | AYNI |
| M4 | Cekmece `IptalMetni` yok sayıldı | CUMA_M_10, CUMA_M_12 (3 kırmızı) | AYNI |
| M5 | Dokunulmayan telefon koşulu `false` yapıldı (her seferinde normalize ediliyor) | CUMA_M_16, TM1_16 | AYNI |
| M6 | Yeni müşteri penceresinde `@Vazgec` yerine `Ortak.Iptal` kondu | CUMA_M_11 | AYNI |
| M7 | Yazarken `Girdi(v)` ile yeniden yazılıyor (imleç atlar) | CUMA_M_14 | AYNI |
| M8 | Cariler çekmecesinden `IptalMetni="@Vazgec"` kaldırıldı | CUMA_M_10, CUMA_M_12 | AYNI |

Hiçbir mutasyon testlerden yeşil geçmedi; test açığı yok. İlk uygulayıcının 7 mutasyon günlüğü (`MCUMA-M-1..7.log`) aynı sonucu veriyor.

## 6. Denetim
`dotnet/denetim.sh` sonucu: **DENETİM TEMİZ** (rc=0, 288 ✓).
- Pages altındaki ModalPencere kullanımı 48 (tavan 55); yeni düzenleme ModalPenceresi yok.
- Satır içi style sayısı 105 (tavan 143).
- Sabit renk 0.

## 7. Canlı doğrulama (API 5160, kiracı `PAKETCUMA-M-TEST canli`)
**Kurulum:** Sistem anahtarı rastgele üretildi ve chmod 600 bir dosyadan okundu; hiçbir çıktıya yazılmadı. Davet `POST /sistem/davetler` 201 döndü. Kayıt `POST /auth/kayit` 201 döndü. Giriş 200 döndü.

**HTTP:**

| İstek | Kod | Yanıt (okundu) |
|---|---|---|
| POST /cari Hasan Kaya, telefon 05324112233 | 201 | telefon "05324112233" |
| POST /cari Ayse Yilmaz / Sabit Hat / Yabanci / Telefonsuz / Bosluklu | 201 ×5 | "05306405521", "02125114040", "+44 20 7946 0958", null, "0541 208 90 17" |
| GET /cari?q=0532 411 | 200 | [Hasan Kaya] |
| GET /cari?q=0532411 | 200 | [Hasan Kaya] |
| GET /cari?q=0541208 (eski boşluklu kayıt) | 200 | [Bosluklu Eski Kayit] |
| GET /cari?q=+90 532 411 | 200 | [] (bkz. § 3) |

**Ekrandan kaydetme ve SQL kanıtı:** Düzenle çekmecesinde "05479876543" yazıldı ve Kaydet'e basıldı. Başarı bandında "Kaydedildi." görüldü; listede "0547 987 65 43" gösterildi. Veritabanı sorgusu:
```
SELECT ad, telefon FROM cariler WHERE tenant = test AND tur = 'MUSTERI'
Ayse Yilmaz 05306405521 · Bosluklu Eski Kayit 0541 208 90 17 · Hasan Kaya 05479876543
Sabit Hat Musteri 02125114040 · Telefonsuz Musteri NULL · Yabanci Musteri +44 20 7946 0958
```
Ekrandan giden değer normalize biçimde ("05479876543") saklandı. Eski boşluklu kayda dokunulmadı; göç yok.

### 7.1 İmleç davranışı (gerçek Chrome, CDP ile gerçek tuş olayları, `#duz-telefon`)
| Adım | Değer | İmleç |
|---|---|---|
| Hepsini sil | "" | 0 |
| 05419876543 yazıldı | "05419876543" (yazarken biçim yok) | 11 |
| Ortadan sil (4. konum, Backspace) | "0549876543" | 3 (yerinde) |
| Araya "7" yazıldı | "05479876543" | 4 (yerinde) |
| Alandan çıkıldı | "0547 987 65 43" | 14 |
| "+90 532 411 22 33" yapıştırıldı | "+90 532 411 22 33" | 17 |
| Alandan çıkıldı | "0532 411 22 33" | 14 |
| Kaydet sonrası liste | "Kaydedildi." / "0547 987 65 43" | none |

### 7.2 Gerçek tarayıcı ölçüm tablosu (sikisma-olc.js, `__olcTam`)
Ölçüm iki yoldan yapıldı. Birincisi başsız gerçek Chrome (`cm.mjs`, `cm-matris-r2.log`). İkincisi tarayıcı bölmesinde kendi sekmem (tab-85): 1440 PRO liste, PRO çekmece, BASİT liste ve BASİT çekmece aynı sayıları verdi. Bulunan telefon hücreleri: "0530 640 55 21", "0541 208 90 17", "0532 411 22 33", "0212 511 40 40", "—", "+44 20 7946 0958".

| Ekran | Genişlik | Mod | Tema | K1 | K2 % | K3 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| liste | 1440 | PRO | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| liste | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| çekmece | 1440 | BASİT | açık | 0 | 61* | 0/0 | 0 | 0 | 0 | 1† | 0 | 0 | 0 | 0 |
| yeni müşteri | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 1† | 6‡ | 0 | 0 | 0 |
| çekmece + imleç | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| çekmece | 1280 | BASİT | açık | 0 | 55* | 0/0 | 0 | 0 | 0 | 1† | 0 | 0 | 0 | 0 |
| çekmece | 1280 | BASİT | koyu | 0 | 55* | 0/0 | 0 | 0 | 0 | 1† | 0 | 0 | 0 | 0 |
| çekmece | 1280 | PRO | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 6‡ | 0 | 0 | 0 |
| çekmece | 1280 | PRO | koyu | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 6‡ | 0 | 0 | 0 |
| çekmece | 375 | BASİT | açık | 0 | – | 0/0 | 0 | 0 | 0 | 1† | 0 | 0 | 0 | 0 |
| çekmece | 375 | BASİT | koyu | 0 | – | 0/0 | 0 | 0 | 0 | 1† | 0 | 0 | 0 | 0 |
| çekmece | 375 | PRO | açık | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 6‡ | 0 | 0 | 0 |
| çekmece | 375 | PRO | koyu | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 6‡ | 0 | 0 | 0 |
| liste | 375 | BASİT | açık | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

İlk koşuda 1440 PRO liste ölçümü 100 istek/dakika sınırı yüzünden 429 aldı; o koşu geçersiz sayılıp atıldı. Matris koşular arasına 55 sn bekleme konarak yeniden koşuldu ve tabloda http4xx = [] olan koşular var.

**Gerekçeli sayaçlar (ölçülerek gösterildi, bu paketin ürünü değil):**
- `*` **K2 = %55–61.** Çekmece açıkken sağ taraftaki blok çekmece; liste perde altında kalıyor. Aynı ekranın çekmece kapalı hali %97–98. Tuvalde de çekmece listeyi örtüyor.
- `†` **K8 = 1.** Betiğin kebab-case kuralı "e-posta" sözcüğünü yakalıyor. Metin tuvalle birebir aynı: "Daha fazla (adres, TC, e-posta, limit)". Jargon değil, Türkçe bir sözcük.
- `‡` **K9 = 6.** Bunlar çekmece ya da yeni müşteri penceresi altında kalan arka plan metinleri. Tarayıcı bölmesinde kendi sekmemde iki ölçüm yapıldı:
  - Arkadaki 76 metin öğesinin **0'ı** `elementFromPoint` ile üstte değil; hepsi perdeyle örtülü.
  - Çekmece içinde kalan çakışmalar kaydırma alanının dışında. Örnek: "Satış fişinde iskonto…" metninin üst kenarı 855 px, `#cekmece-cari-govde` alt kenarı 843 px, görünür yükseklik 0. Yani bu içerik kaydırma alanında kırpılıyor ve kullanıcı görmüyor.
  - BASİT modda aynı çekmecede K9 = 0.

### 7.3 Tuval karşılaştırması
- Görsel: `ham/TUVAL-KARSILASTIRMA/CUMA-M-musteriler-1440.png` (solda tuval, sağda bizde; 1440×900, BASİT, açık tema, çekmece açık, 25.09 R2).
- Ek görüntüler:
  - `CUMA-M-musteriler-matris-R2.png` (375 BASİT açık · 375 PRO koyu · 1280 PRO koyu · 1280 BASİT koyu)
  - `CUMA-M-R2-1440-basit-acik-yeni.png`
  - `CUMA-M-R2-1440-pro-acik-liste.png`
  - `CUMA-M-R2-1440-basit-acik-imlec.png`

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Çekmece ikincil düğme | "Vazgeç" | "Vazgeç" | AYNI |
| Çekmece Telefon kutusu | "0532 411 22 33" | "0532 411 22 33" | AYNI |
| Liste TELEFON sütunu | "0532 411 22 33", "0212 511 40 40" | "0532 411 22 33", "0212 511 40 40", yabancı numara aynen | AYNI |
| Çekmece başlığı, ad soyad, not, "Daha fazla (adres, TC, e-posta, limit)", Kaydet | var | var | AYNI |
| Arama kutusu ve Hepsi/Borçlu/Toptancı | var | var | AYNI |
| Telefon etiketinde "*" | zorunlu | isteğe bağlı | GEREKÇELİ FARK (§ 3, sunucu sözleşmesi) |
| "Vadesi 12 gün geçti" bandı | var | yok | GEREKÇELİ FARK: bu tuvaldeki örnek veri. Test kiracısında vadesi geçmiş borç yok. |
| Soldaki menüde ikonlar, üst barda saat | yok | var | GEREKÇELİ FARK: başka şeritlerin ürünü (menü ve üst bar); bu pakette dokunulmadı. |

## 8. DoD sözleşmesi
Yeni bir varlık ya da kayıt türü yok, migration yok. Cari için PATCH, pasife alma ve aktife alma uçları önceki paketlerde vardı ve ekranda duruyor. Telefon PATCH sözleşmesi:
- Gövdede telefon yoksa değer korunuyor.
- Telefon `null` ise siliniyor (CUMA_M_17).
- Kullanıcı telefona dokunmadıysa saklı değer aynen gidiyor (CUMA_M_16).

Bu pakette yeni düğme eklenmedi; yalnız mevcut düğmelerin metni değişti ve bu düğmeler zaten istek atıyor ya da formu kapatıyor. Denetim kaydı yolu değişmedi.

**Dokunulan sayfalar:** `Pages/Cariler.razor`. Paylaşılan bileşenler: `Bilesenler/Cekmece.razor` (geriye uyumlu parametre) ve yeni `Bilesenler/Form/TelefonBicimli.razor`.

## 9. Test kiracısı ve temizlik
- `kiraci-temizle.sh 01M3BMPPJPQF5S4Z6SJMD8QJQP --uygula` çalıştı. Yeniden sorgu "Kiracı YOK" dedi.
- `information_schema`'da tenant_id kolonu olan 70 tablonun toplamı **0**.
- `kiraci_davetleri` tablosundan 2 davet satırı silindi. Biri kullanıldı; öbürü jeton alanı yanlış okunduğu için kullanılmadan kaldı. Sonuç: 2 → 0.
- **tenants:** bu işe başlarken 15 kayıt vardı, şimdi 14. Benim kiracım +1 eklendi, −1 silindi. Aradaki −1'i L-1b şeridi kendi `PAKETL-1b-TEST` kiracısını silerek yaptı.
- Kalan PAKET kiracıları başka şeritlerin: Q10-4, T-S2, CUMA-J, KATMAN-1. QA-TEST kiracılarına dokunulmadı.
- Kapatılan süreçler: API 5160, vekil 5161, yardımcı 8791 ve başsız Chrome 9481. `lsof` ile üçü de boş görüldü. Tarayıcı sekmem (tab-85) kapatıldı.

## 10. Açık kalanlar
1. **Tam süitte 6 kırmızı var, hepsi KATMAN-1 şeridine ait.** Kırmızı testler: `KatmanTekKalipTestleri.K1_01`, `K1_07`, `K1_08`, `K1_09`, `K1_10`, `K1_11`. Bu şeridin dosyaları (`katman.css`, `katman.js`, `KatmanTekKalipTestleri.cs`) 09:41–09:44 arasında değişiyordu. Bu paket o dosyalara dokunmadı.
2. **UX-S31 sayımı: 35 çekmece hâlâ "İptal" diyor.** `<Cekmece>` toplamı 38; `IptalMetni` verilen 3'ü Cariler'de. Ayrıca `Dil.T.Ortak.Iptal` 48 yerde kullanılıyor. Öneri, her ekranın sahibi şeride:
   - "Formu kapat" anlamındaki düğmelere `IptalMetni="@OnayKilitSozlugu.Al(Dil.Dil).Vazgec"` verilsin.
   - "Kaydı iptal et" anlamındaki düğmeler (fatura iptali, sayım iptali gibi) "İptal" olarak kalsın.
   - Çekmecesi olan ekranlar: Banka (4), Kasalar (5), Emanet (3), Evraklar (3), UrunAgaci (3), Isletme (2), Pos (2), Uyum (2), Ayarlar, Faturalar, Gider, GiderPusulalari, Hurda, KasaRaporu, Kur, Urunler, Vezne, Vitrin, Fis/FisDigerRezerv.
   - Bu pakette değiştirilmedi.
3. **Aramada "+90" ile kısmi numara bulunmuyor.** Örnek: "+90 532 411" boş dönüyor. `Kuyumcu.Api/Modules/Cari/CariArama.cs` baştaki 90'ı yalnız tam numarada atabiliyor. Ayrı bir küçük backend paketi önerilir.

## 11. Değişen dosyalar (bu paket)
```
 M dotnet/Kuyumcu.Web/Pages/Cariler.razor
?? dotnet/Kuyumcu.Web/Servisler/TelefonBicimi.cs                 (yeni)
?? dotnet/Kuyumcu.Web/Bilesenler/Form/TelefonBicimli.razor       (yeni)
?? dotnet/Kuyumcu.Web/Bilesenler/Cekmece.razor                   (IptalMetni parametresi; dosya önceden izlenmiyordu)
?? dotnet/Kuyumcu.Tests/CumaMMusterilerTestleri.cs               (yeni, 37 test)
?? dotnet/Kuyumcu.Tests/TM1MusterilerTuvalTestleri.cs            (2 InlineData beklentisi)
?? dotnet/Kuyumcu.Tests/Dogrulayici/TM1SinirTestleri.cs          (2 InlineData beklentisi)
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/CUMA-M-*.png
```
Değişmeyenler: `tuval-panel-musteri.css` (son değişikliği 24.09 22:57'de, bu paketten önce), Sözlükler hub'ı, `Bicim.cs`, `app.css`, `Form/Telefon.razor`.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Çekmecede "Vazgeç" (TR) ve EN karşılığı sözlükten | KARŞILANDI | OnayKilitSozlugu (TR "Vazgeç", EN "Go back"); CUMA_M_10, 11, 12; canlıda `cekmece-cari-iptal` "Vazgeç" |
| Yalnız formu kapatan düğme değişti, öbür sayfalar değişmedi, sayım rapora yazıldı | KARŞILANDI | § 10.2 |
| Telefon gösterim ve girdi biçimi "0532 411 22 33" | KARŞILANDI | CUMA_M_01, 04, 13; canlı liste ve çekmece |
| Sunucuya giden değer mevcut sözleşmeyle aynı (önce ölçüldü) | KARŞILANDI | § 2; CUMA_M_03, 15, 18, 19, 20; SQL "05479876543" |
| +90 / boşluklu / bitişik yazım, yapıştırma çalışıyor, imleç atlamıyor | KARŞILANDI | § 7.1; CUMA_M_14 |
| Sabit hat ve yabancı numara bozulmuyor | KARŞILANDI | "0212 511 40 40", "+44 20 7946 0958" aynen |
| Mevcut kayıtlar için göç yok | KARŞILANDI | "0541 208 90 17" kaydı veritabanında aynen duruyor |
| Arama "0532 411" ve "0532411" ikisini de buluyor | KARŞILANDI | canlı GET ve CUMA_M_20 |
| Tuvalle yan yana görüntü ve bölge tablosu | KARŞILANDI | § 7.3 |
| 1280 / 375, BASİT + PRO, açık + koyu, K1..K11 | KARŞILANDI (gerekçeli sayaçlarla) | § 7.2; K8 ve K9 ölçülerek açıklandı |
| Mutasyon: biçim yardımcısı kimlik fonksiyonuna çevrilince testler kırmızı | KARŞILANDI | M1, M2, M3 |
| Bu paket yeni kırmızı test üretmedi | KARŞILANDI | Yeni kırmızı 0; 6 kırmızı KATMAN-1 şeridinin |
| Test kiracısı silindi | KARŞILANDI | 70 tabloda 0 satır; davet satırları 0 |

## 13. Kurucuya / mali müşavire notlar
Mevzuat konusu yok. Ürün notu: tuvalde Telefon alanı zorunlu gösteriliyor. Bunun iş kuralına dönüşmesi, yani telefonsuz müşteri açılamaması, kurucu kararı gerektirir. Bugün peşin satışta telefonsuz müşteri açılabiliyor.
