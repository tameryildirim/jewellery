# HİZMET SEVİYESİ — bugün taahhüt edilebilenler

<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->

> **TASLAK — ilan edilmemiştir.** Bu belge sözleşme değildir; **bugün kanıtı olan**
> taahhütlerin listesidir. Kanıtı olmayan hiçbir sayı buraya yazılmaz (Anayasa md.1).
> Ürünün 1.0'da **neyi kapsamadığı** tek kaynaktan okunur: **`docs/HARIC-LISTESI.md`**.
>
> **Paket:** P-29 (S-1) · **Tarih:** 2026-09-23

---

## 0. Bu belgenin kuralı

Üç kutu vardır ve bir satır **ancak** birinde durur:

1. **TAAHHÜT EDİLEBİLİR** — mekanizması kodda, kanıtı ölçülmüş.
2. **YAPILANDIRILMIŞ, ÖLÇÜLMEMİŞ** — mekanizma var ama gerçek koşum kanıtı yok.
   Bu satırlar **müşteriye rakam olarak söylenmez**.
3. **TAAHHÜT YOK** — ölçüm de mekanizma da yok; dürüst cevap "bugün söz veremeyiz".

---

## 1. TAAHHÜT EDİLEBİLİR (kanıtlı)

| # | Taahhüt | Kanıt |
|---|---|---|
| S-1 | **Kiracı verisi başka kiracıya sızmaz.** Her kiracı varlığı `IKiraciVarlik` uygular, global sorgu süzgeci devrededir ve yazma denetçisi kiracı damgasını zorunlu kılar. | `dotnet/denetim.sh` → `IKiraciVarlik uygulayan varlık` sayacı; `IgnoreQueryFilters` kullanımı **0** (denetim.sh `.IgnoreQueryFilters()` sayacı — taze ölçüm; cırcır artmaya kapalı) |
| S-2 | **Deftere yazan tek yol vardır.** Hiçbir modül `Hareketler.Add` çağırmaz; tüm hareketler `HareketFabrikasi.Ekle`'den geçer. | `Kuyumcu.Api/Data/HareketFabrikasi.cs` + denetim.sh tek-yol sayacı |
| S-3 | **Finansal kayıt silinmez.** Hareket, kasa/banka hareketi ve fatura **hard DELETE** edilmez; geri alma yalnız storno / pasifleştirme yoluyla olur. | `Modules/Satis/StornoServisi.cs`, `StornoSuzgeci.cs`; denetim.sh `ExecuteUpdate/Delete` beyaz listesi **6** (denetim.sh `denetle "ExecuteUpdate/Delete"` satırı — taze ölçüm; 17.10'dan beri ham SQL de sayılır: 4 Auth + kur bakımı + satır kilidi; hiçbiri finansal kayıt silmez; cırcır artmaya kapalı) |
| S-4 | **Her yazma denetim izine düşer.** | Canlı koşum (2026-09-23, 5198): 8 adımlık kurulum→ilk satış turunda `denetim_kayitlari` **23 satır** |
| S-5 | **Verinizi dışarı alabilirsiniz** (veri taşınabilirliği, KVKK md.20). CSV dışa aktarım ekranı ve uçları üründedir. | `Kuyumcu.Web/Pages/VeriDisaAktarim.razor`, `Modules/Disaaktarim/` |
| S-6 | **Yedeğin geri yüklenebilirliği iddia değil, ölçümdür.** Yedek betiği yedeği alır, şifreler, **ayrı bir şemaya geri yükler** ve her tablonun `CHECKSUM TABLE` değerini kaynakla karşılaştırır; uyuşmazlık sıfırdan farklı çıkış kodu verir. | `dotnet/yedek-al-ve-dogrula.sh:1-20`, `docs/YEDEK-GERI-YUKLEME.md` |
| S-7 | **Sır ARGV'ye girmez.** Veritabanı parolası `--defaults-extra-file` ile verilir; betikte gömülü parola yoktur. | `dotnet/yedek-al-ve-dogrula.sh:18-21` |

---

## 2. YAPILANDIRILMIŞ, ÖLÇÜLMEMİŞ (rakam olarak SÖYLENMEZ)

| # | Konu | Bugünkü gerçek | Neden taahhüt değil |
|---|---|---|---|
| Y-1 | **Yedek sıklığı** | Zamanlayıcı dosyası tam yedeği **günde 4 kez** planlar: `03:15` ve `09/15/21:15` (Europe/Istanbul). | `deploy/systemd/kuyumcu-yedek-tam.timer:11-12`. Bu bir **şablon**; bugün çalışan bir üretim kurulumunda **koştuğu ölçülmedi** (üretim kurulumu yok — **K-6**). |
| Y-2 | **Geri yükleme tatbikatı sıklığı** | Tatbikat zamanlayıcısı **ayın 1'i 04:30**. | `deploy/systemd/kuyumcu-yedek-tatbikat.timer:11`. **P-33** tatbikat kanıtı gelmeden "ayda bir tatbikat yapılır" denmez. |
| Y-3 | **Geri yükleme süresi (RTO)** | **ÖLÇÜLMEDİ.** | Süre ancak temiz makinede sıfırdan kurulum + geri yükleme tatbikatıyla ölçülür → **P-33**. O ölçüm gelmeden dakika/saat yazılmaz. |
| Y-4 | **Veri kaybı penceresi (RPO)** | Yapılandırılmış sıklık **6 saatlik** bir pencere ima eder. | Bu bir **çıkarım**, ölçüm değil. Üretimde koşan zamanlayıcı kanıtı olmadan "en fazla 6 saat veri kaybı" **taahhüt edilmez**. |

---

## 3. TAAHHÜT YOK (bugün dürüst cevap)

| # | Konu | Neden |
|---|---|---|
| N-1 | **Çalışma süresi / erişilebilirlik (uptime %)** | Üretimde koşan bir kurulum ve izleme geçmişi **yok** (**K-6**). Rakip sayfalarındaki "%99,9" türü iddiaların bizde karşılığı **ölçüm değildir** — bu yüzden yazılmaz. |
| N-2 | **Destek yanıt süresi** | F-8 birim ekonomi belgesinde **destek satırı `OLCULMEDI`** damgalıdır; destek dakikası ölçüm hattı (P-28) kuruldu ama **gerçek yük yok**. Pilot (P-32) ölçmeden saat/dakika sözü verilmez. |
| N-3 | **Kapasite (aynı sunucuda kaç dükkân)** | F-8: *"100 kiracı/tek CX33 **VARSAYIMDIR**"* (FAIL-CLOSED 5). Yük testi (P-25) + telemetri rakamı gelmeden kapasite iddiası yazılmaz. |
| N-4 | **Fiyat / indirim / tasarruf oranı** | **K-16** kurucu imzasına bağlıdır; ayrıca F-8'de **2 satır `OLCULMEDI`** olduğu sürece FAIL-CLOSED 1 gereği **hiçbir kanalda rakam telaffuz edilmez**. |
| N-5 | **e-Fatura / ÖKC / SMS / banka API / imalat** | Bu sürümde **yok** — sebebi ve sınırı `docs/HARIC-LISTESI.md`de tek kaynakta yazılıdır. |

---

## 4. Bu belge ne zaman güncellenir

- **P-33** (kurulum + geri yükleme tatbikatı) kanıt üretince → Y-2, Y-3, Y-4 kutu değiştirir.
- **P-32** (pilot) destek dakikasını ölçünce → N-2 kutu değiştirir.
- **P-25** (yük testi) kapasiteyi ölçünce → N-3 kutu değiştirir.
- **K-16** imzası gelince → N-4 kutu değiştirir.

Kutu değiştiren her satırın yanına **kanıtın yolu** yazılır. Kanıtsız kutu değişimi
bu belgenin kuralını kırar.
