# Mevzuat Gereksinimleri ve Birincil Kaynak İzlenebilirliği

| | |
|---|---|
| **Belge kimliği** | KYM-MEV-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu · *mali müşavir teyidi bekleyen kalemler işaretli* |
| **Durum** | GÖZDEN GEÇİRİLDİ |

---

## 0. İzlenebilirlik ilkesi

Bu üründeki her yasal davranışın **birincil kaynağa** bağlanması zorunludur.

**Blog yazısı, haber sitesi veya rakip ürünün pazarlama metni kaynak sayılmaz.** Bu kural soyut değildir — bir kez ihlal edildi ve maliyeti oldu:

> KMTS, bir blog yazısına dayanarak "olmazsa olmaz yükümlülük" olarak plana alınmıştı. Birincil kaynağa (Bakanlık Usul ve Esaslar, 487 satır) gidildiğinde **"kuyumcu" kelimesinin 0 kez, "perakende" kelimesinin 0 kez** geçtiği görüldü. Yükümlü taraf baştan sona rafineriler ve Darphane. Yanlış tespit düzeltildi.

Aynı iddia bugün **iki rakip ürünün** sitesinde "Nisan 2026 KMTS düzenlemesine tam uyumluluk" olarak satılmaktadır. Birincil kaynakta karşılığı yoktur.

| İşaret | Anlamı |
|---|---|
| ✅ | Birincil kaynak okundu, doğrulandı |
| 🟡 | Kaynak bulundu ama teyit gerekiyor |
| ❌ | **Kaynak doğrulanamadı** — uygulanmadı veya varsayılmadı |

---

## 1. MASAK — 5549 sayılı Kanun ve Tedbirler Yönetmeliği

| # | Gereksinim | Dayanak | Durum | Üründe |
|---|---|---|---|---|
| **M-01** | Kimlik tespiti eşiği **185.000 TL** | Tedbirler Yönetmeliği **md.5/1-b**; rakam 14.01.2023 tarih **32073** sayılı RG, **6702** sayılı CB Kararı ile | ✅ | `MasakEsikleri.KimlikEsigi`, yapılandırmadan |
| **M-02** | Kimlik tespiti **işlem yapılmadan ÖNCE** tamamlanır | Tedbirler Yönetmeliği **md.5/2** | ✅ | İki fazlı kapı — doğrulama transaction'dan önce; eksikse **400** |
| **M-03** | Şüpheli işlem bildirimi **en geç 10 iş günü** | Tedbirler Yönetmeliği **md.28/2** | ✅ | `SonBildirimTarihi` — hafta sonu **ve resmî tatiller** hariç |
| **M-04** | Belge ve kayıtlar **8 yıl** muhafaza | Tedbirler Yönetmeliği **md.46/1** | ✅ | `SaklamaBitis`; süre dolunca **maskeleme** |
| **M-05** | İşlem amacı beyanı, min. **300 karakter** açıklama | MASAK Rehber 2.0 **§4.2.7** | 🟡 | Uygulandı; rehber sürümü teyide açık |
| **M-06** | Beyan formu kademeleri (2.000.000 / 20.000.000 TL) | **Kaynak doğrulanamadı** — Yönetmelikte bulunamadı; Ağustos 2025 **taslak** tebliğ bankalara yönelik, kuyumcu geçmiyor | ❌ | Eşikler **yapılandırmada**, değiştirilebilir. Mali müşavir teyidi **K2** |
| **M-07** | ŞİB gönderimi | md.27 · MASAK Online **XML yükleme** | 🔒 | **API ve test sunucusu YOK.** XSD kamuya açık değil. Sistem hazırlar, **gönderim elle** |

**2026'da MASAK mevzuatında yeni bir değişiklik bulunamadı.** 185.000 TL eşiği 2023 tarihlidir.

---

## 2. VUK — Vergi Usul Kanunu

