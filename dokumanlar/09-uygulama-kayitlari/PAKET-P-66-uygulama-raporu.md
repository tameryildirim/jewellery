# PAKET P-66: Etiket basım taslağının kalıcılığı (/urunler/etiketler): uygulama raporu

- **Tarih:** 2026-09-24 03:00–03:25
- **Rol:** Taşkın (backend, gerekmedi) + Ertuğ (frontend)
- **Şerit:** SERİT D (izole VT `kuyumcu_test_m`, canlı port 5185)
- **Kaynak:** `TRIYAJ-TUR8-2026-09-23.md` satır 97 (ORTA, 1,5 saat tahmin) + orkestratörün ek koşulları
- **Durum:** **BİTTİ.** Paket kapısının 7 maddesi sağlandı (§ 10). Tam süitteki tek kırmızı bu paketin değil, SERİT C'de açık olan P-68'e ait (§ 6).

---

## 1. Spesifikasyonun koda karşı doğrulaması

| İddia (spesifikasyon) | Ölçüm | Sonuç |
|---|---|---|
| Sayfadaki ağ çağrılarının hepsi GET: `/urunler` + `/urunler/madenler` | `UrunEtiketleri.razor:216` ve `:220` (spesifikasyonda :198/:202, satırlar kaymış) | **DOĞRU** |
| POST/PATCH ya da kalıcılık çağrısı yok | `grep localStorage\|PostAsync\|PatchAsync` sayfada 0 | **DOĞRU**: seçim `HashSet<string> secili` alanında yalnız bellekte duruyordu |
| `Kuyumcu.Tests/EtiketEkranTestleri.cs` | `test -e` → **YOK** | Dosya bu pakette **YENİ** yazıldı. Mevcut etiket testleri `S33UrunlerEtiketlerEkranTestleri.cs` (S3-3) ve `EkranTestleri.cs` (`UrunEtiketEkranTestleri` sınıfı) içinde; onlara dokunulmadı |
| Oturumda kiracı kimliği | `OturumKullanici` kaydında kiracı alanı **yok**; `TokenServisi.cs:70-71` jetona `sub` ve `tenantId` talebi koyuyor | Anahtar oturum jetonunun yükünden okunuyor (sapma 1) |

## 2. Yapılanlar

### 2.1 Ekran: `Kuyumcu.Web/Pages/UrunEtiketleri.razor`
- **Taslak içeriği:** seçili ürün kimlikleri, kopya adedi ve arama metni.
- **Nerede saklanır:** tarayıcı deposunda (`localStorage`). Anahtar biçimi `kuyumcu-etiket-taslak:{tenantId}:{sub}`. Değer biçimi `{"v":1,"secili":[…],"kopya":n,"arama":"…"}`.
- **Ne zaman yazılır:** her seçimde, tümünü seçte, seçimi temizlede, arama ve adet değişiminde (`TaslakKaydet`). Form boşsa (seçim yok, arama boş, kopya 1) yazılmaz, anahtar silinir.
- **Geri yükleme (`TaslakGeriYukle`):** ürün listesi **başarıyla** geldikten sonra çalışır (`listeGeldi` bayrağı). Liste gelmezse taslak geri yüklenmez ve budanmaz; aksi halde ağ hatasında her kalem "listede yok" sayılıp taslak silinirdi.
- **S3-3 kuralı korunuyor:** seçim her zaman görünen listenin alt kümesidir. Geri yüklenen kimlikler, kayıtlı aramayla süzülmüş listeyle kesiştirilir.
- **Eksik kalem sessizce düşmez:** ekranda `#et-taslak-eksik` bandı `"{n} kalem artık listede yok; seçimden çıkarıldı."` yazar. Budanmış taslak depoya geri yazılır, böylece bir sonraki açılışta aynı kalem yeniden sayılmaz.
- **"Kaldığınız yerden" bandı:** `#et-taslak-geri-yuklendi` bandı `"Kaldığınız yerden devam ediyorsunuz: {n} ürün seçili, {k} kopya."` yazar.
- **"Taslağı temizle" düğmesi (`#et-taslak-temizle`):**
  - Yalnız temizlenecek bir şey varken çizilir. Etkisiz düğme yok (DoD EK/1).
  - Depodaki anahtarı siler; seçimi, adedi (1) ve aramayı sıfırlar.
  - Başarı bandının yanında **"Geri al"** düğmesi (`#et-taslak-geri-al`) çıkar: bellekteki kopya geri döner ve depo yeniden yazılır.
  - Onay için ModalPencere kullanılmadı, çünkü eylem geri alınabiliyor. Ayrıca bu yol denetim.sh'taki ModalPencere tavanını (55) artırmıyor.
