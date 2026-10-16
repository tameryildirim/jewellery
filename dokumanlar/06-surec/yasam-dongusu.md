# Yazılım Yaşam Döngüsü Süreçleri ve Uyarlama (Tailoring)

| | |
|---|---|
| **Belge kimliği** | KYM-YD-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Tamer Bey |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC/IEEE 12207:2017** — Software life cycle processes |

> ⚠ ISO/IEC 12207:**2008**'in "Primary / Supporting / Organizational" yapısı **eskimiştir**. 2017 sürümü **dört süreç grubu** ve 30 süreç tanımlar. İnternetteki birçok özet hâlâ 2008 yapısını verir.

---

## 0. Uyarlama (tailoring) ilkesi

12207:2017 tüm süreçlerin uygulanmasını değil, **projeye uygun şekilde uyarlanmasını** öngörür. Bu belge her süreç için üç şeyden birini söyler:

| İşaret | Anlamı |
|---|---|
| ✅ | Uygulanıyor — kanıtı var |
| 🟡 | Kısmen uygulanıyor |
| ➖ | **Bilinçli olarak uygulanmıyor** — gerekçesiyle |

**"Uygulanmıyor" demek eksiklik itirafı değildir; gerekçesiz uygulanmaması eksikliktir.**

---

## 1. Agreement processes — Anlaşma süreçleri

| Süreç | Durum | Açıklama |
|---|---|---|
| **Acquisition** | ➖ | ByGatech bu projede tedarik eden taraf değil, geliştiren taraftır |
| **Supply** | 🟡 | Kurucu ile müşteri arasındaki ticari ilişki ByGatech'in kapsamı dışında. **Kod devrinin hukuki belgesi yoktur** — [TB'de](../08-degerlendirme/teknik-oz-degerlendirme.md) açık madde |

---

## 2. Organizational Project-Enabling processes

| Süreç | Durum | Kanıt |
|---|---|---|
| **Life Cycle Model Management** | ✅ | Proje klasör yapısı yaşam döngüsüne göre: `00-brief → 01-mimari → 02-gelistirme → 03-test → 04-guvenlik → 05-teslimat` |
| **Infrastructure Management** | 🟡 | Geliştirme altyapısı var (Docker MySQL, test ortamı). **CI/CD yok** (TB-03) |
| **Portfolio Management** | ✅ | `projeler/` altında çoklu proje yönetimi; her proje ayrı klasör |
| **Human Resource Management** | ✅ | Rol tanımlı kadro; sözcü kuralı; model çeşitliliği (QA ve Güvenlik farklı model ailesinde) |
| **Quality Management** | ✅ | Fail-closed kalite kapıları; [test politikası](../04-test/test-politikasi.md) |
| **Knowledge Management** | ✅ | 12 ADR · kod içi gerekçe yorumları · `hafiza/SESSION-HANDOFF.md` · bu belge seti |

> **Knowledge Management özel not:** Bus factor 1 riski (tek katkı veren) bu süreçle azaltılmaktadır. Tasarım bilgisi kişide değil belgede tutulur. Eksik olan **operasyonel** bilgidir (runbook).

---

## 3. Technical Management processes

| Süreç | Durum | Kanıt |
|---|---|---|
| **Project Planning** | ✅ | İki sıralı plan; her adımın **kabul ölçütü** kanıta bağlı. **Süre tahmini bilinçli verilmiyor** — geçmiş hızdan çarpan uydurmak Anayasa md.1'e aykırı |
| **Project Assessment and Control** | ✅ | Her adım sonunda tamamlama raporu; ilerleme tablosu kanıtla güncellenir |
| **Decision Management** | ✅ | **Çok ajanlı oylama** → Tamer Bey mührü → ADR. Her kararın olumsuz sonuçları da yazılır |
| **Risk Management** | ✅ | [Risk kaydı](../07-proje-yonetimi/risk-kaydi.md) · [teknik borç envanteri](../08-degerlendirme/teknik-borc-envanteri.md) |
| **Configuration Management** | 🟡 | Git kullanılıyor; şema migration ile yönetiliyor. **Ama:** derleme çıktısı sürüm kontrolünde (1.408 dosya) ve **commit yasağı** yürürlükte |
| **Information Management** | ✅ | Belge seti standartlara bağlı; her belgede kimlik, sürüm, durum, onay makamı |
| **Measurement** | ✅ | Ölçüm kültürü belgenin merkezinde: kapsam, karmaşıklık, boyut, zafiyet, lisans, bütünlük — hepsi **tekrarlanabilir komutlarla** |
| **Quality Assurance** | ✅ | `denetim.sh` (11 kural, **kendisi mutasyonla sınanmış**) · mutasyon kanıtı zorunluluğu |

### Decision Management — bu projenin ayırt edici süreci

Karar alma yöntemi:

