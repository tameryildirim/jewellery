# PAKET YILDIZ-KATMAN — uygulama raporu (11.10.2026)

Kapsam: Yıldız'da ortak katmanlar (ModalPencere · Cekmece · perde · sayfa başlığı) tuval dilinde + 5A kalanları
(üst menü seçili öğe, Ürün ağacı adları, Etiketler "Ürün seçin", denetim Y17/O16 eski başlıklar).
Hasımsız paket — kanıt bu rapordaki ölçümlerdir. Para/veri/yasal davranışa dokunulmadı. Migration yok. COMMIT YOK.

Ortam: API 5160, vekil 5161, CDP 9701–9708, `KUYUMCU_TEST_DB=kuyumcu_test_t1`. Kiracı
`PAKETYILDIZKATMAN-TEST Tunalılar Kuyumculuk` (01M4KV2B7Z8D88Q26134APCP3E), `demo-tohumla.js --prova-kiraci --sema yildiz`
(6 cari, 12 ürün, 9 satış). Sistem anahtarı oturumluk rastgele üretildi (dosyası boşaltıldı), hiçbir sır çıktıya yazılmadı.

## TUR 2 (11.10.2026 03:00–05:00) — tasarım hakemi KRİTİK kapatıldı: katman eylem satırı TEK KALIP

**Bulgu (hakem):** çekmece/pencere birincil-ikincil düğmesi tuval .b1/.b2 değildi ve açıcıdan açıcıya dört farklı kalıp vardı
(`btn btn-birincil esnek` 89×44 · `dugme dugme-birincil` 89×44 · Emanet 339×50 esneyen şerit, Vazgeç solda · `esit-pay` 217×44;
ikincil metin "İptal"/"Vazgeç" karışık).

**Taban ölçümü (değişiklikten önce, kendi aracımla, BASIT 1440 koyu, 17 rota):** 12 katman · 12'si düğme kalıbı dışı · 4 imza
(b1 44 px dolgu 16 · b1 50 px · b2 44 · b2 50); Emanet "Vazgeç" Kaydet'in solunda; 8 katmanda ikincil metni "İptal"
(`ham/yildiz-katman/tur2/taban-katman-BASIT-1440-koyu.jsonl`).

### T2.1 Yapılanlar
1. **`tema-yildiz.css` YILDIZ-KATMAN bloğu — "KATMAN EYLEM SATIRI" (7 kural):** satır = katmandaki birincil/tehlike düğmesinin
   ebeveyni (alt şerit, gövde içi yapışkan satır, Emanet `.emanet-cekmece-alt`; `form/fieldset/.form-dikey/gövde` satır sayılmaz).
   Tuval form kalıbı (Y_Isletme · Y_Yasalsinirlar · Y_Veridisaaktar: `display:flex; align-items:center; gap:40px` + `a.b1` + `a.b2`):
   b1 hap 56 px · dolgu 0 32 · 16/500/.02em · satır yüksekliği 1,45 · yazı genişliğinde · SOLDA; b2 altı çizili 56 px · dolgu 0 4 ·
   16/400/.02em (tuval b2 ağırlığı kökten miras 400) · saydam · köşe 0; 40 px ara. `esnek` / `esit-pay` / satır içi `flex:1`
   Yıldız'da nötr (`flex: 0 0 auto !important`). Tehlike (Pasife al) aynı hap ölçüsünde, rengi YILDIZ-TIPO tehlike dili (saydam + --tehlike).
   Tezgâh (`.yz-tezgah`) dışarıda — YILDIZ-4C / TEZGAH-ODEME kalıbı korunur.
2. **Emanet iade et satırı:** öteki çekmecelerin alt şeridiyle aynı yer ve çizgi (kenardan kenara kıl çizgi, 24 px dolgu, Kaydet
   panel dibinden 80 px yukarıda — Emanet 820–876 = Yeni grup 820–876, 1440×900).
3. **İkincil metin "Vazgeç" (tuval b2 metni):** `Bilesenler/Cekmece.razor` — `IptalMetni` verilmediyse Yıldız'da
   `YildizKatmanSozlugu.Vazgec` (TR "Vazgeç" / EN "Cancel"), öteki şemalarda `Ortak.Iptal` AYNEN. Sayfaların ModalPencere/Cekmece
   içindeki 31 ikincil düğmesi (17 sayfa) `<Kuyumcu.Web.Bilesenler.Yildiz.YildizVazgec />` kullanır — yeni bileşen yalnız METİN
   düğümü basar (sarmalayıcı yok): Yıldız'da "Vazgeç", öteki 8 şemada `Ortak.Iptal` ("İptal") birebir (YK_42b markup eşitliği).
   Kimlikler, sınıflar, olaylar AYNEN; düğmelerin çoğunun kimliği zaten `*-vazgec` idi.
4. **Ölçüm aracı:** `betikler/tur/yildiz-katman-tara.mjs` — her katmanın eylem satırı ölçülür (boy, dolgu, yazı, ağırlık, köşe,
   zemin, alt çizgi, esneme = genişlik − yazı genişliği, sıra, ikincil metin), kural dışı düğme KIRMIZI sayılır; koşum sonunda
   "DÜĞME İMZALARI" dağılımı basılır (tek kalıp = her rolde TEK imza).

### T2.2 Gerçek başsız Chrome ölçümü — SON KOD
| Koşum | Ekran | Açıcı | Katman | KIRMIZI | Düğme kalıbı dışı | Düğme imzaları |
|---|---|---|---|---|---|---|
| Taban BASIT 1440 koyu (17 rota) | 17 | 21 | 12 | 12 | 12 | 4 farklı (44/50 px, dolgu 16) |
| Son BASIT (42 ekran × 1440+375 × koyu+açık) | 84 | 42 | 48 | **0** | **0** | **b1 48 × "56px 16/500 dolgu 32 --metin hap" · b2 48 × "56px 16/400 dolgu 4 saydam köşe 0"** |
| Son PRO (aynı) | 84 | 38 | 48 | **0** | **0** | aynı tek imza |
| Emanet + Yeni grup (Emanet düzeltmesinden sonra, BASIT+PRO) | 8 | 12 | 16 | 0 | 0 | aynı tek imza |

