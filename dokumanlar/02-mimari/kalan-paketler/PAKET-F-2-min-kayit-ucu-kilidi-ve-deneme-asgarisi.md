# Paket F-2-min — Kayıt ucu kilidi ve deneme asgarisi

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: Faz A 1,5 gün + Faz B 1 gün = toplam ~2,5 gün (tek uygulayıcı, testler ve canlı doğrulama dâhil; sistem yöneticisi kimliği sorusu 'yapılandırma sırrı' olarak karara bağlanırsa. 'SISTEM' rolü seçilirse +1 gün.) gün · Önkoşullar: T-1 + F-6 birleşik yayın paketi (üretim sırrı üretimi ve varsayılan sırla fail davranışı; Sistem:Anahtari aynı sır hattına girer), 45-API (Mehmet'in canlı güvenlik kapısı, dalga 2b), UX-1-B PATCH/aktife-alma uçları (tek PATCH sözleşmesi kalıbı), Dışa aktarım modülünün mevcut hali (DisaAktarimModulu.cs) — kapı muafiyetinin dayanağı, DOĞRULANMALI: T-4a sürüm damgası bu paketle aynı dalgada; sıralama zorunluluğu değil, çakışma riski var · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 3

## Hedef
Bedava-sınırsız kiracı açmayı kapat: kiracı yalnız sistem yöneticisinin ürettiği tek kullanımlık davetle açılır; kiracı DENEME/AKTIF/SALT_OKUNUR durumuyla yaşar, süre bitince yazma fail-closed kapanır, okuma + dışa aktarım açık kalır; yönetici kiracıları listeler ve süreyi uzatır. Abonelik/faturalama (F-1) YOK.

## Kodda bugünkü durum
Bugün POST /api/v1/auth/kayit [AllowAnonymous] + [EnableRateLimiting(OranSiniri.Kayit)] ile sınırsız kiracı açıyor (AuthModulu.cs:321-326; gövde AuthModulu.cs:~100-145: Tenant+Sube+SAHIP kullanıcı tek tx). Tenant varlığında yalnız Plan/AiEtkin/Aktif var, süre/durum alanı YOK (Entities.cs:52-75). Sistem yöneticisi rolü kodda YOK (roller SAHIP/MUDUR/MUHASEBE/TEZGAHTAR — AktifKullanici.cs:11).

## Kapsam
**İçinde:**
- Kayıt ucunun davet zorunluluğuyla kilitlenmesi (davetsiz istek 403, fail-closed)
- tenants tablosuna durum + deneme_bitis + davet_id alanları
- Tek kullanımlık, süreli KiraciDaveti varlığı (üretim/iptal/listeleme)
- Sistem anahtarı (yapılandırma sırrı) ile korunan /api/v1/sistem/* ucu; varsayılan/boş sırla uçlar kapalı
- KiraciDurumKapisi: durum SALT_OKUNUR veya deneme süresi dolmuşsa tüm mutasyon fiilleri 403
- Dışa aktarım ve okuma uçlarının kapı dışında bırakılması (veri rehin alınmaz)
- Süre uzatma + durum değiştirme PATCH ucu (UX-S3 kalıbı) + denetim kaydı
- /isletme ekranında deneme bandı: kalan gün, salt-okunur uyarısı, dışa aktarıma bağlantı
- Faz B: /sistem/kiracilar yönetici konsolu (liste, uzat, davet üret/iptal)
- Testler: DB + HTTP + bUnit + saf; mutasyon kanıtları

**Dışında:**
- F-1 abonelik çekirdeği, plan-fiyat, fatura/tahsilat (dalga 8)
- F-3 plan bazlı özellik kapısı, F-4 telemetri (KVKK aydınlatması olmadan açılamaz)
- Ödeme sağlayıcı entegrasyonu, kredi kartı, iyzico/Stripe
- Deneme sonunda otomatik veri silme/arşivleme (yalnız salt-okunur)
- E-posta ile davet gönderimi (SMTP yok; jeton yönetici eliyle iletilir) — 1.1
- Sistem yöneticisi için ayrı kullanıcı/rol hiyerarşisi ve oturum yönetimi
- Çoklu deneme uzatma iş akışı/onay zinciri
- Kiracı silme (hard delete) — yalnız pasife alma

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| tenants | DEGISTIR: 3 kolon eklenir | durum VARCHAR(16) NOT NULL DEFAULT 'AKTIF' (DENEME|AKTIF|SALT_OKUNUR); deneme_bitis DATETIME NULL; davet_id VARCHAR(36) NULL | IX_tenants_durum_deneme_bitis (durum, deneme_bitis) | KiraciDenemeVeDavet |
| kiraci_davetleri | YENI tablo. Kiracı-ÖNCESİ kayıt olduğu için IKiraciVarlik DEĞİLDİR (Tenant gibi); KiraciYazmaDenetcisi kapsamı dışında kalır — bu durum sınıf başında yorumla ve KiraciFiltreTestleri'ne eklenen bir testle açıkça belgelenir. | id ULID PK; jeton_hash VARCHAR(100) (ham jeton SAKLANMAZ); email VARCHAR(200); isletme_adi VARCHAR(200); deneme_gun INT; gecerlilik DATETIME; kullanildi_mi BOOL; kullanan_tenant_id VARCHAR(36) NULL; iptal BOOL; olusturan_not VARCHAR(200); olusturuldu/guncellendi | UX_kiraci_davetleri_jeton_hash (UNIQUE); IX_kiraci_davetleri_gecerlilik_kullanildi | KiraciDenemeVeDavet |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/auth/kayit` | Anonim (davet jetonu şart) | DEGISTIR: KayitDto'ya [Required] DavetJetonu eklenir. Jeton hash'i eşleşmiyor / süresi geçmiş / kullanılmış / iptal ise 403 ForbiddenException. Kiracı DENEME durumunda ve deneme_bitis = now + davet.DenemeGun ile doğar; davet aynı tx'te kullanildi_mi=true. | OranSiniri.Kayit + DavetKapisi (fail-closed) |
| POST | `/api/v1/sistem/davetler` | Sistem anahtarı (X-Sistem-Anahtari) | Davet üretir; ham jeton YALNIZ bu yanıtta bir kez döner, sunucuda hash saklanır. Gövde: email, isletmeAdi, denemeGun (1-90), gecerlilikSaat (varsayılan 72), not. | SistemAnahtariKapisi + OranSiniri.Kayit |
| GET | `/api/v1/sistem/davetler` | Sistem anahtarı | Davet listesi (jeton hash'i DÖNMEZ; durum: BEKLIYOR/KULLANILDI/SURESI_GECTI/IPTAL). | SistemAnahtariKapisi |
| PATCH | `/api/v1/sistem/davetler/{id}` | Sistem anahtarı | Daveti pasife alır (iptal=true). UX-S3: gövdede olmayan alan korunur. Kullanılmış davet iptal edilemez (400). | SistemAnahtariKapisi |
| GET | `/api/v1/sistem/kiracilar` | Sistem anahtarı | Kiracı listesi: id, ad, plan, durum, deneme_bitis, kalanGun, kullanıcı sayısı, son giriş. Sorgu filtresi devre dışı bırakılmaz — SistemKapsami.Sistem() ile okunur (SistemKapsami.cs:30). | SistemAnahtariKapisi |
| PATCH | `/api/v1/sistem/kiracilar/{id}` | Sistem anahtarı | Süre uzatma / durum değiştirme: denemeBitis (ileri tarih), durum (DENEME|AKTIF|SALT_OKUNUR), gerekce (>=20 karakter zorunlu). Denetim kaydı yazılır. Kiracı pasife alma mevcut aktif alanıyla yapılır; tablo silinmez. | SistemAnahtariKapisi |
| GET | `/api/v1/admin/tenant` | SAHIP | DEGISTIR: yanıta durum, denemeBitis, kalanGun, saltOkunurMu eklenir (AdminServisi.TenantGetir, AdminModulu.cs:57-63). Yeni alan eklenir, mevcut alan adı değişmez. | [RolGerekli("SAHIP")] (AdminModulu.cs:106) |

## Servis ve dosyalar
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Tenant'a Durum/DenemeBitis/DavetId eklenir (52-75 arası); yeni KiraciDaveti sınıfı (IKiraciVarlik DEĞİL, gerekçe yorumda).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — DbSet<KiraciDaveti> Davetler + benzersiz jeton_hash indeksi + durum/deneme_bitis bileşik indeksi. HasQueryFilter EKLENMEZ (kiracı-öncesi tablo).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Sistem/SistemModulu.cs` — DTO + SistemServisi (DavetUret/DavetListe/DavetIptal/KiraciListe/KiraciGuncelle) + SistemController. Tek dosya kalıbı (Modules/*Modulu.cs). Jeton: Kimlik.Ulid() + rastgele; hash BCrypt/SHA-256 — mevcut hash yardımcısı yeniden kullanılır.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Sistem/SistemAnahtariKapisi.cs` — IAsyncAuthorizationFilter. Sistem:Anahtari yapılandırması boş, 32 karakterden kısa ya da 'DEGISTIR' ile başlıyorsa uç KAPALI (503/403) — fail-closed. Karşılaştırma CryptographicOperations.FixedTimeEquals. Anahtar loglanmaz/ekrana basılmaz.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Sistem/KiraciDurumKapisi.cs` — Global MVC yetkilendirme filtresi: kimliği doğrulanmış istek + mutasyon fiili (POST/PUT/PATCH/DELETE) ise tenants.durum okunur; SALT_OKUNUR ya da (DENEME ve deneme_bitis < now) ise ForbiddenException. Muaf: /auth/*, /sistem/*, /disa-aktarim*. Mevcut kapı deseni: GunKapanisiKapisi.cs.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — AddControllers(o => o.Filters.Add<KiraciDurumKapisi>()); AddScoped<SistemServisi>() (mevcut kapı kayıt satırı: Program.cs:71). Kısa ömürlü bellek içi durum önbelleği (30 sn) ile istek başına ek sorgu maliyeti sınırlanır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` — KayitDto.DavetJetonu [Required]; Kayit() içinde davet doğrulama + tüketme aynı transaction'da (mevcut tx bloğu genişletilir); Tenant.Durum='DENEME', DenemeBitis set edilir. İkinci kayıt yolu AÇILMAZ.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs` — TenantGetir yanıtına durum/denemeBitis/kalanGun/saltOkunurMu eklenir. Plan/AI uçlarına dokunulmaz (ayrı DTO ilkesi, AdminModulu.cs:29-46).
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — "Sistem": { "Anahtari": "DEGISTIR-en-az-32-karakter", "VarsayilanDenemeGun": 14, "DavetGecerlilikSaat": 72 } — rakam koda gömülmez (ADR-0005).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Isletme.razor` — Üstte deneme bandı: durum rozeti (renk + metin, UX-S7), kalan gün, salt-okunur uyarısı ve 'Verilerinizi dışa aktarın' bağlantısı (/veri-disa-aktarim). Sunucu son sözü söyler notu korunur.
- **YENI (Faz B)** `dotnet/Kuyumcu.Web/Pages/SistemKiracilar.razor` — @page "/sistem/kiracilar". Anahtar oturum boyunca sessionStorage'da tutulur, ASLA localStorage'a yazılmaz ve ekranda maskelenir. Liste + Çekmece ile süre uzatma + davet üretme. Ham jeton bir kez gösterilir, kopyala düğmesiyle.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs` — DEĞİŞMEZ tercih: /sistem/kiracilar basit modda da, profesyonel modda da normal kiracı menüsünde GÖRÜNMEZ (yönetici konsolu, adresle girilir). Karar tek yerde kalır (BasitMenu.cs:26-35).

## Ekranlar
- `/isletme` · `dotnet/Kuyumcu.Web/Pages/Isletme.razor` — Deneme/salt-okunur bandı: durum rozeti + kalan gün + dışa aktarım bağlantısı. Salt-okunur iken sayfadaki tüm kaydet düğmeleri disabled (DoD: düğme ya istek atar ya disabled) ve nedeni yazılı. (menü/yetki: SAHIP (mevcut ekran yetkisi korunur))
- `/sistem/kiracilar` · `dotnet/Kuyumcu.Web/Pages/SistemKiracilar.razor` — Faz B yönetici konsolu: kiracı listesi (ad, durum, kalan gün, kullanıcı sayısı), Çekmece ile süre uzatma (gerekçe zorunlu), davet üret/iptal. BosDurum (UX-S8), tek düzenleme kalıbı (UX-S1/S3), ID öneki 'sk-' (UX-S4), yalnız belirteç renkleri (UX-S6). (menü/yetki: Menüde YOK; erişim sistem anahtarıyla, son söz sunucuda)

## İş kuralları (fail-closed)
- Kendi kendine kayıt KAPALI: geçerli davet jetonu olmayan POST /auth/kayit 403 döner; hiçbir yan etki (tenant/şube/kullanıcı) oluşmaz.
- Davet tek kullanımlıktır ve tüketimi kayıt transaction'ının İÇİNDEDİR; eşzamanlı iki istekten yalnız biri başarılı olur (jeton_hash UNIQUE + kullanildi_mi güncellemesi).
- Ham jeton veritabanında saklanmaz; yalnız üretim yanıtında bir kez döner. Loglara, denetim kaydına ve hata mesajına yazılmaz.
- Sistem anahtarı yapılandırmada yoksa/varsayılansa /api/v1/sistem/* uçları KAPALIDIR (fail-closed); 'anahtar yok = serbest' davranışı yasak.
- Deneme bitince yazma kapanır: durum SALT_OKUNUR ya da deneme_bitis geçmişse tüm POST/PUT/PATCH/DELETE 403. Bilinmeyen/eksik durum değeri de KAPALI sayılır.
- Okuma ve dışa aktarım HER ZAMAN açıktır (veri rehin alınmaz): GET uçları ve /api/v1/disa-aktarim (POST + {jeton}/indir dahil) kapıdan muaftır.
- Kimlik doğrulama uçları (giris/yenile/cikis/ben) kapıdan muaftır; salt-okunur kiracı giriş yapıp verisini indirebilir.
- Süre uzatma yalnız sistem anahtarıyla ve >=20 karakter gerekçeyle yapılır; her uzatma/durum değişimi append-only denetim kaydı üretir (DenetimServisi).
- Tenant durumu için tek kaynak tenants.durum + deneme_bitis'tir; modüller kendi içinde ikinci bir süre kontrolü yazmaz (kapı tek yerde).
- Mevcut kiracılar migration sonrası durum='AKTIF', deneme_bitis=NULL ile açılır — hiçbir mevcut kiracının yazması kesilmez.
- Kiracı SİLİNMEZ; yalnız durum/aktif alanı ile pasife alınır (DoD: pasife alma).
- Kayıt ucu oran sınırı korunur (OranSiniri.Kayit); davet doğrulama hatası jeton var/yok ayrımını sızdırmayan tek tip mesaj döner.

## Testler
- `dotnet/Kuyumcu.Tests/KiraciKayitKilidiTestleri.cs` [DB (gerçek MySQL)]
  - Davetsiz_Kayit_403_ve_Hicbir_Tenant_Olusmaz
  - Gecersiz_Jeton_Kayit_Reddedilir_Kullanici_Tablosu_Degismez
  - Kullanilmis_Davet_Ikinci_Kez_Kabul_Edilmez
  - Suresi_Gecmis_Davet_Reddedilir
  - Iptal_Edilmis_Davet_Reddedilir
  - Gecerli_Davet_Tenant_DENEME_Durumunda_Ve_DenemeBitis_Dolu_Dogar
  - Ham_Jeton_Veritabaninda_Saklanmaz_Yalniz_Hash_Vardir
  - Es_Zamanli_Iki_Kayit_Ayni_Jetonla_Yalniz_Birini_Basarir
- `dotnet/Kuyumcu.Tests/KiraciDurumKapisiTestleri.cs` [HTTP (WebApplicationFactory)]
  - SaltOkunur_Kiraci_Satis_POST_403
  - Deneme_Suresi_Gecmis_Kiraci_Urun_PATCH_403
  - Deneme_Suresi_Gecmis_Kiraci_GET_Urunler_200
  - SaltOkunur_Kiraci_DisaAktarim_POST_ve_Indir_200
  - SaltOkunur_Kiraci_Giris_Yapabilir
  - AKTIF_Kiraci_Tum_Mutasyonlar_Etkilenmez
  - Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR
- `dotnet/Kuyumcu.Tests/SistemUclariTestleri.cs` [HTTP]
  - Anahtarsiz_Istek_403
  - Yanlis_Anahtar_403
  - Varsayilan_DEGISTIR_Anahtari_Ucu_Kapatir
  - Dogru_Anahtar_Kiraci_Listesi_Doner
  - SureUzatma_PATCH_DenemeBitisi_Ileri_Alir_Ve_Yazma_Yeniden_Acilir
  - Gerekcesiz_SureUzatma_400
  - SureUzatma_Denetim_Kaydi_Yazar
  - PATCH_Govdede_Olmayan_Alan_Korunur (UX-S3)
  - SAHIP_Jwt_Ile_Sistem_Ucuna_Erisilemez
- `dotnet/Kuyumcu.Tests/DenemeBandiEkranTestleri.cs` [bUnit]
  - Isletme_Deneme_Kalan_Gun_Rozeti_Gorunur
  - SaltOkunur_Iken_Kaydet_Dugmesi_Disabled_Ve_Neden_Yazili
  - SaltOkunur_Bandi_DisaAktarim_Baglantisi_Icerir
  - AKTIF_Kiracida_Band_Gorunmez
  - SistemKiracilar_Bos_Liste_BosDurum_Gosterir
  - SureUzatma_Cekmecesi_Gerekce_Bos_Iken_Kaydet_Disabled
- `dotnet/Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs` [saf]
  - KiraciDaveti_DuzeltmePolitikasi_Haritasinda_Kayitli (UX-S1)
  - Tenant_Durum_Alani_Politika_Haritasinda
  - KiraciDavetleri_IKiraciVarlik_Degil_Gerekce_Belgelendi

## Mutasyon kanıtları
- KiraciDurumKapisi'nde 'deneme_bitis < now' → 'deneme_bitis <= DateTime.MaxValue' yapılırsa Deneme_Suresi_Gecmis_Kiraci_Urun_PATCH_403 kırmızı.
- Kapı muafiyet listesine '/api/v1' eklenirse (hepsini muaf eder) SaltOkunur_Kiraci_Satis_POST_403 kırmızı.
- Muafiyetten '/disa-aktarim' silinirse SaltOkunur_Kiraci_DisaAktarim_POST_ve_Indir_200 kırmızı.
- Kayıt ucunda davet doğrulaması 'if (davet is null) return;' ile atlanırsa Davetsiz_Kayit_403_ve_Hicbir_Tenant_Olusmaz kırmızı.
- kullanildi_mi=true yazımı kaldırılırsa Kullanilmis_Davet_Ikinci_Kez_Kabul_Edilmez kırmızı.
- Davet tüketimi transaction dışına alınırsa Es_Zamanli_Iki_Kayit_Ayni_Jetonla_Yalniz_Birini_Basarir kırmızı.
- SistemAnahtariKapisi'nde 'anahtar boşsa geç' davranışına dönülürse Varsayilan_DEGISTIR_Anahtari_Ucu_Kapatir kırmızı.
- FixedTimeEquals yerine == kullanılsa testler yeşil kalır → bu yüzden denetim.sh'a 'Sistem anahtarı karşılaştırması' grep sayacı eklenir (kanıt: sayaç).
- Ham jeton KiraciDaveti.Jeton alanına da yazılırsa Ham_Jeton_Veritabaninda_Saklanmaz_Yalniz_Hash_Vardir kırmızı.
- PATCH'te gövdedeki null alan körü körüne yazılırsa PATCH_Govdede_Olmayan_Alan_Korunur kırmızı.
- Isletme.razor'da salt-okunur iken disabled kaldırılırsa SaltOkunur_Iken_Kaydet_Dugmesi_Disabled_Ve_Neden_Yazili kırmızı.
- Bilinmeyen durum değeri için 'default: izin ver' yazılırsa Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR kırmızı.

## Canlı doğrulama
- curl -i -X POST localhost:5198/api/v1/auth/kayit -d '{davetsiz gövde}' → 403 ve SELECT COUNT(*) FROM tenants sayısı DEĞİŞMEZ.
- curl -i -X POST localhost:5198/api/v1/sistem/davetler (anahtarsız) → 403; -H 'X-Sistem-Anahtari: <sır>' ile → 201 ve yanıtta ham jeton bir kez.
- SELECT jeton_hash, kullanildi_mi FROM kiraci_davetleri WHERE id='<id>' → hash dolu, ham jeton hiçbir kolonda yok (grep ile doğrula).
- Davetle kayıt → 201; SELECT durum, deneme_bitis FROM tenants WHERE id='<yeni>' → 'DENEME', now+14 gün.
- UPDATE tenants SET deneme_bitis = NOW() - INTERVAL 1 DAY WHERE id='<test>' → POST /api/v1/urunler 403, GET /api/v1/urunler 200.
- POST /api/v1/disa-aktarim + POST /{jeton}/indir → 200 (salt-okunur kiracıda bile).
- PATCH /api/v1/sistem/kiracilar/<id> (denemeBitis +30 gün, gerekçe) → 200; ardından POST /api/v1/urunler 201.
- SELECT COUNT(*) FROM denetim_kayitlari WHERE varlik='TENANT' AND islem='SURE_UZATMA' → uzatma başına 1 satır.
- Mevcut kiracıda migration sonrası SELECT durum FROM tenants → 'AKTIF' ve yazma kesilmemiş (POST /api/v1/urunler 201).
- Doğrulama sonunda test kiracısı ve daveti SİLİNİR (test verisi bırakılmaz).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Migrations/<zaman>_KiraciDenemeVeDavet.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sistem/SistemModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sistem/SistemAnahtariKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Sistem/KiraciDurumKapisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/SistemKiracilar.razor`

## Fazlar
- **Faz A — Kilit + durum + kapı (sevk edilebilir asgari)** (Dalga 3, T-1+F-6'dan hemen sonra): Migration (tenants 3 kolon + kiraci_davetleri); SistemModulu davet üret/listele/iptal + kiracı listesi + süre uzatma uçları; SistemAnahtariKapisi; kayıt ucunun davetle kilitlenmesi; KiraciDurumKapisi + muafiyet listesi; /admin/tenant yanıt genişletmesi; Isletme.razor deneme bandı; DB/HTTP/bUnit testleri + mutasyon kanıtları. Bu faz tek başına 'bedava sınırsız kayıt' bulgusunu kapatır; yönetim curl/HTTP ile yapılabilir.
- **Faz B — Yönetici konsolu ekranı** (Faz A yeşil olduktan sonra, aynı dalga içinde): /sistem/kiracilar Blazor ekranı: kiracı listesi, kalan gün rozeti, Çekmece ile süre uzatma (gerekçe zorunlu), davet üret/iptal, ham jetonu bir kez gösterme. Anahtar sessionStorage'da, maskeli. bUnit testleri + BosDurum + UX-S4/S6/S7/S8 uyumu. Basit/pro mod: ekran her iki modda da menüde görünmez; içerik profesyonel mod düzeninde.
## Mevzuat dayanağı
- 6698 s. KVKK — veri sorumlusunun verileri erişilebilir tutması ve ilgili kişinin verisini alabilmesi — **ORTA** — Deneme bitince verinin kilitlenip indirilememesi hukuki ve ticari risk. Bu yüzden dışa aktarım kapıdan muaf tutuldu. Hukuki nitelendirme DOĞRULANMALI (hukukçu görüşü).
- VUK 253/256 — defter ve belgelerin saklanması ve ibrazı (elektronik kayıtlar dâhil) — **ORTA** — Salt-okunur moda geçen kiracının geçmiş kayıtlarına erişimi ve dışa aktarımı kesilmemeli. Süre/kapsam detayları DOĞRULANMALI.
- 6502 s. TKHK / mesafeli sözleşmeler — deneme süresi ve otomatik ücretlendirme — **DÜŞÜK** — Bu pakette ÜCRETLENDİRME YOK (F-1 kapsam dışı), otomatik yenileme yok; deneme bitince yalnız yazma kapanıyor. Fiyat ilanı yapılmadığı için şimdilik tetiklenmiyor — DOĞRULANMALI.

## Riskler
- Global mutasyon kapısının muafiyet listesi hatalı yazılırsa ya güvenlik açığı (hepsi muaf) ya da işi durduran hata (dışa aktarım kapalı) doğar — iki yönlü test zorunlu.
- Her mutasyon isteğinde tenants okuması ek gecikme getirir; 30 sn'lik bellek içi önbellek eklenmezse sıcak yolda ölçülebilir yavaşlama. Önbellek süresi uzun tutulursa uzatma gecikmeli etki eder.
- Sistem anahtarı tek sırdır; sızarsa tüm kiracı listesi ve süre yönetimi açılır. Rotasyon prosedürü T-1 yayın paketinde tanımlanmalı.
- Ham jetonun yönetici tarafından e-posta/WhatsApp ile iletilmesi kanal riski taşır; kısa geçerlilik (72 saat) bunu sınırlar ama ortadan kaldırmaz.
- kiraci_davetleri tablosu IKiraciVarlik olmadığı için KiraciYazmaDenetcisi korumasının dışındadır; yalnız sistem anahtarı korur — tek katman.
- Kayıt ucunun kilitlenmesi mevcut demo/otomasyon akışlarını (varsa) kırabilir; HttpOrtami/TestOrtami kayıt kullanan testler toplu güncellenmeli.
- SistemKiracilar.razor'da anahtarın tarayıcıda tutulması XSS halinde sızdırır; CSP sıkı olsa da bu bir kabul edilen risktir, Faz B öncesi Mehmet onayı gerekir.
- Migration'da durum varsayılanı yanlış ('DENEME') verilirse tüm mevcut kiracılar süre dolduğunda yazamaz hale gelir — üretim durdurucu; migration testi şart.

## Açık sorular
- Sistem yöneticisi kimliği: yapılandırma sırrı (bu tasarımın varsayımı) mı, yoksa rol hiyerarşisine 'SISTEM' eklemek mi? İkincisi AktifKullanici.cs:11 ve tüm [RolGerekli] tam-eşleşme listelerini etkiler — ekip oyu gerekir.
- Varsayılan deneme süresi kaç gün? Tasarımda 14 varsayıldı (yapılandırmadan okunur) — kurucu/satış kararı DOĞRULANMALI.
- Deneme bitince yazma kapanması otomatik mi (deneme_bitis geçince) yoksa yalnız durum SALT_OKUNUR'a çekilince mi? Tasarım ikisini de kapatıyor; otomatik olan sürpriz yaratabilir.
- Salt-okunur kiracıda okuma/dışa aktarım ne kadar süre açık kalacak (süresiz mi)? Veri saklama/silme politikası bu pakette tanımlı değil.
- MASAK/denetim gibi zorunlu arka plan yazmaları salt-okunur kiracıda ne olacak? Arka plan işleri HTTP kapısından geçmediği için etkilenmez — ancak kasıtlı mı, teyit gerekir.
- Faz B ekranı bu üründe mi yoksa ayrı bir yönetim uygulamasında mı yaşamalı? Aynı WASM paketinde olması saldırı yüzeyini büyütür.

## Oylama
- **YENİ: Baş Mimar (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu/sevk edilebilirlik, dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı elle doğrulandı (Program.cs, appsettings.json, AlanSifreleme.cs, Entities.cs, DuzeltmePolitikasi.cs, CariModulu.cs, KasaModulu.cs, yedek-al-ve-dogrula.sh, deploy/). NOT: bana yalnızca T-1, T-2 ve T-3 tasarım metni ulaştı; T-3 metni 'mevzuat' bölümünde kesildi, 4. ve 5. paketin metni HİÇ gelmedi — Anayasa md.1 gereği görmediğim tasarıma oy vermiyorum.: KOSULLU** — Hedef (bedava-sınırsız kiracı açmayı kapatmak) kanıtlı bir güvenlik boşluğunu kapatıyor ve tasarım fail-closed omurgasında doğru: davet tüketimi kayıt transaction'ının içinde, ham jeton saklanmıyor, dışa aktarım ve okuma kapı dışında (KVKK/VUK gerekçesi ikna edici), mevcut kiracılar migration sonrası AKTIF doğuyor. Veri modeli de doğru: Tenant bugün durum/süre taşımıyor, kiraci_davetleri kiracı-öncesi olduğu için IKiraciVarlik olmaması tutarlı ve denetim.sh'ın IKiraciVarlik sayacı asgari eşik olduğu için bu ekleme denetimi kırmaz. Ancak metindeki üç mimari dayanak kaynakta doğrulanmadı (kapı deseni, TenantGetir DTO'su, BasitMenu'nün izin-listesi olması) ve bunlar düzeltilmeden uygulanırsa ya yanlış mekanizma kopyalanır ya gereksiz dosya çakışması doğar. Paket boyutu iki fazla makul; Faz A tek başına bulguyu kapatıyor. Bu yüzden EVET değil, yukarıdaki 8 koşulla KOSULLU.
  - Koşullar: SistemKapsami.cs:17-23 'İZİN VERİLEN KULLANIM — tam liste (başka yok)' 4 maddedir; /sistem/kiracilar okuması 5. kullanımdır. Yorum listesi aynı commit'te 5. madde ile genişletilmeden SistemKapsami.Sistem() (SistemKapsami.cs:30) çağrılmayacak; aksi hâlde mühürlü ADR ihlali. · Kapı deseni dayanağı YANLIŞ: GunKapanisiKapisi.cs:15 'public static class' (servisten çağrılan yardımcı), TevsikKapisi ise Program.cs:71'de AddScoped servis kapısı. Kod tabanında global MVC yetkilendirme filtresi yok. KiraciDurumKapisi yeni mekanizma olarak ADR'ye yazılacak. · Program.cs'te ikinci bir AddControllers açılmayacak; filtre mevcut '.AddControllers()' zincirine (Program.cs:155) o=>o.Filters.Add<KiraciDurumKapisi>() olarak eklenecek. Filtre MVC dışı uçları (minimal API/statik) kapsamadığı için kapsam dışı yollar listelenip belgelenecek. · Muafiyet eşleşmesi substring ile yapılmayacak: gerçek rota DisaAktarimModulu.cs:330 '[Route("api/v1/disa-aktarim")]'. Muafiyet normalize edilmiş yol üzerinde segment sınırıyla ('/api/v1/disa-aktarim' == veya + '/') karşılaştırılacak; '/api/v1' benzeri geniş önek testle yasaklanacak. · AdminServisi.TenantGetir (AdminModulu.cs:55-60) Tenant VARLIĞINI doğrudan döndürüyor; kalanGun/saltOkunurMu hesaplanamaz ve yeni kolonlar otomatik sızar. Uçta açık TenantDto projeksiyonu eklenecek, entity dönüşü kaldırılacak; mevcut alan adları değişmeyecek. · BasitMenu.cs:26-35 bir İZİN LİSTESİDİR (Yollar dizisi); /sistem/kiracilar zaten görünmez. 'DEGISTIR BasitMenu.cs' kalemi ve dosyası ayak izinden ÇIKARILACAK; bunun yerine profesyonel mod menü kaynağına ekleme YAPILMADIĞI testle kanıtlanacak. · Sistem:Anahtari T-1 ortam-değişkeni sır hattından okunacak; appsettings.json'a yalnız 'DEGISTIR-...' yer tutucu yazılacak ve bu değerle uçlar KAPALI kalacak (SistemAnahtariKapisi fail-closed + FixedTimeEquals + denetim.sh grep sayacı). · Sistem yöneticisi kimliği ('yapılandırma sırrı' mı 'SISTEM' rolü mü) kod yazılmadan ekip oyuyla kapatılacak: AktifKullanici.cs:11 Seviye() tam-eşleşme sözlüğü ve '_ => 0' varsayılanı rol seçeneğinde tüm [RolGerekli] listelerini etkiler.
- **YENİ: GÜVENLİK (Mehmet) — kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek şifreleme, içe aktarım riski, abonelik/plan kapılarının yazma yolunu bozmaması, telemetri/hata paketinde kişisel veri, MFA koşulu.: KOSULLU** — Tasarım fail-closed ilkelerine (varsayılan/boş sırla kapalı, FixedTimeEquals, bilinmeyen durum=kapalı, okuma/dışa aktarım muaf) sağlam biçimde uyuyor ve mutasyon kanıtları güvenlik açısından kritik noktaları (anahtar karşılaştırması, muafiyet listesi, ham jeton saklanmaması) kapsıyor. Ancak üç açık nokta veto gerektirmeyecek ama kodlamayı bağlayacak koşullara ihtiyaç duyuyor: (1) sistem yöneticisi kimlik modeli henüz kapanmamış bir mimari karar, (2) tarayıcıda anahtar tutma riski belgenin kendisi tarafından Mehmet onayına bağlanmış ama oylamaya konu edilmemiş, (3) 30sn önbellek + 45-API bağımlılığı üretim güvenliği için net sıra ve test kapsamı istiyor.
  - Koşullar: KiraciDurumKapisi 30sn önbellek AKTİF→SALT_OKUNUR yönünde de test edilsin; şu an testler yalnız uzatma yönünü kapsıyor (Program.cs:58 belge; KiraciDurumKapisiTestleri.cs listesinde kısıtlama-yönü testi yok). · SistemKiracilar.razor'da anahtar sessionStorage'da (dosya:179 'Mehmet onayı gerekir' yazıyor) — Faz B canlıya alınmadan ÖNCE ayrı CSP/XSS onayı şart, otomatik dahil sayılmasın. · Sistem yöneticisi kimliği kararı (yapılandırma sırrı vs SISTEM rolü, dosya:183) kodlamadan ÖNCE ayrı ekip oyuyla kapatılmalı; bu paket kararı varsaymış, kodlama bu karara kilitlensin. · Sistem:Anahtari üretimde yalnız T-1 ortam-değişkeni sır hattından gelsin; appsettings.json'da yalnız DEGISTIR placeholder kalsın, denetim.sh'a placeholder-sızıntı sayacı eklensin (dosya:61,196). · FixedTimeEquals mutasyon kanıtı (dosya:131) korunsun, PR/kod incelemesinde == kullanımına izin verilmesin. · /api/v1/sistem/* uçları 45-API (MFA) tamamlanmadan üretime açılmasın; T-4a ile Program.cs/appsettings çakışması yaz-seri sırayla önceden sabitlensin (dosya:3,199). · GET /sistem/davetler yanıtındaki email/isletmeAdi alanları için erişim amaçlı loglama ya da maskeleme netleştirilsin; şu an yalnız jeton_hash döndürülmüyor, PII döndürülüyor (dosya:46). · kiraci_davetleri KiraciYazmaDenetcisi kapsamı dışı, tek katman koruması sistem anahtarı (dosya:177) — bu kabul edilen risk olarak SESSION-HANDOFF'a da yazılsın, unutulmasın.
- **YENİ: Aşkın (QA) merceğiyle: gerçek kodda spot-doğrulama yaptım (appsettings.json:9-19 DEGISTIR sırları, Program.cs:22 jwtAyarlar, Program.cs:219 MapHealthChecks, Program.cs:304 UseHttpsRedirection, Entities.cs:24-32 HareketTurleri ve 'dizi kodda ZORLANMAZ' yorumu, CariModulu.cs:684-722 CARI_DUZELTME+OdemeIzi.Yok, KasaModulu.cs:134-145 HareketGir+GunKapanisiKapisi, HareketFabrikasi.cs:79 Ekle imzası, yedek-al-ve-dogrula.sh:36-76 docker exec bağımlılığı, docker-compose.yml:7-10 düz parola+3306 dışa açık). Üç tasarımın (T-1, T-2, T-3) dosya:satır iddiaları büyük ölçüde kanıtlı ve doğru. Genel değerlendirme: mutasyon kanıtları ('X eklenince/kaldırılınca Y testi KIRMIZI' formatı) her üç pakette de gerçek kural ihlalini hedefliyor, süsleme değil; DB/HTTP/bUnit katman dengesi makul; canlı doğrulama adımları ölçülebilir (çıkış kodu, dosya boyutu, SQL COUNT, HTTP durum kodu, 'file' komutuyla şifreleme kanıtı) ve T-1/T-2'de test kiracısı temizliği kanıtla kapatılıyor. Buna karşın: (a) yalnız 3 paket tasarımı (T-1, T-2, T-3) tam olarak elime ulaştı — görevde '5 paket' deniyor ama veri 3 ile sınırlı ve T-3'ün son bölümleri (riskler/açık sorular/tahmin) metin ortasında kesilmiş; bu eksik iki paket için oy veremedim ve T-3 için tam onay veremiyorum. (b) T-2'de kredi limiti kapısı konusunda tasarımın kendi 'uçlar' bölümü ile 'açık_sorular' bölümü birbiriyle çelişiyor ve bu belirsizlik hiçbir teste yansımamış — kararsızlık riski taşıyan gerçek bir kalite açığı. (c) T-1'de statik açılış denetçisinin paralel test izolasyonu ve 'bekleyen migration' canlı doğrulamasının hangi şemada koşacağı netleşmemiş; yanlış şemada çalıştırılırsa geliştirme migration geçmişini bozma riski var. (d) T-3'te görünen canlı doğrulama listesinde T-1/T-2'dekine benzer açık bir 'test kiracısı/geçici şema silindi' kapanış kanıtı yok.: KOSULLU** — Belge test-kural eşleşmesinde genel olarak güçlü: her önemli fail-closed kuralı için ayrı mutasyon kanıtı satırı var (satır 125-136) ve DB/HTTP/bUnit/saf dağılımı doğru katmanlarda (eşzamanlılık DB'de, kapı davranışı HTTP'de, ekran durumu bUnit'te, politika kaydı saf testte). Ancak üç somut boşluk kalıcı regresyon riski taşıyor: (1) eşzamanlılık testinin gerçekten paralel çalıştığı belirtilmemiş — DB unique constraint yarışını sıralı çağrıyla yakalamak mümkün değil; (2) 30sn önbellek riski (satır 174'te ayrıca işaretlenmiş) hiçbir testle korunmuyor, yalnız canlı curl adımına güveniliyor ki bu commit sonrası CI'de tekrar doğrulanmaz; (3) migration varsayılan-durum riski açıkça 'üretim durdurucu' diye işaretlenmiş (satır 180) ama otomatik test yok, yalnız elle SQL kontrolü var. Bunlar mühürlenmeden Faz A 'sevk edilebilir asgari' iddiası (satır 165) kanıtsız kalır.
  - Koşullar: Es_Zamanli_Iki_Kayit_Ayni_Jetonla_Yalniz_Birini_Basarir gerçek paralel (Task.WhenAll, ≥5 eşzamanlı deneme) gerçek MySQL'e karşı koşulsun; sıralı simülasyon kabul edilmez (Testler §, satır 93). · Program.cs'teki 30sn bellek-içi durum önbelleği (satır 58) için ayrı test: SureUzatma sonrası önbellek geçersiz kılınır/kısa sürede 403→201 geçişi doğrulanır; yoksa Canlı doğrulama madde 7 (satır 145) flaky kalır. · Migration varsayılan durum='AKTIF' regresyonu (Riskler satır 180'de üretim-durdurucu denilen risk) yalnız Canlı doğrulama madde 8'de (satır 147) kontrol ediliyor; CI'de yakalanmaz — otomatik migration/entegrasyon testi Testler §'e eklensin. · Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR testinin (satır 101) bilinmeyen durum değerini nasıl ürettiği (raw SQL/DbContext bypass, VARCHAR(16) enum kısıtı yok) test dosyasında açıkça yazılsın. · FixedTimeEquals mutasyonu testle yakalanmıyor, yalnız denetim.sh grep sayacıyla kapanıyor (satır 132) — bu sayaç denetim.sh'a Faz A DoD'una zorunlu madde olarak eklensin, 'test yeşil ama kanıt yok' riski açıkça işaretli kalsın. · Canlı doğrulama sonunda yalnız test tenant siliniyor (satır 148); ilişkili kiraci_davetleri kaydı da (kullanan_tenant_id ile) temizlensin — yoksa test verisi tabloyu kirletir.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- SistemKapsami.cs:17-23 'tam liste (başka yok)' yorumu aynı commit'te 5. kullanım (/sistem/kiracilar okuması) ile genişletilmeden SistemKapsami.Sistem() çağrılmaz; aksi hâlde mühürlü ADR ihlali.
- KiraciDurumKapisi YENİ mekanizma olarak ADR'ye yazılır (GunKapanisiKapisi static yardımcı, TevsikKapisi scoped servis; global MVC filtresi kod tabanında yok). MVC dışı (minimal API/statik) kapsam dışı yollar listelenip belgelenir.
- İkinci AddControllers açılmaz: filtre mevcut .AddControllers() zincirine (Program.cs:155) o=>o.Filters.Add<KiraciDurumKapisi>() olarak eklenir.
- Muafiyet eşleşmesi substring ile değil, normalize yol üzerinde segment sınırıyla ('/api/v1/disa-aktarim' == veya + '/'); '/api/v1' gibi geniş önek testle yasaklanır.
- AdminModulu.cs:55-60 TenantGetir entity dönüşü kaldırılır, açık TenantDto projeksiyonu eklenir (yeni kolonlar sızmaz); mevcut alan adları değişmez.
- BasitMenu.cs bir izin listesidir; ayak izinden ÇIKARILIR ve profesyonel mod menü kaynağına ekleme YAPILMADIĞI testle kanıtlanır.
- Sistem:Anahtari yalnız T-1 ortam-değişkeni sır hattından okunur; appsettings.json'da sadece 'DEGISTIR-' yer tutucu kalır ve bu değerle uçlar KAPALI (fail-closed); denetim.sh'a placeholder-sızıntı sayacı eklenir; FixedTimeEquals zorunlu, '==' yasak.
- Sistem yöneticisi kimliği (yapılandırma sırrı mı SISTEM rolü mü) kod yazılmadan ayrı ekip oyuyla kapatılır; AktifKullanici.cs:11 tam-eşleşme sözlüğü ve '_ => 0' varsayılanı tüm [RolGerekli] listelerini etkilediği için karar kodlamayı kilitler.
- Testler: eşzamanlılık gerçek paralel (Task.WhenAll, ≥5) gerçek MySQL'e karşı; 30sn önbellek için AKTIF→SALT_OKUNUR yön testi; migration varsayılan durum='AKTIF' için otomatik entegrasyon testi; bilinmeyen durum değerinin nasıl üretildiği test dosyasında yazılır.
- sessionStorage'da anahtar tutma Faz B canlı öncesi ayrı CSP/XSS onayına bağlıdır; /api/v1/sistem/* uçları 45-API (MFA) tamamlanmadan üretime açılmaz; GET /sistem/davetler PII (email/işletme adı) için maskeleme ya da erişim logu netleşir; canlı doğrulamada test tenant + ilişkili kiraci_davetleri temizlenir; tek-katman koruma riski SESSION-HANDOFF'a yazılır.

### Mühür 4 eki (2026-09-16 14:56) — sistem yöneticisi kimliği, denetim izi, Faz B ertelemesi (BAĞLAYICI)

> Kod öncesi açık kararlar oylaması (5 mercek KOŞULLU, veto yok; ana belge § MÜHÜR 4). Bu ek tasarım metninin ÜSTÜNDEDİR: çelişen yerde ek geçerlidir. Faz B (SistemKiracilar.razor) dalga 3 dosya ayak izinden ÇIKARILDI (dalga 6, T-4b yanı). X-Sistem-Operator başlığı tasarım metninde yoktu — bu ekle EKLENDİ (uç sözleşmesi: /api/v1/sistem/* mutasyonlarında zorunlu, ≥3 karakter; yoksa 400).

- **K1** — F-2-min sistem yöneticisi kimliği (A) YAPILANDIRMA SIRRI olarak mühürlenir; 'SISTEM' rolü AÇILMAZ. AktifKullanici.cs:11 Seviye sözlüğüne, [RolGerekli] listelerine, BasitMenu.cs:26-35 ve KenarCubugu.razor RolluGorunur satırlarına DOKUNULMAZ; değişmediği testle kanıtlanır. Beş mercek de (A) dedi (F-2-min:3,10,183,206).
- **K2** — Tek kimlik hattı: SistemAnahtariKapisi. Sistem:Anahtari yalnız T-1 ortam-değişkeni sır hattından okunur; appsettings.json'da yalnız DEGISTIR- yer tutucu kalır (fail-closed), FixedTimeEquals zorunlu, dotnet/denetim.sh'a yer-tutucu sızıntı sayacı eklenir. Anahtar değeri hiçbir log/denetim kaydına yazılmaz (F-2-min:61,131-132,205).
- **K3** — Denetim izi 'kim çağırdı' kapatılır: /api/v1/sistem/* her mutasyonda zorunlu X-Sistem-Operator başlığı (>=3 krktr) + gerekce (>=20 krktr) + T-4a X-Istek-Kimligi + kaynak IP, DenetimServisi kaydına yazılır; başlıksız istek 400. NOT: operatör başlığı tasarım metninde YOK, bu turda eklenen koşuldur — uygulama öncesi paket metnine işlenmesi DOĞRULANMALI.
- **K4** — /api/v1/sistem/* uçları 45-API (MFA) tamamlanmadan ÜRETİME AÇILMAZ (Mehmet, veto yetkili; F-2-min:193,208). Bu bir takvim kapısıdır, teknik ikame değildir; anahtar kullanıcı olmadığı için MFA onu korumaz. Bu yüzden Bertan'ın koşulu EKLENİR: T-1 nginx şablonunda /sistem/* için ağ/vekil (IP) kısıtı zorunludur.
- **K5** — F-2-min Faz B (SistemKiracilar.razor + sessionStorage) dalga 3'ten ÇIKARILIR, dalga 6'ya T-4b yanına alınır; dosya çakışma planında dalga 3 bu dosyayı rezerve etmez. Faz A tek başına bulguyu kapatır ve sevk edilebilir (F-2-min:56). Erteleme kapsam kaybı sayılmaz.
- **K6** — Pilot davetleri curl + X-Sistem-Anahtari ile üretilir. Runbook (curl örneği + SMS/WhatsApp'a kopyalanabilir davet metni şablonu; SMTP yok, F-2-min:19) T-1'in DEPLOY.md yeniden yazımıyla AYNI belge kökünde (docs/) yayımlanır; ikinci docs kökü açılmaz (T-1:204, MÜHÜR 3 tek yayın yolu).
- **K7** — Faz B canlıya alınmadan ÖNCE ayrı CSP/XSS onayı şarttır, otomatik dahil sayılmaz (Mehmet). Program.cs:281-291 CSP korunur, script-src'ye 'unsafe-inline'/'unsafe-eval' EKLENMEZ; ayrıca style-src 'unsafe-inline' (Program.cs:284) nonce/hash ile kaldırılır YA DA Faz B ayrı origin/paket olur (F-2-min:79).
- **K8** — Faz B'de sistem anahtarı TERCİHEN yalnız bellekte tutulur (sayfa yenilemede yeniden sorulur); tavan sessionStorage'dır, localStorage her koşulda YASAK (F-2-min:63). Anahtar DOM'a, URL'ye, konsola, hata gövdesine, telemetriye basılmaz; bUnit + canlı doğrulama ile kanıtlanır. id öneki 'sk-' (F-2-min:68) dalga 6'da da korunur.
