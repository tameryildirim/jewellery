# Paket 41 — MASAK kapısının banka hareketlerine yayılması

> Katman: Katman 6 — Uyum (paket 38–42) · Tahmin: 2–3 gün · Önkoşullar: yok · Kaynak: katman mimarı K6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 4
> ⚠ **VETO:** Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: ŞARTLI VETO (K41-1 kapanırsa kalkar): kümülatif MASAK ölçümü kullanıcı tarafından girilen dekont tarihine (BankaModulu.cs:194-197 `DateTime.Parse(dto.Tarih)`) bağlanırsa, 185.000 TL eşiği 'dün' + 'bugün' tarihli iki dekontla aynı oturumda aşılır ve kimlik tespiti hiç tetiklenmez — eşik bölme (structuring) yolu paketin kendisi tarafından AÇILMIŞ olur. hareketler tarafı sunucu saatiyle ölçüldüğü için (MasakKapisi.cs:134 `an`) bu, kapıyı iki farklı eksende çalıştırmak demektir. Kapı `now` ile ölçülüp banka tarafı `Olusturuldu` üzerinden okunmadan ve gelecek tarih reddedilmeden paket 41 sevk EDİLEMEZ.

## Hedef
MASAK kimlik tespiti eşiği bugün yalnız defter hareketlerine (satış, alış, hurda, takas, işçilik, veresiye, tahsilat) uygulanıyor; elle girilen banka dekontu kapıyı HİÇ görmüyor. Aynı müşteriyle aynı gün 100.000 TL satış + 100.000 TL havale yapan biri 200.000 TL'ye ulaştığı hâlde kimlik tespiti tetiklenmiyor. Bu paket kapıyı banka hareketlerine yayar ve kümülatif toplamı çift saymaya yol açmadan iki kaynaktan okur.

## Kodda bugünkü durum
BankaServisi.HareketEkle (BankaModulu.cs:180-223) cariyi doğruluyor (184-189), transaction açıyor (199), BankaHareket satırını yazıyor (203-218) — ama MASAK yok: BankaModulu.cs dosyasında 'masak' kelimesi 0 KEZ geçiyor. Kapı 8 yerden çağrılıyor ve hepsi hareketler yazan akışlar (SatisModulu.cs:435, 554, 651, 891, 1069, 1178, 1391, 1483 + CariModulu.cs:406, 464). Kümülatif toplam YALNIZ hareketler'i okuyor (MasakKapisi.cs:137-142) ve tabi türler hareketler.tur değerleri (MasakKapisi.cs:149-150). MasakKimlikTespiti.HareketId hareketler'i işaret ediyor (MasakVarliklari.cs:50-51); banka hareketi o tabloda DEĞİL. banka_hareketleri'nde cari_id var (Entities.cs:469) ama indeks yalnız (tenant_id, tarih) (KuyumcuDbContext.cs:128) — cari bazlı günlük toplam bu indeksten verimli okunamaz. banka_hareketleri'nde hareketler'e bağ kolonu YOK (Entities.cs:457-473).

## Kapsam
**İçinde:**
- masak_kimlik_tespitleri ve masak_beyanlari'na kaynak ayrımı (kaynak_tur)
- banka_hareketleri'ne hareket_id bağ kolonu + (tenant_id, cari_id, tarih) indeksi
- MasakKapisi.GunlukIslemToplam'ın iki ayrık sorguya bölünmesi (çift sayma yapısal olarak imkânsız)
- BankaServisi.HareketEkle'ye iki fazlı MASAK kapısı (Dogrula tx'ten önce, Kaydet tx içinde)
- HareketEkleDto'ya MasakBilgiDto bloğu + Banka.razor dekont formuna MASAK alanları

