# PAKET P-46 — Vezne kuyruğunda gerçek fiş toplamı (kuyruk = detay) · uygulama raporu

- **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · dalga PARA VE SAYI DOĞRULUĞU (tur 8 triyajı)
- **Tarih:** 2026-09-23, yaklaşık 19:50 – 20:15 (**gerçek süre ≈ 25 dk**; triyaj tahmini 2 saatti. Bu tahmin değil, ölçülen süredir)
- **Durum beyanı:** BİTTİ. Paket kapısının 7 maddesi sağlandı (§ 11). Tam süitteki 5 kırmızının hepsi başka şeridin (S-1 KABUK, yeni `/ekranlar` sayfası) işi (§ 7)

## 0. Devralma ölçümü (yarım izi)
Devir notu, şeridin P-46'nın ortasında durduğunu söylüyordu. Ağaçta önce ölçüm yaptım:
- `grep P46|P-46|damgaliToplam` → P-46'ya ait **hiçbir iz yok**. `damgaliToplamTl` (detay ucu) ve `Sozlukler.DamgaliToplam` anahtarı P-46'dan önce de vardı. `VezneEkranTestleri`/`P12EkranTestleri` fikstürlerinde de bulunuyorlardı (dosya tarihleri 21–22.09).
- `VezneModulu.cs` son değişiklik 22.09 02:57 · `VezneTestleri.cs` 22.09 02:44. Yani paket **sıfırdan** uygulandı; hiçbir adım "yapılmış" sayılmadı.

## 1. Spesifikasyonun koda karşı doğrulanması
| İddia (triyaj) | Ölçüm | Sonuç |
|---|---|---|
| `VezneModulu.cs:411-412` kalemli+ACIK dalı damgasız `f.ToplamTl`'ye (=0) düşüyor | 408-412: `toplamTl = f.Durum == Acik && !Kalemli ? TalepTutarTl : f.ToplamTl` | **DOĞRU** |
| Detay aynı durumda 472-478'de `Toplam(kalemler)` hesaplıyor | 472-478 birebir | **DOĞRU** |
| Dosya ayak izi: 5 dosya | 5'i de `test -e` ile VAR | **DOĞRU**, ama **bir sapma** var: istemci yanıt modelleri `Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs` dosyasında duruyor (S-2/MÜHÜR 5 gereği, Pages içinde record yasak). Bu dosya ayak izinde yoktu ama değişmesi zorunluydu |
| (b) `Dil.cs` yok, `DilServisi.cs` var | Bu paket o dosyaya dokunmadı | — |
| (c) `SatirIciDuzenle` zaten var | Bu paket yeni düzenleme bileşeni yazmadı | — |

Mevzuat: paketin hiçbir mevzuat hükmü yok (yalnız gösterim ve toplama). Birincil kaynak araştırması gerekmedi.

## 2. Yapılanlar
**Backend (`Kuyumcu.Api/Modules/Vezne/VezneModulu.cs`)**
- **`GosterilecekToplam(durum, kalemli, talepTutarTl, damgaliToplamTl, kalemler)`**: gösterilecek toplamın TEK kaynağı. ACIK+kalemli fişte kalem toplamı, ACIK+kalemsiz fişte talep tutarı, diğer her durumda M-V10 damgası döner. `Kuyruk` ve `Detay` bu yardımcıyı çağırıyor; kopyala-yapıştır hesap kaldırıldı.
- **`ToplamTahmini(durum)`** ⇒ ACIK. Kuyruk satırında ve detayda yeni `toplamTahmini` alanı var. Kuyruk satırına ayrıca `damgaliToplamTl` alanı eklendi; detayda bu alan zaten vardı.
- **`KalemHaritasi(fisIdler, tenantId)`**: kuyruktaki ACIK kalemli fişlerin kalemleri **tek sorguda** (`WHERE fis_id IN (…)`) çekiliyor ve bellekte fiş kimliğine göre gruplanıyor. Damgalı fişlerin kalemleri hiç okunmuyor. Liste boşsa veritabanına gidilmiyor. Sorguda `.Take(fisIdler.Count * AzamiKalem)` var. Bu gerçek üst sınırdır, çünkü kalem sayısı girişte `KalemleriKur` ile doğrulanıyor. B-8 "sınırsız ToListAsync" tavanı 145'te kaldı (P-26 emsali).
- Kalem başına yuvarlama, damga anındaki `Toplam()` ile aynı fonksiyondan geçiyor. Bu yüzden gösterilen tahmin ile vezneleme anında damgalanan tutar kuruşu kuruşuna aynı çıkıyor.
- **M-V10 bozulmadı**: `Veznele` damga satırı (`fis.ToplamTl = kalemli ? Toplam(kalemler) : …`) değişmedi. ACIK fişin `toplam_tl` kolonu 0 kalıyor (canlıda SQL ile kanıtlandı, § 8).

