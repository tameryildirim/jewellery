# Paket 40 — MASAK risk skorlama + PEP beyanı + yaptırım listesi eşleşme motoru

> Katman: Katman 6 — Uyum (paket 38–42) · Tahmin: 4–6 (Faz A 2–3, Faz B 2–3) gün · Önkoşullar: 41 · Kaynak: katman mimarı K6 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 5
> ⚠ **VETO:** Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KISMİ VETO — İKİ MADDE. (1) 'Kuyumcu.Api/Data/DenetimIzi.cs → DEGISTIR: DenetimServisi'ne YazmaKaydet(...) eklenir. Bugün yalnız OkumaKaydet/ListeOkumaKaydet var (satır 87 civarı)' maddesine VETO: metot DenetimIzi.cs:120-137'de ZATEN VAR, SaveChanges çağırmıyor, çağıranın tx'inde yazıyor ve CariModulu.cs:606'da kullanımda (paket 33 / A17-BİTTİ Q1 kararı B ile geldi). Var olan metodun 'eklenmesi' ya derleme hatası ya da ikinci bir yazma-izi yolu doğurur. Paket onu yalnız ÇAĞIRSIN. (2) FAZ B'nin (yaptirim_kayitlari.ad_unvan'ın AES-256-GCM dışında, ŞİFRESİZ tutulması) Mehmet'in (06 Güvenlik) açık onayı alınmadan sevk edilmesine VETO. MasakKimlikTespiti.BelgeNo bugün şifreli + HMAC hash'li (MasakVarliklari.cs:64-70); yaptırım listesi bu desenden bilinçli bir sapmadır ve güvenliğin veto yetkisi vardır. Faz A bu vetodan ETKİLENMEZ ve tek başına sevk edilebilir. | Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: ŞARTLI VETO yalnız FAZ B için (K40-2/K40-4/K40-5 kapanırsa kalkar): yaptirim_eslesmeleri tablosu bir kişiye 'yaptırım/terör finansmanı listesinde' etiketi yapıştırır — bu KVKK md.6 özel nitelikli veriye yaklaşan bir kayıttır ve spesifikasyon onu HassasVarliklar dışında, okuma izsiz, 200.000 satırlık kotasız/oran sınırsız bir yükleme ucuyla tasarlıyor. Liste adının şifresiz tutulmasını (kamuya açık kaynak şartıyla) ONAYLIYORUM; eşleşme kaydının hassas varlık olmadan sevkini ONAYLAMIYORUM. Faz A (risk motoru + PEP beyanı) vetosuz, koşullu EVET.

## Hedef
Bugün MASAK yükümlülüğü yalnız TUTAR EŞİĞİYLE işliyor: 185.000 TL altındaki her müşteri sistem için aynıdır. Risk temelli yaklaşımın istediği ayrım (kimin profili yüksek riskli, kim siyasi nüfuz sahibi, kim yaptırım listesinde) hiç yok. Bu paket saf bir risk motoru, PEP beyanı ve kiracının yüklediği yaptırım listesine karşı ad eşleşmesi getirir. Veri beslemesi (ticari abonelik) C sınıfıdır ve bu pakette YAZILMAZ; motor ve liste altyapısı yazılır.

