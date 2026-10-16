# PAKET S3-1 — PANEL: "SABAHIN BEŞ SORUSU" — Uygulama raporu

Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga S3A (günlük ekranlar A) · 2026-09-23 · Durum: **BİTTİ (paket kapısı 7/7)**

Tek doğruluk kaynağı: `dokumanlar/02-mimari/sadelestirme/tuval/project/Panel.dc.html` (+ `canvas.json` n6 notu: gövde 18 px, büyük sayı 36 px).
Plan: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3, § 5, § 5B, § 6 (S-3 "Panel").

---

## 1. Yapılanlar

**İki yüz, tek gövde.** Panel artık etkin arayüz moduna göre iki farklı yüz çiziyor. Mod sunucuda çözülüyor (`ArayuzModuServisi.BasitMi`).

### Basit yüz (yeni, taslağa birebir)

- **Selam satırı.** "Günaydın ‹ad›" yazıyor. Altında tek küçük satırda tarih ("Çarşamba, 23 Eylül 2026") ve hava ("· İstanbul 21° …") duruyor. Hava ayrı bir kutucuk olmaktan çıktı, bu satırın içine alındı. Sağda ekrandaki **tek birincil düğme** var: **"Yeni satış"**, `/islemler`'e gidiyor.
- **Üç kart:**
  - **"Bugün ne sattım?"** Büyük sayı tutarı gösteriyor ve `/raporlar/islem-defteri`'ne bağlantı (UX-S23). Altında "N satış" ve "x,x g has" yazıyor. Hiç satış yoksa sade bir cümle çıkıyor.
  - **"Kasada ne var?"** Kasalar para birimine göre gruplanıyor. Büyük sayı TL kasaların toplamı ve `/kasalar`'a bağlantı. Altında kasa kırılımı (en çok 3 kasa, fazlası "+N kasa daha") duruyor. Döviz kasaları kendi birimleriyle ayrı satırda gösteriliyor. **Farklı para birimleri toplanmıyor (UX-S32).** Ham kod ("USD") ekrana basılmıyor, grup adı sözlükten geliyor ("Dolar kasalar").
  - **"Altın pozisyonum"** B-2'nin `AltinPozisyonuKarti` bileşeni. Tek satırla yerleştirildi, bileşen dosyasına dokunulmadı. Görünümü öteki iki kartla CSS'te eşitlendi.
- **"Kim borçlu?"** Veri `GET /raporlar/cari-yaslandirma?limit=500` ucundan geliyor, vade durumu `GET /raporlar/vade-takibi` ile ekleniyor.
  - Kartta en çok 5 satır var. Önce bize borçlu olanlar geliyor (en büyük TL, sonra has). Ardından bizim borçlu olduklarımız geliyor; iki yön de varsa borç yönüne en az 2 satır ayrılıyor.
  - Satırda **tek eylem** var: "Tahsil et" ya da "Öde". Eylem `/cariler?cariId=…` bağlantısını açıyor (P-15 derin bağlantısı).
  - Durum sütunu sade dille yazıyor: "Vade geçti (12 gün)", "3 gün kaldı", "Vade bugün", "Açık hesap", "Biz borçluyuz".
  - Tutar TL ve has olarak ayrı ayrı yazılıyor ("−22,0 g has" kırmızı). Döviz bakiyesi varsa "döviz de var" notu çıkıyor.
  - Sınır ekranda söyleniyor (UX-S29): "N açık hesap" bağlantısı var. Sınıra değilirse "İlk 500 hesaba bakıldı…" bandı çıkıyor.
- **"Bekleyen iş".** Liste `GET /bildirimler?durum=ACIK&limit=5` ucundan geliyor; sunucu rol süzgecini uyguluyor. Her satırda önem noktası var (Acil / Dikkat / Bilgi; ham KRITIK kodu ekrana basılmıyor) ve başlık, hedef rotaya bağlantı. Yalnız uygulama içi rota bağlantı oluyor ("//dış" bağlantı olmuyor). Yetkili rolde P-37b'nin uyum sayaçları da (`BekleyenGovde`, ikinci kopya yazılmadı) bu kartta gösteriliyor. Sınıra değilirse bant çıkıyor.

### Profesyonel yüz