Açıcı başına genişlik (her mod, her tema): 1440 → Kaydet 119×56 · Vazgeç 65×56 · ara 40 px; 375 → Kaydet 119×56 x16 · Vazgeç 65×56 x175.
12 açıcı: Emanet Geri ver · Müşteriler Yeni müşteri · Ürünler Yeni ürün · Ürün ağacı Yeni grup · Satın alma Yeni sipariş · Kasalar
Hareket ekle · POS ekle · Banka Hesap ekle · Faturalar Yeni fatura · Uyum Yeni bildirim · Kullanıcılar Yeni kullanıcı / Düzenle.
Onay pencereleri ayrıca (1440+375 × koyu+açık): Kullanıcılar ⋯ → Şifre Değiştir (Kaydet 119×56 + Vazgeç 65×56) ·
Ürünler Tüm ayrıntılar ⋯ → Pasife al (Pasife Al 135×56 tehlike hapı + Vazgeç 65×56; satır içi `flex:1` nötr).

**Tuval kıyası (`tuval-kiyas.mjs`, Y_Isletme `a.b1` "Kaydet" / `a.b2` "Vazgeç" ↔ Yeni grup çekmecesi, 1440×900):**
koyu 2/2 AYNI · açık 2/2 AYNI (16px/500 ve 16px/400 · .02em · lh 23,2 · renk · 119×56 ve 65,5×56 · zemin/dolgu/köşe/alt çizgi;
konum "kalıp" — tuvalde çekmece yok). İlk koşumda lh 16 ↔ 23,2 FARK çıktı → `line-height: 1` → `1.45` düzeltildi (tuval kökü 18/1,45).
b1→b2 arası tuvalde 64+119→223 = 40 px, uygulamada 993+119→1152 = 40 px.

**Koruma (tuval-kiyas, 1440, koyu+açık):** Müşteriler 28/28 · 28/28 · Tezgâh 35/36 · 35/36 · Tüm ekranlar 66/67 · 66/67 — FARK'lar
tur 1 ile BİREBİR aynı öğe/değer (Tezgâh "ödeme1 kasa" lh 21,75↔18,16 `yasal.css`; Tüm `main` boyu veri). Bugün "hal" eşlemesi
tur 1'de de 7/7 bulunamayan (o tablonun seçicileri eski; bu paket Bugün'e dokunmuyor).

**Öteki 8 şema — FARK 0 (belirlenimci, `blokfark3.mjs`):** katman AÇIKKEN `data-sema` öteki şemaya alınır, TEK değerlendirmede
(geçiş/animasyon kapalı, zaman geçmeden) YILDIZ-KATMAN kuralları (45) yerinde ↔ silinmiş hâlde sayfadaki TÜM öğelerin 23 hesaplanmış
özelliği karşılaştırılır: 8 şema × 2 tema × 7 katman = **112 karşılaştırma · FARK 0**; denetim kolu Yıldız'da 27–68 öğe fark eder
(kurallar gerçekten etkili), geri koyma her seferinde birebir. (Tur 1'in "aç/kapa iki fotoğraf" yöntemi bu turda kararsız çıktı —
FARK 15 → 25, aynı kurallarla iki fotoğraf bile farklı; zamana bağlı değişim. Bu yüzden tek değerlendirmeli yönteme geçildi.)
Razor dalları: `YildizVazgec` öteki şemada çıktı = `"İptal"` (YK_42b, markup birebir), Cekmece "İptal" (YK_41b).

**Sıkışma (`sikisma-olc.js` + K15) — katman AÇIKKEN, 1280 + 375 · BASIT + PRO · koyu + açık:** 6 katman (Yeni grup, Emanet Geri ver,
Yeni kullanıcı, Yeni ürün, Hareket ekle, Yeni bildirim) × 8 = 48 ölçüm + Emanet düzeltmesi sonrası 8: K1 · K3 · K3c · K5 · K11 · K12 ·
K15 · yatay **hepsi 0**. K9 (üst üste binme) katman açıkken > 0 çıkar; ayrıştırıldı: katman DIŞI öğe ile katman içi öğe (çekmece arkadaki
sayfayı örter — beklenen) ve 12 ölçümde katman içi: Yeni ürün "Ziynet"/"Ayarı seçin." ve "En az bir rakam"/Kaydet = gövdenin kaydırma
kabının dışında KIRPILMIŞ içerik (alt şerit/yapışkan satır üstünü örter; kaydırınca görünür — K15 0, görüntüde doğrulandı).

**Görüntüler (Read ile bakılan 3):** `ham/yildiz-katman/tur2/pano-1440-koyu-basit.png` (5 çekmece yan yana: Yeni ürün · Yeni grup ·
Emanet iade et · Kasa hareketi · Yeni kullanıcı — her birinde Kaydet hapı solda, Vazgeç altı çizili sağda, aynı ölçü),
`pano-375-acik-pro.png` (aynı beşi 375 açık PRO), `urun-1280.png` (Yeni ürün 1280 koyu: kilit sebebi "Ayarı seçin." + soluk Kaydet + Vazgeç).

