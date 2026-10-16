# PAKET ŞUBE-KAPSAM — Seçili şube bütün ekranı yönetir (PAKET-46 FAZ B'nin yerine geçen güncel şartname)

> Tarih: 2026-09-30 · Hazırlayan: Tamer Bey koordinasyonu (şartname ajanı; KOD YAZILMADI) · Durum: ŞARTNAME — ekip oyu bekliyor
> Önkoşul: yok (P-04 global şube süzgeci ve P-15 şube seçicisi zaten ağaçta). PAKET-46 FAZ A (ince yetki, KASİYER/ÇIRAK) bu paketin DIŞINDADIR.
> Öncelik: yasal kuyrukla aynı (AKTIF-KOSUM 30.09 00:25 kaydı).

---

## 1. Amaç

Kurucu önceliği: **"Pusula kadar sade + devletin tarif ettiği her kural otomatik ve görünür."**

Kurucu beklentisi (30.09 00:25, birebir özet):
1. Üstte bir şube seçilince **Panel ve bütün listeler o şubeye göre** gelir.
2. **"Tüm şubeler"** seçilince her şey **birleşik** gelir.
3. Seçim **sayfa yenilenince (ve başka bilgisayarda aynı kullanıcıyla girince) kalır**.

Sade yüz kuralı: seçici yalnız **birden çok şubesi olan ve tüm şubeleri görebilen** kullanıcıya görünür (bugünkü UX-S22 davranışı korunur). Tek şubeli dükkânda ekranda **hiçbir şey değişmez**. Seçili şube her ekranın başlığının altında tek satırlık bir **rozetle** söylenir ("Merkez şube" / "Tüm şubeler") — kullanıcı neye baktığını her an bilir.

Yasal değişmez (sert kural): **şube seçimi yalnız GÖSTERİMİ daraltır.** Tevsik (30.000 TL) ve MASAK (185.000 TL) kümülatifleri, cari bakiye, vade/limit ve gün kilidi **mükellef/yükümlü (kiracı) genelinde** hesaplanır; şube seçimi bunlara **asla** sızmaz.

---

## 2. Mevcut durum (kod kanıtı, 30.09 ağacı)

### 2.1 Seçici var ama yalnız aramayı daraltıyor
| Kanıt | Ne gösteriyor |
|---|---|
| `dotnet/Kuyumcu.Web/Servisler/KabukServisi.cs:42` | `public string? AramaSubesi { get; private set; }` — seçim tek alanda yaşıyor. |
| `KabukServisi.cs:38-41` | Yorum: "Kalıcı DEĞİLDİR (sunucuya yazılmaz, yenilemede sıfırlanır)". Blazor WASM (`Kuyumcu.Web.csproj:1` `Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"`) → sayfa yenilenince statik tekil sıfırlanır. |
| `Kuyumcu.Web/Bilesenler/SubeSecici.razor:130-134` | Seçici `Kabuk.AramaSubesi`'ne yazar. |
| `Bilesenler/GlobalArama.razor:382`, `Bilesenler/KayitSecici.razor:265`, `Bilesenler/Secici.razor:302` | **Seçimi okuyan yalnız bu 3 arama bileşeni** (grep `AramaSubesi`: başka okuyucu 0). |
| `grep -rn 'AramaSubesi\|Kabuk' Kuyumcu.Web/Pages/*.razor` | Hiçbir sayfa seçimi okumuyor (Panel, Kasalar, Hurda, POS, Emanet, Raporlar dahil). |
| `SubeSecici.razor:101` | `TumSubeler => Rol is "SAHIP" or "MUDUR" or "MUHASEBE"` — sunucudaki tek kaynağın (`Kuyumcu.Api/Data/KiraciBaglami.cs:63` `TumSubelerRolleri`) **istemcide ikinci kopyası**. |
| `SubeSecici.razor:103` | Seçici yalnız `TumSubeler && subeler.Count > 1` iken görünür (doğru, korunacak). |
| `Layout/KenarCubugu.razor:303` ve `:321` | Seçici kullanıcı kartında ve kenar çubuğunda çiziliyor; `Layout/UstBar.razor:129-130` üst bardan kaldırıldığını yazıyor. |

