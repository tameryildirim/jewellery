# PLAN 2 — Satılabilirlik Planı
**Tarih:** 2026-09-13 · **Mühür:** Tamer Bey · **Dayanak:** 4 kollu pazar araştırması (`PAZAR-ARASTIRMASI-2026-09-13.md`) + 4 kişilik ekip oylaması
**Önceki plan:** `PLAN-YASAL-ZORUNLULUKLAR-2026-09-11.md` — 9 adımın 8'i + Adım A bitti; yalnız Adım 5 (e-Fatura) K1'e kilitli. **O plan kapanmadı, devrediliyor.**
**Commit YASAK** (Kurucu kuralı, yürürlükte).

---

## 0. NEDEN YENİ PLAN — eski planın hükmü değişti

Plan 1 "yasal zorunluluk" ekseniydi ve o eksen **tamamlandı**. Ama bugünkü araştırma iki şeyi değiştirdi:

**(a) Farklılaşma iddiamız çürüdü.** "MASAK uyumu pazarda yok" savımız **yanlıştı**: 6 firma açıkça satıyor. Terminal Gold birebir **"Kimlik girilmeden işlem kapatılamaz"** yazıyor — bizim "kimse yapmıyor" dediğimiz şey. **MASAK artık farklılaşma değil, giriş bileti.**
> ⚠ Bunlar **yayımlanmış pazarlama metnidir**. Hiçbir rakip ürünün içi görülmedi (hesap açmak yasak). "Yapıyor" değil "iddia ediyor". Ama "kimse iddia etmiyor" savımız kesin düştü.

**(b) Kanıtlanmış bir iç boşluk ölçüldü.** Blazor API'ye 25 yol çağırıyor; MASAK'ın 12 ucundan **yalnız `/masak/esikler`**. Ekranı olmayan **13 uç** var. Kuyumcu gider pusulasını **yazdıramıyor**, şüpheli işlemi ekrandan bildiremiyor. **Arkada çalışan ama ekranı olmayan uç, üründe yok demektir.**

---

## 1. OYLAMA — 4 oy, 4 gerekçe

| Üye | Oy | Gerekçe (özet) |
|---|---|---|
| **Bertan** (Mimari) | **A** — 13 ucun UI'ı | "B, C, D'nin hepsi bu UI iskeletinin üstüne oturur; onu önce kurmak sonrakilerin maliyetini düşürür." |
| **Eray** (Satış) | **A** | "Tek kanıtlanmış farklılaştırıcımız bugün satılamaz — müşteri onu ekranda göremiyor. Backend'i satış toplantısında gösteremem." |
| **Mehmet** (Güvenlik) | **C** — 30.000 TL tevsik | "A yalnız var olan veriyi ekrana taşır, yeni risk yaratır yeni koruma getirmez. C yazma yolunda çalışır ve kullanıcıyı cezadan korur." + **2 koşullu veto** |
| **Aşkın** (QA) | **F** — test altyapısı önce | "Ekransız 13 ucu kanıtsız UI ile kapatmak, en büyük kör noktayı 13 kat büyütmektir." + **F ≤ 1 adım, sonra A** |

### Tamer Bey mührü — uzlaştırma
Oylar çelişmiyor, **sıralamada ayrışıyor**. Aşkın F'yi kendisi "1 adımlık ön koşul, sonra A" diye tanımladı. Mehmet'in koşulları da A'dan **önce** gelmesi gereken maddeler. Bu yüzden:

> **A kazandı (2 oy), ama önüne Adım 0 konuyor: Mehmet'in ön koşulları + Aşkın'ın test kapısı.**
> Mehmet'in C'si Adım 2'ye alındı — reddedilmedi, sıralandı.

---

## 2. İDDİA DENETİMİ — oy gerekçeleri ölçüldü

Oy verenin iddiası ölçülmeden plana girmez (Anayasa md.1). Sonuçlar:

