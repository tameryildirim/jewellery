# Paket 29 — Çek / senet portföyü

> Katman: Katman 4 — Cari ve finans · Tahmin: Faz A 4–5 gün · Faz B 2 gün → toplam 6–7 gün. PARITE tahmini 5–7 ile uyumlu; üst sınıra yakın olmasının sebebi PARITE'de olmayan iki ek iş: modül-başı yetki tohumlaması (K4-F, bir kereliktir ve paket 31/32/45/46'yı kısaltır) ve alan şifrelemeli borçlu kimlik alanı. gün · Önkoşullar: yok · Kaynak: katman mimarı K4-29 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 4

## Hedef
Perakende kuyumcu, veresiye bakiyesini kapatmak için müşteriden çek ya da senet alır; bu kıymetli evrak dükkânın en likit ama en riskli varlığıdır. Bugün sistemde bu varlık HİÇ görünmüyor: müşteri çek verdiğinde ya cari bakiye elle düzeltiliyor ya da borç açık kalıyor. Bu paket evrakı ayrı bir varlık olarak kaydeder, cari borcunu kapatır, vadeye göre takvime dizer, tahsile verme/tahsil/karşılıksız/ciro adımlarını fail-closed bir durum makinesiyle yürütür ve karşılıksız çıkan evrakta borcu otomatik geri doğurur.

## Kodda bugünkü durum
SIFIR. Kod tabanında çek/senet için varlık, tablo, uç, ekran ya da sabit yoktur: `grep -rn 'Cek\b|Senet|CekSenet' Kuyumcu.Api Kuyumcu.Web Kuyumcu.Core` 0 eşleşme verir (eşleşenlerin tamamı 'gerçek', 'çekme' gibi Türkçe kelimelerdir ve yalnız Tests altındadır). KuyumcuDbContext.cs:56-89'daki 27 DbSet arasında evrak yoktur. Sabitler.OdemeTurleri (Entities.cs:34) = NAKIT·KART·HAVALE·ALTIN·DIGER — çek yoktur. Sabitler.HareketTurleri (Entities.cs:24-32) içinde evrak türü yoktur. TahsilatGirisDto.OdemeTuru regex'i `NAKIT|KART|HAVALE` (CariModulu.cs:159) olduğu için çek bugünkü tahsilat ucundan GEÇEMEZ. Menüde (KenarCubugu.razor:14-86) evrak bağlantısı yoktur. Yani paket sıfırdan kurulur; hiçbir mevcut mekanizma yeniden yazılmaz, yalnızca genişletilir.

## Kapsam
**İçinde:**
- ALINAN çek ve senet: portföye giriş, cari borcunu kapatma (Faz A)
- Portföy listesi (durum + vade + tutar süzgeçli, sayfalı) ve vade takvimi (gün başına toplam) (Faz A)
- Tahsile verme (banka hesabına) ve portföyden geri alma (Faz A)
- Tahsil edildi: kasaya nakit ya da banka hesabına (Faz A)
- Karşılıksız: borcun ters kayıtla geri doğması + yeni vade + kredi limiti kapısı (Faz A)
- Fail-closed durum makinesi; geçersiz geçiş 400 (Faz A)
- Ciro: evrakın tedarikçi/atölye carisine devri ve bizim borcumuzun azalması (Faz B)
- VERİLEN evrak (kendi çekimiz / borç senedimiz): düzenleme, ödeme günü, karşılıksız rücu (Faz B)
- Evrak üzerindeki borçlu kimlik numarasının ADR-0003 ile şifreli saklanması (Faz A)
- Yeni modül yetkisi (`evrak`) + mevcut kiracılara modül-başı tohumlama (Faz A, K4-F)