### 2.2 Sunucu: taban süzgeç var, "görünüm daraltması" yok
| Kanıt | Ne gösteriyor |
|---|---|
| `Kuyumcu.Api/Data/KuyumcuDbContext.cs:75` | `SubeFiltresiAcik = !SistemModu && !TumSubeler` — SAHİP/MÜDÜR/MUHASEBE için şube süzgeci **hiç kurulmaz** (kiracı geneli görür). |
| `KuyumcuDbContext.cs:294` | Şubeye kilitli rolde global süzgeç: `e.TenantId == kiraciId && (e.SubeId == null \|\| e.SubeId == subeId)`. |
| `Kuyumcu.Api/Data/SubeSuzgeci.cs:30-37` | Oturum şubesine kilitleme yardımcısı (yetki süzgeci; görünüm daraltması değil). |
| `Modules/Arama/AramaModulu.cs:122` `SubeDarallmasiCoz` | Tek "kullanıcı seçimli daraltma" örneği; `[P-04 sınıf b]` etiketiyle (`AramaModulu.cs:196`, `:253`). |
| `dotnet/denetim.sh:1955-1990` | Modules altındaki her `SubeId ==` satırı `dotnet/sube-suzgec-siniflandirma.txt`'de sınıflanmak ZORUNDA; aksi hâlde `GUVENLIK_ELLE_IZOLASYON` KIRMIZI. |

### 2.3 Uçların şube parametresi
| Uç | Şube parametresi | Kanıt |
|---|---|---|
| `GET /raporlar/gunluk` | **YOK** | `Modules/Rapor/RaporModulu.cs:1167-1171`, servis `:31` |
| `GET /raporlar/aylik` | **YOK** | `RaporModulu.cs:1174-1179`, servis `:152` |
| `GET /raporlar/cari-yaslandirma` | **YOK** (bilinçli — cari kiracı geneli) | `RaporModulu.cs:1182-1186`, servis `:669` |
| `GET /raporlar/vade-takibi` | **YOK** (bilinçli — cari kiracı geneli) | `RaporModulu.cs:1190-1191`, servis `:783` |
| `GET /raporlar/pos-fatura-mutabakati` | **YOK** | `RaporModulu.cs:1194-1198`, servis `:349` |
| `GET /raporlar/stok-degeri`, `/reyon-stok` | VAR (elle `SubeId ==`) | `RaporModulu.cs:891`, `:1010`, `:1203`, `:1210` |
| `GET /raporlar/kar-zarar*`, `/stok-degerlemesi` | VAR | `Modules/Maliyet/KarZararServisi.cs:602`, `:612`, `:619` |
| `GET /kasalar` | VAR ama ekran göndermiyor | `Modules/Kasa/KasaModulu.cs:180`, `:667`; ekran `Kuyumcu.Web/Pages/Kasalar.razor:663` `GetAsync("/kasalar")` parametresiz |
| `GET /hurda/bakiye`, `/hurda/defter` | **YOK**; çok şubeli kiracıda şubesiz SAHİP'e 400 | `Modules/Hurda/HurdaModulu.cs:533-540`; çözüm kuralı `Modules/Hurda/HurdaStokKapisi.cs:148-160` (tek şube varsa o, yoksa oturum aynen → 400) |
| `GET /pos/tahsilatlar` | **YOK** (cihazId var) | `Modules/Pos/PosModulu.cs:1267-1271`; `/pos/cihazlar` VAR `:1223` |
| `GET /emanet/{cariId}` | **YOK** | `Modules/Emanet/EmanetModulu.cs:420`; ekran `Pages/Emanet.razor:918` |
| `GET /urunler/stok-adetleri` | **YOK** | `Modules/Urun/UrunStokOzetModulu.cs:38-39` |

### 2.4 Panel seçimi okumuyor
`Kuyumcu.Web/Pages/Panel.razor`: `:676` kâr-zarar (subeId göndermiyor), `:696` `/bankalar`, `:857` yaşlandırma, `:873` vade, `:1058` `/raporlar/gunluk`, `:1076` `/raporlar/aylik`, `:1104-1106` uyum özetleri, `:1120` işlem defteri — hiçbirinde şube yok.

