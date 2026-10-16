# PAKET EKSIK-B1-YAZDIR — Uygulama raporu

**Tarih:** 09.10.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** t5 (API 5166 · vekil 5167 · CDP 9505/9506 · KUYUMCU_TEST_DB=kuyumcu_test_t5)
**Kaynak:** `EKSIK-OZELLIK-DENETIMI-2026-10-02.md` (B) B1 satır 3, 4, 5, 6 · (F) madde 3. **COMMIT YOK.**


## 0. TUR 2 (09.10.2026 ~10:00) — doğrulayıcı bulgularının kapatılması

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| **[KRİTİK]** Kapora kilit iletileri EN modda Türkçe; `kaporaKilitSebebi` Cevir'den geçmiyor | `SunucuIletiSozlugu.Veri.cs`: satır 183 yeni "zaten alındı… (storno)…" cümlesiyle birebir; satır 812 "İptal edilmiş…", 813 "Teslim edilmiş…" TR/EN çiftleri. `SiparisModulu.Detay`: `kaporaKilitSebebi = SunucuIletiSozlugu.Cevir(KaporaKilidi(...))` (null korunur). 409 iletileri ApiException ara katmanında zaten Cevir'den geçiyor. | `Dil_kapora_kilidi_iletileri_ingilizceye_cevrilir` Skip KALDIRILDI → yeşil · yeni `B15_EN_istekte_detay_kilit_sebebi_ingilizce` (servis, EN/TR kapsamı) · yeni bUnit `B15_EN_modda_kapora_kilit_sebebi_ingilizce` (IPTAL, TESLIM_EDILDI; sahte API kilidi sunucunun gerçek `KaporaKilidi` + `Cevir` fonksiyonuyla ve isteğin Accept-Language başlığıyla üretir) · canlı EN 409 ×3 İngilizce · gerçek Chrome EN 16/16 ölçümde `#sipd-kapora-kilit` Türkçe harf 0 |
| **[ORTA]** Rota değişince kapora çekmecesi/geri verme penceresi açık kalıyor | `SiparisDetay.razor` `OnParametersSetAsync`: `kart.Id != Id` ise `KaporaFormlariniSifirla()` (kaporaAcik = kaporaGeriAcik = false, tutar/ödeme/kasa/gerekçe/hata alanları temizlenir) sonra `Yukle()` | yeni bUnit `B15_rota_degisince_kapora_cekmecesi_kapanir_form_temizlenir` · gerçek Chrome 8/8 kombinasyon: s4'te çekmece açık + 750 yazılı → `/siparisler/{s1-iptal}` → `#sipd-kapora-form` YOK, `#sipd-kapora-kaydet` YOK, kilit satırı görünür (foto `rota-s4-s1__*`) |
| **[DÜŞÜK]** MASAK eşiği üstü kapora ekrandan alınamıyor | Kod değişmedi (kural gevşetilmedi, fail-closed). **Açık kalanlar § 9 madde 5**'e yazıldı. | — |
| **[DÜŞÜK]** Veri.cs değişikliği raporda yok | "Değişen dosyalar" listesine eklendi. **Sahiplik:** satır 438 ("Kapora nakit, kart ya da havale…"), 183 (güncel cümle), 812–813 bu paketindir — üç cümle de yalnız bu paketin kodunda (`SiparisModulu.Kapora`/`KaporaKilidi`) üretilir (`grep`: yalnız SiparisModulu.cs + EksikB1YazdirTestleri.cs + Veri.cs). Dosyanın geri kalanı DIL-SUNUCU şeridinindir; dosya git'te izlenmiyor (`??`), satır sahibi git'ten kanıtlanamaz, içerikten belirlendi. | `grep -rl` çıktısı |

