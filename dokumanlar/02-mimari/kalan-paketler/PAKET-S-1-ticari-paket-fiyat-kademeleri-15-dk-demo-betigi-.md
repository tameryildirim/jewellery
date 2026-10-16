# Paket S-1 — Ticari paket: fiyat kademeleri, 15 dk demo betiği, gerçek görünümlü tanıtım kiracısı, SLA ve pilot referans planı

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: Faz A 3-4 gün (tohumlama ucu+betik+testler 1,5-2 gün · beş belge 1,5-2 gün) · Faz B 1-1,5 gün (F-8 rakamları + pilot sonuçları işlenince). Kurul takviminin 'S-1 2-3 gün' tahmini tohumlama KODUNU ve dört test dosyasını içermiyordu; fark bilinçlidir (Madde 1). gün · Önkoşullar: F-8 birim ekonomi tablosu (dalga 3, paralel belge hattı) — fiyat kademelerinin ÖN ŞARTI; dolmadan rakam ilan edilmez., Dalga 2b + 2c tamamı (UX-1-B, UX-1-C1..C4, 48-A storno, 38-A KDV, 45-API/UI, UX-4d) — demo 'yanlış girdim, düzelttim' anını gösteremezse betik dürüst koşmaz., QA turu 2 + hazırlık kurulu turu 2 — bunlar bitmeden ürün yalnız ücretsiz pilot (Eray'ın bağlayıcı kapısı)., 34-A / 35-A rapor mutabakatı — kâr/zarar adımı (demo 9-12. dk) mutabık rakam olmadan yapılamaz., T-3 yedek otomasyonu + T-4a sürüm/derleme damgası — SLA'nın yedek ve destek izlenebilirliği sözleri bunlara dayanır., T-1 + F-6 üretim yayın paketi — kurulum SLA'sının 'ilk ücretli kurulum' tarihi buna bağlı., 47-A veri içe aktarım (kapılı, kuru provalı) + mevcut Disaaktarim — 'veri taşıma sözü' belgesinin kod dayanağı., Kurucu kararı: canlı DB'deki 2 sızıntı satırının temizlenmesi (ORKESTRATOR-DURUM.md:57) — demo öncesi zorunlu., Kurucu imzası: e-Fatura entegratör sözleşmesi + mali mühür, yaptırım listesi aboneliği, alan adı/TLS — fiyat maliyet satırlarının kaynağı. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 6
> ⚠ **VETO:** YENİ: BAŞ MİMAR (Bertan) merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilirlik), dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı bizzat doğrulandı (Entities.cs Tenant bloğu, Program.cs CSP + statik boru hattı, denetim.sh yüzeyi, wwwroot/index.html, AdminModulu plan yazma yolu, AuthServisi.Kayit). NOT (Madde 1): görevde "4 paket" denmesine rağmen bana ulaşan tasarım metni 3 pakettir (F-8, S-1, PWA-1); PWA-1 'mevzuat' bölümünün ortasında kesilmiş, 4. paket nesnesi hiç başlamamıştır. Olmayan tasarıma oy uydurulmadı.: İTİRAZ (bağlayıcı değil — veto yetkisi Mehmet'tedir): S-1 tek parça sevk edilemez. Bölünmeden (S-1-KOD / S-1-BELGE) ve 'ikinci kiracı-açma yolu yok' kuralı denetim.sh sayacıyla ölçülür hâle gelmeden mimari mühür vermem. AuthModulu.cs:87 Kayit tek bootstrap yoludur; tanıtım ucunun kendi INSERT'ini yazması kiracı izolasyonunda ikinci mekanizma demektir.

## Hedef
1.0 ile eşzamanlı yayımlanacak ticari paketi üretmek: F-8 kanıtına dayalı fiyat kademeleri, 15 dakikalık demo betiği, KODLA tohumlanan gerçek görünümlü "Tanıtım Kuyumculuk" kiracısı, veri taşıma sözü, kurulum/eğitim/destek SLA taslağı ve 3 pilotluk referans planı. Fiyat ilanı F-8 tablosu dolmadan yapılmaz.

## Kodda bugünkü durum
Ticari belge yok: dokumanlar/09-uygulama-kayitlari altında yalnız paket raporları + QA çizelgesi (HAZIRLIK-KURULU-1.md:91). Abonelik/fiyat katmanı yok; Tenant yalnız metin `Plan` taşıyor (dotnet/Kuyumcu.Api/Data/Entities.cs:56, varsayılan "BASLANGIC"), tek yazan yer AdminModulu.cs:64 PlanGuncelle. Kiracı açma yolu AuthServisi.Kayit (Modules/Auth/AuthModulu.cs:88-140: Tenant+Merkez Şube+SAHIP, tek transaction). Demo verisi yalnız test fikstürlerinde ("Demo Kuyumculuk", Kuyumcu.Tests/EkranTestleri.cs:429,627). Tohumlama betiği yok (dotnet/ altında denetim.sh, yedek-al-ve-dogrula.sh, yetim-tara.sh). 15 dk demo betiği yalnız kurul metninde nesir hâlinde (HAZIRLIK-KURULU-1.md:68). Canlı DB'de 2 kiracı sızıntısı kanıt satırı duruyor (ORKESTRATOR-DURUM.md:57) — demo ortamında gösterilemez.

## Kapsam
**İçinde:**
- Fiyat kademeleri belgesi: üç kademe (Tek Şube / Çok Şube / Uyum+), her hücre F-8 birim maliyet satırına referanslı; F-8 boşsa rakam hücresi 'DOĞRULANMALI' damgasıyla doğar ve ilan edilmez.
- 15 dakikalık demo betiği: dakika-dakika rota listesi, gösterilmeyecek ekranların açık listesi, her adımın ön koşulu ve kanıt ekran görüntüsü yeri.
- Tanıtım kiracısı tohumlama KODU: POST /api/v1/tanitim/kiraci ucu + dotnet/tanitim-tohumla.sh; veri gerçek uçlardan HTTP ile üretilir (ürün, cari, kasa, satış, hurda/gider pusulası, emanet, fiyat listesi, kur).
- Yeni varlık TanitimTohumu (IKiraciVarlik): kiracının tanıtım olduğunun damgası, PATCH + pasife alma + denetim kaydı + DegismezIpucu ile doğar.
- Ayarlar ekranına 'Tanıtım verisi' şeridi: rozet, tohum sürümü, pasife alma düğmesi (SAHIP); yeni sayfa açılmaz.
- Veri taşıma sözü belgesi: mevcut Disaaktarim (UTF-8 BOM + RFC 4180 + CSV enjeksiyon kaçışı) üzerine yazılı taahhüt; içe aktarım tarafı 47-A'ya atıf.
- Kurulum / eğitim / destek SLA taslağı: yanıt-çözüm süreleri, çalışma saatleri, kanal, yedek/geri alma sözü (T-3), hata kimliği ile destek akışı (T-4a/T-4b).
- 3 pilot referans planı: pilot protokolü, seçim ölçütleri, 6 haftalık takvim, ölçülecek 5 metrik, referans/vaka çalışması izin metni.
- Fiyat ilanı kapısı: F-8 + dalga 2b/2c + QA turu 2 + kurul turu 2 bitmeden ürün yalnız ÜCRETSİZ PİLOT (Eray koşulu).

**Dışında:**
- Abonelik/faturalama çekirdeği (F-1), deneme hunisi (F-2/F-2-min), plan kapısı (F-3), ölçüm (F-4) — S-1 yalnız belge ve tanıtım verisi üretir, para tahsil etmez.
- Ödeme sağlayıcı (iyzico/PayTR) entegrasyonu; sözleşme imzası, fiyat ilanı, teklif verme — Kurucu'da.
- Katman 1 imalat/atölye (kapsam dışı) senaryolarının demoya girmesi.
- e-Fatura/e-Arşiv (38-B) canlı gösterimi — entegratör imzasına bağlı, demoda yalnız yol haritası olarak söylenir.
- Rakip fiyatlarının birincil kaynaktan doğrulanması (PAZAR-ARASTIRMASI-2026-09-16.md tahmini; DOĞRULANMALI).
- Canlı DB'deki 2 sızıntı satırının silinmesi — Kurucu kararı (ORKESTRATOR-DURUM.md:57), S-1 yalnız demo öncesi denetim adımını zorunlu kılar.
- QA fikstürlerinin (QA-TEST-1, 'Demo Kuyumculuk') tanıtım amaçlı kullanımı — açıkça yasak.
- Web'de genel fiyat sayfası yayını (dışa yayın = Kurucu imzası).

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| tenants | DEGISTIR: `tanitim` (bool, varsayılan false) kolonu — kiracının tanıtım/demo olduğunun otoritesi; raporlarda ve yedek/denetimde ayrıştırma için. | tanitim TINYINT(1) NOT NULL DEFAULT 0 | IX_tenants_tanitim (tanitim) | TanitimKiracisi |
| tanitim_tohumlari | YENI (IKiraciVarlik): tohumlama izi — hangi betik sürümü, ne zaman, kaç kayıt, etiket/not; pasife alınabilir, hard-delete yok. | id CHAR(36) PK · tenant_id CHAR(36) · betik_surumu VARCHAR(20) · etiket VARCHAR(120) · not VARCHAR(400) NULL · kayit_sayisi INT · aktif TINYINT(1) · olusturuldu/guncellendi DATETIME | IX_tanitim_tohumlari_kiraci (tenant_id, aktif) · IX_tanitim_tohumlari_kiraci_tarih (tenant_id, olusturuldu) | TanitimKiracisi |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/tanitim/kiraci` | Kimlik öncesi (Kayit gibi) — rol yok | Tanıtım kiracısı + Merkez Şube + SAHIP kullanıcı yaratır; AuthServisi.Kayit yolunu yeniden kullanır (ikinci kiracı-açma yolu AÇILMAZ), tenants.tanitim=1 damgalar, tanitim_tohumlari satırı açar. Yanıt: kiracı id + tek kullanımlık giriş bilgisi (parola ekrana basılmaz, betik ortam değişkeninden alır). | TanitimKapisi: Tanitim:Etkin=true VE ortam != Production VE X-Tanitim-Anahtar başlığı yapılandırmadaki sırla eşleşmeli; üçünden biri eksikse 404/403 (fail-closed) + OranSiniri.Kayit |
| GET | `/api/v1/tanitim/durum` | SAHIP | Aktif kiracının tanıtım olup olmadığı, tohum sürümü, kayıt sayısı, tohumlama zamanı. Ayarlar şeridi bunu okur. | [Authorize] + [RolGerekli("SAHIP")] + [ModulYetkisi] (Admin modülü yetkisi) |
| PATCH | `/api/v1/tanitim/{id}` | SAHIP | Tohum kaydının etiket/not alanlarını düzeltir (DoD: her yeni varlık düzenlenebilir doğar). Tek düzenleme kalıbı: SatirIciDuzenle. | [Authorize] + [RolGerekli("SAHIP")] + KiraciYazmaDenetcisi + DenetimServisi kaydı |
| POST | `/api/v1/tanitim/{id}/pasif` | SAHIP | Tohum kaydını pasife alır (aktif=0); tenants.tanitim damgası KALIR — tanıtım kiracısı sessizce gerçek kiracıya dönüşemez. Hard-delete yoktur. | [Authorize] + [RolGerekli("SAHIP")] + denetim kaydı |
| POST | `/api/v1/tanitim/{id}/aktif` | SAHIP | Pasife alınmış tohum kaydını geri açar (UX-1-B aktife-alma sözleşmesi). | [Authorize] + [RolGerekli("SAHIP")] + denetim kaydı |

## Servis ve dosyalar
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Tanitim/TanitimModulu.cs` — DTO + TanitimServisi + TanitimController tek dosya (modül kalıbı). Kiracı açma AuthServisi.Kayit'ı çağırır; SistemKapsami yalnız Kayit'ın içinde kullanılır, burada YENİ filtresiz erişim açılmaz. TanitimKapisi statik kontrol sınıfı aynı dosyada.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — TanitimServisi DI kaydı (mevcut kalıpla, Scoped).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs` — Tenant.Tanitim bool (Entities.cs:52-74 Tenant sınıfına) + TanitimTohumu : IKiraciVarlik sınıfı.
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — DbSet<TanitimTohumu> + HasQueryFilter (mevcut IKiraciVarlik kalıbı) + iki indeks tanımı.
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/tanitim-tohumla.sh` — Tohumlama betiği: /api/v1/tanitim/kiraci ile kiracı açar, SAHIP olarak giriş yapar, sonra YALNIZ gerçek uçlardan (urun, cari, kasa, satis, hurda/gider pusulası, emanet, fiyat, kur) 12 aylık tutarlı veri basar. Doğrudan SQL INSERT YOK — defter HareketFabrikasi.Ekle'den geçer. Sır ekrana basılmaz (var/yok).
- **DEGISTIR** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json + appsettings.Development.json` — Tanitim:Etkin (Production'da false, sabit) + Tanitim:Anahtar (yalnız ortam değişkeninden; varsayılan sırla açılış FAIL).
- **YENI** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/10-ticari/` — Ticari belge hattı: FIYAT-KADEMELERI-S1.md · DEMO-BETIGI-15DK.md · SLA-KURULUM-EGITIM-DESTEK.md · VERI-TASIMA-SOZU.md · PILOT-PROTOKOLU-3-REFERANS.md · KURUCU-IMZA-LISTESI.md (imza/para/dış taahhüt kalemleri ayrı).

## Ekranlar
- `/ayarlar` · `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — 'Tanıtım verisi' şeridi: kırmızı rozet 'BU KİRACI TANITIM VERİSİDİR', tohum sürümü + tarih + kayıt sayısı; etiket/not SatirIciDuzenle ile düzenlenir (PATCH), 'Pasife al' / 'Aktife al' düğmeleri istek atar. Tanıtım olmayan kiracıda şerit hiç render edilmez. (menü/yetki: SAHIP (mevcut Ayarlar menü yetkisi); BASİT modda gizli, PROFESYONEL modda görünür (UX-3-B))
- `/panel · /islemler · /cariler · /urunler · /fiyatlandirma · /kasalar · /banka · /karzarar · /uyum` · `(kod değişikliği YOK — demo betiğinin kullandığı mevcut sayfalar)` — 15 dk demo betiğinin rota sırası bu 9 ekranla sınırlıdır; betik her adımda beklenen ekran çıktısını ve 'gösterilmeyecek' listesini (kılavuz/raporlar hariç tutma kararı UX-4d sonrası yeniden değerlendirilir) yazılı sabitler. (menü/yetki: Demo kullanıcısı SAHIP rolüyle girer)

## İş kuralları (fail-closed)
- Fail-closed fiyat kapısı: FIYAT-KADEMELERI-S1.md'de F-8 referansı olmayan hiçbir rakam ilan edilemez; boş hücre 'DOĞRULANMALI' damgalıdır ve belge 'TASLAK — İLAN EDİLEMEZ' başlığıyla doğar (Madde 1).
- Tohumlama ucu Production'da yoktur: Tanitim:Etkin=false veya ortam Production ise uç 404 döner; anahtar eşleşmezse 403. Üç koşul da sağlanmadan tek satır veri yazılmaz.
- Tanıtım kiracısı QA fikstürü DEĞİLDİR: QA-TEST-1 / 'Demo Kuyumculuk' adları betikte yasaklı; ad sabiti 'Tanıtım Kuyumculuk'.
- Tanıtım damgası tek yönlüdür: tenants.tanitim=1 olan kiracı uçtan gerçek kiracıya çevrilemez; dönüştürme yalnız yeni kiracı açıp veri taşımayla olur.
- Tohum verisi gerçek uçlardan yazılır: doğrudan SQL/Add ile defter yazımı yasak (denetim.sh HareketFabrikasi kuralı, dotnet/denetim.sh:55-58).
- Demo öncesi zorunlu adım: yetim-tara.sh çıkışı 0 olmadan ve kiracı sızıntısı satırları temizlenmeden canlı demo yapılmaz (ORKESTRATOR-DURUM.md:57).
- Her tohum kaydı düzenlenebilir ve pasife alınabilir doğar; düğme ya istek atar ya disabled + DegismezIpucu (DoD).
- SLA taslağı taahhüt değildir: 'Kurucu imzası bekliyor' damgası taşır; süre/çalışma saati rakamları imzasız ilan edilmez.
- Referans/vaka çalışması pilotun yazılı izni olmadan yayımlanamaz; izin metni PILOT-PROTOKOLU ekindedir.
- Demoda para konuşulmaz: dalga 2b+2c + QA turu 2 + kurul turu 2 bitmeden yalnız ücretsiz pilot daveti yapılır (Eray'ın bağlayıcı kapısı).
- Tohumlama çıktısı sır basmaz: üretilen SAHIP parolası ekrana/loga yazılmaz, yalnız 'var/yok' bildirilir.
- Rakip fiyatı, tasarruf yüzdesi ve referans rakamı doğrulanmadan satış materyaline girmez (Eray kuralı, kurul md.703).

## Testler
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TanitimTohumTestleri.cs` [DB (gerçek MySQL)]
  - TohumKaydi_KiraciFiltresiyle_BaskaKiracidanGorunmez
  - TohumKaydi_Patch_EtiketiDuzeltir_VeDenetimKaydiYazar
  - TohumKaydi_Pasife_Alinir_HardDeleteYok
  - TanitimDamgasi_Kiracida_TekYonludur
  - Tohumlama_TenantTanitim_BayraginiAcar
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/HttpTanitimKapisiTestleri.cs` [HTTP (WebApplicationFactory)]
  - Tohum_Ucu_ProductionOrtaminda_404
  - Tohum_Ucu_AnahtarYanlissa_403
  - Tohum_Ucu_EtkinDegilse_404
  - Tohum_Ucu_GecerliAnahtarla_201_VeKiraciAcar
  - Durum_Ucu_TezgahtarRolunde_403
  - Patch_Tohum_BaskaKiracininKaydinda_404
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/AyarlarTanitimSeridiTestleri.cs` [bUnit]
  - TanitimKiracisinda_Rozet_Gorunur
  - NormalKiracida_Serit_HicRenderEdilmez
  - PasifeAl_Dugmesi_IstekAtar
  - Etiket_SatirIciDuzenle_PatchGonderir
  - BasitModda_Serit_Gizlenir
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TicariBelgeSozlesmesiTestleri.cs` [saf (dosya sözleşmesi)]
  - FiyatBelgesi_F8Referanssiz_Rakam_Icermez
  - FiyatBelgesi_TaslakDamgasi_Tasir
  - DemoBetigi_Yalnizca_VarOlan_Rotalari_Kullanir
  - TohumBetigi_QATEST1_Veya_DemoKuyumculuk_Adini_Kullanmaz
  - SLA_Belgesi_KurucuImzaDamgasi_Tasir

## Mutasyon kanıtları
- TanitimKapisi ortam kontrolünü `!=Production` → `true` yap: HttpTanitimKapisiTestleri.Tohum_Ucu_ProductionOrtaminda_404 KIRILMALI.
- Anahtar karşılaştırmasını daima true yap: Tohum_Ucu_AnahtarYanlissa_403 KIRILMALI.
- TanitimTohumu'ndan HasQueryFilter'ı kaldır: TanitimTohumTestleri.TohumKaydi_KiraciFiltresiyle_BaskaKiracidanGorunmez ve Patch_Tohum_BaskaKiracininKaydinda_404 KIRILMALI.
- Pasife alma yerine Remove kullan: TohumKaydi_Pasife_Alinir_HardDeleteYok KIRILMALI.
- PATCH'te DenetimServisi çağrısını sil: TohumKaydi_Patch_EtiketiDuzeltir_VeDenetimKaydiYazar KIRILMALI.
- [RolGerekli("SAHIP")] niteliğini kaldır: Durum_Ucu_TezgahtarRolunde_403 KIRILMALI.
- tenants.tanitim=1 yazımını kaldır: Tohumlama_TenantTanitim_BayraginiAcar KIRILMALI.
- Fiyat belgesine F-8 referanssız bir TL rakamı ekle: TicariBelgeSozlesmesiTestleri.FiyatBelgesi_F8Referanssiz_Rakam_Icermez KIRILMALI.
- Tohum betiğinde kiracı adını 'Demo Kuyumculuk' yap: TohumBetigi_QATEST1_Veya_DemoKuyumculuk_Adini_Kullanmaz KIRILMALI.
- Ayarlar şeridinin görünürlük koşulunu sabitle: AyarlarTanitimSeridiTestleri.NormalKiracida_Serit_HicRenderEdilmez KIRILMALI.
- Pasife al düğmesini istek atmayan boş düğmeye çevir: PasifeAl_Dugmesi_IstekAtar KIRILMALI.
- Demo betiğine var olmayan bir rota ekle: DemoBetigi_Yalnizca_VarOlan_Rotalari_Kullanir KIRILMALI.

## Canlı doğrulama
- ASPNETCORE_ENVIRONMENT=Development, API `--urls http://localhost:5198`; POST /api/v1/tanitim/kiraci doğru anahtarla → 201; anahtarsız → 403; Tanitim:Etkin=false ile → 404 (üç kod da kayda geçer).
- ASPNETCORE_ENVIRONMENT=Production ile aynı uç → 404 (fail-closed kanıtı).
- dotnet/tanitim-tohumla.sh koşar; çıktıda yalnız 2xx kodları, sır satırı yok. SQL: SELECT ad, tanitim FROM tenants WHERE ad='Tanıtım Kuyumculuk' → 1 satır, tanitim=1.
- SQL sayımları: SELECT COUNT(*) FROM urunler/cariler/hareketler/kasa_hareketleri WHERE tenant_id='<tanitim>' → beklenen adetlerle birebir; SELECT COUNT(*) FROM hareketler WHERE tenant_id='<tanitim>' AND nakit_payi IS NULL → 0 (fabrika yolu kanıtı).
- Kiracı sızıntısı denetimi: dotnet/yetim-tara.sh çıkış kodu 0 (bugün 2 — önce Kurucu kararı gerekir).
- Ayarlar ekranında rozet + 'Pasife al' → 200; SQL: SELECT aktif FROM tanitim_tohumlari WHERE tenant_id='<tanitim>' → 0; DELETE hiç koşmadı (satır duruyor).
- PATCH etiket → 200; SELECT etiket FROM tanitim_tohumlari ve SELECT COUNT(*) FROM denetim_kayitlari WHERE tenant_id='<tanitim>' AND varlik='tanitim_tohumlari' → ≥1.
- 15 dk demo betiği 1366x768'de baştan sona bir kez koşulur, süre ölçülür (≤15 dk) ve her adımın ekran görüntüsü dokumanlar/09-uygulama-kayitlari/ekran altına dosyalanır.
- Kapanış: tanıtım kiracısı ve test kiracısı canlıdan SİLİNİR/ayrıştırılır; `dotnet test` 0 başarısız + dotnet/denetim.sh TEMİZ çıktısı rapora eklenir.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Tanitim/TanitimModulu.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs (DEGISTIR)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs (DEGISTIR — Tenant:52-74 + TanitimTohumu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DEGISTIR — DbSet + HasQueryFilter + indeksler)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Migrations/<zaman>_TanitimKiracisi.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json + appsettings.Development.json (DEGISTIR — Tanitim:Etkin/Anahtar)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/tanitim-tohumla.sh (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor (DEGISTIR — tanıtım şeridi)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/TanitimTohumTestleri.cs · HttpTanitimKapisiTestleri.cs · AyarlarTanitimSeridiTestleri.cs · TicariBelgeSozlesmesiTestleri.cs (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/10-ticari/FIYAT-KADEMELERI-S1.md · DEMO-BETIGI-15DK.md (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/10-ticari/SLA-KURULUM-EGITIM-DESTEK.md · VERI-TASIMA-SOZU.md (YENI)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/10-ticari/PILOT-PROTOKOLU-3-REFERANS.md · KURUCU-IMZA-LISTESI.md (YENI)`

## Fazlar
- **Faz A — sevk edilebilir asgari (ücretsiz pilot paketi)** (Dalga 6 başı, F-8 dolduktan sonra): TanitimModulu + migration + tohumlama betiği + Ayarlar şeridi + 4 test dosyası; DEMO-BETIGI-15DK.md (rota rota, gösterilmeyecek liste), PILOT-PROTOKOLU-3-REFERANS.md, VERI-TASIMA-SOZU.md, FIYAT-KADEMELERI-S1.md 'TASLAK — İLAN EDİLEMEZ' damgalı iskelet. Çıktı: bugün dürüstçe koşan ücretsiz pilot daveti.
- **Faz B — ücretli satış paketi** (QA turu 2 + kurul turu 2 HAZIR dedikten sonra): F-8 rakamlarının fiyat tablosuna işlenmesi ve damganın kaldırılması (Kurucu imzası), SLA sürelerinin kesinleşmesi, 3 pilotun ölçüm sonuçlarının vaka çalışmasına dönüşmesi, tohum verisinin 'sektör paketi' varyantları (tek şube / çok şube), demo kaydının video/görüntü arşivi.
## Mevzuat dayanağı
- Ticari Reklam ve Haksız Ticari Uygulamalar Yönetmeliği — ilan edilen fiyatın ve 'ücretsiz/bedava' ifadelerinin doğruluğu — **ORTA — genel çerçeve bilinir, S-1'e özgü uygulaması DOĞRULANMALI** — 'İlk 3 ay ücretsiz' benzeri bir vaat yazılırsa koşulları aynı görünürlükte belirtilmeli; rakip karşılaştırmalı iddialar (tasarruf yüzdesi) kanıtlanabilir olmalı.
- 6698 KVKK — demo/tanıtım verisi — **YÜKSEK (ilke), uygulama DOĞRULANMALI** — Tanıtım kiracısındaki tüm kişi/cari kayıtları KURMACA olmalı; gerçek müşteri verisi demoya kopyalanamaz. Pilot sözleşmesinde veri işleyen/veri sorumlusu sıfatları ve 47-A içe aktarımı için aydınlatma yazılı olmalı.
- 6102 TTK — ticari sözleşme/SLA, taraflar tacir; tüketici mevzuatı kapsamı dışı — **ORTA — DOĞRULANMALI (muhatap şahıs işletmesi ise sıfat değerlendirilmeli)** — SLA'nın cezai şart, fesih, veri iade süresi maddeleri Kurucu imzası öncesi hukuki gözden geçirme ister.
- 10380 sayılı Karar (RG 09.09.2025) — büyükşehirde emtia alım-satımında gerçek usule geçiş, 1/1/2026 — **ORTA — PAZAR-ARASTIRMASI-2026-09-16.md'den alıntı, birincil metin DOĞRULANMALI** — Satış argümanının çekirdeği; ilan edilmeden önce Resmî Gazete metniyle doğrulanmalı (Madde 1).
- Rakip fiyatları (Dezy 500/1.000/2.500 TL/ay, M&L 94.686,48 TL peşin) — **DÜŞÜK — ikincil kaynak, birincil doğrulama yok** — HAZIRLIK-KURULU-1.md:285 ve :248; hiçbir rakip ürünün içi görülmedi. Karşılaştırmalı fiyat iddiası birincil kaynak (ekran görüntüsü/teklif) olmadan materyale girmez.

## Riskler
- Erken fiyat ilanı itibar riski: 1.0 olmadan fiyat yayımlanırsa demo ile vaat arasındaki boşluk görünür (kurul md.285). Azaltma: F-8 + kurul turu 2 kapısı, belgede TASLAK damgası.
- Tanıtım ucunun canlıya sızması: yanlış yapılandırmayla Production'da açık kalırsa herkes kiracı açabilir. Azaltma: üç katlı fail-closed kapı + Production testi + denetim.sh'a kural eklenmesi (DOĞRULANMALI: denetim.sh'a yeni sayaç eklenecek mi).
- Demo betiğinin bugünkü ağaçla dürüst koşmaması: 27 sayfanın 22'si gizlenerek koşuluyor (kurul md.68) — müşteri 'gerisi ne' diye sorarsa güven kaybı. Azaltma: Faz A yalnız ücretsiz pilot.
- Tohum verisinin gerçekçi olmaması: uydurma marj/ciro rakamları demoda yakalanır. Azaltma: rakamlar pazar araştırmasındaki gerçek ölçeklere oturtulur, kaynak belgeye yazılır.
- Canlı DB'deki sızıntı satırları temizlenmeden demo yapılması — Kurucu kararı gecikirse dalga 6 kilitlenir.
- SLA'da verilen destek süresinin tek kişilik ekiple karşılanamaması (T-4b destek paketi henüz yok). Azaltma: pilot süresince 'en iyi gayret' ifadesi, imzalı taahhüt Faz B'de.
- Pilot referansının yazılı izin alınmadan kullanılması → hukuki ve itibar riski. Azaltma: izin metni protokol eki, test edilmez ama süreç kapısıdır.
- Fiyatın Dezy'nin altına konumlanmasının marjı negatife düşürmesi (F-8 dolmadan bilinmiyor).
- Tanıtım kiracısının yedek/rapor/ölçüm sayılarına karışması. Azaltma: tenants.tanitim bayrağı ve raporlarda ayrıştırma (DOĞRULANMALI: mevcut raporlar kiracı bazlı çalıştığı için etki sınırlı).

## Açık sorular
- Tohumlama ucu mu, ayrı konsol projesi mi? Öneri uç + betik (ikinci host açmamak için); Bertan'ın onayı gerekir.
- F-8 tablosunun üç kademeye nasıl dağıtılacağı (kiracı başı maliyet + destek payı + hedef brüt marj yüzdesi) — Mustafa'nın kararı.
- Demo betiğinde /kilavuz ve /raporlar gerçekten kapalı mı kalacak? UX-4d 14 ve 34-A/35-A bittikten sonra yeniden değerlendirilmeli.
- 3 pilot kiminle yapılacak (İstanbul Kuyumcular Odası kanalı?) ve pilot ücretsizliği ne kadar sürecek — Kurucu + Eray.
- denetim.sh'a 'tanıtım ucu Production'da kapalı' sayacı eklenecek mi (yeni kural = yeni bakım yükü).
- Tanıtım kiracısı canlı kuyumcu_net'te mi yoksa ayrı bir demo veritabanında mı yaşayacak? Ayrı DB daha güvenli ama T-1 yayın paketine bağımlılık doğurur.
- Tek kullanımlık SAHIP parolası nasıl teslim edilecek (ortam değişkeni mi, ilk girişte zorunlu değiştirme mi) — Mehmet'in kararı.
- Fiyat kademelerinin adları ve Plan metin alanıyla (Entities.cs:56 'BASLANGIC') eşleşmesi gerekiyor mu — F-1 gelmeden kademe adı koda girmemeli.

## Oylama
- **YENİ: BAŞ MİMAR (Bertan) merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilirlik), dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı bizzat doğrulandı (Entities.cs Tenant bloğu, Program.cs CSP + statik boru hattı, denetim.sh yüzeyi, wwwroot/index.html, AdminModulu plan yazma yolu, AuthServisi.Kayit). NOT (Madde 1): görevde "4 paket" denmesine rağmen bana ulaşan tasarım metni 3 pakettir (F-8, S-1, PWA-1); PWA-1 'mevzuat' bölümünün ortasında kesilmiş, 4. paket nesnesi hiç başlamamıştır. Olmayan tasarıma oy uydurulmadı.: KOSULLU** — Ticari paket gerekli; fail-closed refleksleri (üç katlı tanıtım kapısı, hard-delete yok, gerçek uçlardan tohumlama, sır basmama) doğru. Ama TEK PARÇA SEVK EDİLEBİLİR DEĞİL: 5 yeni uç + yeni varlık + migration + 4 test dosyası + ekran + 6 belge tek mühürde; kurulun '2-3 gün' tahmininin 3-4 güne çıkması bunun itirafı. Ayrıca üçüncü kademe adlandırma seti üretme ve tanıtım verisini canlı şemaya karıştırma riski var; ikisi de geri alınması pahalı kararlar.
  - Koşullar: BÖL: S-1-KOD (TanitimModulu + migration + tohumlama betiği + 3 test dosyası) ve S-1-BELGE (6 ticari belge + sözleşme testi) ayrı sevk birimi olsun. · İKİNCİ YOL YASAĞI ölçülsün: tanıtım ucunda Tenant/Sube/Kullanici doğrudan INSERT'i yasak, yalnız AuthServisi.Kayit (AuthModulu.cs:87) çağrılır; denetim.sh'a SistemKapsami.Sistem() çağrı sayacı eklenip sabitlensin. · tenants.tanitim: tenants kiracı tablosunun KENDİSİDİR, IKiraciVarlik değildir — HasQueryFilter uygulanmaz. IX_tenants_tanitim tek bitlik düşük seçicilikli indekstir; gerekçesi ölçülmeden eklenmesin. · Kademe adları koda GİRMESİN: Entities.cs:56 ve Isletme.razor:49-51 zaten iki set; 'Tek Şube/Çok Şube/Uyum+' üçüncüsü olur. F-1/F-3 gelene kadar yalnız belgede kalsın. · Tanıtım kiracısı canlı kuyumcu_net'te mi ayrı demo DB'de mi yaşayacağı, F-8 disk/satır ölçümlerinden ÖNCE karara bağlansın; yoksa F-8 ve F-4 sayaçları sahte veriyle kirlenir. · Ortam/konfig kapısı ortak yardımcıya bağlansın: TanitimKapisi mantığı (Etkin + ortam != Production + başlık sırrı) dosyaya gömülmesin, iş kuralı kapılarından ayrı tek kalıp olsun.
- **YENİ: Güvenlik merceğinden (Mehmet) üç tasarımı inceledim: kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek/TLS, içe aktarım riski ve MFA koşulu açısından. Program.cs:283 canlı kodda script-src 'self' 'wasm-unsafe-eval' olarak doğrulandı (unsafe-inline yok) — PWA-1'in CSP taahhüdünün mevcut temeli sağlam. docker-compose.yml:7'de MySQL root parolası düz metin olarak canlıda doğrulandı; hiçbir pakette bu ele alınmıyor (T-1/F-6/F-7'ye bırakılmış), ayrı takip gerekir.: KOSULLU** — TanitimKapisi'nin üç katlı fail-closed tasarımı (Etkin+ortam+anahtar), kiracı izolasyonunun mevcut IKiraciVarlik/HasQueryFilter desenini yeniden kullanması ve hard-delete yerine pasife alma prensibi güvenlik açısından sağlam. Ancak paket kendi belgesinde üç açık güvenlik kararını (parola teslimi, anahtar yönetimi, tanıtım kiracısının uyum raporlarına karışması) bana ('Mehmet'in kararı') bırakmış; bunlar karara bağlanmadan sevk edilirse fail-closed niyet koşulsuz garanti edilmiyor. Bu yüzden VETO değil KOŞULLU: tasarım iyi, ama üç açık soru bağlayıcı koşula çevrilmeden mühürlenmemeli.
  - Koşullar: Tek kullanımlık SAHIP parolası yalnız ortam değişkeninden okunmalı, hiçbir logda/stdout'ta/HTTP yanıtında düz metin görünmemeli (yalnız 'var/yok'); ilk girişte zorunlu parola değişikliği eklenmeli — açık soruda bana bırakılan karar budur (acik_sorular: 'Tek kullanımlık SAHIP parolası ... Mehmet'in kararı'). · appsettings.json + appsettings.Development.json'da Tanitim:Etkin varsayılanı FALSE olmalı ve Production ortamında kod düzeyinde sabitlenmeli (yapılandırma hatasıyla açılamaz); appsettings.json diff'i PR/inceleme sırasında satır satır kontrol edilmeli (servis_ve_dosyalar: appsettings maddesi). · X-Tanitim-Anahtar en az 32 karakter rastgele değer olmalı, appsettings'e düz yazılmamalı (yalnız ortam değişkeni), ve üretim öncesi rotasyon prosedürü SLA/KURUCU-IMZA-LISTESI belgesine eklenmeli. · tenants.tanitim=1 kiracısının HareketFabrikasi.Ekle ile yazdığı gerçek defter kayıtları MASAK 185.000 TL kümülatif eşiği ve 8 yıl saklama hesaplamalarına KARIŞMAMALI; rapor/uyum sorgularının tanitim=1 kiracıları hariç tuttuğu ayrıca test edilmeli (bugünkü test listesinde yok — eklenmeli). · Demo öncesi zorunlu ön koşul olarak yazılan 'canlı DB'deki 2 kiracı sızıntısı satırının temizlenmesi' (ORKESTRATOR-DURUM.md:57) bir BLOKLAYICI kapı olarak S-1 Faz A'nın DoD'sine (denetim listesine) somut adım şeklinde eklenmeli, yalnız risk metninde kalmamalı. · docker-compose.yml:7'deki düz metin MySQL root parolası demo/pilot ortamına taşınmadan önce (T-1/F-6 beklemeden) en azından tanıtım/demo dağıtımında ortam değişkenine taşınmalı; aksi hâlde ilk gerçek pilotta sır sızıntısı riski demo materyaliyle birlikte büyür.
- **YENİ: Aşkın — QA/fail-closed kalite kapısı. Mercek: (a) her iş kuralının mutasyon kanıtlı bir testi var mı, (b) DB/HTTP/bUnit dengesi kurulu mu, (c) kararsızlık (flaky) riski, (d) canlı doğrulama ölçülebilir/somut mu, (e) kurulum/yedek paketlerinde "kanıt" gerçek publish+geri yükleme mi, (f) test kiracısı temizliği taahhüt edilmiş mi. Doğrulama: Program.cs:271-330 (CSP script-src 'self' 'wasm-unsafe-eval', unsafe-inline YOK — PWA-1'in iddiasıyla örtüşüyor), Kuyumcu.Api.csproj:9-17 (8 paket, F-8'in lisans-0 iddiasıyla örtüşüyor), Entities.cs:16,56 (Planlar dizisi ve Plan varsayılanı doğrulandı), AuthModulu.cs:87,323-326 (Kayit yolu doğrulandı, kota/deneme bitişi yok iddiası tutarlı), denetim.sh:1-60 (kalıp denetimi mekanizması gerçek ve betikte "ExecuteUpdate|ExecuteDelete beyaz liste 4" gibi sayaçlar var — S-1/PWA-1'in "denetim.sh'a blok eklenir" planı bu mekanizmaya uygun).: KOSULLU** — DB/HTTP/bUnit üçlüsü dengeli kurulmuş (4 ayrı test dosyası, saf dosya-sözleşmesi testi dahil — TicariBelgeSozlesmesiTestleri iyi bir QA kalemi: fiyat belgesine kaynaksız rakam girmesini kod seviyesinde engelliyor). Mutasyon kanıtları (12 madde) somut ve her biri belirli bir teste bağlanmış — bu paketin en güçlü yanı. Kararsızlık riskine karşı bUnit STRICT JSInterop modu açıkça istenmiş (Aşkın koşulu diye anılmış, doğru). Kanıt zinciri güçlü ancak tanıtım kiracısının izolasyon/temizlik detaylarındaki boşluklar üretim güvenliği açısından kapatılmalı.
  - Koşullar: Tanıtım kiracısının canlı kuyumcu_net'te mi ayrı DB'de mi yaşayacağı 'açık soru' olarak bırakılmış; bu QA açısından kritik çünkü test/tanıtım verisi gerçek kiracı raporlarına (34-A/35-A kâr-zarar) karışma riski taşır — Faz A başlamadan bu netleşmeli, yoksa mutasyon testi 'TanitimDamgasi ile raporlardan ayrışır' senaryosunu kapsamıyor. · HttpTanitimKapisiTestleri 6 senaryo listeliyor ama 'üç koşuldan İKİSİ doğru biri yanlışken de 404/403' kombinasyon testi (ör. Etkin=true+doğru anahtar+Production) yok; yalnız tekil kırılma testleri var — tam matris olmadan fail-closed iddiası kısmen kanıtsız. · Demo öncesi zorunlu adım (yetim-tara.sh=0) bugün 2 sızıntı satırıyla SAĞLANMIYOR; bu paket başlamadan önkoşul olarak net biçimde bloklayıcı işaretlenmeli, yoksa 'Faz A ücretsiz pilot' sahada erken denenebilir. · Tek kullanımlık SAHIP parolasının teslim mekanizması (ortam değişkeni mi, ilk girişte zorunlu değişim mi) açık soru — test senaryolarında da bu akış yok; parola sızıntısı test edilmeden Faz A kapanmamalı. · canli_dogrulama'da 'test kiracısı canlıdan SİLİNİR/ayrıştırılır' deniyor ama tanıtım kiracısı ayrı: tanıtım kiracısının SİLİNMESİ değil PASİFE ALINMASI öngörülüyor — kapanış maddesindeki 'silinir' ifadesi tanıtım kiracısını da kapsıyor mu belirsiz, netleştirilmeli.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- BÖL: S-1-KOD (TanitimModulu + migration + tohumlama betiği + 3 test dosyası) ve S-1-BELGE (6 ticari belge + TicariBelgeSozlesmesiTestleri) ayrı sevk birimidir; tek parça sevk edilemez (Bertan itirazı).
- İKİNCİ YOL YASAĞI ölçülür: tanıtım ucunda Tenant/Sube/Kullanici doğrudan INSERT yasak, yalnız AuthServisi.Kayit (AuthModulu.cs:87) çağrılır; denetim.sh'a SistemKapsami.Sistem() çağrı sayacı eklenip sabitlenir.
- Tek kullanımlık SAHIP parolası yalnız ortam değişkeninden okunur; log/stdout/HTTP yanıtında düz metin görünmez ('var/yok'), ilk girişte zorunlu parola değişimi eklenir ve bu akış test edilir.
- Tanitim:Etkin varsayılanı FALSE ve Production'da kod düzeyinde sabit; X-Tanitim-Anahtar ≥32 karakter rastgele, yalnız ortam değişkeninde, rotasyon prosedürü SLA/KURUCU-IMZA-LISTESI'ne yazılır.
- TanitimKapisi kombinasyon matrisi test edilir (üçten ikisi doğru + biri yanlış → 404/403); kapı mantığı ortak yardımcıya bağlanır, dosyaya gömülmez.
- tanitim=1 kiracısı MASAK 185.000 TL kümülatifine, 8 yıl saklamaya ve 34-A/35-A kâr-zarar raporlarına KARIŞMAZ; hariç tutma ayrıca test edilir.
- BLOKLAYICI önkoşul: canlı DB'deki 2 kiracı sızıntı satırı temizlenip yetim-tara.sh=0 olmadan Faz A sahaya çıkmaz (DoD'ye somut adım olarak yazılır).
- tenants.tanitim kiracı tablosunun kendisidir, HasQueryFilter uygulanmaz; IX_tenants_tanitim tek bitlik indeks gerekçesi ölçülmeden eklenmez. Tanıtım kiracısı SİLİNMEZ, PASİFE ALINIR — kapanış maddesindeki 'silinir' ifadesi buna göre düzeltilir.
- Kademe adları koda GİRMEZ: Entities.cs:56 ve Isletme.razor:49-51 zaten iki set; üçüncü set (Tek Şube/Çok Şube/Uyum+) F-1/F-3 gelene kadar yalnız belgede kalır.
- Tanıtım kiracısının canlı kuyumcu_net'te mi ayrı demo DB'de mi yaşayacağı Faz A başlamadan karara bağlanır; demo dağıtımında docker-compose.yml:7 düz metin root parolası ortam değişkenine taşınır.
