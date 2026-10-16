# PAKET RAPOR-4 — Rapor kalanları + KAR-HIZ · Uygulama raporu

**Tarih:** 2026-10-08 11:38–15:05 (tur 1) · 15:50–17:45 (tur 2, § T2) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** RAPOR-4 (t1)
**Kaynak (tek doğruluk):** `dokumanlar/09-uygulama-kayitlari/ham/RAPOR-4-BULGULAR.md` + görev metnindeki VERİ ÖNCELİĞİ / KAR-HIZ maddeleri.
**COMMIT YOK. Dosya silinmedi.** Kanıtlar: `dokumanlar/09-uygulama-kayitlari/ham/RAPOR-4-olcum/`.

---

## T2. TUR 2 — önceki doğrulamanın bulguları ve kapanışları (2026-10-08 15:50–17:45)

**Kural:** paket en çok 2 tur; bu ikinci ve son tur. Her bulgu kapatıldı ya da (dosya alanı dışında olduğu için) sahibiyle "açık kalan"a yazıldı. Tur 2 kanıtları: `ham/RAPOR-4-olcum/tur2/`. İzolasyon: canlı/yük API'si bu kez **ayrılan 5160**, vekil **5161** (5160/5161 boştu), CDP 9701; 5162/5163 başka şeritteydi — dokunulmadı.

| # | Bulgu (hasım/hakem) | Kök neden (ölçüldü) | Yapılan | Kanıt |
|---|---|---|---|---|
| T2-1 | [KRİTİK] KZ Grafikler "Satış" çubuğu 89.900 (maliyeti BİLİNEN satış), Özet/Panel/Köprü toplam satışı 635.528 | `GelirGiderNoktalari` KISMI dönemde tek çubuğa bilinen kümeyi "Satış" adıyla koyuyordu | Maliyeti eksik satış varken İKİ çubuk: önce **"Toplam satış"** (= Özet "Toplam satış" alt satırı = Panel/Köprü net hasılat), sonra Özet kartıyla **AYNI etiketli "Maliyeti bilinen satış"** (kârın kümesi). Uzun etiketler dikey sütunda kırılıyordu (ölçer K3 = 12 durum) ve 375'te kart taşıp son çubuk görünmüyordu (önceki turda da 4 çubukla taşıyordu) → `CubukGrafik` yeni **Yatay** kipi: kalem başına satır (ad tek satır · çubuk · tutar), ≤ 480 px'te ad üstte; çubuk genişliği SVG özniteliği (satır içi style yok), negatif sıfırın soluna. Not metni: "Kâr, «Maliyeti bilinen satış» çubuğundan hesaplanır … Toplam satış X; maliyeti eksik kısım kâra katılmadı." | RD4E07(basit ×2), RD4E07b, RD4E13, RG41 (güncellendi); canlı 76.500 / 51.000 / 36.000 / 15.000; görüntü `tur2/foto-kz/raporlar_kar_zarar__PRO_aydinlik_1280__sekme1.png`, `…BASIT_karanlik_375__sekme1.png` |
| T2-2 | [ORTA] D2 akordeon boşluğu RM Grafikler / Stok yaşı / Gün sonu'da 0 px | `#rp-duzeltme` `#raporlar-pro` sarmalayıcısının İÇİNDE değil, KARDEŞİ (Raporlar.razor `}` sonrası); ortak kural `#raporlar-pro .katlanir` onu tutmuyordu ve `katlanir.css margin:0` `.dikey-bosluk > * + *` aralığını eziyordu | Ortak kurala kardeş seçici: `#raporlar-pro ~ .katlanir, #raporlar-bugun ~ .katlanir { margin-top: var(--bosluk-4) }` (sekme sekme değil) | Piksel ölçümü (`tur2/bosluk-olcum.txt`, x=500/30): 1280/1440/375 × Grafikler/Stok yaşı/Gün sonu × açık/koyu: kart alt kenarı → akordeon üst kenarı **15–16 px** (1 px kenar yumuşatma dahil = 1rem); önce 0 px. RD4E09 (CSS kuralı) |
| T2-3 | [ORTA] KAR-HIZ < 800 ms yalnız sıcak önbellek / sakin makinede; değişken aralık senaryosu yok | (a) Ölçüldü: sıcak istekte **kart maliyeti okuması** (`KartMaliyetleriToplu`, 25.000 satır, `ORDER BY id`) eniyileyiciyi kiracı indeksiyle 50.000 satır + PK aramasına itiyordu: **344–745 ms / istek** (MySQL profili). (b) Değişken aralıkta aralık toplamı yeniden kurulur; baştaki kısmi ay her yeni aralıkta canlı okunur ve kiracı kilidi altında hesaplanır | (a) Migration `20261008140742_RaporDortKartMaliyetiIndeksi` → `IX_urunler_tenant_id_alis_maliyeti (tenant_id, alis_maliyeti)`; okuma sırası `(alis_maliyeti, id)` (indeksin kendi sırası, kapsayan aralık okuması, sayfa anahtarı çift) → **23–29 ms** (EXPLAIN `range … Using index`). (b) Yeni k6 senaryosu `betikler/yuk/k6-kar-degisken.js` (başlangıç 30–1095 gün, bitiş 0–20 gün önce, şube süzgeci %50, ofset 0/50/100 + her turda sabit 3 yıl). | § T2.3 tablo; RD_KH08 (sayfa anahtarı eşit maliyetlerde eksiksiz) |
| T2-4 | [DÜŞÜK] RM grafiği Satış çubuğu BRÜT, AYLIK SATIŞ kartı NET | `CubukNoktalari` `g.SatisTl` kullanıyordu | Sözleşme `AylikGun.NetSatisTl` (sunucu `gunler[].netSatisTl` zaten dönüyordu); çubuk net, lejant **"Satış (iade düşüldü)"** (sunucu alanı yoksa "Satış") | RD4E06; canlı gün 08.10: brüt 99.000 · net **76.500**; görüntü `tur2/foto/raporlar__PRO_aydinlik_1280__sekme1.png` (eksen 76.500 ₺) |
| T2-5 | [DÜŞÜK] Kart maliyeti kâra giriyor, stok değerlemesine girmiyor | `StokDegerlemesi` yalnız `stok_partileri.birim_maliyet_tl` okuyordu | Kâr motoruyla AYNI kural: partide TL maliyeti yoksa ve kart > 0 ise o adet kart maliyetiyle bilinir; yanıtta `kartMaliyetliAdet`, `kartMaliyetliTl`; has bacağı kartla HESAPLANMAZ (fail-closed); KZ Stok sekmesinde "kart maliyeti" rozeti + "N adet stoğun partisinde alış maliyeti yok; ürün kartındaki alış maliyeti kullanıldı (kâr hesabıyla aynı kural)." | RD03 (DB), RD4E08 (bUnit); canlı + SQL: maliyetli 10 (8 kart + 2 parti) · maliyetsiz 18 · **stok maliyeti 174.000** = 8×18.000 + 2×15.000; görüntü `tur2/foto/raporlar_kar_zarar__BASIT_aydinlik_1280__sekme4.png` |
| T2-6 | [DÜŞÜK] Servis yolu eksi alış maliyetini kabul ediyor | `UrunServisi.Olustur` servis içi kontrol yok (HTTP yolunda `[Range]` var) | **Yapılmadı — dosya alanı dışı** (UrunModulu bu pakette YALNIZ okuma; oluşturma yolu URUN-ACILIS-ATOMIK şeridinin, eşzamanlı). Kâr motoru ve yeni stok kuralı `> 0` süzgeciyle eksi kartı "bilinmiyor" sayar (RD02b, RD03 `KARTSIFIR`). → Açık kalan, sahibi ürün şeridi | § 13 madde 10 |
| T2-7 | [DÜŞÜK] Önbellek geçersizleştirmesi `olusturuldu` damgasına dayanıyor | Varsayım koda bağlanmamıştı | Mimari bekçi testi **RD_KH07**: kaynakta `Tur = "SATIS"/"SATIS_IADE"` yazan HER `new (Data.)Hareket` ilklendiricisi `Tarih` ve `Olusturuldu`'yu AYNI değişkenle verir ve o değişken `DateTime.UtcNow`'dur (bugün 5 yol; tarama kendini doğrular ≥ 5). Geriye tarihli bir yol eklenirse kırmızı. **Canlı gözlem (kural doğru çalışıyor):** yük tohumu 540.000 geçmiş tarihli satışı `olusturuldu = şimdi` ile tek seferde yazınca, 10 dk güvenlik payı boyunca her istek önbelleği tamamen attı (ilk k6 koşumu p95 26 sn — `tur2/k6/gecersiz/`, GEÇERSİZ sayıldı); pay dolunca isabet döndü. | RD_KH07, M4-26 |
| T2-8 | [DÜŞÜK] Raporlar.razor:1165 yorum ↔ kod çelişkisi; CubukGrafik DegerEkseni dalında satır içi style | — | Yorum doğrulandı ve yedek AYNI tanıma getirildi (`net satış − alış − hurda`, hurda yanıtta yoksa 0); değer ekseni kipinde `style="height:…"` → sınıf `.grafik-svg-sabit` (rapor-gorunum.css (24)). Eski dikey dalın `style`'ı paket öncesinden (dokunulmadı) | RD4E09, M4-28 |