| İddia | Sahibi | Ölçüm |
|---|---|---|
| "`Kilavuz.razor` 9 satırlık boş kabuk, kullanıcıya boş sayfa gösteriyor" | Bertan | ❌ **YANLIŞ** — iframe sarmalayıcı; `wwwroot/kilavuz.html` **41.620 bayt**, gerçek içerik var |
| "PDF/belge üretim kütüphanesi yok" | Bertan | ✅ **DOĞRU** — QuestPDF/iText/PdfSharp/DinkToPdf: 0 |
| "MASAK kuralı istemcide ikizleniyor" | Bertan | ✅ **DOĞRU** — `MasakFormu.Eksiksiz()` (satır 130-139) kural şeklini yeniden yazıyor. Eşikler sunucudan geliyor, sunucu kapıyı tutuyor; ama kural değişirse **istemci sessizce sapar** |
| "`CariModulu` okuma izi düşmüyor" | Mehmet | ✅ **DOĞRU** — `Denetim` geçen satır **0**; cari Telefon/VergiNo/Adres tutuyor |
| "Denetim izini okuyan uç yok" | Mehmet | ✅ **DOĞRU** |
| "tevsik / 30.000 üretim kodunda yok" | Mehmet | ✅ **DOĞRU** — 0 isabet |
| "Denetim kaydının değiştirilemezliği ölçülmedi" | Mehmet | ⚠️ **ÖLÇÜLDÜ: KORUMA YOK.** `DenetimYazmaDinleyicisi` yalnız kendini denetlemekten kaçınıyor; `DenetimKaydi`'nı UPDATE/DELETE etmeyi engelleyen **hiçbir kısıt yok**. → **Değiştirilebilen denetim izi, denetim izi değildir.** Adım 0'a eklendi |
| "`Mvc.Testing` yok, `WebApplicationFactory` 0 eşleşme" | Aşkın | ✅ **DOĞRU** — HTTP boru hattı hiç test edilmiyor |
| "30 razor, 4.836 satır, **0 adet `.razor.cs`**" | Aşkın | ✅ **DOĞRU** |
| "MASAK sınır-değer testleri var mı?" (açık soru) | Aşkın | ✅ **VAR** — `MasakKuralTestleri.cs`'de 32 satır: 184.999,99 / 185.000 / 199.999,99 / 2.000.000 / 2.000.000,01 / 20.000.000 / 20.000.000,01, **negatif tutarlar dahil**. Soru kapandı |

---

## 3. SIRALI PLAN

