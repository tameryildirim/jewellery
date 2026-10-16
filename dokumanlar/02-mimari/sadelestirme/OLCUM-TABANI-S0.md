# S-0 · ÖLÇÜM TABANI — "bugün" sütunu (23 Eylül 2026)

> Kaynak plan: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 4 (kabul ölçütleri) ve § 9 (ölçüm defteri).
> Bağlayıcı: **Anayasa md. 1** — ölçülemeyen kalem "ÖLÇÜLEMEDİ" yazılır, tahmin edilmez.
> Bu dalgada **kod yazılmadı**; yalnız bu dosya üretildi. Üç kod şeridinin (DALGA 7c, PARA DOĞRULUĞU, YAYIN HATTI) dosyalarına dokunulmadı.

Ölçüm kökü: `/Users/ramazanakkocan/ByGatech-yerel/jewellery`
(`~/Desktop/ByGatech/projeler/jewellery` bu dizine **symlink**'tir — aynı ağaç, iki ad.)
Web projesi: `dotnet/Kuyumcu.Web` · 46 sayfa (`Pages/*.razor`).

---

## 0. Yöntemin sınırı (önce bunu oku)

İki ölçüm cinsi vardır ve karıştırılmamalıdır:

| Cins | Ne verir | Bu belgede |
|---|---|---|
| **KAYNAK sayımı** (grep/python, `.razor` + `.css`) | Dosyada kaç tane yazılı olduğu — `@if` / modal / sekme arkasındakiler dâhil | Aşağıdaki kalemlerin çoğu |
| **DOM sayımı** (canlı, oturum açılmış ekran) | "İlk bakışta görünen" gerçek sayı | **Yapılamadı** — gerekçe § 12 |

Plan § 4'teki bazı "bugün" değerleri (ör. Cariler 329 px, 22 ekranda >1 birincil düğme) DOM sayımıdır; bu turda **DOM sayımı yapılamadığı için** onlar geçmiş ölçümden devralınmış ve kaynağı belirtilmiştir — yeniden doğrulanmamıştır.

---

## 1. Menüde ilk bakışta görünen satır (basit / pro) — ÖLÇÜLDÜ

Kaynaklar: `Layout/MenuTanimi.cs`, `Servisler/BasitMenu.cs`, `Layout/KenarCubugu.razor`.

```
# pro: menüdeki toplam bağlantı
grep -cE "^\s*new\(MenuGrubu\." dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs          → 40
# grup dağılımı
grep -oE '^\s*new\(MenuGrubu\.[A-Za-z]+' ... | sort | uniq -c
   BelgeUyum 3 · Gunluk 5 · MusteriStok 6 · ParaBanka 10 · Rapor 6 · Yonetim 10  → 40
# basit: BasitMenu.Yollar
sed -n '26,39p' dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs                        → 8 yol
# alt blok (her modda çizilir)
grep -n "AltBlok" -A4 Layout/MenuTanimi.cs   → /kilavuz, /ayarlar = 2 (+ "Çıkış" düğmesi)
```

| | Bağlantı satırı | Çizilen grup başlığı | Alt blok + çıkış | **Toplam satır** |
|---|---|---|---|---|
| **Basit** | **8** (`/`, `/islemler`, `/bildirimler`, `/cariler`, `/urunler`, `/urunler/etiketler`, `/kasalar`, `/raporlar`) | 4 (Günlük 3 · Müşteri-Stok 3 · Para-Banka 1 · Rapor 1) | 3 | **15** |
| **Pro** | **40** | 6 | 3 | **49** |

Not: "Favoriler" grubu (KenarCubugu.razor:52) kullanıcı yıldızladıkça üstte ek satır çizer; boşken çizilmez — yukarıdaki sayılara dâhil değil.
Hedef: ≤ 7 / ≤ 8 (gruplar kapalı). **Bugünkü açık-satır sayısı basitte 8, proda 40'tır** (plan § 4'teki "8 / 36–40" satırı **40** olarak kesinleşti).