## Kodda bugünkü durum
MASAK tarafı bugün: saf eşik motoru (MasakKuralMotoru.cs:93-115 Gereksinim, 121-161 Dogrula), eşikler yapılandırmadan (MasakKuralMotoru.cs:21-62, appsettings 'Masak'), iki fazlı kapı (MasakKapisi.cs:60-94 Dogrula, 156-207 Kaydet), bağlantılı işlem kümülatifi (MasakKapisi.cs:114-123, A8'de eklendi, 8 çağrı noktası), kimlik tespiti/beyan/şüpheli bildirim varlıkları (MasakVarliklari.cs:44-94, 115-146, 164-202), belge no AES-256-GCM şifreli + HMAC hash (KuyumcuDbContext.cs:157-159, ADR-0003), 12 uç (MasakModulu.cs:267-339), Uyum ekranı (Uyum.razor, 475 satır). ⚠ ÖLÇÜM: 'risk' kelimesi Kuyumcu.Api altında YALNIZ 4 yorum satırında geçiyor (GiderPusulasiKapisi.cs:157, UrunModulu.cs:290, SatisModulu.cs:161, SatisModulu.cs:676); 'pep', 'yaptirim', 'yaptırım', 'sanction' → 0 İSABET. Yani risk skoru, PEP ve yaptırım kod tabanında YOKTUR. Cari varlığında da risk/PEP alanı yok (Entities.cs:113-137).

## Kapsam
**İçinde:**
- FAZ A: saf RiskMotoru (puan + seviye + sebepler), cari PEP beyanı, append-only risk profili tarihçesi, risk uçları, Uyum ekranına 'Risk' sekmesi, panel kartı
- FAZ B: kiracının yüklediği yaptırım listesi (CSV/JSON), Türkçe-duyarlı normalize ad eşleşmesi, eşleşme kaydı ve karar (engellendi / yanlış pozitif / incelemede), varsayılan KAPALI yaptırım kapısı
- DenetimServisi.YazmaKaydet (hassas yazma izi) — bu katmanın ortak borcu, burada ödenir
- IYaptirimKaynagi arayüzü (uygulama YOK — dış çağrı açılmaz)

**Dışında:**
- Ticari PEP/yaptırım veri aboneliği ve otomatik besleme (PARITE §4, C sınıfı)
- Bulanık/fonetik ad eşleşmesi (eşik uydurmak yanlış pozitif üretir; kapı varsa işi durdurur)
- Otomatik ŞİB gönderimi (MASAK'ın API'si yok — mimari-tanimi.md §2)
- Risk skoruna dayalı İŞLEM ENGELLEME (mevzuatta doğrulanmış bir 'yüksek riskliyle işlem yasaktır' hükmü yok)
- Müşteri kabul politikası / uyum programı belgesi üretimi

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| cariler | 3 yeni kolon — BEYAN alanları (profil DEĞİL) | pep_beyani BOOL NOT NULL DEFAULT 0 · pep_gorev VARCHAR(200) NULL · pep_beyan_tarihi DATETIME NULL. Neden cariye: beyan müşterinin O ANKİ durumudur ve tek değerdir; hesaplanan SKOR ise tarihçelidir ve ayrı tabloya gider. | (tenant_id, pep_beyani) | YYYYMMDDHHMMSS_MasakRiskPep |
| cari_risk_profilleri | YENİ — APPEND-ONLY tarihçe | id VARCHAR(36) PK · tenant_id VARCHAR(36) · cari_id VARCHAR(36) · kaynak VARCHAR(10) ('HESAP' | 'ELLE') · puan INT · seviye VARCHAR(10) ('DUSUK'|'ORTA'|'YUKSEK') · sebepler TEXT (JSON dizi — motorun ürettiği gerekçe listesi) · gerekce VARCHAR(500) NULL (ELLE ezmede ZORUNLU, ≥20 karakter) · kullanici_id VARCHAR(36) · hesaplanma_tarihi DATETIME · olusturuldu DATETIME. Güncel profil = en son satır. Üzerine YAZILMAZ: 'o gün hangi skorla işlem yaptın' denetimde sorulur. | (tenant_id, cari_id, olusturuldu) · (tenant_id, seviye) | aynı migration |
| yaptirim_kayitlari | YENİ — FAZ B | id · tenant_id · liste_adi VARCHAR(100) · liste_surumu VARCHAR(50) · kayit_tipi VARCHAR(10) ('KISI'|'KURUM') · ad_unvan VARCHAR(300) · ad_normalize VARCHAR(300) (TurkceMetin ile büyük harf + sadeleştirme; ARANAN sütun) · dogum_tarihi DATE NULL · uyruk VARCHAR(3) NULL · kaynak_referans VARCHAR(300) NULL · aktif BOOL · yukleyen_kullanici_id · yuklenme_tarihi DATETIME. ⚠ ad_unvan ŞİFRELENMEZ (eşleşme SQL'de yapılacak, liste kamuya açık) — ADR-0003'ten bilinçli sapma, Mehmet onayına açık. | (tenant_id, ad_normalize) · (tenant_id, liste_adi, aktif) | YYYYMMDDHHMMSS_YaptirimListesi |
| yaptirim_eslesmeleri | YENİ — FAZ B, APPEND-ONLY | id · tenant_id · cari_id VARCHAR(36) NULL · aranan_ad VARCHAR(300) · yaptirim_kayit_id VARCHAR(36) · eslesme_turu VARCHAR(20) ('TAM'|'TOKEN_KUMESI') · karar VARCHAR(20) ('INCELEMEDE'|'ENGELLENDI'|'YANLIS_POZITIF') · karar_gerekcesi VARCHAR(1000) NULL (YANLIS_POZITIF'te ZORUNLU ≥20) · karar_kullanici_id NULL · hareket_id VARCHAR(36) NULL · tarih DATETIME | (tenant_id, karar, tarih) · (tenant_id, cari_id) | aynı migration |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/masak/risk/{cariId}` | SAHIP, MUDUR, MUHASEBE | Carinin GÜNCEL risk profili + son 10 profil satırı (tarihçe). Kişisel veri içermez (skor ve sebepler). | kiracı filtresi |
| POST | `/api/v1/masak/risk/{cariId}/hesapla` | SAHIP, MUDUR, MUHASEBE | Profili yeniden hesaplar ve YENİ SATIR yazar. Motor saf; girdiler burada toplanır. | cari kiracıya ait olmalı |
| POST | `/api/v1/masak/risk/{cariId}/elle` | SAHIP, MUDUR | Seviyeyi elle ezme (kaynak='ELLE'), gerekçe ≥ 20 karakter ZORUNLU. Denetim izine YAZMA kaydı düşer. | gerekçe uzunluğu · DenetimServisi.YazmaKaydet |
| PATCH | `/api/v1/cari/{id}/pep` | SAHIP, MUDUR, MUHASEBE | PEP beyanı (bool + görev + tarih). Beyan MÜŞTERİNİNDİR; sistem tespit etmez. | pep_beyani=true ise pep_gorev zorunlu |
| GET | `/api/v1/masak/risk/ozet` | SAHIP, MUDUR, MUHASEBE | Panel kartı: seviye dağılımı, PEP sayısı, 30 gündür hesaplanmamış profil sayısı. Kişisel veri yok (MasakModulu.cs:336 'ozet' deseni). | — |
| POST | `/api/v1/masak/yaptirim/liste (FAZ B)` | SAHIP | Liste yükleme: JSON dizi (ad_unvan, kayit_tipi, dogum_tarihi?, uyruk?, kaynak_referans?). Aynı liste_adi için yeni sürüm yüklenince eskiler aktif=0 olur — SİLİNMEZ. | satır sayısı üst sınırı yapılandırmadan · DenetimServisi.YazmaKaydet |
| GET | `/api/v1/masak/yaptirim/tara?ad= (FAZ B)` | SAHIP, MUDUR, MUHASEBE | Salt okunur tarama. Eşleşenleri ve eşleşme türünü döndürür; kayıt YAZMAZ. | — |
| PATCH | `/api/v1/masak/yaptirim/eslesme/{id} (FAZ B)` | SAHIP, MUDUR | Eşleşme kararı (ENGELLENDI | YANLIS_POZITIF). Yanlış pozitifte gerekçe ≥ 20 karakter. | karar tekrar değiştirilemez — yeni satır eklenir (append-only) |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Masak/RiskMotoru.cs` — SAF (DB/ağ/saat yok — MasakKuralMotoru.cs:9-11 ilkesi). record RiskGirdi(string CariTuru, bool PepBeyani, string? Uyruk, decimal SonDonemNakitOran, decimal SonDonemToplamTl, int EsigeYakinIslemSayisi, int KimlikEksikSayisi, int SupheliBildirimSayisi, bool YaptirimEslesmesiVar). record RiskSonuc(int Puan, string Seviye, IReadOnlyList<string> Sebepler). Her katsayı RiskAyarlari'ndan gelir; KODA SABİT RAKAM YAZILMAZ (ADR-0005). Seviye eşikleri de yapılandırmadan.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Masak/RiskAyarlari.cs` — public sealed class MasakRiskAyarlari { public bool Etkin { get; init; } = false; public bool YaptirimKapisiEtkin { get; init; } = false; public bool YuksekRiskteBeyanZorunlu { get; init; } = false; public int OrtaEsik {get;init;} = 40; public int YuksekEsik {get;init;} = 70; public int PepPuan {get;init;} = 30; public int YaptirimPuan {get;init;} = 100; public int NakitAgirlikPuan {get;init;} = 15; public int StructuringPuan {get;init;} = 25; public int SupheliGecmisPuan {get;init;} = 20; public string[] YuksekRiskliUlkeler { get; init; } = []; public int ProfilTazelemeGunu {get;init;} = 30; public int YaptirimListesiAzamiSatir {get;init;} = 200000; } — ⚠ Etkin VARSAYILAN FALSE: risk temelli yaklaşımın kuyumcuyu bağlayan biçimi DOĞRULANMADI. Kapalıyken skor HESAPLANIR ve gösterilir, hiçbir kapı kurmaz.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Masak/RiskServisi.cs` — Girdi toplama (cari türü, PEP, uyruk — carinin kimlik tespitlerinden; nakit oranı ve toplam — hareketler.nakit_tutar/tl_tutari son N gün; eşiğe yakın işlem sayısı — kimlik eşiğinin %80-100 bandındaki işlem sayısı; şüpheli bildirim sayısı — masak_supheli_bildirimler), motoru çağırma, profil satırı yazma. TÜM toplamlar sunucuda SumAsync/CountAsync (TB-19). Tarih aralıkları Zaman.YerelGunAraligiUtc.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs` — FAZ B. Liste yükleme (toplu Add, sürüm bazlı aktif/pasif), Normalize(ad) → TurkceMetin kullanılır (ToUpperInvariant TUZAĞI: 'sentetik' → 'SENTETIK' noktasız I; Core/Domain/TurkceMetin.cs), Tara(ad) → önce TAM eşleşme (ad_normalize =), sonra TOKEN KÜMESİ eşleşmesi (aranan adın tüm tokenları kayıtta var). Bulanık eşleşme YOK.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` — FAZ B: Dogrula/DogrulaKumulatif'e OPSİYONEL bir 'risk/yaptırım kontrolü' adımı EKLENMEZ — kapı sınıfı şişirilmez. Bunun yerine ayrı YaptirimKapisi sınıfı yazılır ve 8 çağrı noktasına DEĞİL, yalnız cari OLUŞTURMA/GÜNCELLEME ve kimlik tespiti kaydı anında çağrılır (ad o an bilinir). Gerekçe: her satışta liste taraması, kapının bugünkü maliyet profilini bozar ve cari değişmediyse sonuç da değişmez.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimKapisi.cs` — FAZ B. Eşleşme varsa yaptirim_eslesmeleri'ne 'INCELEMEDE' satırı yazar. MasakRisk:YaptirimKapisiEtkin TRUE ise BadRequestException (fail-closed); FALSE ise yalnız kayıt + uyarı. Varsayılan FALSE.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/DenetimIzi.cs` — DenetimServisi'ne YazmaKaydet(AktifKullanici, string varlikTuru, string varlikId, string aciklama, bool hassas) eklenir. Bugün yalnız OkumaKaydet/ListeOkumaKaydet var (satır 87 civarı). Append-only koruması (DenetimYazmaDinleyicisi, ADR-0004) aynen geçerli.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs` — CariRiskProfili, YaptirimKaydi, YaptirimEslesmesi sınıfları buraya eklenir (uyum varlıklarının evi burası). Hepsi IKiraciVarlik.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs` — Controller'a 5 (Faz B ile 8) uç. Mevcut 12 uç dokunulmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs` — CariGuncelleDto'ya PEP alanları + PATCH /cari/{id}/pep ucu; Faz B'de cari oluşturma/güncellemede YaptirimKapisi çağrısı.

## Ekranlar
- `/uyum` · `dotnet/Kuyumcu.Web/Pages/Uyum.razor` — Yeni 'Risk' sekmesi (mevcut sekme çubuğu, Uyum.razor:40): seviye dağılımı kartları, yüksek riskli cari listesi (ad + seviye + sebepler; KİMLİK BİLGİSİ YOK), cari seçilince profil tarihçesi ve 'Yeniden hesapla' / 'Seviyeyi elle değiştir' (gerekçe < 20 karakter → Kaydet KAPALI). Faz B'de 'Yaptırım' sekmesi: liste yükleme, açık eşleşmeler, karar verme. Mehmet'in PLAN-2 koşulları aynen geçerli (Uyum.razor:10-17): ön yükleme yasak, detay modal, listede maskeleme. (menü/yetki: Menü değişmez (KenarCubugu.razor:57). MasakController rol bazlı; MUHASEBE+.)
- `/cariler` · `dotnet/Kuyumcu.Web/Pages/Cariler.razor` — Düzenle modalına 'Siyasi nüfuz sahibi (PEP) beyanı' onay kutusu + görev alanı (işaretliyse görev zorunlu, boşsa Kaydet kapalı). Cari kartında risk seviyesi rozeti (salt okunur). (menü/yetki: Modül 'cari'.)
- `/` · `dotnet/Kuyumcu.Web/Pages/Panel.razor` — Uyum kartına 'Yüksek riskli cari: N' ve (Faz B) 'Açık yaptırım eşleşmesi: N' satırları. (menü/yetki: —)

## İş kuralları (fail-closed)
- RİSK SKORU İŞLEMİ DURDURMAZ. Mevzuatta doğrulanmış bir 'yüksek riskli müşteriyle işlem yasaktır' hükmü yoktur; koymak uydurma olurdu. Skor uyarır, sıkılaştırılmış tedbir ÖNERİR, şüpheli bildirime yönlendirir.
- MasakRisk:Etkin VARSAYILAN FALSE. Kapalıyken skor hesaplanır ve gösterilir; hiçbir kapı kurulmaz, hiçbir ek alan zorunlu olmaz.
- MasakRisk:YaptirimKapisiEtkin VARSAYILAN FALSE. Açıldığında TAM eşleşme fail-closed durdurur (BadRequestException). Kapalıyken eşleşme yalnız KAYDEDİLİR ve ekranda gösterilir. Varsayılanın kapalı olması bilinçlidir: yanlış/eksik bir listeyle dükkânı durdurmak da kabul edilemez.
- PEP BEYANDIR, TESPİT DEĞİLDİR. Sistem kimsenin PEP olup olmadığını kendi bilmez; müşterinin beyanını kaydeder ve beyan tarihini tutar. Aksi bir iddia arayüzde yer almaz.
- Risk profili ÜZERİNE YAZILMAZ. Her hesaplama ve her elle müdahale YENİ SATIRdır. Güncel profil en son satırdır.
- Elle seviye ezmede gerekçe ≥ 20 karakter ZORUNLU ve denetim izine YAZMA kaydı düşer. Gerekçesiz ezme, skoru anlamsız kılar.
- Yaptırım listesi SİLİNMEZ; yeni sürüm yüklenince eski satırlar aktif=0 olur. 'O gün hangi listeye karşı taradın' sorusu cevaplanabilir kalmalı.
- Ad eşleşmesi TurkceMetin ile normalize edilir. ToUpperInvariant kullanılırsa noktasız I yüzünden 'İSMAİL' ile 'ISMAIL' eşleşmez (kayıtlı tuzak).
- BULANIK EŞLEŞME YOK. Benzerlik eşiği uydurmak yanlış pozitif üretir; kapı açıkken iş durdurur. Yalnız TAM ve TOKEN KÜMESİ eşleşmesi.
- Yaptırım taraması HER SATIŞTA DEĞİL, cari oluşturma/güncelleme ve kimlik tespiti kaydı anında çalışır. Ad değişmediyse sonuç değişmez; her satışta taramak kapının maliyet profilini bozar.
- Structuring sinyali: kimlik eşiğinin %80–100 bandında kalan işlem sayısı puan üretir. Eşiğin kendisi (185.000) yapılandırmadan gelir, band oranı da (MasakRisk).
- Risk uçları KİŞİSEL VERİ DÖNDÜRMEZ: skor, seviye, sebep listesi ve cari adı. Kimlik belge numarası bu uçlarda hiç geçmez.

## Testler
- `dotnet/Kuyumcu.Tests/RiskMotoruTestleri.cs` [saf]
  - Sıfır girdi → DUSUK, puan 0, sebep listesi boş
  - PEP beyanı → puan += PepPuan ve sebep listesinde 'PEP beyanı' var
  - Yaptırım eşleşmesi → YUKSEK (tek başına eşiği aşar)
  - Nakit oranı yüksek + structuring sinyali birlikte → ORTA'dan YUKSEK'e geçer
  - Seviye eşikleri yapılandırmadan gelir: OrtaEsik değiştirilince seviye değişir (koda gömülü olmadığının kanıtı)
  - Yüksek riskli ülke listesi BOŞken uyruk hiç puan üretmez
- `dotnet/Kuyumcu.Tests/RiskServisiTestleri.cs` [DB]
  - Hesapla YENİ SATIR yazar, eskisini güncellemez (iki hesaplama → iki satır)
  - Elle ezme gerekçesiz → 400; gerekçeli → yeni satır kaynak='ELLE' ve denetim izinde YAZMA kaydı var
  - PEP beyanı true, görev boş → 400
  - Güncel profil = en son olusturuldu satırı (iki satır arasında sıralama doğru)
  - Yabancı kiracının carisi için hesapla → 404
  - MasakRisk:Etkin=false iken hiçbir işlem akışı ETKİLENMEZ (satış aynen geçer)
- `dotnet/Kuyumcu.Tests/YaptirimTestleri.cs` [DB]
  - Liste yükleme: 3 satır, ad_normalize doğru üretilmiş
  - İkinci sürüm yüklenince birinci sürüm aktif=0, SİLİNMEMİŞ
  - 'İsmail Yılmaz' ile 'ISMAIL YILMAZ' EŞLEŞİR (TurkceMetin; ToUpperInvariant ile eşleşmezdi)
  - Token sırası değişik ad ('Yılmaz İsmail') token kümesiyle eşleşir
  - YaptirimKapisiEtkin=false → cari kaydedilir, eşleşme satırı 'INCELEMEDE' yazılır
  - YaptirimKapisiEtkin=true → cari kaydedilmez, 400, HİÇBİR satır yazılmaz
  - Yanlış pozitif kararı gerekçesiz → 400
  - Kararı ikinci kez değiştirmek → yeni satır (append-only), eskisi durur
- `dotnet/Kuyumcu.Tests/RiskEkranTestleri.cs` [bUnit]
  - Risk sekmesi açılınca /masak/risk/ozet çağrılır
  - Cari seçilmeden tarihçe ÇEKİLMEZ (ön yükleme yasağı — Mehmet koşulu)
  - Gerekçe < 20 karakter → 'Seviyeyi değiştir' Kaydet KAPALI (disabled="@(false)" tuzağı)
  - PEP kutusu işaretli, görev boş → Cariler modalında Kaydet KAPALI
  - Risk uçlarının yanıtında kimlik belge numarası alanı YOK (sahte API tam yanıt dönse bile ekran basmaz)
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - TEZGAHTAR ile GET /api/v1/masak/risk/{id} → 403
  - TEZGAHTAR ile POST /api/v1/masak/yaptirim/liste → 403 (SAHİP)

## Mutasyon kanıtları
- Puan/eşik rakamları koda gömülürse → RiskMotoruTestleri 'eşikler yapılandırmadan' KIRMIZI
- Risk profili üzerine yazılırsa (update) → 'iki hesaplama iki satır' KIRMIZI
- Elle ezmede gerekçe kontrolü kaldırılırsa → 'gerekçesiz elle ezme 400' KIRMIZI
- Denetim izine YAZMA kaydı düşmezse → 'elle ezmede yazma kaydı var' KIRMIZI
- Normalize TurkceMetin yerine ToUpperInvariant olursa → 'İsmail/ISMAIL eşleşir' KIRMIZI
- Token kümesi eşleşmesi kaldırılırsa → 'token sırası değişik ad' KIRMIZI
- YaptirimKapisiEtkin varsayılanı true yapılırsa → 'kapalıyken cari kaydedilir' KIRMIZI
- Kapı açıkken eşleşmede işlem geçerse (fail-open) → 'açıkken 400 ve hiçbir satır yazılmaz' KIRMIZI
- Risk skoru işlemi durduracak biçimde kapıya bağlanırsa → 'Etkin=false iken satış aynen geçer' ve satış akış testleri KIRMIZI
- Yaptırım listesi yeni sürümde SİLİNİRSE → 'birinci sürüm aktif=0, silinmemiş' KIRMIZI
- Risk ucu kimlik belge no döndürürse → ekran/DB testi KIRMIZI

## Canlı doğrulama
- Yeni kiracı (RISK-TEST). MasakRisk:Etkin varsayılan false ile başla.
- 3 cari aç: normal müşteri, PEP beyanlı müşteri, yüksek nakit oranlı müşteri (birkaç nakit işlem gir; tevsik haddine takılmamak için 30.000 altı).
- POST /masak/risk/{id}/hesapla → üç cari için puan/seviye; perl ile motorun formülünü bağımsız hesapla, birebir karşılaştır.
- İkinci kez hesapla → SQL: cari_risk_profilleri'nde İKİ satır (güncelleme değil).
- Elle seviye ezme, gerekçe 10 karakter → 400; 25 karakter → 200; SQL: kaynak='ELLE' satırı + denetim_kayitlari'nda yazma kaydı.
- PEP beyanı true + görev boş → 400; görev dolu → 200; SQL: cariler.pep_* doldu.
- MasakRisk:Etkin=false iken normal bir satış yap → 200 (risk hiçbir şeyi durdurmadı).
- FAZ B: 5 satırlık yaptırım listesi yükle. Türkçe harf içeren bir ad koy. Aynı adı taşıyan cari aç → YaptirimKapisiEtkin=false olduğu için 200 + SQL'de 'INCELEMEDE' eşleşme satırı.
- YaptirimKapisiEtkin=true yap, API'yi yeniden başlat, aynı adla ikinci cari → 400 ve SQL: cariler sayısı ARTMADI.
- Eşleşmeyi 'YANLIS_POZITIF' yap (gerekçeli) → yeni satır; eskisi duruyor.
- Listenin ikinci sürümünü yükle → SQL: eski satırlar aktif=0, silinmemiş.
- Test kiracısını sil (API'yi önce durdur).

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/DenetimIzi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/RiskMotoru.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/RiskAyarlari.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/RiskServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Core/Domain/TurkceMetin.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Uyum.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`
- `dotnet/denetim.sh`

## Mevzuat dayanağı
- MASAK Tedbirler Yönetmeliği md.5/1-(b) — 185.000 TL eşiği, işlem tutarı ya da birbiriyle bağlantılı işlemlerin toplamı — **DOGRULANDI** — A7/A8 araştırması, 8.5/10, iki hasım mercek de doğruladı. Kodda uygulanıyor (MasakKapisi.cs:114-123). Bu paket eşiği DEĞİŞTİRMEZ; structuring sinyalini bu eşiğin %80-100 bandından türetir.
- Risk temelli yaklaşım / müşterinin tanınması ve uyum programı yükümlülüğünün KUYUMCUYU kapsayıp kapsamadığı — **DOGRULANMALI** — Uyum programı yükümlülüğü yükümlü gruplarına göre değişir; perakende kuyumcunun bu kapsamda olup olmadığı doğrulanmadı. Bu yüzden MasakRisk:Etkin VARSAYILAN FALSE ve risk hiçbir işlemi durdurmaz. Doğrulanırsa yalnız yapılandırma açılır, kod değişmez.
- Siyasi nüfuz sahibi kişi (PEP) tanımı ve sıkılaştırılmış tedbir yükümlülüğü — **DOGRULANMALI** — Tanımın kapsamı (kişinin kendisi, yakınları, ilişkili kişiler) ve tedbirin içeriği doğrulanmadı. Bu yüzden sistem yalnız BEYAN kaydeder; kendi başına PEP TESPİTİ yapmaz ve PEP olan müşteriye işlem kısıtı koymaz.
- 6415 s. Terörizmin Finansmanının Önlenmesi Hakkında Kanun / BMGK kararları uyarınca malvarlığının dondurulması — **DOGRULANMALI** — Yaptırım listesine düşen kişiyle işlem yasağının kuyumcu için biçimi doğrulanmadı. Kapı yazıldı ama VARSAYILAN KAPALI; açıldığında fail-closed durdurur. Listenin kendisi dış veri beslemesidir (PARITE §4, C sınıfı) ve bu pakette YAZILMAZ.
- MASAK Kuyumcu ŞİB Rehberi 2.0 §4.2.7 — gerekçe asgari 300 / azami 50.000 karakter — **DOGRULANMALI** — Kod tabanında zaten yapılandırmadan okunuyor (MasakKuralMotoru.cs:39-55). Bu paket o kuralı değiştirmez; risk sebepleri yüksek riskte ŞİB gerekçesine ÖNERİ metni olarak sunulur, otomatik doldurulmaz (rehber şablon tekrarını yasaklıyor — MasakVarliklari.cs:176-180).

## Riskler
- Paketin en büyük riski KAPSAM ŞİŞMESİDİR. Faz A (risk + PEP) tek başına sevk edilebilir ve 2–3 gündür; Faz B (yaptırım) 2–3 gün daha. Faz B'yi Faz A ile birlikte sevk etmeye çalışmak paketi 6 günün üstüne taşır.
- Yaptırım listesi ad/unvan taşır ve şifrelenmez. ADR-0003'ten bilinçli sapmadır; Mehmet'in veto yetkisi vardır ve onayı alınmadan Faz B sevk EDİLMEZ.
- Risk skoru yanlış kalibre edilirse kullanıcı onu görmezden gelmeye alışır ve sinyal değerini kaybeder. Bu yüzden tüm katsayılar yapılandırmadadır ve sebep listesi her zaman gösterilir ('neden yüksek' sorusu cevapsız kalmaz).
- Yaptırım taramasının cari oluşturma/güncellemeye bağlanması, cari kartı OLMAYAN tezgâh müşterisini kapsamaz. Bu bilinen bir sınırdır (A7'deki 'tezgâh müşterisi' sorusuyla aynı kök) ve koda yazılır; kimlik tespiti kaydı anında ad bilindiği için orada da taranır, bu boşluğu daraltır ama kapatmaz.
- cariler tablosuna kolon eklemek sıcak bir tabloyu genişletir; 3 kolon ve tek indeks ile sınırlı tutuldu.

## Açık sorular
- Perakende kuyumcu MASAK uyum programı / risk temelli yaklaşım yükümlüsü müdür? (Bu cevap MasakRisk:Etkin varsayılanını belirler.)
- Yaptırım listesi ad/unvanının şifrelenmemesi kabul mü (Mehmet)? Alternatif: HMAC hash ile eşleşme — tam eşleşme çalışır ama token kümesi eşleşmesi ÇALIŞMAZ.
- Risk skoru formülünün katsayıları kim belirleyecek? Bu belge varsayılan katsayı ÖNERİR, mevzuat dayanağı iddia ETMEZ.
- Yüksek riskli ülke listesi nereden gelecek? Varsayılan BOŞ bırakıldı; doldurulmadan uyruk hiç puan üretmez.
- Yaptırım kapısı açıkken eşleşen bir cariyle MEVCUT açık bakiyeler ne olacak? (Yeni işlem engellenir; geçmişe dokunulmaz — ama 'malvarlığı dondurma' yükümlülüğünün karşılığı bu mudur, doğrulanmalı.)
- DenetimServisi.YazmaKaydet'in imzası ve hassaslık bayrağı A17'deki cari düzeltme ihtiyacını da karşılıyor mu? (Karşılıyorsa A17'nin açık maddesi bu paketle kapanır.)

## Oylama
- **Bertan — 02 Baş Mimar. Mercek: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilirlik), TB-19 ölçeklenme, ADR uyumu. İstenen 5 paket yerine 8 pakette dosya:satır kanıtı açıp doğruladım (4, 1, 34, 35, 38, 40, 41, 42); 37 ve 39'un temel iddialarını grep ile ölçtüm. AÇTIĞIM DOSYALAR: PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (tamamı), SONRAKI-OTURUM-PROMPT.md (§1 + A17 / A17-BİTTİ), dotnet/denetim.sh (tamamı), Kuyumcu.Api/Data/Entities.cs (Sabitler 15-40, Hareket 300-340, BankaHesap/BankaHareket/Fatura 440-505), Data/UrunBelgesi.cs (tamamı), Data/KuyumcuDbContext.cs (DbSet + indeks 73-190), Data/DenetimIzi.cs (60-160), Data/MasakVarliklari.cs (40-70), Modules/Fatura/FaturaModulu.cs (1-180), Modules/Banka/BankaModulu.cs (170-310), Modules/Masak/MasakKapisi.cs (100-215), Modules/Tevsik/TevsikKapisi.cs (50-115), Modules/Maliyet/KarZararServisi.cs (35-60, 110-130, 200-215, 255-310), Modules/Rapor/RaporModulu.cs (25-45, 85-100, 150-175, 280-300, 420-495, 505-551), Modules/ModulKayit.cs (tamamı), Kuyumcu.Web/Pages/Faturalar.razor (240-310), dokumanlar/02-mimari/adr/ (dizin + 0007). OKUNMADI (bu yüzden bu dosyalara dayanan iddiaları DOGRULANMALI işaretledim): MaliyetMotoru.cs, EmanetModulu.cs, SatisModulu.cs gövdesi, FaturaNoUretici.cs, UyumServisi.cs gövdesi, mimari-tanimi.md, teknik-borc-envanteri.md, ADR-0003/0004/0005/0008/0009/0012/0013/0014 gövdeleri.: KOSULLU** — Boşluk ölçümü doğru ve kanıtlı (0 isabet). Risk skorunun işlemi DURDURMAMASI, doğrulanmamış hükümden kapı kurulmaması ve dış ağ yüzeyinin genişletilmemesi (IYaptirimKaynagi'nın uygulamasız bırakılması) üçü de projenin mevcut disipliniyle tutarlı. Append-only risk profili kararı ADR-0004 ruhuyla doğru. Paket bu oylamadaki en büyük kalem; Faz ayrımı olmadan sevk edilemez. Bayat okumadan doğan YazmaKaydet kalemi çıkarılmadan uygulamaya geçilmemeli.
  - Koşullar: 'DenetimServisi.YazmaKaydet eklenir' KALEMİ PAKETTEN ÇIKARILACAK — metot DenetimIzi.cs:120'de VAR ve kullanımda. Yeniden yazılırsa çift mekanizma doğar. Paket onu YALNIZ ÇAĞIRIR. Bu düzeltme K6'nın ortak_kararlar maddesini ve paket 42'nin '40'ı bekler' gerekçesini de çürütür. · FAZ A ve FAZ B AYRI PAKET GİBİ SEVK EDİLECEK. Faz A (risk motoru + PEP beyanı + append-only profil) 41'siz de sevk edilebilir; önkoşul [41] YALNIZ Faz B için geçerli olsun (nakit oranı girdisine banka bacağı orada katılıyor). · FAZ B MEHMET'İN (06 Güvenlik) AÇIK ONAYI OLMADAN SEVK EDİLMEZ. yaptirim_kayitlari.ad_unvan'ın şifresiz tutulması ADR-0003'ten bilinçli sapmadır ve Mehmet'in veto yetkisi vardır. Onay gelmezse alternatif: HMAC hash ile YALNIZ tam eşleşme (token kümesi eşleşmesi düşer, bu kayıp açıkça yazılır). Ben Baş Mimar olarak sapmayı savunmuyorum da reddetmiyorum — karar güvenliğindir. · MasakRisk:Etkin ve YaptirimKapisiEtkin VARSAYILAN FALSE kalacak ve 'kapalıyken hesaplar, durdurmaz' davranışı TevsikKapisi.cs:55-60 deseniyle BİREBİR aynı kurulacak. İki ayrı 'kapalı' semantiği doğmasın. · YaptirimKapisi'nın MasakKapisi'ne DEĞİL ayrı sınıfa konması ve 8 çağrı noktasına DEĞİL yalnız cari oluşturma/güncelleme + kimlik tespiti anına bağlanması doğru — KORUNSUN. Her satışta liste taraması MasakKapisi.GunlukIslemToplam'ın (MasakKapisi.cs:136-146) zaten sıcak olan maliyet profilini bozar. · Risk katsayılarının TAMAMI yapılandırmadan gelecek ve KODA TEK SABİT RAKAM YAZILMAYACAK (ADR-0005). 'Eşikler yapılandırmadan' testi paketin ilk testi olsun. · Yaptırım taramasının cari kartı OLMAYAN tezgâh müşterisini kapsamadığı sınırı SistemKapsami.cs'e ya da sınıf başlığına AÇIKÇA yazılacak — riskler bölümünde kalması yetmez.
- **Mehmet — 06 Güvenlik (veto yetkili). Ölçüt: kiracı izolasyonu (IKiraciVarlik + HasQueryFilter KuyumcuDbContext.cs:194-208 + KiraciYazmaDenetcisi Program.cs:93 + ExecuteUpdate beyaz listesi 4 = Auth×3/Kur×1, denetim.sh:53 doğrulandı), rol/yetki ([RolGerekli] TAM eşleşme Auth/RolFiltresi.cs:15 `roller.Contains(kullanici.Rol)`; [ModulYetkisi] YetkiModulu.cs:126-137), fail-closed mevzuat kapıları (MasakKapisi iki faz :12-18, kümülatif :114-143; tevsik; gün kilidi; kasa para birimi; vade), alan şifreleme (ADR-0003, KuyumcuDbContext.cs:157-166), denetim izi append-only (DenetimIzi.cs; YazmaKaydet ZATEN VAR :120-131), oran sınırı (OranSiniri.cs, GlobalLimiter zincirleme :62-74), dosya yükleme (magic byte, boyut, tenant 404), sır yönetimi. Yalnız 11 paket spesifikasyonu geldi (K0: 4,1 · K5: 34,35,37,36 · K6: 38,39,40,41,42); K23/K4/K7 katmanları `sonuc: null` → o 16 paket için oy VERİLEMEZ, oylama eksik.: KOSULLU** — Risk motoru saf ve yapılandırmadan, PEP beyan (tespit değil), kapılar varsayılan kapalı, dış ağ yüzeyi genişlemiyor (IYaptirimKaynagi uygulamasız), bulanık eşleşme yok, TurkceMetin kullanımı doğru (Core/Domain/TurkceMetin.cs:22). Şifreleme sapması gerekçeli ve kabul edilebilir — liste kamuya açık ise; ama EŞLEŞME kaydı kişiye 'yaptırım listesinde' etiketi yapıştırır ve bu, TCKN'den daha hassas bir veridir; bu tabloya ayrı koruma şart. Spesifikasyon ayrıca YazmaKaydet'i yeniden icat ediyor — koda göre eski.
  - Koşullar: K40-1 (STALE — çift mekanizma): 'DenetimServisi.YazmaKaydet eklenir' maddesi ÇIKARILIR. Metot ZATEN VAR: DenetimIzi.cs:120-131 `YazmaKaydet(AktifKullanici islem, string varlikTuru, string? varlikId, string detay, bool hassas = true)`; SaveChanges ÇAĞIRMAZ, çağıranın transaction'ında yazılır (A17-BİTTİ, Q1→B). Paket 40 bu imzayı KULLANIR; ikinci bir YazmaKaydet yazılırsa iki iz mekanizması doğar. Elle ezme ve yaptırım kararı tx içinde bu metotla, tek SaveChanges. · K40-2 (yaptırım listesi ŞİFRESİZ — ONAYLIYORUM, şartlı): ad_unvan şifrelenmeyebilir ÇÜNKÜ (a) kaynak kamuya açık liste olmak zorunda — `kaynak_referans` NOT NULL yapılır ve liste_adi kapalı bir kaynak sözlüğünden seçilir (BMGK/6415 MASAK malvarlığı dondurma/OFAC vb.; serbest metin liste adı kabul edilmez), (b) kiracı tarafından yüklenen listenin 'kamuya açık' olduğu SAHİP onay metniyle beyan edilir (ADR-0014 SART_2 deseni: birebir onay metni), (c) `yaptirim_eslesmeleri` tablosu ise KVKK md.6 anlamında ÖZEL NİTELİKLİ kişisel veriye (ceza/güvenlik tedbiri şüphesi) yaklaşır → 'YaptirimEslesmesi' HassasVarliklar'a EKLENİR, her okuma (liste/detay) ListeOkumaKaydet/OkumaKaydet düşürür, `karar_gerekcesi` serbest metnine TCKN yazılmaz (DenetimIzi.cs:117 ilkesi, ekranda uyarı), (d) eşleşme listesi ekranı Uyum.razor'un PLAN-2 koşullarına tabi: ön yükleme yok, detay modal. · K40-3 (risk uçları kişisel veri): GET /masak/risk/{cariId} ve yüksek riskli cari listesi cari ADI döner → Cari hassas varlık; liste ucu ListeOkumaKaydet, tekil uç OkumaKaydet çağırır. `sebepler` JSON'ına belge no/TCKN/uyruk-adres gibi kimlik alanı yazılmaz (yalnız kod sabitleri: 'PEP_BEYANI', 'NAKIT_AGIRLIK', …); test 'sebepler kimlik verisi içermez'. · K40-4 (liste yükleme DoS yüzeyi): POST /masak/yaptirim/liste 200.000 satır JSON = onlarca MB gövde. (a) [RolGerekli('SAHIP')] attribute, (b) [EnableRateLimiting(OranSiniri.DisaAktarim)] ile aynı saatlik kota (yeni politika gerekmez; 10/saat yeterli), (c) [RequestSizeLimit] yalnız bu uçta, ölçülüp konur, küresel sınır gevşetilmez, (d) `YaptirimListesiAzamiSatir` aşılırsa 400 ve HİÇBİR satır yazılmaz (tek tx). · K40-5: cari_risk_profilleri, yaptirim_kayitlari, yaptirim_eslesmeleri APPEND-ONLY: DenetimModulu'ndaki DenetimIziDegistirilemez desenine (DenetimModulu.cs:21) benzer biçimde KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi zincirinde bu üç tür için Modified/Deleted durumu REDDEDİLİR (yaptirim_kayitlari için yalnız `aktif` alanı Modified kabul edilir — sürüm pasifleştirme). Test: profil satırı Update denemesi → InvalidOperationException. · K40-6: MasakRisk:Etkin ve YaptirimKapisiEtkin varsayılan FALSE — KABUL (doğrulanmamış hüküm işlemi durdurmaz). Ancak açıldığında fail-closed davranışı DEĞİŞMEZ ve 'kapı açıkken eşleşmede 400, hiçbir satır yazılmaz' mutasyonu Faz B sevk şartıdır. · K40-7: Faz B, Faz A'dan AYRI sevk edilir; Faz B bu koşullar (K40-2/4/5) kapanmadan sevk EDİLMEZ.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçüt: (1) her mutasyon kanıtı gerçekten bir testi kırmızıya çeviriyor mu (çok katmanlı koruma tuzağı: global HasQueryFilter zaten 404 üretirken 'TenantId şartı kaldırılırsa test kırmızı' iddiası yanlıştır), (2) test türü yerleşimi (DataAnnotations yalnız controller yolunda çalışır → 'limit yoksa 400' DB testi olamaz), (3) bUnit kararsızlığı (WaitForAssertion, SONRAKI-OTURUM-PROMPT §6:847'de adı yakalanamayan kararsız test hâlâ açık), (4) kültür (GrafikKulturTestleri deseni: testler invariant koşar, tr-TR hatası görünmez), (5) canlı doğrulama ölçülebilirliği (HTTP kodu + SQL sayımı), (6) test kiracısı temizliği, (7) 'mevcut durum' iddialarının kod gerçeğiyle uyumu. Yalnız 11 paket (K0: 4, 1 · K5: 34, 35, 37, 36 · K6: 38, 39, 40, 41, 42) teslim edildi; K23/K4/K7 sonuç null geldi — 16 paket oylanamadı, fail-closed: onaysız. Tüm okuma salt okunur yapıldı; dotnet build/test ÇALIŞTIRILMADI (paralel derleme yasağı).: KOSULLU** — Saf motor + append-only profil + varsayılan KAPALI kapı QA açısından doğru desen (MasakKuralMotoru/TevsikKapisi.Etkin, TevsikKapisi.cs:55-58). Ancak bir test (Türkçe eşleşme) mevcut TurkceMetin ile matematiksel olarak geçemez ve YazmaKaydet iddiası bayat. Faz A koşullarla; Faz B Mehmet'e bağlı.
  - Koşullar: 'DenetimServisi.YazmaKaydet eklenir' maddesi ve ortak_kararlar/6 BAYAT: YazmaKaydet ZATEN VAR (Data/DenetimIzi.cs, 'GEREKÇELİ yazma izi' bloğu; imza YazmaKaydet(AktifKullanici islem, string varlikTuru, string? varlikId, string detay, bool hassas = true); SaveChanges ÇAĞIRMAZ — çağıranın transaction'ında yazılır; A17-BİTTİ 2026-09-16). Paket mevcut imzayı kullanır, DenetimIzi.cs'e DOKUNMAZ; 'Elle ezmede yazma kaydı var' testi aynı SaveChanges'te izin yazıldığını (satır sayısı +1) sınar; acik_sorular 6 kapanmıştır. · YaptirimTestleri "'İsmail Yılmaz' ile 'ISMAIL YILMAZ' EŞLEŞİR (TurkceMetin)" TASARIM GEREĞİ KIRMIZI olur: TurkceMetin.Buyut yalnız tr-TR ToUpper yapar (TurkceMetin.cs:22: 'İsmail'→'İSMAİL', 'ISMAIL'→'ISMAIL'; İ≠I). Normalize = Türkçe büyütme + diyakritik katlama (İ/I→I, Ş→S, Ç→C, Ğ→G, Ü→U, Ö→O) olarak TANIMLANIR (TurkceMetin'e 'Sadelestir' eklenir — dosya ayak izinde zaten var); test her iki yönü ('ISMAIL'→'İsmail' ve tersi) ve mutasyon 'yalnız ToUpperInvariant' ile 'yalnız Buyut' iki ayrı hâli sınar. · Yapılandırma DB testlerinde IConfiguration'dan değil ctor'dan verilir: RiskServisi(db, MasakRiskAyarlari), YaptirimKapisi(db, MasakRiskAyarlari); 'YaptirimKapisiEtkin=true → 400 ve HİÇBİR satır yazılmaz' testi Etkin=true nesnesiyle; canlıda 'API yeniden başlat' adımı korunur. · Faz B (yaptırım listesi, ad_unvan şifresiz) Mehmet'in onayı alınmadan test dosyası bile açılmaz — QA olarak Faz A'yı ayrı sevk edilebilir sayıyorum; Faz B oyu Mehmet'e. · 'Etkin=false iken hiçbir işlem akışı ETKİLENMEZ' testi tek başına tautoloji; mutasyon 'risk kapıya bağlanırsa satış testleri KIRMIZI' için mevcut satış/MASAK süitine dayanılır (MasakEntegrasyonTestleri, MasakBaglantiliIslemTestleri) — kabul, ayrıca yeni test gerekmez. · RiskEkranTestleri 'Cari seçilmeden tarihçe ÇEKİLMEZ' (Mehmet'in ön yükleme yasağı, Uyum.razor:11-16): SahteApi kayıt listesinde /masak/risk/{id} yolunun render sonrası 0 kez geçtiği WaitForAssertion ile sınanır; 'Kaydet KAPALI' disabled varlığı. · 'Risk ucu kimlik belge numarası DÖNDÜRMEZ' DB testi: yanıt JSON'a serialize, TryGetProperty('belgeNo'/'BelgeNo') false (OrdinalIgnoreCase). · Canlı doğrulama: perl ile motor formülü bağımsız hesap ✓; SQL sayımları ✓; MasakRisk ayarları değiştirilip API yeniden başlatıldığında test kiracısı silinmeden API durdurulur.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO (Mehmet, şartlı, yalnız FAZ B): yaptirim_eslesmeleri bir kişiye 'yaptırım/terör finansmanı listesinde' etiketi yapıştırır → KVKK md.6 özel nitelikliye yaklaşır. Hassas varlık olmadan, okuma izsiz, kotasız yükleme ucuyla sevk EDİLMEZ. Faz A vetosuz.
- VETO KALKMA ŞARTI K40-2: ad_unvan ŞİFRESİZ kalabilir ama kaynak KAMUYA AÇIK olmak zorunda — kaynak_referans NOT NULL, liste_adi kapalı kaynak sözlüğünden seçilir (BMGK/641 vb.). (Bertan'ın 'Mehmet onayı şart' vetosu bu onayla kalkar.)
- VETO KALKMA ŞARTI K40-4: POST /masak/yaptirim/liste (200.000 satır = onlarca MB) [RolGerekli("SAHIP")] + [EnableRateLimiting(OranSiniri.DisaAktarim)] saatlik kota + gövde boyutu sınırı.
- VETO KALKMA ŞARTI K40-5: cari_risk_profilleri, yaptirim_kayitlari, yaptirim_eslesmeleri APPEND-ONLY — DenetimModulu.cs:21 DenetimIziDegistirilemez deseniyle KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi zincirine bağlanır.
- VETO (Bertan, kısmi) + Mehmet K40-1 + Aşkın (aynı bulgu): 'DenetimServisi.YazmaKaydet eklenir' maddesi PAKETTEN ÇIKARILIR — metot DenetimIzi.cs:120-137'de VAR, SaveChanges çağırmaz, CariModulu.cs:606'da kullanımda. Paket onu YALNIZ ÇAĞIRIR; K6 ortak kararı da düzeltilir.
- FAZ A ve FAZ B AYRI SEVK. Önkoşul [41] YALNIZ Faz B için geçerli (nakit oranı banka bacağı); Faz A 41'siz sevk edilebilir. Faz B, K40-2/4/5 kapanmadan sevk edilmez; QA Faz B test dosyasını onaysız AÇMAZ (K40-7 + Aşkın).
- Risk uçları kişisel veri: GET /masak/risk/{cariId} tekil → OkumaKaydet, yüksek riskli cari listesi → ListeOkumaKaydet; sebepler JSON'ına belge no/TCKN/uyruk-adres YAZILMAZ. DB testi: yanıtta 'belgeNo' özelliği TryGetProperty false (OrdinalIgnoreCase) — K40-3 + Aşkın.
- MasakRisk:Etkin ve YaptirimKapisiEtkin VARSAYILAN FALSE; 'kapalıyken hesaplar, durdurmaz' TevsikKapisi.cs:55-60 deseniyle birebir. Açıkken fail-closed değişmez: 'kapı açıkken eşleşmede 400 ve HİÇBİR satır yazılmaz' mutasyonu Faz B'nin şartı (K40-6).
- Katsayıların TAMAMI yapılandırmadan, koda tek sabit rakam yazılmaz (ADR-0005); 'eşikler yapılandırmadan' testi ilk test. DB testlerinde ayarlar IConfiguration'dan değil ctor'dan verilir: RiskServisi(db, MasakRiskAyarlari), YaptirimKapisi(db, MasakRiskAyarlari). Sebep listesi her zaman gösterilir.
- YaptirimKapisi ayrı sınıf, yalnız cari oluşturma/güncelleme + kimlik tespiti anına bağlı (8 çağrı noktasına DEĞİL) — korunur. Tezgâh müşterisi kapsam dışı sınırı SistemKapsami.cs'e/sınıf başlığına yazılır. UYARI: 'İsmail Yılmaz ≡ ISMAIL YILMAZ' testi TurkceMetin.cs:22 (İ≠I) nedeniyle tasarım gereği KIRMIZI olur; normalize kuralı önce karara bağlanmalı. Ön yükleme yasağı: cari seçilmeden /masak/risk/{id} 0 kez çağrılır (Uyum.razor:11-16).

### Mühür 4 eki (2026-09-16 14:56) — yaptırım listesi taraması: kalıp mühürlendi, seçim dalga 8 (BAĞLAYICI)

> Kod öncesi açık kararlar oylaması (ana belge § MÜHÜR 4). Seçim (OpenSanctions SaaS vs ücretsiz resmî listeler) dalga 8'e ertelendi; bugün yalnız tek IYaptirimTarayici arayüzü + çoklu adaptör kalıbı ve yön (ilk somutlama ücretsiz OFAC/BM/AB) mühürlüdür. Kota, liste tarihi/skor gösterimi ve operatör kararının denetim izi her seçenekte zorunludur.

- **K9** — Yaptırım taraması SEÇİMİ dalga 3 kapsamı DEĞİLDİR, dalga 8'e ertelenir (Uyum+ ücretsiz pilotta satılmıyor; F-8:52 'ÖNERİDİR, ilan değildir'). Bugün yalnız KALIP mühürlenir: tek IYaptirimTarayici arayüzü + çoklu adaptör (KurModulu.cs:590 kanıtlı kalıbın aynısı). Dalga 3'te tek satır kod doğurmaz.
- **K10** — Yaptırım yönü: ilk somutlama ÜCRETSİZ resmî listeler (OFAC/BM/AB; veri bedeli 0, F-8:24). OpenSanctions SaaS yalnız ikinci adaptör olarak, sıkı kota + Kurucu imzalı sözleşme + marj negatife dönmüyorsa. Aşkın koşulu bağlayıcı: kendi eşleme motoru yazılırsa bilinen yaptırım isimlerinden regresyon test seti olmadan 'test edilebilir' sayılmaz.
- **K11** — Yaptırım arayüz koşulu (hangi seçenek olursa olsun): her sonuçta liste adı + liste tarihi + eşleşme skoru + 'eşleşme yok' kanıtı gösterilir, operatör kararı (kabul/ret + gerekçe) denetim izine düşer, kalan kota ekranda görünür (F-8:107). Yanlış-negatif/MASAK sorumluluk metni Kurucu imzasına gider (F-8:78).
