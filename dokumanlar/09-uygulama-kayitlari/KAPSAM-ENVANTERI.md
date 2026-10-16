# KAPSAM ENVANTERİ — her kimlik, kaynağı ve triyaj kuralı (PAKET P-00)

> **Üretim:** 2026-09-18, DALGA 0 / P-00 · **Kapsam kimlikleri:** B-50 · B-51 · B-58 · R-16 · Y-42
> **Kural (PLAN § 1 md.6):** her kimlik bir **kaynağa** bağlanır; kaynağı bulunamayan kimlik açıkça *"bu oturumda sentezlendi"* diye işaretlenir. Bu belge yayımlanmadan hiçbir dalga başlamaz (BK-18).
> **Anayasa md.1:** aşağıdaki her "Kaynak" hücresi **gerçek bir dosya satırıdır**, tahmin değildir; satır numarası belge bugünkü hâliyle `sed -n` ile doğrulanabilir.

## 1. Sayım (dürüst)

| Küme | PLAN § 12 | Bu envanter | Fark nereden |
|---|---|---|---|
| B — eski borç | 58 | 72 | QA turu 4/5/6 bulgularının triyajı 12 yeni tema kimliği doğurdu (B-59…B-70) |
| Y — yeni iş | 42 | 44 | PLAN § 13 eki: Y-43, Y-44 (QA turu 6) |
| R — karar | 19 | 19 | — |
| D — dondurulmuş | 13 | 13 | — |
| K — kurucu | 26 | 26 | — |
| **TOPLAM** | **158 (+2 = 160)** | **172** | **Gerileme değil, dürüst sayım (PLAN § 2 P-00 risk satırı)** |

**Kökeni bulunamayan kimlik (KIMLIK_KOKENSIZ): 0.** Kaynağı belgeden değil bu plan oturumundan gelen kimlikler ayrıca işaretlidir (aşağıda `SENTEZ` etiketi).

## 2. Kimlik → kaynak tablosu

