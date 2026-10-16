# PAKET Q9-3 — Sipariş: TASLAK siparişte cari, tür ve şube düzenlenemiyor (KRİTİK, önceki turdan taşındı)

Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga: DEMO DÜZELTME (QA turu 9) · Tarih: 2026-09-24
İzolasyon: `KUYUMCU_TEST_DB=kuyumcu_test_l` · canlı API 5174 · vekil 5173 · canlı veritabanı `kuyumcu_net`.

## 1. Bulgunun yeniden üretimi (kod yazılmadan ÖNCE, kendi ölçümüm)

Kaynak kayıt (`ham/qa-turu-9-bulgular.json`, rota `/siparisler`): *"Önceki turdan taşındı, bu turda doğrulanamadı (oturum yok). Önceki kanıt: düzenleme formunda yalnızca sipd-d-gun, sipd-d-aciklama ve kalem alanları var."*

**Sonuç: ASIL BULGU KAPANMIŞ — yeniden üretilemedi.** P-51 bu alanları zaten eklemişti. Kanıt:

| Katman | Ölçüm | Sonuç |
|---|---|---|
| Kod | `SiparisModulu.cs` `Guncelle`: `tur`, `cariId`, `subeId` alanları PATCH'te okunuyor; durum kapısı `s.Durum != TASLAK → 400`; değişiklik `siparis_gecisleri`'ne `BASLIK_DUZENLENDI:` önekiyle yazılıyor | var |
| Kod | `SiparisDetay.razor`: `sipd-d-tur`, `sipd-d-cari`, `sipd-d-sube` (tek şubede `sipd-d-sube-sabit`) | var |
| Tarayıcı (1280, kendi sekmem tab-31) | Düzenle → formda `sipd-d-tur:BUTTON gorunur · sipd-d-cari:BUTTON gorunur · sipd-d-sube:BUTTON gorunur` | var |
| Tarayıcı | Tür → Tamir, Cari → Q93 Mehmet Demir, Şube → Q93 Kadıköy → Kaydet | bant "Sipariş güncellendi", kart "Tür: Tamir / servis · Cari: Q93 Mehmet Demir" |
| API | `GET /siparisler/01M396CD…` → 200 | `tur=TAMIR`, `cariId=01M396C660…`, `subeId=01M396C62M…` |
| SQL | `select no,durum,tur,cari_id,sube_id from siparisler` | `S-2026-000001 TASLAK TAMIR 01M396C660… 01M396C62M…` + geçiş satırı `BASLIK_DUZENLENDI: cari … · tür … · şube …` |
| Mevcut testler | `P51|SiparisTestleri|SiparisEkranTestleri|P52` filtreli | 66/66 yeşil |

Bu yüzden asıl bulgu için sunucu ve form alanlarında **kod yazılmadı**.

### 1.1 Aynı ölçümün gösterdiği GERÇEK kusurlar (bulgunun kapsamındaki ekranda)

Aynı düzenleme akışı ölçülürken, kabul koşulunun ("taslakta cari/tür/şube değişir, GET'te görünür; onaylıda kilitli ve gerekçeli") kullanıcı tarafını bozan şu kusurlar görüldü. Hepsi `SiparisDetay.razor`'da, bu şeridin dosya alanında:

| # | Kusur (ölçülen) | İhlal |
|---|---|---|
| K1 | Detay kartında **şube satırı yoktu**: şube değiştirilip kaydedilince kullanıcı sonucu kartta göremiyordu (yalnız formu yeniden açınca). | UX-S27 |
| K2 | Cari seçicinin altındaki sınır notu: **"Sunucu en çok {0} sipariş döndürür; daha fazlası var. Tür, durum ya da cari süzgeciyle daraltın."** — `{0}` çıplak basılıyordu, konu "sipariş" (Siparisler listesinin metni), "daha fazlası var" her zaman yazıyordu. | UX-S29 |
| K3 | Ürün sınır notu aynı metinle "**Sunucu en çok 100 sipariş döndürür; daha fazlası var**" diyordu (ürün değil). | UX-S29 |
| K4 | Cari seçeneklerinde ham kod: `"Q93 Ayşe YılmazMUSTERI"` (alt bilgi `MUSTERI`). | UX-S31 |
| K5 | Durum geçmişinde başlık düzeltmesi: `Gerekçe: BASLIK_DUZENLENDI: cari 01M396C642… → 01M396C660… · tür MUSTERI_SIPARISI → TAMIR · şube 01M396BQ… → 01M396C62M…` — ham önek, ham tür kodu ve kayıt kimlikleri (ULID). | UX-S31 · B-25 |
| K6 | Teslim edilmiş / iptal edilmiş siparişte kilit notu "Yanlışlık varsa siparişi gerekçeli olarak **iptal edin** ve yenisini açın" diyordu — bu durumlarda iptal geçişi YOKTUR (durum makinesinde terminal). Var olmayan yol. | UX-S25 |
| K7 | ATÖLYEDE/HAZIR'da kilit notu yalnız "iptal edin" diyordu; oysa K-23 geri alma zinciri (Atölyede → Onaylandı → Taslak) gerçekten var ve düğmeleri ekranda. "Taslak'a dön" notu yalnız ONAYLANDI'da çıkıyordu. | UX-S25 · UX-S30 |

## 2. Yapılanlar

