# PAKET P-21 — AVANS · SİPARİŞ→SATIŞ · DEĞİŞİM · KONSİNYE · CİRO/RÜCU · REZERV

**Dalga:** DALGA 5 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21
**Kimlikler:** Y-17 · Y-18 · Y-19 · Y-20 · Y-21 · Y-23
**Uygulayıcı:** Taşkın (03 Backend) — Ertuğ (03 Frontend) payı için bkz. § 3 (PLAN çelişkisi)
**Kaynak sözleşme:** `PLAN-TEK-SEFER-2026-09-18.md` satır 488-502 (P-21 bloğu)

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayım yok)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| **P-19** | ✅ VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-19-uygulama-raporu.md` · migration `20260921022537_P19GiderVeVirman` uygulanmış · `Data/HareketFabrikasi.cs` içinde iki-bacaklı desen (`IBacakliHareket`, `BacaklariEsle`) mevcut |
| **P-20** | ✅ VAR | `PAKET-P-20-uygulama-raporu.md` + `PAKET-P-20b-uygulama-raporu.md` · `HURDA_SATIS` ve `TAHLIL_FARKI` türleri `Sabitler.HareketTurleri`'nde |

Kapsam daraltması **gerekmedi**.

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Konu | Durum | Kanıt |
|---|---|---|---|
| **Y-17** | Avans / kapora işlemi | **KAPANDI** | `Modules/Satis/SatisModulu.P21.cs` `AvansAl` / `AvansMahsup` / `AvansIade` / `AcikAvans`; `Data/Ticaret.cs` `AvansKapisi`; uçlar `POST /islemler/avans`, `/avans/mahsup`, `/avans/iade`, `GET /islemler/avans/{cariId}`. Test: `Avans_kasaya_girer_ve_musteri_ALACAKLI_olur`, `Avans_mahsubu_cari_bakiyeyi_CIFT_SAYMAZ`, `Avans_mahsubu_acik_avansi_ASAMAZ`, `Avans_iadesi_kasadan_cikar_ve_borcumuzu_kapatir`. Canlı: C8–C13 (bakiye 7.000 → mahsup → **7.000**, değişmedi) |
| **Y-18** | Sipariş → satış dönüşümü | **KAPANDI** | `SatisModulu.P21.cs` `SiparisiSatisaCevir`; `Hareket.SiparisId` kolonu; uç `POST /siparisler/{id}/satisa-cevir` (`Modules/Siparis/SiparisModulu.cs` SiparisController). Test: `Siparis_satisa_cevrilir_...`, `Yarim_donusum_imkansiz_satis_patlarsa_siparis_HAZIR_kalir`, `Ayni_siparis_IKINCI_kez_satisa_cevrilemez`, `Durum_geri_alinsa_bile_siparis_id_bagi_...`, `Kalem_fiyati_eksikse_donusum_KISMI_yapilmaz`, `Iptal_edilmis_siparis_satisa_cevrilemez`, `Donusumde_rezerv_kapanir_avans_mahsup_edilir_...`. Canlı: C14–C18 |
| **Y-19** | Değişim (ürünü ürünle) | **KAPANDI** (KART farkı bilinçli KAPALI — § 3/S-3) | `SatisModulu.P21.cs` `Degisim`; uç `POST /islemler/degisim`. Test: `Degisim_iki_bacagi_TEK_fiste_yazar_ve_farki_BIR_KEZ_tahsil_eder`, `Degisimden_sonra_ayni_mal_IKINCI_kez_iade_edilemez`, `Degisimde_KART_farki_kapalidir_ve_sebebini_soyler`. Canlı: C19–C21 |
| **Y-20** | Konsinye çıkış / iade | **KAPANDI** | `SatisModulu.P21.cs` `KonsinyeCikis` / `KonsinyeIade` / `KonsinyeAcik`; `Data/Ticaret.cs` `KonsinyeKapisi21`; türler `KONSINYE_CIKIS` / `KONSINYE_IADE`. Test: `Konsinye_cikis_stoktan_duser_ama_TL_ve_cari_bakiyeye_DOKUNMAZ`, `Konsinye_iadesi_stogu_geri_getirir_fazla_iade_REDDEDILIR`, `Rezerve_mal_konsinyeye_CIKARILAMAZ`. Canlı: C22–C25 |
| **Y-21** | EVRAK_CIRO ucu + rücu durumu | **KAPANDI** | `Modules/Evrak/EvrakModulu.cs` `Ciro` / `Rucu`; `Data/KiymetliEvrak.cs` `rucu_tarihi` / `rucu_hareket_id` / `rucu_tutar`; yeni tür `EVRAK_RUCU`; uçlar `POST /evraklar/{id}/ciro`, `/rucu`. Test: `P21CiroRucuTestleri` (7 test). Canlı: C26–C30 — rücu sonrası `izinliGecisler:["TAHSIL_EDILDI"]` (**terminal DEĞİL**) |
| **Y-23** | Rezerv / bloke adet kolonu | **KAPANDI** | `stoklar.bloke_adet` kolonu; `rezervasyonlar` tablosu; `Data/Ticaret.cs` `BlokeKapisi`; uçlar `POST/GET/PATCH /islemler/rezervler`, `POST /islemler/rezervler/{id}/iptal`. Test: 6 test + stok raporu testi. Canlı: C1–C7, C31 |

**Kapsam dışına taşan, DEVREDİLEN kalemler** (gizlenmedi):

| Kalem | Devredildi | Gerekçe |
|---|---|---|
| P-21 uçlarının **ekran düğmeleri** (rezerv/avans/değişim/konsinye/ciro/rücu/satışa-çevir) | **P-22** (ekran borçları paketi) ve Katman 8 UX dilimi | PLAN'ın P-21 "Dosyalar" listesinde **hiçbir `.razor` yoktur** — P-21 bilinçli olarak bir mekanizma/defter paketidir. Ayrıntı ve çelişki kaydı: § 3/S-1 |
| `AVANS_MAHSUP` / `AVANS_IADE` / `KONSINYE_IADE` ters kaydı (storno) | **P-48 storno dilimi** | Ters kayıt avans havuzunu ve satışı birlikte çevirmeyi gerektirir; genel `StornoServisi` bu hattı bilmez. `DuzeltmePolitikasi`'nda **STORNO_YOK** olarak açıkça damgalandı (uydurma uç yazılmadı) |
| Değişimde **KART** farkı | 1.1 / POS dilimi | POS yan defterinin (`pos_tahsilatlari`) iki bacaklı fişte hangi bacağa yazılacağı kararı P-21 kapsamında değil. Kapı **400** döner ve sebebini söyler (UX-S30) |

---

## § 2 — YAPILANLAR

### 2.1 Veri modeli (migration: EVET)

| Değişiklik | Yer |
|---|---|
| `stoklar.bloke_adet decimal(12,3) NOT NULL DEFAULT 0` | `Data/Entities.cs` (Stok) |
| `hareketler.siparis_id varchar(36) NULL` + `IX (tenant_id, siparis_id)` | `Data/Entities.cs` (Hareket) |
| `kiymetli_evraklar.rucu_tarihi` · `rucu_hareket_id` · `rucu_tutar` | `Data/KiymetliEvrak.cs` |
| **YENİ TABLO** `rezervasyonlar` (+4 indeks) | `Data/Ticaret.cs` (`Rezervasyon`) |
| 6 yeni hareket türü: `AVANS_ALINDI` · `AVANS_MAHSUP` · `AVANS_IADE` · `KONSINYE_CIKIS` · `KONSINYE_IADE` · `EVRAK_RUCU` | `Data/Entities.cs` `Sabitler.HareketTurleri` |

**Ayna tablo AÇILMADI** (bilinçli): avans havuzu ve açık konsinye **defterden türetilir**
(`Σ AVANS_ALINDI − Σ AVANS_MAHSUP − Σ AVANS_IADE`, `Σ KONSINYE_CIKIS.adet − Σ KONSINYE_IADE.adet`).
Gerekçe `KiymetliEvrak.cs`'teki K4-B kuralının aynısı: iki kaynak olursa ikisi ayrışır.
Rezervasyon defterden türetilemez (hiç hareket doğurmaz) — **tek yeni tablo odur.**

### 2.2 Kapılar (mekanizma)

| Kapı | Ne zorlar |
|---|---|
| `BlokeKapisi.Satilabilir/SatilabilirDogrula/Degistir/RezerveSatildi` | `0 ≤ bloke_adet ≤ adet`; mal çıkaran **5 yol** (satış · satış fişi · transfer · alış iadesi · takas) satılabilir adedi sorar |
| `TerminalKapisi.Dogrula` | Kapanmış kayıt (sipariş TESLIM_EDILDI/IPTAL, rezervasyon SATILDI/IPTAL) deftere yazdıramaz → **409** |
| `AvansKapisi.MahsupEdilebilir` | Mahsup açık avansı aşamaz; sessiz kırpma yok (md.1) |
| `KonsinyeKapisi21.IadeEdilebilir` | İade açık konsinye adedini aşamaz |
| `hareketler.siparis_id` + `zatenVar` kontrolü | Aynı sipariş ikinci kez satışa çevrilemez → **409** |

### 2.3 Tek transaction (Y-18 DoD)

`SatisFisi` **ortam transaction'ına katılır**: `db.Database.CurrentTransaction is null ? BeginTransaction() : null`.
Böylece dönüşüm satış mantığını **kopyalamadan** tek transaction içinde çağırır
(kopyalansaydı MASAK · tevsik · iskonto · stok kapıları bir gün ayrışırdı).
`await using` korunduğu için istisnada geri alma davranışı **aynen** durur.

---

## § 3 — SAPMALAR ve PLAN ÇELİŞKİLERİ (gizlenmedi)

**S-1 — PLAN ile UX sözleşmesi çelişiyor (PLAN üstün tutuldu).**
UX-S25 "sunucuda uç var, ekranda düğme yok kabul edilmez" der. PLAN'ın P-21 **Dosyalar**
listesinde ise hiçbir `.razor` yoktur (`HareketFabrikasi.cs`, `Entities.cs`, `Siparis.cs`,
`SiparisModulu.cs`, `SatisModulu.cs`, `EvrakModulu.cs`, `Data/Migrations/`, `Kuyumcu.Tests/`).
Göreve verilen kural: *"Plan bloğu ile bu koşullar çelişirse PLAN üstündür ve çelişkiyi
uygulama raporuna yaz."* → **PLAN uygulandı, ekran yazılmadı.** Ek teknik gerekçe: yeni bir
rota açmak `ham/olcum-2026-09-18.json` rota tabanını (46), P-14 menü mimarisini ve
**BK-24 mobil parite ölçümünü (42/42 rota gerçek tarayıcıda)** etkiler; bunlar P-18/P-22'nin
kalemleridir. **Açık kalan:** § 9/A-1.

**S-2 — `Data/HareketFabrikasi.cs` DEĞİŞTİRİLMEDİ.** PLAN dosya listesinde vardı. Ölçüldü:
altı hattın da ihtiyacı olan tür kapısı, adet kapısı, ödeme izi değişmezi ve `MadenKapisi`
çağrısı fabrikada **zaten** var; yeni türler `Sabitler.HareketTurleri`'ne eklenince kapıdan
geçiyor. Gereksiz dokunuş yapılmadı (§1 md.12 "mevcut varlık yeniden yazılmaz" ruhu).
Fabrikanın **davranışı değişmediği** için `HareketFabrikasi` zinciri (P-08→P-19→P-20→P-21→P-23)
bir sonraki ele temiz devredilir.

**S-3 — Değişimde KART farkı KAPALI.** Yukarıda gerekçelendirildi; 400 + sebep döner.

**S-4 — Değişim `StornoServisi`'ne DOKUNMAZ.** Plan riski "değişim storno motoruyla karışırsa
çift iade". Değişim bir **iade + satış**tır, storno değildir; iade bacağı
`iade_kaynak_hareket_id` ile orijinal satışa bağlanır, böylece mevcut `IadeKaynagi` sayacı
değişimi de görür. **Storno taklidi ("ters iade") YAZILMADI** (MÜHÜR 2).

**S-5 — Mevcut bir test hizalandı, SUSTURULMADI.**
`IadeTestleri.Elimizde_olmayan_mal_tedarikciye_iade_edilemez` — kural aynı (400 + yazma yok),
yalnız hata metni `BlokeKapisi` üzerinden üç sayıyı da söyler hâle geldi (UX-S30).
Assertion yeni metne hizalandı ve **ikinci bir iddia eklendi** (`"Alış iadesi"` bağlamı).
Susturulan test: **0**.

**S-6 — `sube-suzgec-siniflandirma.txt`'ye 1 satır eklendi (c sınıfı).** P-21 beş yerde izlenen
stok satırı arıyordu; beşi de tek yardımcıya (`StokSatiriIzlenen`) toplandı, böylece tabloya
**5 değil 1** satır girdi. `GUVENLIK_ELLE_IZOLASYON = 0` korundu.

**S-7 — Cırcır tabanları güncellendi (yalnız gerekçeyle, tavan artırılmadı):**
`VeresiyeKapanisi` 3 → **4** (dördüncü meşru çağrı `EvrakServisi.Ciro` — vadesiz ama borç
**kapatan** pay; kural vadesiz borç *doğurmayı* yakalar) · `BEKLENEN_VARLIK` 60 → **61**
(yeni varlık `Rezervasyon`) ve aynı değer `ham/olcum-2026-09-18.json`'a yazıldı.

**S-8 — Tahmin aşımı.** PLAN 420 ajan-dk. Bu turda harcanan süre ölçülmedi (kronometre
mekanizması bu pakette yok); **uydurma rakam yazılmadı** (md.1). Ölçüm orkestratör kalemidir.

---

## § 4 — SÖZLEŞME-DoD (Bertan mührü, 2026-09-16 12:12)

| Yeni varlık / kayıt türü | PATCH | DELETE / pasife alma | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|
| **`Rezervasyon`** (yeni varlık) | ✅ `PATCH /islemler/rezervler/{id}` — *gönderilmeyen alan korunur, `null` siler*; `adet` null'lanamaz (iptal ucu vardır) | ✅ pasife alma: `POST …/iptal` (gerekçe ≥ 10 karakter). **Hard DELETE YOK** | `[ModulYetkisi("islem")]` + `[RolGerekli]` (oluştur/patch: SAHIP·MUDUR·TEZGAHTAR · iptal: SAHIP·MUDUR) | ✅ otomatik (`DenetimYazmaDinleyicisi`) — canlıda **5 kayıt** ölçüldü | ⛔ **DEVREDİLDİ → P-22** (§ 3/S-1) |
| **`AVANS_ALINDI`** (defter satırı) | ⛔ salt okunur (VUK md.219) | Gerçek düzeltme yolu: `POST /islemler/avans/iade` | `[ModulYetkisi("islem")]` + SAHIP·MUDUR·TEZGAHTAR·MUHASEBE | ✅ otomatik | ⛔ DEVREDİLDİ |
| **`AVANS_MAHSUP` · `AVANS_IADE`** | ⛔ | **STORNO_YOK** — yokluğu `DuzeltmePolitikasi`'nda açıkça damgalı, uydurma uç yazılmadı | aynı | ✅ | ⛔ DEVREDİLDİ |
| **`KONSINYE_CIKIS`** | ⛔ | Gerçek geri alma: `POST /islemler/konsinye-iade` | SAHIP·MUDUR | ✅ | ⛔ DEVREDİLDİ |
| **`KONSINYE_IADE`** | ⛔ | STORNO_YOK (damgalı) | SAHIP·MUDUR | ✅ | ⛔ DEVREDİLDİ |
| **`EVRAK_CIRO`** | ⛔ | Geri alma **artık VAR**: `POST /evraklar/{id}/rucu` (damga StornoYok → **Defter** olarak güncellendi) | `[ModulYetkisi("evrak")]` + SAHIP·MUDUR·MUHASEBE | ✅ `denetim.YazmaKaydet` (mevcut servis deseni) — canlıda **6 KiymetliEvrak kaydı** | ⛔ DEVREDİLDİ |
| **`EVRAK_RUCU`** | ⛔ | `POST /evraklar/{id}/tahsil-edildi` (keşideci sonradan öderse) | aynı | ✅ | ⛔ DEVREDİLDİ |
| **`SATIS` / `SATIS_IADE`** (değişim bacakları) | ⛔ | Mevcut damgalar değişmedi | mevcut | ✅ | mevcut |

**Finansal varlıkta hard DELETE:** hiçbir yerde açılmadı (`DuzeltmePolitikasiTestleri.Finansal_modullerde_HARD_DELETE_ucu_ACILMAMISTIR` yeşil).

---

## § 5 — MIGRATION

| Ölçü | Değer |
|---|---|
| Ad | `20260921064920_P21TicaretRezervSiparisBagiCiroRucu` |
| `.cs` boyutu | **6.215 bayt** (> 0) |
| `.Designer.cs` boyutu | **205.785 bayt** (> 0) |
| `Up()` içeriği | `bloke_adet` · `rucu_hareket_id` · `rucu_tarihi` · `rucu_tutar` · `siparis_id` · `CreateTable rezervasyonlar` · 5 indeks |
| `Down()` | **YAZILDI ve GERÇEKTEN KOŞTURULDU** (BK-21) |
| `--no-build` | **kullanılmadı** (build yeşilken üretildi) |
| Migration `.cs` sayısı | 127 (P-21 ile +2: migration + designer) |
| 0 baytlık migration `.cs` | **8** (artmadı) |

**Down() tatbikatı (BK-21 — "yazıldı yeterli değil"):**

```
dotnet ef database update P20bTahlilFarki  → Reverting '…P21TicaretRezervSiparisBagiCiroRucu'. Done.
  __EFMigrationsHistory = 58 · SHOW TABLES LIKE 'rezervasyonlar' → (boş) · SHOW COLUMNS stoklar LIKE 'bloke_adet' → (boş)
