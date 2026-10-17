# Mihenk otomasyon mimarisi — bağımsız tasarım (Fable 5.1, 17.10.2026)

> Salt okuma turu: hiçbir dosya değişmedi, sunucu başlatılmadı. Okunanlar: araştırma belgesi (tamamı), müşteri görüşmesi + EK'ler, paket planı, ADR-0004/0012/0018, `HareketFabrikasi`, `OdemeKalemFabrikasi`, `GelenOdemeModulu`, `CariEslestirici` (+`DedupeAnahtari`), `BankaModulu` (`HareketEkleIc`, `FaturayaDonustur`, `DekontEslestir` başlığı), `EkstreModulu`/`BankaEkstresi` başlıkları, `StornoServisi` başlığı ve açık metotları, `MasakKapisi` (`GunlukIslemToplam`), `YasalParametre`, `UrunKuraliModulu`, `VadeKapisi.AcikBorclar`, `IZamanlanmisIs`, `IBildirimKanali`, `IKurAdaptoru`, `SatisBelgeAyari`, `BankaHareket`/`Fatura`/`CariBakiye`/`Bildirim` varlıkları, `GelenOdemeKarari.razor`. Mevcut "Karar Merkezi" taslağı **en son** okundu (§ 12).
> İşaret: **[D]** = koddan/belgeden doğrulanmadı, tasarım varsayımı değil açık soru.

---

## 0. Bir cümlede

Her dış olay (para geldi, ekstre satırı, POS yatması, sipariş teslimi) **tek bir karar defterine** düşer; **politika** o kararın kademesini (OTO / ÖNERİ / SOR) belirler; **eylem** yalnız mevcut tek kapılardan geçer; **geri alma** yalnız storno'dur; mevzuatın insana bıraktığı konular **kodla** kilitlidir, ayarla açılamaz.

## 1. Kavramlar ve sınırlar