### T2.3 Testler
- Yeni: YK_40 (eylem satırı CSS sözleşmesi), YK_41 / YK_41b (Cekmece metni Yıldız/öteki), YK_42 / YK_42b (YildizVazgec + sözlük TR/EN),
  YK_43 (sayfalarda katman içinde çıplak `Ortak.Iptal` 0, YildizVazgec ≥ 31), YK_44 (Emanet satırı). `YildizKatmanTestleri` **47 → 54, 54/54 yeşil**.
- İlgili filtre (Cekmece|Modal|KabukBilesen|OrtakGorunum|Yildiz|Kullanici|Emanet|OnayKilit): 855/855; son CSS sonrası (Yildiz|Tasarim|Belirtec|Tema|Css|Emanet|Cekmece|Modal): 888/888.
- **Tam süit (bir kez, Emanet satırı kuralı ve YK_44 öncesi kodla):** 12623 · geçti 12617 · başarısız 3 · atlandı 3
  (trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-KATMAN-t2/_Mac_2026-10-11_04_21_52.trx`). Kırmızılar: `RaporDortTestleri.RD_KH01`, `RD_KH02`
  (tur 1 listesinde de vardı — rapor önbelleği) ve `YasalKapi2NakitSiniriEkranTestleri.YK3_09` (bUnit "olay kimliği 135 yok" yarış
  hatası; tek başına 3/3 koşumda YEŞİL; test dosyası bu paketin bileşenlerine dokunmuyor — kararsız, YASAL-KAPI şeridi). **Bu paketin yeni kırmızısı 0.**
  Tam süitten sonraki değişiklik yalnız CSS (Emanet satırı) + YK_44: CSS okuyan tüm sınıflar ayrıca koşuldu (888/888) — tam süit kuralı gereği ikinci kez koşulmadı.

### T2.4 Mutasyon (yedek → tek eşleşmeli perl → filtreli test → geri koy → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M11 | b1 `padding: 0 32px` → `0 16px` | YK_40 | AYNI |
| M12 | `flex: 0 0 auto !important` → `1 1 auto` (esneme geri) | YK_40 · canlı: tarayıcı "b2 esniyor +103, b1 esniyor +159" KIRMIZI | AYNI |
| M13 | b2 alt çizgisi silindi | YK_40 | AYNI |
| M14 | Cekmece `YildizMi ?` → `false ?` | YK_41 | AYNI |
| M15 | YildizVazgec koşulu → `false` | YK_42 | AYNI |
| M16 | Kullanıcılar Yeni kullanıcı ikincil metni çıplak `Ortak.Iptal`'e geri | YK_43 | AYNI |
| M17 | b1 `order: -1` → `0` (sıra) | YK_40 | AYNI |
| M18 | Emanet satırının kıl çizgisi silindi | YK_44 | AYNI |
Son mutasyondan sonra `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata.

### T2.5 Derleme · denetim · canlı · temizlik
- `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata** (son).
- `denetim.sh`: 315 ✓ · 2 ✗ — **P-30 DEMO_INSAN_SURESI** (kabulde hariç) ve **GIRDI-2 TEST_GECERSIZ_FIKSTUR 1** (MASAK doğrulayıcı şeridi; tur 1'de de vardı).
  Bu paketin sayaçları ✓: ModalPencere 48/55 (artış 0), satır içi style 24, SABIT_RENK 0, DIL-EKRAN 0. (Ara koşumda EmanetModulu.cs kaynaklı
  3 geçici ✗ görüldü — başka şeridin yarım işi, son koşumda yoktu.)
- Canlı: API 5160 (kendi PID'im, oturumluk rastgele sistem anahtarı — dosyası `t2/anahtar`, çıktıya yazılmadı), vekil 5161, CDP 9710–9714.
  Kiracı `PAKETYILDIZKATMAN2-TEST Tunalılar Kuyumculuk` (01M4M5FAPBFVCDGH0E0BQ3EBF0, `demo-tohumla.js --prova-kiraci --sema yildiz`: 6 cari, 12 ürün, 9 satış).
  Bu paket veri/para davranışına dokunmadı; canlı doğrulama = yukarıdaki tarayıcı ölçümleri (gerçek veriyle). Yeni uç/varlık yok → HTTP/SQL iş kuralı kanıtı gerekmedi.
- Temizlik: API + vekil yalnız kendi PID'leriyle durduruldu; 5160/5161/9710–9714 dinleyen yok. 5199/5200/5230/5241 dokunulmadı.
  `zsh kiraci-temizle.sh 01M4M5FAPBFVCDGH0E0BQ3EBF0 --uygula` rc 0 (75 tablo); SQL: kiracının `tenants` satırı 0, `hareketler` 0; QA-TEST-* 4 kiracı yerinde.
  `tenants` sayısı: başta 23, sonda 21 — benim kiracım +1/−1 (net 0); fark eşzamanlı şeritlerin kendi kiracıları (PAKET-DILBORC-TEST ve
  PAKETP-TEZGAH-TUR-KIMLIK-TEST DOG338d6d silinmiş, …932d52 eklenmiş; başlangıç listesindeki bir satır da arada başka şeritçe silinmiş olabilir) — onlara dokunmadım.

### T2.6 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — YILDIZ-KATMAN bloğu içinde "KATMAN EYLEM SATIRI" (7 kural) + Emanet satırı (2 kural)
- `dotnet/Kuyumcu.Web/Bilesenler/Cekmece.razor` — `IServiceProvider` enjeksiyonu, `YildizMi`, ikincil metin Yıldız'da "Vazgeç"
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizVazgec.razor` (YENİ)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.YildizKatman.cs` — `YildizKatmanSozlugu.Vazgec` (TR+EN)
- 17 sayfa, yalnız katman içi ikincil düğme METNİ (31 satır, `@Dil.T.Ortak.Iptal` → `<…YildizVazgec />`): Ayarlar(2) · BankaEkstre(1) ·
  Bildirimler(1) · Emanet(1) · Faturalar(3) · Gider(1) · GiderPusulalari(2) · Isletme(1) · Kasalar(2) · Kullanicilar(4) · Pos(1) · Sayim(1) ·
  SiparisDetay(2) · UrunAgaci(2) · Urunler(3) · Uyum(3) · Yedekler(1)
- `dotnet/Kuyumcu.Tests/YildizKatmanTestleri.cs` — YK_40–44
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/yildiz-katman-tara.mjs` — düğme ölçümü + imza dağılımı
**Dokunulan sayfalar (TEK DOKUNUŞ gerekçesi):** UrunAgaci tur 1'de de açıldı — gerekçe: hakem KRİTİK'i (ikincil metin tek kalıp) aynı
paketin 2. turu. Öteki 16 sayfa bu dalgada ilk kez ve yalnız tek satırlık metin bileşeniyle açıldı (Sayım/Ürünler: P-SAYIM/P-URUN
mantığına dokunulmadı — yalnız Yıldız görünüm metni).

### T2.7 Belirsiz eşleşme (tur 2 ekleri)
| # | Öğe | Tuvalde | Seçilen en yakın kalıp |
|---|---|---|---|
| 8 | Katman eylem satırı | Tuvalde çekmece yok | Y_Isletme form satırı birebir (b1 Kaydet solda, b2 Vazgeç sağda, 40 px) — tur 1 belirsizlik #4 (Tezgah4 "Vazgeç · Kaydet" sırası) tezgâh dışı tüm katmanlarda form sırasına bağlandı; Emanet de artık b1 önce |
| 9 | Tehlike düğmesi (Pasife al, iptal onayı) | yok | b1 hap ölçüsü + YILDIZ-TIPO tehlike rengi (saydam, --tehlike kenar ve yazı) |
| 10 | Kilit sebebi satırı ("Ayarı seçin.") | yok | alt şeritte düğmelerin üstünde kendi satırı (UX-S30; mevcut davranış) |
| 11 | Pasif (disabled) b1 | yok | soluk hap (opacity .45, `.yz` düğmeleriyle aynı) |

### T2.8 Açık kalanlar (sahibiyle)
1. `RaporDortTestleri.RD_KH01/RD_KH02` — rapor önbelleği şeridi (tur 1'den beri kırmızı).
2. `YasalKapi2NakitSiniriEkranTestleri.YK3_09` — bUnit olay kimliği yarışı, tam süitte kırmızı / tek başına yeşil (YASAL-KAPI şeridi; testte `cut.Find(...).Click()` yeniden bulunmalı).
3. denetim `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (MASAK doğrulayıcı şeridi), `P-30` (kabulde hariç).
4. Tezgâh çekmeceleri (`.yz-tezgah`) bu kalıbın dışında bırakıldı (TEZGAH-ODEME şeridi; Tezgah4 tuvali kendi çekmece dilini çiziyor).
5. Katman açıkken `sikisma-olc.js` K9, çekmecenin örttüğü arka sayfa metnini de "binen" sayar — ölçere "katman açıkken yalnız katman içi" kipi eklenmesi önerilir (bu turda ayrı hesaplandı).
6. `tuval-kiyas.mjs` tarayıcı önbelleğini kapatmıyor (CSS değişikliği sonrası eski stil ölçüldü; önbellek temizlenerek yeniden koşuldu) — `Network.setCacheDisabled` eklenmesi önerilir.

### T2.9 DoD sözleşmesi
Yeni varlık/uç/migration yok. ModalPencere/Cekmece DOM'u, kimlikleri, geri çağrıları AYNEN; değişen yalnız Yıldız görünümü ve Yıldız'daki
ikincil düğme metni. Kilitli düğme sebebi (UX-S30) korunur; her düğme gerçek geri çağrı taşır.

---

# TUR 1 kaydı (aşağısı tur 1 raporudur — değişmedi)

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)

| İddia (görev/5A) | Kodda bulunan | Karar |
|---|---|---|
| "Sayım 'Barkod okut' eski sayfa", "Emanet al/Yeni iş/Yeni liste eski forma düşüyor" | 5A tur 2'de kapatılmış (YildizForm, YildizSayim okutma yuvası) | Canlı doğrulandı: dördü de katmansız Yıldız yüzünde açılıyor (tarama `acici-katmansiz`, eski öğe 0). Yeni kod gerekmedi. |
| ModalPencere/Cekmece eski tema | Ortada yüzen 512 px kart, köşe 16 px, siyah/lacivert perde `rgba(0,0,0,.4)`/`rgba(15,23,42,.45)`, gölge, başlık 28/500 (taban ölçümü) | Düzeltildi (§ 2.1). |
| Üst menüde seçili öğe yok | `YildizKabuk.Aktif` yalnız 5 kelimenin kendi rotası; 42 ekranın 37'sinde seçim yoktu (taban taraması) | Tuvalin kendi kuralı ölçüldü: her `Y_*.dc.html` `a.on` = ekranın MENÜ GRUBU (Günlük iş→Bugün, Müşteri ve stok→Müşteriler, Para ve banka→Kasa, Belge/Rapor/Yönetim→Tüm ekranlar). Birebir uygulandı. |
| O16 "Sayım, Kasalar, Kur, Hurda, Kasa raporu 40 px — 64 olsun" | Tuvalde bu ekranlar (ve Kâr-zarar, Satış analizi, Uyum) 40/300 özet kalıbıdır | **Tuval kazanır**: 40 px korunur; O16 tuvale aykırı olduğu için uygulanmadı. |
| Paket notu okunurluk kararı (≥300) | Kurucu 09.10 iptal etti | Başlık h1 tuvaldeki 200 (`!important` ile YILDIZ-TIPO T2'nin 300'ünü yalnız bu başlıkta geçer). |

## 2. Yapılanlar

### 2.1 Ortak katman — `tema-yildiz.css` `/* YILDIZ-KATMAN */` bloğu (yalnız `html:root[data-sema="yildiz"]`, `.yz` yüzleri hariç)
- **Tek kalıp sağ çekmece:** `.modal-arka` sağa yaslı (`justify-content:flex-end; align-items:stretch`), `.modal-panel` tam boy, köşe 0, gölge yok, sol 1 px `--ayrac`, zemin `--yuzey-govde`; `.cekmece` aynı. Genişlik her pencerenin kendi max-width'i (32rem; fatura kalem tablosu 84rem); ≤768 tam ekran. Sürüklenebilir pencere (Cariler ayrıntı) taşınmaz.
- **Perde saydam** (eski lacivert/siyah perde yok).
- **Baş:** 22/300/−0,01em `--metin`, kıl çizgi; kapat soluk işaret. **Gövde** 24/32 dolgu (≤768: 16).
- **Alt şerit:** zemin `--yuzey-govde`, üstte kıl çizgi; tuval form kalıbı sırası **b1 önce, b2 sonra**, sola yaslı, 40 px ara (DOM sırası AYNEN, yalnız `order`).
- **Altlığı olmayan pencere** (Yeni müşteri PRO 1665 px form / 825 px gövde; Yeni kullanıcı 375): birincil düğmeli eylem satırı gövdenin dibine yapışır (`sticky; bottom:-24px`) — Kaydet ilk görünümde ekranda; satır içinde de b1 önce.
- Katman içi `details` (Yeni fatura "Fatura ayrıntıları") köşe 0 / dolgusuz; `fieldset` (Yeni müşteri PRO "Kimlik ve ticari bilgiler") tarayıcının oluklu çerçevesi yerine kıl çizgi + `.ust` başlık; seçim kutusu dolgusuz.

### 2.2 Sayfa başlığı (Y17/O16) — `Layout/UstBar.razor` Yıldız dalı + `Sozlukler.YildizKatman.cs`
Kendi Yıldız yüzü olmayan 20 ekranda (Faturalar, İrsaliyeler, Uyum, Yasal sınırlar, Rapor merkezi, Kâr-zarar, Vade takibi, İşlem defteri, Satış analizi, Kullanıcılar, İşletme, Yetkiler, Kim ne yaptı, Kurulum, Veri dışa/içe aktar, Yedekler, Kılavuz, Ayarlar, Satın alma) tuvalin `<main>` başlığı: üst satır (12/500/.22em büyük harf soluk, metin tuvaldeki AYNEN) + ince h1 64/200/−0,035em (özet kalıbı 40/300, üst dolgu 64, ara 24). Kur saati üst menüde yazdığı için bu şeritte ikinci kez çizilmez (Yıldız yüzlü ekranlarla aynı kural); Yardım bağlantısı `.ust` kalıbında kalır. Tek `<h1>`; öteki 8 şemada dal çizilmez.

### 2.3 Üst menü seçili öğe — `Layout/YildizKabuk.cs` (`SeciliKimlik`, `BaslikBoyu`) + `Bilesenler/Kabuk/YildizUstMenu.razor`
Doğrudan eşleşme (Tezgâh kendi sözcüğü) → yoksa en uzun önekli menü öğesinin grubu → yoksa "Tüm ekranlar". Grubun sözcüğü yetkiden görünmüyorsa "Tüm ekranlar". Her ekranda tam bir `aria-current="page"`.

### 2.4 5A kalanları
- **Ürün ağacı** (`Pages/UrunAgaci.razor`, yalnız `YildizMi` dalı): düğme "Yeni grup" iken çekmece "Yeni kategori · Kategori adı · Üst kategori" diyordu (UX-S31). Yıldız'da "Yeni grup · Grup adı · Üst grup" ve döngü uyarısı "Bir grup kendi alt grubunun altına taşınamaz." Öteki şemalar AYNEN.
- **Etiketler** (`Bilesenler/Yildiz/YildizEtiketler.razor`): ürün yokken "Ürün seçin" tıklanmayan yazıydı; seçim yalnız "Başka ürün ekle"den açılıyordu. Artık aynı kimlikli (`#yildiz-etiket-urun-deger`) düğme, aynı geri çağrı (`SecimDegistir`), `aria-expanded/controls`; görünüm tuval `.k` değeri + b2 alt çizgisi.

### 2.5 Ölçüm araçları (kalıcı)
- `betikler/tur/yildiz-katman-tara.mjs` (YENİ): her ekranda güvenli açıcılara basar, açılan katmanı ölçer — tip (SAĞ-ÇEKMECE / SERBEST-YÜZEN), panel zemini/köşe/gölge, perde, katman içi eski öğe (gölge, köşeli dolu kart, belirteç dışı zemin), Inter dışı aile, etkileşim sonrası görünürlük, üst menü seçili öğe sayısı.
- `betikler/sikisma-olc.js`: **K15 etkilesimGorunurluk** eklendi (açık katmanda overflow:hidden atasınca kırpılan ya da kaydırılamaz biçimde ekran dışında kalan metin/düğme + ilk görünümde ekran dışı Kaydet).

## 3. Gerçek (başsız) Chrome ölçümleri — SON KOD

### 3.1 Katman taraması (`yildiz-katman-tara.mjs`, 42 ekran × 1440+375 × koyu+açık)
| Koşum | Ekran ölçümü | Açıcı | Katman ölçümü | KIRMIZI | Üst menü seçili yok | K15 |
|---|---|---|---|---|---|---|
| Taban (değişiklikten önce, BASIT 1440 koyu) | 42 | 15 | 8 | **8** (yüzen 4, eski 4) | **37** | 0 |
| Son · BASIT | 84 | 42 | 48 | **0** | **0** | 0 |
| Son · PROFESYONEL | 84 | 38 | 48 | **0** | **0** | 0 |

Katman açan açıcılar (her biri 1440+375 × koyu+açık): Emanet "Geri ver" · Müşteriler "Yeni müşteri" · Ürünler "Yeni ürün" · Ürün ağacı "Yeni grup" · Satın alma "+ Yeni sipariş" · Kasalar "Hareket ekle" · POS "POS ekle" · Banka "Hesap ekle" · Faturalar "+ Yeni fatura" · Uyum "+ Yeni bildirim" · Kullanıcılar "+ Yeni kullanıcı", "Düzenle" → hepsi SAĞ-ÇEKMECE, köşe 0, gölge 0, perde saydam, Inter, eski öğe 0. Katmansız açılanlar (Yıldız form yüzü): Emanet al, Yeni iş, Yeni liste, Fiyat Düzenle, Etiket seçimi, Gider Ayrıntı ekle, Rapor Ayrıntı.
Ara turda bulunan ve düzeltilen: PRO Yeni müşteri Kaydet ekran dışı (yapışkan satır), 375 Yeni kullanıcı Kaydet ekran dışı, Yeni fatura "Fatura ayrıntıları" köşeli kutu.
Kayıtlar: `ham/yildiz-katman/katman-BASIT.jsonl`, `katman-PRO.jsonl`.

### 3.2 Tipografi taraması (`yildiz-tipo-tara.mjs --katman 1`, 15 ekran, BASIT, 1440+375, koyu+açık)
Sayfa 60 · **katman 190** · düğüm 5716 · kural dışı 4 — dördü de sayfa (katman değil): `/pos` ve `/banka` 375'te boş durum cümlesi 32 px (YILDIZ-5B'nin `#yildiz-pos-bos-cumle`, `#yildiz-banka-bos-cumle`). **Katmanlarda kural dışı 0.**

### 3.3 Sıkışma (`sikisma-olc.js` + K15), 1280 + 375 · BASIT + PRO · koyu + açık
6 ekran (Kullanıcılar, Uyum, İşlem defteri, Yetkiler, Ürün ağacı, Etiketler) × 2 genişlik × 2 tema × 2 mod = 48 ölçüm: K1 · K3 · K3c · K5 · K9 · K11 · K12 · K15 · yatay **hepsi 0/48** (`ham/yildiz-katman/sik.out`).

### 3.4 Tuval kıyası (`tuval-kiyas.mjs`, 1440×900, BASIT, navKaynak = Amblem_sol(_acik) — amblem kararı, 5A ile aynı yöntem)
| Ekran | koyu · açık | FARK gerekçesi |
|---|---|---|
| Kullanıcılar (yeni başlık; menü seçili · üst satır · h1) | 3/3 · 3/3 | — |
| İşlem defteri (yeni başlık) | 3/3 · 3/3 | — |
| Uyum (yeni başlık, özet 40) | 1/3 · 1/3 | yalnız blok GENİŞLİĞİ (tuval 631 = iki sütunlu ızgaranın sol sütunu; uygulamada gövde henüz Yıldız yüzü değil). Yazı boyu/ağırlık/aralık/renk/x/y AYNI |
| 5A: Ürünler, Ürün ağacı, Etiketler, Emanet, Sipariş, Fiyat, Sayım (menü · üst satır · h1) | 3/3 · 3/3 (her biri) | — (menü seçili artık tuvaldeki gibi VAR) |
| 5A: Bildirimler | 0/3 · 0/3 | tuval kusuru: Y_Bildirimler'in içeriği 900 px'e sığmıyor, `<nav>` 4 px eziliyor (y 14,8); yazı ölçüleri AYNI. 5A'nın Etiketler gerekçesiyle aynı |
| Koruma: Tüm ekranlar (4C eşlemesi, 67) | 66/67 · 66/67 | `main` boyu (veri) — 5A tabanıyla aynı |
| Koruma: Tezgâh (36) | 35/36 · 35/36 | "ödeme1 kasa" satır yüksekliği — `yasal.css:109` (5A'da da vardı) |
| Koruma: Müşteriler (28) | 24/28 · 24/28 | h1 genişliği/arama x/TOPTANCI x — başlık verisi; bu paket `.yz-musteri`'ye dokunmuyor (5A'da 20/31 idi) |
Tablolar: `ham/yildiz-katman/*-tablo.md`. Not: 5A'nın 39/28/… öğelik eşleme dosyaları korunmamıştı (geçici dizin); 5A ekranları için menü + üst satır + başlık üçlüsü ölçüldü.

### 3.5 Öteki 8 şema — FARK 0
- Kapsam kanıtı: blokta her kural `html:root[data-sema="yildiz"]` ile başlar (test YK_20).
- `sema-fark.mjs` (dosya aç/kapa yöntemi) 128 karşılaştırmada iki koşumda 22 ve 17 FARK verdi; FARK kümeleri koşumdan koşuma değişti (`/urunler/agac` ilkinde 12, ikincide 0) — 5A § 2.4/4D'de kayıtlı yöntem kararsızlığı.
- Bu yüzden belirlenimci ölçüm yazıldı (`blokfark.mjs`): 8 şema × açık/koyu × 5 durum (Yeni ürün modalı, Yeni kategori çekmecesi, Kullanıcılar sayfası, Kullanıcılar Düzenle, Yeni hesap modalı) = **80 karşılaştırma**, sayfadaki TÜM öğelerin 20 hesaplanmış stil özelliği imzası, YILDIZ-KATMAN kuralları (33 kural) silinmiş ↔ yerinde: **FARK 0** (`ham/yildiz-katman/blokfark.out`). Razor dalları yalnız Yıldız'da çizilir (YK_12).

### 3.6 Görüntüler (Read ile bakılan: 3)
`ham/yildiz-katman/son-birlesik.png` — üstte Yeni ürün (koyu 1440 PRO), Yeni grup (açık 1440 BASIT), Uyum başlığı (açık 1440), İşlem defteri başlığı (koyu 1440 PRO); altta 375: Yeni Cari PRO açık (yapışkan Kaydet), Etiketler "Ürün seçin" düğmesi, Kullanıcılar başlığı koyu, Emanet iade et koyu. Ara iki görüntü: taban (yüzen kart) ve başlık ilk hali.

## 4. Testler
- Yeni: `Kuyumcu.Tests/YildizKatmanTestleri.cs` — 47 test (30 satırlı seçili öğe teorisi dahil): YK_01–03 seçili öğe, YK_10–13 başlık dalı/sözlük, YK_20–25 CSS kapsam ve değerler, YK_30 Etiketler düğmesi, YK_31 Ürün ağacı adları. Filtreli: **47/47 yeşil**.
- İlgili filtre (Yildiz|UstBar|RotaBaslik|TK2|UrunAgaci|Cekmece|Modal|Etiket|Kabuk): 816 test, 4 kırmızı — hiçbiri bu paketin dosyası değil (§ 7).
- **Tam süit (son kod):** 12569 · geçti 12561 · başarısız 5 · atlandı 3 (trx: `/private/tmp/kuyumcu-serit/trx-pYILDIZ-KATMAN`). Beşinin hiçbiri bu paketin dosyalarına dokunmuyor (§ 7) → **bu paketin ürettiği yeni kırmızı 0**.

## 5. Mutasyon (yedek → tek eşleşmeli perl → filtreli test → geri koy → cmp)
| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M1 | YildizKabuk: MusteriStok → "tum-ekranlar" | YK_01 (/urunler, /urunler/agac, /urunler/etiketler, /sayim, /satin-alma, /fiyatlandirma, /cariler/c1) + YK_03(/urunler) | AYNI |
| M2 | UstBar: `YildizUst => false` | YK_10, YK_11 | AYNI |
| M3 | perde `transparent` → `var(--ayrac)` | YK_21 | AYNI |
| M4 | `.modal-arka` `flex-end` → `center` (yüzen kart) | YK_21 | AYNI |
| M5 | panel `border-radius: 0` → 16px | YK_22 | AYNI |
| M6 | eylem satırı `sticky` → `static` | YK_25 | AYNI (bkz. § 7 olay) |
| M7 | BaslikBoyu: /uyum 40 → 64 | YK_11 | AYNI |
| M8 | Etiketler düğmesinden `@onclick` silindi | YK_30 | AYNI |
| M9 | SeciliKimlik görünürlük kontrolü silindi | YK_02 | AYNI |
| M10 | UrunAgaci çekmece başlığı Yıldız dalı silindi | YK_31 | AYNI |
Son mutasyondan sonra temiz `dotnet build`: 0 hata.

## 6. Derleme ve denetim
- `dotnet build Kuyumcu.sln`: **0 hata**. Uyarı: tam derlemede 6, hepsi başka şeritlerin test dosyalarında (SUBEKAPSAMASinirTestleri:49, KesifP2Testleri:455, SonKalanlarTestleri:593/824, Yildiz2Testleri:194/234). Bu paketin dosyalarında uyarı 0 (YildizKatmanTestleri'ndeki iki xUnit1031 async'e çevrilerek giderildi).
- `denetim.sh`: 315 ✓, 2 ✗ — **P-30 DEMO_INSAN_SURESI** (kabulde hariç) ve **GIRDI-2 TEST_GECERSIZ_FIKSTUR 1** (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK doğrulayıcı şeridi; 5A/5B raporlarında da kayıtlı). Bu paketin sayaçları ✓: ModalPencere 48/55 (artış 0), satır içi style 24, SABIT_RENK 0, DIL-EKRAN 0.

## 7. Açık kalanlar (sahibiyle)
1. Tam süit kırmızıları (bu paketin değil): `RaporDortTestleri.RD_KH01/RD_KH02` (rapor önbelleği), `Yasal4HttpTestleri.Mutabakat_bugun_iptal…` ve `Y4A_tur4_eszamanli_12…` (yasal/pusula HTTP — YASAL-KAPI şeridi), `Yildiz5ATestleri.Y5A_04` (emanet eşik günü, gece 00:xx'te tarih sınırına bağlı; `Pages/Emanet.Yildiz.cs` 5A). İlk filtreli koşumda ayrıca `Yildiz1Testleri.Y1_10` (TEZGAH-ODEME CSS'inde sabit renk — sonraki koşumda yeşil) ve `PSayimEkranTestleri.PS_B6` (P-SAYIM) görüldü.
2. **Eşzamanlı yazma olayı (orkestratöre):** YILDIZ-TEZGAH-ODEME şeridi `tema-yildiz.css`'i bütün dosya olarak iki kez yeniden yazdı: (a) bu paketin eklediği ikinci parça (başlık + alt şerit sırası) silindi → aynen yeniden eklendi; (b) M6 mutasyonu etkin iken dosyayı okuyup kendi değişikliğiyle geri yazdı → `position: static` mutasyonu dosyada kaldı; YK_25 kırmızısıyla yakalandı, düzeltildi; KATMAN bloğu mutasyon öncesi yedekle **birebir aynı** (diff boş). Aynı dosyada iki şerit varken CSS mutasyonu yapılmamalı ya da dosya kilidi gerekir. Bu sırada TEZGAH-ODEME'nin kendi değişikliğinden bir satırın (`--yz-kaydir-golge` degradesi) benim geri koymamla kaybolup kaybolmadığı kesin bilinemiyor — o şerit kendi bloğunu doğrulamalı.
3. Emanet "Geri ver" çekmecesi `.yz` yüzünün içinde (YILDIZ-4C kimlik çekmecesi kalıbı): alt şerit "Vazgeç → Kaydet" sırasında; ortak katmanlar "Kaydet → Vazgeç". Tuvalde iki sıra da var (Tezgah4: Vazgeç önce; form tuvalleri: b1 önce). Tezgâh şeridini bozmamak için `.yz` dokunulmadı → belirsiz eşleşme #4.
4. Kendi Yıldız yüzü olmayan 20 ekranın GÖVDESİ eski düzende (tablo, kart, filtre); bu paket yalnız başlık + katman. Satın alma / İrsaliye / Kılavuz / Ayarlar için tuval yok (denetim Y17 "tuval çizilsin").
5. `sema-fark.mjs` aç/kapa yöntemi kararsız (4D'den beri); belirlenimci `blokfark` yöntemi bu pakette kullanıldı, araca taşınması önerilir.
6. Kanıt kopyalarken `ham/son-birlesik.png` ham kökünde de bir kopya oluştu (silme kancası nedeniyle bırakıldı; asıl yer `ham/yildiz-katman/`).

## 8. Belirsiz eşleşme (orkestratör kurucuya gösterir)
| # | Öğe | Tuvalde | Seçilen en yakın kalıp |
|---|---|---|---|
| 1 | Çekmece/pencere | Tuvalde katman çizimi yok | Sağ çekmece + form kalıbı (kıl çizgi, b1/b2) + Yıldız tezgâhı kimlik çekmecesinin değerleri (başlık 22/300/−0,01em, kapat soluk, perde saydam) |
| 2 | Perde | yok | saydam (YILDIZ-4C emsali) — arkadaki ekran karartılmaz |
| 3 | Başlık üst satırı, veri taşıyan tuval metinleri (Kâr-zarar "Ekim 2026 · ilk 8 gün", Satış analizi "son 30 gün") ve tuvali olmayan ekranlar (İrsaliyeler, Satın alma, Kılavuz, Ayarlar) | — | yalnız grup adı (uydurma veri yok) |
| 4 | Çekmece alt şerit sırası | Tezgah4 "Vazgeç · Kaydet", form tuvalleri "Kaydet · Vazgeç" | ortak katmanlarda form sırası (b1 önce); `.yz` içindeki çekmeceler değiştirilmedi |
| 5 | Kur saati + Yardım (eski başlık satırı) | yok | Kur saati üst menüde zaten var → bu şeritte gizli (Yıldız yüzlü ekranlarla aynı); Yardım `.ust` kalıbında |
| 6 | Etiketler "Ürün seçin" | tuvalde seçili ürün yazılı | değer düğme olur, b2 alt çizgisi |
| 7 | Katman içi alan grubu başlığı (legend) | yok | `.ust` 12/500/.22em büyük harf soluk |

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK, migration YOK. Düzenleme yolları değişmedi (ModalPencere/Cekmece DOM'u, kimlikleri, geri çağrıları AYNEN; yalnız Yıldız görünümü). Etiketler düğmesi gerçek geri çağrı (`SecimDegistir`) taşır; kilitli düğme yok.

## 10. Canlı doğrulama + SQL
- Canlı: § 3'teki tüm ölçümler 5160/5161 üzerinde gerçek veriyle (Tunalılar tohumu). Ürün ağacı çekmecesi canlıda "Yeni grup · Grup adı · Üst grup" (görüntü), Yeni Cari PRO 375'te Kaydet y 728–772 / 812 (görünür), Yeni kullanıcı 375'te 728–772 / 812.
- API ve vekil yalnız kendi PID'lerimle durduruldu; 5160/5161 boş. 5199/5200/5230/5241 dokunulmadı. Chrome'lar (9701–9708) kendi PID'leriyle kapatıldı.
- Kiracı `zsh kiraci-temizle.sh 01M4KV2B7Z8D88Q26134APCP3E --uygula` (rc 0). SQL: `tenants` başta **22**, sonda **22**; kiracının `tenants` satırı 0, `hareketler` satırı 0. `QA-TEST-*` dokunulmadı.

## 11. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — yalnız `/* YILDIZ-KATMAN */ … /* YILDIZ-KATMAN sonu */` bloğu
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor` — Yıldız başlık dalı (+ `IServiceProvider` enjeksiyonu)
- `dotnet/Kuyumcu.Web/Layout/YildizKabuk.cs` — `SeciliKimlik`, `GrupOgesi`, `BaslikBoyu`
- `dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor` — seçili öğe `SeciliKimlik`'ten
- `dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor` — kategori çekmecesinin üç metni yalnız `YildizMi` dalında
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizEtiketler.razor` — "Ürün seçin" düğme
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.YildizKatman.cs` (yeni; TR+EN)
- `dotnet/Kuyumcu.Tests/YildizKatmanTestleri.cs` (yeni)
- `dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js` (K15), `betikler/tur/yildiz-katman-tara.mjs` (yeni)
- Kanıt: `dokumanlar/09-uygulama-kayitlari/ham/yildiz-katman/` (+ `ham/son-birlesik.png` kopyası)
Dokunulan sayfalar (TEK DOKUNUŞ): UstBar (kabuk), UrunAgaci (5A ekranı), YildizEtiketler (5A bileşeni). Dosya alanı notu: seçili öğe mantığı YildizUstMenu'nün karar noktası `Layout/YildizKabuk.cs`'te; sayfa başlığı bileşeni `Layout/UstBar.razor`; Sözlük TR+EN için `Kuyumcu.Core/I18n` gerekti.

## 12. Kurucu / mali müşavir
Mevzuat hükmü yok; soru yok.
