# Faz 0 — Kritik Hata Düzeltmeleri ve Test Kapısı

**Tarih:** 2026-09-10 · **Uygulayan:** Tamer Bey (yaz-seri: kod tek elden) · **Kapsam:** `projeler/jewellery`
**Girdi:** `DENETIM-RAPORU-2026-09-10.md` (6 kanıtlı kritik hata) · **Karar:** ekip oyu (aşağıda)
**Kurucu talimatı:** "değişiklikleri uygula test et" + **"kodları commitleme"** → hiçbir şey işlenmedi, değişiklikler çalışma alanında.

---

## 0. Sonuç tek bakışta

| | Öncesi | Sonrası |
|---|---|---|
| Test sayısı | **0** | **43** |
| Test sonucu | — | **43 başarılı, 0 başarısız, 0 atlanan** |
| Kritik hata (K1–K6) | 6 açık | **6 kapalı, hepsi testle kilitli** |
| Veritabanı şeması | migration yok (kurulamıyordu) | **`IlkSema` migration'ı var, testler ona karşı koşuyor** |
| Derleme | 0 hata | 0 hata, 0 uyarı |

**Kırmızı-önce kanıtı (Aşkın'ın fail-closed kuralı):** testler düzeltmeden ÖNCE çalıştırıldı →
`Başarısız: 19, Başarılı: 24, Toplam: 43`. Düzeltmeden SONRA → `Başarısız: 0, Başarılı: 43`.
Yani 19 test gerçekten hatayı yakaladı; "sözde geçti" yok.

---

## 1. Ekip oyu (CLAUDE.md zorunlu oy kuralı)

| Rol | Oy | Özet |
|---|---|---|
| **Bertan** (Baş Mimar) | KOŞULLU EVET | İşaret sözleşmesi Seçenek A; `odeme_turu` kolonu şart; K1 önce kapanmalı |
| **Mehmet** (Güvenlik, veto) | **VETO** | "Faz 0 paketi olmadan üretime çıkış onaylanmaz." Sızıntı listesini genişletti |
| **Aşkın** (QA, fail-closed) | KOŞULLU EVET | Gerçek MySQL + xUnit; kırmızı-önce zorunlu; testler migration şemasına karşı |

**Tamer Bey mührü:** Paket onaylandı ve uygulandı. Mehmet'in vetosu **yayına** yöneliktir ve **kalkmadı** —
üretime çıkış için 4. maddesi (EF Core global kiracı filtresi) hâlâ bekliyor, bkz. Bölüm 5.

### Bertan'ın mühürlenen mimari kararı
1. `hareketler.has_grami` = **fiziki akış** (bize giren `+`, çıkan `−`) — dokunulmadı.
2. `cari_bakiyeler.has_gram_bakiye` = **alacak perspektifi** (`> 0` = müşteri bize borçlu) — TL ile simetrik.
3. **TL ile kapanan işlemin has bacağı cariye YAZILMAZ.** `tl_bakiye` ve `has_gram_bakiye` iki ayrı para birimi defteridir, aynı borcun iki ölçüsü değildir.
4. Emanet için ayrı kolon (`emanet_has_gram`) **Faz 1'e** alındı — emanet hukuken vedia, netleştirilmemeli. Ara çözümde emanet ekranı net emaneti defterden ayrı gösterir.

---

## 2. Yapılan düzeltmeler

### K1 — Kiracılar arası yazma (güvenlik)
Yeni `Common/IslemDogrulayici.cs`: kiracı sınırının tek kapısı. Kullanıcıdan gelen her yabancı kimlik
(**cari, kasa, şube, kur snapshot**) transaction açılmadan önce buradan geçer.
- `SatisModulu`: 6 işlem ucunun tamamı doğrulanıyor.
- `Transfer`: **hedef şube** de doğrulanıyor (hiç doğrulanmıyordu).
- `EmanetModulu.Giris`: cari doğrulaması eklendi (Satış'tan bile korumasızdı).
- `CariBakiyeGuncelle` / `KasaGuncelle` / `StokDegistir` / Emanet ve Cari `BakiyeGuncelle`: sorgulara `TenantId` filtresi.
- `kurSnapshotId` artık gerçekten doğrulanıyor — `docs/DOTNET.md`'nin iddiası nihayet doğru.

### K2 — Alışta çifte kayıt
Kural: **kasaId verildiyse peşin** (cariye TL yazılmaz), verilmediyse cari borçlanır.
Ek olarak `OdenenTl` (nullable) ile **kısmi ödeme** desteği geldi — kuyumcunun "yarısı nakit, kalanı cari" vakası artık tek işlemde.

### K3 — Hurda alışta üçlü kayıt
Aynı ödeme kuralı + **has bacağı hiçbir durumda cariye yazılmıyor.** Hurda satın alınmıştır; müşterinin altın alacağı doğmaz. Aynı düzeltme takasa da uygulandı (eski ürünün değeri `farkTl` içinde zaten paraya çevriliyor).

### K4 — Has bakiyesinin işareti
Emanet girişi `−`, çıkışı `+` oldu (alacak perspektifi). Emanet çıkışındaki yeterlilik kontrolü **cari bakiyeden değil defterden** okunuyor — altın borcu olan müşteri artık kendi emanetini geri alabiliyor.
Arayüz: `Cariler.razor`'da has gram sütununa da **Borç/Alacak rozeti** eklendi (TL ile aynı kural).

### K5 — Saat dilimi
Yeni `Common/Zaman.cs`: gün ve ay sınırları **Europe/Istanbul**'a göre hesaplanıp UTC'ye çevriliyor. Günlük ve aylık rapor ile varsayılan "bugün" bundan besleniyor.

### K6 — Kasa para birimi
Kasa hareketi artık **kasanın kendi para birimiyle** kaydediliyor; TL işlemi döviz kasasına yazılamıyor (açık hata mesajıyla reddediliyor).

### Denetimde olmayan, oy sırasında bulunan iki ek hata
- **Fatura numarası:** `COUNT+1` defterde boşluk varsa mevcut bir numarayı yeniden üretiyordu (benzersizlik ihlali). Ortak `Common/FaturaNoUretici.cs` **en büyük sıra + 1** ile üretiyor; Fatura ve Banka modülleri artık aynı sayacı kullanıyor.
- **Kur kanıtının yetim kalması (Bertan):** 90 günlük snapshot temizliği, hareketlere bağlı kayıtları da siliyordu; `hareketler.kur_snapshot_id` NOT NULL olduğu için her işlemin kur kanıtı 90 gün sonra yetim kalıyordu. Temizlik artık referanslı snapshot'ları hariç tutuyor.
- **Dürüstlük düzeltmesi (Madde 1):** kur verisi truncgil'den geliyor ama kaynak alanına `HAREM` yazılıyordu. Artık `TRUNCGIL` yazılıyor.

### Defterden türetilebilirlik
`hareketler.odeme_turu` kolonu eklendi (NAKIT · KART · HAVALE · ALTIN · VERESIYE · KARMA).
Bertan'ın koşulu buydu: peşin/veresiye ayrımı deftere yazılmadan bakiye append-only defterden yeniden inşa edilemiyordu, yani mutabakat imkânsızdı.

### T1 — Şema
`Kuyumcu.Api/Data/Migrations/20260909220316_IlkSema` üretildi + `TasarimZamaniFabrika` eklendi (migration üretmek için ne JWT secret'ı ne ayakta MySQL gerekiyor). Proje artık temiz bir makinede kurulabilir.

### T2 — Testler
`Kuyumcu.Tests` (xUnit, gerçek MySQL `kuyumcu_test`): 43 test.
Aşkın'ın kararı gereği EF InMemory (transaction desteklemiyor) ve SQLite (decimal hassasiyetini uygulamıyor) reddedildi.
Fixture **yalnız `kuyumcu_test`** veritabanını siler/kurar; geliştirme veritabanına dokunmaz, isim kontrolüyle korunur.

---

## 3. Canlı doğrulama (düzeltilmiş sürüm, taze işletme)

Aynı örnek veri (9 cari, 14 ürün, 33 hareket) düzeltilmiş kodla yeniden yüklendi:

| Kanıt | Ölçüm |
|---|---|
| **K3** — 7,9 g hurda peşin alınan müşteri | `Ali Veli Çelik: TL 0,00 · has 0,000000` (eskiden TL −48.416,29 + has +7,2364) |
| **K4** — 42,5 g 22 ayar emanet bırakan müşteri | `Zeynep Koç: has −38,930000` (biz borçluyuz — eskiden +38,93) |
| **K5** — yerel tarihle günlük rapor (yerel 10 Eylül / UTC 9 Eylül) | `satış 743.367,43 TL · 33 hareket` (eskiden 0) |
| **odeme_turu** deftere yazılıyor | ALIS/VERESIYE 14 · SATIS/NAKIT 4 · SATIS/KART 2 · SATIS/HAVALE 2 · SATIS/VERESIYE 1 · HURDA_ALIS/NAKIT 2 · ISCILIK 1+1 · TAKAS/NAKIT 1 |
| **Kur kaynağı** | `TRUNCGIL` (yeni kayıt) · `HAREM` (2 eski yanlış etiketli kayıt duruyor) |
| **Arayüz** | Cariler ekranında has gram sütununda Borç/Alacak rozeti görünüyor |

Giriş: `duzeltme@bygatech.com` / `Duzeltme2026` — http://localhost:5050

---

## 4. Değişen ve eklenen dosyalar (commit YAPILMADI)

**Yeni:** `Kuyumcu.Api/Common/IslemDogrulayici.cs` · `Common/Zaman.cs` · `Common/FaturaNoUretici.cs` · `Data/TasarimZamaniFabrika.cs` · `Data/Migrations/*` · `Kuyumcu.Tests/` (5 dosya)
**Değişen:** `Modules/Satis/SatisModulu.cs` (yeniden yazıldı) · `Modules/Emanet/EmanetModulu.cs` · `Modules/Cari/CariModulu.cs` · `Modules/Rapor/RaporModulu.cs` · `Modules/Fatura/FaturaModulu.cs` · `Modules/Banka/BankaModulu.cs` · `Modules/Kur/KurModulu.cs` · `Data/Entities.cs` · `Kuyumcu.Api.csproj` · `Kuyumcu.sln` · `Kuyumcu.Web/Pages/Cariler.razor`

---

## 5. Açık kalanlar — dürüst kayıt (Madde 1)

1. **Mehmet'in vetosu kalkmadı.** Zorunlu düzeltme listesinin 4. maddesi (**EF Core global kiracı filtresi**) uygulanmadı. Gerekçe: Auth akışı tenant'sız çalışıyor (`Kullanicilar.AnyAsync(k => k.Email == ...)` giriş öncesi tenant bilmiyor); global filtre bunu sessizce bozar. Doğru yol `IgnoreQueryFilters` muafiyetleriyle birlikte yapmak → **Faz 1**. Şu anda savunma açık filtreler + 7 izolasyon testiyle sağlanıyor. **Üretime çıkış Mehmet'in yeniden onayına bağlı.**
2. **Eski demo verisi bayat.** `demo@bygatech.com` ve `ramazan.akkocan@prometbilisim.com` işletmelerinin bakiyeleri eski (hatalı) kurallarla oluştu; düzeltmeler geçmişe uygulanmaz. Bakiye yeniden inşa aracı **Faz 1**'e alındı. Doğru rakamlar için `duzeltme@bygatech.com` işletmesine bakılmalı.
3. **Sızıntı testi artıkları duruyor:** demo işletmesinde "Kurban Kasa" (99.999 TL) ve "Kurban Musteri" (77.777 TL). Silme komutları güvenlik kancasıyla engelli.
4. **Geliştirme veritabanı elle baseline'landı:** `kuyumcu_net` şeması `EnsureCreated` ile kurulduğu için `odeme_turu` kolonu elle eklendi ve migration geçmişi elle işaretlendi. Temiz kurulumda böyle bir müdahale gerekmez.
5. **Test edilmeyenler:** yük/performans (T5 raporların tüm veriyi belleğe çekmesi hâlâ açık), eşzamanlı fatura kesimi gerçek yarış koşulunda denenmedi (deterministik boşluk testiyle yakalandı), `tenant_id` indeksi (T6) eklenmedi, sayımın deftere yazmaması (T3) ve düzenleme audit'i (T4) Faz 1'de.
6. **Commit yapılmadı.** Kurucu talimatı gereği hiçbir değişiklik işlenmedi; `git status` kirli. `bin/obj` git'te izlendiği için derlemeler de kirlilik üretiyor (T7).
7. **Mevzuat katmanı hâlâ yok.** MASAK, KMTS, e-Fatura, fatura kalem satırı, atölye fire — hiçbiri bu fazın kapsamında değildi. Ürün farkı orada; Faz 1.

---

## 6. Test listesi (43)

| Dosya | Adet | Kapsam |
|---|---|---|
| `HasHesapTestleri.cs` | 17 | Milyem sabitleri (9 ayar), taş düşümü, gerçek milyem, 6 hane yuvarlama, geçersiz ayar |
| `KiraciIzolasyonTestleri.cs` | 7 | Satış(kasa/cari), alış, hurda, transfer(hedef şube), emanet, yabancı kur snapshot |
| `MuhasebeTestleri.cs` | 12 | K2 peşin/veresiye, K3 hurda, K4 emanet+veresiye işareti, emanet iade kuralları, takas, K6 döviz kasası, stok sınırı, paralel defter işaretleri |
| `RaporFaturaTestleri.cs` | 7 | K5 gün/ay sınırı (3), kuyum KDV, fatura no boşluğu, iptal sonrası durum kilidi |

Koşum: MySQL konteyneri ayakta olmalı, sonra `dotnet test dotnet/Kuyumcu.Tests`.

---

## 7. Ek — Kur çubuğu: 10 saniyelik otomatik sorgu + son sorgu zamanı

**Kurucu isteği (2026-09-10):** "yenileme işareti var ama kendi kendine sorgu atmıyorsa 10 saniyede bir kendisi atsın" + "hemen yanına son sorgulama tarihi saati de göstersin".

**Önceki durum:** Sunucu 5 dakikada bir, yalnız 08:00–20:59 arası dış kaynaktan çekiyordu. Arayüz hiç sorgu atmıyordu; yalnız SignalR bildirimini bekliyordu. WebSocket kurulamazsa çubuk ilk değerde donuyordu. Son sorgu zamanı hiçbir yerde görünmüyordu.

**Yapılanlar:**
1. **Sunucu aralığı 10 saniye** (`Kur:GuncellemeSaniye`, 5–3600 sn arasına sıkıştırılır). Saat penceresi de ayarlanabilir oldu (`Kur:BaslangicSaat`/`BitisSaat`), varsayılan **gün boyu** — vitrin gece de güncel kalıyor.
2. **Tur başına dış kaynağa TEK istek.** Eskiden kiracı başına ayrı istek atılıyordu; 10 saniyede bu, ücretsiz kaynakta kısıtlanmayı garanti ederdi. Artık bir kez çekilip tüm kiracılara uygulanıyor.
3. **Fiyat değişmediyse yeni satır yazılmıyor** (`KurServisi.KaydetVeyaMevcut`). 10 saniyelik yazım günde kiracı başına ~8.600 gereksiz kayıt üretirdi.
4. **Arayüz kendi yoklamasını yapıyor:** `CanliKurServisi` içinde 10 saniyelik `PeriodicTimer`. SignalR push da korunuyor (anlık güncelleme için). Yoklama yalnız `/kur/guncel` okur, dış kaynağa gitmez.
5. **Son sorgu zamanı** kur çubuğunun hemen yanında: `🕐 Son sorgu 10.09.2026 02:07:24`. Sorgu başarısız olursa kutu kırmızıya döner ve hata metni tooltip'te görünür; son bilinen kur ekranda kalır.

**Canlı kanıt:**
- Log: `Kur zamanlayıcı başladı (aralık: 10 sn, pencere: 0-23)`
- 35 saniyede 3 tur döndü; 3 tur × 3 kiracı = 9 olası yazım yerine **yalnız 3 kayıt** oluştu (fiyat değişmediği için).
- Arayüzde hiç tıklamadan son sorgu `02:06:54` → `02:07:24` ilerledi.
- 43/43 test yeşil kaldı.

**Not:** Sözlük dosyası (`Sozlukler.cs`) otomatik üretilen ve "elle düzenlemeyin" uyarısı taşıyan bir dosya; üreteç kaynağı (TS sözlükleri) bu repoda yok. Bu yüzden "Son sorgu / Last query" metni bileşen içinde TR/EN olarak yazıldı. Üreteç kaynağı gelince sözlüğe taşınmalı.

### Bu gezinti sırasında görülen iki yeni eksik (düzeltilmedi, raporlanıyor)
1. **Panel'deki "Son 7 Günlük Satış" grafiği ve Raporlar'daki "Günlük Satış/Alış" çubuk grafiği hep boş.** Sebep: `GET /raporlar/aylik` yanıtında `gunler` dizisi hiç yok (`RaporModulu.AylikOzet` gün kırılımı döndürmüyor), arayüz de bu alanı bekliyor. İki grafik de sıfır dolgusuyla çiziliyor.
2. **Panel'deki "Net Kasa" kartı yanıltıcı.** `satış − alış − hurda` hesaplıyor; veresiye alışlar kasaya hiç dokunmadığı hâlde bu karta eksi olarak yansıyor. Gerçek nakit sağdaki "Kasa Özeti"nde. Kartın ya adı ya hesabı düzeltilmeli.

---

## 8. Ek — Kur kaynağı artık seçilebilir (Altınkaynak eklendi)

**Kurucu sorusu:** "sorguyu attığımız sunucu değiştirilebiliyor mu, kullanıcı seçebiliyor mu? Altınkaynak, Ankara Altın Borsası, kuyumcular hangi sitelerden bakıyorsa onlar olmasın mı?"

**Önceki durum (kanıtlı):** Hayır. Adres kodda sabitti (`finans.truncgil.com`), adaptör `new TruncgilAdaptoru(...)` ile doğrudan üretiliyordu, DI kaydı yoktu, ayar dosyasında bile karşılığı yoktu. `tenants` tablosunda kaynak kolonu yoktu. Veritabanı HAREM/KAPALICARSI/IAB adlarını kabul ediyordu ama bunların arkasında **hiçbir bağlantı yoktu** — yalnız etiketti.

### Araştırma sonucu: hangi kaynak gerçekten veri veriyor (ölçüldü)

| Kaynak | Durum | Kanıt |
|---|---|---|
| **Altınkaynak** | ✅ **Açık, anahtarsız JSON** | `static.altinkaynak.com/public/Gold` ve `/public/Currency` → HTTP 200. 26 altın kalemi + 14 döviz, **alış ve satış ayrı**, her kalemde `GuncellenmeZamani` |
| Truncgil | ✅ Açık, anahtarsız | Zaten kullanılıyordu |
| TCMB | ✅ Açık (yalnız döviz) | `tcmb.gov.tr/kurlar/today.xml` → HTTP 200, resmî kur |
| **Ankara Altın Borsası** | ⛔ Açık uç nokta **bulunamadı** | Site açık (HTTP 200) ama fiyatlar `api.ankaraaltinborsasi.com` arkasında; 19 farklı yol denendi, hepsi 404. Mobil uygulaması var, yol adları belgelenmemiş |
| Harem Altın | 💰 Anahtar/ücret gerekiyor | Veri `haremapi.tr` ve `altinapi.com` üzerinden satılıyor; ücretsiz kademe ayda 30 istek (10 sn'lik akış için yetersiz) |
| Sağlamoğlu, Özbağ, Nadir | ⛔ Açık uç nokta yok | Siteleri kendi uygulamalarını besliyor, kamuya açık servis görünmüyor |
| Hasfiyat (11 kaynak birleşik) | 💰 Anahtar gerekiyor | Harem, Hakan, Mayda, Akche, Metal, Sağlamoğlu, Agora, Fikri Şahin, Nadir; REST + WebSocket |

**Altınkaynak'ın verdiği kalemler** (kuyumcunun ekranıyla birebir): Has, Külçe, Gram Altın, 22 Ayar Hurda, 22 Ayar Bilezik, Çeyrek/Yarım/Teklik/Gremse, Ata/Reşat/Hamit/Ata Beşli, Eski ziynet takımı, 14 ve 18 Ayar, Gümüş, ONS, İAB Kapanış.

### Yapılanlar
1. **`AltinkaynakAdaptoru`** yazıldı ve **varsayılan kaynak** yapıldı (`Kur:VarsayilanKaynak`).
2. **`KurKaynakDefteri`**: kod → adaptör defteri. Yeni kaynak eklemek artık tek satır.
3. **`IKurAdaptoru.Kod`** eklendi; kaynak adı adaptörün kendisinden geliyor, artık elle yazılan sabit metin değil.
4. **Kiracı başına seçim:** `tenants.kur_kaynagi` kolonu (migration `KurKaynagi`). Boşsa varsayılana düşer, kur akışı hiç durmaz.
5. **Uçlar:** `GET /api/v1/kur/kaynaklar` (liste + seçili), `PATCH /api/v1/admin/kur-kaynagi` (SAHIP).
6. **Ayarlar ekranına "Kur Kaynağı" kartı** — yalnız SAHIP görür, varsayılan işaretli, tıklayınca kaydedilip çubuk tazeleniyor.
7. **Zamanlayıcı kaynağa göre gruplandı:** kiracılar hangi kaynağı seçtiyse o kaynağa tur başına **tek** istek atılıyor.

### Canlı kanıt
| Test | Sonuç |
|---|---|
| `GET /kur/kaynaklar` | `{"kaynaklar":["ALTINKAYNAK","TRUNCGIL"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK"}` |
| Altınkaynak ile kur | has `6.857,15` · gram `6.880,86` · USD `48,62` · EUR `56,58` · GBP `66,08` |
| Truncgil'e geçiş sonrası | has `6.828,33` · USD `48,5128` — **kaynak gerçekten değişti** |
| Geçersiz kaynak (`HAREM`) | `400 — Geçersiz kur kaynağı: HAREM. Seçilebilir: ALTINKAYNAK, TRUNCGIL` |
| Arayüz | Kart göründü, Truncgil'e tıklandı → "Kur kaynağı güncellendi", geri alındı |
| Testler | 43/43 yeşil |

### Bu iş sırasında bulunup düzeltilen ek hata
**Ayarlar ekranında yarış durumu:** sayfa, profil (`/auth/ben`) yüklenmeden rol kontrolü yapıp kaynak listesini hiç istemiyordu; kart boş görünüyordu. Liste artık koşulsuz yükleniyor, yalnız kartın gösterimi ve yazma yetkisi SAHIP'e bağlı.

### Açık kalanlar (dürüst)
1. **Harem, Ankara Altın Borsası, Sağlamoğlu eklenmedi.** Harem ve Hasfiyat **ücretli anahtar**, Ankara borsasının açık uç noktası yok. Anahtar/sözleşme **para ve dış taahhüt** demek → CLAUDE.md gereği bu **Kurucu kararı**. Karar verilirse adaptör yazımı yarım gün.
2. **Altınkaynak için yazılı izin alınmadı.** Adres "public" adını taşıyor ve kendi sitesini besliyor, ama satılacak bir ürün için kullanım izni yazılı alınmalı. Bu da Kurucu'nun alanı.
3. **Alış/satış ayrımı kullanılmıyor.** Altınkaynak hem alış hem satış veriyor; veri modeli tek fiyat tuttuğu için satış kullanılıyor. Hurda alışında kuyumcunun alış fiyatını otomatik görmesi ancak modele alış kolonu eklenince olur (Faz 1, marj işiyle birlikte).
4. **Kaynağın kendi güncelleme saati (`GuncellenmeZamani`) kaydedilmiyor.** Şu an yalnız bizim sorgu saatimiz görünüyor. Kaynağın saatini de göstermek `kur_snapshots`'a kolon ister.

---

## 9. Ek — Ankara Altın Borsası eklendi, seçim panele taşındı

**Kurucu isteği:** "kur kaynağı ayarlarda olmasın, panelde dropdown'da olsun, isteyen istediğinde değiştirsin. Ayrıca neden Ankara Altın Borsası eklenmedi?"

### Neden ilk turda eklenmemişti — ve nasıl bulundu
İlk araştırmada sitenin HTML'ine ve tahmine dayalı 19 yola bakmıştım, hepsi 404 döndü ve "açık uç nokta yok" diye raporladım. **Bu eksik bir aramaydı.** Bu turda sitenin kendi JavaScript paketleri (14 parça, 896 KB) indirilip tarandı ve gerçek arayüz bulundu:

- **GraphQL:** `POST https://api.ankaraaltinborsasi.com/graphql/site`
- **Sorgu:** `GetLatestGoldPrices { siteLatestGoldPrices { parity buyPrice sellPrice currency } }`
- **Canlı akış:** Socket.IO `…/gold-prices`, olay adı `price-update` (henüz kullanılmıyor, 10 sn'lik sorgu yeterli)
- Anahtar gerekmiyor.

**Ölçüm:** HTTP 200, **35 kalem** — HAS-ALTIN, KULCE-ALTIN, GRAM-ALTIN, 22-AYAR-BILEZIK, 14/18 ayar, 1–500 gr külçe (995), eski ve yeni ziynet takımı (çeyrek/yarım/teklik/gremse/ata/5'li), Reşat, Hamit, gümüş, ONS altın ve gümüş. Hepsinde **alış ve satış ayrı**.

**Önemli sınır:** Bu kaynak saf altın borsasıdır, **USD/EUR/GBP TL kuru yayınlamaz**. Bu yüzden ANKARA kaynağı seçiliyken döviz **TCMB resmî günlük kurundan** tamamlanıyor (ücretsiz, anahtarsız). Ölçümde fark görünür: Altınkaynak dolar `48,62` (serbest piyasa), TCMB `48,4777` (resmî). Kod bunu açıkça belgeliyor.

### Yapılanlar
1. **`AnkaraAltinBorsasiAdaptoru`** yazıldı, kaynak defterine eklendi. Artık üç kaynak var: **Altınkaynak (varsayılan) · Ankara Altın Borsası · Truncgil**.
2. **Seçim Ayarlar'dan kaldırıldı, Panel'e taşındı.** Panel'in sağ kolonundaki kur kartı artık güncel has fiyatını, kaynak dropdown'ını ve son sorgu saatini birlikte gösteriyor.
3. **Yetki açıldı:** `PATCH /api/v1/kur/kaynak` **tüm rollere** açık (kiracının kendi ayarı). Yalnız-SAHIP olan `PATCH /admin/kur-kaynagi` kaldırıldı — tek kapı kaldı.
4. **Kaynak değişince beklemek yok:** ayar kaydedilir, yeni kaynaktan **anında** kur çekilir ve bağlı tüm istemcilere push edilir. Çekim başarısız olursa ayar yine kaydedilir ve kullanıcıya uyarı döner (kur akışı durmaz).

### Canlı kanıt
| Test | Sonuç |
|---|---|
| Kaynak listesi | `{"kaynaklar":["ALTINKAYNAK","ANKARA","TRUNCGIL"],"varsayilan":"ALTINKAYNAK"}` |
| ANKARA | has `6.828,81` · gram `6.848,61` · usd `48,4777` (TCMB) · eur `56,4075` · gbp `65,7928` |
| ALTINKAYNAK | has `6.849,76` · usd `48,62` (serbest piyasa) |
| TRUNCGIL | has `6.822,52` · usd `48,5054` |
| **TEZGÂHTAR** ile değiştirme | `{"secili":"ANKARA","uyari":null}` — başarılı, yetki açık |
| Panel dropdown | Ankara → Altınkaynak seçildi, kart `6.828,81` → `6.850,12` oldu, "Son sorgu: 02:38:27" |
| Testler | 43/43 yeşil |

### Not — bilinçli tasarım kararı
Kaynak **kiracı (işletme) ayarıdır**, kullanıcı ayarı değil. Tezgâhtar değiştirdiğinde o işletmenin tüm kullanıcıları etkilenir ve satış fiyatı hesabı da o kaynağa göre işler. Kurucu "isteyen istediğinde değiştirsin" dediği için rol kısıtı konmadı. İleride kısıtlamak istenirse uca tek satırlık `[RolGerekli(...)]` yeterli.

### Hâlâ eklenmeyenler ve gerekçesi
- **Harem Altın:** verisi `haremapi.tr` / `altinapi.com` üzerinden **ücretli** satılıyor (ücretsiz kademe ayda 30 istek → 10 sn'lik akış için imkânsız). Anahtar = para → **Kurucu kararı**.
- **Hasfiyat (tek anahtarla 11 kaynak: Harem, Hakan, Mayda, Akche, Metal, Sağlamoğlu, Agora, Fikri Şahin, Nadir):** aynı şekilde ücretli.
- **Sağlamoğlu / Özbağ / Nadir doğrudan:** kamuya açık servis bulunamadı.
- **Yazılı kullanım izni:** Altınkaynak ve Ankara Altın Borsası uç noktaları anahtarsız ve kendi sitelerini besliyor; satılacak üründe kullanmak için yazılı izin alınması **Kurucu'nun alanı** (dış taahhüt).

---

## 10. Ek — Ürün fiyatları kur kaynağından okunuyor (parite eşleştirmesi)

**Kurucu fikri:** "Ürünler kısmındaki ürünler de zaten sorgulardan geliyordur, eşleşenlerin fiyatını sorgudan gelene göre gösterelim; 'yeni yarım ne kadar' dendiğinde direkt fiyat verilsin."

**Bir düzeltme:** Ürünler sorgudan **gelmiyor** — dükkânın kendi stok kartlarıdır (kod, ayar, gram, taş, işçilik). Kur kaynağı yalnız fiyat verir. Ama eşleştirme fikri doğru ve uygulandı.

### Neden gerekliydi
Eskiden ürün fiyatı tek yolla hesaplanıyordu: `has gram × kur + işçilik`. Bu **imalat takısı** için doğru, **sarrafiye** için yanlış. Çeyreğin, yarımın, Ata'nın piyasada kendi kotasyonu vardır (içinde darphane işçiliği ve prim vardır). "Yeni Yarım" 22.324,87 TL'den işlem görürken has hesabı 21.900 TL veriyordu.

### Kurulan yapı
1. **Kaynaktan bağımsız kanonik parite sözlüğü (22 kalem).** Her kaynak kendi kodunu ortak koda eşler:
   Altınkaynak `PY` · Ankara `YENI-YARIM` · Truncgil `yarim-altin` → hepsi **`YARIM_YENI`**.
   Kaynak değişse ürün eşleşmesi bozulmaz.
2. **Fiyat birimi kavramı** — kritik ayrım: kaynaklar bazı kalemleri **adet**, bazılarını **gram** başına kote eder.
   - `ADET`: çeyrek, yarım, tam, ata, reşat, hamit, gremse, beşli → fiyat = kotasyon
   - `GRAM`: gram altın, külçe, 22 ayar bilezik, 14/18 ayar, hurda, gümüş → fiyat = kotasyon × net gram
   - `HAS_GRAM`: has altın → fiyat = kotasyon × has gram
   Bu ayrım olmasa 20 gramlık bilezik tek gramın fiyatına satılırdı.
3. **`urunler.parite_kodu`** kolonu + migration `UrunPariteKodu`.
4. **Otomatik eşleşme** ürün adından: "Yeni Yarım Altın" → `YARIM_YENI`. Türkçe harfler sadeleştirilir (Külçe → kulce).
   **Temkinli kural:** yalnız kotasyonu doğrudan ürünün fiyatı olan kalemler otomatik bağlanır. "22 Ayar Bilezik", "14/18 Ayar", "Hurda", "Gümüş" gram kotasyonları **otomatik bağlanmaz** — dükkânın kendi işçiliğini ezmemesi için elle seçilir.
5. **Yeni uç:** `GET /api/v1/kur/pariteler` → kaynağın alış/satış/birim listesi. Zamanlayıcının her turda çektiği veri bellekte tutulur, ek dış istek atılmaz.
6. **Ürünler ekranına "Piyasa Fiyatı" sütunu:** eşleşen ürün yeşil parite rozetiyle (`Yeni Çeyrek`), eşleşmeyen gri `hesaplanan` rozetiyle görünür.
7. **Ürün formuna parite seçimi** — otomatik tahmini elle düzeltmek için.
8. **İşlemler ekranındaki otomatik fiyat** artık parite fiyatını tercih ediyor: sarrafiye satışında tezgâhtar doğru fiyatı görüyor.

### Bu iş sırasında bulunan ve düzeltilen hata: gümüş altın kuruyla fiyatlanıyordu
"925 Gümüş Zincir Kolye" (12 g) ekranda **76.972,08 TL** görünüyordu. Sebep: gümüşün has gramı (11,1) **altın** has fiyatıyla (6.904) çarpılıyordu. Bu hata denetim raporunda yoktu, parite sütunu eklenince ortaya çıktı.
Düzeltme: ayarı `G` ile başlayan ürünler gümüş gram kotasyonundan hesaplanır; kotasyon yoksa yanlış rakam yerine `—` gösterilir. Aynı koruma İşlemler ekranına da kondu.
**Ölçüm:** 76.972,08 TL → **1.629,44 TL** (12 g × 106,67 + 350 işçilik).

### Canlı kanıt (Altınkaynak kaynağı, gerçek ekran)
| Ürün | Parite | Ekrandaki fiyat | Kaynak |
|---|---|---|---|
| Ata Lira | ATA_YENI | **48.685,00 TL** | parite (adet) |
| Çeyrek Altın (Yeni Tarihli) | CEYREK_YENI | **11.635,00 TL** | parite (adet) |
| Yarım Altın | YARIM_YENI | **23.275,00 TL** | parite (adet) |
| Tam Altın | TAM_YENI | **46.540,00 TL** | parite (adet) |
| Gram Altın 24 Ayar (1 g) | GRAM | **6.925,40 TL** | parite (gram) |
| Külçe Altın 24 Ayar (10 g) | KULCE | **68.670,30 TL** | parite (gram × 10) |
| 22 Ayar Burma Bilezik (20 g) | — | 127.936,21 TL | hesaplanan (has × kur + işçilik) |
| 925 Gümüş Zincir Kolye (12 g) | — | **1.629,44 TL** | hesaplanan (gümüş gram) — eskiden 76.972,08 |

Parite listesi API'den 22 kalem olarak doğrulandı, birimler doğru (`HAS→HAS_GRAM`, `GRAM/KULCE/BILEZIK_22/AYAR_14/AYAR_18/HURDA_22/GUMUS→GRAM`, sarrafiye→`ADET`).

**Testler: 81/81 yeşil** (43 → 81; 38 yeni test: parite tahmini, birim doğruluğu, elle seçim, geçersiz kod reddi, eşleşmeyi kaldırma).

### Açık kalanlar
1. **Mevcut ürünler otomatik bağlanmadı.** Otomatik tahmin yalnız ürün eklenirken/güncellenirken çalışır. Demo verisindeki 6 sarrafiye ürünü elle bağlandı. Toplu geçmişe dönük eşleştirme komutu yok (Faz 1).
2. **Alış fiyatı gösterilmiyor.** Kaynaklar alış/satış ikisini de veriyor ve API ikisini de taşıyor; ekranda yalnız satış var. Hurda alışında kuyumcunun alış kotasyonunu görmesi doğal devam (Faz 1, marj işiyle birlikte).
3. **Kotasyona marj eklenemiyor.** Kuyumcu kendi payını koyamıyor; parite fiyatı ham geliyor.
4. **ONS kalemi USD cinsinden**, TL ürün fiyatına doğrudan uygun değil; listede duruyor ama ürüne bağlanması önerilmez.

---

## 11. GERİ ALMA — parite fiyatının ürüne/işleme yansıtılması kaldırıldı

**Kurucu kararı (2026-09-11):** "bu son yaptığımız ürün fiyatlarını geri mi alsak, sağlıklı değil gibi." Değerlendirme istendi, gerekçeli geri alma önerildi ve onaylandı.

### Gerekçe (neden sağlıklı değildi)
1. **Kotasyon toptancının fiyatı, dükkânın satış fiyatı değil.** Altınkaynak'ın çeyrek için gösterdiği satış, Altınkaynak'ın sattığı fiyattır; perakendeci ondan alıp daha yükseğe satar. Bunu ürünün fiyatı diye göstermek yanlış vaat.
2. **Marj alanı yok.** Kuyumcunun kendi payını ekleyeceği bir yer olmadığı için parite fiyatı ham kalıyordu. Asıl kök sebep bu.
3. **En riskli kısım İşlemler ekranıydı:** otomatik dolan fiyat **deftere yazılan tutarı** etkiliyordu. Tezgâhtar "Ekle" dediğinde kayda toptancı kotasyonu giriyordu.
4. **Ada dayalı otomatik eşleşme sessizce yanlış etiketleyebilir** ("Ata Lira Kolye" → ATA_YENI).

### Geri alınanlar
- `Urunler.razor`: **Piyasa Fiyatı sütunu kaldırıldı**, ürün formundaki parite seçimi kaldırıldı, fiyat hesabı ve parite yükleme kodu söküldü. Ekran parite öncesi haline döndü (17.165 → 11.983 bayt).
- `Islemler.razor`: otomatik fiyat **artık parite fiyatını kullanmıyor**; `has gram × kur + işçilik` kuralına döndü.
- `UrunServisi.PariteCoz`: **ada göre otomatik tahmin kapatıldı.** Alan yalnız açıkça gönderilirse doldurulur ve geçerliliği doğrulanır.
- Demo verisindeki **6 ürünün parite bağı temizlendi.**

### Bilinçli olarak geri ALINMAYANLAR
1. **Gümüş fiyatlama hatası düzeltmesi kaldı.** Gümüşün has gramı altın kuruyla çarpılıyordu (12 g kolye → 76.972 TL). Bu parite işiyle ilgisi olmayan gerçek bir hataydı. Yeni davranış: gümüş ürünlerde otomatik fiyat **hiç doldurulmuyor** (yanlış rakam yerine boş), kullanıcı elle girer. Gerekçe kodda yazılı.
2. **Parite altyapısı sunucuda uykuda bırakıldı** — kaynak adaptörlerinin içine gömülü olduğu için sökülmesi Altınkaynak ve Ankara adaptörlerini bozardı. `GET /kur/pariteler` ucu ve `urunler.parite_kodu` kolonu duruyor ama **hiçbir ekran kullanmıyor, hiçbir fiyatı etkilemiyor.**
3. **Fiyat birimi (ADET/GRAM/HAS_GRAM) modeli ve testleri kaldı** — Faz 1'de marj ve alış/satış modeliyle birlikte doğru şekilde açılacak.

### Doğrulama
| Kontrol | Sonuç |
|---|---|
| Testler | **81/81 yeşil** (hiç test silinmedi; beklentiler geri alma kararını belgeliyor) |
| Kur akışı | 3 kaynak duruyor: `["ALTINKAYNAK","ANKARA","TRUNCGIL"]`, has 6.798,89 · usd 48,6290 |
| Parite ucu | 22 kalem dönüyor ama ekranlarda kullanılmıyor (uykuda) |
| Otomatik eşleşme | "Yeni Yarım Altın" adıyla ürün eklendi → `pariteKodu: null` (kapalı) |
| Ürünler ekranı | Piyasa Fiyatı sütunu yok, eski hâline döndü (ekran görüntüsüyle doğrulandı) |

**Dürüst kayıt:** Doğrulama için eklenen `GERIALMA-TEST` kodlu ürün pasife alındı (sistem kalıcı silme yapmıyor, soft delete).

### Faz 1 için doğru sıra
Parite fiyatı ancak şu üçü hazır olunca geri açılmalı: **(a)** kotasyona eklenecek marj/prim alanı, **(b)** alış ve satış fiyatının ayrı modellenmesi (kaynaklar ikisini de veriyor), **(c)** eşleşmenin kullanıcı tarafından açıkça onaylanması — otomatik tahmin varsayılan olmamalı.
