# İki oturum — iş bölümü, kurallar, durum defteri

> Kurucu 17.10: "çakışma olmadan iki tarafta devam etsin, emirleri senden veriyorum, durmayın".
> Emri veren: **ANA oturum** (995ecd38). Çalışan: **ANA** ve **FORK** ("son kuyumö (fork)", local_4c95c907).
> **RAPOR oturumu** (salt okunur) bu dosyayı, `git log`'u ve iki oturumun kayıtlarını okuyup kurucuya özetler.

## Alanlar (dosya sahipliği)

| Oturum | Alan | Dosyalar |
|---|---|---|
| **FORK** | Tezgâh · satış · satın alma · cari kartı · ürün | `Pages/Islemler*`, `Bilesenler/Yildiz/YildizTezgah*`, `Modules/Satis/*`, `Modules/Satinalma/*`, `Pages/SatinAlma*`, `Pages/Cariler*`, `Modules/Cari/*` (CariIban hariç), `Modules/Urun/*`, `tuval-satis.css`, kılavuz |
| **ANA** | Banka · fatura · rapor/maliyet · muhasebe aktarımı · yayın altyapısı | `Modules/Banka/*`, `Modules/Fatura/*`, `Modules/Maliyet/*`, `Modules/Rapor/*`, `Modules/Disaaktarim/*`, `Pages/Banka*`, `Pages/KarZarar*`, `Pages/Fatura*`, `Modules/Cari/CariIbanModulu.cs`, `betikler/yayin*.sh`, `betikler/qa-sunucu.sh` |

Alan dışı bir dosyaya girmek gerekirse **önce öteki oturuma yaz**, cevabı bekle.

## Ortak dosyalar (yalnız EKLEME, kendi bloğunda)
`SunucuIletiSozlugu.Veri.cs` (sona kendi bloğun) · `denetim.sh` · `tema-yildiz.css` (sona kendi bloğun) ·
`KuyumcuDbContext.cs` · `DuzeltmePolitikasi.cs` · `ModulKayit.cs` · `ham/olcum-2026-09-18.json` · `Contracts/*`.
Başkasının satırını değiştirme/silme yok.

## Kurallar
1. **Commit yalnız açık yollarla**: `git add <dosya> …`. `git add -A`, `git commit -a`, `git add .` YASAK.
   Ortak dosyada ötekinin de hunk'ı varsa yalnız kendi bloğunu sahnele (HEAD + kendi blok → `git update-index --cacheinfo`).
2. **Geçiş (migration) kilidi**: `dotnet ef migrations add` öncesi öteki oturuma "GEÇİŞ KİLİDİ: <ad>" yaz →
   üret → hemen commit → yedek (`yedekler/kuyumcu_net-<gün>-<ad>-oncesi.sql.gz`) → `dotnet ef database update` → "GEÇİŞ KİLİDİ AÇIK" yaz.
   Aynı anda iki geçiş üretilmez (model özeti ortak).
3. **Yayın yalnız** `zsh dokumanlar/09-uygulama-kayitlari/betikler/yayin-temiz.sh <etiket>` ile: yalnız COMMIT'lenmiş kod
   (`~/ByGatech-yerel/jewellery-yayin` ayrı kopyasından) yayına girer; ana klasördeki yarım iş karışmaz, sunucu
   yeniden başlarken de bayat sayılmaz. Yayından önce öteki oturuma "YAYIN" yaz; **aynı anda iki yayın yok**
   (17.10 23:23'te iki yayın çakıştı, kopyalama kilide çarptı). Eski `yayin-ac.sh` kullanılmaz.
4. **Test veritabanı**: FORK `kuyumcu_test_t1` / `t3`, ANA `kuyumcu_test_ana`. RAPOR test koşmaz.
5. **Her commit sonrası** aşağıdaki deftere tek satır: saat · oturum · commit · ne · test/denetim · yayın.
6. Kurucu kuralları geçerli: eksik/ertelenmiş iş yok; tarayıcıda gerçek veriyle kanıt; hata gizlenmez.

## Sıradaki işler (17.10 gece)
- **FORK**: M6b (Tezgâh'ta TC yazılınca müşteri bul/aç — `CariServisi.OlusturId` + `GelenOdemeServisi.Coz` hazır),
  M5 (gram ve TL yan yana: tezgâh satırı, ürün kartı, fiş), Tezgâh Basit modda 14 → en çok 12 düğme.
- **ANA**: M7 (banka sağlayıcı soyutlaması + sahte sağlayıcı "havale gönder" + arka plan çekici + ekranda canlı yenileme,
  (hesap, banka_referans) tekilliği), M8 kalanı (açıklamadan eşleştirme önerisi, açık fatura önerisi),
  M11'in entegratörsüz kısmı (gün sonu toplu e-Arşiv onay listesi, Luca/Zirve aktarımı), P-23 canlı 2 kayıt.
- **Kurucu kararı bekleyen**: e-Fatura entegratörü, Harem veri izni, P-30 kronometreli demo ölçümü.

## Durum defteri
| Saat | Oturum | Commit | İş | Test / denetim | Yayın |
|---|---|---|---|---|---|
| 17.10 13:20 | ANA | ec3b4ec | M7a gelen ödeme: müşteri bul/aç, borçtan düş kararı, IBAN listesi | 12708/0 · P-23,P-30 | m7a |
| 17.10 22:55 | ANA | 84de2f5 | M9 kâr ayrımı (makas/altın farkı) + alış işçilik belgesi | M9 10/10 · P-23,P-30 | — |
| 17.10 23:20 | FORK | e46f970 | Tezgâh "Fatura kesilsin mi?" + tek adımlı "Mal geldi" | (FORK bildirir) | fatura-sorusu-1710 |
| 17.10 23:25 | ANA | — | Temiz yayın altyapısı (yayin-temiz.sh, .kaynak-kok) | yayın + kirli ağaçta yeniden başlatma 200 | temiz-1710 @ 8546bec |
| 17.10 23:25 | ANA | — | M9 tarayıcı kanıtı: kartlar, uyarı, fatura girişi, 375 px, koyu | ✓ | — |
| 17.10 23:22 | FORK | 8546bec | Fatura sorusu tek satır (alt blok + İndirim'i örtüyordu) | tarayıcı 1280/375 açık/koyu | fatura-sorusu-1710b |
| 17.10 23:25 | FORK | 6cad8f8 | Evet/Hayır hizası — ⚠ ANA'nın yayinla.sh + qa-sunucu.sh değişiklikleri de yanlışlıkla bu commit'e girdi (commit -a; içerik doğru) | — | fork-6cad8f8 @ d2a1feb |
| 17.10 23:35 | FORK | (bu commit) | Evet/Hayır dikey orta (yz-b2 align-self eziyordu; ölçüm orta 720/720) | tarayıcı ölçümü | (sonraki) |
| 17.10 23:55 | FORK | (bu commit) | M6b: tezgâhta TC/VKN ile müşteri bul ya da aç (POST /islemler/musteri-ac, tezgâhtar dahil; seçicide kendiliğinden seçim + "Müşteriyi aç ve seç") | 12635/12635 · denetim P-23,P-30 · M6b mutasyon kanıtı | — (ANA yayınlar) |
