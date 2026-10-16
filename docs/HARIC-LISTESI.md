# HARİÇ LİSTESİ — 1.0 sürümünde NE YOK

> **TASLAK — ilan edilmemiştir.** Bu belge dışarıya (müşteri, pilot, kanal) ancak
> kurucu imzasıyla gider. Rakamlı fiyat ilanı **K-16 kurucu imzasına** bağlıdır ve
> bu belgede fiyat yoktur.
>
> **Paket:** P-29 (S-1) · **Kimlikler:** R-3, R-8, D-8 · **Tarih:** 2026-09-23
> **Köprü K-1 / BK-26 koşulu:** *kısıt beyanının **TEK KAYNAĞI** bu dosyadır.*

---

## 0. Bu dosya niçin tek kaynak

Aynı kısıt iki yerde elle yazılırsa er geç ikisi birbirinden ayrışır ve biri
müşteriye yanlış söz verir. Bu yüzden **kural**: pilot daveti, demo kiracısının
giriş ekranı, kurulum kılavuzu, satış materyali — hepsi bu dosyayı **gömerek**
(bağlantı vererek / metnini buradan üreterek) kullanır, kendi cümlesini **yazmaz**.

Bu kural `dotnet/denetim.sh` içindeki **`HARIC_BEYAN_KOPYA`** sayacıyla ölçülür
(blok: `# >>> P-29`). Sayaç, bu dosyanın dışında elle yazılmış kısıt beyanı
arar; tavan yalnız **azalır**.

Bir belge bu dosyaya dayandığını şu işaretle bildirir (sayaç bu işareti arar):

```
<!-- HARIC-KAYNAK: docs/HARIC-LISTESI.md -->
```

---

## 1. HARİÇ — 1.0'da YOK (kanıtlı)

