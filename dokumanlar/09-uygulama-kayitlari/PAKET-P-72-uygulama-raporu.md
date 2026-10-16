# PAKET P-72 — Denetim izinden makine üretimi KurSnapshot kayıtlarının ayrılması · Uygulama raporu

- Tarih: 2026-09-24 · Şerit B (Yedek + Kur + Denetim izi) · Rol: Taşkın (backend); ekran işi yok (Ertuğ kapsamı boş)
- Kaynak: `TRIYAJ-TUR8-2026-09-23.md` Katman D, P-72 (KRİTİK, ertelenebilir kümenin 1 numarası)
- Durum: **BİTTİ** (paket kapısının 7 maddesi sağlandı; tam süitteki tek kırmızı başkasına ait ve kararsız — aşağıda)

## 1. Spesifikasyonun koda karşı doğrulanması
| İddia | Ölçüm | Sonuç |
|---|---|---|
| `DenetimYazmaDinleyicisi.cs` tek istisna DenetimKaydi, tür bazlı filtre yok | Dosya okundu (140 satır): yalnız `giris.Entity is DenetimKaydi` atlanıyordu | DOĞRU |
| Denetim izinin %78,8'i (8437/6648) otomatik kur kaydı | Canlı `kuyumcu_net` (2026-09-24): `denetim_kayitlari` 152 000 satır, bunun **144 812**'si `varlik_turu='KurSnapshot' AND eylem='EKLEME' AND kullanici_id IS NULL` → **%95,3** | Sorun triyajdakinden BÜYÜK; "tamamı zamanlayıcıdan mı" sorusu: kullanıcısız olanlar 144 812 (triyajın doğrulayamadığı madde ölçüldü) |
| Dosya ayak izi | 3 dosya `test -e` ile VAR | DOĞRU |
| Manuel kur girişi/iptali | `POST /kur/manuel` (Added, oturumlu), `POST /kur/manuel/{id}/iptal` (Modified + `YazmaKaydet` gerekçeli iz) | Dışlama yalnız kullanıcısız EKLEME'yi hedefler → ikisi de izde kalır |

## 2. Yapılanlar
- `Data/DenetimYazmaDinleyicisi.cs`
  - Kurucuya isteğe bağlı `ILogger<DenetimYazmaDinleyicisi>? gunluk = null` (verilmezse `NullLogger`).
  - Tek yerde sabit, yorumla gerekçelendirilmiş dışlama listesi: `public static readonly IReadOnlyList<Type> MakineUretimiDislananTurler = [typeof(KurSnapshot)];` — refleksiyon yok, açık `typeof`.
  - `MakineUretimiDislanir(durum, tur, kullaniciId)` = `kullaniciId is null && durum == Added && liste.Contains(tur)`.
  - Dışlanan olay: `LogInformation("Denetim izi dışlaması (P-72): kullanıcı bağlamı olmayan {VarlikTuru} eklemesi denetim izine yazılmadı, teknik günlüğe düştü · kiracı {TenantId} · kayıt {VarlikId}")` → "gizleme değil ayrı defter".
  - Güncelleme/silme hiçbir türde dışlanmaz; append-only kuralı (DenetimKaydi değiştirilemez) aynen durur.
- `Kuyumcu.Api/Program.cs` — tek satır: dinleyiciye `ILogger<DenetimYazmaDinleyicisi>` verildi (yoksa üretimde günlük satırı düşmezdi). **Sapma gerekçesi:** ayak izinde yoktu ama "dışlanan olay teknik günlüğe düşer" kabul koşulu üretimde ancak böyle sağlanır; dinleyicinin kurulduğu tek yer burası.
- `Kuyumcu.Tests/DenetimIziTestleri.cs` — 4 yeni test + mevcut `SistemBaglamindaYazma_KullanicisizIzDusurur` uyarlandı.
  - **Sapma gerekçesi:** o test tam olarak kullanıcısız KurSnapshot eklemesinin ize düştüğünü bekliyordu, yani P-72'nin kaldırdığı davranışı. Testin niyeti ("kullanıcısız yazma izi düşer") dışlanmayan bir türle (Kasa) korundu.