- **Yazdırma sonrası:** `window.print` hatasız dönünce kayıtlı taslak silinir ve `#et-yazdir-basari` bandı çıkar (UX-S27). Ekrandaki seçim **korunur**, iptal edilen baskı hemen tekrarlanabilsin diye. Kullanıcı bir şey değiştirirse taslak yeniden yazılır.
- **Depo kapalıysa:**
  - Her `getItem`, `setItem` ve `removeItem` çağrısı try/catch içinde.
  - Hata olursa taslak yokmuş gibi davranılır ve sayfa çalışmaya devam eder.
  - Seçimin kaybolacağı sessiz kalmaz: `#et-taslak-saklanamiyor` notu `"Bu tarayıcı seçimi saklamıyor. Sayfadan çıkarsanız seçim kaybolur."` yazar (Anayasa md.1).
- **Yan düzeltme:** `Sayi` bileşenindeki `DegerChanged="@(v => { … })"` öznitelik lambdası, kurala uygun olarak `AdetGirildi(decimal?)` yöntemine taşındı. Bu kural "Razor öznitelikte '{ }' lambda yok" der. Davranış aynı, eleman kimliği `et-adet` değişmedi.
- Mevcut eleman kimliklerinin hiçbiri değişmedi. Yeni kimlikler `et-` önekli.

### 2.2 Metinler: `Kuyumcu.Core/I18n/Sozlukler.EtiketTaslak.cs` (YENİ)
- `EtiketTaslakMetinleri` konumsal tipi (7 anahtar) ve `EtiketTaslakSozlugu.Tr/En/Al`.
- Ortak `Sozlukler.cs` dosyasına ve S3-3'ün `Sozlukler.Urunler.cs` dosyasına **dokunulmadı**.

### 2.3 Testler: `Kuyumcu.Tests/EtiketEkranTestleri.cs` (YENİ, 8 test)
Tarayıcı deposu, bUnit JSInterop üzerinden bellek içi bir sözlükle taklit edildi. "Sayfadan çıkıp dönmek" şöyle kuruldu: bileşen `DisposeComponentsAsync` ile atılıyor, aynı depoyla yeniden çiziliyor. Jeton, `TokenServisi`'nin gerçek talep adlarını (`sub`, `tenantId`) taşıyor.

