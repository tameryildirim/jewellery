# PAKET URUN-ACILIS-ATOMIK — uygulama raporu (Taşkın backend · Ertuğ ekran kanıtı) · 08.10.2026

Kaynak: `ham/URUN-ACILIS-BULGU.md` (MASAK-370 tasarım hakemi bulgusu). COMMIT YOK. Migration gerekmedi (şema değişmedi).

## 1. Kök neden (dosya:satır, düzeltme öncesi)
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` `UrunServisi.Olustur` (~595): ürün kartı `db.SaveChangesAsync()` ile **işlem dışında commit** ediliyordu; açılış stoğu ardından `StokSayimGuncelle` (~1150) içinde **ayrı** `BeginTransactionAsync` ile yazılıyordu. İkinci adım reddedince (yeni kiracıda kur yok → `SonKurId` 400 "Sayım kaydı için en az bir kur girişi gerekir"; ya da çok şubede `AcilisSubesiCoz` 400) kart veritabanında kalıyor, tekrar deneme 409 "kod kullanımda" alıyor, satışta "Stokta yok" ikizi çıkıyordu.
- Ek gizli kusur: dış işlem içinden (içe aktarım) açılış stoklu `Olustur` çağrılırsa `StokSayimGuncelle`'nin `BeginTransactionAsync`'i "connection is already in a transaction" fırlatırdı.

## 2. Düzeltme
- `Olustur`: `await using var tx = await db.Database.IsBaslatVeyaDevral();` kart Add'inden önce; kart SaveChanges + açılış stoğu + tek `CommitAsync`. Herhangi bir adım reddederse `await using` geri alır → 0 satır. Eşzamanlı çift gönderimde `UQ_urunler_tenant_kod/_barkod` ihlali (MySQL 1062) 500 yerine 409 alan:kod (`TekillikIhlaliMi`).
- `StokSayimGuncelle`: `BeginTransactionAsync` → `IsBaslatVeyaDevral` (dış işleme katılır, commit sahibinde; tek başına davranış aynı).
- İdempotent davranış: red sonrası tekrar deneme temiz başlar (yarım kart yok); başarılı kayıttan sonra aynı kod 409 alan:kod — çift ürün oluşmaz (ön kontrol + benzersiz indeks).

## 3. Atomiklik taraması
Yöntem: (a) `tara.py` — bir metotta ≥2 SaveChanges ve işlem yok; (b) adı verilen akışların tek tek okunması; (c) "reddi yakalayıp aynı DbContext ile SaveChanges'e devam eden" catch blokları.

| Uç | Adımlar | Tek işlem mi (önce → sonra) | Red sonrası kalan satır (önce → sonra) |
|---|---|---|---|
| POST /urunler + açılış stoğu | kart → stok/parti/SAYIM hareketi | HAYIR → **EVET** | ürün kartı (aktif=1) → **0** |
| POST /satinalma/{id}/mal-kabul | irsaliye+kalemler (commit) → kalem başına SatisServisi.Alis → bağ+kabul adedi → durum | HAYIR (irsaliye ayrı commit, Alis'ler ayrı) → **EVET** (irsaliye denemesinin işlemi açık tutulur, Alis `IsBaslatVeyaDevral` ile katılır, en sonda tek commit) | irsaliye + bağsız kalem (+ önceki kalemlerin alışları) → **0** |
| POST /bankalar/ekstre/{id}/onayla | satır başına HareketEkleIc → satır ONAYLANDI/HATALI → SaveChanges | satır bazında (bilinçli, değişmedi) | Reddedilen satırda (mükerrer 409) HareketEkleIc'in Add'i ve hesap bakiyesi artışı izleyicide kalıyor, catch'teki SaveChanges bunları yeniden yazmaya çalışıyordu (mükerrer hâlinde 500, MASAK kapısı Add'den SONRA reddetseydi SIZINTI) → **izleyiciden geri alınır** (`ChangeTracker.BekleyenleriGeriAl(satir)`), 200 + satır HATALI |
| Kurulum açılış uygula | AcilisServisi | EVET (zaten; AcilisYazici devralır) | 0 |
| Fatura + kalem (POST /faturalar) | tek SaveChanges | EVET | 0 |
| Fatura TaslakKalemGuncelle | kalem save → başlık toplamı save | HAYIR; arada iş kuralı reddi yok, ikinci adım saf hesap; tekrar idempotent | yalnız altyapı hatasında başlık eski toplamla — açık kalan (düşük) |
| Sipariş + kalem (POST /siparisler) | tek işlem | EVET (zaten) | 0 |
| Sipariş kapora | Cari.TahsilatGirHareket (kendi işlemi, commit) → sipariş.KaporaHareketId | HAYIR; arada red yok, yalnız altyapı hatasında bağsız tahsilat (tekrar = ikinci tahsilat riski) | açık kalan — `CariModulu.cs:1088` `BeginTransactionAsync` devralmaya çevrilmeli; dosya TCKN-DOGRULAMA şeridinin alanında, dokunulmadı |
| Kullanıcı davet + rol (POST /kullanicilar) | rol aynı satırın kolonu, tek SaveChanges | EVET | 0 |
| Kiracı kaydı + sahip (POST /auth/kayit) | işlem içinde | EVET (zaten) | 0 |
| Vezne tahsil | fiş kilidi → defter → bağ | telafili (R9: red → fiş VEZNEDE'ye döner) — bilinçli tasarım | 0 iş etkisi |
| POS eşleşme geri al | eşleşme save → banka hareketi durumu save | HAYIR; arada red yok | yalnız altyapı hatasında — açık kalan (düşük) |
| Auth/MFA/İşler/MASAK tarama | sayaç/durum kayıtları | bilinçli ayrı commit (güvenlik sayaçları geri alınmamalı) | — |

## 4. Değişen dosyalar
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (Olustur işlem sınırı + 409 dönüşümü; StokSayimGuncelle devralma)
- `dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs` (MalKabul tek işlem)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (yalnız `Alis` işlem satırı: `BeginTransactionAsync` → `IsBaslatVeyaDevral`, commit `tx is not null` — 2 satır; kimlik alanlarına dokunulmadı)
- `dotnet/Kuyumcu.Api/Modules/Banka/EkstreModulu.cs` (catch'te `BekleyenleriGeriAl`)
- `dotnet/Kuyumcu.Api/Common/VeriIslemi.cs` (`BekleyenleriGeriAl` yardımcı)
- `dotnet/Kuyumcu.Tests/UrunAcilisAtomikTestleri.cs` (YENİ, 6 test)
- Ekran kodu DEĞİŞMEDİ (razor/css/sözlük yok); ekran kanıtı mevcut hata bandının doğru sonucu gösterdiğini ölçer.

## 5. Testler
Yeni (6): `UrunAcilisAtomikTestleri.A1_Kur_yokken_…_tekrar_deneme_cift_uretmez`, `A2_Sube_belirsizken_…_kart_YAZILMAZ`, `A1b_Acilis_stogu_basariliysa_…_TEK_islemde_dogar`, `A3_Mal_kabulde_alis_reddedilince_irsaliye_kalem_ve_hareket_YAZILMAZ`; `UrunAcilisAtomikHttpTestleri.A4_Ekstre_onayinda_mukerrer_…_SIZMAZ_bakiye_degismez`, `A1_HTTP_Kur_yokken_POST_urunler_400_…`.
- Filtreli: UrunAcilisAtomik + P26Tedarik + UX4dServis + EkstreIceAktarma + Satinalma + Alis → **235/235 geçti**.
- Tam süit (tek koşum, son kod, `kuyumcu_test_t5`): **10554 toplam, 13 başarısız** — hiçbiri bu pakete ait değil (§ 8). Bu paketin 6 testi tam süitte yeşil.
- Tam süitten SONRA başka şerit (GIRDI-AILESI-2, 02:35) `BankaModulu.cs`'e IBAN MOD 97-10 doğrulaması ekledi; A4 testinin rastgele (sağlama haneli olmayan) IBAN'ı 400 almaya başladı → test yardımcısı geçerli MOD-97 IBAN üretecek biçimde düzeltildi (yalnız test dosyası). Yeniden koşum sonucu § 8'de.

## 6. Mutasyon kanıtı (yedek `/private/tmp/kuyumcu-serit/mut-yedek/URUN-ACILIS-ATOMIK/`, geri alındı + `cmp` AYNI)
| Kod | Değişiklik | Kırmızı testler |
|---|---|---|
| M1-1 | Olustur'da işlem sınırı kaldırıldı (`tx = null`) | A1, A2, A1_HTTP |
| M1-2 | StokSayimGuncelle devralma → `BeginTransactionAsync` | A1, A1b, A1_HTTP, UX4dServisTestleri.M1_Urun_kartindan_ACILIS_STOGU… |
| M2-1 | MalKabul irsaliye işlemini erken commit | A3 |
| M3-1 | Ekstre catch'te `BekleyenleriGeriAl` kaldırıldı | A4 |
| M4-1 | Satis.Alis devralma → `BeginTransactionAsync` | A3 + P26TedarikTestleri.Y34_mal_kabul_STOGA…, Y34_tam_kabulde…, Y35_deftere_bagli_irsaliye…, Y34_mal_kabulu_yapilmis… |

## 7. Canlı doğrulama (API 5166, `kuyumcu_net`, bağımsız SQL — `ham/URUN-ACILIS-ATOMIK-olcum/canli-tam.json`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| A1 POST /urunler (kur 0, açılış 5) | 400 + SQL 0 | 400 "Sayım kaydı için en az bir kur girişi gerekir…" · urun/stok/parti/hareket = 0/0/0/0 |
| A1 aynı istek tekrar | 400 (409 değil) | 400 · 0/0/0/0 |
| Kur manuel + aynı kod | 2xx + 1/1/1/1 | 201 · 1/1/1/1 |
| Üçüncü deneme | 409 alan kod | 409 `alanlar.kod` · ürün=1 |
| A3 mal kabul, kiracıda olmayan kur | 4xx, irsaliye değişmez | 404 "Kur kaydı bulunamadı" · irs/kalem/hareket önce 0/0/1 → sonra 0/0/1, kabul 0.000 |
| A3 düzeltilip tekrar | 200, 1 irsaliye, bağsız 0 | 200 IRS-2026-00001 KISMI_KABUL · 1/0/2.000 |
| A4 ekstre onayı, satır mükerrer | 200 basarili 0, banka hareketi ve bakiye değişmez | 200 basarili 0 + mükerrer mesajı · adet/bakiye 1/100.00 → 1/100.00, satır HATALI |

## 8. Ekran (başsız Chrome, vekil 5167, CDP 9507) — `ham/URUN-ACILIS-ATOMIK-olcum/`
Senaryo: kur yokken Ürünler → + Yeni ürün → ad/kod/gram/açılış 5 → Kaydet. Kur yarışı (istemcinin /kur/guncel çağrısı arka planda kur çekiyor) tıklamadan hemen önce SQL ile kapatıldı.
| Mod | Tema | Genişlik | Hata bandı | DB sonra (ürün/stok/parti/hareket) | K1 | K3 | K5 | Y | Görüntü |
|---|---|---|---|---|---|---|---|---|---|
| BASIT | açık | 1280 | "Sayım kaydı için en az bir kur girişi gerekir…" görünür | değişmedi | 0 | 0/0 | 0 | 0 | urun-acilis-hata__BASIT_aydinlik_1280.png |
| BASIT | açık | 375 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …BASIT_aydinlik_375.png |
| BASIT | koyu | 1280 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …BASIT_karanlik_1280.png |
| BASIT | koyu | 375 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …BASIT_karanlik_375.png |
| PRO | açık | 1280 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …PRO_aydinlik_1280.png |
| PRO | açık | 375 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …PRO_aydinlik_375.png |
| PRO | koyu | 1280 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …PRO_karanlik_1280.png |
| PRO | koyu | 375 | görünür | değişmedi | 0 | 0/0 | 0 | 0 | …PRO_karanlik_375.png |
Görüntüler Read ile görüldü (BASIT açık 1280, PRO koyu 1280/375): kırmızı hata bandı Kaydet'in üstünde, liste "1 kayıt" (ilk turda kur yarışıyla meşru biçimde doğan ATM-UI-6; yeni kayıt yok). K9 (binen) 1–6: açık modalın listenin üstüne binmesinden; ekran kodu bu pakette değişmediği için mevcut durum. İlk turda tarayıcı 6. kombinasyonda kur yarışını kaybetti ve ürün BAŞARIYLA (1/1/1/1, tutarlı) oluştu — atomiklik ihlali değil; ikinci tur yarış kapatılarak koşuldu.

## 9. Kapı (paket ölçüsü)
1. Build: `Kuyumcu.sln` 0 hata, 4 uyarı (hepsi mevcut xUnit2013 test uyarıları, bu pakete ait değil).
2. Paket testleri 0 başarısız (filtreli 235/235; tam süitte 6/6).
3. denetim.sh: 306 ✓, 1 ✗ = yalnız P-30 DEMO_INSAN_SURESI (kapsam dışı istisna).
4. Mutasyon 5/5 kırmızı, hepsi cmp AYNI.
5. Canlı: § 7.
6. Test kiracıları silindi (3 kiracı: 01M4CANH7B5FPPNFCSQYXANE50 [ilk koşumda yarım kalan], 01M4CANXPGZ4QZC7YYF1T9S2YB, 01M4CAPHD83BWZPV3XKTEM87V2 — `zsh kiraci-temizle.sh … --uygula`; davet satırları 3 silindi); tenants 17 → 17. Fikstür dosyaları `printf ''` ile boşaltıldı. 5166/5167 kapatıldı.
7. Tam süit kırmızıları (13) sahipleriyle: `P31DestekPaketiTestleri` ×11 — "Kuyumcu.Api.deps.json bulunamadı" (paralel şerit derlemesinin bin dizinini koşum ortasında yeniden yazması; aynı testler ardından filtreli koşumda YEŞİL); `RaporUcEkranTestleri.R3_33_Gun_sonu_doviz_girisi_ayri_BASIT` — RAPOR-3 şeridi (filtreli tekrarda yeşil, yarım iş anlık görüntüsü); `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` — filtreli tekrarda da kırmızı, pusula/mutabakat alanı (YASAL şeridi); bu paket gider pusulasına dokunmadı.

## 10. Açık kalanlar
- Sipariş kapora (CariModulu.TahsilatGirHareket devralma) — TCKN-DOGRULAMA şeridi Cari dosyasından çıkınca tek satırlık iş.
- Fatura TaslakKalemGuncelle ve POS EslesmeGeriAl: arada red yolu yok, yalnız altyapı hatasında yarım; düşük öncelik.
- Satinalma `BagsizKalemler` raporu artık boş dönmesi beklenen bir güvenlik ağıdır; kaldırılmadı.
- Süreç notu (Anayasa md.1): canlı betiğin ilk koşumunda kayıt ucu 201 döndü, betik yalnız 200 beklediği için hata dalında yanıt gövdesinin ilk 300 karakterini (yerel test kiracısının erişim jetonunun bir kısmı dahil) ajan çıktısına bastı. Jeton yalnız localhost:5166'daki, artık silinmiş test kiracısına aitti; betik düzeltildi (gövde basılmaz). Kurucu bilgisine.

## 11. Kurucu / mali müşavir
Mevzuat hükmü içermez; soru yok.

## 12. IBAN düzeltmesi sonrası yeniden koşum (02:45)
Filtre `UrunAcilisAtomik|P31DestekPaketiTestleri`: **26/26 geçti** (6 paket testi + 20 P31; P31 tam süitteki deps.json kırmızılarının ortam kaynaklı olduğunu da teyit eder).

---

# TUR 2 (08.10.2026 ~12:00–) — doğrulayıcı + tasarım hakemi bulgularının kapatılması

## T2.1 Bulgu → yapılan
| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| BASİT modda açılış stoğu reddi ekranda görünmüyor (form uzuyor, bant/Kaydet ekran dışı) | KRİTİK | (1) Sunucu ürün kartı yolunda açılış reddini `alanlar["acilisStogu.adet"]` ile döndürür (`UrunModulu.AcilisAlani`; tavan, kur yok, şube belirsiz/yok — 4 yer). (2) `Urunler.razor` `YeniEsleme`: `acilisStogu.adet`, `acilisStogu.subeId`, `adet` → `u-acilis-stok` (kutu kırmızı, odak kutuya, `Daha fazla` AÇILMAZ). (3) `OdakIlkHataya`: yeni ürün penceresinde alansız hata → `u-urun-hata` bandına kaydırır. (4) Hata bandı + kilit sebebi + İptal/Kaydet ortak `ModalPencere`'nin SABİT altlığına (`<Altlik>`, P-36 kalıbı, Banka dekontla aynı) taşındı; satır içi `style="flex:1"` → `esnek` sınıfı. Mevcut kimlikler (`u-kaydet`, `u-kaydet-kilit`) aynı; yeni kimlik `u-urun-hata`. | bUnit E1×2/E2; başsız Chrome 11 ölçüm (T2.6) |
| Hata metinleri tezgâhtara yanlış/anlaşılmaz ("Sayılan adet çok büyük", "kur fotoğrafına bağlanır") | KRİTİK | Yeni sözlük `Kuyumcu.Core/I18n/Sozlukler.UrunAcilis.cs` (`UrunAcilisSozlugu`, TR+EN, konumsal record = eksik anahtar derleme hatası): `AcilisCokBuyuk` "Açılış stoğu çok büyük — toplam ağırlık kaydedilebilecek sınırı aşıyor. Adedi azaltın." · `AcilisKurYok` "Altın fiyatları henüz gelmedi. Birkaç saniye sonra tekrar Kaydet'e basın ya da Kur ekranından fiyat girin." `Olustur` bu iki kapıyı kart yazıldıktan SONRA ama commit'ten ÖNCE, `StokSayimGuncelle` ile aynı ölçüyle (adet tavanı, adet×has tavanı, kiracıda kur var mı) sınar; son söz yine `StokSayimGuncelle`'dedir. Ürün kartı hiçbir koşulda yazılmaz (işlem). | A1/A6/A1_HTTP + canlı A1/A6 |
| Sayım kapanışı çok adımlı ve işlemsiz (`SayimModulu.Kapat`) | ORTA | `Kapat` `await using var tx = await db.Database.IsBaslatVeyaDevral()` içinde; `StokSayimGuncelle` zaten devralıyor; tek `CommitAsync`. Satır sırası `OrderBy(s => s.Id)` ile sabitlendi (ULID = açılış sırası). Tarama tablosuna eklendi (T2.3). | A5 + canlı A5 + M5-1 |
| Sipariş kapora `CariModulu.cs` `BeginTransactionAsync` | DÜŞÜK | DOKUNULMADI — dosya TCKN-DOGRULAMA şeridinin alanında ve şerit bu dalgada eşzamanlı koşuyor (görev sınırı: "DOKUNMA"). Açık kalan, sahibiyle (§ T2.9). | — |
| Canlı betik hata dalında yanıt gövdesi basıyordu | DÜŞÜK | Tur 2 betiği (`canli2.py`) auth adımlarında yalnız durum kodu basar (`kayit HTTP {kod} (gövde basılmaz)`); diğer adımlarda gövde yalnız `message`/`alanlar` alanlarına süzülür (`alan()`), jeton taşıyan alan hiç basılmaz. Bu turda hiçbir jeton/parola çıktıya düşmedi. | `/private/tmp/kuyumcu-serit/URUN-ACILIS-ATOMIK/canli2.py` |
| "Sayım kaydı için en az bir kur girişi gerekir…" jargon | DÜŞÜK | Ürün kartı yolunda artık bu metin hiç görünmez (yeni sözlük metni). Sayım ekranındaki aynı metin sayım ucunun metnidir; sade dil kuyruğuna (açık kalan). | ekran görüntüleri kuryok__* |

## T2.2 Sapmalar ve gerekçeleri
- Bulgu "kur kontrolü ürün eklenmeden önce" de önermişti: kapı bilerek kart `SaveChanges`'inden SONRA ama aynı işlemde konuldu — böylece işlem sınırı (M1-1) hâlâ ölçülebilir; kart commit edilmediği için kullanıcı açısından sonuç aynıdır (0 satır).
- Sayım kapanışında satır başına iç `SaveChanges` korunur (StokSayimGuncelle'nin sözleşmesi); atomiklik dış işlemle sağlanır.
- Sayım ucunun kendi metni ("Sayılan adet çok büyük…") değiştirilmedi: sayım ekranında kullanıcı gerçekten sayım yapıyor; metin kapsam dışı (sade dil kuyruğu).
- Alanlı hatada `kuyumcuOdak.alana` kutuyu `scrollIntoView({block:'center'})` ile ortalar; 1280 PRO'da kutunun altındaki satır içi mesaj sabit altlığın hemen arkasında kalabiliyor (bkz. tavan__PRO_karanlik_1280.png) — aynı metin altlıktaki bantta TAM görünür. Ortak `cekmece.js` başka şeritlerin de kullandığı dosya olduğundan değiştirilmedi; açık kalan (düşük).

## T2.3 Atomiklik taraması — ek satır
| Uç | Adımlar | Tek işlem mi (önce → sonra) | Red sonrası kalan satır (önce → sonra) |
|---|---|---|---|
| POST /sayim/{id}/kapat | satır başına StokSayimGuncelle (SAYIM_DUZELTME + stok + parti) → satır.DuzeltmeHareketId → oturum KAPANDI | HAYIR (her satır kendi commit'i) → **EVET** | önceki satırların defter/stok/parti satırları + oturum AÇIK → **0** (oturum AÇIK, düzeltme yok) |
| POST /urunler (açılış stoğu, tur 2) | kart → (yeni) tavan/kur ön kapısı → StokSayimGuncelle | EVET (tur 1) | 0; red artık `alanlar.acilisStogu.adet` ile, tezgâhtar dilinde |

## T2.4 Testler (tur 2)
Yeni/güncellenen (dosya `dotnet/Kuyumcu.Tests/UrunAcilisAtomikTestleri.cs`):
- `UrunAcilisAtomikTestleri.A1_…` — artık metin `UrunAcilisSozlugu.Tr.AcilisKurYok` ve `AlanIsareti.Alan == "acilisStogu.adet"` sınanır.
- `UrunAcilisAtomikTestleri.A5_Sayim_kapanisinda_ikinci_satir_reddedilince_ilk_satirin_defteri_YAZILMAZ_oturum_ACIK` (YENİ)
- `UrunAcilisAtomikTestleri.A6_Acilis_stogu_has_tavanini_asinca_kart_dilinde_REDDEDILIR_ve_kart_YAZILMAZ` (YENİ)
- `UrunAcilisAtomikHttpTestleri.A1_HTTP_…` — Türkçe JSON gövdesi AYRIŞTIRILIR (`message` + `alanlar["acilisStogu.adet"][0]`).
- `UrunAcilisAtomikEkranTestleri.E1_Acilis_stogu_reddi_kutunun_ALTINDA_gorunur_Daha_fazla_ACILMAZ_odak_kutuya` (BASIT + PRO, YENİ bUnit)
- `UrunAcilisAtomikEkranTestleri.E2_Alansiz_sunucu_hatasinda_yeni_urun_penceresi_BANDA_kaydirir` (YENİ bUnit)
Filtreli koşumlar: `UrunAcilisAtomik|SayimOturumu` **29/29**; ürün ekranı/sunucu geniş filtre (`UX4d|S33Urunler|P10Ekran|P16Ekran|KesifP4|UX1C1|UrunEkran|P22Sunucu|S3cForm|UX1C2|PatchSozlesme|Urun`) **825/825**. Paket test sayısı 6 → 11 (+5 yeni: A5, A6, E1×2 vaka, E2; E1 iki vakalı Theory).

## T2.5 Mutasyon (tur 2) — yedek `/private/tmp/kuyumcu-serit/mut-yedek/URUN-ACILIS-ATOMIK-t2/`, her biri geri alındı, `cmp` AYNI
| Kod | Değişiklik | Kırmızı testler |
|---|---|---|
| M1-1 (tekrar) | `Olustur` işlem sınırı kaldırıldı (`tx = null`) | A1, A2, A1_HTTP, A6, Dogrulayici.URUNACILISATOMIKSinirTestleri.S3_Cok_buyuk_adet_… (5/49) |
| M5-1 | `SayimServisi.Kapat` işlem sınırı kaldırıldı | A5 (1/49) |
| M6-1 | `Olustur` kur-yok ön kapısı kaldırıldı | A1, A1_HTTP (2/49) |
| M6-2 | `Olustur` tavan ön kapısı kaldırıldı | A6 (1/49) |
| M7-1 | `YeniEsleme`'den `acilisStogu.adet` eşlemesi silindi | E1(BASIT), E1(PROFESYONEL) (2/3) |
| M7-2 | `OdakIlkHataya`'dan `u-urun-hata` dalı silindi | E2 (1/3) |
| M7-3 | Bant kimliği `u-urun-hata` kaldırıldı | E1×2, E2 (3/3) |
Not (Anayasa md.1): ilk koşumda M6-1/M6-2/M7-2 başka şeridin anlık derleme kırığı (GirdiAilesi2Tur3Testleri.cs CS1503; wasm paketleme MSB4018) yüzünden DERLENEMEDİ; kırık kapanınca üçü yeniden koşuldu ve yukarıdaki kırmızıları verdi. Günlükler `/private/tmp/kuyumcu-serit/URUN-ACILIS-ATOMIK/mut/`. Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: 0 hata, 4 uyarı (hepsi test projesinde başka şeritlerin: SUBEKAPSAMASinir CS8601, KesifP2 xUnit2029, SonKalanlar xUnit2013×2) — RAPOR şeridinin yarım `Raporlar.razor`/`KarZarar.razor` düzenlemesi iki denemede derlemeyi kırdı, üçüncüde yeşil.

## T2.6 Canlı doğrulama (API 5166, `kuyumcu_net`, bağımsız SQL) — `ham/URUN-ACILIS-ATOMIK-olcum/tur2/canli2-tam.json`
Kiracı `01M4DDD3PQSFV356FZQB9AAT7H` (PAKETURUN-ACILIS-ATOMIK-TEST, davet+kayıt ürün uçlarından).
| Adım | Beklenen | Gerçek |
|---|---|---|
| A1 POST /urunler, kur 0, açılış 5 | 400 + tezgâhtar metni + `alanlar.acilisStogu.adet` + SQL 0 | 400 "Altın fiyatları henüz gelmedi…" `alanlar.acilisStogu.adet` · ürün/stok/parti/hareket 0/0/0/0 |
| A1 aynı istek tekrar | 400 (409 değil) | 400 · 0/0/0/0 |
| Kur manuel + aynı kod | 2xx + 1/1/1/1 | 201 · 1/1/1/1 |
| Üçüncü deneme | 409 alan kod | 409 `alanlar.kod` · ürün=1 |
| A6 gram 99 999 × açılış 1 000 000 | 400 "Açılış stoğu çok büyük…" + alan + SQL 0 | 400 aynı metin, `alanlar.acilisStogu.adet` · 0/0/0/0 |
| A5 sayım: satır1 5→3, satır2 200 000 000 | 400; SAYIM_DUZELTME, stok1, oturum, bağlı satır değişmez | 400 · önce 2/5.000/ACIK/0 → sonra 2/5.000/ACIK/0 |
| A5 düzeltip tekrar kapat | 200 uygulanan 1, +1 düzeltme, stok 3, KAPANDI | 200 uygulanan 1 · 3/3.000/KAPANDI/1 |
| A3 mal kabul, kiracıda olmayan kur | 4xx, irsaliye/kalem/hareket değişmez | 404 · 0/0/3 → 0/0/3, kabul 0.000 |
| A3 düzeltilip tekrar | 200, 1 irsaliye, bağsız 0 | 200 IRS-2026-00001 KISMI_KABUL · 1/0/2.000 |
| A4 ekstre onayı, satır mükerrer | 200 basarili 0, adet/bakiye değişmez, HATALI | 200 basarili 0 · 1/100.00 → 1/100.00/HATALI |

## T2.7 Ekran — başsız Chrome (cdp.mjs, CDP 9508, vekil 5167), GERÇEK fare tıklaması (`Input.dispatchMouseEvent`) Kaydet nerede duruyorsa orada
Senaryo `tavan`: ad/kod/gram 99 999/açılış 1 000 000 → Kaydet. Senaryo `kuryok`: gram 10/açılış 5, tıklamadan hemen önce kiracının kurları SQL ile silinir. Ölçüm 2,4 sn sonra; `sikisma-olc.js` `__olc()`. Betik `/private/tmp/kuyumcu-serit/URUN-ACILIS-ATOMIK/ui2.mjs`, ham `ham/URUN-ACILIS-ATOMIK-olcum/tur2/ui2.jsonl`.
| Senaryo | Mod | Tema | Gen. | DB önce→sonra (ürün/stok/parti/hareket) | Bant ekranda | Kutu altı mesaj | Kutu kırmızı | Kaydet ekranda | Daha fazla | Odak | K1 | K3 | K5 | K9 | K10 | K11 | Y |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| tavan | BASIT | açık | 1280 | 0/0/0/0→0/0/0/0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | BASIT | açık | 375 | 0→0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | BASIT | koyu | 1280 | 0→0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | BASIT | koyu | 375 | 0→0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | PRO | açık | 1280 | 0→0 | evet | evet | evet | evet | — | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | PRO | açık | 375 | 0→0 | evet | evet | evet | evet | — | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | PRO | koyu | 1280 | 0→0 | evet | evet | evet | evet | — | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| tavan | PRO | koyu | 375 | 0→0 | evet | evet | evet | evet | — | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| kuryok | BASIT | açık | 1280 | 0→0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| kuryok | BASIT | açık | 375 | 0→0 | evet | evet | evet | evet | kapalı | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
| kuryok | PRO | koyu | 375 | 0→0 | evet | evet | evet | evet | — | u-acilis-stok | 0 | 0/0 | 0 | 6 | 0 | 0 | 0 |
Görüntüler (`ham/URUN-ACILIS-ATOMIK-olcum/tur2/<senaryo>__<MOD>_<tema>_<gen>.png`) Read ile GÖRÜLDÜ: tavan BASIT açık 1280 ve 375, tavan PRO koyu 1280, kuryok BASIT açık 375 — kırmızı bant SABİT altlıkta Kaydet'in hemen üstünde, açılış stoğu kutusu kırmızı ve odaklı, 'Daha fazla' kapalı (form uzamadı), yatay kaydırma yok. Tur 1'deki hakem ölçümüyle karşılaştırma: BASİT'te bantEkranda [] → evet, odak BODY → u-acilis-stok.
K9=6 (binen): örnekler `span.secici-metin "Hepsi" ⟂ #u-maden-kilit`, `th "ÜRÜN ADI" ⟂ span.metin-soluk` — açık pencerenin ARKASINDAKİ liste/süzgeç öğeleriyle pencere içeriğinin kesişimi (ölçer örtüyü ayırmıyor); tur 1 ve hakem ölçümünde de aynı 6; pencere içinde binen öğe yok (görüntüler).
Dar sütun (K2): pencere ölçümü, ana alan ölçüsü uygulanmaz.