## 2. POS'ta ilk bakışta görünen kontrol (basit mod) — KISMEN; "ilk bakış" ÖLÇÜLEMEDİ

`Pages/Islemler.razor` (2.576 satır) kaynak sayımı; `ProAlan` blokları (`ZorunluGorunur` olmayanlar) düşülmüştür:

| Ölçüm | Değer |
|---|---|
| Dosyadaki toplam form alanı / `<button>` | 24 / 12 |
| `ProAlan` katlıları düşülünce (basit mod üst sınırı) | **22 alan / 12 düğme** |
| Basit modda görünen işlem türü sekmesi | **3** (`BasitPos.Turler` = SATIS, ALIS, SATIS_IADE; profesyonelde 8) |
| `ProAlan` blok sayısı (Islemler) | 5 |
| Basit modda gövdeden çıkan alan | 1 (`vadeGun`) |

⚠ Bu **üst sınırdır, "ilk bakış" değildir**: alanların bir kısmı `@if` dalları, sekme içerikleri ve modaller arkasındadır. Gerçek "ilk bakışta görünen" sayı DOM ölçümü ister → **ÖLÇÜLEMEDİ** (§ 12).
Hedef: ≤ 3 alan + ≤ 12 düğme, tek ızgara. Pusula eşiği: 3 alan + 13 düğme.

## 3. Satırdaki eylem düğmesi (liste ekranları) — ÖLÇÜLDÜ (kaynak)

Yöntem: her `.razor` dosyasında `<tr>` … `</tr>` bloğu içindeki `<button` + `<a ` sayısının **en büyüğü**.

```
for f in Pages/*.razor; do awk '/<tr/{intr=1;c=0} intr{c+=gsub(/<button/,"&")+gsub(/<a /,"&")} /<\/tr>/{if(intr&&c>max)max=c;intr=0} END{...}' "$f"; done
```

| Sayfa | Satırdaki maks. eylem |
|---|---|
| **Cariler** | **9** |
| Urunler · UrunAgaci · Kullanicilar · Faturalar · Evraklar | 5 |
| Vezne · Pos | 4 |
| Uyum · Isletme · Gider · Fiyatlandirma · Banka | 3 |
| diğerleri | ≤ 2 |

Hedef: ≤ 3 + "⋯". **Bugün 6 ekran hedefin üstünde** (Cariler 9; beş ekran 5; Vezne/Pos 4).
(Plan § 4 "Cariler 7–8" diyordu; bu sayım bağlantıları da saydığı için 9 çıktı — yöntem farkı, çelişki değil.)

## 4. Liste satır yüksekliği (Cariler) — ÖLÇÜLEMEDİ (devralınan: 329 px)

Bu turda ölçülemedi (DOM gerekir, § 12). Plan § 4'teki **329 px** değeri `TASARIM-DENETIMI-2026-09-21.md` turundan devralınmıştır; **23 Eylül'de yeniden doğrulanmamıştır**. Hedef ≤ 64 px.

## 5. Ekran başına birincil düğme — ÖLÇÜLDÜ (kaynak)

```
grep -oE 'btn-birincil|dugme-birincil' Pages/<sayfa>.razor | wc -l
```

| Bulgu | Değer |
|---|---|
| Kaynakta ≥ 1 birincil düğme geçen sayfa | 33 / 46 |
| Kaynakta **> 1** birincil düğme geçen sayfa | **30** |
| En yüksek | Ayarlar 6 · Uyum 6 · Cariler 5 · Banka 4 · Kasalar 4 · Kullanicilar 4 · Kurulum 4 · Sayim 4 · Urunler 4 |

⚠ Kaynak sayımı modal/sekme içindekileri de sayar; plan § 4'teki "22 ekranda >1" DOM sayımıdır. İkisi birbirini nakzetmez: **kaynakta 30, ekranda (21 Eylül ölçümü) 22**. Hedef: 1.

