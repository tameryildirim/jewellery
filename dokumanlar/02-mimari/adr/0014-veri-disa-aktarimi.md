# ADR-0014 — Veri dışa aktarımı: şifreli arşiv, tek kullanımlık bağlantı, ham döküm yok

**Durum:** Kabul edildi · **Tarih:** 2026-09-14 · **Dayanak:** PLAN-2 Adım 4, Mehmet'in VETO-1'i

## Bağlam

Rakip incelemesinde bulunan şikâyetlerin ortak derdi MASAK değil, **verinin programda rehin kalmasıydı** (Şikayetvar: "ömür boyu lisans" sonrası ek ücret, dönem devri ücreti). Bu, rakiplerin en zayıf noktasıdır ve ürünün en güçlü vaadi olabilir: *"verin sende kalır"*.

Ancak dışa aktarım aynı zamanda **bir sızdırma yüzeyidir**. Mehmet'in tespiti: *"Diskteki AES-256-GCM koruması, masaüstüne düşen düz metin CSV karşısında hükümsüzdür."* Veto, altı şart karşılanmadan sevk edilmemesi yönündeydi.

## Karar

Tek `Disaaktarim` modülü, `IDisaAktarilabilir` sözleşmesi, ortak CSV yazıcı. İlk turda **6 kaynak**: cari, ürün, satış (hareket defteri), fatura, kasa hareketi, gider pusulası.

Altı şart şöyle karşılandı:

| # | Şart | Uygulama | Kanıtlayan test |
|---|---|---|---|
| 1 | Yalnız SAHİP + gerekçe zorunlu | Sınıf düzeyinde `[RolGerekli("SAHIP")]`, gerekçe < 20 karakter → 400 | `SART_1_MUDUR_dahi_disa_aktaramaz_yalniz_SAHIP`, `SART_1_Gerekcesiz_aktarim_REDDEDILIR` |
| 2 | Varsayılan maskeli; tam kimlik ayrı onay | `Maskeli = true`; maskesiz için onay metni **birebir** yazılmalı | `SART_2_Maskeli_dosyada_TCKN_duz_metin_GECMEZ`, `SART_2_Tam_kimlik_YANLIS_onay_metniyle_alinamaz` |
| 3 | Denetim izi: kim/ne zaman/kaç satır/kapsam/gerekçe | İki kayıt — talep anında ve indirme sonrası satır sayısıyla | `SART_3_Indirme_denetim_izine_satir_sayisiyla_yazilir` |
| 4 | Oran sınırlı | Saatte 10 istek (talep + indirme ortak kota) | `SART_4_Oran_siniri_asilinca_429` |
| 5 | Dosya şifreli; parola ayrı kanaldan | AES-256 ZIP (WinZip AE-1); parola talep yanıtında bir kez, indirmede geri istenir | `SART_5_Arsiv_PAROLASIZ_acilmaz`, `SART_5_Sifreleme_AES_256_dir` |
| 6 | Kısa ömürlü, tek kullanımlık bağlantı | 10 dakika + `Kullanildi` damgası | `SART_6_Jeton_IKINCI_kez_kullanilamaz`, `SART_6_Suresi_dolan_jeton_calismaz` |

### Parola akışının gerekçesi

Parola **talep yanıtında** (kanal A) bir kez döner; sunucu yalnız SHA-256 özetini saklar. **İndirme** (kanal B) parolayı geri ister. Sonuç:

- Bağlantıyı ele geçiren dosyayı **açamaz**.
- Parolayı gören bağlantı olmadan **indiremez**.
- Sunucu parolayı **hiç saklamaz**.

### Saklanmayanlar

- Üretilen arşiv **diske yazılmaz**; indirme anında yanıt akışına üretilir (`IAsyncEnumerable` + `ZipOutputStream`). 8 yıllık veri belleğe alınmaz; sunucuda düz metin artefakt kalmaz.
- **Ham SQL dökümü verilmez.** Batuhan'ın tespiti: tam yedek mimari değil **operasyon** işidir. Kullanıcı/parola özeti, refresh token ve denetim izi kaynak listesinde **yoktur** — `Kullanici_ve_parola_tablolari_kaynak_listesinde_DEGILDIR` bunu sabitler.