| Kimlik | Dalga | Paket | Konu | Kaynak (dosya:satır) | Kaynak satırından alıntı |
|---|---|---|---|---|---|
| B-1 | D1 | P-02 | QA ölçüm kapısı: 42/42 rota, öykünme kapalı | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:104` | - DOĞRULANMALI: 'S-3f sonrası 11 bulgu daha kapandı' iddiası ölçülmedi (QA turu 6 koşulmadı) — bu turda kapanmış saymadı |
| B-2 | D1 | P-02 | Yayın anlık görüntüsü + ortam/sürüm damgası | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:32` | \| 2 \| Yayın anlık görüntüsü BAYAT ve kendini 'Geliştirme' sanıyor — yanlış sürüm ölçülüyor \| Köprü, Bertan \| KRİTİK  |
| B-3 | D5 | P-22 | TASLAK fatura ve kalemleri düzenlenebilsin | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:163` | - **`/faturalar` — TASLAK fatura ve kalemleri hiç düzenlenemiyor.** Kanıt: bu turda oluşturulan `FTR-2026-000005` "Tasla |
| B-4 | D2 | P-06 | Düzenlenemeyen veri girişi → 0, şablon kuralı | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:95` | - **[KRİTİK] 49 veri girişi hâlâ düzeltilemiyor — kurucunun 'kalem = 0' satış kapısı düştü** — QA turu 5 ozet.duzenlenem |
| B-5 | D7 | P-33 | Temiz makinede sıfırdan kurulum tatbikatı | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-3.md:272` | - **[KRİTİK] Satılabilirlik ayağının tamamı hâlâ 0 satır: kurulum, yedek, veri göçü, açılış sihirbazı** — ORKESTRATOR-DU |
| B-6 | D2 | P-09 | MFA uçtan uca canlı ölçüm | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:39` | \| 9 \| MFA canlı ölçümü iki turdur yapılmadı; yarım kalan kurulum temizlenemiyor (kilitlenme riski) \| Mehmet, Eray, Ra |
| B-7 | D6 | P-24 | Türetilmiş özet ↔ liste toplamı mutabakatı | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:41` | \| 11 \| Türetilmiş özet tek doğruluk kaynağından ayrışıyor: vezne kuyruğu ₺0,00, evrak portföyü boş, kâr-zarar 11,45 g  |
| B-8 | D6 | P-25 | 34-Ö ölçek + kültür borcu | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-3.md:153` | - **[YÜKSEK] Ortak pasta grafik bileşeni tr-TR ondalıkla kültür hatası veriyor (çoklu sayfa)** — tur3.md konsol: '<path> |
| B-9 | D4 | P-18 | Mobil parite, 42/42 rotada 375 px | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:50` | \| 20 \| Ölçek borcu ve mobil parite: 171 sınırsız ToListAsync (15'i rapor), 5 rota 375 px'te taşıyor \| Bertan, Mustafa |
| B-10 | D2 | P-07 | 429 bandı + geri çekilme + jeton yarışı | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:186` | 21. **`/kullanicilar` 429'da sayfa sessizce boş kalıyor (HATA).** Kanıt: `GET kullanicilar` 429 (2×), tabloda tek satır  |
| B-11 | D7 | P-33 | Kılavuzun tek kaynaktan yeniden yazımı | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:191` | 26. **`/kilavuz` ekranların büyük bölümünü hiç anlatmıyor.** Kanıt: 18 başlık arasında Vezne, Emanet, POS, Sayım, Uyum,  |
| B-12 | D3 | P-13 | /uyum GONDERILDI kaydında düzeltme yolu | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:210` | `/islemler` satış fişi (iade / ters kayıt yolu yazılı) · `/vezne` VEZNEDE fişi (gerekçeli iptal + yeni fiş) · `/banka` d |
| B-13 | D2 | P-06 | Ortak SatirIciDuzenle sözleşmesi (Enter/odak) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:172` | 7. **`/sayim` satır içi "Sayılan"da Enter kaydetmiyor.** Kanıt: ipucu "Enter kaydeder" diyor; gerçek klavyeyle "3"+Enter |
| B-14 | D3 | P-11 | /kur hata davranışı (503'te kart kaybolması) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:170` | 5. **`/kur` başarısız "Şimdi çek" ekrandaki kur kartını ve ayar tablosunu siliyor (HATA).** Kanıt: 503 sonrası `main` iç |
| B-15 | D3 | P-11 | /kur geçmişi "İptal et" etkisiz | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:169` | 4. **`/kur` geçmişteki "İptal et" hiçbir şey yapmıyor (HATA).** Kanıt: hem JS `.click()` hem gerçek fare tıklamasıyla hi |
| B-16 | D3 | P-11 | Geri alınamayan QA5 kalıntısı (MANUAL kur) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:247` | - **Geri ALINAMAYAN tek değişiklik (Md.1):** `/kur` ekranındaki "İptal et" düğmesi çalışmadığı için girilen manuel kur * |
| B-17 | D3 | P-13 | Kuruluş sihirbazı (K-24'e bağlı dilim) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:185` | 20. **`/kurulum` kuru prova NEGATİF açılış bakiyesini geçerli sayıyor (HATA).** Kanıt: kasa −250 ₺ → `prova` 200, "Tüm s |
| B-18 | D2 | P-06 | Alan altı doğrulama sözleşmesi + iş kuralı sınırları | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:175` | 10. **`/sayim` zorunlu "Şube" boşken hiçbir alan işaretlenmiyor.** Kanıt: `#sy-sube.className='secim'`, `aria-invalid=nu |
| B-19 | D3 | P-12 | Storno görünürlüğü + 48-A parti geri alma | `RAKIP-KIYAS-RAPORU-2026-09-16.md:51` | \| Storno / denetim izli düzeltme motoru \| YOK \| 48-A (2b) \| |
| B-20 | D2 | P-09 | MFA API borçları (kurtarma kodu, uç adları, iz) | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:304` | - KURTARMA KODU ve MFA MEYDAN OKUMA JETONU yüksek entropili rastgele değerlerdir; refresh token deseniyle birebir SHA-25 |
| B-21 | D1 | P-02 | BEKLENEN_VARLIK sayacı gerçek sayıya | `dotnet/denetim.sh:293` | BEKLENEN_VARLIK=51 |
| B-22 | D3 | P-11 | /bildirimler basit mod menüsünde yok | `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md:138` | - **21:44** — 6-A doğrulayıcı bulguları 2c/UX-1-C4'e devredildi: /bildirimler basit mod menüsüne eklenir (rozet görünüyo |
| B-23 | D3 | P-11 | Zamanlayıcı döngüsünün canlı kanıtı | `dokumanlar/09-uygulama-kayitlari/PAKET-6-uygulama-raporu.md:151` | > **`IsZamanlayici`'nin kendi döngüsü hiçbir testte koşmuyor**. Döngü (uyanma aralığı, kiracı |
| B-24 | D3 | P-11 | Zamanlanmış iş ayarları düzenlenemiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:207` | 13. `/bildirimler` — zamanlanmış iş ayarları (saat/eşik/açık-kapalı) düzenlenemiyor ve gerekçe de gösterilmiyor. |
| B-25 | D4 | P-15 | "Fişten Fatura Kes" ULID → kayıt seçici | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:177` | 12. **`/faturalar` "Fişten Fatura Kes" 26 haneli ULID'yi elle yazdırıyor.** Öneri: arama kutulu fiş seçici (son 50 fiş); |
| B-26 | D5 | P-22 | /siparisler durum geçişi onaysız ve geri alınamaz | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:178` | 13. **`/siparisler` durum geçişi onaysız, geri alınamıyor ve "Düzenle"yi de yok ediyor (DÜZENLENEBİLİRLİK).** Kanıt: `co |
| B-27 | D3 | P-13 | S-3f "kapandı" denen iki bulgunun yeniden ölçümü | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:274` | `getBBox()` ölçümü DOM gerektirir; bUnit path dizgesini ölçer. `devredildi: QA turu 6` |
| B-28 | D6 | P-24 | DOM gerektiren kanıtlar (pasta getBBox) | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:274` | `getBBox()` ölçümü DOM gerektirir; bUnit path dizgesini ölçer. `devredildi: QA turu 6` |
| B-29 | D4 | P-15 | Derin bağlantı / süzgeç sözleşmesi | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:183` | 18. **`/evraklar/takvim` gün bağlantısındaki `vadeBas/vadeBit` hedef sayfada uygulanmıyor (HATA).** Kanıt: `/evraklar?va |
| B-30 | D4 | P-18 | /yetkiler ROL sütunu sticky değil | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:188` | 23. **`/yetkiler` ROL sütunu yapışkan değil (GÖRSEL).** Kanıt: tıklama sonrası kabın `scrollLeft=252`, ilk hücre `left=1 |
| B-31 | D4 | P-16 | Pasif cariler satış listesinde seçilebiliyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:136` | 1. `/cariler` → `/islemler` **pasif cariler satışta seçilebiliyor** (YÜKSEK). |
| B-32 | D4 | P-18 | Karanlık temada satır vurgusu okunamıyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:179` | 14. **`/fiyatlandirma` karanlık temada `satir-vurgu` okunmuyor (GÖRSEL).** Kanıt: zemin `rgb(255,251,235)`, metin `rgb(2 |
| B-33 | D5 | P-22 | /sayim DURUM sütununda düz metin "İptal Et" | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:174` | 9. **`/sayim` DURUM sütunu gerçek durumu göstermiyor; hücrede düz metin "İptal Et" (HATA).** Öneri: gerçek durum rozeti  |
| B-34 | D2 | P-06 | Dolu formda ilk "Kaydet" etkisiz | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:176` | 11. **`/faturalar` dolu formda ilk "Kaydet" etkisiz, ikincide kaydediyor (HATA).** Kanıt: 1. tıklamada hiç POST yok ve e |
| B-35 | D2 | P-09 | Başarısız girişte kullanıcı varlığı ifşası | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur3.md:11` | \| 1 \| `/giris` \| Giriş \| BULGU \| Konsol temiz. Boş submit ve `gecersiz-eposta`da istek gitmedi (istemci doğrulaması |
| B-36 | D2 | P-09 | Tek SAHİP çıkmazı + devir akışı | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:187` | 22. **`/kullanicilar` tek SAHİP kendi rolünü/erişimini hiçbir yoldan değiştiremiyor (DÜZENLENEBİLİRLİK).** Kanıt: Düzenl |
| B-37 | D2 | P-09 | Kullanıcı kaydı silinemiyor / pasife alınamıyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur4.md:122` | 15. **`/kullanicilar`** — Tek SAHİP'li kiracıda kendi rolü/erişimi değiştirilemiyor: Düzenle diyaloğunda rol alanı yok,  |
| B-38 | D3 | P-12 | /vezne Düzenle penceresinde İşçilik alanı yok | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:200` | 6. `/vezne` — düzenleme penceresinde "İşçilik (TL)" alanı yok. |
| B-39 | D3 | P-13 | Aktif cari pasife alınamıyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:197` | 3. `/cariler` — aktif cari pasife alınamıyor. |
| B-40 | D3 | P-13 | /urunler/agac kategori pasife alınamıyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:199` | 5. `/urunler/agac` — kategori pasife alınamıyor/silinemiyor (reyonlarda var). |
| B-41 | D3 | P-12 | Kasa hareketi düzenlenemiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:210` | `/islemler` satış fişi (iade / ters kayıt yolu yazılı) · `/vezne` VEZNEDE fişi (gerekçeli iptal + yeni fiş) · `/banka` d |
| B-42 | D3 | P-12 | Banka dekontu ne düzenlenebiliyor ne stornolanabiliyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:210` | `/islemler` satış fişi (iade / ters kayıt yolu yazılı) · `/vezne` VEZNEDE fişi (gerekçeli iptal + yeni fiş) · `/banka` d |
| B-43 | D3 | P-12 | Emanet hareketi düzenlenemiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:210` | `/islemler` satış fişi (iade / ters kayıt yolu yazılı) · `/vezne` VEZNEDE fişi (gerekçeli iptal + yeni fiş) · `/banka` d |
| B-44 | D5 | P-22 | Sayım oturumunun adı düzeltilemiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:203` | 9. `/sayim` — oturum ADI açıldıktan sonra düzeltilemiyor (ad satır kümesini etkilemez). |
| B-45 | D5 | P-22 | Gün sonu kasa sayımı hiçbir yerden girilemiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur4.md:130` | **Grup 3 (9):** gider pusulası kaydının tamamı *(satıcı ad/TCKN/adres/tip, mal cinsi/gram/ayar, brüt tutar, stopaj, tesl |
| B-46 | D5 | P-22 | POS "Bağlı Kasa" düzeltme yolu yok | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:184` | 19. **`/pos` "Bağlı Kasa" oluşturmadan sonra düzenlenemiyor ve gösterilen düzeltme yolu ilgisiz (DÜZENLENEBİLİRLİK).** K |
| B-47 | D3 | P-13 | /isletme kur kaynağı ve maliyet yöntemi boşken kilitli | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:205` | 11. `/isletme` — kur kaynağı ve maliyet yöntemi hiç belirlenmemişken bile kilitli. |
| B-48 | D3 | P-13 | /yedekler iptal gerekçesi gösterilmiyor | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur4.md:131` | **Grup 4 (19):** `/kurulum` tamamlanan adımlar *(gerekçeli, ilgili ekrana yönlendiriyor)* · mühürlenmiş kurulumda açılış |
| B-49 | D3 | P-13 | UX-1-C3 gider pusulası satıcı kimliği + /uyum düğmeleri | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-3.md:25` | \| 3 \| Gider pusulası arayüzden hiç üretilemiyor — satıcı kimliği alanları formda yok, POST 400 \| Eray, Aşkın, Mehmet, |
| B-50 | D0 | P-00 | QA turu 4 kalan 17 maddesi listelenmedi | `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md:172` | - **06:24** — **S-3d BİTTİ — kapı GEÇTİ (06:23):** build 0/0 · denetim TEMİZ · tam süit **2834/2834** · 49 migration. QA |
| B-51 | D0 | P-00 | S-3e açık kalanlarının S-3f'te kapandığı ölçülmedi | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3e-uygulama-raporu.md:175` | ## 12. Açık kalanlar (devredildi: PAKET S-3f) |
| B-52 | D2 | P-08 | 34-A denetim zenginleştirme dilimi | `dokumanlar/09-uygulama-kayitlari/PAKET-34-uygulama-raporu.md:62` | - **`denetim.sh` sayacı eklendi:** "Rapor materyalizasyonu (TB-19)" — `Modules/Rapor` + `Modules/Maliyet` altındaki `ToL |
| B-53 | D1 | P-02 | mut.sh + 3 DB yedeği sürümlenmemiş | `SONRAKI-OTURUM-PROMPT.md:919` | - **Mutasyon zinciri ikili dosyaları mutant bırakır:** `mut.sh` kaynağı geri alır ama son `dotnet test` mutantı derlemiş |
| B-54 | D2 | P-06 | Gerekçeli düzenlenemezlik istisnaları (21 kalem) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:209` | **B. Gerekçeli ve düzeltme yolu ekranda yazılı (muhasebe/mevzuat kilidi; kabul edilebilir) — 19** |
| B-55 | D5 | P-22 | (YENİ) POS tahsilat varken "yeni kasaya taşı" göç akışı | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:277` | 4. **`/pos` cihazda tahsilat varken "yeni kasaya taşı" akışı açılmadı** — |
| B-56 | D5 | P-22 | (YENİ) POS kasa değişiminin tahsilatlı reddi canlı denenmedi | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:181` | - POS cihazında **tahsilat varken** kasa değişiminin reddi 5198'de denenmedi: gerçek |
| B-57 | D2 | P-07 | (YENİ) kur/guncel 130+ tekrar istek fırtınası + 401 | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur5.md:160` | \| Kendiliğinden oluşan konsol/ağ hatası \| `POST kur/guncelle` **503** (`/` ve `/kur`), `GET kullanicilar` **429**, `ku |
| B-58 | D0 | P-00 | (YENİ) Tur 4/5'in 119 açık + ≈59 yeni bulgusunun kimliklendirilmesi **[SENTEZ]** | `PLAN-TEK-SEFER-2026-09-18.md:972` | \| 3 \| B-50/B-51 açılmamış kutu: tur 5'te 193 bulgu var, envanter 54 B kimliğine sıkıştırıyor \| KRİTİK \| **KABUL.** P |
| B-59 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Ham teknik değer gösterimi: ULID / ham enum / slug / DOVIZ_HESAP_FAZ_B ekrana basılıyor (UX-S31) | `ham/qa-turu-5.json (rota /islemler)` | Başarı mesajında ham ULID fiş kimliği gösteriliyor |
| B-60 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Başarı bandı kalıbı: yazma sonrası sonuç bandı yok / içeriksiz / ekran dışında (UX-S27) | `ham/qa-turu-5.json (rota /)` | Kur hata bandının kapatma düğmesi yok, ekranda kalıcı duruyor |
| B-61 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) /isletme işletme kimliği telefonu doğrulanmıyor (fatura ve gider pusulasına basılıyor) | `ham/qa-turu-5.json (rota /isletme)` | İşletme kimliği telefonu doğrulanmıyor: 'abc' kaydediliyor (aynı alan şube formunda doğrulanıyor) |
| B-62 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) /denetim-izi doğruluğu: bitiş tarihi off-by-one, kur anlık görüntüsü kirliliği, çift getirme, sayfalama | `ham/qa-turu-5.json (rota /denetim-izi)` | Bitiş tarihi filtresi son günü kapsam dışı bırakıyor (off-by-one) |
| B-63 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Erişilebilirlik toplu borcu: isimsiz düğme, bitişik rozet, eksik h1, aria-invalid/role=alert | `ham/qa-turu-5.json (rota /giris)` | Şifre sıfırlama (şifremi unuttum) akışı hiç yok |
| B-64 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Ters tarih / geçersiz süzgeçte eski tablonun geçersizleştirilmemesi | `ham/qa-turu-5.json (rota /vezne)` | Fiş açıldıktan sonra süzgeç değişmediği için kayıt kaybolmuş görünüyor |
| B-65 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Metin ve adlandırma temizliği: kopyala-yapıştır artığı, çift başlık, çelişkili açıklama, tek kavram iki ad (UX-S31) | `ham/qa-turu-5.json (rota /gider-pusulalari)` | Detay açılışında aynı GET iki kez gönderiliyor (çift getirme, denetim izine çift satır) |
| B-66 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Onay ve geri alma: yıkıcı/geri alınamaz eylemde onay kipi ve geri dönüş yolu eksikliği | `ham/qa-turu-5.json (rota /)` | Panelde negatif kasa bakiyeleri hiçbir uyarı işareti olmadan listeleniyor |
| B-67 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Sessiz etkisizlik: tıklama ne istek atıyor ne mesaj veriyor (DoD EK md.1) | `ham/qa-turu-5.json (rota /pos/tahsilatlar)` | Eşleştirme hatası tıklanan düğmeden 685px yukarıda, ekran dışında gösteriliyor — eylem sessiz görünüyor |
| B-68 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Boş durum / sayfa başlığı / yönlendirme tutarlılığı (UX-S28 boş durum ile hata ayrımı) | `ham/qa-turu-5.json (rota /vezne)` | Sayfa gövdesinde başlık ve açıklama yok — diğer ekranlarla tutarsız |
| B-69 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Sistemin bildiği değerin kullanıcıdan istenmesi (liste fiyatı, stok adedi) — UX-S21 | `ham/qa-turu-5.json (rota /islemler)` | Sistem liste fiyatını biliyor ama birim fiyat alanını doldurmuyor — kullanıcı bildiği rakamı elle giriyor |
| B-70 | D2/D4 | P-00 triyajı → ilgili dalga paketi | (P-00 TRİYAJI) Oluşturma formu ↔ düzenleme formu alan paritesi + kip/çekmece davranışı (UX-S26) | `ham/qa-turu-5.json (rota /urunler)` | Stok penceresinde düğme metni ile hata metni bitişik yazılıyor |
| B-71 | D8 | P-36 | (TUR 7 TRİYAJI) Görsel dil / görsel ağırlık / satır eylem yoğunluğu — ekran doğru çalışıyor ama görsel ağırlık yanlış yerde | `ham/qa-turu-7.json (rota /kasalar, /cariler)` | /kasalar: devre dışı 'Pasife Al' kart genişliğinde dolu kırmızı düğme, kartın ağırlık merkezi o · /cariler: satırda 7-8 eylem düğmesi, satır yüksekliği 329 px, 1280×800'de ekranda 2 satır (GERİLEME) |
| B-72 | D8 | P-36 | (TUR 7 TRİYAJI) Ara/masaüstü genişlik (768-1280 px) düzen paritesi — B-9'un DoD'u YALNIZ 375 px'tir, bu bant hiçbir kimliğin kapsamında değildi | `ham/qa-turu-7.json (rota /evraklar, /faturalar)` | /evraklar 890 px'te scrollWidth 1208 > clientWidth 890 (GERİLEME: tur 6'da 943) · /faturalar 1280 px'te tablo kırılıyor, DURUM hücresi bitişik yazılıyor |
| Y-1 | D1 | P-04 | Şube izolasyonu global sorgu filtresine | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:13` | \| 1 \| Şube izolasyonunu global sorgu filtresine taşı (SubeId), 51 elle süzgeci kaldır \| Raporun tek gerçek veri-sızın |
| Y-2 | D1 | P-05 | Eksik 5 unique kısıt | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:14` | \| 2 \| Eksik unique kısıtları koy: stoklar(tenant,sube,urun), urunler.kod, urunler.barkod, cariler.vergi_no, banka_hesa |
| Y-3 | D3 | P-10 | Sunucunun kabul ettiği 7 alanı forma bağla | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:15` | \| 3 \| Sunucunun zaten kabul ettiği ama ekranın göndermediği alanları forma bağla: ürün RFID/KMTS/görsel URL/açıklama,  |
| Y-4 | D4 | P-16 | Satış kalem satırına ayar/milyem/gram/has + iskonto | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:16` | \| 4 \| Satış fişi kalem satırına ayar/milyem/gram/has kolonlarını ekle; satır ve fiş iskontosu girdisi aç \| Kuyumcunun |
| Y-5 | D4 | P-15 | Sunucu taraflı aranabilir seçici bileşeni | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:17` | \| 5 \| Ürün ve cari için metinle arama (açılır listedeki 200 kayıt sınırını kaldır) \| 200 kaydı geçen her dükkânda ürü |
| Y-6 | D4 | P-17 | Ortak yazdırma / PDF şablonu | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:18` | \| 6 \| Satış fişi, tahsilat makbuzu ve emanet teslim belgesi yazdırma sayfaları \| Emanet teslim belgesi olmadan müşter |
| Y-7 | D4 | P-17 | Pano fiyat ucu + vitrin/kur panosu | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:19` | \| 7 \| Pano fiyat ucu + parite başına dükkân marjı/yuvarlama kaydı (tek paket) \| Hesap ve 14 test yeşil duruyor, yalnı |
| Y-8 | D3 | P-10 | Ürün görseli / galerisi | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:20` | \| 8 \| Ürün görselini ekranda göster (belge deposundaki blob → kart galerisi) \| Kuyumda mal görselle satılır; hem gors |
| Y-9 | D4 | P-14 | Kenar çubuğu gruplaması + etiket çarpışmaları | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:21` | \| 9 \| Kenar çubuğunu gruplandır (Rapor Merkezi'nin 4 grup deseniyle) ve dört etiket çarpışmasını çöz \| 38 düz bağlant |
| Y-10 | D5 | P-20 | HURDA SATIŞI hareket türü + has/TL karşı bacağı | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:22` | \| 10 \| HURDA SATIŞI (toptancıya/ramata sevk) hareketi ve ucu \| Biriken hurdanın hiç çıkış kapısı yok: has gram yalnız |
| Y-11 | D5 | P-19 | Virman (kasa↔banka) + şubeler arası transfer | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:23` | \| 11 \| Virman (kasa↔banka) ve şubeler arası NAKİT transferi — tek iki-bacaklı desen \| İki ayrı deftere iki bağımsız k |
| Y-12 | D5 | P-19 | Gider/masraf kategorisi + gider ekranı | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:24` | \| 12 \| Gider/masraf kategorisi ve gider ekranı (kasa çıkışına kategori alanı) \| Gider bir hareket türü bile değil; ka |
| Y-13 | D2 | P-08 | Fişe satıcı personel alanı | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:25` | \| 13 \| Fişe satıcı personel alanı (hareketler.personel_id) ve R13 raporunun kaynağını değiştirme \| Bugünkü personel r |
| Y-14 | D2 | P-08 | hareketler tablosuna adet kolonu | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:26` | \| 14 \| hareketler tablosuna adet kolonu \| Adet defterde yok, yalnız stokta var; adet bazlı her sorgu (iade, satış ana |
| Y-15 | D2 | P-06 | Düzeltme politikası rozeti + iç kod temizliği | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:27` | \| 15 \| Düzeltme politikası rozetini liste ekranlarına yay \| Sunucu ucu, testler ve istemci servisi hazır; kullanıcı h |
| Y-16 | D6 | P-25 | Stok yaşlandırma / ölü stok raporu (16. rapor) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:28` | \| 16 \| Stok yaşlandırma / ölü stok raporu \| Giriş tarihi zaten defterde; cari yaşlandırma kodu birebir şablon. Sermay |
| Y-17 | D5 | P-21 | Avans / kapora işlemi | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-18 | D5 | P-21 | Sipariş → satış dönüşümü | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-19 | D5 | P-21 | Değişim (ürünü ürünle) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-20 | D5 | P-21 | Konsinye çıkış / iade | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-21 | D5 | P-21 | EVRAK_CIRO ucu + rücu durumu | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-22 | **1.1** | — | Emanet parça kimliği (K-12 gelmeden açılmaz) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-23 | D5 | P-21 | Rezerv / bloke adet kolonu | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:29` | \| 17 \| Kalan işlem kapıları: avans/kapora, sipariş→satış dönüşümü, değişim, konsinye çıkış/iade, EVRAK_CIRO, emanet pa |
| Y-24 | **1.1** | — | Müşteri taksit planı (P-23'ün Faz B'si, yer bırakılır) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:58` | \| (e) Taksit planı yok \| **DOGRU** \| Taksit adlı tek kalıcı varlık Entities.cs:1180 `public class PosTaksitOran` ([Ta |
| Y-25 | D2 | P-08 | fatura_no serbest metnin emekliye ayrılması (karar değişti) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:30` | \| 18 \| hareketler.fatura_no'yu kimliğe bağla (unique/FK) — metin alanını emekliye ayır \| Rapor bu maddeyi yanlış kurm |
| Y-26 | D4 | P-16 | Ürün kartının 9 eksik alanı + çoklu taş satırı | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:31` | \| 19 \| Ürün kartının eksik alanları: tedarikçi, alış maliyeti, kâr marjı, KDV oranı, marka/model, seri no, yüzük ölçüs |
| Y-27 | D4 | P-16 | Cari kartı eksikleri (TCKN/VKN, ünvan, segment…) | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:93` | \| Orta \| Müşteri KYC kartı (doğum tarihi, uyruk, segment, temsilci) \| Çoğu \| Cari kartında yok; bu veriler yalnız iş |
| Y-28 | D8 | P-34 | Referans bütünlüğü: gerçek FK'lar | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:32` | \| 20 \| Referans bütünlüğü: ilişkileri düz string eşleşmesinden gerçek FK'ya taşı \| 135 indeks var, sıfır FK var: veri |
| Y-29 | D4 | P-15 | Kabuk katmanı: arama, komut paleti, şube seçici | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:33` | \| 21 \| Genel arama + komut paleti + kırıntı (breadcrumb) + kabukta şube seçici + sık kullanılanlar \| Deneyimi en çok  |
| Y-30 | D6 | P-26 | Tamir/servis iş emrini deftere tam bağla | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:88` | \| Orta \| Tamir/servis iş emri (tam) \| Çoğu \| `/siparisler` altında sekme; deftere yazmıyor, kapora ve usta alanı yok |
| Y-31 | D6 | P-25 | Rapor Merkezi sayısı tutarlılığı (15 → 16) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:82` | \| §5(d) Rapor Merkezi 16 raporu 4 grupta topluyor (teknik hazır) \| **YANLIS** \| dotnet/Kuyumcu.Api/Modules/Rapor/Rapo |
| Y-32 | D2 | P-08 | HareketTurleri sözleşmesi kodda zorlansın | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:140` | Sizde kodda 20 hareket türü var (`Sabitler.HareketTurleri`): ALIS, SATIS, TAKAS, HURDA_ALIS, EMANET_GIRIS, EMANET_CIKIS, |
| Y-33 | D1 | P-02 | RotaBasliklari 42 rota ↔ 42 @page çırçırı | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:286` | \| Sayı hataları \| Menü 36 değil **38**; `RotaBasliklari` 41 değil **42** rota; `window.print` 2 değil **3** yerde \| |
| Y-34 | D6 | P-26 | Satınalma siparişi (PO) + mal kabul | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:91` | \| Orta \| Satınalma siparişi (PO) + mal kabul \| Çoğu \| Yok \| |
| Y-35 | D6 | P-26 | Sevk / satış-alış irsaliyesi | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:92` | \| Orta \| Satış/alış irsaliyesi \| Çoğu \| Hiçbir katmanda yok \| |
| Y-36 | D6 | P-26 | Ustaya metal çıkışı (K-13'e bağlı dilim) | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:94` | \| Orta \| Ustaya metal çıkış / atölye metal defteri \| Çoğu \| ATOLYE cari türü var, metal defteri yok \| |
| Y-37 | D6 | P-26 | Muhasebeleştirme: hesap kodu + muhasebe fişi | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:95` | \| Orta \| Muhasebeleştirme / hesap kodu / mizan \| Çoğu \| Yok — mali müşavire yalnız genel dışa aktarım \| |
| Y-38 | **1.1** | — | Set birleştirme / bozma | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:97` | \| Orta \| Set birleştirme / bozma \| Çoğu \| Yok \| |
| Y-39 | D2 | P-09 | Yetki matrisi otomatik üretimi + şube görünürlüğü | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:37` | \| 46 \| Şube bazlı görünürlük + yetki matrisi tamamlama \| KOSULLU KABUL \| 1.1 \| 1.1 \| Faz A 2–2,5 gün · Faz B 1,5–2 |
| Y-40 | D6 | P-26 | (YENİ) KDV 23/e özel matrah varsayılanı (K-10'a bağlı) | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:112` | \| (i) KDV 23/e özel matrah VARSAYILAN olarak KAPALI (mevzuat üçlüsünün 1. maddesi) \| **DOGRU** \| Motor ve kolon HAZIR |
| Y-41 | D7 | P-32 | (YENİ) Ücretsiz pilot paketi | `docs/RUNBOOK-PILOT-DAVET.md:1` | # RUNBOOK — Ücretsiz pilot daveti |
| Y-42 | D0 + D8 | P-00, P-35 | (YENİ) Triyaj (intake) kuralı **[SENTEZ]** | `PLAN-TEK-SEFER-2026-09-18.md:1256` | \| **Y-42** \| D0 + D8 \| P-00, P-35 \| **(YENİ)** Triyaj (intake) kuralı \| |
| Y-43 | D2 | P-00 triyajı | (YENİ) Kur ekranı 10 sn anketi + SignalR itme kanalı kurulamıyor (performans) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur6.md:160` | 11. **Kur anketi / SignalR** — `/` ve `/bildirimler`: WebSocket el sıkışmasını düzeltin, itme kanalı kuruluyken HTTP ank |
| Y-44 | D1-D2 | P-00 triyajı | (YENİ) Erişim jetonu WebSocket adresinde sorgu dizesi olarak taşınıyor (güvenlik) | `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur6.md:160` | 11. **Kur anketi / SignalR** — `/` ve `/bildirimler`: WebSocket el sıkışmasını düzeltin, itme kanalı kuruluyken HTTP ank |
| R-1 | D7 | P-27 | Karar verildi: koşulsuz 1.0 | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:68` | - 4) Kurul tur 4 mührü (karar, 0,5 gün): V-1'in U1–U4'ü, 6 'karar bekliyor' paketin sürümü, SÖZLEŞME-1 çelişkisi, F-1/F- |
| R-2 | D7 | P-28 | Karar verildi: koşulsuz 1.0 | `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-4.md:71` | - 7) Ölçüm zinciri sırası KORUNSUN: 34-Ö → k6 yük koşumu → F-4 telemetri → F-8 Faz B. 34-Ö kapanmadan koşulan yük testi  |
| R-3 | D7 | P-29 | Karar verildi: 1.0, belge hattında paralel | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:803` | \| 6 \| S-1 ticari paket (fiyat kademeleri, 15 dk demo betiği, gerçek görünümlü demo kiracısı tohumlama betiği, kurulum/ |
| R-4 | D7 | P-30 | Karar verildi: 1.0 | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:764` | - Satış merceği kanıtı: her dalga sonunda 7 demo ekranının görüntüsü + 'yanlış veri gir → düzelt → geçmişi göster' senar |
| R-5 | D7 | P-31 | Karar verildi: 1.0 | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:43` | \| T-4 \| Destek ve işletim görünürlüğü — sürüm/derleme/migration damgası + korelasyonlu hata kimliği (T-4a) ve maskeli  |
| R-6 | D6 | P-25 | Karar verildi: 1.0, B-8'den sonra | `SONRAKI-OTURUM-PROMPT.md:515` | k6 run yuk-testi/k6-senaryo.js |
| R-7 | D7 | P-32 | Karar verildi: 1.0, ücretli tarihin ön şartı | `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md:99` | - Destek saati ve saatlik maliyet OLCULMEDI: kiracı başı aylık destek dakikası hiç ölçülmedi, ilk 3 ay yoğunluğu bilinmi |
| R-8 | D7 | P-29 | Karar verildi: tek rakamlı oran ilan edilmez | `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md:66` | ## Kurucunun tasarruf hedefi — dürüst okuma |
| R-9 | D0 + D8 | P-01, P-35 | Açılış ve kapanış mührü | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:920` | - **3-P — PARALEL hat (yalnız ayak izi ayrık, kod-dışı ağırlıklı):** V-1 sürüm mührü (0,5 gün, kod yok) → F-8 birim ekon |
| R-10 | D1 | P-03 | Karar verildi: 1.0, tavan yalnız azalır | `dokumanlar/09-uygulama-kayitlari/ham/muhur5.md:28` | - Satış asgarisi DIŞI (1.1'e): SÖZLEŞME-1 · PWA-44 · 36 · 37 · 46 · sadakat/prim/RFID · F-1/F-3/F-4 · UX-2-D · UX-3-C. 4 |
| R-11 | D0 | P-01 | Ekip oyuyla mühürlenecek | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:16` | \| 5 \| Belge / etiket şablon motoru \| KOSULLU KABUL \| 5 \| karar bekliyor \| 5–7 gün (Faz A 2,5 · Faz B 1,5 · Faz C 1 |
| R-12 | D0 | P-01 | Ekip oyuyla mühürlenecek | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:134` | - **Has defteri kabul ölçütü (paket 39)** — 'Defter kapanışı = GET /raporlar/stok-degeri has toplamı TUTMALI' ölçütü kal |
| R-13 | D0 | P-01 | Oy 2-2; kurul kırar, kırılmazsa Tamer Bey mühürler | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:34` | \| 43 \| Mobil / dar ekran uyarlaması (25 sayfa; PARITE'de '19 sayfa') \| EVET \| karar (dalga 4 sonu) \| karar bekliyor |
| R-14 | D0 | P-01 | Ekip oyuyla mühürlenecek | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:62` | - **Dalga 5** — paketler 42-A, 5-A, 39-A, 40-A, UX-2-C · sıra: 42-A → 5-A → 39-A → 40-A → UX-2-C |
| R-15 | D0 | P-01 | Karar verildi: sıra 0 kabul, DALGA 4 buna göre dizildi **[SENTEZ]** | `PLAN-TEK-SEFER-2026-09-18.md:1276` | \| R-15 \| D0 \| P-01 \| Karar verildi: sıra 0 kabul, DALGA 4 buna göre dizildi \| |
| R-16 | D0 | P-00 | Belge üzerinden satır satır doğrulanacak | `EKRAN-ALAN-DOGRULAMA-2026-09-18.md:22` | \| 10 \| HURDA SATIŞI (toptancıya/ramata sevk) hareketi ve ucu \| Biriken hurdanın hiç çıkış kapısı yok: has gram yalnız |
| R-17 | D6 | P-23 | Karar verildi: EVET, vezne kuralı korunarak | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:249` | \| Ödeme kalemi tablosu \| Yok \| Karma ödeme üç ayrı yerden birleştirilerek okunuyor \| |
| R-18 | D6 | P-23 | Karar verildi: tümü ya da hiç; kısmi = mimari veto | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:150` | \| Mahsup (ödemeyi borç kalemine bağlama) \| Çoğu \| Tahsilat toplu düşüyor; hangi tahsilat hangi fişi kapattı izlenemiy |
| R-19 | D0 | P-01 | Ekip oyuyla mühürlenecek | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:313` | \| — \| **Ertelenenler:** prim hesabı, seri no/tekil parça (kapsam dışı ilan et), döviz alım-satım (lisans engeli), eksp |
| D-1 | 1.1 | §6 | Sadakat, RFID akışı, seri no/bandrol, teklif-proforma, hediye çeki, istek listesi, marka-koleksiyon, e-ticaret | `YAPABILECEKLERIMIZ-2026-09-17.md:121` | 4. **Sadakat / parapuan** (3-4 gün) — rakiplerin üçünde birden var, bizde yok. |
| D-2 | 1.1 | §6 | Personel prim / hakediş modülü | `YAPABILECEKLERIMIZ-2026-09-17.md:59` | \| 2 \| **Personel prim / hakediş** \| M&L, CetaSoft, KuyumHub (hedef-prim) \| Kodda `Prim` **0 satır** \| 3-4 gün \| 34 |
| D-3 | 1.1 | §6 | Döviz alım-satım (sarraf) ekranı | `YAPABILECEKLERIMIZ-2026-09-17.md:60` | \| 3 \| **Vitrin / fiyat panosu** (ikinci ekrana tabela) \| ZarifSoft, SarrafPano kategorisi \| Fiyat motoru VAR, yayın  |
| D-4 | 1.1 | §6 | Ekspertiz / değerleme fişi | `YAPABILECEKLERIMIZ-2026-09-17.md:69` | \| 12 \| **Ekspertiz / değerleme fişi** \| Jewel360 \| Yok \| 1,5-2 gün \| Hurda alımında ve emanet-rehin işinde kullanı |
| D-5 | 1.1 | §6 | Fiyat kilitleme (süreli fiyat sabitleme) | `YAPABILECEKLERIMIZ-2026-09-17.md:118` | 1. **Fiyat kilitleme** (1-1,5 gün) — en küçük iş, en görünür fayda. |
| D-6 | 1.1 | §6 | Sertifika kartı basımı (pırlanta 4C) | `YAPABILECEKLERIMIZ-2026-09-17.md:65` | \| 8 \| **Sertifika kartı basımı (pırlanta)** \| Dezy ("PVC sertifika kartı") \| 4C verisi VAR, kart çıktısı yok \| 1-1, |
| D-7 | 1.1 | §6 | Müşteri segmentasyonu | `YAPABILECEKLERIMIZ-2026-09-17.md:66` | \| 9 \| **Müşteri segmentasyonu** (son alışveriş, ciro dilimi, doğum günü listesi) \| KuyumBase (CRM segmentasyon) \| Yo |
| D-8 | 1.1 | §6 | Ürün sayfası anlatımı "8 acı nokta → çözümü" **[SENTEZ]** | `PLAN-TEK-SEFER-2026-09-18.md:840` | \| **D-8** \| Ürün sayfası anlatımı "8 acı nokta → çözümü" \| **1.0'A ALINDI** — P-29 içinde (S-1 belge hattı) \| |
| D-9 | 1.1 | §6 | Paket 36 — Patron paneli | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:27` | \| 36 \| Patron paneli \| KOSULLU KABUL \| 1.1 \| 1.1 \| 2–3 gün (PARITE ile aynı) — yeni hesap olmadığı için sürenin ço |
| D-10 | 1.1 | §6 | Paket 37 — Pozisyon / net varlık ekranı | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:28` | \| 37 \| Pozisyon / net varlık ekranı \| KOSULLU KABUL \| 1.1 \| 1.1 \| 2–3 gün (PARITE ile aynı) \| 34, 35 \| [PAKET-37 |
| D-11 | 1.1 | §6 | Paket 44 + PWA-1 kalanı (çevrimdışı / uygulama kabuğu) | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:35` | \| 44 \| PWA kabuğu (manifest, service worker, ana ekrana ekleme) \| KOSULLU KABUL \| 1.1 \| 1.1 \| Faz A 1,5–2 gün · Fa |
| D-12 | 1.1 | §6 | UX-3-C basit/profesyonel mod derinleştirmesi | `SONRAKI-OTURUM-PROMPT.md:872` | - Dalga 8: Faz B/C kuyruğu (1-B, 4-B, 6-B, 3-B, 38-B, 42-B, 5-B/C, 39-B, 48-B/C, UX-1-D, 34-B, 35-B, 47-B, 40-B) → UX-3- |
| D-13 | 1.1 | §6 | F-2 Faz B — `SistemKiracilar.razor` | `dokumanlar/09-uygulama-kayitlari/ham/muhur4.md:19` | - K5 — F-2-min Faz B (SistemKiracilar.razor + sessionStorage) dalga 3'ten ÇIKARILIR, dalga 6'ya T-4b yanına alınır; dosy |
| K-1 | — | §4 | Canlı DB kiracı sızıntı satırlarının silinmesi + olay kaydı + QA fikstür temizliği | `KURUCUYA-KARARLAR-2026-09-16.md:8` | \| 2 \| **Canlı veritabanındaki 2 kiracı sızıntısı kanıt satırının silinmesi** \| 9 Eylül denetiminin kanıt satırları (` |
| K-2 | — | §4 | e-Fatura/e-Arşiv entegratör sözleşmesi + mali mühür + kontör | `KURUCUYA-KARARLAR-2026-09-16.md:7` | \| 1 \| **e-Fatura / e-Arşiv entegratör sözleşmesi + mali mühür (e-imza) başvurusu** \| 2026'da e-Arşiv zorunlu; sözleşm |
| K-3 | — | §4 | Kur verisi için yazılı izin/abonelik | `KURUCUYA-KARARLAR-2026-09-16.md:14` | \| 8 \| **Kur verisi için yazılı izin / sözleşme** \| F-8 tespiti: bugün Truncgil/Harem/AAB kur kaynakları sözleşmesiz v |
| K-4 | — | §4 | Depo temizliği (5 `.mutorj` Pages'te + 11 diğer, 8 adet 0 baytlık migration .cs, `.ek`) | `KURUCUYA-KARARLAR-2026-09-16.md:10` | \| 4 \| **Depo temizliği: 16 `.mutorj` yedeği + 8 sıfır baytlık migration dosyası + `.ek` dosyaları** \| Güvenlik kancas |
| K-5 | — | §4 | Sır rotasyonu (JWT sırrı, MySQL parolası, bir kez ekrana basılan geliştirme sırrı) | `KURUCUYA-KARARLAR-2026-09-16.md:12` | \| 6 \| **Sır rotasyonu (K9)** \| JWT sırrı ve MySQL parolası depoda düz metin; T-1 paketi üretimde kurulum anında ürete |
| K-6 | — | §4 | Bulut hesabı + ödeme yöntemi + KDV-2 teyidi | `KURUCUYA-KARARLAR-2026-09-16.md:16` | \| 10 \| **Bulut hesabı açılışı + ödeme yöntemi** \| Üretim sunucusu (T-1) ve yedek deposu (T-3) için hesap ve kart/kuru |
| K-7 | — | §4 | 40-B: KVKK aydınlatma metni onayı + ad alanı şifreleme kararı | `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:486` | - Paket 40-B'de yaptırım listesi ad/ünvan alanının şifresiz tutulmasına dair KVKK risk kabulünün yazılı onayı. |
| K-8 | — | §4 | Commit kararı (bin/obj depoda, yüzlerce değişiklik çalışma ağacında) | `KURUCUYA-KARARLAR-2026-09-16.md:11` | \| 5 \| **Commit kararı** \| "Commit yok" talimatı duruyor; çalışma ağacında yüzlerce değişiklik var (bin/obj de depoda) |
| K-9 | — | §4 | Alan adı + TLS + yaptırım listesi veri sağlayıcı bedeli | `KURUCUYA-KARARLAR-2026-09-16.md:9` | \| 3 \| **Alan adı + TLS sertifikası ve yaptırım listesi (OFAC/BM/AB) veri sağlayıcı bedeli** \| Üretim yayını (T-1) TLS |
| K-10 | — | §4 | Mali müşavir: KMTS beyanı, KDV 23/e özel matrah kodu teyidi, vade farkı KDV'si | `KURUCUYA-KARARLAR-2026-09-16.md:13` | \| 7 \| **Mali müşavire sorulacaklar** \| KMTS beyan yükümlülüğü, KDV 23/e özel matrah kodu (805) teyidi, taksitli satış |
| K-11 | — | §4 | ÖKC / Z raporu cihaz kararı | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:309` | \| 2 \| **Mevzuat üçlüsü**: KDV 23/e özel matrah varsayılanı → ÖKC yol ayrımı → e-Arşiv entegratörü \| (1) ve (2) gelişt |
| K-12 | — | §4 | Emanet/rehin hukuki görüşü | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:318` | 2. **Emanet modülünün hukuki durumu** — Kuyum Ticareti Yönetmeliği md. 10/3 karşısında kapsam daraltması için dış hukuki |
| K-13 | — | §4 | Katman 1 / fire sınırı | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:159` | Fire/ifraz/ramat bilinçli kapsam kararıdır (Katman 1 = imalat, kapsam dışı). **Ancak perakendeci de biriken hurdasını er |
| K-14 | — | §4 | İkinci el / bandrol kapsamı | `YAPABILECEKLERIMIZ-2026-09-17.md:64` | \| 7 \| **Bandrol / seri numarası takibi** \| KuyumBase \| Kodda `Bandrol`/`SeriNo` **0 satır** \| 2-3 gün \| Tekil taki |
| K-15 | — | §4 | SMS sağlayıcı | `KURUCUYA-KARARLAR-2026-09-16.md:15` | \| 9 \| **SMS sağlayıcı hesabı + başlık (sender ID) başvurusu** \| Vade/borç hatırlatma SMS'i canlıda gönderici başlığı  |
| K-16 | — | §4 | Rakamlı fiyat İLANI | `KURUCUYA-KARARLAR-2026-09-16.md:17` | \| 11 \| **Fiyat ilanı (F-8 Faz B sonrası)** \| Kademe ÖNERİSİ hazır (ilan değil): Tek Şube 1.200-1.500 · Çok Şube 2.500 |
| K-17 | — | §4 | Rakip deneme hesapları (göç provası için) | `KURUCUYA-KARARLAR-2026-09-16.md:18` | \| 12 \| **Rakip deneme hesabı açılması** \| Rakip parite matrisi ve arayüz kıyası bugün firmaların kendi beyanına dayan |
| K-18 | — | §4 | Yedek anahtarının sunucu dışında saklanması | `KURUCUYA-KARARLAR-2026-09-16.md:19` | \| 13 \| **Yedek şifreleme anahtarının sunucu dışı saklanması ve kayıp hâlinde kurtarma yolu** \| Yedekler şifreli tutul |
| K-19 | — | §4 | Dış iletişim / gönderim imzası (pilot daveti dâhil) | `docs/RUNBOOK-PILOT-DAVET.md:169` | Davet kodunuz: <jeton> |
| K-20 | — | §4 | iCloud eşitlemesi | `KURUCUYA-KARARLAR-2026-09-16.md:20` | \| 15 \| **Proje klasörünün iCloud eşitlemesinden çıkarılması (ACİL — işi yavaşlatıyor)** \| 2026-09-17 06:05 ölçümü: ma |
| K-21 | — | §4 | RFID / PVC donanım | `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md:99` | \| Düşük \| Teklif / proforma, marka-model-koleksiyon, istek listesi, sadakat, hediye çeki, RFID akışı, seri no, e-ticar |
| K-22 | — | §4 | Canlı banka API | `YAPABILECEKLERIMIZ-2026-09-17.md:85` | \| Canlı banka / açık bankacılık (19 banka) \| Her bankayla ayrı sözleşme ve API anahtarı \| |
| K-23 | — | §4 | (YENİ) Sipariş ATOLYEDE/HAZIR durumundan geri dönüş kararı (S-3f §12.2) | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:279` | 5. **`ATOLYEDE`/`HAZIR` geri dönüşü** açılmadı (§ 2/3). |
| K-24 | — | §4 | (YENİ) Kasa/cari negatif açılış ve kredili mevduat (KMH) istisnası (S-3f §12.3) | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:295` | cari açılışında negatif serbest (müşteri alacaklı olabilir). Kredili mevduat hesabı |
| K-25 | — | §4 | (YENİ) Gider pusulasında stopaj oranı + 7 günlük düzenleme süresi ve mevcut boş adresli kayıtların geriye dönük durumu (S-3f §12.1 | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:68` | bir biçim kuralıdır; mevzuatın değişen kısmı (stopaj oranı, süre) bu pakette |
| K-26 | — | §4 | (YENİ) Teslimat paketinde uygulama adresinin kısayola gömülmesi (S-3f §12.4) | `dokumanlar/09-uygulama-kayitlari/PAKET-S-3f-uygulama-raporu.md:299` | göre değişen adresin **kısayola gömülmesi** ve kurucu tarafından müşteriye yazılı |

**[SENTEZ] açıklaması (md.1):** B-58, Y-42, R-15 ve D-8 kimlikleri daha eski bir belgede **yoktur**; 2026-09-18 plan oturumunda (hasım turu / § 10 / § 13) doğmuştur. Kaynak sütunu o oturumun belgesini gösterir — "eski bir raporda vardı" iddiası kurulmamıştır.

## 3. QA turu 4 / 5 / 6 bulgularının kimliğe eşlenmesi (B-58)

Makine kaydı: `ham/triyaj-kayit.json` (her bulgu → kimlik + kutu). Ölçüm `ham/qa-turu-{4,5,6}.json` dosyalarından **sayılmıştır**, belgeden kopyalanmamıştır.

| Tur | Bulgu | Damgasız | Kullanılan kimlik | Devralınan (önceki_bulgular) |
|---|---|---|---|---|
| TUR4 | 142 | 0 | 41 | 80 |
| TUR5 | 193 | 0 | 47 | 148 |
| TUR6 | 203 | 0 | 42 | 193 |

Tur 5'in devraldığı 148 kalemin durumu (JSON'dan sayıldı): **KAPANDI 14 · KISMEN 15 · AÇIK 119** — PLAN § 0'daki rakamla birebir uyuşur. Tur 6 aynı ölçümü 193 devralınan kalem üzerinde tekrarlamıştır (24 kapandı · 12 kısmen · 157 açık).

### En çok bulgu taşıyan kimlikler (üç tur toplamı)

| Kimlik | Bulgu | Konu |
|---|---|---|
| B-63 | 55 | (P-00 TRİYAJI) Erişilebilirlik toplu borcu: isimsiz düğme, bitişik rozet, eksik h1, aria-i |
| B-65 | 52 | (P-00 TRİYAJI) Metin ve adlandırma temizliği: kopyala-yapıştır artığı, çift başlık, çelişk |
| B-8 | 37 | 34-Ö ölçek + kültür borcu |
| B-7 | 36 | Türetilmiş özet ↔ liste toplamı mutabakatı |
| B-9 | 27 | Mobil parite, 42/42 rotada 375 px |
| B-60 | 26 | (P-00 TRİYAJI) Başarı bandı kalıbı: yazma sonrası sonuç bandı yok / içeriksiz / ekran dışı |
| B-59 | 25 | (P-00 TRİYAJI) Ham teknik değer gösterimi: ULID / ham enum / slug / DOVIZ_HESAP_FAZ_B ekra |
| B-18 | 23 | Alan altı doğrulama sözleşmesi + iş kuralı sınırları |
| B-70 | 20 | (P-00 TRİYAJI) Oluşturma formu ↔ düzenleme formu alan paritesi + kip/çekmece davranışı (UX |
| B-66 | 20 | (P-00 TRİYAJI) Onay ve geri alma: yıkıcı/geri alınamaz eylemde onay kipi ve geri dönüş yol |
| B-68 | 18 | (P-00 TRİYAJI) Boş durum / sayfa başlığı / yönlendirme tutarlılığı (UX-S28 boş durum ile h |
| B-64 | 16 | (P-00 TRİYAJI) Ters tarih / geçersiz süzgeçte eski tablonun geçersizleştirilmemesi |
| B-62 | 14 | (P-00 TRİYAJI) /denetim-izi doğruluğu: bitiş tarihi off-by-one, kur anlık görüntüsü kirlil |
| B-11 | 14 | Kılavuzun tek kaynaktan yeniden yazımı |
| B-57 | 13 | (YENİ) kur/guncel 130+ tekrar istek fırtınası + 401 |

## 4. P-00 triyajında doğan 12 yeni kimlik (B-59 … B-70)

Bunlar **yeni iş değil**, daha önce tek tek sayılmayan tekrar eden bulgu temalarıdır; hepsi QA turu 4/5/6 bulgularından üretildi ve her birinin altında en az bir gerçek bulgu satırı vardır.

| Kimlik | Tema | Bulgu adedi | İlk kanıt (tur 5 rotası) |
|---|---|---|---|
| B-59 | am teknik değer gösterimi: ULID / ham enum / slug / DOVIZ_HESAP_FAZ_B ekrana basılıy | 25 | /islemler — Başarı mesajında ham ULID fiş kimliği gösteriliyor |
| B-60 | aşarı bandı kalıbı: yazma sonrası sonuç bandı yok / içeriksiz / ekran dışında (UX-S2 | 26 | / — Kur hata bandının kapatma düğmesi yok, ekranda kalıcı duruyor |
| B-61 | isletme işletme kimliği telefonu doğrulanmıyor (fatura ve gider pusulasına basılıyor | 2 | /isletme — İşletme kimliği telefonu doğrulanmıyor: 'abc' kaydediliyor (aynı alan şube formu |
| B-62 | denetim-izi doğruluğu: bitiş tarihi off-by-one, kur anlık görüntüsü kirliliği, çift  | 14 | /denetim-izi — Bitiş tarihi filtresi son günü kapsam dışı bırakıyor (off-by-one) |
| B-63 | rişilebilirlik toplu borcu: isimsiz düğme, bitişik rozet, eksik h1, aria-invalid/rol | 55 | /giris — Şifre sıfırlama (şifremi unuttum) akışı hiç yok |
| B-64 | ers tarih / geçersiz süzgeçte eski tablonun geçersizleştirilmemesi | 16 | /vezne — Fiş açıldıktan sonra süzgeç değişmediği için kayıt kaybolmuş görünüyor |
| B-65 | etin ve adlandırma temizliği: kopyala-yapıştır artığı, çift başlık, çelişkili açıkla | 52 | /gider-pusulalari — Detay açılışında aynı GET iki kez gönderiliyor (çift getirme, denetim izine çift |
| B-66 | nay ve geri alma: yıkıcı/geri alınamaz eylemde onay kipi ve geri dönüş yolu eksikliğ | 20 | / — Panelde negatif kasa bakiyeleri hiçbir uyarı işareti olmadan listeleniyor |
| B-67 | essiz etkisizlik: tıklama ne istek atıyor ne mesaj veriyor (DoD EK md.1) | 6 | /pos/tahsilatlar — Eşleştirme hatası tıklanan düğmeden 685px yukarıda, ekran dışında gösteriliyor — |
| B-68 | oş durum / sayfa başlığı / yönlendirme tutarlılığı (UX-S28 boş durum ile hata ayrımı | 18 | /vezne — Sayfa gövdesinde başlık ve açıklama yok — diğer ekranlarla tutarsız |
| B-69 | istemin bildiği değerin kullanıcıdan istenmesi (liste fiyatı, stok adedi) — UX-S21 | 5 | /islemler — Sistem liste fiyatını biliyor ama birim fiyat alanını doldurmuyor — kullanıcı bi |
| B-70 | luşturma formu ↔ düzenleme formu alan paritesi + kip/çekmece davranışı (UX-S26) | 20 | /urunler — Stok penceresinde düğme metni ile hata metni bitişik yazılıyor |

## 5. S-3e ve S-3f açık kalanları (B-51)

| # | S-3e § 12 açık kalanı | S-3f'te kapandı mı | Kanıt |
|---|---|---|---|
| 1 | /faturalar TASLAK fatura ve kalemlerinin düzenlenememesi | **HAYIR** | PAKET-S-3f § 11 listesinde yok; QA turu 6'da hâlâ tek KRİTİK → B-3 |
| 2 | /faturalar ilk "Kaydet" etkisiz + "Fişten Fatura Kes" ULID | **HAYIR** | tur 6'da açık → B-34, B-25 |
| 3 | /vezne kuyruk toplamının 0 ₺ görünmesi | **HAYIR** | kurul tur 4 engel 11'de duruyor → B-7 |
| 4 | /cariler pasif cari satışta seçilebiliyor; aktif cari pasife alınamıyor | **HAYIR** | tur 6 düzenlenemeyen listesinde → B-31, B-39 |
| 5 | /sayim odak/Enter/DURUM/zorunlu Şube | **HAYIR** | tur 6'da sürüyor → B-13, B-33, B-18 |
| 6 | /kur "İptal et" bulgusunun tazelenmiş yayında yeniden denenmesi | **KISMEN** | S-3e § 14 kur kaynağı seçicisini kapattı; iptal yolu → B-15 |
| 7 | İşletme ayarlarında maliyet yöntemi kontrolü | **HAYIR** | → B-47 |
| 8 | `.ozet-kutu--amber` ham palet kullanımı | **HAYIR** | kapsam kilidi gereği dokunulmadı → B-32 ile aynı hat |

| # | S-3f § 11 açık kalanı | Kimlik |
|---|---|---|
| 1 | /kullanicilar tek SAHİP kendi rolünü değiştiremiyor (devir akışı açılmadı) | B-36 |
| 2 | /raporlar pasta düzeltmesi tarayıcıda görsel doğrulanmadı (getBBox) | B-28 |
| 3 | POS kasa değişiminin tahsilatlı reddi 5198'de denenmedi | B-56 |
| 4 | /pos tahsilat varken "yeni kasaya taşı" akışı açılmadı | B-55 |
| 5 | ATOLYEDE/HAZIR geri dönüşü açılmadı | K-23 (kurucu kararı) |
| 6 | QA turu 5'in ORTA/DÜŞÜK bulgularına dokunulmadı | B-59 … B-70 (bu triyajda kimliklendi) |

**K-23 … K-26 ve B-55/B-56 buradan doğar** (PLAN P-00 DoD): kaynakları yukarıdaki tabloda `PAKET-S-3f-uygulama-raporu.md` satırlarıdır.

## 6. B-50 — QA turu 4'ün kalan 17 maddesi (listelenmedi denen liste)

S-3d raporu "QA turu 4'ün kalan 17 maddesi (DP4-05..21) devredildi" demiş ama maddeleri **saymamıştı**. Sayıldı: DP4-05 … DP4-21 = **17 madde**, karşılıkları:

| DP4 | Konu | Kimlik |
|---|---|---|
| DP4-05 | Düzenlenebilirlik gerilemeleri (/cariler, /urunler/agac, /faturalar, /kullanicilar) | B-39, B-40, B-3, B-36 |
| DP4-06 | Doğrulama kalıbı tek elden (Enter/Escape, zorunlu alan, odak) | B-13, B-18 |
| DP4-07 | /kur dayanıklılığı (503'te anlık görüntü, kapalı kaynakta düğme) | B-14 |
| DP4-08 | Dar ekran 7 rota | B-9 |
| DP4-09 | Karanlık tema + matris düzeni | B-32, B-30 |
| DP4-10 | /sayim liste durumu | B-33 |
| DP4-11 | /denetim-izi doğruluğu | B-62 |
| DP4-12 | Ters tarih / geçersiz süzgeç (6 rota) | B-64 |
| DP4-13 | Kopyala-yapıştır metin temizliği | B-65 |
| DP4-14 | Ham teknik değer gösterimi | B-59 |
| DP4-15 | Sayı biçimi tek kültür | B-8 |
| DP4-16 | Rapor tutarlılığı | B-7 |
| DP4-17 | kur/guncel tekilleştirme | B-57 |
| DP4-18 | Yanıltıcı/etkisiz denetimler | B-67 |
| DP4-19 | Onay ve geri alma | B-66 |
| DP4-20 | Erişilebilirlik (toplu) | B-63 |
| DP4-21 | /ayarlar yayın künyesi + /kilavuz üretimi | B-2, B-11 |

**Sonuç:** 17/17 madde kimliğe bağlandı; kimliksiz kalan madde **0**. B-50 KAPANDI.

## 7. Y-42 — TRİYAJ (INTAKE) KURALI (bağlayıcı)

1. **24 saat kuralı.** Her QA turu / hasım turu / kurucu bulgusu, tur bittikten sonra **24 saat içinde** kimliklenir. Kimliksiz bulgu kapıya giremez.
2. **Dört kutu.** Her bulgu tam olarak birine yazılır: `mevcut pakete ek` · `sonraki dalgada yeni paket` · `1.1` · `gerekçeli kapsam dışı`.
3. **Damga.** Damga `ham/triyaj-kayit.json` içinde tutulur (bulgu → kimlik + kutu); paket/sürüm kararı ayrıca `ham/surum.json`'a yazılır (P-01 alanı). **Damgasız bulgu kapıyı kırar** — `BULGU_DAMGASIZ` sayacı `dotnet/denetim.sh` içinde cırcırdır ve 0'dan büyük olamaz.
4. **Karar ekip oyuyla.** Kutu seçimi tek ajanın kararı değildir; dalga kapısında oylanır ve `ORKESTRATOR-DURUM.md`ye tek satır yazılır.
5. **Kapsam freni (Ramazan K1).** Her dalga sonunda o dalgada doğan **yeni kimlik sayısı** ölçülür ve `ORKESTRATOR-DURUM.md`ye yazılır. Ardışık iki dalgada yeni kimlik bir önceki dalganın %50'sinin altına inmiyorsa kapsam kurulu toplanır (BK-33).

**DALGA 0 ölçümü:** `YENI_KIMLIK_DALGA = 14` (B-59…B-70 = 12 · Y-43, Y-44 = 2). Taban budur; DALGA 1'in hedefi ≤ 14.

## 8. R-16 — 34-Ö paket-aile eşlemesinin satır satır doğrulanması

`EKRAN-ALAN-DOGRULAMA-2026-09-18.md` 10, 11, 12, 16 ve 17. satırlarda aile olarak **"34-Ö (doğrulanmadı)"** yazmıştı. `MIMARI-KALAN-PAKETLER-2026-09-15.md` ile karşılaştırıldı:

| Doğrulama satırı | Orada yazan aile | MIMARI-KALAN karşılığı | Sonuç |
|---|---|---|---|
| 10 — HURDA SATIŞI | 34-Ö | PLAN P-20 (Y-10), dalga 5 | **Eşleme geçersiz**: 34-Ö bir rapor paketidir, işlem kapısı değil |
| 11 — Virman / şubeler arası transfer | 34-Ö | PLAN P-19 (Y-11) | **Eşleme geçersiz** |
| 12 — Gider kategorisi | 34-Ö | PLAN P-19 (Y-12) | **Eşleme geçersiz** |
| 16 — Stok yaşlandırma raporu | 34-Ö (rapor katalog kaydı) | PLAN P-25 (Y-16) — rapor ailesi | **Eşleme geçerli** |
| 17 — Kalan işlem kapıları | 34-Ö | PLAN P-21 (Y-17…Y-23) | **Eşleme geçersiz** |

**Karar (R-16 kapanışı):** "34-Ö" etiketi yalnız **rapor katalog/ölçek** işleri için geçerlidir (B-8, Y-16, Y-31). İşlem/defter kapıları 34-Ö'ye değil P-19/P-20/P-21'e bağlıdır. "tahmindir" etiketi kalkar; PLAN § 12'deki paket sütunu zaten bu dağılımı yazmaktadır — **çelişki yok, doğrulandı**.

## 9. Bu belgenin sayaçları (denetim.sh, cırcır)

| Sayaç | Değer | Ölçüm komutu |
|---|---|---|
| `KIMLIK_KOKENSIZ` | 0 | `KAPSAM-ENVANTERI.md` § 2 tablosunda Kaynak hücresi `—` olan satır sayısı |
| `BULGU_DAMGASIZ` | 0 | `ham/triyaj-kayit.json` içinde `kimlik` ya da `kutu` alanı boş kayıt sayısı |
| `YENI_KIMLIK_DALGA` | 14 | DALGA 0'da doğan kimlik sayısı (raporlanır, kapı kırmaz) |

---

**Bu belge olmadan hiçbir dalga başlamaz (PLAN § 1 md.6 + BK-18).**
