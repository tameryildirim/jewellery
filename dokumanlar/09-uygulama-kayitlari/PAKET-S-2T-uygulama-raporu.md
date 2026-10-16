# PAKET S-2T — Yanıt şekli ↔ istemci modeli taraması (uygulama raporu)

**Tarih:** 2026-09-17 · **Dalga:** 2c · **Rol:** Taşkın (backend) + Ertuğ (frontend)
**Kapsam:** SALT OKUNUR TARAMA — kod değiştirilmedi. Ana çıktı:
`dokumanlar/09-uygulama-kayitlari/YANIT-SEKLI-TARAMASI.md`

## 1. Yapılanlar
1. Kuyumcu.Web'deki tüm `record` tanımları çıkarıldı (167; yalnız `Pages/` altında 122).
2. Tipli API çağrıları (`GetAsync<T>` vb., `JsonElement` hariç) çıkarıldı: **132**.
3. API tarafında 174 uç (Route + `[Http*]`) ve dönüş şekilleri (kontrolör → servis
   zinciri, 2 sıçramaya kadar) çıkarıldı.
4. İstemci alan kümeleri ↔ API anonim nesne/varlık alan kümeleri karşılaştırıldı;
   otomatik çıkarımın **şüpheli/kısmi** işaretlediği 14 satırın **tamamı elle koda
   bakılarak** doğrulandı.
5. Bulgular ciddiyete göre sıralandı ve UX-1-C1..C4 paketlerine eşlendi.

