# PAKET P-57 — Bicim tr-TR birleştirmesi (has/gram nokta ondalığının kökten kaldırılması) — UYGULAMA RAPORU

- Şerit: **SERİT E** (P-77 → P-76 → **P-57**), izole VT `kuyumcu_test_e`, canlı port **5184**
- Rol: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-24 04:50–05:25
- **GÜNCEL BEYAN (EK TUR, 2026-09-24 05:36–06:00 — § 14): YARIM.** Doğrulayıcının 5 kalan satırından **4'ü kapatıldı**
  (SatisAnalizi:174, SiparisDetay:345-346, Raporlar:391 — sahip şeritleri C/D bitti, sayfalar serbest). **Vezne.razor:300
  AÇIK**: Vezne.razor koşmakta olan **S-2b** şeridinin dosya sınırında (plan-serit-f/g.js 05:30 listesi: "S-2b (… Vezne.razor …)")
  — başka şeridin koşan dosyasına dokunulmadı. Tek satır: `@k.Adet.ToString("0.###", CultureInfo.InvariantCulture)` →
  `@Bicim.Miktar(k.Adet, null)`. S-2b biter bitmez uygulanırsa kabul koşulu 3 tam karşılanır.
- İlk tur beyanı (04:50–05:25): YARIM — paket kapsamı (Bicim.cs) ve 7 kapı maddesi tam; kabul koşulu 3 Bicim dışındaki
  4 satır yüzünden tam karşılanmamıştı (§ 10).

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (TRIYAJ-TUR8 § P-57 + görev metni) | Kodda bulunan | Sonuç |
|---|---|---|
| `Bicim.cs:11-12` Has()/Gram() `InvariantCulture`, Tl() tr-TR | Birebir: satır 11 `F4 InvariantCulture`, satır 12 `F3 InvariantCulture`, satır 10 `C2 Tr` | **Doğru** |
| Makine() API'ye giden değer, InvariantCulture | `Makine(decimal/double/int/DateOnly/DateTime)` Invariant | **Doğru** — dokunulmadı |
| "13 çağıran sayfa" | `Bicim.Has/Gram/Miktar` çağıranı: **13 sayfa + 6 bileşen** (Cariler, Emanet, Faturalar, Hurda, IslemDefteri, Kasalar, Panel, Raporlar, SatisAnalizi, Sayim, UrunAgaci, UrunEtiketleri, Urunler · AltinBacagiPaneli, FisKalemTablosu, FisTakas, Form/Gram, Form/Sayi, A4Belge), 82 çağrı | **Doğru** (bileşenler ek) |
| Düzenleme formlarında görüntü biçimiyle geri doldurma | Tüm 82 çağrı GÖRÜNTÜ amaçlı (td/span/StatKart/önizleme/SecenekOgesi etiketi/başarı bandı). `value=`, `Deger=@Bicim.Has…` ile girdiye geri dolum ve görüntü metnini `Parse` eden yol **yok** (grep kanıtı). AltinBacagiPaneli'nde görüntü (`Deger=Bicim.Has`) ile makine (`MakineDeger=Bicim.Makine`) zaten ayrı | Ayrıştırma düzeltmesi **gerekmedi** |
| (spesifikasyonda yok) `Bicim.Miktar` | ADET/birimsiz dallar da `InvariantCulture` "0.###" basıyordu → /raporlar'da devir hızı "2.028", kova adedi "2.5", Ürünler'de "4.25 adet" | **Sapma** — kabul koşulu /raporlar'ı kapsadığı için Miktar da tr-TR'ye alındı (§ 2) |

## 2. Yapılanlar

`dotnet/Kuyumcu.Web/Servisler/Bicim.cs` (yalnız iç düzeltme + ekleme; **hiçbir imza değişmedi**):

