# RUNBOOK — DEMO KİRACISI VE 15 DAKİKALIK GÖSTERİM

> **TASLAK — ilan edilmemiştir.** Bu belge bir satış taahhüdü değildir; rakamlı
> fiyat ilanı kurucu imzasına bağlıdır (K-16). Bu runbook **ücretsiz pilot /
> gösterim** içindir; gösterimde **hiçbir kanalda rakam telaffuz edilmez**
> (BK-49).
>
> **HARIC-KAYNAK: docs/HARIC-LISTESI.md** — 1.0'da bulunmayan yetenekler
> burada İKİNCİ KEZ yazılmaz; tek kaynak o dosyadır ve **demo kiracısının
> giriş ekranında** okunur (BK-47).

Paket **P-30** (kimlik **R-4**) · Sürüm 1.0

---

## 0. Kırmızı çizgiler (okumadan başlama)

| # | Kural | Niçin |
|---|---|---|
| 1 | Demo `QA-TEST-*` fikstüründen **türetilmez** | QA fikstürleri kalıcıdır; demo onların verisini göstermez, sıfırdan kurulur (P-30 DoD) |
| 2 | Demo kiracısının adı **`DEMO-`** ile başlar | `betikler/kiraci-temizle.sh` yalnız `PAKET*/TEST*/QA*` adlarını siler; `DEMO-` adı **silme ad kuralına eklenmemiştir**, yani toplu temizlik demoyu süpüremez |
| 3 | Tohumlama **yalnız ürünün kendi uçlarından** yapılır | Her defter satırı `HareketFabrikasi.Ekle` kapısından geçer; doğrudan `INSERT` yasaktır |
| 4 | Adlar **kurgusaldır** | KVKK algısı: gerçek müşteri adı/telefonu demoya girmez |
| 5 | Gösterimde **rakam telaffuz edilmez** | R-8 / BK-49 |
| 6 | **K-1 kapanmadan** gerçek demo kiracısı **açılmaz** | BK-16: kirli taban üstüne demo kurulmaz |

---

## 1. Demo kiracısını tohumlama

```zsh
cd <depo kökü>
export KUYUMCU_SISTEM_ANAHTARI=<sistem anahtarı>       # ekrana BASILMAZ
node dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js \
     --api http://localhost:5198 --ad "DEMO-VITRIN Kuyumculuk"
```

Betik sırayla (PAKET P-D3 — `DEMO-SENARYOSU-2026-09-26.md` § 3, V1–V17):
planı § 3'e karşı sınar (tutmazsa HİÇ yazmaz) → davet üretir → kiracı + SAHİP
kullanıcı açar → giriş yapar → kur kaydını bekler → kasa bulur/açar ve
**150.000 TL açılış devri** girer (kasa ASLA eksi değil) → 5 kurgusal müşteri +
1 toptancı → 12 ürün (ziynet · takı · 14 ayar taşlı · gümüş · gram altın;
EAN-13 barkod; **açılış stoğu** ürün kartından) → varsayılan perakende fiyat
listesi + gümüş etiket fiyatı → toptancıdan küçük vadeli alış → 9 satış (nakit ·
kart · 2 veresiye) → kısmi tahsilat → 1 emanet kaydı → vitrinde 8 kalem →
dükkân görünümü → **sunucudan geri okuyup V maddelerini ölçer**; bir madde
tutmazsa `BAŞARISIZ` ile çıkar ve ölçümü `ham/demo-tohum-KARSILANMADI-*.json`'a
yazar. Başarılı çıktı `ham/demo-tohum-<damga>.json` (alan `senaryo.maddeler`).
Ağ çağrısız plan denetimi: `node …/demo-tohumla.js --plan-dogrula`.
Kurucunun renk seçimi: `--sema <platin|zumrut|…> --olcek <16|18|20>`.

**Tohumlanan geçmişin takvimi:** `TEK_GUN`. Satış/alış/tahsilat uçları
**işlem tarihi almaz**; hareket tarihi sunucudaki o anki saattir. Bu yüzden bu
sürüm **3 aylık geçmiş üretmez** ve ürettiğini de öyle göstermez (Anayasa
md.1). 3 aylık tutarlı geçmiş, uçlara birinci sınıf "işlem tarihi" alanı
eklenmesini gerektirir — bkz. `PAKET-P-30-uygulama-raporu.md` § devredilenler.
**Tarihi sonradan SQL ile kaydırmak yasaktır:** denetim damga zinciri ve
mutabakat kayıtları o an yalan söylemeye başlar.

---

## 2. 15 dakikalık gösterim — adım adım

| # | Dk | Ekran (rota) | Ne gösterilir | Arkasındaki uç |
|---|---|---|---|---|
| 1 | 1 | `/giris` | Giriş ekranı **ve bu kurulumda HARİÇ olanlar listesi** (BK-47) | `GET /genel/kisitlar` (anonim) |
| 2 | 2 | `/` | Karşılama paneli: günün özeti | `GET /pano/guncel` |
| 3 | 1 | `/kur` | Canlı kur ve kaynağı | `GET /kur/guncel` |
| 4 | 2 | `/urunler` | Stok: ayar, gram, has karşılığı | `GET /urunler` |
| 5 | 2 | `/cariler` | Cari kart: TL **ve** has gram bakiye | `GET /cari` |
| 6 | 2 | `/islemler` | Satış geçmişi ve iade adayları | `GET /islemler/iade-adaylari` |
| 7 | 1 | `/kasalar` | Kasa durumu | `GET /kasalar` |
| 8 | 2 | `/raporlar/vade-takibi` | Vadesi gelen veresiye | `GET /raporlar/vade-takibi` |
| 9 | 1 | `/raporlar` | Günlük özet | `GET /raporlar/gunluk` |
| 10 | 1 | `/denetim-izi` | "Kim, ne zaman, neyi değiştirdi" | `GET /denetim?limit=20` |