**Dışında:**
- Tevsik kapısı banka hareketine UYGULANMAZ: banka hareketi tanımı gereği banka kanalıdır, 459 s. tebliğin istediği tevsik zaten sağlanmıştır. Nakit payı 0 olduğu için kümülatif nakit toplamına da girmez.
- Gün kilidi (GunKapanisiKapisi) ve vade kapısı banka hareketine bu pakette bağlanmaz — ayrı kararlar, ayrı paketler
- Faturaya dönüştürme (BankaModulu.cs:250) MASAK'a tabi değildir: o anda para el değiştirmez, belge üretilir
- Banka API ile otomatik hareket çekme (C sınıfı)
- Risk skorlama (paket 40)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| masak_kimlik_tespitleri | 1 yeni kolon | kaynak_tur VARCHAR(20) NOT NULL DEFAULT 'HAREKET' — 'HAREKET' | 'BANKA_HAREKET'. ⚠ HareketId kolonunun ADI ve ANLAMI korunur; hangi tabloyu işaret ettiği kaynak_tur ile çözülür. Alternatif (yeni kaynak_id kolonu açıp hareket_id'yi boşaltmak) REDDEDİLDİ: 8 yıl saklanan append-only bir tabloda mevcut satırların anlamını geriye dönük değiştirmek denetimde açıklanamaz. | Mevcut (tenant_id, cari_id) yeterli (KuyumcuDbContext.cs:150) | YYYYMMDDHHMMSS_MasakBankaKaynagi |
| masak_beyanlari | 1 yeni kolon — simetri için | kaynak_tur VARCHAR(20) NOT NULL DEFAULT 'HAREKET'. Tespit kaynak taşıyıp beyan taşımasaydı, beyan satırının hangi işleme ait olduğu yalnız kimlik_tespiti_id üzerinden dolaylı çözülürdü. | mevcut | aynı migration |
| banka_hareketleri | 1 yeni kolon + 1 yeni indeks | hareket_id VARCHAR(36) NULL — bu dekontun hareketler defterindeki karşılığı. BUGÜN HER ZAMAN NULL (tüm dekontlar elle girilir). ⚠ NEDEN ŞİMDİ EKLENİYOR: kümülatif sorgusunun 'hareket_id IS NULL' süzgeci ilk günden var olmalıdır. Kolon sonradan eklenip süzgeç de sonradan yazılırsa, arada geçen sürede otomatik dekont üreten bir akış eklenirse çift sayma SESSİZCE açılır. Bugün süzgeç etkisizdir (hepsi null) ve davranışı hiç değiştirmez. | (tenant_id, cari_id, tarih) — kümülatif günlük toplam bu indeksten okur; bugünkü (tenant_id, tarih) indeksi cari süzgeci için yetersiz | aynı migration |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/bankalar/{id}/hareket` | SAHIP, MUDUR, MUHASEBE (BankaModulu.cs:362 aynen) | DEĞİŞTİ. HareketEkleDto'ya 'public MasakBilgiDto? Masak' eklenir (SatisDto'lardaki aynı blok — MasakKapisi.cs:32-46). Eşik altında boş kalır, mevcut çağrılar kırılmaz. Eşik üstünde (kümülatif dâhil) eksik varsa 400 ve HİÇBİR satır yazılmaz. | YENİ: MasakKapisi.DogrulaKumulatif (tx öncesi) + MasakKapisi.Kaydet (tx içinde, SaveChanges öncesi) |
| GET | `/api/v1/masak/gereksinim?tutar=` | tümü | DEĞİŞMEZ. Banka ekranı da bu ucu kullanır (MasakModulu.cs:279-281) — POS ile aynı form davranışı. | — |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` — (a) GunlukIslemToplam (satır 131-143) İKİ AYRIK SORGUYA bölünür: hareketler tarafı BUGÜNKÜ hâliyle kalır; banka tarafı 'db.BankaHareketleri.Where(b => b.TenantId==… && b.CariId==cariId && b.Tarih>=bas && b.Tarih<bit && b.HareketId == null).SumAsync(b => (decimal?)b.Tutar)'. İki toplam TOPLANIR. CASE/UNION/COALESCE bileşimi YAZILMAZ (A5'te alınan karar: bileşik ifadenin MySQL'e çevrimi doğrulanmamıştı). (b) Kaydet (satır 156-207) imzasına 'string kaynakTur = "HAREKET"' opsiyonel parametresi eklenir ve tespit/beyan satırlarına yazılır — mevcut 8 çağrı noktası DEĞİŞMEZ. (c) Sınıf başlığındaki 'Kapıya tabi akışlar' yorumu (satır 20-21) ve 'bilinen eksik ölçüm' notu (satır 106-112) banka kaynağıyla güncellenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs` — (a) HareketEkleDto'ya (satır 56-78) MasakBilgiDto? Masak. (b) BankaServisi ctor'a MasakKapisi masak. (c) HareketEkle içinde cari doğrulamasından sonra, BeginTransactionAsync'ten (satır 199) ÖNCE: var masakGerek = await masak.DogrulaKumulatif(db, islem.TenantId, dto.CariId, dto.Tutar, dto.Masak, now); (d) BankaHareketleri.Add'den sonra, SaveChangesAsync'ten (satır 219) ÖNCE: masak.Kaydet(db, masakGerek, hareketId, dto.CariId, dto.Tutar, dto.Masak, islem, now, kaynakTur: "BANKA_HAREKET"); (e) Yeni BankaHareket satırına HareketId = null açıkça yazılır + gerekçe yorumu. (f) FaturayaDonustur'a (satır 250) MASAK EKLENMEZ ve nedeni yoruma yazılır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs` — MasakKimlikTespiti ve MasakBeyani'ye kaynak_tur kolonu + HareketId'nin özet yorumuna 'kaynak_tur'e göre hareketler ya da banka_hareketleri' notu.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — BankaHareket sınıfına (satır 457-473) hareket_id kolonu + neden bugün hep null olduğunu ve neden şimdi eklendiğini anlatan XML yorumu.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — mb.Entity<BankaHareket>().HasIndex(h => new { h.TenantId, h.CariId, h.Tarih }); — satır 128'in yanına, Hareket'teki (tenant, cari, tarih) indeksinin (satır 113) gerekçesiyle aynı gerekçe yorumu.

## Ekranlar
- `/banka` · `dotnet/Kuyumcu.Web/Pages/Banka.razor` — Dekont giriş formuna MASAK bloğu: tutar girilince GET /masak/gereksinim?tutar= çağrılır (POS'taki mevcut davranış) ve kademe YOK değilse kimlik/beyan alanları AÇILIR. Eşik altında hiçbir alan görünmez — bugünkü akış hiç değişmez. Cari seçilmeden büyük tutar girilirse 'kümülatif hesaplanamıyor, kişi bilinmiyor' uyarısı gösterilir (MasakKapisi.cs:118-119 davranışının ekran karşılığı). (menü/yetki: Menü değişmez (KenarCubugu.razor:38). Modül 'banka' (BankaModulu.cs:328).)

## İş kuralları (fail-closed)
- Kapı İKİ FAZLIDIR ve sıra korunur: Dogrula transaction'dan ÖNCE (eksik kimlikle hiçbir yazma başlamasın), Kaydet transaction İÇİNDE SaveChanges'ten önce (hareket ve tespit AYNI commit'te — 'dekont var, tespit yok' ara durumu doğmasın). MasakKapisi.cs:12-18'deki mühürlü desen.
- Kümülatif İKİ AYRIK KÜMEDEN toplanır: (1) hareketler (bugünkü sorgu, değişmez), (2) banka_hareketleri'nde hareket_id IS NULL olanlar. Kümeler kesişmez; bir dekont bir hareketle eşleştiği gün otomatik olarak banka tarafından DÜŞER ve hareketler tarafında sayılır. Çift sayma yapısal olarak imkânsızdır.
- Yön ayrımı yapılmaz: GELEN de GIDEN de eşiğe tabidir. Motor zaten Math.Abs alıyor (MasakKuralMotoru.cs:100) ve 'alış da satış da eşiğe tabidir' yorumu bunu söylüyor.
- Cari YOKSA kümülatif kurulamaz (kişi bilinmiyor) → yalnız bu işlemin kendi tutarı değerlendirilir. MasakKapisi.cs:118-119'daki mevcut davranış aynen geçerlidir ve banka için de aynıdır.
- TEVSİK KAPISI ÇAĞRILMAZ. Banka hareketi tanımı gereği banka kanalıdır; 459 s. tebliğin istediği tevsik sağlanmıştır. Nakit payı 0 olduğu için GunlukNakitToplam'a da girmez. Bu, kapıyı GEVŞETEN değil, kapsamını doğru çizen bir karardır ve koda gerekçesiyle yazılır.
- Faturaya dönüştürme MASAK'a tabi DEĞİLDİR: para o an el değiştirmez, belge üretilir. Gerekçe koda yazılır ki sonraki oturum 'unutulmuş' sanmasın.
- kaynak_tur varsayılanı 'HAREKET'tir; mevcut 8 çağrı noktası ve tüm eski satırlar anlamını korur.
- banka_hareketleri.hareket_id bu pakette HER ZAMAN null yazılır. Doldurulması ayrı bir karardır (banka ekstresi içe aktarma / havale bacağının otomatik dekontu — PARITE paket 32).
- MASAK eşikleri ve kademeleri DEĞİŞMEZ. Bu paket yeni bir eşik getirmez; var olan eşiği görmeyen bir akışı kapıya bağlar.

## Testler
- `dotnet/Kuyumcu.Tests/MasakBankaTestleri.cs` [DB]
  - 185.000 TL'lik GELEN dekont kimliksiz → 400, HİÇBİR banka_hareketleri satırı yazılmadı, hesap bakiyesi DEĞİŞMEDİ
  - Aynı dekont kimlikle → 200 ve masak_kimlik_tespitleri'nde kaynak_tur='BANKA_HAREKET', hareket_id = banka hareketinin kimliği
  - GIDEN yönde de eşik uygulanır (yön ayrımı yok)
  - BAĞLANTILI İŞLEM: aynı cariye 100.000 TL satış (hareketler) + 100.000 TL dekont (banka) → ikinci işlem 400, mesajda kümülatif 200.000 TL geçiyor
  - Kümülatif ÇİFT SAYMAZ: hareket_id dolu bir banka hareketi elle kurulup aynı gün işlem yapılınca toplam yalnız hareketler tarafından sayılır
  - Cari verilmeden 185.000 TL dekont → kümülatif kurulmaz, tek işlem kuralı işler (kimlik yine istenir çünkü tek başına eşiğin üstünde)
  - Cari verilmeden 100.000 TL dekont → 200 (eşik altı, kümülatif yok)
  - 184.999,99 → 200; 185.000,00 → 400 (sınır, >= operatörü)
  - 2.000.000,01 TL dekont kimlikli ama fon kaynağı yok → 400 (beyan formu kademesi banka yolunda da işliyor)
  - Yabancı kiracının carisiyle dekont → 404
  - Faturaya dönüştürme 185.000 TL'lik bir hareket için kimlik İSTEMEZ (200) — bilinçli kapsam dışı
- `dotnet/Kuyumcu.Tests/MasakEntegrasyonTestleri.cs` [DB]
  - MEVCUT 8 akışın davranışı DEĞİŞMEDİ (regresyon): kaynak_tur varsayılanı 'HAREKET' yazılıyor
- `dotnet/Kuyumcu.Tests/BankaEkranTestleri.cs` [bUnit]
  - Tutar 185.000 girilince /masak/gereksinim çağrılır ve kimlik alanları açılır
  - Eşik altı tutarda MASAK alanları GÖRÜNMEZ
  - Kimlik alanları açıkken boş bırakılırsa Kaydet KAPALI
  - Gövdede masak bloğu doğru alan adlarıyla gidiyor (belgeTuru, belgeNo, adSoyad)
  - Cari seçilmeden eşik üstü tutarda 'kümülatif hesaplanamıyor' uyarısı çıkar
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - POST /api/v1/bankalar/{id}/hareket TEZGAHTAR ile 403 (mevcut rol korunuyor)

## Mutasyon kanıtları
- BankaServisi.HareketEkle'den DogrulaKumulatif çağrısı kaldırılırsa → '185.000 kimliksiz dekont 400' KIRMIZI
- Kaydet çağrısı kaldırılırsa → 'kaynak_tur=BANKA_HAREKET tespit satırı' KIRMIZI
- Dogrula transaction'ın İÇİNE alınırsa → 'hiçbir satır yazılmadı / bakiye değişmedi' KIRMIZI (kısmi yazma görünür)
- GunlukIslemToplam'a banka kümesi eklenmezse → 'satış + dekont kümülatifi' KIRMIZI
- 'hareket_id IS NULL' süzgeci kaldırılırsa → 'kümülatif çift saymaz' KIRMIZI
- Yön süzgeci eklenip yalnız GELEN'e uygulanırsa → 'GIDEN yönde de eşik' KIRMIZI
- kaynak_tur varsayılanı 'BANKA_HAREKET' yapılırsa → MasakEntegrasyonTestleri regresyonu KIRMIZI
- Eşik operatörü >= yerine > yapılırsa → '185.000,00 sınır' KIRMIZI (A8'de aynı mutasyon M34 olarak kayıtlı)
- Ekranda MASAK alanları boşken Kaydet açık bırakılırsa → BankaEkranTestleri KIRMIZI

## Canlı doğrulama
- Yeni kiracı (BANKA-MASAK-TEST). Kur kaydını bekle. Banka hesabı aç, cari aç.
- 100.000 TL GELEN dekont → 200 (eşik altı). SQL: banka_hareketleri 1 satır, hareket_id NULL.
- Aynı cariye aynı gün 100.000 TL daha GELEN dekont, kimliksiz → 400; mesajda kümülatif 200.000,00 TL geçmeli. SQL: satır sayısı 1'de kaldı, hesap bakiyesi 100.000.
- Aynı dekontu kimlik bloğuyla gönder → 200. SQL: masak_kimlik_tespitleri'nde kaynak_tur='BANKA_HAREKET', hareket_id = dönen hareketId, islem_tutari_tl=100.000 (kümülatif değil, İŞLEMİN KENDİ tutarı).
- ÇAPRAZ KÜMÜLATİF: yeni cari aç, 100.000 TL nakit olmayan (HAVALE) satış yap, ardından aynı cariye 100.000 TL dekont gir kimliksiz → 400 (satış hareketler'den, dekont banka'dan sayıldı).
- 184.999,99 dekont → 200; 185.000,00 → 400 (sınır).
- 2.000.000,01 dekont kimlikli ama fonKaynagi yok → 400.
- Cari seçmeden 185.000 dekont → 400 (tek işlem kuralı); 100.000 → 200.
- GIDEN yönde 185.000 kimliksiz → 400.
- Mevcut bir 185.000 TL'lik GELEN hareketi faturaya dönüştür → 200 (MASAK istemez; bilinçli).
- Migration ileri/geri: Down üç kolonu ve indeksi düşürmeli, Up geri koymalı.
- Test kiracısını sil (API'yi önce durdur).

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Banka.razor`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/MasakEntegrasyonTestleri.cs`
- `dotnet/Kuyumcu.Tests/MasakBaglantiliIslemTestleri.cs`

## Mevzuat dayanağı
- MASAK Tedbirler Yönetmeliği md.5/1-(b) — 185.000 TL, işlem tutarı ya da birbiriyle bağlantılı işlemlerin toplam tutarı — **DOGRULANDI** — A7/A8, 8.5/10, iki hasım mercek. Kodda MasakKapisi.cs:114-123 olarak uygulanmış. Bu paket yeni bir eşik GETİRMEZ; eşiği görmeyen bir akışı kapıya bağlar. Yani kapı yalnız SIKILAŞIR, hiçbir yerde gevşemez.
- Bankacılık kanalıyla yapılan bir işlemde kuyumcunun kimlik tespiti yükümlülüğünün kalkıp kalkmadığı — **DOGRULANMALI** — Bankanın kendi yükümlüsü olması, kuyumcunun yükümlülüğünü kaldırır mı doğrulanmadı. Bu paket KALDIRMADIĞINI varsayarak sıkı tarafı seçiyor: havale de eşiğe tabi. Yanlışsa sonuç 'gereksiz kimlik tespiti'dir (zararsız); tersi seçilseydi sonuç 'eksik tespit' olurdu (cezalı). Fail-closed yönü bilinçlidir.
- 459 s. VUK Genel Tebliği tevsik zorunluluğu — **DOGRULANDI** — A7, 8.0/10. Banka hareketi tanımı gereği tevsik edilmiş ödemedir; bu yüzden tevsik kapısı banka yoluna BAĞLANMAZ ve nakit kümülatifine girmez. TevsikKapisi.cs:63-108'deki kümülatif tanımıyla tutarlıdır.

## Riskler
- MasakKapisi.GunlukIslemToplam bu katmanın en sıcak sorgusudur (her satışta koşar). İkinci bir tabloya sorgu eklemek maliyeti artırır; (tenant_id, cari_id, tarih) indeksi bu yüzden ZORUNLUDUR, isteğe bağlı değildir.
- MasakKapisi.Kaydet imzasına parametre eklemek 8 çağrı noktasını ilgilendirir. Varsayılan değerli opsiyonel parametre seçildi ki mevcut çağrılar DEĞİŞMESİN; ama bu 'unutma' riskini de doğurur. Karşı önlem: kaynak_tur'ün doğru yazıldığını sınayan regresyon testi (MasakEntegrasyonTestleri).
- banka_hareketleri.hareket_id bugün hep null. 'Ölü kolon' görünür ve sonraki bir oturum onu silmek isteyebilir. Kolonun XML yorumuna neden var olduğu AÇIKÇA yazılır.
- Banka ekranındaki MASAK formu, POS'takinin ikizi olur (TB-11 deseni). Ortak bir MasakFormu bileşenine çıkarmak doğru olurdu ama Islemler.razor 1390 satır ve ayıklamak paketi büyütür. Bilinçli borç: yeni bileşen yazılır ve YALNIZ banka ekranı kullanır; POS taşınmaz (izinsiz değişiklik yok).

## Açık sorular
- Bankacılık kanalıyla yapılan işlemde kuyumcunun kimlik tespiti yükümlülüğü devam ediyor mu? (Sıkı taraf seçildi; teyit isteniyor.)
- Gün kilidi (GunKapanisiKapisi) banka hareketine de uygulansın mı? Bugün uygulanmıyor; kasa kapanışı banka hesabını kapsamıyor. Ayrı karar.
- Elle girilen dekontun tarihi geçmişe verilebiliyor (BankaModulu.cs:194-197). Kümülatif o TARİHİN gününe bakar; geçmişe dekont girip bugünün haddini boşaltmak mümkün mü? (Kümülatif işlemin kendi gününe baktığı için ihlal değil, ama bilinçli olarak not düşülmeli.)
- MasakFormu ortak bileşeni bu pakette mi çıkarılsın, yoksa POS taşınmasıyla birlikte ayrı bir borç kalemi mi olsun?

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Bu oylamanın en temiz paketi: yeni kavram getirmiyor, var olan kapıyı görmeyen bir akışı kapıya bağlıyor, kapı yalnız SIKILAŞIYOR hiçbir yerde gevşemiyor. Çift saymayı bileşik SQL yerine iki ayrık kümeyle yapısal olarak imkânsız kılması, TevsikKapisi'nde aynı gerekçeyle alınmış mühürlü kararın birebir tekrarı — mevcut mekanizmanın doğru yeniden kullanımı. Sıranın başında olması doğru. Koşullarım tek noktada ciddi: kimlik tespiti satırının iki farklı tabloyu işaret etmesi, FK olmadığı için sessizce yanlış okunabilir.
  - Koşullar: MasakKapisi.Kaydet'e eklenen kaynakTur parametresi OPSİYONEL DEĞİL ZORUNLU olacak. 8 çağrı noktası derleyici zorlamasıyla tek tek elden geçirilsin (A5'te aynı desen kullanıldı ve işe yaradı). Varsayılan değerli parametre 'unutma riskini' regresyon testine devrediyor; derleyici bedava ve kesin. · HareketId'i TÜKETEN HER OKUMA YOLU TARANIP kaynak_tur'e göre dallandırılacak, sonra sevk edilecek. En az şu yüzeyler: MasakModulu'nun 12 ucu, Kuyumcu.Web/Servisler/UyumServisi.cs:117-149 (HareketId taşıyan üç record), Uyum.razor:284-406. ADR-0009 gereği FK yok, yani veritabanı bu anlam çakışmasını yakalamaz — tarama elle yapılmak zorunda. Taranmadan sevk edilmez. · Yeni indeks (tenant_id, cari_id, tarih) ZORUNLU ve migration'la BİRLİKTE gelecek — 'sonra ekleriz' denmeyecek. GunlukIslemToplam her satışta koşuyor (MasakKapisi.cs:114-128'den çağrılıyor); indekssiz ikinci sorgu kapının maliyetini iki katına çıkarır. · banka_hareketleri.hareket_id kolonunun XML yorumuna 'bugün her zaman null, ama süzgeç ilk günden var olmalı' gerekçesi AÇIKÇA yazılacak. Aksi hâlde sonraki oturum 'ölü kolon' diye silmeye kalkar. · Tevsik kapısının banka yoluna BAĞLANMAMASI kararı kod yorumuna gerekçesiyle yazılacak (banka hareketi tanımı gereği tevsik edilmiş ödemedir, nakit payı 0'dır). Yazılmazsa sonraki oturum 'unutulmuş' sanıp bağlar ve nakit kümülatifi bozulur. · FaturayaDonustur'a MASAK eklenmemesi kararı da aynı biçimde koda yazılacak (para o an el değiştirmiyor, belge üretiliyor). · MasakFormu ortak bileşeni bu pakette ÇIKARILMAYACAK (POS taşınması izinsiz değişiklik olur); yeni bileşen yazılıp YALNIZ banka ekranı kullanacak ve bu ikizlenme TB-11'e AÇIK BORÇ olarak eklenecek.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Kapıyı yalnız SIKILAŞTIRAN paket; iki fazlı desen ve kümeler-kesişmez yaklaşımı mühürlü mimariye uygun. Ancak bank dekontunun kullanıcı tarihli olması, kümülatifi bu tarihe bağlayan tasarımda eşik bölmeyi trivial kılar — bu bir mevzuat kapısı atlama yoludur; kapatılmadan sevk VETO'ya gider.
  - Koşullar: K41-1 (GERİYE TARİHLİ DEKONT = KÜMÜLATİF BYPASS — kapatılmadan sevk YOK): BankaModulu.cs:194-197 dekont tarihini KULLANICIDAN alıyor (DateTime.Parse(dto.Tarih)). Kümülatif `dto.Tarih`in gününe bakarsa, 100.000 TL'yi 'dün' tarihli, 100.000 TL'yi 'bugün' tarihli girerek 185.000 eşiği hiç tetiklenmeden aşılır — structuring'in ta kendisi ve kapı hareketler tarafında bunu yapamaz (hareketler.tarih sunucu saatidir). Zorunlu tasarım: (a) DogrulaKumulatif'e `an: now` (sunucu saati) verilir, dto.Tarih DEĞİL; (b) banka tarafı sorgusu `b.Olusturuldu >= bas && b.Olusturuldu < bit` (KAYIT ANI) ile okur, `b.Tarih` ile değil; (c) `(tenant_id, cari_id, olusturuldu)` indeksi buna göre kurulur ((tenant_id, cari_id, tarih) yerine ya da ek); (d) gelecek tarihli dekont 400; (e) test: 'dün tarihli 100.000 + bugün tarihli 100.000 aynı gün girilince ikinci 400' + mutasyon 'kümülatif dto.Tarih gününe bakarsa KIRMIZI'. Bu, spesifikasyonun 'ihlal değil' diye geçtiği açık sorunun cevabıdır: İHLALDİR. · K41-2: MasakKapisi.Kaydet'e `kaynakTur` opsiyonel parametre — KABUL; ancak varsayılan 'HAREKET' regresyon testi (MasakEntegrasyonTestleri) her 10 çağrı noktası için (SatisModulu ×8 + CariModulu ×2 — spesifikasyondaki '8' eksik sayım) kaynak_tur='HAREKET' yazıldığını doğrular. · K41-3: İki ayrık sorgu kararı KABUL (A5 deseni); `hareket_id IS NULL` süzgeci ilk günden — KABUL. Tevsik kapısının banka yoluna bağlanmaması KABUL (banka = tevsik kanalı, 459 s. Tebliğ DOGRULANDI). · K41-4: Faturaya dönüştürme MASAK dışı — KABUL, ancak gerekçe yorumu koda yazılır (spesifikasyonda var).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Kapının banka akışında hiç olmadığı doğrulandı (BankaModulu.cs:180-223'te masak 0 isabet; GunlukIslemToplam yalnız hareketler, MasakKapisi.cs:137-142). İki ayrık sorgu deseni doğru. Ama 'an' parametresi yanlış seçilmiş, bir mutasyon kanıtı yapısal olarak yanlış ve ekran maddesi mevcut MasakFormu'nu görmemiş.
  - Koşullar: 'MASAK formu POS'takinin ikizi olur; yeni bileşen yazılır' maddesi BAYAT: Kuyumcu.Web/Bilesenler/MasakFormu.razor ZATEN VAR ve Islemler.razor:559'da kullanılıyor (Model/Tutar/Esik/Gereksinim parametreleri). Banka.razor bu bileşeni KULLANIR, ikinci form yazılmaz; BankaEkranTestleri MasakFormu'nun kademe alanlarını sınar (KarmaOdemeEkranTestleri/Islemler deseni). TB-11 'ikiz kural' borcu doğmaz. · DogrulaKumulatif'e 'now' DEĞİL dekontun 'tarih'i verilir: kümülatif h.Tarih penceresinden okunur (MasakKapisi.cs:134-142); dto.Tarih geçmişe verilebiliyor (BankaModulu.cs:194-197). now verilirse geçmiş güne yazılan dekont bugünün penceresiyle ölçülür, sonraki aynı-gün dekont ise geçmiş günü okur → aynı gün 2×100.000 kimliksiz geçer. Test: geçmiş tarihli 100.000 + aynı geçmiş tarihe 100.000 → 400. · Mutasyon 'Dogrula transaction'ın İÇİNE alınırsa → hiçbir satır yazılmadı testi KIRMIZI' YANLIŞ: BeginTransactionAsync sonrası fırlayan istisna tx'i dispose'da geri alır, satır yine yazılmaz, test YEŞİL kalır. Listeden çıkarılır; yerine yakalanabilir mutasyon: 'Kaydet SaveChangesAsync'ten SONRA çağrılırsa tespit satırı yazılmaz' → 'kaynak_tur=BANKA_HAREKET tespit satırı' testi KIRMIZI. · MasakEntegrasyonTestleri regresyonu: mevcut 8 akışın tespit satırında kaynak_tur='HAREKET' assert'i EKLENİR (dosya ayak izinde var, içerik belirtilmeli); mutasyon 'varsayılan BANKA_HAREKET' ancak böyle yakalanır. · Yabancı kiracının carisiyle dekont → 404' çok katmanlı koruma: BankaModulu.cs:184-189 elle TenantId şartı zaten var; mutasyon kanıtı için filtresiz bağlam, yoksa regresyon etiketi. · MasakKimlikTespiti.HareketId'ye banka kimliği yazılabilir: FK/navigation YOK (KuyumcuDbContext'te HasOne 0 isabet, MasakVarliklari.cs:51 düz string) — doğrulandı, ek koşul yok; ama XML yorumu güncellenir. · BankaHareket.Tutar Range(0.01) ile pozitif (BankaModulu.cs:60-61); banka kümesi SumAsync'te Abs gereksiz — yine de eklenirse zararsız; test 'GIDEN yönde de eşik' bunu örter. · Migration ileri/geri canlı adımı ölçülebilir: 'SHOW COLUMNS' ile 3 kolon + 'SHOW INDEX' ile yeni indeks Down sonrası yok/Up sonrası var.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO: Mehmet ŞARTLI VETO (K41-1). Kümülatif MASAK ölçümü kullanıcı girdisi dekont tarihine (BankaModulu.cs:194-197 DateTime.Parse(dto.Tarih)) bağlanırsa eşik bölme yolu açılır; kapatılmadan sevk YOK.
- ÇELİŞKİ ÇÖZÜMÜ (Aşkın 'dekont tarihi' vs Mehmet 'sunucu saati'): Mehmet'in vetosu bağlayıcı → DogrulaKumulatif'e an: now verilir, banka sorgusu b.Olusturuldu >= bas && < bit ile okunur, b.Tarih ile DEĞİL; gelecek tarihli dekont 400.
- İndeks ZORUNLU ve migration ile BİRLİKTE: (tenant_id, cari_id, olusturuldu) — Bertan'ın (tenant_id, cari_id, tarih) önerisi K41-1 uyarınca olusturuldu eksenine taşındı. GunlukIslemToplam her satışta koşar (MasakKapisi.cs:114-128).
- Test + mutasyon: 'dün tarihli 100.000 + bugün tarihli 100.000 aynı gün girilince ikinci istek 400' testi; mutasyon 'kümülatif dto.Tarih gününe bakarsa KIRMIZI'. Bu kanıt olmadan paket kapanmaz.
- kaynakTur parametresi ZORUNLU (Bertan, derleyici zorlaması) + Mehmet'in K41-2 regresyonu: 10 çağrı noktası (SatisModulu x8 + CariModulu x2; spesifikasyondaki '8' eksik sayım) kaynak_tur='HAREKET' assert'i ile MasakEntegrasyonTestleri'nde sınanır.
- HareketId'i TÜKETEN HER OKUMA YOLU taranıp kaynak_tur'e göre dallandırılmadan sevk edilmez: MasakModulu'nun 12 ucu, Kuyumcu.Web/Servisler/UyumServisi.cs:117-149, Uyum.razor:284-406. ADR-0009 gereği FK yok, DB yakalamaz.
- İki ayrık sorgu (A5 deseni) + hareket_id IS NULL süzgeci ilk günden KABUL. banka_hareketleri.hareket_id XML yorumuna 'bugün hep null, süzgeç ilk günden var olmalı' gerekçesi yazılır (ölü kolon sanılıp silinmesin).
- Tevsik kapısının banka yoluna BAĞLANMAMASI ve FaturayaDonustur'a MASAK eklenmemesi kararları gerekçesiyle koda yorum olarak yazılır (banka = tevsik kanalı, 459 s. Tebliğ; faturada para el değiştirmiyor).
- BAYAT MADDE DÜZELTMESİ (Aşkın): 'MasakFormu yeni yazılacak' yanlış — Bilesenler/MasakFormu.razor ZATEN VAR ve Islemler.razor:559'da kullanılıyor; banka ekranı mevcut bileşeni kullanır, POS taşınmaz, ikizlenme yaratılmaz.
- Migration ileri/geri canlı adımı ölçülür: Up sonrası SHOW COLUMNS 3 kolon + SHOW INDEX yeni indeks var, Down sonrası yok. Yabancı kiracı carisiyle dekont 404 (BankaModulu.cs:184-189 elle TenantId şartı) regresyon etiketiyle korunur.