| Metot | Önce | Sonra |
|---|---|---|
| `Has(decimal)` | `F4, InvariantCulture` → "77.6014 has" | `F4, Tr` → **"77,6014 has"** |
| `Gram(decimal)` | `F3, InvariantCulture` → "56.991 g" | `F3, Tr` → **"56,991 g"** |
| `Miktar(decimal, birim)` ADET / boş / diğer | `0.###, InvariantCulture` → "4.25 adet" | `0.###, Tr` → **"4,25 adet"** (GRAM dalı zaten Gram'a devreder → "1,000 g"; metodun kendi yorumundaki hedef ancak şimdi gerçek oldu) |
| `Yuzde(decimal)` | yoktu | **YENİ**: `%` + `0.##, Tr` → 12.5 → "%12,5" (girdi yüzde değeri, oran değil) |
| `Makine(...)` ×5 | Invariant | **AYNEN** Invariant (API/SVG/input/sorgu sözleşmesi) |

Binlik ayırıcı bilinçli olarak KONMADI (`F`/`0.###` biçimi): "1.234,5670 has" gibi bir çıktıda nokta hem binlik hem
ondalık sanılabilirdi; basamak sayıları (has 4, gram 3) değişmedi.

Veri modeli / uç / migration / Sozlukler: **yok** (saf biçimlendirici paketi; yeni ekran metni yok → Sozlukler'e dokunulmadı).

### 2.1 Eski (hatalı) biçimi ÇİVİLEYEN test sabitlerinin güncellenmesi — gerekçe

Biçim değişikliği, eski nokta ondalığı birebir sabit olarak bekleyen 8 testi kırdı (tam süit #1, § 5). Bunlar kuralın
kendisini değil **hatanın çıktısını** sabitliyordu; spesifikasyon bu çıktıyı değiştirmeyi emrediyor. Yalnız sabit metin
güncellendi, hiçbir iddia gevşetilmedi (UX1C3'teki `DoesNotContain("26.3428")` de "26,3428"e çevrildi ki koruma boşa
düşmesin; UX4c'deki gövde iddiası `decimal.Parse(..., InvariantCulture) == 9.16` AYNEN kaldı).

| Test (sınıf.metot) | Dosya (son değişiklik, koşan şerit var mı) | Değişiklik |
|---|---|---|
| UX4dSafTestleri.M17_Miktar_GRAM_birimi_icin_g_yazar | UX4dSafTestleri.cs (21.09 14:19 · koşan şerit yok) | "1.000 g" → "1,000 g" |
| P20EkranTestleri.Kaydedilince_sunucunun_KALAN_BAKIYESI_banda_yazilir | P20EkranTestleri.cs (21.09 15:23 · yok) | "4.160 g" → "4,160 g" |
| UX1C3EkranTestleri.C3_41_Net_has_GUNLUK_ile_ayni_tanimdan_cizilir | UX1C3EkranTestleri.cs (21.09 15:07 · yok) | "425.7200"/"26.3428" → virgül; "P-57'ye ertelendi" yorumu kapandı |
| UX4cEkranTestleri.M10_Cikista_22_ayar_10_gram_govdeye_9_16_HAS_GRAM_olarak_gider | UX4cEkranTestleri.cs (24.09 01:39 · yok) | ekran "12.5000"/"9.1600" → virgül; gövde iddiası değişmedi |
| EmanetEkranTestleri.Elimizdeki_emanet_ve_hareketler_gosterilir | EmanetKurEkranTestleri.cs (24.09 00:47 · yok) | "12.5000 has" → "12,5000 has" |
| S35EmanetEkranTestleri.S35_14_… | S35KurVitrinEmanetTestleri.cs (24.09 00:56 · S-3B bitti) | "12.5000 has" → "12,5000 has" |
| S33UrunlerEkranTestleri.S33_05_… ve S33_06_… | S33UrunlerEtiketlerEkranTestleri.cs (24.09 01:59 · S-3A bitti) | "4.25 adet" → "4,25 adet" (2 satır) |

Görev metnindeki "başka şeridin testini kıran biçim değişikliği yapılmaz — DUR" kuralı değerlendirildi: yedi dosyanın
hiçbiri o an KOŞAN bir şeridin (S-2b: Islemler/Fis/Satis/POS/Vezne/Cikti · SERİT D: UrunEtiketleri/Vitrin/Raporlar/
Modules/Rapor) dosyası değil; son değişiklikleri koşan şeritlerin başlangıcından önce ve biten paketlere ait. Bu yüzden
DUR gerekmedi; değişiklik yedeklendi (`scratchpad/mut-yedek/P-57/test-once/`) ve her satır yukarıda. Orkestratör aksini
görürse yedekten birebir geri dönülebilir.

## 3. Yeni testler (5)

| Test | Tür | Ne ölçer |
|---|---|---|
| `Kuyumcu.Tests.BicimTestleri.Has_TurkceVirgulOndalikBasar` | saf | Has(77.6014m)="77,6014 has" (en-US ve tr-TR iş parçacığında), negatif, F4 tamamlama, binliksiz |
| `Kuyumcu.Tests.BicimTestleri.Gram_TurkceVirgulOndalikBasar` | saf | Gram(56.991m)="56,991 g"; Miktar GRAM/ADET/boş virgül |
| `Kuyumcu.Tests.BicimTestleri.Makine_InvariantKalir_ApiSozlesmesiBozulmaz` | saf | tr-TR iş parçacığında Makine(77.6014m)="77.6014", double/int/DateOnly, Invariant ile geri ayrıştırılabilirlik |
| `Kuyumcu.Tests.BicimTestleri.Yuzde_TurkceVirgulOndalikBasar` | saf | "%12,5", "%25", "%33,33", "%-4,2", "%0" |
| `Kuyumcu.Tests.P57.RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez` | bUnit | /raporlar: cari yaşlandırma has hücreleri ("-16,9460 has", toplam "-14,4460 has"), stok yaşlandırma kovaları ("12,500 g"), Miktar kartları ("2,5", "2,028", "180,5"); iki bölümde nokta ondalıklı sayı regex'i 0 eşleşme (tarih ve tr binlik hariç) |

**Sapma (ad):** spesifikasyon `RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez` diyor ve ayak izine
`RaporlarEkranTestleri.cs`'i yazıyor. `RaporEkranTestleri.cs` / `RaporlarEkranTestleri.cs` SERİT D'nin (Raporlar.razor)
alanında ve RaporlarEkranTestleri.cs 04:42'de başka bir ajan tarafından değiştirilmişti. Çakışmamak için test yeni
dosyada (`P57RaporBicimEkranTestleri.cs`) `Kuyumcu.Tests.P57` ad alanında aynı sınıf+metot adıyla yazıldı; filtre
`FullyQualifiedName~RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez` onu bulur.

## 4. Derleme

`dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra, 05:16) → **0 Hata, 1 Uyarı**. Uyarı
`Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` — P-57'nin dosyası değil (SERİT D alanı), bu paketten önce de vardı.

⚠ Paralel derleme tuzağı iki kez yaşandı (Anayasa md.1 kaydı): (1) 04:56'da başka bir şeridin derlemesi Bicim.cs'in
yarım düzenlenmiş hâlini derledi; artımlı derleme "güncel" sandı ve ilk koşumda `Miktar` eski davrandı ("4.25 adet") —
`--no-incremental` ile giderildi; (2) bir `--no-incremental` denemesi WebCil kilidiyle (MSB4018 ConvertDllsToWebCil) düştü,
"Tests.dll bağımsız değişkeni geçersiz" koşumu KANIT SAYILMADI, tekrarlandı. Mutasyon betiği her mutasyonda
`--no-incremental` + 3 deneme kullanır.

## 5. Test koşumları

| Kapsam | Toplam | Geçti | Başarısız | Not |
|---|---|---|---|---|
| Tam süit ÖNCE (P-57 kodu yok, 04:54) | 4885 | 4882 | 3 | `SozlesmeKapsamTestleri` ×2, `SozlesmeYansimaTestleri` ×1 — SERİT D yarım işi (P-57 öncesi) |
| Yeni testler (5) | 5 | 5 | 0 | |
| Tam süit #1 (P-57 kodu, test sabitleri güncellenmeden) | 4890 | 4877 | 13 | 8'i P-57 (§ 2.1'deki eski-biçim sabitleri) · 5'i başkasının: G5SatisKirilimiHttp ×3 + G5SatisKirilimiSaf ×1 (SERİT D, G-5 yazılıyordu), S25bSaticiHttpTestleri.S25b_H3 (S-2b) — biçimle ilgisiz (tutar/400 iddiaları) |
| Etkilenen sınıflar (9 sınıf) | 142 | 142 | 0 | `BicimTestleri, P57.RaporEkranTestleri, EmanetEkranTestleri, S33UrunlerEkranTestleri, S35EmanetEkranTestleri, P20EkranTestleri, UX1C3EkranTestleri, UX4cEkranTestleri, UX4dSafTestleri` |
| Bicim kullanan 24 test dosyası sınıfları (ilk süzgeç) | 327 | 324 → sabitlerden sonra yeşil | — | |
| **Tam süit SON** (`trx-pP-57`, 05:17) | **4890** | **4890** | **0** | ÖNCE'deki 3 kırmızı da bu arada sahibince kapanmış |

Kapı (7) — **yeni kırmızı farkı = 0** (`comm -13 kirmizi-once kirmizi-son` boş).

## 6. Mutasyon kanıtları (her biri: yedek → tek eşleşmeli perl → `--no-incremental` derleme → süzgeçli koşum → geri kopya → `cmp` birebir)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M57-1** (spesifikasyon) | `Has`: `Tr` → `InvariantCulture` | **BicimTestleri.Has_TurkceVirgulOndalikBasar**, P57.RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez, EmanetEkranTestleri.Elimizdeki_emanet…, S35EmanetEkranTestleri.S35_14…, UX1C3EkranTestleri.C3_41…, UX4cEkranTestleri.M10_Cikista… | cmp birebir |
| **M57-2** (spesifikasyon) | `Makine(decimal)`: `InvariantCulture` → `Tr` | **BicimTestleri.Makine_InvariantKalir_ApiSozlesmesiBozulmaz** | cmp birebir |
| M57-3 | `Gram`: `Tr` → `InvariantCulture` | BicimTestleri.Gram_TurkceVirgulOndalikBasar, P20EkranTestleri.Kaydedilince…, P57.RaporEkranTestleri.HasVeGramHucreleri…, UX4dSafTestleri.M17… | cmp birebir |
| M57-4 | `Yuzde`: Invariant + işaret sonda | BicimTestleri.Yuzde_TurkceVirgulOndalikBasar | cmp birebir |
| M57-5 | `Miktar` boş birim: `Tr` → Invariant | BicimTestleri.Gram_TurkceVirgulOndalikBasar, P57.RaporEkranTestleri.HasVeGramHucreleri… | cmp birebir |
| M57-6 | `Miktar` ADET: `Tr` → Invariant | BicimTestleri.Gram_TurkceVirgulOndalikBasar, S33UrunlerEkranTestleri.S33_05…, S33_06… | cmp birebir |

6/6 kırmızı; test açığı yok. Son geri almadan sonra `cmp mut-yedek/P-57/Bicim.cs.son Bicim.cs` → birebir; ardından
temiz `--no-incremental` derleme (§ 4) ve son tam süit (§ 5).

## 7. Denetim (`dotnet/denetim.sh`, 05:19)

**"DENETİM TEMİZ."** (çıkış 0). İlgili sayaç: `Kültürsüz biçimli ToString (Web) 27 (tavan 27)` ✓ (P-57 Invariant
kullanımını azalttı, sayacı artırmadı). ModalPencere 48/55, satır içi style 110, duzenlenenId 1/1 — değişmedi (sayfa
dokunulmadı).

## 8. Canlı doğrulama (port 5184, `kuyumcu_net`, kiracı `PAKETP-57-TEST Kuyumculuk` = `01M38KDSR2J6SHS7KXAQWYEGXM`)

Başlangıç `COUNT(*) tenants` = **10**, `QA-TEST-%` = **4**. Kiracı: tek kullanımlık davet (SHA-256 jeton özeti) →
`POST /auth/kayit`. Biçim değişikliği İSTEMCİDE (Blazor WASM) koşar; canlıda iki şey ölçüldü: (a) API sözleşmesi
(Makine tarafı) değişmedi, (b) 5184'ün sunduğu istemci ikilisi P-57 derlemesidir.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | `POST /api/v1/auth/kayit` | 201 | **201**, tenantId döndü, accessToken var |
| 2 | `GET /kur/guncel` | kur id | var (ilk denemede) |
| 3 | `POST /kasalar` | 200/201 | **200** |
| 4 | `POST /urunler` gövde `"gramAgirlik":10.375` (NOKTA ondalık — Makine sözleşmesi) | 201, değer korunur | **201**, yanıt `gramAgirlik=10.3750 hasGrami=9.503500` |
| 5 | `POST /islemler/alis` NAKIT, boş kasa | 400 (kasa eksiye düşmez) | **400** "bakiyesi yetersiz" — beklenen iş kuralı; HAVALE ile tekrarlandı |
| 6 | `POST /auth/giris` (+tenantId) | 200 | **200** |
| 7 | `POST /cari` (TEDARIKCI) | 201 | **201** |
| 8 | `POST /islemler/alis` HAVALE adet=3 `birimFiyatTl=1000.5` | 200 | **200**, `tur=ALIS, tlTutari=-3001.50` |
| 9 | `GET /raporlar/stok-yaslandirma` | 200, JSON nokta ondalık | **200**, `altinHasGram=28.510500 toplamAdet=3.000` (JSON sayı; biçim sunucuda değişmedi) |
| 10 | `GET /cari/{id}` | 200 | **200**, `tlBakiye=-3001.50 hasGramBakiye=0.000000` |
| 11 | `GET /_framework/Kuyumcu.Web.wasm` | P-57 derlemesi | **200**, 2 907 929 bayt; SHA-256 `007639992c22…` = `Kuyumcu.Web/bin/.../wwwroot/_framework/Kuyumcu.Web.wasm` = `obj/.../webcil/Kuyumcu.Web.wasm` (05:16:20 derlendi, Bicim.cs son hâli 05:16:01) |

SQL:
- `select gram_agirlik,has_grami from urunler where tenant_id=… and kod='P57-ALTIN'` → `10.3750 · 9.503500`
- `select tur,adet,round(has_grami,4),round(tl_tutari,2) from hareketler where tenant_id=…` → `ALIS · 3.000 · 28.5105 · -3001.50`

Ekranın bu canlı değerleri nasıl basacağı: `Bicim.Has(28.5105m)` = "28,5105 has", `Bicim.Gram(10.375m)` = "10,375 g"
(BicimTestleri'ndeki kültür-bağımsız iddialarla aynı yol). **Tarayıcıda oturum açıp ekranı görmek bu ajanın yapmadığı bir
adımdır** (parola girişi güvenlik kuralı gereği ajanlara kapalı); ekran davranışı bUnit ile, sözleşme ve sunulan ikili canlıda
kanıtlandı.

**Temizlik:** API (`dotnet run` PID 5257, dinleyen 5259) `kill` → `lsof -nP -iTCP:5184 -sTCP:LISTEN` **boş**.
`tenant_id` sütunu olan **70 tablodan** test kiracısının satırları silindi (kalan satır: hiçbir tabloda yok); davet ve
`tenants` satırı silindi. Son: `COUNT(*) tenants` = **10** (başlangıç 10) · `PAKETP-57%` = 0 · P57 davet = 0 ·
`QA-TEST-%` = **4** (dokunulmadı).

## 9. DoD sözleşmesi

Yeni varlık/kayıt türü **yok** (saf biçimlendirici). PATCH/DELETE/denetim kaydı/düzenleme yolu gerektiren yeni kayıt
doğmadı. Dokunulan sayfa: **yok** (TEK DOKUNUŞ listesi boş; 13 sayfa yalnız doğrulandı). Yeni düğme/form yok.

## 10. Kabul koşulları

| Koşul | Durum | Kanıt |
|---|---|---|
| `Bicim.Has(77.6014m)` = "77,6014 has" | ✓ | Has_TurkceVirgulOndalikBasar · M57-1 |
| `Bicim.Gram(56.991m)` = "56,991 g" | ✓ | Gram_TurkceVirgulOndalikBasar · M57-3 |
| `Bicim.Makine(...)` değişmedi (nokta, Invariant) | ✓ | Makine_InvariantKalir_ApiSozlesmesiBozulmaz · M57-2 · canlı #4/#9 |
| `Bicim.Yuzde(decimal)` eklendi | ✓ | Yuzde_TurkceVirgulOndalikBasar · M57-4 (henüz hiçbir sayfa çağırmıyor — § 11) |
| Düzenleme formlarında görüntü biçimiyle geri dolum → girdi tipine göre ayrıştırma | ✓ (gerekmedi) | 82 çağrının hepsi görüntü; geri dolum/Parse yolu yok (§ 1) |
| /raporlar, /islemler, /vezne, /siparisler/{Id}, /raporlar/satis-analizi'de nokta ondalık kalmadı | **KISMEN** | Bicim kaynaklı olanların hepsi kalktı (/raporlar bUnit + regex). **Bicim DIŞI, sayfanın kendi `ToString(..., InvariantCulture)` satırları kaldı** — aşağıda |
| dotnet test 0 başarısız | ✓ | 4890/4890 |
| denetim.sh TEMİZ | ✓ | § 7 |

**Kalan Bicim-dışı nokta ondalık (ekrana basılan) — bu şeridin dosya sınırı DIŞINDA, düzenlenmedi:**

| Sayfa:satır | İfade | Örnek çıktı | Alan sahibi |
|---|---|---|---|
| `Pages/SatisAnalizi.razor:174` | `(o2 * 100m).ToString("F1", InvariantCulture) + "%"` (iade oranı) | "25.0%" | SERİT C alanı (tamamlandı) → tek satır: `Bicim.Yuzde(o2 * 100m)` |
| `Pages/SiparisDetay.razor:345` | `k.GramAgirlik?.ToString("0.###", InvariantCulture)` | "5.25" | sahipsiz (P-51/P-52 alanı) → `Bicim.Gram(...)` |
| `Pages/SiparisDetay.razor:346` | `k.Adet.ToString("0.###", InvariantCulture)` | kesirli adette "1.5" | aynı → `Bicim.Miktar(k.Adet, null)` |
| `Pages/Vezne.razor:300` | `k.Adet.ToString("0.###", InvariantCulture)` | kesirli adette "1.5" | **S-2b** (Vezne) → `Bicim.Miktar(k.Adet, null)` |

(`Vezne.razor:772/807/816-818/957` de Invariant kullanır ama bunlar form DEĞERİNE giden makine biçimi — doğru, dokunulmamalı.)
/islemler'de Bicim dışı Invariant görüntü satırı bulunmadı.

## 11. Açık kalanlar

1. **§ 10 tablosundaki 4 satır** — sayfa düzenlemesi gerekir; dosya sınırı gereği bu pakette yapılmadı. Öneri: S-2b
   bitince tek küçük paket (SatisAnalizi + SiparisDetay + Vezne, 4 satır, `Bicim.Yuzde/Gram/Miktar`). Bu yapılınca kabul
   koşulu 3 tam karşılanır.
2. `Bicim.Yuzde` eklendi ama henüz çağıranı yok (KarZarar.razor `$"%{…}"` ve SatisAnalizi SERİT C/D alanında).
3. Canlı API hata metni "0.00 TRY var, 3001.50 TRY isteniyor" (canlı #5) — SUNUCU tarafı Invariant metin; ekran
   biçimi değil ama kullanıcıya gösterilen bir mesaj. Kapsam dışı (Modules/Satis, S-2b alanı) — orkestratöre not.
4. Tam süit ÖNCE'deki 3 kırmızı (`SozlesmeKapsamTestleri` ×2, `SozlesmeYansimaTestleri` ×1) ve tam süit #1'deki
   G5SatisKirilimi ×4 (SERİT D) / S25b_H3 (S-2b) — başkasının yarım işi; son koşumda hepsi yeşildi.

## 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` — bu paketin payı)

```
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs                          (Has/Gram/Miktar iç düzeltme + Yuzde eklendi)
?? dotnet/Kuyumcu.Tests/BicimTestleri.cs                          (YENİ, 4 test)
?? dotnet/Kuyumcu.Tests/P57RaporBicimEkranTestleri.cs             (YENİ, 1 test)
   dotnet/Kuyumcu.Tests/UX4dSafTestleri.cs                        (izlenmeyen dizin; 1 sabit)
   dotnet/Kuyumcu.Tests/P20EkranTestleri.cs                       (1 sabit)
   dotnet/Kuyumcu.Tests/UX1C3EkranTestleri.cs                     (2 sabit + yorum)
   dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs                      (2 ekran sabiti)
   dotnet/Kuyumcu.Tests/EmanetKurEkranTestleri.cs                 (1 sabit)
   dotnet/Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs             (1 sabit)
   dotnet/Kuyumcu.Tests/S33UrunlerEtiketlerEkranTestleri.cs       (2 sabit)
```
(`dotnet/Kuyumcu.Tests/` git'te bütünüyle izlenmeyen dizin `??`.) Proje ağacına geçici dosya bırakılmadı; yedekler
`scratchpad/mut-yedek/P-57/`.

## 13. Kurucuya / mali müşavire notlar

Mevzuat hükmü yok (saf biçim paketi). Kurucuya: ekranlardaki has/gram/adet artık "12,5000 has", "56,991 g",
"4,25 adet" biçiminde — TL ile aynı sayı dili. Kalan 4 satır (§ 10) ayrı küçük pakete bağlı.


## 14. EK TUR (2026-09-24 05:36–06:00) — doğrulayıcı bulgularının kapatılması

### 14.1 Dosya sahipliği yeniden ölçüldü (ORKESTRATOR-DURUM.md + betikler/plan-serit-*.js)

| Dosya | Önceki sahip | Şimdi | Karar |
|---|---|---|---|
| `Pages/SatisAnalizi.razor` | SERİT C | **SERİT C 4/4 GEÇTİ** (04:10 kaydı) | serbest → düzenlendi |
| `Pages/Raporlar.razor` | SERİT D | **SERİT D 4/4 GEÇTİ** (05:35 kaydı) | serbest → düzenlendi |
| `Pages/SiparisDetay.razor` | sahipsiz | koşan S-2b/F/G listelerinde YOK | düzenlendi |
| `Pages/Vezne.razor` | S-2b | **S-2b koşuyor (S2-6)**; F ve G planlarında "S-2b (… Vezne.razor …)" | **DOKUNULMADI** → açık |

TEK DOKUNUŞ gerekçesi (DoD EK 2): üç sayfa bu dalgada ilk kez yalnız P-57 kabul koşulu 3 için açıldı; her birinde TEK
görüntü ifadesi değişti, id/düğme/form değişmedi. Dokunulan sayfalar: **SatisAnalizi.razor, SiparisDetay.razor, Raporlar.razor**.

### 14.2 Değişiklikler (Bicim.cs bu turda DEĞİŞMEDİ)

| Satır | Önce | Sonra | Örnek (önce → sonra) |
|---|---|---|---|
| SatisAnalizi.razor:174 | `(o2 * 100m).ToString("F1", CultureInfo.InvariantCulture) + "%"` | `Bicim.Yuzde(o2 * 100m)` | "12.5%" → "%12,5" |
| SiparisDetay.razor:345 | `k.GramAgirlik?.ToString("0.###", CultureInfo.InvariantCulture) ?? "—"` | `k.GramAgirlik is { } kg ? Bicim.Miktar(kg, null) : "—"` | "10.25" → "10,25" (başlık zaten "Gram"; `Bicim.Gram` "10,250 g" ekler — sütun biçimini değiştirmemek için Miktar) |
| SiparisDetay.razor:346 | `k.Adet.ToString("0.###", CultureInfo.InvariantCulture)` | `Bicim.Miktar(k.Adet, null)` | "1.5" → "1,5" |
| Raporlar.razor:391 | `s.Adet.ToString("0.##")` (kültürsüz → tarayıcı kültürü) | `Bicim.Miktar(s.Adet, null)` | en-US tarayıcıda "2.5" → "2,5" |

Yan etki: denetim "Kültürsüz biçimli ToString (Web)" sayacı **26** (tavan 27) — bir azaldı (cırcır tavanını indirmek
orkestratörün işi; denetim.sh'a dokunulmadı).

### 14.3 Testler

- `P57RaporBicimEkranTestleri.cs` (bu paketin dosyası): stok değeri fikstürüne **kesirli adet** (`adet:2.5, toplamHas:3.1234`)
  eklendi; test gövdesi **en-US CurrentCulture** altında koşar (WASM tarayıcı kültürünü taklit; kültürsüz ToString yakalanır);
  stok tablosu `#rapor-stok-has-baslik`'in tablosundan okunur: "2,5" ve "3,1234 has" + nokta ondalık regex'i.
- YENİ `P57EkNoktaOndalikEkranTestleri.cs` (ad alanı `Kuyumcu.Tests.P57`, sahte API'ler gerçekçi yanıt verir):
  - `P57.SatisAnaliziEkranTestleri.PersonelIadeOrani_TurkceYuzdeBasar_NoktaOndalikIcermez` — personel sekmesi, `iadeOrani:0.125` → "%12,5", "%" ile biten hücre yok, regex temiz.
  - `P57.SiparisDetayEkranTestleri.KalemGramVeAdet_TurkceVirgulBasar_NoktaOndalikIcermez` — `gramAgirlik:10.25, adet:1.5` → "10,25"/"1,5"; gramsız kalem "—", adet 2 → "2"; regex temiz.
- Filtreli: `P57|BicimTestleri` **7/7 geçti**; ilgili sayfa testleri (`SatisAnalizi|Siparis|RaporEkran|RaporlarEkran|P57|Bicim|P37|P36c|S3a|S3f|P51Ekran`) **265/265 geçti**.
- Yeni test sayısı bu turda: **2** (+1 mevcut testin güçlendirilmesi). Paketin toplam P-57 testi: 7.

### 14.4 Mutasyonlar (yedek `scratchpad/mut-yedek/P-57/<kod>.yedek` → tek eşleşmeli perl → derleme → filtreli koşum → geri kopya → `cmp` birebir)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M57-1 (tekrar) | `Bicim.Has` → `InvariantCulture` | `BicimTestleri.Has_TurkceVirgulOndalikBasar` | birebir |
| M57-2 (tekrar) | `Bicim.Makine(decimal)` → `Tr` | `BicimTestleri.Makine_InvariantKalir_ApiSozlesmesiBozulmaz` | birebir |
| M57-3 | SatisAnalizi:174 eski Invariant "F1"+"%" | `P57.SatisAnaliziEkranTestleri.PersonelIadeOrani_TurkceYuzdeBasar_NoktaOndalikIcermez` | birebir |
| M57-4 | SiparisDetay:345 eski Invariant gram | `P57.SiparisDetayEkranTestleri.KalemGramVeAdet_TurkceVirgulBasar_NoktaOndalikIcermez` | birebir |
| M57-5 | SiparisDetay:346 eski Invariant adet | `P57.SiparisDetayEkranTestleri.KalemGramVeAdet_TurkceVirgulBasar_NoktaOndalikIcermez` | birebir |
| M57-6 | Raporlar:391 eski kültürsüz `ToString("0.##")` | `P57.RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez` | birebir |

Her mutasyonda derleme 0 hata, filtreli koşum 1 başarısız (adı yukarıda), diğerleri geçti. Son mutasyondan sonra TEMİZ
`dotnet build Kuyumcu.sln` → **0 Hata, 1 Uyarı** (uyarı `EtiketEkranTestleri.cs(145,47) xUnit1031` — SERİT D dosyası, bu paketin değil).

### 14.5 Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_e`, trx `scratchpad/trx-pP-57-ek`)

**4931 toplam · 4925 geçti · 6 başarısız.** Altısı da bu paketin dokunmadığı dosyalardan:

| Kırmızı | Sebep (hata iletisi) | Sahibi |
|---|---|---|
| `SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor` | "Bilesenler/SatirEylemleri.razor: record Eylem" | SERİT G (P-61, yeni bileşen) |
| `OlcumKapisiTestleri.SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit` | beklenen 4, gerçek 3 (record sayacı) | SERİT G (aynı record) |
| `OlcumKapisiTestleri.P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu` | beklenen 4, gerçek 3 | SERİT G (aynı record) |
| `FiyatlandirmaUX4aEkranTestleri.Pasife_al_TAM_govdeyle_PATCH_atar` | WaitFor zaman aşımı | SERİT F (Fiyatlandirma.razor + OnayKipi) |
| `UX1C2EkranTestleri.Fiyatlandirma_pasife_alinca_KENDI_mesajini_yazar` | WaitFor zaman aşımı | SERİT F (Fiyatlandirma.razor) |
| `P17VitrinEkranTestleri.Ozel_kalemde_varsayilana_don_ACIK_ve_istek_atar` | WaitFor zaman aşımı | SERİT F (Vitrin.razor) |

Hiçbiri SatisAnalizi/SiparisDetay/Raporlar/Bicim ile ilgili değil → **bu tur yeni kırmızı üretmedi** (kapı maddesi 7).

### 14.6 Denetim (`bash dotnet/denetim.sh`, bu tur)

`DENETİM BAŞARISIZ` — 4 ✗, dördü de başka şeridin yarım işi:
`Pages+Bilesenler+Layout record tanımı 4 (tavan 3)` ve `P-10 BU_PAKETTE_EKLENEN_RECORD +1` ve `P-02 OLCUM_TABANI_KOPYA`
(record 4 ≠ 3) → `Bilesenler/SatirEylemleri.razor` (SERİT G) · `P-36 HAM_INPUT 43 > 42` ve `P-36c HAM_INPUT_SERBEST 1` →
`Bilesenler/OnayKipi.razor` (SERİT F, içinde `<input>` var). Bu paketin sayaçları ✓: `Kültürsüz biçimli ToString (Web) 26
(tavan 27)`, `P-36b GIRDI_AILESI_DISI_BICIM 0`. Değiştirilen üç sayfada `<input>` sayısı değişmedi (2/0/1 önce = sonra),
record eklenmedi.

### 14.7 Canlı doğrulama (port 5184, `kuyumcu_net`, kiracı `PAKETP-57-EK-TEST Kuyumculuk` = `01M38MTZDQQCVPZ19PPAPS0Z1J`)

Başlangıç `COUNT(*) tenants` = **11** (başka şeritlerin test kiracıları var: `PAKETS2-6-TEST`, `PAKETP-61-TEST …` — dokunulmadı),
`QA-TEST-%` = **4**.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | tek kullanımlık davet (SHA-256 jeton özeti) → `POST /api/v1/auth/kayit` | 201 | **201**, `tenantId`, `accessToken`, `refreshToken` |
| 2 | `POST /cari` (MUSTERI) | 201 | **201**, id `01M38MV86N…` |
| 3 | `POST /siparisler` kalem `gramAgirlik:10.25, adet:1.5` (Makine sözleşmesi, nokta) | 200 | **200**, `no=S-2026-000001, durum=TASLAK` |
| 4 | `GET /siparisler/{id}` | 200, JSON nokta ondalık (sözleşme değişmedi) | **200**, `"gramAgirlik":10.250,"adet":1.500` |
| 5 | `GET /_framework/Kuyumcu.Web.wasm` | bu turun derlemesi | **200**, 3 070 745 bayt; SHA-256 `c7942a7583b1dd60…` = `Kuyumcu.Web/bin/Debug/net8.0/wwwroot/_framework/Kuyumcu.Web.wasm` (05:44, düzenlemeler 05:38–05:39) |

SQL: `select gram_agirlik, adet from siparis_kalemleri where tenant_id='01M38MTZ…'` → `10.250 · 1.500`.
Ekran bu değerleri `Bicim.Miktar` ile "10,25" / "1,5" basar — bUnit M57-4/M57-5 ile aynı JSON biçimi. Tarayıcıda oturum
açıp ekranı görmek yapılmadı (parola girişi ajanlara kapalı).

**Temizlik:** API (PID 77471, dinleyen 77500) `kill` → `lsof -nP -iTCP:5184 -sTCP:LISTEN` **boş**. `tenant_id` sütunu olan
**70 tablodan** kiracı satırları silindi (kalan toplam **0**), davet ve `tenants` satırı silindi. Son: `COUNT(*) tenants` =
**11** (başlangıç 11) · `PAKETP-57%` = 0 · P57 davet = 0 · `QA-TEST-%` = **4** (dokunulmadı). Scratchpad'deki jeton/parola
içeren geçici dosyalar `printf ""` ile boşaltıldı.

### 14.8 Kabul koşulu 3 — güncel tablo

| Sayfa | Durum |
|---|---|
| /raporlar | ✓ (Bicim + Raporlar:391; en-US kültürde test) |
| /raporlar/satis-analizi | ✓ (SatisAnalizi:174) |
| /siparisler/{Id} | ✓ (SiparisDetay:345-346) |
| /islemler | ✓ (ilk tur: Bicim dışı Invariant görüntü satırı yok) |
| /vezne | **✗ — Vezne.razor:300 açık (S-2b dosyası)** |

### 14.9 DoD sözleşmesi (bu tur)

Yeni varlık/kayıt türü, uç, düğme, form, id **yok**; yalnız üç görüntü ifadesi. Sozlukler'e metin eklenmedi.

### 14.10 Açık kalanlar (bu tur)

1. **Vezne.razor:300** → `@Bicim.Miktar(k.Adet, null)` — S-2b bitince (sahibi S-2b; ya da orkestratör S-2b'ye iletir). bUnit:
   Vezne detayında `adet:1.5` fikstürü + `P57EkNoktaOndalikEkranTestleri` desenindeki regex.
2. Test dosyası yeri: `RaporlarEkranTestleri.cs` yerine `P57RaporBicimEkranTestleri.cs` / `P57EkNoktaOndalikEkranTestleri.cs`
   (SERİT D artık bitti; taşıma orkestratörün kararına).
3. Denetim ✗ ve tam süit 6 kırmızı: SERİT G (`SatirEylemleri.razor` record) ve SERİT F (`OnayKipi.razor` ham input,
   Fiyatlandirma/Vitrin onay testleri) — sahiplerine.
4. `Kültürsüz ToString` cırcır tavanı 27 → 26'ya indirilebilir (orkestratör).
5. Ağaçtaki `.mutorj`/`.bak` artıkları (21.09 21:40, P-57'ye ait değil) — silme kurucuda.

### 14.11 Değişen dosyalar (bu tur)

```
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                       (391: Bicim.Miktar)
?? dotnet/Kuyumcu.Web/Pages/SatisAnalizi.razor                   (174: Bicim.Yuzde) — dosya git'te izlenmiyor
?? dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor                   (345-346: Bicim.Miktar) — dosya git'te izlenmiyor
   dotnet/Kuyumcu.Tests/P57RaporBicimEkranTestleri.cs            (kesirli stok adedi + en-US kültür)
?? dotnet/Kuyumcu.Tests/P57EkNoktaOndalikEkranTestleri.cs        (YENİ, 2 test)
```
Bicim.cs bu turda değişmedi. Proje ağacına geçici dosya bırakılmadı.

---

## 15. TUR 3 (2026-09-24 06:03–06:30) — /vezne kalem adedi (doğrulayıcı KRİTİK) kapatıldı

**Durum: BİTTİ.** Kabul koşulu 3'ün son açığı (/vezne) kapandı. Paket kapısının yedi maddesi karşılandı. Tam süit 0 kırmızı, denetim TEMİZ.

### 15.1 Sahiplik ölçümü (Vezne.razor)

- `ORKESTRATOR-DURUM.md` 05:35 kaydına göre S-2b'nin son paketi S2-6'ydı. `PAKET-S2-6-uygulama-raporu.md` (05:49) **"Durum: BİTTİ"** diyor. S2-6'nın dosyaları `Islemler.razor` + `Islemler.Diger.cs`; raporda "vezne" geçmiyor.
- `Vezne.razor`'un son değişikliği **23.09 20:06**. S-2b bu dosyayı bu gece açmadı.
- Sonuç: doğrulayıcının "S-2b kapıdan geçince tek satır" şartındaki dosya artık boşta. Yalnız **tek görüntü ifadesi** değişti. id, düğme ve form değişmedi. TEK DOKUNUŞ gerekçesi: sayfa bu dalgada ilk kez, yalnız P-57 kabul koşulu 3 için açıldı. **Dokunulan sayfalar (tur 3): Vezne.razor.**

### 15.2 Değişiklik

| Satır | Önce | Sonra | Örnek |
|---|---|---|---|
| Vezne.razor:300 (`#vz-detay-kalemler` adet hücresi) | `@k.Adet.ToString("0.###", CultureInfo.InvariantCulture)` | `@Bicim.Miktar(k.Adet, null)` | 1.5 → "1,5" · 2 → "2" |

Bicim.cs bu turda değişmedi.

### 15.3 Test (YENİ, 1)

- `P57EkNoktaOndalikEkranTestleri.cs` dosyasına yeni sınıf eklendi: `P57.VezneDetayEkranTestleri.KalemAdedi_TurkceVirgulBasar_NoktaOndalikIcermez`.
- Kuyrukta bir VEZNEDE fiş var. Detayda **`adet:1.5`** (kesirli) kalem var. Sahte API gerçekçi yanıt veriyor: kuyruk, detay, kasalar, cari, ürünler, kur, auth/ben.
- Test **en-US CurrentCulture** altında koşar. `#vz-sec-v1` tıklanır, `#vz-detay-kalemler` tek satır okunur ve üç şey doğrulanır: ad "Küpe", adet **"1,5"**, `NoktaOndalikDenetimi` regex'i temiz.
- **Değişiklikten ÖNCE** (eski satırla) koşuldu → KIRMIZI (`Expected "1,5" · Actual "1.5"`). Değişiklikten SONRA → yeşil.
- Filtreli koşum (`P57 | Kuyumcu.Tests.BicimTestleri | VezneEkranTestleri`): **35/35 geçti**. Mevcut 25+ vezne ekran testi bozulmadı.

### 15.4 Mutasyonlar (yedek `scratchpad/mut-yedek/P-57-t3/` → tek eşleşmeli perl → `dotnet build --no-incremental` → `dotnet test --no-build` filtreli → geri kopya → `cmp`)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| **M57-7** (doğrulayıcının istediği) | Vezne.razor:300 eski `k.Adet.ToString("0.###", CultureInfo.InvariantCulture)` geri geldi | `P57.VezneDetayEkranTestleri.KalemAdedi_TurkceVirgulBasar_NoktaOndalikIcermez` (Expected "1,5", Actual "1.5") | birebir |
| M57-1 (tazelendi) | `Bicim.Has` → `InvariantCulture` | `BicimTestleri.Has_TurkceVirgulOndalikBasar` (+ `P57.RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez`, `EmanetEkranTestleri.Elimizdeki_emanet…`, `UX1C3…C3_41…`, `S35…S35_14…`) | birebir |
| M57-2 (tazelendi) | `Bicim.Makine(decimal)` → `Tr` | `BicimTestleri.Makine_InvariantKalir_ApiSozlesmesiBozulmaz` (1 başarısız / 4) | birebir |

Not: `dotnet test --no-incremental` MSB1001 ("Bilinmeyen anahtar") verdi. O koşum kanıt sayılmadı. Mutasyon ayrı `dotnet build --no-incremental` ve ardından `dotnet test --no-build` ile tekrarlandı.

Son mutasyondan sonra iki şey yapıldı:
- `cmp` ile Vezne.razor ve Bicim.cs yedeklerle **birebir** çıktı.
- Temiz `dotnet build Kuyumcu.sln --no-incremental` → **0 Hata, 1 Uyarı**. Uyarı `EtiketEkranTestleri.cs(145,47) xUnit1031`, SERİT D dosyası, bu paketin değil.

### 15.5 Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_e`, trx `scratchpad/trx-pP-57-t3/_Mac_2026-09-24_06_17_28.trx`)

**4946 toplam · 4946 geçti · 0 başarısız** (trx `Counters failed="0"`).
- Tur 2'deki 6 başka-şerit kırmızısı (SERİT F/G) bu koşumda yok.
- Doğrulayıcının andığı `KilitGerekceTestleri.ManuelKurKaydiIptalYoluYazili` da bu koşumda yeşil.
- Bu paketin 8 testi trx'te "Passed": `BicimTestleri` ×4, `P57.RaporEkranTestleri`, `P57.SatisAnaliziEkranTestleri`, `P57.SiparisDetayEkranTestleri`, `P57.VezneDetayEkranTestleri`.
- Kapı maddesi 7: **yeni kırmızı farkı = 0**.

### 15.6 Denetim (`bash dotnet/denetim.sh`, 06:2x)

**"DENETİM TEMİZ."** (çıkış 0). `Kültürsüz biçimli ToString (Web) 26 (tavan 27)` ✓. Vezne.razor'da kaldırılan satır kültürlüydü (Invariant), bu yüzden sayaç değişmedi.

### 15.7 Canlı doğrulama (port 5184, `kuyumcu_net`, kiracı `PAKETP-57-T3-TEST Kuyumculuk` = `01M38PR674E4CFQ7H8HQXVG7GG`)

Başlangıç: `COUNT(*) tenants` = **10**, `QA-TEST-%` = **4**.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | tek kullanımlık davet (SHA-256 jeton özeti, `kiraci_davetleri`) → `POST /api/v1/auth/kayit` | 201 | **201**, anahtarlar `tenantId, accessToken, refreshToken` |
| 2 | `GET /kur/guncel` | kur id | var (`01M38PRFWG…`) |
| 3 | `POST /cari` (MUSTERI) | 201 | **201** |
| 4 | `POST /urunler` (ZIYNET, K22, `gramAgirlik:3.5`) | 201 | **201**, `gramAgirlik:3.5000` |
| 5 | `POST /vezne/fisler` kalem **`adet:1.5`**, `birimFiyatTl:4900.00`, `iscilikTl:100.00` (Makine sözleşmesi, nokta) | 200 | **200**, `durum:ACIK`, `toplamTl:7450.00`, `toplamTahmini:true` |
| 6 | `GET /vezne/fisler/{id}` | 200, JSON'da nokta ondalık (API sözleşmesi değişmedi) | **200**, `"adet":1.500, "birimFiyatTl":4900.00, "tutarTl":7450.00` |
| 7 | `GET /_framework/Kuyumcu.Web.wasm` | bu turun derlemesi | **200**, 3 068 697 bayt; SHA-256 `fa1d96a5adde275a…` = `Kuyumcu.Web/bin/Debug/net8.0/wwwroot/_framework/Kuyumcu.Web.wasm` (06:15:48). Vezne.razor düzeltilmiş hâliyle 06:15:13'te geri yazılmıştı. |

SQL: `select adet, birim_fiyat_tl, iscilik_tl from vezne_fis_kalemleri where tenant_id='01M38PR6…'` → `1.500 · 4900.00 · 100.00`.
Ekran bu JSON'u `Bicim.Miktar(1.500m, null)` ile **"1,5"** basar. bUnit M57-7 aynı JSON biçimini kullanıyor. Tarayıcıda oturum açılmadı, çünkü parola girişi ajanlara kapalı.

**Temizlik:**
- API durduruldu: PID 89712 + dinleyen 89983 `kill` edildi. `lsof -nP -iTCP:5184 -sTCP:LISTEN` → **boş**.
- `tenant_id` sütunu olan **70 tablodan** kiracı satırları silindi, kalan toplam **0**. `tenants` satırı ve davet satırı da silindi.
- Son sayımlar: `COUNT(*) tenants` = **10** (başlangıç 10). Bir ara ölçümde 12 görüldü; iki fazlalık başka şeritlerin kısa ömürlü test kiracılarıydı ve yeniden ölçümde 10'a döndü. `PAKETP-57%` = 0, P-57 davet = 0, `QA-TEST-%` = **4** (dokunulmadı).
- Jeton ve yanıt içeren scratchpad dosyaları `printf ""` ile boşaltıldı.

### 15.8 Kabul koşulları — son tablo

| Koşul | Durum | Kanıt |
|---|---|---|
| `Bicim.Has(77.6014m)` = "77,6014 has"; `Bicim.Gram(56.991m)` = "56,991 g" | ✓ | `BicimTestleri.Has_…`, `Gram_…` Passed; M57-1 kırmızı |
| `Bicim.Makine` değişmedi (nokta, Invariant) | ✓ | `Makine_InvariantKalir_…` Passed; M57-2 kırmızı; canlı JSON `adet:1.500` |
| /raporlar · /islemler · /vezne · /siparisler/{Id} · /raporlar/satis-analizi'de ekranda '.' ondalık yok | **✓ (5/5)** | Raporlar, SatisAnalizi, SiparisDetay: tur 2 · **Vezne: tur 3 (M57-7)** · Islemler: tur 1 (Bicim dışı Invariant görüntü satırı yok; kalan tek Invariant satır :525 bir API sorgu dizgesi) |
| dotnet test 0 başarısız, denetim TEMİZ | ✓ | 4946/4946 · DENETİM TEMİZ |

### 15.9 DoD sözleşmesi (tur 3)

Yeni varlık, uç, düğme, form ya da id **yok**. Tek değişiklik bir görüntü ifadesi. Sozlukler'e metin eklenmedi.

### 15.10 Açık kalanlar (tur 3)

1. **[YÜKSEK — P-57 kapsamı DIŞI, girdi hatası; sahibi: S-2b alanı / yeni paket]** `Vezne.razor:1004-1005` `Sayi()` şöyle: `decimal.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, …)`. Vezne fiş formundaki `Metin` alanlarına (`vz-f-adet-*`, `vz-d-adet-*`, tutar) kullanıcı Türkçe **"1,5"** yazarsa değer **15** olarak ayrışır. Bu ölçüldü: `dotnet fsi` → `1,5 -> true 15`. Formu düzenleme için doldurma satırları (:772, :807, :816-818, :957) Invariant biçimde. Doldurma ve ayrıştırma birbiriyle tutarlı, yani spesifikasyonun "görüntü biçimiyle doldurulup ayrıştırılan" maddesine girmiyor. Ancak serbest metin girdisinde virgül binlik ayırıcı sayılıyor ve bu para ile adet hatası doğurur. P-39 girdi ailesinin (`SayisalAlan`) kullanılması ya da tr-TR/Invariant ikili ayrıştırma gerekir. Paket P-57'nin dosya sınırı ve "görüntü" kapsamı dışında kaldığı için **düzeltilmedi**. Orkestratöre öneri: ayrı paket.
2. Test dosyası yeri: `RaporlarEkranTestleri.cs` yerine `P57RaporBicimEkranTestleri.cs` (sınıf `Kuyumcu.Tests.P57.RaporEkranTestleri`, test adı spesifikasyondakiyle aynı). Taşımak dosya silmeyi gerektirir, bu da yasak. Karar orkestratörde.
3. Şerit dosya sınırı genişletmesi (SatisAnalizi, Raporlar, SiparisDetay: tur 2; Vezne: tur 3) orkestratörün kapı kaydına işlenmeli. Her biri tek ifadelik ve mutasyonla korunuyor.
4. `Kültürsüz ToString` cırcır tavanı 27 → 26'ya indirilebilir (orkestratör).
5. Tur 2'deki SERİT F/G kırmızıları ve `KilitGerekceTestleri` bu koşumda yeşil, yani açık değil.

### 15.11 Değişen dosyalar (tur 3)

```
?? dotnet/Kuyumcu.Web/Pages/Vezne.razor                    (300: Bicim.Miktar — dosya git'te izlenmiyor)
?? dotnet/Kuyumcu.Tests/P57EkNoktaOndalikEkranTestleri.cs  (+ VezneDetayEkranTestleri, 1 test)
 M dokumanlar/09-uygulama-kayitlari/PAKET-P-57-uygulama-raporu.md (§ 15)
```
Paketin tümü: Bicim.cs, Raporlar.razor, SatisAnalizi.razor, SiparisDetay.razor, Vezne.razor, BicimTestleri.cs, P57RaporBicimEkranTestleri.cs, P57EkNoktaOndalikEkranTestleri.cs ve § 2.1'deki test sabitleri. Proje ağacına geçici dosya bırakılmadı. Yedekler `scratchpad/mut-yedek/P-57-t3/` altında.