## 6. Form alanı sayısı — ÖLÇÜLDÜ (kaynak)

Sayılanlar: `Bilesenler/Form/*` bileşenleri (`Metin, Sayi, Secim, Tarih, Para, Gram, Onay, Anahtar, Telefon, Eposta, Sifre, CokSatir, Arama, TarihAralik, SaltOkunur`) + ham `<input|<select|<textarea`.

| Sayfa | Alan | `ProAlan` blok |
|---|---|---|
| **Urunler** | **51** | 0 |
| **Cariler** | **46** | 0 |
| Islemler | 25 | 5 |
| Faturalar | 19 | 1 |
| Evraklar / Ayarlar | 14 | 1 |
| Siparisler | 13 | 1 |
| Vezne / Banka | 12 | 1 / 0 |
| Kullanicilar / Fiyatlandirma | 11 | 0 |
| Pos / Isletme | 10 | 1 |

Kritik bulgu: **plan § 5'in "basit modda ≤ 8 görünür alan" hedefine giden iki ana ekranda (Cariler 46, Urunler 51) hiç `ProAlan` yok** — bugün katlama mekanizması bu iki sayfaya hiç bağlanmamış. S-3'ün iş yükü buradadır.
Hedef: ≤ 8 görünür, gerisi katlı. Pusula: cari 3 zorunlu alan.

## 7. Düğme sistemi sayısı — ÖLÇÜLDÜ

`wwwroot/css/app.css` içindeki düğme selektörleri:

```
grep -oE "\.[A-Za-z0-9ğüşıöç_-]*(btn|dugme|buton)[A-Za-z0-9ğüşıöç_-]*" wwwroot/css/app.css | sort -u
```

| Sistem | Selektörler | Razor'daki kullanım |
|---|---|---|
| **1. `.btn` ailesi** | `.btn`, `-birincil`, `-ikincil`, `-kucuk`, `-buyuk`, `-tam`, `-tehlike` | 233 |
| **2. `.dugme` ailesi** | `.dugme`, `-birincil`, `-ikincil`, `-kucuk`, `-satir`, `-tehlike`, `.dugme-benzeri` | 57 |
| **3. çıplak `button`** | `button { font: inherit; cursor: pointer; }` (app.css:26) | tümü |

Ad-hoc (aileye ait olmayan, düğme biçimleyen) selektörler — **9**:
`.kenar-alt-butonlar button`, `.dil-secici button`, `.mod-anahtar button`, `.sayfa-basi-eylem button`, `.karsilama-araclar button`, `.kasa-duzenle-btn`, `.kur-guncelle-btn`, `.kabuk-arama-dugme`, `.kabuk-menu-dugme`
(+ razor'da sınıfsız/tek kullanımlık: `sekme`, `pill`, `kart-eylem`, `secici-tetik`, `modal-kapat`, `menu-arama-temizle`, `kenar-ortu`, `cekmece-kapat`.)

**Bugün: 3 sistem + 9 ad-hoc CSS selektörü.** Hedef: 1 (`.dugme`, 5 varyant). Plan § 4'teki "3 (+12 ad-hoc)" ile uyumlu (ad-hoc sayımı sınıra göre 9–17 arası oynar; yukarıdaki 9, "app.css'te düğmeye biçim veren, aileye ait olmayan selektör" tanımıdır).

## 8. `app.css`'te ham `font-size` / `padding` — ÖLÇÜLDÜ

```
grep -oE "font-size:[^;}]*" wwwroot/css/app.css | grep -v "var(" | wc -l   → 73
grep -oE "(^|[ ;{])padding(-top|-bottom|-left|-right|-inline|-block)?:[^;}]*" wwwroot/css/app.css | grep -v "var(" | wc -l → 75
```

| | Toplam bildirim | **Ham** (belirteç kullanmayan) |
|---|---|---|
| `font-size` | 121 | **73** |
| `padding` | 114 | **75** |

Hedef: ≤ 10 / ≤ 10 (yalnız print bloğu).
⚠ Plan § 4 "68 / 59" diyordu; bugün **73 / 75**. Aradaki fark 21→23 Eylül arasında app.css'e yazan dalgalardan gelir (dosya 23.09 13:29'da değişmiş). **Taban bu belgedeki 73/75'tir.**

