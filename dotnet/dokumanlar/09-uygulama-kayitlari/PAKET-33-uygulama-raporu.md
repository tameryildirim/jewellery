# PAKET 33 — MANUEL BORÇ/ALACAK (DÜZELTME) FİŞİ — BİTTİ
Tarih: 2026-09-15/16 · Rol: Taşkın (03 Backend) + Ertuğ (03 Frontend) · Model: Opus 5

## 1. Ne yapıldı
`POST /api/v1/cari/{id}/duzeltme` — gerekçeli (≥ 20 karakter) elle bakiye düzeltmesi.
Deftere `CARI_DUZELTME` hareketi (`OdemeIzi.Yok` → nakit/veresiye payı ve vade YOK),
bakiye `BakiyeGuncelle(id, tenant, tlDelta, hasDelta, now)`, hareket işaretleri
VERESİYE sözleşmesiyle aynı (`TlTutari = +tlDelta`, `HasGrami = -hasDelta`),
`[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`, MASAK/tevsik ÇAĞRILMIYOR (para el değiştirmez).

### Mühürlü kararların uygulanışı
| Karar | Uygulama |
|---|---|
| **Q1 = B** | `DenetimServisi.YazmaKaydet(islem, varlikTuru, varlikId, detay, hassas = true)` eklendi (`Data/DenetimIzi.cs`). `Olustur()` yolunu kullanır, `Hassas` PARAMETREDEN gelir (HassasVarliklar listesine EKLEME yapılmadı), **SaveChanges çağırmaz** → çağıranın tx'i içinde tek SaveChanges ile yazılır. `Duzeltme` bunu `VarlikTuru="CariDuzeltme"`, `Detay = "tl=+1250 has=-0.5 · <gerekçe>"` ile çağırır. Gerekçe ayrıca `hareketler.Aciklama`'da. |
| **Q2 = B** | `Sabitler.HareketTurleri` → `CARI_DUZELTME`, `SATIS_IADE`, `ALIS_IADE` eklendi. `MasakKapisi` tür listesine DOKUNULMADI. Tarama sonucu: `SERTIFIKA` bir **Hareket türü değil** (`UrunBelgesi.Tur`, `BelgeModulu.cs:33`) → listeye girmedi. |
| **Q3 = B** | `Cariler.razor`'a `@inject OturumServisi Oturum`; `DuzeltmeGorebilir => Oturum.Kullanici?.Rol is "SAHIP" or "MUDUR" or "MUHASEBE"`; düzeltme düğmesi bu koşulla çizilir. Sunucudaki `[RolGerekli]` ve `HataBandi` yerinde; Tahsilat/Veresiye düğmelerine dokunulmadı. |
| **Q4 = B** | **Hiçbir CSS değişmedi.** `.uyari-kutu` app.css:392-399'da ZATEN VAR (A17'deki "app.css'te yok" notu YANLIŞ — kanıt: `Kuyumcu.Web/wwwroot/css/app.css:392`). `Cariler.razor:226` olduğu gibi kaldı. |

## 2. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son temiz koşu, mutasyonlardan SONRA).

## 3. Test koşumları
| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `~CariDuzeltmeTestleri` | 5 | 5 | 0 |
| `~HttpCariDuzeltmeTestleri` | 3 | 3 | 0 |
| `~CariEkranTestleri` | 22 | 22 | 0 |
| **Tam süit — koşu 1** | **971** | **971** | **0** (53,3 sn) |
| **Tam süit — koşu 2** | **971** | **971** | **0** (28,9 sn) |