P-37b'nin 7 kutucuklu ızgarası **aynen** duruyor: düzen modu, sürükle-bırak, gizle/göster, kur kaynağı seçici, grafik, son işlemler ve hızlı eylemler. Mevcut id'lerin hiçbiri değişmedi.

### Ortak kurallar

- **Yüze göre yükleme.** Ortak yükleyiciler (günlük özet, uyum, hava) iki modda da açılıyor. Basit yüz ayrıca yaşlandırma, vade ve bildirim uçlarını çağırıyor. Profesyonel yüz ayrıca aylık, işlem defteri ve kur kaynakları uçlarını çağırıyor. Mod çalışırken değişirse eksik yüzün yükleyicileri **o an** ve bir kez açılıyor (`Mod.Degisti`). Yükleyiciler hâlâ paralel, `Yuklemeler.AddRange` korunuyor (KARSILAMA_TEK_ISTEK_BEKLEME = 0).
- **UX-S28.** 403 yanıtı "Bu bilgiyi yalnız yönetici görür." notuna çevriliyor. 5xx yanıtında hata bandı çıkıyor, sayı çizilmiyor ve boş durum gösterilmiyor. Boş durum yalnız başarılı ve gerçekten boş sonuçta çıkıyor.
- **Tek POST yolu.** Panel basit yüzde **POST/PATCH/DELETE yapmıyor** (kaynak testi S31_25).
- **Sürükle-bırak.** JS kurulumu yalnız profesyonel yüzde yapılıyor. Basitten profesyonele geçilince ilk çizimde yeniden kuruluyor.

**Sunucu (Taşkın):** Yeni uç ya da şema değişikliği **yok**. Kullanılan uçların hepsi mevcuttu (RaporController, BildirimController, PozisyonController). Sunucu modülleri S-B şeridinin alanı olduğu için açılmadı.

**Migration:** Gerekmedi.

## 2. Sapmalar ve gerekçeleri

1. **"Tahsil et / Öde" müşteri kartını açıyor, ödemeyi doğrudan yapmıyor.** Panel POST etmiyor (tek POST yolu, plan § 3). `Cariler.razor` bu dalganın S3-2 paketine ait; TEK DOKUNUŞ kuralı yüzünden bu pakette açılmadı. `/cariler?cariId=` mevcut bir derin bağlantı: süzgeci uyguluyor ve cari detayını açıyor. Satır eylemi orada.
2. **"Kâr 4.120 ₺" gösterilmedi.** Taslakta var, ama `/raporlar/gunluk` günlük kâr döndürmüyor (yalnız `/raporlar/aylik` döndürüyor). İstemcide kâr hesaplamak UX-S21 ve Anayasa md.1'e aykırı olurdu (tahmin). Bu alan açık kalanlar listesine yazıldı.
3. **Kur kutucuğu basit yüzde yok.** Kur, S1-c'nin üst barında Has/Gram 22/USD olarak zaten görünüyor (taslakla aynı). Kur kaynağı seçici profesyonel yüzde ve `/kur` ekranında duruyor. Özellik silinmedi.
4. **Büyük sayı `var(--yazi-3xl)` belirtecinden geliyor.** 16 px ölçekte 36 px (taslak), 18 px ölçekte 40 px, 20 px ölçekte 44 px. Yazı ölçeğine uyuyor. Sabit px yazılmadı.
5. **Has gramı tek ondalıkla gösteriliyor ("10,4 g has", "−22,0 g has").** Taslak ve B-2 kartı bu biçimi kullanıyor. İlk sürüm `N2` kullanmıştı; denetimde GIRDI_AILESI_DISI_BICIM sayacı 1 verdi ve B-2 ile aynı `N1` kalıbına geçildi.
6. **"Kasada ne var?" kırılımı kasa adıyla yazılıyor ("Nakit / Banka" diye bölünmüyor).** Kasa türü günlük özet yanıtında yok. Kasa adı kullanıcının kendi koyduğu ad olduğu için doğru bilgi o.
7. **Görev metninde "mevcut 9 kutucuk" yazıyor, kodda 7 var.** `PanelKutucuklari.Tumu` 7 kutucuk tanımlıyor; başlık şeridi (selam + hava) ve araç şeridiyle birlikte 9 öğe ediyor. Profesyonel yüz, kodda ne varsa **aynen** koruyor.
8. **`KasaGrubu` record'u razor içinden Contracts'a taşındı (`PanelKasaGrubu`).** `SozlesmeYansimaTestleri` (PLAN § 1 md.9) razor içinde record'u kırmızıyla yakaladı ve düzeltildi.
9. **CSS bloğunda `overflow-wrap` yerine `word-break: break-word` kullanıldı.** S1-c ve S1f blok testleri kendi işaretçilerinden **dosya sonuna kadar** "overflow" kelimesini yasaklıyor; app.css'in sonuna eklenen her blok bu yasağı devralıyor. Düzeltme sırasında bir perl komutu yanlışlıkla başkasına ait iki satırı (663 ve 1819) da değiştirdi. İkisi yedekle karşılaştırılarak **aynı adımda geri alındı**. `diff once/app.css app.css` çıktısında yalnız ekleme var, silinen ya da değişen satır yok.