| Kavram | Tanım | Nerede durur |
|---|---|---|
| **Olay** | Dış dünyadan ya da defterden gelen, kendi başına bir şey yaptırmayan gerçek ("X IBAN'dan 12.500 TL geldi"). Olay **değiştirilmez**; yalnız bir kez kabul edilir. | Mevcut satırlar: `banka_hareketleri`, `ekstre_satirlari`, POS yatması, sipariş durumu. Yeni tablo gerekmez; olay = kaynak satırın kimliği. |
| **Konu** | Olayın doğurduğu karar sorusu. Kapalı liste: `MUSTERI_BAGLA`, `BORC_DUS_TL`, `BORC_DUS_GRAM`, `SIPARIS_KAPAT`, `AMAC_BELIRLE` (avans/satış dışı), `FATURA_KES`, `BELGE_TURU`, `MASAK_KIMLIK`, `CARI_BIRLESTIR`. Tek olay birden çok konu açabilir (sıralı). | `KararKonusu` sabitleri (koda yazılı, `Sabitler.HareketTurleri` gibi kapalı liste; `HareketFabrikasi`'nın tür kapısı gibi 400 ile reddedilir). |
| **Değerlendirme** | Saf fonksiyon: (olay, bağlam, politika) → aday eylemler + neden etiketi + önerilen kademe. **Veritabanına yazmaz.** | `Modules/Otomasyon/Cozuculer/*` — `CariEslestirici` deseni (saf sınıf, birim test). |
| **Politika** | Kiracının konu başına seçimi: kademe (OTO/ÖNERİ/SOR), tutar tavanı, hesap başına aç/kapa, ilk N işlem ÖNERİ. **Sürümlü, append-only.** | `otomasyon_politikalari` (§ 2). |
| **Kilit** | Mevzuatın ya da defter bütünlüğünün insana bıraktığı konu. Politika OTO'ya **çevrilemez**; API 400, ekranda gri. | `KilitliKonular` kod sabiti + test + `denetim.sh` sayacı. |
| **Karar** | Konunun bir olay için yazılı sonucu: kademe, seçenekler, önerilen, seçilen, kim, ne zaman, hangi politika sürümüyle, doğan defter satırı. | `kararlar` (§ 2). |
| **Eylem** | Kararın defterdeki karşılığı. Karar Merkezi **kendisi `Add` yapmaz**; `HareketFabrikasi.Ekle`, `BankaServisi.HareketEkleIc`, `FaturaSeriKapisi.Tuket`, `CariServisi.OlusturId`, emanet/sipariş servisleri çağrılır. | `KararUygulayici` (tek sınıf; `denetim.sh`: `Otomasyon/` altında `Hareketler.Add`/`BankaHareketleri.Add`/`Faturalar.Add` = 0). |
| **Geri alma** | Eylem hareketinin stornosu (`StornoServisi.Storno` — TAHSILAT beyaz listede, doğrulandı) + kararın `GERI_ALINDI`'ya geçmesi + konunun **yeniden açılması**. Silme yok (ADR-0004). | `KararUygulayici.GeriAl`. |

Sınır kuralları:
- **Tek yön:** Olay → Karar → Eylem. Eylem olayı değiştirmez (banka satırının tutarı/yönü/tarihi hiçbir karardan etkilenmez; ADR-0004 ruhu, `BankaHareket.StornoKaynakId` açıklamasıyla uyumlu).
- **Karar defteri ≠ denetim izi.** `DenetimServisi.YazmaKaydet` yine yazılır (bugün M7a yazıyor); `kararlar` iş kaydıdır, denetim izi yasal kayıttır. İkisi birbirinin yerine geçmez.
- **Karar Merkezi MASAK/tevsik kapısı değildir.** Kapılar tek kapıların içindedir (`HareketEkleIc` MASAK faz 1-2; doğrulandı). Otomasyon bir kapıya takılırsa karar `ENGELLENDI(sebep)` olur, insana düşer; kapı asla "otomatik" diye gevşetilmez.

## 2. Veri modeli

### 2.1 `kararlar` (kiracı varlığı, `IKiraciVarlik`, HasQueryFilter + elle tenant şartı — ADR-0010)

| Alan | Tip | Not |
|---|---|---|
| `id` | char(36) ULID | |
| `tenant_id` | | |
| `konu` | varchar(30) | kapalı liste |
| `kaynak_tur` | varchar(20) | `BANKA_HAREKET` · `EKSTRE_SATIR` · `POS_YATMA` · `SIPARIS` (`MasakKaynakTuru` deseni: zorunlu, varsayılansız) |
| `kaynak_id` | char(36) | ADR-0009: yabancı anahtar yok |
| `tur_no` | int | aynı (konu, kaynak) için kaçıncı açılış — geri almadan sonra yeniden açılınca +1 |
| `kademe` | `OTO`/`ONERI`/`SOR` | değerlendirme anında politika sürümünden |
| `politika_id`, `politika_surum` | | "o gün neden otomatik yapıldı"nın cevabı |
| `neden_etiketi` | varchar(20) | `KESIN_ANAHTAR` (TCKN/VKN birebir) · `OGRENILMIS_IBAN` · `KURAL:{id}` · `TEK_BORC_TAM_TUTAR` · `SIPARIS_ESLESTI` · `ADAY_YOK` … (sözlükten çevrilir, ham kod basılmaz) |
| `secenekler_json` | text | eylem adıyla sıralı seçenekler + her birinin özet etkisi (ör. "Borçtan düş → kalan borç 0,00") |
| `onerilen` | varchar(30) | ÖNERİ'de önceden seçili; **SOR'da NULL** (hiçbiri önceden seçilmez) |
| `secilen` | varchar(30) | |
| `durum` | | `BEKLIYOR` · `UYGULANDI` · `GERI_ALINDI` · `ENGELLENDI` · `GECERSIZ` (kaynak stornolandı) |
| `karar_veren` | `SISTEM` ya da kullanıcı id | |
| `karar_ani`, `olusturuldu` | UTC | |
| `eylem_hareket_id`, `eylem_tablo` | | doğan satır (hareketler / faturalar / cariler) |
| `geri_alma_hareket_id` | | storno satırı |
| `engel_sebebi` | varchar(300) | kapı mesajı (MASAK kimlik, tevsik, gün kilidi) |

**Tekillik:** `UNIQUE(tenant_id, konu, kaynak_tur, kaynak_id, tur_no)`. Aynı olay için aynı konu iki kez açılamaz; iki eşzamanlı değerlendirme ikinci `INSERT`'te 1062 alır ve `MukerrerMi` kalıbıyla 409'a döner (mevcut desen, `BankaModulu.MukerrerMi`).

**Append-only değil, durum makineli:** `BEKLIYOR → UYGULANDI → GERI_ALINDI`; `BEKLIYOR → ENGELLENDI → BEKLIYOR` (engel kalkınca yeniden değerlendirme); `* → GECERSIZ` yalnız kaynak satır stornolanınca. `secilen`, `karar_veren`, `karar_ani`, `eylem_*` yalnız BEKLIYOR → UYGULANDI geçişinde bir kez yazılır (dinleyiciyle zorlanır, `DenetimYazmaDinleyicisi` deseni).

### 2.2 `otomasyon_politikalari` (kiracı varlığı, append-only sürümlü)

| Alan | Not |
|---|---|
| `konu` | kapalı liste |
| `kapsam_tur`, `kapsam_id` | `KIRACI` (NULL) · `BANKA_HESABI` · `CARI` (ör. "Toptancı X için OTO") |
| `kademe` | OTO/ONERI/SOR; **kilitli konuda OTO yazılamaz (400)** |
| `tutar_tavani_tl` | üstü daima ÖNERİ/SOR'a düşer; NULL = tavan yok; varsayılan tavan = o günkü `MASAK_KIMLIK_ESIGI_TL` (yasal parametre tablosundan, işlem günü) |
| `ilk_n_onayli` | OTO'ya geçmeden önce kaç işlem ÖNERİ kalır (PAIR "kademeli artır"); sayaç `kararlar`'dan hesaplanır, ayrı kolon tutulmaz |
| `surum`, `gecerlilik_baslangic_ani`, `gecerlilik_bitis_ani`, `yazan`, `olusturuldu` | `YasalParametre` kalıbı: açık uçlu satır tek (işlevsel UNIQUE) |

Varsayılanlar (kurucu kararları): `MUSTERI_BAGLA` kesin anahtar = OTO · `MUSTERI_AC` kanıtlı = OTO · `BORC_DUS_TL` tek borç tam tutar = **SOR** (kiracı OTO açabilir — karar 2) · çoklu borç = ÖNERİ · `BORC_DUS_GRAM` = SOR (**kilitli**) · `AMAC_BELIRLE` = SOR (**kilitli**) · `SIPARIS_KAPAT` = OTO (tam tutar) · `FATURA_KES` = ÖNERİ (kiracı OTO açabilir, bekçiler § 6) · `BELGE_TURU` e-Fatura/e-Arşiv = OTO · `MASAK_KIMLIK` = **kilitli SOR** · `CARI_BIRLESTIR` = ÖNERİ.

### 2.3 Mevcut tablolara dokunuşlar (geriye uyumlu)

- `banka_hareketleri.cari_karari` / `karar_hareket_id` (M7a): **emekliye ayrılır**, yazılmaz; okuma `kararlar`'dan. Kolonlar silinmez (ADR-0018'in `fatura_no` kalıbı), geçmiş satırlar göç satırıyla `kararlar`'a taşınır (`tur_no=1`, `neden_etiketi=GOC_M7A`).
- `cari_ibanlari`: var (`Kaynak = Ogrenildi`); ek alan: `onay_sayisi` (kaç karar bu IBAN'ı onayladı → "önce mavi sonra yeşil" rozeti).
- `cariler`: `kaynak` alanı var mı **[D]** (M7a yalnız `Notlar` metnine "Bankadan gelen ödemeyle kendiliğinden açıldı" yazıyor). Gerekli: `kaynak = BANKA`, `masak_kimlik_tespiti = HAYIR`, `aydinlatma_durumu = BEKLIYOR` (KVKK md.10 — ilk iletişimde).
- `hareketler`: gram borca TL tahsilatta **anlaşılan kurun** nereye yazılacağı **[D]** (`KurSnapshotId` sistem kurudur; "anlaşılan kur" alanı görülmedi). Öneri: `kararlar.secenekler_json` + hareket `Aciklama` yetmez; `hareketler.uzlasi_kur` kolonu açılır ya da kur snapshot'ı "ELLE" kaynaklı yeni satır olarak yazılır (`KurSnapshot` kaynak listesi **[D]**).

### 2.4 Banka tarafı

- `banka_baglantilari`: `banka_hesap_id`, `saglayici_kodu` (`SIMULASYON` · `CSV` · `WEBSERVIS_<BANKA>` · ileride `HBH_<ARACI>`), `kimlik_bilgisi` (AES-GCM, ADR-0003, **yalnız kurumsal web servis kullanıcısı; internet bankacılığı parolası asla**), `son_imlec` (bankanın sayfalama/zaman imleci), `durum`, `kiraci_beyani_id` (§ 7), `son_basarili_cekim`, `ardisik_hata`.
- `ekstre_satirlari` (sahne) **yeniden kullanılır**: sağlayıcıdan gelen her satır önce sahneye düşer (`format = API:<saglayici>`), tekillik `(tenant, hesap, banka_referans)` benzersiz indeks — bugünkü `DedupeAnahtari` buna ek olarak kalır (referanssız banka için). Sahne → `HareketEkleIc` geçişini politika yönetir (`EKSTRE_ONAY` konusu: referanslı ve MASAK eşiği altı = OTO; eşik üstü = daima insan, çünkü `HareketEkleIc` MASAK faz 1 kimlik bloğu ister ve otomatik akış bunu veremez). Böylece "yükleme hiçbir şey yazmaz / onay olmadan hareket yok" sözleşmesi (EkstreModulu, doğrulandı) bozulmaz; yalnız onaylayan bazen `SISTEM` olur.

## 3. Akış (metin diyagramı)

```
[Sağlayıcı çekimi | CSV | elle dekont | POS yatması]
        │  (1) sahneye yaz: banka_referans tekilliği → mükerrer ise MUKERRER, durur
        ▼
[EKSTRE_ONAY kararı] ── politika: referanslı & tavan altı → OTO ─┐  eşik üstü → SOR (kimlik bloğu)
        │                                                          │
        ▼                                                          ▼
BankaServisi.HareketEkleIc  (MASAK faz1/faz2, dedupe, bakiye)   Bekleyenler
        │ hareketId
        ▼
[DEĞERLENDİRİCİ — tek giriş: OlayDegerlendirici.Calistir(kaynakTur, kaynakId)]
   saf çözücüler sırayla, her biri bir karar satırı ÜRETİR (yazmaz):
   a) MusteriCozucu   → MUSTERI_BAGLA / MUSTERI_AC / CARI_BIRLESTIR
   b) SiparisCozucu   → SIPARIS_KAPAT (kapora/sipariş tutar+cari eşleşmesi)
   c) BorcCozucu      → BORC_DUS_TL  (VadeKapisi.AcikBorclar FIFO)
   d) GramBorcCozucu  → BORC_DUS_GRAM (kilitli SOR)
   e) AmacCozucu      → AMAC_BELIRLE (hiçbiri değilse: Avans / Satış dışı / Yeni satış)
   f) FaturaCozucu    → FATURA_KES (bekçiler § 6) + BELGE_TURU
   g) MasakCozucu     → MASAK_KIMLIK (uyarı/kilit; sayım HareketEkleIc'te zaten yapıldı)
        │  kararlar INSERT (UNIQUE; 1062 → zaten değerlendirilmiş, çık)
        ▼
[KararUygulayici]  OTO → aynı istek içinde uygula (tek tx, CariIcinBaslat + Kilitle)
                   ÖNERİ/SOR → BEKLIYOR; Bekleyenler kutusu
        │
        ▼ eylem: HareketFabrikasi.Ekle(TAHSILAT, MasakOlcuTl=0) · CariServisi.OlusturId ·
                 SiparisServisi.Kapat · FaturaServisi (taslak) · …
        │
        ▼
[Geri al] StornoServisi.Storno(eylem_hareket_id) → karar GERI_ALINDI → konu yeniden BEKLIYOR (tur_no+1)
```

Zincir kuralı: b başarılıysa c/d/e **açılmaz** (para siparişe gitti); c tam kapatıyorsa e açılmaz; c fazla kalıyorsa kalan için e açılır. Her adım kendi karar satırını yazar; "sessizce atlanan" adım yok — açılmayan konu `GECERSIZ(sebep: ÖNCEKİ_ADIM_KAPATTI)` olarak yazılır ki "eksik olay" sorusunun kaydı olsun.

## 4. Kademe nasıl belirlenir, ayarlanır, kilitlenir

1. **Belirleme sırası (her karar için):** kilitli mi → (evet) SOR. Değilse politika (CARI > BANKA_HESABI > KIRACI kapsamı, en özel kazanır) → tutar tavanı aşıldıysa bir kademe düşür (OTO→ÖNERİ, ÖNERİ→SOR) → `ilk_n_onayli` dolmadıysa OTO→ÖNERİ → çözücünün güveni yeterli değilse (birden çok aday, tolerans dışı) bir kademe düşür. **Kademe asla yükseltilmez**; yalnız düşer.
2. **Ayarlama:** Ayarlar › "Kendiliğinden yapılsın mı?" tek sayfa: konu başına üç durumlu düğme + tavan + "ilk 10 işlemi bana sor". Kilitli konular görünür ama gri, yanında dayanak (örn. "Tedbirler Yön. md.27 — şüphe insan kararıdır"). Yazma SAHIP/MUDUR; denetim iziyle; yeni sürüm satırı.
3. **Kilitli konular (kodda, `KilitliKonular`):** `MASAK_KIMLIK` tamamlanması ve şüpheli işlem bildirimi · `BORC_DUS_GRAM` (kur riski müşteriyle anlaşmaya bağlı) · `AMAC_BELIRLE` (niyet veride yok) · emanet olarak işaretleme · has gramı bilinmeyen kalemle KUYUM (özel matrah) faturası · belgesiz işçilik/alış kaydı · kasa sayım farkını kapatma · dış muhataba ileti gönderme (mutabakat/hatırlatma — `IBildirimKanali` zaten tek, UYGULAMA_ICI; doğrulandı). Zorlama üç katmanlı: politika yazma ucu 400 · `KararUygulayici` OTO denemesinde `InvalidOperationException` · `denetim.sh` sayacı `KILITLI_KONU_OTO = 0`.
4. **Rol:** kararı yalnız SAHIP/MUDUR/MUHASEBE verir (M7a ile aynı); tezgâhtar Bekleyenler'i görmez (EK-3 madde 6 — personel tüm banka akışını görmesin).

## 5. "Para geldi" akışının tamamı

| Adım | Soru | OTO | ÖNERİ | SOR | Doğrulanan dayanak / açık nokta |
|---|---|---|---|---|---|
| 0 | Mükerrer mi? | `banka_referans` benzersiz indeks (yeni) + `DedupeAnahtari` (var; işletme günü, doğrulandı) | — | Referanssız bankada aynı gün aynı tutar → MUKERRER işaretli, insan "yine de al" der (mevcut sahne davranışı) | (tenant, hesap, dedupe) UNIQUE indeksi varlık yorumunda; migration'da **[D]** |
| 1 | Gönderen kim? | Geçerli TCKN/VKN birebir ya da öğrenilmiş IBAN → bağla (`Coz`: KIMLIK/IBAN yolları, doğrulandı) | Ad benzerliği tek adaya işaret ediyor (skor ≥ 0,5) → öneri; açıklamada cari kodu/fiş no → öneri | Birden çok aday; ad tutup IBAN yeni | K4-OK-5 korunur. MASAK md.24: TCKN garanti değil → birincil anahtar fiilen IBAN+ad. |
| 1b | Müşteri yok → aç? | Aday yok **ve** (geçerli TCKN/VKN **ya da** IBAN) → `CariServisi.OlusturId` (kurucu kararı 4; `Coz` `kanit` şartı doğrulandı). Karta `kaynak=BANKA`, kimlik tespiti=HAYIR, aydınlatma=BEKLIYOR | Aynı ad farklı IBAN → `CARI_BIRLESTIR` önerisi | Yalnız ad (POS yatması, faiz, masraf da ad taşır) → açılmaz, SOR | Kart açılışı MASAK kapısına takılırsa M7a'daki gibi: rollback, para müşterisiz kaydedilir, karar ENGELLENDI (mevcut davranış, doğrulandı). |
| 2 | Sipariş/kapora mı? | Aynı carinin açık siparişi var ve tutar = kalan ya da = tahmini tutar → `SIPARIS_KAPAT` OTO; sipariş kaporası `Kapora` ucu TAHSILAT hareketi doğuruyor (doğrulandı) — buradan geçer, kasa bacağı yok | Tutar yakın (tolerans ±%1, kiracı ayarı) → öneri | Birden çok açık sipariş | Satış fişiyle eşleşme: fişin HAVALE bacağı zaten yazılmışsa (`OdemeKalemi.Arac=HAVALE`) bu para **o fişin tahsilatıdır**, borç düşülmez, fatura kesilmez; eşleştirme önerisi "Fiş #… ile eşleştir". Fiş–dekont bağı kolonu **[D]** (OdemeKalemi'nde banka hareket bağı görülmedi) → yeni `odeme_kalemleri.banka_hareket_id` ya da kararlar üzerinden bağ. |
| 3 | Açık TL borç | Tek açık parti ve tutar = kalan → politika OTO ise TAHSILAT (`OdemeIzi.Etiketten("HAVALE")`, `MasakOlcuTl=0`, `TlTutari=-tutar`; M7a kalıbı doğrulandı). Varsayılan SOR (kurucu 2) | Birden çok parti: FIFO en eskiden; **dağıtım defterde ifade edilmez** (VadeKapisi: "mahsup tablosu yok, bilinçli" — doğrulandı). Öneri yalnız bilgi amaçlıdır: "Bu ödeme 12.03 ve 20.04 borçlarını kapatır". Kuyumcu farklı dağıtım isterse → mahsup tablosu gerekir (§ 11 açık soru) | Tutar < borç: "Kalanı açık bırak" (varsayılan) / "Farkı indirim yaz" (CARI_DUZELTME, gerekçe ≥ 20) · Tutar > borç: borç kadar düş + kalan için adım 5 | Kısmi ödeme toleransı (Odoo kalıbı) kiracı ayarı; 0 varsayılan. |
| 4 | Açık gram borç | — | — | **Daima SOR (kilitli).** Seçenekler: bugünkü satış kuru · bugünkü alış kuru · anlaşılan kur (elle) · "TL alacak olarak tut, gram düşme". Sistem gramı hesaplar gösterir, seçmez | Hareket: TAHSILAT `TlTutari=-tutar, HasGrami=-gram, Maden=ALTIN`. `HareketFabrikasi` TAHSILAT'ta has bacağını kabul ediyor mu, `MadenKapisi.HareketDogrula` ne ister **[D]**; storno beyaz listesinde TAHSILAT var (doğrulandı) ama has bacaklı TAHSILAT'ın stornosu test edilmeli **[D]**. Kur farkı kaydı mali müşavir (§ 11). |
| 5 | Hiçbiri / kalan | — | — | **SOR (kilitli).** Düğmeler eylem adıyla: "Avans olarak kaydet" (TAHSILAT → bakiye eksiye düşer = müşteri alacağı; Açık Pozisyon'da görünür) · "Yeni satış — fiş aç" (Tezgâh'a HAVALE bacağı dolu olarak taşır) · "Satış dışı (kişisel/ortak/iade)" (defterde hareket yok; banka satırı etiketlenir, ciro/MASAK dışı — MASAK dışı bırakma mali müşavir **[D]**) · **"Emanet" seçeneği YOK**: emanet modülü gram defteridir (`EmanetModulu`: tlTutari=0, doğrulandı); TL emanetin defter karşılığı yok. "Müşterinin parası bizde duruyor" = Avans. | Avans faturası zamanı mali müşavir (araştırma 8.3-8). |
| 6 | Fatura | § 6 | § 6 | § 6 | |
| 7 | MASAK | Sayım `HareketEkleIc`'te dekont gününde yapıldı (`GunlukIslemToplam` banka kümesi `hareket_id IS NULL`, doğrulandı). Tüm eylem hareketleri `MasakOlcuTl=0` (çift sayım yok) | — | Eşiğe yaklaşma (≥ %80, kiracı ayarı) → `MASAK_KIMLIK` uyarısı; bankadan açılan cari kimlik tespiti **yapılmış sayılmaz**; eşik üstü → kart kimlik bloğu girilmeden sonraki OTO'lar durur | `banka_hareketleri.hareket_id` bugün daima NULL (doğrulandı). Tasarım bunu **korur**: eylem hareketi `karar.eylem_hareket_id`'ye yazılır, `hareket_id`'ye değil (M7a ile aynı gerekçe). |

Geri alma her adımda: eylem stornosu + karar `GERI_ALINDI` + konu yeniden `BEKLIYOR`. Kaynak dekont stornolanırsa (`BankaHareketiStorno`, var) tüm kararları `GECERSIZ`, uygulanmış eylemleri **zorunlu storno** (yarım geri alma yok — StornoServisi ilkesi).

## 6. Fatura adımı

**İlke:** Banka hareketi **belgeyi değil satışı** tamamlar. Fatura, defterde has bacağı olan bir satış/sipariş satırından kesilir; havaleden doğrudan fatura yalnız iki durumda: (a) eşleşen satış/sipariş var ve belgesi kesilmemiş, (b) kuyumcunun IBAN/cari kuralı var ("bu IBAN = toptancı, havale = 22 ayar X ürün satışı").

Bekçiler (saf, test edilebilir; hepsi fail-closed):
1. **Çift fatura bekçisi:** eşleşen satış hareketi `FaturaHareketBagi.IlkBag` ile faturalıysa → kesilmez (ADR-0018 tek sorgu yolu). `BORC_DUS_TL` kararında → **fatura adımı hiç açılmaz**: TAHSILAT hangi VERESIYE'yi kapattığını bilmez (mahsup yok), dolayısıyla "o veresiye faturalı mıydı" sorusu kesin cevaplanamaz; muhafazakâr yol tahsilatı faturalamamaktır. Faturasız veresiye varsa o veresiyenin faturası ayrı akıştır (Faturalar ekranı). `AVANS` / `SATIS_DISI` → kesilmez.
2. **Has gram bekçisi:** `KdvTuru=KUYUM` fatura yalnız has gramı bilinen kalemle; banka satırından KUYUM kesilemez (mevcut `FaturayaDonustur` zaten `STANDART/HasDeger=0` yazıyor, doğrulandı). Ürün bilinmiyorsa → **SOR: ürün seç** (Tezgâh'a yönlendirme), kuraldan geliyorsa ÖNERİ (ilk N onaylı). Has bedel = BIST son işgünü kapanışı × has gram (`OzelMatrahMotoru` bunu nasıl alıyor **[D]**; araştırma: "sistem has kuru mu borsa kapanışı mı doğrulanacak").
3. **Tarih bekçisi:** fatura numarası **bugün** tüketilir; mevcut `FaturayaDonustur` `Tarih = hareket.Tarih` ile geçmiş tarihli KESILDI belge üretiyor (doğrulandı) — seri sırası/tarih tutarlılığı riski; yeni akışta belge tarihi = kesim günü, dekont tarihi açıklamada.
4. **e-Fatura/e-Arşiv seçimi:** `IEFaturaSaglayici.MukellefMi(vkn)` → OTO; yanıt yoksa belge kesilmez, karar `ENGELLENDI(MUKELLEF_SORGUSU_YOK)`; tahminle tür seçilmez. ADR-0012: sağlayıcı soyutlaması yazılır ama **sahte sağlayıcıyla "e-Fatura bitti" denmez**; entegratör sözleşmesi gelene dek fatura adımı yalnız **taslak** üretir (`Durum=TASLAK`, numara yok — `FaturaSeriKapisi` taslak işareti, doğrulandı) ve "GİB'e gitmedi" rozeti taşır.
5. **Belge türü (fiş/fatura):** `SatisBelgeAyari` (FIS|FATURA) + `KUYUMCU_FATURA_HADDI_TL` yasal parametre (36.000; 514 md.4 yürürlüğü **[D]**) — mevcut M6 doğrulandı. Eşik üstü fatura zorunlu, kimliksiz 11111111111 engel.
6. **Toplu:** gün sonu "bugün kesilmeyi bekleyenler" listesi tek onay (eşik üstü hariç) — Faturalog toplu kalıbı; `IZamanlanmisIs` günlük anahtarlı (doğrulandı) buna uyar.

Entegratör soyutlaması: `IEFaturaSaglayici { Kod; MukellefMi; Gonder(UBL) → (belgeNo, durum); DurumSorgula; Iptal }`; `IKurAdaptoru` kalıbı (Kod + tek yöntem; doğrulandı). DI'da uygulama sayısı sayaçla izlenir; üretimde sahte sağlayıcı kayıtlı olamaz (`IBildirimKanali` "DI kaydı = 1" sayacı kalıbı).

## 7. Banka bağlayıcısı ve kurucu kararı 1'in riski

```csharp
interface IBankaSaglayici {
  string Kod { get; }                                  // SIMULASYON | CSV | WEBSERVIS_KUVEYTTURK | HBH_<ARACI>
  SaglayiciYetenek Yetenekler { get; }                 // SaltOkur zorunlu; OdemeBaslatma YOK (enum'da bile yok)
  Task<HamHareketSayfasi> HareketleriCek(BankaBaglanti b, Imlec? son, CancellationToken ct);
  Task<BaglantiSinama> Sina(BankaBaglanti b, CancellationToken ct);
}
```
Ham satır → `ekstre_satirlari` sahnesi (§ 2.4) → `EKSTRE_ONAY` kararı → `HareketEkleIc`. Sağlayıcı deftere **asla** yazmaz (`denetim.sh`: `Modules/Banka/Saglayicilar/` altında `BankaHareketleri.Add` = 0, `HttpClient.Post*` = 0).

Risk (6493 md.28, araştırma § 6.8: web servis yolunun lisans gerektirmediğini söyleyen kaynak yok) — mimari karşılığı:
1. **Hukuki kapı yazılımın içinde:** `WEBSERVIS_*` sağlayıcıları `Yetenekler.HukukiOnayGerekli=true`; etkinleştirme iki anahtar ister: (a) sistem düzeyinde `BANKA_WEBSERVIS_IZNI` yasal-parametre satırı (dayanak = avukat görüşü tarih/no; `YasalParametre` tablosu kiracı-üstü, append-only, sistem anahtarlı — doğrulandı), (b) kiracı düzeyinde `kiraci_beyanlari` kaydı: "verileri kendi adıma, kendi bankamla imzaladığım sözleşmeyle çektiriyorum" — zaman damgalı, kullanıcı kimlikli. İkisi yoksa sağlayıcı 403.
2. **Teknik hizmet sınırı kodla kanıtlanır (md.12/2-ğ savunması):** salt okur; ödeme başlatma ucu yok; fon tutulmaz; kimlik bilgisi kiracıya ait ve kiracı başına şifreli; Mihenk kendi adına toplu çekim yapmaz (her çekim kiracı kapsamında `SistemKapsami.Kiraci`).
3. **Hassas veri yasağı:** internet bankacılığı parolası/OTP alanı **yoktur** (TCMB Yön. md.61/2 ruhu); yalnız kurumsal web servis kullanıcısı/sertifika.
4. **Değiştirilebilirlik:** lisanslı HBH aracısına geçiş = yeni sağlayıcı sınıfı; akış, sahne, kararlar değişmez.
5. **Ürün bloke olmaz:** ilk teslim `SIMULASYON` (demo "havale gönder", banka referansı üretir) + `CSV` (var). Müşteri 16.10: "şimdilik simülasyon yeterli".
6. **Zamanlama:** webhook yok (araştırma) → periyodik çekim 1–5 dk. `IZamanlanmisIs` **günlük** anahtarlı (`CalismaAnahtari(yerelGun)`, doğrulandı) — dakikalık çekime uymaz; ayrı `BankaCekimHizmeti` (hosted service, kiracı başına kilit, üstel geri çekilme, `ardisik_hata ≥ N` → bildirim). Mevcut `IsZamanlayici` buna uygun mu **[D]**.

## 8. Ekran ilkeleri

- **Tek Bekleyenler kutusu** (Pano'da sayaç + ilgili ekranda satırlar), konuya göre gruplu; `GelenOdemeKarari.razor` (M7a) bu kutunun ilk bölmesi olur, ayrı kutu kalmaz. Boşsa çizilmez (zorunlu önce; mevcut davranış doğrulandı).
- Satır: tarih · kim · tutar · **tek birincil düğme** (ÖNERİ'de önerilen, eylem adıyla: "Borçtan düş", "Avans olarak kaydet") · "Başka…" katlanır alternatifler · neden rozeti (kategorik, yüzde yok) · SOR satırında birincil düğme **yok**, eşit düğmeler.
- Üstte "Hazır olanların hepsini onayla": yalnız ÖNERİ + tavan altı; SOR ve eşik üstü dışarıda, satır nedenini yazar.
- "Yapılanlar" sekmesi: OTO/onaylı işlemler, "neden" + **Geri al**; geri alma gerekçe ≥ 20 karakter (storno kuralı, doğrulandı) — otomatik geri almada gerekçe önceden doldurulur ("Kullanıcı otomatik borç düşmeyi geri aldı").
- "Bunu kural yap": satırdan IBAN/cari kuralı; önizleme + ilk N onaylı (`UrunKuraliServisi.Onizleme/Uygula` kalıbı, doğrulandı).
- Ayarlar › tek sayfa "Kendiliğinden yapılsın mı?" (§ 4.2).
- Sade–eksiksiz dengesi: ekranda yalnız **bugün karar bekleyen** görünür; "eksik olay olmaması" ekran kalabalığıyla değil **karar defterinin tamlığıyla** sağlanır (her konu için satır var: BEKLIYOR/UYGULANDI/GECERSIZ). Denetçi sorusu "bu para ne oldu?" tek sorguyla cevaplanır.
- Tema: yalnız Yıldız (karar 5); yeni ekran `*.Yildiz.cs` düzenini izler (sayfa adlandırması gözlendi).

## 9. Eşzamanlılık, idempotentlik, denetim, çok kiracılık, test

- **İdempotent değerlendirme:** `kararlar` UNIQUE anahtarı; ikinci değerlendirme 1062 → sessizce çık (yeniden değerlendirme yalnız `ENGELLENDI`/`GERI_ALINDI` satırda `tur_no+1`).
- **Kilit sırası** mevcut düzenle aynı: `CariIcinBaslat(cari)` → `Kilitle(BankaHareketleri, id)` → hesap bakiye kilidi (M7a ve `HareketEkleIc` doğrulandı). OTO eylem, olayın yazıldığı **işlemden ayrı** ikinci bir işlemde koşar (dekont commit'i sonrası) ki kapı hatası dekontu geri sarmasın; ama karar satırı ve eylem hareketi **aynı** işlemde doğar (MASAK faz-2 "aynı commit" ilkesi).
- **Tek `SaveChangesAsync`** her uygulama metodunda (denetim.sh `TEK_SAVECHANGES` kalıbı).
- **Denetim izi:** her karar geçişi `DenetimServisi.YazmaKaydet` (var, append-only ADR-0004); hassas alan yok (`hassas:false`).
- **Çok kiracılık:** yeni tablolar `IKiraciVarlik`; sorgularda elle tenant şartı (ADR-0010); banka çekimi kiracı kapsamında; `KiraciYazmaDenetcisi` mevcut (içeriği okunmadı **[D]**).
- **Testler:** gerçek MySQL (ADR-0011); çözücüler saf → birim; uygulayıcı → bütünleşik; mutasyon kanıtı: kilit kaldırılınca `KILITLI_KONU_OTO` testi kırmızı; eşzamanlı iki değerlendirme → tek karar; geri al → yeniden açılış; dekont stornosu → tüm kararlar GECERSIZ + eylemler stornolu. Gerçek tarayıcı kuralı: Bekleyenler 1280+375, açık+koyu.
- **Yeni denetim.sh sayaçları:** `OTOMASYON_DOGRUDAN_ADD = 0` · `KILITLI_KONU_OTO = 0` · `SAGLAYICI_YAZMA_UCU = 0` · `EFATURA_SAHTE_URETIM_DI = 0` · `KARAR_TEKILLIK_INDEKSI = 1`.

## 10. Paket sırası ve bitti tanımları

| # | Paket | Bitti tanımı |
|---|---|---|
| **O-0** | **Mevcut yol düzeltmesi (hotfix):** `FaturayaDonustur` (a) `cari_karari=BORC` ya da avans/satış dışı kararlı dekontu reddeder, (b) virman/storno dışı ek olarak kararı verilmemiş ESLESTI dekontta "önce borç kararı" ister, (c) belge tarihi = bugün. | Test: BORC kararlı dekont → 409; FATURALANDI dekont Bekleyenler'den düşmüyor (ya da düşmeden önce karar zorunlu). |
| **O-1** | `kararlar` + `otomasyon_politikalari` + `KilitliKonular` + `KararUygulayici` + Bekleyenler kutusu (M7a göçü, `cari_karari` emekli) + Ayarlar sayfası | M7a davranışı birebir (SOR varsayılan); kurucu kararı 2 ayarı çalışır; UNIQUE + eşzamanlılık testi; geri al → yeniden açılış; 3 sayaç yeşil; tarayıcı kanıtı. |
| **O-2** | Sahne yeniden kullanımı + `IBankaSaglayici` + `SIMULASYON` + `EKSTRE_ONAY` politikası + `banka_referans` benzersizliği | Demo "havale gönder" → 5 sn içinde Bekleyenler'de; aynı referans ikinci kez → MUKERRER; eşik üstü satır insana düşer. |
| **O-3** | Müşteri: `CARI_BIRLESTIR` önerisi, IBAN onay sayacı rozeti, bankadan açılan kartta kaynak/kimlik/aydınlatma alanları | Aynı ad farklı IBAN → öneri; bankadan açılan kart MASAK eşiğinde kimlik ister. |
| **O-4** | TL borç: FIFO açıklamalı öneri, kısmi/fazla ödeme seçenekleri, tolerans ayarı | Fazla ödeme → borç kadar düşer + kalan AMAC_BELIRLE; "farkı indirim yaz" CARI_DUZELTME gerekçeli. |
| **O-5** | Sipariş/kapora ve fiş-HAVALE bacağı eşleşmesi | Fişte HAVALE bacağı varsa borç düşmez/fatura kesilmez; sipariş tam tutar OTO kapanır. |
| **O-6** | Gram borç (kilitli SOR) + anlaşılan kur alanı + has bacaklı TAHSILAT storno testi | Üç kur seçeneği; storno gramı geri yazar. |
| **O-7** | Amaç: Avans / Yeni satış (Tezgâh'a taşı) / Satış dışı | Avans bakiyeyi eksiye düşürür, Açık Pozisyon'da görünür. |
| **O-8** | Fatura adımı: bekçiler + `IEFaturaSaglayici` soyutlaması + taslak üretimi + gün sonu toplu liste (GİB gönderimi entegratöre bağlı — ADR-0012) | KUYUM faturası banka satırından kesilemez; mükellef sorgusu yoksa ENGELLENDI; taslak numarasız. |
| **O-9** | "Bunu kural yap" (IBAN/cari kuralı, ilk N onaylı, önizleme) | Kural öncesi/sonrası önizleme; N dolunca OTO. |
| **O-10** | Gerçek banka sağlayıcısı (`WEBSERVIS_<ilk banka>`) — **yalnız** `BANKA_WEBSERVIS_IZNI` + kiracı beyanı varsa | Sandbox'tan gerçek yanıt; kimlik bilgisi şifreli; `SAGLAYICI_YAZMA_UCU=0`. |
| **O-11** | Mutabakat mektubu taslağı, hatırlatma planı (varsayılan kapalı; kanal yok) | Gönderim ucu yok; yalnız taslak/plan. |

Her paket: etki analizi → kod → testler → `denetim.sh` → gerçek tarayıcı 1280+375 → yerel commit (CLAUDE.md kapısı).

## 11. Riskler ve açık sorular

**Mimari riskler**
1. **Mahsup tablosu yok** (VadeKapisi bilinçli kararı): çoklu borçta kuyumcunun "şu borcu kapat" seçimi defterde tutulamaz. Seçenek A: FIFO'yu kabul, öneri bilgi amaçlı (önerim). Seçenek B: mahsup tablosu — her ödeme yolunu etkiler, büyük değişiklik. Ekip oyu.
2. **MASAK ile otomatik akış:** eşik üstü satır kimlik bloğu olmadan `HareketEkleIc`'e giremez; akış bunu insan adımı olarak kabul ediyor. "Kimliği sonradan alıp bağla" yolu bilinçli kapalı (Q9-1 notu). Doğru ama müşteri deneyiminde "para ekranda görünmüyor" şikâyeti doğurabilir → sahnede görünür, "kimlik bekliyor" etiketiyle.
3. **Otomatik cari açmada KVKK aydınlatma:** ilk iletişim zamanı hukukçu (araştırma 8.4-2).
4. **Satış dışı** paranın MASAK/ciro dışı sayılması mali müşavir.
5. **Avans faturası** zamanı mali müşavir.

**Koddan doğrulanamayanlar [D]** (uygulama öncesi ölçülecek): TAHSILAT'ta has bacağı kabulü; anlaşılan kur alanı; `Cari.kaynak`; dedupe UNIQUE migration; `OzelMatrahMotoru` has bedel kaynağı; `IsZamanlayici` dakikalık işe uygunluğu; `DogrulaKumulatif(cariId=null)` davranışı; `KiraciYazmaDenetcisi` kapsamı; fiş–dekont bağ kolonu; `MASAK_KIMLIK_ESIGI_TL` tohum değeri (370.000 mi) — *Orkestratör 17.10'da doğruladı: geçiş 20261007140228_MasakKimlikEsigi370 ve canlı tabloda masak370-01 satırı 370.000 TL, 07.10.2026'dan geçerli; RG 33393 birincil metin okundu.*

**Kurucuya / dışa**: avukat görüşü (web servis yolu) · entegratör seçimi · Faturalog'un müşteride otomatik mi onaylı mı çalıştığı · ilk banka.

---

## 12. Mevcut "Karar Merkezi" taslağıyla karşılaştırma

(`dokumanlar/02-mimari/KARAR-MERKEZI-MIMARI-2026-10-17.md`, 99 satır — tasarımım bittikten sonra okundu.)

### 12.1 Aynı olanlar
- Üç kademe (OTO/ÖNERİ/SOR) ve "SOR'da hiçbiri önceden seçilmez".
- Tek `kararlar` + politika tablosu; neden etiketi; Geri al = storno; silme yok.
- Karar Merkezi deftere kendisi yazmaz, mevcut tek kapıları çağırır.
- `IBankaBaglayici`/`IBankaSaglayici` soyutlaması; tüm yollar `HareketEkleIc`'ten.
- Saf çözücüler (müşteri/eşleşme/belge).
- Tek Bekleyenler kutusu, Pano sayacı, toplu onay eşik üstü hariç, "Bunu kural yap", ilk N onaylı.
- Para geldi sırası (mükerrer → müşteri → sipariş → TL → gram → fatura → MASAK).
- Kilit listesi büyük ölçüde örtüşüyor (emanet, ŞİB, gram kuru, has gramsız özel matrah).
- Fatura adımı iki bekçi (çift fatura, has gram); e-Fatura/e-Arşiv OTO, sorgu yoksa kuyruk.
- Banka web servisi avukat görüşü olmadan açılmaz.

### 12.2 Farklı olanlar (hangisi neden daha iyi)
| Konu | Mevcut taslak | Bu tasarım | Değerlendirme |
|---|---|---|---|
| Tekillik/eşzamanlılık | Yok | `kararlar` UNIQUE(konu, kaynak, tur_no); kilit sırası; tek SaveChanges; idempotent değerlendirme | **Bu tasarım.** Taslak "ikinci değerlendirme iki karar yazar mı?" sorusuna cevapsız; mevcut kodun bütün dersleri (dedupe, MukerrerMi, Kilitle) tekillik üzerine. |
| Politika sürümü | Kiracı ayarı, hesap başına, tavan | + append-only sürüm, karar politika sürümüne bağlı, kapsam CARI > HESAP > KIRACI, kademe yalnız düşer | **Bu tasarım.** "O gün neden otomatik yapıldı" denetçiye ancak sürümle anlatılır (YasalParametre kalıbı zaten evde). |
| Kilidin zorlanması | "ayarla OTO'ya çevrilemez" (cümle) | Üç katman: 400 + uygulayıcı istisnası + denetim.sh sayacı + mutasyon testi | **Bu tasarım.** ADR-0004 dersi: yorum kural değildir. |
| Otomatik akışın giriş yeri | Doğrudan `HareketEkleIc` | Önce **sahne** (`ekstre_satirlari` yeniden kullanım), `EKSTRE_ONAY` kararı, sonra `HareketEkleIc` | **Bu tasarım.** `HareketEkleIc` eşik üstünde kimlik bloğu ister; otomatik akış bunu veremez — taslak bu satırların nereye düştüğünü söylemiyor. Sahne "yükleme yazmaz" sözleşmesini korur ve banka referansı tekilliğini tek yerde tutar. |
| Zamanlama | "canlı yenileme" | `IZamanlanmisIs` günlük anahtarlı; dakikalık çekim ayrı hizmet | **Bu tasarım** (taslak altyapı uyumsuzluğunu görmemiş). |
| Hukuki riskin karşılığı | Avukat görüşü şartı (süreç) | + kodla kanıt: salt okur yetenek, ödeme ucu yok sayacı, kiracı başına kimlik, sistem izni + kiracı beyanı çift anahtar, HBH'ye geçiş yolu, SIMULASYON+CSV ile teslim | **Bu tasarım.** Süreç kuralı yeterli değil; md.12/2-ğ savunması yazılımın yapısında görünmeli. |
| Cari açma koşulu | "aday yok → kart aç OTO (kurucu 16.10)" | Aday yok **ve** TCKN/VKN ya da IBAN kanıtı (kurucu 17.10 kararı 4; `Coz` zaten böyle) + kart alanları (kaynak, kimlik tespiti=HAYIR, aydınlatma) | **Bu tasarım**; taslak 16.10 kararını yazmış, 17.10 daraltmasını ve KVKK bayrağını atlamış. |
| TL seçenekleri | "Borçtan düş / Yeni alışveriş / Avans / Emanet / Satış dışı" | Emanet **çıkarıldı** (gram defteri; TL karşılığı yok), Satış dışı'nın defter etkisi tanımlandı (hareket yok, etiket) | **Bu tasarım.** Taslaktaki "Emanet" düğmesi deftere yazılamaz bir seçenek. |
| Çoklu borç dağıtımı | "dağıtım önerisi (en eskiden)" | Dağıtım defterde ifade edilemez (mahsup yok, bilinçli); öneri bilgi amaçlı ya da mahsup tablosu kararı | **Bu tasarım** açık sorunu adlandırıyor; taslak çelişkiyi görmemiş. |
| Çift fatura bekçisi (b) | "borcundan düş kararıyla faturalanmış veresiyeyi kapatıyorsa kesilmez" | BORC kararında fatura adımı **hiç açılmaz** (tahsilat–veresiye bağı yok, "faturalı mıydı" kesin bilinemez) | **Bu tasarım** daha muhafazakâr ve uygulanabilir; taslağın bekçisi hesaplanamaz. |
| Mevcut `FaturayaDonustur` | Değinilmemiş | O-0 hotfix: `cari_karari`'ne bakmıyor, STANDART KESILDI'yi dekont tarihiyle üretiyor, FATURALANDI olunca borç kararı listeden düşüyor | **Bu tasarım.** Bugün çift fatura ve "borç kararı atlandı" yolu açık (koddan doğrulandı). |
| M7a göçü | § 3: "ikinci tür gelince taşınır, önceden taşınmaz" ama § 5 A1: "M7a'nın taşınması" | O-1'de göç; `cari_karari` emekli, kolon kalır | Taslak **kendi içinde çelişkili**; A2 zaten beş yeni karar türü getiriyor, taşımayı A1'e almak doğru. |
| Rol/görünürlük | Yok | SAHIP/MUDUR/MUHASEBE; tezgâhtar görmez | **Bu tasarım** (EK-3 madde 6). |
| Kaynak dekont stornosu | Yok | Tüm kararlar GECERSIZ + eylemlerin zorunlu stornosu | **Bu tasarım.** |
| "Eksik olay yok" güvencesi | Ekranda | Karar defterinin tamlığı (açılmayan konu da GECERSIZ(sebep) yazılır) | **Bu tasarım**: sadelik ekranda, tamlık defterde. |
| Entegratör | A4 notu "GİB gönderimi entegratöre bağlı" | + `IEFaturaSaglayici` imzası, üretimde sahte DI yasağı sayacı, yalnız TASLAK üretimi | Tamamlayıcı. |
| MASAK A0 | "ürün 07.10'dan beri 370.000 uyguluyor" | Tohum değeri **[D]** — *Orkestratör 17.10'da doğruladı: geçiş 20261007140228_MasakKimlikEsigi370 ve canlı tabloda masak370-01 satırı 370.000 TL, 07.10.2026'dan geçerli; RG 33393 birincil metin okundu.* | Taslaktaki iddia bu turda doğrulanamadı; `MASAK_KIMLIK_ESIGI_TL` satırı migration'da kontrol edilmeli. (Orkestratör notuyla kapandı.) |

Taslağın daha iyi olduğu yer: **kısalık** — kurucuya sunum için okunabilir. Bu tasarımın ayrıntısı uygulama kapısı içindir.

### 12.3 Taslakta hata/eksik görülenler (özet)
1. § 3 son paragraf ile § 5 A1 çelişiyor (M7a taşınması).
2. "Emanet" TL seçeneği deftere yazılamaz.
3. Çoklu borç dağıtımı VadeKapisi'nin "mahsup yok" kararıyla çelişir; belirtilmemiş.
4. Çift fatura bekçisi (b) hesaplanamaz bir koşul.
5. Otomatik akışın MASAK eşik üstü satırları nereye düşeceği yok; sahne kullanımı yok.
6. Tekillik, eşzamanlılık, kaynak stornosu, politika sürümü, rol görünürlüğü, KVKK aydınlatma yok.
7. Cari açma koşulu 17.10 kararı 4'ten gevşek yazılmış.
8. `FaturayaDonustur`'daki bugünkü açık (BORC kararı atlanarak faturalama, geçmiş tarihli KESILDI) görülmemiş — en acil düzeltme.
9. Zamanlayıcı altyapısı (günlük anahtar) ile "canlı yenileme" uyumsuzluğu.
10. Hukuki risk yalnız süreçle karşılanmış; yapısal güvence yok.

### 12.4 Birleşik öneri
Taslağın iskeleti (§ 1–2–6 başlıkları) korunur; şunlar eklenir/değişir:
- **Yeni § "Tekillik ve eşzamanlılık"** (UNIQUE anahtar, kilit sırası, tek SaveChanges, idempotent değerlendirme, kaynak stornosu → GECERSIZ).
- **Politika tablosu sürümlü ve kapsamlı** (CARI > HESAP > KIRACI); karar politika sürümünü taşır; kademe yalnız düşer.
- **Kilit üç katman** + yeni denetim.sh sayaçları (§ 9).
- **Sahne yeniden kullanımı** ve `EKSTRE_ONAY` konusu; sağlayıcı salt okur, ödeme ucu yok; `WEBSERVIS_*` çift anahtar (sistem izni + kiracı beyanı); dakikalık çekim ayrı hizmet.
- **Para geldi § 3** düzeltmeleri: cari açma = kanıt şartı + kart bayrakları; "Emanet" çıkar, "Satış dışı" tanımlanır; çoklu borçta FIFO bilgi amaçlı (ya da mahsup kararı ekip oyuna); BORC kararında fatura adımı açılmaz; gram borçta anlaşılan kur alanı **[D]** kapatılır.
- **Paket sırası:** A1 öncesine **O-0 hotfix** (`FaturayaDonustur` bekçileri + tarih); A1 = O-1 (M7a göçü dahil, çelişki giderilir); A3 = O-2 (+sahne) ve O-10 ayrı (hukuki anahtarla); A2 = O-3…O-7; A4 = O-8; A5 = O-9/O-11.
- **Açık sorular bölümü** taslağa eklenir (§ 11 listesi) ve her **[D]** kalemi uygulama paketinin ilk adımı "ölç" olarak yazılır (Anayasa md.1).