**Bütçe:** 15 dk. **Kapı:** paket, prova **20 dakikayı aşarsa kapanmaz**
(Eray koşulu).

### Gösterimde söylenmeyecekler
- Fiyat, indirim oranı, "şu kadar tasarruf" cümlesi (R-8 · BK-49)
- Rakip adı üzerinden karşılaştırmalı iddia
- Hariç listesindeki bir yeteneğin "var" gibi anlatılması

---

## 3. Prova (iki tur zorunlu)

```zsh
node dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js \
     --prova --eposta <demo kullanıcısının e-postası> \
     --kosan "<provayı koşan ajanın/kişinin kimliği>"
```

`--kosan` **zorunludur** ve boşsa betik hiç başlamaz. Sebebi BK-45: *"iki AYRI
ajan … tek koşum kanıt sayılmaz."* Koşanın kimliği kayda geçmezse bu koşul
denetlenemez — 2026-09-23'te tam olarak bu oldu: aynı ajanın 11 saniye arayla
koştuğu iki dosya sayacı yeşil verdi. Artık `denetim.sh` en yeni iki provanın
`kosan` alanının **dolu ve birbirinden FARKLI** olmasını şart koşar.

**İkinci turu koşacak ajan için tam komut dizisi** (kiracı silindiyse önce
tohumlama gerekir; e-posta tohum raporunun `kiraci.eposta` alanındadır):

```zsh
# 1) API (5199'a DOKUNMA)
cd dotnet && ASPNETCORE_ENVIRONMENT=Development Sistem__Anahtari="<anahtar>" \
  dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198 &
# 2) prova kiracısı (silinebilir) — e-postayı rapordan al
KUYUMCU_SISTEM_ANAHTARI="<anahtar>" node dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js \
  --prova-kiraci --ad "PAKET-P-30-TEST-B Kuyumculuk" --rapor /tmp/tohum-B.json
# 3) prova turu — KENDİ kimliğinle
node dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js \
  --prova --eposta <tohum raporundaki e-posta> --kosan "<kendi ajan kimliğin>"
# 4) kiracıyı sil, denetimi koş
```

Her koşum `ham/demo-prova-<damga>.json` yazar: adım adım HTTP durumu, süre,
`kritik` bayrağı. **Kapı kuralı (BK-46 · sayaç `DEMO_ROTA_KRITIK`):** en yeni
**iki** prova dosyasının **ikisinde de** her adımın `kritik` değeri **0**
olacak. `dotnet/denetim.sh` bunu her koşumda ölçer.

### 3.1 Ekran turu (tarayıcı) — API provasından AYRI ve zorunlu

Prova betiği **API uçlarını** ölçer; gösterimde müşterinin gördüğü şey
**ekranlardır**. 2026-09-23'te bu fark gerçek bir kusuru sakladı: runbook'un
8. adımında yazılı `/vade-takibi` rotası uygulamada **yoktu** ve tarayıcıda
"Sayfa bulunamadı" çiziyordu; API ucu (`GET /raporlar/vade-takibi`) ise
çalışıyordu, bu yüzden prova yeşildi. Doğru rota `/raporlar/vade-takibi`.

Bu yüzden her gösterim öncesi **tarayıcıdan** da bir tur atılır: giriş →
tablodaki 9 ekran → her ekranda içerik gerçekten çiziliyor mu, hata bandı var
mı. Ölçüm `ham/demo-ekran-turu-<damga>.json`'a yazılır. Kapı: `denetim.sh`
`DEMO_ROTA_KRITIK` artık tablodaki **her rotanın** `Kuyumcu.Web/Pages`
altında bir `@page` karşılığı olmasını da şart koşar; olmayan rota kırmızıdır.

> ⚠ **Provanın sınırı:** betik gösterimin **veri yolunu** ölçer, insanın
> ekranda geçireceği süreyi değil. Ölçülen süre gösterimin **alt sınırıdır**;
> tablodaki insan dakikaları ayrıca sayılır. **İki ayrı kişi/ajan tarafından
> koşulması** koşulu betikle karşılanmaz — ikinci koşumu **başka biri** yapar.

---

## 4. Demo kiracısını silme

Demo **kendiliğinden silinmez**. Silinmesi gerekiyorsa bu bir **kurucu
kararıdır**; `kiraci-temizle.sh` `DEMO-` adını bilerek reddeder:

```zsh
zsh dokumanlar/09-uygulama-kayitlari/betikler/kiraci-temizle.sh <tenant_id>
# → REDDEDİLDİ: '<ad>' DEMO kiracısıdır (P-30) ...
```

Prova/test kiracıları (`PAKET-*`, `TEST-*`) aynı betikle `--uygula` ile silinir.
`QA-TEST-*` fikstürleri **hiçbir koşulda** silinmez.