**Dışında:**
- Döviz cinsinden evrak ve kur değerlemesi (K4-G; paket 27 kararına bağlı)
- Bankayla elektronik çek tahsil/karekod entegrasyonu (banka kurumsal API sözleşmesi yok — PARITE 'erişilemeyen' listesi)
- Karşılıksız çekin icra/protesto takibi, avukat dosyası
- Çekin e-Fatura/e-İrsaliye belgelerine işlenmesi (ADR-0012)
- Evrak görselinin (çek fotoğrafı) yüklenmesi — paket 4 belge deposu bunu zaten taşır; paket 29 yalnız `Belge.SahipTuru="EVRAK"` sözleşmesini kullanır, yeni yükleme yolu açmaz
- Reeskont / vadeli evrakın bugünkü değere indirgenmesi (muhasebe kararı yok)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| kiymetli_evraklar | YENİ tablo. IKiraciVarlik uygular (KuyumcuDbContext.cs:196-205 kapsam denetimi aksi hâlde uygulamayı AÇTIRMAZ). Ayrı dosya: Data/KiymetliEvrak.cs (GunKapanisi.cs deseni). | id(36,PK) · tenant_id(36) · sube_id(36) · evrak_turu(10: CEK|SENET) · yon(10: ALINAN|VERILEN — Faz A'da yalnız ALINAN yazılır) · durum(20) · tutar decimal(15,2) · para_birimi(5, TRY) · vade_gunu DATE (DateOnly — UTC damga DEĞİL, Hareket.VadeTarihi ile aynı K5 gerekçesi) · evrak_no(40) · banka_adi(100,null) · banka_sube(100,null) · keside_yeri(100,null) · keside_tarihi DATE null · borclu_cari_id(36,null) · borclu_ad(200,null) · borclu_kimlik_no(255,null — AES converter) · borclu_kimlik_no_hash(64,null — HMAC, aranabilirlik) · kaynak_cari_id(36) · giris_hareket_id(36) · tahsile_veren_banka_hesap_id(36,null) · tahsile_verme_tarihi · tahsil_tarihi · tahsil_kasa_id(36,null) · tahsil_banka_hesap_id(36,null) · tahsil_kasa_hareket_id(36,null) · tahsil_banka_hareket_id(36,null) · karsiliksiz_tarihi · karsiliksiz_hareket_id(36,null) · karsiliksiz_aciklama(500,null) · ciro_cari_id(36,null) · ciro_tarihi · ciro_hareket_id(36,null) · notlar(500,null) · olusturan_kullanici_id(36) · olusturuldu · guncellendi | (tenant_id, durum, vade_gunu) — portföy listesi + vade takvimi bu indeksten karşılanır · (tenant_id, borclu_cari_id) — cari kartındaki 'bu müşterinin evrakları' · (tenant_id, kaynak_cari_id) · (tenant_id, vade_gunu) — takvim tüm durumlar için · (tenant_id, evrak_turu, banka_adi, evrak_no) UNIQUE — aynı çek iki kez girilemez. ⚠ MySQL'de UNIQUE indeks birden çok NULL'a izin verir; senette banka_adi NULL olduğu için senet çifti bu indeksle YAKALANMAZ, servis içi elle kontrol zorunludur (mutasyon kanıtı M29-07) · (tenant_id, borclu_kimlik_no_hash) — şifreli sütun aranmaz, hash üzerinden (KuyumcuDbContext.cs:156-157 deseni) | YYYYMMDDHHMMSS_KiymetliEvrak (üretim anındaki gerçek damga; son migration 20260916060026_BelgeDeposu'dan SONRA). ⚠ `dotnet ef migrations add` ÖNCE `dotnet build` yeşilken ve `--no-build` OLMADAN (SONRAKI-OTURUM-PROMPT §6 tuzağı: boş migration üretti, VadeTakibi dosyaları silindi). |
| hareketler | Sütun DEĞİŞİKLİĞİ YOK, migration GEREKMEZ. Yalnız Sabitler.HareketTurleri dizisine (Entities.cs:24-32) belge amaçlı üç değer eklenir: EVRAK_GIRIS · EVRAK_KARSILIKSIZ · EVRAK_CIRO. Dizi kodda zorlanmaz (Entities.cs:31 notu), belge değeri taşır — rapor/denetim tarafı 'geçersiz tür' sanıp süzmesin diye eklenmesi ZORUNLUDUR. | — | Mevcut (tenant_id, cari_id, tarih) indeksi (KuyumcuDbContext.cs:113) evrak sorgularını da karşılar; yeni indeks gerekmez. | — |
| rol_yetkileri | Şema değişikliği YOK. Veri: yeni `evrak` modülü için modül-başı tohum işaretçisi satırı (Rol=__SISTEM__, Modul=__tohum__evrak). K4-F. | — | Mevcut (TenantId, Rol, Modul) UNIQUE (KuyumcuDbContext.cs:104) yeterli. | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| POST | `/api/v1/evraklar` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | Evrak girişi: müşteriden alınan çek/senet portföye girer, cari borcu kapanır. Gövde: evrakTuru, tutar, vadeGunu(ISO date), evrakNo, bankaAdi/bankaSube/kesideYeri/kesideTarihi (CEK'te), borcluCariId|borcluAd, borcluKimlikNo, kaynakCariId(zorunlu), kurSnapshotId(zorunlu), notlar. Yanıt: evrak kartı + güncel cari bakiye. | IslemDogrulayici.KurDogrula · CariDogrula(kaynakCariId, borcluCariId) · şube zorunlu · vade geçmiş gün olamaz · TevsikKapisi ÇALIŞMAZ (nakit payı 0) · MASAK ÇALIŞMAZ (K4-D) |
| GET | `/api/v1/evraklar` | modül yetkisi yeten her rol | Portföy listesi. Sorgu: durum, evrakTuru, yon, cariId, vadeBas, vadeBit, limit(≤200), offset. Yanıt: { toplamAdet, toplamTutar, satirlar[] } — toplamlar SUNUCUDA toplanır (TB-19). | sayfalama zorunlu; limit>200 → 400 |
| GET | `/api/v1/evraklar/takvim` | modül yetkisi yeten her rol | Vade takvimi: verilen aralıkta GÜN BAŞINA evrak adedi ve toplam tutarı (GROUP BY). Sorgu: bas, bit (varsayılan bugün → +30 gün), durum (varsayılan PORTFOYDE,TAHSILE_VERILDI). Satır DÖNDÜRMEZ, toplam döndürür. | aralık ≤ 366 gün; Zaman.YerelGunAraligiUtc |
| GET | `/api/v1/evraklar/{id}` | modül yetkisi yeten her rol | Evrak kartı + durum geçmişi (bağlı hareket kimlikleri ve tarihleri). | tenant şartı elle de yazılır (ADR-0010) |
| POST | `/api/v1/evraklar/{id}/tahsile-ver` | SAHIP, MUDUR, MUHASEBE | Evrakı bankaya tahsile verir. Gövde: bankaHesapId, tarih?. Defter hareketi YAZILMAZ (para el değiştirmez), yalnız durum değişir. | durum PORTFOYDE olmalı · banka hesabı kiracıya ait olmalı |
| POST | `/api/v1/evraklar/{id}/portfoye-al` | SAHIP, MUDUR, MUHASEBE | Tahsile verilmiş evrakı geri portföye alır (banka iade etti). Defter hareketi YOK. | durum TAHSILE_VERILDI olmalı |
| POST | `/api/v1/evraklar/{id}/tahsil-edildi` | SAHIP, MUDUR, MUHASEBE | Evrak tahsil edildi: para kasaya (nakit) ya da banka hesabına girer. Gövde: hedef(KASA|BANKA), kasaId|bankaHesapId, tarih?, kurSnapshotId. KASA yolunda kasa_hareketleri satırı + kasa bakiyesi, BANKA yolunda banka_hareketleri (GELEN) + hesap bakiyesi güncellenir. | durum PORTFOYDE|TAHSILE_VERILDI · KASA yolunda IslemDogrulayici.KasaDogrula(db, kasaId, tenant, "TRY", an) → hem K6 para birimi hem GunKapanisiKapisi gün kilidi · TevsikKapisi.Dogrula(nakit tahsilde) · MASAK ÇALIŞMAZ (K4-D) |
| POST | `/api/v1/evraklar/{id}/karsiliksiz` | SAHIP, MUDUR, MUHASEBE | Evrak karşılıksız çıktı: borç ters kayıtla GERİ DOĞAR. Gövde: aciklama(≥20 karakter, zorunlu), yeniVadeGun?, kurSnapshotId. Yanıt: evrak + güncel cari bakiye + yeni vade. | durum PORTFOYDE|TAHSILE_VERILDI|CIRO_EDILDI · VadeKapisi.Hazirla (kredi limiti + vade) · gerekçe uzunluğu servis içinde de kontrol (DataAnnotations yalnız controller yolunda çalışır) |
| POST | `/api/v1/evraklar/{id}/ciro` | SAHIP, MUDUR, MUHASEBE | FAZ B. Evrak tedarikçi/atölye carisine ciro edilir; bizim o cariye borcumuz azalır. Gövde: ciroCariId, tarih?, kurSnapshotId. | durum PORTFOYDE olmalı · CariDogrula · VadeKapisi.Hazirla(alacakYonu:false) |
| GET | `/api/v1/cari/{id}/evraklar` | modül yetkisi yeten her rol | Cari kartındaki 'bu müşterinin evrakları' kutusu — CariModulu'na DEĞİL, EvrakController'a eklenir (cari modülünü şişirmemek için). | CariDogrula |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Api/Data/KiymetliEvrak.cs` — Varlık sınıfı + durum sabitleri. `[Table("kiymetli_evraklar")] public class KiymetliEvrak : IKiraciVarlik`. Sınıf başında GunKapanisi.cs:6-17 üslubunda 'NEDEN VAR' yorumu: evrak tablosu bakiyenin kaynağı değil, portföyün aynasıdır; bakiyeyi yalnız defter değiştirir. Ayrıca `public static class EvrakDurumu` (PORTFOYDE, TAHSILE_VERILDI, TAHSIL_EDILDI, KARSILIKSIZ, CIRO_EDILDI) ve `EvrakGecisleri` — izinli geçişlerin sözlüğü.
- **YENI** `dotnet/Kuyumcu.Api/Modules/Evrak/EvrakDurumKapisi.cs` — TEK karar noktası: `public static void Dogrula(string mevcut, string hedef)` — izinli geçiş tablosunda yoksa BadRequestException. Geçişlerin çağrı noktalarına serpiştirilmesi GunKapanisiKapisi.cs:11-13'te yazılı gerekçeyle reddedilir ('biri unutulur'). İzinli geçişler: PORTFOYDE→{TAHSILE_VERILDI, TAHSIL_EDILDI, KARSILIKSIZ, CIRO_EDILDI} · TAHSILE_VERILDI→{PORTFOYDE, TAHSIL_EDILDI, KARSILIKSIZ} · CIRO_EDILDI→{KARSILIKSIZ} · KARSILIKSIZ→{TAHSIL_EDILDI} (sonradan ödendi) · TAHSIL_EDILDI→{} (son durum).
- **YENI** `dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs` — DTO'lar + `EvrakServisi(KuyumcuDbContext db, ILogger<EvrakServisi> logger, DenetimServisi denetim, TevsikKurali? tevsikKurali = null)` + `[ApiController][Authorize][Route("api/v1/evraklar")][Yetki.ModulYetkisi("evrak")] EvrakController`. BankaModulu.cs:98-371 desenini birebir izler (DTO → servis → controller, `AktifKullanici Islem => AktifKullanici.FromClaims(User)`, [RolGerekli] TAM eşleşme). Her para hareketi `await using var tx = await db.Database.BeginTransactionAsync()` içinde; hareket HareketFabrikasi.Ekle ile; bakiye CariModulu.BakiyeGuncelle ile aynı sözleşmede güncellenir. Her yazma sonrası denetim izi (ADR-0004, VarlikTuru="KiymetliEvrak").
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — Sabitler.HareketTurleri dizisine (satır 24-32) EVRAK_GIRIS · EVRAK_KARSILIKSIZ · EVRAK_CIRO eklenir; satır 27-32'deki 'liste GERÇEĞİ yansıtır' notunun altına aynı üslupta gerekçe yazılır. Sabitler.OdemeTurleri (satır 34) DEĞİŞMEZ — evrak bir ödeme etiketi değil, ayrı bir varlıktır; OdemeTuru alanına "CEK" yazılsaydı TevsikKapisi.GunlukNakitToplam'ın eski-satır dalı (TevsikKapisi.cs:100-104) etkilenmezdi ama KasaHareket.OdemeTuru üzerinden kasa raporlarında nakitle karışırdı.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — `public DbSet<KiymetliEvrak> KiymetliEvraklar => Set<KiymetliEvrak>();` (satır 89 civarı) + OnModelCreating'e 6 indeks (veri modeli tablosu) + AES converter: `mb.Entity<KiymetliEvrak>().Property(e => e.BorcluKimlikNo).HasConversion(duz => s.Sifrele(duz), sifreli => s.Coz(sifreli));` — satır 161-167 desenine bitişik yazılır.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/MasakHashDinleyicisi.cs` — BorcluKimlikNo → BorcluKimlikNoHash üretimini mevcut dinleyiciye ekler (GiderPusulasi.SaticiTcknHash ile aynı yol). ⚠ Dosya OKUNMADI (yalnız adı ve KuyumcuDbContext.cs:159-170'teki kullanımı görüldü); uygulayıcı önce dosyayı okuyup mevcut deseni izlesin — yeni bir hash yolu AÇMASIN.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` — (1) Moduller.Tumu ve Varsayilan'a "evrak" eklenir: MUDUR ve MUHASEBE alır, TEZGAHTAR ALMAZ (evrak girişini tezgâhtar yapabilsin isteniyorsa ekiple oylanır — açık soru). (2) K4-F: `TohumlaGerekiyorsa` modül-başı işaretçiye çevrilir — tek `__yapilandirildi__` marker'ı yerine her modül için `__tohum__<modul>` satırı yazılır; eksik olanlar Varsayilan'dan tamamlanır. Eski tek marker'ı taşıyan kiracılar için: marker varsa TÜM eski modüller tohumlanmış sayılır, yalnız yeni modüller eklenir. SAHIP'in bilinçli kaldırdığı yetki GERİ GELMEZ çünkü Kaydet (satır 78-97) çağrıldığında tüm __tohum__ satırları da yeniden yazılır. (3) `cache.Remove` davranışı korunur.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — `s.AddScoped<Evrak.EvrakServisi>();` — tek satır, satır 35'ten sonra.
- **DEGISTIR** `dotnet/denetim.sh` — Satır 63 `VeresiyeKapanisi (yalnız Etiketten)` beklenen sayısı 2 → 3. Gerekçe betiğe YAZILIR: evrak girişi meşru bir borç-KAPATAN yoldur (satır 60-62'deki kuralın amacı vadesiz borç DOĞURMAYI yakalamaktır). Alternatif — `OdemeIzi.Etiketten("VERESIYE", tutar, null)` ile sayıyı 2'de tutmak — REDDEDİLDİ: denetimi kandırmak denetimin amacını boşa çıkarır. Satır 66 `BEKLENEN_VARLIK=15` → 16.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukEvrak` sınıfı (başlık, çek, senet, portföy, vade takvimi, tahsile ver, portföye al, tahsil edildi, karşılıksız, ciro, durum adları, boş liste metni, onay metinleri) + SozlukNav'a `Evraklar` + kök sözlüğe `public required SozlukEvrak Evrak { get; init; }` + TR ve EN karşılıkları. Dosya başındaki kural (satır 10-11): Razor içine çıplak metin yazılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Kasalar/Banka bloğuna (satır 34-38 civarı) `<NavLink href="/evraklar">📄 @Dil.T.Nav.Evraklar</NavLink>`. Rol gizlemesi menüde yapılır AMA API de [ModulYetkisi("evrak")] ile kapalıdır (satır 52-57'deki not: ekran gizlemesi tek koruma değildir).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs` — /evraklar ve /evraklar/takvim rota başlıkları eklenir (UX-2 A1'de `_ => "Kuyumcu"` geri dalı KALDIRILDI; eksik rota testi kırar — RotaBaslikTestleri 33 test).

## Ekranlar
- `/evraklar` · `dotnet/Kuyumcu.Web/Pages/Evraklar.razor` — Portföy listesi + üstte 4 StatKart (portföydeki toplam, tahsile verilen, bu hafta vadesi gelen, karşılıksız toplamı) + durum/tür/vade süzgeçleri + 'Evrak Gir' formu (modal). Satır eylemleri: Tahsile Ver · Portföye Al · Tahsil Edildi · Karşılıksız · Ciro (Faz B). Karşılıksız eylemi gerekçe ister (≥20 karakter) ve onay ister. VadeTakibi.razor:1-45 deseni: `@inject ApiIstemci Api`, `@inject DilServisi Dil`, HataBandi, Yukleniyor, StatKart, `id` taşıyan girdiler (bUnit sözleşmesi). Vadesi geçmiş satır kırmızı, 7 gün içinde gelen amber. (menü/yetki: [ModulYetkisi("evrak")] — menüde MUDUR/MUHASEBE (ve oylamaya göre TEZGAHTAR); SAHIP daima)
- `/evraklar/takvim` · `dotnet/Kuyumcu.Web/Pages/EvrakTakvimi.razor` — Ay görünümü: her gün hücresinde o güne vadeli evrak adedi ve toplam tutar (sunucudan GROUP BY ile gelir, satır listesi değil — TB-19). Güne tıklayınca /evraklar?vadeBas=..&vadeBit=.. süzgeçli listeye gider. Sayılar CultureInfo.InvariantCulture ile değil, Bicim.Tl ile basılır; SVG/genişlik hesabı varsa invariant (barkod tuzağı). (menü/yetki: aynı modül yetkisi; menüde /evraklar altında ikinci bağlantı)
- `/cariler (mevcut)` · `dotnet/Kuyumcu.Web/Pages/Cariler.razor` — Cari kartı detayına 'Evrakları' kutusu: GET /evraklar/cari/{id} sonucundan portföydeki adet ve toplam. ⚠ Bu dosya paket 27 ve UX-1 ile de çakışır (K4-I); aynı dalgada iki el değmez. (menü/yetki: mevcut cari yetkisi)

## İş kuralları (fail-closed)
- EVRAK GİRİŞİ BORÇ KAPATIR: hareket Tur=EVRAK_GIRIS, TlTutari = −tutar, OdemeIzi.VeresiyeKapanisi(tutar), VadeTarihi=null. CariBakiye.TlBakiye −= tutar. VadeKapisi.AcikBorclar (VadeKapisi.cs:156-159) bu satırı `sign(−)*VeresiyeTutar` ile borç azaltan olarak görür; formüle DOKUNULMAZ (K4-C).
- KARŞILIKSIZ BORCU GERİ DOĞURUR: hareket Tur=EVRAK_KARSILIKSIZ, TlTutari = +tutar, OdemeIzi.Veresiyeden(tutar, vade) — vade VadeKapisi.Hazirla'dan gelir ve kredi limiti kapısı ALACAK yönünde çalışır. Vadesiz borç yazmak DERLENMEZ (HareketFabrikasi.cs:26-32).
- TOPLAM DEĞİŞMEZİ: bir evrak için defterdeki EVRAK_* satırlarının TL toplamı, evrak TAHSIL_EDILDI ya da CIRO_EDILDI durumundaysa 0 (giriş −t, karşılıksız +t, ...) veya −t olmalıdır. Evrak tablosu ile defter arasında bakiye türetilmez; defter tek doğruluk kaynağıdır (VadeKapisi.cs:115-117).
- DURUM MAKİNESİ FAIL-CLOSED: izinli olmayan her geçiş 400. Tek kapı EvrakDurumKapisi.Dogrula.
- AYNI EVRAK İKİ KEZ GİRİLEMEZ: (tenant, tür, banka, evrakNo) benzersizdir; senette banka NULL olduğu için servis içi elle kontrol de yapılır (NULL'lu UNIQUE indeks yakalamaz).
- VADE GEÇMİŞ OLAMAZ: girişte vade_gunu < yerel bugün → 400. Karşılıksız sonrası yeni vade VadeKapisi.VadeHesapla ile hesaplanır (0..3650 gün).
- KASA PARA BİRİMİ + GÜN KİLİDİ: nakit tahsilde IslemDogrulayici.KasaDogrula(db, kasaId, tenant, "TRY", an) — `an` VERİLİR, yoksa gün kilidi ÇALIŞMAZ (IslemDogrulayici.cs:62-66).
- TEVSİK: yalnız nakit tahsilde TevsikKapisi.Dogrula, transaction'dan ÖNCE (CariModulu.cs:490-492 deseni).
- MASAK: evrak türleri kümülatife girmez (K4-D); bu gevşeklik kodda yorumla ve belgede açıkça yazılır.
- KUR SNAPSHOT: para hareketi doğuran her uçta kurSnapshotId zorunlu ve IslemDogrulayici.KurDogrula'dan geçer (Hareket.KurSnapshotId null değildir — Entities.cs:383).
- ŞUBE ZORUNLU: islem.SubeId null ise 400 (CariModulu.cs deseni) — evrak ve hareket aynı şubeye yazılır.
- DENETİM İZİ: her durum değişimi append-only denetim kaydı üretir (ADR-0004); kayıt SİLİNMEZ, düzeltme yeni satırdır.
- PARA BİRİMİ: TRY dışı → 400 (K4-G).

## Testler
- `dotnet/Kuyumcu.Tests/EvrakTestleri.cs` [DB]
  - Evrak girişi cari TL bakiyesini tutar kadar AZALTIR ve EVRAK_GIRIS satırı yazar
  - Girişten sonra VadeKapisi.AcikBorclar açık borcu tutar kadar düşürür (entegrasyon: iki mekanizma aynı sonucu verir)
  - Karşılıksız borcu tam tutar kadar GERİ DOĞURUR ve yeni vade AcikBorclar'da görünür
  - Karşılıksız kredi limitini aşıyorsa 400 (VadeKapisi.KrediLimitiDogrula)
  - Geçersiz durum geçişi (TAHSIL_EDILDI → TAHSILE_VERILDI) 400
  - Aynı banka + çek no ikinci kez girilirse 409/400
  - Aynı senet no ikinci kez girilirse 400 (NULL'lu UNIQUE indeksin yakalamadığı yol)
  - Nakit tahsil kapanmış kasa gününe yazılamaz (GunKapanisiKapisi)
  - Nakit tahsil USD kasasına yazılamaz (K6)
  - Nakit tahsil tevsik haddini aşarsa 400
  - Banka tahsili banka hesabı bakiyesini artırır ve BankaHareket(GELEN) yazar
  - Başka kiracının evrakı GET/POST ile görülemez/değiştirilemez (404)
  - Başka kiracının bankaHesapId'si ile tahsile verilemez
  - Vadesi geçmiş tarihle giriş 400
  - Borçlu kimlik no diske ŞİFRELİ iner (ham SQL ile okunduğunda düz metin DEĞİL), bellekte düz görünür
  - Takvim ucu gün başına toplam döndürür ve 366 günden uzun aralığı reddeder
- `dotnet/Kuyumcu.Tests/HttpEvrakTestleri.cs` [HTTP]
  - TEZGAHTAR karşılıksız ucuna 403 (RolGerekli TAM eşleşme)
  - Modül yetkisi kapalı rolde tüm uçlar 403 (ModulYetkisi)
  - Yetkisi olmayan MUDUR — YENİ modül tohumlaması sonrası 200 (K4-F regresyonu: mevcut kiracıda sessiz 403 OLMAMALI)
  - Gerekçesiz karşılıksız isteği 400 ve gövde biçimi standart hata zarfı
  - limit=5000 ile liste 400
  - JSON camelCase sözleşmesi (yanıt `id`, `evrakTuru`)
- `dotnet/Kuyumcu.Tests/EvrakEkranTestleri.cs` [bUnit]
  - Liste ekranı GET /evraklar çağırır ve satırları basar
  - 'Tahsile Ver' düğmesi POST /evraklar/{id}/tahsile-ver çağırır, gövdede bankaHesapId var
  - 'Karşılıksız' düğmesi 20 karakterden kısa gerekçeyle isteği GÖNDERMEZ
  - Vadesi geçmiş satır kırmızı sınıf alır
  - Takvim ekranı GET /evraklar/takvim çağırır ve gün hücrelerini toplamla basar
  - Boş portföyde boş-durum metni Sözlük'ten gelir (çıplak metin yok)
- `dotnet/Kuyumcu.Tests/EvrakDurumKapisiTestleri.cs` [saf]
  - İzinli geçişlerin tamamı geçer
  - İzinsiz geçişlerin tamamı BadRequestException atar (tablo güdümlü test)
  - TAHSIL_EDILDI son durumdur, hiçbir geçişi yoktur

## Mutasyon kanıtları
- M29-01: EvrakDurumKapisi.Dogrula gövdesi boşaltılır → EvrakDurumKapisiTestleri tümü + `Gecersiz_gecis_400` kırmızı
- M29-02: Evrak girişinde TlTutari işareti +tutar yapılır → `Giris_cari_bakiyesini_azaltir` ve `Giris_sonrasi_acik_borc_duser` kırmızı
- M29-03: OdemeIzi.VeresiyeKapanisi yerine OdemeIzi.Yok verilir → `Giris_sonrasi_acik_borc_duser` kırmızı (bakiye doğru, VADE dağılımı yanlış — tek başına bakiye testi bunu YAKALAMAZ, bu yüzden iki ayrı test var)
- M29-04: Karşılıksızda VadeKapisi.Hazirla çağrısı kaldırılıp sabit vade yazılır → `Karsiliksiz_kredi_limitini_asamaz` kırmızı
- M29-05: Nakit tahsilde KasaDogrula'nın `an` parametresi kaldırılır → `Kapanmis_gune_tahsil_yazilamaz` kırmızı (⚠ gün kilidi başka katmanda tekrar edilmediği için bu mutasyon gerçekten tek koruma katmanını kırar)
- M29-06: Nakit tahsilde TevsikKapisi.Dogrula kaldırılır → `Tevsik_haddi_asan_nakit_tahsil_400` kırmızı
- M29-07: Senet çift giriş elle kontrolü kaldırılır → `Ayni_senet_iki_kez_girilemez` kırmızı (UNIQUE indeks NULL yüzünden yakalamaz — bu mutasyon 'veritabanı zaten koruyor' yanılgısını ölçer)
- M29-08: ModulYetkisi("evrak") özniteliği kaldırılır → `Yetkisiz_rol_403` kırmızı
- M29-09: YetkiModulu modül-başı tohumlama geri alınır (tek marker) → `Mevcut_kiracida_yeni_modul_200` kırmızı
- M29-10: Liste ucundaki limit kıskacı kaldırılır → `limit_5000_400` kırmızı (TB-19 koruması)
- M29-11: BorcluKimlikNo converter'ı kaldırılır → `Kimlik_no_diske_sifreli_iner` kırmızı

## Canlı doğrulama
- Yeni test kiracısı aç; kur kaydı oluşana kadar bekle (10 sn — `/kur/guncel` boş dönerse her istek 400 verir)
- Müşteri carisi aç, 50.000 TL veresiye sat → bakiye +50.000 doğrula (SQL: `SELECT tl_bakiye FROM cari_bakiyeler`)
- 30.000 TL'lik çek gir → HTTP 201; SQL ile `kiymetli_evraklar` 1 satır, `hareketler` içinde EVRAK_GIRIS 1 satır (tl_tutari=-30000, veresiye_tutar=30000, vade_tarihi NULL), cari bakiye 20.000
- GET /raporlar/vade-takibi → açık borç 20.000, 'vadesiz' kalemi ARTMAMIŞ olmalı
- Çeki tahsile ver → durum TAHSILE_VERILDI; aynı çeki tekrar tahsile ver → 400 (gövdeyi OKU, -o /dev/null kullanma)
- Çeki karşılıksız işaretle (gerekçe 25 karakter) → bakiye tekrar 50.000; `hareketler` EVRAK_KARSILIKSIZ satırı vade_tarihi DOLU
- İkinci çeki nakit tahsil et → kasa bakiyesi artar; aynı kasanın gününü kapat, sonra tekrar tahsil dene → 400 gün kilidi mesajı
- Aynı banka + çek no ile ikinci giriş → 400/409
- Tarayıcıdan /evraklar ve /evraklar/takvim: süzgeçler, StatKart toplamları, karşılıksız onay akışı, karanlık tema ve 375px genişlik kontrolü
- Başka kiracının oturumuyla evrak kimliğine GET → 404
- API'yi durdur (kur zamanlayıcısı 10 sn'de bir yazar), test kiracısını SİL, `SELECT COUNT(*) FROM tenants` = 5 doğrula

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs (Sabitler.HareketTurleri satır 24-32)`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs (DbSet satır 89 civarı, indeksler 170-195, converter 161-167)`
- `dotnet/Kuyumcu.Api/Data/MasakHashDinleyicisi.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs (satır 35 sonrası)`
- `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs (satır 19-33 Moduller, 100-119 TohumlaGerekiyorsa)`
- `dotnet/denetim.sh (satır 63 ve satır 66)`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs (SozlukNav 50-75, kök sözlük 1040-1075, TR/EN gövdeleri)`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor (satır 34-38 bloğu)`
- `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor (evrak kutusu — paket 27/UX-1 ile ÇAKIŞIR)`
- `dotnet/Kuyumcu.Api/Data/Migrations/ (yeni migration + snapshot)`
- `YENİ: dotnet/Kuyumcu.Api/Data/KiymetliEvrak.cs`
- `YENİ: dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs`
- `YENİ: dotnet/Kuyumcu.Api/Modules/Evrak/EvrakDurumKapisi.cs`
- `YENİ: dotnet/Kuyumcu.Web/Pages/Evraklar.razor`
- `YENİ: dotnet/Kuyumcu.Web/Pages/EvrakTakvimi.razor`
- `YENİ: dotnet/Kuyumcu.Tests/EvrakTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/HttpEvrakTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/EvrakEkranTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/EvrakDurumKapisiTestleri.cs`
- `DOKUNULMAZ (bilinçli): Kuyumcu.Api/Modules/Vade/VadeKapisi.cs · Modules/Masak/MasakKapisi.cs · Data/HareketFabrikasi.cs · Modules/Cari/CariModulu.cs`

## Mevzuat dayanağı
- 459 sıra no.lu VUK Genel Tebliği — 30.000 TL tevsik haddi ve işlem tutarı kuralı — **DOGRULANDI** — Devir belgesindeki A7 araştırması doğrulanmış sayılıyor. Kod karşılığı TevsikKapisi; evrakta yalnız NAKİT tahsil bacağında çalışır.
- MASAK 185.000 TL kimlik tespiti eşiği + bağlantılı işlem kümülatifi — **DOGRULANDI** — A7 ile doğrulanmış. Paket 29 kararı: evrak türleri kümülatife EKLENMEZ (çift sayma, K4-D) — bu bir mevzuat yorumu değil, ölçüm doğruluğu kararıdır ve gevşek yönde olduğu için belgelenir.
- 459 sayılı tebliğ kapsamında çekin 'tevsik edici vasıta' sayılması — **DOGRULANMALI** — Kodda tek etkisi: evrak girişinde nakit payı 0 yazılır. Bu zaten fiili doğrudur (nakit el değiştirmiyor), dolayısıyla hüküm doğrulanmasa bile kod yanlış olmaz. Mevzuat metni koda GÖMÜLMEZ.
- 5941 sayılı Çek Kanunu — karşılıksız çek, ibraz süreleri (10/30/60 gün) — **DOGRULANMALI** — Paket 29 HİÇBİR ibraz süresi kuralını koda koymaz. Karşılıksız işaretlemesi kullanıcı beyanıdır; süre uyarısı istenirse ADR-0005 uyarınca yapılandırmadan okunur ve VARSAYILAN KAPALI olur.
- TTK md. 776 (bono/senet zorunlu unsurları) ve md. 780 (çek unsurları) — **DOGRULANMALI** — Alan zorunluluklarına (keşide yeri/tarihi, borçlu adı) dayanak olarak GÖSTERİLMEZ; Faz A'da bu alanlar opsiyoneldir. Zorunlu kılınacaksa önce madde metni doğrulanır.
- Hurda stopajı vatandaştan alımda yoktur; döviz alım-satımı yetkisiz kuyumcuya yasaktır — **DOGRULANDI** — Paket 29 ile ilgisi: döviz cinsinden evrak kapsam dışı bırakılırken bu kural da gözetildi (K4-G).

## Riskler
- YETKİ SESSİZ 403 (yüksek): Moduller.Tumu'ya "evrak" eklenip modül-başı tohumlama YAPILMAZSA, mevcut 5 kiracının MUDUR/MUHASEBE rolleri ekranı hiç göremez ve hata 'yetkiniz yok' diye görünür — sebebi bulunması saatler alır. M29-09 mutasyonu bunu ölçer.
- DENETİM.SH SAYI KIRILMASI (orta): VeresiyeKapanisi sayacı 2'de bırakılırsa denetim BAŞARISIZ döner ve paket kapıdan geçemez; 3 yapılırken gerekçe yazılmazsa sonraki oturum kuralı gevşek sanır.
- VADE ÇİFT SAYMA (orta): Nakit/banka tahsil hareketine CariId yazılıp aynı anda veresiye payı da verilirse borç iki kez kapanır. Kural: tahsil hareketi OdemeIzi.Nakitten kullanır, veresiye payı 0'dır (VadeKapisi etkisi 0 çıkar).
- MIGRATION ARACI (orta): `dotnet ef migrations add --no-build` derleme kırıkken BOŞ migration üretir ve `remove --force` önceki migration'ı siler (15 Eylül'de VadeTakibi dosyaları bu yüzden silindi). Önce `dotnet build` yeşil.
- DOSYA ÇAKIŞMASI (orta): Cariler.razor, Sozlukler.cs ve KenarCubugu.razor paket 27, 28, UX-1 ve UX-2 Faz B ile ortak. Yaz-seri ihlali derleme çakışması değil, SESSİZ metin kaybı üretir.
- bUnit SAHTE API (düşük ama ısırmış): yeni uç eklendiğinde mevcut ekran testlerinin sahte API'sine gerçekçi yanıt eklenmezse `_ => "{}"` varsayılanı JsonException atar ve ilgisiz testler kırılır (5 CariEkran testi böyle kırıldı).
- TÜRKÇE/KÜLTÜR (düşük): durum adları ve tutarlar ekrana basılırken ToUpperInvariant ve `{0:N2}` tuzakları; TurkceMetin ve Bicim kullanılır.

## Açık sorular
- (yok)

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Tek tablo + evrak_turu kararı doğru (yaşam döngüsü aynı, ayrılan yalnız 4 nullable kolon). K4-B (evrak tablosu bakiyenin kaynağı değil, portföyün aynası; bakiyeyi yalnız defter satırı değiştirir) bu projenin çekirdek değişmezine birebir uyuyor. K4-C (yeni türlerin VadeKapisi formülüne dokunmadan doğru çalışması) elegant ve VadeKapisi'na ikinci özel durum eklemekten kaçınıyor — doğru mimari refleks.
  - Koşullar: FAZ A BÖLÜNMELİ (4-5 gün tek parça sevk edilemez): A1 = tablo + EvrakDurumKapisi + giriş/portföy/tahsile ver/portföye al + liste; A2 = tahsil (kasa/banka) + karşılıksız + takvim + modül-başı yetki tohumlaması. · K4-F (modül-başı yetki tohumlaması) bu paketin GERÇEK ÇEKİRDEĞİDİR ve paket 3 ('siparis'), 6 ('bildirim'), 46 (KASIYER/CIRAK) için önkoşuldur. Sıralama kilidi: 29'un tohumlama mekanizması sevk edilmeden o üç paket sevk edilemez, yoksa mevcut kiracılarda sessiz 403 üretirler. · denetim.sh VeresiyeKapanisi sayısının 2→3 çıkarılması KABUL (gerekçe betiğe yazılmak şartıyla): dosyada gerçekten 'VeresiyeKapanisi (yalnız Etiketten)' beklenen 2. 'Etiketten("VERESIYE", …) ile sayıyı 2'de tutma' alternatifinin reddi DOĞRU — denetimi kandırmak denetimi öldürür. · Senet çift girişinin elle kontrolü (NULL'lu UNIQUE indeks yakalamaz) eşzamanlılıkta TOCTOU'dur; senette banka_adi yerine sabit bir ayırt edici (ör. 'SENET') yazılarak UNIQUE'in senetleri de kapsaması tercih edilmeli, elle kontrol ikinci katman olarak kalmalı. · K4-D (evrak MASAK kümülatifine girmez) gevşek yönde bir karardır; kodda MasakKapisi'nin 'bilinen eksik ölçüm' notunun yanına AÇIKÇA yazılmalı ve uyum belgesine işlenmeli.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Durum makinesi saf sınıfta fail-closed, defter tekeli korunuyor, kimlik alanı ADR-0003 ile şifreli. Tek güvenlik-hassas gevşeme MASAK kümülatifi; çift sayma gerekçesi giriş için geçerli, tahsil için parite şartıyla kabul.
  - Koşullar: MASAK K4-D gevşemesi yalnız EVRAK_GIRIS için kabul (para el değiştirmiyor, altta SATIS var). Nakit/banka TAHSİL ve KARŞILIKSIZ hareketleri için mevcut TAHSILAT/VERESIYE türlerinin MasakKapisi.KapiyaTabiTurler'deki durumu neyse AYNI davranış uygulansın (parite); MasakKapisi.cs:148-149 okunup rapora yazılsın. Kodda ve belgede 'evrak girişi kümülatife girmez' notu (spec böyle). · MasakHashDinleyicisi.cs OKUNMADAN değiştirilmez; BorcluKimlikNo AES + BorcluKimlikNoHash HMAC mevcut GiderPusulasi deseninin aynısı; M29-11 zorunlu ve ham SQL ile 'düz metin değil' kanıtı canlıda alınsın. · K4-F modül-başı tohumlama: SAHIP'in bilinçli kaldırdığı yetki GERİ GELMEZ — bunun testi (HttpEvrakTestleri'ne 'SAHIP MUDUR'dan evrak'ı kaldırdı, yeniden tohumlama sonrası hâlâ 403') zorunlu; aksi hâlde yetki genişlemesi sessizce olur. · 'evrak' modülü TEZGAHTAR'a AÇILMASIN (spec öneri) — borçlu kimlik no taşıyor; MASAK/hurda ile aynı kısıt. Oylamada açılırsa vetom saklı. · Nakit tahsilde KasaDogrula(...,"TRY", an) — 'an' parametresi zorunlu (M29-05), TevsikKapisi transaction öncesi (M29-06). · Senet çift giriş elle kontrolü (M29-07) + evrak tablosu üzerinde ExecuteUpdate/Delete yok. · denetim.sh VeresiyeKapanisi 2→3 KABUL; betiğe gerekçe yorumu şart.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Durum makinesi saf sınıfta, mutasyonlar (M29-01..11) test adlarıyla eşleşiyor, NULL'lu UNIQUE tuzağı (M29-07) doğru görülmüş, MASAK çift sayma kararı gevşek yönde olduğu için belgeleniyor. Tohumlama regresyon testinin fikstürü belirsiz.
  - Koşullar: M29-09 (modül-başı tohumlama) testi 'Mevcut_kiracida_yeni_modul_200' HttpOrtami'de yeni kiracı açar; 'mevcut kiracı' durumu için fikstür, ESKİ tek marker'ı (Rol=__SISTEM__, Modul=__yapilandirildi__) taşıyan bir kiracıyı elle kurmalı — aksi hâlde test yeni tohumlamayı değil yeni kiracı yolunu ölçer. · denetim.sh:63 VeresiyeKapanisi 2→3 değişikliği paket 5 ve 6'nın 'VeresiyeKapanisi=2 değişmez' iddiasıyla çelişir; sıralamada 29 önce sevk edilirse sonraki paketler sayıyı 3 olarak devralmalı (çapraz bulgu). · EvrakTestleri 'Borçlu kimlik no diske ŞİFRELİ iner' testi ham SQL ile okumalı (AlanSifrelemeTestleri deseni); ayrıca MasakHashDinleyicisi OKUNMADI notu var — uygulayıcı o dosyayı okuyup hash üretimi için ayrı test yazsın ('hash ile arama bulur, düz metinle bulmaz'). · M29-05 ('an' parametresi kaldırılır) yalnız tek koruma katmanı olduğu için geçerli; ancak KasaDogrula 'an' verilmeden de derlenir — testin kapanmış güne yazımı gerçekten reddettiği GunKapanisiTestleri deseninde iki kasa/iki gün fikstürüyle kurulmalı. · Cariler.razor evrak kutusu paket 27 ile aynı dosyada; aynı dalgada koşmama şartı kabul ölçütüne yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- MASAK gevşemesi (K4-D) YALNIZ EVRAK_GIRIS için kabul; nakit/banka TAHSİL ve KARŞILIKSIZ için mevcut TAHSILAT/VERESIYE türlerinin MasakKapisi.KapiyaTabiTurler'deki davranışıyla PARİTE şart — MasakKapisi.cs:148-149 okunup rapora yazılır; 'evrak girişi kümülatife girmez' notu kodda MasakKapisi'nin bilinen-eksik-ölçüm notunun yanına ve uyum belgesine işlenir.
- Faz A bölünür (4-5 gün tek parça sevk edilemez): A1 = tablo + EvrakDurumKapisi + giriş/portföy/tahsile ver/portföye al + liste; A2 = tahsil (kasa/banka) + karşılıksız + takvim + modül-başı yetki tohumlaması.
- K4-F modül-başı yetki tohumlaması bu paketin çekirdeğidir ve paket 3 ('siparis'), 6 ('bildirim'), 46 (KASIYER/CIRAK) için ÖNKOŞULDUR — o mekanizma sevk edilmeden bu üç paket sevk edilemez (mevcut 5 kiracıda sessiz 403). SAHIP'in bilinçli kaldırdığı yetki GERİ GELMEZ; testi zorunlu ('yeniden tohumlama sonrası hâlâ 403').
- M29-09 fikstürü düzeltilir: 'mevcut kiracı' durumu ESKİ tek marker'ı (Rol=__SISTEM__, Modul=__yapilandirildi__) taşıyan kiracıyla elle kurulur; aksi hâlde test yeni kiracı yolunu ölçer.
- denetim.sh:63 VeresiyeKapanisi 2→3 KABUL, gerekçe betiğe yorum olarak yazılmak şartıyla ('Etiketten ile sayıyı 2'de tutma' reddi doğru). Paket 5/6'nın 'VeresiyeKapanisi=2 değişmez' iddiasıyla çelişki: 29 önce sevk edilirse sonraki paketler 3'ü devralır.
- Senet çift girişi: banka_adi yerine sabit ayırt edici (ör. 'SENET') yazılarak UNIQUE senetleri de kapsasın; elle kontrol (M29-07, NULL'lu UNIQUE'i yakalamaz, TOCTOU) ikinci katman olarak kalır. Evrak tablosunda ExecuteUpdate/Delete YOK.
- Borçlu kimlik: BorcluKimlikNo AES + BorcluKimlikNoHash HMAC, mevcut GiderPusulasi deseninin aynısı; MasakHashDinleyicisi.cs OKUNMADAN değiştirilmez, okunup hash için ayrı test ('hash ile arama bulur, düz metinle bulmaz'); M29-11 ham SQL ile canlıda 'düz metin değil' kanıtı.
- 'evrak' modülü TEZGAHTAR'a AÇILMAZ (borçlu kimlik no taşır; MASAK/hurda ile aynı kısıt) — açılırsa Mehmet'in vetosu saklıdır.
- Nakit tahsilde KasaDogrula(...,"TRY", an) — 'an' zorunlu (M29-05); KasaDogrula 'an' verilmeden de derlendiği için test GunKapanisiTestleri deseninde iki kasa/iki gün fikstürüyle kapanmış güne yazımın gerçekten reddedildiğini ölçer. TevsikKapisi transaction öncesi (M29-06).
- Vade çift sayma yasağı: tahsil hareketi OdemeIzi.Nakitten kullanır, veresiye payı 0 (VadeKapisi etkisi 0). Cariler.razor evrak kutusu paket 27/UX-1/28 ile aynı dosyadır — aynı dalgada koşmama şartı kabul ölçütüne yazılır (yaz-seri ihlali sessiz metin kaybı üretir).
