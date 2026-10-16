# Paket T-1 — Yayın ve kurulum paketi (T-1 + F-6 birleşik)

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: 3-4 gün (Faz A). Mühür ekindeki takvim satırıyla birebir: 'T-1+F-6 3-4 gün'. Faz B ek 0,5 gün, T-4a sonrasına bağlı. Gerçek kurulum provası (temiz makine, iki işletim sistemi) bu sürenin ~1 günü. gün · Önkoşullar: 45-API (MFA, ADR-0018) — Mehmet'in mühürlü şartı: 45 tamamlanmadan üretime yayın adımı başlayamaz. Dalga 2b'de bittiği için T-1 kodu dalga 3'te yazılabilir., Dalga 3 içi sıra: 19, 25, 20, 27, UX-3-B, 34-A, 35-A, 47-A + T-2 T-1'den ÖNCE gelir (mühürdeki nihai sıra)., F-6 ayrı paket DEĞİL: mühür 2 ile T-1 ile tek pakettir; ayrı uygulanamaz., Paket 33 (bitti) — DenetimIzi/DegismezIpucu altyapısı; T-1 yeni varlık doğurmadığı için yalnız dolaylı bağımlılık., F-7 (Let's Encrypt + otomatik yenileme) T-1'DEN SONRA: T-1 TLS'i zorunlu kılar, F-7 sertifikayı otomatikleştirir. T-1 kabulü için elle üretilmiş/geçici sertifika yeterlidir., T-3 (yedek otomasyonu) T-1'den SONRA; T-1 yalnız mevcut dotnet/yedek-al-ve-dogrula.sh betiğini runbook'a bağlar., T-4a (sürüm damgası) T-1'den sonra; T-1'in Faz B yayın kartı T-4a'nın sürüm bilgisini tüketir., F-2-min (kiracı açma kilidi) T-1 ile aynı dalgada, bağımsız. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3
> ⚠ **VETO:** YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: İTİRAZ: T-1 ve T-3 aynı dalgada iki ayrı systemd birim düzeni ve iki ayrı belge/betik kökü kuruyor. 'Tek yayın yolu' T-1'in kendi mührüyken (ecosystem.config.js/start.bat pasifleştiriliyor) bu ikilik mührü delik bırakır; dizin kararı T-1 kodu yazılmadan verilmeli.

## Hedef
Temiz makinede SDK'sız, Development'sız, sabit parolasız, TLS'li kurulum yapılabilsin; varsayılan/örnek sırla uygulama AÇILMASIN (fail-closed); şema yükseltmesi 'önce yedek' runbook'lu migration bundle ile tek yoldan yapılsın; üretim compose'unda 3306 dışa kapansın.

## Kodda bugünkü durum
Yayın yolu yok: docs/DEPLOY.md:15,34-38 hâlâ NestJS/pnpm anlatır; deploy/ecosystem.config.js PM2 + dist/main.js (Node artığı); start.bat:17,58 müşteriye SDK + ASPNETCORE_ENVIRONMENT=Development + dotnet run dayatır. Program.cs (336 satır) içinde Migrate()/PendingMigrations hiç geçmiyor. appsettings.json:9 parola DEGISTIR, :22 Jwt:Secret DEGISTIR-en-az-32-karakter-uretim-secreti — Program.cs:21-22 yalnız null kontrolü yapar, varsayılan değerle sorunsuz açılır. Sifreleme:Anahtar üretim şablonunda yok; tek fail-closed örneği AlanSifreleme.Yukle (Common/AlanSifreleme.cs:115-140). docker-compose.yml root parolasını düz metin gömer ve 3306:3306 ile dışa açar; DB adı compose'ta kuyumcu, appsettings.Development.json:8'de kuyumcu_net (çelişki). deploy/nginx-kuyumcu.conf:11-12 yalnız 80 dinler; TLS/HSTS/80-443 yönlendirmesi yok. Sağlam taban: UseForwardedHeaders (Program.cs:240,260), AddHealthChecks + /saglik (Program.cs:219), dotnet/yedek-al-ve-dogrula.sh (CHECKSUM'lı geri yükleme kanıtı), 31 migration (dotnet/Kuyumcu.Api/Data/Migrations).

## Kapsam
**İçinde:**
- Self-contained publish betiği (win-x64 + linux-x64), tek çıktı klasörü; Blazor WASM aynı süreçten sunulur (Program.cs:315-317 zaten yapıyor).
- appsettings.Production.json ŞABLONU: sır içermez, tüm sırlar ortam değişkeninden okunur; .ornek karşılığı repoda.
- Açılış fail-closed denetçisi: varsayılan/örnek JWT sırrı, 32 bayttan kısa sır, DEGISTIR parolası, eksik Sifreleme:Anahtar, Production'da Development bağlantısı.
- Windows Service (UseWindowsService + sc.exe) ve systemd birim dosyası; Development ortamı üretimde yasak.
- Kurulum sihirbazı betiği: JWT sırrı + MySQL parolası + AES anahtarı üretir, .env ve ortam değişkenlerine yazar, ekrana tam sır basmaz.
- Migration bundle üretimi (dotnet ef migrations bundle) + 'önce yedek al' runbook'u; yedek adımı mevcut dotnet/yedek-al-ve-dogrula.sh ile yapılır.
- Açılışta bekleyen migration varsa fail-closed durma (otomatik Migrate() DEĞİL) — bkz. iş kuralları.
- docker-compose.uretim.yml: sırlar .env, 3306 yalnız iç ağ (expose, ports yok), cpus/mem limiti, json-file log rotasyonu, restart policy, healthcheck.
- Tek kaynaktan DB adı: kuyumcu_net/kuyumcu çelişkisi KUYUMCU_VT değişkeniyle kapanır.
- TLS zorunluluğu: doğrudan Kestrel HTTPS ya da açıkça beyan edilmiş ters vekil; ikisi de yoksa Production'da açılış durur. nginx/IIS 443 + HSTS + 80-443 şablonu.
- Ters vekil şablonlarının güncellenmesi (deploy/nginx-kuyumcu.conf, deploy/iis-web.config) ve Node artığı ecosystem.config.js'in pasifleştirilmesi.
- denetim.sh'a T-1 sayaçları: açılış denetçisi tam 1 kez çağrılır; DEGISTIR/örnek sır dizgesi yalnız kara liste dosyasında ve şablonlarda geçer.

**Dışında:**
- F-7 Let's Encrypt otomasyonu (certbot, otomatik yenileme). T-1 yalnız TLS'i ZORUNLU kılar ve sertifika yolunu yapılandırmadan okur; sertifika edinimi F-7'de.
- T-3 yedek otomasyonu (zamanlayıcı, şifreli dış kopya, 30 gün, Yedekler ekranı) — T-1 yalnız elle çağrılan mevcut betiği runbook'a bağlar.
- T-4a sürüm/derleme damgası ve hata kimliği — ayrı paket; T-1 sürüm ucunu AÇMAZ.
- F-2-min kiracı açma kilidi; F-1/F-3/F-4 abonelik/faturalama.
- Otomatik CI/CD boru hattı, konteyner imajı yayımlama (registry), çok düğümlü/yük dengeli kurulum.
- Veri göçü (eski TS yığınından içe aktarım) ve müşteri verisi taşıma araçları.
- İmalat/atölye (Katman 1) ile ilgili hiçbir şey.
- Şifreleme anahtarı rotasyonu/anahtar kasası (KMS) entegrasyonu — 1.1'e devreder.

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| (yeni tablo YOK) | T-1 hiçbir yeni varlık doğurmaz; DoD'un 'her yeni varlık PATCH/pasife alma + yetki + denetim + DegismezIpucu' maddesi tetiklenmez. IKiraciVarlik yüzeyi değişmez, yeni migration üretilmez (31 migration sabit kalır). | - | - | - |
| __EFMigrationsHistory | Salt okunur kullanım: açılışta GetPendingMigrationsAsync ile karşılaştırılır. Yazma yalnız migration bundle üzerinden olur; uygulama süreci şemayı KENDİ değiştirmez. | - | - | - |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/saglik` | anonim | Mevcut uç (Program.cs:219 AddHealthChecks, MapHealthChecks). DEĞİŞMEZ: anonim, gövdesi Healthy/Unhealthy. Yük dengeleyici/servis yöneticisi bunu yoklar. Sır, sürüm, şema bilgisi SIZDIRMAZ. | yok |
| GET | `/saglik/hazir` | anonim | YENİ hazırlık (readiness) yoklaması: DB bağlantısı açılabiliyor mu + bekleyen migration var mı. Sağlıklıysa 200, bekleyen migration/DB yoksa 503 ve gövde yalnız hazir:false + kod (migration adı VERİLMEZ). | yok (kimlik öncesi; oran sınırına tabi) |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Common/UretimAcilisDenetimi.cs` — Statik Dogrula(IConfiguration, IHostEnvironment). AlanSifreleme.Yukle (AlanSifreleme.cs:115-140) kalıbının birebir ikizi: TEMBEL DEĞİL, Build() öncesi çağrılır, ihlalde InvalidOperationException. Kara liste sabitleri (DEGISTIR ön eki, appsettings.Development.json:22 dizgesi) yalnız bu dosyada yaşar.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Satır 22 civarına, jwtAyarlar kurulumundan HEMEN SONRA tek satır: UretimAcilisDenetimi.Dogrula(builder.Configuration, builder.Environment). AlanSifreleme.Yukle (:40) ile aynı bölgede, ikinci bir yapılandırma okuma yolu açılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — app.Run() öncesi MigrationHazirlikKontrolu: GetPendingMigrationsAsync boş değilse Production'da log + çıkış kodu 3 ile durur. MapHealthChecks yanına /saglik/hazir eklenir. UseWindowsService/UseSystemd host uzantısı eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj` — Microsoft.Extensions.Hosting.WindowsServices ve .Systemd paket başvurusu; InvariantGlobalization false (Türkçe collation/kültür), PublishSingleFile değil (WASM statikleri nedeniyle klasör yayını).
- **YENI** `dotnet/Kuyumcu.Api/appsettings.Production.json` — Sırsız şablon: ConnectionStrings/Jwt/Sifreleme alanları YOK (ortam değişkeninden gelir), Urls https, Uretim:TersVekilArkasinda false, Cors:Origins boş, Logging Warning. Mevcut Masak/Tevsik/Belge/Hurda blokları appsettings.json'dan miras kalır.
- **YENI** `dotnet/yayin-al.sh` — dotnet publish -c Release -r linux-x64 (ve win-x64) --self-contained true; ardından dotnet ef migrations bundle üretir. --no-build KULLANILMAZ (ortam tuzağı notu). Çıktı: yayin/<rid>/ + kuyumcu-migrate(.exe).
- **YENI** `dotnet/kurulum.sh ve dotnet/kurulum.ps1` — Kurulum sihirbazı: openssl/RandomNumberGenerator ile JWT sırrı (48 bayt), MySQL parolası, AES-GCM 32 baytlık base64 anahtar üretir; .env ve servis birimine yazar; ekrana yalnız var/yok basar (sır basılmaz). Var olan .env'i ÜZERİNE YAZMAZ.
- **YENI** `deploy/kuyumcu.service` — systemd birimi: Type=notify, User=kuyumcu, EnvironmentFile=/etc/kuyumcu/.env, ASPNETCORE_ENVIRONMENT=Production, Restart=on-failure, NoNewPrivileges, ProtectSystem=strict, okunabilir tek dizin yayın klasörü.
- **YENI** `deploy/kuyumcu-service-kur.ps1` — Windows: sc.exe create kuyumcu-erp binPath yayin/win-x64/Kuyumcu.Api.exe, ortam değişkenleri makine kapsamında, servis hesabı ile; start.bat'ın yerini alır.
- **YENI** `docker-compose.uretim.yml` — mysql: ports YOK, yalnız expose 3306 + iç ağ; MYSQL_ROOT_PASSWORD ve MYSQL_DATABASE .env'den; deploy.resources.limits cpus/memory; logging json-file max-size 10m max-file 5; healthcheck parolayı komut satırında taşımaz (MYSQL_PWD). api servisi yayın imajını iç ağda 5000'de tutar, dışa yalnız vekil çıkar.
- **DEGISTIR** `deploy/nginx-kuyumcu.conf` — 443 ssl http2 sunucu bloğu + 80'de yalnız 301 yönlendirme; mevcut belge yolu client_max_body_size 8m blokları ve X-Forwarded-* başlıkları korunur (bugünkü :24-52 içeriği kaybedilmez). HSTS başlığı vekilde.
- **DEGISTIR** `dotnet/denetim.sh` — Mevcut kod_say/kod_var yardımcılarıyla iki sayaç: (1) UretimAcilisDenetimi.Dogrula tam 1 kez, yalnız Program.cs'te; (2) DEGISTIR ve örnek sır dizgesi Common/UretimAcilisDenetimi.cs dışında 0 kod satırı. İhlal çıktısı mevcut biçime uyar.

## Ekranlar
- `(yeni rota YOK)` · `-` — T-1 Faz A tamamen sunucu/altyapı paketidir; tek bir Blazor sayfası eklenmez. UX sözleşmesi (UX-S1 düzenleme yolu, UX-S2 deftere yazan yerinde düzenlenmez) yeni veri girişi olmadığı için tetiklenmez; DoD'un 'her düğme istek atar ya da disabled' maddesi yeni düğme doğmadığı için boş geçer. (menü/yetki: -)
- `/ayarlar` · `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — Faz B (T-4a ile birlikte): mevcut sayfaya salt okunur 'Yayın durumu' kartı — ortam adı, TLS var/yok, şema güncel mi (/saglik/hazir yanıtından). Sır DEĞERİ gösterilmez, yalnız var/yok rozeti. Kartta düğme yoktur; basit modda gizli, profesyonel modda görünür. (menü/yetki: SAHIP)

## İş kuralları (fail-closed)
- FAIL-CLOSED 1: Production'da Jwt:Secret kara listedeyse (DEGISTIR ön ekli ya da appsettings.Development.json:22'deki dizge) veya 32 bayttan kısaysa uygulama Build() öncesi durur; süreç sıfırdan farklı çıkış koduyla biter. 401/500 dönen bir sunucu AÇILMAZ, hiç açılmaz.
- FAIL-CLOSED 2: Bağlantı dizgesindeki parola DEGISTIR ise ya da appsettings.Development.json'daki parola ile birebir aynıysa Production açılışı durur (aynı parolanın her müşteride tekrarı kuruldaki KRİTİK bulgudur).
- FAIL-CLOSED 3: Sifreleme:Anahtar yoksa zaten durur (AlanSifreleme.cs:125-128) — T-1 bunu Production şablonunda ortam değişkenine taşır ve kurulum sihirbazını zorunlu kılar; mevcut mekanizma DEĞİŞTİRİLMEZ, yeniden kullanılır.
- FAIL-CLOSED 4: Production'da ne Kestrel HTTPS uç noktası ne de Uretim:TersVekilArkasinda=true beyanı varsa açılış durur. TLS'siz üretim onaylanmaz (Mehmet mührü). Beyan varken X-Forwarded-Proto http gelen isteği UseHttpsRedirection zaten 307'ler (Program.cs:260,304).
- FAIL-CLOSED 5: Bekleyen migration varsa Production'da uygulama HİZMET VERMEZ. Otomatik Migrate() KASITLI OLARAK AÇILMAZ: yedeksiz/çok süreçli şema değişimi geri alınamaz. Şema yükseltmesi yalnız migration bundle + önceden alınmış doğrulanmış yedek ile yapılır.
- FAIL-CLOSED 6: ASPNETCORE_ENVIRONMENT=Development ile üretim birimi başlatılamaz; servis dosyaları Production'ı sabitler ve denetçi Development'ta bile kara liste dışı kontrolleri atlamaz (yalnız Development bağlantısına izin verir).
- Sır hiçbir yerde loga, ekrana, hata mesajına, /saglik gövdesine yazılmaz; denetçi ihlali 'Jwt:Secret varsayılan/örnek değerde' gibi DEĞER İÇERMEYEN mesajla bildirir.
- Tek kaynaktan DB adı: compose, appsettings.Production ve yedek betiği KUYUMCU_VT değişkenini okur; iki farklı ad üreten yapılandırma birleşimi denetçide hata verir.
- docker-compose.uretim.yml MySQL portunu ASLA yayımlamaz (ports anahtarı yok); veritabanına erişim yalnız iç ağ üzerinden.
- Kurulum sihirbazı var olan .env'i sessizce ezmez; ezme yalnız açık --yenile bayrağıyla ve mevcut dosyayı zaman damgalı kopyalayarak olur.
- Yeni bir yayın yolu AÇILMAZ: ecosystem.config.js ve start.bat üretim yolu olmaktan çıkarılır (başlığına GELİŞTİRME içindir uyarısı), ikiz yayın yolu doğmaz (Bertan mührü).
- Mehmet kapısı: 45 (MFA) bitmeden T-1'in üretime yayın ADIMI başlayamaz; paketin kodu yazılabilir, gerçek müşteri kurulumu 45 kapanmadan yapılmaz.

## Testler
- `dotnet/Kuyumcu.Tests/UretimAcilisDenetimiTestleri.cs` [saf]
  - VarsayilanJwtSirriUretimdeAcilisiDurdurur
  - KisaJwtSirriUretimdeAcilisiDurdurur
  - GelistirmeJwtSirriUretimdeAcilisiDurdurur
  - DegistirParolasiUretimdeAcilisiDurdurur
  - GelistirmeParolasiUretimdeAcilisiDurdurur
  - TlsBeyanıYokkenUretimdeAcilisDurur
  - TersVekilBeyaniVarkenAcilisGecer
  - GucluSirlarlaAcilisGecer
  - HataMesajiSirDegeriniIcermez
  - GelistirmeOrtaminda DevBaglantisiKabulEdilir
- `dotnet/Kuyumcu.Tests/YayinAcilisHttpTestleri.cs` [HTTP]
  - VarsayilanSirliFabrikaAcilistaInvalidOperationExceptionAtar
  - SaglikUcuSirVeSurumSizdirmaz
  - HazirUcuBekleyenMigrationVarken503Doner
  - HazirUcuSemaGuncelken200Doner
  - UretimProfilindeHttpIstegi307HttpsYonlendirilir
  - UretimProfilindeHstsBasligiDoner
- `dotnet/Kuyumcu.Tests/MigrationHazirlikTestleri.cs` [DB]
  - GercekVeritabaninda BekleyenMigrationYok
  - UygulamaSemayiKendiDegistirmez_MigrationsHistorySatirSayisiDegismez
  - BundleIleUygulananSemaEfModeliyleUyusur
- `dotnet/Kuyumcu.Tests/YayinDosyaSozlesmesiTestleri.cs` [saf]
  - ProductionSablonuHicbirSirAnahtariIcermez
  - ComposeUretimDosyasi3306PortuYayimlamaz
  - ComposeUretimDosyasiKaynakLimitiVeLogRotasyonuIcerir
  - NginxConf80PortundaYalnizYonlendirmeYapar
  - SystemdBirimiProductionOrtaminiSabitler
- `dotnet/Kuyumcu.Tests/AyarlarYayinKartiTestleri.cs` [bUnit]
  - YayinKartiSirDegeriGostermez_YalnizVarYokRozeti
  - YayinKartiBasitModdaGizlidir
  - YayinKartindaIstekAtmayanDugmeYoktur

## Mutasyon kanıtları
- M-T1-01 — UretimAcilisDenetimi.cs'te JWT kara liste karşılaştırmasını kaldır: VarsayilanJwtSirriUretimdeAcilisiDurdurur KIRMIZI.
- M-T1-02 — Asgari sır uzunluğunu 32'den 1'e düşür: KisaJwtSirriUretimdeAcilisiDurdurur KIRMIZI.
- M-T1-03 — Parola kara listesini kaldır: DegistirParolasiUretimdeAcilisiDurdurur ve GelistirmeParolasiUretimdeAcilisiDurdurur KIRMIZI.
- M-T1-04 — TLS/ters vekil beyanı kontrolünü kaldır: TlsBeyaniYokkenUretimdeAcilisDurur KIRMIZI.
- M-T1-05 — Dogrula çağrısını Program.cs:22'den Build() SONRASINA taşı (tembelleştir): VarsayilanSirliFabrikaAcilistaInvalidOperationExceptionAtar KIRMIZI (AlanSifreleme'de canlıda yakalanan tembellik hatasının tekrarını önler).
- M-T1-06 — Bekleyen migration kontrolünü kaldır: HazirUcuBekleyenMigrationVarken503Doner KIRMIZI.
- M-T1-07 — Program.cs'e db.Database.Migrate() ekle (otomatik şema değişimi): UygulamaSemayiKendiDegistirmez_MigrationsHistorySatirSayisiDegismez KIRMIZI ve denetim.sh sayacı ihlal verir.
- M-T1-08 — appsettings.Production.json'a bir Jwt:Secret değeri ekle: ProductionSablonuHicbirSirAnahtariIcermez KIRMIZI.
- M-T1-09 — docker-compose.uretim.yml'ye ports 3306:3306 ekle: ComposeUretimDosyasi3306PortuYayimlamaz KIRMIZI.
- M-T1-10 — Denetçi hata mesajına sır değerini gömme: HataMesajiSirDegeriniIcermez KIRMIZI.
- M-T1-11 — Dogrula çağrısını Program.cs dışında ikinci bir yerden de çağır: denetim.sh 'açılış denetçisi tam 1 kez' sayacı KIRMIZI.
- M-T1-12 — /saglik gövdesine sürüm ve bağlantı dizgesi ekle: SaglikUcuSirVeSurumSizdirmaz KIRMIZI.

## Canlı doğrulama
- Gerçek publish: dotnet/yayin-al.sh çalıştır; yayin/linux-x64/Kuyumcu.Api ve kuyumcu-migrate dosyalarının varlığı + boyutu kaydedilir. Hedef makinede dotnet --list-sdks BOŞ iken uygulama çalışmalı (self-contained kanıtı).
- Sıfırdan kurulum provası: temiz şema (kuyumcu_prova) + docker-compose.uretim.yml ayağa kaldır; SQL: SHOW DATABASES ile kuyumcu_prova, SELECT COUNT(*) FROM __EFMigrationsHistory = 31.
- Fail-closed 1: ASPNETCORE_ENVIRONMENT=Production + Jwt__Secret=DEGISTIR-en-az-32-karakter-uretim-secreti ile başlat; süreç çıkış kodu sıfırdan farklı, curl http://localhost:5198/saglik bağlantı reddi (HTTP yanıt YOK).
- Fail-closed 2: parola DEGISTIR ile başlat; aynı şekilde süreç açılmaz, log satırı sır değeri içermez (grep ile doğrulanır).
- Fail-closed 3: TLS beyanı olmadan Production başlat; süreç açılmaz. Uretim__TersVekilArkasinda=true verilince açılır ve curl -s -o /dev/null -w %{http_code} ile /saglik 200 döner.
- Bekleyen migration: __EFMigrationsHistory'den son satırı geçici olarak sil, uygulamayı başlat; GET /saglik/hazir 503, GET /saglik 200 (canlılık ile hazırlık ayrımı kanıtı). Sonra kuyumcu-migrate ile 200'e döner.
- Port kapalılığı: docker-compose.uretim.yml ayakta iken ana makineden mysql --host 127.0.0.1 --port 3306 bağlantı REDDEDİLMELİ; aynı bağlantı api konteynerinin içinden BAŞARILI olmalı.
- TLS/vekil: nginx şablonu ile curl -I http://alan/ 301 ve Location https ile başlar; curl -I https://alan/ 200 + Strict-Transport-Security başlığı.
- Uçtan uca duman testi: kurulum sonrası SAHIP kullanıcısıyla giriş 200, bir satış fişi POST 201, SQL: SELECT COUNT(*) FROM hareketler WHERE tenant_id = <prova kiracı> beklenen satır sayısını verir.
- Yedek kapısı: dotnet/yedek-al-ve-dogrula.sh prova şeması üzerinde çalıştırılır, çıkış kodu 0 (CHECKSUM eşleşmesi) — 'önce yedek al' runbook adımının gerçekten çalıştığı kanıtı.
- Servis birimi: systemctl start kuyumcu && systemctl is-active = active; makine yeniden başlatıldığında (ya da systemctl kill -s KILL) süreç kendiliğinden geri gelir.
- Prova bittiğinde test kiracısı ve kuyumcu_prova şeması silinir; silme komutu ve çıktısı rapora yazılır (silme kurucuya bırakılan dosya işlemleri hariç).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/UretimAcilisDenetimi.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs (DEGISTIR: :22 civarı denetçi, :219 civarı /saglik/hazir, host servis uzantısı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj (DEGISTIR: WindowsServices + Systemd paketleri)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.Production.json ve .ornek (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/yayin-al.sh · dotnet/kurulum.sh · dotnet/kurulum.ps1 (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh (DEGISTIR: iki T-1 sayacı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docker-compose.uretim.yml (YENI) ve /docker-compose.yml (DEGISTIR: başlığına yalnız geliştirme notu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/kuyumcu.service · deploy/kuyumcu-service-kur.ps1 · deploy/.env.ornek (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/nginx-kuyumcu.conf · deploy/iis-web.config (DEGISTIR: 443 + HSTS + 80 yönlendirme)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/ecosystem.config.js · /start.bat (DEGISTIR: üretim yolu değildir uyarısı; silme kurucuya bırakılır)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docs/DEPLOY.md (DEGISTIR: NestJS/pnpm anlatımı .NET 8 self-contained + servis + bundle ile değiştirilir) ve docs/RUNBOOK-SEMA-YUKSELTME.md (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/UretimAcilisDenetimiTestleri.cs · YayinAcilisHttpTestleri.cs · MigrationHazirlikTestleri.cs · YayinDosyaSozlesmesiTestleri.cs · AyarlarYayinKartiTestleri.cs (YENI)`

## Fazlar
- **Faz A — sevk edilebilir asgari (yayın + fail-closed + TLS + üretim compose)** (Dalga 3, 47-A + T-2 bittikten hemen sonra): UretimAcilisDenetimi (5 fail-closed kural) + Program.cs kancası + migration hazırlık kontrolü + /saglik/hazir + appsettings.Production şablonu + yayin-al.sh (self-contained, bundle) + kurulum sihirbazı (sır üretimi) + systemd/Windows Service birimi + docker-compose.uretim.yml + nginx/IIS 443 şablonu + DEPLOY.md yeniden yazımı + şema yükseltme runbook'u + denetim.sh iki sayaç + beş test dosyası + gerçek kurulum provası.
- **Faz B — kurulum konforu** (T-4a ve F-7 sonrası, satış öncesi): Ayarlar.razor salt okunur Yayın durumu kartı (ortam, TLS var/yok, şema güncel mi — sır değeri yok, düğme yok); kurulum sihirbazının etkileşimli sürümü (alan adı, sertifika yolu, yedek dizini soruları); win-x64 ve linux-x64 çıktılarının tek arşivde paketlenmesi; kurulum kaydının dosyaya yazılması (destek için).
## Mevzuat dayanağı
- MASAK / 5549 sayılı Kanun kapsamında kimlik verisinin korunması — **YUKSEK (kodda uygulanmış)** — AlanSifreleme.cs:126-128 gerekçesi zaten bunu söylüyor: kimlik verisi şifresiz yazılamaz, uygulama açılmaz. T-1 bu anahtarı üretim şablonunda ortam değişkenine taşır; mevcut kural değişmez.
- KVKK m.12 — veri güvenliğine ilişkin teknik tedbirler (aktarımda şifreleme, yetkisiz erişimin önlenmesi) — **ORTA-YUKSEK (yorum)** — TLS zorunluluğu, 3306'nın dışa kapatılması ve müşteri başına benzersiz parola bu maddenin doğrudan teknik karşılığıdır. Hukuki nitelendirme DOĞRULANMALI (hukukçu görüşü alınmadı).
- KVKK Veri Güvenliği Rehberi — varsayılan parolaların değiştirilmesi, test/üretim ortamı ayrımı — **ORTA (DOĞRULANMALI)** — Rehberin bağlayıcılığı idari tavsiye düzeyindedir; T-1'in kara liste kuralı bu tavsiyeyi teknik olarak zorunlu kılar. Rehberin güncel sürümü ve tam madde numarası DOĞRULANMALI.
- VUK 219/A ve elektronik defter/kayıt saklama — yedek ve geri yükleme yükümlülüğü — **DUSUK (DOĞRULANMALI)** — T-1 yalnız 'önce yedek al' runbook'unu bağlar; saklama süresi ve elektronik kayıt ibraz yükümlülüğünün tam dayanağı T-3'te ele alınmalı. Bu paket için bağlayıcı bir gereklilik varsaymıyorum.

## Riskler
- Fail-closed denetçi fazla katı olursa meşru bir kurulumu kilitler (örn. müşterinin sırrı tesadüfen DEGISTIR ile başlıyorsa). Azaltma: kara liste TAM dizge eşleşmesi + ön ek kuralı dar tutulur, hata mesajı hangi kuralın tetiklendiğini açıkça söyler.
- Otomatik Migrate() açmama kararı, kurulumu iki adımlı yapar; müşteri bundle'ı unutursa uygulama 503 verir. Azaltma: /saglik/hazir mesajı ve kurulum betiğinin bundle'ı kendiliğinden çağırması (yedek doğrulaması başarılıysa).
- Test projesi WebApplicationFactory ile Program'ı çalıştırıyor (Program.cs:322 partial); yeni açılış denetçisi tüm mevcut HTTP testlerini kırabilir. Azaltma: denetçi Development/Testing ortamında kara liste dışı kuralları uygular, ilk iş olarak tam test koşusu alınır.
- DB adı birleştirmesi (kuyumcu vs kuyumcu_net) mevcut geliştirme veritabanını ya da yedek betiğinin varsayılanını bozabilir. Azaltma: geliştirme adı DEĞİŞMEZ, yalnız üretim tek değişkene bağlanır.
- Self-contained yayın çıktısı büyür (~80-120 MB, DOĞRULANMALI) ve Blazor WASM statikleriyle birlikte taşınır; yavaş bağlantıda kurulum uzar.
- OpenVZ tabanlı VPS'te Docker güvenilir çalışmıyor (docs/DEPLOY.md:26 notu) — docker-compose.uretim.yml her müşteride kullanılamaz. Azaltma: Docker'sız yol (systemd + yerel MySQL) eşdeğer şekilde belgelenir ve provası ayrıca alınır.
- Windows Service kurulumu yönetici hakkı ve makine kapsamlı ortam değişkeni ister; müşteri ortamında engellenebilir.
- Sır üretimi betiği ekrana/loga sır sızdırırsa F-6'nın tüm kazancı kaybolur. Azaltma: betik yalnız var/yok basar, testte log çıktısı grep ile taranır.
- ecosystem.config.js ve start.bat silinmiyor (hook kısıtı ve izinsiz silme yasağı) — ikiz yayın yolu algısı sürebilir. Azaltma: dosya başlıklarına net uyarı, silme kurucuya eskale edilir.
- TLS sertifikası F-7'ye bırakıldığı için T-1 kabul provası geçici/kendinden imzalı sertifika ile yapılır; bu, gerçek müşteri kurulumunun kanıtı SAYILMAZ (Madde 1).
- Mehmet'in 45 (MFA) şartı takvim kayarsa T-1'in kodu hazır olsa bile yayın adımı bloklanır; bu bir kusur değil, kasıtlı kapıdır — planlamada gecikme olarak görünür.

## Açık sorular
- Üretimde MySQL nerede koşacak: docker-compose.uretim.yml mi, yoksa sunucuya doğrudan kurulan MySQL 8.4 mü? İkisi de destekleniyorsa kabul provası HANGİSİ üzerinde alınacak (ikisi de mi)? Ekip oyu gerekir.
- Hedef işletim sistemi önceliği: Windows Server (VDS) mi Ubuntu (VPS) mi önce? İkisinin de publish RID'i üretilecek ama gerçek kurulum provası hangisinde yapılacak?
- Otomatik Migrate() açılmaması kararı ekipçe onaylanıyor mu? (Öneri: hayır, bundle + yedek runbook'u. Karar mühürlenmeli.)
- Kara listeye hangi dizgeler girecek: yalnız DEGISTIR ön eki ve appsettings.Development.json:22'deki sır mı, yoksa genel entropi eşiği (örn. tekrar eden karakter) de mi? Yanlış pozitif riski var.
- Alan adı ve TLS sertifikası kurucuya bırakılan kalemler arasında (mühür: 'alan adı/TLS bedeli kurucuya çıkar') — T-1 kabulü için geçici sertifika yeterli sayılacak mı?
- Ters vekil arkasında çalışırken UseHttpsRedirection'ın açık kalması mı yoksa vekile bırakılması mı tercih edilecek? (Şu an Program.cs:304 her non-Development'ta açık.)
- start.bat ve ecosystem.config.js silinsin mi? Silme kurucunun kararı; silinmezse yalnız uyarı başlığı eklenecek.
- Kurulum sihirbazı .env'i nereye yazacak: Linux'ta /etc/kuyumcu/.env, Windows'ta hangi dizin ve hangi ACL ile?

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — Mimari olarak doğru kurulmuş: ikinci yapılandırma okuma yolu açmıyor, mevcut AlanSifreleme fail-closed kalıbını ikizliyor, otomatik Migrate() açmama kararı (yedeksiz şema değişimi geri alınamaz) ADR ruhuna uygun ve /saglik ile /saglik/hazir ayrımı canlılık-hazırlık ayrımını doğru kuruyor. Paket boyutu Faz A olarak sevk edilebilir sınırda (publish + denetçi + servis birimi + compose + nginx + doküman aynı pakette) — bu yüzden koşullu. Sayısal iddialar ve dosya konumları düzeltilmeden mühürlenmemeli.
  - Koşullar: Migration sayısı '31 sabit' iddiası kanıtsız: projeler/jewellery/CLAUDE.md §3 '22 migration' diyor. Canlı doğrulamadaki COUNT(*) FROM __EFMigrationsHistory beklentisi sabit sayı değil, `dotnet ef migrations list` çıktısından türetilen değişken olmalı. · Program.cs iddiası düzeltilsin: /saglik/hazir MapHealthChecks yanına eklenecekse doğru satır :313, AddHealthChecks :219 değil. Tasarım metni :219 diyor; uygulayıcı yanlış yere eklemesin. · Açılış denetçisi Build() ÖNCESİ, AlanSifreleme.Yukle (Program.cs:40 civarı) ile aynı bölgede çağrılsın; tembel fabrika kesinlikle kullanılmasın (aynı hata canlıda yakalanmış — Program.cs:35-39 yorumu). · systemd birimlerinin yeri T-3 ile ÇAKIŞIYOR: T-1 deploy/kuyumcu.service, T-3 deploy/systemd/*.timer. Tek dizin seçilsin (öneri: deploy/systemd/), aksi halde iki yayın düzeni doğar. · Runbook dizini tek olsun: T-1 docs/RUNBOOK-SEMA-YUKSELTME.md, T-3 deploy/YEDEK-GERI-YUKLEME.md. İkisi de aynı kökte toplanmalı (öneri: docs/). · WebApplicationFactory testleri Program.cs:330 partial üzerinden gerçek Program'ı koşuyor; denetçi Development/Testing'te kara liste kurallarını uygulamamalı, yoksa 971 testin HTTP bacağı topluca kırılır.
- **YENİ: GÜVENLİK (Mehmet) — kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek şifreleme, içe aktarım riski, abonelik/plan kapılarının yazma yolunu bozmaması, telemetri/hata paketinde kişisel veri, MFA koşulu.: EVET** — Tasarım, kodda ölçülen gerçek açıkları (düz metin sır, dışa açık 3306, TLS'siz vekil, tek adımlı otomatik migration riski) doğru teşhis etmiş ve her biri için fail-closed karşılık önermiş. En kritik güvenlik kazanımı: varsayılan sırla uygulamanın hiç açılmaması ve Production'da otomatik Migrate() KASITLI olarak kapatılması (yedeksiz şema değişimi riskini ortadan kaldırıyor). Koşullar kapatılırsa veto gerekmez.
  - Koşullar: appsettings.json:11 (password=DEGISTIR) ve :20 (Jwt:Secret=DEGISTIR-...) kara liste değeri gerçek dosyadan doğrulandı — UretimAcilisDenetimi bu TAM dizgeleri karşılaştırmalı, alt dizge eşleşmesiyle yanlış pozitif üretmemeli. · docker-compose.yml:7-8,10 gerçekten MYSQL_ROOT_PASSWORD düz metin + 3306:3306 açık; docker-compose.uretim.yml bu üç satırı KESİNLİKLE değiştirmeli, aksi hâlde T-1 kabul edilmiş sayılmaz. · nginx-kuyumcu.conf:12'de yalnızca `listen 80;` var, TLS/443/HSTS yok — bu doğrulandı; yeni conf'ta 80→443 redirect + HSTS başlığı olmadan kabul verilmez. · Mehmet kapısı bağlayıcı: 45-API (MFA) tamamlanmadan T-1'in GERÇEK ÜRETİM KURULUM adımı başlamaz; kod yazımı bu şarttan muaf ama tasarımın kendisi bunu açıkça yazmış (onkosul_paketler). · Fail-closed denetçinin Development ortamında kara liste dışı kuralları (TLS/migration) atlamaması gerektiği netleştirilmeli — aksi hâlde Development'ta test edilen davranış Production'da farklılaşır ve görünmeyen açık kapı doğar. · Kurulum sihirbazının .env yazma yetkisi (root/sudo gerektirebilir) ve ACL'i belirsiz (açık soru) — sır dosyasının dünya-okunabilir izinle kalması riski koşul olarak kapatılmalı (chmod 600, kuyumcu kullanıcısı sahipliği).
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: EVET** — Fail-closed kuralları (5 adet) ve mutasyon kanıtları (M-T1-01..12) somut ve gerçek kodun bugünkü zafiyetlerine (appsettings.json:9-19 DEGISTIR sırları, docker-compose.yml:7-10 düz parola+3306 dışa açık, yedek-al-ve-dogrula.sh:36-76 docker bağımlılığı) doğru şekilde bağlanmış. Canlı doğrulama adımları ölçülebilir (çıkış kodu, curl durum kodu, mysql bağlantı reddi, SQL COUNT) ve test kiracısı/prova şeması silme son adım olarak açıkça yazılı. Test katman dengesi (10 saf + 6 HTTP + 3 DB + 5 saf + 3 bUnit) makul.
  - Koşullar: M-T1-05 mutasyonu (Dogrula çağrısını tembelleştirme) test edilirken WebApplicationFactory'nin paralel test koşularında statik denetçi durumundan etkilenmemesi için xUnit collection izolasyonu tanımlanmalı; Program.cs:22 civarı tek çağrı noktası korunmalı. · 'Bekleyen migration' canlı doğrulama adımı (__EFMigrationsHistory'den satır silme) hangi şemada çalışacağı (kuyumcu_prova mı, geliştirme mi) açıkça yazılmalı; yanlış şemada koşarsa gerçek migration geçmişi bozulur. · kurulum.sh/kurulum.ps1 betiğinin STDOUT/STDERR'inde sır sızdırmadığını doğrulayan ayrı bir otomatik test veya canlı-doğrulama adımı eklenmeli (şu an yalnız risk notunda 'grep ile taranır' geçiyor, senaryo listede yok). · MigrationHazirlikTestleri.cs listesindeki 'GercekVeritabaninda BekleyenMigrationYok' senaryo adında boşluk var; geçerli bir C# metod adına düzeltilmeli. · Aylık tatbikat betiğinin (T-3) ayrı şemaya yüklerken migration bundle mı ham restore mu kullandığı, T-1'in 'uygulama şemayı kendi değiştirmez' ilkesiyle (Program.cs'te otomatik Migrate() yasağı) çakışmayacak şekilde netleştirilmeli.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Migration sayısı SABİT yazılmaz: canlı doğrulamadaki __EFMigrationsHistory beklentisi `dotnet ef migrations list` çıktısından türetilen değişkene bağlanır (T-1 '31' vs CLAUDE.md '22' çelişkisi).
- Açılış denetçisi Build() ÖNCESİ, AlanSifreleme.Yukle ile aynı bölgede çağrılır; tembel fabrika yasak. /saglik/hazir Program.cs:313 MapHealthChecks yanına eklenir (:219 değil).
- Kara liste TAM dizge karşılaştırır (appsettings.json password=DEGISTIR, Jwt:Secret=DEGISTIR-...); alt dizge eşleşmesi yanlış pozitif üretmez.
- Development/Testing'te kara liste kuralları uygulanmaz (971 testin HTTP bacağı korunur), ama TLS/migration kuralları ortamlar arası davranış farkı yaratmayacak şekilde netleştirilir.
- docker-compose.uretim.yml MYSQL_ROOT_PASSWORD düz metnini ve 3306:3306 dışa açık portu KESİNLİKLE kaldırır; nginx conf'ta 80→443 yönlendirme + HSTS olmadan kabul yok.
- systemd birimleri tek dizinde: deploy/systemd/ (T-3 timer'ları aynı kullanıcı/EnvironmentFile düzenine yaslanır). Runbook'lar tek kökte: docs/.
- Sır dosyası izinleri chmod 600 + kuyumcu kullanıcısı sahipliği; kurulum betiği STDOUT/STDERR'de sır sızdırmadığını gösteren otomatik adım ekler.
- M-T1-05 için xUnit collection izolasyonu tanımlanır; 'bekleyen migration' canlı doğrulaması yalnız kuyumcu_prova şemasında koşar (geliştirme geçmişi bozulmaz).
- Mehmet kapısı bağlayıcı: 45-API (MFA) tamamlanmadan GERÇEK ÜRETİM KURULUM adımı başlamaz; kod yazımı muaf.
- Sevk bölünmesi işaretlenir: çekirdek (denetçi + /saglik/hazir + publish) ile compose+vekil şablonları ayrı sevk edilebilir alt-adımdır.

### Mühür 4 eki (2026-09-16 14:56) — kalıcı günlük asgarisi, /sistem/* IP kısıtı, tek docs kökü (BAĞLAYICI)

> Kod öncesi açık kararlar oylaması (ana belge § MÜHÜR 4). Program.cs'e kod eklenmez; kalıcılık yayın artefaktlarıyla (systemd birimi, compose logging, appsettings.Production Logging, DEPLOY.md). Büyüme tahmini (+0,25-0,5 gün) DOĞRULANMALI — rapora gerçek süre yazılır.

- **K14** — T-1(+F-6) kapsamına kalıcı günlük ASGARİSİ eklenir: systemd StandardOutput=journal + SyslogIdentifier, Windows'ta EventLog, docker json-file rotasyonu (T-1:22,60), saklama süresi/boyut sınırı, maskeleme (sır/PII/istek gövdesi YOK; T-1:75). Dosyalar: deploy/systemd/kuyumcu.service, docker-compose.uretim.yml, appsettings.Production.json (T-1:55), docs/DEPLOY.md. Program.cs'e KOD EKLENMEZ (tek-el, T-1:52).
- **K4** — /api/v1/sistem/* uçları 45-API (MFA) tamamlanmadan ÜRETİME AÇILMAZ (Mehmet, veto yetkili; F-2-min:193,208). Bu bir takvim kapısıdır, teknik ikame değildir; anahtar kullanıcı olmadığı için MFA onu korumaz. Bu yüzden Bertan'ın koşulu EKLENİR: T-1 nginx şablonunda /sistem/* için ağ/vekil (IP) kısıtı zorunludur.
- **K6** — Pilot davetleri curl + X-Sistem-Anahtari ile üretilir. Runbook (curl örneği + SMS/WhatsApp'a kopyalanabilir davet metni şablonu; SMTP yok, F-2-min:19) T-1'in DEPLOY.md yeniden yazımıyla AYNI belge kökünde (docs/) yayımlanır; ikinci docs kökü açılmaz (T-1:204, MÜHÜR 3 tek yayın yolu).
