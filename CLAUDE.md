# Kuyumcu ERP — OTURUM BAŞLANGIÇ TALİMATI (önce bunu oku)

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Amaç: yeni oturum
> nerede kalındığını 2 dakikada bilsin, hiçbir şeyi yeniden keşfetmesin.

## 1. İLK YAPACAKLARIN (sırayla)
1. `SONRAKI-OTURUM-PROMPT.md` dosyasını **tamamen** oku — bağlayıcı kurallar, yapılan paketler
   (A1–A16), açık kalanlar, tuzaklar. En sonda **A17 "YARIM KALDI"** bölümü var: kaldığımız yer.
2. `~/.claude/CLAUDE.md` (şirket kuralları) ve `~/.claude/projects/.../memory/kuyumcu-erp-ortam-tuzaklari.md`
   (python3 artık çalışıyor, perl yine güvenli; `dotnet ef` `--no-build` YASAK; API `--urls 5199`; zsh `${=VAR}`).
3. Kurucuya tek cümle: hangi kalemden devam edeceğini söyle, sonra sormadan devam et
   ("sıradaki iş" deyip durmak yok; her adım: `dotnet test` 0 başarısız + `dotnet/denetim.sh` TEMİZ +
   canlıda gerçek veriyle doğrulama + mutasyon kanıtı + test kiracısını sil).

## 2. KIRMIZI ÇİZGİLER
- **COMMIT YOK** (kurucu: "kodları commitleme yok sakın ha"). Çalışma ağacında kal.
- **Varsayım yok.** Bilinmeyeni doğrula ya da sor. Sır/parola ekrana basma ("var/yok").
- TurkReport hook `rm`/`mv`/`: >` bloklar — geçici dosyayı `printf '' >` ile boşalt, silmeyi kurucuya bırak.
- İzinsiz mevcut kod değiştirme; dil Türkçe.
- **GERÇEK TARAYICI KURALI (kurucu, 2026-09-24):** ekrana dokunan hiçbir iş gerçek tarayıcıda ölçülüp ekran
  görüntüsüyle kanıtlanmadan "geçti" sayılmaz; bUnit/API kanıtı görünürlük/sığma/taşma/biçim için YETMEZ.
  1280 + 375 px · Basit + Profesyonel · açık + koyu; ölçüm `dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js`,
  parolasız oturum `betikler/inceleme-vekili.py`. Kurucuya "tamam" demeden önce ekran görüntüsüne bakılır.

## 3. KALDIĞIMIZ YER (2026-09-16 15:57 — otonom dalga uygulaması sürüyor)
**Paket 33 + DALGA 1 (4-A, UX-1-A, UX-3-A, 1-A) + DALGA 2a (UX-4a, 49, UX-4b, UX-4c) BİTTİ**; dalga 2b (UX-4d → UX-1-B → 38-A → 48-A → 45-API → 6-A → 3-A) 15:55'te başladı, QA turu 2 BİTTİ (27 sayfa, 149 bulgu, 11 kritik; önceki 147 → 12 kapandı) ve kurul tur 2 HAZIR DEĞİL 3,0/10 (HAZIRLIK-KURULU-2.md); MÜHÜR 2/3/4 ve F-8 birim ekonomi belgesi hazır (ana belge § MÜHÜR 2-4, `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md`); kurucu kalemleri `KURUCUYA-KARARLAR-2026-09-16.md` (`SONRAKI-OTURUM-PROMPT.md` § A17-BİTTİ ve § A18). **Şimdi (§ A18, revize dalga planı 2a→8; kurucu: "satışa hazır diyene kadar devam, her veri girişi düzenlenebilir, en güzel arayüz"):** kalan 27 paket + Katman 8 deneyim (UX-1 her veri girişi düzenlenebilir · UX-2 görsel · UX-3 basit/profesyonel mod) — mimari `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`, uygulama dalgaları Opus uygulayıcı + hasım doğrulayıcı + orkestratör kapısı; raporlar `dokumanlar/09-uygulama-kayitlari/`. Kurucu talimatı: soru sorma, ekip oyuyla karar ver, kapsam dışı (Katman 1) hariç hepsini bitir.
**23 Eylül 2026 — SADELEŞTİRME PLANI (kurucu: müşteri "çok karışık, Pusula sade" dedi):** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` (kök) + `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md`. Ürün adı **ByGatech** (kurucu kararı 2 Kasım 2026: önceki ad TÜRKPATENT'te başka kuyumcu adına tescilli çıktı → bütün projeden kaldırıldı; tek kaynak `dotnet/Kuyumcu.Core/I18n/Sozlukler.Marka.cs`; eski ad yalnız `AD-ARASTIRMA-2026-11-01.md` ve `MARKA-ARASTIRMA-*` araştırma kayıtlarında kalır — oraya yazma, buraya yazma). Taslak tuvali: https://claude.ai/artifact/XZha3bXRKPVz3YPn4jKtQx (Platin Mavi vb. 4 tema seçilebilir, Manrope+Sora, gövde 18 px). Plan yazıldı, kod yazılmadı; uygulama ekip oyu (D1–D11) + kurucunun görsel tercihi (renk şeması, yazı ölçeği, menü zemini) ile başlar. Kural: özellik silinmez, jargon sıfır (hedef kitle ilkokul/lise mezunu tezgâhtar), ekran başına tek birincil düğme; § 0 devralma algoritması.
Durum (2026-09-22 06:04 kapısı, orkestratör kapısı): 3869/3869 test yeşil · dotnet/denetim.sh TEMİZ · 66 migration · 0 uyarı 0 hata. Tek kaynak: `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` (devralma algoritması + son durum); betikler `dokumanlar/09-uygulama-kayitlari/betikler/` (kapi.sh, dalga-uygula.js, qa-turu.js, hazirlik-kurulu.js). İnceleme/QA sunucusu: `.claude/launch.json` "kuyumcu-qa" → http://localhost:5199 (Release yayın anlık görüntüsü, scratchpad/qa-yayin; "kuyumcu-api" = dotnet run --no-build; ajanlar canlı doğrulama için 5198 kullanır).