| # | Gereksinim | Dayanak | Durum | Üründe |
|---|---|---|---|---|
| **V-01** | Vergi mükellefi olmayandan alımda **gider pusulası** | VUK **md.234** | ✅ | Kimlik zorunlu, TCKN checksum, tutar **yazıyla** |
| **V-02** | Gider pusulası **7 gün** içinde düzenlenir | VUK md.234 | ✅ | `DuzenlemeSonTarihi` + gecikme listesi |
| **V-03** | Tevsik zorunluluğu haddi **30.000 TL** | Dayanak **VUK mükerrer md.257/1-(2)**; düzenleme **459 Sıra No'lu VUK Genel Tebliği** (RG 24.12.2015/29572), had **575** sıra no'lu Tebliğ ile (RG **30.11.2024 / 32738**) 7.000 → 30.000 | ✅ Kaynak | ✅ `TevsikKurali` + `TevsikKapisi` — aynı gün/aynı cari kümülatif, fail-closed |
| **V-03a** | **Mükellef olmayandan** (vatandaştan hurda altın) yapılan alım da kapsamdadır | **572** Sıra No'lu Tebliğ, RG **18.10.2024 / 32696** — tanımlar (ç) bendine *"mükellef olmayanları"* eklendi | ✅ Birincil metin okundu | ✅ Kapı satıcı tipine bakmaz — doğru davranış |
| **V-03b** | **Kısım kısım ödeme**: haddi aşan tek bir işlemin taksitlendirilmesinde **her bir** ödeme banka kanalıyla olmalı | 459 s. Tebliğ **§4.1.2** | ✅ Kaynak | ❌ **Üründe YOK** — TB-15. Kapı yalnız *aynı gün* kümülatifini görüyor |
| **V-03c** | Tevsike aykırı ödemede ceza **işlem tutarının %10'u**, asgari tutardan az olmamak üzere; **her iki tarafa ayrı ayrı** | VUK **mükerrer md.355/4** (7524 s. Kanun, RG 02.08.2024/32620 ile "mükelleflerden" → "uymayanların", %5 → %10) | ✅ | ➖ Bilgilendirme — kapı ihlali zaten engelliyor |
| **V-03d** | Aykırı ödemeyi **5 iş günü** içinde kendiliğinden idareye bildiren **ödeyene** ceza kesilmez | VUK mükerrer md.355/4 son cümle | ✅ | ❌ Üründe YOK — TB-16 (geç fark edilen ihlal için bildirim akışı) |
| **V-04** | Fatura / e-fatura haddi | **ÇELİŞKİLİ** — dört farklı rakam bulundu: 12.000 / 36.000 / 3.000.000 / 5.000.000 TL | ❌ | **Kodlanmadı.** Eşik yapılandırmadan okunacak biçimde hazırlandı. Mali müşavir teyidi **K2** |

> **V-03 terminoloji düzeltmesi (2026-09-14):** Önceki sürümde "VUK md.459" yazıyordu. **Böyle bir madde yoktur.** 459, Vergi Usul Kanunu'nun *mükerrer 257'nci maddesine* dayanılarak çıkarılan **Genel Tebliğ**in sıra numarasıdır. Kod içindeki hata mesajları da bu düzeltmeyi taşır.
>
> **V-03a önemlidir:** 18.10.2024'ten **önce** vatandaştan alımın kapsamda olup olmadığı tartışmalıydı; 572 s. Tebliğ ile tartışma bitti. Kuyumcunun vatandaştan 30.000 TL üstü hurda altını **nakit** alması bugün açık bir ihlaldir ve **hem kuyumcuya hem vatandaşa** ayrı ayrı ceza kesilir. Kapının uyarı değil **durdurucu** olması bu yüzdendir.

> **V-04 için not:** Dört çelişen rakamdan birini seçmek tahmin olurdu. Mevzuatı yanlış uygulamanın bedeli, özelliği geciktirmenin bedelinden ağırdır.

---

## 3. Kuyum Ticareti Yönetmeliği

| # | Gereksinim | Dayanak | Durum | Üründe |
|---|---|---|---|---|
| **K-01** | **Sentetik / laboratuvar üretimi taşlar** etiket, sertifika, fatura, internet sayfası ve reklamda açıkça belirtilmeli; doğal taşlardan **ayrı kategoride** satılmalı | **RG 24.06.2026 / 33290** — md.11'e eklenen **(e) bendi** | ✅ Birincil metin okundu | ✅ `TasKokeni` + `TasKapisi`: ürün kartında zorunlu alan, satış kapısı, `/urunler/{id}/etiket`, `?tasKategori=` ayrımı, listede ibare rozeti |
| **K-02** | Yetki belgesi tadili; başvuru ve sonuçlandırma **on gün** | **RG 29.04.2026 / 33238** | ✅ | ➖ Yazılım yükümlülüğü değil (işletme süreci) |

**K-01 birebir metin:**
> *"Laboratuvar ortamında üretilen veya insan müdahalesiyle oluşturulan kıymetli taş ürünlerini 'sentetik', 'laboratuvar üretimi', 'yapay üretim' … ibarelerinden en az birine etiket, ürün sertifikası, fatura, internet sayfası, reklam ve tanıtım materyallerinde … açıkça yer vermeksizin satışa sunamaz. Bu ürünler ile doğal kıymetli taşları … ayrı bölümlerde, internet ortamında ayrı kategorilerde satışa sunar."*