### 2.5 Cari ve Fatura şube taşımıyor
`Kuyumcu.Api/Data/Entities.cs:363` `public class Cari : IKiraciVarlik` ve `:1200` `public class Fatura : IKiraciVarlik` — `ISubeVarlik` değil. PAKET-46 § Kapsam dışı: "Cari bakiyelerinin şubeye bölünmesi (mimari olarak reddedildi)". Bu karar korunur.

### 2.6 YENİ BULGU — şubeye kilitli kullanıcıda yasal kümülatif şubeye bölünüyor (doğrulanmalı)
- `Hareket : IKiraciVarlik, ISubeVarlik` (`Data/Entities.cs:709`) → TEZGÂHTAR oturumunda global süzgeç devrede (`KuyumcuDbContext.cs:294`).
- Tevsik kümülatifi `Modules/Tevsik/TevsikKapisi.cs:113-117` ve MASAK kümülatifi `Modules/Masak/MasakKapisi.cs:228-234` `db.Hareketler` üzerinden okur — **ikisi de istek bağlamındaki DbContext'i kullanır**.
- Sonuç (koddan çıkarım): **tezgâhtar A şubesindeyken aynı müşterinin aynı gün B şubesindeki nakdi toplama girmez.** 20.000 TL (A) + 15.000 TL (B) nakit ikinci işlemde durmalıyken geçer. PAKET-46 bunu "YASAK" diye yazmıştı (`PAKET-46…md:79`, `:210`) ama P-04 global süzgeci sonradan geldiği için yasak sessizce delinmiş görünüyor.
- Test boşluğu: `Kuyumcu.Tests/SubeIzolasyonTestleri.cs` (13 test) içinde kümülatif testi yok; `KumulatifKapiSubedenEtkilenmez` adıyla test deposunda 0 isabet.
- **Durum: ÇIKARIM — canlıda iki şube + tezgâhtar ile ölçülerek doğrulanacak (Kabul K-10).** Doğrulanırsa bu paketin EN yüksek öncelikli kalemidir.

---

## 3. Mevzuat

Bu paket yeni bir mevzuat kuralı getirmez; yasal kümülatiflerin **şubeye bölünmemesi** gerektiğini söyleyen mevcut hükümlere dayanır:

| Hüküm | Birincil kaynak | Alıntı (≤15 kelime) | Durum |
|---|---|---|---|
| Tevsik: aynı gün aynı kişiyle yapılan işlemler **mükellef** düzeyinde toplanır (tebliğde şube ayrımı yok) | 459 s. VUK GT §4.1.1 · https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm | "Aynı günde aynı kişi veya kurumlarla yapılan işlemlerin toplam tutarının" | DOĞRULANDI (ayrıntı PAKET-YASAL-2 § 3) |
| MASAK: kimlik eşiği tek işlem **ya da bağlantılı işlemler toplamı**; yükümlü = kuyumcu (md.4/1-k) | Tedbirler Yön. md.5/1-b · https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf | "İşlem tutarı ya da birbiriyle bağlantılı birden fazla işlemin toplam tutarı" | DOĞRULANDI (ayrıntı PAKET-YASAL-3 § 3) |
| MASAK Genel Tebliği 34'te "yükümlü" tanımı şubeleri **kapsar** | MASAK GT Sıra No 34 md.3/1-c · https://www.mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=46302&mevzuatTur=Teblig&mevzuatTertip=5 | "bunların şube, acente, temsilci ve ticari vekilleri ile benzeri bağlı birimlerini" | DOĞRULANDI — şube yükümlünün parçasıdır |

Şube, mevzuatta yükümlünün **bir birimidir** (MASAK GT 34 md.3/1-c); eşiği şube bazında bölen bir birincil hüküm bulunmadı. "Kümülatif kiracı genelidir" kuralı bu yüzden tutucu (yükümlü aleyhine hata yapmayan) yorumdur. **Teyit (DOĞRULANAMADI — mali müşavir):** "şube ayrımı yok" sonucu hükümlerde şube istisnası bulunmamasından çıkarılmıştır; S-2 ile birlikte sorulur (PAKET-YASAL-3 § 3.2 T-1).