## 9. Yalnız-ikon (metinsiz) düğme — ÖLÇÜLDÜ

Yöntem: `<button …>…</button>` içi düzleştirilip `<Ikon/>` ve etiketler çıkarıldıktan sonra metin kalmayanlar.

| Düğme | Dosya | Hedefe dâhil mi |
|---|---|---|
| `si-…-kaydet` (satır içi düzenle) | `Bilesenler/SatirIciDuzenle.razor` | **evet** |
| `si-…-iptal` | `Bilesenler/SatirIciDuzenle.razor` | **evet** |
| `ust-menu-ac` | `Layout/UstBar.razor` | **evet** |
| `kabuk-arama-temizle` | `Bilesenler/GlobalArama.razor` | **evet** |
| `modal-kapat` (`&times;`) | `Bilesenler/ModalPencere.razor` | hayır (kapat ×) |
| `cekmece-kapat` (`&times;`) | `Bilesenler/Cekmece.razor` | hayır (kapat ×) |
| `kenar-ortu` (görünmez örtü) | `Layout/AnaYerlesim.razor` | hayır (tutamaç/örtü) |

**Toplam 7 · hedef kapsamında 4.** Hedef: 0. (Dördü de `aria-label`/`title` taşır — erişilebilirlik kusuru değil, görünürlük kusuru.)

## 10. Sayfada vurgu rengi taşıyan öğe — ÖLÇÜLEMEDİ

Sayfa başına "vurgu rengi taşıyan öğe" ancak hesaplanmış stille (computed style) sayılabilir → DOM gerekir (§ 12).
Ölçülebilen tek yan: `app.css` içinde `var(--vurgu…)` kullanan bildirim sayısı = **39** (`grep -c "var(--vurgu" wwwroot/css/app.css`). Hedef ≤ 3 öğe/sayfa; bu sayı onunla **kıyaslanamaz**, yalnız S-1'de daralacak yüzeyin büyüklüğünü gösterir.

## 11. Ekranda ham kod / enum / ULID — ÖLÇÜLDÜ (kaynak)

Yöntem: `.razor`'da doğrudan basılan `@nesne.Rol|Tur|Durum|Kod|OdemeTuru` (sözlükten geçen `@Dil.T.*`, `value=`/`id=`/`@key` bağlamaları ve `@( … == … ? … )` üçlü karşılaştırmaları hariç).

**19 yer** (10 enum + 9 kod):

- Enum (Durum/Tür/Rol): `BankaEkstre.razor:53`, `Bildirimler.razor:294`, `Cariler.razor:783`, `DenetimIzi.razor:138`, `IslemDefteri.razor:164`, `Siparisler.razor:112`, `Uyum.razor:193`, `Yedekler.razor:135`, `Yedekler.razor:203`, `Yedekler.razor:205`
- Kod: `Panel.razor:88`, `Kur.razor:226`, `Sayim.razor:127`, `UrunAgaci.razor:218`, `UrunEtiketleri.razor:159`, `Raporlar.razor:590`, `FiyatListesiUrunPaneli.razor:151`, `FiyatListesiUrunPaneli.razor:254`, `TemaSecici.razor:15`

Ham **ULID** basan yer bulunamadı (`@…\.Id` yalnız `value=`, `id=`, `@key`, `href` bağlamlarında geçiyor) → **ULID: 0**.
Hedef: 0. (QA tur 7'nin "≥ 9 yer" bulgusuyla uyumlu ve ondan geniş; QA gözle, bu grep'le saydı.)