Bu doğrudan bir yazılım işidir: ürün kartı zorunlu alanı, etiket şablonu, fatura satır metni, kategori ayrımı.

---

## 4. KMTS — Kıymetli Maden Takip Sistemi

| # | Tespit | Dayanak | Durum |
|---|---|---|---|
| **KM-01** | **Perakende kuyumcuya doğrudan yükümlülük getirmiyor** | Hazine ve Maliye Bakanlığı, *KMTS Uygulamasına İlişkin Usul ve Esaslar* (487 satır, **tam metin tarandı**): "kuyumcu" **0 kez**, "perakende" **0 kez** geçiyor. Kapsam md.5: rafineriler ve Darphane | ✅ |
| **KM-02** | Rakiplerin "Nisan 2026 KMTS tüm kuyumcuları kapsıyor" iddiası | kmts.gov.tr'de böyle bir tarih/kapsam **bulunamadı** | ❌ **Çürük iddia** |
| **KM-03** | Kalan risk | kmts.gov.tr mevzuat sayfası tam okunamadı; 12 Ağustos 2026 tarihli bir Usul-Esaslar değişikliği duyurusu var, içeriği incelenmedi | 🟡 **K2** |

**Üründe:** `urunler.kmts_kodu` ve `hareketler.kmts_kodu` — opsiyonel, **biçim doğrulaması yok** (şema yayımlanmadı, uydurulmadı).

---

## 5. KVKK

| # | Gereksinim | Dayanak | Durum | Üründe |
|---|---|---|---|---|
| **KV-01** | Veri güvenliği tedbirleri | md.12 | ✅ | AES-256-GCM + HMAC, diskte düz metin TCKN 0 |
| **KV-02** | Erişim kaydı | md.12 | ✅ | Okuma izi, append-only, **ibraz edilebilir** (SAHİP ekranı) |
| **KV-03** | Amaçla sınırlılık, süre | md.4 | ✅ | Saklama süresi maskelemesi |
| **KV-04** | Veri asgariliği | md.4 | ✅ | Denetim izinde **değer yazılmaz**, yalnız alan adı. TCKN `Cari`'de değil, işleme bağlı kayıtta |
| **KV-05** | Aydınlatma yükümlülüğü | md.10 | ❌ | **Metin yok** — hukuki metin, danışman verir |
| **KV-06** | VERBİS kaydı | — | ❌ | İşletme yükümlülüğü |

---

## 6. Kuyum KDV

| # | Gereksinim | Durum | Üründe |
|---|---|---|---|
| **KD-01** | Has altın bedeli KDV matrahından düşülür (özel matrah) | ✅ | `Fatura.KdvTuru = "KUYUM"` + `HasDeger` — devralınan kodda **zaten vardı** |

---

## 7. Özet — karşılanan / karşılanmayan

| Durum | Kalemler |
|---|---|
| ✅ **Karşılanıyor** (11) | M-01, M-02, M-03, M-04, V-01, V-02, KM-01, KV-01, KV-02, KV-03, KV-04, KD-01 |
| ❌ **Yapılacak, kaynağı kesin** (2) | **V-03** (30.000 TL tevsik) · **K-01** (sentetik taş) |
| ❌ **Kaynak doğrulanamadı — bilinçli yapılmadı** (2) | M-06 (beyan kademeleri) · V-04 (fatura haddi) |
| 🔒 **Dış bağımlılık** (2) | M-07 (MASAK API yok) · e-Fatura (K1) |
| ❌ **Hukuki metin** (2) | KV-05 (aydınlatma) · KV-06 (VERBİS) |

---

## 8. Mali müşavir teyidi bekleyen kalemler (K2)

Bu üç soru, tahminle kapatılmayacağı için kurucuya eskale edilmiştir:

1. **Fatura ve e-fatura haddi hangisidir?** Dört çelişen rakam bulundu (12.000 / 36.000 / 3.000.000 / 5.000.000 TL).
2. **Hurda alımında stopaj oranları nedir?** Satıcı tipine göre uygulanan oran **doğrudan para hareketini** etkiler.
3. **MASAK beyan formu kademeleri gerçekten kuyumcuyu bağlıyor mu?** Tedbirler Yönetmeliği'nde karşılığı bulunamadı.

**Bu üç soru bir sohbet konusu değil, bir belge talebidir.** Mali müşavire okunabilir bir metin olarak sunulmak üzere hazırlanmıştır.