---

## 4. Kapsam

### 4.1 İlke: iki ayrı kavram, iki ayrı isim
| Kavram | Ne | Nerede |
|---|---|---|
| **Yetki kapsamı** (değişmez) | Kullanıcının GÖREBİLECEĞİ şubeler. Taban: P-04 global süzgeci. | Sunucu, JWT + `SubeYetkisi` |
| **Görünüm şubesi** (YENİ, tercih) | Tüm-şube yetkili kullanıcının O AN baktığı şube (`null` = tüm şubeler). Yalnız DARALTIR; asla genişletmez. | Tercih (`kullanici_tercihleri`) + istemci durumu + uçlarda `?subeId=` |

`AramaSubesi` → `GorunumSubesi` olarak yeniden adlandırılır; arama da aynı değeri okur (tek kaynak).

### 4.2 Sunucu
1. **`Kuyumcu.Api/Common/GorunumSubesi.cs` (YENİ, tek yardımcı):**
   - `string? Coz(AktifKullanici islem, string? istenen)`:
     - Rol şubeye kilitli (TumSubeler=false): istenen boş → oturum şubesi; istenen ≠ oturum şubesi → **403** ("Bu şubeyi görme yetkiniz yok") — sessiz düşürme yok (AramaModulu deseni).
     - Rol tüm-şube yetkili: istenen boş → `null` (birleşik); istenen dolu → kiracıda **aktif şube mi** doğrulanır (yoksa 404), döner.
   - `IQueryable<T> Daralt<T>(this IQueryable<T> q, string? subeId) where T : ISubeVarlik` — tek `SubeId ==` satırı; `sube-suzgec-siniflandirma.txt`'e **tek** `b|` satırı eklenir. Mevcut elle yazılmış `RaporModulu.cs:891`, `:1010`, `:1079`, `:1095` ve `KasaModulu.cs:180` bu yardımcıya taşınır (sınıf b satır sayısı düşer, artmaz).
2. **Uçlara `?subeId=` (isteğe bağlı) eklenir** — hepsi `GorunumSubesi.Coz` ile:
   - `GET /raporlar/gunluk`, `/raporlar/aylik`, `/raporlar/pos-fatura-mutabakati`
   - `GET /hurda/bakiye`, `/hurda/defter` — `subeId` boş + tüm-şube rolü → **birleşik** (bugünkü 400 kalkar; `HurdaStokKapisi.OturumSubesiCoz` yalnız YAZMA yolunda kalır). Birleşik yanıtta şube kırılımı (`subeler[]: {subeId, ad, hasBakiye}`) döner.
   - `GET /pos/tahsilatlar`, `/pos/valor-takvimi`
   - `GET /emanet/{cariId}` (emanet satırının şubesi = hareketin şubesi)
   - `GET /urunler/stok-adetleri`
   - Kasa defteri raporu ucu (`Pages/KasaRaporu.razor:296` `Rapor.KasaDefteri`) — kasa zaten şubeye bağlı; yalnız kasa SEÇENEKLERİ (`KasaRaporu.razor:284` `/kasalar`) görünüm şubesiyle daraltılır
   - Mevcut `subeId` alan uçlar (`/kasalar`, `/raporlar/stok-degeri`, `/reyon-stok`, `/kar-zarar*`, `/stok-degerlemesi`, `/pos/cihazlar`, `/raporlar/islem-defteri`, `/raporlar/odeme-araci`, `/raporlar/personel` — `SatisRaporlari.cs:515-534`) **aynı `Coz`'a bağlanır** (bugün kilitli rol parametreyle başka şube isteyebiliyor mu — global süzgeç engeller, ama 403 yerine boş liste döner; tek davranışa indirilir).