### Biçim

UTF-8 (BOM'lu) CSV, RFC 4180, **nokta ondalık**, **ISO-8601 UTC** tarih. Türkçe virgüllü ondalık bilinçli olarak kullanılmadı: veri başka bir programa taşınacak, Excel'in yerel ayarına bağlanamaz.

**CSV enjeksiyonu** kapatıldı: `=`, `+`, `-`, `@` ile başlayan hücrelerin başına tek tırnak konur. Müşteri adı `=cmd|...` olabilir ve Excel bunu **çalıştırır** — dışa aktardığımız veri bizim yazdığımız veri değildir.

## Gerekçe — reddedilen alternatifler

**Her modüle ayrı export ucu** reddedildi (Bertan): 18 modül × export = 18 kopya, 18 ayrı kiracı filtresi hatası riski.

**Şifresiz CSV** reddedildi: dosya e-postayla, WhatsApp'la, USB ile dolaşır; 8 yıllık müşteri kimlik verisi düz metin olarak.

**Kendi şifreli konteyner biçimimiz** reddedildi: müşteri dosyayı yalnız bizim programımızla açabilseydi, "verin sende kalır" vaadi anlamsız olurdu. AES-256 ZIP standarttır; 7-Zip, WinRAR, Keka, `pyzipper` açar. Canlı doğrulamada bağımsız bir araçla (pyzipper) açıldı — Python'un yerleşik `zipfile`'ı ise `NotImplementedError` verdi; bu, arşivin gerçekten **AES** olduğunun (kırılabilir ZipCrypto olmadığının) ek kanıtıdır.

**Yeni bağımlılık — SharpZipLib 1.4.2 (MIT):** .NET'in `System.IO.Compression`'ı şifreli ZIP üretemez. Lisans MIT (0 copyleft kuralı korundu), `dotnet list package --vulnerable` → 0 açık.

## Sonuçlar

**Olumlu**
- Ölçülen ticari fark: rakiplerin en çok şikâyet alan davranışının tersi, ürün içinde ve kanıtlı.
- Kiracı sınırı **iki katmanlı**: global sorgu filtresi (ADR-0001) + elle `TenantId` şartı (ADR-0010). Mutasyonla ölçüldü: elle şart kaldırılınca test **yeşil kaldı** (global filtre tuttu); global filtre de devre dışı bırakılınca test **kırmızıya döndü**.

**Olumsuz / kabul edilen maliyetler**
- **Senkron yazmaya izin verildi** — yalnız indirme isteğinde. SharpZipLib'in `CloseEntry`/`Finish`'i senkron yazar; Kestrel bunu yasaklar. Alternatifleri (belleğe/geçici dosyaya kurup async kopyalamak) bu adımın tasarım kararına aykırıydı. Uç SAHİP'e kapalı ve saatte 10 istekle sınırlı; bayrak isteğe özeldir, sunucu geneline değil.
- **Fatura satır kalemi yok** (TB-17), dolayısıyla dışa aktarılan fatura dosyası da başlık düzeyindedir.
- **Geri alma (import) yoktur.** Bu bilinçlidir: dışarıdan gelen veriyi kabul etmek, doğrulanmamış kaydı sisteme sokar ve mevzuat kapılarını (MASAK, tevsik, taş kökeni) baypas eder. Veri taşınabilirliği tek yönlüdür.

## Yan bulgu — oran sınırı bölütlemesi düzeltildi

Bu adımda yakalandı: `OranSiniri` kullanıcı bölütlemesini `User.FindFirst("sub")` ile arıyordu. JwtBearer varsayılan `MapInboundClaims = true` ile `sub` talebini `ClaimTypes.NameIdentifier`'a **çevirir**; bu yüzden ifade kimlikli isteklerde bile **her zaman null** dönüyor, "kullanıcı başına 100/dk" yazan genel sınır fiilen **IP başına** çalışıyordu. Tek internet hattı arkasındaki bir dükkânın bütün personeli tek kotayı paylaşıyordu. `AktifKullanici` ile aynı çözümleme sırası kullanılacak şekilde düzeltildi.
