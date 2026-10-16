# Paket T-3 — Yedek otomasyonu ve doğrulanmış geri yükleme (docker'sız)

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: 1,5–2 gün (Faz A 1–1,5 · Faz B 0,5) — mühürlü takvimdeki 'T-3 1,5-2 gün' ile birebir (MIMARI-KALAN-PAKETLER-2026-09-15.md:707) gün · Önkoşullar: T-1 + F-6 (üretim yayın paketi: systemd servis düzeni, kuyumcu kullanıcısı, appsettings.Production, TLS, sır üretimi) — timer unit'leri bu düzene yaslanır, ikinci yayın yolu açılmaz., Paket 4 belge deposu (belgeler LONGBLOB) — yedek boyutu/süresi bundan etkilenir; kiracı başına 500 MB tavanı izlenmeli (MIMARI-KALAN-PAKETLER-2026-09-15.md:161)., UX-1-A / DuzeltmePolitikasi.cs (Dalga 1'de bitti) — yeni varlıkların politika damgaları bu haritaya yazılır., UX-3-A ArayuzModu + BasitMenu (Dalga 1'de bitti) — /yedekler profesyonel mod menüsüne eklenir. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Yedeği iddia olmaktan çıkarıp kanıta çevirmek: docker'sız mysqldump, systemd timer ile gecelik tam + 6 saatlik artımlı, AES-256 şifreli dış kopya, 30 gün saklama, uygulama içi 'Yedekler' ekranı (son yedek/boyut/doğrulama) ve aylık otomatik geri-yükleme tatbikat raporu.

## Kodda bugünkü durum
Tek varlık dotnet/yedek-al-ve-dogrula.sh (111 satır): elle çalışır, `docker exec kuyumcu-mysql` zorunlu (:39, :83), şifresiz düz .sql /tmp'e yazar (:35), saklama/dış kopya/zamanlayıcı yok, parola betikte gömülü varsayılan (:30). Uygulamada yedek varlığı/ucu/ekranı YOK (Modules/ altında yedek modülü yok, Pages/ 27 rotada /yedekler yok). deploy/ yalnız ecosystem.config.js, iis-web.config, nginx-kuyumcu.conf — systemd unit yok.

## Kapsam
**İçinde:**
- Docker'sız mysqldump yolu: --single-transaction --routines --triggers --events; kimlik --defaults-extra-file ile (parola argv'de GEÇMEZ).
- systemd: kuyumcu-yedek-tam.timer (gecelik 03:15), -artimli.timer (6 saat, binlog kopyası), -tatbikat.timer (aylık), -talep.timer (dakikalık elle talep kuyruğu).
- AES-256 şifreleme + dış kopya: yerel /var/lib/kuyumcu/yedek + dış hedef (yapılandırmadan); şifresiz ara dosya diske DÜŞMEZ (akış içinde şifrelenir).
- 30 gün yaş bazlı saklama + 'son 30 günde DOĞRULANMIŞ en az bir tam yedek yoksa silme yok' kuralı.
- Her koşu manifesti DB'ye yazılır (yedek_kayitlari: durum, boyut, sha256, doğrulama, dış kopya).
- Uygulama içi /yedekler ekranı (SAHIP): son yedek zamanı/boyutu/doğrulama, RPO-RTO kartı, koşu listesi, tatbikat raporları, gerekçeli 'Elle yedek al'.
- Elle yedek TALEBİ kuyruğu: API süreç başlatmaz, satır yazar; betik kuyruğu tüketir.
- Aylık tatbikat: mevcut betiğin CHECKSUM TABLE karşılaştırması ayrı şemaya geri yükleyerek koşar, sonuç yedek_tatbikatlari'na yazılır.
- denetim.sh'a T-3 sayaçları (API'de Process.Start yasağı, betikte şifresiz .sql yazımı yasağı).
- deploy/YEDEK-GERI-YUKLEME.md runbook'u (RPO ≤ 6 saat, RTO ≤ 2 saat).