## 12. ÖLÇÜLEMEYENLER ve neden

| Kalem | Neden ölçülemedi | Kapatma yolu |
|---|---|---|
| POS'ta **ilk bakışta görünen** kontrol (basit) | DOM sayımı gerekir; 5199'daki yayın **ayakta** (HTTP 200) ama tezgâh ekranları **oturum açmayı** ister. Parola girmek bu oturumun sınırları dışındadır (kimlik bilgisi girilmez). | Kurucu/uygulayıcı oturum açar ve `read_page` ile sayar; ya da bUnit ile `Islemler` basit modda render edilip DOM'dan sayılır |
| Cariler **satır yüksekliği** (px) | aynı (DOM) | aynı |
| **Sayfa başına vurgu öğesi** | computed style gerekir (DOM) | S-1'de `denetçi VURGU_OGE_MAX` sayacı |
| Ekran başına birincil düğmenin **ekrandaki** sayısı | DOM; kaynak sayımı 30, ekran sayımı 21 Eylül'de 22 idi | S-1 sayacı BIRINCIL_DUGME |
| **Nakit barkodlu satış tık sayısı** | canlı senaryo + test kiracısı gerekir; bu dalgada kod/veri yazılmadı | Aşkın canlı senaryo (5198, test kiracısı sonunda silinir) |
| **Pusula nakit satış tık sayısı** (referans) | `PUSULA-REFERANS-2026-09-23.md`'de "SAYILAMADI" — video kareleri siyah | kurucu Pusula'yı 10 dk izletir |
| **Menü anlatım süresi / uçtan uca satış süresi / yeni tezgâhtar ilk satış** | kronometreli insan ölçümü; kişi atanmadı (§ 8.6) | Aşkın senaryosu + kurucunun bulacağı tezgâhtar |
| **QA turu 8** | `ham/qa-turu-8.json` **yok**; son tur 7 | `betikler/qa-turu.js` koşulur (yayın = ağaç şartıyla) |
| **Müşteri puanı (3 soru)** | `sadelestirme/MUSTERI-SESI.md` yok; kurucu henüz sormadı (§ 8.3) | kurucu sorar |
| **Kontrast AA / 46 rota taşma / test sayısı** | bu turda yeniden koşulmadı (kod şeritleri ağaçta; `dotnet build`/test koşmak üç şeridi etkiler) | S-1 kapısında `denetim.sh` + tam süit |

---

## 13. § 9 ölçüm defteri — S-0 sütunu

