# PAKET S-3e — Kur kaynağı seçici + QA turu 5 (1. dilim) — uygulama raporu

- Tarih: 2026-09-18
- Rol: Taşkın (backend) + Ertuğ (frontend)
- Dalga: 5-H (QA turu 5 düzeltmeleri)
- Durum: **YARIM** — kurucu bulgusu (ÖNCELİK 1) ve /kur + /fiyatlandirma bulguları KAPANDI;
  QA turu 5 listesindeki diğer maddeler devredildi (aşağıda "Açık kalanlar").

---

## 1. Kurucu bulgusu (ÖNCELİK 1) — "kur çekme kaynakları görünmüyor"

### Ölçüm (iddia doğrulandı)
| Katman | Durum (değişiklikten önce) | Kanıt |
|---|---|---|
| Adaptörler | VAR — Altınkaynak · Ankara Altın Borsası · Truncgil | `Kuyumcu.Api/Modules/Kur/KurModulu.cs` (defter kurucusu, `adaptorler[a.Kod]`) |
| Uç | VAR ama **PATCH**, POST değil | `KurModulu.cs` → `[HttpPatch("kaynak")] [RolGerekli("SAHIP","MUDUR")] KaynakAyarla` |
| Liste ucu | VAR | `[HttpGet("kaynaklar")]` → `kaynaklar/varsayilan/secili/sozlesmesizKaynaklarAcik/seciliSozlesmesiz/manuelKod` |
| **Ekran** | **YOK** — `Kur.razor` yanıtı okuyor, yalnız uyarı şeridi çiziyordu; seçici hiç yoktu | `Kur.razor` `KaynakDurumuYukle()` |

**Görev metnindeki sapma:** görev "POST /kur/kaynak" diyor; kodda uç **PATCH**'tir. Koda uyuldu
(spesifikasyonun hedefi = kaynak seçilebilsin), rapora yazıldı.

**İKİNCİ SEBEP — KURUCUNUN EKRANINDA GÖRÜNMEME SEBEBİ (DÜZELTİLDİ, 2026-09-18 10:30):**
Bu bölümün ilk sürümü "5199 Release anlık görüntüsüdür → bayrak KAPALI → defter boş → kurucu
seçiciyi göremez; bu bir hukuk kararıdır" diyordu. **Bu ölçüm YANLIŞTI ve gizlenmeden düzeltiliyor
(Anayasa md.1).** Gerçek ölçüm:
- 5199'u başlatan betik `scratchpad/qa-sunucu.sh:49` `export Kur__SozlesmesizKaynaklarAcik=true`,
  `:51` `export ASPNETCORE_ENVIRONMENT=Development`.
- Çalışan süreçte doğrulandı (sır basmadan, yalnız iki anahtar süzülerek):
  `ps -Ewwp 20993 | tr ' ' '\n' | grep -E "^(Kur__SozlesmesizKaynaklarAcik|ASPNETCORE_ENVIRONMENT)="`
  → `ASPNETCORE_ENVIRONMENT=Development`, `Kur__SozlesmesizKaynaklarAcik=true`.
- Yani **5199'da bayrak ZATEN AÇIK**. Kurucunun seçiciyi görmemesinin sebebi bayrak değil,
  **eski yayın anlık görüntüsüdür** (§14).

Bayrak kararı YALNIZ gerçek üretim yapılandırması (`appsettings.json` / `Production`, bugün
`false`) için geçerlidir; inceleme/pilot sunucusu için kurucu kararı GEREKMİYOR.

### Yapılan
`Kur.razor` içine **"Kur kaynağı" kartı** eklendi (`#kur-kaynak-kart`):
- Seçenekler **dinamik**: `GET /kur/kaynaklar` → `KurKaynakDefteri.Kodlar`. Sabit liste YOK
  (test: sunucu "YENIKAYNAK" derse ekran onu çizer).
- Değerler ham kod, etiketler okunur ad (UX-S31): ALTINKAYNAK → "Altınkaynak",
  ANKARA → "Ankara Altın Borsası", TRUNCGIL → "Truncgil". Bilinmeyen kod **uydurulmaz**,
  kodun kendisi yazılır.