3. **Şube bilmeyen (kiracı geneli) uçlar DEĞİŞMEZ ve bunu yanıtta söyler:** `/raporlar/cari-yaslandirma`, `/raporlar/vade-takibi`, `/cariler*`, `/faturalar*`, `/bankalar*`, `/masak/*`, `/tevsik/*`. Bu uçlar `subeId` gelirse **yok sayar** ve yanıta `kapsam: "KIRACI_GENELI"` alanı ekler (ekran rozeti buna göre "Tüm şubeler (müşteri hesapları şubeye bölünmez)" yazar).
4. **Tercih anahtarı:** `Modules/Tercih/TercihModulu.cs:28-36` beyaz listesine `gorunum-subesi` eklenir (değer: şube ULID ya da `"TUMU"`). Migration YOK (anahtar→metin deposu). Okurken şube pasif/silinmiş ya da kullanıcının yetkisi dışındaysa **"Tüm şubeler"e değil, oturum şubesine** düşülür ve istemciye `gecersizTercih: true` bildirilir (sessiz genişleme yok).
5. **YASAL DÜZELTME (2.6 doğrulanırsa):** Tevsik ve MASAK kümülatif okuması şubeden bağımsız yapılır.
   - Yol: `TevsikKapisi.GunlukNakitToplam` ve `MasakKapisi.GunlukIslemToplam` sorguları `SistemKapsami.Kiraci(islem.TenantId)` kapsamında (kiracı süzgeci VAR, şube süzgeci YOK — `KiraciBaglami.cs:115` `tumSubeler = true`) çalıştırılır. Salt okuma.
   - `Data/SistemKapsami.cs` izinli kullanım listesine **9. madde** eklenir: "Yasal kümülatif okuyucu (tevsik/MASAK) — kiracıya sabit, şube süzgeçsiz, SALT OKUMA". ADR-0002'ye ek not. **Mehmet onayı zorunlu** (güvenlik yüzeyi).
   - Alternatif (oyda tartışılır): `Hareket` için yalnız kümülatif sorgusunda kullanılan `IKiraciGeneliOkuma` işaretli ayrı bir DbContext. Önerilen: SistemKapsami (yeni mekanizma açmaz).

### 4.3 İstemci
1. `KabukServisi` → `GorunumSubesi` alanı + `GorunumSubesiAta()`; atamada `PATCH /tercihler/benim {"gorunum-subesi": …}` (başarısızsa HataBandi; yerel değer geri alınır — "görünen ≠ kaydedilen" yasak).
2. Açılışta (oturum kurulunca) `GET /tercihler/benim` → `gorunum-subesi` okunur; seçici o değerle çizilir.
3. **Tek istemci yardımcısı** `SubeliSorgu.Ekle(url)`: seçim doluysa `subeId=` ekler. Aşağıdaki ekranlar bu yardımcıyla çağırır ve `Kabuk.Degisti` olayında **yeniden yükler**:
   - Panel (`Panel.razor:676, 1058, 1076, 1120` + stok/hurda kartları)
   - Kasalar (`Kasalar.razor:663`) — yeni kasa formunda şube varsayılanı = görünüm şubesi
   - Ürünler/stok adetleri (`Urunler.razor:1277`), Raporlar stok (`Raporlar.razor:1569`), Kâr-zarar (`KarZarar.razor:495`)
   - Hurda (`Hurda.razor:813`, `:816`)
   - POS tahsilatlar (`Servisler/PosServisi.cs:113`), Emanet (`Emanet.razor:918`, `:978`)
   - İşlem defteri, Kasa raporu, Gün sonu (pos-fatura-mutabakati) ekranları
4. **Rozet bileşeni `GorunumRozeti`** (YENİ, ortak): başlık altında tek satır — "Merkez şube" ya da "Tüm şubeler". Kiracı geneli uçları kullanan ekranda (Cariler, Faturalar, Vade, Yaşlandırma, Banka, Uyum) "Tüm şubeler — müşteri hesapları şubeye bölünmez" yazar. Tek şubeli kiracıda ve şubeye kilitli rolde **çizilmez** (sade yüz).
5. `SubeSecici.razor:101` istemci rol kopyası kaldırılır; sunucudan `GET /yetkiler/benim` (ya da `/auth/ben`) yanıtına eklenecek `tumSubeler: bool` okunur (tek kaynak).
6. Seçici seçenekleri: "Tüm şubeler" (ilk sıra) + aktif şubeler. Seçici yalnız `tumSubeler && aktifSube > 1` iken görünür (bugünkü koşul).

