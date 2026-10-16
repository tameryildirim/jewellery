# PAKET S3-4: Raporlar ekranına basit mod için "Bugün" sayfası (uygulama raporu)

**Tarih:** 2026-09-23/24 · **Dalga:** S3B, günlük ekranlar B · **Rol:** Taşkın (backend) + Ertuğ (frontend), Opus
**Spesifikasyon:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-3 "Raporlar (basit)". Hedef: basit modda "Bugün" tek sayfa olsun (sattım, aldım, kasa, has pozisyonu, bekleyen). Rapor merkezi kartları profesyonel modda kalsın. Gün sonu mutabakatında fark varsa düzeltme yoluna bağlantı verilsin.
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı. Tam süitteki 20 kırmızı test başka şeritlere ait; ayrıntısı § 9'da.

---

## 1. Yapılanlar

| # | Ne | Nerede |
|---|---|---|
| 1 | **İki yüz, tek gövde.** Basit modda `@BugunYuzu` çizilir. Profesyonel modda eski içerik **olduğu gibi** yeni `<div class="dikey-bosluk" id="raporlar-pro">` sarmalayıcısının içinde çizilir. Eski DOM id'lerinin hiçbiri değişmedi. Silinen id sayısı 0. | `Pages/Raporlar.razor` |
| 2 | **Bugün ne sattım?** Tutar, adet ve has gösterilir. Sayı `/raporlar/islem-defteri` sayfasına bağlanır (UX-S23). | aynı |
| 3 | **Bugün ne aldım?** Alış tutarı, adet ve has gösterilir. **Hurda ayrı satırda** durur (TL ve has, `/hurda` bağlantısıyla) ve alış toplamına **eklenmez**. | aynı |
| 4 | **Altın pozisyonum:** B-2 kartı `<AltinPozisyonuKarti />` tek satırla yerleştirildi. Kart verisini kendisi çeker. | aynı |
| 5 | **Kasada ne var?** Kasalar para birimine göre gruplanır ve farklı para birimleri **toplanmaz** (UX-S32). Ham para kodu ekrana basılmaz (UX-S31). "Şu anki para" etiketi değerin dönemden bağımsız olduğunu söyler (UX-S33). Tutar `/kasalar` sayfasına bağlanır. | aynı |
| 6 | **Bekleyen iş:** en çok 5 açık bildirim gösterilir. Önem düzeyi sözlükten okunur, ham `KRITIK` kodu basılmaz. Yalnız iç rotalar bağlantı olur (`//…` bağlantı olmaz). Liste sınıra değerse "ilk 5 gösterildi" uyarısı çıkar (UX-S29). "Tümü" bağlantısı `/bildirimler` sayfasına gider. | aynı |
| 7 | **Gün sonu:** gün mutabıksa "Tamam, fark yok." rozeti çıkar. **Fark varsa** kırmızı "Fark var" rozeti, fark tutarı ve sunucunun `farkTanimi` metni gösterilir (metin gelmezse sözlükteki sade açıklama yazar). Düzeltme bağlantıları: "Faturasız satış: N" → **Fatura kes** (`/faturalar`), "Satışa bağlanmamış fatura: N" → **Faturalara bak**. İkisi de yoksa **Faturaları kontrol et** bağlantısı çıkar. Sunucunun uyarıları basit modda da gizlenmez. | aynı |
| 8 | **Özellik silinmedi.** Basit yüzün altındaki not diğer raporların profesyonel görünümde olduğunu söyler ve **Görünümü değiştir →** (`/ayarlar`) bağlantısını verir. `DegismezIpucu Rapor` iki yüzde de durur (UX-S25). | aynı |
| 9 | **Yüze göre yükleme.** Basit yüz yalnız `/raporlar/gunluk`, `/raporlar/pos-fatura-mutabakati`, `/bildirimler` ve `/pozisyon` uçlarını çağırır. Profesyonel yüzün yükleme sırası paketten önceki gövdeyle birebir aynıdır. Mod çalışırken değişirse eksik yüzün verisi o an, bir kez yüklenir (`Mod.Degisti`). | aynı |
| 10 | **UX-S21:** tarih **istemciden gönderilmez**. Sunucu işletme gününü kendisi çözer (`Zaman.YerelBugun`, Europe/Istanbul). Panel `DateTime.Today` gönderiyor; bu yüz göndermiyor. | `Raporlar.razor` + `UyumServisi.BugunMutabakati()` |
| 11 | **UX-S28:** hata "0 ₺" ya da "kayıt yok" diye gösterilmez. Hata bandı ayrı durur. 403 alınırsa "Bu bilgiyi yalnız yönetici görür." notu çıkar (**yetki ≠ mod**). | aynı |
| 12 | TR ve EN metinler **ayrı dosyada**, konumsal record olarak durur. Bir dilde anahtar eksikse derleme kırılır. Metinlerde jargon yok, etiketler en çok 3 kelime. | `Kuyumcu.Core/I18n/Sozlukler.RaporBugun.cs` (YENİ) |
| 13 | `UyumServisi.BugunMutabakati()` eklendi. Tarihsiz bir GET yapar ve **eklemeli** bir değişikliktir; mevcut `Mutabakat(DateTime)` değişmedi. Gerekçe § 2-3'te. | `Servisler/UyumServisi.cs` |
| 14 | Mevcut iki bUnit sınıfına `Services.AddScoped<ArayuzModuServisi>()` kaydı eklendi, çünkü sayfa artık modu okuyor. Varsayılan mod PROFESYONEL olduğundan eski testler aynı DOM'u görür. | `RaporlarEkranTestleri.cs`, `RaporEkranTestleri.cs` |
| 15 | 20 yeni bUnit testi yazıldı. | `Kuyumcu.Tests/S34RaporBugunTestleri.cs` (YENİ) |