Kural (Plan 1'den devralındı): **bir adım kabul ölçütü kanıtlanmadan sonrakine geçilmez.**

---

### ADIM 0 — Kapıyı kur `[QA + Güvenlik ön koşulu]`

Aşkın'ın F'si + Mehmet'in A-öncesi koşulları. **Azami 1 adım, şişirilmeyecek.**

**0a — HTTP boru hattı testi.** `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory`. Bugün middleware sırası, auth, oran sınırı, CORS, JSON sözleşmesi **hiç test edilmiyor**; `denetim.sh` bunların *varlığını* grep'liyor, *çalıştığını* değil. **Geçmişteki 1. ve 2. hata tam buradan geçti.**

**0b — bUnit iskeleti + code-behind ayrımı.** Yalnız test edilecek bileşenler için (Aşkın'ın tahmini: 30 razor'un ~12'si — para/has gösteren, yasal kayıt üreten, yetki gizleyen).

**0c — `CariModulu`'na okuma izi.** *(Mehmet VETO-2)* MASAK ekranında iz tutup cari kartında tutmamak denetimde savunulamaz.

**0d — Denetim izi okuma ucu + ekranı.** SAHIP rolüne özel, filtrelenebilir, **silme ucu yok**. Bugün kuyumcu bir KVKK talebine cevap veremiyor.

**0e — Denetim kaydının değiştirilemezliği.** Interceptor'da `DenetimKaydi` için `Modified`/`Deleted` **reddedilecek**; mutasyon testiyle kanıtlanacak.

**Kabul ölçütü:** `dotnet test` 0 başarısız + `denetim.sh` TEMİZ (çıktı yapıştırılmış) · en az 1 HTTP testi gerçek **429** ve gerçek **401/403** alıyor · 0e için mutasyon kanıtı (korumayı kaldır → kırmızı) · cari okuma izi canlıda görülüyor.

---

### ADIM 1 — Ekransız 13 ucun UI'ı `[A — oylamayı kazanan]`

Bertan'ın somut yerleşimi: **2 yeni sayfa + 1 yazdırma yolu + 4 gömme.** Menü 10 → 12.

| Yer | İçerik |
|---|---|
| **`Pages/Uyum.razor`** (yeni) | Sekmeler: Kimlik Tespitleri (3 uç) · Şüpheli İşlemler (2 uç) · Saklama (2 uç). **Detay ayrı route DEĞİL, modal** — URL'e kişisel veri kimliği yazılmaz, geri tuşuyla kaza yeniden-okuma olmaz |
| **`Pages/GiderPusulalari.razor`** (yeni) | Liste + detay modal (4 uç) |
| **`/gider-pusulalari/{Id}/belge`** (yazdırma yolu) | Sade yerleşim + `@media print` + `window.print()`. **Sunucuda PDF üreticisi yok ve eklenmiyor** — belge ucu veri döndürür, render istemcide. VUK belgesi PDF bağımlılığı olmadan basılır |
| `Panel.razor` | `masak/ozet` kartı + 7 günlük süre uyarı bandı |
| `Raporlar.razor` | `pos-fatura-mutabakati` sekmesi |
| `MasakFormu.razor` | **`masak/gereksinim` ucuna bağlanacak** — kural ikizlenmesi giderilir, tek doğruluk kaynağı sunucu |

**Mehmet'in 5 şartı (VETO kapsamında):** UI rol matrisini tekrar eder ama **API kararı bağlayıcıdır** · TEZGAHTAR bu ekranları menüde görmez · listede TCKN **maskeli**, toplu tam TCKN gösteren ekran **yasak** · **prefetch yasak** (sayfa açılışında 50 detay çekmek okuma izini gürültüye boğar) · her yazdır/indir düğmesi ize yazar · **idle kilit** (tezgâhta açık kalan MASAK listesi gerçek risk).

**Bertan'ın mimari şartı:** `Servisler/` altına tiplenmiş `MasakServisi` / `GiderPusulasiServisi` (istemci tarafı, DTO'lu). `JsonElement` ile ekran yazmak test edilemez. Sayfa-içi doğrudan `ApiIstemci` çağrısı deseni **büyütülmeyecek** (`Islemler.razor` 733, `Banka.razor` 540 satır — kopyala-yapıştır üretiyor).

**Kabul ölçütü:** Her ekran bUnit smoke + rol gizleme testi · gider pusulası **canlıda yazdırma önizlemesi** ile kanıtlanır · maskelemenin çalıştığı canlı çıktıyla gösterilir · 13 ucun tamamı ekrandan erişilebilir.

---

### ADIM 2 — 30.000 TL tevsik kümülasyonu `[C — Mehmet'in oyu]`

**Dayanak:** VUK md.459, **575 s. Tebliğ, RG 30.11.2024/32738** — *"'7.000' ibareleri '30.000' şeklinde değiştirilmiştir."* Cezası **iki tarafa birden** kesiliyor.

Aynı gün / aynı kişi **kümülatif** nakit kontrolü. Mevcut `MasakKuralMotoru`'nun içine ek kural — küçük iş, büyük uyum etkisi.

**Mehmet'in şartı:** MASAK md.5/2 ile aynı **fail-closed** kapıdan geçecek — eşik aşıldığında işlem **kapanmadan önce** uyarı. **Sessiz loglama kabul değil.**
**Aşkın'ın şartı:** sınır-değer üçlüsü — 29.999 / 30.000 / 30.001 — ve **aynı gün** tanımının Europe/Istanbul gün sınırıyla test edilmesi.

**Kabul ölçütü:** Kümülatif eşiği aşan ikinci işlem canlıda **durur**; tek işlemle aşan da durur; gün sınırı testi yeşil; mutasyon kanıtı.

---

### ADIM 3 — Sentetik taş etiketleme `[B — yeni yasal zorunluluk]`

**Dayanak (birincil, Resmî Gazete tam metin):** **24 Haziran 2026 / RG 33290** — Kuyum Ticareti Yönetmeliği md.11'e (e) bendi:
> *"…'sentetik', 'laboratuvar üretimi', 'yapay üretim' … ibarelerinden en az birine **etiket, ürün sertifikası, fatura, internet sayfası, reklam ve tanıtım materyallerinde** … açıkça yer vermeksizin satışa sunamaz. Bu ürünler ile doğal kıymetli taşları … **ayrı bölümlerde**, internet ortamında **ayrı kategorilerde** satışa sunar."*

**Yapılacak:** Ürün kartına zorunlu taş kökeni alanı (doğal / sentetik / lab üretimi / yapay) · fatura satırına ibare yansıması · listede ayrı kategori · etiket metni.

⚠ **Bertan'ın uyarısı kabul edildi:** "hiçbir rakipte yok" ifadesi **kanıt değil** — 19 üründen yalnız 6'sı bilgi yayımlıyor, **"bulunamadı" ile "yok" aynı şey değil.** Bu, satış vaadi olarak **kullanılmayacak**; yasal zorunluluk olduğu için yapılıyor.

**Kabul ölçütü:** Kökeni işaretlenmemiş sentetik taşlı ürün **satışa çıkarılamaz** (fail-closed, test) · faturada ibare canlı çıktıyla gösterilir.

---

### ADIM 4 — Veri dışa aktarımı `[D — kanıtlı müşteri acısı]`

**Neden:** Şikayetvar'da bulunan iki kayıt (M&L "ömür boyu lisans" → 5 yıl sonra ~600 USD; Bilsoft dönem devri ücreti). Ortak dert **MASAK değil — verinin programda rehin kalması.** Bu, rakiplerin en zayıf noktası.

**Bertan'ın mimarisi:** Her modüle ayrı export ucu **YOK**. Tek `Disaaktarim` modülü + `IDisaAktarilabilir` sözleşmesi; modül `IQueryable` verir, ortak yazıcı CSV/XLSX üretir. Yoksa 18 modül × export = 18 kopya. 8 yıllık veri belleğe alınamaz → `IAsyncEnumerable` + streaming (`text/csv` chunked). **İlk turda 6 varlık:** cari, ürün, satış, fatura, kasa hareketi, gider pusulası.

**Mehmet'in 6 şartı (VETO-1 — hepsi karşılanmadan sevk edilemez):**
1. Yalnız **SAHIP**; tek tıkla değil, **gerekçe metni zorunlu**
2. Varsayılan **maskeli**; tam TCKN'li mod **ayrı onay ekranı**
3. Her aktarım denetim izine: kim, ne zaman, kaç satır, hangi alanlar, hangi gerekçe
4. **Oran sınırlı** (mevcut `OranSiniri.cs`)
5. Dosya **şifreli**; parola dosyayla aynı kanaldan verilmez
6. İndirme bağlantısı **kısa ömürlü, tek kullanımlık**

**Mehmet'in tespiti:** *"Diskteki AES-256-GCM koruması, masaüstüne düşen düz metin CSV karşısında hükümsüzdür."*
**Ayrıca:** "Tam SQL yedek" mimari değil **operasyon** işidir (Batuhan). Uygulama içinden ham dump **verilmeyecek**.
**Bertan'ın uyarısı:** Export tek yerden çalıştığı için kiracı filtresini **bir kez** yanlış yaparsanız tüm müşterilerin verisi tek dosyada dışarı çıkar → **mutasyon testi olmadan sevk edilmez.**

---

### ADIM 5 — Barkod / etiket basımı `[E'nin tek kalemi]`

Bertan: "ürün tamlığı kaleminin içinde en çok istenen ve en ucuz olanı." **RFID, toptan, imalat, personel/prim bu turda HAYIR.**

---

### 🔒 KİLİTLİ — ADIM 5 (Plan 1): e-Fatura
K1 (özel entegratör sözleşmesi) gelmeden başlamaz. Kabul ölçütü değişmedi: **entegratörün test ortamına gerçek fatura**; mock ile "oldu" denmez.

---

## 4. KALİTE KAPISI — Aşkın'ın 8 şartı (her adımda geçerli)

| # | Şart |
|---|---|
| K1 | `dotnet test` 0 başarısız + `denetim.sh` TEMİZ — **çıktı yapıştırılmış**, özet değil |
| K2 | Yeni her API ucu HTTP entegrasyon testiyle gelir: yetkisiz=401, yanlış kiracı=403/boş, geçerli=200, geçersiz gövde=400 |
| K3 | Her yeni uç için "B kiracısı A'nın kaydını göremez/yazamaz" testi — **istisnasız** |
| K4 | Para/has dokunan her adımda **özdeşlik testi**: bakiye ↔ defter toplamı, has borç ↔ has alacak, belge net ↔ kasa hareketi — **işaret dahil** |
| K5 | Yeni her kural için **mutasyon kanıtı**: kuralı boz → en az 1 test kırmızı, **isimleriyle** |
| K6 | Yeni her ekran için bUnit smoke; hesap gösteriyorsa biçimleme/yuvarlama testi |
| K7 | Adım kapanışında **canlı sunucuda elle doğrulama çıktısı** |
| K8 | Mehmet'in yayın vetosu duruyor; QA kapısından geçmek **yayın izni değildir** |

**Aşkın'ın geçirmeyecekleri (seçme):** "testler yeşil" tek kanıt olarak sunulan hiçbir adım (*"dört hata da yeşilden geçti; yeşil artık bu projede kanıt sayılmıyor, yalnız ön koşul"*) · canlıda bir kez bile koşturulmamış hiçbir özellik · dolu veritabanına göç denenmeden hiçbir yayın · `bin/obj` commit'li repo düzeltilmeden hiçbir commit.

---

## 5. MİMARİ BORÇ — Bertan'ın listesi (plana bilgi notu)

1. **Sayfa şişmesi / servis katmanı yokluğu** → Adım 1'de kısmen kapanıyor (yeni ekranlar servis üzerinden)
2. **Tek DbContext + 21 tablo bugün sorun DEĞİL** — Bertan: *"acele bölme erken optimizasyon olur"*
3. **Kural ikizlenmesi** → Adım 1'de kapanıyor (`gereksinim` ucuna bağlanma)
4. **Belge render standardı yok** → Adım 1'de `BelgeYerlesimi` + print CSS ile kuruluyor (dördüncü belgeyi yazmadan)
5. **7,53 MB Debug rakamı karar için kullanılamaz** — Bertan haklı, metodolojik hata. **Release + IL trimming + Brotli ölçülmeden "WASM ağır" denmeyecek.** Ölçüm ayrı iş olarak backlog'a

---

## 6. KONUMLANDIRMA — Eray'ın önerisi (Kurucu onayına tabi)

**"MASAK uyumluyuz" artık savunulamaz** (Terminal Gold aynısını, daha iddialı söylüyor). Eray'ın önerdiği yeni cümle:

> *"Kuyumcunun mevzuat yükünü ekrana taşıyan program: has hesabı ve özel matrahlı KDV'nin üstüne MASAK şüpheli işlem bildirimini 10 iş günü sayacıyla, 8 yıl muhafazayı ve gider pusulası + stopajı üründe taşıyoruz."*

**Eray'ın kendi şartı:** *"Bu cümlenin son yarısı Adım 1 bitmeden reklama girmez — ekranda gösteremediğimiz şeyi satmam."*

### Fiyat — Eray'ın önerisi (⚠ **birim maliyetimiz ÖLÇÜLMEDİ**; marj onayı Mustafa/FinOps'a ait)
**1.250 TL/ay/şube · yıllık peşin 15.000 TL · kurulum, güncelleme ve veri ihracı dahil.**

3 yıllık toplam sahip olma maliyeti (45.000 TL), KDV hariç:

| Rakip | 3 yıl | Bizim | Tasarruf |
|---|---|---|---|
| Kuyumcu Programı (69.000 tek sefer) | 69.000 | 45.000 | **%34,8** |
| M&L Perakende | 94.686 | 45.000 | **%52,5** |
| M&L Diamond | 135.266 | 45.000 | **%66,7** |
| Ariyen (5.000 TL/ay, alt bant) | 180.000 | 45.000 | **%75,0** |
| M&L Gold ERP | 236.716 | 45.000 | **%81,0** |
| Logo Tiger 3 (LEM hariç) | 350.100 | 45.000 | **%87,1** |

Kuyumcum ($30-40/kul/ay) ve Terminal Gold ($4.000-9.000) **dolar bazlı** — kur varsayımı yapmadan TL tasarrufu hesaplanamaz, **hesaplanmadı**. Dolar bazlı olmaları başlı başına satış argümanı: **biz TL sabitliyoruz.**

**Dürüst çerçeve (Eray):** *"'Memleketten silecek' oran orta/üst segmentte (%66-87) gerçekleşiyor; en alt segmentte %35 ile yetinmek zorundayız, abartmam."*

### Hedef segment
**Tek dükkân + 2-3 şubeli perakende kuyumcu.** 19.905 firmalık havuzun ezici çoğunluğu orada; eksiklerimiz (toptan, imalat, barkod, personel) toptancı ve imalatçıyı diskalifiye ediyor, perakendeciyi etmiyor.

### Eray'ın "SATILAMAZ" listesi — bugünkü ürünle gidilemeyecekler
Toptancı · imalatçı/atölyeci/tamiratçı · **e-Fatura mükellefi olan herkes** (*"listedeki en büyük ticari risk"*) · barkod/RFID/sertifika isteyen mağaza · çok kasiyerli primli zincir · mobil/e-ticaret isteyen · **ve Adım 1 bitene kadar MASAK'ı gerçekten kullanmak isteyen her kuyumcu.**

---

## 7. İLERLEME TABLOSU

| Adım | Konu | Durum | Kanıt |
|---|---|---|---|
| 0 | Test kapısı + güvenlik ön koşulları | ✅ **BİTTİ** | 367/367 test (321→367, +46) · `denetim.sh` TEMİZ · mutasyon: append-only kapatılınca 5 testin 4'ü kırmızı; bUnit'te hatalı bileşen parametresi 8 testi birden kırmızıya çevirdi (derleme 0 hata veriyordu) · canlı: cari okuması HASSAS izle düştü, MÜDÜR `/denetim`'e **403**, DELETE **405** |
| 1 | Ekransız 13 ucun UI'ı | ⬜ | |
| 2 | 30.000 TL tevsik kümülasyonu | ⬜ | |
| 3 | Sentetik taş etiketleme | ⬜ | |
| 4 | Veri dışa aktarımı | ⬜ | |
| 5 | Barkod / etiket | ⬜ | |
| — | e-Fatura (Plan 1 Adım 5) | 🔒 K1 | |

Kanıtsız "bitti" yazılmaz (Anayasa md.1).

---

## 8. KURUCU KALEMLERİ — güncellendi

| # | İş | Durum |
|---|---|---|
| **K1** | e-Fatura özel entegratör sözleşmesi | açık — **en büyük ticari risk** |
| **K2** | Mali müşavir: fatura haddi + hurda stopaj oranı | açık |
| **K3** | 2-3 rakip demosu | **kısmen kapandı** — 4 kollu araştırma yapıldı, ama **hiçbir ürünün içi görülmedi**. CetaSoft deneme `.exe`'si halka açık (şifre sayfada), **bilinçli indirilmedi** — güvenilmeyen kaynaktan çalıştırılabilir dosya kuralı. Kurucu ayrı bir makinede açabilir |
| **K4** | Pilot kuyumcu erişimi | açık |
| **K5** | Pilotun MASAK Online kaydı | açık |
| **K6** | ŞİB XML şeması (XSD) | açık |
| **K7 (YENİ)** | **Fiyat kararı** — Eray'ın 1.250 TL/ay önerisi; birim maliyet ölçülmedi, marj onayı gerekiyor | **yeni açıldı** |
| **K8 (YENİ)** | **Konumlandırma metni onayı** — "MASAK uyumluyuz" iddiası değiştirilecek | **yeni açıldı** |

---

## 9. BİLİNÇLİ OLARAK YAPILMAYACAKLAR

| Yapılmayacak | Neden |
|---|---|
| RFID, toptan modülü, imalat/atölye, tamirat, personel/prim, masraf, sertifika/katalog, mobil uygulama, e-ticaret entegrasyonu | Bu turda kapsam dışı. Hedef segment (tek dükkân perakende) bunları istemiyor; isteyen segmente zaten gidilmiyor (§6) |
| Tam SQL dump ucu | Mehmet: yeni saldırı yüzeyi. Operasyon işi (Batuhan), uygulama işi değil |
| Playwright'ın 3 duman senaryosu ötesine genişletilmesi | Aşkın: maliyeti kanıtlanmadan HAYIR |
| DbContext bölme / CQRS | Bertan: erken optimizasyon |
| Pano Faz A'nın kalanı | Plan 1 §3'te bilinçli beklemede — karar değişmedi |
| MFA | Mehmet backlog'a yazdı; bu turda şart değil ama **yazılı kayda geçti** |
| Saklama işinin otomatik zamanlayıcısı | Mehmet: kararı ekipçe oylanacak; "bilinçli konmadı" gerekçesi **bu belgeyle yazılı kayda geçti** ki sonraki oturum unutulmuş eksik sanmasın |

---

## 10. SÜRE

**Süre tahmini verilmiyor.** Plan 1'de de verilmedi ve doğru karardı. Adım 0 ve 1 bitince gerçek hız ölçülür.

---

## EK — ADIM 0 TAMAMLAMA (2026-09-13)

### Yapılanlar

| Madde | İş | Kanıt |
|---|---|---|
| **0a** | HTTP boru hattı testi (`Mvc.Testing` + `WebApplicationFactory`) | `HttpOrtami.cs` + `HttpBoruHattiTestleri.cs` — **17 test**. Gerçek 401 (iki ayrı mesaj), 403, 400 (Nest dizi biçimi), 200, **429 + Retry-After**, camelCase sözleşmesi, kiracı izolasyonu HTTP katmanında |
| **0b** | bUnit altyapısı | `EkranTestleri.cs` — **20 test** (MasakFormu 12 + DenetimIzi ekranı 8) |
| **0c** | `CariModulu` okuma izi | `GetirIzli()` denetleyici yolunda; canlı kanıt: `OKUMA · Cari · HASSAS=EVET · "cari kartı görüntülendi"` |
| **0d** | Denetim izi okuma ucu + ekranı | `Modules/Denetim/DenetimModulu.cs` (2 uç) + `Pages/DenetimIzi.razor` + menü (SAHİP'e görünür) |
| **0e** | Denetim izi append-only | `DenetimIziDegistirilemez` + `DenetimAppendOnlyTestleri` (5 test) |

**Test sayısı: 321 → 367 (+46).** `./denetim.sh` TEMİZ.

### Yol boyunca yakalanan 3 gerçek sorun

1. **`HttpFikstur` veritabanını siliyordu.** İkinci `TestVeritabani` örneği kurucusunda `EnsureDeleted()` çağırıyor ve **diğer koleksiyonun koşan testlerinin altından** şemayı siliyordu → **132 test birden kırmızı**. Düzeltme: şema kurulumu süreç başına bir kez (`SemayiBirKezKur`).
2. **camelCase / PascalCase ikiliği yine patladı.** `d.Eylem` kısayolu doğrudan serileştirmede `"Eylem"`, HTTP yolunda `"eylem"` üretiyordu. Yeni modülde alan adları **açıkça küçük harfle** yazıldı.
3. **Blazor bileşen parametresi hatası derlemeden GEÇTİ.** `<Rozet Metin=...>` ve `<StatKart Baslik=... Ikon=...>` — bu parametreler yok, ama `dotnet build` **0 hata** verdi; Blazor bunu çalışma anında patlatır. Ekran hiç açılmadan "derlendi" sayılıyordu. bUnit yakaladı: mutasyonla geri konduğunda **8 test kırmızı**, derleme yine 0 hata.

### Mehmet'in koşulları — durum

| # | Koşul | Durum |
|---|---|---|
| 1 | Cari modülüne okuma izi (VETO-2) | ✅ |
| 2 | Denetim izi görüntüleme ucu + ekranı, silme ucu yok | ✅ (canlı: DELETE → **405**) |
| 3 | Denetim kaydının değiştirilemezliği + mutasyon kanıtı | ✅ |
| 4 | 13 uç UI'ı (5 şartla) | Adım 1 |
| 5 | Dışa aktarım (6 şartla, VETO-1) | Adım 4 |
| 6 | MFA | backlog'a yazıldı, bu turda değil |
| 7 | Saklama otomatik tetikleme kararı | oylanacak — gerekçe PLAN-2 §9'da yazılı |
| 8 | 30.000 TL tevsik fail-closed | Adım 2 |
| 9 | Mutasyon kanıtı şartı | ✅ sürüyor |

### Aşkın'ın açık sorusu — KAPANDI
"MASAK eşiklerinde sınır-değer testleri var mı?" → **VAR.** `MasakKuralTestleri.cs`'de 32 satır: 184.999,99 / 185.000 / 199.999,99 / 2.000.000 / 2.000.000,01 / 20.000.000 / 20.000.000,01 — **negatif tutarlar dahil**.

### Bilinçli sapma — code-behind ayrımı yapılmadı
Aşkın "test edilecek 12 bileşen için code-behind ayrımı F adımının parçasıdır" demişti. Yapılmadı çünkü **bUnit `@code` bloğunu da test ediyor**; ayrım yalnız private üyeye erişmek gerektiğinde şart. 12 bileşeni toplu taşımak, ölçülmüş kazanç olmadan 4.836 satırlık bir yeniden düzenleme riski. Ayrım, ihtiyaç duyan bileşende o bileşen test edilirken yapılacak. **Aşkın'ın onayına açıktır.**

### Canlı doğrulama (K7)
```
POST /api/v1/cari                → 201  (Ayşe Demir, telefon/vergiNo/adres)
GET  /api/v1/cari/{id}           → 200
GET  /api/v1/denetim?limit=10    → 200, toplam 9 kayıt
     OKUMA  · Cari         · HASSAS=EVET · "cari kartı görüntülendi"
     OKUMA  · DenetimKaydi · —          · "liste: 8 kayıt · denetim izi görüntülendi"
     EKLEME · Cari         · HASSAS=EVET
DELETE /api/v1/denetim/{id}      → 405   (silme ucu yok)
MÜDÜR  GET /api/v1/denetim       → 403   {"message":"Bu işlem için gerekli rol: SAHIP"}
MÜDÜR  GET /api/v1/cari          → 200   (kendi işine erişebiliyor)
```
Test kiracısı `kuyumcu_net`'ten silindi (3 kiracı = başlangıç durumu), sunucu kapatıldı. **Commit yok.**
