# Karar Merkezi — otomasyon + "kuyumcuya sor" mimarisi (17.10.2026)

> Dayanak: `dokumanlar/01-gereksinimler/arastirma/MUADIL-ARASTIRMASI-2026-10-17.md` (§ 5 otomasyon haritası,
> § 7.3 onay kuyruğu öğeleri) ve kurucu kararları (17.10, aşağıda § 7).
> Kurucu: "otomatize edebildiğimiz her yeri otomatize edeceğiz… kararı müşteriye bırakmak zorunda olduğumuz
> yerlerde de soracağız… ekranlar sade, kullanıcıyı yormayacak ama eksik bir olay da olmayacak."

## 1. Ana fikir

Programdaki her olay (para geldi, satış kaydedildi, fatura bekliyor, kur değişti) için sistem **ne yapılacağını
hesaplar**, sonra ya **yapar** ya **önerir** ya **sorar**. Üç kademe:

| Kademe | Sistem | Ekranda |
|---|---|---|
| **OTO** | Yapar | Satırda "neden" etiketi + **Geri al** |
| **ÖNERİ** | Hazırlar | Tek dokunuşla onay; eşik altı satırlar için toplu onay |
| **SOR** | Seçenekleri sıralar, **hiçbirini önceden seçmez** | Karar kuyumcunun |

## 2. Katmanlar

1. **Kaynak bağlayıcıları** — `IBankaBaglayici`: ekstre dosyası (var), deneme bağlayıcısı (demo "havale gönder"),
   müşterinin kurumsal banka web servisi (kurucu kararı, § 7-1). Hepsi **tek giriş kapısından**
   (`BankaServisi.HareketEkleIc`) geçer: banka referansı + mükerrer anahtarı ile tekilleşir, MASAK/tevsik kapıları atlanamaz.
2. **Çözücüler** (saf, veritabanına yazmaz, birim test edilir):
   - *Müşteri çözücü* — TCKN/VKN → onaylı IBAN → benzer ad (öneri) → yeni kart (M7a `GelenOdemeServisi.Coz`).
   - *Eşleşme çözücü* — para bir satış/siparişe mi ait; açık TL/gram borç; çoklu borçta en eskiden dağıtım önerisi.
   - *Belge çözücü* — fiş/fatura, e-Fatura/e-Arşiv (mükellef sorgusu), KDV 23/e özel matrah, fatura haddi.
3. **Politika** — `karar_politikalari`: karar türü → kademe; kiracı ayarı, banka hesabı başına aç/kapa, tutar eşiği
   (eşik üstü her zaman insana). **Mevzuat kilidi:** aşağıdakiler ayarla OTO'ya çevrilemez:
   emanet · MASAK şüpheli işlem bildirimi · gram borca TL düşme kuru · has gramı bilinmeyen kalemle özel matrahlı fatura.
4. **Karar kaydı** — tek `kararlar` tablosu: kaynak olay, tür, önerilen eylem + alternatifler, neden etiketi
   ("TC birebir", "IBAN daha önce onaylandı", "kural: Toptancı X"), kademe, durum (bekliyor/yapıldı/geri alındı),
   karar veren + zaman, doğan defter kaydı. Denetim izi kendiliğinden.
5. **Eylem** — Karar Merkezi deftere **kendisi yazmaz**; yalnız mevcut tek kapıları çağırır (`HareketFabrikasi`,
   tahsilat, satış, `FaturaServisi`, emanet). **Geri al = mevcut storno motoru**; silme yok (ADR-0004).
6. **Ekran** — tek **"Bekleyenler"** kutusu: Pano'da sayaç, ilgili ekranda satırlar. Satır: tarih · kim · tutar ·
   **tek önerilen eylem** (alternatifler katlı) · neden etiketi · onay. Üstte "hazır olanların hepsini onayla"
   (eşik üstü hariç), "İşlenenler" sekmesinde **Geri al**, satırda **"Bunu kural yap"** (M4 ürün kuralı kalıbı:
   önizleme + ilk N işlem onaylı).

## 3. "Para geldi" akışı

1. Mükerrer mi (banka referansı / mükerrer anahtarı)? → atla.
2. Müşteri: tek kesin aday (TCKN/VKN, onaylı IBAN) → **OTO** · aday yok → kart aç **OTO** (kurucu 16.10) ·
   çok aday ya da yalnız ad → **SOR** (iki "Mehmet Yılmaz" olabilir).
3. Bekleyen satış/sipariş eşleşiyor mu → satışı tamamlar.
4. Açık **TL** borç → **SOR** ("Borcundan düş / Yeni alışveriş / Avans / Emanet / Satış dışı"); tek borç + tam tutar
   için kuyumcu ayarla "kendiliğinden düş" açabilir (§ 7-2). Çoklu borç → dağıtım önerisi (en eskiden).
5. Açık **gram** borç → **SOR**; sistem kuru önerir, seçmez.
6. **Fatura adımı** (§ 7-3, Faturalog gibi her havaleye): bkz. § 4.
7. MASAK: bağlantılı işlemlerle 370.000 TL'ye yaklaşıyor/aşıyorsa kimlik tespiti uyarısı (bankadan açılan kart
   kimlik tespiti yapılmış SAYILMAZ).

M7a'daki `banka_hareketleri.cari_karari` bu yapının ilk örneğidir; ikinci karar türü gelince ortak `kararlar`
tablosuna taşınır (önceden taşınmaz — gereksiz karmaşa yok).

## 4. Havaleden fatura (kurucu 17.10: "Faturalog gibi her havaleye")