**Yeni CSS yazılmadı.** S3-1'in belirteç kullanan `.panel-sade`, `.panel-soru` ve `.panel-liste` sınıfları yeniden kullanıldı. Satır içi `style` sayısı değişmedi (paket öncesi 14, sonrası 14; eklenen 0). Sabit renk yok. Birincil düğme yok (sayfa bir rapor).

## 2. Sapmalar ve gerekçeleri

1. **Sunucuya dokunulmadı, migration yok.** Bugün sayfasının ihtiyaç duyduğu bütün veriyi mevcut uçlar zaten döndürüyor. Sunucu modülleri S-B şeridinin alanı.
2. **Tuval:** Raporlar için ayrı bir taslak yok. Görsel dil `Panel.dc.html`'in soru kartlarından alındı; aynı sınıflar kullanıldı. Kart dizilişi şöyle: üst satırda Sattım, Aldım ve Altın pozisyonum (taslaktaki 3 büyük sayı düzeni). Alt satırda Kasa ve Bekleyen iş. En altta Gün sonu. Taslakta Kasa üst satırdaydı, burada "Aldım" oraya kondu. Gerekçe: bu sayfanın sorusu "bugün ne oldu"; Kasa ise kasa listesiyle birlikte alt satırda daha çok bilgi taşıyor. Tasarım denetçisi farklı karar verirse değişiklik yalnız sıra değişikliğidir.
3. **`UyumServisi` bu şeridin dosya listesinde yok.** İlk denemede `Api.GetAsync<Mutabakat>` doğrudan sayfaya yazılmıştı. Bu, `SozlesmeKapsamTestleri` cırcırını kırdı: sayfada kilitsiz yeni istemci tipi oluştu (`Mutabakat`, 59→60). Doğru çözüm, tipin zaten kullanıldığı servis üzerinden çağırmak. Bu yüzden tek bir eklemeli metot yazıldı; başka hiçbir satır değişmedi. Düzeltmeden sonra bu paketin sayfasında kilitsiz yeni tip yok (§ 5).
4. **"FARK VAR" düzeltme yolu:** sunucunun FARK tanımı "kasaya giren para − satış faturaları". Doğru düzeltme, faturası kesilmemiş satışa fatura kesmek. `/faturalar` ekranındaki "Faturalanacak işlem" seçicisi `/faturalar/faturalanabilir-islemler` ucundan besleniyor; canlıda bu satışların orada listelendiği doğrulandı (§ 6, adım 12). Derin bağlantı (`?islemId=`) yazılmadı, çünkü Faturalar.razor bu şeridin alanı değil.
5. **Mevzuat:** bu paket mevzuat hükmü **yazmadı** ve eşik sayısı gömmedi. Fark hesabı ve uyarıları sunucudan olduğu gibi gelir.