| Test | Ne sınar |
|---|---|
| `EtiketTaslagiGeriYuklenir` | Seçim (u1, u3) ve adet 4 verildikten sonra anahtar **kiracı + kullanıcı** ile yazılıyor ve JSON içeriği doğru. Dönüşte aynı seçim geliyor, adet 4, 8 önizleme, bant metni tam eşleşiyor |
| `EksikUrunlerGeriYuklemedeBildirilir` | Kayıtlı 3 kimliğin 2'si listede yok: bant `"2 kalem artık listede yok"`, 1×2 önizleme, depo budanmış |
| `TaslagiTemizleFormuBosaltir` | Depo anahtarı silindi. Önizleme 0, adet "1", arama boş, kutuların hepsi boş, başarı bandı var, düğme gizlendi. "Geri al" 10 önizlemeyi ve depoyu geri getiriyor |
| `DepoErisilemezseSayfaCalisir` | `getItem`, `setItem` ve `removeItem` JSException fırlatıyor. Hata bandı yok, not görünüyor, seçim + adet + önizleme + Yazdır + temizle çalışıyor |
| `EtiketTaslagi_baska_kullanicinin_taslagini_okumaz` | Aynı kiracıda başka kullanıcının taslağı okunmuyor |
| `Yazdirma_sonrasi_taslak_temizlenir_ekran_secimi_korunur` | Tek `window.print` çağrısı var, depo silindi, bant çıktı, önizleme korundu |
| `Kimliksiz_oturumda_depo_kullanilmaz_ve_bu_soylenir` | Jeton yoksa depoya hiç yazılmıyor ve not çıkıyor |
| `Taslak_metinleri_TR_EN_dolu_ve_farkli` | Yansımayla 7 anahtarın hepsi TR ve EN'de dolu ve iki dilde farklı |

## 3. Sapmalar ve gerekçeleri

1. **Anahtar jetondan okunuyor, `OturumKullanici`'dan değil.**
   - `OturumKullanici` kiracı kimliği taşımıyor. Onu değiştirmek dosya sınırının dışında kalıyor ve onlarca testte kullanılan ortak bir tip.
   - Profil (`/auth/ben`) sayfa açılışından sonra gelebildiği için kullanıcı kimliği de oradan güvenilir okunamıyor.
   - Jeton yükü (`tenantId`, `sub`) yalnız **anahtar üretmek** için okunuyor. İmza doğrulaması yok, çünkü bir yetki kararı verilmiyor.
   - Kimlik çözülemezse depo **kullanılmıyor** (fail-closed). Başka bir kullanıcının taslağına karışma riski yerine ekrana açık not düşülüyor.
2. **"Yazdırma başarıyla tamamlanınca" koşulu.** Tarayıcı, baskı diyaloğunun "Yazdır" ile mi "İptal" ile mi kapandığını bildirmez. Bu yüzden ölçüt `window.print` çağrısının hatasız dönmesi. İptal ihtimaline karşı ekrandaki seçim korunuyor (§ 2.1).
3. **Temizleme için onay modalı yerine "Geri al".** Eylem geri alınabildiği için onay gereksiz. ModalPencere tavanına dokunulmadı.
4. **Canlı doğrulama portu 5185.** Genel kural 5198 diyor, ancak şerit koşulu 5185'i ve "5185 dışındaki 5185-5200 portlarına dokunma" kuralını koyuyor. Şerit koşulu uygulandı.
5. **Tam süit `--no-build` ile koşuldu.** Hemen önce temiz `dotnet build Kuyumcu.sln` yeşil alındı (0 uyarı, 0 hata). Böylece paralel şeritlerle derleme yarışına girilmedi. İlk filtreli koşumda MSB4018 (`Kuyumcu.Web.dll` başka süreçte açık) görüldü; yeniden koşumda derleme temiz geçti.

## 4. Migration
**Gerekmedi.** Sunucuda şema, uç ya da varlık değişikliği yok. Taslak tarayıcıya özgü bir kolaylık.

## 5. DoD sözleşmesi

| Kayıt | Düzenleme / pasife alma yolu | Gerekçe |
|---|---|---|
| Etiket basım taslağı (tarayıcı deposu, sunucu varlığı DEĞİL) | Seçim, adet ve arama her an değiştirilebilir. "Taslağı temizle" siler, "Geri al" geri getirir, yazdırma sonrası otomatik silinir | Yeni sunucu varlığı yok. Bu yüzden PATCH/DELETE ucu, `[RolGerekli]` ve denetim kaydı gerekmez |
| Ürün (mevcut) | Değişmedi. Pasife alınan ürün taslaktan **sayıyla** düşer | — |

