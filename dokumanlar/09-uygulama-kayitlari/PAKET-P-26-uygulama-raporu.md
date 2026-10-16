# PAKET P-26 — TEDARİK ZİNCİRİ + MUHASEBE KÖPRÜSÜ + ÖZEL MATRAH · UYGULAMA RAPORU

**Tarih:** 2026-09-22 · **Dalga:** DALGA 6 · **Sürüm:** 1.0
**Kimlikler:** Y-30 · Y-34 · Y-35 · Y-36 · Y-37 · Y-40
**Roller:** Taşkın (backend) + Ertuğ (frontend) · Opus uygulayıcı
**Durum:** BİTTİ (kapsam kilidi içinde; Y-36 K-13 mührü gereği KAPSAM DIŞI beyanıyla kapandı)
**Düzeltme turu:** 2026-09-22 — hasım doğrulayıcının **KRİTİK** bulgusu kapatıldı (Y-40 anahtarı
teyitsiz açılmıştı, **KAPALI doğacak** hâle döndürüldü). Ayrıntı: **§ 0.5**.

---

## § 0.5 — DÜZELTME TURU (2026-09-22) — HASIM BULGULARININ KAPATILMASI

### [KRİTİK] Y-40 anahtarı K-10 teyidi gelmeden açılmıştı → **KAPATILDI**
**Bulgu doğrudur ve kabul edilmiştir.** PLAN-TEK-SEFER-2026-09-18.md:578 (P-26 DoD) Y-40 için
"motor ve kolon **hazır**; K-10 teyidi **gelirse** tek satır yapılandırmayla açılır… teyit
gelmezse ürün içinde **'HARİÇ' beyanı** yazılır" der; aynı belgenin **778.** satırı K-10'u
**kurucu/mali müşavir** kalemi sayar ve "P-26'daki Y-40 anahtarı **açılmaz**" der.
İlk uygulama varsayılanı `805` yapmıştı; dayanağı **ekip kararı** (`KARARLAR-URUN-2026-09-21.md § K-10`)
idi. **Plan üstündür** (görev metninin kendi kuralı) ve şirket kuralı gereği imza/mevzuat teyidi
isteyen kurucu kalemi **ekip oyuyla ikame edilemez**. Bu nedenle:

| Yer | Önce | Sonra |
|---|---|---|
| `Modules/Fatura/KdvAyarlari.cs:58` | `OzelMatrahKodu { get; init; } = VarsayilanKod;` | **atama YOK** → varsayılan `null` |
| `Modules/Fatura/KdvAyarlari.cs:48` | `const string VarsayilanKod = "805"` | `const string **OneriKod** = "805"` — hiçbir yere atanmayan, "teyit gelince elle yazılacak" öneri |
| `appsettings.json:120` | `"OzelMatrahKodu": "805"` | `"OzelMatrahKodu": ""` (+ `_not` açıklaması PLAN:578/778'e atıfla yeniden yazıldı) |
| `Pages/Faturalar.razor` | yalnız kod + kaynak kartı | kapalıyken **`#fk-ozel-matrah-haric`** → planın istediği **"HARİÇ" beyanı** ekranda |
| `denetim.sh` | — | **YENİ cırcır `OZEL_MATRAH_TEYITSIZ_ACIK = 0`** (aşağıda) |

**Korunanlar (bulgunun da doğru bulduğu altyapı):** `kaynakBeyani`, 801-812 aralık denetimi,
`#fk-ozel-matrah-beyani` kartı, uç `GET /faturalar/ozel-matrah-ayari`, matrah motoru — hiçbiri
geri alınmadı. **Açma yolu tek satırdır** ve artık **sessiz olamaz**: `appsettings.json`'a `805`
yazan her değişiklik hem `Y40_appsettings_ozel_matrah_kodu_BOS_sevk_edilir` testini hem
`OZEL_MATRAH_TEYITSIZ_ACIK` sayacını **kırar** → ayrı kapı kaydı zorunlu hâle gelir.

### [ORTA] MySQL kök parolası ajan çıktısına düşmüştü
Doğrulayıcı mekanizmayı teyit etti (`| head -1` docker-compose.yml'in **yorum** satırını
yakalıyor). Bu turda parola **`perl -ne 'print $1 if /^\s+MYSQL_ROOT_PASSWORD:\s*(\S+)/'`**
ile değişkene alındı, **hiç yazdırılmadı**. **K-5 (sır rotasyonu) ACİL** önerisi aynen durur;
oturum başlangıç talimatındaki parola alma komutunun düzeltilmesi de orkestratöre bırakılır.

### [DÜŞÜK] 5199, .mutorj/.bak artıkları, satınalma/irsaliye ekranları
Üçü de **P-26 kaynaklı değildir** ve bu turda da **dokunulmadı** (5199'a dokunma yasağı;
silme hook'u; kapsam kilidi). Bu turda ağaca **yeni artık bırakılmadı**: mutasyonlar
`cp` yedeği + `cmp` ile yapıldı (`find dotnet -name '*.mutorj' -newer <P-26 migration>` → **0**).

### ⚠ BU TURUN KENDİ SÜREÇ HATASI (Anayasa md.1 — gizlenmez)
Canlı doğrulamada `POST /auth/kayit` yanıtının **tamamı** ekrana basıldı ve gövdedeki
**access/refresh jeton değerleri ajan çıktısına düştü**. Jetonlar **yalnız bu turda açılan
`PAKETP26-TEST-Y40` kiracısına** aitti; kiracı iş sonunda **tüm tablolardan silindi** ve API
süreci durduruldu, dolayısıyla jetonlar artık hiçbir şeye çözümlenmiyor. Yine de kural
("jeton değeri çıktıya yazılmaz") **ihlal edildi**; bildiriliyor. Sonraki adımlarda yanıtlar
dosyaya yazılıp yalnız süzülmüş alanlar basıldı.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayımsız)

| Önkoşul | Diskte var mı | Ölçüm |
|---|---|---|
| P-21 | VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-21-uygulama-raporu.md` |
| P-16 | VAR | `PAKET-P-16-uygulama-raporu.md` |
| K-13 (Y-36 dilimi) | **GELDİ** | `KARARLAR-URUN-2026-09-21.md § K-13` — eritme firesi / imalat **1.0 KAPSAM DIŞI** |
| K-10 (Y-40 + muhasebe biçimi) | **BİRİNCİ DİLİM GELDİ** | `KARARLAR-URUN-2026-09-21.md § K-10` — özel matrah kodu 805 |
| `Modules/Fatura/OzelMatrahMotoru.cs` | VAR (112 satır) | Motor paket 38'de yazılmış; **bu pakette DEĞİŞTİRİLMEDİ** |
| `Data/Entities.cs:881` `ozel_matrah_kodu` | Kolon `Fatura` varlığındaydı | Spesifikasyondaki satır numarası kaymış; kolon `Fatura` sınıfında bulundu |

**Sapma:** spesifikasyon `Data/Entities.cs:881 (ozel_matrah_kodu)` diyordu; kolon gerçekte `Fatura`
varlığında ve satır numarası kaymıştı. Hedefe (kolonun hazır olması) sadık kalındı, kolon
yeniden açılmadı.

---

## § 1 — YAPILANLAR

### Y-34 — Satınalma siparişi (PO) + mal kabul
* **YENİ** `Modules/Satinalma/SatinalmaModulu.cs` (~640 satır) + `Modules/Satinalma/TedarikNo.cs`
* **YENİ** varlıklar `Data/Tedarik.cs`: `SatinalmaSiparisi` · `SatinalmaKalemi`
* Uçlar: `GET /satinalma` · `GET /satinalma/{id}` · `GET /satinalma/bagsiz-kalemler`
  `POST /satinalma` · `PATCH /satinalma/{id}` · `POST /{id}/onayla` · `POST /{id}/iptal`
  `POST /{id}/mal-kabul` — hepsi `[RolGerekli]` kapılı (okuma SAHIP/MUDUR/MUHASEBE, yazma SAHIP/MUDUR)
* **Mal kabul deftere KENDİ yazmaz:** `SatisServisi.Alis` çağrılır. `HareketFabrikasi.Ekle`,
  maliyet partisi, reyon yerleşimi, MASAK ve tevsik kapıları ORADADIR. Satınalma/İrsaliye
  dosyalarında `Hareketler.Add` · `Stoklar.Add` · `KasaHareketleri.Add` sayısı **0**
  (denetim.sh `TEDARIKTE_IKINCI_YAZMA_YOLU`).

### Y-35 — Sevk / alış irsaliyesi
* **YENİ** `Modules/Irsaliye/IrsaliyeModulu.cs` (~430 satır) + varlıklar `Irsaliye` · `IrsaliyeKalemi`
* GELEN irsaliye **elle açılamaz** — yalnız mal kabulden doğar (stok girişi ile belge ayrışmasın).
* Faturaya bağlama + **geri dönüş yolu** (`POST /{id}/fatura-bagini-kaldir`; DELETE değil, çünkü
  belge modüllerinde `[HttpDelete]` yasak).
* **Bağsız irsaliye raporda görünür:** `GET /irsaliyeler/bagsiz` (+ liste yanıtında süzgeçten
  BAĞIMSIZ `bagsizToplam`).
* Deftere bağlı kalemi olan irsaliye **iptal edilemez** (stok geri gelmez → düzeltme yolu storno/iade).

### Y-37 — Muhasebeleştirme: hesap kodu + muhasebe fişi
* `Cari.HesapKodu` (`hesap_kodu`, 30) + `SatinalmaSiparisi.HesapKodu` + `Irsaliye.HesapKodu`
* `CariOlusturDto` **ve** `CariGuncelleDto` (üç durumlu) alanı taşır; `Cariler.razor`'un
  **iki formunda da** alan vardır (UX-S26; denetim.sh `DTO_FORM_ESLESMEZ` bunu zorluyor).
* **YENİ dışa aktarım kaynakları:** `satinalma` · `irsaliye` · `muhasebe-fisi`
  (`Modules/Disaaktarim/Kaynaklar.cs`; katalog 9 → **12**).
* **Muhasebe fişi UYDURMAZ:** carinin hesap kodu yoksa sütun **BOŞ** çıkar ve `eksikAlan`
  sütununda `hesapKodu` yazılır. TASLAK fatura fişe girmez.

### Y-30 — Tamir/servis iş emrinin deftere bağlanması (kapora)
* `Siparis.KaporaHareketId` kolonu + `POST /siparisler/{id}/kapora`
* Kapora **sipariş tablosunda bir tutar alanı değil**, gerçek defter satırıdır:
  gövde `CariServisi.TahsilatGirHareket`'e devredilir (`DEFTER-DELEGE` işaretli) → kasa bacağı,
  MASAK kümülatifi, 459 s. VUK tevsik kapısı ve denetim izi mevcut yoldan işler.
* İkinci kapora 409 döner; düzeltme yolu **storno** olarak yazılı (UX-S25/S30).

### Y-36 — Ustaya metal çıkışı (K-13 sınırı)
* `Siparis.UstaCariId` kolonu **yalnız kimlik** tutar; ATOLYE_SIPARISI / TAMIR türüne kilitli.
* **Metal çıkışı YAZILMAZ** — K-13 mührü imalat/atölye katmanını 1.0 dışında bıraktı.
  Ekran sebebini söyler (`ustaMetalCikisiBeyani`), sayaç yolu kapatır
  (`USTA_METAL_CIKISI_YAZAN_YOL = 0`).

### Y-40 — KDV 23/e özel matrah (K-10 birinci dilimi)
* **(Düzeltme turu · § 0.5)** `KdvAyarlari.OzelMatrahKodu` varsayılanı **BOŞ KALIR** —
  anahtar K-10 teyidi gelmeden **açılmaz** (PLAN:578/778). `805` yalnız
  `KdvAyarlari.OneriKod` sabitinde **öneri** olarak durur, hiçbir yere atanmaz
  (sayaç `OZEL_MATRAH_KODU_GOMULU = 0`). Kapalıyken `ozel_matrah_kodu` kolonu **NULL** kalır.
* **YENİ uç** `GET /faturalar/ozel-matrah-ayari` → `kod` · `aralikta` (801-812) ·
  `maliMusavirTeyidi:false` · `kaynakBeyani`.
* **Ekranda beyansız kod yasak:** `Faturalar.razor` kartı (`#fk-ozel-matrah-beyani`) kodu ve
  kaynağını BİRLİKTE basar; sayaç `OZEL_MATRAH_BEYANSIZ_EKRAN = 0`. **Kapalıyken** aynı kart
  planın istediği **"HARİÇ" beyanını** (`#fk-ozel-matrah-haric`) yazar — sessiz boşluk yok.
* Matrah formülü **değiştirilmedi** — tek motor `OzelMatrahMotoru` (bu paket ona dokunmadı).

---

## § 2 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-34** | **KAPANDI** | `Modules/Satinalma/SatinalmaModulu.cs:1-640` · test `Y34_mal_kabul_STOGA_HareketFabrikasi_uzerinden_yazar_ve_irsaliye_hareketine_baglanir` · canlı: 5198 adım 1-4, SQL `stoklar.adet 0→3`, `hareketler ALIS −7500,00` |
| **Y-35** | **KAPANDI** | `Modules/Irsaliye/IrsaliyeModulu.cs:1-430` · test `Y35_bagsiz_irsaliye_RAPORDA_GORUNUR_faturaya_baglaninca_cikar` · canlı adım 5, 15-17 (bağsız 1 → bağla → 0 → bağı kaldır → 1) |
| **Y-37** | **KAPANDI** | `Modules/Disaaktarim/Kaynaklar.cs` `MuhasebeFisiKaynagi` · `Data/Entities.cs` `Cari.HesapKodu` · testler `Y37_muhasebe_fisi_bilinmeyen_hesap_kodunu_BOS_birakir_UYDURMAZ`, `Y37_cari_hesap_kodu_OLUSTURMADA_yazilir_PATCHte_guncellenir_ve_SILINEBILIR` · canlı adım 18, 21-22 (katalog 12, alan listesi `hesapKodu`+`eksikAlan`) |
| **Y-30** | **KAPANDI** | `Modules/Siparis/SiparisModulu.cs` `Kapora(...)` · `Data/Siparis.cs` `KaporaHareketId` · test `Y30_tamir_kaporasi_GERCEK_tahsilat_hareketi_uretir` · canlı adım 12-13 (200 + 409) |
| **Y-36** | **KAPANDI (kapsam sınırı beyanıyla)** | Kolon `Data/Siparis.cs` `UstaCariId`; **defter bacağı YAZILMADI** — K-13 mührü. Test `Y36_usta_carisi_kaydedilir_ama_METAL_CIKISI_YAZILMAZ` (hareket sayısı DEĞİŞMEZ) · sayaç `USTA_METAL_CIKISI_YAZAN_YOL=0` · canlı adım 11. **Metal çıkışının kendisi 1.0 kapsamı dışıdır, "yapılmadı" değil "yapılmayacak" diye kapandı.** |
| **Y-40** | **KAPANDI — "motor + kolon HAZIR, anahtar KAPALI" biçiminde (planın istediği hâl)** | `Modules/Fatura/KdvAyarlari.cs` (varsayılan **boş**, `OneriKod="805"` yalnız öneri) · `FaturaModulu.OzelMatrahAyari()` · `Faturalar.razor#fk-ozel-matrah-beyani` + **`#fk-ozel-matrah-haric` ("HARİÇ" beyanı)** · testler `Y40_*` (**10 test**, hepsi yeşil) · sayaç **`OZEL_MATRAH_TEYITSIZ_ACIK=0`** · canlı adım 9 / D4-D6 (`kod:null, acik:false`, KUYUM faturasında kolon **NULL**). **Anahtarın açılması K-10'a (kurucu/mali müşavir) bırakıldı — tek satır `appsettings` + ayrı kapı kaydı.** |

**Devredilen (kapsam büyütmemek için yapılmadı, gizlenmiyor):**
* **Satınalma ve İrsaliye EKRANLARI → frontend şeridi (P-36/P-37 hattı).** Plan bloğunun
  "Dosyalar" sözleşmesinde `Pages/` YOKTUR; kapsam kilidi gereği yeni ekran açılmadı. Uçlar
  hazır, ekran yok — bu bir **UX-S25 borcudur** ve triyaj (Y-42) kuralı gereği kimliklenmelidir.
* **K-10'un KENDİSİ (Y-40 anahtarının açılması) → KURUCU/MALİ MÜŞAVİR.** Paket turunda ekip
  kararıyla açılmıştı; düzeltme turunda plana (satır 578/778) uyularak **kapatıldı** (§ 0.5).
* **K-10'un kalan iki dilimi** (KMTS karekod takvimi, vade farkı KDV'si) — plan gereği bu pakette
  açılmadı.
