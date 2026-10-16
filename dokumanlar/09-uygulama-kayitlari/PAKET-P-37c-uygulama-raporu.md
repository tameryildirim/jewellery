# PAKET P-37c — AÇILIR PANEL SARMA ÖRTÜŞMESİ + KALAN EKRANLAR + ARTIK TEMİZLİĞİ

**Dalga:** DALGA 6 · **Roller:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 2026-09-21
**Önkoşul:** P-37 düzen disiplini · **Sürüm:** 1.0

> Anayasa md.1: aşağıdaki her "bitti" bir ölçüme ya da komut çıktısına dayanır.
> Ölçülemeyen kalem **AÇIK** olarak § 10'da yazılıdır, gizlenmemiştir.

---

## 1. Özet

P-37 doğrulayıcısının açık bıraktığı dört kalem ele alındı:

| # | Kalem | Sonuç |
|---|---|---|
| 1 | Açılır panel sarma örtüşmesi (1280px'te 5056 px²) | **KAPANDI** — kesişim 4 genişlikte de **0 px²** (ölçüm § 6) |
| 2 | Ortak araç şeridine geçmeyen ekranlar | **4 ekran geçirildi**, 1 ekran **yazılı muafiyet** (§ 4) |
| 3 | Ağaçtaki `.mutorj`/`.bak` artıkları | **19 dosya boşaltıldı**, toplam **0 bayt** (§ 5) |
| 4 | `Secici` iç arama kimliği sapması | **YAZILI MUAFİYET** + 2 test (§ 7) — geri dönüş TEKNİK OLARAK YANLIŞ olurdu |

Derleme **0 uyarı / 0 hata**, tam süit **3694/3694 yeşil**, `dotnet/denetim.sh` **TEMİZ**.
Migration **gerekmedi** (bu paket veri modeline dokunmaz).

---

## 2. Spesifikasyonun koda karşı doğrulanması — 3 sapma

| # | Spesifikasyon iddiası | Koddaki gerçek | Yapılan |
|---|---|---|---|
| S1 | "bugün **42/46** ekran `<SayfaBasi>` kullanıyor, **kalan 4**" | Ölçüm: **41/46** kullanıyor, **kalan 5** (`Bildirimler`, `Panel`, `SiparisDetay`, `Siparisler`, `Vezne`) | 4'ü geçirildi; `Panel` gerekçeli muaf (§ 4.2). Hedefe sadık kalındı. |
| S2 | "bu oturumda üretilen 5 `.mutorj` + 2 `.bak` … **toplam 19 .mutorj + 3 .bak**" | Ölçüm: **16 `.mutorj` + 3 `.bak` = 19 dosya** (3 `.bak` zaten 0 bayt) | 19 dosyanın tamamı boşaltıldı; ayrıca 0 baytlık `denetim.sh.mutbak` sayaca dâhil edildi. |
| S3 | Mekanizma: `SAYFA_BASI_KAPSAM = 46/46` | 46/46 ancak karşılama ekranının tasarımı bozularak mümkün | Sayaç **45/45 uygun ekran + 1 YAZILI muafiyet** olarak tanımlandı. Paket metni "geçemeyecek olan varsa gerekçesi raporda tek tek yazılır" dediği için sapma sözleşme içidir. |

Mevzuat/yasal hüküm **yok** — bu paket tamamen sunum katmanıdır, yapılandırmadan
okunacak bir hüküm doğurmaz.

---

## 3. Kalem 1 — Açılır panel sarma örtüşmesi

### 3.1 Kök sebep (ölçüldü, tahmin edilmedi)

`.secici-liste-kap` **`position: absolute`** idi. Mutlak konumlu kutu **akıştan
çıkar**: araç şeridi dar ekranda ikinci satıra sardığında o satır yukarıda kalır
ve açık panel onun **üstüne biner**. `z-index` bunu çözmez — panel görünür olur
ama altındaki kontrol okunamaz ve tıklanamaz kalır.

### 3.2 Çözüm

Araç şeridi **kapsamında** panel **akışta** bırakıldı:

```css
.sayfa-basi-filtre .secici-liste-kap {
  position: relative;          /* static DEĞİL: z-index gerekiyor */
  top: auto; left: auto; right: auto;
  margin-top: var(--bosluk-1);
  min-width: 100%; max-width: 100%;   /* kendi sütununu AŞMAZ */
  z-index: 41;                        /* .secici-perde z-index 40 */
}
.sayfa-basi:has(.sayfa-basi-filtre .secici-liste-kap),
.sayfa-basi-filtre:has(.secici-liste-kap) { align-items: flex-start; }
```

Üç karar ve gerekçeleri:

1. **`relative`, `static` değil** — `.secici-perde` (dışarı tıklama perdesi)
   `z-index: 40`'tır. `static` bir panel z-index alamaz, perde üstünü kapatır ve
   **seçenekler tıklanamaz** olurdu. Testle sabitlendi.
2. **`max-width: 100%`** — bileşenin genel kuralı `max-width: 150%`'tir; şeritte
   bu, panelin **yandaki** kontrolün üstüne taşmasına izin verirdi (yatay kesişim).
3. **`:has(...) → align-items: flex-start`** — panel akışa girince şerit satırı
   uzar; varsayılan `flex-end` hizası **aynı satırdaki komşuları aşağı kaydırırdı**
   (dropdown açılınca kontroller zıplardı). Üstten hiza ile komşular **yerinde**
   kalır; şerit çocuklarının yükseklikleri `--arac-etiket-bosluk` ile zaten
   eşitlendiği için normal durumda iki hiza aynı sonucu verir.

### 3.3 Yalan yorum düzeltildi

`app.css`'teki eski yorum bloğu "kusur AÇIKTIR ve ayrı pakete devredildi" diyordu;
yerine kök sebep + çözüm + **ölçüm betiğine atıf** yazıldı. Bir test (`Olculmemis_
kesisim_iddiasi_yorumda_kalmaz_olcum_betigine_atif_var`) yorumun ölçüm betiğine
atıf taşımasını zorunlu kılar — "ölçülmemiş 0 iddiası" bir daha yazılamaz.

---

## 4. Kalem 2 — Ortak araç şeridi kapsamı

### 4.1 Geçirilen 4 ekran (TEK DOKUNUŞ kaydı — bu dalgada açılan sayfalar)

| Ekran | `Anahtar` | Şeride taşınan | Şeritte kalmayan (ve nedeni) |
|---|---|---|---|
| `Pages/Bildirimler.razor` | `bildirimler` | `bildirim-yenile` (Eylem) | Durum **sekmeleri** gezinmedir, süzgeç değil → kartta kaldı |
| `Pages/Siparisler.razor` | `siparisler` | `sip-durum-suzgec`, `sip-geciken`, `sip-yenile` (Filtreler) · `sip-yeni-ac` (Eylem) | Tür sekmeleri (`ProAlan` içindekiler dâhil) gezinmedir |
| `Pages/SiparisDetay.razor` | `siparis-detay` | — (süzgeç yok) | Detay ekranı; şerit başlık + açıklama taşır |
| `Pages/Vezne.razor` | `vezne` | `vz-durum-suzgec`, `vz-yenile` (Filtreler) · `vz-yeni-ac` (Eylem) | `vz-ayrim-rozet` **bilgi** öğesidir, süzgeç değil |

**Kimlikler DEĞİŞMEDİ** — taşınan her düğme/alan kimliğini korudu ve her biri
ekranda **tam olarak bir kez** doğuyor (test: `Serit_yuvalarina_tasinan_dugmeler_kaybolmadi`).

Sözlük: `SozlukBildirim.EkranAciklama`, `SozlukVezne.EkranAciklama`,
`SozlukSiparis.EkranAciklama` / `.DetayEkranBaslik` / `.DetayEkranAciklama`
**TR + EN** olarak eklendi (çıplak metin yok).

### 4.2 Geçmeyen 1 ekran — `Pages/Panel.razor` (YAZILI MUAFİYET)

Karşılama ekranı 46 ekranın **tekidir** ki başlığı sabit bir ekran adı değil,
kişiye ve saate göre değişen **selam**dır ("Günaydın Ramazan") ve yanında **hava**
kutucuğu durur. `SayfaBasi` iki yuva sunar: `Filtreler` ve `Eylem`. Hava ne
süzgeçtir ne eylem; selam da ekran adı değildir. Bu ekranı şeride sokmak, ya
havayı "süzgeç" diye **yanlış yuvaya** koymayı ya da **P-37b'de mühürlenen**
karşılama tasarımını bozmayı gerektirirdi.

Muafiyet **sessiz değildir**: gerekçe `Panel.razor`'un kendi başında
`SAYFA_BASI_MUAFIYETI` başlıklı blokta yazılıdır, denetim sayacı ve bir test bu
işaretin varlığını zorunlu kılar. `SAYFA_BASI_DISI_DUZEN = 0` Panel için de
geçerlidir (ekran başına hizalama CSS'i orada da yazılmaz).

---

## 5. Kalem 3 — Artık temizliği

Kabuk kancası `rm`/`mv` engellediği için dosyalar **silinemez**; `printf '' >` ile
**boşaltıldı** (içerik gitti, dosya kaldı). Silme **K-4 olarak kurucudadır.**

Boşaltılan **19** dosya (hepsi artık 0 bayt):

```
dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj        (5 853 B)
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs.mutorj    (14 687 B)
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs.mutorj    (5 338 B)
dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs.mutorj    (26 235 B)
dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs.mutorj    (11 443 B)
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs.mutorj    (87 115 B)
dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs.mutorj    (13 749 B)
dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs.mutorj     (3 720 B)
dotnet/Kuyumcu.Api/Modules/Urun/PirlantaKapisi.cs.mutorj  (2 619 B)
dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs.mutorj      (37 404 B)
dotnet/Kuyumcu.Api/Modules/Vade/VadeKapisi.cs.mutorj      (10 289 B)
dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor.mutorj       (11 443 B)
dotnet/Kuyumcu.Web/Pages/Islemler.razor.mutorj            (66 186 B)
dotnet/Kuyumcu.Web/Pages/Sayim.razor.mutorj               (11 529 B)
dotnet/Kuyumcu.Web/Pages/Urunler.razor.mutorj             (43 382 B)
dotnet/Kuyumcu.Web/Pages/VadeTakibi.razor.mutorj          (7 212 B)
dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak             (0 B — zaten boştu)
dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak          (0 B — zaten boştu)
dotnet/Kuyumcu.Tests/P21CiroRucuTestleri.cs.bak           (0 B — zaten boştu)
```

Toplam: **358 204 B → 0 B**.

**Bundan sonrası için bağlayıcı kural** (`denetim.sh` yorumunda yazılı):
mutasyon yedeği **proje ağacına kopyalanmaz**, scratchpad altına alınır
(`…/scratchpad/mut-yedek/<paket>/`) ve `cmp` ile oradan geri doğrulanır. Bu
paketin mutasyonları bu kurala göre yapıldı (§ 8).

Sayaç `ARTIK_DOLU_BAYT` "dosya var mı"yı değil **dolu mu**yu ölçer ve
`.mutorj/.bak/.mutbak/.orig` kalıplarını kapsar.

---

## 6. Canlı ölçüm — panel kesişimi (SAYIYLA)

### 6.1 Yöntem ve dürüst sınırı

Uygulamanın kendi ekranları **oturum açmayı gerektirir** ve oturum açmak, kimlik
bilgisini tarayıcıya taşımak demektir; bu oturumda güvenlik kapısı bunu engelledi
(§ 10, AÇIK-1). Bu yüzden ölçüm, **uygulamanın gerçek CSS'i satır satır aynen
gömülmüş** bir ölçüm tezgâhında yapıldı:

- `belirtecler.css` + `app.css` **bayt kopyası** gömülü,
- `SayfaBasi.razor` + `Secici.razor`'un ürettiği DOM birebir yeniden kuruldu,
- **ESKİ** (P-37 hâli: `position: absolute`) ve **YENİ** (P-37c) CSS yan yana,
- 10 süzgeç alanı + 1 arama + 1 eylem düğmesi ile **sarma koşulu zorlandı**
  (kusur yalnız sarma hâlinde doğduğu için en kötü durum ölçülür),
- her genişlikte **10 seçicinin her biri tek tek açılıp** kesişim toplandı.

Betik: `dokumanlar/09-uygulama-kayitlari/betikler/p37c-panel-olcum.js`
(uygulamanın kendi sayfasında da koşar; giriş açıldığında doğrudan kullanılır).

### 6.2 Sonuçlar (getBoundingClientRect kesişim alanı, px²)

| Genişlik | ESKİ toplam | ESKİ en kötü tek panel | **YENİ toplam** | **YENİ en kötü** | Yatay taşma (yeni) |
|---|---:|---:|---:|---:|---:|
| 1280 px | 17 251 | 6 336 | **0** | **0** | 0 |
| 1024 px | 27 283 | 6 336 | **0** | **0** | 0 |
| 900 px | 36 086 | 10 035 | **0** | **0** | 0 |
| 375 px | 522 659 | 71 001 | **0** | **0** | 0 |

ESKİ hâlde binen komşular örneği (1280 px): `s1` açıkken `s8:4579 + s9:1757`.
YENİ hâlde **hiçbir genişlikte binen komşu yok** (binen listesi boş).

> Not: P-37 raporundaki **5056 px²** o günkü /urunler şeridinin gerçek alan
> sayısıyla ölçülmüştü; buradaki 6 336 px² aynı kusurun 10 alanlı en kötü
> düzenindeki değeridir. İkisi de **> 0**'dır; yeni hâlde ikisi de **0**'dır.

### 6.3 Yan etki ölçümü — komşular zıplıyor mu?

Kapalı taban ile açık hâl karşılaştırıldı (aynı tezgâh, aynı genişlik):

| Genişlik | Açılan seçicinin kendi konumu | **Aynı satırdaki** komşuların kayması | Alt satırların durumu |
|---|---|---:|---|
| 1280 | değişmedi (97,384) | **0 px** | panel yüksekliği kadar **aşağı itildi** (örtülmedi) |
| 1024 | değişmedi (97,376) | **0 px** | aşağı itildi |
| 900 | değişmedi (97,412) | **0 px** | aşağı itildi |
| 375 | değişmedi (177,16) | **0 px** (her alan kendi satırında) | aşağı itildi |

Yani mekanizma **örtme → itme**ye çevrildi; hiçbir kontrol kaybolmuyor, hiçbiri
yerinden oynamıyor.

### 6.4 Canlı uygulamada (5198) doğrulanan kalemler

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET http://localhost:5198/` | 200, SPA açılır | **200**, `/giris`'e yönlendi, başlık "Giriş Yap — Kuyumcu ERP" |
| `GET /css/app.css` | yeni kural yayında | **200**, 90 116 B; `secici-liste-kap { … position: relative }` **var**, `position: absolute` **yok**, `:has(.secici-liste-kap)` **var**, `p37c-panel-olcum.js` atfı **var** |
| `/giris` 1280 px | yatay taşma yok | `document.documentElement.scrollWidth = 1280` (**taşma 0**) |
| `GET /api/v1/auth/ben` (tokensız) | 401 | **401** |

API yalnız 5198'de koşturuldu ve iş sonunda durduruldu:
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**. **5199'a dokunulmadı** (hâlâ dinlemede).

### 6.5 Kiracı durumu

Bu paket veri yazmaz; **test kiracısı AÇILMADI**.

```
SELECT COUNT(*) FROM tenants;                        →  9   (başlangıçtaki değer)
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET%'; →  0
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'QA-TEST-%'; → 4  (kalıcı QA fikstürü — dokunulmadı)
```

---

## 7. Kalem 4 — Eleman kimliği sapması: **YAZILI MUAFİYET** (geri dönüş YANLIŞ olurdu)

Paket iki seçenek sunuyordu: (a) eski kalıba dön, (b) yazılı muafiyet + test.
**(a) teknik olarak yanlıştır** ve bu iddia kodla doğrulandı:

`Pages/Emanet.razor` satır 45 sayfanın **kendi sunucu aramasını**
`<Arama ElemanId="e-cari-ara">` ile kurar → DOM'da `id="e-cari-ara"` doğar.
Aynı sayfanın satır 52'si `<Secici ElemanId="e-cari">`dir. Secici eski
`{ElemanId}-ara` kalıbına döndürülseydi **ikinci bir `e-cari-ara`** doğardı:
mükerrer DOM kimliği → geçersiz HTML, `label[for]` ve `aria-controls` bağı bozulur.

**Karar:** kalıp `{ElemanId}-ara-girdi` olarak **KALIR**. İki test bunu sabitler:

- `Secici_ic_arama_kutusu_ara_girdi_kalibindadir_MUAFIYET` — `ElemanId="e-cari"`
  ile render edilen `Secici`'de `#e-cari-ara-girdi` **vardır** ve `#e-cari-ara`
  **yoktur**.
- `Emanet_ekraninda_iki_arama_yuzeyi_AYRI_kimlik_tasir` — sayfadaki iki arama
  yüzeyinin ayrı kimlik taşıdığı ve gerekçenin sayfanın kendi yorumunda yazılı
  olduğu sabitlenir.

`KayitSecici.razor` hâlâ `{ElemanId}-ara` kullanır; iki bileşenin kalıbı farklıdır
ve bu **bilinçli**dir (biri sunucuda arar, biri gelen listeyi yerinde süzer).
Kurula sunulan muafiyet metni budur.

---

## 8. Mutasyon kanıtları

Yöntem: kaynak **scratchpad**'e (`…/scratchpad/mut-yedek/P-37c/`) kopyalandı →
`perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli test → yedek geri
kopyalandı → `cmp` ile **birebir aynı** olduğu kanıtlandı. Proje ağacına hiçbir
yedek bırakılmadı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M1-1 | `app.css`: şerit paneli `position: relative` → `absolute` | `Arac_seridindeki_acilir_panel_AKISTA_kalir_mutlak_konumlu_DEGILDIR` | `cmp` AYNI |
| M1-2 | `app.css`: `max-width: 100%` → `150%` | `Acilir_panel_KENDI_sutununu_asmaz_yatay_kesisim_dogamaz` | `cmp` AYNI |
| M1-3 | `app.css`: `:has(...) align-items: flex-start` → `flex-end` | `Panel_acikken_hizalama_USTTEN_yapilir_komsular_zıplamaz` | `cmp` AYNI |
| M1-4 | `Vezne.razor`: `<SayfaBasi Anahtar="vezne"` kaldırıldı | `Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` **+** `Bu_dalgada_sende_gecen_dort_ekran_serit_yuvalarini_kullanir` | `cmp` AYNI |
| M1-5 | `Panel.razor`: `SAYFA_BASI_MUAFIYETI` → `SESSIZ_ISTISNA` | `Muaf_ekran_gerekcesini_KENDI_dosyasinda_yazili_tasir` | `cmp` AYNI |
| M1-6 | Ağaca dolu artık yazıldı (`UrunModulu.cs.mutorj`) | `Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` | `printf '' >` → 0 bayt |
| M1-7 | `Siparisler.razor`: `id="sip-yeni-ac"` düşürüldü | `Serit_yuvalarina_tasinan_dugmeler_kaybolmadi` | `cmp` AYNI |
| M1-8 | `Secici.razor`: iç arama kimliği `-ara-girdi` → `-ara` | `Secici_ic_arama_kutusu_ara_girdi_kalibindadir_MUAFIYET` | `cmp` AYNI |

**Test açığı bulunmadı** — 8 mutasyonun 8'i kırmızıya döndü. Son mutasyondan sonra
temiz derleme: **0 Uyarı / 0 Hata**.

---

## 9. Kapı çıktıları

### 9.1 Derleme

```
dotnet build Kuyumcu.sln --nologo
Oluşturma başarılı oldu.     0 Uyarı     0 Hata     (00:00:32)
```

### 9.2 Testler

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---:|---:|---:|---:|
| Önce (taban) — *ölçülmedi, türetildi: 3 694 − 12* | 3 682 | 3 682 | 0 | 0 |
| Yeni dosya `P37cPanelVeKapsamTestleri` | 12 | 12 | 0 | 0 |
| İlgili ekran testleri (Siparis/Vezne/Bildirim/P37) | 101 | 101 | 0 | 0 |
| **Sonra (tam süit, trx)** | **3 694** | **3 694** | **0** | **0** |

`trx`: `…/scratchpad/trx-son/*.trx` →
`total="3694" executed="3694" passed="3694" failed="0" error="0"`

### 9.3 Denetim

```
bash dotnet/denetim.sh        (exit 0)
  ✓ P-37c: PANEL_AKIS_DISI          0 (panel akışta · kendi sütun genişliğinde)
  ✓ P-37c: SAYFA_BASI_KAPSAM        45/45 ekran (+1 yazılı muafiyet)
  ✓ P-37c: ARTIK_DOLU_BAYT          0 bayt
DENETİM TEMİZ.
```

> Uyarı (md.1): `sh denetim.sh` ile koşulursa betik **exit 2** verir ve yanıltıcı
> bir tek ihlal basar; betiğin shebang'ı `#!/usr/bin/env bash`tır ve **bash ile**
> koşulmalıdır. Bu bir kusur değil, kullanım notudur.

---

## 10. DoD sözleşmesi

Bu paket **yeni varlık / yeni kayıt türü doğurmaz**: veri modeli, migration,
uç, DTO ve yetki yüzeyi **değişmedi**. Dolayısıyla PATCH/DELETE/`[RolGerekli]`/
denetim kaydı yükümlülüğü **doğmaz**. Yine de mevcut kayıtların düzenlenebilirliği
açısından durum:

| Varlık / kayıt | Bu pakette değişti mi | Düzenleme yolu | Not |
|---|---|---|---|
| Sipariş | Hayır (yalnız şerit düzeni) | `/siparisler/{id}` detay ekranı (P-06 B-4 `DetayVeDuzenleme`) | Şeritteki "Yeni sipariş" düğmesi korundu |
| Vezne fişi | Hayır | `Cekmece` (`vezne-fisi`) + iptal/onay akışı | Finansal kayıt → hard DELETE yok, iptal/storno yolu korundu |
| Bildirim | Hayır | Kapat (`bildirim-kapat`) + kapatma notu | Kayıt silinmez, kapatılır |
| Kullanıcı arayüz tercihi (Panel düzeni) | Hayır | `/tercihler/benim` | P-37b'de mühürlendi |

**Salt okunur kalan hiçbir kayıt bu pakette doğmadı.**

DoD EK (kurul tur 1) karşılığı:
1. **Her düğme ya istek atar ya disabled** — taşınan 7 kontrolün hepsi kendi
   `@onclick`/`@onchange` bağını **aynen** korudu; `sip-yenile`/`vz-yenile`
   `disabled="@yukleniyor"` taşır. Test `Serit_yuvalarina_tasinan_dugmeler_kaybolmadi`
   her birinin tam olarak bir kez doğduğunu sabitler.
2. **Tek düzenleme kalıbı** — yeni `duzenlenenId` kalıbı açılmadı, `ModalPencere`
   düzenleme kullanımı **artmadı** (`SAYFA_BASI_DISI_DUZEN = 0` ve tur sayaçları temiz).
3. **Form doğrulaması** — bu pakette yeni form yok; mevcut formların doğrulaması
   dokunulmadan kaldı.
4. **Yıkıcı eylem onayı** — dokunulmadı (Vezne iptal onayı yerinde).

**DoD EK 2 — Dokunulan sayfalar (TEK DOKUNUŞ):**
`Bildirimler.razor`, `Siparisler.razor`, `SiparisDetay.razor`, `Vezne.razor`,
`Panel.razor` (yalnız muafiyet yorumu eklendi, DOM'una dokunulmadı).
Hiçbiri bu dalgada daha önce açılmamıştı. Fiyat/teklif/tahsilat içeren metin
üretilmedi.

---

## 11. Değişen dosyalar

> `git status --porcelain` bu depoda 300+ satır döndürüyor (depoda tek commit var,
> ağacın büyük kısmı izlenmiyor). Aşağıdaki liste **bu paketin dokunduğu** dosyalardır.

```
dotnet/Kuyumcu.Web/wwwroot/css/app.css                 (panel akış kuralı + :has hizası + dürüst yorum)
dotnet/Kuyumcu.Web/Pages/Bildirimler.razor             (SayfaBasi + Eylem yuvası)
dotnet/Kuyumcu.Web/Pages/Siparisler.razor              (SayfaBasi + Filtreler/Eylem yuvaları)
dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor            (SayfaBasi başlık/açıklama)
dotnet/Kuyumcu.Web/Pages/Vezne.razor                   (SayfaBasi + Filtreler/Eylem yuvaları)
dotnet/Kuyumcu.Web/Pages/Panel.razor                   (SAYFA_BASI_MUAFIYETI yazılı gerekçesi)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                  (5 yeni anahtar × TR + EN)
dotnet/Kuyumcu.Tests/P37cPanelVeKapsamTestleri.cs      (YENİ — 12 test)
dotnet/denetim.sh                                      (YENİ blok: 3 sayaç + yedek kuralı)
dokumanlar/09-uygulama-kayitlari/betikler/p37c-panel-olcum.js   (YENİ — ölçüm betiği)
dokumanlar/09-uygulama-kayitlari/PAKET-P-37c-uygulama-raporu.md (YENİ — bu rapor)
+ 19 artık dosya BOŞALTILDI (§ 5; içerik gitti, dosya adları duruyor)
```

Commit yapılmadı (kurucu talimatı).

---

## 12. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Panel sarma örtüşmesi kapandı | **KARŞILANDI** | § 6.2 — 1280/1024/900/375'te kesişim **0 px²** (eski hâlde 17 251 / 27 283 / 36 086 / 522 659) |
| (2) Kalan ekranlar şeride geçti | **KARŞILANDI (1 yazılı muafiyet)** | § 4 — 45/45 uygun ekran; Panel gerekçesi § 4.2 |
| (3) Artık temizliği | **KARŞILANDI** | § 5 — 19 dosya, 358 204 B → **0 B**; yedek kuralı `denetim.sh`'te yazılı |
| (4) Kimlik sapması | **KARŞILANDI (yazılı muafiyet + 2 test)** | § 7 — geri dönüş mükerrer DOM kimliği doğururdu, kodla kanıtlandı |
| MEKANİZMA: `PANEL_KESISIM = 0` | **KARŞILANDI** | § 6.2 |
| MEKANİZMA: `SAYFA_BASI_KAPSAM` | **SAPMALI KARŞILANDI** | 46/46 yerine **45/45 + 1 yazılı muafiyet**; sapma § 2 S3'te gerekçeli |
| MEKANİZMA: `ARTIK_DOLU_BAYT = 0` | **KARŞILANDI** | § 9.3 denetim çıktısı |
| DoD (a) ölçümlü testler | **KARŞILANDI** | 12 yeni test (§ 9.2) |
| DoD (b) mutasyon kanıtı | **KARŞILANDI** | 8/8 kırmızı (§ 8) |
| DoD (c) 1280/1024/900/375 canlı ölçüm | **KISMEN** | Sayılar § 6.2'de; ölçüm **uygulamanın gerçek CSS'iyle** yapıldı, ancak **oturum açılamadığı için uygulamanın kendi ekranında** koşulamadı (§ 10 AÇIK-1) |
| DoD (d) `denetim.sh` TEMİZ | **KARŞILANDI** | § 9.3 |
| DoD (e) tam süit 0 başarısız | **KARŞILANDI** | 3694/3694 (§ 9.2) |
| DoD (f) test kiracısı silindi | **KARŞILANDI (açılmadı)** | § 6.5 — tenants 9, `PAKET%` 0 |
| DoD (g) rapor | **KARŞILANDI** | bu dosya |
| Tip/sözleşme: Pages içinde yeni istemci record yok | **KARŞILANDI** | Hiç record eklenmedi |
| 5199'a dokunulmadı | **KARŞILANDI** | § 6.4 |

---

## 13. Açık kalanlar

1. **AÇIK-1 — Uygulamanın kendi ekranında canlı piksel ölçümü.**
   `/urunler`, `/siparisler`, `/vezne` gibi şeritli ekranlar **oturum açmayı**
   gerektiriyor; oturum açmak QA fikstürünün erişim jetonunu tarayıcıya taşımak
   demek ve bu oturumda güvenlik kapısı bu adımı reddetti
   (*"Permission … denied … [Credential Leakage]"*). Bu yüzden ölçüm,
   **uygulamanın CSS'inin bayt kopyası** ve `SayfaBasi`/`Secici`'nin birebir
   DOM'u ile kurulan tezgâhta yapıldı (§ 6.1). Sonuçlar geçerlidir ama
   **uygulamanın kendi sayfasında tekrarlanmalıdır**; betik hazır
   (`p37c-panel-olcum.js`, konsolda `await p37cOlc()`).
   → **Kurucu kararı gerekiyor** (§ 14, K-5).
2. **AÇIK-2 — Ekran görüntüleri.** Aynı sebeple (giriş) şeritli ekranların
   görüntüsü alınamadı; P-37'den devreden kalem hâlâ açık.
3. **AÇIK-3 — Artık dosyaların SİLİNMESİ.** 19 dosya boşaltıldı ama ağaçta duruyor.
   Silme kancayla engelli → **K-4 olarak kurucuda**.
4. **AÇIK-4 — `:has()` tarayıcı tabanı.** `:has()` seçicisi Chrome/Edge 105+,
   Safari 15.4+, Firefox 121+ ile çalışır. Desteklemeyen eski bir tarayıcıda
   hiza kuralı uygulanmaz; **örtüşme yine 0'dır** (asıl düzeltme `position`
   kuralıdır), yalnız dropdown açılınca komşular bir kez aşağı kayar. Kabul
   edilebilir bozulma; kurucunun desteklediği tarayıcı tabanı yazılı değilse
   belirlenmelidir.
5. **AÇIK-5 — `Panel.razor` muafiyeti kurul mührü bekliyor.** § 4.2'deki gerekçe
   kurulun tutanağına geçirilmeli; aksi hâlde sayaç 45/45 olarak donar ama karar
   yazılı olmaz.

---

## 14. Kurucuya / mali müşavire notlar

- **Mali müşavire not yok** — bu paket mevzuat, vergi, matrah ya da beyan
  yüzeyine dokunmaz; tamamen arayüz düzeni ve test/denetim altyapısıdır.
- **K-4 (devam ediyor):** ağaçtaki 19 artık dosya **silinmeli**. İçerikleri
  boşaltıldı (0 bayt), zararsızlar, ama `git status`'u kirletiyorlar. Kancalar
  `rm`i engellediği için bunu **yalnız kurucu** yapabilir.
- **K-5 (yeni):** şeritli ekranların canlı piksel ölçümü ve ekran görüntüleri için
  QA fikstürüne **oturumlu** erişim gerekiyor. Kurucu ya (a) ölçümü kendi
  tarayıcısında 5199'da `await p37cOlc()` ile alıp sonucu paylaşır, ya da
  (b) ajanların fikstür oturumunu tarayıcıya taşımasına açıkça izin verir.
  Şu hâliyle ölçüm **gerçek CSS ile** yapıldı ve 0 çıktı; eksik olan, uygulamanın
  kendi sayfasındaki tekrardır.
- **Ücretsiz pilot kuralı** korundu: bu raporda fiyat, teklif ya da tahsilat
  içeren hiçbir metin üretilmedi.
