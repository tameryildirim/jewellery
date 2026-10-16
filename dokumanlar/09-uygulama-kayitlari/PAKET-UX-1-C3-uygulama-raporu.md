# PAKET UX-1-C3 — Uygulama raporu

> "Her veri girilen yer düzeltilebilir" · Faz C, sayfa grubu 3
> (Gider pusulaları · Gider pusulası belgesi · Uyum · Raporlar · Kâr-Zarar ·
> Vade takibi · Denetim izi)
> Dalga 2c · Tarih: 2026-09-17 · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **BİTTİ** — derleme **0 uyarı / 0 hata** · **1798/1798 test yeşil**
> (önce 1766, **+32 yeni test**) · `dotnet/denetim.sh` **TEMİZ** ·
> **14 mutasyon kanıtı** (hepsi kırmızıya döndü, dosyalar `cmp` ile birebir geri
> alındı) · canlı doğrulama 5198'de yapıldı, sunucu durduruldu, test kiracısı
> 39 tablodan silindi (tenants **8 → 8**).

---

## 0. Önkoşul kanıtları (ilk adımda okundu)

| Önkoşul | Kaynak | Sonuç |
|---|---|---|
| **48-A storno motoru** | `PAKET-48-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` 20:48 | **GEÇTİ** (hasım doğrulayıcı tur 1, 1508 test) — MÜHÜR 5 KOŞUL 1 karşılandı |
| **45-API (MFA)** | `PAKET-45-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` 21:10 | **GEÇTİ** (tur 1, 1540 test) |
| QA turu 2 ham bulgular | `ham/qa-turu-2.json` (grup 3'ün 7 rotası) | tamamı okundu, §6'da tek tek kapatıldı/ertelendi |
| UX-1-B (işletme kimliği) | `PAKET-UX-1-B-uygulama-raporu.md` + `Entities.cs:79-83` | `tenants.adres/vergi_dairesi/vergi_no/telefon` mevcut — belgeye bağlandı |
| UX-4d devri (Raporlar) | `PAKET-UX-4d-uygulama-raporu.md` + `RaporModulu.cs:170-174` | sunucu alanları vardı, ekran çizmiyordu → bu pakette çizildi |
| S-2 / record dondurma | `PAKET-S-2-uygulama-raporu.md`, `denetim.sh:113` | tavan 122; **yeni istemci record YAZILMADI**, mevcut `AylikYanit` sunucunun bugünkü şekline hizalandı |
| UX-1-C1 / C2 kalıpları | ilgili raporlar | `SayfaBasi` · `Cekmece` · `DegismezIpucu` · `FormDurumu` yeniden kullanıldı, ikinci kalıp açılmadı |

---

## 1. Yapılanlar — sayfa × düzenlenebilirlik tablosu (DoD sözleşmesi)

| Sayfa | Düzenlenebilir alan | Yol / id | Düzenlenemeyen → DegismezIpucu gerekçesi |
|---|---|---|---|
| **/gider-pusulalari** | (yok — kayıt hurda alımıyla doğar) | Birincil eylem `#gp-hurda-alimi` → `/islemler` | `#degismez-ipucu-GiderPusulasi` · gerekçe anahtarı **STORNO_YOK** (`Duzeltme.StornoYokGerekce`; damga `DuzeltmePolitikasi.cs:123` = `SALT_OKUNUR`/`STORNO_YOK`) + alternatif yol: "sistem içinde düzeltilemez, mali müşavire başvurun" (`SayfaDuzeltme.PusulaDuzeltmeYolu`). Detay artık `#cekmece-gider-pusulasi`; Kaydet **kilitli** ve sebebi `#cekmece-gider-pusulasi-kilit`'te yazılı — kilit sebebi de **`Duzeltme.StornoYokGerekce`** (3. geçiş, § E.8; eskiden `DefterGerekce` idi ve "düzeltme yalnız ters kayıtla yapılır" diyerek OLMAYAN bir yol vaat ediyordu). |
| **/gider-pusulalari/{id}/belge** | işletme kimliği (belgeden değil, işletme kartından) | `#gpb-isletme-karti` → `/isletme` | Bilgi eksikse `#degismez-ipucu-IsletmeKimligi` · "adres ve vergi dairesi/no girilmemiş; belge bu alanlar olmadan basılıyor". Dolu bilgi `#gpb-duzenleyen-adres` / `#gpb-duzenleyen-vergi` / `#gpb-duzenleyen-telefon` ile **kâğıda** basılır. |
| **/uyum — kimlik tespiti** | (yok) | `#cekmece-kimlik-tespiti` (salt okunur, Kaydet kilitli) | `#degismez-ipucu-MasakKimlikTespiti` · anahtar **MASAK** + yol: "yeni işlem yeni kimlik tespitiyle kaydedilir; eski kayıt saklama süresi sonunda maskelenir" |
| **/uyum — şüpheli bildirim** | durum geçişi: **Gönderildi** (`#uy-gonderildi-{id}` → PATCH `/masak/supheli/{id}/gonderildi`), **Kapat** (`#uy-kapat-{id}` → PATCH `…/kapat`, gerekçe ≥ 20) | onay modalı + `#uy-gonderim-onayla` / `#uy-kapatma-onayla` | tutar/gerekçe için `#degismez-ipucu-MasakSupheliBildirim-{id}` · anahtar **MASAK** + yol: "yanlış açılan bildirim gerekçesiyle KAPATILIR, doğrusu yeni bildirimle açılır" |
| **/uyum — yeni bildirim formu** | tutar `#sb-tutar`, hareket `#sb-hareket`, gerekçe `#sb-gerekce` (POST `/masak/supheli`) | `#sb-kaydet` | kilitliyken sebebi `#sb-kaydet-kilit`; alan altı mesajlar `#sb-tutar-hata`, `#sb-gerekce-hata` |
| **/raporlar** | dönem seçicisi (yıl/ay), gün seçicisi `#mut-tarih` | `#rp-kar-zarar` → kârın tek yetkili kaynağı | `#degismez-ipucu-Rapor` · anahtar **TURETILMIS** + yol: "rakamın kaynağı olan işlem kendi ekranından ters kayıtla düzeltilir" |
| **/raporlar/kar-zarar** | tarih aralığı `#kz-bas`/`#kz-bit`, kısayollar `#kz-bu-ay`/`#kz-gecen-ay`/`#kz-bugun` | — | `#degismez-ipucu-KarZarar` · **TURETILMIS** |
| **/raporlar/vade-takibi** | ufuk `#vade-ufuk` + kısayollar `#vade-ufuk-7/30/60` | müşteri → `#vade-cari-{id}` (cari kartı), telefon → `#vade-tel-{id}` (`tel:`) | `#degismez-ipucu-VadeTakibi` · **TURETILMIS** |
| **/denetim-izi** | yalnız süzgeçler (`#f-baslangic`, `#f-bitis`, `#f-eylem`, `#f-varlik`, `#f-sube`, `#f-yalniz-hassas`) | — | `#degismez-ipucu-DenetimKaydi` · anahtar **DENETIM** — "yalnız eklenir, hiç değiştirilmez". Sayfada hiç "Düzenle" düğmesi yok (test bunu tarar). |

**Finansal varlıkta hard DELETE YOK**: bu grubun hiçbir ekranına DELETE bağlanmadı;
gider pusulası ve MASAK kayıtları append-only kaldı.

### Storno düğmeleri (48-A) — bu grupta NEDEN YOK
48-A `StornolanabilirTurler` = `ISCILIK · VERESIYE · TAHSILAT · EMANET_GIRIS ·
EMANET_CIKIS`; `ErtelenenTurler` = `TAKAS · **HURDA_ALIS** · TRANSFER`.
Gider pusulasının kaynağı **HURDA_ALIS**'tır → storno ucu 409 döner.
MÜHÜR 2 "storno taklidi (ters iade) YASAK" gereği ekrana **sahte storno düğmesi
konmadı**; yerine 48-A devir koşulunun istediği **gerçek gerekçe** yazıldı
(stok + maliyet partisi geri alması gerektiği için ters kayıt üretilemez).
Diğer altı sayfa defter satırı listelemiyor (rapor/uyum/denetim) → storno düğmesi
konusu düşüyor. Kullanıcıya iç paket/dalga/ADR kodu **sızdırılmadı** — bunu
`GerekceDurust()` yardımcısı 7 testte tarar.

---

## 2. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **Gider pusulası için PATCH/POST ucu YAZILMADI** (QA turu 2'nin KRİTİK bulgusu "kayıt düzenlenemiyor, Yeni de yok") | (a) Görev koşulu: "eksik uç bulunursa YAZMA — **UX-1-B boşluğu** olarak listele, DegismezIpucu ile gerekçe koy (fail-closed)". (b) UX-1 spesifikasyonu gider pusulasını **kapsam dışı** bırakır: `DuzeltmePolitikasi` haritasında `T("GiderPusulasi", DEFTER)` = STORNO politikası, DUZENLENEBILIR değil. Ekran politikanın söylediğini yapar. → **UX-1-B boşluğu #1** (aşağıda) |
| S2 | **MASAK kimlik tespiti / şüpheli bildirim için düzenleme ucu YAZILMADI** (QA KRİTİK) | UX-1 spesifikasyonu, "Kapsam → Dışında" bölümü: *"MASAK kayıtlarına düzeltme yolu — **mevzuat gerekçesi araştırılmadan açılmaz**"*. Politika haritası da `S("Masak…", MASAK)` = SALT_OKUNUR. Gerekçe + gerçek düzeltme yolu (yeni tespit / bildirimi kapatıp yenisini açma) ekrana yazıldı. → **kurucuya/mali müşavire not 1** |
| S3 | ProAlan bu grupta **kullanılmadı** | Grup 3'te en büyük form 3 alanlıdır (sözleşme tavanı 8). Uyum formuna denendi, `UyumEkranTestleri` `ArayuzModuServisi`'ni kaydetmiyor ve **mevcut test değiştirilemez** (K4) → ProAlan geri alındı, alan açıkça "(isteğe bağlı)" etiketiyle görünür kaldı. |
| S4 | `#sb-tutar` alanında `@bind:event="oninput"` **kullanılmadı** | Mevcut `UyumEkranTestleri` bu alanı `onchange` ile sürüyor; `oninput`'a çevirmek o testi kırdı. Mevcut davranış korundu (K4), alan altı mesaj `onchange` sonrası çıkar. |
| S5 | `VarlikAdlari["Cari"] = "Cari"`, `["Kasa"] = "Kasa"` | QA bulgusu **teknik sınıf adları** hakkındaydı (`KurSnapshot`, `RefreshToken`, `StokPartiTuketimi`). "Cari" ve "Kasa" zaten uygulamanın Türkçe terimleridir (menü: Cariler / Kasalar) ve `EkranTestleri.Varlik_filtresi_sunucudan_gelen_turlerle_dolar` onları birebir bekler. Çeviri bu iki terimi **değiştirmez**. |
| S6 | `Bicim.Has` / `Bicim.Tl` kültür karışıklığı **düzeltilmedi** | `Bicim.cs` 27 sayfanın ortak dosyasıdır ve InvariantCulture seçimi SVG/CSV sözleşmesine bağlıdır (değişmez listesi). Tek dokunuş kuralı gereği bu pakette dokunulmadı → §6'da **ERTELENDİ**. |

---

## 3. Migration

**Gerekmedi.** Bu paket şema değiştirmedi: `tenants.adres/vergi_dairesi/vergi_no/
telefon` kolonları UX-1-B'de (`IsletmeKimlikBilgileri`) gelmişti; bu paket yalnız
onları **belge yanıtına** taşıdı. `kuyumcu_net` migration sayısı değişmedi (33).

---

## 4. Testler (önce / sonra)

| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 uyarı / 0 hata** (`scratchpad/b3.log`) |
| Filtreli: `UX1C3EkranTestleri` | 30 / 30 yeşil |
| Filtreli: `UX1C3BelgeTestleri` (gerçek MySQL) | 2 / 2 yeşil |
| **Tam süit** `--logger trx` → `scratchpad/trx-pUX-1-C3c` | **Toplam 1798 · Geçti 1798 · Başarısız 0 · Atlanan 0** |
| Önceki toplam (UX-1-C2 raporu) | 1766 → **+32** |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (satır içi style 149/196 **artmadı**; Pages record 122/122 **artmadı**; ModalPencere 42/45; duzenlenenId 1/1) |

**Ara arıza (Madde 1 — gizlenmedi):** ilk tam süitte **2 test kırmızıydı**
(`EkranTestleri.Varlik_filtresi_sunucudan_gelen_turlerle_dolar`,
`UyumEkranTestleri.Bildirim_formunda_300_karakterden_kisa_gerekce_KAYDEDILEMEZ`).
İkisi de benim değişikliğimin regresyonuydu; **testler değiştirilmedi**, kod
düzeltildi (S3, S4, S5). İkinci tam süit 1798/1798.

### Yeni test dosyaları
- `dotnet/Kuyumcu.Tests/UX1C3EkranTestleri.cs` — 30 bUnit testi (7 sayfanın
  `SayfaBasi`'sı, Cekmece sözleşmesi + Esc, DegismezIpucu pozitif/negatif çifti,
  ham kod taraması, UX-4d alanlarının çizimi, alan altı mesaj + kilit sebebi).
- `dotnet/Kuyumcu.Tests/UX1C3BelgeTestleri.cs` — 2 DB testi (belgede işletme
  kimliği basılır / kart boşsa **uydurulmaz**, null döner).

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/UX-1-C3/` altına kopyalandı → `perl -0777 -pi -e`
ile TEK eşleşmeli değişiklik → filtreli test → yedek geri kopyalandı → `cmp` ile
birebir aynılık kanıtlandı. **14/14 kırmızı, 14/14 `cmp` BİREBİR AYNI.**

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M-C3-01 | `GiderPusulalari`: `<DegismezIpucu Varlik="GiderPusulasi"` çizilmez hâle getirildi | `C3_11_Pusula_detayinda_DUZENLE_dugmesi_YOK_gerekce_VAR` |
| M-C3-02 | `GiderPusulalari`: Çekmece `KaydetAktif="false"` → `@(true)` | `C3_10_Pusula_detayi_CEKMECEDE_acilir_ve_Kaydet_KILITLIDIR` |
| M-C3-03 | `GiderPusulalari`: `SaticiTipiAdi(d.SaticiTipi)` → ham `@d.SaticiTipi` | `C3_12_Pusula_detayinda_HAM_KOD_basilmaz` |
| M-C3-04 | `HurdaModulu`: `adres = tenant?.Adres` → `(string?)null` | `UX1C3BelgeTestleri.Belgede_isletme_ADRESI_ve_VERGI_bilgisi_basilir` |
| M-C3-05 | `PusulaBelgesi`: `IsletmeEksik` hep `false` | `C3_21_Isletme_bilgisi_EKSIKSE_uydurulmaz_doldurma_yolu_gosterilir` |
| M-C3-06 | `Uyum`: kimlik tespiti `DegismezIpucu`'su çizilmez | `C3_30_Kimlik_tespiti_CEKMECEDE_acilir_gerekce_VAR_duzenle_YOK` |
| M-C3-07 | `Uyum`: `#sb-tutar-hata` id'si kaldırıldı (alan altı mesaj kaybolur) | `C3_34_Yeni_bildirimde_GECERSIZ_tutar_alan_alti_mesaj_verir` |
| M-C3-08 | `Raporlar`: kâr kartının `Alt="@KarKaynagiMetni"` → `@(null)` | `C3_40_Kar_karti_KAYNAGINI_yazar_ve_satis_alis_farki_AYRI_karttir` |
| M-C3-09 | `Raporlar`: `NetHas` yeniden eski alandan türetiliyor (`netHasGramHareketi` okunmaz) | `C3_41_Net_has_GUNLUK_ile_ayni_tanimdan_cizilir` |
| M-C3-10 | `Raporlar`: `#mut-bagsiz-faturalar` bloğunun id'si kaldırıldı | `C3_44_Gun_sonu_FARK_TANIMI_ve_bagsiz_faturalar_gorunur` |
| M-C3-11 | `KarZarar`: adet `ToString("N0")` kaldırıldı | `C3_50_Adet_para_gibi_bicimlenmez` |
| M-C3-12 | `VadeTakibi`: `tel:` bağlantısı `href="#"` yapıldı | `C3_60_Musteri_adi_cari_kartina_telefon_aramaya_gider` |
| M-C3-13 | `DenetimIzi`: `VarlikAdi(t)` → ham `@t` | `C3_70_Kayit_turu_TEKNIK_sinif_adi_olarak_basilmaz` |
| M-C3-14 | `Raporlar`: `SayfaBasi Anahtar="raporlar"` → `"rapor"` (id sözleşmesi bozulur) | `C3_03_Raporlar_sayfa_basi_vardir` |

**Test açığı bulundu ve kapatıldı (Madde 1):** M-C3-11 ilk denemede **hiçbir testi
kırmadı** — sahte API `"maliyetiBilinenAdet":5` döndürüyordu, `decimal 5` zaten "5"
basıyordu. Fikstür gerçek sunucu davranışına (`5.000`, ölçekli decimal) çevrildi;
mutasyon tekrarlandı → **kırmızı**. Mutasyonlardan sonra temiz `dotnet build`:
**0 uyarı / 0 hata**.

---

## 6. QA turu 2 (grup 3) bulgu tablosu — BAĞLAYICI

> ⚠ QA turu 2, **dalga 2a yayınına** (5199, 16/09 15:53 anlık görüntüsü) bakmıştı.
> Dalga 2b'nin (UX-4d) düzelttiği maddeler o turda hâlâ AÇIK görünüyor; aşağıda
> hangisinin **bu pakette**, hangisinin **2b'de** kapandığı ayrı yazıldı.

### /gider-pusulalari
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| KRİTİK — kayıt hiçbir şekilde düzenlenemiyor; 'Yeni' de yok | **KISMEN KAPANDI (gerekçe yolu)** + **ERTELENDİ (uç)** | Düzenleme yolu yerine UX-S25 gerekçesi + gerçek düzeltme yolu + "Hurda alımı gir" birincil eylemi geldi (`C3_11`, `C3_10`). Yerinde PATCH **açılmadı** → UX-1-B boşluğu #1, hedef: ayrı oy (bkz. §9) |
| DÜŞÜK — Stopaj listede "—", detayda "0,00 ₺ (%0)" | **KAPANDI** | `C3_13_Stopaj_sifirken_listede_de_TUTAR_yazar` |
| DÜŞÜK — Detayda ham enum `NIHAI_TUKETICI` | **KAPANDI** | `C3_12` |
| DÜŞÜK — Dialog Escape ile kapanmıyor | **KAPANDI** | Modal → `Cekmece` (Esc + odak hapsi): `C3_32_Cekmece_ESCAPE_ile_kapanir` |
| ORTA — dar ekranda yatay kaydırma (üst çubuk) | **ERTELENDİ** → **UX-2-A2** (ortak kabuk; bu sayfanın borcu değil) | ortak üst çubuk/menü düzeltmesi 2c'nin UX-2-A2 paketinde |

### /gider-pusulalari/{Id}/belge
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| DÜŞÜK — yalnız görüntüleme "yazdırıldı" olarak düşüyor | **KAPANDI (UX-4d md.20, dalga 2b)** | canlı denetim kaydı: `OKUMA · GiderPusulasi · "gider pusulası belgesi görüntülendi"` (§7 adım 6) |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |
| (yeni) VUK md.234 düzenleyen bloğu eksik | **KAPANDI** | `UX1C3BelgeTestleri` + canlı belge yanıtı (§7 adım 5) |

### /uyum
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| YÜKSEK — belge no aramasında `DeserializeUnableToConvertValue` | **KAPANDI (UX-4d md.18, dalga 2b)** | `UyumServisi.cs:43` zarfı okuyor; `UX4dEkranTestleri.M18_*` |
| YÜKSEK — "Gönderildi"/"Kapat" düğmeleri sessizce hiçbir şey yapmıyor | **KAPANDI (UX-4d md.10, dalga 2b) + canlıda doğrulandı** | §7 adım 7: PATCH 200, SQL `durum=GONDERILDI, gonderim_referansi=MASAK-REF-2026-0001` |
| KRİTİK — kimlik tespiti ve bildirim kayıtları düzenlenemiyor/silinemiyor | **KISMEN KAPANDI (gerekçe + gerçek yol)** + **ERTELENDİ (uç)** | `C3_30`, `C3_33`. Mevzuat gerekçesi araştırılmadan uç açılmaz (UX-1 kapsam dışı) → §9 not 1 |
| ORTA — 0 sonuçta liste eski hâliyle kalıyor | **KAPANDI (UX-4d md.18)** | `UX4dEkranTestleri.M18_Belge_no_aramasi_0_sonucta_BOS_DURUM_gosterir` |
| ORTA — geçersiz formda Kaydet kilitli ama sebebi yok | **KAPANDI** | `#sb-tutar-hata`, `#sb-gerekce-hata`, `#sb-kaydet-kilit` (`C3_34`) |
| DÜŞÜK — kimlik tespiti modalında not etiketi yanlış metin | **KAPANDI** | `#uy-beyan-aciklama-etiket` = "İşlem açıklaması" |
| DÜŞÜK — gerekçe listede kesiliyor, tam metin yok | **ERTELENDİ** → UX-5 (bildirim detay çekmecesi; bu pakette ikinci bir çekmece açmak "tek düzenleme kalıbı" bütçesini aşardı) | — |
| DÜŞÜK — kimlik tespiti dialogu Escape ile kapanmıyor | **KAPANDI** | `C3_32` |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |

### /raporlar
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| YÜKSEK — gün sonu mutabakatı kendi içinde çelişiyor (fatura ≠ faturalı sayısı, FARK açıklanmıyor) | **KAPANDI** | `#mut-fark-tanimi` + bağlı/bağsız fatura satırları + `#mut-bagsiz-faturalar` listesi (`C3_44`); canlı: `faturaEslesme` zarfı (§7 adım 8) |
| ORTA — "NET HAS GRAM" kartının anlamı belirsiz, panelle uyumsuz | **KAPANDI** | kart adı "Net has gram hareketi" + formül alt metni, kaynak `netHasGramHareketi` (`C3_41`) |
| ORTA — aynı ay için iki farklı "kâr" | **KAPANDI** | kâr = `KarZararServisi` (kaynak rozetiyle), eski rakam ayrı kartta "Alış-satış nakit farkı" (`C3_40`); hesaplanamazsa "—" (`C3_43`) |
| ORTA — aylık API veresiye 0 / tahsilat kartta yok | **KAPANDI (ekran tarafı)** | Tahsilat ve Veresiye kartları eklendi (`C3_42`). ⚠ Sunucu `veresiye.toplamTl` hesabı bu paketin kapsamı değil — rakam sunucudan ne gelirse o basılır (uydurma yok) |
| DÜŞÜK — sayı biçimi karışık (has noktalı, TL virgüllü) | **ERTELENDİ** → ortak `Bicim.cs` (S6) | — |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |

### /raporlar/kar-zarar
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| DÜŞÜK — "Maliyeti bilinen: 5,000" adet para gibi | **KAPANDI** | `C3_50` (+ mutasyon M-C3-11) |
| DÜŞÜK — geçersiz aralık sunucuya gidiyor | **KAPANDI (UX-4d md.23, dalga 2b)** | `UX4dEkranTestleri.M23_*`, `#kz-aralik-hata` |
| ORTA — HAS hasılatı aylık API ile uyuşmuyor (24,8404 g vs 23,657) | **ERTELENDİ** → **34-A/35-A** (has hesabının tek serviste toplanması sunucu işidir; ekran uydurma yapamaz) | — |
| DÜŞÜK — ürün adı ile "maliyet eksik" rozeti bitişik okunuyor | **KAPANDI** | ad `<span>` içine alındı + rozete `aria-label` |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |

### /raporlar/vade-takibi
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| ORTA — 7 günlük ufukta üç kart da ₺0,00, toplam açık alacak yok | **KAPANDI (UX-4d md.16, dalga 2b)** | `UfukOtesiToplam` + `AcikToplam` kartları ("dönemden bağımsız" alt metniyle) |
| DÜŞÜK — telefon tıklanabilir değil, ad cari kartına gitmiyor | **KAPANDI** | `#vade-tel-{id}` (`tel:`) + `#vade-cari-{id}` (`C3_60`, mutasyon M-C3-12) |
| DÜŞÜK — ufuk filtresi açıklamasız, 7 gün varsayılanı gizli | **KAPANDI** | `#vade-ufuk-7/30/60` kısayolları + `#vade-ufuk-aciklama` (`C3_61`) |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |

### /denetim-izi
| Bulgu | Sonuç | Kanıt |
|---|---|---|
| YÜKSEK — filtre değişiminde çift istek → çift satır | **KAPANDI (UX-4d md.22, dalga 2b)** | `sonSorgu` kapısı + `UX4dEkranTestleri.M22_*` |
| ORTA — ters tarih aralığı sessizce kabul ediliyor | **KAPANDI (UX-4d md.23)** | `#f-aralik-hata` |
| DÜŞÜK — "QA TestSAHIP" bitişik okunuyor | **KAPANDI** | ad `<span>`, rol `Rozet` + `aria-label` |
| DÜŞÜK — onay kutusunun erişilebilir adı "on" | **KAPANDI** | `#f-yalniz-hassas` + `aria-label` (`C3_72`) |
| DÜŞÜK — kayıt türü listesi teknik entity adları | **KAPANDI** | `SayfaDuzeltme.VarlikAdi()` (41 ad, TR+EN) — `C3_70`, mutasyon M-C3-13 |
| ORTA — dar ekranda yatay taşma | **ERTELENDİ** → UX-2-A2 | — |

---

## 7. Canlı doğrulama (5198) — HTTP + SQL

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --urls http://localhost:5198`
(PID 53170). Test kiracısı: **PAKETUX1C3-TEST Kuyumculuk** (`01M2Q68MMRE4BCY9DE9FK277BM`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201**, tenantId `01M2Q68MMRE4BCY9DE9FK277BM` |
| 2 | `GET /kur/guncel` | 200, kur doğdu | **200**, `01M2Q692J0PZDHJC9W2XPSYK0T` (altınHas 6794,10) |
| 3 | `PATCH /admin/tenant/kimlik` | 200, kimlik yazıldı | **200** — `adres/vergiDairesi/vergiNo/telefon` yanıtta |
| 4 | `POST /islemler/hurda-alis` (K22, 54,6 g, NIHAI_TUKETICI) | 200 + pusula doğar | **200**, `GP-2026-000001`, brüt 74.020,13 TL, stopaj 0,00 |
| 5 | `GET /gider-pusulalari/{id}/belge` | `duzenleyen` bloğunda adres + vergi dairesi/no + telefon | **200** — `{"unvan":"PAKETUX1C3-TEST Kuyumculuk","sube":"Merkez Şube","adres":"Kuyumcular Carsisi No:5 Fatih/Istanbul","vergiDairesi":"Fatih","vergiNo":"1234567890","telefon":"02125550000"}`; yazıyla "YETMİŞDÖRTBİNYİRMİ TL ONÜÇ KR" |
| 6 | Denetim izi (SQL) | görüntüleme ≠ yazdırma | `OKUMA · GiderPusulasi · "gider pusulası belgesi görüntülendi"` (yazdırıldı YAZILMADI) |
| 7 | `POST /masak/supheli` → `PATCH /masak/supheli/{id}/gonderildi` | 201 → 200, durum değişir | **201 → 200**; SQL: `durum=GONDERILDI · gonderim_referansi=MASAK-REF-2026-0001 · gonderim_tarihi dolu` |
| 8 | `GET /raporlar/pos-fatura-mutabakati?tarih=2026-09-17` | `faturaEslesme` + `farkTanimi` döner | **200** — `farkTanimi = "FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar"`, `bagimsizFaturalar: []` |
| 9 | `GET /raporlar/aylik?yil=2026&ay=9` | UX-4d alanları döner | **200** — `karKaynagi=KAR_ZARAR_SERVISI · karTl=0 · satisAlisFarkiTl=0 · netHasGramHareketi=50.0136` (alınan hasla birebir) |
| 10 | Temizlik | tenants başlangıç değerine döner | API PID 53170 durduruldu (`lsof -iTCP:5198` **boş**); 39 tablo + `tenants` silindi; **tenants 8 → 8**; artık satır sayımı `gider_pusulalari/masak_supheli_bildirimler/hareketler = 0 / 0 / 0` |

`QA-TEST-1/2/3` kalıcı fikstür kiracılarına **dokunulmadı**.

**Tarayıcı turu (375/768/1280 px `scrollWidth ≤ clientWidth`) bu raporda
yapılmadı:** canlı tarayıcı turu QA ajanınca yapılır (QA turu 3). Yukarıdaki
HTTP + SQL kanıtı uygulayıcı tarafından üretilmiştir.

---

## 8. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` (bu paketin dokunduğu satırlar):

```
dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs        (belge "düzenleyen" bloğu)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                  (SozlukSayfaDuzeltme TR+EN + Pusula.HurdaAlimiGir)
dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs            (BelgeDuzenleyen genişletildi, MutabakatFaturaEslesme)
dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor
dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor
dotnet/Kuyumcu.Web/Pages/Uyum.razor
dotnet/Kuyumcu.Web/Pages/Raporlar.razor
dotnet/Kuyumcu.Web/Pages/KarZarar.razor
dotnet/Kuyumcu.Web/Pages/VadeTakibi.razor
dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor
dotnet/Kuyumcu.Tests/UX1C3EkranTestleri.cs             (YENİ)
dotnet/Kuyumcu.Tests/UX1C3BelgeTestleri.cs             (YENİ)
```

**Dokunulan sayfalar (MÜHÜR 2 — TEK DOKUNUŞ kaydı):**
`GiderPusulalari.razor · PusulaBelgesi.razor · Uyum.razor · Raporlar.razor ·
KarZarar.razor · VadeTakibi.razor · DenetimIzi.razor` — yedisi de bu dalgada
**ilk kez** açıldı. Bu sayfalar başka bir dalgada tekrar açılırsa kapı kaydına
gerekçe yazılması zorunludur.

**id envanteri diff'i (K4):** silinen/değişen id **YOK**; yalnız EKLENEN:
`gp-hurda-alimi · gp-yazdir-{id} · gpb-geri · gpb-yazdir · gpb-duzenleyen-adres ·
gpb-duzenleyen-vergi · gpb-duzenleyen-telefon · gpb-isletme-karti ·
uy-yeni-bildirim · uy-yeni-bildirim-sekme · uy-beyan-aciklama-etiket ·
sb-kaydet · sb-kaydet-kilit · sb-tutar-hata · sb-gerekce-hata ·
rp-kar-zarar · mut-fark-tanimi · mut-bagsiz-faturalar ·
kz-bu-ay · kz-gecen-ay · kz-bugun · vade-ufuk-7/30/60 · vade-ufuk-aciklama ·
vade-cari-{id} · vade-tel-{id} · f-yalniz-hassas`
artı bileşenlerin ürettiği `sayfa-basi-{anahtar}*`, `cekmece-gider-pusulasi*`,
`cekmece-kimlik-tespiti*`, `degismez-ipucu-*`.
İki `ModalPencere` `Cekmece`'ye çevrildi; ikisi de adlandırılmış id taşımıyordu
(ModalPencere yalnız otomatik `BaslikId` üretir), içteki alan id'leri **aynen**
kaldı.

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| MÜHÜR 2 önkoşulu: 48-A + 45-API "GEÇTİ" okundu | ✅ | §0 |
| Her sayfada `<SayfaBasi>` (başlık + araç çubuğu), mevcut başlık DOM'u sarıldı | ✅ | `C3_01`–`C3_07`; KarZarar'ın `div.sayfa-baslik` bloğu SayfaBasi'na taşındı, metinler korundu |
| Her veri girişi için düzenleme yolu ya da DegismezIpucu | ✅ | §1 tablosu |
| Çok alanlı düzenleme **Cekmece** (mevcut modal kabuğu çevrildi, iç id'ler aynı) | ✅ | `#cekmece-gider-pusulasi`, `#cekmece-kimlik-tespiti` (`C3_10`, `C3_30`) |
| İkinci `duzenlenenId` kalıbı açılmadı | ✅ | denetim.sh `duzenlenenId 1 (tavan 1)` |
| Pages'te yeni `ModalPencere` düzenleme kullanımı artmadı | ✅ | denetim.sh `ModalPencere 42 (tavan 45)` — 2 azaldı |
| Storno: defter kayıtlarında 48-A ucu; kapsam dışında disabled + gerçek gerekçe; "ters iade" taklidi yok | ✅ | §1 "Storno düğmeleri" — bu grupta kaynak tür HURDA_ALIS (48-A ertelenen listesi) |
| PASİFE AL / hard DELETE yok (finansal varlık) | ✅ | denetim.sh `Finansal varlıkta [HttpDelete] 0` |
| Her düğme ya istek atar ya disabled + sebep (DoD EK md.1) | ✅ | `C3_10` (kilit sebebi), `C3_34` (`#sb-kaydet-kilit`), `C3_61` (kısayol gerçek istek atar) |
| Her form: geçersiz gönderimde alan altı Türkçe mesaj (DoD EK md.3) | ✅ | `C3_34` |
| Yıkıcı eylem ortak onay modalı (DoD EK md.4) | ✅ | Gönderildi/Kapat onay modalları korundu (UX-4d md.10) |
| Sözlükler TR + EN, çıplak metin yok | ✅ | `SozlukSayfaDuzeltme` (41 varlık adı + 5 belge türü + tüm metinler TR+EN) |
| MÜHÜR 5 KOŞUL 3: Pages'te yeni istemci record YOK | ✅ | denetim.sh `Pages içinde istemci record tanımı 122 (tavan 122)` |
| Sayfaya yeni `Api.*` çağrısı eklenmedi (TB-09) | ✅ | Yeni uç çağrısı yok; `UyumServisi`/`ApiIstemci`'nin mevcut metotları kullanıldı |
| Satır içi `style=""` yazılmadı, sabit renk yok | ✅ | denetim.sh `satır içi style 149 (tavan 196)` — **artmadı** |
| Mevcut testler değiştirilmedi | ✅ | Yalnız 2 YENİ test dosyası eklendi; regresyonlar **kod tarafında** çözüldü (S3–S5) |
| Tam süit 0 başarısız | ✅ | 1798/1798 |
| `denetim.sh` TEMİZ | ✅ | §4 |
| Test kiracısı silindi, tenants sabit | ✅ | 8 → 8 (§7 adım 10) |
| `QA-TEST-*` fikstürlerine dokunulmadı | ✅ | tenants listesi §7 |
| UX-4d devri (Raporlar ekran bağlama) | ✅ | `C3_40`–`C3_45` + canlı §7 adım 8-9 |
| UX-1-B devri (belgede işletme kimliği) | ✅ | `UX1C3BelgeTestleri` + canlı §7 adım 5 |

---

## 10. Açık kalanlar

1. **UX-1-B boşluğu #1 — gider pusulası yerinde düzeltme ucu yok.** Bugün
   politika STORNO/DEFTER; kaynak hareket türü HURDA_ALIS ise 48-A onu
   **ertelenen** listesinde tutuyor (stok + maliyet partisi geri alması). Yani
   kuyumcunun yanlış girdiği bir hurda alımı **hiçbir yoldan** düzeltilemiyor.
   Bu, 48-A raporundaki "TAKAS/HURDA_ALIS/TRANSFER stornosu ayrı paket (~1,5-2
   gün, ayrı oy)" sapmasının doğrudan kullanıcı yüzüdür. **Ekip oyuna gitmeli.**
2. **UX-1-B boşluğu #2 — MASAK kayıtlarında düzeltme ucu yok** (kimlik tespiti
   alanları, açık bildirimin tutar/gerekçesi). UX-1 spesifikasyonu mevzuat
   araştırması olmadan açılmasını yasaklıyor → §11 not 1.
3. **Ortak sayı biçimi** (`Bicim.Has` InvariantCulture vs `Bicim.Tl` tr-TR) —
   27 sayfayı ilgilendirir, tek dokunuş kuralı gereği ertelendi. Hedef: UX-5.
4. **Dar ekran (≤768 px) taşması** — 7 rotanın hepsinde ORTA bulgu; ortak kabuk
   işi, UX-2-A2.
5. **Aylık özet `veresiye.toplamTl` / `tahsilat.toplamTl` doğruluğu** sunucu
   tarafıdır; ekran artık bu alanları çiziyor, rakamı **uydurmuyor**. QA turu
   2'nin "ekranda 98.300 veresiye var ama API 0 diyor" bulgusunun kökü
   `RaporModulu` sorgusudur — 34-A/35-A kapsamında doğrulanmalı.
6. **Kâr-zarar HAS hasılatı ile aylık API `satis.toplamHas` farkı** (24,8404 g
   vs 23,657) — iki ayrı sunucu hesabı; tek servise indirilmesi 34-A/35-A.
7. **`.mutorj` artıkları** ağaçta duruyor (`Pages/Fiyatlandirma.razor.mutorj`,
   `Islemler`, `Urunler`, `Sayim`, `VadeTakibi`, `Modules/Satis/SatisModulu.cs.mutorj`).
   Ajan silmez (kurucu kalemi 4) — **kurucu silmeli**.

---

## 11. Kurucuya / mali müşavire notlar

1. **MASAK kayıtları düzeltilebilir mi?** (mali müşavir + hukuk sorusu)
   Bugünkü seçim **fail-closed**: kimlik tespiti ve şüpheli bildirim kaydı
   oluşturulduktan sonra içeriği değiştirilemez; yanlış bildirim gerekçesiyle
   KAPATILIR, doğrusu yeni bildirimle açılır. 5549 s. Kanun ve MASAK Rehberi
   düzeltme/iptal yolu tanımlıyorsa ekrana açılabilir — **ekip bunu varsaymadı**.
2. **Gider pusulasında VUK md.234 alanları.** Belge artık işletme adresi ve
   vergi dairesi/numarasını basıyor. Bu alanlar `tenants` kartından gelir;
   **kart boşsa belge eksik basılır** (ekran uyarıyor, ama yazdırmayı
   engellemiyor). Mali müşavir "eksik belge basılmasın" derse bir kapı eklenir —
   bugün engellenmiyor, çünkü bu bir **karardır**, varsayım değil.
3. **Hurda alımının düzeltilmesi** (açık kalan 1) ticari olarak en can yakıcı
   boşluktur: yanlış gram/ayar girilen bir hurda alımı bugün geri alınamıyor.
   Ayrı paket + ayrı oy gerekiyor.
4. **Ücretsiz pilot kuralı** (MÜHÜR 2) korundu: bu pakette fiyat/teklif/tahsilat
   içeren hiçbir kullanıcı metni üretilmedi.

---

# EK — DOĞRULAMA BULGULARININ KAPATILMASI (2026-09-17, ikinci geçiş)

Hasım doğrulayıcının 5 bulgusu ele alındı. Aşağıdaki tablo her bulgunun
durumunu ve **kanıtını** verir.

| # | Bulgu | Durum | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** Gider pusulası `DegismezIpucu`'su VAR OLMAYAN düzeltme yolu gösteriyor (sunucu 409 veriyor) | **KAPANDI** | Sözlük metni TR+EN gerçeğe çekildi; ekran gerekçesi `DefterGerekce` → `StornoYokGerekce`; politika damgası `STORNO/DEFTER` → `SALT_OKUNUR/STORNO_YOK`; canlı: `POST /islemler/{id}/storno` → **409** |
| 2 | **[ORTA]** `GerekceDurust()` yalnız sızıntı tarıyor, metnin DOĞRULUĞUNU taramıyor (test açığı) | **KAPANDI** | Yeni sözleşme testi dosyası `Kuyumcu.Tests/UX1C3DuzeltmeSozlesmeTestleri.cs` (5 test) — metin ↔ `StornoServisi.ErtelenenTurler` / `DuzeltmePolitikasi` karşılaştırması; 2 mutasyon kırmızıya döndürdü |
| 3 | **[ORTA]** `Faturalar.razor` `id="fatura-hasdeger"` düşmüş (K4 ihlali, önceki dalga borcu) | **ERTELENDİ** | Bu paketin dosya ayak izinde değil; **tek dokunuş** kuralı gereği Faturalar.razor açılmadı. Hedef: C1/C2 kapı kaydı (aşağıda "orkestratöre" notu) |
| 4 | **[DÜŞÜK]** 8 adet 0 baytlık migration + 16 `.mutorj` artığı | **ERTELENDİ — kurucu kalemi** | Ajanın silme yetkisi yok (hook `rm`/`mv` bloklar). Bu geçiş yeni artık eklemedi |
| 5 | **[DÜŞÜK]** ≤768 px canlı tarayıcı doğrulaması yapılmadı | **ERTELENDİ** | Tarayıcı turu QA ajanınındır (QA turu 3 / UX-2-A2); bu geçişte de tarayıcı kullanılmadı |

## E.1 Bulgu 1 — yapılan değişiklik (yalanın kaldırılması)

**Önce** (`Sozlukler.cs:2779`, TR):
> "… Yanlış bilgi varsa: İşlemler ekranından alım işlemi **ters kayıtla
> düzeltilir** ve yeni pusula düzenlenir."

**Sonra**:
> "Gider pusulası, hurda alımı işlemiyle birlikte doğar ve basıldıktan sonra
> içeriği değiştirilmez (VUK md.234). Hurda alımı için sistemde ters kayıt
> (storno) yolu **BULUNMUYOR**: bu kayıt bugün sistem içinden hiçbir şekilde
> düzeltilemiyor. Hatalı pusula için **mali müşavirinize** başvurun."

EN karşılığı (`:4226`) aynı anlamda yeniden yazıldı ("There is NO reversing-entry
(void) path … cannot be corrected in-system today. Contact your accountant…").

Ayrıca **iki tutarsızlık daha** aynı kökten düzeltildi:

1. `GiderPusulalari.razor:167` — gerekçe `Dil.T.Duzeltme.DefterGerekce` idi;
   o metin de "Düzeltme yalnız **ters kayıtla** yapılır" diyor. Hurda alımında
   ters kayıt yok → `Dil.T.Duzeltme.StornoYokGerekce` ("… ters kayıtla (storno)
   **hükümsüz kılınamaz**") ile değiştirildi.
2. `DuzeltmePolitikasi.cs:123` — `T("GiderPusulasi", Defter)` damgası
   **STORNO** politikası ilan ediyordu; yani `/meta/duzeltme-politikalari`
   ucu da istemciye olmayan bir yolu bildiriyordu. `S("GiderPusulasi",
   StornoYok)` (SALT_OKUNUR) yapıldı. `HedefUc` zaten null.

**Kapsam dürüstlüğü:** hurda alımının düzeltilmesi hâlâ **YOK** (açık kalan 1).
Bu geçiş yeni bir yetenek eklemedi; yalnız **ekranın yalanı kaldırıldı**.
48-A'nın `HURDA_ALIS/TAKAS/TRANSFER` storno paketi ekip oyuna götürülmelidir.

## E.2 Bulgu 2 — yeni sözleşme testleri

`Kuyumcu.Tests/UX1C3DuzeltmeSozlesmeTestleri.cs` (mevcut hiçbir test
değiştirilmedi — yalnız EKLEME, K4):

| Test | Ne ölçer |
|---|---|
| `Ertelenen_turler_STORNOLANABILIR_listesinde_DEGILDIR` | `StornoServisi` kendi içinde çelişmez |
| `Ertelenen_her_tur_politikada_STORNO_YOK_damgasi_tasir` | `StornoServisi` ↔ `DuzeltmePolitikasi` aynı gerçeği anlatır |
| `Storno_yok_gerekcesi_olmayan_bir_yol_vaat_etmez` | STORNO_YOK gerekçesi (TR+EN) vaat kalıbı içermez, "yapılamıyor" der |
| `Gider_pusulasi_ekran_metni_HURDA_ALIS_gercegiyle_uyusur` | `PusulaDuzeltmeYolu` (TR+EN) ↔ `ErtelenenTurler` sözleşmesi |
| `Gider_pusulasi_politikasi_olmayan_bir_storno_yolu_damgalamaz` | Politika damgası SALT_OKUNUR/STORNO_YOK/HedefUc=null |

Vaat kalıbı listesi (TR): `ters kayıtla düzeltilir/düzeltin`, `ters kayıt girilir`,
`storno edilir/edin/yapılır`, `iade fişi`, `düzeltme fişi`,
`yeni pusula düzenlenir`, `İşlemler ekranından` — EN eşleniği ayrı liste.

## E.3 Mutasyon kanıtı (ek)

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı |
|---|---|---|---|
| M-C3-15 | `PusulaDuzeltmeYolu` eski (yalan) metne döndürüldü | `UX1C3DuzeltmeSozlesmeTestleri.Gider_pusulasi_ekran_metni_HURDA_ALIS_gercegiyle_uyusur` (**FAIL**, 1/5) | evet — `grep` ile eski cümlenin 0 kez geçtiği doğrulandı |
| M-C3-16 | Politika damgası `T("GiderPusulasi", Defter)`'e döndürüldü | `UX1C3DuzeltmeSozlesmeTestleri.Gider_pusulasi_politikasi_olmayan_bir_storno_yolu_damgalamaz` (**FAIL**, 1/5) | evet — satır 123 `S("GiderPusulasi", GerekceAnahtarlari.StornoYok)`, derleme 0 uyarı 0 hata |

## E.4 Kapılar (ikinci geçiş)

| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı, 0 Hata** |
| Tam süit (trx) | **total=1803 executed=1803 passed=1803 failed=0** (önce 1798 → **+5**; hiçbir test düşmedi) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (satır içi style 149/196 — **artmadı**; ModalPencere 42/45; duzenlenenId 1/1; Pages/*.razor 30/30) |
| Migration | **gerekmedi** (şema değişmedi; DB `__EFMigrationsHistory` = 33) |
| K4 id envanteri | **değişmedi** — bu geçişte hiçbir id eklenmedi/silinmedi (yalnız metin + gerekçe anahtarı + politika damgası + yeni test dosyası) |

## E.5 Canlı doğrulama (5198, ikinci geçiş)

Test kiracısı: **PAKETUX1C3-TEST2 Kuyumculuk** (`01M2Q751JRF1VQ0D1C5KKNN617`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201** |
| 2 | `GET /kur/guncel` | 200, kur doğar | **200** (`altinHasFiyati` 6780,44) |
| 3 | `GET /meta/duzeltme-politikalari` | GiderPusulasi artık SALT_OKUNUR | **200** — `{"varlik":"GiderPusulasi","politika":"SALT_OKUNUR","gerekceAnahtari":"STORNO_YOK","hedefUc":null,"alternatifYol":null}` |
| 4 | aynı uç, hareket türü | HURDA_ALIS gerekçesi STORNO_YOK | `{"varlik":"HURDA_ALIS","politika":"STORNO","gerekceAnahtari":"STORNO_YOK","hedefUc":null}` |
| 5 | `POST /islemler/hurda-alis` (K22, 54,6 g, NIHAI_TUKETICI) | 200 + pusula | **200** — `GP-2026-000001`, has 50,0136 g, brüt 67.518,36 TL, stopaj 0,00 |
| 6 | **`POST /islemler/{hareketId}/storno`** (gerekçe 62 karakter) | ekrandaki eski metnin vaat ettiği yol **çalışmaz** | **409 Conflict** — `"HURDA_ALIS stornosu bu sürümde KAPALIDIR: … Kısmi geri alma yapılmaz (fail-closed)."` → **eski ekran metni kanıtlı biçimde yalandı** |
| 7 | Temizlik | tenants başlangıca döner | API PID 60438 durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**); 40 tenant_id'li tablo + `tenants` silindi; **tenants 8 → 8**; `hareketler/gider_pusulalari` artık satırı **0 / 0** |

SQL kanıtı (hareket kimliği): `select h.id, h.tur from hareketler h join tenants t
on t.id=h.tenant_id where t.ad like 'PAKETUX1C3-TEST2%'` →
`01M2Q763H0W43X0Y8RVJCJYEQY  HURDA_ALIS`.

`QA-TEST-` önekli kalıcı fikstür kiracılarına **dokunulmadı** (tenants 8 sabit).
Dar ekran (375/768/1280 px) ölçümü **yapılmadı** — tarayıcı turu QA ajanınındır.

## E.6 Bu geçişte değişen dosyalar

- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR:2779 · EN:4226 — `PusulaDuzeltmeYolu`)
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` (:119-123 — `GiderPusulasi` damgası)
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor` (:167 — gerekçe anahtarı)
- `dotnet/Kuyumcu.Tests/UX1C3DuzeltmeSozlesmeTestleri.cs` (**yeni**, 5 test)

## E.7 Orkestratöre / kurucuya notlar (ikinci geçiş)

1. **Kapı kaydına geçirilecek borç:** `Faturalar.razor` id `fatura-hasdeger`
   ağaçtan düşmüş, yerine `fom-hasdeger` var (`Faturalar.razor:238`). K4 gereği
   ya geri getirilmeli ya da yeniden adlandırma gerekçesi C1/C2 kapı kaydına
   yazılmalı. **Bu paket dosyayı açmadı** (tek dokunuş).
2. **Ekip oyu gereken karar:** 48-A'nın `HURDA_ALIS` / `TAKAS` / `TRANSFER`
   storno paketi. Bugün bu üç tür için sistem içinde **hiçbir** düzeltme yolu
   yok; ekran artık bunu dürüstçe söylüyor, ama boşluk ticari olarak duruyor.
3. **Kurucu kalemi (ajan yetkisi yok):** 8 adet 0 baytlık migration dosyası
   (`20260915131748/132123/132303/132645_FiyatListesiVeKampanya*`) ve 16 adet
   `.mutorj` artığı silinmeli.
4. **Mali müşavire:** VUK md.234 kapsamında hatalı düzenlenmiş bir gider
   pusulasının dışarıdan (beyanname/düzeltme yoluyla) nasıl düzeltileceği
   ekranda "mali müşavirinize başvurun" olarak yazıldı; kesin yol **varsayılmadı**.


---

# EK E.8 — ÜÇÜNCÜ GEÇİŞ (doğrulayıcı KRİTİK bulgusu: çekmece kilit sebebi)

> 2026-09-17 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend)

## E.8.1 Bulgu ve gerçek

Doğrulayıcı KRİTİK: 2. geçiş yalnız `DegismezIpucu` metnini (satır 167) düzeltti;
**çekmecenin kilit sebebi** (`GiderPusulalari.razor:142`) `Duzeltme.DefterGerekce`
olarak kaldı ve `Cekmece.razor:44-46` bunu `#cekmece-gider-pusulasi-kilit` içinde
kullanıcıya **"Düzeltme yalnız ters kayıtla yapılır."** diye bastı. Aynı çekmecede
25 satır arayla iki çelişkili cümle vardı. Sunucu gerçeği: `HURDA_ALIS` ∈
`StornoServisi.ErtelenenTurler` → storno **409**.

## E.8.2 Yapılan değişiklik

| Dosya | Satır | Önce | Sonra |
|---|---|---|---|
| `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor` | 142 | `KilitSebebi="@Dil.T.Duzeltme.DefterGerekce"` | `KilitSebebi="@Dil.T.Duzeltme.StornoYokGerekce"` |

Başka hiçbir satıra dokunulmadı: yedekle `diff` → tek fark bu satır (2. geçişte
değişen 167. satır hariç). Yeni sözlük anahtarı açılmadı; `StornoYokGerekce`
metni (TR:2775 / EN:4222) zaten "…ters kayıtla (storno) **hükümsüz kılınamaz**"
diyor → vaat kalıplarına uymuyor, "yapılamıyor" işaretini taşıyor.

## E.8.3 Yeni testler (mevcut testler değiştirilmedi — K4)

`dotnet/Kuyumcu.Tests/UX1C3KilitDurustlukTestleri.cs` (**yeni**, 2 bUnit testi):

| Test | Ölçtüğü |
|---|---|
| `C3_D1_Pusula_cekmecesinde_HICBIR_metin_olmayan_yolu_vaat_etmez` | Ön koşul (`HURDA_ALIS ∈ ErtelenenTurler`, damga `STORNO_YOK`) + **render edilen DOM**: `#cekmece-gider-pusulasi-kilit` VE `#cekmece-gider-pusulasi` içindeki hiçbir metin 11 vaat kalıbından ("ters kayıtla yapılır", "storno edilir", "düzeltme fişi", "İşlemler ekranından" …) hiçbirini içermez |
| `C3_D2_Pusula_kilit_sebebi_yapilamadigini_soyler` | Kilit sebebi düzeltmenin **yapılamadığını** açıkça söyler (5 işaretten biri) + `#cekmece-gider-pusulasi-kaydet` gerçekten `disabled` |

Bu testler artık **metni değil ekranı** bağlar; `KilitSebebi` yolu denetim altına girdi.

## E.8.4 Mutasyon kanıtı

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| **M-C3-17** | `GiderPusulalari.razor:142` `KilitSebebi` → `Duzeltme.DefterGerekce` (bulgu öncesi hâl) | `UX1C3KilitDurustlukTestleri.C3_D1_…_vaat_etmez` (**FAIL**: `→ "ters kayıtla yapılır"`) ve `C3_D2_…_yapilamadigini_soyler` (**FAIL**) — 2/2 | evet — `diff` ile yedeğe karşı birebir doğrulandı (tek fark satır 167, 2. geçişin kendi düzeltmesi) |

## E.8.5 Kapılar (üçüncü geçiş)

| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı / 0 Hata** |
| Filtreli `--filter FullyQualifiedName~UX1C3` | **39/39 geçti** (önce 37 → **+2**) |
| Tam süit (`--logger trx`) | **Toplam 1805 · Başarısız 0** (2. geçiş 1803 → **+2**; hiçbir test düşmedi) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (satır içi `style=` 149/196 — **artmadı**; Pages `.razor` 30/30; ModalPencere 42/45) |

## E.8.6 Canlı doğrulama (5198, üçüncü geçiş)

Test kiracısı: **PAKETUX1C3-TEST3 Kuyumculuk** (`01M2Q7MJZJXA5QK4SNH6Y5EJVT`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /health` | 200 | **200** |
| 2 | `POST /auth/kayit` | 201 | **201** (tenantId `01M2Q7MJZJXA5QK4SNH6Y5EJVT`) |
| 3 | `GET /meta/duzeltme-politikalari` (yetkisiz) | 401 | **401** (kapı çalışıyor) |
| 4 | `GET /meta/duzeltme-politikalari` (Bearer) | GiderPusulasi = SALT_OKUNUR / STORNO_YOK | **200** — `{"varlik":"GiderPusulasi","politika":"SALT_OKUNUR","gerekceAnahtari":"STORNO_YOK","hedefUc":null,"alternatifYol":null}` |
| 5 | aynı uç, hareket türü | HURDA_ALIS gerekçesi STORNO_YOK | **`{"varlik":"HURDA_ALIS","politika":"STORNO","gerekceAnahtari":"STORNO_YOK","hedefUc":null}`** → ekranın yeni kilit sebebi sunucu gerçeğiyle aynı |
| 6 | Temizlik | tenants başlangıca döner | tenant_id kolonu olan tüm tablolar + `tenants` silindi; **tenants 8 → 8**, `ad like 'PAKETUX1C3%'` → **0** |
| 7 | API durduruldu | 5198 boş | PID 64942 `kill`; `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. 5199 (kurucunun sunucusu) **dokunulmadı** (PID 76434 çalışmaya devam ediyor) |

6. adımda storno POST'u **tekrarlanmadı**: 2. geçişte (§ E.5 adım 6) 409 kanıtı
alınmıştı ve bu geçişte sunucu tarafında hiçbir değişiklik yok (yalnız bir Razor
metin bağlaması). Politika ucu (adım 4-5) aynı gerçeği doğrudan veriyor.

## E.8.7 Doğrulayıcının diğer bulguları

| Bulgu | Durum | Kanıt |
|---|---|---|
| **[KRİTİK]** çekmece `KilitSebebi` hâlâ "ters kayıtla düzeltilir" vaat ediyor | **KAPANDI** | `GiderPusulalari.razor:142` → `StornoYokGerekce`; `C3_D1`/`C3_D2`; mutasyon **M-C3-17** |
| **[ORTA]** rapor § 1 DoD tablosu (satır 33) eski/yanlış gerekçeyi anlatıyor | **KAPANDI** | Bu raporun § 1 satırı güncellendi: anahtar **STORNO_YOK**, alternatif yol "sistem içinde düzeltilemez, mali müşavire başvurun", kilit sebebi tabloya yazıldı |
| **[DÜŞÜK]** `GerekceDurust()` yalnız iki metin alanını tarıyor; TAKAS/TRANSFER/SAYIM_DUZELTME/SATIS_IADE/ALIS_IADE sayfa metinleri denetimsiz | **ERTELENDİ** | Bu paketin kapsamı grup 3 sayfaları; `Islemler.razor` **başka dalganın tek dokunuşu** (TEK DOKUNUŞ kuralı, MÜHÜR 2). Hedef: **UX-1-D / bir sonraki dalga** — "STORNO_YOK/SALT_OKUNUR damgalı tüm türlerin sayfa metinleri vaat denetimine alınır" (açık kalanlar § 10'a eklendi). Bu geçişte vaat denetimi bileşen düzeyine (render edilen DOM) taşındı; yayılma yalnız sayfa dokunuşu gerektiriyor. |

## E.8.8 Bu geçişte değişen dosyalar

- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor` (:142 — çekmece kilit sebebi)
- `dotnet/Kuyumcu.Tests/UX1C3KilitDurustlukTestleri.cs` (**yeni**, 2 test)
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-1-C3-uygulama-raporu.md` (§ 1 satırı + bu ek)

Commit **yapılmadı** (kurucu yasağı). Migration **gerekmedi** (şema değişikliği yok).