## 2. Sonuç (132 çağrı)
| Durum | Adet |
|---|---|
| UYUMLU | 69 |
| UYUMSUZ-ÇÖKER | **0** (kurulun 2 kalemi S-2'de kapandı) |
| UYUMSUZ-SESSİZ | **2** |
| DOĞRULANMADI | 61 |

**B-1 (orta):** `Pages/Raporlar.razor:257` `NetHasGram` ↔ API `netHasGramHareketi`
(`Modules/Rapor/RaporModulu.cs:175`) → alan daima null. → UX-1-C3.
**B-2 (düşük):** `Pages/Islemler.razor:638` `UrunVeri.Ayar` — `Urun` varlığında
(`Data/Entities.cs:193-305`) karşılığı yok → daima null (ölü yedek). → UX-1-C1.

S-2'de kapanan iki kalem:
- Stok NRE: `Urunler.razor:661 StokYaniti(List<StokVeri>? SubeStoklar)` ↔ `UrunModulu.cs:659-679 subeStoklar` — **uyumlu**.
- `/uyum` deserialize: `UyumServisi.cs:25 List<KimlikTespitSatir>` ↔ `MasakModulu.cs:77` dizi döner — **uyumlu**.

## 3. Sapmalar ve gerekçeleri
- **Spesifikasyon "101 record" diyor; bugün 122 (Pages) / 167 (tüm Web).** Dalga 2a/2b'de
  yeni sayfalar eklendi. Spesifikasyonun HEDEFİNE (uç bazında tam tarama) sadık kalınarak
  güncel sayı üzerinden çalışıldı; `denetim.sh` sayacı da 122 tavanını doğruluyor.
- **DOĞRULANMADI kutusu boş bırakılmadı, doldurulmadı da.** 61 çağrının ucu ham varlık/
  liste döndürüyor ya da çok katmanlı; Madde 1 gereği tahmin edilmedi, "uyumlu" sayılmadı.
  Bu sınıfın kapanışı SÖZLEŞME-1 (Kuyumcu.Contracts) paketinin işidir.

## 4. Migration
**gerekmedi** — şema değişikliği yok, kod değişikliği yok.

## 5. Derleme / test
- **Derleme:** çalıştırılmadı — kaynak ağaçta tek bayt değişmedi (salt okunur paket;
  görev tanımı: "derleme/test/git YOK").
- **Test koşumu:** yok (aynı gerekçe). Yeni test sayısı: 0.
- **Mutasyon:** yok (değiştirilecek kod yok).

## 6. denetim.sh
`cd dotnet && ./denetim.sh` → **DENETİM TEMİZ.**
İlgili sayaçlar: `Pages içinde istemci record tanımı 122 (tavan 122)`,
`Finansal varlıkta [HttpDelete] 0`, `HasQueryFilter 2`, `IKiraciVarlik uygulayan varlık 40`.

## 7. Canlı doğrulama
**Yapılmadı / gerekmedi.** Paket kod değiştirmiyor, yeni uç açmıyor, veri yazmıyor;
spesifikasyonun "canlı doğrulama" listesi bu paket için boştur. API 5198 başlatılmadı,
**test kiracısı açılmadı** → `tenants` sayısı değişmedi (dokunulmadı). `QA-TEST-` önekli
kalıcı fikstürlere dokunulmadı.

## 8. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` — **bu paketin eklediği tek şey:**
- `dokumanlar/09-uygulama-kayitlari/YANIT-SEKLI-TARAMASI.md` (yeni)
- `dokumanlar/09-uygulama-kayitlari/PAKET-S-2T-uygulama-raporu.md` (yeni, bu dosya)

Çıktıdaki diğer tüm `M`/`??` satırları **oturum başındaki snapshot ile aynıdır**
(önceki paketlerin çalışma ağacı); bu pakette hiçbiri açılmadı/yazılmadı.
Geçici dosyaların tamamı scratchpad altında kaldı; proje ağacına `.tmp/.bak/.mutorj` bırakılmadı.

## 9. DoD sözleşmesi
Bu paket **yeni varlık/kayıt türü doğurmuyor** (veri modeli, uç, ekran, form yok).
Dolayısıyla PATCH/DELETE/[RolGerekli]/denetim kaydı/düzenleme yolu yükümlülüğü
**doğmuyor**; salt okunur belge üretimidir. Değişmezlik gerekçesi: üretilen çıktı bir
analiz belgesidir, kullanıcı verisi değildir; düzeltme yolu belgenin yeniden üretilmesidir.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Rapor `YANIT-SEKLI-TARAMASI.md` var | ✅ | dosya yazıldı |
| Tabloda her satırda dosya:satır kanıtı | ✅ | tarama raporu §1-§3 |
| Kaynak ağaç değişmedi | ✅ | yalnız 2 yeni belge |
| denetim.sh TEMİZ | ✅ | "DENETİM TEMİZ." |
| Tam süit koşulmadı | ✅ (beklenen) | değişiklik yok |
| Uyumsuzlar ciddiyete göre sıralı | ✅ | çöken 0 → sessiz 2 |
| UX-1-C eşlemesi | ✅ | tarama raporu §4 |
| `MIMARI-KALAN-PAKETLER` / `HAZIRLIK-KURULU-2` / `SONRAKI-OTURUM-PROMPT` dokunulmadı | ✅ | yalnız okundu |

## 11. Açık kalanlar
1. 61 çağrı DOĞRULANMADI → SÖZLEŞME-1.
2. B-1, B-2 düzeltmeleri UX-1-C3 / UX-1-C1'e devredildi (bu pakette yapılmadı).
3. Ham varlık serileştirmesi (`tenantId` istemciye iniyor) → Mehmet (06 Güvenlik).
4. 8 adet `.mutorj` artığının silinmesi kurucuya bırakıldı (hook `rm` blokluyor).
5. Adım ekonomisi: hedef ≤40 araç çağrısıydı; şüpheli satırların elle doğrulanması
   (Madde 1: tahmin yok) nedeniyle ~48 çağrı kullanıldı. Kayda geçirilir.

## 12. Kurucuya / mali müşavire not
Mevzuat hükmü koda yazılmadı, yorumlanmadı. Tek uyarı: gider pusulası belge ucunun
(`/gider-pusulalari/{id}/belge`) yanıt şekli doğrulanamadı; o ekranın mevzuata uygun alan
kümesi taşıdığı **iddia edilmiyor**, UX-1-C3'te ayrıca doğrulanmalıdır.