## 3. Migration

Gerekmedi. Şema değişikliği yok.

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Taban: tam süit, paket öncesi, 23.09 23:49 | 4541 | 4541 | 0 | kırmızı listesi BOŞ |
| S34RaporBugunTestleri | 20 | 20 | 0 | yeni |
| Rapor ekranları: RaporlarEkran + RaporEkran + UX1C3Ekran | 53 | 53 | 0 | mevcut testler korundu |
| Paket filtresi: S34 + Rapor* + UX1C3 + SozlesmeKapsam (son tam süitten) | 75 | 75 | 0 | |
| **Tam süit, son koşum, 24.09 00:18** (`trx-pS3-4/_Mac_2026-09-24_00_18_52.trx`) | **4612** | **4592** | **20** | 20 kırmızının hepsi başka şeritlere ait (§ 9) |

Yeni testler şunları sınıyor: iki yüz (S34_01, S34_02); yüze göre yükleme ve tarihsiz istek (S34_03, S34_04); mod değişince yükleme (S34_05); satış kartı (S34_06); alış ve hurda ayrımı (S34_07, S34_08); kasa para birimi ayrımı (S34_09); gün sonu fark/tamam/genel yol (S34_10, S34_11, S34_12); hata ve 403 davranışı (S34_13, S34_14, S34_15); bekleyen iş rota/sınır/boş/hata durumları (S34_16, S34_17, S34_18); birincil düğme yok ve her düğme gerçek istek atar (S34_19); TR/EN sözlük eksiksiz ve İngilizce yüz (S34_20).

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/S3-4/` altına kopyalandı. `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı, derlendi, filtreli test koşuldu (`S34RaporBugunTestleri|RaporlarEkranTestleri`). Sonra yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu doğrulandı.

**Geçersiz sayılan koşum:** M34-5'in ilk koşumunda başka bir şeridin eşzamanlı derlemesi `obj/Kuyumcu.Web.dll` dosyasını kilitledi (`MSB4018 … being used by another process`). Test ikilisi bayat kaldı. O koşum sayılmadı. Betik "0 Hata görülene kadar 3 deneme" kuralıyla düzeltildi ve M34-5 yeniden koşuldu.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M34-1 | `@if (Mod.BasitMi)` → `@if (false)` (basit yüz hiç çizilmez) | 18 test: S34_01, 03, 05–20 | cmp BİREBİR |
| M34-2 | Alış tutarına hurda eklendi | S34_07 | cmp BİREBİR |
| M34-3 | Kasa gruplaması `GroupBy(k => "TRY")` (para birimleri toplanır) | S34_09 | cmp BİREBİR |
| M34-4 | `@if (m.Mutabik)` → `@if (!m.Mutabik)` | S34_10, S34_11, S34_12 | cmp BİREBİR |
| M34-5 | Özet hatası yutuldu (`bugunOzetHata = null`) | S34_13 | cmp BİREBİR |
| M34-6 | Günlük özete istemci tarihi eklendi (`?tarih=`) | S34_03 | cmp BİREBİR |
| M34-7 | İç rota denetimi kaldırıldı (`BugunIcRota => rota`) | S34_16 | cmp BİREBİR |
| M34-8 | `Mod.Degisti += ModDegisti` silindi | S34_05 | cmp BİREBİR |
| M34-9 | Bildirim sınır uyarısı `@if (false)` | S34_17 | cmp BİREBİR |
| M34-10 | Gün sonu 403 dalı silindi (403 hata bandına düşer) | S34_14 (UyumServisi değişikliğinden sonra yeniden koşuldu, yine kırmızı) | cmp BİREBİR |
| M34-11 | Hurda satırı koşulu `@if (true)` | S34_08 | cmp BİREBİR |
| M34-12 | Yüz seçimi `if (true)` (profesyonel yüz de Bugün yükler) | S34_04 + RaporlarEkranTestleri P47_01–P47_04 | cmp BİREBİR |
| M34-13 | `UyumServisi.BugunMutabakati` tarih gönderir | S34_03 | cmp BİREBİR (`UyumServisi.cs`) |