Tur 2 test sayıları: kendi testlerim 45 → **49** (+ doğrulayıcının `Dil_` testi Skip'ten çıktı). Filtreli (EksikB1 · EKSIKB1 · DilSunucu · P26 · DilEkran · P51Ekran · SiparisEkran): **194/194 geçti**. **Tam süit (trx `/private/tmp/kuyumcu-serit/trx-pEKSIK-B1-YAZDIR-t2/_Ramazan-MacBook-Air_2026-10-09_09_58_09.trx`): 11647 toplam · 11645 geçti · 0 başarısız · 2 koşmadı (Skip, başka paketlerin).** Tur 1'deki 7 yabancı kırmızı bu koşumda yok.

Tur 2 mutasyonları (perl tek eşleşme → filtreli `EksikB1|EKSIKB1` → yedekten geri → cmp; yedek `/private/tmp/kuyumcu-serit/mut-yedek/EKSIK-B1-YAZDIR-t2/`):

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M11 | Detay: `Cevir(kilitTr)` → `kilitTr` | B15_EN_istekte_detay_kilit_sebebi_ingilizce | AYNI |
| M12 | Veri.cs "Teslim edilmiş…" çifti silindi | Dil_kapora_kilidi_iletileri_ingilizceye_cevrilir · B15_EN_modda_kapora_kilit_sebebi_ingilizce(TESLIM_EDILDI) | AYNI |
| M13 | `OnParametersSetAsync` içindeki `KaporaFormlariniSifirla()` çağrısı silindi | B15_rota_degisince_kapora_cekmecesi_kapanir_form_temizlenir | AYNI |
| M14 | Veri.cs "zaten alındı" TR anahtarı bozuldu | Dil_kapora_kilidi_iletileri_ingilizceye_cevrilir | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

Tur 2 canlı (5166, kendi PID 87074/87085; vekil 5167 PID 92678; kiracılar PAKETEKSIK-B1-TEST-A3 `01M4FQA8465NS7NC5WFB6S6Z52`, -B3 `01M4FQA8QD3ATGGYWF7EE1HE88`):

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET s1 (tamir + usta) | 200 ustaAd | 200 "Usta Hasan" |
| kapora s1 2000 NAKIT (gövdede yabancı cariId) | 200 | 200, kaporaTutari 2000 |
| HASIM çift kapora (TR) | 409 | 409 "Bu siparişte kapora zaten alındı…" |
| HASIM VERESIYE / 0 / −50 / 10,555 / "abc" / 1e17 | 400 | 400 ×6 (alan mesajlarıyla) |
| HASIM başka kiracının siparişi | 404 | 404 "Sipariş bulunamadı" |
| HASIM olmayan kasa | 400/404 | 404 |
| s1 IPTAL → kapora | 409 | 409 "İptal edilmiş siparişe kapora alınamaz." |
| iptalde geri verme (48-A storno) | 200 | 200; kaporaStornolu=true |
| **EN** çift kapora (Accept-Language: en) | 409 İngilizce | 409 "A deposit has already been taken on this order. To correct it, return the deposit (reversal); …" |
| **EN** iptal s1 kapora | 409 İngilizce | 409 "A deposit cannot be taken on a cancelled order." |
| **EN** GET s1 `kaporaKilitSebebi` | İngilizce | "A deposit cannot be taken on a cancelled order." |
| stok alış 1×1000 NAKIT · s2 kapora 2000 → ONAY → HAZIR → satışa çevir 8000 nakit + 2000 açık hesap | 201 | 201; durum TESLIM_EDILDI |
| **EN** teslim s2 kapora · **TR** aynı | 409 / 409 | "A deposit cannot be taken on a delivered order." / "Teslim edilmiş siparişe kapora alınamaz." |

Not: canlı betikteki ilk "veresiye alış" adımı 400 "Ödeme türü beklenen biçimde değil" döndü (betik girdisi; alış ucu bu paketin alanı değil) → stok nakit alışla girildi.

SQL (kuyumcu_net, kiracı A):
```
no             tur              durum         kapora_bagli usta       h_tur    tl_tutari ayni_cari storno_sayisi
S-2026-000001  TAMIR            IPTAL         1            Usta Hasan TAHSILAT -2000.00  1         1
S-2026-000002  MUSTERI_SIPARISI TESLIM_EDILDI 1            NULL       TAHSILAT -2000.00  1         0
TAHSILAT satırları: -2000 (s1 kapora) · +2000 (s1 storno, storno_kaynak_id dolu) · -2000 (s2 kapora)
cari_bakiyeler: Ayşe Kapora 0.00 (kapora satışta düştü) · Usta Hasan 0.00 · Merkez Kasa 9000.00
```

Tur 2 gerçek tarayıcı (başsız Chrome CDP 9507, sikisma-olc.js, **48 ölçüm**; `olcum-t2.json`, foto `/private/tmp/kuyumcu-serit/EKSIK-B1-YAZDIR/tur2/foto/`):

| Rota × {1280,375} × {BASİT,PRO} × {açık,koyu} | K1 | K3 taşan/kırık | K5 TRY | K9 binen | K10 | K11 | yatay | Not |
|---|---|---|---|---|---|---|---|---|
| s1 iptal (TR) — kilit satırı | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 kilit görünür |
| s3 atölyede, canlı kapora, usta | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | şerit · usta · makbuz · geri ver 8/8 görünür |
| s4 taslak + kapora çekmecesi | 0 | 0/0 | 0 | 6 | 0 | 0 | yok | K9=6: çekmece sayfanın üstünde (katman örtüşmesi, beklenen; tur 1 ile aynı) |
| s4 çekmece açık → s1'e rota | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 form YOK, kaydet YOK |
| **EN** s1 iptal | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | 8/8 "A deposit cannot be taken on a cancelled order.", Türkçe harf 0 |
| **EN** s2 teslim | 0 | 0/0 | 0 | 0 | 0 | 0 | yok | canlı kapora olduğu için ekran kilit satırı değil kapora kartını gösterir (tasarım); EN teslim cümlesi canlı 409 + bUnit ile kanıtlandı |

Read ile bakılan görüntüler (3): `EN-s1-iptal__PRO_karanlik_375` (Deposit kartı ve kilit satırı İngilizce), `rota-s4-s1__BASIT_aydinlik_1280` (çekmece kapalı, iptal siparişte yalnız kilit satırı), `s4-cekmece__BASIT_karanlik_375` (çekmece, 750 ₺, tek kasa salt okunur).

Tur 2 denetim: `bash denetim.sh` → yalnız **✗ P-30 DEMO_INSAN_SURESI** (görev metninde hariç). `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 0` ✓ · `P-11 …_TOPLAM 47 (tavan 47)` ✓ · `ModalPencere 48 (tavan 55)` ✓ · `P-26 DEFTERE_YAZMAYAN_PARA_MODULU 0` ✓.

Tur 2 temizlik: API (87074/87085) ve vekil (92678) `kill`; `lsof` 5166/5167 boş. İki kiracı `zsh kiraci-temizle.sh <id> --uygula` ile silindi; kendi 2 davet satırım (`kiraci_davetleri`, email `paketeksikb1%`) silindi. **tenants 19 → 19**, `PAKETEKSIK%` = 0, QA-TEST kiracıları (4) dokunulmadı. Sır dosyaları (sistem.anahtar, fikstur, creds, canli-durum) `printf '' >` ile boşaltıldı; tur 1'den 178 bayt dolu kalmış `/private/tmp/kuyumcu-serit/EKSIK-B1-YAZDIR/creds.json` da boşaltıldı (silinmiş kiracıların hesabıydı).

**Dokunulan sayfalar (tur 2):** yalnız `SiparisDetay.razor` (gerekçe: doğrulayıcı ORTA bulgusu; aynı paket, aynı dalga).

---
# TUR 1 KAYDI (aşağısı tur 1 raporudur; § 4, § 9, Değişen dosyalar tur 2 ile güncellendi)

## 1. Yapılanlar

| Satır | Ne yapıldı | Nerede |
|---|---|---|
| B1-3 | Cari detay penceresine **"Ekstreyi yazdır"** (`#cari-detay-ekstre-yazdir`) → `/cikti/CARI_EKSTRE/{cariId}`. İki modda da var; `yazdirma-gizle` kabında (kâğıda basılmaz). | `Web/Pages/Cariler.razor` (yalnız düğme + sözlük erişimi) |
| B1-4 | Emanet geçmiş satırına **"Belgeyi yazdır"** (`#eh-belge-{id}`) ve ayrıntı çekmecesine (`#eh-detay-belge-{id}`) → `/cikti/EMANET_TESLIM/{id}?donus=%2Femanet`. İki modda (eskiden yalnız İşlem Defteri + BASİT). | `Web/Pages/Emanet.razor` |
| B1-4 ek | Çıktı sayfasının **"Geri"** düğmesi `?donus=` ile geldiği ekrana döner (yalnız uygulama içi yol: `/` ile başlar, `//`, `/\`, `:` reddedilir — açık yönlendirme yok). Yoksa eski varsayılan. | `Web/Pages/Cikti.razor` (`GuvenliDonus`) |
| B1-5 | Sipariş detayına **Kapora kartı**: kapora yoksa tek satır + "+ Kapora al" (zorunlu önce kuralı) → Cekmece (tutar · ödeme şekli NAKIT/KART/HAVALE · kasa; tek TL kasasında seçici çizilmez, UX-S22) → `POST /siparisler/{id}/kapora` (mevcut P-26 ucu). Kapora varsa: tutar, hesap notu, **"Makbuzu yazdır"** (`TAHSILAT_MAKBUZU` A4) ve **"Kaporayı geri ver"** (yıkıcı → onay penceresi + gerekçe ≥ 20 → 48-A storno `POST /islemler/{hareketId}/storno`, kasa sunucunun verdiği kaynak kasa). Tezgâhtar için düğme kilitli + sebep. İptal/teslimde "+ Kapora al" yerine sunucunun sebebi. | `Web/Pages/SiparisDetay.razor` |
| B1-5 | **Usta**: kartta adıyla (`#sipd-usta`, ATÖLYE/TAMİR türünde); düzenlemede `Secici #sipd-d-usta` (atölyede zorunlu, tamirde isteğe bağlı); PATCH gövdesine `ustaCariId` (usta tutmayan türde `null` = bağ silinir). Atölye işinde usta boşsa istek gitmez, alan altı mesaj. | `SiparisDetay.razor` |
| B1-6 | **Süreç şeridi** `AdimGostergesi` (Alındı → İşlemde/**Tamirde** → Hazır → Teslim edildi) sipariş ve tamir detayında; iptalde çizilmez. | `SiparisDetay.razor` |
| Sunucu (en küçük ekleme, Api/Siparis) | `Detay` artık `ustaAd`, `kaporaTutari`, `kaporaTarihi`, `kaporaKasaId`, `kaporaStornolu` döner (defterden okunur). Kapora kilidi TEK yerde (`KaporaKilidi`): iptal → 409, **teslim edilmiş → 409 (yeni)**, canlı kapora → 409; **stornolanmış kapora "alınmış" sayılmaz** (yanlış tutar geri verilince doğrusu alınabilir — eskiden kalıcı kilitti). **Ödeme beyaz listesi**: NAKIT/KART/HAVALE dışı (VERESIYE, ALTIN, çöp) → 400 (eskiden VERESIYE "kapora" yazılabiliyordu). | `Api/Modules/Siparis/SiparisModulu.cs` |
| Sözleşme | `Kart` kaydına isteğe bağlı alanlar + `KaporaKasasi`. | `Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs` |
| Sözlük | `EksikB1Metinleri` TR+EN (konumsal; eksik dil = derleme hatası). | `Core/I18n/Sozlukler.EksikB1.cs` |

## 2. Sapmalar ve gerekçeleri

1. **Kapora yolu = P-26 ucu (`/siparisler/{id}/kapora`, TAHSILAT).** Spesifikasyon bu ucu gösteriyor. Alternatif Y-17 avans yolu (`/islemler/avans`, AVANS_ALINDI) incelendi; **MASAK kümülatif denetimi yalnız cari tahsilat gövdesinde** (`CariServisi.TahsilatGirHareket`) var, avans yolunda yok. Kaporayı avans yoluna taşımak mevzuat kapısını zayıflatırdı → yapılmadı. Sonuç: kapora müşterinin hesabına **alacak** yazılır; **satışta düşüm** cari hesap üzerinden olur (teslimde kalan tutar nakit, kapora kadarı "Açık hesap" → bakiye 0; canlı SQL § 6). Ekran bunu kartta tek cümleyle söyler.
2. **"Başka cariye ait kapora"**: P-26 ucu cariyi gövdeden ALMAZ, siparişin carisini kullanır; gövdeye yabancı `cariId` eklense de yok sayılır (canlı: hareket.cari_id = sipariş.cari_id = 1). Başka kiracının siparişi → 404.
3. **Hasım kodları**: çift kapora / iptal / teslim → **409** (çakışma — mevcut sözleşme ve P-26 testi böyle); geçersiz değerler → 400.
4. **Siparisler.razor (oluşturma formu)**: usta seçici bu pakette EKLENMEDİ (açık kalan § 9). Usta, oluşturduktan hemen sonra TASLAK düzenlemesinden girilir.
5. **Cikti.razor** dosya alanında açıkça yoktu; `?donus=` olmadan Emanet/Sipariş'ten açılan belgenin "Geri"si İşlem Defteri'ne gidiyordu (yanlış ekran). En küçük dokunuş (1 parametre + 1 saf fonksiyon), eski davranış varsayılan.
6. **FisDigerSiparis.razor'a dokunulmadı** (GOC-C-2 alanı); düşüm cari hesap üzerinden çalıştığı için gerek kalmadı.
7. **P51EkranTestleri** (başka paketin testi) 1 satır: atölyeye çevirirken artık usta seçilmezse istek istemcide durur; testin amacı (sunucu reddi alan altında görünür) usta SEÇİLİ gövdeyle korunarak sınanıyor.

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| Sipariş kaporası (hareketler TAHSILAT) | Düzenlenmez — **storno** (48-A, SAHİP/MÜDÜR, gerekçe ≥ 20) ekrandan "Kaporayı geri ver"; sonra yeniden alınabilir | Finansal satır: hard DELETE yok |
| siparisler.kapora_hareket_id | Sunucu yazar; storno sonrası yeni kapora bağı günceller | denetim izi SaveChanges dinleyicisiyle |
| siparisler.usta_cari_id | PATCH (TASLAK) — gönderilmezse korunur, `null` siler; ekranda Secici | `[RolGerekli]` mevcut PATCH ucu |
| A4 belgeler (ekstre, emanet teslim, makbuz) | Türetilmiş çıktı — DegismezIpucu mevcut ("Belge kayıttan üretilir") | salt okunur |
Yeni varlık/tablo yok → migration gerekmedi.

## 4. Testler

Yeni test: **45** (`EksikB1YazdirTestleri.cs` 23 · `EksikB1YazdirEkranTestleri.cs` 14 + saf teori satırları). Filtreli son koşum (EksikB1 + P51 + P26 + Q93 + SiparisEkran + EmanetSade + KesifP2): **196/196 geçti**.

Tam süit (trx `/private/tmp/kuyumcu-serit/trx-pEKSIK-B1-YAZDIR/_Mac_2026-10-09_06_33_07.trx`): **11616 toplam · 11607 geçti · 7 başarısız · 2 koşmadı**. 7 kırmızının hiçbiri bu paketin dosyalarına dokunmuyor (§ 9 sahipleriyle). Bu paket yeni kırmızı üretmedi (kendi + komşu süitler yeşil).

## 5. Mutasyon kanıtı (perl tek eşleşme → filtreli test → yedekten geri → cmp)

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M1 | `KaporaKilidi` canlı kapora dalı etkisiz | B15_kapora_kilidi_iptal_teslim_canli_kapora · B15_hasim_cift_kapora_409_ve_defter_tek_satir · B15_kapora_siparise_baglanir… · P26 Y30_ikinci_kapora_REDDEDILIR… | AYNI |
| M2 | ödeme beyaz listesine VERESIYE/ALTIN/çöp | B15_hasim_kapora_gercek_para_degilse_400 (3 satır) | AYNI |
| M3 | storno tespiti hep `false` | B15_kapora_geri_verilince_kasa_ve_cari_doner_dogrusu_yeniden_alinir | AYNI |
| M4 | Detay `ustaAd` null | B15_usta_detayda_adiyla_gelir | AYNI |
| M5 | şerit HAZIR → 1 | B16_serit_IKI_MODDA_hazir… · B16_durum_seritte_dogru_asamaya_duser(HAZIR) | AYNI |
| M6 | Emanet belge türü SATIS_FISI | B14_emanet_satirinda_ve_ayrintida_belge_yazdir_IKI_MODDA | AYNI |
| M7 | Cariler belge türü TAHSILAT_MAKBUZU | B13_cari_detayinda_ekstre_yazdir_IKI_MODDA… | AYNI |
| M8 | PATCH ustaCariId hep null | B15_usta_kartta_adiyla_ve_duzenlemede_PATCH_ile_gider | AYNI |
| M9 | GuvenliDonus `//` denetimi kaldırıldı | B14_cikti_donus_yolu_yalniz_uygulama_ici("//kotu.example.com") | AYNI |
| M10 | kapora boş tutar denetimi kaldırıldı | B15_kapora_bos_tutar_POST_gitmez_alan_alti_mesaj | AYNI |

Not: M3 ve M6 ilk koşumda paralel şeridin derlemesiyle çakıştı (MSB4018 dosya kilidi) — tekrar koşuldu, sonuç yukarıda. M5 koşumunda `B15_kapora_geri_verilince…` de bir kez kırmızı göründü (M3'ten kalan derleme durumu); sonraki koşumlarda mutasyonsuz yeşil. Son mutasyondan sonra temiz `dotnet build`: **0 hata** (6 uyarı, hepsi başka paketlerin test dosyalarında: SUBEKAPSAMA, KesifP2, SonKalanlar, Yildiz2).

## 6. Canlı doğrulama (5166, kendi PID'im; test kiracısı PAKETEKSIK-B1-TEST-A3/B3)

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET tamir siparişi (usta ile açıldı) | 200, ustaAd | 200, "Usta Hasan" |
| Kapora 2000 NAKIT (gövdede yabancı cariId) | 200 | 200, kaporaTutari 2000 |
| HASIM çift kapora | 409 | 409 "kapora zaten alındı… geri verin (storno)" |
| HASIM VERESIYE kapora | 400 | 400 |
| HASIM 0 / −50 / 10,555 / "abc" / 1e17 | 400 | 400 ×5 (alan mesajlarıyla) |
| HASIM başka kiracının siparişi (B jetonu) | 404 | 404 |
| HASIM olmayan kasa | 400/404 | 404 |
| Sipariş IPTAL → kapora | 409 | 409 "İptal edilmiş siparişe kapora alınamaz." |
| İptalde geri verme (48-A storno) | 200 | 200; kart kaporaStornolu=true |
| s2 kapora 2000 → ONAY → HAZIR → satışa çevir 8000 nakit + 2000 açık hesap | 201 | 201 (fiş) ; durum TESLIM_EDILDI; kapora kilidi "Teslim edilmiş…" |

SQL (kuyumcu_net):
```
no              tur              durum          kapora_bagli usta        h_tur    tl_tutari ayni_cari storno_sayisi
S-2026-000001   TAMIR            IPTAL          1            Usta Hasan  TAHSILAT -2000.00  1         1
S-2026-000002   MUSTERI_SIPARISI TESLIM_EDILDI  1            NULL        TAHSILAT -2000.00  1         0
cari_bakiyeler: Ayşe Kapora 0.00 (kapora satışta düştü) · kasalar: Merkez Kasa 9000.00 (= +2000 −2000 +2000 −1000 alış +8000)
```
PDF/yazdırma önizlemesi gerçek Chrome'da (`Page.printToPDF`, print medyası): ekstre 173.611 bayt, makbuz 143.734 bayt; print medyasında görünen `.yazdirma-gizle`/menü 0; kâğıt `rgb(255,255,255)`.

## 7. Gerçek tarayıcı ölçümü (başsız Chrome, sikisma-olc.js, 48 ölçüm)

Rotalar × {1280, 375} × {BASİT, PRO} × {açık, koyu}: cari detay (+ekstre düğmesi), `/cikti/CARI_EKSTRE`, emanet (+belge düğmesi), `/cikti/EMANET_TESLIM` (+Geri → `/emanet`), sipariş detay tamir/atölyede (şerit · usta · makbuz · geri ver), sipariş taslak + kapora çekmecesi.

| Ölçüt | Sonuç (48/48) |
|---|---|
| K1 gizli-görünen · K5 "TRY" · K10 elipsis · K11 hata bandı · K12 dikey düğme · yatay kaydırma | 0 |
| Yeni öğeler görünür (ekstreYazdir, belgeYazdir, şerit, usta, makbuz, geri ver, tutar, kaydet) | 48/48 "gorunur" |
| Kâğıt rengi (açık ve KOYU temada) | rgb(255,255,255) — 16/16 belge ölçümü |
| "Geri" emanet belgesinden | `/emanet` (8/8) |
| K3 kırık 1 | yalnız eski hücreler: Cariler listesi "Usta HasanAtölye" (BASİT), ekstre "Satış · Nakit + Veresiye" — bu paketin öğesi değil |
| K9 binen 5–6 | cari detay **penceresi** ve kapora **çekmecesi** sayfanın üstünde açık (katman örtüşmesi, beklenen) — ör. "dd 'Müşteri siparişi' ⟂ #cekmece-kapat" |

Görüntüler (Read ile bakıldı, 4): `siparis-s3__PRO_karanlik_375` (şerit 375'te iki satıra sarıyor, taşma yok; Kapora kartı), `siparis-s4-kapora__BASIT_aydinlik_375` (çekmece), `emanet__BASIT_aydinlik_375` (Ayrıntı · Belgeyi yazdır · Geri al tek satır), `cikti-emanet__PRO_karanlik_1280` (koyu temada beyaz kâğıt). Tümü: `/private/tmp/kuyumcu-serit/EKSIK-B1-YAZDIR/foto/` · ham ölçüm `olcum-BASITPRO.json`.

## 8. Denetim

`bash denetim.sh` → yalnız **✗ P-30 DEMO_INSAN_SURESI** (görev metninde hariç). İlk koşumda ✗ P-11 `DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47` benim catch içi `kaporaKasalari = []` satırımdandı → kaldırıldı, yeniden koşum: P-11 ✓. `Pages: ModalPencere kullanımı 48 (tavan 55)` (yeni tek pencere = yıkıcı onay) · `P-26 DEFTERE_YAZMAYAN_PARA_MODULU 0` ✓ · `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 0` ✓.

Temizlik: API (PID 41094) ve vekil (PID 44914) `kill`; `lsof` 5166/5167 boş. Kiracılar 01M4FA8NJ1C4GE7F09FTZMZCXH, 01M4FA8QAE5WH4VG0D4VRJW0E6, 01M4FA9RJS5MV1T0TQ1MRFYMFW (kayıt hız sınırına takılan ara denemeler), 01M4FAG0BJZJP299YBP3R7XXW6, 01M4FAG1996GHAZCA72RHVNPN9 `zsh kiraci-temizle.sh <id> --uygula` ile silindi; kendi 12 davet satırım silindi. **tenants 19 → 19**, `PAKETEKSIK%` = 0. QA-TEST kiracılarına dokunulmadı. Geçici sır dosyaları `printf '' >` ile boşaltıldı.

## 9. Açık kalanlar

1. **Siparisler.razor oluşturma formunda usta seçici yok** (UX-S26 kısmi) — usta TASLAK düzenlemesinden girilir. Küçük ek paket önerisi.
2. **Satışa çevirmede kaporanın otomatik düşülmesi** — bugün cari hesap üzerinden ("Açık hesap" bacağı). Satış ekranının ödeme bacağını kaporayla önceden doldurması Satış/Fiş şeridinin (GOC-C-2) alanında — sahibine öneri.
3. **Y-17 avans yolu (`/islemler/avans`) `siparisId` verilince siparişin carisini denetlemiyor** ve MASAK kümülatifine girmiyor (bu paket değiştirmedi — Satış modülü sahibi).
4. (Tur 1 — tur 2 tam süitinde YOK, 0 başarısız) Tam süitte başkasına ait 7 kırmızı: DilSunucu DS02/DS09/DS30 ("Customer not found" — DIL-SUNUCU şeridi, t2) · GOCC2SinirTur3 DG31 ve GocC2Ekran GC2T03 (GOC-C-2, t1) · SATIS3Ekran T17 (Islemler, t1) · Yasal4Http Y4A eşzamanlı 12 düzenleme 500 (eşzamanlılık; Yasal4 sahibi).
5. **MASAK eşiği üstündeki kapora ekrandan alınamıyor (fail-closed, doğrulayıcı DÜŞÜK bulgusu):** canlı 999.999.999,99 HAVALE → 400 "MASAK: … kimlik tespiti zorunludur". Kapora çekmecesinde ve `SiparisKaporaDto`'da MASAK kimlik/beyan alanı yok; kural gevşemiyor, iş akışı kapanıyor, hata bantta görünüyor. Öneri: büyük kapora için MASAK kimlik + beyan alanları ya da tahsilat ekranına yönlendirme (eşik değeri bu paket yazmadı; mevcut MASAK kapısından okunur).
6. **Sır hijyeni notu (Anayasa md.1):** tur 2'de sistem anahtarının kaynağını ararken bir `grep | sed` süzgeci değeri gizleyemedi ve `appsettings.Development.json` satır 35 `"Anahtar"` alanının ilk ~20 karakteri ile `appsettings.json` satır 41'in yer tutucu öneki ("DEGISTIR-…") araç çıktısına düştü. Rapora değer yazılmadı. İlki geliştirme yapılandırmasındaki yedek anahtarıdır; üretimde kullanılıyorsa döndürülmesi önerilir (Mehmet).

## 10. Kurucu / mali müşavir

- Kapora TAHSILAT olarak deftere yazılıyor ve MASAK kümülatifine/tevsik kapısına giriyor (cari tahsilat gövdesi). Kaporanın muhasebede "alınan avans" (340) olarak mı izleneceği mali müşavir sorusudur; bu paket hiçbir mevzuat eşiği yazmadı.

## Değişen dosyalar
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.EksikB1.cs` (yeni)
- `dotnet/Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs` (yalnız satır 183, 438, 812, 813 — bu paketin cümleleri; dosyanın sahibi DIL-SUNUCU şeridi)
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor`, `Cariler.razor`, `Emanet.razor`, `Cikti.razor`
- `dotnet/Kuyumcu.Tests/EksikB1YazdirTestleri.cs` (yeni), `EksikB1YazdirEkranTestleri.cs` (yeni), `P51EkranTestleri.cs` (1 satır), `Dogrulayici/EKSIKB1YAZDIRSinirTestleri.cs` (tur 2: yalnız `Dil_` testinin Skip'i kaldırıldı; dosya doğrulayıcınındır)

**Dokunulan sayfalar:** Cariler.razor, Emanet.razor, SiparisDetay.razor, Cikti.razor.