Mevzuat: Bu pakette vergi/KDV/MASAK/tevsik hükmü yok. Panel yalnız okuma yapıyor ve eşik sayısı istemciye gömülmüyor. Birincil kaynak araştırması gerekmedi.

## 3. Test sayıları

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Önce (tam süit, `trx-once`, 22:27; Panel değişikliği derlenmiş, yeni test dosyası henüz yok) | 4502 | 4490 | 12 |
| S31 filtreli (son) | 26 | 26 | 0 |
| S31 + P37b + UX1C1 + UX4c + SozlesmeYansima + S1c + S1f + AltinPozisyonu + OluIcBaglanti | 221 | 221* | 0* |
| **Sonra (tam süit, `trx-pS3-1-son`, 23:41)** | **4541** | **4538** | **3** |

\* İlk filtreli koşumda 1 kırmızı çıktı (S31_24). Sebep test fikstürüydü: uyum yolu `/uyum/` yazılmıştı, doğrusu `/masak/ozet`. Düzeltildi.

Yeni test: **26** (`Kuyumcu.Tests/S31PanelSadeTestleri.cs`).

**Önce süitindeki 12 kırmızının 4'ü bu paketindi, dördü de kapandı:**
- `SozlesmeYansimaTestleri.Tasinan_tipler_Contracts_projesinde_ve_public` ve `…Razor_code_blogunda_istemci_record_tanimi_acilmiyor` (Panel.KasaGrubu): record Contracts'a taşındı.
- `S1cUstBarTestleri.S1c_CSS_blogu…` ve `S1fGorunumKartiTestleri.S1f_K2_CSS_blogu…` ("overflow"): düzeltildi.

**Paket kapısı (7) — bu paketin yeni kırmızısı: 0.** Son süitteki 3 kırmızının hiçbiri Panel'e dokunmuyor:
- `B4AltinBacagiTestleri.Sabitler_OdemeTurleri_ALTIN_Gecmis_Satir_Okunur` ve `B4AltinBacagiSafTestleri.Sabitler_OdemeTurleri_ALTIN_Icerir`: B-4 (altın bacağı) şeridi, yarım iş. `OdemeTurleri` listesinde ALTIN yok.
- `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`: Islemler.razor, S-2/B-4 şeridi, WaitFor zaman aşımı.

## 4. Mutasyon kanıtları