### 4.4 Ekranda nasıl görünür
| Durum | Görünüm | Tür |
|---|---|---|
| Tek şubeli dükkân | Hiçbir değişiklik; seçici ve rozet yok | otomatik |
| Çok şube, SAHİP, "Merkez" seçili | Panel kartları, Kasalar, Hurda, Stok, POS, Emanet yalnız Merkez; başlık altında "Merkez şube" | otomatik |
| "Tüm şubeler" | Birleşik; Hurda/Kasalar'da şube sütunu görünür (Basit modda katlanır) | otomatik |
| Cariler/Faturalar/Vade | "Tüm şubeler — müşteri hesapları şubeye bölünmez" rozeti; liste değişmez | bilgi |
| Tercih edilen şube pasife alınmış | Sarı bant: "Seçtiğiniz şube kapalı; kendi şubenize dönüldü." + seçici | uyarı |
| Tezgâhtar | Seçici yok; kendi şubesi (bugünkü gibi) | otomatik |
| Tevsik/MASAK kapısı | Şube seçimi ne olursa olsun kiracı geneli toplar; ekranda değişiklik yok | engel (mevcut) |

---

## 5. Kapsam dışı
- PAKET-46 FAZ A (ince yetki, iskonto tavanı, zararına satış, KASİYER/ÇIRAK).
- Cari, fatura, banka hesabı, MASAK kayıtlarının şubeye bölünmesi (mimari red korunur).
- "Seçili birden çok şube" (liste) — ikili (tek şube / tümü) ile başlanır.
- Şubeler arası fiyat listesi / ayrı kasa politikası.
- JWT'deki şubenin değiştirilmesi (görünüm şubesi yetki DEĞİLDİR; yazma işlemleri oturum şubesinde kalır). Yazma yolunda "hangi şubede işlem yapıyorum" sorusu ayrı paket (şubesiz SAHİP'in çok şubeli kiracıda hurda/satış 400'ü — AKTIF-KOSUM 29.09 05:45 "şubesiz tezgâhtarda 403" kuyruk kalemiyle birlikte ele alınır).

---

## 6. Kabul ölçütleri (sayısal, test edilebilir)
| # | Ölçüt | Kanıt türü |
|---|---|---|
| K-1 | 2 şubeli test kiracısında SAHİP "Merkez" seçer → `/raporlar/gunluk?subeId=M` toplamı = SQL `SUM(tl_tutari) WHERE sube_id=M AND gün=bugün` (kuruş farkı 0) | HTTP testi + canlı SQL |
| K-2 | "Tüm şubeler" → gunluk toplamı = Merkez + Şube-2 toplamı (fark 0) | HTTP testi |
| K-3 | Aynı ölçüm `/raporlar/aylik`, `/hurda/bakiye`, `/hurda/defter`, `/pos/tahsilatlar`, `/kasalar`, `/urunler/stok-adetleri`, `/emanet/{cariId}` için ayrı ayrı (7 uç × 2 durum = 14 test) | HTTP testi |
| K-4 | Çok şubeli kiracıda şubesiz SAHİP `/hurda/bakiye` → **200** (bugün 400) ve `subeler[]` kırılımı toplamı = birleşik toplam | HTTP testi |
| K-5 | TEZGÂHTAR (şube A) `?subeId=B` ile 9 uçtan herhangi birini ister → **403**; `?subeId=` boş → yalnız A | HTTP testi (tablo güdümlü) |
| K-6 | Pasif şube ULID'i ile istek → 404; başka kiracının şubesi → 404 | HTTP testi |
| K-7 | Seçim → sayfa yenile (F5) → aynı şube seçili ve Panel aynı rakamlar | Gerçek tarayıcı (başsız Chrome) + ekran görüntüsü |
| K-8 | Aynı kullanıcı ikinci tarayıcı profilinde girer → aynı seçim | Gerçek tarayıcı |
| K-9 | Tercih yazımı 500 dönerse seçici eski değere döner + HataBandi (görünen = kaydedilen) | bUnit |
| K-10 | **Yasal değişmez:** aynı cari, aynı gün, Şube A'da TEZGÂHTAR 20.000 TL nakit, Şube B'de başka TEZGÂHTAR 15.000 TL nakit → ikinci istek **400** (tevsik) ve `hareketler`'de yeni satır 0. Aynı desen MASAK: 100.000 + 90.000 → ikinci istekte kimlik bloğu yoksa 400 | HTTP testi + canlı SQL — **bu test ağaçta şimdi koşulursa KIRMIZI beklenir (2.6)**; paket onu yeşile çevirir |
| K-11 | SAHİP görünüm şubesini "Merkez" seçmişken aynı cariye Şube-2 hareketiyle birlikte tevsik kümülatifi yine kiracı geneli (görünüm şubesi kapıya sızmaz) | HTTP testi |
| K-12 | `denetim.sh` TEMİZ; `GUVENLIK_ELLE_IZOLASYON` = 0; sınıf-b satır sayısı paket öncesinden **az ya da eşit** | denetim çıktısı |
| K-13 | Gerçek tarayıcı: 1280 + 375 px · Basit + Profesyonel · açık + koyu = 8 görüntü; seçici ve rozet taşma 0 (`betikler/sikisma-olc.js`) | ekran görüntüsü |
| K-14 | Tek şubeli kiracıda Panel/Kasalar DOM'unda `#gorunum-rozeti` ve seçici **0** adet | bUnit + tarayıcı |
| K-15 | `dotnet test` 0 başarısız; test kiracıları silinmiş | kapı çıktısı |