dotnet ef database update                  → Applying '…P21TicaretRezervSiparisBagiCiroRucu'. Done.
  __EFMigrationsHistory = 59 · rezervasyonlar tablosu geri geldi (0 satır)
```

`kuyumcu_net` uygulama kanıtı: `SELECT COUNT(*) FROM __EFMigrationsHistory` → **59** ·
`SHOW COLUMNS FROM stoklar LIKE 'bloke_adet'` → `decimal(12,3) NO  0.000` ·
`SHOW COLUMNS FROM hareketler LIKE 'siparis_id'` → `varchar(36) YES NULL`.

---

## § 6 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Yalnız P-21 filtresi | 31 | 31 | 0 | 0 |
| `Duzeltme|Iade|P21` filtresi | 148 | 148 | 0 | 0 |
| **TAM SÜİT** (`--logger trx`) | **3515** | **3515** | **0** | **0** |

`trx`: `…/scratchpad/trx-pP-21/*.trx` → `total="3515" executed="3515" passed="3515" failed="0"`.

**Yeni test sayısı: 31** (ölçüldü: `grep -c "\[Fact\]"` → `P21TicaretTestleri.cs` 24 +
`P21CiroRucuTestleri.cs` 7). Tam süit önce (aritmetik) 3484 → sonra **3515**.

**Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı**.

---

## § 7 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P21/` altına kopyalandı → `perl -0777 -pi -e` ile
**tek eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile
birebir aynı** olduğu kanıtlandı. Son mutasyondan sonra **TEMİZ `dotnet build`** (0/0).

| Kod | Değişiklik | KIRMIZIYA DÖNEN test(ler) | Geri alındı |
|---|---|---|---|
| **M21-01** | `BlokeKapisi.Satilabilir` blokeyi yok saysın (`Adet - BlokeAdet` → `Adet`) | `Rezervasyon_blokeyi_artirir_ADEDI_DEGISTIRMEZ` · `Rezerve_mal_SATILAMAZ_satilabilir_stok_kapisi_calisir` · `Rezerve_mal_konsinyeye_CIKARILAMAZ` (3) | `cmp AYNI` |
| **M21-02** | `BlokeKapisi.Degistir` tavan kontrolü kalksın (`if (yeni > stok.Adet)` → `if (false)`) | `Bloke_adet_stok_adedini_ASAMAZ_BLOKE_TUTARSIZ_sifir` | `cmp AYNI` |
| **M21-03** | `KonsinyeKapisi21.IadeEdilebilir` fazla-iade kontrolü kalksın | `Konsinye_iadesi_stogu_geri_getirir_fazla_iade_REDDEDILIR` | `cmp AYNI` |
| **M21-04** | `AvansMahsup` cari bakiyeyi de düşsün (**çifte sayma**) | `Avans_mahsubu_cari_bakiyeyi_CIFT_SAYMAZ` | `cmp AYNI` |
| **M21-05** | Çift dönüşüm kapısı kalksın (`if (zatenVar)` → `if (false)`) | `Durum_geri_alinsa_bile_siparis_id_bagi_ikinci_donusumu_reddeder` | `cmp AYNI` |
| **M21-06** | Dönüşümde `TerminalKapisi.Dogrula` kalksın | `Iptal_edilmis_siparis_satisa_cevrilemez` · `Ayni_siparis_IKINCI_kez_satisa_cevrilemez` (2) | `cmp AYNI` |
| **M21-07** | Değişimin iade bacağı kaynağa bağlanmasın (`IadeKaynakHareketId = null`) | `Degisim_iki_bacagi_TEK_fiste_yazar_...` · `Degisimden_sonra_ayni_mal_IKINCI_kez_iade_edilemez` (2) | `cmp AYNI` |
| **M21-08** | `EvrakGecisleri`'nde `CiroEdildi` **TERMİNAL** yapılsın (`= []`) | `Rucu_IKI_borc_dogurur_ve_iki_ayri_cariye_yazar` · `Rucu_sonrasi_durum_TERMINAL_DEGILDIR_tahsil_edilebilir` (2) **ve ayrıca `denetim.sh` sayacı `TERMINAL_SONRASI_YAZAN_YOL = 1` ile KIRMIZI** | `cmp AYNI` |

**Test açığı bulunan mutasyon: 0** (hepsi en az bir testi adıyla kırdı).
M21-08 ayrıca yeni cırcır sayacının **gerçek bir kapı** olduğunu kanıtlar (BK-05 ruhu).

---

## § 8 — DENETİM (`dotnet/denetim.sh`)

### 8.1 Bu paketin eklediği sayaçlar (blok `# >>> P-21` … `# <<< P-21`)

| Sayaç | Sonuç | Bileşenler | Taban |
|---|---|---|---|
| `P-21: BLOKE_TUTARSIZ` | **✓ 0** | (a) `.BlokeAdet` ataması `BlokeKapisi` dışında = 0 · (b) mal çıkaran yolda ham `.Adet < dto|k.` karşılaştırması = 0 · (c) `SatilabilirDogrula` çağrısı ≥ 5 (**ölçülen 5**) | **TAZE ÖLÇÜLDÜ 2026-09-21**, belgeden kopyalanmadı; ölçüm komutları betikte yorum olarak yazılı (BK-07) |
| `P-21: TERMINAL_SONRASI_YAZAN_YOL` | **✓ 0** | (a) rezervasyon durumunu yazan her dosyada `TerminalKapisi.Dogrula` · (b) dönüşümde `SiparisDurumu.Terminal` kapısı · (c) `siparis_id` çift dönüşüm kapısı · (d) `CiroEdildi` çıkışı **boş olmamalı** | aynı |

Tavan/eşik **artırılmadı**; yalnız iki cırcır tabanı gerekçeyle güncellendi (§ 3/S-7).

### 8.2 Tam denetim çıktısı

`./denetim.sh` → **1 adet ✗ kaldı ve BU PAKETİN İŞİ DEĞİLDİR** (md.1 gereği gizlenmiyor):

```
✗ P-02: QA_TUR_SURESI_OLCUM (BK-39)  devir turu 6 sonrası tur 7 KOŞTU ama süre ölçümü YAZILMADI
```

**Bu paketin sebep olmadığının kanıtı:** sayaç `ham/qa-tur-suresi.json` (son değişiklik
**21 Eylül 06:49**) ile `QA-TARAYICI-TURU-2026-09-16-tur7.md` (**21 Eylül 08:51**) tarihlerini
karşılaştırır; ikisi de bu paket **başlamadan önce** (ilk dosya yazımı 09:47) bugünkü hâlindeydi
ve P-21 `dokumanlar/09-uygulama-kayitlari/ham/` altında **yalnız `olcum-2026-09-18.json`**
dosyasına dokundu (`BEKLENEN_VARLIK` 60→61). Aynı bulgu **P-20b raporunda da** aynı
gerekçeyle eskale edilmişti (§ 10). Ölçümü yazacak merci **orkestratör/QA**'dır; uydurma bir
süre yazmak Madde 1 ihlali olurdu.

**Bu paketin başlangıcında kırılan ve bu pakette KAPATILAN denetim bulguları** (hepsi P-21'in
kendi yan etkisiydi): `VeresiyeKapanisi` (3→4) · `IKiraciVarlik uygulayan varlık` (60→61) ·
`P-02: OLCUM_TABANI_KOPYA` (json güncellendi) · `P-04: GUVENLIK_ELLE_IZOLASYON` (5→0, tek
yardımcıya toplandı + tabloya 1 satır).

---

## § 9 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` (nohup, **PID 18716**, iş sonunda `kill`).
Kiracı gerçek uçla açıldı: `kiraci_davetleri`ne SHA-256 özetli satır + `POST /auth/kayit` →
**201**, `PAKETP-21-TEST`.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C1 | `POST /islemler/rezervler` 3 adet (stok 10) | 201, bloke 3, satılabilir 7 | **201** · `stokAdet:10.000` `blokeAdet:3.000` `satilabilirAdet:7.000` |
| C2 | Aynı üründe 12 adet daha rezerv | 400 (bloke > adet) | **400** "Bloke adet stok adedini aşamaz: stok 10,000, istenen bloke 15,000. Satılabilir: 7,000 adet." |
| C3 | `POST /islemler/satis` 8 adet | 400 (satılabilir 7) | **400** "Satış: yetersiz satılabilir stok. Stok 10,000 adet, rezerve 3,000 adet, satılabilir 7,000 adet; talep 8 adet." |
| C4 | `PATCH` yalnız `aciklama`, sonra yalnız `adet:2` | 200; gönderilmeyen alan KORUNUR; bloke 2'ye iner | **200** · 1. istek `aciklama:"Nişan için ayrıldı"`; 2. istek `adet:2` + `aciklama` **hâlâ dolu** · `blokeAdet:2.000` `satilabilirAdet:8.000` |
| C5 | `POST …/iptal` gerekçe "kısa" | 400 alan hatası | **400** `{"alanlar":{"gerekce":["İptal gerekçesi en az 10 karakter olmalıdır"]}}` |
| C6 | `POST …/iptal` geçerli gerekçe | 200; bloke 0 | **200** · `durum:"IPTAL"` `blokeAdet:0.000` `satilabilirAdet:10.000` |
| C7 | İkinci iptal | **409** | **409** "Rezervasyon 'IPTAL' durumunda kapanmıştır; iptal yapılamaz…" |
| C8 | `POST /islemler/avans` 5.000 NAKIT | 201; havuz 5.000 | **201** · `acikAvans:5000.00` |
| C9 | `POST /islemler/satis` 12.000 VERESIYE | 200 | **200** · `tlTutari:12000.00` |
| C10 | `GET /cari/{id}` (mahsup ÖNCESİ) | 7.000 (−5.000 avans + 12.000) | **`tlBakiye: 7000.0`** |
| C11 | `POST /islemler/avans/mahsup` 5.000 | 200; havuz 0 | **200** · `mahsupTutar:5000.00` `acikAvans:0.00` |
| C12 | `GET /cari/{id}` (mahsup SONRASI) | **DEĞİŞMEMELİ** → 7.000 | **`tlBakiye: 7000.0`** ✔ **ÇİFTE SAYMA YOK** |
| C13 | Tekrar mahsup 1.000 | 400 | **400** "Bu carinin açık avansı yok; mahsup yapılamaz." |
| C14 | `POST /siparisler` | 201 | **201** · `S-2026-000001` · `TASLAK` |
| C15 | TASLAK siparişi satışa çevir | 400 | **400** "Sipariş 'TASLAK' durumundan satışa çevrilemez; önce HAZIR durumuna getirin." |
| C16 | ONAYLANDI → HAZIR | 200 / 200 | **200 · 200** |
| C17 | `POST /siparisler/{id}/satisa-cevir` | 201; TESLIM_EDILDI + fiş | **201** · `yeniDurum:"TESLIM_EDILDI"` `fisId` + `satisHareketId` dolu · `kalemSayisi:1` |
| C18 | İkinci dönüşüm | **409** | **409** "Sipariş 'TESLIM_EDILDI' durumunda kapanmıştır; satışa çevirme yapılamaz…" |
| C19 | `POST /islemler/degisim` fark **KART** | 400 + sebep | **400** `{"alanlar":{"farkOdemeTuru":["Değişim farkı bu sürümde NAKIT ya da VERESIYE ile kapanır"]}}` |
| C20 | Değişim: 12.000 geri / 15.000 yeni, NAKIT fark | 201; fark 3.000 tek kez | **201** · `fisId` · `iadeHareketId` · `satisHareketId` · `farkTl:3000` · `farkYonu:"MUSTERI_ODER"` · `kalanIadeHakki:0.000` |
| C21 | Değişim sonrası aynı satışa `satis-iade` | 400 (**çift iade yok**) | **400** "Bu işlemin tamamı zaten iade edilmiş" |
| C22 | `POST /islemler/konsinye-cikis` 4 adet | 201; stok 8→4, TL 0 | **`adet:4` `stokAdet:4.000`** · SQL: `konsinye_tl_toplam 0.00` |
| C23 | `GET /islemler/konsinye-acik` | 200; açık 4 | **200** · `acikAdet:4.000` · `limit:50` `sinirAsildi:false` (UX-S29) |
| C24 | `POST /islemler/konsinye-iade` 3 | 200; kalan 1 | **200** · `kalanAcikAdet:1.000` |
| C25 | 2 adet daha iade | 400 | **400** "İade adedi açık konsinye adedini aşıyor: açık 1,000, istenen 2." |
| C26 | `POST /evraklar` 10.000 çek | 201 | **PORTFOYDE** |
| C27 | Kaynak cariye geri **ciro** | 400 | **400** "Evrak, onu size veren cariye ciro edilemez. Geri vermek istiyorsanız storno yolunu kullanın." |
| C28 | `POST /evraklar/{id}/ciro` (tedarikçi) | 200; CIRO_EDILDI; ona borcumuz kapanır | **200** · `durum:"CIRO_EDILDI"` · `izinliGecisler:["KARSILIKSIZ"]` (**terminal DEĞİL**) |
| C29 | `POST …/rucu` gerekçe "kısa" | 400 | **400** `{"alanlar":{"aciklama":["Rücu gerekçesi en az 20 karakter olmalıdır"]}}` |
| C30 | `POST …/rucu` geçerli | 200; KARSILIKSIZ; **iki borç** | **200** · `durum:"KARSILIKSIZ"` · `izinliGecisler:["TAHSIL_EDILDI"]` ✔ **RÜCU TERMİNAL DEĞİL** |
| C31 | `GET /raporlar/stok-degeri` (2 adet rezervliyken) | bloke AYRI sütun | **`adet=7.0 · bloke=2.0 · satilabilir=5.0` · `toplamBlokeAdet=2.0`** |
| C32 | `GET /islemler/avans/{cariId}` | 200 | **200** · `acikAvans:0.00` |

### SQL kanıtı (`docker exec kuyumcu-mysql …`, parola değişkenden, ekrana basılmadı)

```
hareket_turleri            AVANS_ALINDI:1 | AVANS_MAHSUP:1 | EVRAK_CIRO:1 | EVRAK_GIRIS:1 |
                           EVRAK_KARSILIKSIZ:1 | EVRAK_RUCU:1 | KONSINYE_CIKIS:1 |
                           KONSINYE_IADE:1 | SATIS:3 | SATIS_IADE:1 | SAYIM_DUZELTME:1
BLOKE_TUTARSIZ_satir       0          (bloke_adet < 0 VEYA bloke_adet > adet olan satır YOK)
stok_adet_bloke            7.000 / 2.000
siparis_id_damgali_hareket 1          (Y-18 bağı deftere düştü)
siparis_durum              TESLIM_EDILDI
avans_havuzu               0.00       (5.000 alındı − 5.000 mahsup)
avans_mahsup_nakit         0.00       (mahsup PARA HAREKETİ DEĞİL — tevsik/MASAK çifte saymaz)
degisim_fis_bacak          2          (tek fis_id altında iade + satış)
konsinye_tl_toplam         0.00       (konsinye SATIŞ DEĞİLDİR)
evrak_durum                KARSILIKSIZ
evrak_rucu_dolu            10000.00 / dolu
rezervasyon_durumlari      AKTIF,IPTAL
maden_bos                  0          (has bacağı olan madensiz satır yok — K20-1)
denetim_Rezervasyon        5          (otomatik denetim izi yazıldı)
denetim_KiymetliEvrak      6          (ciro + rücu izleri)
__EFMigrationsHistory      59
```

### Temizlik (BK-25)

`information_schema`tan `tenant_id` kolonu olan **60 tablodan** `DELETE`, ardından davet satırı
ve `tenants` kaydı silindi. Jeton dosyaları `printf '' >` ile boşaltıldı.

| Ölçü | Sonuç |
|---|---|
| `tenants` | **8 → 9 → 8** (başlangıç değerine döndü) |
| `PAKET%` önekli kalıntı kiracı | **0** |
| Kalıntı `rezervasyonlar` / `hareketler` satırı (test kiracısı) | **0 / 0** |
| `QA-TEST-%` kiracı | **3** — **DOKUNULMADI**, verisine hiç girilmedi |
| `lsof -nP -iTCP:5198 -sTCP:LISTEN` | **boş** |
| `lsof -nP -iTCP:5199 -sTCP:LISTEN` | **hâlâ dinliyor** (kurucunun sunucusu — dokunulmadı) |

---

## § 10 — DEĞİŞEN DOSYALAR

> Depoda yalnız tek bir `first commit` olduğu için `git status --porcelain` neredeyse tüm ağacı
> `??` gösteriyor; aşağıdaki liste **bu paketin fiilen yazdığı/değiştirdiği** dosyalardır.

**Yeni:**
```
dotnet/Kuyumcu.Api/Data/Ticaret.cs
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260921064920_P21TicaretRezervSiparisBagiCiroRucu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260921064920_P21TicaretRezervSiparisBagiCiroRucu.Designer.cs
dotnet/Kuyumcu.Tests/P21TicaretTestleri.cs
dotnet/Kuyumcu.Tests/P21CiroRucuTestleri.cs
dokumanlar/09-uygulama-kayitlari/PAKET-P-21-uygulama-raporu.md
```

**Değişen:**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                       (Stok.BlokeAdet · Hareket.SiparisId · 6 yeni tür)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs               (DbSet<Rezervasyon> + 5 indeks)
dotnet/Kuyumcu.Api/Data/KiymetliEvrak.cs                  (rücu alanları + ciro yorumu)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs   (EF üretti)
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs           (partial · 5 satılabilir kapısı · SatisFisi ortam tx)
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs       (satisa-cevir ucu + SatisServisi enjeksiyonu)
dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs           (CiroDto · RucuDto · Ciro · Rucu + 2 uç)
dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs           (stok raporunda blokeAdet/satilabilirAdet/toplamBlokeAdet)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (Rezervasyon + 7 tür damgası)
dotnet/Kuyumcu.Tests/IadeTestleri.cs                      (yeni hata metnine hizalama — § 3/S-5)
dotnet/denetim.sh                                         (P-21 sayaç bloğu + 2 cırcır tabanı)
dotnet/sube-suzgec-siniflandirma.txt                      (+1 satır, c sınıfı)
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json (BEKLENEN_VARLIK 60→61)
```

**Dokunulmayanlar (bilerek):** `MIMARI-KALAN-PAKETLER-2026-09-15.md` · `PLAN-TEK-SEFER-2026-09-18.md` ·
`SONRAKI-OTURUM-PROMPT.md` · `Data/HareketFabrikasi.cs` (§ 3/S-2) · hiçbir `.razor` (§ 3/S-1).

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Sipariş → satış **tek transaction**, yarım dönüşüm imkânsız | ✅ | § 2.3 · test `Yarim_donusum_imkansiz_...` · M21-06 |
| Avans mahsubu cari bakiyeyi **çift saymaz** | ✅ | test `Avans_mahsubu_cari_bakiyeyi_CIFT_SAYMAZ` · M21-04 · canlı C10/C12 (7.000 → 7.000) |
| Bloke adet satılabilir stoktan düşer | ✅ | `BlokeKapisi.SatilabilirDogrula` × 5 yol · canlı C3 |
| Sayım/stok raporu blokeyi **ayrı** gösterir | ✅ | `RaporModulu` `blokeAdet`+`satilabilirAdet`+`toplamBlokeAdet` · test `Stok_raporu_blokeyi_AYRI_gosterir` · canlı C31 |
| `stok.adet < bloke_adet` testte **0** | ✅ | test `Bloke_adet_stok_adedini_ASAMAZ_...` · SQL `BLOKE_TUTARSIZ_satir 0` |
| Ciro edilen evrakta **rücu durumu terminal değildir** | ✅ | `EvrakGecisleri[CiroEdildi] = [Karsiliksiz]` · test `Rucu_sonrasi_durum_TERMINAL_DEGILDIR_...` · M21-08 · canlı C28/C30 |
| 1.1 için yer bırakıldı: taksit (Y-24) ve emanet parça kimliği (Y-22) engellenmedi | ✅ | Hiçbir taksit/emanet alanı tüketilmedi; `Hareket`'e yalnız `siparis_id` eklendi, `Siparis.EmanetHareketId` dokunulmadan kaldı |
| Sayaç `TERMINAL_SONRASI_YAZAN_YOL = 0` | ✅ | § 8.1 |
| Sayaç `BLOKE_TUTARSIZ = 0` | ✅ | § 8.1 |
| Sayaçlar denetim.sh'a **cırcır** olarak eklendi, taban taze ölçüldü (BK-07) | ✅ | § 8.1 · ölçüm komutları betikte yorum |
| Her iş kuralı için hatayı yeniden üreten test | ✅ | 31 yeni test |
| Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | ✅ | § 7 (8 mutasyon, 0 test açığı) |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | ✅ | § 9 (32 adım) |
| `dotnet/denetim.sh` TEMİZ | ⚠️ **KISMEN** | P-21 sayaçları ✓; kalan tek ✗ **P-02/BK-39 QA tur süresi** — bu paketin işi değil, tarih kanıtıyla eskale (§ 8.2) |
| Tam süit 0 başarısız | ✅ | 3515/3515 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 9 Temizlik |
| Migration `Down()` gerçekten koşturuldu (BK-21) | ✅ | § 5 |
| COMMIT YOK / `rm`-`mv` yok / sır basılmadı | ✅ | Hiç `git` yazma komutu çalıştırılmadı; parola daima `$P` değişkeninden |
| SÖZLEŞME-DoD bölümü | ✅ | § 4 |
| DoD EK 1 (her düğme gerçek istek atar) · DoD EK 2 (tek dokunuş) | **UYGULANAMADI — konusuz** | Bu pakette **hiçbir `.razor` açılmadı** (§ 3/S-1). "Dokunulan sayfalar" listesi: **BOŞ** |

---

## § 12 — AÇIK KALANLAR

| # | Kalem | Devredildiği yer |
|---|---|---|
| **A-1** | P-21 uçlarının **ekranları**: rezervasyon listesi/aç/düzenle/iptal · avans al/mahsup/iade · değişim · konsinye çıkış/iade · evrak ciro/rücu düğmeleri · sipariş kartında "satışa çevir". Sunucu tarafı **hazır ve canlı kanıtlı**; UX-S25 ancak ekran gelince sağlanır. | **P-22** (`Siparisler.razor`, satış/POS ekran borçları) + Katman 8 UX dilimi. Yeni rota açılacaksa **BK-24 mobil parite** ölçümü ve `olcum-…json` rota tabanı birlikte güncellenmeli |
| **A-2** | `AVANS_MAHSUP` / `AVANS_IADE` / `KONSINYE_IADE` için ters kayıt (storno) yolu | **P-48** storno dilimi (bugün `DuzeltmePolitikasi`'nda STORNO_YOK damgalı) |
| **A-3** | Değişimde **KART** farkı (POS yan defteri kararı) | 1.1 / POS dilimi |
| **A-4** | Kısmi rücu (evrakın bir kısmı için rücu) | 1.1 — bugün daima tam tutar; `rucu_tutar` kolonu ileriyi engellemiyor |
| **A-5** | `P-02: QA_TUR_SURESI_OLCUM (BK-39)` denetim kırmızısı | **Orkestratör / QA** — P-20b'de de aynı gerekçeyle eskale edildi |
| **A-6** | Kalıntı dosyalar: `dotnet/Kuyumcu.Tests/P21CiroRucuTestleri.cs.bak` (**bu paket üretti**, `printf '' >` ile boşaltıldı) · `dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj` · `dotnet/denetim.sh.mutbak` · `Kuyumcu.Tests/AramaHttpTestleri.cs.bak` · `MadenDefteriTestleri.cs.bak` (bu paketten **ÖNCE** vardı) | **Kurucu** — TurkReport hook `rm`'i bloklar; silme K-4 kalemidir |

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **RÜCU ve TTK md.724 (mali müşavire).** Ciro edilen çek karşılıksız çıkınca uygulanan model:
   (a) ciro ettiğimiz cariye olan borcumuz **tam tutarıyla** geri doğar (`EVRAK_RUCU`),
   (b) keşidecinin bize olan borcu **tam tutarıyla** geri doğar (`EVRAK_KARSILIKSIZ`, yeni vadeyle).
   **Protesto masrafı, faiz ve komisyon YAZILMAZ** — bunlar mevzuata bağlı kalemlerdir,
   koda sabit yazılmadı. Teyit isteniyor: bu iki satırlı model yeterli mi, yoksa masraf/faiz
   ayrı bir kalem olarak mı istenir?
2. **AVANS ve KDV (mali müşavire).** Avans tahsili bu sürümde **yalnız bir tahsilat**tır;
   KDV doğurucu olay olarak işlenmez (fatura kesilmez, matrah oluşmaz). Avans faturası
   uygulaması isteniyorsa ayrı bir kalem olarak açılmalıdır — **varsayım yapılmadı**.
3. **AVANS ve 459 s. VUK tevsik (mali müşavire).** Nakit avans, gerçek bir nakit tahsilat
   sayılarak tevsik kümülatifine **dâhil edildi**; mahsup satırı ise para hareketi olmadığı
   için kümülatife **girmez** (aksi hâlde aynı para iki kez sayılırdı). Bu yorum teyide açıktır.
4. **KONSİNYE ÇIKIŞ ve mülkiyet (kurucuya).** `KONSINYE_CIKIS` **satış değildir**: TL bacağı 0,
   cari bakiye değişmez, mal stoktan çıkar. Karşı taraf malı satarsa ayrı bir **satış** işlemi
   girilmelidir; "konsinye satıldı" diye otomatik satış üretilmez (varsayım yapılmadı).
   Otomatik dönüşüm isteniyorsa yeni bir kalem açılması gerekir.
5. **Ekran eksikliği (kurucuya).** § 3/S-1 ve § 12/A-1: altı yeni yetenek **sunucuda çalışıyor**
   ama **ekranda düğmesi yok**. Ürün bu hâliyle pilota sunulursa bu yetenekler kullanıcıya
   görünmez. Kapı kararı: PLAN'ın dosya sözleşmesi uygulandı; ekran dilimi P-22'ye yazıldı.