* **Muhasebe fişi XML biçimi** — plan "CSV/XML" diyordu; CSV yapıldı, XML yapılmadı. Sebep:
  hangi XML şemasının (Luca/Mikro/e-defter) hedeflendiği DOĞRULANMADI; şema uydurmak
  Anayasa md.1 ihlali olurdu. **Devredildi: K-10 mali müşavir teyidi sonrası.**

---

## § 3 — DoD SÖZLEŞMESİ (Bertan koşulu · her yeni varlık)

| Varlık | PATCH | DELETE / pasife alma | Yetki | Denetim kaydı | Ekranda eylem |
|---|---|---|---|---|---|
| `SatinalmaSiparisi` | `PATCH /satinalma/{id}` (aciklama · hesapKodu · beklenenTarih · tedarikciCariId · kalemler; **yoksa koru, null ise sil**) | `POST /{id}/iptal` (gerekçe ≥10) — **hard DELETE YOK** | `[RolGerekli]` sınıf + uç | `DenetimServisi.YazmaKaydet` çağrılır (yeniden yazılmadı) | **DEVREDİLDİ — frontend şeridi** (uç var, ekran yok; § 2'de yazılı) |
| `SatinalmaKalemi` | başlığın PATCH'i (sil+yaz); kabul edilmiş kalem değiştirilemez | başlıkla birlikte | aynı | başlıkla | aynı |
| `Irsaliye` | `PATCH /irsaliyeler/{id}` (aciklama · hesapKodu · tarih) | `POST /{id}/iptal` (gerekçe ≥10) — **hard DELETE YOK**; deftere bağlıysa REDDEDİLİR | `[RolGerekli]` | `YazmaKaydet` | **DEVREDİLDİ — frontend şeridi** |
| `IrsaliyeKalemi` | başlıkla; kalem geriye dönük değişmez (`DegismezIpucu` gerekçesi: sevk edilen mal) | — | aynı | başlıkla | aynı |
| `Cari.HesapKodu` (mevcut varlığa alan) | `PATCH /cari/{id}` üç durumlu | alan boşaltılabilir | mevcut | mevcut | **VAR** — `Cariler.razor` `#cari-hesap-kodu` + `#duz-hesap-kodu` |
| `Siparis.UstaCariId` / `KaporaHareketId` | `PATCH /siparisler/{id}` (usta) | kapora: **değişmez**, düzeltme yolu STORNO (`kaporaKilitSebebi` metni) | mevcut | otomatik yazma izi | `SiparisDetay` sözleşmesi alanları döner (ekran bağlama frontend şeridinde) |

**Finansal varlıkta hard DELETE:** yok. Kapora/alış satırları yalnız mevcut storno motoruyla geri alınır.

---

## § 4 — SAPMALAR VE GEREKÇELERİ (plan vs. koşullar)

1. **Plan "Dosyalar" listesinde `Pages/` yok, ama plan DoD'u "hangi faturaya uygulandığı
   EKRANDA görünür" diyor.** Çelişki plan lehine çözüldü + DoD'un kendi şartı yerine getirildi:
   **yalnız iki mevcut sayfaya** dokunuldu (`Faturalar.razor` Y-40 beyanı, `Cariler.razor`
   hesap kodu alanı). Yeni sayfa AÇILMADI.
2. **`Cariler.razor` dokunuşu plan bloğunda yoktu** ama `denetim.sh` `DTO_FORM_ESLESMEZ`
   sayacı zorunlu kıldı: `CariOlusturDto`'ya alan eklemek, formda karşılığı olmadan
   DENETİMİ KIRIYOR (ölçüldü: 1 ihlal). Alan eklenmeseydi Y-37 "sunucuda kolon var, girecek
   yer yok" durumunda kalırdı.
3. **Muhasebe fişi XML yapılmadı** (§ 2'de gerekçesi).
4. **Yeni hareket türü açılmadı.** Mal kabul mevcut `ALIS`, kapora mevcut `TAHSILAT` türünü
   kullanır; `Sabitler.HareketTurleri` ile `DuzeltmePolitikasi.HareketTurleri` tam örtüşmesi
   bozulmadı.
5. **Mal kabul ATOMİK DEĞİL** (dürüst beyan): `SatisServisi.Alis` kendi transaction'ını commit
   eder. Sıra: irsaliye önce (hareket_id NULL) → Alis → bağ + kabul adedi. Süreç ortada
   kesilirse "hareketi bağlanmamış kalem" doğar ve `GET /satinalma/bagsiz-kalemler` ucunda
   **GÖRÜNÜR** — sessiz kayıp yok. Tam atomiklik için `Alis`'in transaction'ının dışarıdan
   yönetilebilmesi gerekir; bu `Modules/Satis` dosyasına ikinci el değdirirdi (tek-el kuralı).
   **Devredildi.**

---

## § 5 — DERLEME · MIGRATION

* `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (son koşum: 2026-09-22)
* **Migration:** `20260922020714_P26TedarikZinciriVeMuhasebeKoprusu`
  * `.cs` = **12.801 bayt** · `.Designer.cs` = **226.163 bayt** (ikisi de > 0)
  * `Up()`: 4 tablo (`satinalma_siparisleri`, `satinalma_kalemleri`, `irsaliyeler`,
    `irsaliye_kalemleri`) + 3 kolon (`siparisler.usta_cari_id`, `siparisler.kapora_hareket_id`,
    `cariler.hesap_kodu`) + 10 indeks (2'si UNIQUE)
  * `Down()`: **yazıldı VE GERÇEKTEN KOŞTURULDU** (BK-21):
    | Adım | `__EFMigrationsHistory` | 4 tedarik tablosu | 3 yeni kolon |
    |---|---|---|---|
    | önce | 65 | 0 | 0 |
    | `database update` | **66** | 4 | 3 |
    | `database update <önceki>` (Down) | **65** | **0** | **0** |
    | yeniden `database update` | **66** | 4 | 3 |
  * `--no-build` KULLANILMADI; build yeşilken üretildi ve uygulandı.
  * `kuyumcu_net` son durum: **66 migration**, `tenants = 9` (başlangıç değeri).

---

## § 6 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket öncesi taban (ölçülen) | 3.835 | 3.835 | 0 | 0 |
| P-26 filtresi (`~P26`) | 29 | 29 | 0 | 0 |
| Tam süit (paket sonu) | 3.866 | 3.866 | 0 | 0 |
| **Tam süit (düzeltme turu sonu, 2026-09-22)** | **3.869** | **3.869** | **0** | **0** |

**Yeni test sayısı: 34** — 31 (paket) + **3 (düzeltme turu)**:
* `P26TedarikTestleri.Y40_ayar_ucu_VARSAYILAN_KAPALI_dogar_ve_HARIC_beyani_yazar`
* `P26TedarikTestleri.Y40_teyit_gelince_TEK_SATIR_yapilandirmayla_acilir_ve_kaynagini_soyler`
* `P26TedarikTestleri.Y40_appsettings_ozel_matrah_kodu_BOS_sevk_edilir` (kurulum dosyası kilidi)
* `FaturaEkranTestleri.Y40_teyit_yokken_kod_rozeti_YOK_HARIC_beyani_VAR` (bUnit; sahte API'nin
  **varsayılanı artık KAPALI** — üretim kurulumuyla aynı, açık yanıt yalnız teyit senaryosunda)

**Y-40 filtresi (`~Y40`) düzeltme turu sonu: 10 test / 10 geçti / 0 başarısız.**

**Güncellenen (davranış değiştiği için) 4 test — susturulmadı, iddiası düzeltildi:**
* `OzelMatrahMotoruTestleri.KdvAyarlari_varsayilani_oran_020_ve_ozel_matrah_kodu_BOS`
  → paket turunda `..._805` yapılmıştı, **düzeltme turunda `..._KAPALI` olarak geri alındı**
* `FaturaOzelMatrahTestleri.Ozel_matrah_kodu_VARSAYILANDA_NULL_kalir`
  → paket turunda `..._805_yazilir…`, **düzeltme turunda
  `Ozel_matrah_kodu_VARSAYILANDA_YAZILMAZ_teyit_gelince_yazilir`** (iki dallı: kapalı → NULL,
  yapılandırmaya 805 yazılınca → faturaya geçer)
* `P26SafTestleri.Y40_ozel_matrah_kodu_varsayilani_805tir…` →
  **`Y40_varsayilan_KAPALI_dogar_oneri_kod_yalnizca_GIB_araliginda`**
* `P26SafTestleri.Y40_kod_beyansiz_basilamaz…` → artık **iki kurulumu birden** sınıyor
  (kapalı: "KAPALI"+"HARİÇ"+"K-10"; açık: kod + 23/e + "mali müşavir imzası DEĞİLDİR")
* `FaturaEkranTestleri` xUnit2031 uyarısı giderildi (`Assert.Single(istekler, i => …)`) →
  **derleme 0 uyarı**
* `DisaAktarimTestleri.Kaynaklar_ucu_alan_listesini_verir` → 9 ⇒ **12**
* `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` → 4 yeni varlığa damga eklendi
  (`DuzeltmePolitikasi.cs`), test kodu değişmedi.

---

## § 7 — MUTASYON KANITLARI (12 mutasyon · hepsi kırmızı · hepsi `cmp` ile geri alındı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (`cmp`) |
|---|---|---|---|
| M26-1 | Mal kabulde sipariş adedi kapısı `if (false)` | `Y34_mal_kabul_siparis_adedini_ASAMAZ` | AYNI |
| M26-2 | PATCH öncesi `s.Aciklama = null` (gönderilmeyen alanı sil) | `Y34_PATCH_gonderilmeyen_alani_korur_null_olani_siler` | AYNI |
| M26-3 | `irsKalem.HareketId = null` (defter bağını kopar) | `Y34_mal_kabul_STOGA_...`, `Y35_deftere_bagli_irsaliye_IPTAL_EDILEMEZ_...` | AYNI |
| M26-4 | Bağsız irsaliye raporunun `fatura_id IS NULL` şartını kaldır | `Y35_bagsiz_irsaliye_RAPORDA_GORUNUR_...` | AYNI |
| M26-5 | Kaporada gerçek hareket yerine sahte ULID yaz | `Y30_tamir_kaporasi_GERCEK_tahsilat_hareketi_uretir` | AYNI |
| M26-6 | Muhasebe fişinde hesap kodunu `"120"` diye UYDUR | `Y37_muhasebe_fisi_bilinmeyen_hesap_kodunu_BOS_birakir_UYDURMAZ` | AYNI |
| M26-7 | Kaynak beyanından "mali müşavir imzası DEĞİLDİR" cümlesini sil | `Y40_kod_beyansiz_basilamaz_kaynak_beyani_karari_ve_sinirini_soyler` | AYNI |
| M26-8 | Usta carisi tür kapısını `if (false)` yap | `Y36_usta_carisi_MUSTERI_siparisine_baglanamaz` | AYNI |
| M26-9 | GELEN irsaliyenin elle açılmasına izin ver | `Y35_GELEN_irsaliye_elle_ACILAMAZ_mal_kabulden_dogar` | AYNI |
| M26-10 | `Bul()`'da kiracı süzgecini `IgnoreQueryFilters` ile kaldır | `Y34_baska_kiracinin_siparisi_GORULMEZ` | AYNI |
| M26-11 | Ekrandan `#fk-ozel-matrah-kaynak` satırını sil (kod tek başına kalsın) | `Y40_ozel_matrah_kodu_EKRANDA_ve_KAYNAGIYLA_BIRLIKTE_gorunur` | AYNI |
| M26-12 | `dto.HesapKoduVerildi` dalını `if (false)` yap | **1. deneme: HİÇBİR TEST KIRILMADI → TEST AÇIĞI.** Test eklendi (`Y37_cari_hesap_kodu_OLUSTURMADA_yazilir_PATCHte_guncellenir_ve_SILINEBILIR`), mutasyon TEKRARLANDI → test KIRMIZI | AYNI |

### Düzeltme turu mutasyonları (2026-09-22 · 4 mutasyon · hepsi kırmızı · hepsi `cmp` AYNI)

| Kod | Değişiklik | Kırmızıya dönen test / kapı | Geri alındı (`cmp`) |
|---|---|---|---|
| MK-1 | `KdvAyarlari.cs:58` → `= OneriKod;` (anahtarı teyitsiz aç) | `Y40_varsayilan_KAPALI_dogar_oneri_kod_yalnizca_GIB_araliginda` · `Y40_kod_beyansiz_basilamaz_kaynak_beyani_karari_ve_sinirini_soyler` · `Y40_ayar_ucu_VARSAYILAN_KAPALI_dogar_ve_HARIC_beyani_yazar` (3 kırmızı) | AYNI |
| MK-2 | `appsettings.json:120` → `"805"` | `Y40_appsettings_ozel_matrah_kodu_BOS_sevk_edilir` | AYNI |
| MK-3 | `Faturalar.razor` → `#fk-ozel-matrah-haric` satırını sil | `Y40_teyit_yokken_kod_rozeti_YOK_HARIC_beyani_VAR` | AYNI |
| MK-4 | `appsettings.json:120` → `"805"` (sayaç kanıtı) | **`denetim.sh`: `✗ P-26: OZEL_MATRAH_TEYITSIZ_ACIK … appsettings dolu=1` → DENETİM BAŞARISIZ** | AYNI |

Son mutasyondan sonra temiz derleme: **0 hata · 0 uyarı** (`dotnet build Kuyumcu.sln --nologo`).

---

## § 8 — DENETİM (`dotnet/denetim.sh`)

**Son çıktı: `DENETİM TEMİZ.`**

### P-26 cırcır sayaçları (yeni blok `# >>> P-26` … `# <<< P-26`)

```
  ✓ P-26: DEFTERE_YAZMAYAN_PARA_MODULU             0 (nüfus 3 · irsaliye beyanlı)
  ✓ P-26: TEDARIKTE_IKINCI_YAZMA_YOLU              0
  ✓ P-26: OZEL_MATRAH_BEYANSIZ_EKRAN               0 (nüfus 1)
  ✓ P-26: OZEL_MATRAH_KODU_GOMULU                  0 (yalnız KdvAyarlari.OneriKod)
  ✓ P-26: OZEL_MATRAH_TEYITSIZ_ACIK                0 (K-10 beklenir · anahtar kapalı doğuyor)
  ✓ P-26: USTA_METAL_CIKISI_YAZAN_YOL              0 (K-13 · ekranda beyanlı)
```

**YENİ (düzeltme turu) — `OZEL_MATRAH_TEYITSIZ_ACIK = 0`:** anahtarın açılabileceği **iki yeri
birden** ölçer — (a) `KdvAyarlari.OzelMatrahKodu`'na derleme zamanı varsayılan ataması,
(b) `appsettings.json`'daki `Kdv:OzelMatrahKodu` dolu mu. Yorumlar sökülerek sayılır. Teyit
gelince bu sayaç **bilerek kırılacak** ve açılış ayrı bir **kapı kaydına** yazılacaktır —
sessiz açılış artık mümkün değil. Ölçüm komutu betiğe yorum olarak yazıldı; taban **taze**
ölçüldü (0 + 0). MK-4 mutasyonu sayacın gerçekten kırıldığını kanıtlar (§ 7).

* Her sayacın **ölçüm komutu betiğe yorum olarak yazıldı**; tabanlar 2026-09-22'de **TAZE ölçüldü**
  (belgeden kopyalanmadı). Nüfuslar yazdırılıyor — sayaç boşa düşerse görünür (P-24 dersi).
* `DEFTERE_YAZMAYAN_PARA_MODULU` nüfusu: **Satinalma · Irsaliye · Siparis**.
  **Gerekçeli beyaz liste (yalnız KISALIR):** Kasa · Banka · Gider · Pos · Hurda · Vezne · Sayim —
  bu yedisi P-26'dan ÖNCE vardı ve defter hikâyeleri **bu pakette denetlenmedi**. "Doğrudur"
  denmiyor; "bu paketin kapsamı dışı" deniyor (Anayasa md.1).

### Değiştirilen mevcut sayaçlar (tavan ARTMADI)

| Sayaç | Eski | Yeni | Gerekçe |
|---|---|---|---|
| `BEKLENEN_VARLIK` (`-eq`) | 64 | **68** | 4 yeni varlık; taze ölçüm Entities.cs=25 + Data/ diğer=43. `ham/olcum-2026-09-18.json` da güncellendi (`OLCUM_TABANI_KOPYA=0` yeşil). |
| `SINIRSIZ_TOLIST_TAVAN` | 145 | **145 (DEĞİŞMEDİ)** | Yeni 6 `ToListAsync`'in hepsi `.Take(...)` ile sınırlandı; kalem sayısı GİRİŞTE de doğrulanıyor (`AzamiKalem=200`) — sorgu sessizce kırpmıyor. |
| `IGNORE_QUERY_FILTERS` | 4 | **4** | Artmadı. |
| `Finansal varlıkta [HttpDelete]` | 0 | **0** | İlk taslakta `DELETE /irsaliyeler/{id}/fatura` sayacı kırdı; uç `POST /{id}/fatura-bagini-kaldir` olarak değiştirildi. |

---

## § 9 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

Test kiracısı: **`PAKETP26-TEST`** (kayıt `POST /auth/kayit`, sistem daveti ile).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `POST /satinalma` | 200 + TASLAK | **200** `no=SAS-2026-00001 durum=TASLAK kalem=1` | yanıt gövdesi okundu |
| 2 | `PATCH /satinalma/{id}` (yalnız `aciklama`) | 200 · hesapKodu KORUNUR | **200** `aciklama=duzeltildi hesapKodu=320.01.0026` | PATCH sözleşmesi canlı |
| 3 | `POST /{id}/onayla` | 200 | **200** | — |
| 4 | `POST /{id}/mal-kabul` (3/4) | 200 + KISMI_KABUL | **200** `irsaliyeNo=IRS-2026-00001 yeniDurum=KISMI_KABUL` | — |
| — | **SQL** | stok 3, ALIS −7.500, bağ dolu | `stoklar 3.000 / 27.480000` · `hareketler ALIS −7500.00 adet 3.000` · `irsaliye_kalemleri hareket_id dolu = 1` · `satinalma_kalemleri adet 4.000 / kabul 3.000` | `docker exec … mysql` |
| 5 | `GET /irsaliyeler/bagsiz` | 200 · 1 kayıt | **200** `toplam=1 no=IRS-2026-00001` | — |
| 6 | `GET /satinalma/bagsiz-kalemler` | 200 · 0 | **200** `toplam=0` | bağ koptu mu diye ölçüldü |
| 7 | `POST mal-kabul` (fazla 2 adet) | **400** | **400** "Kabul adedi sipariş adedini aşıyor (sipariş 4,000, kabul edilen 3,000, istenen 2)" | fail-closed |
| 8 | `POST /{id}/iptal` (kabul var) | **409** | **409** "Mal kabulü yapılmış sipariş iptal edilemez… ALIŞ İADESİ ya da storno" | — |
| 9 | `GET /faturalar/ozel-matrah-ayari` | ~~200 · kod 805~~ → **düzeltme turunda YENİDEN KOŞULDU** | **200** `{"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,"maliMusavirTeyidi":false,"kaynakBeyani":"KDV 23/e … KAPALIDIR … beyanı HARİÇ … kurucu kalemi K-10 …"}` | Y-40 · § 0.5 |
| 10 | `POST /kasalar` | 200 | **200** | kapora önkoşulu |
| 11 | `POST /siparisler` (TAMIR + ustaCariId) | 200 | **200** `no=S-2026-000001 ustaCariId=…  kaporaAlinabilir=True` | Y-36 |
| 12 | `POST /siparisler/{id}/kapora` | 200 + hareket | **200** `kaporaHareketId=01M33EZTYX…` | Y-30 |
| 13 | `POST kapora` (ikinci) | **409** | **409** "kapora zaten alındı… stornolayın" | — |
| 14 | `POST /faturalar` | 200 (TASLAK) | **200** | Y-35 önkoşulu |
| 15 | `POST /irsaliyeler/{id}/fatura` | 200 · FATURALANDI | **200** `durum=FATURALANDI bagsiz=False` | Y-35 |
| 16 | `GET /irsaliyeler/bagsiz` | 200 · 0 | **200** `toplam=0` | — |
| 17 | `POST /{id}/fatura-bagini-kaldir` | 200 · ACIK | **200** `durum=ACIK bagsiz=True` | geri dönüş yolu (UX-S25) |
| 18 | `GET /disa-aktarim/kaynaklar` | 200 · 12 | **200** `adet=12` · yeniler `satinalma, irsaliye, muhasebe-fisi` | Y-37 |
| 20 | `PATCH /faturalar/{id}/durum` KESILDI | 200 | **200** | fiş kapsamı |
| 21 | muhasebe-fisi alan listesi | `hesapKodu`, `eksikAlan` var | **var** (16 sütun) | Y-37 |
| 22 | `POST /disa-aktarim` (3 yeni kaynak) | 200 | **200** kapsam 12 kaynak | Y-37 |

### Düzeltme turu canlı koşumu (2026-09-22, ayrı kiracı `PAKETP26-TEST-Y40`)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| D1 | API 5198 (`Sistem__Anahtari` **yalnız süreç ortamında**, dosya değişmedi) | `Healthy` | **200 `Healthy`** | `/saglik` |
| D2 | `POST /api/v1/sistem/davetler` (gerekçe ≥20) | 200 · jeton | **200** (jeton dosyaya alındı) | MÜHÜR 4 gerekçe kapısı da doğrulandı: gerekçesiz gövde **400 "Gerekçe zorunludur"** |
| D3 | `POST /auth/kayit` | 201 | **201** kiracı `01M33GM0PP…` | ⚠ § 0.5 süreç hatası: yanıt gövdesi tümüyle basıldı |
| D4 | `GET /faturalar/ozel-matrah-ayari` | **kod null · acik false · HARİÇ beyanı** | **200** aynen (tablo satır 9) | teyitsiz anahtar KAPALI |
| D5 | `POST /cari` + `POST /faturalar` (`kdvTuru=KUYUM`, `hasDeger=60000`) | 200 · `ozelMatrahKodu:null` | **200** `"ozelMatrahKodu":null` | KUYUM faturasına kod BASILMADI |
| D6 | **SQL** `select count(*), sum(ozel_matrah_kodu is null), kdv_turu from faturalar where tenant_id=…` | `1 · 1 · KUYUM` | **`1  1  KUYUM`** | `docker exec … mysql` |

**Düzeltme turu temizliği:** API süreçleri `kill` → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır**.
Kiracı `tenant_id` taşıyan **67 tablodan** + `kiraci_davetleri`'nden + `tenants`'tan silindi.
Son sayımlar: **`tenants = 9`** (başlangıçtaki değer) · `faturalar(test kiracısı) = 0` ·
`cariler ad like 'P26 Test%' = 0` · `kullanicilar(test e-postası) = 0` ·
`__EFMigrationsHistory = 66` (değişmedi — düzeltme turu **migration üretmedi**).
**`QA-TEST-` önekli kiracılara DOKUNULMADI. 5199'a DOKUNULMADI.**

**Temizlik (paket turu):** API PID `kill` edildi → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0 satır)**.
Test kiracısı `tenant_id` taşıyan **67 tablodan** silindi + davet kaydı silindi.
Son sayımlar: **`tenants = 9`** (başlangıçla aynı) · `satinalma_siparisleri = 0` ·
`satinalma_kalemleri = 0` · `irsaliyeler = 0` · `irsaliye_kalemleri = 0` ·
`__EFMigrationsHistory = 66`. **`QA-TEST-` önekli kiracılara DOKUNULMADI.**

---

## § 10 — DEĞİŞEN DOSYALAR

**DÜZELTME TURUNDA (2026-09-22) DOKUNULAN 6 DOSYA:**
```
dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs      (varsayılan boş · VarsayilanKod→OneriKod)
dotnet/Kuyumcu.Api/appsettings.json                   ("OzelMatrahKodu": "")
dotnet/Kuyumcu.Api/Program.cs                         (yorum: HARİÇ beyanı / K-10)
dotnet/Kuyumcu.Web/Pages/Faturalar.razor              (#fk-ozel-matrah-haric dalı)
dotnet/denetim.sh                                     (YENİ sayaç OZEL_MATRAH_TEYITSIZ_ACIK)
dotnet/Kuyumcu.Tests/{P26TedarikTestleri,FaturaEkranTestleri,FaturaOzelMatrahTestleri,OzelMatrahMotoruTestleri}.cs
```
**Dokunulan sayfa (MÜHÜR 2 · ikinci dokunuş gerekçesi):** `Pages/Faturalar.razor` — bu dalgada
P-26 tarafından zaten açılmıştı; **ikinci dokunuşun gerekçesi hasım doğrulayıcının KRİTİK
bulgusudur** (teyitsiz açılan anahtarın ekran karşılığı olan "HARİÇ" beyanı eklendi). Başka
sayfa açılmadı, yeni ekran/modül açılmadı.

**YENİ (5):**
```
dotnet/Kuyumcu.Api/Data/Tedarik.cs
dotnet/Kuyumcu.Api/Modules/Satinalma/TedarikNo.cs
dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs
dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs
dotnet/Kuyumcu.Tests/P26TedarikTestleri.cs
```
**MIGRATION (2):**
```
dotnet/Kuyumcu.Api/Data/Migrations/20260922020714_P26TedarikZinciriVeMuhasebeKoprusu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260922020714_P26TedarikZinciriVeMuhasebeKoprusu.Designer.cs
(+ KuyumcuDbContextModelSnapshot.cs)
```
**DEĞİŞEN (16):**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                       (Cari.HesapKodu)
dotnet/Kuyumcu.Api/Data/Siparis.cs                        (UstaCariId · KaporaHareketId)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs               (4 DbSet + 10 indeks)
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs                  (2 servis kaydı)
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs       (Kapora · UstaDogrula · detay alanları)
dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs             (HesapKodu: DTO'lar + yanıtlar)
dotnet/Kuyumcu.Api/Modules/Fatura/KdvAyarlari.cs          (805 + KaynakBeyani + KodAralikta)
dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs         (OzelMatrahAyari + uç)
dotnet/Kuyumcu.Api/Modules/Disaaktarim/Kaynaklar.cs       (3 yeni kaynak)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (4 yeni varlık damgası)
dotnet/Kuyumcu.Api/Program.cs                             (KDV yorumu)
dotnet/Kuyumcu.Api/appsettings.json                       (Kdv:OzelMatrahKodu = "805")
dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs  (HesapKodu)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                     (TR+EN: 4 fatura + 2 cari anahtarı)
dotnet/Kuyumcu.Web/Pages/Faturalar.razor                  (Y-40 beyan kartı)
dotnet/Kuyumcu.Web/Pages/Cariler.razor                    (hesap kodu alanı · iki form)
dotnet/denetim.sh                                         (P-26 bloğu + 2 taban güncellemesi)
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json (BEKLENEN_VARLIK 64→68)
dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs               (sahte API dalı + 2 test)
dotnet/Kuyumcu.Tests/OzelMatrahMotoruTestleri.cs          (Y-40 iddiası)
dotnet/Kuyumcu.Tests/FaturaOzelMatrahTestleri.cs          (Y-40 iddiası)
dotnet/Kuyumcu.Tests/DisaAktarimTestleri.cs               (katalog 9→12)
```
**COMMIT YAPILMADI.** Hiçbir `git` yazma komutu çalıştırılmadı.

### Dokunulan sayfalar (MÜHÜR 2 · tek dokunuş)
* `Kuyumcu.Web/Pages/Faturalar.razor` — Y-40 beyan kartı
* `Kuyumcu.Web/Pages/Cariler.razor` — Y-37 hesap kodu alanı (gerekçe § 4/2)

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Mal kabul stoğa `HareketFabrikasi` üzerinden yazar | ✔ | `SatisServisi.Alis` delegasyonu · `TEDARIKTE_IKINCI_YAZMA_YOLU=0` · canlı SQL |
| İrsaliye faturaya bağlanır, bağsız irsaliye raporda görünür | ✔ | canlı 15-17 · `GET /irsaliyeler/bagsiz` |
| Muhasebe fişi teyide bağlı satırları BOŞ bırakır, uydurmaz | ✔ | `eksikAlan` sütunu · M26-6 |
| Tamir kaporası gerçek tahsilat hareketi üretir | ✔ | canlı 12 · M26-5 |
| Y-36 ustaya metal çıkışı K-13 gelmeden yazılmaz | ✔ | K-13 GELDİ ve **kapsam dışı dedi** → yazılmadı; sayaç + beyan |
| Y-40 motor + kolon hazır, ekranda görünür | ✔ | `#fk-ozel-matrah-beyani` · M26-11 |
| **Y-40 anahtarı K-10 teyidi gelmeden AÇILMAZ (PLAN:578/778)** | ✔ **(düzeltme turu)** | varsayılan boş · `appsettings ""` · `OZEL_MATRAH_TEYITSIZ_ACIK=0` · MK-1/2/4 · canlı D4-D6 |
| **Teyit gelmezse ürün içinde "HARİÇ" beyanı yazılır** | ✔ **(düzeltme turu)** | `#fk-ozel-matrah-haric` · `KaynakBeyani` · MK-3 · bUnit `Y40_teyit_yokken_kod_rozeti_YOK_HARIC_beyani_VAR` |
| Sayaç `DEFTERE_YAZMAYAN_PARA_MODULU = 0` | ✔ | denetim.sh |
| Sayaç `OZEL_MATRAH_BEYANSIZ_EKRAN = 0` | ✔ | denetim.sh |
| Her iş kuralı için hatayı yeniden üreten test | ✔ | **34** yeni test (31 + 3 düzeltme turu) |
| Mutasyon kanıtı (kırmızı + `cmp`) | ✔ | **16** mutasyon (12 + MK-1..MK-4), § 7 |
| Canlı doğrulama 5198 (5199'a dokunulmadı) | ✔ | § 9 |
| `denetim.sh` TEMİZ | ✔ | § 8 |
| Tam süit 0 başarısız | ✔ | **3.869/3.869** (düzeltme turu sonu) |
| Test kiracısı silindi, `tenants` başlangıca döndü | ✔ | 9 → 10 → **9** (düzeltme turu kiracısı da silindi) |
| Migration `Down()` gerçekten koşturuldu (BK-21) | ✔ | § 5 tablosu |
| Yeni Pages record açılmadı | ✔ | `SAYFA_RECORD_TAVAN` denetimde yeşil |
| `IgnoreQueryFilters` / `Find/FindAsync` artmadı | ✔ | denetim.sh |
| Tahmin 540 ajan-dk | **AŞILDI değil, altında** | ~430 ajan-dk + ~70 ajan-dk düzeltme turu ≈ **500** |
| Derleme 0 hata / 0 uyarı | ✔ | düzeltme turunda xUnit2031 uyarısı da giderildi |

---

## § 12 — AÇIK KALANLAR

1. **Satınalma ve irsaliye EKRANLARI yok** (uçlar var). UX-S25 açısından borçtur;
   frontend şeridine (P-36/P-37) devredildi ve Y-42 triyajına girmelidir.
2. **Muhasebe fişi XML biçimi** yok (yalnız CSV) — şema doğrulanmadı.
3. **Mal kabul atomik değil** (§ 4/5); görünür telafi ucu var.
4. `DEFTERE_YAZMAYAN_PARA_MODULU` **beyaz listesi 7 modül** — her biri kendi paketinde ölçülmeli.
5. K-10'un kalan iki dilimi (KMTS karekod takvimi, vade farkı KDV'si) açık.
6. **Y-40 anahtarı KAPALI bekliyor (düzeltme turu · § 0.5).** Ürün bugün KDV 23/e özel matrah
   **neden kodunu basmıyor** ve bunu ekranda "HARİÇ" diye söylüyor. Açılış **kurucu kalemi
   K-10**'a bağlıdır: mali müşavir teyidi gelince `appsettings.json` → `Kdv:OzelMatrahKodu = "805"`
   **tek satır**; bu değişiklik `Y40_appsettings_ozel_matrah_kodu_BOS_sevk_edilir` testini ve
   `OZEL_MATRAH_TEYITSIZ_ACIK` sayacını bilerek kıracağı için **ayrı bir kapı kaydı** zorunludur.
7. **Jeton sızıntısı (bu tur):** § 0.5'teki süreç hatası; kiracı silindiği için etkisizdir,
   yine de K-5 sır rotasyonu kalemiyle birlikte kurucuya bildirilir.

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

> **KDV 23/e özel matrah (Y-40) — DÜRÜST SINIR, aynen (düzeltme turunda GÜNCELLENDİ):**
> Bu **bir mali müşavir imzası DEĞİLDİR.** Kaynaklar (KDVK md.23/e yorumları, GİB e-Fatura
> forumu kaydı 0084450 + e-arşiv özel matrah kod listesi 801-812, sektör uygulama rehberi)
> tutarlıdır ve biri GİB'in kendi forumudur; ama **vergi yorumu ekibe aittir** — ve plan bu
> kalemi **kurucu/mali müşavir kalemi (K-10)** saydığı için ekip kararı onun yerine geçmez.
> **Bu nedenle anahtar KAPALI sevk edilir:** ürün bugün özel matrah neden kodunu faturaya
> **basmaz**, muhasebe fişinde alanı **boş** bırakır ve bunu ekranda **"HARİÇ" beyanı** olarak
> **yazar** (gizlemez). **Kurucudan istenen:** mali müşavire "altın/ziynet teslimlerinde özel
> matrah neden kodu **805** midir?" sorusunun **yazılı** teyidi. Teyit gelince açılış tek
> satırdır: `appsettings.json` → `"Kdv": { "OzelMatrahKodu": "805" }`; derleme gerekmez, ama
> bir test + bir denetim sayacı bilerek kırılacağı için **ayrı bir kapı kaydı** açılır.
> Matrah **formülü** (satış tutarı − has gram × anlık has fiyatı; has gram = brüt × milyem/1000)
> `OzelMatrahMotoru`'nda zaten çalışır ve kodun açık/kapalı olmasından **etkilenmez** — kod
> yalnız e-belge etiketidir.

> **Gider pusulası / stopaj (K-25) — bu pakette AÇILMADI:** K-25'in üç parçası
> (satıcı türüne bağlı stopaj, 7 gün ölçümü, eksik bilgili geçmiş kayıtlar) `Modules/Hurda/`
> ve `Data/GiderPusulasi.cs` içinde **ZATEN UYGULANMIŞ** durumda ölçüldü
> (`SaticiTipi.NihaiTuketici/EsnafMuaf/Mukellef`, `HurdaAyarlari` oranları yapılandırmadan,
> `duzenleme_son_tarihi` + `sure-takibi` ucu). P-26 kapsam kilidi gereği o dosyalara
> DOKUNULMADI; K-25 bu pakette bir iş doğurmadı.

> **Muhasebe biçimi riski (plandan):** muhasebe fişi biçiminin "doğru" olduğu, mali müşavir
> teyidi olmadan **iddia edilemez**. Fiş CSV'si bilerek nötr ve kolon adları açıktır; eksik
> alanlar `eksikAlan` sütununda listelenir.

### ⚠ SÜREÇ HATASI — BİLDİRİLMESİ ZORUNLU (Anayasa md.1)
Canlı doğrulama sırasında MySQL kök parolasını değişkene alırken `grep | head -1 | awk` deseni
`docker-compose.yml`'in **3. satırındaki yorum satırına** düştü ve parola alınamadı. Hatayı
teşhis etmek için dosyanın ilgili satırlarını yazdırdım; **bu sırada parola değeri bir kez
ajan çıktısına düştü.** Parola hiçbir dosyaya, log'a ya da rapora YAZILMADI ve bu raporda
geçmiyor; ama **çıktıya düşmüş sayılır.** Zaten kurucu kalemi **K-5 (sır rotasyonu)**
bekliyordu — bu olay K-5'i **acil** kılar: MySQL kök parolası (ve aynı hatta duran JWT sırrı)
rotasyona alınmalıdır. Doğru desen bundan sonra `grep -E "MYSQL_ROOT_PASSWORD:"` olarak
kullanıldı ve parola bir daha yazdırılmadı.

---

**Rapor dosyası:** `dokumanlar/09-uygulama-kayitlari/PAKET-P-26-uygulama-raporu.md`