- **Dokunulan sayfalar:** `Pages/UrunEtiketleri.razor`.
  - Bu sayfa önceki dalgada S3-3 tarafından açılmıştı. İkinci dokunuşun gerekçesi: triyaj tur 8, P-66'yı bu dosyaya atadı; çakışma haritasındaki sıra "P-66 → P-63".
  - S3-3'ün "seçim görünen listenin alt kümesi" kuralı korundu. Kanıt: S33 etiket testleri 6/6 yeşil.
- **Mevzuat:** bu paket hiçbir mevzuat hükmüne dokunmadı. md.11/(e) ibaresi ve rodyum politikası aynen duruyor.

## 6. Test sayıları

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Filtre: `EtiketEkranTestleri` + `S33EtiketEkranTestleri` | 14 | 14 | 0 |
| Filtre: `UrunEtiketEkranTestleri`, `UX1C1EkranTestleri`, `B1GorunumSecenekleriTestleri`, `~Etiket` | 105 | 105 | 0 |
| **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_m`, trx `scratchpad/trx-pP-66/_Mac_2026-09-24_03_12_23.trx`) | **4813** | 4812 | **1** |

- **Önce/sonra:** bu paketin yeni testleri 8. Aynı ağaçta paket öncesi süit 4805 olur. Paralel şeritler de test ekliyor; bu değer hesaplama, ölçüm değil.
- **Tek kırmızı:** `Kuyumcu.Tests.KurulumEkranTestleri.Eksik_adim_maddeleri_ilgili_rotaya_baglanir`, mesajı "ISLETME bağlantısı boş".
  - **Sahibi SERİT C / P-68** (kurulum kilit gerekçesi). Bu paket `Kurulum.razor`'a dokunmadı.
  - Koşum sırasında `Kuyumcu.Web/Pages/Kurulum.razor` 03:14:02'de, `KurulumEkranTestleri.cs` 03:08:32'de değişti: P-68 yazıyordu.
  - **Bu paketin ürettiği yeni kırmızı: 0.**

## 7. Mutasyon tablosu
Yöntem: kaynak `scratchpad/mut-yedek/P-66/UrunEtiketleri.razor` altına kopyalandı. Her mutasyon için `perl -0777 -pi` ile tek değişiklik yapıldı, filtreli `dotnet test -v n` koşuldu, yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı (betik `scratchpad/p66-mut.sh`).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M66-1 | `if (listeGeldi) await TaslakGeriYukle();` kaldırıldı (geri yükleme çağrısı yok) | **EtiketTaslagiGeriYuklenir**, EksikUrunlerGeriYuklemedeBildirilir, TaslagiTemizleFormuBosaltir, DepoErisilemezseSayfaCalisir | cmp=aynı |
| M66-2 | `eksikKalem = kayitli.Count - secili.Count;` → `eksikKalem = 0;` (eksik sayım sessizce düşer) | **EksikUrunlerGeriYuklemedeBildirilir** | cmp=aynı |
| M66-3 | `TaslagiTemizle` içindeki `await TaslakSil();` kaldırıldı | **TaslagiTemizleFormuBosaltir** | cmp=aynı |
| M66-4 | `getItem` sarmalayıcısı `catch (Exception)` → `catch (InvalidCastException)` | **DepoErisilemezseSayfaCalisir** | cmp=aynı |
| M66-5 | Anahtardan kiracı çıkarıldı (`{TaslakOnek}:{kullanici}`) | **EtiketTaslagiGeriYuklenir**, EksikUrunlerGeriYuklemedeBildirilir, TaslagiTemizleFormuBosaltir, Yazdirma_sonrasi_… | cmp=aynı |
| M66-6 | Yazdırma sonrası `await TaslakSil();` kaldırıldı | **Yazdirma_sonrasi_taslak_temizlenir_ekran_secimi_korunur** | cmp=aynı |

Spesifikasyonun istediği iki mutasyon (M66-1, M66-2) da adı yazılı testi kırmızıya döndürdü. Test açığı yok. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata**.

## 8. Denetim
`bash dotnet/denetim.sh` → **rc=0, "DENETİM TEMİZ."** Çıktıdaki ✗ satırı sayısı 0.

Bu paketin ilgili sayaçları:
- Sayfa içi `record ` tanımı artmadı: taslak için tip yazılmadı, JSON `Dictionary` ve `JsonDocument` ile işleniyor.
- ModalPencere kullanımı artmadı.
- Satır içi `style=` artmadı.
- `catch` içinde veri alanı boşaltılmadı (P-11 sayacı).

## 9. Canlı doğrulama (port 5185, gerçek Chrome headless + CDP, gerçek veri)

**Ortam:**
- API: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5185`, PID 74545. İş sonunda `kill` edildi; `lsof -iTCP:5185 -sTCP:LISTEN` boş.
- Test kiracısı: tek kullanımlık davetle `POST /auth/kayit` → **201**, `PAKETP-66-TEST Kuyumculuk`, `01M38C7CA0QNEK0PM5CM6DPH2T`. Parola ve jeton ekrana basılmadı; kayıt gövdesi boşaltıldı.
- Betikler: `scratchpad/p66-kur.sh`, `scratchpad/p66-canli.js`. Ham çıktı: `scratchpad/p66-canli.json`. Ekran görüntüleri: `scratchpad/p66-1..5-*.png`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /urunler` × 3 | 201 | **201 × 3** (P66-U1..U3) |
| İlk açılış | taslak yok, bant yok, temizle düğmesi yok | seçim [], adet 1, önizleme 0, bant yok, düğme yok, depo [] ✓ |
| U1 + U3 seç, adet 4 | depoda kiracı+kullanıcı anahtarı | anahtar `kuyumcu-etiket-taslak:01M38C7CA0QNEK0PM5CM6DPH2T:01M38C7CA0XYCNEFDMG86933G1`, değer `{"v":1,"secili":[U1,U3],"kopya":4,"arama":""}`, önizleme 8 ✓ |
| `/urunler`'e çık, geri dön | aynı seçim ve adet + bant | seçim [U1,U3], adet "4", önizleme 8, bant "Kaldığınız yerden devam ediyorsunuz: 2 ürün seçili, 4 kopya." ✓ |
| `DELETE /urunler/{U3}` (pasife al) | 200 | **200** `{"mesaj":"Ürün deaktif edildi"}` |
| Sayfaya yeniden dön | U3 düşer ve sayılır | seçim [U1], önizleme 4, bant "1 kalem artık listede yok; seçimden çıkarıldı.", depo [U1] olarak budandı ✓ |
| "Taslağı temizle" | depo silinir, form boşalır | depo [], seçim [], adet "1", önizleme 0, bant "Taslak temizlendi. Seçim, adet ve arama sıfırlandı." + "Geri al" ✓ |
| U2 seç → Yazdır (`window.print` sayaçlı saplama) | 1 baskı, taslak silinir, ekran korunur | baskı 1, depo [], seçim [U2] korundu, bant "Yazdırma penceresi açıldı. Kaydedilen taslak temizlendi." ✓ |
| Depo erişimi engelli (taslak anahtarları için `Storage.prototype` getItem/setItem/removeItem `SecurityError` fırlatır) | sayfa hatasız, taslak yok gibi | hata bandı yok, bant yok, not "Bu tarayıcı seçimi saklamıyor…", seçim sonrası önizleme 1, Yazdır açık ✓ |

**SQL kanıtı:**
```
select kod, aktif from urunler where tenant_id='01M38C7CA0QNEK0PM5CM6DPH2T' order by kod;
P66-U1 1 · P66-U2 1 · P66-U3 0
```
Sunucuda taslakla ilgili tablo ya da satır yok; taslak yalnız tarayıcıda.

**Test kiracısının silinmesi:**
- `information_schema`'da `tenant_id` kolonu olan tabloların hepsinden bu kiracının satırları silindi, ardından `tenants` ve `kiraci_davetleri` satırları.
- Kalan satır: tüm tablolarda **0**. `tenants where id=…`: **0**.
- `tenants` sayısı başlangıçta **10**, sonda **11**. Aradaki fark bu paketin değil: koşum sırasında SERİT C, `01M38CAXD2CCJ7GRGG6435BFFD PAKETP-68-TEST Kuyumcu` kiracısını açtı (2026-09-24 00:16 UTC). Ona dokunulmadı. `QA-TEST-` önekli fikstürlere de dokunulmadı.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Seçim + adet girildikten sonra sayfadan çıkıp dönünce aynısı gelir | ✓ | `EtiketTaslagiGeriYuklenir` + canlı adım 3–4 |
| Bulunamayan ürünler için `<n> kalem artık listede yok` bildirimi | ✓ | `EksikUrunlerGeriYuklemedeBildirilir` + canlı pasife alma adımı |
| "Taslağı temizle" taslağı siler, form boşalır | ✓ | `TaslagiTemizleFormuBosaltir` + canlı adım |
| Depo engelliyken sayfa hatasız açılır | ✓ | `DepoErisilemezseSayfaCalisir` + canlı engelli depo adımı |
| Kiracı + kullanıcı anahtarı, try/catch | ✓ | Anahtar canlıda görüldü; M66-4, M66-5 |
| Yazdırma sonrası taslak temizlenir | ✓ | `Yazdirma_sonrasi_…` + canlı adım |
| Paket kapısı (1): build 0 hata | ✓ | 0 uyarı, 0 hata |
| Paket kapısı (2): kendi testleri 0 başarısız | ✓ | 14/14 ve 105/105 |
| Paket kapısı (3): denetim.sh sayaçları | ✓ | DENETİM TEMİZ |
| Paket kapısı (4): mutasyon kanıtı | ✓ | 6/6 kırmızı, cmp aynı |
| Paket kapısı (5): canlı doğrulama | ✓ | § 9 |
| Paket kapısı (6): test kiracısı silindi | ✓ | § 9 |
| Paket kapısı (7): yeni kırmızı yok | ✓ | Tek kırmızı P-68'in |
| Dosya sınırı | ✓ | Yalnız `UrunEtiketleri.razor`, yeni `Sozlukler.EtiketTaslak.cs`, yeni `EtiketEkranTestleri.cs`. Ortak bileşenlere ve hub'a dokunulmadı |

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında bu paketin dosyaları:
- `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor` (değiştirildi; git'te `??`, çünkü ilk commit'ten sonra eklenmişti)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.EtiketTaslak.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/EtiketEkranTestleri.cs` (YENİ; `Kuyumcu.Tests/` dizini git'te tümüyle `??`)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-66-uygulama-raporu.md` (bu rapor)

## 12. Açık kalanlar
1. **Tam süitteki tek kırmızı:** `KurulumEkranTestleri.Eksik_adim_maddeleri_ilgili_rotaya_baglanir`. Sahibi SERİT C / P-68; dalga kapısında orkestratör ölçecek.
2. **P-63 (liste durum makinesi)** aynı sayfaya sırada giriyor (P-66 → P-63). P-63 `yukleniyor/hata/bos` dallarını ortak `ListeDurumu` bileşenine taşırken iki şeyi korumalı:
   - `listeGeldi` bayrağı: taslak yalnız başarılı listeden sonra geri yüklenir;
   - `#et-taslak-*` kimlikleri.
3. **Tarayıcı iptal ettiği baskıyı bildirmiyor.** Taslak `window.print` dönüşünde siliniyor, ekran seçimi korunuyor (sapma 2). İleride `afterprint` olayı ile bir teyit adımı düşünülebilir; kurucu kararı gerekmez.