| # | Kalem | Durum | Kanıt (bugün depoda ölçüldü) | Sebep / kurucu kalemi |
|---|---|---|---|---|
| H-1 | **e-Fatura / e-Arşiv / e-İrsaliye gönderimi** (GİB'e iletim) | **HARİÇ** | Kod tabanında entegratör istemcisi yok: `grep -ril "entegrator"` → **0 dosya**; `efatura` geçen tek dosya `Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs` ve orada yalnız **iç fatura bağı** DTO'su var. Fatura modülü **kendi içinde** fatura üretir (`Modules/Fatura/FaturaModulu.cs:18` — `FTR-{yıl}-{sıra}`), GİB'e **iletmez**. | **K-2** — entegratör sözleşmesi + mali mühür + kontör. Para ve imza işidir; ekip uygulayamaz. |
| H-2 | **ÖKC / yazarkasa / Z raporu** | **HARİÇ** | `grep -ril "okc"` → tek eşleşme `HurdaModulu.cs:525`'teki `HurdaStokController` adının içinde geçen harfler; **ÖKC entegrasyonu yok**. | **K-11** — cihaz kararı + TSM/ÖKC firma sözleşmesi. |
| H-3 | **SMS / WhatsApp gönderimi** | **HARİÇ** | `Modules/Isler/IBildirimKanali.cs:8` birebir: *"E-posta / SMS / WhatsApp **GÖNDERİMİ** bu üründe **YOKTUR**"*. Bildirim **ekranda** kalır (`Pages/Bildirimler.razor`). | **K-15** — operatör/BSP sözleşmesi (para işi). |
| H-4 | **Banka canlı API'si (açık bankacılık)** | **HARİÇ** | Banka modülünde canlı banka istemcisi yok; banka hareketi **ekstre içe aktarımı** ve elle giriş yoluyla girer (`Pages/BankaEkstre.razor`, `Modules/Banka/BankaModulu.cs`). | **K-22** — banka kurumsal API sözleşmesi. |
| H-5 | **İmalat / atölye (Katman 1)** | **KAPSAM DIŞI** | Ürün perakende işletme içindir; imalat emri, fire/zayiat, döküm-cila iş akışı **yoktur**. | Kapsam kararı: müşteri perakende kuyumcudur. |
| H-6 | **RFID sayım donanımı** | **HARİÇ** | Sayım ekranı/oturumu var (`Pages/Sayim.razor`, `Modules/Sayim/SayimModulu.cs`) ama **RFID okuyucu entegrasyonu yok**; donanımsız kanıtlanamaz. | **K-21** — donanım kararı. |
| H-7 | **Emanet parça kimliği (kalıcı emanet envanteri)** | **1.1** | Emanet ekranı ve uçları var (`Modules/Emanet/EmanetModulu.cs`); parça bazlı kalıcı kimlik yok. | **K-12** — hukuki görüş gelmeden açılmaz. |
| H-8 | **Seri no / bandrol (KMTS)** | **1.1** | KMTS'in birincil metni **rafineri** odaklıdır; perakende kuyumcu açıkça kapsamda sayılmıyor (`PAZAR-ARASTIRMASI-2026-09-16.md`). Ürün bandrol takibi **iddia etmez**. | **K-14** — mevzuat teyidi. |

> **Not (H-1):** "e-Fatura yok" demek **fatura yok** demek değildir. Ürün fatura
> keser, KDV'yi (standart ve **kuyum matrahı**) hesaplar, iptal/düzeltme yolunu
> tutar; eksik olan yalnız **GİB'e elektronik iletim** ayağıdır.

---

## 2. GÖÇ KARARI (Köprü K-6 / BK-31 — sessiz bırakılamaz)

**Karar: 1.0'da göç KISMEN vardır. Sınırı burada yazılıdır.**

| Ne | 1.0'da | Kanıt |
|---|---|---|
| **Cari kartları CSV ile içe aktarım** | **VAR** | `Modules/Iceaktarim/Kaynaklar.cs:102` `CariKaynagi` · uçlar `api/v1/ice-aktarim/{sablonlar,prova,uygula,geri-al}` (`IceAktarimModulu.cs:672+`, `[RolGerekli("SAHIP")]`) · **kuru prova** + **geri alma** (pasife alma) var |
| **Ürün kartları CSV ile içe aktarım** | **VAR** | `Kaynaklar.cs:228` `UrunKaynagi` · aynı uçlar |
| **Açılış bakiyesi — cari / kasa / banka** | **VAR** (sihirbaz) | `Modules/Kurulum/AcilisServisi.cs:201-203` (`AcilisTuru.Cari/Kasa/Banka`) · uçlar `api/v1/kurulum/acilis/{prova,uygula,geri-al}` |
| **Açılış STOKU (devir malı)** | **HARİÇ — 1.0'da yok** | `Modules/Kurulum/AcilisKaynaklari.cs:21` ve `IceAktarimModulu.cs:596`: *"Açılış bakiyesi ve açılış stoku **Faz B** kapsamındadır"*; `AcilisServisi`'nin uygulama dalında `AcilisTuru.Stok` **yok** |
| **Açılış satırlarının CSV ile toplu yüklenmesi** | **HARİÇ — Faz B** | `AcilisKaynaklari.cs:21` — Faz A'da satırlar **sihirbaz formundan** girilir |
| **Rakip programın kendi dosya biçiminden doğrudan göç** | **HARİÇ** | Rakip deneme hesabı yok (**K-17**); doğrulanmamış biçim için dönüştürücü yazılmaz (md.1) |

**Bir cümleyle söylenecek hâli:** *"Cari ve ürün kartlarınızı CSV ile toplu
alabiliyoruz; kasa/banka/cari açılış bakiyelerini kurulum sihirbazından
giriyoruz. **Devir stoğunuz elle girilir** — toplu stok aktarımı bu sürümde
yoktur."*

---

## 3. Bu listenin dışında kalan kurucu kalemleri (işi durdurmaz, sınırı çizer)

| Kalem | Ne bekliyor | Gelmezse ürüne etkisi |
|---|---|---|
| **K-6** | Bulut hesabı + ödeme yöntemi | Kurulum tatbikatı "YEREL KAP TATBİKATI" başlığında kalır; "üretim kanıtı" denmez |
| **K-16** | Rakamlı fiyat **ilanı** | Ürün teknik olarak hazır olur, fiyat ilan edilemez; tüm materyal "TASLAK" damgasında kalır |
| **K-19** | Pilot davetinin **gönderimi** | Pilot ölçümü başlamaz |

---

## 4. Değişiklik kuralı

Bu listeye kalem **eklenirse** ya da bir kalem **HARİÇ'ten çıkarsa**: değişiklik
yalnız bu dosyada yapılır, sebebi ve kanıtı (dosya:satır ya da karar kaydı) aynı
satıra yazılır. Bu dosyayı gömerek kullanan belgeler otomatik olarak güncel kalır;
**ikinci bir elle yazılmış kopya açılmaz** (`HARIC_BEYAN_KOPYA`).