- `DenetimDamgaTestleri.cs` değişmedi: KurSnapshot'a bağlı bir beklentisi yok, 7/7 yeşil.
- Sozlukler.cs, razor sayfası, sahte API: **dokunulmadı** (ekran metni yok; şerit sınırı).
- Migration: **gerekmedi** (şema değişikliği yok).

### Dokunulan sayfalar
Yok (razor dokunuşu yok).

## 3. DoD sözleşmesi
Yeni varlık/kayıt türü doğmadı. `DenetimKaydi` append-only ve salt okunur kalır (değiştirilebilen denetim izi, denetim izi değildir). `KurSnapshot`'ın düzeltme yolu değişmedi: manuel kur → `POST /kur/manuel/{id}/iptal` (S-3b); otomatik kur → değişmez, yeni kurla geçilir. Finansal varlıklara dokunulmadı.

## 4. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (mutasyonlardan sonraki temiz derleme dahil).

## 5. Testler
| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| `DenetimIziTestleri` + `DenetimDamgaTestleri` (önce: 18 + 7 = 25) | 29 | 29 | 0 |
| Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_q`, trx) | 4702 | 4701 | 1 (başkasına ait, aşağıda) |
| `S33UrunlerEkranTestleri` 3 kez yeniden | 22 ×3 | 22 ×3 | 0 |

Yeni testler:
- `DenetimIzi_ZamanlayiciKurSnapshotu_IzeYazilmaz`: HttpContext yokken KurSnapshot yazılır, iz sayısı değişmez.
- `DenetimIzi_ManuelKurGirisi_IzdeKalir`: oturumlu ekleme, kullanıcı damgalı EKLEME izi bırakır; iptal (Modified) izi kalır; kullanıcısız GÜNCELLEME de kalır; günlük boş.
- `DenetimIzi_KurDisiVarliklar_HalaYazilir`: liste tam olarak `[typeof(KurSnapshot)]`; kullanıcısız Kasa, KasaHareket ve Hareket (HareketFabrikasi.Ekle) eklemeleri ize düşer.
- `DenetimIzi_DislananOlay_TeknikGunlugeDuser`: iki kayıt için birer Information satırı yazılır; satırda tür, kayıt kimliği ve kiracı geçer.

Not (tuzak): `HttpContextAccessor` AsyncLocal paylaşır. Aynı test akışında oturum kurulduktan sonra `new HttpContextAccessor()` kullanıcıyı hâlâ görür, bu yüzden kullanıcısız adımda `HttpContext = null` açıkça verildi.

## 6. Mutasyon kanıtı
Yöntem: yedek `scratchpad/mut-yedek/P-72/orj.cs` → `perl -0777 -pi` ile tek eşleşme → `DenetimIziTestleri` filtreli koşum → geri kopyala → `cmp` ile AYNI olduğu doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma (cmp) |
|---|---|---|---|
| M72-1 | Dışlama koşulu kaldırıldı (`if (false && MakineUretimiDislanir(...))`) | `DenetimIzi_ZamanlayiciKurSnapshotu_IzeYazilmaz`, `DenetimIzi_DislananOlay_TeknikGunlugeDuser` | AYNI |
| M72-2 | Kullanıcı bağlamına bakılmadı (`kullaniciId is null` → `true`) | `DenetimIzi_ManuelKurGirisi_IzdeKalir` | AYNI |
| M72-3 | Listeye ikinci tür eklendi (`typeof(Kasa)`) | `DenetimIzi_KurDisiVarliklar_HalaYazilir` (ayrıca `Ekleme_OtomatikIzDusurur`, `SistemBaglamindaYazma_KullanicisizIzDusurur`, `Silme_IzDusurur`) | AYNI |
| M72-4 | Log satırı silindi | `DenetimIzi_DislananOlay_TeknikGunlugeDuser` | AYNI |

## 7. denetim.sh
`dotnet/denetim.sh` → rc=0, **DENETİM TEMİZ.**

## 8. Canlı doğrulama (port 5187, izolasyon notu gereği; 5190-5200 aralığına dokunulmadı)
Test kiracısı `PAKETP-72-TEST Kuyumcu` (tek kullanımlık davet satırı SQL ile açıldı, iş sonunda silindi).

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /api/v1/auth/kayit` | 201 | 201, tenantId döndü |
| Zamanlayıcı KurSnapshot yazar (P-72 kodlu 5187 süreci) | Satır yazılır, izi yazılmaz, Information satırı düşer | Günlükte 12 "Denetim izi dışlaması (P-72)" satırı. SQL: günlükteki 12 kimliğin `kur_snapshots`'ta **12**'si var, `denetim_kayitlari`'nda **0**'ı var |
| `POST /api/v1/kur/manuel` (SAHİP oturumu) | 201, izde kalır | 201. SQL: `EKLEME` izi var, kullanıcılı=1 |
| `POST /api/v1/kur/manuel/{id}/iptal` | 200, izde kalır | 200. SQL: `GUNCELLEME` (IptalEdildi, IptalGerekce, IptalTarihi) kullanıcılı=1 + gerekçeli `EKLEME` izi "manuel kur kaydı GEÇERSİZ kılındı" |