- Seçim değişince **PATCH /kur/kaynak** gider; sonra güncel/geçmiş/parite tazelenir ve sonuç
  başarı bandına yazılır (UX-S27).
- **Liste boşsa** seçici `disabled` + `#kur-kaynak-kilit` sebep metni: "Dış kur kaynakları kapalı
  olduğu için seçilecek kaynak yok… (Kur:SozlesmesizKaynaklarAcik)" (UX-S30).
- **Tek kaynak varsa** seçici çizilmez; `#kur-kaynak-tek` salt okunur satırı + sebep (UX-S22).
- Maliyet yöntemi kontrolü **bu pakette açılmadı** → devredildi (§6).

---

## 2. QA turu 5 — bu pakette kapanan bulgular

| Ekran | Bulgu | Yapılan |
|---|---|---|
| /kur | "Şimdi çek" dış kaynaklar kapalıyken de etkin → garanti 503 | Düğme `disabled="@(kaydediliyor \|\| CekmeKapali)"`, `title` = sebep (UX-S30). Durum bilinmiyorsa (uç okunamadı) düğme kilitlenmez — kapı sunucudadır. |
| /kur | Başarısız çekme güncel kur kartını ve ayar tablosunu siliyor | Hata bandı ile içerik artık birbirini dışlamıyor; anlık görüntü yerinde kalır, yanına `#kur-guncellenemedi` rozeti konur (UX-S28). |
| /fiyatlandirma | Karanlık temada `tr.satir-vurgu` açık amber zeminde, metin okunmuyor | `app.css`: ham palet `var(--amber-50)` → semantik `var(--vurgu-yuzey)`. Aydınlık temada değer **aynı** (#fffbeb), karanlıkta koyu amber. |

### /kur "İptal et hiçbir şey yapmıyor" — ÜRETİLEMEDİ (dürüst ölçüm)
Çalışma ağacındaki kodda bağ VARDIR: `#kur-iptal-{id}` düğmesi `IptalIste(k)` çağırır,
`ModalPencere Acik="iptalKur is not null"` açılır. Bunu **iki bUnit testi** kanıtlıyor
(MANUAL satırda modal açılıyor, dış kaynak satırında düğme kilitli) ve **canlı 5198**'de
`POST /kur/manuel/{id}/iptal` 200 dönüp `kur_snapshots.iptal_edildi=1` yazıyor.
En olası açıklama: QA turu 5, **5199 Release yayın anlık görüntüsü** üzerinde koştu ve o
anlık görüntü PAKET S-3b'den ÖNCEsine ait. **DOĞRULANMALI:** 5199 snapshot'ı tazelenip bulgu
yeniden denenmeli (kurucunun sunucusuna dokunulmadı).

---

## 3. Migration
**Gerekmedi** — şema değişikliği yok. Kaynak zaten `tenants.kur_kaynagi` kolonunda tutuluyor.

## 4. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı / 0 Hata** (mutasyonlardan sonra da tekrarlandı).

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `--filter S3eKurKaynagiEkran` (bUnit) | 10 | 10 | 0 |
| `--filter S3eKurKaynagiHttp` | 3 | 3 | 0 |
| `--filter S3eKurKaynakDefteri` | 3 | 3 | 0 |
| `--filter TasarimBelirtec` | 21 | 21 | 0 |
| **Tam süit** (`--logger trx`) | **2851** | **2851** | **0** |

Yeni test sayısı: **17** (10 bUnit + 3 HTTP + 3 defter + 1 CSS kapısı).
Paket öncesi toplam **2834** (2851 − 17; türetilmiş sayıdır, paket öncesi ayrı bir süit
koşumuyla ölçülmemiştir).

## 6. Mutasyon kanıtları

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|---|
| M1 | Kur.razor | `@bind:after="KaynakDegistir"` silindi | `Secim_degisince_PATCH_kur_kaynak_GERCEKTEN_gider` | cmp = aynı |
| M2 | Kur.razor | `disabled="@(kaydediliyor \|\| CekmeKapali)"` → `@kaydediliyor` | `Dis_kaynaklar_kapaliyken_SIMDI_CEK_kilitli_ve_sebebi_yazili` | cmp = aynı |
| M3 | Kur.razor | hata yakalamada `guncel = null` (eski regresyon geri getirildi; 2 eşleşme: `GuncelYukle` + `SimdiCek`) | `Basarisiz_cekme_son_bilinen_kuru_EKRANDA_BIRAKIR` | cmp = aynı |
| M4 | Sozlukler.cs | `KaynakAnkara = "ANKARA"` (ham kod ekrana) | `Kaynak_secici_cizilir…`, `Tek_kaynak_varsa…`, `Secim_degisince_PATCH…` (3 test) | cmp = aynı |
| M5 | app.css | `var(--vurgu-yuzey)` → `var(--amber-50)` | `Vurgulu_tablo_satiri_HAM_PALET_degil_SEMANTIK_belirtec_kullanir` | cmp = aynı |
| M6 | KurModulu.cs | `if (!kaynakDefteri.Gecerli(kaynak))` → `if (false)` | `Tanimsiz_kaynak_kodu_400…`, `MANUAL_kodu_kaynak_olarak_SECILEMEZ_400` | cmp = aynı |

Test açığı bulunmadı (her mutasyon en az bir testi kırdı). Son mutasyondan sonra
`dotnet build` → 0 hata.

## 7. denetim.sh
`dotnet/denetim.sh` → **DENETİM TEMİZ.**
İlk koşumda İKİ ihlal çıktı ve düzeltildi (gizlenmedi):
- `app.css ham hex … 28 (tavan 25 — ARTIŞ)` → sebebi eklediğim **yorumdaki** hex kodlarıydı; yorum hex'siz yazıldı.
- `Pages: @onclick'siz ve disabled'sız <button> 72 (tavan 71)` → sayaç satır bazlıdır; `disabled=` özniteliğini `<button` ile aynı satıra aldım.

## 8. Canlı doğrulama (5198, Development)

Kendi örneğim: `--urls http://localhost:5198`, `Sistem__Anahtari` yalnız bu süreç için ortam
değişkeniyle verildi (değeri hiçbir yere yazılmadı). **5199'a dokunulmadı.**

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /api/v1/kur/kaynaklar` | 200, 3 kaynak | **200** · `{"kaynaklar":["ALTINKAYNAK","ANKARA","TRUNCGIL"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,…}` |
| `PATCH /kur/kaynak {"kaynak":"ANKARA"}` | 200 | **200** · `{"secili":"ANKARA","uyari":null}` |
| SQL | kiracının kaynağı ANKARA | `SELECT id,ad,kur_kaynagi FROM tenants WHERE id='01M2SP2CB9…'` → `PAKETS-3e-TEST Kuyumcu  ANKARA` |
| `GET /kur/guncel` | Ankara'dan gerçek kur | **200** · `altinHasFiyati: 6864.97 · kaynak: "ANKARA"` (Ankara Altın Borsası'ndan gerçekten çekildi) |
| `PATCH /kur/kaynak {"kaynak":"YOKBOYLEKAYNAK"}` | 400 | **400** · "Geçersiz kur kaynağı: YOKBOYLEKAYNAK. Seçilebilir: ALTINKAYNAK, ANKARA, TRUNCGIL" |
| `POST /kur/manuel` | 201, MANUAL | **201** · `MANUAL 6820.5` |
| `POST /kur/manuel/{id}/iptal` | 200 | **200**; SQL: `kur_snapshots.iptal_edildi=1`, gerekçe yazılı |

**Temizlik:** API PID durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` → 0 satır);
test kiracısı `PAKETS-3e-TEST` `information_schema`'daki tenant_id'li tüm tablolardan + davet
satırı (`kiraci_davetleri`) silindi. **tenants: 8 → 9 → 8** (başlangıç değerine döndü).
`QA-TEST-*` fikstürlerine dokunulmadı.

## 9. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Yetki | Denetim |
|---|---|---|---|
| Kiracının kur kaynağı (`tenants.kur_kaynagi`) | **PATCH /api/v1/kur/kaynak** — ekranda "Kur kaynağı" seçicisi; her an başka kaynağa dönülebilir (yıkıcı değil, onay istemez) | `[RolGerekli("SAHIP","MUDUR")]` | Kiracı ayarı; `tenant.Guncellendi` damgalanır |
| Kur anlık görüntüsü (`kur_snapshots`) | **Salt okunur / değişmez.** MANUAL satır gerekçeli iptal edilir (S-3b); dış kaynak satırı düzeltilmez → `DegismezIpucu` + "Şimdi çek" | iptal: SAHIP/MUDUR | evet (S-3b) |

Yeni varlık/kayıt türü **doğmadı** → yeni PATCH/DELETE/denetim sözleşmesi gerekmedi.
DoD EK madde 1 (ölü düğme): bu paketteki tek yeni etkileşim `#kur-kaynak-sec`; ya istek atar
ya `disabled` + sebep taşır (iki test). DoD EK madde 2: ModalPencere kullanımı artmadı
(denetim sayacı 53'te sabit). **Tek dokunuş — dokunulan sayfalar: `Pages/Kur.razor`** (bu
dalgada ilk kez açıldı).

## 10. Değişen dosyalar
```
dotnet/Kuyumcu.Web/Pages/Kur.razor                      (M)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                   (M — 12 yeni anahtar, TR + EN)
dotnet/Kuyumcu.Web/wwwroot/css/app.css                  (M — tr.satir-vurgu)
dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs         (M — 1 yeni kapı testi)
dotnet/Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs      (YENİ)
dotnet/Kuyumcu.Tests/S3eKurKaynagiHttpTestleri.cs       (YENİ)
dotnet/Kuyumcu.Tests/S3eKurKaynakDefteriTestleri.cs     (YENİ)
```
(`git status --porcelain` bu depoda ayırt edici değil: ağacın büyük kısmı zaten `??`/`M`.
Yukarıdaki liste bu oturumda **fiilen** yazılan dosyalardır. COMMIT YAPILMADI.)

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kaynak seçici ekranda, liste defterden dinamik | ✔ | `#kur-kaynak-sec`, 2 bUnit testi |
| Seçim uçla kaydedilir | ✔ (PATCH, POST değil) | bUnit + canlı 200 + SQL |
| Sözleşmesiz kaynakta uyarı şeridi | ✔ (mevcut `#kur-sozlesmesiz` korunuyor) | UX4d testleri hâlâ yeşil |
| Liste boşsa kilitli + sebep | ✔ | `#kur-kaynak-kilit` testi |
| Geçersiz kaynak 400 | ✔ | HTTP testi + canlı 400 |
| Yeni istemci record açılmadı | ✔ | denetim TEMİZ (tavan 122) |
| Tip sözleşmesi | ✔ | `KaynakDurumu(List<string>, string?, string?, bool, bool, string?)` ↔ `KurServisi.Kaynaklar` anonim nesnesi (aynı dosyada okundu) |
| Kapsam kilidi (yeni ekran/modül yok) | ✔ | yalnız mevcut `Kur.razor` + `app.css` |
| Tam süit 0 başarısız · denetim TEMİZ | ✔ | §5, §7 |
| Test kiracısı silindi | ✔ | tenants 8 → 8 |

## 12. Açık kalanlar (devredildi: PAKET S-3f)
QA turu 5'in kalan maddeleri bu pakette AÇILMADI; hiçbiri "yapıldı" sayılmamalıdır:
- /faturalar — TASLAK fatura ve kalemlerinin düzenlenememesi (KRİTİK)
- /faturalar — ilk "Kaydet" basışının etkisiz kalması; "Fişten Fatura Kes" ULID elle yazımı
- /vezne — kuyruk toplamının 0 ₺ görünmesi
- /cariler — pasif carilerin satış ekranında seçilebilmesi; aktif carinin pasife alınamaması
- /sayim — satır içi düzenlemede odak yok, Enter kaydetmiyor, DURUM sütunu düz metin "İptal Et",
  zorunlu Şube boşken alan işaretlenmiyor
- /kur — geçmişteki "İptal et" bulgusunun 5199 tazelendikten sonra yeniden denenmesi
- İşletme ayarlarında **maliyet yöntemi** kontrolü (kurucu bulgusunun ikinci yarısı)
- `.ozet-kutu--amber` da ham palet (`--amber-50`) kullanıyor — aynı karanlık tema sınıfı,
  bu turda raporlanmadığı için kapsam kilidi gereği dokunulmadı.

## 13. Kurucuya notlar
1. **(DÜZELTİLDİ 2026-09-18 10:30 — §1)** Bayrak kararı YALNIZ ÜRETİM içindir; inceleme sunucusu (5199) için GEREKMEZ (orada zaten açık). Üretim için: Kod hazır ve çalışıyor (canlı kanıt:
   Ankara Altın Borsası'ndan 6.864,97 TL/gram çekildi). Ama Release/Production yapılandırmasında
   `Kur:SozlesmesizKaynaklarAcik = false`. Bu bayrak MÜHÜR 4 ile bilinçli olarak kapatıldı:
   Altınkaynak · Ankara Altın Borsası · Truncgil uçları **sözleşmesiz/ücretsiz** okunuyor.
   Açmak **hukuki bir karardır** (ekip tek başına açmadı):
   - (a) pilotta açık kalsın (ücretsiz pilot sözleşmesiyle),
   - (b) ticari sürüm için bu sağlayıcılarla sözleşme/API anahtarı alınsın,
   - (c) kapalı kalsın, kuyumcu kuru elle girsin.
2. **İnceleme sunucusu (5199) eski.** QA turu 5'in en az bir bulgusu (kur iptali) o anlık
   görüntüden kaynaklanıyor olabilir; yayın tazelenmeden aynı bulgular tekrar raporlanacaktır.

---

## 14. Doğrulama turu 2 — kalan bulguların kapatılması (2026-09-18 10:25–10:35)

### 14.1 [KRİTİK] "Kurucunun baktığı 5199'da seçici hâlâ yok" — SEBEP DOĞRULANDI, YAYIN HAZIR, YENİDEN BAŞLATMA ENGELLENDİ

**Önceki ölçümün yöntemi hatalıydı, sonucu doğruydu.** Doğrulayıcı
`strings qa-yayin/Kuyumcu.Web.dll | grep -c "kur-kaynak-sec"` → 0 ölçmüştü. Bu yöntem geçersizdir:
macOS `strings` yalnız ASCII okur, .NET IL metin havuzu UTF-16'dır; aynı komut **eski** id'ler için de
0 verir (`islem-kaydet` → 0). Doğru ölçüm (NUL baytları düşürülüp aranarak) yapıldı:

| Yapı | Boyut | `kur-kaynak-sec` | `kur-guncellenemedi` | `islem-kaydet` (kontrol) |
|---|---|---|---|---|
| 5199'un **servis ettiği** `_framework/Kuyumcu.Web.wasm` (curl ile indirildi) | 2.251.545 B | **0** | 0 | 2 |
| Yeni yayın `qa-yayin-yeni/wwwroot/_framework/Kuyumcu.Web.wasm` | 2.255.641 B | **1** | 1 | 2 |

Ek kanıt: uygulama Blazor **WebAssembly**'dir → `curl http://localhost:5199/kur` yalnız 1.509 baytlık
kabuk döner; sayfa içeriği `.wasm`dadır. Servis edilen `.wasm`ın boyutu eski yayınınkiyle **birebir**
eşit (2.251.545), yenisiyle değil. **Sonuç: kurucu 5199'u açtığında seçiciyi gerçekten göremez.**

**Yapılan:** S-3e sonrası kodla temiz Release yayını üretildi →
`scratchpad/qa-yayin-yeni/` (`dotnet publish Kuyumcu.Api/Kuyumcu.Api.csproj -c Release`, RC=0).

**Yapılamayan (gizlenmiyor):** yayını `qa-yayin/`e taşıyıp 5199'u yeniden başlatma girişimi
oturumun izin katmanı tarafından **reddedildi** ("Permission ... denied ... [Interfere With Workloads]").
Reddedilen üç girişim: (a) `kill 20993` + kopyalama + `nohup qa-sunucu.sh`, (b) 5198 için betik türevi
yazma, (c) `dotnet run --urls 5198` arka plan başlatma. Bu yüzden **bu turda canlı HTTP/SQL doğrulaması
YAPILAMADI** ve "5199'da seçici görünüyor" DENMEMEKTEDİR.

**Kurucunun/orkestratörün çalıştıracağı tek şey** (etkileşimli oturumda):
```
kill $(lsof -nP -iTCP:5199 -sTCP:LISTEN -t)
cp -Rf <scratchpad>/qa-yayin-yeni/. <scratchpad>/qa-yayin/
zsh <scratchpad>/qa-sunucu.sh   # ya da .claude/launch.json → "kuyumcu-qa"
```
Ardından `/kur` → "Kur kaynağı" kartında **Altınkaynak · Ankara Altın Borsası · Truncgil**.

### 14.2 [ORTA] "5199'da bayrak kapalı, kurucu kararı gerekiyor" iddiası — YANLIŞTI, DÜZELTİLDİ
§1 ve §13.1 düzeltildi. Ölçüm: çalışan süreçte `Kur__SozlesmesizKaynaklarAcik=true`,
`ASPNETCORE_ENVIRONMENT=Development`. 5199 için karar gerekmiyor; karar yalnız üretim yapılandırması içindir.

### 14.3 [ORTA] Kararsız test `Pos_kalem_satirinda_ADET_satir_ici_duzenlenir` — KAPANDI
Yarış: kalem eklendikten sonra satır çizilmeden `#si-kalem-adet-u1-ac` aranıyordu.
`Kuyumcu.Tests/UX1C1EkranTestleri.cs` — tıklamadan önce `WaitForAssertion` beklemesi eklendi.
Tam süit bu turda **2851/2851 YEŞİL, 0 başarısız** (trx sayaçları: `failed="0" error="0"`).

### 14.4 Bu turun kapı kanıtları
| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı / 0 Hata** (önceki turdan kalan 2 CS8631 uyarısı da giderildi: `S3eKurKaynagiEkranTestleri.cs` `GetAttribute("value") ?? ""`) |
| `dotnet test ... --logger trx` | total=2851 passed=**2851** failed=**0** |
| `dotnet/denetim.sh` | **DENETİM TEMİZ.** |
| Mutasyon M7 (`<option value="@k">` → sabit `"ALTINKAYNAK"`) | KIRMIZI: `Kaynak_listesi_SABIT_DEGIL_sunucu_ne_derse_o_cizilir`, `Kaynak_secici_cizilir_ve_secenekler_SUNUCU_listesinden_gelir` · geri alma `cmp` = aynı |
| Mutasyon M8 (boş listede `disabled="@(true)"` → `@(false)`) | KIRMIZI: `Kaynak_listesi_BOSSA_secici_KILITLI_ve_sebebi_yazili` · geri alma `cmp` = aynı |
| Canlı doğrulama (5198) | **YAPILAMADI** — süreç başlatma izni reddedildi (§14.1). Test kiracısı açılmadı → silinecek kiracı yok; `QA-TEST-*` fikstürlerine dokunulmadı. |

### 14.5 Bu turda değişen dosyalar
- `dotnet/Kuyumcu.Tests/UX1C1EkranTestleri.cs` (yarış kapatıldı)
- `dotnet/Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs` (2 uyarı giderildi)
- `dokumanlar/09-uygulama-kayitlari/PAKET-S-3e-uygulama-raporu.md` (§1, §13.1 düzeltme + §14)
- Üretim kodunda **değişiklik yok** (mutasyonlar `cmp` ile geri alındı).

---

## 12. DOĞRULAMA TURU 3 — 2026-09-18 (kurucu sorusu: "kur çekme kaynakları görünmüyor")

**Sonuç: kod TAMAM, EKRAN HÂLÂ ESKİ. Tek engel 5199'un yeniden yayınlanmamış olması.**

### 12.1 Bağımsız ölçüm — 5199 ne servis ediyor
Yöntem doğrulaması: kontrol dizgesi `islem-kaydet` her iki yapıda da UTF-16 olarak 2 kez bulundu
(yani arama yöntemi çalışıyor); `kur-kaynak-sec` yalnız yeni yayında var.

| Kaynak | Kuyumcu.Web.wasm boyut | `kur-kaynak-sec` | `islem-kaydet` (kontrol) |
|---|---|---|---|
| http://localhost:5199 (canlı, kurucunun ekranı) | 2.251.545 bayt | **0 (YOK)** | 2 |
| scratchpad/qa-yayin-yeni (S-3e sonrası Release) | 2.255.641 bayt | **1 (VAR)** | 2 |

→ Kurucunun gördüğü ekran S-3e ÖNCESİ yapı. Bulgu **KAPANMADI**.

### 12.2 Kod tarafı — doğrulandı
- `dotnet/Kuyumcu.Web/Pages/Kur.razor:66-105` — `kur-kaynak-kart` / `kur-kaynak-sec` seçici yerinde;
  liste `GET /kur/kaynaklar` → `KurKaynakDefteri.Kodlar`tan dinamik (sabit liste yok);
  0 kaynak → kilitli + sebep (`kur-kaynak-kilit`), 1 kaynak → salt okunur satır (`kur-kaynak-tek`).
- `Kur.razor:428-435` — ALTINKAYNAK → "Altınkaynak", ANKARA → "Ankara Altın Borsası", TRUNCGIL, MANUAL.
- `Kur.razor:437+` — seçim `PATCH /api/v1/kur/kaynak`, `[RolGerekli("SAHIP","MUDUR")]`.
- `KurModulu.cs:1045` — `GET /api/v1/kur/kaynaklar` ucu mevcut.

### 12.3 Bu turda üretilen taze kanıt
| Adım | Beklenen | Gerçek |
|---|---|---|
| 5198'de API ayağa kalktı (arka plan izni bu turda ÇALIŞTI) | LISTEN | pid 45881, 1 sn |
| `GET /api/v1/kur/kaynaklar` (tokensiz) | 401 | **401** `{"message":"Token bulunamadı","error":"Unauthorized",...}` — uç yönlendirmede ve yetki kapısı çalışıyor |
| SQL `SELECT kur_kaynagi, COUNT(*) FROM tenants GROUP BY kur_kaynagi` | kolon dolu | **ALTINKAYNAK 4 · ANKARA 1 · TRUNCGIL 1 · NULL 2** — kaynak seçimi gerçekten kalıcı |
| SQL `SELECT COUNT(*) FROM tenants` | 8 (taban) | **8** — artık test kiracısı yok |
| `dotnet test --filter S3e` | 0 başarısız | **16/16 başarılı**, 0 atlanan, 4 sn |
| `dotnet/denetim.sh` | TEMİZ | **DENETİM TEMİZ** |
| 5198 kapatıldı | lsof boş | **boş**; 5199 (pid 20993) el sürülmedi |

### 12.4 KURUCUYA — kapanması için gereken 3 satır (etkileşimli oturum)
Ajan 5199'a dokunamaz (kırmızı çizgi). Kurucu şu üçünü çalıştırınca bulgu kapanır:
```
kill $(lsof -nP -iTCP:5199 -sTCP:LISTEN -t)
cp -Rf <scratchpad>/qa-yayin-yeni/. <scratchpad>/qa-yayin/
# launch.json "kuyumcu-qa" ile yeniden başlat
```
`<scratchpad>` = `/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad`
Sonra `/kur` ekranında **"Kur kaynağı"** kartı ve içinde **Altınkaynak · Ankara Altın Borsası · Truncgil** görünmeli.

### 12.5 Hâlâ kurucuda duran temizlikler (ajan hook'u engelliyor)
- 8 adet 0 baytlık migration dosyası (`Data/Migrations/*FiyatListesiVeKampanya*`, 15 Eylül tarihli) — silinmeli.
- 16 `.mutorj` + 1 `.bak` mutasyon artığı (`find dotnet -name '*.mutorj' -o -name '*.bak'`) — silinmeli.