Her gelen ödeme fatura adımına girer; araştırmanın bulduğu iki riske karşı bekçiler:

- **Çift fatura bekçisi:** ödeme (a) zaten faturalanmış bir satışın/siparişin tahsilatıysa ya da (b) "borcundan düş"
  kararıyla faturalanmış bir veresiyeyi kapatıyorsa **fatura kesilmez**, satırda nedeni yazar.
  "Avans", "Emanet", "Satış dışı" kararlarında da kesilmez.
- **Has gram bekçisi:** KDV 23/e özel matrah ürünün has gramını ister; havale ürünü taşımaz. Eşleşen satış yoksa
  fatura adımı **ürün sorar** (SOR) — kuyumcunun kuralı varsa ("Bu IBAN = 22 ayar bilezik, e-Fatura") ürün kuraldan
  gelir (ÖNERİ, ilk N işlem onaylı).
- **Otomatik mi onaylı mı:** kuyumcu ayarı (Faturalog sayfaları çelişkili — araştırma § 4.3; müşteriye sorulacak).
- e-Fatura / e-Arşiv seçimi **OTO** (mükellef sorgusu); sorgu yanıt vermezse belge kesilmez, kuyruğa alınır.

## 5. Paket sırası

| Paket | İçerik | Bağımlılık |
|---|---|---|
| **A0** | MASAK eşiği belgesi (ürün 07.10'dan beri 370.000 uyguluyor; belge düzeltildi 17.10) | — |
| **A1** | Karar Merkezi altyapısı: `kararlar` + `karar_politikalari`, neden etiketi, Geri al, Bekleyenler kutusu (Pano sayacı), M7a'nın taşınması | — |
| **A2** | Para geldi akışının tamamı: satış/sipariş eşleşmesi, çoklu borç dağıtımı, avans/emanet/satış dışı, gram borç + kur önerisi | A1 |
| **A3** | `IBankaBaglayici` + deneme bağlayıcısı + canlı yenileme; ilk gerçek bağlayıcı: müşterinin kurumsal web servisi (§ 7-1 koşuluyla) | A1 |
| **A4** | Fatura adımı (§ 4) + gün sonu toplu e-Arşiv onay listesi; GİB'e gönderim entegratör seçimine bağlı | A2 |
| **A5** | Banka kuralları ("Bunu kural yap"), borç hatırlatma (varsayılan kapalı), mutabakat | A1 |

Sonraki adım (kurucu sırası): mimari onayından sonra **mevcut projede ne eksik ne fazla** — koda karşı satır satır.

## 6. Değişmezler

- Kayıt yalnız mevcut tek kapılardan; Karar Merkezi kendi başına deftere yazmaz.
- Geri al = storno; silme yok.
- Ad benzerliği hiçbir koşulda kendiliğinden bağlamaz (K4-OK-5).
- Mevzuat kilidindeki kararlar hiçbir ayarla otomatikleşmez.
- Her OTO satırında neden etiketi; eşik üstü tutar toplu onaya girmez.

## 7. Kurucu kararları (17.10)

1. **Banka verisi yolu: müşterinin kurumsal web servisi.** ⚠ Araştırma § 6.8: bu yolun lisans gerektirmediğini
   söyleyen kaynak bulunamadı; 6493 md.28 izinsiz faaliyet cezası 1–3 yıl hapis. Mimari bağlayıcıyı soyut tutar;
   **gerçek bankaya canlı bağlantı avukat görüşü yazılı alınmadan açılmaz** (Tamer Bey önerisi — kurucu onayı bekler).
   Teknik ön koşul: müşterinin bankasından alınmış servis sözleşmesi + teknik doküman.
2. **Tek borç + tam tutar:** varsayılan SOR; kuyumcu ayarla "kendiliğinden düş"ü açabilir (Geri al'lı).
3. **Havaleden fatura:** Faturalog gibi her havaleye — § 4 bekçileriyle.
4. **MASAK eşiği:** güncel (370.000 TL, RG 07.10.2026/33393 birincil metin okundu); belge düzeltildi.

## 8. Kurucu kararları (17.10, ikinci tur — eksik/fazla incelemesinden sonra)

1. **Havaleden satış (Faturalog gibi):** hesaba para gelince **listeye düşer**; **tezgâhtar ne sattıysa kendisi seçer** ve
   fatura **doğrudan kesilir** (kesim kipini kullanıcı belirler — Faturalog nasılsa öyle). Aynı anda **arka planda satış
   fişine de dönüşür** (stok, satış hareketi, banka bacağı); kullanıcı fişi görmez, isterse ekrana gidip bakar.
   "Kullanıcıyı yormamak lazım." → Eski "Faturaya dönüştür" yolu (`BankaServisi.FaturayaDonustur`) bu akışla değişir.
   Not: tezgâhtar bu listeyi görür (önceki "yalnız SAHIP/MUDUR/MUHASEBE" kısıtı bu akış için gevşer; satır yalnız
   tarih · gönderen · tutar gösterir).
2. **Borçlu satışlardan seçim:** fatura/karar ekranında müşterinin **borcu olan satışları liste olarak gelir**, oradan seçilir.
   Gelen para seçilen satışları **tam karşılamıyorsa** (kısmi/fazla) nasıl işleneceği **araştırılarak** belirlenecek
   (mahsup/kısmi tahsilat — araştırma işi açıldı).
3. **P0'daki 9 sorun çözülecek** (EKSIK-FAZLA § 1). **Tema:** yalnız Yıldız, açık + koyu mod; diğerleri kaldırılır.