Canlı gözlem: aynı `kuyumcu_net`'te eski kodla çalışan başka bir API süreci (PID 12577, port 5200, orkestratörün/QA'nın süreci) de zamanlayıcı çalıştırıyor. Onun yazdığı KurSnapshot'lar hâlâ ize düşüyor. Örnek: kiracı 01M23ZP… için 22:47:32–22:48:37 aralığında 7 snapshot var; P-72 sürecinin yazdığı 2'sinin izi yok, eski sürecin yazdığı 5'inin izi var. Bu, yayın anlık görüntüsü yenilenince kendiliğinden kapanır.

API durduruldu (`lsof :5187` boş). Test kiracısının `tenant_id` kolonlu tüm tablolardaki satırları silindi (kalan 0); tenants satırı ve davet satırı da silindi. **tenants sayısı: başlangıç 10 → son 10.** `QA-TEST-` kiracılarına dokunulmadı.

## 9. Değişen dosyalar
- `dotnet/Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs`
- `dotnet/Kuyumcu.Api/Program.cs` (tek çağrı satırı)
- `dotnet/Kuyumcu.Tests/DenetimIziTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-72-uygulama-raporu.md`

`git status` notu: Program.cs ağaçta zaten "M" idi. Api/Tests dosyaları izlenmeyen (`??`) ağaçta.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Zamanlayıcı bağlamında KurSnapshot eklenince DenetimKayitlari artmaz | KARŞILANDI | Test 1 + canlıda 12/12 kimliğin 0 izi var |
| Manuel kur girişi/iptali izde kalır | KARŞILANDI | Test 2 + canlı SQL (3 iz satırı, kullanıcılı) |
| Dışlanan her olay için Information log | KARŞILANDI | Test 4 + canlıda 12 günlük satırı |
| Liste tam olarak 1 tür | KARŞILANDI | Test 3 ve M72-3 |
| dotnet test 0 başarısız (paket ölçüsü) | KARŞILANDI | Kendi 29 testi yeşil; tam süitte P-72'nin ürettiği kırmızı 0 |
| denetim.sh TEMİZ | KARŞILANDI | "DENETİM TEMİZ." |

## 11. Açık kalanlar
- Tam süitte 1 kırmızı: `S33UrunlerEkranTestleri.S33_10_Durum_suzgeci_aktif_parametresiyle_uca_gider_ve_limit_ACIKCA_istenir`. bUnit `WaitForFailedException` (zaman aşımı) verdi. Tek başına 3/3 koşuda 22/22 yeşil, yani yük altında kararsız. Sahibi: Ürünler ekranı / S33 (bu şeridin dosya sınırı dışında). P-72 yalnız sunucu tarafı interceptor'a dokundu.
- Canlıda eski kodla çalışan 5200 süreci kullanıcısız kur izi yazmaya devam ediyor. Yayın anlık görüntüsü yenilenince kapanır (orkestratör / YAYIN şeridi).
- Tarihsel ~144 812 makine izi DB'de duruyor. Append-only kuralı gereği bu paket silmedi. Arşivlenip arşivlenmeyeceği kurucu/Mehmet kararıdır; mevzuat saklama süresi doğrulanmadan silinmemeli.

## 12. Kurucuya / mali müşavire not
Mevzuat hükmü kodlanmadı. Dışlama salt teknik bir karar: kullanıcı eylemi değil, makine kur çekimi. Kur verisinin kendisi `kur_snapshots`'ta tam olarak saklanmaya devam ediyor; yalnız denetim izinde çoğaltılmıyor.
