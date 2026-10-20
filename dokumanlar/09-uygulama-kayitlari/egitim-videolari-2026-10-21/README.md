# Eğitim videoları — Y-01 (21.10.2026)

Rakip taraması § 4.1 Y-01 (Hasfiyat 49 video, Pusula 27, KuyumSmart bölümlü): **ekran başına 1–2 dakikalık sesli video**,
bölüm zaman damgaları, Kılavuz ekranından izleme. Hiçbir rakipte "adım sayısı + kısayol" birlikte yok → anlatımda söylenir.

## Çıktılar
- `egitim-<kimlik>.mp4` (1440×900, 12 fps, H.264 + AAC −16 LUFS, gömülü bölüm işaretleri) — 9 video: satis · hurda · emanet ·
  banka · fatura · masak · kasa-kapanis · etiket · rapor. Depodaki tek kopya: `dotnet/Kuyumcu.Web/wwwroot/video/` (bu dizindeki mp4 ve kayıt artıkları .gitignore ile dışarıda).
- `BOLUMLER-<kimlik>.md` — bölüm başlangıçları (kılavuz ve Kılavuz ekranı bunlardan üretilir).
- `dotnet/Kuyumcu.Core/Kilavuz/EgitimVideolari.cs` — ÜRETİLMİŞ liste (betik/kilavuz-bolum.py); Kılavuz ekranı (`/kilavuz`) bu
  listeden oynatıcı + bölüm düğmeleri çizer; `docs/KULLANIM_KILAVUZU.md` "Eğitim videoları" bölümü + ekran bölümlerine tek satır.

## Hat (betik/)
1. `plan.py` → `plan-<kimlik>.json`: adımlar (eylemler: git · tik · tikVarsa · sec · yaz · tus · etiket · bekle · imlec · imlecSel ·
   kaydir · kopyala) + anlatım cümleleri (ton S/H/V). Yazım kuralı ses modeli için: şapka yok, kesme yok, rakam yok, ≥ 4 kelime.
2. `seslendir.sh` → onaylı sesle (Chatterbox v3, eşin referansı; 02.10 onayı) `klipler-egitim-<kimlik>.json` + wav'lar
   (`demo-kayit-2026-10-01-uzun/seslendirme/`). ~1 dk/cümle (MPS; kayıtla aynı anda yavaşlar).
3. `egitim-kaydet.mjs <kimlik> [--prova]` → başsız Chrome + CDP screencast; parolasız vekil (`inceleme-vekili.py` → 5181 → 5192);
   adım süresi = klip süresi + 1,3 sn; `--prova` ekran görüntüsü alır, screencast yapmaz.
4. `uret-egitim.py <kimlik>` → sessiz video (video-yap.py) + klipler adım başlangıcına (+0,4 sn) + loudnorm + bölüm işaretleri.
5. `kilavuz-bolum.py` → C# liste + kılavuz md; sonra `./dotnet/kilavuz-uret.sh`.

## Ortam (yalıtılmış)
- Şema `kuyumcu_egitim` = 20.10 23:47 yedeğinin kopyası (Altın Çarşı demo kiracısı); API 5192 (Debug, `dotnet run --no-build`);
  vekil 5181. Kayıtlardaki satış/kapanış bu şemaya yazılır, canlı demoya DOKUNMAZ. MASAK "DENEME KAYDI" bildirimi bu şemadan silindi.
- Kayıt sırası: kasa-kapanis EN SON (günü kilitler); prova kapanışı `POST …/gun-kapanisi/{gün}/ac` ile geri açıldı.

## Tarayıcı bulguları (kayıt sırasında)
- Tezgâh alt düğme satırı (Beklet · Fiyat teklifi ver · Vazgeç · Kaydet) 1280/1440'ta sağdan taşıyordu → `tema-yildiz.css` sarma
  (commit ile). CDP fare tıklaması Blazor düğmelerinde düşüyor → DOM `click()`; açılır listelerde çift tetiklenme → yalnız DOM.