**İstemci sözleşmesi (`Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs`)**
- `VezneKuyrukSatiri` + `DamgaliToplamTl = 0m, bool ToplamTahmini = false` (sonda, varsayılanlı)
- `VezneDetayYaniti` + `bool ToplamTahmini = false, DateTime? Veznelendi = null`

**Ekran (`Kuyumcu.Web/Pages/Vezne.razor`)**
- Kuyrukta tutar hücresi artık `id="vz-toplam-{id}"`. ACIK fişte yanında sarı rozet var: `vz-toplam-rozet-{id}`, metni "Tahmini — vezneye gönderilince kesinleşir". Veznelenmiş fişte gri rozet: "Kesin — vezneye gönderildiği an sabitlendi".
- Hiç veznelenmemiş, ACIK'tan iptal edilmiş fişte rozet çizilmiyor. Bu fişe "kesin" demek yanlış olurdu.
- Detay kartında aynı ayrım `vz-detay-toplam-rozet` ile gösteriliyor.
- Mevcut element id'lerinin hiçbiri değişmedi. Yeni düğme, ham input, satır içi style ya da sabit renk yok (rozet `rozet-sari`/`rozet-gri` belirteç sınıflarını kullanıyor).

**Sözlük (`Kuyumcu.Core/I18n/Sozlukler.cs`)**: `Vezne.ToplamTahmini`, `Vezne.ToplamDamgali` (TR+EN).

**Sahte API (`VezneEkranTestleri.DoluKuyruk`)**: v2 (ACIK) artık gerçekçi yanıt veriyor: `toplamTl 2150`, `damgaliToplamTl 0`, `toplamTahmini true`. v1 (VEZNEDE): `damgaliToplamTl 5000`, `toplamTahmini false`.

## 3. DoD sözleşmesi
P-46 **yeni varlık ya da kayıt türü doğurmadı**. Migration yok, yeni uç yok. Yalnız mevcut iki GET yanıtına salt okunur hesaplanmış alan eklendi.
| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Vezne fişi (ACIK) | `PATCH /vezne/fisler/{id}` (mevcut) + ekranda "Düzenle" | Kalem düzenlenince kuyruk tutarı da değişir (P46_02) |
| Vezne fişi (VEZNEDE+) | Değişmez: mevcut `DegismezIpucu` (gerekçeli iptal → yeni fiş) | Damga yeniden hesaplanmaz (P46_03) |
| `toplamTl` / `damgaliToplamTl` / `toplamTahmini` | Salt okunur, hesaplanmış alanlar | Kullanıcı girdisi değil |

## 4. Migration
Gerekmedi. Şema değişmedi.