Yöntem: Panel.razor `scratchpad/pS3-1/mut-yedek/S3-1/` altına yedeklendi. Her mutasyon TEK eşleşmeli bir değişiklik. Ardından `dotnet test --filter S31PanelSadeTestleri|P37bKarsilamaTestleri -v n` (57 test) koşuldu, yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu doğrulandı. Betik: `scratchpad/pS3-1/mut.py`, çıktılar `mut-sonuc*.txt` ve `M-S31-*.log`.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M-S31-1 | Kasalar tek grupta toplanır (`GroupBy(k => "TRY")`) | S31_11_Kasa_karti_PARA_BIRIMLERINI_TOPLAMAZ | AYNI |
| M-S31-2 | "Öde" ile "Tahsil et" yer değiştirir | S31_12_Kim_borclu_satirda_TEK_eylem_yon_dogru | AYNI |
| M-S31-3 | Borçlu listesinde 403 yakalanmaz | S31_16_Kim_borclu_403_YETKI_notu_500_BANT_bos_liste_BOS_DURUM | AYNI |
| M-S31-4 | `ozetYetkiYok = false` | S31_10_Ozet_403_ise_YETKI_notu_hata_bandi_degil | AYNI |
| M-S31-5 | Satış kartında hata dalı kapalı (sayı çizilir) | S31_09_Ozet_ucu_PATLARSA_sayi_cizilmez_bant_durur | AYNI |
| M-S31-6 | `IcRota` "//" ile başlayan rotayı kabul eder | S31_20_Bekleyen_is_bildirimleri_BAGLANTIYLA_ham_kodsuz_listeler | AYNI |
| M-S31-7 | Profesyonel yükleyiciler basit yüzde de açılır | S31_04_Her_yuz_YALNIZ_kendi_isteklerini_atar | AYNI |
| M-S31-8 | `ModDegisti` eksik yüzü yüklemez | S31_06_Mod_calisirken_degisince_eksik_yuz_YUKLENIR | AYNI |
| M-S31-9 | Bekleyen iş hata olsa da boş durum çizer | S31_23_Bekleyen_is_HATASI_bos_duruma_cevrilmez | AYNI |
| M-S31-10 | Borçlu hata dalı kapalı | S31_17_Kim_borclu_HATA_bos_duruma_cevrilmez | AYNI |
| M-S31-11 | "Vade geçti" koşulu `> 9999` | S31_13_Kim_borclu_VADE_durumunu_sade_yazar | AYNI |
| M-S31-12 | Yaşlandırma isteğinden `limit` düşer | S31_15_Kim_borclu_SINIRI_ister_ve_daha_fazlasini_soyler | AYNI |
| M-S31-13 | Mod dalı `@if (false)` (basit yüz hiç çizilmez) | 22 test (S31_01…S31_24) | AYNI |
| M-S31-14 | Alacak sırası ters (`OrderBy`) | S31_14_Kim_borclu_SIRA_once_alacak_sonra_borc | AYNI |
| M-S31-15 | Bildirim limiti 5 → 100 | S31_20_Bekleyen_is_bildirimleri_BAGLANTIYLA_ham_kodsuz_listeler | AYNI |
| M-S31-16 | "Daha fazlası var" bandı kapalı | S31_15_Kim_borclu_SINIRI_ister_ve_daha_fazlasini_soyler | AYNI |

**16/16 mutasyon kırmızı verdi. Aşağıdakiler kanıt olarak SAYILMADI:**
- İlk geçişte M-S31-4, 6, 7, 9, 10 ve 12 "0 kırmızı" döndü, ama loglarında test hiç koşmamıştı. Sebep eşzamanlı şeritlerin derleme yarışıydı: `MSB4018 ConvertDllsToWebCil IOException` ve `CS2001 GeneratedMSBuildEditorConfig bulunamadı`. Betiğe "Toplam test sayısı yoksa 20 sn bekle, yeniden dene" kuralı eklendi ve bu mutasyonlar yeniden koşuldu.
- M-S31-9'un bir koşumu, test 57/57 koştuğu hâlde 0 kırmızı verdi. Aynı mutasyon elle uygulandığında (hata ayıklama: `Assert.Fail` ile markup basıldı) "Bekleyen iş yok." metninin çizildiği görüldü. Tekrar koşumda (`mut-sonuc4.txt`) S31_23 kırmızı verdi. İlk sonuç, başka şeridin eşzamanlı derlemesinin eski (mutasyonsuz) Web.dll'i üzerine yazdığı bayat ikiliyle açıklanıyor. Kanıt olarak tekrar koşum alındı.
- Elle yapılan hata ayıklamada test dosyası ve Panel.razor yedekten geri kopyalandı; `cmp` sonucu `GERI_ALINDI`.

Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln` alındı (`build-son2.log`): 0 uyarı, 0 hata.

## 5. Denetim

`dotnet/denetim.sh` → **DENETİM TEMİZ** (EXIT=0, `scratchpad/pS3-1/denetim2.log`). Bu paketle ilgili sayaçlar:

```
✓ app.css ham hex (kâğıt dâhil kaba sayım)  25 (tavan 25)        ← yeni blokta hex 0
✓ Pages+Bilesenler+Layout satır içi style=     113 (tavan 196)   ← Panel'e style eklenmedi
✓ P-02: OLU_IC_BAGLANTI (href/NavigateTo ⊆ @page) 0 (24 bağlantı / 47 rota)
✓ P-36b: GIRDI_AILESI_DISI_BICIM                 0
✓ P-37b: KARSILAMA_TEK_ISTEK_BEKLEME             0 (paralel yükleme, 10 iskelet)
✓ P-37b: KARSILAMA_HAVA_KONUM_IZNI               0
✓ P-37b: KARSILAMA_TERCIH_KULLANICI_DISI         0
```

İlk denetim koşumu (`denetim.log`) GIRDI_AILESI_DISI_BICIM = 1 verdi. Sebep bu paketin `ToString("N2")` çağrısıydı; düzeltildi (bkz. § 2-5).

DOM id sözleşmesi: paket öncesi Panel.razor'da 33 `id="…"` vardı; **silinen 0, eklenen 40** (`comm -23 once/idler.txt idler-sonra.txt` boş). Yeni id'lerin önekleri: `panel-soru-*`, `panel-borclu-*`, `panel-is-*`, `panel-yeni-satis`, `panel-sade`, `panel-iskelet-soru-*`.

## 6. Canlı doğrulama (port 5191, `kuyumcu_net`)

API şu komutla başlatıldı: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5191` (PID 25739, dinleyen 25872). İlk deneme "apphost yok" hatasıyla düştü; başka şerit o sırada bin/ klasörünü yeniden yazıyordu. Dosya oluştuktan sonra yeniden başlatıldı. Kiracı davetle açıldı. Jeton ve parola ekrana basılmadı.