## T2.8 Kapı (paket ölçüsü, tur 2)
1. Build: `Kuyumcu.sln` 0 hata, 4 uyarı (başka şeritlerin test uyarıları).
2. Paket testleri 0 başarısız (29/29; geniş filtre 825/825).
3. denetim.sh: 313 ✓, 2 ✗ — `P-30 DEMO_INSAN_SURESI` (kapsam dışı istisna) ve `Sınırsız okuma beyaz listesi` (`KarZararServisi.cs:UrunBazli → UrunSatirlari`; RAPOR şeridinin eşzamanlı işi — bu paket KarZararServisi'ne dokunmadı). Bu paketin sayaçları ✓ (ModalPencere düzenleme kullanımı artmadı: yeni ürün penceresi zaten ModalPencere idi, yalnız `Altlik` yuvası kullanıldı).
4. Mutasyon 7/7 kırmızı, hepsi cmp AYNI (T2.5).
5. Canlı T2.6 + ekran T2.7.
6. Test kiracısı `01M4DDD3PQSFV356FZQB9AAT7H` `zsh kiraci-temizle.sh … --uygula` ile silindi; davet satırı silindi; SQL: kiracı 0, ürün 0, `PAKETURUN-ACILIS-ATOMIK%` davet 0; tenants 16 → 16. Fikstür `printf ''` ile boşaltıldı. 5166/5167 kapatıldı (`lsof` boş).
7. Tam süit (TEK koşum, son kod, `kuyumcu_test_t5`, trx `/private/tmp/kuyumcu-serit/trx-pURUN-ACILIS-ATOMIK-t2/_Ramazan-MacBook-Air_2026-10-08_12_33_02.trx`): **11023 toplam · 11012 geçti · 9 başarısız · 2 atlandı**. Bu paketin 11 testinin TAMAMI yeşil. Kırmızılar ve sahipleri:
   - `RaporDortTestleri` ×6 (RD01, RD02, RD02b×2, RD_KH01, RD_KH02) — RAPOR-4 şeridi; test dosyası koşum sırasında (12:33) yeniden yazılıyordu, ilk koşum denemesi bu dosyanın derleme hatasıyla (`KarZararServisi.R2` yok) hiç başlayamadı.
   - `Toplu5GunSonuTestleri.T5_PRO_gun_sonu_durumu_cumle_duzeninde_formul_ipucunda` — filtreli tekrarda da kırmızı ("Fark var" bulunamadı); gün sonu `Raporlar.razor` 12:24'te RAPOR şeridince değiştirildi. Bu paket Raporlar'a dokunmadı.
   - `Dogrulayici.MASAK370SinirHttpTestleri.H1_…` ve `Dogrulayici.Q105SinirTestleri.C_Vezne_…` — filtreli tekrarda YEŞİL (`tekrar.log`: 33 testten yalnız T5 kırmızı) → paralel yük kararsızlığı; MASAK/Vezne alanı (YASAL-PARAM/TCKN şeritleri).
   Tur 1 devir listesiyle (P31×11, R3_33, Yasal4 Mutabakat) kesişim YOK ve hiçbiri Ürün/Sayım/ürün ekranı yoluna değmiyor → bu paketin ürettiği yeni kırmızı **0**.

## T2.9 Açık kalanlar (sahibiyle)
- Sipariş kapora `CariModulu.cs` `BeginTransactionAsync` → `IsBaslatVeyaDevral` (TCKN-DOGRULAMA şeridi kapanınca tek satır; sahibi: bu paketin devamı/orkestratör).
- Sayım ucunun kendi metni "Sayım kaydı için en az bir kur girişi gerekir — hareket kur fotoğrafına bağlanır" ve "Sayılan adet çok büyük…" — sade dil kuyruğu (öneri: "Önce bugünün altın kurunu girin, sonra sayımı kapatın.").
- `cekmece.js` `alanaOdakla` sabit altlığı hesaba katmıyor: 1280 PRO'da kutu altı mesaj altlığın arkasında kalabiliyor (aynı metin bantta tam görünür) — ortak dosya, düşük.
- Fatura TaslakKalemGuncelle ve POS EslesmeGeriAl (tur 1'den, düşük).
- denetim.sh `Sınırsız okuma beyaz listesi` (KarZararServisi — RAPOR şeridi) ve P-30 (kapsam dışı).
- Tam süit kırmızıları § T2.8/7 (RAPOR-4, RAPOR gün sonu, iki kararsız).

## T2.10 Dokunulan sayfalar (TEK DOKUNUŞ kaydı)
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor` — yeni ürün penceresi: `<ChildContent>`/`<Altlik>` ayrımı, `u-urun-hata`, `YeniEsleme` açılış eşlemeleri, `OdakIlkHataya` yeni ürün dalı. Gerekçe: tasarım hakemi KRİTİK bulgusu bu sayfanın davranışıdır; aynı paketin ikinci turu (ilk turda razor'a dokunulmamıştı).

## T2.11 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (açılış ön kapıları + `AcilisAlani` + şube hatalarına alan)
- `dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs` (Kapat tek işlem + sabit sıra)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.UrunAcilis.cs` (YENİ, TR+EN)
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Tests/UrunAcilisAtomikTestleri.cs`
Migration: gerekmedi (şema değişmedi).

## T2.12 Koşul karşılama (tur 2)
| Koşul | Durum | Kanıt |
|---|---|---|
| Kök neden + tek işlem, red → 0 satır | KARŞILANDI | tur 1 § 1-2; canlı A1/A6 0/0/0/0 |
| Tekrar deneme çift üretmez | KARŞILANDI | canlı A1 tekrar 400, sonra 201 tek ürün, üçüncü 409 |
| Tarama + bulunanları kapat (sayım kapanışı dahil) | KARŞILANDI (kapora hariç — şerit sınırı) | § 3 + § T2.3; A5 |
| Mutasyon: işlem sınırı kaldırılınca kırmızı, cmp AYNI | KARŞILANDI | § T2.5 (7/7) |
| Ekranda hata mesajı + kayıt yok, 1280+375 · BASİT+PRO · açık+koyu | KARŞILANDI | § T2.7 (11 ölçüm, görüntüler görüldü) |
| Tezgâhtar dili, Sozlukler TR+EN | KARŞILANDI | `Sozlukler.UrunAcilis.cs`; A1/A6/A1_HTTP eşitlik |
| Build 0 hata | KARŞILANDI | 0 hata / 4 uyarı (başkasının) |
| denetim TEMİZ (P-30 hariç) | KISMEN — tek ek ✗ RAPOR şeridinin KarZararServisi kalemi; bu paketin sayaçları ✓ | `ham/URUN-ACILIS-ATOMIK-olcum/tur2/denetim.log` |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § T2.8/7 |
| Test kiracısı silindi, tenants eski değerine döndü | KARŞILANDI | 16 → 16 |

## T2.13 Kurucu / mali müşavir
Mevzuat hükmü içermez (tavan kolon kapasitesidir); soru yok.