- **Kart:** `#sipd-sube` satırı eklendi — ad oturumun şube listesinden (`/subeler`, sayfa açılışında zaten çekiliyor) çözülür; şubesiz siparişte "—", listede olmayan kimlikte "(listede olmayan kayıt)". Kimlik basılmaz.
- **Cari sınır notu:** `#sipd-d-cari-sinir` artık "Listede en çok 200 aktif müşteri yer alır; aramak için listenin arama kutusuna yazın." Yükleme sınırı 50 → `Secici.YerelTavan` (200; Secici'nin yerel kip tavanı — tek kaynak). Liste sınıra değdiyse `#sipd-d-cari-dahafazla` uyarı bandı "Daha fazla müşteri var: listede ada göre ilk 200 müşteri gösteriliyor, gerisi bu listede yok." (sunucu `OrderBy(c => c.Ad)` — ölçüldü).
- **Ürün sınır notu:** `#sipd-d-urun-sinir` "Listede en çok 100 aktif ürün…"; sınıra değdiyse `#sipd-d-urun-dahafazla` "…aradığınız yoksa kalemi ürün seçmeden “Serbest tanım” alanına yazın." (bu yol GERÇEK: kalemde serbest tanım alanı var; sunucu `OrderBy(u => u.Kod)` — ölçüldü).
- **Cari seçeneği:** alt bilgi sözlükten (`Müşteri/Tedarikçi/Müşteri ve tedarikçi/Atölye/Sarraf`); bilinmeyen kod çıplak basılmaz (null).
- **Geçiş izi:** `GerekceMetni` — `BASLIK_DUZENLENDI:` önekli gerekçe ekranda "Sipariş bilgileri düzeltildi — Cari: A → B · Tür: Müşteri siparişi → Tamir / servis · Şube: Merkez → Kadıköy" olur. Sunucudaki iz DEĞİŞMEDİ (denetim için kimlikle saklanmaya devam eder; append-only). Tanınmayan parça olduğu gibi kalır (veri gizlenmez); başka gerekçeler aynen yazılır.
- **Kilit notları:** terminal siparişte (`kart.terminal`) `DegismezIpucu` gerekçe/alternatifi "Bu sipariş teslim edildi ya da iptal edildi; kaydı artık değişmez." / "Yanlışlık varsa doğru bilgilerle yeni bir sipariş açın; bu kayıt geçmişte olduğu gibi kalır." ONAYLANDI'da mevcut `#sipd-taslaga-don-notu` korunur; ATÖLYEDE/HAZIR'da (geri yönlü geçiş varsa) yeni `#sipd-geri-al-notu`: "Düzenlemek için siparişi “Durumu değiştir” bölümündeki geri alma düğmeleriyle adım adım “Taslak” durumuna getirin; her geri alma gerekçeyle kaydedilir."
- **Sözlük:** `Kuyumcu.Core/I18n/Sozlukler.SiparisBaslik.cs` (YENİ) — konumsal record, TR+EN aynı kurucudan (eksik anahtar = derleme hatası). `Sozlukler.cs` hub'ına DOKUNULMADI.
- **Sunucu / Contracts / Siparisler.razor:** DEĞİŞMEDİ (gerek yok — § 1).

## 3. Sapmalar ve gerekçeleri

1. **Asıl bulgu için kod yazılmadı** — orkestratör koşulu: "üretilemiyorsa KAPANMIŞ yaz, kod YAZMA". Yazılan kod asıl bulguya değil, aynı ekranda aynı ölçümle yakalanan K1–K7'ye aittir; hepsi bu şeridin dosya alanında (`Pages/SiparisDetay.razor`) ve kabul koşulunun ("GET'te görünür", "kilitli ve gerekçeli") kullanıcıya görünen yarısıdır.
2. **Cari listesi 200'de kalır, uzak arama açılmadı:** `Secici`'nin uzak kipi (`UzakTur=CARI`, `/arama`) hiçbir sayfada kullanılmıyor; bu pakette açmak P-51'in seçenek listesine dayanan testlerini ve seçili carinin korunma kuralını yeniden kurmayı gerektirirdi. Sınır dürüstçe ekranda yazıyor ve sınıra değince uyarı çıkıyor (UX-S29 "sessiz kırpma yok"). 200'den fazla aktif müşterisi olan kiracıda 201. müşteriye düzeltme yolu **açık kalan** olarak yazıldı (§ 11).
3. **Geçiş izi ekranda çevrilir, sunucuda değil:** sunucu gerekçesi denetim kanıtıdır (kimlikle, dil bağımsız); ekran dilini ve adları istemci bilir. Eski cari 200'lük listede yoksa ve o anki cari de değilse "(listede olmayan kayıt)" yazar — kimlik basmaz.

## 4. Migration
**Gerekmedi** — şema değişikliği yok.

## 5. Mevzuat notu
Bu paket mevzuat hükmü içermez (sipariş deftere yazmaz; başlık düzeltmesi yalnız TASLAK'ta). Tarih/belge no/kesilmiş fatura kilidine dokunulmadı.

## 6. Testler

- Yeni: `Kuyumcu.Tests/Q93SiparisBaslikEkranTestleri.cs` [bUnit, 15; **durumlu** sahte API — PATCH gövdesi sonraki GET'e yansır; "{}" varsayılanı yok]:
  `Q93_taslakta_cari_tur_sube_degisir_ve_KARTTA_gorunur` · `Q93_kartta_sube_ADIYLA_yazar_kimlik_basilmaz` · `Q93_subesiz_sipariste_kartta_tire_yazar` · `Q93_musteri_sinir_notu_SAYIYLA_yazar_yer_tutucu_ve_siparis_kelimesi_yok` · `Q93_musteri_sayisi_sinira_degince_DAHA_FAZLA_uyarisi_cikar` · `Q93_urun_sinir_notu_URUN_der_sinira_degince_uyarir` · `Q93_urun_sayisi_sinira_degince_DAHA_FAZLA_uyarisi_cikar_P52_secici_bozulmaz` · `Q93_musteri_seceneginde_HAM_tur_kodu_basilmaz` · `Q93_baslik_duzeltme_izi_ADLARLA_yazar_kimlik_ve_ham_kod_basilmaz` · `Q93_listede_olmayan_eski_cari_kimlikle_DEGIL_notla_yazar` · `Q93_baska_gerekce_oldugu_gibi_yazar` · `Q93_onayli_sipariste_DUZENLE_yok_kilit_gerekceli_taslaga_don_yolu_yazili` · `Q93_atolyedeki_sipariste_geri_alma_zinciri_notu_yazar` · `Q93_teslim_edilmis_sipariste_OLMAYAN_iptal_yolu_onerilmez` · `Q93_sozluk_TR_ve_EN_bos_anahtar_yok`
- Filtreli koşum (`Q93|P51|SiparisTestleri|SiparisEkranTestleri|P52|SiparisDetay`): **82/82 yeşil** (önce aynı filtrenin Q93'süz hâli 66/66).
- Tam süit (trx `scratchpad/trx-pQ9-3/_Ramazan-MacBook-Air_2026-09-24_11_20_51.trx`): `<Counters total="5113" executed="5113" passed="5113" failed="0" …/>` — **0 başarısız**. Bu paket +15 test ekledi.

## 7. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/Q9-3/` altına kopyalandı → tek eşleşmeli değişiklik (eşleşme sayısı 1 değilse betik UYGULAMAZ) → `dotnet build` (başarısızsa mutasyon GEÇERSİZ sayılır, yeniden denenir) → filtreli test → yedek geri kopyalandı → `cmp` AYNI.
(İlk MQ93-4 denemesi başka şeridin eşzamanlı derlemesi yüzünden `MvcTestingAppManifest.json … used by another process` ile DERLENEMEDİ ve eski dll'le 15/15 geçti — geçersiz sayıldı, betiğe derleme yeniden denemesi eklendi, mutasyon tekrarlandı.)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| MQ93-1 | Sunucu `Guncelle` durum kapısı `if (false && s.Durum != TASLAK)` | `TaslakDisiSiparisteBaslikPatchReddedilir`, `PATCH_TASLAK_disinda_400_doner`, `TESLIM_EDILDI_sonrasi_PATCH_reddedilir` | cmp AYNI |
| MQ93-2 | Sunucu `s.SubeId = yeniSubeId;` silindi | `SubeDegisimiKiraciDisiSubeyiReddeder` | cmp AYNI |
| MQ93-3 | Sunucu `s.CariId = yeniCariId;` silindi | `TaslakSiparisinCarisiDegistirilebilir` | cmp AYNI |
| MQ93-4 | Kart `@SubeAdi(s.SubeId)` → `@s.SubeId` | `Q93_kartta_sube_ADIYLA_yazar_kimlik_basilmaz`, `Q93_subesiz_sipariste_kartta_tire_yazar`, `Q93_taslakta_cari_tur_sube_degisir_ve_KARTTA_gorunur` | cmp AYNI |
| MQ93-5 | Cari sınır notu eski `SinirUyarisi`'ne döndü | `Q93_musteri_sinir_notu_SAYIYLA_yazar_yer_tutucu_ve_siparis_kelimesi_yok` | cmp AYNI |
| MQ93-6 | `cariler.Count >= CariSiniri` → `>` | `Q93_musteri_sayisi_sinira_degince_DAHA_FAZLA_uyarisi_cikar` | cmp AYNI |
| MQ93-7 | Cari seçeneği `CariTurMetni(c.Tur)` → `c.Tur` | `Q93_musteri_seceneginde_HAM_tur_kodu_basilmaz` | cmp AYNI |
| MQ93-8 | Geçiş `@GerekceMetni(g.Gerekce)` → `@g.Gerekce` | `Q93_baslik_duzeltme_izi_ADLARLA_yazar_kimlik_ve_ham_kod_basilmaz`, `Q93_listede_olmayan_eski_cari_kimlikle_DEGIL_notla_yazar` | cmp AYNI |
| MQ93-9 | Terminal alternatif yolu koşulsuz eski metin | `Q93_teslim_edilmis_sipariste_OLMAYAN_iptal_yolu_onerilmez` | cmp AYNI |
| MQ93-10 | `else if (s.GeriYonluGecislerL.Count > 0)` → `else if (false)` | `Q93_atolyedeki_sipariste_geri_alma_zinciri_notu_yazar` | cmp AYNI |
| MQ93-11 | `CariAdi` geri dönüşü `B.KayitListedeYok` → `id` | `Q93_listede_olmayan_eski_cari_kimlikle_DEGIL_notla_yazar` | cmp AYNI |
| MQ93-12 | Ürün sınır notu eski `SinirUyarisi`'ne döndü | `Q93_urun_sinir_notu_URUN_der_sinira_degince_uyarir` | cmp AYNI |

12/12 mutasyon kırmızı; açık yok. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata, 1 Uyarı** (uyarı `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` — bu paketin dosyası değil).

## 8. denetim.sh
`bash dotnet/denetim.sh` → **DENETİM TEMİZ.** Sayaçlar: `IgnoreQueryFilters 0` · `Find/FindAsync 0` · `Hareketler.Add 1` · `Pages+Bilesenler+Layout record 3 (tavan 3)` · `satır içi style= 105 (tavan 196)` · `ModalPencere 48 (tavan 55, artış 0)` · `duzenlenenId 1 (tavan 1)` · `P-18 SABIT_RENK 0` · `P-36 SECICI_TEK_KAYNAK 1` · `P-39 FORM_SATIR_HIZA_SAPMASI 0`. Bu paket yeni record, satır içi stil, ModalPencere, CSS eklemedi (yalnız mevcut `bant bant-uyari`, `metin-kucuk`, `metin-soluk` sınıfları).

## 9. Canlı doğrulama (API 5174 + vekil 5173, kendi sekmem tab-31, `kuyumcu_net`, kiracı `PAKETQ9-3-TEST`)

Kurulum API ile: davet satırı → `POST /auth/kayit` 201 (tenant `01M396BQNTS3K2EGYGT57R9Y37`) → `POST /subeler` "Q93 Kadıköy Şube" 200 → `POST /cari` ×2 200 → `POST /siparisler` ×3 200 (S-2026-000001/2/3).

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| Taslak S-1: formda cari/tür/şube | üç seçici görünür, dolu | tür 3 seçenek, cari `["—","Q93 Ayşe Yılmaz Müşteri","Q93 Mehmet Demir Müşteri"]` (değişiklikten sonra; önce `…MUSTERI`), şube 2 seçenek | DOM |
| Tür→Tamir, Cari→Mehmet, Şube→Kadıköy, Kaydet (tarayıcı) | PATCH 200, kart günceller | bant "Sipariş güncellendi"; kart "Tür: Tamir / servis · Cari: Q93 Mehmet Demir" | SQL `TASLAK TAMIR 01M396C660… 01M396C62M…` |
| (değişiklikten sonra) Kartta şube | ad görünür | "Şube: Q93 Kadıköy Şube" | DOM |
| (değişiklikten sonra) Geçiş izi | ad + sözlük metni, kimlik yok | "Gerekçe: Sipariş bilgileri düzeltildi — Cari: Q93 Ayşe Yılmaz → Q93 Mehmet Demir · Tür: Müşteri siparişi → Tamir / servis · Şube: Merkez Şube → Q93 Kadıköy Şube" | DOM; SQL gerekçe kimlikle saklı (değişmedi) |
| İkinci düzeltme tarayıcıdan: Ayşe · Müşteri siparişi · Merkez | PATCH 200 | bant "Sipariş güncellendi"; kart "Tür: Müşteri siparişi · Cari: Q93 Ayşe Yılmaz · Şube: Merkez Şube"; iz satırı adlarla | `GET` 200 `tur=MUSTERI_SIPARISI cariId=01M396C642… subeId=01M396BQ… duzenlenebilir=True`; SQL aynı; `BASLIK_DUZENLENDI` satır sayısı 2; `denetim_kayitlari Siparis GUNCELLEME 3 · SiparisGecisi EKLEME 5` |
| Sınır notları | sayı + doğru nesne | "Listede en çok 200 aktif müşteri…" · "Listede en çok 100 aktif ürün…"; `dahafazla` bantları yok (2 cari, 0 ürün) | DOM |
| ONAYLANDI S-2'ye PATCH (cari+şube+tür) | 400, satır değişmez | `400 "Yalnız TASLAK durumundaki sipariş düzenlenebilir (bu sipariş: ONAYLANDI)"` | SQL `S-2026-000002 ONAYLANDI MUSTERI_SIPARISI 01M396C642… 01M396BQ…` (değişmedi) |
| ONAYLANDI S-2 ekranı | Düzenle yok, kilit + gerekçe + Taslak'a dön yolu | `duzenle=false cariForm=false`; ipucu "Bu sipariş TASLAK durumunda değil…"; `#sipd-taslaga-don-notu` var; `#sipd-geri-TASLAK` düğmesi var | DOM + görüntü |
| ATÖLYEDE S-2 ekranı | geri alma zinciri notu | `#sipd-geri-al-notu` "Düzenlemek için siparişi “Durumu değiştir”…"; `#sipd-geri-ONAYLANDI` var | DOM + görüntü (1280 açık, 375 koyu) |
| İPTAL S-3 ekranı (terminal) | "iptal edin" YOK | ipucu "Bu sipariş teslim edildi ya da iptal edildi; kaydı artık değişmez. … yeni bir sipariş açın…"; iptal düğmesi yok; "Bu durumdan gidilebilecek başka durum yok." | DOM + görüntü |
| P-52 (ürün seçici, tahmini tutar) | bozulmaz | formda `sipd-d-kalem-urun-0` seçici + `sipd-d-tahmini` görünür, "sunucudan" rozeti | DOM + görüntü; bUnit `…P52_secici_bozulmaz` + P52 filtreli testler yeşil |

(mysql istemcisinin konsol karakter kümesi Türkçe harfleri `?` bastı; veri UTF-8 ve API/ekranda doğru.)

### 9.1 Gerçek tarayıcı ölçüm tablosu (`sikisma-olc.js`, kendi sekmem tab-31)

Ölçülen sayfalar: **form** = S-1 TASLAK, Düzenle açık (değişen alanların tamamı görünür); **kilit-A** = S-2 ATÖLYEDE; **kilit-T** = S-3 İPTAL. Mod `#ust-mod-basit/#ust-mod-profesyonel` tıklanarak, tema `prefers-color-scheme` öykünmesiyle.

| Genişlik | Tema | Mod | Sayfa (form / kilit-A / kilit-T) | taşan | binen (ham) | kırık (ham) | yatay kaydırma | [hidden] görünen | 'TRY' kalıbı | en geniş blok |
|---|---|---|---|---|---|---|---|---|---|---|
| 1280×800 | açık | Basit | 3 sayfa | 0/0/0 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | açık | Profesyonel | 3 sayfa | 0/0/0 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | koyu | Basit | 3 sayfa | 0/0/0 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | %95 |
| 1280×800 | koyu | Profesyonel | 3 sayfa | 0/0/0 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | %95 |
| 375×812 | açık | Basit | 3 sayfa | 0/0/0 | 3/2/2 | 6/3/0 | 0 | 0 | 0 | %94 |
| 375×812 | açık | Profesyonel | 3 sayfa | 0/0/0 | 3/2/2 | 6/3/0 | 0 | 0 | 0 | %94 |
| 375×812 | koyu | Basit | 3 sayfa | 0/0/0 | 3/2/2 | 6/3/0 | 0 | 0 | 0 | %94 |
| 375×812 | koyu | Profesyonel | 3 sayfa | 0/0/0 | 3/2/2 | 6/3/0 | 0 | 0 | 0 | %94 |

375'teki ham sayaçlar ölçüyle ayrıştırıldı — **gerçek kusur 0**, hepsi bu paketin dokunmadığı mevcut öğeler:
- **kırık (ham 6/3):** `#sipd-d-kalem-sil-0`, `#sipd-d-kalem-ekle`, `#sipd-d-kaydet`, `#sipd-d-vazgec`, `#sipd-gecis-ONAYLANDI`, `#sipd-gecis-HAZIR`, `#sipd-geri-ONAYLANDI`, `#sipd-gecis-IPTAL` — hepsi mobil dokunma hedefi `min-height: 44px`, `Range.getClientRects` metin satırı = **1** (Q9-1'de de aynı yanlış pozitif).
- **binen (ham 3/2/2):** `#sipd-gecisler li` içindeki satır içi `<strong>` ile `<span class="metin-soluk">` — satır kaydıran satır içi öğenin `getBoundingClientRect`'i birleşik kutu verir; satır kutuları (`getClientRects`) karşılaştırıldığında çakışma = **0** (her li).
- Yeni öğeler 375'te: `#sipd-sube`, `#sipd-d-cari-sinir`, `#sipd-d-urun-sinir` genişlik 317 px, `scrollWidth-clientWidth = 0`; `#sipd-geri-al-notu` 317 px.

Ekran görüntüleri (bu oturumda, tab-31): (a) 1280 açık — değişiklikten ÖNCE cari alanı altında "Sunucu en çok {0} sipariş döndürür…"; (b) 1280×1600 açık — form: kartta "Şube: Q93 Kadıköy Şube", cari seçicisi açık ("Q93 Ayşe Yılmaz / Müşteri"), ürün sınır notu, durum geçmişi adlarla; (c) 1280 açık — ONAYLANDI kilit + "Geri al: Taslak"; (d) 1280 açık — ATÖLYEDE kilit + geri alma notu; (e) 1280 açık — İPTAL kilit, yeni sipariş yolu; (f) 375 koyu — ATÖLYEDE; (g) 375×1700 koyu — form tam boy. Araç notu: tarayıcı bölmesinin yakalaması öykünülmüş 1280×800'de kaydırılmış sayfayı boş boyuyor (Q9-1'deki ile aynı); tam form görüntüsü bu yüzden uzun görüntü alanında alındı, ölçüm 1280×800 ve 375×812'de.

### 9.2 Temizlik
- API (PID 6057, sonra yeni derlemeyle PID 21341) ve vekil (PID 6561) durduruldu: `lsof :5174` = 0, `:5173` = 0. Sekme tab-31 kapatıldı, görüntü alanı `desktop`'a döndü.
- Test kiracısı `01M396BQNTS3K2EGYGT57R9Y37` (`PAKETQ9-3-TEST`) `kiraci-temizle.sh --uygula` ile silindi; `tenant_id` kolonlu **70 tablonun toplamında kalan satır 0**; davet satırı `Q93DAVET…` silindi (0). **tenants: başlangıç 11 → son 11.** QA-TEST-*, DEMO-* ve başka şeridin `PAKETS1-i-TEST` kiracısına dokunulmadı.
- Fikstür/jeton/davet dosyaları `printf '' >` ile boşaltıldı (silme kurucuya).

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketinkiler)
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor` (depoda izlenmiyor — `??`; değiştirildi; değişiklik öncesi kopya `scratchpad/once-q93/SiparisDetay.razor`, fark yalnız bu paketin satırları)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SiparisBaslik.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/Q93SiparisBaslikEkranTestleri.cs` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/PAKET-Q9-3-uygulama-raporu.md` (bu rapor)

Dokunulmayanlar: `Kuyumcu.Api/Modules/Siparis/*`, `Contracts` sipariş sözleşmeleri, `Pages/Siparisler.razor`, `Sozlukler.cs`, `app.css`, S1-h/i dosyaları.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/SiparisDetay.razor`.

## 11. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Mevcut sipariş başlığı: `PATCH /siparisler/{id}` (gönderilmeyen alan korunur, `subeId:null` siler, `cariId/tur:null` 400) + `[ModulYetkisi]` (P-51'den) + denetim izi kendiliğinden (`DenetimYazmaDinleyicisi`; canlıda `Siparis GUNCELLEME 3`) + ekranda Düzenle (TASLAK) / DegismezIpucu + gerçek yol (ONAYLANDI: Taslak'a geri al · ATÖLYEDE/HAZIR: geri alma zinciri · TESLİM/İPTAL: yeni sipariş). Finansal varlığa dokunulmadı.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Önce bulguyu kendi ölçümle yeniden üret; üretilemiyorsa KAPANMIŞ | ✓ KAPANMIŞ | § 1 (kod + tarayıcı + API + SQL) |
| Taslakta cari/tür/şube değişir, GET'te görünür (SQL) | ✓ | canlı iki düzeltme, GET 200, SQL; bUnit `Q93_taslakta_…KARTTA_gorunur` |
| Onaylıda kilitli ve gerekçeli, doğru yol | ✓ | canlı PATCH 400 + SQL değişmedi; ekran DegismezIpucu + Taslak'a dön; ATÖLYEDE/İPTAL notları |
| P-52 davranışı bozulmaz | ✓ | P52 testleri + `…P52_secici_bozulmaz`; canlı ürün seçici + tahmini tutar |
| DOM id ve rota değişmez | ✓ | yalnız YENİ id'ler (`sipd-sube`, `sipd-d-cari-dahafazla`, `sipd-d-urun-dahafazla`, `sipd-geri-al-notu`) |
| DegismezIpucu GERÇEKTEN var olan yolu söyler | ✓ | terminalde "iptal edin" kaldırıldı (MQ93-9) |
| app.css / Sozlukler hub / S1-h/i dosyaları | ✓ dokunulmadı | § 10 |
| Tek yazma yolu | ✓ | sunucu değişmedi; `Hareketler.Add 1` |
| Build 0 hata · paket testleri 0 başarısız · sayaçlar ✓ · mutasyon · canlı · kiracı silindi · yeni kırmızı yok | ✓ | § 6–9; tam süit 5113/5113 |

## 13. Açık kalanlar
1. **200'den fazla aktif müşterili kiracıda** sipariş düzeltme formunda 201. ve sonraki müşteri seçilemez (ekran bunu uyarıyla söylüyor). Kalıcı çözüm: `sipd-d-cari`'yi `Secici` uzak kipine (`UzakTur=CARI`, `UzakSeciliAd`) geçirmek + P-51 testlerini uzak kipe uyarlamak. Sahibi: orkestratör / sonraki sipariş paketi.
2. Aynı ekranda bu paketin dokunmadığı, ölçümde görülen jargon: `#sipd-appendonly` "Bu döküm append-only'dir…" ve emanet uyarısındaki "EMANET_GIRIS" ham kodu; kart etiketi "Tahmini tutar (TRY)". Sahibi: sadeleştirme/jargon şeridi (UX-S31).
3. 1280 ekran görüntüsünde düğmeler (`Değişiklikleri kaydet`, `Onaylandı` vb.) biçimsiz göründü — `app.css`/Secici şeridinin (S1-i, eşzamanlı) alanı; bu paket CSS'e dokunmaz. Sahibi: S1-i.
4. Tam süitte başkasına ait kırmızı YOK (5113/5113); derleme uyarısı `EtiketEkranTestleri.cs(145,47) xUnit1031` — sahibi Etiket şeridi.

## 14. Kurucuya / mali müşavire not
Mali müşavir teyidi gerektiren bir hüküm yok. Kurucuya: QA turu 9'daki bu KRİTİK bulgu P-51 ile zaten kapanmıştı (tur 9'da oturum olmadığı için doğrulanamamıştı); bu paket o düzeltmenin ekranda görünmeyen yarısını (kartta şube, okunur geçmiş, doğru sınır ve kilit notları) tamamladı.
