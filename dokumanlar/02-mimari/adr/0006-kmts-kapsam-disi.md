# ADR-0006 — KMTS perakende kuyumcu için kapsam dışıdır; yalnız opsiyonel alan tutulur

**Durum:** Kabul edildi · **Tarih:** 2026-09-11 · **Yeniden doğrulandı:** 2026-09-13

## Bağlam

Bir önceki değerlendirmemiz, Kıymetli Maden Takip Sistemi'ni (KMTS) "olmazsa olmaz" bir yükümlülük olarak listelemişti. **Bu tespit yanlıştı ve kaynağı bir blog yazısıydı.**

Pazarda da benzer iddialar dolaşıyor: en az iki rakip ürün sitesinde "Nisan 2026 KMTS düzenlemesine tam uyumluluk" ifadesi bulunuyor.

## Karar

Birincil kaynağa gidildi: Hazine ve Maliye Bakanlığı, *KMTS Uygulamasına İlişkin Usul ve Esaslar* (487 satır, tam metin tarandı).

- **"kuyumcu" kelimesi: 0 kez geçiyor.**
- **"perakende" kelimesi: 0 kez geçiyor.**
- Kapsam (md.5): *"rafineriler tarafından üretilen granül halindeki gümüş haricindeki standart işlenmemiş kıymetli madenlerin ve basılı kıymetli madenlerin tamamının KMTS'ye dahil edilmesi zorunluluğu"*.
- Yükümlü taraf baştan sona **rafineriler** ve **Darphane**. İşlenmiş takı kapsam dışı.

→ KMTS bir zorunluluk olarak plandan **çıkarıldı**. Yerine ucuz bir hazırlık kondu: `urunler.kmts_kodu` ve `hareketler.kmts_kodu` — opsiyonel, **biçimi doğrulanmıyor**.

## Gerekçe

Blog yazısı birincil kaynak değildir. Bir yükümlülüğü kabul etmeden önce mevzuatın kendisi okunur.

Biçim doğrulaması yazılmadı çünkü **şema yayımlanmamıştır**. Bilinmeyen bir biçim için doğrulama yazmak uydurma olurdu.

## Sonuçlar

**Olumlu** — Gereksiz bir modül yazılmadı; kapsam genişlerse alan hazır.

**Olumsuz / kalan risk** — `kmts.gov.tr` mevzuat sayfası tam okunamadı; sitede 12 Ağustos 2026 tarihli bir Usul-Esaslar değişikliği duyurusu bulunuyor, içeriği incelenmedi. **Mali müşavir teyidi gerekiyor.**

## Yeniden doğrulandı: 2026-10-01 (PAKET YASAL-4 / Y4-C)

- **Birincil kaynaklar:** Resmî Gazete 29.07.2026 / 33324 (Kıymetli Maden Standartları ve Rafinerileri Hakkında Tebliğ değişikliği, https://www.resmigazete.gov.tr/eskiler/2026/07/20260729-2.htm) ve KMTS 12.08.2026 duyurusu (https://www.kmts.gov.tr/duyuru/kiymetli-maden-takip-sistemi-uygulamasina-iliskin-usul-ve-esaslar-degisikligi-hakkinda).
- **Sonuç:** RG metninde bildirim yükümlülüğü Türkiye'de yerleşik tüzel kişi statüsündeki rafinerilere yöneliktir; perakende kuyumcu (gerçek kişi) için bildirim yükümlülüğü incelenen metinlerde **bulunamadı**. KMTS duyurusu da yükümlü meslek gruplarını saymıyor. Karar **değişmedi**: perakende kapsam dışı.
- **Ürüne etkisi:** ürün formundaki KMTS kodu alanı varsayılan **gizli** (kiracı ayarı `kmts_kullanimi`, Ayarlar → İşletme, yalnız SAHİP açar; açıkken yalnız Profesyonel modda "Ek bilgiler" katlanırında). Kod biçimi YASAL-1 anahtarı `KMTS_KOD_DESENI` ile tek yerden (`Common/KmtsKodu.cs`) doğrulanır; anahtar KAPALI (boş) → yalnız uzunluk + yazdırılabilir karakter.
- **Açık:** 12.08.2026 Usul-Esaslar metninin tam içeriği okunamadı (duyuru sayfası özet veriyor). Mali müşavir teyidi gerekir; perakendeye yükümlülük çıkarsa ayrı entegrasyon paketi açılır.

---
*Bu kayıt Michael Nygard'ın ADR formatını izler (15 Kasım 2011). ISO/IEC/IEEE 42010:2022 §6.10 "Recording of architecture decisions and rationale" maddesini karşılar.*