### T2.1 Testler (tur 2)
Yeni: **RD03** (stok değerlemesi kart kuralı), **RD_KH07** (mimari bekçi, `RaporDortMimariTestleri`), **RD_KH08** (kart maliyeti (maliyet, id) sayfası — `KartSayfasiTest` akış-yerel sınırla 7'li sayfa, eşit maliyetler), **RD4E06** (RM net çubuk + lejant), **RD4E07** (Theory basit/pro: toplam + bilinen, Özet ile aynı etiket ve sayı), **RD4E07b** (maliyet tamken tek çubuk), **RD4E08** (KZ Stok rozet/not; yoksa çizilmez), **RD4E09** (satır içi style yok + ortak akordeon kuralı), **RD4E13** (yatay kip: tam ad, ölçek, negatif sola, sıfır çizgisi). = **9 yöntem / 10 durum**. Bilinçli güncellenen: `RaporGorunumTestleri.RG41` (1. öğe artık "Toplam satış" 10.000, 2. "Maliyeti bilinen satış" 6.000 — T2-1 kararı).
Filtreli koşumlar: `RaporDort|RaporGorunum|KarZarar|KarRapor|Maden|RaporlarEkran|RaporIki|RaporUc|Panel|RaporEkran|S34RaporBugun|Grafik|ParaDemo|Sozluk|RaporSeti|Yaslandirma|OlcekYuk|Toplu5|StokDeger` → **1.145 / 1.145** (yatay kipten önce); yatay kip sonrası `RaporDort|RaporGorunum|KarZarar|KarRapor|Maden|Grafik` **434 / 434**; indeks + sayfa anahtarı sonrası `RaporDort|OlcekYuk|KarZarar|KarRapor` **252 / 252**.

**Tam süit (tur 2, son kodla, BİR kez):** `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pRAPOR-4-tur2` → `_Ramazan-MacBook-Air_2026-10-08_17_26_32.trx`: **toplam 11.182 · geçti 11.173 · başarısız 7 · atlandı 2**.

| Kırmızı | trx iletisi | Sahibi | Bu paketin ürünü mü? |
|---|---|---|---|
| `TasarimBelirtecTestleri.Sema_beyaz_listesi_CSS_ile_AYNI_kodlari_tasir` | "tema.js 'yildiz' şemasını yazıyor ama belirtecler.css'te blok YOK" | yeni "yildiz" tema şeridi (`tema-yildiz.css` 17:30'da, süit koşarken değişti) | Hayır |
| `OlcekTekTestleri.OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` | `tema-yildiz.css … .tuval-tezgah … .ekran-karo padding` envanter dışı | aynı tema şeridi (denetim OLCEK-1 ✗ ile aynı) | Hayır |
| `S1fGorunumKartiEkranTestleri.S1f_E1…`, `S1f_E14…` | beklenen 8/18, gerçek 9/19 (tema seçeneği bir fazla) | aynı tema şeridi | Hayır |
| `B1GorunumEkranTestleri.BE2…`, `B1KullaniciGorunumHttpTestleri.B1H2…` | beklenen 9/8, gerçek 10/9 (şema listesi) | aynı tema şeridi | Hayır |
| `S23FisMusteriEkranTestleri.S23E_04_musteri_bosaltilinca_rozet_CIZILMEZ` | "Secici 'islem-cari': boş seçenek yok, '' seçilemez." | ortak `Secici` / form bileşenleri (GIRDI-AILESI-2, `Bilesenler/Form`) | Hayır — bu paket Secici'ye ve fiş ekranına dokunmadı |

Devir notundaki (tur 1 sonu) 2 kırmızı (`P54HttpTestleri.Miktar_SIFIR…`, `FaturaEkranTestleri.Y4C_K12…`) bu koşumda YEŞİL (sahipleri kapattı). **PARALEL ŞERİT KAPISI madde 7:** 7 kırmızının hiçbiri bu paketin dosyalarına dokunan test değil; bu paketin testleri (RaporDort*, RaporGorunum, KarZarar*, KarRapor*, Maden*, OlcekYuk) bu koşumda yeşil → **bu paketin yeni kırmızısı 0**.

### T2.2 Mutasyon (tur 2; `tur2/mut.sh`: yedek → tek eşleşmeli perl → build → filtreli test → geri kopya + touch → cmp)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M4-22 | Stok değerlemesinde kart dolgusu kapalı | RD03 | AYNI |
| M4-23 | KZ grafiğinde "Toplam satış" çubuğu hiç konmuyor | RD4E07(basit: True), RD4E07(basit: False), RG41 | AYNI |
| M4-24 | RM grafiği eski brüt `g.SatisTl` | RD4E06 | AYNI |
| M4-25 | Ortak akordeon kuralından `#raporlar-pro ~ .katlanir` çıkarıldı | RD4E09 | AYNI |
| M4-26 | `SatisModulu.P21.cs` SATIS_IADE ilklendiricisinde `Olusturuldu = gecmisAn` (yalnız kaynak taraması; derlenmedi, 1 dk içinde geri) | RD_KH07 | AYNI |
| M4-27 | Bilinen çubuğun etiketi eski kısa "Satış" | RD4E07 ×2, RG41 | AYNI |
| M4-28 | Değer ekseni SVG'sine satır içi `style` geri | RD4E09 | AYNI |
| M4-29 | KZ Stok kart maliyeti notu çizilmiyor | RD4E08 | AYNI |
| M4-30 | Yatay kipte negatif değer sola inmiyor | RD4E13 | AYNI |
| M4-31 | Kart sayfa anahtarında eşit maliyet dalı (`maliyet = son && id > son`) kaldırıldı | RD_KH08 | AYNI |
Kayıtlar: `ham/RAPOR-4-olcum/mutasyon/tur2/ozet-tur2.txt` + `M4-*.diff`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata**, 4 Uyarı — dördü de başka şeritlerin test dosyalarında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:593/824`); bu paketin dosyalarında uyarı yok.

### T2.3 KAR-HIZ ölçümü (tur 2) — 3 büyük kiracı (K001–K003: 50.000 ürün · 180.000 satış / 3 yıl + maliyet bacağı), 6 VU, 2 dk, Release yayın kopyası (Staging), API 5160
Yük DB'si yeniden kuruldu (migration → `yuk-kiraci-kur.mjs` 3 kiracı → `yuk-tohum.sql` → `yuk-tohum-ek.sql`; 1.462 MB) ve iş sonunda `sql_log_bin=0; DROP DATABASE` (schemata 0). Her koşumdan önce API yeniden başlatıldı (soğuk önbellek).

| Koşum | Kod | Makine yükü (k6 öncesi → sonrası) | Senaryo | n | p50 ms | **p95 ms** | p99 ms | hata/429 |
|---|---|---|---|---|---|---|---|---|
| gecersiz | tur-2 kodu, indeks ÖNCESİ | 16 → 16 | sabit 3 yıl | 36 | 17.246 | 26.114 | — | 0/0 — **GEÇERSİZ** (tohumun `olusturuldu` damgası 10 dk güvenlik payı içinde → her istek önbelleği attı; T2-7) |
| yuklu | tur-2 kodu, indeks ÖNCESİ | 21 → 21 | k6-agir-rapor (sabit 3 yıl) | 188 | 688 | 1.550 | 1.872 | 0/0 |
| yuklu | tur-2 kodu, indeks ÖNCESİ | 16 → 6 | k6-kar-degisken: değişken / sabit | 193 | 354 / 238 | 1.027 / 687 | 1.772 / 1.154 | 0/0 |
| sakin1 | tur-2 kodu, indeks ÖNCESİ | 3,5 → 5,2 | k6-kar-degisken: değişken / sabit | 192 | 364 / 229 | 1.022 / 690 | 2.037 / 1.313 | 0/0 |
| sakin1 | tur-2 kodu, indeks ÖNCESİ | 13 → 5,6 | k6-agir-rapor (sabit 3 yıl) | 210 | 290 | 1.346 | 1.606 | 0/0 |
| **idx** | **son kod (indeks + (maliyet,id) sırası)** | **18 → 12** | **k6-kar-degisken: değişken / sabit** | **185** | **497 / 229** | **1.259 / 722** | 1.768 / 937 | 0/0 |
| **idx** | **son kod** | **11 → 9** | **k6-agir-rapor (sabit 3 yıl)** | **216** | **168** | **664** | 1.192 | 0/0 |

Soğuk ilk istek (süreç açıldıktan sonra kiracının ilk 3 yıllık isteği): sakin 3,4–3,7 sn, yüklü 5,2–12,1 sn; ikinci istek 148–628 ms (`isinma-*.txt`).
**Sonuç (Anayasa md.1 — olduğu gibi):** spesifikasyonun ölçütü (sabit **3 yıl**, büyük kiracı) son kodla, makine yükü 9–18 iken **p95 664 ms (agir) / 722 ms (değişken senaryonun sabit isteği) < 800 ✓**. Hasımın istediği **değişken aralık/şube/sayfa** senaryosunda p95 **1.022–1.259 ms > 800 ✗** — yeni aralıkta aralık toplamı yeniden kurulur ve baştaki kısmi ay canlı okunur (kiracı kilidi altında). Bu senaryo spesifikasyonun kabul ölçütü değil, ama gerçek kullanımın kötü durumudur → açık kalan (§ 13 madde 11) + orkestratör kapısında sakin makinede `k6-kar-degisken.js` ile yeniden ölçüm. Kanıt: `tur2/k6/{gecersiz,yuklu,sakin1}/` ve `tur2/k6/idx-*`.

### T2.4 Canlı doğrulama (tur 2; API 5160 Release · `kuyumcu_net` · kiracı `PAKETRAPOR-4-TEST Kuyumculuk` `01M4DTDYDQBZ01PTMVWHR0JTG2`, yalnız ürün uçlarıyla `canli-tohum.mjs`)
| Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|
| `GET /raporlar/aylik` | 200; `gunler[].netSatisTl` = brüt − iade | 200; gün 2026-10-08 `satisTl 99.000 · netSatisTl 76.500 · alisTl 30.000`; ay: brüt 99.000 · iade 22.500 · net 76.500 · alış 30.000 · hurda 0 · fark 46.500 |
| `GET /raporlar/stok-degerlemesi` | 200; kart maliyetli 8 adet stoğa girer | 200; toplam 28 · maliyetli **10** · maliyetsiz 18 · `kartMaliyetliAdet 8` · `kartMaliyetliTl 144.000` · **stokMaliyetiTl 174.000** · kısmi true · has maliyetli 2 / maliyetsiz 26 (kart has'a girmez) |
| `GET /raporlar/kar-zarar` (bugün) | 200; grafik çubukları ile aynı sayılar | 200; hasılat 76.500 · bilinen 51.000 · maliyet 36.000 · kâr 15.000 · KISMI · kart 2 |
| `GET /raporlar/stok-degerlemesi?subeId=YOK-SUBE` | 404 Türkçe | 404 "Şube bulunamadı ya da pasif." |

**Bağımsız SQL** (`tur2/canli-sql-tur2.txt`): kalan partiler — Kartlı Bilezik `SAYIM_FAZLA 8 · NULL · kart 18000`, Kartsız Kolye `ALIS 2 · 15000` + `SAYIM_FAZLA 9 · NULL`, Gram Kolye `SAYIM_FAZLA 9 · NULL`; özet `toplam 28 · maliyetli 10 · kartlı 8 · maliyet 174.000` ✓; gün `brüt 99.000 · iade −22.500 · net 76.500` ✓.

### T2.5 Gerçek tarayıcı (başsız Chrome `tur.mjs` + `analiz.py`, vekil 5161, CDP 9701)
Tam tur: `/raporlar`, `/raporlar/kar-zarar`, `/` × 1440 · 1280 · 375 × BASİT/PRO × açık/koyu = 36 sayfa + 78 sekme; yatay kipten sonra `/raporlar/kar-zarar` yeniden (12 sayfa + 48 sekme). Birleşik tablo (her satır rota × sekme × genişlik × mod × tema; KZ satırları son turdan): `tur2/tablo.md` — **114 ölçüm: K1 gizli-görünen 0 · K3 taşan/kırık 0/0 · K5 'TRY' 0 · K9 binen 0 · K12 0 · K13 0 · yatay kaydırma 0**; hata 0, geçersiz 0. Kalan tek sınıf K4 (sayfa boyu, 375 ve Gün sonu PRO 1280 — önceki turla aynı; yeni: KZ Grafikler PRO 375 2,1×5, yatay kipte her kalem iki satır). İlk tam turda K3 "kırık" 12 durum (dikey sütunda "Toplam satış" / "Maliyeti bilinen satış") → yatay kip → 0.
Görüntüler Read ile bakıldı: `foto-kz/raporlar_kar_zarar__PRO_aydinlik_1280__sekme1.png` (Toplam satış 76.500 · Maliyeti bilinen satış 51.000 · Maliyet 36.000 · Kâr 15.000 · Dönem kârı 15.000, tek satır adlar), `foto-kz/raporlar_kar_zarar__BASIT_karanlik_375__sekme1.png` (beş kalem, kart taşmıyor), `foto/raporlar__PRO_aydinlik_1280__sekme1.png` (net 76.500 ₺ ekseni, "Satış (iade düşüldü)", akordeon 16 px aralıklı), `foto/raporlar__PRO_karanlik_375__sekme1.png`, `foto/raporlar_kar_zarar__BASIT_aydinlik_1280__sekme4.png` (STOK 28 · BİLİNEN STOK MALİYETİ 174.000 · "kart maliyeti" rozeti + not), 375 akordeon kırpıntısı.

### T2.6 Denetim, temizlik, değişen dosyalar
`bash dotnet/denetim.sh` (son kod): **313 ✓, 2 ✗** — ✗ P-30 DEMO_INSAN_SURESI (kabul istisnası) · ✗ OLCEK-1 YUZ_OLCU_ENVANTER_DISI 2 = `tema-yildiz.css` `.tuval-tezgah … .ekran-karo padding-left/right 8px` (başka şeridin dosyası; bu paket tema-yildiz.css'e dokunmadı). Ara koşumdaki ✗ GIRDI-2 DTO_DECIMAL_RANGESIZ 21/20 (Satinalma/Siparis/Pano/Cari… DTO'ları, GIRDI/GOC şeritleri) son koşumda kendi sahiplerince kapanmış. Bu paketin sayaçları ✓ (Sınırsız rapor dökümü 18/18, ToListAsync 140/140, TB-19 24/24, beyaz liste 14/14, satır içi style 53/196, Migrations 0 baytlık 0, BuildTargetModel'siz Designer 0). Çıktı: `tur2/denetim-son.log`.
Temizlik: API 5160 ve vekil 5161 durduruldu (`lsof` boş), başsız Chrome (9701) yok, k6 yok. Test kiracısı `01M4DTDYDQBZ01PTMVWHR0JTG2` `zsh kiraci-temizle.sh … --uygula` ile silindi; 75 tenant_id tablosu tarandı → **0 satır**. `tenants`: tur başında 18 → kiracım eklendi (19) → silindi; tur sonunda **17** — aradaki −1 başka şeridin kendi kiracısını silmesi (bu paket QA-TEST ve başka kiracılara dokunmadı). `kuyumcu_test_yuk` DROP (schemata 0). Sır dosyaları (`yuk-sifre`, `canli-sifre`, `sistem.anahtar`, `fikstur.json`, `kiracilar.json`) `printf '' >` ile boşaltıldı.
Değişen dosyalar (tur 2): `Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` (StokDegerlemesi kart kuralı; KartMaliyetleriToplu (maliyet, id) sırası, `public`, `KartSayfasiTest`), `Kuyumcu.Api/Data/KuyumcuDbContext.cs` (+ `IX_urunler_tenant_id_alis_maliyeti`), **YENİ** `Kuyumcu.Api/Data/Migrations/20261008140742_RaporDortKartMaliyetiIndeksi.cs` (788 B) + `.Designer.cs` (259.702 B) + `KuyumcuDbContextModelSnapshot.cs`, `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` (`AylikGun.NetSatisTl`), `Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` (`StokVeri.KartMaliyetliAdet`), `Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (+3 anahtar TR+EN, GrafikHasilatNotu), `Kuyumcu.Web/Pages/Raporlar.razor`, `Kuyumcu.Web/Pages/KarZarar.razor`, `Kuyumcu.Web/Bilesenler/CubukGrafik.razor` (Yatay kip, değer ekseni sınıfı), `Kuyumcu.Web/wwwroot/css/rapor-gorunum.css` ((23)–(26)), testler `RaporDortTestleri.cs`, `RaporDortEkranTestleri.cs`, `RaporGorunumTestleri.cs`; **YENİ** `dokumanlar/09-uygulama-kayitlari/betikler/yuk/k6-kar-degisken.js`. Migration `kuyumcu_net`'e uygulandı: `__EFMigrationsHistory` son satır `20261008140742_RaporDortKartMaliyetiIndeksi`, `information_schema.statistics` → `IX_urunler_tenant_id_alis_maliyeti (tenant_id, alis_maliyeti)`.
**Dokunulan sayfalar (TEK DOKUNUŞ, ikinci dokunuş gerekçesi):** `Raporlar.razor`, `KarZarar.razor`, `CubukGrafik.razor` — aynı paketin 2. turu, hasım bulgularının kapanışı (yeni dalga değil).

---

## 0. İzolasyon ve sapmalar (Anayasa md.1)

| Konu | Beklenen | Gerçek | Gerekçe |
|---|---|---|---|
| Canlı API / vekil portu | 5160 / 5161 | **5162 / 5163** (yük API'si 5162, son ölçümler 5168) | 5160 ve 5161'i iş başında **başka bir şeridin artığı** tutuyordu: `Kuyumcu.Api --urls http://localhost:5160` PID 43565 (06:56'dan beri) ve `inceleme-vekili.py /private/tmp/kuyumcu-serit/GRID-1-hakem/fik-a.json 5160 5161` PID 44792. Başkasının sürecini durdurmamak için (kural: başka portlara/süreçlere dokunma) boştaki 5162/5163 kullanıldı. 5199/5200/5166/5167/5230-5233'e dokunulmadı. CDP 9701–9704 (izinli aralık). |
| Canlı API türü | `dotnet run --no-build` (Development) | Release yayın kopyası (`dotnet publish -c Release`), **Staging** ortamı, `kuyumcu_net` | Mutasyon koşumu Debug `bin/`'i sürekli yeniden derliyordu; Debug ikilisinden koşan API mutasyonlu kodu yükleyebilirdi. Yayın kopyası `obj/Release`'ten bağımsız. İstek yolu Production ile aynıdır (Program.cs yalnız `IsDevelopment` dallanır; OLCEK-YUK ile aynı yöntem). Sırlar `~/.kuyumcu` ve değişkenlerden, basılmadı. |
| Yük DB | `kuyumcu_test_yuk` (4 GB'a kadar) | `kuyumcu_test_yuk` migration ile sıfırdan kuruldu, 1.428 MB (+ maliyet bacağı) | OLCEK-YUK'un DB'si 07.10'da DROP edilmişti. 3 büyük kiracı (K001–K003) OLCEK-YUK betikleriyle (`yuk-kiraci-kur.mjs` + `yuk-tohum.sql`) kuruldu. İş sonunda DROP (§ 12). |
| Eşzamanlı şeritler | GIRDI-AILESI-2 (t9), URUN-ACILIS-ATOMIK (t5) | Ölçüm süresince makine yük ortalaması **9–54** (10 çekirdek) | Başka şeritlerin tam süit / derleme koşumları. Yük ölçümü bu yüzden A/B (aynı koşulda eski ve yeni ikili) yapıldı (§ 5). |

---

## 1. Bulgular → yapılanlar

### VERİ 1 — "Ürüne girilen alış maliyeti kâr hesabında hiç kullanılmıyor" (KÖK NEDEN + karar)

**Kök neden (canlı + SQL ile kanıtlandı, § 7):** Ürün kartı oluşturulurken `alisMaliyeti` yalnız `urunler.alis_maliyeti` kolonuna yazılıyor. Açılış stoğu `UrunServisi.StokSayimGuncelle` üzerinden **SAYIM_FAZLA** partisi olarak açılıyor ve `MaliyetMotoru.PartiAc(..., birimMaliyetTl: null)` çağrılıyor ("Fazla çıkan malın maliyeti BİLİNMEZ"). Satış bu partiden tüketince `stok_parti_tuketimleri.birim_maliyet_tl = NULL` olur; kâr motoru (KarZararServisi) maliyeti YALNIZ parti satırından okuduğu için satış "maliyet eksik" sayılır. Canlı SQL: kartlı ürünün partisi `SAYIM_FAZLA · birim_maliyet_tl NULL · 10 adet`, 2 tüketim satırının 2'si maliyetsiz, kartta `alis_maliyeti = 18000.00`.

**Ekip oyu (Bertan + Mustafa, fail-closed ilkesiyle):**
- Seçenek A (yazma anında: açılış partisine kart maliyetini yaz) muhasebece en doğrusu ama **oluşturma yolu URUN-ACILIS-ATOMIK şeridinin** (t5, eşzamanlı) — dosya alanı dışında; dokunulmadı → kurucuya/orkestratöre not (§ 13).
- Seçenek B (seçilen, okuma anında): **parti satırında maliyet YOKSA ve ürün kartındaki alış maliyeti > 0 ise** o satır kart maliyetiyle bilinir sayılır; kaynak AYRI sayılır (`kartMaliyetliAdet`) ve ekranda **"kart maliyeti" rozeti + açıklama** ile söylenir (UX-S21). Fail-closed korunur: kart boş ya da 0 → eskisi gibi "hesaplanamadı"; **has bacağı kartla HESAPLANMAZ** (alış günü altın fiyatı bilinmiyor → `karHas = null`, has durumu HESAPLANAMADI); satırsız (parti defteri geride kalmış) adet kartla kapatılmaz. Parti maliyeti varsa HER ZAMAN parti önceliklidir.
- Bilinen yan etki (karara yazıldı): kart maliyeti sonradan düzenlenirse geçmiş dönemin "kart maliyetli" satırlarının kârı da değişir (kart bir beyandır, parti kaydı değildir). Rozet bunu görünür kılar; kalıcı çözüm Seçenek A.

**Kod:** `KarZararServisi.Olc` (dönem kârı — Kâr/Zarar, Rapor merkezi, Panel aynı sayıyı okur) ve ürün bazlı hesap (`UrunSatirlari` + ay kovaları) aynı kuralı kullanır; tek yardımcı `KartMaliyetleri` (kimlik kümesiyle, `Take` sınırlı). `UrunModulu.cs`'e dokunulmadı.

### VERİ 2 — "Alış-satış nakit farkı" aynı ekrandaki sayılarla tutmuyordu
`RaporModulu.AylikOzet`: `satisAlisFarkiTl = satisTl − alisTl` (BRÜT satış) → **`netSatış − alış − hurda alışı`** (Panel "Günün nakit akışı (satış − alış − hurda)" ile TEK tanım). Bileşen `hurdaAlisTl` yanıta eklendi (sözleşme `AylikYanit.HurdaAlisTl`). Kartın alt satırı formülü yazar ("net satış − alış − hurda"), ⓘ açıklaması yeni tanımı söyler (`Sozlukler.KarRapor.cs` NakitFarkiAlt/NakitFarkiAciklama). Canlı: 99.000 brüt − 22.500 iade − 30.000 alış − 0 hurda = **46.500** (eski tanım 69.000 derdi); ekran AYLIK SATIŞ 76.500 − AYLIK ALIŞ 30.000 = 46.500 ✓ (görüntü § 8).

### VERİ 3 — Panel ile Kâr-zarar aynı gün altın satışı farklı has
İki AYRI büyüklük (Panel: satılan malın defter has gramı; Kâr-zarar: satış TUTARININ satış günü altın fiyatıyla karşılığı — kârın ölçüldüğü bacak). Tek tanıma indirmek kâr hesabını bozardı (RAPOR-2 kalıbı: kapsam etiketi). Yapılan: (a) has bacağının ilk kartı her iki modda **"Satış tutarının altın karşılığı"** adını taşır; (b) BASİT'te altın kârı hesaplanamıyorken üç kart (96,69 has + "Hesaplanamadı" + "Hesaplanamadı") yerine **tek cümle** "Altın kârı hesaplanamadı: satılan malın alış maliyeti altın olarak bilinmiyor." Canlı: `hasilatHas 11,2052` (TL karşılığı) · `hasilatHasDefterGram 27,9880` (Panel ile aynı net defter gramı) · `gunluk.satis.toplamHas 35,316` (brüt).

### KAR-HIZ — `/raporlar/kar-zarar/urun` büyük kiracı 3 yıl p95
Ayrıntı § 4–5. Özet: dönemin tüm satırlarını her istekte belleğe almak yerine **kapanmış tam ayların ürün kovaları** (kart maliyetinden bağımsız) kiracı başına bellekte, doğrulamalı önbellekte tutulur; içinde bulunulan ay her istekte canlı; sayfadaki ürünlerin ayrıntısı eski bellek hesabıyla (değiştirilmeden taşınan `UrunSatirlari`) üretilir. Geçersizleştirme için yeni indeks `IX_hareketler_tenant_id_olusturuldu` (migration).

### Tasarım bulguları (RAPOR-3 tasarım hakemi)

| # | Bulgu | Yapılan |
|---|---|---|
| Y1 | [YÜKSEK] Nakit farkı tutmuyor | VERİ 2 |
| Y2 | [YÜKSEK] RM "Günlük Satış / Alış" ekseni ezik, değer ekseni/lejant yok | `CubukGrafik` yeni kip (`DegerEkseni`): solda HTML değer ekseni (en yüksek · orta · 0, gerçek yazı), altta seyrek HTML tarih ekseni (en çok 6 yazı + son; her yazı kendi çubuk sütununun ortasında, ilk sola/son sağa yaslı — AlanGrafik kuralı), Satış/Alış lejantı. SVG içinde `<text>` yok. Parametre verilmeyen kullanımların çıktısı aynı. |
| O1 | [ORTA] KZ BASIT altın kârı 96,69 vs Panel 163,79 | VERİ 3 |
| D1 | Stok yaşı başlığında "dönemden bağımsız" notu | `#sy-donem-bagimsiz` aynı id ile "Nasıl hesaplanır? ⓘ" içine taşındı. |
| D2 | Akordeonlar son kart sırasına yapışık | Ortak kural (sekme sekme değil): rapor ekranlarında (`#raporlar-pro`, `#raporlar-bugun`, `.kz-sayfa`) `.katlanir:not(:first-child) { margin-top: var(--bosluk-4) }`. **Tur 1'de ✓ denmesi YANLIŞTI** (hasım ölçtü: RM Grafikler/Stok yaşı/Gün sonu'da 0 px — `#rp-duzeltme` sarmalayıcının kardeşi). **Tur 2'de kapatıldı:** kardeş seçici `#raporlar-pro ~ .katlanir`; ölçülen boşluk 15–16 px (§ T2 T2-2). |
| D3 | RM PRO Gün sonu: "Fark var" belirsiz, satış toplamı kapsamsız, tahsilat sarkan "·" | Durum kartı faturasız satış varsa "N satışın faturası yok" (BASİT ile aynı metin, `DegisimGunSonuSozlugu.FaturasizAdet`); SATIŞ TOPLAMI alt satırı "(iade düşülmeden)"; tahsilat dökümü her kalem ayrı satır (`#mut-kartlar .stat-alt { white-space: pre-line }`). |
| D4 | KZ "Karşılaştır" açılışta tam kutu | "+ Karşılaştır" düğmesi (`#kz-karsilastir-ac`), tıklanınca ortak Secici açılır (id `kz-karsilastir` AYNI); seçim yapılmışsa açık kalır. Açmak istek atmaz, seçim atar. |
| D5 | RM BASIT "Kasada ne var?" Panel'den farklı düzen | Açıklama satırı ("TL kasalar · Şu anki para") tek para biriminde çizilmez (birden çok para biriminde ayırt edici olduğu için kalır — S34 testi); "Nakit X · Kart/Havale Y" iki AYRI öğe (Panel'deki `.panel-soru-alt > *` nowrap kuralı) — tutar satır sonunda yalnız düşmez. Birleşik cümle `aria-label`'da. |
| D6 | Panel BASIT "Yasal liste yüklenmedi" hem bant hem satır | BASİT'te üst bant yalnız KRİTİK (açık yasal liste eşleşmesi) varken çizilir; liste eski/yok ve saklama işi "Bekleyen iş" satırında (RAPOR-3'ün `Gorunmez` parametresi). PRO değişmedi. |
| D7 | KZ grafik tek "Hasılat" çubuğu etiketin solunda | `CubukGrafik`: lejantta 1. seri adı verilmiş, 2. seri yok ve tüm 2. değerler 0 ise çubuk sütun ORTASINDA (tek çubuk). Karşılaştırmada iki çubuk aynen. |
| K1 | KAPSAM DIŞI: Panel PRO tasarım artıkları | Dokunulmadı (ayrı sadeleştirme paketi). |
| K2 | KAPSAM DIŞI: canlı tema geçişinde ikincil düğmeler | Dokunulmadı. |
| K3 | KAPSAM DIŞI → VERİ 1 | Yapıldı (VERİ 1). |

---

## 2. Spesifikasyonun koda karşı doğrulanması (sapmalar)
- "RaporModulu.cs:402 `satisAlisFarkiTl = satisTl - alisTl`" — doğrulandı (satır 402).
- "Panel'deki düzeltme (metin preserveAspectRatio dışında)" — Panel `AlanGrafik` kullanıyor, RM `CubukGrafik`; aynı kural CubukGrafik'e yeni kip olarak taşındı.
- "KarZararServisi maliyeti stok_partileri.birim_maliyet_tl'den mi alıyor?" — HAYIR: `stok_parti_tuketimleri.birim_maliyet_tl`'den (partiden kopyalanan). Kök neden VERİ 1'de.
- KAR-HIZ önerisi "SQL'de ön-toplama (ürün bazında GROUP BY)" **denendi ve reddedildi (ölçüldü)**: hareket başına parti toplamı → ürün toplamı iki katlı SQL toplaması 3 yıllık büyük kiracıda **2.0 sn** (elle yazılmış en iyi sorgu, FORCE INDEX ile), EF çevirisi 2,4 sn × 2 sorgu = 4,6–5,1 sn (eski yoldan YAVAŞ). MySQL eniyileyici kapsayan indeksi büyük aralıkta seçmiyor (`IX_hareketler_tenant_id_cari_id_tarih` ile 180.000 satır arama). Bu yüzden "günlük ön-toplam" seçeneğinin ay düzeyinde, bellekte ve doğrulamalı hâli seçildi (§ 4).
- Yeni tablo yerine bellek önbelleği: tablo yazımı her satır için denetim izine (DenetimYazmaDinleyicisi) düşerdi ve denetim dışlama listesi (`MakineUretimiDislananTurler`) başka dosyanın "insan kararı" listesidir; 165.000 kova satırı kadar denetim izi üretmemek için kalıcı tablo kurulmadı.
- Sözleşme dosyaları (`Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs`, `RaporlarSozlesmeleri.cs`) dosya alanında sayılmamıştı; yeni alanlar (`KartMaliyetliAdet`, `HurdaAlisTl`) istemci kaydına girmeden ekran gösteremezdi → yalnız sona varsayılanlı alan EKLENDİ (geriye uyumlu).

---

## 3. Migration
`20261008090525_RaporDortOlusturulduIndeksi` — `Up`: `CreateIndex IX_hareketler_tenant_id_olusturuldu (tenant_id, olusturuldu)`; `Down`: `DropIndex`. Dosyalar 0 bayttan büyük (793 B / 259.422 B), Up gövdesi okundu (yalnız bu indeks). Prosedür: `dotnet build Kuyumcu.sln` YEŞİL → `dotnet ef migrations add` (build'li) → `dotnet ef database update --connection …kuyumcu_net…` (parola değişkenden). İlk `database update` "No migrations were applied" dedi (eşzamanlı şerit derlemesiyle yarış — ikili migration'sız okundu); ikinci koşum `Applying migration '20261008090525_RaporDortOlusturulduIndeksi'. Done.` Kanıt: `__EFMigrationsHistory` son satır `20261008090525_RaporDortOlusturulduIndeksi 8.0.20`, `information_schema.statistics`'te indeks var. Yük DB'sine de uygulandı.
DbContext: `mb.Entity<Hareket>().HasIndex(h => new { h.TenantId, h.Olusturuldu })` (yeni varlık yok → IKiraciVarlik/HasQueryFilter değişmedi).

---

## 4. KAR-HIZ tasarımı (`Modules/Maliyet/KarUrunOnToplam.cs`, YENİ)

**Kova** (şube × ürün × dönem parçası, kart maliyetinden BAĞIMSIZ): `MaliyetiBilinenler` kuralının birebir sınıflaması — **K**: en az bir parti satırı, hepsi maliyetli, adet karşılanmış (tolerans 0,0005); **N**: karşılanmış ama boş maliyetli satır var (kart > 0 ise bilinir, maliyet = bilinen satırlar + kart × boş satır adedi); **U**: satır yok ya da adet karşılanmamış (her zaman eksik). Ayrıca mutlak bilinen/boş adet (güvenilirlik), kursuz satış (kur kaydı yok ya da fiyat ≤ 0), hareket sayısı. Kâr = R2(bilinen hasılat) − R2(maliyet), bilinen yoksa null (KarDurumuBelirle ile aynı).

**İstek akışı (`UrunBazli`):** (1) `DonemKovalari`: kapanmış TAM aylar önbellekten + kapanmış kısmi parçalar (aralık başı; bitiş geçmişteyse son kısım) ve eşdeğerlik dışı aylar **aralık toplamı önbelleğinde**; içinde bulunulan aya düşen parça her istekte canlı. (2) Kart maliyeti istek anında; ürünler eski sırayla (kârı olan önce, kâr azalan, eşitlikte ürün kimliği ordinal) sıralanır, sayfa kesilir. (3) Sayfadaki ürünlerin satırları eski pencere/şube süzgeçleriyle okunur ve **eski bellek hesabı** (`UrunSatirlari`, değiştirilmeden taşındı) çalışır → ekrandaki her sayı eski koddan çıkar.

**Eşdeğerlik koşulları (fail-closed):** Eski hesap parti satırlarını İSTEK PENCERESİNDEN (+ şube süzgecinden) süzer; ay kovası ise satırları hareket kimliğiyle bağlar. Bu yüzden her ay özeti satırlarının en erken/en geç tarihini, şube uyumsuzluğunu ve ürün uyumsuzluğunu taşır; pencere dışına taşan, şube süzgeçli istekte şubesi uyumsuz ya da ürünü uyumsuz ay **canlı** hesaplanır (RD-KH05).

**Geçersizleştirme:** defter (`HareketFabrikasi`) ve parti satırları yalnız eklenir. Kapanmış dönemi değiştirebilen tek olaylar: tarihi kapanmış döneme düşen yeni satış/iade ve storno. Her istek, son denetimden bu yana **oluşturulan** bu tür satırları `(tenant_id, olusturuldu)` indeksiyle arar (en çok 501 satır; aşarsa kiracının önbelleği tamamen atılır), etkilenen ayları atar ve aralık toplamlarının sürümünü artırır. Güvenlik payı 10 dk (uzun işlemlerin geç commit'i). Kur kaydı harekete bağlıysa silinmez (`KurModulu.EskiKurlariTemizle`), fiyatı değişmez → kursuz sayısı kararlı. Kart maliyeti değişimi önbelleği etkilemez (istek anında uygulanır).
**Bilinen sınır (rapor notu):** ham SQL ile defter/parti satırı SİLMEK veya DEĞİŞTİRMEK (bakım, test) önbelleği görmez → süreç yeniden başlatılır ya da `KarUrunOnToplam.Temizle()` çağrılır. Uygulama yolu bunu yapmaz (append-only).
**Bellek:** kiracı başına ay → (şube, ürün) kovası; toplam tavan 600.000 kova (≈ 100 MB), aşılınca en eski kullanılan kiracı atılır; kiracı başına en çok 8 aralık toplamı. Soğuk başlangıç (süreç açıldıktan sonra kiracının ilk isteği) 3 yıl büyük kiracıda **3,5–4,1 sn** (sakin) / 4,2–21 sn (yük altında) — 36 ayın kovası bir kez kurulur. Sonraki istekler § 5. **Okuma:** kapanmış kısmın toplamı aynı kiracının istekleri arasında paylaşılır ve DEĞİŞTİRİLMEZ; canlı parça ayrı sözlükte toplanır ve ikisi `DonemKovaGorunumu` ile okuma anında birleşir (50.000 ürünlük sözlük her istekte kopyalanmaz); sıralama yalnız ilk (ofset + limit) ürünü öbekte tutar (aynı karşılaştırıcı → tam sıralamayla aynı sayfa); kart maliyetleri tek sorguda.

---

## 5. KAR-HIZ ölçümü (OLCEK-YUK betikleri: `k6-agir-rapor.js`, 3 büyük kiracı × 2 personel = 6 VU, 2 dk; 3 yıl, limit 50)
Veri: K001–K003 her biri 50.000 ürün · 180.000 satış hareketi / 1.095 gün (OLCEK-YUK tohumu) **+ RAPOR-4 maliyet bacağı** (`yuk-tohum-ek.sql`: her satışa bir parti satırı, 1/3'ü maliyetsiz; ürünlerin yarısında kart maliyeti 3.500) — eski ölçümde parti satırı hiç yoktu, yani bu ölçüm daha ağırdır.

**Yöntem:** her koşum öncesi API yeniden başlatılır (Release yayın kopyası, Staging, yük DB), 3 kiracıya birer ısınma isteği (soğuk önbellek kurulumu ayrıca kaydedilir), sonra k6 2 dk. Makine yük ortalaması her koşumun önünde/sonunda `uptime` ile kaydedildi (`k6/yuk-ab*.txt`). **Kabul koşumu ab6/ab7** makinenin sakin olduğu pencerede (yük 3,6–8) peş peşe aynı veride eski ve yeni ikiliyle koşuldu.

| Koşum | İkili | Makine yükü (önce → sonra) | n | p50 ms | **p95 ms** | p99 ms | max ms | hata | 429 | vade takibi p95 (çevre göstergesi) |
|---|---|---|---|---|---|---|---|---|---|---|
| once-a | ESKİ (yalnız OLCEK-YUK tohumu, parti satırı yok) | yüksek (ölçülmedi) | 150 | 1.866 | 2.052 | 2.718 | 2.723 | 0 | 0 | 56 |
| once-b | ESKİ (+ maliyet bacağı) | yüksek | 122 | 2.785 | 4.381 | 12.354 | 12.650 | 0 | 0 | 85 |
| ab1 | ESKİ | 11–30 | 60 | 9.067 | 13.393 | 13.472 | 13.481 | 0 | 0 | 51 |
| ab1 | YENİ (ilk sürüm) | 22–9 | 190 | 757 | 1.098 | 2.001 | 2.069 | 0 | 0 | 50 |
| ab3 | ESKİ | 32–40 | 42 | 12.428 | 24.054 | 26.000 | 26.071 | 0 | 0 | 509 |
| ab3 | YENİ (kısmi baş önbellekte) | 28–18 | 210 | 298 | 998 | 1.155 | 1.163 | 0 | 0 | 155 |
| ab4/ab5 | YENİ (+ ilk-k sıralama, tek kart sorgusu) | 27–19 / 42–20 | 182/207 | 691/264 | 1.706/1.019 | 1.968/1.402 | 2.112/1.492 | 0 | 0 | 285/131 |
| **ab6** | **YENİ (son kod: + kopyasız birleşik görünüm)** | **7,7 → 4,9** | **216** | **319** | **514** | **535** | **572** | **0** | **0** | **50** |
| **ab7** | **ESKİ (aynı veri, hemen ardından)** | **3,6 → 13,5** | **80** | **6.233** | **7.591** | **7.626** | **7.638** | **0** | **0** | **47** |

**Sonuç:** son kodla, sakin pencerede **p95 514 ms < 800 ms ✓** (aynı koşulda eski yol p95 7.591 ms — 14,8 kat). Çevre göstergesi (vade takibi p95 50 ms) OLCEK-YUK'un sakin ölçümüyle (41–45 ms) aynı düzeyde. Makine 20–54 yük ortalamasındayken (başka şeritlerin tam süit/derleme koşumları) yeni yolun p95'i 998–1.706 ms ölçüldü; aynı anda vade takibi gibi ucuz uçlar da 3–10 kat yavaşladı (155–509 ms) — bu koşumlar kabul ölçümü SAYILMADI ama tabloda gizlenmedi.
**Soğuk başlangıç** (süreç açıldıktan sonra kiracının ilk 3 yıllık isteği, 36 ay kovası kurulur): sakin pencerede 3,5–4,1 sn, yük altında 4,2–21,1 sn (ab5) — eski yolun her isteği kadar; sonrası önbellekten. Kanıt: `ham/RAPOR-4-olcum/k6/` (`ab*-*.json`, `isinma-*.txt`, `yuk-ab*.txt`).

---

## 6. Testler

### Yeni testler
- `Kuyumcu.Tests/RaporDortTestleri.cs` (DB, 10): **RD01** aylık nakit farkı = net satış − alış − hurda (iade varken); **RD02** kart maliyeti parti maliyeti yokken kâra girer (dönem + ürün; `kartMaliyetliAdet`; `karHas` null; kök neden kanıtı: açılış partisi tüketim satırları maliyetsiz); **RD02b**(null, 0) kart boş/0 → HESAPLANAMADI; **RD_KH01** rastgele 260 satış (14 ay; K/N/U, kursuz, gümüş, 2. şube, pencere dışı satır, storno) × 4 aralık × 3 şube × 2 sayfa = 24 karşılaştırma, ilk tur önbelleği kurar, ikinci tur isabet eder — ESKİ yolla alan alan AYNI; **RD_KH02** önbellek isabetinden sonra kapanmış aya yeni satış, kapanmış aydan storno, kart değişimi, kısmi baş parçasına ve bu aya yeni satış — her adımda eski yolla AYNI; **RD_KH03** kart maliyeti yokken paket ÖNCESİ yolla (kartMaliyeti=false) BİREBİR; **RD_KH04** (saf) kova sınıfları + 0,0005 toleransı + kart kuralı; **RD_KH05** pencere dışı ve şube uyumsuz parti satırı olan ay canlı hesaplanır; **RD_KH06** aralık toplamı isabetinden sonra kısmi baş parçasına düşen satır görülür.
- `Kuyumcu.Tests/RaporDortEkranTestleri.cs` (bUnit, 5): **RD4E01** RM grafiği değer ekseni (31.000 ₺ / 0 ₺) + Satış/Alış lejantı + 31 hücre, ≤7 yazı, ilk/son gün, SVG'de `<text>` yok; **RD4E02** nakit farkı kartı sunucu değerini ve formülü basar; **RD4E03** Gün sonu PRO "8 satışın faturası yok" + "(iade düşülmeden)"; **RD4E04** stok yaşı notu ⓘ içinde; **RD4E05** tek seri çubuk sütun ortasında, iki seri aynen.
- `KarZararEkranTestleri.cs` +3: **RD4E10** "+ Karşılaştır" açılışta katlı, tıklayınca Secici açılır, istek gitmez; **RD4E11** kart maliyeti rozeti + notu; **RD4E12** kart yoksa not yok.
- Eski yol (eşdeğerlik başvurusu): `EskiKarYolu.EskiUrunBazli` — paket öncesi `UrunBazli` gövdesi AYNEN (yalnız bağlam parametresi, özel yardımcıların açık adları ve RD02 kararının satır düzeyi kart dolgusu; `kartMaliyeti=false` iken birebir eski).

### Bilinçli olarak güncellenen mevcut testler (davranış değişikliğinin gerekçesiyle)
| Test | Eski beklenti | Yeni | Gerekçe |
|---|---|---|---|
| `ParaDemoRaporEkranTestleri.PDE1` | tahsilat kalemleri " · " ile | "\n" ile (ayrı satır) | D3 |
| `ParaDemo2EkranTestleri.PD2E_4_BASIT…` | `#bugun-kasa-ayrim` metni tek cümle | cümle `aria-label`'da, iki öğe ayrı | D5 |
| `MadenIkiTestleri.MI11` | has ilk kart etiketi "Hasılat" | "Satış tutarının altın karşılığı" | VERİ 3 |
| `KarRaporTestleri.KR62(basit:true)` | BASİT'te has maliyeti kartı "Hesaplanamadı" | BASİT'te tek cümle `#kz-altin-kari-yok` (PRO aynen) | VERİ 3 |
| `KarZararEkranTestleri.KarsilastirSec` (yardımcı) | doğrudan `#kz-karsilastir` | önce `#kz-karsilastir-ac` | D4 |
| `KarZararEkranTestleri.Sayfa_EnAzBirVeriGrafigiCizer` | 8 çubuk (4×2, boş 2. seri) | 4 çubuk (tek seri) | D7 |

### Koşumlar
| Koşum | Toplam | Başarılı | Başarısız | Atlanan | Not |
|---|---|---|---|---|---|
| Önce (OLCEK-YUK t2 tam süit, 07.10) | 10.354 | 10.350 | 2 | 2 | başvuru |
| `RaporDortTestleri` (son) | 10 | 10 | 0 | 0 | — |
| `RaporDort` + `RD4E` (bUnit dahil) | 18 | 18 | 0 | 0 | 16 + RD_KH05/06 sonradan eklendi |
| İlgili sınıflar (KarZarar, KarRapor, OlcekYuk, RaporGorunum, RaporlarEkran, RaporIki, RaporUc, Maden*, Panel*, RaporEkran, S34RaporBugun, Grafik, ParaDemo, Sozluk, RaporSeti, Yaslandirma, RaporDort) | 1.069 | 1.069 | 0 | 0 | 13:0x |
| Son kodla ilgili sınıflar (RaporDort, OlcekYuk, KarRapor, KarZarar, Maden) | 428 | 428 | 0 | 0 | ilk-k + birleşik görünüm sonrası |
| `Toplu5` | 53 | 53 | 0 | 0 | T5 testinin güncellenmesinden sonra |

**Tam süit (son kodla)** — `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pRAPOR-4-son` → `_Ramazan-MacBook-Air_2026-10-08_14_53_16.trx`: **toplam 11.122 · geçti 11.118 · başarısız 2 · atlandı 2**.

| Kırmızı | trx mesajı | Sahibi | Bu paketin ürünü mü? |
|---|---|---|---|
| `P54HttpTestleri.Miktar_SIFIR_400_Turkce_mesaj_ve_veri_DEGISMEZ` | beklenen "Miktar sıfırdan büyük olmalıdır", gerçek "Miktar: Değer 0'dan büyük olm…" | GIRDI-AILESI-2 (t9: `Api/Common/Dogrulama/GirdiKapisi.cs` 13:36'da, `Sozlukler.Girdi.cs`) | **Hayır** — bu paket doğrulama iletilerine dokunmadı |
| `FaturaEkranTestleri.Y4C_K12_ekranda_gelistirici_dili_YOK_Basit_ve_Pro(basit: False)` | Fatura ekranında `ElementReference="…"` geliştirici dili | Fatura ekranı / ortak form bileşenleri (GIRDI-AILESI-2 `Bilesenler/Form`) | **Hayır** — Fatura ekranına ve form bileşenlerine dokunulmadı |

**İlk tam koşum (13:37, ara kod)** 11.084 / 11.036 geçti / 46 kırmızı: 45'i başka şeritlerin (18'i "Virgülden sonra en çok 2/3 hane", diğerleri "Değer 0'dan büyük olmalı", "Eksi değer yazılamaz", "Tarih bugünden ileri olamaz", "Gerekçe en az 20 karakter", Kur/S3f/KesifP6 form olayları — hepsi GIRDI-AILESI-2'nin doğrulama ailesi değişikliği, koşum sırasında ilerledi), **1'i bu paketin**: `Toplu5GunSonuTestleri.T5_PRO_gun_sonu_durumu_cumle_duzeninde_formul_ipucunda` ("Fark var" bekliyordu; D3 kararıyla "N satışın faturası yok" oldu) → test yeni davranışa göre güncellendi (`Toplu5Testleri.cs`, `Toplu5` 53/53). Ardından üretim kodunda KAR-HIZ iyileştirmesi (ilk-k sıralama, tek kart sorgusu, kopyasız birleşik görünüm) yapıldığı için tam süit **son kodla ikinci kez** koşuldu (yukarıdaki sonuç) — kural "tur başına bir kez, son kodla"; ilk koşum ara kodla olduğundan kanıt sayılmadı.
**PARALEL ŞERİT KAPISI madde 7:** son koşumdaki 2 kırmızının ikisi de bu paketin dosyalarına dokunmayan testler; bu paketin yeni kırmızısı **0**.

---

## 7. Canlı doğrulama (API 5162 · `kuyumcu_net` · kiracı `PAKETRAPOR-4-TEST Kuyumculuk` `01M4DFFJKWW5PY5CZ6KEZYNEXM`)
Kiracı YALNIZ ürünün uçlarıyla kuruldu (`canli-tohum.mjs`: davet → kayıt → giriş → elle kur → kasa → cari → 3 ürün açılış stoğuyla (biri kart maliyeti 18.000) → alış 2×15.000 → 5 satış (25.000 nakit, 26.000 kart, 22.500 veresiye, 4 g K14 nakit, 23.000 kart) → 22.500 veresiye iadesi). Hurda alışı yapılamadı (`400 Hurda alışında satıcı kimliği zorunludur (VUK md.234 — gider pusulası)`) — hurda bileşeni DB testi RD01'de sınandı. İlk iki tohum denemesi 400 ile yarıda kaldı (gümüş ayarı ZIYNET tipine uymadı; kişi başı günlük nakit sınırı 30.000) → yarım kiracılar `01M4DFETF1SEM4E88J8C0KMKT7`, `01M4DFF8YMKH42X2FV20NA31P0` da silindi (§ 12).

| Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|
| `GET /raporlar/aylik?yil=2026&ay=10` | 200; `satisAlisFarkiTl = netSatisTl − alis − hurdaAlisTl` | 200; brüt 99.000 · iade 22.500 · **net 76.500** · alış 30.000 · hurda 0 · **fark 46.500** (76.500 − 30.000 − 0 ✓; eski tanım 69.000 derdi) · kâr 15.000 · KISMI |
| `GET /raporlar/kar-zarar?baslangic=bugün&bitis=bugün` | 200; kart maliyetli 2 adet, kâr bilinenlerden | 200; KISMI · hasılat 76.500 · bilinen 51.000 · maliyet **36.000** (2 × 18.000 kart) · **kâr 15.000** · `kartMaliyetliAdet 2` · maliyeti eksik 3 satış · `hasilatHas 11,2052` · `hasilatHasDefterGram 27,988` · `karHas null` · has durumu HESAPLANAMADI |
| `GET /raporlar/kar-zarar/urun?…&limit=50` | 200; kartlı ürün TAM, diğerleri HESAPLANAMADI | 200; toplam 3 · Kartlı Bilezik: kâr **15.000** · TAM · maliyet 36.000 · kart 2 · karHas null; Kartsız Kolye: HESAPLANAMADI (FIFO açılış partisinden tüketti); Gram Kolye: HESAPLANAMADI |
| `GET /raporlar/gunluk?tarih=bugün` | 200; Panel has (defter) | 200; satış 99.000 · `toplamHas 35,316` (brüt defter) — Kâr-zarar'ın 11,2052'si TL karşılığıdır (VERİ 3, etiket) |
| `…/kar-zarar/urun?…&ofset=-1` | 400 Türkçe alan mesajı | 400 `ofset 0 ile 100000 arasında olmalıdır` |
| `…/kar-zarar/urun?…&limit=0` | 400 | 400 |

**Bağımsız SQL** (`kuyumcu_net`, storno hariç): `SATIS 5 adet 99.000,00 · SATIS_IADE 1 adet −22.500,00 · ALIS 1 adet −30.000,00` → net 76.500 − 30.000 = 46.500 ✓. Kartlı ürün: `alis_maliyeti 18000.00`, tüketim satırı 2, maliyetsiz satır **2**, adet 2; partisi `SAYIM_FAZLA · birim_maliyet_tl NULL · adet_giren 10`; satış tutarı 51.000 → 51.000 − 2×18.000 = 15.000 ✓.

**Yük DB'sinde canlı eşdeğerlik (eski ikili ↔ yeni ikili, aynı veri, kart maliyeti boşken):** 2 kiracı × 6 sorgu (3 yıl limit 50 ofset 0/100, limit 500 ofset 24.800/49.500, 30 gün, 1 gün) → **12/12 yanıt alan alan AYNI** (`esdeger-once/` ↔ `esdeger-sonra-kartsiz/`).

---

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/tur.mjs` + `analiz.py`; parolasız oturum `inceleme-vekili.py`; rotalar `/raporlar`, `/raporlar/kar-zarar`, `/` (Panel); tüm sekmeler ve güvenli açıcılar)
Kombinasyon: **3 rota × 3 genişlik (1440 · 1280 · 375) × BASİT/PRO × açık/koyu = 36 sayfa + 78 sekme** ölçümü, 168 açıcı (48'i açıldı); `hata 0 · geçersiz 0`. Tam tablo (114 satır, her satır rota × genişlik × mod × tema): `ham/RAPOR-4-olcum/tarayici/tur3-tablo.md`; ham ölçüm `olcum-1..4.jsonl`; 162 görüntü `tarayici/foto/`.

**Özet (114 ölçümün hepsinde):** K1 gizli-görünen **0** · K3 taşan/kırık **0/0** · K5 'TRY' **0** · K7 hizasız **0** · K9 binen **0** · K10 kesik **0** · K11 hata bandı **0** · K12 ezik **0** · K13 kırpılan **0** · yatay kaydırma **0**. Tek kalan bulgu sınıfı **K4 "tren" (sayfa boyu)**: RM PRO 375 (2,4×9), KZ 375 (BASİT 2,4×4 · PRO 3,1×5), Gün sonu PRO 1280/375 (2,1×5 / 2,9×5), Stok yaşı PRO 375 (3×6) — rapor ekranlarının doğal uzunluğu (tablo/kart yığını); bu paket bölüm EKLEMEDİ (KZ'de Karşılaştır'ı katlayarak 375 boyunu kısalttı).

**Ara tur (tur2) bulgularından düzeltilenler** (tur3'te 0): K3 taşan `"TL kasalar · Şu anki para"` (görsel gizli satır ölçerce taşan sayılıyordu → tek para biriminde çizilmez), K9 `"kart maliyeti"` rozeti not metnine biniyordu (375) → esnek satır, K9 RM grafiği `"07.10"`↔`"08.10"` (375) → son yazıya tam adımdan yakın yazı düşer; ilk turda (tur1) "+ Karşılaştır" ızgaranın başına itiliyordu → kendi satırı.

**Görüntüler (Read ile bakıldı):** `raporlar__PRO_aydinlik_1440` (AYLIK SATIŞ 76.500 − ALIŞ 30.000 = NAKİT FARKI 46.500, alt satır "net satış − alış − hurda"), `…__sekme1` (Günlük Satış/Alış: 99.000 ₺ / 49.500 ₺ / 0 ₺ değer ekseni, 01.10 · 03.10 · 05.10 · 07.10 · 08.10 okunur, Satış/Alış lejantı; akordeon karttan ayrık), `raporlar__PRO_karanlik_375__sekme1` (koyu 375: 01.10 · 03.10 · 05.10 · 08.10, binme yok), `…_1440__sekme4` (Stok yaşı başlığında not yok, ⓘ içinde), `…_1440__sekme5` (SATIŞ TOPLAMI 99.000 "(iade düşülmeden)", DURUM "5 satışın faturası yok", TOPLAM TAHSİLAT dökümü iki satır), `raporlar__BASIT_aydinlik_1440` (Kasada ne var: "Nakit 27.500,00 ₺   Kart/Havale 49.000,00 ₺", açıklama satırı yok), `raporlar_kar_zarar__BASIT_aydinlik_1280/375` ("+ Karşılaştır" tarihlerin altında; "kart maliyeti" rozeti + not; "Altın kârı hesaplanamadı: …" tek cümle), `…__sekme1` (tek seri çubuklar etiketlerinin tam üstünde), `raporlar_kar_zarar__PRO_karanlik_1280` (has: "SATIŞ TUTARININ ALTIN KARŞILIĞI 11,2052 has" ↔ "DEFTERDEKİ ALTIN (HAS) 27,9880 has" yan yana), `kok__BASIT_aydinlik_1440` (Panel BASİT: üst sarı yasal bant YOK, "● Yasal liste yüklenmedi — Uyum" Bekleyen iş satırında; satış kartı "27,9880 has").

---

## 9. Mutasyon kanıtı (kaynak `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-4/` → perl tek değişiklik → filtreli test → geri kopya + touch → cmp)
Her satır: tek eşleşmeli `perl -0777` değişikliği → `dotnet build` → filtreli test → yedeği geri kopya + `touch` → `cmp` **AYNI**. Kayıtlar `ham/RAPOR-4-olcum/mutasyon/` (`ozet*.txt`, `*.diff`).

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M4-01 | `satisAlisFarkiTl` → eski `satisTl − alisTl` | RD01 (beklenen 19.500, gerçek 30.000) | AYNI |
| M4-02 | Olc'ta kart dolgusu kaldırıldı | RD02 | AYNI |
| M4-03 | Kart maliyeti `> 0` → `>= 0` (0 bilinen sayılır) | RD02b(kart: 0) | AYNI |
| M4-04 | Kova karşılama toleransı (+0,0005) kaldırıldı | RD_KH04 | AYNI |
| M4-05 | Doğrulama ayı önbellekten ATMIYOR | RD_KH02 | AYNI |
| M4-06 → **M4-06b** | Ay "pencere içinde" koşulu her zaman doğru | ilk koşumda **yeşil (test açığı)** → deterministik RD_KH05 yazıldı → **RD_KH05** kırmızı | AYNI |
| M4-07 | `UrunKovasi.Kar` kart `>= 0` | RD_KH04 | AYNI |
| M4-08 / M4-08b | Sayfada `Skip(ofset)` kaldırıldı (tam sıralı / ilk-k sürüm) | OY08, RD_KH01, RD_KH02, doğrulayıcının SNRK06/SNRD01 | AYNI |
| M4-09 | Ön-toplamda storno süzgeci kaldırıldı | RD_KH01, RD_KH02, RD_KH03 | AYNI |
| M4-10 → **M4-10b** | Şube uyumsuz ay da önbellekten | ilk koşumda **yeşil (test açığı)** → **RD_KH05** kırmızı | AYNI |
| M4-11 | CubukGrafik tek seri ortalama kapalı | RD4E05, Sayfa_EnAzBirVeriGrafigiCizer | AYNI |
| M4-12 | KZ Karşılaştır hep açık | RD4E10 | AYNI |
| M4-13 | Nakit farkı kartı eski açıklamaya döndü | RD4E02 | AYNI |
| M4-14 | Kart maliyeti notu hiç çizilmiyor | RD4E11 | AYNI |
| M4-15 | Gün sonu PRO durumu eski "Fark var" | RD4E03 | AYNI |
| M4-16 | Seyrek eksen adımı 1 (31 yazı) | RD4E01 | AYNI |
| M4-17 → **M4-17b** | Doğrulamada iz → sürüm artışı kaldırıldı (kısmi baş bayatlar) | ilk koşumda **yeşil** (aralık önbelleği 12 anahtarla tavanı aşıp boşalıyordu) → tek aralıklı **RD_KH06** yazıldı → kırmızı | AYNI |
| M4-18 | İlk-k öbeğinde karşılaştırma ters (`> 0`) | OY08, RD_KH01, RD_KH02, SNRK06 | AYNI |
| M4-19 | İlk-k sonrası son sıralama kaldırıldı | OY08, RD_KH01, RD_KH02, SNRK06 | AYNI |
| M4-20 | Birleşik görünüm paylaşılan+canlı ürünü birleştirmiyor | RD_KH01, RD_KH02, RD_KH03, RD_KH06 | AYNI |
| M4-21 → **M4-21b** | Yalnız canlı parçadaki ürün düşürülüyor | ilk koşumda **yeşil** → RD_KH02'ye "yalnız bu ay satılan yeni ürün" adımı → **RD_KH02** kırmızı | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

---

## 10. Denetim (`dotnet/denetim.sh`)
Son koşum (`ham/RAPOR-4-olcum/denetim-final.log`, son kodla): **313 ✓, 2 ✗**.
- ✗ **P-30 DEMO_INSAN_SURESI** — kabulde istisna (kurucu kalemi).
- ✗ **P-18 MOBIL_TABLO_SARMALAYICISIZ** "0 elle sarmalayıcı · 6 KartListe" — sayaç `KartListe` kullanımının ≥ 9 olmasını ister. Bu paketin 12:40 ve 13:0x denetim koşumlarında **✓ 10 KartListe** idi; düşüş başka şeridin (GRID-1 / ortak `Bilesenler/Grid/Izgara.razor` geçişi) işi. Bu paketin dokunduğu `Raporlar.razor`'da KartListe sayısı önce 1 / sonra 1 (değişmedi); `KarZarar.razor`, `Panel.razor`'da yok. → orkestratör kapısında GRID-1 sahibine.
**Bu paketin sayaçları (hepsi ✓, dördü SIKILAŞTIRILDI):** Sınırsız rapor dökümü **18 (tavan 20 → 18)** · Sınırsız ToListAsync (Modules) **140 (142 → 140)** · TB-19 gerçekten sınırsız **24 (26 → 24)** · TB-19 büyüyen toplama 1 (1) · Sınırsız okuma beyaz listesi **14 (15 → 14)**: `KarZararServisi.cs:UrunBazli` kalemi kapandı ve listeden ÇIKARILDI (liste yalnız kısalır). Not: ölçüm bir deyimi kendinden önceki fonksiyona atfediyordu — `StokDegerlemesi`'nin ilk deyimi yeni `UrunSatirlari`'na atfedilince sahte "yeni sınırsız okuma" çıktı; `StokDegerlemesi`'ne bir yerel değişken deyimi (`var tenantId = islem.TenantId;`) konarak doğru fonksiyona atfedilmesi sağlandı (davranış aynı).

---

## 11. DoD sözleşmesi
Yeni VARLIK / kayıt türü YOK (yeni migration yalnız indeks). Ön-toplam kovaları bellekte türetilmiş önbellektir — kullanıcı kaydı değildir, düzenleme/pasife alma yolu YOKTUR; düzeltme kaynağı olan işlemin ekranından (satış iadesi, storno — 48-A motoru) yapılır, önbellek kendini geçersizleştirir. Değişen uçlar salt okunur raporlardır (`GET /raporlar/aylik`, `/raporlar/kar-zarar`, `/raporlar/kar-zarar/urun`); `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` ve `[ModulYetkisi("rapor")]` DEĞİŞMEDİ. Kart maliyeti ürün kartında zaten düzenlenebilir (`PATCH /urunler/{id}` — `AlisMaliyetiGonderildi`: gönderilmeyen = koru, null = sil; değişmedi). Yeni düğme `#kz-karsilastir-ac` istek atmaz ama tıklanınca görünür sonucu vardır (Secici açılır; RD4E10). Finansal tablolarda yazma yok.
**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Raporlar.razor`, `Pages/KarZarar.razor`, `Pages/Panel.razor` (yalnız `Gorunmez` parametresi + 1 özellik), `Bilesenler/CubukGrafik.razor`.

---

## 12. Temizlik
- API'ler durduruldu: 5162 (canlı, `kuyumcu_net`) ve 5162/5168 (yük) — `lsof` boş (15:00). Vekil 5163 durduruldu. Başsız Chrome süreçleri kapandı (`remote-debugging-port=970*` 0). k6 süreci yok.
- Test kiracıları (`kiraci-temizle.sh <id> --uygula`, zsh): `01M4DFFJKWW5PY5CZ6KEZYNEXM` (asıl), `01M4DFETF1SEM4E88J8C0KMKT7` ve `01M4DFF8YMKH42X2FV20NA31P0` (yarım tohum denemeleri). Silme anında (14:12:05) başka bir API'nin zamanlanmış iş koşucusu asıl kiracı için 6 `is_calismalari` + 12 `denetim_kayitlari` satırı yazdı (betik sildikten SONRA) → 75 tenant_id tablosu tarandı, bu 18 satır doğrudan SQL ile silindi (kurucu izni: proje DB satırları). Son tarama: üç kimlik için tüm tablolarda **0 satır**.
- `kuyumcu_net.tenants`: başta **18**, iş sonunda **16** — bu paketin net etkisi **0** (3 eklendi, 3 silindi); aradaki −2 başka şeritlerin kendi kiracılarını silmesi (`PAKETGIRDI-AILESI-2-TEST T3`, `PAKETURUN-ACILIS-HAKEM-TEST …` artık yok — bu paket bunlara dokunmadı). QA-TEST kiracılarına dokunulmadı.
- `kuyumcu_test_yuk` (1.783 MB, maliyet bacağı + yeni indeks dahil) `SET SESSION sql_log_bin=0; DROP DATABASE` → `information_schema.schemata` 0. Test DB `kuyumcu_test_t1` testlerce süreç başına kurulur.
- Sır/geçici dosyalar `printf '' >` ile boşaltıldı: `fikstur.json`, `yuk-sifre`, `sistem.anahtar`, `kiracilar.json` (hepsi `/private/tmp/kuyumcu-serit/RAPOR-4/`). Proje ağacına `.bak/.tmp/.mutorj` bırakılmadı (ağaçtaki `Islemler.YasalParam.cs.tmp` bu paketin değil).

---

## 13. Açık kalanlar ve notlar
1. **VERİ 1 kalıcı çözüm (Seçenek A) — URUN-ACILIS-ATOMIK sahibine:** açılış stoğu partisi (`StokSayimGuncelle` → `PartiAc(SayimFazla, birimMaliyetTl: null)`) ürün kartındaki `alisMaliyeti` girilmişse o değeri (ve o günün has fiyatıyla `birimMaliyetHas`) taşımalı; böylece kart sonradan düzenlense de geçmiş kâr değişmez ve has kârı da hesaplanır. Bu paket oluşturma yoluna dokunmadı (dosya alanı). Okuma anındaki kart dolgusu (Seçenek B) o zaman yalnız eski kayıtlar için kalır.
2. **Kart maliyetinin geriye etkisi (bilinçli, rozetle görünür):** kart maliyeti düzenlenirse "kart maliyetli" geçmiş satışların kârı da değişir. Kurucu/mali müşavir görüşü istenebilir (§ 15).
3. **Ham SQL bakımı ↔ ön-toplam önbelleği:** defter/parti satırı SQL ile silinir/değiştirilirse API süreci yeniden başlatılmalı ya da `KarUrunOnToplam.Temizle()` çağrılmalı (uygulama yolları append-only; bu yalnız bakım içindir). Çok örnekli yayında her örnek kendi önbelleğini aynı kurallarla doğrular.
4. **Soğuk başlangıç:** süreç açılınca büyük kiracının ilk 3 yıllık isteği 3,5–4,1 sn (sakin), yük altında daha uzun. İstenirse açılışta arka planda ısıtma (ayrı paket).
5. **K4 sayfa boyu** (375'te RM PRO, KZ, Gün sonu, Stok yaşı; 1280'de Gün sonu PRO) — rapor ekranlarının yapısı; sadeleştirme/sekme paketi.
6. **KAPSAM DIŞI (bulgu listesinden, dokunulmadı):** Panel PRO tasarım artıkları (Kur/Bekleyen kart boşluğu, Son işlemler punto/işaret, 7 günlük grafikte değer ekseni, Kasa özeti 375 düzeni, etiket kırılması); canlı tema geçişinde ikincil düğmeler.
7. **Tam süitte başkasına ait 2 kırmızı** (§ 6): GIRDI-AILESI-2 (P54 doğrulama iletisi; Fatura Y4C geliştirici dili). **Denetim P-18** KartListe 10 → 6 (GRID-1 / Izgara geçişi).
8. **Yük ölçümü ortam notu:** makine gün boyu 20–54 yük ortalamasındaydı; kabul ölçümü sakin pencerede (ab6/ab7) alındı. Yük altında p95 998–1.706 ms ölçüldü (gizlenmedi) — orkestratör dalga kapısında sakin makinede tekrar ölçebilir (`ham/RAPOR-4-olcum/ab-olc.sh`, `yuk-tohum-ek.sql`).
9. **Port çakışması:** t1'e ayrılan 5160/5161 iş boyunca GRID-1-hakem artığı süreçlerdeydi (PID 43565/44792, 06:56'dan beri); 14:47'de 5162'yi de başka bir şerit (GOC-A, Debug ikili) aldı → son yük ölçümleri boştaki 5168'de. Bu paket o süreçlere dokunmadı.

10. **(Tur 2, T2-6) Servis yolu eksi alış maliyeti** — `UrunServisi.Olustur` (`UrunModulu.cs`) `AlisMaliyeti < 0`'ı reddetmiyor (HTTP yolunda `[Range]` var). Bu paketin dosya alanı UrunModulu'nda YALNIZ okuma; oluşturma yolu URUN-ACILIS-ATOMIK şeridinin → **sahibi: ürün şeridi** (`UrunKolonKapisi` içinde `alisMaliyeti < 0` → 400 Türkçe alan mesajı + test). Rapor tarafı korumalı: kâr ve stok kuralı yalnız `> 0` kartı kullanır.
11. **(Tur 2, T2-3) KAR-HIZ değişken aralık p95 1,0–1,26 sn** — sabit 3 yıl ölçütü ✓ (664/722 ms) ama yeni aralıkta aralık toplamı yeniden kurulur ve baştaki kısmi ay kiracı kilidi altında canlı okunur. Öneri (ayrı paket): kısmi baş/son parçayı kilidin DIŞINDA ve gün kovası olarak önbelleğe almak, ay kovalarından ön-ek toplamı (prefix) ile aralık toplamını O(ürün) yerine O(değişen ay) kurmak; açılışta büyük kiracılar için arka plan ısıtma (soğuk ilk istek 3,4–12 sn). Orkestratör kapısında `betikler/yuk/k6-kar-degisken.js` ile sakin makinede yeniden ölçüm.
12. **(Tur 2) Tam süitte başkasına ait 7 kırmızı** (§ T2.1): 6'sı "yildiz" tema şeridi, 1'i ortak Secici (GIRDI-AILESI-2). **Denetim** ✗ OLCEK-1 (tema-yildiz.css) + ✗ P-30 (kabul istisnası).

### Kurucu / mali müşavir
- Bu pakette **mevzuat hükmü yok** (yasal eşik/oran yazılmadı). Mali müşavire tek soru (karar onayı): *"Partide alış maliyeti bulunmayan açılış stoğu satışlarında, ürün kartına girilen alış maliyetinin kâr hesabında (yönetim raporu, resmî defter DEĞİL) 'kart maliyeti' etiketiyle kullanılması uygun mu; yoksa açılış envanteri maliyeti girilene kadar 'hesaplanamadı' mı kalmalı?"* — kod her iki yönde tek satırla değiştirilebilir (`KartMaliyetleri` → boş sözlük = eski davranış).

---

## 14. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (ağaçta 611 satır; bu paketin dosyaları):
- ` M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` — nakit farkı tek tanım + `hurdaAlisTl`.
- ` M dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — `IX_hareketler_tenant_id_olusturuldu`.
- ` M dotnet/Kuyumcu.Web/Bilesenler/CubukGrafik.razor` — değer ekseni + seyrek eksen kipi, tek seri ortalama.
- ` M dotnet/Kuyumcu.Web/Pages/Raporlar.razor` — nakit farkı kartı, grafik, stok yaşı notu, gün sonu PRO, kasa kartı.
- ` M dotnet/Kuyumcu.Web/Pages/Panel.razor` — BASİT yasal bant yalnız kritikte.
- `?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor` (izlenmeyen) — + Karşılaştır, kart maliyeti notu/rozeti, has etiketi, BASİT altın kârı cümlesi, `kz-sayfa`.
- `?? dotnet/Kuyumcu.Web/wwwroot/css/rapor-gorunum.css` (izlenmeyen) — (18)–(22).
- `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (izlenmeyen) — 9 anahtar TR+EN.
- `?? dotnet/denetim.sh` (izlenmeyen) — 4 tavan sıkılaştırma + beyaz listeden `KarZararServisi.cs:UrunBazli` çıktı.
- İzlenmeyen dizinlerin içinde: `dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` (kart dolgusu, yeni `UrunBazli`, `UrunSatirlari`, `KartMaliyetleri*`), **YENİ** `dotnet/Kuyumcu.Api/Modules/Maliyet/KarUrunOnToplam.cs`, **YENİ** `dotnet/Kuyumcu.Api/Data/Migrations/20261008090525_RaporDortOlusturulduIndeksi.cs` + `.Designer.cs`, `KuyumcuDbContextModelSnapshot.cs`, `dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` (+`KartMaliyetliAdet`), `RaporlarSozlesmeleri.cs` (+`HurdaAlisTl`), **YENİ** `dotnet/Kuyumcu.Tests/RaporDortTestleri.cs`, **YENİ** `dotnet/Kuyumcu.Tests/RaporDortEkranTestleri.cs`, güncellenen `KarZararEkranTestleri.cs`, `KarRaporTestleri.cs`, `MadenIkiTestleri.cs`, `ParaDemoEkranTestleri.cs`, `ParaDemo2EkranTestleri.cs`, `Toplu5Testleri.cs`.
- Kanıt dizini: `dokumanlar/09-uygulama-kayitlari/ham/RAPOR-4-olcum/` (k6, eşdeğerlik JSON'ları, canlı yanıt + SQL, mutasyon özet/diff, tarayıcı ölçümü + 162 görüntü, denetim çıktısı, betikler).
- `dotnet/denetim.sh.mutbak` (0 bayt, 18.09) bu paketin değil.

---

## 15. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| VERİ 1 kök neden + karar + canlı/SQL | ✓ | § 1, § 7 (SAYIM_FAZLA partisi NULL maliyet; kart 18.000 → kâr 15.000) |
| VERİ 2 nakit farkı tek tanım + test | ✓ | RD01, RD4E02, canlı 46.500 = 76.500 − 30.000 |
| VERİ 3 Panel/KZ has etiketi | ✓ | MI11/KR62 güncel, görüntüler § 8 |
| KAR-HIZ eşdeğerlik testi (rastgele + sınır) | ✓ | RD_KH01–06, canlı 12/12 AYNI |
| KAR-HIZ p95 < 800 ms (3 yıl, büyük kiracı) | ✓ (sakin pencere) | ab6 p95 514 ms; ab7 eski 7.591 ms |
| Mutasyon ≥ 1 kırmızı, cmp AYNI | ✓ | 21 kalem + 4 açık kapandı (§ 9) |
| Başsız Chrome 1440/1280/375 × BASİT/PRO × açık/koyu, Read | ✓ | 36 sayfa + 78 sekme; K1–K13 = 0 (K4 hariç), § 8 |
| build 0/0 | ✓ | son temiz build 0 Uyarı 0 Hata |
| denetim TEMİZ (P-30 hariç) | ⚠ bu paketin sayaçları ✓; ağaçta P-18 ✗ başka şeridin | § 10 |
| Tam süit 0 yeni kırmızı | ✓ | 11.118/11.122, 2 kırmızı başka şeridin (§ 6) |
| Migration prosedürü | ✓ | § 3 |
| Test kiracısı silindi, tenants net 0 | ✓ | § 12 |
| COMMIT YOK · dosya silme yok · sır basılmadı | ✓ | — |
| **Tur 2:** KRİTİK KZ grafik "Satış" = bilinen küme | ✓ kapandı | T2-1; RD4E07/07b/13, RG41; canlı + görüntü |
| **Tur 2:** D2 akordeon boşluğu (RM Grafikler/Stok yaşı/Gün sonu) | ✓ kapandı | T2-2; piksel ölçümü 15–16 px, RD4E09 |
| **Tur 2:** KAR-HIZ sabit 3 yıl p95 < 800 (son kod) | ✓ (yük 9–18) | agir 664 ms · değişkenin sabit isteği 722 ms |
| **Tur 2:** KAR-HIZ değişken aralık/şube senaryosu | ⚠ eklendi + ölçüldü, p95 1.022–1.259 ms > 800 | `k6-kar-degisken.js`; § 13 madde 11 |
| **Tur 2:** RM grafik net satış / stok kart kuralı / mimari bekçi / yorum + style | ✓ | T2-4, T2-5, T2-7, T2-8 |
| **Tur 2:** servis yolu eksi alış maliyeti | ✗ dosya alanı dışı → ürün şeridi | T2-6, § 13 madde 10 |
| **Tur 2:** mutasyon | ✓ 10 kalem, hepsi kırmızı, cmp AYNI | § T2.2 |
| **Tur 2:** tam süit yeni kırmızı | ✓ 0 (7 kırmızı başka şeritlerin) | § T2.1 |
| **Tur 2:** migration | ✓ `20261008140742_RaporDortKartMaliyetiIndeksi` kuyumcu_net'e uygulandı | § T2.6 |
| **Tur 2:** test kiracısı silindi | ✓ 75 tablo 0 satır | § T2.6 |