Taban 957 → 971 (+14): CariDuzeltme 4 (paket 33'te yazılmıştı, ilk kez derlendi/koştu)
+ HareketTurleri listesi 1 + ekran 6 + HTTP 3.
trx: `.../scratchpad/trx/`

## 4. Mutasyon kanıtları
| Kod | Değişiklik | Kırmızıya dönen test | Geri alım |
|---|---|---|---|
| M104 | `gerekce.Length < 20` kontrolü silindi | `CariDuzeltmeTestleri.Gerekce_20_karakterden_kisa_REDDEDILIR_ve_sifir_delta_REDDEDILIR` | `cmp` birebir |
| M105 | `VadeKapisi.KrediLimitiDogrula` çağrısı silindi | `CariDuzeltmeTestleri.Borc_artiran_duzeltme_kredi_limitine_TAKILIR_azaltan_takilmaz` | `cmp` birebir |
| M106 | sıfır delta kontrolü silindi | `CariDuzeltmeTestleri.Gerekce_20_karakterden_kisa_REDDEDILIR_ve_sifir_delta_REDDEDILIR` | `cmp` birebir |
| M107 | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` silindi | `HttpCariDuzeltmeTestleri.TEZGAHTAR_duzeltme_fisi_kesemez_403` | `cmp` birebir |
| M108 | `#dz-kaydet` disabled koşulundan gerekçe şartı silindi | `CariEkranTestleri.Gerekce_19_karakterse_KAYDET_KAPALI_20_karakterde_ACILIR` | `cmp` birebir |
| M109 | `denetim.YazmaKaydet(...)` çağrısı silindi | `CariDuzeltmeTestleri.Duzeltme_deftere_CARI_DUZELTME_yazar_ve_bakiyeyi_delta_kadar_degistirir` | `cmp` birebir |

Her mutasyonda 30 testlik kapsamda tam olarak **1 kırmızı / 29 yeşil**. Test açığı bulunmadı.
Kaynak yedekleri: `.../scratchpad/mut-yedek/` (proje ağacına `.mutorj`/`.bak` bırakılmadı).

## 5. denetim.sh
`DENETİM TEMİZ.` — `Hareketler.Add` = **1** (fabrika kullanıldı), `IKiraciVarlik` 31,
`IgnoreQueryFilters` 0, `Find/FindAsync` 0.

## 6. Canlı doğrulama (port 5198, kiracı "DUZELTME-TEST Kuyumcu", SİLİNDİ)
| Adım | Beklenen | Gerçek |
|---|---|---|
| kısa gerekçe | 400 | **400** — `The field Gerekce must be ... minimum length of '20'` (denetleyici DataAnnotations) |
| sıfır delta (tl=0, has=0) | 400 | **400** — `Düzeltme için en az bir fark girilmelidir (TL veya has gram)` |
| TEZGAHTAR token | 403 | **403** — `Bu işlem için gerekli rol: SAHIP veya MUDUR veya MUHASEBE` |
| SAHIP +1250 / −0,5 | 200 | **200** — yanıt: `bakiye.tlBakiye = 1250.00`, `bakiye.hasGramBakiye = -0.500000` |
| kredi limiti 1000 iken +1500 | 400 | **400** — `Kredi limiti aşılıyor. Mevcut bakiye: 1250 TL, Eklenecek: 1500 TL, Limit: 1000.00 TL` |

### SQL (kuyumcu_net)
```
hareketler:  tur=CARI_DUZELTME · tl_tutari=1250.00 · has_grami=0.500000 ·
             nakit_tutar=0.00 · veresiye_tutar=0.00 · vade_tarihi=NULL ·
             aciklama="Düzeltme fişi: Eski sistemden devir farkı — mutabakat 2026-09" ·
             kullanici_id DOLU
cari_bakiyeler: tl_bakiye=1250.00 · has_gram_bakiye=-0.500000
denetim_kayitlari:
  EKLEME · CariDuzeltme · hassas=1 · detay="tl=+1250 has=-0.5 · Eski sistemden devir farkı — mutabakat 2026-09" · kullanici_id DOLU
  EKLEME · Hareket      · hassas=0 · detay=NULL   ← otomatik dinleyici (Mehmet'in tespiti doğrulandı)
```
Gerekçe metni hem `hareketler.aciklama`'da hem `denetim_kayitlari.detay`'da **birebir** (LIKE = 1).

### Temizlik
API yalnız 5198 PID'i (30740) durduruldu; **5199'daki kurucunun API'sine (PID 4683) dokunulmadı** — hâlâ ayakta.
Test kiracısı `tenant_id` kolonu olan **31 tablodan** + `tenants`'tan silindi.
`SELECT COUNT(*) FROM tenants` = **5** · `hareketler` içinde `CARI_DUZELTME` = **0** ·
test kiracısının `denetim_kayitlari` satırı = **0**. (Şemada `tenant_id` taşımayan tek tablolar:
`tenants` ve `__EFMigrationsHistory` — ikisi de ele alındı; yetim satır yok.)

## 7. Değişen dosyalar
```
dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs        (CariDuzeltmeDto + Duzeltme servisi + denetleyici ucu)
dotnet/Kuyumcu.Api/Data/DenetimIzi.cs                (DenetimServisi.YazmaKaydet)
dotnet/Kuyumcu.Api/Data/Entities.cs                  (HareketTurleri: +CARI_DUZELTME, +SATIS_IADE, +ALIS_IADE)
dotnet/Kuyumcu.Web/Pages/Cariler.razor               (OturumServisi enjeksiyonu + rol koşullu düğme)
dotnet/Kuyumcu.Tests/CariDuzeltmeTestleri.cs         (denetim izi assert'leri + HareketTurleri testi)
dotnet/Kuyumcu.Tests/CariEkranTestleri.cs            (Ciz(rol) + 6 düzeltme modalı testi)
dotnet/Kuyumcu.Tests/HttpCariDuzeltmeTestleri.cs     (YENİ — 3 HTTP testi)
```
Migration YOK (yeni sütun yok). **Commit yapılmadı.**
