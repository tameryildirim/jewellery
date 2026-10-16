# Paydaş Gereksinimleri (StRS — Stakeholder Requirements Specification)

| | |
|---|---|
| **Belge kimliği** | KYM-StRS-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | TASLAK — *pilot kuyumcu görüşmesiyle doğrulanmalı (K4)* |
| **Dayanak** | **ISO/IEC/IEEE 29148:2018** §9.4 |

---

## ⚠ 0. Doğrulama uyarısı — bu belgenin en önemli maddesi

**Bu belgedeki paydaş gereksinimleri, gerçek kuyumcularla yapılmış görüşmelere dayanmamaktadır.**

Kaynakları şunlardır:
- Devralınan kod tabanının işlev kapsamı (ne yapıldığına bakarak ne istendiğini çıkarmak)
- 19 rakip ürünün **yayımlanmış** özellik listeleri
- Mevzuat metinleri (MASAK, VUK, KVKK)
- Şikâyet sitelerinde bulunan **iki** kullanıcı kaydı

**Bu, gerçek saha araştırmasının yerini tutmaz.** Pilot kuyumcu erişimi (Kurucu kalemi **K4**) sağlandığında bu belge yeniden yazılmalıdır.

Aşağıdaki her gereksinimin yanında **kaynak** ve **doğrulama durumu** vardır. Anayasa md.1 gereği doğrulanmamış bir gereksinim "doğrulandı" diye işaretlenmemiştir.

---

## 1. Paydaşlar

| # | Paydaş | Rol |
|---|---|---|
| P1 | **Kuyumcu sahibi** | İşletme sahibi; satın alma kararını verir |
| P2 | **Tezgâhtar** | Günlük işlemleri giren personel |
| P3 | **Muhasebe personeli / mali müşavir** | Belge, fatura, mutabakat |
| P4 | **Müdür** | Çok şubeli işletmede şube yöneticisi |
| P5 | **Müşteri (veri sahibi)** | Kişisel verisi işlenen kişi |
| P6 | **Denetçi** (MASAK, vergi) | İbraz ister |
| P7 | **ByGatech** | Geliştirici, bakım sağlayıcı |

---

## 2. Paydaş gereksinimleri

### P1 — Kuyumcu sahibi

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-01** | İşletmenin anlık mali durumunu (kasa, cari, stok, has) görebilmeli | Mevcut kod + rakip özellikleri | ⬜ Pilot |
| **StR-02** | Personelin ne yaptığını görebilmeli | Denetim izi mevcut | ⬜ Pilot |
| **StR-03** | Yasal yükümlülüklerini hatırlamak zorunda kalmamalı; sistem durdurmalı | Mevzuat + fail-closed tasarım | ⬜ Pilot |
| **StR-04** | **Verisi kendisinin olmalı; istediği an dışarı alabilmeli** | **Gerçek şikâyet kaydı** (2 örnek) | 🟡 Kısmi — zayıf kanıt |
| **StR-05** | Çok şubeyi tek yerden yönetebilmeli | Mevcut kod | ⬜ Pilot |
| **StR-06** | Fiyatın öngörülebilir olması; sonradan sürpriz ücret çıkmaması | **Gerçek şikâyet kaydı** | 🟡 Kısmi |

> **StR-04 ve StR-06'nın kaynağı:** Şikâyetvar'da bulunan iki kayıt — M&L Yazılım'da "ömür boyu lisans" denip 5 yıl sonra ~600 USD güncelleme ücreti istenmesi ve verinin programda kilitli kalması; Bilsoft'ta dönem devri için ek ücret. **İki kayıt genelleme yapmaya yetmez** ama sıfır kayıttan iyidir ve bu ikisi aynı şeye işaret etmektedir.

### P2 — Tezgâhtar

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-07** | İşlem girişi hızlı olmalı; müşteri tezgâhta beklememeli | Sektör doğası | ⬜ Pilot |
| **StR-08** | Yasal zorunluluk çıktığında ne yapacağı **ekranda** yazmalı | Fail-closed tasarım gereği | ⬜ Pilot |
| **StR-09** | Yetkisi olmayan ekranı **görmemeli** | Rol matrisi mevcut | ✅ Tasarımda |
| **StR-10** | Yanlışlıkla veri silememeli | Append-only + soft-delete | ✅ Tasarımda |