---

## 7. Mutasyon önerileri (her biri en az bir testi KIRMIZI yapmalı)
1. `GorunumSubesi.Coz` kilitli rol dalında istenen şubeyi doğrulamadan döndürsün → K-5 KIRMIZI.
2. `Daralt` içinde `==` yerine `!=` → K-1 KIRMIZI.
3. `/raporlar/gunluk` ucunda `subeId` servise geçirilmesin → K-1 KIRMIZI.
4. Tevsik kümülatifi yeniden istek bağlamındaki `db` ile okunsun (SistemKapsami kaldırılsın) → K-10 KIRMIZI.
5. `TevsikKapisi.GunlukNakitToplam`'a görünüm şubesi koşulu eklensin → K-11 KIRMIZI.
6. Tercih okumada geçersiz şube "TUMU"ya düşsün → pasif şube testi KIRMIZI.
7. İstemci `SubeliSorgu.Ekle` Panel'de çağrılmasın → Panel bUnit (istek URL'inde `subeId`) KIRMIZI.
8. `PATCH /tercihler` hatasında yerel değer geri alınmasın → K-9 KIRMIZI.

---

## 8. Riskler
- **Yasal (en yüksek):** 2.6 doğrulanırsa üründe bugün şube üzerinden tevsik/MASAK kümülatifi delinebiliyor demektir; bu paket yalnız görünüm işi değil, **yasal düzeltme** taşır. SistemKapsami'ne yeni kullanım eklemek güvenlik yüzeyidir → Mehmet vetosu olası; alternatif mekanizma oyda hazır olmalı.
- **Yanlış toplam riski:** Birleşik yanıtta şube kırılımı toplamı ile birleşik toplam farklı yoldan hesaplanırsa sapar → her iki rakam tek sorgudan türetilir (K-4).
- **İndeks:** `hareketler (tenant_id, sube_id, tarih)` indeksi var (`KuyumcuDbContext.cs:332`); diğer tablolar için (`pos_tahsilatlar`, `stoklar`) `(tenant_id, sube_id)` var (`:493-495`). Yeni indeks ölçülmeden eklenmez (TB-19).
- **Ekran kalabalığı:** Rozet her ekrana eklenince sade yüz bozulabilir → tek şubede ve kilitli rolde çizilmez (K-14).
- **Önbellek:** `Servisler/ListeOnbellegi.cs:80` `/kasalar` tazelik anahtarı şubeden bağımsız → anahtar `url+subeId` olmalı; yoksa şube değişince eski liste görünür.
- **PAKET-46 ile çakışma:** 46 FAZ B'nin `SubeKapsami` tasarımı (KENDI/TUMU ince yetkisi) bu paketle **değiştirilmiştir**: yetki tabanı P-04, görünüm tercihi bu paket. 46 FAZ A açılınca `sube_kapsami` alanı yalnız P-04 rol listesini kullanıcı bazında ezer; `GorunumSubesi.Coz` değişmez.