13 mutasyonun 13'ü kırmızıya döndü; test açığı yok. Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln` **0 uyarı, 0 hata**.

## 6. Canlı doğrulama

Ortam: port 5190, veritabanı `kuyumcu_net`. API şu komutla başlatıldı: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5190` (başlatan PID 58887, dinleyen PID 58926).

Kiracı, tek kullanımlık bir davetle açıldı. Davet `kiraci_davetleri` tablosuna SHA-256 özetiyle eklendi (`PAKETS3-4-DAVET-01`); jeton ve parola ekrana basılmadı. Kiracı: **PAKETS3-4-TEST** (`01M381REKDB6DZW0SAFH41AVSK`). Betik: `scratchpad/pS3-4/canli.py`, çıktı: `canli.json`.

| # | Adım | Beklenen | Gerçek | Özet |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | kiracı açıldı |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | POST /cari ×2 | 201/201 | 201/201 | müşteri ve toptancı açıldı |
| 4 | POST /urunler K22 10 g | 201 | 201 | has 9,16 |
| 4b | POST /kasalar TRY + USD | 201/201 | 200/200 | uç 200 dönüyor; iki kasa da açıldı |
| 5 / 5b | GET /kasalar; kasalara GIRIS 50.000 TL + 500 USD | 200; 200/200 | 200; 200/200 | açılış bakiyesi girildi |
| 6 | POST /islemler/alis 3 adet, 3.000 TL NAKIT | 200 | 200 | stok girdi |
| 7 | POST /islemler/satis NAKIT 5.000 | 200 | 200 | |
| 8 | POST /islemler/satis VERESIYE 3.000 | 200 | 200 | |
| **10** | **GET /raporlar/gunluk (tarihsiz)** | 200; satış 2 adet 8.000; alış 1 adet 3.000; TRY 52.000; USD 500 ayrı | **200** | `tarih=2026-09-24` (sunucunun işletme günü) · satış=(2, 8000.0, 18.32) · alış=(1, 3000.0, 27.48) · hurda=(0,0) · kasalar=[S34 Dolar USD 500.0, S34 Nakit TRY 52000.0] |
| **11** | **GET /raporlar/pos-fatura-mutabakati (tarihsiz)** | 200; mutabık değil; fark ≠ 0 | **200** | fark=8000.0 · mutabik=False · faturasız=(2, 8000.0) · bağımsız=0 · farkTanimi var · 2 uyarı → ekranda "Fark var", "Faturasız satış: 2", **Fatura kes** |
| **12** | **GET /faturalar/faturalanabilir-islemler** (düzeltme bağlantısının hedefi) | 200; faturasız satış listede | **200** | 3 işlem listede |
| 13 | GET /bildirimler?durum=ACIK&limit=5 | 200 | 200 | limit=5, 0 kayıt → ekranda "Bekleyen iş yok." |
| 14 | GET /pozisyon | 200 | 200 | net 9,16 · vitrin 9,16 |
| 15 | PATCH /kullanicilar/ben/arayuz-modu BASIT, ardından GET /auth/ben | 200/200 BASIT | 200/200 | etkinArayuzModu=BASIT |
| 16 | GET /raporlar (SPA kabuğu) | 200 html | 200 | html döndü |
| 17–18 | TEZGÂHTAR (BASIT) aç ve giriş yap | 201 / 200 | 201 / 200 | jeton alındı |
| **19–20** | **TEZGÂHTAR: GET /raporlar/gunluk ve /pos-fatura-mutabakati** | 403 | **403 ×2** | "gerekli rol: SAHIP veya MUDUR veya MUHASEBE" → ekranda "Bu bilgiyi yalnız yönetici görür." (**yetki ≠ mod**) |
| 21 | TEZGÂHTAR GET /auth/ben | 200 BASIT | 200 | BASIT |
| 22 | GET /raporlar/gunluk jetonsuz | 401 | 401 | |

**SQL kanıtı** (`scratchpad/pS3-4/canli-sql.txt`):

```
hareketler: ALIS NAKIT 1 −3000.00 27.48 · SATIS NAKIT 1 5000.00 −9.16 · SATIS VERESIYE 1 3000.00 −9.16
kasalar:    S34 Nakit TRY 52000.00 · S34 Dolar USD 500.00     → ekranda 52.000 ₺ büyük sayı; "Dolar kasalar" AYRI satır
faturalar:  0                                                   → gün sonunda FARK VAR ve "Faturasız satış: 2"
kullanicilar: SAHIP BASIT · TEZGAHTAR BASIT
```

Hesap kontrolü: TL kasası 50.000 − 3.000 (alış) + 5.000 (nakit satış) = 52.000. Satış: 2 adet, 8.000 TL. Has: 9,16 × 2 = 18,32. Ekrandaki "Bugün ne sattım?" kartı bunu "8.000,00 ₺ · 2 satış · 18,3 g has" olarak yazar.

Not: canlı koşum `UyumServisi.BugunMutabakati()` eklenmeden önce yapıldı. Sayfa o sırada aynı URL'yi (`/raporlar/pos-fatura-mutabakati`, tarihsiz) doğrudan çağırıyordu. Sunucuya giden istek birebir aynı olduğu için canlı sonuç geçerli.

**Tarayıcıda görsel doğrulama YAPILMADI.** Ekranı görmek için giriş formuna parola girmek gerekir ve bu ajan bunu yapamaz. Ekran davranışı 20 bUnit testiyle (id'ler üzerinden) ve uçların canlı yanıtlarıyla doğrulandı. 375/768/1440 px taşma ölçümü orkestratörün 5199 yayınına ve QA turuna bırakıldı.

**Kapanış:**
- API durduruldu (`kill 58887 58926`). `lsof -nP -iTCP:5190 -sTCP:LISTEN` 0 satır döndü.
- PAKETS3-4-TEST kiracısı, `tenant_id` kolonu olan **70 tablonun hepsinden** ve `tenants` tablosundan silindi. Kalan satır 0. Davet satırı da silindi (0).
- `QA-TEST-*` kiracılarına ve `DEMO-Vitrin` kiracısına dokunulmadı.
- `tenants` sayısı: başlangıçta **13**, sonda **11**. Farkın hiçbiri bu paketten değil. Başlangıçtaki 3 `PAKETS3-1-TEST*` kiracısını başka bir şerit sildi (−3). Koşum sırasında başka bir şerit `PAKETP52-TEST` kiracısını açtı (+1). Bu paketin kiracı etkisi +1 −1 = 0.

## 7. denetim.sh

- Son çıktı: `DENETİM BAŞARISIZ`. Tek ihlal: `✗ Pages: @onclick'siz ve disabled'sız <button> 74 (tavan 71 — ARTIŞ)`.
- **Bu paketten değil:** Raporlar.razor'daki sayı paket öncesi 1, sonrası 1. Bu paket yeni `<button>` eklemedi; eylemler bağlantıdır. Artışın olduğu sırada en son değişen sayfalar Cariler.razor (00:18, S3-A şeridi, 15 satır) ve SiparisDetay.razor (00:10).
- Aynı ağaçta birkaç dakika önceki koşum (`denetim.log`) **DENETİM TEMİZ** idi.
- Bu paketin ilgili sayaçları ✓: HAM_SELECT ve HAM_INPUT artmadı (yeni `<select>`/`<input>` yok); inline style artmadı; ModalPencere kullanımı artmadı; sayfaya yeni record yazılmadı.

## 8. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Açıklama |
|---|---|---|
| — (yeni varlık YOK) | — | Bu paket veri modeli, PATCH ya da DELETE ucu açmadı. |
| Bugün kartlarındaki sayılar | salt okunur | Türetilmiş değerler. `DegismezIpucu Rapor` gerekçeyi ve düzeltme yolunu yazar (UX-S25). Her sayı kendi kayıtlarının ekranına bağlanır: işlem defteri, kasalar, hurda, bildirimler, faturalar. |
| Gün sonu farkı | salt okunur → düzeltme **Faturalar** ekranında | "Fatura kes" / "Faturalara bak" / "Faturaları kontrol et" bağlantıları. Yıkıcı eylem yok, storno yok. |

Düğmeler: basit yüzde `<button>` yalnız B-2 kartında var ve her biri GET atar (S34_19 bunu tıklayarak sınar). Diğer bütün eylemler gerçek rotaya giden bağlantıdır.

**Dokunulan sayfalar (DoD EK 2):** `Pages/Raporlar.razor`.

## 9. Açık kalanlar (sahibiyle)

1. **Tam süitte 20 kırmızı test var, hiçbiri bu paketin değil.** Taban kırmızı listesi boştu. Bu paketin testlerinin (S34, Rapor*, UX1C3, SozlesmeKapsam; 75 test) tamamı yeşil. Kırmızılar:
   - **S-2 şeridi** (`Pages/Islemler.razor` 00:15 ve `Bilesenler/Fis/*` 00:10–00:11 saatlerinde değişti): B4AltinBacagiFisEkranTestleri ×3, DovizBacagiEkranTestleri ×1, KarmaOdemeEkranTestleri ×3, P10PosBacagiEkranTestleri ×2, S21FisYapiTestleri ×2, S22TezgahFisiTestleri ×5, UX4bEkranTestleri ×1.
   - **S3-A şeridi** (`Pages/Cariler.razor` 00:18'de değişti): P10CariNotEkranTestleri ×2, S32MusterilerEkranTestleri ×1.
   - Ara koşumda görülen `SozlesmeKapsamTestleri`'nin iki kırmızısının kaynağı ikiydi: bu paketin `Mutabakat` tipi (düzeltildi, § 2-3) ve S-2'nin `AltinBacagiAyarlari` tipi. Son tam süitte ikisi de yeşil.
2. **denetim.sh `OLU_DUGME` 74 > 71:** Raporlar.razor'dan değil (§ 7). Muhtemel sahibi S3-A (Cariler.razor); orkestratörün dalga kapısında ölçülmeli.
3. **Görsel ölçüm** (375/768/1440 taşma, tasarım denetçisi) yapılmadı. Orkestratörün 5199 yayınına ve QA turuna bırakıldı.
4. **Panel tutarlılığı:** Panel'in "Bugün ne sattım?" kartı hâlâ istemcinin `DateTime.Today` tarihini gönderiyor. Bu sayfa göndermiyor ve sunucunun işletme gününü kullanıyor. Gece yarısı sonrası ile 03:00 arası gibi saat dilimi kenarlarında iki ekran farklı günü gösterebilir. Panel.razor S3-A şeridinin dosyası olduğu için dokunulmadı; S3-A'ya öneri.
5. **Faturalar'a derin bağlantı** (`/faturalar?islemId=…`) yok; bağlantı Faturalar ekranının kesme formuna gider. Faturalar.razor'u açan paket bu parametreyi ekleyebilir.

## 10. Kurucuya / mali müşavire not

Bu paket hiçbir mevzuat hükmü yazmadı. Gün sonu farkının tanımı ve uyarıları sunucudaki mevcut `PosFaturaMutabakati` servisinden olduğu gibi gelir. Ekran yalnız bunları sade dille gösterir ve düzeltme ekranına bağlar.

## 11. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısının bu pakete ait satırları:

```
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.RaporBugun.cs          (YENİ)
?? dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs               (ağaçta izlenmiyor; bu pakette yalnız BugunMutabakati() eklendi)
   dotnet/Kuyumcu.Tests/S34RaporBugunTestleri.cs              (YENİ)
   dotnet/Kuyumcu.Tests/RaporlarEkranTestleri.cs              (1 satır: ArayuzModuServisi kaydı)
   dotnet/Kuyumcu.Tests/RaporEkranTestleri.cs                 (1 satır: ArayuzModuServisi kaydı)
   dokumanlar/09-uygulama-kayitlari/PAKET-S3-4-uygulama-raporu.md (bu rapor)
```

Commit yok. Proje ağacında `.mutorj`, `.bak` ya da `.tmp` dosyası bırakılmadı. Mutasyon yedekleri scratchpad'dedir.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Basit modda "Bugün" tek sayfa: sattım / aldım / kasa / has pozisyonu / bekleyen | ✓ | S34_01, S34_06, S34_07, S34_09, S34_16; canlı adım 10, 13, 14 |
| Rapor merkezi kartları profesyonel modda | ✓ | S34_01 (basitte yok), S34_02 (profesyonelde var) |
| Gün sonu "FARK VAR" ise düzeltmeye bağlantı | ✓ | S34_10, S34_11, S34_12; canlı adım 11–12 |
| P-47 has gram kolonu korundu | ✓ | RaporlarEkranTestleri P47_01–04 yeşil; M34-12 bunları kırdı, yani profesyonel yüz gerçekten sınanıyor |
| Özellik silinmez (görünür / katlı / pro) | ✓ | Profesyonel DOM birebir; basit yüzde "Görünümü değiştir" bağlantısı |
| Mevcut DOM id'leri değişmez | ✓ | Silinen `id=` satırı 0 (diff) |
| Yetki ≠ mod | ✓ | S34_14; canlı adım 19–20 (403) |
| Sabit renk / inline style / CDN yok | ✓ | Yeni CSS yok, style 14→14 |
| Sozlukler.cs'e dokunulmadı; ayrı dosya, TR+EN | ✓ | `Sozlukler.RaporBugun.cs`, S34_20 |
| Tek POST yolu / Islemler'e dokunulmadı | ✓ | Bu yüz POST etmez (S34_03) |
| UX-S21 / S23 / S28 / S29 / S31 / S32 / S33 | ✓ | § 1 tablosu, ilgili testler |
| Boş durum + hata bandı | ✓ | S34_08, S34_13, S34_15, S34_18 |
| Paket kapısı (1) derleme 0 hata | ✓ | 0 uyarı, 0 hata |
| (2) kendi testlerim 0 başarısız | ✓ | 20/20 + 53/53 |
| (3) kendi sayaçlarım ✓ | ✓ | § 7 |
| (4) mutasyon | ✓ | 13/13 |
| (5) canlı | ✓ | § 6 |
| (6) test kiracısı silindi | ✓ | 70 tablo, kalan 0 |
| (7) yeni kırmızı üretilmedi | ✓ | 20 kırmızının hepsi S-2 ve S3-A dosyalarına ait (§ 9-1) |