| Ölçüt | **S-0 (taban, 23.09)** | Hedef |
|---|---|---|
| Menü ilk bakış satır (basit/pro) | **8 / 40** (grup başlıkları + alt blokla 15 / 49) | ≤7 / ≤8 |
| POS ilk bakış kontrol (basit) | **ÖLÇÜLEMEDİ** — kaynak üst sınırı 22 alan / 12 düğme / 3 tür sekmesi | ≤3 alan + ≤12 düğme |
| Nakit barkodlu satış tık | **ÖLÇÜLEMEDİ** (devralınan: 5; elle ürün 7) | ≤3 |
| Pusula nakit satış tık (videodan) | **SAYILAMADI** | referans |
| Satır eylemi maks (liste ekranları) | **9** (Cariler) | ≤3 |
| Cariler satır yüksekliği (px) | **ÖLÇÜLEMEDİ** (devralınan: 329) | ≤64 |
| Birincil düğme >1 olan ekran | **30 / 46** (kaynak); DOM devralınan: 22 | 0 |
| Düğme sistemi sayısı | **3** (+9 ad-hoc selektör) | 1 |
| Ham font-size / padding (app.css) | **73 / 75** | ≤10 / ≤10 |
| Form alanı — Cariler / Urunler | **46 / 51** (ikisinde de ProAlan yok) | ≤8 görünür |
| Yalnız-ikon düğme | **4** (kapat ×/örtü hariç; toplam 7) | 0 |
| Sayfada vurgu öğesi | **ÖLÇÜLEMEDİ** (app.css'te 39 `var(--vurgu)` bildirimi) | ≤3 |
| Ekranda ham kod/enum | **19 yer** (10 enum + 9 kod) · ham ULID **0** | 0 |
| Kontrast AA başarısız çift | **ÖLÇÜLMEDİ (bu turda koşulmadı)** | 0 |
| Taşma (46 rota × 4 genişlik) | **ÖLÇÜLMEDİ (bu turda koşulmadı)** | 0 |
| QA görsel / kullanıcı dostu | **3,64 / 2,98** (tur **7**, 44 rota; tur 8 yok) | ≥4,3 / ≥4,0 |
| Özellik kaybı | 0 (taban) — envanter `sadelestirme/ENVANTER-S0.json` ile donduruldu | 0 |
| Test sayısı | devralınan 3869/3869 yeşil (bu turda koşulmadı) | düşmez |
| Müşteri puanı | **YOK** (MUSTERI-SESI.md üretilmedi) | ≥4 |

---

## 14. S-0 kapısı — durum

Plan § 6 S-0 kapısı: *"§ 9 defterinin S-0 sütunu dolu; envanter dosyası var; PUSULA-REFERANS'ta en az 3 videonun tık sayısı var."*

| Şart | Durum |
|---|---|
| § 9 S-0 sütunu dolu | **KISMEN** — 13 kalem sayıyla dolu, 6 kalem "ÖLÇÜLEMEDİ/ÖLÇÜLMEDİ" |
| Envanter dosyası | **VAR** — `sadelestirme/ENVANTER-S0.json` (+ `envanter-uret.sh`) |
| 3 videonun tık sayısı | **YOK** — `PUSULA-REFERANS-2026-09-23.md` "SAYILAMADI" |
| QA turu 8 | **YOK** — son tur 7 |
| MUSTERI-SESI.md | **YOK** |

→ **S-0 kapısı AÇILMADI.** Açılması için: (a) oturum açılmış DOM ölçümü, (b) QA turu 8, (c) Pusula tık sayımı (kurucu), (d) müşteri sesi (kurucu). Bunlar tamamlanmadan S-1'in "önce/sonra" kıyası sayıyla yapılamaz.

## 15. Frontend tasarımının backend'e etkisi (ölçülen gerçekler)

| Karar | Backend etkisi | Kanıt |
|---|---|---|
| D1 — yeni kiracı varsayılanı BASİT | **Migration GEREKMEZ.** `tenants.varsayilan_arayuz_modu` canlıda var (varchar(12), varsayılan 'PROFESYONEL'), sunucuda çözülüyor → yalnız **varsayılan değer değişikliği** | orkestratör ölçümü (23.09), `AuthModulu.cs:500` |
| D2 — "Peşin Müşteri" sistem carisi | **Migration GEREKİR** — `cariler.sistem_carisi` canlıda yok | orkestratör ölçümü |
| D3 — karma ödemeye ALTIN bacağı | **Yeni ödeme türü GEREKMEZ**; `Entities.cs:112` `OdemeTurleri` zaten `ALTIN` içerir. İş: has defteri yazma yolunu bağlamak | orkestratör ölçümü |
| S-1/S-2 (kabuk, görsel dil, fiş) | **Backend'e dokunmaz** — CSS + Layout + Islemler.razor; tek POST yolu (`Kaydet()`) korunur (Bertan KOŞUL 4) | plan § 3 md.5 |
| Basit modda gizlenen alan | Gövdeye **hiç konmaz** (`BasitPos.GovdedenCikan = ["vadeGun"]`) — null ezme riski yok | `Servisler/BasitPos.cs:36` |

Yani sadeleştirmenin backend maliyeti **tek migration**'dır (D2 oy alırsa); gerisi istemci katmanıdır.