### P3 — Muhasebe / mali müşavir

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-11** | Gün sonu POS–fatura mutabakatı alabilmeli | Mevzuat + mevcut kod | ⬜ Pilot |
| **StR-12** | Gider pusulası VUK md.234 alanlarıyla **yazdırılabilmeli** | VUK md.234 | ✅ Mevzuat doğrulandı |
| **StR-13** | Stopaj doğru hesaplanmalı ve **kasadan net tutar çıkmalı** | GVK md.94 | 🟡 Oran teyidi **K2** |
| **StR-14** | Kuyum KDV özel matrahı doğru uygulanmalı | KDVK | ✅ Mevcut |
| **StR-15** | e-Fatura kesebilmeli | Mevzuat | 🔒 **K1** |

### P4 — Müdür

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-16** | Kendi şubesinin verilerini yönetebilmeli | Mevcut kod | ⬜ Pilot |
| **StR-17** | **Denetim izine erişememeli** — o sahibin aracıdır | Güvenlik kararı | ✅ Canlı: 403 |

### P5 — Müşteri (veri sahibi)

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-18** | Kişisel verisi gerektiğinden uzun tutulmamalı | KVKK | ✅ Saklama maskelemesi |
| **StR-19** | Verisine kimin baktığı kayıtlı olmalı | KVKK md.12 | ✅ Okuma izi |
| **StR-20** | Verisi şifreli saklanmalı | KVKK md.12 | ✅ AES-256-GCM |
| **StR-21** | Aydınlatma metni sunulmalı | KVKK md.10 | ❌ **Metin yok** — hukuk danışmanı |

### P6 — Denetçi

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-22** | Kimlik tespiti kayıtları **8 yıl** ibraz edilebilmeli | MASAK md.46 | ✅ |
| **StR-23** | Kayıtlar **değiştirilemez** olmalı | Denetim ilkesi | ✅ Append-only, mutasyon kanıtlı |
| **StR-24** | ŞİB **10 iş günü** içinde yapılmalı | MASAK md.28/2 | ✅ Sayaç mevcut |
| **StR-25** | Bildirim MASAK'a **ulaşmalı** | MASAK md.27 | 🔒 **API yok** — elle |

### P7 — ByGatech

| # | Gereksinim | Kaynak | Doğrulama |
|---|---|---|---|
| **StR-26** | Kiracı sızıntısı **imkânsız** olmalı | Ürünün varlık şartı | ✅ İki katman + mutasyon |
| **StR-27** | Mevzuat değişince kod değişmemeli | Bakım maliyeti | ✅ Yapılandırmadan |
| **StR-28** | Yeni geliştirici kararların **gerekçesine** ulaşabilmeli | Bus factor 1 | ✅ 12 ADR |

---

## 3. Doğrulama durumu özeti

| Durum | Adet |
|---|---|
| ✅ Tasarımda karşılanıyor / mevzuatla doğrulanmış | 13 |
| 🟡 Kısmi veya zayıf kanıtlı | 4 |
| ⬜ **Pilot görüşmesi bekliyor** | 8 |
| ❌ / 🔒 Dış bağımlılık | 3 |

**8 gereksinim hiçbir gerçek kullanıcıyla doğrulanmamıştır.** Bu, belgenin en büyük zayıflığıdır ve gizlenmemektedir.

---

## 4. Çatışan gereksinimler ve çözümleri

| Çatışma | Çözüm |
|---|---|
| **StR-18** (veri uzun tutulmasın) ↔ **StR-22** (8 yıl saklansın) | Silme değil **maskeleme**: kimlik alanları maskelenir, tutar/tarih/kademe korunur ([ADR-0008](../02-mimari/adr/0008-saklama-suresi-maskeleme.md)) |
| **StR-07** (hızlı giriş) ↔ **StR-03** (yasal durdurma) | Kapı **yalnız eşik aşılınca** açılır; eşik altında hiçbir ek alan istenmez |
| **StR-04** (veri dışarı alınabilsin) ↔ **StR-20** (veri korunsun) | Dışa aktarım varsayılan **maskeli**; tam veri ayrı onay + gerekçe + iz + oran sınırı |
| **StR-02** (sahip personeli görsün) ↔ **StR-19** (müşteri mahremiyeti) | Denetim izinde **değer yazılmaz**, yalnız alan adı |