**Dışında:**
- Uygulama içinden GERİ YÜKLEME tetikleme (yıkıcı; yalnız sunucuda runbook + betik ile insan eliyle).
- Yedek dosyasını uygulamadan indirme ucu — dosya TÜM kiracıların verisini taşır, indirme yüzeyi açılmaz.
- Kiracı bazlı kısmi yedek / kiracı geri yükleme — veri taşınabilirliği DisaAktarimModulu'nda, ikinci yol açılmaz.
- Point-in-time recovery aracı/UI (binlog kopyası alınır, PITR prosedürü 1.1'e).
- Çoklu bölge / replica / failover (F serisi kapsamı).
- Nesne depolama sağlayıcı aboneliği ve bedeli (Kurucu kalemi).

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| yedek_kayitlari | YENİ — sistem düzeyi, kiracısız (Tenants kalıbı); yazma yalnız SistemKapsami.Sistem() ya da betik üzerinden | id(36), tur(TAM|ARTIMLI|ELLE), baslangic, bitis, durum(SURUYOR|BASARILI|BASARISIZ), dosya_adi(200), boyut_bayt, sha256(64), sifreli(bool), dis_kopya(YOK|KOPYALANDI|HATA), dogrulama(BEKLIYOR|DOGRULANDI|UYUSMADI), dogrulama_tarih, tablo_sayisi, hata_ozeti(500) | ix_yedek_kayitlari_baslangic(baslangic DESC); ix_yedek_kayitlari_tur_baslangic(tur, baslangic DESC) | YedekOtomasyonu |
| yedek_talepleri | YENİ — IKiraciVarlik (elle yedek talebi; kim istedi izlenir, HasQueryFilter uygulanır) | id(36), tenant_id(36), kullanici_id(36), gerekce(500, min 20), durum(BEKLIYOR|ISLENDI|IPTAL|HATA), olusturuldu, islendi, yedek_kaydi_id(36, null), iptal_gerekce(500) | ix_yedek_talepleri_tenant_durum(tenant_id, durum, olusturuldu DESC) — kuyruk taraması bu indeksi kullanır | YedekOtomasyonu |
| yedek_tatbikatlari | YENİ — sistem düzeyi; aylık geri-yükleme provasının kanıtı | id(36), tarih, yedek_kaydi_id(36), tablo_sayisi, uyusan, uyusmayan, sonuc(BASARILI|BASARISIZ), sure_sn, rapor(4000: yalnız uyuşmayan tablo adları, VERİ DEĞERİ YAZILMAZ) | ix_yedek_tatbikatlari_tarih(tarih DESC) | YedekOtomasyonu |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/yedekler/ozet` | SAHIP | Son tam yedek zamanı/boyutu/sha, doğrulama durumu, dış kopya durumu, saklanan koşu sayısı, RPO (son artımlıdan bu yana dk) ve RTO taahhüdü. | [Authorize] + [RolGerekli("SAHIP")] sınıf düzeyi (AdminController:71 kalıbı) + DenetimServisi.ListeOkumaKaydet("YedekKaydi") |
| GET | `/api/v1/yedekler?limit=50` | SAHIP | Koşu listesi (tur, durum, boyut, doğrulama). Sınır 50 ve daraltma yolu ekranda yazılır (UX-S29). | [RolGerekli("SAHIP")] |
| GET | `/api/v1/yedekler/tatbikatlar` | SAHIP | Aylık geri-yükleme tatbikat raporları: uyuşan/uyuşmayan tablo sayısı, süre, sonuç. | [RolGerekli("SAHIP")] |
| POST | `/api/v1/yedekler/talep` | SAHIP | Elle yedek TALEBİ oluşturur (201). Gerekçe ≥20 karakter. Süreç başlatmaz; betik kuyruğu 60 sn içinde tüketir. Kiracı başına aynı anda 1 bekleyen talep, ikincisi 429. | [RolGerekli("SAHIP")] + [Required, MinLength(20)] + DenetimServisi.YazmaKaydet(hassas:true) |
| PATCH | `/api/v1/yedekler/talep/{id}` | SAHIP | Bekleyen talebi İPTAL eder (pasife alma; hard DELETE yok — DoD). İşlenmiş talep 400 döner. | [RolGerekli("SAHIP")] + iptal_gerekce ≥20 + denetim kaydı |
| GET | `/api/v1/meta/duzeltme-politikalari` | Oturumlu | MEVCUT uç genişletilir: YedekKaydi/YedekTatbikati = SALT_OKUNUR (GerekceAnahtarlari.Sistem), YedekTalebi = DUZENLENEBILIR (HedefUc: PATCH /yedekler/talep/{id}). | Değişmez — DuzeltmePolitikasi.cs haritasına 3 satır |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Yedek/YedekModulu.cs` — DTO + YedekServisi + YedekController tek dosya (modül kalıbı). Servis yalnız DB okur/yazar; Process.Start, dosya sistemi ve mysqldump ÇAĞIRMAZ.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs` — YedekKaydi (kiracısız), YedekTalebi : IKiraciVarlik, YedekTatbikati eklenir.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — 3 DbSet + indeksler; YedekTalebi kiracı filtresine girer, diğer ikisi Tenants ile aynı muafiyette kalır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — s.AddScoped<Yedek.YedekServisi>(); tek satır.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` — 3 politika satırı (yeni gerekçe anahtarı gerekmez, Sistem mevcut).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/betikler/yedek.sh` — Tek betik, dört mod: tam | artimli | talep | tatbikat. mysqldump akışı -> gzip -> AES-256 şifreleme (pass file) -> sha256 -> dış kopya -> yedek_kayitlari INSERT. Şifresiz ara dosya yok.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/yedek-al-ve-dogrula.sh` — Docker bağımlılığı kaldırılır: KUYUMCU_KAP boşsa my() ve dump doğrudan mysql/mysqldump kullanır. İkinci doğrulama betiği YAZILMAZ; tatbikat modu bunu çağırır. :30 gömülü parola kaldırılır.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/systemd/` — kuyumcu-yedek-tam / -artimli / -tatbikat / -talep .service+.timer. User=kuyumcu, NoNewPrivileges, ProtectSystem=strict, OnCalendar Europe/Istanbul.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/YEDEK-GERI-YUKLEME.md` — Runbook: dosya çözme, ayrı şemaya yükleme, doğrulama, üretime alma, RPO ≤ 6 saat / RTO ≤ 2 saat taahhüdü, yetkili kişi.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` — İki sayaç: Kuyumcu.Api altında Process.Start/ProcessStartInfo = 0; yedek.sh içinde mysqldump çıktısının şifrelenmeden '> *.sql' yönlendirilmemesi.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json` — Yedek: { Dizin, DisHedef, SaklamaGun=30, RpoDakika=360, RtoDakika=120, AnahtarDosyasi }. SIR DEĞİL, YOL tutulur; değerler Math.Clamp ile sıkıştırılır (KurZamanlayici kalıbı).

## Ekranlar
- `/yedekler` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Yedekler.razor` — 4 StatKart: Son yedek (zaman+boyut), Doğrulama (yeşil/kırmızı Rozet), Dış kopya, RPO/RTO. Altta koşu tablosu, tatbikat raporları ve gerekçeli 'Elle yedek al' formu (GerekceAlani, #yd-gerekce). Politika ipuçları sunucudan (DegismezIpucu ile SALT_OKUNUR gerekçesi). (menü/yetki: KenarCubugu.razor ~:112 bloğunda RolluGorunur("/yedekler", "SAHIP"); BasitMenu'ye EKLENMEZ (yalnız profesyonel mod).)

## İş kuralları (fail-closed)
- FAIL-CLOSED 1: CHECKSUM karşılaştırması uyuşmazsa koşu BASARISIZ yazılır, dosya saklama temizliğinden muaf tutulur (silinmez), betik çıkışı ≠ 0, ekranda kırmızı bant.
- FAIL-CLOSED 2: Şifreleme anahtarı yok/boş/32 bayttan kısa ise yedek ALINMAZ — şifresiz yedek üretmek yasak (AlanSifreleme.cs:137 sertliği).
- FAIL-CLOSED 3: Dış kopya başarısızsa koşu BASARISIZ sayılır; tek kopya yedek, yedek değildir (Mühür 2, Mehmet).
- FAIL-CLOSED 4: Son 30 günde DOĞRULANMIŞ tam yedek yoksa saklama temizliği hiçbir dosyayı silmez.
- API hiçbir koşulda süreç/komut çalıştırmaz; elle yedek yalnız kuyruk satırıdır (denetim.sh sayacı zorlar).
- Yedek dosyası uygulamadan indirilemez; ekran yalnız üstveri gösterir.
- Elle yedek gerekçesiz açılamaz (≥20, sunucuda [Required, MinLength]); kilitli düğme sebebini yazar (UX-S30).
- Kiracı başına aynı anda 1 bekleyen talep; ikincisi 429 + Türkçe mesaj.
- Tatbikat kaynak şemaya ASLA yazmaz; ayrı şemaya yükler ve iş bitince düşürür (mevcut betik sözleşmesi korunur).
- UX-S27: talep POST'u başarı bandıyla biter, dönen talep no yazılır. UX-S28: hata boş listeye çevrilmez, hata bandı ayrıdır.
- Denetim: liste okuması ListeOkumaKaydet, talep açma/iptal YazmaKaydet ile iz bırakır.
- Saat dilimi Europe/Istanbul; zamanlama tek yerde (systemd), uygulamada ikinci zamanlayıcı açılmaz.

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YedekTestleri.cs` [DB (gerçek MySQL)]
  - ElleTalep_Acilinca_BekliyorDurumundaKaydedilir
  - AyniKiracidaIkinciBekleyenTalep_Reddedilir
  - Talep_IptalEdilince_HardDeleteOlmaz_DurumIPTAL
  - IslenmisTalep_IptalEdilemez
  - DisKopyaHatasi_KosuBASARISIZ
  - Saklama_SonDogrulanmisYedekYoksa_HicbirKayitSilinmez
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YedekHttpTestleri.cs` [HTTP (WebApplicationFactory)]
  - Ozet_SahipOlmayan_403
  - Ozet_Sahip_200
  - TalepAc_GerekceKisa_400
  - TalepAc_Gecerli_201_DenetimKaydiOlusur
  - TalepAc_IkinciKez_429
  - YedekDosyasiIndirmeUcu_YOK_404
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YedekKiraciTestleri.cs` [DB (izolasyon)]
  - BaskaKiracininTalebi_Listede_Gorunmez
  - YedekTalebi_TenantId_KiraciYazmaDenetcisiTarafindanZorlanir
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs` [bUnit]
  - Yedekler_SonYedekBoyutVeDogrulamaDurumunuGosterir
  - DogrulanmamisYedek_KirmiziRozetVeUyariBandi
  - Gerekce_YirmiKarakterAltinda_DugmeDisabled_SebepYazili
  - Hata_BosDurumaCevrilmez_HataBandiGorunur
  - Menu_SahipDisindaGorunmez_BasitModdaGizli
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/YedekPolitikaTestleri.cs` [Saf]
  - YedekKaydi_SALT_OKUNUR_GerekceAnahtariSozluklerdeVar
  - YedekTalebi_DUZENLENEBILIR_HedefUcDolu
  - RpoRto_Yapilandirmadan_Okunur_VarsayilanKlemplenir

## Mutasyon kanıtları
- Dış kopya hatası BASARILI sayılırsa → YedekTestleri.DisKopyaHatasi_KosuBASARISIZ KIRILIR.
- Saklamadaki 'doğrulanmış yedek var mı' koşulu silinirse → Saklama_SonDogrulanmisYedekYoksa_HicbirKayitSilinmez KIRILIR.
- PATCH iptali hard DELETE'e çevrilirse → Talep_IptalEdilince_HardDeleteOlmaz_DurumIPTAL KIRILIR.
- [RolGerekli("SAHIP")] kaldırılırsa → YedekHttpTestleri.Ozet_SahipOlmayan_403 KIRILIR.
- Hız sınırı kaldırılırsa → TalepAc_IkinciKez_429 KIRILIR.
- MinLength(20) → MinLength(0) → TalepAc_GerekceKisa_400 KIRILIR.
- YedekTalebi'nin kiracı filtresi kaldırılırsa → BaskaKiracininTalebi_Listede_Gorunmez KIRILIR.
- Denetim kaydı satırı silinirse → TalepAc_Gecerli_201_DenetimKaydiOlusur KIRILIR.
- Ekrandaki doğrulama rozeti sabit yeşile çevrilirse → DogrulanmamisYedek_KirmiziRozetVeUyariBandi KIRILIR.
- Bir yedek dosyası indirme ucu eklenirse → YedekDosyasiIndirmeUcu_YOK_404 KIRILIR.
- API'ye Process.Start eklenirse → denetim.sh yeni sayacı çıkış 1 verir (kod kapısı kapanır).
- yedek.sh'te şifreleme adımı atlanıp düz .sql yazılırsa → denetim.sh betik sayacı + 'anahtar boşken çıkış ≠ 0' canlı doğrulaması KIRILIR.

## Canlı doğrulama
- `bash dotnet/betikler/yedek.sh tam` → çıkış 0; ls -l $YEDEK_DIZIN/*.enc dosya var; `file` çıktısı 'data' (düz SQL değil); dizinde *.sql YOK.
- SQL: SELECT tur,durum,boyut_bayt,sifreli,dis_kopya,dogrulama FROM yedek_kayitlari ORDER BY baslangic DESC LIMIT 3; → son satır TAM/BASARILI/sifreli=1/dis_kopya=KOPYALANDI.
- `bash dotnet/betikler/yedek.sh tatbikat` → çıkış 0; SELECT uyusan,uyusmayan,sonuc FROM yedek_tatbikatlari ORDER BY tarih DESC LIMIT 1; → uyusmayan=0, sonuc=BASARILI.
- GET http://localhost:5198/api/v1/yedekler/ozet (SAHIP jetonu) → 200, gövdede sonYedek/dogrulama/rpoDakika dolu.
- Aynı uç TEZGAHTAR jetonuyla → 403.
- POST /api/v1/yedekler/talep {gerekce:'aylık denetim öncesi elle yedek'} → 201; 60 sn sonra SELECT durum,yedek_kaydi_id FROM yedek_talepleri ORDER BY olusturuldu DESC LIMIT 1; → ISLENDI + dolu id.
- Aynı talebi tekrar POST → 429.
- PATCH /api/v1/yedekler/talep/{bekleyenId} {iptalGerekce:'yanlışlıkla açıldı, iptal edildi'} → 200; SQL durum=IPTAL ve COUNT(*) değişmemiş (silinmedi).
- SELECT COUNT(*) FROM denetim_kayitlari WHERE varlik_turu='YedekTalebi' AND eylem='EKLEME'; → açılan talep sayısıyla eşit.
- Anahtar dosyasını boşaltıp `yedek.sh tam` → çıkış ≠ 0, Türkçe hata, hiçbir dosya üretilmemiş (fail-closed kanıtı).
- Tarayıcı: http://localhost:5199/yedekler SAHIP ile açılır; son yedek zamanı/boyutu görünür; gerekçe 5 karakterken düğme disabled ve sebep yazılı.
- Saklama: sahte 40 günlük kayıt+dosya ekleyip `yedek.sh tam` → eski dosya silinir, doğrulanmamış son yedek durur (SELECT ile teyit).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Yedek/YedekModulu.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json ve appsettings.Development.json (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/<damga>_YedekOtomasyonu.cs (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Yedekler.razor (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor (DEGISTIR, ~:112 bloğu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs ve Sozlukler (DEGISTIR: TR/EN anahtarlar)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/betikler/yedek.sh (YENİ) · dotnet/yedek-al-ve-dogrula.sh (DEGISTIR) · dotnet/denetim.sh (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/systemd/*.service|*.timer (YENİ) · /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/YEDEK-GERI-YUKLEME.md (YENİ)`

## Fazlar
- **Faz A — sevk edilebilir asgari (Mühür 2 asgari 1.0)** (Dalga 3, T-1+F-6 hemen sonrası): Docker'sız yedek.sh (tam + talep modu, AES-256 akış şifreleme, sha256, dış kopya, 30 gün saklama) · gecelik + dakikalık talep timer'ı · 3 tablo + migration · GET ozet/liste, POST+PATCH talep · /yedekler ekranı (son yedek, boyut, doğrulama, elle yedek) · yedek-al-ve-dogrula.sh docker bağımlılığının ve gömülü parolanın kaldırılması · denetim.sh sayaçları · runbook. Bu haliyle Mehmet'in şifreli+dış kopya şartı ve Aşkın'ın gerçek geri yükleme provası karşılanır.
- **Faz B — derinleştirme** (Faz A kapısı geçtikten sonra, aynı dalga içinde): 6 saatlik artımlı (binlog) kopyası ve RPO ölçümünün canlılaştırılması · aylık otomatik tatbikat timer'ı + tatbikat raporlarının ekranda listelenmesi · tatbikat başarısızlığında Panel'de uyarı rozeti · yedek boyutu/süresi trend kartı (belge deposu büyümesi izlenir).
## Mevzuat dayanağı
- VUK md.253 ve TTK md.82 — defter/belge saklama süreleri (VUK 5 yıl, TTK 10 yıl) — **YÜKSEK (genel kural)** — 30 gün YEDEK saklamasıdır, KAYIT saklaması değildir: veri üretimde yasal süre boyunca durur, yedek dönüşümlü 30 gündür. Bu ayrım runbook'a ve satış belgesine yazılmalı; müşteriye '30 gün veri saklıyoruz' denmez.
- KVKK md.12/1 — veri güvenliğine ilişkin teknik ve idari tedbirler — **YÜKSEK** — Yedek kişisel veri taşır: şifreli olması, indirme ucunun bulunmaması ve erişimin denetim izine yazılması bu maddenin doğrudan karşılığıdır (DenetimIzi.cs:1-25 gerekçesiyle aynı hat).
- KVKK Kişisel Veri Güvenliği Rehberi — yedeklerin ana sistemden ayrı ortamda ve şifreli tutulması — **ORTA (rehber; denetimde ölçüt, madde metni değil)** — Dış kopya zorunluluğunun dayanağı; güncel sürüm ve tam ifade DOĞRULANMALI.
- e-Defter/e-Belge ikincil kopya (saklayıcı kuruluş) yükümlülüğü — **DÜŞÜK — bu pakette KAPSAM DIŞI, DOĞRULANMALI** — 38-B/E-BELGE hattı açılırsa GİB'in ikincil kopya şartı ayrı değerlendirilir; bu paketin yedeği onun yerine geçmez.

## Riskler
- Şifreleme anahtarının kaybı = tüm yedeklerin kaybı. Anahtarın sunucu dışı kopyası Kurucu kalemidir; runbook'ta yazılı olmalı, koda gömülmez.
- yedek-al-ve-dogrula.sh:30'da GÖMÜLÜ VARSAYILAN PAROLA var; docker'sızlaştırma sırasında kaldırılmazsa üretim parolası betikte kalır (Mehmet veto riski).
- Sistem düzeyi (kiracısız) tablolar denetim.sh kiracı sayaçlarını tetikleyebilir; muafiyet Tenants kalıbıyla birebir yazılmazsa kapı yanlış yerden kapanır.
- Belge deposu (LONGBLOB) büyüdükçe dump süresi/boyutu sessizce artar; 500 MB/kiracı tavanı izlenmezse gece penceresi taşar.
- mysqldump --single-transaction yalnız InnoDB'de tutarlıdır; şemada MyISAM tablo varsa tutarsız yedek üretir — information_schema ENGINE taraması Faz A'da koşmalı (DOĞRULANMALI).
- Dış hedef kimlik bilgisi yeni bir sır yüzeyidir; T-1'in sır mekanizmasına bağlanmazsa ikinci sır yolu doğar.
- systemd timer'lar yalnız Linux üretimde koşar; macOS'ta doğrulanamaz — 'timer çalışıyor' iddiası ancak üretimde kanıtlanır, geliştirmede betik elle çağrılır.
- Tatbikat geri yüklemesi üretim sunucusunda CPU/IO yükü yaratır; pencere dışına taşarsa satış saatlerini etkiler.

## Açık sorular
- Dış kopya hedefi: ikinci disk/NAS mı, S3 uyumlu nesne depolama mı? Bedel Kurucu kalemi; varsayılan ekip oyuyla seçilmeli.
- Şifreleme anahtarı nerede tutulacak (sunucuda chmod 600 dosya + işletme sahibinde ayrı kopya)? Kayıp senaryosunun yazılı kabulü gerekiyor.
- RPO 6 saat / RTO 2 saat taahhüdü S-1 ticari paketine girecek mi? Girerse sözleşme metni Kurucu imzası ister.
- Elle yedekte 60 sn kuyruk gecikmesi kabul mü, yoksa anlık koşu mu bekleniyor (API'den süreç başlatmama kararıyla çelişir)?
- Çok kiracılı kurulumda bir kiracının SAHİP'i tüm örneği kapsayan yedek üstverisini görmeli mi? Öneri: zaman/boyut/durum görünür, dosya adı ve yol gizlenir — oylanmalı.
- Üretimde MySQL sürümü 8.4 mü (docker-compose.yml:3 8.4 diyor) ve sunucudaki mysqldump istemci sürümü uyumlu mu? DOĞRULANMALI.

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — API'nin süreç başlatmaması, elle yedeğin kuyruk satırına indirgenmesi ve indirme ucunun hiç açılmaması mimari olarak en doğru kararlar; yedek dosyası tüm kiracıların verisini taşıdığı için bu yüzeyin kapalı kalması şart. Üç tablo (kiracısız kayıt + kiracılı talep + tatbikat) veri modeli olarak tutarlı. Koşullar betik konumu, çift zamanlayıcı ve T-1 ile çakışan dosyalarla ilgili.
  - Koşullar: Betik konumu tek olsun: mevcut betikler dotnet/ kökünde (dotnet/yedek-al-ve-dogrula.sh, dotnet/denetim.sh). Yeni dotnet/betikler/ dizini ikinci konum yaratır; ya hepsi taşınsın ya yedek.sh de dotnet/ kökünde dursun. · yedek-al-ve-dogrula.sh'in docker'sızlaştırılması ile yeni yedek.sh arasında ikiz dump yolu doğmasın: dump+doğrulama tek fonksiyon kümesinde toplansın, yedek.sh onu çağırsın (tasarım bunu söylüyor, denetim.sh sayacıyla zorlansın). · systemd dizini T-1 ile ortak kararlaştırılsın (bkz. T-1 koşulu); timer'lar T-1'in kuyumcu kullanıcısı/EnvironmentFile düzenine yaslanmalı, kendi ortam düzenini kurmamalı. · Şifreleme anahtarı KUYUMCU_ALAN_ANAHTARI ile AYNI anahtar olmasın; yedek anahtarı ayrı dosyadan okunsun — alan şifreleme anahtarının yedekle aynı yerde durması tek nokta çöküşü yaratır (AlanSifreleme.cs:114-121 ortam değişkeni yolu). · appsettings.json'a Yedek bloğu eklenirken değerler Math.Clamp ile sıkıştırılsın ve appsettings.Production şablonuyla çelişmesin (T-1 şablonu sırsız; Yedek yalnız YOL tutuyor, bu doğru). · yedek_kayitlari/yedek_tatbikatlari kiracısız tablolar HasQueryFilter muafiyetini Tenants kalıbıyla birebir aynı yerden alsın; KiraciYazmaDenetcisi'ne istisna eklenmesi gerekiyorsa bu denetim.sh'ta sayılsın.
- **YENİ: GÜVENLİK (Mehmet) — kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek şifreleme, içe aktarım riski, abonelik/plan kapılarının yazma yolunu bozmaması, telemetri/hata paketinde kişisel veri, MFA koşulu.: EVET** — Şifresiz düz .sql üretimi (mevcut betikte :35 civarı), docker bağımlılığı ve gömülü parola gerçek kod incelemesiyle doğrulandı; T-3 bunların hepsini kapatıyor ve en önemlisi 'tek kopya yedek, yedek değildir' ilkesini dış kopya zorunluluğuyla operasyonelleştiriyor. API'nin Process.Start çağırmaması (yalnız kuyruk satırı yazması) ayrı bir saldırı yüzeyini (uzaktan komut enjeksiyonu) kapatan isabetli bir mimari karar.
  - Koşullar: docker-compose.yml:7 (düz metin MYSQL_ROOT_PASSWORD) ve mevcut yedek-al-ve-dogrula.sh'ın docker'a bağımlılığı doğrulandı; yeni betikte parolanın argv'de GEÇMEMESİ (--defaults-extra-file) test edilebilir kanıtla (ps aux sırasında görünmemeli) doğrulanmalı, yalnız tasarım metninde kalmamalı. · Şifreleme anahtarının yedek dosyasıyla AYNI diskte/dizinde durmaması şart — tasarımda 'AnahtarDosyasi' appsettings'te yol olarak tutuluyor ama fiziksel ayrı depolama (ör. ayrı disk/HSM) net değil; en azından yedek hedefinden farklı bir dizin/izin seti zorunlu kılınmalı. · Dış kopya hedefinin kimlik bilgileri (S3/sftp vb.) nerede saklanacağı belirtilmemiş — bu sır de appsettings'e düz yazılırsa T-1'in fail-closed felsefesiyle çelişir; aynı ortam değişkeni disiplinine tabi olmalı. · Tatbikat raporunun 'yalnız tablo adı, veri değeri yazılmaz' kuralı testte (YedekTestleri) somut olarak doğrulanmalı — mutasyon kanıtı listesinde bu senaryo eksik, eklenmeli. · yedek_talepleri kiracı filtresi (IKiraciVarlik) ile yedek_kayitlari/yedek_tatbikatlari'nın kiracısız (sistem düzeyi) olması arasındaki sınır net: SAHIP rolü başka kiracının TALEBİNİ göremez ama tüm kiracıları etkileyen sistem yedeğinin ÖZETİNİ görüyor — bu tasarım gereği kabul edilebilir, ancak ekranda 'bu, tüm kiracıları kapsayan sistem yedeğidir' ibaresi UX-S20 benzeri bir uyarı olarak eklenmeli.
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: KOSULLU** — Docker'sız mysqldump geçişi ve şifreleme+dış kopya fail-closed kuralları gerçek koddaki bugünkü zafiyeti (yedek-al-ve-dogrula.sh:36-76 docker exec, appsettings gömülü parola) doğru teşhis ediyor; canlı doğrulamada 'file' komutuyla gerçekten şifrelendiğinin kanıtlanması ('*.sql YOK, *.enc VAR') teatral olmayan somut bir kontrol. Ancak metnin eksik/kesik ulaşması ve iki test kapsamı boşluğu nedeniyle tam EVET veremiyorum.
  - Koşullar: Bana ulaşan tasarım metni 'mevzuat' bölümünde yarım cümlede kesiliyor; riskler, açık_sorular ve tahmin_gün bölümleri görülemedi — tam metin olmadan nihai onay verilemez, yalnız görülen kısım üzerinden koşullu oy veriyorum. · Görünen canlı_dogrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi ve komut çıktısıyla kanıtlandı' kapanış adımı yok; eklenmeli. · Aylık tatbikatın ayrı şemayı gerçekten düşürdüğünü (kaynak şemaya yazmadığını) doğrulayan ayrı bir test/mutasyon kanıtı yok — yalnız uyusan/uyusmayan tablo sayıları test ediliyor, temizlik davranışı test edilmiyor. · YedekKiraciTestleri.cs yalnız 2 senaryoyla kiracı izolasyonunu kapsıyor; kuyruk tablosu (yedek_talepleri) kiracılar-arası sızıntıya karşı en az bir de HTTP seviyesinde çapraz-kiracı 404/403 testiyle desteklenmeli (yalnız DB seviyesinde var).

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Betik konumu tek: mevcut düzen dotnet/ kökü; dotnet/betikler/ yeni dizini açılmaz ya da tüm betikler tek seferde taşınır.
- yedek-al-ve-dogrula.sh'ın docker'sızlaştırılması ile yeni yedek.sh ikiz dump yolu doğurmaz: dump+doğrulama tek fonksiyon kümesinde toplanır, denetim.sh sayacıyla zorlanır.
- systemd timer'ları T-1'in deploy/systemd/ düzenine, kuyumcu kullanıcısına ve EnvironmentFile'ına yaslanır; kendi ortam düzenini kurmaz.
- Yedek şifreleme anahtarı KUYUMCU_ALAN_ANAHTARI ile AYNI olmaz ve yedek hedefinden farklı dizin/izin setinde durur; appsettings'te yalnız YOL tutulur, SIR değil.
- Dış kopya hedefinin kimlik bilgileri (S3/sftp) appsettings'e yazılmaz; T-1'in ortam değişkeni disiplinine tabidir.
- mysqldump parolası argv'de geçmez (--defaults-extra-file); 'ps aux' çıktısında görünmediği test edilebilir kanıtla doğrulanır.
- Tatbikat raporunun 'yalnız tablo adı, veri değeri yazılmaz' kuralı YedekTestleri'nde somut senaryoyla ve mutasyon kanıtıyla doğrulanır.
- Aylık tatbikat ayrı şemaya HAM RESTORE ile yüklenir (dotnet ef database update kullanılmaz) — T-1'in 'uygulama şemayı kendi değiştirmez' ilkesiyle ikinci migration yolu doğmaz; tatbikat şemasının düşürüldüğü ayrıca test edilir.
- yedek_kayitlari/yedek_tatbikatlari kiracısız muafiyeti Tenants kalıbından alınır; KiraciYazmaDenetcisi istisnası denetim.sh'ta sayılır. Ekranda 'bu, tüm kiracıları kapsayan sistem yedeğidir' uyarısı yer alır.
- Canlı doğrulamaya 'test kiracısı/geçici şema silindi' kapanış kanıtı ve yedek_talepleri için çapraz-kiracı HTTP 403/404 testi eklenir.