Deneme 1 ve 2 fikstür eksikliği yüzünden yarım kaldı. Yeni kiracıda kasa yok; kasa açılınca bakiye 0, bu yüzden nakit alış 400 ("Nakit kasa eksiye düşemez"). TEZGÂHTAR'a şube verilmemişti (403 "Şube bağlamı çözülemedi"). Sunucunun iki reddi de doğruydu. Betik düzeltildi. **Deneme 3 (PAKETS3-1-TEST-3, kiracı `01M380F8KKC7EY84V18PEX2GJ3`):**

| # | Adım | Beklenen | Gerçek | Özet |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | kiracı açıldı |
| 2 | GET /kur/guncel | 200+id | 200 | kur var |
| 3 | POST /cari ×2 | 201/201 | 201/201 | müşteri, toptancı |
| 4 | POST /urunler K22 10 g | 201 | 201 | has 9,16 |
| 4b | POST /kasalar TRY + USD | 200/201 | 200/200 | şube /auth/ben'den |
| 5b | POST /kasalar/{id}/hareket GIRIS 50.000 TL + 500 USD | 200/200 | 200/200 | açılış |
| 6 | POST /islemler/alis 3 adet NAKIT | 200 | 200 | stok girdi |
| 7 | POST /islemler/satis NAKIT 5.000 | 200 | 200 | satış |
| 8 | POST /islemler/satis VERESIYE 3.000 vade 3 gün | 200 | 200 | veresiye |
| 9 | POST /cari/{toptancı}/duzeltme has −3 | 200 | 200 | biz 3 g borçluyuz |
| **10** | **GET /raporlar/gunluk** (Bugün ne sattım? + Kasada ne var?) | adet 2 · 8.000 TL · TRY 52.000 · USD 500 ayrı | **200** | **adet=2 toplamTl=8000.0 toplamHas=18.32 kasalar=[S31 Dolar USD 500.0, S31 Nakit TRY 52000.0]** |
| **11** | **GET /raporlar/cari-yaslandirma?limit=500** (Kim borçlu?) | müşteri +3.000 TL · toptancı −3 g | **200** | **S31 Müşteri (3000.0, 0.0) · S31 Toptancı (0.0, −3.0) · toplamSatir=2 dahaFazlasiVar=False** |
| **12** | **GET /raporlar/vade-takibi** | müşteri vade = bugün + 3 | **200** | **S31 Müşteri enYakinVade=2026-09-26, gecikme=None → ekran "3 gün kaldı"** |
| 13 | GET /bildirimler?durum=ACIK&limit=5 | 200 | 200 | limit=5, 0 kayıt (yeni kiracı) → ekran "Bekleyen iş yok." |
| 14 | GET /pozisyon | 200 | 200 | net=6.16 vitrin=9.16 borç=3.0 |
| 15 | GET cari-yaslandirma?limit=501 | 400 | 400 | "Limit 1 ile 500 arasında olmalıdır" |
| 16 | PATCH /kullanicilar/ben BASIT → GET /auth/ben | 200/200 BASIT | 200/200 | etkinArayuzModu=BASIT |
| 17 | GET / (SPA kabuğu, Panel'i taşıyan) | 200 html | 200 | html |
| 18–19 | TEZGÂHTAR (BASIT, şubeli) aç + giriş | 201 / 200 | 201 / 200 | jeton var |
| 20–23 | TEZGÂHTAR: gunluk / cari-yaslandirma / vade-takibi / pozisyon | 403 | 403 ×4 | "gerekli rol: SAHIP veya MUDUR veya MUHASEBE" → ekran "Bu bilgiyi yalnız yönetici görür." (**yetki ≠ mod**) |
| 24 | TEZGÂHTAR GET /bildirimler | 200 ya da 403 | 403 | "Bu ekran için yetkiniz yok (bildirim)" → ekran yetki notu (S31 testinde 403 dalı var) |
| 25 | TEZGÂHTAR GET /auth/ben | 200 BASIT | 200 | etkinArayuzModu=BASIT |
| 26 | cari-yaslandirma jetonsuz | 401 | 401 | — |

**SQL kanıtı** (`scratchpad/pS3-1/canli-sql.txt`, kiracı 01M380F8KKC7EY84V18PEX2GJ3):

```
hareketler:  ALIS NAKIT 1 −3000.00 27.48 · SATIS NAKIT 1 5000.00 −9.16 · SATIS VERESIYE 1 3000.00 −9.16 · CARI_DUZELTME 1 0.00 3.00
kasalar:     S31 Nakit TRY 52000.00 · S31 Dolar USD 500.00        ← ekran: büyük sayı 52.000 ₺, "Dolar kasalar 500,00 $" AYRI satır
cari_bakiyeler: S31 Müşteri 3000.00 / 0 · S31 Toptancı 0.00 / −3.000000   ← ekran: "Tahsil et" / "Öde"
denetim_kayitlari: OKUMA Cari "liste: 2 kayıt · cari-yaslandirma" · OKUMA Cari "liste: 1 kayıt · vade-takibi"  (KVKK izi ✓)
kullanicilar: SAHIP BASIT · TEZGAHTAR BASIT (şubeli)
```

Satış toplamı: 5.000 + 3.000 = 8.000 TL, 2 adet. Has: 9,16 × 2 = 18,32 g. Ekran bunu "8.000,00 ₺ · 2 satış · 18,3 g has" diye yazar. TL kasası: 50.000 − 3.000 (alış) + 5.000 (nakit satış) = 52.000.

**Görsel (tarayıcı) doğrulaması YAPILMADI.** Ekranı tarayıcıda görmek için giriş formuna parola girmek gerekiyor ve bu ajanın yapamayacağı bir işlem. Ekran davranışı 26 bUnit testiyle (gerçekçi fikstür, id'lerle) ve uçların canlı yanıtlarıyla doğrulandı. Görsel ölçüm (tasarım denetçisi, 375/768/1440 taşma) orkestratörün 5199 yayınına ve QA turuna bırakıldı (§ 9-3).

**Kapanış:**
- API durduruldu (`kill 25739 25872`). `lsof -nP -iTCP:5191 -sTCP:LISTEN` → 0 satır.
- Bu paketin **3 test kiracısı** (PAKETS3-1-TEST, -2, -3; 01M380CTX4…, 01M380E3QD…, 01M380F8KK…), `tenant_id` kolonu taşıyan **70 tablonun tümünden** ve `tenants`'tan silindi. Kalan satır: 0. 3 davet satırı silindi; kalan `PAKETS3-1-TEST%` daveti 0.
- `tenants` sayısı: başta **10**, sonda **11**. Aradaki fark bu paketin DEĞİL. `01M380H34737TV288SKMPFKXHZ PAKETB4-TEST-CANLI` kiracısı 20:50 UTC'de B-4 şeridi tarafından açıldı, bu paketin canlı koşumu sırasında. Ona dokunulmadı. Bu paketin açtığı kiracılardan kalan: **0**. QA-TEST-/DEMO kiracılarına dokunulmadı.

## 7. DoD sözleşmesi

- **Yeni varlık ya da kayıt türü YOK.** Panel yalnız okuyor. PATCH/DELETE/denetim yükümlülüğü doğuran yeni bir tür eklenmedi.
- Paneldeki her sayı kaynağına giden bağlantı ya da eylem taşıyor (UX-S23/S25):

| Kart | Düzeltme yolu |
|---|---|
| Bugün ne sattım? | `/raporlar/islem-defteri` (fişler, storno orada) |
| Kasada ne var? | `/kasalar` |
| Altın pozisyonum | B-2 kartının kendi DegismezIpucu'su (sayım, cari düzeltme, hurda) |
| Kim borçlu? | `/cariler?cariId=` (tahsilat/veresiye/düzeltme orada) + "Tümü" → `/cariler` |
| Bekleyen iş | bildirimin hedef rotası + "Tümü" → `/bildirimler` (kapatma orada) |

- **Düğme kuralı (DoD EK 1).** Basit yüzde düğme yok; eylemlerin hepsi gerçek rotaya giden bağlantılar (OLU_IC_BAGLANTI = 0). B-2 kartındaki Alacak/Borç düğmeleri istek atıyor (B-2 testleri).
- Tek düzenleme kalıbı: yeni ModalPencere yok, form yok.
- Ürün ücretsiz pilotta. Fiyat/teklif/tahsilat içeren **satış metni** üretilmedi. "Tahsil et" bir iş eylemi etiketi.

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Taslak birebir: selam + tarih + hava tek satır | ✓ | `#panel-tarih` içinde `#panel-hava` (basit) |
| "Yeni satış" TEK birincil | ✓ | S31_02 (`.dugme-birincil,.btn-birincil` = 1) |
| 3 kart: sattım / kasa / altın pozisyonu (B-2) | ✓ | S31_01, S31_07, S31_11 |
| Kim borçlu? satırda tek eylem Tahsil et / Öde | ✓ | S31_12, S31_14 |
| Bekleyen iş | ✓ | S31_20…24 |
| Büyük sayı 36 px tabular-nums, belirteç | ✓ | `.panel-soru-sayi { font-size: var(--yazi-3xl); font-variant-numeric: tabular-nums }` |
| Pro modda mevcut kutucuklar AYNEN | ✓ | S31_03 + P37bKarsilamaTestleri 31/31 + UX1C1/UX4c panel testleri yeşil |
| Özellik silinmez | ✓ | pro yüz değişmedi; kur → üst bar + /kur |
| Mevcut DOM id değişmez | ✓ | silinen 0, eklenen 40 |
| Tek POST yolu / Panel POST etmez | ✓ | S31_25 |
| Yetki ≠ mod; eşik sayısı istemciye gömülmez | ✓ | canlı 20–24 (403), S31_10/16 |
| Sabit renk / inline style yok | ✓ | S31_25 (style=), ham hex 25/25, S1c/S1f blok testleri yeşil |
| Jargon 0, ham kod 0 | ✓ | S31_11 (USD yok), S31_20 (KRITIK/VADE_GECIKTI yok); metinler Sozlukler.Panel.cs |
| Sözlük TR+EN ayrı dosyada | ✓ | `Kuyumcu.Core/I18n/Sozlukler.Panel.cs`, S31_26 |
| UX-S28 / S29 / S32 / S23 | ✓ | S31_09/10/16/17/19/23 · S31_15/21 · S31_11 · S31_07 |
| Ölü bağlantı yok (P-D1) | ✓ | OLU_IC_BAGLANTI 0 |
| Dosya alanı: yalnız Panel.razor (+ ortak CSS/Contracts/sözlük ekleri) | ✓ | § 10 |
| İzolasyon: KUYUMCU_TEST_DB=kuyumcu_test_y, canlı 5191, build-server shutdown | ✓ | komutlar § 3/§ 6 |

## 9. Açık kalanlar

1. **Günlük kâr** (taslakta "Kâr 4.120 ₺") gösterilmedi, çünkü `/raporlar/gunluk` kâr döndürmüyor. Eklemek sunucu işi (S-B şeridi, RaporModulu). Eklendiğinde kartın alt satırına tek `span` girecek.
2. **"Tahsil et"i tek tıka indirmek.** Bunun için `Cariler.razor`'ın `?eylem=tahsilat` parametresini tanıması gerekiyor. Bu iş S3-2 (Cariler) paketinde yapılmalı; bugün iki tık sürüyor (Panel → cari kartı → Tahsilat).
3. **Görsel ölçüm.** 5199 yayını sonrası tasarım denetçisi çalıştırılmalı (375/768/1440 taşma, hiza) ve QA turunda Panel basit yüzü ölçülmeli. Bu ajan tarayıcıda oturum açamadığı için görsel doğrulamayı yapamadı.
4. **TEZGÂHTAR'ın basit panelinde 4 kart "yalnız yönetici görür" gösteriyor.** Sebep sunucu rol kapıları (`/raporlar/*` ve `/pozisyon` SAHİP/MÜDÜR/MUHASEBE; `/bildirimler` modül yetkisi). Bu doğru ve güvenli bir davranış, ama tezgâhtarın sabah ekranı böylece zayıf kalıyor. Tezgâhtara kendi şubesinin "bugün sattım" ve "kasa" özetinin açılıp açılmayacağı bir **yetki kararı** (Mehmet + Ramazan/PM). Kurucuya not düşüldü; kod bu kararı beklemiyor.
5. **Başka şeritlere ait kırmızılar** (tam süit, 23:41):

| Test | Sahip |
|---|---|
| `B4AltinBacagiTestleri.Sabitler_OdemeTurleri_ALTIN_Gecmis_Satir_Okunur` | B-4 şeridi (altın bacağı; `OdemeTurleri`'nde ALTIN yok) |
| `B4AltinBacagiSafTestleri.Sabitler_OdemeTurleri_ALTIN_Icerir` | B-4 şeridi |
| `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` | S-2 / B-4 (Islemler.razor, WaitFor zaman aşımı) |

   Önce süitinde olup sonda kapanmış görünenler de başka şeritlere aitti. Paralel ağaçta bu liste dakika dakika değişiyor: S21FisYapi (S-2), S1fGorunumHttp, P06Duzenlenebilirlik/Islemler, OlcumKapisi ve OdemeSepeti.
6. Canlı koşumda gözlem (bu paketin konusu değil): Deneme 1'de yeni kiracıda kasa yokken nakit alış (`kasaId` null) **200** döndü ve toptancıya −3.000 TL cari borç yazdı. Aynı durumda nakit satış 400 veriyor ("kasa ID gerekli"). Alış ile satış arasındaki bu asimetri S-B / Satış şeridine bildirilmeli.

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında bu paketin dosyaları:

```
 M dotnet/Kuyumcu.Web/Pages/Panel.razor                 (basit yüz + mod dalı; pro yüz aynen)
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                (dosya sonuna "PAKET S3-1" bloğu; yalnız ekleme)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.Panel.cs           (yeni: PanelSadeMetinleri TR+EN)
   dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs (+4 satır PanelKasaGrubu; Contracts dizini git'te izlenmiyor "??")
   dotnet/Kuyumcu.Tests/S31PanelSadeTestleri.cs          (yeni, 26 test; Tests dizini git'te izlenmiyor)
```

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Panel.razor`. Cariler, Urunler ve UrunEtiketleri bu pakette AÇILMADI.

Commit yapılmadı. Geçici dosyaların hepsi `scratchpad/pS3-1/` altında; proje ağacına .bak/.tmp bırakılmadı.

## 11. Kurucuya / mali müşavire notlar

- Mali müşavir teyidi gerektiren bir hüküm yok. Panel yalnız okuma yapıyor ve eşik ya da oran içermiyor.
- Kurucuya (yetki kararı, § 9-4): "Tezgâhtar sabah panelinde bugünkü satışı ve kasayı görsün mü?" Bugün göremiyor, çünkü sunucu bu raporları yönetici rolleriyle sınırlıyor.