---

## 9. Tahmini süre (en çok 2 tur kuralı)
- Uygulama turu 1: sunucu yardımcı + 9 uç + tercih + yasal kümülatif düzeltmesi + testler: **~4–5 saat**; istemci (kabuk, 9 ekran, rozet): **~3–4 saat**.
- Tur 2 (düzeltme + gerçek tarayıcı kanıtı): **~2–3 saat**.
- Toplam: **~1–1,5 iş günü** (ölçülmüş hız: paket başına 2–4 saat, bu paket ~2 paket büyüklüğünde). Tahmindir, ölçülmedi.
- Bölme önerisi (2 tur sınırına sığsın): **ŞK-A** (sunucu + yasal kümülatif, K-1…K-6, K-10…K-12) ve **ŞK-B** (istemci + tarayıcı, K-7…K-9, K-13, K-14) iki ayrı şerit.

---

## 10. Şerit dosya alanı önerisi (çakışma önleme)
**ŞK-A (sunucu) yazar:**
- `dotnet/Kuyumcu.Api/Common/GorunumSubesi.cs` (YENİ)
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs`, `Rapor/SatisRaporlari.cs`, `Rapor/StokYaslandirmaRaporu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`, `Hurda/HurdaModulu.cs` (yalnız `HurdaStokController` + `HurdaStokServisi`), `Pos/PosModulu.cs` (yalnız okuma uçları), `Emanet/EmanetModulu.cs` (yalnız GET), `Urun/UrunStokOzetModulu.cs`, `Maliyet/KarZararServisi.cs` (yalnız controller imzaları)
- `dotnet/Kuyumcu.Api/Modules/Tercih/TercihModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs` (yalnız `GunlukNakitToplam`), `Masak/MasakKapisi.cs` (yalnız `GunlukIslemToplam`) — **PAKET-YASAL-2 ve YASAL-3 ile ÇAKIŞIR → bu iki dosyada önce ŞK-A, sonra YASAL-2/3 (sıralı)**
- `dotnet/Kuyumcu.Api/Data/SistemKapsami.cs`, `dokumanlar/02-mimari/adr/0002-sistem-kapsami-tek-kacis-yolu.md`
- `dotnet/sube-suzgec-siniflandirma.txt`, `dotnet/denetim.sh` (yalnız P-04 bloğu)
- Testler: `dotnet/Kuyumcu.Tests/GorunumSubesiTestleri.cs` (YENİ), `KumulatifKapiSubedenEtkilenmezTestleri.cs` (YENİ)

**ŞK-B (istemci) yazar:**
- `dotnet/Kuyumcu.Web/Servisler/KabukServisi.cs`, `Servisler/SubeliSorgu.cs` (YENİ), `Servisler/ListeOnbellegi.cs`, `Servisler/PosServisi.cs`, `Servisler/RaporServisi.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/SubeSecici.razor`, `GorunumRozeti.razor` (YENİ), `GlobalArama.razor`, `KayitSecici.razor`, `Secici.razor` (yalnız ad değişimi)
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`, `Kasalar.razor`, `Hurda.razor`, `PosTahsilatlar.razor`, `Emanet.razor`, `Urunler.razor` (yalnız stok-adetleri çağrısı), `Raporlar.razor` (yalnız stok), `KarZarar.razor` (yalnız çağrılar), `IslemDefteri.razor`, `KasaRaporu.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (yalnız yeni anahtarlar, dosya sonuna)
- Testler: `dotnet/Kuyumcu.Tests/GorunumSubesiEkranTestleri.cs` (YENİ)

**Dokunmaz:** `Islemler*.razor` (SATIŞ şeridi), `Faturalar.razor` (ZO-BELGE), `Cariler.razor`.