1. **Araştırma** — birincil kaynak zorunlu; blog kaynak sayılmaz
2. **Çok ajanlı oylama** — Mimari, Güvenlik, QA, Satış ayrı ayrı oy verir
3. **İddia denetimi** — oy verenin gerekçesi **ölçülür**; yanlışsa düzeltilir
4. **Mühür** — Tamer Bey
5. **ADR** — bağlam, karar, gerekçe, **olumsuz sonuçlar**, "ne zaman yanlış olur"

**3. adım kritiktir.** Örnek: ADR-0010'da Baş Mimar'ın gerekçesi ("iki bağımsız savunma katmanı") denetlendi ve **yanlış bulundu** — iki kontrol aynı JWT claim'ini okuyordu. Karar yine de doğruydu, ama **sebebi başkaydı**. Gerekçe düzeltilerek mühürlendi.

> Bir gerekçenin yanlış olması, kararın da yanlış olduğu anlamına gelmez. İkisi ayrı ayrı denetlenir.

---

## 4. Technical processes

| Süreç | Durum | Kanıt |
|---|---|---|
| **Business or Mission Analysis** | ✅ | Pazar araştırması: 19 rakip, doğrulanmış fiyat aralığı, birincil kaynaklı pazar büyüklüğü (İSO/TÜİK 2023: 19.905 perakende firma) |
| **Stakeholder Needs and Requirements Definition** | 🟡 | [StRS](../01-gereksinimler/StRS.md) yazıldı **ama 8 gereksinim hiçbir gerçek kullanıcıyla doğrulanmadı** — pilot erişimi (K4) bekliyor. Bu, belgenin kendi içinde açıkça işaretlenmiştir |
| **System/Software Requirements Definition** | ✅ | [SRS](../01-gereksinimler/SRS.md) — 29148:2018 yapısı, mevzuata izlenebilir |
| **Architecture Definition** | ✅ | [Mimari tanımı](../02-mimari/mimari-tanimi.md) — 42010:2022 + arc42 + C4 |
| **Design Definition** | ✅ | 12 ADR + kod içi gerekçeler |
| **System Analysis** | 🟡 | Yapısal analiz var (karmaşıklık, bağımlılık, şema). **Başarım analizi yok** (TB-12) |
| **Implementation** | ✅ | 16.451 satır C#, 5.072 satır Blazor, 9 migration |
| **Integration** | ✅ | Modüller tek süreçte; HTTP boru hattı testleriyle doğrulanıyor |
| **Verification** | ✅ | 392 test · `denetim.sh` · mutasyon kanıtı · `yetim-tara.sh` |
| **Transition** | ➖ | **Yayın yapılmadı.** Güvenlik vetosu duruyor; pilot erişimi yok |
| **Validation** | ❌ | **Gerçek kullanıcıyla doğrulama YAPILMADI.** En büyük süreç açığı — K4 |
| **Operation** | ➖ | Üretim ortamı yok |
| **Maintenance** | ➖ | Yayın yapılmadığı için bakım döngüsü başlamadı |
| **Disposal** | ✅ | Veri saklama ve **imha** politikası tanımlı (8 yıl + maskeleme, ADR-0008) |

---

## 5. Açık süreçler — dürüst liste

| Süreç | Durum | Neden |
|---|---|---|
| **Validation** | ❌ Yapılmadı | Pilot kuyumcu erişimi yok (**K4**). Doğrulanmamış bir ürün "doğrulandı" diye işaretlenemez |
| **Transition** | ➖ Başlamadı | Güvenlik vetosu + pilot yok |
| **Operation / Maintenance** | ➖ Başlamadı | Üretim ortamı yok |
| **Infrastructure Management** | 🟡 | CI/CD yok |
| **Configuration Management** | 🟡 | `bin`/`obj` sürüm kontrolünde; commit yasağı |
| **Stakeholder Requirements** | 🟡 | 8 gereksinim doğrulanmadı |
| **Supply** | 🟡 | Kod devrinin hukuki belgesi yok |

**Yedi süreç tam uygulanmıyor, yedisinin de gerekçesi yazılı.** Bunların dördü dış bağımlılığa (pilot, sözleşme), üçü yapılabilir işe bağlıdır.

---

## 6. Yaşam döngüsü modeli

**Artımlı ve kanıta bağlı.** Şelale değil, ama serbest de değil: her adımın **kabul ölçütü** önceden yazılır ve kanıtlanmadan sonraki adıma geçilmez.

```
Araştırma (birincil kaynak)
   → Ekip oylaması  → İddia denetimi  → Mühür (ADR)
   → Uygulama
   → Doğrulama: test + mutasyon kanıtı + denetim betiği + CANLI doğrulama
   → Tamamlama raporu (gerçek komut çıktısıyla)
   → Sonraki adım
```

**Bu modelin ayırt edici kuralı:** "Testler geçti" tek başına bir tamamlanma kanıtı sayılmaz. Gerekçesi ölçülmüştür — bu projede **4 gerçek hata** yüzlerce yeşil testten geçtikten sonra ancak canlı ortamda ortaya çıkmıştır.