## 5. Testler
Yeni **6 test** yazıldı: 4'ü spesifikasyondan (DB), 2'si ekran için bUnit.
| Test | Tür |
|---|---|
| `VezneTestleri.P46_01_Kuyruk_ve_detay_ayni_fis_icin_ayni_toplami_dondurur` | DB (2×1.000+150 = 2.150; kuyruk = detay; damgalı 0; tahmini=true; `toplam_tl` 0 kaldı) |
| `VezneTestleri.P46_02_Kalem_duzenlenince_kuyruk_toplami_da_degisir` | DB (PATCH 3×1.000+200 = 3.200; kuyruk = detay) |
| `VezneTestleri.P46_03_Veznelenmis_fiste_damgali_tutar_yeniden_hesaplanmaz` | DB (damga 2.150, sonra kalem adedi DB'de 5'e çekiliyor; kuyruk ve detay 2.150'de kalıyor) |
| `VezneTestleri.P46_04_Kuyruk_sorgusu_N_plus_1_acmaz` | DB + EF komut yakalayıcı (1 fişte ve 6 fişte SQL sayısı eşit; kalem sorgusu tek ve `IN`'li) |
| `VezneEkranTestleri.P46_Ekran_ACIK_fiste_hesaplanan_tutari_TAHMINI_rozetiyle_gosterir` | bUnit |
| `VezneEkranTestleri.P46_Ekran_veznelenmis_fiste_DAMGALI_tutari_KESIN_rozetiyle_gosterir` | bUnit |

Filtreli koşum (`P46|Vezne`): **74/74 geçti**. Tam süit: § 7.

## 6. Mutasyon kanıtları
Yöntem: yedek `scratchpad/mut-yedek/P-46/` → `perl -0777 -pi` ile tek değişiklik → `dotnet test --filter P46 -v n` → yedeğin geri kopyalanması → `cmp`.
| Kod | Değişiklik | Kırmızıya dönen testler | cmp |
|---|---|---|---|
| M46-1 | Kuyrukta kalemli+ACIK dalı eski `f.ToplamTl`'ye geri alındı | **P46_01, P46_02** (+P46_04, `toplamTl>0` iddiası) | CMP-AYNI |
| M46-2 | Veznelenmiş fişte de kalem toplamı yeniden hesaplandı (yardımcıda `Acik` → `!= Iptal`, kuyruk tüm kalemli fişleri okudu) | **P46_03** | CMP-AYNI |
| M46-3 | Kalem toplamı fiş başına ayrı sorguyla çekildi (`foreach … await Kalemler(id)`) | **P46_04** (beklenen 5, gerçek 10 komut) | CMP-AYNI |
| M46-4 (ek) | Ekranda rozet hep "Kesin" yazıldı | **P46_Ekran_ACIK_…_TAHMINI_…** | CMP-AYNI |

Son mutasyondan sonra `.Take` sınırı eklendi, yeni yedek alındı ve temiz `dotnet build Kuyumcu.sln` çalıştırıldı: **0 hata**. 2 uyarı var, ikisi de `Kuyumcu.Tests/OlcumKapisiTestleri.cs(525)` CS8604; dosya DALGA 7c'nin, P-46'nın değil.

## 7. Tam süit ve denetim
**Tam süit** (`--logger trx --results-directory scratchpad/trx-pP-46-3`, 20:10:56–20:12:36): **4207 toplam · 4202 geçti · 5 başarısız · 0 atlandı**.

İlk iki koşum geçersiz sayıldı, gizlenmiyor:
- 1. koşum eşzamanlı çalışan başka bir `dotnet test`'in `kuyumcu_test`'i silmesi yüzünden 1878 "Unknown database 'kuyumcu_test'" hatası verdi.
- 2. koşum `.Take` düzeltmesi için durduruldu.
- 3. koşum, başka vstest süreci kalmayınca başlatıldı.

Tam süitteki 5 kırmızı:
| Kırmızı | İleti | Sahibi |
|---|---|---|
| `RotaBaslikTestleri.Yansima_TAM_42_rota_bulur` | beklenen 46, gerçek 47 | S-1 KABUK (yeni `Pages/Ekranlar.razor`, 20:09) |
| `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` | 46 ≠ 47 | S-1 KABUK |
| `IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` | menü rotaları farklı | S-1 KABUK (Layout/menü) |
| `OlcumKapisiTestleri.qa_turu_rota_listesini_page_direktiflerinden_URETIR_ve_eksikse_BASLAMAZ` | 47 ≠ 46 | S-1 KABUK / DALGA 7c |
| `OlcumKapisiTestleri.P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu` | 47 ≠ 46 | S-1 KABUK / DALGA 7c |

**Fark analizi (kapı maddesi 7):** Devir notundaki bilinen kırmızı (`denetim_sh_HER_CAGRI_…`) bu koşumda yeşil. Yeni 5 kırmızının hepsi rota/sayfa sayısı (46→47) ya da menü üzerine. P-46 hiçbir `@page`, menü ya da Layout dosyasına dokunmadı; `Vezne.razor` diff'inde `<button|<input|style=|href` sayısı **0**. **P-46'nın ürettiği yeni kırmızı yok: 0.**

**denetim.sh** (20:0x, ağaç ölçüsü): çıkış 1, "DENETİM BAŞARISIZ". P-46'nın sayaçları ✓:
- `Sınırsız ToListAsync 145 (tavan 145)`. İlk ölçümde 146 idi ve P-46'nın ürünüydü; `.Take` ile düzeltildi.
- `Vezne/ altında defter yazımı 0`, `Vezne → SatisFisi ≥1`, `ModalPencere 55 (tavan 55)`.

Kalan ✗'ler P-46'ya ait değil:
- **S-1 KABUK** (Pages dosya sayısı 47 > 46, butonlar 74 > 71, `HAM_INPUT` 43 > 42, `HAM_INPUT_SERBEST` 1, `P-37c SAYFA_BASI_KAPSAM acik=1`, `P-14 MENU_ELLE_BAGLANTI`, `P-02 OLCUM_TABANI_KOPYA` rota 47/46): hepsi yeni `/ekranlar` sayfası ve menü üzerine.
- **DALGA 7c:** ölçüm tabanı.

## 8. Canlı doğrulama (localhost:5198 · `kuyumcu_net`)
Test kiracısı **`PAKET46-TEST Kuyumculuk`** (`01M37KT9XDRK7J6AE3DB0EAJ1Y`) davet satırı elle eklenerek açıldı. 5199'a dokunulmadı; koşum sonunda 2 dinleyici hâlâ ayaktaydı.
| # | İstek | Beklenen | Gerçek | Yanıt |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | tenantId + accessToken |
| 2 | `GET /kur/guncel` | 200 + id | **200** | id var |
| 3 | `POST /urunler` (ZIYNET, K22, 10 g) | 201 | **201** | ürün id |
| 4 | `POST /vezne/fisler` 2×1.000 + 150 | 200 ACIK | **200** | `durum: ACIK` |
| 5 | `GET /vezne/kuyruk?durum=ACIK` ↔ `GET /vezne/fisler/{id}` | eşit 2.150 | **200 / 200** | kuyruk `toplamTl=2150.0, damgaliToplamTl=0.0, toplamTahmini=true` · detay `toplamTl=2150.0, toplamTahmini=true` |
| 6 | `PATCH /vezne/fisler/{id}` 3×1.000 + 200 | 200 | **200** | `toplamTl=3200.0` |
| 7 | kuyruk ↔ detay | eşit 3.200 | **200 / 200** | `3200.0` = `3200.0` |
| 8 | `POST /vezne/fisler/{id}/veznele` | 200 VEZNEDE | **200** | `VEZNEDE`, `toplamTl=3200.0` |
| 9 | Kalem adedi DB'de 5'e çekildi → `GET /vezne/kuyruk?durum=VEZNEDE` + detay | damga (3.200), yeniden hesaplanmaz | **200 / 200** | kuyruk `toplamTl=3200.0, damgaliToplamTl=3200.0, toplamTahmini=false` · detay `3200.0` (yeniden hesaplansaydı 5.200 olurdu) |

**SQL kanıtı:**
- ACIK iken `SELECT durum,toplam_tl FROM vezne_fisleri` → `ACIK 0.00` (damga yazılmadı, M-V10)
- Veznelendikten sonra → `VEZNEDE 3200.00`
- `vezne_fis_kalemleri` → `5.000 1000.00 200.00` (değiştirilmiş kalem; gösterim yine 3.200)

N+1: canlıda SQL sayımı yapılmadı. Ölçüm, EF komut yakalayıcısıyla `P46_04` testinde yapıldı ve M46-3 mutasyonu 5→10 komut farkını yakaladı.
Ekran: tarayıcıda canlı açılmadı. Rozet ve tutar, bUnit ile sunucunun gerçek yanıt şekline uyan fikstürle doğrulandı.

**Temizlik:**
- API PID'leri (92337 `dotnet run`, 92680 dinleyici) `kill` ile durduruldu. `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş** döndü.
- `tenant_id` kolonu olan **70 tablonun tamamından** kiracı satırları, `tenants` satırı ve `kiraci_davetleri` satırı silindi. Kalan satır denetimi **0 satır** döndü.
- `SELECT COUNT(*) FROM tenants` → **başlangıçta 10, sonda 10**. `QA-TEST-*` kiracılarına dokunulmadı.

## 9. Değişen dosyalar (P-46)
`git status --porcelain | grep -v -E "/(bin|obj)/"` Vezne dosyalarını `??` gösteriyor (modül 19'dan beri izlenmiyor, commit yok). P-46'nın dokunduğu dosyalar:
- `dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs`
- `dotnet/Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs` (ayak izi dışı, gerekçesi § 1)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (HUB, +6 satır)
- `dotnet/Kuyumcu.Web/Pages/Vezne.razor`
- `dotnet/Kuyumcu.Tests/VezneTestleri.cs`
- `dotnet/Kuyumcu.Tests/VezneEkranTestleri.cs`

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/Vezne.razor`. Bu dosya bu dalgada P-58, P-60, P-63 ve P-71 ile çakışıyor; dosya 18:02'de başka bir işle değişmişti. P-46'nın değişikliği yalnız iki toplam hücresine rozet eklemekten ibaret: 2 blok, mevcut id'ler korundu.

## 10. Kabul koşulları karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Kalemli ACIK fişte kuyruk `toplamTl` = detay `toplamTl` (2.150 ve düzenleme sonrası 3.200) | ✅ | Canlı #5, #7 · P46_01, P46_02 |
| Ekran kuyrukta 0,00 yerine gerçek tutarı gösterir; damgalanmadığı rozetle belirtilir | ✅ | `P46_Ekran_ACIK_…` (`2.150,00` + `rozet-sari` "Tahmini") · M46-4 |
| Vezneleme sonrası damgalı tutar gösterilir, yeniden HESAPLANMAZ | ✅ | Canlı #9 · P46_03 · M46-2 |
| Kuyruk sorgusu fiş sayısından bağımsız sabit sayıda SQL atar | ✅ | P46_04 (1 fiş = 6 fiş komut sayısı) · M46-3 (5 → 10) |
| Tek yardımcı, kopyala-yapıştır yok | ✅ | `GosterilecekToplam` iki uçtan çağrılıyor |
| `dotnet test` 0 başarısız / denetim TEMİZ | Paket ölçüsünde ✅ · ağaç ölçüsünde ✗ (orkestratör kapısı) | Paralel şerit kapısı: P-46 kaynaklı yeni kırmızı 0, P-46 sayaçları ✓ (§ 7) |

## 11. Paket kapısı (7 madde)
1. build 0 hata ✅
2. P-46 testleri 6/6 yeşil ✅
3. P-46 sayaçları ✓ (B-8 145/145) ✅
4. Mutasyon 4/4 kırmızı + cmp ✅
5. Canlı doğrulama ✅
6. Test kiracısı silindi, tenants 10→10 ✅
7. Yeni kırmızı farkı 0 ✅

## 12. Açık kalanlar
- **S-1 KABUK:** `Pages/Ekranlar.razor` (47. sayfa) ve menü değişikliği 5 testi ve denetim.sh'daki 8 ✗'yi kırıyor (§ 7 tablosu). Rota haritası, sayfa tavanı, buton, ham input, SAYFA_BASI ve ölçüm tabanı güncellenmeli.
- **DALGA 7c:** `OlcumKapisiTestleri.cs(525)` iki CS8604 uyarısı (0 uyarı hedefi).
- **Test altyapısı riski:** paralel şeritler aynı `kuyumcu_test` veritabanını kullanıyor. Eşzamanlı iki tam süit birbirini "Unknown database" ile düşürüyor (bu pakette gerçekleşti, 1878 sahte kırmızı). Orkestratöre öneri: tam süit tek sıraya alınmalı ya da şerit başına ayrı test DB adı kullanılmalı.
- `dotnet build`'de artımlı derleme bir kez bayat Core referansıyla CS1061 verdi. `--no-incremental` ile giderildi; eşzamanlı derlemelerin yarışından kaynaklandığı düşünülüyor. Bu bir çıkarımdır, nedeni ölçülmedi.

## 13. Kurucuya / mali müşavire not
Mevzuat içeriği yok. Kullanıcıya görünen metin: ACIK fişin tutarı "Tahmini — vezneye gönderilince kesinleşir". Tahsil edilecek tutar, vezneleme anında sabitlenen tutardır (M-V10).
